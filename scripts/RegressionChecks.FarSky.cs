using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE FAR SKY (.ai/second-half.md section 5, piece F4; FarSky). What a build cannot see, on sample saves for every
// character on the roster:
//   - the picking rules: every recipe named is a fusion of two spells; never the one the wizard opens on; never one
//     naming his own starting spell; always makeable from the save's spells (owned, not set aside, not another
//     wizard's alone); the same night names the same recipe; two nights running never name the same one while there
//     is another; a recipe never made is named more often than one made; nothing is named with nothing to name;
//   - who sees it: nothing before the save has learned it, nothing off the far shore, a tool's override obeyed;
//   - its lesson: owed when the far shore's first god is rekindled and not before, learned once taught;
//   - its two Feats: hidden until it is learned, Under the Far Sky met by a run that made tonight's, Every
//     Conjunction Known measured against every fusion.
// Every line is a warning, as the rest of RunAll's are. The favour itself is the deal's, measured by
// _BalanceHarness --far-sky (the section 9 note).
public static partial class RegressionChecks
{
	private const int FarSkyNightsChecked = 240;

	private static void ValidateFarSky(List<string> warnings)
	{
		string before = FarSky.ToolOverride;
		FarSky.ToolOverride = null;
		try
		{
			ValidateFarSkyPicks(warnings);
			ValidateFarSkyWeighting(warnings);
			ValidateFarSkyShown(warnings);
			ValidateFarSkyLessonAndFeats(warnings);
		}
		finally
		{
			FarSky.ToolOverride = before;
		}
	}

	// A save that owns every spell, has learned everything and has made nothing: the far shore as most will reach it.
	private static SaveData FarSkySave(Action<SaveData> shape = null)
	{
		var save = new SaveData();
		foreach (string id in UnlockCatalog.AllSpellIds)
			save.UnlockedSpellIds.Add(id);
		foreach (string id in UnlockCatalog.AllCharacterIds)
			save.UnlockedCharacterIds.Add(id);
		save.AscensionLearned = true;
		foreach (Teachings.Lesson lesson in Teachings.All)
			Teachings.Teach(save, lesson);
		shape?.Invoke(save);
		return save;
	}

	private static void ValidateFarSkyPicks(List<string> warnings)
	{
		if (!ComboCatalog.Fusions.Any())
		{
			warnings.Add("Far sky: ComboCatalog has no fusion for the sky to name.");
			return;
		}
		ComboDefinition first = ComboCatalog.Fusions.First();
		var saves = new List<(string Name, SaveData Save)>
		{
			("a save with every spell", FarSkySave()),
			// One parent of the first recipe never owned: that recipe must never be named.
			("a save without " + first.First.SpellName, FarSkySave(s =>
				s.UnlockedSpellIds.RemoveAll(id => id.Equals(first.First.SpellId, StringComparison.OrdinalIgnoreCase)))),
			// One parent set aside (the spellbook's Remove): owned, never dealt, so never named.
			("a save that set " + first.Second.SpellName + " aside", FarSkySave(s => s.RemovedSpellIds.Add(first.Second.SpellId))),
			// Every recipe made once: still named, at the made weight.
			("a save that has made every recipe", FarSkySave(s =>
			{
				foreach (ComboDefinition r in ComboCatalog.Fusions)
					s.UnlockedAchievementIds.Add(ComboCatalog.FeatId(r.Id));
			})),
		};

		int named = 0, characters = 0, warningsBefore = warnings.Count;
		var namedAtAll = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (CharacterData character in CharacterRoster.GetAll().Where(c => c != null))
		{
			string own = FarSky.OwnSpellOf(character);
			characters++;
			foreach (var (name, save) in saves)
			{
				List<ComboDefinition> candidates = FarSky.Candidates(save, character.Id, own);
				ComboDefinition previous = null;
				for (int night = 0; night < FarSkyNightsChecked; night++)
				{
					ComboDefinition pick = FarSky.PickForNight(save, character.Id, own, night);
					if (pick == null)
					{
						if (candidates.Count > 0)
							warnings.Add($"Far sky: night {night} names nothing for '{character.Id}' on {name}, with {candidates.Count} recipes it could name.");
						break;
					}
					named++;
					namedAtAll.Add(pick.Id);
					string where = $"night {night}, '{character.Id}' on {name}";
					if (!ComboCatalog.Fusions.Contains(pick))
						warnings.Add($"Far sky: {where} names '{pick.Id}', which is not a fusion of two spells.");
					if (ComboCatalog.StartsFused(pick, character.Id))
						warnings.Add($"Far sky: {where} names '{pick.Id}', which {character.Id} opens on.");
					if (!string.IsNullOrEmpty(own) && pick.Ingredients.Any(i => i.SpellId.Equals(own, StringComparison.OrdinalIgnoreCase)))
						warnings.Add($"Far sky: {where} names '{pick.Id}', which names {character.Id}'s own starting spell '{own}' (ComboSeal.OwnSpell).");
					foreach (ComboIngredient parent in pick.Ingredients)
					{
						if (!GlobalStatsManager.IsSpellUnlockedForLevelUp(save, parent.SpellId)
							|| GlobalStatsManager.IsSpellRemovedFromPool(save, parent.SpellId)
							|| !UnlockCatalog.MayBeOffered(parent.SpellId, character.Id))
							warnings.Add($"Far sky: {where} names '{pick.Id}', but its parent '{parent.SpellId}' cannot be dealt from that save.");
					}
					if (!candidates.Contains(pick))
						warnings.Add($"Far sky: {where} names '{pick.Id}', which FarSky.Candidates does not list.");
					if (previous != null && pick == previous && candidates.Count > 1)
						warnings.Add($"Far sky: {where} names '{pick.Id}' two nights running, with {candidates.Count} to name.");
					if (night < 8 && FarSky.PickForNight(save, character.Id, own, night) != pick)
						warnings.Add($"Far sky: {where} is not the same recipe asked twice.");
					previous = pick;
				}
			}
		}
		if (named == 0)
			warnings.Add("Far sky: no character on any sample save was ever named a recipe.");
		GD.Print($"RegressionChecks: the far sky over {FarSkyNightsChecked} nights for {characters} characters on {saves.Count} saves - "
			+ $"{named} nights named, {namedAtAll.Count} of {ComboCatalog.Fusions.Count()} fusions named at all, "
			+ $"{warnings.Count - warningsBefore} broken rules (an opener, an own spell, an unmakeable parent, a repeat)");

		// A save that owns no parent of anything names nothing, for anyone.
		var bare = new SaveData();
		Teachings.Teach(bare, Teachings.FarSkyLesson);
		if (CharacterRoster.GetAll().Where(c => c != null).Any(c => FarSky.Candidates(bare, c.Id, FarSky.OwnSpellOf(c))
				.Any(r => r.Ingredients.Any(i => !UnlockCatalog.IsStarter(i.SpellId, UnlockKind.Spell)))))
			warnings.Add("Far sky: a save that owns only the starters is named a recipe whose parent it does not own.");
	}

	// "Weighted toward recipes the save has never made": with one recipe made, every never-made one is named more often.
	private static void ValidateFarSkyWeighting(List<string> warnings)
	{
		CharacterData wide = CharacterRoster.GetAll().Where(c => c != null && !c.Id.Equals("test_wizard", StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(c => FarSky.Candidates(FarSkySave(), c.Id, FarSky.OwnSpellOf(c)).Count).FirstOrDefault();
		if (wide == null)
			return;
		List<ComboDefinition> all = FarSky.Candidates(FarSkySave(), wide.Id, FarSky.OwnSpellOf(wide));
		// Two to name alternate by the no-repeat rule, whatever their weights: the weighting needs three.
		if (all.Count < 3)
			return;
		ComboDefinition made = all[0];
		SaveData save = FarSkySave(s => s.UnlockedAchievementIds.Add(ComboCatalog.FeatId(made.Id)));
		var counts = all.ToDictionary(r => r.Id, _ => 0);
		const int nights = 600;
		for (int night = 0; night < nights; night++)
			counts[FarSky.PickForNight(save, wide.Id, FarSky.OwnSpellOf(wide), night).Id]++;
		GD.Print($"RegressionChecks: the far sky's weighting for '{wide.Id}' over {nights} nights with '{made.Id}' made - "
			+ string.Join(", ", all.Select(r => $"{r.Id} {counts[r.Id]}")));
		foreach (ComboDefinition other in all.Skip(1))
			if (counts[other.Id] <= counts[made.Id])
				warnings.Add($"Far sky: over {nights} nights for '{wide.Id}', the never-made '{other.Id}' was named {counts[other.Id]} times and the made '{made.Id}' {counts[made.Id]}: the weighting does not favour the never made.");
	}

	private static void ValidateFarSkyShown(List<string> warnings)
	{
		StageDefinition far = StageCatalog.FarShore.FirstOrDefault();
		StageDefinition road = StageCatalog.Campaign.FirstOrDefault();
		CharacterData wizard = CharacterRoster.GetAll().FirstOrDefault(c => c != null && c.Id == "laelaps")
			?? CharacterRoster.GetAll().FirstOrDefault(c => c != null && !c.Id.Equals("test_wizard", StringComparison.OrdinalIgnoreCase));
		if (far == null || road == null || wizard == null)
		{
			warnings.Add("Far sky: no far-shore chapter, road chapter or wizard to check who sees the sky.");
			return;
		}
		SaveData learned = FarSkySave();
		SaveData unlearned = FarSkySave(s => s.LessonsLearned.RemoveAll(id => id.Equals(Teachings.FarSkyLesson.Id, StringComparison.OrdinalIgnoreCase)));

		if (FarSky.Tonight(unlearned, wizard) != null || FarSky.ForRun(unlearned, wizard, far) != null || FarSky.CardLine(unlearned, far, wizard) != null)
			warnings.Add("Far sky: a save that has not learned the far sky is named a recipe (Tonight, ForRun or the card).");
		ComboDefinition tonight = FarSky.Tonight(learned, wizard);
		if (tonight == null)
			warnings.Add($"Far sky: a save with every spell that has learned the far sky names nothing for '{wizard.Id}'.");
		else
		{
			if (FarSky.ForRun(learned, wizard, far) != tonight)
				warnings.Add($"Far sky: a run on {far.DisplayName} is not played under tonight's recipe.");
			string card = FarSky.CardLine(learned, far, wizard);
			if (card == null || !card.Contains(tonight.Name, StringComparison.Ordinal)
				|| !card.Contains(tonight.First.AscensionName, StringComparison.Ordinal) || !card.Contains(tonight.Second.AscensionName, StringComparison.Ordinal))
				warnings.Add($"Far sky: {far.DisplayName}'s card line \"{card}\" does not name tonight's {tonight.Name} and both its Ascensions.");
			// The night turns with the run count, and the next night names another (the no-repeat rule).
			int night = FarSky.Night(learned);
			learned.Lifetime.TotalRuns = night + 1;
			if (FarSky.Candidates(learned, wizard.Id, FarSky.OwnSpellOf(wizard)).Count > 1 && FarSky.Tonight(learned, wizard) == tonight)
				warnings.Add("Far sky: the sky did not turn when a run ended.");
			learned.Lifetime.TotalRuns = night;
		}
		if (FarSky.ForRun(learned, wizard, road) != null || FarSky.CardLine(learned, road, wizard) != null)
			warnings.Add($"Far sky: a run on the road ({road.DisplayName}) is named a recipe; the far sky is the far shore's own.");

		ComboDefinition forced = ComboCatalog.Fusions.First();
		FarSky.ToolOverride = forced.Id;
		if (FarSky.ForRun(unlearned, wizard, road) != forced)
			warnings.Add("Far sky: --far-sky=ID did not name that recipe on a road chapter.");
		FarSky.ToolOverride = FarSky.Off;
		if (FarSky.ForRun(learned, wizard, far) != null)
			warnings.Add("Far sky: --far-sky=off still named a recipe on the far shore.");
		FarSky.ToolOverride = ComboCatalog.Rite?.Id ?? "no_such_recipe";
		if (FarSky.ForRun(learned, wizard, far) != null)
			warnings.Add("Far sky: a tool naming the Sovereign Rite (or no recipe) was given one; the sky names fusions of two spells only.");
		FarSky.ToolOverride = null;
	}

	private static void ValidateFarSkyLessonAndFeats(List<string> warnings)
	{
		Teachings.Lesson lesson = Teachings.FarSkyLesson;
		if (!Teachings.All.Contains(lesson) || lesson.System != Teaching.FarSky)
			warnings.Add("Far sky: its lesson is not in Teachings.All under Teaching.FarSky.");
		string teacher = Teachings.FarSkyTeacherId;
		StageDefinition seat = StageCatalog.FarShore.FirstOrDefault();
		if (string.IsNullOrEmpty(teacher) || !UnlockCatalog.IsGod(teacher))
			warnings.Add($"Far sky: no god is seated at {seat?.DisplayName ?? "the far shore's first chapter"} to teach it (UnlockCatalog.GodSeatedAt).");
		else
		{
			var fresh = new SaveData();
			if (Teachings.HasLearned(fresh, Teaching.FarSky) || Teachings.IsShown(fresh, Teaching.FarSky) || Teachings.Owed(fresh).Contains(lesson))
				warnings.Add("Far sky: a new save has learned it, is shown it, or owes it.");
			var rekindled = new SaveData();
			rekindled.UnlockedCharacterIds.Add(teacher);
			List<Teachings.Lesson> owed = Teachings.Owed(rekindled);
			if (!owed.Contains(lesson))
				warnings.Add($"Far sky: a save that has rekindled '{teacher}' does not owe its lesson.");
			if (Teachings.HasLearned(rekindled, Teaching.FarSky) || Teachings.IsShown(rekindled, Teaching.FarSky))
				warnings.Add("Far sky: a save owed the lesson has learned it, or is shown it, before the reel plays.");
			Teachings.Teach(rekindled, lesson);
			if (!Teachings.HasLearned(rekindled, Teaching.FarSky) || !Teachings.IsShown(rekindled, Teaching.FarSky)
				|| Teachings.Owed(rekindled).Contains(lesson) || Teachings.Teach(rekindled, lesson))
				warnings.Add("Far sky: once taught, the lesson is not learned and shown, is still owed, or was taught twice.");
			// Another god rekindled first teaches nothing: the lesson is the first seat's god's.
			UnlockDefinition other = UnlockCatalog.Gods.FirstOrDefault(g => !g.Id.Equals(teacher, StringComparison.OrdinalIgnoreCase));
			if (other != null)
			{
				var wrongGod = new SaveData();
				wrongGod.UnlockedCharacterIds.Add(other.Id);
				if (Teachings.Owed(wrongGod).Contains(lesson))
					warnings.Add($"Far sky: rekindling '{other.Id}' owes its lesson; only '{teacher}' teaches it.");
			}
		}

		AchievementDefinition under = AchievementDefinitions.GetById(FarSky.UnderTheFarSkyId);
		AchievementDefinition every = AchievementDefinitions.GetById(FarSky.EveryConjunctionKnownId);
		if (under == null || every == null)
		{
			warnings.Add("Far sky: Under the Far Sky or Every Conjunction Known is missing from AchievementDefinitions.");
			return;
		}
		SaveData unlearned = new SaveData();
		SaveData learned = FarSkySave();
		foreach (AchievementDefinition deed in new[] { under, every })
		{
			if (deed.Visibility != AchievementVisibility.FarSky || AchievementDefinitions.IsListed(deed, unlearned) || !AchievementDefinitions.IsListed(deed, learned))
				warnings.Add($"Far sky: '{deed.Id}' is not hidden until the far sky is learned and listed after (AchievementVisibility.FarSky).");
			if (deed.CurrencyReward <= 0 || !string.IsNullOrEmpty(deed.SpellUnlockId) || !string.IsNullOrEmpty(deed.CharacterUnlockId) || !string.IsNullOrEmpty(deed.BoonUnlockId))
				warnings.Add($"Far sky: '{deed.Id}' should pay Arcane Energy only (.ai/second-half.md 4.3).");
		}
		if (!under.IsComplete(AchievementContext.ForRun(learned, new RunResult { FarSkyMade = true, FarSkyRecipeId = ComboCatalog.Fusions.First().Id })))
			warnings.Add("Far sky: a run that made tonight's Conjunction does not meet Under the Far Sky.");
		if (under.IsComplete(AchievementContext.ForRun(learned, new RunResult { FarSkyRecipeId = ComboCatalog.Fusions.First().Id })))
			warnings.Add("Far sky: a run that did not make tonight's Conjunction meets Under the Far Sky.");
		int fusions = ComboCatalog.Fusions.Count();
		if (every.Kind != AchievementKind.Collection || Math.Abs(every.Target - fusions) > 0.01f)
			warnings.Add($"Far sky: Every Conjunction Known is not a Collection whose target is every fusion ({fusions}).");
		SaveData madeAll = FarSkySave(s =>
		{
			foreach (ComboDefinition r in ComboCatalog.Fusions)
				s.UnlockedAchievementIds.Add(ComboCatalog.FeatId(r.Id));
		});
		SaveData madeAllButOne = FarSkySave(s =>
		{
			foreach (ComboDefinition r in ComboCatalog.Fusions.Skip(1))
				s.UnlockedAchievementIds.Add(ComboCatalog.FeatId(r.Id));
		});
		if (!every.IsComplete(AchievementContext.ForSave(madeAll)) || every.IsComplete(AchievementContext.ForSave(madeAllButOne)))
			warnings.Add("Far sky: Every Conjunction Known is not met by every recipe made, or is met one short.");
	}
}
