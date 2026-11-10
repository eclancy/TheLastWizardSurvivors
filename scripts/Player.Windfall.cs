using Godot;
using System;
using TheOneHeMissed.scripts;

// WINDFALL (scripts/Windfall.cs): everything the wizard owns of it is here, so the fire loop says one line.
//
// THE CAST PLANTS A TREE at his feet, on the ONE spellFireTimers loop's cooldown - or, when one of his trees already stands
// close by, answers NoTarget, so the timer stays ripe and the tree goes in the moment he steps clear (Hunter's Draw's pattern:
// silent, uncounted, nothing spent). A planting is Fired: the Grass cast cue is the tree going in, the cast is tallied and
// charges the Canticle. No new sound (.ai/audio-direction.md section 5): the thuds are the impacts DealDamageToEnemy plays.
//
// The tree cap is the spell's projectile count plus the player's Amount (Kindled Ward's pattern: a placed spell's count is a
// ceiling on how many stand). The ripening clock runs at his cast speed: the fire loop's interval over the spell's own cooldown,
// so a haste that plants faster ripens faster too, and a tree still fills before it is the oldest.
//
// The numbers are read on every cast and, while the cast waits ripe, every RefreshSeconds (Windfall.RefreshDue) - so a level
// taken mid-cooldown reaches the standing trees by the next planting at the latest. What is standing ripens, is bumped and
// shaken whatever silences the fire loop afterwards: a tree that is planted stays planted.
public partial class Player
{
	private Windfall? windfall;

	private CastOutcome FireWindfall(SpellData spell, PackedScene scene, float interval)
	{
		if (scene == null || spell == null)
			return CastOutcome.NoTarget;
		if (windfall == null || !IsInstanceValid(windfall))
		{
			if (GetParent() is not Node2D arena)
				return CastOutcome.NoTarget;
			windfall = scene.Instantiate<Windfall>();
			windfall.PlayerRef = this;
			arena.AddChild(TagSpell(windfall));
		}
		if (windfall.RefreshDue(spell))
		{
			int trees = Math.Max(1, spell.GetProjectileCountAtLevel(spell.CurrentLevel) + AmountFor(spell));
			float own = MathF.Max(0.05f, spell.GetCooldownAtLevel(spell.CurrentLevel));
			// It draws itself and repaints its own element path (Cider Rot's rot), so it takes only a Legendary's gold.
			windfall.Refresh(spell, GetSpellDamageMultiplier(spell), GetSpellVisualTint(spell, paintsOwnPath: true),
				GetEffectiveAreaMultiplier(), trees, interval / own);
		}
		return windfall.Plant(FootPosition) ? CastOutcome.Fired : CastOutcome.NoTarget;
	}

	// The orchard goes with the spell (RemovePersistentSpellInstance: the Penknife, a fusion), every tree and fruit in it.
	private void ReleaseWindfall(SpellData spell)
	{
		if (spell == null || !spell.Id.Equals(Windfall.SpellId, StringComparison.OrdinalIgnoreCase) || windfall == null)
			return;
		if (IsInstanceValid(windfall))
			windfall.QueueFree();
		windfall = null;
	}
}
