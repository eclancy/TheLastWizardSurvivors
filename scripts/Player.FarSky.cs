using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE FAR SKY'S FAVOUR (.ai/second-half.md section 5, piece F4; FarSky is the one authority on what is named).
//
// "The deal favours it; the recipe is unchanged." While tonight's Conjunction is named for this run and can still be
// made - not made yet, not sealed by a parent's other Ascension or a fourth element, the Rite not taken - the deal
// leans on its two parents in three places and nowhere else:
//
//   - when a slot is free, a parent not yet held is dealt before any other new spell (TakeFarSkyFavoured);
//   - while held, a parent's upgrade card is always in the hand, as if Player.UpgradeOfferChance were 1 for those two
//     alone and the card were then dealt first (TakeFarSkyFavoured);
//   - at level 8 the named Ascension is the first card on the parent's page, under the linked rings it already
//     carries (FavourNamedAscension).
//
// Both parents at 8 down both named Ascensions is still what the strip asks (Eric's rule, .ai/combo-spells.md 0):
// nothing here changes ComboCatalog.Evaluate, the player level the Ascension waits for, or a ban the player made.
// A parent banned is banned; a parent set aside is never named (FarSky.MayName).
public partial class Player
{
	/// <summary>Tonight's Conjunction for this run, or null (Node2DGame.StartFarSky; FarSky.ForRun).</summary>
	public ComboDefinition FarSkyRecipe { get; private set; }

	/// <summary>True once tonight's Conjunction was made in this run: the Feat and the run's-end bonus read it.</summary>
	public bool FarSkyMade { get; private set; }

	/// <summary>Cards the favour put in a hand this run that the deal alone might not have: for the harness.</summary>
	public int FarSkyFavouredCards { get; private set; }

	/// <summary>Names tonight's Conjunction for this run (null for none). Called once, as the run starts.</summary>
	public void BeginFarSky(ComboDefinition recipe)
	{
		FarSkyRecipe = recipe;
		FarSkyMade = false;
		FarSkyFavouredCards = 0;
		if (recipe != null)
			GD.Print($"FAR SKY: tonight's Conjunction is {FarSky.Describe(recipe)}");
	}

	/// <summary>
	/// Whether the favour still holds: a recipe named, Conjunctions learned, not made, nothing sealing it, and no Rite
	/// holding the loadout. Read live, so the favour ends the moment a parent takes its other Ascension.
	/// </summary>
	public bool FarSkyFavourHolds
	{
		get
		{
			if (FarSkyRecipe == null || FarSkyMade || !ConjunctionsLearned || SovereignSpell != null)
				return false;
			ComboStanding standing = EvaluateCombo(FarSkyRecipe);
			return !standing.Taken && !standing.Sealed;
		}
	}

	// The parent tonight's recipe names for this spell, or null.
	private ComboIngredient FarSkyParent(string spellId) =>
		FarSkyRecipe?.Ingredients.FirstOrDefault(i => i.SpellId.Equals(spellId ?? string.Empty, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Takes tonight's parents' cards out of the deal's two pools - a new spell's card (only there when a slot is free)
	/// and a held parent's upgrade - and returns them in the recipe's order, to be dealt before anything is drawn.
	/// Empty when the favour does not hold. Bans have already been applied to both pools.
	/// </summary>
	private List<LevelUpOption> TakeFarSkyFavoured(List<LevelUpOption> newSpells, List<float> newWeights,
		List<LevelUpOption> heldUpgrades, List<float> heldWeights)
	{
		var favoured = new List<LevelUpOption>();
		if (!FarSkyFavourHolds)
			return favoured;
		foreach (ComboIngredient parent in FarSkyRecipe.Ingredients)
		{
			if (TakeFrom(newSpells, newWeights, parent.SpellId) is LevelUpOption fresh)
				favoured.Add(fresh);
			else if (TakeFrom(heldUpgrades, heldWeights, parent.SpellId) is LevelUpOption upgrade)
			{
				favoured.Add(upgrade);
				// Counted only when this level-up's roll left the held upgrades out: then the card is the favour's.
				if (!upgradesOnOfferThisLevelUp)
					FarSkyFavouredCards++;
			}
		}
		return favoured;
	}

	private static LevelUpOption TakeFrom(List<LevelUpOption> pool, List<float> weights, string spellId)
	{
		int index = pool.FindIndex(o => o != null && o.SpellId.Equals(spellId, StringComparison.OrdinalIgnoreCase));
		if (index < 0)
			return null;
		LevelUpOption option = pool[index];
		pool.RemoveAt(index);
		weights.RemoveAt(index);
		return option;
	}

	/// <summary>On a parent's level 8 page, the named Ascension first. The rest keep their order.</summary>
	private void FavourNamedAscension(LevelUpOption option, SpellData spell)
	{
		if (option?.EvolutionChoices == null || option.EvolutionChoices.Count < 2 || !FarSkyFavourHolds)
			return;
		ComboIngredient parent = FarSkyParent(spell?.Id);
		if (parent == null)
			return;
		int named = option.EvolutionChoices.FindIndex(e => e != null && e.Id.Equals(parent.AscensionId, StringComparison.OrdinalIgnoreCase));
		if (named <= 0)
			return;
		SpellEvolutionOption first = option.EvolutionChoices[named];
		option.EvolutionChoices.RemoveAt(named);
		option.EvolutionChoices.Insert(0, first);
	}

	// TryFuse's last word: tonight's recipe made.
	private void NoteFarSkyFusion(ComboDefinition recipe)
	{
		if (FarSkyRecipe == null || FarSkyMade || recipe == null
			|| !recipe.Id.Equals(FarSkyRecipe.Id, StringComparison.OrdinalIgnoreCase))
			return;
		FarSkyMade = true;
		GD.Print($"FAR SKY: tonight's Conjunction made - {recipe.Name}, at player level {CurrentLevel}");
	}
}
