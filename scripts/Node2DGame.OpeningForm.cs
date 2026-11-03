using Godot;
using TheOneHeMissed.scripts;

// THE RUN'S FIRST SCREEN, for a wizard who opens on a spell with forms (Ma Liang on the Familiar,
// .ai/chapter-wizards.md 4.8; Player.MaLiang.cs).
//
// A spell with forms is chosen in a form on a page of its own, on the level-up screen, in the run
// (.ai/decisions/2026-10-28-spell-variants.md), and a starting spell used to get its first form without a word. Ma Liang
// is the first wizard who opens on one, and which creature he paints is the whole of his run: only the crow has a final
// form (The Painted Door). So his spell is owed (Player.OwesOpeningForm) and this opens the page before anything moves -
// the ordinary page of forms on the ordinary level-up screen (LevelUpMenu.ShowOpeningForms), with no Back, the tree
// paused under it. Answered, the spell is equipped in that form at level 1 and the run starts as any does, with the
// menu's grace.
//
// It goes ahead of every other screen in the queue (ResumeOrOpenNextMenu), so a chapter that opens screens of its own at
// the start (the Throne's Muster) deals its cards to a wizard who already holds his spell.
public partial class Node2DGame
{
	/// <summary>True while the opening page is up. For the tools.</summary>
	public bool OpeningFormPageOpen { get; private set; }

	// Asked once, deferred from _Ready (so the run is built), and again by ResumeOrOpenNextMenu.
	private bool OpenOpeningFormIfOwed()
	{
		if (runFinished || player == null || !IsInstanceValid(player) || !player.OwesOpeningForm
			|| IsLevelUpMenuOpen || IsChestMenuOpen || levelupMenuScene == null)
			return false;
		LevelUpOption option = player.BuildOpeningFormOffer();
		if (option == null || !option.OffersVariants)
		{
			GD.PushError($"Node2DGame: '{player.OpeningFormSpellId}' is owed a form but offers none; the run starts without it.");
			return false;
		}
		levelUpMenu = levelupMenuScene.Instantiate<CanvasLayer>();
		levelUpMenu.ProcessMode = ProcessModeEnum.WhenPaused;
		AddChild(levelUpMenu);
		levelUpMenu.Connect("WeaponSelected", new Callable(this, nameof(OnOpeningFormChosen)));
		levelUpMenu.Show();
		if (levelUpMenu is LevelUpMenu menu)
			menu.ShowOpeningForms(option);
		OpeningFormPageOpen = true;
		GetTree().Paused = true;
		GD.Print($"Node2DGame: the opening page of '{option.SpellId}'s forms is up.");
		return true;
	}

	private void OnOpeningFormChosen(string selection)
	{
		if (player == null || !IsInstanceValid(player))
			return;
		if (!player.TakeOpeningForm(selection))
		{
			// Refused (a selection that is not one of its forms): the page goes back up rather than the run starting
			// with no spell.
			if (levelUpMenu is LevelUpMenu menu && player.BuildOpeningFormOffer() is LevelUpOption again)
			{
				menu.Show();
				menu.ShowOpeningForms(again);
			}
			return;
		}
		OpeningFormPageOpen = false;
		RefreshElementHud();
		CloseLevelUpMenu();
	}
}
