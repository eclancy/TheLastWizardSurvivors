using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

// AN ENTRY'S PAGE (.ai/evolution-codex.md sections 2-4, layout B "the book"): the right page on a PC, the
// turned page on a phone. A spell's evolutions are a family tree down the page - the base, the level 4
// mutations in a row, the level 8 Ascensions under them, then the Apotheosis - joined by ink lines
// (CodexTreeLines): the gold shade into what is open or taken, dashed into the rest, and the way to the
// Apotheosis in the ember line. An item is its effect and its sets; a recipe is its ingredients into its
// result. The recipes a spell is part of are a note in the margin at the foot.
//
// These are the PARTS; EvolutionCodex.Layout.cs places them (ArrangeTree, ArrangeRecipe). Everything the
// Ascensions browser showed is here and nothing was lost: every gate met or not, with the shortfall in
// numbers on the rubric band; sealed paths ("???") until first met; the path taken and the ones closed; the
// Apotheosis with its own gates and "out of reach"; the recipes a path joins. What a node DOES is one line in
// the reading line under the tree, for the node the selection or the pointer is on.
public partial class EvolutionCodex
{
	/// <summary>What a sealed node says (AscensionSightings' rule, section 4e of the level-up doc).</summary>
	private const string SealedLine = "Sealed until you first meet its requirements.";

	private static CodexRows.Palette V => CodexRows.Vellum;

	/// <summary>The page's focus stops, line by line: a tier each, then the margin note.</summary>
	private readonly List<List<Control>> treeLines = new();
	private readonly Dictionary<Control, CodexNode> stopNodes = new();
	private Label readingName;
	private Label readingLine;

	private void ShowPage()
	{
		if (page == null || !IsInstanceValid(page))
			return;
		foreach (Node child in page.GetChildren())
		{
			page.RemoveChild(child);
			child.QueueFree();
		}
		treeLines.Clear();
		stopNodes.Clear();
		readingName = null;
		readingLine = null;

		CodexEntry entry = model.Get(selectedId);
		if (entryTitle != null)
			entryTitle.Text = entry?.ShownName ?? string.Empty;
		if (entry == null)
		{
			page.AddChild(CodexRows.MakeText("Nothing chosen.", ResponsiveLayout.TextRole.Body, V.InkSoft));
			return;
		}

		page.AddChild(BuildHead(entry));
		if (entry.Discovered)
		{
			switch (entry.Kind)
			{
				case CodexKind.Spell:
					BuildSpellPage(entry);
					break;
				case CodexKind.Relic:
				case CodexKind.Boon:
					BuildItemPage(entry);
					break;
				default:
					page.AddChild(ArrangeRecipe(entry.Recipe));
					AddRecipeTiers(entry.Recipe);
					break;
			}
		}
		WireTreeFocus();
		if (IsInstanceValid(pageScroll))
			pageScroll.ScrollVertical = 0;
	}

	// ---- the head: what the entry is ----------------------------------------------------------------

	private Control BuildHead(CodexEntry entry)
	{
		var head = new HBoxContainer { Name = "CodexHead", MouseFilter = Control.MouseFilterEnum.Ignore };
		head.AddThemeConstantOverride("separation", 14);
		head.AddChild(entry.Discovered
			? Picture(entry.Icon ?? DefaultIcon, 64f, dim: false)
			: Picture(StatIcons.Load("sealed"), 48f, dim: false));

		var info = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		info.AddThemeConstantOverride("separation", 4);
		head.AddChild(info);

		var titleRow = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		titleRow.AddThemeConstantOverride("h_separation", 10);
		titleRow.AddThemeConstantOverride("v_separation", 4);
		info.AddChild(titleRow);
		// On a phone the turned page's band already names it ("‹ Index   Cinderbreath").
		if (!narrow)
		{
			Label name = CodexRows.MakeText(entry.ShownName, ResponsiveLayout.TextRole.Title, V.Ink);
			name.Name = "CodexDetailName";
			name.AutowrapMode = TextServer.AutowrapMode.Off;
			titleRow.AddChild(name);
		}
		if (entry.Discovered)
		{
			foreach (var (element, weight) in entry.Elements)
				titleRow.AddChild(CodexRows.BuildChip(element, weight > 1 ? $"{element} x{weight}" : element.ToString(), focusable: false));
		}
		Label kind = CodexRows.MakeText(KindAndLevel(entry), ResponsiveLayout.TextRole.Label, V.InkSoft);
		kind.AutowrapMode = TextServer.AutowrapMode.Off;
		kind.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		titleRow.AddChild(kind);

		if (entry.Discovered)
		{
			if (!string.IsNullOrWhiteSpace(entry.Line))
				info.AddChild(CodexRows.MakeText(entry.Line, ResponsiveLayout.TextRole.Label, V.Ink));
		}
		else
		{
			// How it is found: the one useful thing a sealed entry has (UnlockCatalog's line for a spell).
			Texture2D glyph = entry.Kind == CodexKind.Spell ? CodexRows.SpellHintGlyph(entry.Id) : StatIcons.Load("lock");
			info.AddChild(GlyphLine(glyph, entry.Hint, V.InkSoft));
		}

		if (StateTag(entry) is Control tag)
			head.AddChild(tag);
		if (CurationToggle(entry) is Control curation)
			head.AddChild(curation);
		return head;
	}

	// "Attack · Level 7 · your starting spell", "Rare relic", "Conjunction".
	private string KindAndLevel(CodexEntry entry)
	{
		if (!entry.Discovered)
			return entry.Kind switch
			{
				CodexKind.Spell => "Spell, not yet found",
				CodexKind.Relic => "Relic, not yet found",
				CodexKind.Boon => "Boon, not yet found",
				CodexKind.Combination => "Conjunction, not yet known",
				_ => "Full Set Enchantment, not yet known",
			};
		// The level is on the base node, and the pips on the index line: said once here would be said thrice.
		var parts = new List<string> { entry.KindLine };
		if (entry.Own)
			parts.Add("your starting spell");
		return string.Join("  ·  ", parts);
	}

	// "Ready", "Held", "Made": the line's state said once in words.
	private Control StateTag(CodexEntry entry)
	{
		string text = entry.State switch
		{
			CodexState.Ready => "Ready",
			CodexState.Taken when entry.Kind == CodexKind.Spell => "Maxed",
			CodexState.Taken when entry.Kind == CodexKind.Combination => "Made",
			CodexState.Taken when entry.Kind == CodexKind.FullSet => "Complete",
			CodexState.Taken => "Held",
			CodexState.Owned when entry.Kind == CodexKind.Combination => "Made",
			_ => null,
		};
		if (text == null)
			return null;
		Control tag = Tag(text);
		tag.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
		return tag;
	}

	// A small solid tag in the gold shade with the page's face for its word ("READY").
	private static Control Tag(string text)
	{
		var pill = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
		var box = new StyleBoxFlat { BgColor = V.Reward };
		box.SetCornerRadiusAll(2);
		box.ContentMarginLeft = 6;
		box.ContentMarginRight = 6;
		box.ContentMarginTop = 1;
		box.ContentMarginBottom = 1;
		pill.AddThemeStyleboxOverride("panel", box);
		Label words = CodexRows.MakeText(text.ToUpperInvariant(), ResponsiveLayout.TextRole.Micro, V.Face);
		words.AutowrapMode = TextServer.AutowrapMode.Off;
		pill.AddChild(words);
		return pill;
	}

	// The Spell Index's Set aside, kept: a Redaction takes a spell out of the level-up pool. Collection mode
	// only, and only once the save owns one (or has something set aside) - before that it is nothing at all.
	private Control CurationToggle(CodexEntry entry)
	{
		if (model.Mode != CodexMode.Collection || entry.Kind != CodexKind.Spell || !entry.Owned)
			return null;
		SaveData save = context.Save;
		bool curating = GlobalStatsManager.GetCurationSlotsOwned(save) > 0 || GlobalStatsManager.GetRemovedSpellCount(save) > 0;
		if (!curating)
			return null;
		bool canSetAside = GlobalStatsManager.CanRemoveSpellFromPool(save, entry.Id, out string reason);
		var button = new Button
		{
			Name = "CurationToggle_" + entry.Id,
			Text = entry.SetAside ? "Restore" : "Set aside",
			Disabled = !entry.SetAside && !canSetAside,
			FocusMode = Control.FocusModeEnum.All,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
			TooltipText = entry.SetAside ? "Put it back in the level-up pool." : canSetAside ? "Stop offering it at level-up." : reason,
		};
		ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Label);
		CodexRows.StyleQuietToggle(button, V);
		ResponsiveLayout.EnsureTouchTarget(button, 0f);
		string id = entry.Id;
		bool wasAside = entry.SetAside;
		button.Pressed += () =>
		{
			var manager = GetNodeOrNull<SaveManager>("/root/SaveManager");
			if (manager?.Data == null || !GlobalStatsManager.SetSpellRemovedFromPool(manager.Data, id, !wasAside))
				return;
			manager.SaveGame();
			Callable.From(() => RefreshAfterCuration(id)).CallDeferred();
		};
		return button;
	}

	private void RefreshAfterCuration(string id)
	{
		model = CodexModel.Build(context);
		RebuildIndex();
		ShowPage();
		UpdateCount();
		if (page.FindChild("CurationToggle_" + id, true, false) is Button again && !again.Disabled)
			again.GrabFocus();
	}

	// ---- a spell: its family tree -----------------------------------------------------------------

	private void BuildSpellPage(CodexEntry entry)
	{
		if (entry.Forms.Count == 0)
			return;
		// The form a tab chose; else the one a search hit lit a node in (a search for Purr opens the cat); else
		// the held one, or the first.
		int lit = string.IsNullOrEmpty(litNodeId) ? -1
			: entry.Forms.FindIndex(f => f.AllNodes.Any(n => n.Id.Equals(litNodeId, StringComparison.OrdinalIgnoreCase)));
		int shown = formShown >= 0 && formShown < entry.Forms.Count ? formShown
			: lit >= 0 ? lit
			: Mathf.Clamp(entry.CurrentForm, 0, entry.Forms.Count - 1);
		if (entry.Forms.Count > 1)
			page.AddChild(BuildFormTabs(entry, shown));
		CodexForm form = entry.Forms[shown];
		if (form.Mutations.Count == 0 && form.Ascensions.Count == 0 && form.Finals.Count == 0)
		{
			page.AddChild(CodexRows.MakeText("No evolutions.", ResponsiveLayout.TextRole.Label, V.InkSoft));
			return;
		}
		page.AddChild(ArrangeTree(entry, form));
		page.AddChild(BuildReading());
		if (entry.UsedIn.Count > 0)
			page.AddChild(BuildMarginNote(entry));
		ShowReading(DefaultReadingNode(form));
	}

	// A spell taken in forms (Spiritual Weapon's axe, hammer, sword): each form's milestones are its own.
	private Control BuildFormTabs(CodexEntry entry, int shown)
	{
		var row = new HBoxContainer { Name = "CodexForms", MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 6);
		var group = new ButtonGroup();
		for (int i = 0; i < entry.Forms.Count; i++)
		{
			CodexForm form = entry.Forms[i];
			bool held = entry.Held && i == entry.CurrentForm;
			var tab = new Button
			{
				Name = $"CodexForm_{form.Id}",
				Text = held ? $"{form.Name} (held)" : form.Name,
				ToggleMode = true,
				ButtonGroup = group,
				ButtonPressed = i == shown,
				FocusMode = Control.FocusModeEnum.All,
			};
			ResponsiveLayout.SetFont(tab, ResponsiveLayout.TextRole.Label);
			CodexRows.StyleTab(tab, V);
			ResponsiveLayout.EnsureTouchTarget(tab, 0f);
			int index = i;
			tab.Pressed += () =>
			{
				formShown = index;
				litNodeId = string.Empty;
				ShowPage();
				(page.FindChild($"CodexForm_{entry.Forms[index].Id}", true, false) as Button)?.GrabFocus();
			};
			row.AddChild(tab);
		}
		treeLines.Add(row.GetChildren().OfType<Control>().ToList());
		return row;
	}

	// A tier's heading in the book's small capitals, and the requirements every node of the tier shares.
	private Control TierHeading(string label, List<CodexChip> gates)
	{
		var row = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("h_separation", 8);
		row.AddThemeConstantOverride("v_separation", 4);
		Label words = CodexRows.MakeText(label.ToUpperInvariant(), ResponsiveLayout.TextRole.Label, V.Heading);
		words.AutowrapMode = TextServer.AutowrapMode.Off;
		words.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		headingFont ??= GD.Load<Font>("res://scenes/resources/HeadingFont.tres");
		if (headingFont != null)
			words.AddThemeFontOverride("font", headingFont);
		// On a patch of the page itself: a tree line passing behind the heading stops at its words (the ink
		// report measured the ember line under "MUTATION" as their ground).
		var patch = new StyleBoxFlat { BgColor = V.Face };
		patch.ContentMarginLeft = 2;
		patch.ContentMarginRight = 4;
		words.AddThemeStyleboxOverride("normal", patch);
		row.AddChild(words);
		foreach (CodexChip chip in gates ?? new List<CodexChip>())
			row.AddChild(BuildChip(chip, ember: false));
		return row;
	}

	private Control BuildBaseNode(CodexEntry entry, CodexForm form)
	{
		var node = new CodexNode
		{
			Id = $"base_{entry.Id}",
			SpellId = entry.Id,
			Milestone = 0,
			Name = entry.Forms.Count > 1 ? $"{entry.Name}: {form.Name}" : entry.Name,
			Line = entry.Line,
			Icon = form.Icon ?? entry.Icon,
			State = CodexNodeState.Known,
			Note = entry.Held ? $"level {entry.Level} of {entry.LevelCap}" : "base",
		};
		return BuildNode(node, isBase: true);
	}

	/// <summary>
	/// One node (section 3) on the page: its picture and name on a face that says its state, what it is to
	/// this run in a quiet line, its own requirements as chips. The Apotheosis wears the ember. A focus stop for
	/// the stick and a hover target for the pointer, both of which put what it does in the reading line.
	/// </summary>
	private Control BuildNode(CodexNode node, bool isBase = false)
	{
		bool ember = node.Milestone == ApotheosisCatalog.MilestoneLevel;
		var box = new PanelContainer
		{
			Name = $"CodexNode_{node.Id}",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		bool lit = !string.IsNullOrEmpty(litNodeId) && node.Id.Equals(litNodeId, StringComparison.OrdinalIgnoreCase);
		box.AddThemeStyleboxOverride("panel", NodeStyle(node.State, ember, lit));

		Button stop = BonelightSkin.MakeCardClickOverlay(true, 3);
		stop.Name = "NodeStop";
		stop.MouseFilter = Control.MouseFilterEnum.Pass;
		stop.FocusEntered += () => ShowReading(node);
		stop.MouseEntered += () => ShowReading(node);
		if (!string.IsNullOrEmpty(node.OpensEntryId) && node.OpensEntryId != selectedId && model.Get(node.OpensEntryId) is CodexEntry opens && opens.Discovered)
		{
			// An ingredient turns to its own entry's page: the reverse lookup walks both ways.
			string target = opens.Id;
			stop.Pressed += () => SelectEntry(target);
		}
		box.AddChild(stop);
		stopNodes[stop] = node;

		var pad = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		pad.AddThemeConstantOverride("margin_left", 8);
		pad.AddThemeConstantOverride("margin_right", 8);
		pad.AddThemeConstantOverride("margin_top", 6);
		pad.AddThemeConstantOverride("margin_bottom", 6);
		box.AddChild(pad);
		var content = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		content.AddThemeConstantOverride("separation", 3);
		pad.AddChild(content);

		bool quiet = node.State is CodexNodeState.Sealed or CodexNodeState.Closed;
		Color ink = ember ? CodexInk.EmberInk : quiet ? V.InkMeta : V.Ink;
		if (ember)
		{
			string owner = string.IsNullOrEmpty(node.Owner) ? "APOTHEOSIS" : $"APOTHEOSIS  ·  {node.Owner.ToUpperInvariant()}";
			Label crown = CodexRows.MakeText($"✶ {owner}", ResponsiveLayout.TextRole.Micro, CodexInk.EmberSoft);
			crown.AutowrapMode = TextServer.AutowrapMode.Off;
			content.AddChild(crown);
		}
		var title = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		title.AddThemeConstantOverride("separation", 6);
		content.AddChild(title);
		Texture2D picture = node.State == CodexNodeState.Sealed ? StatIcons.Load("sealed") : node.Icon ?? DefaultIcon;
		// 32: the 64 art halved, or the 24 sealed glyph unscaled in a 32 box.
		TextureRect icon = Picture(picture, 32f, dim: node.State == CodexNodeState.Closed);
		if (node.State == CodexNodeState.Sealed)
			icon.StretchMode = TextureRect.StretchModeEnum.KeepCentered;
		// On a phone three nodes share a row a third of the page wide: the picture stands over the name, which
		// beside it broke mid-word ("Hellbreat / h"). The base and the Apotheosis have the row to themselves.
		if (narrow && !isBase && !ember)
		{
			icon.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
			content.AddChild(icon);
			content.MoveChild(icon, title.GetIndex());
		}
		else
			title.AddChild(icon);
		Label name = CodexRows.MakeText(node.ShownName, ResponsiveLayout.TextRole.Label, ink);
		headingFont ??= GD.Load<Font>("res://scenes/resources/HeadingFont.tres");
		if (headingFont != null)
			name.AddThemeFontOverride("font", headingFont);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		title.AddChild(name);
		if (node.State == CodexNodeState.Ready)
			title.AddChild(Tag("Ready"));
		if (node.State == CodexNodeState.Closed)
			title.AddChild(Glyph("lock"));

		if (StatusLine(node, isBase) is string status)
		{
			Label quietLine = CodexRows.MakeText(status, ResponsiveLayout.TextRole.Micro, ember ? CodexInk.EmberSoft : V.InkSoft);
			// The base's line ("level 7 of 8") rides beside its name: the base is one band.
			if (isBase)
			{
				quietLine.AutowrapMode = TextServer.AutowrapMode.Off;
				quietLine.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
				title.AddChild(quietLine);
			}
			else
				content.AddChild(quietLine);
		}
		if (node.Chips.Count > 0)
			content.AddChild(ChipFlow(node.Chips, ember));
		return box;
	}

	// What the node is to this run, in a quiet line: the model's note, or the state in words.
	private string StatusLine(CodexNode node, bool isBase)
	{
		// Only what the face does not already say: a sealed node's "???" and glyph, a closed one's lock and fade
		// say themselves (and the reading line says them in words), so they cost the page no line.
		if (isBase || !string.IsNullOrEmpty(node.Note))
			return string.IsNullOrEmpty(node.Note) || node.Note == "Not taken" ? null
				: node.Note == "Chosen" ? "taken this run"
				: node.Note;
		return node.State == CodexNodeState.Taken ? "taken this run" : null;
	}

	private static StyleBoxFlat NodeStyle(CodexNodeState state, bool ember, bool lit)
	{
		var box = new StyleBoxFlat();
		int width = 2;
		if (ember)
		{
			box.BgColor = CodexInk.EmberFace;
			box.BorderColor = CodexInk.EmberRim;
			width = 3;
		}
		else
		{
			switch (state)
			{
				case CodexNodeState.Taken:
					box.BgColor = CodexInk.BookSelected;
					box.BorderColor = V.Reward;
					break;
				case CodexNodeState.Ready:
					box.BgColor = CodexInk.BookNode;
					box.BorderColor = V.Reward;
					width = 3;
					break;
				case CodexNodeState.Sealed:
				case CodexNodeState.Closed:
					// Ink on the bare page, ruled faintly: present, but not the player's.
					box.BgColor = new Color(CodexInk.BookNode, 0f);
					box.BorderColor = V.InkMeta;
					width = 1;
					break;
				default:
					box.BgColor = CodexInk.BookNode;
					box.BorderColor = V.Ink;
					break;
			}
		}
		if (lit)
		{
			// A search hit: the node it named, in the hover gold with a heavy rule.
			if (!ember)
				box.BgColor = CodexInk.BookHover;
			width = 3;
			box.BorderColor = ember ? CodexInk.EmberSoft : V.Reward;
		}
		box.SetBorderWidthAll(width);
		box.SetCornerRadiusAll(3);
		box.SetContentMarginAll(0);
		return box;
	}

	private static Control StopOf(Control node) => node.GetNodeOrNull<Control>("NodeStop") ?? node;

	// An ink line is solid into what is open now or was taken, dashed into everything else.
	private static bool IsLive(CodexNode node) => node.State is CodexNodeState.Ready or CodexNodeState.Taken;

	// ---- chips --------------------------------------------------------------------------------------

	private Control ChipFlow(List<CodexChip> chips, bool ember)
	{
		var flow = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		flow.AddThemeConstantOverride("h_separation", 5);
		flow.AddThemeConstantOverride("v_separation", 4);
		foreach (CodexChip chip in chips)
			flow.AddChild(BuildChip(chip, ember));
		return flow;
	}

	/// <summary>
	/// A requirement as a chip. An element's is its own colour (a tap opens what the element does); the rest
	/// are pills. Measured in a run: met with a tick, unmet on the rubric band with the socket, closed with a
	/// lock. In the collection there is nothing to measure, and the pill is only an outline.
	/// </summary>
	private Control BuildChip(CodexChip chip, bool ember)
	{
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 2);
		if (chip.Element is Element element)
		{
			if (chip.Met.HasValue)
				line.AddChild(Glyph(chip.Met.Value ? "done" : chip.Closed ? "lock" : "todo"));
			line.AddChild(CodexRows.BuildChip(element, chip.Text, focusable: false));
			return line;
		}
		var pill = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		var style = new StyleBoxFlat();
		Color ink;
		string glyph = null;
		if (chip.Met == false && !chip.Closed && !ember)
		{
			style.BgColor = CodexInk.UnmetBand;
			style.BorderColor = CodexInk.UnmetBand;
			ink = CodexInk.UnmetInk;
			glyph = "todo";
		}
		else
		{
			// A neutral chip is on the page's own face, so a line behind it stops at its rim too.
			style.BgColor = ember ? CodexInk.EmberPill : chip.Met == true ? CodexInk.BookPill : V.Face;
			style.BorderColor = ember ? CodexInk.EmberPill : chip.Met == true ? CodexInk.BookPill : V.Edge;
			ink = ember ? CodexInk.EmberInk : V.Ink;
			glyph = chip.Met == true ? "done" : chip.Met == false ? (chip.Closed ? "lock" : "todo") : null;
		}
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(3);
		style.ContentMarginLeft = glyph != null ? 2 : 7;
		style.ContentMarginRight = 7;
		style.ContentMarginTop = 2;
		style.ContentMarginBottom = 2;
		pill.AddThemeStyleboxOverride("panel", style);
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 2);
		if (glyph != null)
			row.AddChild(Glyph(glyph));
		Label words = CodexRows.MakeText(chip.Text, ResponsiveLayout.TextRole.Micro, ink);
		words.AutowrapMode = TextServer.AutowrapMode.Off;
		row.AddChild(words);
		pill.AddChild(row);
		line.AddChild(pill);
		return line;
	}

	private static TextureRect Glyph(string key) => new()
	{
		// The 24-pixel stat glyphs, unscaled (CodexRows sets them the same).
		Texture = StatIcons.Load(key),
		CustomMinimumSize = new Vector2(24, 24),
		ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
		StretchMode = TextureRect.StretchModeEnum.KeepCentered,
		TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		MouseFilter = Control.MouseFilterEnum.Ignore,
	};

	private static TextureRect Picture(Texture2D texture, float side, bool dim) => new()
	{
		Texture = texture,
		CustomMinimumSize = new Vector2(side, side),
		ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
		StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
		MouseFilter = Control.MouseFilterEnum.Ignore,
		Modulate = dim ? new Color(1f, 1f, 1f, 0.55f) : Colors.White,
	};

	private static Texture2D defaultIcon;
	private static Texture2D DefaultIcon => defaultIcon ??= BonelightSkin.LoadTextureSafe("res://assets/bonelight/ui/spells/unknown.png");

	private static Control GlyphLine(Texture2D glyph, string text, Color ink)
	{
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 8);
		if (glyph != null)
			row.AddChild(new TextureRect
			{
				Texture = glyph,
				CustomMinimumSize = new Vector2(24, 24),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = glyph.GetWidth() > 24 ? TextureRect.StretchModeEnum.KeepAspectCentered : TextureRect.StretchModeEnum.KeepCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			});
		Label label = CodexRows.MakeText(text, ResponsiveLayout.TextRole.Label, ink);
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(label);
		return row;
	}

	// ---- the reading line: what the node under the selection does ----------------------------------

	private Control BuildReading()
	{
		var box = new VBoxContainer { Name = "CodexReading", MouseFilter = Control.MouseFilterEnum.Ignore };
		box.AddThemeConstantOverride("separation", 0);
		readingName = CodexRows.MakeText(string.Empty, ResponsiveLayout.TextRole.Label, V.Heading);
		headingFont ??= GD.Load<Font>("res://scenes/resources/HeadingFont.tres");
		if (headingFont != null)
			readingName.AddThemeFontOverride("font", headingFont);
		box.AddChild(readingName);
		readingLine = CodexRows.MakeText(string.Empty, ResponsiveLayout.TextRole.Label, V.Ink);
		box.AddChild(readingLine);
		return box;
	}

	private void ShowReading(CodexNode node)
	{
		if (node == null || readingName == null || !IsInstanceValid(readingName))
			return;
		readingName.Text = node.ShownName;
		readingLine.Text = node.Revealed ? node.Line : SealedLine;
	}

	private CodexNode DefaultReadingNode(CodexForm form)
	{
		IEnumerable<CodexNode> all = form.AllNodes;
		return all.FirstOrDefault(n => !string.IsNullOrEmpty(litNodeId) && n.Id.Equals(litNodeId, StringComparison.OrdinalIgnoreCase))
			?? all.FirstOrDefault(n => n.State == CodexNodeState.Ready)
			?? form.Ascensions.Concat(form.Finals).LastOrDefault(n => n.State == CodexNodeState.Taken)
			?? form.Mutations.LastOrDefault(n => n.State == CodexNodeState.Taken)
			?? form.Mutations.FirstOrDefault()
			?? all.FirstOrDefault();
	}

	// ---- the margin note: the recipes a spell is part of ------------------------------------------

	/// <summary>
	/// "In the margin: Hunter's Mark + Starfall · Constellation → ? ? ?" - each known recipe this spell is a part
	/// of, its own named Ascension first, the result sealed until made. Each is a press that turns to the recipe.
	/// </summary>
	private Control BuildMarginNote(CodexEntry entry)
	{
		var note = new VBoxContainer { Name = "CodexMargin", MouseFilter = Control.MouseFilterEnum.Ignore };
		note.AddThemeConstantOverride("separation", 2);
		note.AddChild(new ColorRect { Color = V.Edge, CustomMinimumSize = new Vector2(0, 1), MouseFilter = Control.MouseFilterEnum.Ignore });
		Label lead = CodexRows.MakeText("In the margin", ResponsiveLayout.TextRole.Micro, V.InkSoft);
		note.AddChild(lead);
		var stops = new List<Control>();
		foreach (string id in entry.UsedIn.Take(CodexModel.MaxUsedIn))
		{
			CodexEntry recipe = model.Get(id);
			if (recipe?.Recipe == null)
				continue;
			CodexNode own = recipe.Recipe.Ingredients.FirstOrDefault(i => i.SpellId.Equals(entry.Id, StringComparison.OrdinalIgnoreCase));
			string partner = string.Join(" + ", recipe.Recipe.Ingredients
				.Where(i => !i.SpellId.Equals(entry.Id, StringComparison.OrdinalIgnoreCase))
				.Select(i => i.Milestone == 8 ? $"{i.Line} · {i.Name}" : i.ShownName));
			string result = recipe.Recipe.Result.Revealed ? recipe.Recipe.Result.Name : "? ? ?";
			string kind = recipe.Kind == CodexKind.Combination ? ", a Conjunction" : string.Empty;
			var button = new Button
			{
				Name = $"CodexUsedIn_{id}",
				Text = $"{own?.Name ?? entry.Name} + {partner}  →  {result}{kind}",
				FocusMode = Control.FocusModeEnum.All,
				Alignment = HorizontalAlignment.Left,
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			};
			ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Label);
			CodexRows.StyleQuietToggle(button, V);
			ResponsiveLayout.EnsureTouchTarget(button, 0f);
			button.Pressed += () => SelectEntry(id);
			note.AddChild(button);
			stops.Add(button);
		}
		if (stops.Count > 0)
			treeLines.Add(stops);
		return note;
	}

	// ---- an item: what it does, and its sets ------------------------------------------------------

	private void BuildItemPage(CodexEntry entry)
	{
		if (entry.Kind == CodexKind.Boon)
		{
			if (model.Mode == CodexMode.Collection && !entry.Owned)
				page.AddChild(GlyphLine(StatIcons.Load("lock"), entry.Hint, V.InkSoft));
			return;
		}
		bool any = false;
		foreach (string id in entry.UsedIn)
		{
			CodexEntry set = model.Get(id);
			if (set?.Recipe == null)
				continue;
			any = true;
			page.AddChild(RecipeHeading(set));
			page.AddChild(ArrangeRecipe(set.Recipe));
			AddRecipeTiers(set.Recipe);
		}
		if (!any)
			page.AddChild(CodexRows.MakeText("Part of no set yet known.", ResponsiveLayout.TextRole.Label, V.InkSoft));
	}

	private Control RecipeHeading(CodexEntry set)
	{
		var head = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		head.AddThemeConstantOverride("separation", 8);
		head.AddChild(Picture(set.Icon ?? DefaultIcon, 32f, dim: false));
		Label title = CodexRows.MakeText($"Full Set Enchantment: {set.Name}", ResponsiveLayout.TextRole.Label, V.Heading);
		title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		head.AddChild(title);
		return head;
	}

	// A set's two tiers under its recipe, each ticked once this run holds enough of it (the chest page's "now").
	private void AddRecipeTiers(CodexRecipe recipe)
	{
		if (recipe == null || recipe.Kind != CodexKind.FullSet)
			return;
		bool partialOn = recipe.PartialCount > 0 && recipe.HeldCount >= recipe.PartialCount;
		if (!string.IsNullOrEmpty(recipe.Partial))
			page.AddChild(GlyphLine(StatIcons.Load(partialOn ? "done" : "todo"), recipe.Partial, partialOn ? V.Ink : V.InkSoft));
		if (recipe.Result.Revealed)
			page.AddChild(GlyphLine(StatIcons.Load(recipe.Made ? "done" : "todo"), recipe.Result.Line, V.Ink));
	}

	// ---- the page's focus -----------------------------------------------------------------------------

	/// <summary>
	/// The stick walks the page tier by tier - along a tier, down to the next - explicitly, because Godot's
	/// geometry from a centred node could land anywhere. The first tier steps back to the index line it belongs
	/// to (a PC) or to "‹ Index" (a phone), and nothing walks off the page.
	/// </summary>
	private void WireTreeFocus()
	{
		var lines = treeLines.Where(l => l.Count > 0).ToList();
		if (lines.Count == 0)
			return;
		rows.TryGetValue(selectedId, out Button row);
		Control back = narrow ? indexButton : row;
		for (int l = 0; l < lines.Count; l++)
		{
			List<Control> line = lines[l];
			for (int i = 0; i < line.Count; i++)
			{
				Control stop = line[i];
				Link(stop, i + 1 < line.Count ? line[i + 1] : stop, Side.Right);
				Link(stop, i > 0 ? line[i - 1] : narrow || back == null ? stop : back, Side.Left);
				Link(stop, l + 1 < lines.Count ? Nearest(lines[l + 1], i, line.Count) : stop, Side.Bottom);
				Link(stop, l > 0 ? Nearest(lines[l - 1], i, line.Count) : back ?? stop, Side.Top);
			}
		}
	}

	private static Control Nearest(List<Control> line, int index, int count)
	{
		if (line.Count == 0)
			return null;
		// The same place along the other line, proportionally: the middle of three lands on the middle of two.
		int at = count <= 1 ? (line.Count - 1) / 2 : Mathf.RoundToInt((float)index / (count - 1) * (line.Count - 1));
		return line[Mathf.Clamp(at, 0, line.Count - 1)];
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

	private Control FirstTreeStop()
	{
		foreach (var pair in stopNodes)
			if (!string.IsNullOrEmpty(litNodeId) && pair.Value.Id.Equals(litNodeId, StringComparison.OrdinalIgnoreCase) && IsInstanceValid(pair.Key))
				return pair.Key;
		foreach (var pair in stopNodes)
			if (pair.Value.State == CodexNodeState.Ready && IsInstanceValid(pair.Key))
				return pair.Key;
		// The first stop past the base, else the base.
		List<Control> line = treeLines.Where(l => l.Count > 0).Skip(1).FirstOrDefault() ?? treeLines.FirstOrDefault(l => l.Count > 0);
		return line?.FirstOrDefault();
	}

	private void FocusTree()
	{
		Control stop = FirstTreeStop();
		if (stop != null && IsInstanceValid(stop) && stop.IsVisibleInTree())
			stop.GrabFocus();
	}
}
