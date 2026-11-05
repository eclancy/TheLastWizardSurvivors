using Godot;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// A teaching landmark's warm light reaches back toward the start. A single draw node, never a particle
// node per light or a per-frame terrain search. Ground is checked once; gaps across water stay gaps,
// and the POI arrow keeps the bearing while the player walks round them. No reward or lesson gate moves.
public partial class TeachingTrail : Node2D
{
	public SecretDirector Director { get; set; }
	public SecretSite Site { get; set; }
	public const float Spacing = 160f;
	public const float NoticeRadius = 300f;
	private readonly List<Vector2> lights = new();
	private PointOfInterest point;
	private float clock;
	private bool finished;
	public IReadOnlyList<Vector2> LightPositions => lights;
	public PointOfInterest Point => point;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		Vector2 start = Director.Game.StageOrigin;
		Vector2 destination = Site.GlobalPosition;
		Vector2 direction = (destination - start).Normalized();
		float length = start.DistanceTo(destination);
		for (float along = 144f; along < length - 120f; along += Spacing)
		{
			Vector2 at = (start + direction * along).Round();
			if (Director.Game.IsExploreGroundClearDense(at + direction * 36f, 52f))
				lights.Add(at);
		}
		point = PointsOfInterest.Add(new PointOfInterest(destination, "seal") { IsTeaching = true });
		GD.Print($"TeachingTrail: {Site.SecretId}, {lights.Count} lights, first at {(lights.Count > 0 ? lights[0].ToString() : "none")}");
	}

	public override void _Process(double delta)
	{
		if (finished)
			return;
		if (Site.State.Done || Director.FoundThisRun(Site.SecretId))
		{
			finished = true;
			point.Done = true;
			GD.Print($"TeachingTrail: {Site.SecretId} found; lights and marker retired");
			QueueRedraw();
			return;
		}
		clock += (float)delta;
		Player player = Director.Player;
		if (!point.Seen && player != null)
			foreach (Vector2 at in lights)
				if (player.GlobalPosition.DistanceSquaredTo(at) <= NoticeRadius * NoticeRadius)
				{
					point.Seen = true;
					GD.Print($"TeachingTrail: {Site.SecretId}'s light encountered; direction marked");
					break;
				}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (finished || PointsOfInterest.Suspended)
			return;
		Rect2 visible = GetViewportRect().Grow(40f);
		Transform2D canvas = GetViewport().GetCanvasTransform();
		Vector2 direction = (Site.GlobalPosition - Director.Game.StageOrigin).Normalized();
		for (int i = 0; i < lights.Count; i++)
		{
			Vector2 at = lights[i];
			if (!visible.HasPoint(canvas * at))
				continue;
			// A short train of glints travels toward the landmark, on the world's existing opaque 2px grid.
			for (int mote = 0; mote < 3; mote++)
			{
				float drift = Mathf.PosMod(clock * 36f + mote * 24f, 72f);
				SecretSite.DrawGlint(this, at + direction * drift, clock + i * 0.4f + mote, 1f);
			}
			SecretSite.DrawLight(this, at + direction * Mathf.PosMod(clock * 36f, 72f), 14f, 0.6f);
		}
	}

	public override void _ExitTree()
	{
		if (point != null)
			PointsOfInterest.Live.Remove(point);
	}
}
