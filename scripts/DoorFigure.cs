using System;
using System.Collections.Generic;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// MA LIANG'S DOOR, AS A FIGURE (.ai/unlock-puzzles.md 3.2): a door-like drawing of five to seven charcoal dots joined
/// by seven to ten lines - its jambs and arch always, then a lintel, a sill, a brace, the spokes of its ring - that can
/// be inked in ONE unbroken stroke, and only from one of exactly two dots. Which figure a run draws is the seed's.
/// </summary>
/// <remarks>
/// <para>THE FAMILY is every subset of the pool that keeps the rules, found at first use: the four outline lines plus
/// three to six others; five to seven dots; connected; exactly two dots where an odd number of lines meet (so an
/// Euler path exists and must start at one of them - starting anywhere else is the wrong approach the puzzle asks the
/// player to see past); no line within <see cref="DotClearance"/> of a dot that is not its own end (so his feet on
/// a line are never also on a dot it does not touch); and no two lines that overlap or leave a shared dot at less
/// than <see cref="MinAngleDegrees"/> (so which line he is walking is never ambiguous inside the trace tolerance).
/// The pair and line-dot tests are precomputed once, so a candidate is a handful of bit tests and the whole family
/// is enumerated in milliseconds - there is no hand-kept table to drift from these rules.</para>
/// <para>A figure may be mirrored left to right, which moves its odd dots without changing a rule.</para>
/// <para>Coordinates are world px relative to the door's middle, y down, on the old door's outline
/// (DottedDoorEvent.HalfWidth, HalfHeight, ArchRadius), so the inked door that opens still matches the drawing.</para>
/// </remarks>
public sealed class DoorFigure
{
	public const float HalfWidth = 80f;
	public const float HalfHeight = 150f;
	public const float ArchRadius = 80f;
	public const float Spring = -HalfHeight + ArchRadius;

	/// <summary>No line passes nearer than this to a dot it does not end at.</summary>
	public const float DotClearance = 44f;

	/// <summary>Two lines leaving one dot part by at least this.</summary>
	public const float MinAngleDegrees = 30f;

	public const int MinLines = 7, MaxLines = 10, MinDots = 5, MaxDots = 7;

	// The dots: the jambs' feet, the arch's springs, its crown, the ring, the sill's middle, the keystone.
	public enum Dot { LeftFoot, RightFoot, LeftSpring, RightSpring, Crown, Ring, Sill, Keystone }
	private static readonly Vector2[] DotAt =
	{
		new(-HalfWidth, HalfHeight), new(HalfWidth, HalfHeight), new(-HalfWidth, Spring), new(HalfWidth, Spring),
		// The ring on the door's middle line: off it (36, 40) the family shrank to 30 figures, nearly all of seven
		// lines; centred, with the keystone, it is 317 with seven to ten lines evenly (measured 2026-11-07).
		new(0f, -HalfHeight), new(0f, 40f), new(0f, HalfHeight), new(0f, Spring),
	};
	public static int DotTotal => DotAt.Length;

	public enum Kind { Line, Arc }

	public readonly struct Edge
	{
		public readonly Dot A, B;
		public readonly Kind Kind;
		public Edge(Dot a, Dot b, Kind kind = Kind.Line) { A = a; B = b; Kind = kind; }
	}

	// The pool. The first four are the door's outline and are in every figure; the rest are what makes each figure its own.
	private static readonly Edge[] Pool =
	{
		new(Dot.LeftFoot, Dot.LeftSpring), new(Dot.RightFoot, Dot.RightSpring),
		new(Dot.LeftSpring, Dot.Crown, Kind.Arc), new(Dot.Crown, Dot.RightSpring, Kind.Arc),
		new(Dot.LeftSpring, Dot.RightSpring), new(Dot.LeftFoot, Dot.RightFoot),
		new(Dot.LeftFoot, Dot.RightSpring), new(Dot.RightFoot, Dot.LeftSpring),
		new(Dot.Ring, Dot.RightSpring), new(Dot.Ring, Dot.RightFoot), new(Dot.Ring, Dot.LeftFoot),
		new(Dot.Ring, Dot.LeftSpring), new(Dot.Ring, Dot.Crown),
		new(Dot.LeftFoot, Dot.Sill), new(Dot.Sill, Dot.RightFoot), new(Dot.Sill, Dot.Ring), new(Dot.Sill, Dot.Crown),
		new(Dot.LeftSpring, Dot.Keystone), new(Dot.Keystone, Dot.RightSpring), new(Dot.Crown, Dot.Keystone),
		new(Dot.Keystone, Dot.Ring), new(Dot.Keystone, Dot.Sill),
	};
	private const int CoreLines = 4;

	// --- the figure ----------------------------------------------------------------------------------------------

	private readonly int[] lines;     // pool indices
	private readonly bool mirrored;

	/// <summary>Its lines, in a fixed order; <see cref="Index"/> into the family that drew it.</summary>
	public int LineCount => lines.Length;
	public int Index { get; }
	public bool Mirrored => mirrored;

	private DoorFigure(int[] lines, bool mirrored, int index)
	{
		this.lines = lines;
		this.mirrored = mirrored;
		Index = index;
	}

	public Dot EndA(int line) => Pool[lines[line]].A;
	public Dot EndB(int line) => Pool[lines[line]].B;
	public Kind KindOf(int line) => Pool[lines[line]].Kind;

	public Vector2 DotPosition(Dot d) => Flip(DotAt[(int)d]);
	private Vector2 Flip(Vector2 p) => mirrored ? new Vector2(-p.X, p.Y) : p;

	/// <summary>The dots this figure uses.</summary>
	public IEnumerable<Dot> Dots
	{
		get
		{
			for (int d = 0; d < DotTotal; d++)
				if (Degree((Dot)d) > 0)
					yield return (Dot)d;
		}
	}

	public int Degree(Dot d)
	{
		int n = 0;
		foreach (int p in lines)
			if (Pool[p].A == d || Pool[p].B == d)
				n++;
		return n;
	}

	/// <summary>The other end of <paramref name="line"/> from <paramref name="from"/>.</summary>
	public Dot Other(int line, Dot from) => EndA(line) == from ? EndB(line) : EndA(line);

	public bool Touches(int line, Dot d) => EndA(line) == d || EndB(line) == d;

	/// <summary>The two dots an odd number of lines meet at: where a one-stroke drawing must begin (and end).</summary>
	public (Dot, Dot) OddDots
	{
		get
		{
			Dot first = Dot.LeftFoot, second = Dot.LeftFoot;
			int found = 0;
			for (int d = 0; d < DotTotal; d++)
				if (Degree((Dot)d) % 2 == 1)
				{
					if (found++ == 0) first = (Dot)d;
					else second = (Dot)d;
				}
			return (first, second);
		}
	}

	// --- geometry: a point along a line, its length, and the nearest point to his feet --------------------------

	/// <summary>The point <paramref name="t"/> (0 to 1) along <paramref name="line"/> from <paramref name="from"/>.</summary>
	public Vector2 Point(int line, Dot from, float t)
	{
		t = Mathf.Clamp(t, 0f, 1f);
		Edge e = Pool[lines[line]];
		Vector2 a = DotAt[(int)(from == e.A ? e.A : e.B)], b = DotAt[(int)(from == e.A ? e.B : e.A)];
		return Flip(e.Kind == Kind.Line ? a.Lerp(b, t) : ArcPoint(a, b, t));
	}

	public float Length(int line)
	{
		Edge e = Pool[lines[line]];
		if (e.Kind == Kind.Line)
			return DotAt[(int)e.A].DistanceTo(DotAt[(int)e.B]);
		(float a0, float a1) = ArcAngles(DotAt[(int)e.A], DotAt[(int)e.B]);
		return Mathf.Abs(a1 - a0) * ArchRadius;
	}

	/// <summary>The fraction along <paramref name="line"/> from <paramref name="from"/> nearest <paramref name="rel"/>,
	/// and how far <paramref name="rel"/> is from it.</summary>
	public float Nearest(int line, Dot from, Vector2 rel, out float distance)
	{
		Edge e = Pool[lines[line]];
		Vector2 p = Flip(rel);
		Vector2 a = DotAt[(int)(from == e.A ? e.A : e.B)], b = DotAt[(int)(from == e.A ? e.B : e.A)];
		if (e.Kind == Kind.Line)
		{
			Vector2 ab = b - a;
			float t = ab.LengthSquared() > 0f ? Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0f, 1f) : 0f;
			distance = p.DistanceTo(a.Lerp(b, t));
			return t;
		}
		(float a0, float a1) = ArcAngles(a, b);
		Vector2 c = new(0f, Spring);
		float ang = Mathf.Atan2(p.Y - c.Y, p.X - c.X);
		if (ang > 0f)
			ang = p.X < 0f ? -Mathf.Pi : 0f;          // below the springs: the nearest is an end
		float tt = Mathf.Clamp((ang - a0) / (a1 - a0), 0f, 1f);
		distance = p.DistanceTo(ArcPoint(a, b, tt));
		return tt;
	}

	// The arch's circle is centred between the springs; its lines run over the TOP (angles from -pi to 0, y down).
	private static (float, float) ArcAngles(Vector2 a, Vector2 b)
	{
		Vector2 c = new(0f, Spring);
		float a0 = Mathf.Atan2(a.Y - c.Y, a.X - c.X), a1 = Mathf.Atan2(b.Y - c.Y, b.X - c.X);
		if (a0 > 0.0001f) a0 -= Mathf.Tau;
		if (a1 > 0.0001f) a1 -= Mathf.Tau;
		return (a0, a1);
	}

	private static Vector2 ArcPoint(Vector2 a, Vector2 b, float t)
	{
		(float a0, float a1) = ArcAngles(a, b);
		float ang = Mathf.Lerp(a0, a1, t);
		return new Vector2(0f, Spring) + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * ArchRadius;
	}

	// --- the family ----------------------------------------------------------------------------------------------

	private static List<(int[] Lines, int Index)> family;
	private static bool[,] pairConflict;
	private static bool[,] dotConflict;

	/// <summary>Every figure the rules allow (unmirrored), found once.</summary>
	public static IReadOnlyList<(int[] Lines, int Index)> Family
	{
		get
		{
			if (family == null)
				BuildFamily();
			return family;
		}
	}

	/// <summary>The run's figure: one of the family, mirrored or not, by <paramref name="seed"/>.</summary>
	public static DoorFigure Roll(ulong seed)
	{
		IReadOnlyList<(int[] Lines, int Index)> all = Family;
		int pick = (int)(seed % (ulong)all.Count);
		bool mirror = ((seed >> 20) & 1UL) == 1UL;
		return new DoorFigure(all[pick].Lines, mirror, all[pick].Index);
	}

	/// <summary>Figure <paramref name="index"/> of the family, for the validator and the probes.</summary>
	public static DoorFigure At(int index, bool mirror) => new(Family[index].Lines, mirror, index);

	private static void BuildFamily()
	{
		int n = Pool.Length;
		// A throwaway figure over the whole pool, to measure the lines with the same code the game uses.
		int[] all = new int[n];
		for (int i = 0; i < n; i++) all[i] = i;
		var probe = new DoorFigure(all, false, -1);

		dotConflict = new bool[n, DotTotal];
		for (int i = 0; i < n; i++)
			for (int d = 0; d < DotTotal; d++)
			{
				if (probe.Touches(i, (Dot)d))
					continue;
				probe.Nearest(i, Pool[i].A, DotAt[d], out float dist);
				dotConflict[i, d] = dist < DotClearance;
			}

		pairConflict = new bool[n, n];
		for (int i = 0; i < n; i++)
			for (int j = i + 1; j < n; j++)
			{
				bool conflict = Overlap(probe, i, j);
				Dot? shared = Pool[i].A == Pool[j].A || Pool[i].A == Pool[j].B ? Pool[i].A
					: Pool[i].B == Pool[j].A || Pool[i].B == Pool[j].B ? Pool[i].B : null;
				if (!conflict && shared is Dot s)
				{
					float hi = Heading(probe, i, s), hj = Heading(probe, j, s);
					float diff = Mathf.Abs(Mathf.AngleDifference(hi, hj));
					conflict = Mathf.RadToDeg(diff) < MinAngleDegrees;
				}
				pairConflict[i, j] = pairConflict[j, i] = conflict;
			}

		family = new List<(int[], int)>();
		int extras = n - CoreLines;
		var chosen = new List<int>(MaxLines);
		for (int mask = 0; mask < (1 << extras); mask++)
		{
			int count = CoreLines + CountBits(mask);
			if (count < MinLines || count > MaxLines)
				continue;
			chosen.Clear();
			for (int i = 0; i < CoreLines; i++) chosen.Add(i);
			for (int i = 0; i < extras; i++)
				if ((mask & (1 << i)) != 0) chosen.Add(CoreLines + i);
			if (Keeps(chosen))
				family.Add((chosen.ToArray(), family.Count));
		}
	}

	private static bool Keeps(List<int> chosen)
	{
		Span<int> degree = stackalloc int[8];
		foreach (int i in chosen)
		{
			degree[(int)Pool[i].A]++;
			degree[(int)Pool[i].B]++;
		}
		int dots = 0, odd = 0;
		for (int d = 0; d < DotTotal; d++)
		{
			if (degree[d] == 0) continue;
			dots++;
			if (degree[d] % 2 == 1) odd++;
		}
		if (dots < MinDots || dots > MaxDots || odd != 2)
			return false;
		for (int a = 0; a < chosen.Count; a++)
		{
			for (int d = 0; d < DotTotal; d++)
				if (degree[d] > 0 && dotConflict[chosen[a], d])
					return false;
			for (int b = a + 1; b < chosen.Count; b++)
				if (pairConflict[chosen[a], chosen[b]])
					return false;
		}
		// Connected: a flood from the first line's end over the chosen lines reaches every used dot.
		int reached = 1 << (int)Pool[chosen[0]].A, used = 0;
		foreach (int i in chosen)
			used |= (1 << (int)Pool[i].A) | (1 << (int)Pool[i].B);
		bool grew = true;
		while (grew)
		{
			grew = false;
			foreach (int i in chosen)
			{
				int a = 1 << (int)Pool[i].A, b = 1 << (int)Pool[i].B;
				if (((reached & a) != 0) != ((reached & b) != 0))
				{
					reached |= a | b;
					grew = true;
				}
			}
		}
		return reached == used;
	}

	// Two lines overlap when a stretch of one, away from its ends, lies along the other.
	private static bool Overlap(DoorFigure probe, int i, int j)
	{
		int near = 0;
		for (int k = 4; k <= 36; k++)
		{
			Vector2 p = probe.Point(i, Pool[i].A, k / 40f);
			probe.Nearest(j, Pool[j].A, p, out float d);
			if (d < 20f)
				near++;
		}
		return near > 6;
	}

	private static float Heading(DoorFigure probe, int line, Dot from)
	{
		Vector2 a = probe.Point(line, from, 0f), b = probe.Point(line, from, 0.15f);
		return (b - a).Angle();
	}

	private static int CountBits(int v)
	{
		int c = 0;
		for (; v != 0; v &= v - 1) c++;
		return c;
	}

	// --- one stroke ----------------------------------------------------------------------------------------------

	/// <summary>
	/// An order to ink every line in one stroke from <paramref name="start"/> (Hierholzer's), as the lines to walk and
	/// the dot each is walked from; null if there is none - which, with exactly two odd dots, is every other start.
	/// For the probe (which walks it at his pace) and the validator (which proves it on paper).
	/// </summary>
	public List<(int Line, Dot From)> StrokeFrom(Dot start) => StrokeFrom(start, null);

	/// <summary>The same over the lines not yet <paramref name="done"/>: how a stroke already under way can finish
	/// from <paramref name="start"/> (its pen), or null if it cannot.</summary>
	public List<(int Line, Dot From)> StrokeFrom(Dot start, bool[] done)
	{
		var used = new bool[lines.Length];
		int left = 0;
		for (int l = 0; l < lines.Length; l++)
		{
			used[l] = done != null && done[l];
			if (!used[l]) left++;
		}
		if (left == 0)
			return new List<(int, Dot)>();
		bool touches = false;
		for (int l = 0; l < lines.Length; l++)
			if (!used[l] && Touches(l, start)) touches = true;
		if (!touches)
			return null;
		var stack = new Stack<(Dot At, int Via)>();
		var path = new List<(Dot At, int Via)>();
		stack.Push((start, -1));
		while (stack.Count > 0)
		{
			(Dot at, int via) = stack.Peek();
			int next = -1;
			for (int l = 0; l < lines.Length; l++)
				if (!used[l] && Touches(l, at)) { next = l; break; }
			if (next < 0)
			{
				path.Add(stack.Pop());
				continue;
			}
			used[next] = true;
			stack.Push((Other(next, at), next));
		}
		if (path.Count != left + 1)
			return null;
		path.Reverse();
		var stroke = new List<(int, Dot)>(lines.Length);
		// From a wrong start Hierholzer still hands back every line, but spliced: a line that does not leave the dot the
		// last one reached. Only an unbroken chain from `start` is a stroke.
		Dot reached = start;
		for (int k = 1; k < path.Count; k++)
		{
			int via = path[k].Via;
			if (!Touches(via, reached))
				return null;
			stroke.Add((via, reached));
			reached = Other(via, reached);
		}
		return stroke;
	}
}

public enum StrokeEvent { None, Begun, Inked, Smudged, Done }

/// <summary>
/// THE ONE-STROKE RULE (.ai/unlock-puzzles.md 3.2), one step at a time, pure: the game feeds it his feet each frame and
/// the validator feeds it walks on paper, so the rule the player meets and the rule that is proven are one.
/// </summary>
/// <remarks>
/// <para>Walking a line from dot to dot within <see cref="Tolerance"/> inks it. Before the stroke has begun any line
/// can start it; after, only a line leaving the dot the ink has reached (<see cref="Pen"/>) - it is ONE stroke, so
/// the insight is where to begin. Stepping off a line only pauses it: what is inked stays, and the line in hand keeps
/// how far it got. Walking ALONG a line already inked - <see cref="SmudgeShare"/> of it, away from its dots, which
/// is what separates walking it from crossing it - smudges the whole drawing back to charcoal, and the stroke starts
/// again anywhere.</para>
/// <para>A wobbly first step is forgiven: a line chosen less than <see cref="SwitchWithin"/> out from its dot gives
/// way to another from the same dot that his feet are nearer.</para>
/// </remarks>
public sealed class DoorStroke
{
	public const float Tolerance = 30f;
	public const float DotReach = 30f;
	/// <summary>3.2's open question at its first guess: walk 40% of an inked line along it and the drawing smudges.</summary>
	public const float SmudgeShare = 0.4f;
	public const float SwitchWithin = 60f;
	/// <summary>How far out along a line his feet must be before it is the one in hand (off the dot itself).</summary>
	public const float StartOut = 16f;
	public const float LookBack = 30f;
	public const float LookAhead = 120f;

	private readonly DoorFigure fig;
	private readonly bool[] inked;
	private int alongLine = -1;
	private float alongEntry;
	// The dot his feet last stood on. A stroke begins only out of it: on the shortest lines both ends are within reach
	// of the middle, and without it a walk out of one end could be read as a walk out of the other.
	private DoorFigure.Dot? lastDot;

	public DoorStroke(DoorFigure figure)
	{
		fig = figure;
		inked = new bool[figure.LineCount];
	}

	public DoorFigure Figure => fig;
	public bool IsInked(int line) => inked[line];

	/// <summary>A copy of which lines are inked, for planning the rest of a stroke (DoorFigure.StrokeFrom).</summary>
	public bool[] InkedLines() => (bool[])inked.Clone();
	public int InkedCount { get; private set; }
	public int Smudges { get; private set; }
	public bool Begun { get; private set; }
	public bool IsDone => InkedCount == fig.LineCount;

	/// <summary>The dot the ink has reached: where the stroke goes on from.</summary>
	public DoorFigure.Dot Pen { get; private set; }

	/// <summary>The line in hand, or -1; the dot it is walked from; how far along it the ink is (0 to 1).</summary>
	public int Active { get; private set; } = -1;
	public DoorFigure.Dot ActiveFrom { get; private set; }
	public float ActiveT { get; private set; }

	/// <summary>True while his feet are on the line in hand, or on a dot the stroke can go on from.</summary>
	public bool OnLine { get; private set; }

	/// <summary>Begun, unfinished, nothing in hand, and no uninked line left at the pen: the wrong start's dead end.</summary>
	public bool Stuck
	{
		get
		{
			if (!Begun || IsDone || Active >= 0)
				return false;
			for (int l = 0; l < fig.LineCount; l++)
				if (!inked[l] && fig.Touches(l, Pen))
					return false;
			return true;
		}
	}

	/// <summary>A dot every line into which is inked: it is drawn lit.</summary>
	public bool DotDone(DoorFigure.Dot d)
	{
		bool any = false;
		for (int l = 0; l < fig.LineCount; l++)
		{
			if (!fig.Touches(l, d)) continue;
			any = true;
			if (!inked[l]) return false;
		}
		return any;
	}

	public StrokeEvent Step(Vector2 feet)
	{
		OnLine = false;
		if (IsDone)
			return StrokeEvent.None;
		float nearest = DotReach;
		foreach (DoorFigure.Dot d in fig.Dots)
		{
			float dist = feet.DistanceTo(fig.DotPosition(d));
			if (dist <= nearest)
			{
				nearest = dist;
				lastDot = d;
			}
		}
		if (InkedCount > 0 && WalkedAlongInk(feet))
		{
			Smudge();
			return StrokeEvent.Smudged;
		}

		if (Active >= 0)
		{
			float len = fig.Length(Active);
			if (ActiveT * len < SwitchWithin)
			{
				int better = BestStart(ActiveFrom, feet, out float bt);
				if (better >= 0 && better != Active)
				{
					Active = better;
					ActiveT = bt;
					len = fig.Length(Active);
				}
			}
			float t = fig.Nearest(Active, ActiveFrom, feet, out float d);
			bool onActive = d <= Tolerance && t >= ActiveT - LookBack / len && t <= ActiveT + LookAhead / len;
			// Nothing inked yet: the stroke is not committed, so a walk out of another dot begins it afresh there - a
			// line brushed on the way to the chosen start costs nothing.
			if (!onActive && InkedCount == 0 && lastDot is DoorFigure.Dot again && again != ActiveFrom)
			{
				int fresh = BestStart(again, feet, out float ft);
				if (fresh >= 0)
				{
					Pen = again;
					Active = fresh;
					ActiveFrom = again;
					ActiveT = ft;
					OnLine = true;
					return StrokeEvent.Begun;
				}
			}
			if (onActive)
			{
				OnLine = true;
				ActiveT = Mathf.Max(ActiveT, t);
				if (ActiveT >= 1f - DotReach / len)
				{
					inked[Active] = true;
					InkedCount++;
					Pen = fig.Other(Active, ActiveFrom);
					Active = -1;
					ActiveT = 0f;
					return IsDone ? StrokeEvent.Done : StrokeEvent.Inked;
				}
			}
			return StrokeEvent.None;
		}

		// Nothing in hand: a line out of the pen's dot, or before it has begun, out of any dot.
		if (Begun)
		{
			OnLine = feet.DistanceTo(fig.DotPosition(Pen)) <= DotReach;
			int line = BestStart(Pen, feet, out float t);
			if (line >= 0)
			{
				Active = line;
				ActiveFrom = Pen;
				ActiveT = t;
				OnLine = true;
			}
			return StrokeEvent.None;
		}
		if (lastDot is DoorFigure.Dot start)
		{
			int line = BestStart(start, feet, out float t);
			if (line >= 0)
			{
				Begun = true;
				Pen = start;
				Active = line;
				ActiveFrom = start;
				ActiveT = t;
				OnLine = true;
				return StrokeEvent.Begun;
			}
		}
		return StrokeEvent.None;
	}

	// The uninked line out of `from` his feet are walking out along: on the line, in its first stretch from `from` -
	// off the dot itself, but no further than SwitchWithin. Never its far end: on the shortest lines (80 px) the far dot
	// is near enough to `from` that standing on it would otherwise start, and at once ink, a line never walked.
	private int BestStart(DoorFigure.Dot from, Vector2 feet, out float bestT)
	{
		bestT = 0f;
		if (feet.DistanceTo(fig.DotPosition(from)) > SwitchWithin + Tolerance)
			return -1;
		int best = -1;
		float bestD = Tolerance;
		for (int l = 0; l < fig.LineCount; l++)
		{
			if (inked[l] || !fig.Touches(l, from)) continue;
			float t = fig.Nearest(l, from, feet, out float d);
			float out_ = t * fig.Length(l);
			if (d > bestD || out_ < StartOut || out_ > SwitchWithin) continue;
			best = l;
			bestD = d;
			bestT = t;
		}
		return best;
	}

	// Along an inked line, away from its dots, for SmudgeShare of it in one go. A crossing barely moves along it. The
	// guard at each end is a fifth of the line at most: the shortest lines are 80 px, and a full DotReach at both ends
	// would leave a 20 px middle no walk could travel 40% of, so walking back along one would never smudge.
	private bool WalkedAlongInk(Vector2 feet)
	{
		int best = -1;
		float bestT = 0f, bestD = Tolerance * 0.8f;
		for (int l = 0; l < fig.LineCount; l++)
		{
			if (!inked[l]) continue;
			float t = fig.Nearest(l, fig.EndA(l), feet, out float d);
			float len = fig.Length(l);
			float guard = Mathf.Min(DotReach, len * 0.2f);
			if (d > bestD || t * len < guard || (1f - t) * len < guard) continue;
			best = l;
			bestD = d;
			bestT = t;
		}
		if (best < 0)
		{
			alongLine = -1;
			return false;
		}
		if (best != alongLine)
		{
			alongLine = best;
			alongEntry = bestT;
			return false;
		}
		return Mathf.Abs(bestT - alongEntry) >= SmudgeShare;
	}

	/// <summary>Back to charcoal: nothing inked, nothing in hand, the stroke not begun.</summary>
	public void Smudge()
	{
		for (int l = 0; l < inked.Length; l++)
			inked[l] = false;
		InkedCount = 0;
		Active = -1;
		ActiveT = 0f;
		Begun = false;
		alongLine = -1;
		Smudges++;
	}

	/// <summary>For the tools: the first <paramref name="count"/> lines of a true stroke inked, the pen at its end.</summary>
	public void DebugInk(int count)
	{
		(DoorFigure.Dot odd, _) = fig.OddDots;
		List<(int Line, DoorFigure.Dot From)> stroke = fig.StrokeFrom(odd);
		if (stroke == null)
			return;
		Smudge();
		Smudges = 0;
		Begun = true;
		Pen = odd;
		for (int k = 0; k < Mathf.Min(count, stroke.Count); k++)
		{
			inked[stroke[k].Line] = true;
			InkedCount++;
			Pen = fig.Other(stroke[k].Line, stroke[k].From);
		}
	}
}
