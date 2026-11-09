using Godot;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

public class SaveData
{
	// 8 introduced the unlock economy. Before it, every spell was free, so a save written at 7 or
	// below has UnlockedSpellIds that mean nothing - Migrate reads that as "owned everything".
	// 11 added SeenAscensionIds (the ascension browser's sealed paths); 12 grants John Dee to a
	// save that has already finished the campaign; 13 re-derives the opened chapters under the
	// campaign order that moved the Cursed Dungeon to fifth (.ai/campaign.md); 14 grants Starfall
	// to a finished campaign the way 12 granted John Dee; 15 grants the Familiar the same way, and 16
	// the facing family (Stormsweep and Sleet Runes). 17 adds the stage secrets' two fields,
	// FoundSecretIds and FoundUpgradeLevels (.ai/stage-secrets.md section 5, issue #20). 18 refunds
	// the retired level-up charges. 19 makes every spell an achievement's (.ai/achievements-and-secrets.md
	// section 3): GlimpsedSecretIds and LifetimeStats.EventsWon, the event deeds seeded from what the save
	// owns, moved rewards reconciled, and the Emberdeep kept open for anyone who had met the old gate.
	// 20 is the difficulty curve's (.ai/difficulty-curve.md, section 9): AscensionLearned for every save
	// that has played a run, and the levels bought of the re-priced Powers (Fate Fracture, Emberheart)
	// paid back the difference. 21 is the castle's (.ai/endgame-castle.md 1.5, piece C2): the Cursed
	// Dungeon moved from fifth into the castle, so what each save could enter is written down under the
	// old order, and the Dungeon itself is kept in GrandfatheredStageIds - entry, and evidence of nothing.
	// 22 is the slow start's (.ai/slow-start.md, section 6): a played save keeps its six pages
	// (FullBookKept) and every system it could already use (LessonsLearned). 23 merges Chronoweave
	// into Tempest Rhythm (Eric, 2026-11-07): bought cooldown levels become attack-speed levels one
	// for one, any past the new 16-level cap refunded at what they cost. 24 is Shadow Bolt's move
	// onto the Conjunction (in-flight 17): a save that owned the old spell or its deed is granted
	// the new Feat, so nothing it earned is confiscated. (The branch that built it numbered it 23,
	// not knowing 23 was taken; players' saves already carry 23, so it has to be 24.)
	// 25 is the boon rework's (in-flight 41): runs hold two boon slots now and the shop sells the
	// third and fourth, so a save that has played - every run it played held six - is granted both
	// Reliquary Shelf levels rather than being asked to buy back capacity it had.
	// 26 is the Sovereign Rite's move onto its own recipe (Eric, 2026-11-08; .ai/sovereign-spell.md
	// section 9): Arcane Explosion and Gravewell made one, paid by its Feat rather than the Warden of
	// the Deep's deed. A save that owned the Rite, did that deed or beat the Warden keeps it - its
	// Feat is recorded done, as 24 did for Shadow Bolt.
	// 27 is the Conjunction ladder's (.ai/conjunction-progression.md, rung I): PinnedRecipeId, the one recipe the
	// player has pinned. A save from before has pinned nothing, so the step only makes that explicit. (Built as 26 in
	// parallel with the Rite's; the Rite reached the trunk first, so the pin is 27.)
	public const int CurrentSchemaVersion = 27;
	public const int CampaignSchemaVersion = 8;
	private const int MaxTelemetryHistory = 25;

	public int SchemaVersion { get; set; } = CurrentSchemaVersion;
	public int TotalCurrency { get; set; } = 0;
	public List<string> UnlockedCharacterIds { get; set; } = new();
	/// <summary>
	/// The chapters opened ON THE ROAD: what a boss victory writes, and what the road's rules read
	/// (GlobalStatsManager.IsStageEarned - rule 1, and rule 2's "a later chapter is opened").
	/// </summary>
	public List<string> UnlockedStageIds { get; set; } = new();

	/// <summary>
	/// Chapters this save may enter because an OLDER campaign order opened them (schema 21,
	/// .ai/endgame-castle.md 1.5): the Cursed Dungeon, for every save that had reached it as chapter five
	/// before it moved into the castle. Entry and nothing else (GlobalStatsManager.IsGrandfathered): never
	/// evidence for the road's rules, and a victory in a chapter held only this way opens nothing. Only the
	/// schema 21 step writes it; nothing ever removes from it, so nobody loses a chapter they could play.
	/// </summary>
	public List<string> GrandfatheredStageIds { get; set; } = new();

	/// <summary>
	/// The Boss Rush was open to this save before schema 21 - its finale, the Warden of the Deep, had fallen -
	/// so it stays open whatever BossRushCatalog.IsOpen asks now ("every boss on its roster beaten", since
	/// the Gaoler moved behind the Warden). Written only by the schema 21 step.
	/// </summary>
	public bool BossRushGrandfathered { get; set; } = false;
	public List<string> UnlockedSpellIds { get; set; } = new();

	/// <summary>Boons the player has earned or bought. Starter boons are not listed; they are free.</summary>
	public List<string> UnlockedBoonIds { get; set; } = new();
	public List<string> UnlockedAchievementIds { get; set; } = new();
	// Spells the player has deliberately set aside so they stop being offered at level-up. Distinct
	// from "locked": these are owned, and can be put back at any time.
	public List<string> RemovedSpellIds { get; set; } = new();

	/// <summary>
	/// Every level 8 Ascension (and Apotheosis form) whose requirements the player has met at least
	/// once, as "spell_id:branch_id". Until a path is in here the ascension browser shows it SEALED -
	/// "???", no effect, no icon - and from the first level-up that finds it met it is shown in full
	/// for good, in every later run, taken or not (AscensionSightings, .ai/level-up-screen.md 4e).
	/// </summary>
	/// <remarks>
	/// Keyed by spell AND branch: the derived evolutions share their ids across spells (Titan
	/// Cataclysm is a branch of three of them), and meeting it on Cinderbreath says nothing about
	/// Bramble Seed's. Only a real run writes it; the harnesses never do.
	/// </remarks>
	public List<string> SeenAscensionIds { get; set; } = new();

	public Dictionary<string, int> MaxDifficultyCleared { get; set; } = new();
	public Dictionary<string, int> ArcaneUpgradeLevels { get; set; } = new();

	/// <summary>Every stage secret found at least once (SecretCatalog ids). Drives the Secrets page.</summary>
	public List<string> FoundSecretIds { get; set; } = new();

	/// <summary>
	/// Permanent upgrade levels FOUND in the world rather than bought (issue #20): a chapter's keepsake
	/// secret pays one. Kept apart from ArcaneUpgradeLevels so a shop refund or a respec can never eat
	/// them, and summed with it in Player.ApplyArcaneUpgrades, so they stack above the shop's cap.
	/// </summary>
	public Dictionary<string, int> FoundUpgradeLevels { get; set; } = new();

	public bool HasFoundSecret(string secretId) =>
		FoundSecretIds.Any(id => id.Equals(secretId, System.StringComparison.OrdinalIgnoreCase));

	/// <summary>Marks a secret found. False when it already was, so a reward can be paid once ever.</summary>
	public bool RecordSecretFound(string secretId)
	{
		if (string.IsNullOrWhiteSpace(secretId) || HasFoundSecret(secretId))
			return false;
		FoundSecretIds.Add(secretId);
		return true;
	}

	public int FoundUpgradeLevel(string upgradeId) =>
		FoundUpgradeLevels.TryGetValue(upgradeId, out int level) ? level : 0;

	/// <summary>
	/// Every secret whose near tell has been on screen at least once, in any run (SecretCatalog ids):
	/// what turns a hidden achievement from a count into a row with its name and what was seen
	/// (.ai/achievements-and-secrets.md 4.3). Written by the run's SecretDirector (piece A4) and saved with
	/// the next save, not at once - a seen mark lost to a crash costs nothing.
	/// </summary>
	public List<string> GlimpsedSecretIds { get; set; } = new();

	public bool HasGlimpsedSecret(string secretId) =>
		!string.IsNullOrWhiteSpace(secretId)
		&& GlimpsedSecretIds.Any(id => id.Equals(secretId.Trim(), System.StringComparison.OrdinalIgnoreCase));

	/// <summary>Marks a secret seen. False when it already was.</summary>
	public bool RecordSecretGlimpsed(string secretId)
	{
		if (string.IsNullOrWhiteSpace(secretId) || HasGlimpsedSecret(secretId))
			return false;
		GlimpsedSecretIds.Add(secretId.Trim());
		return true;
	}

	/// <summary>The Boss Rush's records (issue #12, BossRushCatalog): the most bosses one rush has felled.</summary>
	/// <remarks>
	/// No schema bump for these two: they only add, and FromVariant guards every read with
	/// ContainsKey, so an older save reads as "never rushed".
	/// </remarks>
	public int BossRushBestFelled { get; set; } = 0;

	/// <summary>The fastest whole rush, in real seconds with the fights in. 0 until one is won.</summary>
	public float BossRushBestClearSeconds { get; set; } = 0f;

	// --- ENDLESS (.ai/endless-mode.md 3.5; EndlessCatalog) ---------------------------------------------------------
	// No schema step for either: both only add and default to the quiet state (the pattern of HasSeenLightGoesOn and
	// BossRushBestFelled), FromVariant guards each read with ContainsKey, and the ledger rule means no existing record
	// changes meaning.

	/// <summary>The best night per chapter, wizard and preset (EndlessCatalog.Record): written at every fall in a night, and at the run's end.</summary>
	public List<EndlessRecord> EndlessRecords { get; set; } = new();

	/// <summary>The first vigil kept: the world-map card's new mark and the first fall's second banner line until then.</summary>
	public bool HasKeptVigil { get; set; } = false;
	public List<RunTelemetryRecord> RunTelemetryHistory { get; set; } = new();
	// Aggregates across every run ever played. The telemetry ring above is capped at 25 entries and
	// drops most of what an achievement asks about, so it cannot serve this purpose.
	public LifetimeStats Lifetime { get; set; } = new();
	public bool HasSeenGameplayOnboarding { get; set; } = false;
	/// <summary>
	/// The story-mode prologue has been shown (Prologue, .ai/prologue.md): Start Run plays it first
	/// until this is true. No schema bump - it only adds, and a missing key reads as false, so an
	/// existing save sees the prologue once, on its next Start Run, which is intended. Written only
	/// when writes are on, so no tool scene or --no-save-writes run ever sets it.
	/// </summary>
	public bool HasSeenPrologue { get; set; } = false;

	/// <summary>
	/// Reel E, "The light goes on", has been shown (LightGoesOn, .ai/second-half.md 6.3): the light going east over
	/// the dark sea after the Throne is won. Owed only to a save that has taken back the Throne, so the default
	/// (unseen) is the quiet state for everyone else and NO SCHEMA STEP is needed: it only adds, a missing key reads as
	/// false, and a save that won the Throne before the reel existed meets it once, the first time the world map
	/// opens. Written when the reel ends, played through or skipped; set in memory but never saved by a tool scene
	/// or a --no-save-writes run (SaveManager.SaveGame).
	/// </summary>
	public bool HasSeenLightGoesOn { get; set; } = false;

	/// <summary>
	/// The quests the player has chosen to see on screen during a run (QuestLog, .ai/quests-and-notes.md):
	/// achievement ids, and <see cref="QuestLog.NextWizardId"/> for "free the next wizard", which resolves to
	/// whoever is next each time it is read. No schema bump: it only adds, and a save without the key keeps
	/// this default, so every save - new or old - starts tracking the next wizard. An empty list is the
	/// player's own choice (everything untracked), which is why the default lives here and not in a reader.
	/// </summary>
	public List<string> TrackedQuestIds { get; set; } = new() { QuestLog.NextWizardId };

	/// <summary>
	/// The one Conjunction the player has pinned (ConjunctionLadder, rung I of .ai/conjunction-progression.md): a
	/// recipe's id, or empty. Its checklist is on the pause screen and its next step one line in the quest corner.
	/// Pinned from the Codex; read only once the Conjunctions lesson is learned (ConjunctionLadder.Pinned). Schema 27
	/// added it and pins nothing (MigrateToPinnedRecipe).
	/// </summary>
	public string PinnedRecipeId { get; set; } = string.Empty;

	/// <summary>
	/// The chapters whose note (ChapterNotes) has been shown, as stage ids: a note shows on the first entry
	/// to its chapter and then only in the journal. No schema bump - additive, empty by default - so an
	/// existing save reads each chapter's note once, on its next run there, as it did the prologue.
	/// </summary>
	public List<string> SeenChapterNoteIds { get; set; } = new();

	/// <summary>
	/// The relics and boons the player has met - offered by a chest or a level-up, or held - by id: the
	/// Evolution Codex shows the rest as "???" (.ai/evolution-codex.md, question 3). No schema bump, the way
	/// <see cref="HasSeenPrologue"/> was added: it only adds, a save without the key reads as empty, and an
	/// older save's items open on their next sighting. Written by CodexSightings, never by a tool scene.
	/// </summary>
	public List<string> SeenItemIds { get; set; } = new();

	/// <summary>
	/// Met: recorded here, or claimed in an earlier run - LifetimeStats.ChestItemsCollected, which every save
	/// has kept since before this record existed, so a returning player's relics are not all "???".
	/// </summary>
	public bool IsItemSeen(string itemId) =>
		!string.IsNullOrWhiteSpace(itemId)
		&& (SeenItemIds.Any(id => id.Equals(itemId.Trim(), System.StringComparison.OrdinalIgnoreCase))
			|| (Lifetime?.ChestItemsCollected?.Any(id => id.Equals(itemId.Trim(), System.StringComparison.OrdinalIgnoreCase)) ?? false));

	/// <summary>Records an item as met. True when it was not before (the caller saves).</summary>
	public bool MarkItemSeen(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId) || IsItemSeen(itemId))
			return false;
		SeenItemIds.Add(itemId.Trim().ToLowerInvariant());
		return true;
	}

	public bool EnableGameplayOnboardingTips { get; set; } = true;
	public bool HasToggledOnboardingTipsAtLeastOnce { get; set; } = false;
	public bool PlaytestModeEnabled { get; set; } = false;
	public string BalancePresetId { get; set; } = GlobalStatsManager.BalancePresetDefault;
	public Dictionary<string, bool> PlaytestChecklistState { get; set; } = new();
	// Bus volumes, linear 0..1 exactly as the sliders show them. Before these existed the
	// options screen wrote straight to AudioServer and every launch came back at full volume.
	// No schema bump is needed: FromGodotDictionary guards every read with ContainsKey, so an
	// older save simply keeps these defaults.
	public float MasterVolume { get; set; } = 1.0f;
	public float MusicVolume { get; set; } = 1.0f;
	public float SfxVolume { get; set; } = 1.0f;
	public bool AudioMuted { get; set; } = false;
	/// <summary>Enemies throw their damage numbers when hit (DisplaySettings). No schema bump: an older
	/// save simply has the key missing and keeps the default, on.</summary>
	public bool ShowDamageNumbers { get; set; } = true;
	/// <summary>Screen shake at a quarter strength (DisplaySettings.ShakeScale, Eric, 2026-11-02). No schema
	/// bump, like the damage numbers: an older save has the key missing and keeps the default, off.</summary>
	public bool ReduceScreenShake { get; set; } = false;
	/// <summary>A dark chapter's dark lifted to DisplaySettings.ReducedDarknessScale (LightPool, 2026-11-03). No
	/// schema bump, like the shake: an older save has the key missing and keeps the default, off.</summary>
	public bool ReduceDarkness { get; set; } = false;
	public bool Fullscreen { get; set; } = true;
	/// <summary>Zero means an automatically fitted window. Fullscreen always uses the display's native size.</summary>
	public int WindowWidth { get; set; }
	public int WindowHeight { get; set; }
	/// <summary>The monitor the game puts itself on (DisplayProfile). -1 means wherever the OS put the
	/// window; a stale index (monitor unplugged) is ignored, not clamped. No schema bump, like the
	/// other display settings: an older save has the key missing and keeps the default.</summary>
	public int MonitorIndex { get; set; } = -1;

	/// <summary>
	/// Taught the Ascension outside the campaign's own record (GlobalStatsManager.HasLearnedAscension):
	/// set by schema 20 for every save that had already played a run, all of which had the Ascension in
	/// every run they played. A new save learns it from the Hayward, which the campaign records anyway.
	/// </summary>
	public bool AscensionLearned { get; set; } = false;

	/// <summary>
	/// The lessons given (Teachings): the systems this save has been taught, each by its post-run cutscene,
	/// by id ("ascension", "apotheosis", "conjunctions", "far_sky"). A system is usable once its id is here
	/// (the Ascension also by <see cref="AscensionLearned"/>). Schema 22 writes the first three for a played
	/// save that could already use them, so it never sees the cutscenes. "sovereign_rite" may still be here
	/// from the Rite's retired lesson (2026-11-08); nothing reads it.
	/// </summary>
	public List<string> LessonsLearned { get; set; } = new();

	/// <summary>
	/// The book this save played with before the slow start: six pages, for good (SpellBook). Set by schema 22
	/// for every save with a run on record; the shop then shows Bound Pages complete, at no charge.
	/// </summary>
	public bool FullBookKept { get; set; } = false;

	/// <summary>
	/// The freeing reels owed (Freeings, .ai/cutscenes.md): the wizards a deed has freed whose cutscene has not
	/// played yet, by character id, in the order they were freed. Written the moment a deed frees one, cleared as
	/// each reel ends after a run. No schema bump: it only adds, a save without the key reads as owing nothing,
	/// and nothing that was freed before it existed is owed (which is why it is a debt and not a "seen" list).
	/// </summary>
	public List<string> FreeingsOwed { get; set; } = new();

	// --- The story's epoch (.ai/decisions/2026-11-02-story-resets.md) ---------------------------------

	/// <summary>
	/// Which telling of the story this build is. Bumped by hand, for a release that changes the story so
	/// much that progress earned under the old telling no longer means anything (Eric, 2026-11-02: "reset
	/// progress for people when we push an update that affects the progress of the story"). A save from an
	/// earlier epoch is not migrated: SaveManager copies it aside and starts it again, keeping only the
	/// settings (<see cref="SettingsKeys"/>). release.sh writes this into version.json as "progressEpoch",
	/// which is how UpdateScreen and UpdateBanner warn a player before the download. Raising it is a release
	/// decision, never a side effect of other work: see .ai/releasing.md, "Releasing a story reset".
	/// </summary>
	/// <remarks>
	/// This is the one place a save is allowed to lose what it earned, and only on purpose. Every other
	/// change that narrows what a save grants still needs a schema step in <see cref="Migrate"/>.
	/// </remarks>
	public const int CurrentProgressEpoch = 2;

	/// <summary>
	/// The epoch of a save with no "ProgressEpoch" key - one written before epochs existed, whose story is
	/// the first telling. So it is never reset while the constant stands at 1, and is reset by the first
	/// bump: the "counts as epoch 0 once the constant passes 1" rule, without a special case.
	/// </summary>
	public const int FirstProgressEpoch = 1;

	/// <summary>The telling of the story this save's progress was earned in. New saves are in the current one.</summary>
	public int ProgressEpoch { get; set; } = CurrentProgressEpoch;

	/// <summary>
	/// Set by a story reset; cleared when the title screen has said so (TitleScreen's notice). Saved, so a
	/// player who quits before the title appears still hears it once.
	/// </summary>
	public bool ProgressResetNoticePending { get; set; } = false;

	/// <summary>
	/// The keys a story reset keeps: everything the Options screen sets (sound, damage numbers, the tips
	/// switch, the playtest toolkit and the difficulty preset) and nothing it does not. Every other key comes
	/// back as a brand-new save's, so a field added later is reset unless it is listed here - a setting
	/// lost is a nuisance, progress kept across a new story is the bug this exists to prevent.
	/// RegressionChecks.ValidateStoryReset holds the list to the save's real keys.
	/// </summary>
	public static readonly string[] SettingsKeys =
	{
		"MasterVolume", "MusicVolume", "SfxVolume", "AudioMuted", "ShowDamageNumbers", "ReduceScreenShake", "ReduceDarkness",
		"Fullscreen", "WindowWidth", "WindowHeight", "MonitorIndex",
		"EnableGameplayOnboardingTips", "HasToggledOnboardingTipsAtLeastOnce",
		"PlaytestModeEnabled", "BalancePresetId", "PlaytestChecklistState",
	};

	/// <summary>True when this save was earned in an earlier telling of the story than this build's.</summary>
	public bool IsFromEarlierStory => ProgressEpoch < CurrentProgressEpoch;

	/// <summary>
	/// The save this one becomes when the story starts again: a new save in the current epoch, carrying
	/// this one's settings and the title's notice. Pure - the caller keeps the old save on disk first.
	/// </summary>
	public SaveData StartNewStory()
	{
		// Through the dictionary, so SettingsKeys IS the rule rather than a description of a copy written
		// beside it. A save written by a newer epoch is never sent here; see SaveManager.LoadFrom.
		Godot.Collections.Dictionary old = ToGodotDictionary();
		Godot.Collections.Dictionary fresh = new SaveData().ToGodotDictionary();
		foreach (string key in SettingsKeys)
		{
			if (old.ContainsKey(key))
				fresh[key] = old[key];
		}
		SaveData result = FromVariant(fresh);
		result.ProgressEpoch = CurrentProgressEpoch;
		result.ProgressResetNoticePending = true;
		return result;
	}

	/// <summary>The key a seen ascension is stored under.</summary>
	public static string AscensionKey(string spellId, string branchId)
		=> $"{spellId?.Trim().ToLowerInvariant()}:{branchId?.Trim().ToLowerInvariant()}";

	/// <summary>
	/// Apotheosis forms that moved to another spell after a save could have seen them: the key now, to the key a
	/// save holds from before. A seen key is spell AND branch, so a form that changed spell would read as never
	/// seen, and the browser would seal again a form the player had already met. Imhotep's Eye of Ra moved from
	/// Fireball to Scorching Ray with his opener on 2026-11-09 (.ai/character-fit.md section 1), and John Dee's Spirit
	/// Mirror from Hollow Star to Folio with his the same day (section 6). Read in
	/// <see cref="IsAscensionSeen"/>, as the Familiar's renames are, so no save is rewritten and no schema step is needed.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> MovedAscensions =
		new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
		{
			[AscensionKey("scorching_ray", ScorchingRayBeam.EyeOfRaId)] = AscensionKey("fireball", "fireball_eye_of_ra"),
			[AscensionKey(Folio.SpellId, Folio.SpiritMirrorId)] = AscensionKey("hollow_star", Folio.HollowStarSpiritMirrorId),
		};

	public bool IsAscensionSeen(string spellId, string branchId)
	{
		if (string.IsNullOrWhiteSpace(spellId) || string.IsNullOrWhiteSpace(branchId))
			return false;
		string key = AscensionKey(spellId, branchId);
		if (SeenAscensionIds.Any(id => id.Equals(key, System.StringComparison.OrdinalIgnoreCase)))
			return true;
		// A form seen on the spell it lived on before it moved (MovedAscensions) is still seen.
		if (MovedAscensions.TryGetValue(key, out string before)
			&& SeenAscensionIds.Any(id => id.Equals(before, System.StringComparison.OrdinalIgnoreCase)))
			return true;
		// A milestone renamed since it was seen is still seen: the Familiar's Cerberus and Griffin were
		// "familiar_cerberus" and "familiar_griffin" before it had forms, and are each a form's own now
		// (Familiar.RenamedMilestones). Read here rather than rewritten into the save, so nothing about a
		// save changes and no schema step is needed - an old key simply keeps meaning what it meant.
		foreach (var renamed in Familiar.RenamedMilestones)
		{
			if (!renamed.Value.Equals(branchId.Trim(), System.StringComparison.OrdinalIgnoreCase))
				continue;
			string old = AscensionKey(spellId, renamed.Key);
			if (SeenAscensionIds.Any(id => id.Equals(old, System.StringComparison.OrdinalIgnoreCase)))
				return true;
		}
		return false;
	}

	/// <summary>Records a path as seen. True when it was not seen before (the caller saves).</summary>
	public bool MarkAscensionSeen(string spellId, string branchId)
	{
		if (string.IsNullOrWhiteSpace(spellId) || string.IsNullOrWhiteSpace(branchId)
			|| IsAscensionSeen(spellId, branchId))
			return false;
		SeenAscensionIds.Add(AscensionKey(spellId, branchId));
		return true;
	}

	public void RecordRunTelemetry(RunResult result)
	{
		if (result == null)
			return;

		RunTelemetryHistory.Insert(0, RunTelemetryRecord.FromRunResult(result));
		if (RunTelemetryHistory.Count > MaxTelemetryHistory)
			RunTelemetryHistory.RemoveRange(MaxTelemetryHistory, RunTelemetryHistory.Count - MaxTelemetryHistory);
	}

	/// <summary>
	/// Brings an older save up to <see cref="CurrentSchemaVersion"/>. Call once, immediately after
	/// loading, before anything reads unlock state.
	/// </summary>
	/// <remarks>
	/// Until now SchemaVersion was stored, read, and never once compared against anything - there was
	/// no migration step at all, and forward compatibility rested entirely on missing JSON keys
	/// falling back to property defaults.
	///
	/// That is not good enough for this change. Before schema 8 every spell in the game was unlocked
	/// by default, so UnlockedSpellIds was almost always empty - not because the player had earned
	/// nothing, but because there was nothing to earn. Shipping the campaign without this step would
	/// silently confiscate 27 spells from anyone with an existing save. So a pre-8 save is credited
	/// with everything the old default set contained, and loses nothing.
	/// </remarks>
	/// <param name="log">False for the validator's sample saves, so their migrations are not
	/// mistaken in the console for the player's own.</param>
	public bool Migrate(bool log = true)
	{
		if (SchemaVersion >= CurrentSchemaVersion)
		{
			// Also covers a save written by a *newer* build than this one: leave it alone rather than
			// rewriting fields this version does not understand.
			SchemaVersion = System.Math.Max(SchemaVersion, CurrentSchemaVersion);
			return false;
		}

		int from = SchemaVersion;

		// Stepwise, and it has to be. This used to be one block guarded only by "is the save old",
		// which works exactly once: the moment a second migration exists, a schema-8 save runs the
		// pre-8 grant as well and is handed content it never earned. Each step below is gated on the
		// version it was introduced at, so a save skips only what it already has.

		if (SchemaVersion < 8)
			MigrateToCampaignUnlocks();

		if (SchemaVersion < 9)
			MigratePassiveSpellsToBoons();

		if (SchemaVersion < 10)
			MigrateDiscoveriesIntoCompletedCampaign();

		if (SchemaVersion < 11)
			MigrateToSeenAscensions();
		// 11 is main's (SeenAscensionIds); John Dee is 12, so the two stack whichever lands first.
		if (SchemaVersion < 12)
			MigrateCollectorIntoCompletedCampaign();

		if (SchemaVersion < 13)
			MigrateToCampaignOrder();

		if (SchemaVersion < 14)
			MigrateStarfallIntoCompletedCampaign();

		if (SchemaVersion < 15)
			MigrateFamiliarIntoCompletedCampaign();

		if (SchemaVersion < 16)
			MigrateFacingSpellsIntoCompletedCampaign();

		if (SchemaVersion < 17)
			MigrateToSecrets();

		if (SchemaVersion < 18)
			MigrateRetiredLevelUpCharges();

		if (SchemaVersion < 19)
			MigrateToAchievementUnlocks();

		if (SchemaVersion < 20)
			MigrateToLearnedAscensionsAndRepricedPowers();

		if (SchemaVersion < 21)
			MigrateDungeonIntoCastle();

		if (SchemaVersion < 22)
			MigrateToSlowStart();

		if (SchemaVersion < 23)
			MigrateChronoweaveIntoTempestRhythm();

		// 24: Shadow Bolt onto the Conjunction. Its branch numbered this 23 in parallel with the
		// Chronoweave merge; saves in the wild already carry 23, so it runs as 24.
		if (SchemaVersion < 24)
			MigrateShadowBoltToConjunction();

		if (SchemaVersion < 25)
			MigrateToBoonRework();

		if (SchemaVersion < 26)
			MigrateRiteToConjunction();

		// 27: the Conjunction ladder's pin (.ai/conjunction-progression.md, rung I). Adds the field and pins nothing.
		if (SchemaVersion < 27)
			MigrateToPinnedRecipe();

		if (log)
			GD.Print($"SaveData: migrated schema {from} -> {CurrentSchemaVersion}; " +
				$"{UnlockedSpellIds.Count} spells, {UnlockedBoonIds.Count} boons, {UnlockedCharacterIds.Count} wizards, " +
				$"{UnlockedStageIds.Count} chapters opened.");
		SchemaVersion = CurrentSchemaVersion;
		return true;
	}

	/// <summary>Schema 8: nothing used to be earned, so credit a pre-campaign save with everything.</summary>
	private void MigrateToCampaignUnlocks()
	{
		foreach (string spellId in GlobalStatsManager.LegacyDefaultUnlockedSpellIds)
		{
			if (!UnlockedSpellIds.Any(id => id.Equals(spellId, System.StringComparison.OrdinalIgnoreCase)))
				UnlockedSpellIds.Add(spellId);
		}

		// Characters were likewise all free, and the roster is small enough that granting it outright
		// is kinder than asking a returning player to re-earn wizards they have already played.
		//
		// Discovery wizards are excluded: they did not exist when everything was free, and a wizard
		// rescued from a cell is earned by going there (.ai/side-events.md), not handed to an old save.
		//
		// From the frozen lists, not the live catalog. Since schema 19 the rescued wizards are
		// Achievement entries, and a live "is it a Discovery" test would hand Tituba and Vainamoinen to
		// every pre-campaign save that loads.
		foreach (string characterId in WizardsAtSchema18)
		{
			// Only the wizards that existed when everything was free, which were the starters. A
			// wizard added since - John Dee, at 12 - is earned, not handed to an old save.
			if (DiscoveryWizardsAtSchema18.Contains(characterId, System.StringComparer.OrdinalIgnoreCase)
				|| WizardsAddedAtSchema12.Contains(characterId, System.StringComparer.OrdinalIgnoreCase))
				continue;
			if (!UnlockedCharacterIds.Any(id => id.Equals(characterId, System.StringComparison.OrdinalIgnoreCase)))
				UnlockedCharacterIds.Add(characterId);
		}
	}

	/// <summary>
	/// Schema 10: side events added five Discovery unlocks, which the final chapter's gate counts.
	/// </summary>
	/// <remarks>
	/// The Emberdeep's gate is evaluated live from "every spell recovered and every wizard freed", so
	/// adding five new things to recover would lock a player out of a chapter they had already
	/// opened. A save that met the gate as it stood before - everything that is not a Discovery
	/// unlock - is credited with the discoveries too. Any other save loses nothing and gains nothing.
	/// </remarks>
	private void MigrateDiscoveriesIntoCompletedCampaign()
	{
		// The gate as it stood at 9: the wizards added since (at 12) are not part of it, or a save
		// that had everything at 9 would be refused its discoveries for want of a wizard who did not
		// exist yet.
		// From the frozen lists (schema 19): the discoveries are deeds' rewards now, and the catalog
		// may grow, so the live catalog no longer says what this step meant.
		bool hadEverything =
			SpellsAtSchema18.All(id => DiscoverySpellsAtSchema18.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| SpellsAddedAtSchema14.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| SpellsAddedAtSchema15.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| SpellsAddedAtSchema16.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| HadSpellAtSchema18(id))
			&& WizardsAtSchema18.All(id => DiscoveryWizardsAtSchema18.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| WizardsAddedAtSchema12.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| HadWizardAtSchema18(id));
		if (!hadEverything)
			return;

		foreach (string characterId in DiscoveryWizardsAtSchema18)
			GlobalStatsManager.UnlockCharacter(this, characterId);
		foreach (string spellId in DiscoverySpellsAtSchema18)
			GlobalStatsManager.UnlockSpell(this, spellId);
	}

	/// <summary>
	/// Schema 13: the campaign order (.ai/campaign.md) - the Cursed Dungeon moved from second to fifth,
	/// two new chapters went in before the Cave, and which boss opens which chapter is now read from
	/// StageCatalog's order instead of being written on each boss.
	/// </summary>
	/// <remarks>
	/// Nothing is taken away: every id in UnlockedStageIds stays, so a chapter a save could enter it
	/// can still enter. What has to be ADDED is what the save's victories open under the new order,
	/// which the old order never wrote. A save that had beaten the Gaoler had the Cave (what the
	/// Gaoler used to open); now the Gaoler opens the Blighted Swamp, and a player who has beaten him
	/// keeps everything after him reachable.
	///
	/// So: find every boss the save has beaten, and open what each opens now. A beaten boss is known
	/// three ways, any of which is enough - LifetimeStats.BossesDefeated (kept since schema 8), the
	/// boss-clear deeds in UnlockedAchievementIds, and the chapters the OLD order opened, since a
	/// boss victory was the only thing that ever wrote one (<see cref="OpenedBySchema12"/>).
	///
	/// Two things are deliberately NOT done here, because GlobalStatsManager.IsStageEarned derives
	/// them live: a chapter BEFORE one the save has opened is open (so the Cave opens for a save that
	/// only had the Dungeon, and the two early chapters will open by themselves for everyone past
	/// them the day they become playable - no second migration then), and a chapter whose previous
	/// boss is in the lifetime record is open.
	///
	/// "What each opens now" is FROZEN since schema 21 (<see cref="OpenedAtSchema19"/>): this step read the
	/// live order until the Dungeon moved into the castle, and the live order would now have the Gaoler
	/// open nothing (or the Court, once it is built) and the Choir open the Swamp - not what schema 13
	/// meant, and the schema 21 step reads the save in the terms this step wrote it in.
	/// </remarks>
	private void MigrateToCampaignOrder()
	{
		var beaten = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
		foreach (string bossId in Lifetime?.BossesDefeated ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(bossId))
				beaten.Add(bossId);
		}
		foreach (string achievementId in UnlockedAchievementIds)
		{
			string bossId = AchievementDefinitions.GetById(achievementId)?.BossId;
			if (!string.IsNullOrWhiteSpace(bossId))
				beaten.Add(bossId);
		}
		foreach ((string stageId, string bossId) in OpenedBySchema12)
		{
			if (UnlockedStageIds.Any(id => id.Equals(stageId, System.StringComparison.OrdinalIgnoreCase)))
				beaten.Add(bossId);
		}

		foreach (string bossId in beaten)
		{
			string opened = OpenedAtSchema19(bossId);
			if (opened != null)
				GlobalStatsManager.UnlockStage(this, opened);
		}
	}

	/// <summary>
	/// What each boss opened up to schema 12 - BossDefinition.UnlocksStageId as it last stood, the
	/// only writer UnlockedStageIds ever had. Frozen here so the migration reads a save in the terms
	/// it was written in, whatever the order becomes. The Long Coil and the Sketchbook opened nothing.
	/// </summary>
	private static readonly (string StageId, string BossId)[] OpenedBySchema12 =
	{
		("stage_1", "forest_treant"),
		("stage_2", "castle_warden"),
		("stage_3", "cave_choir"),
		("stage_4", "swamp_mother"),
		("stage_5", "ruins_sentinel"),
		("stage_6", "frost_warden"),
	};

	/// <summary>Wizards that joined the roster at schema 12. The steps before it must neither count nor grant them.</summary>
	private static readonly string[] WizardsAddedAtSchema12 = { "john_dee" };

	/// <summary>
	/// Schema 12: John Dee joined the roster, freed by opening chests (the "Mortlake" achievement).
	/// </summary>
	/// <remarks>
	/// The same reason as schema 10: the Emberdeep's gate is "every wizard freed", evaluated live, so a
	/// new wizard would close a chapter a player had already opened - until the end of their next run,
	/// when the achievement (measured on lifetime chests, which such a player has long since opened)
	/// would hand him over. A save that met the gate as it stood before 12 is credited with him now.
	/// Any other save is untouched: he is earned the ordinary way.
	/// </remarks>
	private void MigrateCollectorIntoCompletedCampaign()
	{
		bool hadEverything =
			SpellsAtSchema18.All(id => SpellsAddedAtSchema14.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| SpellsAddedAtSchema15.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| SpellsAddedAtSchema16.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| HadSpellAtSchema18(id))
			&& WizardsAtSchema18.All(id => WizardsAddedAtSchema12.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| HadWizardAtSchema18(id));
		if (!hadEverything)
			return;

		foreach (string characterId in WizardsAddedAtSchema12)
			GlobalStatsManager.UnlockCharacter(this, characterId);
	}

	/// <summary>Spells that joined the catalog at schema 14. The steps before it must neither count nor grant them.</summary>
	private static readonly string[] SpellsAddedAtSchema14 = { "starfall" };

	/// <summary>
	/// Schema 14: Starfall joined the shop.
	/// </summary>
	/// <remarks>
	/// Schema 10's and 12's reason again: the final chapter's gate is "every spell recovered and every
	/// wizard freed", evaluated live, so a new spell in the catalog would close a chapter a player had
	/// already opened until they bought it. A save that met the gate as it stood before 14 is credited
	/// with the new spell now. Any other save is untouched: it buys Starfall like anyone else.
	/// </remarks>
	private void MigrateStarfallIntoCompletedCampaign()
	{
		bool hadEverything =
			SpellsAtSchema18.All(id => SpellsAddedAtSchema14.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| SpellsAddedAtSchema15.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| SpellsAddedAtSchema16.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| HadSpellAtSchema18(id))
			&& WizardsAtSchema18.All(HadWizardAtSchema18);
		if (!hadEverything)
			return;

		foreach (string spellId in SpellsAddedAtSchema14)
			GlobalStatsManager.UnlockSpell(this, spellId);
	}

	/// <summary>Spells that joined the catalog at schema 15. The steps before it must neither count nor grant them.</summary>
	private static readonly string[] SpellsAddedAtSchema15 = { "familiar" };

	/// <summary>
	/// Schema 15: the Familiar joined the shop.
	/// </summary>
	/// <remarks>
	/// Schema 14's reason again (and 10's and 12's): the final chapter's gate counts every spell in the
	/// catalog, live, so a new one would close a chapter a player had already opened until they bought
	/// it. A save that met the gate as it stood before 15 is credited with the new spell now; any other
	/// save buys it like anyone else.
	/// </remarks>
	private void MigrateFamiliarIntoCompletedCampaign()
	{
		bool hadEverything =
			SpellsAtSchema18.All(id => SpellsAddedAtSchema15.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| SpellsAddedAtSchema16.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| HadSpellAtSchema18(id))
			&& WizardsAtSchema18.All(HadWizardAtSchema18);
		if (!hadEverything)
			return;

		foreach (string spellId in SpellsAddedAtSchema15)
			GlobalStatsManager.UnlockSpell(this, spellId);
	}

	/// <summary>Spells that joined the catalog at schema 16. The steps before it must neither count nor grant them.</summary>
	private static readonly string[] SpellsAddedAtSchema16 = { "stormsweep", "sleet_runes" };

	/// <summary>
	/// Schema 16: the facing family - Stormsweep and Sleet Runes - joined the shop.
	/// </summary>
	/// <remarks>
	/// Schema 14's reason again: the final chapter's gate counts every spell in the catalog, live, so two
	/// new ones would close a chapter a player had already opened until they bought both. A save that
	/// met the gate as it stood before 16 is credited with them now. Any other save is untouched. The
	/// same kind of grant as main's schema 15 (the Familiar), so the two can be folded into one step.
	/// </remarks>
	private void MigrateFacingSpellsIntoCompletedCampaign()
	{
		bool hadEverything =
			SpellsAtSchema18.All(id => SpellsAddedAtSchema16.Contains(id, System.StringComparer.OrdinalIgnoreCase)
				|| HadSpellAtSchema18(id))
			&& WizardsAtSchema18.All(HadWizardAtSchema18);
		if (!hadEverything)
			return;

		foreach (string spellId in SpellsAddedAtSchema16)
			GlobalStatsManager.UnlockSpell(this, spellId);
	}

	/// <summary>
	/// Schema 11: the ascension browser seals a path until its requirements have been met once.
	/// </summary>
	/// <remarks>
	/// Starts every older save EMPTY, which is the honest answer rather than a cautious one: nothing
	/// a save wrote before this records an ascension met or taken. The run telemetry keeps how often
	/// each spell was upgraded, not which level 8 branch it took, and GameStats.ApotheosisIds is
	/// per-run and never saved - so there is nothing to seed from, and a guess would reveal paths the
	/// player never stood in front of. A returning player's sealed paths open on the first level-up
	/// that finds them met, which is the same moment a new player's do.
	/// </remarks>
	private void MigrateToSeenAscensions()
	{
		SeenAscensionIds ??= new List<string>();
	}

	/// <summary>
	/// Schema 17: the stage secrets' fields. Nothing to credit, on purpose: a secret pays nothing the
	/// campaign's gates count - never a spell, a wizard or a boon (.ai/stage-secrets.md section 1) - so
	/// no existing save can lose a chapter by not having found one.
	/// </summary>
	private void MigrateToSecrets()
	{
		FoundSecretIds ??= new List<string>();
		FoundUpgradeLevels ??= new Dictionary<string, int>();
	}

	/// <summary>
	/// The shop prices of the level-up charges removed on 2026-10-28 ("remove save and augury
	/// mechanics"): (base cost, cost added per level bought), as MainMenu registered them then. Kept
	/// here rather than read from the shop, so the refund never moves with the shop's prices - the
	/// rows are gone from the shop anyway.
	/// </summary>
	internal static readonly IReadOnlyDictionary<string, (int Base, int PerLevel)> RetiredLevelUpCharges =
		new Dictionary<string, (int Base, int PerLevel)>
		{
			["banked_levels"] = (110, 80), // Hoarded Insight: a saved level-up, two picks at the next
			["auguries"] = (130, 90),      // Augury: name a spell the next offer must hold
		};

	/// <summary>
	/// Schema 18: Save and Augury left the level-up screen, so every level bought of them is paid back
	/// in Arcane Energy at what it cost - level n (from 0) cost base + n x per-level - and the rows
	/// leave the save. A removal that kept the energy would be the silent confiscation CLAUDE.md warns
	/// of. Returns the refund, for the validator.
	/// </summary>
	internal int MigrateRetiredLevelUpCharges()
	{
		int refund = 0;
		foreach (KeyValuePair<string, (int Base, int PerLevel)> charge in RetiredLevelUpCharges)
		{
			if (!ArcaneUpgradeLevels.TryGetValue(charge.Key, out int levels))
				continue;
			for (int n = 0; n < levels; n++)
				refund += charge.Value.Base + n * charge.Value.PerLevel;
			ArcaneUpgradeLevels.Remove(charge.Key);
		}
		TotalCurrency += refund;
		return refund;
	}

	/// <summary>
	/// Schema 23 (Eric, 2026-11-07: "merge cooldown and attack speed - confusing to have both,
	/// especially in the power ups"). Chronoweave (-5% cooldown a level) is retired; Tempest Rhythm
	/// (+8% attack speed a level, cap 16) is the one clock Power. Nothing anybody had is taken away:
	/// every Chronoweave level becomes a Tempest Rhythm level, both upgrades having always cost the
	/// same (35 + 25 a level, frozen here the way RetiredLevelUpCharges freezes prices), and any
	/// level past the new cap is refunded at the top of the price curve - the levels past the cap
	/// were the dearest bought. Returns (moved, refund), for the validator.
	/// </summary>
	internal (int Moved, int Refund) MigrateChronoweaveIntoTempestRhythm()
	{
		const string Retired = "cooldowns";
		const string Kept = "attack_speed";
		const int Cap = 16;
		const int BaseCost = 35;
		const int CostPerLevel = 25;
		if (!ArcaneUpgradeLevels.TryGetValue(Retired, out int levels) || levels <= 0)
		{
			ArcaneUpgradeLevels.Remove(Retired);
			return (0, 0);
		}
		ArcaneUpgradeLevels.TryGetValue(Kept, out int kept);
		int room = Cap - kept > 0 ? Cap - kept : 0;
		int moved = levels < room ? levels : room;
		if (moved > 0)
			ArcaneUpgradeLevels[Kept] = kept + moved;
		int refund = 0;
		for (int n = levels - 1; n >= moved; n--)
			refund += BaseCost + n * CostPerLevel;
		ArcaneUpgradeLevels.Remove(Retired);
		TotalCurrency += refund;
		return (moved, refund);
	}

	/// <summary>
	/// Schema 25, the boon rework's (in-flight item 41). Runs used to hold six boon slots; they hold
	/// two now, and the shop's Reliquary Shelf sells the third and fourth. Nothing anybody had is
	/// taken away: a save that has played a run - every run it played held the six - is granted both
	/// shop slots outright, the same played-save test schema 20 grants AscensionLearned on. A fresh
	/// save never held the capacity, so it buys them like any Power. Returns the levels granted,
	/// for the validator.
	/// </summary>
	/// <summary>
	/// Schema 27: the Conjunction ladder's pin (ConjunctionLadder.Pin). Nothing is credited or taken: a save from before
	/// had no pin, so it has none now - written down rather than left to a default, so a stray value in an older file
	/// (there is none to find) could never pin a recipe the player did not choose. RegressionChecks.ValidatePinnedRecipeMigration.
	/// </summary>
	internal void MigrateToPinnedRecipe()
	{
		PinnedRecipeId = string.Empty;
	}

	internal int MigrateToBoonRework()
	{
		if ((Lifetime?.TotalRuns ?? 0) <= 0 && (RunTelemetryHistory?.Count ?? 0) <= 0)
			return 0;

		ArcaneUpgradeLevels.TryGetValue(GlobalStatsManager.BoonSlotsUpgradeId, out int held);
		const int BothSlots = 2;
		if (held >= BothSlots)
			return 0;
		ArcaneUpgradeLevels[GlobalStatsManager.BoonSlotsUpgradeId] = BothSlots;
		return BothSlots - held;
	}

	/// <summary>
	/// The Powers whose price came down with what a level pays (.ai/difficulty-curve.md): (base cost, cost
	/// per level) as the shop priced them before and after. Frozen here, as RetiredLevelUpCharges is, so
	/// the refund never moves with a later price.
	/// </summary>
	internal static readonly IReadOnlyDictionary<string, (int OldBase, int OldPerLevel, int NewBase, int NewPerLevel)> RepricedPowers =
		new Dictionary<string, (int, int, int, int)>
		{
			// Fate Fracture: +1 reroll at every level-up became +2 rerolls per run.
			["rerolls"] = (75, 50, 40, 25),
			// Emberheart: +0.4 HP a second a level became +0.05 (the healing budget).
			["recovery"] = (30, 20, 15, 10),
		};

	/// <summary>
	/// Schema 20 (.ai/difficulty-curve.md, section 9). Nothing anybody had is taken away:
	///   1. AscensionLearned for every save that has played a run - each had the Ascension in every run
	///      it played, and a new save now learns it from the Hayward;
	///   2. every level bought of a re-priced Power is paid back what it cost less what it costs now.
	///      The levels stay; what a level pays is the new rule. Returns the refund, for the validator.
	/// </summary>
	internal int MigrateToLearnedAscensionsAndRepricedPowers()
	{
		if ((Lifetime?.TotalRuns ?? 0) > 0 || (RunTelemetryHistory?.Count ?? 0) > 0)
			AscensionLearned = true;

		int refund = 0;
		foreach (var (id, price) in RepricedPowers)
		{
			if (!ArcaneUpgradeLevels.TryGetValue(id, out int levels))
				continue;
			for (int n = 0; n < levels; n++)
				refund += (price.OldBase + n * price.OldPerLevel) - (price.NewBase + n * price.NewPerLevel);
		}
		TotalCurrency += refund;
		return refund;
	}

	/// <summary>
	/// Schema 21 (.ai/endgame-castle.md 1.5): the Cursed Dungeon moved from fifth into the castle, between
	/// the Last Bridge and the Court. Nothing anybody could enter is closed, and the Dungeon's old place is
	/// evidence of nothing on the new road.
	/// </summary>
	/// <remarks>
	/// Why a step at all. The road's three rules (GlobalStatsManager.IsStageEarned) each read the Dungeon's
	/// old place as evidence: its id in the opened list; rule 2, which with the Dungeon eleventh would put a
	/// save that reached the old chapter five past the Swamp, the Sands and the bridge; and rule 3, under
	/// which every old Gaoler kill would open the Court. Taking the id out instead would close a chapter a
	/// save could enter. So the old evidence is kept, filed where it means only what it meant:
	///   1. write down what the save could enter under the order and rules as they stood until now, frozen
	///      (<see cref="ChaptersEnterableAtSchema19"/>) - every chapter but the Dungeon. That keeps rule 2's
	///      old gifts: the early chapters a save had BECAUSE the Dungeon stood fifth;
	///   2. if it could enter the Dungeon, the Dungeon leaves UnlockedStageIds for
	///      <see cref="GrandfatheredStageIds"/>: entry, and nothing else;
	///   3. nothing for the castle - no save can have been past it;
	///   4. the Boss Rush, which now waits on the Gaoler too (BossRushCatalog.IsOpen): a save that had it
	///      open (the Warden of the Deep beaten) keeps it, <see cref="BossRushGrandfathered"/>.
	/// Internal for RegressionChecks.ValidateDungeonMove, which runs it a second time on a migrated save to
	/// prove it changes nothing then.
	/// </remarks>
	internal void MigrateDungeonIntoCastle()
	{
		UnlockedStageIds ??= new List<string>();
		GrandfatheredStageIds ??= new List<string>();

		// Read before anything is written: the save as the old order saw it.
		List<string> enterable = ChaptersEnterableAtSchema19();
		foreach (string stageId in enterable)
		{
			if (!stageId.Equals(DungeonStageId, System.StringComparison.OrdinalIgnoreCase))
				GlobalStatsManager.UnlockStage(this, stageId);
		}
		if (enterable.Contains(DungeonStageId, System.StringComparer.OrdinalIgnoreCase))
		{
			UnlockedStageIds.RemoveAll(id => id.Equals(DungeonStageId, System.StringComparison.OrdinalIgnoreCase));
			if (!GrandfatheredStageIds.Contains(DungeonStageId, System.StringComparer.OrdinalIgnoreCase))
				GrandfatheredStageIds.Add(DungeonStageId);
		}

		if (Lifetime != null && Lifetime.HasDefeatedBoss(FinaleBossAtSchema19))
			BossRushGrandfathered = true;
	}

	/// <summary>The chapter that moved.</summary>
	private const string DungeonStageId = "stage_1";

	/// <summary>
	/// The campaign from schema 19 to 20 (13 to 20 but for the gate's kind): each chapter and the boss that
	/// is the evidence for the next. Every one was playable. The last is the gate chapter (its previous boss
	/// and <see cref="WizardsAtSchema18"/>, the seven A1 left the gate counting); every other after the first
	/// is opened by its previous boss. Frozen, so steps 13 and 21 read a save in the terms it was written
	/// in, whatever StageCatalog's order becomes.
	/// </summary>
	private static readonly (string StageId, string BossId)[] CampaignOrderAtSchema19 =
	{
		("stage_0", "forest_treant"),
		("stage_9", "fields_hayward"),
		("stage_10", "heath_ninth_stone"),
		("stage_2", "cave_choir"),
		("stage_1", "castle_warden"),
		("stage_3", "swamp_mother"),
		("stage_4", "ruins_sentinel"),
		("stage_5", "frost_warden"),
		("stage_6", "sands_coil"),
		("stage_7", "emberdeep_warden"),
	};

	/// <summary>The chapter held until that campaign was finished: the Sketchbook.</summary>
	private const string AfterCampaignAtSchema19 = "stage_8";

	/// <summary>That campaign's last boss: his fall finished it and opened the Boss Rush.</summary>
	private const string FinaleBossAtSchema19 = "emberdeep_warden";

	/// <summary>
	/// What beating <paramref name="bossId"/> opened under <see cref="CampaignOrderAtSchema19"/>: the next
	/// chapter, unless that is the gate (a victory never writes a gate open). Null for anything else.
	/// </summary>
	private static string OpenedAtSchema19(string bossId)
	{
		for (int i = 0; i < CampaignOrderAtSchema19.Length - 2; i++)
		{
			if (CampaignOrderAtSchema19[i].BossId.Equals(bossId ?? string.Empty, System.StringComparison.OrdinalIgnoreCase))
				return CampaignOrderAtSchema19[i + 1].StageId;
		}
		return null;
	}

	/// <summary>
	/// Every chapter this save could enter under <see cref="CampaignOrderAtSchema19"/> and the rules of 19
	/// and 20, frozen: the first chapter always; another by its id stored (rule 1), a later boss-gated
	/// chapter stored (rule 2) or its previous boss beaten (rule 3); the gate by rules 1 and 2 or its
	/// previous boss AND the seven wizards; the Sketchbook once stored, played, or the campaign finished.
	/// </summary>
	/// <remarks>
	/// "Stored" includes <see cref="GrandfatheredStageIds"/>: empty on the first pass, and on a second it
	/// holds the Dungeon that was stored when the first ran - so a second pass reads the same save the
	/// first did, and finds nothing to do. Internal for the validator.
	/// </remarks>
	internal List<string> ChaptersEnterableAtSchema19()
	{
		var stored = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
		foreach (string id in UnlockedStageIds ?? new List<string>())
			stored.Add(id);
		foreach (string id in GrandfatheredStageIds ?? new List<string>())
			stored.Add(id);
		bool Beaten(string bossId) => Lifetime != null && Lifetime.HasDefeatedBoss(bossId);

		var enterable = new List<string>();
		int gate = CampaignOrderAtSchema19.Length - 1;
		for (int i = 0; i < CampaignOrderAtSchema19.Length; i++)
		{
			string stageId = CampaignOrderAtSchema19[i].StageId;
			bool earned = i == 0 || stored.Contains(stageId);
			// Rule 2 counted only boss-gated chapters past this one, never the gate itself.
			for (int later = i + 1; !earned && later < gate; later++)
				earned = stored.Contains(CampaignOrderAtSchema19[later].StageId);
			if (!earned && i > 0)
			{
				bool previousBeaten = Beaten(CampaignOrderAtSchema19[i - 1].BossId);
				earned = i == gate ? previousBeaten && WizardsAtSchema18.All(HadWizardAtSchema18) : previousBeaten;
			}
			if (earned)
				enterable.Add(stageId);
		}

		if (stored.Contains(AfterCampaignAtSchema19) || Beaten(FinaleBossAtSchema19)
			|| (Lifetime != null && Lifetime.BestTimeOnStage(AfterCampaignAtSchema19) > 0f))
			enterable.Add(AfterCampaignAtSchema19);
		return enterable;
	}

	/// <summary>
	/// Schema 22, the slow start (.ai/slow-start.md, section 6). Nothing anybody had is taken away:
	///   1. FullBookKept for every save with a run on record (schema 20's population) - each played every run
	///      on six pages, and keeps six. The shop's Bound Pages reads as complete, with nothing charged or
	///      refunded;
	///   2. the Ascension, the Apotheosis and Conjunctions learned, cutscenes and all, for every save that had
	///      learned the Ascension under the rule it was taught by until now - the flag, the Hayward beaten, or
	///      a chapter past the Fields opened - because until now both of the others rode it;
	///   3. the Sovereign Rite's lesson for a save that already owns the Rite, so its cutscene does not play
	///      for a spell he has been using.
	/// The rule in 2 is frozen here, never read from GlobalStatsManager or the live campaign order: the step
	/// must say tomorrow what it said the day it shipped. Returns whether it learned the ladder.
	/// </summary>
	internal bool MigrateToSlowStart()
	{
		LessonsLearned ??= new List<string>();
		if ((Lifetime?.TotalRuns ?? 0) > 0 || (RunTelemetryHistory?.Count ?? 0) > 0)
			FullBookKept = true;

		bool hadAscension = AscensionLearned
			|| (Lifetime?.HasDefeatedBoss(SlowStartHaywardId) ?? false)
			|| UnlockedStageIds.Any(id => SlowStartPastFieldsStageIds.Contains(id, System.StringComparer.OrdinalIgnoreCase));
		if (hadAscension)
		{
			foreach (string id in SlowStartLadderIds)
			{
				if (!LessonsLearned.Contains(id, System.StringComparer.OrdinalIgnoreCase))
					LessonsLearned.Add(id);
			}
		}
		if (UnlockedSpellIds.Contains(SlowStartRiteSpellId, System.StringComparer.OrdinalIgnoreCase)
			&& !LessonsLearned.Contains(SlowStartRiteLessonId, System.StringComparer.OrdinalIgnoreCase))
			LessonsLearned.Add(SlowStartRiteLessonId);
		return hadAscension;
	}

	// Schema 22's frozen lists: the Hayward who taught the Ascension until now, every chapter after his in the
	// campaign order of 2026-11-02 (the old rule's "a chapter past the Fields opened"), the ladder's lesson ids
	// and the Rite's. Never read from the live catalogs.
	private const string SlowStartHaywardId = "fields_hayward";
	private static readonly string[] SlowStartPastFieldsStageIds =
		{ "stage_10", "stage_2", "stage_1", "stage_3", "stage_4", "stage_5", "stage_6", "stage_7" };
	private static readonly string[] SlowStartLadderIds = { "ascension", "apotheosis", "conjunctions" };
	private const string SlowStartRiteSpellId = "sovereign_rite";
	private const string SlowStartRiteLessonId = "sovereign_rite";

	/// <summary>
	/// Schema 23: Shadow Bolt became a Conjunction (in-flight item 17; .ai/combo-spells.md section 7). Its
	/// old source, the Gaoler's deed, pays only Bone Rattle now, and the spell is made in a run from Void
	/// Lance and Contagion. A save that owned it keeps it: its recipe's derived Feat is recorded done -
	/// quietly, as schema 19 recorded owned facts - so the spell stays granted, its Spell Index page keeps
	/// its name, and the recipe is known rather than offered as new. No Arcane Energy changes hands (the
	/// Feat pays the spell and nothing else). Frozen ids below, never the live catalogs. True when it did
	/// anything (RegressionChecks.ValidateShadowBoltMigration).
	/// </summary>
	internal bool MigrateShadowBoltToConjunction()
	{
		bool owned = UnlockedAchievementIds.Contains(ShadowBoltOldDeedId, System.StringComparer.OrdinalIgnoreCase)
			|| UnlockedSpellIds.Contains(ShadowBoltSpellId, System.StringComparer.OrdinalIgnoreCase);
		if (!owned)
			return false;
		bool changed = false;
		if (!UnlockedAchievementIds.Contains(ShadowBoltFeatId, System.StringComparer.OrdinalIgnoreCase))
		{
			UnlockedAchievementIds.Add(ShadowBoltFeatId);
			changed = true;
		}
		// The deed done is granted its unlock, as schema 19 reconciled: a save whose Gaoler fell before
		// spells were written into UnlockedSpellIds still owns this one.
		if (!UnlockedSpellIds.Contains(ShadowBoltSpellId, System.StringComparer.OrdinalIgnoreCase))
		{
			UnlockedSpellIds.Add(ShadowBoltSpellId);
			changed = true;
		}
		return changed;
	}

	// Schema 23's frozen ids: the spell, the deed that paid it until then, and its recipe's derived Feat.
	private const string ShadowBoltSpellId = "shadow_bolt";
	private const string ShadowBoltOldDeedId = "castle_conqueror";
	private const string ShadowBoltFeatId = "conjunction_shadow_bolt";

	/// <summary>
	/// Schema 26: the Sovereign Rite became a Conjunction of its own (Eric, 2026-11-08; .ai/sovereign-spell.md
	/// section 9) - Arcane Explosion down Resonance and Gravewell down Mass Grave, made in a run and paid by its
	/// recipe's derived Feat - and the Warden of the Deep's deed pays only its Arcane Energy now. A save that owned
	/// the Rite keeps it, as schema 24 kept Shadow Bolt: the spell stays granted and its Feat is recorded done -
	/// quietly, so the recipe is known rather than offered as new, its Codex page keeps its name, and no Arcane
	/// Energy changes hands. "Owned" is the spell in the save, the Warden's deed done, or the Warden beaten (the
	/// deed promised the Rite to a save that beat him, paid at the reconcile after its next run, which a save
	/// migrated before that run has not had). What the Rite needed to be MADE changes for everyone - its two
	/// parents, and Conjunctions learned - and that is the design, not a confiscation: this grants neither parent
	/// and teaches nothing. Frozen ids below, never the live catalogs. True when it did anything
	/// (RegressionChecks.ValidateRiteMigration).
	/// </summary>
	internal bool MigrateRiteToConjunction()
	{
		bool owned = UnlockedSpellIds.Contains(RiteSpellId, System.StringComparer.OrdinalIgnoreCase)
			|| UnlockedAchievementIds.Contains(RiteOldDeedId, System.StringComparer.OrdinalIgnoreCase)
			|| (Lifetime?.HasDefeatedBoss(RiteOldBossId) ?? false);
		if (!owned)
			return false;
		bool changed = false;
		if (!UnlockedAchievementIds.Contains(RiteFeatId, System.StringComparer.OrdinalIgnoreCase))
		{
			UnlockedAchievementIds.Add(RiteFeatId);
			changed = true;
		}
		if (!UnlockedSpellIds.Contains(RiteSpellId, System.StringComparer.OrdinalIgnoreCase))
		{
			UnlockedSpellIds.Add(RiteSpellId);
			changed = true;
		}
		return changed;
	}

	// Schema 26's frozen ids: the Rite, the deed that paid it from 2026-11-02 to 2026-11-08, that deed's boss, and
	// the recipe's derived Feat.
	private const string RiteSpellId = "sovereign_rite";
	private const string RiteOldDeedId = "emberdeep_ended";
	private const string RiteOldBossId = "emberdeep_warden";
	private const string RiteFeatId = "conjunction_sovereign_rite";

	/// <summary>
	/// Schema 19: every spell that is not a starter is an achievement's (.ai/achievements-and-secrets.md
	/// section 3). It only adds: nothing anybody owns is taken away, and no Arcane Energy changes hands.
	/// </summary>
	/// <remarks>
	/// The steps, in the doc's order:
	///   1. initialise GlimpsedSecretIds and LifetimeStats.EventsWon;
	///   2. seed EventsWon from what the save owns, through the seven discovery unlocks as they stood at
	///      18 (<see cref="DiscoveriesAtSchema18"/>), and record as done - quietly - every event and secret
	///      deed whose fact the save already holds, so the next run's end does not announce as new an
	///      event won a month ago. A save credited with discoveries by schema 10, or with Wishing Coin and
	///      Gilded Mote by schema 9's payback, owns them and so has their deeds: what it was credited with;
	///   3. reconcile: every deed done is granted its current unlock (Survivor done gets Hollow Star,
	///      Unwound done gets Fireball), never its Arcane Energy - The Long Road Back done keeps its Fireball
	///      and is not paid the 300 its deed pays now;
	///   4. a save that met the schema 18 gate - every spell of the 35 and every wizard of the 7, frozen
	///      here - gets the Emberdeep written into its opened list, which the gate honours (rule 1), so
	///      whatever the gate is, a save that could enter the Emberdeep still can; and any gate chapter
	///      met under the new rule is written open too;
	///   5. shop spells: nothing. A bought spell is in UnlockedSpellIds and stays; its vault still pays the
	///      run reward and the page mark when found.
	/// Internal for RegressionChecks.ValidateAchievementMigration, which runs it a second time on a
	/// migrated save to prove it changes nothing then.
	/// </remarks>
	internal void MigrateToAchievementUnlocks()
	{
		// Read before anything is added: the old gate as the save stood at 18.
		bool metOldGate = MetFinalGateAtSchema18();

		GlimpsedSecretIds ??= new List<string>();
		Lifetime ??= new LifetimeStats();
		Lifetime.EventsWon ??= new List<string>();

		foreach ((string siteId, UnlockKind kind, string id) in DiscoveriesAtSchema18)
		{
			bool owned = kind switch
			{
				UnlockKind.Character => UnlockedCharacterIds.Any(c => c.Equals(id, System.StringComparison.OrdinalIgnoreCase)),
				UnlockKind.Boon => UnlockedBoonIds.Any(b => b.Equals(id, System.StringComparison.OrdinalIgnoreCase)),
				_ => UnlockedSpellIds.Any(s => s.Equals(id, System.StringComparison.OrdinalIgnoreCase)),
			};
			if (owned)
				Lifetime.RecordEventWon(siteId);
		}

		AchievementContext context = AchievementContext.ForSave(this);
		foreach (AchievementDefinition deed in AchievementDefinitions.All)
		{
			if (deed.CompletesInRun && !GlobalStatsManager.IsAchievementUnlocked(this, deed.Id) && deed.IsComplete(context))
				UnlockedAchievementIds.Add(deed.Id);
		}

		AchievementManager.Reconcile(this);

		if (metOldGate)
			GlobalStatsManager.UnlockStage(this, FinalChapterAtSchema18);
		GlobalStatsManager.RecordMetGates(this);
	}

	/// <summary>
	/// The seven discovery unlocks as they stood at schema 18 - (site, kind, id) - frozen here so the
	/// schema 19 step reads a save in the terms it was written in, whatever UnlockCatalog becomes.
	/// </summary>
	internal static readonly (string SiteId, UnlockKind Kind, string Id)[] DiscoveriesAtSchema18 =
	{
		("the_grove", UnlockKind.Spell, "bramble_seed"),
		("bog_lanterns", UnlockKind.Spell, "contagion"),
		("caravan_master", UnlockKind.Spell, "iron_palisade"),
		("gaolers_key", UnlockKind.Character, "tituba"),
		("the_thaw", UnlockKind.Character, "vainamoinen"),
		("the_well", UnlockKind.Boon, "wishing_coin"),
		("the_moot", UnlockKind.Boon, "gilded_mote"),
	};

	/// <summary>
	/// Every spell in the catalog at schema 18, the 35 the old final gate counted. The steps before 19 read
	/// this rather than UnlockCatalog.AllSpellIds: they used to read the live list, so a spell added later
	/// would have changed what a step that already shipped did to an old save - which is why each new spell
	/// needed a "SpellsAddedAtSchemaN" list. A new spell needs nothing now.
	/// </summary>
	private static readonly string[] SpellsAtSchema18 =
	{
		"cinderbreath", "cone_of_cold", "chain_lightning", "obsidian_spike", "magic_missile", "aegis_ward",
		"arcane_explosion", "frost_shard", "meteor_swarm", "solar_flare", "cyclone_slash", "void_lance",
		"scorching_ray", "glacial_spike", "toxic_spore_burst", "black_tentacles", "gale_blade", "thorn_vine",
		"shadow_bolt", "spiritual_weapon", "riptide", "mirefoot", "fireball", "molten_shard", "gravewell",
		"hollow_star", "kindled_ward", "hunters_draw", "starfall", "familiar", "stormsweep", "sleet_runes",
		"iron_palisade", "contagion", "bramble_seed",
	};

	/// <summary>The starters among them at 18, owned by every save without being listed in it.</summary>
	private static readonly string[] StarterSpellsAtSchema18 =
	{
		"cinderbreath", "cone_of_cold", "chain_lightning", "obsidian_spike", "magic_missile", "aegis_ward",
	};

	/// <summary>The seven wizards the old final gate counted (Laelaps was outside it, the sandbox is no wizard).</summary>
	private static readonly string[] WizardsAtSchema18 =
	{
		"pyromancer", "frostweaver", "stormcaller", "geomancer", "tituba", "vainamoinen", "john_dee",
	};

	private static readonly string[] StarterWizardsAtSchema18 = { "pyromancer", "frostweaver", "stormcaller", "geomancer" };

	/// <summary>The two wizards found in the world at 18 - Discovery entries then, deeds' rewards since 19.</summary>
	private static readonly string[] DiscoveryWizardsAtSchema18 = { "tituba", "vainamoinen" };

	/// <summary>The three spells found in the world at 18.</summary>
	private static readonly string[] DiscoverySpellsAtSchema18 = { "iron_palisade", "contagion", "bramble_seed" };

	/// <summary>The chapter the old gate opened.</summary>
	private const string FinalChapterAtSchema18 = "stage_7";

	private bool HadSpellAtSchema18(string spellId) =>
		StarterSpellsAtSchema18.Contains(spellId, System.StringComparer.OrdinalIgnoreCase)
		|| UnlockedSpellIds.Any(id => id.Equals(spellId, System.StringComparison.OrdinalIgnoreCase));

	private bool HadWizardAtSchema18(string wizardId) =>
		StarterWizardsAtSchema18.Contains(wizardId, System.StringComparer.OrdinalIgnoreCase)
		|| UnlockedCharacterIds.Any(id => id.Equals(wizardId, System.StringComparison.OrdinalIgnoreCase));

	/// <summary>The schema 18 gate: every spell and every wizard, in the frozen lists.</summary>
	internal bool MetFinalGateAtSchema18() =>
		SpellsAtSchema18.All(HadSpellAtSchema18) && WizardsAtSchema18.All(HadWizardAtSchema18);

	/// <summary>
	/// Schema 9: passive spells left the game and boons replaced them.
	/// </summary>
	/// <remarks>
	/// Two jobs. Strip the eleven dead ids, because a save carrying references to spells that no
	/// longer exist keeps offering them to a validator that will rightly complain. And pay back what
	/// was stripped: a player who had bought or earned a passive is credited with a boon instead,
	/// rather than silently losing currency they had already spent.
	/// </remarks>
	private void MigratePassiveSpellsToBoons()
	{
		int owned = UnlockedSpellIds.Count(id => RetiredPassiveSpellIds.Contains(id, System.StringComparer.OrdinalIgnoreCase));

		UnlockedSpellIds.RemoveAll(id => RetiredPassiveSpellIds.Contains(id, System.StringComparer.OrdinalIgnoreCase));
		RemovedSpellIds.RemoveAll(id => RetiredPassiveSpellIds.Contains(id, System.StringComparer.OrdinalIgnoreCase));

		// Paid back as purchasable boons, cheapest first, one for one. Starter boons are free to
		// everybody so they are not worth crediting, and an achievement boon should still be earned.
		//
		// From a FROZEN list - the purchasable boons as they stood at schema 9 - not from the catalog
		// as it is now. Schema 13 moved four of the seven to the early chapters (.ai/campaign.md), and
		// reading the live catalog would have quietly cut this payback from seven boons to three for
		// anyone still migrating up from 8.
		foreach (string boonId in PurchasableBoonsAtSchema9.Take(owned))
		{
			if (!UnlockedBoonIds.Any(id => id.Equals(boonId, System.StringComparison.OrdinalIgnoreCase)))
				UnlockedBoonIds.Add(boonId);
		}
	}

	/// <summary>The schema-9 payback's boons, cheapest first, exactly as the catalog priced them then.</summary>
	private static readonly string[] PurchasableBoonsAtSchema9 =
	{
		"quicksilver_bead", "thornseed", "deepwater_pearl", "shadegrease", "saltbound_chain",
		"gilded_mote", "wishing_coin",
	};

	/// <summary>The eleven passive spells that schema 9 removed. Kept only so a save can be cleaned.</summary>
	private static readonly string[] RetiredPassiveSpellIds =
	{
		"aegis_ward", "thornmail_barrier", "frozen_bulwark", "stormguard_aura", "venom_cloak",
		"guardian_vines", "tidal_barrier", "stone_bulwark", "blur", "fortunes_favor", "haste",
	};

	public Godot.Collections.Dictionary ToGodotDictionary()
	{
		var characterIds = new Godot.Collections.Array<string>();
		foreach (string id in UnlockedCharacterIds)
		{
			characterIds.Add(id);
		}

		var stageIds = new Godot.Collections.Array<string>();
		foreach (string id in UnlockedStageIds)
		{
			stageIds.Add(id);
		}

		var grandfatheredStageIds = new Godot.Collections.Array<string>();
		foreach (string id in GrandfatheredStageIds)
		{
			grandfatheredStageIds.Add(id);
		}

		var spellIds = new Godot.Collections.Array<string>();
		foreach (string id in UnlockedSpellIds)
		{
			spellIds.Add(id);
		}

		var boonIds = new Godot.Collections.Array<string>();
		foreach (string id in UnlockedBoonIds)
		{
			boonIds.Add(id);
		}

		var achievementIds = new Godot.Collections.Array<string>();
		foreach (string id in UnlockedAchievementIds)
		{
			achievementIds.Add(id);
		}

		var removedSpellIds = new Godot.Collections.Array<string>();
		foreach (string id in RemovedSpellIds)
		{
			removedSpellIds.Add(id);
		}

		var seenAscensionIds = new Godot.Collections.Array<string>();
		foreach (string id in SeenAscensionIds)
		{
			seenAscensionIds.Add(id);
		}

		var foundSecretIds = new Godot.Collections.Array<string>();
		foreach (string id in FoundSecretIds)
		{
			foundSecretIds.Add(id);
		}

		var glimpsedSecretIds = new Godot.Collections.Array<string>();
		foreach (string id in GlimpsedSecretIds)
		{
			glimpsedSecretIds.Add(id);
		}

		var trackedQuestIds = new Godot.Collections.Array<string>();
		foreach (string id in TrackedQuestIds)
		{
			trackedQuestIds.Add(id);
		}

		var seenChapterNoteIds = new Godot.Collections.Array<string>();
		foreach (string id in SeenChapterNoteIds)
		{
			seenChapterNoteIds.Add(id);
		}

		var seenItemIds = new Godot.Collections.Array<string>();
		foreach (string id in SeenItemIds)
			seenItemIds.Add(id);

		var lessonsLearned = new Godot.Collections.Array<string>();
		foreach (string id in LessonsLearned ?? new List<string>())
			lessonsLearned.Add(id);

		var freeingsOwed = new Godot.Collections.Array<string>();
		foreach (string id in FreeingsOwed ?? new List<string>())
			freeingsOwed.Add(id);

		var foundUpgradeLevels = new Godot.Collections.Dictionary<string, int>();
		foreach (KeyValuePair<string, int> entry in FoundUpgradeLevels)
		{
			foundUpgradeLevels[entry.Key] = entry.Value;
		}

		var difficultyMap = new Godot.Collections.Dictionary<string, int>();
		foreach (KeyValuePair<string, int> entry in MaxDifficultyCleared)
		{
			difficultyMap[entry.Key] = entry.Value;
		}

		var arcaneUpgradeLevels = new Godot.Collections.Dictionary<string, int>();
		foreach (KeyValuePair<string, int> entry in ArcaneUpgradeLevels)
		{
			arcaneUpgradeLevels[entry.Key] = entry.Value;
		}

		var telemetryHistory = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (RunTelemetryRecord record in RunTelemetryHistory)
		{
			telemetryHistory.Add(record.ToGodotDictionary());
		}

		var playtestChecklistState = new Godot.Collections.Dictionary<string, bool>();
		foreach (KeyValuePair<string, bool> entry in PlaytestChecklistState)
		{
			playtestChecklistState[entry.Key] = entry.Value;
		}

		// Endless (EndlessCatalog): the records.
		var endlessRecords = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (EndlessRecord record in EndlessRecords ?? new List<EndlessRecord>())
			endlessRecords.Add(record.ToGodotDictionary());

		return new Godot.Collections.Dictionary
		{
			["SchemaVersion"] = SchemaVersion,
			["ProgressEpoch"] = ProgressEpoch,
			["ProgressResetNoticePending"] = ProgressResetNoticePending,
			["TotalCurrency"] = TotalCurrency,
			["UnlockedCharacterIds"] = characterIds,
			["UnlockedStageIds"] = stageIds,
			["GrandfatheredStageIds"] = grandfatheredStageIds,
			["BossRushGrandfathered"] = BossRushGrandfathered,
			["UnlockedSpellIds"] = spellIds,
			["UnlockedBoonIds"] = boonIds,
			["UnlockedAchievementIds"] = achievementIds,
			["RemovedSpellIds"] = removedSpellIds,
			["SeenAscensionIds"] = seenAscensionIds,
			["MaxDifficultyCleared"] = difficultyMap,
			["ArcaneUpgradeLevels"] = arcaneUpgradeLevels,
			["FoundSecretIds"] = foundSecretIds,
			["FoundUpgradeLevels"] = foundUpgradeLevels,
			["GlimpsedSecretIds"] = glimpsedSecretIds,
			["BossRushBestFelled"] = BossRushBestFelled,
			["BossRushBestClearSeconds"] = BossRushBestClearSeconds,
			["EndlessRecords"] = endlessRecords,
			["HasKeptVigil"] = HasKeptVigil,
			["RunTelemetryHistory"] = telemetryHistory,
			["Lifetime"] = Lifetime.ToGodotDictionary(),
			["HasSeenGameplayOnboarding"] = HasSeenGameplayOnboarding,
			["HasSeenPrologue"] = HasSeenPrologue,
			["HasSeenLightGoesOn"] = HasSeenLightGoesOn,
			["TrackedQuestIds"] = trackedQuestIds,
			["SeenChapterNoteIds"] = seenChapterNoteIds,
			["SeenItemIds"] = seenItemIds,
			["EnableGameplayOnboardingTips"] = EnableGameplayOnboardingTips,
			["HasToggledOnboardingTipsAtLeastOnce"] = HasToggledOnboardingTipsAtLeastOnce,
			["PlaytestModeEnabled"] = PlaytestModeEnabled,
			["MasterVolume"] = MasterVolume,
			["MusicVolume"] = MusicVolume,
			["SfxVolume"] = SfxVolume,
			["AudioMuted"] = AudioMuted,
			["ShowDamageNumbers"] = ShowDamageNumbers,
			["ReduceScreenShake"] = ReduceScreenShake,
			["ReduceDarkness"] = ReduceDarkness,
			["Fullscreen"] = Fullscreen,
			["WindowWidth"] = WindowWidth,
			["WindowHeight"] = WindowHeight,
			["MonitorIndex"] = MonitorIndex,
			["AscensionLearned"] = AscensionLearned,
			["LessonsLearned"] = lessonsLearned,
			["FullBookKept"] = FullBookKept,
			["FreeingsOwed"] = freeingsOwed,
			["BalancePresetId"] = BalancePresetId,
			["PinnedRecipeId"] = PinnedRecipeId ?? string.Empty,
			["PlaytestChecklistState"] = playtestChecklistState
		};
	}

	public static SaveData FromVariant(Variant variant)
	{
		var result = new SaveData();
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return result;
		}

		var root = variant.AsGodotDictionary();

		if (root.ContainsKey("SchemaVersion"))
		{
			result.SchemaVersion = root["SchemaVersion"].AsInt32();
		}

		// No schema bump: the epoch is not a format, it is which story the progress belongs to. A save
		// without the key predates epochs and so belongs to the first telling (FirstProgressEpoch), never
		// to "now" - otherwise the first bump would pass over every save written before it.
		result.ProgressEpoch = root.ContainsKey("ProgressEpoch")
			? root["ProgressEpoch"].AsInt32()
			: FirstProgressEpoch;

		if (root.ContainsKey("ProgressResetNoticePending"))
			result.ProgressResetNoticePending = root["ProgressResetNoticePending"].AsBool();

		if (root.ContainsKey("TotalCurrency"))
		{
			result.TotalCurrency = root["TotalCurrency"].AsInt32();
		}

		if (root.ContainsKey("UnlockedCharacterIds"))
		{
			var array = root["UnlockedCharacterIds"].AsGodotArray();
			foreach (Variant value in array)
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id))
				{
					result.UnlockedCharacterIds.Add(id);
				}
			}
		}

		if (root.ContainsKey("UnlockedStageIds"))
		{
			var array = root["UnlockedStageIds"].AsGodotArray();
			foreach (Variant value in array)
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id))
				{
					result.UnlockedStageIds.Add(id);
				}
			}
		}

		if (root.ContainsKey("GrandfatheredStageIds"))
		{
			foreach (Variant value in root["GrandfatheredStageIds"].AsGodotArray())
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id))
				{
					result.GrandfatheredStageIds.Add(id);
				}
			}
		}

		if (root.ContainsKey("BossRushGrandfathered"))
			result.BossRushGrandfathered = root["BossRushGrandfathered"].AsBool();

		if (root.ContainsKey("UnlockedSpellIds"))
		{
			var array = root["UnlockedSpellIds"].AsGodotArray();
			foreach (Variant value in array)
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id))
				{
					result.UnlockedSpellIds.Add(id);
				}
			}
		}

		if (root.ContainsKey("UnlockedBoonIds"))
		{
			var array = root["UnlockedBoonIds"].AsGodotArray();
			foreach (Variant value in array)
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id))
				{
					result.UnlockedBoonIds.Add(id);
				}
			}
		}

		if (root.ContainsKey("UnlockedAchievementIds"))
		{
			var array = root["UnlockedAchievementIds"].AsGodotArray();
			foreach (Variant value in array)
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id))
				{
					result.UnlockedAchievementIds.Add(id);
				}
			}
		}

		if (root.ContainsKey("RemovedSpellIds"))
		{
			var array = root["RemovedSpellIds"].AsGodotArray();
			foreach (Variant value in array)
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id))
				{
					result.RemovedSpellIds.Add(id);
				}
			}
		}

		if (root.ContainsKey("SeenAscensionIds"))
		{
			var array = root["SeenAscensionIds"].AsGodotArray();
			foreach (Variant value in array)
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id))
				{
					result.SeenAscensionIds.Add(id);
				}
			}
		}

		if (root.ContainsKey("MaxDifficultyCleared"))
		{
			var map = root["MaxDifficultyCleared"].AsGodotDictionary();
			foreach (Variant key in map.Keys)
			{
				string mapId = key.AsString();
				if (string.IsNullOrWhiteSpace(mapId))
				{
					continue;
				}

				result.MaxDifficultyCleared[mapId] = map[key].AsInt32();
			}
		}

		if (root.ContainsKey("ArcaneUpgradeLevels"))
		{
			var map = root["ArcaneUpgradeLevels"].AsGodotDictionary();
			foreach (Variant key in map.Keys)
			{
				string upgradeId = key.AsString();
				if (string.IsNullOrWhiteSpace(upgradeId))
				{
					continue;
				}

				result.ArcaneUpgradeLevels[upgradeId] = map[key].AsInt32();
			}
		}

		if (root.ContainsKey("FoundSecretIds"))
		{
			foreach (Variant value in root["FoundSecretIds"].AsGodotArray())
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id))
				{
					result.FoundSecretIds.Add(id);
				}
			}
		}

		if (root.ContainsKey("GlimpsedSecretIds"))
		{
			foreach (Variant value in root["GlimpsedSecretIds"].AsGodotArray())
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id))
				{
					result.GlimpsedSecretIds.Add(id);
				}
			}
		}

		if (root.ContainsKey("FoundUpgradeLevels"))
		{
			var map = root["FoundUpgradeLevels"].AsGodotDictionary();
			foreach (Variant key in map.Keys)
			{
				string upgradeId = key.AsString();
				if (!string.IsNullOrWhiteSpace(upgradeId))
				{
					result.FoundUpgradeLevels[upgradeId] = map[key].AsInt32();
				}
			}
		}

		if (root.ContainsKey("RunTelemetryHistory"))
		{
			var array = root["RunTelemetryHistory"].AsGodotArray();
			foreach (Variant entry in array)
			{
				if (entry.VariantType == Variant.Type.Dictionary)
					result.RunTelemetryHistory.Add(RunTelemetryRecord.FromDictionary(entry.AsGodotDictionary()));
			}

			if (result.RunTelemetryHistory.Count > MaxTelemetryHistory)
				result.RunTelemetryHistory = result.RunTelemetryHistory.GetRange(0, MaxTelemetryHistory);
		}

		if (root.ContainsKey("Lifetime") && root["Lifetime"].VariantType == Variant.Type.Dictionary)
			result.Lifetime = LifetimeStats.FromDictionary(root["Lifetime"].AsGodotDictionary());

		if (root.ContainsKey("HasSeenGameplayOnboarding"))
			result.HasSeenGameplayOnboarding = root["HasSeenGameplayOnboarding"].AsBool();

		if (root.ContainsKey("HasSeenPrologue"))
			result.HasSeenPrologue = root["HasSeenPrologue"].AsBool();

		if (root.ContainsKey("HasSeenLightGoesOn"))
			result.HasSeenLightGoesOn = root["HasSeenLightGoesOn"].AsBool();

		// Both additive (no schema bump): a save without the key keeps the defaults - the next wizard
		// tracked, no note seen. A present key replaces the default, so an empty list stays empty.
		if (root.ContainsKey("TrackedQuestIds") && root["TrackedQuestIds"].VariantType == Variant.Type.Array)
		{
			result.TrackedQuestIds.Clear();
			foreach (Variant value in root["TrackedQuestIds"].AsGodotArray())
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id) && !result.TrackedQuestIds.Contains(id))
					result.TrackedQuestIds.Add(id);
			}
		}

		if (root.ContainsKey("SeenChapterNoteIds") && root["SeenChapterNoteIds"].VariantType == Variant.Type.Array)
		{
			foreach (Variant value in root["SeenChapterNoteIds"].AsGodotArray())
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id) && !result.SeenChapterNoteIds.Contains(id))
					result.SeenChapterNoteIds.Add(id);
			}
		}

		if (root.ContainsKey("SeenItemIds") && root["SeenItemIds"].VariantType == Variant.Type.Array)
		{
			foreach (Variant value in root["SeenItemIds"].AsGodotArray())
				result.MarkItemSeen(value.AsString());
		}

		if (root.ContainsKey("EnableGameplayOnboardingTips"))
			result.EnableGameplayOnboardingTips = root["EnableGameplayOnboardingTips"].AsBool();

		if (root.ContainsKey("HasToggledOnboardingTipsAtLeastOnce"))
			result.HasToggledOnboardingTipsAtLeastOnce = root["HasToggledOnboardingTipsAtLeastOnce"].AsBool();

		if (root.ContainsKey("PlaytestModeEnabled"))
			result.PlaytestModeEnabled = root["PlaytestModeEnabled"].AsBool();

		if (root.ContainsKey("BossRushBestFelled"))
			result.BossRushBestFelled = root["BossRushBestFelled"].AsInt32();

		if (root.ContainsKey("BossRushBestClearSeconds"))
			result.BossRushBestClearSeconds = (float)root["BossRushBestClearSeconds"].AsDouble();

		// Endless (EndlessCatalog): additive, no schema step - a save without the keys has kept no vigil.
		if (root.ContainsKey("EndlessRecords") && root["EndlessRecords"].VariantType == Variant.Type.Array)
		{
			foreach (Variant entry in root["EndlessRecords"].AsGodotArray())
			{
				if (entry.VariantType != Variant.Type.Dictionary)
					continue;
				EndlessRecord record = EndlessRecord.FromDictionary(entry.AsGodotDictionary());
				if (!string.IsNullOrWhiteSpace(record.StageId))
					result.EndlessRecords.Add(record);
			}
		}
		if (root.ContainsKey("HasKeptVigil"))
			result.HasKeptVigil = root["HasKeptVigil"].AsBool();

		if (root.ContainsKey("MasterVolume"))
			result.MasterVolume = (float)root["MasterVolume"].AsDouble();

		if (root.ContainsKey("MusicVolume"))
			result.MusicVolume = (float)root["MusicVolume"].AsDouble();

		if (root.ContainsKey("SfxVolume"))
			result.SfxVolume = (float)root["SfxVolume"].AsDouble();

		if (root.ContainsKey("AudioMuted"))
			result.AudioMuted = root["AudioMuted"].AsBool();

		if (root.ContainsKey("ShowDamageNumbers"))
			result.ShowDamageNumbers = root["ShowDamageNumbers"].AsBool();
		if (root.ContainsKey("ReduceScreenShake"))
			result.ReduceScreenShake = root["ReduceScreenShake"].AsBool();
		if (root.ContainsKey("ReduceDarkness"))
			result.ReduceDarkness = root["ReduceDarkness"].AsBool();
		if (root.ContainsKey("Fullscreen")) result.Fullscreen = root["Fullscreen"].AsBool();
		if (root.ContainsKey("WindowWidth")) result.WindowWidth = System.Math.Max(0, root["WindowWidth"].AsInt32());
		if (root.ContainsKey("WindowHeight")) result.WindowHeight = System.Math.Max(0, root["WindowHeight"].AsInt32());
		if (root.ContainsKey("MonitorIndex")) result.MonitorIndex = System.Math.Max(-1, root["MonitorIndex"].AsInt32());
		if (root.ContainsKey("AscensionLearned"))
			result.AscensionLearned = root["AscensionLearned"].AsBool();
		if (root.ContainsKey("FullBookKept"))
			result.FullBookKept = root["FullBookKept"].AsBool();
		if (root.ContainsKey("LessonsLearned"))
		{
			foreach (Variant value in root["LessonsLearned"].AsGodotArray())
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id) && !result.LessonsLearned.Contains(id))
					result.LessonsLearned.Add(id);
			}
		}
		if (root.ContainsKey("FreeingsOwed"))
		{
			foreach (Variant value in root["FreeingsOwed"].AsGodotArray())
			{
				string id = value.AsString();
				if (!string.IsNullOrWhiteSpace(id) && !result.FreeingsOwed.Contains(id))
					result.FreeingsOwed.Add(id);
			}
		}

		if (root.ContainsKey("BalancePresetId"))
			result.BalancePresetId = GlobalStatsManager.NormalizeBalancePresetId(root["BalancePresetId"].AsString());
		// Schema 27 (rung I of the Conjunction ladder): the one recipe pinned, or empty.
		if (root.ContainsKey("PinnedRecipeId") && root["PinnedRecipeId"].VariantType == Variant.Type.String)
			result.PinnedRecipeId = root["PinnedRecipeId"].AsString().Trim();

		if (root.ContainsKey("PlaytestChecklistState"))
		{
			var map = root["PlaytestChecklistState"].AsGodotDictionary();
			foreach (Variant key in map.Keys)
			{
				string itemId = key.AsString();
				if (string.IsNullOrWhiteSpace(itemId))
					continue;

				result.PlaytestChecklistState[itemId] = map[key].AsBool();
			}
		}

		return result;
	}
}

public sealed class RunTelemetryRecord
{
	public string BalancePresetId { get; set; } = GlobalStatsManager.BalancePresetDefault;
	public string StageId { get; set; } = string.Empty;
	public string Outcome { get; set; } = "Defeat";
	public int FinalPlayerLevel { get; set; } = 1;
	public float TimeSurvived { get; set; } = 0f;
	public int EnemiesKilled { get; set; } = 0;
	public int TotalDamageDealt { get; set; } = 0;
	public int TotalDamageTaken { get; set; } = 0;
	public int HitsTaken { get; set; } = 0;
	public int LevelUpsGained { get; set; } = 0;
	public int RerollsUsed { get; set; } = 0;
	public int SwapsUsed { get; set; } = 0;
	public int RemovalsUsed { get; set; } = 0;
	public int SkipsUsed { get; set; } = 0;
	public float ArcaneRewardMultiplier { get; set; } = 1.0f;
	public string ArcaneRewardBreakdown { get; set; } = string.Empty;
	public Dictionary<string, int> SpellPickCounts { get; set; } = new();
	public Dictionary<string, int> SpellUpgradeCounts { get; set; } = new();

	public static RunTelemetryRecord FromRunResult(RunResult run)
	{
		return new RunTelemetryRecord
		{
			BalancePresetId = GlobalStatsManager.NormalizeBalancePresetId(run.BalancePresetId),
			StageId = run.StageId,
			Outcome = run.Outcome.ToString(),
			FinalPlayerLevel = run.FinalPlayerLevel,
			TimeSurvived = run.TimeSurvived,
			EnemiesKilled = run.EnemiesKilled,
			TotalDamageDealt = run.TotalDamageDealt,
			TotalDamageTaken = run.TotalDamageTaken,
			HitsTaken = run.HitsTaken,
			LevelUpsGained = run.LevelUpsGained,
			RerollsUsed = run.RerollsUsed,
			SwapsUsed = run.SwapsUsed,
			RemovalsUsed = run.RemovalsUsed,
			SkipsUsed = run.SkipsUsed,
			ArcaneRewardMultiplier = run.ArcaneRewardMultiplier,
			ArcaneRewardBreakdown = run.ArcaneRewardBreakdown ?? string.Empty,
			SpellPickCounts = CopyIntMap(run.SpellPickCounts),
			SpellUpgradeCounts = CopyIntMap(run.SpellUpgradeCounts)
		};
	}

	public Godot.Collections.Dictionary ToGodotDictionary()
	{
		return new Godot.Collections.Dictionary
		{
			["BalancePresetId"] = BalancePresetId,
			["StageId"] = StageId,
			["Outcome"] = Outcome,
			["FinalPlayerLevel"] = FinalPlayerLevel,
			["TimeSurvived"] = TimeSurvived,
			["EnemiesKilled"] = EnemiesKilled,
			["TotalDamageDealt"] = TotalDamageDealt,
			["TotalDamageTaken"] = TotalDamageTaken,
			["HitsTaken"] = HitsTaken,
			["LevelUpsGained"] = LevelUpsGained,
			["RerollsUsed"] = RerollsUsed,
			["SwapsUsed"] = SwapsUsed,
			["RemovalsUsed"] = RemovalsUsed,
			["SkipsUsed"] = SkipsUsed,
			["ArcaneRewardMultiplier"] = ArcaneRewardMultiplier,
			["ArcaneRewardBreakdown"] = ArcaneRewardBreakdown,
			["SpellPickCounts"] = ToGodotMap(SpellPickCounts),
			["SpellUpgradeCounts"] = ToGodotMap(SpellUpgradeCounts)
		};
	}

	public static RunTelemetryRecord FromDictionary(Godot.Collections.Dictionary dict)
	{
		var result = new RunTelemetryRecord();

		if (dict.ContainsKey("BalancePresetId")) result.BalancePresetId = GlobalStatsManager.NormalizeBalancePresetId(dict["BalancePresetId"].AsString());
		if (dict.ContainsKey("StageId")) result.StageId = dict["StageId"].AsString();
		if (dict.ContainsKey("Outcome")) result.Outcome = dict["Outcome"].AsString();
		if (dict.ContainsKey("FinalPlayerLevel")) result.FinalPlayerLevel = dict["FinalPlayerLevel"].AsInt32();
		if (dict.ContainsKey("TimeSurvived")) result.TimeSurvived = dict["TimeSurvived"].AsSingle();
		if (dict.ContainsKey("EnemiesKilled")) result.EnemiesKilled = dict["EnemiesKilled"].AsInt32();
		if (dict.ContainsKey("TotalDamageDealt")) result.TotalDamageDealt = dict["TotalDamageDealt"].AsInt32();
		if (dict.ContainsKey("TotalDamageTaken")) result.TotalDamageTaken = dict["TotalDamageTaken"].AsInt32();
		if (dict.ContainsKey("HitsTaken")) result.HitsTaken = dict["HitsTaken"].AsInt32();
		if (dict.ContainsKey("LevelUpsGained")) result.LevelUpsGained = dict["LevelUpsGained"].AsInt32();
		if (dict.ContainsKey("RerollsUsed")) result.RerollsUsed = dict["RerollsUsed"].AsInt32();
		if (dict.ContainsKey("SwapsUsed")) result.SwapsUsed = dict["SwapsUsed"].AsInt32();
		if (dict.ContainsKey("RemovalsUsed")) result.RemovalsUsed = dict["RemovalsUsed"].AsInt32();
		if (dict.ContainsKey("SkipsUsed")) result.SkipsUsed = dict["SkipsUsed"].AsInt32();
		if (dict.ContainsKey("ArcaneRewardMultiplier")) result.ArcaneRewardMultiplier = dict["ArcaneRewardMultiplier"].AsSingle();
		if (dict.ContainsKey("ArcaneRewardBreakdown")) result.ArcaneRewardBreakdown = dict["ArcaneRewardBreakdown"].AsString();

		if (dict.ContainsKey("SpellPickCounts"))
			result.SpellPickCounts = FromGodotMap(dict["SpellPickCounts"].AsGodotDictionary());
		if (dict.ContainsKey("SpellUpgradeCounts"))
			result.SpellUpgradeCounts = FromGodotMap(dict["SpellUpgradeCounts"].AsGodotDictionary());

		return result;
	}

	private static Godot.Collections.Dictionary<string, int> ToGodotMap(Dictionary<string, int> source)
	{
		var map = new Godot.Collections.Dictionary<string, int>();
		foreach (KeyValuePair<string, int> entry in source)
			map[entry.Key] = entry.Value;

		return map;
	}

	private static Dictionary<string, int> FromGodotMap(Godot.Collections.Dictionary map)
	{
		var result = new Dictionary<string, int>();
		foreach (Variant key in map.Keys)
		{
			string id = key.AsString();
			if (string.IsNullOrWhiteSpace(id))
				continue;

			result[id] = map[key].AsInt32();
		}

		return result;
	}

	private static Dictionary<string, int> CopyIntMap(Dictionary<string, int> source)
	{
		if (source == null)
			return new Dictionary<string, int>();

		var copy = new Dictionary<string, int>();
		foreach (KeyValuePair<string, int> entry in source)
			copy[entry.Key] = entry.Value;

		return copy;
	}
}
