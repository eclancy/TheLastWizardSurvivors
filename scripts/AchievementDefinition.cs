using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>Everything an achievement is allowed to ask about.</summary>
/// <remarks>
/// <see cref="Run"/> is null everywhere except the moment a run ends - the main menu has no current
/// run. A condition that can only be measured against <see cref="Run"/> therefore cannot show
/// progress anywhere the player would look at it, which is why most conditions are written against
/// <see cref="Lifetime"/> instead and the run only folds into that at the end.
/// </remarks>
public sealed class AchievementContext
{
	public global::RunResult Run { get; init; }
	public SaveData Save { get; init; }

	public LifetimeStats Lifetime => lifetimeOverride ?? Save?.Lifetime ?? EmptyLifetime;
	private static readonly LifetimeStats EmptyLifetime = new();
	// A projection's lifetime: the save's with the run folded in (Projected). Null otherwise.
	private LifetimeStats lifetimeOverride;

	public static AchievementContext ForSave(SaveData save) => new() { Save = save };
	public static AchievementContext ForRun(SaveData save, global::RunResult run) => new() { Save = save, Run = run };

	/// <summary>
	/// Where every deed would stand if the run ended now: the save's lifetime stats copied and the run
	/// so far folded into the copy, the way the run's end folds it in (<see cref="LifetimeStats.Absorb"/>,
	/// before <see cref="AchievementManager.ApplyRunAchievements"/> evaluates). Read-only to the save: the
	/// copy goes through the save's own serialisation, so it holds every field the save would. The
	/// pause tab's bars (AchievementsPage) and the mid-run achievement moment (Node2DGame) both read it,
	/// so the moment fires exactly when the tab starts to say "This run".
	/// </summary>
	public static AchievementContext Projected(SaveData save, global::RunResult run)
	{
		LifetimeStats lifetime = LifetimeStats.FromDictionary(save?.Lifetime?.ToGodotDictionary());
		lifetime.Absorb(run);
		return new AchievementContext { Save = save, Run = run, lifetimeOverride = lifetime };
	}
}

public sealed class AchievementProgress
{
	public float Current { get; init; }
	public float Target { get; init; }
	public string Display { get; init; } = string.Empty;
	public bool IsComplete => Current >= Target;
	public float Fraction => Target <= 0f ? 1f : Math.Clamp(Current / Target, 0f, 1f);
}

public enum AchievementCategory
{
	Progress,
	Mastery,
	Exploration,
	Challenge,
}

/// <summary>What kind of thing an achievement asks (.ai/achievements-and-secrets.md, section 2).</summary>
public enum AchievementKind
{
	/// <summary>A simple challenge measured over runs - kills, time survived, level, elements held. Names no chapter.</summary>
	Feat,
	/// <summary>A chapter's deed: beat its boss, win its side event, run its quarry down.</summary>
	Road,
	/// <summary>Something found in the world; derived from a <see cref="SecretDefinition"/>.</summary>
	Secret,
	/// <summary>Owning a whole set: every spell, a dozen relics.</summary>
	Collection,
}

/// <summary>When an achievement is shown on the achievements page (.ai/achievements-and-secrets.md 4.2).</summary>
public enum AchievementVisibility
{
	/// <summary>From the first run.</summary>
	Always,
	/// <summary>
	/// Once its chapter is playable. A deed that frees a SECRET wizard is a sealed row
	/// (<see cref="AchievementDefinition.SealedLine"/>) until his chapter opens - the character select's rule.
	/// </summary>
	ChapterOpen,
	/// <summary>Counted, never named, until it has been seen (<see cref="SaveData.GlimpsedSecretIds"/>) or found.</summary>
	Hidden,
	/// <summary>
	/// A combination's Feat (ComboCatalog): not on the page at all until the save owns both parents, then the
	/// two parents with their named Ascensions and a sealed result, and the result's name once made
	/// (.ai/combo-spells.md section 2, "Unlocks and the save"). Derived from the save, never stored.
	/// </summary>
	Recipe,
	/// <summary>
	/// The far sky's Feats (FarSky, piece F4): not on the page at all until the save has learned the far sky
	/// (Teachings), since nothing of the far shore is on show before (.ai/second-half.md 6.2).
	/// </summary>
	FarSky,
}

public sealed class AchievementDefinition
{
	public string Id { get; init; } = string.Empty;
	public string DisplayName { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string RewardText { get; init; } = string.Empty;
	public string SpellUnlockId { get; init; } = string.Empty;

	/// <summary>A boon this achievement grants, if any. Same role as SpellUnlockId.</summary>
	public string BoonUnlockId { get; init; } = string.Empty;
	public string CharacterUnlockId { get; init; } = string.Empty;
	/// <summary>A named-character challenge is listed only after that character is owned.</summary>
	public string RequiredCharacterId { get; init; } = string.Empty;
	public int CurrencyReward { get; init; }

	/// <summary>
	/// Spell pages this deed adds to the save's book (SpellBook, .ai/slow-start.md section 2): 1 on Survivor,
	/// 0 everywhere else. Derived from the done deeds, never written, so the page can move to another deed
	/// later without a schema step.
	/// </summary>
	public int PageReward { get; init; }
	public AchievementCategory Category { get; init; } = AchievementCategory.Progress;

	/// <summary>Feat, Road, Secret or Collection (.ai/achievements-and-secrets.md section 2).</summary>
	public AchievementKind Kind { get; init; } = AchievementKind.Feat;

	/// <summary>
	/// The chapter it belongs to, a StageCatalog id: a Road deed's chapter, a secret's. Empty for a Feat
	/// or a Collection, which may be done anywhere. Groups the achievements page by chapter, and is what
	/// RegressionChecks.ValidateGateReachability reads to prove a gate counts nothing behind itself.
	/// </summary>
	public string StageId { get; init; } = string.Empty;

	/// <summary>When it is shown (<see cref="AchievementDefinitions.IsListed"/>).</summary>
	public AchievementVisibility Visibility { get; init; } = AchievementVisibility.Always;

	/// <summary>
	/// True for side events and secrets: completed the moment it happens, mid-run, and saved on the spot
	/// (<see cref="AchievementManager.CompleteNow"/>), not measured at the end of the run.
	/// </summary>
	public bool CompletesInRun { get; init; }

	/// <summary>For a side event's deed: the site it mirrors (one of SideEventDirector.EventsFor its chapter). Empty otherwise.</summary>
	public string SiteId { get; init; } = string.Empty;

	/// <summary>For a secret's deed: the <see cref="SecretDefinition"/> it mirrors. Empty otherwise.</summary>
	public string SecretId { get; init; } = string.Empty;

	/// <summary>For a combination's Feat: the recipe it mirrors (ComboCatalog). Empty otherwise.</summary>
	public string ComboId { get; init; } = string.Empty;

	/// <summary>
	/// What a Recipe deed is called and pays before it is made: the name is the result's, and stays sealed
	/// until the run that makes it ("Conjunction: ???", "Unlocks ???"). Empty on every other deed.
	/// </summary>
	public string SealedName { get; init; } = string.Empty;
	public string SealedReward { get; init; } = string.Empty;

	/// <summary>
	/// A Hidden deed's words once seen (.ai/achievements-and-secrets.md 4.3): what the player saw, as an
	/// observation. Taken from the secret, so the page invents nothing.
	/// </summary>
	public string SeenLine { get; init; } = string.Empty;

	/// <summary>
	/// What may be said before it is seen or open: a Hidden deed's chapter ("Hidden somewhere in the
	/// Sunken Cave" - also the Spell Index's line for a vault's spell), or, for a deed that frees a secret
	/// wizard, "The Cursed Dungeon holds someone". Empty for every deed that is never sealed.
	/// </summary>
	public string SealedLine { get; init; } = string.Empty;

	/// <summary>
	/// For a chapter clear, the boss it records; empty for every other deed. It is what lets the
	/// deeds list follow the campaign order and keep a chapter that is not built yet off the list
	/// (<see cref="AchievementDefinitions.Listed"/>).
	/// </summary>
	public string BossId { get; init; } = string.Empty;

	/// <summary>
	/// How far along the player is, in the same unit as <see cref="Target"/>.
	/// </summary>
	/// <remarks>
	/// Completion and progress are derived from this one function rather than being two independent
	/// lambdas. That is the whole point of the shape: an achievement whose "am I done" and "how far
	/// am I" disagree is a bug nobody notices, because each half looks correct on its own.
	/// </remarks>
	public Func<AchievementContext, float> Measure { get; init; } = _ => 0f;

	public float Target { get; init; } = 1f;

	/// <summary>Formats a measured value for the card. Defaults to a plain integer.</summary>
	public Func<float, string> FormatValue { get; init; }

	public bool IsComplete(AchievementContext context) => Measure(context) >= Target;

	public AchievementProgress GetProgress(AchievementContext context)
	{
		float current = Measure(context);
		Func<float, string> format = FormatValue ?? (value => ((int)value).ToString());
		return new AchievementProgress
		{
			Current = current,
			Target = Target,
			// A one-of-one achievement is a yes/no question; showing "0 / 1" for it reads as a
			// counter that is broken rather than as something not yet done.
			Display = Target <= 1f ? string.Empty : $"{format(Math.Min(current, Target))} / {format(Target)}",
		};
	}

	/// <summary>Seconds formatted as m:ss, for time-based targets.</summary>
	public static string FormatTime(float seconds)
	{
		int total = Math.Max(0, (int)seconds);
		return $"{total / 60}:{total % 60:00}";
	}
}

public static class AchievementDefinitions
{
	/// <summary>
	/// How many Black Hares Laelaps' hound must run down in one run of the Broken Circle (the
	/// "coursing" deed). Measured in real time on the Circle, opening on the hound (seed 11, 15 minutes):
	/// the --survivor bot, which spread its picks over six spells and never levelled the hound, coursed 116
	/// of 344 hares - a third of them, the rest falling to its other spells; the same bot taking only the
	/// Familiar coursed 436 of 444. A hundred is most of a run with the hound beside other spells, or a few
	/// minutes of one built round it - a deed, not a side effect of taking the dog. (Sixty, first set from
	/// the fixed-fps bench, which understates the hound, came in a short opening.)
	/// </summary>
	public const int CoursingTarget = 100;

	/// <summary>
	/// Lifetime chests that free John Dee (The Library at Mortlake). Every gate counts him, so it must stay under what
	/// a player who beats each chapter at the first try has opened by the Emberdeep - about 134 (eight runs of about
	/// seventeen); RegressionChecks.ValidateCollectorEconomy holds it there.
	/// </summary>
	public const int MortlakeChestTarget = 100;

	/// <summary>
	/// A side event's deed is "event_" and its site id; a secret's is "secret_" and the secret's id. Made
	/// from the id rather than written by hand, so a deed can never drift from what it mirrors, and
	/// prefixed so neither can ever collide with a hand-written deed. Constants, so UnlockCatalog can
	/// name a deed while this class is still building the list that reads it.
	/// </summary>
	public const string EventDeedPrefix = "event_";
	public const string SecretDeedPrefix = "secret_";

	public static string EventDeedId(string siteId) => EventDeedPrefix + (siteId ?? string.Empty).Trim();

	public static string SecretDeedId(string secretId) => SecretDeedPrefix + (secretId ?? string.Empty).Trim();

	/// <summary>
	/// A far chapter's warden deed - the one that rekindles its god (.ai/second-half.md 4.3) - is "seat_" and the
	/// chapter's stage id ("seat_stage_14"). Made from the seat, so the god's unlock (UnlockCatalog.FromGod) and the
	/// chapter's deed (<see cref="SeatClear"/>) name the same deed without either piece knowing the other's words: a
	/// god's piece and its chapter's piece may merge in either order.
	/// </summary>
	public const string SeatDeedPrefix = "seat_";

	public static string SeatDeedId(string stageId) => SeatDeedPrefix + (stageId ?? string.Empty).Trim();

	/// <summary>
	/// The deed that takes the dark wizard: every chapter's boss beaten on Hardcore (.ai/dark-wizard-playable.md;
	/// Eric, 2026-11-02, "dark wizard should unlock after defeating all stages on hard mode").
	/// </summary>
	public const string TakerTakenId = "the_taker_taken";

	/// <summary>
	/// The Road Restored: every chapter's boss beaten on Default or harder. Merlin now belongs to the Moot.
	/// The id keeps the deed's first name, "The One He Missed", so a save that has done it keeps it; that name is the
	/// player's now, and the game's (.ai/world-and-tone.md), never a deed's.
	/// </summary>
	public const string OneHeMissedId = "the_one_he_missed";

	/// <summary>The Throne's boss, the dark wizard (BossCatalog).</summary>
	public const string ThroneBossId = "throne_dark_wizard";

	/// <summary>Taken Back, the Throne's clear: a third of the far shore's gate (GlobalStatsManager.HasTakenBackTheThrone).</summary>
	public const string TakenBackId = "throne_taken_back";

	/// <summary>
	/// The whole road at a difficulty: how many of the campaign's chapters have had their boss beaten on the preset
	/// of <paramref name="rank"/> or harder (SaveData.MaxDifficultyCleared, with the run that is ending folded in -
	/// the pause tab's projection reads the save before the run's end records it). The two deeds' measure, out of
	/// <see cref="CampaignChapterCount"/>, which counts the chapters not yet playable. Read from StageCatalog.Campaign,
	/// never a list of its own, so the Sketchbook - outside the campaign, and with no unlocks (Eric, 2026-11-02) - is
	/// never asked for.
	/// </summary>
	public static float ChaptersClearedAt(AchievementContext context, int rank) =>
		GlobalStatsManager.ChaptersClearedAt(context?.Save, rank, context?.Run);

	/// <summary>Every chapter of the campaign, built or not: the target of the two whole-road deeds.</summary>
	public static int CampaignChapterCount => StageCatalog.Campaign.Count;

	/// <summary>The Grimoire Restored: every spell recovered, now the collection rather than the final gate's key.</summary>
	public const string GrimoireRestoredId = "grimoire_restored";

	/// <summary>
	/// What each side event's deed is called and says (.ai/achievements-and-secrets.md 2.1 and 2.3). The
	/// reward line names what UnlockCatalog lists at the site; RegressionChecks holds the two together.
	/// The Cursed Dungeon's deed is "The Open Cell", not the doc's "Tituba Freed": a rescued wizard's name
	/// is his reward and stays hidden while he is locked (.ai/side-events.md, "Secret wizards"), and the
	/// deed is listed from the day the Dungeon opens.
	/// </summary>
	private static readonly (string Site, string Name, string Description, string Reward)[] SideEventWords =
	{
		("the_grove", "The Grove Held", "Hold the corrupted grove in the Enchanted Forest until it grows back.", "Unlocks Bramble Seed"),
		("walking_hut", "The Hut Herded Home", "Drive the walking hut of the Enchanted Forest into the bone-fenced yard.", "Frees Baba Yaga"),
		("the_well", "The Last Coin", "Keep the last clean well in the Fallow Fields from being fouled.", "Unlocks Wishing Coin"),
		("tithe_scale", "The Scale Held Level", "Fill both pans of the Tithe-Reeve's scale in the Fallow Fields and hold its beam level.", "Frees Medea"),
		("the_moot", "The Circle Relit", "Restore the shattered council wards of the Broken Circle to free their keeper.", "Frees Merlin, unlocks Orb of Ages and Gilded Mote"),
		("gaolers_key", "The Open Cell", "Slip the Gaoler's key past his lantern-bearers to the locked cell in the Cursed Dungeon.", "Frees Tituba"),
		("bog_lanterns", "The Lanterns Out", "Snuff the lanterns of the Blighted Swamp and face what tends them.", "Unlocks Contagion"),
		("gibbet_cage", "The Ferry Reached", "Lower the gibbet's cage in the Blighted Swamp and bring the man in it to the ferry landing alive.", "Frees Paracelsus"),
		("five_pointed_seal", "The Seal Broken", "Turn the four guardian stones to face the seal in the Mystic Ruins.", "Frees Abe no Seimei"),
		("nine_mouths", "The True Mouth", "Find the one mouth of nine her voice comes out of first, in the Sunken Cave.", "Frees The Cumaean Sibyl"),
		("the_thaw", "Out of the Ice", "Thaw the wizard out of the ice in the Frozen Waste.", "Frees Väinämöinen"),
		("caravan_master", "The Caravan Stopped", "Catch the caravan master on the Scorched Sands road.", "Unlocks Iron Palisade"),
		("tomb_beacons", "The Sun Let In", "Turn the bronze mirrors on the Scorched Sands until the sunlight reaches the disc over the dry tomb's door.", "Frees Imhotep"),
		("great_anvil", "The Chains Struck", "Bring the forge's own hammers down on the four chains in the Emberdeep.", "Frees Wayland the Smith"),
		("the_barge", "The Way Lit", "Light the dead lamps along the Last Bridge ahead of the barge below until it comes in to its landing.", "Frees Morgan le Fay"),
		("seers_voice", "The Last Reading", "Follow the voice reading room to room through the Court until you find the reader.", "Frees Nostradamus"),
		("dotted_door", "The Door Inked In", "Ink every line of the door sketched in the Court's Gallery, in one stroke.", "Frees Ma Liang"),
		// THE FAR SHORE (.ai/second-half.md 4.3, marked by F0): one side event a chapter, paying Arcane Energy and a
		// keepsake - never a spell, boon or wizard. Each piece replaces its own comment line with its words.
		// F3 (White Downs)
		// (F6's line is below; leave this one as it is)
		// F6 (Drowned Shore)
		// (F7's line is below; leave this one as it is)
		// F7 (Three Ways)
		// (F8's line is below; leave this one as it is)
		// F8 (Twelfth Hour)
		// (F9's line is below; leave this one as it is)
		// F9 (Thunder Fell)
		// (F10's line is below; leave this one as it is)
		// F10 (Bronze Isle)
	};

	private const float TenMinutes = 600f;
	private const float ThreeMinutes = 180f;

	/// <summary>
	/// Every deed: the hand-written ones - Feats, the chapters' boss clears, the Collection deeds - with
	/// the clears standing in campaign order (<see cref="InCampaignOrder"/>), then one Road deed derived
	/// from every side event's site and one Secret deed derived from every secret, each in campaign order
	/// (.ai/achievements-and-secrets.md 2.6).
	/// </summary>
	public static readonly IReadOnlyList<AchievementDefinition> All = InCampaignOrder(new List<AchievementDefinition>
	{
		// --- Progress: the spine of the campaign ---------------------------------------------
		new()
		{
			Id = "first_blood", DisplayName = "First Blood",
			Description = "Defeat 1 enemy.",
			RewardText = "Unlocks Arcane Explosion", SpellUnlockId = "arcane_explosion",
			Category = AchievementCategory.Progress,
			Measure = c => c.Lifetime.TotalKills, Target = 1,
		},
		new()
		{
			// Hollow Star since schema 19 (it was sold): Darkness's only pure carrier stays early. Frost
			// Shard, which this paid, is the Geode Heart's - Eric's giant crystal (SecretCatalog).
			// And the 4th spell page (.ai/slow-start.md 2.2): at 10:00 of the run that earns it, about four
			// levels before a three-page book would run out of upgrades. It opens at the next level-up.
			Id = "survivor", DisplayName = "Survivor",
			Description = "Survive for 10 minutes in a single run.",
			RewardText = "Unlocks Hollow Star and a spell page", SpellUnlockId = "hollow_star", PageReward = 1,
			Category = AchievementCategory.Progress,
			Measure = c => c.Lifetime.BestTimeByStage.Values.DefaultIfEmpty(0f).Max(),
			Target = TenMinutes, FormatValue = AchievementDefinition.FormatTime,
		},
		new()
		{
			Id = "veteran", DisplayName = "Veteran",
			Description = "Clear a chapter without falling.",
			RewardText = "Unlocks Meteor Swarm", SpellUnlockId = "meteor_swarm",
			Category = AchievementCategory.Progress,
			Measure = c => c.Lifetime.TotalVictories, Target = 1,
		},
		new()
		{
			Id = "max_level", DisplayName = "Archmage Training",
			Description = "Reach level 20 in a run.",
			RewardText = "Unlocks Solar Flare", SpellUnlockId = "solar_flare",
			Category = AchievementCategory.Progress,
			Measure = c => c.Lifetime.BestPlayerLevel, Target = 20,
		},
		new()
		{
			Id = "hoarder", DisplayName = "Full Grimoire",
			Description = "Carry six spells at once.",
			RewardText = "Unlocks Void Lance", SpellUnlockId = "void_lance",
			Category = AchievementCategory.Progress,
			Measure = c => c.Lifetime.BestSpellSlotsFilled, Target = Player.MaxSpellSlots,
		},

		// --- Mastery: element thresholds, measured at their peak, not at run end ---------------
		new()
		{
			Id = "elementalist", DisplayName = "Elementalist",
			Description = "Hold 4 instances of any one element.",
			RewardText = "Unlocks Cyclone Slash", SpellUnlockId = "cyclone_slash",
			Category = AchievementCategory.Mastery,
			Measure = c => c.Lifetime.BestElementCount.Values.DefaultIfEmpty(0).Max(), Target = 4,
		},
		ElementAdept("fire_adept", "Fire Adept", "Fire", "scorching_ray", "Scorching Ray"),
		ElementAdept("ice_adept", "Ice Adept", "Ice", "glacial_spike", "Glacial Spike"),
		ElementAdept("poison_adept", "Poison Adept", "Poison", "toxic_spore_burst", "Toxic Spore Burst"),
		ElementAdept("earth_adept", "Earth Adept", "Earth", "black_tentacles", "Black Tentacles"),
		ElementAdept("wind_adept", "Wind Adept", "Wind", "gale_blade", "Gale Blade"),
		// The defensive returns (in-flight item 9): two more adepts in the five's exact shape,
		// paying the storm that answers Lightning and the veil that answers Darkness.
		ElementAdept("lightning_adept", "Lightning Adept", "Lightning", "stormguard", "Stormguard"),
		ElementAdept("darkness_adept", "Darkness Adept", "Darkness", "gloam_veil", "Gloam Veil"),

		// --- Challenge -------------------------------------------------------------------------
		new()
		{
			// Easier since 2026-10-28 ("umbral veil should be easier to unlock - instead, not taking
			// damage for less time (getting shield hit wouldn't count for example)"): three minutes,
			// not five, and only a hit that costs HP ends it (Player.TakeDamage records it).
			Id = "untouchable", DisplayName = "Untouchable",
			Description = "Survive 3 minutes without losing health. A hit your shield takes does not count.",
			RewardText = "Unlocks Umbral Veil", BoonUnlockId = "umbral_veil",
			Category = AchievementCategory.Challenge,
			Measure = c => c.Lifetime.BestUndamagedSeconds,
			Target = ThreeMinutes, FormatValue = AchievementDefinition.FormatTime,
		},
		new()
		{
			Id = "exterminator", DisplayName = "Exterminator",
			Description = "Defeat 2,000 enemies in total.",
			RewardText = "300 Arcane Energy", CurrencyReward = 300,
			Category = AchievementCategory.Challenge,
			Measure = c => c.Lifetime.TotalKills, Target = 2000,
		},
		new()
		{
			Id = "elite_hunter", DisplayName = "Elite Hunter",
			Description = "Open 50 chests.",
			RewardText = "200 Arcane Energy", CurrencyReward = 200,
			Category = AchievementCategory.Challenge,
			Measure = c => c.Lifetime.TotalChestsOpened, Target = 50,
		},

		// --- Exploration: chapters, bosses and relics ------------------------------------------
		BossClear("forest_cleared", "Forest Cleared", "forest_treant", "the Enchanted Forest", "thorn_vine", "Thorned Vine"),
		// Bone Rattle, since a jailer carries a rattle (.ai/new-items.md section 7). It paid Shadow Bolt too
		// until 2026-11-07, when that became a Conjunction (ComboCatalog; a save that owned it keeps it,
		// SaveData.MigrateShadowBoltToConjunction).
		new()
		{
			Id = "castle_conqueror", DisplayName = "Dungeon Conqueror",
			Description = "Defeat the boss of the Cursed Dungeon.",
			RewardText = "Unlocks Bone Rattle",
			BoonUnlockId = "bone_rattle",
			Category = AchievementCategory.Exploration,
			Kind = AchievementKind.Road,
			Visibility = AchievementVisibility.ChapterOpen,
			StageId = StageOfBoss("castle_warden"),
			BossId = "castle_warden",
			Measure = c => c.Lifetime.HasDefeatedBoss("castle_warden") ? 1f : 0f,
			Target = 1,
		},
		BossClear("cave_choir_silenced", "Silence in the Deep", "cave_choir", "the Sunken Cave", "riptide", "Riptide"),
		BossClear("swamp_cleansed", "Cut the Rot Out", "swamp_mother", "the Blighted Swamp", "mirefoot", "Mirefoot"),
		BossClear("ruins_delver", "Ruins Delver", "ruins_sentinel", "the Mystic Ruins", "spiritual_weapon", "Spiritual Weapon"),
		BossClearBoon("waste_thawed", "Broke the Guard", "frost_warden", "the Frozen Waste", "rimebriar", "Rimebriar"),
		// Two things since schema 19: the boon it always paid, and Fireball, moved here from the Warden of
		// the Deep's deed - a spell only the far side of the old final gate paid, while that gate counted
		// every spell, so no save written since schema 8 could ever open the Emberdeep
		// (.ai/achievements-and-secrets.md 1.4). The Sands are the latest chapter before the gate.
		new()
		{
			Id = "sands_unwound", DisplayName = "Unwound",
			Description = "Defeat the boss of the Scorched Sands.",
			RewardText = "Unlocks Fireball and Sunsteel Filament",
			SpellUnlockId = "fireball", BoonUnlockId = "sunsteel_filament",
			Category = AchievementCategory.Exploration,
			Kind = AchievementKind.Road,
			Visibility = AchievementVisibility.ChapterOpen,
			StageId = StageOfBoss("sands_coil"),
			BossId = "sands_coil",
			Measure = c => c.Lifetime.HasDefeatedBoss("sands_coil") ? 1f : 0f,
			Target = 1,
		},
		// The gate chapter's own deed pays Arcane Energy since schema 19 - its Fireball moved to the Long
		// Coil - and opens the castle (.ai/endgame-castle.md) once it is built. A save that had done it
		// keeps its Fireball and is not paid the 300: reconciling pays unlocks, never currency.
		// Since 2026-11-02 it also pays the Sovereign Rite (.ai/sovereign-spell.md section 3): the end of the
		// road, so nothing on a new save reaches it early. A save that has beaten the Warden is handed it by
		// the reconcile at the end of its next run (AchievementManager.Reconcile) - no schema step.
		new()
		{
			Id = "emberdeep_ended", DisplayName = "The Long Road Back",
			Description = "Defeat the boss of the Emberdeep.",
			RewardText = "Unlocks the Sovereign Rite and 300 Arcane Energy", CurrencyReward = 300,
			SpellUnlockId = SovereignRite.SpellId,
			Category = AchievementCategory.Exploration,
			Kind = AchievementKind.Road,
			Visibility = AchievementVisibility.ChapterOpen,
			StageId = StageOfBoss("emberdeep_warden"),
			BossId = "emberdeep_warden",
			Measure = c => c.Lifetime.HasDefeatedBoss("emberdeep_warden") ? 1f : 0f,
			Target = 1,
		},
		// THE CASTLE'S CHAPTER CLEARS (.ai/endgame-castle.md 1.4). Arcane Energy, never a spell, wizard or
		// boon: the castle is past the Emberdeep's gate, and a gate may never count what is earned behind
		// it (.ai/achievements-and-secrets.md 2.5). Not listed until their chapters are playable
		// (IsChapterBuilt); each boss fights on its stand-in under its own id until then.
		BossClearArcane("bridge_crossed", "Across the Water", "bridge_constable", "the Last Bridge", 350),
		BossClearArcane("court_emptied", "No One Left", "court_seneschal", "the Court", 400),
		// The Throne's clear pays a wizard too since 2026-11-03: Nimue, the Lady of the Lake, the one he kept by him
		// (.ai/endgame-castle.md decision 11), freed when he falls. Not a second deed for the same fall - one row says
		// it. A wizard here is past every gate (UnlockCatalog: outside the campaign gate), so 2.5's rule - the castle
		// pays Arcane Energy, never what a gate could count - still holds. Sealed while she is a secret (until the
		// Throne opens), as a side event that frees a secret wizard is. A save that has already beaten him is handed
		// her by the reconcile at the end of its next run (AchievementManager.Reconcile), which owes her freeing too.
		new()
		{
			Id = TakenBackId, DisplayName = "Taken Back",
			Description = "Defeat the boss of the Throne.",
			RewardText = "Frees Nimue and 1,000 Arcane Energy", CurrencyReward = 1000,
			CharacterUnlockId = "nimue",
			Category = AchievementCategory.Exploration,
			Kind = AchievementKind.Road,
			Visibility = AchievementVisibility.ChapterOpen,
			StageId = StageOfBoss(ThroneBossId),
			BossId = ThroneBossId,
			SealedLine = SomeoneHeldIn(StageOfBoss(ThroneBossId)),
			Measure = c => c.Lifetime.HasDefeatedBoss(ThroneBossId) ? 1f : 0f,
			Target = 1,
		},
		// THE TAKER TAKEN (.ai/dark-wizard-playable.md). Eric, 2026-11-02, a second time: "dark wizard should unlock
		// after defeating all stages on hard mode" - every chapter's boss beaten on Hardcore, the stricter reading
		// the first build left for later. Read off SaveData.MaxDifficultyCleared (each chapter's hardest clear,
		// written by AchievementManager.ApplyRunAchievements) out of every chapter of the campaign, built or not,
		// so it cannot be done until the Throne exists. Still a Road deed of the Throne, listed once it is playable:
		// the end of that road is where it is finished. No BossId: the chapter's clear is Taken Back, and a second
		// deed naming the boss would be read as the Throne's warden deed (StageRewards.WardenDeed). Pays him alone -
		// his spell, The Taking, is a Conjunction he opens on, which its own Feat pays everyone else (ComboCatalog);
		// he is never named while locked (CodexRows.HidesWizard: "Frees someone").
		new()
		{
			Id = TakerTakenId, DisplayName = "The Taker Taken",
			Description = "Defeat every chapter's boss on Hardcore.",
			RewardText = "Unlocks The Dark Wizard",
			CharacterUnlockId = "dark_wizard",
			Category = AchievementCategory.Challenge,
			Kind = AchievementKind.Road,
			Visibility = AchievementVisibility.ChapterOpen,
			StageId = StageOfBoss(ThroneBossId),
			Measure = c => ChaptersClearedAt(c, GlobalStatsManager.PresetRankHardcore),
			Target = CampaignChapterCount,
		},
		// The campaign-completion deed retains its ID and progress after Merlin moves to the Circle.
		// Its replacement reward is currency; gods remain an extended-story design note.
		new()
		{
			Id = OneHeMissedId, DisplayName = "The Road Restored",
			Description = "Defeat every chapter's boss on Default or harder.",
			RewardText = "1000 Arcane Energy", CurrencyReward = 1000,
			Category = AchievementCategory.Challenge,
			Measure = c => ChaptersClearedAt(c, GlobalStatsManager.PresetRankDefault),
			Target = CampaignChapterCount,
		},
		// The two early chapters (.ai/campaign.md) pay in boons, for the reason the Frozen Waste and
		// the Sands do. Both boons were sold-but-never-sold (the shop registers no boons), so until then
		// no new save could ever be dealt them. Listed only once their chapter is playable.
		// The Hayward taught the Ascension until the slow start (2026-11-02); a landmark teaches it now (Teachings).
		BossClearBoon("fields_hurdled", "Round the Hurdle", "fields_hayward", "the Fallow Fields", "thornseed", "Thornseed"),
		BossClearBoon("circle_unclosed", "Out of the Ring", "heath_ninth_stone", "the Broken Circle", "shadegrease", "Shadegrease"),
		// THE FAR SHORE'S CHAPTER CLEARS (.ai/second-half.md 4.3, marked by F0): each warden's deed pays its god -
		// the one unlock a far chapter carries - and is that god's one source (UnlockCatalog). Not listed until its
		// chapter is playable (IsChapterBuilt), and sorted after the campaign's (CampaignRankOfStage). A line a
		// chapter, each its piece's alone: replace your comment line with your deed; leave the others as they are.
		// F3 (White Downs): the Unstrung Hunter's deed, freeing Artemis.
		// (F6's line is below; leave this one as it is)
		// F6 (Drowned Shore): the Weir's deed, freeing Manannan mac Lir.
		// (F7's line is below; leave this one as it is)
		// F7 (Three Ways): the Three-Faced's deed, freeing Hecate.
		// (F8's line is below; leave this one as it is)
		// F8 (Twelfth Hour): the Eclipse's deed, freeing Ra.
		// (F9's line is below; leave this one as it is)
		// F9 (Thunder Fell): the Still Eye's deed, freeing Ukko.
		// (F10's line is below; leave this one as it is)
		// F10 (Bronze Isle): Talos's deed, freeing Hephaestus.
		// The Sketchbook has no deed at all (Eric, 2026-11-02: "the sketchbook should not have any unlocks
		// or any achievements"). Its clear, Best Drawing (400 Arcane Energy), was the last; the kids' chapter
		// is played for itself. RegressionChecks.ValidateSketchbookHoldsNothing keeps it that way.
		new()
		{
			Id = "relic_hunter", DisplayName = "Relic Hunter",
			Description = "Collect 12 different relics.",
			RewardText = "250 Arcane Energy", CurrencyReward = 250,
			Category = AchievementCategory.Exploration,
			Kind = AchievementKind.Collection,
			Measure = c => c.Lifetime.ChestItemsCollected.Count, Target = 12,
		},
		// The collector's wizard, freed by collecting. A hundred is about six runs that reach the boss on the chest
		// economy where elites carry them (about seventeen a run - _ChestSim, .ai/passives-and-items.md section 8);
		// it was thirty, two runs, which made him a gift rather than a collection (Eric, 2026-11-03). Lifetime chests
		// are a stat every save has kept since the campaign began, so nothing new is recorded, and a save that was
		// handed him at thirty keeps him (AchievementManager.Reconcile only ever grants).
		// A Feat: it names no chapter, so every gate can count him (GlobalStatsManager.GateWizardIds) - which is what
		// holds the number down. The Emberdeep counts him, and a player who beats each of the eight chapters before it
		// at the first try reaches it with about 134 chests; 150 would have held that player at its gate to farm.
		new()
		{
			Id = "mortlake_library", DisplayName = "The Library at Mortlake",
			Description = $"Open {MortlakeChestTarget} chests.",
			RewardText = "Frees John Dee", CharacterUnlockId = "john_dee",
			Category = AchievementCategory.Exploration,
			Measure = c => c.Lifetime.TotalChestsOpened, Target = MortlakeChestTarget,
		},
		// THE COURSING. An optional hunt for Laelaps after his rescue in the Fields. Only a kill by the Familiar taken as a dog counts
		// (Familiar.Strike), and only a Black Hare - the Circle's fast body and the Warrener's summons - so
		// the deed names one map, one form and one quarry. Measured over one run (RunEvents, then the
		// lifetime best), so the menu shows the best run so far and the pause screen this run's count.
		// A Road deed of the Circle, paying currency rather than a second rescue.
		new()
		{
			Id = "coursing", DisplayName = "The Coursing",
			RequiredCharacterId = "laelaps",
			Description = $"As Laelaps in the Broken Circle, let your hound Familiar run down {CoursingTarget} Black Hares in one run.",
			RewardText = "50 Arcane Energy", CurrencyReward = 50,
			Category = AchievementCategory.Challenge,
			Kind = AchievementKind.Road,
			Visibility = AchievementVisibility.ChapterOpen,
			StageId = "stage_10",
			Measure = c => Math.Max(c.Lifetime.BestLaelapsHaresCoursed,
				c.Run?.CharacterId == "laelaps" && c.Run.StageId == "stage_10" ? c.Run.HaresCoursed : 0),
			Target = CoursingTarget,
		},
		new()
		{
			Id = "set_collector", DisplayName = "Attuned",
			Description = "Complete 3 different relic sets.",
			RewardText = "250 Arcane Energy", CurrencyReward = 250,
			Category = AchievementCategory.Exploration,
			Kind = AchievementKind.Collection,
			Measure = c => c.Lifetime.ChestSetsCompleted.Count, Target = 3,
		},
		new()
		{
			Id = "many_hands", DisplayName = "Many Hands",
			Description = "Play a run as 4 different wizards.",
			RewardText = "200 Arcane Energy", CurrencyReward = 200,
			Category = AchievementCategory.Exploration,
			Measure = c => c.Lifetime.CharactersPlayed.Count(id => !id.Equals("test_wizard", StringComparison.OrdinalIgnoreCase)),
			Target = 4,
		},
		// THE GRIMOIRE RESTORED (.ai/achievements-and-secrets.md 2.5, decision 1). Every spell recovered was
		// the final gate's key until schema 19; the gate counts wizards now, and the spells are the
		// collection. Measured live against the catalog, so a spell added later raises the target for those
		// still collecting - and needs no migration, which a gate counting spells did every time.
		// The combinations are left out (UnlockCatalog.GrimoireSpellIds).
		new()
		{
			Id = GrimoireRestoredId, DisplayName = "The Grimoire Restored",
			Description = "Recover every spell.",
			RewardText = "500 Arcane Energy", CurrencyReward = 500,
			Category = AchievementCategory.Exploration,
			Kind = AchievementKind.Collection,
			Measure = c => GlobalStatsManager.GetGrimoireSpellCount(c.Save),
			Target = UnlockCatalog.GrimoireSpellIds.Count(),
		},
		// THE FAR SHORE'S HALF-WIDE FEATS (.ai/second-half.md 4.3, marked by F0), Arcane Energy only. Section 9
		// names a piece for the first alone; the others are given to the piece that can first measure them.
		// F4: Under the Far Sky - make tonight's Conjunction (section 5). Met in the run (the achievement watch reads
		// RunResult.FarSkyMade on its one-second snapshot), so a run that makes it and is then quit keeps it.
		new()
		{
			Id = FarSky.UnderTheFarSkyId, DisplayName = "Under the Far Sky",
			Description = "On the far shore, make the Conjunction the far sky names.",
			RewardText = $"{FarSky.UnderTheFarSkyCurrency} Arcane Energy", CurrencyReward = FarSky.UnderTheFarSkyCurrency,
			Category = AchievementCategory.Mastery,
			Kind = AchievementKind.Feat,
			Visibility = AchievementVisibility.FarSky,
			Measure = c => (c.Save != null && GlobalStatsManager.IsAchievementUnlocked(c.Save, FarSky.UnderTheFarSkyId))
				|| c.Run?.FarSkyMade == true ? 1f : 0f,
			Target = 1,
		},
		// (the next line is F4's too; leave this one as it is)
		// F4: Every Conjunction Known - every recipe made once, measured live against ComboCatalog.Fusions: a recipe
		// added later (F6-F10's) raises the target for those still collecting, as the Grimoire's spells do. "Made" is
		// the recipe's own Feat, the code's one record of it (ComboCatalog.IsMet).
		new()
		{
			Id = FarSky.EveryConjunctionKnownId, DisplayName = "Every Conjunction Known",
			Description = "Make every Conjunction once.",
			RewardText = $"{FarSky.EveryConjunctionKnownCurrency} Arcane Energy", CurrencyReward = FarSky.EveryConjunctionKnownCurrency,
			Category = AchievementCategory.Mastery,
			Kind = AchievementKind.Collection,
			Visibility = AchievementVisibility.FarSky,
			Measure = c => ComboCatalog.Fusions.Count(r => ComboCatalog.IsMet(r, c.Save)),
			Target = ComboCatalog.Fusions.Count(),
		},
		// (F10's line is below; leave this one as it is)
		// F10: Every Seat Lit - all six gods rekindled.
		// (F10's second line is below; leave this one as it is)
		// F10: All the Gods' Forms - each god's Apotheosis taken.
	}).Concat(SideEventDeeds()).Concat(SecretDeeds()).Concat(CombinationDeeds()).ToList();

	/// <summary>
	/// One Road deed for every side event of every chapter (SideEventDirector.EventsFor - a chapter may hold
	/// two), in campaign order and then the chapter's own order. Its reward is whatever UnlockCatalog lists at
	/// the site; its words are <see cref="SideEventWords"/>. Completed the moment the event is won
	/// (DiscoveryRewards), measured by LifetimeStats.EventsWon - both by site, never by chapter.
	/// </summary>
	private static IEnumerable<AchievementDefinition> SideEventDeeds()
	{
		foreach (StageDefinition stage in StageCatalog.InDisplayOrder)
		foreach (SideEventEntry entry in global::SideEventDirector.EventsFor(stage.Id))
		{
			string siteId = entry.SiteId;
			if (string.IsNullOrWhiteSpace(siteId))
				continue;
			(string Site, string Name, string Description, string Reward) words = WordsFor(siteId);
			List<UnlockDefinition> pays = UnlockCatalog.ForDiscoverySite(siteId).ToList();
			string wizard = pays.FirstOrDefault(d => d.Kind == UnlockKind.Character)?.Id ?? string.Empty;
			yield return new AchievementDefinition
			{
				Id = EventDeedId(siteId),
				// RegressionChecks.ValidateAchievements reports a site with no words; the id stands in.
				DisplayName = words.Name ?? siteId,
				Description = words.Description ?? string.Empty,
				RewardText = words.Reward ?? string.Empty,
				SpellUnlockId = pays.FirstOrDefault(d => d.Kind == UnlockKind.Spell)?.Id ?? string.Empty,
				BoonUnlockId = pays.FirstOrDefault(d => d.Kind == UnlockKind.Boon)?.Id ?? string.Empty,
				CharacterUnlockId = wizard,
				Category = AchievementCategory.Exploration,
				Kind = AchievementKind.Road,
				Visibility = AchievementVisibility.ChapterOpen,
				CompletesInRun = true,
				StageId = stage.Id,
				SiteId = siteId,
				SealedLine = UnlockCatalog.GetCharacter(wizard)?.IsSecret == true ? SomeoneHeldIn(stage.Id) : string.Empty,
				Measure = c => c.Lifetime.HasWonEvent(siteId) ? 1f : 0f,
				Target = 1,
			};
		}
	}

	/// <summary>
	/// One Feat for every combination (ComboCatalog), "Conjunction: Name": completed the first time a run
	/// meets the recipe (Node2DGame records it as the strip first offers it, through AchievementManager.CompleteNow)
	/// and paid with the combination there and then. Its measure is its own record - the deed is the fact -
	/// so no save field is added for it (no schema bump), and a run's end never completes it twice.
	/// </summary>
	private static IEnumerable<AchievementDefinition> CombinationDeeds()
	{
		foreach (ComboDefinition recipe in ComboCatalog.All)
		{
			string id = ComboCatalog.FeatId(recipe.Id);
			// The Rite's row: the Warden of the Deep's deed pays the Rite itself, so its Feat - met the first
			// time the strip offers it - pays Arcane Energy, and its line is the gate rather than two Ascensions.
			if (recipe.IsRite)
			{
				yield return new AchievementDefinition
				{
					Id = id,
					DisplayName = $"Conjunction: {recipe.Name}",
					Description = $"In one run, reach level {SovereignRite.MinimumPlayerLevel} holding {SovereignRite.MinimumActiveSpells} active spells and the {recipe.Name}.",
					RewardText = $"{SovereignRiteFeatCurrency} Arcane Energy",
					CurrencyReward = SovereignRiteFeatCurrency,
					Category = AchievementCategory.Mastery,
					Kind = AchievementKind.Feat,
					Visibility = AchievementVisibility.Recipe,
					CompletesInRun = true,
					ComboId = recipe.Id,
					SealedName = "Conjunction: ???",
					SealedReward = "???",
					Measure = c => c.Save != null && GlobalStatsManager.IsAchievementUnlocked(c.Save, id) ? 1f : 0f,
					Target = 1,
				};
				continue;
			}
			yield return new AchievementDefinition
			{
				Id = id,
				DisplayName = $"Conjunction: {recipe.Name}",
				Description = $"In one run, take {recipe.First.SpellName} down {recipe.First.AscensionName} and {recipe.Second.SpellName} down {recipe.Second.AscensionName}.",
				RewardText = $"Unlocks {recipe.Name}",
				SpellUnlockId = recipe.Id,
				Category = AchievementCategory.Mastery,
				Kind = AchievementKind.Feat,
				Visibility = AchievementVisibility.Recipe,
				CompletesInRun = true,
				ComboId = recipe.Id,
				SealedName = "Conjunction: ???",
				SealedReward = "Unlocks ???",
				Measure = c => c.Save != null && GlobalStatsManager.IsAchievementUnlocked(c.Save, id) ? 1f : 0f,
				Target = 1,
			};
		}
	}

	/// <summary>What the Sovereign Rite's Feat pays: the Rite itself is the Warden of the Deep's.</summary>
	public const int SovereignRiteFeatCurrency = 250;

	/// <summary>Whether a side event's site has words written for its deed. For the validator.</summary>
	public static bool HasSideEventWords(string siteId) => WordsFor(siteId).Site != null;

	// A site's words: the table's, or the stand-in second event's on a tool run that has it (SecondEventStandIn).
	// Default (all null) when there are none.
	private static (string Site, string Name, string Description, string Reward) WordsFor(string siteId)
	{
		var words = SideEventWords.FirstOrDefault(w => w.Site.Equals(siteId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		if (words.Site == null && SecondEventStandIn.Active && SecondEventStandIn.IsStandInSite(siteId))
			words = SecondEventStandIn.Words;
		return words;
	}

	/// <summary>The site ids the words table names, for the validator's other direction.</summary>
	public static IEnumerable<string> SideEventWordSites => SideEventWords.Select(w => w.Site);

	/// <summary>One Secret deed for every secret written down, built or planned, in campaign order.</summary>
	private static IEnumerable<AchievementDefinition> SecretDeeds() =>
		SecretCatalog.Defined
			.OrderBy(s => CampaignRankOfStage(s.StageId))
			.Select(SecretCatalog.AsAchievement);

	/// <summary>
	/// The deeds a save-less caller may show: all of them but a deed for a chapter not built yet, a
	/// Hidden one, and one that frees a secret wizard - those need the save to know whether they have
	/// been seen or revealed (<see cref="IsListed"/>). The old Unlocks list reads this until the
	/// achievements page (piece A2) reads <see cref="ListedFor"/>. Evaluation still walks <see cref="All"/>.
	/// </summary>
	public static IEnumerable<AchievementDefinition> Listed => ListedFor(null);

	/// <summary>The deeds this save is shown in full (<see cref="IsListed"/>).</summary>
	public static IEnumerable<AchievementDefinition> ListedFor(SaveData save) => All.Where(d => IsListed(d, save));

	/// <summary>
	/// Whether a deed is shown in full on this save (.ai/achievements-and-secrets.md 4.2):
	///   - Always: yes;
	///   - ChapterOpen: once its chapter is playable, and a deed that frees a secret wizard only once he is
	///     revealed (his chapter open, or him already free) - before that it is a sealed row (<see cref="IsSealed"/>);
	///   - Hidden: once the secret is built and has been seen or found.
	/// A null save reveals nothing secret.
	/// </summary>
	public static bool IsListed(AchievementDefinition deed, SaveData save)
	{
		if (deed == null || !IsChapterBuilt(deed))
			return false;
		if (!string.IsNullOrEmpty(deed.RequiredCharacterId)
			&& !GlobalStatsManager.IsCharacterUnlocked(save, deed.RequiredCharacterId))
			return false;
		switch (deed.Visibility)
		{
			case AchievementVisibility.Hidden:
				return save != null && SecretCatalog.Get(deed.SecretId)?.IsBuilt == true
					&& (save.HasGlimpsedSecret(deed.SecretId) || save.HasFoundSecret(deed.SecretId)
						|| GlobalStatsManager.IsAchievementUnlocked(save, deed.Id));
			case AchievementVisibility.ChapterOpen:
				return !IsSealed(deed, save);
			case AchievementVisibility.Recipe:
				return save != null && ComboCatalog.IsKnown(ComboCatalog.ForId(deed.ComboId), save);
			case AchievementVisibility.FarSky:
				return save != null && Teachings.HasLearned(save, Teaching.FarSky);
			default:
				return true;
		}
	}

	/// <summary>
	/// A deed that frees a secret wizard whose chapter has not opened: shown as a sealed row with its
	/// <see cref="AchievementDefinition.SealedLine"/>, never named (the character select's rule).
	/// </summary>
	public static bool IsSealed(AchievementDefinition deed, SaveData save)
	{
		if (deed == null || string.IsNullOrWhiteSpace(deed.CharacterUnlockId))
			return false;
		UnlockDefinition wizard = UnlockCatalog.GetCharacter(deed.CharacterUnlockId);
		return wizard?.IsSecret == true && !GlobalStatsManager.IsCharacterRevealed(save, wizard.Id);
	}

	/// <summary>The deeds that belong to a chapter, in the order of <see cref="All"/>.</summary>
	public static IEnumerable<AchievementDefinition> ForStage(string stageId) =>
		All.Where(d => d.StageId.Equals(stageId ?? string.Empty, StringComparison.OrdinalIgnoreCase));

	/// <summary>Kept for the old callers: listed with no save in hand.</summary>
	public static bool IsListed(AchievementDefinition deed) => IsListed(deed, null);

	// A deed whose chapter is not built yet is a deed nobody can do: not listed, still evaluated.
	private static bool IsChapterBuilt(AchievementDefinition deed)
	{
		if (!string.IsNullOrWhiteSpace(deed.BossId))
		{
			BossDefinition boss = BossCatalog.GetById(deed.BossId);
			int index = boss != null ? BossCatalog.StageIndexOf(boss) : -1;
			return index < 0 || StageCatalog.GetByIndex(index).IsPlayable;
		}
		StageDefinition stage = StageCatalog.GetById(deed.StageId);
		return stage == null || stage.IsPlayable;
	}

	/// <summary>"the Sunken Cave", "the Fallow Fields": a chapter's name mid-sentence, with one article.</summary>
	public static string ChapterPhrase(string stageId)
	{
		string name = StageCatalog.DisplayNameFor(stageId) ?? string.Empty;
		return name.StartsWith("The ", StringComparison.Ordinal) ? "the " + name.Substring(4) : "the " + name;
	}

	// "The Cursed Dungeon holds someone" (.ai/achievements-and-secrets.md 4.2): a sealed row's line.
	private static string SomeoneHeldIn(string stageId)
	{
		string phrase = ChapterPhrase(stageId);
		return phrase.Length > 0 ? char.ToUpperInvariant(phrase[0]) + phrase.Substring(1) + " holds someone" : string.Empty;
	}

	// The chapter clears are written in the order the chapters were made, and the deeds page lists
	// them as written. Moving them into campaign order here - in the slots they already hold, so no
	// other deed moves - is what keeps that page following the campaign when the order changes,
	// rather than a second copy of the order kept by hand in the list above.
	private static List<AchievementDefinition> InCampaignOrder(List<AchievementDefinition> deeds)
	{
		var slots = new List<int>();
		for (int i = 0; i < deeds.Count; i++)
		{
			if (!string.IsNullOrWhiteSpace(deeds[i].BossId))
				slots.Add(i);
		}
		var ordered = slots.Select(i => deeds[i])
			.OrderBy(d => CampaignRank(d.BossId))
			.ToList();
		for (int k = 0; k < slots.Count; k++)
			deeds[slots[k]] = ordered[k];
		return deeds;
	}

	private static int CampaignRank(string bossId)
	{
		BossDefinition boss = BossCatalog.GetById(bossId);
		int index = boss != null ? BossCatalog.StageIndexOf(boss) : -1;
		return index >= 0 ? CampaignRankOfStage(StageCatalog.GetByIndex(index).Id) : int.MaxValue;
	}

	// The campaign's chapters in their order, then the far shore's after them (StageCatalog.OrderedPositionOf), then
	// anything outside both.
	private static int CampaignRankOfStage(string stageId)
	{
		int position = StageCatalog.OrderedPositionOf(stageId);
		return position >= 0 ? position : int.MaxValue;
	}

	// The chapter a boss's deed belongs to: the boss's own (BossCatalog), as an id.
	private static string StageOfBoss(string bossId)
	{
		BossDefinition boss = BossCatalog.GetById(bossId);
		int index = boss != null ? BossCatalog.StageIndexOf(boss) : -1;
		return index >= 0 ? StageCatalog.GetByIndex(index).Id : string.Empty;
	}

	// The five element achievements differ only by element and reward, so they are built rather
	// than written out - five near-identical literals is five chances to paste the wrong element.
	private static AchievementDefinition ElementAdept(string id, string name, string element, string spellId, string spellName) => new()
	{
		Id = id,
		DisplayName = name,
		Description = $"Hold 4 instances of {element}.",
		RewardText = $"Unlocks {spellName}",
		SpellUnlockId = spellId,
		Category = AchievementCategory.Mastery,
		Measure = c => c.Lifetime.BestCountForElement(element),
		Target = 4,
	};

	// Boss ids are matched exactly now. They used to be matched by *substring* against terms like
	// "forest" and "castle", guarded only by a validator warning - so a boss id that happened to
	// contain another's keyword would silently grant the wrong spell.
	private static AchievementDefinition BossClear(string id, string name, string bossId, string place, string spellId, string spellName) => new()
	{
		Id = id,
		DisplayName = name,
		Description = $"Defeat the boss of {place}.",
		RewardText = $"Unlocks {spellName}",
		SpellUnlockId = spellId,
		Category = AchievementCategory.Exploration,
		Kind = AchievementKind.Road,
		Visibility = AchievementVisibility.ChapterOpen,
		StageId = StageOfBoss(bossId),
		BossId = bossId,
		Measure = c => c.Lifetime.HasDefeatedBoss(bossId) ? 1f : 0f,
		Target = 1,
	};

	// Two chapter clears pay in a boon rather than a spell. Not an oversight and not filler: the
	// shop's stock is the same finite pool the achievement track draws from, and moving all seven
	// boss rewards onto spells would have left four spells in it. A boon is a real reward and the
	// Frozen Waste handing over Rimebriar is a better fit than any spell left unclaimed was.
	// alsoReward: a second thing the kill pays that is not an item, said on the same line. None does since
	// the slow start moved the Hayward's "teaches Ascension" to a landmark (Teachings).
	private static AchievementDefinition BossClearBoon(string id, string name, string bossId, string place, string boonId, string boonName,
		string alsoReward = "") => new()
	{
		Id = id,
		DisplayName = name,
		Description = $"Defeat the boss of {place}.",
		RewardText = $"Unlocks {boonName}{(string.IsNullOrEmpty(alsoReward) ? "" : " and " + alsoReward)}",
		BoonUnlockId = boonId,
		Category = AchievementCategory.Exploration,
		Kind = AchievementKind.Road,
		Visibility = AchievementVisibility.ChapterOpen,
		StageId = StageOfBoss(bossId),
		BossId = bossId,
		Measure = c => c.Lifetime.HasDefeatedBoss(bossId) ? 1f : 0f,
		Target = 1,
	};

	/// <summary>
	/// A far chapter's warden deed, which rekindles the god whose seat it is (.ai/second-half.md 4.3, 6.3; plumbing F1).
	/// A chapter's piece writes one line in its marked slot in <see cref="All"/> ("THE FAR SHORE'S CHAPTER CLEARS") -
	/// <c>SeatClear("stage_14", "...", "&lt;boss id&gt;",
	/// "Artemis")</c> - and nothing else: the id is the seat's (<see cref="SeatDeedId"/>, which the god's
	/// UnlockCatalog.FromGod line names too), and the god it pays is read from the catalog
	/// (UnlockCatalog.GodSeatedAt), so a chapter that merges before its god pays nobody and says so on the far
	/// shore's to-do line until the god lands. <paramref name="godName"/> is written out rather than loaded, as a
	/// recipe's names are, because a save migration builds this list before any resource is
	/// (RegressionChecks.ValidateGods holds it to the god's CharacterData).
	/// </summary>
	public static AchievementDefinition SeatClear(string stageId, string name, string bossId, string godName) => new()
	{
		Id = SeatDeedId(stageId),
		DisplayName = name,
		Description = $"Defeat the boss of {ChapterPhrase(stageId)}.",
		RewardText = $"{QuestLog.RekindlesVerb} {godName}",
		CharacterUnlockId = UnlockCatalog.GodSeatedAt(stageId)?.Id ?? string.Empty,
		Category = AchievementCategory.Exploration,
		Kind = AchievementKind.Road,
		Visibility = AchievementVisibility.ChapterOpen,
		StageId = stageId ?? string.Empty,
		BossId = bossId ?? string.Empty,
		SealedLine = SomeoneHeldIn(stageId),
		Measure = c => c.Lifetime.HasDefeatedBoss(bossId) ? 1f : 0f,
		Target = 1,
	};

	// A chapter clear that pays Arcane Energy, as the Emberdeep's does: the castle's three.
	private static AchievementDefinition BossClearArcane(string id, string name, string bossId, string place, int amount) => new()
	{
		Id = id,
		DisplayName = name,
		Description = $"Defeat the boss of {place}.",
		// "1,000", as "Defeat 2,000 enemies" is written, whatever the machine's culture.
		RewardText = amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " Arcane Energy",
		CurrencyReward = amount,
		Category = AchievementCategory.Exploration,
		Kind = AchievementKind.Road,
		Visibility = AchievementVisibility.ChapterOpen,
		StageId = StageOfBoss(bossId),
		BossId = bossId,
		Measure = c => c.Lifetime.HasDefeatedBoss(bossId) ? 1f : 0f,
		Target = 1,
	};

	public static AchievementDefinition GetById(string id) =>
		string.IsNullOrWhiteSpace(id) ? null : All.FirstOrDefault(a => a.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
}
