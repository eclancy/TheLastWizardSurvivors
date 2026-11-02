using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WizardSurvivors.scripts;

/// <summary>
/// The journal, built once for both places it is shown: the pause screen's Journal tab and the world map's
/// Journal button (JournalScreen). Two pages under one strip - Quests (the log, with Track on every row)
/// and Notes (every chapter note the player has read, on one sheet of paper). See .ai/quests-and-notes.md.
/// </summary>
/// <remarks>
/// It is AchievementsPage's sibling and is made of the same rows (CodexRows, the stone register), because
/// every quest IS an achievement: the log only chooses which to list, words a wizard's by his epithet, and
/// puts a Track button on it. It shows no rewards - the achievements tab says what each pays - so a quest
/// row is its name, one line of what to do and its count. "Keep text minimal."
///
/// The notes are ONE page, not a card each: a stack of paper scraps on a stone panel would be the same
/// material over and over, and a journal is one book.
/// </remarks>
public static class JournalPage
{
	public enum View
	{
		Quests,
		Notes,
	}

	/// <summary>The page last open, remembered for the session; an enum, so a static is safe.</summary>
	public static View Current { get; set; } = View.Quests;

	private static CodexRows.Palette Ink => CodexRows.Stone;

	// After a rebuild the selection goes back to the control it was on, found by name.
	private static string pendingFocus = string.Empty;

	/// <summary>
	/// Builds the journal into <paramref name="host"/>, which must already be in the tree. Pressing Track or
	/// a tab changes the save or the page and calls <paramref name="rebuild"/>, which must clear the host and
	/// call this again; <paramref name="trackedChanged"/> is told after the tracked list changed (the HUD).
	/// <paramref name="currentStageId"/> - the chapter being played, on the pause screen - is listed first.
	/// </summary>
	public static void Build(VBoxContainer host, SaveData save, AchievementContext context, string currentStageId,
		Action rebuild, Action trackedChanged)
	{
		save ??= new SaveData();
		host.AddChild(BuildTabs(rebuild));
		if (Current == View.Notes)
			BuildNotes(host, save, currentStageId);
		else
			BuildQuests(host, save, context, currentStageId, rebuild, trackedChanged);

		if (!string.IsNullOrEmpty(pendingFocus))
		{
			string name = pendingFocus;
			pendingFocus = string.Empty;
			Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(host) && host.FindChild(name, true, false) is Control again && again.IsVisibleInTree())
					again.GrabFocus();
			}).CallDeferred();
		}
	}

	/// <summary>The tab the page opens on, for a screen's initial selection.</summary>
	public static Control TabFor(Node host, View view) => host?.FindChild("JournalTab" + view, true, false) as Control;

	// --- the strip --------------------------------------------------------------------------------

	private static HBoxContainer BuildTabs(Action rebuild)
	{
		var bar = new HBoxContainer
		{
			Name = "JournalTabs",
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		bar.AddThemeConstantOverride("separation", 6);
		var group = new ButtonGroup();
		foreach (View view in Enum.GetValues<View>())
		{
			var tab = new Button
			{
				Name = "JournalTab" + view,
				Text = view == View.Quests ? "Quests" : "Notes",
				ToggleMode = true,
				ButtonGroup = group,
				ButtonPressed = view == Current,
				FocusMode = Control.FocusModeEnum.All,
				ProcessMode = Node.ProcessModeEnum.Always,
				CustomMinimumSize = new Vector2(150f, ResponsiveLayout.MinTouchTarget),
			};
			ResponsiveLayout.SetFont(tab, ResponsiveLayout.TextRole.Title);
			CodexRows.StyleTab(tab, Ink);
			View mine = view;
			tab.Pressed += () =>
			{
				if (Current == mine)
					return;
				Current = mine;
				SfxPlayer.Global(SfxCatalog.UiClick);
				pendingFocus = "JournalTab" + mine;
				rebuild?.Invoke();
			};
			bar.AddChild(tab);
		}
		// The ends stop, as the achievements strip's do, rather than carrying the stick into a row.
		if (bar.GetChild(0) is Button first)
			first.FocusNeighborLeft = first.GetPathTo(first);
		if (bar.GetChild(bar.GetChildCount() - 1) is Button last)
			last.FocusNeighborRight = last.GetPathTo(last);
		return bar;
	}

	// --- quests -----------------------------------------------------------------------------------

	private static void BuildQuests(VBoxContainer host, SaveData save, AchievementContext context, string currentStageId,
		Action rebuild, Action trackedChanged)
	{
		QuestLog.Prune(save);
		QuestLog.Model model = QuestLog.BuildModel(save, context, currentStageId);
		int columns = CodexRows.ColumnsFor(host);
		var list = CodexRows.BuildList("JournalQuests");
		host.AddChild(list);

		List<Quest> tracked = QuestLog.Tracked(save, context);
		list.AddChild(CodexRows.BuildSummary(
			tracked.Count == 0 ? "Nothing tracked. Track a quest to see it during a run."
				: $"{tracked.Count} of {QuestLog.MaxTracked} tracked  ·  shown during a run", Ink));

		void Section(string title, string count, IEnumerable<Control> rows)
		{
			var built = rows.ToList();
			if (built.Count > 0)
				CodexRows.AddSection(list, title, count, Ink, columns, built);
		}

		Control Row(Quest quest, string tag = "") => QuestRow(quest, save, tag, rebuild, trackedChanged);

		Section("Tracked", $"{tracked.Count} of {QuestLog.MaxTracked}", tracked.Select(q => Row(q)));

		var wizards = new List<Control>();
		if (model.Next != null)
			wizards.Add(Row(model.Next, "Next"));
		else
			wizards.Add(CodexRows.BuildRow(new CodexRows.Entry
			{
				Title = "Every wizard is free",
				Quiet = true,
				FocusStop = true,
			}, Ink));
		wizards.AddRange(model.Wizards.Select(q => Row(q)));
		wizards.AddRange(model.Freed.Select(q => Row(q)));
		Section("Wizards", model.Freed.Count > 0 ? $"{model.Freed.Count} freed" : string.Empty, wizards);

		if (!string.IsNullOrEmpty(currentStageId))
			Section("This chapter", StageCatalog.DisplayNameFor(currentStageId), model.ThisChapter.Select(q => Row(q)));
		Section("Chapters", model.ChaptersDone > 0 ? $"{model.ChaptersDone} done" : string.Empty, model.Chapters.Select(q => Row(q)));
		Section("Feats", model.FeatsDone > 0 ? $"{model.FeatsDone} done" : string.Empty, model.Feats.Select(q => Row(q)));
	}

	private static Control QuestRow(Quest quest, SaveData save, string tag, Action rebuild, Action trackedChanged)
	{
		var entry = new CodexRows.Entry
		{
			Title = quest.Title,
			Body = quest.Done ? string.Empty : quest.Step,
			Quiet = quest.Done,
			Tag = tag,
			Icon = WizardGlyph(quest, save),
		};
		if (quest.Progress != null && !quest.Done)
		{
			entry.Progress = quest.Progress.Fraction;
			entry.ProgressText = quest.Progress.Display;
		}
		if (!string.IsNullOrEmpty(quest.StageId) && !quest.Done)
			entry.Meta = StageCatalog.DisplayNameFor(quest.StageId);
		if (quest.Done)
		{
			entry.FocusStop = true;
			if (!string.IsNullOrEmpty(quest.WizardId))
				entry.Body = UnlockCatalog.GetLockedHint(quest.WizardId, UnlockKind.Character);
		}
		else
		{
			entry.Trailing = TrackButton(quest.Id, save, rebuild, trackedChanged);
		}
		return CodexRows.BuildRow(entry, Ink);
	}

	// A quest that frees someone carries his shape: the sealed blank while the road has not reached him,
	// his silhouette while he is held, his portrait once he is out - the character select's three states.
	private static Texture2D WizardGlyph(Quest quest, SaveData save)
	{
		if (string.IsNullOrEmpty(quest.WizardId))
			return null;
		// On the road, or one sealed until he is free (the dark wizard, UnlockDefinition.SealedUntilFreed): the seal.
		if (quest.OnTheRoad || (UnlockCatalog.GetCharacter(quest.WizardId)?.SealedUntilFreed == true
			&& !GlobalStatsManager.IsCharacterUnlocked(save, quest.WizardId)))
			return BonelightSkin.LoadTextureSafe("res://assets/bonelight/characters/sealed-portrait.png");
		Texture2D portrait = CodexRows.WizardPortrait(quest.WizardId);
		if (portrait == null)
			return null;
		return GlobalStatsManager.IsCharacterUnlocked(save, quest.WizardId) ? portrait : CodexRows.BuildSilhouette(portrait, Ink);
	}

	private static Button TrackButton(string id, SaveData save, Action rebuild, Action trackedChanged)
	{
		bool tracked = QuestLog.IsTracked(save, id);
		var button = new Button
		{
			Name = "Track_" + id,
			Text = tracked ? "Tracked" : "Track",
			FocusMode = Control.FocusModeEnum.All,
			ProcessMode = Node.ProcessModeEnum.Always,
			TooltipText = tracked ? "Stop showing this during a run" : "Show this during a run",
		};
		ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Label);
		CodexRows.StyleQuietToggle(button, Ink);
		if (tracked)
		{
			// Tracked reads in the reward gold with the done mark, so a list of rows says at a glance which.
			button.AddThemeColorOverride("font_color", Ink.Reward);
			button.AddThemeColorOverride("font_focus_color", Ink.Reward);
			button.Icon = StatIcons.Load("done");
			button.ExpandIcon = false;
		}
		ResponsiveLayout.EnsureTouchTarget(button, 120f);
		button.Pressed += () =>
		{
			if (!QuestLog.SetTracked(save, id, !tracked))
			{
				// Full: say so on the button, and leave the list as it is.
				SfxPlayer.Global(SfxCatalog.UiDenied);
				button.Text = $"{QuestLog.MaxTracked} at most";
				return;
			}
			SfxPlayer.Global(SfxCatalog.UiClick);
			Persist();
			trackedChanged?.Invoke();
			pendingFocus = button.Name;
			rebuild?.Invoke();
		};
		return button;
	}

	// Tracking is the player's choice about the save, made outside a run as often as in one: written now,
	// as an event's win is. SaveGame does nothing while writes are off (every tool scene).
	private static void Persist()
	{
		if (Engine.GetMainLoop() is SceneTree tree && tree.Root.GetNodeOrNull<SaveManager>("/root/SaveManager") is SaveManager manager)
			manager.SaveGame();
	}

	// --- notes ------------------------------------------------------------------------------------

	private static void BuildNotes(VBoxContainer host, SaveData save, string currentStageId)
	{
		List<StageDefinition> chapters = ChapterNotes.JournalChapters(save);
		// The chapter being played leads; the rest follow the road.
		StageDefinition here = chapters.FirstOrDefault(s => s.Id.Equals(currentStageId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		if (here != null)
		{
			chapters.Remove(here);
			chapters.Insert(0, here);
		}

		var page = new PanelContainer { Name = "JournalNotes", MouseFilter = Control.MouseFilterEnum.Ignore };
		var paper = new StyleBoxFlat { BgColor = CodexRows.Vellum.Face, BorderColor = CodexRows.Vellum.Edge };
		paper.SetBorderWidthAll(2);
		paper.SetCornerRadiusAll(3);
		paper.ContentMarginLeft = 30;
		paper.ContentMarginRight = 30;
		paper.ContentMarginTop = 22;
		paper.ContentMarginBottom = 26;
		page.AddThemeStyleboxOverride("panel", paper);
		host.AddChild(page);

		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 22);
		page.AddChild(column);

		if (chapters.Count == 0)
		{
			column.AddChild(Hand("Nothing written yet.", CodexRows.Vellum.InkMeta));
			// Something for a pad to stand on, so the page is not a dead end.
			column.AddChild(FocusStop(new Control { CustomMinimumSize = new Vector2(0f, 8f) }));
			return;
		}

		var hand = HandFont();
		foreach (StageDefinition stage in chapters)
		{
			var block = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			block.AddThemeConstantOverride("separation", 4);
			var heading = new Label { Text = stage.DisplayName, MouseFilter = Control.MouseFilterEnum.Ignore };
			ResponsiveLayout.SetFont(heading, ResponsiveLayout.TextRole.Title);
			heading.AddThemeFontSizeOverride("font_size", ResponsiveLayout.FontSize(host, ResponsiveLayout.TextRole.Body));
			heading.AddThemeColorOverride("font_color", CodexRows.Vellum.Heading);
			block.AddChild(heading);
			foreach (string line in ChapterNotes.LinesFor(stage.Id, save))
			{
				Label written = Hand(line, CodexRows.Vellum.Ink);
				written.AddThemeFontOverride("font", hand);
				block.AddChild(written);
			}
			column.AddChild(FocusStop(block));
		}
	}

	private static Label Hand(string text, Color ink)
	{
		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		ResponsiveLayout.SetFont(label, ResponsiveLayout.TextRole.Body);
		label.AddThemeColorOverride("font_color", ink);
		return label;
	}

	/// <summary>The note hand: the engine sans slanted by a fifth (ChapterNoteCard's).</summary>
	private static FontVariation HandFont() => new()
	{
		BaseFont = ThemeDB.FallbackFont,
		VariationTransform = new Transform2D(1f, 0.2f, 0f, 1f, 0f, 0f),
	};

	// A controller scrolls a page by moving the selection through it, so each chapter's note is a stop -
	// the focus-only overlay CodexRows gives a row with no control of its own.
	private static Control FocusStop(Control content)
	{
		var holder = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		holder.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		Button overlay = BonelightSkin.MakeCardClickOverlay(true);
		overlay.Name = "NoteFocus";
		overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		holder.AddChild(overlay);
		holder.AddChild(content);
		return holder;
	}
}
