using Godot;
using TheOneHeMissed.scripts;

// PAINT WHAT IS NEEDED, the moment it pays (Ma Liang's passive, Player.MaLiang.cs; .ai/chapter-wizards.md 4.8). A flask
// is painted on the ground beside him: four strokes of his brush's light laid one after another - the belly, the
// neck, the stopper, the line of the dose across it - each drawn along its length as a brush draws, and where the last
// one closes, the drawing IS a flask: the ordinary HealthPickup appears on it and comes to him as any flask does, and
// the ink fades off it.
//
// The ink is the arcane ramp, stamped on the world's 2 px grid (the sprites' pixel), so it reads as drawn in the
// game's own pixels and not as a vector line; over the bodies (DrawLayers.Air) so a crowd round him at a quarter of his
// health cannot hide the one thing that is for him. Code-created, no scene; it frees itself.
public partial class PaintedFlask : Node2D
{
	/// <summary>How long the four strokes take to draw: quick, but slow enough to be seen being drawn.</summary>
	public const float PaintSeconds = 0.55f;

	/// <summary>How long the ink lingers on the flask once it is real, fading.</summary>
	public const float FadeSeconds = 0.6f;

	// The flask in world px about its foot (y up is negative): a round belly, a neck, a stopper, and the level of the
	// dose. Each stroke a polyline, drawn in order.
	private static readonly Vector2[][] Strokes =
	{
		// The belly, from the near shoulder round the bottom to the far shoulder.
		new[]
		{
			new Vector2(-5f, -20f), new Vector2(-10f, -16f), new Vector2(-12f, -10f), new Vector2(-11f, -4f),
			new Vector2(-6f, 0f), new Vector2(0f, 1f), new Vector2(6f, 0f), new Vector2(11f, -4f),
			new Vector2(12f, -10f), new Vector2(10f, -16f), new Vector2(5f, -20f),
		},
		// The neck, both sides up.
		new[] { new Vector2(-5f, -20f), new Vector2(-4f, -28f), new Vector2(4f, -28f), new Vector2(5f, -20f) },
		// The stopper.
		new[] { new Vector2(-3f, -28f), new Vector2(-3f, -32f), new Vector2(3f, -32f), new Vector2(3f, -28f) },
		// The dose: a level across the belly, with a wave in it.
		new[] { new Vector2(-10f, -9f), new Vector2(-5f, -11f), new Vector2(0f, -9f), new Vector2(5f, -11f), new Vector2(10f, -9f) },
	};

	private static readonly Color[] Ink =
	{
		new(0.910f, 0.753f, 1.0f),   // arcane hot (#E8C0FF)
		new(0.753f, 0.549f, 1.0f),   // arcane mid (#C08CFF)
		new(0.478f, 0.247f, 0.831f), // arcane edge (#7A3FD4)
	};

	private float age;
	private bool real;
	private float totalLength;

	public override void _Ready()
	{
		ZIndex = DrawLayers.Air;
		ZAsRelative = false;
		foreach (Vector2[] stroke in Strokes)
			totalLength += Length(stroke);
		SfxPlayer.AtPosition(SfxCatalog.HintBrush, GlobalPosition, 0.04f);
	}

	private static float Length(Vector2[] stroke)
	{
		float l = 0f;
		for (int i = 1; i < stroke.Length; i++)
			l += stroke[i - 1].DistanceTo(stroke[i]);
		return l;
	}

	public override void _Process(double delta)
	{
		age += (float)delta;
		QueueRedraw();
		if (!real && age >= PaintSeconds)
		{
			real = true;
			// The drawing is a flask: the ordinary one, so it heals, sounds and flies to him as every flask does.
			if (ResourceLoader.Load<PackedScene>("res://scenes/HealthPickup.tscn")?.Instantiate() is HealthPickup flask)
			{
				flask.Position = Position;
				GetParent()?.CallDeferred(Node.MethodName.AddChild, flask);
			}
		}
		if (age >= PaintSeconds + FadeSeconds)
			QueueFree();
	}

	public override void _Draw()
	{
		float drawn = totalLength * Mathf.Clamp(age / PaintSeconds, 0f, 1f);
		float fade = real ? Mathf.Clamp(1f - (age - PaintSeconds) / FadeSeconds, 0f, 1f) : 1f;
		float run = 0f;
		foreach (Vector2[] stroke in Strokes)
		{
			for (int i = 1; i < stroke.Length && run < drawn; i++)
			{
				Vector2 a = stroke[i - 1], b = stroke[i];
				float seg = a.DistanceTo(b);
				float take = Mathf.Min(seg, drawn - run);
				int stamps = Mathf.Max(1, Mathf.CeilToInt(take / 2f));
				for (int k = 0; k <= stamps; k++)
				{
					Vector2 p = a.Lerp(b, take / seg * k / stamps);
					Vector2 cell = new(Mathf.Floor(p.X / 2f) * 2f, Mathf.Floor(p.Y / 2f) * 2f);
					// Wet at the head of the stroke (the hot stop), settling to the mid behind it, the edge stop under it.
					bool head = run + take * k / stamps > drawn - 6f && !real;
					DrawRect(new Rect2(cell + new Vector2(0f, 2f), new Vector2(2f, 2f)), new Color(Ink[2], 0.8f * fade));
					DrawRect(new Rect2(cell, new Vector2(2f, 2f)), new Color(head ? Ink[0] : Ink[1], fade));
				}
				run += take;
			}
		}
	}
}
