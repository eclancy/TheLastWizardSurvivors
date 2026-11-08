using System;
using System.Collections.Generic;
using Godot;

namespace TheOneHeMissed.scripts;

// THE LANTERN-BEARERS' ROUNDS (.ai/unlock-puzzles.md 3.8, GaolersKeyEvent). Tituba's rescue: the way home from the Warden's
// post to her cell is swept by the gaol's lantern-bearers walking their rounds, each throwing a cone of cold light ahead
// (LanternCone). Caught in a cone with the key and the key drops where he stands. The insight is watching the rounds,
// waiting in a side passage, and slipping through.
//
// Everything here is pure - no node, no scene - so the event, the validator (RegressionChecks.GaolersKey) and the probe
// (_ExploreProbe --stage=1) step the same rules:
//
//   GaolGround  the maze's lattice as the rounds read it: cell centres a pitch apart, a passage open where the wall strip
//               between two centres is floor (.ai/terrain.md section 6), and the wall flag the light stops at - read once
//               into an array round the cell (Bake), so the rest may run off the main thread (BakedOnly).
//   GaolRound   one bearer's round: a loop of cells, walked centre to centre at Speed, the lantern turning into a corner a
//               stride before the feet do (LookAhead).
//   GaolLayout  the seed: the Warden's post four to six cells from the cell, and one to three rounds laid across or beside
//               the way between - kept only if the gap proof below passes at several phase sets (a turned bearer goes back
//               to its round at any phase), and the one that bites a straight walk home hardest. WayHeKnows: the way
//               home and its side passages, the ground a player who watches the rounds walks.
//   GaolWalk    the walk graph the proof and the probe walk: every cell centre and a point every Step along each open
//               passage, so a walker on it keeps to the middle of the corridors.
//   GaolSweep   what one round lights, at every step of its loop.
//   GaolSlip    the proof: a walker carrying the key, one Step a Tick (200 px a second, under his 220), who may stand
//               still wherever it is dark, searched forward in time. "A gap exists" means this finds the cell.
//   KeyCarry    the key's rule: taken up by stepping on it, lost where he stands when a light falls on his feet.

/// <summary>The maze's lattice, as the gaol's rounds read it. In world coordinates.</summary>
public sealed class GaolGround
{
	/// <summary>The world point at the middle of lattice cell (0, 0) - the stage origin on the Cursed Dungeon.</summary>
	public Vector2 Origin { get; init; }

	/// <summary>Pixels from one cell centre to the next: a six-tile corridor block and its two-tile wall strip.</summary>
	public float Pitch { get; init; }

	/// <summary>True if a world point is wall: what stops a walker and what stops the light. The streamer's flag in a run.</summary>
	public Func<Vector2, bool> IsWall { get; init; }

	/// <summary>The ground's tile, px: the walls are whole tiles, aligned on <see cref="Origin"/>. 0 if it cannot be baked.</summary>
	public float Tile { get; init; }

	// THE WALLS, BAKED. The rounds and every lantern ask "is this a wall" hundreds of thousands of times, and the streamer
	// answers through a dictionary of chunks; the walls never move, so the box round the cell is read once into an array
	// (Bake) and every later question inside it is an index. Outside the box the source is asked, as before.
	private bool[] baked;
	private int bakedX0, bakedY0, bakedW, bakedH;
	private Func<Vector2, bool> wallTest;

	/// <summary>
	/// While set, a question outside the baked box is answered "wall" rather than asked of the source: the generator then
	/// reads nothing but the array, so it may run off the main thread (the streamer it bakes from is not thread-safe).
	/// GaolersKeyEvent sets it for the worker and clears it once the layout is in.
	/// </summary>
	public bool BakedOnly { get; set; }

	/// <summary>
	/// Questions asked outside the baked box while <see cref="BakedOnly"/> was set. The validator lays its gaols that way and
	/// holds this to 0: the generator never looks past the box, so the worker lays exactly what a main-thread run would.
	/// </summary>
	public int OutsideQueries { get; private set; }

	/// <summary>
	/// Reads every wall tile of the cells in [<paramref name="minCell"/>, <paramref name="maxCell"/>] (and a cell round) once.
	/// A box already baked is not read again.
	/// </summary>
	public void Bake(Vector2I minCell, Vector2I maxCell)
	{
		if (BeginBake(minCell, maxCell))
			while (!BakeStep(int.MaxValue)) { }
	}

	// The bake under way (BeginBake / BakeStep): the array is filled into a staging copy and only goes live, whole, when the
	// last tile is read - a half-baked box would answer "open floor" for every tile not yet read.
	private bool[] baking;
	private int bakeX0, bakeY0, bakeW, bakeH, bakeNext;

	/// <summary>
	/// Starts reading the box a few rows at a time (<see cref="BakeStep"/>), for a caller that must not stall a frame on it:
	/// the streamer makes each chunk's data the first time a tile of it is asked for. False if the box is already baked.
	/// </summary>
	public bool BeginBake(Vector2I minCell, Vector2I maxCell)
	{
		if (Tile <= 0f)
			return false;
		int perCell = Mathf.RoundToInt(Pitch / Tile);
		if (baked != null && (minCell.X - 1) * perCell >= bakedX0 && (minCell.Y - 1) * perCell >= bakedY0
			&& (maxCell.X + 2) * perCell <= bakedX0 + bakedW && (maxCell.Y + 2) * perCell <= bakedY0 + bakedH)
			return false;
		bakeX0 = (minCell.X - 1) * perCell;
		bakeY0 = (minCell.Y - 1) * perCell;
		bakeW = (maxCell.X - minCell.X + 3) * perCell;
		bakeH = (maxCell.Y - minCell.Y + 3) * perCell;
		baking = new bool[bakeW * bakeH];
		bakeNext = 0;
		return true;
	}

	/// <summary>Reads up to <paramref name="tiles"/> more tiles of the bake under way; true once it is whole and live.</summary>
	public bool BakeStep(int tiles)
	{
		if (baking == null)
			return true;
		int end = (int)Math.Min((long)bakeNext + tiles, baking.Length);
		for (int i = bakeNext; i < end; i++)
			baking[i] = IsWall(Origin + new Vector2((bakeX0 + i % bakeW + 0.5f) * Tile, (bakeY0 + i / bakeW + 0.5f) * Tile));
		bakeNext = end;
		if (bakeNext < baking.Length)
			return false;
		(bakedX0, bakedY0, bakedW, bakedH) = (bakeX0, bakeY0, bakeW, bakeH);
		baked = baking;
		baking = null;
		return true;
	}

	/// <summary>Whether a world point is wall: from the baked box when it is inside it, from the source otherwise.</summary>
	public bool Wall(Vector2 world)
	{
		if (baked != null)
		{
			int x = Mathf.FloorToInt((world.X - Origin.X) / Tile) - bakedX0;
			int y = Mathf.FloorToInt((world.Y - Origin.Y) / Tile) - bakedY0;
			if (x >= 0 && y >= 0 && x < bakedW && y < bakedH)
				return baked[x + y * bakedW];
		}
		if (BakedOnly)
		{
			OutsideQueries++;
			return true;
		}
		return IsWall(world);
	}

	/// <summary><see cref="Wall"/> as a delegate, made once, for the lanterns to march their rays against.</summary>
	public Func<Vector2, bool> WallTest => wallTest ??= Wall;

	public Vector2 CentreOf(Vector2I cell) => Origin + new Vector2(cell.X, cell.Y) * Pitch;

	public Vector2I CellOf(Vector2 world)
	{
		Vector2 rel = (world - Origin) / Pitch;
		return new Vector2I(Mathf.RoundToInt(rel.X), Mathf.RoundToInt(rel.Y));
	}

	/// <summary>A cell is floor at its middle - every corridor block is.</summary>
	public bool IsCell(Vector2I cell) => !Wall(CentreOf(cell));

	/// <summary>Whether two side-by-side cells are joined: the wall strip between them is floor at its middle.</summary>
	public bool Open(Vector2I a, Vector2I b) =>
		Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) == 1 && IsCell(a) && IsCell(b) && !Wall((CentreOf(a) + CentreOf(b)) * 0.5f);

	public static readonly Vector2I[] Steps = { new(1, 0), new(0, 1), new(-1, 0), new(0, -1) };

	/// <summary>Walking steps from <paramref name="from"/> to every cell reachable inside the box, over open passages.</summary>
	public Dictionary<Vector2I, int> Distances(Vector2I from, Vector2I min, Vector2I max)
	{
		var dist = new Dictionary<Vector2I, int> { [from] = 0 };
		var queue = new Queue<Vector2I>();
		queue.Enqueue(from);
		while (queue.Count > 0)
		{
			Vector2I c = queue.Dequeue();
			foreach (Vector2I step in Steps)
			{
				Vector2I n = c + step;
				if (n.X < min.X || n.Y < min.Y || n.X > max.X || n.Y > max.Y || dist.ContainsKey(n) || !Open(c, n))
					continue;
				dist[n] = dist[c] + 1;
				queue.Enqueue(n);
			}
		}
		return dist;
	}

	/// <summary>A shortest walk of cells from <paramref name="from"/> to <paramref name="to"/> inside the box, or null.</summary>
	public Vector2I[] PathBetween(Vector2I from, Vector2I to, Vector2I min, Vector2I max)
	{
		Dictionary<Vector2I, int> dist = Distances(to, min, max);
		if (!dist.ContainsKey(from))
			return null;
		var path = new List<Vector2I> { from };
		Vector2I c = from;
		while (c != to)
		{
			Vector2I best = c;
			foreach (Vector2I step in Steps)
			{
				Vector2I n = c + step;
				if (dist.TryGetValue(n, out int d) && d == dist[c] - 1 && Open(c, n))
				{
					best = n;
					break;
				}
			}
			if (best == c)
				return null;
			c = best;
			path.Add(c);
		}
		return path.ToArray();
	}
}

/// <summary>One lantern-bearer's round: a loop of cells walked centre to centre.</summary>
public sealed class GaolRound
{
	/// <summary>A bearer's walk on its round, px a second: a measured pace, well under his 220.</summary>
	public const float Speed = 80f;

	/// <summary>How far ahead along its round a bearer's lantern looks: into a corner half a second before its feet turn it.</summary>
	public const float LookAhead = 40f;

	public GaolRound(Vector2I[] cells, GaolGround ground, int direction)
	{
		Cells = cells;
		Pitch = ground.Pitch;
		Corners = new Vector2[cells.Length];
		for (int i = 0; i < cells.Length; i++)
			Corners[i] = ground.CentreOf(cells[i]);
		Direction = direction < 0 ? -1 : 1;
	}

	/// <summary>The loop: each cell beside the next, the last beside the first.</summary>
	public Vector2I[] Cells { get; }

	/// <summary>The cells' centres, in world coordinates.</summary>
	public Vector2[] Corners { get; }

	public float Pitch { get; }

	/// <summary>+1 walks the cells in their order, -1 against it.</summary>
	public int Direction { get; }

	/// <summary>Where its bearer stands on the layout's first frame, as arc length along the cells' order.</summary>
	public float StartArc { get; set; }

	public float Length => Cells.Length * Pitch;

	/// <summary>The point on the loop <paramref name="arc"/> px along the cells' order, wrapped.</summary>
	public Vector2 PointAt(float arc)
	{
		float s = Mathf.PosMod(arc, Length);
		int i = Math.Min((int)(s / Pitch), Cells.Length - 1);
		float f = (s - i * Pitch) / Pitch;
		return Corners[i].Lerp(Corners[(i + 1) % Cells.Length], f);
	}

	/// <summary>The way the lantern points at <paramref name="arc"/>: at the point <see cref="LookAhead"/> further along the walk.</summary>
	public Vector2 FacingAt(float arc)
	{
		Vector2 d = PointAt(arc + Direction * LookAhead) - PointAt(arc);
		return d.LengthSquared() > 1e-6f ? d.Normalized() : Vector2.Right;
	}

	/// <summary>The arc of cell <paramref name="index"/>'s centre.</summary>
	public float ArcOfCell(int index) => index * Pitch;

	public int IndexOf(Vector2I cell) => Array.IndexOf(Cells, cell);
}

/// <summary>
/// The Gaoler's key's seed (.ai/unlock-puzzles.md 3.8): the Warden's post and the rounds between it and the cell, laid once
/// from the run's seed and the maze, and kept only if a walker carrying the key can always slip through.
/// </summary>
public sealed class GaolLayout
{
	/// <summary>The post is this many cells' walk from the cell, at least...</summary>
	public const int PostMinSteps = 4;

	/// <summary>...and at most: the old carry crossed about 1,500 px, and four cells is 1,536.</summary>
	public const int PostMaxSteps = 6;

	/// <summary>The most bearers walking rounds.</summary>
	public const int MaxRounds = 3;

	/// <summary>Rounds are loops of four to this many cells (a lattice loop is always even).</summary>
	public const int MaxLoopCells = 12;

	/// <summary>How far round the cell the post is looked for, in cells.</summary>
	public const int SearchRadius = 8;

	/// <summary>Cells round the way home that the rounds, the proof and the probe's walk may use.</summary>
	public const int RegionMargin = 2;

	/// <summary>
	/// The share of start times at which walking the shortest way home at once, without waiting, is caught: under this the
	/// rounds do not sweep the way home and the layout is rolled again. Measured over <see cref="BiteStarts"/> starts.
	/// </summary>
	public const float MinCaughtShare = 0.5f;

	public const int BiteStarts = 24;

	/// <summary>
	/// Phase sets the gap proof is run at when laying the gaol: the layout's own, then ones a turned bearer could come back
	/// in. The validator runs ten more on every gaol it lays (RegressionChecks.ValidateGaolLayouts).
	/// </summary>
	public const int PhaseSets = 4;

	public const int MaxAttempts = 8;

	/// <summary>How long a walker with the key may take, seconds, from the post to the cell turning in its lock.</summary>
	public const float SlipHorizonSeconds = 60f;

	public GaolGround Ground { get; private set; }
	public Vector2I Home { get; private set; }
	public Vector2I Post { get; private set; }

	/// <summary>A shortest walk of cells from the post to the cell: the obvious way home.</summary>
	public Vector2I[] Route { get; private set; }

	public List<GaolRound> Rounds { get; private set; } = new();

	/// <summary>The box of cells the rounds, the proof and the probe's walk use.</summary>
	public Vector2I RegionMin { get; private set; }
	public Vector2I RegionMax { get; private set; }

	/// <summary>The walk graph over the region, built once with the layout.</summary>
	public GaolWalk Walk { get; private set; }

	/// <summary>Each round's sweep, in <see cref="Rounds"/>' order (the default lantern, inflated by the planner's margin).</summary>
	public List<GaolSweep> Sweeps { get; private set; } = new();

	// --- what the generator measured, for the validator and the log -----------------------------------------
	public int Attempts { get; private set; }
	public int LoopsFound { get; private set; }
	public float CaughtShare { get; private set; }
	/// <summary>The slowest slip home over the phase sets, seconds, from the post with the key in hand.</summary>
	public float SlowestSlipSeconds { get; private set; }
	/// <summary>True when no roll met every rule and the layout was the best kept.</summary>
	public bool Fallback { get; private set; }

	/// <summary>Milliseconds the generator spent: finding loops, sweeping them, searching for gaps, measuring the bite.</summary>
	public (double Loops, double Sweeps, double Search, double Bite) Cost { get; private set; }

	public Vector2 PostPosition => Ground.CentreOf(Post);
	public Vector2 HomePosition => Ground.CentreOf(Home);

	/// <summary>
	/// Lays the gaol out round the cell at <paramref name="home"/>: the post on the side of the cell <paramref name="away"/>
	/// points to (beyond the cell from where the player comes) when there is one there, and the rounds between.
	/// Deterministic in its arguments. Returns null only if no cell four to six steps away exists, which the maze never does.
	/// </summary>
	public static GaolLayout Generate(GaolGround ground, Vector2I home, Vector2 away, ulong seed)
	{
		var rng = new LevelGenerator.Rng(LevelGenerator.Hash(seed, home.X, home.Y, 0x6A01));
		Vector2I searchMin = home - new Vector2I(SearchRadius, SearchRadius);
		Vector2I searchMax = home + new Vector2I(SearchRadius, SearchRadius);
		ground.Bake(searchMin, searchMax);
		Dictionary<Vector2I, int> fromHome = ground.Distances(home, searchMin, searchMax);

		// The post: on the far side of the cell from where he comes, when the maze has a cell there.
		Vector2 awayDir = away.LengthSquared() > 1e-6f ? away.Normalized() : Vector2.Right;
		var posts = new List<(Vector2I Cell, float Score)>();
		foreach (var (cell, steps) in fromHome)
		{
			if (steps < PostMinSteps || steps > PostMaxSteps)
				continue;
			Vector2 dir = new Vector2(cell.X - home.X, cell.Y - home.Y).Normalized();
			posts.Add((cell, dir.Dot(awayDir)));
		}
		if (posts.Count == 0)
			return null;
		posts.Sort((a, b) => b.Score.CompareTo(a.Score) != 0 ? b.Score.CompareTo(a.Score) : (a.Cell.X * 1000 + a.Cell.Y).CompareTo(b.Cell.X * 1000 + b.Cell.Y));
		int top = Math.Max(1, Math.Min(posts.Count, Math.Max(3, posts.Count / 4)));
		Vector2I post = posts[rng.Next(top)].Cell;

		var layout = new GaolLayout { Ground = ground, Home = home, Post = post };
		layout.Route = ground.PathBetween(post, home, searchMin, searchMax);
		Vector2I lo = new(Math.Min(post.X, home.X), Math.Min(post.Y, home.Y));
		Vector2I hi = new(Math.Max(post.X, home.X), Math.Max(post.Y, home.Y));
		foreach (Vector2I c in layout.Route)
		{
			lo = new Vector2I(Math.Min(lo.X, c.X), Math.Min(lo.Y, c.Y));
			hi = new Vector2I(Math.Max(hi.X, c.X), Math.Max(hi.Y, c.Y));
		}
		layout.RegionMin = lo - new Vector2I(RegionMargin, RegionMargin);
		layout.RegionMax = hi + new Vector2I(RegionMargin, RegionMargin);
		layout.Walk = GaolWalk.Build(ground, layout.RegionMin, layout.RegionMax);

		// Rounds are walked across the way between or beside it - never through the cell (its door is where the lock is
		// turned) or the post (where the Warden stands its ground).
		var forbidden = new HashSet<Vector2I> { home, post };
		var between = new HashSet<Vector2I>();
		foreach (Vector2I c in layout.Route)
			if (!forbidden.Contains(c))
				between.Add(c);
		// Beside the way: a cell joined to one of the way's, whose lantern looks down into it at the junction.
		var beside = new HashSet<Vector2I>(between);
		foreach (Vector2I c in between)
			foreach (Vector2I step in GaolGround.Steps)
				if (!forbidden.Contains(c + step) && ground.Open(c, c + step))
					beside.Add(c + step);

		long clock = System.Diagnostics.Stopwatch.GetTimestamp();
		double loopsMs = 0, sweepMs = 0, searchMs = 0, biteMs = 0;
		double Lap()
		{
			long now = System.Diagnostics.Stopwatch.GetTimestamp();
			double ms = (now - clock) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
			clock = now;
			return ms;
		}

		List<Vector2I[]> loops = ShortLoops(ground, layout.RegionMin, layout.RegionMax, forbidden);
		layout.LoopsFound = loops.Count;
		var crossing = loops.FindAll(l => Array.Exists(l, beside.Contains));
		loopsMs += Lap();

		// The proof's walker waits a step off the key (as a player stands beside it for the dark) and steps onto it.
		int keyPoint = layout.Walk.PointOfCell(post);
		IReadOnlyList<int> nextToKey = keyPoint >= 0 ? layout.Walk.NeighboursOf(keyPoint) : Array.Empty<int>();
		int startPoint = nextToKey.Count > 0 ? nextToKey[0] : keyPoint;
		int homePoint = layout.Walk.PointOfCell(home);
		int horizon = GaolSlip.StepsFor(SlipHorizonSeconds);

		// A loop walked one way always sweeps the same: built once however many rolls pick it.
		var planned = new Dictionary<(Vector2I[], int), GaolSweep>();
		var actual = new Dictionary<(Vector2I[], int), GaolSweep>();
		GaolSweep SweepOf(Dictionary<(Vector2I[], int), GaolSweep> cache, GaolRound r, Func<LanternCone> cone)
		{
			if (!cache.TryGetValue((r.Cells, r.Direction), out GaolSweep s))
				cache[(r.Cells, r.Direction)] = s = new GaolSweep(r, layout.Walk, ground, cone());
			return s;
		}

		// Roll: rounds across the way, which way each is walked and where each bearer stands. Kept: the first roll that
		// leaves a gap at every phase set AND bites a straight walk home. Failing that over MaxAttempts rolls, the roll that
		// left a gap and bit hardest; failing even a gap, one round fewer, and so on.
		GaolLayout best = null;
		float bestScore = 0f;
		int attempts = 0;
		for (int wanted = Math.Min(MaxRounds, crossing.Count); wanted >= 1 && best == null; wanted--)
		{
			for (int attempt = 0; attempt < MaxAttempts; attempt++)
			{
				attempts++;
				List<GaolRound> rounds = PickRounds(crossing, between, ground, wanted, ref rng);
				if (rounds.Count == 0)
					break;
				var sweeps = new List<GaolSweep>();
				foreach (GaolRound r in rounds)
				{
					GaolSweep s = SweepOf(planned, r, GaolSlip.PlanningCone);
					r.StartArc = rng.Next(s.Positions) * (r.Length / s.Positions);
					sweeps.Add(s);
				}
				sweepMs += Lap();
				bool gaps = true;
				float slowest = 0f;
				for (int set = 0; set < PhaseSets && gaps; set++)
				{
					int[] starts = new int[rounds.Count];
					for (int i = 0; i < rounds.Count; i++)
						starts[i] = set == 0 ? sweeps[i].IndexOfArc(rounds[i].StartArc) : rng.Next(sweeps[i].Positions);
					GaolSlip slip = GaolSlip.Search(layout.Walk, sweeps, starts, startPoint, keyPoint, homePoint, horizon, keepPath: false,
						roam: false);
					gaps = slip.Found;
					slowest = Mathf.Max(slowest, slip.Seconds);
				}
				searchMs += Lap();
				if (!gaps)
					continue;
				// The bite is read with the lanterns as the bearers carry them, not the planner's cautious ones.
				var trueSweeps = rounds.ConvertAll(r => SweepOf(actual, r, () => new LanternCone()));
				sweepMs += Lap();
				float share = StraightCaughtShare(layout.Walk, trueSweeps, layout.Route, seed + (ulong)attempts);
				biteMs += Lap();
				// A roll is as good as it bites, and a second bearer is worth a good deal of bite: two rounds to read is the
				// puzzle, one is a timing.
				float score = share + 0.25f * (rounds.Count - 1);
				if (best == null || score > bestScore)
				{
					bestScore = score;
					best = new GaolLayout
					{
						Ground = ground, Home = home, Post = post, Route = layout.Route, RegionMin = layout.RegionMin,
						RegionMax = layout.RegionMax, Walk = layout.Walk, Rounds = rounds, Sweeps = sweeps,
						LoopsFound = layout.LoopsFound, CaughtShare = share, SlowestSlipSeconds = slowest,
					};
				}
				// Two rounds that bite: done. One that bites, three rolls in: the maze has no second loop clear of it here, and
				// more rolls only cost time.
				if (share >= MinCaughtShare && (rounds.Count >= Math.Min(2, wanted) || attempt >= 2))
					break;
			}
		}
		if (best == null)
		{
			// Nothing crosses the way, or nothing leaves a gap: a cell with no rounds. The validator says if this ever happens.
			layout.Attempts = attempts;
			layout.Fallback = true;
			layout.Cost = (loopsMs, sweepMs, searchMs, biteMs);
			return layout;
		}
		best.Attempts = attempts;
		best.Fallback = best.CaughtShare < MinCaughtShare;
		best.Cost = (loopsMs, sweepMs, searchMs, biteMs);
		return best;
	}

	/// <summary>
	/// THE WAY HE KNOWS: which points of <paramref name="walk"/> a player who watches the rounds walks - the shortest way home
	/// (<see cref="Route"/>: its cells and the passages between them, <paramref name="onRoute"/>) and the side passages off it:
	/// each cell beside a route cell, with the one passage joining it to the route, so a side passage is a dead end to stand
	/// in while a light goes by, never a way round. Points off the lattice (ones a plan attached: where he stands, where the
	/// key lies) are always allowed. A point on a passage belongs to the two cells it joins - the lattice decides it, as the
	/// walls are made from it. The validator proves a slip along this ground alone; the probe plans on it.
	/// </summary>
	public bool[] WayHeKnows(GaolWalk walk, out bool[] onRoute) => WayHeKnows(walk, Route, out onRoute);

	/// <summary>The same for any walk of cells: the probe's, from where he stands to where the key lies and on home.</summary>
	public bool[] WayHeKnows(GaolWalk walk, IReadOnlyList<Vector2I> cells, out bool[] onRoute)
	{
		var route = new HashSet<Vector2I>(cells);
		var routeSteps = new HashSet<(Vector2I, Vector2I)>();
		for (int i = 1; i < cells.Count; i++)
		{
			routeSteps.Add((cells[i - 1], cells[i]));
			routeSteps.Add((cells[i], cells[i - 1]));
		}
		var allowed = new bool[walk.Count];
		onRoute = new bool[walk.Count];
		for (int p = 0; p < walk.Count; p++)
		{
			Vector2 rel = (walk.Points[p] - Ground.Origin) / Ground.Pitch;
			var c = new Vector2I(Mathf.RoundToInt(rel.X), Mathf.RoundToInt(rel.Y));
			Vector2 d = rel - new Vector2(c.X, c.Y);
			bool centre = Mathf.Abs(d.X) < 0.01f && Mathf.Abs(d.Y) < 0.01f;
			if (!centre && Mathf.Abs(d.X) >= 0.01f && Mathf.Abs(d.Y) >= 0.01f)
			{
				allowed[p] = true;
				continue;
			}
			if (centre)
			{
				onRoute[p] = route.Contains(c);
				allowed[p] = onRoute[p] || Array.Exists(GaolGround.Steps, s => route.Contains(c + s));
				continue;
			}
			Vector2I other = c + (Mathf.Abs(d.X) >= 0.01f ? new Vector2I(Math.Sign(d.X), 0) : new Vector2I(0, Math.Sign(d.Y)));
			onRoute[p] = routeSteps.Contains((c, other));
			allowed[p] = onRoute[p] || route.Contains(c) || route.Contains(other);
		}
		return allowed;
	}

	// Every loop of four to MaxLoopCells cells inside the box that keeps off the forbidden cells: the shortest loop through
	// each open passage (a breadth-first walk from one end to the other without it), each found once.
	private static List<Vector2I[]> ShortLoops(GaolGround ground, Vector2I min, Vector2I max, HashSet<Vector2I> forbidden)
	{
		var loops = new List<Vector2I[]>();
		var seen = new HashSet<string>();
		for (int x = min.X; x <= max.X; x++)
		{
			for (int y = min.Y; y <= max.Y; y++)
			{
				var a = new Vector2I(x, y);
				if (forbidden.Contains(a))
					continue;
				foreach (Vector2I step in new[] { new Vector2I(1, 0), new Vector2I(0, 1) })
				{
					Vector2I b = a + step;
					if (b.X > max.X || b.Y > max.Y || forbidden.Contains(b) || !ground.Open(a, b))
						continue;
					Vector2I[] path = PathAvoiding(ground, b, a, a, b, min, max, forbidden, MaxLoopCells - 1);
					if (path == null || path.Length < 4)
						continue;
					var key = new List<string>();
					foreach (Vector2I c in path)
						key.Add($"{c.X},{c.Y}");
					key.Sort(StringComparer.Ordinal);
					if (seen.Add(string.Join(";", key)))
						loops.Add(path);
				}
			}
		}
		return loops;
	}

	// A shortest walk from `from` to `to` that never uses the passage (skipA, skipB) and never enters a forbidden cell, of at
	// most maxSteps steps; null if none. The loop is that walk closed by the passage.
	private static Vector2I[] PathAvoiding(GaolGround ground, Vector2I from, Vector2I to, Vector2I skipA, Vector2I skipB,
		Vector2I min, Vector2I max, HashSet<Vector2I> forbidden, int maxSteps)
	{
		var parent = new Dictionary<Vector2I, Vector2I> { [from] = from };
		var depth = new Dictionary<Vector2I, int> { [from] = 0 };
		var queue = new Queue<Vector2I>();
		queue.Enqueue(from);
		while (queue.Count > 0)
		{
			Vector2I c = queue.Dequeue();
			if (c == to)
				break;
			if (depth[c] >= maxSteps)
				continue;
			foreach (Vector2I step in GaolGround.Steps)
			{
				Vector2I n = c + step;
				if (n.X < min.X || n.Y < min.Y || n.X > max.X || n.Y > max.Y || parent.ContainsKey(n) || forbidden.Contains(n))
					continue;
				if ((c == skipA && n == skipB) || (c == skipB && n == skipA))
					continue;
				if (!ground.Open(c, n))
					continue;
				parent[n] = c;
				depth[n] = depth[c] + 1;
				queue.Enqueue(n);
			}
		}
		if (!parent.ContainsKey(to))
			return null;
		var path = new List<Vector2I>();
		for (Vector2I c = to; ; c = parent[c])
		{
			path.Add(c);
			if (c == from)
				break;
		}
		path.Reverse();
		return path.ToArray();
	}

	// Up to `wanted` loops, no two sharing a cell, each crossing a cell of the way that none picked before it crosses.
	private static List<GaolRound> PickRounds(List<Vector2I[]> crossing, HashSet<Vector2I> between, GaolGround ground, int wanted,
		ref LevelGenerator.Rng rng)
	{
		var order = new List<Vector2I[]>(crossing);
		for (int i = order.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			(order[i], order[j]) = (order[j], order[i]);
		}
		// Shuffled, then the loops walked THROUGH the way before those only beside it, and short ones before long: a short
		// loop across the way sweeps it often and leaves room for another. A stable sort, so a tie keeps the shuffle's order.
		int Tier(Vector2I[] loop) => (Array.Exists(loop, between.Contains) ? 0 : 100) + loop.Length;
		var tiered = new List<(int Tier, int Index, Vector2I[] Loop)>(order.Count);
		for (int i = 0; i < order.Count; i++)
			tiered.Add((Tier(order[i]), i, order[i]));
		tiered.Sort((a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier) : a.Index.CompareTo(b.Index));
		for (int i = 0; i < order.Count; i++)
			order[i] = tiered[i].Loop;
		var used = new HashSet<Vector2I>();
		var covered = new HashSet<Vector2I>();
		var rounds = new List<GaolRound>();
		foreach (bool strict in new[] { true, false })
		{
			foreach (Vector2I[] loop in order)
			{
				if (rounds.Count >= wanted)
					break;
				if (Array.Exists(loop, used.Contains))
					continue;
				if (strict && !Array.Exists(loop, c => between.Contains(c) && !covered.Contains(c)))
					continue;
				foreach (Vector2I c in loop)
				{
					used.Add(c);
					if (between.Contains(c))
						covered.Add(c);
				}
				rounds.Add(new GaolRound(loop, ground, rng.NextFloat() < 0.5f ? -1 : 1));
			}
		}
		return rounds;
	}

	/// <summary>
	/// The share of <see cref="BiteStarts"/> moments, each with every bearer somewhere on its round at random (as a run's
	/// turned bearers leave them), at which a walker who picks the key up at the post and walks the shortest way home at
	/// once - no waiting, no watching - and turns it in the lock is caught. Read with the lanterns the bearers carry.
	/// </summary>
	public static float StraightCaughtShare(GaolWalk walk, List<GaolSweep> sweeps, Vector2I[] route, ulong seed)
	{
		List<int> points = walk.PointsAlong(route);
		if (points == null || sweeps.Count == 0)
			return 0f;
		var rng = new LevelGenerator.Rng(LevelGenerator.Hash(seed, route.Length, sweeps.Count, 0x6A04));
		int caught = 0;
		int[] starts = new int[sweeps.Count];
		for (int k = 0; k < BiteStarts; k++)
		{
			for (int i = 0; i < sweeps.Count; i++)
				starts[i] = rng.Next(sweeps[i].Positions);
			if (GaolSlip.WalkIsCaught(walk, sweeps, starts, points, GaolSlip.DwellSteps))
				caught++;
		}
		return caught / (float)BiteStarts;
	}
}

/// <summary>
/// The walk graph over the gaol's region: every cell centre, and a point every <see cref="Step"/> px along each open passage
/// between two cells. A walker on it keeps to the middle of the corridors, as the bearers do.
/// </summary>
public sealed class GaolWalk
{
	/// <summary>Between two points: a walker on the graph moves one a tick (GaolSlip.Tick), 200 px a second.</summary>
	public const float Step = 24f;

	public readonly List<Vector2> Points = new();
	private readonly List<List<int>> links = new();
	private readonly Dictionary<Vector2I, int> cellPoint = new();
	private readonly HashSet<int> cellPoints = new();
	public GaolGround Ground { get; private set; }

	public int Count => Points.Count;

	/// <summary>A point's neighbours. The list itself (not an interface), so a foreach over it allocates nothing.</summary>
	public List<int> NeighboursOf(int point) => links[point];

	// The points bucketed on a coarse grid, so a lantern asks only the points near it (PointsNear). Built on first use and
	// rebuilt after Attach.
	private const float BucketSize = 96f;
	private Dictionary<Vector2I, List<int>> buckets;

	/// <summary>Adds to <paramref name="into"/> every point within <paramref name="radius"/> of <paramref name="at"/>'s box.</summary>
	public void PointsNear(Vector2 at, float radius, List<int> into)
	{
		if (buckets == null)
		{
			buckets = new Dictionary<Vector2I, List<int>>();
			for (int i = 0; i < Points.Count; i++)
			{
				var key = new Vector2I(Mathf.FloorToInt(Points[i].X / BucketSize), Mathf.FloorToInt(Points[i].Y / BucketSize));
				if (!buckets.TryGetValue(key, out List<int> list))
					buckets[key] = list = new List<int>();
				list.Add(i);
			}
		}
		int x0 = Mathf.FloorToInt((at.X - radius) / BucketSize), x1 = Mathf.FloorToInt((at.X + radius) / BucketSize);
		int y0 = Mathf.FloorToInt((at.Y - radius) / BucketSize), y1 = Mathf.FloorToInt((at.Y + radius) / BucketSize);
		for (int x = x0; x <= x1; x++)
			for (int y = y0; y <= y1; y++)
				if (buckets.TryGetValue(new Vector2I(x, y), out List<int> list))
					into.AddRange(list);
	}

	/// <summary>The point at a cell's centre, or -1 if the cell is not in the graph.</summary>
	public int PointOfCell(Vector2I cell) => cellPoint.TryGetValue(cell, out int i) ? i : -1;

	public static GaolWalk Build(GaolGround ground, Vector2I min, Vector2I max)
	{
		var walk = new GaolWalk { Ground = ground };
		for (int x = min.X; x <= max.X; x++)
			for (int y = min.Y; y <= max.Y; y++)
			{
				var c = new Vector2I(x, y);
				if (!ground.IsCell(c))
					continue;
				int p = walk.Add(ground.CentreOf(c));
				walk.cellPoint[c] = p;
				walk.cellPoints.Add(p);
			}
		int between = Math.Max(1, Mathf.RoundToInt(ground.Pitch / Step));
		foreach (var (cell, index) in new Dictionary<Vector2I, int>(walk.cellPoint))
		{
			foreach (Vector2I step in new[] { new Vector2I(1, 0), new Vector2I(0, 1) })
			{
				Vector2I next = cell + step;
				if (!walk.cellPoint.TryGetValue(next, out int far) || !ground.Open(cell, next))
					continue;
				int prev = index;
				for (int k = 1; k < between; k++)
				{
					int p = walk.Add(ground.CentreOf(cell).Lerp(ground.CentreOf(next), k / (float)between));
					walk.Link(prev, p);
					prev = p;
				}
				walk.Link(prev, far);
			}
		}
		return walk;
	}

	private int Add(Vector2 at)
	{
		Points.Add(at);
		links.Add(new List<int>(4));
		buckets = null;
		return Points.Count - 1;
	}

	private void Link(int a, int b)
	{
		links[a].Add(b);
		links[b].Add(a);
	}

	/// <summary>The nearest point of the graph to a world point, or -1 for an empty graph.</summary>
	public int Nearest(Vector2 at)
	{
		int best = -1;
		float bestD = float.MaxValue;
		for (int i = 0; i < Points.Count; i++)
		{
			float d = Points[i].DistanceSquaredTo(at);
			if (d < bestD)
			{
				bestD = d;
				best = i;
			}
		}
		return best;
	}

	/// <summary>
	/// Joins an off-graph world point (where a key lies, where the probe stands) to the nearest point it can walk to in a
	/// straight line, by points no more than <see cref="Step"/> apart. Returns the new point, or -1 if no graph point within
	/// a corridor's reach is in a straight line from it. Mutates the graph: build a fresh one per plan.
	/// </summary>
	public int Attach(Vector2 at)
	{
		int best = -1;
		float bestD = float.MaxValue;
		for (int i = 0; i < Points.Count; i++)
		{
			float d = Points[i].DistanceSquaredTo(at);
			if (d >= bestD || d > Ground.Pitch * Ground.Pitch || !ClearLine(at, Points[i]))
				continue;
			bestD = d;
			best = i;
		}
		if (best < 0)
			return -1;
		if (bestD < 1f)
			return best;
		int n = Math.Max(1, Mathf.CeilToInt(Mathf.Sqrt(bestD) / Step));
		int first = Add(at);
		int prev = first;
		for (int k = 1; k < n; k++)
		{
			int p = Add(at.Lerp(Points[best], k / (float)n));
			Link(prev, p);
			prev = p;
		}
		Link(prev, best);
		return first;
	}

	private bool ClearLine(Vector2 a, Vector2 b)
	{
		int n = Math.Max(1, Mathf.CeilToInt(a.DistanceTo(b) / 12f));
		for (int k = 0; k <= n; k++)
			if (Ground.Wall(a.Lerp(b, k / (float)n)))
				return false;
		return true;
	}

	/// <summary>The graph's points along a walk of cells, centre to centre, in order; null if a step is not joined.</summary>
	public List<int> PointsAlong(Vector2I[] cells)
	{
		if (cells == null || cells.Length == 0)
			return null;
		var points = new List<int> { PointOfCell(cells[0]) };
		for (int i = 1; i < cells.Length; i++)
		{
			int from = PointOfCell(cells[i - 1]);
			int to = PointOfCell(cells[i]);
			if (from < 0 || to < 0)
				return null;
			// Follow the chain of passage points out of `from` that ends at `to`.
			bool found = false;
			foreach (int start in links[from])
			{
				var chain = new List<int>();
				int prev = from, cur = start;
				while (cur != to && links[cur].Count == 2 && !cellPoints.Contains(cur))
				{
					chain.Add(cur);
					int next = links[cur][0] == prev ? links[cur][1] : links[cur][0];
					prev = cur;
					cur = next;
				}
				if (cur != to)
					continue;
				points.AddRange(chain);
				points.Add(to);
				found = true;
				break;
			}
			if (!found)
				return null;
		}
		return points;
	}
}

/// <summary>What one round lights, at every step of its loop: a bit per walk point, for each of its positions.</summary>
public sealed class GaolSweep
{
	/// <summary>How far a bearer walks in one tick: GaolRound.Speed x GaolSlip.Tick, 9.6 px - forty to a 384 px cell.</summary>
	public const float ArcStep = GaolRound.Speed * GaolSlip.Tick;

	private readonly ulong[][] lit;
	private readonly int words;

	public GaolSweep(GaolRound round, GaolWalk walk, GaolGround ground, LanternCone cone)
	{
		Round = round;
		Positions = Math.Max(1, Mathf.RoundToInt(round.Length / ArcStep));
		words = (walk.Count + 63) / 64;
		lit = new ulong[Positions][];
		float reach2 = cone.Reach * cone.Reach;
		// A cheap first look - behind the lantern, or wide of its spread by more than a degree - before Covers, which is
		// the authority and is asked of everything the first look keeps.
		float cosWide = Mathf.Cos(Mathf.DegToRad(cone.HalfAngleDegrees + 1f));
		float cos2 = cosWide * cosWide;
		var near = new List<int>(256);
		for (int p = 0; p < Positions; p++)
		{
			var bits = new ulong[words];
			float arc = p * round.Length / Positions;
			Vector2 at = round.PointAt(arc);
			Vector2 facing = round.FacingAt(arc);
			cone.Aim(at, facing, ground.WallTest);
			near.Clear();
			walk.PointsNear(at, cone.Reach, near);
			foreach (int i in near)
			{
				Vector2 d = walk.Points[i] - at;
				float dd = d.LengthSquared();
				if (dd > reach2)
					continue;
				if (dd > 1f)
				{
					float along = d.Dot(facing);
					if (along <= 0f || along * along < cos2 * dd)
						continue;
				}
				if (cone.Covers(walk.Points[i]))
					bits[i >> 6] |= 1UL << (i & 63);
			}
			lit[p] = bits;
		}
	}

	public GaolRound Round { get; }

	/// <summary>How many positions the loop is stepped through: one a tick of its bearer's walk.</summary>
	public int Positions { get; }

	/// <summary>The position nearest an arc along the loop.</summary>
	public int IndexOfArc(float arc) => (int)Mathf.PosMod(Mathf.Round(arc / Round.Length * Positions), Positions);

	/// <summary>The position <paramref name="ticks"/> after <paramref name="start"/>, walking the round's way.</summary>
	public int After(int start, int ticks) => (int)(((start + (long)Round.Direction * ticks) % Positions + Positions) % Positions);

	public bool Lights(int position, int point) => (lit[position][point >> 6] & (1UL << (point & 63))) != 0;

	/// <summary>ORs what this round lights at <paramref name="position"/> into <paramref name="into"/>.</summary>
	public void OrInto(int position, ulong[] into)
	{
		ulong[] bits = lit[position];
		for (int w = 0; w < bits.Length && w < into.Length; w++)
			into[w] |= bits[w];
	}
}

/// <summary>
/// The gap proof and the probe's plan: a walker on the walk graph, one point a tick, searched forward in time against the
/// rounds' sweeps. Without the key he may go anywhere; he picks it up only where it is dark (in a cone it would be caught
/// out of his hands on the spot); with it he may only ever stand where it is dark; and he is home once he has stood at the
/// cell's door, dark, for as long as the lock takes to turn.
/// </summary>
public sealed class GaolSlip
{
	/// <summary>Seconds a tick: a walker moves one <see cref="GaolWalk.Step"/> (200 px a second), a bearer one ArcStep.</summary>
	public const float Tick = 0.12f;

	/// <summary>Ticks at the door for the key to turn: GaolersKeyEvent.UnlockSeconds (1.2 s).</summary>
	public const int DwellSteps = 10;

	/// <summary>The planner's cone: a little longer and wider than the bearers', so a way it finds keeps a margin of dark.</summary>
	public static LanternCone PlanningCone() => new(LanternCone.DefaultReach + 24f, LanternCone.DefaultHalfAngleDegrees + 4f);

	public static int StepsFor(float seconds) => Mathf.CeilToInt(seconds / Tick);

	public bool Found { get; private set; }

	/// <summary>Ticks from the start to the lock turning (the end of the dwell), when found.</summary>
	public int Ticks { get; private set; }

	public float Seconds => Ticks * Tick;

	/// <summary>The tick the key was picked up at, when found.</summary>
	public int PickupTick { get; private set; } = -1;

	/// <summary>The point the walker stands on at each tick, from the start to the lock turning, when asked for.</summary>
	public int[] Path { get; private set; }

	/// <summary>
	/// Ticks spent standing still with the key in hand before the door, when a path was kept: the waiting for a light to pass.
	/// The lock's own dwell at the door is not counted.
	/// </summary>
	public int HeldWaits { get; private set; }

	/// <summary>The points the walker stood still on with the key, when a path was kept (the dwell at the door left out).</summary>
	public List<int> WaitPoints { get; } = new();

	/// <summary>
	/// Searches from <paramref name="start"/> at tick 0, each round's bearer at <paramref name="startPositions"/>[i], for the
	/// first tick the walker has the key from <paramref name="key"/> and has stood <see cref="DwellSteps"/> at
	/// <paramref name="home"/> in the dark. A <paramref name="key"/> equal to <paramref name="start"/> means he stands on it.
	/// Without <paramref name="roam"/> the walker without the key only waits where he is and steps onto it from there (the
	/// proof, which starts him beside it); with it he may walk anywhere first (the probe, which starts wherever it stands).
	/// <paramref name="pickupRadius"/>: every point that close to the key takes it up the moment it is stepped on, as
	/// KeyCarry does (the probe passes KeyCarry.PickupRadius); 0 for the key's own point only. <paramref name="allowed"/>,
	/// when given, is the only ground the walker may step onto (the probe's "the way he knows"); null for all of it.
	/// </summary>
	public static GaolSlip Search(GaolWalk walk, IReadOnlyList<GaolSweep> sweeps, int[] startPositions, int start, int key, int home,
		int horizon, bool keepPath, float pickupRadius = 0f, bool roam = true, bool[] allowed = null)
	{
		var result = new GaolSlip();
		int n = walk.Count;
		if (start < 0 || key < 0 || home < 0)
			return result;
		int words = (n + 63) / 64;
		var lit = new ulong[words];
		// Points a free walker cannot stand on without taking the key up: the key's own and every one within the radius.
		var pickup = new bool[n];
		pickup[key] = true;
		if (pickupRadius > 0f)
		{
			var near = new List<int>();
			walk.PointsNear(walk.Points[key], pickupRadius, near);
			foreach (int p in near)
				if (walk.Points[p].DistanceTo(walk.Points[key]) <= pickupRadius)
					pickup[p] = true;
		}

		var free = new bool[n];
		var held = new bool[n];
		var freeNext = new bool[n];
		var heldNext = new bool[n];
		int[][] freeFrom = keepPath ? new int[horizon + 1][] : null;
		int[][] heldFrom = keepPath ? new int[horizon + 1][] : null;
		int streak = 0;

		for (int t = 0; t <= horizon; t++)
		{
			Array.Clear(lit);
			for (int i = 0; i < sweeps.Count; i++)
				sweeps[i].OrInto(sweeps[i].After(startPositions[i], t), lit);
			bool Dark(int p) => (lit[p >> 6] & (1UL << (p & 63))) == 0;

			Array.Clear(freeNext);
			Array.Clear(heldNext);
			int[] ff = keepPath ? NewFrom(n) : null;
			int[] hf = keepPath ? NewFrom(n) : null;
			if (t == 0)
			{
				if (pickup[start])
				{
					if (Dark(start))
					{
						heldNext[start] = true;
						if (keepPath) hf[start] = -2;
					}
				}
				else
				{
					freeNext[start] = true;
					if (keepPath) ff[start] = start;
				}
			}
			else
			{
				for (int p = 0; p < n; p++)
				{
					if (free[p])
					{
						Reach(p, p);
						foreach (int q in walk.NeighboursOf(p))
							if (roam || pickup[q])
								Reach(p, q);
					}
					if (held[p])
					{
						HoldTo(p, p);
						foreach (int q in walk.NeighboursOf(p))
							HoldTo(p, q);
					}
				}
			}

			// A free walker stepping onto the key picks it up - in the dark, or it would be caught out of his hand at once.
			void Reach(int from, int to)
			{
				if (allowed != null && to != from && !allowed[to])
					return;
				if (pickup[to])
				{
					if (Dark(to) && !heldNext[to])
					{
						heldNext[to] = true;
						if (keepPath) hf[to] = -3 - from;
					}
					return;
				}
				if (freeNext[to])
					return;
				freeNext[to] = true;
				if (keepPath) ff[to] = from;
			}

			void HoldTo(int from, int to)
			{
				if (heldNext[to] || !Dark(to) || (allowed != null && to != from && !allowed[to]))
					return;
				heldNext[to] = true;
				if (keepPath) hf[to] = from;
			}

			(free, freeNext) = (freeNext, free);
			(held, heldNext) = (heldNext, held);
			if (keepPath)
			{
				freeFrom[t] = ff;
				heldFrom[t] = hf;
			}

			streak = held[home] ? streak + 1 : 0;
			if (streak >= DwellSteps)
			{
				result.Found = true;
				result.Ticks = t;
				if (keepPath)
					result.Trace(freeFrom, heldFrom, home, t);
				return result;
			}
		}
		return result;
	}

	private static int[] NewFrom(int n)
	{
		var a = new int[n];
		Array.Fill(a, -1);
		return a;
	}

	// Walks the parents back from the lock turning to the start. A held parent of -2 is "picked up where he stood at
	// tick 0", of -3 - p "stepped onto the key from free point p".
	private void Trace(int[][] freeFrom, int[][] heldFrom, int home, int end)
	{
		var path = new int[end + 1];
		bool holding = true;
		int p = home;
		for (int t = end; t >= 0; t--)
		{
			path[t] = p;
			if (t == 0)
				break;
			if (holding)
			{
				int from = heldFrom[t][p];
				if (from == -2)
				{
					PickupTick = 0;
					break;
				}
				if (from <= -3)
				{
					PickupTick = t;
					holding = false;
					p = -3 - from;
				}
				else
				{
					// Standing still with the key - but not the lock's own dwell at the door, the last ticks at home.
					if (from == p && !(p == home && t > end - DwellSteps + 1))
					{
						HeldWaits++;
						WaitPoints.Add(p);
					}
					p = from;
				}
			}
			else
				p = freeFrom[t][p];
		}
		if (PickupTick < 0)
			PickupTick = 0;
		Path = path;
	}

	/// <summary>
	/// True if a walker going straight along <paramref name="points"/> one a tick from tick 0, then standing
	/// <paramref name="dwell"/> ticks at the last, is ever in the light of <paramref name="sweeps"/>.
	/// </summary>
	public static bool WalkIsCaught(GaolWalk walk, IReadOnlyList<GaolSweep> sweeps, int[] startPositions, List<int> points, int dwell)
	{
		int total = points.Count + dwell;
		for (int t = 0; t < total; t++)
		{
			int p = points[Math.Min(t, points.Count - 1)];
			for (int i = 0; i < sweeps.Count; i++)
				if (sweeps[i].Lights(sweeps[i].After(startPositions[i], t), p))
					return true;
		}
		return false;
	}
}

/// <summary>
/// The key's rule, pure, so the event and the validator step the same one: it lies on the floor until he steps on it, he
/// carries it until a lantern's light falls on his feet, and then it is on the floor where he stood - and it cannot be
/// taken up again until he has stepped off it, or standing in the light on it would pick it up and lose it every frame.
/// </summary>
public sealed class KeyCarry
{
	/// <summary>How close he must come to take it up: within this of where it lies.</summary>
	public const float PickupRadius = 30f;

	/// <summary>How far he must step off a key he lost before it can be taken up again.</summary>
	public const float RearmRadius = 70f;

	public enum Change { None, PickedUp, Dropped }

	public bool Carrying { get; private set; }
	public Vector2 KeyAt { get; private set; }

	/// <summary>False from the moment it is lost until he has stepped off it.</summary>
	public bool Armed { get; private set; } = true;

	public int Drops { get; private set; }
	public int Pickups { get; private set; }

	/// <summary>The key falls here and can be taken up (the Warden's death).</summary>
	public void Lay(Vector2 at)
	{
		KeyAt = at;
		Carrying = false;
		Armed = true;
	}

	/// <summary>Held: posed in his hands (_UiShot --win-event).</summary>
	public void Hold()
	{
		Carrying = true;
		Armed = true;
	}

	/// <summary>
	/// One frame. <paramref name="body"/> is where he stands (the key is picked up from and dropped at it);
	/// <paramref name="caught"/> whether any lantern's light is on his feet this frame. Pick-up first, then the light, so a
	/// key taken up in the light is lost on the same frame. <paramref name="dropAt"/> maps where it falls onto the floor.
	/// </summary>
	public Change Step(Vector2 body, bool caught, Func<Vector2, Vector2> dropAt = null)
	{
		Change change = Change.None;
		if (!Carrying)
		{
			float d = body.DistanceTo(KeyAt);
			if (!Armed && d > RearmRadius)
				Armed = true;
			if (Armed && d <= PickupRadius)
			{
				Carrying = true;
				Pickups++;
				change = Change.PickedUp;
			}
		}
		if (Carrying && caught)
		{
			Carrying = false;
			Armed = false;
			KeyAt = dropAt != null ? dropAt(body) : body;
			Drops++;
			change = Change.Dropped;
		}
		return change;
	}
}
