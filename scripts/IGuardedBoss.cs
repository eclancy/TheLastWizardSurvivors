using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// A boss holding a directional guard: a front that cuts what lands on it, and an open side a
/// seeking projectile can reach by flying round (.ai/in-flight.md 28 - the Hayward's hurdle).
/// </summary>
/// <remarks>
/// Two consumers, deliberately decoupled. Magic Missile reads it to bend its flight round the
/// guarded front and come in on the open side, so the player SEES the dart take the way round.
/// The damage rule itself stays the boss's own: its <c>AdjustIncomingDamage</c> judges each hit
/// by where it arrived from (<c>Enemy.IncomingHitFrom</c>) when the dealer said, else by the
/// player's position - this interface changes no number, only the flight that earns one.
/// </remarks>
public interface IGuardedBoss
{
	/// <summary>Whether the guard is up at all right now. A dart ignores a guard that is down.</summary>
	bool GuardActive { get; }

	/// <summary>The way the guard faces, a world direction (enemies never rotate their node).</summary>
	Vector2 GuardFacing { get; }

	/// <summary>Half-angle of the guarded front, in radians.</summary>
	float GuardHalfAngleRadians { get; }

	/// <summary>Whether a hit arriving from <paramref name="worldPoint"/> lands on the guard.</summary>
	bool GuardCovers(Vector2 worldPoint);
}
