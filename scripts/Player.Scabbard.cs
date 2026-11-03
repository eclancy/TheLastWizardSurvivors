using Godot;
using System;

// THE SCABBARD (Nimue's passive, `bite_interval_25`; .ai/endgame-castle.md decision 11, its Built note). The Lady of
// the Lake gave the king the sword and its scabbard, and Merlin told him the scabbard was worth ten of the sword:
// "whiles ye have the scabbard upon you, ye shall lose no blood, be ye never so sore wounded" (Malory, I.25). So the
// crowd pressed against her bleeds her less often: every touching body's bite comes a quarter later than it would
// (Player's contact clock, DamageCooldownSeconds 0.2 s, is 0.25 s on her), a fifth fewer bites a second.
//
// WHY THIS AXIS. Every other wizard's defence is a share of every hit (the Geomancer's armour), a chance to take none
// (the boons' and Darkness's evasion, Laelaps's catch), one death refused (the Archmage), a revive made whole (Morgan)
// or a shove (Merlin). None touches the RATE at which a body in contact wounds - and that is where this game's damage
// is: a first build capped the SIZE of one blow at a tenth of her health and, measured (the survivor bot in the
// Forest, the Last Bridge and the Throne), it never once bound - 2,000-odd HP of hits in 1s, 2s and 3s, nothing over
// five. The swarm's touch is the danger, so the Scabbard answers the touch. It does nothing to a slam, a bolt or a
// boss's ring; it gives no health back (.ai/difficulty-curve.md's budget is untouched).
//
// WHERE. Both contact sites (the loop and the first touch on entering) read ContactBiteSeconds. The hit itself goes
// through TakeDamage unchanged - dodge, the signal, armour, the shield - so every reactive boon still answers it.
public partial class Player
{
	/// <summary>How much longer a touching body waits between bites under the Scabbard: a quarter.</summary>
	public const float CharacterScabbardBiteScale = 1.25f;

	private bool characterScabbard = false;

	/// <summary>The seconds between one touching body's bites on this wizard: 0.2, or 0.25 under the Scabbard.</summary>
	public float ContactBiteSeconds => DamageCooldownSeconds * (characterScabbard ? CharacterScabbardBiteScale : 1f);

	/// <summary>Contact bites that landed this run, for the balance harness.</summary>
	public int ContactBitesTaken { get; private set; }

	private void CountContactBite() => ContactBitesTaken++;
}
