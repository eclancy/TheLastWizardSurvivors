using System;
using System.Collections.Generic;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// Where a chapter's growth stands thick and where it opens out: one smooth field over the world, read by
/// the ground, the ground detail and the decor alike. The SHAPE and the THRESHOLDS are the chapter's
/// (<see cref="CanopyProfile"/>, held in its Foliage.&lt;Chapter&gt;.cs); the field itself is here.
/// .ai/terrain.md section 13.
/// </summary>
/// <remarks>
/// WHY ONE FIELD (the foliage pass, .ai/genre-lessons.md section 2). The Forest read flat: trees and
/// bushes scattered evenly, at the same density everywhere, on a turf whose only variation was a few
/// random blotches. A wood is not even. Its trees mass into dark stands with clearings between them,
/// the ground under a stand is moss and leaf litter, and the edge of a stand - where the light gets in -
/// is where the bushes, the young trees and the long grass are. That is one quantity, how closed the
/// canopy is, and every layer that dresses the ground reads it from here, so the moss, the trunks, the
/// thickets and the grass all agree about where the edge of a stand is. Other chapters' "stands" are not
/// woods (the Fallow Fields' are hedgerows), so the field has a shape per chapter, but the three habitats
/// it is read as - clearing, fringe, stand - are the same everywhere.
///
/// A PURE FUNCTION OF (seed, world cell). Value noise and cell noise on lattices hashed in world cells,
/// like GroundDetail's clustering, so any chunk, in any order, on any visit, gets the same answer
/// (.ai/terrain.md section 7, the seam rule).
///
/// THE RUN OPENS IN THE OPEN. Within SpawnClearingCells of the start the field is pressed down to the
/// clearing's range, and it is itself again by SpawnClearingFadeCells (768 px, about half a PC's view), so
/// the first minute is fought in the open with the growth at the screen's edges. A first try pressed it
/// out to 28 cells, and the opening screen was one wide, empty clearing: flatter than before the pass.
/// </remarks>
public static class Woodland
{
	/// <summary>
	/// How closed the growth is at a point in world cells (fractional; cell (0, 0)'s corner is the start):
	/// near 0 wide open ground, over the profile's <see cref="CanopyProfile.StandFloor"/> a stand.
	/// </summary>
	public static float Canopy(CanopyProfile p, ulong seed, float cellX, float cellY)
	{
		float c = p.Shape switch
		{
			CanopyShape.Hedgerows => Hedgerows(p, seed, cellX, cellY),
			CanopyShape.Own => p.Own!(seed, cellX, cellY),
			_ => Stands(p, seed, cellX, cellY),
		};
		float d = MathF.Sqrt(cellX * cellX + cellY * cellY);
		if (d < p.SpawnClearingFadeCells)
			c *= 0.5f + 0.5f * Smooth(p.SpawnClearingCells, p.SpawnClearingFadeCells, d);
		return c;
	}

	/// <summary>The canopy at the middle of a world cell.</summary>
	public static float CanopyAtCell(CanopyProfile p, ulong seed, int cellX, int cellY) =>
		Canopy(p, seed, cellX + 0.5f, cellY + 0.5f);

	/// <summary>Open ground, away from any stand: 1 in a clearing, 0 at a stand's fringe and inside it.</summary>
	public static float Clearing(CanopyProfile p, float canopy) => 1f - Smooth(p.ClearingEnd0, p.ClearingEnd1, canopy);

	/// <summary>Inside a stand: where the mature trees are (or a hedge's thick middle).</summary>
	public static float Stand(CanopyProfile p, float canopy) => Smooth(p.StandStart0, p.StandStart1, canopy);

	/// <summary>The edge of a stand, where the light gets in: bushes, young trees, long grass.</summary>
	public static float Fringe(CanopyProfile p, float canopy) => 1f - Clearing(p, canopy) - Stand(p, canopy);

	/// <summary>
	/// How a thing that grows in all three habitats, at these strengths, takes to a point: the mix of
	/// its three weights by how much of each habitat the point is.
	/// </summary>
	public static float Weight(CanopyProfile p, in Habitat h, float canopy) =>
		h.Clearing * Clearing(p, canopy) + h.Fringe * Fringe(p, canopy) + h.Stand * Stand(p, canopy);

	/// <summary>Where a set grows, as a weight (0..1) in each habitat.</summary>
	public readonly record struct Habitat(float Clearing, float Fringe, float Stand)
	{
		public float Max => MathF.Max(Clearing, MathF.Max(Fringe, Stand));
	}

	// --- The shapes -------------------------------------------------------------------------------

	// STANDS (the Enchanted Forest): two octaves of value noise, one at StandLattice cells, which decides
	// where a stand is (a stand is a screen or two across - big enough to read as a wood, small enough that
	// a screen usually shows a clearing framed by one), and one at EdgeLattice cells, which makes the edge
	// ragged. Its numbers are the pilot's exactly, which is what lets the Forest come out as it was.
	private static float Stands(CanopyProfile p, ulong seed, float cellX, float cellY)
	{
		float stand = Noise(seed, p.SaltStand, cellX / p.StandLattice, cellY / p.StandLattice);
		float edge = Noise(seed, p.SaltEdge, cellX / p.EdgeLattice, cellY / p.EdgeLattice);
		return stand * (1f - p.EdgeWeight) + edge * p.EdgeWeight;
	}

	// HEDGEROWS (the Fallow Fields): the borders of fields. The land is cut into fields round one centre
	// each, a lattice of FieldCells jittered by FieldJitter of a cell, and the hedge is the line between
	// the two nearest centres - the exact distance to their bisector, so a hedge is the same width all
	// along it. 1 on the line, falling to 0 HedgeHalfWidth cells out, its edge made ragged by a value noise
	// at EdgeLattice (EdgeWeight cells of wander), and broken where a third noise, at GapLattice, falls
	// under GapShare: a fallow land's hedges have gone to gaps nobody mends.
	private static float Hedgerows(CanopyProfile p, ulong seed, float cellX, float cellY)
	{
		float gx = cellX / p.FieldCells, gy = cellY / p.FieldCells;
		int ix = (int)MathF.Floor(gx), iy = (int)MathF.Floor(gy);
		float d1 = float.MaxValue, d2 = float.MaxValue;
		float ax = 0f, ay = 0f, bx = 0f, by = 0f;
		for (int j = -1; j <= 1; j++)
		{
			for (int i = -1; i <= 1; i++)
			{
				ulong h = LevelGenerator.Hash(seed, ix + i, iy + j, p.SaltStand);
				float px = ix + i + 0.5f + (Unit(h) - 0.5f) * p.FieldJitter;
				float py = iy + j + 0.5f + (Unit(LevelGenerator.Mix(h)) - 0.5f) * p.FieldJitter;
				float d = (px - gx) * (px - gx) + (py - gy) * (py - gy);
				if (d < d1)
				{
					d2 = d1; bx = ax; by = ay;
					d1 = d; ax = px; ay = py;
				}
				else if (d < d2)
				{
					d2 = d; bx = px; by = py;
				}
			}
		}
		float sep = MathF.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
		float toBorder = sep > 1e-4f ? (d2 - d1) / (2f * sep) * p.FieldCells : 0f;
		float rag = (Noise(seed, p.SaltEdge, cellX / p.EdgeLattice, cellY / p.EdgeLattice) - 0.5f) * 2f * p.EdgeWeight;
		float hedge = 1f - Smooth(0f, p.HedgeHalfWidth, MathF.Max(0f, toBorder + rag));
		float present = Smooth(p.GapShare - 0.08f, p.GapShare + 0.08f, Noise(seed, p.SaltGap, cellX / p.GapLattice, cellY / p.GapLattice));
		return hedge * present;
	}

	/// <summary>
	/// RegressionChecks, for every chapter with a canopy (Foliage): the three habitats are a partition of
	/// one; the field stays in 0..1 and is the same when asked twice; the shares of stand and clearing on
	/// three seeds stay in the ranges the chapter's profile was tuned to; a chapter that lays a stand floor
	/// or sorts its blobs names terrains it has; and the start is never inside a stand.
	/// </summary>
	public static void Validate(List<string> warnings)
	{
		foreach (ChapterFoliage chapter in Foliage.All)
		{
			if (chapter.Canopy is not CanopyProfile p)
				continue;
			string who = $"Woodland ({chapter.Kind})";
			if (p.Shape == CanopyShape.Own && p.Own == null)
			{
				warnings.Add($"{who}: the shape is Own but the profile gives no field function.");
				continue;
			}
			for (int i = 0; i <= 100; i++)
			{
				float c = i / 100f;
				float clearing = Clearing(p, c), fringe = Fringe(p, c), stand = Stand(p, c);
				if (clearing < 0f || fringe < -1e-4f || stand < 0f || MathF.Abs(clearing + fringe + stand - 1f) > 1e-4f)
				{
					warnings.Add($"{who}: the habitats are not a partition of one at canopy {c:0.00} "
						+ $"(clearing {clearing:0.000}, fringe {fringe:0.000}, stand {stand:0.000}). The clearing must end before the stand starts.");
					break;
				}
			}

			foreach (ulong seed in new ulong[] { 4242, 31, 7 })
			{
				const int samples = 70, step = 6, offset = 300;
				int standCells = 0, clearingCells = 0;
				bool reported = false;
				for (int y = 0; y < samples; y++)
					for (int x = 0; x < samples; x++)
					{
						float c = CanopyAtCell(p, seed, offset + x * step, offset + y * step);
						if (!reported && (c < 0f || c > 1f || float.IsNaN(c) || c != CanopyAtCell(p, seed, offset + x * step, offset + y * step)))
						{
							warnings.Add($"{who}: the field gives {c} at a cell - it must be in 0..1 and the same every time it is asked.");
							reported = true;
						}
						if (c > p.StandFloor) standCells++;
						if (Clearing(p, c) > 0.5f) clearingCells++;
					}
				float standShare = standCells / (float)(samples * samples);
				float clearingShare = clearingCells / (float)(samples * samples);
				if (standShare < p.StandShareMin || standShare > p.StandShareMax
					|| clearingShare < p.ClearingShareMin || clearingShare > p.ClearingShareMax)
					warnings.Add($"{who}: seed {seed} gives {standShare:P0} stand and {clearingShare:P0} clearing, outside the "
						+ $"{p.StandShareMin:P0}-{p.StandShareMax:P0} and {p.ClearingShareMin:P0}-{p.ClearingShareMax:P0} its profile was "
						+ "tuned to. A changed lattice or threshold?");

				for (int k = 0; k < 32; k++)
				{
					Vector2 at = Vector2.FromAngle(k * MathF.Tau / 32f) * p.SpawnClearingCells;
					if (Canopy(p, seed, at.X, at.Y) > p.StandFloor || Canopy(p, seed, at.X * 0.5f, at.Y * 0.5f) > p.StandFloor)
					{
						warnings.Add($"{who}: seed {seed} puts a stand within {p.SpawnClearingCells} cells of the start.");
						break;
					}
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

/// <summary>Which field a chapter's canopy is (<see cref="Woodland"/>).</summary>
public enum CanopyShape
{
	/// <summary>Massed stands with clearings between: two octaves of value noise (the Enchanted Forest).</summary>
	Stands,
	/// <summary>A network of hedges round fields, with gaps (the Fallow Fields).</summary>
	Hedgerows,
	/// <summary>The chapter's own field function (<see cref="CanopyProfile.Own"/>), in its own file.</summary>
	Own,
}

/// <summary>
/// One chapter's canopy: the shape of its field and the thresholds it is read by. Held in the chapter's
/// Foliage.&lt;Chapter&gt;.cs; every default is the Enchanted Forest's, so a profile names only what differs.
/// .ai/terrain.md section 13, "Adding a chapter".
/// </summary>
public sealed record CanopyProfile
{
	public CanopyShape Shape { get; init; } = CanopyShape.Stands;

	// Stands: the two octaves.
	/// <summary>Cells between the stand octave's lattice points: 18 is about 860 px, a stand of 1-2 screens.</summary>
	public int StandLattice { get; init; } = 18;
	/// <summary>Cells between the edge octave's lattice points (Stands), or the raggedness noise's (Hedgerows).</summary>
	public int EdgeLattice { get; init; } = 7;
	/// <summary>Stands: the edge octave's share of the field. Hedgerows: how far, in cells, the edge wanders.</summary>
	public float EdgeWeight { get; init; } = 0.3f;

	// Hedgerows: the fields and their borders.
	/// <summary>Cells between the centres of two fields.</summary>
	public int FieldCells { get; init; } = 26;
	/// <summary>How far, in lattice cells, a field's centre wanders from its lattice point (0..0.8).</summary>
	public float FieldJitter { get; init; } = 0.75f;
	/// <summary>Cells from a hedge's line to where the field is open.</summary>
	public float HedgeHalfWidth { get; init; } = 4f;
	/// <summary>Cells between the gap noise's lattice points: how long a hedge runs before it breaks.</summary>
	public int GapLattice { get; init; } = 7;
	/// <summary>About this share of the hedges' length is gap.</summary>
	public float GapShare { get; init; } = 0.22f;

	/// <summary>Own: the chapter's field, a PURE function of (seed, world cell x, y) into 0..1. Called per cell
	/// by the ground, the detail and the decor, so it must be cheap; Woodland.Validate holds its range.</summary>
	public Func<ulong, float, float, float>? Own { get; init; }

	// The three habitats, a partition of one: the clearing must end before the stand starts.
	public float ClearingEnd0 { get; init; } = 0.47f;
	public float ClearingEnd1 { get; init; } = 0.55f;
	public float StandStart0 { get; init; } = 0.58f;
	public float StandStart1 { get; init; } = 0.66f;

	/// <summary>Canopy over this is a stand: the ground under it is <see cref="StandFloorTerrain"/>.</summary>
	public float StandFloor { get; init; } = 0.625f;
	/// <summary>The walkable terrain laid under every stand before the blobs (LevelGenerator.StampStandFloors),
	/// or null for none. Must be one of the palette's blob terrains, so its tiles are built.</summary>
	public string? StandFloorTerrain { get; init; }
	/// <summary>With <see cref="StandFloorTerrain"/>, the palette's blobs of these two are sorted to where they
	/// grow: one of this terrain in a stand becomes the stand's floor, one of the stand's floor out in the open
	/// becomes this (LevelGenerator.SortedForStands). Null: blobs fall where they roll.</summary>
	public string? ClearingFloorTerrain { get; init; }
	/// <summary>A clearing blob centred within this much canopy of the stand floor becomes the stand's floor too.</summary>
	public float StandBlobMargin { get; init; } = 0.04f;

	/// <summary>The start's clearing, in cells from world tile (0, 0): fully open inside the first, ramping back by the second.</summary>
	public float SpawnClearingCells { get; init; } = 6f;
	public float SpawnClearingFadeCells { get; init; } = 16f;

	/// <summary>Salts for the field's noises. A chapter keeps its own, so two chapters never share a field.</summary>
	public int SaltStand { get; init; } = 0x57A2;
	public int SaltEdge { get; init; } = 0x57A3;
	public int SaltGap { get; init; } = 0x57A4;

	/// <summary>The shares Woodland.Validate holds on three seeds (stand: canopy over the stand floor;
	/// clearing: more clearing than not). Set them from a measurement, not a guess.</summary>
	public float StandShareMin { get; init; } = 0.18f;
	public float StandShareMax { get; init; } = 0.38f;
	public float ClearingShareMin { get; init; } = 0.35f;
	public float ClearingShareMax { get; init; } = 0.65f;
}
