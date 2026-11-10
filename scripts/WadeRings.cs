using Godot;
using System;
using TheOneHeMissed.scripts;

// What the water does round the wizard's feet (in-flight 74, Eric 2026-11-09: "When walking over water, we should make
// the feet of the wizard sink in"). Player.Wading.cs sinks the body; this is the water's half: a collar of the water's
// glint where he stands in it, and rings rolling out from it - every few tenths of a second standing, twice as often
// walking, each left where it was born so a walk across a pond trails a wake. Every water style (WaterSurface's forest,
// swamp, frozen and pool), which drew none; and the Swallow-Hole's mud. Baba Yaga and Nimue, who ride over the water
// (CharacterData.RidesOverWater), raise rings too; Wayland, flying, does not touch it.
//
// THE WATER'S OWN INKS. The two colours are read once from the chapter's water art (tools/art/water.py's tones strip:
// slot 4, the moving crests, and slot 5, the glint), so a ring is drawn in the marks the water already makes. A young
// ring is the glint and a spreading one the crest's tone, thinned on an ordered pattern as it goes rather than faded -
// opaque 2 px cells on the world grid, as every ground mark is (.ai/art-direction.md).
//
// A child of the player at z -1, beside the StanceGlow: under his body and the swarm, over the water and the ground
// spells. Nothing allocates per frame - a fixed ring buffer in world space, read through one inverse transform - and the
// node stops redrawing once he is out and the last ring has run out.
public partial class WadeRings : Node2D
{
	public Player PlayerRef { get; set; }

	/// <summary>Set by the player every physics frame: his feet are in it and nothing holds him off it.</summary>
	public bool Wading { get; set; }

	/// <summary>The water's crest tone and its glint, for the rings and for the line on his body (the shader's ink).</summary>
	public Color Ripple { get; private set; }
	public Color Glint { get; private set; }

	private const int MaxRings = 6;
	private const float RingSeconds = 0.8f;
	private const float StandingEverySeconds = 0.45f;
	private const float WalkingEverySeconds = 0.22f;
	// The rings in world px: born round the boots, spread to a little more than his width; flattened to the ground.
	private const float StartRadius = 12f;
	private const float EndRadius = 34f;
	private const float Squash = 0.38f;
	// The collar: the water standing round his legs, the width of the boots and a step more.
	private const float CollarRadius = 16f;
	// While the young ring is the glint (a share of its life).
	private const float GlintShare = 0.3f;
	private const float Cell = 2f;
	private const float WalkingSpeedSquared = 20f * 20f;

	private readonly Vector2[] ringAt = new Vector2[MaxRings];
	private readonly float[] ringBorn = new float[MaxRings];
	private int nextRing;
	private float clock;
	private float spawnClock;
	private bool drewLastFrame;
	private Vector2 collarAt;

	public override void _Ready()
	{
		ZIndex = -1;
		Array.Fill(ringBorn, -100f);
		string style = StageEnvironmentCatalog.GetForStageIndex(Global.SelectedStageIdx).WaterStyle;
		ReadInks(style);
	}

	public override void _Process(double delta)
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;
		float dt = (float)delta;
		clock += dt;
		bool wading = Wading && !PlayerRef.IsDead;
		if (wading)
		{
			collarAt = PlayerRef.FootPosition;
			spawnClock -= dt;
			if (spawnClock <= 0f)
			{
				ringAt[nextRing] = collarAt;
				ringBorn[nextRing] = clock;
				nextRing = (nextRing + 1) % MaxRings;
				spawnClock = PlayerRef.Velocity.LengthSquared() > WalkingSpeedSquared ? WalkingEverySeconds : StandingEverySeconds;
			}
		}
		else
			// The first ring the moment he steps in.
			spawnClock = 0f;

		bool any = wading;
		for (int i = 0; i < MaxRings && !any; i++)
			any = clock - ringBorn[i] < RingSeconds;
		if (any || drewLastFrame)
			QueueRedraw();
		drewLastFrame = any;
	}

	public override void _Draw()
	{
		if (!drewLastFrame)
			return;
		Transform2D toLocal = GlobalTransform.AffineInverse();
		for (int i = 0; i < MaxRings; i++)
		{
			float age = clock - ringBorn[i];
			if (age < 0f || age >= RingSeconds)
				continue;
			float t = age / RingSeconds;
			float spread = 1f - (1f - t) * (1f - t);
			float rx = Mathf.Lerp(StartRadius, EndRadius, spread);
			DrawEllipseCells(toLocal, ringAt[i], rx, t < GlintShare ? Glint : Ripple, t);
		}
		if (Wading && !PlayerRef.IsDead)
			DrawEllipseCells(toLocal, collarAt, CollarRadius, Glint, 0f);
	}

	// An ellipse of 2 px cells on the world grid, <paramref name="thin"/> of them left out on an ordered pattern.
	private void DrawEllipseCells(Transform2D toLocal, Vector2 centre, float rx, Color ink, float thin)
	{
		float ry = rx * Squash;
		int steps = Mathf.Clamp((int)(rx * 1.4f), 16, 64);
		var size = new Vector2(Cell, Cell);
		for (int k = 0; k < steps; k++)
		{
			if (((k * 5) % 8) / 8f < thin)
				continue;
			float angle = k * Mathf.Tau / steps;
			Vector2 world = centre + new Vector2(MathF.Cos(angle) * rx, MathF.Sin(angle) * ry);
			world = new Vector2(MathF.Round(world.X / Cell) * Cell, MathF.Round(world.Y / Cell) * Cell);
			DrawRect(new Rect2(toLocal * world, size), ink);
		}
	}

	// tools/art/water.py TONE_SLOTS: 4 the ripple, 5 the glint. Read once, from the art the chapter's water is drawn in.
	private void ReadInks(string style)
	{
		string path = $"res://assets/bonelight/tiles/water/{style}_tones.png";
		Image image = GD.Load<Texture2D>(path)?.GetImage();
		if (image == null || image.GetWidth() < 6)
		{
			GD.PushError($"WadeRings: no water tones at {path}; the rings round a wader would be drawn in nothing. Re-run: python tools/art/water.py, then import.");
			Ripple = Glint = Colors.Transparent;
			return;
		}
		if (image.IsCompressed())
			image.Decompress();
		Ripple = image.GetPixel(4, 0);
		Glint = image.GetPixel(5, 0);
	}
}
