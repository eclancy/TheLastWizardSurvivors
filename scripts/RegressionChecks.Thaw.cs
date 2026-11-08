using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// Vainamoinen (.ai/unlock-puzzles.md 3.6, "sing it back"): the thaw's kantele, played on paper. Where the strings lie,
// that the art draws them there, that their notes exist, and above all that every song the seed can pick is sung back
// by walking the way Kantele.Route lays - touching each string the phrase names and nothing else - while a wrong string
// and a walk round the lake both fail. A run shows one song once; this shows them all. Every line is a warning, as
// the rest of RunAll's are.
public static partial class RegressionChecks
{
	// Rolls laid out on paper: enough to cover the songs' shapes, few enough to stay a fraction of RunAll.
	private const int ThawSongsChecked = 64;
	// The walk on paper steps this far a frame: a player at full speed on a 60 fps frame, roughly.
	private const float ThawWalkStep = 6f;

	private static void ValidateThawStrings(List<string> warnings)
	{
		IReadOnlyList<SideEventEntry> waste = SideEventDirector.TableEventsFor("stage_5");
		if (waste.Count != 1 || waste[0].SiteId != "the_thaw")
			warnings.Add("Thaw: Vainamoinen's thaw must remain the Frozen Waste's one event (site the_thaw).");

		// --- where the strings lie --------------------------------------------------------------------------
		for (int i = 0; i < Kantele.StringCount; i++)
		{
			float length = Kantele.Inner(i).DistanceTo(Kantele.Outer(i));
			if (Kantele.OuterRadius(i) > 0.9f * Kantele.EllipseRadius(Kantele.Angle(i)) || length < 120f)
				warnings.Add($"Thaw: string {i} runs {length:0} px to {Kantele.OuterRadius(i):0} px out; it must be at least 120 px long "
					+ "and its stake inside the shore (0.9 of the lake's ellipse; the shore's bays come in to 0.92).");
			for (int j = i + 1; j < Kantele.StringCount; j++)
			{
				// Two strings come nearest at their pins; feet must fit between them without touching either.
				float apart = Kantele.Inner(i).DistanceTo(Kantele.Inner(j));
				if (apart < 2f * Kantele.TouchHalfWidth + 24f)
					warnings.Add($"Thaw: strings {i} and {j} begin {apart:0} px apart; feet could not pass between them untouched.");
			}
		}
		if (Kantele.InnerRadius < 64f)
			warnings.Add($"Thaw: the strings begin {Kantele.InnerRadius} px from the block, inside its foot; the hub must be open ice.");
		if (Kantele.RearmDistance <= Kantele.TouchHalfWidth)
			warnings.Add("Thaw: a string must be left further than it is touched before it can sound again, or standing on one rings it.");

		// --- the art draws them where the code lays them ------------------------------------------------------
		string lakePath = ThawLake.Directory + "thaw-lake.png";
		string stringsPath = ThawLake.Directory + ThawLake.StringsFile;
		Texture2D lakeTexture = ResourceLoader.Exists(lakePath) ? GD.Load<Texture2D>(lakePath) : null;
		Texture2D stringsTexture = ResourceLoader.Exists(stringsPath) ? GD.Load<Texture2D>(stringsPath) : null;
		if (stringsTexture == null)
			warnings.Add($"Thaw: {stringsPath} is missing or not imported (python tools/art/kantele.py, then import).");
		else if (lakeTexture != null && stringsTexture.GetSize() != lakeTexture.GetSize())
			warnings.Add($"Thaw: thaw-strings.png is {stringsTexture.GetSize()}, but the lake is {lakeTexture.GetSize()}; it is laid over it texel for texel.");
		else if (stringsTexture.GetImage() is Image image)
		{
			Vector2 centre = new Vector2(image.GetWidth(), image.GetHeight()) / 2f;
			for (int i = 0; i < Kantele.StringCount; i++)
			{
				int missing = 0;
				for (int k = 0; k <= 8; k++)
				{
					Vector2 p = centre + Kantele.Along(i, 0.05f + 0.1125f * k) / 2f;
					if (!OpaqueNear(image, p))
						missing++;
				}
				if (missing > 0)
					warnings.Add($"Thaw: string {i} is not drawn where Kantele lays it ({missing} of 9 points along it bare); "
						+ "tools/art/kantele.py ANGLES, INNER and OUTER must match Kantele.");
			}
		}

		// --- the notes ---------------------------------------------------------------------------------------
		var known = new HashSet<string>(SfxCatalog.AllNames);
		foreach (string note in Kantele.Notes)
		{
			if (!known.Contains(note) || !ResourceLoader.Exists(SfxCatalog.PathFor(note)))
				warnings.Add($"Thaw: the string note {note} is not in SfxCatalog or not on disk (python tools/audio/build.py ice_string).");
		}
		if (!known.Contains(SfxCatalog.IceStringWrong) || !ResourceLoader.Exists(SfxCatalog.PathFor(SfxCatalog.IceStringWrong)))
			warnings.Add("Thaw: ice_string_wrong is not in SfxCatalog or not on disk.");

		// --- the songs: every one the seed can pick keeps the rules --------------------------------------------
		IReadOnlyList<int[]> songs = Kantele.AllSongs;
		if (songs.Count < 100)
			warnings.Add($"Thaw: only {songs.Count} songs keep the rules; the seed needs more than a guide could list.");
		foreach (int[] song in songs)
		{
			if (Kantele.Fault(song) is string fault)
			{
				warnings.Add($"Thaw: the song {string.Join(" ", song)} is offered but breaks a rule: {fault}.");
				break;
			}
		}
		if (Kantele.Fault(new[] { 0, 1, 2, 3, 4 }) == null || Kantele.Fault(new[] { 2, 3, 4, 0, 1 }) == null)
			warnings.Add("Thaw: a song that walks round the lake string by string must be refused.");

		var way = new Vector2[4];
		var feet = new StringFeet();
		int stray = 0, unsung = 0;
		float longest = 0f;
		for (int r = 0; r < ThawSongsChecked; r++)
		{
			int[] song = Kantele.SongFromRoll((r + 0.5f) / ThawSongsChecked);
			if (Kantele.Fault(song) is string fault)
			{
				warnings.Add($"Thaw: roll {r} picked {string.Join(" ", song)}, which breaks a rule: {fault}.");
				continue;
			}

			// Sung back, walked: from where the arrow brings him (the block's top), each note by Kantele.Route, every
			// touch on the way heard. Exactly the notes, in order, and nothing else.
			var answer = new SingBack(song);
			Vector2 at = new(0f, -108f);
			feet.Place(at);
			float walked = 0f;
			for (int phrase = 0; phrase < Kantele.PhraseCount && stray == 0; phrase++)
			{
				answer.Restart();
				for (int k = 0; k < Kantele.PhraseLengths[phrase]; k++)
				{
					int legs = Kantele.Route(at, song[k], way);
					int touched = 0;
					for (int leg = 0; leg < legs; leg++)
					{
						while (at != way[leg])
						{
							Vector2 next = at.MoveToward(way[leg], ThawWalkStep);
							walked += at.DistanceTo(next);
							at = next;
							int hits = feet.Step(at);
							for (int h = 0; h < hits; h++)
							{
								touched++;
								SingOutcome o = answer.Pluck(feet.Hit(h));
								if (o == SingOutcome.Wrong || feet.Hit(h) != song[k])
									stray++;
							}
						}
					}
					if (touched != 1)
						stray++;
				}
				if (answer.Phrase != phrase + 1)
					unsung++;
			}
			if (stray > 0)
			{
				warnings.Add($"Thaw: walking Kantele.Route for the song {string.Join(" ", song)} touched a string it was not singing, "
					+ "or missed one; the way through the hub must touch only the string it is for.");
				break;
			}
			if (!answer.Done)
				unsung++;
			longest = Mathf.Max(longest, walked);

			// A wrong string fails, and costs only the phrase being sung back: never a phrase already sung back.
			var wrong = new SingBack(song);
			int notFirst = (song[0] + 2) % Kantele.StringCount;
			if (wrong.Pluck(notFirst) != SingOutcome.Wrong || wrong.Answered != 0 || wrong.Phrase != 0)
				warnings.Add($"Thaw: string {notFirst} first, for the song {string.Join(" ", song)}, must be wrong and empty the phrase.");
			for (int k = 0; k < Kantele.PhraseLengths[0]; k++)
				wrong.Pluck(song[k]);
			wrong.Pluck(song[0]);
			if (wrong.Pluck(song[0]) != SingOutcome.Wrong || wrong.Phrase != 1 || wrong.Answered != 0)
				warnings.Add($"Thaw: a wrong string in the second phrase must empty that phrase and keep the first sung back.");
		}
		if (unsung > 0)
			warnings.Add($"Thaw: {unsung} phrase(s) of the {ThawSongsChecked} songs walked were not sung back by notes that were all right.");
		// Twelve notes at most a few hundred px each: if the way grows past this, the hub or the route has broken.
		if (longest > 7000f)
			warnings.Add($"Thaw: singing a whole song back by Kantele.Route took {longest:0} px of walking; the strings or the way are too far apart.");

		// --- a walk round the lake sings nothing back ----------------------------------------------------------
		// Round the block at mid-string, both ways, two turns, every string it crosses taken as a note and the phrase
		// begun again after each wrong one - stricter than a run, where the ice sings again before it listens.
		for (int dir = -1; dir <= 1; dir += 2)
		{
			for (int r = 0; r < ThawSongsChecked; r++)
			{
				var answer = new SingBack(Kantele.SongFromRoll((r + 0.5f) / ThawSongsChecked));
				Vector2 at = Vector2.FromAngle(0.1f) * 150f;
				feet.Place(at);
				for (int step = 1; step <= 2 * 180; step++)
				{
					at = Vector2.FromAngle(0.1f + dir * step * Mathf.Tau / 180f) * 150f;
					int hits = feet.Step(at);
					for (int h = 0; h < hits; h++)
						answer.Pluck(feet.Hit(h));
				}
				if (answer.Phrase > 0)
				{
					warnings.Add($"Thaw: walking round the lake ({(dir > 0 ? "clockwise" : "anticlockwise")}) sang back a phrase of "
						+ $"{string.Join(" ", Kantele.SongFromRoll((r + 0.5f) / ThawSongsChecked))}; every phrase needs a leap.");
					break;
				}
			}
		}

		// --- the feet: one note a visit, a teleport touches nothing, a long frame tunnels nothing --------------
		{
			Vector2 on = Kantele.Middle(2);
			Vector2 across = (Kantele.Outer(2) - Kantele.Inner(2)).Orthogonal().Normalized();
			feet.Place(on + across * 60f);
			int rings = 0;
			rings += feet.Step(on);
			for (int k = 0; k < 20; k++)
				rings += feet.Step(on + across * ((k % 2 == 0 ? 1f : -1f) * 6f));
			if (rings != 1)
				warnings.Add($"Thaw: stepping onto a string and shuffling on it rang it {rings} times; it must ring once a visit.");
			feet.Place(Kantele.Middle(0) + new Vector2(2000f, 0f));
			if (feet.Step(Kantele.Middle(0)) != 0)
				warnings.Add("Thaw: feet moved further than a teleport in one frame must touch nothing on the way.");
			// One long frame straight over a string (a 2 fps machine at 4x) is still a touch.
			Vector2 mid = Kantele.Middle(4);
			Vector2 side = (Kantele.Outer(4) - Kantele.Inner(4)).Orthogonal().Normalized();
			feet.Place(mid + side * 200f);
			if (feet.Step(mid - side * 200f) != 1 || feet.Hit(0) != 4)
				warnings.Add("Thaw: a single long step over a string must still touch it (the feet's way is tested, not where they land).");
		}

		// --- the song's clock ---------------------------------------------------------------------------------
		if (ThawEvent.SongSeconds(Kantele.SongLength) + ThawEvent.BreathSeconds >= ThawEvent.IdleSeconds
			|| ThawEvent.ListenRadius >= ThawEvent.LeaveRadius || ThawEvent.WrongSeconds <= 0f || ThawEvent.NoteSeconds <= 0.3f)
			warnings.Add("Thaw: the song's clock must leave time to answer (the longest singing well inside the wait before it sings "
				+ "again), sing nearer than it falls silent, and keep its notes far enough apart to be told apart.");
	}

	// Any opaque texel within a pixel of p: the art's lines are Bresenham, so one may sit a pixel off the true line.
	private static bool OpaqueNear(Image image, Vector2 p)
	{
		int x0 = Mathf.FloorToInt(p.X), y0 = Mathf.FloorToInt(p.Y);
		for (int y = y0 - 1; y <= y0 + 1; y++)
		{
			for (int x = x0 - 1; x <= x0 + 1; x++)
			{
				if (x >= 0 && y >= 0 && x < image.GetWidth() && y < image.GetHeight() && image.GetPixel(x, y).A > 0.5f)
					return true;
			}
		}
		return false;
	}
}
