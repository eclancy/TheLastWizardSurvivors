using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// THE ROT ON HIS WAY (.ai/unlock-puzzles.md 3.4, "choose his path"): where the pools lie between the gibbet's cage and
// the ferry landing, and the proof, on paper, that they always leave a way through.
//
// Paracelsus walks the player's own steps (LedEscort), and the pools hurt only him (GibbetCageEvent), so the route the
// player walks is the route he takes and the puzzle is finding a way through. This class is only where the rot lies -
// a plain class with no node, so the validator lays out hundreds of them on paper (RegressionChecks.ValidateGibbetCage).
//
// THE SHAPE. Three walls of rot across the way, each a chain of merged pools with ONE gap in it and one pool beyond
// the gap, the gaps on alternate sides of the straight line. Every wall's chain covers the straight line, so walking
// straight takes him through all three (the wrong approach), and reaches across it past its neighbours' gaps, so no
// straight line a step to the side threads them either: the way through weaves from gap to gap, and the other way is
// a wide berth round the walls' ends, which is longer. The walls are spaced so that two walls never come within MinGap
// of each other: what is between them is open bog. Six to twelve pools, 120 to 260 px across (3.4's first guesses were
// six to ten; MaxCount says why), from six sizes the art is drawn at (gibbet.py ROT_RADII), so the pool drawn is the
// pool that bites.
//
// NO SQUEEZES. Two pools are either merged - overlapping, one stain - or at least MinGap apart edge to edge, and a slot
// between that is not walkable ground or is filled by a third pool does not count. A 60 px slot between two pools looks
// like a way through on a phone in a crowd and puts him in the rot; there are none.
//
// THE ROUTE (FindRoute) is a flood over a grid laid along the line from the start to the landing, RouteMargin past each
// end and RouteSide to either side. A cell is shut within RouteClearance of a pool's edge or on ground nobody can stand
// on (the placer's IsWalkable: water, walls, fences). Generate keeps rolling until the layout keeps every rule above and
// the flood gets through; failing that it takes pools away, the largest first, until it does. When the ground alone
// shuts the box (a lake wider than it), the pools are proved on open ground instead - the ground's own way round is the
// player's to find, as it was before there were pools - and RouteOnGround says so.
public sealed class RotPoolLayout
{
	/// <summary>A pool's radius by its size, in world px: tools/art/gibbet.py ROT_RADII x2. Change one, change both.</summary>
	public static readonly float[] Radii = { 60f, 74f, 88f, 102f, 116f, 130f };

	/// <summary>The art: square cells this many art px (x2 in the world), ArtShapes rows per size, ArtFrames across.</summary>
	public const int ArtCell = 132;
	public const int ArtShapes = 2;
	public const int ArtFrames = 4;

	/// <summary>
	/// A pool bites his feet within this share of its radius: the art's bank never comes inside it (gibbet.py BITE_SHARE),
	/// so his feet are in sludge the player can see before it takes anything.
	/// </summary>
	public const float BiteShare = 0.86f;

	/// <summary>
	/// What a layout holds, before any pool that would lie on water is left out. 3.4's first guess was six to ten; walls
	/// that weave (see the header) need eight to twelve on the shortest walks, where the pools are small.
	/// </summary>
	public const int MinCount = 6;
	public const int MaxCount = 12;
	public const int Walls = 3;
	/// <summary>The most pools in one wall's chain (its outer pool is one more).</summary>
	public const int ChainMax = 3;

	/// <summary>Two pools are merged (overlapping) or at least this far apart, edge to edge.</summary>
	public const float MinGap = 120f;

	/// <summary>
	/// Merged pools overlap by this much (the sum of their radii less their distance), so they read as one - and their
	/// bites overlap too, so no neck of rot the player can see lets him through: two of the largest bite within
	/// 2 x 130 x (1 - BiteShare) = 36 px of where their sludge meets.
	/// </summary>
	public const float MergeMin = 40f;
	public const float MergeMax = 60f;

	/// <summary>A wall's gap, edge to edge, and how far off the straight line its near edge is.</summary>
	public const float GapMin = 140f;
	public const float GapMax = 170f;
	public const float GapEdgeMin = 40f;
	public const float GapEdgeMax = 90f;

	/// <summary>How far each pool sits off its wall's line, either way: the wall is a ragged row, not a ruled one.</summary>
	public const float AlongJitter = 25f;

	/// <summary>
	/// No pool's edge comes nearer the way's start than this, or the landing's foot than EndClear. The gibbet starts the
	/// way at its winch, 92 px from the cage and 110 from its door, so 280 keeps the rot 180 px off the cage (its site's
	/// footprint) and 170 off the door he steps out of.
	/// </summary>
	public const float StartClear = 280f;
	public const float EndClear = 220f;

	/// <summary>The route keeps this far off every pool's edge: a way through is at least twice this wide.</summary>
	public const float RouteClearance = 50f;

	public const float RouteCell = 24f;
	public const float RouteMargin = 240f;
	public const float RouteSide = 720f;

	/// <summary>Layouts rolled before Generate starts taking pools away.</summary>
	public const int MaxAttempts = 12;

	public readonly struct Pool
	{
		public Pool(Vector2 centre, int size, int shape, bool flip)
		{
			Centre = centre;
			Size = size;
			Shape = shape;
			Flip = flip;
		}

		public Vector2 Centre { get; }
		/// <summary>Index into <see cref="Radii"/>.</summary>
		public int Size { get; }
		public int Shape { get; }
		public bool Flip { get; }
		public float Radius => Radii[Size];
		public float BiteRadius => Radius * BiteShare;
		/// <summary>The art's row in rot-pools.png.</summary>
		public int ArtRow => Size * ArtShapes + Shape;

		public bool Bites(Vector2 feet) => feet.DistanceSquaredTo(Centre) < BiteRadius * BiteRadius;
	}

	/// <summary>A wall's gap: its middle, its width edge to edge, and the direction across it (pool to pool).</summary>
	public readonly struct Opening
	{
		public Opening(Vector2 centre, float width, Vector2 across)
		{
			Centre = centre;
			Width = width;
			Across = across;
		}

		public Vector2 Centre { get; }
		public float Width { get; }
		public Vector2 Across { get; }
	}

	private readonly Pool[] pools;
	private readonly Opening[] openings;

	private RotPoolLayout(Pool[] pools, Opening[] openings, Vector2[] route, bool routeOnGround, int attempts, bool trimmed)
	{
		this.pools = pools;
		this.openings = openings;
		Route = route;
		RouteOnGround = routeOnGround;
		Attempts = attempts;
		Trimmed = trimmed;
	}

	public IReadOnlyList<Pool> Pools => pools;
	public IReadOnlyList<Opening> Openings => openings;
	/// <summary>The way through, start to landing, as straight legs (null only if even open ground has none).</summary>
	public Vector2[] Route { get; }
	/// <summary>False when the ground alone shut the route's box and the pools were proved on open ground.</summary>
	public bool RouteOnGround { get; }
	public int Attempts { get; }
	/// <summary>True when no roll kept every rule and pools were taken away to make it.</summary>
	public bool Trimmed { get; }

	/// <summary>The pool whose bite his feet are in, or -1.</summary>
	public int PoolUnder(Vector2 feet)
	{
		for (int i = 0; i < pools.Length; i++)
		{
			if (pools[i].Bites(feet))
				return i;
		}
		return -1;
	}

	/// <summary>True if the straight step from <paramref name="a"/> to <paramref name="b"/> passes through any pool's bite.</summary>
	public bool CrossesBite(Vector2 a, Vector2 b)
	{
		for (int i = 0; i < pools.Length; i++)
		{
			if (DistanceToSegment(pools[i].Centre, a, b) < pools[i].BiteRadius)
				return true;
		}
		return false;
	}

	// --- laying it out ----------------------------------------------------------------------------------------

	/// <summary>
	/// The rot between <paramref name="start"/> and <paramref name="landing"/>, from <paramref name="seed"/>, kept off ground
	/// <paramref name="walkable"/> says nobody can stand on (null: all of it can be stood on).
	/// </summary>
	public static RotPoolLayout Generate(Vector2 start, Vector2 landing, ulong seed, Func<Vector2, bool> walkable)
	{
		var rng = new LevelGenerator.Rng(seed);
		// Whether the ground itself lets anyone from the start to the landing inside the route's box; if not, the pools are
		// proved on open ground (see the header).
		bool groundOpen = walkable == null || FindGrid(Array.Empty<Pool>(), start, landing, walkable, RouteClearance) != null;
		Func<Vector2, bool> ground = groundOpen ? walkable : null;
		List<Pool> last = null;
		List<Opening> lastOpenings = null;
		for (int attempt = 1; attempt <= MaxAttempts; attempt++)
		{
			(List<Pool> rolled, List<Opening> gaps) = Roll(start, landing, ref rng, walkable);
			last = rolled;
			lastOpenings = gaps;
			// Six to twelve are rolled; a pool that would have lain on water is not laid, and the count is what is left.
			if (FirstSqueeze(rolled, walkable) >= 0 || !CutsStraightLine(rolled, start, landing))
				continue;
			Vector2[] route = FindRoute(rolled, start, landing, ground, RouteClearance);
			if (route != null)
				return new RotPoolLayout(rolled.ToArray(), gaps.ToArray(), route, groundOpen, attempt, false);
		}
		// No roll kept every rule: the last one, a pool of every squeeze taken away, then the largest until it is open.
		var kept = new List<Pool>(last);
		for (int squeeze; (squeeze = FirstSqueeze(kept, walkable)) >= 0;)
			kept.RemoveAt(squeeze);
		Vector2[] trimmedRoute;
		while ((trimmedRoute = FindRoute(kept, start, landing, ground, RouteClearance)) == null && kept.Count > 0)
		{
			int largest = 0;
			for (int i = 1; i < kept.Count; i++)
			{
				if (kept[i].Radius > kept[largest].Radius)
					largest = i;
			}
			kept.RemoveAt(largest);
		}
		return new RotPoolLayout(kept.ToArray(), lastOpenings.ToArray(), trimmedRoute, groundOpen, MaxAttempts, true);
	}

	// One roll of the three walls (see the header). Positions are worked in the line's own frame - along from the start,
	// and to either side of it - then turned into the world.
	private static (List<Pool>, List<Opening>) Roll(Vector2 start, Vector2 landing, ref LevelGenerator.Rng rng, Func<Vector2, bool> walkable)
	{
		float length = start.DistanceTo(landing);
		Vector2 u = length > 0.001f ? (landing - start) / length : Vector2.Right;
		Vector2 n = u.Orthogonal();
		// The largest pool the walk has room for: three walls between the two clear ends, each at most two radii and
		// two jitters deep, MinGap apart - 6r <= length - (the clear ends, six jitters and the two gaps between walls).
		float roomFor = (length - StartClear - EndClear - 6f * AlongJitter - 2f * MinGap) / 6f;
		int largest = 0;
		while (largest + 1 < Radii.Length && Radii[largest + 1] <= roomFor)
			largest++;
		float rMax = Radii[largest];
		float first = StartClear + AlongJitter + rMax;
		float lastWall = length - EndClear - AlongJitter - rMax;

		// The gaps first, every wall's: which side of the line it is on (alternating), how far off the line its near edge
		// is, and how wide it is. A chain is then grown until it covers its neighbours' gaps (below), so they come first.
		var side = new float[Walls];
		var edge = new float[Walls];
		var width = new float[Walls];
		side[0] = rng.NextFloat() < 0.5f ? -1f : 1f;
		for (int g = 0; g < Walls; g++)
		{
			if (g > 0)
				side[g] = -side[g - 1];
			edge[g] = Mathf.Lerp(GapEdgeMin, GapEdgeMax, rng.NextFloat());
			width[g] = Mathf.Lerp(GapMin, GapMax, rng.NextFloat());
		}
		// Chain pools are the larger half of the sizes the walk has room for: a wall is as few pools as will reach.
		int chainSmallest = largest / 2;

		var pools = new List<Pool>(MaxCount);
		var gaps = new List<Opening>(Walls);
		for (int g = 0; g < Walls; g++)
		{
			float along = Mathf.Lerp(first, lastWall, g / (float)(Walls - 1));
			// How far across the line the chain must reach, on the side away from its gap: past the far edge of every
			// neighbour's gap (on that side, since they alternate) and a way's width beyond it. Short of that, one
			// straight line a step to the side of the straight one would slip through this wall's end and the next
			// wall's gap, and on through the wall after - the first build's walls did, and most of the way through them
			// was two legs: no weave at all.
			float reach = 0f;
			for (int k = g - 1; k <= g + 1; k += 2)
			{
				if (k >= 0 && k < Walls)
					reach = Mathf.Max(reach, edge[k] + width[k] + 2f * RouteClearance + 20f);
			}
			// The chain, from the gap's near edge across the line: each pool merged with the last by MergeMin to MergeMax
			// measured centre to centre, until its far edge is past `reach`, or ChainMax pools.
			var size = new List<int>(ChainMax);
			var jitter = new List<float>(ChainMax);
			var across = new List<float>(ChainMax);
			do
			{
				int i = size.Count;
				size.Add(chainSmallest + rng.Next(largest - chainSmallest + 1));
				jitter.Add((rng.NextFloat() * 2f - 1f) * AlongJitter);
				if (i == 0)
				{
					across.Add(edge[g] - Radii[size[0]]);
					continue;
				}
				float d = Radii[size[i - 1]] + Radii[size[i]] - Mathf.Lerp(MergeMin, MergeMax, rng.NextFloat());
				float dj = jitter[i] - jitter[i - 1];
				across.Add(across[i - 1] - Mathf.Sqrt(Mathf.Max(0f, d * d - dj * dj)));
			}
			while (size.Count < ChainMax && across[^1] - Radii[size[^1]] > -reach);
			for (int i = 0; i < size.Count; i++)
				pools.Add(MakePool(start + u * (along + jitter[i]) + n * (side[g] * across[i]), size[i], ref rng));
			// The pool on the far side of the gap, which makes it a gap and not open ground: walking round the wall's
			// end on that side is a long way out.
			int outerSize = chainSmallest + rng.Next(largest - chainSmallest + 1);
			float outerJitter = (rng.NextFloat() * 2f - 1f) * AlongJitter;
			float outerLateral = edge[g] + width[g] + Radii[outerSize];
			pools.Add(MakePool(start + u * (along + outerJitter) + n * (side[g] * outerLateral), outerSize, ref rng));
			Vector2 nearCentre = start + u * (along + jitter[0]) + n * (side[g] * across[0]);
			Vector2 outerCentre = start + u * (along + outerJitter) + n * (side[g] * outerLateral);
			gaps.Add(new Opening(start + u * (along + (jitter[0] + outerJitter) * 0.5f) + n * (side[g] * (edge[g] + width[g] * 0.5f)),
				width[g], (outerCentre - nearCentre).Normalized()));
		}
		// The rot never lies on water: a pool whose middle or any of eight points round it is not ground is not laid.
		if (walkable != null)
			pools.RemoveAll(p => !OnGround(p, walkable));
		return (pools, gaps);
	}

	private static Pool MakePool(Vector2 centre, int size, ref LevelGenerator.Rng rng) =>
		new(centre, size, rng.Next(ArtShapes), rng.NextFloat() < 0.5f);

	private static bool OnGround(Pool pool, Func<Vector2, bool> walkable)
	{
		if (!walkable(pool.Centre))
			return false;
		for (int k = 0; k < 8; k++)
		{
			if (!walkable(pool.Centre + Vector2.FromAngle(k * Mathf.Tau / 8f) * pool.Radius * 0.7f))
				return false;
		}
		return true;
	}

	/// <summary>
	/// The index of a pool in a squeeze - two pools neither merged nor MinGap apart, with walkable ground between them
	/// that no third pool fills - or -1 if there is none. Of the two, the smaller.
	/// </summary>
	public static int FirstSqueeze(IReadOnlyList<Pool> pools, Func<Vector2, bool> walkable)
	{
		for (int i = 0; i < pools.Count; i++)
		{
			for (int j = i + 1; j < pools.Count; j++)
			{
				float d = pools[i].Centre.DistanceTo(pools[j].Centre);
				float gap = d - pools[i].Radius - pools[j].Radius;
				if (gap <= -MergeMin + 0.5f || gap >= MinGap)
					continue;
				// The middle of the slot between their edges.
				Vector2 dir = d > 0.001f ? (pools[j].Centre - pools[i].Centre) / d : Vector2.Right;
				Vector2 slot = pools[i].Centre + dir * (pools[i].Radius + gap * 0.5f);
				if (walkable != null && !walkable(slot))
					continue;
				bool filled = false;
				for (int k = 0; k < pools.Count && !filled; k++)
					filled = k != i && k != j && slot.DistanceTo(pools[k].Centre) < pools[k].Radius;
				if (!filled)
					return pools[i].Radius <= pools[j].Radius ? i : j;
			}
		}
		return -1;
	}

	/// <summary>True if the straight line from the start to the landing passes through at least one pool's bite.</summary>
	public static bool CutsStraightLine(IReadOnlyList<Pool> pools, Vector2 start, Vector2 landing)
	{
		for (int i = 0; i < pools.Count; i++)
		{
			if (DistanceToSegment(pools[i].Centre, start, landing) < pools[i].BiteRadius)
				return true;
		}
		return false;
	}

	// --- the route --------------------------------------------------------------------------------------------

	/// <summary>
	/// A way from <paramref name="from"/> to <paramref name="to"/> that keeps <paramref name="clearance"/> off every pool's
	/// edge and stays on ground <paramref name="walkable"/> allows (null: anywhere), as straight legs; null if there is
	/// none in the box round the line between them. A flood over the line's grid (see the header), then pulled straight
	/// wherever a leg can skip corners and still keep its distance.
	/// </summary>
	public static Vector2[] FindRoute(IReadOnlyList<Pool> pools, Vector2 from, Vector2 to, Func<Vector2, bool> walkable, float clearance)
	{
		List<Vector2> cells = FindGrid(pools, from, to, walkable, clearance);
		if (cells == null)
			return null;
		cells[0] = from;
		cells[^1] = to;
		// Pull it straight: from each corner, the furthest point after it that a straight leg reaches keeping a little
		// less than the clearance (the grid's own corners are a cell inside it).
		float keep = clearance - RouteCell * 0.5f;
		var legs = new List<Vector2> { cells[0] };
		int at = 0;
		while (at < cells.Count - 1)
		{
			int reach = at + 1;
			while (reach + 1 < cells.Count && LegClear(pools, cells[at], cells[reach + 1], walkable, keep))
				reach++;
			legs.Add(cells[reach]);
			at = reach;
		}
		return legs.ToArray();
	}

	private static bool LegClear(IReadOnlyList<Pool> pools, Vector2 a, Vector2 b, Func<Vector2, bool> walkable, float keep)
	{
		for (int i = 0; i < pools.Count; i++)
		{
			if (DistanceToSegment(pools[i].Centre, a, b) < pools[i].Radius + keep)
				return false;
		}
		if (walkable == null)
			return true;
		// The ground every 8 px along it, and a body's half-width either side: a straightened leg never shaves the corner
		// of a lake the flood's cells went round (two of 3,000 swept layouts did, at 12 px and on the line alone).
		float length = a.DistanceTo(b);
		Vector2 aside = length > 0.001f ? (b - a).Orthogonal() / length * LegHalfWidth : Vector2.Zero;
		int steps = Mathf.Max(1, Mathf.CeilToInt(length / 8f));
		for (int s = 1; s < steps; s++)
		{
			Vector2 p = a.Lerp(b, s / (float)steps);
			if (!walkable(p) || !walkable(p + aside) || !walkable(p - aside))
				return false;
		}
		return true;
	}

	// How far either side of a straightened leg the ground must be walkable.
	private const float LegHalfWidth = 12f;

	// The flood: cells along the line from `from` to `to`, RouteMargin past each end and RouteSide to either side, eight
	// ways round, never across a shut cell's corner. Returns the cells' middles, start to goal, or null.
	private static List<Vector2> FindGrid(IReadOnlyList<Pool> pools, Vector2 from, Vector2 to, Func<Vector2, bool> walkable, float clearance)
	{
		float length = from.DistanceTo(to);
		Vector2 u = length > 0.001f ? (to - from) / length : Vector2.Right;
		Vector2 n = u.Orthogonal();
		int cols = Mathf.CeilToInt((length + 2f * RouteMargin) / RouteCell) + 1;
		int rows = Mathf.CeilToInt(2f * RouteSide / RouteCell) + 1;
		Vector2 origin = from - u * RouteMargin - n * RouteSide;
		Vector2 CellAt(int c, int r) => origin + u * (c * RouteCell) + n * (r * RouteCell);

		// The ground first, cell by cell; then each pool shuts only the cells in its own square, not every cell in the box
		// (a pool covers a few hundred of the six thousand: testing all of them against all of it was most of the cost).
		// A cell is ground if a body can stand on it with LegHalfWidth to spare every way, so that a step between two open
		// cells can never shave a lake's edge between them.
		var open = new bool[cols * rows];
		Vector2 du = u * LegHalfWidth, dn = n * LegHalfWidth;
		for (int c = 0; c < cols; c++)
		{
			for (int r = 0; r < rows; r++)
			{
				Vector2 p = CellAt(c, r);
				open[c * rows + r] = walkable == null
					|| (walkable(p) && walkable(p + du) && walkable(p - du) && walkable(p + dn) && walkable(p - dn));
			}
		}
		for (int i = 0; i < pools.Count; i++)
		{
			float reach = pools[i].Radius + clearance;
			Vector2 at = pools[i].Centre - origin;
			float pc = at.Dot(u) / RouteCell, pr = at.Dot(n) / RouteCell, span = reach / RouteCell;
			int c0 = Math.Max(0, Mathf.FloorToInt(pc - span)), c1 = Math.Min(cols - 1, Mathf.CeilToInt(pc + span));
			int r0 = Math.Max(0, Mathf.FloorToInt(pr - span)), r1 = Math.Min(rows - 1, Mathf.CeilToInt(pr + span));
			for (int c = c0; c <= c1; c++)
			{
				for (int r = r0; r <= r1; r++)
				{
					if (CellAt(c, r).DistanceSquaredTo(pools[i].Centre) < reach * reach)
						open[c * rows + r] = false;
				}
			}
		}
		int start = Mathf.RoundToInt(RouteMargin / RouteCell) * rows + Mathf.RoundToInt(RouteSide / RouteCell);
		int goal = Mathf.RoundToInt((RouteMargin + length) / RouteCell) * rows + Mathf.RoundToInt(RouteSide / RouteCell);
		// The two ends are where they are: the start and the landing's foot stand open whatever the ground says of a cell.
		open[start] = true;
		open[goal] = true;
		var cameFrom = new int[cols * rows];
		Array.Fill(cameFrom, -1);
		// Every cell is queued at most once, so the queue is an array as long as the grid.
		var queue = new int[cols * rows];
		int head = 0, tail = 0;
		queue[tail++] = start;
		cameFrom[start] = start;
		while (head < tail)
		{
			int cell = queue[head++];
			if (cell == goal)
				break;
			int c = cell / rows, r = cell % rows;
			for (int dc = -1; dc <= 1; dc++)
			{
				for (int dr = -1; dr <= 1; dr++)
				{
					if (dc == 0 && dr == 0)
						continue;
					int nc = c + dc, nr = r + dr;
					if (nc < 0 || nr < 0 || nc >= cols || nr >= rows)
						continue;
					int next = nc * rows + nr;
					if (!open[next] || cameFrom[next] >= 0)
						continue;
					if (dc != 0 && dr != 0 && (!open[nc * rows + r] || !open[c * rows + nr]))
						continue;
					cameFrom[next] = cell;
					queue[tail++] = next;
				}
			}
		}
		if (cameFrom[goal] < 0)
			return null;
		var path = new List<Vector2>();
		for (int cell = goal; ; cell = cameFrom[cell])
		{
			path.Add(CellAt(cell / rows, cell % rows));
			if (cell == start)
				break;
		}
		path.Reverse();
		return path;
	}

	public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float t = ab.LengthSquared() > 0f ? Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0f, 1f) : 0f;
		return p.DistanceTo(a + ab * t);
	}
}
