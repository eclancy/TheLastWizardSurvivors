using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>How a chapter is opened.</summary>
public enum StageGate
{
	/// <summary>Always available. Somewhere to play on a fresh save.</summary>
	Open,
	/// <summary>Opened by defeating the previous chapter's boss.</summary>
	PreviousBoss,
	/// <summary>
	/// A gate chapter: opened by defeating the previous chapter's boss AND freeing every wizard whose
	/// rescue comes before this chapter (GlobalStatsManager.GateWizardIds) - written per chapter, so the
	/// Emberdeep counts the wizards on the road and a later gate (the castle's Throne) counts those freed
	/// before it (.ai/achievements-and-secrets.md 2.5, .ai/endgame-castle.md 1.6). It replaced
	/// CampaignComplete, "every spell and every wizard", at schema 19: spells are earned in secrets now,
	/// a secret is missable, and the old gate could not even be met - Fireball was paid only behind it.
	/// The boss alone never writes the chapter open (StageCatalog.ChapterOpenedBy skips it).
	/// </summary>
	PreviousBossAndWizards,
	/// <summary>
	/// The far shore's first chapter (.ai/second-half.md 6.1): opened by Taken Back (the Throne won), The
	/// Grimoire Restored (every spell recovered) and every wizard a place on the road holds, Merlin and Laelaps
	/// among them - never the Dark Wizard (a difficulty) or John Dee (a grind). It stands in the far shore's own
	/// order (StageCatalog.FarShore), never in the campaign's, so it has no previous chapter: its gate is the
	/// whole road behind it, read by GlobalStatsManager.IsFarShoreGateMet. Every later far chapter is
	/// <see cref="PreviousBoss"/> in the far shore's order.
	/// </summary>
	FarShore,
}

public sealed class StageDefinition
{
	/// <summary>Stable id, "stage_0" upward. Matches <c>RunResult.StageId</c> and the save's unlocked list.</summary>
	public string Id { get; init; } = string.Empty;
	/// <summary>Index into the roster. Equals <c>Global.SelectedStageIdx</c>.</summary>
	public int Index { get; init; }
	public string DisplayName { get; init; } = string.Empty;
	public string TerrainCategory { get; init; } = string.Empty;
	/// <summary>What the place is - terrain and mood, the line the run itself opens on.</summary>
	public string FlavorText { get; init; } = string.Empty;
	/// <summary>
	/// What the dark wizard has done to it. Every chapter is somewhere he holds a prisoner
	/// (.ai/world-and-tone.md), and the stage list is the only screen where that is ever said out
	/// loud - without it a chapter reads as a texture swap rather than a place worth taking back.
	/// It is the card's only line under the name, so keep it to one short sentence (under ten words).
	/// </summary>
	public string CorruptionText { get; init; } = string.Empty;
	public StageEnvironmentKind EnvironmentKind { get; init; }
	public StageGate Gate { get; init; }
	/// <summary>
	/// Why the chapter is closed, in one line. Derived from the campaign order rather than written
	/// per chapter: it used to be a literal on each entry ("Locked — clear the Cursed Dungeon"),
	/// which is a statement about the ORDER, and the first time the order moved every one of them
	/// would have named the wrong chapter.
	/// </summary>
	public string LockedHint => StageCatalog.LockedHintFor(this);
	/// <summary>False while a chapter has no boss or content yet - it is listed, but not enterable.</summary>
	public bool IsPlayable { get; init; } = true;
	/// <summary>
	/// A chapter outside the campaign that is reached only once the campaign is finished
	/// (<see cref="GlobalStatsManager.HasFinishedCampaign"/>): off the world map, on a page of its own
	/// (.ai/world-map.md, section 4). The Sketchbook. Its <see cref="Gate"/> stays Open - nothing in
	/// the road opens it - and <see cref="GlobalStatsManager.IsStageEarned"/> adds this rule.
	/// </summary>
	public bool AfterCampaign { get; init; }
	/// <summary>
	/// How long the run is: real seconds until the survival clock stops and the boss walks in. Every road
	/// chapter is <see cref="StageCatalog.RoadRunSeconds"/>, the far shore's are
	/// <see cref="StageCatalog.FarShoreRunSeconds"/>. Read it through <see cref="StageCatalog.VictorySecondsFor"/>,
	/// never off a definition and never as a literal: the run (Node2DGame.TimerVictorySeconds), the pace clock and
	/// the XP curve that follows it (.ai/run-pacing.md section 12) all take it from there.
	/// </summary>
	public float RunSeconds { get; init; } = StageCatalog.RoadRunSeconds;
}

/// <summary>
/// The campaign's chapters, and the single authority on what a stage is called.
/// </summary>
/// <remarks>
/// Stage naming was previously hardcoded in four places that had already drifted apart:
/// <c>StageSelection</c> knew two stages, <c>GameOverScreen.StageDisplayName</c> knew three,
/// <c>Node2DGame</c> had ten names and ten flavour strings, and
/// <c>StageEnvironmentCatalog.GetForStageIndex</c> mapped ten indices onto seven environments with
/// no relation to any of them. All of those become readers of this list.
///
/// The roster is a list, not a fixed set: post-final bonus chapters append, and nothing here
/// assumes eight.
/// </remarks>
public static class StageCatalog
{
	/// <summary>A road chapter's run, and the reference run every pacing number was tuned on (Node2DGame.ReferenceRunSeconds): fifteen minutes.</summary>
	public const float RoadRunSeconds = 900f;

	/// <summary>
	/// A far-shore chapter's run: twenty minutes, the boss at 20:00. Eric, 2026-11-08, after the far sky's element
	/// waiver: "okay lets let the waiver land ... still make the far shore longer". The half is "excuses to keep trying
	/// new spell combinations on new maps", so the longer night is more time with the combination once it is made -
	/// not an earlier one: the pace clock stretches with it, and the XP curve with that, so a far-shore run reaches every
	/// moment of the road's at the same share of its length (.ai/run-pacing.md section 12). Every chapter in
	/// FarShoreOrder sets it; RegressionChecks.ValidateRunLengths holds that, and the road at RoadRunSeconds.
	/// </summary>
	public const float FarShoreRunSeconds = 1200f;

	/// <summary>
	/// THE run length: real seconds until the survival clock stops and the boss arrives, for this chapter. The one
	/// accessor everything on a run's path reads (Node2DGame.ResolveRunLength, the XP curve, the tools); a stage id it
	/// does not know is a road run.
	/// </summary>
	public static float VictorySecondsFor(string stageId) => GetById(stageId)?.RunSeconds ?? RoadRunSeconds;

	private static readonly StageDefinition[] Stages =
	{
		new()
		{
			Id = "stage_0", Index = 0,
			DisplayName = "Enchanted Forest",
			TerrainCategory = "Forest path",
			FlavorText = "A bright woodland trail where ancient trees and thick brush crowd the battlefield.",
			CorruptionText = "Elderbark's wood. Every path now leads inward.",
			EnvironmentKind = StageEnvironmentKind.Forest,
			Gate = StageGate.Open,
		},
		// Since piece C2 (.ai/endgame-castle.md 1.3, section 4) the cells under his castle: the castle's second
		// chapter, between the Last Bridge and the Court, by its line in CampaignOrder. Its id stays stage_1 -
		// a chapter moves by its place in the order, never by renumbering. The corruption line is unchanged
		// and stronger here: they are his own cells now.
		new()
		{
			Id = "stage_1", Index = 1,
			DisplayName = "Cursed Dungeon",
			TerrainCategory = "Dungeon stone",
			FlavorText = "The cells under his castle: stone corridors, and a wizard behind one of the doors.",
			CorruptionText = "He keeps a wizard in one of these cells.",
			EnvironmentKind = StageEnvironmentKind.Castle,
			Gate = StageGate.PreviousBoss,
		},
		// Every chapter below now has a boss in BossCatalog, which is what IsPlayable was waiting
		// on: a chapter that ends on the timer with nothing to kill is a free non-victory, and
		// that was the reason these were listed but closed. What they still share is one tile set
		// and one enemy roster per environment - the fights differ, the ground does not yet.
		new()
		{
			Id = "stage_2", Index = 2,
			DisplayName = "Sunken Cave",
			TerrainCategory = "Wet stone",
			FlavorText = "Dripping tunnels of black rock, where the only light is the one you brought.",
			CorruptionText = "His shadow followed the water down into the dark.",
			EnvironmentKind = StageEnvironmentKind.Cave,
			Gate = StageGate.PreviousBoss,
		},
		new()
		{
			Id = "stage_3", Index = 3,
			DisplayName = "Blighted Swamp",
			TerrainCategory = "Bog and reed",
			FlavorText = "Reeds whisper over murky water while bog lanterns glow through the fog.",
			CorruptionText = "The lanterns still burn. Nothing living lights them.",
			EnvironmentKind = StageEnvironmentKind.Swamp,
			Gate = StageGate.PreviousBoss,
		},
		new()
		{
			Id = "stage_4", Index = 4,
			DisplayName = "Mystic Ruins",
			TerrainCategory = "Shattered stone",
			FlavorText = "Broken spires and shattered walls form a harsh field of rubble and ancient danger.",
			CorruptionText = "The old wards were broken from the inside.",
			EnvironmentKind = StageEnvironmentKind.Ruins,
			Gate = StageGate.PreviousBoss,
		},
		new()
		{
			Id = "stage_5", Index = 5,
			DisplayName = "Frozen Waste",
			TerrainCategory = "Ice field",
			FlavorText = "Ice-slick ground and wind-carved ridges make every step a balancing act.",
			CorruptionText = "A wizard lies under the ice, and the cold stands guard.",
			EnvironmentKind = StageEnvironmentKind.Ice,
			Gate = StageGate.PreviousBoss,
		},
		new()
		{
			Id = "stage_6", Index = 6,
			DisplayName = "Scorched Sands",
			TerrainCategory = "Desert waste",
			FlavorText = "A blistering desert expanse of cracked earth and long shadows.",
			CorruptionText = "He hid the water. The dead still walk the trade road.",
			EnvironmentKind = StageEnvironmentKind.Desert,
			Gate = StageGate.PreviousBoss,
		},
		new()
		{
			Id = "stage_7", Index = 7,
			DisplayName = "The Emberdeep",
			TerrainCategory = "Volcanic rock",
			FlavorText = "Blackened ground and glowing embers mark the last road, and what waits at its end.",
			// "His seat" until the castle was drawn across the water (.ai/endgame-castle.md 1.4, 8): the world
			// map's one dark light moved to his tower, and the summit keeps the forge's fire.
			CorruptionText = "His forge, at the end of the road.",
			EnvironmentKind = StageEnvironmentKind.Volcanic,
			// The Long Coil beaten and every wizard on the road freed (since schema 19).
			Gate = StageGate.PreviousBossAndWizards,
		},
		// THE ONE CHAPTER OUTSIDE THE CAMPAIGN, and the reason the roster is a list rather than
		// a fixed eight. It gates nothing, so it can sit after the finale in the roster without
		// implying it comes after it in the story. It was open from the first run; since 2026-10-30
		// it is reached once the campaign is finished, on its own page off the world map (Eric:
		// "Once you beat the whole campaign, you should be able to navigate to it, but it shows up as
		// its own separate map"). A save that has already played it keeps it (IsStageEarned).
		//
		// Every sprite in it - eight enemies, a boss and its projectile - was drawn by Eric's
		// kids. It has no CorruptionText because there is nothing here the dark wizard did: the
		// field exists to say what he has taken from a place, and he has not been to this one.
		new()
		{
			Id = "stage_8", Index = 8,
			DisplayName = "The Sketchbook",
			TerrainCategory = "Squared paper",
			FlavorText = "A world drawn in a workbook, loose.",
			EnvironmentKind = StageEnvironmentKind.Sketchbook,
			Gate = StageGate.Open,
			AfterCampaign = true,
		},
		// THE TWO CHAPTERS THAT OPEN EARLY (.ai/campaign.md). Eric, 2026-10-24: "cursed dungeon
		// should be a later level. there should be another few levels that open up earlier." They
		// sit second and third in CampaignOrder below, between the Forest and the Cave, and their
		// ids are new rather than renumbered: stage ids are in every save and every unlock rule, so
		// a chapter appends here and takes its place in the campaign through the order alone.
		//
		// Not playable until Phase 2 builds them. A closed chapter is skipped by the order - the
		// Forest's boss opens the Cave until these exist - so nobody can be stopped by one, and
		// flipping IsPlayable is the LAST step of building each, not the first.
		new()
		{
			Id = "stage_9", Index = 9,
			DisplayName = "The Fallow Fields",
			TerrainCategory = "Stubble and hedgerow",
			FlavorText = "Open fields gone to stubble and thistle between black hedgerows, and a well in the middle of them.",
			CorruptionText = "He takes a tithe of everything that grows here.",
			EnvironmentKind = StageEnvironmentKind.Fields,
			Gate = StageGate.PreviousBoss,
		},
		new()
		{
			Id = "stage_10", Index = 10,
			DisplayName = "The Broken Circle",
			TerrainCategory = "Heath and standing stone",
			FlavorText = "A high heath of heather and granite, ringed by stones that are older than the wizards.",
			CorruptionText = "The circle where the wizards met. He came here first.",
			EnvironmentKind = StageEnvironmentKind.Heath,
			Gate = StageGate.PreviousBoss,
		},
		// THE CASTLE (.ai/endgame-castle.md): his seat, across the water from the end of the road. Three
		// chapters after the Emberdeep - the Last Bridge, the Court and the Throne - with the Cursed
		// Dungeon moved in between the first two by piece C2 (its line in CampaignOrder, not here). New
		// ids, appended, for the reason the early chapters' were (.ai/campaign.md section 3).
		//
		// Not playable until each is built (section 9). A closed chapter is skipped by the order, so the
		// Warden of the Deep opens nothing until the bridge exists and nobody is stopped by one. Each
		// entry below is its own chapter's: its builder flips IsPlayable last (one line), and the
		// validator's campaign to-do line for it must be gone first.
		new()
		{
			Id = "stage_11", Index = 11,
			DisplayName = "The Last Bridge",
			TerrainCategory = "Bridge deck",
			FlavorText = "A long stone bridge across the dark sea to the castle on its rock, and it falls in behind you.",
			CorruptionText = "His seat is across the water.",
			EnvironmentKind = StageEnvironmentKind.Bridge,
			Gate = StageGate.PreviousBoss,
			// Flipped 2026-11-02 (Eric: "Yes, flip it"), with C2 and C8 merged as 9.2 step 6 asks.
			IsPlayable = true,
		},
		new()
		{
			Id = "stage_12", Index = 12,
			DisplayName = "The Court",
			TerrainCategory = "Castle halls",
			FlavorText = "The halls of his house: galleries, a great hall laid for a feast, and rooms behind rooms.",
			CorruptionText = "A court with no one left in it.",
			EnvironmentKind = StageEnvironmentKind.Court,
			Gate = StageGate.PreviousBoss,
			// Flipped 2026-11-02 with the Throne (Eric: "You can switch all of that on").
			IsPlayable = true,
		},
		new()
		{
			Id = "stage_13", Index = 13,
			DisplayName = "The Throne",
			TerrainCategory = "Throne hall",
			FlavorText = "One hall of black stone, its doors shut behind you, and the throne at its end.",
			CorruptionText = "He is here.",
			EnvironmentKind = StageEnvironmentKind.Throne,
			// The second gate chapter (.ai/endgame-castle.md 1.6): the Seneschal beaten AND every wizard
			// freed. "Every wizard whose rescue comes before this chapter" (GlobalStatsManager.GateWizardIds)
			// is every wizard here, Tituba included, so "he is reachable only when every wizard is free" is
			// true exactly where he is.
			Gate = StageGate.PreviousBossAndWizards,
			// Flipped 2026-11-02 with the Court (Eric: "You can switch all of that on").
			IsPlayable = true,
		},
		// THE FAR SHORE (.ai/second-half.md; Eric accepted section 8 on 2026-11-08, the half named "The Far Shore").
		// Six gods' seats across the dark sea, east of his rock: new ids, appended, never renumbered (campaign.md 3).
		// They stand in FarShoreOrder below, NEVER in CampaignOrder - appending them there would add six Hardcore
		// clears to the Dark Wizard's deed and to The Road Restored, and hold the Sketchbook shut until the far
		// shore was done (section 7). Built by F0 as stand-ins: not playable, so no save, screen or run reaches
		// them, and each borrows another chapter's ground (EnvironmentKind) until its own piece gives it one.
		//
		// EACH ENTRY IS ITS OWN PIECE'S (section 9): F3 the White Downs, F6 the Drowned Shore, F7 the Three Ways,
		// F8 the Twelfth Hour, F9 the Thunder Fell, F10 the Bronze Isle. A piece edits only its own block - its
		// EnvironmentKind once its ground exists, and IsPlayable = true LAST, once RegressionChecks' far shore
		// to-do line for it is gone. The texts are section 4.2's; the corruption line is its column.
		new()
		{
			// F3's block.
			Id = "stage_14", Index = 14,
			DisplayName = "The White Downs",
			TerrainCategory = "Chalk downland",
			FlavorText = "Chalk downland at night: white turf, flint and barrows, and a hound and a hart cut in the hills.",
			CorruptionText = "No moon has risen here since he came.",
			// Its own ground (F3): cropped turf on chalk, flint, and the barrows standing on it as walls.
			EnvironmentKind = StageEnvironmentKind.WhiteDowns,
			Gate = StageGate.FarShore,
			RunSeconds = FarShoreRunSeconds,
			// Playable 2026-11-08, its to-do line gone once F2 (Artemis) and F5 (its place on the map) merged; the far
			// shore's gate still keeps it shut to any save that has not taken the Throne back.
			IsPlayable = true,
		},
		new()
		{
			// F6's block.
			Id = "stage_15", Index = 15,
			DisplayName = "The Drowned Shore",
			TerrainCategory = "Tide sand and channel",
			FlavorText = "The seabed the tide left: ribbed wet sand, weed and wrecks, and the sea standing off at the horizon.",
			CorruptionText = "The sea was told to stay out.",
			// Stand-in ground: the Swamp's standing water and reed.
			EnvironmentKind = StageEnvironmentKind.Swamp,
			Gate = StageGate.PreviousBoss,
			RunSeconds = FarShoreRunSeconds,
			IsPlayable = false,
		},
		new()
		{
			// F7's block.
			Id = "stage_16", Index = 16,
			DisplayName = "The Three Ways",
			TerrainCategory = "Hedged roads",
			FlavorText = "A dry night country of roads that fork in three, yew hedge between them and a cold torch at every fork.",
			CorruptionText = "Every torch at every crossing is out.",
			// Stand-in ground: the Forest's, for its hedges.
			EnvironmentKind = StageEnvironmentKind.Forest,
			Gate = StageGate.PreviousBoss,
			RunSeconds = FarShoreRunSeconds,
			IsPlayable = false,
		},
		new()
		{
			// F8's block.
			Id = "stage_17", Index = 17,
			DisplayName = "The Twelfth Hour",
			TerrainCategory = "Black sand avenue",
			FlavorText = "An avenue of sphinxes and pylon gates on black sand beside a still river, in a night that has not ended.",
			CorruptionText = "The sun did not rise.",
			// Stand-in ground: the Sands'. Its darkness and the dawn on the pace clock are F8's (section 7).
			EnvironmentKind = StageEnvironmentKind.Desert,
			Gate = StageGate.PreviousBoss,
			RunSeconds = FarShoreRunSeconds,
			IsPlayable = false,
		},
		new()
		{
			// F9's block.
			Id = "stage_18", Index = 18,
			DisplayName = "The Thunder Fell",
			TerrainCategory = "Bare granite fell",
			FlavorText = "Bare granite above the treeline, cairns and lightning-split pines, under a storm that never breaks.",
			CorruptionText = "The storm was told never to break.",
			// Stand-in ground: the Frozen Waste's, high and bare.
			EnvironmentKind = StageEnvironmentKind.Ice,
			Gate = StageGate.PreviousBoss,
			RunSeconds = FarShoreRunSeconds,
			IsPlayable = false,
		},
		new()
		{
			// F10's block.
			Id = "stage_19", Index = 19,
			DisplayName = "The Bronze Isle",
			TerrainCategory = "Bronze yards",
			FlavorText = "A god's workshop island gone cold: bronze yards in verdigris, anvils as tall as houses, dead bellows.",
			CorruptionText = "The god's forge is cold.",
			// Stand-in ground: the Cursed Dungeon's walled lattice, for the walled yards. No lava, ever: the
			// Emberdeep is his forge, this is a god's put out (section 4.2).
			EnvironmentKind = StageEnvironmentKind.Castle,
			Gate = StageGate.PreviousBoss,
			RunSeconds = FarShoreRunSeconds,
			IsPlayable = false,
		},
	};

	// THE CAMPAIGN, IN THE ORDER IT IS PLAYED - and the only place that order is written.
	//
	// It is deliberately a list of ids and not the order of the roster above. Stage ids and their
	// indices are in every save (UnlockedStageIds, BestTimeByStage, the run log), in the unlock
	// rules (UnlockCatalog's secret reveals) and in every harness flag (--stage=N), so a chapter can
	// never be renumbered to move it. Moving one is editing this list and nothing else: which boss
	// opens which chapter, the locked hint, the stage select's order and the deeds' order are all
	// read from it.
	//
	// The road is eight chapters before the Emberdeep, the first gate chapter (PreviousBossAndWizards):
	// its previous boss AND the wizards freed before it. The castle comes after it (.ai/endgame-castle.md),
	// boss-gated, and ends on a second gate chapter, the Throne; the last chapter is always a gate.
	// Chapters outside the campaign (the Sketchbook) are not listed; they must be Open.
	//
	// THE CURSED DUNGEON MOVED INTO THE CASTLE (piece C2, .ai/endgame-castle.md 1.3-1.8). It was fifth,
	// after four open chapters (.ai/campaign.md); it is the castle's second chapter now, the cells under
	// his house, and nothing replaces it at fifth place (1.7). Saves that had reached it keep it
	// grandfathered - SaveData.GrandfatheredStageIds, schema 21 - which lets them in and is evidence of
	// nothing on the road (GlobalStatsManager.IsStageEarned). Until the bridge is playable the Warden of
	// the Deep opens it directly, since a closed chapter is skipped.
	//
	// The castle's lines are skipped until each chapter is playable, and kept a line apart so that each
	// piece's edit lands on a line of its own.
	private static readonly string[] CampaignOrder =
	{
		"stage_0",  // 1  Enchanted Forest - Elderbark
		"stage_9",  // 2  The Fallow Fields - the Hayward
		"stage_10", // 3  The Broken Circle - the Ninth Stone
		"stage_2",  // 4  Sunken Cave - the Hollow Choir
		"stage_3",  // 5  Blighted Swamp - Mother Rot
		"stage_4",  // 6  Mystic Ruins - the Archivist
		"stage_5",  // 7  Frozen Waste - the Still Warden
		"stage_6",  // 8  Scorched Sands - the Long Coil
		"stage_7",  // 9  The Emberdeep - the Warden of the Deep (the Long Coil and every wizard on the road first)
		"stage_11", // 10 The Last Bridge - the Constable (castle)
		"stage_1",  // 11 Cursed Dungeon - the Gaoler (castle; it was fifth until C2)
		"stage_12", // 12 The Court - the Seneschal (castle, not yet playable)
		"stage_13", // 13 The Throne - the procession, then him (the Seneschal and every wizard first; not yet playable)
	};

	// THE FAR SHORE, IN THE ORDER IT IS PLAYED (.ai/second-half.md 4.2, 7) - the second order, and the only place
	// it is written. Kept apart from CampaignOrder on purpose: GlobalStatsManager.ChaptersClearedAt counts every
	// chapter of the Campaign for the Dark Wizard's Hardcore deed and The Road Restored, and LastPlayableChapter
	// (what opens the Sketchbook) is the campaign's, so neither moves when the far shore grows. RegressionChecks'
	// ValidateFarShore holds that no id is in both.
	//
	// The first is gated FarShore (Taken Back, every spell, every wizard a place holds); every later one opens on
	// the boss of the playable far chapter before it - PreviousChapter and ChapterOpenedBy read the order a
	// chapter is in, and skip an unbuilt one, as the campaign's do. A line a chapter, each its piece's alone.
	private static readonly string[] FarShoreOrder =
	{
		"stage_14", // 1 The White Downs - the Unstrung Hunter; Artemis (F3 the chapter, F2 the god)
		"stage_15", // 2 The Drowned Shore - the Weir; Manannan mac Lir (F6)
		"stage_16", // 3 The Three Ways - the Three-Faced; Hecate (F7)
		"stage_17", // 4 The Twelfth Hour - the Eclipse; Ra (F8)
		"stage_18", // 5 The Thunder Fell - the Still Eye; Ukko (F9)
		"stage_19", // 6 The Bronze Isle - Talos; Hephaestus (F10)
	};

	private static StageDefinition[] campaign;
	private static StageDefinition[] farShore;

	public static IReadOnlyList<StageDefinition> All => Stages;

	public static int Count => Stages.Length;

	/// <summary>The campaign's chapters in the order they are played. Excludes chapters outside it.</summary>
	public static IReadOnlyList<StageDefinition> Campaign =>
		campaign ??= CampaignOrder.Select(GetById).Where(s => s != null).ToArray();

	/// <summary>The raw order, ids as written, for validation - including any id that names no stage.</summary>
	public static IReadOnlyList<string> CampaignOrderIds => CampaignOrder;

	/// <summary>The far shore's chapters in the order they are played (.ai/second-half.md 4.2). Never in <see cref="Campaign"/>.</summary>
	public static IReadOnlyList<StageDefinition> FarShore =>
		farShore ??= FarShoreOrder.Select(GetById).Where(s => s != null).ToArray();

	/// <summary>The far shore's raw order, ids as written, for validation.</summary>
	public static IReadOnlyList<string> FarShoreOrderIds => FarShoreOrder;

	/// <summary>
	/// Every stage in the order the stage select lists them: the campaign as it is played, then the far
	/// shore as it is played, then the chapters outside both in roster order.
	/// </summary>
	public static IEnumerable<StageDefinition> InDisplayOrder =>
		Campaign.Concat(FarShore).Concat(Stages.Where(s => !IsInEitherOrder(s)));

	/// <summary>0-based position on the far shore, or -1 for a chapter not on it.</summary>
	public static int FarShorePositionOf(string stageId) => PositionIn(FarShore, stageId);

	public static bool IsOnFarShore(StageDefinition stage) => stage != null && FarShorePositionOf(stage.Id) >= 0;

	/// <summary>A chapter of either order - the campaign or the far shore - rather than one outside both (the Sketchbook).</summary>
	public static bool IsInEitherOrder(StageDefinition stage) => IsInCampaign(stage) || IsOnFarShore(stage);

	/// <summary>
	/// 0-based position across both orders: the campaign's chapters, then the far shore's after them. -1 for a
	/// chapter outside both. For whatever lists every chapter in the order it is reached (the quest chain, the
	/// deeds page) - never for a gate, which reads the order its own chapter is in.
	/// </summary>
	public static int OrderedPositionOf(string stageId)
	{
		int position = CampaignPositionOf(stageId);
		if (position >= 0)
			return position;
		int far = FarShorePositionOf(stageId);
		return far >= 0 ? Campaign.Count + far : -1;
	}

	// The order a chapter stands in, for the two rules that walk it (PreviousChapter, ChapterOpenedBy): the
	// campaign's, the far shore's, or none.
	private static IReadOnlyList<StageDefinition> OrderOf(string stageId) =>
		CampaignPositionOf(stageId) >= 0 ? Campaign
		: FarShorePositionOf(stageId) >= 0 ? FarShore
		: Array.Empty<StageDefinition>();

	private static int PositionIn(IReadOnlyList<StageDefinition> order, string stageId)
	{
		if (string.IsNullOrWhiteSpace(stageId))
			return -1;
		for (int i = 0; i < order.Count; i++)
		{
			if (order[i].Id.Equals(stageId.Trim(), StringComparison.OrdinalIgnoreCase))
				return i;
		}
		return -1;
	}

	/// <summary>0-based position in the campaign, or -1 for a chapter outside it (the far shore's included).</summary>
	public static int CampaignPositionOf(string stageId) => PositionIn(Campaign, stageId);

	public static bool IsInCampaign(StageDefinition stage) => stage != null && CampaignPositionOf(stage.Id) >= 0;

	/// <summary>What the world map's card says over a chapter that is not on the road (the Sketchbook).</summary>
	public const string OffRoadPlace = "Beyond the map";

	/// <summary>
	/// Where a chapter is, in the words the world map's card uses over its name (WorldMap.RebuildCard): "Chapter N"
	/// by its place on the road (CampaignOrder), "Far Shore N" by its place across the water (FarShoreOrder), or
	/// <see cref="OffRoadPlace"/> for one outside both. The pause screen and the results numbered chapters by their
	/// roster index, so the Fallow Fields, second on the road, called itself Chapter 10 under a map that said
	/// "Chapter 2 of 10".
	/// </summary>
	public static string ChapterPlace(StageDefinition stage)
	{
		int position = stage == null ? -1 : CampaignPositionOf(stage.Id);
		if (position >= 0)
			return $"Chapter {position + 1}";
		int far = stage == null ? -1 : FarShorePositionOf(stage.Id);
		return far >= 0 ? $"Far Shore {far + 1}" : OffRoadPlace;
	}

	/// <summary>The campaign's last chapter - the one his seat is in.</summary>
	public static StageDefinition FinalChapter => Campaign.Count > 0 ? Campaign[Campaign.Count - 1] : null;

	/// <summary>
	/// The last chapter of the campaign that can be played: its boss beaten is the campaign finished
	/// (<see cref="GlobalStatsManager.HasFinishedCampaign"/>). The Emberdeep today; the Throne once the
	/// castle is built. Not <see cref="FinalChapter"/>, which names the Throne already, unbuilt.
	/// </summary>
	public static StageDefinition LastPlayableChapter => Campaign.LastOrDefault(s => s.IsPlayable);

	/// <summary>
	/// Open from a new save's first run: gated Open and not held back until the campaign is finished
	/// (<see cref="StageDefinition.AfterCampaign"/>). What the gate validators mean by "reachable before
	/// any gate" for a chapter outside the campaign.
	/// </summary>
	public static bool IsOpenFromStart(StageDefinition stage) =>
		stage != null && stage.Gate == StageGate.Open && !stage.AfterCampaign;

	/// <summary>
	/// The first gate chapter in the campaign (<see cref="StageGate.PreviousBossAndWizards"/>): the
	/// Emberdeep. "The road" is everything before it. Not <see cref="FinalChapter"/>, which stays "last"
	/// once the castle's chapters come after the gate (.ai/endgame-castle.md 1.9).
	/// </summary>
	public static StageDefinition GateChapter => Campaign.FirstOrDefault(s => s.Gate == StageGate.PreviousBossAndWizards);

	/// <summary>Every gate chapter in the campaign, in order: the Emberdeep today, the Throne later.</summary>
	public static IEnumerable<StageDefinition> GateChapters => Campaign.Where(s => s.Gate == StageGate.PreviousBossAndWizards);

	/// <summary>
	/// The last gate chapter in the campaign: the Throne, where he is. The only gate that counts a chapter
	/// wizard (<see cref="UnlockDefinition.CountedAtFinalGateOnly"/>, .ai/chapter-wizards.md G2).
	/// </summary>
	public static StageDefinition FinalGateChapter => Campaign.LastOrDefault(s => s.Gate == StageGate.PreviousBossAndWizards);

	/// <summary>True for a gate whose previous chapter's boss is part of it.</summary>
	public static bool IsBossGated(StageGate gate) => gate == StageGate.PreviousBoss || gate == StageGate.PreviousBossAndWizards;

	/// <summary>
	/// The chapter whose boss opens <paramref name="stage"/> (or, for a gate chapter, is half its gate):
	/// the nearest PLAYABLE chapter before it in the order it stands in - the campaign's, or the far shore's
	/// (.ai/second-half.md 7). Null for the first chapter of either order (the far shore's first is opened by
	/// the whole road, <see cref="StageGate.FarShore"/>, not by a boss), for a chapter outside both, and for one
	/// that is not boss-gated. Never a chapter of the other order.
	/// </summary>
	/// <remarks>
	/// Playable, because a chapter that is not built yet has no boss anyone can beat. Skipping it is
	/// what lets the two new early chapters sit in the order before they exist: until they do, the
	/// Forest opens the Cave, exactly as if they were not there.
	/// </remarks>
	public static StageDefinition PreviousChapter(StageDefinition stage)
	{
		if (stage == null || !IsBossGated(stage.Gate))
			return null;
		IReadOnlyList<StageDefinition> order = OrderOf(stage.Id);
		int position = PositionIn(order, stage.Id);
		for (int i = position - 1; i >= 0; i--)
		{
			if (order[i].IsPlayable)
				return order[i];
		}
		return null;
	}

	/// <summary>
	/// The chapter that beating <paramref name="stageId"/>'s boss opens: the nearest playable,
	/// boss-gated chapter after it in the order it stands in. Null when the next chapter is opened some other
	/// way or the stage is outside both orders - and for a gate chapter, whose boss is only half of
	/// it: a victory that wrote the gate into the opened list would open it without the wizards. So the
	/// Throne's fall opens nothing: the far shore is another order, and its first chapter's gate is
	/// <see cref="StageGate.FarShore"/>.
	/// </summary>
	public static StageDefinition ChapterOpenedBy(string stageId)
	{
		IReadOnlyList<StageDefinition> order = OrderOf(stageId);
		int position = PositionIn(order, stageId);
		if (position < 0)
			return null;
		for (int i = position + 1; i < order.Count; i++)
		{
			StageDefinition next = order[i];
			if (!next.IsPlayable)
				continue;
			return next.Gate == StageGate.PreviousBoss ? next : null;
		}
		return null;
	}

	/// <summary>The line a closed chapter's lock gives, derived from the order. See <see cref="StageDefinition.LockedHint"/>.</summary>
	public static string LockedHintFor(StageDefinition stage)
	{
		if (stage == null)
			return string.Empty;
		if (!stage.IsPlayable)
			return "Not yet open - this chapter is still being built";
		if (stage.AfterCampaign)
			return "Beyond the map - finish the campaign to find it";

		switch (stage.Gate)
		{
			case StageGate.PreviousBossAndWizards:
			{
				// "Sealed - defeat the Long Coil in the Scorched Sands and free every wizard on the road"
				// (.ai/endgame-castle.md 1.6). Which wizards are still held is the save's to say; the world
				// map's card counts them under this line.
				StageDefinition before = PreviousChapter(stage);
				BossDefinition gateBoss = before != null ? BossCatalog.ForStageIndex(before.Index) : null;
				string road = stage == GateChapter ? " on the road" : string.Empty;
				return gateBoss != null
					? $"Sealed — defeat {MidSentence(gateBoss.DisplayName)} in the {StripArticle(before.DisplayName)} and free every wizard{road}"
					: $"Sealed — free every wizard{road}";
			}
			case StageGate.FarShore:
				return FarShoreLockedHint;
			case StageGate.PreviousBoss:
				StageDefinition previous = PreviousChapter(stage);
				if (previous == null)
					return "Locked";
				BossDefinition boss = BossCatalog.ForStageIndex(previous.Index);
				// "defeat the Hayward", not "defeat The Hayward": a name's article is lower case mid-sentence.
				// The world map shows every locked chapter's hint, where the old list hid them all.
				return boss != null
					? $"Locked — defeat {MidSentence(boss.DisplayName)} in the {StripArticle(previous.DisplayName)}"
					: $"Locked — clear the {StripArticle(previous.DisplayName)}";
			default:
				return string.Empty;
		}
	}

	/// <summary>
	/// The lock line with what only the save knows: for a gate chapter, WHO is still held
	/// (.ai/endgame-castle.md 1.6, "each naming those still held"), so the one gate that is not a single
	/// fight says what is missing. Named by epithet, never by name - a locked wizard's name is his reward
	/// (QuestLog, "A LOCKED WIZARD'S NAME IS HIS REWARD") - and a wizard not yet revealed is only "a wizard"
	/// (GlobalStatsManager.HeldGateWizards). Without a save, or for any other chapter, the line above.
	/// </summary>
	public static string LockedHintFor(StageDefinition stage, SaveData save)
	{
		string hint = LockedHintFor(stage);
		if (save == null || stage == null || !stage.IsPlayable)
			return hint;
		if (stage.Gate == StageGate.FarShore)
			return FarShoreLockedHintFor(save);
		if (stage.Gate != StageGate.PreviousBossAndWizards)
			return hint;
		List<string> held = GlobalStatsManager.HeldGateWizards(save, stage);
		return held.Count == 0 ? hint : $"{hint}. Still held: {string.Join(", ", held)}";
	}

	/// <summary>The far shore's lock with no save in hand: the whole of its gate (.ai/second-half.md 6.1).</summary>
	public const string FarShoreLockedHint = "Sealed — take back the Throne, recover every spell and free every wizard";

	/// <summary>
	/// The far shore's lock on this save: only what it still lacks (GlobalStatsManager.FarShoreGateMissing) -
	/// "Sealed — recover 3 more spells" is the first stop's line once the Throne is won (6.2). Wizards by epithet,
	/// never by name. Public apart from <see cref="LockedHintFor(StageDefinition, SaveData)"/>, which answers "still
	/// being built" while the White Downs are unbuilt, so the validator can read the line the day they open.
	/// </summary>
	public static string FarShoreLockedHintFor(SaveData save)
	{
		List<string> missing = GlobalStatsManager.FarShoreGateMissing(save);
		if (missing.Count == 0)
			return FarShoreLockedHint;
		string said = missing.Count == 1 ? missing[0]
			: string.Join(", ", missing.Take(missing.Count - 1)) + " and " + missing[missing.Count - 1];
		return "Sealed — " + said;
	}

	// "the The Emberdeep" is what a hint built from a name that already carries its article reads as.
	private static string StripArticle(string name) =>
		name != null && name.StartsWith("The ", StringComparison.Ordinal) ? name.Substring(4) : name;

	private static string MidSentence(string name) =>
		name != null && name.StartsWith("The ", StringComparison.Ordinal) ? "the " + name.Substring(4) : name;

	public static StageDefinition GetByIndex(int index)
	{
		if (Stages.Length == 0)
			return null;

		// Clamped rather than null-returning: every caller here is a display path reached from a
		// stage index that some other system already chose, and a missing name should degrade to
		// the first chapter's rather than crash a run that is otherwise fine.
		int clamped = Math.Clamp(index, 0, Stages.Length - 1);
		return Stages[clamped];
	}

	public static StageDefinition GetById(string stageId)
	{
		if (string.IsNullOrWhiteSpace(stageId))
			return null;

		return Stages.FirstOrDefault(s => s.Id.Equals(stageId.Trim(), StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>Display name for a stage id, falling back to the raw id so a log line is never blank.</summary>
	public static string DisplayNameFor(string stageId)
	{
		StageDefinition definition = GetById(stageId);
		return definition != null ? definition.DisplayName : stageId;
	}
}
