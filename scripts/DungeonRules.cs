using Godot;
using System.Collections.Generic;
using System.Text.Json;
using TheOneHeMissed.scripts;

// THE CURSED DUNGEON IN THE CASTLE (.ai/endgame-castle.md section 4, piece C2). The chapter moved in as it
// was - its maze, its key, its portal and its boss are the road's own - and it asks one thing of the hooks:
// the stair foot at the start (4.1, "a stair foot drawn in the spawn clearing - the way down from the
// bridge's gate"), so the first thing the chapter shows is that it is under his house. The art is
// tools/art/props_dungeon.py's, and stair.json says where its foot and its walls are.
//
// Hook 1 stands it just north of where the wizard appears, inside the clearing the maze always opens
// round the start (LevelGenerator.ClearSpawn, four tiles). Hook 3 closes its footprint to everything
// placed, so no chest, event site, secret or portal lands on the steps. Its back wall and cheek walls
// block like the old decor props (layer 1: the wizard's body and every enemy meet it); the flight between
// them is walked on, and sorts under whoever stands on it.
public partial class DungeonRules : ChapterRules
{
	public const string Manifest = "res://assets/bonelight/world/dungeon/stair.json";

	/// <summary>World px from the wizard's start to the stair's foot, north: a step in front of him.</summary>
	private const float FootNorthOfStart = 24f;

	private Texture2D texture;
	private Vector2 footInPng;
	private float backRowInPng;
	private readonly List<Rect2> blocks = new();
	private Rect2 footprint;
	private bool loaded;

	public override void _Ready()
	{
		// Before anything is placed: hook 3 is asked while the terrain, the event and the secrets are laid.
		loaded = LoadManifest();
	}

	private Vector2 Foot => Game.StageOrigin + new Vector2(0f, -FootNorthOfStart);

	private bool LoadManifest()
	{
		string text = FileAccess.GetFileAsString(Manifest);
		if (string.IsNullOrEmpty(text))
		{
			GD.PushError($"DungeonRules: {Manifest} is missing; the Dungeon has no stair. Run: python tools/art/props_dungeon.py, then import.");
			return false;
		}
		using JsonDocument doc = JsonDocument.Parse(text);
		JsonElement root = doc.RootElement;
		texture = GD.Load<Texture2D>(root.GetProperty("texture").GetString());
		if (texture == null)
		{
			GD.PushError("DungeonRules: the stair's texture is not imported; the Dungeon has no stair.");
			return false;
		}
		JsonElement foot = root.GetProperty("foot");
		footInPng = new Vector2(foot[0].GetSingle(), foot[1].GetSingle());
		backRowInPng = root.GetProperty("back").GetSingle();
		Vector2 at = Foot;
		footprint = new Rect2();
		foreach (JsonElement b in root.GetProperty("blocks").EnumerateArray())
		{
			var rect = new Rect2(at.X + b[0].GetSingle(), at.Y + b[1].GetSingle(), b[2].GetSingle(), b[3].GetSingle());
			blocks.Add(rect);
			footprint = footprint.HasArea() ? footprint.Merge(rect) : rect;
		}
		// The whole stair, flight and all, from its back wall down to its foot.
		footprint = footprint.Expand(at);
		return true;
	}

	// Hook 1: the stair, once the stage exists.
	public override void RunStarted()
	{
		if (!loaded)
			return;
		Vector2 foot = Foot;
		var root = new Node2D { Name = "DungeonStair", YSortEnabled = true };

		// It sorts at its back wall's ground line (WorldSprites.SortLift above it, as every grounded sprite
		// does): a wall with the floor in front of it, so whoever stands at its foot or on its steps is drawn
		// in front, and whoever is north of it behind.
		float backY = foot.Y - (footInPng.Y - backRowInPng);
		root.AddChild(new Sprite2D
		{
			Texture = texture,
			Centered = false,
			Position = new Vector2(foot.X, backY - WorldSprites.SortLift),
			Offset = new Vector2(-footInPng.X, -backRowInPng + WorldSprites.SortLift),
		});

		var body = new StaticBody2D { Name = "StairWalls", CollisionLayer = 1, CollisionMask = 0, YSortEnabled = false };
		foreach (Rect2 rect in blocks)
		{
			body.AddChild(new CollisionShape2D
			{
				Position = rect.GetCenter(),
				Shape = new RectangleShape2D { Size = rect.Size },
			});
		}
		root.AddChild(body);
		Game.AddChild(root);
	}

	// Hook 3: nothing placed on the stair.
	public override bool IsPlacementBlocked(Vector2 point) => loaded && footprint.HasPoint(point);
}
