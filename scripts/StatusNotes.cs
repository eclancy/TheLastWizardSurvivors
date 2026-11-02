using Godot;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

/// <summary>
/// What a status does: the card a <see cref="StatusKeyword"/> opens, and - for a pad - the notes a
/// focused card's statuses show beside it.
/// </summary>
/// <remarks>
/// Eric, 2026-11-02: "On controller, just show the status descriptions when focused." A pad cannot point
/// at a word, and a stop on every keyword would make a hand of three cards twelve presses wide. So this
/// watches the selection, once for the whole game (started by the first keyword to enter the tree): when
/// the control that takes focus - or, for a card whose Button is a sibling of its words (the chest page,
/// a phone's level-up card), the panel holding both - names a status, every status it names is listed in
/// a stone note BESIDE that card, below it if there is room, then above, then to either side, so the
/// card's own words are never under it. It goes when the selection moves on, when the card leaves the
/// screen, and the moment the mouse or a finger is the pointer again (MenuFocusHighlight.PointerActive):
/// a mouse user reads a keyword by clicking it.
///
/// On a layer just under the popover's (ElementTagPopover, 150), so a keyword's own card opened over a
/// focused card's notes is on top. Nothing on it takes the mouse.
/// </remarks>
public partial class StatusNotes : CanvasLayer
{
	private const int NotesLayer = 149;
	private const float MaxWidth = 420f;
	private const float MinSideWidth = 260f;
	private const float EdgeMargin = 12f;
	private const float Gap = 8f;

	private static StatusNotes instance;

	/// <summary>
	/// Meta on a focusable control whose statuses are drawn somewhere else: the Codex's index row, whose
	/// entry's words are on the page beside the index. The value is that node.
	/// </summary>
	public const string SourceMeta = "status_notes_source";

	/// <summary>Reads the selection again: for a screen that rebuilt what the focused control stands for.</summary>
	public static void Refresh()
	{
		if (instance != null && IsInstanceValid(instance) && instance.IsInsideTree())
			instance.OnFocusChanged(instance.GetViewport().GuiGetFocusOwner());
	}
	private Control anchor;
	private PanelContainer notes;
	private bool placing;

	/// <summary>Starts the watcher if it is not running. Called by every keyword as it enters the tree.</summary>
	public static void EnsureWatching(Node any)
	{
		if (instance != null && IsInstanceValid(instance))
			return;
		if (any?.GetTree()?.Root is not Window root)
			return;
		instance = new StatusNotes { Name = "StatusNotes", Layer = NotesLayer, ProcessMode = ProcessModeEnum.Always };
		root.CallDeferred(Node.MethodName.AddChild, instance);
	}

	public override void _Ready()
	{
		GetViewport().GuiFocusChanged += OnFocusChanged;
		// A selection made before the watcher existed (the first card of the first screen with a keyword).
		if (GetViewport().GuiGetFocusOwner() is Control focused)
			OnFocusChanged(focused);
	}

	public override void _ExitTree()
	{
		GetViewport().GuiFocusChanged -= OnFocusChanged;
		if (instance == this)
			instance = null;
	}

	// A phone with no pad never shows them: its selection is a finger's, and a focused first card on a
	// chest the run just opened is not a question the player asked.
	private static bool PadPossible => !DisplayServer.IsTouchscreenAvailable() || Input.GetConnectedJoypads().Count > 0;

	private void OnFocusChanged(Control focused)
	{
		if (MenuFocusHighlight.PointerActive || focused == null || !PadPossible)
		{
			HideNotes();
			return;
		}
		Control holder = focused;
		List<StatusId> statuses = focused.HasMeta(SourceMeta) && focused.GetMeta(SourceMeta).AsGodotObject() is Node source
			&& IsInstanceValid(source) ? StatusesUnder(source) : StatusesUnder(focused);
		// The card whose Button is a sibling of its words: the panel holding both.
		if (statuses.Count == 0 && focused.GetParent() is Control parent && parent is PanelContainer or MarginContainer)
		{
			statuses = StatusesUnder(parent);
			holder = parent;
		}
		if (statuses.Count == 0)
		{
			HideNotes();
			return;
		}
		Show(holder, statuses);
	}

	private static List<StatusId> StatusesUnder(Node root)
	{
		var found = new List<StatusId>();
		Collect(root, found);
		return found;
	}

	private static void Collect(Node node, List<StatusId> found)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is StatusKeyword keyword && keyword.IsVisibleInTree() && !found.Contains(keyword.Status))
				found.Add(keyword.Status);
			Collect(child, found);
		}
	}

	private void Show(Control holder, List<StatusId> statuses)
	{
		HideNotes();
		anchor = holder;
		notes = BuildList(statuses);
		float width = Mathf.Min(MaxWidth, ResponsiveLayout.ViewportSize(holder).X - EdgeMargin * 2f);
		notes.CustomMinimumSize = new Vector2(width, 0f);
		notes.Name = "FocusedStatuses";
		AddChild(notes);
		notes.Resized += Place;
		Callable.From(Place).CallDeferred();
	}

	private void HideNotes()
	{
		anchor = null;
		if (notes != null && IsInstanceValid(notes))
			notes.QueueFree();
		notes = null;
	}

	public override void _Process(double delta)
	{
		if (notes == null)
			return;
		Control focused = GetViewport().GuiGetFocusOwner();
		if (anchor == null || !IsInstanceValid(anchor) || !anchor.IsVisibleInTree() || MenuFocusHighlight.PointerActive
			|| focused == null || (focused != anchor && !anchor.IsAncestorOf(focused) && focused.GetParent() != anchor))
		{
			HideNotes();
			return;
		}
		Place();
	}

	/// <summary>The rect that must stay clear, and where the notes go round it.</summary>
	private void Place()
	{
		if (notes == null || anchor == null || !IsInstanceValid(anchor) || !anchor.IsInsideTree())
			return;
		if (!placing)
		{
			placing = true;
			notes.ResetSize();
			placing = false;
		}

		Transform2D xform = anchor.GetGlobalTransformWithCanvas();
		var card = new Rect2(xform.Origin, anchor.Size * xform.Scale);
		Vector2 screen = ResponsiveLayout.ViewportSize(anchor);
		var (left, top, right, bottom) = SafeArea.Insets(anchor.GetViewport());
		float minX = left + EdgeMargin, maxX = screen.X - right - EdgeMargin;
		float minY = top + EdgeMargin, maxY = screen.Y - bottom - EdgeMargin;
		Vector2 size = notes.Size;

		float cx = Mathf.Clamp(card.GetCenter().X - size.X * 0.5f, minX, Mathf.Max(minX, maxX - size.X));
		Vector2 at;
		if (card.End.Y + Gap + size.Y <= maxY)
			at = new Vector2(cx, card.End.Y + Gap);
		else if (card.Position.Y - Gap - size.Y >= minY)
			at = new Vector2(cx, card.Position.Y - Gap - size.Y);
		else if (maxX - (card.End.X + Gap) >= Mathf.Min(size.X, MinSideWidth))
			at = new Vector2(card.End.X + Gap, card.Position.Y);
		else if (card.Position.X - Gap - minX >= Mathf.Min(size.X, MinSideWidth))
			at = new Vector2(card.Position.X - Gap - size.X, card.Position.Y);
		else
			at = new Vector2(cx, maxY - size.Y);
		at.X = Mathf.Clamp(at.X, minX, Mathf.Max(minX, maxX - size.X));
		at.Y = Mathf.Clamp(at.Y, minY, Mathf.Max(minY, maxY - size.Y));
		notes.Position = new Vector2(Mathf.Round(at.X), Mathf.Round(at.Y));
	}

	// --- the cards --------------------------------------------------------------------------------

	/// <summary>One status's card, for a keyword's popover and its PC hover tooltip.</summary>
	public static PanelContainer BuildCard(StatusId status) => BuildList(new[] { status });

	/// <summary>
	/// Stone, like the element card (ElementTagPopover.BuildCard), in its inks (CodexRows.Stone): each
	/// status's name on its own colour, its sentence under it, and the rule all of them share at the foot.
	/// </summary>
	public static PanelContainer BuildList(IEnumerable<StatusId> statuses)
	{
		CodexRows.Palette ink = CodexRows.Stone;
		var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		var style = new StyleBoxFlat
		{
			BgColor = ink.Face,
			BorderColor = new Color(0.847f, 0.690f, 0.290f),
			ShadowColor = new Color(0f, 0f, 0f, 0.45f),
			ShadowSize = 8,
		};
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(6);
		style.ContentMarginLeft = 16;
		style.ContentMarginRight = 16;
		style.ContentMarginTop = 12;
		style.ContentMarginBottom = 14;
		panel.AddThemeStyleboxOverride("panel", style);

		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 8);
		panel.AddChild(column);

		foreach (StatusId status in statuses.Distinct())
		{
			KeywordText.StatusInfo info = KeywordText.Statuses[status];
			var line = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			line.AddThemeConstantOverride("separation", 4);
			Color fill = CodexRows.ChipFill(ElementColors.GetColor(info.Element));
			var swatch = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
			var swatchStyle = new StyleBoxFlat { BgColor = fill, BorderColor = fill.Darkened(0.35f) };
			swatchStyle.SetBorderWidthAll(1);
			swatchStyle.SetCornerRadiusAll(4);
			swatchStyle.ContentMarginLeft = 10;
			swatchStyle.ContentMarginRight = 10;
			swatchStyle.ContentMarginTop = 1;
			swatchStyle.ContentMarginBottom = 1;
			swatch.AddThemeStyleboxOverride("panel", swatchStyle);
			Label name = CodexRows.MakeText(info.Name, ResponsiveLayout.TextRole.Label, StatIcons.ReadableOn(fill));
			name.AutowrapMode = TextServer.AutowrapMode.Off;
			swatch.AddChild(name);
			line.AddChild(swatch);
			line.AddChild(CodexRows.MakeText(info.Sentence, ResponsiveLayout.TextRole.Label, ink.Ink));
			column.AddChild(line);
		}
		column.AddChild(CodexRows.MakeText(KeywordText.StackingNote, ResponsiveLayout.TextRole.Label, ink.InkMeta));
		return panel;
	}
}
