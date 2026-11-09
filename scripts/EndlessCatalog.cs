using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>
/// ENDLESS: THE NIGHT GOES ON (.ai/endless-mode.md; Eric, 2026-11-08: "we want to also unlock endless mode once
/// you beat the original campaign", then, of the design, "Build it"). The one place the mode is defined: which
/// chapters may be held through the night, when a save may, how the night's strength climbs, what the returning
/// warden brings and what a night may pay. Node2DGame.Endless.cs plays it; RegressionChecks.ValidateEndless holds
/// it to its rules.
/// </summary>
/// <remarks>
/// A run that can go on is an ordinary run until its warden falls. Then a ring of his own light opens beside the
/// way home (VigilRing), and standing in it keeps the vigil: the field comes back a little stronger every minute and
/// the warden is sent again every quarter hour. Nothing new spawns that the phone has not already carried - the cap,
/// the burst window and the spawn interval all stay where the first night left them - so only health and damage
/// climb, and a night ends the way a lost chapter does, with the swarm closing in.
///
/// The first night is an ordinary run in every way the save can see (the ledger, Node2DGame.Endless.cs): what comes
/// after pays its own records, three Feats and a capped purse of Arcane Energy, and never a campaign deed more cheaply.
/// </remarks>
public static class EndlessCatalog
{
	// --- Which chapters (section 1.3) -----------------------------------------------------------------------------

	/// <summary>
	/// The road's chapters that can be held through the night, in campaign order: open or walled ground that streams
	/// forever, and a warden who fights in it - and the Cursed Dungeon, an endless maze whose only rule is the stair at
	/// the start. A list of its own, never derived from StageCatalog's order, so a chapter added to the road is not
	/// endless until someone decides it should be. Every Vigil Kept counts exactly these.
	/// </summary>
	public static readonly IReadOnlyList<string> RoadChapterIds = new[]
	{
		"stage_0",  // Enchanted Forest - Elderbark
		"stage_9",  // The Fallow Fields - the Hayward
		"stage_10", // The Broken Circle - the Ninth Stone
		"stage_2",  // Sunken Cave - the Hollow Choir
		"stage_3",  // Blighted Swamp - Mother Rot
		"stage_4",  // Mystic Ruins - the Archivist
		"stage_5",  // Frozen Waste - the Still Warden
		"stage_6",  // Scorched Sands - the Long Coil
		"stage_7",  // The Emberdeep - the Warden of the Deep
		"stage_1",  // Cursed Dungeon - the Gaoler (the castle's, but its ground is a maze like any other)
	};

	/// <summary>
	/// The Sketchbook: the kids' page can be held all night, for its records only - nothing there counts toward a
	/// Feat (Eric: no achievements there).
	/// </summary>
	public const string SketchbookId = "stage_8";

	/// <summary>
	/// The road's chapters that are NOT endless, and why (section 1.3): the Last Bridge is a band that ends at the gate
	/// yard under a front that collapses on the pace clock; the Court is one building, explored and finished; the
	/// Throne is not a map but the story's end. For the validator, which holds them out.
	/// </summary>
	public static readonly IReadOnlyList<string> ExcludedRoadChapterIds = new[] { "stage_11", "stage_12", "stage_13" };

	/// <summary>True for a chapter that can be held through the night at all (built, and on the lists above).</summary>
	public static bool IsEndlessChapter(StageDefinition stage) =>
		stage != null && stage.IsPlayable
		&& (RoadChapterIds.Contains(stage.Id, StringComparer.OrdinalIgnoreCase) || IsRecordsOnly(stage));

	/// <summary>The Sketchbook: endless, but for its records only.</summary>
	public static bool IsRecordsOnly(StageDefinition stage) =>
		stage != null && stage.Id.Equals(SketchbookId, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Endless is open on this save: the Throne taken back (GlobalStatsManager.HasTakenBackTheThrone - the deed done,
	/// or the dark wizard in the lifetime record), the one meaning the game has for "the campaign is beaten" in
	/// everything after it. Nothing of endless is shown before (section 2.2).
	/// </summary>
	public static bool IsOpen(SaveData save) => save != null && GlobalStatsManager.HasTakenBackTheThrone(save);

	/// <summary>
	/// A run on this chapter can go on past its warden: endless is open, the chapter is endless, and its own warden is
	/// in the lifetime record - Boss Rush's rule, nobody meets a returning warden before he has beaten it where it
	/// lives. So a chapter's first-ever clear never offers the vigil: that fall is the chapter's own moment.
	/// </summary>
	public static bool IsOpenOn(SaveData save, StageDefinition stage)
	{
		if (!IsOpen(save) || !IsEndlessChapter(stage))
			return false;
		BossDefinition warden = BossCatalog.ForStageIndex(stage.Index);
		return warden != null && save.Lifetime != null && save.Lifetime.HasDefeatedBoss(warden.Id);
	}

	// --- The chapter's own victory time ----------------------------------------------------------------------------

	/// <summary>
	/// THE SHIM every "past the boss" rule of endless reads: when this chapter's warden walks in, in real seconds of run
	/// clock. The pace clamp (Node2DGame.PaceMinutes), the returning warden's quarter hour and the first night's end
	/// (Node2DGame.Endless.cs) all come through here and nowhere else, never a literal 900 or TimerVictorySeconds read
	/// beside it. Today the reference run, 900 on every chapter (Node2DGame.TimerVictorySeconds' value); the far shore's
	/// per-chapter run length (StageCatalog.VictorySecondsFor, another piece's) is to be pointed at from here when the
	/// two merge - one line.
	/// </summary>
	public static float ChapterVictorySeconds(string stageId) => global::Node2DGame.ReferenceRunSeconds;

	// --- The night's strength (section 1.5) -----------------------------------------------------------------------
	//
	// One number grows: the night's strength, read from m, the minutes of the pace clock since the first warden fell.
	// It multiplies on top of the chapter's row (ChapterDifficulty) and the preset, and reaches every ordinary spawn's
	// health, the Warden's, the returning warden's, and every hit an enemy lands (Node2DGame.EnemyDamageScale, through
	// Enemy.ScaleOutgoingDamage, so Weaken still works on it).
	//
	//   health(m) = 1 + 0.08 m      for m <= 60, then health(60) x 1.08^(m - 60)
	//   damage(m) = 1 + 0.02 m      for m <= 60, then damage(60) x 1.04^(m - 60)
	//
	// Per fifteen-minute night that is +120% health and +30% damage, about Vampire Survivors' +100% and +25% a cycle.

	/// <summary>Enemy health added a minute, of the first night's, until the wall.</summary>
	public const float HealthPerMinute = 0.08f;

	/// <summary>Enemy damage added a minute, until the wall.</summary>
	public const float DamagePerMinute = 0.02f;

	/// <summary>
	/// The wall (question 3, decided): minutes past the first fall after which the strength compounds rather than
	/// climbs, doubling health about every nine minutes. At 90:00 on the clock enemies carry 18 times their first
	/// night's health, at 105:00 58 times: every run is over by about an hour and a half on the clock.
	/// </summary>
	public const float WallMinutes = 60f;

	/// <summary>Health's step a minute past the wall.</summary>
	public const float HealthCompoundPerMinute = 1.08f;

	/// <summary>Damage's step a minute past the wall.</summary>
	public const float DamageCompoundPerMinute = 1.04f;

	/// <summary>The night's health multiplier <paramref name="minutes"/> past the first fall. 1 at the fall and before it.</summary>
	public static float HealthStrength(float minutes) =>
		Strength(minutes, HealthPerMinute, HealthCompoundPerMinute);

	/// <summary>The night's damage multiplier <paramref name="minutes"/> past the first fall. 1 at the fall and before it.</summary>
	public static float DamageStrength(float minutes) =>
		Strength(minutes, DamagePerMinute, DamageCompoundPerMinute);

	private static float Strength(float minutes, float perMinute, float compound)
	{
		float m = Math.Max(0f, minutes);
		if (m <= WallMinutes)
			return 1f + perMinute * m;
		return (1f + perMinute * WallMinutes) * MathF.Pow(compound, m - WallMinutes);
	}

	/// <summary>
	/// "+120% health, +30% damage": the strength as the pause screen says it, in player words (percentages, never a
	/// raw multiplier; .ai/level-up-screen.md 4a).
	/// </summary>
	public static string DescribeStrength(float health, float damage) =>
		$"enemies +{Math.Max(0, (int)MathF.Round((health - 1f) * 100f))}% health, +{Math.Max(0, (int)MathF.Round((damage - 1f) * 100f))}% damage";

	// --- The returning warden (section 1.4) ------------------------------------------------------------------------

	/// <summary>
	/// Of the chapter's warden health, the share a returning warden brings - times the night's health strength at the
	/// moment he arrives. The second night's warden is 1.1 times the first's, the third's 1.7, the fourth's 2.3.
	/// Target: a fight of one to three minutes for the build that reached it.
	/// </summary>
	public const float ReturningWardenShare = 0.5f;

	/// <summary>
	/// Real seconds of no trickle after a kept vigil: Boss Rush's breather (Node2DGame.BossRushBreatherSeconds) - long
	/// enough to walk to the chest before the field fills again. It is how the moment feels, not where in the run it is.
	/// </summary>
	public const float BreatherSeconds = 6f;

	// --- The vigil (section 1.2) -----------------------------------------------------------------------------------

	/// <summary>Seconds he must stand in the ring to keep the vigil: long enough that walking across it to the chest never does.</summary>
	public const float VigilHoldSeconds = 1.5f;

	// --- What a night pays (section 3.2) ---------------------------------------------------------------------------

	/// <summary>
	/// The most the later nights pay in one run: about one more clear of a late chapter. The first night pays the
	/// ordinary payout exactly; endless's Feats pay their own once-only rewards, outside this.
	/// </summary>
	public const int ArcaneCap = 600;

	/// <summary>Of each returning warden's ArcaneVictoryBonus, the share a night pays (Boss Rush's ArcaneBonusShare).</summary>
	public const float WardenBonusShare = 0.5f;

	/// <summary>
	/// The later nights' purse before the cap: <paramref name="minutes"/> of clock past the first night at
	/// <paramref name="perMinute"/> (Node2DGame.ArcanePerMinuteSurvived) through the run's own <paramref name="multiplier"/>,
	/// plus the returning wardens' shares already summed. No level term: it would pay for levels a night makes cheap.
	/// </summary>
	public static int NightPurseEarned(int minutes, int perMinute, float multiplier, int wardenArcane) =>
		(int)MathF.Round(Math.Max(0, minutes) * perMinute * multiplier) + Math.Max(0, wardenArcane);

	/// <summary>What the later nights pay: <see cref="NightPurseEarned"/>, at most <see cref="ArcaneCap"/>.</summary>
	public static int NightPurse(int earned) => Math.Clamp(earned, 0, ArcaneCap);

	// --- The ledger (section 3.1) ----------------------------------------------------------------------------------

	/// <summary>
	/// THE LEDGER RULE: what the save learns from a run. Once the vigil has been kept, the first night's result (taken
	/// as the victory it was, the moment the first vigil was kept); the run itself otherwise. Node2DGame.FinishRunAndReward
	/// hands this - never the live run - to everything that writes the save: the lifetime totals, the achievements, the
	/// chapter it opens, the telemetry, and the Arcane payout's first-night part. The results screen shows the live run.
	/// </summary>
	public static RunResult LedgerFor(RunResult live, RunResult firstNight) => firstNight ?? live;

	// --- What a long night leaves behind (section 4.1) --------------------------------------------------------------

	/// <summary>
	/// Loose XP orbs a night lets lie before the oldest fold into one bank (Node2DGame.SweepFarXpOrbs' two-second
	/// sweep, XPOrb.Absorb). A fifteen-minute run bounds them; an hour at seven kills a second, circling, does not.
	/// </summary>
	public const int MaxLooseOrbs = 300;

	// --- The three Feats (section 3.3) -----------------------------------------------------------------------------
	// Arcane Energy only, never an unlock: a Feat that paid a spell or a wizard would put a gate behind a mode. Counted
	// on Default or harder, never in the Sketchbook, measured from the records - so each completes at a fall.

	public const string IntoTheNightId = "endless_into_the_night";
	public const int IntoTheNightCurrency = 150;
	public const int IntoTheNightWardens = 2;

	public const string LongWatchId = "endless_the_long_watch";
	public const int LongWatchCurrency = 400;
	public const int LongWatchWardens = 4;

	public const string EveryVigilKeptId = "endless_every_vigil_kept";
	public const int EveryVigilKeptCurrency = 500;

	/// <summary>The three Feats' ids, for the validator.</summary>
	public static readonly IReadOnlyList<string> FeatIds = new[] { IntoTheNightId, LongWatchId, EveryVigilKeptId };

	/// <summary>
	/// Whether a record counts toward the Feats: a road chapter (never the Sketchbook) on Default or harder.
	/// </summary>
	public static bool CountsForFeats(EndlessRecord record) =>
		record != null
		&& RoadChapterIds.Contains(record.StageId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
		&& GlobalStatsManager.PresetRank(record.PresetId) >= GlobalStatsManager.PresetRankDefault;

	/// <summary>The most wardens one run has felled on a chapter that counts for the Feats.</summary>
	public static int MostWardensFelled(SaveData save) =>
		save?.EndlessRecords == null ? 0
		: save.EndlessRecords.Where(CountsForFeats).Select(r => r.WardensFelled).DefaultIfEmpty(0).Max();

	/// <summary>How many of the road's endless chapters have seen the warden felled a second time in one run.</summary>
	public static int ChaptersHeldThroughTheNight(SaveData save) =>
		save?.EndlessRecords == null ? 0
		: save.EndlessRecords.Where(r => CountsForFeats(r) && r.WardensFelled >= IntoTheNightWardens)
			.Select(r => r.StageId.ToLowerInvariant()).Distinct().Count();

	// --- Records (section 3.4) -------------------------------------------------------------------------------------

	/// <summary>The save's record for one chapter, wizard and preset, or null.</summary>
	public static EndlessRecord RecordFor(SaveData save, string stageId, string characterId, string presetId)
	{
		if (save?.EndlessRecords == null)
			return null;
		string preset = GlobalStatsManager.NormalizeBalancePresetId(presetId);
		return save.EndlessRecords.FirstOrDefault(r => r.Matches(stageId, characterId, preset));
	}

	/// <summary>The best record on a chapter and preset by any wizard, or null.</summary>
	public static EndlessRecord BestOnStage(SaveData save, string stageId, string presetId)
	{
		if (save?.EndlessRecords == null)
			return null;
		string preset = GlobalStatsManager.NormalizeBalancePresetId(presetId);
		return save.EndlessRecords
			.Where(r => string.Equals(r.StageId, stageId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(r.PresetId, preset, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(r => r.WardensFelled).ThenByDescending(r => r.ClockSeconds)
			.FirstOrDefault();
	}

	/// <summary>
	/// Writes a run's standing into the save's records if it beats the record for its key: more wardens felled, or
	/// as many in a longer clock. True when it is a new record (the caller saves). A run that kept no vigil is not an
	/// endless run and records nothing.
	/// </summary>
	public static bool Record(SaveData save, EndlessRecord standing)
	{
		if (save == null || standing == null || string.IsNullOrWhiteSpace(standing.StageId))
			return false;
		save.EndlessRecords ??= new List<EndlessRecord>();
		standing.PresetId = GlobalStatsManager.NormalizeBalancePresetId(standing.PresetId);
		EndlessRecord held = RecordFor(save, standing.StageId, standing.CharacterId, standing.PresetId);
		if (held != null && !standing.Beats(held))
			return false;
		if (held != null)
			save.EndlessRecords.Remove(held);
		save.EndlessRecords.Add(standing);
		return true;
	}

	/// <summary>"Night 3 · 41:20": a run's or a record's standing in the words the HUD, the results and the card share.</summary>
	/// <remarks>Non-breaking spaces: a wrapping line (the card's) never parts "Night 4 ·" from its clock.</remarks>
	public static string NightLine(int night, float clockSeconds) => $"Night {Math.Max(1, night)} · {FormatClock(clockSeconds)}";

	/// <summary>The night a record or a run stood in: one past the vigils it kept.</summary>
	public static int NightOf(EndlessRecord record) => record == null ? 1 : Math.Max(1, record.NightsReached);

	/// <summary>"third" for 3: the results' heading for a death in a night ("Fell in the third night").</summary>
	public static string Ordinal(int n) => n switch
	{
		1 => "first", 2 => "second", 3 => "third", 4 => "fourth", 5 => "fifth", 6 => "sixth", 7 => "seventh",
		8 => "eighth", 9 => "ninth", 10 => "tenth",
		_ => $"{n}th",
	};

	/// <summary>The run clock as m:ss, or h:mm:ss past an hour.</summary>
	public static string FormatClock(float seconds)
	{
		int total = Math.Max(0, (int)seconds);
		int hours = total / 3600;
		int minutes = total / 60 % 60;
		return hours > 0 ? $"{hours}:{minutes:00}:{total % 60:00}" : $"{total / 60}:{total % 60:00}";
	}
}

/// <summary>
/// One endless record (SaveData.EndlessRecords): the best night a wizard has held a chapter through on one preset.
/// Additive, with no schema step - a save without the key reads as "never kept a vigil".
/// </summary>
public sealed class EndlessRecord
{
	public string StageId { get; set; } = string.Empty;
	public string CharacterId { get; set; } = string.Empty;
	public string PresetId { get; set; } = GlobalStatsManager.BalancePresetDefault;
	/// <summary>Wardens felled in the run, the first night's included.</summary>
	public int WardensFelled { get; set; }
	/// <summary>The night it reached: one past the vigils kept. A run left by the portal at a fall stands in that fall's night.</summary>
	public int NightsReached { get; set; } = 1;
	/// <summary>The run clock it reached - the timer the player watched, fights out (they stop it).</summary>
	public float ClockSeconds { get; set; }
	public int Level { get; set; }
	/// <summary>When it was set, as an ISO date (the card does not show it; a record's own history).</summary>
	public string When { get; set; } = string.Empty;

	public bool Matches(string stageId, string characterId, string presetId) =>
		string.Equals(StageId, stageId, StringComparison.OrdinalIgnoreCase)
		&& string.Equals(CharacterId, characterId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
		&& string.Equals(PresetId, presetId, StringComparison.OrdinalIgnoreCase);

	/// <summary>More wardens felled, or as many in a longer clock.</summary>
	public bool Beats(EndlessRecord other) =>
		other == null || WardensFelled > other.WardensFelled
		|| (WardensFelled == other.WardensFelled && ClockSeconds > other.ClockSeconds + 0.5f);

	public Godot.Collections.Dictionary ToGodotDictionary() => new()
	{
		["StageId"] = StageId ?? string.Empty,
		["CharacterId"] = CharacterId ?? string.Empty,
		["PresetId"] = PresetId ?? GlobalStatsManager.BalancePresetDefault,
		["WardensFelled"] = WardensFelled,
		["NightsReached"] = NightsReached,
		["ClockSeconds"] = ClockSeconds,
		["Level"] = Level,
		["When"] = When ?? string.Empty,
	};

	public static EndlessRecord FromDictionary(Godot.Collections.Dictionary dict)
	{
		var record = new EndlessRecord();
		if (dict == null)
			return record;
		if (dict.ContainsKey("StageId")) record.StageId = dict["StageId"].AsString();
		if (dict.ContainsKey("CharacterId")) record.CharacterId = dict["CharacterId"].AsString();
		if (dict.ContainsKey("PresetId")) record.PresetId = GlobalStatsManager.NormalizeBalancePresetId(dict["PresetId"].AsString());
		if (dict.ContainsKey("WardensFelled")) record.WardensFelled = Math.Max(0, dict["WardensFelled"].AsInt32());
		if (dict.ContainsKey("NightsReached")) record.NightsReached = Math.Max(1, dict["NightsReached"].AsInt32());
		if (dict.ContainsKey("ClockSeconds")) record.ClockSeconds = Math.Max(0f, (float)dict["ClockSeconds"].AsDouble());
		if (dict.ContainsKey("Level")) record.Level = Math.Max(0, dict["Level"].AsInt32());
		if (dict.ContainsKey("When")) record.When = dict["When"].AsString();
		return record;
	}
}
