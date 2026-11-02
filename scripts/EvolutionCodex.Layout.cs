using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

// THE CODEX'S ARRANGEMENT, one builder per canvas, kept apart from everything else so a layout can be
// swapped without touching the model, the search, the keyboard, the cells, the nodes or the input: those are
// the parts (EvolutionCodex.cs, EvolutionCodex.Detail.cs, CodexKeyboard.cs), and this file only says where
// they go (.ai/evolution-codex.md section 2, and the wireframe's four artboards).
//
//   PC      the heading band (title, search, count, Back), the filter tabs and the run line, then the grid on
//           the left beside the detail of the selection on the right; a tree in columns, a recipe stacked.
//   Phone   the band (title, count), the search field, the tabs, the grid filling the panel and Back at the
//           foot; the detail is a full sheet over the grid, its tree in rows and a recipe two abreast.
public partial class EvolutionCodex
{
	// The grid's width on a PC: six cells and their gaps, the scroll gutter, a little air.
	private float GridWidth() => 6f * CellWidth + 5f * CellGap + 24f;

	// A phone: five across inside the 720 canvas less the safe area, the panel margins and the gutter.
	private float CellWidth => narrow ? 108f : 80f;
	private float FrameSide => narrow ? 88f : 76f;
	private const float CellGap = 6f;

	/// <summary>The cells across: six on a PC, five on a phone.</summary>
	private int GridColumns => narrow ? 5 : 6;

	/// <summary>The PC's arrangement: band, tabs, then the grid beside the detail.</summary>
	private void BuildPcLayout()
	{
		var column = BuildPanel();
		column.AddChild(BuildHeader(withSearch: true, withBack: true));
		column.AddChild(BuildFilterRow());
		BuildGridScroll();
		var body = new HBoxContainer { Name = "CodexBody", SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		body.AddThemeConstantOverride("separation", 14);
		column.AddChild(body);
		gridScroll.CustomMinimumSize = new Vector2(GridWidth(), 0f);
		body.AddChild(gridScroll);
		body.AddChild(BuildDetailPanel());
	}

	/// <summary>The phone's arrangement: band, search, tabs, the grid, Back; the detail on a sheet over it.</summary>
	private void BuildPhoneLayout()
	{
		var column = BuildPanel();
		column.AddChild(BuildHeader(withSearch: false, withBack: false));
		column.AddChild(BuildSearchField(expand: true));
		column.AddChild(BuildFilterRow());
		BuildGridScroll();
		gridScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		column.AddChild(gridScroll);
		column.AddChild(BuildPhoneFoot());
		root.AddChild(BuildSheet());
	}

	/// <summary>The PC's tree: four columns, left to right, an arrow bus between each.</summary>
	private Control BuildTreeColumns(CodexEntry entry, CodexForm form)
	{
		var row = new HBoxContainer { Name = "CodexTree", MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 0);

		Control baseNode = BuildBaseNode(entry, form, wide: false);
		row.AddChild(Column("Base", null, new List<Control> { baseNode }, fixedWidth: 56f));
		treeLines.Add(new List<Control> { StopOf(baseNode) });

		void AddColumn(string label, List<CodexChip> gates, List<CodexNode> nodes)
		{
			if (nodes.Count == 0)
				return;
			var controls = nodes.Select(n => BuildNode(n, wide: false)).ToList();
			var arrows = new AscensionForkArrows
			{
				Name = $"Arrows{label.Replace(" ", "")}",
				Shape = AscensionForkArrows.Layout.Spine,
				CustomMinimumSize = new Vector2(22f, 0f),
			};
			for (int i = 0; i < nodes.Count; i++)
				arrows.AddTarget(controls[i], IsLive(nodes[i]));
			row.AddChild(arrows);
			row.AddChild(Column(label, gates, controls, fixedWidth: 0f));
			treeLines.Add(controls.Select(StopOf).ToList());
		}

		AddColumn("Level 4", form.MutationGates, form.Mutations);
		AddColumn("Level 8", form.AscensionGates, form.Ascensions);
		AddColumn("Apotheosis", new List<CodexChip>(), form.Finals);
		return row;
	}

	/// <summary>The phone's tree: rows, top to bottom, a fork down into each.</summary>
	private Control BuildTreeRows(CodexEntry entry, CodexForm form)
	{
		var tree = new VBoxContainer { Name = "CodexTree", MouseFilter = Control.MouseFilterEnum.Ignore };
		tree.AddThemeConstantOverride("separation", 4);
		Control baseNode = BuildBaseNode(entry, form, wide: true);
		tree.AddChild(baseNode);
		treeLines.Add(new List<Control> { StopOf(baseNode) });
		Control source = baseNode;

		void AddRow(string label, List<CodexChip> gates, List<CodexNode> nodes)
		{
			if (nodes.Count == 0)
				return;
			var controls = nodes.Select(n => BuildNode(n, wide: true)).ToList();
			var arrows = new AscensionForkArrows
			{
				Name = $"Arrows{label.Replace(" ", "")}",
				Shape = AscensionForkArrows.Layout.Fork,
				Source = source,
				CustomMinimumSize = new Vector2(0f, 34f),
			};
			for (int i = 0; i < nodes.Count; i++)
				arrows.AddTarget(controls[i], IsLive(nodes[i]));
			tree.AddChild(ColumnHeader(label, gates));
			tree.AddChild(arrows);
			var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			row.AddThemeConstantOverride("separation", 10);
			foreach (Control control in controls)
			{
				control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				row.AddChild(control);
			}
			tree.AddChild(row);
			treeLines.Add(controls.Select(StopOf).ToList());
			source = row;
		}

		AddRow("Level 4", form.MutationGates, form.Mutations);
		AddRow("Level 8", form.AscensionGates, form.Ascensions);
		AddRow("Apotheosis", new List<CodexChip>(), form.Finals);
		return tree;
	}


	/// <summary>A PC's recipe: the ingredients stacked on the left, "+" between, an arrow bus into the result.</summary>
	private void BuildRecipePc(CodexRecipe recipe)
	{
		var ingredients = recipe.Ingredients.Select(n => BuildNode(n, wide: false)).ToList();
		Control result = BuildNode(recipe.Result, wide: false);
		bool open = recipe.Made || recipe.Offered;

		var row = new HBoxContainer { Name = $"CodexRecipe_{recipe.Id}", MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 6);
		var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.6f, MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 6);
		for (int i = 0; i < ingredients.Count; i++)
		{
			if (i > 0 && recipe.Kind == CodexKind.Combination)
			{
				Label plus = CodexRows.MakeText("+", ResponsiveLayout.TextRole.Title, CodexRows.Stone.Heading);
				plus.HorizontalAlignment = HorizontalAlignment.Center;
				column.AddChild(plus);
			}
			column.AddChild(ingredients[i]);
		}
		row.AddChild(column);
		var arrows = new AscensionForkArrows
		{
			Shape = AscensionForkArrows.Layout.Spine,
			CustomMinimumSize = new Vector2(30f, 0f),
		};
		arrows.AddTarget(result, open);
		row.AddChild(arrows);
		var resultColumn = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
		resultColumn.AddChild(result);
		row.AddChild(resultColumn);
		detail.AddChild(row);
		treeLines.Add(ingredients.Select(StopOf).ToList());
		treeLines.Add(new List<Control> { StopOf(result) });
	}

	/// <summary>A phone's recipe: the ingredients two abreast, a fork down into the result.</summary>
	private void BuildRecipePhone(CodexRecipe recipe)
	{
		var ingredients = recipe.Ingredients.Select(n => BuildNode(n, wide: true)).ToList();
		Control result = BuildNode(recipe.Result, wide: true);
		bool open = recipe.Made || recipe.Offered;

		var tree = new VBoxContainer { Name = $"CodexRecipe_{recipe.Id}", MouseFilter = Control.MouseFilterEnum.Ignore };
		tree.AddThemeConstantOverride("separation", 4);
		var parts = new GridContainer { Columns = Math.Min(2, Math.Max(1, ingredients.Count)), MouseFilter = Control.MouseFilterEnum.Ignore };
		parts.AddThemeConstantOverride("h_separation", 10);
		parts.AddThemeConstantOverride("v_separation", 10);
		foreach (Control part in ingredients)
		{
			part.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			parts.AddChild(part);
		}
		tree.AddChild(parts);
		var arrows = new AscensionForkArrows
		{
			Shape = AscensionForkArrows.Layout.Fork,
			Source = parts,
			CustomMinimumSize = new Vector2(0f, 34f),
		};
		arrows.AddTarget(result, open);
		tree.AddChild(arrows);
		var centre = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		centre.AddChild(Spacer());
		result.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		result.SizeFlagsStretchRatio = 2f;
		centre.AddChild(result);
		centre.AddChild(Spacer());
		tree.AddChild(centre);
		detail.AddChild(tree);
		treeLines.Add(ingredients.Select(StopOf).ToList());
		treeLines.Add(new List<Control> { StopOf(result) });
	}
}
