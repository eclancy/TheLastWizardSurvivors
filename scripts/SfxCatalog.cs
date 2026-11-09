namespace TheOneHeMissed.scripts;

using System;
using System.Collections.Generic;
using System.Linq;

// The names of every file in assets/sfx/, which tools/audio/sfx.py generates from its
// REGISTRY. See .ai/audio-direction.md for the contract and .ai/audio-manifest.md for what
// each one is for.
//
// This is the same allowlist discipline ContentValidator.SpellResourcePaths follows, for the
// same reason: a sound that is not named here is never preloaded and never validated, so a
// missing or misnamed file surfaces at the exact moment it is needed - mid-fight, as silence -
// instead of at startup where RegressionChecks can shout about it.
//
// The twenty-four element sounds are COMPUTED rather than listed. Element is a closed enum of
// twelve and the generator derives its filenames from the same twelve names, so deriving them
// here too means the two lists cannot drift apart; a thirteenth element would be a compile
// error at the enum, not a silent missing file.
public static class SfxCatalog
{
	public const string Directory = "res://assets/sfx/";

	// --- spell shapes no element table can express -------------------------
	public const string SpellExplosion = "spell_explosion";
	public const string SpellBeamStart = "spell_beam_start";
	public const string SpellBeamLoop = "spell_beam_loop";
	public const string SpellBeamEnd = "spell_beam_end";
	public const string SpellOrbit = "spell_orbit";
	public const string SpellSummon = "spell_summon";
	// Elder God Maw breaking up through the earth (BlackTentacles, timed to the breach): the maw's
	// summon cue, in place of SpellSummon. Normal tier - one maw every three to four seconds.
	public const string SpellMawOpen = "spell_maw_open";
	public const string SpellBowDraw = "spell_bow_draw";
	public const string SpellBowRelease = "spell_bow_release";
	// One Starfall star landing (Starfall.Land): a small glass twinkle. It plays for every star, hit
	// or miss, so it is the quietest spell sound there is and is throttled at the call site.
	public const string SpellStarfall = "spell_starfall";
	// One Familiar bite (Familiar.Bite, and each strike of the Griffin's swoop): a short snap of jaws
	// with a low chuff behind it. A pack bites several times a second, so it sits in the Swarm tier and
	// is throttled at the call site; the hit itself is the Arcane impact, as for every spell.
	public const string SpellFamiliarBite = "spell_familiar_bite";
	// The facing family. A Stormsweep discharge (Stormsweep.Tick, every pass of every arc, throttled at
	// 60 ms) and a Sleet Rune leaving the hand (SleetRunes.Release, every rune, throttled at 45 ms). Both
	// replace the element cast cue for their spell rather than adding to it (Player.FireStormsweep).
	public const string SpellStormsweep = "spell_stormsweep";
	public const string SpellSleetRune = "spell_sleet_rune";
	// Sagittarius's loose (Sagittarius.Loose): a bowstring and a glass chord, Light's voice - once a loose,
	// never once an arrow (.ai/combo-spells.md section 3). A loose comes every second or two at most, so it
	// sits in the Normal tier with the bow's own release.
	public const string SpellSagittarius = "spell_sagittarius";

	// --- the player --------------------------------------------------------
	public const string PlayerHurt = "player_hurt";
	public const string PlayerDodge = "player_dodge";
	public const string PlayerHeal = "player_heal";
	public const string PlayerShieldAbsorb = "player_shield_absorb";
	public const string PlayerShieldBreak = "player_shield_break";
	public const string PlayerDeath = "player_death";
	public const string PlayerLowHealth = "player_low_health";

	// --- enemies -----------------------------------------------------------
	// Three hurt variants exist only because this one repeats fast enough to machine-gun.
	// They differ by seed and a few Hz, never by intent, and SfxPlayer picks between them.
	public static readonly string[] EnemyHurtVariants = { "enemy_hurt_a", "enemy_hurt_b", "enemy_hurt_c" };
	public const string EnemyDeathSmall = "enemy_death_small";
	public const string EnemyDeathHeavy = "enemy_death_heavy";
	public const string EnemyShoot = "enemy_shoot";
	public const string EnemyMelee = "enemy_melee";
	public const string EliteSpawn = "elite_spawn";
	public const string BossRoar = "boss_roar";
	// A boss's guard turning a hit (the Hayward's hurdle): wood where a landed hit is the element,
	// so the player HEARS why the number went small (.ai/in-flight.md 57). SfxPlayer.BossBlock.
	public const string BossBlock = "boss_block";
	public const string BossDeath = "boss_death";

	// --- pickups and progression -------------------------------------------
	public const string PickupXp = "pickup_xp";
	public const string PickupHealth = "pickup_health";
	public const string PickupMagnet = "pickup_magnet";
	// The speed potion (BuffItem): a double gust and a climbing shimmer.
	public const string PickupHaste = "pickup_haste";
	// A side event's chest rising out of the ground as a ghost (SideEvent.Succeed).
	public const string EventReward = "event_reward";
	// A wayfinder taken - the Arcane Compass, the Gaoler's map, the bearing cairn (WayfinderPickup):
	// a needle swinging and settling, then an open fifth while the arrows come up.
	public const string BearingFound = "bearing_found";
	public const string LevelUp = "level_up";
	public const string ChestOpen = "chest_open";
	public const string SpellEvolve = "spell_evolve";

	// --- interface ---------------------------------------------------------
	public const string CardAppear = "card_appear";
	public const string CardSelect = "card_select";
	public const string Reroll = "reroll";
	public const string UiHover = "ui_hover";
	public const string UiClick = "ui_click";
	public const string UiBack = "ui_back";
	public const string UiDenied = "ui_denied";
	public const string UiOpen = "ui_open";
	public const string UiClose = "ui_close";
	public const string UiPause = "ui_pause";

	// --- run and meta stingers ---------------------------------------------
	public const string StageClear = "stage_clear";
	public const string GameOver = "game_over";
	public const string MetaUnlock = "meta_unlock";
	public const string WaveWarning = "wave_warning";

	// --- the defeat and its payout (2026-11-02) ---------------------------
	// Eric: "Defeat should have a dramatic animation ... It then tallys up the reward with a
	// satisfying payout sound." PlayerFall: the wizard reaching the ground (DefeatSequence).
	// TallyTick: one step of the Arcane Energy count (GameOverScreen, SfxPlayer.TallyTick pitches it
	// up the count). TallyPayout: the count landing on its figure.
	public const string PlayerFall = "player_fall";
	public const string TallyTick = "tally_tick";
	public const string TallyPayout = "tally_payout";

	// --- chapter hints -----------------------------------------------------
	// One per chapter that still holds a discovery, played from its direction a few times a run
	// (StageHintCatalog, StageHintDirector). Far away by construction: lowpassed, in a room.
	public const string HintGroveWhisper = "hint_grove_whisper";
	public const string HintCellCry = "hint_cell_cry";
	public const string HintLanternFlare = "hint_lantern_flare";
	public const string HintIceSong = "hint_ice_song";
	public const string HintCaravanBell = "hint_caravan_bell";
	public const string HintPaperWind = "hint_paper_wind";
	public const string HintAnvil = "hint_anvil";
	public const string HintSibylVoice = "hint_sibyl_voice";
	// The walking hut in the Enchanted Forest (WalkingHutEvent): two heavy falls of a great foot and its timbers creaking.
	public const string HintHutStep = "hint_hut_step";
	public const string HintHullKnock = "hint_hull_knock";
	// The gibbet's cage in the Blighted Swamp (GibbetCageEvent): a glass flask tapped twice, and a man coughing.
	public const string HintFlaskChime = "hint_flask_chime";
	// The Court's seer, heard reading his verse from a room (SeersVoiceEvent): the chapter hint, and his readings in the seek.
	public const string HintQuatrain = "hint_quatrain";
	// The dry tomb on the Scorched Sands (TombBeaconsEvent): slow knocks of stone on stone under hissing sand.
	public const string HintTombKnock = "hint_tomb_knock";
	// The Tithe-Reeve's granary in the Fallow Fields (TitheScaleEvent): a woman's voice, level and unhurried, through
	// plank walls - she speaks, and does not plead.
	public const string HintGranaryVoice = "hint_granary_voice";
	// The Court's painter (DottedDoorEvent): three strokes of a wet brush along a wall and a drop running off the last -
	// the chapter's second hint, and Paint What Is Needed drawing his flask (PaintedFlask).
	public const string HintBrush = "hint_brush";
	// The nine mouths' call in the Sunken Cave (NineMouthsEvent): her voice out of the true mouth, first, and
	// the cave's echo of it out of the other eight, a beat apart - played at each mouth, so the 2D listener
	// pans and fades each from where it comes.
	public const string SibylVoice = "sibyl_voice";
	public const string SibylEcho = "sibyl_echo";
	// The gibbet's escort (GibbetCageEvent, .ai/unlock-puzzles.md 3.4): Paracelsus stepping into the rot on his way to the
	// ferry - the bog's wet gulp and his cough. A mistake the player made; throttled, a few a walk at most.
	public const string EscortCough = "escort_cough";
	// The thaw's kantele (ThawEvent, Kantele; .ai/unlock-puzzles.md 3.6): its five strings of ice, low to high - each
	// played where the string lies, as the ice sings a phrase and as he steps on them in turn - and the choked one a
	// string gives when it is stepped on out of turn. Throttled by the song itself: a note every 0.6 s at most.
	public const string IceString1 = "ice_string_1";
	public const string IceString2 = "ice_string_2";
	public const string IceString3 = "ice_string_3";
	public const string IceString4 = "ice_string_4";
	public const string IceString5 = "ice_string_5";
	public const string IceStringWrong = "ice_string_wrong";
	// The dry tomb's mirrors (TombBeaconsEvent, SunMirrors; .ai/unlock-puzzles.md 3.1, "route the sun"). MirrorTurn: a
	// bronze disc swinging round on its spindle in a stone socket, as his feet turn it - played at the mirror, at most every
	// 0.14 s however fast he walks round it. SunArrive: the sunlight landing on a mirror it had not reached, and on the
	// sun-disc over the door - played where it lands, at most every 0.2 s.
	public const string MirrorTurn = "mirror_turn";
	public const string SunArrive = "sun_arrive";
	// The Gaoler's key (GaolersKeyEvent, .ai/unlock-puzzles.md 3.8): a lantern-bearer's light falls on him with the key - its
	// shutter thrown wide and the cold flame flaring, and the key ringing on the flagstones. A mistake the player made, once a
	// catch; the light then holds on him, so it cannot repeat until he has stepped off the key and taken it up again.
	public const string LanternCatch = "lantern_catch";
	// THE WHITE DOWNS (F3, .ai/second-half.md 4.2; .ai/audio-manifest.md, the far shore's rows).
	// HintHuntHorn: the hunt's horn far off on the downs (SilverHindEvent's hint, and the Unstrung Hunter calling his
	// hounds - one horn, one hunt). HunterDraw / HunterLoose: the Unstrung Hunter's great bow drawn and loosed
	// (UnstrungHunterBoss) - heavier and colder than the player's own bow, every few seconds in his fight. HindBolt: the
	// Silver Hind seeing him move, a snort and her hooves on the turf; a mistake the player made, once a bolt.
	public const string HintHuntHorn = "hint_hunt_horn";
	public const string HunterDraw = "hunter_draw";
	public const string HunterLoose = "hunter_loose";
	public const string HindBolt = "hind_bolt";

	// --- secrets (.ai/stage-secrets.md 7.7, .ai/achievements-and-secrets.md 4.9 and 8.4.1) ------
	// Generated and allowlisted, not yet played: the secret agents wire them as they build the sites.
	// The warm glint as sound - breathed, never struck - so a secret never sounds like a reward bell.
	// SecretSeen: the first time a secret's near tell is on screen (the seen mark), played AT the site.
	public const string SecretSeen = "secret_seen";
	// SecretFound: SecretDirector.Found, in place of EventReward - which goes back to meaning a side
	// event's chest. MetaUnlock stays for a find that changes the save.
	public const string SecretFound = "secret_found";
	// SecretShatter: the Sunken Cave's Geode Heart breaking, once a run at most. Critical below.
	public const string SecretShatter = "secret_shatter";
	// SecretVoiceBelow: the Cursed Dungeon's Oubliette answering (Oubliette.cs) - a woman's voice up a long shaft,
	// wordless, played AT the grate once its five untouched seconds are in, and only while someone is still held
	// below. Once a run at most. Never names her.
	public const string SecretVoiceBelow = "secret_voice_below";

	private static readonly string[] NonElementNames =
	{
		SpellExplosion, SpellBeamStart, SpellBeamLoop, SpellBeamEnd,
		SpellOrbit, SpellSummon, SpellMawOpen, SpellBowDraw, SpellBowRelease, SpellStarfall, SpellStormsweep, SpellSleetRune,
		SpellFamiliarBite, SpellSagittarius,
		PlayerHurt, PlayerDodge, PlayerHeal, PlayerShieldAbsorb,
		PlayerShieldBreak, PlayerDeath, PlayerLowHealth,
		EnemyDeathSmall, EnemyDeathHeavy, EnemyShoot, EnemyMelee,
		EliteSpawn, BossRoar, BossBlock, BossDeath,
		PickupXp, PickupHealth, PickupMagnet, PickupHaste, EventReward, BearingFound, LevelUp, ChestOpen, SpellEvolve,
		CardAppear, CardSelect, Reroll,
		UiHover, UiClick, UiBack, UiDenied, UiOpen, UiClose, UiPause,
		StageClear, GameOver, MetaUnlock, WaveWarning,
		PlayerFall, TallyTick, TallyPayout,
		HintGroveWhisper, HintCellCry, HintLanternFlare, HintIceSong, HintCaravanBell, HintPaperWind, HintAnvil, HintSibylVoice, HintHutStep, HintHullKnock, HintFlaskChime, HintQuatrain, HintTombKnock, HintGranaryVoice, HintBrush,
		SibylVoice, SibylEcho, EscortCough,
		IceString1, IceString2, IceString3, IceString4, IceString5, IceStringWrong, LanternCatch,
		MirrorTurn, SunArrive,
		HintHuntHorn, HunterDraw, HunterLoose, HindBolt,
		SecretSeen, SecretFound, SecretShatter, SecretVoiceBelow,
	};

	// The generator lowercases the enum name, so Element.Darkness is cast_darkness.wav.
	public static string CastFor(Element element) => "cast_" + element.ToString().ToLowerInvariant();

	public static string ImpactFor(Element element) => "impact_" + element.ToString().ToLowerInvariant();

	private static string[] allNames;

	// Every sound the game expects to exist: the 64 named above, the three hurt variants, and
	// the twelve casts and twelve impacts derived from the element enum. tools/audio/build.py reports
	// writing 112 (counted 2026-11-08, with the dry tomb's mirror_turn and sun_arrive).
	public static string[] AllNames
	{
		get
		{
			if (allNames == null)
			{
				var names = new List<string>(NonElementNames);
				names.AddRange(EnemyHurtVariants);
				foreach (Element element in Enum.GetValues<Element>())
				{
					names.Add(CastFor(element));
					names.Add(ImpactFor(element));
				}
				allNames = names.Distinct().ToArray();
			}
			return allNames;
		}
	}

	public static string PathFor(string name) => Directory + name + ".wav";

	/// <summary>
	/// How a sound behaves when the mix is crowded. Two different jobs hang off this and they
	/// are easy to confuse: PRIORITY decides who wins when voices run out, and applies to
	/// everything; DUCK AUTHORITY decides who makes other sounds quieter, and belongs to almost
	/// nothing. tools/audio/mixsim.py measured a tiered policy as worth 5 to 8 dB of headroom on
	/// the sounds that matter, and cut late-game voice stealing from 215 to 35.
	/// </summary>
	public enum SfxTier
	{
		/// <summary>Fires many times a second. Dropped before anything else is disturbed.</summary>
		Swarm,
		/// <summary>Ordinary events. Yields to Critical, never to Swarm.</summary>
		Normal,
		/// <summary>Rare and run-defining. Never stolen, ignores distance, ducks everything else.</summary>
		Critical,
	}

	// Critical is deliberately tiny - thirteen of ninety. player_hurt is loud and important and is
	// NOT here: while the player is being swarmed it fires several times a second, and a duck
	// that retriggers before it releases turns the mix into a pump and removes hit feedback at
	// the exact moment the player is dying. The Geode Heart's shatter is: once a run at most, a
	// spell vault opening, and it lands in the thick of the fight the crystal asked for, where a
	// Normal sound can lose its voice to the swarm. The fall and the payout are (2026-11-02): once a
	// run each, with nothing else playing, and each lands inside the duck of the Critical sound
	// before it - the fall inside player_death's, the payout inside game_over's - which would take
	// 6 dB off the two beats the defeat is built round. The tick stays Normal: it is a ladder.
	private static readonly string[] CriticalNames =
	{
		BossRoar, BossDeath, PlayerDeath, EliteSpawn,
		LevelUp, SpellEvolve, StageClear, MetaUnlock, GameOver, WaveWarning,
		SecretShatter, PlayerFall, TallyPayout,
	};

	// Everything that can fire many times a second: the hurt variants, every impact, the XP orb,
	// the orbit tick, a Starfall landing and a Sleet Rune cast. These are what a policy has to
	// manage; the rest looks after itself.
	private static readonly string[] SwarmNames =
	{
		PickupXp, SpellOrbit, SpellStarfall, SpellSleetRune,
		SpellFamiliarBite,
	};

	public static SfxTier TierOf(string name)
	{
		foreach (string critical in CriticalNames)
		{
			if (critical == name)
				return SfxTier.Critical;
		}
		foreach (string swarm in SwarmNames)
		{
			if (swarm == name)
				return SfxTier.Swarm;
		}
		foreach (string variant in EnemyHurtVariants)
		{
			if (variant == name)
				return SfxTier.Swarm;
		}
		// Impacts are derived rather than listed for the same reason the filenames are: there is
		// one per element and the enum is the authority on how many elements there are.
		return name.StartsWith("impact_") ? SfxTier.Swarm : SfxTier.Normal;
	}

	/// <summary>
	/// Which element voice a spell speaks with. A spell can carry two element tags; the heavier
	/// one wins, and an exact tie falls to the earlier enum member so the choice is stable
	/// between runs rather than depending on dictionary order.
	/// </summary>
	public static Element DominantElement(SpellData spell)
	{
		Element best = Element.Arcane;
		int bestWeight = int.MinValue;
		if (spell == null)
			return best;
		foreach (var pair in spell.GetElementWeights())
		{
			if (pair.Value > bestWeight || (pair.Value == bestWeight && pair.Key < best))
			{
				best = pair.Key;
				bestWeight = pair.Value;
			}
		}
		return bestWeight == int.MinValue ? Element.Arcane : best;
	}
}
