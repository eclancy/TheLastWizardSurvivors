using Godot;
using System;

// What standing still looks like (Player.TickStandStill): one node at the player's feet carrying
// both stand-still tells, so the screen has one place to look and the player one extra node.
//
//  - THE BRAZIER'S HEAT. Twelve embers on an ellipse round the boots that catch one at a time as
//    the heat builds - from the front, round the left, across the back and down the right - so the
//    ring is a gauge: half the embers lit is half the heat, half the bonus. (It filled from the
//    front up both sides at first, and at half heat the whole visible front was lit while the
//    unlit half hid behind his legs, so half read as nearly full.) Each one rises and
//    brightens as it catches, from the Fire mid toward its white-hot core, and once the heat is
//    full a soft glow spreads under him and sparks lift off the ring. Walking puts the embers out
//    one by one, which is the cooling the set's card describes, at the speed it describes.
//  - TAPROOT'S STANCE. Roots growing out of the ground at his feet, fanned across the FRONT where
//    the player can see them: each creeps outward as the still clock fills, and at the hold they
//    are full length and breathing, with small rootlets off their knees. (It was an earthen ring
//    whose two arcs met at the BACK - the moment it completed was hidden behind the sprite; Eric,
//    2026-11-07.) It vanishes the moment he moves, because the stance is all or nothing where the
//    heat cools on a slope - the two read differently because they behave differently.
//
// The Fire and Earth rows of the emissive ramp (.ai/art-direction.md section 3): elements are
// light, not pigment, and their cores are white-hot. Every mark is laid over a darker stroke of the
// same row's edge tone, the way BowDrawVisual backs its limbs, so it holds on a pale floor (the
// Frozen Waste) as well as a dark one.
//
// A child of the player at ZIndex -1: under his body and the swarm, over the ground and the walkable
// decor (-20), like HasteTrail. Drawn procedurally because the gauge has to move continuously with
// the heat, which a handful of frames cannot. Nothing here allocates: the ellipse is a cached table
// of unit vectors, colours are structs, and the node hides itself and stops redrawing whenever there
// is nothing to show - which is always, for a run holding neither the set nor the boon, since the
// player only makes it when one of them is first owned (Player.EnsureStanceGlow).
public partial class StanceGlow : Node2D
{
	public Player PlayerRef { get; set; }

	// The ellipse the boots stand in. The Feet capsule is 56 wide and 16 deep (player.tscn), so
	// the embers sit just outside it and the stance ring a step further out.
	private const float EmberRadiusX = 31f;
	private const float EmberRadiusY = 11f;
	private const float StanceRadiusX = 39f;
	private const float StanceRadiusY = 15f;
	// From the capsule's centre down toward the soles, so the ring reads as on the ground he
	// stands on rather than round his shins.
	private const float GroundOffsetY = 3f;

	private const int Embers = 12;
	private const int RingSegments = 40;
	private const int Sparks = 3;

	// Fire ramp: core, hot, mid, edge.
	private static readonly Color FireCore = new Color("#FFF4D0");
	private static readonly Color FireHot = new Color("#FFC848");
	private static readonly Color FireMid = new Color("#E87A1A");
	private static readonly Color FireEdge = new Color("#B8381A");
	// Earth ramp: hot, mid, edge (its core is too pale to sit on the snow).
	private static readonly Color EarthHot = new Color("#E8C48C");
	private static readonly Color EarthMid = new Color("#C99A5E");
	private static readonly Color EarthEdge = new Color("#7A5024");

	// Unit directions round the ellipse, made once for every instance.
	private static readonly Vector2[] RingUnit = BuildRing(RingSegments);
	// The order the embers catch in: one sweep from the front (angle pi/2, straight down the
	// screen) through the left, the back and the right. Each entry is the ember's angle; index is
	// its place in the order.
	private static readonly float[] EmberAngle = BuildEmberOrder();

	private Node2D feet;
	private float clock;
	private float heat;
	private float stance;
	private bool rooted;
	private bool showing;

	public override void _Ready()
	{
		ZIndex = -1;
		feet = PlayerRef?.GetNodeOrNull<Node2D>("Feet/CollisionShape2D");
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;

		heat = PlayerRef.BrazierHeat;
		stance = PlayerRef.StanceProgress;
		rooted = PlayerRef.IsStanceRooted;
		bool want = !PlayerRef.IsDead && (heat > 0.001f || stance > 0f);
		if (!want)
		{
			if (showing)
			{
				showing = false;
				Visible = false;
			}
			return;
		}

		showing = true;
		Visible = true;
		clock += (float)delta;
		// The feet move when FitFeetToSprite fits the capsule to another cell size; follow them.
		Position = (feet != null ? feet.Position : new Vector2(0f, 36f)) + new Vector2(0f, GroundOffsetY);
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!showing)
			return;
		if (heat > 0.001f)
			DrawHeatGlow();
		if (stance > 0f)
			DrawStance();
		if (heat > 0.001f)
			DrawEmbers();
	}

	// The glow and the sparks: only near full, fading in over the last 15% so "full" arrives as a
	// moment rather than a switch.
	private void DrawHeatGlow()
	{
		float full = Mathf.Clamp((heat - 0.85f) / 0.15f, 0f, 1f);
		if (full <= 0f)
			return;

		float breath = 0.5f + 0.5f * MathF.Sin(clock * 3.6f);
		// Two filled ellipses, wide and faint then tight and warmer, for a soft edge without a shader.
		FillEllipse(EmberRadiusX + 8f, EmberRadiusY + 5f, new Color(FireEdge, (0.14f + 0.05f * breath) * full));
		FillEllipse(EmberRadiusX - 3f, EmberRadiusY - 1f, new Color(FireMid, (0.22f + 0.08f * breath) * full));

		for (int j = 0; j < Sparks; j++)
		{
			// Each spark leaves the ring at its own place, rises about a body's height and fades.
			float phase = Mathf.PosMod(clock * 0.75f + j / (float)Sparks, 1f);
			float angle = j * 2.1f + MathF.Floor(clock * 0.75f + j / (float)Sparks) * 1.3f;
			Vector2 start = new Vector2(MathF.Cos(angle) * EmberRadiusX, MathF.Sin(angle) * EmberRadiusY);
			Vector2 at = start + new Vector2(MathF.Sin(phase * 6f + j) * 3f, -phase * 34f);
			float a = (1f - phase) * full;
			DrawCircle(at, 2.2f, new Color(FireEdge, 0.55f * a));
			DrawCircle(at, 1.3f, new Color(phase < 0.4f ? FireCore : FireHot, a));
		}
	}

	private void DrawEmbers()
	{
		float lit = heat * Embers;
		float ringAlpha = Mathf.Clamp(heat * 6f, 0f, 1f);

		// The rim the coals sit in: a faint line in the edge tone, so the unlit part of the gauge
		// reads as "still to fill" rather than as nothing.
		for (int s = 0; s < RingSegments; s++)
		{
			Vector2 a = new Vector2(RingUnit[s].X * EmberRadiusX, RingUnit[s].Y * EmberRadiusY);
			Vector2 b = new Vector2(RingUnit[(s + 1) % RingSegments].X * EmberRadiusX, RingUnit[(s + 1) % RingSegments].Y * EmberRadiusY);
			DrawLine(a, b, new Color(FireEdge, 0.35f * ringAlpha), 1.5f);
		}

		Color body = FireMid.Lerp(FireHot, heat);
		for (int k = 0; k < Embers; k++)
		{
			float angle = EmberAngle[k];
			Vector2 seat = new Vector2(MathF.Cos(angle) * EmberRadiusX, MathF.Sin(angle) * EmberRadiusY);
			float b = Mathf.Clamp(lit - k, 0f, 1f);
			if (b <= 0f)
			{
				// An unlit coal: how much of the gauge is still to fill.
				DrawCircle(seat, 2.2f, new Color(FireEdge, 0.6f * ringAlpha));
				continue;
			}

			float flicker = MathF.Sin(clock * 9f + k * 1.7f);
			Vector2 at = seat + new Vector2(0f, -b * (1.5f + 4.5f * heat) - flicker * 0.7f * b);
			float size = 2.2f + 1.4f * b + 0.35f * flicker * heat;
			DrawCircle(at, size + 1.6f, new Color(FireEdge, 0.7f * b));
			DrawCircle(at, size, new Color(body, b));
			if (heat > 0.6f)
				DrawCircle(at + new Vector2(0f, -0.5f), size * 0.45f, new Color(FireCore, b * Mathf.Clamp((heat - 0.6f) / 0.4f, 0f, 1f)));
		}
	}

	// The roots' fan: angles off the front of the ellipse (RingUnit[0], straight down the screen),
	// a full length each, and which side its crook bends - all constants, so the same roots grow
	// every time and nothing allocates. The spread stays inside the lower half of the ellipse, in
	// front of the boots, where the sprite cannot hide it.
	private const int Roots = 6;
	private static readonly float[] RootOffset = { -1.25f, -0.75f, -0.25f, 0.25f, 0.75f, 1.25f };
	private static readonly float[] RootLength = { 13f, 17f, 12f, 16f, 11f, 15f };
	private static readonly float[] RootSide = { 1f, -1f, 1f, -1f, 1f, -1f };

	// Building: each root creeps out of the ground at the boots, front first, its length the gauge
	// of the still clock. Rooted: full length, breathing, with a rootlet off each knee.
	private void DrawStance()
	{
		float alpha = rooted ? 0.75f + 0.15f * MathF.Sin(clock * 2.2f) : 0.35f + 0.4f * stance;
		for (int r = 0; r < Roots; r++)
		{
			// A slight stagger, middle roots first, so the fan reads as growth rather than a switch.
			float grow = rooted ? 1f : Mathf.Clamp(stance * 1.3f - MathF.Abs(RootOffset[r]) * 0.16f, 0f, 1f);
			if (grow <= 0f)
				continue;

			float angle = Mathf.Pi / 2f + RootOffset[r];
			// Where it breaks the ground (just outside the boots) and the way it grows: radially out
			// of the feet ellipse, with a crook at the knee so it reads as a root, not a spoke.
			Vector2 unit = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
			Vector2 from = new Vector2(unit.X * StanceRadiusX * 0.55f, unit.Y * StanceRadiusY * 0.55f);
			Vector2 dir = new Vector2(unit.X * StanceRadiusX, unit.Y * StanceRadiusY).Normalized();
			Vector2 side = new Vector2(-dir.Y, dir.X) * RootSide[r];
			float len = RootLength[r] * grow;
			// Rooted, the tips stir a little with the breath, so "held" never reads as a still image.
			float stir = rooted ? MathF.Sin(clock * 2.2f + r * 1.9f) * 0.8f : 0f;
			Vector2 knee = from + dir * (len * 0.45f) + side * 1.6f;
			Vector2 tip = knee + dir * (len * 0.55f) + side * (stir - 2.2f);

			// The mound it breaks out of, then the root: a darker stroke under a lit one, like the rest.
			DrawCircle(from, 1.8f, new Color(EarthEdge, 0.8f * alpha));
			DrawLine(from, knee, new Color(EarthEdge, 0.8f * alpha), 3.4f);
			DrawLine(knee, tip, new Color(EarthEdge, 0.7f * alpha), 2.6f);
			DrawLine(from, knee, new Color(rooted ? EarthHot : EarthMid, alpha), 1.7f);
			DrawLine(knee, tip, new Color(EarthMid, 0.85f * alpha), 1.2f);

			if (!rooted)
				continue;
			// The rootlet off the knee, the hold's own mark: it only exists once the stance has taken.
			Vector2 sprout = knee + (dir + side * 0.9f).Normalized() * 4f;
			DrawLine(knee, sprout, new Color(EarthEdge, 0.7f * alpha), 2f);
			DrawLine(knee, sprout, new Color(EarthMid, 0.8f * alpha), 1f);
		}
	}

	// A filled ellipse without allocating a polygon: a circle under a squashing transform. Only
	// fills go this way - a stroke under it would thin to nothing at the top and bottom.
	private void FillEllipse(float radiusX, float radiusY, Color colour)
	{
		DrawSetTransform(Vector2.Zero, 0f, new Vector2(1f, radiusY / radiusX));
		DrawCircle(Vector2.Zero, radiusX, colour);
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
	}

	// Index 0 is the front of the ellipse (angle pi/2, straight down the screen), then round.
	private static Vector2[] BuildRing(int segments)
	{
		var ring = new Vector2[segments];
		for (int i = 0; i < segments; i++)
		{
			float angle = Mathf.Pi / 2f + i * Mathf.Tau / segments;
			ring[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
		}
		return ring;
	}

	// Angles grow clockwise on a y-down screen: from straight down, to the left, up, to the right.
	private static float[] BuildEmberOrder()
	{
		var order = new float[Embers];
		float step = Mathf.Tau / Embers;
		for (int k = 0; k < Embers; k++)
			order[k] = Mathf.Pi / 2f + k * step;
		return order;
	}
}
