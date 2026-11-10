using Godot;
using System;
using TheOneHeMissed.scripts;

// INK RING (scripts/InkRing.cs): everything the wizard owns of it is here, so the fire loop says one line.
//
// NO TIMER PUTS ANYTHING INTO THE WORLD. The spell's cooldown is the RECHARGE between rings, kept by the ONE
// spellFireTimers loop like every other spell's; nothing here runs a clock of its own. While the cooldown is ripe the
// loop calls FireInkRing every frame, which arms the node and answers NoTarget - so the timer stays ripe, as Hunter's
// Draw's does while the bow is drawn - until the wizard walks across his own line and the node strikes. The next call
// answers Fired: the Water or Darkness cast cue is the ring closing (no new sound, .ai/audio-direction.md section 5), the
// cast is tallied and charges the Canticle, and the recharge starts. A ring closed is one cast, however many it struck.
//
// The line trails him whatever the cooldown is doing, so the next ring can be walked while the last one recharges; only
// what silences the fire loop - a boss arriving, the dark wizard's seal, the Sovereign Rite's silence, death - lifts the
// brush (InkRingMayCast). What is on the ground dries away as it would.
public partial class Player
{
	private InkRing? inkRing;

	private CastOutcome FireInkRing(SpellData spell, PackedScene scene)
	{
		InkRing? ring = EnsureInkRing(spell, scene);
		if (ring == null)
			return CastOutcome.NoTarget;
		return ring.TakeClosed() ? CastOutcome.Fired : CastOutcome.NoTarget;
	}

	// The node, made the first time the cooldown comes round; its numbers are read again whenever the level or a
	// milestone changes and a few times a second otherwise (InkRing.RefreshDue), never on every ripe frame.
	private InkRing? EnsureInkRing(SpellData spell, PackedScene scene)
	{
		if (scene == null || spell == null)
			return null;
		if (inkRing == null || !IsInstanceValid(inkRing))
		{
			if (GetParent() is not Node2D arena)
				return null;
			inkRing = scene.Instantiate<InkRing>();
			inkRing.PlayerRef = this;
			arena.AddChild(TagSpell(inkRing));
		}
		if (inkRing.RefreshDue(spell))
			inkRing.Refresh(spell, GetSpellDamageMultiplier(spell), durationMultiplier);
		return inkRing;
	}

	/// <summary>Whether Ink Ring's brush may lay ink now: what silences the fire loop lifts the brush too.</summary>
	public bool InkRingMayCast(SpellData spell) =>
		spell != null && !IsDead && !HeldForBossEntrance && !IsSilenced(spell) && !IsSealed(spell);

	// The ink goes with the spell (RemovePersistentSpellInstance: the Penknife, a fusion), every ring and prison wall in it.
	private void ReleaseInkRing(SpellData spell)
	{
		if (spell == null || !spell.Id.Equals(InkRing.SpellId, StringComparison.OrdinalIgnoreCase) || inkRing == null)
			return;
		if (IsInstanceValid(inkRing))
			inkRing.QueueFree();
		inkRing = null;
	}
}
