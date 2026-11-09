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

	// RebuildCard's last word: shown, and what it names, for this stop and the wizard chosen.
	private void RefreshForetell(Stop stop, bool open)
	{
		if (foretellButton == null)
			return;
		CharacterData who = CharacterRoster.GetByIndex(Global.SelectedCharacterIdx);
		bool can = open && stop != null && !stop.IsBossRush && ConjunctionLadder.MayForetellOn(stop.Stage, bossRush: false)
			&& ConjunctionLadder.ForetellCandidates(save, who).Count > 0;
		foretellButton.Visible = can;
		if (!can)
			return;
		ComboDefinition foretold = ConjunctionLadder.Foretold(save, who, stop.Stage, bossRush: false);
		foretellButton.Text = foretold == null
			? "✶ Nostradamus foretells nothing"
			: $"✶ Foretold: {ConjunctionLadder.ShownName(foretold, save)}";
	}

	private void CycleForetold()
	{
		if (selected < 0 || selected >= stops.Count)
			return;
		ConjunctionLadder.CycleForetold(save, CharacterRoster.GetByIndex(Global.SelectedCharacterIdx), stops[selected].Stage);
		RefreshForetell(stops[selected], stops[selected].Look == WorldMapView.StopLook.Open);
	}
}
