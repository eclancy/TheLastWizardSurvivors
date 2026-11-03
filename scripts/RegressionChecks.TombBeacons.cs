using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// The dry tomb and its three beacons (.ai/chapter-wizards.md 4.6): what holds the kindling to its sentence, which no
// build sees and a single run would only show once. Every line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	// How much of his walk a route through a crowd is taken to leave him, for the clock's slack: the hut's model put
	// a crowd at 80%; this asks the beacons to be lit together at 60%, so the dead the light draws can slow him hard.
	private const float TombCrowdPace = 0.6f;
	private const float TombBaseWalk = 220f;

	private static void ValidateTombBeacons(List<string> warnings)
	{
		// The three stand on a triangle BeaconSpacing a side round the tomb.
		float side = TombBeaconsEvent.RingRadius * MathF.Sqrt(3f);
		if (MathF.Abs(side - TombBeaconsEvent.BeaconSpacing) > 1f)
			warnings.Add($"Tomb beacons: the ring radius {TombBeaconsEvent.RingRadius} puts the beacons {side:0} px apart, not {TombBeaconsEvent.BeaconSpacing}.");

		// The clock is the puzzle, and it must be one a player can beat: lighting all three, the best order, at the base
		// walk through a crowd that leaves him TombCrowdPace of it, has to finish before the first goes out - and the
		// guttering that warns him has to be a part of a burn, not all of it.
		float walk = 2f * TombBeaconsEvent.BeaconSpacing / (TombBaseWalk * TombCrowdPace);
		float needed = TombBeaconsEvent.BeaconCount * TombBeaconsEvent.KindleSeconds + walk;
		if (needed >= TombBeaconsEvent.BurnSeconds)
			warnings.Add($"Tomb beacons: three kindles and two legs at {TombCrowdPace:P0} of his walk take {needed:0.0} s, but a beacon burns only {TombBeaconsEvent.BurnSeconds} s.");
		if (TombBeaconsEvent.GutterSeconds <= 0f || TombBeaconsEvent.GutterSeconds >= TombBeaconsEvent.BurnSeconds * 0.5f)
			warnings.Add($"Tomb beacons: the gutter ({TombBeaconsEvent.GutterSeconds} s) must be a warning at the end of a {TombBeaconsEvent.BurnSeconds} s burn.");
		if (TombBeaconsEvent.KindleDrainShare <= 0f || TombBeaconsEvent.KindleDrainShare > 1f)
			warnings.Add($"Tomb beacons: kindling he steps off must drain back, at no more than the rate it fills (share {TombBeaconsEvent.KindleDrainShare}).");
		// Standing by one and lighting it must not be possible from the next: the reaches never overlap.
		if (TombBeaconsEvent.KindleRadius * 2f >= TombBeaconsEvent.BeaconSpacing)
			warnings.Add($"Tomb beacons: a {TombBeaconsEvent.KindleRadius} px reach lets one stand light two beacons {TombBeaconsEvent.BeaconSpacing} px apart.");

		// It is the Sands' SECOND event, after the caravan (P2's API): the chapter's original event keeps stream 1.
		IReadOnlyList<SideEventEntry> sands = SideEventDirector.TableEventsFor("stage_6");
		if (sands.Count < 2 || sands[0].SiteId != "caravan_master" || sands[1].SiteId != "tomb_beacons")
			warnings.Add($"Tomb beacons: the Scorched Sands' line must be the caravan, then the tomb ({string.Join(", ", sands.Select(e => e.SiteId))}).");

		// The hint's cue is the sand, on the sound's knocks (sfx.py _TOMB_KNOCKS: 0.12, 0.98, 1.96).
		float[] knocks = StageHintCue.TombKnockTimes;
		float[] want = { 0.12f, 0.98f, 1.96f };
		if (knocks.Length != want.Length || knocks.Where((k, i) => MathF.Abs(k - want[i]) > 0.001f).Any())
			warnings.Add($"Tomb beacons: the hint's sand lifts on [{string.Join(", ", knocks)}], not hint_tomb_knock's knocks (0.12, 0.98, 1.96).");
		if (StageHintCatalog.ForSite("tomb_beacons") is not StageHint hint || hint.Cue != HintCue.Sand || hint.Sound != SfxCatalog.HintTombKnock)
			warnings.Add("Tomb beacons: its hint must be hint_tomb_knock with the sand cue.");

		// The art, at the sizes the event reads it at (tools/art/tomb_beacons.py).
		CheckTombSheet(warnings, "res://assets/bonelight/world/tomb/tomb.png", new Vector2(88 * 4, 72), "the tomb's four 88x72 frames");
		CheckTombSheet(warnings, "res://assets/bonelight/world/tomb/beacon.png", new Vector2(28 * 8, 52), "the beacon's eight 28x52 frames");
	}

	private static void CheckTombSheet(List<string> warnings, string path, Vector2 size, string what)
	{
		Texture2D texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		if (texture == null)
			warnings.Add($"Tomb beacons: {path} ({what}) is missing or not imported.");
		else if (texture.GetSize() != size)
			warnings.Add($"Tomb beacons: {path} is {texture.GetSize()}, expected {size} ({what}).");
	}
}
