using System;

namespace TheOneHeMissed.scripts;

// THE BARROWS TOPOLOGY: the White Downs' burial mounds on open turf (.ai/second-half.md 4.2, piece F3).
// "Open, the barrows solid." The downs are open ground - long sightlines, the chalk and the turf - and on them
// stand the barrows, grassed mounds of the people who kept the downs before him. They are what the chapter asks
// of the player: the Unstrung Hunter draws on him along a line that follows him, and the answer is to put a
// barrow between them (UnstrungHunterBoss); the Silver Hind cannot see him behind one (SilverHindEvent).
//
// THE BARROWS ARE WALLS. A barrow cell is the `wall` terrain like every wall in the game, so the four places
// .ai/terrain.md section 2 says must agree about a blocker all do already - the collision (the player's feet and
// the swarm's bodies), the navigation grid (the flow field routes the swarm round), the placement tests, and the
// shot test (MazeNavigation.IsWallAt / HasLineOfSight, which is what makes a barrow cover against every bolt in
// the chapter and not only the boss's). It is drawn as a mound by its own wall set (downs_walls.py,
// StageEnvironmentProfile.WallSet = "barrow_wall"). Walled, then, as the Ruins are: no cleanup pass (it would
// eat a mound's rounded corners), and the maze's spawn and routing rules on ground that is nearly all open.
//
// WHERE THEY STAND. One candidate a super-cell of SuperCell x SuperCell tiles, a pure function of (seed,
// super-cell): whether it holds a barrow, which kind, where inside its inner square, how big. Three kinds -
// a round barrow (a bowl barrow, the commonest), a great round one, and a long barrow lying east-west or
// north-south. A candidate too close to a neighbour's (its bounding circle within MinGapTiles of the other's)
// stands down when the neighbour's priority roll is the higher, so two mounds never leave a notch a body
// wedges in or close a pocket between them, and the answer is the same from every chunk that asks: each
// decision reads only the hashed candidates of its own and its eight neighbouring super-cells. None stands
// within SpawnKeepTiles of the start, so the run never opens beside a mound the spawn clearing has cut.
//
// THE SHAPES are drawn for the wall set's autotile roles. A round barrow is a disc centred on a tile centre:
// at radius 3.2 its rows are 3-5-7-7-7-5-3, which the set's corners read as a curve (2.5 reads as a plus and
// 2.9 as a square - the ground agent's sizes). A long barrow is a squared ellipse (exponent 4), blunt at the
// ends, three or four tiles wide, so neither end is a one-tile spike.
public sealed partial class LevelGenerator
{
	static partial void BuildBarrowsTopology(ref TopologyBuild build)
	{
		build.Generate = GenerateBarrows;
		build.Walled = true;
	}

	/// <summary>Tiles a super-cell: one barrow candidate in each (768 px, about a phone's width).</summary>
	public const int BarrowSuperCell = 16;
	// The candidate's centre stays this many tiles inside its super-cell, so it lies wholly in its own and its
	// eight neighbours' reach.
	private const int BarrowInset = 3;
	/// <summary>The least clear turf between two barrows' bounding circles, in tiles (192 px: a wide way through).</summary>
	public const float BarrowMinGapTiles = 4f;
	// No barrow's bounding circle comes this close to the start, in tiles.
	private const float BarrowSpawnKeepTiles = 9f;

	// How many super-cells hold a barrow, and of which kind.
	private const float BarrowChance = 0.6f;
	private const float BarrowGreatRoundShare = 0.12f;
	private const float BarrowLongShare = 0.38f;

	private const int SaltBarrow = 0x7B11;

	/// <summary>The barrow kinds the downs carry.</summary>
	public enum BarrowKind : byte { None, Round, Long }

	/// <summary>One barrow, in tile coordinates: centre (tile-centre aligned), and its half-extents.</summary>
	public readonly struct Barrow
	{
		public Barrow(BarrowKind kind, float cx, float cy, float halfX, float halfY, float priority)
		{
			Kind = kind;
			Cx = cx;
			Cy = cy;
			HalfX = halfX;
			HalfY = halfY;
			Priority = priority;
		}

		public BarrowKind Kind { get; }
		public float Cx { get; }
		public float Cy { get; }
		public float HalfX { get; }
		public float HalfY { get; }
		public float Priority { get; }
		/// <summary>The radius of the circle round it, in tiles: the spacing test's.</summary>
		public float Reach => MathF.Max(HalfX, HalfY);

		/// <summary>Whether the tile whose centre is (x + 0.5, y + 0.5) is barrow.</summary>
		/// <remarks>
		/// No row or column of a barrow may end in a one-tile spike: the wall set draws one as a stub, and on the
		/// first photographs every mound wore a nub on its crown. A disc centred on a tile centre ends in a row
		/// one tile wide whenever the row past it is just inside the radius, so the radii are kept to the bands
		/// where the last row is three wide (BarrowCandidate). A long barrow is a stadium - a band its own width,
		/// capped with half-discs of that width - so its ends are as wide as it is.
		/// </remarks>
		public bool Covers(int x, int y)
		{
			if (Kind == BarrowKind.None)
				return false;
			float dx = x + 0.5f - Cx, dy = y + 0.5f - Cy;
			if (Kind == BarrowKind.Round)
				return dx * dx + dy * dy <= HalfX * HalfX;
			// The stadium: along its axis, the body runs to (half-length - width) and a half-disc of the width caps it.
			bool alongX = HalfX >= HalfY;
			float along = MathF.Abs(alongX ? dx : dy), across = MathF.Abs(alongX ? dy : dx);
			float width = alongX ? HalfY : HalfX, body = (alongX ? HalfX : HalfY) - width;
			if (across > width)
				return false;
			if (along <= body)
				return true;
			float over = along - body;
			return over * over + across * across <= width * width;
		}
	}

	/// <summary>
	/// The candidate of super-cell (<paramref name="sx"/>, <paramref name="sy"/>) before spacing: a pure function
	/// of the seed and the super-cell. <see cref="BarrowKind.None"/> when it holds none.
	/// </summary>
	public static Barrow BarrowCandidate(ulong seed, int sx, int sy)
	{
		var rng = new Rng(Hash(seed, sx, sy, SaltBarrow));
		// Every roll drawn, in order, whatever the first one says: nothing later shifts with it.
		float present = rng.NextFloat();
		float kindRoll = rng.NextFloat();
		float ox = rng.NextFloat(), oy = rng.NextFloat();
		float sizeRoll = rng.NextFloat();
		float widthRoll = rng.NextFloat();
		float axisRoll = rng.NextFloat();
		float priority = rng.NextFloat();
		if (present >= BarrowChance)
			return default;

		int span = BarrowSuperCell - 2 * BarrowInset;
		float cx = sx * BarrowSuperCell + BarrowInset + MathF.Floor(ox * (span + 1)) + 0.5f;
		float cy = sy * BarrowSuperCell + BarrowInset + MathF.Floor(oy * (span + 1)) + 0.5f;

		Barrow barrow;
		if (kindRoll < BarrowLongShare)
		{
			float half = 4f + sizeRoll * 2f;            // 4 to 6 tiles either side of the middle: 9 to 13 long
			// Three across (1.5) or five (2.4), the widths whose caps end three tiles wide; 2.2 ends in a one-tile point.
			float width = widthRoll < 0.65f ? 1.5f : 2.4f;
			barrow = axisRoll < 0.5f
				? new Barrow(BarrowKind.Long, cx, cy, half, width, priority)
				: new Barrow(BarrowKind.Long, cx, cy, width, half, priority);
		}
		else
		{
			// Radii whose last row is three tiles wide (Barrow.Covers): a bowl barrow 3.2 to 3.6 (rows 3-5-7-7-7-5-3),
			// a great round one 4.15 to 4.45 (9 across). Between them, and below 3.17, the crown ends in a nub.
			float r = kindRoll < BarrowLongShare + BarrowGreatRoundShare
				? 4.15f + sizeRoll * 0.3f
				: 3.2f + sizeRoll * 0.4f;
			barrow = new Barrow(BarrowKind.Round, cx, cy, r, r, priority);
		}

		float d = MathF.Sqrt(barrow.Cx * barrow.Cx + barrow.Cy * barrow.Cy);
		return d - barrow.Reach < BarrowSpawnKeepTiles ? default : barrow;
	}

	/// <summary>
	/// The barrow super-cell (<paramref name="sx"/>, <paramref name="sy"/>) actually holds: its candidate, unless a
	/// neighbour's stands within <see cref="BarrowMinGapTiles"/> of it with a higher priority roll (ties to the
	/// lower super-cell). Reads only the nine candidates round it, so every chunk that asks gets the same answer.
	/// </summary>
	public static Barrow BarrowAt(ulong seed, int sx, int sy)
	{
		Barrow own = BarrowCandidate(seed, sx, sy);
		if (own.Kind == BarrowKind.None)
			return own;
		for (int dy = -1; dy <= 1; dy++)
		{
			for (int dx = -1; dx <= 1; dx++)
			{
				if (dx == 0 && dy == 0)
					continue;
				Barrow other = BarrowCandidate(seed, sx + dx, sy + dy);
				if (other.Kind == BarrowKind.None)
					continue;
				float ddx = other.Cx - own.Cx, ddy = other.Cy - own.Cy;
				float gap = MathF.Sqrt(ddx * ddx + ddy * ddy) - own.Reach - other.Reach;
				if (gap >= BarrowMinGapTiles)
					continue;
				bool otherWins = other.Priority > own.Priority
					|| (other.Priority == own.Priority && (dy < 0 || (dy == 0 && dx < 0)));
				if (otherWins)
					return default;
			}
		}
		return own;
	}

	// The barrows that can reach a grid: one per super-cell, kept for the chunk being built.
	[ThreadStatic] private static Barrow[] barrowScratch;

	private static void GenerateBarrows(string[,] grid, TopologyChunk chunk)
	{
		int ext = WalledFloorMargin;
		int size = chunk.Size + 2 * ext;
		int ex0 = chunk.X0 - ext;
		int ey0 = chunk.Y0 - ext;

		// The super-cells whose barrows can reach this grid: a barrow lies inside its own super-cell's reach.
		int s0x = FloorDiv(ex0, BarrowSuperCell) - 1, s1x = FloorDiv(ex0 + size - 1, BarrowSuperCell) + 1;
		int s0y = FloorDiv(ey0, BarrowSuperCell) - 1, s1y = FloorDiv(ey0 + size - 1, BarrowSuperCell) + 1;
		int count = (s1x - s0x + 1) * (s1y - s0y + 1);
		Barrow[] barrows = barrowScratch is { } b && b.Length >= count ? b : barrowScratch = new Barrow[count];
		int n = 0;
		for (int sy = s0y; sy <= s1y; sy++)
			for (int sx = s0x; sx <= s1x; sx++)
			{
				Barrow barrow = BarrowAt(chunk.Seed, sx, sy);
				if (barrow.Kind != BarrowKind.None)
					barrows[n++] = barrow;
			}

		sbyte[,] cells = WalledScratch(size);
		for (int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				int wx = ex0 + x, wy = ey0 + y;
				sbyte cell = CellFloor;
				for (int k = 0; k < n; k++)
				{
					if (barrows[k].Covers(wx, wy))
					{
						cell = CellWall;
						break;
					}
				}
				cells[x, y] = cell;
			}
		}

		LayFloorBetweenWalls(grid, chunk, cells, ext);
	}
}
