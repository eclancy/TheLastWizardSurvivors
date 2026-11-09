using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// ENDLESS (.ai/endless-mode.md; EndlessCatalog, Node2DGame.Endless.cs). What a build cannot see about the mode: which
// chapters may be held through the night and on which saves, that the strength climbs and walls as the design says,
// that a night never pays a campaign deed the first night could not, and that the purse is capped.
public static partial class RegressionChecks
{
	private static void ValidateEndless(List<string> warnings)
	{
		ValidateEndlessChapters(warnings);
		ValidateEndlessStrength(warnings);
		ValidateEndlessUnlock(warnings);
		ValidateEndlessClamp(warnings);
		ValidateEndlessLedger(warnings);
		ValidateEndlessPurse(warnings);
		ValidateEndlessFeats(warnings);
	}

	// The clamp (section 1.6): past the chapter's victory time, the pace minutes every growing reader reads hold there -
	// spawn health, the Warden, the XP multiplier - and before it nothing moves. Measured on a real Node2DGame, never
	// added to the tree, its clock set by hand.
	private static void ValidateEndlessClamp(List<string> warnings)
	{
		const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
		System.Reflection.FieldInfo clock = typeof(Node2DGame).GetField("timeElapsed", Private);
		System.Reflection.PropertyInfo minutes = typeof(Node2DGame).GetProperty("PaceMinutes", Private);
		if (clock == null || minutes == null)
		{
			warnings.Add("Endless: Node2DGame has no timeElapsed or PaceMinutes to check the clamp against.");
			return;
		}
		var probe = new Node2DGame();
		try
		{
			float victory = probe.EndlessVictorySeconds;
			clock.SetValue(probe, victory * 0.5f);
			float before = (float)minutes.GetValue(probe);
			if (!Mathf.IsEqualApprox(before, victory * 0.5f / probe.RunPace / 60f))
				warnings.Add($"Endless: halfway through the first night the pace reads {before:0.00} minutes; the clamp must not move it.");
			clock.SetValue(probe, victory * 3f);
			float after = (float)minutes.GetValue(probe);
			float held = victory / probe.RunPace / 60f;
			if (!Mathf.IsEqualApprox(after, held))
				warnings.Add($"Endless: in the third night the pace reads {after:0.00} minutes; spawn health, the Warden and XP must hold at {held:0.00}.");
			float xp = probe.XpPaceMultiplier;
			float xpHeld = 1f + Mathf.Max(0f, probe.XpGrowthPerPaceMinute) * held;
			if (!Mathf.IsEqualApprox(xp, xpHeld))
				warnings.Add($"Endless: the XP multiplier reads x{xp:0.00} in the third night; it must hold at its victory-time x{xpHeld:0.00}.");
		}
		finally
		{
			probe.Free();
		}
	}

	// THE LEDGER (section 3.1): a run that kept the vigil and died in the third night feeds the save exactly what a portal
	// exit at the first fall would have - every lifetime total, deed, chapter clear and telemetry row - and a vault found
	// in the night is still recorded. And the night itself would have earned more had it been read, so the check bites.
	private static void ValidateEndlessLedger(List<string> warnings)
	{
		StageDefinition forest = StageCatalog.GetById(EndlessCatalog.RoadChapterIds[0]);
		BossDefinition warden = forest != null ? BossCatalog.ForStageIndex(forest.Index) : null;
		if (warden == null)
			return;
		SaveData baseline = EndlessSampleSave(takenBack: true);
		RunResult firstNight = new()
		{
			Outcome = RunOutcome.Victory, StageId = forest.Id, BossId = warden.Id, CharacterId = "aedan",
			BalancePresetId = GlobalStatsManager.BalancePresetDefault,
			TimeSurvived = 905f, FinalPlayerLevel = 31, EnemiesKilled = 1400, UndamagedSeconds = 60f,
		};
		RunResult wholeNight = new()
		{
			Outcome = RunOutcome.Defeat, StageId = forest.Id, BossId = string.Empty, CharacterId = "aedan",
			BalancePresetId = GlobalStatsManager.BalancePresetDefault,
			TimeSurvived = 2480f, FinalPlayerLevel = 54, EnemiesKilled = 6100, UndamagedSeconds = 600f,
		};

		SaveData Fed(RunResult run)
		{
			SaveData copy = SaveData.FromVariant(baseline.ToGodotDictionary());
			copy.RecordRunTelemetry(run);
			AchievementManager.ApplyRunAchievements(copy, run);
			return copy;
		}
		string Json(SaveData save) => Godot.Json.Stringify(save.ToGodotDictionary());

		string kept = Json(Fed(EndlessCatalog.LedgerFor(wholeNight, firstNight)));
		string portal = Json(Fed(firstNight));
		if (kept != portal)
			warnings.Add("Endless: a run that kept the vigil and fell in the third night writes a different save than leaving by the portal at the first fall would have (the ledger, section 3.1).");
		if (Json(Fed(wholeNight)) == portal)
			warnings.Add("Endless: the ledger check is vacuous - the whole night would have written the same save as its first night.");
		if (!ReferenceEquals(EndlessCatalog.LedgerFor(wholeNight, null), wholeNight))
			warnings.Add("Endless: a run that kept no vigil must hand the save its own result.");

		// Places count: a vault found in the night (completed on the spot, as SecretDirector.Found does) stays found
		// and its deed stays done through the run's end.
		SecretDefinition vault = SecretCatalog.Defined.FirstOrDefault(s => s.IsBuilt);
		if (vault != null)
		{
			SaveData found = SaveData.FromVariant(baseline.ToGodotDictionary());
			string deed = AchievementDefinitions.SecretDeedId(vault.Id);
			AchievementManager.Complete(found, deed);
			AchievementManager.ApplyRunAchievements(found, EndlessCatalog.LedgerFor(wholeNight, firstNight));
			if (!found.HasFoundSecret(vault.Id) || !GlobalStatsManager.IsAchievementUnlocked(found, deed))
				warnings.Add($"Endless: the secret '{vault.Id}', found in a night, is not recorded once the run ends.");
		}
	}

	// The purse (section 3.2): capped, and a night always pays less a minute than replaying a chapter does - worked from
	// CalculateArcaneReward's terms with no multipliers on either side, for every endless chapter's own warden.
	private static void ValidateEndlessPurse(List<string> warnings)
	{
		if (EndlessCatalog.NightPurse(EndlessCatalog.NightPurseEarned(100000, 8, 3f, 100000)) != EndlessCatalog.ArcaneCap)
			warnings.Add($"Endless: a very long night pays past the cap of {EndlessCatalog.ArcaneCap}.");
		if (EndlessCatalog.NightPurse(EndlessCatalog.NightPurseEarned(-5, 8, 1f, 0)) != 0)
			warnings.Add("Endless: a night of no minutes pays something.");
		foreach (string id in EndlessCatalog.RoadChapterIds)
		{
			StageDefinition stage = StageCatalog.GetById(id);
			BossDefinition boss = stage != null ? BossCatalog.ForStageIndex(stage.Index) : null;
			if (boss == null)
				continue;
			// An ordinary clear: base 20, fifteen minutes at 8, level 30 at 2, the boss's bonus, over 15 minutes and a
			// four-minute fight. A later night: fifteen minutes at 8 and half the bonus, over 15 minutes and a two-minute fight.
			float clear = (20f + 15f * 8f + 30f * 2f + boss.ArcaneVictoryBonus) / 19f;
			float night = EndlessCatalog.NightPurseEarned(15, 8, 1f, (int)MathF.Round(boss.ArcaneVictoryBonus * EndlessCatalog.WardenBonusShare)) / 17f;
			if (night >= clear)
				warnings.Add($"Endless: a night in the {stage.DisplayName} pays {night:0.0} a minute, no less than a clear's {clear:0.0} - it could be farmed.");
		}
	}

	// The three Feats: listed only once endless is open, never an unlock, counted on Default or harder and never in the
	// Sketchbook, and each completed by the record it names.
	private static void ValidateEndlessFeats(List<string> warnings)
	{
		SaveData closed = EndlessSampleSave(takenBack: false);
		SaveData open = EndlessSampleSave(takenBack: true);
		foreach (string id in EndlessCatalog.FeatIds)
		{
			AchievementDefinition feat = AchievementDefinitions.GetById(id);
			if (feat == null)
			{
				warnings.Add($"Endless: the Feat '{id}' is not in AchievementDefinitions.");
				continue;
			}
			if (feat.Visibility != AchievementVisibility.Endless || AchievementDefinitions.IsListed(feat, closed) || !AchievementDefinitions.IsListed(feat, open))
				warnings.Add($"Endless: '{id}' is not hidden until the Throne is taken back and listed after.");
			if (!string.IsNullOrWhiteSpace(feat.SpellUnlockId) || !string.IsNullOrWhiteSpace(feat.BoonUnlockId)
				|| !string.IsNullOrWhiteSpace(feat.CharacterUnlockId) || feat.CurrencyReward <= 0)
				warnings.Add($"Endless: '{id}' must pay Arcane Energy and never an unlock (section 3.3).");
			if (feat.CompletesInRun)
				warnings.Add($"Endless: '{id}' is measured from the records, not completed in the run.");
		}

		EndlessRecord Rec(string stage, int wardens, string preset = GlobalStatsManager.BalancePresetDefault) =>
			new() { StageId = stage, CharacterId = "aedan", PresetId = preset, WardensFelled = wardens, NightsReached = wardens, ClockSeconds = wardens * 900f };
		bool Done(SaveData save, string id) => AchievementDefinitions.GetById(id)?.IsComplete(AchievementContext.ForSave(save)) == true;

		SaveData two = EndlessSampleSave(takenBack: true);
		EndlessCatalog.Record(two, Rec(EndlessCatalog.RoadChapterIds[0], 2));
		if (!Done(two, EndlessCatalog.IntoTheNightId) || Done(two, EndlessCatalog.LongWatchId))
			warnings.Add("Endless: a second warden felled completes Into the Night and not The Long Watch.");
		SaveData offRoad = EndlessSampleSave(takenBack: true);
		EndlessCatalog.Record(offRoad, Rec(EndlessCatalog.SketchbookId, 5));
		EndlessCatalog.Record(offRoad, Rec(EndlessCatalog.RoadChapterIds[1], 5, GlobalStatsManager.BalancePresetCasual));
		if (Done(offRoad, EndlessCatalog.IntoTheNightId))
			warnings.Add("Endless: a night in the Sketchbook or on Casual counts toward a Feat.");
		SaveData every = EndlessSampleSave(takenBack: true);
		foreach (string id in EndlessCatalog.RoadChapterIds.Take(EndlessCatalog.RoadChapterIds.Count - 1))
			EndlessCatalog.Record(every, Rec(id, 2, GlobalStatsManager.BalancePresetHardcore));
		if (Done(every, EndlessCatalog.EveryVigilKeptId))
			warnings.Add("Endless: Every Vigil Kept completes one chapter short.");
		EndlessCatalog.Record(every, Rec(EndlessCatalog.RoadChapterIds[^1], 4));
		if (!Done(every, EndlessCatalog.EveryVigilKeptId) || !Done(every, EndlessCatalog.LongWatchId))
			warnings.Add("Endless: every road chapter held a second time does not complete Every Vigil Kept (or four wardens The Long Watch).");

		// The record keeps the better of two runs: more wardens, or as many in a longer clock.
		SaveData kept = new();
		EndlessCatalog.Record(kept, Rec("stage_0", 3));
		if (EndlessCatalog.Record(kept, Rec("stage_0", 2)) || kept.EndlessRecords.Count != 1 || kept.EndlessRecords[0].WardensFelled != 3)
			warnings.Add("Endless: a worse night replaced a record.");
		// And the save keeps it through a round trip.
		SaveData back = SaveData.FromVariant(kept.ToGodotDictionary());
		if (back.EndlessRecords.Count != 1 || back.EndlessRecords[0].WardensFelled != 3 || back.HasKeptVigil != kept.HasKeptVigil)
			warnings.Add("Endless: the records do not survive the save's round trip.");
	}

	// Every endless chapter is playable, on the road or the Sketchbook, has a warden with a scene of its own, and is
	// never the Bridge, the Court, the Throne or a chapter of the far shore (section 1.3, question 5).
	private static void ValidateEndlessChapters(List<string> warnings)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string id in EndlessCatalog.RoadChapterIds.Append(EndlessCatalog.SketchbookId))
		{
			if (!seen.Add(id))
				warnings.Add($"Endless: '{id}' is listed twice.");
			StageDefinition stage = StageCatalog.GetById(id);
			if (stage == null)
			{
				warnings.Add($"Endless: '{id}' names no chapter in StageCatalog.");
				continue;
			}
			if (!stage.IsPlayable)
				warnings.Add($"Endless: {stage.DisplayName} is listed but not playable.");
			if (!EndlessCatalog.IsRecordsOnly(stage) && !StageCatalog.IsInCampaign(stage))
				warnings.Add($"Endless: {stage.DisplayName} is listed as a road chapter but is not on the road.");
			if (StageCatalog.IsOnFarShore(stage))
				warnings.Add($"Endless: {stage.DisplayName} is on the far shore, which is never endless (question 5).");
			if (EndlessCatalog.ExcludedRoadChapterIds.Contains(id, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"Endless: {stage.DisplayName} is both endless and excluded.");
			BossDefinition warden = BossCatalog.ForStageIndex(stage.Index);
			if (warden == null)
				warnings.Add($"Endless: {stage.DisplayName} has no warden to send again.");
			else if (warden.IsOnStandIn || !ResourceLoader.Exists(warden.ScenePath))
				warnings.Add($"Endless: {stage.DisplayName}'s warden {warden.DisplayName} has no scene of its own at '{warden.ScenePath}'.");
		}
		foreach (string id in EndlessCatalog.ExcludedRoadChapterIds)
		{
			if (EndlessCatalog.IsEndlessChapter(StageCatalog.GetById(id)))
				warnings.Add($"Endless: {StageCatalog.DisplayNameFor(id)} is held through the night, but section 1.3 rules it out.");
		}
		foreach (StageDefinition far in StageCatalog.FarShore)
		{
			if (EndlessCatalog.IsEndlessChapter(far))
				warnings.Add($"Endless: {far.DisplayName}, on the far shore, is endless.");
		}
		// The road's every chapter is either endless or excluded: a chapter added to the road is a decision, not a default.
		foreach (StageDefinition road in StageCatalog.Campaign.Where(s => s.IsPlayable))
		{
			bool listed = EndlessCatalog.RoadChapterIds.Contains(road.Id, StringComparer.OrdinalIgnoreCase)
				|| EndlessCatalog.ExcludedRoadChapterIds.Contains(road.Id, StringComparer.OrdinalIgnoreCase);
			if (!listed)
				warnings.Add($"Endless: {road.DisplayName} is on the road but neither endless nor excluded (EndlessCatalog).");
		}
		// Every Vigil Kept counts the road's endless chapters: ten, as the doc says it.
		if (EndlessCatalog.RoadChapterIds.Count != 10)
			warnings.Add($"Endless: {EndlessCatalog.RoadChapterIds.Count} road chapters are endless; section 1.3 and Every Vigil Kept name ten.");
	}

	// Both curves start at 1, never fall, damage never under 1; the wall comes after the linear part and climbs faster
	// than it; the cap and the shares are positive; and the clamp holds the pace at the chapter's victory time.
	private static void ValidateEndlessStrength(List<string> warnings)
	{
		if (!Mathf.IsEqualApprox(EndlessCatalog.HealthStrength(0f), 1f) || !Mathf.IsEqualApprox(EndlessCatalog.DamageStrength(0f), 1f))
			warnings.Add($"Endless: the strength at the first fall is health x{EndlessCatalog.HealthStrength(0f):0.00}, damage x{EndlessCatalog.DamageStrength(0f):0.00}; both must be 1.");
		float lastHealth = 1f, lastDamage = 1f;
		for (float m = 0f; m <= 120f; m += 0.5f)
		{
			float health = EndlessCatalog.HealthStrength(m);
			float damage = EndlessCatalog.DamageStrength(m);
			if (health + 1e-4f < lastHealth || damage + 1e-4f < lastDamage)
			{
				warnings.Add($"Endless: the strength falls at {m:0.0} minutes past the first fall.");
				break;
			}
			if (damage < 1f)
			{
				warnings.Add($"Endless: enemy damage is under x1 at {m:0.0} minutes.");
				break;
			}
			lastHealth = health;
			lastDamage = damage;
		}
		// The order of the three stretches: a climb, then compounding, then the wall compounding harder still.
		if (EndlessCatalog.ClimbMinutes <= 0f || EndlessCatalog.WallMinutes <= EndlessCatalog.ClimbMinutes
			|| EndlessCatalog.HealthCompoundPerMinute <= 1f || EndlessCatalog.DamageCompoundPerMinute <= 1f
			|| EndlessCatalog.WallHealthPerMinute <= EndlessCatalog.HealthCompoundPerMinute
			|| EndlessCatalog.WallDamagePerMinute <= EndlessCatalog.DamageCompoundPerMinute)
			warnings.Add("Endless: the strength must climb, then compound, then compound harder past the wall, in that order.");
		// Past the wall the step a minute is larger than the linear one: it is a wall, not a gentler slope.
		float linearStep = EndlessCatalog.HealthPerMinute;
		float wallStep = EndlessCatalog.HealthStrength(EndlessCatalog.WallMinutes + 1f) - EndlessCatalog.HealthStrength(EndlessCatalog.WallMinutes);
		if (wallStep <= linearStep)
			warnings.Add($"Endless: a minute past the wall adds x{wallStep:0.000} health, no more than the linear x{linearStep:0.000}.");
		// "Every run is over by about an hour and a half" (question 3): at 90:00 on the clock the field is far past what
		// the strongest farmed build measured held (x653 at 1:33:17, still killing at the spawn supply, E5).
		float at90 = EndlessCatalog.HealthStrength(75f);
		if (at90 < 2000f)
			warnings.Add($"Endless: at 90:00 on the clock enemies carry x{at90:0.0} health; a farmed build held x653 - the wall must have closed in.");
		if (EndlessCatalog.ArcaneCap <= 0)
			warnings.Add($"Endless: the Arcane cap is {EndlessCatalog.ArcaneCap}; it must be positive.");
		if (EndlessCatalog.ReturningWardenShare <= 0f || EndlessCatalog.WardenBonusShare <= 0f || EndlessCatalog.WardenBonusShare > 1f)
			warnings.Add("Endless: the returning warden's share and his bonus share must be positive (the bonus share at most 1).");
		if (EndlessCatalog.MaxLooseOrbs <= 0 || EndlessCatalog.VigilHoldSeconds <= 0f)
			warnings.Add("Endless: the loose-orb fold and the vigil's hold must be positive.");
		// A returning warden is never weaker than the first, and each is stronger than the last (section 1.4).
		float second = EndlessCatalog.ReturningWardenScale(15f);
		float third = EndlessCatalog.ReturningWardenScale(30f);
		if (second < 1f || third <= second)
			warnings.Add($"Endless: the returning wardens are x{second:0.00} and x{third:0.00} the first's; never weaker than him, each stronger than the last.");
	}

	// Never before Taken Back, never on an excluded chapter, never before the chapter's own warden has fallen there.
	private static void ValidateEndlessUnlock(List<string> warnings)
	{
		SaveData beaten = EndlessSampleSave(takenBack: false);
		SaveData open = EndlessSampleSave(takenBack: true);
		if (EndlessCatalog.IsOpen(new SaveData()))
			warnings.Add("Endless: a new save has endless open.");
		if (EndlessCatalog.IsOpen(beaten))
			warnings.Add("Endless: a save with every warden beaten but the Throne not taken back has endless open.");
		if (!EndlessCatalog.IsOpen(open))
			warnings.Add("Endless: a save that took back the Throne does not have endless open.");
		foreach (StageDefinition stage in StageCatalog.All)
		{
			bool endless = EndlessCatalog.IsEndlessChapter(stage);
			if (EndlessCatalog.IsOpenOn(beaten, stage))
				warnings.Add($"Endless: {stage.DisplayName} can be held through the night before Taken Back.");
			if (EndlessCatalog.IsOpenOn(open, stage) != endless)
				warnings.Add($"Endless: {stage.DisplayName} {(endless ? "cannot" : "can")} be held through the night on a save that took back the Throne.");
		}
		// The chapter's own warden is Boss Rush's rule: unbeaten there, no vigil.
		StageDefinition forest = StageCatalog.GetById(EndlessCatalog.RoadChapterIds[0]);
		BossDefinition elderbark = forest != null ? BossCatalog.ForStageIndex(forest.Index) : null;
		if (elderbark != null)
		{
			SaveData unbeaten = EndlessSampleSave(takenBack: true);
			unbeaten.Lifetime.BossesDefeated.RemoveAll(id => id.Equals(elderbark.Id, StringComparison.OrdinalIgnoreCase));
			if (EndlessCatalog.IsOpenOn(unbeaten, forest))
				warnings.Add($"Endless: {forest.DisplayName} can be held through the night before {elderbark.DisplayName} has fallen there.");
		}
	}

	// Every warden of every chapter in the lifetime record; Taken Back done or not.
	private static SaveData EndlessSampleSave(bool takenBack)
	{
		var save = new SaveData();
		foreach (StageDefinition stage in StageCatalog.All)
		{
			BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
			if (boss != null && !save.Lifetime.HasDefeatedBoss(boss.Id))
				save.Lifetime.BossesDefeated.Add(boss.Id);
		}
		if (!takenBack)
			save.Lifetime.BossesDefeated.RemoveAll(id => id.Equals(AchievementDefinitions.ThroneBossId, StringComparison.OrdinalIgnoreCase));
		else if (!save.UnlockedAchievementIds.Contains(AchievementDefinitions.TakenBackId))
			save.UnlockedAchievementIds.Add(AchievementDefinitions.TakenBackId);
		return save;
	}
}
