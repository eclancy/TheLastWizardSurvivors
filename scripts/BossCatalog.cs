using System.Collections.Generic;
using System.Linq;
using Godot;

namespace WizardSurvivors.scripts;

/// <summary>
/// One stage's end-of-run boss. A stage without an entry here still ends at
/// <c>Node2DGame.TimerVictorySeconds</c> the old way, so stages can gain bosses one at a time.
/// </summary>
public sealed class BossDefinition
{
	/// <summary>
	/// Stable id written into <c>RunResult.BossId</c>, and matched EXACTLY by the achievement that
	/// rewards the kill. It used to be matched by substring against terms like "forest" and
	/// "castle", which meant a new boss whose id happened to contain another chapter's keyword
	/// would silently grant the wrong spell.
	/// </summary>
	public string Id { get; init; } = string.Empty;
	public string DisplayName { get; init; } = string.Empty;
	/// <summary>One line shown under the name on the arrival banner: what this boss punishes.</summary>
	public string Tagline { get; init; } = string.Empty;
	/// <summary>The boss's own scene - or, for a boss not built yet, where it will be.</summary>
	public string ScenePath { get; init; } = string.Empty;
	/// <summary>
	/// What fights in its place until <see cref="ScenePath"/> exists: another chapter's boss scene,
	/// spawned under this boss's id and name so its victory, achievement and unlock are this
	/// chapter's. Empty for a boss that was built with its chapter. The same pattern as a planned
	/// bestiary creature (StageBestiary): building the boss is creating the scene at ScenePath, and
	/// nothing else has to change for the chapter to switch to it.
	/// </summary>
	public string StandInScenePath { get; init; } = string.Empty;
	/// <summary>Health before the balance preset's elite scaling is applied.</summary>
	public int Health { get; init; } = 1000;
	/// <summary>Flat Arcane Energy added on top of the usual run reward for the kill.</summary>
	public int ArcaneVictoryBonus { get; init; } = 120;

	/// <summary>
	/// The health the Boss Rush scales this boss from, when it is not <see cref="Health"/>; 0 for "the
	/// same". For a boss whose chapter moved and was retuned for its new place while the rush, held as it
	/// was (.ai/endgame-castle.md 1.8), still meets him at the old one: the Gaoler. Read only through
	/// BossRushCatalog.HealthOf.
	/// </summary>
	public int BossRushHealth { get; init; }

	/// <summary>True while the boss's own scene does not exist and its stand-in fights instead.</summary>
	public bool IsOnStandIn =>
		!string.IsNullOrEmpty(StandInScenePath)
		&& (string.IsNullOrEmpty(ScenePath) || !ResourceLoader.Exists(ScenePath));

	/// <summary>The scene a run actually spawns: its own once it exists, the stand-in until then.</summary>
	public string SpawnScenePath => IsOnStandIn ? StandInScenePath : ScenePath;

	// There used to be an UnlocksStageId here, one per boss. Which chapter a victory opens is a fact
	// about the campaign ORDER, not about the boss, and keeping it on the boss meant that moving the
	// Cursed Dungeon was four edits that had to agree. It is BossCatalog.ChapterOpenedBy now, read
	// from StageCatalog's order (.ai/campaign.md).
}

public static class BossCatalog
{
	// Keyed by Global.SelectedStageIdx, the same index StageSelection hands to the run.
	private static readonly Dictionary<int, BossDefinition> ByStageIndex = new()
	{
		[0] = new BossDefinition
		{
			Id = "forest_treant",
			DisplayName = "Elderbark, the Treant",
			Tagline = "Slow, immense, and impossible to burst down. Bring damage you can sustain.",
			ScenePath = "res://scenes/ForestTreantBoss.tscn",
			// THE ROAD'S BOSS HEALTH follows the difficulty curve (.ai/difficulty-curve.md, section 4): the
			// first chapter's boss a little lighter, the second's heavier, and from the first wall on about
			// three times what it was - measured, a farmed build's single-target damage at 15:00 killed
			// every boss of the road within seconds of its arrival. 4200 until then.
			// 2026-10-31: every boss fight made to last three to five minutes, by phases first and health
			// second (.ai/enemy-behaviour.md, "Boss entrances and longer fights"). The value before is kept
			// on each line.
			Health = 9500, // 3800 before the fights were lengthened
			ArcaneVictoryBonus = 150
		},
		[1] = new BossDefinition
		{
			Id = "castle_warden",
			DisplayName = "The Gaoler",
			Tagline = "It charges down a lane it shows you first. Step sideways, never back.",
			ScenePath = "res://scenes/GaolerBoss.tscn",
			// Tuned as chapter 2 (5200, 170), then chapter 5 (.ai/campaign.md) at 150000 / 200 - and since
			// piece C2 the castle's eleventh chapter, the cells under his house (.ai/endgame-castle.md 4.3).
			// The bonus sits between the Constable's 360 and the Seneschal's 390; the health does not, because
			// it is set by the fight's length: he charges the player down a lane and stands in every area spell,
			// so he takes damage about twice as fast for his health as the Seneschal. Between them (420000) he
			// fell in a minute and a half; at 900000 the median farmed build takes about 2.3 minutes. The fight
			// is unchanged: the lane shown and run, dismounted at half, the enrage. Measured in
			// .ai/enemy-behaviour.md, "The Gaoler in the castle, measured".
			Health = 900000, // 420000 then 700000 measured first (fights 0.6-2.8 and 0.8-1.8 min); 150000 at chapter five, 70000 before the fights were lengthened, 6200 before the difficulty curve
			ArcaneVictoryBonus = 375, // 200 at chapter five
			// The Boss Rush is held as it was, the Gaoler fifth (1.8): it meets him at his chapter-five health.
			BossRushHealth = 150000,
		},
		[2] = new BossDefinition
		{
			Id = "cave_choir",
			DisplayName = "The Hollow Choir",
			Tagline = "Three voices. Drop one and the others sing it back - they have to fall together.",
			ScenePath = "res://scenes/HollowChoirBoss.tscn",
			// A third each. Node2DGame sets this on the voice it spawns and that voice hands the
			// same number to the two it calls up, so the fight is three times what is written here.
			// The voices now hold their places apart, so one blast no longer takes the set: at this health
			// a level 32 build took six minutes, and 18,000 let a level 39 one through in one.
			Health = 26000, // 20000 before the fights were lengthened, 1900 before the difficulty curve
			ArcaneVictoryBonus = 190
		},
		[3] = new BossDefinition
		{
			Id = "swamp_mother",
			DisplayName = "Mother Rot",
			Tagline = "She heals from everything she sheds. Clear the spawn or the bar goes backwards.",
			ScenePath = "res://scenes/MotherRotBoss.tscn",
			Health = 170000, // 80000 before the fights were lengthened, 6800 before the difficulty curve
			ArcaneVictoryBonus = 210
		},
		[4] = new BossDefinition
		{
			Id = "ruins_sentinel",
			DisplayName = "The Archivist",
			Tagline = "It never moves and its beam never stops. Keep orbiting, and watch where it surfaces.",
			ScenePath = "res://scenes/ArchivistBoss.tscn",
			Health = 140000, // 90000 before the fights were lengthened, 7200 before the difficulty curve
			ArcaneVictoryBonus = 230
		},
		[5] = new BossDefinition
		{
			Id = "frost_warden",
			DisplayName = "The Still Warden",
			Tagline = "Frozen in place, and the arena does the chasing. The safe ground is not where it is.",
			ScenePath = "res://scenes/StillWardenBoss.tscn",
			Health = 400000, // 110000 before the fights were lengthened, 7800 before the difficulty curve
			ArcaneVictoryBonus = 250
		},
		[6] = new BossDefinition
		{
			Id = "sands_coil",
			DisplayName = "The Long Coil",
			Tagline = "Only killable while it is above the sand. Bring something that spikes.",
			ScenePath = "res://scenes/LongCoilBoss.tscn",
			Health = 300000, // 130000 before the fights were lengthened, 8200 before the difficulty curve
			ArcaneVictoryBonus = 270
		},
		[7] = new BossDefinition
		{
			Id = "emberdeep_warden",
			DisplayName = "The Warden of the Deep",
			Tagline = "Every fight you have already won, in sequence, with nothing in between.",
			ScenePath = "res://scenes/DeepWardenBoss.tscn",
			Health = 500000, // 200000 before the fights were lengthened, 11000 before the difficulty curve
			ArcaneVictoryBonus = 400
		},
		[8] = new BossDefinition
		{
			Id = "kid_wizard",
			// Named for Elim, who drew him (Eric, 2026-11-01: "Wizard dude is elims drawing. The boss can be
			// named after elim").
			// Only the name changed: the id, the scene and every save key are the Wizard Dude's still.
			DisplayName = "Elim",
			Tagline = "Throws a fan of missiles down the wedge he shows you. Leave the wedge.",
			ScenePath = "res://scenes/KidWizardBoss.tscn",
			Health = 9000, // 4600 before the fights were lengthened
			// Opens nothing: the Sketchbook is outside the campaign order (StageCatalog), so
			// ChapterOpenedBy has no next chapter for it - beating it is never on the critical path.
			ArcaneVictoryBonus = 200
		},
		// THE TWO EARLY CHAPTERS' BOSSES (.ai/campaign.md). Designed, not built: each fights as
		// Elderbark under its own id and name until Phase 2 creates the scene at ScenePath, at which
		// point the chapter switches to it with no edit here. Health and bonus sit between the
		// Forest's and the Cave's, in campaign order; Phase 2 may tune them in its own block.
		[9] = new BossDefinition
		{
			Id = "fields_hayward",
			DisplayName = "The Hayward",
			Tagline = "He keeps his hurdle between you and him. Go round to his back.",
			ScenePath = "res://scenes/HaywardBoss.tscn",
			StandInScenePath = "res://scenes/ForestTreantBoss.tscn",
			Health = 10000, // 6500 before the fights were lengthened, 4500 before the difficulty curve
			ArcaneVictoryBonus = 160
		},
		[10] = new BossDefinition
		{
			Id = "heath_ninth_stone",
			DisplayName = "The Ninth Stone",
			Tagline = "It closes the circle on you. Find the gap and get out before the middle falls.",
			ScenePath = "res://scenes/NinthStoneBoss.tscn",
			StandInScenePath = "res://scenes/ForestTreantBoss.tscn",
			Health = 46000, // 20000 before the fights were lengthened, 5000 before the difficulty curve
			ArcaneVictoryBonus = 175
		},
		// THE CASTLE'S BOSSES (.ai/endgame-castle.md 3.6, 5.6, 6.4). Designed, not built: each fights as
		// the Warden of the Deep under its own id and name until its piece (C5) creates the scene at
		// ScenePath, so each chapter's deed and opening are its own from the first day. Health is the
		// doc's first guess, after the Warden's 11000; the bonus climbs from the Gaoler's castle 375
		// (4.3). Each block below is its own boss's to tune, numbers only, and none is in the Boss Rush:
		// a boss on its stand-in never joins the rush's roster (BossRushCatalog), and the rush is held to
		// the road's ten (1.8).
		[11] = new BossDefinition
		{
			Id = "bridge_constable",
			DisplayName = "The Constable",
			Tagline = "He holds the gate from the wall. Get to the gate.",
			ScenePath = "res://scenes/BridgeConstableBoss.tscn",
			StandInScenePath = "res://scenes/DeepWardenBoss.tscn",
			// From the runs (.ai/enemy-behaviour.md, "The Constable, measured"): he is in reach about two
			// thirds of the fight, and the bridge's median survivor build (L36) takes about five minutes,
			// the Warden of the Deep's length for the same class of build. 11800 was the doc's first guess.
			Health = 400000,
			ArcaneVictoryBonus = 360
		},
		[12] = new BossDefinition
		{
			Id = "court_seneschal",
			DisplayName = "The Seneschal",
			Tagline = "He leaves when it suits him. Follow.",
			ScenePath = "res://scenes/CourtSeneschalBoss.tscn",
			StandInScenePath = "res://scenes/DeepWardenBoss.tscn",
			Health = 440000, // 13000 the doc's first guess (5.6), before the fights were lengthened
			ArcaneVictoryBonus = 390
		},
		// Never named on screen (.ai/endgame-castle.md 6.4): "the dark wizard" is what he is, not a name,
		// and the banner's line is the tagline. Fought after the procession (ThroneRules, C4), not on the
		// survival clock.
		[13] = new BossDefinition
		{
			Id = "throne_dark_wizard",
			DisplayName = "The Dark Wizard",
			Tagline = "He takes what you have. Take it back.",
			ScenePath = "res://scenes/ThroneDarkWizardBoss.tscn",
			StandInScenePath = "res://scenes/DeepWardenBoss.tscn",
			// C5 (2026-11-02): at 18,000 (the doc's guess) _BalanceHarness --stage=13 --all-unlocked
			// --seek-apotheosis --immortal (the Pyromancer, seed 1234) felled him 108 s after his arrival - the
			// procession's pairs fall in 60-80 s, and the story's last fight should be its longest. 30,000 is
			// about three minutes for that build. ONE BUILD, and a bot that lets every seal run its twenty
			// seconds; a player who takes them back deals more.
			Health = 30000,
			ArcaneVictoryBonus = 450
		}
	};

	/// <summary>
	/// The chapter that beating <paramref name="definition"/> opens, read from the campaign order
	/// (StageCatalog.ChapterOpenedBy). Null for a boss whose next chapter is not boss-gated - the
	/// Long Coil, whose next is the finale - and for one outside the campaign.
	/// </summary>
	public static StageDefinition ChapterOpenedBy(BossDefinition definition)
	{
		int index = StageIndexOf(definition);
		return index < 0 ? null : StageCatalog.ChapterOpenedBy(StageCatalog.GetByIndex(index).Id);
	}

	public static BossDefinition ForStageIndex(int stageIndex)
	{
		return ByStageIndex.TryGetValue(stageIndex, out BossDefinition definition) ? definition : null;
	}

	public static BossDefinition GetById(string bossId)
	{
		if (string.IsNullOrWhiteSpace(bossId))
			return null;

		return ByStageIndex.Values.FirstOrDefault(boss => boss.Id.Equals(bossId, System.StringComparison.OrdinalIgnoreCase));
	}

	public static IEnumerable<BossDefinition> All => ByStageIndex.Values;

	/// <summary>The stage index a boss guards, for validation. -1 when the boss is not in the table.</summary>
	public static int StageIndexOf(BossDefinition definition)
	{
		foreach (KeyValuePair<int, BossDefinition> entry in ByStageIndex)
		{
			if (ReferenceEquals(entry.Value, definition))
				return entry.Key;
		}

		return -1;
	}
}
