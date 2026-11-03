using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

/// <summary>
/// THE EVOLUTION CODEX (.ai/evolution-codex.md, approved by Eric 2026-11-02): one book for every spell, item,
/// evolution and recipe. It replaced the level-up screen's Ascensions browser and the main menu's Spell Index,
/// from a player's request (Amy, 2026-11-02) that the progression stop being put together from three places.
/// </summary>
/// <remarks>
/// LAYOUT B, "THE BOOK" (Eric's pick, 2026-11-02). On a PC an open book on the stone plate: the LEFT PAGE is
/// the index - the search field, then every entry by its full name, spells under their element in the
/// Element order, items under their set or rarity, recipes under their kind - and the RIGHT PAGE is the
/// selected entry, its evolutions as a family tree down the page. On a phone one page at a time: the index,
/// and a tap turns to the entry's page ("‹ Index" turns back; a swipe, or LB/RB, turns to the next entry).
/// Spells, Items and Recipes are bookmarks on the top edge. Parchment in the level-up screen's manuscript
/// inks (CodexRows.Vellum; tools/art/ui_contrast.py "codex book").
///
/// One screen, two modes (CodexModel): in a run the states are the run's, on the main menu the save's. A
/// CanvasLayer of its own over whatever opened it - the level-up screen, the pause screen or the main menu -
/// added to the tree's root so it is never inside the opener's menu: the opener's navigator sees the
/// selection is elsewhere and stands down, and this one's takes it (MenuNavigator, "screens stack"). It
/// reads, it never grants: nothing here takes an Ascension or makes a recipe. The one thing it changes is the
/// save's Set aside (a Redaction), which the Spell Index carried and the Codex keeps, in collection mode.
///
/// THE PARTS AND THE ARRANGEMENT. This file is the shell (opening, the index, the bookmarks, search, the
/// keyboard, selection, input); EvolutionCodex.Detail.cs writes an entry's page; EvolutionCodex.Layout.cs is
/// the only place that says where things go on each canvas.
/// </remarks>
public partial class EvolutionCodex : CanvasLayer
{
	/// <summary>Above the level-up screen (10), the chest (100) and the set detail (110); under an element's card (150).</summary>
	public const int ScreenLayer = 120;

	public const string Title = "Evolution Codex";

	/// <summary>The book's three bookmarks (section 4's filters; "All" has no page of its own in the book).</summary>
	private static readonly CodexFilter[] Bookmarks = { CodexFilter.Spells, CodexFilter.Items, CodexFilter.Recipes };

	/// <summary>True while a Codex is open: the screens under it stand down from their own pad buttons.</summary>
	public static bool IsOpen { get; private set; }

	private CodexContext context = new();
	private CodexModel model;
	private CodexFilter filter = CodexFilter.Spells;
	private string query = string.Empty;
	private string selectedId = string.Empty;
	/// <summary>A node a search hit lit inside the selected entry (an evolution, an ingredient).</summary>
	private string litNodeId = string.Empty;
	/// <summary>The form tab the page shows, for a spell taken in forms; -1 for the entry's own.</summary>
	private int formShown = -1;
	private Action closed;
	private bool narrow;
	private bool built;

	private Control root = null!;
	private LineEdit searchField = null!;
	private Label countLabel = null!;
	private Button backButton = null!;
	private readonly List<Button> tabs = new();
	private ScrollContainer indexScroll = null!;
	private VBoxContainer indexList = null!;
	/// <summary>Where an entry's page is written: the right page on a PC, the turned page on a phone.</summary>
	private VBoxContainer page = null!;
	private ScrollContainer pageScroll = null!;
	/// <summary>A phone's two pages, one shown at a time; null on a PC, where both are open.</summary>
	private Control indexPage;
	private Control entryPage;
	private Label entryTitle;
	private Button indexButton;
	private CodexKeyboard keyboard;
	private readonly Dictionary<string, Button> rows = new();

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
	/// <summary>A phone's entry page is turned to (the index is behind it).</summary>
	public bool PageOpen => entryPage != null && entryPage.Visible;

	// The harness's hands (_UiShot --codex-search, --codex-sheet, --codex-keyboard): each does what the
	// player's own press would, through the same method.
	public void DebugSearch(string text)
	{
		SetQuery(text);
		if (rows.TryGetValue(selectedId, out Button row) && IsInstanceValid(row))
			row.GrabFocus();
	}

	public void DebugOpenSheet() => OpenPage();

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
		// Android's back steps back exactly as B does: the page to the index, then the Codex.
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
		rows.Clear();
		tabs.Clear();
		indexPage = null;
		entryPage = null;
		entryTitle = null;
		indexButton = null;
		keyboard = null;
		narrow = ResponsiveLayout.IsNarrow(this);

		root = new Control { Name = "CodexRoot", MouseFilter = Control.MouseFilterEnum.Stop };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		var dim = new ColorRect { Color = new Color(0.025f, 0.03f, 0.045f, 0.92f), MouseFilter = Control.MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(dim);

		// The arrangement, one builder per canvas (EvolutionCodex.Layout.cs): everything else is shared.
		if (narrow)
			BuildPhoneBook();
		else
			BuildPcBook();

		keyboard = new CodexKeyboard { Name = "CodexKeyboard", Visible = false };
		keyboard.Place(narrow);
		keyboard.Typed += OnKeyboardTyped;
		keyboard.Erased += OnKeyboardErased;
		keyboard.Cleared += OnKeyboardCleared;
		keyboard.Done += CloseKeyboard;
		keyboard.SuggestionChosen += OnSuggestionChosen;
		root.AddChild(keyboard);

		built = true;
		RebuildIndex();
		ShowPage();
		UpdateCount();

		SafeArea.Apply(root);
		MenuNavigator.Attach(this, InitialFocus());
		// A selection made by the opener (a search hit, a recipe) is scrolled to once the index has a size.
		Callable.From(ScrollToSelected).CallDeferred();
	}

	/// <summary>The title, the count of what has been found, and - where the arrangement puts it - Back.</summary>
	private Control BuildTitleRow(bool withBack)
	{
		var row = new HBoxContainer { Name = "CodexTitleRow", MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 14);
		var title = new Label
		{
			Name = "CodexTitle",
			Text = Title,
			VerticalAlignment = VerticalAlignment.Center,
			CustomMinimumSize = new Vector2(0f, ResponsiveLayout.MinTouchTarget),
		};
		ResponsiveLayout.SetFont(title, narrow ? ResponsiveLayout.TextRole.Title : ResponsiveLayout.TextRole.Display);
		title.AddThemeColorOverride("font_color", CodexRows.Stone.Heading);
		row.AddChild(title);
		countLabel = new Label { Name = "CodexCount", VerticalAlignment = VerticalAlignment.Center };
		ResponsiveLayout.SetFont(countLabel, ResponsiveLayout.TextRole.Label);
		countLabel.AddThemeColorOverride("font_color", CodexRows.Stone.InkSoft);
		row.AddChild(countLabel);
		row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore });
		if (withBack)
		{
			backButton = BuildBackButton();
			row.AddChild(backButton);
		}
		return row;
	}

	private Button BuildBackButton()
	{
		var back = new Button { Name = "CodexBack", Text = "Back", FocusMode = Control.FocusModeEnum.All };
		ResponsiveLayout.SetFont(back, ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(back, BonelightSkin.Emphasis.Normal);
		ResponsiveLayout.EnsureTouchTarget(back, 112f);
		back.Pressed += Close;
		return back;
	}

	/// <summary>A page of the book: the vellum, the level-up screen's manuscript (BonelightSkin's Vellum register).</summary>
	private static PanelContainer BuildBookPage(string name)
	{
		var sheet = new PanelContainer
		{
			Name = name,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		sheet.AddThemeStyleboxOverride("panel", BonelightSkin.PanelStyle(BonelightSkin.Register.Vellum));
		return sheet;
	}

	private Control BuildSearchField()
	{
		searchField = new LineEdit
		{
			Name = "CodexSearch",
			PlaceholderText = "Search the index",
			Text = query,
			ClearButtonEnabled = true,
			CaretBlink = true,
			// A phone raises its own keyboard for the field; a pad gets ours (CodexKeyboard), through Y.
			VirtualKeyboardEnabled = true,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0f, ResponsiveLayout.MinTouchTarget),
		};
		ResponsiveLayout.SetFont(searchField, ResponsiveLayout.TextRole.Body);
		StyleBookField(searchField);
		searchField.TextChanged += OnSearchTextChanged;
		return searchField;
	}

	// On the page: a lighter field ruled in the page's edge, the gold shade when it has the selection.
	private static void StyleBookField(LineEdit field)
	{
		StyleBoxFlat Box(Color edge, int width)
		{
			var box = new StyleBoxFlat { BgColor = CodexInk.BookField, BorderColor = edge };
			box.SetBorderWidthAll(width);
			box.SetCornerRadiusAll(3);
			box.ContentMarginLeft = 10;
			box.ContentMarginRight = 8;
			box.ContentMarginTop = 4;
			box.ContentMarginBottom = 4;
			return box;
		}
		field.AddThemeStyleboxOverride("normal", Box(CodexRows.Vellum.Edge, 2));
		field.AddThemeStyleboxOverride("focus", Box(CodexRows.Vellum.Reward, 2));
		field.AddThemeStyleboxOverride("read_only", Box(CodexRows.Vellum.Edge, 1));
		field.AddThemeColorOverride("font_color", CodexRows.Vellum.Ink);
		field.AddThemeColorOverride("font_placeholder_color", CodexRows.Vellum.InkMeta);
		field.AddThemeColorOverride("caret_color", CodexRows.Vellum.Ink);
		field.AddThemeColorOverride("selection_color", new Color(CodexInk.BookSelected, 0.9f));
		field.AddThemeColorOverride("clear_button_color", CodexRows.Vellum.InkMeta);
		field.AddThemeColorOverride("clear_button_color_pressed", CodexRows.Vellum.Ink);
	}

	/// <summary>Spells, Items, Recipes: bookmarks on the book's top edge, the open one in the page's own vellum.</summary>
	private Control BuildBookmarks()
	{
		var row = new HBoxContainer { Name = "CodexBookmarks", MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 4);
		var group = new ButtonGroup();
		foreach (CodexFilter f in Bookmarks)
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
				SizeFlagsVertical = Control.SizeFlags.ShrinkEnd,
			};
			// Cinzel on a PC; on a phone three Cinzel bookmarks crowd the width, and the sans fits.
			ResponsiveLayout.SetFont(tab, narrow ? ResponsiveLayout.TextRole.Body : ResponsiveLayout.TextRole.Title);
			StyleBookmark(tab);
			ResponsiveLayout.EnsureTouchTarget(tab, narrow ? 0f : 112f);
			CodexFilter chosen = f;
			tab.Pressed += () => SetFilter(chosen, keepFocusOnTab: true);
			tabs.Add(tab);
			row.AddChild(tab);
		}
		return row;
	}

	private static void StyleBookmark(Button tab)
	{
		StyleBoxFlat Tab(Color face, Color edge)
		{
			var box = new StyleBoxFlat { BgColor = face, BorderColor = edge };
			box.SetBorderWidthAll(2);
			box.BorderWidthBottom = 0;
			box.CornerRadiusTopLeft = 4;
			box.CornerRadiusTopRight = 4;
			box.ContentMarginLeft = 16;
			box.ContentMarginRight = 16;
			box.ContentMarginTop = 4;
			box.ContentMarginBottom = 4;
			return box;
		}
		// The open bookmark is the page's vellum; the others a darker leaf behind it.
		tab.AddThemeStyleboxOverride("normal", Tab(CodexInk.BookmarkShut, CodexRows.Vellum.Edge));
		tab.AddThemeStyleboxOverride("hover", Tab(CodexInk.BookmarkHover, CodexRows.Vellum.Edge));
		tab.AddThemeStyleboxOverride("focus", Tab(CodexInk.BookmarkHover, CodexRows.Vellum.Reward));
		tab.AddThemeStyleboxOverride("pressed", Tab(CodexRows.Vellum.Face, CodexRows.Vellum.Reward));
		tab.AddThemeStyleboxOverride("hover_pressed", Tab(CodexRows.Vellum.Face, CodexRows.Vellum.Reward));
		foreach (string state in new[] { "font_color", "font_hover_color", "font_focus_color", "font_pressed_color", "font_hover_pressed_color" })
			tab.AddThemeColorOverride(state, CodexRows.Vellum.Ink);
	}

	// ---- the index (the left page) ----------------------------------------------------------------

	private void RebuildIndex()
	{
		foreach (Node child in indexList.GetChildren())
		{
			indexList.RemoveChild(child);
			child.QueueFree();
		}
		rows.Clear();

		List<CodexEntry> shown = model.Filtered(filter, query);
		if (shown.Count == 0)
		{
			Label none = CodexRows.MakeText(string.IsNullOrWhiteSpace(query) ? "Nothing here yet." : $"Nothing matches \"{query}\".",
				ResponsiveLayout.TextRole.Body, CodexRows.Vellum.InkSoft);
			none.Name = "CodexEmpty";
			indexList.AddChild(none);
		}
		foreach ((string heading, Color? swatch, List<CodexEntry> entries) in IndexGroups(shown))
		{
			indexList.AddChild(BuildIndexHeading(heading, swatch));
			foreach (CodexEntry entry in entries)
				indexList.AddChild(BuildIndexRow(entry));
		}
		WireIndexFocus();
		RefreshSelectionMarks();
	}

	/// <summary>
	/// How the index files its entries: spells under their element (the Element order, the game's own), items
	/// under the set they are a piece of - once that set is known - or else their rarity, then the boons, and
	/// recipes under their kind. Inside a heading the model's order holds (section 5's sort).
	/// </summary>
	private List<(string Heading, Color? Swatch, List<CodexEntry> Entries)> IndexGroups(List<CodexEntry> shown)
	{
		var groups = new List<(string, Color?, List<CodexEntry>)>();
		void Add(string heading, Color? swatch, IEnumerable<CodexEntry> entries)
		{
			List<CodexEntry> list = entries.ToList();
			if (list.Count > 0)
				groups.Add((heading, swatch, list));
		}
		switch (filter)
		{
			case CodexFilter.Spells:
				foreach (Element element in Enum.GetValues(typeof(Element)))
					Add(element.ToString(), ElementColors.GetColor(element), shown.Where(e => e.IndexElement == element));
				Add("Untagged", null, shown.Where(e => e.IndexElement == null));
				break;
			case CodexFilter.Items:
				var placed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				List<CodexEntry> relics = shown.Where(e => e.Kind == CodexKind.Relic).ToList();
				foreach (CodexEntry set in model.Entries.Where(e => e.Kind == CodexKind.FullSet && e.Discovered))
				{
					List<CodexEntry> pieces = relics.Where(r => !placed.Contains(r.Id)
						&& r.SetIds.FirstOrDefault(id => model.Get(id)?.Discovered == true) == set.Id).ToList();
					foreach (CodexEntry piece in pieces)
						placed.Add(piece.Id);
					Add(set.Name, null, pieces);
				}
				foreach (ChestItemRarity rarity in new[] { ChestItemRarity.Relic, ChestItemRarity.Rare, ChestItemRarity.Uncommon, ChestItemRarity.Common })
					Add(rarity == ChestItemRarity.Relic ? "Relics" : $"{rarity} relics", null,
						relics.Where(r => !placed.Contains(r.Id) && r.Rarity == rarity));
				Add("Boons", null, shown.Where(e => e.Kind == CodexKind.Boon));
				break;
			default:
				Add("Conjunctions", null, shown.Where(e => e.Kind == CodexKind.Combination));
				Add("Full Set Enchantments", null, shown.Where(e => e.Kind == CodexKind.FullSet));
				break;
		}
		return groups;
	}

	private static Font headingFont;

	private Control BuildIndexHeading(string heading, Color? swatch)
	{
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 8);
		var pad = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		pad.AddThemeConstantOverride("margin_top", 8);
		pad.AddChild(row);
		if (swatch is Color colour)
		{
			// The element's own colour, rimmed in the page's ink so the pale ones (Light, Wind) still show.
			var chip = new PanelContainer { CustomMinimumSize = new Vector2(14, 14), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
			var box = new StyleBoxFlat { BgColor = colour, BorderColor = CodexRows.Vellum.Ink };
			box.SetBorderWidthAll(1);
			chip.AddThemeStyleboxOverride("panel", box);
			row.AddChild(chip);
		}
		var label = new Label { Text = heading.ToUpperInvariant(), VerticalAlignment = VerticalAlignment.Center };
		ResponsiveLayout.SetFont(label, ResponsiveLayout.TextRole.Label);
		// Cinzel, as every heading (the level-up doc's 4a), at the index's size.
		headingFont ??= GD.Load<Font>("res://scenes/resources/HeadingFont.tres");
		if (headingFont != null)
			label.AddThemeFontOverride("font", headingFont);
		label.AddThemeColorOverride("font_color", CodexRows.Vellum.Heading);
		row.AddChild(label);
		var rule = new ColorRect { Color = CodexRows.Vellum.Rule, CustomMinimumSize = new Vector2(0, 2), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddChild(rule);
		return pad;
	}

	/// <summary>
	/// One line of the index: the full name, never cut short (a ★ on the wizard's own), in the ink while it is
	/// held or owned and faded while it is only known, "? ? ?" while unfound; the pips of a spell held this
	/// run, or what an item or a recipe is to this run, at the right.
	/// </summary>
	private Control BuildIndexRow(CodexEntry entry)
	{
		var button = new Button
		{
			Name = $"CodexRow_{entry.Id}",
			FocusMode = Control.FocusModeEnum.All,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0f, narrow ? ResponsiveLayout.MinTouchTarget : 34f),
		};
		StyleIndexRow(button, selected: false);
		if (!DisplayServer.IsTouchscreenAvailable() && !entry.Discovered)
			button.TooltipText = entry.Hint;

		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		line.OffsetLeft = 10f;
		line.OffsetRight = -8f;
		line.AddThemeConstantOverride("separation", 8);
		button.AddChild(line);

		bool faded = !entry.Discovered || IsOnlyKnown(entry);
		Color ink = faded ? CodexRows.Vellum.InkMeta : CodexRows.Vellum.Ink;
		if (entry.Own)
			line.AddChild(RowText("★", CodexRows.Vellum.Reward));
		Label name = RowText(entry.Discovered ? entry.Name : "? ? ?", ink);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		line.AddChild(name);

		if (!entry.Discovered)
			line.AddChild(RowText("not yet found", CodexRows.Vellum.InkMeta, small: true));
		else if (entry.Held && entry.Kind == CodexKind.Spell && entry.LevelCap > 0)
			line.AddChild(new LevelPips
			{
				Count = entry.LevelCap,
				Filled = Mathf.Clamp(entry.Level, 0, entry.LevelCap),
				Pip = 9f,
				Spacing = 2f,
				SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			});
		else if (RowNote(entry) is string note)
			line.AddChild(RowText(note, entry.State == CodexState.Ready ? CodexRows.Vellum.Reward : CodexRows.Vellum.InkSoft, small: true));

		string id = entry.Id;
		button.FocusEntered += () => OnRowFocused(id);
		button.Pressed += () => OnRowPressed(id);
		rows[entry.Id] = button;
		return button;
	}

	// Known but not the player's: in a run anything not held; on the menu a spell, boon or recipe not owned.
	private bool IsOnlyKnown(CodexEntry entry)
	{
		if (!entry.Discovered)
			return false;
		if (model.Mode == CodexMode.InRun)
			return !entry.Held && entry.State != CodexState.Ready && entry.State != CodexState.Leveling;
		return entry.Kind is CodexKind.Spell or CodexKind.Boon or CodexKind.Combination && !entry.Owned;
	}

	private static string RowNote(CodexEntry entry) => entry.State switch
	{
		CodexState.Ready => "ready",
		CodexState.Taken when entry.Kind == CodexKind.Combination => "made",
		CodexState.Taken when entry.Kind == CodexKind.FullSet => "complete",
		CodexState.Taken => "held",
		CodexState.Leveling when entry.Kind == CodexKind.FullSet => $"{entry.Level}/{entry.LevelCap}",
		CodexState.Owned when entry.Kind == CodexKind.Combination => "made",
		_ => entry.SetAside ? "set aside" : null,
	};

	private Label RowText(string text, Color ink, bool small = false)
	{
		Label label = CodexRows.MakeText(text, small ? ResponsiveLayout.TextRole.Micro : narrow ? ResponsiveLayout.TextRole.Body : ResponsiveLayout.TextRole.Label, ink);
		label.AutowrapMode = TextServer.AutowrapMode.Off;
		label.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		return label;
	}

	private static void StyleIndexRow(Button button, bool selected)
	{
		StyleBoxFlat Row(Color face, int marker)
		{
			var box = new StyleBoxFlat { BgColor = face, BorderColor = CodexRows.Vellum.Reward };
			box.BorderWidthLeft = marker;
			box.SetCornerRadiusAll(2);
			return box;
		}
		// The selected entry is marked on the page itself - a gold-washed line with a gold rule at its left -
		// so it stays marked while the selection is on the right page.
		Color rest = selected ? CodexInk.BookSelected : new Color(CodexInk.BookSelected, 0f);
		int marker = selected ? 4 : 0;
		button.AddThemeStyleboxOverride("normal", Row(rest, marker));
		button.AddThemeStyleboxOverride("hover", Row(selected ? CodexInk.BookSelected : CodexInk.BookHover, marker));
		button.AddThemeStyleboxOverride("focus", Row(selected ? CodexInk.BookSelected : CodexInk.BookHover, marker));
		button.AddThemeStyleboxOverride("pressed", Row(CodexInk.BookSelected, 4));
		button.AddThemeStyleboxOverride("hover_pressed", Row(CodexInk.BookSelected, 4));
	}

	private void RefreshSelectionMarks()
	{
		foreach (var pair in rows)
			if (IsInstanceValid(pair.Value))
				StyleIndexRow(pair.Value, pair.Key == selectedId);
	}

	/// <summary>
	/// The index's focus: down the page, with two walls - sideways never jumps onto the entry's page (A goes
	/// there), and the first line steps up to the search field, which steps up to the open bookmark.
	/// </summary>
	private void WireIndexFocus()
	{
		List<Button> ordered = OrderedRows();
		foreach (Button row in ordered)
		{
			row.FocusNeighborRight = new NodePath(".");
			row.FocusNeighborLeft = new NodePath(".");
		}
		Button first = ordered.FirstOrDefault();
		if (first != null && searchField != null)
		{
			first.FocusNeighborTop = first.GetPathTo(searchField);
			searchField.FocusNeighborBottom = searchField.GetPathTo(first);
		}
		Button chosenTab = TabFor(filter);
		if (searchField != null && chosenTab != null)
			searchField.FocusNeighborTop = searchField.GetPathTo(chosenTab);
		foreach (Button tab in tabs)
			tab.FocusNeighborBottom = searchField != null ? tab.GetPathTo(searchField) : new NodePath();
	}

	private Button TabFor(CodexFilter f)
	{
		int at = Array.IndexOf(Bookmarks, f);
		return at >= 0 && at < tabs.Count ? tabs[at] : null;
	}

	/// <summary>Every index line's button, in reading order.</summary>
	private List<Button> OrderedRows() => indexList.GetChildren().OfType<Button>().ToList();

	private static string IdOf(Button row) => row?.Name.ToString().Substring("CodexRow_".Length);

	private Control InitialFocus()
	{
		if (!string.IsNullOrEmpty(selectedId) && rows.TryGetValue(selectedId, out Button row))
			return row;
		return OrderedRows().FirstOrDefault() ?? (Control)tabs.FirstOrDefault() ?? backButton;
	}

	private void ScrollToSelected()
	{
		if (IsInstanceValid(indexScroll) && !string.IsNullOrEmpty(selectedId) && rows.TryGetValue(selectedId, out Button row) && IsInstanceValid(row))
			indexScroll.EnsureControlVisible(row);
	}

	private void UpdateCount()
	{
		if (countLabel != null)
			countLabel.Text = $"{model.DiscoveredCount} of {model.Total} found";
	}

	// ---- selection ----------------------------------------------------------------------------------

	private void ResolveSelection(string id)
	{
		CodexEntry entry = model.Get(id);
		if (entry == null && !string.IsNullOrWhiteSpace(id))
		{
			// An evolution's id: its spell, with the node lit.
			CodexSearchHit hit = model.SearchIndex.FirstOrDefault(h => h.NodeId.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
			entry = hit != null ? model.Get(hit.EntryId) : null;
			litNodeId = hit?.NodeId ?? string.Empty;
		}
		// The book has no "All": it opens on the entry's own bookmark, or on Spells.
		if (entry != null)
			filter = entry.Group;
		else if (filter == CodexFilter.All)
			filter = CodexFilter.Spells;
		selectedId = entry?.Id ?? model.Filtered(filter).FirstOrDefault()?.Id ?? string.Empty;
	}

	private void OnRowFocused(string id)
	{
		if (id == selectedId)
			return;
		selectedId = id;
		litNodeId = string.Empty;
		formShown = -1;
		RefreshSelectionMarks();
		if (!narrow)
			ShowPage();
	}

	private void OnRowPressed(string id)
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
			OpenPage();
			return;
		}
		if (!same)
			ShowPage();
		// A on a PC goes onto the page, where the stick walks the tree (section 2).
		Callable.From(FocusTree).CallDeferred();
	}

	/// <summary>Turns to <paramref name="id"/>'s page, from anywhere: a recipe's ingredient, a margin note, a search.</summary>
	private void SelectEntry(string id, string nodeId = "")
	{
		CodexEntry entry = model.Get(id);
		if (entry == null)
			return;
		if (entry.Group != filter)
			SetFilter(entry.Group, keepFocusOnTab: false);
		if (!string.IsNullOrWhiteSpace(query) && !model.Filtered(filter, query).Contains(entry))
			SetQuery(string.Empty);
		selectedId = entry.Id;
		litNodeId = nodeId ?? string.Empty;
		formShown = -1;
		RefreshSelectionMarks();
		if (narrow)
		{
			OpenPage();
			return;
		}
		ShowPage();
		if (rows.TryGetValue(entry.Id, out Button row) && IsInstanceValid(row))
		{
			row.GrabFocus();
			Callable.From(ScrollToSelected).CallDeferred();
		}
	}

	/// <summary>The next or previous entry in the index's order: a phone's swipe, or LB/RB on a turned page.</summary>
	private void TurnEntry(int step)
	{
		List<Button> ordered = OrderedRows();
		if (ordered.Count == 0)
			return;
		int at = ordered.FindIndex(b => IdOf(b) == selectedId);
		int next = Mathf.Clamp(at + step, 0, ordered.Count - 1);
		if (next == at)
			return;
		selectedId = IdOf(ordered[next]);
		litNodeId = string.Empty;
		formShown = -1;
		RefreshSelectionMarks();
		OpenPage();
		SfxPlayer.Global(SfxCatalog.UiClick);
	}

	// ---- bookmarks and search -----------------------------------------------------------------------

	private void SetFilter(CodexFilter next, bool keepFocusOnTab)
	{
		if (next == CodexFilter.All)
			next = CodexFilter.Spells;
		filter = next;
		for (int i = 0; i < tabs.Count; i++)
			tabs[i].SetPressedNoSignal(Bookmarks[i] == next);
		RebuildIndex();
		if (IsInstanceValid(indexScroll))
			indexScroll.ScrollVertical = 0;
		// The selection stays on its entry when the bookmark still shows it; otherwise the bookmark's first.
		if (!rows.ContainsKey(selectedId))
		{
			selectedId = IdOf(OrderedRows().FirstOrDefault()) ?? selectedId;
			litNodeId = string.Empty;
			formShown = -1;
			RefreshSelectionMarks();
		}
		if (!narrow)
			ShowPage();
		if (keepFocusOnTab)
			TabFor(next)?.GrabFocus();
		SfxPlayer.Global(SfxCatalog.UiClick);
	}

	/// <summary>LB / RB: the next or previous bookmark, the selection kept in the index.</summary>
	private void CycleFilter(int step)
	{
		int at = Array.IndexOf(Bookmarks, filter);
		CodexFilter next = Bookmarks[((at < 0 ? 0 : at) + step + Bookmarks.Length) % Bookmarks.Length];
		bool onTab = tabs.Contains(GetViewport()?.GuiGetFocusOwner());
		SetFilter(next, keepFocusOnTab: onTab);
		if (!onTab && !KeyboardOpen)
		{
			if (rows.TryGetValue(selectedId, out Button row) && IsInstanceValid(row))
				row.GrabFocus();
			else
				OrderedRows().FirstOrDefault()?.GrabFocus();
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
		// One field searches every name. The index filters live as letters arrive and the selection moves to
		// the best hit; when the open bookmark holds no match the book turns to the one the best hit is under.
		CodexSearchHit best = string.IsNullOrWhiteSpace(query) ? null : model.Search(query, 1).FirstOrDefault();
		if (best != null && model.Get(best.EntryId) is CodexEntry hitEntry && hitEntry.Group != filter
			&& model.Filtered(filter, query).Count == 0)
		{
			filter = hitEntry.Group;
			for (int i = 0; i < tabs.Count; i++)
				tabs[i].SetPressedNoSignal(Bookmarks[i] == filter);
		}
		RebuildIndex();
		if (best != null && rows.ContainsKey(best.EntryId))
		{
			selectedId = best.EntryId;
			litNodeId = best.NodeId;
			formShown = -1;
			RefreshSelectionMarks();
		}
		if (!rows.ContainsKey(selectedId))
			selectedId = IdOf(OrderedRows().FirstOrDefault()) ?? selectedId;
		if (!narrow)
			ShowPage();
		keyboard?.ShowQuery(query);
		keyboard?.ShowSuggestions(model.Search(query, CodexKeyboard.MaxSuggestions));
	}

	// ---- the pad's keyboard -------------------------------------------------------------------------

	private void OpenKeyboard()
	{
		if (keyboard == null || keyboard.Visible)
			return;
		ClosePageQuietly();
		keyboard.Open(narrow, query, model.Search(query, CodexKeyboard.MaxSuggestions));
		SfxPlayer.Global(SfxCatalog.UiOpen);
	}

	private void CloseKeyboard()
	{
		if (keyboard == null || !keyboard.Visible)
			return;
		keyboard.Close();
		// Back on the index, on the best hit.
		if (rows.TryGetValue(selectedId, out Button row) && IsInstanceValid(row))
			row.GrabFocus();
		else
			OrderedRows().FirstOrDefault()?.GrabFocus();
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
		SetQuery(hit.Name);
		SelectEntry(hit.EntryId, hit.NodeId);
		if (!narrow && rows.TryGetValue(selectedId, out Button row) && IsInstanceValid(row))
			row.GrabFocus();
	}

	// ---- input --------------------------------------------------------------------------------------

	private Vector2? swipeFrom;

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

		// A phone's page turns under a swipe (a drag mostly sideways, a fifth of the screen wide). Watched, never
		// consumed: a drag that is not a swipe still scrolls the page.
		if (PageOpen && @event is InputEventScreenTouch touch)
		{
			if (touch.Pressed)
				swipeFrom = touch.Position;
			else if (swipeFrom is Vector2 from)
			{
				swipeFrom = null;
				Vector2 moved = touch.Position - from;
				float width = GetViewport().GetVisibleRect().Size.X;
				if (Mathf.Abs(moved.X) > width * 0.2f && Mathf.Abs(moved.X) > Mathf.Abs(moved.Y) * 2f)
					TurnEntry(moved.X < 0f ? 1 : -1);
			}
		}

		if (@event is InputEventJoypadButton { Pressed: true } pad)
		{
			switch (pad.ButtonIndex)
			{
				case JoyButton.LeftShoulder:
				case JoyButton.RightShoulder:
					int step = pad.ButtonIndex == JoyButton.RightShoulder ? 1 : -1;
					// On a turned page the shoulders turn to the next entry; on the index they change bookmark.
					if (PageOpen)
						TurnEntry(step);
					else
						CycleFilter(step);
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

	/// <summary>B: the page to its index line, the turned page back to the index, the index out of the Codex.</summary>
	private void StepBack()
	{
		if (KeyboardOpen)
		{
			CloseKeyboard();
			return;
		}
		if (PageOpen)
		{
			ClosePage();
			return;
		}
		Control owner = GetViewport()?.GuiGetFocusOwner();
		if (owner != null && page != null && page.IsAncestorOf(owner) && rows.TryGetValue(selectedId, out Button row))
		{
			row.GrabFocus();
			SfxPlayer.Global(SfxCatalog.UiBack);
			return;
		}
		Close();
	}

	/// <summary>A phone turns from the index to the selected entry's page.</summary>
	private void OpenPage()
	{
		if (entryPage == null)
			return;
		ShowPage();
		indexPage.Visible = false;
		entryPage.Visible = true;
		if (entryTitle != null)
			entryTitle.Text = model.Get(selectedId)?.ShownName ?? string.Empty;
		if (IsInstanceValid(pageScroll))
			pageScroll.ScrollVertical = 0;
		Control first = FirstTreeStop() ?? indexButton;
		first?.GrabFocus();
		SfxPlayer.Global(SfxCatalog.UiOpen);
	}

	private void ClosePage()
	{
		if (!PageOpen)
			return;
		ClosePageQuietly();
		// Back to the index, the same line selected (section 2).
		if (rows.TryGetValue(selectedId, out Button row) && IsInstanceValid(row))
		{
			row.GrabFocus();
			Callable.From(ScrollToSelected).CallDeferred();
		}
		SfxPlayer.Global(SfxCatalog.UiBack);
	}

	private void ClosePageQuietly()
	{
		if (entryPage == null)
			return;
		entryPage.Visible = false;
		indexPage.Visible = true;
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
/// The Codex's inks and grounds, in one place. The book's are the level-up screen's manuscript (CodexRows.Vellum)
/// and a few washes of it; the keyboard's are the stone register's. Measured in tools/art/ui_contrast.py
/// ("codex book", "codex keyboard"): change one, change both.
/// </summary>
public static class CodexInk
{
	// --- the book (parchment) ---
	/// <summary>A node's face: the page a step lighter.</summary>
	public static readonly Color BookNode = new(0.965f, 0.918f, 0.824f);
	/// <summary>The selected index line, a taken node: the page washed in gold.</summary>
	public static readonly Color BookSelected = new(0.890f, 0.788f, 0.561f);
	/// <summary>An index line under the pointer or the stick: the lightest gold wash.</summary>
	public static readonly Color BookHover = new(0.922f, 0.851f, 0.722f);
	/// <summary>The search field on the page.</summary>
	public static readonly Color BookField = new(0.973f, 0.937f, 0.863f);
	/// <summary>A met requirement's pill: the page a step darker.</summary>
	public static readonly Color BookPill = new(0.890f, 0.812f, 0.663f);
	/// <summary>A shut bookmark, and one under the pointer.</summary>
	public static readonly Color BookmarkShut = new(0.706f, 0.604f, 0.447f);
	public static readonly Color BookmarkHover = new(0.800f, 0.706f, 0.553f);
	/// <summary>The rubric band an unmet requirement sits on, and its ink (the browser's, LevelUpMenu).</summary>
	public static readonly Color UnmetBand = new(0.896f, 0.789f, 0.708f);
	public static readonly Color UnmetInk = new(0.416f, 0.125f, 0.157f);
	/// <summary>The way to an Apotheosis on the page: a burnt orange (3:1 and over as a graphic on the vellum).</summary>
	public static readonly Color EmberLine = new(0.702f, 0.290f, 0.071f);
	/// <summary>The Apotheosis node: an ember-dark face, its words in the warm light, its rim in fire.</summary>
	public static readonly Color EmberFace = new(0.165f, 0.078f, 0.063f);
	public static readonly Color EmberPill = new(0.231f, 0.125f, 0.094f);
	public static readonly Color EmberInk = new(1.0f, 0.839f, 0.659f);
	public static readonly Color EmberSoft = new(1.0f, 0.698f, 0.478f);
	public static readonly Color EmberRim = new(0.910f, 0.412f, 0.180f);

	// --- the keyboard (stone) ---
	/// <summary>stone.deep: the keyboard's panel.</summary>
	public static readonly Color StoneDeep = new(0.043f, 0.059f, 0.094f);
	/// <summary>The heading band's gold rule (JournalScreen's).</summary>
	public static readonly Color BandRule = new(0.541f, 0.416f, 0.118f);
	/// <summary>A pressed key: stone deep, a step lighter.</summary>
	public static readonly Color FieldFace = new(0.071f, 0.090f, 0.129f);
}
