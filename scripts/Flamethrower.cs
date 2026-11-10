using Godot;
using System;

namespace TheOneHeMissed.scripts;

// Cinderbreath: a sustained jet of fire, thrown as individual tongues.
//
// This node no longer draws or damages anything. It is an emitter: while it is lit it throws a
// FlamePuff every EmitInterval into a cone, and the puffs carry the damage, the reach and the art.
//
// WHY IT CHANGED. The first version drew the whole cone in _Draw as three shaded polygons and
// tested that cone analytically. Both halves were correct and the result still did not read as
// fire: a filled wedge has nothing in it for the eye to track, so it looked like a lit area rather
// than something being poured. Splitting it into tongues costs a sprite sheet and buys the thing
// the spell is actually for - you can see the fire leave the staff, travel, and go out.
//
// WHAT SURVIVED, because it was the good part:
//   - The duty cycle. Lit for BurnSeconds, dark for the spell's cooldown. An always-on jet is an
//     aura, and an aura is the always-on passive the boon rework removed.
//   - Re-aiming on an interval rather than per frame. Ten times a second is faster than anything
//     can walk out of a 55 degree arc, and it is one enemy-group scan instead of sixty.
//   - Living as a persistent child of the Player, so the nozzle travels with the caster.
//
// The puffs themselves are parented to the arena, not to this node - see FlamePuff.
public partial class Flamethrower : Node2D
{
	[Export] public PackedScene FlamePuffScene { get; set; }

	[Export] public SpellData SpellData { get; set; }
	[Export] public int CurrentLevel { get; set; } = 1;

	// Short. A flamethrower that outranged a bolt would simply be a better bolt; the trade is that
	// it covers a whole arc but only just past arm's reach, so using it means letting them close.
	[Export] public float BaseRange { get; set; } = 170f;
	[Export] public float ConeAngleDegrees { get; set; } = 55f;

	[Export] public float BurnSeconds { get; set; } = 1.5f;

	// How often a tongue leaves the nozzle. Fast enough to read as a continuous jet, slow enough
	// that a burn is about sixteen puffs rather than ninety - this is the spell's whole per-frame
	// cost, and CLAUDE.md is explicit about not allocating per frame in swarm-heavy effects.
	[Export] public float EmitInterval { get; set; } = 0.09f;

	// Each tongue is thrown at a slightly different angle and speed. Without the jitter every puff
	// follows the same line and the cone collapses into a single stuttering arrow.
	[Export] public float SpeedJitter { get; set; } = 0.18f;

	[Export] public float AimRefreshSeconds { get; set; } = 0.1f;

	public float DamageMultiplier { get; set; } = 1.0f;
	public float AreaMultiplier { get; set; } = 1.0f;
	public float CooldownMultiplier { get; set; } = 1.0f;
	public Player PlayerRef;

	private float range = 170f;
	private int damagePerPuff = 4;
	private float rechargeSeconds = 1.4f;
	// What the evolutions buy a jet (it took the default archetype's words for a bolt and read none of
	// them but damage and cooldown). Area is how WIDE each tongue burns - its hit radius, and its sprite
	// with it, so it is drawn as wide as it hurts - and never how far: a flamethrower that outranged a
	// bolt would be a better bolt (see BaseRange). Speed is how fast tongues leave the nozzle. Count is
	// how many tongues each throw sends, at their own jittered angles. A tongue already burns through
	// everything it touches, so there is no pierce to buy.
	private float tongueWidth = 1f;
	private float burnSeconds = 1.5f;
	private float throwRate = 1f;
	private int tonguesPerThrow = 1;
	private const int MaxTonguesPerThrow = 4;
	// An element path (Hellbreath) repaints the fire in that element's ramp. The puffs and the ground
	// are parented to the arena, so the evolution tint Player puts on this emitter never reached them:
	// each tongue takes its ramp from the spell (FlamePuff.MaterialFor), and this repaint of the crater
	// fire's sprite is handed to the ground. Null while the spell has not branched.
	private ShaderMaterial pathMaterial;

	// One aim per tongue in a throw (2026-10-28: "cinderbreath ascension that spreads 3 tongues should
	// all target enemies in different directions"). They all went into the one cone at the nearest enemy,
	// so three tongues were one thicker jet. Now each is its own jet: the first at the nearest enemy, each
	// other at the nearest enemy that is not another jet's and lies at least JetSeparationDegrees from
	// every other jet's bearing, and a jet with none is fanned evenly round from the first - opposite it,
	// for two. A single tongue aims as it always did. Since 2026-11-10 the second jet is Backdraft's (it
	// was Fire Fan's three).
	private readonly float[] aims = new float[MaxTonguesPerThrow];
	private readonly Node2D[] aimTargets = new Node2D[MaxTonguesPerThrow];
	// Each jet re-aims on its own clock (Eric, 2026-11-10: "they should never face the same direction /
	// target the same enemy - we want to feel like there are two different flames around him"), the
	// clocks staggered a share of AimRefreshSeconds apart, so the two never swing together; and the
	// moment the first jet's swing brings it within JetSeparationDegrees of another, that one re-aims at
	// once rather than waiting its beat. The jets can never line up.
	private readonly float[] jetAimSeconds = new float[MaxTonguesPerThrow];
	// How far a second jet will look for its own enemy: a little past the jet's reach.
	private const float SpreadSearchRangeFactor = 2.2f;
	// How far apart two jets always point: a right angle, so two flames read as two (the cone itself is
	// 55 degrees, and two cones one cone apart read as one fat jet).
	public const float JetSeparationDegrees = 90f;

	// EVERY CARD ITS OWN (.ai/spell-roster.md section 2e, 2026-10-29: "yes lets make all mutations more
	// unique per spell"). Each milestone changes what the jet DOES where you can see it; the numbers ride
	// along. The ids are the archetype's, kept so the Pyromancer's Apotheosis path (Backdraft, then
	// Dragon's Breath) and every save's choices carry over; the cards in SpellEvolutionCatalog say what
	// each does with these numbers, which are read from here and from nowhere else.
	//
	// Backdraft: two flames (Eric, 2026-11-10: "update cinderbreath's 'backdraft' so it produces two flames"),
	// the second on an enemy of its own at least JetSeparationDegrees from the first. The jet count is the
	// card's ProjectileCountBonus and the share each flame burns at its DamageMultiplier (SpellEvolutionCatalog);
	// this id is named so the card and the Apotheosis path point at one place. It was one jet whose far end
	// burst for 3x as each burn went out (CinderGround's burst went with it).
	public const string BackdraftId = "cinderbreath_amplified_impact";
	// Ember Wake: every second tongue drops an ember where it goes out, across the swept cone.
	public const string EmberWakeId = "cinderbreath_hyper_acceleration";
	public const int EmberEvery = 2;
	public const float EmberRadius = 20f;
	public const float EmberSeconds = 2f;
	public const float EmberDamageShare = 0.5f;
	// Dragon's Breath: the jet reaches farther - 75%, not the proposal's double, which at level 8 would
	// be 480 against a bolt's 450 (BaseRange's rule) - and where a burn ends a wall of fire stands across
	// its path.
	public const string DragonsBreathId = "cinderbreath_titan_cataclysm";
	public const float DragonReach = 1.75f;
	public const float WallSeconds = 3f;
	public const float WallDamageShare = 1f;
	public const float WallHalfThickness = 16f;
	// The wall's half-length as a share of the reach: the cone's own chord where the wall stands.
	private const float WallHalfLengthShare = 0.4f;
	private const float WallAtShare = 0.85f;
	// Fire Fan: ONE broad flame swept back and forth across a wide arc in front of him (Eric, 2026-11-10:
	// reworked "so it never reads as Backdraft's second flame"; it was three jets, each on its own enemy).
	// Every jet sweeps - FanSweepHalfDegrees either side of its own aim, there and back once every
	// FanSweepSeconds - and its tongues scatter over FanScatterShare of the cone rather than all of it, so
	// what the eye follows is one flame moving, not a wider wedge. All jets sweep on the one clock, so
	// Backdraft's two keep their right angle. Its breadth is the card's AreaMultiplier (a tongue's width).
	public const string FireFanId = "cinderbreath_infinite_horizon";
	public const float FanSweepHalfDegrees = 60f;
	public const float FanSweepSeconds = 1.3f;
	public const float FanScatterShare = 0.4f;

	private bool emberWake;
	private bool dragonsBreath;
	private bool fireFan;
	private float sweepSeconds;
	private int tongueCounter;
	private CinderGround cinderGround;

	private bool burning = false;
	private float stateSeconds = 0f;
	private float emitSeconds = 0f;

	private readonly RandomNumberGenerator rng = new();

	public override void _Ready()
	{
		rng.Randomize();
		RefreshComputedStats();

		// Starts lit. A starter spell that opens the run with a second and a half of nothing reads
		// as broken rather than as recharging.
		burning = true;
		stateSeconds = 0f;
		emitSeconds = EmitInterval;
		StaggerAimClocks();
		PlayIgnitionCue();
	}

	// One cast cue per burn, on the frame the jet lights. The emitter is persistent, so the fire
	// loop's cue only ever reached its creation and every later burn started in silence. Per burn
	// rather than per puff: sixteen cues a burn is a buzz, not a cast. Creation plays it from
	// _Ready and the fire loop stays quiet for that frame, so the first burn is heard once.
	private void PlayIgnitionCue()
	{
		SfxPlayer.Cast(SfxCatalog.DominantElement(SpellData), GlobalPosition);
	}

	public void SetSpellLevel(int level)
	{
		CurrentLevel = Math.Max(1, level);
		RefreshComputedStats();
	}

	private void RefreshComputedStats()
	{
		if (SpellData == null)
		{
			range = BaseRange * AreaMultiplier;
			return;
		}

		damagePerPuff = Math.Max(1, Mathf.RoundToInt(SpellData.GetDamageAtLevel(CurrentLevel) * DamageMultiplier));

		float spellRange = SpellData.GetRangeAtLevel(CurrentLevel);
		range = MathF.Max(40f, (spellRange > 0f ? spellRange : BaseRange) * AreaMultiplier);

		rechargeSeconds = MathF.Max(0.2f, SpellData.GetCooldownAtLevel(CurrentLevel) * CooldownMultiplier);

		// With the level table's Area line (a fraction), and Zone Duration keeps the jet lit longer: the
		// Area and Duration tags' level-ups, shown on their cards and read by nothing until 2026-10-26.
		tongueWidth = MathF.Max(0.25f, SpellData.GetAreaMultiplierAtLevel(CurrentLevel)
			* (1f + SpellData.GetEffectValueAtLevel(SpellEffect.AreaSize, CurrentLevel)));
		burnSeconds = BurnSeconds + SpellData.GetEffectValueAtLevel(SpellEffect.ZoneDuration, CurrentLevel);
		throwRate = MathF.Max(0.25f, SpellData.GetSpeedMultiplier());
		int jetsBefore = tonguesPerThrow;
		tonguesPerThrow = Math.Clamp(SpellData.GetProjectileCountAtLevel(CurrentLevel), 1, MaxTonguesPerThrow);
		// A jet gained mid-burn (Backdraft taken while the jet is lit) is aimed now, not left pointing
		// east until its own clock comes round.
		if (tonguesPerThrow != jetsBefore && IsInsideTree())
		{
			RefreshAim();
			StaggerAimClocks();
		}

		// Kept current here because the emitter outlives the level 4 pick that branches it: the fire
		// loop refreshes it every cooldown.
		pathMaterial = SpellData.GetBranchElement() is Element branch ? ElementRampMaterial.For(branch) : null;

		emberWake = SpellData.HasEvolution(EmberWakeId);
		dragonsBreath = SpellData.HasEvolution(DragonsBreathId);
		fireFan = SpellData.HasEvolution(FireFanId);
		if (dragonsBreath)
			range *= DragonReach;
		if (cinderGround != null && IsInstanceValid(cinderGround))
			cinderGround.PathMaterial = pathMaterial;
	}

	// The ground the jet's fire is left on, made the first time a card leaves any. Parented to the arena,
	// so it stays where it fell; freed with the emitter.
	private CinderGround EnsureGround()
	{
		if (cinderGround != null && IsInstanceValid(cinderGround))
			return cinderGround;
		if (PlayerRef?.GetParent() is not Node2D arena)
			return null;
		cinderGround = new CinderGround { PlayerRef = PlayerRef, SpellData = SpellData };
		arena.AddChild(Player.TagSpell(cinderGround));
		cinderGround.PathMaterial = pathMaterial;
		return cinderGround;
	}

	public override void _ExitTree()
	{
		if (cinderGround != null && IsInstanceValid(cinderGround))
			cinderGround.QueueFree();
		cinderGround = null;
	}

	// The moment a burn goes out: Dragon's Breath's wall, at the tip of every jet, across the way that jet
	// is pointing as it goes out (a Fire Fan's sweep included).
	private void BurnOut()
	{
		if (!dragonsBreath)
			return;
		CinderGround ground = EnsureGround();
		if (ground == null)
			return;
		for (int t = 0; t < tonguesPerThrow; t++)
		{
			Vector2 dir = Vector2.FromAngle(JetAim(t));
			Vector2 centre = GlobalPosition + dir * range * WallAtShare;
			Vector2 half = dir.Orthogonal() * range * WallHalfLengthShare;
			ground.RaiseWall(centre - half, centre + half, WallHalfThickness * MathF.Sqrt(tongueWidth * AreaMultiplier),
				WallSeconds, Math.Max(1, Mathf.RoundToInt(damagePerPuff * WallDamageShare)));
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		stateSeconds += dt;

		if (!burning)
		{
			if (stateSeconds >= rechargeSeconds)
			{
				burning = true;
				stateSeconds = 0f;
				// Throw on the frame it lights rather than one interval later, so the first thing
				// the player sees is fire and not a pause.
				emitSeconds = EmitInterval;
				RefreshAim();
				StaggerAimClocks();
				PlayIgnitionCue();
			}
			return;
		}

		for (int t = 0; t < tonguesPerThrow; t++)
		{
			jetAimSeconds[t] += dt;
			if (jetAimSeconds[t] < AimRefreshSeconds)
				continue;
			jetAimSeconds[t] = 0f;
			if (t == 0)
			{
				AimFirstJet();
				KeepJetsApart();
			}
			else
			{
				AimOtherJet(t, tonguesPerThrow);
			}
		}

		// The sweep's clock runs only while the jet is lit, so each burn takes up the sweep where the last
		// left it rather than snapping back to the middle.
		if (fireFan)
			sweepSeconds += dt;

		emitSeconds += dt;
		if (emitSeconds >= EmitInterval / throwRate)
		{
			emitSeconds = 0f;
			for (int t = 0; t < tonguesPerThrow; t++)
				ThrowOnePuff(JetAim(t));
		}

		if (stateSeconds >= burnSeconds)
		{
			burning = false;
			stateSeconds = 0f;
			BurnOut();
		}
	}

	// Where jet t is pointing now: its aim, swung by the Fire Fan's sweep. The sweep is one sine on one
	// clock for every jet, so two jets swing together and stay their right angle apart.
	private float JetAim(int t)
	{
		float aim = aims[t];
		if (fireFan)
			aim += Mathf.DegToRad(FanSweepHalfDegrees) * MathF.Sin(Mathf.Tau * sweepSeconds / FanSweepSeconds);
		return aim;
	}

	// Each jet's next re-aim a share of AimRefreshSeconds after the one before it, so two jets never
	// swing on the same frame.
	private void StaggerAimClocks()
	{
		for (int t = 0; t < MaxTonguesPerThrow; t++)
			jetAimSeconds[t] = tonguesPerThrow > 1 ? AimRefreshSeconds * t / tonguesPerThrow : 0f;
	}

	// Every jet at once, as a burn lights: the first, then each other against those already aimed.
	private void RefreshAim()
	{
		AimFirstJet();
		for (int t = 1; t < tonguesPerThrow; t++)
			AimOtherJet(t, t);
	}

	// The first jet: the nearest enemy, as the single jet always aimed.
	private void AimFirstJet()
	{
		Node2D nearest = null;
		float best = float.MaxValue;

		foreach (Node node in EnemyRegistry.Snapshot)
		{
			if (node is not Node2D enemy || !IsInstanceValid(node))
				continue;

			float dist = GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
			if (dist < best)
			{
				best = dist;
				nearest = enemy;
			}
		}

		// No target: keep pointing wherever it last pointed. Snapping back to a default direction
		// makes the jet twitch every time the last enemy in range dies.
		if (nearest != null)
		{
			Vector2 toTarget = nearest.GlobalPosition - GlobalPosition;
			if (toTarget.LengthSquared() > 0.0001f)
				Rotation = toTarget.Angle();
		}
		aims[0] = Rotation;
		aimTargets[0] = nearest;
	}

	// The first jet has just swung: any other it has come within JetSeparationDegrees of, or whose enemy it
	// has taken, re-aims now rather than on its own beat.
	private void KeepJetsApart()
	{
		float minSeparation = Mathf.DegToRad(JetSeparationDegrees);
		for (int t = 1; t < tonguesPerThrow; t++)
		{
			bool sameEnemy = aimTargets[t] != null && ReferenceEquals(aimTargets[t], aimTargets[0]);
			if (sameEnemy || MathF.Abs(Mathf.AngleDifference(aims[0], aims[t])) < minSeparation)
				AimOtherJet(t, tonguesPerThrow);
		}
	}

	// Jet t takes the nearest enemy, within SpreadSearchRangeFactor of the reach, that no other jet is on
	// and whose bearing is at least JetSeparationDegrees from every other jet below <paramref name="upTo"/>
	// (those already aimed this pass); with none it is fanned evenly round from the first - opposite it,
	// for two. A pass over the swarm, into fixed arrays - nothing allocated.
	private void AimOtherJet(int t, int upTo)
	{
		float minSeparation = Mathf.DegToRad(JetSeparationDegrees);
		float searchSq = range * SpreadSearchRangeFactor * range * SpreadSearchRangeFactor;
		Node2D pick = null;
		float best = float.MaxValue;
		float pickAngle = 0f;
		foreach (Node node in EnemyRegistry.Snapshot)
		{
			if (node is not Node2D enemy || !IsInstanceValid(node))
				continue;
			Vector2 to = enemy.GlobalPosition - GlobalPosition;
			float dist = to.LengthSquared();
			if (dist >= best || dist > searchSq || dist < 0.0001f)
				continue;
			float angle = to.Angle();
			bool apart = true;
			for (int k = 0; k < upTo && apart; k++)
			{
				if (k == t)
					continue;
				apart = !ReferenceEquals(aimTargets[k], enemy)
					&& MathF.Abs(Mathf.AngleDifference(aims[k], angle)) >= minSeparation;
			}
			if (!apart)
				continue;
			best = dist;
			pick = enemy;
			pickAngle = angle;
		}
		aimTargets[t] = pick;
		aims[t] = pick != null ? pickAngle : aims[0] + Mathf.Tau * t / tonguesPerThrow;
	}

	private void ThrowOnePuff(float aim)
	{
		if (FlamePuffScene == null || PlayerRef == null)
			return;

		Node2D arena = PlayerRef.GetParent<Node2D>();
		if (arena == null)
			return;

		// A Fire Fan's tongues keep close to the sweep (FanScatterShare of the cone), so the eye follows one
		// flame moving across the arc rather than a wider cone flickering.
		float halfAngle = Mathf.DegToRad(ConeAngleDegrees * 0.5f) * (fireFan ? FanScatterShare : 1f);
		float angle = aim + rng.RandfRange(-halfAngle, halfAngle);

		// Speed and lifetime are derived from range together, so the cone's reach is the spell's
		// range whatever the jitter does - a faster puff simply dies sooner.
		float speedScale = 1.0f + rng.RandfRange(-SpeedJitter, SpeedJitter);
		float lifetime = MathF.Max(0.12f, BaseFlightSeconds);
		float speed = (range / lifetime) * speedScale;

		var puff = FlamePuffScene.Instantiate<Node2D>();
		if (puff is FlamePuff script)
		{
			script.SpellData = SpellData;
			script.PlayerRef = PlayerRef;
			script.Damage = damagePerPuff;
			script.Speed = speed;
			script.Lifetime = lifetime / speedScale;
			script.HitRadius *= AreaMultiplier * tongueWidth;
			// Ember Wake: every EmberEvery-th tongue drops an ember somewhere along its flight, so the
			// embers fall across the whole cone it swept rather than in a ring at its reach.
			if (emberWake && tongueCounter++ % EmberEvery == 0 && EnsureGround() is CinderGround ground)
			{
				script.EmberGround = ground;
				script.EmberAtFraction = rng.RandfRange(0.35f, 0.95f);
				script.EmberRadius = EmberRadius * tongueWidth * AreaMultiplier;
				script.EmberSeconds = EmberSeconds;
				script.EmberDamage = Math.Max(1, Mathf.RoundToInt(damagePerPuff * EmberDamageShare));
			}
		}

		arena.AddChild(Player.TagSpell(puff));
		puff.GlobalPosition = GlobalPosition;
		puff.Rotation = angle;
		// The evolutions' width is drawn as well as hit. (The player's own Area still grows only the
		// hit, as it did before the evolutions reached the jet.)
		if (MathF.Abs(tongueWidth - 1f) > 0.01f)
			puff.Scale = Vector2.One * tongueWidth;
	}

	// How long a tongue is in the air. Held constant so the animation always plays at a sane rate;
	// range is bought with speed instead.
	private const float BaseFlightSeconds = 0.42f;
}
