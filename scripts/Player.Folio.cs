using Godot;
using System;
using TheOneHeMissed.scripts;

// FOLIO, John Dee's opener (.ai/character-fit.md section 6; scripts/Folio.cs). Everything the wizard owns of it is here,
// so the fire loop and the orb pickup each say one line.
//
// TWO WAYS A PAGE TURNS, ONE BOOK. The fire loop's cooldown turns the book (FireFolio - the ONE spellFireTimers loop, no
// timer of Folio's own), and every XP orb he picks up turns one more page at once (NotifyOrbGathered, called by
// XPOrb.OnBodyEntered after it has paid its XP). Both put pages into the same node, which lets them go one at a time
// while something is in reach. A full book with nobody near returns NoTarget, so the timed turn stays ripe rather than
// being spent on nothing.
//
// WHEN IT MAY NOT TURN. Whatever silences the fire loop silences the pickup turn too (FolioMayCast): a boss arriving
// (HeldForBossEntrance), the dark wizard's seal, the Sovereign Rite's silence, death. Pages already in flight fly on - what
// was thrown before the hand landed was thrown (Player.Seal.cs) - but nothing new leaves the book.
public partial class Player
{
	private Folio? folio;

	// The cast: the turn's pages - the spell's count plus the player's Amount - into the book. Maintained, because the
	// node plays a page's own sound as each one leaves (the element cast cue would play for a turn whose pages wait).
	private CastOutcome FireFolio(SpellData spell, PackedScene scene)
	{
		Folio? book = EnsureFolio(spell, scene, true);
		if (book == null)
			return CastOutcome.NoTarget;
		int pages = Math.Max(1, spell.GetProjectileCountAtLevel(spell.CurrentLevel) + AmountFor(spell));
		return book.TurnByTime(pages) ? CastOutcome.Maintained : CastOutcome.NoTarget;
	}

	// The node, made the first time the book turns (by the cooldown or by an orb). Its numbers are read again on every
	// timed turn, as Sleet Runes' are on every cast; an orb's turn takes the last ones, unless it is the one that made it.
	private Folio? EnsureFolio(SpellData spell, PackedScene scene, bool refresh)
	{
		if (scene == null || spell == null)
			return null;
		bool made = false;
		if (folio == null || !IsInstanceValid(folio))
		{
			if (GetParent() is not Node2D arena)
				return null;
			folio = scene.Instantiate<Folio>();
			folio.PlayerRef = this;
			arena.AddChild(TagSpell(folio));
			made = true;
		}
		if (refresh || made)
			folio.Refresh(spell, GetSpellDamageMultiplier(spell), GetSpellVisualTint(spell), GetEffectiveAreaMultiplier());
		return folio;
	}

	/// <summary>
	/// An XP orb reached him (XPOrb.OnBodyEntered): if he holds Folio, its book turns one page at once. The Spirit Mirror's
	/// orbs come this way too, which is how "every orb it brings turns a page" holds without a second path.
	/// </summary>
	public void NotifyOrbGathered()
	{
		SpellData? spell = HeldFolio();
		if (spell == null || !FolioMayCast(spell))
			return;
		EnsureFolio(spell, FolioScene, false)?.TurnByOrb();
	}

	/// <summary>Whether Folio may put a page into the world now: what silences the fire loop silences the book.</summary>
	public bool FolioMayCast(SpellData spell) =>
		spell != null && !IsDead && !HeldForBossEntrance && !IsSilenced(spell) && !IsSealed(spell);

	private SpellData? HeldFolio()
	{
		foreach (SpellData spell in equippedSpells)
		{
			if (spell != null && spell.Id.Equals(Folio.SpellId, StringComparison.OrdinalIgnoreCase))
				return spell;
		}
		return null;
	}

	// The book goes with the spell (RemovePersistentSpellInstance: the Penknife, a fusion, the Rite), pages and all.
	private void ReleaseFolio(SpellData spell)
	{
		if (spell == null || !spell.Id.Equals(Folio.SpellId, StringComparison.OrdinalIgnoreCase) || folio == null)
			return;
		if (IsInstanceValid(folio))
			folio.QueueFree();
		folio = null;
	}
}
