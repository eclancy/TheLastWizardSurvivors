using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

// THE CODEX'S ARRANGEMENT, LAYOUT B "THE BOOK" (Eric's pick, 2026-11-02), one builder per canvas and kept apart
// from everything else, so it says only where the parts go - the model, search, keyboard, index lines, nodes,
// chips and input live in the other files (EvolutionCodex.cs, EvolutionCodex.Detail.cs, CodexKeyboard.cs).
//
//   PC      the title, the count, the bookmarks on the book's top edge and Back over an open book on the
//           stone plate: the index on the left page (the search at its top, the list scrolling under it),
//           the selected entry on the right page, its tree down the page.
//   Phone   the title, the count and Back, the bookmarks, then one page filling the plate edge to edge: the
//           index, or - turned to - the entry's page with "‹ Index" at its top. The two pages are siblings
//           in one holder, so a turned page covers exactly what the index did.
public partial class EvolutionCodex
{
	/// <summary>The index page on a PC: wide enough for the longest name with its pips beside it.</summary>
	private const float IndexPageWidth = 420f;

	/// <summary>The PC's open book.</summary>
	private void BuildPcBook()
	{
		VBoxContainer column = BuildPlate();
		column.AddThemeConstantOverride("separation", 0);

		// The bookmarks stand on the book's top edge, so the row they are in sits flush on it.
		Control top = BuildTitleRow(withBack: false);
		top.AddChild(BuildBookmarks());
		backButton = BuildBackButton();
		backButton.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		var backGap = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		backGap.AddThemeConstantOverride("margin_left", 10);
		backGap.AddThemeConstantOverride("margin_bottom", 6);
		backGap.AddChild(backButton);
		top.AddChild(backGap);
		column.AddChild(top);

		var book = new HBoxContainer { Name = "CodexBook", SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		book.AddThemeConstantOverride("separation", 0);
		column.AddChild(book);

		PanelContainer left = BuildBookPage("CodexIndexPage");
		left.CustomMinimumSize = new Vector2(IndexPageWidth, 0f);
		book.AddChild(left);
		var leftColumn = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		leftColumn.AddThemeConstantOverride("separation", 8);
		left.AddChild(leftColumn);
		leftColumn.AddChild(BuildSearchField());
		leftColumn.AddChild(BuildIndexScroll());

		// The spine: the page's darkest ink between the two leaves.
		book.AddChild(new ColorRect { Name = "CodexSpine", Color = CodexRows.Vellum.Ink, CustomMinimumSize = new Vector2(6f, 0f), MouseFilter = Control.MouseFilterEnum.Ignore });

		PanelContainer right = BuildBookPage("CodexEntryPage");
		right.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		book.AddChild(right);
		right.AddChild(BuildPageScroll());
	}

	/// <summary>The phone's book: one page at a time, filling the plate.</summary>
	private void BuildPhoneBook()
	{
		VBoxContainer column = BuildPlate();
		column.AddThemeConstantOverride("separation", 4);
		column.AddChild(BuildTitleRow(withBack: true));
		column.AddChild(BuildBookmarks());

		// Both pages fill one holder exactly, so the turned page can never stop short of the index's edges.
		var holder = new Control { Name = "CodexPages", SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddChild(holder);

		PanelContainer index = BuildBookPage("CodexIndexPage");
		index.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		holder.AddChild(index);
		var indexColumn = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		indexColumn.AddThemeConstantOverride("separation", 8);
		index.AddChild(indexColumn);
		indexColumn.AddChild(BuildSearchField());
		indexColumn.AddChild(BuildIndexScroll());
		indexPage = index;

		PanelContainer entry = BuildBookPage("CodexEntryPage");
		entry.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		entry.Visible = false;
		holder.AddChild(entry);
		var entryColumn = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		entryColumn.AddThemeConstantOverride("separation", 8);
		entry.AddChild(entryColumn);
		var band = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		band.AddThemeConstantOverride("separation", 12);
		entryColumn.AddChild(band);
		indexButton = new Button { Name = "CodexIndexButton", Text = "‹ Index", FocusMode = Control.FocusModeEnum.All };
		ResponsiveLayout.SetFont(indexButton, ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(indexButton, BonelightSkin.Emphasis.Normal);
		ResponsiveLayout.EnsureTouchTarget(indexButton, 124f);
		indexButton.Pressed += ClosePage;
		band.AddChild(indexButton);
		entryTitle = new Label
		{
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		ResponsiveLayout.SetFont(entryTitle, ResponsiveLayout.TextRole.Title);
		entryTitle.AddThemeColorOverride("font_color", CodexRows.Vellum.Ink);
		band.AddChild(entryTitle);
		entryColumn.AddChild(new ColorRect { Color = CodexRows.Vellum.Edge, CustomMinimumSize = new Vector2(0, 2), MouseFilter = Control.MouseFilterEnum.Ignore });
		entryColumn.AddChild(BuildPageScroll());
		entryPage = entry;
	}

	/// <summary>The stone plate both books lie on, and the column inside it.</summary>
	private VBoxContainer BuildPlate()
	{
		var plate = new PanelContainer { Name = "CodexPanel", MouseFilter = Control.MouseFilterEnum.Ignore };
		plate.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		plate.OffsetLeft = 12f;
		plate.OffsetRight = -12f;
		plate.OffsetTop = 12f;
		plate.OffsetBottom = -12f;
		plate.AddThemeStyleboxOverride("panel", BonelightSkin.PanelStyle(BonelightSkin.Register.Stone));
		root.AddChild(plate);
		var column = new VBoxContainer { Name = "CodexColumn", MouseFilter = Control.MouseFilterEnum.Ignore };
		plate.AddChild(column);
		return column;
	}

	private Control BuildIndexScroll()
	{
		indexList = new VBoxContainer { Name = "CodexIndex", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		indexList.AddThemeConstantOverride("separation", 2);
		indexScroll = new ScrollContainer
		{
			Name = "CodexIndexScroll",
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			FollowFocus = true,
		};
		indexScroll.AddChild(CodexRows.WithGutter(indexList));
		return indexScroll;
	}

	private Control BuildPageScroll()
	{
		page = new VBoxContainer { Name = "CodexDetail", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		page.AddThemeConstantOverride("separation", 10);
		pageScroll = new ScrollContainer
		{
			Name = "CodexDetailScroll",
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			FollowFocus = true,
		};
		pageScroll.AddChild(CodexRows.WithGutter(page));
		return pageScroll;
	}

	/// <summary>A tier's nodes across the page: all in a row on a PC; on a phone two to a row past three (the cat's four).</summary>
	private int TierColumns(int count) => narrow && count > 3 ? 2 : Math.Max(1, count);

	// A control centred across the page at a share of its width.
	private static Control Centred(Control control, float share)
	{
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 0);
		Control Spacer() => new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsStretchRatio = (1f - share) / 2f, MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddChild(Spacer());
		control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		control.SizeFlagsStretchRatio = share;
		row.AddChild(control);
		row.AddChild(Spacer());
		return row;
	}

	/// <summary>
	/// A spell's family tree down the page: the base centred, then a tier for each milestone - its heading and
	/// shared requirements, its nodes across - with ink lines from the node each tier grows from (the one taken,
	/// else the whole row) into each node of the next. The way to the Apotheosis is drawn in the ember line while
	/// it is still open - and since 2026-11-02 every route is the way (ApotheosisCatalog.SpellMeetsGate names no
	/// path), so on the wizard's own spell that is every line into a node not closed by another choice.
	/// </summary>
	private Control ArrangeTree(CodexEntry entry, CodexForm form)
	{
		// The lines first, behind: a MarginContainer fits both children to its rect, and draws them in order.
		var holder = new MarginContainer { Name = "CodexTree", MouseFilter = Control.MouseFilterEnum.Ignore };
		var lines = new CodexTreeLines { Name = "CodexTreeLines" };
		holder.AddChild(lines);
		var tree = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		tree.AddThemeConstantOverride("separation", 6);
		holder.AddChild(tree);

		Control baseNode = BuildBaseNode(entry, form);
		tree.AddChild(Centred(baseNode, narrow ? 0.8f : 0.6f));
		treeLines.Add(new List<Control> { StopOf(baseNode) });
		Control source = baseNode;
		bool apotheosisOpen = form.Finals.Any(f => f.State != CodexNodeState.Closed);

		void Tier(string label, List<CodexChip> gates, List<CodexNode> nodes, bool finals)
		{
			if (nodes.Count == 0)
				return;
			tree.AddChild(TierHeading(label, gates));
			var grid = new GridContainer { Columns = finals ? 1 : TierColumns(nodes.Count), MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			grid.AddThemeConstantOverride("h_separation", 10);
			grid.AddThemeConstantOverride("v_separation", 10);
			List<Control> controls = nodes.Select(n => BuildNode(n)).ToList();
			foreach (Control control in controls)
			{
				control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				grid.AddChild(control);
			}
			tree.AddChild(finals ? Centred(grid, narrow ? 0.9f : 0.8f) : grid);

			for (int i = 0; i < nodes.Count; i++)
			{
				bool path = apotheosisOpen && nodes[i].State != CodexNodeState.Closed;
				bool live = IsLive(nodes[i]);
				Color ink = path ? CodexInk.EmberLine : live ? CodexRows.Vellum.Reward : CodexRows.Vellum.InkMeta;
				lines.AddEdge(source, controls[i], ink, dashed: !path && !live, layer: path ? 2 : live ? 1 : 0);
			}
			treeLines.Add(controls.Select(StopOf).ToList());

			int taken = nodes.FindIndex(n => n.State == CodexNodeState.Taken);
			source = taken >= 0 ? controls[taken] : grid;
		}

		Tier("Mutation · Lv 4", form.MutationGates, form.Mutations, finals: false);
		Tier("Ascension · Lv 8", form.AscensionGates, form.Ascensions, finals: false);
		Tier("Apotheosis", new List<CodexChip>(), form.Finals, finals: true);
		return holder;
	}

	/// <summary>
	/// A recipe down the page: its ingredients across (four on a PC, two to a row on a phone), the ink lines
	/// into its result centred under them - solid from an ingredient held or taken, dashed from the rest.
	/// </summary>
	private Control ArrangeRecipe(CodexRecipe recipe)
	{
		var holder = new MarginContainer { Name = $"CodexRecipe_{recipe?.Id}", MouseFilter = Control.MouseFilterEnum.Ignore };
		if (recipe == null)
			return holder;
		var lines = new CodexTreeLines { Name = "CodexTreeLines" };
		holder.AddChild(lines);
		var tree = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		tree.AddThemeConstantOverride("separation", 26);
		holder.AddChild(tree);

		var grid = new GridContainer
		{
			Columns = narrow ? Math.Min(2, Math.Max(1, recipe.Ingredients.Count)) : Math.Min(4, Math.Max(1, recipe.Ingredients.Count)),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		grid.AddThemeConstantOverride("h_separation", 10);
		grid.AddThemeConstantOverride("v_separation", 10);
		List<Control> parts = recipe.Ingredients.Select(n => BuildNode(n)).ToList();
		foreach (Control part in parts)
		{
			part.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			grid.AddChild(part);
		}
		tree.AddChild(grid);
		Control result = BuildNode(recipe.Result);
		tree.AddChild(Centred(result, narrow ? 0.8f : 0.45f));

		for (int i = 0; i < parts.Count; i++)
		{
			bool live = IsLive(recipe.Ingredients[i]);
			lines.AddEdge(parts[i], result, live ? CodexRows.Vellum.Reward : CodexRows.Vellum.InkMeta, dashed: !live, layer: live ? 1 : 0);
		}
		treeLines.Add(parts.Select(StopOf).ToList());
		treeLines.Add(new List<Control> { StopOf(result) });
		return holder;
	}
}
