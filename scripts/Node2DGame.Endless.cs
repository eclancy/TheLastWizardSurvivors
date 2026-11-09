using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// ENDLESS: THE NIGHT GOES ON (.ai/endless-mode.md; EndlessCatalog says what the mode is). The run's half: whether this
// run can go on, the vigil at a warden's fall, the nights after it, the strength that climbs through them, the warden
// sent again every quarter hour, and the ledger - the first night's result, which is all the save ever learns from a
// run that kept the vigil.
//
// It rides the ordinary boss phase, as the Boss Rush does. A warden's fall comes back through CompleteBossVictory,
// which opens the way home and - on a run that can go on - the vigil beside it (OfferVigil). Keeping it closes the
// portal, clears the fall (bossVictoryPending, bossFightActive) so the clock, the trickle, the elites, the Warden, the
// side event and the secrets all come back, and starts the next night. The next warden arrives through SpawnBoss at
// the chapter's victory time times the night (EndlessCatalog.ChapterVictorySeconds, the one shim), and his fall is the same CompleteBossVictory.
//
// THE LEDGER (section 3.1). The moment the first vigil is kept the run's result is taken as the victory it is
// (SnapshotRunResult, with its telemetry, element peaks and boss), exactly what stepping into the portal then would
// have recorded. From then SnapshotRunResult returns it - so the pause tab and the one-second achievement watch read
// the first night, and nothing measured completes mid-night - and FinishRunAndReward hands it, not the live run, to
// everything that writes the save. The results screen still shows the whole night.
public partial class Node2DGame
{
	// Read once, at the run's start, as every flag is: a run that can go on past its warden.
	private bool endlessEligible;
	// Whether endless was open to the save when the run began: the run that first takes the Throne back says so on
	// its results (ApplyEndlessOpenedEntry), and Taken Back may complete mid-run, through the achievement watch.
	private bool endlessOpenAtStart;
	private string endlessStageId = string.Empty;
	// The record this run has to beat, as it stood when the run began (a copy: the run's own falls write the save's).
	private EndlessRecord? endlessRecordBefore;

	// The vigil open at a fall, or null.
	private VigilRing? vigilRing;
	// Vigils kept this run, and wardens felled (the first night's included). The night is one past the vigils.
	private int vigilsKept;
	private int endlessWardensFelled;

	// The first night's result, taken as the first vigil is kept; null before (and on every ordinary run).
	private RunResult? endlessLedger;
	// The run clock when the ledger was taken: the first night's end, where the later nights' purse counts from.
	private float endlessLedgerClock;
	// Arcane Energy the returning wardens' falls have earned (their bonus shares), before the cap.
	private int endlessWardenArcane;

	// The night's strength, ticked once a second (never per frame) and read by the spawner, the Warden and the boss.
	private float endlessHealthStrength = 1f;
	private float endlessDamageStrength = 1f;
	// EnemyDamageScale as the chapter's row and the preset left it, which the night's damage multiplies.
	private float endlessBaseDamageScale = 1f;
	private float endlessTickTimer;
	private float endlessOrbSweepTimer;

	/// <summary>This run can go on past its warden (EndlessCatalog.IsOpenOn, read at the start).</summary>
	public bool IsEndlessRun => endlessEligible;

	/// <summary>The vigil has been kept at least once: the run is in a night past the first.</summary>
	public bool InEndlessNight => endlessLedger != null;

	/// <summary>The night the run is in: one past the vigils kept.</summary>
	public int EndlessNight => vigilsKept + 1;

	/// <summary>Wardens felled this run, the first night's included.</summary>
	public int EndlessWardensFelled => endlessWardensFelled;

	/// <summary>The vigil is open: a warden has fallen and the run may go on.</summary>
	public bool VigilOffered => vigilRing != null && IsInstanceValid(vigilRing);

	/// <summary>The night's enemy health multiplier, on top of the chapter's and the preset's.</summary>
	public float EndlessHealthStrength => endlessHealthStrength;

	/// <summary>The night's enemy damage multiplier.</summary>
	public float EndlessDamageStrength => endlessDamageStrength;

	// THE CHAPTER'S OWN VICTORY TIME, through the one shim (EndlessCatalog.ChapterVictorySeconds): when its warden walks
	// in, real seconds of run clock, read once a run and cached - PaceMinutes asks for it many times a frame.
	private float endlessVictorySecondsCache = -1f;

	/// <summary>When this chapter's warden walks in, in real seconds of run clock (EndlessCatalog.ChapterVictorySeconds).</summary>
	public float EndlessVictorySeconds
	{
		get
		{
			if (endlessVictorySecondsCache < 0f)
				endlessVictorySecondsCache = Mathf.Max(1f, EndlessCatalog.ChapterVictorySeconds(StageCatalog.GetByIndex(Global.SelectedStageIdx)?.Id));
			return endlessVictorySecondsCache;
		}
	}

	/// <summary>The same moment on the pace clock: where everything that grows with the run stops growing (PaceMinutes).</summary>
	private float EndlessVictoryPaceSeconds => EndlessVictorySeconds / RunPace;

	/// <summary>The run clock at which the next warden is sent: the chapter's victory time, times the night.</summary>
	public float NextEndlessWardenSeconds => EndlessVictorySeconds * EndlessNight;

	// Minutes past the first fall, on the pace clock (CLAUDE.md: what decides WHEN in a run reads the pace clock), so a
	// chapter whose run is longer than fifteen minutes stretches its nights with it. 0 in the first night.
	private float EndlessNightMinutes =>
		endlessLedger == null ? 0f : Mathf.Max(0f, (PaceSeconds - EndlessVictoryPaceSeconds) / 60f);

	// From _Ready, once the save and the chapter are known.
	private void InitEndless()
	{
		SaveData? save = CurrentSaveData();
		StageDefinition? stage = StageCatalog.GetByIndex(Global.SelectedStageIdx);
		endlessOpenAtStart = EndlessCatalog.IsOpen(save);
		// Never a Boss Rush: it is its own mode, its records untouched and the night's strength never applied in it.
		endlessEligible = !IsBossRush && save != null && stage != null && EndlessCatalog.IsOpenOn(save, stage);
		if (!endlessEligible)
			return;
		endlessStageId = stage!.Id;
		EndlessRecord? held = EndlessCatalog.RecordFor(save, stage.Id, RunCharacterId, save!.BalancePresetId);
		endlessRecordBefore = held == null ? null : EndlessRecord.FromDictionary(held.ToGodotDictionary());
		GD.Print($"ENDLESS {stage.DisplayName} can be held through the night (record {(held == null ? "none" : EndlessCatalog.NightLine(EndlessCatalog.NightOf(held), held.ClockSeconds))})");
	}

	private static string RunCharacterId => CharacterRoster.GetByIndex(Global.SelectedCharacterIdx)?.Id ?? string.Empty;

	// Every frame, after the portal's tick and before the ordinary boss phase.
	private void TickEndless(float delta)
	{
		if (!endlessEligible || runFinished || player == null || !IsInstanceValid(player))
			return;

		// The vigil's hold: standing in the ring keeps it.
		if (VigilOffered && !player.IsDead && vigilRing!.Tick(player.FootPosition, delta))
		{
			KeepVigil();
			return;
		}

		if (endlessLedger == null)
			return;

		endlessTickTimer -= delta;
		if (endlessTickTimer <= 0f)
		{
			endlessTickTimer = 1f;
			TickEndlessStrength();
		}

		endlessOrbSweepTimer += delta;
		if (endlessOrbSweepTimer >= XpOrbSweepIntervalSeconds)
		{
			endlessOrbSweepTimer = 0f;
			FoldLooseOrbs();
		}

		// The warden, sent again: at the chapter's victory time times the night, the field held as every boss arrives.
		if (!bossFightActive && !bossVictoryPending && timeElapsed >= NextEndlessWardenSeconds && endlessWardenRefusedNight != EndlessNight)
			SendReturningWarden();
	}

	// The night whose warden would not spawn (SpawnBoss said why), so it is not asked for again every frame.
	private int endlessWardenRefusedNight;

	// Once a second: m, and the strength it gives, into the spawner, the Warden and every hit an enemy lands.
	private void TickEndlessStrength()
	{
		float m = EndlessNightMinutes;
		endlessHealthStrength = EndlessCatalog.HealthStrength(m);
		endlessDamageStrength = EndlessCatalog.DamageStrength(m);
		EnemyDamageScale = endlessBaseDamageScale * endlessDamageStrength;
	}

	// --- the vigil ---------------------------------------------------------------------------------------------------

	// From CompleteBossVictory, after the portal opened: on a run that can go on, the ring beside the way home.
	private void OfferVigil(Vector2 fellAt, BossDefinition? fallen)
	{
		if (!endlessEligible)
			return;
		endlessWardensFelled++;
		if (InEndlessNight && fallen != null)
		{
			// A returning warden's fall pays half his bonus into the night's purse, and the record is written now and
			// saved: an hour-long run killed by the phone keeps the nights it held (section 3.4).
			endlessWardenArcane += Mathf.RoundToInt(fallen.ArcaneVictoryBonus * EndlessCatalog.WardenBonusShare);
			WriteEndlessRecord(save: true);
		}
		if (player == null || !IsInstanceValid(player))
			return;

		// On the far side of the chest from the portal (the chest is set out between him and the body, the portal past
		// it), so the two choices are never one place - and never under his feet.
		Vector2 chest = BossChestPoint(fellAt);
		Vector2 portalAt = victoryPortal != null && IsInstanceValid(victoryPortal) ? victoryPortal.Position : fellAt;
		Vector2 away = chest - portalAt;
		Vector2 direction = away.LengthSquared() > 1f ? away.Normalized() : Vector2.Down;
		Vector2 at = ReachablePoint(chest + direction * VigilPastChest);
		if (at.DistanceTo(player.GlobalPosition) < VigilRing.RadiusX + 40f)
			at = ReachablePoint(chest + direction.Orthogonal() * VigilPastChest);
		vigilRing = new VigilRing { Name = "VigilRing", Position = at };
		CallDeferred(Node.MethodName.AddChild, vigilRing);

		SaveData? save = CurrentSaveData();
		// Two short lines at most, so the band's panel holds them on both canvases (a third overran its top): the
		// choice, then - the first time a save ever sees it - what the ring is (section 1.2). A later fall says which
		// night it ended. The Night banner, when the vigil is kept, says when the warden comes again.
		bool first = save != null && !save.HasKeptVigil;
		string line = InEndlessNight && fallen != null
			? $"{ShortBossName(fallen)} falls  ·  the {EndlessCatalog.Ordinal(EndlessNight)} night is held\nThe way home is open - or keep the vigil."
			: first ? "The way home is open - or keep the vigil.\nStand in your light, and the night goes on."
			: "The way home is open - or keep the vigil,\nand the night goes on.";
		ShowRunBanner(line, new Color(1f, 0.90f, 0.66f, 1f), first ? 6.5f : 4.5f);
		GD.Print($"ENDLESS vigil offered at {FormatTime(timeElapsed)} (warden {endlessWardensFelled} felled, night {EndlessNight}, level {player.CurrentLevel})");
	}

	// How far past the chest, away from the portal, the ring opens.
	private const float VigilPastChest = 150f;

	/// <summary>
	/// Keeps the vigil: the portal closes and the night goes on. The player stands in the ring to call it; the tools
	/// (_BalanceHarness --endless, _UiShot) call it directly.
	/// </summary>
	public void KeepVigil()
	{
		if (!VigilOffered || runFinished || !bossVictoryPending)
			return;

		// The first night ends here, as a victory, and that is what the save will learn.
		if (endlessLedger == null)
		{
			endlessLedger = SnapshotRunResult();
			endlessLedgerClock = timeElapsed;
			endlessBaseDamageScale = EnemyDamageScale;
			GD.Print($"ENDLESS ledger taken at {FormatTime(timeElapsed)}: {endlessLedger.Outcome} over {endlessLedger.BossId}, level {endlessLedger.FinalPlayerLevel}, {endlessLedger.EnemiesKilled} kills");
		}
		vigilsKept++;

		vigilRing?.QueueFree();
		vigilRing = null;
		if (victoryPortal != null && IsInstanceValid(victoryPortal))
			victoryPortal.QueueFree();
		victoryPortal = null;

		// The fall is over: the field comes back, after the breather.
		bossVictoryPending = false;
		bossFightActive = false;
		activeBossDefinition = null;
		spawnGraceRemaining = Mathf.Max(spawnGraceRemaining, EndlessCatalog.BreatherSeconds);
		TickEndlessStrength();
		// The run's music back, the hook in for good as it is past pace minute five.
		if (runMusicSet != null)
			runMusic?.PlaySet(runMusicSet, MusicPlayer.BossCrossfadeSeconds, 1f, 1f);
		SfxPlayer.Global(SfxCatalog.EventReward);

		BossDefinition? warden = BossCatalog.ForStageIndex(Global.SelectedStageIdx);
		string next = warden == null ? string.Empty
			: $"\nHe will send {EndlessMidSentence(ShortBossName(warden))} again at {EndlessCatalog.FormatClock(NextEndlessWardenSeconds)}.";
		ShowRunBanner($"Night {EndlessNight}{next}", new Color(1f, 0.90f, 0.66f, 1f), 3.5f);
		WidenRunTimer();
		runTimerLastSecond = -1;

		SaveManager? saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager?.Data != null && !saveManager.Data.HasKeptVigil)
		{
			saveManager.Data.HasKeptVigil = true;
			saveManager.SaveGame();
		}
		GD.Print($"ENDLESS vigil kept: night {EndlessNight} from {FormatTime(timeElapsed)}, the warden at {FormatTime(NextEndlessWardenSeconds)}");
	}

	// "the Hayward", not "The Hayward": a name's article is lower case mid-sentence.
	private static string EndlessMidSentence(string name) =>
		name != null && name.StartsWith("The ", StringComparison.Ordinal) ? "the " + name.Substring(4) : name ?? string.Empty;

	// The warden, sent again: the chapter's own, at half its chapter health times the night's strength now.
	private void SendReturningWarden()
	{
		BossDefinition? warden = BossCatalog.ForStageIndex(Global.SelectedStageIdx);
		if (warden == null)
			return;
		TickEndlessStrength();
		float scale = EndlessCatalog.ReturningWardenShare * endlessHealthStrength;
		if (!SpawnBoss(warden, scale))
		{
			// SpawnBoss has said why. The night goes on rather than stalling on a warden that cannot come.
			GD.PushError($"Node2DGame: endless could not send {warden.Id} again; the night goes on without him.");
			endlessWardenRefusedNight = EndlessNight;
			return;
		}
		activeBossDefinition = warden;
		bossFightActive = true;
		PlayBossMusic();
		ShowRunBanner($"{warden.DisplayName}\nSent again, to end the {EndlessCatalog.Ordinal(EndlessNight)} night.",
			new Color(1.0f, 0.86f, 0.72f, 1.0f), 3.6f);
		GD.Print($"ENDLESS warden returns: {warden.Id} with {bossInstance?.Health ?? 0} HP (x{scale:0.00}) at {FormatTime(timeElapsed)}, night {EndlessNight}, level {player?.CurrentLevel ?? 0}");
	}

	// --- what a long night leaves behind ------------------------------------------------------------------------------

	// Past MaxLooseOrbs loose orbs, the oldest fold into one bank (XPOrb.Absorb): the XP stays in the world, the node
	// count does not grow with an hour of circling. Every two seconds, in a night only - a fifteen-minute run bounds them.
	private void FoldLooseOrbs()
	{
		Godot.Collections.Array<Node> orbs = GetTree().GetNodesInGroup(XPOrb.GroupName);
		int loose = 0;
		foreach (Node node in orbs)
		{
			if (node is XPOrb orb && !orb.IsQueuedForDeletion() && !orb.Tithed)
				loose++;
		}
		int toFold = loose - EndlessCatalog.MaxLooseOrbs;
		if (toFold <= 0)
			return;
		XPOrb? bank = null;
		int folded = 0;
		// In tree order, which is the order they were dropped: the oldest first.
		foreach (Node node in orbs)
		{
			if (node is not XPOrb orb || orb.IsQueuedForDeletion() || orb.Tithed || orb == farXpBank)
				continue;
			if (bank == null)
			{
				bank = orb;
				continue;
			}
			bank.Absorb(orb.Value);
			orb.QueueFree();
			if (++folded >= toFold)
				break;
		}
	}

	/// <summary>
	/// The night's chests are dry: the run holds every relic it can take, so a chest would be a hand of dead cards
	/// (ChestItemCatalog pads with owned ones, which Player.AddChestItem ignores). In a night, a carrier then carries
	/// nothing (it still pays its XP) and a warden's fall sets out no chest (section 1.7).
	/// </summary>
	private bool EndlessChestPoolDry()
	{
		if (!InEndlessNight || player == null || !IsInstanceValid(player))
			return false;
		var owned = new HashSet<string>(player.GetOwnedChestItems(), StringComparer.OrdinalIgnoreCase);
		var blocked = new HashSet<string>(player.GetChestUnavailableItems(), StringComparer.OrdinalIgnoreCase);
		return !ChestItemCatalog.AllItemIds.Any(id => !owned.Contains(id) && !blocked.Contains(id));
	}

	// --- the HUD -----------------------------------------------------------------------------------------------------

	/// <summary>The run timer's text in a night ("Night 2 · 21:40"), or null for the plain clock.</summary>
	private string? EndlessTimerText() =>
		InEndlessNight ? EndlessCatalog.NightLine(EndlessNight, timeElapsed) : null;

	// The timer's box was sized for "15:00"; a night's line is wider, and grows both ways about the centre.
	private void WidenRunTimer()
	{
		if (GetNodeOrNull<Control>("UIOverlay/RunTimerPanel") is Control panel)
		{
			panel.GrowHorizontal = Control.GrowDirection.Both;
			panel.OffsetLeft = -96f;
			panel.OffsetRight = 96f;
		}
	}

	/// <summary>
	/// The pause screen's Run tab, in a night: where the night stands and what it has made of the field, in player
	/// words (section 1.4). Null outside a night.
	/// </summary>
	private Control? BuildEndlessSection()
	{
		if (!InEndlessNight)
			return null;
		var list = new VBoxContainer { Name = "EscapeEndless" };
		list.AddThemeConstantOverride("separation", 4);
		list.AddChild(BuildDetailLabel(EndlessCatalog.NightLine(EndlessNight, timeElapsed), PauseInk.Ink, ResponsiveLayout.TextRole.Body));
		list.AddChild(BuildDetailLabel($"The night: {EndlessCatalog.DescribeStrength(endlessHealthStrength, endlessDamageStrength)}", PauseInk.InkSoft));
		BossDefinition? warden = BossCatalog.ForStageIndex(Global.SelectedStageIdx);
		string felled = endlessWardensFelled == 1 ? "1 warden felled" : $"{endlessWardensFelled} wardens felled";
		string returns = bossFightActive ? "the warden is here"
			: warden == null ? string.Empty : $"{ShortBossName(warden)} returns at {EndlessCatalog.FormatClock(NextEndlessWardenSeconds)}";
		list.AddChild(BuildDetailLabel(returns.Length == 0 ? felled : $"{felled}  ·  {returns}", PauseInk.InkSoft));
		return BuildEscapeSection("The Night", BuildInfoFrame(list, PauseInk.Face, PauseInk.Edge, focusStop: true));
	}

	// --- the ledger, the purse and the records -------------------------------------------------------------------------

	/// <summary>
	/// What the save learns from this run: the first night's result once the vigil has been kept, the run itself
	/// otherwise. FinishRunAndReward hands this to everything that writes the save.
	/// </summary>
	private RunResult LedgerOr(RunResult live) => EndlessCatalog.LedgerFor(live, endlessLedger);

	/// <summary>
	/// The later nights' purse (section 3.2), flat after the first night's own payout: 8 a minute of clock past the
	/// first night's end, through the same multipliers, and half of each returning warden's bonus - at most
	/// EndlessCatalog.ArcaneCap a run. No level term: it would pay for levels a night makes cheap. Recorded on the
	/// breakdown. 0 outside a night.
	/// </summary>
	private int EndlessArcane(ArcaneRewardBreakdown breakdown, float rewardMultiplier)
	{
		if (endlessLedger == null)
			return 0;
		int minutes = Mathf.Max(0, Mathf.FloorToInt((timeElapsed - endlessLedgerClock) / 60f));
		int earned = EndlessCatalog.NightPurseEarned(minutes, ArcanePerMinuteSurvived, rewardMultiplier, endlessWardenArcane);
		int fromMinutes = earned - endlessWardenArcane;
		int paid = EndlessCatalog.NightPurse(earned);
		if (breakdown != null)
		{
			breakdown.NightMinutes = minutes;
			breakdown.NightWardens = Mathf.Max(0, endlessWardensFelled - 1);
			breakdown.NightBonus = paid;
			breakdown.NightCapped = earned > paid;
		}
		GD.Print($"ENDLESS purse: {minutes} min past the first night ({fromMinutes}) + wardens {endlessWardenArcane} = {earned}, paid {paid}");
		return paid;
	}

	// This run's standing now, as a record: wardens felled, the night, the clock it reached, the level.
	private EndlessRecord EndlessStanding()
	{
		SaveData? save = CurrentSaveData();
		return new EndlessRecord
		{
			StageId = endlessStageId,
			CharacterId = RunCharacterId,
			PresetId = GlobalStatsManager.NormalizeBalancePresetId(save?.BalancePresetId),
			WardensFelled = endlessWardensFelled,
			NightsReached = EndlessNight,
			ClockSeconds = timeElapsed,
			Level = player != null && IsInstanceValid(player) ? player.CurrentLevel : 0,
			When = DateTime.UtcNow.ToString("yyyy-MM-dd"),
		};
	}

	// Writes the standing into the save's records if it is better than what they hold; saves when asked to.
	private void WriteEndlessRecord(bool save)
	{
		SaveManager? saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager?.Data == null || vigilsKept <= 0)
			return;
		EndlessRecord standing = EndlessStanding();
		if (EndlessCatalog.Record(saveManager.Data, standing))
		{
			GD.Print($"ENDLESS record: {EndlessCatalog.NightLine(standing.NightsReached, standing.ClockSeconds)}, {standing.WardensFelled} wardens, level {standing.Level}");
			if (save)
				saveManager.SaveGame();
		}
	}

	// From FinishRunAndReward, with the save about to be written: the last record of a run that kept a vigil.
	private void RecordEndlessRun(SaveData data)
	{
		if (data == null || vigilsKept <= 0)
			return;
		WriteEndlessRecord(save: false);
	}

	// From FinishRunAndReward, after the boss's unlocks: the run that first takes the Throne back lists endless among
	// what it opened, after the Sketchbook's page (section 2.3). No reel and no lesson: the vigil's banner teaches it.
	private void ApplyEndlessOpenedEntry(SaveData data, RunUnlockSummary summary)
	{
		if (endlessOpenAtStart || data == null || summary == null || !EndlessCatalog.IsOpen(data))
			return;
		summary.Entries.Add(new RunUnlockEntry
		{
			Kind = RunUnlockKind.Mode,
			Id = "endless",
			Name = "Endless",
			Source = "Every chapter whose warden you have beaten can be held through the night.",
		});
	}

	/// <summary>The results' endless line, for the report (BuildRunReport). Zeros on a run that kept no vigil.</summary>
	private void FillEndlessReport(RunReport report)
	{
		if (report == null || vigilsKept <= 0)
			return;
		report.EndlessNight = EndlessNight;
		report.EndlessClockSeconds = timeElapsed;
		report.EndlessWardensFelled = endlessWardensFelled;
		report.EndlessNewRecord = EndlessStanding().Beats(endlessRecordBefore);
	}

	// --- the tools -----------------------------------------------------------------------------------------------------

	/// <summary>
	/// Tools only (_UiShot --endless-steps): one step of a night - fell the warden being fought, keep an open vigil, or
	/// bring the next warden now. Before the first fall, bring the chapter's warden. Nothing in play calls it.
	/// </summary>
	public void DebugAdvanceEndless()
	{
		if (runFinished)
			return;
		if (bossFightActive && !bossVictoryPending && bossInstance != null && IsInstanceValid(bossInstance))
			bossInstance.TakeDamage(bossInstance.Health * 10 + 1, false);
		else if (VigilOffered)
			KeepVigil();
		else if (InEndlessNight)
			timeElapsed = Mathf.Max(timeElapsed, NextEndlessWardenSeconds);
		else if (!bossPhaseTriggered && TimerVictorySeconds > 0f)
			timeElapsed = TimerVictorySeconds;
	}

	/// <summary>
	/// Tools only (_UiShot --endless-night=N,CLOCK): the run put straight into night <paramref name="night"/> at
	/// <paramref name="clockSeconds"/>, as if every vigil before it had been kept - the ledger taken as the chapter's
	/// victory, the wardens felled, the strength at that clock - so a later night's HUD and pause tab can be
	/// photographed without an hour of play. Nothing in play calls it.
	/// </summary>
	public void DebugEnterNight(int night, float clockSeconds)
	{
		if (!endlessEligible || runFinished || night < 2)
			return;
		BossDefinition? warden = BossCatalog.ForStageIndex(Global.SelectedStageIdx);
		victoryBossId = warden?.Id ?? string.Empty;
		bossPhaseTriggered = true;
		bossVictoryPending = true;
		endlessLedger = SnapshotRunResult();
		bossVictoryPending = false;
		endlessLedgerClock = EndlessVictorySeconds;
		endlessBaseDamageScale = EnemyDamageScale;
		vigilsKept = night - 1;
		endlessWardensFelled = night - 1;
		timeElapsed = Mathf.Clamp(clockSeconds, EndlessVictorySeconds * (night - 1), EndlessVictorySeconds * night - 1f);
		TickEndlessStrength();
		WidenRunTimer();
		runTimerLastSecond = -1;
		GD.Print($"ENDLESS (tool) night {night} at {FormatTime(timeElapsed)}: {EndlessCatalog.DescribeStrength(endlessHealthStrength, endlessDamageStrength)}");
	}
}
