using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

/// <summary>
/// The Codex's on-screen keyboard, for a pad (.ai/evolution-codex.md section 2; Eric: "controller should have
/// search in the most user friendly way possible. On screen keyboard?"). Godot draws none on a desktop, so this
/// is ours: the letters in an A-Z grid - alphabetical, which a pad finds faster than QWERTY - with Space,
/// Delete, Clear and Done, and above the keys a row of the best-matching names, so two or three letters and
/// one press usually finish the search.
/// </summary>
/// <remarks>
/// The stick or the D-pad walks the keys, A types, B deletes a letter, X clears, Start or Done closes; RB moves
/// the selection into the suggestions and LB back to the keys. The Codex filters its grid live under it as
/// letters arrive. A phone types into the search field with its own keyboard and a PC keyboard straight into
/// the field: this opens only on Y, or on A with the field selected - a pad's two ways in. Letters typed on a
/// real keyboard while it is open are typed too, so nobody is ever stuck in it.
///
/// Every key's four neighbours are set by hand and wrap inside the panel, so the stick can never walk out of
/// the keyboard onto the grid behind it. The panel is stone with the heading band's gold rule; the keys are
/// the stone register's face and edge, gold when selected - no new art (CodexInk, ui_contrast.py "codex").
/// </remarks>
public partial class CodexKeyboard : PanelContainer
{
	public const int MaxSuggestions = 4;
	private const int Columns = 7;

	public event Action<string> Typed;
	public event Action Erased;
	public event Action Cleared;
	public event Action Done;
	public event Action<CodexSearchHit> SuggestionChosen;

	private bool built;
	private bool narrow;
	private Label typed;
	private HBoxContainer suggestionRow;
	private readonly List<Button> keys = new();
	private readonly List<Button> suggestions = new();
	private List<CodexSearchHit> hits = new();
	private Button clearKey;
	private Button doneKey;

	public IReadOnlyList<Button> Suggestions => suggestions;

	public void Open(bool narrowLayout, string query, List<CodexSearchHit> found)
	{
		if (!built || narrow != narrowLayout)
			Build(narrowLayout);
		Visible = true;
		ShowQuery(query);
		ShowSuggestions(found);
		keys.FirstOrDefault()?.GrabFocus();
	}

	public void Close()
	{
		Visible = false;
	}

	/// <summary>
	/// Anchored to the foot of the screen, so the grid it filters stays in sight above it. Called as the Codex
	/// builds, BEFORE SafeArea.Apply: the safe area insets a control once (it marks it), so anchors set later
	/// would never be pulled in from a phone's rounded corners.
	/// </summary>
	public void Place(bool narrowLayout)
	{
		AnchorLeft = narrowLayout ? 0f : 0.5f;
		AnchorRight = narrowLayout ? 1f : 0.5f;
		AnchorTop = 1f;
		AnchorBottom = 1f;
		const float halfWidth = 330f;
		OffsetLeft = narrowLayout ? 16f : -halfWidth;
		OffsetRight = narrowLayout ? -16f : halfWidth;
		OffsetBottom = narrowLayout ? -24f : -18f;
		OffsetTop = OffsetBottom;
		GrowVertical = GrowDirection.Begin;
	}

	private void Build(bool narrowLayout)
	{
		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.QueueFree();
		}
		keys.Clear();
		suggestions.Clear();
		narrow = narrowLayout;
		built = true;
		MouseFilter = MouseFilterEnum.Stop;

		var style = new StyleBoxFlat { BgColor = CodexInk.StoneDeep, BorderColor = CodexInk.BandRule };
		style.SetBorderWidthAll(2);
		style.BorderWidthTop = 3;
		style.SetCornerRadiusAll(6);
		style.SetContentMarginAll(14);
		style.ShadowColor = new Color(0f, 0f, 0f, 0.6f);
		style.ShadowSize = 12;
		AddThemeStyleboxOverride("panel", style);

		var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 10);
		AddChild(column);

		var head = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		head.AddThemeConstantOverride("separation", 12);
		Label title = CodexRows.MakeText("Search", ResponsiveLayout.TextRole.Title, CodexRows.Stone.Heading);
		title.AutowrapMode = TextServer.AutowrapMode.Off;
		head.AddChild(title);
		typed = CodexRows.MakeText(string.Empty, ResponsiveLayout.TextRole.Title, CodexRows.Stone.Ink);
		typed.Name = "CodexKeyboardTyped";
		typed.AutowrapMode = TextServer.AutowrapMode.Off;
		typed.ClipText = true;
		typed.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		head.AddChild(typed);
		column.AddChild(head);

		suggestionRow = new HBoxContainer { Name = "CodexSuggestions", MouseFilter = MouseFilterEnum.Ignore };
		suggestionRow.AddThemeConstantOverride("separation", 8);
		suggestionRow.CustomMinimumSize = new Vector2(0f, ResponsiveLayout.MinTouchTarget);
		column.AddChild(suggestionRow);

		var grid = new GridContainer { Name = "CodexKeys", Columns = Columns, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		grid.AddThemeConstantOverride("h_separation", 6);
		grid.AddThemeConstantOverride("v_separation", 6);
		column.AddChild(grid);
		for (char letter = 'A'; letter <= 'Z'; letter++)
		{
			string text = letter.ToString();
			grid.AddChild(Key($"CodexKey_{text}", text, () => Typed?.Invoke(text.ToLowerInvariant())));
		}
		grid.AddChild(Key("CodexKey_Space", "Space", () => Typed?.Invoke(" ")));
		grid.AddChild(Key("CodexKey_Delete", "⌫", () => Erased?.Invoke()));

		var foot = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		foot.AddThemeConstantOverride("separation", 8);
		column.AddChild(foot);
		clearKey = Key("CodexKey_Clear", "Clear  (X)", () => Cleared?.Invoke(), register: false);
		clearKey.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		foot.AddChild(clearKey);
		doneKey = Key("CodexKey_Done", "Done  (Start)", () => Done?.Invoke(), register: false);
		doneKey.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		foot.AddChild(doneKey);
		Label hint = CodexRows.MakeText("A type  ·  B delete  ·  RB names", ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkSoft);
		hint.HorizontalAlignment = HorizontalAlignment.Center;
		column.AddChild(hint);
		WireKeys();
	}

	private Button Key(string name, string text, Action pressed, bool register = true)
	{
		var key = new Button
		{
			Name = name,
			Text = text,
			FocusMode = FocusModeEnum.All,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(narrow ? 0f : 52f, narrow ? 58f : ResponsiveLayout.MinTouchTarget),
		};
		ResponsiveLayout.SetFont(key, text.Length == 1 ? ResponsiveLayout.TextRole.Title : ResponsiveLayout.TextRole.Label);
		StyleKey(key);
		key.Pressed += () =>
		{
			pressed();
			SfxPlayer.Global(SfxCatalog.UiClick);
		};
		if (register)
			keys.Add(key);
		return key;
	}

	private static void StyleKey(Button key)
	{
		StyleBoxFlat Cap(Color face, Color edge, int width)
		{
			var box = new StyleBoxFlat { BgColor = face, BorderColor = edge };
			box.SetBorderWidthAll(width);
			// A carved key: a darker lip under it, the stone's own.
			box.BorderWidthBottom = width + 2;
			box.SetCornerRadiusAll(3);
			box.ContentMarginLeft = 6;
			box.ContentMarginRight = 6;
			box.ContentMarginTop = 2;
			box.ContentMarginBottom = 2;
			return box;
		}
		key.AddThemeStyleboxOverride("normal", Cap(CodexRows.Stone.Face, CodexRows.Stone.Edge, 2));
		key.AddThemeStyleboxOverride("hover", Cap(CodexRows.Stone.Face, CodexRows.Stone.Reward, 2));
		key.AddThemeStyleboxOverride("focus", Cap(CodexRows.Stone.Face, CodexRows.Stone.Reward, 2));
		key.AddThemeStyleboxOverride("pressed", Cap(CodexInk.FieldFace, CodexRows.Stone.Reward, 2));
		key.AddThemeColorOverride("font_color", CodexRows.Stone.Ink);
		key.AddThemeColorOverride("font_hover_color", CodexRows.Stone.Heading);
		key.AddThemeColorOverride("font_focus_color", CodexRows.Stone.Heading);
		key.AddThemeColorOverride("font_pressed_color", CodexRows.Stone.Heading);
	}

	public void ShowQuery(string query)
	{
		if (typed != null)
			typed.Text = string.IsNullOrEmpty(query) ? "_" : $"{query}_";
	}

	/// <summary>The best-matching names over the keys. Rebuilt as each letter arrives.</summary>
	public void ShowSuggestions(List<CodexSearchHit> found)
	{
		if (!built || suggestionRow == null)
			return;
		hits = found ?? new List<CodexSearchHit>();
		bool focusWasHere = suggestions.Any(s => s.HasFocus());
		foreach (Node child in suggestionRow.GetChildren())
		{
			suggestionRow.RemoveChild(child);
			child.QueueFree();
		}
		suggestions.Clear();
		if (hits.Count == 0)
		{
			Label none = CodexRows.MakeText(typed?.Text == "_" ? "Type a few letters." : "No name matches.", ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkSoft);
			none.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			suggestionRow.AddChild(none);
		}
		for (int i = 0; i < Math.Min(MaxSuggestions, hits.Count); i++)
		{
			CodexSearchHit hit = hits[i];
			var button = new Button
			{
				Name = $"CodexSuggestion_{i}",
				Text = hit.Name,
				TooltipText = hit.What,
				FocusMode = FocusModeEnum.All,
				ClipText = true,
				TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(0f, ResponsiveLayout.MinTouchTarget),
			};
			ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Label);
			StyleKey(button);
			// The suggestion's ink is the gold of a heading: a name to take, not a letter to type.
			button.AddThemeColorOverride("font_color", CodexRows.Stone.Heading);
			button.Pressed += () => SuggestionChosen?.Invoke(hit);
			suggestionRow.AddChild(button);
			suggestions.Add(button);
		}
		WireKeys();
		if (focusWasHere)
			(suggestions.FirstOrDefault() as Control ?? keys.FirstOrDefault())?.GrabFocus();
	}

	/// <summary>Every key's four neighbours, wrapping inside the panel: the stick never leaves the keyboard.</summary>
	private void WireKeys()
	{
		if (keys.Count == 0 || !IsInsideTree())
		{
			if (keys.Count > 0)
				Callable.From(WireKeys).CallDeferred();
			return;
		}
		int rows = (keys.Count + Columns - 1) / Columns;
		Button At(int row, int col)
		{
			int index = row * Columns + col;
			return index < keys.Count ? keys[index] : keys[^1];
		}
		for (int i = 0; i < keys.Count; i++)
		{
			int row = i / Columns;
			int col = i % Columns;
			int rowLength = Math.Min(Columns, keys.Count - row * Columns);
			Button key = keys[i];
			Link(key, At(row, (col + 1) % rowLength), Side.Right);
			Link(key, At(row, (col - 1 + rowLength) % rowLength), Side.Left);
			Control up = row > 0 ? At(row - 1, col) : NearestSuggestion(col) ?? (Control)doneKey;
			Link(key, up, Side.Top);
			Control down = row + 1 < rows ? At(row + 1, Math.Min(col, Math.Min(Columns, keys.Count - (row + 1) * Columns) - 1))
				: col < Columns / 2 ? clearKey : doneKey;
			Link(key, down, Side.Bottom);
		}
		Button lastRowFirst = keys[(rows - 1) * Columns];
		Link(clearKey, doneKey, Side.Right);
		Link(clearKey, doneKey, Side.Left);
		Link(doneKey, clearKey, Side.Right);
		Link(doneKey, clearKey, Side.Left);
		Link(clearKey, lastRowFirst, Side.Top);
		Link(doneKey, keys[^1], Side.Top);
		Link(clearKey, (Control)suggestions.FirstOrDefault() ?? keys[0], Side.Bottom);
		Link(doneKey, (Control)suggestions.FirstOrDefault() ?? keys[0], Side.Bottom);
		for (int i = 0; i < suggestions.Count; i++)
		{
			Button s = suggestions[i];
			Link(s, suggestions[(i + 1) % suggestions.Count], Side.Right);
			Link(s, suggestions[(i - 1 + suggestions.Count) % suggestions.Count], Side.Left);
			Link(s, doneKey, Side.Top);
			Link(s, keys[Math.Min(keys.Count - 1, i * Columns / Math.Max(1, suggestions.Count))], Side.Bottom);
		}
	}

	private Button NearestSuggestion(int col)
	{
		if (suggestions.Count == 0)
			return null;
		return suggestions[Math.Min(suggestions.Count - 1, col * suggestions.Count / Columns)];
	}

	private static void Link(Control from, Control to, Side side)
	{
		if (from == null || to == null || !from.IsInsideTree() || !to.IsInsideTree())
			return;
		NodePath path = from == to ? new NodePath(".") : from.GetPathTo(to);
		switch (side)
		{
			case Side.Top: from.FocusNeighborTop = path; break;
			case Side.Bottom: from.FocusNeighborBottom = path; break;
			case Side.Left: from.FocusNeighborLeft = path; break;
			case Side.Right: from.FocusNeighborRight = path; break;
		}
	}

	/// <summary>
	/// The pad's buttons while the keyboard is open (the Codex hands every event here first). True when the
	/// event was the keyboard's: the stick and A are left to the keys themselves.
	/// </summary>
	public bool HandleInput(InputEvent @event)
	{
		if (!Visible)
			return false;
		if (@event is InputEventJoypadButton { Pressed: true } pad)
		{
			switch (pad.ButtonIndex)
			{
				case JoyButton.B:
					Erased?.Invoke();
					return true;
				case JoyButton.X:
					Cleared?.Invoke();
					return true;
				case JoyButton.Start:
					Done?.Invoke();
					return true;
				case JoyButton.RightShoulder:
					suggestions.FirstOrDefault()?.GrabFocus();
					return true;
				case JoyButton.LeftShoulder:
					keys.FirstOrDefault()?.GrabFocus();
					return true;
				case JoyButton.Y:
				case JoyButton.Back:
					return true;
			}
			return false;
		}
		if (@event is InputEventJoypadButton)
			return IsKeyboardRelease(@event);
		if (@event is InputEventKey { Pressed: true } key)
		{
			if (key.Keycode == Godot.Key.Escape)
			{
				Done?.Invoke();
				return true;
			}
			if (key.Keycode == Godot.Key.Backspace)
			{
				Erased?.Invoke();
				return true;
			}
			// A real keyboard types as well: a letter, a digit, an apostrophe, a space.
			char c = (char)key.Unicode;
			if (key.Unicode > 0 && (char.IsLetterOrDigit(c) || c == ' ' || c == '\'' || c == '-'))
			{
				Typed?.Invoke(char.ToLowerInvariant(c).ToString());
				return true;
			}
		}
		return false;
	}

	// A released pad button is the keyboard's only if its press was (B, X, Start, the shoulders, Y).
	private static bool IsKeyboardRelease(InputEvent @event) =>
		@event is InputEventJoypadButton { Pressed: false } up
		&& up.ButtonIndex is JoyButton.B or JoyButton.X or JoyButton.Start or JoyButton.RightShoulder or JoyButton.LeftShoulder or JoyButton.Y;
}
