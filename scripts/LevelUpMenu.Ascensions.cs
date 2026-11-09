using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// The level 8 choice, drawn as a TREE: the spell on top, an arrow down to each of its paths. See
// .ai/level-up-screen.md, section 4e. The ascension browser that used to share these parts was replaced by the
// Evolution Codex (.ai/evolution-codex.md), which the level-up's Codex button opens.
public partial class LevelUpMenu
{
	// The ready node's face: gold hi, a step warmer and lighter than the page. Measured in
	// tools/art/ui_contrast.py (ASC_READY_FACE) - copy any change there.
	private static readonly Color AscensionReadyFace = new Color(1.0f, 0.941f, 0.753f);
	// How far a path's plate is dimmed, as a SelfModulate - the frame only, never the words on it, so
	// greying a plate can only darken the ground under light ink (ui_contrast.py, "ascension tree").
	private static readonly Color PathSeenDim = new Color(0.78f, 0.78f, 0.84f);
	private static readonly Color PathSealedDim = new Color(0.56f, 0.56f, 0.64f);

	private const string SealedName = "???";
	private const string SealedLine = "Sealed until you first meet its requirements.";

	// The plates, built once per menu (instance fields, never static - CardFamilyArt's remarks). The
	// content margin is 0 because the padding is an inner MarginContainer: the focus overlay is a
	// sibling of it and has to cover the whole plate, or its ring sits 20 inside the edge.
	private StyleBoxTexture treePlateResting, treePlateLit;

	private (StyleBoxTexture resting, StyleBoxTexture lit) TreePlates()
	{
		treePlateResting ??= CardFamilyArt.Frame(CardFamily.Ascension, false, 0);
		treePlateLit ??= CardFamilyArt.Frame(CardFamily.Ascension, true, 0);
		return (treePlateResting, treePlateLit);
	}

	/// <summary>
	/// The spell at the top of a tree: icon, name, its level as pips, and - in the browser - the
	/// checklist of gates its paths share.
	/// </summary>
	/// <param name="pipBecoming">The 0-based pip this choice reaches, animated (LevelPips); -1 for none.</param>
	/// <param name="stops">Filled with the node's focus stops: its element chips, or one overlay.</param>
	private Control BuildTreeNode(string spellId, string name, Texture2D icon, string levelText, int pipCount, int pipFilled,
		int pipBecoming, bool ready, List<Control> checklist, bool narrow, bool focusStop, List<Control> stops)
	{
		CodexRows.Palette ink = CodexRows.Vellum;
		var node = new PanelContainer
		{
			Name = $"AscensionNode_{spellId}",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		// A solid edge in the darkest ink, not the 45% one a quiet Codex row has: the node is the root
		// of a figure, and a root you have to squint for is not one. Ready, it takes the gold face.
		var style = new StyleBoxFlat
		{
			BgColor = ready ? AscensionReadyFace : ink.Face,
			BorderColor = ready ? InkGold : ink.Ink,
		};
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(6);
		style.SetContentMarginAll(0);
		node.AddThemeStyleboxOverride("panel", style);

		bool hasChips = checklist.Any(c => FindChips(c).Any());
		if (focusStop && !hasChips)
		{
			// First child, so the content draws over its wash; it ignores the mouse - to a finger the
			// node is text, and a drag that starts on it must reach the scroll.
			Button overlay = BonelightSkin.MakeCardClickOverlay(true);
			overlay.Name = "NodeFocus";
			overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
			node.AddChild(overlay);
			stops.Add(overlay);
		}

		var padding = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		padding.AddThemeConstantOverride("margin_left", 16);
		padding.AddThemeConstantOverride("margin_right", 16);
		padding.AddThemeConstantOverride("margin_top", 12);
		padding.AddThemeConstantOverride("margin_bottom", 14);
		node.AddChild(padding);

		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 6);
		padding.AddChild(column);

		var head = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		head.AddThemeConstantOverride("separation", 14);
		column.AddChild(head);
		if (icon != null)
		{
			head.AddChild(new TextureRect
			{
				Texture = icon,
				CustomMinimumSize = new Vector2(48, 48),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			});
		}

		// On a PC the checklist shares this row and takes the width left over, so the node is one
		// band - about 90 tall rather than 170 - and the paths under it start above the fold of a
		// 1280x720 screen. On a phone it gets lines of its own under the name.
		var identity = new VBoxContainer
		{
			SizeFlagsHorizontal = narrow ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkBegin,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		identity.AddThemeConstantOverride("separation", 2);
		head.AddChild(identity);
		Label title = CodexRows.MakeText(name, ResponsiveLayout.TextRole.Title, ink.Ink);
		// Never wrapped: beside the checklist flow it would otherwise give up its width a letter
		// at a time ("Cinderbre / ath"). The flow is what wraps.
		title.AutowrapMode = TextServer.AutowrapMode.Off;
		identity.AddChild(title);

		var levelRow = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		levelRow.AddThemeConstantOverride("separation", 10);
		Label level = CodexRows.MakeText(levelText, ResponsiveLayout.TextRole.Label, ink.InkMeta);
		level.AutowrapMode = TextServer.AutowrapMode.Off;
		levelRow.AddChild(level);
		if (pipCount > 0)
		{
			levelRow.AddChild(new LevelPips
			{
				Count = pipCount,
				Filled = Mathf.Clamp(pipFilled, 0, pipCount),
				Becoming = pipBecoming,
				SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			});
		}
		// With no checklist on a PC - the choice page - the level sits beside the name and the node is
		// one row, which on a 720-high canvas is a line of the plates under it above the fold.
		if (!narrow && checklist.Count == 0)
			head.AddChild(levelRow);
		else
			identity.AddChild(levelRow);

		if (checklist.Count > 0)
		{
			// Down the node on a phone, where width is scarce; across it on a PC, where height is.
			Container lines = narrow
				? new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }
				: new HFlowContainer
				{
					MouseFilter = Control.MouseFilterEnum.Ignore,
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
					SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
				};
			lines.AddThemeConstantOverride("separation", 4);
			lines.AddThemeConstantOverride("h_separation", 12);
			lines.AddThemeConstantOverride("v_separation", 6);
			foreach (Control line in checklist)
				lines.AddChild(line);
			if (narrow)
			{
				var gap = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
				gap.AddThemeConstantOverride("margin_top", 4);
				gap.AddChild(lines);
				column.AddChild(gap);
			}
			else
			{
				head.AddChild(lines);
			}
			stops.AddRange(checklist.SelectMany(FindChips));
		}
		else if (!narrow)
		{
			head.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore });
		}

		// "Ready" at the row's end. The line that used to follow it - "Waiting at the top of every
		// level-up" - is said once, in the page's summary, rather than on every ready spell.
		if (ready)
		{
			Control tag = BuildRequirementGlyphText("Ready", "done", ink.Reward, expand: false);
			tag.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			head.AddChild(tag);
		}

		return node;
	}

	private static IEnumerable<Control> FindChips(Node node)
	{
		if (node is ElementChip chip)
			yield return chip;
		foreach (Node child in node.GetChildren())
			foreach (Control found in FindChips(child))
				yield return found;
	}

	/// <summary>
	/// The level 8 page's mark (section 2): a path a recipe the save knows names carries two linked rings and
	/// the partner it joins, so the choice that opens or closes a recipe is made knowingly. The result is not
	/// named - only making it reveals that. Null when no known recipe names this path.
	/// </summary>
	private Control BuildConjunctionMark(string spellId, string ascensionId, bool onPlate)
	{
		foreach (ComboPreview combo in currentCombinations ?? new List<ComboPreview>())
		{
			if (combo == null || combo.Taken || !combo.Names.Any(n => n.SpellId == spellId && n.AscensionId == ascensionId))
				continue;
			ComboIngredient partner = ComboCatalog.PartnerOf(ComboCatalog.ForId(combo.Id), spellId);
			if (partner == null)
				continue;
			// Gold lit on the obsidian plate (7.8:1), the same advice ink the old level 8 cards wrote in. It wraps:
			// unwrapped, a phone's side-by-side plate took the line's whole width and squeezed its sibling's
			// name into "Starbur / st", and the browser's page with it past the screen's edge.
			Control mark = BuildRequirementGlyphText($"Joins {partner.Label}", "conjunction",
				onPlate ? CardFamilyArt.AscensionAdvice : CodexRows.Stone.InkSoft, expand: true);
			mark.Name = $"ConjunctionMark_{combo.Id}";
			return mark;
		}
		return null;
	}

	/// <summary>
	/// The level 4 page's mark (rung I of .ai/conjunction-progression.md): a path that would make a shown recipe's pair
	/// name a fourth element - and so seal it for the run - carries the two linked rings and "Seals Sagittarius", in the
	/// page's rubric (the red a scribe used for a line that mattered, measured 4.6:1 on the resting page). Read off
	/// <see cref="LevelUpOption.RecipeSeals"/>, which Player.NoteRecipeSeals fills; null when the path seals nothing.
	/// </summary>
	private Control BuildRecipeSealMark(LevelUpOption option, SpellEvolutionOption evo, bool centred)
	{
		if (option?.RecipeSeals == null || evo == null || !option.RecipeSeals.TryGetValue(evo.Id, out string recipes) || string.IsNullOrEmpty(recipes))
			return null;
		Control mark = BuildRequirementGlyphText($"Seals {recipes}", "conjunction", Rubric, expand: !centred);
		mark.Name = $"RecipeSeal_{evo.Id}";
		if (!centred)
			return mark;
		var centre = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		centre.AddChild(mark);
		return centre;
	}

	/// <summary>
	/// What is written on a path's plate: icon, name and what it does - or, sealed, the seal, "???"
	/// and one line saying why. <paramref name="extras"/> (the choice page's tag, element line and
	/// advice) are left out of a sealed path, since each would give part of it away.
	/// </summary>
	/// <param name="family">The plate it is written on: obsidian (an Ascension) or ember (the Apotheosis), for the inks.</param>
	/// <param name="sealedName">What a sealed plate is called: "???", or for the Apotheosis its name, which the character select already says.</param>
	/// <param name="extrasWhenSealed">Keep the extras on a sealed plate: the Apotheosis's "out of reach" gives nothing away.</param>
	private Control BuildPathContent(bool revealed, bool met, Texture2D icon, Color iconTint, string name, string description,
		IEnumerable<Control> extras, bool small, CardFamily family = CardFamily.Ascension, string sealedName = SealedName,
		bool extrasWhenSealed = false)
	{
		// The ember plate's inks (CardFamilyArt): Fire hot for the name, bone for what it does, Fire mid
		// for the quiet line - the Apotheosis page's own, measured on the ember face.
		bool ember = family == CardFamily.Apotheosis;
		bool bronze = family == CardFamily.Conjunction;
		Color titleInk = bronze ? CardFamilyArt.ConjunctionTitle : ember ? CardFamilyArt.ApotheosisTitle : CardFamilyArt.AscensionTitle;
		Color softInk = bronze ? CardFamilyArt.ConjunctionSoft : ember ? CardFamilyArt.ApotheosisSoft : CardFamilyArt.AscensionSoft;
		// Clear of the plate's rune band and the gilt line inside it (18 drawn pixels), with air.
		var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		margin.AddThemeConstantOverride("margin_left", 22);
		margin.AddThemeConstantOverride("margin_right", 22);
		margin.AddThemeConstantOverride("margin_top", 20);
		margin.AddThemeConstantOverride("margin_bottom", 20);

		// 64: two canvas units an art pixel, so the path's own picture (EvolutionIcons) is drawn whole.
		// At 48 it was one and a half, which nearest filtering draws as a mix of one- and two-unit
		// pixels; it was the spell's icon then and a thumbnail was enough.
		const float iconSide = 64f;
		Control picture = revealed
			? new TextureRect
			{
				Texture = icon ?? DefaultSpellIcon,
				CustomMinimumSize = new Vector2(iconSide, iconSide),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
				SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				// Faded toward the glass while it is out of reach - never darkened, which on obsidian
				// would simply erase it.
				Modulate = met ? iconTint : new Color(iconTint, 0.55f),
			}
			: new TextureRect
			{
				// The 24-pixel sealed glyph at exactly x2, so it stays on whole pixels.
				Texture = StatIcons.Load("sealed"),
				CustomMinimumSize = new Vector2(48, 48),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
				SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};

		// The picture beside the NAME only, and the words full width under both: a picture down the
		// side of every line cost a 1280x720 PC a line of description per plate, which was the line
		// that put the lock band under the fold.
		var words = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		words.AddThemeConstantOverride("separation", 6);
		margin.AddChild(words);

		Label title = CodexRows.MakeText(revealed ? name : sealedName, ResponsiveLayout.TextRole.Title, titleInk);
		title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		var rest = revealed || extrasWhenSealed ? (extras ?? Enumerable.Empty<Control>()).ToList() : new List<Control>();
		if (small)
		{
			// A phone's side-by-side plate is about 240 wide: beside the picture the name had 136, and
			// "Cataclysm" at the Title size broke mid-word. The picture goes above it instead.
			words.AddChild(picture);
			words.AddChild(title);
		}
		else
		{
			var titleRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			titleRow.AddThemeConstantOverride("separation", 12);
			picture.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			titleRow.AddChild(picture);
			// The first extra (the synergy tag, or the browser's Chosen mark) stands under the name,
			// beside the 64 picture, rather than on a row of its own under the description: the picture
			// grew from 48 when every path got one of its own (EvolutionIcons), and the row it saves is
			// what keeps a long path (Cerberus, Griffin) above the fold on a 1280x720 PC.
			if (rest.Count > 0)
			{
				var head = new VBoxContainer
				{
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
					SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
					MouseFilter = Control.MouseFilterEnum.Ignore,
				};
				head.AddThemeConstantOverride("separation", 4);
				head.AddChild(title);
				rest[0].SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
				head.AddChild(rest[0]);
				rest.RemoveAt(0);
				titleRow.AddChild(head);
			}
			else
			{
				titleRow.AddChild(title);
			}
			words.AddChild(titleRow);
		}

		if (revealed)
		{
			if (!string.IsNullOrWhiteSpace(description))
				words.AddChild(CodexRows.MakeText(description,
					small ? ResponsiveLayout.TextRole.Label : ResponsiveLayout.TextRole.Body, CardFamilyArt.AscensionInk));
			foreach (Control extra in rest)
				words.AddChild(extra);
		}
		else
		{
			words.AddChild(CodexRows.MakeText(SealedLine, ResponsiveLayout.TextRole.Label, softInk));
			foreach (Control extra in rest)
				words.AddChild(extra);
		}
		return margin;
	}

	/// <summary>
	/// Whether <paramref name="count"/> plates fit side by side under the node at their smallest
	/// readable width - if not, they stack and the fork becomes a spine. Measured from the panel as
	/// laid out, less the page's inset and the content's margins.
	/// </summary>
	private bool FitsAcross(int count, bool narrow)
	{
		float panelWidth = GetNodeOrNull<Control>("Panel")?.Size.X ?? 0f;
		if (panelWidth <= 0f)
			panelWidth = ResponsiveLayout.ViewportSize(this).X - 24f;
		const float side = 24f;
		float width = panelWidth - 2f * PageInset - 2f * side;
		float gap = narrow ? 12f : 20f;
		float each = count <= 0 ? width : (width - (count - 1) * gap) / count;
		// A Title-role name and a sentence a line need about this much plate, frame included.
		float least = narrow ? 240f : 300f;
		return each >= least;
	}

	/// <summary>
	/// The figure: the node, the fork, the plates. On a PC the node sits centred over the plates and
	/// a little narrower than them, so the shape is a fork rather than a heading over two boxes; on a
	/// phone it takes the full width, which is the scarce axis there.
	/// </summary>
	/// <param name="loneShare">How much of the row a lone plate takes, centred: half for a lone path.</param>
	private Control BuildTree(string name, Control node, List<(Control Outer, Control Plate)> cards, List<bool> open,
		bool narrow, bool sideBySide, bool spotlight = false, float loneShare = 0.5f)
	{
		var tree = new VBoxContainer
		{
			Name = name,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		tree.AddThemeConstantOverride("separation", 0);

		if (narrow)
		{
			tree.AddChild(node);
		}
		else
		{
			var nodeRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			nodeRow.AddThemeConstantOverride("separation", 0);
			nodeRow.AddChild(Spacer(0.07f));
			node.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			node.SizeFlagsStretchRatio = 0.86f;
			nodeRow.AddChild(node);
			nodeRow.AddChild(Spacer(0.07f));
			tree.AddChild(nodeRow);
		}

		var arrows = new AscensionForkArrows
		{
			Name = "Fork",
			Shape = sideBySide ? AscensionForkArrows.Layout.Fork : AscensionForkArrows.Layout.Spine,
			Source = node,
		};
		for (int i = 0; i < cards.Count; i++)
			arrows.AddTarget(cards[i].Plate, i < open.Count && open[i]);

		if (sideBySide)
		{
			arrows.CustomMinimumSize = new Vector2(0, narrow ? 48 : 44);
			tree.AddChild(arrows);
			var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			row.AddThemeConstantOverride("separation", narrow ? 12 : 20);
			// A lone path keeps half the width, centred under the node, rather than becoming a slab.
			bool lone = cards.Count == 1 && loneShare < 1f;
			if (lone)
				row.AddChild(Spacer((1f - loneShare) / 2f));
			foreach ((Control outer, Control _) in cards)
			{
				outer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				outer.SizeFlagsStretchRatio = cards.Count == 1 ? loneShare : 1f;
				row.AddChild(outer);
			}
			if (lone)
				row.AddChild(Spacer((1f - loneShare) / 2f));
			tree.AddChild(row);
			// The level 8 fork's plates are choices: the ones the selection is not on step down.
			if (spotlight)
				FocusSpotlight.Attach(row);
		}
		else
		{
			var body = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			body.AddThemeConstantOverride("separation", 0);
			arrows.CustomMinimumSize = new Vector2(40, 0);
			body.AddChild(arrows);
			var column = new VBoxContainer
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			column.AddThemeConstantOverride("separation", 14);
			foreach ((Control outer, Control _) in cards)
				column.AddChild(outer);
			if (spotlight)
				FocusSpotlight.Attach(column);
			// So the spine has length before its first turn.
			var drop = new MarginContainer
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			drop.AddThemeConstantOverride("margin_top", 16);
			drop.AddChild(column);
			body.AddChild(drop);
			tree.AddChild(body);
		}
		return tree;
	}

	private static Control Spacer(float ratio) => new Control
	{
		SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		SizeFlagsStretchRatio = ratio,
		MouseFilter = Control.MouseFilterEnum.Ignore,
	};

	/// <summary>
	/// Spell, then left path, then right path, then the next spell: the order the figure is read in,
	/// set explicitly rather than left to geometry, which from a node centred over two plates could
	/// land on either.
	/// </summary>
	private static void WireTreeFocus(List<(List<Control> Node, List<Control> Paths)> trees, Control after)
	{
		static void Link(Control from, Control to, Side side)
		{
			if (from == null || to == null || from == to || !from.IsInsideTree() || !to.IsInsideTree())
				return;
			NodePath path = from.GetPathTo(to);
			switch (side)
			{
				case Side.Top: from.FocusNeighborTop = path; break;
				case Side.Bottom: from.FocusNeighborBottom = path; break;
				case Side.Left: from.FocusNeighborLeft = path; break;
				case Side.Right: from.FocusNeighborRight = path; break;
			}
		}

		static void Order(Control from, Control to)
		{
			if (from == null || to == null || !from.IsInsideTree() || !to.IsInsideTree())
				return;
			from.FocusNext = from.GetPathTo(to);
			to.FocusPrevious = to.GetPathTo(from);
		}

		Control previousLast = null;
		List<Control> previousPaths = null;
		for (int t = 0; t < trees.Count; t++)
		{
			List<Control> node = trees[t].Node;
			List<Control> paths = trees[t].Paths;
			Control firstNode = node.FirstOrDefault();
			Control firstPath = paths.FirstOrDefault();

			for (int i = 0; i + 1 < node.Count; i++)
				Order(node[i], node[i + 1]);
			foreach (Control stop in node)
			{
				Link(stop, firstPath, Side.Bottom);
				if (previousPaths != null && previousPaths.Count > 0)
					Link(stop, previousPaths[0], Side.Top);
			}
			for (int i = 0; i < paths.Count; i++)
			{
				if (i > 0)
				{
					Link(paths[i], paths[i - 1], Side.Left);
					Link(paths[i - 1], paths[i], Side.Right);
					Order(paths[i - 1], paths[i]);
				}
				Link(paths[i], firstNode ?? firstPath, Side.Top);
			}
			if (node.Count > 0 && firstPath != null)
				Order(node[^1], firstPath);
			if (previousLast != null && (firstNode ?? firstPath) != null)
				Order(previousLast, firstNode ?? firstPath);
			if (previousPaths != null)
				foreach (Control stop in previousPaths)
					Link(stop, firstNode ?? firstPath, Side.Bottom);

			previousLast = paths.LastOrDefault() ?? node.LastOrDefault() ?? previousLast;
			previousPaths = paths.Count > 0 ? paths : previousPaths;
		}
		if (previousPaths != null && after != null)
		{
			foreach (Control stop in previousPaths)
				Link(stop, after, Side.Bottom);
			// And back up into the page, to the stop just above Back in reading order. Left to geometry,
			// up from Back landed on whatever plate was on screen - with the list at its top, the
			// Apotheosis's - and skipped every tree in between, as the level 8 fork's Back once did.
			Link(after, previousPaths[0], Side.Top);
		}
	}

	// ---- the level 8 choice: one spell's fork ---------------------------------------------------
	//
	// The same figure as the browser, for the one spell being ascended: the spell on top with its
	// eighth pip breathing (LevelPips - the level this choice makes it), the fork, and the paths as
	// obsidian plates that ARE the buttons. A path whose element gate is not met is shown locked,
	// with what it wants on a band under it and a dashed arrow into it - and if the player has never
	// met it, sealed as well, since this is the same "has the player stood here" rule the browser
	// follows. Only here does a tap choose.
	private void BuildAscensionChoicePage(LevelUpOption option)
	{
		var panel = GetNodeOrNull<Control>("Panel");
		if (panel == null)
			return;

		ClearButtons();
		SetNormalViewVisible(false);
		// A viewport resize re-invokes this while the view is already up (OnViewportSizeChanged).
		RemoveEvolutionRoot();

		// On the plate's stone, not a vellum page: the node is parchment, and a page on a page was the
		// same background twice (LevelUpMenu.PageInset's remarks).
		bool narrow = ResponsiveLayout.IsNarrow(this);
		var root = new VBoxContainer { Name = EvolutionRootName };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.OffsetLeft = narrow ? PageInset : 24;
		root.OffsetTop = narrow ? 16f : 26f;
		root.OffsetRight = narrow ? -PageInset : -24;
		root.OffsetBottom = -PageInset;
		root.AddThemeConstantOverride("separation", 6);
		panel.AddChild(root);

		root.AddChild(BuildPageHeading("★ ASCENSION (Level 8) ★"));

		// Counted, never asserted (the old page said "1 of 3" to a two-branch spell). The stakes in
		// the same breath: level 8 is reached only through one of these, so the other is gone.
		int count = option.EvolutionChoices?.Count ?? 0;
		root.AddChild(BuildPageSubtitle(count == 1
			? $"One path ascends {option.DisplayName} to level 8."
			: $"Choose 1 of {count}. {option.DisplayName} takes one, for good."));

		// A scroll, as before: on a 1280x720 PC a long description can still outgrow the band. Not
		// FollowFocus on a PC - focus moves sideways between plates there, and following it opened a
		// tall page scrolled to a plate's foot - but on a phone, where a stick can walk down a stack.
		var scroll = new ScrollContainer
		{
			Name = "AscensionChoiceScroll",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			FollowFocus = narrow,
		};
		root.AddChild(scroll);
		var inner = new MarginContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		inner.AddThemeConstantOverride("margin_top", 8);
		inner.AddThemeConstantOverride("margin_bottom", 8);
		scroll.AddChild(inner);

		int to = Mathf.Max(option.NextLevel, Player.AscensionSpellLevel);
		var noStops = new List<Control>();
		Control node = BuildTreeNode(option.SpellId, option.DisplayName, option.Icon, $"Level {to - 1} → {to}",
			to, to - 1, to - 1, ready: true, new List<Control>(), narrow, focusStop: false, noStops);

		bool sideBySide = FitsAcross(count, narrow);
		bool small = narrow && sideBySide;
		var cards = new List<(Control Outer, Control Plate)>();
		var buttons = new List<Button>();
		var open = new List<bool>();
		Button firstOpen = null;
		foreach (SpellEvolutionOption evo in option.EvolutionChoices ?? new List<SpellEvolutionOption>())
		{
			string reason = null;
			option.EvolutionLockReason?.TryGetValue(evo.Id, out reason);
			bool locked = !string.IsNullOrEmpty(reason);
			bool revealed = AscensionSightings.IsRevealed(this, option.SpellId, evo.Id, metNow: !locked);
			(Control outer, Control plate, Button button) = BuildChoicePathCard(option, evo, reason, revealed, small);
			cards.Add((outer, plate));
			buttons.Add(button);
			open.Add(!locked);
			if (!locked)
				firstOpen ??= button;
		}
		inner.AddChild(BuildTree($"AscensionTree_{option.SpellId}", node, cards, open, narrow, sideBySide, spotlight: true));

		var backButton = new Button
		{
			Name = "AscensionChoiceBack",
			Text = "Back",
			CustomMinimumSize = new Vector2(104, 48),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			SizeFlagsVertical = Control.SizeFlags.ShrinkEnd,
		};
		ResponsiveLayout.SetFont(backButton, ResponsiveLayout.TextRole.Label);
		backButton.Pressed += () =>
		{
			pendingEvolutionOption = null;
			BuildButtonsFrom(currentOptions);
		};
		BonelightSkin.StyleButton(backButton);
		root.AddChild(backButton);

		// Left path, right path, Back: the node has nothing to press here, so it is not a stop.
		var pathStops = buttons.Where(b => !b.Disabled).Cast<Control>().ToList();
		WireTreeFocus(new List<(List<Control>, List<Control>)> { (new List<Control>(), pathStops) }, backButton);
		if (pathStops.Count > 0)
			backButton.FocusNeighborTop = backButton.GetPathTo(pathStops[0]);

		// The open path is the primary action. MenuNavigator rescans every quarter second, so this
		// only decides where the selection starts.
		MenuNavigator.Attach(this, (Control)firstOpen ?? backButton);
	}

	/// <summary>
	/// A path on the level 8 page: the obsidian plate as the button. A PanelContainer holding the
	/// Button and the words as siblings, the phone card's build, so the plate grows to its text and
	/// is one target for a thumb and one ring for a stick.
	/// </summary>
	private (Control Outer, Control Plate, Button Button) BuildChoicePathCard(LevelUpOption option, SpellEvolutionOption evo,
		string lockReason, bool revealed, bool small)
	{
		bool locked = !string.IsNullOrEmpty(lockReason);
		var plate = new PanelContainer
		{
			Name = $"AscendPath_{evo.Id}",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		plate.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());

		(StyleBoxTexture resting, StyleBoxTexture lit) = TreePlates();
		var card = new Button
		{
			Name = $"AscendCard_{evo.Id}",
			Text = string.Empty,
			ClipText = false,
			Disabled = locked,
			// The frame only, as on the browser's plates.
			SelfModulate = locked ? (revealed ? PathSeenDim : PathSealedDim) : Colors.White,
		};
		CardFamilyArt.ApplyFrame(card, resting, lit);
		card.Pressed += () => OnEvolutionChosen(option, evo);
		plate.AddChild(card);

		// The tag, which way its element goes, and the advice - each a piece of what the path IS, so a
		// sealed path gets none of them.
		var extras = new List<Control>();
		if (revealed)
		{
			if (!string.IsNullOrWhiteSpace(evo.SynergyTag))
				extras.Add(BuildSynergyChip(evo.SynergyTag, CardFamily.Ascension, Control.SizeFlags.ShrinkBegin));
			Control elements = BuildTagGainRow(evo, option, FlowContainer.AlignmentMode.Begin, CardFamily.Ascension);
			if (elements != null)
				extras.Add(elements);
			// Two linked rings when a recipe the save knows names this path (ComboCatalog).
			if (BuildConjunctionMark(option.SpellId, evo.Id, onPlate: true) is Control conjunctionMark)
				extras.Add(conjunctionMark);
			// No advice line: it put both paths below a 1280x720 screen (BuildEvolutionOptionCard).
		}

		// The path's own picture, as drawn (EvolutionIcons); the spell's icon in the path's tint only
		// for one that has none.
		Texture2D own = EvolutionIcons.Own(evo);
		plate.AddChild(BuildPathContent(revealed, !locked, own ?? option.Icon, own != null ? Colors.White : evo.ModulateColor,
			evo.DisplayName, evo.Description, extras, small));

		if (!locked)
			return (plate, plate, card);

		// What the locked path wants, on a band under it: bone on red (10.1:1), as on the old page.
		var stamp = new Label
		{
			Text = lockReason,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		ResponsiveLayout.SetFont(stamp, ResponsiveLayout.TextRole.Label);
		var band = new StyleBoxFlat { BgColor = CardFamilyArt.LockBand };
		band.SetCornerRadiusAll(3);
		band.SetContentMarginAll(4);
		stamp.AddThemeStyleboxOverride("normal", band);
		stamp.AddThemeColorOverride("font_color", CardFamilyArt.LockInk);

		var outer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		outer.AddThemeConstantOverride("separation", 6);
		outer.AddChild(plate);
		outer.AddChild(stamp);
		return (outer, plate, card);
	}

	private static Control RequirementGlyph(string key)
	{
		// The 24-pixel stat glyphs, unscaled in a 32 box, exactly as CodexRows sets them.
		return new TextureRect
		{
			Texture = StatIcons.Load(key),
			CustomMinimumSize = new Vector2(32, 32),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepCentered,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
	}

	private static Control BuildRequirementGlyphText(string text, string glyph, Color colour, bool expand)
	{
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 8);
		if (expand)
			line.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		line.AddChild(RequirementGlyph(glyph));
		Label label = CodexRows.MakeText(text, ResponsiveLayout.TextRole.Label, colour);
		label.CustomMinimumSize = new Vector2(0, 32);
		if (expand)
			label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		else
			label.AutowrapMode = TextServer.AutowrapMode.Off;
		line.AddChild(label);
		return line;
	}
}
