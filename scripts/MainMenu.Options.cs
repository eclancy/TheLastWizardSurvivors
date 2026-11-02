using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WizardSurvivors.scripts;

/// <summary>
/// The main menu's Options panel: Audio, Display, Gameplay, Story, Testing, then Reset Progress on
/// its own at the foot.
/// </summary>
/// <remarks>
/// Rebuilt in the menu pass of 2026-11-01 (Eric: "give more space and organization to menu options,
/// etc for useability. Remove information not needed, remove first session checklist"). It had been
/// one undivided list in Godot's default controls, grown a row at a time by eleven Ensure* helpers
/// that each found their place by counting from the Back button - three volume bars, a run of
/// switches, the prologue, a dropdown, a "Playtest Toolkit", a five-item First Session Checklist
/// that ticked itself from the run history, and Reset Progress - and on a phone it ran on for two
/// screens with nothing to say where one kind of setting stopped and the next began.
///
/// It is now the Achievements panel's shape: a scroll of CodexRows sections under the heading, Back
/// pinned outside the scroll (a Back button that scrolls away is a dead end). The Audio and Display
/// sections are SettingsRows', which the pause screen's Options tab builds too, so the two cannot
/// drift apart. The checklist is gone; its save key (SaveData.PlaytestChecklistState) stays where
/// it is, unread, because removing a key from SaveData.SettingsKeys is a migration for no gain.
/// </remarks>
public partial class MainMenu
{
	private const string OptionsVBoxPath = "MarginContainer/VBoxContainer/Content/OptionsPanel/OptionsVBox";

	// Casual, Default, Hardcore, in the order the choice row shows them.
	private static readonly string[] DifficultyIds =
	{
		GlobalStatsManager.BalancePresetCasual,
		GlobalStatsManager.BalancePresetDefault,
		GlobalStatsManager.BalancePresetHardcore,
	};

	private GridContainer optionsColumns;
	private CheckButton playtestModeToggle;
	private List<Button> difficultyButtons = new();
	private Label runLogStatus;
	private Control runLogStatusRow;

	// Two-press reset, no dialog: the first press arms it and relabels, the second wipes. An accidental
	// single press must never be able to destroy a save.
	private Button resetProgressButton;
	private bool resetArmed;

	// The danger ink: a warm red on the unlit stone frame, measured in tools/art/ui_contrast.py
	// (options_pairs). COPIED there as DANGER_INK: change one, change both.
	private static readonly Color DangerInk = new(0.949f, 0.541f, 0.502f);

	private void BuildOptions()
	{
		var optionsVBox = GetNode<VBoxContainer>(OptionsVBoxPath);
		CodexRows.Palette ink = CodexRows.Stone;
		SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;

		// Back sits under the list at its own size, as on the Spell Index and Achievements, rather
		// than stretched across the whole panel.
		backFromOptionsButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		ResponsiveLayout.SetFont(backFromOptionsButton, ResponsiveLayout.TextRole.Title);

		VBoxContainer list = CodexRows.BuildList("OptionsList");
		ScrollContainer scroll = CodexRows.BuildScroll("OptionsScroll", list);
		optionsVBox.AddChild(scroll);
		optionsVBox.MoveChild(scroll, backFromOptionsButton.GetIndex());

		// Two columns of sections on a PC, one on a phone (CodexRows.ColumnsFor, kept by
		// OnViewportSizeChanged): the sound and the screen on the left, how the game plays on the
		// right. In one column the landscape canvas showed a section and a half at a time, with the
		// rest behind the scroll bar; in two, most of the panel opens in view and the rest is a short
		// scroll. On a phone the grid's two cells stack, so the order there is the same, top to bottom.
		optionsColumns = new GridContainer
		{
			Name = "OptionsColumns",
			Columns = CodexRows.ColumnsFor(this),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		optionsColumns.AddThemeConstantOverride("h_separation", 40);
		optionsColumns.AddThemeConstantOverride("v_separation", CodexRows.SectionSeparation);
		list.AddChild(optionsColumns);
		VBoxContainer left = CodexRows.BuildList("OptionsLeft");
		VBoxContainer right = CodexRows.BuildList("OptionsRight");
		optionsColumns.AddChild(left);
		optionsColumns.AddChild(right);

		List<Control> audio = SettingsRows.AudioRows(this, ink);
		List<Control> display = SettingsRows.DisplayRows(this, ink);
		CodexRows.AddSection(left, SettingsRows.Audio, null, ink, 1, audio);
		CodexRows.AddSection(left, SettingsRows.Display, null, ink, 1, display);

		// FEEDBACK: Report a bug, which stood at the foot of the main menu beside Options and the Codex
		// until Eric moved it here (2026-11-02: "Report a bug can be in options").
		reportBugButton = SettingsRows.BuildActionButton("Report a bug", "ReportBugButton");
		reportBugButton.Pressed += OnReportBugPressed;
		var feedback = new List<Control> { SettingsRows.BuildButtonRow(reportBugButton) };
		CodexRows.AddSection(left, SettingsRows.Feedback, null, ink, 1, feedback);

		// GAMEPLAY: what changes how a run plays. The difficulty was a dropdown called "Run Difficulty
		// Curve", which the root menu's corner called "Preset" - one name now, the one a player would use.
		// The Gameplay Tips switch is gone (Eric, 2026-11-02): it only ever mattered until the onboarding
		// had been seen once, so the tips simply play for a new player and stop.
		Control difficulty = SettingsRows.BuildChoiceRow("Difficulty", new[] { "Casual", "Default", "Hardcore" },
			DifficultyIndex(save?.BalancePresetId), ink, OnDifficultyChosen, out difficultyButtons, "Difficulty");
		var gameplay = new List<Control> { difficulty };
		CodexRows.AddSection(right, SettingsRows.Gameplay, null, ink, 1, gameplay);

		// STORY: the prologue again, on demand, and back to this menu after (.ai/prologue.md).
		Button prologue = SettingsRows.BuildActionButton("Watch the Prologue", "WatchPrologueButton");
		prologue.Pressed += () => Prologue.Play(this, Prologue.After.Menu);
		var story = new List<Control> { SettingsRows.BuildButtonRow(prologue) };
		// And every lesson this save has been taught (Teachings, .ai/slow-start.md), in the order it learns them,
		// each back to this menu. One a row: "The Sovereign Rite" does not share a phone's width with another.
		// A lesson not yet taught is not listed - its name alone would be a system shown before its time.
		foreach (Teachings.Lesson lesson in Teachings.All.Where(l => Teachings.HasBeenTaught(save, l) && save != null))
		{
			string reel = lesson.Id;
			Button replay = SettingsRows.BuildActionButton($"Lesson: {lesson.Name}", "WatchLesson_" + lesson.Id);
			replay.Pressed += () => Prologue.Play(this, Prologue.After.Menu, reel);
			story.Add(SettingsRows.BuildButtonRow(replay));
		}
		CodexRows.AddSection(right, SettingsRows.Story, null, ink, 1, story);

		// TESTING: the tester's knobs, below everything a player sets. Playtest Mode was "Enable Playtest
		// Mode (faster XP + 4 options per level-up)"; the heading now says what kind of switch it is.
		playtestModeToggle = SettingsRows.BuildSwitch("Playtest Mode", save?.PlaytestModeEnabled ?? false,
			ink, OnPlaytestModeToggled, "PlaytestModeToggle");
		Button latestLog = SettingsRows.BuildActionButton("Open Latest Log", "OpenLatestPlaytestLogButton");
		latestLog.Pressed += OnOpenLatestPlaytestLogPressed;
		Button logFolder = SettingsRows.BuildActionButton("Open Log Folder", "OpenPlaytestLogFolderButton");
		logFolder.Pressed += OnOpenPlaytestLogFolderPressed;
		// Says something only when a press needs an answer ("No run logs yet"), and is hidden until then.
		runLogStatus = CodexRows.MakeText(string.Empty, ResponsiveLayout.TextRole.Label, ink.InkMeta);
		runLogStatus.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		runLogStatusRow = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
		runLogStatusRow.AddThemeConstantOverride("margin_left", 8);
		runLogStatusRow.AddChild(runLogStatus);
		var testing = new List<Control> { playtestModeToggle, SettingsRows.BuildButtonRow(latestLog, logFolder), runLogStatusRow };
		// Only where a tester is: never in a release build (SettingsRows.TestingAvailable).
		if (SettingsRows.TestingAvailable)
			CodexRows.AddSection(right, SettingsRows.Testing, null, ink, 1, testing);
		else
			testing.Clear();

		list.AddChild(BuildResetProgress(ink));

		optionsLeftRows = audio.Concat(display).Concat(feedback).Select(FocusStopsIn).Where(row => row.Count > 0).ToList();
		optionsRightRows = gameplay.Concat(story).Concat(testing).Select(FocusStopsIn).Where(row => row.Count > 0).ToList();
		ChainOptionsFocus();
	}

	// The selection walks each column top to bottom and then to Reset Progress. Left to Godot's own
	// geometric search it did not: down from Mute All went across to Watch the Prologue, which sat a
	// little higher than Damage Numbers in the other column, and down from there came back. Left and
	// right still cross between the columns by geometry (a slider keeps them, to move itself).
	private List<List<Control>> optionsLeftRows = new();
	private List<List<Control>> optionsRightRows = new();

	private void ChainOptionsFocus()
	{
		if (optionsColumns == null || resetProgressButton == null)
			return;
		// In one column (a phone) the right-hand sections follow the left-hand ones.
		List<List<List<Control>>> columns = optionsColumns.Columns > 1
			? new() { optionsLeftRows, optionsRightRows }
			: new() { optionsLeftRows.Concat(optionsRightRows).ToList() };
		foreach (List<List<Control>> rows in columns)
		{
			for (int i = 0; i < rows.Count; i++)
			{
				foreach (Control stop in rows[i])
				{
					stop.FocusNeighborBottom = stop.GetPathTo(i + 1 < rows.Count ? rows[i + 1][0] : resetProgressButton);
					stop.FocusNeighborTop = i > 0 ? stop.GetPathTo(rows[i - 1][0]) : new NodePath();
				}
			}
		}
	}

	// The controls a row can be selected by, in tree order: a switch is its own, a slider row's is its
	// slider, a row of buttons has one per button.
	private static List<Control> FocusStopsIn(Control row)
	{
		var stops = new List<Control>();
		void Walk(Node node)
		{
			if (node is Control control && control.FocusMode == Control.FocusModeEnum.All
				&& (control is BaseButton || control is Slider))
				stops.Add(control);
			foreach (Node child in node.GetChildren())
				Walk(child);
		}
		Walk(row);
		return stops;
	}

	/// <summary>Opens Options on its first row (the master volume), at the top of the list.</summary>
	private void FocusFirstOption()
	{
		if (optionsLeftRows.Count > 0 && optionsLeftRows[0][0].IsVisibleInTree())
			optionsLeftRows[0][0].GrabFocus();
		// After the grab, and deferred: the panel has only just been shown, so FollowFocus measures the
		// slider before the list is laid out and scrolled the headings 14 units off the top. Initial
		// focus must not scroll (.ai/controller-and-focus.md); the first row is in view at 0 anyway.
		if (optionsPanel?.FindChild("OptionsScroll", true, false) is ScrollContainer scroll)
			Callable.From(() =>
			{
				if (IsInstanceValid(scroll))
					scroll.ScrollVertical = 0;
			}).CallDeferred();
	}

	// Reset Progress lives INSIDE Options, not on the main menu, and last in it: the end of a settings
	// list is where a destructive action is expected, and the press that arms it should not be one the
	// player can reach by accident on the way to Start Run. Alone under a rule of its own, at its own
	// width, in the danger ink - it used to be a faint full-width bar that read as disabled.
	private Control BuildResetProgress(CodexRows.Palette ink)
	{
		var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		box.AddThemeConstantOverride("separation", 20);
		box.AddChild(new ColorRect { Color = ink.Rule, CustomMinimumSize = new Vector2(0, 2), MouseFilter = Control.MouseFilterEnum.Ignore });

		resetProgressButton = new Button
		{
			Name = "ResetProgressButton",
			Text = "Reset Progress",
			FocusMode = Control.FocusModeEnum.All,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			CustomMinimumSize = new Vector2(320, 0),
		};
		BonelightSkin.StyleButton(resetProgressButton, BonelightSkin.Emphasis.Quiet);
		ResponsiveLayout.SetFont(resetProgressButton, ResponsiveLayout.TextRole.Body);
		foreach (string state in new[] { "font_color", "font_hover_color", "font_focus_color", "font_pressed_color" })
			resetProgressButton.AddThemeColorOverride(state, DangerInk);
		ResponsiveLayout.EnsureTouchTarget(resetProgressButton);
		resetProgressButton.Pressed += OnResetProgressPressed;
		// Armed is a state of THIS moment: walking off the button puts the safety back on.
		resetProgressButton.FocusExited += DisarmReset;
		box.AddChild(resetProgressButton);
		return box;
	}

	private void DisarmReset()
	{
		resetArmed = false;
		if (resetProgressButton != null)
			resetProgressButton.Text = "Reset Progress";
	}

	private void OnResetProgressPressed()
	{
		var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager == null || resetProgressButton == null)
			return;

		if (!resetArmed)
		{
			resetArmed = true;
			resetProgressButton.Text = "Press again to erase everything";
			return;
		}

		resetArmed = false;
		if (!saveManager.ResetProgress())
		{
			// The old save could not be kept, so nothing was reset (SaveManager.ResetProgress).
			resetProgressButton.Text = "Could not back up the save";
			return;
		}
		resetProgressButton.Text = "Reset Progress";

		// Everything on this screen is a view over the save, so all of it is now stale - the switches
		// in this panel included, which a new save puts back to its defaults.
		RefreshArcaneEnergy();
		RefreshUpgradeControls();
		RefreshAchievementCards();
		SyncOptionsFromSave();
	}

	private void SyncOptionsFromSave()
	{
		SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		if (save == null)
			return;
		playtestModeToggle?.SetPressedNoSignal(save.PlaytestModeEnabled);
		// SetPressedNoSignal does not consult the ButtonGroup, so every button is set.
		int chosen = DifficultyIndex(save.BalancePresetId);
		for (int i = 0; i < difficultyButtons.Count; i++)
			difficultyButtons[i].SetPressedNoSignal(i == chosen);
	}

	private static int DifficultyIndex(string presetId)
	{
		int index = System.Array.IndexOf(DifficultyIds, GlobalStatsManager.NormalizeBalancePresetId(presetId));
		return index >= 0 ? index : 1;
	}

	private void OnPlaytestModeToggled(bool enabled)
	{
		var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager == null)
			return;

		saveManager.Data.PlaytestModeEnabled = enabled;
		saveManager.SaveGame();
	}

	private void OnDifficultyChosen(int index)
	{
		var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager == null || index < 0 || index >= DifficultyIds.Length)
			return;

		saveManager.Data.BalancePresetId = DifficultyIds[index];
		saveManager.SaveGame();
		// The root menu's corner line names the difficulty and the next run's reward.
		RefreshArcaneEnergy();
	}

	private void ShowRunLogStatus(string text)
	{
		if (runLogStatus == null || runLogStatusRow == null)
			return;
		runLogStatus.Text = text;
		runLogStatusRow.Visible = !string.IsNullOrEmpty(text);
	}

	private void OnOpenLatestPlaytestLogPressed()
	{
		string logsDir = ProjectSettings.GlobalizePath("user://playtest_logs");
		string latestLogPath = Directory.Exists(logsDir)
			? Directory.GetFiles(logsDir, "run_*.txt").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
			: null;
		if (string.IsNullOrWhiteSpace(latestLogPath))
		{
			ShowRunLogStatus("No run logs yet. Finish a run first.");
			return;
		}

		OS.ShellOpen($"file:///{latestLogPath.Replace('\\', '/')}");
		ShowRunLogStatus($"Opened {Path.GetFileName(latestLogPath)}");
	}

	private void OnOpenPlaytestLogFolderPressed()
	{
		string logsDir = ProjectSettings.GlobalizePath("user://playtest_logs");
		Directory.CreateDirectory(logsDir);
		OS.ShellOpen($"file:///{logsDir.Replace('\\', '/')}");
		ShowRunLogStatus(string.Empty);
	}
}
