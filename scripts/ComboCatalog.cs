using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace WizardSurvivors.scripts;

/// <summary>One parent of a combination: a spell, and the one level 8 Ascension the recipe names on it.</summary>
public sealed class ComboIngredient
{
	public string SpellId { get; init; } = string.Empty;

	/// <summary>For a spell taken in a form (SpellData.Variants): the form the Ascension is the form's own of. Empty otherwise.</summary>
	public string VariantId { get; init; } = string.Empty;

	/// <summary>The level 8 option the recipe names (SpellEvolutionCatalog's id). Never "any".</summary>
	public string AscensionId { get; init; } = string.Empty;

	/// <summary>
	/// The parent's and the Ascension's names as a player reads them, written here rather than loaded so the
	/// derived Feat can be built before any resource is (a save migration reads the deeds). The validator
	/// holds both to the resources (RegressionChecks.ValidateComboCatalog).
	/// </summary>
	public string SpellName { get; init; } = string.Empty;
	public string AscensionName { get; init; } = string.Empty;

	/// <summary>"Hunter's Draw - Hunter's Mark": the parent as the browser and the Achievements row name it.</summary>
	public string Label => $"{SpellName} - {AscensionName}";
}

/// <summary>One combination: two fully ascended spells, each down a named Ascension, made into one.</summary>
public sealed class ComboDefinition
{
	/// <summary>The combination's own spell id, and its SpellData's (SpellData_&lt;Name&gt;.tres at the repo root).</summary>
	public string Id { get; init; } = string.Empty;

	/// <summary>Its name, as its SpellData's (held to it by the validator): the Feat is "Conjunction: Name".</summary>
	public string Name { get; init; } = string.Empty;

	/// <summary>The one verb this combination owns and no Apotheosis or other combination may (.ai/combo-spells.md section 4).</summary>
	public string Verb { get; init; } = string.Empty;

	/// <summary>The first parent. The combination takes its slot.</summary>
	public ComboIngredient First { get; init; }

	/// <summary>The second parent. Its slot is freed.</summary>
	public ComboIngredient Second { get; init; }

	public string ResourcePath { get; init; } = string.Empty;

	/// <summary>The chip on the conjunction page's card: "Conjunction / Converge".</summary>
	public string SynergyTag { get; init; } = string.Empty;

	public IEnumerable<ComboIngredient> Ingredients
	{
		get
		{
			yield return First;
			yield return Second;
		}
	}
}

/// <summary>Why a recipe cannot be made this run, once that is settled for good.</summary>
public enum ComboSeal
{
	None,
	/// <summary>A parent held in the loadout took its other Ascension (an Ascension is permanent).</summary>
	OtherAscension,
	/// <summary>A parent is the wizard's own starting spell, whose last form is his Apotheosis.</summary>
	OwnSpell,
	/// <summary>The two parents name more than <see cref="ComboCatalog.MaxElements"/> elements between them.</summary>
	TooManyElements,
}

/// <summary>Where one recipe stands in a run: each parent's state, and whether the strip may offer it.</summary>
public sealed class ComboStanding
{
	public ComboDefinition Recipe { get; init; }

	/// <summary>The parents as held now, or null when not in the loadout.</summary>
	public SpellData FirstHeld { get; set; }
	public SpellData SecondHeld { get; set; }

	/// <summary>Held at level 8 with the named Ascension.</summary>
	public bool FirstMet { get; set; }
	public bool SecondMet { get; set; }

	/// <summary>Held with the other Ascension taken: sealed for the run.</summary>
	public bool FirstClosed { get; set; }
	public bool SecondClosed { get; set; }

	/// <summary>The parent is the wizard's own starting spell.</summary>
	public bool FirstOwn { get; set; }
	public bool SecondOwn { get; set; }

	/// <summary>Distinct elements the two parents name now, evolution gains included.</summary>
	public int Elements { get; set; }

	/// <summary>When <see cref="Elements"/> is over the ceiling: the milestone that brought the extra one ("Frostfall").</summary>
	public string ExtraElementBy { get; set; } = string.Empty;

	/// <summary>The combination is in the loadout: it was made this run.</summary>
	public bool Taken { get; set; }

	public ComboSeal Seal =>
		FirstOwn || SecondOwn ? ComboSeal.OwnSpell
		: FirstClosed || SecondClosed ? ComboSeal.OtherAscension
		: Elements > ComboCatalog.MaxElements ? ComboSeal.TooManyElements
		: ComboSeal.None;

	public bool Sealed => !Taken && Seal != ComboSeal.None;

	/// <summary>What the strip offers: both parents met, nothing sealing it, not yet made.</summary>
	public bool Ready => !Taken && FirstMet && SecondMet && Seal == ComboSeal.None;
}

/// <summary>
/// The single authority on combination spells (.ai/combo-spells.md, approved by Eric 2026-11-01).
/// </summary>
/// <remarks>
/// A combination is two spells, each at level 8 down the ONE Ascension its recipe names, made into one
/// spell. It is offered in the Ascension strip above the level-up cards, as the Apotheosis is
/// (Player.GetAscensionOffers); taking it spends the pick, both parents leave the loadout, the
/// combination takes the first parent's slot and the second slot is freed. It is terminal: no levels,
/// no milestones, never offered again, never fused again. Neither parent comes back to the level-up
/// pool that run (Player.WasFusedAway).
///
/// The rules this catalog and RegressionChecks.ValidateComboCatalog hold between them:
///   - every recipe names exactly one Ascension on each parent, and that id is one of the parent's
///     level 8 options (for a spell taken in forms, the named form's) - never "any Ascension";
///   - a parent that took the other Ascension seals the recipe for the run, and the strip never
///     offers it;
///   - a wizard's own starting spell is never an ingredient for him: its last form is his Apotheosis;
///   - the combination carries both parents' current weights exactly, so a fusion never moves an
///     element tier, and it is the one spell allowed to name three elements - but never four, so a
///     level 4 element path that brings a fourth refuses the fusion (the browser says which);
///   - each recipe derives one Feat, "Conjunction: Name" (<see cref="FeatId"/>), completed the first
///     time a run meets the recipe, which grants the spell there and then. No schema bump: the deed is
///     derived and its completion stored as every deed's is.
///
/// What the combination DOES lives in its own script (Sagittarius.cs), built from its parents'
/// numbers, read at the fusion from the two frozen parents it carries (SpellData.ComboParents).
/// </remarks>
public static class ComboCatalog
{
	/// <summary>
	/// The milestone a combination's strip offer and its page card carry: past level 8 and past the
	/// Apotheosis's 9, so nothing that reads a milestone as "level 4" or "level 8" mistakes it. Every
	/// screen that tells an Apotheosis by MilestoneLevel asks <see cref="IsComboOption"/> first.
	/// </summary>
	public const int MilestoneLevel = 10;

	/// <summary>The most elements a combination may name: one past every other spell's two.</summary>
	public const int MaxElements = 3;

	/// <summary>The derived Feat's id is this and the recipe's ("conjunction_sagittarius").</summary>
	public const string FeatPrefix = "conjunction_";

	public static string FeatId(string comboId) => FeatPrefix + (comboId ?? string.Empty).Trim();

	private static readonly ComboDefinition[] Definitions =
	{
		// Sagittarius (section 3): Hunter's Mark is the volley that seeks one quarry; Constellation is the
		// lit stars and the lines - the archer's figure. Every lit star becomes a bow aimed at the mark.
		// With Starburst there are no lit stars, with Rain of Arrows the volley falls from the sky: either
		// seals it.
		new()
		{
			Id = "sagittarius",
			Name = "Sagittarius",
			Verb = "converge",
			First = new ComboIngredient
			{
				SpellId = "hunters_draw", AscensionId = BoltEvolutions.HuntersMarkId,
				SpellName = "Hunter's Draw", AscensionName = "Hunter's Mark",
			},
			Second = new ComboIngredient
			{
				SpellId = "starfall", AscensionId = Starfall.ConstellationId,
				SpellName = "Starfall", AscensionName = "Constellation",
			},
			ResourcePath = "res://SpellData_Sagittarius.tres",
			SynergyTag = "Conjunction / Converge",
		},
	};

	public static IReadOnlyList<ComboDefinition> All => Definitions;

	public static ComboDefinition ForId(string id) =>
		string.IsNullOrWhiteSpace(id) ? null
			: Definitions.FirstOrDefault(d => d.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));

	/// <summary>True for a combination's own spell id.</summary>
	public static bool IsCombination(string spellId) => ForId(spellId) != null;

	/// <summary>The recipe a derived Feat mirrors, or null.</summary>
	public static ComboDefinition ForFeat(string achievementId) =>
		achievementId != null && achievementId.StartsWith(FeatPrefix, StringComparison.OrdinalIgnoreCase)
			? ForId(achievementId.Substring(FeatPrefix.Length))
			: null;

	/// <summary>A level-up option that is a combination's strip offer or its page's card.</summary>
	public static bool IsComboOption(LevelUpOption option) =>
		option != null && !string.IsNullOrEmpty(option.ComboId);

	/// <summary>The combination's resource, as ContentValidator loaded it (one load a process, never a static of ours).</summary>
	public static SpellData Template(ComboDefinition recipe) =>
		recipe == null ? null
			: ContentValidator.LoadAllSpells().FirstOrDefault(s => s != null && s.Id.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase));

	/// <summary>Every recipe that names this Ascension on this spell: the level 8 page's mark.</summary>
	public static IEnumerable<ComboDefinition> Naming(string spellId, string ascensionId) =>
		Definitions.Where(d => d.Ingredients.Any(i => Matches(i, spellId) && i.AscensionId.Equals(ascensionId ?? string.Empty, StringComparison.OrdinalIgnoreCase)));

	/// <summary>Every recipe this spell is a parent in, whichever Ascension it names.</summary>
	public static IEnumerable<ComboDefinition> WithParent(string spellId) =>
		Definitions.Where(d => d.Ingredients.Any(i => Matches(i, spellId)));

	/// <summary>The other parent of a recipe this spell is in.</summary>
	public static ComboIngredient PartnerOf(ComboDefinition recipe, string spellId) =>
		recipe == null ? null : Matches(recipe.First, spellId) ? recipe.Second : recipe.First;

	private static bool Matches(ComboIngredient ingredient, string spellId) =>
		ingredient != null && ingredient.SpellId.Equals(spellId ?? string.Empty, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Whether the save knows the recipe: it owns both parents (section 2, "Unlocks and the save"). Derived,
	/// never stored. A save that has made it knows it whatever it owns.
	/// </summary>
	public static bool IsKnown(ComboDefinition recipe, SaveData save) =>
		recipe != null && save != null
		&& (GlobalStatsManager.IsAchievementUnlocked(save, FeatId(recipe.Id))
			|| recipe.Ingredients.All(i => GlobalStatsManager.IsSpellUnlockedForLevelUp(save, i.SpellId)));

	/// <summary>Whether the save has made the recipe at least once: its Feat is done, and the name may be shown.</summary>
	public static bool IsMet(ComboDefinition recipe, SaveData save) =>
		recipe != null && save != null && GlobalStatsManager.IsAchievementUnlocked(save, FeatId(recipe.Id));

	/// <summary>The page's one card, built fresh each time (a Resource held in a static outlives the engine's).</summary>
	public static SpellEvolutionOption FormOf(ComboDefinition recipe, SpellData template) => new()
	{
		Id = recipe.Id,
		DisplayName = template?.Name ?? recipe.Id,
		Description = template?.Description ?? string.Empty,
		MilestoneLevel = MilestoneLevel,
		SynergyTag = recipe.SynergyTag,
		VisualTag = "Conjunction",
	};

	/// <summary>
	/// Where one recipe stands against this loadout. Reports and never decides anything but itself: the
	/// strip offers exactly what <see cref="ComboStanding.Ready"/> says, and the browser draws the rest.
	/// </summary>
	/// <param name="starterSpellId">The wizard's own starting spell, never an ingredient for him.</param>
	public static ComboStanding Evaluate(ComboDefinition recipe, IEnumerable<SpellData> loadout, string starterSpellId)
	{
		var standing = new ComboStanding { Recipe = recipe };
		if (recipe == null)
			return standing;
		List<SpellData> held = (loadout ?? Enumerable.Empty<SpellData>()).Where(s => s != null).ToList();
		standing.Taken = held.Any(s => s.Id.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase));

		standing.FirstHeld = Find(held, recipe.First);
		standing.SecondHeld = Find(held, recipe.Second);
		(standing.FirstMet, standing.FirstClosed) = Judge(standing.FirstHeld, recipe.First);
		(standing.SecondMet, standing.SecondClosed) = Judge(standing.SecondHeld, recipe.Second);
		standing.FirstOwn = IsOwn(recipe.First, starterSpellId);
		standing.SecondOwn = IsOwn(recipe.Second, starterSpellId);

		// The elements the two would carry together, as they stand. A level 4 element path is permanent, so
		// a pair that names four now names four for the rest of the run.
		var elements = new HashSet<Element>();
		foreach (SpellData parent in new[] { standing.FirstHeld, standing.SecondHeld })
		{
			if (parent == null)
				continue;
			foreach (Element element in parent.GetElementWeights().Keys)
				elements.Add(element);
		}
		standing.Elements = elements.Count;
		if (elements.Count > MaxElements)
			standing.ExtraElementBy = ElementPathOf(standing.FirstHeld) ?? ElementPathOf(standing.SecondHeld) ?? string.Empty;
		return standing;
	}

	private static SpellData Find(List<SpellData> held, ComboIngredient ingredient) =>
		held.FirstOrDefault(s => s.Id.Equals(ingredient.SpellId, StringComparison.OrdinalIgnoreCase)
			&& (string.IsNullOrEmpty(ingredient.VariantId)
				|| ingredient.VariantId.Equals(s.SelectedVariantId ?? string.Empty, StringComparison.OrdinalIgnoreCase)));

	// Met: at level 8 down the named Ascension. Closed: ascended down the other. A spell in a form other
	// than the one named never meets it, and is not sealed by that alone - forms are chosen when taken.
	private static (bool Met, bool Closed) Judge(SpellData spell, ComboIngredient ingredient)
	{
		if (spell == null || spell.IsApotheosis)
			return (false, spell?.IsApotheosis == true);
		string taken = spell.SelectedLevel8EvolutionId ?? string.Empty;
		if (string.IsNullOrEmpty(taken))
			return (false, false);
		bool named = taken.Equals(ingredient.AscensionId, StringComparison.OrdinalIgnoreCase);
		return (named && spell.CurrentLevel >= spell.MaxLevel, !named);
	}

	private static bool IsOwn(ComboIngredient ingredient, string starterSpellId) =>
		!string.IsNullOrEmpty(starterSpellId) && ingredient.SpellId.Equals(starterSpellId, StringComparison.OrdinalIgnoreCase);

	// The milestone that brought an element the spell's own tags do not carry: its element path.
	private static string ElementPathOf(SpellData spell)
	{
		if (spell == null || spell.GetBranchElement() == null)
			return null;
		foreach (SpellEvolutionOption evo in new[] { spell.SelectedLevel4Evolution, spell.SelectedLevel8Evolution })
		{
			if (evo?.BonusElementWeights == null)
				continue;
			foreach (var pair in evo.BonusElementWeights)
				if (pair.Value > 0 && !spell.ElementWeights.ContainsKey(pair.Key))
					return evo.DisplayName;
		}
		return null;
	}

	/// <summary>
	/// The sum of both parents' current element weights: what the combination carries, exactly, so the
	/// fusion can never move a tier in either direction (the ADR's rug-pull trap).
	/// </summary>
	public static Godot.Collections.Dictionary<string, int> SumWeights(SpellData first, SpellData second)
	{
		var sum = new Godot.Collections.Dictionary<string, int>();
		foreach (SpellData parent in new[] { first, second })
		{
			if (parent == null)
				continue;
			foreach (var pair in parent.GetElementWeights())
			{
				string key = pair.Key.ToString();
				sum[key] = (sum.TryGetValue(key, out int existing) ? existing : 0) + pair.Value;
			}
		}
		return sum;
	}

	/// <summary>
	/// A parent's level 8 options as a run holds them: the spell's own, or for a spell taken in a form the
	/// named form's (SpellVariants.Preview). Empty when the spell is missing.
	/// </summary>
	public static IReadOnlyList<SpellEvolutionOption> Level8OptionsOf(SpellData template, ComboIngredient ingredient)
	{
		if (template == null)
			return Array.Empty<SpellEvolutionOption>();
		SpellData spell;
		if (!string.IsNullOrEmpty(ingredient?.VariantId) && template.FindVariant(ingredient.VariantId) is SpellVariant form)
			spell = SpellVariants.Preview(template, form);
		else
		{
			spell = (SpellData)template.Duplicate(true);
			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
		}
		return spell?.Level8Options?.ToList() ?? new List<SpellEvolutionOption>();
	}
}
