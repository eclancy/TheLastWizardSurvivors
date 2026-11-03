using Godot;
using System;

namespace TheOneHeMissed.scripts;

// A FLASH, the pixel-art way (.ai/cutscenes.md, "The cinematic rules", rule 1: "a flash or a hit-stop only where
// an impact calls for it"). Not a translucent wash over the picture - the art contract allows none
// (.ai/art-direction.md) - but opaque light pixels through the 4x4 Bayer matrix on the 2 px grid: every pixel on
// screen is either the picture's or a stop of the light's ramp, never a blend.
//
// RADIAL, FROM THE LIGHT. A flash of the whole screen at even density read as a screen door drawn over the art (the
// first cut, photographed over the prologue's break). This one bursts out of the light that makes it: dense at the
// source and thinning ring by ring to nothing at its edge, the rings widening as it goes and every level dropping
// with the square of what is left, so it is a bloom on the first frames and a scatter of sparks by the last. The
// inner rings are drawn in the hot stop while it is young, the rest in the edge stop, as the boss flare's spill steps.
// Drawn in strips one Bayer tile high with every edge on the tile grid, so the pattern runs unbroken from ring to ring.
//
// Code only, like SpeedLines: the owner ticks it and draws it over whatever it covers (the prologue's fx layer; the
// run's letterbox layer, for the dark wizard's fall). The tiles are the owner's, not a static's: a Godot resource held
// by a C# static outlives the engine's cleanup and is reported at exit (Prologue's note).
public sealed class DitherFlash
{
	private static readonly int[] Bayer4 = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

	/// <summary>One Bayer tile, canvas units: the strip height and the grid every edge snaps to.</summary>
	private const float Tile = 8f;
	private const int Rings = 6;

	private Texture2D[] tiles;
	private float age = -1f;
	private float seconds = 0.2f;
	private float peak;
	private Color hot;
	private Color edge;
	private Vector2 centre;
	private float radius;

	/// <summary>True while the flash is still on screen.</summary>
	public bool Active => age >= 0f && age < seconds;

	/// <summary>
	/// Flashes out of <paramref name="source"/> (in the canvas's own space) to <paramref name="reach"/>: up to
	/// <paramref name="peakDensity"/> (0-1) of the pixels lit at the source on the first frame, fewer outward, none
	/// after <paramref name="flashSeconds"/>.
	/// </summary>
	public void Flash(Color hotStop, Color edgeStop, float peakDensity, float flashSeconds, Vector2 source, float reach)
	{
		hot = new Color(hotStop, 1f);
		edge = new Color(edgeStop, 1f);
		peak = Mathf.Clamp(peakDensity, 0f, 1f);
		seconds = Mathf.Max(0.04f, flashSeconds);
		centre = source;
		radius = Mathf.Max(Tile * 2f, reach);
		age = 0f;
	}

	public void Tick(float delta)
	{
		if (age < 0f)
			return;
		age += delta;
		if (age >= seconds)
			age = -1f;
	}

	/// <summary>Draws the flash onto <paramref name="canvas"/>, inside <paramref name="clip"/> (its visible rect).</summary>
	public void Draw(CanvasItem canvas, Rect2 clip)
	{
		if (!Active || canvas == null)
			return;
		float u = age / seconds;
		float fade = (1f - u) * (1f - u);
		// The bloom widens as it goes: 60% of its reach on the first frame, all of it at the end.
		float reach = radius * (0.6f + 0.4f * (1f - (1f - u) * (1f - u)));
		tiles ??= BuildBayerTiles();

		float top = Mathf.Max(clip.Position.Y, centre.Y - reach);
		float bottom = Mathf.Min(clip.End.Y, centre.Y + reach);
		top = Mathf.Floor(top / Tile) * Tile;
		Span<float> half = stackalloc float[Rings];
		for (float y = top; y < bottom; y += Tile)
		{
			float dy = Mathf.Abs(y + Tile * 0.5f - centre.Y);
			for (int k = 0; k < Rings; k++)
			{
				float r = reach * (k + 1) / Rings;
				half[k] = r > dy ? Mathf.Sqrt(r * r - dy * dy) : -1f;
			}
			for (int k = 0; k < Rings; k++)
			{
				if (half[k] < 0f)
					continue;
				// Thinning with the square of the way out, so the outer rings are a scatter and the disc has no rim.
				float out01 = 1f - (float)k / Rings;
				int level = Mathf.Clamp(Mathf.RoundToInt(peak * fade * 16f * out01 * out01), 0, 16);
				if (level == 0)
					continue;
				Color stop = k < Rings / 2 && u < 0.4f ? hot : edge;
				float outer = half[k];
				float inner = k > 0 ? half[k - 1] : -1f;
				if (inner < 0f)
					Run(canvas, clip, y, centre.X - outer, centre.X + outer, level, stop);
				else
				{
					Run(canvas, clip, y, centre.X - outer, centre.X - inner, level, stop);
					Run(canvas, clip, y, centre.X + inner, centre.X + outer, level, stop);
				}
			}
		}
	}

	// One run of a strip, its ends on the tile grid and inside the clip.
	private void Run(CanvasItem canvas, Rect2 clip, float y, float from, float to, int level, Color stop)
	{
		float a = Mathf.Max(Mathf.Round(from / Tile) * Tile, Mathf.Floor(clip.Position.X / Tile) * Tile);
		float b = Mathf.Min(Mathf.Round(to / Tile) * Tile, clip.End.X);
		if (b - a < 1f)
			return;
		canvas.DrawTextureRect(tiles[level], new Rect2(a, y, b - a, Tile), true, stop);
	}

	/// <summary>
	/// Seventeen levels of the 4x4 Bayer matrix on a 2 px grid, as 8x8 white tiles (transparent where unlit), to
	/// be drawn tiled and modulated to an opaque ramp stop. The boss arrival's flare spill uses the same.
	/// </summary>
	public static Texture2D[] BuildBayerTiles()
	{
		var built = new Texture2D[17];
		for (int level = 0; level <= 16; level++)
		{
			Image image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8);
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					bool lit = Bayer4[(y / 2) % 4 * 4 + (x / 2) % 4] < level;
					image.SetPixel(x, y, lit ? Colors.White : new Color(1f, 1f, 1f, 0f));
				}
			}
			built[level] = ImageTexture.CreateFromImage(image);
		}
		return built;
	}
}
