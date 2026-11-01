using Godot;

/// <summary>
/// Dims every card of a row but the one the selection is on, while a stick or the keys are what the
/// player is using - so on the level-up screen the card that A will take is the bright one, at a
/// glance, on every family.
/// </summary>
/// <remarks>
/// From the player (2026-10-31): "make it easier to see what i have highlighted when using keyboard
/// or controller". <see cref="MenuFocusHighlight"/>'s ring and corner brackets mark the card; this
/// takes the others down a step, which is the cue that works when the eye has not yet found the ring.
///
/// A node beside the cards, not a change to them: it polls the focus owner once a frame (a handful of
/// children, no allocation) and writes Modulate only when the lit card changes, so it never fights
/// whatever rebuilt the row. It keeps the three rules in .ai/controller-and-focus.md:
/// <list type="bullet">
/// <item>The mouse drops it. While <see cref="MenuFocusHighlight.PointerActive"/> every card is at full
/// light: the pointer shows where it is, and a mouse user never sees two cards dimmed by a focus they
/// are not using.</item>
/// <item>It moves nothing, so it cannot scroll a list or move focus.</item>
/// <item>It reads focus, it never takes it: a screen stacked over this one holds the selection, the
/// owner is no longer inside this row, and every card goes back to full light.</item>
/// </list>
///
/// HOW DIM. Modulate multiplies the ink and its ground alike, so a dimmed card loses contrast as well
/// as light. <see cref="Dimmed"/> is the most it can take with every family's weakest ink still above
/// its floor - measured by tools/art/ui_contrast.py ("spotlight-dimmed" rows; the weakest is the obsidian
/// plate's element line, about 5:1). The Apotheosis page has none: its Fire-mid line would fall to 4.4:1,
/// and it holds one form. The selected card is never
/// touched, so selecting a card still never changes how readable it is (.ai/level-up-screen.md 2).
/// </remarks>
public partial class FocusSpotlight : Node
{
	public const string NodeName = "FocusSpotlight";

	/// <summary>An unselected card. COPIED into tools/art/ui_contrast.py (SPOTLIGHT_DIM).</summary>
	public static readonly Color Dimmed = new Color(0.78f, 0.78f, 0.82f);

	private Control lit;
	private bool anyDimmed;

	/// <summary>Puts a spotlight on the cards that are <paramref name="row"/>'s children. Safe to call twice.</summary>
	public static void Attach(Control row)
	{
		if (row == null || !GodotObject.IsInstanceValid(row) || row.GetNodeOrNull<FocusSpotlight>(NodeName) != null)
			return;
		// A plain Node: a Container lays out only Control children, so this takes no room in the row.
		row.AddChild(new FocusSpotlight { Name = NodeName, ProcessMode = ProcessModeEnum.Always });
	}

	public override void _Process(double delta)
	{
		if (GetParent() is not Control row)
			return;

		Control target = null;
		if (!MenuFocusHighlight.PointerActive && row.IsVisibleInTree()
			&& row.GetViewport()?.GuiGetFocusOwner() is Control focus && row.IsAncestorOf(focus))
		{
			Node card = focus;
			while (card != null && card.GetParent() != row)
				card = card.GetParent();
			target = card as Control;
		}

		if (target == lit && (target != null || !anyDimmed))
			return;
		lit = target;
		anyDimmed = false;
		foreach (Node child in row.GetChildren())
		{
			if (child is not Control card)
				continue;
			// Only a card is dimmed: something in the row with nothing to choose (the fork's arrows, a
			// label) keeps its light.
			bool dim = target != null && card != target && IsChoice(card);
			card.Modulate = dim ? Dimmed : Colors.White;
			anyDimmed |= dim;
		}
	}

	private static bool IsChoice(Node node)
	{
		if (node is BaseButton button && button.FocusMode != Control.FocusModeEnum.None)
			return true;
		foreach (Node child in node.GetChildren())
		{
			if (IsChoice(child))
				return true;
		}
		return false;
	}
}
