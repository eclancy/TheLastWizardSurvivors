using Godot;
using System;
using TheOneHeMissed.scripts;

// A TURNED BODY: DRAGON'S TEETH (Medea's Apotheosis, the verb TURN; .ai/chapter-wizards.md 4.2).
//
// In the story Jason sowed the dragon's teeth in the field of Ares, and they came up armed men; Medea told him to
// throw one stone among them, and they fell on each other. At full growth her Solar Flare throws a stone of light
// into the crowd (ElementalPulse.ThrowTeethStone), and what it strikes TURNS: for TurnSeconds a turned body stops
// going for the wizard and goes for the nearest other body instead, and strikes it when it gets there.
//
// THE STATE IS SMALL ON PURPOSE (.ai/performance.md). A turned body is a body that has a target other than the
// player: it looks for the nearest other enemy through the swarm's grid (EnemyRegistry.ForEachNear, a struct
// visitor, nothing allocated) every TurnedRetargetSeconds, walks at it - the steering override in
// Enemy._PhysicsProcess, over everything AdjustSteering or a side event's ScriptedHeading would ask - and lands a
// blow on it every TurnedBlowSeconds once it stands within TurnedStrikeReach. Never more than MaxTurnedAtOnce at
// once in the whole arena (TurnedCount), so the cost is bounded whatever the swarm does. Everyone else pays
// nothing: an unturned body reads one float.
//
// WHAT A TURNED BODY DOES NOT DO. It never touches the player: Player's contact loop passes over it (IsTurned), and
// TargetPlayer is null while it is turned, so a caster does not draw its bolt, a slammer's or an exploder's wind-up
// does not start, and one already winding up resolves against nobody (GroundSlamAttack.ResolveAgainst takes a null
// target and hits nothing). It is still an enemy: in "enemies", hit by his spells, killed by them.
//
// WHO IS NEVER TURNED. Elites, minibosses and the Boo (IsMiniBoss) are struck by the stone, not turned (4.2:
// "elites are struck, not turned"); a boss never (the CC rule, chapter-wizards.md section 1); nor a body that is
// sealed or held (IsTargetable, IsHeld), dying, or recycled - a recycle ends the turn, as it ends every status.
//
// WHERE ITS BLOWS GO. A blow is TurnedBlowShare of the STRIKER's own health - a man of the sown field is as strong
// against his neighbour as he is - straight into the target's TakeDamage, never through Player.DealDamageToEnemy:
// no crit, no damage multiplier, no Vulnerable, no on-hit or on-kill rider (lifesteal, the chalice, the Hungry
// Hedge's feeding, the spell's damage tally), and no Player.EnemyDamagedAt, so the thaw is never warmed by it. It
// is not scaled by ScaleOutgoingDamage either: that is the rule for what an enemy deals the PLAYER (the chapter's
// EnemyDamage and Weaken), and this is dealt to one of its own. A death it causes is still a death - Enemy.StartDeath
// counts the kill (RunEvents.RecordKill), tells the player (OnEnemyDiedAt) and drops the orb, exactly as a poison
// tick's does - so it counts on his kill tally and pays its XP (decided 2026-11-03: a form that cost the run its
// orbs would be a form nobody took). TurnedBlows and TurnedKills keep the tally for the harness.
public partial class Enemy
{
	/// <summary>How long a turned body stays turned (4.2: "for three seconds").</summary>
	public const float TurnSeconds = 3f;

	/// <summary>The most bodies turned at once in the whole arena; a stone that would turn more strikes them only.</summary>
	public const int MaxTurnedAtOnce = 16;

	/// <summary>Seconds between a turned body's blows, once it stands on its target.</summary>
	public const float TurnedBlowSeconds = 0.6f;

	/// <summary>
	/// A blow, as a share of the striker's own health: three blows kill a neighbour of its own kind, a little under
	/// two seconds of a three-second turn - "and they fight", not a scratch.
	/// </summary>
	public const float TurnedBlowShare = 0.34f;

	/// <summary>How far a turned body looks for another to go for; past it, it stands.</summary>
	public const float TurnedSeekRadius = 360f;

	/// <summary>Centre to centre: within this it strikes. The soft crowd's personal space is about 59 px.</summary>
	public const float TurnedStrikeReach = 52f;

	// A turned body goes at its own a little faster than it walks at him: the steering's length is a speed
	// multiplier, and past 1.05 the chase takes it at once rather than easing in, as a dash does.
	private const float TurnedLunge = 1.2f;
	private const float TurnedRetargetSeconds = 0.3f;

	/// <summary>How many bodies are turned right now, in the whole arena (at most <see cref="MaxTurnedAtOnce"/>).</summary>
	public static int TurnedCount { get; private set; }

	/// <summary>This run's tally, for the harness: blows turned bodies landed, and the bodies those blows killed.</summary>
	public static int TurnedBlows { get; private set; }
	public static int TurnedKills { get; private set; }

	/// <summary>A run starts with nobody turned and an empty tally. Player._Ready calls it.</summary>
	public static void ResetTurnedTally()
	{
		TurnedCount = 0;
		TurnedBlows = 0;
		TurnedKills = 0;
	}

	/// <summary>True while this body is turned on its own (<see cref="Turn"/>).</summary>
	public bool IsTurned => turnedTimeRemaining > 0f;

	/// <summary>Who it is going for, or null while it looks. For the harness, the shots and the validator.</summary>
	public Enemy TurnedTarget => turnedTarget != null && IsInstanceValid(turnedTarget) ? turnedTarget : null;

	private float turnedTimeRemaining;
	private Enemy turnedTarget;
	private float turnedRetarget;
	private float turnedBlowClock;
	private Vector2 turnedHeading;

	/// <summary>Whether a stone may turn this body: an ordinary one, in play, not held, not dying.</summary>
	public bool CanBeTurned => !isDying && !IsMiniBoss && this is not BossEnemy && IsTargetable && !IsHeld;

	/// <summary>
	/// Turns this body on its own for <paramref name="seconds"/>. Stacking-resistant like every status here: a body
	/// already turned keeps the longer of the two, and is not counted twice. False if it cannot be turned, or the
	/// arena already holds <see cref="MaxTurnedAtOnce"/>.
	/// </summary>
	public bool Turn(float seconds)
	{
		if (seconds <= 0f || !CanBeTurned)
			return false;
		if (turnedTimeRemaining <= 0f)
		{
			if (TurnedCount >= MaxTurnedAtOnce)
				return false;
			TurnedCount++;
			turnedTarget = null;
			turnedRetarget = 0f;
			// The first blow comes a beat after it reaches its man, not on the frame.
			turnedBlowClock = TurnedBlowSeconds * 0.5f;
			turnedHeading = Vector2.Zero;
		}
		turnedTimeRemaining = Math.Max(turnedTimeRemaining, seconds);
		RefreshStatusLook();
		return true;
	}

	// Lets go of the turn wherever it ends: its time up, a death, a recycle, a seal, leaving the tree.
	private void EndTurn()
	{
		if (turnedTimeRemaining <= 0f)
			return;
		turnedTimeRemaining = 0f;
		turnedTarget = null;
		turnedHeading = Vector2.Zero;
		TurnedCount = Math.Max(0, TurnedCount - 1);
		RefreshStatusLook();
	}

	// Every physics step of a turned body, before it steers: the clock, who it is going for, and the blow.
	private void TickTurned(float delta)
	{
		turnedTimeRemaining -= delta;
		if (turnedTimeRemaining <= 0f)
		{
			turnedTimeRemaining = 0.0001f;   // so EndTurn counts it out
			EndTurn();
			return;
		}
		Vector2 here = GlobalPosition;
		turnedRetarget -= delta;
		if (!IsTurnedTargetValid(turnedTarget) || turnedRetarget <= 0f)
		{
			turnedRetarget = TurnedRetargetSeconds;
			var nearest = new NearestOther(this, here, TurnedSeekRadius);
			EnemyRegistry.ForEachNear(here, TurnedSeekRadius, ref nearest);
			turnedTarget = nearest.Best;
		}
		if (!IsTurnedTargetValid(turnedTarget))
		{
			// Nobody near enough: it stands where it is, looking, rather than going back to him.
			turnedTarget = null;
			turnedHeading = Vector2.Zero;
			return;
		}
		Vector2 offset = turnedTarget.GlobalPosition - here;
		float distance = offset.Length();
		if (distance > TurnedStrikeReach)
		{
			turnedHeading = offset / distance * TurnedLunge;
			turnedBlowClock = Math.Max(turnedBlowClock, TurnedBlowSeconds * 0.35f);
			return;
		}
		// On him: stand, and strike. Not while thrown, staggered or in the air - a body that cannot step cannot swing.
		turnedHeading = Vector2.Zero;
		if (shockTimeRemaining > 0f || airborneTime > 0f || knockbackTime > 0f)
			return;
		turnedBlowClock -= delta;
		if (turnedBlowClock > 0f)
			return;
		turnedBlowClock = TurnedBlowSeconds;
		int blow = Math.Max(1, Mathf.RoundToInt(Math.Max(1, maxHealth) * TurnedBlowShare));
		Enemy victim = turnedTarget;
		PlayAttackAnimation();
		victim.TakeTurnedBlow(blow);
		TurnedBlows++;
		if (victim.IsDying)
		{
			TurnedKills++;
			turnedTarget = null;
			turnedRetarget = 0f;
		}
	}

	private bool IsTurnedTargetValid(Enemy target) =>
		target != null && target != this && IsInstanceValid(target) && !target.IsDying && target.InEnemyRegistry && !target.IsHeld;

	// The nearest other body in reach, from the swarm's grid. A struct, so the query allocates nothing.
	private struct NearestOther : INeighbourVisitor
	{
		private readonly Enemy self;
		private readonly Vector2 origin;
		private float bestDistanceSquared;
		public Enemy Best;

		public NearestOther(Enemy self, Vector2 origin, float radius)
		{
			this.self = self;
			this.origin = origin;
			bestDistanceSquared = radius * radius;
			Best = null;
		}

		public void Visit(Enemy enemy, Vector2 position)
		{
			if (enemy == self || enemy.IsDying || enemy.IsHeld)
				return;
			float d2 = origin.DistanceSquaredTo(position);
			if (d2 >= bestDistanceSquared)
				return;
			bestDistanceSquared = d2;
			Best = enemy;
		}
	}

	/// <summary>
	/// A blow from one of its own that the stone turned: the same as <see cref="TakeDamage"/> - the hit's flash and
	/// number, the damage statistic, the death and everything a death pays - in the stone's warm light rather than
	/// the player's red, so a crowd fighting itself is not read as his hits. Never the player's pipeline.
	/// </summary>
	public void TakeTurnedBlow(int amount) => ApplyHit(amount, false, TurnedBlowColor);

	private static readonly Color TurnedBlowColor = new(1f, 0.86f, 0.46f, 1f);

	// The turned look: lit warm by the stone of light - the light ramp's hot stop on a body that is, for three
	// seconds, not his. Composes with the hit flash and the elite tints as every status look does (SelfModulate).
	private static readonly Color TurnedLook = new(1.36f, 1.20f, 0.74f);
}
