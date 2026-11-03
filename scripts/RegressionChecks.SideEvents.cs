using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// The side events' table now that a chapter may hold two (P2, .ai/chapter-wizards.md 6.3): what keeps
// SideEventDirector's list right, which no build can see and no single run would show. Every line is a
// warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	// Seeds sampled for each chapter's layout, on open ground and on a maze lattice each.
	private const int SideEventSampleSeeds = 16;

	// THE TABLE. In a chapter, no two events share a site (a site is one deed, one reward, one hint); across
	// chapters, a site is one chapter's (StageRewards, the deeds and the reveal rule all read a site's chapter
	// off it); no chapter holds more events than there are placer streams for them; and every event has a hint,
	// because the hints alternate a chapter's events and one without a hint is never heard of - except the two
	// early chapters', built before hints were asked of every event (StageHintCatalog.SitesWithoutHints).
	private static void ValidateSideEventTable(List<string> warnings)
	{
		var chapterOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (StageDefinition stage in StageCatalog.All)
		{
			IReadOnlyList<SideEventEntry> events = SideEventDirector.EventsFor(stage.Id);
			if (events.Count > SideEventDirector.MaxEventsPerChapter)
				warnings.Add($"Side events: {stage.DisplayName} holds {events.Count} events; the placer has streams for {SideEventDirector.MaxEventsPerChapter}.");
			var here = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < events.Count; i++)
			{
				SideEventEntry entry = events[i];
				string at = $"Side events: {stage.DisplayName}'s event {i}";
				if (string.IsNullOrWhiteSpace(entry.SiteId))
				{
					warnings.Add($"{at} has no site id, so it pays nothing and nobody can win it.");
					continue;
				}
				if (entry.Create == null)
					warnings.Add($"{at} ('{entry.SiteId}') has no way to make it.");
				if (!here.Add(entry.SiteId))
					warnings.Add($"{at}: two of its events stand on '{entry.SiteId}'; a site is one deed, one reward and one hint.");
				else if (chapterOf.TryGetValue(entry.SiteId, out string other))
					warnings.Add($"{at}: '{entry.SiteId}' is also {other}'s; a site is one chapter's.");
				else
					chapterOf[entry.SiteId] = stage.DisplayName;

				bool exempt = StageHintCatalog.SitesWithoutHints.Contains(entry.SiteId, StringComparer.OrdinalIgnoreCase);
				StageHint hint = StageHintCatalog.ForSite(entry.SiteId);
				if (hint == null && !exempt)
					warnings.Add($"{at} ('{entry.SiteId}') has no hint (StageHintCatalog); the hints alternate a chapter's events, and this one would never be heard of.");
				else if (hint != null && exempt)
					warnings.Add($"Side events: '{entry.SiteId}' has a hint now; take it off StageHintCatalog.SitesWithoutHints.");
			}
		}
		foreach (string site in StageHintCatalog.SitesWithoutHints)
		{
			if (!chapterOf.ContainsKey(site))
				warnings.Add($"Side events: StageHintCatalog.SitesWithoutHints names '{site}', which no chapter's event stands on.");
		}

		ValidateSecondEventStandIn(warnings);
		ValidateSideEventSeparation(warnings);
	}

	// THE STAND-IN (SecondEventStandIn): a tool run's second event, which must never reach play. The game's own
	// launch never gets it whatever its arguments; a tool run that asks does (or the probes would test nothing);
	// no real catalog names its site; and on any launch where it is in a chapter's list it is reported - a note
	// on the tool run that asked for it, a leak anywhere else.
	private static void ValidateSecondEventStandIn(List<string> warnings)
	{
		const string at = "Side events (stand-in second event)";
		string flag = SecondEventStandIn.Flag;
		if (SecondEventStandIn.Resolve(new[] { flag, "--stage=3" }, toolLaunch: false).Length > 0
			|| SecondEventStandIn.Resolve(new[] { flag + "=stage_3" }, toolLaunch: false).Length > 0)
			warnings.Add($"{at}: LEAK - a launch that is not a tool scene gets the stand-in when given {flag}.");
		string asked = StageCatalog.GetByIndex(3)?.Id ?? string.Empty;
		if (asked.Length > 0 && SecondEventStandIn.Resolve(new[] { flag + "=3" }, toolLaunch: true) != asked)
			warnings.Add($"{at}: a tool run given {flag}=3 does not get it in {asked}, so the probes would test nothing.");

		foreach (StageDefinition stage in StageCatalog.All)
		{
			if (SideEventDirector.TableEventsFor(stage.Id).Any(e => SecondEventStandIn.IsStandInSite(e.SiteId)))
				warnings.Add($"{at}: LEAK - SideEventDirector's table lists the stand-in's site '{SecondEventStandIn.SiteId}' for {stage.DisplayName}; it is a tool run's, added by {flag} alone.");
		}
		if (StageHintCatalog.All.Any(h => SecondEventStandIn.IsStandInSite(h.SiteId)))
			warnings.Add($"{at}: LEAK - StageHintCatalog lists a hint for the stand-in's site.");
		if (ChapterNotes.SiteClueLines.Keys.Any(SecondEventStandIn.IsStandInSite))
			warnings.Add($"{at}: LEAK - ChapterNotes lists a clue for the stand-in's site.");
		if (AchievementDefinitions.SideEventWordSites.Any(SecondEventStandIn.IsStandInSite))
			warnings.Add($"{at}: LEAK - AchievementDefinitions has deed words for the stand-in's site.");
		if (UnlockCatalog.All.Any(d => SecondEventStandIn.IsStandInSite(d.SiteId)))
			warnings.Add($"{at}: LEAK - UnlockCatalog pays something at the stand-in's site.");

		if (SecondEventStandIn.Active)
		{
			if (!SaveManager.IsToolSceneLaunch())
				warnings.Add($"{at}: LEAK - it is in {SecondEventStandIn.StageId}'s list on a launch that is not a tool scene.");
			else
				warnings.Add($"{at}: in {SecondEventStandIn.StageId}'s list on this tool run ({flag}), as asked; a real run never has it.");
		}
	}

	// THE PLACEMENT. A chapter's events keep their main sites at least SideEventDirector.MinSiteSeparationDegrees
	// round from each other, seen from the start, and each in the event band - on every sampled seed, on open
	// ground with lakes and on a maze lattice, laid out by SideEventDirector.LayOut, the run's own code. Checked
	// for every chapter that holds two, and for every chapter with a stand-in appended to its list, which is the
	// rule a chapter's second event will meet whatever its first is. And the wayfinder's bearing keeps its 75
	// degrees off every event's, and with one event is exactly the bearing it always was.
	private static void ValidateSideEventSeparation(List<string> warnings)
	{
		int before = PointsOfInterest.Live.Count;
		try
		{
			foreach (StageDefinition stage in StageCatalog.All)
			{
				IReadOnlyList<SideEventEntry> real = SideEventDirector.EventsFor(stage.Id);
				if (real.Count == 0)
					continue;
				if (real.Count > 1)
					CheckSideEventSeparation(stage, "its events", real, warnings);
				if (!real.Any(e => SecondEventStandIn.IsStandInSite(e.SiteId)) && real.Count < SideEventDirector.MaxEventsPerChapter)
					CheckSideEventSeparation(stage, "its events with a second after them", real.Append(SecondEventStandIn.Entry).ToList(), warnings);
			}
		}
		finally
		{
			if (PointsOfInterest.Live.Count > before)
				PointsOfInterest.Live.RemoveRange(before, PointsOfInterest.Live.Count - before);
		}

		float wayfinderNeed = SideEventDirector.WayfinderMinSeparationDegrees - 0.01f;
		var rng = new RandomNumberGenerator { Seed = 20261102UL };
		for (int k = 0; k < 400; k++)
		{
			float a = rng.Randf() * Mathf.Tau;
			float b = a + Mathf.DegToRad(SideEventDirector.MinSiteSeparationDegrees) + rng.Randf() * Mathf.DegToRad(360f - 2f * SideEventDirector.MinSiteSeparationDegrees);
			float roll = rng.Randf();
			float bearing = SideEventDirector.WayfinderBearing(roll, new[] { a, b });
			float gap = Mathf.Min(Mathf.RadToDeg(ExplorePlacer.Separation(bearing, a)), Mathf.RadToDeg(ExplorePlacer.Separation(bearing, b)));
			if (gap < wayfinderNeed)
			{
				warnings.Add($"Side events: the wayfinder's bearing is {gap:0.0} degrees from an event's with two events at {Mathf.RadToDeg(a):0} and {Mathf.RadToDeg(b):0} degrees; it keeps {SideEventDirector.WayfinderMinSeparationDegrees:0}.");
				break;
			}
			float sep = Mathf.DegToRad(SideEventDirector.WayfinderMinSeparationDegrees);
			if (SideEventDirector.WayfinderBearing(roll, new[] { a }) != a + sep + roll * (Mathf.Tau - 2f * sep))
			{
				warnings.Add("Side events: with one event the wayfinder's bearing is no longer the one it always had; every one-event chapter's wayfinder has moved.");
				break;
			}
		}
	}

	private static void CheckSideEventSeparation(StageDefinition stage, string what, IReadOnlyList<SideEventEntry> entries, List<string> warnings)
	{
		float need = SideEventDirector.MinSiteSeparationDegrees - 0.01f;
		const float chunk = 1536f;
		// The bands are where the roll lands; the open-ground search may move a site up to 1600 px (_ExploreProbe's slack).
		const float slack = 1700f;
		float min = SideEventDirector.EventMinChunks * chunk - slack;
		float max = SideEventDirector.EventMaxChunks * chunk + slack;
		int layouts = 0, apart = 0, outOfBand = 0;
		float closest = 180f;
		string firstFault = null;
		foreach (bool maze in new[] { false, true })
		{
			for (int s = 1; s <= SideEventSampleSeeds; s++)
			{
				ulong seed = (ulong)(s * 7919 + stage.Index * 104729 + (maze ? 15485863 : 0));
				ExplorePlacer.Ground ground = SampleExploreGround(seed, maze);
				List<(SideEvent Event, ExplorePlacer Placer)> laid = SideEventDirector.LayOut(entries,
					stream => new ExplorePlacer(seed, stage.Index, stream, ground)
					{
						EventMinChunks = SideEventDirector.EventMinChunks,
						EventMaxChunks = SideEventDirector.EventMaxChunks,
					});
				layouts++;
				try
				{
					var mains = laid.Select(l => l.Event.Sites.Count > 0 ? l.Event.Sites[0].Position : ground.Origin).ToList();
					for (int i = 0; i < mains.Count; i++)
					{
						float d = mains[i].DistanceTo(ground.Origin);
						if (d < min || d > max)
						{
							outOfBand++;
							firstFault ??= $"seed {seed}{(maze ? " (maze)" : "")}: event {i}'s site {d / chunk:0.0} chunks out";
						}
						for (int j = i + 1; j < mains.Count; j++)
						{
							float sep = Mathf.RadToDeg(ExplorePlacer.Separation(
								ExplorePlacer.BearingFrom(ground.Origin, mains[i]), ExplorePlacer.BearingFrom(ground.Origin, mains[j])));
							closest = Mathf.Min(closest, sep);
							if (sep < need)
							{
								apart++;
								firstFault ??= $"seed {seed}{(maze ? " (maze)" : "")}: events {i} and {j} {sep:0.0} degrees apart";
							}
						}
					}
				}
				finally
				{
					foreach (var (sideEvent, _) in laid)
						sideEvent.Free();
				}
			}
		}
		if (apart > 0)
			warnings.Add($"Side events: {stage.DisplayName}, {what}: {apart} pair(s) in {layouts} sampled layouts closer than {SideEventDirector.MinSiteSeparationDegrees:0} degrees (closest {closest:0.0}; {firstFault}).");
		if (outOfBand > 0)
			warnings.Add($"Side events: {stage.DisplayName}, {what}: {outOfBand} main site(s) in {layouts} sampled layouts outside the {SideEventDirector.EventMinChunks:0}-{SideEventDirector.EventMaxChunks:0} chunk band ({firstFault}).");
	}

	// Ground for sampling a layout without a run: lakes - zero to two in every 2048 px square, 120 to 680 px
	// across - so the search and the sweep both have work, and on a maze the placer's snap to cell centres
	// (384 px). A pure function of the seed, as the real ground is of the terrain seed.
	private static ExplorePlacer.Ground SampleExploreGround(ulong seed, bool maze)
	{
		const float cell = 2048f;
		static ulong Mix(ulong h)
		{
			h ^= h >> 33; h *= 0xff51afd7ed558ccdUL; h ^= h >> 33; h *= 0xc4ceb9fe1a85ec53UL; h ^= h >> 33;
			return h;
		}
		static float Unit(ulong h) => (h >> 40) / (float)(1UL << 24);
		bool Wet(Vector2 p)
		{
			int cx = Mathf.FloorToInt(p.X / cell), cy = Mathf.FloorToInt(p.Y / cell);
			for (int dx = -1; dx <= 1; dx++)
			for (int dy = -1; dy <= 1; dy++)
			{
				ulong h = Mix(seed ^ Mix((ulong)(uint)(cx + dx) * 0x9E3779B97F4A7C15UL ^ (ulong)(uint)(cy + dy)));
				int lakes = (int)(h % 3UL);
				for (int k = 0; k < lakes; k++)
				{
					h = Mix(h + (ulong)k + 1UL);
					var centre = new Vector2((cx + dx + Unit(h)) * cell, (cy + dy + Unit(Mix(h ^ 0xA5A5UL))) * cell);
					float radius = 120f + 560f * Unit(Mix(h ^ 0x5A5AUL));
					if (centre.DistanceSquaredTo(p) < radius * radius)
						return true;
				}
			}
			return false;
		}
		bool Clear(Vector2 c, float r)
		{
			if (Wet(c))
				return false;
			for (int k = 0; k < 8; k++)
			{
				var dir = new Vector2(Mathf.Cos(k * Mathf.Tau / 8f), Mathf.Sin(k * Mathf.Tau / 8f));
				if (Wet(c + dir * r) || Wet(c + dir * r * 0.5f))
					return false;
			}
			return true;
		}
		bool Dense(Vector2 c, float r)
		{
			if (!Clear(c, r))
				return false;
			int rings = Mathf.CeilToInt(r / 80f);
			for (int i = 1; i <= rings; i++)
			{
				float ring = r * i / rings;
				int around = Mathf.Max(8, Mathf.CeilToInt(Mathf.Tau * ring / 80f));
				for (int k = 0; k < around; k++)
				{
					float angle = k * Mathf.Tau / around;
					if (Wet(c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring))
						return false;
				}
			}
			return true;
		}
		return new ExplorePlacer.Ground
		{
			IsClear = Clear,
			IsClearDense = Dense,
			Origin = new Vector2(96f, -48f),
			ChunkPixels = 1536f,
			MazeCellPixels = maze ? 384f : 0f,
		};
	}
}
