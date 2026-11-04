using System;

namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Broken Circle (stage_10): the high heath on the foliage groundwork (2026-11-04, .ai/terrain.md section
	/// 13, "The Broken Circle"). Its stands are not woods: they are DRIFTS - granite rises combed out along the
	/// wind, crowned with tors and clothed in gorse - and the open moor between them is what the chapter is.
	/// "Heather, granite, and a wind with nothing in its way" (ChapterNotes).
	/// </summary>
	/// <remarks>
	/// THE LADDER, largest first. Tors on a drift's crown, Dartmoor's kind: piles of weathered granite
	/// pillows, leaning and overhanging, clitter spilt down their flanks (nothing tall and single: that is the
	/// Ninth Stone's); wind-cut thorns and boulders on the drift; the standing and fallen stones of the old
	/// circles where they always were, out on the open moor; gorse thickets, the drifts' body; the old gorse
	/// and bramble at their edge; heather clumps where the moor thickens toward them; humps of bedrock breaking
	/// the turf toward the tor; and in the detail layer, moor grass combed east by the wind, bilberry under the
	/// gorse, lichen, and more bedrock and clitter of every size toward the crown. The heather runs on under the
	/// crowns: no floor of slabs. Living things on the dark half of verdant, stone on the granite the standing
	/// stones were drawn in (tools/art/props_heath.py, detail_heath.py).
	///
	/// THE WIND. Every grounded sprite is mirrored at random (Node2DGame.FurnishGroundedSet), so a sprite cannot
	/// lean one way and keep to it: the thorns are wind-CUT, shorn flat and low, which reads either way. The
	/// wind is drawn where nothing is mirrored - the drifts run along it (the field below is stretched three to
	/// one on a bearing a little north of east) and the moor grass in the detail layer all leans east.
	///
	/// WHAT STAYS AS IT WAS. The standing stones, the fallen stones and the rocks keep their places in the list
	/// (0, 1 and 4) and their rolls; the gorse drift (2) and the lone trees (3) keep their index with their
	/// art and their habitat changed. The moot's footprint and the Fallen Star's crater stay clear as every
	/// set and clutter rule does (Node2DGame.IsInExploreReserve), and nothing drawn is a line, a black mark or
	/// a warm fleck: the Fallen Star's tell is a dotted line of char across the heather, glinting (BC1).
	/// </remarks>
	private static class BrokenCircle
	{
		private const string Manifest = "res://assets/bonelight/world/heath/heath.json";

		// The field's three salts: the heath's block (.ai/terrain.md section 13, the recipe).
		private const int SaltDrift = 0x5AD2;
		private const int SaltRag = 0x5AD3;
		private const int SaltChain = 0x5AD4;

		// THE DRIFTS: two octaves stretched along the wind and one round one. The wind's bearing, a little
		// north of east (screen y runs down); a drift runs about 30 cells (1440 px, a PC screen) along it and
		// 10 across, its run broken into a chain of rises by an octave at 12 by 7, its edge made ragged by
		// one at 5. Then the middle is stretched so the tails reach the open moor and the crowns.
		private const float WindBearing = -0.35f;
		private const float DriftAlong = 30f, DriftAcross = 10f;
		private const float ChainAlong = 12f, ChainAcross = 7.2f;
		private const float RagCells = 5f;
		private static readonly float WindCos = MathF.Cos(WindBearing), WindSin = MathF.Sin(WindBearing);

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Heath,

			// Measured on the field (Woodland.Validate's 70 x 70 sample, three seeds): 70-72% of the moor is open,
			// 14-15% is a drift (the stand habitat, over 0.76), the rest the drifts' flanks; the crowns, over
			// StandFloor (0.84), are 7-8%. Tried first at the Forest's thresholds: a quarter of the moor was drift,
			// and a PC screen often showed no open ground across it.
			//
			// NO STAND FLOOR. The heather and the moor grass run on under a drift's crown; its rock is the decor
			// and the detail - outcrops and boulders of very different sizes breaking the turf, thicker toward the
			// tor. The palette's granite outcrop was laid as the crown's floor first (under the whole drift, then
			// under the crown alone), and its slabs, edge to edge across a crown, read as cobbled paving.
			Canopy = new CanopyProfile
			{
				Shape = CanopyShape.Own,
				Own = Drifts,
				ClearingEnd0 = 0.6f,
				ClearingEnd1 = 0.68f,
				StandStart0 = 0.72f,
				StandStart1 = 0.8f,
				StandFloor = 0.84f,
				SaltStand = SaltDrift,
				SaltEdge = SaltRag,
				SaltGap = SaltChain,
				StandShareMin = 0.04f,
				StandShareMax = 0.13f,
				ClearingShareMin = 0.62f,
				ClearingShareMax = 0.8f,
			},

			// A habitat set's Count is its density INSIDE its habitat (per 8400 x 8400 of it).
			Decor = new[]
			{
				// 0. The stones of the old circles, standing in twos and threes - unchanged, out on the open moor.
				new FoliageSet
				{
					Grounded = new[] { "standing-stone-1", "standing-stone-2", "standing-stone-3" }, Manifest = Manifest,
					Count = 70, ClumpSize = 2, Clump = ClumpBase.Tree, RadiusMin = 0.7f, RadiusMax = 0.8f,
					Loners = 0.3f, MinSpacing = YoungTreeSpacing,
				},
				// 1. Fallen stones, apart - unchanged.
				new FoliageSet
				{
					Grounded = new[] { "fallen-stone-1", "fallen-stone-2" }, Manifest = Manifest,
					Count = 40, ClumpSize = 1, Clump = ClumpBase.Tree, Loners = 0.5f, MinSpacing = MatureTreeSpacing,
				},
				// 2. Gorse and the odd bramble, now on a drift's flanks rather than anywhere.
				new FoliageSet
				{
					Grounded = new[] { "gorse-1", "gorse-2", "gorse-1", "bramble-2" }, Manifest = Manifest,
					Count = 900, ClumpSize = 4, Clump = ClumpBase.Bush, RadiusMin = 0.6f, RadiusMax = 0.9f, Separation = 0.7f,
					Loners = 0.06f, MinSpacing = 36f, Habitat = new Woodland.Habitat(0.04f, 1f, 0.5f),
				},
				// 3. Thorns the wind has cut flat, one dead, on the flanks and now and then alone on the moor: the
				// shared dead trees (250 px) were the tallest thing on the heath, and a moor's sightlines are long.
				new FoliageSet
				{
					Grounded = new[] { "windthorn-1", "windthorn-2", "windthorn-1", "windthorn-bare-1" }, Manifest = Manifest,
					Count = 300, ClumpSize = 2, Clump = ClumpBase.Tree, RadiusMin = 0.4f, RadiusMax = 0.6f, Separation = 0.8f,
					Loners = 0.3f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0.03f, 1f, 0.3f),
				},
				// 4. Granite lying about - unchanged.
				new FoliageSet
				{
					Sprites = new[] { "res://assets/bonelight/world/rock-1.png", "res://assets/bonelight/world/rock-2.png" },
					MinScale = 0.86f, MaxScale = 1.06f,
					Count = 50, ClumpSize = 3, Clump = ClumpBase.GroundAccent, Loners = 0.05f,
				},
				// 5. Gorse thickets: the drifts' body, massed on the crown.
				new FoliageSet
				{
					Grounded = new[] { "gorse-thicket-1", "gorse-thicket-2" }, Manifest = Manifest,
					Count = 1300, ClumpSize = 3, Clump = ClumpBase.Stand, RadiusMin = 0.5f, RadiusMax = 0.8f, Separation = 0.6f,
					Loners = 0.02f, MinSpacing = 72f, Habitat = new Woodland.Habitat(0f, 0.45f, 1f),
				},
				// 6. Tors, one to a crown, rarely two.
				new FoliageSet
				{
					Grounded = new[] { "tor-1", "tor-2" }, Manifest = Manifest,
					Count = 240, ClumpSize = 1, Clump = ClumpBase.Tree, Loners = 0.4f, MinSpacing = 240f,
					Habitat = new Woodland.Habitat(0f, 0.05f, 1f),
				},
				// 7. Boulders come off the tors: sparse on a drift's flanks, thicker toward its crown.
				new FoliageSet
				{
					Grounded = new[] { "boulder-1", "boulder-2" }, Manifest = Manifest,
					Count = 420, ClumpSize = 2, Clump = ClumpBase.GroundAccent, RadiusMax = 1.2f, Separation = 1.2f,
					Loners = 0.2f, MinSpacing = 60f, Habitat = new Woodland.Habitat(0.01f, 0.5f, 1f),
				},
				// 8. Heather grown into clumps, where the moor thickens toward a drift.
				new FoliageSet
				{
					Grounded = new[] { "heather-clump-1", "heather-clump-2" }, Manifest = Manifest,
					Count = 700, ClumpSize = 4, Clump = ClumpBase.Bush, RadiusMin = 0.5f, RadiusMax = 0.8f, Separation = 0.6f,
					Loners = 0.1f, MinSpacing = 28f, Habitat = new Woodland.Habitat(0.25f, 1f, 0.4f),
				},
				// 9. Bedrock breaking the turf: low humps of granite, a few on the flanks and more toward the tor.
				// Walked over, like the heather they show through.
				new FoliageSet
				{
					Grounded = new[] { "outcrop-1", "outcrop-2", "outcrop-3", "outcrop-3" }, Manifest = Manifest,
					Count = 1500, ClumpSize = 3, Clump = ClumpBase.GroundAccent, RadiusMax = 1.4f, Separation = 0.9f,
					Loners = 0.06f, MinSpacing = 44f, Habitat = new Woodland.Habitat(0f, 0.25f, 1f),
				},
			},

			// The bottom rungs (tools/art/detail_heath.py). Moor grass combed by the wind across all the open moor;
			// heather thickening and bilberry under the gorse at a drift's edge; lichen in the open; on a drift,
			// humps of bedrock and clitter of every size, few on the flanks and many toward the crown; and on the
			// palette's own granite outcrops, heather between the slabs, clitter and lichen. Not more cotton-grass:
			// its white heads stand +68 L* over the heather at their brightest, above the cast's rims. Densities are
			// the open ground's, before the habitat weight (clearing, fringe, stand).
			Clutter = new[]
			{
				GroundDetail.Clutter("moor_grass", 0.1f, 5, 1f, 0.7f, 0.3f),
				GroundDetail.Clutter("heather_tufts", 0.06f, 4, 0.15f, 1f, 0.5f),
				GroundDetail.Clutter("bilberry", 0.05f, 5, 0.1f, 1f, 0.6f),
				GroundDetail.Clutter("lichen_specks", 0.03f, 6, 1f, 0.5f, 0.2f),
				GroundDetail.Clutter("clitter", 0.07f, 3, 1f, 1f, 1f, "granite"),
				GroundDetail.Clutter("heather_tufts", 0.06f, 4, 1f, 1f, 1f, "granite"),
				GroundDetail.Clutter("lichen_specks", 0.03f, 4, 1f, 1f, 1f, "granite"),
				GroundDetail.Clutter("moor_grass", 0.04f, 5, 1f, 1f, 1f, "peat"),
				GroundDetail.Clutter("rock_hump", 0.07f, 3, 0f, 0.25f, 1f),
				GroundDetail.Clutter("clitter", 0.06f, 3, 0.02f, 0.4f, 1f),
			},
		};

		/// <summary>
		/// The drifts: how far toward a granite rise a point of the moor is, a pure function of (seed, world cell).
		/// Asked per cell by the ground, the detail and the decor, so each octave keeps its last four lattice
		/// corners (Woodland's own field does the same).
		/// </summary>
		private static float Drifts(ulong seed, float x, float y)
		{
			float u = x * WindCos + y * WindSin;
			float v = -x * WindSin + y * WindCos;
			float drift = Noise(seed, SaltDrift, u / DriftAlong, v / DriftAcross, 0);
			float chain = Noise(seed, SaltChain, u / ChainAlong, v / ChainAcross, 1);
			float rag = Noise(seed, SaltRag, x / RagCells, y / RagCells, 2);
			float c = drift * 0.62f + chain * 0.18f + rag * 0.2f;
			return Math.Clamp(0.5f + (c - 0.5f) * 1.6f, 0f, 1f);
		}

		[ThreadStatic] private static float[]? corners;
		[ThreadStatic] private static (ulong Seed, int X, int Y)[]? cornerKeys;

		// Smooth value noise: a hashed value at each lattice point, blended with a smoothstep. `slot` is the
		// octave's own memo of its last lattice cell's four corners.
		private static float Noise(ulong seed, int salt, float x, float y, int slot)
		{
			int gx = (int)MathF.Floor(x), gy = (int)MathF.Floor(y);
			float fx = x - gx, fy = y - gy;
			fx = fx * fx * (3f - 2f * fx);
			fy = fy * fy * (3f - 2f * fy);
			float[] memo = corners ??= new float[12];
			(ulong, int, int)[] keys = cornerKeys ??= new (ulong, int, int)[3];
			int at = 4 * slot;
			var key = (seed, gx, gy);
			if (keys[slot] != key || (memo[at] == 0f && memo[at + 1] == 0f && memo[at + 2] == 0f && memo[at + 3] == 0f))
			{
				memo[at] = Unit(LevelGenerator.Hash(seed, gx, gy, salt));
				memo[at + 1] = Unit(LevelGenerator.Hash(seed, gx + 1, gy, salt));
				memo[at + 2] = Unit(LevelGenerator.Hash(seed, gx, gy + 1, salt));
				memo[at + 3] = Unit(LevelGenerator.Hash(seed, gx + 1, gy + 1, salt));
				keys[slot] = key;
			}
			float a = memo[at], b = memo[at + 1], c = memo[at + 2], d = memo[at + 3];
			float top = a + (b - a) * fx;
			return top + ((c + (d - c) * fx) - top) * fy;
		}

		private static float Unit(ulong h) => (float)((h >> 11) * (1.0 / (1UL << 53)));
	}
}
