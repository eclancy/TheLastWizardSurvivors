using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>
/// How hard one chapter is, on top of the run's own pace curve and the balance preset: the levers of
/// the difficulty curve (.ai/difficulty-curve.md, section 4). Every value is a multiplier, and 1 is the
/// game as it was tuned before the curve existed.
/// </summary>
public sealed class ChapterDifficulty
{
	/// <summary>A StageCatalog id.</summary>
	public string StageId { get; init; } = string.Empty;

	/// <summary>
	/// The ordinary spawns' health, on top of the pace curve (Node2DGame.UpdateSpawnScaling) and each
	/// creature's own multiplier. Elites are built from a spawn, so they carry it too. The main lever: it
	/// decides whether a build keeps the field clear, and one that cannot is overrun by the swarm
	/// (.ai/run-pacing.md 10.3) - which is how a wall stops a fresh save and lets a farmed one by.
	/// </summary>
	public float EnemyHealth { get; init; } = 1f;

	/// <summary>
	/// Every hit an enemy lands on the player, through Enemy.ScaleOutgoingDamage - contact, bolts, slams,
	/// the boss's attacks - rounded stochastically as Weaken is, so 1.25 makes a quarter of the one-damage
	/// touches land for two. Never below 1: under 1 a touch could roll nothing and read as a dodge.
	/// </summary>
	public float EnemyDamage { get; init; } = 1f;

	/// <summary>The gap between elites (Node2DGame.GetEliteIntervalSeconds): under 1 is more often.</summary>
	public float EliteInterval { get; init; } = 1f;

	/// <summary>Elites' and Wardens' health on top of everything above. The boss is BossCatalog's.</summary>
	public float ElitePower { get; init; } = 1f;
}

/// <summary>The one table of how hard each chapter is. Read once per run by Node2DGame.ApplyChapterDifficulty.</summary>
public static class ChapterDifficultyCatalog
{
	/// <summary>What a chapter without an entry plays at: the game as it was tuned (the Sketchbook).</summary>
	public static readonly ChapterDifficulty Neutral = new();

	// In CAMPAIGN order (StageCatalog.CampaignOrder), the order the curve is read in. The spawn tables
	// underneath are not flat - the Ruins' mean health multiplier is 1.41, the Cave's 1.00 - so these
	// are set for the OUTCOME the doc's target asks of each chapter (fresh and farmed saves, section 3),
	// measured with _BalanceHarness, not for a tidy column. RegressionChecks.ValidateChapterDifficulty
	// holds every playable road chapter to an entry and the damage to a road that never eases.
	private static readonly ChapterDifficulty[] Table =
	{
		// 1. Easy: a new save clears it within a try or two. Lighter spawns did not do it alone - a fresh
		// build is swamped at minute four by the NUMBER that arrive, not their health - so its elites
		// come more often and weaker: each carries a chest, and the relics and 50 XP they pay are what
		// let a starters-only build break out of the crowd (measured: siege 7-8 minutes to none).
		new() { StageId = "stage_0", EnemyHealth = 0.65f, EnemyDamage = 1.00f, EliteInterval = 0.85f, ElitePower = 0.80f },
		// 2. Clearly harder, still a fresh save's to clear - and its boss teaches the Ascension.
		new() { StageId = "stage_9", EnemyHealth = 0.75f, EnemyDamage = 1.00f, EliteInterval = 0.95f, ElitePower = 0.90f },
		// 3 on: walls. A fresh save is overrun; a farmed one passes and has to work for it.
		new() { StageId = "stage_10", EnemyHealth = 1.80f, EnemyDamage = 1.10f, EliteInterval = 0.90f, ElitePower = 1.25f },
		new() { StageId = "stage_2", EnemyHealth = 2.60f, EnemyDamage = 1.10f, EliteInterval = 0.90f, ElitePower = 1.30f },
		new() { StageId = "stage_3", EnemyHealth = 3.00f, EnemyDamage = 1.15f, EliteInterval = 0.85f, ElitePower = 1.40f },
		new() { StageId = "stage_4", EnemyHealth = 3.00f, EnemyDamage = 1.20f, EliteInterval = 0.80f, ElitePower = 1.45f },
		new() { StageId = "stage_5", EnemyHealth = 3.80f, EnemyDamage = 1.25f, EliteInterval = 0.80f, ElitePower = 1.50f },
		new() { StageId = "stage_6", EnemyHealth = 4.00f, EnemyDamage = 1.25f, EliteInterval = 0.80f, ElitePower = 1.55f },
		// 9. The hardest pass on the road (the tenth until the Cursed Dungeon moved into the castle).
		new() { StageId = "stage_7", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
		// The castle (.ai/endgame-castle.md), not playable yet: the Emberdeep's row until its builders tune
		// their own, so a chapter flipped playable never starts easier than the road behind it.
		new() { StageId = "stage_11", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
		// 11. The Cursed Dungeon, moved into the castle (.ai/endgame-castle.md, C2): 2.40 / 1.15 / 0.85 / 1.35 at
		// chapter five. The Emberdeep's row here, as the castle's are, so it never plays easier than the road
		// behind it; its own table (mean 1.35, above the Emberdeep's 1.28) makes it the harder of the two.
		new() { StageId = "stage_1", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
		new() { StageId = "stage_12", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
		new() { StageId = "stage_13", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
		// THE FAR SHORE (.ai/second-half.md 4.3, decision 11 accepted 2026-11-07): the castle's row, held flat across
		// the half - it is a place to try builds, and Hardcore is the climb. Read after the campaign's by
		// ValidateChapterDifficulty, so a far row may never ease below the Throne's damage. Each row is its chapter's
		// piece's (F3, F6-F10) to retune; none is playable yet.
		new() { StageId = "stage_14", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
		new() { StageId = "stage_15", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
		new() { StageId = "stage_16", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
		new() { StageId = "stage_17", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
		new() { StageId = "stage_18", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
		new() { StageId = "stage_19", EnemyHealth = 3.80f, EnemyDamage = 1.35f, EliteInterval = 0.75f, ElitePower = 1.65f },
	};

	public static IReadOnlyList<ChapterDifficulty> All => Table;

	public static ChapterDifficulty For(string stageId) =>
		Table.FirstOrDefault(d => d.StageId.Equals(stageId ?? string.Empty, StringComparison.OrdinalIgnoreCase)) ?? Neutral;
}
