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
						// An event its chapter places (the barge, on the Last Bridge's band) is held to its own rules
						// (ValidateBarge), not to the explore band.
						// The Court's rooms are its building's, not the band's (SideEvent.PlacedByChapterRules).
						float eventMin = laid[i].Event.PlacementMinChunks * chunk - slack;
						float eventMax = laid[i].Event.PlacementMaxChunks * chunk + slack;
						if (!laid[i].Event.PlacedByChapterRules && (d < eventMin || d > eventMax))
						{
							outOfBand++;
							firstFault ??= $"seed {seed}{(maze ? " (maze)" : "")}: event {i}'s site {d / chunk:0.0} chunks out";
						}
						for (int j = i + 1; j < mains.Count; j++)
						{
							// Two events a chapter's building places (the Court's seer and its dotted door) are where the
							// house has them, not rolled round the start: the 90 degrees is the band's rule.
							if (laid[i].Event.PlacedByChapterRules && laid[j].Event.PlacedByChapterRules)
								continue;
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

	// THE BARGE (BargeEvent, .ai/chapter-wizards.md 4.9; its lamps since 2026-11-07, .ai/unlock-puzzles.md 3.7), what no
	// build sees. Its numbers: her drift clear of the fall and slower than his walk, a lamp outlasting one stretch and
	// not three. On every seed: moored under one of its spans on the water beside the deck, its row of lamps on clear
	// deck at the parapet on her side (never in a gatehouse, at a jog or beside a hole), her whole drift inside the span
	// and on the water all the way, and room before the castle wall for her landing however far the fall bears her.
	// And ON PAPER, with the event's own rule (BargeDrift) and a paper player walking the deck at his walk, from where
	// he arrives: lighting the next two ahead of her - never one that would burn out before she reached it, which on a
	// long stretch (past a hole, through a gatehouse) means waiting to light it - lands her without once leaving her in
	// the dark; lighting the whole row in one run ahead and waiting at
	// her landing does not - a lamp goes out before she reaches it and she waits in the dark; the same rhythm against
	// the fall from the tightest arrival lands her and never lets the fall within RestLead of her; and lighting only
	// her first lamp and then nothing lets the fall bear her on past dark lamps without losing what she set out from -
	// her landing moves on one lamp for each - until the rhythm, taken up again, lands her there.
	private static void ValidateBarge(List<string> warnings)
	{
		const string at = "Barge (the Last Bridge)";
		float walk = 220f;
		float stretch = BargeEvent.LampSpacing / BargeEvent.DriftSpeed;
		if (BargeEvent.DriftSpeed <= BridgeRules.FrontSpeed * 1.1f)
			warnings.Add($"{at}: it drifts at {BargeEvent.DriftSpeed:0} px a second, not clear of the fall's {BridgeRules.FrontSpeed:0.0}; under way, the fall would catch her.");
		if (BargeEvent.DriftSpeed >= walk * 0.5f)
			warnings.Add($"{at}: it drifts at {BargeEvent.DriftSpeed:0} px a second, more than half his walk; he could never get ahead of her to light a lamp.");
		if (BargeEvent.BurnSeconds <= stretch + BargeEvent.LightSeconds)
			warnings.Add($"{at}: a lamp burns {BargeEvent.BurnSeconds:0} s, no longer than one stretch ({stretch:0} s); a lamp lit as she sets out goes out before she reaches it.");
		if (BargeEvent.BurnSeconds >= 3f * stretch)
			warnings.Add($"{at}: a lamp burns {BargeEvent.BurnSeconds:0} s, three stretches or more ({3f * stretch:0} s); the whole row lit at once would carry her in, and there is no rhythm to find.");
		if (BargeEvent.GutterSeconds <= 0f || BargeEvent.GutterSeconds >= BargeEvent.BurnSeconds * 0.5f)
			warnings.Add($"{at}: a lamp gutters for {BargeEvent.GutterSeconds:0.0} s of its {BargeEvent.BurnSeconds:0} s; the warning should be a short last part of the burn.");
		if (BargeEvent.LightRadius < 40f || BargeEvent.LampInset + BargeEvent.LightRadius > BridgeBand.Find(BridgeBand.PieceKind.Gatehouse, 0).HalfWidthPx)
			warnings.Add($"{at}: a lamp's reach ({BargeEvent.LightRadius:0} px) is too small to stand in, or reaches past a narrow deck's middle.");
		// Waiting ahead of the stopped fall - which stops at the gate yard's mouth - a whole drift must still fit before
		// the castle wall.
		if (BridgeBand.GateYardMouth + BargeEvent.RestLead + BargeEvent.DriftLength > BridgeBand.End - BargeEvent.LastLampFromEnd)
			warnings.Add($"{at}: a barge waiting ahead of the stopped fall has no room for a whole drift before the castle wall.");
		foreach (int index in BargeEvent.MooringSpans)
		{
			BridgeBand.Piece span = BridgeBand.Find(BridgeBand.PieceKind.Span, index);
			if (span.S1 - span.S0 < BargeEvent.MooringFromSpanStart + BargeEvent.DriftLength + BargeEvent.LandingBeforeSpanEnd)
				warnings.Add($"{at}: span {index + 1} is too short to moor it with its whole drift inside it.");
		}

		int before = PointsOfInterest.Live.Count;
		var ground = new ExplorePlacer.Ground { IsClear = (_, _) => true, Origin = Vector2.Zero, ChunkPixels = 1536f };
		int sides = 0, layouts = 0, slid = 0;
		var spans = new HashSet<int>();
		string fault = null, paper = null;
		float slowest = 0f, worstLead = float.MaxValue;
		int fewestAtWall = int.MaxValue;
		try
		{
			for (int k = 0; k < 64; k++)
			{
				ulong seed = (ulong)(k * 6151 + 11);
				var placer = new ExplorePlacer(seed, 11, SideEventDirector.EventStream(0), ground);
				var barge = new BargeEvent();
				barge.PlaceSites(placer);
				layouts++;
				var row = new float[barge.LampCount];
				for (int i = 0; i < row.Length; i++)
					row[i] = barge.LampAlong(i);
				int side = barge.Side;
				int piece = BridgeBand.PieceIndexAt(barge.MooringS);
				BridgeBand.Piece p = piece >= 0 ? BridgeBand.Pieces[piece] : default;
				if (row.Length <= BargeEvent.LampsPerDrift)
					fault ??= $"seed {seed}: a row of only {row.Length} lamps";
				else if (piece < 0 || p.Kind != BridgeBand.PieceKind.Span || Array.IndexOf(BargeEvent.MooringSpans, p.Index) < 0)
					fault ??= $"seed {seed}: moored {barge.MooringS:0} px along, not under one of its spans";
				else
				{
					spans.Add(p.Index);
					if (row[BargeEvent.LampsPerDrift] > p.S1 - BargeEvent.LandingBeforeSpanEnd + 0.5f)
						fault ??= $"seed {seed}: her landing at {row[BargeEvent.LampsPerDrift]:0} px runs past the end of span {p.Index + 1}";
					if (row[0] != barge.MooringS)
						fault ??= $"seed {seed}: moored at {barge.MooringS:0} px, not beside her first lamp ({row[0]:0})";
				}
				if (side > 0)
					sides++;
				// The row: each lamp where a lamp may stand, its foot and the deck he lights it from on the deck, one
				// spacing apart at least and never a long dark gap in it.
				for (int i = 0; i < row.Length && fault == null; i++)
				{
					float s = row[i];
					float foot = BargeEvent.LampAcross(s, side);
					if (!BargeEvent.LampMayStand(s, side))
						fault = $"seed {seed}: lamp {i} at {s:0} px stands where a lamp may not";
					else if (!BridgeBand.IsOnDeck(BridgeBand.LocalAt(s, foot), 20f) || !BridgeBand.IsOnDeck(BridgeBand.LocalAt(s, foot - side * BargeEvent.LightRadius), 20f))
						fault = $"seed {seed}: lamp {i} at {s:0} px is not on the deck, or he cannot stand by it";
					else if (i > 0 && (s - row[i - 1] < BargeEvent.LampSpacing - 0.5f || s - row[i - 1] > BargeEvent.LampSpacing + 1100f))
						fault = $"seed {seed}: lamps {i - 1} and {i} are {s - row[i - 1]:0} px apart";
					if (i > 0 && i <= BargeEvent.LampsPerDrift && s - row[i - 1] > BargeEvent.LampSpacing + 1f)
						slid++;
				}
				// In the water beside the deck, never on it, for the whole of the drift.
				for (float s = barge.MooringS; fault == null && row.Length > BargeEvent.LampsPerDrift && s <= row[BargeEvent.LampsPerDrift]; s += 100f)
				{
					float t = BargeEvent.TrackAcross(s, side);
					if (Mathf.Abs(t) < BridgeBand.HalfWidthAt(s) + 40f)
						fault = $"seed {seed}: at {s:0} px along it rides {Mathf.Abs(t):0} px out, on the deck (the shore is at {BridgeBand.HalfWidthAt(s):0})";
				}
				// However far the fall bears her - it stops at the gate yard's mouth - lamps are left to light before the wall.
				float furthest = BridgeBand.GateYardMouth + BargeEvent.RestLead;
				int beyond = 0;
				foreach (float s in row)
					if (s > furthest)
						beyond++;
				fewestAtWall = Math.Min(fewestAtWall, beyond);
				if (placer.EventBearing is float bearing && float.IsNaN(bearing))
					fault ??= $"seed {seed}: its site was placed without a bearing, so the wayfinder could not keep off it";
				barge.Free();

				if (fault != null || paper != null || row.Length <= BargeEvent.LampsPerDrift)
					continue;
				// ON PAPER, from where he arrives (a little behind her).
				PaperBarge two = PlayBargeOnPaper(row, PaperPolicy.TwoAhead, float.NaN, 150f);
				if (!two.Landed || two.Drift.DarkStops > 0)
					paper = $"seed {seed}: lighting the next two ahead of her (never one that would burn out before she reached it) "
						+ (two.Landed ? $"left her waiting in the dark {two.Drift.DarkStops} times" : $"did not land her in 150 s (she set out from {two.Drift.SetOutFrom})");
				else
					slowest = Mathf.Max(slowest, two.Seconds);
				PaperBarge all = PlayBargeOnPaper(row, PaperPolicy.LightAll, float.NaN, 150f);
				if (paper == null && (all.Landed || all.Drift.DarkStops == 0 || all.Drift.TimesWentOut == 0))
					paper = $"seed {seed}: lighting the whole row in one run ahead and waiting at her landing "
						+ (all.Landed ? $"landed her in {all.Seconds:0} s" : "never left her waiting in the dark") + " - there is no rhythm to find";
				// Against the fall, from the tightest arrival: the fall RestLead behind her as he comes.
				float tight = row[0] - BargeEvent.RestLead;
				PaperBarge fall = PlayBargeOnPaper(row, PaperPolicy.TwoAhead, tight, 200f);
				if (paper == null && !fall.Landed)
					paper = $"seed {seed}: from the tightest arrival, lighting the next two ahead did not land her in 200 s";
				if (paper == null && fall.MinLead < BargeEvent.RestLead - 1f)
					paper = $"seed {seed}: the fall came within {fall.MinLead:0} px of her (she keeps {BargeEvent.RestLead:0})";
				worstLead = Mathf.Min(worstLead, fall.MinLead);
				// Idle after her first lamp, until the fall has borne her past two dark ones; then the rhythm.
				PaperBarge idle = PlayBargeOnPaper(row, PaperPolicy.IdleThenTwoAhead, tight, 300f);
				if (paper == null && (!idle.Landed || idle.BorneBeforeResuming < 2 || idle.SetOutWhenBorne != 1
						|| idle.LandedAt != BargeEvent.LampsPerDrift + idle.Drift.BornePast || idle.MinLead < BargeEvent.RestLead - 1f))
					paper = $"seed {seed}: left idle after her first lamp, the fall bore her past {idle.BorneBeforeResuming} dark lamps with "
						+ $"{idle.SetOutWhenBorne} set out from, and taken up again she {(idle.Landed ? $"landed at lamp {idle.LandedAt}" : "never landed")} "
						+ $"(borne past {idle.Drift.BornePast} in all; the fall's nearest {idle.MinLead:0} px)";
			}
		}
		finally
		{
			if (PointsOfInterest.Live.Count > before)
				PointsOfInterest.Live.RemoveRange(before, PointsOfInterest.Live.Count - before);
		}
		if (fault != null)
			warnings.Add($"{at}: {fault}.");
		if (paper != null)
			warnings.Add($"{at}: on paper, {paper}.");
		if (spans.Count < BargeEvent.MooringSpans.Length)
			warnings.Add($"{at}: {layouts} seeds moored it under only {spans.Count} of its {BargeEvent.MooringSpans.Length} spans.");
		if (sides == 0 || sides == layouts)
			warnings.Add($"{at}: {layouts} seeds moored it on one side of the bridge every time.");
		if (fewestAtWall < 3)
			warnings.Add($"{at}: borne by the fall to the gate yard, a row has only {fewestAtWall} lamps left before the castle wall.");
		// The measurements, on a clean pass only (a fault stops the paper play, and the warning above says what it found).
		if (fault == null && paper == null)
			GD.Print($"RegressionChecks: the barge on paper over {layouts} seeds - the next two ahead lands her in {slowest:0} s at most, never in the dark; "
				+ $"lighting the whole row at once never does; {slid} lamps of the drift stepped along past a hole; "
				+ $"the fall's nearest from the tightest arrival {worstLead:0} px; {fewestAtWall} lamps at least past the furthest the fall bears her.");

		// The hint's mist comes in on the knocks of the hull (StageHintCue.HullKnocks = sfx._HULL_KNOCKS): every beat
		// inside the sound, in order.
		float[] knocks = StageHintCue.HullKnockBeats;
		string path = SfxCatalog.PathFor(SfxCatalog.HintHullKnock);
		double length = ResourceLoader.Exists(path) && ResourceLoader.Load<AudioStream>(path) is AudioStream sound ? sound.GetLength() : 0.0;
		for (int i = 0; i < knocks.Length; i++)
		{
			if ((i > 0 && knocks[i] <= knocks[i - 1]) || (length > 0.0 && knocks[i] >= length))
			{
				warnings.Add($"{at}: the hint's knock {i} at {knocks[i]:0.00} s is out of order or past the end of {SfxCatalog.HintHullKnock} ({length:0.00} s).");
				break;
			}
		}
	}

	// How the paper player plays the barge (ValidateBarge): the next two ahead of her; the whole row in one run ahead,
	// then wait at her landing; or her first lamp only, idle until the fall has borne her past two dark lamps, then the
	// next two ahead.
	private enum PaperPolicy { TwoAhead, LightAll, IdleThenTwoAhead }

	private struct PaperBarge
	{
		public bool Landed;
		public float Seconds;
		public int LandedAt;
		public float MinLead;
		public int BorneBeforeResuming;
		public int SetOutWhenBorne;
		public BargeDrift Drift;
	}

	// A paper player on the Last Bridge: only his place along it, walking at his walk to the lamp his policy names and
	// lighting it by standing there, against the event's own rule (BargeDrift), stepped twenty times a second, from a
	// little behind her. frontStart is where the fall is as he arrives, px along, or NaN for a fall not yet moving.
	private static PaperBarge PlayBargeOnPaper(float[] row, PaperPolicy policy, float frontStart, float limitSeconds)
	{
		const float dt = 0.05f;
		const float walk = 220f;
		var drift = new BargeDrift(row);
		var result = new PaperBarge { Drift = drift, MinLead = float.MaxValue, LandedAt = -1, BorneBeforeResuming = -1, SetOutWhenBorne = -1 };
		bool falling = !float.IsNaN(frontStart);
		float front = frontStart;
		float me = row[0] - 200f;
		bool idle = policy == PaperPolicy.IdleThenTwoAhead;
		for (float t = 0f; t < limitSeconds; t += dt)
		{
			if (falling)
				front += BridgeRules.FrontSpeed * dt;
			float push = falling ? front + BargeEvent.RestLead : float.NegativeInfinity;
			float goal;
			if (drift.Landed)
				goal = drift.BargeS;
			else if (policy == PaperPolicy.LightAll)
			{
				// The next dark lamp of her stretch ahead of him; none left, and he waits at her landing.
				goal = PaperLamp(drift, drift.Stop, drift.Landing - 1, me - 1f) is int next ? row[next] : row[drift.Landing];
			}
			else if (idle && drift.SetOutFrom == 0)
				goal = row[drift.Stop];
			else if (idle)
			{
				goal = me;
				if (drift.BornePast >= 2)
				{
					idle = false;
					result.BorneBeforeResuming = drift.BornePast;
					result.SetOutWhenBorne = drift.SetOutFrom;
				}
			}
			else if (PaperLamp(drift, drift.Stop, Math.Min(drift.Stop + 1, drift.Landing - 1), float.NegativeInfinity) is int next)
			{
				// Never one she will not reach before it burns out (a long stretch past a hole or a gatehouse): he waits a
				// stride short of it until she is near enough.
				bool tooEarly = (row[next] - drift.BargeS) / BargeEvent.DriftSpeed > BargeEvent.BurnSeconds - 3f;
				goal = tooEarly ? row[next] - 110f : row[next];
			}
			else
				goal = drift.BargeS;
			me = Mathf.MoveToward(me, goal, walk * dt);
			int near = BargeDrift.IndexNear(row, me);
			drift.Step(dt, Mathf.Abs(row[near] - me) <= BargeEvent.LightRadius * 0.5f ? near : -1, push);
			if (falling)
				result.MinLead = Mathf.Min(result.MinLead, drift.BargeS - front);
			if (drift.Landed && Mathf.Abs(me - drift.BargeS) <= BargeEvent.Reach)
			{
				result.Landed = true;
				result.Seconds = t;
				result.LandedAt = drift.Stop;
				break;
			}
		}
		return result;
	}

	// The first dark lamp from..to (inclusive) at or past along, or null.
	private static int? PaperLamp(BargeDrift drift, int from, int to, float along)
	{
		for (int i = Math.Max(0, from); i <= to && i < drift.LampS.Length; i++)
			if (drift.BurnLeft[i] <= 0f && drift.LampS[i] >= along)
				return i;
		return null;
	}
}
