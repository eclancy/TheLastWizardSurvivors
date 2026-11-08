using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

public static class GlobalStatsManager
{
	public const float DynamicRewardMinMultiplier = 0.90f;
	public const float DynamicRewardMaxMultiplier = 1.55f;
	public const string BalancePresetCasual = "casual";
	public const string BalancePresetDefault = "default";
	public const string BalancePresetHardcore = "hardcore";

	// Was a 35-entry hardcoded set containing literally every spell in the game, which is what made
	// the whole unlock economy inert. UnlockCatalog is the authority now; this stays only as the
	// grandfathering list for saves written before the campaign existed (see SaveData.Migrate).
	public static readonly IReadOnlyList<string> LegacyDefaultUnlockedSpellIds = new[]
	{
		"magic_missile", "arcane_explosion", "spiritual_weapon", "fireball", "frost_shard",
		"shadow_bolt", "thorn_vine", "gale_blade", "solar_flare", "molten_shard", "chain_lightning",
		"toxic_spore_burst", "obsidian_spike", "cyclone_slash", "void_lance", "glacial_spike",
		"black_tentacles", "cone_of_cold", "scorching_ray", "meteor_swarm", "aegis_ward",
		"thornmail_barrier", "frozen_bulwark", "stormguard_aura", "venom_cloak", "guardian_vines",
		"tidal_barrier", "stone_bulwark", "blur", "fortunes_favor", "haste",
	};

	public static int GetUpgradeLevel(SaveData data, string upgradeId)
	{
		if (data == null || string.IsNullOrWhiteSpace(upgradeId))
			return 0;

		return data.ArcaneUpgradeLevels.TryGetValue(upgradeId, out int level) ? level : 0;
	}

	public static bool IsSpellUnlockedForLevelUp(SaveData data, string spellId)
	{
		if (string.IsNullOrWhiteSpace(spellId))
			return false;

		return UnlockCatalog.IsStarter(spellId, UnlockKind.Spell)
			|| (data != null && data.UnlockedSpellIds.Any(id => id.Equals(spellId, StringComparison.OrdinalIgnoreCase)));
	}

	public static bool UnlockSpell(SaveData data, string spellId)
	{
		if (data == null || string.IsNullOrWhiteSpace(spellId) || IsSpellUnlockedForLevelUp(data, spellId))
			return false;

		data.UnlockedSpellIds.Add(spellId);
		return true;
	}

	public static bool IsBoonUnlocked(SaveData data, string boonId)
	{
		if (string.IsNullOrWhiteSpace(boonId))
			return false;

		return UnlockCatalog.IsStarter(boonId, UnlockKind.Boon)
			|| (data != null && data.UnlockedBoonIds.Any(id => id.Equals(boonId, StringComparison.OrdinalIgnoreCase)));
	}

	public static bool UnlockBoon(SaveData data, string boonId)
	{
		if (data == null || string.IsNullOrWhiteSpace(boonId) || IsBoonUnlocked(data, boonId))
			return false;

		data.UnlockedBoonIds.Add(boonId);
		return true;
	}

	// Stage 0 is always available - a fresh save with no stages unlocked must still have somewhere
	// to play. Everything past it is earned by beating the previous stage's boss.
	public static bool IsStageUnlocked(SaveData data, string stageId)
	{
		if (string.IsNullOrWhiteSpace(stageId))
			return false;

		if (stageId.Equals(DefaultUnlockedStageId, StringComparison.OrdinalIgnoreCase))
			return true;

		return data != null && data.UnlockedStageIds.Any(id => id.Equals(stageId, StringComparison.OrdinalIgnoreCase));
	}

	public static bool UnlockStage(SaveData data, string stageId)
	{
		if (data == null || string.IsNullOrWhiteSpace(stageId) || IsStageUnlocked(data, stageId))
			return false;

		data.UnlockedStageIds.Add(stageId);
		return true;
	}

	public const string DefaultUnlockedStageId = "stage_0";

	// --- Spellbook curation ---------------------------------------------------------------------
	// Setting a spell aside stops it being offered at level-up. It is not a lock: the player owns
	// it, chose this, and can undo it at any time from the spellbook.
	//
	// Two things bound it, and they matter for different reasons. The *floor* is a design bound:
	// a book narrowed to three pages would make every level-up identical, so the pool may never
	// drop below MinimumOfferablePool. The *slots* are an economic bound: each one is bought with
	// Arcane Energy, so curation competes with the stat upgrades rather than being free.
	//
	// Capacity grows with the book, which is the point - on a fresh save with eight spells there is
	// nothing worth pruning, and the feature correctly offers nothing at all.

	/// <summary>The pool may never be narrowed below this many offerable spells.</summary>
	public const int MinimumOfferablePool = 12;

	public const string CurationUpgradeId = "curation";

	/// <summary>
	/// The shop's Reliquary Shelf: each level is one more boon slot in every run, above
	/// Player.BaseBoonSlots and up to Player.MaxBoonSlots (the boon rework, in-flight item 41).
	/// Schema 25 grants both levels to a save that had played - its runs all held six slots.
	/// </summary>
	public const string BoonSlotsUpgradeId = "boon_slots";

	public static int GetUnlockedSpellCount(SaveData data) =>
		UnlockCatalog.AllSpellIds.Count(id => IsSpellUnlockedForLevelUp(data, id));

	/// <summary>The Grimoire Restored's progress: spells recovered, the combinations not counted.</summary>
	public static int GetGrimoireSpellCount(SaveData data) =>
		UnlockCatalog.GrimoireSpellIds.Count(id => IsSpellUnlockedForLevelUp(data, id));

	/// <summary>How many curation slots the player could ever buy, given the book they have.</summary>
	public static int GetCurationSlotCapacity(SaveData data) =>
		Math.Max(0, GetUnlockedSpellCount(data) - MinimumOfferablePool);

	/// <summary>How many they have actually bought.</summary>
	public static int GetCurationSlotsOwned(SaveData data) =>
		Math.Min(GetUpgradeLevel(data, CurationUpgradeId), GetCurationSlotCapacity(data));

	public static bool IsSpellRemovedFromPool(SaveData data, string spellId)
	{
		if (data == null || string.IsNullOrWhiteSpace(spellId))
			return false;

		return data.RemovedSpellIds.Any(id => id.Equals(spellId, StringComparison.OrdinalIgnoreCase));
	}

	public static int GetRemovedSpellCount(SaveData data) => data?.RemovedSpellIds.Count ?? 0;

	/// <summary>
	/// Whether this spell could be set aside right now, and if not, why. The reason is returned
	/// rather than logged so the Spell Index's Set aside button can say it out loud instead of just refusing.
	/// </summary>
	public static bool CanRemoveSpellFromPool(SaveData data, string spellId, out string reason)
	{
		reason = string.Empty;
		if (data == null || string.IsNullOrWhiteSpace(spellId))
		{
			reason = "No save loaded.";
			return false;
		}

		if (!IsSpellUnlockedForLevelUp(data, spellId))
		{
			reason = "You have not found this spell yet.";
			return false;
		}

		if (GetRemovedSpellCount(data) >= GetCurationSlotsOwned(data))
		{
			int capacity = GetCurationSlotCapacity(data);
			reason = capacity > 0
				? "Buy another Redaction in Arcane Upgrades."
				: $"Find {MinimumOfferablePool} spells that can be offered before any can be set aside.";
			return false;
		}

		if (GetUnlockedSpellCount(data) - GetRemovedSpellCount(data) - 1 < MinimumOfferablePool)
		{
			reason = $"At least {MinimumOfferablePool} pages must stay in the book.";
			return false;
		}

		return true;
	}

	/// <summary>Sets a spell aside or puts it back. Returns whether anything changed.</summary>
	public static bool SetSpellRemovedFromPool(SaveData data, string spellId, bool removed)
	{
		if (data == null || string.IsNullOrWhiteSpace(spellId))
			return false;

		bool currently = IsSpellRemovedFromPool(data, spellId);
		if (currently == removed)
			return false;

		if (removed)
		{
			if (!CanRemoveSpellFromPool(data, spellId, out _))
				return false;

			data.RemovedSpellIds.Add(spellId);
			return true;
		}

		// Putting a page back is always allowed - it only ever widens the pool.
		data.RemovedSpellIds.RemoveAll(id => id.Equals(spellId, StringComparison.OrdinalIgnoreCase));
		return true;
	}

	// --- Characters -----------------------------------------------------------------------------
	// SaveData.UnlockedCharacterIds has existed since the save format did, and until now nothing
	// anywhere wrote to it: the list had exactly one reader, every CharacterData shipped with
	// IsUnlocked = true, and so the whole rail was dormant. These two are the missing write path.

	public static bool IsCharacterUnlocked(SaveData data, string characterId)
	{
		if (string.IsNullOrWhiteSpace(characterId))
			return false;

		return UnlockCatalog.IsStarter(characterId, UnlockKind.Character)
			|| (data != null && data.UnlockedCharacterIds.Any(id => id.Equals(characterId, StringComparison.OrdinalIgnoreCase)));
	}

	/// <summary>
	/// Whether a wizard's card may show who he is - his silhouette and how he is found - rather than
	/// a sealed blank. Always true except for a locked secret (<see cref="UnlockDefinition.IsSecret"/>)
	/// whose reveal chapter has not opened.
	/// </summary>
	/// <remarks>
	/// Derived, never stored: the save already says which chapters are open, so there is no
	/// "revealed" flag to migrate or to fall out of step with it. It also means the reveal follows
	/// the stage list exactly - a chapter appears there, and the wizard it holds appears here, on the
	/// same unlock. A wizard with a reveal secret (<see cref="UnlockDefinition.RevealSecretId"/>, Nimue's
	/// Oubliette) is revealed by its find too, whichever comes first - read from the found list the save
	/// already keeps, so that is derived as well.
	/// </remarks>
	public static bool IsCharacterRevealed(SaveData data, string characterId)
	{
		UnlockDefinition definition = UnlockCatalog.GetCharacter(characterId);
		if (definition == null || !definition.IsSecret || IsCharacterUnlocked(data, characterId))
			return true;
		if (!string.IsNullOrWhiteSpace(definition.RevealSecretId) && data != null && data.HasFoundSecret(definition.RevealSecretId))
			return true;

		return IsStageAvailable(data, StageCatalog.GetById(definition.RevealStageId));
	}

	public static bool UnlockCharacter(SaveData data, string characterId)
	{
		if (data == null || string.IsNullOrWhiteSpace(characterId) || IsCharacterUnlocked(data, characterId))
			return false;

		data.UnlockedCharacterIds.Add(characterId);
		return true;
	}

	// --- Achievements ---------------------------------------------------------------------------
	// Membership was open-coded as the same LINQ at three call sites, each free to get the string
	// comparison wrong independently.

	public static bool IsAchievementUnlocked(SaveData data, string achievementId)
	{
		if (data == null || string.IsNullOrWhiteSpace(achievementId))
			return false;

		return data.UnlockedAchievementIds.Any(id => id.Equals(achievementId, StringComparison.OrdinalIgnoreCase));
	}

	// --- The gate chapters ----------------------------------------------------------------------
	// A gate chapter (StageGate.PreviousBossAndWizards: the Emberdeep, and the castle's Throne when it
	// is built) opens when its previous chapter's boss is beaten and every wizard whose rescue comes
	// before it is free (.ai/achievements-and-secrets.md 2.5, .ai/endgame-castle.md 1.6). It used to
	// be one global gate, "every spell recovered and every wizard freed", and a new save could never
	// meet it: Fireball was paid only behind it. Spells are the collection now (The Grimoire Restored).

	public static bool AllSpellsUnlocked(SaveData data) =>
		UnlockCatalog.GrimoireSpellIds.All(id => IsSpellUnlockedForLevelUp(data, id));

	/// <summary>Spells still missing, for the Grimoire's "what is left".</summary>
	public static int RemainingSpellCount(SaveData data) =>
		UnlockCatalog.GrimoireSpellIds.Count(id => !IsSpellUnlockedForLevelUp(data, id));

	/// <summary>
	/// The wizards a gate chapter counts: every wizard (<see cref="UnlockCatalog.AllWizardIds"/>, which
	/// leaves out Laelaps) whose rescue can happen before it. Written per chapter, so a wizard freed in
	/// a chapter that moves behind a gate - Tituba, when the Cursed Dungeon goes into the castle - drops
	/// out of that gate's count by himself and into the next one's, and a gate can never wait on a
	/// person held behind it (the Fireball bug, for a wizard).
	/// </summary>
	public static IEnumerable<string> GateWizardIds(StageDefinition gate) =>
		UnlockCatalog.AllWizardIds.Where(id => CountsTowardGate(id, gate));

	/// <summary>
	/// Whether a gate counts this wizard: he is a wizard (not outside the campaign gate) and his rescue
	/// chapter - the chapter his source deed belongs to - comes before the gate. A starter, or one freed by
	/// a Feat that names no chapter (John Dee's chests), can be had before any gate, so every gate counts
	/// him. A chapter outside the campaign counts only if it is open from the first run. A chapter wizard
	/// (<see cref="UnlockDefinition.CountedAtFinalGateOnly"/>, .ai/chapter-wizards.md G2) is counted by the
	/// last gate chapter alone, wherever his rescue falls.
	/// </summary>
	public static bool CountsTowardGate(string wizardId, StageDefinition gate)
	{
		UnlockDefinition wizard = UnlockCatalog.GetCharacter(wizardId);
		if (wizard == null || wizard.OutsideCampaignGate || gate == null)
			return false;
		if (wizard.CountedAtFinalGateOnly && !gate.Id.Equals(StageCatalog.FinalGateChapter?.Id ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			return false;
		string chapter = RescueChapterOf(wizard);
		if (string.IsNullOrWhiteSpace(chapter))
			return true;
		int position = StageCatalog.CampaignPositionOf(chapter);
		if (position < 0)
			return StageCatalog.IsOpenFromStart(StageCatalog.GetById(chapter));
		return position < StageCatalog.CampaignPositionOf(gate.Id);
	}

	/// <summary>The chapter a wizard is freed in: his source deed's StageId. Empty for a starter or a Feat.</summary>
	public static string RescueChapterOf(UnlockDefinition wizard) =>
		UnlockCatalog.SourceDeedOf(wizard)?.StageId ?? string.Empty;

	/// <summary>The wizards a gate counts that this save has not freed yet, for the world map's card.</summary>
	public static int RemainingGateWizardCount(SaveData data, StageDefinition gate) =>
		GateWizardIds(gate).Count(id => !IsCharacterUnlocked(data, id));

	/// <summary>
	/// Who a gate is still waiting on, for its lock line (StageCatalog.LockedHintFor with a save): each
	/// wizard it counts that this save has not freed, by his epithet (QuestLog.EpithetOf, "the accused") -
	/// never his name, which is his reward - or only "a wizard" while his card is still sealed
	/// (<see cref="IsCharacterRevealed"/>), so a gate never says who a secret is before his chapter does.
	/// </summary>
	public static List<string> HeldGateWizards(SaveData data, StageDefinition gate) =>
		GateWizardIds(gate)
			.Where(id => !IsCharacterUnlocked(data, id))
			.Select(id => IsCharacterRevealed(data, id) ? QuestLog.EpithetOf(id) : "a wizard")
			.ToList();

	/// <summary>
	/// Whether a gate chapter's gate is met live: its previous chapter's boss beaten (the lifetime record)
	/// and every wizard it counts free. False for a chapter that is not a gate chapter.
	/// </summary>
	public static bool IsGateMet(SaveData data, StageDefinition gate) =>
		gate != null && gate.Gate == StageGate.PreviousBossAndWizards
		&& HasClearedPreviousChapter(data, gate)
		&& GateWizardIds(gate).All(id => IsCharacterUnlocked(data, id));

	/// <summary>
	/// Writes every gate chapter whose gate is met into the save's opened list, so it stays open (rule 1
	/// of <see cref="IsStageEarned"/>) whatever is added to its count later. A wizard added before a
	/// gate would otherwise close it, live, to every save that had it open - which is why schemas 10,
	/// 12, 14, 15 and 16 each had to grant what they added. Only adds. Returns the chapters it wrote,
	/// which the results screen shows as opened. Run after every run's achievements and in the schema 19
	/// step (which writes the ones a save had already met, so none of those is announced as new).
	/// </summary>
	/// <remarks>
	/// Only a PLAYABLE gate is recorded. A gate chapter not built yet has no built chapter before it to be
	/// gated on - the Throne's previous playable chapter is the Emberdeep until the bridge and the Court
	/// exist - so "met" would mean the Warden of the Deep and the wizards, and writing that down would hand
	/// the Throne to a save the day it flips, past a bridge and a court it never crossed
	/// (.ai/endgame-castle.md 1.9). Nobody can have had an unbuilt chapter open, so skipping it closes
	/// nothing; it is recorded by the same rule once it is playable and its real previous boss is known.
	/// </remarks>
	public static List<StageDefinition> RecordMetGates(SaveData data)
	{
		var opened = new List<StageDefinition>();
		if (data == null)
			return opened;
		foreach (StageDefinition gate in StageCatalog.GateChapters)
		{
			if (gate.IsPlayable && !IsStageUnlocked(data, gate.Id) && IsGateMet(data, gate) && UnlockStage(data, gate.Id))
				opened.Add(gate);
		}
		return opened;
	}

	/// <summary>
	/// Whether the world map shows a campaign chapter at all (Eric, 2026-10-31: "you should only unlock
	/// the map as you defeat the previous level"). The first chapter; any chapter the save has earned
	/// (<see cref="IsStageEarned"/>) or keeps from an older order (<see cref="IsGrandfathered"/>) - so
	/// nothing a save can enter is ever hidden from it; and a gate chapter whose previous boss has fallen,
	/// shown sealed until its wizards are free. Read from what the save already holds - beaten bosses and
	/// the two opened lists - so it needs no field of its own.
	/// </summary>
	public static bool IsChapterRevealed(SaveData data, StageDefinition stage)
	{
		if (stage == null)
			return false;
		int position = StageCatalog.CampaignPositionOf(stage.Id);
		if (position < 0)
		{
			// THE FAR SHORE (.ai/second-half.md 6.2): nothing of it on show before Taken Back - the Throne is the
			// locked thing on show until then. Once the Throne is won its first stop is shown under its seal, as a
			// gate chapter is once its previous boss falls; every later far chapter once it is earned.
			if (!StageCatalog.IsOnFarShore(stage))
				return false;
			return IsStageEarned(data, stage)
				|| (stage.Gate == StageGate.FarShore && HasTakenBackTheThrone(data));
		}
		return position == 0
			|| IsStageEarned(data, stage)
			|| IsGrandfathered(data, stage)
			|| (stage.Gate == StageGate.PreviousBossAndWizards && HasClearedPreviousChapter(data, stage));
	}

	/// <summary>
	/// The campaign is finished: the boss of its last playable chapter has fallen there, and that chapter is
	/// on the save's road (<see cref="StageCatalog.LastPlayableChapter"/>: the Cursed Dungeon while the
	/// castle's other chapters are unbuilt, the Throne once it is). What opens the Sketchbook's page (Eric,
	/// 2026-10-30: "Once you beat the whole campaign, you should be able to navigate to it").
	/// </summary>
	/// <remarks>
	/// On the road (.ai/endgame-castle.md 1.5): a Gaoler beaten in a grandfathered Dungeon - every save that
	/// won it as chapter five - finished nothing. A save that had finished the old campaign keeps the
	/// Sketchbook because the schema 21 step wrote it into the opened list (SaveData.MigrateDungeonIntoCastle).
	/// </remarks>
	public static bool HasFinishedCampaign(SaveData data)
	{
		StageDefinition last = StageCatalog.LastPlayableChapter;
		BossDefinition boss = last != null ? BossCatalog.ForStageIndex(last.Index) : null;
		return boss != null && data?.Lifetime != null && data.Lifetime.HasDefeatedBoss(boss.Id)
			&& IsStageEarned(data, last);
	}

	/// <summary>
	/// A chapter held back until the campaign is finished (<see cref="StageDefinition.AfterCampaign"/>) is
	/// reached three ways, any one enough: it is in the opened list (<see cref="RecordFinishedCampaign"/>
	/// wrote it), the campaign is finished now, or the save has already played there - it was open from
	/// the first run until 2026-10-30, and nothing a save could enter is ever closed to it.
	/// </summary>
	private static bool HasReachedAfterCampaign(SaveData data, StageDefinition stage) =>
		IsStageUnlocked(data, stage.Id)
		|| HasFinishedCampaign(data)
		|| (data?.Lifetime != null && data.Lifetime.BestTimeOnStage(stage.Id) > 0f);

	/// <summary>
	/// Writes every chapter held back until the campaign is finished into the opened list once it is, so
	/// it stays open when the campaign grows a new last chapter (the castle's) - the reason
	/// <see cref="RecordMetGates"/> writes the gates down. Only adds. Returns the chapters it wrote: the
	/// run that finished the campaign shows the Sketchbook opened on its results screen.
	/// </summary>
	public static List<StageDefinition> RecordFinishedCampaign(SaveData data)
	{
		var opened = new List<StageDefinition>();
		if (data == null || !HasFinishedCampaign(data))
			return opened;
		foreach (StageDefinition stage in StageCatalog.All)
		{
			if (stage.AfterCampaign && stage.IsPlayable && UnlockStage(data, stage.Id))
				opened.Add(stage);
		}
		return opened;
	}

	/// <summary>
	/// Whether a chapter can be entered: it is built, and the save has earned it on the road
	/// (<see cref="IsStageEarned"/>) or keeps it from an older order (<see cref="IsGrandfathered"/>).
	/// The gate chapters are the reason this is not just a lookup in the unlocked-stages list.
	/// </summary>
	public static bool IsStageAvailable(SaveData data, StageDefinition stage)
	{
		if (stage == null || !stage.IsPlayable)
			return false;

		return IsStageEarned(data, stage) || IsGrandfathered(data, stage);
	}

	/// <summary>
	/// The save may enter this chapter because an older campaign order opened it
	/// (<see cref="SaveData.GrandfatheredStageIds"/>, schema 21): the Cursed Dungeon, for every save that had
	/// reached it as chapter five before it moved into the castle (.ai/endgame-castle.md 1.5). Entry and
	/// nothing else: never read by the road's rules (<see cref="IsStageEarned"/>), and a victory in a chapter
	/// held only this way opens nothing (<see cref="ChapterOpenedByVictory"/>).
	/// </summary>
	public static bool IsGrandfathered(SaveData data, StageDefinition stage) =>
		stage != null && data?.GrandfatheredStageIds != null
		&& data.GrandfatheredStageIds.Any(id => id.Equals(stage.Id, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// The chapter a boss victory writes open (Node2DGame.ApplyBossVictoryUnlocks): what the order says the
	/// boss opens (<see cref="BossCatalog.ChapterOpenedBy"/>), but only when the chapter it was won in is
	/// on the save's road (<see cref="IsStageEarned"/>). Null otherwise - a win in a chapter held only by
	/// <see cref="IsGrandfathered"/> still pays its deed, its event, its vault and the lifetime record, and
	/// opens nothing (.ai/endgame-castle.md 1.5): an old Gaoler kill can never open the Court.
	/// </summary>
	public static StageDefinition ChapterOpenedByVictory(SaveData data, BossDefinition boss)
	{
		int index = BossCatalog.StageIndexOf(boss);
		if (index < 0 || !IsStageEarned(data, StageCatalog.GetByIndex(index)))
			return null;
		return BossCatalog.ChapterOpenedBy(boss);
	}

	/// <summary>
	/// Whether the save has earned a chapter ON THE ROAD, whether or not it is built yet. What
	/// <see cref="IsStageAvailable"/> answers once the chapter is playable, together with the
	/// grandfathered list, which this never reads.
	/// </summary>
	/// <remarks>
	/// A boss-gated chapter is earned three ways, and any one is enough (.ai/campaign.md, "The gate"):
	///
	///   1. Its id is in the save's opened list - what a boss victory writes, and what every save
	///      before the campaign order already holds.
	///   2. The save has opened a chapter LATER in the campaign. The campaign is one road, so being
	///      past a chapter is having been through it. This is what keeps the order free to change:
	///      the two early chapters, inserted before the Cave, open by themselves for a save that is
	///      already at the Cave or beyond - now, and the day they become playable - with no
	///      migration then, and nobody is ever stopped by a chapter that was added behind them.
	///   3. The boss of the chapter before it in the order has been beaten (LifetimeStats, which
	///      every save has kept since schema 8). Same purpose: a reorder re-derives what a recorded
	///      kill opens instead of trusting what the old order wrote. PAST THE FIRST GATE THE ROAD IS A
	///      CHAIN (<see cref="HasClearedPreviousChapter"/>): rule 3 counts there only when the previous
	///      chapter is itself earned, so a Gaoler beaten as chapter five, in a Dungeon a save now keeps
	///      only grandfathered, can never open the Court (.ai/endgame-castle.md 1.5).
	///
	/// Nothing here ever closes a chapter a save could already enter: each rule only adds, and the one
	/// chapter whose evidence moved (the Dungeon) is kept enterable by the schema 21 step instead.
	///
	/// A gate chapter (<see cref="StageGate.PreviousBossAndWizards"/>) keeps rules 1 and 2 - the
	/// opened list, which the schema 19 step writes for every save that had met the old gate, and being
	/// past it - and takes <see cref="IsGateMet"/> in place of rule 3: its previous boss AND the wizards.
	/// </remarks>
	public static bool IsStageEarned(SaveData data, StageDefinition stage)
	{
		if (stage == null)
			return false;

		return stage.Gate switch
		{
			StageGate.Open => !stage.AfterCampaign || HasReachedAfterCampaign(data, stage),
			StageGate.PreviousBossAndWizards => IsStageUnlocked(data, stage.Id)
				|| HasPassedInOrder(data, stage)
				|| IsGateMet(data, stage),
			// The far shore's first chapter (.ai/second-half.md 6.1): rules 1 and 2 as a gate chapter keeps them,
			// and the whole road behind it in place of rule 3.
			StageGate.FarShore => IsStageUnlocked(data, stage.Id)
				|| HasPassedInOrder(data, stage)
				|| IsFarShoreGateMet(data),
			_ => IsStageUnlocked(data, stage.Id)
				|| HasPassedInOrder(data, stage)
				|| HasClearedPreviousChapter(data, stage),
		};
	}

	/// <summary>
	/// Rule 2 of <see cref="IsStageEarned"/>: a boss-gated chapter later in the same order - the campaign's, or
	/// the far shore's - is open. Never across the two: a far chapter opened says nothing about the road.
	/// </summary>
	public static bool HasPassedInOrder(SaveData data, StageDefinition stage)
	{
		if (data == null || stage == null)
			return false;

		bool far = StageCatalog.IsOnFarShore(stage);
		int position = far ? StageCatalog.FarShorePositionOf(stage.Id) : StageCatalog.CampaignPositionOf(stage.Id);
		if (position < 0)
			return false;

		foreach (string id in data.UnlockedStageIds)
		{
			StageDefinition opened = StageCatalog.GetById(id);
			if (opened == null || opened.Gate != StageGate.PreviousBoss)
				continue;
			int at = far ? StageCatalog.FarShorePositionOf(opened.Id) : StageCatalog.CampaignPositionOf(opened.Id);
			if (at > position)
				return true;
		}
		return false;
	}

	/// <summary>
	/// Rule 3 of <see cref="IsStageEarned"/>, and the boss half of a gate (<see cref="IsGateMet"/>): the
	/// previous playable chapter's boss is beaten - and, for a chapter past the first gate, that chapter is
	/// itself on the road.
	/// </summary>
	/// <remarks>
	/// The chain (.ai/endgame-castle.md 1.5). Before the gate a kill is enough, unchanged: the road there
	/// has always re-derived what a recorded kill opens. Past it, the Cursed Dungeon stands in the castle
	/// and every save that won it as chapter five has the Gaoler in its lifetime record; without the chain
	/// that kill would open the Court (and, until the Court is built, half the Throne's gate). With it, the
	/// kill counts once the Dungeon is on the road, and it does then: a save that finished the old campaign
	/// takes the Court the day the Constable falls - a generous skip of one fight it has already won,
	/// accepted in the doc. Recursive down the castle to the gate, which is never chained.
	///
	/// The far shore is a chain from its first chapter (.ai/second-half.md 6.1, "every later far chapter opens on
	/// the one before it"): a far boss counts only once its own chapter is on the save's far shore, down to the
	/// first, whose gate is the whole road.
	/// </remarks>
	public static bool HasClearedPreviousChapter(SaveData data, StageDefinition stage)
	{
		StageDefinition previous = StageCatalog.PreviousChapter(stage);
		BossDefinition boss = previous != null ? BossCatalog.ForStageIndex(previous.Index) : null;
		if (boss == null || data?.Lifetime == null || !data.Lifetime.HasDefeatedBoss(boss.Id))
			return false;
		bool chained = IsPastFirstGate(stage) || StageCatalog.IsOnFarShore(stage);
		return !chained || IsStageEarned(data, previous);
	}

	// --- The far shore's gate (.ai/second-half.md 6.1) ----------------------------------------------
	// Eric's words for the half are "once every spell and character is unlocked and the main boss beaten", taken
	// as the doc recommends and he accepted (2026-11-07, decision 5): Taken Back, The Grimoire Restored and every
	// wizard a place on the road holds, Merlin and Laelaps with them - not the Dark Wizard (every chapter on
	// Hardcore: a difficulty) and not John Dee (100 chests: a grind), neither a place. It is a gate kind of its own
	// (StageGate.FarShore) because its "previous chapter" is the whole road, and it stands in the far shore's own
	// order, so nothing here moves what the campaign's deeds count.

	/// <summary>
	/// Taken Back: the dark wizard has fallen in the Throne - the deed done, or his fall in the lifetime record
	/// (the deed's own measure), so a save whose deed has not been evaluated yet is not held back by it.
	/// </summary>
	public static bool HasTakenBackTheThrone(SaveData data) =>
		data != null && (IsAchievementUnlocked(data, AchievementDefinitions.TakenBackId)
			|| (data.Lifetime != null && data.Lifetime.HasDefeatedBoss(AchievementDefinitions.ThroneBossId)));

	/// <summary>
	/// The Grimoire Restored: the deed done, or every spell it counts owned now. The deed alone would keep a save
	/// out until its next run's end evaluated it; the live count alone would close the shore to a save that had
	/// met it the day a spell was added - which RecordMetFarShore makes moot once the shore is built.
	/// </summary>
	public static bool HasRestoredGrimoire(SaveData data) =>
		data != null && (IsAchievementUnlocked(data, AchievementDefinitions.GrimoireRestoredId) || AllSpellsUnlocked(data));

	/// <summary>The wizards the far shore's gate counts, in catalog order (<see cref="CountsTowardFarShore"/>).</summary>
	public static IEnumerable<string> FarShoreWizardIds =>
		UnlockCatalog.AllCharacterIds.Where(CountsTowardFarShore);

	/// <summary>
	/// Whether the far shore's gate waits on this character: he is freed by a deed done in a PLACE on the road -
	/// a campaign chapter's warden, its side event, or a secret there. That is every chapter wizard, Tituba and
	/// Vainamoinen, Merlin (the moot) and Laelaps (the Pinfold), and Nimue, whom Taken Back frees anyway. It is not
	/// the Dark Wizard (his deed is the road on Hardcore, done nowhere), not John Dee (a Feat of chests, no
	/// place), not a starter, and never anyone freed on the far shore itself, which no gate before it may wait on.
	/// </summary>
	/// <remarks>
	/// The doc calls this "every wizard a place holds - which the Throne's gate already asks". In the code the
	/// Throne's gate also counts John Dee (every gate counts a Feat's wizard, GateWizardIds), so a save that won the
	/// Throne has him anyway; the far shore's own rule leaves him out, as the doc says, rather than inherit him.
	/// </remarks>
	public static bool CountsTowardFarShore(string characterId)
	{
		UnlockDefinition who = UnlockCatalog.GetCharacter(characterId);
		if (who == null || who.Source != UnlockSource.Achievement)
			return false;
		AchievementDefinition deed = UnlockCatalog.SourceDeedOf(who);
		if (deed == null || StageCatalog.CampaignPositionOf(deed.StageId) < 0)
			return false;
		return !string.IsNullOrWhiteSpace(deed.BossId) || !string.IsNullOrWhiteSpace(deed.SiteId)
			|| !string.IsNullOrWhiteSpace(deed.SecretId);
	}

	/// <summary>The far shore's gate, live: the Throne taken back, every spell recovered, every wizard it counts free.</summary>
	public static bool IsFarShoreGateMet(SaveData data) =>
		HasTakenBackTheThrone(data) && HasRestoredGrimoire(data)
		&& FarShoreWizardIds.All(id => IsCharacterUnlocked(data, id));

	/// <summary>
	/// What the far shore's gate still waits on, in the lock line's words (StageCatalog.FarShoreLockedHintFor):
	/// "take back the Throne", "recover 3 more spells", "free the accused and a wizard". Wizards by epithet while
	/// revealed and only "a wizard" while sealed - never by name (<see cref="HeldGateWizards"/>' rule). Empty once met.
	/// </summary>
	public static List<string> FarShoreGateMissing(SaveData data)
	{
		var missing = new List<string>();
		if (!HasTakenBackTheThrone(data))
			missing.Add("take back the Throne");
		if (!HasRestoredGrimoire(data))
		{
			int left = RemainingSpellCount(data);
			missing.Add(left == 1 ? "recover 1 more spell" : $"recover {left} more spells");
		}
		List<string> held = FarShoreWizardIds
			.Where(id => !IsCharacterUnlocked(data, id))
			.Select(id => IsCharacterRevealed(data, id) ? QuestLog.EpithetOf(id) : "a wizard")
			.ToList();
		if (held.Count > 0)
			missing.Add("free " + string.Join(", ", held));
		return missing;
	}

	/// <summary>
	/// Writes the far shore's first chapter into the save's opened list once its gate is met, so a spell or a
	/// wizard added to the road later can never close it again - <see cref="RecordMetGates"/>' reason, for the
	/// far shore's gate. Only adds, and only a PLAYABLE chapter: nothing unbuilt is ever written into a save
	/// (.ai/endgame-castle.md 1.9), so while the White Downs are a stand-in this writes nothing. Returns what it
	/// wrote, for the results screen (AchievementManager).
	/// </summary>
	public static List<StageDefinition> RecordMetFarShore(SaveData data)
	{
		var opened = new List<StageDefinition>();
		if (data == null)
			return opened;
		foreach (StageDefinition shore in StageCatalog.FarShore.Where(s => s.Gate == StageGate.FarShore))
		{
			if (shore.IsPlayable && !IsStageUnlocked(data, shore.Id) && IsFarShoreGateMet(data) && UnlockStage(data, shore.Id))
				opened.Add(shore);
		}
		return opened;
	}

	/// <summary>A campaign chapter after the first gate chapter (the castle's, and the Dungeon since C2).</summary>
	private static bool IsPastFirstGate(StageDefinition stage)
	{
		StageDefinition gate = StageCatalog.GateChapter;
		return gate != null && StageCatalog.CampaignPositionOf(stage.Id) > StageCatalog.CampaignPositionOf(gate.Id);
	}

	/// <param name="parts">
	/// Optional. Each bonus that applied, by the name a player is shown (the results screen's
	/// Arcane Energy breakdown) and its size as a fraction. The multiplier is the same with or
	/// without it.
	/// </param>
	public static float GetDynamicArcaneRewardMultiplier(SaveData data, RunResult currentRun, out string breakdown,
		List<(string Name, float Bonus)> parts = null)
	{
		float multiplier = 1.0f;
		var notes = new List<string>();
		if (data == null || currentRun == null)
		{
			breakdown = "No dynamic modifiers";
			return multiplier;
		}

		// There is no losing-streak ("pity") bonus any more: Eric, 2026-10-30, "remove pity streak". It
		// paid up to +24% after consecutive defeats and showed as "Pity streak N" on the main menu.

		int victoryStreak = GetConsecutiveOutcomeCount(data, "Victory");
		if (victoryStreak >= 2)
		{
			float momentumBonus = Math.Min(0.14f, (victoryStreak - 1) * 0.025f);
			multiplier += momentumBonus;
			notes.Add($"Momentum +{Mathf.RoundToInt(momentumBonus * 100f)}%");
			parts?.Add(("Winning streak", momentumBonus));
		}

		float varietyBonus = CalculateVarietyBonus(data, currentRun);
		if (varietyBonus > 0f)
		{
			multiplier += varietyBonus;
			notes.Add($"Variety +{Mathf.RoundToInt(varietyBonus * 100f)}%");
			parts?.Add(("Spells new to you lately", varietyBonus));
		}

		float improvementBonus = CalculateImprovementBonus(data, currentRun);
		if (improvementBonus > 0f)
		{
			multiplier += improvementBonus;
			notes.Add($"Improvement +{Mathf.RoundToInt(improvementBonus * 100f)}%");
			parts?.Add(("Better than your last run", improvementBonus));
		}

		multiplier = Mathf.Clamp(multiplier, DynamicRewardMinMultiplier, DynamicRewardMaxMultiplier);
		breakdown = notes.Count == 0 ? "No dynamic modifiers" : string.Join(", ", notes);
		return multiplier;
	}

	public static float GetNextRunArcanePreviewMultiplier(SaveData data)
	{
		if (data == null)
			return 1.0f;

		int victoryStreak = GetConsecutiveOutcomeCount(data, "Victory");
		float multiplier = 1.0f;
		if (victoryStreak >= 2)
			multiplier += Math.Min(0.14f, (victoryStreak - 1) * 0.025f);

		return Mathf.Clamp(multiplier, DynamicRewardMinMultiplier, DynamicRewardMaxMultiplier);
	}

	public static string NormalizeBalancePresetId(string? presetId)
	{
		string normalized = presetId?.Trim().ToLowerInvariant() ?? BalancePresetDefault;
		return normalized switch
		{
			BalancePresetCasual => BalancePresetCasual,
			BalancePresetHardcore => BalancePresetHardcore,
			_ => BalancePresetDefault
		};
	}

	public static string GetBalancePresetDisplayName(string? presetId)
	{
		return NormalizeBalancePresetId(presetId) switch
		{
			BalancePresetCasual => "Casual",
			BalancePresetHardcore => "Hardcore",
			_ => "Default"
		};
	}

	// --- the hardest clear of every chapter (SaveData.MaxDifficultyCleared) -----------------------------------
	//
	// Eric, 2026-11-02: "dark wizard should unlock after defeating all stages on hard mode" and Merlin (asked for as
	// the blue-robed wizard, the player's figure since; .ai/merlin.md) "unlocked when finishing the full campaign
	// on medium". Easy, medium and hard are the Options
	// difficulty presets, Casual, Default and Hardcore, ranked 0, 1 and 2 here. Each chapter keeps the highest
	// rank its boss has fallen on (RecordChapterClear, from AchievementManager.ApplyRunAchievements), and the two
	// deeds read it through ClearedEveryChapterAt. The field was saved and loaded since the first save format
	// and never written, so no save holds a stale value in it.

	public const int PresetRankCasual = 0;
	public const int PresetRankDefault = 1;
	public const int PresetRankHardcore = 2;

	/// <summary>Casual 0, Default 1, Hardcore 2: how hard a preset is, for "this hard or harder".</summary>
	public static int PresetRank(string? presetId) => NormalizeBalancePresetId(presetId) switch
	{
		BalancePresetCasual => PresetRankCasual,
		BalancePresetHardcore => PresetRankHardcore,
		_ => PresetRankDefault,
	};

	/// <summary>
	/// The chapter whose boss this run beat, and the rank of the preset it was played on: a victory with a boss
	/// (a Boss Rush records none, Node2DGame.BuildRunResult), at the boss's own chapter (BossCatalog). Null for
	/// anything else.
	/// </summary>
	public static (string StageId, int Rank)? ChapterClearOf(global::RunResult run)
	{
		if (run == null || run.Outcome != global::RunOutcome.Victory || string.IsNullOrWhiteSpace(run.BossId))
			return null;
		BossDefinition boss = BossCatalog.GetById(run.BossId);
		int index = boss != null ? BossCatalog.StageIndexOf(boss) : -1;
		StageDefinition stage = index >= 0 ? StageCatalog.GetByIndex(index) : null;
		return stage == null ? null : (stage.Id, PresetRank(run.BalancePresetId));
	}

	/// <summary>
	/// Writes a boss victory into <see cref="SaveData.MaxDifficultyCleared"/>: its chapter keeps the hardest
	/// preset its boss has fallen on, never a lower one. True when the record rose.
	/// </summary>
	public static bool RecordChapterClear(SaveData data, global::RunResult run)
	{
		if (data == null || ChapterClearOf(run) is not (string stageId, int rank))
			return false;
		data.MaxDifficultyCleared ??= new Dictionary<string, int>();
		if (data.MaxDifficultyCleared.TryGetValue(stageId, out int kept) && kept >= rank)
			return false;
		data.MaxDifficultyCleared[stageId] = rank;
		return true;
	}

	/// <summary>
	/// The hardest preset this chapter's boss has fallen on, -1 if never. With <paramref name="run"/>, as if that
	/// run had been recorded too - the pause tab's projection, which reads the save before the run's end writes it.
	/// </summary>
	public static int HardestClearOf(SaveData data, string stageId, global::RunResult run = null)
	{
		int best = -1;
		if (data?.MaxDifficultyCleared != null && !string.IsNullOrWhiteSpace(stageId))
		{
			foreach (KeyValuePair<string, int> entry in data.MaxDifficultyCleared)
			{
				if (entry.Key.Equals(stageId, StringComparison.OrdinalIgnoreCase))
					best = Math.Max(best, entry.Value);
			}
		}
		if (ChapterClearOf(run) is (string runStage, int runRank) && runStage.Equals(stageId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			best = Math.Max(best, runRank);
		return best;
	}

	/// <summary>How many of the campaign's chapters have been cleared at <paramref name="rank"/> or harder.</summary>
	public static int ChaptersClearedAt(SaveData data, int rank, global::RunResult run = null) =>
		StageCatalog.Campaign.Count(stage => HardestClearOf(data, stage.Id, run) >= rank);

	/// <summary>
	/// Every chapter of the campaign (StageCatalog.Campaign) has had its boss beaten at <paramref name="rank"/> or
	/// harder. COUNTING THE CHAPTERS NOT YET PLAYABLE (the Court and the Throne today): neither deed that reads
	/// this - the Dark Wizard's on Hardcore, Merlin's on Default - can be earned until the whole campaign exists
	/// and has been beaten. A chapter outside the campaign (the Sketchbook) is not asked for.
	/// </summary>
	public static bool ClearedEveryChapterAt(SaveData data, int rank, global::RunResult run = null) =>
		StageCatalog.Campaign.Count > 0 && ChaptersClearedAt(data, rank, run) == StageCatalog.Campaign.Count;

	public static float GetArcaneRewardScaleForPreset(string? presetId)
	{
		return NormalizeBalancePresetId(presetId) switch
		{
			BalancePresetCasual => 0.92f,
			BalancePresetHardcore => 1.12f,
			_ => 1.00f
		};
	}

	public static int GetConsecutiveOutcomeCount(SaveData data, string outcome)
	{
		if (data == null || data.RunTelemetryHistory == null || data.RunTelemetryHistory.Count == 0 || string.IsNullOrWhiteSpace(outcome))
			return 0;

		int count = 0;
		foreach (RunTelemetryRecord run in data.RunTelemetryHistory)
		{
			if (!string.Equals(run.Outcome, outcome, StringComparison.OrdinalIgnoreCase))
				break;
			count++;
		}

		return count;
	}

	private static float CalculateVarietyBonus(SaveData data, RunResult currentRun)
	{
		if (currentRun.EquippedSpells == null || currentRun.EquippedSpells.Count == 0)
			return 0f;

		var recentSpellIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (RunTelemetryRecord run in data.RunTelemetryHistory.Take(3))
		{
			foreach (string id in run.SpellPickCounts.Keys)
			{
				if (!string.IsNullOrWhiteSpace(id))
					recentSpellIds.Add(id);
			}
		}

		if (recentSpellIds.Count == 0)
			return 0f;

		int freshSpells = currentRun.EquippedSpells.Count(spell => !recentSpellIds.Contains(spell.Id));
		if (freshSpells <= 0)
			return 0f;

		return Math.Min(0.10f, freshSpells * 0.025f);
	}

	private static float CalculateImprovementBonus(SaveData data, RunResult currentRun)
	{
		RunTelemetryRecord? previous = data.RunTelemetryHistory.FirstOrDefault();
		if (previous == null)
			return 0f;

		float bonus = 0f;
		if (currentRun.TimeSurvived > previous.TimeSurvived)
			bonus += Math.Min(0.06f, (currentRun.TimeSurvived - previous.TimeSurvived) / 2100f);

		if (currentRun.EnemiesKilled > previous.EnemiesKilled)
			bonus += Math.Min(0.04f, (currentRun.EnemiesKilled - previous.EnemiesKilled) / 900f);

		return Math.Min(0.08f, bonus);
	}
}