using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>One line of a pinned recipe's checklist (the pause screen's): a stat glyph, the words, and where it stands.</summary>
public sealed class RecipeCheck
{
	/// <summary>The stat glyph before the line (tools/art/stat_icons.py): done, todo, lock, sealed, xp, conjunction.</summary>
	public string Glyph = "todo";
	public string Text = string.Empty;
	public bool Met;
	/// <summary>Settled against him for this run: a parent down its other path, a fourth element.</summary>
	public bool Closed;
}

/// <summary>Where one recipe stands for this run, read for a person: the pause screen's checklist and the corner's one line.</summary>
public sealed class RecipeChecklist
{
	public ComboDefinition Recipe;
	public readonly List<RecipeCheck> Lines = new();
	/// <summary>The one next thing to do, without a full stop: the quest tracker's line.</summary>
	public string NextStep = string.Empty;
	public bool Made;
	public bool Sealed;
	public bool Ready;
}

/// <summary>
/// THE CONJUNCTION LADDER (.ai/conjunction-progression.md; Eric, 2026-11-08: "Build the ladder"). The one authority on
/// what each rung shows and allows; ComboCatalog stays the one authority on the recipes themselves.
/// </summary>
/// <remarks>
/// <para>Four rungs, each earned by a different kind of progress and each appearing only once earned (one locked thing
/// on show at a time, .ai/slow-start.md): <b>I, know it</b> (the Conjunctions lesson, chapter 4) - a recipe shows once
/// the save owns either parent, one may be pinned, and the level 4 path that would seal it is marked; <b>II, Kindred
/// Draw</b> (a shop Power, shown from the lesson on) - a held parent's partner is dealt more often; <b>III, the pair
/// shares its light</b> (the Archivist's deed, chapter 6); <b>IV, Nostradamus foretells</b> (the Court, chapter 12).</para>
///
/// <para><b>What no rung touches</b> (the doc's own promise, Eric's rule): the recipe - both parents at level 8, each
/// down the one Ascension it names - and XP, rerolls, bans or element tags. Kindred Draw changes WHICH card is dealt,
/// never WHETHER (the Betrothal Hoop's rule), and only among cards the deal already holds.</para>
/// </remarks>
public static class ConjunctionLadder
{
	// --- rung I: know it ------------------------------------------------------------------------------------

	/// <summary>
	/// Whether a recipe may be shown to this save (the browser, the Codex's Recipes, the Achievements row): today's
	/// rule - the save owns both parents (<see cref="ComboCatalog.IsKnown"/>, shown from the Apotheosis learned on) - or,
	/// once the Conjunctions lesson is learned, either parent (rung I, idea 1). The missing half reads by its locked
	/// hint (<see cref="MissingHalfLine"/>). Never the Rite, which has its own rule. A null save shows nothing extra.
	/// </summary>
	public static bool IsShown(ComboDefinition recipe, SaveData save)
	{
		if (recipe == null || recipe.IsRite || save == null)
			return false;
		if (ComboCatalog.IsKnown(recipe, save))
			return true;
		return Teachings.HasLearned(save, Teaching.Conjunctions) && recipe.Ingredients.Any(i => Owns(save, i));
	}

	/// <summary>Whether the save owns this parent: it can be dealt it.</summary>
	public static bool Owns(SaveData save, ComboIngredient ingredient) =>
		save != null && ingredient != null && !ingredient.AnyActiveSpell
		&& GlobalStatsManager.IsSpellUnlockedForLevelUp(save, ingredient.SpellId);

	/// <summary>
	/// What the ladder calls a recipe on this save: its name once made (its Feat done), else its two parents - "Hunter's
	/// Draw + Starfall" - because the result stays sealed until made (.ai/combo-spells.md 2, "Unlocks and the save"; the
	/// browser's and the Codex's rule). Only the far sky names an unmade recipe, and that is its own decision (F4).
	/// </summary>
	public static string ShownName(ComboDefinition recipe, SaveData save) =>
		recipe == null ? string.Empty
			: ComboCatalog.IsMet(recipe, save) ? recipe.Name
			: $"{recipe.First.SpellName} + {recipe.Second.SpellName}";

	/// <summary>"Hunter's Draw (Hunter's Mark) and Starfall (Constellation)", led by the name once made.</summary>
	public static string Describe(ComboDefinition recipe, SaveData save) =>
		recipe == null ? string.Empty
			: ComboCatalog.IsMet(recipe, save) ? FarSky.Describe(recipe)
			: $"{recipe.First.SpellName} ({recipe.First.AscensionName}) and {recipe.Second.SpellName} ({recipe.Second.AscensionName})";

	/// <summary>The parent the save does not own yet, by the hint its locked page already wears: "Starfall: hidden ...".</summary>
	public static string MissingHalfLine(ComboIngredient ingredient) =>
		ingredient == null ? string.Empty : $"{ingredient.SpellName}: {UnlockCatalog.GetLockedHint(ingredient.SpellId, UnlockKind.Spell)}";

	/// <summary>
	/// The recipe the save has pinned, or null: nothing before the Conjunctions lesson (the pin is rung I's), nothing
	/// the save may not be shown, never the Rite. Read live, so a pin made in the Codex shows on the next pause.
	/// </summary>
	public static ComboDefinition Pinned(SaveData save)
	{
		if (save == null || string.IsNullOrWhiteSpace(save.PinnedRecipeId) || !Teachings.HasLearned(save, Teaching.Conjunctions))
			return null;
		ComboDefinition recipe = ComboCatalog.ForId(save.PinnedRecipeId);
		return IsShown(recipe, save) ? recipe : null;
	}

	/// <summary>Whether this save may pin this recipe now, and if not, why.</summary>
	public static bool MayPin(ComboDefinition recipe, SaveData save, out string why)
	{
		why = string.Empty;
		if (recipe == null || recipe.IsRite)
			why = "not a recipe of two spells";
		else if (save == null || !Teachings.HasLearned(save, Teaching.Conjunctions))
			why = "Conjunctions are not learned yet";
		else if (!IsShown(recipe, save))
			why = "the save knows neither half of it";
		return why.Length == 0;
	}

	/// <summary>Pins the recipe (one at a time: a new pin replaces the old). True when it changed. The caller saves.</summary>
	public static bool Pin(SaveData save, string recipeId)
	{
		ComboDefinition recipe = ComboCatalog.ForId(recipeId);
		if (!MayPin(recipe, save, out _) || recipe.Id.Equals(save.PinnedRecipeId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			return false;
		save.PinnedRecipeId = recipe.Id;
		return true;
	}

	/// <summary>Takes the pin off. True when there was one. The caller saves.</summary>
	public static bool Unpin(SaveData save)
	{
		if (save == null || string.IsNullOrEmpty(save.PinnedRecipeId))
			return false;
		save.PinnedRecipeId = string.Empty;
		return true;
	}

	/// <summary>
	/// The elements a recipe's named path is gated on (Player.EvaluateElementGate's AnyOf, read off the parent's
	/// resource): the path's own element if it carries one, else the parent's own tags. Empty for a path with no gate.
	/// </summary>
	public static IReadOnlyCollection<Element> NamedPathGateElements(ComboIngredient ingredient)
	{
		if (ingredient == null || ingredient.AnyActiveSpell)
			return Array.Empty<Element>();
		SpellData template = ContentValidator.LoadAllSpells().FirstOrDefault(s => s != null && s.Id.Equals(ingredient.SpellId, StringComparison.OrdinalIgnoreCase));
		SpellEvolutionOption named = ComboCatalog.Level8OptionsOf(template, ingredient)
			.FirstOrDefault(o => o != null && o.Id.Equals(ingredient.AscensionId, StringComparison.OrdinalIgnoreCase));
		AscensionRequirement gate = named == null ? null : Player.EvaluateElementGate(named, template, new Dictionary<Element, int>());
		return gate?.AnyOf ?? (IReadOnlyCollection<Element>)Array.Empty<Element>();
	}

	/// <summary>
	/// The known recipes a wizard SUITS (idea 4, the character select's line): a named path is gated on an element his
	/// own spell carries, and neither parent is a spell of his own (his starting spell, or one that is his alone).
	/// Derived, never authored, and nothing before the Conjunctions lesson. Never the one he opens on.
	/// </summary>
	public static List<ComboDefinition> Suits(CharacterData character, SaveData save)
	{
		var suits = new List<ComboDefinition>();
		SpellData own = character?.StartingSpellResource;
		if (own == null || save == null || !Teachings.HasLearned(save, Teaching.Conjunctions)
			|| character.Id.Equals("test_wizard", StringComparison.OrdinalIgnoreCase))
			return suits;
		var ownElements = new HashSet<Element>();
		foreach (var pair in own.ElementWeights)
			if (pair.Value > 0 && Enum.TryParse(pair.Key, true, out Element element))
				ownElements.Add(element);
		if (ownElements.Count == 0)
			return suits;
		foreach (ComboDefinition recipe in ComboCatalog.Fusions)
		{
			if (!IsShown(recipe, save) || ComboCatalog.StartsFused(recipe, character.Id))
				continue;
			if (recipe.Ingredients.Any(i => IsHisOwn(i.SpellId, character)))
				continue;
			if (recipe.Ingredients.Any(i => NamedPathGateElements(i).Any(ownElements.Contains)))
				suits.Add(recipe);
		}
		return suits;
	}

	// His starting spell, or a spell that is his alone (UnlockDefinition.ExclusiveTo: Merlin's Orb of Ages).
	private static bool IsHisOwn(string spellId, CharacterData character)
	{
		if (string.IsNullOrEmpty(spellId) || character == null)
			return false;
		if (spellId.Equals(character.StartingSpellResource?.Id ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			return true;
		string alone = UnlockCatalog.GetSpell(spellId)?.ExclusiveTo;
		return !string.IsNullOrEmpty(alone) && alone.Equals(character.Id, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Whether taking <paramref name="evo"/> (a level 4 path) on <paramref name="spell"/> would make the recipe name
	/// more than <see cref="ComboCatalog.MaxElements"/> elements, and so seal it for the run - the mark rung I puts on
	/// the level 4 page. Measured against the partner as it is held, else as its resource declares it. An untagged path,
	/// or one whose element the pair already names, seals nothing.
	/// </summary>
	public static bool WouldSealByElement(SpellData spell, SpellEvolutionOption evo, SpellData partner)
	{
		if (spell == null || evo?.BonusElementWeights == null || evo.BonusElementWeights.Count == 0)
			return false;
		var elements = new HashSet<Element>(spell.GetElementWeights().Keys);
		bool adds = false;
		foreach (var pair in evo.BonusElementWeights)
			if (pair.Value > 0 && Enum.TryParse(pair.Key, true, out Element element) && elements.Add(element))
				adds = true;
		if (!adds)
			return false;
		if (partner != null)
			foreach (Element element in partner.GetElementWeights().Keys)
				elements.Add(element);
		return elements.Count > ComboCatalog.MaxElements;
	}

	// --- rung II: Kindred Draw -------------------------------------------------------------------------------

	/// <summary>The shop Power's id (MainMenu.BuildUpgradeDefinitions; SaveData.ArcaneUpgradeLevels).</summary>
	public const string KindredDrawId = "kindred_draw";
	public const string KindredDrawName = "Kindred Draw";
	public const int KindredDrawBaseCost = 100;
	public const int KindredDrawCostPerLevel = 60;
	public const int KindredDrawLevels = 3;

	/// <summary>
	/// Tools only (_BalanceHarness <c>--kindred=N</c>): the level a run plays Kindred Draw at, whatever the save bought.
	/// Null plays by the save.
	/// </summary>
	public static int? KindredToolLevel { get; set; }

	/// <summary>The shop row is shown once the Conjunctions lesson is learned, and not before (one locked thing at a time).</summary>
	public static bool KindredDrawShown(SaveData save) => save != null && Teachings.HasLearned(save, Teaching.Conjunctions);

	/// <summary>How much likelier a leaned card is within its own group: x2, x3, x4 at levels I, II, III.</summary>
	public static float KindredMultiplier(int level) => 1f + Math.Clamp(level, 0, KindredDrawLevels);

	/// <summary>
	/// Kindred Draw's lean, on the deal's two pools as GetLevelUpOptions has built them (bans already applied): for every
	/// recipe in <paramref name="recipes"/>, one parent held and the other not, the other's new-spell card is
	/// <see cref="KindredMultiplier"/> times as heavy among the new spells; both held, their upgrades are that much
	/// heavier among the upgrades. It only re-weighs cards already in a pool - it adds none, removes none, and touches
	/// no card of a spell outside the recipes - so it can never deal a card the save could not be dealt, and the group
	/// shares (NormalizeOfferWeightsByGroup) keep it "which card, never whether". A card leaned by two recipes is leaned
	/// once. Returns how many cards it leaned.
	/// </summary>
	public static int ApplyKindredDraw(int level, IEnumerable<ComboDefinition> recipes, Func<string, bool> isHeld,
		List<LevelUpOption> newSpells, List<float> newWeights, List<LevelUpOption> upgrades, List<float> upgradeWeights)
	{
		if (level <= 0 || recipes == null || isHeld == null)
			return 0;
		var leanNew = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var leanUpgrade = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (ComboDefinition recipe in recipes)
		{
			if (recipe == null || recipe.IsRite)
				continue;
			bool first = isHeld(recipe.First.SpellId);
			bool second = isHeld(recipe.Second.SpellId);
			if (first && second)
			{
				leanUpgrade.Add(recipe.First.SpellId);
				leanUpgrade.Add(recipe.Second.SpellId);
			}
			else if (first)
				leanNew.Add(recipe.Second.SpellId);
			else if (second)
				leanNew.Add(recipe.First.SpellId);
		}
		float multiplier = KindredMultiplier(level);
		int leaned = 0;
		leaned += Lean(newSpells, newWeights, leanNew, multiplier);
		leaned += Lean(upgrades, upgradeWeights, leanUpgrade, multiplier);
		return leaned;
	}

	private static int Lean(List<LevelUpOption> pool, List<float> weights, HashSet<string> spellIds, float multiplier)
	{
		if (pool == null || weights == null || spellIds.Count == 0)
			return 0;
		int leaned = 0;
		for (int i = 0; i < pool.Count && i < weights.Count; i++)
		{
			if (pool[i] == null || !spellIds.Contains(pool[i].SpellId ?? string.Empty))
				continue;
			weights[i] *= multiplier;
			leaned++;
		}
		return leaned;
	}
}
