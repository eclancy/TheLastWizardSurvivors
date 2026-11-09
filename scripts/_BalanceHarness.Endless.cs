using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// ENDLESS UNDER THE BOT (.ai/endless-mode.md, E1 and E5; Node2DGame.Endless.cs).
//
//   --endless       keep every vigil: at each warden's fall the bot stands in the ring rather than the portal (two game
//                   seconds after the fall, so the chest is set out first). The save is made one that has taken the
//                   Throne back and beaten this chapter's warden - in memory, writes off - so the run can go on.
//   --leave-at=N    with --endless: step into the portal at the N-th fall instead (N=1 is an ordinary clear).
//   --warden-limit=S with --endless: a returning warden still standing S game seconds after he arrived is felled
//                   outright (Node2DGame.DebugAdvanceEndless), so a long immortal run - the wall, the performance run -
//                   keeps its clock running through every night instead of holding at a warden the build cannot kill.
//                   Logged as "HARNESS NIGHT warden felled by the limit". Never for a measurement of the fights.
//
// A HARNESS NIGHT line at every fall: the clock, the level, the night's strength, kills a minute since the last fall,
// swarm pressure, Marginalia taken, and HP a minute offered and lost - the health ledger a night is tuned against
// (section 1.7). HARNESS SUMMARY gains nights= and endlessClock=, and the report a nights line.
//
// Give it --minutes past where the run should end (100 for the wall); the bot's clock is game time, fights included.
public partial class _BalanceHarness
{
	private bool Endless;
	private int LeaveAt;
	private float endlessFallSeenAt = -1f;
	private int endlessFallsLogged;
	private float endlessLastFallAt;
	private int endlessLastFallKills;
	private float endlessLastFallHealing;
	private int endlessLastFallHpLost;
	private readonly List<string> endlessNightLines = new();
	private float WardenLimitSeconds = -1f;
	private float endlessFightSeenAt = -1f;
	private int endlessWardensForced;

	// From the argument loop: true when the argument was endless's.
	private bool TryParseEndlessArg(string arg)
	{
		if (arg == "--endless")
		{
			Endless = true;
			SaveManager.WritesDisabled = true;
			return true;
		}
		if (arg.StartsWith("--leave-at="))
		{
			LeaveAt = Math.Max(1, int.Parse(arg.Substring("--leave-at=".Length)));
			return true;
		}
		if (arg.StartsWith("--warden-limit="))
		{
			WardenLimitSeconds = float.Parse(arg.Substring("--warden-limit=".Length), System.Globalization.CultureInfo.InvariantCulture);
			return true;
		}
		return false;
	}

	// Every harness step: --warden-limit's clock on a returning warden's fight.
	private void TickEndlessWardenLimit()
	{
		if (!Endless || WardenLimitSeconds <= 0f || game is not Node2DGame run || !IsInstanceValid(run)
			|| !run.InEndlessNight || !run.BossFightActive || run.VigilOffered)
		{
			endlessFightSeenAt = -1f;
			return;
		}
		if (endlessFightSeenAt < 0f)
			endlessFightSeenAt = elapsed;
		if (elapsed - endlessFightSeenAt < WardenLimitSeconds || BossEnemy.EnteringCount > 0)
			return;
		endlessFightSeenAt = -1f;
		endlessWardensForced++;
		GD.Print($"HARNESS NIGHT warden felled by the limit ({WardenLimitSeconds:0} s) at clock {EndlessCatalog.FormatClock(run.RunSeconds)}, night {run.EndlessNight}");
		run.DebugAdvanceEndless();
	}

	// After every other save the harness builds (--all-unlocked, --farmed, --fresh-save): the save made one on which this
	// chapter can be held through the night. In memory only.
	private void OpenEndlessOnSave()
	{
		if (!Endless || GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data is not SaveData data)
			return;
		SaveManager.WritesDisabled = true;
		if (!GlobalStatsManager.IsAchievementUnlocked(data, AchievementDefinitions.TakenBackId))
			data.UnlockedAchievementIds.Add(AchievementDefinitions.TakenBackId);
		StageDefinition stage = StageCatalog.GetByIndex(Global.SelectedStageIdx);
		BossDefinition warden = stage != null ? BossCatalog.ForStageIndex(stage.Index) : null;
		if (warden != null && !data.Lifetime.HasDefeatedBoss(warden.Id))
			data.Lifetime.BossesDefeated.Add(warden.Id);
		GD.Print($"HARNESS endless open on {stage?.DisplayName ?? "?"} (Taken Back, {warden?.Id ?? "no warden"} beaten), "
			+ (LeaveAt > 0 ? $"leaving at fall {LeaveAt}" : "keeping every vigil"));
	}

	/// <summary>
	/// At a fall: keep the vigil (true: the run goes on) or let the portal end it (false). Called where the bot steps
	/// into the portal.
	/// </summary>
	private bool KeepEndlessVigil(Node2DGame run)
	{
		if (!Endless || run == null || !run.VigilOffered)
			return false;
		if (run.EndlessWardensFelled > endlessFallsLogged)
		{
			endlessFallsLogged = run.EndlessWardensFelled;
			LogEndlessNight(run, "fall");
		}
		if (LeaveAt > 0 && run.EndlessWardensFelled >= LeaveAt)
			return false;
		if (endlessFallSeenAt < 0f)
			endlessFallSeenAt = elapsed;
		// Two game seconds, as a player walks to the chest before choosing; it also lets the ring be added (deferred).
		if (elapsed - endlessFallSeenAt < 2f)
			return true;
		endlessFallSeenAt = -1f;
		run.KeepVigil();
		return true;
	}

	private void LogEndlessNight(Node2DGame run, string what)
	{
		float minutes = Math.Max(0.01f, (elapsed - endlessLastFallAt) / 60f);
		int kills = ledgerKills - endlessLastFallKills;
		float healed = GameStats.RunHealingOffered - endlessLastFallHealing;
		int lost = GameStats.RunHpLost - endlessLastFallHpLost;
		string line = $"HARNESS NIGHT {what} {run.EndlessWardensFelled}: clock {EndlessCatalog.FormatClock(run.RunSeconds)}"
			+ $" (game {elapsed / 60f:0.0} min) night {run.EndlessNight} level {player?.CurrentLevel ?? 0}"
			+ $" strength health x{run.EndlessHealthStrength:0.00} damage x{run.EndlessDamageStrength:0.00}"
			+ $" kills/min {kills / minutes:0}"
			+ $" pressure {SwarmStat<float>("SwarmPressure"):0.00} marginalia {player?.MarginaliaTaken ?? 0}"
			+ $" healPerMin {healed / minutes:0.0} lostPerMin {lost / minutes:0.0}"
			+ $" enemies {GetTree().GetNodeCountInGroup("enemies")} orbs {GetTree().GetNodeCountInGroup(XPOrb.GroupName)}"
			+ $" nodes {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount):0}";
		GD.Print(line);
		endlessNightLines.Add("    " + line.Substring("HARNESS NIGHT ".Length));
		endlessLastFallAt = elapsed;
		endlessLastFallKills = ledgerKills;
		endlessLastFallHealing = GameStats.RunHealingOffered;
		endlessLastFallHpLost = GameStats.RunHpLost;
	}

	// The report's lines, before the run is torn down: the nights and how the run ended.
	private IEnumerable<string> EndlessReportLines()
	{
		if (!Endless || game is not Node2DGame run || !IsInstanceValid(run))
			yield break;
		if (endlessNightLines.Count == 0 || run.InEndlessNight)
			LogEndlessNight(run, "end");
		yield return $"  endless      : night {run.EndlessNight}, {run.EndlessWardensFelled} wardens felled, clock {EndlessCatalog.FormatClock(run.RunSeconds)};"
			+ $" strength at the end health x{run.EndlessHealthStrength:0.00} damage x{run.EndlessDamageStrength:0.00}";
		foreach (string line in endlessNightLines)
			yield return line;
	}

	// The summary's endless fields.
	private string EndlessSummary()
	{
		if (!Endless || game is not Node2DGame run || !IsInstanceValid(run))
			return string.Empty;
		return $" nights={run.EndlessNight} wardens={run.EndlessWardensFelled}{(endlessWardensForced > 0 ? $"(forced{endlessWardensForced})" : "")} endlessClock={run.RunSeconds / 60f:0.0}";
	}
}
