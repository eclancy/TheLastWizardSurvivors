using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// WHICH CHAPTERS HAVE RULES OF THEIR OWN (ChapterRules, .ai/endgame-castle.md 1.9), and the one place
// that is decided. The pattern is SideEventDirector's table: a line per chapter that needs one,
// written by the plumbing as null, so each chapter's agent replaces ONE line and writes one file of its
// own - BridgeRules.cs, CourtRules.cs, ThroneRules.cs - and never edits Node2DGame.
//
// The lines are a comment apart, so two agents' edits are never on adjacent lines (what git calls a
// conflict). A chapter not listed has no rules, which is every chapter of the road. The Cursed Dungeon
// moved into the castle (C2) with its maze, its key and its portal, which are the road's own, and one rule:
// the stair at the start (DungeonRules.cs).
public static class ChapterRulesCatalog
{
	/// <summary>A new rules object for one run of the chapter, or null for a chapter with none.</summary>
	public static Func<ChapterRules> ForStage(string stageId) => stageId switch
	{
		// The Last Bridge (piece C1): the collapsing front, the spawns on the band, the Constable at the
		// gate, the gate as the way home, the strip under the timer (BridgeRules.cs).
		"stage_11" => () => new BridgeRules(),
		// (the Dungeon's line is below; leave this one as it is)
		// The Cursed Dungeon (piece C2): the stair foot in the spawn clearing, the way down from the bridge.
		"stage_1" => () => new DungeonRules(),
		// (the Court's line is below; leave this one as it is)
		// The Court (piece C3): its rooms, the boss in a room near the player, the throne room doors as the
		// way home (CourtRules.cs).
		"stage_12" => () => new CourtRules(),
		// (the Throne's line is below; leave this one as it is)
		// The Throne (piece C4): the pinned pace clock, the Muster, the procession, the throne and the doors
		// behind it as the way home (ThroneRules.cs, ThroneProcession.cs).
		"stage_13" => () => new ThroneRules(),
		// THE FAR SHORE (.ai/second-half.md 7, F0): a pre-placed null line per chapter, each its piece's to fill if
		// its chapter needs rules. Three are designed (Designed, below): the Drowned Shore's tide and the Weir's
		// sluices, the Twelfth Hour's dawn on the pace clock, the Thunder Fell's storm fronts.
		// F3: the White Downs (rules only if the barrows need placing).
		"stage_14" => null,
		// F6: the Drowned Shore - designed.
		"stage_15" => null,
		// F7: the Three Ways (rules only if the hedged roads need them).
		"stage_16" => null,
		// F8: the Twelfth Hour - designed.
		"stage_17" => null,
		// F9: the Thunder Fell - designed.
		"stage_18" => null,
		// F10: the Bronze Isle (rules only if the walled yards or Talos's rim need them).
		"stage_19" => null,
		_ => null,
	};

	/// <summary>
	/// The chapters whose rules are designed and still to be written: the validator's campaign to-do list
	/// names each until its line above is filled, and calls it a fault if the chapter is playable first.
	/// A chapter leaves this list never - filling its line is what ends the to-do.
	/// </summary>
	public static IReadOnlyList<string> Designed => DesignedIds;

	// The castle's three, and the far shore's three designed (.ai/second-half.md 7; RegressionChecks.ValidateFarShore
	// names each on its to-do line until its line above is filled).
	private static readonly string[] DesignedIds = { "stage_11", "stage_12", "stage_13", "stage_15", "stage_17", "stage_18" };

	/// <summary>True for a chapter whose rules are designed but whose line is still the null placeholder.</summary>
	public static bool IsAwaitingRules(string stageId) =>
		ForStage(stageId) == null && Array.IndexOf(DesignedIds, stageId) >= 0;
}
