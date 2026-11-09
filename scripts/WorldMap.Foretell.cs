using Godot;
using TheOneHeMissed.scripts;

// NOSTRADAMUS FORETELLS (rung IV of .ai/conjunction-progression.md; ConjunctionLadder.Foretold). The world map is the
// stage select, so the foretelling is named on its card, before the run, as the far sky's recipe is: one quiet button
// over Enter - "Foretold: Void Lance + Contagion" - that turns to the next recipe that may be foretold for this wizard,
// then to nothing, then round again. Only once the rung is earned (Nostradamus freed, Conjunctions learned), only on an
// open campaign chapter (never the far shore, where the sky's night wins, nor the Boss Rush), only when there is
// something to foretell. The choice is this session's (ConjunctionLadder.ForetoldChoice); until one is made the pin
// stands in for it. The recipe is named once made, by its two parents before (ConjunctionLadder.ShownName).
public partial class WorldMap
{
	private Button foretellButton;

	private Button BuildForetellButton()
	{
		foretellButton = new Button
		{
			Name = "ForetellButton",
			Visible = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Alignment = HorizontalAlignment.Left,
			ClipText = true,
			TooltipText = "Nostradamus foretells a Conjunction for this run: its two spells come to your hand first.",
		};
		ResponsiveLayout.SetFont(foretellButton, ResponsiveLayout.TextRole.Label);
		BonelightSkin.StyleButton(foretellButton, BonelightSkin.Emphasis.Normal);
		ResponsiveLayout.EnsureTouchTarget(foretellButton, 0f);
		foretellButton.Pressed += CycleForetold;
		return foretellButton;
	}

	// RebuildCard's last word: shown, and what it names, for this stop and the wizard chosen. Once the rung is earned the
	// button keeps its place on every stop - greyed where nothing may be foretold (a locked chapter, the far shore, the
	// Boss Rush) - so the card, and on a phone the map above it, never change size as the player walks the road.
	private void RefreshForetell(Stop stop, bool open)
	{
		if (foretellButton == null)
			return;
		CharacterData who = CharacterRoster.GetByIndex(Global.SelectedCharacterIdx);
		bool earned = ConjunctionLadder.ForetellCandidates(save, who).Count > 0;
		bool can = earned && open && stop != null && !stop.IsBossRush && ConjunctionLadder.MayForetellOn(stop.Stage, bossRush: false);
		foretellButton.Visible = earned;
		foretellButton.Disabled = !can;
		foretellButton.FocusMode = can ? FocusModeEnum.All : FocusModeEnum.None;
		MakeRoomForForetell(earned);
		if (!earned)
			return;
		ComboDefinition foretold = can ? ConjunctionLadder.Foretold(save, who, stop.Stage, bossRush: false) : null;
		foretellButton.Text = !can ? "✶ Nostradamus foretells on the road"
			: foretold == null ? "✶ Nostradamus foretells nothing"
			: $"✶ Foretold: {ConjunctionLadder.ShownName(foretold, save)}";
	}

	private bool foretellRoomMade;

	// On a phone the card has a fixed height (NarrowCardHeight) and the map stops above it: the button's row is taken
	// from the map, once, when the rung is first on show, so Enter is never pushed off the foot of the screen.
	private void MakeRoomForForetell(bool earned)
	{
		if (!narrow || !earned || foretellRoomMade || GetNodeOrNull<Control>("Card") is not Control card)
			return;
		foretellRoomMade = true;
		float row = foretellButton.GetCombinedMinimumSize().Y + 10f;
		card.OffsetTop -= row;
		if (view != null)
			view.OffsetBottom -= row;
	}

	private void CycleForetold()
	{
		if (selected < 0 || selected >= stops.Count)
			return;
		ConjunctionLadder.CycleForetold(save, CharacterRoster.GetByIndex(Global.SelectedCharacterIdx), stops[selected].Stage);
		RefreshForetell(stops[selected], stops[selected].Look == WorldMapView.StopLook.Open);
	}
}
