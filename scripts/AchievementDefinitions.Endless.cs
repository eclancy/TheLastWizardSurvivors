using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// ENDLESS'S THREE FEATS (.ai/endless-mode.md 3.3; EndlessCatalog). Arcane Energy only, never an unlock: endless is the
// far shore's "achievements welcome, unlocks not required", and a Feat that paid a spell or a wizard would put a gate
// behind a mode. Counted on Default or harder, never in the Sketchbook (EndlessCatalog.CountsForFeats). Listed only
// once endless is open (AchievementVisibility.Endless), as the far sky's wait for the far sky.
//
// Measured from the records (SaveData.EndlessRecords), which a night writes and saves at every warden's fall - so each
// completes at a fall, in the night, through the run's one-second achievement watch, as a deed done should. Never from
// the run: past the first night the run IS its first night (the ledger), which never fells a second warden.
public static partial class AchievementDefinitions
{
	private static IEnumerable<AchievementDefinition> EndlessDeeds()
	{
		yield return new AchievementDefinition
		{
			Id = EndlessCatalog.IntoTheNightId, DisplayName = "Into the Night",
			Description = "Keep the vigil, and fell a chapter's warden a second time in one run.",
			RewardText = $"{EndlessCatalog.IntoTheNightCurrency} Arcane Energy", CurrencyReward = EndlessCatalog.IntoTheNightCurrency,
			Category = AchievementCategory.Challenge,
			Kind = AchievementKind.Feat,
			Visibility = AchievementVisibility.Endless,
			Measure = c => EndlessCatalog.MostWardensFelled(c.Save),
			Target = EndlessCatalog.IntoTheNightWardens,
		};
		yield return new AchievementDefinition
		{
			Id = EndlessCatalog.LongWatchId, DisplayName = "The Long Watch",
			Description = "Fell a chapter's warden four times in one run.",
			RewardText = $"{EndlessCatalog.LongWatchCurrency} Arcane Energy", CurrencyReward = EndlessCatalog.LongWatchCurrency,
			Category = AchievementCategory.Challenge,
			Kind = AchievementKind.Feat,
			Visibility = AchievementVisibility.Endless,
			Measure = c => EndlessCatalog.MostWardensFelled(c.Save),
			Target = EndlessCatalog.LongWatchWardens,
		};
		yield return new AchievementDefinition
		{
			Id = EndlessCatalog.EveryVigilKeptId, DisplayName = "Every Vigil Kept",
			Description = "On every chapter of the road that can be held through the night, fell its warden a second time in one run.",
			RewardText = $"{EndlessCatalog.EveryVigilKeptCurrency} Arcane Energy", CurrencyReward = EndlessCatalog.EveryVigilKeptCurrency,
			Category = AchievementCategory.Challenge,
			Kind = AchievementKind.Collection,
			Visibility = AchievementVisibility.Endless,
			Measure = c => EndlessCatalog.ChaptersHeldThroughTheNight(c.Save),
			Target = EndlessCatalog.RoadChapterIds.Count,
		};
	}
}
