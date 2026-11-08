using Godot;
using TheOneHeMissed.scripts;

/// <summary>
/// The quiet grace after a level-up or chest pick, made visible (in-flight item 23, Eric
/// 2026-11-05): a small arcane ward forming round the wizard while nothing can hurt him, dissolving
/// away exactly as the protection ends. It reads the protection it shows
/// (Player.InvincibilitySecondsRemaining) every frame rather than running a clock of its own, so a
/// grace extended while it is up - a chest menu straight after a level-up - keeps the ward up too.
/// </summary>
/// <remarks>
/// Deliberately not Aegis Ward: well under half that dome's reach (22 against 52), the Arcane
/// ramp's violet rather than Light's white-gold, and a thin ring rather than a dome - a courtesy
/// mark, never a shield spell gained. Drawn with PixelInk on the world's 2 px art grid, arriving
/// and leaving through the Bayer dissolve (.ai/art-direction.md section 3). ForemostAir, as a
/// status mark on the wizard is (DrawLayers): never buried under him, a boss or a spell. One node,
/// one ring and two glints a frame, alive only while the grace is (.ai/performance.md).
/// </remarks>
public partial class PostPickWard : Node2D
{
	private const float Radius = 22f;
	// The ring is a touch flatter than round: a hint of the dome seen from the game's high angle.
	private const float Squash = 0.92f;
	private const float FormSeconds = 0.3f;
	/// <summary>How long before the protection's end the dissolve starts: the ward is gone on the
	/// frame the grace is, so what the ring promises never outlives what the player has.</summary>
	private const float FadeSeconds = 0.4f;
	private const float GlintOrbitRadiansPerSecond = 1.7f;

	private static readonly ElementColors.Ramp Arcane = ElementColors.GetRamp(Element.Arcane);

	private Player player;
	private float age;

	/// <summary>Puts the ward on <paramref name="player"/>, or leaves the one already there alone:
	/// an extended grace reaches the ward through the player, not through a second node.</summary>
	public static void Show(Player player)
	{
		if (player == null || !IsInstanceValid(player) || player.GetNodeOrNull("PostPickWard") != null)
			return;
		player.AddChild(new PostPickWard { Name = "PostPickWard", player = player });
	}

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = DrawLayers.ForemostAir;
	}

	public override void _Process(double delta)
	{
		if (player == null || !IsInstanceValid(player) || player.IsDead
			|| player.InvincibilitySecondsRemaining <= 0f)
		{
			QueueFree();
			return;
		}
		age += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (player == null || !IsInstanceValid(player))
			return;
		float form = Mathf.Clamp(age / FormSeconds, 0f, 1f);
		float fade = Mathf.Clamp(player.InvincibilitySecondsRemaining / FadeSeconds, 0f, 1f);
		float coverage = Mathf.Min(form, fade);
		if (coverage <= 0f)
			return;
		// Forming, it settles inward onto its radius; ending, it slips outward as it dissolves.
		float radius = Radius + (1f - form) * 8f + (1f - fade) * 6f;
		PixelInk.Ring(this, Vector2.Zero, radius, radius * Squash, Arcane.Mid, 1, coverage * 0.75f);
		// Two glints chasing round the ring say "still live" without the ring pulsing or growing.
		for (int glint = 0; glint < 2; glint++)
		{
			float head = age * GlintOrbitRadiansPerSecond + glint * Mathf.Pi;
			for (int step = 0; step < 4; step++)
			{
				float angle = head - step * 0.12f;
				var at = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * Squash);
				PixelInk.Cell(this, at, step == 0 ? Arcane.Core : Arcane.Hot, coverage * (1f - step * 0.2f));
			}
		}
	}
}
