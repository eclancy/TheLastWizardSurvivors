using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

/// <summary>
/// THE EVOLUTION CODEX (.ai/evolution-codex.md, approved by Eric 2026-11-02): one page for every spell,
/// item, evolution and recipe. It replaced the level-up screen's Ascensions browser and the main menu's
/// Spell Index, from a player's request (Amy, 2026-11-02) that the progression stop being put together
/// from three places.
/// </summary>
/// <remarks>
/// One screen, two modes (CodexModel): in a run the states are the run's, on the main menu the save's.
/// A CanvasLayer of its own over whatever opened it - the level-up screen, the pause screen or the main
/// menu - added to the tree's root so it is never inside the opener's menu: the opener's navigator sees the
/// selection is elsewhere and stands down, and this one's takes it (MenuNavigator, "screens stack"). It
/// reads, it never grants: nothing here takes an Ascension or makes a recipe. The one thing it changes is
/// the save's Set aside (a Redaction), which the Spell Index carried and the Codex keeps, in collection mode.
///
/// THE LAYOUTS (section 2). A PC is the grid on the left, 6 across, and the detail of the selected entry on
/// the right, which follows the selection; a phone is the grid alone, 5 across, and a tap opens the detail
/// as a full sheet over it. The header never scrolls; only the grid does, and only when the filter shows
/// more than fit. The tree never scrolls on a PC by construction (CodexModel's bounds, ValidateCodex).
///
/// THE CONTROLLER (section 2). The grid is home: the stick moves the selection, the detail follows. A on a
/// PC moves into the tree, where the stick walks the nodes, and B steps back out, then closes. LB and RB
/// cycle the filters. Y - or A on the search field - opens the on-screen keyboard (CodexKeyboard).
/// </remarks>
public partial class EvolutionCodex : CanvasLayer
{
	/// <summary>Above the level-up screen (10), the chest (100) and the set detail (110); under an element's card (150).</summary>
	public const int ScreenLayer = 120;

	public const string Title = "Evolution Codex";

	/// <summary>True while a Codex is open: the screens under it stand down from their own pad buttons.</summary>
	public static bool IsOpen { get; private set; }

	private CodexContext context = new();
	private CodexModel model;
	private CodexFilter filter = CodexFilter.All;
	private string query = string.Empty;
	private string selectedId = string.Empty;
	/// <summary>A node a search hit lit inside the selected entry (an evolution, an ingredient).</summary>
	private string litNodeId = string.Empty;
	/// <summary>The form tab the detail shows, for a spell taken in forms; -1 for the entry's own.</summary>
	private int formShown = -1;
	private Action closed;
	private bool narrow;
	private bool built;

	private Control root = null!;
	private PanelContainer panel = null!;
	private LineEdit searchField = null!;
	private Label countLabel = null!;
	private Button backButton = null!;
	private readonly List<Button> tabs = new();
	private ScrollContainer gridScroll = null!;
	private VBoxContainer gridList = null!;
	/// <summary>Where the detail is written: the right panel on a PC, the sheet on a phone.</summary>
	private VBoxContainer detail = null!;
	private ScrollContainer detailScroll = null!;
	/// <summary>The phone's full sheet over the grid; null on a PC.</summary>
	private Control sheet;
	private Label sheetTitle;
	private CodexKeyboard keyboard;
	private readonly Dictionary<string, Button> cells = new();
	private readonly Dictionary<string, PanelContainer> frames = new();

	/// <summary>
	/// Opens the Codex over <paramref name="opener"/>'s screen.
	/// </summary>
	/// <param name="select">An entry id, or an evolution's id (its spell is selected and the node lit).</param>
	/// <param name="onClosed">Called once it has closed, so the opener can put the selection back on its button.</param>
	public static EvolutionCodex Open(Node opener, CodexContext context, string select = null,
		CodexFilter filter = CodexFilter.All, Action onClosed = null)
	{
		var screen = new EvolutionCodex
		{
			Name = "EvolutionCodex",
			context = context ?? CodexContext.ForSave(opener?.GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data),
			filter = filter,
			selectedId = select ?? string.Empty,
			closed = onClosed,
		};
		opener.GetTree().Root.AddChild(screen);
		return screen;
	}

	/// <summary>The model on screen (read by _UiShot and the validators).</summary>
	public CodexModel Model => model;
	public CodexFilter Filter => filter;
	public string SelectedId => selectedId;
	public bool KeyboardOpen => keyboard != null && keyboard.Visible;
	public bool SheetOpen => sheet != null && sheet.Visible;

	// The harness's hands (_UiShot --codex-search, --codex-sheet, --codex-keyboard): each does what the
	// player's own press would, through the same method.
	public void DebugSearch(string text)
	{
		SetQuery(text);
		if (cells.TryGetValue(selectedId, out Button cell) && IsInstanceValid(cell))
			cell.GrabFocus();
	}

	public void DebugOpenSheet() => OpenSheet();

	public void DebugOpenKeyboard() => OpenKeyboard();

	public override void _Ready()
	{
		IsOpen = true;
		Layer = ScreenLayer;
		ProcessMode = ProcessModeEnum.Always;
		model = CodexModel.Build(context);
		ResolveSelection(selectedId);
		Build();
		SfxPlayer.Global(SfxCatalog.UiOpen);
		GetViewport().SizeChanged += OnViewportSizeChanged;
	}

	public override void _ExitTree()
	{
		IsOpen = false;
		if (GetViewport() is Viewport viewport)
			viewport.SizeChanged -= OnViewportSizeChanged;
	}

	public override void _Notification(int what)
	{
		// Android's back steps back exactly as B does: the sheet, then the Codex.
		if (what == NotificationWMGoBackRequest)
			StepBack();
	}

	private void OnViewportSizeChanged()
	{
		if (!built || ResponsiveLayout.IsNarrow(this) == narrow)
			return;
		// A window dragged across the phone/PC line: rebuilt whole in the other shape, on the same entry.
		Build();
	}

	// ---- building the screen ------------------------------------------------------------------------

	private void Build()
	{
		foreach (Node child in GetChildren())
		{
			if (child is MenuNavigator)
				continue;
			RemoveChild(child);
			child.QueueFree();
		}
		cells.Clear();
		frames.Clear();
		tabs.Clear();
		sheet = null;
		keyboard = null;
		narrow = ResponsiveLayout.IsNarrow(this);

		root = new Control { Name = "CodexRoot", MouseFilter = Control.MouseFilterEnum.Stop };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		var dim = new ColorRect { Color = new Color(0.025f, 0.03f, 0.045f, 0.92f), MouseFilter = Control.MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(dim);

		panel = new PanelContainer { Name = "CodexPanel", MouseFilter = Control.MouseFilterEnum.Ignore };
		panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		panel.OffsetLeft = 12f;
		panel.OffsetRight = -12f;
		panel.OffsetTop = 12f;
		panel.OffsetBottom = -12f;
		panel.AddThemeStyleboxOverride("panel", BonelightSkin.PanelStyle(BonelightSkin.Register.Stone));
		root.AddChild(panel);

		var column = new VBoxContainer { Name = "CodexColumn", MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", narrow ? 10 : 8);
		panel.AddChild(column);

		column.AddChild(BuildHeader());
		if (narrow)
			column.AddChild(BuildSearchField(expand: true));
		column.AddChild(BuildFilterRow());

		gridList = new VBoxContainer { Name = "CodexGrid", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		gridList.AddThemeConstantOverride("separation", 14);
		gridScroll = new ScrollContainer
		{
			Name = "CodexGridScroll",
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			FollowFocus = true,
		};
		gridScroll.AddChild(CodexRows.WithGutter(gridList));

		if (narrow)
		{
			gridScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			column.AddChild(gridScroll);
			column.AddChild(BuildPhoneFoot());
		}
		else
		{
			var body = new HBoxContainer { Name = "CodexBody", SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
			body.AddThemeConstantOverride("separation", 14);
			column.AddChild(body);
			gridScroll.CustomMinimumSize = new Vector2(GridWidth(), 0f);
			body.AddChild(gridScroll);
			body.AddChild(BuildDetailPanel());
		}

		if (narrow)
			root.AddChild(BuildSheet());

		keyboard = new CodexKeyboard { Name = "CodexKeyboard", Visible = false };
		keyboard.Typed += OnKeyboardTyped;
		keyboard.Erased += OnKeyboardErased;
		keyboard.Cleared += OnKeyboardCleared;
		keyboard.Done += CloseKeyboard;
		keyboard.SuggestionChosen += OnSuggestionChosen;
		root.AddChild(keyboard);

		built = true;
		RebuildGrid();
		if (!narrow)
			ShowDetail();
		UpdateCount();

		SafeArea.Apply(root);
		MenuNavigator.Attach(this, InitialFocus());
		// A selection made by the opener (a search hit, a recipe) is scrolled to once the grid has a size.
		Callable.From(ScrollToSelected).CallDeferred();
	}

	// The grid's width on a PC: six cells and their gaps, the scroll gutter, a little air.
	private float GridWidth() => 6f * CellWidth + 5f * CellGap + 24f;

	// A phone: five across inside the 720 canvas less the safe area, the panel margins and the gutter.
	private float CellWidth => narrow ? 108f : 80f;
	private float FrameSide => narrow ? 88f : 76f;
	private const float CellGap = 6f;

	private Control BuildHeader()
	{
		// The heading band every stone screen wears (the pause screen's, JournalScreen's): stone deep, a gold rule.
		var band = new PanelContainer { Name = "CodexHeader", MouseFilter = Control.MouseFilterEnum.Ignore };
		var style = new StyleBoxFlat { BgColor = CodexInk.StoneDeep, BorderColor = CodexInk.BandRule };
		style.BorderWidthBottom = 2;
		style.SetCornerRadiusAll(2);
		style.ContentMarginLeft = 16;
		style.ContentMarginRight = 8;
		style.ContentMarginTop = 4;
		style.ContentMarginBottom = 6;
		band.AddThemeStyleboxOverride("panel", style);
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 16);
		band.AddChild(row);

		var title = new Label
		{
			Name = "CodexTitle",
			Text = Title,
			VerticalAlignment = VerticalAlignment.Center,
			CustomMinimumSize = new Vector2(0f, ResponsiveLayout.MinTouchTarget),
			SizeFlagsHorizontal = narrow ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkBegin,
		};
		ResponsiveLayout.SetFont(title, narrow ? ResponsiveLayout.TextRole.Title : ResponsiveLayout.TextRole.Display);
		title.AddThemeColorOverride("font_color", CodexRows.Stone.Heading);
		row.AddChild(title);

		if (!narrow)
			row.AddChild(BuildSearchField(expand: true));

		countLabel = new Label
		{
			Name = "CodexCount",
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		ResponsiveLayout.SetFont(countLabel, ResponsiveLayout.TextRole.Label);
		countLabel.AddThemeColorOverride("font_color", CodexRows.Stone.InkSoft);
		row.AddChild(countLabel);

		if (!narrow)
		{
			backButton = BuildBackButton();
			row.AddChild(backButton);
		}
		return band;
	}

	private Button BuildBackButton()
	{
		var back = new Button { Name = "CodexBack", Text = "Back", FocusMode = Control.FocusModeEnum.All };
		ResponsiveLayout.SetFont(back, ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(back, BonelightSkin.Emphasis.Normal);
		ResponsiveLayout.EnsureTouchTarget(back, 120f);
		back.Pressed += Close;
		return back;
	}

	private Control BuildPhoneFoot()
	{
		var foot = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
		backButton = BuildBackButton();
		foot.AddChild(backButton);
		return foot;
	}

	private Control BuildSearchField(bool expand)
	{
		searchField = new LineEdit
		{
			Name = "CodexSearch",
			PlaceholderText = "Search spells, items, recipes",
			Text = query,
			ClearButtonEnabled = true,
			CaretBlink = true,
			// A phone raises its own keyboard for the field; a pad gets ours (CodexKeyboard), through Y.
			VirtualKeyboardEnabled = true,
			SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkBegin,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			CustomMinimumSize = new Vector2(narrow ? 0f : 280f, ResponsiveLayout.MinTouchTarget),
		};
		ResponsiveLayout.SetFont(searchField, ResponsiveLayout.TextRole.Body);
		StyleSearchField(searchField);
		searchField.TextChanged += OnSearchTextChanged;
		return searchField;
	}

	private static void StyleSearchField(LineEdit field)
	{
		StyleBoxFlat Box(Color edge, int width)
		{
			var box = new StyleBoxFlat { BgColor = CodexInk.FieldFace, BorderColor = edge };
			box.SetBorderWidthAll(width);
			box.SetCornerRadiusAll(4);
			box.ContentMarginLeft = 12;
			box.ContentMarginRight = 8;
			box.ContentMarginTop = 4;
			box.ContentMarginBottom = 4;
			return box;
		}
		field.AddThemeStyleboxOverride("normal", Box(CodexRows.Stone.Edge, 2));
		field.AddThemeStyleboxOverride("focus", Box(CodexRows.Stone.Reward, 2));
		field.AddThemeStyleboxOverride("read_only", Box(CodexRows.Stone.Edge, 1));
		field.AddThemeColorOverride("font_color", CodexRows.Stone.Ink);
		field.AddThemeColorOverride("font_placeholder_color", CodexRows.Stone.InkSoft);
		field.AddThemeColorOverride("caret_color", CodexRows.Stone.Reward);
		field.AddThemeColorOverride("selection_color", new Color(CodexRows.Stone.Reward, 0.35f));
	}

	private Control BuildFilterRow()
	{
		var row = new HBoxContainer { Name = "CodexFilters", MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 6);
		var group = new ButtonGroup();
		foreach (CodexFilter f in new[] { CodexFilter.All, CodexFilter.Spells, CodexFilter.Items, CodexFilter.Recipes })
		{
			var tab = new Button
			{
				Name = $"CodexTab{f}",
				Text = f.ToString(),
				ToggleMode = true,
				ButtonGroup = group,
				ButtonPressed = f == filter,
				FocusMode = Control.FocusModeEnum.All,
				SizeFlagsHorizontal = narrow ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkBegin,
			};
			// Cinzel on a PC; on a phone four Cinzel tabs are wider than the panel, and the sans fits.
			ResponsiveLayout.SetFont(tab, narrow ? ResponsiveLayout.TextRole.Body : ResponsiveLayout.TextRole.Title);
			CodexRows.StyleTab(tab, CodexRows.Stone);
			ResponsiveLayout.EnsureTouchTarget(tab, narrow ? 0f : 96f);
			CodexFilter chosen = f;
			tab.Pressed += () => SetFilter(chosen, keepFocusOnTab: true);
			tabs.Add(tab);
			row.AddChild(tab);
		}
		if (!narrow)
		{
			row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore });
			// One quiet line naming the run (section 2): the wizard and his level.
			if (!string.IsNullOrEmpty(model.RunLine))
			{
				Label run = CodexRows.MakeText(model.RunLine, ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkSoft);
				run.Name = "CodexRunLine";
				run.AutowrapMode = TextServer.AutowrapMode.Off;
				row.AddChild(run);
			}
			else
			{
				// The pad's three buttons, said once, where the run line would be.
				Label hint = CodexRows.MakeText("LB/RB filters  ·  Y search", ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkSoft);
				hint.Name = "CodexPadHint";
				hint.AutowrapMode = TextServer.AutowrapMode.Off;
				hint.Visible = Input.GetConnectedJoypads().Count > 0;
				row.AddChild(hint);
			}
		}
		return row;
	}

	private Control BuildDetailPanel()
	{
		var frame = new PanelContainer
		{
			Name = "CodexDetailPanel",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		var style = new StyleBoxFlat { BgColor = new Color(CodexInk.StoneDeep, 0.55f), BorderColor = CodexRows.Stone.QuietEdge };
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(4);
		style.SetContentMarginAll(12);
		frame.AddThemeStyleboxOverride("panel", style);
		detailScroll = new ScrollContainer
		{
			Name = "CodexDetailScroll",
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			FollowFocus = true,
		};
		frame.AddChild(detailScroll);
		detail = new VBoxContainer { Name = "CodexDetail", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		detail.AddThemeConstantOverride("separation", 10);
		detailScroll.AddChild(detail);
		return frame;
	}

	private Control BuildSheet()
	{
		// The phone's detail: a full sheet over the grid (section 2), its tree reading top to bottom.
		var cover = new PanelContainer { Name = "CodexSheet", Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
		cover.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		cover.OffsetLeft = 12f;
		cover.OffsetRight = -12f;
		cover.OffsetTop = 12f;
		cover.OffsetBottom = -12f;
		cover.AddThemeStyleboxOverride("panel", BonelightSkin.PanelStyle(BonelightSkin.Register.Stone));
		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 10);
		cover.AddChild(column);

		var band = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		var style = new StyleBoxFlat { BgColor = CodexInk.StoneDeep, BorderColor = CodexInk.BandRule };
		style.BorderWidthBottom = 2;
		style.SetCornerRadiusAll(2);
		style.ContentMarginLeft = 8;
		style.ContentMarginRight = 12;
		style.ContentMarginTop = 4;
		style.ContentMarginBottom = 6;
		band.AddThemeStyleboxOverride("panel", style);
		column.AddChild(band);
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 12);
		band.AddChild(row);
		var back = new Button { Name = "CodexSheetBack", Text = "‹ Codex", FocusMode = Control.FocusModeEnum.All };
		ResponsiveLayout.SetFont(back, ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(back, BonelightSkin.Emphasis.Normal);
		ResponsiveLayout.EnsureTouchTarget(back, 132f);
		back.Pressed += CloseSheet;
		row.AddChild(back);
		sheetTitle = new Label
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ClipText = true,
			TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
		};
		ResponsiveLayout.SetFont(sheetTitle, ResponsiveLayout.TextRole.Title);
		sheetTitle.AddThemeColorOverride("font_color", CodexRows.Stone.Heading);
		row.AddChild(sheetTitle);

		detailScroll = new ScrollContainer
		{
			Name = "CodexDetailScroll",
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			FollowFocus = true,
		};
		column.AddChild(detailScroll);
		detail = new VBoxContainer { Name = "CodexDetail", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		detail.AddThemeConstantOverride("separation", 12);
		detailScroll.AddChild(CodexRows.WithGutter(detail));
		sheet = cover;
		return cover;
	}

	// ---- the grid -----------------------------------------------------------------------------------

	private void RebuildGrid()
	{
		foreach (Node child in gridList.GetChildren())
		{
			gridList.RemoveChild(child);
			child.QueueFree();
		}
		cells.Clear();
		frames.Clear();

		List<CodexEntry> shown = model.Filtered(filter, query);
		if (shown.Count == 0)
		{
			Label none = CodexRows.MakeText(string.IsNullOrWhiteSpace(query) ? "Nothing here yet." : $"Nothing matches \"{query}\".",
				ResponsiveLayout.TextRole.Body, CodexRows.Stone.InkSoft);
			none.Name = "CodexEmpty";
			gridList.AddChild(none);
		}
		int columns = narrow ? 5 : 6;
		foreach (IGrouping<CodexFilter, CodexEntry> group in shown.GroupBy(e => e.Group))
		{
			List<CodexEntry> all = model.Entries.Where(e => e.Group == group.Key).ToList();
			gridList.AddChild(BuildSectionHead(group.Key, all.Count(e => e.Discovered), all.Count));
			var grid = new GridContainer
			{
				Name = $"CodexCells{group.Key}",
				Columns = columns,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};
			grid.AddThemeConstantOverride("h_separation", (int)CellGap);
			grid.AddThemeConstantOverride("v_separation", 10);
			foreach (CodexEntry entry in group)
				grid.AddChild(BuildCell(entry));
			gridList.AddChild(grid);
		}
		WireGridFocus(columns);
		RefreshSelectionMarks();
	}

	private Control BuildSectionHead(CodexFilter group, int found, int total)
	{
		var head = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		head.AddThemeConstantOverride("separation", 10);
		var heading = new Label { Text = group.ToString(), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Bottom };
		ResponsiveLayout.SetFont(heading, ResponsiveLayout.TextRole.Title);
		heading.AddThemeColorOverride("font_color", CodexRows.Stone.Heading);
		head.AddChild(heading);
		Label tally = CodexRows.MakeText($"{found} / {total}", ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkSoft);
		tally.AutowrapMode = TextServer.AutowrapMode.Off;
		tally.VerticalAlignment = VerticalAlignment.Bottom;
		head.AddChild(tally);
		var box = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		box.AddThemeConstantOverride("separation", 4);
		box.AddChild(head);
		box.AddChild(new ColorRect { Color = CodexRows.Stone.Rule, CustomMinimumSize = new Vector2(0, 2), MouseFilter = Control.MouseFilterEnum.Ignore });
		return box;
	}

	/// <summary>One cell (section 3): the icon on a frame that says its state, the name on one line under it.</summary>
	private Control BuildCell(CodexEntry entry)
	{
		var cell = new VBoxContainer
		{
			Name = $"Cell_{entry.Id}",
			CustomMinimumSize = new Vector2(CellWidth, 0f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		cell.AddThemeConstantOverride("separation", 4);

		var frame = new PanelContainer
		{
			Name = "Frame",
			CustomMinimumSize = new Vector2(FrameSide, FrameSide),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		frame.AddThemeStyleboxOverride("panel", CodexInk.CellFrame(entry.State, selected: false));
		cell.AddChild(frame);

		// The whole frame is one target and one stop; the content over it ignores the mouse.
		Button overlay = BonelightSkin.MakeCardClickOverlay(true, 4);
		overlay.Name = $"CodexCell_{entry.Id}";
		if (!DisplayServer.IsTouchscreenAvailable())
			overlay.TooltipText = entry.ShownName;
		frame.AddChild(overlay);

		var centre = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		frame.AddChild(centre);
		centre.AddChild(CellPicture(entry));

		if (entry.Held && entry.LevelCap > 0 && entry.Kind == CodexKind.Spell && entry.State != CodexState.Taken)
		{
			// Levelling (in-run): the pips under the icon, inside the frame's foot.
			var foot = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
			var pips = new LevelPips
			{
				Count = entry.LevelCap,
				Filled = Mathf.Clamp(entry.Level, 0, entry.LevelCap),
				Pip = narrow ? 8f : 7f,
				Spacing = 2f,
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			var inset = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			inset.AddThemeConstantOverride("margin_bottom", 4);
			inset.AddChild(pips);
			foot.AddChild(inset);
			frame.AddChild(foot);
		}
		if (entry.State == CodexState.Taken)
			frame.AddChild(CornerMark("★", CodexInk.TakenRim));
		if (entry.SetAside)
			frame.AddChild(CornerMark("Set aside", CodexRows.Stone.InkSoft, small: true, bottom: true));
		if (entry.State == CodexState.Ready)
			AscensionReadyGlow.AttachTo(frame, showRing: true).SetActive(true);

		// Ready: the name in the frame's gold too, so a still screenshot says it as well as the breathing ring.
		Label name = CodexRows.MakeText(entry.ShownName, ResponsiveLayout.TextRole.Micro,
			entry.State == CodexState.Ready ? CodexRows.Stone.Reward : entry.Discovered ? CodexRows.Stone.Ink : CodexRows.Stone.InkSoft);
		name.AutowrapMode = TextServer.AutowrapMode.Off;
		name.ClipText = true;
		name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		name.HorizontalAlignment = HorizontalAlignment.Center;
		name.CustomMinimumSize = new Vector2(CellWidth, 0f);
		cell.AddChild(name);

		string id = entry.Id;
		overlay.FocusEntered += () => OnCellFocused(id);
		overlay.Pressed += () => OnCellPressed(id);
		cells[entry.Id] = overlay;
		frames[entry.Id] = frame;
		return cell;
	}

	private Control CellPicture(CodexEntry entry)
	{
		if (!entry.Discovered)
		{
			// The sealed glyph (24 drawn pixels) at exactly x2: never the thing's own shape.
			return new TextureRect
			{
				Texture = StatIcons.Load("sealed"),
				CustomMinimumSize = new Vector2(48, 48),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				Modulate = new Color(1f, 1f, 1f, 0.7f),
			};
		}
		bool dim = entry.State == CodexState.Known;
		return new TextureRect
		{
			Texture = entry.Icon ?? DefaultIcon,
			// The icons are drawn at 64: shown whole, never at a fraction.
			CustomMinimumSize = new Vector2(64, 64),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			// Known but not owned or not taken: dimmed toward the stone, never erased.
			Modulate = dim ? new Color(0.62f, 0.62f, 0.68f, 0.85f) : Colors.White,
		};
	}

	private Control CornerMark(string text, Color ink, bool small = false, bool bottom = false)
	{
		var holder = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		holder.AddThemeConstantOverride("margin_top", 1);
		holder.AddThemeConstantOverride("margin_right", 3);
		holder.AddThemeConstantOverride("margin_left", 3);
		holder.AddThemeConstantOverride("margin_bottom", 1);
		var label = new Label
		{
			Text = text,
			HorizontalAlignment = small ? HorizontalAlignment.Center : HorizontalAlignment.Right,
			VerticalAlignment = bottom ? VerticalAlignment.Bottom : VerticalAlignment.Top,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		ResponsiveLayout.SetFont(label, small ? ResponsiveLayout.TextRole.Micro : ResponsiveLayout.TextRole.Body);
		label.AddThemeColorOverride("font_color", ink);
		// A dark lip so the word reads over an icon's light pixels too.
		label.AddThemeColorOverride("font_outline_color", CodexInk.StoneDeep);
		label.AddThemeConstantOverride("outline_size", 5);
		holder.AddChild(label);
		return holder;
	}

	private static Texture2D defaultIcon;
	private static Texture2D DefaultIcon => defaultIcon ??= BonelightSkin.LoadTextureSafe("res://assets/bonelight/ui/spells/unknown.png");

	/// <summary>
	/// The grid's focus: Godot's own geometry inside it, with three walls. The last column never steps
	/// right into the detail (A goes there), the first row steps up to the chosen filter, and the filters
	/// step down to the first cell.
	/// </summary>
	private void WireGridFocus(int columns)
	{
		List<Button> ordered = OrderedCells();
		Button chosenTab = tabs.ElementAtOrDefault((int)filter);
		for (int i = 0; i < ordered.Count; i++)
		{
			Button cell = ordered[i];
			GridContainer grid = cell.GetParent()?.GetParent()?.GetParent() as GridContainer;
			int index = grid != null ? cell.GetParent().GetParent().GetIndex() : i;
			int gridColumns = grid?.Columns ?? columns;
			if (!narrow && (index % gridColumns == gridColumns - 1 || index == (grid?.GetChildCount() ?? ordered.Count) - 1))
				cell.FocusNeighborRight = new NodePath(".");
			if (grid != null && grid == FirstGrid() && index < gridColumns && chosenTab != null)
				cell.FocusNeighborTop = cell.GetPathTo(chosenTab);
		}
		Button first = ordered.FirstOrDefault();
		foreach (Button tab in tabs)
		{
			tab.FocusNeighborBottom = first != null ? tab.GetPathTo(first) : new NodePath();
			tab.FocusNeighborTop = searchField != null ? tab.GetPathTo(searchField) : new NodePath();
		}
		if (searchField != null && tabs.Count > 0)
			searchField.FocusNeighborBottom = searchField.GetPathTo(chosenTab ?? tabs[0]);
	}

	private GridContainer FirstGrid() => gridList.GetChildren().OfType<GridContainer>().FirstOrDefault();

	/// <summary>Every cell's button in reading order.</summary>
	private List<Button> OrderedCells()
	{
		var list = new List<Button>();
		foreach (GridContainer grid in gridList.GetChildren().OfType<GridContainer>())
			foreach (Node cell in grid.GetChildren())
				if (cell.GetNodeOrNull<Button>($"Frame/CodexCell_{cell.Name.ToString().Substring(5)}") is Button button)
					list.Add(button);
		return list;
	}

	private void RefreshSelectionMarks()
	{
		foreach (var pair in frames)
		{
			CodexEntry entry = model.Get(pair.Key);
			if (entry != null && IsInstanceValid(pair.Value))
				pair.Value.AddThemeStyleboxOverride("panel", CodexInk.CellFrame(entry.State, selected: !narrow && pair.Key == selectedId));
		}
	}

	private Control InitialFocus()
	{
		if (!string.IsNullOrEmpty(selectedId) && cells.TryGetValue(selectedId, out Button cell))
			return cell;
		return OrderedCells().FirstOrDefault() ?? (Control)tabs.FirstOrDefault() ?? backButton;
	}

	private void ScrollToSelected()
	{
		if (IsInstanceValid(gridScroll) && !string.IsNullOrEmpty(selectedId) && cells.TryGetValue(selectedId, out Button cell) && IsInstanceValid(cell))
			gridScroll.EnsureControlVisible(cell.GetParent<Control>());
	}

	private void UpdateCount()
	{
		if (countLabel != null)
			countLabel.Text = $"{model.DiscoveredCount} / {model.Total}";
	}

	// ---- selection ----------------------------------------------------------------------------------

	private void ResolveSelection(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			selectedId = model.Filtered(filter).FirstOrDefault()?.Id ?? string.Empty;
			return;
		}
		if (model.Get(id) is CodexEntry entry)
		{
			selectedId = entry.Id;
			return;
		}
		// An evolution's id: its spell, with the node lit.
		CodexSearchHit hit = model.SearchIndex.FirstOrDefault(h => h.NodeId.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
		selectedId = hit?.EntryId ?? model.Filtered(filter).FirstOrDefault()?.Id ?? string.Empty;
		litNodeId = hit?.NodeId ?? string.Empty;
	}

	private void OnCellFocused(string id)
	{
		if (id == selectedId)
			return;
		selectedId = id;
		litNodeId = string.Empty;
		formShown = -1;
		RefreshSelectionMarks();
		if (!narrow)
			ShowDetail();
	}

	private void OnCellPressed(string id)
	{
		bool same = id == selectedId;
		selectedId = id;
		if (!same)
		{
			litNodeId = string.Empty;
			formShown = -1;
		}
		RefreshSelectionMarks();
		if (narrow)
		{
			OpenSheet();
			return;
		}
		if (!same)
			ShowDetail();
		// A on a PC goes into the tree, where the stick walks the nodes (section 2).
		Callable.From(FocusTree).CallDeferred();
	}

	private void SelectEntry(string id, string nodeId = "")
	{
		CodexEntry entry = model.Get(id);
		if (entry == null)
			return;
		// An entry the filter hides: back to All, so the selection is on the grid.
		if (filter != CodexFilter.All && entry.Group != filter)
			SetFilter(CodexFilter.All, keepFocusOnTab: false);
		if (!string.IsNullOrWhiteSpace(query) && !model.Filtered(filter, query).Contains(entry))
			SetQuery(string.Empty);
		selectedId = entry.Id;
		litNodeId = nodeId ?? string.Empty;
		formShown = -1;
		RefreshSelectionMarks();
		if (narrow)
			OpenSheet();
		else
			ShowDetail();
		if (cells.TryGetValue(entry.Id, out Button cell) && IsInstanceValid(cell))
		{
			if (!narrow)
				cell.GrabFocus();
			Callable.From(ScrollToSelected).CallDeferred();
		}
	}

	// ---- filters and search -------------------------------------------------------------------------

	private void SetFilter(CodexFilter next, bool keepFocusOnTab)
	{
		filter = next;
		for (int i = 0; i < tabs.Count; i++)
			tabs[i].SetPressedNoSignal(i == (int)next);
		RebuildGrid();
		if (IsInstanceValid(gridScroll))
			gridScroll.ScrollVertical = 0;
		// The selection stays on its entry when the filter still shows it; otherwise the filter's first.
		if (!cells.ContainsKey(selectedId))
		{
			selectedId = model.Filtered(filter, query).FirstOrDefault()?.Id ?? selectedId;
			litNodeId = string.Empty;
			RefreshSelectionMarks();
		}
		if (!narrow)
			ShowDetail();
		if (keepFocusOnTab)
			tabs.ElementAtOrDefault((int)next)?.GrabFocus();
		SfxPlayer.Global(SfxCatalog.UiClick);
	}

	/// <summary>LB / RB: the next or previous filter, the selection kept in the grid.</summary>
	private void CycleFilter(int step)
	{
		int count = Enum.GetValues(typeof(CodexFilter)).Length;
		var next = (CodexFilter)(((int)filter + step + count) % count);
		bool onTab = tabs.Contains(GetViewport()?.GuiGetFocusOwner());
		SetFilter(next, keepFocusOnTab: onTab);
		if (!onTab && !KeyboardOpen)
		{
			if (cells.TryGetValue(selectedId, out Button cell) && IsInstanceValid(cell))
				cell.GrabFocus();
			else
				OrderedCells().FirstOrDefault()?.GrabFocus();
		}
	}

	private void OnSearchTextChanged(string text)
	{
		query = text ?? string.Empty;
		ApplyQuery();
	}

	private void SetQuery(string text)
	{
		query = text ?? string.Empty;
		if (searchField != null && searchField.Text != query)
			searchField.Text = query;
		ApplyQuery();
	}

	private void ApplyQuery()
	{
		// The grid filters live as letters arrive; the selection moves to the best hit.
		RebuildGrid();
		if (!string.IsNullOrWhiteSpace(query))
		{
			CodexSearchHit best = model.Search(query, 1).FirstOrDefault();
			if (best != null && cells.ContainsKey(best.EntryId))
			{
				selectedId = best.EntryId;
				litNodeId = best.NodeId;
				formShown = -1;
				RefreshSelectionMarks();
			}
		}
		if (!cells.ContainsKey(selectedId))
			selectedId = model.Filtered(filter, query).FirstOrDefault()?.Id ?? selectedId;
		if (!narrow)
			ShowDetail();
		keyboard?.ShowQuery(query);
		keyboard?.ShowSuggestions(model.Search(query, CodexKeyboard.MaxSuggestions));
	}

	// ---- the pad's keyboard -------------------------------------------------------------------------

	private void OpenKeyboard()
	{
		if (keyboard == null || keyboard.Visible)
			return;
		CloseSheetQuietly();
		keyboard.Open(narrow, query, model.Search(query, CodexKeyboard.MaxSuggestions));
		SfxPlayer.Global(SfxCatalog.UiOpen);
	}

	private void CloseKeyboard()
	{
		if (keyboard == null || !keyboard.Visible)
			return;
		keyboard.Close();
		// Back on the grid, on the best hit.
		if (cells.TryGetValue(selectedId, out Button cell) && IsInstanceValid(cell))
			cell.GrabFocus();
		else
			OrderedCells().FirstOrDefault()?.GrabFocus();
		Callable.From(ScrollToSelected).CallDeferred();
		SfxPlayer.Global(SfxCatalog.UiClose);
	}

	private void OnKeyboardTyped(string letter) => SetQuery(query + letter);

	private void OnKeyboardErased()
	{
		if (query.Length > 0)
			SetQuery(query.Substring(0, query.Length - 1));
	}

	private void OnKeyboardCleared() => SetQuery(string.Empty);

	private void OnSuggestionChosen(CodexSearchHit hit)
	{
		if (hit == null)
			return;
		keyboard.Close();
		// The search becomes the name chosen, and the selection its entry, with the node lit.
		query = hit.Name;
		if (searchField != null)
			searchField.Text = query;
		RebuildGrid();
		SelectEntry(hit.EntryId, hit.NodeId);
		if (!narrow && cells.TryGetValue(selectedId, out Button cell) && IsInstanceValid(cell))
			cell.GrabFocus();
	}

	// ---- input --------------------------------------------------------------------------------------

	public override void _Input(InputEvent @event)
	{
		if (!Visible || !built)
			return;
		// An element's card over the Codex hears its own presses first (ElementTagPopover, layer 150).
		if (ElementTagPopover.IsOpen)
			return;

		if (KeyboardOpen)
		{
			if (keyboard.HandleInput(@event))
				GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventJoypadButton { Pressed: true } pad)
		{
			switch (pad.ButtonIndex)
			{
				case JoyButton.LeftShoulder:
					CycleFilter(-1);
					GetViewport().SetInputAsHandled();
					return;
				case JoyButton.RightShoulder:
					CycleFilter(1);
					GetViewport().SetInputAsHandled();
					return;
				case JoyButton.Y:
					OpenKeyboard();
					GetViewport().SetInputAsHandled();
					return;
				case JoyButton.A when GetViewport()?.GuiGetFocusOwner() == searchField:
					OpenKeyboard();
					GetViewport().SetInputAsHandled();
					return;
				case JoyButton.B:
				case JoyButton.Start:
					StepBack();
					GetViewport().SetInputAsHandled();
					return;
			}
		}

		// Escape on a keyboard: as B. A field being typed in lets go of the field first.
		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			if (GetViewport()?.GuiGetFocusOwner() == searchField && searchField.IsEditing())
				searchField.Unedit();
			else
				StepBack();
			GetViewport().SetInputAsHandled();
		}
	}

	/// <summary>B: the tree to its cell, the sheet to the grid, the grid out of the Codex.</summary>
	private void StepBack()
	{
		if (KeyboardOpen)
		{
			CloseKeyboard();
			return;
		}
		if (SheetOpen)
		{
			CloseSheet();
			return;
		}
		Control owner = GetViewport()?.GuiGetFocusOwner();
		if (owner != null && detail != null && detail.IsAncestorOf(owner) && cells.TryGetValue(selectedId, out Button cell))
		{
			cell.GrabFocus();
			SfxPlayer.Global(SfxCatalog.UiBack);
			return;
		}
		Close();
	}

	private void OpenSheet()
	{
		if (sheet == null)
			return;
		ShowDetail();
		sheet.Visible = true;
		if (sheetTitle != null)
			sheetTitle.Text = model.Get(selectedId)?.ShownName ?? string.Empty;
		if (IsInstanceValid(detailScroll))
			detailScroll.ScrollVertical = 0;
		Control first = FirstTreeStop() ?? sheet.FindChild("CodexSheetBack", true, false) as Control;
		first?.GrabFocus();
		SfxPlayer.Global(SfxCatalog.UiOpen);
	}

	private void CloseSheet()
	{
		if (sheet == null || !sheet.Visible)
			return;
		CloseSheetQuietly();
		// Back to the grid, the same cell selected (section 2).
		if (cells.TryGetValue(selectedId, out Button cell) && IsInstanceValid(cell))
			cell.GrabFocus();
		SfxPlayer.Global(SfxCatalog.UiBack);
	}

	private void CloseSheetQuietly()
	{
		if (sheet != null)
			sheet.Visible = false;
	}

	private void Close()
	{
		if (IsQueuedForDeletion())
			return;
		IsOpen = false;
		ElementTagPopover.Close();
		SfxPlayer.Global(SfxCatalog.UiClose);
		QueueFree();
		closed?.Invoke();
	}
}

/// <summary>
/// The Codex's inks and frames, in one place: every colour is the stone register's (CodexRows.Stone), the
/// heading band's, or a milestone's mark (GameOverScreen), so the Codex teaches the visual language the
/// player meets in play (section 3). Measured in tools/art/ui_contrast.py ("codex"): change one, change both.
/// </summary>
public static class CodexInk
{
	/// <summary>stone.deep: the heading band, a sealed cell's well.</summary>
	public static readonly Color StoneDeep = new(0.043f, 0.059f, 0.094f);
	/// <summary>The heading band's gold rule (JournalScreen's).</summary>
	public static readonly Color BandRule = new(0.541f, 0.416f, 0.118f);
	/// <summary>The search field's face: stone deep, a step lighter.</summary>
	public static readonly Color FieldFace = new(0.071f, 0.090f, 0.129f);
	/// <summary>An owned or held cell's rim: steel, the loadout strip's held rim.</summary>
	public static readonly Color HeldRim = new(0.416f, 0.494f, 0.596f);
	/// <summary>Taken / maxed: the solid gold rim (the mutation mark's gold).</summary>
	public static readonly Color TakenRim = new(0.847f, 0.690f, 0.290f);
	/// <summary>The selected cell on a PC, while the selection is in the tree: gold hi.</summary>
	public static readonly Color SelectedRim = new(1.0f, 0.941f, 0.753f);
	/// <summary>Dark ink on a milestone's solid mark (GameOverScreen.MarkInk).</summary>
	public static readonly Color MarkInk = new(0.06f, 0.06f, 0.07f);
	/// <summary>The rubric band an unmet requirement sits on, and its ink (the browser's, LevelUpMenu).</summary>
	public static readonly Color UnmetBand = new(0.896f, 0.789f, 0.708f);
	public static readonly Color UnmetInk = new(0.416f, 0.125f, 0.157f);

	public static readonly Color MutationMark = new(0.847f, 0.690f, 0.290f);
	public static readonly Color AscensionMark = new(0.416f, 0.722f, 0.910f);
	public static readonly Color ApotheosisMark = new(0.910f, 0.478f, 0.102f);
	/// <summary>A combination's mark: the conjunction plate's starlight.</summary>
	public static Color ConjunctionMark => CardFamilyArt.ConjunctionTitle;

	public static Color MarkFor(int milestone) => milestone switch
	{
		4 => MutationMark,
		8 => AscensionMark,
		ApotheosisCatalog.MilestoneLevel => ApotheosisMark,
		ComboCatalog.MilestoneLevel => ConjunctionMark,
		_ => TakenRim,
	};

	public static StyleBoxFlat CellFrame(CodexState state, bool selected)
	{
		var box = new StyleBoxFlat
		{
			BgColor = state == CodexState.Undiscovered ? StoneDeep : CodexRows.Stone.Face,
			BorderColor = state switch
			{
				CodexState.Undiscovered => CodexRows.Stone.QuietEdge,
				CodexState.Known => CodexRows.Stone.Edge,
				CodexState.Ready => CodexRows.Stone.Reward,
				CodexState.Taken => TakenRim,
				_ => HeldRim,
			},
		};
		int width = state is CodexState.Ready or CodexState.Taken ? 3 : 2;
		if (selected)
		{
			box.BorderColor = SelectedRim;
			width = 3;
		}
		box.SetBorderWidthAll(width);
		box.SetCornerRadiusAll(4);
		box.SetContentMarginAll(0);
		return box;
	}
}
