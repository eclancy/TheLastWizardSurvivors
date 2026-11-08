using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// THE THAW'S KANTELE (.ai/unlock-puzzles.md 3.6, "sing it back"): five strings of ice stretched over the frozen lake
// round the block Vainamoinen stands in - the instrument he made in the legend, the old five-string one, a string to
// a note - and the phrases the ice sings on them. Plain values and two small classes with no node, so the validator
// plays the whole thing on paper (RegressionChecks.ValidateThawStrings) and ThawEvent plays the same code in a run.
//
// THE STRINGS. They fan out from the block to the shore at fixed bearings, low to high clockwise from the lower left:
// A3 D4 F4 C5 A5 (tools/audio/sfx.py _KANTELE), each in its own colour (fire, lightning, grass, water, arcane - low to
// high, as light's frequency runs), the lowest the thickest. The colour is on each string's tuning pin and its shore
// stake at rest (tools/art/kantele.py), and the whole string lights in it while it sounds, so the puzzle is read with
// the sound off; the pitch says the same thing to anyone listening. Nothing anywhere says which string comes first.
//
// A STRING COUNTS WHERE HIS FEET TOUCH IT: the feet's own motion since the last frame, tested as a segment against the
// string (StringFeet), so a step over one in a single long frame is still a touch and a slow frame tunnels nothing. A
// string touched cannot sound again until the feet have been RearmDistance off it, so standing on one is one note.
// Only the feet: an enemy on a string is nothing.
//
// THE WAY ROUND. The strings stop InnerRadius short of the block, and that hub is open ice: from any string he can
// walk in to the block and out to any other without touching a third. Round the outside, past the stakes, is open too
// and longer. Walking round the lake crosses them in turn, which is the wrong answer: every phrase has a LEAP in it
// (two strings that do not lie side by side), so no walk round in a circle sings one back.
//
// THE PHRASES (the seed). Three, of three, four and five notes, each the last with a note added (3.6's first guess):
// one song of five notes, sung back three, four and five notes long. Picked by one roll from every song that keeps
// the rules (Fault): no string twice running, the first phrase three different strings with a leap in it, at least
// four of the five strings in the whole song.
public static class Kantele
{
	public const int StringCount = 5;

	/// <summary>The three phrases, each the last with a note added: one song of SongLength notes, sung back this long.</summary>
	public static readonly int[] PhraseLengths = { 3, 4, 5 };
	public const int PhraseCount = 3;
	public const int SongLength = 5;

	// tools/art/kantele.py ANGLES, INNER (x2), OUTER - change one, change both. Degrees in screen space (y down), so
	// increasing is clockwise: lower left, upper left, upper right, lower right, straight down. The two upper strings
	// flank the block's top rather than run behind it.
	private static readonly float[] AnglesDeg = { 162f, 234f, 306f, 18f, 90f };

	/// <summary>The tuning pins: where the strings begin, this far from the block's foot. The hub inside it is open ice.</summary>
	public const float InnerRadius = 80f;

	/// <summary>The shore stakes, as a share of the lake's ellipse along the string (the shore is never under 0.92).</summary>
	public const float OuterFraction = 0.88f;

	/// <summary>Feet within this of a string's line are on it.</summary>
	public const float TouchHalfWidth = 12f;

	/// <summary>A string touched sounds again only once the feet have been this far off it.</summary>
	public const float RearmDistance = 30f;

	/// <summary>Feet that move further than this in one frame were moved, not walked: nothing is touched on the way.</summary>
	public const float TeleportDistance = 600f;

	/// <summary>Each string's sound, low to high (tools/audio/sfx.py ice_string_N).</summary>
	public static readonly string[] Notes =
		{ SfxCatalog.IceString1, SfxCatalog.IceString2, SfxCatalog.IceString3, SfxCatalog.IceString4, SfxCatalog.IceString5 };

	/// <summary>
	/// Each string's light, as the contract's emissive ramps (bonelight.py ELEMENTS): its core, hot, mid and edge.
	/// Fire, lightning, grass, water, arcane - the pins and stakes tools/art/kantele.py draws are the same.
	/// </summary>
	private static readonly Color[][] Ramps =
	{
		new[] { new Color("FFF4D0"), new Color("FFC848"), new Color("E87A1A"), new Color("B8381A") },
		new[] { new Color("FFFFF0"), new Color("FFF8B0"), new Color("FFF06A"), new Color("E0B02A") },
		new[] { new Color("F4FFDC"), new Color("CFEE94"), new Color("9BD455"), new Color("4A8C3A") },
		new[] { new Color("F0FAFF"), new Color("B4DCFF"), new Color("6FB8F0"), new Color("2A5FC4") },
		new[] { new Color("FFF0FF"), new Color("E8C0FF"), new Color("C08CFF"), new Color("7A3FD4") },
	};

	public static Color Core(int i) => Ramps[i][0];
	public static Color Hot(int i) => Ramps[i][1];
	public static Color Mid(int i) => Ramps[i][2];
	public static Color Edge(int i) => Ramps[i][3];

	/// <summary>Each string's thickness in the world (kantele.py WIDTHS at x2): the lowest the thickest.</summary>
	public static float Width(int i) => i < 2 ? 6f : 4f;

	/// <summary>The lake's ellipse radius along a bearing (ThawLake.WorldAxes, the shore before its bays and points).</summary>
	public static float EllipseRadius(float angle)
	{
		float a = ThawLake.WorldAxes.X, b = ThawLake.WorldAxes.Y;
		float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
		return a * b / Mathf.Sqrt(b * c * b * c + a * s * a * s);
	}

	public static float Angle(int i) => Mathf.DegToRad(AnglesDeg[i]);
	public static Vector2 Direction(int i) => Vector2.FromAngle(Angle(i));
	public static float OuterRadius(int i) => OuterFraction * EllipseRadius(Angle(i));

	/// <summary>String <paramref name="i"/>'s tuning pin, from the block's foot.</summary>
	public static Vector2 Inner(int i) => Direction(i) * InnerRadius;

	/// <summary>String <paramref name="i"/>'s shore stake, from the block's foot.</summary>
	public static Vector2 Outer(int i) => Direction(i) * OuterRadius(i);

	/// <summary>A point <paramref name="u"/> of the way along string <paramref name="i"/>, pin (0) to stake (1).</summary>
	public static Vector2 Along(int i, float u) => Inner(i).Lerp(Outer(i), u);

	public static Vector2 Middle(int i) => Along(i, 0.5f);

	/// <summary>How far <paramref name="p"/> (from the block) is from string <paramref name="i"/>.</summary>
	public static float DistanceTo(int i, Vector2 p)
	{
		Vector2 a = Inner(i), b = Outer(i);
		Vector2 ab = b - a;
		float t = Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0f, 1f);
		return p.DistanceTo(a + ab * t);
	}

	/// <summary>How far along string <paramref name="i"/> the point nearest <paramref name="p"/> is, 0 at the pin.</summary>
	public static float AlongOf(int i, Vector2 p)
	{
		Vector2 a = Inner(i), ab = Outer(i) - a;
		return Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0f, 1f);
	}

	/// <summary>
	/// Whether feet moving from <paramref name="from"/> to <paramref name="to"/> (from the block) touch string
	/// <paramref name="i"/>: the two segments come within TouchHalfWidth. <paramref name="when"/> is how far through the
	/// move (0-1) they come nearest, so two touched in one frame are heard in the order he reached them.
	/// </summary>
	public static bool Touches(int i, Vector2 from, Vector2 to, out float when)
	{
		ClosestBetween(from, to, Inner(i), Outer(i), out when, out _, out float distance);
		return distance <= TouchHalfWidth;
	}

	// The closest points of two segments (Ericson, Real-Time Collision Detection 5.1.9): s along p1-q1, t along p2-q2.
	private static void ClosestBetween(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2, out float s, out float t, out float distance)
	{
		Vector2 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
		float a = d1.LengthSquared(), e = d2.LengthSquared(), f = d2.Dot(r);
		if (a <= 1e-6f && e <= 1e-6f)
		{
			s = t = 0f;
			distance = p1.DistanceTo(p2);
			return;
		}
		if (a <= 1e-6f)
		{
			s = 0f;
			t = Mathf.Clamp(f / e, 0f, 1f);
		}
		else
		{
			float c = d1.Dot(r);
			if (e <= 1e-6f)
			{
				t = 0f;
				s = Mathf.Clamp(-c / a, 0f, 1f);
			}
			else
			{
				float b = d1.Dot(d2);
				float denom = a * e - b * b;
				s = denom > 1e-6f ? Mathf.Clamp((b * f - c * e) / denom, 0f, 1f) : 0f;
				t = (b * s + f) / e;
				if (t < 0f)
				{
					t = 0f;
					s = Mathf.Clamp(-c / a, 0f, 1f);
				}
				else if (t > 1f)
				{
					t = 1f;
					s = Mathf.Clamp((b - c) / a, 0f, 1f);
				}
			}
		}
		distance = (p1 + d1 * s).DistanceTo(p2 + d2 * t);
	}

	// --- the phrases ----------------------------------------------------------------------------------

	/// <summary>How many strings apart two are round the block: 1 side by side, 2 a leap (with five, nothing is further).</summary>
	public static int Apart(int a, int b)
	{
		int d = Math.Abs(a - b) % StringCount;
		return Math.Min(d, StringCount - d);
	}

	/// <summary>Why <paramref name="song"/> breaks the rules, or null if it keeps them (see the header).</summary>
	public static string Fault(IReadOnlyList<int> song)
	{
		if (song == null || song.Count != SongLength)
			return $"a song is {SongLength} notes";
		for (int k = 0; k < SongLength; k++)
		{
			if (song[k] < 0 || song[k] >= StringCount)
				return $"note {k} is string {song[k]}, not one of the {StringCount}";
			if (k > 0 && song[k] == song[k - 1])
				return $"notes {k - 1} and {k} are the same string";
		}
		if (song[0] == song[2])
			return "the first phrase uses a string twice";
		if (Apart(song[0], song[1]) < 2 && Apart(song[1], song[2]) < 2)
			return "the first phrase has no leap: walking round the lake would sing it";
		int used = 0;
		for (int s = 0; s < StringCount; s++)
		{
			for (int k = 0; k < SongLength; k++)
			{
				if (song[k] == s)
				{
					used++;
					break;
				}
			}
		}
		if (used < StringCount - 1)
			return $"the song uses {used} strings, fewer than {StringCount - 1}";
		return null;
	}

	private static int[][] songs;

	/// <summary>Every song that keeps the rules, in a fixed order: what a roll picks from.</summary>
	public static IReadOnlyList<int[]> AllSongs
	{
		get
		{
			if (songs != null)
				return songs;
			var found = new List<int[]>();
			var song = new int[SongLength];
			int total = 1;
			for (int k = 0; k < SongLength; k++)
				total *= StringCount;
			for (int code = 0; code < total; code++)
			{
				int c = code;
				for (int k = SongLength - 1; k >= 0; k--)
				{
					song[k] = c % StringCount;
					c /= StringCount;
				}
				if (Fault(song) == null)
					found.Add((int[])song.Clone());
			}
			songs = found.ToArray();
			return songs;
		}
	}

	/// <summary>The song a roll (0-1) picks: every song that keeps the rules is as likely.</summary>
	public static int[] SongFromRoll(float roll)
	{
		IReadOnlyList<int[]> all = AllSongs;
		int index = Math.Clamp((int)(roll * all.Count), 0, all.Count - 1);
		return (int[])all[index].Clone();
	}

	// --- the way round ---------------------------------------------------------------------------------

	/// <summary>How far either side of a string the way to it comes in and goes out (Route), degrees.</summary>
	public const float CrossSpreadDegrees = 18f;

	/// <summary>Where the way to string <paramref name="i"/> crosses it: half way along it.</summary>
	public static float CrossRadius(int i) => (InnerRadius + OuterRadius(i)) * 0.5f;

	/// <summary>
	/// A way for the feet from <paramref name="from"/> (from the block) to string <paramref name="target"/> that touches
	/// it and nothing else, written into <paramref name="into"/> (at least four long); returns how many points. What a
	/// player who has seen the strings does, and what the probe walks: into the middle of the gap between the two strings
	/// he stands between, in along it to the block - the hub, open ice - out beside the target, and across it. Each leg
	/// stays inside one gap or the hub, so it touches nothing on the way (RegressionChecks.ValidateThawStrings walks it).
	/// </summary>
	public static int Route(Vector2 from, int target, Vector2[] into)
	{
		int n = 0;
		float r = from.Length();
		if (r > 4f)
		{
			// The gap he stands in: between the string at or before his bearing and the next one round.
			float a = from.Angle();
			float middle = GapMiddle(a);
			if (r > InnerRadius * 0.75f)
				into[n++] = Vector2.FromAngle(middle) * r;
			into[n++] = Vector2.Zero;
		}
		float spread = Mathf.DegToRad(CrossSpreadDegrees);
		float rc = CrossRadius(target);
		into[n++] = Vector2.FromAngle(Angle(target) + spread) * rc;
		into[n++] = Vector2.FromAngle(Angle(target) - spread) * rc;
		return n;
	}

	/// <summary>The bearing half way between the two strings either side of <paramref name="angle"/>.</summary>
	public static float GapMiddle(float angle)
	{
		float best = 0f, bestAhead = float.MaxValue;
		for (int i = 0; i < StringCount; i++)
		{
			// How far round (clockwise) from string i the bearing is; the string it is least far round from is the
			// one before it, and the gap's middle is half a gap on from that string.
			float ahead = Mathf.PosMod(angle - Angle(i), Mathf.Tau);
			if (ahead < bestAhead)
			{
				bestAhead = ahead;
				best = Angle(i);
			}
		}
		// The next string round from that one, however far (the five are not evenly spaced in every build).
		float next = float.MaxValue;
		for (int i = 0; i < StringCount; i++)
		{
			float gap = Mathf.PosMod(Angle(i) - best, Mathf.Tau);
			if (gap > 1e-4f && gap < next)
				next = gap;
		}
		return best + next * 0.5f;
	}
}

/// <summary>
/// The feet on the strings (Kantele): fed where the feet are each frame, says which strings they touched since the
/// last, in the order they reached them. A string touched is spent until the feet are RearmDistance off it. Fixed
/// arrays: nothing is allocated per frame.
/// </summary>
public sealed class StringFeet
{
	private readonly bool[] armed = new bool[Kantele.StringCount];
	private readonly int[] hit = new int[Kantele.StringCount];
	private readonly float[] hitWhen = new float[Kantele.StringCount];
	private readonly float[] hitAlong = new float[Kantele.StringCount];
	private Vector2 last;
	private bool placed;

	/// <summary>How many strings the last Step touched, and which, in order.</summary>
	public int Count { get; private set; }
	public int Hit(int k) => hit[k];

	/// <summary>Where along the string (0 at the pin) the feet touched it: where its light runs out from.</summary>
	public float Along(int k) => hitAlong[k];

	/// <summary>The feet are at <paramref name="at"/> without having walked there: every string under them is spent.</summary>
	public void Place(Vector2 at)
	{
		last = at;
		placed = true;
		Count = 0;
		for (int i = 0; i < Kantele.StringCount; i++)
			armed[i] = Kantele.DistanceTo(i, at) > Kantele.RearmDistance;
	}

	/// <summary>The feet are at <paramref name="at"/> (from the block) now; returns how many strings they touched on the way.</summary>
	public int Step(Vector2 at)
	{
		Count = 0;
		if (!placed || last.DistanceSquaredTo(at) > Kantele.TeleportDistance * Kantele.TeleportDistance)
		{
			Place(at);
			return 0;
		}
		for (int i = 0; i < Kantele.StringCount; i++)
		{
			if (!armed[i])
			{
				if (Kantele.DistanceTo(i, at) > Kantele.RearmDistance)
					armed[i] = true;
				continue;
			}
			if (!Kantele.Touches(i, last, at, out float when))
				continue;
			armed[i] = false;
			// In the order he reached them: an insertion into a list of at most five.
			int k = Count++;
			while (k > 0 && hitWhen[k - 1] > when)
			{
				hit[k] = hit[k - 1];
				hitWhen[k] = hitWhen[k - 1];
				hitAlong[k] = hitAlong[k - 1];
				k--;
			}
			hit[k] = i;
			hitWhen[k] = when;
			hitAlong[k] = Kantele.AlongOf(i, last.Lerp(at, when));
		}
		last = at;
		return Count;
	}
}

/// <summary>What a string touched in turn did to the answer.</summary>
public enum SingOutcome
{
	/// <summary>The next note of the phrase.</summary>
	Right,
	/// <summary>Not the next note: the phrase is to be sung again, and nothing else is lost.</summary>
	Wrong,
	/// <summary>The phrase's last note: one phrase sung back, the next to come.</summary>
	PhraseSung,
	/// <summary>The last phrase's last note: the song is sung back.</summary>
	SongSung,
	/// <summary>The song was already sung back.</summary>
	Over,
}

/// <summary>
/// The answer, on paper (Kantele): which phrase is being sung back and how much of it so far. A wrong note empties the
/// phrase; a phrase sung back stays sung. ThawEvent and RegressionChecks.ValidateThawStrings play the same one.
/// </summary>
public sealed class SingBack
{
	private readonly int[] song;

	public SingBack(IReadOnlyList<int> song)
	{
		this.song = new int[Kantele.SongLength];
		for (int k = 0; k < Kantele.SongLength; k++)
			this.song[k] = song[k];
	}

	/// <summary>Phrases sung back so far, 0 to PhraseCount; the one being sung back is this one.</summary>
	public int Phrase { get; private set; }

	/// <summary>Notes of the phrase sung back so far.</summary>
	public int Answered { get; private set; }

	public bool Done => Phrase >= Kantele.PhraseCount;

	/// <summary>How many notes the phrase being sung back has.</summary>
	public int Length => Kantele.PhraseLengths[Math.Min(Phrase, Kantele.PhraseCount - 1)];

	/// <summary>The phrase's note <paramref name="k"/>.</summary>
	public int Note(int k) => song[Math.Clamp(k, 0, Kantele.SongLength - 1)];

	/// <summary>String <paramref name="s"/> touched while the ice listens.</summary>
	public SingOutcome Pluck(int s)
	{
		if (Done)
			return SingOutcome.Over;
		if (s != song[Answered])
		{
			Answered = 0;
			return SingOutcome.Wrong;
		}
		if (++Answered < Length)
			return SingOutcome.Right;
		Answered = 0;
		Phrase++;
		return Done ? SingOutcome.SongSung : SingOutcome.PhraseSung;
	}

	/// <summary>The phrase is sung again: what was answered of it is gone, what was sung back stays.</summary>
	public void Restart() => Answered = 0;

	/// <summary>_UiShot: the answer posed at <paramref name="phrase"/> with <paramref name="answered"/> of it sung back.</summary>
	public void Pose(int phrase, int answered)
	{
		Phrase = Math.Clamp(phrase, 0, Kantele.PhraseCount);
		Answered = Done ? 0 : Math.Clamp(answered, 0, Length - 1);
	}
}
