using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

/// <summary>
/// The crown on every cast of the spell the Sovereign Rite anointed (.ai/sovereign-spell.md section 5): a thin
/// corona in the spell's own element ramp - its white-hot core on the line, its hot stop either side, thinned
/// by an ordered Bayer dither, never a translucent wash - with gilt glints at the eight points and a slow trail
/// of motes. The warm light of .ai/world-and-tone.md gathered onto one spell, so it reads as changed at a
/// glance in a crowd.
/// </summary>
/// <remarks>
/// Code-drawn, no art. Attached by Player.ApplyLegendaryVisual (through CrownIfSovereign) to whatever node a
/// cast of the anointed spell makes - a bolt, a strike, a zone - as a child, so it goes when its host does.
/// Top-level, so the host's tint does not reach the ring (a Modulate multiplies children) and its rotation
/// does not turn it; it follows the host's position every frame instead.
///
/// The ring itself is one texture per element, radius and dither phase, built once and kept for the run
/// (<see cref="RingCache"/>): a cast draws one texture a frame, two phases alternating, never a pixel loop.
/// Texels are art pixels, two canvas units each, as the sprites are drawn (.ai/art-direction.md).
/// </remarks>
public partial class SovereignCorona : Node2D
{
	private const float Texel = 2f;
	// A bolt's hit circle is a few units (Fireball's is 6) inside a picture four times that, and many casts
	// draw themselves with no sprite to measure, so the ring never closes tighter than a bolt's picture.
	private const float MinRadius = 20f;
	private const float MaxRadius = 120f;
	private const float DefaultRadius = 20f;
	// How often the dither turns over: a shimmer, not a flicker.
	private const float PhaseSeconds = 0.18f;
	private const int MoteCount = 6;
	private const float MoteEvery = 0.16f;
	private const float MoteLife = 0.9f;
	private const float MoteRise = 14f;
	private const float FlashSeconds = 0.45f;
	private const float FlashReach = 46f;

	// White-gold: the gilt every crown in the game is drawn in (the conjunction plate's gold lit, #D8B04A, and
	// the fire ramp's hot stop for the glint's heart).
	private static readonly Color Gilt = new(1.0f, 0.784f, 0.282f);
	private static readonly Color GiltCore = new(1.0f, 0.957f, 0.816f);

	// 4x4 ordered (Bayer) thresholds, 0..15.
	private static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

	private static readonly Dictionary<(Element, int, int), ImageTexture> RingCache = new();

	private Node2D host;
	private Element element;
	private int radiusTexels;
	private float clock;
	private float flashLeft;
	private float moteClock;
	private int nextMote;
	private readonly Vector2[] motes = new Vector2[MoteCount];
	private readonly float[] moteAge = new float[MoteCount];
	private ElementColors.Ramp ramp;
	private uint rng;

	/// <summary>Forgets every ring built this run (Player._ExitTree).</summary>
	public static void ClearCache() => RingCache.Clear();

	/// <summary>Crowns <paramref name="host"/>. A second call on the same host does nothing.</summary>
	public static void Attach(Node2D host, Element element, bool flash)
	{
		if (host == null || !IsInstanceValid(host) || host.GetNodeOrNull("SovereignCorona") != null)
			return;
		var corona = new SovereignCorona
		{
			Name = "SovereignCorona",
			host = host,
			element = element,
			flashLeft = flash ? FlashSeconds : 0f,
			TopLevel = true,
			ZAsRelative = false,
			// Over the cast it crowns, whatever layer that is drawn on (a bolt in the air, a zone on the floor).
			ZIndex = DrawLayers.ForemostAir,
			TextureFilter = TextureFilterEnum.Nearest,
		};
		host.AddChild(corona);
	}

	public override void _Ready()
	{
		ramp = ElementColors.GetRamp(element);
		float radius = Mathf.Clamp(EstimateRadius(host), MinRadius, MaxRadius);
		radiusTexels = Mathf.Max(5, Mathf.RoundToInt(radius / Texel));
		for (int i = 0; i < MoteCount; i++)
			moteAge[i] = MoteLife;
		rng = (uint)GetInstanceId() | 1u;
		Follow();
	}

	public override void _Process(double delta)
	{
		if (host == null || !IsInstanceValid(host))
		{
			QueueFree();
			return;
		}
		float dt = (float)delta;
		clock += dt;
		flashLeft = MathF.Max(0f, flashLeft - dt);
		Follow();

		for (int i = 0; i < MoteCount; i++)
		{
			if (moteAge[i] >= MoteLife)
				continue;
			moteAge[i] += dt;
			motes[i].Y -= MoteRise * dt;
		}
		moteClock += dt;
		if (moteClock >= MoteEvery)
		{
			moteClock = 0f;
			// Born on the ring, at a point that walks round it, and left behind where the cast was.
			float angle = Next01() * Mathf.Tau;
			motes[nextMote] = GlobalPosition + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radiusTexels * Texel;
			moteAge[nextMote] = 0f;
			nextMote = (nextMote + 1) % MoteCount;
		}
		QueueRedraw();
	}

	private void Follow()
	{
		Vector2 at = host.GlobalPosition;
		GlobalPosition = new Vector2(MathF.Round(at.X), MathF.Round(at.Y));
	}

	public override void _Draw()
	{
		int phase = (int)(clock / PhaseSeconds) & 1;
		DrawRing(radiusTexels, phase);

		// The flash: the anointed spell's first cast after the Rite, a ring thrown outward and a hot heart.
		if (flashLeft > 0f)
		{
			float t = 1f - flashLeft / FlashSeconds;
			int outward = radiusTexels + Mathf.RoundToInt(t * FlashReach / Texel);
			DrawRing(outward, phase ^ 1);
			if (t < 0.5f)
				DrawRing(Mathf.Max(3, radiusTexels / 2), phase);
		}

		// The motes: one art pixel each, the core, then the hot stop, then gone (a step, never a fade).
		for (int i = 0; i < MoteCount; i++)
		{
			if (moteAge[i] >= MoteLife)
				continue;
			float age = moteAge[i] / MoteLife;
			Color ink = age < 0.35f ? ramp.Core : age < 0.7f ? Gilt : ramp.Hot;
			Vector2 local = motes[i] - GlobalPosition;
			local = new Vector2(MathF.Round(local.X / Texel) * Texel, MathF.Round(local.Y / Texel) * Texel);
			DrawRect(new Rect2(local - new Vector2(Texel / 2f, Texel / 2f), new Vector2(Texel, Texel)), ink);
		}
	}

	private void DrawRing(int radius, int phase)
	{
		ImageTexture ring = RingFor(element, ramp, radius, phase);
		if (ring == null)
			return;
		Vector2 size = ring.GetSize() * Texel;
		DrawTextureRect(ring, new Rect2(-size / 2f, size), false);
	}

	// The ring at one radius, in texels: the core on the line, the hot stop half-dithered on each side, a
	// quarter-dithered outer edge, and a gilt glint standing out at each of the eight points.
	public static ImageTexture RingFor(Element element, ElementColors.Ramp ramp, int radius, int phase)
	{
		var key = (element, radius, phase);
		if (RingCache.TryGetValue(key, out ImageTexture cached))
			return cached;
		int side = radius * 2 + 7;
		Image image = Image.CreateEmpty(side, side, false, Image.Format.Rgba8);
		float centre = (side - 1) / 2f;
		int shift = phase * 2;
		for (int y = 0; y < side; y++)
		{
			for (int x = 0; x < side; x++)
			{
				float d = new Vector2(x - centre, y - centre).Length();
				float off = d - radius;
				int threshold = Bayer[((y + shift) & 3) * 4 + ((x + shift) & 3)];
				Color? ink = null;
				if (MathF.Abs(off) < 0.5f)
					ink = ramp.Core;
				else if (MathF.Abs(off) < 1.5f && threshold < 8)
					ink = ramp.Hot;
				else if (off >= 1.5f && off < 2.5f && threshold < 4)
					ink = ramp.Hot;
				if (ink != null)
					image.SetPixel(x, y, ink.Value);
			}
		}
		// The eight glints: two texels of gilt standing outward from the line, the inner one white-gold.
		for (int i = 0; i < 8; i++)
		{
			float a = i * Mathf.Tau / 8f + (phase == 1 ? Mathf.Tau / 16f : 0f);
			for (int k = 1; k <= 2; k++)
			{
				int gx = Mathf.RoundToInt(centre + MathF.Cos(a) * (radius + k));
				int gy = Mathf.RoundToInt(centre + MathF.Sin(a) * (radius + k));
				if (gx >= 0 && gy >= 0 && gx < side && gy < side)
					image.SetPixel(gx, gy, k == 1 ? GiltCore : Gilt);
			}
		}
		ImageTexture texture = ImageTexture.CreateFromImage(image);
		RingCache[key] = texture;
		return texture;
	}

	// The host's reach in canvas units: the larger of its collision shape and its picture (a sprite's frame,
	// three quarters of it - pixel art carries a margin), whichever it has at hand; else a bolt's size.
	private static float EstimateRadius(Node2D host)
	{
		if (host == null)
			return DefaultRadius;
		float scale = (MathF.Abs(host.GlobalScale.X) + MathF.Abs(host.GlobalScale.Y)) * 0.5f;
		float reach = 0f;
		foreach (Node child in host.GetChildren())
		{
			if (FindShape(child) is CollisionShape2D shape && shape.Shape != null)
			{
				float s = (MathF.Abs(shape.Scale.X) + MathF.Abs(shape.Scale.Y)) * 0.5f;
				reach = MathF.Max(reach, shape.Shape switch
				{
					CircleShape2D circle => circle.Radius * s,
					CapsuleShape2D capsule => MathF.Max(capsule.Radius, capsule.Height * 0.5f) * s,
					RectangleShape2D rect => MathF.Max(rect.Size.X, rect.Size.Y) * 0.5f * s,
					_ => 0f,
				});
			}
			Vector2 picture = child switch
			{
				Sprite2D sprite when sprite.Texture != null => sprite.Texture.GetSize() / new Vector2(Math.Max(1, sprite.Hframes), Math.Max(1, sprite.Vframes)) * sprite.Scale.Abs(),
				AnimatedSprite2D animated when animated.SpriteFrames != null && animated.SpriteFrames.GetFrameCount(animated.Animation) > 0
					=> animated.SpriteFrames.GetFrameTexture(animated.Animation, 0).GetSize() * animated.Scale.Abs(),
				_ => Vector2.Zero,
			};
			reach = MathF.Max(reach, MathF.Max(picture.X, picture.Y) * 0.5f * 0.75f);
		}
		return (reach > 0f ? reach : DefaultRadius) * scale;
	}

	private static CollisionShape2D FindShape(Node node)
	{
		if (node is CollisionShape2D shape)
			return shape;
		// One level down: an Area2D hitbox under the cast's root.
		if (node is Area2D)
		{
			foreach (Node inner in node.GetChildren())
			{
				if (inner is CollisionShape2D innerShape)
					return innerShape;
			}
		}
		return null;
	}

	private float Next01()
	{
		rng ^= rng << 13;
		rng ^= rng >> 17;
		rng ^= rng << 5;
		return (rng & 0xFFFFFF) / (float)0x1000000;
	}
}
