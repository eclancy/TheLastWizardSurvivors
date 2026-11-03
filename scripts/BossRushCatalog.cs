using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>
/// BOSS RUSH (issue #12, "the ultimate endgame mode with sequential boss encounters";
/// .ai/boss-levels-design.md, "Boss Rush"). One run in which every warden of the road comes for
/// the player in the order the road met them, and the last is the Warden of the Deep.
/// </summary>
/// <remarks>
/// It is an ordinary run with its bosses spread through it rather than one at the end, and the
/// shape is chosen so that nothing about pacing has to be re-tuned for it:
///
///   - The arena is the final chapter, his seat. The rush is the long road back walked in one go,
///     which is also what the Warden of the Deep's own fight already says ("every fight you have
///     already won, in sequence"), so the mode ends on the one boss built to end it.
///   - The bosses arrive on the PACE clock, evenly from <see cref="FirstArrivalPaceSeconds"/>, and the
///     last at the ordinary boss time. The clock stops for every fight, as it does for the one boss
///     of a normal run, so a fight never costs the player run time and the gap between two arrivals is
///     always the full step of swarm, XP and chests.
///   - Each boss is scaled to where in the run it arrives, by the same curve the swarm's health
///     follows (Node2DGame.BossRushHealthScale): a boss met at minute four is as tough against a
///     minute-four build as it is against a fifteen-minute one at the end of its own chapter.
///   - Felling one pays a chest where it fell and half its chapter bonus in Arcane Energy; the run
///     is won only when the last falls.
///
/// It opens only once every boss on its roster has fallen in his own chapter (<see cref="IsOpen"/>), so
/// nobody meets a boss here before they have beaten it where it lives, and so the rush records nothing
/// about chapters: it opens none, grants no chapter deed and writes no boss to the save's defeated list
/// (Node2DGame.BuildRunResult). Its own records are SaveData.BossRushBestFelled and
/// BossRushBestClearSeconds.
///
/// HELD AS IT WAS when the Cursed Dungeon moved into the castle (.ai/endgame-castle.md 1.8, Eric: "Boss Rush
/// unchanged"): the road's ten in the order the road met them, the Gaoler fifth (<see cref="RoadOrder"/>),
/// on the Emberdeep (<see cref="Arena"/>), meeting the Gaoler at the health it always did
/// (<see cref="HealthOf"/>).
/// </remarks>
public static class BossRushCatalog
{
	/// <summary>When the first boss arrives, on the pace clock. The rest follow evenly to the ordinary boss time.</summary>
	/// <remarks>
	/// Two and a half minutes: long enough for a wizard to hold two or three spells, and early enough
	/// that the run is mostly bosses. Earlier and Elderbark meets a lone level-five starter spell.
	/// </remarks>
	public const float FirstArrivalPaceSeconds = 150f;

	/// <summary>
	/// The floor on a boss's health scale. The swarm curve starts at 6 / 156 of its end value, which
	/// would make a boss at minute two a few seconds' work: too short to see the fight it is.
	/// </summary>
	public const float MinHealthScale = 0.2f;

	/// <summary>
	/// Of a chapter's boss health, the share every rush boss brings, the finale included - on top of
	/// the arrival curve above.
	/// </summary>
	/// <remarks>
	/// 2026-10-31: a chapter's boss fight was made to last three to five minutes (.ai/enemy-behaviour.md,
	/// "Boss entrances and longer fights"), by phases and by health. Ten of those back to back would be
	/// forty minutes of bosses in a mode built as a run a few minutes longer than an ordinary one, so
	/// the rush keeps every phase (thresholds are fractions, so each fight still turns at the same
	/// points) and brings about a third of the health: each fight its whole shape, at about the length
	/// a chapter's fight had before.
	/// </remarks>
	public const float FightHealthShare = 0.35f;

	/// <summary>Of each felled boss's ArcaneVictoryBonus, the share the rush pays.</summary>
	/// <remarks>
	/// Half: a full clear pays about twice what the finale pays on its own, for a run a few minutes
	/// longer than an ordinary one that faces eight bosses instead of one.
	/// </remarks>
	public const float ArcaneBonusShare = 0.5f;

	/// <summary>
	/// The chapter the rush is fought in: the Emberdeep, the first gate chapter (StageCatalog.GateChapter).
	/// It was the campaign's last (FinalChapter), which is the Throne once the castle comes after the
	/// gate - and the rush is the road's wardens on the road's summit, not a fight in his hall
	/// (.ai/endgame-castle.md 1.8, unchanged by Eric's choice).
	/// </summary>
	public static StageDefinition Arena => StageCatalog.GateChapter;

	/// <summary>
	/// The rush's chapters, in the order it sends their bosses: the road's ten as the road met them before
	/// the Cursed Dungeon moved into the castle, the Gaoler fifth, the arena's own boss last. A list of its
	/// own, never derived from StageCatalog's campaign order - which now puts the Gaoler after the Warden of
	/// the Deep and the castle's bosses after him - so no later reordering can change the rush.
	/// RegressionChecks.ValidateBossRush holds every id to a playable chapter whose boss has its own scene.
	/// </summary>
	public static readonly IReadOnlyList<string> RoadOrder = new[]
	{
		"stage_0",  // Elderbark
		"stage_9",  // the Hayward
		"stage_10", // the Ninth Stone
		"stage_2",  // the Hollow Choir
		"stage_1",  // the Gaoler (chapter five when the rush was made; the castle's eleventh since C2)
		"stage_3",  // Mother Rot
		"stage_4",  // the Archivist
		"stage_5",  // the Still Warden
		"stage_6",  // the Long Coil
		"stage_7",  // the Warden of the Deep, the finale
	};

	/// <summary>Every boss of <see cref="RoadOrder"/>, in its order; the arena's is last.</summary>
	public static IReadOnlyList<BossDefinition> Roster =>
		RoadOrder
			.Select(StageCatalog.GetById)
			.Select(stage => stage != null ? BossCatalog.ForStageIndex(stage.Index) : null)
			.Where(boss => boss != null)
			.ToList();

	/// <summary>The rush's last boss: the arena's.</summary>
	public static BossDefinition Finale => Arena == null ? null : BossCatalog.ForStageIndex(Arena.Index);

	/// <summary>
	/// The health a rush boss is scaled from (Node2DGame.SpawnBoss, before the arrival curve and
	/// <see cref="FightHealthShare"/>): <see cref="BossDefinition.BossRushHealth"/> where a boss has one - the
	/// Gaoler, whose catalog health is his castle chapter's now - and his catalog health otherwise.
	/// </summary>
	public static int HealthOf(BossDefinition boss) =>
		boss == null ? 0 : boss.BossRushHealth > 0 ? boss.BossRushHealth : boss.Health;

	/// <summary>
	/// Whether the save has opened the rush: every boss on its roster has fallen in his own chapter (the
	/// lifetime record), or it was open to the save before schema 21 (SaveData.BossRushGrandfathered).
	/// </summary>
	/// <remarks>
	/// It was "the Warden of the Deep has fallen", which meant the same thing while he was the road's last:
	/// nobody meets a boss here before they have beaten it where it lives. With the Gaoler behind the
	/// Emberdeep, a new save meets the Warden first, so the rule is now said in full (.ai/endgame-castle.md
	/// 1.8, 10.2 item 3): on a new save the rush opens once the castle's Dungeon falls. Every save that had
	/// it open keeps it through the schema 21 step, whatever it skipped on the way (the early chapters
	/// were passed, never fought, by saves already beyond them when they were built).
	/// </remarks>
	public static bool IsOpen(SaveData data)
	{
		if (data == null)
			return false;
		if (data.BossRushGrandfathered)
			return true;
		IReadOnlyList<BossDefinition> roster = Roster;
		return roster.Count > 0 && data.Lifetime != null && roster.All(boss => data.Lifetime.HasDefeatedBoss(boss.Id));
	}

	/// <summary>
	/// When the boss at <paramref name="order"/> (0-based) of <paramref name="count"/> arrives, on the
	/// pace clock. The last arrives at <paramref name="finalePaceSeconds"/>, the ordinary boss time.
	/// </summary>
	public static float ArrivalPaceSeconds(int order, int count, float finalePaceSeconds)
	{
		if (count <= 1 || order >= count - 1)
			return finalePaceSeconds;
		float step = (finalePaceSeconds - FirstArrivalPaceSeconds) / (count - 1);
		return FirstArrivalPaceSeconds + order * step;
	}
}
