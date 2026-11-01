using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

// Measures whether the horde can actually reach the player, headless. Built for the maze; it reads
// the same navigation on open stages, where the obstacles are lakes.
//
// Run:
//   "$GODOT_BIN" --headless --path . scenes/_MazeProbe.tscn -- --stage=1 --seconds=240 --bot=roam
//   ... --stage=3 --no-open-routing   (Swamp, enemies chasing in a straight line as they did before
//                                      open stages had routing: the A/B for it)
//   "$GODOT_BIN" --headless --path . --fixed-fps 60 scenes/_MazeProbe.tscn -- --timescale=1 --stage=0
//       --seconds=180 --bot=circle --bot-speed=0.5 --stress-swarm --no-save-writes
//                                     (the crowd, at the game's own step: a full swarm, the player
//                                      circling at its pace - the jam measurement, see FlowWindowFrames)
//   ... --stage=12 --seconds=130 --timescale=2 --bot=corners --check-walls --no-save-writes
//                                     (the Court: every room's four corners walked into, and every cell
//                                      round each checked - wall flag, collision, layout - see CheckWalls)
//
// WHY THIS EXISTS. The Cursed Dungeon was reported as "enemies trapped in other corridors most of
// the time". A screenshot shows that it happens but not how often, and a fix judged by eye from two
// photographs is a fix judged on whichever two seconds the camera caught. This reads the shared flow
// field (MazeNavigation) for every live enemy every few seconds and reports four numbers:
//
//   reach    - share of enemies standing on a cell the field can route from at all
//   detour   - median of walking distance / straight-line distance; 1.0 is a straight run, and a
//              perfect maze puts neighbours seen through one wall twenty tiles of corridor apart
//   stalled  - share of enemies more than 4 tiles out whose walking distance has not fallen by a
//              tile and a half in three seconds: pinned on a corner, or queued in a jam
//   close    - share within 8 walking tiles of the player
//
// and five more on whether a body is held up by the bodies round it (speed, jammed, closing,
// stacked, contact - see FlowWindowFrames).
//
// The player is immortal and either stands still (--bot=still), roams the corridors, turning
// whenever it walks into a wall (--bot=roam), which is the case that actually strands enemies, or
// walks a circle (--bot=circle). --bot-speed slows roam and circle to the swarm's own pace.
// It renders nothing; whether the maze READS still needs a photograph (_UiShot).
public partial class _MazeProbe : Node
{
	[Export] public int StageIndex { get; set; } = 1;
	[Export] public float Seconds { get; set; } = 240f;
	[Export] public float TimeScale { get; set; } = 4f;
	[Export] public string Bot { get; set; } = "roam";
	[Export] public long Seed { get; set; } = 20261014;
	// --bot-speed=S: the bot walks at S of the player's speed (the move actions are analog). At 1 a
	// roaming player outruns the whole cast - the fastest enemy walks at under half his speed - so the
	// horde trails behind him in a thin stream and never crowds; at about 0.5 he walks at the swarm's
	// own pace and it presses round him while he moves, which is how a crowd is met in play.
	[Export] public float BotSpeed { get; set; } = 1f;
	// --bot=circle walks a circle of this radius (--circle=R): a player kiting round in arcs.
	[Export] public float CircleRadius { get; set; } = 260f;
	private float circleAngle;

	// --check-walls: do the places that must agree about a wall (.ai/terrain.md section 2) agree? Every
	// physics step it asks whether the player's feet stand in a cell the chunk data calls wall, and every
	// sample it walks the painted chunks round him cell by cell: the chunk's wall flag (what navigation and
	// placement read) against the ground's collision at the cell's centre (what the feet meet), and on an
	// Authored chapter against the layout's own grid (what the Court's rooms, its boss and the tools route
	// on). Built for the Court, whose Seneschal probe found the bot's feet in a cell the grid calls wall.
	// The roam bot walks into every wall it meets, which is the case to test.
	[Export] public bool CheckWalls { get; set; }
	private long wallFeetFrames, wallFeetInWall, wallCellsChecked, wallNoCollision, wallStrayCollision, wallFloorBodies, wallLayoutDisagrees;
	private readonly List<string> wallReports = new();
	private readonly HashSet<Vector2I> wallChunksSeen = new();
	private PhysicsPointQueryParameters2D groundPoint;

	private const float SampleEvery = 5f;
	private const float StallWindow = 3f;
	private const float WarmupSeconds = 45f;

	private Node game;
	private Player player;
	private float elapsed;
	private float sinceSample;
	private bool finished;

	private readonly Dictionary<ulong, (float At, float Path)> lastPath = new();
	private readonly List<float> detours = new();
	private readonly List<float> recomputeMs = new();
	private int samples;
	private double sumReach, sumStalled, sumPinned, sumSnagged, sumClose, sumDetour, sumAlive;

	// FLOW: is a body on its way to the player moving freely, or held up by the bodies around it?
	// ("enemies shouldn't get stuck on each other, they appear to be having a hard time chasing
	// player because of it", Eric, 2026-10-28.) Stalled and close above say whether the horde
	// arrives; these say why not, and none of them depends on HOW bodies keep apart, so a hard-body
	// crowd and a soft one are measured on the same terms:
	//
	//   speed    - distance a plain chaser actually covered in a window, over the distance its own
	//              Speed covers; about 1 for a body walking unhindered, whichever way it walks
	//   jammed   - share of those under JamRatio of their speed, and how many of them had another
	//              body (an enemy, or the player) in their last slide
	//   closing  - the fall in straight-line distance to the player over the distance its Speed
	//              covers; read it with --bot=still, where the player does not move the target
	//   stacked  - share of all non-boss bodies whose centre is within StackDistance of another's:
	//              the readability cost if bodies stop blocking each other
	//   contact  - enemies touching the player's HurtBox, which is what contact damage counts
	//
	// Only plain chasers (exactly Enemy) are timed: a caster holding its range or a slammer planted
	// mid wind-up is slow on purpose. Only bodies beyond FarRadius: the ranks pressed against the
	// player are meant to stop. Run it at the game's own step, or contacts are measured at a step
	// four times the size play has:
	//   "$GODOT_BIN" --headless --path . --fixed-fps 60 scenes/_MazeProbe.tscn -- --timescale=1 ...
	private const int FlowWindowFrames = 15;
	private const float FarRadius = 160f;
	private const float JamRatio = 0.4f;
	private const float StackDistance = 16f;
	private ulong flowFrame;
	private readonly Dictionary<ulong, (Vector2 Pos, float Dist, ulong Frame)> flowLast = new();
	private readonly List<Vector2> flowPositions = new();
	private long flowFar, flowJammed, flowJammedOnBody, flowBodies, flowStacked, contactSamples;
	private double flowRatioSum, flowClosingSum, contactSum;
	private Area2D hurtBox;

	// Roam state.
	private Vector2 heading = Vector2.Right;
	private Vector2 lastPos;
	private float sinceMoveCheck, sinceTurn;
	private readonly RandomNumberGenerator rng = new() { Seed = 4242 };

	private static readonly string[] MoveActions = { "move_left", "move_right", "move_up", "move_down" };
	private static readonly Vector2[] Cardinals = { Vector2.Right, Vector2.Left, Vector2.Down, Vector2.Up };

	public override void _Ready()
	{
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			if (arg.StartsWith("--stage=")) StageIndex = int.Parse(arg.Substring(8));
			else if (arg.StartsWith("--seconds=")) Seconds = float.Parse(arg.Substring(10));
			else if (arg.StartsWith("--timescale=")) TimeScale = float.Parse(arg.Substring(12));
			else if (arg.StartsWith("--bot=")) Bot = arg.Substring(6);
			else if (arg.StartsWith("--seed=")) Seed = long.Parse(arg.Substring(7));
			else if (arg.StartsWith("--bot-speed=")) BotSpeed = float.Parse(arg.Substring(12), System.Globalization.CultureInfo.InvariantCulture);
			else if (arg.StartsWith("--circle=")) CircleRadius = float.Parse(arg.Substring(9), System.Globalization.CultureInfo.InvariantCulture);
			else if (arg == "--no-open-routing") MazeNavigation.OpenStageRouting = false;
			else if (arg == "--check-walls") CheckWalls = true;
		}

		ProcessMode = ProcessModeEnum.Always;
		Global.SelectedCharacterIdx = 1;
		Global.SelectedStageIdx = StageIndex;
		Engine.TimeScale = TimeScale;
		GameStats.ResetRunTelemetry();

		game = ResourceLoader.Load<PackedScene>("res://scenes/node_2d_game.tscn").Instantiate();
		// One maze for every run of the probe, so a before and an after are measured on the same
		// corridors. Set before the add so _Ready builds the terrain with it.
		game.Set("ProceduralSeed", Seed);
		AddChild(game);
		GD.Print($"MAZEPROBE stage={StageIndex} seed={Seed} bot={Bot} bot-speed={BotSpeed} circle={CircleRadius} seconds={Seconds} timescale={TimeScale} open-routing={MazeNavigation.OpenStageRouting}");
	}

	public override void _ExitTree()
	{
		Engine.TimeScale = 1.0f;
		MazeNavigation.OpenStageRouting = true;
		foreach (string a in MoveActions)
			Input.ActionRelease(a);
	}

	public override void _Process(double delta)
	{
		if (finished)
			return;

		float d = (float)delta;
		elapsed += d;

		if (player == null || !IsInstanceValid(player))
		{
			player = FindNode<Player>(game);
			if (player == null)
				return;
			lastPos = player.GlobalPosition;
		}

		player.MaxHP = Math.Max(player.MaxHP, 100000);
		player.CurrentHP = player.MaxHP;
		AutoPickLevelUp();
		if (Bot == "roam")
			Roam(d);
		else if (Bot == "circle")
			Circle(d);
		else if (Bot == "corners")
			Corners(d);

		sinceSample += d;
		if (sinceSample >= SampleEvery)
		{
			sinceSample = 0f;
			Sample();
		}

		if (elapsed >= Seconds)
			Finish();
	}

	// The flow numbers (see FlowWindowFrames), on physics steps so a window is a fixed count of the
	// steps the enemies move on. A paused tree (a level-up menu the bot has not closed yet) moves
	// nothing, so it drops every record rather than timing a window that spans it.
	public override void _PhysicsProcess(double delta)
	{
		if (CheckWalls && !finished && player != null && IsInstanceValid(player) && !GetTree().Paused)
			CheckFeetInWall();
		if (finished || player == null || !IsInstanceValid(player) || elapsed < WarmupSeconds)
			return;
		if (GetTree().Paused)
		{
			flowLast.Clear();
			return;
		}
		flowFrame++;
		if (flowFrame % FlowWindowFrames != 0)
			return;

		float window = FlowWindowFrames * (float)delta;
		Vector2 p = player.GlobalPosition;
		flowPositions.Clear();
		foreach (Enemy e in EnemyRegistry.Snapshot)
		{
			if (!IsInstanceValid(e) || e is BossEnemy || e.IsMiniBoss || e.IsDying || !e.IsTargetable)
				continue;
			Vector2 pos = e.GlobalPosition;
			flowPositions.Add(pos);
			float dist = pos.DistanceTo(p);
			ulong id = e.GetInstanceId();
			if (e.GetType() == typeof(Enemy)
				&& flowLast.TryGetValue(id, out var prev)
				&& prev.Frame == flowFrame - FlowWindowFrames
				&& prev.Dist > FarRadius)
			{
				float expected = e.Speed * e.SwarmSpeedScale * window;
				float moved = pos.DistanceTo(prev.Pos);
				// Further than three windows' walk is the recycler moving it, not a step.
				if (expected > 1f && moved < expected * 3f)
				{
					float ratio = moved / expected;
					flowFar++;
					flowRatioSum += Mathf.Min(ratio, 2f);
					flowClosingSum += (prev.Dist - dist) / expected;
					if (ratio < JamRatio)
					{
						flowJammed++;
						if (LastSlideHitBody(e))
							flowJammedOnBody++;
					}
				}
			}
			flowLast[id] = (pos, dist, flowFrame);
		}

		// Records of enemies that died or were folded since: anything not refreshed this window.
		if (flowFrame % (FlowWindowFrames * 40) == 0)
		{
			foreach (ulong stale in flowLast.Where(kv => kv.Value.Frame != flowFrame).Select(kv => kv.Key).ToList())
				flowLast.Remove(stale);
		}

		float stackSquared = StackDistance * StackDistance;
		for (int i = 0; i < flowPositions.Count; i++)
		{
			for (int j = 0; j < flowPositions.Count; j++)
			{
				if (i != j && flowPositions[i].DistanceSquaredTo(flowPositions[j]) < stackSquared)
				{
					flowStacked++;
					break;
				}
			}
		}
		flowBodies += flowPositions.Count;

		hurtBox ??= player.GetNodeOrNull<Area2D>("HurtBox");
		if (hurtBox != null)
		{
			int touching = 0;
			foreach (Node2D body in hurtBox.GetOverlappingBodies())
			{
				if (body is Enemy toucher && toucher.IsInGroup(EnemyRegistry.GroupName))
					touching++;
			}
			contactSum += touching;
			contactSamples++;
		}
	}

	// Another body - an enemy, or the player - in the last slide: held up by the crowd rather than
	// by the ground.
	private static bool LastSlideHitBody(Enemy e)
	{
		for (int i = 0; i < e.GetSlideCollisionCount(); i++)
		{
			if (e.GetSlideCollision(i).GetCollider() is CharacterBody2D)
				return true;
		}
		return false;
	}

	private void Sample()
	{
		MazeNavigation nav = MazeNavigation.Active;
		if (nav == null)
		{
			GD.Print("MAZEPROBE no navigation on this stage - nothing to measure.");
			Finish();
			return;
		}

		// What one whole recompute costs, in one go. In play it is built a slice a frame
		// (MazeNavigation.FieldCellsPerFrame), at most five times a second and not at all while the
		// player holds one cell, so this is the total a field costs rather than what any frame pays.
		if (CheckWalls)
			CheckWallCells();

		var watch = System.Diagnostics.Stopwatch.StartNew();
		nav.RecomputeNow(player.FootPosition);
		watch.Stop();
		recomputeMs.Add((float)watch.Elapsed.TotalMilliseconds);

		int alive = 0, reach = 0, stalled = 0, eligible = 0, close = 0, pinned = 0, snagged = 0;
		detours.Clear();
		Vector2 p = player.GlobalPosition;
		var seen = new HashSet<ulong>();
		foreach (Node n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy e || !IsInstanceValid(e) || e is BossEnemy)
				continue;
			alive++;
			ulong id = e.GetInstanceId();
			seen.Add(id);
			float path = nav.PathTilesAt(e.GlobalPosition);
			if (path < 0f)
				continue;
			reach++;
			if (path <= 8f)
				close++;

			float straight = e.GlobalPosition.DistanceTo(p) / 48f;
			if (straight > 3f)
				detours.Add(path / straight);

			if (lastPath.TryGetValue(id, out var prev) && elapsed - prev.At >= StallWindow - 0.01f && prev.Path > 4f)
			{
				eligible++;
				if (prev.Path - path < 1.5f)
				{
					stalled++;
					if (IsAgainstWall(e))
						pinned++;
					else if (IsAgainstTrunkOrFence(e))
						snagged++;
				}
			}
			if (!lastPath.ContainsKey(id) || elapsed - lastPath[id].At >= StallWindow - 0.01f)
				lastPath[id] = (elapsed, path);
		}
		foreach (ulong stale in lastPath.Keys.Where(k => !seen.Contains(k)).ToList())
			lastPath.Remove(stale);

		detours.Sort();
		float medianDetour = detours.Count > 0 ? detours[detours.Count / 2] : 0f;
		float reachPct = alive > 0 ? 100f * reach / alive : 0f;
		float stallPct = eligible > 0 ? 100f * stalled / eligible : 0f;
		float closePct = alive > 0 ? 100f * close / alive : 0f;
		float pinPct = eligible > 0 ? 100f * pinned / eligible : 0f;
		float snagPct = eligible > 0 ? 100f * snagged / eligible : 0f;
		GD.Print($"MAZEPROBE t={elapsed:0}s alive={alive} reach={reachPct:0}% detour={medianDetour:0.00} stalled={stallPct:0}% pinned={pinPct:0}% snagged={snagPct:0}% close={closePct:0}%");
		// When most of the horde reads as unreachable, say where the player is standing: a field seeded
		// from the wrong cell (a shoreline, a garden fence) is the usual reason, and it is invisible
		// from the numbers alone.
		if (reachPct < 90f && Node2DGame.Current?.Terrain is TerrainStreamer t)
		{
			Vector2 foot = player.FootPosition;
			string on = t.IsFenceAtWorld(foot) ? "a fence cell" : t.IsBlockedAtWorld(foot) ? "water" : "open ground";
			GD.Print($"MAZEPROBE   low reach: feet at ({foot.X:0},{foot.Y:0}) on {on}, cell {t.CellOfWorld(foot)}, window centre {nav.WindowCentreChunk}");
		}

		if (elapsed >= WarmupSeconds && alive > 0)
		{
			samples++;
			sumAlive += alive;
			sumReach += reachPct;
			sumStalled += stallPct;
			sumPinned += pinPct;
			sumSnagged += snagPct;
			sumClose += closePct;
			sumDetour += medianDetour;
		}
	}

	// A stalled enemy whose last slide hit the ground - a maze wall (a TileMapLayer) or water (a
	// WaterSurface's body, since water stopped being tiles) - rather than another body is pinned on
	// geometry: the flow field's fault, not a queue's.
	private static bool IsAgainstWall(Enemy e)
	{
		for (int i = 0; i < e.GetSlideCollisionCount(); i++)
		{
			if (e.GetSlideCollision(i).GetCollider() is TileMapLayer or WaterSurface)
				return true;
		}
		return false;
	}

	// Stalled against a tree trunk or a flower pen's fence - the two obstacles on the ground's layer
	// that are NOT in the navigation grid as walls of their own kind (trunks are not in it at all; a
	// fence is, as FlagFence). Counted apart from pinned so a wood that walls the swarm off, or a
	// garden it wedges on, shows up as its own number (.ai/terrain.md section 10).
	private static bool IsAgainstTrunkOrFence(Enemy e)
	{
		for (int i = 0; i < e.GetSlideCollisionCount(); i++)
		{
			if (e.GetSlideCollision(i).GetCollider() is StaticBody2D body
				&& (body.IsInGroup("decor_props") || body.GetParent() is Landmark))
				return true;
		}
		return false;
	}

	// --- --check-walls -----------------------------------------------------------------------------

	private void CheckFeetInWall()
	{
		if (Node2DGame.Current?.Terrain is not TerrainStreamer t)
			return;
		wallFeetFrames++;
		Vector2 foot = player.FootPosition;
		if (!t.IsWallAtWorld(foot))
			return;
		wallFeetInWall++;
		if (wallFeetInWall <= 5)
			ReportWall($"feet in a wall cell at t={elapsed:0.0}s: footprint ({foot.X:0},{foot.Y:0}), {DescribeCell(t.CellOfWorld(foot))}");
	}

	// The painted chunks round the player, every cell: flag against collision, and flag against the layout.
	private void CheckWallCells()
	{
		if (Node2DGame.Current is not Node2DGame run || run.Terrain is not TerrainStreamer t)
			return;
		PhysicsDirectSpaceState2D space = player.GetWorld2D().DirectSpaceState;
		groundPoint ??= new PhysicsPointQueryParameters2D
		{
			CollisionMask = 1u << (LevelTilePainter.TerrainCollisionLayer - 1),
			CollideWithBodies = true,
			CollideWithAreas = false,
		};
		AuthoredLayout layout = AuthoredLayout.For(StageEnvironmentCatalog.GetForStageIndex(StageIndex).Kind);
		Vector2I centre = t.ChunkOfWorld(player.GlobalPosition);
		float half = run.ProceduralTilePixels * 0.5f;
		for (int cy = centre.Y - 2; cy <= centre.Y + 2; cy++)
		{
			for (int cx = centre.X - 2; cx <= centre.X + 2; cx++)
			{
				TerrainChunk chunk = t.DataFor(new Vector2I(cx, cy));
				if (!chunk.Painted)
					continue;
				wallChunksSeen.Add(chunk.Coord);
				for (int y = 0; y < TerrainStreamer.ChunkTiles; y++)
				{
					for (int x = 0; x < TerrainStreamer.ChunkTiles; x++)
					{
						var cell = new Vector2I(chunk.MinCell.X + x, chunk.MinCell.Y + y);
						bool wall = (chunk.Flags[x + y * TerrainStreamer.ChunkTiles] & TerrainStreamer.FlagWall) != 0;
						groundPoint.Position = t.CellOriginWorld(cell) + new Vector2(half, half);
						bool ground = false, body = false;
						foreach (Godot.Collections.Dictionary hit in space.IntersectPoint(groundPoint, 4))
						{
							if (hit["collider"].AsGodotObject() is TileMapLayer)
								ground = true;
							else
								body = true;
						}
						wallCellsChecked++;
						if (wall && !ground)
						{
							wallNoCollision++;
							if (wallNoCollision <= 5)
								ReportWall($"a wall cell with no collision: {DescribeCell(cell)}");
						}
						else if (!wall && ground && !t.IsBlockedAtWorld(groundPoint.Position))
						{
							wallStrayCollision++;
							if (wallStrayCollision <= 5)
								ReportWall($"wall collision on a floor cell: {DescribeCell(cell)}");
						}
						if (!wall && body)
							wallFloorBodies++;
						if (layout != null && layout.IsWalkable(layout.CellOfWorldTile(cell)) == wall)
						{
							wallLayoutDisagrees++;
							if (wallLayoutDisagrees <= 5)
								ReportWall($"the layout and the chunk disagree: {DescribeCell(cell)}, chunk {(wall ? "wall" : "floor")}");
						}
					}
				}
			}
		}
	}

	// A world cell, and on an Authored chapter the layout's cell it is and what the layout says of it.
	private string DescribeCell(Vector2I cell)
	{
		string s = $"world cell {cell}";
		AuthoredLayout layout = AuthoredLayout.For(StageEnvironmentCatalog.GetForStageIndex(StageIndex).Kind);
		if (layout != null)
		{
			Vector2I g = layout.CellOfWorldTile(cell);
			s += $" = layout cell {g} ({(layout.IsWalkable(g) ? "floor" : "wall")}{(layout.RoomAt(g) is AuthoredLayout.Room r ? ", " + r.Id : "")})";
		}
		return s;
	}

	private void ReportWall(string line)
	{
		wallReports.Add(line);
		GD.Print($"MAZEPROBE WALLS {line}");
	}

	// Walk one way until a wall stops it, then turn - a crude corridor crawler, which is all it needs
	// to be: the subject is whether the horde keeps up, not where the player goes.
	private void Roam(float delta)
	{
		sinceMoveCheck += delta;
		sinceTurn += delta;
		if (sinceMoveCheck >= 0.5f)
		{
			bool blocked = player.GlobalPosition.DistanceTo(lastPos) < 10f;
			lastPos = player.GlobalPosition;
			sinceMoveCheck = 0f;
			if (blocked || sinceTurn > 7f)
			{
				Vector2 next;
				do next = Cardinals[rng.RandiRange(0, 3)];
				while (next == heading);
				heading = next;
				sinceTurn = 0f;
			}
		}

		Press("move_right", "move_left", heading.X * BotSpeed);
		Press("move_down", "move_up", heading.Y * BotSpeed);
	}

	// --bot=corners (an Authored chapter, with --check-walls): every room of the layout in turn, set down a
	// cell and a half inside each of its four corners and walking hard into the corner for a second - the
	// inner corner of two walls is where a footprint can be squeezed between two bodies' shapes. It sees the
	// whole building, where the roam bot sees what it happens to walk into; each visit also lets the cell
	// scan (--check-walls) read the chunks round that room.
	private readonly List<(Vector2 Start, Vector2 Push, string Name)> cornerVisits = new();
	private int cornerIndex = -1;
	private float cornerClock;
	private int cornerBodyInWall;
	private const float CornerPushSeconds = 1.2f;

	private void Corners(float delta)
	{
		if (cornerIndex < 0)
		{
			cornerIndex = 0;
			AuthoredLayout layout = AuthoredLayout.For(StageEnvironmentCatalog.GetForStageIndex(StageIndex).Kind);
			if (layout == null || Node2DGame.Current is not Node2DGame run)
			{
				GD.Print("MAZEPROBE --bot=corners needs an Authored chapter (the Court); standing still.");
				return;
			}
			float tile = run.ProceduralTilePixels;
			foreach (AuthoredLayout.Room room in layout.Rooms)
			{
				Rect2I r = room.Rect;
				var corners = new (Vector2I Cell, Vector2 Push, string Name)[]
				{
					(new Vector2I(r.Position.X, r.Position.Y), new Vector2(-1f, -1f), "NW"),
					(new Vector2I(r.End.X - 1, r.Position.Y), new Vector2(1f, -1f), "NE"),
					(new Vector2I(r.Position.X, r.End.Y - 1), new Vector2(-1f, 1f), "SW"),
					(new Vector2I(r.End.X - 1, r.End.Y - 1), new Vector2(1f, 1f), "SE"),
				};
				foreach (var (cell, push, name) in corners)
				{
					if (!layout.IsWalkable(cell))
						continue;
					// The corner cell's centre, a cell and a half back from the corner.
					Vector2 centre = run.StageOrigin + ((Vector2)layout.WorldTileOf(cell) + new Vector2(0.5f, 0.5f)) * tile;
					cornerVisits.Add((centre - push * tile * 1.5f, push.Normalized(), $"{room.Id} {name}"));
				}
			}
			GD.Print($"MAZEPROBE --bot=corners: {cornerVisits.Count} corners in {layout.Rooms.Count} rooms");
			cornerClock = CornerPushSeconds;
		}
		if (cornerVisits.Count == 0)
			return;
		cornerClock += delta;
		if (cornerClock >= CornerPushSeconds)
		{
			cornerClock = 0f;
			// Where the push ended: the feet on the floor (CheckFeetInWall says if not), and the body's
			// centre, 36 px above them, often in the wall the feet stopped at - which is a cell the grid
			// calls wall, and is not where he stands. Read a cell from FootPosition, never GlobalPosition.
			if (cornerIndex > 0 && cornerIndex <= cornerVisits.Count && Node2DGame.Current?.Terrain is TerrainStreamer t
				&& t.IsWallAtWorld(player.GlobalPosition) && !t.IsWallAtWorld(player.FootPosition) && (++cornerBodyInWall <= 2 || cornerVisits[cornerIndex - 1].Name.StartsWith("map_room")))
				GD.Print($"MAZEPROBE corners: at {cornerVisits[cornerIndex - 1].Name} the feet stand on {DescribeCell(t.CellOfWorld(player.FootPosition))} "
					+ $"and the body's centre is over {DescribeCell(t.CellOfWorld(player.GlobalPosition))}");
			var (start, _, name) = cornerVisits[cornerIndex % cornerVisits.Count];
			cornerIndex++;
			// Set down by the feet, which are what the walls meet.
			player.GlobalPosition = start - (player.FootPosition - player.GlobalPosition);
			player.Velocity = Vector2.Zero;
			if (cornerIndex <= cornerVisits.Count && cornerIndex % 8 == 1)
				GD.Print($"MAZEPROBE corners: {cornerIndex}/{cornerVisits.Count} ({name})");
		}
		Vector2 h = cornerVisits[(cornerIndex - 1 + cornerVisits.Count) % cornerVisits.Count].Push * BotSpeed;
		Press("move_right", "move_left", h.X);
		Press("move_down", "move_up", h.Y);
	}

	// Round and round at a steady turn, whatever is in the way: the subject is the crowd that follows.
	private void Circle(float delta)
	{
		circleAngle += BotSpeed * player.Speed / Mathf.Max(1f, CircleRadius) * delta;
		Vector2 h = Vector2.FromAngle(circleAngle) * BotSpeed;
		Press("move_right", "move_left", h.X);
		Press("move_down", "move_up", h.Y);
	}

	// Analog, as a stick is: the strength is how hard the action is pressed, and the player's walk
	// reads it (Player.MovePlayer), so a strength of 0.5 walks at half his speed.
	private static void Press(string positive, string negative, float axis)
	{
		if (axis > 0.02f) { Input.ActionRelease(negative); Input.ActionPress(positive, Mathf.Min(1f, axis)); }
		else if (axis < -0.02f) { Input.ActionRelease(positive); Input.ActionPress(negative, Mathf.Min(1f, -axis)); }
		else { Input.ActionRelease(positive); Input.ActionRelease(negative); }
	}

	private void AutoPickLevelUp()
	{
		LevelUpMenu menu = FindNode<LevelUpMenu>(game);
		if (menu == null || !menu.Visible)
			return;

		IReadOnlyList<LevelUpOption> options = menu.GetOfferedOptions();
		if (options == null || options.Count == 0)
			return;

		LevelUpOption pick = options.FirstOrDefault(o => o.IsNewUnlock)
			?? options.OrderBy(o => o.NextLevel).First();
		menu.EmitSignal(LevelUpMenu.SignalName.WeaponSelected, pick.AutoChoice());
	}

	private void Finish()
	{
		finished = true;
		foreach (string a in MoveActions)
			Input.ActionRelease(a);

		if (samples > 0)
		{
			GD.Print("");
			GD.Print($"MAZEPROBE RESULT stage {StageIndex} bot={Bot} ({samples} samples after {WarmupSeconds:0}s)");
			GD.Print($"  alive   : {sumAlive / samples:0}");
			GD.Print($"  reach   : {sumReach / samples:0.0}%");
			GD.Print($"  detour  : {sumDetour / samples:0.00}");
			GD.Print($"  stalled : {sumStalled / samples:0.0}%");
			GD.Print($"  pinned  : {sumPinned / samples:0.0}%  (stalled with a wall in the last slide)");
			GD.Print($"  snagged : {sumSnagged / samples:0.0}%  (stalled on a decor body or a garden fence)");
			recomputeMs.Sort();
			GD.Print($"  recompute: median {recomputeMs[recomputeMs.Count / 2]:0.00} ms, worst {recomputeMs[^1]:0.00} ms (desktop)");
			GD.Print($"  close   : {sumClose / samples:0.0}%");
		}
		if (flowFar > 0)
		{
			GD.Print($"  speed   : {flowRatioSum / flowFar:0.00} of its own  (plain chasers beyond {FarRadius:0} px, {flowFar} windows)");
			GD.Print($"  jammed  : {100.0 * flowJammed / flowFar:0.0}%  (under {JamRatio:0.0} of its speed; {100.0 * flowJammedOnBody / flowFar:0.0}% with a body in its last slide)");
			GD.Print($"  closing : {flowClosingSum / flowFar:0.00}  (distance to the player shed, over its own travel)");
		}
		if (flowBodies > 0)
			GD.Print($"  stacked : {100.0 * flowStacked / flowBodies:0.0}%  (centre within {StackDistance:0} px of another)");
		if (contactSamples > 0)
			GD.Print($"  contact : {contactSum / contactSamples:0.00} enemies touching the player");
		if (CheckWalls)
		{
			GD.Print($"MAZEPROBE WALLS SUMMARY stage={StageIndex} feetInWall={wallFeetInWall}/{wallFeetFrames} frames, cells={wallCellsChecked} in {wallChunksSeen.Count} chunks "
				+ $"wallNoCollision={wallNoCollision} collisionOnFloor={wallStrayCollision} bodiesOnFloor={wallFloorBodies} layoutDisagrees={wallLayoutDisagrees}");
		}
		GD.Print($"MAZEPROBE SUMMARY stage={StageIndex} bot={Bot} reach={(samples > 0 ? sumReach / samples : 0):0.0} detour={(samples > 0 ? sumDetour / samples : 0):0.00} stalled={(samples > 0 ? sumStalled / samples : 0):0.0} close={(samples > 0 ? sumClose / samples : 0):0.0} speed={(flowFar > 0 ? flowRatioSum / flowFar : 0):0.00} jammed={(flowFar > 0 ? 100.0 * flowJammed / flowFar : 0):0.0} onbody={(flowFar > 0 ? 100.0 * flowJammedOnBody / flowFar : 0):0.0} closing={(flowFar > 0 ? flowClosingSum / flowFar : 0):0.00} stacked={(flowBodies > 0 ? 100.0 * flowStacked / flowBodies : 0):0.0} contact={(contactSamples > 0 ? contactSum / contactSamples : 0):0.00} alive={(samples > 0 ? sumAlive / samples : 0):0}");

		player = null;
		game = null;
		GetTree().Quit();
	}

	private static T FindNode<T>(Node root) where T : Node
	{
		if (root == null)
			return null;
		if (root is T hit)
			return hit;
		foreach (Node child in root.GetChildren())
		{
			T found = FindNode<T>(child);
			if (found != null)
				return found;
		}
		return null;
	}
}
