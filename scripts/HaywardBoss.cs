using Godot;
using TheOneHeMissed.scripts;

// Chapter 2, the Fallow Fields: THE HAYWARD (.ai/campaign.md section 5).
//
// The parish officer who kept the hedges and impounded strays, walking behind a hurdle - a woven
// fence panel - held upright before him. "He keeps his hurdle between you and him. Go round to his
// back."
//
// THE VERB NO OTHER BOSS ASKS: GO ROUND IT. Every spell in this game aims itself, so the only way
// to hit his back is to stand there. While the player is in front of him - within GuardHalfAngle of
// the way his hurdle faces - every hit is cut to GuardedDamageMultiplier; from the side and behind it
// lands in full. He turns to face the player at a limited rate, so circling at a comfortable radius
// outpaces him (the wizard walks 220 px/s: at 150 px that is 84 degrees a second against his 55).
// Elderbark asked the player to stand and deliver; this one asks him to move with intent, and
// teaches the sidestep the Gaoler asks at chapter 5 and the orbit the Archivist asks at 7.
//
// THE COMBO (Eric, 2026-11-06, .ai/in-flight.md 57): a swing, a charge, then his back vulnerable.
//
//   1. THE SWING - a cone in front, driven along the hurdle's facing, so it lands only where he is
//      blocking: the ground that is bad to stand on (your hits are wasted there) is the ground that
//      hurts. He only winds it up at a player in front and in reach, plants, and his facing locks -
//      a cone that kept turning through its tell would have no sideways to step to.
//   2. THE CHARGE - the swing's resolve primes it (AttackTelegraph.Prime, so it chains at once but
//      never skips its own tell): a lane aimed once at the player, a planted tell for the first
//      ChargeTellFraction of its wind-up, then he drives the hurdle down it at ChargeSpeedMultiplier.
//      The lane travels with him and hurts where he passes (TickRunOver), exactly the Gaoler's run.
//      The aim is the one fast turn he has, and it is paid for with the longest tell in the fight.
//   3. SPENT - the charge ends with the hurdle driven into the ground and him hanging on it: for
//      SpentSeconds he cannot turn, walk or attack, and the whole unguarded side is drawn as a hot
//      open arc that takes SpentRearMultiplier. The side-step that dodged the charge leaves the
//      player standing at his back - the combo's answer IS its reward.
//
// HITTING THE SHIELD IS TOLD TWICE (item 57): a cut hit flares the guard arc, stamps a little
// woven-panel mark where the hit arrived, and plays boss_block - wood, not the element - so the
// small number has both a mark and a sound that say WHY.
//
// SLOWED HE TURNS SLOWER, HELD HE CANNOT TURN (item 57): the hurdle comes round at the pace the
// body has (Enemy.PaceShare), so Ice buys circling time and a stagger pins the guard still.
//
// PHASE 2, at half health: he turns faster, and every third opener is a full SWING - a ring all
// round him - so hugging his back is answered too. The ring also comes due early when the player
// has stood behind him in reach for a while, or a player glued to his back would never see it.
//
// Built from the shared primitives only: GroundSlamAttack as a Cone, a Line and a Ring,
// AdjustSteering for the plant and the run, and two shared hooks on Enemy - AdjustIncomingDamage,
// and the hit-origin channel (IncomingHitFrom) item 28 added so a seeking dart that flies round
// the hurdle (MagicMissile.FlankAim, IGuardedBoss) is paid in full for arriving behind it.
// Read with .ai/enemy-behaviour.md, "The ten chapter bosses".
public partial class HaywardBoss : BossEnemy, IGuardedBoss
{
	/// <summary>Half-angle of the front his hurdle covers. Hits are cut while the player is inside it.</summary>
	[Export] public float GuardHalfAngleDegrees { get; set; } = 60f;

	/// <summary>What a hit is worth while the player stands in front of the hurdle.</summary>
	[Export] public float GuardedDamageMultiplier { get; set; } = 0.2f;

	/// <summary>How fast he turns to face the player, degrees a second. The circling player's margin.</summary>
	[Export] public float TurnDegreesPerSecond { get; set; } = 55f;

	/// <summary>The turn rate from phase 2 on.</summary>
	[Export] public float PhaseTwoTurnDegreesPerSecond { get; set; } = 75f;

	/// <summary>Half-angle of the shove. Under the guard's, so the shove lands only where he blocks.</summary>
	[Export] public float ShoveHalfAngleDegrees { get; set; } = 50f;

	/// <summary>Reach of the phase 2 swing, a ring all round him.</summary>
	[Export] public float SwingRadius { get; set; } = 120f;

	/// <summary>In phase 2, every this-many-th attack is the swing.</summary>
	[Export] public int SwingEvery { get; set; } = 3;

	/// <summary>Seconds a player may stand behind him, in reach, before the swing comes due anyway.</summary>
	[Export] public float BackHuggingSeconds { get; set; } = 2.5f;

	/// <summary>Where the fight changes.</summary>
	[Export] public float PhaseTwoHealthFraction { get; set; } = 0.5f;

	// --- The combo's charge and the spent window (.ai/in-flight.md 57) ---------------------------

	/// <summary>The charge's whole wind-up: the planted tell, then the run. The fight's longest.</summary>
	[Export] public float ChargeWindUpSeconds { get; set; } = 1.5f;

	/// <summary>Share of the wind-up spent planted before he runs. ValidateBossPatterns holds it inside 0-1.</summary>
	[Export] public float ChargeTellFraction { get; set; } = 0.45f;

	/// <summary>The run's speed, over his walk. His walk is 70; the wizard's 220, so the lane must be left, not outrun.</summary>
	[Export] public float ChargeSpeedMultiplier { get; set; } = 4.5f;

	/// <summary>The lane's drawn (and hit) length. It travels with him, so this is reach ahead, not total ground.</summary>
	[Export] public float ChargeLaneLength { get; set; } = 300f;

	/// <summary>The lane's half-width: the hurdle is a fence panel, wider than his shoulders.</summary>
	[Export] public float ChargeHalfWidth { get; set; } = 42f;

	/// <summary>Seconds he hangs spent on the driven hurdle after the charge: no turn, no walk, no attack.</summary>
	[Export] public float SpentSeconds { get; set; } = 2.6f;

	/// <summary>What a hit on the open side is worth while he is spent. The combo's payoff window.</summary>
	[Export] public float SpentRearMultiplier { get; set; } = 1.3f;

	// The hurdle's facing, a world direction. Starts toward the player at _Ready and only ever turns
	// at the turn rate.
	private Vector2 guardFacing = Vector2.Right;
	private float turnRate;
	private bool phaseTwo;
	private int attacksSinceSwing;
	private float behindSeconds;
	private bool swingDue;
	private bool swinging;
	// The combo's spine: the swing's resolve sets chargeNext, the charge's wind-up is charging, and
	// its resolve leaves spentRemaining on the clock.
	private bool chargeNext;
	private bool charging;
	private bool chargeHitLanded;
	private float spentRemaining;
	// Runs while he is spent, for the open arc's pulse.
	private float spentClock;
	// Seconds left on the brief flare drawn along the guarded front when a hit is cut. The read that
	// tells a player WHY the numbers went small.
	private float blockFlash;
	// The little woven-panel mark stamped where a cut hit arrived (item 57's shield mark): its clock
	// and the world angle, boss to arrival, it is drawn at.
	private float blockMarkSeconds;
	private float blockMarkAngle;
	private float guardHalfAngleRadians = Mathf.DegToRad(60f);
	private AnimatedSprite2D? sprite;
	private readonly RandomNumberGenerator damageRng = new();

	// Straw and old wood: the shove's warning. Warm, against the cold of every taken thing's mark.
	private static readonly Color ShoveColor = new(0.94f, 0.68f, 0.26f);
	// The swing is the same verb's other answer, a shade hotter so the ring and the cone read apart.
	private static readonly Color SwingColor = new(1.0f, 0.5f, 0.22f);
	// The charge's lane: the hottest of the three, because it is the beat that really moves.
	private static readonly Color ChargeColor = new(1.0f, 0.42f, 0.18f);
	// The spent window's open side: gold, an offer rather than a warning.
	private static readonly Color OpenColor = new(1.0f, 0.82f, 0.36f);
	private static readonly Color GuardColor = new(0.86f, 0.74f, 0.48f);

	// The drawn guard: a dashed arc on the ground round his feet, spanning the guarded front. The
	// sprite can only face left or right; the arc says exactly where "in front" is. An ellipse,
	// because it lies on the floor in three-quarter view, centred where his boots are (the node is
	// the middle of a 192 px sprite, 92 px above them) - drawn round the node it was a halo.
	private static readonly Vector2 GuardArcCentre = new(0f, 78f);
	private const float GuardArcRadiusX = 104f;
	private const float GuardArcRadiusY = 46f;
	private const int GuardArcDashes = 9;
	private const float BlockFlashSeconds = 0.18f;
	private const float BlockMarkTotalSeconds = 0.35f;
	// How far a body reaches past the lane's half-width for the run-over (DeepWardenBoss's number).
	private const float RunOverBodyRadius = 30f;

	protected override GroundSlamAttack BuildSlam() =>
		new(SlamIntervalSeconds, SlamTelegraphSeconds, SlamRadius, SlamDamage, ShoveColor)
		{
			Shape = TelegraphShape.Cone,
			HalfAngleDegrees = ShoveHalfAngleDegrees,
		};

	/// <summary>
	/// Where he loses his temper (2026-10-31: every boss has at least two phases). He turns faster still,
	/// swings every other attack instead of every third, and comes on quicker - the same two answers,
	/// asked with less time to give them.
	/// </summary>
	[Export] public float PhaseThreeHealthFraction { get; set; } = 0.28f;
	[Export] public float PhaseThreeTurnDegreesPerSecond { get; set; } = 92f;

	protected override float[] BuildPhaseThresholds() => new[] { PhaseTwoHealthFraction, PhaseThreeHealthFraction };

	public override void _Ready()
	{
		base._Ready();
		turnRate = TurnDegreesPerSecond;
		guardHalfAngleRadians = Mathf.DegToRad(Mathf.Clamp(GuardHalfAngleDegrees, 1f, 179f));
		damageRng.Randomize();
		sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		Node2D target = TargetPlayer;
		if (target != null && IsInstanceValid(target))
		{
			Vector2 toPlayer = target.GlobalPosition - GlobalPosition;
			if (toPlayer.LengthSquared() > 0.01f)
				guardFacing = toPlayer.Normalized();
		}
		// The shove must land only where he blocks: a cone wider than the guard would hurt a player
		// who had already stepped out of the front. ValidateBossPatterns holds this and the combo's
		// numbers too; this catches a bad scene tweak in an ordinary run, where the validator is off.
		if (ShoveHalfAngleDegrees >= 90f || ShoveHalfAngleDegrees > GuardHalfAngleDegrees)
			GD.PushError($"HaywardBoss: ShoveHalfAngleDegrees {ShoveHalfAngleDegrees} must be under 90 and within the guard's {GuardHalfAngleDegrees}.");
	}

	protected override void OnPhaseEntered(int phase)
	{
		if (phase >= 2)
		{
			turnRate = PhaseThreeTurnDegreesPerSecond;
			SwingEvery = Mathf.Max(2, SwingEvery - 1);
			Speed *= 1.25f;
			SlamIntervalSeconds *= 0.75f;
			Slam.IntervalSeconds = SlamIntervalSeconds;
			// The temper narrows the combo too: a quicker charge and less of him spent on it. The
			// window never closes entirely - the payoff is the fight's promise, not a phase's.
			ChargeWindUpSeconds = Mathf.Max(1.0f, ChargeWindUpSeconds * 0.85f);
			SpentSeconds = Mathf.Max(1.6f, SpentSeconds * 0.7f);
			SetBaseModulate(new Color(1.0f, 0.72f, 0.60f));
			return;
		}
		phaseTwo = true;
		turnRate = PhaseTwoTurnDegreesPerSecond;
		// Warmer: the change has a cause on screen, as every boss's does (BossEnemy.ApplyEnrage).
		SetBaseModulate(new Color(1.0f, 0.84f, 0.72f));
		// It announces itself: the first attack of the new phase is the new attack.
		swingDue = true;
		Slam.Prime();
	}

	/// <summary>The way the hurdle faces, a world direction. For probes, photographs and the dart's detour.</summary>
	public Vector2 GuardFacing => guardFacing;

	/// <summary>The hurdle is up for the whole fight (IGuardedBoss; the spent window opens the BACK, not the front).</summary>
	public bool GuardActive => !IsDying;

	/// <summary>Half-angle of the guarded front, radians (IGuardedBoss).</summary>
	public float GuardHalfAngleRadians => guardHalfAngleRadians;

	/// <summary>Whether a hit arriving from <paramref name="worldPoint"/> lands on the hurdle (IGuardedBoss).</summary>
	public bool GuardCovers(Vector2 worldPoint)
	{
		Vector2 offset = worldPoint - GlobalPosition;
		// On his centre is behind the hurdle, as standing on him always counted as in front.
		if (offset.LengthSquared() < 1f)
			return true;
		return Mathf.Abs(guardFacing.AngleTo(offset)) <= guardHalfAngleRadians;
	}

	/// <summary>True while the player stands inside the guarded front.</summary>
	public bool PlayerInFront
	{
		get
		{
			Node2D target = TargetPlayer;
			return target != null && IsInstanceValid(target) && GuardCovers(target.GlobalPosition);
		}
	}

	/// <summary>True while he hangs spent on the driven hurdle. For the probe.</summary>
	public bool IsSpent => spentRemaining > 0f;

	// The combo's beats as they happen, for _BossProbe (the Choir's and the dark wizard's pattern):
	// an attack count alone cannot say whether the swing actually chained into its charge.
	public int SwingsBegun { get; private set; }
	public int ChargesBegun { get; private set; }
	public int RingSwingsBegun { get; private set; }
	public int SpentWindowsEntered { get; private set; }
	/// <summary>Hits the hurdle cut (each stamped a mark and knocked), for the probe.</summary>
	public int HitsTurned { get; private set; }

	// THE HURDLE. A hit whose dealer named where it arrived from (a dart that flew round,
	// Enemy.IncomingHitFrom) is judged by its arrival, so reaching the open side counts however the
	// player stands (.ai/in-flight.md 28); every other hit is judged by where the player stands,
	// which is the only choice an auto-aimed spell gives him. Rounded by chance, not to nearest, as
	// Enemy.ScaleOutgoingDamage is - a 3-damage hit at x0.2 must average 0.6, not round to 1 every
	// time and make the hurdle a 33% cut against small hits.
	protected override int AdjustIncomingDamage(int amount)
	{
		if (amount <= 0)
			return amount;

		Vector2? from = IncomingHitFrom;
		bool onGuard = from is Vector2 origin ? GuardCovers(origin) : PlayerInFront;
		if (!onGuard)
		{
			// Spent (the combo's third beat): the open side pays extra, so the window the charge
			// bought is a burst window rather than only a safe one.
			return spentRemaining > 0f && SpentRearMultiplier > 1f
				? RoundByChance(amount * SpentRearMultiplier)
				: amount;
		}

		MarkBlockedHit(from);
		return RoundByChance(amount * Mathf.Clamp(GuardedDamageMultiplier, 0f, 1f));
	}

	private int RoundByChance(float scaled)
	{
		int whole = Mathf.FloorToInt(scaled);
		return damageRng.Randf() < scaled - whole ? whole + 1 : whole;
	}

	// A cut hit is told twice over the number (item 57): the guard arc flares with a woven-panel
	// mark stamped where the hit arrived, and boss_block plays - wood, not the element.
	private void MarkBlockedHit(Vector2? from)
	{
		blockFlash = BlockFlashSeconds;
		Vector2 at = from ?? (TargetPlayer is Node2D target && IsInstanceValid(target)
			? target.GlobalPosition
			: GlobalPosition + guardFacing);
		Vector2 toHit = at - GlobalPosition;
		blockMarkAngle = toHit.LengthSquared() > 0.01f ? toHit.Angle() : guardFacing.Angle();
		blockMarkSeconds = BlockMarkTotalSeconds;
		HitsTurned++;
		SfxPlayer.BossBlock(GlobalPosition);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		base._PhysicsProcess(delta);
		if (blockFlash > 0f)
			blockFlash -= dt;
		if (blockMarkSeconds > 0f)
			blockMarkSeconds -= dt;
		if (spentRemaining > 0f)
		{
			spentRemaining -= dt;
			spentClock += dt;
		}
		TickRunOver();
		TickTurn(dt);
		TickBackHugging(dt);
		// The sprite can only face left or right, and Enemy faces it toward the player the moment he
		// crosses. That would show the hurdle toward the player while the guard still faced away, so
		// the sprite follows the guard instead - it turns when he has turned.
		if (sprite != null)
			sprite.FlipH = SpriteFacesRightByDefault ? guardFacing.X < 0f : guardFacing.X > 0f;
	}

	private void TickTurn(float dt)
	{
		// Spent: the hurdle is driven into the ground and he hangs on it - he cannot bring it round,
		// which is the whole of what the window pays.
		if (spentRemaining > 0f)
			return;
		// Locked through a wind-up: the shove goes where the hurdle pointed when it began.
		if (Slam != null && Slam.IsWindingUp)
			return;
		Node2D target = TargetPlayer;
		if (target == null || !IsInstanceValid(target))
			return;
		Vector2 toPlayer = target.GlobalPosition - GlobalPosition;
		if (toPlayer.LengthSquared() < 1f)
			return;
		float diff = guardFacing.AngleTo(toPlayer);
		// At the pace the body has (item 57): slowed he turns slower, held or frozen he cannot turn
		// at all. PaceShare is the same number his walk already obeys, so the two never disagree.
		float step = Mathf.DegToRad(turnRate) * PaceShare * dt;
		guardFacing = guardFacing.Rotated(Mathf.Clamp(diff, -step, step)).Normalized();
	}

	// The charge hurts where it passes, not only where it stops (the Gaoler's rule): a player it ran
	// through would be behind it by the time the telegraph resolves.
	private void TickRunOver()
	{
		if (!charging || chargeHitLanded || Slam == null || !Slam.IsWindingUp || Slam.WindUpProgress < ChargeTellFraction)
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

	// Phase 2 only: standing in reach behind him brings the swing due, so the answer to "go round"
	// is never "and then stand there forever". Never mid-combo, and never while he is spent: the
	// spent window is the combo's promised payoff, and a ring inside it would make the promise a lie.
	private void TickBackHugging(float dt)
	{
		if (!phaseTwo || swingDue || chargeNext || charging || spentRemaining > 0f)
			return;
		Node2D target = TargetPlayer;
		if (target == null || !IsInstanceValid(target))
			return;
		bool behindInReach = !PlayerInFront && GlobalPosition.DistanceTo(target.GlobalPosition) <= SwingRadius;
		behindSeconds = behindInReach ? behindSeconds + dt : Mathf.Max(0f, behindSeconds - dt);
		if (behindSeconds >= BackHuggingSeconds)
			swingDue = true;
	}

	protected override bool WantsToAttack(Node2D target, float distanceToPlayer)
	{
		// Spent: he hangs on the hurdle and asks nothing. The window is the player's.
		if (spentRemaining > 0f)
			return false;
		// The combo chains: the primed charge follows the swing wherever the player stepped.
		if (chargeNext)
			return true;
		if (swingDue)
			return distanceToPlayer <= SwingRadius;
		// The shove: only at a player in front and in reach of the hurdle.
		return PlayerInFront && distanceToPlayer <= SlamRadius;
	}

	protected override void OnAttackStarted(Node2D target)
	{
		PlayAttackAnimation();
		if (chargeNext)
		{
			chargeNext = false;
			charging = true;
			chargeHitLanded = false;
			swinging = false;
			ChargesBegun++;
			Slam.Shape = TelegraphShape.Line;
			Slam.Radius = ChargeLaneLength;
			Slam.HalfWidth = ChargeHalfWidth;
			Slam.WarningColor = ChargeColor;
			Slam.WindUpSeconds = ChargeWindUpSeconds;
			if (target != null && IsInstanceValid(target))
				Slam.AimAt(GlobalPosition, target.GlobalPosition);
			// He swings the hurdle onto the lane as the tell begins: the one fast turn he has, paid
			// for with the fight's longest planted tell, and the guard goes with the panel - whoever
			// steps out of the lane is beside him, off the hurdle.
			guardFacing = Slam.Facing;
			return;
		}
		swinging = swingDue;
		if (swinging)
		{
			RingSwingsBegun++;
			Slam.Shape = TelegraphShape.Ring;
			Slam.Radius = SwingRadius;
			Slam.WarningColor = SwingColor;
			Slam.WindUpSeconds = SlamTelegraphSeconds;
			swingDue = false;
			behindSeconds = 0f;
			attacksSinceSwing = 0;
		}
		else
		{
			SwingsBegun++;
			Slam.Shape = TelegraphShape.Cone;
			Slam.Radius = SlamRadius;
			Slam.HalfAngleDegrees = ShoveHalfAngleDegrees;
			Slam.WarningColor = ShoveColor;
			Slam.WindUpSeconds = SlamTelegraphSeconds;
			// Along the hurdle, not at the player: it is the hurdle being driven forward.
			Slam.Facing = guardFacing;
			if (phaseTwo && ++attacksSinceSwing >= SwingEvery - 1)
				swingDue = true;
		}
	}

	protected override void OnAttackResolved(Node2D target)
	{
		if (charging)
		{
			// The run already paid where it passed; the resolve only pays whoever stands in the
			// lane's last frame, exactly once (the Warden of the Deep's rule).
			if (!chargeHitLanded)
				base.OnAttackResolved(target);
			charging = false;
			// Beat three: the hurdle is in the ground and his back is open.
			spentRemaining = SpentSeconds;
			spentClock = 0f;
			behindSeconds = 0f;
			SpentWindowsEntered++;
			return;
		}

		base.OnAttackResolved(target);
		// The swing's resolve primes the charge - the chain skips only the wait, never a tell
		// (AttackTelegraph.Prime). A ring is the back-hugging answer, not a combo opener.
		if (!swinging)
		{
			chargeNext = true;
			Slam.Prime();
		}
	}

	// Planted for the cone's and the ring's tells (the base rule); the charge instead plants for its
	// own tell and then runs the lane, and spent he stands on the driven hurdle.
	protected override bool PlantsDuringWindUp => !charging;

	protected override Vector2 AdjustSteering(Vector2 chaseDirection, float distanceToPlayer)
	{
		if (spentRemaining > 0f)
			return Vector2.Zero;
		if (charging && Slam != null && Slam.IsWindingUp)
		{
			// Plant for the tell, then drive the hurdle down the lane. Not re-aimed: a lane that
			// curved could not be stepped out of, and stepping out of it is the answer.
			return Slam.WindUpProgress < ChargeTellFraction
				? Vector2.Zero
				: Slam.Facing * ChargeSpeedMultiplier;
		}
		return base.AdjustSteering(chaseDirection, distanceToPlayer);
	}

	// The guard is an arc on the ground, under every body, with the slam over it (Enemy.DrawGroundTells).
	protected override void DrawGroundTells(CanvasItem floor)
	{
		DrawGuard(floor);
		DrawSpentOpening(floor);
		DrawBlockMark(floor);
		base.DrawGroundTells(floor);
	}

	// Spent: the open side drawn as its own arc - gold, pulsing, spanning everything the hurdle does
	// not cover - so the weak spot is SEEN, not inferred from small numbers stopping.
	private void DrawSpentOpening(CanvasItem floor)
	{
		if (IsDying || spentRemaining <= 0f)
			return;
		// Breathe while the window is open; fold away over its last half second.
		float pulse = 0.72f + 0.28f * Mathf.Sin(spentClock * 9f);
		float closing = Mathf.Clamp(spentRemaining / 0.5f, 0f, 1f);
		var color = new Color(OpenColor, 0.55f * pulse * closing);
		float rearAngle = guardFacing.Angle() + Mathf.Pi;
		float rearHalf = Mathf.Pi - guardHalfAngleRadians;
		float start = rearAngle - rearHalf;
		float span = rearHalf * 2f;
		const int dashes = 11;
		const int stepsPerDash = 4;
		for (int k = 0; k < dashes; k++)
		{
			float a0 = start + k * span / dashes;
			float a1 = a0 + span / dashes * 0.6f;
			for (int i = 0; i < stepsPerDash; i++)
			{
				Vector2 p0 = OnGround(a0 + (a1 - a0) * i / stepsPerDash, 1.1f);
				Vector2 p1 = OnGround(a0 + (a1 - a0) * (i + 1) / stepsPerDash, 1.1f);
				floor.DrawLine(p0, p1, color, 4f);
			}
		}
		// Two chevrons pointing in at the dead centre of his back: "here".
		Vector2 mid = OnGround(rearAngle, 1.1f);
		Vector2 inward = (GuardArcCentre - mid).Normalized();
		Vector2 side = new(-inward.Y, inward.X);
		for (int c = 0; c < 2; c++)
		{
			Vector2 tip = mid + inward * (10f + 12f * c);
			floor.DrawLine(tip - inward * 8f + side * 7f, tip, color, 3f);
			floor.DrawLine(tip - inward * 8f - side * 7f, tip, color, 3f);
		}
	}

	// The shield mark (item 57): a little woven panel stamped where a cut hit arrived, popping as it
	// fades, so each turned hit has a mark as well as a sound.
	private void DrawBlockMark(CanvasItem floor)
	{
		if (IsDying || blockMarkSeconds <= 0f)
			return;
		float life = Mathf.Clamp(blockMarkSeconds / BlockMarkTotalSeconds, 0f, 1f);
		Vector2 at = OnGround(blockMarkAngle, 1.04f) + new Vector2(0f, -14f);
		var ink = new Color(1f, 0.92f, 0.62f, 0.95f * life);
		float w = 8f + 3f * (1f - life);
		float h = 6f + 2f * (1f - life);
		// The panel's frame, and two stakes through it: the hurdle in miniature.
		floor.DrawRect(new Rect2(at - new Vector2(w, h), new Vector2(w * 2f, h * 2f)), ink, false, 2f);
		floor.DrawLine(at + new Vector2(-w * 0.35f, -h), at + new Vector2(-w * 0.35f, h), ink, 2f);
		floor.DrawLine(at + new Vector2(w * 0.35f, -h), at + new Vector2(w * 0.35f, h), ink, 2f);
	}

	// The guarded front as a dashed arc on the ground: faint at rest, bright for a moment when it
	// turns a hit. Drawn in local space (enemies never rotate, so a world direction is a local one),
	// and before the slam's warning so the warning is always on top of it.
	private void DrawGuard(CanvasItem floor)
	{
		if (IsDying)
			return;
		float flash = Mathf.Clamp(blockFlash / BlockFlashSeconds, 0f, 1f);
		var color = new Color(GuardColor, 0.30f + 0.5f * flash);
		float width = 3f + 2f * flash;
		float start = guardFacing.Angle() - guardHalfAngleRadians;
		float span = guardHalfAngleRadians * 2f;
		const int stepsPerDash = 4;
		for (int k = 0; k < GuardArcDashes; k++)
		{
			float a0 = start + k * span / GuardArcDashes;
			float a1 = a0 + span / GuardArcDashes * 0.62f;
			for (int i = 0; i < stepsPerDash; i++)
			{
				Vector2 p0 = OnGround(a0 + (a1 - a0) * i / stepsPerDash, 1f);
				Vector2 p1 = OnGround(a0 + (a1 - a0) * (i + 1) / stepsPerDash, 1f);
				floor.DrawLine(p0, p1, color, width);
			}
		}
		// The two ends, a short tick outward, so the edge of the front - the line to get across -
		// is the clearest part of it.
		for (int side = -1; side <= 1; side += 2)
		{
			float a = guardFacing.Angle() + side * guardHalfAngleRadians;
			floor.DrawLine(OnGround(a, 0.86f), OnGround(a, 1.14f), color, 3f);
		}
	}

	// A point on the guard's ellipse at world angle a, scaled out by k.
	private static Vector2 OnGround(float a, float k) =>
		GuardArcCentre + new Vector2(Mathf.Cos(a) * GuardArcRadiusX, Mathf.Sin(a) * GuardArcRadiusY) * k;

	// --- The arrival (BossDeath, entrance) ---------------------------------------------------------
	//
	// OUT OF A DUST DEVIL. The stubble lifts: a whirl of straw and chaff spins up off the field, thick
	// enough to hide what is in it; he takes shape inside it; then he drives his hurdle down into the
	// ground with a thud that throws the straw out flat, and the whirl is gone.
	protected internal override float EntranceSeconds => 3.1f;
	// The climax (BossDeath calls it on the roar): straw; the thud of the hurdle (Planted) is his heavy beat.
	protected internal override float EntranceClimaxWeight => 0.6f;
	protected internal override Element EntranceFlareElement => Element.Earth;

	private BossDeath.Piece hayward;
	private const float WhirlUp = 1.2f;
	private const float Planted = 2.35f;
	private static readonly Color Chaff = new(0.86f, 0.74f, 0.46f);

	protected internal override void OnEntranceBegin(BossDeath stage)
	{
		hayward = stage.CutWhole();
		hayward.Color = new Color(1f, 1f, 1f, 0f);
	}

	protected internal override void TickEntrance(BossDeath stage, float t, float dt)
	{
		if (hayward == null)
			return;
		float shape = BossDeath.Span(t, WhirlUp - 0.2f, Planted - 0.4f);
		hayward.Color = new Color(0.55f, 0.5f, 0.4f, shape).Lerp(Colors.White, BossDeath.Span(t, Planted - 0.3f, Planted));
		// Lifted a little in the whirl, then driven down onto the hurdle's foot.
		float lift = 16f * BossDeath.Span(t, WhirlUp, Planted - 0.35f) * (1f - BossDeath.EaseIn(BossDeath.Span(t, Planted - 0.35f, Planted)));
		hayward.Offset = new Vector2(0f, -lift);

		Vector2 foot = stage.Foot;
		for (int k = 0; k < 8; k++)
		{
			// The straw going up in the whirl: thrown nearly straight up, fluttering, a few at a time.
			if (stage.Crossed(0.1f + k * 0.2f))
			{
				stage.EmitAt(new BossDeath.Throw
				{
					Cell = BossDeath.Cell.StrawA, Variants = 3, Angle = 0f, Spread = Mathf.Tau, SpeedMin = 40f, SpeedMax = 90f,
					UpMin = 260f, UpMax = 360f, Gravity = 300f, Terminal = 60f, Drag = 1.2f, Flutter = 14f, FlutterHz = 2.2f,
					Bounce = 0f, Friction = 10f, Life = 2.2f, Fade = 0.5f, Spin = 6f,
				}, 4, foot, 2f, 40f);
			}
		}
		if (stage.Crossed(Planted))
		{
			stage.Impact(0.9f);
			stage.DustRing(foot, 74f, 10, new Color(0.62f, 0.54f, 0.36f, 0.6f), 0.8f, 16f);
			stage.EmitAt(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.StrawA, Variants = 3, SpeedMin = 120f, SpeedMax = 240f, UpMin = 40f, UpMax = 110f,
				Spin = 6f, Bounce = 0.1f, Friction = 8f, Life = 1.4f, Fade = 0.4f,
			}, 14, foot, 4f, 20f);
			stage.EmitAt(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.Withy, SpeedMin = 60f, SpeedMax = 140f, UpMin = 60f, UpMax = 140f,
				Spin = 6f, Bounce = 0.2f, Life = 1.4f, Fade = 0.4f,
			}, 3, foot, 4f, 12f);
		}
	}

	// The whirl itself: a column of chaff turning, drawn on the art grid round and over him, thinning
	// as he takes shape and gone when the hurdle lands.
	protected internal override void DrawEntranceAir(BossDeath stage, CanvasItem canvas, float t)
	{
		float thick = BossDeath.Span(t, 0f, WhirlUp) * (1f - BossDeath.Span(t, Planted - 0.5f, Planted));
		if (thick <= 0f)
			return;
		Vector2 foot = stage.Foot;
		for (int k = 0; k < 70; k++)
		{
			float h = 200f * BossDeath.Hash(83u, k);
			float width = 22f + h * 0.32f;
			float a = Mathf.Tau * BossDeath.Hash(89u, k) + t * (7f + 3f * BossDeath.Hash(97u, k));
			Vector2 at = BossDeath.Snap(foot + new Vector2(Mathf.Cos(a) * width, Mathf.Sin(a) * width * 0.3f - h));
			bool front = Mathf.Sin(a) > 0f;
			var c = new Color(Chaff * (front ? 1f : 0.7f), (front ? 0.9f : 0.6f) * thick);
			canvas.DrawRect(new Rect2(at, new Vector2(BossDeath.Px * 2f, BossDeath.Px)), c);
		}
	}

	// --- The fall (BossDeath) ---------------------------------------------------------------------
	//
	// THE HURDLE GOES DOWN. He was the parish officer behind his woven panel, and the fight was about
	// the panel; so it is the panel that falls first and flattest - toppling away from him onto the
	// stubble, bursting straw and withies where it lands. His hat is knocked off and spins away behind
	// him, his crook drops the other way, and the man himself folds down onto his knees where he
	// stood. What he kept out is let in. Straw and old wood, from cast_fields.py's gold row.
	private BossDeath.Piece deathCrook;
	private BossDeath.Piece deathHat;
	private BossDeath.Piece deathMan;
	private BossDeath.Piece deathHurdle;

	private const float HurdleLands = 0.7f;
	private const float CrookLands = 0.82f;
	private const float HatLands = 0.86f;
	private static readonly Color StubbleDust = new(0.62f, 0.55f, 0.40f, 0.55f);

	protected internal override float DeathSeconds => 2.7f;

	protected internal override void OnDeathBegin(BossDeath death)
	{
		// Four pieces that do not overlap, cut on the drawing (boss_hayward.py): the crook on the left,
		// the hat, the man under it, and the hurdle with the stake tips above it.
		deathCrook = death.Cut(new Vector2I(19, 94), new Rect2I(0, 0, 23, 96));
		deathHat = death.Cut(new Vector2I(42, 12), new Rect2I(26, 2, 32, 13));
		deathMan = death.Cut(new Vector2I(36, 94), new Rect2I(23, 15, 25, 81));
		deathHurdle = death.Cut(new Vector2I(68, 92), new Rect2I(48, 15, 48, 81), new Rect2I(58, 0, 38, 15));
	}

	protected internal override void TickDeath(BossDeath death, float t, float dt)
	{
		if (deathHurdle == null)
			return;

		float s = death.ArtScale.X;
		// The hurdle topples away from him, flat onto the field, with a little bounce.
		float topple = BossDeath.EaseIn(BossDeath.Span(t, 0.1f, HurdleLands));
		float bounce = 0.07f * Mathf.Sin(Mathf.Pi * BossDeath.Span(t, HurdleLands, HurdleLands + 0.2f));
		deathHurdle.Rotation = Mathf.DegToRad(86f) * topple - bounce;

		// His crook drops the other way.
		deathCrook.Rotation = -Mathf.DegToRad(80f) * BossDeath.EaseIn(BossDeath.Span(t, 0.18f, CrookLands));

		// The hat goes up and spins away behind him: once round, and down on its brim, a little tilted.
		float hat = BossDeath.Span(t, 0.04f, HatLands);
		float arc = 4f * hat * (1f - hat);
		deathHat.Offset = new Vector2(-46f * s * hat, (80f * hat * hat - 26f * arc) * s);
		deathHat.Rotation = -Mathf.DegToRad(375f) * BossDeath.EaseOut(hat);

		// And he folds down onto his knees, slumping toward where his hurdle was.
		float fold = BossDeath.EaseInOut(BossDeath.Span(t, 0.3f, 1.1f));
		deathMan.Sink(death, 26f * fold);
		deathMan.Rotation = Mathf.DegToRad(8f) * fold;

		float dim = BossDeath.Span(t, 1.05f, 1.75f);
		var dusk = new Color(1f - 0.5f * dim, 1f - 0.5f * dim, 1f - 0.4f * dim, 1f);
		deathMan.Color = new Color(dusk, 1f - BossDeath.Span(t, 1.6f, 2.2f));
		var gone = new Color(dusk, 1f - BossDeath.Span(t, 1.9f, 2.5f));
		deathHurdle.Color = gone;
		deathCrook.Color = gone;
		deathHat.Color = gone;

		if (death.Crossed(0.02f))
		{
			// The last blow, through the weave.
			death.Emit(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.StrawA, Variants = 3, Tinted = true, SpeedMin = 40f, SpeedMax = 150f,
				UpMin = 60f, UpMax = 180f, Gravity = 520f, Drag = 0.8f, Bounce = 0.1f, Friction = 8f, Spin = 6f,
				Life = DeathSeconds - 0.05f, Fade = 0.4f,
			}, 10, new Vector2(70f, 52f), new Vector2(14f, 28f));
		}

		if (death.Crossed(HurdleLands))
		{
			death.Impact(0.6f);
			// Flat on the stubble: the weave bursts along its whole length.
			for (int k = 0; k < 4; k++)
			{
				Vector2 along = death.PieceToLocal(deathHurdle, new Vector2(68f, 84f - k * 20f));
				var ground = new Vector2(along.X, death.Foot.Y - 4f);
				death.EmitAt(new BossDeath.Throw
				{
					Cell = BossDeath.Cell.StrawA, Variants = 3, Tinted = true, SpeedMin = 30f, SpeedMax = 140f,
					UpMin = 70f, UpMax = 190f, Gravity = 560f, Drag = 0.8f, Bounce = 0.1f, Friction = 8f, Spin = 6f,
					Life = DeathSeconds - HurdleLands, Fade = 0.4f,
				}, 5, ground, 6f, 14f);
				death.PuffAt(ground, 6f, 22f, 0.6f, StubbleDust, 12f, k * 0.03f);
			}
			Vector2 mid = death.PieceToLocal(deathHurdle, new Vector2(68f, 50f));
			death.EmitAt(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.Withy, Tinted = true, SpeedMin = 40f, SpeedMax = 120f, UpMin = 60f, UpMax = 140f,
				Bounce = 0.2f, Friction = 7f, Spin = 5f, Life = DeathSeconds - HurdleLands, Fade = 0.4f,
			}, 4, new Vector2(mid.X, death.Foot.Y - 4f), 6f, 30f);
		}

		if (death.Crossed(CrookLands))
		{
			Vector2 tip = death.PieceToLocal(deathCrook, new Vector2(14f, 10f));
			death.DustRing(new Vector2(tip.X, death.Foot.Y - 4f), 22f, 4, StubbleDust, 0.5f, 10f);
		}
		if (death.Crossed(HatLands))
		{
			Vector2 brim = death.PieceToLocal(deathHat, new Vector2(42f, 12f));
			death.PuffAt(brim, 6f, 18f, 0.45f, StubbleDust, 6f);
		}
	}
}
