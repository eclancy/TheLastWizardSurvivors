using System;
using System.Collections.Generic;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// IMHOTEP'S MIRRORS, AS A LAYOUT AND A RULE (.ai/unlock-puzzles.md 3.1, "route the sun"; built 2026-11-08). Sunlight
/// falls on one bronze mirror, the source; more stand on the sand round his tomb. A mirror faces one of eight ways, and
/// while he stands within <see cref="Reach"/> of it, it swings to face AWAY from him. A mirror the light reaches shines
/// where it faces, and the light that reaches the sun-disc over the tomb's door opens it.
/// </summary>
/// <remarks>
/// <para>THE ONE AUTHORITY. The event (TombBeaconsEvent) and the proof (RegressionChecks.ValidateTombBeacons) both read
/// this class: where the mirrors stand, which way the light goes from each of them in each facing (<see cref="HitFrom"/>,
/// worked out once against the ground and kept, since neither the mirrors nor the terrain move), where it ends, and
/// how a facing follows his feet (<see cref="FacingAway"/>). So the beam the player sees and the answer on paper cannot
/// disagree.</para>
/// <para>THE LIGHT. A beam goes straight from a mirror's foot along its facing, at most <see cref="BeamReach"/>. The
/// first of these along it stops it: another mirror (it passes within <see cref="MirrorCatch"/> of its foot), which
/// lights; the socket (within <see cref="SocketCatch"/> of the tomb's foot, and only coming from the front - the north,
/// north-east or north-west: the disc faces south); the tomb's own body (<see cref="TombBody"/>); terrain (water, rock,
/// walls: the ground's <c>Blocked</c>, sampled every <see cref="TerrainStep"/>). Bodies do not stop it - the event burns
/// them as it passes (3.1, decided). A beam that falls on a mirror already lit ends there.</para>
/// <para>THE SEED (3.1, the seed). A solution is drawn first, backward from the socket: three or four legs, each
/// <see cref="MinLeg"/>-<see cref="MaxLeg"/> long, each turning off the last by 45, 90 or 135 degrees, the last coming in
/// from the south; every leg on open ground the whole way (the ground's <c>Blocked</c> every <see cref="LegSampleStep"/>,
/// and its explore test - <c>Node2DGame.IsExploreGroundClear</c> through the placer - every <see cref="LegClearStep"/>).
/// Then one or two decoys, each standing where a path mirror could send the light if turned the wrong way. Then every
/// mirror is set to a wrong facing (the source's into a decoy where one hangs off it, so from the first moment the
/// light is seen going mirror to mirror, into a dead end). A layout is kept only if <see cref="Proves"/>: the solution
/// lights the socket along exactly its path; nothing reaches it with fewer mirrors than the path holds; the start
/// facings do not reach it; no single path mirror turned any other way reaches it; and every decoy can catch light.</para>
/// <para>THE OPEN QUESTIONS, decided in the build: five or six mirrors in all (the source and four or five more - three
/// legs take two decoys, four legs one or two), and no leg of the solution crosses another, or comes within
/// <see cref="LegClearance"/> of a leg it does not meet, so the route reads as one line across the sand.</para>
/// </remarks>
public sealed class SunMirrors
{
	// --- the rule ---------------------------------------------------------------------------------------------------

	/// <summary>Eight facings (3.1, decided): 0 east, 1 south-east, 2 south, 3 south-west, 4 west, 5 north-west, 6 north,
	/// 7 north-east - the frames of tools/art/tomb_beacons.py's mirror sheet, in its order.</summary>
	public const int Facings = 8;

	/// <summary>Within this of a mirror's foot his feet turn it (3.1: first guess 140 px).</summary>
	public const float Reach = 140f;

	/// <summary>Nearer than this the bearing from his feet is noise; the mirror holds (its stand is solid anyway).</summary>
	public const float MinTurnDistance = 16f;

	/// <summary>A facing holds until he is this far past the half-way line to the next, so standing on the line between two
	/// facings does not set the mirror chattering.</summary>
	public const float TurnHysteresisDegrees = 7f;

	/// <summary>How high the light goes over the ground: a mirror's disc and the sun-disc's middle are both 32 art rows over
	/// their feet (tomb_beacons.py FACE_Y, the socket on the tomb's row 32), 64 world px at x2.</summary>
	public const float BeamHeight = 64f;

	/// <summary>How far a beam goes with nothing in its way, and fades out.</summary>
	public const float BeamReach = 760f;

	/// <summary>A beam passing this near a mirror's foot is caught by it.</summary>
	public const float MirrorCatch = 18f;

	/// <summary>A beam coming in from the front this near the tomb's foot lands on the sun-disc.</summary>
	public const float SocketCatch = 22f;

	/// <summary>How finely a beam is walked against the terrain.</summary>
	public const float TerrainStep = 8f;

	/// <summary>The tomb's body round its foot: the facade and the dune it is cut into, which stop a beam. Its front edge
	/// is 2 px behind the foot, so light coming in from the front reaches the socket first.</summary>
	public static readonly Rect2 TombBody = new(-84f, -112f, 168f, 110f);

	// --- the seed ---------------------------------------------------------------------------------------------------

	public const int MinLegs = 3, MaxLegs = 4, MinDecoys = 1, MaxDecoys = 2;

	/// <summary>The most mirrors a layout holds: four legs' mirrors and two decoys.</summary>
	public const int MaxMirrors = MaxLegs + MaxDecoys;

	/// <summary>A leg's length (3.1: at least 300). 340 at least, so a mirror is never within reach of where he stands to
	/// turn the next (<see cref="MinSpacing"/>).</summary>
	public const float MinLeg = 340f, MaxLeg = 540f;

	/// <summary>No two mirrors nearer than this: standing at <see cref="ApproachDistance"/> from one he is still 30 px
	/// outside the next one's reach.</summary>
	public const float MinSpacing = 340f;

	/// <summary>A leg of the solution keeps this far from every leg it does not meet and from every mirror not at its
	/// ends - the open question "may a leg cross another", answered no.</summary>
	public const float LegClearance = 110f;

	/// <summary>Every mirror at least this far from the tomb's foot.</summary>
	public const float TombKeepOff = 340f;

	/// <summary>Where a player who means it stands to set a facing: this far behind the mirror from the way it is to face.
	/// For the proof and the probe.</summary>
	public const float StandDistance = 80f;

	/// <summary>Just outside a mirror's reach on the same line, where he walks in from so nothing on the way turns it.</summary>
	public const float ApproachDistance = Reach + 30f;

	/// <summary>Open ground kept round each mirror, so it can be walked round.</summary>
	public const float MirrorFootprint = 100f;

	/// <summary>How finely a leg is walked against what blocks light, and how often (and how wide) against the explore
	/// test of open ground.</summary>
	public const float LegSampleStep = 16f;
	public const float LegClearStep = 100f;
	public const float LegClearRadius = 50f;

	/// <summary>The rolls Generate tries before it gives up on a tomb. Over the proof's 80 tombs the slowest was kept on
	/// roll 28 (2026-11-08), so this leaves room; the band's sweep allows a candidate fewer (TombBeaconsEvent.SiteAttempts).</summary>
	public const int MaxAttempts = 80;
	private const int Salt = 0x5017;

	// The last leg comes in from the front: north, north-west or north-east (facings 6, 5, 7).
	private static readonly int[] LastLeg = { 6, 5, 7 };
	// A leg turns off the next by 45, 90 or 135 degrees: never straight on (the mirror between would only be in the way)
	// and never straight back.
	private static readonly int[] Turns = { 1, -1, 2, -2, 3, -3 };

	public enum End { Mirror, Socket, Blocked, Open }

	/// <summary>Where a beam from a mirror in a facing ends, and how far it went.</summary>
	public readonly struct Hit
	{
		public readonly End Kind;
		public readonly int Mirror;
		public readonly float Length;

		public Hit(End kind, int mirror, float length)
		{
			Kind = kind;
			Mirror = mirror;
			Length = length;
		}
	}

	/// <summary>What the light and the seed need to know about the ground.</summary>
	public sealed class Ground
	{
		/// <summary>True where the light is stopped and nothing may stand: water, rock, walls (Node2DGame.IsGroundBlockedAt).
		/// Null: nothing is.</summary>
		public Func<Vector2, bool> Blocked { get; init; }

		/// <summary>True if a footprint round a point is open explore ground (ExplorePlacer.IsClear). Null: everywhere.</summary>
		public Func<Vector2, float, bool> Clear { get; init; }

		/// <summary>True if a footprint keeps off every other event's (ExplorePlacer.IsOffOtherSites). Null: everywhere.</summary>
		public Func<Vector2, float, bool> Allowed { get; init; }

		/// <summary>Open ground everywhere.</summary>
		public static readonly Ground Open = new();

		internal bool IsBlocked(Vector2 p) => Blocked != null && Blocked(p);

		internal bool IsOpen(Vector2 p, float radius) =>
			(Clear == null || Clear(p, radius)) && (Allowed == null || Allowed(p, radius));
	}

	// --- the layout -------------------------------------------------------------------------------------------------

	private readonly Vector2[] mirrors;
	private readonly int[] path;
	private readonly int[] solution;
	private readonly int[] start;
	private readonly Hit[] table;

	private SunMirrors(Vector2 tomb, Vector2[] mirrors, int[] path, int[] solution, int[] start, Hit[] table,
		int attempts, ulong seed, bool relaxed)
	{
		Tomb = tomb;
		this.mirrors = mirrors;
		this.path = path;
		this.solution = solution;
		this.start = start;
		this.table = table;
		Attempts = attempts;
		Seed = seed;
		Relaxed = relaxed;
	}

	/// <summary>The tomb's foot: the socket's point on the ground.</summary>
	public Vector2 Tomb { get; }

	/// <summary>Every mirror's foot, the source first, then the path's, then the decoys.</summary>
	public IReadOnlyList<Vector2> Mirrors => mirrors;
	public int MirrorCount => mirrors.Length;

	/// <summary>The mirrors the solution carries the light through, the source first.</summary>
	public IReadOnlyList<int> Path => path;
	public int Legs => path.Length;
	public int DecoyCount => mirrors.Length - path.Length;

	/// <summary>Each mirror's facing in the solution; -1 for a decoy, which the solution never lights.</summary>
	public IReadOnlyList<int> Solution => solution;

	/// <summary>Each mirror's facing as he finds it: every path mirror wrong.</summary>
	public IReadOnlyList<int> Start => start;

	public bool IsDecoy(int mirror) => solution[mirror] < 0;

	/// <summary>Which roll the seed was kept on, the seed, and whether it had to drop the open-ground footprints.</summary>
	public int Attempts { get; }
	public ulong Seed { get; }
	public bool Relaxed { get; }

	/// <summary>The length of the solution's leg out of path mirror <paramref name="k"/>.</summary>
	public float LegLength(int k) => HitFrom(path[k], solution[path[k]]).Length;

	/// <summary>Where the light from <paramref name="mirror"/> in <paramref name="facing"/> ends. Worked out once.</summary>
	public Hit HitFrom(int mirror, int facing) => table[mirror * Facings + Wrap(facing)];

	/// <summary>The point on the ground a beam from <paramref name="mirror"/> in <paramref name="facing"/> ends at: the
	/// mirror or the socket it lands on, else where the terrain stopped it or it ran out.</summary>
	public Vector2 BeamEnd(int mirror, int facing)
	{
		Hit h = HitFrom(mirror, facing);
		return h.Kind switch
		{
			End.Mirror => mirrors[h.Mirror],
			End.Socket => Tomb,
			_ => mirrors[mirror] + Direction(facing) * h.Length,
		};
	}

	/// <summary>
	/// The light from the source through the mirrors as they face: the mirrors it lights, in order, into
	/// <paramref name="chain"/> (at least <see cref="MirrorCount"/> long); true if it ends on the socket. A beam that falls
	/// on a mirror already lit ends there. Allocates nothing.
	/// </summary>
	public bool Trace(IReadOnlyList<int> facings, int[] chain, out int length)
	{
		length = 0;
		int at = 0;
		while (length < chain.Length)
		{
			chain[length++] = at;
			Hit h = HitFrom(at, facings[at]);
			if (h.Kind == End.Socket)
				return true;
			if (h.Kind != End.Mirror)
				return false;
			for (int k = 0; k < length; k++)
				if (chain[k] == h.Mirror)
					return false;
			at = h.Mirror;
		}
		return false;
	}

	/// <summary>The solution's facings, with the decoys as they start.</summary>
	public int[] SolutionFacings()
	{
		var f = new int[mirrors.Length];
		for (int i = 0; i < f.Length; i++)
			f[i] = solution[i] >= 0 ? solution[i] : start[i];
		return f;
	}

	/// <summary>
	/// The fewest mirrors from the source to the socket, as (mirror, facing) in order - what a player who has seen where
	/// every mirror stands, and which way each could send the light, works out. Null if nothing reaches it.
	/// </summary>
	public List<(int Mirror, int Facing)> ShortestRoute()
	{
		int n = mirrors.Length;
		var cameFrom = new int[n];
		var cameFacing = new int[n];
		var seen = new bool[n];
		var queue = new Queue<int>();
		seen[0] = true;
		cameFrom[0] = -1;
		queue.Enqueue(0);
		while (queue.Count > 0)
		{
			int i = queue.Dequeue();
			for (int f = 0; f < Facings; f++)
			{
				Hit h = HitFrom(i, f);
				if (h.Kind == End.Socket)
				{
					var route = new List<(int, int)> { (i, f) };
					for (int at = i; cameFrom[at] >= 0; at = cameFrom[at])
						route.Add((cameFrom[at], cameFacing[at]));
					route.Reverse();
					return route;
				}
				if (h.Kind == End.Mirror && !seen[h.Mirror])
				{
					seen[h.Mirror] = true;
					cameFrom[h.Mirror] = i;
					cameFacing[h.Mirror] = f;
					queue.Enqueue(h.Mirror);
				}
			}
		}
		return null;
	}

	/// <summary>How many mirrors the fewest that reach the socket light, or -1.</summary>
	public int FewestMirrors() => ShortestRoute()?.Count ?? -1;

	/// <summary>
	/// Everything the seed promises, on paper: the solution lights the socket along exactly its path; nothing reaches it
	/// with fewer mirrors; the start facings do not, and no path mirror starts right; no single path mirror turned any
	/// other way reaches it (the rest at the solution); and every decoy can catch light. <paramref name="why"/> says
	/// which failed.
	/// </summary>
	public bool Proves(out string why)
	{
		int n = mirrors.Length;
		var chain = new int[n];
		int[] facings = SolutionFacings();
		if (!Trace(facings, chain, out int len) || len != path.Length)
		{
			why = $"the solution does not bring the light to the socket through its {path.Length} mirrors (it lit {len})";
			return false;
		}
		for (int k = 0; k < len; k++)
			if (chain[k] != path[k])
			{
				why = $"the solution's light goes through mirror {chain[k]} where the path has {path[k]}";
				return false;
			}
		int fewest = FewestMirrors();
		if (fewest != path.Length)
		{
			why = $"solvable with {fewest} mirrors, not the {path.Length} on its path";
			return false;
		}
		if (Trace(start, chain, out _))
		{
			why = "its start facings already bring the light to the socket";
			return false;
		}
		foreach (int m in path)
			if (start[m] == solution[m])
			{
				why = $"path mirror {m} starts facing the right way";
				return false;
			}
		foreach (int m in path)
		{
			int right = facings[m];
			for (int f = 0; f < Facings; f++)
			{
				if (f == right)
					continue;
				facings[m] = f;
				bool lit = Trace(facings, chain, out _);
				facings[m] = right;
				if (lit)
				{
					why = $"path mirror {m} turned to {f} (not {right}) still brings the light to the socket";
					return false;
				}
			}
		}
		for (int d = path.Length; d < n; d++)
		{
			bool reached = false;
			for (int i = 0; i < n && !reached; i++)
				for (int f = 0; f < Facings && !reached; f++)
				{
					Hit h = HitFrom(i, f);
					reached = h.Kind == End.Mirror && h.Mirror == d;
				}
			if (!reached)
			{
				why = $"decoy {d} can never catch the light";
				return false;
			}
		}
		why = null;
		return true;
	}

	// --- the facing -------------------------------------------------------------------------------------------------

	private const float Diagonal = 0.70710678f;
	private static readonly Vector2[] Directions =
	{
		new(1f, 0f), new(Diagonal, Diagonal), new(0f, 1f), new(-Diagonal, Diagonal),
		new(-1f, 0f), new(-Diagonal, -Diagonal), new(0f, -1f), new(Diagonal, -Diagonal),
	};

	public static int Wrap(int facing) => ((facing % Facings) + Facings) % Facings;

	/// <summary>The unit step a facing points along on the ground (y down).</summary>
	public static Vector2 Direction(int facing) => Directions[Wrap(facing)];

	/// <summary>The facing nearest a direction on the ground.</summary>
	public static int FacingOf(Vector2 direction) =>
		Wrap(Mathf.RoundToInt(Mathf.Atan2(direction.Y, direction.X) / (Mathf.Pi / 4f)));

	/// <summary>
	/// THE ONE ACTION: the facing a mirror at <paramref name="mirror"/> takes with his feet at <paramref name="feet"/>.
	/// Within <see cref="Reach"/> it faces away from him, snapped to eight; out of reach, or closer than
	/// <see cref="MinTurnDistance"/>, it keeps <paramref name="current"/>; and it keeps it until he is
	/// <see cref="TurnHysteresisDegrees"/> past the half-way line to the next facing.
	/// </summary>
	public static int FacingAway(Vector2 mirror, Vector2 feet, int current)
	{
		Vector2 away = mirror - feet;
		float d = away.Length();
		if (d > Reach || d < MinTurnDistance)
			return current;
		float angle = Mathf.Atan2(away.Y, away.X);
		if (current >= 0)
		{
			float held = Mathf.Abs(Mathf.AngleDifference(Wrap(current) * Mathf.Pi / 4f, angle));
			if (held <= Mathf.DegToRad(22.5f + TurnHysteresisDegrees))
				return current;
		}
		return FacingOf(away);
	}

	/// <summary>Where to stand to turn <paramref name="mirror"/> to <paramref name="facing"/>: behind it.</summary>
	public static Vector2 StandPoint(Vector2 mirror, int facing, float distance = StandDistance) =>
		mirror - Direction(facing) * distance;

	// --- the light --------------------------------------------------------------------------------------------------

	/// <summary>Where a beam from <paramref name="from"/> in <paramref name="facing"/> ends (THE LIGHT, in the remarks).</summary>
	public static Hit Cast(IReadOnlyList<Vector2> mirrors, Vector2 tomb, int from, int facing, Func<Vector2, bool> blocked)
	{
		Vector2 p = mirrors[from];
		Vector2 v = Direction(facing);
		float best = BeamReach;
		End kind = End.Open;
		int caught = -1;
		for (int j = 0; j < mirrors.Count; j++)
		{
			if (j == from)
				continue;
			Vector2 rel = mirrors[j] - p;
			float t = rel.Dot(v);
			if (t <= MirrorCatch || t >= best || Mathf.Abs(rel.Cross(v)) > MirrorCatch)
				continue;
			best = t;
			kind = End.Mirror;
			caught = j;
		}
		if (v.Y < -0.1f)
		{
			Vector2 rel = tomb - p;
			float t = rel.Dot(v);
			if (t > 0f && t < best && Mathf.Abs(rel.Cross(v)) <= SocketCatch)
			{
				best = t;
				kind = End.Socket;
				caught = -1;
			}
		}
		if (RayEntersRect(p - tomb, v, TombBody, out float into) && into < best)
		{
			best = into;
			kind = End.Blocked;
			caught = -1;
		}
		if (blocked != null)
		{
			for (float s = TerrainStep; s < best; s += TerrainStep)
			{
				if (blocked(p + v * s))
				{
					best = s;
					kind = End.Blocked;
					caught = -1;
					break;
				}
			}
		}
		return new Hit(kind, caught, best);
	}

	/// <summary>True if <paramref name="relToTomb"/> (a point relative to the tomb's foot) is in the tomb's body, grown by
	/// <paramref name="margin"/>.</summary>
	public static bool InTombBody(Vector2 relToTomb, float margin = 0f) => TombBody.Grow(margin).HasPoint(relToTomb);

	private static bool RayEntersRect(Vector2 origin, Vector2 dir, Rect2 rect, out float t)
	{
		float tmin = 0f, tmax = float.MaxValue;
		for (int axis = 0; axis < 2; axis++)
		{
			float o = axis == 0 ? origin.X : origin.Y;
			float d = axis == 0 ? dir.X : dir.Y;
			float lo = axis == 0 ? rect.Position.X : rect.Position.Y;
			float hi = axis == 0 ? rect.End.X : rect.End.Y;
			if (Mathf.Abs(d) < 1e-6f)
			{
				if (o < lo || o > hi)
				{
					t = 0f;
					return false;
				}
				continue;
			}
			float t1 = (lo - o) / d, t2 = (hi - o) / d;
			if (t1 > t2)
				(t1, t2) = (t2, t1);
			tmin = Mathf.Max(tmin, t1);
			tmax = Mathf.Min(tmax, t2);
			if (tmin > tmax)
			{
				t = 0f;
				return false;
			}
		}
		t = tmin;
		return true;
	}

	/// <summary>How far <paramref name="p"/> is from the segment <paramref name="a"/>-<paramref name="b"/>.</summary>
	public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float l2 = ab.LengthSquared();
		float t = l2 > 0f ? Mathf.Clamp((p - a).Dot(ab) / l2, 0f, 1f) : 0f;
		return p.DistanceTo(a + ab * t);
	}

	/// <summary>How near two segments come: 0 if they cross.</summary>
	public static float SegmentGap(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
	{
		float d1 = (b - a).Cross(c - a), d2 = (b - a).Cross(d - a);
		float d3 = (d - c).Cross(a - c), d4 = (d - c).Cross(b - c);
		if ((d1 > 0f) != (d2 > 0f) && (d3 > 0f) != (d4 > 0f))
			return 0f;
		return Mathf.Min(Mathf.Min(DistanceToSegment(a, c, d), DistanceToSegment(b, c, d)),
			Mathf.Min(DistanceToSegment(c, a, b), DistanceToSegment(d, a, b)));
	}

	// --- the seed ---------------------------------------------------------------------------------------------------

	/// <summary>
	/// A layout round the tomb at <paramref name="tomb"/> from <paramref name="seed"/> (THE SEED, in the remarks): the first
	/// of <paramref name="maxAttempts"/> rolls that keeps every rule and <see cref="Proves"/>, or null. Deterministic in
	/// (tomb, seed, ground). With <paramref name="relaxed"/> the open-ground footprints are not asked - only what blocks
	/// the light - for a tomb the band could find no better ground for.
	/// </summary>
	public static SunMirrors Generate(Vector2 tomb, ulong seed, Ground ground, bool relaxed = false, int maxAttempts = MaxAttempts)
	{
		ground ??= Ground.Open;
		var rng = new LevelGenerator.Rng(LevelGenerator.Hash(seed, Mathf.RoundToInt(tomb.X), Mathf.RoundToInt(tomb.Y), Salt));
		for (int attempt = 1; attempt <= maxAttempts; attempt++)
		{
			SunMirrors layout = TryOnce(tomb, ref rng, ground, relaxed, attempt, seed);
			if (layout != null)
				return layout;
		}
		return null;
	}

	private static SunMirrors TryOnce(Vector2 tomb, ref LevelGenerator.Rng rng, Ground ground, bool relaxed, int attempt, ulong seed)
	{
		int legs = rng.NextFloat() < 0.5f ? MinLegs : MaxLegs;
		int decoys = legs == MinLegs ? MaxDecoys : (rng.NextFloat() < 0.5f ? MinDecoys : MaxDecoys);
		// The path, backward from the socket: pts[k] is path mirror k (0 the source), travel[k] the way its light goes.
		var travel = new int[legs];
		var pts = new Vector2[legs + 1];
		pts[legs] = tomb;
		travel[legs - 1] = LastLeg[rng.Next(LastLeg.Length)];
		for (int k = legs - 1; k >= 0; k--)
		{
			if (k < legs - 1)
				travel[k] = Wrap(travel[k + 1] + Turns[rng.Next(Turns.Length)]);
			pts[k] = pts[k + 1] - Direction(travel[k]) * Mathf.Lerp(MinLeg, MaxLeg, rng.NextFloat());
		}
		if (!PathKeepsShape(pts, legs, tomb) || !PathOnGround(pts, travel, legs, ground, relaxed))
			return null;

		// The decoys: each where a path mirror could send the light if turned the wrong way.
		var all = new List<Vector2>(legs + decoys);
		for (int k = 0; k < legs; k++)
			all.Add(pts[k]);
		var hangsOff = new List<(int Mirror, int Facing)>();
		for (int d = 0; d < decoys; d++)
		{
			bool placed = false;
			for (int tries = 0; tries < 16 && !placed; tries++)
			{
				int k = rng.Next(legs);
				int f = rng.Next(Facings);
				float len = Mathf.Lerp(MinLeg, MaxLeg, rng.NextFloat());
				if (f == travel[k])
					continue;
				Vector2 at = pts[k] + Direction(f) * len;
				if (!DecoyFits(at, pts[k], all, pts, legs, tomb, ground, relaxed))
					continue;
				all.Add(at);
				hangsOff.Add((k, f));
				placed = true;
			}
			if (!placed)
				return null;
		}

		int n = all.Count;
		var solution = new int[n];
		var start = new int[n];
		for (int i = 0; i < n; i++)
			solution[i] = i < legs ? travel[i] : -1;
		for (int i = 0; i < n; i++)
			start[i] = i < legs ? Wrap(travel[i] + 1 + rng.Next(Facings - 1)) : rng.Next(Facings);
		// The light begins in a decoy where one hangs off the source: mirror to mirror from the first moment, into a dead end.
		foreach ((int k, int f) in hangsOff)
			if (k == 0)
			{
				start[0] = f;
				break;
			}

		Vector2[] placedMirrors = all.ToArray();
		var table = new Hit[n * Facings];
		for (int i = 0; i < n; i++)
			for (int f = 0; f < Facings; f++)
				table[i * Facings + f] = Cast(placedMirrors, tomb, i, f, ground.Blocked);
		var path = new int[legs];
		for (int k = 0; k < legs; k++)
			path[k] = k;
		var layout = new SunMirrors(tomb, placedMirrors, path, solution, start, table, attempt, seed, relaxed);
		return layout.Proves(out _) ? layout : null;
	}

	// The path's geometry, before any ground is asked: mirrors spaced and off the tomb, legs that never cross or crowd one
	// another or a mirror not at their ends, and none but the last near the tomb.
	private static bool PathKeepsShape(Vector2[] pts, int legs, Vector2 tomb)
	{
		for (int i = 0; i < legs; i++)
		{
			if (pts[i].DistanceTo(tomb) < TombKeepOff || InTombBody(pts[i] - tomb, 60f))
				return false;
			for (int j = i + 1; j < legs; j++)
				if (pts[i].DistanceTo(pts[j]) < MinSpacing)
					return false;
		}
		for (int i = 0; i < legs; i++)
		{
			for (int j = i + 2; j < legs; j++)
				if (SegmentGap(pts[i], pts[i + 1], pts[j], pts[j + 1]) < LegClearance)
					return false;
			for (int m = 0; m < legs; m++)
				if (m != i && m != i + 1 && DistanceToSegment(pts[m], pts[i], pts[i + 1]) < LegClearance)
					return false;
			if (i < legs - 1)
			{
				if (DistanceToSegment(tomb, pts[i], pts[i + 1]) < TombKeepOff * 0.5f)
					return false;
			}
		}
		return true;
	}

	// The path on the ground: nothing that stops light on any leg or on the walk in to each mirror's stand point, and -
	// unless relaxed - open explore ground round every mirror and along every leg, off every other event's footprint.
	private static bool PathOnGround(Vector2[] pts, int[] travel, int legs, Ground ground, bool relaxed)
	{
		for (int k = 0; k < legs; k++)
		{
			Vector2 a = pts[k], b = pts[k + 1];
			float len = a.DistanceTo(b);
			Vector2 v = (b - a) / len;
			for (float s = 0f; s <= len; s += LegSampleStep)
				if (ground.IsBlocked(a + v * s))
					return false;
			Vector2 dir = Direction(travel[k]);
			for (float s = StandDistance * 0.5f; s <= ApproachDistance + 1f; s += LegSampleStep)
				if (ground.IsBlocked(a - dir * s))
					return false;
		}
		if (relaxed)
			return true;
		for (int k = 0; k < legs; k++)
			if (!ground.IsOpen(pts[k], MirrorFootprint))
				return false;
		for (int k = 0; k < legs; k++)
		{
			Vector2 a = pts[k], b = pts[k + 1];
			float len = a.DistanceTo(b);
			Vector2 v = (b - a) / len;
			for (float s = LegClearStep * 0.5f; s <= len - LegClearStep * 0.5f; s += LegClearStep)
				if (!ground.IsOpen(a + v * s, LegClearRadius))
					return false;
		}
		return true;
	}

	private static bool DecoyFits(Vector2 at, Vector2 from, List<Vector2> placed, Vector2[] pts, int legs, Vector2 tomb,
		Ground ground, bool relaxed)
	{
		foreach (Vector2 other in placed)
			if (other.DistanceTo(at) < MinSpacing)
				return false;
		if (at.DistanceTo(tomb) < TombKeepOff || InTombBody(at - tomb, 60f))
			return false;
		for (int k = 0; k < legs; k++)
			if (DistanceToSegment(at, pts[k], pts[k + 1]) < LegClearance)
				return false;
		float len = from.DistanceTo(at);
		Vector2 v = (at - from) / len;
		for (float s = 0f; s <= len; s += LegSampleStep)
			if (ground.IsBlocked(from + v * s))
				return false;
		return relaxed ? !ground.IsBlocked(at) : ground.IsOpen(at, MirrorFootprint);
	}
}
