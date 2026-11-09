using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TheOneHeMissed.scripts;

/// <summary>One stop on the world map, as tools/art/world_map.py placed it. All in art pixels.</summary>
public sealed class WorldMapStop
{
	/// <summary>A StageCatalog id, or <see cref="WorldMapLayout.BossRushId"/>.</summary>
	public string Id { get; init; } = string.Empty;
	/// <summary>The index of the stand point among the route's samples.</summary>
	public int RouteIndex { get; init; }
	/// <summary>Where the wizard's feet are when he waits here.</summary>
	public Vector2 Stand { get; init; }
	/// <summary>The landmark's middle: what the camera frames.</summary>
	public Vector2 Focus { get; init; }
	/// <summary>The fog's radii over a locked place; zero for a stop that is never fogged.</summary>
	public Vector2 Shroud { get; init; }
	public string ShroudImage { get; init; } = string.Empty;
	/// <summary>Whether the stand point is on the world picture. Every stop's is (the Sketchbook once stood
	/// off the world's edge, then on a page of its own; the chapter was removed on 2026-11-09).</summary>
	public bool OnMap { get; init; }
	/// <summary>Distance along the route to the stand point.</summary>
	public float ArcLength { get; set; }
}

/// <summary>
/// The world map's geometry, read from the file the generator writes beside the art
/// (assets/bonelight/ui/worldmap/worldmap.json). Nothing in code knows a coordinate: moving a place
/// is an edit to tools/art/world_map.py and a re-run (.ai/world-map.md).
/// </summary>
public sealed class WorldMapLayout
{
	public const string Directory = "res://assets/bonelight/ui/worldmap/";
	public const string Manifest = Directory + "worldmap.json";
	public const string BossRushId = "boss_rush";

	public Vector2 Size { get; private set; }
	public Vector2[] Route { get; private set; } = Array.Empty<Vector2>();
	/// <summary>Cumulative arc length at each route sample.</summary>
	public float[] Arc { get; private set; } = Array.Empty<float>();
	public List<WorldMapStop> Stops { get; } = new();

	// THE LIGHT GOING HOME (.ai/second-half.md 6.2, piece F5): what the map lays over the world once the Throne is
	// taken back - his tower unlit, and the path the warm motes stream east along off its crown. Art pixels.
	/// <summary>The unlit tower's picture (in <see cref="Directory"/>), drawn over the world after Taken Back; empty if none.</summary>
	public string TowerUnlitImage { get; private set; } = string.Empty;
	/// <summary>Where the unlit tower's picture sits on the world (its top-left), and its size.</summary>
	public Rect2 TowerUnlitRect { get; private set; }
	/// <summary>The motes' path, west to east: off his crown, over the dark sea and on over the far shore.</summary>
	public Vector2[] MotePath { get; private set; } = Array.Empty<Vector2>();

	public float Length => Arc.Length > 0 ? Arc[^1] : 0f;

	/// <summary>How far east the world is drawn, so the camera knows where it ends.</summary>
	public float RightEdge => Size.X;

	public WorldMapStop Get(string id)
	{
		foreach (WorldMapStop stop in Stops)
		{
			if (stop.Id.Equals(id ?? string.Empty, StringComparison.OrdinalIgnoreCase))
				return stop;
		}
		return null;
	}

	/// <summary>The layout, or null with an error printed when the file is missing or malformed.</summary>
	public static WorldMapLayout Load(out string problem)
	{
		problem = null;
		if (!FileAccess.FileExists(Manifest))
		{
			problem = $"{Manifest} is missing - run python tools/art/world_map.py";
			return null;
		}
		string text = FileAccess.GetFileAsString(Manifest);
		try
		{
			using JsonDocument doc = JsonDocument.Parse(text);
			JsonElement root = doc.RootElement;
			var layout = new WorldMapLayout();
			JsonElement size = root.GetProperty("size");
			layout.Size = new Vector2(size[0].GetSingle(), size[1].GetSingle());

			var route = new List<Vector2>();
			foreach (JsonElement p in root.GetProperty("route").EnumerateArray())
				route.Add(new Vector2(p[0].GetSingle(), p[1].GetSingle()));
			layout.Route = route.ToArray();
			layout.Arc = new float[layout.Route.Length];
			for (int i = 1; i < layout.Route.Length; i++)
				layout.Arc[i] = layout.Arc[i - 1] + layout.Route[i].DistanceTo(layout.Route[i - 1]);

			foreach (JsonElement s in root.GetProperty("stops").EnumerateArray())
			{
				int index = s.GetProperty("route_index").GetInt32();
				JsonElement stand = s.GetProperty("stand");
				JsonElement focus = s.GetProperty("focus");
				JsonElement shroud = s.GetProperty("shroud");
				layout.Stops.Add(new WorldMapStop
				{
					Id = s.GetProperty("id").GetString() ?? string.Empty,
					RouteIndex = index,
					Stand = new Vector2(stand[0].GetSingle(), stand[1].GetSingle()),
					Focus = new Vector2(focus[0].GetSingle(), focus[1].GetSingle()),
					Shroud = new Vector2(shroud[0].GetSingle(), shroud[1].GetSingle()),
					ShroudImage = s.GetProperty("shroud_image").GetString() ?? string.Empty,
					OnMap = s.GetProperty("on_map").GetBoolean(),
					ArcLength = index >= 0 && index < layout.Arc.Length ? layout.Arc[index] : 0f,
				});
			}

			// Optional, so a manifest from before the far shore still loads (it shows the tower lit, always).
			if (root.TryGetProperty("light_home", out JsonElement light))
			{
				layout.TowerUnlitImage = light.GetProperty("tower_image").GetString() ?? string.Empty;
				JsonElement towerAt = light.GetProperty("tower_at");
				JsonElement towerSize = light.GetProperty("tower_size");
				layout.TowerUnlitRect = new Rect2(towerAt[0].GetSingle(), towerAt[1].GetSingle(),
					towerSize[0].GetSingle(), towerSize[1].GetSingle());
				var motes = new List<Vector2>();
				foreach (JsonElement p in light.GetProperty("motes").EnumerateArray())
					motes.Add(new Vector2(p[0].GetSingle(), p[1].GetSingle()));
				layout.MotePath = motes.ToArray();
			}
			return layout;
		}
		catch (Exception e) when (e is JsonException || e is KeyNotFoundException || e is InvalidOperationException
			|| e is IndexOutOfRangeException)
		{
			problem = $"{Manifest} could not be read: {e.Message}";
			return null;
		}
	}

	/// <summary>The point on the road at arc length <paramref name="s"/>, clamped to its ends.</summary>
	public Vector2 PointAt(float s)
	{
		if (Route.Length == 0)
			return Vector2.Zero;
		int i = SegmentAt(s);
		if (i >= Route.Length - 1)
			return Route[^1];
		float span = Arc[i + 1] - Arc[i];
		float t = span <= 0.0001f ? 0f : Mathf.Clamp((s - Arc[i]) / span, 0f, 1f);
		return Route[i].Lerp(Route[i + 1], t);
	}

	/// <summary>The index of the route segment holding arc length <paramref name="s"/>. Binary search: no allocation.</summary>
	public int SegmentAt(float s)
	{
		if (Arc.Length < 2 || s <= 0f)
			return 0;
		if (s >= Arc[^1])
			return Arc.Length - 1;
		int lo = 0;
		int hi = Arc.Length - 1;
		while (hi - lo > 1)
		{
			int mid = (lo + hi) / 2;
			if (Arc[mid] <= s)
				lo = mid;
			else
				hi = mid;
		}
		return lo;
	}
}
