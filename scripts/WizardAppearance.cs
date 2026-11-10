using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// A WIZARD FREED IN THE RUN STEPS OUT BESIDE HIM (in-flight 83; Eric, 2026-11-09: "When we unlock a character, they should
// appear on the screen for a moment, like Baba Yaga coming out of her hut").
//
// Twelve side events show the wizard they free themselves - Baba Yaga out of her hut, Morgan off her barge, Paracelsus out
// of the gibbet's cage, the seer at his desk, Imhotep out of his tomb, Medea out of the granary, Ma Liang out of the door he
// drew, Seimei out of his seal, Wayland off his anvil, the Sibyl out of her mouth in the rock, Merlin bound in the moot's
// wards and Vainamoinen through his ice (SitesWithOwnBeat). Every other wizard freed DURING a run had no moment in it, only
// the freeing reel after the run (Freeings, .ai/cutscenes.md): John Dee (his chests), Laelaps (the Pinfold), Tituba (the
// Gaoler's key: a cell door, never her), the Dark Wizard and Nimue (the Throne), Artemis and the gods (a seat's warden),
// and any deed added later that frees someone. This is their moment, one beat shared by all of them: the wizard's own
// sprite stepping out of a column of light beside the player, with his name over him, a couple of seconds, then gone
// back into the light. The reel after the run still plays: it is the freeing's cutscene, this the moment it happened.
//
// WHEN. The moment the deed is done (AchievementManager.CompleteNow, DiscoveryRewards.GrantSite call Freed), but only on
// the screen's turn (Announcements, Kind.Achievement): after the deed's own plate or line, never on top of it, never over
// the run's opening. A boss's arrival is a banner, which never waits: it suspends the beat, which hides and stops its
// clock until the banner has gone, and the beat neither starts nor runs while the field is held for an arrival
// (Player.HeldForBossEntrance). It is Pausable, so under any menu (a level-up, the pause screen) it simply waits. Once a
// run per wizard (Node2DGame.ShowWizardAppearance), and a deed frees a wizard only once a save, so it never plays twice.
// The Throne's fall goes straight to the ending, so Nimue and the Dark Wizard may get no beat at all: their reel says it.
//
// PACING (.ai/cutscenes.md, the cinematic rules): the light stands alone LightSeconds before anyone is in it (rule 3, a
// hold before), he steps out of it, stands named HoldSeconds, goes back into it, and the light thins away FadeSeconds after
// he has gone (rule 4, a beat before the screen is the run's again). Calm: no shake, no speed lines. The run is not held:
// the swarm keeps coming, so the beat is presence, not a cutscene, and if the player walks off the wizard walks with him.
//
// THE PICTURE. His own SpriteFrames (CharacterData.Frames, or the idle built from his portrait), at the world's x2 on the
// wizard cell (middle column 24, contact row 47), facing the player; the column is the side events' win column's light
// (PhantasmRise's Arcane cyan), drawn additively through ExploreArt's shaft and glow; the name in the heading face with the
// damage numbers' outline. Code-drawn, no scene, nothing allocated per frame.
public partial class WizardAppearance : Node2D
{
	/// <summary>The column alone before he steps out of it: the hold before (cinematic rule 3).</summary>
	public const float LightSeconds = 0.5f;
	/// <summary>Stepping out of the light.</summary>
	public const float StepSeconds = 0.6f;
	/// <summary>Standing beside the player, named.</summary>
	public const float HoldSeconds = 1.8f;
	/// <summary>Going back into the light.</summary>
	public const float GoSeconds = 0.6f;
	/// <summary>The column thinning away after he has gone: the hold before the screen is the run's again (rule 4).</summary>
	public const float FadeSeconds = 0.45f;

	public static float TotalSeconds => LightSeconds + StepSeconds + HoldSeconds + GoSeconds + FadeSeconds;

	/// <summary>How far to the side of the player he stands, world px: beside him, clear of his own sprite.</summary>
	public const float Beside = 64f;

	/// <summary>
	/// The side events whose own scene shows the wizard they free stepping out (or standing bound, or seen through the
	/// ice): no second beat for them. A site here must be a side event that frees a wizard (RegressionChecks).
	/// </summary>
	public static readonly IReadOnlySet<string> SitesWithOwnBeat = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"walking_hut", "the_barge", "gibbet_cage", "seers_voice", "tomb_beacons", "tithe_scale", "dotted_door",
		"five_pointed_seal", "great_anvil", "nine_mouths", "the_moot", "the_thaw",
	};

	/// <summary>True when the deed <paramref name="deedId"/> is a side event's whose own scene shows the wizard it frees.</summary>
	public static bool HasOwnBeat(string deedId)
	{
		AchievementDefinition deed = AchievementDefinitions.GetById(deedId ?? string.Empty);
		return deed != null && !string.IsNullOrWhiteSpace(deed.SiteId) && SitesWithOwnBeat.Contains(deed.SiteId);
	}

	/// <summary>
	/// A deed done in the run has just freed <paramref name="wizardId"/>: the run in progress plays his beat when the
	/// screen is free, unless the deed's own event already shows him. Nothing off a run (no Node2DGame in play).
	/// </summary>
	public static void Freed(string wizardId, string deedId)
	{
		if (string.IsNullOrWhiteSpace(wizardId) || HasOwnBeat(deedId))
			return;
		Node2DGame game = Node2DGame.Current;
		if (game == null || !IsInstanceValid(game) || !game.IsInsideTree())
			return;
		game.ShowWizardAppearance(wizardId);
	}

	// The light: PhantasmRise's Arcane cyan, the colour every freeing column is in.
	private static readonly Color Hot = new(0.863f, 0.957f, 1f);
	private static readonly Color Mid = new(0.416f, 0.722f, 0.910f);
	private static readonly Color NameInk = new(0.96f, 0.94f, 0.88f);

	// From his feet to the middle of his cell, drawn at x2 and centred: the player's own (player.tscn: the sprite at -4, the
	// feet's capsule at +36), so the two stand on one line.
	private const float FootToCentre = 40f;
	private const float ShaftHeight = 230f;
	// He keeps up with the player: once further than Slack from where he should stand, he walks there.
	private const float Slack = 24f;
	private const float WalkSpeed = 300f;

	public Player Target { get; init; }
	public CharacterData Wizard { get; init; }

	private AnimatedSprite2D body;
	private Label nameLabel;
	private Node2D lightLayer;
	private bool started;
	private float clock;
	private float side = 1f;
	private float columnAlpha;
	private bool facesLeftNative;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Pausable;
		TopLevel = true;
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		Visible = false;
		facesLeftNative = CharacterVisuals.SpriteFacesLeft(Wizard?.Id);

		lightLayer = new Node2D { Name = "Light", Material = ExploreArt.Additive, ZAsRelative = false, ZIndex = DrawLayers.Air };
		lightLayer.Draw += DrawLight;
		AddChild(lightLayer);

		body = new AnimatedSprite2D
		{
			Name = "Wizard",
			Scale = new Vector2(2f, 2f),
			Position = new Vector2(0f, -FootToCentre),
			TextureFilter = TextureFilterEnum.Nearest,
			ZAsRelative = false,
			ZIndex = DrawLayers.Air + 1,
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		if (Wizard?.Frames != null && Wizard.Frames.HasAnimation("idle"))
			body.SpriteFrames = Wizard.Frames;
		else if (CharacterVisuals.TryBuildIdleFrames(Wizard?.Portrait, out SpriteFrames built))
			body.SpriteFrames = built;
		if (body.SpriteFrames != null && body.SpriteFrames.HasAnimation("idle"))
			body.Play("idle");
		AddChild(body);

		var settings = new LabelSettings
		{
			Font = ResourceLoader.Exists(HeadingFontPath) ? GD.Load<Font>(HeadingFontPath) : ThemeDB.FallbackFont,
			FontSize = 20,
			FontColor = NameInk,
			OutlineSize = 6,
			OutlineColor = DamageNumberInk.Outline,
		};
		nameLabel = new Label
		{
			Name = "Name",
			Text = Wizard?.Name ?? string.Empty,
			LabelSettings = settings,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			Size = new Vector2(320f, 32f),
			Position = new Vector2(-160f, -FootToCentre * 2f - 44f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZAsRelative = false,
			ZIndex = DrawLayers.FloatingText,
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		AddChild(nameLabel);
	}

	private const string HeadingFontPath = "res://scenes/resources/HeadingFont.tres";

	public override void _ExitTree()
	{
		Announcements.Release(this);
		if (lightLayer != null && IsInstanceValid(lightLayer))
			lightLayer.Draw -= DrawLight;
	}

	public override void _Process(double delta)
	{
		if (Target == null || !IsInstanceValid(Target) || Target.IsDead || body?.SpriteFrames == null)
		{
			QueueFree();
			return;
		}
		if (!started)
		{
			// Never while the field is held for a boss's arrival, and only on the screen's turn.
			if (Target.HeldForBossEntrance || !Announcements.Request(this, Announcements.Kind.Achievement))
				return;
			started = true;
			side = ClearerSide();
			GlobalPosition = Snap(Target.FootPosition + new Vector2(side * Beside, 0f));
		}
		// A banner over it, or an arrival: hidden, its clock stopped, its turn kept.
		bool held = Announcements.IsSuspended(this) || Target.HeldForBossEntrance;
		Visible = !held;
		if (held)
			return;

		float dt = (float)delta;
		clock += dt;
		if (clock >= TotalSeconds)
		{
			QueueFree();
			return;
		}
		Follow(dt);
		Pose();
		lightLayer.QueueRedraw();
	}

	// Beside the player, on the side with fewer of the swarm near it: one walk of the registry, once.
	private float ClearerSide()
	{
		Vector2 at = Target.GlobalPosition;
		int left = 0, right = 0;
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (enemy == null || !IsInstanceValid(enemy))
				continue;
			Vector2 d = enemy.GlobalPosition - at;
			if (d.LengthSquared() > 200f * 200f)
				continue;
			if (d.X < 0f) left++; else right++;
		}
		return right <= left ? 1f : -1f;
	}

	// He keeps up: where he should stand is beside the player; past Slack from it he walks there, and stands again when
	// he has caught up. Facing the player while he stands, the way he walks while he walks.
	private void Follow(float dt)
	{
		Vector2 want = Target.FootPosition + new Vector2(side * Beside, 0f);
		Vector2 to = want - GlobalPosition;
		float distance = to.Length();
		bool walking = distance > Slack;
		if (walking)
			GlobalPosition = Snap(GlobalPosition + to / distance * MathF.Min(distance, WalkSpeed * dt));
		string anim = walking && body.SpriteFrames.HasAnimation("moving") ? "moving" : "idle";
		if (body.Animation != anim && body.SpriteFrames.HasAnimation(anim))
			body.Play(anim);
		bool faceLeft = walking ? to.X < 0f : Target.GlobalPosition.X < GlobalPosition.X;
		body.FlipH = facesLeftNative ? !faceLeft : faceLeft;
	}

	// The beat's alphas and his step, from the clock.
	private void Pose()
	{
		float t = clock;
		float stepEnd = LightSeconds + StepSeconds;
		float holdEnd = stepEnd + HoldSeconds;
		float goEnd = holdEnd + GoSeconds;
		float him;
		float lift;
		if (t < LightSeconds)
		{
			columnAlpha = Smooth(t / LightSeconds);
			him = 0f;
			lift = -12f;
		}
		else if (t < stepEnd)
		{
			columnAlpha = 1f;
			float u = Smooth((t - LightSeconds) / StepSeconds);
			him = u;
			lift = -12f * (1f - u);
		}
		else if (t < holdEnd)
		{
			// The light settles to a glow round him while he stands.
			columnAlpha = Mathf.Lerp(1f, 0.55f, Smooth((t - stepEnd) / 0.5f));
			him = 1f;
			lift = 0f;
		}
		else if (t < goEnd)
		{
			float u = Smooth((t - holdEnd) / GoSeconds);
			columnAlpha = Mathf.Lerp(0.55f, 1f, u);
			him = 1f - u;
			lift = -16f * u;
		}
		else
		{
			columnAlpha = 1f - Smooth((t - goEnd) / FadeSeconds);
			him = 0f;
			lift = -16f;
		}
		body.Position = new Vector2(0f, Mathf.Round(-FootToCentre + lift));
		body.Modulate = new Color(1f, 1f, 1f, him);
		nameLabel.Modulate = new Color(1f, 1f, 1f, him);
	}

	private void DrawLight()
	{
		if (columnAlpha <= 0.002f)
			return;
		ExploreArt.DrawGlow(lightLayer, Vector2.Zero, 64f, new Color(Mid, 0.30f * columnAlpha));
		ExploreArt.DrawShaft(lightLayer, Vector2.Zero, 26f, ShaftHeight, new Color(Mid, 0.42f * columnAlpha));
		ExploreArt.DrawShaft(lightLayer, Vector2.Zero, 9f, ShaftHeight * 0.92f, new Color(Hot, 0.55f * columnAlpha));
	}

	private static float Smooth(float u)
	{
		u = Mathf.Clamp(u, 0f, 1f);
		return u * u * (3f - 2f * u);
	}

	// On the world's 2 px grid, as every grounded sprite is.
	private static Vector2 Snap(Vector2 p) => new(Mathf.Round(p.X * 0.5f) * 2f, Mathf.Round(p.Y * 0.5f) * 2f);
}
