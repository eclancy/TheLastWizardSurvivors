using Godot;
using System;
using WizardSurvivors.scripts;

// The stage boss. Everything about chasing, damage, status and death already lives in Enemy, so
// this only adds what makes a boss a boss: an identity the run result can record, a telegraphed
// attack that punishes standing in the wrong place, health thresholds that change how it fights,
// and a signal the run listens for so a kill can end the level in victory.
//
// Used bare, it is Elderbark: a slow, immense melee threat with one ground slam and one enrage. It
// is also the base every other chapter boss subclasses, and the division of labour between them is
// deliberate. THIS CLASS OWNS THE CLOCK - cooldown, wind-up, phase thresholds, the victory signal -
// and a subclass owns only what is different about its fight. A boss that needed its own copy of
// any of the above would be the third place in the project where a wind-up got written by hand.
//
// The five hooks a subclass has, and nothing else:
//
//   BuildSlam()            what the telegraphed attack IS - shape, reach, damage, colour
//   BuildPhaseThresholds() the health fractions where the fight changes
//   OnPhaseEntered(n)      what changes at each one
//   PlantsDuringWindUp     false for a boss that keeps moving through its own tell (a charge)
//   WantsToAttack(...)     when the wind-up is allowed to begin
//
// Besides those, two presentations that touch no gameplay, each played on the BossDeath stage: the
// fall (OnDeathBegin / TickDeath / DrawDeath*) and, since 2026-10-31, the ARRIVAL (OnEntranceBegin /
// TickEntrance / DrawEntrance*). Every boss arrives untargetable and holding still for
// EntranceSeconds while its entrance plays, and only then starts its clock; Node2DGame holds the
// player's spells and the swarm for as long as any boss is arriving (EnteringCount).
//
// See .ai/enemy-behaviour.md for the eight chapter sketches and which primitive each needs.
public partial class BossEnemy : Enemy
{
	[Signal] public delegate void BossDefeatedEventHandler(string bossId);

	[Export] public string BossId { get; set; } = string.Empty;
	[Export] public string BossDisplayName { get; set; } = "Boss";

	// Ground slam: the boss plants its feet, its reach is marked and its icon fills, then everything
	// inside it takes a heavy hit. The telegraph is the whole fight - the slam is only unfair if the
	// player cannot see it coming.
	[Export] public float SlamIntervalSeconds { get; set; } = 4.6f;
	[Export] public float SlamTelegraphSeconds { get; set; } = 0.85f;
	[Export] public float SlamRadius { get; set; } = 150f;
	[Export] public int SlamDamage { get; set; } = 4;

	// Below this fraction of max health the boss speeds up and slams harder. The last of Elderbark's
	// two thresholds; subclasses that want their own phases override BuildPhaseThresholds.
	[Export] public float EnrageHealthFraction { get; set; } = 0.34f;

	// ELDERBARK'S ROOTS (2026-10-31: every boss has at least two phases, each asking something new).
	// Below RootsHealthFraction the forest joins in: a line of roots breaks the ground from the tree to
	// the player, each root marked on the ground before it bursts and each a beat after the one behind
	// it, so the crack is seen racing outward and the answer is the sidestep. It asks the first chapter's
	// question harder - keep delivering while moving - without asking a new one.
	[Export] public float RootsHealthFraction { get; set; } = 0.66f;
	[Export] public float RootIntervalSeconds { get; set; } = 5.2f;
	[Export] public int RootsPerLine { get; set; } = 5;
	[Export] public float RootTellSeconds { get; set; } = 0.9f;
	[Export] public float RootStaggerSeconds { get; set; } = 0.16f;
	[Export] public float RootRadius { get; set; } = 54f;
	[Export] public int RootDamage { get; set; } = 3;
	[Export] public float EnrageSpeedMultiplier { get; set; } = 1.35f;
	[Export] public float EnrageSlamIntervalMultiplier { get; set; } = 0.66f;

	// How far out the boss will commit to a wind-up, as a multiple of its reach. Above 1 so it
	// starts while the player is still approaching.
	[Export] public float SlamEngageRadiusMultiplier { get; set; } = 2.2f;

	// Cooldown, wind-up, warning shape and the hit itself all live in the shared telegraph, which
	// SlammerEnemy and ExploderEnemy run too.
	private GroundSlamAttack slam;
	private float[] phaseThresholds = Array.Empty<float>();
	private bool defeatAnnounced;

	/// <summary>The telegraphed attack, for subclasses that re-aim or retune it mid-fight.</summary>
	protected GroundSlamAttack Slam => slam;

	/// <summary>
	/// How many thresholds have been crossed. 0 is the opening fight; 1 is the enrage on a plain
	/// boss. Read it in <c>_PhysicsProcess</c> to gate behaviour a later phase adds.
	/// </summary>
	protected int CurrentPhase { get; private set; }

	/// <summary>The same number, readable from outside. For the HUD and for _BossProbe.</summary>
	public int BossPhase => CurrentPhase;

	/// <summary>
	/// What the boss health bar shows, 0 to 1. Its own health for every ordinary boss, and the
	/// whole set for a boss that is more than one body.
	/// </summary>
	/// <remarks>
	/// The HUD reads this rather than <c>HealthFraction</c> because the Hollow Choir is three linked
	/// bodies and the bar has to mean the fight rather than whichever third the run holds a
	/// reference to - a bar that jumped back to full on a revive would be lying about progress the
	/// player really had made.
	/// </remarks>
	public virtual float BossHealthFraction => HealthFraction;

	// Softer debuff ceilings than an ordinary enemy's, set here so a boss scene that says nothing
	// still gets them; a .tscn value overrides. Same reasoning as MinSlowMultiplier: a quarter of
	// an ordinary enemy's health is one shard, a quarter of a boss is a quarter of the fight.
	public BossEnemy()
	{
		MaxWitherFraction = 0.08f;
		MaxWeakenFraction = 0.25f;
	}

	public override void _Ready()
	{
		// Bosses are always elites as far as the rest of the game is concerned: the gold ring marker,
		// the fat XP drop and the elite headcount all key off this. Set before base._Ready, which is
		// where Enemy builds the marker.
		IsMiniBoss = true;
		base._Ready();
		slam = BuildSlam();
		phaseThresholds = BuildPhaseThresholds() ?? Array.Empty<float>();
		BeginEntrance();
	}

	/// <summary>How many thresholds this boss's fight has. For _BossProbe.</summary>
	public int PhaseCount => phaseThresholds.Length;

	/// <summary>The thresholds as the scene builds them, without the tree. For RegressionChecks.</summary>
	internal float[] DesignedPhaseThresholds => BuildPhaseThresholds() ?? Array.Empty<float>();

	/// <summary>
	/// The telegraphed attack this boss opens with. Override to change its shape - a cone or a lane
	/// tells the player which way to dodge, which a ring cannot.
	/// </summary>
	protected virtual GroundSlamAttack BuildSlam() =>
		new(SlamIntervalSeconds, SlamTelegraphSeconds, SlamRadius, SlamDamage, new Color(1.0f, 0.45f, 0.20f));

	/// <summary>
	/// Health fractions, descending, at which the fight changes. The default is Elderbark's: the roots,
	/// then the enrage - so a boss that overrides nothing behaves exactly as Elderbark does.
	/// </summary>
	/// <remarks>
	/// Read against <see cref="BossHealthFraction"/>, not the body's own health: for the Hollow Choir
	/// that is the whole set, so its three voices change together.
	/// </remarks>
	protected virtual float[] BuildPhaseThresholds() => new[] { RootsHealthFraction, EnrageHealthFraction };

	/// <summary>
	/// Called once when the boss crosses into phase <paramref name="phase"/> (1-based). The default
	/// is Elderbark's: the roots at the first threshold, then the enrage - faster on its feet, faster
	/// on its slam, and visibly hotter so the change in pace has a cause rather than feeling like a bug.
	/// </summary>
	protected virtual void OnPhaseEntered(int phase)
	{
		if (phase < phaseThresholds.Length)
		{
			rootsActive = true;
			untilRoots = 0.6f;
			return;
		}
		ApplyEnrage();
		RootIntervalSeconds *= 0.72f;
	}

	/// <summary>
	/// Whether the boss holds still through its wind-up. True for anything that draws its warning on
	/// the ground it is standing on; false for a charge, which carries the warning with it.
	/// </summary>
	/// <remarks>
	/// This is the whole of the travelling-telegraph primitive. <see cref="GroundSlamAttack"/>
	/// already draws on the caster and resolves at the caster's position on the frame it lands, so
	/// a boss that simply does not plant has a moving attack with no new machinery at all.
	/// </remarks>
	protected virtual bool PlantsDuringWindUp => true;

	/// <summary>
	/// Whether a wind-up may begin this frame. The default only commits when the player is close
	/// enough that the attack could plausibly land, so the boss does not spend the fight rooted in
	/// place slamming empty ground.
	/// </summary>
	protected virtual bool WantsToAttack(Node2D target, float distanceToPlayer) =>
		distanceToPlayer <= SlamRadius * SlamEngageRadiusMultiplier;

	/// <summary>Called on the frame the wind-up begins. Aim here, not during it.</summary>
	protected virtual void OnAttackStarted(Node2D target)
	{
		PlayAttackAnimation();
		if (target != null && IsInstanceValid(target))
			slam.AimAt(GlobalPosition, target.GlobalPosition);
	}

	/// <summary>Called on the frame the attack lands, exactly once per wind-up.</summary>
	protected virtual void OnAttackResolved(Node2D target) => slam.ResolveAgainst(this, target);

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		TickPhases();
		TickSlam((float)delta);
		TickRoots((float)delta);
	}

	private bool rootsActive;
	private float untilRoots;

	// Elderbark's roots: a line of TelegraphedGroundHits from its feet through where the player stands,
	// each marked from the same frame and each landing RootStaggerSeconds after the one nearer the tree.
	private void TickRoots(float delta)
	{
		if (!rootsActive || IsDying)
			return;
		untilRoots -= delta;
		if (untilRoots > 0f)
			return;
		untilRoots = RootIntervalSeconds;

		Node2D target = TargetPlayer;
		Node arena = GetParent();
		if (target == null || !IsInstanceValid(target) || arena == null || !IsInstanceValid(arena))
			return;

		Vector2 from = GlobalPosition;
		Vector2 toPlayer = target.GlobalPosition - from;
		float reach = toPlayer.Length() + RootRadius;
		if (reach < 1f)
			return;
		Vector2 along = toPlayer / Mathf.Max(1f, toPlayer.Length());
		int count = Math.Max(2, RootsPerLine);
		int damage = ScaleOutgoingDamage(RootDamage);
		var bark = new Color(0.62f, 0.78f, 0.36f);
		for (int i = 0; i < count; i++)
		{
			Vector2 at = from + along * reach * (i + 1) / count;
			TelegraphedGroundHit.Place(arena, at, RootTellSeconds + RootStaggerSeconds * i, RootRadius, damage, bark);
		}
		PlayAttackAnimation();
	}

	// Planting the boss for its wind-up is the base class's job: returning Zero holds it exactly
	// where the warning is drawn, which is the only place the attack is honest. A boss that opts out
	// of planting keeps chasing, and the warning travels with it.
	protected override Vector2 AdjustSteering(Vector2 chaseDirection, float distanceToPlayer)
	{
		return PlantsDuringWindUp && slam != null && slam.IsWindingUp ? Vector2.Zero : chaseDirection;
	}

	// Thresholds are crossed in order and only ever forward: a boss healed back above one does not
	// un-enrage, because a phase that can be undone teaches the player nothing.
	private void TickPhases()
	{
		while (CurrentPhase < phaseThresholds.Length && BossHealthFraction <= phaseThresholds[CurrentPhase])
		{
			CurrentPhase++;
			OnPhaseEntered(CurrentPhase);
		}
	}

	/// <summary>The default phase change: faster, harder, hotter. Callable from an override.</summary>
	protected void ApplyEnrage()
	{
		Speed *= EnrageSpeedMultiplier;
		SlamIntervalSeconds *= EnrageSlamIntervalMultiplier;
		if (slam != null)
			slam.IntervalSeconds = SlamIntervalSeconds;
		SetBaseModulate(new Color(1.0f, 0.62f, 0.52f));
	}

	private void TickSlam(float delta)
	{
		Node2D target = TargetPlayer;
		bool valid = target != null && IsInstanceValid(target);
		float distance = valid ? GlobalPosition.DistanceTo(target.GlobalPosition) : float.MaxValue;

		switch (slam.Tick(delta, valid && WantsToAttack(target, distance)))
		{
			case AttackTelegraph.Beat.Started:
				OnAttackStarted(target);
				break;
			case AttackTelegraph.Beat.Resolved:
				OnAttackResolved(target);
				break;
		}
	}

	// The warning is drawn on the boss's own floor (Enemy.DrawGroundTells), a node made once, rather
	// than a node per attack, so an attack allocates nothing - this runs in a scene that already has a
	// swarm in it. It is a dashed outline on the ground (TelegraphOutline), so it is under every body,
	// the boss's own included.
	//
	// Never on a corpse. A boss killed mid wind-up stops its clock with the warning half grown, and a
	// warning that stays drawn through the fall says a hit is coming that never will.
	protected override void DrawGroundTells(CanvasItem floor)
	{
		if (!IsDying)
			slam?.Draw(floor);
	}

	// The clock is the icon over the boss (TelegraphIcon): a lane's charge shows the claw, a ring or a
	// wedge the slam, unless the subclass's BuildSlam names another (GroundSlamAttack.GlyphOverride).
	// Enemy keeps it off a corpse.
	protected override bool ReadAttackTell(out TelegraphGlyph glyph, out float progress)
	{
		glyph = slam?.Glyph ?? TelegraphGlyph.Slam;
		progress = slam?.WindUpProgress ?? 0f;
		return slam != null && slam.IsWindingUp;
	}

	// A boss death is a run-defining event, so it skips the throttling and the heavy/small
	// split that ordinary deaths go through and plays its own sound outright.
	protected override void PlayDeathSound()
	{
		SfxPlayer.AtPosition(SfxCatalog.BossDeath, GlobalPosition, 0.0f);
	}

	protected override void StartDeath()
	{
		// Only a tool can fell a boss that is still arriving (it is untargetable), but if one does, the
		// fall must cut the whole body, not the half-grown arrival.
		AbortEntrance();
		bool alreadyDying = defeatAnnounced;
		defeatAnnounced = true;
		base.StartDeath();

		// Guard the emit rather than the call: base.StartDeath is itself re-entrant-safe, but the
		// run must never be told it won twice.
		if (!alreadyDying)
			EmitSignal(SignalName.BossDefeated, BossId);
	}

	// --- The arrival --------------------------------------------------------------------------------
	//
	// "Whenever a boss spawns, all of the players spells should stop firing, and you should see an
	// animation of the boss spawning, such as bursting up from the ground, growing from a seed, whatever
	// fits its theme" (2026-10-31). Each boss arrives as what it is, for EntranceSeconds (2.5 - 4 s):
	// Elderbark grows from a seed, the burrowers break the ground, the Choir's voices gather out of the
	// dark. The BossDeath stage plays it (in its entrance mode), from the boss's own sheet.
	//
	// FAIR BY CONSTRUCTION. For the whole of it the boss is untargetable (SetTargetable) and its clock
	// is stopped (physics off: no step, no wind-up, no phase, nothing it summons), so it cannot touch
	// the player and nothing can touch it; Node2DGame reads EnteringCount and holds the player's spells
	// and the swarm, and keeps the player from being hurt, until every arriving body has landed.

	/// <summary>Bodies of any boss still arriving. Node2DGame holds the field while it is above zero.</summary>
	public static int EnteringCount { get; private set; }

	/// <summary>True from the frame the boss is added until its arrival has played.</summary>
	public bool IsEntering { get; private set; }

	private BossDeath entrance;
	private bool countedAsEntering;

	/// <summary>0 to 1 through the arrival; 1 once it has landed. The boss bar fills with it.</summary>
	public float EntranceProgress => IsEntering && entrance != null && IsInstanceValid(entrance) ? entrance.Progress : 1f;

	/// <summary>How long the arrival plays. RegressionChecks holds it to 2.5 - 4 s.</summary>
	protected internal virtual float EntranceSeconds => 3.3f;

	/// <summary>Seconds the arrival waits before it begins: the Choir's voices gather in turn.</summary>
	protected internal virtual float EntranceDelaySeconds => 0f;

	/// <summary>When in the arrival the boss roars (boss_roar, once an arrival); negative for never.</summary>
	protected internal virtual float EntranceRoarSeconds => EntranceSeconds * 0.72f;

	/// <summary>True for a boss whose own <c>_Draw</c> is part of its arrival (the Long Coil's body).</summary>
	protected internal virtual bool RedrawsWhileEntering => false;

	private void BeginEntrance()
	{
		if (EntranceSeconds <= 0f)
			return;
		IsEntering = true;
		countedAsEntering = true;
		EnteringCount++;
		SetTargetable(false);
		SetPhysicsProcess(false);
		SetTellsVisible(false);
		Node2D witness = TargetPlayer;
		entrance = new BossDeath();
		entrance.ConfigureEntrance(this, BodySprite, BodySpriteRestPosition, EntranceDelaySeconds, EntranceSeconds,
			witness != null && IsInstanceValid(witness) ? witness.GlobalPosition : (Vector2?)null);
		AddChild(entrance);
	}

	/// <summary>Called by the stage on the arrival's last frame: the fight begins.</summary>
	internal void FinishEntrance()
	{
		if (!IsEntering)
			return;
		IsEntering = false;
		entrance = null;
		ReleaseEnteringCount();
		if (IsDying)
			return;
		SetTargetable(true);
		SetPhysicsProcess(true);
		SetTellsVisible(true);
	}

	/// <summary>
	/// Called when the arrival hands the body back, whether it played out or was cut short: undo
	/// anything done to the boss's own sprite rather than to a cut piece (Wizard Dude's pop).
	/// </summary>
	protected internal virtual void OnEntranceEnd(BossDeath stage) { }

	/// <summary>The whole body up out of the ground: <paramref name="u"/> 0 is under, 1 is standing.</summary>
	protected static void RiseFromGround(BossDeath stage, BossDeath.Piece body, float u) =>
		body.Sink(stage, stage.FrameSize.Y * (1f - Mathf.Clamp(u, 0f, 1f)));

	internal void RoarOnArrival() => SfxPlayer.AtPosition(SfxCatalog.BossRoar, GlobalPosition, 0.0f);

	private void AbortEntrance()
	{
		if (!IsEntering)
			return;
		IsEntering = false;
		if (entrance != null && IsInstanceValid(entrance))
		{
			entrance.ReleaseBody();
			entrance.QueueFree();
		}
		entrance = null;
		ReleaseEnteringCount();
	}

	private void ReleaseEnteringCount()
	{
		if (!countedAsEntering)
			return;
		countedAsEntering = false;
		EnteringCount = Math.Max(0, EnteringCount - 1);
	}

	public override void _ExitTree()
	{
		// A run left mid-arrival must not leave the next run's field held.
		ReleaseEnteringCount();
		base._ExitTree();
	}

	/// <summary>Called once, on the first frame of the arrival. Cut the body here.</summary>
	protected internal virtual void OnEntranceBegin(BossDeath stage)
	{
		growth = stage.CutWhole();
		growth.Scale = new Vector2(0.18f, 0.06f);
		growth.Visible = false;
	}

	/// <summary>Every frame of the arrival. <paramref name="t"/> is its own clock, 0 to EntranceSeconds.</summary>
	protected internal virtual void TickEntrance(BossDeath stage, float t, float dt)
	{
		if (growth == null)
			return;

		// ELDERBARK: GROWN. A seed of the cold light that looks out of it falls into the ground, the ground
		// heaves, roots crack out across it, and the tree comes up out of the soil far faster than a tree
		// should - a shoot, a trunk, a crown - overshooting and settling, shedding leaves and clods. The
		// mark in the knot opens last.
		float s = stage.ArtScale.X;
		float up = BossDeath.EaseOut(BossDeath.Span(t, GrowStart, GrowEnd));
		float wide = BossDeath.EaseOut(BossDeath.Span(t, GrowStart + 0.3f, GrowEnd + 0.25f));
		float overshoot = 0.07f * MathF.Sin(Mathf.Pi * BossDeath.Span(t, GrowEnd - 0.2f, GrowEnd + 0.45f));
		growth.Visible = t >= GrowStart;
		growth.Scale = new Vector2(0.18f + 0.82f * wide, 0.06f + 0.94f * up + overshoot);
		growth.Rotation = 0.05f * MathF.Sin(t * 5.3f) * (1f - BossDeath.Span(t, GrowEnd, EntranceSeconds));
		growth.Offset = new Vector2(BossDeath.Shudder(t, 1f - BossDeath.Span(t, 0.3f, GrowStart), 15f) * s, 0f);
		// Pale as a shoot, darkening into bark as it grows; lit by the seed until it has taken root.
		growth.Color = new Color(1f, 1f, 1f, 1f).Lerp(new Color(1.25f, 1.3f, 1.1f, 1f), 1f - up);
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		growth.Glow = new Color(arcane.Mid, 0.45f * (1f - BossDeath.Span(t, GrowStart, GrowStart + 1.2f)));

		Vector2 foot = stage.Foot;
		if (stage.Crossed(SeedLands))
		{
			stage.DustRing(foot, 30f, 6, new Color(0.40f, 0.34f, 0.26f, 0.7f), 0.7f, 12f);
			stage.PuffAt(foot, 6f, 30f, 0.5f, new Color(arcane.Mid, 0.7f), 0f, 0f, true);
		}
		if (stage.Crossed(GrowStart))
		{
			// The soil the shoot comes up through.
			stage.EmitAt(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.Clod, SpeedMin = 40f, SpeedMax = 150f, UpMin = 120f, UpMax = 260f,
				Spin = 6f, Bounce = 0.25f, Life = 1.6f, Fade = 0.4f,
			}, 12, foot, 4f, 18f);
			stage.EmitAt(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.Moss, SpeedMin = 30f, SpeedMax = 110f, UpMin = 90f, UpMax = 200f,
				Spin = 4f, Bounce = 0.2f, Life = 1.6f, Fade = 0.4f, Tinted = true,
			}, 6, foot, 4f, 20f);
		}
		if (stage.Crossed(GrowEnd - 0.3f))
		{
			// The crown opens: leaves thrown off it, drifting down for two seconds.
			stage.Emit(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.Leaf, Frames = 4, Fps = 7f, Tinted = true,
				SpeedMin = 30f, SpeedMax = 140f, UpMin = 60f, UpMax = 200f, Gravity = 260f, Terminal = 46f,
				Drag = 1.4f, Flutter = 7f, FlutterHz = 1.4f, Bounce = 0f, Friction = 12f,
				Life = 2.2f, Fade = 0.5f, DepthJitter = 40f,
			}, 26, new Vector2(48f, 18f), new Vector2(40f, 14f), 0f, 0.01f);
		}
		if (stage.Crossed(GrowEnd + 0.1f))
			stage.DustRing(foot, 64f, 9, new Color(0.48f, 0.54f, 0.50f, 0.55f), 0.8f, 16f);
	}

	private BossDeath.Piece growth;
	private const float SeedLands = 0.45f;
	private const float GrowStart = 0.75f;
	private const float GrowEnd = 2.45f;

	/// <summary>On the ground, under every actor: the heave, the cracks, the stain.</summary>
	protected internal virtual void DrawEntranceGround(BossDeath stage, CanvasItem canvas, float t)
	{
		// The roots crack out from where the seed went in, across the ground, and sink back as it stands.
		// Elderbark's alone: a subclass whose arrival draws nothing here must not inherit its roots.
		if (growth == null)
			return;
		Vector2 foot = stage.Foot;
		float grow = BossDeath.EaseOut(BossDeath.Span(t, SeedLands, GrowEnd));
		float fade = 1f - BossDeath.Span(t, EntranceSeconds, EntranceSeconds + 1.1f);
		if (grow <= 0f || fade <= 0f)
			return;
		BossDeath.Disc(canvas, foot, 12f + 30f * grow, 6f + 13f * grow, new Color(0.16f, 0.12f, 0.08f, 0.7f * fade));
		var root = new Color(0.40f, 0.30f, 0.18f, 0.95f * fade);
		var shade = new Color(0.10f, 0.08f, 0.05f, 0.8f * fade);
		for (int k = 0; k < 8; k++)
		{
			// All round the foot, flattened onto the ground the way everything on the floor is.
			float a = Mathf.Tau * k / 8f + (BossDeath.Hash(13u, k) - 0.5f) * 0.5f;
			float length = (56f + 56f * BossDeath.Hash(17u, k)) * grow;
			var dir = new Vector2(MathF.Cos(a), 0.45f * MathF.Sin(a));
			var kink = new Vector2(-dir.Y, dir.X) * (BossDeath.Hash(23u, k) - 0.5f) * 18f;
			Vector2 mid = foot + dir * length * 0.55f + kink;
			Vector2 tip = foot + dir * length;
			BossDeath.Line(canvas, foot + new Vector2(0f, 2f), mid + new Vector2(0f, 2f), shade, BossDeath.Px * 3f);
			BossDeath.Line(canvas, foot, mid, root, BossDeath.Px * 3f);
			BossDeath.Line(canvas, mid, tip, root, BossDeath.Px * 2f);
		}
	}

	/// <summary>In the air, over every body: dust, debris that is not light.</summary>
	protected internal virtual void DrawEntranceAir(BossDeath stage, CanvasItem canvas, float t) { }

	/// <summary>Light, drawn additively over everything: the seed, glows, sparks.</summary>
	protected internal virtual void DrawEntranceLight(BossDeath stage, CanvasItem canvas, float t)
	{
		if (growth == null)
			return;
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		Vector2 foot = stage.Foot;
		// The seed: a mote of the mark's cold light, falling to the ground and going in.
		if (t < SeedLands + 0.25f)
		{
			float fall = BossDeath.EaseIn(BossDeath.Span(t, 0f, SeedLands));
			Vector2 at = foot - new Vector2(0f, 150f * (1f - fall));
			float a = 1f - BossDeath.Span(t, SeedLands, SeedLands + 0.25f);
			BossDeath.Disc(canvas, at, 10f, 10f, new Color(arcane.Mid, 0.45f * a));
			BossDeath.Disc(canvas, at, 4f, 4f, new Color(arcane.Hot, a));
		}
		// The mark in the knot opens as it settles: two points of it, flaring once.
		float eyes = BossDeath.Span(t, GrowEnd + 0.2f, GrowEnd + 0.35f) * (1f - BossDeath.Span(t, GrowEnd + 0.5f, EntranceSeconds + 0.3f));
		if (eyes > 0f && growth != null)
		{
			BossDeath.Disc(canvas, stage.PieceToLocal(growth, new Vector2(45f, 42f)), 6f, 6f, new Color(arcane.Hot, 0.8f * eyes));
			BossDeath.Disc(canvas, stage.PieceToLocal(growth, new Vector2(51f, 42f)), 6f, 6f, new Color(arcane.Hot, 0.8f * eyes));
		}
	}

	// --- The fall ---------------------------------------------------------------------------------
	//
	// "Bosses should have cool death animations that relate to what they are" (2026-10-28). Every boss
	// used to end on the same six frames - the body squashed into the floor and darkened - which said
	// "an enemy died" about fifteen minutes of fight with something that had a name. Now each one
	// falls as what it is: this tree splits and topples, the Gaoler's keys scatter, the Choir's voices
	// go out one by one. BossDeath is the stage; the play is written here and in each subclass.
	//
	// Presentation only, and after the fact: Enemy.StartDeath has already dropped the rewards, left
	// the group and zeroed the collision on the killing frame, exactly as before. Nothing below can
	// touch the player, and the victory flow (Node2DGame) hears BossDefeated on the same frame it
	// always did. The corpse is freed when the fall ends instead of when the strip did.

	protected override bool StagesOwnDeath => true;

	protected override void BeginOwnDeath()
	{
		// The telegraph and anything else _Draw and DrawGroundTells paint were drawn by a clock that has
		// just stopped; one redraw now that IsDying is set takes them off the corpse.
		RedrawTells();
		Node2D witness = TargetPlayer;
		var death = new BossDeath();
		death.Configure(this, BodySprite, BodySpriteRestPosition, DeathDelaySeconds, DeathSeconds,
			witness != null && IsInstanceValid(witness) ? witness.GlobalPosition : (Vector2?)null);
		AddChild(death);
	}

	/// <summary>How long the fall lasts once it begins. About the length of boss_death (2.4 s).</summary>
	protected internal virtual float DeathSeconds => 2.7f;

	/// <summary>Seconds the body holds before it begins to fall. The Hollow Choir's voices take turns.</summary>
	protected internal virtual float DeathDelaySeconds => 0f;

	/// <summary>True for a boss whose own <c>_Draw</c> is part of its fall (the Long Coil's body).</summary>
	protected internal virtual bool RedrawsWhileDying => false;

	// ELDERBARK: FELLED. It is the forest with something looking out of it, and it goes the way a
	// great tree goes - except that it splits first. The cold mark that was looking out of the knot
	// runs down the trunk as a seam of light, the trunk parts along it, and the two halves fall away
	// from each other like a lightning-split oak, each hinged on its own foot. The crown sheds its dead
	// leaves into the air as it goes, the bones it had grown over drop out of the hollow where the
	// ribcage was, and where each half hits the ground it throws bark and dust. Then the wood goes
	// dark and the leaves lie where they fell. sprite_treant.py: bark on stone, moss on lichen, bone
	// caught in the trunk - every piece of debris is one of those three.
	private BossDeath.Piece fellLeft;
	private BossDeath.Piece fellRight;
	private BossDeath.Piece fellRoots;

	// The two outer feet of the trunk at the root line, which each half tips over on, and the row the
	// trunk is cut from its roots.
	private const int FellRootRow = 82;
	private const int FellLeftFoot = 24;
	private const int FellRightFoot = 72;
	private const float FellStart = 0.38f;
	private const float FellLand = 1.04f;

	private static readonly Color FellDust = new(0.48f, 0.54f, 0.50f, 0.6f);

	/// <summary>Called once, on the first frame of the fall: cut the body, throw what goes at once.</summary>
	protected internal virtual void OnDeathBegin(BossDeath death)
	{
		fellRoots = death.Cut(new Vector2I(48, 94), new Rect2I(0, FellRootRow, 96, 96 - FellRootRow));
		fellLeft = death.Cut(new Vector2I(FellLeftFoot, FellRootRow), new Rect2I(0, 0, 48, FellRootRow));
		fellRight = death.Cut(new Vector2I(FellRightFoot, FellRootRow), new Rect2I(48, 0, 48, FellRootRow));
	}

	/// <summary>Every frame of the fall, before the pieces are placed. <paramref name="t"/> is its own clock.</summary>
	protected internal virtual void TickDeath(BossDeath death, float t, float dt)
	{
		if (fellLeft == null)
			return;

		float s = death.ArtScale.X;
		// The seam opens a crack before anything falls, and the tree shudders against it.
		float shudder = t < FellStart ? BossDeath.Shudder(t, 1f - t / FellStart, 17f) * s : 0f;
		float part = BossDeath.Span(t, 0.12f, FellStart) * s;
		float fall = BossDeath.EaseIn(BossDeath.Span(t, FellStart, FellLand));
		float settle = 0.07f * MathF.Sin(Mathf.Pi * BossDeath.Span(t, FellLand, FellLand + 0.24f));
		float angle = Mathf.DegToRad(82f) * fall - settle;
		fellLeft.Offset = new Vector2(shudder - part, 0f);
		fellRight.Offset = new Vector2(shudder + part, 0f);
		fellLeft.Rotation = -angle;
		fellRight.Rotation = angle;
		fellRoots.Offset = new Vector2(shudder * 0.5f, 0f);

		// Then the wood goes cold and is gone - the roots last, where it stood.
		float dim = BossDeath.Span(t, 1.25f, 2.0f);
		var wood = new Color(1f - 0.5f * dim, 1f - 0.48f * dim, 1f - 0.4f * dim, 1f - BossDeath.Span(t, 1.85f, 2.45f));
		fellLeft.Color = wood;
		fellRight.Color = wood;
		fellRoots.Color = new Color(wood, 1f - BossDeath.Span(t, 2.05f, DeathSeconds));

		if (death.Crossed(0.16f))
		{
			// The crown sheds as the seam runs: dead leaves into the air, drifting down for two seconds.
			death.Emit(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.Leaf, Frames = 4, Fps = 7f, Tinted = true,
				SpeedMin = 20f, SpeedMax = 120f, UpMin = 40f, UpMax = 170f, Gravity = 260f, Terminal = 46f,
				Drag = 1.4f, Flutter = 7f, FlutterHz = 1.4f, Bounce = 0f, Friction = 12f,
				Life = 2.45f, Fade = 0.5f, DepthJitter = 44f,
			}, 30, new Vector2(48f, 18f), new Vector2(42f, 14f), 0f, 0.008f);
		}

		if (death.Crossed(0.46f))
		{
			// The hollow where the ribcage was is open now, and what the tree had grown over falls out.
			death.Emit(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.Bone, SpeedMin = 20f, SpeedMax = 70f, UpMin = 30f, UpMax = 90f,
				Angle = Mathf.Pi * 0.5f, Spread = Mathf.Pi, Spin = 5f, Bounce = 0.3f, Life = 2.1f, Fade = 0.4f,
			}, 4, new Vector2(48f, 68f), new Vector2(3f, 8f));
		}

		if (death.Crossed(FellLand))
		{
			// Each half lands on its crown: bark off the trunk, the leaves still on it shaken loose, dust.
			foreach (BossDeath.Piece half in new[] { fellLeft, fellRight })
			{
				bool left = half == fellLeft;
				Vector2 crown = death.PieceToLocal(half, new Vector2(left ? 22f : 74f, 20f));
				var onGround = new Vector2(crown.X, death.Foot.Y - 6f);
				float height = Mathf.Max(0f, onGround.Y - crown.Y);
				death.EmitAt(new BossDeath.Throw
				{
					Cell = BossDeath.Cell.BarkLong, Variants = 2, Tinted = true, SpeedMin = 60f, SpeedMax = 170f,
					UpMin = 90f, UpMax = 210f, Spin = 6f, Bounce = 0.35f, Life = 1.5f, Fade = 0.4f,
				}, 7, onGround, height * 0.3f, 40f);
				death.EmitAt(new BossDeath.Throw
				{
					Cell = BossDeath.Cell.Leaf, Frames = 4, Fps = 7f, Tinted = true,
					SpeedMin = 10f, SpeedMax = 70f, UpMin = 60f, UpMax = 150f, Gravity = 240f, Terminal = 44f,
					Drag = 1.6f, Flutter = 6f, Bounce = 0f, Life = 1.6f, Fade = 0.45f,
				}, 10, onGround, height * 0.6f, 50f);
				Vector2 thud = death.PieceToLocal(half, new Vector2(left ? 30f : 66f, 40f));
				death.DustRing(new Vector2(thud.X, death.Foot.Y - 8f), 56f, 8, FellDust, 0.8f, 16f);
			}
		}
	}

	/// <summary>On the ground, under every actor (absolute z -15): stains, scorches, frost.</summary>
	protected internal virtual void DrawDeathGround(BossDeath death, CanvasItem canvas, float t) { }

	/// <summary>In the air, over the body: anything that is not light.</summary>
	protected internal virtual void DrawDeathAir(BossDeath death, CanvasItem canvas, float t) { }

	/// <summary>Light, drawn additively over everything: glows, seams, sparks.</summary>
	protected internal virtual void DrawDeathLight(BossDeath death, CanvasItem canvas, float t)
	{
		if (fellLeft == null || t > 0.6f)
			return;

		// The mark leaves through the seam: a line of cold light from the crown to the roots, racing
		// down, then dying as the halves part. Jagged by a whole art pixel every few rows - a split, not
		// a ruled line.
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		float run = BossDeath.EaseOut(BossDeath.Span(t, 0f, 0.3f));
		float fade = 1f - BossDeath.Span(t, 0.32f, 0.6f);
		int bottom = 6 + (int)((FellRootRow - 6) * run);
		for (int row = 6; row < bottom; row += 2)
		{
			float jag = MathF.Round(BossDeath.Hash(7u, row / 4) * 2f - 1f);
			Vector2 at = death.ArtToLocal(new Vector2(47.5f + jag, row));
			canvas.DrawRect(WhipLimb.Cell(at, BossDeath.Px * 2f), new Color(arcane.Mid, 0.75f * fade));
			canvas.DrawRect(WhipLimb.Cell(at, BossDeath.Px), new Color(arcane.Core, fade));
		}

		// And the two points of it in the knot flare once and go out.
		float eyes = BossDeath.Span(t, 0f, 0.08f) * (1f - BossDeath.Span(t, 0.12f, 0.4f));
		BossDeath.Disc(canvas, death.ArtToLocal(new Vector2(45f, 42f)), 6f, 6f, new Color(arcane.Hot, 0.8f * eyes));
		BossDeath.Disc(canvas, death.ArtToLocal(new Vector2(51f, 42f)), 6f, 6f, new Color(arcane.Hot, 0.8f * eyes));
	}
}
