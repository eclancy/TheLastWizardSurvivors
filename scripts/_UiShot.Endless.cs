using Godot;
using System;
using System.Linq;
using TheOneHeMissed.scripts;

// ENDLESS IN PHOTOGRAPHS (.ai/endless-mode.md E1, E3; Node2DGame.Endless.cs). Every flag works on the in-memory save
// with writes off, as every _UiShot flag that changes a save does.
//
//   --endless-open           the save made one on which this chapter can be held through the night: the Throne taken
//                            back (its deed), and the chapter's own warden in the lifetime record.
//   --endless-steps=N        N steps of a night from a quarter of the warmup, one every 40 frames, never mid-arrival
//                            (Node2DGame.DebugAdvanceEndless): bring the warden, fell him (the portal and the vigil),
//                            keep the vigil (the next night), bring the next warden, fell him... --endless-steps=2 is
//                            the fall with the vigil open; 3 is night 2 begun.
//   --endless-after=F        the first step waits for frame F as well (the run's opening banner and note are over).
//   --endless-leave          once a fall's portal is open, his feet are held in it every frame until the run ends (a
//                            level-up opened on the frame --walk-into-portal stood him there let the bot walk him out).
//   --vigil-stand=K          K frames before the shutter, his feet set in the open vigil, so the hold's arc is filling
//                            (it keeps the vigil at 1.5 s - pick K under that, about 30 at 60 fps).
//   --endless-night=N,CLOCK  a quarter of the way in, the run put straight into night N at CLOCK seconds, as if every
//                            vigil before it had been kept (Node2DGame.DebugEnterNight): the HUD and the pause tab of a
//                            later night, with --open-pause. Pair with --force-death for "Fell in the Nth night".
//   --endless-record=STAGE:WARDENS:CLOCK[:CHARACTER][:PRESET]
//                            a record written into the save, for the world map's card (repeatable).
//   --kept-vigil             the save has kept its first vigil (the card's New mark and the first banner line go).
public partial class _UiShot
{
	private bool endlessOpen;
	private int endlessSteps;
	private int endlessStepsDone;
	private int endlessNextStepFrame = -1;
	private int endlessAfter;
	private int vigilStandLead = -1;
	private bool endlessLeave;
	private bool vigilStood;
	private int endlessNightTo;
	private float endlessNightClock;
	private bool endlessNightEntered;

	// From the argument loop: true when the argument was endless's.
	private bool TryParseEndlessShotArg(string arg)
	{
		if (arg == "--endless-open")
		{
			endlessOpen = true;
			SaveManager.WritesDisabled = true;
			return true;
		}
		if (arg.StartsWith("--endless-steps="))
		{
			endlessSteps = int.Parse(arg.Substring("--endless-steps=".Length));
			SaveManager.WritesDisabled = true;
			return true;
		}
		if (arg.StartsWith("--endless-after="))
		{
			endlessAfter = int.Parse(arg.Substring("--endless-after=".Length));
			return true;
		}
		if (arg == "--endless-leave")
		{
			endlessLeave = true;
			SaveManager.WritesDisabled = true;
			return true;
		}
		if (arg.StartsWith("--vigil-stand="))
		{
			vigilStandLead = int.Parse(arg.Substring("--vigil-stand=".Length));
			return true;
		}
		if (arg.StartsWith("--endless-night="))
		{
			string[] parts = arg.Substring("--endless-night=".Length).Split(',');
			endlessNightTo = int.Parse(parts[0]);
			endlessNightClock = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 0f;
			SaveManager.WritesDisabled = true;
			return true;
		}
		if (arg.StartsWith("--endless-record="))
		{
			SaveManager.WritesDisabled = true;
			string[] parts = arg.Substring("--endless-record=".Length).Split(':');
			int wardens = parts.Length > 1 ? int.Parse(parts[1]) : 2;
			EndlessCatalog.Record(CurrentSave(), new EndlessRecord
			{
				StageId = parts[0],
				WardensFelled = wardens,
				NightsReached = Math.Max(1, wardens),
				ClockSeconds = parts.Length > 2 ? float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture) : wardens * 900f,
				CharacterId = parts.Length > 3 ? parts[3] : CharacterRoster.GetByIndex(Global.SelectedCharacterIdx)?.Id ?? string.Empty,
				PresetId = parts.Length > 4 ? parts[4] : CurrentSave().BalancePresetId,
				Level = 40,
			});
			return true;
		}
		if (arg == "--kept-vigil")
		{
			SaveManager.WritesDisabled = true;
			CurrentSave().HasKeptVigil = true;
			return true;
		}
		return false;
	}

	// Taken Back and the chapter's warden beaten, in memory. Called once every flag is read (so --stage has named the
	// chapter) and before the scene is made, whose run reads it at its start.
	private void OpenEndlessOnShotSave()
	{
		SaveData save = CurrentSave();
		if (save == null)
			return;
		if (!GlobalStatsManager.IsAchievementUnlocked(save, AchievementDefinitions.TakenBackId))
			save.UnlockedAchievementIds.Add(AchievementDefinitions.TakenBackId);
		StageDefinition stage = StageCatalog.GetByIndex(Global.SelectedStageIdx);
		BossDefinition warden = stage != null ? BossCatalog.ForStageIndex(stage.Index) : null;
		if (warden != null && !save.Lifetime.HasDefeatedBoss(warden.Id))
			save.Lifetime.BossesDefeated.Add(warden.Id);
	}

	// Every frame of the warmup, beside the rush's steps.
	private void TickEndlessShot()
	{
		if (FindNodeOfType<Node2DGame>(GetTree().Root) is not Node2DGame run)
			return;
		Player hero = FindNodeOfType<Player>(GetTree().Root);
		// Kept whole while the night is stepped through, as the rush keeps him: the photographs are of the night.
		if ((endlessSteps > 0 || endlessNightTo > 0) && !deathForced && hero != null && !hero.IsDead)
			hero.CurrentHP = hero.MaxHP;

		if (endlessNightTo > 0 && !endlessNightEntered && frames >= WarmupFrames / 4)
		{
			endlessNightEntered = true;
			run.DebugEnterNight(endlessNightTo, endlessNightClock);
			GD.Print($"_UiShot: entered night {endlessNightTo} at {endlessNightClock:0}s (endless run: {run.IsEndlessRun})");
		}

		if (endlessStepsDone < endlessSteps && frames >= WarmupFrames / 4 && frames >= endlessAfter && frames >= endlessNextStepFrame
			&& BossEnemy.EnteringCount == 0 && BossEnemy.AwaitingFightCount == 0)
		{
			run.DebugAdvanceEndless();
			endlessStepsDone++;
			endlessNextStepFrame = frames + 40;
			GD.Print($"_UiShot: endless step {endlessStepsDone} of {endlessSteps} on frame {frames} (night {run.EndlessNight}, vigil {(run.VigilOffered ? "open" : "shut")}, endless run: {run.IsEndlessRun})");
		}

		if (endlessLeave && hero != null && !hero.IsDead && !GetTree().Paused && run.VictoryPortalOpen
			&& FindNodeOfType<ArcanePortal>(GetTree().Root) is ArcanePortal door && door.Openness >= 1f)
			hero.GlobalPosition += door.GlobalPosition - hero.FootPosition;

		if (vigilStandLead >= 0 && !vigilStood && frames >= WarmupFrames - vigilStandLead && hero != null
			&& FindNodeOfType<VigilRing>(GetTree().Root) is VigilRing ring && ring.Openness >= 1f)
		{
			vigilStood = true;
			hero.GlobalPosition += ring.GlobalPosition - hero.FootPosition;
			GD.Print($"_UiShot: stood the wizard in the vigil at {ring.GlobalPosition}");
		}
	}
}
