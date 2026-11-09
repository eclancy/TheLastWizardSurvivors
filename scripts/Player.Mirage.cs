using Godot;
using TheOneHeMissed.scripts;

// FATA MORGANA's second casting origin (Morgan le Fay's Apotheosis of Sleet Runes, .ai/chapter-wizards.md 4.9).
//
// The fire loop casts every spell from the player. Under this form one spell is cast from two places: Sleet Runes
// lets each cast go from his hand and again from a mirage of him walking beside him the other way
// (FataMorganaMirage), which the rune node reads as its second origin (SleetRunes.Mirage). This file owns that
// origin's life: made the first cast after the form is taken, handed to the rune node every cast (so a node
// remade after the spell is lost and regained still has it), and freed with the spell. Nothing else in the fire
// loop changes - one timer, one cast, two hands.
//
// AVALON, her passive, is in TakeDamage's death step (step 10): the first extra life a run spends heals to full,
// not half. It is here beside her form so everything that is hers in Player reads in one place.
public partial class Player
{
	private FataMorganaMirage? fataMorgana;

	/// <summary>The mirage walking beside him while Fata Morgana is held, or null.</summary>
	public FataMorganaMirage? Mirage => fataMorgana != null && IsInstanceValid(fataMorgana) ? fataMorgana : null;

	// Called by FireSleetRunes before each cast: the form held, the mirage exists and the rune node knows it.
	private void KeepFataMorgana(SpellData spell, SleetRunes runes)
	{
		bool held = spell.HasEvolution(SleetRunes.FataMorganaId);
		if (!held)
		{
			ReleaseFataMorgana();
			runes.Mirage = null;
			return;
		}
		if (Mirage == null && GetParent() is Node2D arena)
		{
			fataMorgana = new FataMorganaMirage { Name = "FataMorgana", PlayerRef = this };
			arena.AddChild(fataMorgana);
			fataMorgana.GlobalPosition = fataMorgana.Place();
			GD.Print($"Fata Morgana: the mirage walks beside him from {Node2DGame.Current?.RunSeconds ?? 0f:0.0} s.");
		}
		runes.Mirage = Mirage;
	}

	private void ReleaseFataMorgana()
	{
		if (fataMorgana != null && IsInstanceValid(fataMorgana))
			fataMorgana.QueueFree();
		fataMorgana = null;
	}

	// --- Glamour ----------------------------------------------------------------------------------------

	// Glamour (Morgan le Fay, since 2026-11-09): one blow in ten strikes a mirage of her instead - the enchantress's
	// glamour, and her Fata Morgana in small. Added to the dodge roll in TakeDamage's step 2, so a blow it turns is
	// what any dodge is: no signal, no HP lost, the Blur's ghosts and the dodge's sound. It replaced Avalon (the first
	// extra life heals to full), which did nothing unless she happened to hold a revive (Phoenix Oath, the cat's Nine
	// Lives); Eric chose it over making Avalon a revive of her own, which would have blurred into the Archmage's
	// Overlooked (.ai/character-fit.md section 5).
	public const float CharacterGlamourEvasion = 0.10f;
	private float characterGlamourEvasion = 0f;

	// What an extra life gives back: half the bar. Called once per fall (TakeDamage step 10).
	private int ExtraLifeHeal()
	{
		int half = System.Math.Max(1, MaxHP / 2);
		if (GameStats.TrackHealing)
			GameStats.RecordHealing("Player.TakeDamage (an extra life)", half);
		return half;
	}
}
