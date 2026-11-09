using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// THE SOVEREIGN RITE - Arcane Explosion + Gravewell, the capstone Conjunction (.ai/sovereign-spell.md section 9;
// ComboCatalog; SovereignRite holds the gate and the fuel). Eric, 2026-11-08: "the soverign rite should be a
// combination spell itself - pick two spells that make sense to combine to create this".
//
// Made from Arcane Explosion at level 8 down Resonance and Gravewell at level 8 down Mass Grave, and from nothing
// else: both parents ride on the spell frozen as they were at the fusion (SpellData.ComboParents), and every number
// here is built from theirs. The verb is ANOINT: making it silences every other active spell for the rest of the
// run, and their levels - fuel - pour into it through the boon the Rite and both frozen parents share
// (SpellData.SovereignBoon, SovereignRite.GainsFor: damage, cooldown, area, and one more blast a cast per 20).
//
//   aim      the thickest pack in reach, and with more than one blast a cast the next thickest that is not
//            under the first - the front of the swarm, where Mass Grave digs.
//   cadence  the frozen Gravewell's own cooldown: one great grave a cast, as Mass Grave was.
//   demand   stand where packs form. A crowned blast on a straggler opens a grave nothing walks into.
//   payload  a crowned blast (the frozen Arcane Explosion's hit, every body within its reach) falls on the pack and
//            throws nothing; a great grave opens under it already armed and caves in on what the blast struck,
//            holding it and draining it (Mass Grave, on the frozen Gravewell's numbers); and as the grave springs
//            the blast goes off again inside it - Resonance's echo, at its share, made the grave's.
//
// NO PULL: the gather is Hollow Star's verb (SpellEvolutionCatalog's Gravewell note). The blast throws nothing,
// so the crowd stays on the grave it is about to fall into, and the grave's own tug - a third of the way - is all
// the drawing in there is.
//
// BOUNDED: at most MaxBlastsPerCast blasts a cast; each strikes every body in its reach once; each grave echoes
// once, when it springs; the graves are GravewellTrap's own and live under its cap (MaxWaiting). The cast walks
// EnemyRegistry.Snapshot once into a reused list; per-frame work is the echo watch over a handful of graves and
// the blast fronts' progress, and nothing here allocates per frame.
//
// THE LOOK (no new sheet): the blast is Arcane Explosion's own front (scenes/shaders/arcane_blast.gdshader) with
// its gilded ramp - arcane white running into gold, the crown's colours - and the echo is the same front run the
// other way, collapsing into a dark heart, which is the blast going down into the grave. The grave is Gravewell's
// great grave (tools/art/gravewell.py), crowned by the Rite's corona (Player.ApplySpellVisual, SovereignCorona).
public partial class SovereignRiteCaster : Node2D
{
	public const string SpellId = SovereignRite.SpellId;
	/// <summary>The first parent: the crowned blast (down Resonance).</summary>
	public const string BlastSpellId = "arcane_explosion";
	/// <summary>The second parent: the great grave (down Mass Grave).</summary>
	public const string GraveSpellId = "gravewell";

	// --- the Conjunction's own multipliers (tuned in scenes/SovereignRite.tscn) -------------------------------

	/// <summary>Each crowned blast deals this share of the frozen Arcane Explosion's hit to every body in its reach.</summary>
	[Export] public float BlastShare { get; set; } = DefaultBlastShare;

	/// <summary>
	/// BlastShare's default, which Freeze also writes onto the card as the stand-in damage. 1.8, as measured
	/// (.ai/sovereign-spell.md 9.5): at 1 the Rite alone dealt x0.68-1.04 of the loadout it silenced, at 2 x1.16-1.66,
	/// against a 1.3 target - one spell's parents' numbers standing in for six spells' worth.
	/// </summary>
	public const float DefaultBlastShare = 1.8f;

	/// <summary>The echo in the grave as it springs: this share of the blast's hit (Resonance's ResonanceShare).</summary>
	[Export] public float EchoShare { get; set; } = ArcaneExplosion.ResonanceShare;

	/// <summary>The grave's collapse and drain: this share of the frozen Gravewell's. 1.8, as BlastShare.</summary>
	[Export] public float GraveShare { get; set; } = 1.8f;

	/// <summary>The most blasts one cast lets fall, whatever the fuel and the player's Amount.</summary>
	public const int MaxBlastsPerCast = 4;

	// The blast's front lasts as long as Arcane Explosion's (ArcaneExplosion.BlastSeconds).
	private const float FrontSeconds = 0.4f;
	// Fronts in the air at once: blasts and echoes, one slot each, reused round.
	private const int MaxFronts = 8;
	// Two blasts of one cast keep at least one blast radius apart, so the second falls on another pack.
	private const float BlastSpacing = 1f;
	// A pack is counted within the great grave's bank (Gravewell's trip radius, Mass Grave's width): the blast
	// reaches far wider, and counted at its reach every body of a crowd ties and the nearest edge of it wins.
	private const float PackRadius = GravewellTrap.DefaultTriggerRadius * GravewellTrap.MassBankScale;
	private const string BlastShaderPath = "res://scenes/shaders/arcane_blast.gdshader";

	public Player PlayerRef { get; set; }

	/// <summary>The Rite, which every hit is credited to.</summary>
	public SpellData Spell { get; private set; }

	private SpellData blast;
	private SpellData grave;
	private PackedScene graveScene;
	private float playerArea = 1f;
	private float playerDamage = 1f;
	private float durationMultiplier = 1f;

	// The graves the Rite opened that have not sprung yet: each echoes the blast once, as it springs.
	private struct Grave
	{
		public GravewellTrap Trap;
		public int Echo;
		public float Radius;
	}

	private readonly List<Grave> graves = new();
	private readonly List<Enemy> scratch = new(160);
	private readonly Vector2[] centres = new Vector2[MaxBlastsPerCast];

	private static Shader blastShader;
	private static readonly StringName ParamRadius = "radius";
	private static readonly StringName ParamProgress = "progress";
	private static readonly StringName ParamSeed = "seed";
	private static readonly StringName ParamGilded = "gilded";
	private static readonly StringName ParamEventHorizon = "event_horizon";
	private readonly Node2D[] fronts = new Node2D[MaxFronts];
	private readonly ShaderMaterial[] frontMaterials = new ShaderMaterial[MaxFronts];
	private readonly float[] frontAges = { -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f };
	private readonly float[] frontRadii = new float[MaxFronts];
	private int nextFront;
	private int frontCount;

	public override void _Ready()
	{
		// In the air over the bodies it strikes: the fronts are its only drawing (DrawLayers).
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		blastShader ??= GD.Load<Shader>(BlastShaderPath);
		if (blastShader == null)
			GD.PushError($"SovereignRiteCaster: {BlastShaderPath} failed to load.");
	}

	/// <summary>
	/// The fire loop's cast (Player.FireSovereignRite): up to <paramref name="count"/> crowned blasts on the thickest
	/// packs in <paramref name="castRange"/>, each opening a great grave. False with nothing in reach.
	/// </summary>
	public bool Cast(SpellData rite, SpellData frozenBlast, SpellData frozenGrave, PackedScene graveTrapScene, int count,
		float castRange, float area, float damage, float duration)
	{
		if (rite == null || frozenBlast == null || frozenGrave == null || PlayerRef == null || !IsInstanceValid(PlayerRef))
			return false;
		Spell = rite;
		blast = frozenBlast;
		grave = frozenGrave;
		graveScene = graveTrapScene;
		playerArea = area;
		playerDamage = damage;
		durationMultiplier = duration;

		float radius = BlastRadius();
		int found = PickCentres(castRange, radius, Math.Clamp(count, 1, MaxBlastsPerCast));
		if (found == 0)
			return false;
		int hit = Math.Max(1, Mathf.RoundToInt(blast.GetDamageAtLevel(blast.CurrentLevel) * playerDamage * BlastShare));
		int echo = Math.Max(1, Mathf.RoundToInt(hit * EchoShare));
		for (int i = 0; i < found; i++)
		{
			BlastAt(centres[i], radius, hit);
			ShowFront(centres[i], radius, echo: false);
			OpenGrave(centres[i], echo, radius);
		}
		return true;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef) || graves.Count == 0)
			return;
		// A boss is arriving: the field is held, and the echoes wait with it (Player.HeldForBossEntrance).
		if (PlayerRef.HeldForBossEntrance)
			return;
		// Resonance, made the grave's: the blast goes off again inside each grave the moment it springs.
		for (int i = graves.Count - 1; i >= 0; i--)
		{
			Grave g = graves[i];
			if (g.Trap == null || !IsInstanceValid(g.Trap) || g.Trap.IsQueuedForDeletion())
			{
				graves.RemoveAt(i);
				continue;
			}
			if (!g.Trap.HasSprung)
				continue;
			Vector2 at = g.Trap.GlobalPosition;
			BlastAt(at, g.Radius, g.Echo);
			ShowFront(at, g.Radius, echo: true);
			graves.RemoveAt(i);
		}
	}

	public override void _Process(double delta)
	{
		if (frontCount == 0)
			return;
		float dt = (float)delta;
		int live = 0;
		for (int i = 0; i < MaxFronts; i++)
		{
			if (fronts[i] == null || frontAges[i] < 0f)
				continue;
			frontAges[i] += dt;
			if (frontAges[i] >= FrontSeconds)
				frontAges[i] = -1f;
			else
			{
				frontMaterials[i].SetShaderParameter(ParamProgress, frontAges[i] / FrontSeconds);
				live++;
			}
			fronts[i].QueueRedraw();
		}
		frontCount = live;
	}

	// The blast's reach: Arcane Explosion's own, as ArcaneExplosion.RefreshComputedStats measures it - its range,
	// the player's Area, the frozen spell's area (the fuel's share among it) and its AreaSize lines.
	private float BlastRadius() =>
		blast.GetRangeAtLevel(blast.CurrentLevel) * playerArea * blast.GetAreaMultiplierAtLevel(blast.CurrentLevel)
			* (1f + blast.GetEffectValueAtLevel(SpellEffect.AreaSize, blast.CurrentLevel));

	// The thickest packs in reach: the enemy with the most others within a grave's bank of it (PackRadius) first,
	// then the next thickest at least BlastSpacing blasts from every centre already chosen. Ties go to the nearer,
	// so a lone straggler is still a target when nothing stands together. One walk of the field into a reused list.
	private int PickCentres(float range, float radius, int count)
	{
		Vector2 from = PlayerRef.GlobalPosition;
		float rangeSq = range * range;
		scratch.Clear();
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (enemy != null && IsInstanceValid(enemy) && !enemy.IsDying && from.DistanceSquaredTo(enemy.GlobalPosition) <= rangeSq)
				scratch.Add(enemy);
		}
		if (scratch.Count == 0)
			return 0;
		float packSq = PackRadius * PackRadius;
		float apartSq = radius * BlastSpacing * radius * BlastSpacing;
		int found = 0;
		while (found < count)
		{
			Enemy best = null;
			int bestCount = -1;
			float bestDist = float.MaxValue;
			foreach (Enemy candidate in scratch)
			{
				Vector2 at = candidate.GlobalPosition;
				bool taken = false;
				for (int c = 0; c < found && !taken; c++)
					taken = at.DistanceSquaredTo(centres[c]) < apartSq;
				if (taken)
					continue;
				int near = 0;
				foreach (Enemy other in scratch)
				{
					if (!ReferenceEquals(candidate, other) && at.DistanceSquaredTo(other.GlobalPosition) <= packSq)
						near++;
				}
				float d = from.DistanceSquaredTo(at);
				if (near > bestCount || (near == bestCount && d < bestDist))
				{
					best = candidate;
					bestCount = near;
					bestDist = d;
				}
			}
			if (best == null)
				break;
			centres[found++] = best.GlobalPosition;
		}
		scratch.Clear();
		return found;
	}

	// One blast's hits: every body within its reach, once, credited to the Rite. It throws nothing (no pull is
	// ours; no push would leave the grave empty), so no knockback rides on it.
	private void BlastAt(Vector2 at, float radius, int amount)
	{
		float r2 = radius * radius;
		foreach (Enemy e in EnemyRegistry.Snapshot)
		{
			if (e == null || !IsInstanceValid(e) || e.IsDying || at.DistanceSquaredTo(e.GlobalPosition) > r2)
				continue;
			PlayerRef.DealDamageToEnemy(e, amount, source: Spell, numberElement: Element.Arcane);
		}
	}

	// The great grave under the blast: Gravewell's own trap on the frozen Gravewell's numbers (Mass Grave's width,
	// hold and drain), opened already armed - it opens under what the blast just struck - and credited to the Rite.
	// Never on blocked ground (.ai/terrain.md): the blast still falls there, with no grave under it.
	private void OpenGrave(Vector2 at, int echo, float radius)
	{
		if (graveScene == null || PlayerRef.GetParent() is not Node2D arena)
			return;
		if (MazeNavigation.Active?.IsBlockedAt(at) == true)
			return;
		GravewellTrap.Setup setup = GravewellTrap.For(grave, playerArea, playerDamage * GraveShare, durationMultiplier);
		setup.Quickset = true;
		GravewellTrap.MakeRoom(1);
		var trap = graveScene.Instantiate<GravewellTrap>();
		trap.Configure(Spell, PlayerRef, setup);
		arena.AddChild(Player.TagSpell(trap));
		trap.GlobalPosition = at;
		// Crowned (the Rite's corona) and a Legendary's gold, as every cast's node is; the grave draws its own
		// bank and blast, so it keeps its scale and its own colours (Player.OpenGraves' rule).
		PlayerRef.ApplySpellVisual(trap, Spell);
		trap.Scale = Vector2.One;
		if (!Spell.IsLegendary)
			trap.Modulate = Colors.White;
		trap.Opened();
		graves.Add(new Grave { Trap = trap, Echo = echo, Radius = radius });
	}

	// A front: the blast's own shader on a node of its own in the air, gilded going out, collapsing into a dark
	// heart coming back (the echo going down into the grave).
	private void ShowFront(Vector2 at, float radius, bool echo)
	{
		if (blastShader == null)
			return;
		int i = nextFront;
		nextFront = (nextFront + 1) % MaxFronts;
		if (fronts[i] == null || !IsInstanceValid(fronts[i]))
		{
			frontMaterials[i] = new ShaderMaterial { Shader = blastShader };
			var node = new Node2D { Name = $"RiteFront{i}", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Air, Material = frontMaterials[i] };
			int index = i;
			node.Draw += () => DrawFront(index);
			AddChild(node);
			fronts[i] = node;
		}
		fronts[i].GlobalPosition = at;
		frontAges[i] = 0f;
		frontRadii[i] = radius;
		frontCount++;
		ShaderMaterial m = frontMaterials[i];
		m.SetShaderParameter(ParamRadius, radius);
		m.SetShaderParameter(ParamProgress, 0f);
		m.SetShaderParameter(ParamSeed, (at.X * 0.6180339f + at.Y * 0.381966f) % 97f);
		m.SetShaderParameter(ParamGilded, !echo);
		m.SetShaderParameter(ParamEventHorizon, echo);
		fronts[i].QueueRedraw();
	}

	private void DrawFront(int i)
	{
		if (frontAges[i] < 0f || frontAges[i] >= FrontSeconds)
			return;
		float half = frontRadii[i] + 4f;
		fronts[i].DrawRect(new Rect2(-half, -half, half * 2f, half * 2f), Colors.White);
	}

	// --- the fusion -----------------------------------------------------------------------------------------

	/// <summary>
	/// The numbers the fire loop and the cards read from the spell itself, set once at the fusion from the frozen
	/// parents (Player.TryFuse), before the fuel's boon is put on: the cooldown and the reach are the grave's (one
	/// great grave a cast, looked for as far as Gravewell looked), the damage one blast's hit before the player's
	/// multipliers, the count one blast a cast. Everything else is read from the parents as it fires.
	/// </summary>
	public static void Freeze(SpellData rite)
	{
		SpellData frozenBlast = rite.ComboParent(BlastSpellId);
		SpellData frozenGrave = rite.ComboParent(GraveSpellId);
		if (frozenBlast == null || frozenGrave == null)
			return;
		rite.LevelUpgrades = new Godot.Collections.Array<SpellLevelUpgrade>();
		rite.BaseCooldown = frozenGrave.GetCooldownAtLevel(frozenGrave.CurrentLevel);
		rite.BaseDamage = Math.Max(1, Mathf.RoundToInt(frozenBlast.GetDamageAtLevel(frozenBlast.CurrentLevel) * DefaultBlastShare));
		rite.BaseProjectileCount = 1;
		rite.BaseRange = frozenGrave.GetRangeAtLevel(frozenGrave.CurrentLevel);
	}
}
