using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

/// <summary>
/// The Throne's numbers (.ai/endgame-castle.md 6.2, 6.3): the Muster's size and the procession - who
/// comes, in which pairs, with how much health, and the breathers between. ThroneRules runs it; this is
/// the one place it is tuned, and a boss's own scene and BossCatalog block are never touched for it.
/// </summary>
/// <remarks>
/// THE PROCESSION, as decided (6.3): the household guard alone for 45 s; the Hayward and the Gaoler; a
/// breather of 20 s with a chest; Mother Rot and the Archivist; a breather with a chest; the Still Warden
/// and the Long Coil; then him. The pace clock is pinned at 15:00 throughout (ThroneRules), so the guard
/// is the end-of-run swarm from the first second. Every time here is REAL seconds: how long a moment
/// lasts, not where in a run it falls.
///
/// THE HEALTH IS THE THRONE'S, NOT THE ROAD'S. The doc's "each at half its chapter health" was written when
/// the road's bosses were 4,500-8,200; the fights have since been lengthened, chapter by chapter, to three to
/// five minutes against a build that walked the whole road to them (.ai/enemy-behaviour.md, "Boss entrances
/// and longer fights": the Hayward 10,000, the Gaoler 150,000, the Still Warden 400,000). Half of those would
/// pair a Hayward of 5,000 with a Gaoler of 75,000 - one gone in seconds, the pair's question never asked -
/// and put a 350,000 pair in front of a level-18 build. So each pair is set to FALL TOGETHER in about two
/// minutes against the build the Muster makes, with the field drained to it (GuardStandsDownForFights),
/// measured with <c>_BalanceHarness --stage=13 --all-unlocked --seek-apotheosis --immortal</c> (the THRONE
/// lines give each fall, and each boss's health every 20 s). Equal health would not fall together: the bot
/// put about 265 HP/s into the Gaoler and 50 into the Hayward behind his hurdle, 225 into Mother Rot (who
/// walks in) and 50 into the Archivist (who stands off). 2026-11-01, the Pyromancer, seed 1234, with the
/// Muster at 18: the Hayward at 79 s and the Gaoler at 98; Mother Rot at 104 and the Archivist at 135; the
/// Still Warden at 82 and the Long Coil at 95 - the procession 7.0 minutes to his arrival, levels 19 to 23.
/// A player who goes round the Hayward and walks up to the Archivist fells them sooner, which is the pairs'
/// question. ONE BUILD, NOT A RANGE: the Stormcaller (--character=3 --seed=77) was overrun by the guard in
/// the warm-up and spent 515 s on the first pair. The Throne's ChapterDifficulty row is still the
/// Emberdeep's stand-in.
///
/// HIM. Wave 4 is whatever BossCatalog[13] names - the Warden of the Deep's scene as a stand-in under the
/// dark wizard's id, until piece C5 builds ThroneDarkWizardBoss - at the catalog's health. He is C5's to tune.
/// </remarks>
public static class ThroneProcession
{
	/// <summary>Levels granted at the start (level 1 to 19), each through the ordinary screen (6.2).</summary>
	/// <remarks>
	/// 6.2 decided seventeen, "moved by measurement" (Eric's decision 8) until the Apotheosis lands in the
	/// third wave or later (6.6). With the guard standing down for fights, seventeen left the planned build at
	/// level 22 when he fell (his stand-in, 18,000 health) - the form never came; one run in which the guard
	/// leaked into the fights put it 20 s into his. Eighteen puts it 56 s into the third wave (level 23 at 6.2
	/// minutes). The Ascension (player level 17, a spell at 8) is still taken inside the Muster by a planned
	/// build. <c>--throne-muster=N</c> measures another size.
	/// </remarks>
	public const int MusterLevels = 18;

	/// <summary>Relic chests opened after the Muster's last screen, through the chest screen (6.2).</summary>
	public const int MusterRelicChests = 2;

	/// <summary>Wave 0: the household guard alone, from the end of the Muster.</summary>
	public const float WarmUpSeconds = 45f;

	/// <summary>Between the first two pairs and the next: no boss, and a chest where the last one fell.</summary>
	public const float BreatherSeconds = 20f;

	/// <summary>
	/// Of a breather, the first seconds with no trickle: the walk to its chest, as the Boss Rush gives
	/// (Node2DGame.BossRushBreatherSeconds).
	/// </summary>
	public const float BreatherTrickleHoldSeconds = 6f;

	/// <summary>
	/// After the third pair, before him: no breather (6.3), only long enough for the last warden's fall to
	/// play out before his arrival holds the field.
	/// </summary>
	public const float FinaleBeatSeconds = 4f;

	/// <summary>
	/// While a pair (or he) stands, no new guard arrives - no trickle, formation or swarm surge - and the
	/// field drains down to the fight, as it does for every boss in the game (Node2DGame's boss phase, the
	/// Boss Rush). The guard comes in the warm-up and the breathers.
	/// </summary>
	/// <remarks>
	/// MEASURED, NOT ASSUMED (6.6 assumed the guard keeps coming). With the end-of-run guard arriving
	/// throughout, a level-18 build's spells - nearly all aimed at whatever is nearest - went to the swarm:
	/// _BalanceHarness put about 110 HP/s into Mother Rot, who walks to the player, and about 14 HP/s into
	/// the Archivist, who stands off. The Archivist then needs about 2,000 health to fall in two minutes,
	/// which a player who walked up to it would erase in seconds; no single number serves both. Draining
	/// the field puts the build on the wardens, keeps each pair's two demands readable in a closed room,
	/// and leaves the Apotheosis's XP to the warm-up, the breathers and the drained swarm.
	/// </remarks>
	public static readonly bool GuardStandsDownForFights = true;

	/// <summary>The stage whose boss is him (BossCatalog).</summary>
	public const int FinaleStageIndex = 13;

	/// <summary>One warden called home: its boss (BossCatalog id) and the health it brings to the Throne.</summary>
	public sealed class Warden
	{
		public Warden(string bossId, int health)
		{
			BossId = bossId;
			Health = health;
		}

		public string BossId { get; }

		/// <summary>Before the balance preset, which every boss rides (Node2DGame.SendBoss).</summary>
		public int Health { get; }
	}

	/// <summary>One wave of the procession.</summary>
	public sealed class Wave
	{
		/// <summary>The doc's number: 1-3 the pairs, 4 him.</summary>
		public int Number { get; init; }

		public IReadOnlyList<Warden> Wardens { get; init; } = System.Array.Empty<Warden>();

		/// <summary>A breather of <see cref="BreatherSeconds"/> with a chest follows this wave's fall.</summary>
		public bool BreatherAfter { get; init; }

		/// <summary>Him: his fall wins the run. His health is BossCatalog's.</summary>
		public bool Finale { get; init; }
	}

	/// <summary>The procession, in order. Each pair is two demands that pull against each other (6.3).</summary>
	public static readonly IReadOnlyList<Wave> Waves = new[]
	{
		// Go round one while you sidestep the other's lane.
		new Wave
		{
			Number = 1,
			Wardens = new[] { new Warden("fields_hayward", 9000), new Warden("castle_warden", 30000) },
			BreatherAfter = true,
		},
		// Kill what heals one while the other's sweep moves you.
		new Wave
		{
			Number = 2,
			Wardens = new[] { new Warden("swamp_mother", 30000), new Warden("ruins_sentinel", 12000) },
			BreatherAfter = true,
		},
		// Hold ground, and burst a window: the two demands at odds.
		new Wave
		{
			Number = 3,
			Wardens = new[] { new Warden("frost_warden", 17000), new Warden("sands_coil", 17000) },
		},
		new Wave { Number = 4, Finale = true },
	};
}
