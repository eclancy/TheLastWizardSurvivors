using System;

namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Mystic Ruins (stage_4, StageEnvironmentKind.Ruins): the foliage pass (2026-11-04, .ai/terrain.md section
	/// 13, "The Mystic Ruins"). The Forest's stands are woods, the Fields' hedgerows, the Sands' stone the wind
	/// carved; the Ruins' are THE RUIN ITSELF where it has fallen in and the growth has taken it - overgrown quarters
	/// of the old seat of learning, thickest along the lines its walls stood on - with the open flags between them,
	/// most of the ground, "dust and cold ash", to fight on.
	/// </summary>
	/// <remarks>
	/// "Our old seat of learning. The wards broke from inside." THE LADDER, largest first (tools/art/props_ruins.py,
	/// detail_ruins.py): the stump of a round tower ("broken spires"), a tree grown out of a wall's top, the last
	/// piers of fallen walls and what is left of a colonnade, in the overgrown quarters, with the chapter's gnarled
	/// and dead trees; a statue's empty plinth, a fallen column lying in pieces, a capital sunk in the ground, on
	/// their edges; heaps of broken blocks and rubble the ivy has taken, and the brambles, straying into the open;
	/// and in the detail layer, cold ash in the open, weeds in the cracks, mossed chips on the edges, ivy and roots
	/// over the flags in the quarters. The stone is the walls' own (ruin_walls.py's top-lit, face-in-shade model),
	/// quieter than the cast's stone; the growth is verdant's dark half.
	///
	/// WHAT STAYS AS IT WAS. The stones and crystals keep their places in the list (0, 1), so they are where they
	/// were; every older detail rule keeps its index. The gnarled trees (2) and the brambles (3) grow by habitat now.
	/// The Five-Pointed Seal's court and the Broken Ward's ring keep their footprints as every set and clutter rule
	/// does (Node2DGame.IsInExploreReserve, trunks by 140 px more). Nothing here is the cast (no lone pillar on a
	/// base, no slab on an upright, no urn, no statue: the plinth is empty) or a tell of a secret (no pale or
	/// notched stone, no statue's head, no lone door frame, no benches, no paper, nothing warm or glinting).
	/// </remarks>
	private static class MysticRuins
	{
		private const string Rock1 = "res://assets/bonelight/world/rock-1.png";
		private const string Rock2 = "res://assets/bonelight/world/rock-2.png";
		private const string Manifest = "res://assets/bonelight/world/ruins/ruins.json";

		// THE FIELD: overgrown quarters, thickest along the lattice. A value noise at 13 cells (624 px, a quarter of a
		// screen or two across) and an edge noise at 5 decide where the ruin has fallen in and grown over; along the
		// maze lattice's wall lines - where a wall stands, or stood and fell - the growth and the rubble gather, and
		// the middle of a passage stays clearer, as rubble lies at the foot of what shed it. How much the lines count
		// wanders on a third noise at 9 cells, so not every line of an overgrown quarter is heaped. A trunk keeps
		// 72 px off a standing wall (Footing), so on a line whose wall still stands the line's growth is bushes and
		// clutter, and on a line whose wall fell, its last piers stand: the fragments stand where the walls ran.
		private const float QuarterCells = 13f;
		private const float EdgeCells = 5f;
		private const float EdgeWeight = 0.3f;
		private const float HeapCells = 9f;
		private const float LineWeight = 0.42f;
		// How far from a wall line, in cells, its pull reaches: to the middle of a six-tile passage.
		private const float LineReach = 2.6f;
		private const int SaltQuarter = 0x5E02;
		private const int SaltEdge = 0x5E03;
		private const int SaltHeap = 0x5E04;

		// The maze lattice the Ruin topology stands on (LevelGenerator.Ruin.cs): eight-tile units, the wall strip at
		// offsets six and seven after a shift of three, so world tiles x = 3 and 4 (mod 8) are the strip and its middle
		// is the line x = 4.0. These are LevelGenerator's MazeUnit and MazeLatticeShift, which are private to it; if
		// they ever change, this field only gathers its rubble beside the walls instead of along them - nothing
		// breaks - and the photographs are where to see it.
		private const float LatticeUnit = 8f;
		private const float LineCentre = 4f;
		private const float StripHalf = 1f;

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Ruins,

			// Measured on the field (Woodland.Validate's 70 x 70 sample, three seeds, and a Python port of it): 16% of
			// the ground is an overgrown quarter's core, 67-68% open flags, the rest their edges. The start's clearing
			// is the Forest's (6 and 16 cells).
			Canopy = new CanopyProfile
			{
				Shape = CanopyShape.Own,
				Own = Field,
				ClearingEnd0 = 0.52f,
				ClearingEnd1 = 0.58f,
				StandStart0 = 0.60f,
				StandStart1 = 0.66f,
				// The quarter's core, as Woodland.Validate counts the shares; no floor is laid by it (below).
				StandFloor = 0.64f,
				// NO STAND FLOOR, on purpose. The palette's overgrowth was the obvious one (grass and moss over the
				// stone), but the chapter's old rules lay its brightest clutter on it - white and violet flowers and
				// clover, hi +55 to +78 over the flags (detail_contrast.py), the brightest things on this floor after
				// the cast and the orbs - and laid under every quarter it would have spread them over a sixth of the
				// ground, where today they lie on the odd blob.
				// Its dark_dirt carries the old mushrooms, and its cobble is a carpet of even stones (paving). So
				// the quarters are read by what stands and lies in them, the ground is exactly the ground it was,
				// and so is everything placed by it (the seal's court, the ward's ring).
				SaltStand = SaltQuarter,
				SaltEdge = SaltEdge,
				SaltGap = SaltHeap,
				StandShareMin = 0.11f,
				StandShareMax = 0.22f,
				ClearingShareMin = 0.58f,
				ClearingShareMax = 0.76f,
			},

			Decor = new[]
			{
				// 0. Stones, in threes, as before.
				new FoliageSet
				{
					Sprites = new[] { Rock1, Rock2 }, MinScale = 0.88f, MaxScale = 1.02f,
					Count = 18, ClumpSize = 3, Clump = ClumpBase.GroundAccent, Loners = 0.05f,
				},
				// 1. Crystals: the profile's RuinCount (24), as before.
				new FoliageSet
				{
					Sprites = new[] { "res://assets/bonelight/world/crystal-yellow.png", "res://assets/bonelight/world/crystal-violet.png" },
					MinScale = 0.95f, MaxScale = 1.12f,
					Count = 24, ClumpSize = 3, Clump = ClumpBase.Ruin, Loners = 0.06f,
				},
				// 2. The trees that grew up through the stones, gnarled and dead: in the overgrown quarters now, and on
				// their edges, a few.
				new FoliageSet
				{
					Grounded = new[] { "gnarled-1", "gnarled-2", "dead-tree-1" },
					Count = 260, ClumpSize = 2, Clump = ClumpBase.Stand, RadiusMin = 0.4f, RadiusMax = 0.6f, Separation = 0.7f,
					Loners = 0.2f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0.02f, 0.4f, 1f),
				},
				// 3. Brambles in the rubble: thickest on a quarter's edge, straying into the open.
				new FoliageSet
				{
					Grounded = new[] { "bramble-1", "bramble-2" },
					Count = 520, ClumpSize = 3, Clump = ClumpBase.Bush, Loners = 0.08f, MinSpacing = 40f,
					Habitat = new Woodland.Habitat(0.08f, 1f, 0.5f),
				},
				// 4. The standing ruin: fallen walls' last piers, a colonnade's remains, a tree grown out of a wall, and
				// rarest, the stump of a round tower (one pick in eight). Massed in the quarters; the rule that no stand
				// pins the player (Footing) keeps every gap between them, and between them and the walls, at least 72 px.
				// 2100 (was 1500, 2026-11-07): at 1500 the walls and the 72 px rule left 2.4 a chunk, and at zoom 0.3 the
				// quarters read as scattered pieces, not as massed ruin. The stone sits near the floor's own value, so a
				// quarter is told by how much stands in it.
				new FoliageSet
				{
					Grounded = new[] { "wall-run-1", "wall-run-2", "colonnade-1", "wall-run-1", "root-wall-1", "wall-run-2", "colonnade-1", "spire-stump-1" },
					Manifest = Manifest,
					Count = 2100, ClumpSize = 2, Clump = ClumpBase.Stand, RadiusMin = 0.35f, RadiusMax = 0.55f, Separation = 0.6f,
					Loners = 0.15f, MinSpacing = 130f, Habitat = new Woodland.Habitat(0f, 0.15f, 1f),
				},
				// 5. A fallen column lying in its pieces, and a capital sunk where it fell: on a quarter's edge, now and then
				// out on the flags. Knee-high, walked through.
				new FoliageSet
				{
					Grounded = new[] { "drums-1", "drums-2", "capital-1", "drums-1" }, Manifest = Manifest,
					Count = 520, ClumpSize = 2, Clump = ClumpBase.Bush, Loners = 0.35f, MinSpacing = 90f,
					Habitat = new Woodland.Habitat(0.06f, 1f, 0.45f),
				},
				// 6. Heaps of broken blocks and rubble the ivy has taken: the edges and the quarters, a few out in the open.
				// 2000 in fours, as thick in a quarter as on its edge (was 1300 in threes, stand 0.8; 2026-11-07, as set 4):
				// walked over, so it thickens a quarter without narrowing a way through it.
				new FoliageSet
				{
					Grounded = new[] { "rubble-1", "rubble-2", "ivy-mound-1", "rubble-2" }, Manifest = Manifest,
					Count = 2000, ClumpSize = 4, Clump = ClumpBase.Bush, RadiusMin = 0.6f, RadiusMax = 0.8f, Separation = 0.7f,
					Loners = 0.1f, MinSpacing = 44f, Habitat = new Woodland.Habitat(0.07f, 1f, 1f),
				},
				// 7. A statue's empty plinth, the feet broken off at the ankle: where the statues stood - the edges of the
				// quarters and the open courts - and alone. The chapter's Warded Statues stepped down off them.
				new FoliageSet
				{
					Grounded = new[] { "plinth-1" }, Manifest = Manifest,
					Count = 160, ClumpSize = 1, Clump = ClumpBase.Tree, RadiusMin = 0.4f, RadiusMax = 0.6f,
					Loners = 0.6f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0.25f, 1f, 0.3f),
				},
			},

			// The bottom rungs (tools/art/detail_ruins.py), densities the open ground's before the habitat weight
			// (clearing, fringe, stand). The open flags keep to cold ash and the odd weed: the old rules already lay
			// rubble, cracks, pebbles and moss seams on them. The quarters get the ivy and the roots that say what
			// has taken them, in place of a floor of their own (above).
			Clutter = new[]
			{
				GroundDetail.Clutter("cold_ash", 0.03f, 6, 1f, 0.35f, 0f),
				GroundDetail.Clutter("crack_weeds", 0.03f, 4, 0.35f, 1f, 0.6f),
				GroundDetail.Clutter("moss_chips", 0.035f, 3, 0.1f, 1f, 0.8f),
				// The quarters' two, raised with sets 4 and 6 (2026-11-07; were 0.055 and 0.03).
				GroundDetail.Clutter("ivy_runners", 0.075f, 4, 0f, 0.5f, 1f),
				GroundDetail.Clutter("root_runs", 0.04f, 2, 0f, 0.3f, 1f),
			},
		};

		/// <summary>The field (CanopyProfile.Own): a pure function of (seed, world cell), in 0..1.</summary>
		private static float Field(ulong seed, float cellX, float cellY)
		{
			float quarter = Noise(seed, SaltQuarter, 0, cellX / QuarterCells, cellY / QuarterCells) * (1f - EdgeWeight)
				+ Noise(seed, SaltEdge, 1, cellX / EdgeCells, cellY / EdgeCells) * EdgeWeight;
			float d = MathF.Min(StripDistance(cellX), StripDistance(cellY));
			float line = 1f - Smooth(0f, LineReach, d);
			float heap = Noise(seed, SaltHeap, 2, cellX / HeapCells, cellY / HeapCells);
			float weight = LineWeight * (0.4f + 0.6f * heap);
			return quarter * (1f - weight + weight * line);
		}

		/// <summary>Cells from a world coordinate to the nearest wall strip of the lattice on its axis (0 inside one).</summary>
		private static float StripDistance(float t)
		{
			float w = (t - LineCentre) % LatticeUnit;
			if (w < 0f)
				w += LatticeUnit;
			if (w > LatticeUnit * 0.5f)
				w -= LatticeUnit;
			return MathF.Max(0f, MathF.Abs(w) - StripHalf);
		}

		private static float Smooth(float e0, float e1, float x)
		{
			float t = Math.Clamp((x - e0) / (e1 - e0), 0f, 1f);
			return t * t * (3f - 2f * t);
		}

		// The field is asked per cell (the clutter and the decor), and a cell's neighbours almost always share its
		// lattice cell, so each noise keeps its last four corners - the same numbers, computed once (the Sands' field
		// does the same). One copy per thread.
		[ThreadStatic] private static float[]? corners;
		[ThreadStatic] private static (ulong Seed, int X, int Y, bool Set)[]? cornerKeys;

		private static float Noise(ulong seed, int salt, int slot, float x, float y)
		{
			int gx = (int)MathF.Floor(x);
			int gy = (int)MathF.Floor(y);
			float fx = x - gx, fy = y - gy;
			fx = fx * fx * (3f - 2f * fx);
			fy = fy * fy * (3f - 2f * fy);
			float[] memo = corners ??= new float[12];
			var keys = cornerKeys ??= new (ulong, int, int, bool)[3];
			if (keys[slot] != (seed, gx, gy, true))
			{
				memo[4 * slot] = Unit(LevelGenerator.Hash(seed, gx, gy, salt));
				memo[4 * slot + 1] = Unit(LevelGenerator.Hash(seed, gx + 1, gy, salt));
				memo[4 * slot + 2] = Unit(LevelGenerator.Hash(seed, gx, gy + 1, salt));
				memo[4 * slot + 3] = Unit(LevelGenerator.Hash(seed, gx + 1, gy + 1, salt));
				keys[slot] = (seed, gx, gy, true);
			}
			float a = memo[4 * slot], b = memo[4 * slot + 1], c = memo[4 * slot + 2], d = memo[4 * slot + 3];
			float top = a + (b - a) * fx;
			return top + ((c + (d - c) * fx) - top) * fy;
		}

		private static float Unit(ulong h) => (float)((h >> 11) * (1.0 / (1UL << 53)));
	}
}
