using Godot;

// LEVELS A CHAPTER GRANTS OUTRIGHT (piece C4, the Throne's Muster, .ai/endgame-castle.md 6.2 and 11.5).
//
// The seven ChapterRules hooks let a chapter ANSWER the run. The Muster needs to TELL it something: "the
// player has eighteen levels, now". That is machinery any chapter could use, so it lives here, in the run,
// and the Throne's numbers stay in ThroneProcession.cs. Nothing here names a chapter.
//
// Granted levels go through the queue every level-up already uses (ResumeOrOpenNextMenu): one screen at a
// time, each the ordinary screen - rerolls, bans, the wizard's own spell, the Ascension strip - and the
// level is gained as its screen opens, not all at once, so the strip arrives at level 17 exactly as it does
// in a run (Player.AscensionMinimumPlayerLevel). A level the player earned meanwhile goes first. Hook 8
// (ChapterRules.LevelUpOpening) tells the rules which screens are granted ones, so the Muster can say so
// on its heading.
public partial class Node2DGame
{
	private int grantedLevelUps;
	// True only while a granted level-up's screen is being built, for hook 8.
	private bool openingGrantedLevelUp;

	/// <summary>Granted levels still waiting for their screen.</summary>
	public int GrantedLevelUpsWaiting => grantedLevelUps;

	/// <summary>
	/// Grants <paramref name="count"/> levels, each through the ordinary level-up screen, one after another
	/// (the Throne's Muster). The first opens now unless a screen is already up; the rest wait their turn
	/// in the menu queue, after any level the player earned.
	/// </summary>
	public void GrantLevelUps(int count)
	{
		if (count <= 0 || runFinished || player == null || !IsInstanceValid(player))
			return;
		grantedLevelUps += count;
		if (!IsLevelUpMenuOpen && !IsChestMenuOpen)
			OpenNextGrantedLevelUp();
	}

	// One granted level: the player gains it and its screen opens (Player.LevelUpImmediately emits
	// LevelGained, which is OnPlayerLevelGained). False when none is waiting.
	private bool OpenNextGrantedLevelUp()
	{
		if (grantedLevelUps <= 0 || player == null || !IsInstanceValid(player))
			return false;
		grantedLevelUps--;
		openingGrantedLevelUp = true;
		try
		{
			player.LevelUpImmediately();
		}
		finally
		{
			openingGrantedLevelUp = false;
		}
		return true;
	}
}
