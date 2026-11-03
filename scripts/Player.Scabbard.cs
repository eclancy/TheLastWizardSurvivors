using Godot;
using System;

// THE SCABBARD (Nimue's passive, `hit_cap_10`; .ai/endgame-castle.md decision 11, its Built note). The Lady of the
// Lake gave the king the sword and its scabbard, and Merlin told him the scabbard was worth ten of the sword: "whiles
// ye have the scabbard upon you, ye shall lose no blood, be ye never so sore wounded" (Malory, I.25). So no one blow
// takes more than a tenth of her health.
//
// WHY THIS AXIS. Every other wizard's defence is a share of every hit (the Geomancer's armour), a chance to take none
// (the boons' and Darkness's evasion, Laelaps's catch), one death refused (the Archmage), a revive made whole (Morgan),
// or a shove (Merlin). None of them touches the SIZE of a single blow, and in this game that is its own danger: a
// swarm's touch is 1 or 2, but a slam, an exploder, a boss's ring or the dark wizard's echoes land 5 to 9 at the
// castle's x1.35, a fifth of a 50 HP bar in one beat. The Scabbard leaves the chip of the crowd exactly as it is and
// takes the spike out of the big telegraphed hits - so she can stand under a blow she misread, never in a crowd.
// Healing stays where .ai/difficulty-curve.md put it: nothing here gives health back.
//
// WHERE IN THE PIPELINE (Player.TakeDamage): after armour and the shield, on what would really land, and before
// Overlooked's step and before any HP leaves. The DamageTaken signal has already gone out with the raw amount, so
// every reactive boon still answers being struck, as it does for armour.
public partial class Player
{
	/// <summary>The most of her max HP one hit may take under the Scabbard: a tenth.</summary>
	public const float CharacterScabbardShare = 0.10f;

	private bool characterScabbard = false;

	/// <summary>Hits the Scabbard has capped this run, and the HP it kept. For the balance harness.</summary>
	public int ScabbardHitsCapped { get; private set; }
	public int ScabbardHpKept { get; private set; }

	/// <summary>The largest single hit the Scabbard lets through at the current max HP; never under 1.</summary>
	public int ScabbardCap => Math.Max(1, Mathf.RoundToInt(MaxHP * CharacterScabbardShare));

	// The cap, applied: what lands of a hit that has been through armour and the shield.
	private int ApplyScabbard(int mitigated)
	{
		if (!characterScabbard || mitigated <= 0)
			return mitigated;
		int cap = ScabbardCap;
		if (mitigated <= cap)
			return mitigated;
		ScabbardHitsCapped++;
		ScabbardHpKept += mitigated - cap;
		return cap;
	}
}
