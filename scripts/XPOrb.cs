using Godot;
using System;
using TheOneHeMissed.scripts;

public partial class XPOrb : Area2D
{
	[Export] public int Value { get; set; } = 2;
	[Export] public float AttractDistance { get; set; } = 80f;
	[Export] public float AttractSpeed { get; set; } = 200f;
	[Export] public float HoverAmplitude { get; set; } = 2.0f;
	[Export] public float HoverSpeed { get; set; } = 3.2f;

	// Value thresholds for the three art tiers. Enemy.DropXp drops 2 for an ordinary kill (its 3 and
	// 5 branches never fire - .ai/run-pacing.md section 7), 10 for a greater orb, 16 for the bonus
	// table's orb and 50 for anything IsMiniBoss, so small / medium / large split exactly there.
	[Export] public int MediumOrbValue { get; set; } = 5;
	[Export] public int LargeOrbValue { get; set; } = 25;

	private CharacterBody2D? player = null;
	private bool attracted = false;
	// How long this orb has been chasing, and how hard the chase ramps. AttractSpeed (200) starts
	// under the wizard's own walk (Player.Speed is 220 before any boon), so a flat-speed orb could
	// trail a moving player forever (Eric, 2026-11-06): it speeds up until it finally wins.
	private float chaseSeconds;
	private const float ChaseAcceleration = 240f;
	private AnimatedSprite2D? sprite = null;
	private Vector2 spriteBasePosition = Vector2.Zero;
	// The orb art is authored 16x16 and drawn at the contract's x2, which the scene sets. The
	// script no longer touches Scale at all - see UpdateIdleMotion.
	private Vector2 spriteBaseScale = new Vector2(2.0f, 2.0f);
	private float motionTime = 0f;
	private float motionPhase = 0f;

	/// <summary>Every orb in the world, for Node2DGame.SweepFarXpOrbs.</summary>
	public const string GroupName = "xp_orbs";

	public override void _Ready()
	{
		AddToGroup(GroupName);
		// Fallen within the Tithe-Reeve's reach while his scale is taking its due (TitheScaleEvent): this one is his.
		if (GranaryTithe.Claims(GlobalPosition))
			BeginTithe();
		motionPhase = (GetInstanceId() % 29) * 0.19f;
		sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite != null)
		{
			sprite.Scale = spriteBaseScale;
			spriteBasePosition = sprite.Position;
			sprite.Play(TierAnimation());
		}
		var cs = GetNode<CollisionShape2D>("CollisionShape2D");
		if (cs != null)
			cs.Disabled = false;
		Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
	}

	public override void _Process(double delta)
	{
		var perfStart = FrameProfiler.BeginSampledFrame();
		motionTime += (float)delta;
		UpdateIdleMotion();

		if (player == null)
		{
			var first = GetTree().GetFirstNodeInGroup("player");
			if (first is CharacterBody2D cb) player = cb;
		}
		if (tithed)
		{
			TickTithe((float)delta);
			FrameProfiler.End(FrameProfiler.Section.XpOrbs, perfStart);
			return;
		}
		if (player != null)
		{
			float dynamicAttractDistance = AttractDistance;
			if (player is Player typedPlayer)
			{
				// Every magnet bonus and Surveyor's share of the whole (Player.PickupReach).
				dynamicAttractDistance = typedPlayer.PickupReach(AttractDistance);
			}

			var dist = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (dist < dynamicAttractDistance) attracted = true;
			if (attracted)
			{
				float speed = AttractSpeed + ChaseAcceleration * chaseSeconds;
				chaseSeconds += (float)delta;
				if (gleanSpeed > 0f)
				{
					gleanSpeed = Mathf.Min(GleanMaxSpeed, gleanSpeed + GleanAcceleration * (float)delta);
					// The glean keeps its own ramp; the chase ramp stands in wherever it is faster.
					speed = Mathf.Max(speed, gleanSpeed);
				}
				float step = speed * (float)delta;
				// Never past him: at a gleaned orb's speed one frame's step can be longer than what is
				// left, and an orb that overshoots swings back and forth across him instead of landing.
				if (dist <= step)
					GlobalPosition = player.GlobalPosition;
				else
					GlobalPosition += (player.GlobalPosition - GlobalPosition) / dist * step;
			}
		}
		FrameProfiler.End(FrameProfiler.Section.XpOrbs, perfStart);
	}

	/// <summary>
	/// Take another orb's value. Node2DGame folds orbs left far behind on an endless stage into
	/// one, so the XP stays in the world without one live node per kill the player walked away from.
	/// </summary>
	public void Absorb(int value)
	{
		Value += value;
		if (sprite != null)
			sprite.Play(TierAnimation());
	}

	/// <summary>
	/// Fly to the player from wherever this orb is, as if it had come into pickup range. The Spirit
	/// Mirror (John Dee's Apotheosis) pulls every orb inside its well's reach to him when it closes.
	/// </summary>
	public void PullToPlayer()
	{
		// The reeve's scale holds what it has taken; nothing of his calls it back (GranaryTithe).
		if (!tithed)
			attracted = true;
	}

	// THE TITHE (Medea's prison, TitheScaleEvent; .ai/chapter-wizards.md 4.2). "From arrival, every XP orb that falls
	// within 320 px of it drifts into its pan instead of to him." An orb that comes into the world inside the reach of
	// the Tithe-Reeve's scale while it is taking its due (GranaryTithe.Claims, read once, as it lands) is the reeve's:
	// it lifts off the stubble and drifts up into the pan on its side of the post (Medea's balance: left of it the left
	// pan, right the right), slow at first and quickening, over every body as it goes; his magnet, his feet, the
	// Gleaning Bell and the Spirit Mirror do not touch it. In the pan it is weighed at what it was worth to him at that
	// moment of the run (Player.XpWorthOf) and is gone. If the scale stops taking before it gets there - held level,
	// or the boss come - it falls back to the ground an ordinary orb, and is his. A bool, a float and an int an orb;
	// nothing allocated.
	[Export] public float TitheStartSpeed { get; set; } = 70f;
	[Export] public float TitheAcceleration { get; set; } = 520f;
	[Export] public float TitheMaxSpeed { get; set; } = 620f;
	private bool tithed;
	private float titheSpeed;
	// Which pan it is bound for (Medea's balance, unlock-puzzles.md 3.5): the side of the post it LANDED on, -1 left,
	// +1 right, decided once as it is claimed - where he fought, not where it drifts.
	private int titheSide = 1;

	/// <summary>True while this orb is drifting into the Tithe-Reeve's pan.</summary>
	public bool Tithed => tithed;

	private void BeginTithe()
	{
		tithed = true;
		titheSide = GranaryTithe.SideOf(GlobalPosition);
		titheSpeed = TitheStartSpeed;
		attracted = false;
		GranaryTithe.OrbsInFlight++;
		ZIndex = DrawLayers.Air;
		ZAsRelative = false;
	}

	private void TickTithe(float delta)
	{
		if (!GranaryTithe.Open)
		{
			// The scale has stopped taking: it drops back to the ground where it is, an ordinary orb again.
			tithed = false;
			GranaryTithe.OrbsInFlight = Math.Max(0, GranaryTithe.OrbsInFlight - 1);
			ZIndex = 0;
			ZAsRelative = true;
			return;
		}
		titheSpeed = Mathf.Min(TitheMaxSpeed, titheSpeed + TitheAcceleration * delta);
		Vector2 pan = GranaryTithe.PanFor(titheSide);
		float dist = GlobalPosition.DistanceTo(pan);
		float step = titheSpeed * delta;
		if (dist > step)
		{
			GlobalPosition += (pan - GlobalPosition) / dist * step;
			// Where it is now, for the stream of motes the event draws between it and its pan (item 61).
			GranaryTithe.ReportFlight(GlobalPosition, titheSide);
			return;
		}
		GlobalPosition = pan;
		tithed = false;
		GranaryTithe.OrbsInFlight = Math.Max(0, GranaryTithe.OrbsInFlight - 1);
		GranaryTithe.Pay(player is Player weigher && IsInstanceValid(weigher) ? weigher.XpWorthOf(Value) : Value, Value, titheSide);
		QueueFree();
	}

	// THE GLEANING BELL's pull (GleaningBell.cs). PullToPlayer flies an orb in at the magnet's 200 px a
	// second, which is right for the few inside a well's reach and hopeless for the whole map: an orb
	// four screens off would take twenty seconds and the far bank (Node2DGame.SweepFarXpOrbs) longer.
	// A gleaned orb starts at that speed and gains GleanAcceleration every second, so the near ones
	// land first and the far ones stream in behind them - 1000 px in about a second, the far bank at
	// 4200 in about two - which is the moment: everything he walked past, coming home in order.
	// Nothing is allocated and nothing runs that did not already; it is one float per orb.
	[Export] public float GleanAcceleration { get; set; } = 1800f;
	[Export] public float GleanMaxSpeed { get; set; } = 3200f;
	private float gleanSpeed;

	/// <summary>True while this orb is flying in to the Gleaning Bell's call.</summary>
	public bool Gleaned => gleanSpeed > 0f;

	/// <summary>
	/// Fly to the player from anywhere on the map, gathering speed the whole way (the Gleaning Bell).
	/// The orb is lifted into the air for the flight, over every body and tree, so the stream reads
	/// as one thing crossing the field rather than as orbs sorted in among the crowd.
	/// </summary>
	public void Glean()
	{
		// Not even the bell calls back what the reeve has taken (GranaryTithe).
		if (tithed)
			return;
		attracted = true;
		if (gleanSpeed <= 0f)
			gleanSpeed = Mathf.Max(1f, AttractSpeed);
		ZIndex = DrawLayers.Air;
		ZAsRelative = false;
	}

	/// <summary>Which of the three tiers this orb's value puts it in. Bigger and a different
	/// hue, because size alone does not survive a crowded screen and hue alone does not survive
	/// being 16px: two orbs at different distances already look like different sizes.</summary>
	private string TierAnimation()
	{
		if (Value >= LargeOrbValue) return "large";
		if (Value >= MediumOrbValue) return "medium";
		return "small";
	}

	private void UpdateIdleMotion()
	{
		if (sprite == null)
			return;

		// The breath and the spin are in the FRAMES now, not in the transform. Rotating and
		// scaling a 16px pixel-art sprite rendered at x2 with nearest filtering is exactly the
		// sub-pixel crawl .ai/art-direction.md exists to prevent: the edge boils and the orb
		// shimmers against a floor that is holding perfectly still. Rotation is gone; Scale is
		// set once in _Ready and never touched again.
		//
		// The hover survives because it is a translation, but it is SNAPPED TO WHOLE PIXELS.
		// A float offset resamples the sprite on every frame, which is the same bug wearing a
		// different hat - and at x2 render, one art pixel is two device pixels, so quantising to
		// 2.0 keeps the orb on the same grid as everything else it sits next to.
		float hoverWave = Mathf.Sin((motionTime * HoverSpeed) + motionPhase);
		float attractBlend = attracted ? 0.45f : 1.0f;
		float offset = Mathf.Round(hoverWave * HoverAmplitude * attractBlend) * 2.0f;
		sprite.Position = spriteBasePosition + new Vector2(0f, offset);
	}

	private void OnBodyEntered(Node body)
	{
		// The reeve's, on its way to his pan: walking through it does not take it back.
		if (tithed)
			return;
		if (body.IsInGroup("player") && body.HasMethod("AddXp"))
		{
			body.Call("AddXp", Value);
			SfxPlayer.PickupXp(GlobalPosition);
			QueueFree();
		}
	}
}
