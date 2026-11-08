using System;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// The cold light a gaol lantern throws ahead of its bearer (.ai/unlock-puzzles.md 3.8, GaolersKeyEvent): a cone on the
/// floor, stopped by the walls. One per bearer, aimed every frame it matters; the planner and the validator aim their own.
/// </summary>
/// <remarks>
/// WHAT IS DRAWN IS WHAT CATCHES (CLAUDE.md, "Enemy attack patterns"). The cone's shape is a
/// <see cref="GroundSlamAttack"/> with <see cref="TelegraphShape.Cone"/>, and <see cref="GroundSlamAttack.Contains"/> is
/// the single authority on it: <see cref="Covers"/> asks it first and never second-guesses it. The walls are the only
/// thing laid on top, and they are laid on ONCE, as a reach table - <see cref="Rays"/> rays fanned edge to edge across
/// the cone, each marched out until it meets a wall (<see cref="Aim"/>). The light layer draws that fan, ray end to ray
/// end; <see cref="Covers"/> reads the same table, taking the SHORTER of the two rays either side of the point. So a
/// point is caught only inside what is drawn - and where a wall's corner falls between two rays, the drawn fan reaches a
/// sliver further than the catch (at most one ray's gap, 2.5 degrees: 13 px at the far end). The sliver is the player's.
///
/// Rays are marched at <see cref="MarchStep"/>, a quarter of a 48 px wall tile, so no wall is stepped over; a ray stops
/// short of the wall it meets by less than a step, so the light ends at the wall's face rather than inside it.
///
/// Allocates nothing once built: the table is a fixed array, refilled in place.
/// </remarks>
public sealed class LanternCone
{
	/// <summary>How far the light reaches down a corridor, px: most of a cell (384) and further than a corridor is wide (288).</summary>
	public const float DefaultReach = 300f;

	/// <summary>Half the cone's spread, degrees. At 30 the light fills a 288 px corridor wall to wall 250 px ahead.</summary>
	public const float DefaultHalfAngleDegrees = 30f;

	/// <summary>The rays fanned across the cone, edge to edge: 2.5 degrees apart at the default spread.</summary>
	public const int Rays = 25;

	/// <summary>How far a ray is marched between wall tests: a quarter of a wall tile, so none is stepped over.</summary>
	public const float MarchStep = 12f;

	private readonly GroundSlamAttack shape;
	private readonly float[] reach = new float[Rays];
	private readonly Vector2[] direction = new Vector2[Rays];

	/// <summary>The default lantern's cone.</summary>
	public LanternCone() : this(DefaultReach, DefaultHalfAngleDegrees) { }

	/// <summary>
	/// A cone of another size: the planner aims a slightly LARGER one than the bearers carry, so a way it finds keeps a
	/// little dark between the walker and the light (a cautious player), never a way that brushes the edge.
	/// </summary>
	public LanternCone(float reachPx, float halfAngleDegrees)
	{
		// Interval and wind-up mean nothing here: the light is always on. The shape is what is borrowed.
		shape = new GroundSlamAttack(1f, 0.01f, reachPx, 0, Colors.White)
		{
			Shape = TelegraphShape.Cone,
			HalfAngleDegrees = halfAngleDegrees,
		};
	}

	/// <summary>The cone's length before any wall.</summary>
	public float Reach => shape.Radius;

	/// <summary>Half its spread, degrees.</summary>
	public float HalfAngleDegrees => shape.HalfAngleDegrees;

	/// <summary>Where the lantern stands on the floor (the bearer's feet): the cone's point.</summary>
	public Vector2 Origin { get; private set; }

	/// <summary>The way the light is thrown, a unit vector.</summary>
	public Vector2 Facing => shape.Facing;

	/// <summary>
	/// Points the cone from <paramref name="origin"/> along <paramref name="facing"/> and marches every ray out to the
	/// first wall. <paramref name="isWall"/> answers for a world point (the streamer's wall flag in the game, a grid in
	/// the validator): the walls are the only thing that stops the light - bodies, water and decor do not.
	/// </summary>
	public void Aim(Vector2 origin, Vector2 facing, Func<Vector2, bool> isWall)
	{
		Origin = origin;
		shape.Facing = facing.LengthSquared() > 1e-6f ? facing.Normalized() : Vector2.Right;
		float baseAngle = shape.Facing.Angle();
		float half = Mathf.DegToRad(shape.HalfAngleDegrees);
		float full = shape.Radius;
		for (int k = 0; k < Rays; k++)
		{
			Vector2 dir = Vector2.FromAngle(baseAngle - half + 2f * half * k / (Rays - 1));
			direction[k] = dir;
			float r = 0f;
			bool blocked = isWall != null && isWall(origin);
			while (!blocked && r < full)
			{
				float next = Mathf.Min(full, r + MarchStep);
				if (isWall != null && isWall(origin + dir * next))
					blocked = true;
				else
					r = next;
			}
			reach[k] = r;
		}
	}

	/// <summary>
	/// True if <paramref name="point"/> is in the light: inside the cone (<see cref="GroundSlamAttack.Contains"/>, the one
	/// authority on its shape) and no further out than the walls let the light go on that bearing - the shorter of the two
	/// drawn rays either side of it.
	/// </summary>
	public bool Covers(Vector2 point)
	{
		if (!shape.Contains(Origin, point))
			return false;
		Vector2 offset = point - Origin;
		float distance = offset.Length();
		if (distance <= 0.001f)
			return reach[Rays / 2] > 0f;
		float half = Mathf.DegToRad(shape.HalfAngleDegrees);
		float u = (shape.Facing.AngleTo(offset) + half) / (2f * half) * (Rays - 1);
		int k = Math.Clamp(Mathf.FloorToInt(u), 0, Rays - 2);
		return distance <= Mathf.Min(reach[k], reach[k + 1]);
	}

	/// <summary>How far ray <paramref name="k"/> (0 at one edge, <see cref="Rays"/> - 1 at the other) reaches before a wall.</summary>
	public float ReachOf(int k) => reach[k];

	/// <summary>The unit direction of ray <paramref name="k"/>.</summary>
	public Vector2 DirectionOf(int k) => direction[k];

	/// <summary>Where ray <paramref name="k"/> ends: on the floor at the wall's face, or at the cone's full length.</summary>
	public Vector2 EndOf(int k) => Origin + direction[k] * reach[k];

	/// <summary>How far the middle ray goes before a wall: how far down the corridor the light is thrown.</summary>
	public float AxisReach => reach[Rays / 2];
}
