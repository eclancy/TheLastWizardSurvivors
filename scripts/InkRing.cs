using Godot;
using System;

namespace TheOneHeMissed.scripts;

// INK RING (Water + Darkness), built 2026-11-09. Eric approved it from .ai/character-fit.md's queue: a thin line of ink
// trails the walker; crossing it closes the loop and strikes everything inside, then slows it a moment; a short recharge
// between rings; the ink fades fast at level 1 (small rings) and lasts longer as it levels. Ma Liang's opener since
// 2026-11-10 (Eric: "would it make sense for ink ring to be his starting spell instead?" - "Yes, add it"), earned by
// everyone else only as him (the Cormorant feat, in-flight 84); his Apotheosis, The Painted Door, is its final form.
//
// IT FIRES WHEN A LOOP CLOSES, which no spell did before: no timer puts anything into the world. The cooldown is the
// RECHARGE between rings, and it lives in Player's one spellFireTimers loop like every other spell's (Player.InkRing.cs):
// while the cooldown is ripe the fire loop ARMS this node every frame (TakeClosed) and is told NoTarget, so the timer
// stays ripe - Hunter's Draw's pattern - until the wizard's path crosses its own line. Then this node strikes, and the
// next call reports Fired: the cast cue, the cast tally and the Canticle's charge, and the recharge starts. While the
// fire loop is silent (recharging, a boss arriving, the dark wizard's seal) a crossing closes nothing; the line still
// trails, so the next ring can be drawn while the last recharges.
//
// THE LINE. The wizard's feet (Player.FootPosition) sampled every SampleSpacing world units into a ring buffer
// (InkPath), each point dated; a point older than the ink's life is gone. Each new segment is tested against the older
// live segments, skipping the newest few it touches; a crossing whose loop - the crossing, then the points walked since -
// holds at least MinRingArea closes a ring. Everything before the crossing is then inert: a line closes one ring once.
// A jump (a door, a teleport) starts a new stroke rather than drawing a line across the gap.
//
// THE STRIKE. Every EnemyRegistry.Snapshot body whose ground point lies inside the loop (InkPath.Contains, crossing
// number) is struck once for the ring's damage and slowed SlowShare for SlowSeconds. One walk of the swarm a ring, and
// one a tick for Wet Ink's sting and Inkwell's pool, each position read once. Nothing is allocated per frame: the line,
// the rings, the floods, the splats and the prison walls are fixed pools, and a stung body is remembered by reference.
//
// SPELLS START SMALL (Eric, 2026-11-02): at level 1 the ink lasts BaseInkSeconds, so only a small ring closes before its
// start has dried away (about 175 units across at the wizard's walk); each ZoneDuration line adds to it, so the rings grow
// with the spell (SpellData_InkRing.tres). Long Stroke and the wizard's own Duration lengthen it again.
//
// THE DRAWING IS INK, the one sanctioned departure from Bonelight (.ai/art-direction.md section 3a). No colour is picked
// here: every stamp is tools/art/ink_ring.py's sheet, painted by tools/art/ink_wash.py, drawn at x2 with plain blending
// (no SpellInk dither, which would break a wash into specks) and never tinted - the ink is the creature, and it is the
// same black whatever element it is branched toward. The line is dabs laid one per point, each the age of its point: wet
// from the brush near his feet, drying to hard tide lines, thinning to a pale wash and a ghost at its tail. A closing
// ring re-wets its own line and FLOODS INWARD - blots of ink appearing from the rim to the middle and drying back to a
// wash - and a splat lands under every body it struck, a splash of ink on it in the air for a moment. All of it on
// DrawLayers.Ground (under the bodies) but the splash, on DrawLayers.Air. Every stamp boils: its wet margins swap
// between two paintings of the same stroke every BoilSeconds.
//
// THE MILESTONES (SpellEvolutionCatalog.SetupInkRingEvolutions):
//   Long Stroke   (level 4) - the ink lasts LongStrokeShare as long, so wider rings close.
//   Wet Ink       (level 4, +1 Water) - the line stays wet and stings whatever touches it for WetShare of a strike, a
//                 body at most every WetEverySeconds.
//   Inkwell       (level 4) - the ring's flood stands as a pool for PoolSeconds, slowing whatever wades it PoolSlowShare.
//   Ink Prison    (level 8, +1 Darkness) - the ring dries into a wall for PrisonSeconds: a StaticBody2D on the
//                 enemy-only layer 7 (LevelTilePainter.EnemyOnlyCollisionLayer, Ma Liang's inked door lines' layer), so
//                 what it caught cannot get out and the wizard walks through it. A boss is never penned (the crowd-
//                 control rule, Enemy.CapsCrowdControl): each prison wall takes a collision exception for every boss.
//   Second Stroke (level 8) - SecondStrokeDelay after it closes, the ring re-wets, floods again and strikes again.
//   The Painted Door (milestone 9, Ma Liang's Apotheosis, displace) - a closed ring paints a door inside it, and what
//                 it holds steps out of a second door far across the field (InkRing.PaintedDoor.cs).
public partial class InkRing : Node2D
{
	public const string SpellId = "ink_ring";

	// Milestone ids. SpellEvolutionCatalog builds the cards from these; this node reads them.
	public const string LongStrokeId = "ink_ring_long_stroke";
	public const string WetInkId = "ink_ring_wet_ink";
	public const string InkwellId = "ink_ring_inkwell";
	public const string InkPrisonId = "ink_ring_ink_prison";
	public const string SecondStrokeId = "ink_ring_second_stroke";

	public const string TexturePath = "res://assets/bonelight/effects/ink-ring.png";

	// --- the line ------------------------------------------------------------------------------------
	/// <summary>
	/// Seconds the ink lasts at level 1, before the levels' ZoneDuration lines and the wizard's own Duration. 2.5 since
	/// 2026-11-10, when Ink Ring became Ma Liang's opener (in-flight 94): at 2.0 a loop had to be walked in under 2 s, the
	/// smallest a bot can walk round (_BalanceHarness --character=22 --immortal --circle=60 --xp-first=100000, Ink Ring
	/// alone) took 1.7-2.1 s, and once the crowd closed in and his laps slowed past the ink's life no ring closed again -
	/// 9-11 rings and then none, 6-9 kills and 450-464 damage taken in the first minute on three seeds. At 2.5 the rings
	/// kept closing: 50-54 kills and 141-220 taken in the first minute, 124-129 kills and 563-607 in the second, against
	/// the crow Familiar he opened on before at 67 and 25, then 103 and 614. The validator's "starts small" ceiling.
	/// </summary>
	public const float BaseInkSeconds = 2.5f;
	/// <summary>World units between two points of the line.</summary>
	public const float SampleSpacing = 12f;
	/// <summary>The least area a loop must hold to close: a step back and forth over the line closes nothing.</summary>
	public const float MinRingArea = 2400f;
	private const int MaxPoints = 640;
	// A step longer than this is a jump (a door, a teleport): the line starts a new stroke.
	private const float BreakDistance = 160f;
	private const int MaxPointsPerFrame = 24;

	// --- the strike -----------------------------------------------------------------------------------
	/// <summary>How much a ring slows what it struck, and for how long.</summary>
	public const float SlowShare = 0.4f;
	public const float SlowSeconds = 1.2f;
	// A body's ground point: its middle, dropped toward its feet, where the ring is drawn round it.
	private const float GroundDrop = 12f;

	// --- Long Stroke ----------------------------------------------------------------------------------
	/// <summary>The ink's life under Long Stroke, as a multiple of the level's. The card says it; RegressionChecks.InkRing holds the two.</summary>
	public const float LongStrokeShare = 1.5f;

	// --- Wet Ink --------------------------------------------------------------------------------------
	/// <summary>Wet Ink's sting, of a ring's strike.</summary>
	public const float WetShare = 0.25f;
	/// <summary>The least time between two stings of one body: crossing the line stings once.</summary>
	public const float WetEverySeconds = 0.6f;
	private const float WetTickSeconds = 0.1f;
	private const float WetReach = 8f;
	private const int WetMemory = 96;

	// --- Inkwell --------------------------------------------------------------------------------------
	public const float PoolSeconds = 3f;
	/// <summary>How much the standing pool slows what wades it.</summary>
	public const float PoolSlowShare = 0.5f;
	private const float PoolTickSeconds = 0.2f;

	// --- Ink Prison -----------------------------------------------------------------------------------
	public const float PrisonSeconds = 3f;
	/// <summary>The collision bit the prison wall stands on: layer 7, which every enemy masks and the wizard never does.</summary>
	public static uint PrisonLayerBit => 1u << (LevelTilePainter.EnemyOnlyCollisionLayer - 1);
	private const int PrisonSegments = 48;

	// --- Second Stroke --------------------------------------------------------------------------------
	public const float SecondStrokeDelay = 0.5f;

	// --- the rings ------------------------------------------------------------------------------------
	private const int MaxRings = 4;
	private const int MaxRingPoints = 256;
	private const int MaxFlood = 120;
	// Wide enough that the blots only partly merge: packed tighter, the flood read as one flat grey disc on the floor.
	private const float FloodSpacing = 38f;
	private const float FloodInSeconds = 0.28f;
	private const int MaxSplats = 160;
	private const int MaxSplashes = 64;
	private const float SplashSeconds = 0.35f;
	private const float BoilSeconds = 0.25f;
	private const float RefreshSeconds = 0.25f;

	// --- the sheet (tools/art/ink_ring.py's layout, in art pixels; drawn at x2) -------------------------
	private const int LineCell = 16;
	private const int Headings = 8;
	private const int Windows = 3;
	private const int Boils = 2;
	private const int FloodCell = 24;
	private const int FloodY = 80;
	private const int SplatCell = 16;
	private const int SplatX = 96;
	private const int SplatY = 80;
	private const int SplatShapes = 4;

	// When each stage ends, in seconds from its start (the line's are shares of its life).
	private static readonly float[] LineAges = { 0.15f, 0.35f, 0.65f, 0.85f, 1f };
	private static readonly float[] WetLineAges = { 0.5f, 0.62f, 0.75f, 0.9f, 1f };
	private static readonly float[] RingLineAges = { 0.15f, 0.4f, 0.9f, 1.2f, 1.5f };
	// The flood is a moment: wet, a backrun, a short dried wash and its ghost. Held longer, its dried blots read as a
	// field of pebbles; only Inkwell holds that stage, for its pool.
	private static readonly float[] FloodAges = { 0.12f, 0.35f, 0.55f, 0.75f };
	private static readonly float[] SplatAges = { 0.25f, 0.8f, 1.2f };

	private struct Ring
	{
		public int Count;
		public Vector2 Lo, Hi, Centre;
		public float Area;
		public float LineAt;
		public float FloodAt;
		public float SecondAt;
		public float PoolUntil;
		public float PrisonUntil;
		public float HoldUntil;
		public int FloodCount;
		public int Damage;
		public uint Seed;
		public bool Prisoned;
		public bool Live;
	}

	private struct Splat { public Vector2 At; public float Born; public uint Seed; public bool Live; }

	private struct Splash { public Enemy Body; public Vector2 At; public float Born; public uint Seed; public bool Live; }

	public Player PlayerRef { get; set; }
	public SpellData SpellData { get; private set; }

	// For the harness (_BalanceHarness's INK RING line). Process-wide, as Corposant's are.
	public static int StatRings { get; set; }
	public static int StatStruck { get; set; }
	public static int StatTooSmall { get; set; }
	public static int StatStings { get; set; }
	public static int StatPoolSlows { get; set; }
	public static int StatPrisons { get; set; }
	public static int StatSecondStrokes { get; set; }
	/// <summary>The largest loop closed this process, in square world units: how big the rings the bot draws are.</summary>
	public static float StatLargestArea { get; set; }

	private static Texture2D sheet;

	private readonly InkPath path = new(MaxPoints);
	private readonly Vector2[] loopScratch = new Vector2[MaxRingPoints];
	private readonly Ring[] rings = new Ring[MaxRings];
	private readonly Vector2[] ringPts = new Vector2[MaxRings * MaxRingPoints];
	private readonly byte[] ringHeading = new byte[MaxRings * MaxRingPoints];
	private readonly Vector2[] floodPts = new Vector2[MaxRings * MaxFlood];
	private readonly float[] floodDelay = new float[MaxRings * MaxFlood];
	private readonly Splat[] splats = new Splat[MaxSplats];
	private readonly Splash[] splashes = new Splash[MaxSplashes];
	private readonly Enemy[] stung = new Enemy[WetMemory];
	private readonly float[] stungAt = new float[WetMemory];
	private readonly StaticBody2D[] prisonBodies = new StaticBody2D[MaxRings];
	private readonly SegmentShape2D[] prisonSegments = new SegmentShape2D[MaxRings * PrisonSegments];

	private Node2D air;
	private int damage = 14;
	private float inkLife = BaseInkSeconds;
	private bool longStroke, wetForm, poolForm, prisonForm, secondForm;
	private float clock;
	private float refreshedAt = float.NegativeInfinity;
	private int refreshedLevel = -1;
	private string refreshedLevel4, refreshedLevel8, refreshedForm;
	private float wetClock, poolClock;
	private int nextRing, nextSplat, nextSplash, stungNext;
	private uint seedClock;
	private ulong armedFrame = ulong.MaxValue;
	private bool closedPending;
	private bool drewLastFrame, drewAirLastFrame;

	public override void _Ready()
	{
		// The floor, under every body (DrawLayers): the line and the flood are ink lying on the ground.
		ZAsRelative = false;
		ZIndex = DrawLayers.Ground;
		// Plain blending: a wash is translucent, and SpellInk's dither would read its alpha as coverage.
		Material = null;
		TopLevel = true;
		GlobalPosition = Vector2.Zero;
		air = new Node2D { Name = "InkSplash", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Air };
		air.Draw += DrawAir;
		AddChild(air);
		air.GlobalPosition = Vector2.Zero;
		for (int i = 0; i < stungAt.Length; i++)
			stungAt[i] = float.NegativeInfinity;
		sheet ??= GD.Load<Texture2D>(TexturePath);
	}

	public override void _ExitTree()
	{
		if (air != null && IsInstanceValid(air))
			air.Draw -= DrawAir;
	}

	/// <summary>True when the numbers should be read again: a new level, a milestone taken, or RefreshSeconds gone by.</summary>
	public bool RefreshDue(SpellData spell) =>
		spell != SpellData || spell.CurrentLevel != refreshedLevel || clock - refreshedAt >= RefreshSeconds
		|| !ReferenceEquals(spell.SelectedLevel4EvolutionId, refreshedLevel4)
		|| !ReferenceEquals(spell.SelectedLevel8EvolutionId, refreshedLevel8)
		|| !ReferenceEquals(spell.SelectedApotheosisId, refreshedForm);

	/// <summary>The ring's numbers: its strike, the ink's life and the milestones. Rings already closed keep their own.</summary>
	public void Refresh(SpellData spell, float damageMultiplier, float playerDuration)
	{
		if (spell == null)
			return;
		SpellData = spell;
		int level = Math.Max(1, spell.CurrentLevel);
		damage = Math.Max(1, Mathf.RoundToInt(spell.GetDamageAtLevel(level) * damageMultiplier));
		longStroke = spell.HasEvolution(LongStrokeId);
		wetForm = spell.HasEvolution(WetInkId);
		poolForm = spell.HasEvolution(InkwellId);
		prisonForm = spell.HasEvolution(InkPrisonId);
		secondForm = spell.HasEvolution(SecondStrokeId);
		SetPaintedDoor(spell);
		inkLife = InkSecondsAt(spell, level) * (longStroke ? LongStrokeShare : 1f) * MathF.Max(0.25f, playerDuration);
		refreshedAt = clock;
		refreshedLevel = spell.CurrentLevel;
		refreshedLevel4 = spell.SelectedLevel4EvolutionId;
		refreshedLevel8 = spell.SelectedLevel8EvolutionId;
		refreshedForm = spell.SelectedApotheosisId;
	}

	/// <summary>How long the ink of this spell lasts at <paramref name="level"/>, before Long Stroke and the wizard's own Duration.</summary>
	public static float InkSecondsAt(SpellData spell, int level) =>
		BaseInkSeconds + (spell?.GetEffectValueAtLevel(SpellEffect.ZoneDuration, level) ?? 0f);

	/// <summary>The ink's life now, everything counted (for the harness and the checks).</summary>
	public float InkLife => inkLife;

	/// <summary>
	/// The fire loop's question, asked every frame its cooldown is ripe: did a ring close since it last asked? Asking
	/// ARMS the node for this frame and the next - only an armed node closes a ring - and a yes disarms it, so one ring
	/// is one cast and the recharge that follows it.
	/// </summary>
	public bool TakeClosed()
	{
		if (closedPending)
		{
			closedPending = false;
			armedFrame = ulong.MaxValue;
			return true;
		}
		armedFrame = Engine.GetPhysicsFrames();
		return false;
	}

	private bool Armed => armedFrame != ulong.MaxValue && Engine.GetPhysicsFrames() - armedFrame <= 1;

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (dt <= 0f)
			return;
		clock += dt;
		bool alive = PlayerRef != null && IsInstanceValid(PlayerRef) && !PlayerRef.IsDead;
		path.Expire(clock, inkLife);
		// What silences the fire loop stops the brush too (Player.InkRingMayCast): a line that could never close would
		// only mislead. What is already on the ground dries away as it would.
		if (alive && SpellData != null && PlayerRef.InkRingMayCast(SpellData))
			Lay(PlayerRef.FootPosition);
		TickRings();
		TickDoors(dt);

		bool wetTick = false, poolTick = false;
		if (wetForm && path.Count > 0)
		{
			wetClock += dt;
			if (wetClock >= WetTickSeconds)
			{
				wetClock -= WetTickSeconds;
				wetTick = true;
			}
		}
		if (AnyPool())
		{
			poolClock += dt;
			if (poolClock >= PoolTickSeconds)
			{
				poolClock -= PoolTickSeconds;
				poolTick = true;
			}
		}
		if (alive && (wetTick || poolTick))
			Sweep(wetTick, poolTick);

		bool any = path.Count > 0 || AnyRing() || AnySplat();
		if (any || drewLastFrame)
			QueueRedraw();
		drewLastFrame = any;
		bool splashing = AnySplash();
		if (splashing || drewAirLastFrame)
			air.QueueRedraw();
		drewAirLastFrame = splashing;
	}

	// --- the line ---------------------------------------------------------------------------------------

	private void Lay(Vector2 foot)
	{
		if (path.Count == 0)
		{
			path.Add(foot, clock, NextSeed());
			return;
		}
		Vector2 last = path.Newest;
		Vector2 to = foot - last;
		float distance = to.Length();
		if (distance > BreakDistance)
		{
			// A jump: a new stroke where he landed. The old one dries where it lies and closes nothing.
			path.Break();
			path.Add(foot, clock, NextSeed());
			return;
		}
		if (distance < SampleSpacing)
			return;
		Vector2 dir = to / distance;
		int laid = 0;
		while (distance >= SampleSpacing && laid < MaxPointsPerFrame)
		{
			last += dir * SampleSpacing;
			distance -= SampleSpacing;
			path.Add(last, clock, NextSeed());
			laid++;
			if (Armed)
				TryClose();
		}
	}

	private void TryClose()
	{
		int count = path.TryClose(loopScratch, MinRingArea, out float area, out bool tooSmall);
		if (tooSmall)
			StatTooSmall++;
		if (count < 3)
			return;
		Close(count, area);
	}

	// --- the ring ---------------------------------------------------------------------------------------

	private void Close(int count, float area)
	{
		int slot = FreeRing();
		int off = slot * MaxRingPoints;
		Array.Copy(loopScratch, 0, ringPts, off, count);
		Vector2 lo = loopScratch[0], hi = loopScratch[0], sum = Vector2.Zero;
		for (int i = 0; i < count; i++)
		{
			Vector2 p = loopScratch[i];
			lo = new Vector2(MathF.Min(lo.X, p.X), MathF.Min(lo.Y, p.Y));
			hi = new Vector2(MathF.Max(hi.X, p.X), MathF.Max(hi.Y, p.Y));
			sum += p;
			Vector2 prev = loopScratch[(i + count - 1) % count], next = loopScratch[(i + 1) % count];
			ringHeading[off + i] = HeadingOf(next - prev);
		}
		ref Ring ring = ref rings[slot];
		ring = new Ring
		{
			Count = count,
			Lo = lo,
			Hi = hi,
			Centre = sum / count,
			Area = area,
			LineAt = clock,
			FloodAt = clock,
			SecondAt = secondForm ? clock + SecondStrokeDelay : float.PositiveInfinity,
			PoolUntil = poolForm ? clock + PoolSeconds : 0f,
			PrisonUntil = prisonForm ? clock + PrisonSeconds : 0f,
			Damage = damage,
			Seed = NextSeed(),
			Live = true,
		};
		ring.HoldUntil = MathF.Max(ring.PoolUntil, ring.PrisonUntil);
		LayFlood(slot);
		// The Painted Door: a door a ring, inside it (InkRing.PaintedDoor.cs). A slot taken over drops the old ring's door.
		doors[slot] = default;
		if (doorForm)
			PaintDoor(slot);
		StatRings++;
		StatLargestArea = MathF.Max(StatLargestArea, area);
		Strike(slot);
		if (prisonForm)
			RaisePrison(slot);
		closedPending = true;
		armedFrame = ulong.MaxValue;
	}

	private int FreeRing()
	{
		for (int i = 0; i < MaxRings; i++)
		{
			int s = (nextRing + i) % MaxRings;
			if (!rings[s].Live)
			{
				nextRing = (s + 1) % MaxRings;
				return s;
			}
		}
		// Every ring is still drying: the oldest gives way (its prison, if it has one, with it).
		int oldest = nextRing;
		LowerPrison(oldest);
		nextRing = (oldest + 1) % MaxRings;
		return oldest;
	}

	// The flood's blots: a jittered grid over the ring, kept where it lies inside, each timed by how deep inside it is -
	// the rim floods first and the middle last, so the ink runs inward over what it struck.
	private void LayFlood(int slot)
	{
		ref Ring ring = ref rings[slot];
		int off = slot * MaxRingPoints, fOff = slot * MaxFlood;
		float spacing = MathF.Max(FloodSpacing, MathF.Sqrt(ring.Area / MaxFlood) * 1.15f);
		float rowStep = spacing * 0.87f;
		int rows = Math.Max(1, (int)MathF.Ceiling((ring.Hi.Y - ring.Lo.Y) / rowStep));
		int cols = Math.Max(1, (int)MathF.Ceiling((ring.Hi.X - ring.Lo.X) / spacing));
		int n = 0;
		float deepest = 1f;
		for (int j = 0; j <= rows && n < MaxFlood; j++)
		{
			for (int i = 0; i <= cols && n < MaxFlood; i++)
			{
				uint h = ring.Seed * 2654435761u + (uint)(j * 131 + i);
				float x = ring.Lo.X + i * spacing + ((j & 1) != 0 ? spacing * 0.5f : 0f) + (Hash(h, 1) - 0.5f) * spacing * 0.6f;
				float y = ring.Lo.Y + j * rowStep + (Hash(h, 2) - 0.5f) * rowStep * 0.6f;
				var p = new Vector2(x, y);
				if (!InkPath.Contains(ringPts, off, ring.Count, p))
					continue;
				float depth = MathF.Sqrt(InkPath.DistanceSquaredToEdges(ringPts, off, ring.Count, p));
				floodPts[fOff + n] = p;
				floodDelay[fOff + n] = depth;
				deepest = MathF.Max(deepest, depth);
				n++;
			}
		}
		for (int k = 0; k < n; k++)
			floodDelay[fOff + k] = FloodInSeconds * floodDelay[fOff + k] / deepest;
		ring.FloodCount = n;
	}

	private void Strike(int slot)
	{
		ref Ring ring = ref rings[slot];
		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;
		int off = slot * MaxRingPoints;
		var drop = new Vector2(0f, GroundDrop);
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			Vector2 e = enemy.GlobalPosition + drop;
			if (e.X < ring.Lo.X || e.Y < ring.Lo.Y || e.X > ring.Hi.X || e.Y > ring.Hi.Y
				|| !InkPath.Contains(ringPts, off, ring.Count, e))
				continue;
			StatStruck++;
			AddSplat(e);
			AddSplash(enemy);
			PlayerRef.DealDamageToEnemy(enemy, ring.Damage, 0f, true, SpellData, null, ring.Centre);
			if (IsInstanceValid(enemy) && !enemy.IsDying)
				enemy.ApplySlow(1f - SlowShare, SlowSeconds);
		}
	}

	private void TickRings()
	{
		for (int s = 0; s < MaxRings; s++)
		{
			ref Ring ring = ref rings[s];
			if (!ring.Live)
				continue;
			if (clock >= ring.SecondAt)
			{
				// Second Stroke: the brush goes round again - the ring re-wets, floods again and strikes again.
				ring.SecondAt = float.PositiveInfinity;
				ring.LineAt = clock;
				ring.FloodAt = clock;
				StatSecondStrokes++;
				Strike(s);
			}
			if (ring.Prisoned && clock >= ring.PrisonUntil)
				LowerPrison(s);
			float lineEnd = ring.LineAt + RingLineAges[^1] + MathF.Max(0f, ring.HoldUntil - ring.LineAt - RingLineAges[2]);
			float floodEnd = ring.FloodAt + FloodInSeconds + FloodAges[^1] + MathF.Max(0f, ring.HoldUntil - ring.FloodAt - FloodAges[2]);
			if (clock >= lineEnd && clock >= floodEnd && float.IsPositiveInfinity(ring.SecondAt) && !ring.Prisoned
				&& !DoorHolds(s))
				ring.Live = false;
		}
	}

	// --- Ink Prison -------------------------------------------------------------------------------------

	// The ring dries into a wall: thin segments along it on the enemy-only layer (Ma Liang's inked door lines' layer,
	// DottedDoorEvent.RaiseWall), so what it caught cannot get out and the wizard walks through it. A body per ring slot,
	// made the first time it is needed and kept; a lowered wall stands on no layer at all.
	private void RaisePrison(int slot)
	{
		ref Ring ring = ref rings[slot];
		StaticBody2D body = prisonBodies[slot];
		if (body == null || !IsInstanceValid(body))
		{
			body = new StaticBody2D { Name = $"InkPrison{slot}", CollisionLayer = 0, CollisionMask = 0 };
			for (int k = 0; k < PrisonSegments; k++)
			{
				var segment = new SegmentShape2D();
				prisonSegments[slot * PrisonSegments + k] = segment;
				body.AddChild(new CollisionShape2D { Shape = segment });
			}
			AddChild(body);
			body.GlobalPosition = Vector2.Zero;
			prisonBodies[slot] = body;
		}
		int off = slot * MaxRingPoints;
		int step = Math.Max(1, (int)MathF.Ceiling(ring.Count / (float)PrisonSegments));
		int used = 0;
		for (int i = 0; i < ring.Count && used < PrisonSegments; i += step)
		{
			SegmentShape2D segment = prisonSegments[slot * PrisonSegments + used];
			segment.A = ringPts[off + i];
			segment.B = ringPts[off + Math.Min(ring.Count, i + step) % ring.Count];
			used++;
		}
		// The shapes this ring does not need lie on its first wall, doubling it: a spare shape is never a stray wall.
		for (int k = used; k < PrisonSegments; k++)
		{
			SegmentShape2D spare = prisonSegments[slot * PrisonSegments + k];
			spare.A = prisonSegments[slot * PrisonSegments].A;
			spare.B = prisonSegments[slot * PrisonSegments].B;
		}
		// A boss shoulders through every wall (Enemy.CapsCrowdControl): this one never holds one.
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
			if (enemy != null && IsInstanceValid(enemy) && enemy.CapsCrowdControl)
				enemy.AddCollisionExceptionWith(body);
		body.CollisionLayer = PrisonLayerBit;
		ring.Prisoned = true;
		StatPrisons++;
	}

	private void LowerPrison(int slot)
	{
		if (prisonBodies[slot] != null && IsInstanceValid(prisonBodies[slot]))
			prisonBodies[slot].CollisionLayer = 0;
		rings[slot].Prisoned = false;
	}

	// --- Wet Ink and Inkwell ----------------------------------------------------------------------------

	// One walk of the swarm a tick: Wet Ink stings what touches the live line, Inkwell slows what wades a standing pool.
	private void Sweep(bool wet, bool pool)
	{
		Vector2 lineLo = default, lineHi = default;
		float reach = WetReach + RiptideWave.BodyAllowance * 0.5f;
		float reachSquared = reach * reach;
		if (wet)
		{
			lineLo = lineHi = path.At(path.Tail);
			for (long q = path.Tail; q < path.Head; q++)
			{
				Vector2 p = path.At(q);
				lineLo = new Vector2(MathF.Min(lineLo.X, p.X), MathF.Min(lineLo.Y, p.Y));
				lineHi = new Vector2(MathF.Max(lineHi.X, p.X), MathF.Max(lineHi.Y, p.Y));
			}
			lineLo -= new Vector2(reach, reach);
			lineHi += new Vector2(reach, reach);
		}
		int wetDamage = Math.Max(1, Mathf.RoundToInt(damage * WetShare));
		var drop = new Vector2(0f, GroundDrop);
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			Vector2 e = enemy.GlobalPosition + drop;
			if (pool)
			{
				for (int s = 0; s < MaxRings; s++)
				{
					ref Ring ring = ref rings[s];
					if (!ring.Live || clock >= ring.PoolUntil
						|| e.X < ring.Lo.X || e.Y < ring.Lo.Y || e.X > ring.Hi.X || e.Y > ring.Hi.Y
						|| !InkPath.Contains(ringPts, s * MaxRingPoints, ring.Count, e))
						continue;
					enemy.ApplySlow(1f - PoolSlowShare, PoolTickSeconds + 0.15f);
					StatPoolSlows++;
					break;
				}
			}
			if (!wet || e.X < lineLo.X || e.Y < lineLo.Y || e.X > lineHi.X || e.Y > lineHi.Y || StungLately(enemy))
				continue;
			for (long q = path.Tail; q < path.Head; q++)
			{
				Vector2 p = path.At(q);
				if (e.DistanceSquaredTo(p) > reachSquared)
					continue;
				NoteSting(enemy);
				StatStings++;
				AddSplat(p);
				// Many small stings: no elemental chain of their own (Corposant's wake's rule); the ring's strike sets one off.
				PlayerRef.DealDamageToEnemy(enemy, wetDamage, 0f, false, SpellData, null, p);
				break;
			}
		}
	}

	private bool StungLately(Enemy enemy)
	{
		float since = clock - WetEverySeconds;
		for (int k = 0; k < WetMemory; k++)
			if (ReferenceEquals(stung[k], enemy) && stungAt[k] > since)
				return true;
		return false;
	}

	private void NoteSting(Enemy enemy)
	{
		stung[stungNext] = enemy;
		stungAt[stungNext] = clock;
		stungNext = (stungNext + 1) % WetMemory;
	}

	private bool AnyPool()
	{
		for (int s = 0; s < MaxRings; s++)
			if (rings[s].Live && clock < rings[s].PoolUntil)
				return true;
		return false;
	}

	private bool AnyRing()
	{
		for (int s = 0; s < MaxRings; s++)
			if (rings[s].Live)
				return true;
		return false;
	}

	private bool AnySplat()
	{
		for (int s = 0; s < MaxSplats; s++)
		{
			if (!splats[s].Live)
				continue;
			if (clock - splats[s].Born < SplatAges[^1])
				return true;
			splats[s].Live = false;
		}
		return false;
	}

	private bool AnySplash()
	{
		for (int s = 0; s < MaxSplashes; s++)
		{
			if (!splashes[s].Live)
				continue;
			if (clock - splashes[s].Born < SplashSeconds)
				return true;
			splashes[s].Live = false;
			splashes[s].Body = null;
		}
		return false;
	}

	private void AddSplat(Vector2 at)
	{
		int slot = nextSplat;
		nextSplat = (nextSplat + 1) % MaxSplats;
		splats[slot] = new Splat { At = at, Born = clock, Seed = NextSeed(), Live = true };
	}

	private void AddSplash(Enemy body)
	{
		int slot = nextSplash;
		nextSplash = (nextSplash + 1) % MaxSplashes;
		splashes[slot] = new Splash { Body = body, At = body.GlobalPosition, Born = clock, Seed = NextSeed(), Live = true };
	}

	private uint NextSeed() => ++seedClock * 2654435761u;

	// --- the drawing ------------------------------------------------------------------------------------

	public override void _Draw()
	{
		if (sheet == null)
			return;
		int boil = (int)(clock / BoilSeconds);
		// The floods first, under everything: the wash the closed rings filled with.
		for (int s = 0; s < MaxRings; s++)
			if (rings[s].Live)
				DrawFlood(s, boil);
		DrawLine(boil);
		for (int s = 0; s < MaxRings; s++)
			if (rings[s].Live)
				DrawRingLine(s, boil);
		for (int s = 0; s < MaxSplats; s++)
		{
			if (!splats[s].Live)
				continue;
			int stage = StageOf(clock - splats[s].Born, SplatAges);
			if (stage >= 0)
				Stamp(SplatRect(stage, (int)(splats[s].Seed >> 7) % SplatShapes), splats[s].At, SplatCell);
		}
	}

	// The line, a dab a point, each the age of its point; and a wet dab at his feet, so the line runs to the brush.
	private void DrawLine(int boil)
	{
		if (path.Count == 0)
			return;
		float[] ages = wetForm ? WetLineAges : LineAges;
		float life = MathF.Max(0.05f, inkLife);
		for (long q = path.Tail; q < path.Head; q++)
		{
			int stage = StageOf((clock - path.BornAt(q)) / life, ages);
			if (stage < 0)
				continue;
			uint seed = path.SeedAt(q);
			Stamp(LineRect(stage, path.HeadingAt(q), (int)((seed >> 5) % Windows), (boil + (int)(seed >> 11)) & 1), path.At(q), LineCell);
		}
		if (PlayerRef != null && IsInstanceValid(PlayerRef) && !PlayerRef.IsDead && path.Count > 0
			&& PlayerRef.FootPosition.DistanceSquaredTo(path.Newest) < BreakDistance * BreakDistance)
		{
			Vector2 tip = PlayerRef.FootPosition;
			Vector2 last = path.Newest;
			if (tip.DistanceSquaredTo(last) > 9f)
				Stamp(LineRect(0, HeadingOf(tip - last), 1, boil & 1), (tip + last) * 0.5f, LineCell);
		}
	}

	private void DrawRingLine(int s, int boil)
	{
		ref Ring ring = ref rings[s];
		float t = Held(clock - ring.LineAt, RingLineAges, 2, ring.HoldUntil - ring.LineAt);
		int stage = StageOf(t, RingLineAges);
		if (stage < 0)
			return;
		int off = s * MaxRingPoints;
		for (int i = 0; i < ring.Count; i++)
		{
			uint seed = ring.Seed + (uint)i * 40503u;
			Stamp(LineRect(stage, ringHeading[off + i], (int)((seed >> 5) % Windows), (boil + (int)(seed >> 9)) & 1), ringPts[off + i], LineCell);
		}
	}

	private void DrawFlood(int s, int boil)
	{
		ref Ring ring = ref rings[s];
		int fOff = s * MaxFlood;
		for (int k = 0; k < ring.FloodCount; k++)
		{
			float start = ring.FloodAt + floodDelay[fOff + k];
			if (clock < start)
				continue;
			float t = Held(clock - start, FloodAges, 2, ring.PoolUntil - start);
			int stage = StageOf(t, FloodAges);
			if (stage < 0)
				continue;
			uint seed = ring.Seed + (uint)k * 69069u;
			int shape = (int)((seed >> 6) & 1);
			Stamp(FloodRect(stage, shape * 2 + ((boil + (int)(seed >> 10)) & 1)), floodPts[fOff + k], FloodCell);
		}
	}

	// The splash on what a ring struck, in the air over it: a splat of ink that dries in a third of a second.
	private void DrawAir()
	{
		if (sheet == null)
			return;
		for (int s = 0; s < MaxSplashes; s++)
		{
			if (!splashes[s].Live)
				continue;
			float age = clock - splashes[s].Born;
			if (age >= SplashSeconds)
				continue;
			Enemy body = splashes[s].Body;
			if (body != null && IsInstanceValid(body) && !body.IsDying)
				splashes[s].At = body.GlobalPosition;
			Vector2 at = splashes[s].At + new Vector2(0f, -6f);
			int shape = (int)(splashes[s].Seed >> 7) % SplatShapes;
			Rect2 src = SplatRect(age < SplashSeconds * 0.5f ? 0 : 1, shape);
			var size = new Vector2(SplatCell * 2f, SplatCell * 2f);
			air.DrawTextureRectRegion(sheet, new Rect2(SpellInk.Snap(at) - size * 0.5f, size), src);
		}
	}

	private void Stamp(Rect2 src, Vector2 at, int cell)
	{
		var size = new Vector2(cell * 2f, cell * 2f);
		DrawTextureRectRegion(sheet, new Rect2(SpellInk.Snap(at) - size * 0.5f, size), src);
	}

	private static Rect2 LineRect(int stage, int heading, int window, int boil) =>
		new(((heading * Windows + window) * Boils + boil) * LineCell, stage * LineCell, LineCell, LineCell);

	private static Rect2 FloodRect(int stage, int column) =>
		new(column * FloodCell, FloodY + stage * FloodCell, FloodCell, FloodCell);

	private static Rect2 SplatRect(int stage, int shape) =>
		new(SplatX + shape * SplatCell, SplatY + stage * SplatCell, SplatCell, SplatCell);

	/// <summary>The stage <paramref name="t"/> falls in, by when each ends; -1 once the last is over.</summary>
	public static int StageOf(float t, float[] ends)
	{
		if (t < 0f)
			return 0;
		for (int k = 0; k < ends.Length; k++)
			if (t < ends[k])
				return k;
		return -1;
	}

	// A stage held: while the hold lasts (until `holdEnd`, from the stamp's start) the clock stops inside stage `held`,
	// and after it the rest of the stages run as they would have. A pool's flood and a prison's line keep their dried
	// wash and their hard tide lines this way, and then dry away like any other.
	private static float Held(float t, float[] ends, int held, float holdEnd)
	{
		float extra = holdEnd - ends[held];
		if (extra <= 0f || t < ends[held - 1])
			return t;
		if (t < ends[held] + extra)
			return MathF.Min(t, ends[held] - 0.001f);
		return t - extra;
	}

	/// <summary>The sheet's heading (0-7, 22.5 degrees apart over half a turn) nearest a direction.</summary>
	public static byte HeadingOf(Vector2 d)
	{
		if (d.LengthSquared() < 0.0001f)
			return 0;
		float a = MathF.Atan2(d.Y, d.X);
		if (a < 0f)
			a += MathF.PI;
		return (byte)(Mathf.RoundToInt(a / (MathF.PI / Headings)) % Headings);
	}

	// A small integer hash to 0..1 (Corposant's), so the flood's jitter needs no stored state.
	private static float Hash(uint seed, int i)
	{
		uint h = (uint)i * 374761393u + seed * 668265263u;
		h = (h ^ (h >> 13)) * 1274126177u;
		h ^= h >> 16;
		return (h & 0xFFFFFF) / 16777216f;
	}
}

/// <summary>
/// Ink Ring's line as geometry: a ring buffer of dated points, the test that finds where the newest segment crosses an
/// older one, and the loop that crossing closes. Plain C# with no node, so RegressionChecks.InkRing can walk it round a
/// loop by hand. Points are numbered by a sequence that only grows; the buffer holds the last <c>Capacity</c> of them.
/// </summary>
public sealed class InkPath
{
	/// <summary>The newest segments a new one is never tested against: the ones it touches, and two more of the curve.</summary>
	public const int SkipNewest = 3;

	private readonly Vector2[] points;
	private readonly float[] born;
	private readonly uint[] seeds;
	private readonly byte[] headings;
	// crossFloor: no segment starting before it can be crossed. strokeStart: the first point of the stroke being laid.
	private long head, tail, crossFloor, strokeStart;
	private const byte Unset = 255;

	public InkPath(int capacity)
	{
		points = new Vector2[capacity];
		born = new float[capacity];
		seeds = new uint[capacity];
		headings = new byte[capacity];
	}

	public long Head => head;
	public long Tail => tail;
	public int Count => (int)(head - tail);
	public Vector2 Newest => At(head - 1);
	public Vector2 At(long seq) => points[Index(seq)];
	public float BornAt(long seq) => born[Index(seq)];
	public uint SeedAt(long seq) => seeds[Index(seq)];
	public byte HeadingAt(long seq) => headings[Index(seq)] == Unset ? (byte)0 : headings[Index(seq)];

	private int Index(long seq) => (int)(seq % points.Length);

	/// <summary>Drops every point older than <paramref name="life"/> seconds at <paramref name="now"/>.</summary>
	public void Expire(float now, float life)
	{
		while (tail < head && now - born[Index(tail)] >= life)
			tail++;
		if (crossFloor < tail)
			crossFloor = tail;
	}

	public void Add(Vector2 p, float now, uint seed)
	{
		if (head - tail >= points.Length)
			tail++;
		int i = Index(head);
		points[i] = p;
		born[i] = now;
		seeds[i] = seed;
		// The dab's heading is its segment's; a stroke's first point takes its second's as soon as there is one.
		bool continues = head > tail && head > strokeStart;
		headings[i] = continues ? InkRing.HeadingOf(p - At(head - 1)) : Unset;
		if (continues && headings[Index(head - 1)] == Unset)
			headings[Index(head - 1)] = headings[i];
		head++;
		if (crossFloor < tail)
			crossFloor = tail;
	}

	/// <summary>Starts a new stroke: nothing before the next point can be crossed, and no segment joins it to them.</summary>
	public void Break()
	{
		crossFloor = head;
		strokeStart = head;
	}

	/// <summary>
	/// The newest segment against every older one that may still be crossed, newest first. On a crossing whose loop holds
	/// at least <paramref name="minArea"/>, writes the loop into <paramref name="loop"/> - the crossing, then every point
	/// walked since, thinned evenly to fit - returns its count and makes everything up to the newest point inert, so the
	/// line closes the loop once. Returns 0 when nothing closes; <paramref name="tooSmall"/> says whether a crossing was
	/// turned down for its size.
	/// </summary>
	public int TryClose(Vector2[] loop, float minArea, out float area, out bool tooSmall)
	{
		area = 0f;
		tooSmall = false;
		long a0 = head - 2;
		if (a0 < tail || a0 < crossFloor)
			return 0;
		Vector2 a = At(a0), b = At(a0 + 1);
		long floor = Math.Max(tail, crossFloor);
		for (long s = a0 - 1 - SkipNewest; s >= floor; s--)
		{
			if (!Cross(a, b, At(s), At(s + 1), out float t))
				continue;
			Vector2 x = a + (b - a) * t;
			// The points walked since the crossed segment: s + 1 to a0.
			int walked = (int)(a0 - s);
			int total = walked + 1;
			int step = Math.Max(1, (int)Math.Ceiling(total / (double)loop.Length));
			int count = 0;
			loop[count++] = x;
			for (int k = step - 1; k < walked && count < loop.Length; k += step)
				loop[count++] = At(s + 1 + k);
			area = MathF.Abs(SignedArea(loop, 0, count));
			if (area < minArea)
			{
				tooSmall = true;
				continue;
			}
			crossFloor = head - 1;
			return count;
		}
		return 0;
	}

	/// <summary>Whether segment a-b crosses segment c-d, and where along a-b (0 to 1).</summary>
	public static bool Cross(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out float t)
	{
		t = 0f;
		Vector2 r = b - a, s = d - c;
		float denominator = r.X * s.Y - r.Y * s.X;
		if (MathF.Abs(denominator) < 1e-6f)
			return false;
		Vector2 ac = c - a;
		float along = (ac.X * s.Y - ac.Y * s.X) / denominator;
		float other = (ac.X * r.Y - ac.Y * r.X) / denominator;
		if (along < 0f || along > 1f || other < 0f || other > 1f)
			return false;
		t = along;
		return true;
	}

	/// <summary>The shoelace area of <paramref name="count"/> points from <paramref name="offset"/>, signed by their turn.</summary>
	public static float SignedArea(Vector2[] poly, int offset, int count)
	{
		float sum = 0f;
		for (int i = 0, j = count - 1; i < count; j = i++)
			sum += poly[offset + j].X * poly[offset + i].Y - poly[offset + i].X * poly[offset + j].Y;
		return sum * 0.5f;
	}

	/// <summary>Whether <paramref name="p"/> lies inside the polygon (crossing number, so a loop that turns either way works).</summary>
	public static bool Contains(Vector2[] poly, int offset, int count, Vector2 p)
	{
		bool inside = false;
		for (int i = 0, j = count - 1; i < count; j = i++)
		{
			Vector2 pi = poly[offset + i], pj = poly[offset + j];
			if ((pi.Y > p.Y) != (pj.Y > p.Y) && p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y) + pi.X)
				inside = !inside;
		}
		return inside;
	}

	/// <summary>The squared distance from <paramref name="p"/> to the nearest edge of the polygon.</summary>
	public static float DistanceSquaredToEdges(Vector2[] poly, int offset, int count, Vector2 p)
	{
		float best = float.MaxValue;
		for (int i = 0, j = count - 1; i < count; j = i++)
			best = MathF.Min(best, DistanceSquaredToSegment(p, poly[offset + j], poly[offset + i]));
		return best;
	}

	/// <summary>The squared distance from <paramref name="p"/> to the segment from <paramref name="a"/> to <paramref name="b"/>.</summary>
	public static float DistanceSquaredToSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float lengthSquared = ab.LengthSquared();
		if (lengthSquared < 0.0001f)
			return p.DistanceSquaredTo(a);
		float t = Mathf.Clamp((p - a).Dot(ab) / lengthSquared, 0f, 1f);
		return p.DistanceSquaredTo(a + ab * t);
	}
}
