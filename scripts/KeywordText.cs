using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheOneHeMissed.scripts;

/// <summary>The debuffs a description can name, as .ai/gameplay-design.md "Status effects" defines them.</summary>
/// <remarks>Root covers a freeze too: both are a slow at 0 (Enemy.IsFrozen). There is no burn status.</remarks>
public enum StatusId
{
	Slow,
	Root,
	Poison,
	Stagger,
	Vulnerable,
	Weaken,
	Wither,
	Fear,
}

/// <summary>
/// THE ONE PARSER for what a description names: status keywords and element tags (2026-11-02).
/// </summary>
/// <remarks>
/// Two requests from Eric the same day, built together because they are the same job - finding the
/// words in a line of text that mean something the game can explain:
///
///   * "If a description lists element tags, they shouldn't just list the text, but use the colored
///     boxes." A "+1 Poison" in any line this helper draws becomes the element chip every card already
///     wears (the chest's "Fire 1/2", counting what the player would hold after taking it).
///   * "Status effects should be outlined or underlined or something that looks clickable. When
///     clicked, they should show the description of what that debuff does. On controller, just show
///     the status descriptions when focused." A status word ("Weakened", "Withers", "slowed") becomes a
///     <see cref="StatusKeyword"/>: the word in the line's own ink, underlined in the status's colour,
///     opening its one-sentence card through the element chips' own popover (ElementTagPopover). On a
///     pad, <see cref="StatusNotes"/> shows every status a focused card names beside it.
///
/// DROP-IN. A screen builds its Label as it always did and hands it to <see cref="Wrap"/>: a line with
/// nothing to explain comes back as the same Label, untouched, so no screen changes where there is no
/// keyword; a line with one comes back as a flow of word labels in the Label's own size, face, ink and
/// alignment, with the keywords and chips among them. StatIcons.BuildLine does this for every glyph-led
/// stat line, which is most of the level-up cards and the chest pages in one place.
///
/// The words are matched whole and by their family ("weaken", "weakens", "weakened"). "Poison" is both a
/// status and an element: the capitalised bare word is the element ("Fire, Ice, Lightning and Poison
/// tier bonuses"), the lower-case one and every inflection the status.
/// </remarks>
public static class KeywordText
{
	public sealed class StatusInfo
	{
		public StatusId Id { get; init; }
		public string Name { get; init; }
		/// <summary>The element whose ramp marks the status on an enemy (StatusEffectLayer): its chip colour.</summary>
		public Element Element { get; init; }
		/// <summary>The underline on a light page (parchment). 3:1 or better there (ui_contrast.py, keyword_pairs).</summary>
		public Color OnLight { get; init; }
		/// <summary>The underline on a dark ground (stone). 3:1 or better there.</summary>
		public Color OnDark { get; init; }
		/// <summary>What it does, one sentence, from the rules in .ai/gameplay-design.md "Status effects".</summary>
		public string Sentence { get; init; }
	}

	private static Color Hex(string hex) => Color.FromHtml(hex);

	// Palette tones only (tools/art/bonelight.py): the light-ground underline is the dark end of the
	// status's ramp or its material row, the dark-ground one the light end. Mirrored in ui_contrast.py.
	public static readonly IReadOnlyDictionary<StatusId, StatusInfo> Statuses = new Dictionary<StatusId, StatusInfo>
	{
		[StatusId.Slow] = new()
		{
			Id = StatusId.Slow, Name = "Slowed", Element = Element.Ice,
			OnLight = Hex("#1A3E8A"), OnDark = Hex("#7FDCF8"),
			Sentence = $"Moves slower until it wears off; a boss never drops below {Mathf.RoundToInt(Enemy.BossSlowFloor * 100f)}% speed.",
		},
		[StatusId.Root] = new()
		{
			Id = StatusId.Root, Name = "Rooted", Element = Element.Ice,
			OnLight = Hex("#1A3E8A"), OnDark = Hex("#7FDCF8"),
			Sentence = $"Held where it stands until it wears off; a boss for {Enemy.BossHoldMaxSeconds:0.##} s at most.",
		},
		[StatusId.Poison] = new()
		{
			Id = StatusId.Poison, Name = "Poisoned", Element = Element.Poison,
			OnLight = Hex("#255416"), OnDark = Hex("#B4E04A"),
			Sentence = "Takes damage every second until the poison runs out.",
		},
		[StatusId.Stagger] = new()
		{
			Id = StatusId.Stagger, Name = "Staggered", Element = Element.Lightning,
			OnLight = Hex("#8A6A1E"), OnDark = Hex("#FFF06A"),
			Sentence = "Stops where it stands for a moment.",
		},
		[StatusId.Vulnerable] = new()
		{
			Id = StatusId.Vulnerable, Name = "Vulnerable", Element = Element.Darkness,
			OnLight = Hex("#35205C"), OnDark = Hex("#B48CE8"),
			Sentence = $"Takes more damage from everything, up to +{Mathf.RoundToInt(Enemy.MaxVulnerability * 100f)}%.",
		},
		[StatusId.Weaken] = new()
		{
			Id = StatusId.Weaken, Name = "Weakened", Element = Element.Light,
			OnLight = Hex("#8A5E0A"), OnDark = Hex("#FFC63C"),
			Sentence = $"Deals you less damage, up to {Mathf.RoundToInt(Enemy.MaxWeakenCeiling * 100f)}%, and its lightest touches can miss.",
		},
		[StatusId.Wither] = new()
		{
			Id = StatusId.Wither, Name = "Withered", Element = Element.Metal,
			OnLight = Hex("#8A1220"), OnDark = Hex("#E87A1A"),
			Sentence = "Loses a share of its max health for good, but never dies of it.",
		},
		[StatusId.Fear] = new()
		{
			Id = StatusId.Fear, Name = "Frightened", Element = Element.Darkness,
			OnLight = Hex("#35205C"), OnDark = Hex("#B48CE8"),
			Sentence = $"Runs from you for {Player.FearSeconds:0.#} s; elites and bosses are never frightened.",
		},
	};

	// The cards once ended on a shared line, "Debuffs never add up: the strongest and the longest
	// win." Eric had it removed (2026-11-02); each card is its own sentence only.

	// Every form of every status word, lower case. "poison" alone is handled in MatchStatus (the element).
	private static readonly Dictionary<string, StatusId> Words = new(StringComparer.Ordinal)
	{
		["slows"] = StatusId.Slow, ["slowed"] = StatusId.Slow, ["slowing"] = StatusId.Slow,
		["rooted"] = StatusId.Root, ["rooting"] = StatusId.Root,
		["freeze"] = StatusId.Root, ["freezes"] = StatusId.Root, ["frozen"] = StatusId.Root, ["freezing"] = StatusId.Root,
		["poisons"] = StatusId.Poison, ["poisoned"] = StatusId.Poison, ["poisoning"] = StatusId.Poison,
		["stagger"] = StatusId.Stagger, ["staggers"] = StatusId.Stagger, ["staggered"] = StatusId.Stagger,
		["vulnerable"] = StatusId.Vulnerable, ["vulnerability"] = StatusId.Vulnerable,
		["weaken"] = StatusId.Weaken, ["weakens"] = StatusId.Weaken, ["weakened"] = StatusId.Weaken, ["weakening"] = StatusId.Weaken,
		["wither"] = StatusId.Wither, ["withers"] = StatusId.Wither, ["withered"] = StatusId.Wither, ["withering"] = StatusId.Wither,
		["fear"] = StatusId.Fear, ["feared"] = StatusId.Fear, ["frighten"] = StatusId.Fear, ["frightens"] = StatusId.Fear,
		["frightened"] = StatusId.Fear, ["frightening"] = StatusId.Fear,
	};

	// "+1 Poison", "+2 Water": a tag the thing grants, as the set lines used to write them out.
	private static readonly Regex TagPattern = new(
		@"\+(\d+)\s+(Fire|Ice|Arcane|Darkness|Light|Grass|Earth|Wind|Lightning|Poison|Metal|Water)\b",
		RegexOptions.Compiled);

	private const string Punctuation = ".,;:!?()\"'";

	// Three bare words mean something else as often as the status: "a slow fireball", "a root that only
	// feeds", "Fire, Ice and Poison tier bonuses". "slow" and "root" count only as the label that opens a
	// stat line ("Slow 20%"); "poison" only in lower case (the element is capitalised).
	private static StatusId? MatchStatus(string core, bool opensLine)
	{
		if (core.Length == 0)
			return null;
		string lower = core.ToLowerInvariant();
		if (lower == "poison")
			return core == "poison" ? StatusId.Poison : null;
		if (lower == "slow" || lower == "root")
			return opensLine && char.IsUpper(core[0]) ? (lower == "slow" ? StatusId.Slow : StatusId.Root) : null;
		return Words.TryGetValue(lower, out StatusId id) ? id : null;
	}

	public enum TokenKind { Word, Status, Tag }

	public readonly record struct Token(TokenKind Kind, string Text, string Before, string After, StatusId Status, Element Element, int Weight);

	/// <summary>The line as words, status keywords and element tags, in order. Spaces separate tokens.</summary>
	public static List<Token> Parse(string text)
	{
		var tokens = new List<Token>();
		if (string.IsNullOrEmpty(text))
			return tokens;
		int at = 0;
		foreach (Match m in TagPattern.Matches(text))
		{
			AddWords(tokens, text.Substring(at, m.Index - at));
			Enum.TryParse(m.Groups[2].Value, out Element element);
			tokens.Add(new Token(TokenKind.Tag, m.Value, "", "", default, element, int.Parse(m.Groups[1].Value)));
			at = m.Index + m.Length;
		}
		AddWords(tokens, text.Substring(at));
		return tokens;
	}

	private static void AddWords(List<Token> tokens, string segment)
	{
		foreach (string word in segment.Split(' ', StringSplitOptions.RemoveEmptyEntries))
		{
			int start = 0, end = word.Length;
			while (start < end && Punctuation.IndexOf(word[start]) >= 0)
				start++;
			while (end > start && Punctuation.IndexOf(word[end - 1]) >= 0)
				end--;
			string core = word.Substring(start, end - start);
			StatusId? status = MatchStatus(core, tokens.Count == 0);
			// Punctuation straight after a chip ("+1 Poison," -> the comma) rides on the chip.
			if (status == null && start == end && tokens.Count > 0 && tokens[^1].Kind == TokenKind.Tag && tokens[^1].After.Length == 0)
			{
				Token chip = tokens[^1];
				tokens[^1] = chip with { After = word };
				continue;
			}
			tokens.Add(status is StatusId id
				? new Token(TokenKind.Status, core, word.Substring(0, start), word.Substring(end), id, default, 0)
				: new Token(TokenKind.Word, word, "", "", default, default, 0));
		}
	}

	/// <summary>True when <paramref name="text"/> names a status or an element tag this helper would draw.</summary>
	public static bool HasKeywords(string text) =>
		!string.IsNullOrEmpty(text) && Parse(text).Any(t => t.Kind != TokenKind.Word);

	/// <summary>Every status <paramref name="text"/> names, in order of first mention.</summary>
	public static List<StatusId> StatusesIn(string text) =>
		Parse(text).Where(t => t.Kind == TokenKind.Status).Select(t => t.Status).Distinct().ToList();

	/// <summary>
	/// <paramref name="label"/> as it is when its text names nothing; otherwise a flow of its words in its
	/// own size, face, ink and alignment, with every status a <see cref="StatusKeyword"/> and every
	/// "+N Element" an element chip. The returned control takes the label's name and size flags.
	/// </summary>
	public static Control Wrap(Label label)
	{
		if (label == null || !HasKeywords(label.Text))
			return label;

		List<Token> tokens = Parse(label.Text);
		bool wrap = label.AutowrapMode != TextServer.AutowrapMode.Off;
		Container flow;
		if (wrap)
		{
			var hflow = new HFlowContainer
			{
				Alignment = label.HorizontalAlignment switch
				{
					HorizontalAlignment.Center => FlowContainer.AlignmentMode.Center,
					HorizontalAlignment.Right => FlowContainer.AlignmentMode.End,
					_ => FlowContainer.AlignmentMode.Begin,
				},
			};
			flow = hflow;
		}
		else
		{
			flow = new HBoxContainer
			{
				Alignment = label.HorizontalAlignment == HorizontalAlignment.Center ? BoxContainer.AlignmentMode.Center : BoxContainer.AlignmentMode.Begin,
			};
		}
		if (!string.IsNullOrEmpty(label.Name))
			flow.Name = label.Name;
		flow.MouseFilter = Control.MouseFilterEnum.Ignore;
		flow.SizeFlagsHorizontal = label.SizeFlagsHorizontal;
		flow.SizeFlagsVertical = label.SizeFlagsVertical;
		flow.SizeFlagsStretchRatio = label.SizeFlagsStretchRatio;

		int size = label.GetThemeFontSize("font_size");
		Font font = label.GetThemeFont("font");
		Color ink = label.GetThemeColor("font_color");
		float space = font != null ? font.GetStringSize(" ", HorizontalAlignment.Left, -1, size).X : size * 0.3f;
		flow.AddThemeConstantOverride(wrap ? "h_separation" : "separation", Mathf.Max(2, Mathf.RoundToInt(space)));
		// The line's own pitch, tight where it was set tight - but never into a row of chips, whose boxes
		// would cover the underline of a keyword on the row above.
		bool chips = tokens.Any(t => t.Kind == TokenKind.Tag);
		if (wrap)
			flow.AddThemeConstantOverride("v_separation", chips ? Mathf.Max(0, label.GetThemeConstant("line_spacing")) : label.GetThemeConstant("line_spacing"));

		foreach (Token token in tokens)
		{
			switch (token.Kind)
			{
				case TokenKind.Word:
					flow.AddChild(Word(label, token.Text));
					break;
				case TokenKind.Status:
					flow.AddChild(Joined(label, token.Before, StatusKeyword.Create(token.Status, token.Text, ink, size, font), token.After));
					break;
				case TokenKind.Tag:
					// A chip needs no comma after it: the boxes are the list.
					flow.AddChild(TagChip(label, token.Element, token.Weight, focusable: false));
					break;
			}
		}
		Discard(label);
		return flow;
	}

	// The label the flow replaced. Built for this and never in the tree, so freed at once.
	private static void Discard(Label label)
	{
		if (label.IsInsideTree())
			label.QueueFree();
		else
			label.Free();
	}

	/// <summary>
	/// A line built from scratch, then <see cref="Wrap"/>ped: <paramref name="lead"/> in its own ink before
	/// the text (a tier line's "All 3:"), and the rest in <paramref name="ink"/>.
	/// </summary>
	public static Control Build(Node context, string text, Color ink, ResponsiveLayout.TextRole role,
		HorizontalAlignment alignment = HorizontalAlignment.Left, bool wrap = true, string lead = null, Color leadInk = default,
		int lineSpacing = 0)
	{
		var label = new Label
		{
			Text = string.IsNullOrEmpty(lead) ? text : $"{lead} {text}",
			HorizontalAlignment = alignment,
			AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		label.AddThemeFontSizeOverride("font_size", ResponsiveLayout.FontSize(context, role));
		label.AddThemeColorOverride("font_color", ink);
		if (lineSpacing != 0)
			label.AddThemeConstantOverride("line_spacing", lineSpacing);
		if (string.IsNullOrEmpty(lead))
			return Wrap(label);

		// A lead in its own ink needs the flow even with no keyword in the line.
		Control built = HasKeywords(label.Text) ? Wrap(label) : WrapAll(label, label.Text);
		int leadWords = lead.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
		int index = 0;
		foreach (Node child in built.GetChildren())
		{
			if (index++ >= leadWords)
				break;
			if (child is Label word)
				word.AddThemeColorOverride("font_color", leadInk);
		}
		return built;
	}

	// The same flow Wrap makes, for a line with nothing in it but a coloured lead.
	private static Control WrapAll(Label label, string text)
	{
		var hflow = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = label.SizeFlagsHorizontal };
		if (!string.IsNullOrEmpty(label.Name))
			hflow.Name = label.Name;
		int size = label.GetThemeFontSize("font_size");
		Font font = label.GetThemeFont("font");
		float space = font != null ? font.GetStringSize(" ", HorizontalAlignment.Left, -1, size).X : size * 0.3f;
		hflow.AddThemeConstantOverride("h_separation", Mathf.Max(2, Mathf.RoundToInt(space)));
		hflow.AddThemeConstantOverride("v_separation", label.GetThemeConstant("line_spacing"));
		foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
			hflow.AddChild(Word(label, word));
		Discard(label);
		return hflow;
	}

	// One word, in the line's own size, face and ink. Never wraps: the flow is what wraps.
	private static Label Word(Label proto, string text)
	{
		var word = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.Off,
			VerticalAlignment = VerticalAlignment.Top,
			SizeFlagsVertical = Control.SizeFlags.Fill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		word.AddThemeFontSizeOverride("font_size", proto.GetThemeFontSize("font_size"));
		if (proto.HasThemeFontOverride("font"))
			word.AddThemeFontOverride("font", proto.GetThemeFont("font"));
		word.AddThemeColorOverride("font_color", proto.GetThemeColor("font_color"));
		return word;
	}

	// A keyword or chip with the punctuation that touches it, set tight against it.
	private static Control Joined(Label proto, string before, Control middle, string after)
	{
		if (string.IsNullOrEmpty(before) && string.IsNullOrEmpty(after))
			return middle;
		var box = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		box.AddThemeConstantOverride("separation", 0);
		if (!string.IsNullOrEmpty(before))
			box.AddChild(Word(proto, before));
		box.AddChild(middle);
		if (!string.IsNullOrEmpty(after))
			box.AddChild(Word(proto, after));
		return box;
	}

	/// <summary>
	/// One element tag as the chip every card wears: "Fire 1/2" - what the player would hold after taking
	/// the thing, over the next tier's threshold - or the bare name outside a run. Its card adds
	/// <paramref name="weight"/> to the live count (ElementChip.Gain).
	/// </summary>
	public static ElementChip TagChip(Node context, Element element, int weight, bool focusable)
	{
		int held = ElementTagPopover.HeldCount(context, element);
		int after = held >= 0 ? held + weight : -1;
		string text = after >= 0 ? $"{element} {ElementPassiveDescriptions.GetProgressLabel(after)}"
			: weight > 1 ? $"+{weight} {element}" : $"+1 {element}";
		ElementChip chip = CodexRows.BuildChip(element, text, focusable, countAfter: after);
		chip.Gain = weight;
		return chip;
	}

	/// <summary>A row of tag chips, for a set's or a relic's tags where no sentence carries them.</summary>
	public static HFlowContainer TagChips(Node context, IEnumerable<(string Element, int Weight)> tags, bool focusable)
	{
		var row = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("h_separation", 8);
		row.AddThemeConstantOverride("v_separation", 4);
		foreach (var (name, weight) in tags ?? Array.Empty<(string, int)>())
		{
			if (Enum.TryParse(name, true, out Element element))
				row.AddChild(TagChip(context, element, weight, focusable));
		}
		return row;
	}

	/// <summary>The underline for a status on a ground the line's own ink is legible on.</summary>
	public static Color UnderlineFor(StatusId status, Color ink) =>
		StatIcons.RelativeLuminance(ink) < 0.25f ? Statuses[status].OnLight : Statuses[status].OnDark;
}
