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

	// On a PC the heading stacks over the where-line in a 250-wide column: a sentence wraps there, two short lines,
	// instead of widening the column and squeezing the headline's tiles.
	private void WrapEndlessHeading(Label title)
	{
		if (EndlessHeading(report?.Result) == null)
			return;
		title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		title.CustomMinimumSize = new Vector2(250f, 0f);
	}

	// The headline's first tile in a run that kept the vigil: the night and the clock, the wardens felled, a record.
	private Control BuildEndlessTile()
	{
		int felled = Mathf.Max(0, report.EndlessWardensFelled);
		string wardens = felled == 1 ? "1 warden felled" : $"{felled} wardens felled";
		// "A new record" - for this wizard, here, on this preset, which the line beside the heading already names.
		string caption = report.EndlessNewRecord ? "A new record" : string.Empty;
		// The bar is how far into its night the run got: full at a fall it left by the portal.
		float night = Mathf.Max(1f, EndlessCatalog.ChapterVictorySeconds(StageCatalog.GetByIndex(report.StageIndex)?.Id));
		float into = report.EndlessClockSeconds - night * (report.EndlessNight - 1);
		float fraction = Mathf.Clamp(into / night, 0f, 1f);
		// The clock as the number, the night and its wardens as the label - "41:20" over "Night 3 · 2 wardens felled".
		// The whole "Night 3 · 41:20" as the number, at the headline's size, pushed a PC's band past 1280; the wardens
		// as a wrapping caption broke a word a line in a tile that narrow. The label never wraps, so it widens the tile
		// by what the level and kill tiles beside it do not need.
		Control tile = BuildHeadlineTile("duration", EndlessCatalog.FormatClock(report.EndlessClockSeconds),
			$"Night {report.EndlessNight}  ·  {wardens}", fraction, caption, report.EndlessNewRecord);
		tile.Name = "EndlessTile";
		tile.SizeFlagsStretchRatio = narrow ? 1.25f : 1.6f;
		return tile;
	}

	// The breakdown's line for the later nights' purse: "Past the first night: 26 minutes, 1 warden  (at most 600)".
	private static string EndlessBreakdownLabel(ArcaneRewardBreakdown a)
	{
		string minutes = a.NightMinutes == 1 ? "1 minute" : $"{a.NightMinutes} minutes";
		string wardens = a.NightWardens <= 0 ? string.Empty : a.NightWardens == 1 ? ", 1 warden" : $", {a.NightWardens} wardens";
		string cap = a.NightCapped ? $"  (at most {EndlessCatalog.ArcaneCap})" : string.Empty;
		return $"Past the first night: {minutes}{wardens}{cap}";
	}
}
