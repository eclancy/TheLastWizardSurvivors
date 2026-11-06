using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// Familiar's environmental evidence. Ground-level scuffs and splayed paws, never a beam or POI arrow.
// Route the tracks once through walkable cells; only visible clusters redraw, at ten ticks a second.
public partial class TeachingTrail : Node2D
{
	public SecretDirector Director { get; set; }
	public SecretSite Site { get; set; }
	public const float StartDistance = 1536f;
	private readonly List<Vector2> tracks = new();
	private readonly List<Vector2> headings = new();
	public IReadOnlyList<Vector2> TrackPositions => tracks;
	public bool Retired { get; private set; }
	/// <summary>Arrange the actual mark renderer for the isolated animation study tool.</summary>
	public void PoseForProbe(Vector2 centre)
	{
		tracks.Clear(); headings.Clear();
		for (int i = 0; i < 6; i++)
		{
			tracks.Add(centre + new Vector2((i % 3 - 1) * 420f, (i / 3 == 0 ? -1f : 1f) * 170f));
			headings.Add(new Vector2(1f, i % 2 == 0 ? 0.25f : -0.2f).Normalized());
		}
	}
	private float clock;
	private float redrawAt;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = DrawLayers.Decal;
		ulong started = Time.GetTicksUsec();
		BuildTracks();
		GD.Print($"Familiar tracks: {tracks.Count} clusters, {(Time.GetTicksUsec() - started) / 1000f:0.0} ms; no direction marker");
	}
	private void BuildTracks()
	{
		if (Site is not Pinfold pen || Director.Game.Terrain is not TerrainStreamer terrain) return;
		Vector2 origin = Director.Game.StageOrigin;
		Vector2I goal = terrain.CellOfWorld(pen.Entrance);
		Vector2 At(Vector2I c) => terrain.CellOriginWorld(c) + Vector2.One * terrain.TilePixels * 0.5f;
		bool Clear(Vector2I c) => !terrain.IsSolidCell(c.X, c.Y);
		if (!Clear(goal)) return;
		// Grow OUT from the pen, so the first print cannot start on an isolated patch across a hedge.
		// A bounded search chooses the nearest reachable outskirts, rather than crossing an obstacle.
		var frontier = new PriorityQueue<Vector2I, float>();
		var cost = new Dictionary<Vector2I, float> { [goal] = 0f };
		var parent = new Dictionary<Vector2I, Vector2I>();
		Vector2I nearSpawn = terrain.CellOfWorld(origin), first = goal;
		float nearest = At(goal).DistanceTo(origin);
		frontier.Enqueue(goal, 0f);
		int visits = 0;
		while (frontier.TryDequeue(out Vector2I cell, out _) && visits++ < 12000)
		{
			float distance = At(cell).DistanceTo(origin);
			if (distance < nearest) { first = cell; nearest = distance; }
			if (distance <= StartDistance + 48f) { first = cell; break; }
			for (int y = -1; y <= 1; y++)
				for (int x = -1; x <= 1; x++)
				{
					if (x == 0 && y == 0) continue;
					Vector2I next = cell + new Vector2I(x, y);
					if (next.X < Math.Min(nearSpawn.X, goal.X) - 24 || next.X > Math.Max(nearSpawn.X, goal.X) + 24
						|| next.Y < Math.Min(nearSpawn.Y, goal.Y) - 24 || next.Y > Math.Max(nearSpawn.Y, goal.Y) + 24
						|| !Clear(next) || (x != 0 && y != 0 && (!Clear(cell + new Vector2I(x, 0)) || !Clear(cell + new Vector2I(0, y))))) continue;
					float score = cost[cell] + (x != 0 && y != 0 ? 1.414214f : 1f);
					if (cost.TryGetValue(next, out float previous) && previous <= score) continue;
					parent[next] = cell;
					cost[next] = score;
					frontier.Enqueue(next, score + Math.Max(0f, (At(next).DistanceTo(origin) - StartDistance) / terrain.TilePixels));
				}
		}
		var route = new List<Vector2>();
		for (Vector2I c = first; ; c = parent[c]) { route.Add(At(c)); if (c == goal) break; }
		float walked = 1000f;
		float gap = 320f;
		for (int i = 0; i < route.Count; i++)
		{
			if (i > 0) walked += route[i].DistanceTo(route[i - 1]);
			if (walked < gap && i < route.Count - 1) continue;
			if (route[i].DistanceTo(origin) < StartDistance - 48f) continue;
			if (!Director.Game.IsExploreGroundClearDense(route[i], 24f)) continue;
			tracks.Add(route[i]);
			Director.Game.ReserveExploreGround(route[i], 32f);
			headings.Add((route[Math.Min(i + 1, route.Count - 1)] - route[Math.Max(0, i - 1)]).Normalized());
			walked = 0f;
			gap = 300f + MarkHash(tracks.Count, 17) * 220f;
		}
		GD.Print($"Familiar tracks route: {visits} cells searched, first evidence {nearest:0} px from spawn");
	}
	public override void _Process(double delta)
	{
		if (Retired) return;
		if (Site.State.Done || Director.FoundThisRun(Site.SecretId))
		{
			Retired = true;
			QueueRedraw();
			return;
		}
		clock += (float)delta;
		if (clock >= redrawAt) { redrawAt = clock + 0.1f; QueueRedraw(); }
	}
	private static float MarkHash(int index, int salt)
	{
		uint h = unchecked((uint)(index * 73856093) ^ (uint)(salt * 19349663));
		h = unchecked((h ^ (h >> 13)) * 1274126177u);
		return (h & 65535) / 65535f;
	}
	public override void _Draw()
	{
		if (Retired || PointsOfInterest.Suspended) return;
		Rect2 visible = GetViewportRect().Grow(140f);
		Transform2D canvas = GetViewport().GetCanvasTransform();
		for (int i = 0; i < tracks.Count; i++)
		{
			Vector2 at = tracks[i];
			if (!visible.HasPoint(canvas * at)) continue;
			Vector2 along = headings[i], across = along.Orthogonal();
			float pulse = 0.5f + 0.5f * MathF.Sin(clock * Mathf.Tau / 3.6f + i * 0.83f);
			int type = (i * 5 + 2) % 6;
			float length = 28f + MarkHash(i, 3) * 60f;
			// Different evidence, not one decal stamped at regular intervals: drag furrows,
			// claw fans, partial paws, a sideways skid, staggered steps and a broad scuff.
			if (type is 0 or 2 or 4 or 5)
			{
				int grooves = type == 2 ? 4 : type == 5 ? 1 : 2;
				for (int n = 0; n < grooves; n++)
				{
					float side = (n - (grooves - 1) * 0.5f) * (type == 2 ? 5f : 11f);
					Vector2 a = at - along * length * 0.5f + across * side;
					Vector2 bend = at + across * (side + (MarkHash(i + n, 7) - 0.5f) * 16f);
					Vector2 b = at + along * length * 0.45f + across * side * 1.4f;
					PixelInk.Line(this, a, bend, new Color("#543F10"), type == 5 ? 3 : 2, 0.9f);
					PixelInk.Line(this, bend, b, SecretSite.GoldBase, 2, 0.75f);
					PixelInk.Line(this, a + across * 2f, bend, SecretSite.GoldLit, 1, 0.2f + pulse * 0.6f);
					PixelInk.Line(this, bend, b, SecretSite.GoldLit, 1, 0.15f + pulse * 0.45f);
					PixelInk.Cell(this, b, SecretSite.GoldHi, pulse * 0.45f);
				}
			}
			int paws = type == 1 ? 1 : type == 2 ? 0 : type == 3 ? 4 : 2;
			for (int paw = 0; paw < paws; paw++)
			{
				float side = (paw % 2 == 0 ? 1f : -1f) * (14f + MarkHash(i + paw, 11) * 18f);
				Vector2 p = at + along * ((paw - (paws - 1) * 0.5f) * (13f + MarkHash(i, 5) * 17f)) + across * side;
				Vector2 facing = (-along + across * (MarkHash(i + paw, 23) * 2f - 1f)).Normalized();
				DrawPaw(p, facing, pulse, i + paw, type == 1);
			}
			if (type == 4)
				for (int k = 0; k < 5; k++)
				{
					Vector2 p = at + across * (k * 7f - 14f) - along * 20f;
					PixelInk.Line(this, p, p + across * 20f + along * 11f, SecretSite.GoldLit, 1, 0.2f + pulse * 0.5f);
				}
		}
	}
	private void DrawPaw(Vector2 at, Vector2 facing, float pulse, int seed, bool partial)
	{
		Vector2 spread = facing.Orthogonal();
		float size = 4f + MarkHash(seed, 31) * 2f;
		PixelInk.Disc(this, at, size, SecretSite.GoldBase);
		PixelInk.Disc(this, at - spread * 1f, size - 1f, SecretSite.GoldLit, 0.35f + pulse * 0.55f);
		PixelInk.Cell(this, at - facing * 2f, SecretSite.GoldHi, pulse * 0.25f);
		for (int toe = -2; toe <= 1; toe++)
		{
			if (partial && toe == -2) continue;
			Vector2 t = at + facing * (11f + (toe is -1 or 0 ? 2f : 0f)) + spread * (toe + 0.5f) * 4f;
			PixelInk.Disc(this, t, 2.2f, SecretSite.GoldLit, 0.5f + pulse * 0.5f);
			float scrape = 4f + MarkHash(seed + toe, 37) * 15f;
			PixelInk.Line(this, t + facing * 3f, t + facing * scrape, SecretSite.GoldLit, 1, 0.25f + pulse * 0.65f);
			PixelInk.Cell(this, t, SecretSite.GoldHi, pulse * 0.3f);
		}
	}
}
