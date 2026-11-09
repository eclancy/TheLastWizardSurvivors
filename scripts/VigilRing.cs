using Godot;
using TheOneHeMissed.scripts;

// THE VIGIL (.ai/endless-mode.md 1.2; EndlessCatalog, Node2DGame.Endless.cs): a ring of the wizard's own warm light
// on the ground, opened beside the way home when a chapter's warden falls in a run that can go on. Step into the
// portal and the run ends as it always has; stand in this for a moment and the night goes on.
//
// Drawn, not a sprite, in the Light ramp, as the portal is drawn in the Arcane one - his light against the arcane
// door home, so the two read as two choices and not one effect. Flat on the ground (DrawLayers.Ground, the portal
// glow's layer: under every body, over the craters' floor and a dark chapter's room pass), and it lights the dark
// round itself where the chapter has no other light (LightPool), as a warm spell does.
//
// THE HOLD. It is kept by standing in it for EndlessCatalog.VigilHoldSeconds, shown as an arc filling round the
// rim, so a player who walks across it to the chest does not keep it by accident; stepping out lets the arc fall
// back fast. And it only counts a hold that began with him stepping in: opened under his feet, it waits for him to
// step out once. There is no timer - the field is held until he chooses.
public partial class VigilRing : Node2D
{
	/// <summary>How long the ring takes to open, in real seconds: a little after the portal, so the eye meets the way home first.</summary>
	public const float OpenSeconds = 1.1f;
	// The ring on the ground, an ellipse wider than the portal's foot: a place to stand, not a door.
	public const float RadiusX = 54f;
	public const float RadiusY = 24f;
	// How fast the arc falls back when he steps out, as a multiple of how fast it fills.
	private const float DrainRate = 3f;
	private const int Embers = 10;

	private float age;
	private float hold;
	private bool armed;
	private bool startedInside;
	private bool firstFrame = true;
	private Color core, hot, mid, edge;
	private readonly Vector2[] rim = new Vector2[41];
	private readonly Vector2[] flame = new Vector2[25];
	private readonly Vector2[] flameCore = new Vector2[25];

	/// <summary>0 while it opens, 1 once it is open.</summary>
	public float Openness => Mathf.Clamp(age / OpenSeconds, 0f, 1f);

	/// <summary>How far through the hold he is, 0 to 1.</summary>
	public float HoldFraction => Mathf.Clamp(hold / EndlessCatalog.VigilHoldSeconds, 0f, 1f);

	public override void _Ready()
	{
		ElementColors.Ramp light = ElementColors.GetRamp(Element.Light);
		core = light.Core;
		hot = light.Hot;
		mid = light.Mid;
		edge = light.Edge;
		ZIndex = DrawLayers.Ground;
		ZAsRelative = false;
		// Added to the ground rather than painted over it: light, not a decal. Painted, the pool read as a tan stain
		// on the Forest's dirt (the first photograph); added, it brightens whatever it lies on, as the wizard's own
		// light does.
		Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
	}

	/// <summary>True when a point on the ground is inside the ring.</summary>
	public bool Contains(Vector2 groundPoint)
	{
		Vector2 d = groundPoint - GlobalPosition;
		return (d.X * d.X) / (RadiusX * RadiusX) + (d.Y * d.Y) / (RadiusY * RadiusY) <= 1f;
	}

	/// <summary>
	/// One frame of the hold, with where his feet are. True on the frame the vigil is kept. Called by the run while
	/// the ring is open; it does nothing until the ring has finished opening.
	/// </summary>
	public bool Tick(Vector2 feet, float delta)
	{
		bool inside = Contains(feet);
		if (firstFrame)
		{
			// Opened under his feet: not a choice he made. It waits for him to step out once.
			startedInside = inside;
			firstFrame = false;
		}
		if (!inside)
			startedInside = false;
		armed = !startedInside;
		if (Openness < 1f || !armed)
		{
			hold = 0f;
			return false;
		}
		hold = inside ? hold + delta : Mathf.Max(0f, hold - delta * DrainRate);
		return hold >= EndlessCatalog.VigilHoldSeconds;
	}

	public override void _Process(double delta)
	{
		age += (float)delta;
		// His light, in a chapter that has none (LightPool): a no-op where the chapter is lit evenly.
		LightPool.Light(GlobalPosition, 120f * Openness, 0.85f);
		QueueRedraw();
	}

	public override void _Draw()
	{
		float open = 1f - Mathf.Pow(1f - Openness, 3f);
		if (open <= 0.01f)
			return;
		float t = age;
		float breath = 0.85f + 0.15f * Mathf.Sin(t * 2.2f);
		float rx = RadiusX * open, ry = RadiusY * open;

		// The pool of light it lays on the ground: wide and faint, then the ring's own floor.
		DrawSetTransform(Vector2.Zero, 0f, new Vector2(1f, RadiusY / RadiusX));
		DrawCircle(Vector2.Zero, rx * 1.7f, new Color(edge, 0.10f * open));
		DrawCircle(Vector2.Zero, rx * 1.15f, new Color(edge, 0.14f * open * breath));
		DrawCircle(Vector2.Zero, rx * 0.6f, new Color(mid, 0.12f * open));
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);

		// The rim, flat on the ground: a warm glow and a bright line.
		for (int i = 0; i < rim.Length; i++)
		{
			float a = Mathf.Tau * i / (rim.Length - 1);
			rim[i] = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
		}
		DrawPolyline(rim, new Color(edge, 0.75f * open * breath), 9f, true);
		DrawPolyline(rim, new Color(core, 0.95f * open), 3f, true);

		// The light at its heart: a small upright flame, his own, flickering. It is what makes the ring a place of
		// light to stand in rather than a mark on the floor (the results' Endless card is the same candle flame).
		float flicker = 1f + 0.08f * Mathf.Sin(t * 11f) + 0.05f * Mathf.Sin(t * 17.3f);
		float fh = 34f * open * flicker, fw = 10f * open;
		for (int i = 0; i < flame.Length; i++)
		{
			float u = i / (float)flame.Length;   // 0 at the tip, round by the base, back toward the tip (never twice)
			float a = u * Mathf.Tau;
			float y = -fh * (0.5f + 0.5f * Mathf.Cos(a));
			float bulge = Mathf.Sin(a);
			float width = fw * bulge * (0.35f + 0.65f * (-y / fh) * (1f + y / fh) * 2.2f);
			flame[i] = new Vector2(width + Mathf.Sin(t * 6f) * 1.2f * (-y / fh), y + 2f);
		}
		DrawColoredPolygon(flame, new Color(edge, 0.85f * open));
		for (int i = 0; i < flame.Length; i++)
			flameCore[i] = new Vector2(flame[i].X * 0.5f, 2f + (flame[i].Y - 2f) * 0.62f);
		DrawColoredPolygon(flameCore, new Color(hot, 0.95f * open));
		DrawCircle(new Vector2(0f, -fh * 0.18f), 2.2f * open, new Color(core, open));

		// The hold: an arc of white light filling round the rim from the top, clockwise, as he stands in it.
		float fill = HoldFraction;
		if (fill > 0.001f)
		{
			// Drawn as a circle's arc squashed onto the ground, so nothing is allocated while he holds.
			int points = Mathf.Max(3, Mathf.CeilToInt(rim.Length * fill));
			float start = -Mathf.Pi / 2f;
			DrawSetTransform(Vector2.Zero, 0f, new Vector2(1f, (ry + 3f) / (rx + 5f)));
			DrawArc(Vector2.Zero, rx + 5f, start, start + Mathf.Tau * fill, points, new Color(hot, 0.7f), 9f, true);
			DrawArc(Vector2.Zero, rx + 5f, start, start + Mathf.Tau * fill, points, new Color(core, 1f), 3.5f, true);
			DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
		}

		// A few embers rising from the ring and fading, each on its own beat: a vigil is kept by a light that stays lit.
		for (int k = 0; k < Embers; k++)
		{
			float beat = (t * 0.42f + k * 0.173f) % 1f;
			float a = Mathf.Tau * ((k * 0.381f) % 1f);
			Vector2 from = new(Mathf.Cos(a) * rx * 0.9f, Mathf.Sin(a) * ry * 0.9f);
			Vector2 p = from + new Vector2(0f, -38f * beat);
			DrawCircle(p, 1.4f + 1.4f * (1f - beat), new Color(k % 2 == 0 ? hot : mid, (1f - beat) * 0.85f * open));
		}
	}
}
