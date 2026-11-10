using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// WINGS, Wayland the Smith's passive since 2026-11-09 (Player.Wings.cs; .ai/character-fit.md section 5, row 20): floor
// traps never touch him and water never slows him. Each site is one line asking one flag, and a site that stops asking
// fails silently in play - he takes a bite from a plate he flies over, or crawls through a pond on wings. A build cannot
// see that, so this reads the sites' own source (the validator runs from the project, where res://scripts is on disk),
// as well as his file, the id and the card's glyph.
public static partial class RegressionChecks
{
	private const string WingsPassiveId = "over_traps_and_water";

	// (file under res://scripts, what the site is, the text that says it asks the flag)
	private static readonly (string File, string Site, string Asks)[] WingsSites =
	{
		("StageHazard.cs", "the spike plates and fire vents", "FliesOverTrap()"),
		("Node2DGame.cs", "the hazard ground's tick (lava)", "FliesOverTrap()"),
		("BurningOil.cs", "the lantern-tender's burning oil", "FliesOverTrap()"),
		("SwallowHole.cs", "the Swallow-Hole's mud", "FliesOverTrap()"),
		("Player.cs", "the wade's slow (MovePlayer)", "IsWading && !Winged"),
		("Player.cs", "the flight held over water or a trap (UpdateBodyAnimation)", "|| HoldsFlight"),
	};

	private static void ValidateWings(List<string> warnings)
	{
		CharacterData wayland = CharacterRoster.GetAll().FirstOrDefault(c => c?.Id == "wayland");
		if (wayland == null)
			warnings.Add("Wings: no character 'wayland' in the roster to carry it.");
		else
		{
			if (!string.Equals(wayland.StartingPassiveId, WingsPassiveId, StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Wings: Wayland names starting passive '{wayland.StartingPassiveId}', not '{WingsPassiveId}', so traps bite him and water slows him.");
			// The boot with the air streaming off it - not the slow glyph the line's last words would claim.
			if (StatIcons.KeyFor(wayland.StartingPassiveDescription) != "speed")
				warnings.Add($"Wings: his card line takes the '{StatIcons.KeyFor(wayland.StartingPassiveDescription)}' glyph, not the winged boot ('speed').");
		}
		if (!Player.CharacterPassiveBonusIds.Contains(WingsPassiveId))
			warnings.Add($"Wings: '{WingsPassiveId}' is not in Player.CharacterPassiveBonusIds, so it silently grants nothing.");
		if (Player.CharacterPassiveBonusIds.Contains("move_speed_10"))
			warnings.Add("Wings: the retired 'move_speed_10' (+10% move speed) is still a recognised passive.");

		foreach (var (file, site, asks) in WingsSites)
		{
			string text = Godot.FileAccess.GetFileAsString("res://scripts/" + file);
			if (string.IsNullOrEmpty(text))
				warnings.Add($"Wings: res://scripts/{file} could not be read to check {site}.");
			else if (!text.Contains(asks, StringComparison.Ordinal))
				warnings.Add($"Wings: {site} ({file}) no longer asks the flag ('{asks}'), so it touches Wayland as it does anyone.");
		}
		// The Last Bridge's collapse front is the chapter's chase, not a trap: Wings must never carry him over it.
		string front = Godot.FileAccess.GetFileAsString("res://scripts/BridgeCollapseFront.cs");
		if (!string.IsNullOrEmpty(front) && (front.Contains("FliesOverTrap", StringComparison.Ordinal) || front.Contains("Winged", StringComparison.Ordinal)))
			warnings.Add("Wings: BridgeCollapseFront asks the Wings flag; the bridge's collapse is the chapter's chase, not a floor trap.");
		ValidateWaterRiders(warnings);
	}

	// RIDING OVER WATER (in-flight 74, Eric 2026-11-09): Baba Yaga and Nimue are neither slowed nor sunk by the water, by
	// a trait on their CharacterData rather than a passive. Exactly the two carry it, and each site still asks it: a lost
	// flag in a .tres, or a site that stops asking, slows the mortar silently.
	private static readonly string[] WaterRiderIds = { "baba_yaga", "nimue" };

	private static readonly (string File, string Site, string Asks)[] WaterRiderSites =
	{
		("Player.cs", "the wade's slow (MovePlayer)", "!Winged && !RidesOverWater"),
		("Player.Wading.cs", "the wade's sink", "!RidesOverWater"),
		("SwallowHole.cs", "the Swallow-Hole's crawl", "!player.RidesOverWater"),
	};

	private static void ValidateWaterRiders(List<string> warnings)
	{
		foreach (CharacterData character in CharacterRoster.GetAll())
		{
			if (character == null)
				continue;
			bool should = WaterRiderIds.Contains(character.Id);
			if (character.RidesOverWater != should)
				warnings.Add(should
					? $"Water riders: {character.Name} does not set RidesOverWater, so the water slows and sinks her."
					: $"Water riders: {character.Name} sets RidesOverWater, which only Baba Yaga and Nimue were given (in-flight 74).");
		}
		foreach (var (file, site, asks) in WaterRiderSites)
		{
			string text = Godot.FileAccess.GetFileAsString("res://scripts/" + file);
			if (string.IsNullOrEmpty(text))
				warnings.Add($"Water riders: res://scripts/{file} could not be read to check {site}.");
			else if (!text.Contains(asks, StringComparison.Ordinal))
				warnings.Add($"Water riders: {site} ({file}) no longer asks RidesOverWater ('{asks}').");
		}
	}
}
