using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE CONJUNCTION LADDER (.ai/conjunction-progression.md; ConjunctionLadder). What a build cannot see, rung by rung:
//   I   know it - a recipe is shown from one half only once the Conjunctions lesson is learned (from both halves, as
//       before, from the Apotheosis on), never the Rite; its Feat's row follows; a pin is refused before the lesson, for
//       the Rite and for a recipe the save knows nothing of, is one at a time, survives the save's round trip and a
//       story reset clears it; the character select's Suits is derived by its rule and never names a recipe of his own
//       spell or the one he opens on; the level 4 mark ("Seals ...") agrees with ComboCatalog.Evaluate's own fourth-
//       element seal on every parent's every level 4 path;
//   II  Kindred Draw - its row shown only once the lesson is learned, priced 100 / 160 / 220, x2 / x3 / x4; it re-weighs
//       only the recipe's parents' cards already in a pool and adds or removes none, so it can never deal a card the
//       save does not own.
// Every line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	private static void ValidateConjunctionLadder(List<string> warnings)
	{
		int before = warnings.Count;
		ValidateLadderShown(warnings);
		ValidateLadderPin(warnings);
		ValidateLadderSuits(warnings);
		ValidateLadderSealMark(warnings);
		ValidateKindredDraw(warnings);
		if (warnings.Count == before)
			GD.Print($"ContentValidator: the Conjunction ladder, rungs I-II - {ComboCatalog.Fusions.Count()} recipes shown from one half only once learned, the pin, Suits and the level 4 seal mark agree, Kindred Draw leans and never adds.");
	}

	// A save that has learned up to and including <paramref name="through"/> (null: nothing), owning <paramref name="spells"/>.
	private static SaveData LadderSave(Teaching? through, params string[] spells)
	{
		var save = new SaveData();
		foreach (string id in spells)
			GlobalStatsManager.UnlockSpell(save, id);
		if (through != null)
		{
			save.AscensionLearned = true;
			foreach (Teachings.Lesson lesson in Teachings.Ladder)
			{
				Teachings.Teach(save, lesson);
				if (lesson.System == through)
					break;
			}
		}
		return save;
	}

	private static void ValidateLadderShown(List<string> warnings)
	{
		foreach (ComboDefinition recipe in ComboCatalog.Fusions)
		{
			string one = recipe.First.SpellId, other = recipe.Second.SpellId;
			// One half, the Apotheosis learned but not Conjunctions: not shown (one locked thing at a time).
			SaveData early = LadderSave(Teaching.Apotheosis, one);
			if (ConjunctionLadder.IsShown(recipe, early))
				warnings.Add($"Ladder I: '{recipe.Id}' is shown from one half ({one}) on a save that has not learned Conjunctions.");
			// Both halves, the Apotheosis learned: shown, as it always was (ComboCatalog.IsKnown).
			if (!ConjunctionLadder.IsShown(recipe, LadderSave(Teaching.Apotheosis, one, other)))
				warnings.Add($"Ladder I: '{recipe.Id}' is not shown on a save that owns both halves.");
			// One half, Conjunctions learned: shown - either half.
			foreach (string half in new[] { one, other })
				if (!ConjunctionLadder.IsShown(recipe, LadderSave(Teaching.Conjunctions, half)))
					warnings.Add($"Ladder I: '{recipe.Id}' is not shown from one half ({half}) once Conjunctions are learned.");
			// Neither half: never.
			if (ConjunctionLadder.IsShown(recipe, LadderSave(Teaching.Conjunctions)))
				warnings.Add($"Ladder I: '{recipe.Id}' is shown on a save that owns neither half.");

			// The Feat's row follows the recipe, and still reads sealed until made.
			AchievementDefinition feat = AchievementDefinitions.All.FirstOrDefault(d => d.Id == ComboCatalog.FeatId(recipe.Id));
			if (feat == null)
				continue;
			SaveData half1 = LadderSave(Teaching.Conjunctions, one);
			if (!AchievementDefinitions.IsListed(feat, half1))
				warnings.Add($"Ladder I: the Feat row for '{recipe.Id}' is not listed on a save that knows it from one half.");
			if (AchievementDefinitions.IsListed(feat, early))
				warnings.Add($"Ladder I: the Feat row for '{recipe.Id}' is listed from one half before Conjunctions are learned.");
			if (ConjunctionLadder.ShownName(recipe, half1).Contains(recipe.Name, StringComparison.OrdinalIgnoreCase)
				&& !recipe.Name.Contains(recipe.First.SpellName) && !recipe.Name.Contains(recipe.Second.SpellName))
				warnings.Add($"Ladder I: '{recipe.Id}' is named before it is made (ConjunctionLadder.ShownName).");
		}
		ComboDefinition rite = ComboCatalog.Rite;
		if (rite != null && ConjunctionLadder.IsShown(rite, LadderSave(Teaching.Conjunctions, rite.First.SpellId)))
			warnings.Add("Ladder I: the Sovereign Rite is shown by the ladder's rule; it keeps its own.");
	}

	private static void ValidateLadderPin(List<string> warnings)
	{
		ComboDefinition recipe = ComboCatalog.Fusions.FirstOrDefault();
		if (recipe == null)
			return;
		SaveData early = LadderSave(Teaching.Apotheosis, recipe.First.SpellId, recipe.Second.SpellId);
		if (ConjunctionLadder.Pin(early, recipe.Id))
			warnings.Add($"Ladder I: '{recipe.Id}' was pinned before the Conjunctions lesson.");
		early.PinnedRecipeId = recipe.Id;
		if (ConjunctionLadder.Pinned(early) != null)
			warnings.Add("Ladder I: a pin is read on a save that has not learned Conjunctions.");

		SaveData learned = LadderSave(Teaching.Conjunctions, recipe.First.SpellId);
		if (ComboCatalog.Rite != null && ConjunctionLadder.Pin(learned, ComboCatalog.Rite.Id))
			warnings.Add("Ladder I: the Sovereign Rite was pinned.");
		if (ConjunctionLadder.Pin(LadderSave(Teaching.Conjunctions), recipe.Id))
			warnings.Add($"Ladder I: '{recipe.Id}' was pinned on a save that owns neither half.");
		if (!ConjunctionLadder.Pin(learned, recipe.Id) || ConjunctionLadder.Pinned(learned) != recipe)
			warnings.Add($"Ladder I: '{recipe.Id}' could not be pinned from one half once learned.");

		// One at a time: a second pin replaces the first.
		ComboDefinition second = ComboCatalog.Fusions.Skip(1).FirstOrDefault();
		if (second != null)
		{
			GlobalStatsManager.UnlockSpell(learned, second.First.SpellId);
			if (!ConjunctionLadder.Pin(learned, second.Id) || ConjunctionLadder.Pinned(learned) != second)
				warnings.Add("Ladder I: a second pin did not replace the first.");
		}

		// The save's round trip keeps it; a story reset does not (it is progress, not a setting).
		ComboDefinition held = ConjunctionLadder.Pinned(learned);
		SaveData back = SaveData.FromVariant(learned.ToGodotDictionary());
		if (held != null && !string.Equals(back.PinnedRecipeId, held.Id, StringComparison.OrdinalIgnoreCase))
			warnings.Add("Ladder I: the pin did not survive the save's round trip.");
		if (!string.IsNullOrEmpty(learned.StartNewStory().PinnedRecipeId))
			warnings.Add("Ladder I: a story reset kept the pin (it is progress, not a setting).");
		if (!ConjunctionLadder.Unpin(learned) || ConjunctionLadder.Pinned(learned) != null)
			warnings.Add("Ladder I: Unpin did not take the pin off.");
	}

	private static void ValidateLadderSuits(List<string> warnings)
	{
		SaveData full = LadderSave(Teaching.Conjunctions, UnlockCatalog.AllSpellIds.ToArray());
		SaveData early = LadderSave(Teaching.Apotheosis, UnlockCatalog.AllSpellIds.ToArray());
		var table = new List<string>();
		foreach (CharacterData character in CharacterRoster.GetAll().Where(c => c != null))
		{
			if (ConjunctionLadder.Suits(character, early).Count > 0)
				warnings.Add($"Ladder I: the character select names Conjunctions '{character.Id}' suits before the lesson is learned.");
			List<ComboDefinition> suits = ConjunctionLadder.Suits(character, full);
			string own = character.StartingSpellResource?.Id ?? string.Empty;
			var ownElements = new HashSet<Element>((character.StartingSpellResource?.ElementWeights ?? new Godot.Collections.Dictionary<string, int>())
				.Where(p => p.Value > 0 && Enum.TryParse(p.Key, true, out Element _)).Select(p => Enum.Parse<Element>(p.Key, true)));
			foreach (ComboDefinition recipe in suits)
			{
				if (recipe.Ingredients.Any(i => i.SpellId.Equals(own, StringComparison.OrdinalIgnoreCase)))
					warnings.Add($"Ladder I: '{character.Id}' is said to suit '{recipe.Id}', which names his own {own}.");
				if (ComboCatalog.StartsFused(recipe, character.Id))
					warnings.Add($"Ladder I: '{character.Id}' is said to suit '{recipe.Id}', which he opens on.");
				if (!recipe.Ingredients.Any(i => ConjunctionLadder.NamedPathGateElements(i).Any(ownElements.Contains)))
					warnings.Add($"Ladder I: '{character.Id}' is said to suit '{recipe.Id}', but no named path is gated on an element his spell carries.");
			}
			if (suits.Count > 0)
				table.Add($"{character.Id}={string.Join("+", suits.Select(r => r.Id))}");
		}
		GD.Print($"ContentValidator: ladder Suits on a save with every spell - {string.Join(", ", table)}");
	}

	// The mark on the level 4 page against the seal itself: a level 4 path the mark says seals a recipe must leave
	// ComboCatalog.Evaluate calling the pair TooManyElements, and one it does not mark must not.
	private static void ValidateLadderSealMark(List<string> warnings)
	{
		int marks = 0;
		foreach (ComboDefinition recipe in ComboCatalog.Fusions)
		{
			foreach (ComboIngredient part in recipe.Ingredients)
			{
				ComboIngredient other = ComboCatalog.PartnerOf(recipe, part.SpellId);
				SpellData spellTemplate = ContentValidator.LoadAllSpells().FirstOrDefault(s => s != null && s.Id == part.SpellId);
				SpellData partnerTemplate = ContentValidator.LoadAllSpells().FirstOrDefault(s => s != null && s.Id == other.SpellId);
				if (spellTemplate == null || partnerTemplate == null)
					continue;
				var probe = (SpellData)spellTemplate.Duplicate(true);
				SpellEvolutionCatalog.EnsureEvolutionCoverage(probe);
				foreach (SpellEvolutionOption evo in probe.GetEvolutionOptionsForLevel(4) ?? Array.Empty<SpellEvolutionOption>())
				{
					if (evo == null)
						continue;
					bool marked = ConjunctionLadder.WouldSealByElement(probe, evo, partnerTemplate);
					var taken = (SpellData)spellTemplate.Duplicate(true);
					SpellEvolutionCatalog.EnsureEvolutionCoverage(taken);
					taken.CurrentLevel = 4;
					taken.ApplyEvolution(evo);
					var partner = (SpellData)partnerTemplate.Duplicate(true);
					ComboStanding standing = ComboCatalog.Evaluate(recipe, new[] { taken, partner }, null);
					bool sealedByIt = standing.Seal == ComboSeal.TooManyElements;
					if (marked != sealedByIt)
						warnings.Add($"Ladder I: {part.SpellName}'s level 4 '{evo.DisplayName}' is {(marked ? "" : "not ")}marked as sealing '{recipe.Id}', but the recipe {(sealedByIt ? "is" : "is not")} sealed by a fourth element when it is taken.");
					if (marked)
						marks++;
				}
			}
		}
		GD.Print($"ContentValidator: ladder level 4 seal marks - {marks} paths mark a recipe they would seal.");
	}

	private static void ValidateKindredDraw(List<string> warnings)
	{
		if (ConjunctionLadder.KindredDrawShown(LadderSave(Teaching.Apotheosis)))
			warnings.Add("Ladder II: Kindred Draw is on the shelf before the Conjunctions lesson is learned.");
		if (!ConjunctionLadder.KindredDrawShown(LadderSave(Teaching.Conjunctions)))
			warnings.Add("Ladder II: Kindred Draw is not on the shelf once Conjunctions are learned.");
		int[] prices = Enumerable.Range(0, ConjunctionLadder.KindredDrawLevels)
			.Select(l => ConjunctionLadder.KindredDrawBaseCost + l * ConjunctionLadder.KindredDrawCostPerLevel).ToArray();
		if (!prices.SequenceEqual(new[] { 100, 160, 220 }))
			warnings.Add($"Ladder II: Kindred Draw costs {string.Join(" / ", prices)}; the doc's decision is 100 / 160 / 220.");
		for (int level = 0; level <= ConjunctionLadder.KindredDrawLevels; level++)
			if (Math.Abs(ConjunctionLadder.KindredMultiplier(level) - (1 + level)) > 0.001f)
				warnings.Add($"Ladder II: Kindred Draw at level {level} leans x{ConjunctionLadder.KindredMultiplier(level)}, not x{1 + level}.");

		ComboDefinition recipe = ComboCatalog.Fusions.FirstOrDefault();
		if (recipe == null)
			return;
		string a = recipe.First.SpellId, b = recipe.Second.SpellId;
		string stranger = UnlockCatalog.AllSpellIds.FirstOrDefault(id => id != a && id != b && !ComboCatalog.IsCombination(id)) ?? "x";
		LevelUpOption Card(string id) => new() { SpellId = id };

		// One parent held: the partner's new-spell card, and nothing else.
		var news = new List<LevelUpOption> { Card(b), Card(stranger) };
		var newWeights = new List<float> { 1f, 1f };
		var ups = new List<LevelUpOption> { Card(a) };
		var upWeights = new List<float> { 1f };
		ConjunctionLadder.ApplyKindredDraw(3, new[] { recipe }, id => id == a, news, newWeights, ups, upWeights);
		if (Math.Abs(newWeights[0] - 4f) > 0.001f || Math.Abs(newWeights[1] - 1f) > 0.001f || Math.Abs(upWeights[0] - 1f) > 0.001f)
			warnings.Add($"Ladder II: holding {a}, Kindred Draw III leaned [{string.Join(", ", newWeights)}] / [{string.Join(", ", upWeights)}]; expected [4, 1] / [1].");
		// Both held: their upgrades, and no new spell.
		news = new List<LevelUpOption> { Card(stranger) };
		newWeights = new List<float> { 1f };
		ups = new List<LevelUpOption> { Card(a), Card(b), Card(stranger) };
		upWeights = new List<float> { 1f, 1f, 1f };
		ConjunctionLadder.ApplyKindredDraw(1, new[] { recipe }, id => id == a || id == b, news, newWeights, ups, upWeights);
		if (Math.Abs(upWeights[0] - 2f) > 0.001f || Math.Abs(upWeights[1] - 2f) > 0.001f || Math.Abs(upWeights[2] - 1f) > 0.001f || Math.Abs(newWeights[0] - 1f) > 0.001f)
			warnings.Add($"Ladder II: holding both, Kindred Draw I leaned upgrades [{string.Join(", ", upWeights)}]; expected [2, 2, 1].");
		// Level 0 is nothing at all.
		news = new List<LevelUpOption> { Card(b) };
		newWeights = new List<float> { 1f };
		ConjunctionLadder.ApplyKindredDraw(0, new[] { recipe }, id => id == a, news, newWeights, new List<LevelUpOption>(), new List<float>());
		if (Math.Abs(newWeights[0] - 1f) > 0.001f)
			warnings.Add("Ladder II: Kindred Draw at level 0 leaned a card.");

		// Never a card the save does not own: a pool built from a save that owns the held half and not its partner
		// stays exactly that pool - same cards, same count.
		SaveData save = LadderSave(Teaching.Conjunctions, a, stranger);
		var pool = UnlockCatalog.AllSpellIds.Where(id => id != a && GlobalStatsManager.IsSpellUnlockedForLevelUp(save, id)).Select(Card).ToList();
		var poolWeights = pool.Select(_ => 1f).ToList();
		var ids = pool.Select(o => o.SpellId).ToList();
		ConjunctionLadder.ApplyKindredDraw(3, new[] { recipe }, id => id == a, pool, poolWeights, new List<LevelUpOption>(), new List<float>());
		if (!pool.Select(o => o.SpellId).SequenceEqual(ids))
			warnings.Add("Ladder II: Kindred Draw changed which cards are in the pool.");
		if (pool.Any(o => !GlobalStatsManager.IsSpellUnlockedForLevelUp(save, o.SpellId)))
			warnings.Add("Ladder II: Kindred Draw dealt a card the save does not own.");
		if (poolWeights.Any(w => Math.Abs(w - 1f) > 0.001f) && !pool.Any(o => o.SpellId == b))
			warnings.Add($"Ladder II: Kindred Draw leaned a card although {b} is not in the pool.");
	}

	// Schema 26 (SaveData.MigrateToPinnedRecipe): a save from before pins nothing, and the step changes nothing else.
	private static void ValidatePinnedRecipeMigration(List<string> warnings)
	{
		var old = new SaveData { SchemaVersion = 25 };
		GlobalStatsManager.UnlockSpell(old, "hollow_star");
		old.PinnedRecipeId = "stray";
		int spells = old.UnlockedSpellIds.Count;
		old.Migrate(log: false);
		if (!string.IsNullOrEmpty(old.PinnedRecipeId))
			warnings.Add("Pinned recipe migration: a schema-25 save came out of schema 26 with a pin.");
		if (old.SchemaVersion != SaveData.CurrentSchemaVersion || old.UnlockedSpellIds.Count != spells)
			warnings.Add("Pinned recipe migration: schema 26 changed more than the pin.");
		var current = new SaveData { PinnedRecipeId = ComboCatalog.Fusions.FirstOrDefault()?.Id ?? string.Empty };
		current.Migrate(log: false);
		if (current.PinnedRecipeId != (ComboCatalog.Fusions.FirstOrDefault()?.Id ?? string.Empty))
			warnings.Add("Pinned recipe migration: a current save lost its pin to a migration.");
	}
}
