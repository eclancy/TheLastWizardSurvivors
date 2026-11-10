using Godot;

// WINGS (Wayland the Smith's passive, `over_traps_and_water`; .ai/character-fit.md section 5, row 20). The smith made
// himself wings to get off the king's island, and since 2026-11-09 he flies when he moves (roster.wayland's flight
// frames, anim_sets.flight). Eric: "I like A, but instead of extra speed, make him immune to floor traps / water
// slowing." So Wings stopped being +10% move speed ("move_speed_10") and became what the wings would really give a
// man who skims the ground: FLOOR TRAPS NEVER TOUCH HIM, AND WATER NEVER SLOWS HIM. The same day the speed came back on
// top (in-flight 87, Eric: "Wayland can also have a movement speed buff"): +10% move speed, CharacterWingsSpeedBonus,
// applied with the flag in ApplyCharacterPassiveBonus - the flight is faster than a walk.
//
// WHERE. One flag, Winged, and every site asks it rather than asking who he is:
//   - StageHazard (the spike plates and fire vents)           - FliesOverTrap, no bite
//   - Node2DGame.UpdateHazardDamage (lava, the hazard ground)  - FliesOverTrap, no tick
//   - BurningOil (the lantern-tender's spilled oil)            - FliesOverTrap, no burn
//   - SwallowHole (the Swamp vault's mud)                     - FliesOverTrap, neither the crawl nor the sting
//   - Player.MovePlayer (wading water)                        - Winged, the wade's slow not applied
// NOT the Last Bridge's collapse front (BridgeCollapseFront): that is the chapter's chase, not a trap. And nothing
// about collision changes: his Feet still meet walls and blocking ground, and IsWading still says when his boots are in
// water - it just costs him nothing. RegressionChecks.ValidateWings checks each site still asks.
//
// HOW IT LOOKS. Standing he stands, wings folded. Over water or a trap he keeps his flight even standing still
// (UpdateBodyAnimation reads HoldsFlight): a trap that spared him this frame says so through FliesOverTrap, which is
// the only per-frame cost - a frame number written by a site that already knew his boots were on it.
public partial class Player
{
	/// <summary>
	/// True for a wizard whose passive is Wings (Wayland the Smith): floor traps never touch him and water never slows
	/// him. The one authority every trap and the wade read - never "is this Wayland".
	/// </summary>
	public bool Winged { get; private set; }

	// The physics frame a trap last found his boots over it and let him pass; -2 while none has.
	private long trapUnderfootFrame = -2;

	/// <summary>
	/// Asked by a floor trap whose ground is under his boots this frame, before it bites or holds him back. True when
	/// Wings carries him over it: the caller deals nothing and slows nothing, and he holds his flight while he stands
	/// there. False for every other wizard, and the trap does what it always did.
	/// </summary>
	public bool FliesOverTrap()
	{
		if (!Winged)
			return false;
		trapUnderfootFrame = (long)Engine.GetPhysicsFrames();
		return true;
	}

	/// <summary>
	/// Wings holding him off the ground under him: water at his boots, or a trap that spared him this frame or the last
	/// (a trap's physics step may run before or after his own). Read where the walk/idle choice is made.
	/// </summary>
	private bool HoldsFlight => Winged && (IsWading || (long)Engine.GetPhysicsFrames() - trapUnderfootFrame <= 1);
}
