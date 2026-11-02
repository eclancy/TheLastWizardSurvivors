# The dark wizard, playable

Eric, 2026-11-02: *"the dark wizard should be unlockable if you beat the whole game on hard mode"*.

**Status: proposed for Eric's review, and built on `wip/dark-wizard-playable` (section 5).** Nobody can earn
him yet: the Throne is not playable (`StageCatalog`, `IsPlayable`), so the deed is unlisted and unreachable
until the castle flips. Every number below is a first guess the bot measured once (section 5, "Measured").

Read first: `.ai/endgame-castle.md` 6.4 and 13 (the fight he comes out of), `.ai/spell-roster.md` section 2
(the four axes), `.ai/world-and-tone.md` (the one figure who sheds dark light), `.ai/chapter-wizards.md`
section 1 (the verbs already claimed).

---

## 1. The rule: beat him in the Throne on Hardcore

"The whole game on hard mode" is read as **the dark wizard defeated in the Throne with the Difficulty set
to Hardcore** (`GlobalStatsManager.BalancePresetHardcore`, the save's `BalancePresetId`, recorded on the
run as `RunResult.BalancePresetId`). The Throne is the last chapter and he is its end, and its gate already
asks for the whole road: the Seneschal beaten and every wizard freed (`StageGate.PreviousBossAndWizards`).
So a Hardcore win in the Throne is a Hardcore finish of a campaign that was finished first.

**The stricter reading** - every chapter cleared on Hardcore - is not natural in the code: the save keeps
which bosses fell (`LifetimeStats.BossesDefeated`), never on which difficulty, so it would need a new
lifetime list per preset and every existing save would start it empty. Not built; one line in
`AchievementDefinition` if Eric wants it later (a `HardcoreBossesDefeated` list and a measure over the
campaign).

The deed: **The Taker Taken** - *"Defeat the boss of the Throne on Hardcore."* - a Road deed of the Throne,
measured on the run that ends (`RunResult`: victory, the Throne's boss, Hardcore) and kept once done
(`UnlockedAchievementIds`). It pays him and his spell. No schema step: nothing a save holds is narrowed,
and no new field is written.

**He is outside the campaign gate** (`OutsideCampaignGate`, Laelaps's flag): he is not one of the
imprisoned, and counting him at the Throne would make the Throne a gate on itself.

## 2. Hidden until taken

A locked wizard's name is his reward (`.ai/side-events.md`, "Secret wizards"), and this one is the story's
last surprise, so he is **never shown, not even as a silhouette, until he is free**:

- **Character select:** the sealed card (the wax seal), with a vague line in place of "Not yet
  discovered.": *"Something waits at the end of the hardest road."* A new flag,
  `UnlockDefinition.SealedUntilFreed`, keeps the card sealed after any reveal.
- **Achievements page:** the deed is listed once the Throne is playable; its reward reads "Frees someone"
  under the wax seal, not his silhouette (`CodexRows.DeedEntry`).
- **Quest log:** epithet *"the one on the throne"*; no held line (he is not held anywhere), and once free
  the Throne's note reads *"The Dark Wizard has nothing left to take."*
- **The ending's row** is every wizard he took; he is never in it.

## 3. His kit

### The spell: The Taking (Darkness + Light)

*"Take their light; catch it home."* The boss's three verbs - the taking, his light, all he took - turned
round to face the horde.

1. **A hand of his dark light** flies at the **strongest enemy in reach** (most health; the nearest of
   equals) and turns to follow it. It closes on it: holds it still (roots it - never a boss), drains it
   twice, then tears its light out.
2. **The light drifts home.** A warm mote rises out of the taken body and drifts back toward him,
   gathering speed. It lives five seconds; if he runs from it all that time, it gutters out.
3. **Catch it and his light crosses the field:** two lanes of dark light through where he caught it, one
   along the way the mote came home (back into the crowd it came from), one across it.

| axis | The Taking | nearest in the roster, and the difference |
|---|---|---|
| aim | **the strongest in reach** | Spiritual Weapon's sword (toughest, a blow from above, close); Molten Shard (random) |
| cadence | a timer for the hands, **then the catch** for the lanes | nothing fires on a pickup |
| demand | **meet what you took**: where he stands when the light arrives is where the lanes cross, one of them back through the crowd it came from | Starfall walks into rain; this stands in the light coming back |
| payload | a hold and drain on one, then **a cross of piercing lanes** | Void Lance's one rotating beam; Stormsweep's band at the side |

Two elements, which a freed wizard may open on (`chapter-wizards.md` section 0). No spell carries
Darkness and Light today. The pair is the boss's art: his own dark light, and the warm light he took.

**Milestones** (`SpellEvolutionCatalog.SetupTheTakingEvolutions`), each a thing seen:

| | card | what it does |
|---|---|---|
| 4 | **Crushing Grip** (+Darkness) | the tear throws down what stands round the taken, 60% of a hit; +20% damage |
| 4 | **Long Reach** | hands 40% faster and turning harder; -20% cooldown |
| 4 | **Eightfold Cross** | a caught light throws a star of four lanes, not two |
| 8 | **Great Taking** (+Light) | the hand closes on the strongest and the three nearest it: four lights home a cast; +40% damage |
| 8 | **Many Hands** | three more hands a cast, each at its own quarry; -25% cooldown |

### His passive: Dread

*"Your Darkness hits frighten ordinary enemies twice as often."* Fear is Darkness's status
(`Player.TryFrighten`, 8% a hit, never an elite or a boss): his makes it 16%. The horde is his own; it
knows him. An axis no wizard owns - crowd control as a passive - and it never touches damage.

### His Apotheosis: All He Took (verb **take**)

**Take** is unclaimed (taken: burn, stop, link, hold, flood, swarm, pull, reflect, banish; reserved: rewind,
and the nine planned in `chapter-wizards.md`). It is his verb in the fight and in the fiction.

*"Whatever your lanes kill gives up its light too. Catch it, and the lanes cross again."* Every kill on a
lane looses a mote of its own, at most ten in the air, so a good catch becomes a run of them. Element:
Darkness (+1, the form's rule). It is not "pull" (nothing moves the swarm) nor "swarm" (nothing is
summoned): it takes, and what it takes comes home.

### Health and speed

1.0 health, 0.96 speed: he is carried by his shadow, not quick on his feet.

## 4. Art

`tools/art/player_dark_wizard.py`: the boss's silhouette (`boss_dark_wizard.py`, itself the prologue's)
redrawn on the 48 cell, not scaled: the cowl in its collar of iron points, the raised arm with its sleeve
hanging like a wing, the warm light he took in that hand, the keys at his belt, and **no feet** - the hem
goes into the ground as shadow. His walk moves the hem and the shadow's tendrils, not legs. The darkness
ramp as emission (the eclipse rim, the eyes, the shadow's edge), the light ramp in his hand: on the player
he is still the only figure who sheds dark light. Sheets: idle (the portrait is frame one), moving, hurt
and death (`DarkWizardFrames.tres`). Icon: `the-taking-dark.png` (`spell_icons.py`), the hand closing on a
warm mote; the milestones' icons are `evolution_icons.py`'s, derived.

## 5. Built (2026-11-02, `wip/dark-wizard-playable`)

| piece | where |
|---|---|
| him | `CharacterData_DarkWizard.tres`, roster index **10** (appended); `CharacterVisuals` (untinted; The Taking leaves from the light in his raised hand, cell (11, 5)) |
| his sheet | `tools/art/player_dark_wizard.py` -> `dark-wizard-{1..4,idle,moving,hurt,death}.png`, `DarkWizardFrames.tres`; 1.01 edges/px |
| The Taking | `SpellData_TheTaking.tres` (8 dmg, 2.4 s, 1 hand, 340 reach; a hand more at 3 and 7, lanes 25% bigger at 4 and 6), `scenes/TheTaking.tscn` + `TheTaking.cs` (the hand), `TakenLight.cs` (the light home), `TakingLanes.cs` (the cross); `Player.FireTheTaking`; `ContentValidator.SpellResourcePaths`; `SpellEvolutionCatalog.SetupTheTakingEvolutions`; its `EvolutionStatReaders` row; icon `the-taking-dark.png` and six evolution icons (two hand-drawn: Eightfold Cross, Long Reach) |
| Dread | `fear_x2`: `Player.characterFearScale` doubles `FearChance` in `TryFrighten` |
| All He Took | `ApotheosisCatalog` (verb **take**), `TheTaking.AllHeTookId`, read in `TakingLanes.Strike` |
| the unlock | the `the_taker_taken` deed (`AchievementDefinitions.WonTheThroneOnHardcore`, measured on the run's `RunResult`), `UnlockCatalog` (`OutsideCampaignGate`, `SealedUntilFreed`; The Taking `OutsideGrimoire`) |
| hidden | `CharacterSelection` (sealed card with his line; long names now shrink to one line on the compact card, `FitOneLine`), `CodexRows.DeedEntry` and `JournalPage` (the seal, not his shape), `StageRewards` (his slot sealed with his line, never "Someone is held here") |
| the Throne's card | `StageRewards` lists what a chapter's own Road deed pays when it is neither its warden's, an event's nor a secret's - him and The Taking on the Throne |
| quest log | `QuestLog.Prisoners`: "the one on the throne", no held line, freed line "{0} has nothing left to take." |
| the ending | `ThroneEnding.Wizards` leaves him out |
| the check | `RegressionChecks.ValidateTakerTaken`: the Throne won on Default, lost on Hardcore, another chapter won on Hardcore - none pays him; won on Hardcore - pays both; no gate counts him; the Grimoire does not count his spell; his card and his row stay sealed while he is locked |

**The light comes home faster than drawn in section 3.** The first build let a light drift at a flat 95 px/s
and the balance bot, which kites, caught none and died at 1.5 minutes. A light now starts at 110 px/s and
gathers speed (240 px/s², to 380), so a wizard walking on still meets it within a second or two; only one
who runs from it for its whole five seconds loses it. The demand that is left is where he stands when it
arrives, since the lanes cross there.

**Measured** (seed 1234, the Enchanted Forest, `--all-unlocked --seek-apotheosis`, 15 minutes):

| run | ended | Ascension | form | level at 15 | his spell's share | damage a minute | HP lost a minute |
|---|---|---|---|---|---|---|---|
| him, kiting (light at a flat 95 px/s) | died 0.8 min | - | - | 2 | 79% | 337 | - |
| him, `--immortal` | 15:00 | 6.9 min (Many Hands) | **All He Took at 9.1 min**, level 21 | 32 | 89% | 19,023 | 135.9 |
| Seimei, `--immortal`, same seed | 15:00 | 6.9 min | 9.5 min | 28 | 94% | 27,353 | 50.1 |

Without `--immortal` the kiting bot died inside two minutes on this seed with Seimei too (1.25), so that
column says nothing about him. Immortal, he ascends when Seimei does and takes his form a little earlier,
over the 7.5-minute floor; he deals about 70% of Seimei's damage and loses nearly three times the health
(Seimei has a heal; he has none). **His late frames are heavier**: from his form on, 290-480 ms of game time
a frame at `--timescale` 6 against Seimei's 190-390 - Many Hands and All He Took stand several crosses at
once. The lanes' hot line was thinned to whole 4 px cells after this run; not re-measured.

**Shots** (`_UiShot`, `user://`): `dw-select-locked-{pc,phone}` (`--campaign-complete`, the sealed card and
his line; `--hover-late=CharacterButton11` on the PC), `dw-select-unlocked-{pc,phone}` (`--all-unlocked`),
`dw-taking-pc-{1..5}` and `dw-taking-phone-{1..8}` (`--spell-stage=10 --cast-on-stage --burst`: the hand in
its grip, the light rising, the cross), `dw-death-{pc,phone}` (`--force-death --defeat-hold`).

**Needs eyes:** whether meeting the light is a demand or a chore at full speed; the lanes' width and their
flat violet over a crowd on a phone (they read at 1280x720, thin at 540x1212); the hand at game size (it is
small in its grip); his sheet beside the boss's; the late-run frame cost on the Pixel 9; and every number
in section 3, which the bot has met once.

## 6. Open for Eric

1. **The rule.** Throne-on-Hardcore, or every chapter on Hardcore (section 1)?
2. **Hidden, not a silhouette.** Every other locked wizard shows his shape; he shows a seal. Right for the
   last surprise, or should the Throne's opening reveal his shadow like a secret's chapter does?
3. **His spell in everyone's pool.** Once he is free, The Taking can be offered to any wizard, as every
   found spell is. It is not counted by The Grimoire Restored (`UnlockDefinition.OutsideGrimoire`): it was
   never taken, so it is not recovered, and counting it would put the collection behind Hardcore.
4. **Name.** "The Dark Wizard" is what the boss bar calls him (`BossCatalog`); never a proper name.
5. **Playing him in the Throne** means fighting himself. Nothing stops it; the ending's row leaves him out.
