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
	/// <summary>
	/// Endless's Feats (EndlessCatalog, AchievementDefinitions.Endless.cs): not on the page at all until endless is
	/// open - the Throne taken back - since nothing of endless is on show before (.ai/endless-mode.md 2.2).
	/// </summary>
	Endless,
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
	/// <summary>
	/// A named-character challenge is listed only after that character is owned. On a wizard's opener deed
	/// (<see cref="IsWizardOpener"/>, in-flight 84) it is also the only wizard the deed counts for: done in a run played
	/// as him, once he is free (<see cref="AchievementDefinitions.PlayedAsFreed"/>), and a sealed row until then.
	/// </summary>
	public string RequiredCharacterId { get; init; } = string.Empty;

	/// <summary>
	/// A wizard's opener deed (in-flight 84, Eric 2026-11-09: "spells that are another wizard's default spell to only be
	/// unlockable once that wizard is unlocked" - "those achievements should require being completed with that wizard"):
	/// it pays <see cref="RequiredCharacterId"/>'s starting spell, counts only as him, and is a sealed row on the
	/// achievements page while he is shown but not free (<see cref="AchievementDefinitions.IsSealed"/>).
	/// </summary>
	public bool IsWizardOpener { get; init; }

	/// <summary>
	/// For an opener deed done at a side event or a secret: the deed it is done beside (an event_ or secret_ deed), which
	/// keeps whatever else it pays and asks no wizard - it is the road's and the record's. Completed with that deed, the
	/// moment it is done, when the run is played as <see cref="RequiredCharacterId"/> and he is free
	/// (AchievementManager.Complete). Empty on every other deed.
	/// </summary>
	public string AsWizardOf { get; init; } = string.Empty;
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

public static partial class AchievementDefinitions
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
	/// Chests in ONE run that pay Folio (the Magpie feat): John Dee opens on it, and everyone else earns it by collecting
	/// as he does. A run that reaches the boss opens about seventeen on the elite-chest economy (_ChestSim,
	/// .ai/passives-and-items.md section 8), so ten is a good run's worth, not a full one's.
	/// </summary>
	public const int MagpieChestTarget = 10;

	/// <summary>
	/// Kills numbered in Lightning's ink in ONE run that pay Corposant (the Storm Petrel feat): a Lightning spell's, a spell
	/// branched into Lightning, the Lightning tier's chain (RunEvents.RecordLightningKill). The Stormcaller opens on Chain
	/// Lightning, so every save can reach it from its first run. Measured 2026-11-09 (_BalanceHarness --minutes=15
	/// --character=3, seed 1234, the real save read-only): the bot spread its picks over five spells and never took Chain
	/// Lightning past level 3, and still made 310 by minute 5, 492 by minute 10 and 672 in the run. Five hundred is most
	/// of a run with Lightning in it, or the first half of one built round it - Magpie's "a good run's worth".
	/// </summary>
	public const int StormPetrelLightningKills = 500;

	/// <summary>
	/// Kills numbered in Water's ink or Darkness's in ONE run that pay Ink Ring (the Cormorant feat): the two halves of Ma
	/// Liang's ink, each counted where its number is chosen (RunEvents.RecordInkKill). Tituba opens on Riptide (Water), and
	/// Hollow Star (Darkness) is the Survivor feat's, so a save reaches it without the spell it pays. Measured 2026-11-09
	/// (_BalanceHarness --minutes=15 --character=5, seed 1234, the real save read-only; the bot kept Riptide at level 2 to
	/// 6 among four or five spells): the kiting bot died at minute 7 with 168, the --survivor bot at minute 7.3 with 233
	/// (119 by minute 5), and the --immortal bot made 195 by minute 5, 638 by minute 10 and 1,235 in the run. Five hundred
	/// is Storm Petrel's number and the same ask: most of a run with Water or Darkness in it, or the first half of one built
	/// round them.
	/// </summary>
	public const int CormorantInkKills = 500;

	/// <summary>
	/// Kills numbered in Grass's ink or Metal's in ONE run that pay Windfall (the Fieldfare feat): the living wood and the
	/// iron, each counted where its number is chosen (RunEvents.RecordGrassMetalKill). Väinämöinen opens on Bramble Seed
	/// (Grass), Wayland on Iron Palisade (Metal), Baba Yaga on Thorned Vine (numbered in Grass), and Thorned Vine is the
	/// Forest's deed, so a save reaches it without the spell it pays. Measured 2026-11-09 (_BalanceHarness --minutes=15,
	/// seed 1234, the real save read-only): Väinämöinen's kiting bot (--character=6) lived the run and made 378 by minute 5,
	/// 1,234 by minute 10 and 1,676 in all; Wayland's (--character=12) died at minute 5.4 with 190, and his --immortal bot
	/// made 303 by minute 5, 1,236 by minute 10 and 2,108 in all. Both openers kill in a crowd, so they reach Storm Petrel's
	/// and Cormorant's 500 by minute 6: the same ask in time - most of a run with Grass or Metal in it, where the Stormcaller
	/// bot made 500 at minute 10 - is a thousand.
	/// </summary>
	public const int FieldfareGrassMetalKills = 1000;

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

	/// <summary>What a mystery deed's name and reward read until it is done: The Taker Taken's (Eric, 2026-11-09).</summary>
	public const string MysteryName = "???";

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
	/// never a list of its own, so a chapter outside the campaign is never asked for.
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
		// The grove, the lanterns and the caravan master paid Väinämöinen's, Paracelsus's and Wayland's openers until in-flight
		// 84 moved each to a deed done as its wizard (WizardOpenerRoadDeeds); their own deeds pay Arcane Energy instead.
		// Wayland's is done at the Cold Forge since in-flight 66 (Eric, 2026-11-10); the caravan's deed keeps its Energy.
		("the_grove", "The Grove Held", "Hold the corrupted grove in the Enchanted Forest until it grows back.", $"{OpenerMovedArcane} Arcane Energy"),
		("walking_hut", "The Hut Herded Home", "Drive the walking hut of the Enchanted Forest into the bone-fenced yard.", "Frees Baba Yaga"),
		("the_well", "The Last Coin", "Keep the last clean well in the Fallow Fields from being fouled.", "Unlocks Wishing Coin"),
		("tithe_scale", "The Scale Held Level", "Fill both pans of the Tithe-Reeve's scale in the Fallow Fields and hold its beam level.", "Frees Medea"),
		("the_moot", "The Circle Relit", "Restore the shattered council wards of the Broken Circle to free their keeper.", "Frees Merlin, unlocks Orb of Ages and Gilded Mote"),
		("gaolers_key", "The Open Cell", "Slip the Gaoler's key past his lantern-bearers to the locked cell in the Cursed Dungeon.", "Frees Tituba"),
		("bog_lanterns", "The Lanterns Out", "Snuff the lanterns of the Blighted Swamp and face what tends them.", $"{OpenerMovedArcane} Arcane Energy"),
		("gibbet_cage", "The Ferry Reached", "Lower the gibbet's cage in the Blighted Swamp and bring the man in it to the ferry landing alive.", "Frees Paracelsus"),
		("five_pointed_seal", "The Seal Broken", "Turn the four guardian stones to face the seal in the Mystic Ruins.", "Frees Abe no Seimei"),
		("nine_mouths", "The True Mouth", "Find the one mouth of nine her voice comes out of first, in the Sunken Cave.", "Frees The Cumaean Sibyl"),
		("the_thaw", "Out of the Ice", "Thaw the wizard out of the ice in the Frozen Waste.", "Frees Väinämöinen"),
		("caravan_master", "The Caravan Stopped", "Catch the caravan master on the Scorched Sands road.", $"{OpenerMovedArcane} Arcane Energy"),
		("tomb_beacons", "The Sun Let In", "Turn the bronze mirrors on the Scorched Sands until the sunlight reaches the disc over the dry tomb's door.", "Frees Imhotep"),
		("great_anvil", "The Chains Struck", "Bring the forge's own hammers down on the four chains in the Emberdeep.", "Frees Wayland the Smith"),
		("the_barge", "The Way Lit", "Light the dead lamps along the Last Bridge ahead of the barge below until it comes in to its landing.", "Frees Morgan le Fay"),
		("seers_voice", "The Last Reading", "Follow the voice reading room to room through the Court until you find the reader.", "Frees Nostradamus"),
		("dotted_door", "The Door Inked In", "Ink every line of the door sketched in the Court's Gallery, in one stroke.", "Frees Ma Liang"),
		// THE FAR SHORE (.ai/second-half.md 4.3, marked by F0): one side event a chapter, paying Arcane Energy and a
		// keepsake - never a spell, boon or wizard. Each piece replaces its own comment line with its words.
		("silver_hind", "The Hind at Gaze", "Stalk the Silver Hind on the White Downs until you stand at her side.", $"{SilverHindCurrency} Arcane Energy"),
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

	/// <summary>The White Downs' event's Arcane Energy (F3), once, with its deed.</summary>
	public const int SilverHindCurrency = 200;

	/// <summary>
	/// What a road deed pays, once, in place of the wizard's opener that moved off it to a deed done as that wizard
	/// (in-flight 84): the three side events (the grove, the lanterns, the caravan master). A deed must pay something, and
	/// Arcane Energy is the one prize no gate counts. A first guess for Eric to tune. The Sunken Cave's boss paid it too,
	/// until in-flight 66's reward review gave it the Emberdeep's 300 (Eric, 2026-11-10).
	/// </summary>
	public const int OpenerMovedArcane = 100;

	/// <summary>
	/// What a far chapter's side event pays, once, with its deed: Arcane Energy, and the deed itself is the keepsake
	/// (.ai/second-half.md 4.3 - "an achievement, not an unlock"). A site here gives nothing in UnlockCatalog; the
	/// first win completes its deed and pays this, and every win after pays a chest, as an exhausted road site does
	/// (DiscoveryRewards.IsSiteExhausted). One line a chapter, each its piece's; F3 built the table with the first.
	/// A switch, not a dictionary: the deeds are built in a static initializer, and this must answer before any
	/// field of a partial class is known to exist.
	/// </summary>
	public static int SideEventCurrency(string siteId) => (siteId ?? string.Empty).ToLowerInvariant() switch
	{
		// Three road sites whose only prize was a wizard's opener (in-flight 84): the grove's and the lanterns' are held there
		// for their wizards now (WizardOpenerRoadDeeds, DiscoveryRewards.IsSiteExhausted), and the site's own deed pays this,
		// once. Wayland's moved off the caravan to the Cold Forge (in-flight 66); the caravan's deed still pays this.
		"the_grove" or "bog_lanterns" or "caravan_master" => OpenerMovedArcane,
		// F3 (White Downs)
		"silver_hind" => SilverHindCurrency,
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
		_ => 0,
	};

	/// <summary>
	/// True for a far chapter's side event: it pays its deed and Arcane Energy rather than an unlock, so "what the
	/// site gives" is the deed until it is done (the hint, the note's clue, the chest after).
	/// </summary>
	public static bool IsKeepsakeEvent(string siteId) => SideEventCurrency(siteId) > 0;

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
			// Lancet since in-flight 66 (Eric, 2026-11-09: "I like first blood unlocking lancet"): the lancet is the tool that
			// draws first blood. Canticle, which this paid, is the Sibyl's opener and earned as her (Swansong, below).
			Id = "first_blood", DisplayName = "First Blood",
			Description = "Defeat 1 enemy.",
			RewardText = "Unlocks Lancet", SpellUnlockId = Lancet.SpellId,
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
			// Shadegrease since in-flight 66's reward review (Eric, 2026-11-10): Meteor Swarm, which this paid, is the
			// Ninth Stone's (Out of the Ring) - this completed in the same run as Forest Cleared and Survivor, three spells
			// at once, while the Circle, the first wall, paid a +6% boon. And "without falling" is measured now: a win an
			// extra life saved does not count (RunResult.RevivesUsed). Off the run, then, as the openers are, since the
			// lifetime's victories never said how they were won; the menu's bar reads 0 until the run that does it.
			// From the boss's fall the snapshot is the win (Node2DGame.SnapshotRunResult), and the endless ledger is taken
			// at the first vigil, so a life spent in the night after it never takes back the clear.
			Id = "veteran", DisplayName = "Veteran",
			Description = "Clear a chapter without falling.",
			RewardText = "Unlocks Shadegrease", BoonUnlockId = "shadegrease",
			Category = AchievementCategory.Progress,
			Measure = c => c.Run != null && c.Run.Outcome == global::RunOutcome.Victory && c.Run.RevivesUsed == 0 ? 1f : 0f,
			Target = 1,
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
		// THE OPENERS EARNED AS THEIR WIZARDS (in-flight 84): the four Feats that paid only a wizard's opener are his now,
		// renamed in the Feats' house style - birds - and done only as him. Laelaps's goshawk is the hunter's own hawk.
		OpenerFeat("elementalist", "Goshawk", "laelaps", "As Laelaps, hold 4 instances of any one element.", "cyclone_slash",
			"Cyclone Slash", AchievementCategory.Mastery, c => RunPeakOfAny(c), 4),
		// Imhotep was High Priest of Ra at Heliopolis, whose sun-bird was the bennu - the heron the phoenix came from.
		OpenerFeat("fire_adept", "Bennu", "imhotep", "As Imhotep, hold 4 instances of Fire.", "scorching_ray", "Scorching Ray",
			AchievementCategory.Mastery, c => RunPeakOf(c, "Fire"), 4),
		// Medea's opener (in-flight 84): the pheasant is the bird of the Phasis, the river of her own Colchis. It asks
		// for Light held since in-flight 66's reward review (Eric, 2026-11-10): level 20 came at about minute 8 and said
		// nothing about her. Light is her grandfather the Sun and Solar Flare's own tag, and no Adept asks for it. The id
		// stays "max_level", the deed's since schema 19, so nothing that names it has to move.
		OpenerFeat("max_level", "Pheasant", "medea", "As Medea, hold 4 instances of Light.", "solar_flare", "Solar Flare",
			AchievementCategory.Mastery, c => RunPeakOf(c, "Light"), 4),
		// Canticle's deed (in-flight 66 and 84): harmony, as the Sibyl - the chord it sings is gathered from its casters'
		// elements, so the ask is four of them held at once. Swans are Apollo's, and she is his prophetess.
		OpenerFeat("swansong", "Swansong", "sibyl", "As the Cumaean Sibyl, hold four different elements at once.",
			"arcane_explosion", "Canticle", AchievementCategory.Mastery, c => c.Run.PeakDistinctElements, 4),
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
		// FOLIO'S FEAT (.ai/character-fit.md section 6, 2026-11-09): John Dee opens on Folio, and it is shared by collecting
		// the way he does - ten chests in a single run. One run, not a lifetime: Elite Hunter and The Library at Mortlake
		// already count every chest ever opened. Since in-flight 84 it is done as him, once he is free, so it is measured
		// off the run itself and the menu shows no bar; the pause screen shows this run's count.
		OpenerFeat("magpie", "Magpie", "john_dee", $"As John Dee, open {MagpieChestTarget} chests in a single run.", "folio", "Folio",
			AchievementCategory.Challenge, c => c.Run.ChestsOpened, MagpieChestTarget),
		// CORPOSANT'S FEAT (2026-11-09): St Elmo's fire is the sailor's sign of a storm, and the storm petrel is the bird
		// sailors saw before one - a bird named for St Peter, as Magpie (Folio's) is a bird too. Earned by fighting with
		// Lightning: kills numbered in its ink in a single run. Not the Lightning Adept (Stormguard's), which counts
		// Lightning held; this counts what Lightning killed. Measured off the best run kept in the lifetime stats
		// (LifetimeStats.BestLightningKillsInRun), so the menu shows the best run so far and the pause screen this run's.
		new()
		{
			Id = "storm_petrel", DisplayName = "Storm Petrel",
			Description = $"Defeat {StormPetrelLightningKills} enemies with Lightning in a single run.",
			RewardText = "Unlocks Corposant", SpellUnlockId = "corposant",
			Category = AchievementCategory.Challenge,
			Measure = c => c.Lifetime.BestLightningKillsInRun, Target = StormPetrelLightningKills,
		},
		// INK RING'S FEAT (2026-11-09): the cormorant is the black diving bird the river fishermen of Ma Liang's China work
		// with - ink-black, and at home in the water - and a bird beside Magpie and Storm Petrel. Earned by fighting with
		// the two halves of his ink: kills numbered in Water's ink or Darkness's in a single run. Since 2026-11-10 Ink Ring
		// is Ma Liang's opener (in-flight 94), so under in-flight 84 it is done AS him, once he is free, and measured off the
		// run itself as Magpie is; the menu shows no bar and the pause screen this run's count. LifetimeStats keeps
		// BestInkKillsInRun, which nothing reads now, so no save changes shape.
		OpenerFeat("cormorant", "Cormorant", "ma_liang", $"As Ma Liang, defeat {CormorantInkKills} enemies with Water or Darkness in a single run.",
			InkRing.SpellId, "Ink Ring", AchievementCategory.Challenge, c => c.Run.InkKills, CormorantInkKills),
		// WINDFALL'S FEAT (2026-11-09): the fieldfare is the winter thrush that comes down on an orchard for its windfalls,
		// the apples lying in the grass - a bird beside Magpie, Storm Petrel and Cormorant. Earned by fighting with the
		// orchard's two halves: kills numbered in Grass's ink or Metal's in a single run. Measured off the best run kept in
		// the lifetime stats (LifetimeStats.BestGrassMetalKillsInRun), as Storm Petrel and Cormorant are.
		new()
		{
			Id = "fieldfare", DisplayName = "Fieldfare",
			Description = $"Defeat {FieldfareGrassMetalKills} enemies with Grass or Metal in a single run.",
			RewardText = "Unlocks Windfall", SpellUnlockId = Windfall.SpellId,
			Category = AchievementCategory.Challenge,
			Measure = c => c.Lifetime.BestGrassMetalKillsInRun, Target = FieldfareGrassMetalKills,
		},
		// The Shrike (500 poisoned enemies in a run) paid Lancet until in-flight 66 moved Lancet to First Blood; it paid
		// nothing else, so it went (Eric, 2026-11-09).
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
		// Riptide, which this paid, is Tituba's opener and earned as her since in-flight 84 (The Undertow); the deed stays the
		// road's and pays Arcane Energy in its place - 300 since in-flight 66's reward review (Eric, 2026-11-10), the
		// Emberdeep's, which pays Fireball now: at 100 the Hollow Choir paid less than Exterminator.
		BossClearArcane("cave_choir_silenced", "Silence in the Deep", "cave_choir", "the Sunken Cave", 300),
		BossClear("swamp_cleansed", "Cut the Rot Out", "swamp_mother", "the Blighted Swamp", "mirefoot", "Mirefoot"),
		// The Archivist teaches rung III of the Conjunction ladder (ConjunctionLadder.PairShareDeedId; Teachings.PairSharesLesson):
		// "teaches a lesson", the one locked thing it puts on show (.ai/conjunction-progression.md section 3).
		BossClear("ruins_delver", "Ruins Delver", "ruins_sentinel", "the Mystic Ruins", "spiritual_weapon", "Spiritual Weapon", "teaches a lesson"),
		BossClearBoon("waste_thawed", "Broke the Guard", "frost_warden", "the Frozen Waste", "rimebriar", "Rimebriar"),
		// The boon it always paid. From schema 19 it paid Fireball too, moved here from the Warden of the Deep's deed while
		// the final gate counted every spell (.ai/achievements-and-secrets.md 1.4); the gate counts wizards now (2.5), so
		// Fireball went back to the fire depths in in-flight 66's reward review (Eric, 2026-11-10) - the Sands pay the sun.
		new()
		{
			Id = "sands_unwound", DisplayName = "Unwound",
			Description = "Defeat the boss of the Scorched Sands.",
			RewardText = "Unlocks Sunsteel Filament",
			BoonUnlockId = "sunsteel_filament",
			Category = AchievementCategory.Exploration,
			Kind = AchievementKind.Road,
			Visibility = AchievementVisibility.ChapterOpen,
			StageId = StageOfBoss("sands_coil"),
			BossId = "sands_coil",
			Measure = c => c.Lifetime.HasDefeatedBoss("sands_coil") ? 1f : 0f,
			Target = 1,
		},
		// The gate chapter's own deed, which opens the castle (.ai/endgame-castle.md). Fireball again since in-flight 66's
		// reward review (Eric, 2026-11-10): it moved to the Long Coil at schema 19 only because the final gate then counted
		// every spell, and since the gate counts wizards (.ai/achievements-and-secrets.md 2.5) a later chapter may pay a
		// spell - and the hardest chapter on the road, the fire depths, paid only Arcane Energy. Its 300 went to the
		// Sunken Cave's boss. From 2026-11-02 to 2026-11-08 it also paid the Sovereign Rite; the Rite is a Conjunction
		// since, paid by its own Feat (.ai/sovereign-spell.md section 9), and a save that beat the Warden keeps it
		// (SaveData.MigrateRiteToConjunction, schema 26).
		new()
		{
			Id = "emberdeep_ended", DisplayName = "The Long Road Back",
			Description = "Defeat the boss of the Emberdeep.",
			RewardText = "Unlocks Fireball", SpellUnlockId = "fireball",
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
		// A MYSTERY until it is done (Eric, 2026-11-09, .ai/character-fit.md section 5: "do not change the dark wizard,
		// but add his unlock condition as a mystery achievement"). The condition is on the page; the deed's name and
		// what it pays are not - the title "The Taker Taken" says who is taken - so the row reads "???" for both
		// (SealedName and SealedReward, CodexRows.DeedEntry, the way a Conjunction's Feat reads before it is made),
		// with the seal for its glyph, until the last Hardcore boss falls and the name and the reward are revealed.
		new()
		{
			Id = TakerTakenId, DisplayName = "The Taker Taken",
			SealedName = MysteryName, SealedReward = MysteryName,
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
		// The two early chapters (.ai/campaign.md) paid in boons, for the reason the Frozen Waste and
		// the Sands do. Both boons were sold-but-never-sold (the shop registers no boons), so until then
		// no new save could ever be dealt them. Listed only once their chapter is playable. The Circle's
		// pays a spell since in-flight 66 (below); its Shadegrease is Veteran's.
		// The Hayward taught the Ascension until the slow start (2026-11-02); a landmark teaches it now (Teachings).
		BossClearBoon("fields_hurdled", "Round the Hurdle", "fields_hayward", "the Fallow Fields", "thornseed", "Thornseed"),
		// Meteor Swarm since in-flight 66's reward review (Eric, 2026-11-10), swapped with Veteran's Shadegrease: the
		// Circle is the first wall and paid a +6% boon. Meteors suit the heath the Fallen Star came down on.
		BossClear("circle_unclosed", "Out of the Ring", "heath_ninth_stone", "the Broken Circle", "meteor_swarm", "Meteor Swarm"),
		// THE FAR SHORE'S CHAPTER CLEARS (.ai/second-half.md 4.3, marked by F0): each warden's deed pays its god -
		// the one unlock a far chapter carries - and is that god's one source (UnlockCatalog). Not listed until its
		// chapter is playable (IsChapterBuilt), and sorted after the campaign's (CampaignRankOfStage). A line a
		// chapter, each its piece's alone: replace your comment line with your deed; leave the others as they are.
		// F3 (White Downs): the Unstrung Hunter's deed, rekindling Artemis (her FromGod line is F2's).
		SeatClear("stage_14", "The Hunter Brought Down", "downs_unstrung_hunter", "Artemis"),
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
		// (The Sketchbook had no deed from 2026-11-02, and the chapter itself was removed on 2026-11-09.)
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
		// A Road deed of the Circle. It paid 50 Arcane Energy - the smallest reward for one of the hardest early asks -
		// until in-flight 66's reward review (Eric, 2026-11-10: "should be something special, not just energy"): the
		// Quarry, a hunting boon now called Hare's Foot, and its only source (UnlockCatalog).
		new()
		{
			Id = "coursing", DisplayName = "The Coursing",
			RequiredCharacterId = "laelaps",
			Description = $"As Laelaps in the Broken Circle, let your hound Familiar run down {CoursingTarget} Black Hares in one run.",
			RewardText = "Unlocks Hare's Foot", BoonUnlockId = "the_quarry",
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
			Id = "set_collector", DisplayName = "Fully Enchanted",
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
			// The Test Wizard never counted; the schema 28 step took him out of the record when he was removed.
			Measure = c => c.Lifetime.CharactersPlayed.Count,
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
	}).Concat(WizardOpenerRoadDeeds()).Concat(SideEventDeeds()).Concat(SecretDeeds()).Concat(CombinationDeeds()).Concat(EndlessDeeds()).ToList();

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
				// A far chapter's event pays its deed and Arcane Energy, never an unlock (SideEventCurrency).
				CurrencyReward = SideEventCurrency(siteId),
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
			// The Sovereign Rite's Feat is met the first time the strip offers it, as every recipe's is - and the
			// strip waits for its late gate too, so its line names the gate (SovereignRite.MeetsGate).
			string gate = recipe.IsRite
				? $", at level {SovereignRite.MinimumPlayerLevel} with {SovereignRite.MinimumFuel} other spell levels"
				: string.Empty;
			yield return new AchievementDefinition
			{
				Id = id,
				DisplayName = $"Conjunction: {recipe.Name}",
				Description = $"In one run, take {recipe.First.SpellName} down {recipe.First.AscensionName} and {recipe.Second.SpellName} down {recipe.Second.AscensionName}{gate}.",
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
				// Known from one half once the Conjunctions lesson is learned (ConjunctionLadder.IsShown, rung I); the
				// row still reads "Conjunction: ???" until made (CodexRows.DeedEntry). The Rite too, a recipe like any.
				return save != null && ConjunctionLadder.IsShown(ComboCatalog.ForId(deed.ComboId), save);
			case AchievementVisibility.FarSky:
				return save != null && Teachings.HasLearned(save, Teaching.FarSky);
			case AchievementVisibility.Endless:
				return EndlessCatalog.IsOpen(save);
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
		// A wizard's opener deed (in-flight 84): its ask names him, so it is a sealed row while he is shown but not free,
		// and not on the page at all before he is shown (a secret wizard's chapter not open) - one locked thing at a time.
		if (deed != null && deed.IsWizardOpener)
			return !GlobalStatsManager.IsCharacterUnlocked(save, deed.RequiredCharacterId)
				&& GlobalStatsManager.IsCharacterRevealed(save, deed.RequiredCharacterId);
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

	// --- A wizard's opener, earned as that wizard (in-flight 84) -------------------------------------------------------
	//
	// Eric, 2026-11-09: "I also want spells that are another wizard's default spell to only be unlockable once that
	// wizard is unlocked" ... "those achievements should require being completed with that wizard". Every such opener is
	// paid by a deed of its own (IsWizardOpener), done only in a run played as its wizard once he is free. Where the
	// opener came from a chapter's boss, side event or secret, that deed keeps whatever else it pays and asks no wizard -
	// it is the road's and the record's - and the opener's deed is done beside it (AsWizardOf). The exceptions are
	// Eric's: Thorn Vine (the Forest's boss) and Spiritual Weapon (the Ruins' boss). The Familiar was the third (Laelaps
	// needs it) until Ma Liang moved to Ink Ring (in-flight 94, 2026-11-10); nobody opens on it now, and it comes only
	// from the Pinfold.
	// RegressionChecks.ValidateWizardOpeners holds every other wizard's opener to a deed that requires him.

	/// <summary>What an opener deed's row says while its wizard is shown but not free. It never names him.</summary>
	public const string WizardOpenerSealedLine = "A spell kept for a wizard not yet freed";

	/// <summary>
	/// True when the run measured is played as <paramref name="wizardId"/> and the save has him free: the rule every
	/// opener deed's measure reads. Never true off a run - the menu has none - so only the run that does it completes it.
	/// </summary>
	public static bool PlayedAsFreed(AchievementContext context, string wizardId) =>
		context?.Run != null && !string.IsNullOrWhiteSpace(wizardId)
		&& string.Equals(context.Run.CharacterId, wizardId, StringComparison.OrdinalIgnoreCase)
		&& GlobalStatsManager.IsCharacterUnlocked(context.Save, wizardId);

	/// <summary>
	/// Whether a deed done as <paramref name="playedAs"/> counts for the opener deed <paramref name="deed"/> on this save:
	/// played as its wizard, and he is free. The in-run half of <see cref="PlayedAsFreed"/>, for the deeds done beside a
	/// side event or a secret (AchievementManager.Complete, DiscoveryRewards.IsSiteExhausted).
	/// </summary>
	public static bool CountsAsWizard(AchievementDefinition deed, SaveData save, string playedAs) =>
		deed != null && !string.IsNullOrWhiteSpace(deed.RequiredCharacterId)
		&& string.Equals(playedAs, deed.RequiredCharacterId, StringComparison.OrdinalIgnoreCase)
		&& GlobalStatsManager.IsCharacterUnlocked(save, deed.RequiredCharacterId);

	/// <summary>The opener deeds done beside <paramref name="deedId"/> (their <see cref="AchievementDefinition.AsWizardOf"/>).</summary>
	public static IReadOnlyList<AchievementDefinition> DoneAsWizardBeside(string deedId)
	{
		if (string.IsNullOrWhiteSpace(deedId))
			return Array.Empty<AchievementDefinition>();
		besideCache ??= All.Where(d => !string.IsNullOrWhiteSpace(d.AsWizardOf))
			.GroupBy(d => d.AsWizardOf, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
		return besideCache.TryGetValue(deedId, out AchievementDefinition[] deeds) ? deeds : Array.Empty<AchievementDefinition>();
	}

	private static Dictionary<string, AchievementDefinition[]> besideCache;

	// One element's peak in the run measured, and the highest of any.
	private static float RunPeakOf(AchievementContext c, string element) =>
		c.Run?.PeakElementCounts != null && c.Run.PeakElementCounts.TryGetValue(element, out int n) ? n : 0;

	private static float RunPeakOfAny(AchievementContext c) =>
		c.Run?.PeakElementCounts?.Values.DefaultIfEmpty(0).Max() ?? 0;

	// An opener Feat: measured off the run, and only one played as its wizard once he is free.
	private static AchievementDefinition OpenerFeat(string id, string name, string wizardId, string ask, string spellId,
		string spellName, AchievementCategory category, Func<AchievementContext, float> measure, float target) => new()
	{
		Id = id,
		DisplayName = name,
		Description = ask,
		RewardText = $"Unlocks {spellName}",
		SpellUnlockId = spellId,
		RequiredCharacterId = wizardId,
		IsWizardOpener = true,
		SealedLine = WizardOpenerSealedLine,
		Category = category,
		Measure = c => PlayedAsFreed(c, wizardId) ? measure(c) : 0f,
		Target = target,
	};

	// An opener done at a chapter: its boss as him (measured off the run), or its side event or secret as him (done
	// beside that deed, the moment it is, and measured by its own record as a combination's Feat is).
	private static AchievementDefinition OpenerRoad(string id, string name, string wizardId, string stageId, string ask,
		string spellId, string spellName, string asWizardOf = "", string bossId = "")
	{
		bool beside = !string.IsNullOrWhiteSpace(asWizardOf);
		return new AchievementDefinition
		{
			Id = id,
			DisplayName = name,
			Description = ask,
			RewardText = $"Unlocks {spellName}",
			SpellUnlockId = spellId,
			RequiredCharacterId = wizardId,
			IsWizardOpener = true,
			SealedLine = WizardOpenerSealedLine,
			AsWizardOf = asWizardOf ?? string.Empty,
			Category = AchievementCategory.Exploration,
			Kind = AchievementKind.Road,
			Visibility = AchievementVisibility.ChapterOpen,
			StageId = stageId ?? string.Empty,
			CompletesInRun = beside,
			Measure = beside
				? c => c.Save != null && GlobalStatsManager.IsAchievementUnlocked(c.Save, id) ? 1f : 0f
				: c => PlayedAsFreed(c, wizardId) && string.Equals(c.Run.BossId, bossId, StringComparison.OrdinalIgnoreCase) ? 1f : 0f,
			Target = 1,
		};
	}

	// The chapter's half of the openers: a boss, two side events and four secrets, each as its wizard. The secrets'
	// asks NAME the secret, in its catalog name, as well as the chapter (in-flight 84, Eric 2026-11-10: "Find the Buried
	// Runes as Morgan"): an exception to decision 7 (a hidden thing is never named before it is seen), since these deeds
	// stay sealed until their wizard is freed and are about a secret the road has already taught exists.
	private static IEnumerable<AchievementDefinition> WizardOpenerRoadDeeds()
	{
		// Tituba's Riptide: it was the Sunken Cave boss's prize; that deed now pays nothing else and stays the road's.
		yield return OpenerRoad("undertow", "The Undertow", "tituba", StageOfBoss("cave_choir"),
			"As Tituba, defeat the boss of the Sunken Cave.", "riptide", "Riptide", bossId: "cave_choir");
		yield return OpenerRoad("doctors_round", "The Doctor's Round", "paracelsus", StageOfSite("bog_lanterns"),
			"As Paracelsus, snuff the lanterns of the Blighted Swamp and face what tends them.", "contagion", "Contagion",
			asWizardOf: EventDeedId("bog_lanterns"));
		yield return OpenerRoad("grove_sung_green", "The Grove Sung Green", "vainamoinen", StageOfSite("the_grove"),
			"As Väinämöinen, hold the corrupted grove in the Enchanted Forest until it grows back.", "bramble_seed", "Bramble Seed",
			asWizardOf: EventDeedId("the_grove"));
		// Wayland's Iron Palisade at the Cold Forge, the forge that made the shackles, since in-flight 66's reward review (Eric,
		// 2026-11-10) - it was the caravan master's, which suits a chained smith far less. The forge's own deed keeps Molten
		// Shard; this is done beside it, so finding the forge again as him completes it even when the forge was found before
		// (SecretDirector.Found completes the secret's deed on every find, and AchievementManager.Complete the deeds beside).
		yield return OpenerRoad("smiths_toll", "The Smith's Toll", "wayland", StageOfSecret(SecretCatalog.ColdForgeId),
			$"As Wayland the Smith, find {SecretNamed(SecretCatalog.ColdForgeId)} in the Cursed Dungeon.", "iron_palisade", "Iron Palisade",
			asWizardOf: SecretDeedId(SecretCatalog.ColdForgeId));
		yield return OpenerRoad("fays_frost", "The Fay's Frost", "morgan", StageOfSecret(SecretCatalog.BuriedRunesId),
			$"As Morgan le Fay, find {SecretNamed(SecretCatalog.BuriedRunesId)} in the Frozen Waste.", "sleet_runes", "Sleet Runes",
			asWizardOf: SecretDeedId(SecretCatalog.BuriedRunesId));
		yield return OpenerRoad("seers_star", "The Seer's Star", "nostradamus", StageOfSecret(SecretCatalog.FallenStarId),
			$"As Nostradamus, find {SecretNamed(SecretCatalog.FallenStarId)} in the Broken Circle.", "starfall", "Starfall",
			asWizardOf: SecretDeedId(SecretCatalog.FallenStarId));
		yield return OpenerRoad("onmyojis_ward", "The Onmyoji's Ward", "seimei", StageOfSecret(SecretCatalog.BrokenWardId),
			$"As Abe no Seimei, find {SecretNamed(SecretCatalog.BrokenWardId)} in the Mystic Ruins.", "kindled_ward", "Kindled Ward",
			asWizardOf: SecretDeedId(SecretCatalog.BrokenWardId));
	}

	private static string StageOfSite(string siteId) => global::SideEventDirector.ChapterOfSite(siteId)?.Id ?? string.Empty;

	private static string StageOfSecret(string secretId) => SecretCatalog.Get(secretId)?.StageId ?? string.Empty;

	// A secret's catalog name mid-sentence: "The Buried Runes" reads "the Buried Runes" after "find".
	private static string SecretNamed(string secretId)
	{
		string name = SecretCatalog.Get(secretId)?.DisplayName ?? secretId;
		return name.StartsWith("The ", StringComparison.Ordinal) ? "the " + name.Substring(4) : name;
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
	private static AchievementDefinition BossClear(string id, string name, string bossId, string place, string spellId, string spellName,
		string alsoReward = "") => new()
	{
		Id = id,
		DisplayName = name,
		Description = $"Defeat the boss of {place}.",
		RewardText = $"Unlocks {spellName}{(string.IsNullOrEmpty(alsoReward) ? "" : " and " + alsoReward)}",
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
