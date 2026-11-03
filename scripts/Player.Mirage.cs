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

	// --- Avalon -----------------------------------------------------------------------------------------

	// Avalon (Morgan le Fay): the first extra life a run spends heals to full, not half - the isle she carried the
	// dying king to, to be healed. Read in TakeDamage's step 10.
	private bool characterAvalon = false;
	private bool avalonSpent = false;

	/// <summary>Run seconds at which Avalon turned a fall into a full heal, or -1 while it has not. For the harness.</summary>
	public float AvalonAtSeconds { get; private set; } = -1f;

	// What an extra life gives back: half the bar, or the whole of it for the first fall of a run under Avalon.
	// Spent here, so call it once per fall.
	private int ExtraLifeHeal()
	{
		int half = System.Math.Max(1, MaxHP / 2);
		if (!characterAvalon || avalonSpent)
		{
			if (GameStats.TrackHealing)
				GameStats.RecordHealing("Player.TakeDamage (an extra life)", half);
			return half;
		}
		avalonSpent = true;
		AvalonAtSeconds = Node2DGame.Current?.RunSeconds ?? 0f;
		// The healing ledger names the extra half on its own line, so a report shows what Avalon gave and when.
		if (GameStats.TrackHealing)
		{
			GameStats.RecordHealing("Player.TakeDamage (an extra life)", half);
			GameStats.RecordHealing("Avalon (the first fall heals to full)", MaxHP - half);
		}
		GD.Print($"Avalon: the first fall healed to full ({MaxHP} HP) at {AvalonAtSeconds:0.0} s.");
		return MaxHP;
	}
}
