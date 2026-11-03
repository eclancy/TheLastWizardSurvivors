using System;
using TheOneHeMissed.scripts;

// THE HUNGRY HEDGE (Baba Yaga's Apotheosis, the verb FEED; .ai/chapter-wizards.md 4.1). Every enemy the vine kills
// feeds it: it sweeps further and wider for the rest of the run. A kill counts when a hit whose source is Thorned
// Vine in this form takes the last of an enemy's health (DealDamageToEnemy) - the lash, the Thicket's brambles and the
// Barbed Lash's thorns all hand over the spell, so all of them feed it; its poison ticks are the enemy's own and do not.
//
// What it grows is the run's, not a vine's: each cast reads HungryHedgeGrowth when it is launched (ThornVine.Feed) and
// lengthens and widens its stroke by it, so a vine cast now is as big as everything it has eaten. Growth is capped
// (HedgeFeedToFull kills to full) and never shrinks: "Feed it early. It remembers."
public partial class Player
{
	/// <summary>
	/// Kills to full growth. 60, not the first guess of 400: the vine's own last hits are a small share of a run's kills
	/// (one spell of six, and its poison's are not counted), and the seek bot's ate 73 in the five minutes after taking the
	/// form - at 400 it ended the run 18% grown. At 60 a vine taken at ten minutes is full by about fourteen.
	/// </summary>
	public const int HedgeFeedToFull = 60;

	private int hedgeFed;

	/// <summary>Enemies the Hungry Hedge has eaten this run. For the harness.</summary>
	public int HungryHedgeFed => hedgeFed;

	/// <summary>0 to 1: how far the Hungry Hedge has grown toward its cap this run.</summary>
	public float HungryHedgeGrowth => Math.Min(1f, hedgeFed / (float)HedgeFeedToFull);

	// A kill landed: if the hit was the vine's, in its final form, it is fed.
	private void FeedHungryHedge(SpellData source)
	{
		if (source == null || !source.IsApotheosis
			|| !string.Equals(source.SelectedApotheosisId, ThornVine.HungryHedgeId, StringComparison.OrdinalIgnoreCase))
			return;
		hedgeFed++;
	}
}
