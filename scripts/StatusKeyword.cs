using Godot;
using TheOneHeMissed.scripts;

/// <summary>
/// A status word in a description (KeywordText) that opens what the status does: "Weakened", "Withers",
/// "slowed". Eric, 2026-11-02: "Status effects should be outlined or underlined or something that looks
/// clickable. When clicked, they should show the description of what that debuff does."
/// </summary>
/// <remarks>
/// THE WORD STAYS IN THE LINE'S INK, the underline carries the status's colour. The status colours
/// (KeywordText.Statuses) are the debuff marks' element ramps, and several of them - Light's gold, Ice's
/// pale blue - are no ink on parchment; as a 2px rule under a word they only need the 3:1 a non-text mark
/// does, and the light-ground and dark-ground pairs are each measured for that (ui_contrast.py,
/// keyword_pairs). One look everywhere: underlined, a pointing hand, a faint wash of the colour on hover.
///
/// A BUTTON, FOR THE SAME REASONS AN ELEMENT CHIP IS ONE (ElementChip's remarks): it fires on release and
/// drops the press when a scroll takes it, and it stops the mouse, so a tap on the word never claims the
/// card under it. It takes no focus: the stick stops on cards, and a focused card shows its statuses
/// itself (StatusNotes) - a stop on every word would make a hand of three cards twelve presses wide.
/// The press opens the card through ElementTagPopover, the one popover the menus have.
/// </remarks>
public partial class StatusKeyword : Button
{
	public StatusId Status { get; private set; }

	public static StatusKeyword Create(StatusId status, string word, Color ink, int fontSize, Font font)
	{
		var keyword = new StatusKeyword
		{
			Name = $"StatusKeyword_{status}",
			Status = status,
			Text = word,
			FocusMode = FocusModeEnum.None,
			MouseFilter = MouseFilterEnum.Stop,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			SizeFlagsVertical = SizeFlags.ShrinkBegin,
			ClipText = false,
		};
		keyword.AddThemeFontSizeOverride("font_size", fontSize);
		if (font != null)
			keyword.AddThemeFontOverride("font", font);
		foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color",
			"font_focus_color", "font_hover_pressed_color" })
			keyword.AddThemeColorOverride(state, ink);

		Color line = KeywordText.UnderlineFor(status, ink);
		StyleBoxFlat Face(float wash)
		{
			var style = new StyleBoxFlat { BgColor = new Color(line, wash), BorderColor = line, DrawCenter = wash > 0f };
			style.BorderWidthBottom = 2;
			style.SetCornerRadiusAll(0);
			style.ContentMarginLeft = 0;
			style.ContentMarginRight = 0;
			style.ContentMarginTop = 0;
			style.ContentMarginBottom = 0;
			return style;
		}
		keyword.AddThemeStyleboxOverride("normal", Face(0f));
		keyword.AddThemeStyleboxOverride("hover", Face(0.16f));
		keyword.AddThemeStyleboxOverride("pressed", Face(0.24f));
		keyword.AddThemeStyleboxOverride("hover_pressed", Face(0.24f));
		keyword.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

		if (!DisplayServer.IsTouchscreenAvailable())
			keyword.TooltipText = KeywordText.Statuses[status].Name;

		keyword.Pressed += () =>
		{
			SfxPlayer.Global(SfxCatalog.UiClick);
			ElementTagPopover.Toggle(keyword, () => StatusNotes.BuildCard(keyword.Status));
		};
		return keyword;
	}

	public override void _EnterTree()
	{
		// The watcher that shows a focused card's statuses to a pad: started by the first keyword on
		// any screen, one for the whole game (StatusNotes).
		StatusNotes.EnsureWatching(this);
	}

	// The hover card on a PC is the card a click opens, built by the same code.
	public override GodotObject _MakeCustomTooltip(string forText)
	{
		var card = StatusNotes.BuildCard(Status);
		// Native TooltipPanel windows measure their contents before containers have laid them out.
		// Give the wrapped sentence its width up front, or its initial one-word-per-line height
		// can permanently inflate the popup even after the card has wrapped correctly.
		float width = Mathf.Min(420f, ResponsiveLayout.ViewportSize(this).X - 40f);
		card.CustomMinimumSize = new Vector2(Mathf.Max(1f, width), 0f);
		foreach (Node node in card.FindChildren("*", "Label", true, false))
		{
			if (node is Label { AutowrapMode: not TextServer.AutowrapMode.Off } label)
			{
				label.CustomMinimumSize = new Vector2(Mathf.Max(1f, width - 32f), 0f);
				label.Size = label.CustomMinimumSize;
			}
		}
		return card;
	}
}
