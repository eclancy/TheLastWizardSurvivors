using Godot;
using System;

// THE HUNTED: a person the player leads, whom the swarm goes for (.ai/chapter-wizards.md 4.4, the primitive section 7
// asked for: "an escorted person enemies hunt"). Paracelsus, brought out of the gibbet's cage in the Blighted Swamp, is
// the first ask in the game where someone other than the player is hunted.
//
// TWO HALVES, BOTH CHEAP.
//
//   What the swarm sees is three statics: whether anyone is being led, where he is, and how near a body must be to go
//   for him (HuntRadius). Enemy._PhysicsProcess reads them in its chase branch - one flag test while nobody is led, one
//   distance while someone is - and a plain chaser inside the radius chases HIM with everything it would have chased
//   the player with (the lead, the ring slot, the stand-off at its touch distance). Only plain chasers: a body whose
//   steering is its own (a caster, a lunger, a slammer, an exploder, a summoner) keeps its aim on the player, and no
//   boss ever retargets (Enemy.ClassHuntsEscort). One escort at a time: whoever publishes withdraws, on every way out
//   (a fall, the landing, the boss arriving, the event leaving the tree).
//
//   How he walks is the leader's own steps. He keeps a trail of where the player has been - a crumb every CrumbSpacing,
//   in a ring of Capacity - and walks it in order at SpeedShare of the leader's walk, stopping FollowGap short of him.
//   So he goes where the player went: round the lake the player walked round, through the gap he found, and never
//   anywhere the player's boots could not go. He cuts no corners but his own loops (a crumb further along that he is
//   already standing on is taken at once). A plain class with no node, so the validator walks him on paper.
public sealed class HuntedEscort
{
	// --- what the swarm sees ------------------------------------------------------------------------------------

	/// <summary>Within this of him a plain chaser goes for him instead of the player (4.4: 250).</summary>
	public const float HuntRadius = 250f;
	public const float HuntRadiusSquared = HuntRadius * HuntRadius;

	/// <summary>True while someone is being led and the swarm may go for him.</summary>
	public static bool Active { get; private set; }

	/// <summary>Where he stands (his feet), while <see cref="Active"/>.</summary>
	public static Vector2 Target { get; private set; }

	/// <summary>How many times a body turned from the player to him since the last <see cref="ResetTally"/>. For probes.</summary>
	public static int TurnedOnHim { get; private set; }

	/// <summary>He is being led, and stands here this frame.</summary>
	public static void Publish(Vector2 at)
	{
		Active = true;
		Target = at;
	}

	/// <summary>Nobody is being led: the swarm goes back to the player.</summary>
	public static void Withdraw() => Active = false;

	internal static void NoteTurned() => TurnedOnHim++;

	public static void ResetTally() => TurnedOnHim = 0;

	// --- how he walks ---------------------------------------------------------------------------------------

	/// <summary>His walk as a share of the leader's (4.4: 75%).</summary>
	public float SpeedShare { get; init; } = 0.75f;

	/// <summary>He stops this far short of the leader: close behind, never under his feet.</summary>
	public float FollowGap { get; init; } = 64f;

	/// <summary>A crumb is laid each time the leader is this far from the last one.</summary>
	public const float CrumbSpacing = 20f;

	/// <summary>Crumbs kept: 10,000 px of the leader's walk. Past that he walks to the oldest he still has.</summary>
	public const int Capacity = 512;

	// How far along the trail he looks for a crumb he is already on (a loop the leader walked).
	private const int LookAhead = 16;

	private readonly Vector2[] crumbs = new Vector2[Capacity];
	private long newest = -1;   // the sequence number of the last crumb laid; -1 for none
	private long next;          // the sequence number of the crumb he is walking to

	public Vector2 Position { get; set; }
	public Vector2 Heading { get; private set; } = Vector2.Down;

	/// <summary>How fast he moved on the last step, px a second.</summary>
	public float Speed { get; private set; }

	/// <summary>Everything he has walked since <see cref="Reset"/>. For probes.</summary>
	public float Walked { get; private set; }

	/// <summary>He stands at <paramref name="at"/> with no trail to follow yet.</summary>
	public void Reset(Vector2 at)
	{
		Position = at;
		newest = -1;
		next = 0;
		Speed = 0f;
		Walked = 0f;
	}

	/// <summary>The leader is here: a crumb if he has gone far enough from the last.</summary>
	public void Record(Vector2 leader)
	{
		if (newest >= 0 && crumbs[newest % Capacity].DistanceSquaredTo(leader) < CrumbSpacing * CrumbSpacing)
			return;
		newest++;
		crumbs[newest % Capacity] = leader;
	}

	/// <summary>
	/// One step after the leader: along his trail at <see cref="SpeedShare"/> of <paramref name="leaderSpeed"/>, stopping
	/// <see cref="FollowGap"/> short of him. Returns the distance walked.
	/// </summary>
	public float Step(float delta, Vector2 leader, float leaderSpeed)
	{
		Record(leader);
		Speed = 0f;
		if (delta <= 0f || Position.DistanceSquaredTo(leader) <= FollowGap * FollowGap)
			return 0f;
		long oldest = Math.Max(0L, newest - Capacity + 1);
		if (next < oldest)
			next = oldest;
		// A crumb further along that he is already standing on: the leader walked a loop, and he need not.
		for (long k = Math.Min(newest, next + LookAhead); k > next; k--)
		{
			if (crumbs[k % Capacity].DistanceSquaredTo(Position) <= CrumbSpacing * CrumbSpacing)
			{
				next = k;
				break;
			}
		}
		float budget = SpeedShare * leaderSpeed * delta;
		float moved = 0f;
		for (int guard = 0; guard < 8 && budget > 0.001f; guard++)
		{
			bool onTrail = next <= newest;
			Vector2 goal = onTrail ? crumbs[next % Capacity] : leader;
			Vector2 to = goal - Position;
			float d = to.Length();
			if (d > 0.001f)
				Heading = to / d;
			if (d <= budget)
			{
				Position = goal;
				budget -= d;
				moved += d;
				if (!onTrail)
					break;
				next++;
			}
			else
			{
				Position += to / d * budget;
				moved += budget;
				budget = 0f;
			}
			if (Position.DistanceSquaredTo(leader) <= FollowGap * FollowGap)
				break;
		}
		Speed = moved / delta;
		Walked += moved;
		return moved;
	}
}
