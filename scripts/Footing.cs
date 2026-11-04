using System;
using System.Collections.Generic;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// No stand of trees may pin the player: the rule every blocking thing the decor places keeps, and the
/// geometry it is measured in. .ai/terrain.md section 13, "No stand pins the player".
/// </summary>
/// <remarks>
/// THE PROBLEM (the Forest pilot, 2026-11-03). Massed into stands, trunks came close enough to wall the
/// player in: the balance bot walked into a stand and was caught among the trunks, and the busy
/// photograph caught him between an oak's foot and a pond. A trunk blocks only its foot (12-28 px wide, on
/// the ground's physics layer, which the player meets with his Feet - a capsule 56 wide), and two trunks,
/// or a trunk and a shore, closer than that are a wall; a little wider than that, a notch the swarm wedges
/// in. Spacing trunks within one set was never enough: the stand's oaks and its birches are two sets, and
/// nothing kept a birch off an oak, a trunk off the water, or a trunk off its neighbour across a chunk
/// border.
///
/// THE RULE: every trunk keeps <see cref="Clearance"/>, edge to edge, from every other blocking footprint -
/// another trunk of any set in any chunk, water, a wall, a garden fence - and the layer-1 props placed after
/// the trees (rocks, the themed props that block) keep the same from every trunk. Why that is a guarantee and
/// not a tuning: grow every obstacle by the mover's own half-size and the places he can stand are what is
/// left; a trunk's grown footprint is a convex box, and it touches no other grown obstacle, so no chain of
/// touching obstacles - the only thing that can wall a place in or narrow a way out below a body's width -
/// can pass through a trunk. Whatever a stand is, every gap between two of its trunks, or between a trunk
/// and anything else, is open to the player's feet with room for a crowd, so there is always a way out of it.
/// Water, walls and rocks among themselves are what they were before; a stand adds no corner of its own.
///
/// WHY 72. The Feet are 56 wide, so 56 is the least that lets him through; a crowd needs the widest ordinary
/// body to pass with room (the tank is 51 across) and more than <see cref="PropClusters.Gap"/> (48), the
/// notch a body wedges in - so no pair a trunk is part of is ever a prop cluster in the navigation grid
/// either. 72 is the Feet's 56 with 8 px a side, a cell and a half. Against the body, which meets the
/// layer-1 props 29.5 px above the Feet, 72 still leaves 17 px vertically and 31 px across after both are
/// grown (worked in <see cref="Validate"/>).
///
/// The placement that keeps it is Node2DGame.Footing.cs (the trunk plan); this file is the geometry, shared
/// with the census that measures it (_BalanceHarness --footing-census).
/// </remarks>
public static class Footing
{
	/// <summary>Edge to edge, world px: the least gap between a trunk and any other blocking footprint.</summary>
	public const float Clearance = 72f;

	/// <summary>The player's Feet (player.tscn): a capsule 56 wide and 16 deep, its centre 36 px below his origin.</summary>
	public const float FeetWidth = 56f;
	public const float FeetDepth = 16f;
	public const float FeetBelowOrigin = 36f;

	/// <summary>The player's body collider (player.tscn), which meets layer 1 (rocks, props): its size and its
	/// centre from his origin.</summary>
	public static readonly Vector2 BodySize = new(26f, 35f);
	public static readonly Vector2 BodyFromOrigin = new(-3f, 6.5f);

	/// <summary>The depth of a trunk's footprint, world px, ending 2 px below its ground line.</summary>
	public const float TrunkFootDepth = 12f;

	/// <summary>
	/// The trunk's collision box, relative to its body's node (which stands <see cref="WorldSprites.SortLift"/>
	/// above the ground line): Node2DGame.CreateGroundedDecor builds the body from this, and the trunk plan
	/// and the census measure it, so the three cannot disagree.
	/// </summary>
	public static Vector2 TrunkFootCentre => new(0f, WorldSprites.SortLift + 2f - TrunkFootDepth * 0.5f);

	/// <summary>The node position CreateGroundedDecor gives a grounded sprite standing at <paramref name="ground"/>.</summary>
	public static Vector2 GroundedOrigin(Vector2 ground) =>
		new(Mathf.Round(ground.X), Mathf.Round(ground.Y - WorldSprites.SortLift));

	/// <summary>A trunk's footprint in world space, standing at <paramref name="ground"/>.</summary>
	public static Rect2 TrunkRect(Vector2 ground, int trunkWidth)
	{
		Vector2 centre = GroundedOrigin(ground) + TrunkFootCentre;
		var size = new Vector2(trunkWidth, TrunkFootDepth);
		return new Rect2(centre - size * 0.5f, size);
	}

	/// <summary>
	/// A decor prop's collision shape (a rock, a crystal: Node2DGame.CreateDecorPropNode builds its body from
	/// this), its centre relative to the node and its size, for a texture of this size at its scale.
	/// </summary>
	public static (Vector2 Centre, Vector2 Size) DecorPropShape(Vector2 texSize) =>
		(new Vector2(0f, texSize.Y * 0.22f), new Vector2(Mathf.Max(8f, texSize.X * 0.42f), Mathf.Max(6f, texSize.Y * 0.24f)));

	/// <summary>A themed prop's collision shape, when it is an obstacle (Node2DGame.CreateCuratedPropNode).</summary>
	public static (Vector2 Centre, Vector2 Size) CuratedPropShape(Vector2 texSize) =>
		(new Vector2(0f, texSize.Y * 0.20f), new Vector2(Mathf.Max(8f, texSize.X * 0.55f), Mathf.Max(6f, texSize.Y * 0.34f)));

	/// <summary>A shape's box in world space, for a node at <paramref name="position"/>.</summary>
	public static Rect2 Box(Vector2 position, (Vector2 Centre, Vector2 Size) shape) =>
		new(position + shape.Centre - shape.Size * 0.5f, shape.Size);

	/// <summary>Edge-to-edge distance between two boxes; zero when they touch or overlap.</summary>
	public static float Gap(Rect2 a, Rect2 b) => PropClusters.RectGap(a, b);

	/// <summary>True if a trunk's footprint keeps the clearance from every box in <paramref name="others"/>.</summary>
	public static bool Clears(Rect2 trunk, List<Rect2> others)
	{
		foreach (Rect2 other in others)
		{
			if (Gap(trunk, other) < Clearance)
				return false;
		}
		return true;
	}

	/// <summary>
	/// True if a box keeps the clearance from every blocking CELL of the ground round it (water, wall,
	/// fence): <paramref name="solid"/> answers for a world cell, whose corner is at
	/// <paramref name="origin"/> + cell * <paramref name="tile"/>. A whole cell, although a shore's body
	/// covers only part of its cell: routing a trunk a little wider of a shore costs nothing.
	/// </summary>
	public static bool ClearsGround(Rect2 box, Vector2 origin, float tile, Func<int, int, bool> solid)
	{
		Rect2 reach = box.Grow(Clearance);
		int x0 = Mathf.FloorToInt((reach.Position.X - origin.X) / tile);
		int y0 = Mathf.FloorToInt((reach.Position.Y - origin.Y) / tile);
		int x1 = Mathf.FloorToInt((reach.End.X - origin.X) / tile);
		int y1 = Mathf.FloorToInt((reach.End.Y - origin.Y) / tile);
		for (int y = y0; y <= y1; y++)
		{
			for (int x = x0; x <= x1; x++)
			{
				if (!solid(x, y))
					continue;
				var cell = new Rect2(origin + new Vector2(x, y) * tile, new Vector2(tile, tile));
				if (Gap(box, cell) < Clearance)
					return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Where the player's Feet centre may NOT be, for a box on the ground's layer (4: a trunk, water, a fence):
	/// the box grown by the Feet's half-size. A box on layer 1 (a rock) meets his body instead, which stands
	/// 29.5 px above the Feet: <see cref="BodyForbids"/>.
	/// </summary>
	public static Rect2 FeetForbids(Rect2 box) => box.GrowIndividual(FeetWidth * 0.5f, FeetDepth * 0.5f, FeetWidth * 0.5f, FeetDepth * 0.5f);

	/// <summary>Where the Feet centre may not be because the body would overlap this layer-1 box.</summary>
	public static Rect2 BodyForbids(Rect2 box)
	{
		Vector2 feetCentre = new(0f, FeetBelowOrigin);
		Vector2 bodyFromFeet = BodyFromOrigin - feetCentre;
		Rect2 grown = box.GrowIndividual(BodySize.X * 0.5f, BodySize.Y * 0.5f, BodySize.X * 0.5f, BodySize.Y * 0.5f);
		return new Rect2(grown.Position - bodyFromFeet, grown.Size);
	}

	/// <summary>
	/// RegressionChecks: the clearance passes the Feet and the widest ordinary body with room, beats the
	/// prop-cluster notch, and leaves a positive gap in the player's own terms against the ground's layer
	/// and against the body's - whichever way round a trunk and a rock stand. Then the rule itself on a
	/// crowded synthetic stand: candidates kept only when they clear, and every pair kept is clear.
	/// </summary>
	public static void Validate(List<string> errors)
	{
		if (Clearance <= FeetWidth)
			errors.Add($"Footing: the clearance ({Clearance}) does not pass the player's Feet ({FeetWidth} wide).");
		if (Clearance <= PropClusters.Gap)
			errors.Add($"Footing: the clearance ({Clearance}) is inside a prop cluster's notch ({PropClusters.Gap}).");

		// Two boxes the clearance apart, in the four directions: grown into the Feet's terms (both layer 4,
		// or a trunk and a rock), they must still leave a gap.
		var trunk = new Rect2(-14f, -6f, 28f, 12f);
		foreach (Vector2 dir in new[] { Vector2.Right, Vector2.Left, Vector2.Up, Vector2.Down })
		{
			Rect2 other = new(trunk.Position + dir * (Clearance + (dir.X != 0 ? trunk.Size.X : trunk.Size.Y)), trunk.Size);
			if (FeetForbids(trunk).Intersects(FeetForbids(other)))
				errors.Add($"Footing: two trunks {Clearance} apart ({dir}) still meet in the Feet's terms.");
			if (FeetForbids(trunk).Intersects(BodyForbids(other)))
				errors.Add($"Footing: a trunk and a rock {Clearance} apart ({dir}) still meet in the player's terms.");
		}

		// The rule on a synthetic stand: 600 candidate trunks in a 1200 px square, kept only if they clear
		// every kept one. Every kept pair must then clear, and the stand must still be a stand.
		var rng = new LevelGenerator.Rng(0xF007);
		var kept = new List<Rect2>();
		for (int i = 0; i < 600; i++)
		{
			var ground = new Vector2(rng.NextFloat() * 1200f, rng.NextFloat() * 1200f);
			Rect2 box = TrunkRect(ground, 12 + rng.Next(17));
			if (Clears(box, kept))
				kept.Add(box);
		}
		for (int a = 0; a < kept.Count; a++)
			for (int b = a + 1; b < kept.Count; b++)
				if (Gap(kept[a], kept[b]) < Clearance)
				{
					errors.Add($"Footing: the synthetic stand kept two trunks {Gap(kept[a], kept[b]):0.0} px apart.");
					return;
				}
		if (kept.Count < 60)
			errors.Add($"Footing: the synthetic stand kept only {kept.Count} trunks of 600 - the clearance would empty a wood.");
	}
}

/// <summary>One member of a chunk's trunk plan: which decor set, which sprite, where it stands, and its trunk.</summary>
public readonly record struct PlannedGrounded(int Set, GroundedSprite Art, Vector2 Ground, bool FlipH, Rect2 Trunk)
{
	public bool Blocks => Art.Trunk > 0;
}

/// <summary>
/// What a chunk's decor sets that stand trunks will place (Node2DGame.Footing.cs), kept with the chunk's data
/// like its hazard plan: a pure function of the stage seed and the ground, worked out once, because more than
/// the chunk itself reads it - its later neighbours give way to its trunks, and every prop round it that
/// blocks keeps clear of them.
/// </summary>
public sealed class GroundedPlan
{
	/// <summary>Every member the chunk's own rules keep - habitat, ground, landmarks, spacing, and the clearance
	/// from its own trunks and from the blocking ground - before it gives way to its neighbours.</summary>
	public readonly List<PlannedGrounded> Raw = new();
	/// <summary>What the chunk places: the raw plan less any trunk within the clearance of a trunk in an
	/// earlier neighbour's raw plan. Null until somebody asks.</summary>
	public List<PlannedGrounded>? Kept;
	/// <summary>Per set index, for --terrain-log: rolled, off habitat, refused, crowded, cleared out, gave way.</summary>
	public readonly Dictionary<int, int[]> Tally = new();
}
