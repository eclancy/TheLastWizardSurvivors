using Godot;
using TheOneHeMissed.scripts;

// Suicide detonation (issue #33). It runs at the player faster than an ordinary chaser, and once it
// is close it plants, marks its blast on the ground with a starburst filling over its head, and blows
// itself apart.
//
// What it adds is a cost to letting things reach you. The swarm's ordinary answer - stand in a good
// spot and let contact damage tick while your spells clear the pile - stops being free, because one
// of the things in that pile is counting down. It makes spacing a decision rather than a habit.
//
// Two rules keep it fair, and both are deliberate:
//
// - Killing it during the wind-up cancels the blast entirely. Enemy.StartDeath turns physics off,
//   so the telegraph simply stops ticking and never resolves. An exploder that detonated on death
//   would punish the player for correctly identifying and bursting the dangerous thing, which is
//   the exact opposite of what this enemy is for.
// - It drops nothing when it detonates. The player did not kill it, so it does not pay out - and a
//   detonation that fed the XP curve would quietly make ignoring it the optimal play.
public partial class ExploderEnemy : Enemy
{
	[Export] public float FuseSeconds { get; set; } = 0.85f;
	[Export] public float BlastRadius { get; set; } = 110f;
	[Export] public int BlastDamage { get; set; } = 5;

	/// <summary>How close it must get before the fuse starts, as a multiple of the blast radius.</summary>
	[Export] public float TriggerRadiusMultiplier { get; set; } = 0.85f;

	[Export] public Color BlastWarningColor { get; set; } = new Color(0.95f, 0.35f, 0.35f);

	/// <summary>Seconds the spent blast ring lingers. The body is freed once this and its death strip are both done.</summary>
	[Export] public float BlastLingerSeconds { get; set; } = 0.3f;

	private GroundSlamAttack blast;
	private bool detonated;
	private float lingerRemaining;

	public override void _Ready()
	{
		base._Ready();
		blast = BuildBlast();
	}

	// The interval is the shortest the telegraph allows rather than something long meaning "once".
	// AttackTelegraph deliberately starts on a full cooldown so nothing attacks the frame it
	// spawns, so a nominal interval of 9999 does not mean "fires once" - it means the fuse is
	// never lit at all, and the enemy is a slightly fast chaser forever. The fuse only ever burns
	// once because Detonate ends the enemy, not because the cooldown is long.
	private GroundSlamAttack BuildBlast()
	{
		return new GroundSlamAttack(0.05f, FuseSeconds, BlastRadius, BlastDamage, BlastWarningColor)
		{
			GlyphOverride = TelegraphGlyph.Burst,
			Foreseeable = true,
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		if (detonated)
		{
			// The body is inert but still on screen: age the ring out by hand, because base
			// _PhysicsProcess - which is what normally drives QueueRedraw - is skipped below.
			lingerRemaining -= (float)delta;
			spentSeconds += (float)delta;
			blast.Tick((float)delta, false);
			// Enemy's own step, which ticks the icon, is skipped below: the burst still has to flash.
			TickAttackIcon((float)delta);
			RedrawTells();
			bool bodyDone = TickSpentBody();
			if (lingerRemaining <= 0f && bodyDone)
				QueueFree();
			return;
		}

		base._PhysicsProcess(delta);
		TickFuse((float)delta);
	}

	// Planted once the fuse is lit. It must not drift: the ring is drawn at its feet and the blast
	// is measured from them, so a sliding exploder would detonate somewhere it never warned about.
	protected override Vector2 AdjustSteering(Vector2 chaseDirection, float distanceToPlayer)
	{
		return blast != null && blast.IsWindingUp ? Vector2.Zero : chaseDirection;
	}

	private void TickFuse(float delta)
	{
		Node2D target = TargetPlayer;
		bool inTriggerRange = target != null
			&& IsInstanceValid(target)
			&& GlobalPosition.DistanceTo(target.GlobalPosition) <= BlastRadius * TriggerRadiusMultiplier;

		// Blinded (Enemy.ApplyBlind, the Eye of Ra): it has lost him, so it begins nothing; one already begun still lands.
		switch (blast.Tick(delta, inTriggerRange && !IsBlinded))
		{
			case AttackTelegraph.Beat.Started:
				PlayAttackAnimation();
				// Hot and bright for the length of the fuse, so an exploder mid-crowd is findable
				// by colour as well as by the starburst over it.
				coldModulate = BaseModulate;
				SetBaseModulate(new Color(1.0f, 0.55f, 0.45f));
				break;
			case AttackTelegraph.Beat.Resolved:
				Detonate();
				break;
		}
	}

	private void Detonate()
	{
		if (detonated)
			return;

		detonated = true;
		// The inert body no longer runs the base physics step that eases a hit flash back.
		EndHitFlash();
		lingerRemaining = Mathf.Max(0.05f, BlastLingerSeconds);
		blast.ResolveAgainst(this, TargetPlayer);

		// Leave the fight in the same order Enemy.StartDeath does - out of the group first, so an
		// AoE sweep or auto-target running later this frame skips a corpse that is already spent -
		// but without its rewards. See the class comment.
		EnemyRegistry.Leave(this);
		Velocity = Vector2.Zero;
		CollisionLayer = 0;
		CollisionMask = 0;
		BeginSpentBody();
	}

	// THE BODY AFTER THE BLAST (in-flight 104, Eric 2026-11-10: "there are enemies that blow themselves up. They
	// should have an animation to show them dying, however they go about doing that"). It used to stand inert in
	// its walk pose until the ring aged out and then vanish. Now it plays its sheet's death strip - every exploder
	// sheet carries a six-frame one (the Grave Bloat and all twelve chapter bodies, 2026-11-13) - and is freed only
	// when both the strip and the ring are done. A sheet without a strip (or with a looping one, which would never
	// end) bursts instead: its own art swelling and fading in BurstSeconds.
	//
	// The strip is stepped by hand rather than played. Play would raise AnimationFinished, and Enemy's one-shot
	// handler puts any body that is not dying back on its walk - which a detonated exploder is not (its rewards are
	// withheld, so it never runs StartDeath), so the corpse would stand up again for the frames until its free.
	private static readonly StringName DeathStrip = "death";
	private const float BurstSeconds = 0.28f;
	private const float BurstSwell = 0.6f;
	private float spentSeconds;
	private float stripSeconds;
	private Vector2 burstRestScale = Vector2.One;

	private void BeginSpentBody()
	{
		spentSeconds = 0f;
		stripSeconds = 0f;
		AnimatedSprite2D body = BodySprite;
		if (body == null)
			return;
		SpriteFrames frames = body.SpriteFrames;
		if (frames != null && frames.HasAnimation(DeathStrip) && !frames.GetAnimationLoop(DeathStrip)
			&& frames.GetFrameCount(DeathStrip) > 0 && frames.GetAnimationSpeed(DeathStrip) > 0)
		{
			double units = 0;
			for (int i = 0; i < frames.GetFrameCount(DeathStrip); i++)
				units += frames.GetFrameDuration(DeathStrip, i);
			stripSeconds = (float)(units / frames.GetAnimationSpeed(DeathStrip));
			body.Stop();
			body.Animation = DeathStrip;
			body.Frame = 0;
			return;
		}
		burstRestScale = body.Scale;
	}

	// True once the body has finished showing its end (the strip's last frame reached, or the burst faded out).
	private bool TickSpentBody()
	{
		AnimatedSprite2D body = BodySprite;
		if (body == null)
			return true;
		if (stripSeconds > 0f)
		{
			SpriteFrames frames = body.SpriteFrames;
			int count = frames.GetFrameCount(DeathStrip);
			double elapsedUnits = spentSeconds * frames.GetAnimationSpeed(DeathStrip);
			int frame = 0;
			double reached = 0;
			while (frame < count - 1 && reached + frames.GetFrameDuration(DeathStrip, frame) <= elapsedUnits)
			{
				reached += frames.GetFrameDuration(DeathStrip, frame);
				frame++;
			}
			if (body.Frame != frame)
				body.Frame = frame;
			return spentSeconds >= stripSeconds;
		}
		float k = Mathf.Clamp(spentSeconds / BurstSeconds, 0f, 1f);
		body.Scale = burstRestScale * (1f + BurstSwell * k);
		body.Modulate = new Color(body.Modulate, 1f - k);
		return k >= 1f;
	}

	public override void ResetForRespawn(Vector2 newPos, int newHealth)
	{
		base.ResetForRespawn(newPos, newHealth);
		blast = BuildBlast();
		detonated = false;
		lingerRemaining = 0f;
		spentSeconds = 0f;
		stripSeconds = 0f;
	}

	// The blast's reach is a dashed ring on the floor, under every body (Enemy.DrawGroundTells); the fuse
	// is the starburst over its head, filling as it burns.
	protected override void DrawGroundTells(CanvasItem floor) => blast?.Draw(floor);

	// The fuse put out by a star of The Quatrains: it cools to its own colour and never bursts. Its interval is
	// the shortest there is (BuildBlast), so one still inside its trigger reach lights a NEW fuse at once - a whole
	// fresh fuse, shown from its first frame, which is all a cancel may ever give back.
	private Color coldModulate = Colors.White;

	protected override bool CancelWindUp()
	{
		if (detonated || blast == null || !blast.Cancel())
			return false;
		SetBaseModulate(coldModulate);
		return true;
	}

	protected override bool ReadAttackTell(out TelegraphGlyph glyph, out float progress)
	{
		glyph = TelegraphGlyph.Burst;
		progress = blast?.WindUpProgress ?? 0f;
		return blast != null && blast.IsWindingUp;
	}
}
