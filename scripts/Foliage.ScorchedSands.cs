using System;

namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Scorched Sands (stage_6, StageEnvironmentKind.Desert): the foliage pass (2026-11-03, .ai/terrain.md
	/// section 13, "The Scorched Sands"). The Forest's stands are woods and the Fields' hedgerows; the Sands'
	/// are STONE - outcrops the wind carved, in belts that lie along it, on aprons of their own scree - with
	/// open sand between them, most of the ground, to fight on.
	/// </summary>
	/// <remarks>
	/// "He hid the water. The dead still walk the trade road." Nothing grows thick here, so what masses is
	/// rock. THE LADDER, largest first (tools/art/props_sands.py, detail_sands.py): red-stone tors, a yardang
	/// ridge and a hoodoo in the belts (the only big trunks); dead camelthorns and, rarest, a dead palm on a
	/// belt's fringe, where a wadi ran along the rock; wind-worn boulders on the fringe and now and then alone;
	/// low thornbush and the chapter's old dead scrub on the fringe, straying into the open; and in the detail
	/// layer, sand ripples and drift tails in the open, gravel and bunch grass on the fringe, rock chips on the
	/// scree. All of it on the dark half of flesh and the floor's own gold, within about ten L* of the sand,
	/// where the cast in sand cloth and steel stands well above it.
	///
	/// WHAT STAYS AS IT WAS. The stones and the crystals keep their places in the list (0, 1, 2), so they are
	/// where they were; every older detail rule keeps its index. The dead trees (3) are the camelthorns now,
	/// not the sun-bleached snags - bone-pale wood, hi +55 over this floor, was the nearest thing in the
	/// chapter to the Fulgurite's glass tree, "the one cold thing on a warm floor" - and they and the old
	/// scrub (4) grow by habitat. The caravan's road, the dry tomb with its mirrors and the light's path, and the Fulgurite keep
	/// their footprints clear as every set and clutter rule does (Node2DGame.IsInExploreReserve, trunks by 140
	/// px more). Nothing here is a tell of a planned secret (.ai/stage-secrets.md 6.7): no wheel, no carved
	/// stone in a line, no reed or green, no cracked earth added, nothing that shimmers, no glass.
	/// </remarks>
	private static class ScorchedSands
	{
		private const string Rock1 = "res://assets/bonelight/world/rock-1.png";
		private const string Rock2 = "res://assets/bonelight/world/rock-2.png";
		private const string Manifest = "res://assets/bonelight/world/sands/sands.json";

		// THE FIELD: belts of rock lying along the wind. Value noise on a lattice turned by the wind's angle and
		// stretched along it (32 cells, about 1540 px, by 8 across, about 380 px), so an outcrop belt is a long
		// streak and not a round stand; a second noise at 6 cells, a quarter of the field, ragged at its edge.
		// The ground detail's ripples cross this same angle and its drift tails trail along it
		// (tools/art/detail_sands.py, WIND_ANGLE), so the ground agrees about where the wind comes from.
		// Previewed in a Python port before it was built: at 24 by 9 the belts read as blobs.
		private const float WindAngle = -0.32f;
		private const float BeltAlongCells = 32f;
		private const float BeltAcrossCells = 8f;
		private const float EdgeCells = 6f;
		private const float EdgeWeight = 0.24f;
		private const int SaltBelt = 0x5BE2;
		private const int SaltEdge = 0x5BE3;
		// The third of the chapter's block (0x5BE2-4), held so no other chapter takes it; the field uses two.
		private const int SaltSpare = 0x5BE4;

		private static readonly float WindCos = MathF.Cos(WindAngle);
		private static readonly float WindSin = MathF.Sin(WindAngle);

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Desert,

			// Measured on the field (Woodland.Validate's 70 x 70 sample, three seeds): 8-10% of the ground is an
			// outcrop belt (the scree under it), 74-79% open sand, the rest the belts' fringe. The start's clearing
			// is the Forest's (6 and 16 cells): a run opens on open sand with the nearest belt at the screen's edge.
			Canopy = new CanopyProfile
			{
				Shape = CanopyShape.Own,
				Own = Field,
				ClearingEnd0 = 0.60f,
				ClearingEnd1 = 0.68f,
				StandStart0 = 0.70f,
				StandStart1 = 0.78f,
				StandFloor = 0.74f,
				// The scree an outcrop sheds, under every belt: the palette's own rocky_pit (walkable, not a
				// hazard). No clearing floor: the palette's scree and paving blobs fall where they rolled.
				StandFloorTerrain = "rocky_pit",
				SaltStand = SaltBelt,
				SaltEdge = SaltEdge,
				SaltGap = SaltSpare,
				StandShareMin = 0.05f,
				StandShareMax = 0.14f,
				ClearingShareMin = 0.68f,
				ClearingShareMax = 0.86f,
			},

			Decor = new[]
			{
				// 0. Stones, in small groups, as before.
				new FoliageSet
				{
					Sprites = new[] { Rock1, Rock2 }, MinScale = 0.84f, MaxScale = 0.98f,
					Count = 12, ClumpSize = 3, Clump = ClumpBase.GroundAccent, RadiusMin = 0.86f, RadiusMax = 0.86f,
					Separation = 0.9f, Loners = 0.05f,
				},
				// 1. Crystals, as before.
				new FoliageSet
				{
					Sprites = new[] { "res://assets/bonelight/world/crystal-yellow.png", "res://assets/bonelight/world/crystal-violet.png" },
					MinScale = 0.88f, MaxScale = 1.02f,
					Count = 12, ClumpSize = 3, Clump = ClumpBase.Ruin, RadiusMin = 0.86f, RadiusMax = 0.86f,
					Separation = 0.9f, Loners = 0.06f,
				},
				// 2. Smaller stones, in twos, as before.
				new FoliageSet
				{
					Sprites = new[] { Rock1, Rock2 }, MinScale = 0.58f, MaxScale = 0.78f,
					Count = 8, ClumpSize = 2, Clump = ClumpBase.GroundAccent, RadiusMin = 0.72f, RadiusMax = 0.72f,
					Separation = 0.78f, Loners = 0.04f,
				},
				// 3. The dead trees: camelthorns on a belt's fringe, in twos or alone, and now and then the dead palm
				// (one pick in four). Rarer than the snags they replace: 0.43 a chunk on a walk, where the bleached
				// snags stood 0.82, evenly.
				new FoliageSet
				{
					Grounded = new[] { "camelthorn-1", "camelthorn-2", "camelthorn-1", "dead-palm-1" }, Manifest = Manifest,
					Count = 150, ClumpSize = 2, Clump = ClumpBase.Tree, RadiusMin = 0.4f, RadiusMax = 0.6f, Separation = 0.8f,
					Loners = 0.3f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0.02f, 1f, 0.3f),
				},
				// 4. The old dead bushes and dry scrub, by habitat now: thickest on the fringe, a third as thick out
				// in the open.
				new FoliageSet
				{
					Grounded = new[] { "dead-bush-1", "dead-bush-2", "dry-scrub-1" },
					Count = 420, ClumpSize = 3, Clump = ClumpBase.Bush, RadiusMin = 1.2f, RadiusMax = 1.3f,
					Loners = 0.2f, MinSpacing = 40f, Habitat = new Woodland.Habitat(0.3f, 1f, 0.4f),
				},
				// 5. The outcrops: tors, a yardang ridge and a hoodoo, massed along the belts. The rule that no stand
				// pins the player (Footing) keeps every gap between them at least 72 px, so a belt is a broken line of
				// stone you thread, never a wall.
				new FoliageSet
				{
					Grounded = new[] { "tor-1", "tor-2", "ridge-1", "tor-1", "hoodoo-1" }, Manifest = Manifest,
					Count = 2600, ClumpSize = 2, Clump = ClumpBase.Stand, RadiusMin = 0.35f, RadiusMax = 0.55f, Separation = 0.55f,
					Loners = 0.1f, MinSpacing = 100f, Habitat = new Woodland.Habitat(0f, 0.12f, 1f),
				},
				// 6. Wind-worn boulders on the fringe and among the tors, and once in a long while alone on the sand.
				new FoliageSet
				{
					Grounded = new[] { "boulder-1", "boulder-2" }, Manifest = Manifest,
					Count = 700, ClumpSize = 3, Clump = ClumpBase.Bush, Loners = 0.15f, MinSpacing = 90f,
					Habitat = new Woodland.Habitat(0.03f, 1f, 0.5f),
				},
				// 7. Thornbush, low, on the fringe.
				new FoliageSet
				{
					Grounded = new[] { "thornbush-1", "thornbush-2" }, Manifest = Manifest,
					Count = 1500, ClumpSize = 3, Clump = ClumpBase.Bush, RadiusMin = 0.5f, RadiusMax = 0.7f, Separation = 0.6f,
					Loners = 0.05f, MinSpacing = 36f, Habitat = new Woodland.Habitat(0.04f, 1f, 0.35f),
				},
			},

			// The bottom rungs (tools/art/detail_sands.py), densities the open ground's before the habitat weight
			// (clearing, fringe, stand). Sparse on purpose: the open sand is the chapter, and the old rules already
			// lay dry grass, pebbles, cracked earth, bone and rubble on it. No more of the old dry grass (gold's base
			// at its tips, hi +33 over the sand) or bone (cold and pale, beside the Fulgurite's glass beads).
			Clutter = new[]
			{
				GroundDetail.Clutter("sand_ripples", 0.035f, 6, 1f, 0.2f, 0f),
				GroundDetail.Clutter("drift_tail", 0.02f, 4, 1f, 0.5f, 0f),
				GroundDetail.Clutter("desert_gravel", 0.03f, 5, 0.35f, 1f, 0.5f),
				GroundDetail.Clutter("bunchgrass", 0.035f, 4, 0.1f, 1f, 0.3f),
				GroundDetail.Clutter("rock_chips", 0.04f, 3, 0f, 0.7f, 1f),
				GroundDetail.Clutter("rock_chips", 0.05f, 2, 1f, 1f, 1f, "rocky_pit"),
			},
		};

		/// <summary>The field (CanopyProfile.Own): a pure function of (seed, world cell), in 0..1.</summary>
		private static float Field(ulong seed, float cellX, float cellY)
		{
			float u = cellX * WindCos + cellY * WindSin;
			float v = -cellX * WindSin + cellY * WindCos;
			float belt = Noise(seed, SaltBelt, 0, u / BeltAlongCells, v / BeltAcrossCells);
			float edge = Noise(seed, SaltEdge, 1, cellX / EdgeCells, cellY / EdgeCells);
			return belt * (1f - EdgeWeight) + edge * EdgeWeight;
		}

		// The field is asked per cell (the scree under every padded chunk, the clutter, the decor), and a cell's
		// neighbours almost always share its lattice cell, so each noise keeps its last four corners - the same
		// numbers, computed once (Woodland does the same for its own shapes). One copy per thread.
		[ThreadStatic] private static float[]? corners;
		[ThreadStatic] private static (ulong Seed, int X, int Y, bool Set)[]? cornerKeys;

		private static float Noise(ulong seed, int salt, int slot, float x, float y)
		{
			int gx = (int)MathF.Floor(x);
			int gy = (int)MathF.Floor(y);
			float fx = x - gx, fy = y - gy;
			fx = fx * fx * (3f - 2f * fx);
			fy = fy * fy * (3f - 2f * fy);
			float[] memo = corners ??= new float[8];
			var keys = cornerKeys ??= new (ulong, int, int, bool)[2];
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
