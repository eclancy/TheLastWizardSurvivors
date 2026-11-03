using System;
using Godot;

namespace WizardSurvivors.scripts;

/// <summary>
/// The stand-in second event: a TOOL-ONLY way to put a second side event in a chapter, so the plumbing for
/// two (P2, .ai/chapter-wizards.md 6.3) can be driven and photographed before any real chapter has two.
/// </summary>
/// <remarks>
/// <para><c>--second-event-test[=STAGE]</c> on a tool scene (<c>_ExploreProbe</c>, <c>_UiShot</c>,
/// <c>_BalanceHarness</c> - any scene whose file name starts with "_", <see cref="SaveManager.IsToolSceneLaunch"/>)
/// appends one event to that chapter's list in <c>SideEventDirector</c>: a copy of the grove (the simplest
/// event there is - stand in a ring), at its own site, <see cref="SiteId"/>. STAGE is a stage index or id;
/// bare, it is the launch's <c>--stage=N</c>, or the Enchanted Forest. Nothing else turns it on: the game,
/// launched without a tool scene, never has it, whatever its arguments say.</para>
///
/// <para>So that every system downstream has something real to read, the stand-in carries what a real second
/// event would: a hint (<see cref="Hint"/>, the grove's sound and leaves with its own lines), a clue for the
/// chapter note (<see cref="Clue"/>), the words of its derived Road deed (<see cref="Words"/>), and a prize -
/// <see cref="Prize"/>, the Quicksilver Bead, a shop boon a new save does not own, so the hints play on a
/// fresh save, the map card gains a slot and winning it is a first win. Each is answered only by the LOOKUP
/// that would find a real one (StageHintCatalog.ForSite, ChapterNotes.CluesFor, AchievementDefinitions'
/// words, UnlockCatalog.ForDiscoverySite), never added to a catalog's list, so the catalogs themselves never
/// hold it. RegressionChecks.ValidateSideEventTable reports it whenever it is in a chapter's list - on the
/// tool run that asked for it as a note, and anywhere else as the leak it would be.</para>
/// </remarks>
public static class SecondEventStandIn
{
	/// <summary>The user argument that asks for it.</summary>
	public const string Flag = "--second-event-test";

	/// <summary>The stand-in's site: no real event may use it.</summary>
	public const string SiteId = "standin_second_event";

	/// <summary>What it pays: a shop boon, unowned on a new save, so a win on a fresh save is a first win.</summary>
	public const string PrizeBoonId = "quicksilver_bead";

	private static string stageId;

	/// <summary>
	/// The chapter this launch adds the stand-in to, or empty: only a tool scene given <see cref="Flag"/>.
	/// Read from the command line once, so every catalog asking at any moment of the launch gets one answer.
	/// </summary>
	public static string StageId => stageId ??= Resolve(OS.GetCmdlineUserArgs(), SaveManager.IsToolSceneLaunch());

	/// <summary>True on a tool run that asked for the stand-in.</summary>
	public static bool Active => StageId.Length > 0;

	/// <summary>True when this launch adds the stand-in to <paramref name="stage"/>'s events.</summary>
	public static bool AddsTo(string stage) =>
		Active && StageId.Equals(stage ?? string.Empty, StringComparison.OrdinalIgnoreCase);

	/// <summary>True for the stand-in's site id.</summary>
	public static bool IsStandInSite(string siteId) =>
		SiteId.Equals(siteId ?? string.Empty, StringComparison.OrdinalIgnoreCase);

	/// <summary>Its line in the chapter's list: a copy of the grove, at the stand-in's own site.</summary>
	public static readonly SideEventEntry Entry = new(SiteId, () => new GroveEvent());

	/// <summary>Its hint: the grove's whisper and leaves, under lines of its own that name nothing.</summary>
	public static readonly StageHint Hint = new()
	{
		SiteId = SiteId,
		Voice = HintVoice.Sound,
		Sound = SfxCatalog.HintGroveWhisper,
		Cue = HintCue.Leaves,
		Lines = new[]
		{
			"Stand-in: a second sound, from another quarter.",
			"Stand-in: the second sound again.",
			"Stand-in: still the second sound.",
		},
	};

	/// <summary>Its clue in the chapter note, as a site clue (a boon is not a wizard, so it has no held line).</summary>
	public const string Clue = "Stand-in: a second thing is kept out here.";

	/// <summary>Its Road deed's name, line and reward (AchievementDefinitions' side-event words).</summary>
	public static readonly (string Site, string Name, string Description, string Reward) Words =
		(SiteId, "The Stand-in", "Win the stand-in second event (a tool run's test).", "Unlocks Quicksilver Bead");

	/// <summary>What the site pays, as UnlockCatalog.ForDiscoverySite would list a real one.</summary>
	public static readonly UnlockDefinition Prize = new()
	{
		Id = PrizeBoonId,
		Kind = UnlockKind.Boon,
		Source = UnlockSource.Achievement,
		SourceId = AchievementDefinitions.EventDeedPrefix + SiteId,
		SiteId = SiteId,
		LockedHint = "Stand-in: won at the second event (a tool run's test).",
	};

	/// <summary>
	/// The stage id <paramref name="userArgs"/> ask for, or empty when they do not, or when this is not
	/// <paramref name="toolLaunch"/>. Public for the validator, which proves the game's own launch never gets one.
	/// </summary>
	public static string Resolve(string[] userArgs, bool toolLaunch)
	{
		if (!toolLaunch || userArgs == null)
			return string.Empty;
		string asked = null;
		string stageArg = null;
		foreach (string arg in userArgs)
		{
			if (arg == Flag)
				asked = string.Empty;
			else if (arg.StartsWith(Flag + "=", StringComparison.Ordinal))
				asked = arg.Substring(Flag.Length + 1).Trim();
			else if (arg.StartsWith("--stage=", StringComparison.Ordinal))
				stageArg = arg.Substring("--stage=".Length).Trim();
		}
		if (asked == null)
			return string.Empty;
		string wanted = asked.Length > 0 ? asked : stageArg ?? "stage_0";
		StageDefinition stage = int.TryParse(wanted, out int index) ? StageCatalog.GetByIndex(index) : StageCatalog.GetById(wanted);
		if (stage == null)
		{
			GD.PushWarning($"SecondEventStandIn: {Flag}={wanted} names no chapter; no stand-in is added.");
			return string.Empty;
		}
		return stage.Id;
	}
}
