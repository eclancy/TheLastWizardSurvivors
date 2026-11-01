using Godot;
using System.Collections.Generic;
using WizardSurvivors.scripts;

// The Last Bridge (castle piece C5): THE CONSTABLE (.ai/endgame-castle.md 3.6).
//
// "He holds the gate from the wall. Get to the gate." The keeper of his castle's gate, taken: the
// tallest figure on the bridge, plated under the gate's red livery, the gate's keys at his hip and a
// halberd taller than he is (tools/art/boss_constable.py).
//
// THE VERB NO OTHER BOSS ASKS: ADVANCE UNDER FIRE. He fights from the wall-walk over the gate, where
// nothing can reach him (Enemy.SetTargetable(false), the burrowers' primitive, with his sprite kept on
// show: he is up there, not under anything), and directs volleys onto the yard - files of crossbow bolts
// down the yard at the player (TelegraphedGroundHit lanes, one through where he stands and the rest
// beside it, so the way forward is a sidestep) and rings of poured pitch on him and ahead of him. Every
// one is marked well before it lands. When the player has stood at the gate's foot - within
// DropZoneRadius of it - for DropHoldSeconds, the Constable comes down into the yard, targetable, and
// fights there for YardSeconds: a halberd charge down the yard (a Line, the Gaoler's shape at the end of
// a travel level) and a Cone sweep at anyone who closes. Then he climbs back.
//
// THE GATE HAS TO BE RETAKEN. Every time he reaches the wall - the first time included - the gate's
// murder-holes pour pitch over its whole foot (one ring the size of the gate's ground, GatePourTellSeconds
// to get off it), and the hold at the gate only starts to count once it has landed. A player who stood
// his ground at the gate through the yard fight is driven off it and has to come back under the volleys;
// that is "the yard has to be crossed three times" made true for a player who never left the gate.
//
// PHASES {0.66, 0.33}: each one sends him back up to the wall at once (he finishes the swing he is in,
// never more) and thickens the volleys - more files, more pitch, sooner - so the gate is taken three
// times. Nothing else changes: the yard fight is the same fight each time, the ground to it harder.
//
// WHERE HE STANDS. He appears at the gate (ChapterRules hook 4, BridgeRules: the gate's foot) and goes
// straight up onto the wall-walk over the gate, where his arrival plays. "The gate's foot" for the hold is
// the gate itself as hook 5 names it (BridgeRules.VictoryPortalPoint, 190 px in front of the castle wall):
// a player who walks up to the gate is on it. The wall-walk is measured off the gate's art
// (props_bridge.py, gate_shut: the merlon tops stand about 170 px over the gate's foot on its middle
// column), so his boots are hidden behind the parapet and he is sorted behind the gate - the wall is in
// front of him. Off the bridge (a tool scene, or a Boss Rush arena if the chapter ever joins it) there is
// no wall: the gate is where he spawned and the walk WallFallbackRise above it.
//
// Built from the primitives only: SetTargetable, TelegraphedGroundHit (lanes and rings), GroundSlamAttack
// (Line and Cone) and the travelling telegraph (PlantsDuringWindUp). No shared behaviour was edited.
public partial class BridgeConstableBoss : BossEnemy
{
	// --- The gate and the wall ------------------------------------------------------------------------

	/// <summary>How near the gate's foot the player must stand for the hold to count, px.</summary>
	[Export] public float DropZoneRadius { get; set; } = 280f;
	/// <summary>Seconds of standing there before he comes down. Counted down again, not reset, off it.</summary>
	[Export] public float DropHoldSeconds { get; set; } = 1.5f;
	/// <summary>Seconds he fights in the yard before he climbs back.</summary>
	[Export] public float YardSeconds { get; set; } = 12f;
	/// <summary>The leap down off the wall, s.</summary>
	[Export] public float LeapDownSeconds { get; set; } = 0.85f;
	/// <summary>The leap back up, s, before the distance he covers is added (LeapUpPxPerSecond).</summary>
	[Export] public float LeapUpSeconds { get; set; } = 0.7f;
	[Export] public float LeapUpPxPerSecond { get; set; } = 1500f;
	/// <summary>He lands at least this far from the player: coming down is never itself a hit.</summary>
	[Export] public float LandingClearance { get; set; } = 180f;
	/// <summary>Seconds after he lands before his first swing may begin.</summary>
	[Export] public float FirstSwingDelaySeconds { get; set; } = 0.7f;

	// --- The volleys, from the wall ---------------------------------------------------------------------

	[Export] public float VolleyIntervalSeconds { get; set; } = 3.0f;
	/// <summary>A file of bolts: a lane down the yard at the player.</summary>
	[Export] public float BoltTellSeconds { get; set; } = 1.3f;
	[Export] public int BoltDamage { get; set; } = 3;
	/// <summary>Between neighbouring files, centre to centre. A lane is 52 px wide (TelegraphedGroundHit).</summary>
	[Export] public float BoltLaneSpacing { get; set; } = 150f;
	/// <summary>How far toward the gate from the player a file starts, at most, and how far past him it runs.</summary>
	[Export] public float BoltLaneLead { get; set; } = 420f;
	[Export] public float BoltLaneOvershoot { get; set; } = 320f;
	/// <summary>A ring of pitch poured on him, and ahead of him toward the gate.</summary>
	[Export] public float PitchTellSeconds { get; set; } = 1.4f;
	[Export] public float PitchRadius { get; set; } = 72f;
	[Export] public int PitchDamage { get; set; } = 4;
	/// <summary>The gate's own pour, over its whole foot, each time he reaches the wall.</summary>
	[Export] public float GatePourTellSeconds { get; set; } = 1.7f;
	[Export] public int GatePourDamage { get; set; } = 5;

	// THE VOLLEYS THICKEN, by phase: files per volley, rings per volley, and the interval's share. A
	// volley is files or pitch in turn; in the last phase it is both at once.
	private static readonly int[] FilesByPhase = { 2, 3, 4 };
	private static readonly int[] RingsByPhase = { 1, 2, 3 };
	private static readonly float[] IntervalByPhase = { 1f, 0.85f, 0.72f };

	// --- The yard ------------------------------------------------------------------------------------

	// The charge is BossEnemy's slam as a Line: SlamRadius its length, SlamTelegraphSeconds its wind-up,
	// SlamDamage its hit, SlamIntervalSeconds the clock between any two swings.
	[Export] public float ChargeHalfWidth { get; set; } = 34f;
	/// <summary>How much of the charge's wind-up is spent planted, showing the lane.</summary>
	[Export] public float ChargeTellFraction { get; set; } = 0.45f;
	[Export] public float ChargeSpeedMultiplier { get; set; } = 4.0f;
	/// <summary>The sweep: a wide cone of the halberd at anyone who closes.</summary>
	[Export] public float SweepRadius { get; set; } = 170f;
	[Export] public float SweepHalfAngleDegrees { get; set; } = 62f;
	[Export] public float SweepTelegraphSeconds { get; set; } = 0.9f;
	[Export] public int SweepDamage { get; set; } = 6;

	[Export] public float FirstWallHealthFraction { get; set; } = 0.66f;
	[Export] public float SecondWallHealthFraction { get; set; } = 0.33f;

	/// <summary>Off the bridge only: how far above the gate the walk is, px (there is no wall to measure).</summary>
	[Export] public float WallFallbackRise { get; set; } = 360f;

	// The gate's art (props_bridge.py, gate_shut, anchored on the gate's foot): the merlon tops on its
	// middle column stand 172 px over the foot and slope 45 degrees with the wall. His boots stand 128 px
	// over the foot, so the parapet hides them across the width of his stance.
	private const float WallWalkRise = 128f;
	// His sprite: the 96 cell at x2, centred on the body, boots on art row 94 (art-direction 5).
	private const float FeetBelowNode = (94f - 48f) * 2f;
	// How high he rises before he goes over the parapet, and the hop on the way down.
	private const float LeapApexRise = 76f;
	private const float LeapHop = 46f;
	// The player's walk, for TuningProblems' yardsticks (Player.Speed).
	private const float PlayerWalkSpeed = 220f;

	// The castle's bolts are his cold fire, the archers' own (BridgeRules' volleys before he comes);
	// the pitch burns. His own two swings are warm iron, so his lane is never read as the archers': the
	// charge a pale gold, the sweep hotter.
	private static readonly Color BoltColor = new(0.78f, 0.84f, 0.96f);
	private static readonly Color PitchColor = new(0.96f, 0.52f, 0.18f);
	private static readonly Color ChargeColor = new(0.98f, 0.82f, 0.42f);
	private static readonly Color SweepColor = new(1.0f, 0.60f, 0.28f);

	private enum Post { Wall, LeapingDown, Yard, LeapingUp }
	private enum Swing { Charge, Sweep }

	private Post post = Post.Wall;
	private Swing swing = Swing.Charge;
	// The ground he reads, set once in _Ready.
	private Vector2 gateFoot;         // the centre of the hold
	private Vector2 wallSpot;         // his node on the wall-walk
	private Vector2 laneSource;       // where the files come from: the gate, on the deck
	private BridgeRules bridge;

	private float holdSeconds;
	private float pourLeft;
	private float volleyLeft;
	private int volleyCount;
	private int volleyPhase;
	private float leapT;
	private float leapSeconds;
	private Vector2 leapFrom;
	private Vector2 leapApex;
	private Vector2 leapTo;
	private float yardLeft;
	private float sinceLanding;
	private float sinceResolve = 10f;
	private bool climbOrdered;
	private bool chargeHitLanded;
	private bool wallHeld;
	// Seconds since his arrival ended (the run clock stops when a boss comes).
	private float fightSeconds;

	/// <summary>Where he is: on the wall, in the yard, or between them. For _BossProbe and photographs.</summary>
	public bool HoldsTheWall => post == Post.Wall;
	public bool IsInYard => post == Post.Yard;
	/// <summary>The middle of the gate's foot, where the hold counts.</summary>
	public Vector2 GateFoot => gateFoot;

	// --- Lifetime ----------------------------------------------------------------------------------------

	public override void _Ready()
	{
		// Before the base, which starts the arrival where the body stands: it plays on the wall.
		ReadTheGround(GlobalPosition);
		GlobalPosition = wallSpot;
		base._Ready();
		post = Post.Wall;
		// Among the bodies while he is on the wall-walk, so the parapet in front of him hides his boots;
		// over every spell (BossEnemy's DrawLayers.Foremost) from the leap down until he climbs back.
		ZIndex = DrawLayers.Bodies;
		pourLeft = 0f;
		foreach (string problem in TuningProblems())
			GD.PushError("BridgeConstableBoss: " + problem);
		GD.Print($"CONSTABLE: the gate's foot {gateFoot.Round()}, the wall-walk {wallSpot.Round()}"
			+ (bridge == null ? " (no bridge here: the walk is WallFallbackRise over where he came)" : ""));
	}

	// One line at each change of post, on the fight's own clock (the run's stops when a boss comes): the
	// evidence a probe or a balance run reads to say how long he was in reach.
	private void Note(string what) =>
		GD.Print($"CONSTABLE: {what} {fightSeconds:0.0}s into the fight, health {BossHealthFraction * 100f:0}%, phase {CurrentPhase}");

	// The gate, the walk and the files' source, from the bridge's own geometry when he is on it.
	private void ReadTheGround(Vector2 spawnedAt)
	{
		bridge = Node2DGame.Current?.Rules as BridgeRules;
		if (bridge != null)
		{
			gateFoot = bridge.VictoryPortalPoint(spawnedAt) ?? spawnedAt;
			// The gate's art stands on the lattice line past the gate yard's last row of deck
			// (BridgeFurniture.CastleWall: x - y = e * 48 for e the first row of water).
			float wallS = Mathf.Ceil(BridgeBand.End / BridgeBand.StepPx) * BridgeBand.StepPx;
			Vector2 gate = bridge.WorldAt(wallS, 0f);
			wallSpot = gate - new Vector2(0f, WallWalkRise + FeetBelowNode);
			laneSource = bridge.WorldAt(BridgeBand.End - 40f, 0f);
			return;
		}
		gateFoot = spawnedAt;
		laneSource = spawnedAt + new Vector2(0f, -120f);
		wallSpot = spawnedAt - new Vector2(0f, WallFallbackRise + FeetBelowNode);
	}

	protected override float[] BuildPhaseThresholds() => new[] { FirstWallHealthFraction, SecondWallHealthFraction };

	protected override GroundSlamAttack BuildSlam() =>
		new(SlamIntervalSeconds, SlamTelegraphSeconds, SlamRadius, SlamDamage, ChargeColor)
		{
			Shape = TelegraphShape.Line,
			HalfWidth = ChargeHalfWidth,
		};

	// Each threshold: the volleys thicken, and he goes back up at once. A colder cast to him each time,
	// so the change has a cause on screen, as every boss's does.
	protected override void OnPhaseEntered(int phase)
	{
		volleyPhase = Mathf.Clamp(phase, 0, FilesByPhase.Length - 1);
		SetBaseModulate(phase >= 2 ? new Color(0.80f, 0.82f, 1.0f) : new Color(0.90f, 0.91f, 1.0f));
		if (post == Post.Yard)
			climbOrdered = true;
	}

	// The arrival hands the body back targetable (BossEnemy.FinishEntrance) on the stage's frame; he is
	// on the wall, so it is taken out of play again before anything can aim at it.
	protected internal override void OnEntranceEnd(BossDeath stage) => CallDeferred(MethodName.HoldTheWall);

	private void HoldTheWall()
	{
		if (IsDying || post != Post.Wall)
			return;
		KeepOutOfReach();
		ReachTheWall();
	}

	// Untargetable, but seen: SetTargetable hides the sprite, which is right for a burrower under the
	// floor and wrong for a man on a wall (BooEnemy does the same for its phase).
	private void KeepOutOfReach()
	{
		SetTargetable(false);
		if (BodySprite != null)
			BodySprite.Visible = true;
	}

	// --- The clock ------------------------------------------------------------------------------------

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		sinceResolve += dt;
		fightSeconds += dt;
		base._PhysicsProcess(delta);
		if (IsDying)
			return;
		switch (post)
		{
			case Post.Wall:
				if (!wallHeld)
					HoldTheWall();
				GlobalPosition = wallSpot;
				Velocity = Vector2.Zero;
				TickVolleys(dt);
				TickHold(dt);
				break;
			case Post.LeapingDown:
				TickLeapDown(dt);
				break;
			case Post.Yard:
				TickYard(dt);
				TickRunOver();
				break;
			case Post.LeapingUp:
				TickLeapUp(dt);
				break;
		}
	}

	// --- The wall -------------------------------------------------------------------------------------

	// On the wall: the gate pours first, and the hold only counts once it has landed.
	private void ReachTheWall()
	{
		wallHeld = true;
		post = Post.Wall;
		ZIndex = DrawLayers.Bodies;
		holdSeconds = 0f;
		pourLeft = GatePourTellSeconds;
		volleyLeft = GatePourTellSeconds + 0.5f;
		Node arena = GetParent();
		if (arena != null && IsInstanceValid(arena))
			TelegraphedGroundHit.Place(arena, gateFoot, GatePourTellSeconds, DropZoneRadius, ScaleOutgoingDamage(GatePourDamage), PitchColor);
		Note("holds the wall; the gate pours");
	}

	private void TickHold(float dt)
	{
		if (pourLeft > 0f)
		{
			pourLeft -= dt;
			holdSeconds = 0f;
			return;
		}
		Node2D target = TargetPlayer;
		if (target == null || !IsInstanceValid(target))
			return;
		NoteWhereTheWizardIs(target, dt);
		bool atTheGate = target.GlobalPosition.DistanceTo(gateFoot) <= DropZoneRadius;
		holdSeconds = atTheGate ? holdSeconds + dt : Mathf.Max(0f, holdSeconds - dt);
		if (holdSeconds >= DropHoldSeconds)
			BeginLeapDown(target.GlobalPosition);
	}

	// Debug builds (every probe, harness and photograph): every 20 s he holds the wall, where the wizard
	// is from the gate's foot - so a balance run that never brings him down says why.
	private float untilWhereabouts = 20f;

	private void NoteWhereTheWizardIs(Node2D wizard, float dt)
	{
		if (!OS.IsDebugBuild())
			return;
		untilWhereabouts -= dt;
		if (untilWhereabouts > 0f)
			return;
		untilWhereabouts = 20f;
		Vector2 p = wizard.GlobalPosition;
		string where = bridge != null
			? $" ({bridge.AlongOf(p) - bridge.AlongOf(gateFoot):0} px along, {bridge.AcrossOf(p):0} across)"
			: "";
		Note($"holds the wall; the wizard is {p.DistanceTo(gateFoot):0} px from the gate's foot{where}, the hold at {holdSeconds:0.0}s,");
	}

	private void TickVolleys(float dt)
	{
		volleyLeft -= dt;
		if (volleyLeft > 0f)
			return;
		volleyLeft = VolleyIntervalSeconds * IntervalByPhase[volleyPhase];
		Node2D target = TargetPlayer;
		Node arena = GetParent();
		if (target == null || !IsInstanceValid(target) || arena == null || !IsInstanceValid(arena))
			return;
		bool last = volleyPhase >= FilesByPhase.Length - 1;
		if (last || volleyCount % 2 == 0)
			LooseFiles(arena, target.GlobalPosition);
		if (last || volleyCount % 2 == 1)
			PourPitch(arena, target.GlobalPosition);
		volleyCount++;
		PlayAttackAnimation();
	}

	// Files of bolts down the yard: one through where he stands, the rest beside it at BoltLaneSpacing,
	// alternately either side, all from the gate's end. The way forward is the gap beside the file.
	private void LooseFiles(Node arena, Vector2 at)
	{
		Vector2 toPlayer = at - laneSource;
		float d = toPlayer.Length();
		Vector2 down = d > 1f ? toPlayer / d : (bridge != null ? -BridgeBand.Along : Vector2.Down);
		var across = new Vector2(-down.Y, down.X);
		float back = Mathf.Min(d, BoltLaneLead);
		int damage = ScaleOutgoingDamage(BoltDamage);
		int files = FilesByPhase[volleyPhase];
		for (int k = 0; k < files; k++)
		{
			// 0, +1, -1, +2: the first file on him, the rest stepping out either side in turn.
			float offset = ((k + 1) / 2) * (k % 2 == 1 ? 1f : -1f) * BoltLaneSpacing;
			Vector2 start = at - down * back + across * offset;
			if (!OnDeck(start + down * back, 0f))
				continue;
			TelegraphedGroundHit.Place(arena, start, BoltTellSeconds + 0.08f * k, back + BoltLaneOvershoot, damage,
				BoltColor, TelegraphShape.Line, down);
		}
	}

	// Pitch: a ring on him, and the rest on his way to the gate - poured where he is going.
	private void PourPitch(Node arena, Vector2 at)
	{
		int damage = ScaleOutgoingDamage(PitchDamage);
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		Vector2 toGate = gateFoot - at;
		float d = toGate.Length();
		Vector2 ahead = d > 1f ? toGate / d : Vector2.Up;
		var across = new Vector2(-ahead.Y, ahead.X);
		int rings = RingsByPhase[volleyPhase];
		for (int k = 0; k < rings; k++)
		{
			Vector2 spot = k == 0
				? at + new Vector2(rng.RandfRange(-20f, 20f), rng.RandfRange(-14f, 14f))
				: at + ahead * Mathf.Min(d, 150f + 110f * k) + across * rng.RandfRange(-90f, 90f);
			if (k > 0 && !OnDeck(spot, 30f))
				continue;
			TelegraphedGroundHit.Place(arena, spot, PitchTellSeconds + 0.15f * k, PitchRadius, damage, PitchColor);
		}
	}

	private bool OnDeck(Vector2 world, float margin) =>
		bridge == null || BridgeBand.IsOnDeck(world - (bridge.Game?.StageOrigin ?? Vector2.Zero), margin);

	// --- Down into the yard, and back up ---------------------------------------------------------------

	private void BeginLeapDown(Vector2 player)
	{
		post = Post.LeapingDown;
		leapT = 0f;
		leapSeconds = Mathf.Max(0.2f, LeapDownSeconds);
		leapFrom = wallSpot;
		leapApex = wallSpot - new Vector2(0f, LeapApexRise);
		leapTo = LandingNear(player);
		PlayAttackAnimation();
		Note("comes down into the yard");
	}

	// The gate's foot unless he would land on top of the player; then the first clear spot beside him,
	// never past the gate and never off the deck.
	private Vector2 LandingNear(Vector2 player)
	{
		if (player.DistanceTo(gateFoot) >= LandingClearance)
			return gateFoot;
		Vector2 along = bridge != null ? BridgeBand.Along : Vector2.Up;
		var across = new Vector2(along.Y, -along.X);
		Vector2 away = player.DistanceTo(gateFoot) > 1f ? (gateFoot - player).Normalized() : along;
		foreach (Vector2 dir in new[] { away, across, -across, -along, -away })
		{
			Vector2 spot = player + dir * LandingClearance;
			if (OnDeck(spot, 60f) && (bridge == null || bridge.AlongOf(spot) <= BridgeBand.End - 80f))
				return spot;
		}
		return gateFoot;
	}

	// Up first, behind the parapet; over it at the top (drawn over the wall from there); down into the yard.
	private void TickLeapDown(float dt)
	{
		leapT += dt / leapSeconds;
		const float rise = 0.3f;
		if (leapT < rise)
		{
			GlobalPosition = leapFrom.Lerp(leapApex, BossDeath.EaseOut(leapT / rise));
			ZIndex = DrawLayers.Bodies;
		}
		else
		{
			float u = Mathf.Clamp((leapT - rise) / (1f - rise), 0f, 1f);
			GlobalPosition = leapApex.Lerp(leapTo, u * u) - new Vector2(0f, LeapHop * 4f * u * (1f - u));
			ZIndex = DrawLayers.Foremost;
		}
		Velocity = Vector2.Zero;
		if (leapT >= 1f)
			Land();
	}

	private void Land()
	{
		GlobalPosition = leapTo;
		ZIndex = DrawLayers.Foremost;
		post = Post.Yard;
		wallHeld = false;
		yardLeft = YardSeconds;
		sinceLanding = 0f;
		climbOrdered = false;
		SetTargetable(true);
		Note("lands in the yard");
	}

	private void TickYard(float dt)
	{
		sinceLanding += dt;
		yardLeft -= dt;
		// A swing once begun is always finished (AttackTelegraph): he climbs when it has landed.
		if ((yardLeft <= 0f || climbOrdered) && !Slam.IsWindingUp)
			BeginLeapUp();
	}

	private void BeginLeapUp()
	{
		post = Post.LeapingUp;
		KeepOutOfReach();
		leapT = 0f;
		leapFrom = GlobalPosition;
		leapApex = wallSpot - new Vector2(0f, LeapApexRise);
		leapTo = wallSpot;
		leapSeconds = Mathf.Max(0.3f, LeapUpSeconds + leapFrom.DistanceTo(leapApex) / Mathf.Max(1f, LeapUpPxPerSecond));
		PlayAttackAnimation();
		Note(climbOrdered ? "climbs back, sent up by the threshold" : "climbs back, the yard's time up");
	}

	// Over the bodies to the top of the parapet, then down behind it onto the walk.
	private void TickLeapUp(float dt)
	{
		leapT += dt / leapSeconds;
		const float over = 0.72f;
		if (leapT < over)
		{
			float u = leapT / over;
			GlobalPosition = leapFrom.Lerp(leapApex, BossDeath.EaseOut(u)) - new Vector2(0f, LeapHop * 4f * u * (1f - u));
			ZIndex = DrawLayers.Foremost;
		}
		else
		{
			GlobalPosition = leapApex.Lerp(leapTo, BossDeath.EaseIn(Mathf.Clamp((leapT - over) / (1f - over), 0f, 1f)));
			ZIndex = DrawLayers.Bodies;
		}
		Velocity = Vector2.Zero;
		if (leapT >= 1f)
		{
			GlobalPosition = wallSpot;
			ReachTheWall();
		}
	}

	// --- The yard fight: the charge and the sweep --------------------------------------------------------

	protected override bool PlantsDuringWindUp => swing == Swing.Sweep;

	// The swing is chosen BEFORE its wind-up starts, because the wind-up's length is the swing's own and
	// the clock takes it on the frame it starts. Never while one is winding up, and never in the strike's
	// flash after one lands, so the shape on the ground cannot change under the player's eyes.
	protected override bool WantsToAttack(Node2D target, float distanceToPlayer)
	{
		if (post != Post.Yard || climbOrdered || yardLeft <= 0f || sinceLanding < FirstSwingDelaySeconds)
			return false;
		if (Slam.IsWindingUp || sinceResolve < 0.35f)
			return false;
		if (distanceToPlayer <= SweepRadius * 1.15f)
			return ChooseSwing(Swing.Sweep);
		if (distanceToPlayer <= SlamRadius * 1.25f)
			return ChooseSwing(Swing.Charge);
		return false;
	}

	private bool ChooseSwing(Swing next)
	{
		swing = next;
		if (next == Swing.Sweep)
		{
			Slam.Shape = TelegraphShape.Cone;
			Slam.Radius = SweepRadius;
			Slam.HalfAngleDegrees = SweepHalfAngleDegrees;
			Slam.WindUpSeconds = SweepTelegraphSeconds;
			Slam.Damage = SweepDamage;
			Slam.WarningColor = SweepColor;
		}
		else
		{
			Slam.Shape = TelegraphShape.Line;
			Slam.Radius = SlamRadius;
			Slam.HalfWidth = ChargeHalfWidth;
			Slam.WindUpSeconds = SlamTelegraphSeconds;
			Slam.Damage = SlamDamage;
			Slam.WarningColor = ChargeColor;
		}
		return true;
	}

	protected override void OnAttackStarted(Node2D target)
	{
		chargeHitLanded = false;
		base.OnAttackStarted(target);
	}

	protected override void OnAttackResolved(Node2D target)
	{
		sinceResolve = 0f;
		// Already billed for being run down: one charge is one hit.
		if (swing == Swing.Charge && chargeHitLanded)
			return;
		base.OnAttackResolved(target);
	}

	protected override Vector2 AdjustSteering(Vector2 chaseDirection, float distanceToPlayer)
	{
		if (post != Post.Yard)
			return Vector2.Zero;
		if (Slam.IsWindingUp)
		{
			if (swing == Swing.Sweep)
				return Vector2.Zero;
			// The tell: planted, the lane drawn. Then down the lane he drew, not at the player.
			return Slam.WindUpProgress < ChargeTellFraction ? Vector2.Zero : Slam.Facing * ChargeSpeedMultiplier;
		}
		return climbOrdered || yardLeft <= 0f ? Vector2.Zero : chaseDirection;
	}

	// THE CHARGE HURTS WHERE IT PASSES (GaolerBoss's reasoning): standing on the lane when he arrives is
	// the hit, once a charge; the lane ahead of him still resolves at the end for a player who backed off
	// down it instead of stepping aside.
	private void TickRunOver()
	{
		if (swing != Swing.Charge || chargeHitLanded || !Slam.IsWindingUp || Slam.WindUpProgress < ChargeTellFraction)
			return;
		Node2D target = TargetPlayer;
		if (target == null || !IsInstanceValid(target) || !target.HasMethod("TakeDamage"))
			return;
		if (GlobalPosition.DistanceTo(target.GlobalPosition) > ChargeHalfWidth + RunOverBodyRadius)
			return;
		chargeHitLanded = true;
		int dealt = ScaleOutgoingDamage(SlamDamage);
		if (dealt > 0)
			target.Call("TakeDamage", dealt);
	}

	private const float RunOverBodyRadius = 26f;

	// --- What would make this fight unfair or pointless ---------------------------------------------------

	/// <summary>
	/// The tuning held to the player's walk and to the fight's own shape, in words; empty when it holds. Run
	/// at _Ready (each an error), and RegressionChecks.ValidateBossPattern's case for this boss.
	/// </summary>
	public IEnumerable<string> TuningProblems()
	{
		if (DropZoneRadius <= 120f)
			yield return $"the gate's foot is {DropZoneRadius:0} px across its middle; a hold that small is a pixel to find under fire.";
		if (DropHoldSeconds <= 0f)
			yield return "he comes down the moment the player is near the gate, so the advance is the whole fight and the hold is nothing.";
		if (YardSeconds < SlamIntervalSeconds * 2f)
			yield return $"{YardSeconds}s in the yard is under two of his swings ({SlamIntervalSeconds}s apart); the yard fight is a visit.";
		if (GatePourTellSeconds * PlayerWalkSpeed < DropZoneRadius + 40f)
			yield return $"the gate's pour is marked {GatePourTellSeconds}s, too little to walk {DropZoneRadius:0} px off the gate's foot.";
		if (BoltTellSeconds <= 0.6f || PitchTellSeconds <= 0.6f)
			yield return $"a volley is marked {Mathf.Min(BoltTellSeconds, PitchTellSeconds)}s; the volleys are meant to be read and walked through.";
		if (BoltLaneSpacing <= 52f + 56f)
			yield return $"files {BoltLaneSpacing:0} px apart leave no gap a wizard fits between two 52 px lanes.";
		if (PitchRadius * 2f >= DropZoneRadius)
			yield return $"a {PitchRadius:0} px ring of pitch covers the gate's foot from side to side, so the hold cannot be kept by stepping.";
		if (BoltDamage <= 0 || PitchDamage <= 0 || GatePourDamage <= 0 || SweepDamage <= 0)
			yield return "a volley, the pour or the sweep deals nothing, so it is scenery.";
		if (ChargeTellFraction <= 0f || ChargeTellFraction >= 1f)
			yield return $"ChargeTellFraction {ChargeTellFraction}: at 0 the charge has no tell and at 1 it never runs.";
		if (ChargeSpeedMultiplier <= 1f)
			yield return $"he charges at {ChargeSpeedMultiplier}x his walk, so the lane promises a reach he never covers.";
		if (SweepHalfAngleDegrees <= 0f || SweepHalfAngleDegrees >= 90f)
			yield return $"the sweep's half-angle is {SweepHalfAngleDegrees}; outside 0-90 it is nothing or a half-plane.";
		if (SweepTelegraphSeconds <= 0f)
			yield return "the sweep is not marked, so it lands unseen.";
		if (LandingClearance <= SweepRadius * 0.9f && FirstSwingDelaySeconds <= 0f)
			yield return "he lands inside his own sweep with no pause, so coming down is a hit the player cannot read.";
		if (FirstWallHealthFraction <= SecondWallHealthFraction)
			yield return $"the walls are at {FirstWallHealthFraction} and {SecondWallHealthFraction}; the first has to be the higher.";
		if (LeapDownSeconds <= 0.2f || LeapUpSeconds <= 0f)
			yield return "the leaps take no time, so he is on the wall or in the yard with nothing seen between.";
	}

	// --- The arrival (BossDeath, entrance) ---------------------------------------------------------------
	//
	// UP THE GATEHOUSE STAIR. The castle sounds him: his cold fire runs along the battlements over the gate,
	// merlon by merlon, from the towers in toward the middle; then the halberd's head comes up over the
	// parapet, then the sallet with the mark lit in its slit, then the whole of him, climbing; and he brings
	// the butt of the halberd down on the walk with a ring of iron that shakes grit and dust off the merlons
	// in front of him. He is on the wall: the arrival plays where he will fight from.
	protected internal override float EntranceSeconds => 3.2f;

	private BossDeath.Piece climbing;
	private const float FlamesFrom = 0.2f;
	private const float FlamesLit = 1.15f;
	private const float ClimbFrom = 0.95f;
	private const float ClimbTop = 2.35f;
	private const float Grounds = 2.6f;
	private static readonly Color MerlonDust = new(0.46f, 0.50f, 0.60f, 0.6f);
	// The parapet in front of him, in the stage's space: the merlon line passes about 44 px over his boots
	// on his middle and runs down to the right at 45 degrees (the wall's own slope on screen).
	private const float ParapetOverFeet = 44f;
	private const int Flames = 6;
	private const float FlameStep = 26f;

	protected internal override void OnEntranceBegin(BossDeath stage)
	{
		climbing = stage.CutWhole();
		RiseFromGround(stage, climbing, 0f);
	}

	protected internal override void TickEntrance(BossDeath stage, float t, float dt)
	{
		if (climbing == null)
			return;
		float s = stage.ArtScale.X;
		// Up the stair a step at a time: the rise comes in short heaves rather than a glide.
		float climb = BossDeath.Span(t, ClimbFrom, ClimbTop);
		float steps = Mathf.Floor(climb * 7f) / 7f + BossDeath.EaseInOut(climb * 7f - Mathf.Floor(climb * 7f)) / 7f;
		RiseFromGround(stage, climbing, Mathf.Clamp(steps, 0f, 1f));
		float jolt = t > Grounds ? BossDeath.Shudder(t, 1f - BossDeath.Span(t, Grounds, Grounds + 0.3f), 18f) * s : 0f;
		climbing.Offset = new Vector2(jolt, climbing.Offset.Y);
		climbing.Color = new Color(0.62f, 0.64f, 0.78f).Lerp(Colors.White, BossDeath.Span(t, ClimbFrom + 0.4f, ClimbTop));

		Vector2 foot = stage.Foot;
		Vector2 parapet = foot - new Vector2(0f, ParapetOverFeet);
		if (stage.Crossed(Grounds))
		{
			// The butt of the halberd on the walk: grit off the merlons in front of him, and dust.
			stage.EmitAt(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.ChipA, Variants = 3, Tinted = true, SpeedMin = 40f, SpeedMax = 130f,
				UpMin = 40f, UpMax = 120f, Angle = Mathf.Pi * 0.75f, Spread = Mathf.Pi * 0.9f, Spin = 6f,
				Bounce = 0.2f, Life = 1.3f, Fade = 0.4f,
			}, 9, parapet + new Vector2(-10f, 10f), 4f, 34f);
			stage.DustRing(parapet + new Vector2(0f, 8f), 46f, 7, MerlonDust, 0.75f, 14f);
		}
	}

	// The cold fire running along the battlements, merlon by merlon from either tower in toward him.
	protected internal override void DrawEntranceLight(BossDeath stage, CanvasItem canvas, float t)
	{
		if (climbing == null)
			return;
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		Vector2 parapet = stage.Foot - new Vector2(0f, ParapetOverFeet);
		float fade = 1f - BossDeath.Span(t, ClimbTop, EntranceSeconds + 0.3f);
		for (int k = Flames; k >= 1; k--)
		{
			// Outermost first: each pair lights FlamesLit/Flames after the one outside it.
			float lit = BossDeath.Span(t, FlamesFrom + (Flames - k) * (FlamesLit - FlamesFrom) / Flames, FlamesFrom + (Flames - k + 1) * (FlamesLit - FlamesFrom) / Flames);
			if (lit <= 0f)
				continue;
			float flicker = 0.8f + 0.2f * Mathf.Sin(t * 11f + k * 1.7f);
			for (int side = -1; side <= 1; side += 2)
			{
				Vector2 at = BossDeath.Snap(parapet + new Vector2(side * k * FlameStep, side * k * FlameStep - 4f));
				BossDeath.Disc(canvas, at, 7f * lit, 7f * lit, new Color(arcane.Mid, 0.45f * fade * flicker));
				BossDeath.Disc(canvas, at - new Vector2(0f, 2f), 3f * lit, 4f * lit, new Color(arcane.Hot, 0.9f * fade * flicker));
			}
		}
		// The mark comes alight in the slit as his head clears the parapet.
		float eyes = BossDeath.Span(t, ClimbFrom + 0.55f, ClimbFrom + 0.7f) * (1f - BossDeath.Span(t, Grounds + 0.1f, EntranceSeconds + 0.3f));
		if (eyes > 0f)
		{
			BossDeath.Disc(canvas, stage.PieceToLocal(climbing, new Vector2(54f, 16f)), 5f, 4f, new Color(arcane.Hot, 0.85f * eyes));
		}
	}

	// --- The fall (BossDeath) ------------------------------------------------------------------------------
	//
	// THE GATE GIVEN UP. He falls in the yard, at his gate. The mark gutters in the slit; his grip opens and
	// the halberd goes over, away from him, ringing on the setts; he goes down on his knees where he stood,
	// and the ring of the gate's keys drops from his belt into a heap at his side - not thrown, given up -
	// where the brass key of the gate winks. His head bows; the plate goes dark; he is gone, kneeling, and
	// the keys are the last of him. (The Gaoler's keys burst from its hand and skitter; his only fall.)
	// Three pieces cut on the drawing (boss_constable.py, HOW THE FIGHT CUTS IT): the halberd, the sallet,
	// and the body.
	private BossDeath.Piece fallHalberd;
	private BossDeath.Piece fallHelm;
	private BossDeath.Piece fallBody;

	private const float HalberdTips = 0.15f;
	private const float HalberdLands = 0.78f;
	private const float KeysDrop = 0.5f;
	private static readonly Color SettDust = new(0.44f, 0.46f, 0.54f, 0.6f);

	protected internal override float DeathSeconds => 2.8f;

	protected internal override void OnDeathBegin(BossDeath death)
	{
		fallHalberd = death.Cut(new Vector2I(72, 93), new Rect2I(62, 0, 34, 26), new Rect2I(71, 26, 4, 70));
		fallHelm = death.Cut(new Vector2I(46, 25), new Rect2I(26, 0, 36, 26));
		fallBody = death.Cut(new Vector2I(46, 94), new Rect2I(0, 26, 71, 70), new Rect2I(75, 26, 21, 70), new Rect2I(0, 0, 26, 26));
	}

	protected internal override void TickDeath(BossDeath death, float t, float dt)
	{
		if (fallBody == null)
			return;
		float s = death.ArtScale.X;

		// The halberd goes over forward, off his open hand, and rings once where it lands.
		float topple = BossDeath.EaseIn(BossDeath.Span(t, HalberdTips, HalberdLands));
		float bounce = 0.06f * Mathf.Sin(Mathf.Pi * BossDeath.Span(t, HalberdLands, HalberdLands + 0.2f));
		fallHalberd.Rotation = Mathf.DegToRad(84f) * topple - bounce;

		// He goes down on his knees, leaning a little over them; the head bows further than the body.
		float kneel = BossDeath.EaseInOut(BossDeath.Span(t, 0.25f, 1.05f));
		float shudder = t < 0.3f ? BossDeath.Shudder(t, 1f - t / 0.3f, 16f) * s : 0f;
		fallBody.Sink(death, 20f * kneel);
		fallBody.Offset = new Vector2(shudder, fallBody.Offset.Y);
		fallBody.Rotation = Mathf.DegToRad(5f) * kneel;
		// The sallet rides the body down (the neck's pivot sinks with it) and then bows on its own.
		Vector2 neck = death.PieceToLocal(fallBody, new Vector2(46f, 26f)) - death.ArtToLocal(new Vector2(46f, 26f));
		fallHelm.Offset = new Vector2(neck.X * (death.Mirrored ? -1f : 1f), neck.Y);
		fallHelm.Rotation = fallBody.Rotation + Mathf.DegToRad(18f) * BossDeath.EaseInOut(BossDeath.Span(t, 0.7f, 1.4f));

		// Then the plate goes dark and he is gone, kneeling.
		float dim = BossDeath.Span(t, 1.3f, 2.0f);
		var plate = new Color(1f - 0.5f * dim, 1f - 0.48f * dim, 1f - 0.4f * dim, 1f - BossDeath.Span(t, 1.9f, 2.65f));
		fallBody.Color = plate;
		fallHelm.Color = plate;
		fallHalberd.Color = new Color(plate, 1f - BossDeath.Span(t, 2.0f, DeathSeconds));

		if (death.Crossed(HalberdLands))
		{
			// The whole length of it on the setts: dust along it, a spark of iron where the head lands.
			for (int k = 0; k < 4; k++)
			{
				Vector2 along = death.PieceToLocal(fallHalberd, new Vector2(72f, 80f - k * 22f));
				death.PuffAt(new Vector2(along.X, death.Foot.Y - 4f), 6f, 20f, 0.6f, SettDust, 10f, k * 0.04f);
			}
			Vector2 head = death.PieceToLocal(fallHalberd, new Vector2(78f, 14f));
			death.EmitAt(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.ChipB, Tinted = true, SpeedMin = 50f, SpeedMax = 150f, UpMin = 50f, UpMax = 130f,
				Spin = 6f, Bounce = 0.3f, Life = DeathSeconds - HalberdLands, Fade = 0.4f,
			}, 5, new Vector2(head.X, death.Foot.Y - 6f), 6f, 14f);
		}

		if (death.Crossed(KeysDrop))
		{
			// The ring of the gate's keys off his belt, into a heap at his side. A small hop, no skitter.
			var keys = new BossDeath.Throw
			{
				Cell = BossDeath.Cell.KeyLong, SpeedMin = 10f, SpeedMax = 40f, UpMin = 30f, UpMax = 70f,
				Angle = Mathf.Pi, Spread = Mathf.Pi * 0.6f, Spin = 3f, Bounce = 0.15f, Friction = 10f,
				Life = DeathSeconds - KeysDrop, Fade = 0.25f, Glint = new Color(1f, 0.94f, 0.7f, 0.9f),
			};
			death.Emit(keys, 2, new Vector2(24f, 70f), new Vector2(3f, 3f));
			keys.Cell = BossDeath.Cell.KeyShort;
			death.Emit(keys, 2, new Vector2(24f, 70f), new Vector2(4f, 3f), 0.05f);
			keys.Cell = BossDeath.Cell.Ring;
			keys.UpMax = 40f;
			death.Emit(keys, 1, new Vector2(24f, 66f), new Vector2(1f, 1f), 0.02f);
		}

		if (death.Crossed(1.05f))
			death.DustRing(death.Foot, 40f, 6, SettDust, 0.7f, 14f);
	}

	// The mark gutters in the slit and goes out: a flare, two catches, dark.
	protected internal override void DrawDeathLight(BossDeath death, CanvasItem canvas, float t)
	{
		if (fallHelm == null || t > 0.9f)
			return;
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		float flare = BossDeath.Span(t, 0f, 0.05f) * (1f - BossDeath.Span(t, 0.1f, 0.3f));
		float gutter = (t > 0.38f && t < 0.46f) || (t > 0.6f && t < 0.66f) ? 0.5f : 0f;
		float a = Mathf.Max(flare, gutter);
		if (a <= 0f)
			return;
		BossDeath.Disc(canvas, death.PieceToLocal(fallHelm, new Vector2(54f, 16f)), 6f, 4f, new Color(arcane.Hot, 0.85f * a));
	}
}
