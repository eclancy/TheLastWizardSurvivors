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
		float walked = 144f;
		for (int i = 0; i < route.Count; i++)
		{
			if (i > 0) walked += route[i].DistanceTo(route[i - 1]);
			if (walked < 144f && i < route.Count - 1) continue;
			if (route[i].DistanceTo(origin) < StartDistance - 48f) continue;
			if (!Director.Game.IsExploreGroundClearDense(route[i], 24f)) continue;
			tracks.Add(route[i]);
			Director.Game.ReserveExploreGround(route[i], 32f);
			headings.Add((route[Math.Min(i + 1, route.Count - 1)] - route[Math.Max(0, i - 1)]).Normalized());
			walked = 0f;
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
	public override void _Draw()
	{
		if (Retired || PointsOfInterest.Suspended) return;
		Rect2 visible = GetViewportRect().Grow(100f);
		Transform2D canvas = GetViewport().GetCanvasTransform();
		for (int i = 0; i < tracks.Count; i++)
		{
			Vector2 at = tracks[i];
			if (!visible.HasPoint(canvas * at)) continue;
			Vector2 along = headings[i], across = along.Orthogonal();
			float pulse = 0.5f + 0.5f * MathF.Sin(clock * Mathf.Tau / 3.2f + i * 0.61f);
			// Two interrupted drag furrows, then paws splayed AGAINST the pull: evidence of struggling.
			for (int side = -1; side <= 1; side += 2)
			{
				Vector2 a = at - along * 28f + across * (side * 7f);
				Vector2 b = at + along * 18f + across * (side * 9f);
				PixelInk.Line(this, a, b, new Color("#2E2208"), 2, 0.75f);
				PixelInk.Line(this, a + across * side * 2f, b, SecretSite.GoldBase, 1, 0.3f + pulse * 0.18f);
			}
			for (int paw = 0; paw < 3; paw++)
			{
				Vector2 p = at + along * (paw * 15f - 18f) + across * ((paw % 2 == 0 ? 1f : -1f) * 18f);
				Vector2 facing = (-along + across * (paw % 2 == 0 ? 0.7f : -0.7f)).Normalized();
				Vector2 spread = facing.Orthogonal();
				PixelInk.Disc(this, p, 3.8f, new Color("#543F10"));
				PixelInk.Line(this, p - spread * 2f, p + spread * 2f, SecretSite.GoldBase, 1, 0.45f + pulse * 0.3f);
				for (int toe = -1; toe <= 1; toe++)
				{
					Vector2 t = p + facing * 6f + spread * toe * 4f;
					PixelInk.Cell(this, t, SecretSite.GoldBase);
					PixelInk.Line(this, t + facing * 2f, t + facing * 8f, SecretSite.GoldLit, 1, pulse * 0.36f);
				}
			}
		}
	}
}
