using Godot;
using System;
using TheOneHeMissed.scripts;

// CORPOSANT (scripts/Corposant.cs): everything the wizard owns of it is here, so the fire loop says one line.
//
// THE CAST AIMS AT NOTHING. Every cooldown of the ONE spellFireTimers loop looses the cast's balls - the spell's count
// plus the player's Amount - in random directions from where he stands, whether or not anything is near: a spell that
// waited for a target would be aiming itself (the facing family's rule, Player.FireStormsweep). So it is always Fired,
// and the Lightning cast cue is its launch; every strike sounds through Player.DealDamageToEnemy's Lightning impact,
// throttled there. No new sound (.ai/audio-direction.md section 5: a launch is a once-a-second cast, a strike a
// many-a-second impact, and both voices exist).
//
// Balls already loosed burn out their life whatever silences the fire loop afterwards (a boss arriving, the dark
// wizard's seal): what left the hand has left it, as Folio's pages fly on (Player.Seal.cs).
public partial class Player
{
	private Corposant? corposant;

	private CastOutcome FireCorposant(SpellData spell, PackedScene scene)
	{
		if (scene == null || spell == null)
			return CastOutcome.NoTarget;
		if (corposant == null || !IsInstanceValid(corposant))
		{
			if (GetParent() is not Node2D arena)
				return CastOutcome.NoTarget;
			corposant = scene.Instantiate<Corposant>();
			corposant.PlayerRef = this;
			arena.AddChild(TagSpell(corposant));
		}
		int balls = Math.Max(1, spell.GetProjectileCountAtLevel(spell.CurrentLevel) + AmountFor(spell));
		// It draws itself from the radius it strikes with and repaints its own element path (Spindrift's sea-blue), so it
		// takes only a Legendary's gold from the visual tint, never a node scale.
		corposant.Refresh(spell, GetSpellDamageMultiplier(spell), GetSpellVisualTint(spell, paintsOwnPath: true),
			GetEffectiveAreaMultiplier(), durationMultiplier);
		return corposant.Launch(balls) > 0 ? CastOutcome.Fired : CastOutcome.NoTarget;
	}

	// The balls go with the spell (RemovePersistentSpellInstance: the Penknife, a fusion), every one in flight.
	private void ReleaseCorposant(SpellData spell)
	{
		if (spell == null || !spell.Id.Equals(Corposant.SpellId, StringComparison.OrdinalIgnoreCase) || corposant == null)
			return;
		if (IsInstanceValid(corposant))
			corposant.QueueFree();
		corposant = null;
	}
}
