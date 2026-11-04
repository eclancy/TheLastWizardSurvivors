using System;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// Where the Enchanted Forest's trees stand thick and where it opens into clearings: one smooth field
/// over the world, read by the ground, the ground detail and the decor alike. .ai/terrain.md section 13.
/// </summary>
/// <remarks>
/// WHY ONE FIELD (the foliage pass, .ai/genre-lessons.md section 2). The Forest read flat: trees and
/// bushes scattered evenly, at the same density everywhere, on a turf whose only variation was a few
/// random blotches. A wood is not even. Its trees mass into dark stands with clearings between them,
/// the ground under a stand is moss and leaf litter, and the edge of a stand - where the light gets in -
/// is where the bushes, the young trees and the long grass are. That is one quantity, how closed the
/// canopy is, and every layer that dresses the ground reads it from here, so the moss, the trunks, the
/// thickets and the grass all agree about where the edge of a stand is.
///
/// A PURE FUNCTION OF (seed, world cell). Value noise on lattices hashed in world cells, like
/// GroundDetail's clustering, so any chunk, in any order, on any visit, gets the same answer
/// (.ai/terrain.md section 7, the seam rule). Two octaves: one at StandLattice cells, which decides
/// where a stand is (a stand is a screen or two across - big enough to read as a wood, small enough
/// that a screen in the Forest usually shows a clearing framed by one), and one at EdgeLattice cells,
/// which makes the edge ragged.
///
/// THE RUN OPENS IN A CLEARING. Within SpawnClearingCells of the start the field is pressed down to the
/// clearing's range, and it is itself again by SpawnClearingFadeCells (768 px, about half a PC's view), so
/// the first minute is fought in the open with the wood at the screen's edges. A first try pressed it out
/// to 28 cells, and the opening screen was one wide, empty clearing: flatter than before the pass.
///
/// THE SHARES (measured on the distribution of this field, not guessed): about 28% of the ground is
/// stand (canopy over StandFloor: the moss floor), 20% is fringe, half is clearing.
/// </remarks>
public static class Woodland
{
	/// <summary>Cells between the stand octave's lattice points: about 860 px, so a stand is 1-2 screens.</summary>
	public const int StandLattice = 18;
	/// <summary>Cells between the edge octave's lattice points.</summary>
	public const int EdgeLattice = 7;
	private const float EdgeWeight = 0.3f;

	/// <summary>Canopy over this is a stand: the ground under it is moss (LevelGenerator.StampStandFloors).</summary>
	public const float StandFloor = 0.625f;

	// The three habitats, a partition of one: Clearing + Fringe + Stand == 1 everywhere.
	private const float ClearingEnd0 = 0.47f, ClearingEnd1 = 0.55f;
	private const float StandStart0 = 0.58f, StandStart1 = 0.66f;

	/// <summary>The start's clearing, in cells from world tile (0, 0): fully open inside the first, ramping back by the second.</summary>
	public const float SpawnClearingCells = 6f;
	public const float SpawnClearingFadeCells = 16f;

	private const int SaltStand = 0x57A2;
	private const int SaltEdge = 0x57A3;

	/// <summary>
	/// How closed the canopy is at a point in world cells (fractional; cell (0, 0)'s corner is the start):
	/// near 0 a wide clearing, over <see cref="StandFloor"/> a stand.
	/// </summary>
	public static float Canopy(ulong seed, float cellX, float cellY)
	{
		float stand = Noise(seed, SaltStand, cellX / StandLattice, cellY / StandLattice);
		float edge = Noise(seed, SaltEdge, cellX / EdgeLattice, cellY / EdgeLattice);
		float c = stand * (1f - EdgeWeight) + edge * EdgeWeight;
		float d = MathF.Sqrt(cellX * cellX + cellY * cellY);
		if (d < SpawnClearingFadeCells)
			c *= 0.5f + 0.5f * Smooth(SpawnClearingCells, SpawnClearingFadeCells, d);
		return c;
	}

	/// <summary>The canopy at the middle of a world cell.</summary>
	public static float CanopyAtCell(ulong seed, int cellX, int cellY) => Canopy(seed, cellX + 0.5f, cellY + 0.5f);

	/// <summary>Open ground, away from any stand: 1 in a clearing, 0 at a stand's fringe and inside it.</summary>
	public static float Clearing(float canopy) => 1f - Smooth(ClearingEnd0, ClearingEnd1, canopy);

	/// <summary>Inside a stand: where the mature trees are.</summary>
	public static float Stand(float canopy) => Smooth(StandStart0, StandStart1, canopy);

	/// <summary>The edge of a stand, where the light gets in: bushes, young trees, long grass.</summary>
	public static float Fringe(float canopy) => 1f - Clearing(canopy) - Stand(canopy);

	/// <summary>
	/// How a thing that grows in all three habitats, at these strengths, takes to a point: the mix of
	/// its three weights by how much of each habitat the point is.
	/// </summary>
	public static float Weight(in Habitat h, float canopy) =>
		h.Clearing * Clearing(canopy) + h.Fringe * Fringe(canopy) + h.Stand * Stand(canopy);

	/// <summary>Where a set grows, as a weight (0..1) in each habitat.</summary>
	public readonly record struct Habitat(float Clearing, float Fringe, float Stand)
	{
		public float Max => MathF.Max(Clearing, MathF.Max(Fringe, Stand));
	}

	/// <summary>
	/// RegressionChecks: the three habitats are a partition of one; the shares of stand and clearing on
	/// three seeds stay near the ones this field was tuned to (.ai/terrain.md section 13: about 28% stand,
	/// half clearing); and the start is never inside a stand.
	/// </summary>
	public static void Validate(System.Collections.Generic.List<string> warnings)
	{
		for (int i = 0; i <= 100; i++)
		{
			float c = i / 100f;
			float clearing = Clearing(c), fringe = Fringe(c), stand = Stand(c);
			if (clearing < 0f || fringe < -1e-4f || stand < 0f || MathF.Abs(clearing + fringe + stand - 1f) > 1e-4f)
			{
				warnings.Add($"Woodland: the habitats are not a partition of one at canopy {c:0.00} "
					+ $"(clearing {clearing:0.000}, fringe {fringe:0.000}, stand {stand:0.000}).");
				break;
			}
		}

		foreach (ulong seed in new ulong[] { 4242, 31, 7 })
		{
			const int samples = 70, step = 6, offset = 300;
			int standCells = 0, clearingCells = 0;
			for (int y = 0; y < samples; y++)
				for (int x = 0; x < samples; x++)
				{
					float c = CanopyAtCell(seed, offset + x * step, offset + y * step);
					if (c > StandFloor) standCells++;
					if (Clearing(c) > 0.5f) clearingCells++;
				}
			float standShare = standCells / (float)(samples * samples);
			float clearingShare = clearingCells / (float)(samples * samples);
			if (standShare < 0.18f || standShare > 0.38f || clearingShare < 0.35f || clearingShare > 0.65f)
				warnings.Add($"Woodland: seed {seed} gives {standShare:P0} stand and {clearingShare:P0} clearing, "
					+ "outside the 18-38% and 35-65% the Forest was tuned to. A changed lattice or threshold?");

			for (int k = 0; k < 32; k++)
			{
				Vector2 at = Vector2.FromAngle(k * MathF.Tau / 32f) * SpawnClearingCells;
				if (Canopy(seed, at.X, at.Y) > StandFloor || Canopy(seed, at.X * 0.5f, at.Y * 0.5f) > StandFloor)
				{
					warnings.Add($"Woodland: seed {seed} puts a stand within {SpawnClearingCells} cells of the start.");
					break;
				}
			}
		}
	}

	private static float Smooth(float e0, float e1, float x)
	{
		float t = Math.Clamp((x - e0) / (e1 - e0), 0f, 1f);
		return t * t * (3f - 2f * t);
	}

	// Smooth value noise: a hashed value at each lattice point, blended with a smoothstep.
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
