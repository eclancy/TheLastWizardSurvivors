using System;

namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Frozen Waste (stage_5, StageEnvironmentKind.Ice): the foliage pass (2026-11-04, .ai/terrain.md section
	/// 13, "The Frozen Waste"). The Forest's stands are woods, the Fields' hedgerows, the Swamp's tree islands
	/// and the Sands' stone; the Waste's are SNOW GHOSTS - groves of spruce buried standing in snow and rime,
	/// lying across the wind - with the open waste between them, most of the ground, to fight on.
	/// </summary>
	/// <remarks>
	/// "A wizard lies under the ice, and the cold stands guard." THE LADDER, largest first
	/// (tools/art/props_frozen.py, detail_frost.py): the snow ghosts in a grove's heart, hunched white columns
	/// with their tops hooked over (crown snow-load: Väinämöinen is under the ice here); snow-laden spruce spires
	/// at a grove's edge, where the load is lighter; dead spruce bare to the stubs and rimed, on the fringe and
	/// now and then alone in the open; frost-split granite half buried in drift, on the fringe and among the
	/// groves; dwarf birch and juniper through the snow; and in the detail layer, frosted stalks out in the open,
	/// half-buried stones on a fringe, needle litter and fallen bough snow under a grove. The snow on all of it
	/// is the bone row's shade in the light and steel's in the shadow - pale things a step over the floor, where
	/// the cast are dark bodies at the floor's own value with a rim, so the two never read alike.
	///
	/// WHAT STAYS AS IT WAS. The stones and the crystals keep their places in the list (0, 1, 2), so they are
	/// where they were; every older detail rule keeps its index (sastrugi, the snow drifts, the frost, the ice
	/// shards). No stand floor: the palette's only blobs are its ice and its open water, and the ice is
	/// avoided by every prop - a grove stands on snow. The thaw's lake, the Buried Runes' drift field and every
	/// other site keep their footprints clear as every set and clutter rule does (Node2DGame.IsInExploreReserve,
	/// trunks by 140 px more), and THE ICE IS NEVER A TARGET (.ai/side-events.md): nothing here is a body an
	/// enemy or a spell can take for it. Nothing here is a tell of the chapter's secrets (.ai/stage-secrets.md
	/// 6.6): no track or footprint (I1), no flat or cut stone and no free-standing drift (I2), nothing warm (I1,
	/// I3), no clear ice (I4), and no stones piled on one another (the bearing cairn, its wayfinder).
	/// </remarks>
	private static class FrozenWaste
	{
		private const string Rock1 = "res://assets/bonelight/world/rock-1.png";
		private const string Rock2 = "res://assets/bonelight/world/rock-2.png";
		private const string Manifest = "res://assets/bonelight/world/frozen/frozen.json";

		// THE FIELD: groves lying ACROSS the wind, each with its lee. The wind is the drift field's
		// (scenes/shaders/drift_field.gdshader, `wind`: the snow blows toward the west-south-west), and the
		// sastrugi lie along it (detail_frost.py). Value noise on a lattice turned to the wind, short along it (12
		// cells, about 580 px) and long across it (22, about 1060 px), so a grove is a little longer across the
		// wind than along it; the same noise read again a few cells upwind and a step weaker, so every grove
		// trails a lee downwind where its fringe runs longer (snow sheltered there, and the dead and the low
		// things with it); and a noise at 5 cells, a quarter of the field, rags its edge. Previewed first in a
		// Python port: at 10 by 26 the groves ran together into streaks, a waste that read as the Sands' belts.
		private const float WindX = -0.97f;
		private const float WindY = 0.24f;
		private const float AlongCells = 12f;
		private const float AcrossCells = 22f;
		private const float LeeCells = 4f;
		private const float LeeStrength = 0.85f;
		private const float EdgeCells = 5f;
		private const float EdgeWeight = 0.26f;
		private const int SaltGrove = 0x5CF2;
		private const int SaltEdge = 0x5CF3;
		// The third of the chapter's block (0x5CF2-4), held so no other chapter takes it; the field uses two.
		private const int SaltSpare = 0x5CF4;

		private static readonly float WindLength = MathF.Sqrt(WindX * WindX + WindY * WindY);
		private static readonly float WindCos = WindX / WindLength;
		private static readonly float WindSin = WindY / WindLength;

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Ice,

			// Measured on the field (Woodland.Validate's 70 x 70 sample, three seeds): 11-13% of the ground is a
			// grove's heart and 67-71% the open waste, the rest the groves' fringes and lees. The start's clearing
			// is the Forest's (6 and 16 cells): a run opens on open snow with the nearest groves at the screen's edge.
			Canopy = new CanopyProfile
			{
				Shape = CanopyShape.Own,
				Own = Field,
				ClearingEnd0 = 0.55f,
				ClearingEnd1 = 0.63f,
				StandStart0 = 0.66f,
				StandStart1 = 0.74f,
				StandFloor = 0.70f,
				SaltStand = SaltGrove,
				SaltEdge = SaltEdge,
				SaltGap = SaltSpare,
				StandShareMin = 0.07f,
				StandShareMax = 0.18f,
				ClearingShareMin = 0.60f,
				ClearingShareMax = 0.80f,
			},

			Decor = new[]
			{
				// 0. Stones, in small groups, as before.
				new FoliageSet
				{
					Sprites = new[] { Rock1, Rock2 }, MinScale = 0.80f, MaxScale = 0.96f,
					Count = 16, ClumpSize = 3, Clump = ClumpBase.GroundAccent, RadiusMin = 1f, RadiusMax = 0.82f,
					Separation = 0.86f, Loners = 0.05f,
				},
				// 1. Crystals, as before.
				new FoliageSet
				{
					Sprites = new[] { "res://assets/bonelight/world/crystal-yellow.png", "res://assets/bonelight/world/crystal-violet.png" },
					MinScale = 0.82f, MaxScale = 0.96f,
					Count = 10, ClumpSize = 3, Clump = ClumpBase.Ruin, RadiusMin = 0.78f, RadiusMax = 0.78f,
					Separation = 0.82f, Loners = 0.06f,
				},
				// 2. Smaller stones, in twos, as before.
				new FoliageSet
				{
					Sprites = new[] { Rock1, Rock2 }, MinScale = 0.54f, MaxScale = 0.74f,
					Count = 6, ClumpSize = 2, Clump = ClumpBase.GroundAccent, RadiusMin = 0.60f, RadiusMax = 0.60f,
					Separation = 0.70f, Loners = 0.04f,
				},
				// 3. The snow ghosts, massed in a grove's heart. Clumped tight (a Stand clump at half to three
				// quarters of its radius), as the Swamp's islands taught: at full radius a clump spills into the open,
				// where the habitat drops it. The rule that no stand pins the player (Footing) keeps every gap a
				// trunk is part of at least 72 px, so a grove is a mass you thread, never a wall.
				new FoliageSet
				{
					Grounded = new[] { "rime-spruce-1", "rime-spruce-2", "rime-spruce-3" }, Manifest = Manifest,
					Count = 7000, ClumpSize = 5, Clump = ClumpBase.Stand, RadiusMin = 0.5f, RadiusMax = 0.75f, Separation = 0.6f,
					Loners = 0.05f, MinSpacing = 96f, Habitat = new Woodland.Habitat(0f, 0.15f, 1f),
				},
				// 4. Snow-laden spruce spires at a grove's edge, where the load is lighter, and now and then among the
				// ghosts - fewer than the ghosts: at as many, a grove photographed as a plantation of dark spires.
				new FoliageSet
				{
					Grounded = new[] { "snow-spruce-1", "snow-spruce-2" }, Manifest = Manifest,
					Count = 700, ClumpSize = 2, Clump = ClumpBase.Stand, RadiusMin = 0.5f, RadiusMax = 0.8f, Separation = 0.7f,
					Loners = 0.15f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0f, 1f, 0.2f),
				},
				// 5. Dead spruce, rimed, on the fringe and the lee, in twos or alone, and now and then out in the open.
				new FoliageSet
				{
					Grounded = new[] { "rimed-snag-1", "rimed-snag-2" }, Manifest = Manifest,
					Count = 320, ClumpSize = 2, Clump = ClumpBase.Tree, RadiusMin = 0.4f, RadiusMax = 0.6f, Separation = 0.8f,
					Loners = 0.4f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0.04f, 1f, 0.15f),
				},
				// 6. Frost-split granite half buried in drift: the fringe and among the groves, and once in a long
				// while alone on the open snow.
				new FoliageSet
				{
					Grounded = new[] { "frost-boulder-1", "frost-boulder-2", "frost-boulder-3", "frost-boulder-2" }, Manifest = Manifest,
					Count = 900, ClumpSize = 2, Clump = ClumpBase.Bush, Loners = 0.2f, MinSpacing = 90f,
					Habitat = new Woodland.Habitat(0.03f, 1f, 0.5f),
				},
				// 7. Dwarf birch and juniper through the snow, low, walked through: the fringe, straying into the open.
				new FoliageSet
				{
					Grounded = new[] { "snow-shrub-1", "snow-shrub-2", "snow-shrub-3" }, Manifest = Manifest,
					Count = 1800, ClumpSize = 3, Clump = ClumpBase.Bush, RadiusMin = 0.5f, RadiusMax = 0.7f, Separation = 0.6f,
					Loners = 0.08f, MinSpacing = 36f, Habitat = new Woodland.Habitat(0.12f, 1f, 0.3f),
				},
			},

			// The bottom rungs (tools/art/detail_frost.py), densities the open ground's before the habitat weight
			// (clearing, fringe, stand). Sparse in the open on purpose: the open snow is the chapter and the old
			// rules already lay sastrugi, drifts, frost and ice shards on it. No more of the old snow drifts (the
			// bone row's base, hi +40 over the floor) or the frost and ice shards (steel's lit, hi +53).
			Clutter = new[]
			{
				GroundDetail.Clutter("frost_stalks", 0.03f, 4, 0.7f, 1f, 0.15f),
				GroundDetail.Clutter("buried_stones", 0.025f, 3, 0.12f, 1f, 0.6f),
				GroundDetail.Clutter("needle_litter", 0.05f, 3, 0f, 0.3f, 1f),
				GroundDetail.Clutter("bough_snow", 0.04f, 3, 0f, 0.25f, 1f),
			},
		};

		/// <summary>The field (CanopyProfile.Own): a pure function of (seed, world cell), in 0..1.</summary>
		private static float Field(ulong seed, float cellX, float cellY)
		{
			float u = cellX * WindCos + cellY * WindSin;    // along the wind, downwind positive
			float v = -cellX * WindSin + cellY * WindCos;   // across it
			float grove = Noise(seed, SaltGrove, 0, u / AlongCells, v / AcrossCells);
			float lee = Noise(seed, SaltGrove, 1, (u - LeeCells) / AlongCells, v / AcrossCells) * LeeStrength;
			float edge = Noise(seed, SaltEdge, 2, cellX / EdgeCells, cellY / EdgeCells);
			return MathF.Max(grove, lee) * (1f - EdgeWeight) + edge * EdgeWeight;
		}

		// The field is asked per cell (the clutter of every padded chunk, the decor), and a cell's neighbours
		// almost always share its lattice cell, so each noise keeps its last four corners - the same numbers,
		// computed once (Woodland does the same for its own shapes). Three slots: the grove, its lee (the same
		// noise read upwind, so its own corners) and the edge. One copy per thread.
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
