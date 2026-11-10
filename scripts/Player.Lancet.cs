using Godot;
using System;
using TheOneHeMissed.scripts;

// LANCET (scripts/Lancet.cs): everything the wizard owns of it is here, so the fire loop says one line.
//
// THE CAST AIMS BY HEALTH. Every cooldown of the ONE spellFireTimers loop throws the cast's needles - the spell's count plus
// the player's Amount - at the most wounded bodies in reach, one each (Lancet.Cast walks the swarm once to choose them).
// With nothing in reach it answers NoTarget, so the timer stays ripe and the needles leave the moment something comes
// close, as every aimed spell's do. A throw is Fired: the Poison cast cue (SfxCatalog.DominantElement breaks the Metal-Poison
// tie by enum order; Metal's once Bodkin deepens it) is the needles leaving, and every strike sounds through
// Player.DealDamageToEnemy's impact, throttled there. No new sound (.ai/audio-direction.md section 5). Its numbers are Metal's
// ink all the same (SpellData.GetNumberElement: the first listed of a tie), one of the duals that sound one element and
// number in the other (.ai/art-direction.md, "Damage numbers wear the element").
//
// Needles already thrown fly on and strike whatever silences the fire loop afterwards (a boss arriving, the dark wizard's
// seal): what left the hand has left it, as Corposant's balls and Folio's pages do.
public partial class Player
{
	private Lancet? lancet;

	private CastOutcome FireLancet(SpellData spell, PackedScene scene)
	{
		if (scene == null || spell == null)
			return CastOutcome.NoTarget;
		if (lancet == null || !IsInstanceValid(lancet))
		{
			if (GetParent() is not Node2D arena)
				return CastOutcome.NoTarget;
			lancet = scene.Instantiate<Lancet>();
			lancet.PlayerRef = this;
			arena.AddChild(TagSpell(lancet));
		}
		int needles = Math.Max(1, spell.GetProjectileCountAtLevel(spell.CurrentLevel) + AmountFor(spell));
		// It draws itself in steel and poison, so it takes only a Legendary's gold from the visual tint, never a node scale.
		lancet.Refresh(spell, GetSpellDamageMultiplier(spell), GetSpellVisualTint(spell), spell.GetReachAtLevel(spell.CurrentLevel));
		return lancet.Cast(needles) > 0 ? CastOutcome.Fired : CastOutcome.NoTarget;
	}

	// The needles go with the spell (RemovePersistentSpellInstance: the Penknife, a fusion), every one in flight.
	private void ReleaseLancet(SpellData spell)
	{
		if (spell == null || !spell.Id.Equals(Lancet.SpellId, StringComparison.OrdinalIgnoreCase) || lancet == null)
			return;
		if (IsInstanceValid(lancet))
			lancet.QueueFree();
		lancet = null;
	}
}
