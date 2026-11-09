using Godot;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

public sealed class LevelUpOption
{
	public string SpellId { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	/// <summary>A new spell's one line on what it does (SpellData.Tagline). Empty on every other card.</summary>
	public string Tagline { get; set; } = string.Empty;
	public string UpgradeSummary { get; set; } = string.Empty;

	/// <summary>The spell's stats as it would arrive, for a card with no previous level to diff.</summary>
	/// <remarks>
	/// UpgradeSummary is a list of CHANGES, so it is empty for a brand-new spell - there is nothing
	/// to change yet. Before this existed the description was the only body text a new-spell card
	/// had, so removing descriptions left those cards blank. This is what "just leave the stats"
	/// means for a card that has no deltas to show.
	/// </remarks>
	public string StatSummary { get; set; } = string.Empty;
	public int NextLevel { get; set; } = 1;
	// The spell's maximum level, used to draw one level pip per available level on the option card.
	public int MaxLevel { get; set; } = 1;
	public bool IsNewUnlock { get; set; } = true;
	public bool IsPassive { get; set; } = false;

	// A boon rather than a spell: permanent, never levelled, and it does not consume a spell slot.
	// See BoonCatalog and .ai/passives-and-items.md.
	public bool IsBoon { get; set; } = false;
	// (RequiresSlotSwap is gone with the swap itself: a full book is offered no new spell, so no
	// option ever needs a held one erased to make room. See Player.GetLevelUpOptions.)

	// Optional spell icon (SpellData.Icon) shown on the card - LevelUpMenu falls back to a shared
	// default icon when this is null (no unique art for most spells yet, #30).
	public Texture2D Icon { get; set; }

	// Element preview data (issue #15): how much this option would change each element's instance
	// count if chosen, keyed by element name, plus the resulting count after the pick.
	public Dictionary<string, int> ElementContribution { get; set; } = new();
	public Dictionary<string, int> ResultingElementCounts { get; set; } = new();

	// The spell's own element tag weights (what tags this spell provides), independent of whether
	// it's a new unlock or an upgrade. Used to render the colored tag chips on each option card.
	public Dictionary<string, int> SpellElementTags { get; set; } = new();

	// Evolution / Branching Milestone Support (Issue #48 & #49).
	public bool IsEvolutionMilestone { get; set; } = false;
	public int MilestoneLevel { get; set; } = 0; // 4 or 8
	public List<TheOneHeMissed.scripts.SpellEvolutionOption> EvolutionChoices { get; set; } = new();
	public TheOneHeMissed.scripts.SpellEvolutionOption SelectedEvolution { get; set; }

	/// <summary>Evolution id to the reason it cannot be taken. Absent means it can.</summary>
	/// <remarks>
	/// Locked ascensions are still shown, greyed, carrying their requirement. Filtering them out
	/// instead would mean the player never learns the rule exists - they would just see two
	/// choices one run and three the next and conclude the game was being random at them.
	/// </remarks>
	public Dictionary<string, string> EvolutionLockReason { get; set; } = new();

	/// <summary>
	/// A level 4 page's paths that would seal a shown recipe by making its pair name a fourth element: evolution id to
	/// the recipe's name ("Sagittarius"). The card says "Seals Sagittarius" (rung I of .ai/conjunction-progression.md,
	/// Player.NoteRecipeSeals). Absent means the path seals nothing.
	/// </summary>
	public Dictionary<string, string> RecipeSeals { get; set; } = new();

	/// <summary>
	/// Evolution ids open only because the far sky waives their element gate (Player.FarSkyWaivesElementGate): the
	/// level 8 page says so on the path, so the player sees why it is offered.
	/// </summary>
	public HashSet<string> ElementGateWaived { get; set; } = new();
	public string SynergyTag { get; set; } = string.Empty;
	public string SynergyDescription { get; set; } = string.Empty;

	/// <summary>
	/// A combination's strip offer (ComboCatalog): the recipe's id, which is also the combination's spell
	/// id. Empty on every other option. Its MilestoneLevel is ComboCatalog.MilestoneLevel, past the
	/// Apotheosis's, so every screen that tells an Apotheosis by its milestone asks this first.
	/// </summary>
	public string ComboId { get; set; } = string.Empty;

	// --- Forms (SpellData.Variants; .ai/decisions/2026-10-28-spell-variants.md) -----------------------

	/// <summary>
	/// For a new spell that is taken in a form: a card per form, each built from the spell in that form.
	/// Taking this card opens a page of them instead of choosing (LevelUpMenu.BuildVariantSelectionButtons);
	/// taking one of them chooses. Empty for every other card.
	/// </summary>
	public List<LevelUpOption> Variants { get; set; } = new();

	/// <summary>On a form's own card: the form it is. Its selection is SpellVariants.Selection(SpellId, VariantId).</summary>
	public string VariantId { get; set; } = string.Empty;

	/// <summary>The heading of the page of forms ("Choose your weapon"), from SpellData.VariantPrompt.</summary>
	public string VariantPrompt { get; set; } = string.Empty;

	/// <summary>True for a card that opens a page of forms rather than choosing.</summary>
	public bool OffersVariants => Variants != null && Variants.Count > 0 && string.IsNullOrEmpty(VariantId);

	/// <summary>What choosing this card sends (LevelUpMenu.WeaponSelected): the spell, or the spell and its form.</summary>
	public string Selection => TheOneHeMissed.scripts.SpellVariants.Selection(SpellId, VariantId);

	/// <summary>
	/// What a bot sends to take this card without stopping on a page: the card's own selection, or - for a
	/// card that opens a page of forms - the form <paramref name="preferredVariantId"/> if it is one of them,
	/// else the first. Every tool that drives level-ups (the balance harness, the probes, _UiShot) takes
	/// cards through this, so none can stall on the page.
	/// </summary>
	public string AutoChoice(string preferredVariantId = null)
	{
		if (!OffersVariants)
			return Selection;
		LevelUpOption form = null;
		if (!string.IsNullOrWhiteSpace(preferredVariantId))
			form = Variants.FirstOrDefault(v => v.VariantId.Equals(preferredVariantId.Trim(), System.StringComparison.OrdinalIgnoreCase));
		return (form ?? Variants[0]).Selection;
	}

	public string GetButtonText()
	{
		string prefix = IsNewUnlock ? "New" : "Upgrade";
		return $"{DisplayName}  [{prefix} Lv {NextLevel}]";
	}

	public string GetElementPreviewText()
	{
		if (ResultingElementCounts == null || ResultingElementCounts.Count == 0)
			return string.Empty;

		var parts = new List<string>();
		foreach (var pair in ResultingElementCounts.OrderByDescending(p => p.Value))
		{
			string element = pair.Key;
			int resultingCount = pair.Value;
			int delta = ElementContribution != null && ElementContribution.TryGetValue(element, out int d) ? d : 0;
			int tier = resultingCount >= 6 ? 6 : resultingCount >= 4 ? 4 : resultingCount >= 2 ? 2 : 0;
			string deltaText = delta > 0 ? $" +{delta}" : string.Empty;
			string tierText = tier > 0 ? $" (Tier {tier})" : string.Empty;
			parts.Add($"{element}{deltaText} -> {resultingCount}{tierText}");
		}
		return string.Join("   ", parts);
	}
}

// Minimal info about a currently-equipped spell, used by the ascension browser and (via
// ElementWeights/IsPassive) the elemental tag section's passive-highlight logic.
public sealed class EquippedSpellInfo
{
	public string Id { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public int CurrentLevel { get; set; } = 1;
	public Dictionary<string, int> ElementWeights { get; set; } = new();
	public bool IsPassive { get; set; } = false;

	// Optional spell icon (SpellData.Icon) shown in the ascension browser.
	public Texture2D Icon { get; set; }

	/// <summary>
	/// Every level 8 branch this spell has, and what still stands between the player and each one.
	/// Filled by Player.BuildAscensionPreview and read only by the ascension browser.
	/// </summary>
	/// <remarks>
	/// An ascension is the biggest single decision in a run and until now the player met it by
	/// surprise: nothing anywhere said which spells had one, what it wanted, or how close they
	/// were. Carrying it on the equipped-spell record rather than adding a parameter to SetOptions
	/// keeps the two probe scenes that call that method compiling unchanged.
	/// </remarks>
	public List<AscensionInfo> Ascensions { get; set; } = new();

	/// <summary>
	/// The wizard's Apotheosis, on the one spell it is the final form of; null on every other spell
	/// and for a wizard with none (the Test Wizard). Filled by Player.BuildApotheosisPreview.
	/// </summary>
	public ApotheosisPreview Apotheosis { get; set; }
}

/// <summary>
/// A wizard's Apotheosis as the ascension browser and the pause screen show it: the form, every gate
/// with how far along the run is, and where it stands. It reports, it never grants - the strip's
/// offer (Player.GetAscensionOffers) is still the only way to take it.
/// </summary>
/// <remarks>
/// From Eric's playtest (2026-11-01): "Let's make it clearer that apotheosis is an option, showing in
/// 'ascensions', etc." Until then a player met it only when the strip offered it.
/// </remarks>
public sealed class ApotheosisPreview
{
	public string SpellId { get; set; } = string.Empty;
	public string SpellName { get; set; } = string.Empty;
	public string FormId { get; set; } = string.Empty;
	public string FormName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	/// <summary>The form's own picture (EvolutionIcons), or the spell's.</summary>
	public Texture2D Icon { get; set; }

	/// <summary>Every gate, met or not: the spell at max level, ascended, not combined, your level (and the Hayward's, unlearned).</summary>
	public List<AscensionRequirement> Requirements { get; set; } = new();

	/// <summary>In the strip now: Player.TryBuildApotheosisOffer's own answer, never a second copy of the gate.</summary>
	public bool Offered { get; set; }

	/// <summary>The spell has taken its final form.</summary>
	public bool Taken { get; set; }

	/// <summary>Out of reach this run: the spell went into a Conjunction (ApotheosisCatalog.SpellMeetsGate's "not combined").</summary>
	public bool Closed { get; set; }
}

/// <summary>
/// One combination recipe the save knows (ComboCatalog), as the ascension browser shows it: the two
/// parents with their named Ascensions, each marked when this run holds it, and the result - sealed
/// until the save has made it once. It reports, it never grants: the strip's offer
/// (Player.GetAscensionOffers) is the only way to make it.
/// </summary>
public sealed class ComboPreview
{
	public string Id { get; set; } = string.Empty;
	/// <summary>"Hunter's Draw + Starfall": what the tree's node is called.</summary>
	public string ParentsName { get; set; } = string.Empty;
	/// <summary>The first parent's icon, for the node.</summary>
	public Texture2D ParentIcon { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public Texture2D Icon { get; set; }

	/// <summary>One Ingredient gate per parent, then any seal (OwnSpell, ElementCeiling).</summary>
	public List<AscensionRequirement> Requirements { get; set; } = new();

	/// <summary>The save has made it once (its Feat is done): the name, line and picture may be shown.</summary>
	public bool Revealed { get; set; }

	/// <summary>In the strip now: ComboStanding.Ready, the strip's own answer.</summary>
	public bool Offered { get; set; }

	/// <summary>Made this run.</summary>
	public bool Taken { get; set; }

	/// <summary>Out of reach this run: a parent took the other Ascension, is the wizard's own, or the pair names four.</summary>
	public bool Sealed { get; set; }

	/// <summary>The recipe's two parent ids and named Ascensions, for the level 8 page's mark.</summary>
	public List<(string SpellId, string AscensionId)> Names { get; set; } = new();
}

/// <summary>One level 8 branch, as the ascension browser needs to show it.</summary>
public sealed class AscensionInfo
{
	public string Id { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public Texture2D Icon { get; set; }

	/// <summary>
	/// What is not yet true, in the player's own terms - "Spell level 8 (5/8)", "Requires 4 Fire
	/// (2/4)". Empty means the branch is available the moment the spell next levels.
	/// </summary>
	public List<string> UnmetRequirements { get; set; } = new();

	/// <summary>
	/// Every gate, met or not, in the order the browser lists them (spell level, element, player
	/// level), so it can tick the met ones and point at the rest. Filled alongside
	/// <see cref="UnmetRequirements"/> by Player.BuildAscensionPreview; a caller that only fills the
	/// strings (the probe scenes) gets them listed as plain lines.
	/// </summary>
	public List<AscensionRequirement> Requirements { get; set; } = new();

	public bool IsAvailable => UnmetRequirements.Count == 0;

	/// <summary>This is the branch the spell ascended into this run.</summary>
	/// <remarks>
	/// The requirements of an ascended spell stay met - its level, tags and the player's level do not
	/// go down - so before this the browser went on calling the spell "Ready" after it had ascended,
	/// and the Ascensions button kept glowing for it.
	/// </remarks>
	public bool Taken { get; set; }

	/// <summary>Another branch of this spell was taken, so this one can no longer be chosen this run.</summary>
	public bool Closed { get; set; }

	/// <summary>Takeable now: every gate holds and the spell has not ascended yet.</summary>
	public bool Open => IsAvailable && !Taken && !Closed;
}

public enum AscensionRequirementKind
{
	SpellLevel,
	Element,
	PlayerLevel,
	/// <summary>
	/// The campaign's gate: the save has not been taught this system yet (Teachings; Player.AscensionLearned,
	/// ApotheosisLearned, ConjunctionsLearned). <see cref="AscensionRequirement.Line"/> says where it is learned.
	/// </summary>
	Learned,
	/// <summary>An Apotheosis gate: the spell has taken a level 8 Ascension, any of them. Have 1 when it has.</summary>
	Ascended,
	/// <summary>
	/// An Apotheosis gate: the spell was not combined with another (a Conjunction, ComboCatalog). Have 1 while
	/// it was not; Closed when it was, since a fusion is never undone.
	/// </summary>
	NotCombined,
	/// <summary>
	/// A combination's parent (ComboCatalog): <see cref="AscensionRequirement.SpellName"/> at level 8 down the
	/// Ascension <see cref="AscensionRequirement.PathName"/>. Have 1 when held so; Closed when it took the other.
	/// </summary>
	Ingredient,
	/// <summary>A combination whose parent is the wizard's own starting spell: never his to make. Always Closed.</summary>
	OwnSpell,
	/// <summary>A combination whose parents name four elements between them: never offered. Always Closed.</summary>
	ElementCeiling,
}

/// <summary>One gate on an ascension, with how far along the player is.</summary>
public sealed class AscensionRequirement
{
	public AscensionRequirementKind Kind { get; set; }
	public int Have { get; set; }
	public int Need { get; set; }

	/// <summary>
	/// For an element gate, the element the player is closest in (what the browser names), and every
	/// element that would satisfy it - an ascension with no element of its own takes any of its
	/// spell's tags.
	/// </summary>
	public TheOneHeMissed.scripts.Element? Element { get; set; }
	public List<TheOneHeMissed.scripts.Element> AnyOf { get; set; } = new();

	/// <summary>For a combination's Ingredient gate: the Ascension it names on that parent.</summary>
	public string PathName { get; set; } = string.Empty;
	/// <summary>Out of reach for the rest of the run: an ingredient down the other Ascension, a fused spell.</summary>
	public bool Closed { get; set; }

	/// <summary>For a combination's gates: the parent spell's name ("Hunter's Draw"), or the path that brought a fourth element.</summary>
	public string SpellName { get; set; } = string.Empty;

	/// <summary>For a Learned gate: where the system is learned (Teachings.SealedLine). Empty otherwise.</summary>
	public string Line { get; set; } = string.Empty;

	/// <summary>
	/// An element gate the far sky waives (Player.FarSkyWaivesElementGate, decision 10 of .ai/second-half.md): tonight's
	/// recipe's named Ascension on a far-shore run. Met whatever Have says; set only when Have alone would not meet it.
	/// </summary>
	public bool Waived { get; set; }

	/// <summary>
	/// An element gate's share from the recipe's other parent (rung III of .ai/conjunction-progression.md, "the pair shares
	/// its light"; ConjunctionLadder.PairShareFor): counted toward <see cref="Need"/> beside <see cref="Have"/>, which stays
	/// the player's own count. Zero, and <see cref="SharedBy"/> empty, everywhere the rule does not apply.
	/// </summary>
	public int Shared { get; set; }
	public string SharedBy { get; set; } = string.Empty;

	public bool Met => Waived || Have + Shared >= Need;
	public int Missing => Waived ? 0 : System.Math.Max(0, Need - Have - Shared);
}
