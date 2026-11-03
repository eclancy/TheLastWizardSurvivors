using Godot;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// THE CINEMATIC RULES' CHECKS (.ai/cutscenes.md, "The cinematic rules"; Eric, 2026-11-03: "Always give a pause before
// panning, or beginning play after a cutscene, and don't move too fast, so the player can keep up visually"). Every
// hold is a named constant somewhere, and a constant can be set to zero in a hurry by someone fixing something else;
// these say so when one is. Every line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	// The floors: under them a hold reads as a hitch, not a held shot. The ceilings: over them, the game waiting.
	private const float MinHoldSeconds = 0.4f;
	private const float MaxHoldSeconds = 1.2f;
	// Rule 5's ceiling on any arrival pan, world px/s (a sixth of a PC screen a second at the arrival's zoom is 220).
	private const float MaxEyePanSpeed = 260f;
	// The share of the furthest lean the camera must have covered by a boss's climax, so the reveal is framed.
	private const float ClimaxFramedShare = 0.85f;

	private static void ValidateCinematicRules(List<string> warnings)
	{
		ValidateArrivalHolds(warnings);
		ValidateDefeatBeats(warnings);
		ValidateReelMoves(warnings);
		if (ThroneEnding.RollHoldSeconds < MinHoldSeconds || ThroneEnding.RollEaseSeconds <= 0f)
			warnings.Add($"Cinematics: the ending's credits roll after {ThroneEnding.RollHoldSeconds}s and ease in over {ThroneEnding.RollEaseSeconds}s; rule 3 wants a hold of at least {MinHoldSeconds}s and an ease.");
	}

	// A BOSS'S ARRIVAL (Node2DGame.BossEntrance.cs): the hold before the pan, on the arrival after it, before play;
	// the pan's caps; and the furthest lean walked through the camera's own step, which must ease in, keep under the
	// cap and land without overshoot.
	private static void ValidateArrivalHolds(List<string> warnings)
	{
		if (Node2DGame.BossEntrancePanDelay < Node2DGame.LetterboxInSeconds || Node2DGame.BossEntrancePanDelay > MaxHoldSeconds)
			warnings.Add($"Cinematics: an arrival pans {Node2DGame.BossEntrancePanDelay}s into its hold; rule 3 wants the bars in ({Node2DGame.LetterboxInSeconds}s) and a beat first, and no more than {MaxHoldSeconds}s.");
		CheckHold(warnings, "an arrival's hold on the landed boss (BossEntranceLandedHold)", Node2DGame.BossEntranceLandedHold);
		CheckHold(warnings, "an arrival's hold before play (BossEntrancePlayHold)", Node2DGame.BossEntrancePlayHold);
		if (Node2DGame.BossEntranceRestHold < 0.3f || Node2DGame.BossEntranceRestHold > MaxHoldSeconds)
			warnings.Add($"Cinematics: the arrival's camera goes back after {Node2DGame.BossEntranceRestHold}s at rest on the boss; under 0.3 s it is a pan straight into a pan.");
		if (Node2DGame.BossEntrancePanMaxSpeed <= 0f || Node2DGame.BossEntrancePanMaxSpeed > MaxEyePanSpeed || Node2DGame.BossEntrancePanMaxAccel <= 0f)
			warnings.Add($"Cinematics: the arrival pans at up to {Node2DGame.BossEntrancePanMaxSpeed} px/s and {Node2DGame.BossEntrancePanMaxAccel} px/s2; rule 5 wants a cap of at most {MaxEyePanSpeed} px/s and an eased start.");

		var (peak, overshoot, firstFrame, _) = WalkFurthestPan(float.MaxValue);
		if (peak > Node2DGame.BossEntrancePanMaxSpeed + 0.5f)
			warnings.Add($"Cinematics: the arrival's furthest pan peaks at {peak:0} px/s, over its own cap of {Node2DGame.BossEntrancePanMaxSpeed}.");
		if (overshoot > 1f)
			warnings.Add($"Cinematics: the arrival's furthest pan overshoots by {overshoot:0.0} px and comes back: rule 5's ease out is a settle, not a bounce.");
		// An ease in: the first sixtieth moves the camera no further than one step at the accel cap does.
		if (firstFrame > Node2DGame.BossEntrancePanMaxAccel / 3600f + 0.01f)
			warnings.Add($"Cinematics: the arrival's pan moves {firstFrame:0.00} px on its first frame; it lurches rather than easing in.");
	}

	private static void CheckHold(List<string> warnings, string what, float seconds)
	{
		if (seconds < MinHoldSeconds || seconds > MaxHoldSeconds)
			warnings.Add($"Cinematics: {what} is {seconds}s; outside {MinHoldSeconds}-{MaxHoldSeconds}s it is a hitch or a wait.");
	}

	/// <summary>
	/// The furthest lean (BossEntranceCameraLean) walked from rest through Node2DGame.StepPan at 60 fps: its peak speed,
	/// its overshoot, how far it moved on its first frame, and the share of the lean covered at <paramref name="atSeconds"/>.
	/// </summary>
	private static (float Peak, float Overshoot, float FirstFrame, float CoveredAt) WalkFurthestPan(float atSeconds)
	{
		const float Lean = 230f;
		var want = new Vector2(Lean, 0f);
		Vector2 now = Vector2.Zero, velocity = Vector2.Zero;
		float peak = 0f, overshoot = 0f, first = 0f, covered = 0f;
		for (int frame = 1; frame <= 360; frame++)
		{
			Node2DGame.StepPan(ref now, ref velocity, want, 1f / 60f);
			if (frame == 1)
				first = now.X;
			peak = Mathf.Max(peak, velocity.Length());
			overshoot = Mathf.Max(overshoot, now.X - Lean);
			if (frame / 60f <= atSeconds)
				covered = now.X / Lean;
		}
		return (peak, overshoot, first, covered);
	}

	// Per boss, from ValidateBossPatterns: its climax - the reveal, where the speed lines burst - lands once the camera
	// has panned most of the way to it, even from the furthest lean, so the impact is framed rather than at the edge.
	private static void ValidateArrivalClimaxFramed(List<string> warnings, string bossId, BossEnemy probe)
	{
		if (probe.EntranceRoarSeconds < 0f || probe.EntranceSeconds <= 0f)
			return;
		float climax = probe.EntranceDelaySeconds + probe.EntranceRoarSeconds;
		float panning = climax - Node2DGame.BossEntrancePanDelay;
		float covered = panning <= 0f ? 0f : WalkFurthestPan(panning).CoveredAt;
		if (covered < ClimaxFramedShare)
			warnings.Add($"Boss '{bossId}' reaches its climax {climax:0.00}s into its arrival, with the camera {covered:P0} of the way to it; "
				+ $"under {ClimaxFramedShare:P0} the reveal lands at the edge of the picture (lead it with EntranceDelaySeconds, .ai/cutscenes.md rule 3).");
	}

	// THE DEFEAT (DefeatSequence): the freeze held, the zoom long enough to ride, a held close-up, a beat before the word.
	private static void ValidateDefeatBeats(List<string> warnings)
	{
		if (DefeatSequence.FreezeHold < MinHoldSeconds)
			warnings.Add($"Cinematics: the defeat's freeze holds {DefeatSequence.FreezeHold}s before the zoom; under {MinHoldSeconds}s it is not seen.");
		if (DefeatSequence.ZoomSeconds < 0.45f)
			warnings.Add($"Cinematics: the defeat's crash zoom takes {DefeatSequence.ZoomSeconds}s; under 0.45 s the eye cannot ride it (rule 5).");
		if (DefeatSequence.CloseUpHold < 0.2f)
			warnings.Add($"Cinematics: the defeat's close-up holds {DefeatSequence.CloseUpHold}s before he falls; under 0.2 s the zoom runs into the fall.");
		if (DefeatSequence.WordBeat < MinHoldSeconds)
			warnings.Add($"Cinematics: \"Defeat\" comes {DefeatSequence.WordBeat}s after he lands; under {MinHoldSeconds}s the word runs into the fall.");
		if (DefeatSequence.ArmAt <= DefeatSequence.TitleAt)
			warnings.Add("Cinematics: the defeat arms before its word is up: a press could skip what it is there to show.");
	}

	// THE REELS (Prologue): every panel's drift slow and still at both ends, a black hold after the last, and every
	// impact beat inside its panel's hold, on its frame - and the break's own, which the freeing turns on.
	private static void ValidateReelMoves(List<string> warnings)
	{
		if (Prologue.DriftStillIn < 0.7f || Prologue.DriftStillOut <= 0f)
			warnings.Add($"Cinematics: a reel's panels drift after {Prologue.DriftStillIn}s and stop {Prologue.DriftStillOut}s before the end; rule 3 wants them still through the dissolve in (0.7 s) and at rest before the next.");
		CheckHold(warnings, "the black after a reel's last panel (Prologue.EndHoldSeconds)", Prologue.EndHoldSeconds);
		Vector2 frame = Prologue.FrameArtSize;
		foreach (Prologue.Reel reel in Prologue.Reels.Values)
		{
			foreach (Prologue.Beat beat in reel.Beats)
			{
				float speed = Prologue.PeakDriftSpeed(beat);
				if (speed > Prologue.MaxDriftSpeed)
					warnings.Add($"Cinematics: '{reel.Id}' panel '{beat.Name}' drifts at up to {speed:0} canvas px/s; rule 5 keeps a panel under {Prologue.MaxDriftSpeed}.");
				if (beat.Impact is Prologue.PanelImpact impact)
				{
					if (impact.At <= Prologue.DriftStillIn * 0.5f || impact.At >= beat.Seconds - 0.7f)
						warnings.Add($"Cinematics: '{reel.Id}' panel '{beat.Name}' lands its impact at {impact.At}s, outside its hold (0.35 s to {beat.Seconds - 0.7f:0.0}s).");
					if (impact.Art.X < 0f || impact.Art.Y < 0f || impact.Art.X >= frame.X || impact.Art.Y >= frame.Y)
						warnings.Add($"Cinematics: '{reel.Id}' panel '{beat.Name}' throws its impact from {impact.Art}, off its {frame.X}x{frame.Y} frame.");
				}
			}
		}
		bool breakHasImpact = false;
		foreach (Prologue.Beat beat in Prologue.Reels[Prologue.PrologueReelId].Beats)
			breakHasImpact |= beat.Name == "break" && beat.Impact != null;
		if (!breakHasImpact)
			warnings.Add("Cinematics: the prologue's break has no impact beat: the chain breaks without speed lines, a shake or a flash (rule 1).");
	}
}
