using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// SHADOW BOLT - Void Lance + Contagion, the third combination (.ai/combo-spells.md section 7; ComboCatalog;
// in-flight item 17: Eric moved the old chain spell into the Conjunction roster on 2026-11-05).
//
// Made from Void Lance at level 8 down Cosmic Collapse and Contagion at level 8 down Pandemic, and from
// nothing else: both parents ride on the spell frozen as they were at the fusion (SpellData.ComboParents),
// and every number here is built from theirs. The verb is FESTER.
//
//   aim      the thickest pack in reach: the bolt wants bodies standing together, as the plague does.
//   cadence  the bolt on the lance's own cooldown; the cave-ins on Pandemic's shared beat, every 2 s.
//   demand   feed it crowds. Against stragglers the curse has nothing to fester onto and the spell is
//            at its weakest - Contagion's inheritance, kept on purpose.
//   payload  a bolt of void that leaps body to body laying the festering curse - poisoned, slowed and
//            exposed, the old Shadow Bolt's identity - and, on each beat, a small cosmic collapse on
//            every cursed body that strikes everything round it and passes the curse on.
//
// WHAT IT IS MADE OF. The bolt's hit and the cave-in are shares of the frozen lance's strike (Cosmic
// Collapse's x2.4 already folded in by SpellData.GetDamageAtLevel); the cave-in's reach is a share of the
// lance's collapse burst (VoidLance.CollapseReach). The curse's poison is the frozen plague's own dose,
// its length the plague's mark (Contagion's MarkSeconds plus its zone-duration lines), the leap's reach
// the plague's jump (SpreadRadius plus its area lines), and the beat is Pandemic's (Contagion.PandemicSeconds,
// marks swelling over its last fifth as Pandemic's do). The slow and the exposure are the conjunction's
// own riders, carried over from the spell it used to be (0.72x, +18% damage taken).
//
// BOUNDED, because the old Shadow Bolt once made 9,841 arcs from one cast (.ai/spell-roster.md, "a cast
// is capped"): the bolt strikes no enemy twice in one cast; at most MaxCursed bodies carry the curse at
// once; a beat hits each enemy on the field at most once (Pandemic's own rule); and each cave-in passes
// the curse to at most FesterSpread bodies. Per-frame work is drawing only - the beat and the casts walk
// EnemyRegistry.Snapshot, never GetNodesInGroup, and nothing here allocates per frame.
//
// THE LOOK (no new sheet, no new shader): the bolt as jagged strokes of the Darkness ramp leaping body
// to body (SpellStrokes, the pixel-art stroke contract); the curse as a dark ring at the carrier's feet
// with three motes of rot turning round it, swelling together before each beat; the cave-in as a disc of
// void that swells and caves in on itself, its rim going white-hot as it closes (Cosmic Collapse's own
// timing), with thin strokes of the Poison ramp festering out to each body it curses. Silent for now, as
// the old Shadow Bolt was: its voice is the audio pass's (.ai/audio-manifest.md).
public partial class ShadowBolt : Node2D
{
	public const string SpellId = "shadow_bolt";
	public const string LanceSpellId = "void_lance";
	public const string PlagueSpellId = "contagion";

	// --- the combination's own multipliers (tuned in scenes/ShadowBolt.tscn) --------------------------------

	/// <summary>Each body the bolt leaps through takes this share of the frozen lance's strike.</summary>
	[Export] public float BoltShare { get; set; } = DefaultBoltShare;

	/// <summary>BoltShare's default, which Freeze also writes onto the card as the stand-in damage.</summary>
	public const float DefaultBoltShare = 0.75f;

	/// <summary>Each beat's cave-in deals this share of the frozen lance's strike, to the carrier and all in reach.</summary>
	[Export] public float CollapseShare { get; set; } = 0.5f;

	/// <summary>The cave-in's reach as a share of the frozen lance's own collapse burst (VoidLance.CollapseRadius).</summary>
	[Export] public float CollapseReachShare { get; set; } = 0.55f;

	/// <summary>How many bodies each cave-in passes the curse to, nearest first.</summary>
	[Export] public int FesterSpread { get; set; } = 2;

	/// <summary>The most bodies that may carry the curse at once. Contagion's own MaxInfected.</summary>
	[Export] public int MaxCursed { get; set; } = 24;

	/// <summary>Bodies one cast leaps through, the card's count (Freeze). Contagion's MaxCarriersPerCast.</summary>
	public const int DefaultBoltLeaps = 6;

	// The curse's riders: the old Shadow Bolt's identity (poison, slow, expose), now the conjunction's own.
	public const float CurseSlowMultiplier = 0.72f;
	public const float CurseVulnerableBonus = 0.18f;

	// The plague's jump, as Contagion's scene carries it (SpreadRadius): the leap's base reach, grown by
	// the frozen plague's area lines exactly as Player.FireContagion grows the jump.
	public const float PlagueJumpBase = 95f;
	// Contagion's MarkSeconds: the curse's base length, grown by the plague's zone-duration lines.
	public const float CurseBaseSeconds = 6f;

	// One leap of the bolt lands every step; the old chain's cadence (ArcDuration 0.08).
	public const float LeapStepSeconds = 0.07f;
	// A cave-in's life: swell for its first 0.3, cave for the rest - the lance's own hole timing.
	public const float CollapseSeconds = 0.45f;
	// How long a leap's stroke, or a fester's, stays drawn.
	public const float ArcSeconds = 0.28f;

	public Player PlayerRef { get; set; }

	/// <summary>The combination, which every hit is credited to.</summary>
	public SpellData Spell { get; private set; }

	private SpellData lance;
	private SpellData plague;

	// What the last cast handed over.
	private float damageMultiplier = 1f;
	private float areaMultiplier = 1f;
	private float durationMultiplier = 1f;

	// --- the curse ----------------------------------------------------------------------------------------

	private struct Curse
	{
		public Enemy Enemy;
		// The enemy's instance id, kept here so a curse on a freed body can still be unregistered.
		public ulong Id;
		public Vector2 LastSeen;
		public float Remaining;
	}

	private readonly List<Curse> curses = new();
	private readonly HashSet<ulong> cursedIds = new();
	private float pollTimer;

	// Pandemic's beat: one clock for every cursed body, so the field goes off together.
	private float beatClock;
	private long answeredBeat;
	private readonly HashSet<ulong> hitThisBeat = new();

	// --- the bolt in flight -------------------------------------------------------------------------------

	// One cast at a time: where the bolt is, how many leaps it has left, and what it has already struck
	// (no enemy twice in one cast - the rule the old runaway chain taught, .ai/spell-roster.md).
	private Vector2 leapFrom;
	private int leapsLeft;
	private float leapTimer;
	private readonly HashSet<ulong> struckThisCast = new();

	// --- what is drawn ------------------------------------------------------------------------------------

	private struct Arc
	{
		public Vector2 A;
		public Vector2 B;
		public float Age;
		public uint Seed;
	}

	private struct CaveIn
	{
		public Vector2 At;
		public float Reach;
		public float Age;
	}

	private readonly List<Arc> boltArcs = new();
	private readonly List<Arc> festerArcs = new();
	private readonly List<CaveIn> caveIns = new();

	// The bolt's strokes in the Darkness ramp, the fester's in Poison's, each on a layer of its own
	// (SpellStrokes tones are per material); the cave-ins as snapped rects on a third. All in the air,
	// over the bodies; the curse rings are this node's own draw, on the floor at the carriers' feet.
	private SpellStrokes boltStrokes;
	private SpellStrokes festerStrokes;
	private Node2D boltLayer;
	private Node2D festerLayer;
	private Node2D holeLayer;
	private float clock;

	private static readonly ElementColors.Ramp Dark = ElementColors.GetRamp(Element.Darkness);
	private static readonly ElementColors.Ramp Rot = ElementColors.GetRamp(Element.Poison);
	private static readonly ElementColors.Ramp Seam = ElementColors.GetRamp(Element.Arcane);
	private static readonly Color Occ = new Color(0.020f, 0.027f, 0.047f, 1f);

	// Scratch for a cast's aim and a beat's spread. Reused, never allocated per frame.
	private readonly List<Enemy> packScratch = new(64);
	private readonly List<Enemy> spreadScratch = new(8);

	public override void _Ready()
	{
		// The curse rings are marks at the carriers' feet: on the floor, under every body, over the pools
		// (DrawLayers.GroundTell, where Contagion draws its carriers' marks).
		ZAsRelative = false;
		ZIndex = DrawLayers.GroundTell;

		boltStrokes = new SpellStrokes(64);
		boltStrokes.SetTones(Dark, Dark.Edge, Occ);
		boltLayer = new Node2D { Name = "ShadowBoltArcs", ZAsRelative = false, ZIndex = DrawLayers.Air, Material = boltStrokes.Material };
		AddChild(boltLayer);
		boltLayer.Draw += DrawBoltArcs;

		festerStrokes = new SpellStrokes(96);
		festerStrokes.SetTones(Rot, Rot.Edge, Occ);
		festerLayer = new Node2D { Name = "ShadowBoltFester", ZAsRelative = false, ZIndex = DrawLayers.Air, Material = festerStrokes.Material };
		AddChild(festerLayer);
		festerLayer.Draw += DrawFesterArcs;

		holeLayer = new Node2D { Name = "ShadowBoltCaveIns", ZAsRelative = false, ZIndex = DrawLayers.Air };
		AddChild(holeLayer);
		holeLayer.Draw += DrawCaveIns;
	}

	/// <summary>
	/// The fire loop's cast (Player.FireShadowBolt): hurls one bolt at the thickest pack in
	/// <paramref name="castRange"/> and keeps the node current. False with nothing in reach.
	/// </summary>
	public bool Cast(SpellData combination, SpellData frozenLance, SpellData frozenPlague, int leaps,
		float castRange, float playerArea, float playerDamage, float duration)
	{
		if (combination == null || frozenLance == null || frozenPlague == null || PlayerRef == null)
			return false;
		Spell = combination;
		lance = frozenLance;
		plague = frozenPlague;
		areaMultiplier = playerArea;
		damageMultiplier = playerDamage;
		durationMultiplier = duration;

		Enemy target = ThickestPack(castRange);
		if (target == null)
			return false;

		// A cast still in flight resolves where it stands; the new bolt is the one animated.
		while (leapsLeft > 0)
			LeapOnce();

		struckThisCast.Clear();
		leapFrom = PlayerRef.GlobalPosition;
		leapsLeft = Math.Max(1, leaps);
		leapTimer = 0f;
		// The first leap lands now: a cast that visibly does nothing for a beat reads as a misfire.
		LeapTo(target);
		return true;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef) || lance == null || plague == null)
			return;
		// A boss is arriving: the field is held and so is the curse - the beat does not advance and the
		// bolt waits mid-leap (the fire loop is silent already; Player.HeldForBossEntrance).
		if (PlayerRef.HeldForBossEntrance)
			return;
		float dt = (float)delta;

		if (leapsLeft > 0)
		{
			leapTimer -= dt;
			if (leapTimer <= 0f)
				LeapOnce();
		}

		beatClock += dt;
		long beat = (long)(beatClock / Contagion.PandemicSeconds);
		if (beat != answeredBeat)
		{
			answeredBeat = beat;
			CaveInAll();
		}

		pollTimer -= dt;
		if (pollTimer <= 0f)
		{
			pollTimer = 0.1f;
			Poll(0.1f);
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		clock += dt;
		for (int i = boltArcs.Count - 1; i >= 0; i--)
		{
			Arc arc = boltArcs[i];
			arc.Age += dt;
			if (arc.Age > ArcSeconds) boltArcs.RemoveAt(i); else boltArcs[i] = arc;
		}
		for (int i = festerArcs.Count - 1; i >= 0; i--)
		{
			Arc arc = festerArcs[i];
			arc.Age += dt;
			if (arc.Age > ArcSeconds) festerArcs.RemoveAt(i); else festerArcs[i] = arc;
		}
		for (int i = caveIns.Count - 1; i >= 0; i--)
		{
			CaveIn hole = caveIns[i];
			hole.Age += dt;
			if (hole.Age > CollapseSeconds) caveIns.RemoveAt(i); else caveIns[i] = hole;
		}
		QueueRedraw();
		boltLayer?.QueueRedraw();
		festerLayer?.QueueRedraw();
		holeLayer?.QueueRedraw();
	}

	// --- the bolt -----------------------------------------------------------------------------------------

	// The thickest pack in reach: the enemy with the most neighbours within one leap of it. Ties go to
	// the nearer one, so a lone straggler is still a target when nothing stands together.
	private Enemy ThickestPack(float range)
	{
		Vector2 at = PlayerRef.GlobalPosition;
		float rangeSq = range * range;
		packScratch.Clear();
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (enemy != null && IsInstanceValid(enemy) && !enemy.IsDying
				&& at.DistanceSquaredTo(enemy.GlobalPosition) <= rangeSq)
				packScratch.Add(enemy);
		}
		if (packScratch.Count == 0)
			return null;
		float leapSq = LeapReach() * LeapReach();
		Enemy best = null;
		int bestCount = -1;
		float bestDist = float.MaxValue;
		foreach (Enemy candidate in packScratch)
		{
			int count = 0;
			foreach (Enemy other in packScratch)
			{
				if (!ReferenceEquals(candidate, other)
					&& candidate.GlobalPosition.DistanceSquaredTo(other.GlobalPosition) <= leapSq)
					count++;
			}
			float d = at.DistanceSquaredTo(candidate.GlobalPosition);
			if (count > bestCount || (count == bestCount && d < bestDist))
			{
				best = candidate;
				bestCount = count;
				bestDist = d;
			}
		}
		return best;
	}

	// One leap: the nearest body within reach of where the bolt stands that this cast has not struck.
	// Nothing in reach ends the cast - the bolt does not cross the field for a straggler.
	private void LeapOnce()
	{
		float reachSq = LeapReach() * LeapReach();
		Enemy next = null;
		float best = reachSq;
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying || struckThisCast.Contains(enemy.GetInstanceId()))
				continue;
			float d = leapFrom.DistanceSquaredTo(enemy.GlobalPosition);
			if (d <= best)
			{
				best = d;
				next = enemy;
			}
		}
		if (next == null)
		{
			leapsLeft = 0;
			return;
		}
		LeapTo(next);
	}

	private void LeapTo(Enemy target)
	{
		struckThisCast.Add(target.GetInstanceId());
		boltArcs.Add(new Arc { A = leapFrom, B = target.GlobalPosition, Age = 0f, Seed = (uint)(target.GetInstanceId() % 9973) });
		leapFrom = target.GlobalPosition;
		leapsLeft--;
		leapTimer = LeapStepSeconds;
		int hit = Math.Max(1, Mathf.RoundToInt(lance.GetDamageAtLevel(lance.CurrentLevel) * damageMultiplier * BoltShare));
		PlayerRef.DealDamageToEnemy(target, hit, source: Spell, numberElement: Element.Darkness);
		ApplyCurse(target);
	}

	// --- the curse ----------------------------------------------------------------------------------------

	// The plague's jump, grown by the frozen plague's own area lines exactly as Player.FireContagion
	// grows SpreadRadius; the bolt leaps as far as the plague it carries.
	private float LeapReach() =>
		(PlagueJumpBase + plague.GetEffectValueAtLevel(SpellEffect.AreaSize, plague.CurrentLevel))
			* plague.GetAreaMultiplierAtLevel(plague.CurrentLevel) * areaMultiplier;

	private float CurseSeconds() =>
		(CurseBaseSeconds + plague.GetEffectValueAtLevel(SpellEffect.ZoneDuration, plague.CurrentLevel)) * durationMultiplier;

	// The festering curse: the frozen plague's poison, and the old Shadow Bolt's slow and exposure as the
	// conjunction's own riders. Laying it deals nothing - the bolt's and the cave-ins' hits carry the
	// damage - and a body already cursed only has its curse refreshed (every rider is stacking-resistant).
	private void ApplyCurse(Enemy enemy)
	{
		if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
			return;
		float seconds = CurseSeconds();
		ulong id = enemy.GetInstanceId();
		if (cursedIds.Add(id))
		{
			if (curses.Count >= MaxCursed)
			{
				// Full: the oldest curse ends early. The cap is the intensity of the epidemic, not its total.
				cursedIds.Remove(curses[0].Id);
				curses.RemoveAt(0);
			}
			curses.Add(new Curse { Enemy = enemy, Id = id, LastSeen = enemy.GlobalPosition, Remaining = seconds });
		}
		else
		{
			for (int i = 0; i < curses.Count; i++)
			{
				if (curses[i].Id == id)
				{
					Curse curse = curses[i];
					curse.Remaining = MathF.Max(curse.Remaining, seconds);
					curses[i] = curse;
					break;
				}
			}
		}
		// The plague's dose, as Player.FireContagion builds it: its damage plus its Dot level lines.
		int poison = Math.Max(1, Mathf.RoundToInt(plague.GetDamageAtLevel(plague.CurrentLevel) * damageMultiplier
			+ plague.GetEffectValueAtLevel(SpellEffect.DotDamage, plague.CurrentLevel)));
		enemy.ApplyPoison(poison, seconds);
		enemy.ApplySlow(CurseSlowMultiplier, seconds);
		enemy.ApplyVulnerable(CurseVulnerableBonus, seconds);
	}

	// Dead carriers and lapsed curses leave; a live one's last seen spot follows it. One poll, on a slow
	// clock, as Contagion's - never per frame.
	private void Poll(float step)
	{
		for (int i = curses.Count - 1; i >= 0; i--)
		{
			Curse curse = curses[i];
			bool dead = curse.Enemy == null || !IsInstanceValid(curse.Enemy) || curse.Enemy.IsDying;
			curse.Remaining -= step;
			if (dead || curse.Remaining <= 0f)
			{
				cursedIds.Remove(curse.Id);
				curses.RemoveAt(i);
				continue;
			}
			curse.LastSeen = curse.Enemy.GlobalPosition;
			curses[i] = curse;
		}
	}

	// --- the beat -----------------------------------------------------------------------------------------

	// Pandemic's beat, made the lance's: every cursed body caves in at once. Each cave-in strikes its
	// carrier and everything in its reach - no enemy more than once a beat, however many cave-ins stand
	// round it - and passes the curse to the FesterSpread nearest bodies not yet carrying it.
	private void CaveInAll()
	{
		int carriers = curses.Count;
		if (carriers == 0 || lance == null)
			return;
		hitThisBeat.Clear();
		float reach = VoidLance.CollapseRadius * lance.GetAreaMultiplierAtLevel(lance.CurrentLevel)
			* areaMultiplier * CollapseReachShare;
		float reachSq = reach * reach;
		int hit = Math.Max(1, Mathf.RoundToInt(lance.GetDamageAtLevel(lance.CurrentLevel) * damageMultiplier * CollapseShare));

		// Only the bodies cursed when the beat lands cave in: what this beat festers onto goes off next beat.
		for (int i = 0; i < carriers && i < curses.Count; i++)
		{
			Enemy carrier = curses[i].Enemy;
			if (carrier == null || !IsInstanceValid(carrier) || carrier.IsDying)
				continue;
			Vector2 at = carrier.GlobalPosition;
			caveIns.Add(new CaveIn { At = at, Reach = reach, Age = 0f });

			if (hitThisBeat.Add(carrier.GetInstanceId()))
				PlayerRef.DealDamageToEnemy(carrier, hit, source: Spell, numberElement: Element.Darkness);

			spreadScratch.Clear();
			foreach (Enemy enemy in EnemyRegistry.Snapshot)
			{
				if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying || ReferenceEquals(enemy, carrier))
					continue;
				if (at.DistanceSquaredTo(enemy.GlobalPosition) > reachSq)
					continue;
				if (hitThisBeat.Add(enemy.GetInstanceId()))
					PlayerRef.DealDamageToEnemy(enemy, hit, source: Spell, numberElement: Element.Darkness);
				if (!cursedIds.Contains(enemy.GetInstanceId()))
					InsertNearest(spreadScratch, enemy, at);
			}
			for (int s = 0; s < spreadScratch.Count && s < FesterSpread; s++)
			{
				Enemy caught = spreadScratch[s];
				festerArcs.Add(new Arc { A = at, B = caught.GlobalPosition, Age = 0f, Seed = (uint)(caught.GetInstanceId() % 9973) });
				ApplyCurse(caught);
			}
		}
	}

	// Keeps the few nearest candidates, nearest first, without sorting the field.
	private void InsertNearest(List<Enemy> list, Enemy enemy, Vector2 at)
	{
		int keep = Math.Max(1, FesterSpread);
		float d = at.DistanceSquaredTo(enemy.GlobalPosition);
		int index = list.Count;
		for (int i = 0; i < list.Count; i++)
		{
			if (d < at.DistanceSquaredTo(list[i].GlobalPosition))
			{
				index = i;
				break;
			}
		}
		if (index >= keep)
			return;
		list.Insert(index, enemy);
		if (list.Count > keep)
			list.RemoveAt(list.Count - 1);
	}

	// How far into the beat the field is, 0..1: the rings swell over its last fifth, Pandemic's own tell.
	private float BeatPhase => (float)(beatClock % Contagion.PandemicSeconds / Contagion.PandemicSeconds);

	// --- the look -----------------------------------------------------------------------------------------

	// Each carrier's ring at its feet: the Darkness ramp on the world's 2 px grid, in an occlusion line so
	// it reads on any floor, with three motes of rot turning round it - the curse is the plague's, and the
	// ring the void's. Before each beat the rings swell together, the tell that the field is about to go.
	public override void _Draw()
	{
		if (curses.Count == 0)
			return;
		float swellPhase = BeatPhase;
		float swell = swellPhase > 0.8f ? 1f + 0.6f * (swellPhase - 0.8f) / 0.2f : 1f;
		float g = PixelCells.Grid;
		foreach (Curse curse in curses)
		{
			Vector2 c;
			float half = 9f;
			if (curse.Enemy != null && IsInstanceValid(curse.Enemy) && !curse.Enemy.IsDying)
			{
				StatusFxGeometry body = curse.Enemy.StatusFxGeometry;
				c = ToLocal(curse.Enemy.GlobalPosition) + new Vector2(0f, body.FeetY - 1f);
				half = MathF.Max(half, body.HalfWidth + 2f);
			}
			else
			{
				c = ToLocal(curse.LastSeen) + new Vector2(0f, 10f);
			}
			float rx = MathF.Round(half * swell / g) * g;
			float ry = MathF.Max(g * 2f, MathF.Round(rx * 0.45f / g) * g);
			int steps = Math.Max(14, (int)(Mathf.Tau * rx / g));
			for (int pass = 0; pass < 2; pass++)
			{
				for (int i = 0; i < steps; i++)
				{
					float a = i * Mathf.Tau / steps;
					Vector2 at = c + new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry);
					if (pass == 0)
						DrawRect(PixelCells.Snap(this, at, g * 2f), Occ);
					else
						DrawRect(PixelCells.Snap(this, at, g), MathF.Sin(a) < -0.3f ? Dark.Hot : MathF.Sin(a) > 0.3f ? Dark.Edge : Dark.Mid);
				}
			}
			// The rot: three motes of the plague turning round the ring, the one part of the mark that moves.
			for (int m = 0; m < 3; m++)
			{
				float a = clock * 1.9f + m * (Mathf.Tau / 3f);
				Vector2 at = c + new Vector2(MathF.Cos(a) * (rx + g), MathF.Sin(a) * (ry + g));
				DrawRect(PixelCells.Snap(this, at, g), m == 0 ? Rot.Hot : Rot.Mid);
			}
			// Swelling: a broken second ring of the void's hot tone just outside - the beat about to land.
			if (swell > 1.25f)
			{
				for (int i = 0; i < steps; i += 2)
				{
					float a = i * Mathf.Tau / steps;
					DrawRect(PixelCells.Snap(this, c + new Vector2(MathF.Cos(a) * (rx + g * 2f), MathF.Sin(a) * (ry + g)), g), Dark.Hot);
				}
			}
		}
	}

	// The bolt: a jagged stroke of void from body to body, a bright seam while it is fresh, dissolving as
	// it ages. Three segments a leap, kinked by a hash of the leap itself, never an allocation.
	private void DrawBoltArcs()
	{
		if (boltArcs.Count == 0)
			return;
		boltStrokes.Begin();
		foreach (Arc arc in boltArcs)
		{
			float t = arc.Age / ArcSeconds;
			// Arcs are kept in world space, as the curses are; the layer hangs at this node's origin.
			Vector2 a = boltLayer.ToLocal(arc.A);
			Vector2 b = boltLayer.ToLocal(arc.B);
			Vector2 span = b - a;
			float length = span.Length();
			if (length < 2f)
				continue;
			Vector2 normal = new Vector2(-span.Y, span.X) / length;
			// Two kinks, thrown either side of the line by the leap's own seed; the jag of the old chain.
			float k1 = (((arc.Seed * 73u + 17u) % 97u) / 97f - 0.5f) * 0.42f;
			float k2 = (((arc.Seed * 131u + 53u) % 89u) / 89f - 0.5f) * 0.42f;
			Vector2 p1 = a + span * 0.33f + normal * length * k1;
			Vector2 p2 = a + span * 0.66f + normal * length * k2;
			float half = t < 0.25f ? 2f : 1f;
			AddBoltStroke(a, p1, half, t);
			AddBoltStroke(p1, p2, half, t);
			AddBoltStroke(p2, b, half, t);
		}
		boltStrokes.Draw(boltLayer, Colors.White);
	}

	private void AddBoltStroke(Vector2 from, Vector2 to, float half, float t)
	{
		boltStrokes.Add(new SpellStrokes.Stroke
		{
			A = from, B = to, HalfA = half, HalfB = half,
			Glow = 2.5f * (1f - t), Density = 0.8f, Spine = t < 0.4f ? 0 : 1, GlowTone = 3,
			Dissolve = t < 0.5f ? 0f : (t - 0.5f) * 2f, Flash = t < 0.12f,
		});
	}

	// The fester: a thin rot stroke from a cave-in to each body it cursed.
	private void DrawFesterArcs()
	{
		if (festerArcs.Count == 0)
			return;
		festerStrokes.Begin();
		foreach (Arc arc in festerArcs)
		{
			float t = arc.Age / ArcSeconds;
			Vector2 a = festerLayer.ToLocal(arc.A);
			Vector2 b = festerLayer.ToLocal(arc.B);
			Vector2 span = b - a;
			float length = span.Length();
			if (length < 2f)
				continue;
			Vector2 normal = new Vector2(-span.Y, span.X) / length;
			float kink = (((arc.Seed * 59u + 29u) % 83u) / 83f - 0.5f) * 0.5f;
			Vector2 mid = a + span * 0.5f + normal * length * kink;
			festerStrokes.Add(new SpellStrokes.Stroke
			{
				A = a, B = mid, HalfA = 1f, HalfB = 1f, Glow = 1.6f * (1f - t), Density = 0.6f,
				Spine = 1, GlowTone = 2, Dissolve = t,
			});
			festerStrokes.Add(new SpellStrokes.Stroke
			{
				A = mid, B = b, HalfA = 1f, HalfB = 1f, Glow = 1.6f * (1f - t), Density = 0.6f,
				Spine = 1, GlowTone = 2, Dissolve = t,
			});
		}
		festerStrokes.Draw(festerLayer, Colors.White);
	}

	// A cave-in: a disc of void that swells to its reach and caves in on itself, the rim brightening to
	// the seam's white-hot as it closes - Cosmic Collapse's hole at body scale, in snapped rects.
	private void DrawCaveIns()
	{
		if (caveIns.Count == 0)
			return;
		float g = PixelCells.Grid;
		foreach (CaveIn hole in caveIns)
		{
			float t = hole.Age / CollapseSeconds;
			float radius = hole.Reach * (t < 0.3f ? t / 0.3f : 1f - (t - 0.3f) / 0.7f);
			if (radius < g)
				continue;
			Vector2 c = holeLayer.ToLocal(hole.At);
			bool closing = t > 0.65f;
			int steps = Math.Max(10, (int)(Mathf.Tau * radius / (g * 2f)));
			for (int i = 0; i < steps; i++)
			{
				float a = i * Mathf.Tau / steps;
				Vector2 rim = c + new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius * 0.8f);
				DrawRectOn(holeLayer, rim, g * 2f, Occ);
				DrawRectOn(holeLayer, rim, g, closing ? Seam.Hot : (MathF.Sin(a) < 0f ? Dark.Mid : Dark.Edge));
			}
			// The inside, falling in: flecks of the void pulled toward the centre as the hole closes.
			for (int m = 0; m < 6; m++)
			{
				float a = m * (Mathf.Tau / 6f) + t * 2.4f;
				float r = radius * (0.65f - 0.3f * t);
				if (r < g)
					continue;
				DrawRectOn(holeLayer, c + new Vector2(MathF.Cos(a) * r, MathF.Sin(a) * r * 0.8f), g, m % 2 == 0 ? Dark.Mid : Rot.Mid);
			}
		}
	}

	private static void DrawRectOn(CanvasItem canvas, Vector2 at, float size, Color color) =>
		canvas.DrawRect(PixelCells.Snap(canvas, at, size), color);

	// --- the fusion ---------------------------------------------------------------------------------------

	/// <summary>
	/// The numbers the fire loop and the cards read from the spell itself, set once at the fusion from the
	/// frozen parents (Player.TryFuse): the cooldown and the range are the lance's, the damage one leap's
	/// hit before the player's multipliers, the count the bodies one cast leaps through. Everything else is
	/// read from the parents as it fires.
	/// </summary>
	public static void Freeze(SpellData combination)
	{
		SpellData frozenLance = combination.ComboParent(LanceSpellId);
		if (frozenLance == null)
			return;
		int level = frozenLance.CurrentLevel;
		combination.LevelUpgrades = new Godot.Collections.Array<SpellLevelUpgrade>();
		combination.BaseCooldown = frozenLance.GetCooldownAtLevel(level);
		combination.BaseDamage = Math.Max(1, Mathf.RoundToInt(frozenLance.GetDamageAtLevel(level) * 0.75f));
		combination.BaseProjectileCount = DefaultBoltLeaps;
		combination.BaseRange = frozenLance.GetRangeAtLevel(level);
	}
}
