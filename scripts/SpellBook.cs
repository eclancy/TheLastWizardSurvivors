using System;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>
/// The one authority on how many spells a save's book holds (.ai/slow-start.md, section 2). Eric,
/// 2026-11-01: "When starting the story, let's limit the number of spells you can have equipped, and have
/// unlockables to increase that, like additional spell pages in the shop."
/// </summary>
/// <remarks>
/// <para>A new save holds <see cref="NewSavePages"/>: the wizard's own spell and two picks. Every done deed's
/// <see cref="AchievementDefinition.PageReward"/> adds - today only Survivor (survive 10 minutes), the 4th
/// page, which opens mid-run in the run that earns it - and the shop's Bound Pages add the 5th and 6th. The
/// three add in any order, and the book is capped at <see cref="Player.MaxSpellSlots"/>, which still means
/// the full book: the element reachability floor, Full Grimoire's target and the HUD's widest strip.</para>
///
/// <para>A save that played before the slow start kept its six (<see cref="SaveData.FullBookKept"/>, schema
/// 22), and the shop shows its Bound Pages complete at no charge. The Blank Folio is not a page of the book:
/// it is a relic, one run, read by Player.SpellSlots on top of this.</para>
///
/// <para>Derived from the deeds rather than written when one completes, so <c>Reconcile</c> needs nothing and
/// a page can move to another deed without a migration.</para>
/// </remarks>
public static class SpellBook
{
	/// <summary>What a new save's book holds: the wizard's own spell and two picks.</summary>
	public const int NewSavePages = 3;

	/// <summary>The shop's row (MainMenu.BuildUpgradeDefinitions): an ArcaneUpgradeLevels id.</summary>
	public const string ShopUpgradeId = "pages";
	public const int ShopLevels = 2;
	public const int ShopBaseCost = 200;
	public const int ShopCostPerLevel = 200;

	/// <summary>
	/// A tool's override for every run's book (_BalanceHarness --slots=N), or 0 for none. Never set by the game.
	/// </summary>
	public static int ToolOverride { get; set; }

	/// <summary>How many spells this save's book holds in a run, before the Blank Folio. Null: the full book.</summary>
	public static int PagesFor(SaveData data)
	{
		if (ToolOverride > 0)
			return Math.Clamp(ToolOverride, 1, Player.MaxSpellSlots);
		if (data == null || data.FullBookKept)
			return Player.MaxSpellSlots;
		return Math.Min(Player.MaxSpellSlots, NewSavePages + DeedPages(data) + BoughtPages(data));
	}

	/// <summary>What the book would hold with <paramref name="bought"/> levels of Bound Pages: the shop's stat line.</summary>
	public static int PagesWithBought(SaveData data, int bought)
	{
		if (data == null || data.FullBookKept)
			return Player.MaxSpellSlots;
		return Math.Min(Player.MaxSpellSlots, NewSavePages + DeedPages(data) + Math.Clamp(bought, 0, ShopLevels));
	}

	/// <summary>Pages paid by the save's done deeds.</summary>
	public static int DeedPages(SaveData data) => data == null ? 0
		: AchievementDefinitions.All.Where(d => d.PageReward > 0 && GlobalStatsManager.IsAchievementUnlocked(data, d.Id))
			.Sum(d => d.PageReward);

	/// <summary>Levels of Bound Pages bought.</summary>
	public static int BoughtPages(SaveData data) =>
		data == null ? 0 : Math.Clamp(GlobalStatsManager.GetUpgradeLevel(data, ShopUpgradeId), 0, ShopLevels);

	/// <summary>The shop row's level as the shop shows it: complete for a kept book, at no charge.</summary>
	public static int ShopLevelShown(SaveData data) =>
		data != null && data.FullBookKept ? ShopLevels : BoughtPages(data);
}
