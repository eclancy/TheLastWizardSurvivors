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
		ValidatePairShares(warnings);
		ValidateForetell(warnings);
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

	// RUNG III, the pair shares its light (ConjunctionLadder.PairShareFor; Teachings.PairSharesLesson):
	//   - it never applies off its deed (not earned: nothing), to a third Ascension (the parent's other level 8 path),
	//     with the partner not held, to a sealed recipe (the partner down its other path; the wizard's own spell);
	//   - earned, both parents held at 7, it shares on every built recipe's named paths, and at half never more than 1;
	//   - with the far sky's waiver it never stacks: the waiver is set only where the share leaves the gate unmet;
	//   - its lesson: owed by the Archivist's deed and only once Conjunctions are learned, not shown before it is learned.
	private static void ValidatePairShares(List<string> warnings)
	{
		int before = warnings.Count;
		var table = new List<string>();
		foreach (ComboDefinition recipe in ComboCatalog.Fusions)
		{
			SpellData first = LadderParent(recipe.First, 7);
			SpellData second = LadderParent(recipe.Second, 7);
			if (first == null || second == null)
				continue;
			var both = new List<SpellData> { first, second };
			foreach ((ComboIngredient part, SpellData spell) in new[] { (recipe.First, first), (recipe.Second, second) })
			{
				SpellEvolutionOption named = spell.GetEvolutionOptionsForLevel(8)?.FirstOrDefault(o => o?.Id == part.AscensionId);
				var gateElements = named == null ? new List<Element>()
					: Player.EvaluateElementGate(named, spell, new Dictionary<Element, int>())?.AnyOf ?? new List<Element>();
				int Share(bool earned, IEnumerable<SpellData> held, string ascension, string own = null, bool half = false) =>
					ConjunctionLadder.PairShareFor(earned, part.SpellId, ascension, held, own, gateElements, half).Weight;
				string where = $"{part.SpellName} - {part.AscensionName} ('{recipe.Id}')";
				int full = Share(true, both, part.AscensionId);
				table.Add($"{part.AscensionName}+{full}");
				if (full <= 0)
					warnings.Add($"Ladder III: {where} gets no share from its partner held at 7 - the rung does nothing for it.");
				if (Share(false, both, part.AscensionId) != 0)
					warnings.Add($"Ladder III: {where} is shared on a save that has not earned the rung.");
				if (Share(true, both, part.AscensionId, half: true) > 1)
					warnings.Add($"Ladder III: {where} shares more than 1 at half strength.");
				if (Share(true, new[] { spell }, part.AscensionId) != 0)
					warnings.Add($"Ladder III: {where} is shared with its partner not held.");
				if (Share(true, both, part.AscensionId, own: part.SpellId) != 0)
					warnings.Add($"Ladder III: {where} is shared for a wizard whose own spell it is.");
				foreach (SpellEvolutionOption third in spell.GetEvolutionOptionsForLevel(8) ?? Array.Empty<SpellEvolutionOption>())
				{
					if (third == null || third.Id == part.AscensionId || ComboCatalog.Naming(part.SpellId, third.Id).Any())
						continue;
					if (Share(true, both, third.Id) != 0)
						warnings.Add($"Ladder III: {part.SpellName}'s '{third.DisplayName}', which no recipe names, is shared - a third Ascension.");
				}
				// The partner down its other path seals the recipe, and the share ends with it.
				ComboIngredient otherPart = ComboCatalog.PartnerOf(recipe, part.SpellId);
				SpellData partnerSealed = LadderParent(otherPart, 8, otherPath: true);
				if (partnerSealed != null && Share(true, new[] { spell, partnerSealed }, part.AscensionId) != 0)
					warnings.Add($"Ladder III: {where} is shared after its partner took its other path (the recipe is sealed).");
			}
		}

		// Never stacked with the far sky's waiver: the caller waives only where the share leaves the gate unmet.
		foreach ((int have, int shared) in new[] { (1, 2), (2, 2), (0, 0), (3, 1) })
		{
			var gate = new AscensionRequirement { Kind = AscensionRequirementKind.Element, Have = have, Shared = shared, Need = Player.AscensionElementRequirement };
			bool metByShare = gate.Met;
			gate.Waived = !gate.Met && true;
			if (metByShare && gate.Waived)
				warnings.Add("Ladder III: a gate the share already meets is also waived.");
			if (gate.Missing < 0 || (gate.Met && gate.Missing != 0))
				warnings.Add($"Ladder III: a gate at {have} + {shared} of 4 reads {gate.Missing} missing.");
		}

		// The lesson: owed by the Archivist's deed only once Conjunctions are learned, and nothing shown before.
		SaveData early = LadderSave(Teaching.Apotheosis);
		early.UnlockedAchievementIds.Add(ConjunctionLadder.PairShareDeedId);
		if (Teachings.Owed(early).Contains(Teachings.PairSharesLesson))
			warnings.Add("Ladder III: its lesson is owed before Conjunctions are learned.");
		SaveData ready = LadderSave(Teaching.Conjunctions);
		if (Teachings.Owed(ready).Contains(Teachings.PairSharesLesson))
			warnings.Add("Ladder III: its lesson is owed without the Archivist's deed.");
		ready.UnlockedAchievementIds.Add(ConjunctionLadder.PairShareDeedId);
		if (!Teachings.Owed(ready).Contains(Teachings.PairSharesLesson))
			warnings.Add("Ladder III: its lesson is not owed once the Archivist is beaten and Conjunctions are learned.");
		if (Teachings.IsShown(ready, Teaching.PairShares) || Teachings.HasLearned(ready, Teaching.PairShares))
			warnings.Add("Ladder III: it is shown or learned before its lesson has played.");
		Teachings.Teach(ready, Teachings.PairSharesLesson);
		if (!Teachings.HasLearned(ready, Teaching.PairShares) || Teachings.Owed(ready).Contains(Teachings.PairSharesLesson))
			warnings.Add("Ladder III: its lesson, once taught, is not learned (or is still owed).");
		AchievementDefinition deed = AchievementDefinitions.All.FirstOrDefault(d => d.Id == ConjunctionLadder.PairShareDeedId);
		if (deed == null || deed.BossId != "ruins_sentinel" || !deed.RewardText.Contains("teaches a lesson"))
			warnings.Add($"Ladder III: '{ConjunctionLadder.PairShareDeedId}' should be the Archivist's deed and say it teaches a lesson.");
		if (warnings.Count == before)
			GD.Print($"ContentValidator: ladder III, the pair's share at level 7 - {string.Join(", ", table)}; never off its deed, a third Ascension, an unheld partner or a sealed recipe; never stacked with the far sky's waiver.");
	}

	// RUNG IV, Nostradamus foretells (ConjunctionLadder.Foretold): nothing before Nostradamus is freed or Conjunctions
	// are learned; what may be foretold is exactly what the far sky may name for that wizard (never his opener, never his
	// own spell's recipe, both parents dealable); never on the far shore, in a Boss Rush or off the road; the pin stands
	// in until a choice is made; a press walks every candidate and nothing, and comes round.
	private static void ValidateForetell(List<string> warnings)
	{
		int before = warnings.Count;
		string choiceBefore = ConjunctionLadder.ForetoldChoice, toolBefore = ConjunctionLadder.ForetellToolOverride;
		ConjunctionLadder.ForetellToolOverride = null;
		try
		{
			string[] every = UnlockCatalog.AllSpellIds.ToArray();
			SaveData unfreed = LadderSave(Teaching.Conjunctions, every);
			SaveData early = LadderSave(Teaching.Apotheosis, every);
			early.UnlockedCharacterIds.Add(ConjunctionLadder.ForetellerId);
			SaveData earned = LadderSave(Teaching.Conjunctions, every);
			earned.UnlockedCharacterIds.Add(ConjunctionLadder.ForetellerId);
			StageDefinition road = StageCatalog.Campaign.FirstOrDefault(s => s.IsPlayable);
			StageDefinition farShore = StageCatalog.FarShore.FirstOrDefault();
			int characters = 0, foretellable = 0;
			foreach (CharacterData character in CharacterRoster.GetAll().Where(c => c != null))
			{
				characters++;
				if (ConjunctionLadder.ForetellCandidates(unfreed, character).Count > 0)
					warnings.Add($"Ladder IV: '{character.Id}' may be foretold a recipe before Nostradamus is freed.");
				if (ConjunctionLadder.ForetellCandidates(early, character).Count > 0)
					warnings.Add($"Ladder IV: '{character.Id}' may be foretold a recipe before Conjunctions are learned.");
				List<ComboDefinition> candidates = ConjunctionLadder.ForetellCandidates(earned, character);
				List<ComboDefinition> sky = FarSky.Candidates(earned, character.Id, FarSky.OwnSpellOf(character));
				if (!candidates.SequenceEqual(sky))
					warnings.Add($"Ladder IV: what may be foretold for '{character.Id}' is not what the far sky may name.");
				if (candidates.Count == 0)
					continue;
				foretellable++;
				// The pin stands in until a choice is made, and only if it may be foretold.
				ConjunctionLadder.ForetoldChoice = null;
				earned.PinnedRecipeId = candidates[0].Id;
				if (road != null && ConjunctionLadder.Foretold(earned, character, road, false) != candidates[0])
					warnings.Add($"Ladder IV: the pin '{candidates[0].Id}' does not stand in for an unmade choice for '{character.Id}'.");
				if (farShore != null && ConjunctionLadder.Foretold(earned, character, farShore, false) != null)
					warnings.Add($"Ladder IV: '{character.Id}' is foretold a recipe on the far shore, where the sky's night wins.");
				if (road != null && ConjunctionLadder.Foretold(earned, character, road, true) != null)
					warnings.Add($"Ladder IV: '{character.Id}' is foretold a recipe in a Boss Rush.");
				// A press walks every candidate, then nothing, and comes round.
				if (road != null)
				{
					var seen = new List<string>();
					for (int press = 0; press <= candidates.Count + 1; press++)
					{
						ConjunctionLadder.CycleForetold(earned, character, road);
						seen.Add(ConjunctionLadder.Foretold(earned, character, road, false)?.Id ?? "-");
					}
					if (!candidates.All(c => seen.Contains(c.Id)) || !seen.Contains("-"))
						warnings.Add($"Ladder IV: pressing the foretelling for '{character.Id}' does not walk every recipe and nothing ([{string.Join(",", seen)}]).");
				}
				string own = FarSky.OwnSpellOf(character);
				foreach (ComboDefinition recipe in candidates)
				{
					if (ComboCatalog.StartsFused(recipe, character.Id) || (!string.IsNullOrEmpty(own) && recipe.Ingredients.Any(i => i.SpellId == own)))
						warnings.Add($"Ladder IV: '{character.Id}' may be foretold '{recipe.Id}', which he opens on or which names his own spell.");
				}
			}
			earned.PinnedRecipeId = string.Empty;
			if (warnings.Count == before)
				GD.Print($"ContentValidator: ladder IV, Nostradamus foretells - {foretellable} of {characters} wizards have a recipe to be foretold on a save with every spell; nothing before he is freed or the lesson learned, never on the far shore or a Boss Rush, the far sky's rules for what may be named.");
		}
		finally
		{
			ConjunctionLadder.ForetoldChoice = choiceBefore;
			ConjunctionLadder.ForetellToolOverride = toolBefore;
		}
	}

	// A recipe's parent from its resource at <paramref name="level"/>; at 8 down the named path, or its other one.
	private static SpellData LadderParent(ComboIngredient part, int level, bool otherPath = false)
	{
		SpellData template = ContentValidator.LoadAllSpells().FirstOrDefault(s => s != null && s.Id == part?.SpellId);
		if (template == null)
			return null;
		var spell = (SpellData)template.Duplicate(true);
		SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
		spell.CurrentLevel = Math.Min(level, spell.MaxLevel);
		if (level >= 8)
		{
			SpellEvolutionOption path = (spell.GetEvolutionOptionsForLevel(8) ?? Array.Empty<SpellEvolutionOption>())
				.FirstOrDefault(o => o != null && (otherPath ? o.Id != part.AscensionId : o.Id == part.AscensionId));
			if (path == null)
				return null;
			spell.ApplyEvolution(path);
		}
		return spell;
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
