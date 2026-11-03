using Godot;
using System;

namespace TheOneHeMissed.scripts;

// SPEED LINES (Eric, 2026-11-03: "Make sure we use things like speed lines, screen shake, etc to really bring out
// the cinematics when appropriate in cutscenes"; .ai/cutscenes.md, "The cinematic rules", rule 1). The manga
// impact burst: a ring of straight streaks round a subject on the frame something lands on it - thrown OUT from a
// boss as it is revealed or from the Archmage's light as it breaks the chain - or rushing IN on it, for the crash
// zoom onto a fallen wizard.
//
// PIXEL-ART HONEST. Every streak is stepped on a grid - the world's 2 px, the prologue panel's x2 (one art pixel,
// two canvas units) - and drawn as one rect per row (or column) of that grid it crosses, never as an anti-aliased
// line. Each is two opaque stops of a ramp, the hotter nearer the subject. They go by getting shorter and fewer,
// never by fading (.ai/art-direction.md: opaque stops and dither only).
//
// ALLOCATION-FREE PER FRAME. A fixed pool of streaks, laid out once per burst from a seeded hash, so a burst
// photographs the same every time; Tick only advances a clock and Draw only issues DrawRect.
//
// CODE ONLY, NOT A NODE. Whoever owns the canvas ticks it and draws it from its own Draw: the run's impact layer
// (Node2DGame.BossEntrance.cs, world space, for a boss's climax, the dark wizard's fall and the defeat's crash
// zoom) and the prologue's fx layer (Prologue.cs, canvas space, for the break).
public sealed class SpeedLines
{
	/// <summary>The most streaks one burst throws.</summary>
	public const int MaxStreaks = 48;

	/// <summary>Out from the subject (an impact, a reveal) or in on it (a crash zoom).</summary>
	public enum Motion { Outward, Inward }

	private readonly float[] angle = new float[MaxStreaks];
	// Where the streak's far end sits, as a share of the way from the inner radius to the outer.
	private readonly float[] reach = new float[MaxStreaks];
	// The share of the life at which this streak goes: they thin out rather than all vanishing at once.
	private readonly float[] goneAt = new float[MaxStreaks];
	// Grid cells thick: most one, a few two, so the burst has a weight to it.
	private readonly int[] thick = new int[MaxStreaks];

	private int count;
	private float age;
	private float life;
	private Vector2 centre;
	private float inner;
	private float outer;
	private float grid = 2f;
	private Color near;
	private Color far;
	private Motion motion;

	/// <summary>True while a burst is still drawing.</summary>
	public bool Active => count > 0 && age < life;

	/// <summary>0 to 1 through the burst; 1 once it has gone.</summary>
	public float Progress => life > 0f ? Mathf.Clamp(age / life, 0f, 1f) : 1f;

	/// <summary>
	/// Starts a burst round <paramref name="burstCentre"/> (in the canvas item's local space): streaks between
	/// <paramref name="innerRadius"/> (the clear ring round the subject) and <paramref name="outerRadius"/>, on a
	/// <paramref name="gridSize"/> grid, in <paramref name="nearColour"/> on their half nearer the subject and
	/// <paramref name="farColour"/> beyond it. Replaces any burst still drawing.
	/// </summary>
	public void Burst(Vector2 burstCentre, float innerRadius, float outerRadius, int streaks, float seconds,
		Color nearColour, Color farColour, float gridSize, uint seed, Motion burstMotion = Motion.Outward)
	{
		count = Math.Clamp(streaks, 0, MaxStreaks);
		age = 0f;
		life = Mathf.Max(0.05f, seconds);
		grid = Mathf.Max(1f, gridSize);
		centre = new Vector2(Mathf.Round(burstCentre.X / grid) * grid, Mathf.Round(burstCentre.Y / grid) * grid);
		inner = Mathf.Max(grid, innerRadius);
		outer = Mathf.Max(inner + grid * 4f, outerRadius);
		near = nearColour;
		far = farColour;
		motion = burstMotion;
		for (int i = 0; i < count; i++)
		{
			// Evenly round the ring with a jitter of most of a slot, so no two bursts read as a clock face.
			float slot = Mathf.Tau / Mathf.Max(1, count);
			angle[i] = slot * i + (Hash(seed, i * 3) - 0.5f) * slot * 0.8f;
			reach[i] = 0.62f + 0.38f * Hash(seed, i * 3 + 1);
			goneAt[i] = 0.55f + 0.45f * Hash(seed, i * 3 + 2);
			thick[i] = Hash(seed ^ 0x9E37u, i) < 0.22f ? 2 : 1;
		}
	}

	/// <summary>Advances the burst's clock.</summary>
	public void Tick(float delta)
	{
		if (count > 0 && age < life)
			age += delta;
	}

	/// <summary>Ends the burst at once.</summary>
	public void Stop() => age = life;

	/// <summary>Draws the burst onto <paramref name="canvas"/>, in its local space. Nothing once it has gone.</summary>
	public void Draw(CanvasItem canvas)
	{
		if (!Active || canvas == null)
			return;
		float u = age / life;
		for (int i = 0; i < count; i++)
		{
			if (u >= goneAt[i])
				continue;
			float far0 = inner + (outer - inner) * reach[i];
			float a, b;
			if (motion == Motion.Outward)
			{
				// The whole streak is there on the impact frame and its near end chases its far end out.
				a = inner + (far0 - inner) * EaseIn(u / goneAt[i]);
				b = far0 * (1f + 0.12f * EaseOut(u));
			}
			else
			{
				// Rushing in: a short streak at the edge that reaches in toward the subject.
				b = outer;
				a = outer - (outer - inner) * reach[i] * EaseOut(Mathf.Min(1f, u / goneAt[i] * 1.6f));
			}
			if (b - a < grid)
				continue;
			var dir = new Vector2(MathF.Cos(angle[i]), MathF.Sin(angle[i]));
			// The hotter stop on the share nearer the subject.
			float split = a + (b - a) * 0.4f;
			Segment(canvas, centre + dir * a, centre + dir * split, thick[i], near);
			Segment(canvas, centre + dir * split, centre + dir * b, thick[i], far);
		}
	}

	// A straight run from a to b on the grid, one rect per row it crosses (or per column, for a steep one).
	private void Segment(CanvasItem canvas, Vector2 a, Vector2 b, int cells, Color colour)
	{
		float g = grid;
		int x0 = Mathf.RoundToInt(a.X / g), y0 = Mathf.RoundToInt(a.Y / g);
		int x1 = Mathf.RoundToInt(b.X / g), y1 = Mathf.RoundToInt(b.Y / g);
		int dx = x1 - x0, dy = y1 - y0;
		int adx = Math.Abs(dx), ady = Math.Abs(dy);
		int lead = (cells - 1) / 2;
		if (adx >= ady)
		{
			if (ady == 0)
			{
				canvas.DrawRect(new Rect2(Math.Min(x0, x1) * g, (y0 - lead) * g, (adx + 1) * g, cells * g), colour);
				return;
			}
			int sy = Math.Sign(dy);
			for (int j = 0; j <= ady; j++)
			{
				int xa = x0 + (int)MathF.Round(dx * Mathf.Clamp((j - 0.5f) / ady, 0f, 1f));
				int xb = x0 + (int)MathF.Round(dx * Mathf.Clamp((j + 0.5f) / ady, 0f, 1f));
				int lo = Math.Min(xa, xb), hi = Math.Max(xa, xb);
				canvas.DrawRect(new Rect2(lo * g, (y0 + j * sy - lead) * g, (hi - lo + 1) * g, cells * g), colour);
			}
			return;
		}
		if (adx == 0)
		{
			canvas.DrawRect(new Rect2((x0 - lead) * g, Math.Min(y0, y1) * g, cells * g, (ady + 1) * g), colour);
			return;
		}
		int sx = Math.Sign(dx);
		for (int j = 0; j <= adx; j++)
		{
			int ya = y0 + (int)MathF.Round(dy * Mathf.Clamp((j - 0.5f) / adx, 0f, 1f));
			int yb = y0 + (int)MathF.Round(dy * Mathf.Clamp((j + 0.5f) / adx, 0f, 1f));
			int lo = Math.Min(ya, yb), hi = Math.Max(ya, yb);
			canvas.DrawRect(new Rect2((x0 + j * sx - lead) * g, lo * g, cells * g, (hi - lo + 1) * g), colour);
		}
	}

	private static float EaseIn(float u)
	{
		u = Mathf.Clamp(u, 0f, 1f);
		return u * u;
	}

	private static float EaseOut(float u)
	{
		u = Mathf.Clamp(u, 0f, 1f);
		return 1f - (1f - u) * (1f - u);
	}

	// A seeded hash to 0-1 (BossDeath.Hash's shape): the same burst lays out the same way every time.
	private static float Hash(uint seed, int i)
	{
		uint h = seed * 747796405u + (uint)i * 2891336453u;
		h ^= h >> 16;
		h *= 2246822519u;
		h ^= h >> 13;
		h *= 3266489917u;
		h ^= h >> 16;
		return (h & 0xFFFFFF) / 16777215f;
	}
}
