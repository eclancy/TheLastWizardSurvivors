using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

// THE DETAIL of the selected entry (.ai/evolution-codex.md, sections 2-4): a spell's whole tree with its
// requirements, an item with its sets, a recipe as ingredients into a result. On a PC it is the panel on the
// right and its tree reads left to right in columns - base, the level 4 mutations, the level 8 Ascensions,
// the final form - with gold arrows into what is open and dashed ones into what is not (AscensionForkArrows,
// the browser's own lines). On a phone it is the sheet, and the tree reads top to bottom in rows.
//
// Everything a player reads here was on the Ascensions browser it replaced, and nothing it showed was lost:
// the gates, each met or not with the shortfall in numbers on the rubric band; sealed paths ("???" and the
// sealed glyph) until first met; the path taken and the one closed; the Apotheosis with its own gates and
// "Out of reach this run"; the recipes a path joins. What a path DOES is one line in the reading box under
// the tree, for the node the selection or the pointer is on, so the tree itself stays names and chips.
public partial class EvolutionCodex
{
	/// <summary>What a sealed node says (AscensionSightings' rule, section 4e of the level-up doc).</summary>
	private const string SealedLine = "Sealed until you first meet its requirements.";

	/// <summary>The tree's focus stops, line by line: a column each on a PC, a row each on a phone.</summary>
	private readonly List<List<Control>> treeLines = new();
	private readonly Dictionary<Control, CodexNode> stopNodes = new();
	private TextureRect readingIcon;
	private Label readingName;
	private Label readingLine;
	private Control usedInRow;

	private void ShowDetail()
	{
		if (detail == null || !IsInstanceValid(detail))
			return;
		foreach (Node child in detail.GetChildren())
		{
			detail.RemoveChild(child);
			child.QueueFree();
		}
		treeLines.Clear();
		stopNodes.Clear();
		readingIcon = null;
		readingName = null;
		readingLine = null;
		usedInRow = null;

		CodexEntry entry = model.Get(selectedId);
		if (entry == null)
		{
			detail.AddChild(CodexRows.MakeText("Nothing chosen.", ResponsiveLayout.TextRole.Body, CodexRows.Stone.InkSoft));
			return;
		}

		detail.AddChild(BuildHead(entry));
		if (!entry.Discovered)
			return;

		switch (entry.Kind)
		{
			case CodexKind.Spell:
				BuildSpellDetail(entry);
				break;
			case CodexKind.Relic:
			case CodexKind.Boon:
				BuildItemDetail(entry);
				break;
			default:
				BuildRecipeDetail(entry, entry.Recipe, heading: null);
				break;
		}
		WireTreeFocus();
		if (IsInstanceValid(detailScroll))
			detailScroll.ScrollVertical = 0;
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

		// On a phone the sheet's band already names it ("< Codex   Cinderbreath").
		if (!narrow)
		{
			Label name = CodexRows.MakeText(entry.ShownName, ResponsiveLayout.TextRole.Title, CodexRows.Stone.Ink);
			name.Name = "CodexDetailName";
			info.AddChild(name);
		}

		var meta = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		meta.AddThemeConstantOverride("h_separation", 10);
		meta.AddThemeConstantOverride("v_separation", 4);
		info.AddChild(meta);
		Label kind = CodexRows.MakeText(entry.Discovered ? entry.KindLine : KindOf(entry), ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkSoft);
		kind.AutowrapMode = TextServer.AutowrapMode.Off;
		meta.AddChild(kind);
		if (entry.Discovered)
		{
			foreach (var (element, weight) in entry.Elements)
				meta.AddChild(CodexRows.BuildChip(element, weight > 1 ? $"{element} x{weight}" : element.ToString(), focusable: false));
			// A held spell's level is on its base node (PC) and its pips on its cell; on a phone the base node
			// is a row of its own, so the head says it there instead.
			if (narrow && entry.Held && entry.Kind == CodexKind.Spell && entry.LevelCap > 0)
			{
				var level = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
				level.AddThemeConstantOverride("separation", 8);
				Label words = CodexRows.MakeText($"Level {entry.Level}", ResponsiveLayout.TextRole.Label, CodexRows.Stone.Ink);
				words.AutowrapMode = TextServer.AutowrapMode.Off;
				level.AddChild(words);
				level.AddChild(new LevelPips
				{
					Count = entry.LevelCap,
					Filled = Mathf.Clamp(entry.Level, 0, entry.LevelCap),
					SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
					MouseFilter = Control.MouseFilterEnum.Ignore,
				});
				meta.AddChild(level);
			}
			else if (entry.Kind == CodexKind.FullSet && model.Mode == CodexMode.InRun)
			{
				Label held = CodexRows.MakeText($"{entry.Level}/{entry.LevelCap} held", ResponsiveLayout.TextRole.Label, CodexRows.Stone.Ink);
				held.AutowrapMode = TextServer.AutowrapMode.Off;
				meta.AddChild(held);
			}
		}

		if (entry.Discovered && !string.IsNullOrWhiteSpace(entry.Line))
		{
			// A spell's tagline is one short line (34 characters, SpellData.TaglineMaxLength): it rides with the
			// chips, which keeps the head one band and the tree under it above the fold. An item's effect is a
			// sentence, and gets its own.
			Label line = CodexRows.MakeText(entry.Line, ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkSoft);
			if (entry.Kind == CodexKind.Spell && !narrow)
			{
				line.AutowrapMode = TextServer.AutowrapMode.Off;
				meta.AddChild(line);
			}
			else
				info.AddChild(line);
		}
		else
		{
			// How it is found: the one useful thing a sealed entry has (UnlockCatalog's line for a spell).
			Texture2D glyph = entry.Kind == CodexKind.Spell ? CodexRows.SpellHintGlyph(entry.Id) : StatIcons.Load("lock");
			info.AddChild(GlyphLine(glyph, entry.Hint, CodexRows.Stone.InkSoft));
		}

		if (StateTag(entry) is Control tag)
			head.AddChild(tag);
		if (CurationToggle(entry) is Control curation)
			head.AddChild(curation);
		return head;
	}

	private static string KindOf(CodexEntry entry) => entry.Kind switch
	{
		CodexKind.Spell => "Spell",
		CodexKind.Relic => "Relic",
		CodexKind.Boon => "Boon",
		CodexKind.Combination => "Conjunction",
		_ => "Full Set Enchantment",
	};

	// "Ready", "Held", "Owned", "Made": the cell's state said once in words.
	private Control StateTag(CodexEntry entry)
	{
		(string text, string glyph, Color ink) = entry.State switch
		{
			CodexState.Ready => ("Ready", "done", CodexRows.Stone.Reward),
			CodexState.Taken when entry.Kind == CodexKind.Spell => ("Maxed", "done", CodexRows.Stone.Reward),
			CodexState.Taken when entry.Kind == CodexKind.Combination => ("Made", "done", CodexRows.Stone.Reward),
			CodexState.Taken when entry.Kind == CodexKind.FullSet => ("Complete", "done", CodexRows.Stone.Reward),
			CodexState.Taken => ("Held", "done", CodexRows.Stone.Reward),
			CodexState.Owned when entry.Kind == CodexKind.Combination => ("Made", "done", CodexRows.Stone.InkSoft),
			_ => (null, null, default),
		};
		if (text == null)
			return null;
		Control tag = GlyphLine(StatIcons.Load(glyph), text, ink, expand: false);
		tag.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
		return tag;
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
		CodexRows.StyleQuietToggle(button, CodexRows.Stone);
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
		RebuildGrid();
		ShowDetail();
		UpdateCount();
		if (detail.FindChild("CurationToggle_" + id, true, false) is Button again && !again.Disabled)
			again.GrabFocus();
	}

	// ---- a spell: its tree --------------------------------------------------------------------------

	private void BuildSpellDetail(CodexEntry entry)
	{
		if (entry.Forms.Count == 0)
			return;
		int shown = formShown >= 0 && formShown < entry.Forms.Count ? formShown : Mathf.Clamp(entry.CurrentForm, 0, entry.Forms.Count - 1);
		if (entry.Forms.Count > 1)
			detail.AddChild(BuildFormTabs(entry, shown));
		CodexForm form = entry.Forms[shown];
		if (form.Mutations.Count == 0 && form.Ascensions.Count == 0 && form.Finals.Count == 0)
		{
			detail.AddChild(CodexRows.MakeText("No evolutions.", ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkSoft));
			return;
		}
		detail.AddChild(narrow ? BuildTreeRows(entry, form) : BuildTreeColumns(entry, form));
		detail.AddChild(BuildReading());
		if (entry.UsedIn.Count > 0)
			detail.AddChild(BuildUsedIn(entry));
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
			CodexRows.StyleTab(tab, CodexRows.Stone);
			ResponsiveLayout.EnsureTouchTarget(tab, 0f);
			int index = i;
			tab.Pressed += () =>
			{
				formShown = index;
				litNodeId = string.Empty;
				ShowDetail();
				(detail.FindChild($"CodexForm_{entry.Forms[index].Id}", true, false) as Button)?.GrabFocus();
			};
			row.AddChild(tab);
		}
		return row;
	}

	// An arrow is gold into what is open now or was taken, dashed into everything else.
	private static bool IsLive(CodexNode node) => node.State is CodexNodeState.Ready or CodexNodeState.Taken;

	private Control Column(string label, List<CodexChip> gates, List<Control> nodes, float fixedWidth)
	{
		var column = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = fixedWidth > 0f ? Control.SizeFlags.ShrinkBegin : Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(fixedWidth, 0f),
		};
		column.AddThemeConstantOverride("separation", 8);
		column.AddChild(ColumnHeader(label, gates));
		foreach (Control node in nodes)
			column.AddChild(node);
		return column;
	}

	// "Level 8" and the chips every node in the column shares ("Spell 7/7", "Fire 4/4", "You 17/17").
	private Control ColumnHeader(string label, List<CodexChip> gates)
	{
		var head = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		head.AddThemeConstantOverride("separation", 4);
		Label words = CodexRows.MakeText(label, ResponsiveLayout.TextRole.Label, CodexRows.Stone.Heading);
		words.AutowrapMode = TextServer.AutowrapMode.Off;
		head.AddChild(words);
		if (gates != null && gates.Count > 0)
			head.AddChild(ChipFlow(gates));
		return head;
	}

	private Control BuildBaseNode(CodexEntry entry, CodexForm form, bool wide)
	{
		if (!wide)
			return BuildCompactBase(entry, form);
		var node = new CodexNode
		{
			Id = $"base_{entry.Id}",
			SpellId = entry.Id,
			Milestone = 0,
			Name = wide ? (entry.Forms.Count > 1 ? $"{entry.Name}: {form.Name}" : entry.Name) : (entry.Held ? $"Lv {entry.Level}" : form.Name),
			Line = entry.Line,
			Icon = form.Icon ?? entry.Icon,
			State = entry.Held ? CodexNodeState.Taken : CodexNodeState.Known,
		};
		if (entry.Held)
			node.Note = wide ? $"Level {entry.Level} of {entry.LevelCap}" : string.Empty;
		return BuildNode(node, wide, baseOf: entry);
	}

	// On a PC the base is the spell's picture at its own size and, held, its level: the head above already
	// names it, and a name in an 84-unit column broke a letter a line.
	private Control BuildCompactBase(CodexEntry entry, CodexForm form)
	{
		var node = new CodexNode
		{
			Id = $"base_{entry.Id}",
			SpellId = entry.Id,
			Name = entry.Forms.Count > 1 ? $"{entry.Name}: {form.Name}" : entry.Name,
			Line = entry.Line,
			Icon = form.Icon ?? entry.Icon,
			State = entry.Held ? CodexNodeState.Taken : CodexNodeState.Known,
		};
		var box = new PanelContainer { Name = $"CodexNode_{node.Id}", MouseFilter = Control.MouseFilterEnum.Ignore };
		box.AddThemeStyleboxOverride("panel", NodeStyle(CodexNodeState.Known, CodexInk.HeldRim, lit: false, isBase: true));
		Button stop = BonelightSkin.MakeCardClickOverlay(true, 4);
		stop.Name = "NodeStop";
		stop.MouseFilter = Control.MouseFilterEnum.Pass;
		stop.FocusEntered += () => ShowReading(node);
		stop.MouseEntered += () => ShowReading(node);
		box.AddChild(stop);
		stopNodes[stop] = node;
		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
		column.AddThemeConstantOverride("separation", 2);
		box.AddChild(column);
		TextureRect icon = Picture(node.Icon ?? DefaultIcon, 32f, dim: false);
		icon.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		column.AddChild(icon);
		if (entry.Held)
		{
			Label level = CodexRows.MakeText($"Lv {entry.Level}", ResponsiveLayout.TextRole.Label, CodexRows.Stone.Ink);
			level.HorizontalAlignment = HorizontalAlignment.Center;
			level.AutowrapMode = TextServer.AutowrapMode.Off;
			column.AddChild(level);
		}
		return box;
	}

	/// <summary>
	/// One node (section 3): its picture and name on a frame that says its state, its own gates as chips,
	/// a quiet note. A focus stop for the stick and a hover target for the pointer, both of which put what it
	/// does in the reading box.
	/// </summary>
	private Control BuildNode(CodexNode node, bool wide, CodexEntry baseOf = null)
	{
		var box = new PanelContainer
		{
			Name = $"CodexNode_{node.Id}",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		Color mark = baseOf != null ? CodexInk.HeldRim : CodexInk.MarkFor(node.Milestone);
		bool lit = !string.IsNullOrEmpty(litNodeId) && node.Id.Equals(litNodeId, StringComparison.OrdinalIgnoreCase);
		box.AddThemeStyleboxOverride("panel", NodeStyle(node.State, mark, lit, baseOf != null));

		Button stop = BonelightSkin.MakeCardClickOverlay(true, 4);
		stop.Name = "NodeStop";
		stop.MouseFilter = Control.MouseFilterEnum.Pass;
		stop.FocusEntered += () => ShowReading(node);
		stop.MouseEntered += () => ShowReading(node);
		if (!string.IsNullOrEmpty(node.OpensEntryId) && node.OpensEntryId != selectedId && model.Get(node.OpensEntryId) is CodexEntry opens && opens.Discovered)
		{
			// An ingredient opens its own entry: the reverse lookup walks both ways.
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
		content.AddThemeConstantOverride("separation", 4);
		pad.AddChild(content);

		// The base is held, not taken: it keeps the stone face, so it keeps the stone's ink.
		bool taken = node.State == CodexNodeState.Taken && baseOf == null;
		Color ink = taken ? CodexInk.MarkInk : node.State is CodexNodeState.Sealed or CodexNodeState.Closed ? CodexRows.Stone.InkSoft : CodexRows.Stone.Ink;
		var title = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		title.AddThemeConstantOverride("separation", 6);
		content.AddChild(title);
		Texture2D picture = node.State == CodexNodeState.Sealed ? StatIcons.Load("sealed") : node.Icon ?? DefaultIcon;
		// 32: the 64 art halved, or the 24 sealed glyph unscaled in a 32 box.
		TextureRect icon = Picture(picture, 32f, dim: node.State == CodexNodeState.Closed);
		if (node.State == CodexNodeState.Sealed)
			icon.StretchMode = TextureRect.StretchModeEnum.KeepCentered;
		title.AddChild(icon);
		Label name = CodexRows.MakeText(node.ShownName, wide ? ResponsiveLayout.TextRole.Label : ResponsiveLayout.TextRole.Label, ink);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		title.AddChild(name);
		if (node.State == CodexNodeState.Closed)
			title.AddChild(Glyph("lock"));

		if (node.Chips.Count > 0)
			content.AddChild(ChipFlow(node.Chips));
		// No "Ready" line: the lit frame and its breathing ring say it, and the line cost the tree its fold.
		string note = node.Note;
		if (!string.IsNullOrEmpty(note) && (node.Revealed || node.NameAlwaysShown || node.State == CodexNodeState.Closed))
		{
			Label quiet = CodexRows.MakeText(note, ResponsiveLayout.TextRole.Micro,
				taken ? CodexInk.MarkInk : node.State == CodexNodeState.Ready ? CodexRows.Stone.Reward : CodexRows.Stone.InkSoft);
			content.AddChild(quiet);
		}

		if (node.State == CodexNodeState.Ready)
			AscensionReadyGlow.AttachTo(box, showRing: true).SetActive(true);
		return box;
	}

	private static StyleBoxFlat NodeStyle(CodexNodeState state, Color mark, bool lit, bool isBase)
	{
		var box = new StyleBoxFlat();
		switch (state)
		{
			case CodexNodeState.Taken:
				// Solid in the milestone's colour: the loadout strip's gold, cyan, fire (section 3).
				box.BgColor = isBase ? CodexRows.Stone.Face : mark;
				box.BorderColor = mark;
				break;
			case CodexNodeState.Ready:
				box.BgColor = CodexRows.Stone.Face;
				box.BorderColor = CodexRows.Stone.Reward;
				break;
			case CodexNodeState.Sealed:
			case CodexNodeState.Closed:
				box.BgColor = new Color(CodexInk.StoneDeep, 0.7f);
				box.BorderColor = CodexRows.Stone.QuietEdge;
				break;
			default:
				box.BgColor = CodexRows.Stone.Face;
				box.BorderColor = CodexRows.Stone.Edge;
				break;
		}
		box.SetBorderWidthAll(state is CodexNodeState.Sealed or CodexNodeState.Closed ? 1 : 2);
		if (lit)
		{
			// A search hit: the node it named, ringed in gold hi.
			box.BorderColor = CodexInk.SelectedRim;
			box.SetBorderWidthAll(3);
		}
		box.SetCornerRadiusAll(4);
		box.SetContentMarginAll(0);
		return box;
	}

	private static Control StopOf(Control node) => node.GetNodeOrNull<Control>("NodeStop") ?? node;

	// ---- chips --------------------------------------------------------------------------------------

	// Short chips flow side by side; a long one ("L8: Dragon's Breath") takes a line of its own and wraps, so
	// no chip can force its column wider than the tree has room for.
	private const int LongChip = 13;

	private Control ChipFlow(List<CodexChip> chips)
	{
		var stack = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		stack.AddThemeConstantOverride("separation", 4);
		HFlowContainer flow = null;
		foreach (CodexChip chip in chips)
		{
			bool wide = chip.Element == null && chip.Text.Length > LongChip;
			if (wide)
			{
				flow = null;
				stack.AddChild(BuildChip(chip, wrap: true));
				continue;
			}
			if (flow == null)
			{
				flow = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
				flow.AddThemeConstantOverride("h_separation", 6);
				flow.AddThemeConstantOverride("v_separation", 4);
				stack.AddChild(flow);
			}
			flow.AddChild(BuildChip(chip, wrap: false));
		}
		return stack;
	}

	/// <summary>
	/// A requirement as a chip. An element's is its own colour (a tap opens what the element does); the rest
	/// are pills. Measured in a run: met with a tick, unmet on the rubric band with the socket, closed with a
	/// lock. In the collection there is nothing to measure, and the pill is only an outline.
	/// </summary>
	private Control BuildChip(CodexChip chip, bool wrap)
	{
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 3);
		if (chip.Element is Element element)
		{
			if (chip.Met == false)
				line.AddChild(Glyph(chip.Closed ? "lock" : "todo"));
			line.AddChild(CodexRows.BuildChip(element, chip.Text, focusable: false));
			return line;
		}
		var pill = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		var style = new StyleBoxFlat();
		Color ink = CodexRows.Stone.InkSoft;
		string glyph = null;
		if (chip.Met == true)
		{
			// Met: the steel rim and the full count ("7/7") - the numbers say it, no tick needed.
			style.BgColor = CodexRows.Stone.Face;
			style.BorderColor = CodexInk.HeldRim;
		}
		else if (chip.Met == false && chip.Closed)
		{
			style.BgColor = new Color(0, 0, 0, 0);
			style.BorderColor = CodexRows.Stone.QuietEdge;
			glyph = "lock";
		}
		else if (chip.Met == false)
		{
			style.BgColor = CodexInk.UnmetBand;
			style.BorderColor = CodexInk.UnmetBand;
			ink = CodexInk.UnmetInk;
			glyph = "todo";
		}
		else
		{
			style.BgColor = new Color(0, 0, 0, 0);
			style.BorderColor = CodexRows.Stone.Edge;
		}
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(4);
		style.ContentMarginLeft = glyph != null ? 2 : 8;
		style.ContentMarginRight = 8;
		style.ContentMarginTop = 1;
		style.ContentMarginBottom = 1;
		pill.AddThemeStyleboxOverride("panel", style);
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 2);
		if (glyph != null)
			row.AddChild(Glyph(glyph));
		Label words = CodexRows.MakeText(chip.Text, ResponsiveLayout.TextRole.Label, ink);
		if (wrap)
		{
			words.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			pill.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			line.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		}
		else
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

	private static Control GlyphLine(Texture2D glyph, string text, Color ink, bool expand = true)
	{
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 8);
		if (expand)
			row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
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
		if (expand)
			label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		else
			label.AutowrapMode = TextServer.AutowrapMode.Off;
		row.AddChild(label);
		return row;
	}

	// ---- the reading box: what the node under the selection does --------------------------------

	private Control BuildReading()
	{
		var box = new PanelContainer { Name = "CodexReading", MouseFilter = Control.MouseFilterEnum.Ignore };
		var style = new StyleBoxFlat { BgColor = new Color(CodexInk.StoneDeep, 0.8f), BorderColor = CodexRows.Stone.QuietEdge };
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(4);
		style.ContentMarginLeft = 10;
		style.ContentMarginRight = 10;
		style.ContentMarginTop = 6;
		style.ContentMarginBottom = 6;
		box.AddThemeStyleboxOverride("panel", style);
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 10);
		box.AddChild(row);
		readingIcon = Picture(null, 32f, dim: false);
		row.AddChild(readingIcon);
		var words = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		words.AddThemeConstantOverride("separation", 2);
		row.AddChild(words);
		readingName = CodexRows.MakeText(string.Empty, ResponsiveLayout.TextRole.Label, CodexRows.Stone.Heading);
		words.AddChild(readingName);
		readingLine = CodexRows.MakeText(string.Empty, ResponsiveLayout.TextRole.Label, CodexRows.Stone.Ink);
		// Two lines on a PC hold any description (the 130-character cap, ValidateCardTextBudget).
		readingLine.CustomMinimumSize = new Vector2(0f, narrow ? 0f : 48f);
		words.AddChild(readingLine);
		return box;
	}

	private void ShowReading(CodexNode node)
	{
		if (node == null || readingName == null || !IsInstanceValid(readingName))
			return;
		bool open = node.Revealed;
		readingIcon.Texture = open ? node.Icon ?? DefaultIcon : StatIcons.Load("sealed");
		readingIcon.StretchMode = open ? TextureRect.StretchModeEnum.KeepAspectCentered : TextureRect.StretchModeEnum.KeepCentered;
		readingName.Text = node.ShownName;
		readingLine.Text = open ? node.Line : SealedLine;
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

	// ---- used in: the recipes a spell is an ingredient of -----------------------------------------

	private Control BuildUsedIn(CodexEntry entry)
	{
		var row = new HFlowContainer { Name = "CodexUsedIn", MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("h_separation", 10);
		row.AddThemeConstantOverride("v_separation", 4);
		Label label = CodexRows.MakeText("Used in", ResponsiveLayout.TextRole.Label, CodexRows.Stone.Heading);
		label.AutowrapMode = TextServer.AutowrapMode.Off;
		row.AddChild(label);
		var stops = new List<Control>();
		foreach (string id in entry.UsedIn.Take(CodexModel.MaxUsedIn))
		{
			CodexEntry recipe = model.Get(id);
			if (recipe?.Recipe == null)
				continue;
			string partner = string.Join(", ", recipe.Recipe.Ingredients
				.Where(i => !i.SpellId.Equals(entry.Id, StringComparison.OrdinalIgnoreCase))
				.Select(i => i.Milestone == 8 ? $"{i.Line}: {i.Name}" : i.ShownName));
			string result = recipe.Recipe.Result.Revealed ? recipe.Recipe.Result.Name : CodexModel.SealedName;
			var button = new Button
			{
				Name = $"CodexUsedIn_{id}",
				Text = string.IsNullOrEmpty(partner) ? $"✶ {result}" : $"✶ {result}  (+ {partner})",
				FocusMode = Control.FocusModeEnum.All,
			};
			ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Label);
			CodexRows.StyleQuietToggle(button, CodexRows.Stone);
			ResponsiveLayout.EnsureTouchTarget(button, 0f);
			button.Pressed += () => SelectEntry(id);
			row.AddChild(button);
			stops.Add(button);
		}
		if (stops.Count > 0)
			treeLines.Add(stops);
		usedInRow = row;
		return row;
	}

	// ---- an item: what it does, and its sets -----------------------------------------------------

	private void BuildItemDetail(CodexEntry entry)
	{
		if (entry.Kind == CodexKind.Boon)
		{
			if (model.Mode == CodexMode.Collection && !entry.Owned)
				detail.AddChild(GlyphLine(StatIcons.Load("lock"), entry.Hint, CodexRows.Stone.InkSoft));
			return;
		}
		bool any = false;
		foreach (string id in entry.UsedIn)
		{
			CodexEntry set = model.Get(id);
			if (set?.Recipe == null)
				continue;
			any = true;
			BuildRecipeDetail(set, set.Recipe, heading: set.Name);
		}
		if (!any)
			detail.AddChild(CodexRows.MakeText("Part of no set.", ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkSoft));
	}

	// ---- a recipe: ingredients into a result ------------------------------------------------------

	/// <summary>
	/// A recipe as ingredients into its result (section 4): a combination's two named Ascensions, or a set's
	/// pieces, then the arrow, then the result - the combination sealed until made, the enchantment with its
	/// bonus. Selecting it, or searching a name in it, is the reverse lookup: the same card, ingredients first.
	/// </summary>
	private void BuildRecipeDetail(CodexEntry entry, CodexRecipe recipe, string heading)
	{
		if (recipe == null)
			return;
		if (!string.IsNullOrEmpty(heading))
		{
			var head = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			head.AddThemeConstantOverride("separation", 10);
			head.AddChild(Picture(recipe.Result.Icon ?? DefaultIcon, 32f, dim: false));
			Label title = CodexRows.MakeText(recipe.Kind == CodexKind.FullSet ? $"Full Set Enchantment: {heading}" : heading,
				ResponsiveLayout.TextRole.Label, CodexRows.Stone.Heading);
			title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			head.AddChild(title);
			detail.AddChild(head);
		}

		if (narrow)
			BuildRecipePhone(recipe);
		else
			BuildRecipePc(recipe);

		// A set's two tiers, each ticked once this run holds enough of it (the chest page's "now").
		bool partialOn = recipe.PartialCount > 0 && recipe.HeldCount >= recipe.PartialCount;
		if (!string.IsNullOrEmpty(recipe.Partial))
			detail.AddChild(GlyphLine(StatIcons.Load(partialOn ? "done" : "todo"), recipe.Partial, partialOn ? CodexRows.Stone.Ink : CodexRows.Stone.InkSoft));
		// A recipe's own page needs no reading box: the head says what the result does, and each ingredient
		// opens its own spell or relic (A, or a click), which is where what it does is written.
		if (!string.IsNullOrEmpty(heading) && recipe.Result.Revealed)
		{
			// Under an item, the enchantment's bonus is said outright: there is no reading box for a set inside an item.
			detail.AddChild(GlyphLine(StatIcons.Load(recipe.Made ? "done" : "todo"), recipe.Result.Line, CodexRows.Stone.Ink));
		}
	}

	private static Control Spacer() => new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };

	// ---- the tree's focus -----------------------------------------------------------------------------

	/// <summary>
	/// The stick walks the tree line by line - down a column and across to the next on a PC, along a row and
	/// down to the next on a phone - explicitly, because Godot's geometry from a node beside a tall column
	/// could land anywhere. The first line steps back to the cell it belongs to, and nothing walks out of the
	/// detail into the header.
	/// </summary>
	private void WireTreeFocus()
	{
		var lines = treeLines.Where(l => l.Count > 0).ToList();
		if (lines.Count == 0)
			return;
		cells.TryGetValue(selectedId, out Button cell);
		Side along = narrow ? Side.Right : Side.Bottom;
		Side back = narrow ? Side.Left : Side.Top;
		Side next = narrow ? Side.Bottom : Side.Right;
		Side previous = narrow ? Side.Top : Side.Left;
		for (int l = 0; l < lines.Count; l++)
		{
			List<Control> line = lines[l];
			for (int i = 0; i < line.Count; i++)
			{
				Control stop = line[i];
				Link(stop, i + 1 < line.Count ? line[i + 1] : stop, along);
				Link(stop, i > 0 ? line[i - 1] : stop, back);
				Link(stop, l + 1 < lines.Count ? Nearest(lines[l + 1], i, line.Count) : stop, next);
				Control before = l > 0 ? Nearest(lines[l - 1], i, line.Count) : !narrow && cell != null && IsInstanceValid(cell) ? cell : stop;
				if (narrow && l == 0)
					before = sheet?.FindChild("CodexSheetBack", true, false) as Control ?? stop;
				Link(stop, before, previous);
			}
		}
	}

	private static Control Nearest(List<Control> line, int index, int count)
	{
		if (line.Count == 0)
			return null;
		// The same place along the other line, proportionally: the middle of three lands on the middle of two.
		int at = count <= 1 ? 0 : Mathf.RoundToInt((float)index / (count - 1) * (line.Count - 1));
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
		// The node the reading box opened on, else the first stop past the base.
		foreach (var pair in stopNodes)
			if (pair.Value.State == CodexNodeState.Ready && IsInstanceValid(pair.Key))
				return pair.Key;
		List<Control> line = treeLines.Skip(1).FirstOrDefault(l => l.Count > 0) ?? treeLines.FirstOrDefault();
		return line?.FirstOrDefault();
	}

	private void FocusTree()
	{
		Control stop = FirstTreeStop();
		if (stop != null && IsInstanceValid(stop) && stop.IsVisibleInTree())
			stop.GrabFocus();
	}
}
