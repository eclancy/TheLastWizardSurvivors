using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// THE UNSTRUNG HUNTER - the White Downs' warden (stage_14, .ai/second-half.md 4.2, piece F3). "The hunter the sky
// lost: it draws on you along a line that follows you - put a barrow between you." Orion, the sky's hunter, lost
// when the gods' seats went dark, and worn now by the dark wizard's shadow: a tall hunter in a lion's pelt beside a
// great bow taller than himself, his mark the three cold stars of his belt and the thread of cold light he draws
// for a string (tools/art/boss_unstrung_hunter.py). His fall rekindles Artemis (AchievementDefinitions.SeatClear).
//
// COVER, THE ASK NO BOSS ON THE ROAD MAKES. His one attack is THE DRAW, a lane (TelegraphShape.Line) from his body
// out along his aim, as long as the downs are open. While he draws, the lane FOLLOWS the wizard (it turns toward
// him at DrawTurnDegreesPerSecond, faster than a running wizard sweeps round him at any range he keeps), and only in
// its last DrawLockSeconds does it hold still and whiten - the sidestep the telegraph contract promises
// (GroundSlamAttack.Facing), and a short one. The sure answer is a barrow (LevelGenerator.Downs.cs): the lane is
// cut where it first meets one, every frame, and that cut length is the Radius both the dashed outline and the
// hit read (GroundSlamAttack.Contains, the one authority), so what the player is shown stopping at the mound is
// exactly what stops there. Loosed into a barrow, the arrow strikes the turf and throws chalk.
//
// HE KEEPS HIS RANGE. A hunter does not close: he backs off inside KeepMinDistance, circles inside the band, and
// comes on only from beyond KeepMaxDistance - and whenever a barrow is between them he comes round it, by the
// swarm's own flow field, so cover is a place to stand for a draw or two and then to leave as he flanks it. He
// only begins a draw with a clear line (MazeNavigation.HasLineOfSight, the shot test every bolt uses).
//
// THREE FIGHTS (BuildPhaseThresholds: 0.66, 0.33):
//   1. The draw, as above.
//   2. THE HUNT IS UP (HuntHealthFraction): he winds the hunt's horn and slips his coursing hounds - the chapter's
//      own Fast (StageBestiary.WhiteDowns) - every HoundCallSeconds. Hounds run round a barrow, so the mound that
//      saves him from the arrow is no longer a place to wait: hold it while they come, or move to the next.
//   3. UNSTRUNG (EnrageHealthFraction): the thread at full stretch. The lane turns faster and holds for less, and
//      after every loose the sky answers - StarsPerFall marked stars fall round where the wizard stood, one on the
//      spot itself, and stars fall from above, over any barrow. So the cover he stands in is marked too, now and
//      then, and the answer is to leave it for a breath and come back.
//
// Every hit he and his stars land passes through ScaleOutgoingDamage (the chapter's damage and Weaken), the
// hounds are ordinary bodies, and nothing he places joins "enemies".
public partial class UnstrungHunterBoss : BossEnemy
{
	// --- The draw ---------------------------------------------------------------------------------
	/// <summary>How far the lane reaches across open ground, world px (the downs' long sightlines).</summary>
	[Export] public float DrawRange { get; set; } = 1150f;
	/// <summary>Half the lane's width. A wizard's feet are 28 px either side of his middle.</summary>
	[Export] public float DrawHalfWidth { get; set; } = 30f;
	/// <summary>How fast the lane turns after him while it is still following, degrees a second.</summary>
	[Export] public float DrawTurnDegreesPerSecond { get; set; } = 80f;
	/// <summary>The last stretch of the wind-up in which the lane holds still: the sidestep.</summary>
	[Export] public float DrawLockSeconds { get; set; } = 0.45f;
	/// <summary>He begins a draw only with the wizard this close (a share of the reach).</summary>
	[Export] public float DrawEngageShare { get; set; } = 0.85f;
	[Export] public Color DrawColor { get; set; } = new Color(0.74f, 0.58f, 1.0f);
	[Export] public Color LockedColor { get; set; } = new Color(0.96f, 0.92f, 1.0f);

	// --- Range ------------------------------------------------------------------------------------
	[Export] public float KeepMinDistance { get; set; } = 360f;
	[Export] public float KeepMaxDistance { get; set; } = 680f;
	/// <summary>How fast he circles inside the band, as a share of his walk.</summary>
	[Export] public float CircleShare { get; set; } = 0.55f;

	// --- Phase 2: the hunt is up ------------------------------------------------------------------
	[Export] public float HuntHealthFraction { get; set; } = 0.66f;
	[Export] public float HoundCallSeconds { get; set; } = 11f;
	[Export] public int HoundsPerCall { get; set; } = 3;
	/// <summary>A hound's health against the run's ordinary spawn health (Node2DGame.SpawnEventEnemy).</summary>
	[Export] public float HoundHealthMultiplier { get; set; } = 1.2f;
	[Export] public float HuntDrawIntervalMultiplier { get; set; } = 0.85f;

	// --- Phase 3: unstrung ------------------------------------------------------------------------
	[Export] public float UnstrungTurnDegreesPerSecond { get; set; } = 120f;
	[Export] public float UnstrungLockSeconds { get; set; } = 0.36f;
	[Export] public float UnstrungWindUpSeconds { get; set; } = 1.3f;
	[Export] public float UnstrungDrawIntervalMultiplier { get; set; } = 0.85f;
	[Export] public int StarsPerFall { get; set; } = 4;
	[Export] public float StarTellSeconds { get; set; } = 1.1f;
	[Export] public float StarRadius { get; set; } = 54f;
	[Export] public int StarDamage { get; set; } = 4;
	/// <summary>How far round the wizard's spot the other stars fall.</summary>
	[Export] public float StarSpread { get; set; } = 150f;
	[Export] public Color StarColor { get; set; } = new Color(0.98f, 0.82f, 0.44f);

	/// <summary>The wizard's walk (Player.Speed), for the sidestep's proof (TuningProblems).</summary>
	public const float WizardWalk = 220f;
	/// <summary>Half the wizard's feet (Player's Feet capsule, 56 wide).</summary>
	public const float WizardFootHalf = 28f;

	// The lane's reach is marched out to the first barrow in steps this long: a barrow is at least three tiles
	// (144 px) across, so a step of a quarter tile can never cross one unseen.
	private const float ReachStep = 12f;

	// Where the arrow leaves the bow, in the sprite's own cell (the grip at full draw, facing right); the node's
	// origin is the cell's middle (row 48), and the arrow row is 47, so the lane's origin is the body.
	private static readonly Vector2 GripArt = new(79f, 47f);
	private const float CellHalf = 48f;
	private const float SpriteScale = 2f;

	// The mark: the three stars of his belt, and his eye, in the walk's first frame (facing right).
	private static readonly Vector2[] BeltArt = { new(52f, 53f), new(48f, 54f), new(44f, 55f) };
	private static readonly Vector2 EyeArt = new(50.5f, 29f);

	private float turnDegreesPerSecond;
	private float lockSeconds;
	private bool locked;
	private bool huntUp;
	private float untilHounds;
	private bool unstrung;
	private float strafeSign = 1f;
	private float untilStrafeFlip = 5f;
	private readonly RandomNumberGenerator rng = new();

	/// <summary>Draws loosed, and how many of them a barrow caught. For _BossProbe.</summary>
	public int Looses { get; private set; }
	public int LoosesIntoBarrows { get; private set; }

	/// <summary>True while the lane holds still before the loose. For _UiShot and the probe.</summary>
	public bool IsLocked => locked && Slam != null && Slam.IsWindingUp;

	public override void _Ready()
	{
		turnDegreesPerSecond = DrawTurnDegreesPerSecond;
		lockSeconds = DrawLockSeconds;
		rng.Randomize();
		base._Ready();
		foreach (string problem in TuningProblems())
			GD.PushError("UnstrungHunterBoss: " + problem);
	}

	// THE DRAW: a lane, its reach the open ground before the first barrow (set every frame, below).
	protected override GroundSlamAttack BuildSlam() =>
		new(SlamIntervalSeconds, SlamTelegraphSeconds, DrawRange, SlamDamage, DrawColor)
		{
			Shape = TelegraphShape.Line,
			HalfWidth = DrawHalfWidth,
			GlyphOverride = TelegraphGlyph.Shot,
			Facing = Vector2.Right,
		};

	protected override float[] BuildPhaseThresholds() => new[] { HuntHealthFraction, EnrageHealthFraction };

	protected override void OnPhaseEntered(int phase)
	{
		if (phase == 1)
		{
			huntUp = true;
			untilHounds = 0.8f;
			SlamIntervalSeconds *= HuntDrawIntervalMultiplier;
			Slam.IntervalSeconds = SlamIntervalSeconds;
			return;
		}
		unstrung = true;
		turnDegreesPerSecond = UnstrungTurnDegreesPerSecond;
		lockSeconds = UnstrungLockSeconds;
		SlamIntervalSeconds *= UnstrungDrawIntervalMultiplier;
		Slam.IntervalSeconds = SlamIntervalSeconds;
		Slam.WindUpSeconds = UnstrungWindUpSeconds;
		// The thread at full stretch: he goes colder.
		SetBaseModulate(new Color(0.84f, 0.82f, 1.0f));
	}

	// He plants to draw: the lane is drawn from where he stands.
	protected override bool PlantsDuringWindUp => true;

	// A draw begins only with the wizard in reach and in sight: an arrow he can see would be swallowed by the
	// barrow before it is begun is a telegraph wasted (the shot test every ranged body uses).
	protected override bool WantsToAttack(Node2D target, float distanceToPlayer) =>
		distanceToPlayer <= DrawRange * DrawEngageShare && HasSight(target.GlobalPosition);

	protected override void OnAttackStarted(Node2D target)
	{
		base.OnAttackStarted(target);
		locked = false;
		Slam.WarningColor = DrawColor;
		Slam.Radius = ReachAlong(GlobalPosition, Slam.Facing);
		// The attack strip's release frame (5 of 6, at 1.25 s of its 1.5 s) lands on the loose.
		if (BodySprite != null)
			BodySprite.SpeedScale = 1.25f / Mathf.Max(0.3f, Slam.WindUpSeconds);
		SfxPlayer.AtPosition(SfxCatalog.HunterDraw, GlobalPosition, 0.02f);
	}

	protected override void OnAttackResolved(Node2D target)
	{
		Slam.Radius = ReachAlong(GlobalPosition, Slam.Facing);
		base.OnAttackResolved(target);
		locked = false;
		if (BodySprite != null)
			BodySprite.SpeedScale = 1f;
		Looses++;
		bool caught = Slam.Radius < DrawRange - ReachStep;
		if (caught)
			LoosesIntoBarrows++;
		LooseArrow(caught);
		SfxPlayer.AtPosition(SfxCatalog.HunterLoose, GlobalPosition, 0.03f);
		if (unstrung && target != null && IsInstanceValid(target))
			FallStars(target.GlobalPosition);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (IsDying)
			return;
		float dt = (float)delta;
		TickDraw(dt);
		TickHounds(dt);
		untilStrafeFlip -= dt;
		if (untilStrafeFlip <= 0f)
		{
			strafeSign = -strafeSign;
			untilStrafeFlip = rng.RandfRange(4f, 7f);
		}
	}

	// While he draws: the lane follows the wizard until its last DrawLockSeconds, then holds and whitens; and
	// every frame its reach is the open ground before the first barrow on it.
	private void TickDraw(float dt)
	{
		if (Slam == null || !Slam.IsWindingUp)
			return;
		float remaining = (1f - Slam.WindUpProgress) * Slam.WindUpSeconds;
		Node2D target = TargetPlayer;
		if (!locked && remaining <= lockSeconds)
		{
			locked = true;
			Slam.WarningColor = LockedColor;
		}
		if (!locked && target != null && IsInstanceValid(target))
		{
			Vector2 want = target.GlobalPosition - GlobalPosition;
			if (want.LengthSquared() > 1f)
			{
				float current = Slam.Facing.Angle();
				float turn = Mathf.AngleDifference(current, want.Angle());
				float step = Mathf.DegToRad(turnDegreesPerSecond) * dt;
				Slam.Facing = Vector2.FromAngle(current + Mathf.Clamp(turn, -step, step));
			}
		}
		Slam.Radius = ReachAlong(GlobalPosition, Slam.Facing);
		RedrawTells();
	}

	/// <summary>
	/// The open ground along <paramref name="direction"/> from <paramref name="from"/> before the first barrow, out to
	/// <see cref="DrawRange"/>: the lane's length, read by the outline and the hit alike. Barrows are walls
	/// (LevelGenerator.Downs.cs), and walls are what stop a shot (MazeNavigation.IsWallAt).
	/// </summary>
	public float ReachAlong(Vector2 from, Vector2 direction)
	{
		MazeNavigation nav = MazeNavigation.Active;
		if (nav == null || direction.LengthSquared() < 0.0001f)
			return DrawRange;
		Vector2 dir = direction.Normalized();
		for (float d = ReachStep; d <= DrawRange; d += ReachStep)
		{
			if (nav.IsWallAt(from + dir * d))
				return Mathf.Max(ReachStep, d - ReachStep * 0.5f);
		}
		return DrawRange;
	}

	private bool HasSight(Vector2 to) => MazeNavigation.Active?.HasLineOfSight(GlobalPosition, to) ?? true;

	// PHASE 2: the horn, and the hounds slipped at his side.
	private void TickHounds(float dt)
	{
		if (!huntUp)
			return;
		untilHounds -= dt;
		if (untilHounds > 0f)
			return;
		untilHounds = HoundCallSeconds;
		if (Node2DGame.Current is not Node2DGame game || !IsInstanceValid(game))
			return;
		SfxPlayer.AtPosition(SfxCatalog.HintHuntHorn, GlobalPosition, 0f);
		float baseAngle = rng.RandfRange(0f, Mathf.Tau);
		for (int i = 0; i < HoundsPerCall; i++)
		{
			float a = baseAngle + i * Mathf.Tau / Math.Max(1, HoundsPerCall);
			Vector2 at = game.ReachablePoint(GlobalPosition + Vector2.FromAngle(a) * 70f);
			game.SpawnEventEnemy(BestiaryRole.Fast, at, HoundHealthMultiplier, false);
		}
	}

	// PHASE 3: the sky answers the loose - stars marked round where the wizard stood, one on the spot.
	private void FallStars(Vector2 where)
	{
		Node arena = GetParent();
		if (arena == null || !IsInstanceValid(arena))
			return;
		int damage = ScaleOutgoingDamage(StarDamage);
		TelegraphedGroundHit.Place(arena, where, StarTellSeconds, StarRadius, damage, StarColor, owner: this);
		float offset = rng.RandfRange(0f, Mathf.Tau);
		int others = Math.Max(0, StarsPerFall - 1);
		for (int i = 0; i < others; i++)
		{
			float a = offset + i * Mathf.Tau / Math.Max(1, others);
			Vector2 at = where + Vector2.FromAngle(a) * StarSpread;
			TelegraphedGroundHit.Place(arena, at, StarTellSeconds + 0.12f * (i + 1), StarRadius, damage, StarColor, owner: this);
		}
	}

	// A hunter keeps his range: back off when the wizard comes in, circle in the band, come on from beyond it - and
	// come round a barrow whenever one is between them (the flow field's way to the wizard goes round it).
	protected override Vector2 AdjustSteering(Vector2 chaseDirection, float distanceToPlayer)
	{
		if (Slam != null && Slam.IsWindingUp)
			return Vector2.Zero;
		Node2D target = TargetPlayer;
		if (target == null || !IsInstanceValid(target))
			return chaseDirection;
		if (!HasSight(target.GlobalPosition))
			return chaseDirection;
		if (distanceToPlayer < KeepMinDistance)
			return -chaseDirection * 0.9f;
		if (distanceToPlayer > KeepMaxDistance)
			return chaseDirection;
		return new Vector2(-chaseDirection.Y, chaseDirection.X) * strafeSign * CircleShare;
	}

	// --- The arrow --------------------------------------------------------------------------------

	// The arrow is the lane's light thrown along it in an instant: a streak from the bow to where it stopped, gone
	// in a fifth of a second, and a burst of chalk and turf where a barrow caught it. In the air, over the bodies.
	private void LooseArrow(bool caught)
	{
		Node arena = GetParent();
		if (arena == null || !IsInstanceValid(arena))
			return;
		Vector2 facing = Slam.Facing.Normalized();
		bool right = facing.X >= 0f;
		Vector2 grip = GlobalPosition + new Vector2((right ? GripArt.X - CellHalf : CellHalf - GripArt.X) * SpriteScale, (GripArt.Y - CellHalf) * SpriteScale);
		Vector2 end = GlobalPosition + facing * Slam.Radius;
		arena.AddChild(new HunterArrow { From = grip, To = end, Caught = caught });
	}

	/// <summary>The loosed arrow's streak and, where a barrow caught it, the strike. Code-made; frees itself.</summary>
	private sealed partial class HunterArrow : Node2D
	{
		public Vector2 From;
		public Vector2 To;
		public bool Caught;
		private float age;
		private const float Life = 0.32f;

		public override void _Ready()
		{
			ZAsRelative = false;
			ZIndex = DrawLayers.Air;
			Material = ExploreArt.Additive;
		}

		public override void _Process(double delta)
		{
			age += (float)delta;
			if (age >= Life)
			{
				QueueFree();
				return;
			}
			QueueRedraw();
		}

		public override void _Draw()
		{
			ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
			float fade = 1f - age / Life;
			Vector2 a = ToLocal(From), b = ToLocal(To);
			// The streak runs out from the bow in the first tenth of a second, then fades from its tail.
			float run = Mathf.Clamp(age / 0.08f, 0f, 1f);
			Vector2 head = a.Lerp(b, run);
			Vector2 tail = a.Lerp(b, Mathf.Clamp((age - 0.08f) / (Life - 0.08f), 0f, 1f));
			DrawLine(tail, head, new Color(arcane.Mid, 0.55f * fade), 6f);
			DrawLine(tail, head, new Color(arcane.Core, 0.95f * fade), 2f);
			if (!Caught || run < 1f)
				return;
			// Struck into the mound: a starburst of cold light, and the turf and chalk thrown off it.
			float burst = Mathf.Clamp((age - 0.08f) / 0.2f, 0f, 1f);
			Vector2 dir = (b - a).Normalized();
			for (int k = 0; k < 7; k++)
			{
				float ang = dir.Angle() + Mathf.Pi + (k - 3) * 0.32f;
				Vector2 tip = b + Vector2.FromAngle(ang) * (10f + 26f * burst) * (k % 2 == 0 ? 1f : 0.65f);
				DrawLine(b, tip, new Color(arcane.Hot, 0.8f * fade), 2f);
			}
			DrawCircle(b, 7f * (1f - burst) + 2f, new Color(arcane.Core, 0.9f * fade));
		}
	}

	// --- Tuning ------------------------------------------------------------------------------------

	/// <summary>
	/// What would silently break the fight (RegressionChecks.ValidateBossPattern): a lane that never holds still
	/// (no sidestep - the telegraph contract) or holds too short for the wizard to step out of it, a lane narrower
	/// than a body, a range band upside down or past his reach, hounds or stars that never come.
	/// </summary>
	public IEnumerable<string> TuningProblems()
	{
		float stepOut = (DrawHalfWidth + WizardFootHalf) / WizardWalk;
		foreach (var (name, lockFor, windUp) in new[]
		{
			("the draw", DrawLockSeconds, SlamTelegraphSeconds),
			("the unstrung draw", UnstrungLockSeconds, UnstrungWindUpSeconds),
		})
		{
			if (lockFor <= 0f || lockFor >= windUp)
				yield return $"the Unstrung Hunter's {name} holds still for {lockFor}s of a {windUp}s wind-up; it must hold for part of it and follow for the rest.";
			if (lockFor < stepOut)
				yield return $"the Unstrung Hunter's {name} holds still for {lockFor}s, but stepping out of a {DrawHalfWidth * 2f} px lane takes {stepOut:0.00}s at the wizard's walk.";
		}
		if (DrawHalfWidth <= 0f || DrawRange <= KeepMaxDistance)
			yield return $"the Unstrung Hunter's lane ({DrawRange} px by {DrawHalfWidth * 2f}) must reach past the range he keeps ({KeepMaxDistance}).";
		if (KeepMinDistance <= 0f || KeepMinDistance >= KeepMaxDistance)
			yield return $"the Unstrung Hunter keeps {KeepMinDistance}-{KeepMaxDistance} px; the band is empty or upside down.";
		if (DrawTurnDegreesPerSecond <= 0f || UnstrungTurnDegreesPerSecond < DrawTurnDegreesPerSecond)
			yield return "the Unstrung Hunter's lane must follow the wizard, and follow faster unstrung.";
		if (HoundsPerCall <= 0 || HoundCallSeconds <= 0f)
			yield return "the Unstrung Hunter's horn calls no hounds, so the hunt never comes.";
		if (StarsPerFall <= 0 || StarTellSeconds <= 0f || StarRadius <= 0f || StarDamage <= 0)
			yield return "the Unstrung Hunter's stars fall unmarked, harmless or not at all.";
		if (HuntHealthFraction <= EnrageHealthFraction)
			yield return $"the hunt is up at {HuntHealthFraction} and the thread unstrung at {EnrageHealthFraction}; the hunt must come first.";
	}

	// --- The arrival (BossDeath, entrance) ---------------------------------------------------------
	//
	// THE SKY'S HUNTER, COME DOWN. Three cold stars fall out of the empty sky in a line - his belt - onto the turf;
	// the dark gathers round them into a man, the lion's pelt over him, the bow at his side; last the thread of
	// cold light springs taut between the bow's horns, and he stands to draw.
	protected internal override float EntranceSeconds => 3.2f;

	private BossDeath.Piece arriving;
	private const float StarsLand = 1.0f;
	private const float Gathered = 2.2f;

	protected internal override void OnEntranceBegin(BossDeath stage)
	{
		arriving = stage.CutWhole();
		arriving.Color = new Color(1f, 1f, 1f, 0f);
	}

	protected internal override void TickEntrance(BossDeath stage, float t, float dt)
	{
		if (arriving == null)
			return;
		float gather = BossDeath.EaseInOut(BossDeath.Span(t, StarsLand, Gathered));
		arriving.Color = new Color(0.4f + 0.6f * gather, 0.4f + 0.6f * gather, 0.55f + 0.45f * gather, gather);
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		arriving.Glow = new Color(arcane.Mid, 0.45f * BossDeath.Span(t, StarsLand, StarsLand + 0.3f) * (1f - BossDeath.Span(t, Gathered - 0.3f, Gathered + 0.3f)));
		arriving.Scale = new Vector2(1f, 0.86f + 0.14f * gather);

		Vector2 foot = stage.Foot;
		if (stage.Crossed(StarsLand))
		{
			stage.Impact(0.35f);
			stage.DustRing(foot, 44f, 8, DownsDust, 0.7f, 12f);
		}
		if (stage.Crossed(Gathered))
		{
			stage.Impact(0.6f);
			stage.EmitAt(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.Clod, SpeedMin = 30f, SpeedMax = 120f, UpMin = 80f, UpMax = 180f,
				Spin = 5f, Bounce = 0.2f, Life = 1.2f, Fade = 0.4f,
			}, 6, foot, 4f, 26f);
		}
	}

	// A shadow pooling on the turf under where the stars came down.
	protected internal override void DrawEntranceGround(BossDeath stage, CanvasItem canvas, float t)
	{
		float pool = BossDeath.EaseOut(BossDeath.Span(t, StarsLand - 0.2f, Gathered));
		float fade = 1f - BossDeath.Span(t, EntranceSeconds - 0.4f, EntranceSeconds + 0.6f);
		if (pool <= 0f || fade <= 0f)
			return;
		BossDeath.Disc(canvas, stage.Foot, 18f + 34f * pool, 7f + 12f * pool, new Color(0.04f, 0.05f, 0.09f, 0.7f * fade));
	}

	// The three stars falling in a line out of the sky to his belt; his eye opening; the thread springing taut.
	protected internal override void DrawEntranceLight(BossDeath stage, CanvasItem canvas, float t)
	{
		if (arriving == null)
			return;
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		for (int i = 0; i < BeltArt.Length; i++)
		{
			Vector2 belt = stage.PieceToLocal(arriving, BeltArt[i]);
			float start = 0.12f * i;
			float fall = BossDeath.EaseIn(BossDeath.Span(t, start, StarsLand));
			if (t < start)
				continue;
			Vector2 at = belt - new Vector2(30f * (1f - fall), 420f * (1f - fall));
			float glow = t < StarsLand ? 1f : 1f - 0.6f * BossDeath.Span(t, StarsLand, Gathered);
			if (t < StarsLand)
				BossDeath.Line(canvas, at - new Vector2(10f, 60f), at, new Color(arcane.Mid, 0.45f * glow), BossDeath.Px);
			BossDeath.Disc(canvas, at, 8f, 8f, new Color(arcane.Mid, 0.4f * glow));
			BossDeath.Disc(canvas, at, 3f, 3f, new Color(arcane.Core, 0.95f * glow));
		}
		float eye = BossDeath.Span(t, Gathered - 0.1f, Gathered + 0.05f) * (1f - BossDeath.Span(t, Gathered + 0.3f, EntranceSeconds));
		if (eye > 0f)
			BossDeath.Disc(canvas, stage.PieceToLocal(arriving, EyeArt), 6f, 6f, new Color(arcane.Hot, 0.85f * eye));
		// The thread, top horn to bottom, springing taut at the climax.
		float thread = BossDeath.Span(t, Gathered + 0.05f, Gathered + 0.2f) * (1f - BossDeath.Span(t, Gathered + 0.5f, EntranceSeconds + 0.2f));
		if (thread > 0f)
		{
			Vector2 top = stage.PieceToLocal(arriving, new Vector2(66f, 7f));
			Vector2 bottom = stage.PieceToLocal(arriving, new Vector2(66f, 89f));
			BossDeath.Line(canvas, top, bottom, new Color(arcane.Mid, 0.6f * thread), BossDeath.Px * 2f);
			BossDeath.Line(canvas, top, bottom, new Color(arcane.Core, 0.95f * thread));
		}
	}

	protected internal override void OnEntranceEnd(BossDeath stage)
	{
		if (BodySprite != null)
			BodySprite.SpeedScale = 1f;
	}

	// --- The fall (BossDeath) ---------------------------------------------------------------------
	//
	// THE SKY TAKES ITS HUNTER BACK. The three stars of his belt flare and leave him, rising out of the frame the way
	// they fell - the hunter going home to a sky that will hold him again when the huntress is rekindled - and what
	// they leave is shadow: the bow drops from his hand onto the turf, he goes down on one knee, and he sinks into
	// the dark he is.
	protected internal override float DeathSeconds => 2.6f;

	private BossDeath.Piece falling;
	private static readonly Color DownsDust = new(0.50f, 0.56f, 0.58f, 0.6f);

	protected internal override void OnDeathBegin(BossDeath death)
	{
		falling = death.CutWhole();
	}

	protected internal override void TickDeath(BossDeath death, float t, float dt)
	{
		if (falling == null)
			return;
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		float s = death.ArtScale.X;
		// A shudder as the stars leave, then down on one knee (a squash) and into the ground.
		float shake = t < 0.6f ? BossDeath.Shudder(t, BossDeath.Span(t, 0f, 0.6f), 10f) * s : 0f;
		float kneel = BossDeath.EaseOut(BossDeath.Span(t, 0.6f, 1.0f));
		falling.Offset = new Vector2(shake, 0f);
		falling.Scale = new Vector2(1f + 0.05f * kneel, 1f - 0.16f * kneel);
		falling.Sink(death, 88f * BossDeath.EaseIn(BossDeath.Span(t, 1.15f, 2.4f)));
		float dark = BossDeath.Span(t, 0.3f, 1.4f);
		falling.Color = new Color(1f - 0.6f * dark, 1f - 0.6f * dark, 1f - 0.45f * dark, 1f - BossDeath.Span(t, 2.0f, 2.6f));
		falling.Glow = new Color(arcane.Mid, 0.35f * (1f - BossDeath.Span(t, 0.1f, 0.5f)));

		if (death.Crossed(0.25f))
		{
			// The belt's three stars go up and away, out of the top of the picture.
			for (int i = 0; i < BeltArt.Length; i++)
			{
				death.Emit(new BossDeath.Throw
				{
					Size = 4f, Color = arcane.Core, ColorB = arcane.Mid, Light = true, NoLand = true,
					SpeedMin = 2f, SpeedMax = 10f, UpMin = 260f, UpMax = 320f, Gravity = -240f, Drag = 0.2f,
					Life = 1.6f, Fade = 0.3f,
				}, 1, BeltArt[i], new Vector2(1f, 1f), 0.08f * i);
			}
		}
		if (death.Crossed(1.0f))
		{
			death.Impact(0.7f);
			death.DustRing(death.Foot, 70f, 10, DownsDust, 0.8f, 16f);
		}
	}

	// The stars flaring on his belt before they go.
	protected internal override void DrawDeathLight(BossDeath death, CanvasItem canvas, float t)
	{
		if (falling == null || t > 0.4f)
			return;
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		float flare = BossDeath.Span(t, 0f, 0.08f) * (1f - BossDeath.Span(t, 0.2f, 0.4f));
		foreach (Vector2 star in BeltArt)
			BossDeath.Disc(canvas, death.PieceToLocal(falling, star), 7f, 7f, new Color(arcane.Hot, 0.85f * flare));
	}
}
