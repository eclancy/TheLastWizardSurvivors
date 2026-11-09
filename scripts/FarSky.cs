using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>
/// THE FAR SKY: tonight's Conjunction (.ai/second-half.md section 5, piece F4). The half's one new system, and its
/// answer to Eric's "excuses to keep trying new spell combinations" (in-flight item 53): every far-shore run the far
/// shore's own sky names one recipe, the deal favours its two parents, and making it pays.
/// </summary>
/// <remarks>
/// <para><b>What is named.</b> One of <see cref="ComboCatalog.Fusions"/> (never the Sovereign Rite, which is not a
/// recipe of two spells) whose two parents this save can be dealt: owned, not set aside, nobody else's alone. Never
/// one the wizard opens on (<see cref="ComboCatalog.StartsFused"/> - the dark wizard's The Taking, a god's own), and
/// never one that names the wizard's own starting spell (<see cref="ComboSeal.OwnSpell"/>: its last form is his
/// Apotheosis, so the strip would never offer it). Weighted toward recipes the save has never made
/// (<see cref="NeverMadeWeight"/> against <see cref="MadeWeight"/>), and never the same two nights running while
/// there is another to name.</para>
///
/// <para><b>The night is the save's run count</b> (<see cref="Night"/>, <c>LifetimeStats.TotalRuns</c>), so the sky
/// turns after every run, won, lost or quit - which is after every far-shore run, as the doc asks - and needs no
/// save field and no schema step. A road run between two far-shore runs turns it too; nobody on the road sees it.
/// The pick is a pure function of the night, the wizard and the save (a hash, not Godot's RNG), so the world map's
/// card, the run and the pause screen all name the same recipe without anything being stored.</para>
///
/// <para><b>Who sees it.</b> Nothing of it before the save has learned it (<see cref="Teaching.FarSky"/>, taught when
/// the far shore's first god is rekindled - .ai/second-half.md 6.2), and only on a far-shore chapter. The favour
/// itself lives in the deal (Player.FarSky.cs); the card line in WorldMap, the pause line and the payout in
/// Node2DGame.FarSky.cs.</para>
/// </remarks>
public static class FarSky
{
	/// <summary>A recipe the save has never made is this many times likelier to be named than one it has.</summary>
	public const int NeverMadeWeight = 3;

	public const int MadeWeight = 1;

	/// <summary>
	/// Arcane Energy paid at the run's end, every run that makes tonight's Conjunction - flat, after every multiplier,
	/// as a boss's victory bonus is (Node2DGame.CalculateArcaneReward). About half of what a run itself pays.
	/// </summary>
	public const int ArcaneBonus = 100;

	/// <summary>The Feat for making tonight's Conjunction the first time (.ai/second-half.md 4.3).</summary>
	public const string UnderTheFarSkyId = "under_the_far_sky";

	public const int UnderTheFarSkyCurrency = 300;

	/// <summary>The Collection for every recipe made once, measured live against <see cref="ComboCatalog.Fusions"/>.</summary>
	public const string EveryConjunctionKnownId = "every_conjunction_known";

	public const int EveryConjunctionKnownCurrency = 600;

	/// <summary>
	/// Tools only (_BalanceHarness and _UiShot <c>--far-sky</c>): null plays by the game's rule; <see cref="Off"/>
	/// names nothing, even on the far shore; a recipe's id names that one on any chapter, whatever the save has
	/// learned - which is how the favour is measured on a chapter that is not the far shore's.
	/// </summary>
	public static string ToolOverride { get; set; }

	public const string Off = "off";

	/// <summary>Which night it is: the save's run count. Turns once every run ends.</summary>
	public static int Night(SaveData save) => Math.Max(0, save?.Lifetime?.TotalRuns ?? 0);

	/// <summary>The wizard's own starting spell, never a parent for him (the Test Wizard has none to protect).</summary>
	public static string OwnSpellOf(CharacterData character) =>
		character == null || character.Id.Equals("test_wizard", StringComparison.OrdinalIgnoreCase)
			? null
			: character.StartingSpellResource?.Id;

	/// <summary>
	/// Whether the sky may name <paramref name="recipe"/> for this wizard on this save, and if not, why: the rules of
	/// section 5 in one place, which the pick and RegressionChecks.ValidateFarSky both ask.
	/// </summary>
	public static bool MayName(ComboDefinition recipe, SaveData save, string characterId, string ownSpellId, out string why)
	{
		why = string.Empty;
		if (recipe == null || recipe.IsRite)
		{
			why = "not a recipe of two spells";
			return false;
		}
		if (ComboCatalog.StartsFused(recipe, characterId))
		{
			why = "the wizard opens on it";
			return false;
		}
		foreach (ComboIngredient parent in recipe.Ingredients)
		{
			if (!string.IsNullOrEmpty(ownSpellId) && parent.SpellId.Equals(ownSpellId, StringComparison.OrdinalIgnoreCase))
			{
				why = $"it names the wizard's own {parent.SpellName}";
				return false;
			}
			if (!GlobalStatsManager.IsSpellUnlockedForLevelUp(save, parent.SpellId))
			{
				why = $"the save does not own {parent.SpellName}";
				return false;
			}
			if (GlobalStatsManager.IsSpellRemovedFromPool(save, parent.SpellId))
			{
				why = $"{parent.SpellName} is set aside";
				return false;
			}
			if (!UnlockCatalog.MayBeOffered(parent.SpellId, characterId))
			{
				why = $"{parent.SpellName} is another wizard's alone";
				return false;
			}
		}
		return true;
	}

	/// <summary>Every recipe the sky may name for this wizard tonight, in catalog order.</summary>
	public static List<ComboDefinition> Candidates(SaveData save, string characterId, string ownSpellId) =>
		ComboCatalog.Fusions.Where(r => MayName(r, save, characterId, ownSpellId, out _)).ToList();

	/// <summary>A recipe's weight in the pick: heavier never made.</summary>
	public static int WeightOf(ComboDefinition recipe, SaveData save) =>
		ComboCatalog.IsMet(recipe, save) ? MadeWeight : NeverMadeWeight;

	/// <summary>
	/// The recipe the sky names on <paramref name="night"/>, or null when there is none to name. The nights are walked
	/// from the first, each excluding the one before it, so two nights running never name the same recipe while there
	/// is another (the walk is a few thousand steps for the longest-played save, and touches nothing but integers).
	/// </summary>
	public static ComboDefinition PickForNight(SaveData save, string characterId, string ownSpellId, int night)
	{
		List<ComboDefinition> candidates = Candidates(save, characterId, ownSpellId);
		if (candidates.Count == 0)
			return null;
		if (candidates.Count == 1)
			return candidates[0];
		int[] weights = candidates.Select(r => WeightOf(r, save)).ToArray();
		int previous = -1;
		for (int n = 0; n <= Math.Max(0, night); n++)
			previous = Roll(weights, previous, (uint)n);
		return candidates[previous];
	}

	/// <summary>
	/// What the sky names tonight for this wizard on this save, or null: nothing before the save has learned the far
	/// sky, and nothing for a save or wizard with no recipe to name.
	/// </summary>
	public static ComboDefinition Tonight(SaveData save, CharacterData character)
	{
		if (save == null || character == null || !Teachings.HasLearned(save, Teaching.FarSky))
			return null;
		return PickForNight(save, character.Id, OwnSpellOf(character), Night(save));
	}

	/// <summary>
	/// The recipe a run on <paramref name="stage"/> is played under, or null: tonight's, on a far-shore chapter. A tool's
	/// <see cref="ToolOverride"/> wins: off is nothing, a recipe's id is that recipe on any chapter.
	/// </summary>
	public static ComboDefinition ForRun(SaveData save, CharacterData character, StageDefinition stage)
	{
		if (ToolOverride != null)
			return ToolOverride.Equals(Off, StringComparison.OrdinalIgnoreCase) ? null : ComboCatalog.ForId(ToolOverride) is { IsRite: false } forced ? forced : null;
		return StageCatalog.IsOnFarShore(stage) ? Tonight(save, character) : null;
	}

	/// <summary>"Sagittarius - Hunter's Draw (Hunter's Mark) and Starfall (Constellation)": the recipe and how it is made.</summary>
	public static string Describe(ComboDefinition recipe) =>
		recipe == null ? string.Empty
			: $"{recipe.Name} - {recipe.First.SpellName} ({recipe.First.AscensionName}) and {recipe.Second.SpellName} ({recipe.Second.AscensionName})";

	/// <summary>
	/// The line a world map card carries on every far-shore stop - "Tonight: The Taking" - or null where the sky names
	/// nothing. The recipe's name alone: the card has one line to spare once a far chapter's four rewards, its fallen
	/// warden and its longest stand are on it, and the full line (<see cref="Describe"/>) wrapped to three on a PC and two
	/// on a phone and pushed Enter off the foot of the screen on both (2026-11-08, on F3's and F5's White Downs). The pause
	/// screen names it in full; RegressionChecks.ValidateFarSky holds every recipe's line to one card line.
	/// </summary>
	public static string CardLine(SaveData save, StageDefinition stage, CharacterData character)
	{
		if (!StageCatalog.IsOnFarShore(stage))
			return null;
		ComboDefinition tonight = Tonight(save, character);
		return tonight == null ? null : $"Tonight: {tonight.Name}";
	}

	// One weighted draw, never <paramref name="exclude"/> when anything else has weight. A hash of the night, not an
	// RNG, so the same night always names the same recipe on every screen and every machine.
	private static int Roll(int[] weights, int exclude, uint night)
	{
		int total = 0;
		for (int i = 0; i < weights.Length; i++)
			if (i != exclude)
				total += Math.Max(0, weights[i]);
		if (total <= 0)
			return exclude;
		int pick = (int)(Mix(night) % (uint)total);
		for (int i = 0; i < weights.Length; i++)
		{
			if (i == exclude)
				continue;
			pick -= Math.Max(0, weights[i]);
			if (pick < 0)
				return i;
		}
		return exclude;
	}

	// SplitMix32's finaliser over the night: every bit of the night reaches every bit of the roll.
	private static uint Mix(uint x)
	{
		x += 0x9E3779B9u;
		x = (x ^ (x >> 16)) * 0x85EBCA6Bu;
		x = (x ^ (x >> 13)) * 0xC2B2AE35u;
		return x ^ (x >> 16);
	}
}
