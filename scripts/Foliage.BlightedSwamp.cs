using System;

namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Blighted Swamp (stage_3): the foliage pass (2026-11-04, .ai/terrain.md section 13, "The Blighted
	/// Swamp"). Its stands are not woods and not hedges: they are TREE ISLANDS - hummocks of black peat standing
	/// out of an open fen, each a knot of drowned wood ringed with reed beds - and the fen between them, mud and
	/// pools and rush, is where the fighting is. "Reeds, fog, and lanterns nobody living lit."
	/// </summary>
	/// <remarks>
	/// THE LADDER, largest first. Drowned giants, bog alders up on stilt roots and the chapter's willows,
	/// massed on the islands (the only trunks, kept apart by the rule that no stand pins the player); the
	/// old moss-hung snags, on the islands and now and then standing dead out in the fen; reed beds - the
	/// swamp's own mass - ringing every island and lying in patches across the fen; tussock hummocks and the
	/// old sedge tussocks on the fringes; and in the detail layer rushes, duckweed on the pools, cypress
	/// knees round the trees, peat clods, sodden litter and rotwood. All of it in the dark half of verdant
	/// and linen's grey-green (tools/art/props_swamp.py, detail_swamp.py).
	///
	/// THE GROUND. The islands' floor is the palette's dark peat (dark_dirt), laid under every island, so a
	/// drowned wood stands in the dark of its own shade. Not the palette's moss, which was tried first: its
	/// tile is lichen's teal, and laid under a fifth of the land on brown mud it read as shallow water - a
	/// floor you can walk on that looks like the water you cannot. The blobs are not sorted (no clearing
	/// floor): moss, peat, rock and water fall where they rolled, as before.
	///
	/// WHAT STAYS AS IT WAS. The three old sets keep their places in the list (0-2) and their sprites; they
	/// grow by the field now. Every older detail rule keeps its index. The lanterns, the gibbet's cage and its
	/// ferry landing, and the Swallow-Hole keep their footprints clear, as every decor set and clutter rule
	/// does (Node2DGame.IsInExploreReserve); the escorted man walks the player's own steps, so the islands are
	/// never in his way. Nothing here leans one way, is a puddle of black mud, is a warm point or is straight:
	/// those are the tells of the Swamp's secrets (.ai/achievements-and-secrets.md 8.6).
	/// </remarks>
	private static class BlightedSwamp
	{
		private const string Manifest = "res://assets/bonelight/world/swamp/swamp.json";

		// The field's salts (assigned: 0x59C2-4). Islands (where each lattice cell's island is, and the
		// drowned woods' clustering), their ragged edges, and the reed patches out in the fen.
		private const int SaltIslands = 0x59C2;
		private const int SaltRag = 0x59C3;
		private const int SaltReeds = 0x59C4;

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Swamp,

			// Islands on a 16-cell lattice (768 px), each 3-13 cells across with a lobe on most, so a PC screen
			// shows two or three with fen between; clustered by a slow noise into drowned woods and stretches of
			// open fen. Measured on the field (Woodland.Validate's sample, three seeds; first in a Python port):
			// about a fifth of the ground is island, half is open fen, the rest the reed fringe round the
			// islands and the reed patches out in the fen.
			Canopy = new CanopyProfile
			{
				Shape = CanopyShape.Own,
				Own = Islands,
				ClearingEnd0 = 0.08f,
				ClearingEnd1 = 0.24f,
				StandStart0 = 0.55f,
				StandStart1 = 0.7f,
				StandFloor = 0.66f,
				StandFloorTerrain = "dark_dirt",
				SaltStand = SaltIslands,
				SaltEdge = SaltRag,
				SaltGap = SaltReeds,
				StandShareMin = 0.15f,
				StandShareMax = 0.27f,
				ClearingShareMin = 0.42f,
				ClearingShareMax = 0.58f,
			},

			Decor = new[]
			{
				// 0. The old sedge tussocks and dark shrubs (trees.py), on the fringes now.
				new FoliageSet
				{
					Grounded = new[] { "reed-tuft-1", "reed-tuft-2", "swamp-shrub-1" },
					Count = 700, ClumpSize = 4, Clump = ClumpBase.Bush, Loners = 0.06f, MinSpacing = 36f,
					Habitat = new Woodland.Habitat(0.25f, 1f, 0.4f),
				},
				// 1. The willows, on the islands. Tight clumps (a Stand clump's radius at half to three quarters):
				// an island is a few hundred pixels across, and a full Stand clump spilled most of its members into
				// the fen, where the habitat dropped them - the first islands held three or four trees.
				new FoliageSet
				{
					Grounded = new[] { "willow-1", "willow-2" },
					Count = 900, ClumpSize = 3, Clump = ClumpBase.Stand, RadiusMin = 0.5f, RadiusMax = 0.7f, Separation = 0.6f,
					Loners = 0.03f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0f, 0.2f, 1f),
				},
				// 2. The moss-hung snags: on the islands, and now and then one standing dead out in the fen.
				new FoliageSet
				{
					Grounded = new[] { "swamp-snag-1", "swamp-snag-2" },
					Count = 260, ClumpSize = 2, Clump = ClumpBase.Tree, RadiusMin = 0.5f, RadiusMax = 0.7f, Separation = 0.6f,
					Loners = 0.3f, MinSpacing = YoungTreeSpacing, Habitat = new Woodland.Habitat(0.05f, 0.45f, 1f),
				},
				// 3. The islands' trees: bog alders on stilt roots, and a drowned giant one in five.
				new FoliageSet
				{
					Grounded = new[] { "stilt-tree-1", "stilt-tree-2", "stilt-tree-1", "stilt-tree-2", "drowned-giant-1" },
					Manifest = Manifest,
					Count = 2600, ClumpSize = 4, Clump = ClumpBase.Stand, RadiusMin = 0.5f, RadiusMax = 0.75f, Separation = 0.6f,
					Loners = 0.03f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0f, 0.12f, 1f),
				},
				// 4. Reed beds: the ring round every island and the patches out in the fen. Waded through.
				new FoliageSet
				{
					Grounded = new[] { "reed-bed-1", "reed-bed-2", "reed-bed-3" }, Manifest = Manifest,
					Count = 2200, ClumpSize = 4, Clump = ClumpBase.Bush, RadiusMin = 0.5f, RadiusMax = 0.8f, Separation = 0.6f,
					Loners = 0.04f, MinSpacing = 70f, Habitat = new Woodland.Habitat(0.04f, 1f, 0.3f),
				},
				// 5. Tussock hummocks, on the fringe and straying into the fen.
				new FoliageSet
				{
					Grounded = new[] { "hummock-1", "hummock-2", "hummock-3" }, Manifest = Manifest,
					Count = 900, ClumpSize = 3, Clump = ClumpBase.Bush, RadiusMin = 0.6f, RadiusMax = 0.9f, Separation = 0.7f,
					Loners = 0.12f, MinSpacing = 44f, Habitat = new Woodland.Habitat(0.2f, 1f, 0.35f),
				},
			},

			// THE BOTTOM RUNGS (tools/art/detail_swamp.py): rush across the open mud and thickest at a fringe;
			// clods of peat out in the open fen; cypress knees round the islands' trees; sodden litter and rotwood
			// on the islands' peat, and fewer rushes there; duckweed on the pools, gathered where an island shades
			// them. Not more of the old rules' white flowers, mushrooms or clover: they stand +53 to +62 L* over the
			// mud at their brightest (detail_contrast.py), above the cast's rims. Densities are the open ground's,
			// before the habitat weight (clearing, fringe, stand).
			Clutter = new[]
			{
				GroundDetail.Clutter("rushes", 0.11f, 5, 0.6f, 1f, 0.35f),
				GroundDetail.Clutter("peat_clods", 0.025f, 4, 1f, 0.3f, 0f),
				GroundDetail.Clutter("knees", 0.03f, 4, 0f, 0.5f, 1f),
				GroundDetail.Clutter("rotwood", 0.01f, 1, 0.2f, 1f, 0.6f),
				GroundDetail.Clutter("knees", 0.05f, 4, 0.2f, 0.7f, 1f, "dark_dirt"),
				GroundDetail.Clutter("bog_litter", 0.07f, 5, 0.3f, 0.8f, 1f, "dark_dirt"),
				GroundDetail.Clutter("rotwood", 0.01f, 1, 0.3f, 1f, 1f, "dark_dirt"),
				GroundDetail.Clutter("rushes", 0.04f, 5, 1f, 1f, 0.4f, "dark_dirt"),
				GroundDetail.Clutter("duckweed", 0.14f, 5, 0.3f, 1f, 1f, "water"),
			},
		};

		// --- The field: tree islands in an open fen ------------------------------------------------------

		// The island lattice, in cells: one island site per 16 x 16 cells (768 px).
		private const int IslandCells = 16;
		// How likely a site holds an island, from open fen to a drowned wood (the clustering noise, at three
		// lattice cells, decides which).
		private const float IslandChanceOpen = 0.3f;
		private const float IslandChanceWood = 0.95f;
		// How far an island's centre wanders from its site, in lattice cells.
		private const float IslandJitter = 0.3f;
		// An island's radius in cells, its stretch (an oval, turned at random) and the chance of a second lobe.
		private const float IslandRadiusMin = 2.6f;
		private const float IslandRadiusMax = 6.4f;
		private const float IslandStretch = 0.45f;
		private const float IslandLobeChance = 0.75f;
		// Where an island is solid (1) and where it has gone to fen (0), in radii, before the ragged edge.
		private const float IslandCore = 0.45f;
		private const float IslandEdge = 2f;
		// The ragged edge: a noise at 5 cells moves the edge by up to a third of a radius either way.
		private const int RagCells = 5;
		private const float Rag = 0.32f;
		// Reed patches out in the fen: a noise at 8 cells, rising to 0.45 where it is high - pure fringe, so a
		// patch is reed beds and hummocks and never trees.
		private const int ReedCells = 8;
		private const float ReedLow = 0.64f;
		private const float ReedHigh = 0.84f;
		private const float ReedTop = 0.45f;
		// The clustering noise is read on the island lattice's coordinates, offset so it never reads the
		// island sites' own hashes.
		private const float WoodOffset = 7919.5f;

		private readonly struct Island
		{
			public readonly bool Present;
			public readonly float X, Y, Rx, Ry, Cos, Sin;
			public readonly bool HasLobe;
			public readonly float LobeX, LobeY, LobeR;

			public Island(float x, float y, float rx, float ry, float cos, float sin, bool hasLobe, float lx, float ly, float lr)
			{
				Present = true;
				X = x; Y = y; Rx = rx; Ry = ry; Cos = cos; Sin = sin;
				HasLobe = hasLobe; LobeX = lx; LobeY = ly; LobeR = lr;
			}
		}

		// THE FIELD IS ASKED PER CELL (the stand floor of every padded chunk, the clutter, the decor), and a
		// cell's neighbours almost always fall in the same lattice cell, so the nine islands round the last one
		// are kept (one copy per thread, as Woodland keeps its own).
		[ThreadStatic] private static Island[]? islandMemo;
		[ThreadStatic] private static (ulong Seed, int X, int Y, bool Set) islandKey;

		/// <summary>
		/// The Swamp's canopy at a point in world cells: 1 inside an island, falling to 0 in the open fen over the
		/// island's reed fringe, and up to <see cref="ReedTop"/> in a reed patch. A pure function of (seed, cell).
		/// </summary>
		private static float Islands(ulong seed, float cellX, float cellY)
		{
			int ix = (int)MathF.Floor(cellX / IslandCells);
			int iy = (int)MathF.Floor(cellY / IslandCells);
			Island[] islands = islandMemo ??= new Island[9];
			if (!islandKey.Set || islandKey.Seed != seed || islandKey.X != ix || islandKey.Y != iy)
			{
				int k = 0;
				for (int j = -1; j <= 1; j++)
					for (int i = -1; i <= 1; i++, k++)
						islands[k] = IslandAt(seed, ix + i, iy + j);
				islandKey = (seed, ix, iy, true);
			}

			float rag = (Noise(seed, SaltRag, cellX / RagCells, cellY / RagCells) - 0.5f) * 2f * Rag;
			float best = 0f;
			foreach (Island island in islands)
			{
				if (!island.Present)
					continue;
				float dx = cellX - island.X, dy = cellY - island.Y;
				float u = (dx * island.Cos + dy * island.Sin) / island.Rx;
				float v = (-dx * island.Sin + dy * island.Cos) / island.Ry;
				float d = MathF.Sqrt(u * u + v * v);
				if (island.HasLobe)
				{
					float lx = cellX - island.LobeX, ly = cellY - island.LobeY;
					d = MathF.Min(d, MathF.Sqrt(lx * lx + ly * ly) / island.LobeR);
				}
				best = MathF.Max(best, 1f - Smooth(IslandCore, IslandEdge, d + rag));
			}
			float reed = ReedTop * Smooth(ReedLow, ReedHigh, Noise(seed, SaltReeds, cellX / ReedCells, cellY / ReedCells));
			return MathF.Max(best, reed);
		}

		private static Island IslandAt(ulong seed, int i, int j)
		{
			ulong h = LevelGenerator.Hash(seed, i, j, SaltIslands);
			float wood = Noise(seed, SaltIslands, i / 3f + WoodOffset, j / 3f + WoodOffset);
			float chance = IslandChanceOpen + (IslandChanceWood - IslandChanceOpen) * Smooth(0.25f, 0.75f, wood);
			if (Unit(h) >= chance)
				return default;
			ulong h2 = LevelGenerator.Mix(h), h3 = LevelGenerator.Mix(h2), h4 = LevelGenerator.Mix(h3), h5 = LevelGenerator.Mix(h4);
			float x = (i + 0.5f + (Unit(h2) - 0.5f) * 2f * IslandJitter) * IslandCells;
			float y = (j + 0.5f + (Unit(h3) - 0.5f) * 2f * IslandJitter) * IslandCells;
			float r = IslandRadiusMin + (IslandRadiusMax - IslandRadiusMin) * Unit(h4);
			float e = IslandStretch * Unit(h5);
			ulong h6 = LevelGenerator.Mix(h5);
			float a = Unit(h6) * MathF.PI;
			ulong h7 = LevelGenerator.Mix(h6);
			bool lobe = Unit(h7) < IslandLobeChance;
			float b = Unit(LevelGenerator.Mix(h7)) * MathF.Tau;
			float lr = r * (0.45f + 0.3f * Unit(LevelGenerator.Mix(LevelGenerator.Mix(h7))));
			return new Island(x, y, r * (1f + e), r / (1f + e), MathF.Cos(a), MathF.Sin(a),
				lobe, x + MathF.Cos(b) * r * 0.95f, y + MathF.Sin(b) * r * 0.95f, lr);
		}

		private static float Smooth(float e0, float e1, float x)
		{
			float t = Math.Clamp((x - e0) / (e1 - e0), 0f, 1f);
			return t * t * (3f - 2f * t);
		}

		// Smooth value noise: a hashed value at each lattice point, blended with a smoothstep (Woodland's, which
		// is private to it; the same construction).
		private static float Noise(ulong seed, int salt, float x, float y)
		{
			int gx = (int)MathF.Floor(x);
			int gy = (int)MathF.Floor(y);
			float fx = x - gx, fy = y - gy;
			fx = fx * fx * (3f - 2f * fx);
			fy = fy * fy * (3f - 2f * fy);
			float a = Unit(LevelGenerator.Hash(seed, gx, gy, salt));
			float b = Unit(LevelGenerator.Hash(seed, gx + 1, gy, salt));
			float c = Unit(LevelGenerator.Hash(seed, gx, gy + 1, salt));
			float d = Unit(LevelGenerator.Hash(seed, gx + 1, gy + 1, salt));
			float top = a + (b - a) * fx;
			return top + ((c + (d - c) * fx) - top) * fy;
		}

		private static float Unit(ulong h) => (float)((h >> 11) * (1.0 / (1UL << 53)));
	}
}
