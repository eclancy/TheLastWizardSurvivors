using Godot;
using System;

// THE LED: a person the player leads, who walks where the player walked. Paracelsus, brought out of the gibbet's cage in
// the Blighted Swamp, is the one it walks (.ai/unlock-puzzles.md 3.4). It was HuntedEscort when it was built
// (.ai/chapter-wizards.md 4.4): then the swarm went for him and bit him, through three statics read in Enemy's chase
// branch. 3.4 took the hunt out ("the puzzle is the route, not guarding him"), and with it everything any enemy reads.
//
// HE WALKS THE LEADER'S OWN STEPS. He keeps a trail of where the player has been - a crumb every CrumbSpacing, in a ring
// of Capacity - and walks it in order at SpeedShare of the leader's walk, stopping FollowGap short of him. So he goes
// where the player went: round the lake the player walked round, through the gap he found, and never anywhere the
// player's boots could not go. That is the gibbet's whole puzzle: the route the player walks is the route he takes.
//
// A LOOP THE LEADER CLOSES IS ONE HE NEVER WALKS. When the player comes back to within LoopRadius of a crumb the man has
// not reached yet, having walked at least LoopMinCrumbs on from it, that stretch is a loop, and the man steps across it
// from the one crumb to the other and leaves it out. It is what makes going back cost time and not his health: walk him
// toward a pool, see the steps ahead of him run into it, go back the way you came and round, and he takes the way round
// (.ai/unlock-puzzles.md section 1: "a wrong move costs time, never progress"). The step across is at most LoopRadius,
// so he is never more than half of it off the walked path; and MayCut, when it is set, can refuse a step (the gibbet
// refuses one that would cross a pool or water), and then he walks the loop as the player did.
//
// What he will walk is settled as the trail is laid - each crumb remembers the furthest later crumb it closes a loop
// with - so the steps drawn ahead of him (CopyAhead) and the steps he takes are one list read one way. A plain class
// with no node, so the validator walks him on paper. Nothing in it allocates.
public sealed class LedEscort
{
	/// <summary>His walk as a share of the leader's (4.4: 75%).</summary>
	public float SpeedShare { get; init; } = 0.75f;

	/// <summary>He stops this far short of the leader: close behind, never under his feet.</summary>
	public float FollowGap { get; init; } = 64f;

	/// <summary>Whether he may step straight across from one crumb to a later one to leave a loop out. Null: always.</summary>
	public Func<Vector2, Vector2, bool> MayCut { get; init; }

	/// <summary>A crumb is laid each time the leader is this far from the last one.</summary>
	public const float CrumbSpacing = 20f;

	/// <summary>Crumbs kept: 10,000 px of the leader's walk. Past that he walks to the oldest he still has.</summary>
	public const int Capacity = 512;

	/// <summary>
	/// A crumb laid this near one he has not reached closes a loop. Twice the spacing: a player retracing his own steps
	/// on a phone, in a crowd, is never within 20 px of them, and 40 keeps the step across within 20 px of the path.
	/// </summary>
	public const float LoopRadius = 40f;

	/// <summary>A loop is at least this many crumbs long: a corner, or a stride back, is walked.</summary>
	public const int LoopMinCrumbs = 4;

	private readonly Vector2[] crumbs = new Vector2[Capacity];
	// For each crumb, the furthest later crumb he steps across to from it (a loop the leader closed); -1 for none.
	private readonly long[] across = new long[Capacity];
	private long newest = -1;   // the sequence number of the last crumb laid; -1 for none
	private long next;          // the sequence number of the crumb he is walking to

	public Vector2 Position { get; set; }
	public Vector2 Heading { get; private set; } = Vector2.Down;

	/// <summary>How fast he moved on the last step, px a second.</summary>
	public float Speed { get; private set; }

	/// <summary>Everything he has walked since <see cref="Reset"/>. For probes.</summary>
	public float Walked { get; private set; }

	/// <summary>Loops he stepped across and left out since <see cref="Reset"/>. For probes.</summary>
	public int LoopsLeftOut { get; private set; }

	/// <summary>He stands at <paramref name="at"/> with no trail to follow yet.</summary>
	public void Reset(Vector2 at)
	{
		Position = at;
		newest = -1;
		next = 0;
		Speed = 0f;
		Walked = 0f;
		LoopsLeftOut = 0;
	}

	/// <summary>The leader is here: a crumb if he has gone far enough from the last.</summary>
	public void Record(Vector2 leader)
	{
		if (newest >= 0 && crumbs[newest % Capacity].DistanceSquaredTo(leader) < CrumbSpacing * CrumbSpacing)
			return;
		newest++;
		int slot = (int)(newest % Capacity);
		crumbs[slot] = leader;
		across[slot] = -1;
		// Back near a crumb he has still to reach, the long way round: he will step across from that one to this.
		long first = Math.Max(next, newest - Capacity + 1);
		for (long j = first; j <= newest - LoopMinCrumbs; j++)
		{
			int s = (int)(j % Capacity);
			if (crumbs[s].DistanceSquaredTo(leader) <= LoopRadius * LoopRadius && (MayCut == null || MayCut(crumbs[s], leader)))
				across[s] = newest;
		}
	}

	// The crumb he goes on to once he stands on crumb k: across a loop, or the next.
	private long After(long k)
	{
		long to = across[k % Capacity];
		return to > k ? to : k + 1;
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
				long after = After(next);
				if (after > next + 1)
					LoopsLeftOut++;
				next = after;
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

	/// <summary>
	/// The crumbs he will walk from where he is, in the order he will walk them and with every closed loop left out, into
	/// <paramref name="into"/> (as many as fit), and each one's sequence number into <paramref name="ids"/> if given - it
	/// stays the crumb's while he walks, so a drawing keyed on it holds still. Returns how many. The gibbet draws them as
	/// the player's steps glowing ahead of him.
	/// </summary>
	public int CopyAhead(Vector2[] into, long[] ids = null)
	{
		int n = 0;
		long k = Math.Max(next, Math.Max(0L, newest - Capacity + 1));
		for (; k <= newest && n < into.Length; k = After(k))
		{
			if (ids != null && n < ids.Length)
				ids[n] = k;
			into[n++] = crumbs[k % Capacity];
		}
		return n;
	}
}
