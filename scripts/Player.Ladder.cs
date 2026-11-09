using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE CONJUNCTION LADDER IN A RUN (.ai/conjunction-progression.md; ConjunctionLadder is the one authority on the rungs).
//
// Rung I, know it: the pinned recipe's checklist (BuildRecipeChecklist, read by the pause screen and the quest corner)
// and the level 4 page's mark on a path that would make the pair name a fourth element and seal it (NoteRecipeSeals).
// Rung II, Kindred Draw: the deal's lean toward a held parent's partner, and toward both parents' upgrades once both
// are held (ApplyKindredDrawLean), re-weighing only cards the deal already holds - which card, never whether.
//
// Neither changes ComboCatalog.Evaluate, the Ascension's gates, XP, rerolls, bans or a tag: the recipe is Eric's, both
// parents at 8 down both named paths.
public partial class Player
{
	// Kindred Draw's level for this run, read once with the shop (ApplyArcaneUpgrades); a tool's --kindred wins.
	private int kindredDrawLevel;

	/// <summary>Kindred Draw's level this run (0 when not bought, or Conjunctions not yet learned).</summary>
	public int KindredDrawLevel => ConjunctionsLearned ? kindredDrawLevel : 0;

	/// <summary>Cards Kindred Draw re-weighed this run, across every hand dealt: for the harness.</summary>
	public int KindredLeanedCards { get; private set; }

	private void ReadKindredDraw(int bought) => kindredDrawLevel = Math.Clamp(ConjunctionLadder.KindredToolLevel ?? bought, 0, ConjunctionLadder.KindredDrawLevels);

	private SaveData LadderSave => GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;

	/// <summary>
	/// The recipes Kindred Draw leans toward this run: every fusion the save knows (it owns both parents - a parent it
	/// does not own is never in the deal to lean toward), not the one this wizard opens on, not one naming his own
	/// spell, and still makeable - not made, not sealed by a parent's other path or a fourth element, no Rite held.
	/// </summary>
	private IEnumerable<ComboDefinition> KindredRecipes()
	{
		if (KindredDrawLevel <= 0 || SovereignSpell != null)
			yield break;
		SaveData save = LadderSave;
		foreach (ComboDefinition recipe in ComboCatalog.Fusions)
		{
			if (!ComboCatalog.IsKnown(recipe, save) || ComboCatalog.StartsFused(recipe, selectedCharacter?.Id))
				continue;
			if (recipe.Ingredients.Any(i => WasFusedAway(i.SpellId)))
				continue;
			ComboStanding standing = EvaluateCombo(recipe);
			if (standing.Taken || standing.Sealed)
				continue;
			yield return recipe;
		}
	}

	// GetLevelUpOptions' hook, after the Betrothal Hoop's: the lean, inside each group.
	private void ApplyKindredDrawLean(List<LevelUpOption> newSpells, List<float> newWeights,
		List<LevelUpOption> upgrades, List<float> upgradeWeights)
	{
		if (KindredDrawLevel <= 0)
			return;
		KindredLeanedCards += ConjunctionLadder.ApplyKindredDraw(KindredDrawLevel, KindredRecipes().ToList(),
			id => equippedSpells.Any(s => s != null && s.Id.Equals(id, StringComparison.OrdinalIgnoreCase)),
			newSpells, newWeights, upgrades, upgradeWeights);
	}

	/// <summary>
	/// GetLevelUpOptions' hook on a level 4 page: every path that would make a shown recipe's pair name a fourth element
	/// carries "Seals Sagittarius" (option.RecipeSeals). Only once the Conjunctions lesson is learned, never for a recipe
	/// he cannot make anyway (his own spell, the one he opens on), and never for one already sealed or made.
	/// </summary>
	private void NoteRecipeSeals(LevelUpOption option, SpellData spell)
	{
		if (option?.EvolutionChoices == null || option.EvolutionChoices.Count == 0 || spell == null || !ConjunctionsLearned)
			return;
		SaveData save = LadderSave;
		foreach (ComboDefinition recipe in ComboCatalog.WithParent(spell.Id))
		{
			if (recipe.IsRite || !ConjunctionLadder.IsShown(recipe, save) || ComboCatalog.StartsFused(recipe, selectedCharacter?.Id))
				continue;
			ComboStanding standing = EvaluateCombo(recipe);
			if (standing.Taken || standing.Sealed)
				continue;
			ComboIngredient partner = ComboCatalog.PartnerOf(recipe, spell.Id);
			SpellData partnerSpell = equippedSpells.FirstOrDefault(s => s != null && s.Id.Equals(partner?.SpellId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
				?? (partner != null && spellCatalog.TryGetValue(partner.SpellId, out SpellData template) ? template : null);
			// Named once made; until then by the partner, as the level 8 mark names it ("Joins Starfall").
			string what = ComboCatalog.IsMet(recipe, save) ? recipe.Name : $"the Conjunction with {partner?.SpellName}";
			foreach (SpellEvolutionOption evo in option.EvolutionChoices)
			{
				if (evo == null || !ConjunctionLadder.WouldSealByElement(spell, evo, partnerSpell))
					continue;
				option.RecipeSeals[evo.Id] = option.RecipeSeals.TryGetValue(evo.Id, out string already)
					? $"{already} and {what}" : what;
			}
		}
	}

	/// <summary>
	/// Where a recipe stands this run, for a person (rung I, idea 2): each parent in the book and its level, each named
	/// path's element count, the player's level against the Ascension's, and whether it is sealed - in the order a
	/// planner meets them - and the one next step. Reads ComboCatalog.Evaluate and the gates' own authorities
	/// (EvaluateElementGate, CardLevelCap, AscensionMinimumPlayerLevel); decides nothing.
	/// </summary>
	public RecipeChecklist BuildRecipeChecklist(ComboDefinition recipe, SaveData save)
	{
		var list = new RecipeChecklist { Recipe = recipe };
		if (recipe == null || recipe.IsRite)
			return list;
		ComboStanding standing = EvaluateCombo(recipe);
		if (standing.Taken)
		{
			list.Made = true;
			list.NextStep = "Made";
			list.Lines.Add(new RecipeCheck { Glyph = "done", Met = true,
				Text = ComboCatalog.StartsFused(recipe, selectedCharacter?.Id) ? "You opened the run on it." : "Made this run." });
			return list;
		}
		if (standing.Seal != ComboSeal.None || SovereignSpell != null)
		{
			list.Sealed = true;
			list.NextStep = "Sealed this run";
			list.Lines.Add(new RecipeCheck { Glyph = "sealed", Closed = true, Text = SealedWords(recipe, standing) });
			return list;
		}
		list.Ready = standing.Ready;

		Dictionary<Element, int> counts = GetElementInstanceCounts();
		string next = null;
		bool anyPathToTake = false;
		foreach ((ComboIngredient part, SpellData held, bool met) in new[]
		{
			(recipe.First, standing.FirstHeld, standing.FirstMet),
			(recipe.Second, standing.SecondHeld, standing.SecondMet),
		})
		{
			if (met)
			{
				list.Lines.Add(new RecipeCheck { Glyph = "done", Met = true, Text = $"{part.SpellName}: {part.AscensionName} taken" });
				continue;
			}
			anyPathToTake = true;
			int cap = held != null ? CardLevelCap(held) : AscensionSpellLevel - 1;
			if (held != null)
			{
				bool levelled = held.CurrentLevel >= cap;
				list.Lines.Add(new RecipeCheck { Glyph = levelled ? "done" : "todo", Met = levelled,
					Text = $"{part.SpellName}: level {held.CurrentLevel} of {cap}" });
				if (!levelled)
					next ??= $"{part.SpellName} to level {cap}  ·  {held.CurrentLevel}/{cap}";
			}
			else if (!ConjunctionLadder.Owns(save, part))
			{
				list.Lines.Add(new RecipeCheck { Glyph = "lock", Text = ConjunctionLadder.MissingHalfLine(part) });
				next ??= $"Find {part.SpellName} first";
			}
			else
			{
				list.Lines.Add(new RecipeCheck { Glyph = "todo",
					Text = IsSpellBookFull ? $"{part.SpellName}: no page free for it" : $"{part.SpellName}: not in your book yet" });
				next ??= IsSpellBookFull ? $"No page free for {part.SpellName}" : $"Take {part.SpellName} when it is dealt";
			}

			// The named path's own gate, measured by the one authority, against what he holds now.
			SpellData gateSpell = held ?? (spellCatalog.TryGetValue(part.SpellId, out SpellData template) ? template : null);
			SpellEvolutionOption named = NamedPathOf(part, held, gateSpell);
			AscensionRequirement gate = named == null ? null : EvaluateElementGate(named, gateSpell, counts);
			if (gate != null && gate.Element is Element element)
			{
				int have = Math.Min(gate.Have, gate.Need);
				list.Lines.Add(new RecipeCheck { Glyph = gate.Met ? "done" : "todo", Met = gate.Met,
					Text = $"{part.AscensionName}: {ElementWords(gate)} {have} of {gate.Need}" });
				// The corner names an element short only once the levels are in hand (FirstElementShort, below).
			}
		}

		if (anyPathToTake)
		{
			bool levelMet = CurrentLevel >= AscensionMinimumPlayerLevel;
			list.Lines.Add(new RecipeCheck { Glyph = levelMet ? "done" : "xp", Met = levelMet,
				Text = $"Your level: {Math.Min(CurrentLevel, AscensionMinimumPlayerLevel)} of {AscensionMinimumPlayerLevel}" });
			if (!levelMet)
				next ??= $"Reach level {AscensionMinimumPlayerLevel}  ·  {CurrentLevel}/{AscensionMinimumPlayerLevel}";
		}

		// The level 4 path that would seal it, while a parent has not taken its level 4 yet.
		foreach ((ComboIngredient part, SpellData held) in new[] { (recipe.First, standing.FirstHeld), (recipe.Second, standing.SecondHeld) })
		{
			if (held == null || held.CurrentLevel >= 4)
				continue;
			ComboIngredient other = ComboCatalog.PartnerOf(recipe, part.SpellId);
			SpellData partner = equippedSpells.FirstOrDefault(s => s != null && s.Id.Equals(other.SpellId, StringComparison.OrdinalIgnoreCase))
				?? (spellCatalog.TryGetValue(other.SpellId, out SpellData t) ? t : null);
			foreach (SpellEvolutionOption evo in held.GetEvolutionOptionsForLevel(4) ?? Enumerable.Empty<SpellEvolutionOption>())
			{
				if (evo != null && ConjunctionLadder.WouldSealByElement(held, evo, partner))
					list.Lines.Add(new RecipeCheck { Glyph = "lock", Text = $"{part.SpellName}'s level 4: {evo.DisplayName} would seal it" });
			}
		}

		if (list.Ready)
			next = "Ready above the cards";
		// The element gates are the last brake: once the levels are in hand, the first one short.
		next ??= FirstElementShort(list);
		next ??= anyPathToTake ? "Take the named paths at level 8" : "Ready above the cards";
		list.NextStep = next;
		return list;
	}

	// "Constellation: Light 2 of 4" -> "Light 2/4 for Constellation", the corner's shape.
	private static string FirstElementShort(RecipeChecklist list)
	{
		foreach (RecipeCheck check in list.Lines)
		{
			if (check.Met || check.Closed || !check.Text.Contains(" of ") || check.Text.StartsWith("Your level"))
				continue;
			int colon = check.Text.IndexOf(": ", StringComparison.Ordinal);
			if (colon <= 0)
				continue;
			string path = check.Text.Substring(0, colon);
			string rest = check.Text.Substring(colon + 2);
			// Only the element lines: "Light 2 of 4" (a parent's level line reads "level 3 of 7").
			if (rest.StartsWith("level "))
				continue;
			return $"{rest.Replace(" of ", "/")} for {path}";
		}
		return null;
	}

	// "Light", or "Metal or Wind" for a path any of two may open.
	private static string ElementWords(AscensionRequirement gate) =>
		gate.AnyOf.Count > 1 ? string.Join(" or ", gate.AnyOf) : gate.Element?.ToString() ?? string.Empty;

	// The named Ascension as this run would offer it: off the held spell (its form's own), else off the catalog's.
	private static SpellEvolutionOption NamedPathOf(ComboIngredient part, SpellData held, SpellData template)
	{
		IEnumerable<SpellEvolutionOption> options = held != null
			? held.GetEvolutionOptionsForLevel(AscensionSpellLevel) ?? Enumerable.Empty<SpellEvolutionOption>()
			: ComboCatalog.Level8OptionsOf(template, part);
		return options.FirstOrDefault(o => o != null && o.Id.Equals(part.AscensionId, StringComparison.OrdinalIgnoreCase));
	}

	private string SealedWords(ComboDefinition recipe, ComboStanding standing)
	{
		if (standing.FirstOwn || standing.SecondOwn)
			return $"It names your own {(standing.FirstOwn ? recipe.First.SpellName : recipe.Second.SpellName)}: its last form is your Apotheosis.";
		if (standing.Seal == ComboSeal.OtherAscension)
		{
			SpellData closed = standing.FirstClosed ? standing.FirstHeld : standing.SecondHeld;
			return $"Sealed this run: {closed?.Name ?? "a parent"} took its other path.";
		}
		if (standing.Seal == ComboSeal.TooManyElements)
			return string.IsNullOrEmpty(standing.ExtraElementBy)
				? "Sealed this run: the two would name four elements."
				: $"Sealed this run: {standing.ExtraElementBy} made the two name four elements.";
		return "Sealed this run: the Sovereign Rite holds your spells.";
	}
}
