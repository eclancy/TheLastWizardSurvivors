using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// The slow start's checks (.ai/slow-start.md): the book's arithmetic, the teachers' order and art, and the
// schema 22 step on sample saves. Every line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	// THE BOOK (SpellBook). Three on a new save, the deeds' pages and the shop's levels must add up to the full
	// book exactly: one short and a new save can never reach six, one over and a page is paid for nothing. A
	// page deed must be one a player meets on the road - a Feat, or a Road deed before the first gate - or the
	// sixth page waits behind content the first wall is tuned without (the ValidateGateReachability rule).
	private static void ValidateSpellBook(List<string> warnings)
	{
		int deedPages = AchievementDefinitions.All.Sum(d => Math.Max(0, d.PageReward));
		int total = SpellBook.NewSavePages + deedPages + SpellBook.ShopLevels;
		if (total != Player.MaxSpellSlots)
			warnings.Add($"Spell book: {SpellBook.NewSavePages} + {deedPages} deed pages + {SpellBook.ShopLevels} bought is {total}, not the full book of {Player.MaxSpellSlots}.");

		StageDefinition firstGate = StageCatalog.Campaign.FirstOrDefault(s => s.Gate == StageGate.PreviousBossAndWizards);
		int gateAt = firstGate != null ? StageCatalog.CampaignPositionOf(firstGate.Id) : int.MaxValue;
		foreach (AchievementDefinition deed in AchievementDefinitions.All.Where(d => d.PageReward > 0))
		{
			bool onRoad = deed.Kind == AchievementKind.Feat
				|| (deed.Kind == AchievementKind.Road && StageCatalog.CampaignPositionOf(deed.StageId) is int at && at >= 0 && at < gateAt);
			if (!onRoad)
				warnings.Add($"Spell book: '{deed.Id}' pays a page but is not a Feat or a Road deed before the first gate.");
			if (deed.RewardText.IndexOf("page", StringComparison.OrdinalIgnoreCase) < 0)
				warnings.Add($"Spell book: '{deed.Id}' pays a page and its reward line does not say so ('{deed.RewardText}').");
		}

		var fresh = new SaveData();
		if (SpellBook.PagesFor(fresh) != SpellBook.NewSavePages)
			warnings.Add($"Spell book: a new save holds {SpellBook.PagesFor(fresh)}, not {SpellBook.NewSavePages}.");
		var kept = new SaveData { FullBookKept = true };
		if (SpellBook.PagesFor(kept) != Player.MaxSpellSlots || SpellBook.ShopLevelShown(kept) != SpellBook.ShopLevels)
			warnings.Add("Spell book: a kept book should hold the full book and show Bound Pages complete.");
		var earned = new SaveData();
		foreach (AchievementDefinition deed in AchievementDefinitions.All.Where(d => d.PageReward > 0))
			earned.UnlockedAchievementIds.Add(deed.Id);
		earned.ArcaneUpgradeLevels[SpellBook.ShopUpgradeId] = SpellBook.ShopLevels + 3;
		if (SpellBook.PagesFor(earned) != Player.MaxSpellSlots)
			warnings.Add($"Spell book: every page earned and more than every level bought holds {SpellBook.PagesFor(earned)}, not {Player.MaxSpellSlots}.");
		if (SpellBook.PagesFor(null) != Player.MaxSpellSlots)
			warnings.Add("Spell book: with no save (the tools) the book is not the full one.");

		// The Blank Folio's card is about the run's book, which is three to six now, never "instead of 6".
		if (ChestItemCatalog.BlankFolioDescription.Contains(Player.MaxSpellSlots.ToString()))
			warnings.Add($"Blank Folio: its card names the full book ({ChestItemCatalog.BlankFolioDescription}); a new save's book is smaller.");
		// Every relic's card is read for a system it names (Eric's Q9); none may throw on the way.
		foreach (string id in ChestItemCatalog.AllItemIds)
			ChestItemCatalog.TeachingNamedBy(id);
	}

	// THE TEACHERS (Teachings). Each rung of the ladder is taught by a built spell vault in a playable chapter,
	// the three in campaign order; every lesson has a reel and every panel of it is on disk; the Rite is still
	// the Warden of the Deep's. Then the rules on paper: a fresh save is shown the Ascension and nothing after
	// it; a teacher found out of order teaches the lowest rung, never one the save cannot use.
	private static void ValidateTeachers(List<string> warnings)
	{
		int last = -1;
		foreach (Teachings.Lesson lesson in Teachings.Ladder)
		{
			SecretDefinition secret = SecretCatalog.Get(lesson.TeacherSecretId);
			if (secret == null || !secret.IsBuilt || secret.MetaReward != SecretMetaReward.Spell)
			{
				warnings.Add($"Teachings: {lesson.Name}'s teacher '{lesson.TeacherSecretId}' is not a built spell vault.");
				continue;
			}
			StageDefinition stage = StageCatalog.GetById(secret.StageId);
			int at = StageCatalog.CampaignPositionOf(secret.StageId);
			if (stage == null || !stage.IsPlayable || at < 0)
				warnings.Add($"Teachings: {lesson.Name}'s teacher stands in '{secret.StageId}', which is not a playable chapter of the campaign.");
			else if (at <= last)
				warnings.Add($"Teachings: {lesson.Name}'s teacher ({stage.DisplayName}) is not after the rung before it in the campaign.");
			last = Math.Max(last, at);
		}

		foreach (Teachings.Lesson lesson in Teachings.All)
		{
			if (!Prologue.Reels.TryGetValue(lesson.Id, out Prologue.Reel reel))
			{
				warnings.Add($"Teachings: {lesson.Name} has no reel in Prologue.Reels.");
				continue;
			}
			foreach (Prologue.Beat beat in reel.Beats)
			{
				foreach (string layer in new[] { "back", "mid", "fore" })
				{
					string path = $"{reel.Folder}{beat.Name}-{layer}.png";
					if (!ResourceLoader.Exists(path))
						warnings.Add($"Teachings: {lesson.Name}'s panel {path} is missing or not imported (python tools/art/lessons.py, then import).");
				}
				if (string.IsNullOrWhiteSpace(beat.Caption) || beat.Caption.Length > 46)
					warnings.Add($"Teachings: {lesson.Name}'s panel '{beat.Name}' needs one short line (46 characters at most: one line on a PC).");
			}
		}

		AchievementDefinition warden = AchievementDefinitions.GetById("emberdeep_ended");
		if (warden == null || !SovereignRite.SpellId.Equals(warden.SpellUnlockId, StringComparison.OrdinalIgnoreCase))
			warnings.Add("Teachings: the Sovereign Rite is no longer the Warden of the Deep's deed; its lesson is owed by owning it.");

		var fresh = new SaveData();
		foreach (Teachings.Lesson teacher in Teachings.Ladder)
			if (Teachings.LessonAt(fresh, teacher.TeacherSecretId)?.System != Teaching.Ascension
				|| string.IsNullOrWhiteSpace(Teachings.LandmarkHint(teacher.TeacherSecretId)))
				warnings.Add($"Teachings: an unlearned save needs an Ascension hint and guidance at {teacher.TeacherSecretId}, even when visited out of order.");
		// Owning a vault's spell is not evidence of having found its teacher. It must still have a lead.
		var ownsFamiliar = new SaveData();
		ownsFamiliar.UnlockedSpellIds.Add("familiar");
		if (Teachings.LessonAt(ownsFamiliar, SecretCatalog.PinfoldId) == null)
			warnings.Add("Teachings: owning the Familiar must not hide the Pinfold's lesson guidance.");
		if (!Teachings.IsShown(fresh, Teaching.Ascension) || Teachings.IsShown(fresh, Teaching.Apotheosis)
			|| Teachings.IsShown(fresh, Teaching.Conjunctions) || Teachings.Ladder.Any(l => Teachings.HasLearned(fresh, l.System))
			|| Teachings.Owed(fresh).Count > 0)
			warnings.Add("Teachings: a new save should be shown the Ascension, sealed, and nothing after it, and owe nothing.");

		// Out of order: the Circle's landmark found first teaches the Ascension, not the Apotheosis.
		var skipped = new SaveData();
		skipped.RecordSecretFound(Teachings.Ladder[1].TeacherSecretId);
		List<Teachings.Lesson> owed = Teachings.Owed(skipped);
		if (Teachings.LessonAt(skipped, Teachings.Ladder[1].TeacherSecretId) != null
			|| Teachings.LessonAt(skipped, Teachings.Ladder[0].TeacherSecretId)?.System != Teaching.Apotheosis)
			warnings.Add("Teachings: a found teacher needs no trail; the next teacher hints the next lesson, including one already owed.");
		if (owed.Count != 1 || owed[0].System != Teaching.Ascension)
			warnings.Add($"Teachings: a save that found only the second teacher owes [{string.Join(", ", owed.Select(l => l.Id))}], not the Ascension alone.");
		Teachings.Teach(skipped, owed.FirstOrDefault());
		if (!Teachings.HasLearned(skipped, Teaching.Ascension) || Teachings.Owed(skipped).Count != 0
			|| !Teachings.IsShown(skipped, Teaching.Apotheosis) || Teachings.IsShown(skipped, Teaching.Conjunctions))
			warnings.Add("Teachings: once the Ascension is taught, it is learned, nothing more is owed, and only the Apotheosis is shown next.");
		if (Teachings.Teach(skipped, Teachings.Get(Teaching.Ascension)))
			warnings.Add("Teachings: a lesson was taught twice.");
		var taught = new SaveData();
		foreach (Teachings.Lesson lesson in Teachings.Ladder)
			Teachings.Teach(taught, lesson);
		if (Teachings.Ladder.Any(l => Teachings.LessonAt(taught, l.TeacherSecretId) != null))
			warnings.Add("Teachings: a save taught every lesson should have no teaching trail, even with unfound vaults.");
		skipped.RecordSecretFound(Teachings.Ladder[0].TeacherSecretId);
		owed = Teachings.Owed(skipped);
		if (owed.Count != 1 || owed[0].System != Teaching.Apotheosis)
			warnings.Add($"Teachings: the second teacher found should owe the Apotheosis; it owes [{string.Join(", ", owed.Select(l => l.Id))}].");
	}

	// SCHEMA 22 (.ai/slow-start.md section 6), on sample saves at 21 (the step before it, which is the Dungeon
	// move's and not on this branch):
	//   - a played save keeps six pages and the three systems, owes no lesson, and its shop row reads complete;
	//   - a played save that never had the Ascension keeps six pages and nothing else;
	//   - a save that beat the Hayward or opened a chapter past the Fields had the Ascension under the old rule,
	//     and keeps all three;
	//   - a save that owns the Sovereign Rite is not shown its lesson;
	//   - an unplayed save and a new one start on three pages, unlearned;
	//   - a second migration changes nothing.
	private static void ValidateSlowStartMigration(List<string> warnings)
	{
		SaveData At21(Action<SaveData> shape)
		{
			var save = new SaveData { SchemaVersion = 21 };
			shape(save);
			save.Migrate(log: false);
			return save;
		}
		bool AllLearned(SaveData s) => Teachings.Ladder.All(l => Teachings.HasLearned(s, l.System));
		bool NoneLearned(SaveData s) => Teachings.Ladder.All(l => !Teachings.HasLearned(s, l.System));

		SaveData played = At21(s => { s.Lifetime.TotalRuns = 4; s.AscensionLearned = true; });
		if (!played.FullBookKept || SpellBook.PagesFor(played) != Player.MaxSpellSlots || !AllLearned(played) || Teachings.Owed(played).Count > 0)
			warnings.Add("Slow start migration: a played save with the Ascension should keep six pages and all three systems, and owe no lesson.");
		int lessons = played.LessonsLearned.Count;
		played.SchemaVersion = 21;
		played.Migrate(log: false);
		if (played.LessonsLearned.Count != lessons || !played.FullBookKept)
			warnings.Add("Slow start migration: a second migration changed the save.");

		SaveData playedBare = At21(s => s.RunTelemetryHistory.Add(new RunTelemetryRecord()));
		if (!playedBare.FullBookKept || !NoneLearned(playedBare))
			warnings.Add("Slow start migration: a played save that never had the Ascension should keep six pages and learn nothing.");

		SaveData beatHayward = At21(s => { s.Lifetime.TotalRuns = 2; s.Lifetime.BossesDefeated.Add("fields_hayward"); });
		SaveData pastFields = At21(s => { s.Lifetime.TotalRuns = 2; s.UnlockedStageIds.Add("stage_2"); });
		if (!AllLearned(beatHayward) || !AllLearned(pastFields))
			warnings.Add("Slow start migration: a save that had the Ascension through the Hayward or a chapter past the Fields lost it.");

		SaveData rite = At21(s => { s.Lifetime.TotalRuns = 9; s.AscensionLearned = true; s.UnlockedSpellIds.Add(SovereignRite.SpellId); });
		if (!Teachings.HasBeenTaught(rite, Teachings.Rite) || Teachings.Owed(rite).Count > 0)
			warnings.Add("Slow start migration: a save that owns the Sovereign Rite should not be owed its lesson.");

		SaveData unplayed = At21(_ => { });
		if (unplayed.FullBookKept || SpellBook.PagesFor(unplayed) != SpellBook.NewSavePages || !NoneLearned(unplayed))
			warnings.Add("Slow start migration: a save that never played should start on three pages, unlearned, as a new save does.");
		var brandNew = new SaveData();
		if (brandNew.Migrate(log: false) || brandNew.FullBookKept || !NoneLearned(brandNew))
			warnings.Add("Slow start migration: a brand-new save was migrated, or starts with the full book or a system learned.");

		// Through the file format: what the step writes survives a save and a load.
		SaveData reread = SaveData.FromVariant(played.ToGodotDictionary());
		if (!reread.FullBookKept || !AllLearned(reread))
			warnings.Add("Slow start migration: FullBookKept or LessonsLearned is lost between a save and a load.");
	}
}
