using Godot;
using TheOneHeMissed.scripts;

// ENDLESS ON THE RESULTS (.ai/endless-mode.md 1.8, 3.4; RunReport.EndlessNight). A run that kept the vigil shows its night
// in the headline instead of the chapter's clock - "Night 3 · 41:20", the wardens it felled, and whether that is a new
// record for this wizard here - built as the Boss Rush's tile is. A death in a night is headed "Fell in the third
// night", not "Defeat": the save already has the first night's victory.
public partial class GameOverScreen
{
	// "Fell in the third night" for a death in a night; null for every other run, which keeps its own heading.
	private string EndlessHeading(RunResult result) =>
		report != null && report.EndlessNight > 0 && result != null && result.Outcome == RunOutcome.Defeat
			? $"Fell in the {EndlessCatalog.Ordinal(report.EndlessNight)} night"
			: null;

	// The headline's first tile in a run that kept the vigil: the night and the clock, the wardens felled, a record.
	private Control BuildEndlessTile()
	{
		int felled = Mathf.Max(0, report.EndlessWardensFelled);
		string caption = felled == 1 ? "1 warden felled" : $"{felled} wardens felled";
		if (report.EndlessNewRecord)
		{
			string who = string.IsNullOrWhiteSpace(report.CharacterName) ? "you" : report.CharacterName;
			caption += $"  ·  a new record for {who} here";
		}
		// The bar is how far into its night the run got: full at a fall it left by the portal.
		float night = Mathf.Max(1f, EndlessCatalog.ChapterVictorySeconds(StageCatalog.GetByIndex(report.StageIndex)?.Id));
		float into = report.EndlessClockSeconds - night * (report.EndlessNight - 1);
		float fraction = Mathf.Clamp(into / night, 0f, 1f);
		Control tile = BuildHeadlineTile("duration", EndlessCatalog.NightLine(report.EndlessNight, report.EndlessClockSeconds),
			"The night", fraction, caption, report.EndlessNewRecord);
		tile.Name = "EndlessTile";
		tile.SizeFlagsStretchRatio = narrow ? 1.25f : 1.6f;
		return tile;
	}

	// The breakdown's line for the later nights' purse: "2 nights more - 26 minutes, 1 warden (at most 600)".
	private static string EndlessBreakdownLabel(ArcaneRewardBreakdown a)
	{
		string minutes = a.NightMinutes == 1 ? "1 minute" : $"{a.NightMinutes} minutes";
		string wardens = a.NightWardens <= 0 ? string.Empty : a.NightWardens == 1 ? ", 1 warden" : $", {a.NightWardens} wardens";
		string cap = a.NightCapped ? $"  (at most {EndlessCatalog.ArcaneCap})" : string.Empty;
		return $"The night after: {minutes}{wardens}{cap}";
	}
}
