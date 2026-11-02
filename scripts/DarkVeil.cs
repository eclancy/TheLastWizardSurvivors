using Godot;
using WizardSurvivors.scripts;

// HIS LIGHT (.ai/endgame-castle.md 6.4, phase 2): "The hall goes dark but for him and the player." One quad
// the size of what the camera sees, following it, drawn through scenes/shaders/dark_veil.gdshader: the
// floor and the walls go to his dark, with a pool of light round the wizard and one round him.
//
// UNDER EVERY TELL AND EVERY BODY. It sits on the floor (between DrawLayers.Ground and GroundTell), so the
// warnings painted on the ground - his lines, the wardens' shapes, every ring - stay on top of it, and
// every body, bolt, chest and orb above it stays lit. Only the room goes dark; nothing the player has to
// read does. That is the fairness rule the boss tells keep everywhere else, kept here too.
//
// It comes in over a second and a half, and when he falls it goes out with his light (Lift), freeing itself.
public partial class DarkVeil : Node2D
{
	private const string ShaderPath = "res://scenes/shaders/dark_veil.gdshader";
	private const float FadeSeconds = 1.5f;
	// What the camera sees is covered with this much to spare, so a shake or a push-in never shows an edge.
	private const float Margin = 1.3f;

	private ThroneDarkWizardBoss source;
	private ShaderMaterial material;
	private float strength;
	private float target = 1f;
	private Vector2 halfSize = new(800f, 800f);

	private static readonly StringName LightA = "light_a";
	private static readonly StringName LightB = "light_b";
	private static readonly StringName Strength = "strength";

	/// <summary>True while it is on or coming on (not lifted).</summary>
	public bool Shown => target > 0f;

	/// <summary>How dark it is now, 0 to 1. For the tools.</summary>
	public float Darkness => strength;

	public static DarkVeil Cast(Node arena, ThroneDarkWizardBoss boss)
	{
		if (arena == null || !IsInstanceValid(arena))
			return null;
		Shader shader = ResourceLoader.Exists(ShaderPath) ? GD.Load<Shader>(ShaderPath) : null;
		if (shader == null)
		{
			GD.PushError($"DarkVeil: no shader at '{ShaderPath}'; the hall cannot go dark.");
			return null;
		}
		var veil = new DarkVeil
		{
			Name = "DarkVeil",
			source = boss,
			ZAsRelative = false,
			ZIndex = DrawLayers.GroundTell - 1,
		};
		veil.material = new ShaderMaterial { Shader = shader };
		veil.Material = veil.material;
		arena.AddChild(veil);
		return veil;
	}

	/// <summary>His light goes out: the dark lifts, and the node frees itself when it has.</summary>
	public void Lift() => target = 0f;

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		strength = Mathf.MoveToward(strength, target, dt / FadeSeconds);
		if (target <= 0f && strength <= 0f)
		{
			QueueFree();
			return;
		}

		Viewport viewport = GetViewport();
		Camera2D camera = viewport?.GetCamera2D();
		if (camera != null)
		{
			Vector2 zoom = camera.Zoom;
			Vector2 seen = viewport.GetVisibleRect().Size / new Vector2(Mathf.Max(0.05f, zoom.X), Mathf.Max(0.05f, zoom.Y));
			Vector2 half = seen * 0.5f * Margin;
			if (!half.IsEqualApprox(halfSize))
			{
				halfSize = half;
				QueueRedraw();
			}
			GlobalPosition = camera.GetScreenCenterPosition();
		}

		Player wizard = GetTree().GetFirstNodeInGroup("player") as Player;
		Vector2 a = wizard != null ? wizard.GlobalPosition : GlobalPosition;
		// His pool is round his whole height, from his feet up: his centre is mid-body.
		Vector2 b = source != null && IsInstanceValid(source) ? source.GlobalPosition + new Vector2(0f, 40f) : a;
		material.SetShaderParameter(LightA, a);
		material.SetShaderParameter(LightB, b);
		material.SetShaderParameter(Strength, strength);
	}

	public override void _Draw() => DrawRect(new Rect2(-halfSize, halfSize * 2f), Colors.White);
}
