# Work in flight (snapshot, 2026-11-01, updated after the 0.10.0 release)

A handoff from the cloud session that ran 2026-10-30 to 11-01. Eric moved the work to a local Claude
Code session. **A snapshot, not a source of truth:** check `git log` and the branches before acting
on it, and delete this file once everything below has landed or been dropped.

## 1. Released

**0.10.0 is released** (`Release v0.10.0`, 6b3af6e, tag `v0.10.0`, 2026-11-01, from the Windows PC
by the local session at Eric's request; dry run first; ecec.dev's `version.json` serves
it). Releases need this PC: Inno Setup, the Android release keystore and the Android SDK live here
(`.ai/releasing.md`).

## 2. Where the branches stand

- **`main` and `main-ff86ni` were level at the release** (6b3af6e). `main-ff86ni` stays the
  development branch; every merge into it is built, validated and pushed. The validator now ends
  **0 errors, 4 warnings**: the Test Wizard and the three castle chapters' campaign to-do lines.

In 0.10.0, on top of the 0.9.2 work the local session added (2026-11-01): **C6, both castle casts**
(`tools/art/cast_bridge.py`, `cast_court.py`; their bolts arcane, `.ai/bestiary.md` 6), **the pixel
pass on Sleet Runes and Aegis Ward** (`.ai/art-replacement-manifest.md` 9; the shield ring is opaque
now), and **`BossRushCatalog.Roster` takes playable chapters only**, so a castle boss can merge before
C2 without joining the rush.

Merged into `main-ff86ni` before that:

- **Difficulty curve, schema 20** (`.ai/difficulty-curve.md`): `ChapterDifficulty` per-chapter
  enemy health and damage; boss health 3,800 (Elderbark) up to 200,000; max HP 50, no base regen,
  every heal cut and capped, chest heals gone, no number on the health bar; the Ascension sealed
  until the Hayward is beaten (saves with a run keep it); rerolls a per-run pool (Fate Fracture +2
  a level, cap 10), Skip pays half an XP bar; Fate Fracture and Emberheart re-priced and refunded.
  **Six spell slots kept** (Eric reverted the four-slot idea).
- **Ascension audit** (`.ai/spell-roster.md` 2i): Tidal Wall starts small, swells, fires a third as
  often and can never be wider than the screen; Trident tamed; Sun Dogs above and below on a phone;
  Arcane Orb's Threaded Needle retargets; **Mjolnir replaces Titan's Maul** (trades Light for
  Lightning, calls lightning on each slam); Doomcap drawn from above.
- **Spell caps:** a Shadow Bolt or Chain Lightning cast makes at most 20 arcs and never strikes an
  enemy twice (Cascade 37). Starfall rains 1.6x wider and its skips scatter up to 80 degrees.
- **Whips** (`WhipStroke.cs`): Thorned Vine and Stormsweep crack like whips; the vine wraps and
  cracks on the enemy it catches; new icons.
- **Prologue** (`.ai/prologue.md`): six generated panels before the first Start Run, replay from
  Options, `SaveData.HasSeenPrologue` (no schema bump).
- **Level-up screens** (`.ai/level-up-screen.md` section 7): one background per screen (no page on
  page), no scrolling (130-character cap, validator warns), `FocusSpotlight` for keys and pad, a
  `Tagline` on every spell for its new-spell card, ban as a tear that spends the level-up with no XP.
- **Familiars:** hounds (only the first ascends, pups, Pancho under Annwn, a 96-pixel Cerberus);
  the cat never attacks (pounce weakens and slows, returns to buff; Purr 3 HP every 20 s with a
  heart; Nine Lives at level 4; **Black Cat** Ascension casts a random spell of yours every 3 s);
  the crow circles and dives; the dog heels.
- **Stormsweep, Sleet Runes, Aegis Ward** phase 2 evolutions (Ball Lightning, Forked Lightning,
  Great Rune, Braided Wind, Parting Wind, Rampart, Reforged).
- **World map reveal:** a chapter appears only once the boss before it falls; the Sketchbook has
  its own page off the road. Pause screen shows each spell's evolutions; achievements count up live
  with a kind-first banner; no white flash after a pick.
- Reward lines read kind first ("Spell recovered - Bramble Seed"); clipped bush and Sands art fixed.

## 3. Branches in flight

Merged since the release (2026-11-01): the shield ring recoloured (Eric: Metal ring, Light motes;
elite markers read gold), **the Seneschal** (440,000; flees at 75/50/25% and is followed; a door
"hop" when unreachable for 12 s, beyond 5.6) and **the Constable** (400,000; holds the gate from the
wall). Both bosses' chapters now miss only their world-map stop. Then **the Throne** (C4, 66235ac):
the hall (`throne_room.py`, `throne_walls.py`), the Muster (hook 8 `LevelUpOpening`, `GrantLevelUps`;
**18 levels**, moved by measurement), the procession (`ThroneProcession`, `SendBoss`; the guard stands
down while a pair fights), its own roster (`StageBestiary.Throne.cs`). Its line misses only his boss and
the map. Eyes: the pillars read squat, the south wall is a void, the doors are not drawn, the throne
reads small, a pair overlaps on the player; a Throne defeat shows "Boss at 0:01" on the results.

Three agents in flight: **`wip/boss-dark-wizard`** (C5, him and the ending, `ws-wip-boss-dark-wizard`,
from 66235ac); **`wip/dungeon-move`** (C2, schema 21, `ws-wip-dungeon-move`, from 412df99; held, to land with
the Bridge's flip and C8); **`wip/castle-preflip`** (`ws-wip-castle-preflip`, from 412df99: the Constable
off the top of the PC screen, the bridge's XP far above the Emberdeep's, `BridgeRules`' unseeded spawns,
a Court wall cell the player stands in). None flips `IsPlayable` or touches the world map. Expect
conflicts in `BossCatalog.cs`, `RegressionChecks.ValidateBossPattern`
and `.ai/enemy-behaviour.md`'s boss tables (keep both sides), and in the generated `tiles.json`,
`detail.json` and atlas (take either side, re-run `tiles.py` and `ground_detail.py`).

At the first handoff there were none: `wip/boss-entrances` was merged last (every boss makes a themed 2.5-4 s arrival with the field
held and the player's spells silent; every boss has at least two phases; fights run about 2-6 minutes
on the harness, against 0.3-1.1 before on the same code; Boss Rush at 35% health). The final health
values for the Fields, Circle, Archivist, Still Warden and Warden of the Deep were set from single runs
and not re-measured; see `.ai/enemy-behaviour.md`, "Boss entrances and longer fights". Needs eyes: every
arrival in real play, whether a 3-4 s hold feels like a beat or a wait, the Choir's spacing, Mother Rot's
healing, the Still Warden's ice ring.

Also merged since the first version of this note: **`wip/evolutions2-blasts`** (Fireball's Brimstone and
Armageddon Sun, Arcane Explosion's Sympathetic Blast, Resonance and an Event Horizon that stops time,
Meteor Swarm's Breakup, Skipstone, Extinction Event and Creeping Barrage; `.ai/spell-roster.md` 2h) and
**`wip/quests-notes`** (chapter notes, the journal as a pause tab and a world-map button, the quest log
with three tracked quests in the lower-left HUD corner, the run-start flourish; `.ai/quests-and-notes.md`).
Every other `wip/*` branch is fully merged and can be deleted on GitHub.

## 3a. The 0.10.0 playtest fixes (Eric, 2026-11-01) - before any castle work

The castle agents (`wip/boss-dark-wizard`, `wip/dungeon-move`, `wip/castle-preflip`) were cut off by the
usage limit mid-work; their worktrees keep what they did. They resume after these. Done: bosses excluded
from executes (e6f7dfd), Blank Folio offer weight 0.02 (63be6da), a short level-up hand keeps a
three-card hand's card size (dd5cc6a), no two Black Tentacles rifts ever overlap, of any face (fc28f95), the chapter note's paper always
draws (0175bab: the Fallow Fields' was missing), Black Tentacles starts at 60 px and two tentacles and
builds to 120 and seven.

1. **Boss cinematics** (merged, 83ef1d2; open: props under the lifted bodies, a reduce-motion option): no movement while a boss arrives; letterbox
   bars, zoom, screen shake, a flare; shake in boss animations where it helps; **the player and bosses
   always draw above every spell and effect**.
2. **Spell visuals** (merged, 77394d4; Stormsweep's level 1 hits land up to 0.22 s later with the slower lash; the familiar icon options sheet is with Eric): Cone of Cold hard to see; Solar Flare rises from
   under the player and the enemies; Stormsweep's crack slower at first, quicker with level; Scorching
   Ray's icon a thinner ray into the ground; Iron Palisade's rolling walls dig up dirt; Bramble Seed withers quickly at its end instead of shrinking; its vines taper smoothly from the woody base to
   the green (no abrupt step); a better Mirefoot icon (a stray black line at its foot); **familiar icons
   too hard to read**: Eric picked option B, Moonlit (`wip/familiar-icons`, in flight).
3. **Enemy pathing** (merged, a70cb11: `Enemy.Skirt`, prop clusters in the nav grid; bodies stuck 6 s+ fell from 24 to 3 across ten runs; open: crowds queuing in Cave and Dungeon passages are crowd spacing, Eric's call): every enemy sticks on terrain; an efficient fix.
4. **Boss crowd control, and the Hollow Choir** (merged: bosses slow to 0.5 at most, a freeze, root,
   time stop or stagger holds one 0.75 s and then not again for 2.5x as long, knockback and pulls add to
   its walk; the Choir routes round rock, sings a crescent at you, dips when 560 px off, and revives in
   7 s of visible song that freezing pauses. Open for Eric: are those numbers right?). Bosses are
   immune to any crowd control that would stop them ever reaching the player (freeze and root, pulls,
   knockback, time-stop, walls): they always keep a way to put the player in danger. The Choir's
   freeze-pauses-revive below must still work. Then the Choir: pathing sticks; their ranged attack needs a launched attack, not a
   circle and arrow; one too far from the player dips into the ground and reappears; a longer revive,
   with the voices visibly walking to a fallen one to raise it, so the player sees to stop it; freezing
   and other crowd control pause the revive.
5. **The prologue** (merged: the taken wizards cut from real sprites, the one he missed animated with a
   hound sniffing past him, a TEMPORARY "Replay intro" button bottom-left on the world map, Laelaps in hat,
   mantle and star collar; open for Eric: show Tituba, Väinämöinen and Laelaps in "He took them all"
   though secret? the hider is the title wizard, not the chosen one; keep the mantle?): wizard silhouettes look goofy - use the in-game characters' silhouettes; "You are
   the one he missed" animates creatures searching for the wizard while he hides cleverly; a button on
   the world map to replay the intro, **for testing, to be removed later**. And **Laelaps wears the
   wizard outfit** (the familiar hound's hat, mantle and star collar, `tools/art/laelaps.py`).
6. **Wizard clues and the ice lake** (merged: a cue in each site's language from its direction, the line
   fading in on a dithered capsule clear of the quest tracker, Vainamoinen in a block at the centre of a
   640 px living lake, clue lines in italics under each chapter note's intro (the intro now upright), and
   **`Announcements`: one message on screen at a time**, the boss banner never waiting; First Blood in
   the opening now waits about 11 s, one constant. Open for Eric: keep the vague vault clue?): every wizard's hint becomes a cue in the world from its direction
   (the singer: musical notes drifting in from his side, and his singing heard), its line fading in on
   screen as a hint rather than in a speech bubble; all wizards' clues this way. The wizard trapped in
   ice sits in the centre of a large, magically animated lake (the current art reads corny).
7. **Music** (merged: Eric listened and said "Merge it", 2026-11-01): drums and bass from bar one in each chapter's own rhythm, the
   melody back at pace minute 5, a boss layer per chapter, a 1 s menu handover; +2.6 MB per release zip.
   Open: the melody leaves at 0:24 until minute 5; no melody under the boss layer; the castle's stems
   left for their flip. The menu's music should give way to more interesting, thematic battle music for each level.
   And **the Elder God Maw opens with a deep, scary crash** (a new generated sound, by
   `.ai/audio-direction.md`).
8. **Ground per chapter** (merged: the Emberdeep on cracked basalt with smouldering seams and molten
   lava, the Frozen Waste on packed snow over crazed ice; the rest passed the audit. Open for Eric: the
   Ruins' plain slate floor as broken paving? the lava too loud over 15 min? the Frozen Waste now the
   lightest floor): every chapter's ground should read as its place; "Emberdeep doesn't look like
   cracked volcanic earth." A ground pass through `tools/art/tiles.py` and the chapters' terrain
   modules (`.ai/terrain.md` first), the Emberdeep first.
12. **Done** (Eric, 2026-11-01; eyes merged: round 4 px pairs placed by `tools/art/title_eyes.py`, each
   blinking on its own clock by frames, the 24 whole-screen eye layers removed): the title screen's eyes in the dark "don't always look like eyes. they
   should all be circular and blink, and be randomly spread out rather than some being on top of each
   other" (`.ai/title-screen.md`, "The eyes in the dark blink" was already on its not-done list); and
   **announcements must never stack** (the Forest's opening under First Blood) - with the clues agent.
11. **Done:** the Test Wizard is off character selection (69986af); the Sketchbook's boss is **Elim**
   (b3695cf). **Waiting on Eric:** Elim's "it's wizarding time" drawing, to show when he arrives - it is
   not in the repo or this PC's Downloads or Pictures.
9. **Make the Apotheosis visible** (merged: an "Apotheosis" section atop the Ascensions browser with the
   starting spell's checklist and the form, "Apotheosis: <form>" on character select, the form after the
   Ascension on the pause screen; its name always shows, its line stays sealed until first offered) ("make it clearer that apotheosis is an option, showing in
   'ascensions', etc"). Today the Ascensions browser never mentions it (`LevelUpMenu.Ascensions.cs` does not
   read `ApotheosisCatalog`); a player meets it only when the strip offers it. Show the wizard's own
   Apotheosis in the browser with its four requirements and their progress, and name it where the
   wizard is chosen and where the starting spell's evolutions are shown.
10. **Design, in flight** (`wip/design-wizards-combos`, docs only): a wizard to free in every chapter
   (`.ai/chapter-wizards.md`), and combination spells beginning with Sagittarius = Hunter's Draw +
   Starfall/Constellation (`.ai/combo-spells.md`). **Both approved by Eric, 2026-11-01** (only the starters
   a new save holds must open on one element; Ma Liang for the Sketchbook); nothing built yet.
   **Decided:** a combination needs both parents fully ascended, each through one specific named
   Ascension (revision of the doc in flight).

## 3b. After the playtest list (2026-11-02)

- **Combination spells and Sagittarius** merged (`ComboCatalog`, the Conjunction offer in the strip, fusion
  freeing a slot, the derived "Conjunction: Sagittarius" Feat, a Conjunctions section in the browser). Open
  for Eric: should combinations count toward "The Grimoire Restored" (now 36)? which parents' level 4s carry
  over (kept Heavy Draw, Barbed, Shooting Stars, Skipping Stars; dropped Running Shot)? does a held full
  draw fire by itself? the bronze plate? **The recipe was never offered inside 15 minutes** in four bot runs:
  the parents' upgrade cards come too rarely, not the gates. Damage against the 1.2x target unmeasured.
- **Bridge and Court pre-flip fixes** merged: hook 9 `CameraLean` frames the Constable on a PC, bridge runs
  reproduce from the run's seed, the Court's walls and grid agree (the report read the body, not the
  feet), his bolt files show the reticle. Open for Eric: the bridge ends about 3 levels above the
  Emberdeep (pickup, not spawns); +3 fine, or fold overtaken orbs?
- **No Godot logo while loading** (`boot_splash/show_image=false`, black).
- **The Evolution Codex** proposed and wireframed from Amy's emailed feedback (`.ai/evolution-codex.md`,
  https://claude.ai/artifact/NEHkx6sJQXjwqYrdMtuie9); six questions in its section 9.
- **Merged:** Abe no Seimei (Mystic Ruins, the pilot chapter wizard, P1's rules); **story resets**
  (`SaveData.CurrentProgressEpoch`, still 1: raising it on a release copies each save to
  `savegame.before-epoch-<n>.json`, keeps only `SettingsKeys`, shows a one-time notice; the Windows updater
  asks first and the Android strip warns; `.ai/decisions/2026-11-02-story-resets.md`, `.ai/releasing.md`).
  Open for Eric: keep all Options settings on a reset? make Options' Reset Progress back up too? the first
  reset must come one normal release after this ships, since 0.10.0 copies don't know to ask. **CLAUDE.md
  should gain a line** on the reset as the one sanctioned way to take progress away (the integrator left
  CLAUDE.md for Eric). The ecec.dev download page needs the same warning, in its own repo.
- **Approved and queued:** the Evolution Codex (building, `wip/evolution-codex`); the Sovereign Rite, a
  Conjunction you point at a spell (`.ai/sovereign-spell.md`, to build); Seimei's coupled puzzle and the
  Ruins vault on open ground (`wip/seimei-puzzle`, building); a batch of new items (designing,
  `wip/design-items`).
- **Later on 2026-11-02:** merged - the sweep answers (3c), Seimei's coupled seal and vaults on open
  ground, the Ruins on broken paving (`ruinstone`), "Free the wizard", Toxic Spore Burst from one
  mushroom. **Building:** the menu styling pass (`wip/menu-pass`, Options first, the First Session
  Checklist removed; told to leave GameOverScreen alone), the Sovereign Rite (`wip/sovereign-rite`), the
  Evolution Codex's logic (`wip/evolution-codex`; its LAYOUT waits on Eric choosing A, B or C on the
  canvas), the slow-start plan (`wip/design-slow-start`, design only: fewer spell slots on a new save,
  pages in the shop, systems unlocked one at a time). **Queued, in this order as slots free:** the
  defeat sequence and end-of-run screen (below); the first item wave plus rerolls and bans on the chest
  screen (`.ai/new-items.md`, after the menu pass, which also touches the chest screen).
- **Update, end of 2026-11-02:** merged the Evolution Codex (layout A; Eric still to pick A, B or C;
  on the phone the detail sheet stops short of the right edge, to fix in the layout pass) and the
  menu pass (`MainMenu.Options.cs`, `SettingsRows.cs`, new switch/knob art from `ui_controls.py`).
  **Building:** the Sovereign Rite, the defeat sequence (`wip/defeat-sequence`), the first item wave
  with chest rerolls and bans (`wip/items-wave-1`). **Queued next:** the slow start
  (`.ai/slow-start.md`, answered: a design pass first on which achievement teaches which system and
  the post-run cutscenes, then the build; the epoch goes to 2 in the next release commit).
  **Open for Eric** from the menu pass: drop Testing from shipped builds? the Gameplay Tips switch?
  the "Momentum streak" line? dead FullSetDetailScreen/ChestItemHUD and the Test Wizard's spell
  picker? From the Codex: keep "Evolution Codex" on the menu and "★ Codex" in runs? the Familiar's
  four level 4 paths scroll on PC? relics claimed in earlier runs count as seen?
- **Merged, night of 2026-11-02:** the Sovereign Rite (tuned to x1.39 against the 1.3 target at
  0.5% damage a fuel, gate level 28, offered at minutes 10.4-12.2; `.ai/sovereign-spell.md` 8), the
  defeat sequence and folded results (`DefeatSequence.cs`, `Node2DGame.Defeat.cs`; three generated
  sounds nobody has listened to yet), Report a bug moved into Options, Testing hidden in release
  builds. **Merged since:** the Codex as layout **B, the book** (index grouped in `Element` enum order - there is no element wheel) (Eric: "build the one you think makes the
  most sense and is easiest to read on phone", revisit when votes come in; it must also stop showing
  "L8" on the Rite's recipe), the first item wave. **Votes:** Eric wants them posted through the bug intake, so the vote page is
  being built on ecec.dev (branch `spell-index-vote` in the website repo, worktree `../ecec-spell-vote`;
  the claude.ai draft is https://claude.ai/artifact/B4pnZKUXgtnzCYp1BBJFjE).
- **Released 0.11.0 (night of 2026-11-02, Eric: "once all current work is done, commit push
  release. I will review tomorrow").** It carries everything above plus: the item wave's second pass
  (element tags as chips, chest pages coloured by rarity, the Gimmel Ring simplified to one sentence a
  card, clickable status keywords with a pad note; the "debuffs never add up" line removed), the
  Apotheosis gate Eric asked for (the wizard's own spell at max level, ascended down any branch, never
  combined; **player level 21, down from 23** by the run-pacing rule, so forms come ~1.5 min earlier -
  one line back to 23 if he prefers), the PC title with no dark pockets under the ward and the phone
  title's prompt pocket held to shade. **No story reset in it** (epoch stays 1): Eric said "the next
  release will reset game progress for everyone" while answering the slow start, which is not built
  yet, so the reset rides the slow-start release instead - **confirm with Eric**. `release.sh` now
  judges a desktop export that finishes but never exits by its log and .pck.
  **For Eric to look at:** the phone title is still dark under the ring (its foreground grass is drawn
  as occlusion blades, plus the ward falloff) - the PC one is lit; whether the earlier Apotheosis feels
  right; the chest Ban closing the chest with no relic; nobody has listened to the three defeat sounds.
  **Next:** the slow start (a design pass on which achievement teaches which system and the post-run
  cutscenes, then the build); the castle (C5 dark wizard, C2, C8, C9); the secrets catalogue.
- **After 0.11.0 (released, `main` = `main-ff86ni` at the release):** GitHub triaged - ten issues closed
  as done or obsolete, #67 cancelled by Eric, only **#44** open. Its decisions are applied (the Reliquary
  -18%, the Lightning Rod's speed always on). **Building:** the slow start (`wip/slow-start`, schema
  **22**), the dark wizard (C5, `wip/boss-dark-wizard`), the Dungeon move with the castle's map stops
  (C2 + C8, `wip/dungeon-move`, schema **21**; nothing flips `IsPlayable` without Eric). **Queued as
  slots free:** #44's 10,000-run chest simulation and a fresh relic sweep at 50 HP with the new sets;
  the rest of the secrets catalogue with the castle's (C9).
- **Later the same night:** boss arrivals hold the camera through the bars, then spring-pan; and every
  spell already out freezes for the arrival (`Player.SpellGroup` / `TagSpell`, `FreezePlayerSpells`).
  **Merged: the dark wizard (C5)** - the taking and its seal, his light, all he took, the ending
  (`.ai/endgame-castle.md` 13; health 30,000). **Held, ready: `wip/dungeon-move` (C2 + C8)** - the
  Dungeon in the castle (schema 21, the Gaoler at 900,000), the castle on the world map; flipping the
  Last Bridge's `IsPlayable` is then one line and raises no validator line. Per 9.2 the move and the
  flip land together: **waiting on Eric's OK**. **Open for Eric:** the Apotheosis now lands in the
  Throne's first wave (the gate at 21 under an 18-level Muster sized for 23) - cut the Muster or put
  the gate back to 23; the ending's credits say "drawn by Eric's kids" - check the wording.
- **Eric's answers and merges, end of the night:** the ending's credits drop the Sketchbook line; the
  Apotheosis may come early in the Throne ("the throne should be the final level, basically a boss fight
  the whole time"). **Merged:** `wip/dungeon-move` (C2 + C8; the Last Bridge still NOT flipped - until it
  is, the Warden of the Deep opens the Dungeon directly) and `wip/slow-start` (schema 22 after 21; the
  ladder of three vault lessons is the integrator's choice, for Eric's review). **Building:** the Dark
  Wizard as a playable wizard, unlocked by defeating him in the Throne on Hardcore
  (`wip/dark-wizard-playable`; Eric: "unlockable if you beat the whole game on hard mode"); #44's chest
  simulation and 50 HP relic sweep (`wip/chest-sim`). **The Last Bridge is flipped playable** (Eric, the same night; chapter 10 of
  13, the Dungeon 11). **Still to ask:** the next release raises the story epoch to 2 (the slow start is in);
  the Court and the Throne flips.
- **The defeat sequence** (Eric, 2026-11-02): *"Defeat should have a dramatic animation where the game
  freezes, then crash zooms onto the wizard and they fall, then it says defeat, and when the player
  clicks, the defeat screen covers up. It then tallys up the reward with a satisfying payout sound. The
  end game info pane should not show much info at first, but the player can click into info sections to
  see stats and details."* There is no player death animation today, and `GameOverScreen.cs` (1485
  lines) shows everything at once.

## 3c. Eric's answers, 2026-11-02 (the open-questions sweep)

Accepted as recommended: keep every Options setting on a story reset; **Options' Reset Progress backs
the save up too (to build)**; the first real reset comes one release after the reset code ships;
**combinations do not count toward "The Grimoire Restored" (to change: it counts 36 today)**; the
carried-over level 4s stay as built; a held full draw fires by itself; the boss crowd-control limits
stay; the wizard and bosses stay drawn over trees; **a "reduce screen shake" option in Options (to
build)**; keep the vague vault clue; **the Mystic Ruins' floor redrawn as broken paving (to build)**; the
bridge's +3 levels is fine; the title wizard hides in the prologue; Laelaps keeps the mantle; the music's
0:24 break and boss fights without melody stay.

Answered otherwise: Sagittarius's offer rate - **Eric will test it** (no change). Seimei's Exorcist
**keeps its +15% on bosses too** (damage, not an execute). Announcements: **never two at once** - the
`Announcements` queue already guarantees it; First Blood's wait stays. **Seimei's Apotheosis is not early:**
the agent compared against run-pacing's 2026-10-15 table (16.5-20 min); the current measurement in
`.ai/gameplay-design.md` is 7.8-9.1 min against a 7.5 floor, and he took his at 9.9.

Elim's "it's wizarding time" drawing: **not in Eric's email** - the only image Elim sent is
`fnfdodgeythingy!!!!11!!!11!!1!!.png` (2026-10-14, for Blobby). Waiting on Eric for the file.

## 3d. Later on 2026-11-02: wizards, the Sketchbook, the castle switched on

- **Names.** The blue-robed title wizard is **Merlin** (verb *rewind*, the Orb of Ages, unlocked by the
  campaign cleared at Default; later that day Eric took it back - the blue-robed figure is the player and Merlin
  has a look of his own, `.ai/merlin.md` section 5); the Throne's kept wizard is now **Nimue**, the Lady of the Lake, keeping
  Spiritual Weapon's sword, Apotheosis **Excalibur** on the Sword form, verb **sever** (Eric). Nimue is
  docs only until built.
- **The Sketchbook holds nothing to earn** (Eric: "no unlocks or any achievements. move that to other
  maps"): Best Drawing removed, its secrets shelved, `ValidateSketchbookHoldsNothing`; Ma Liang moves to
  the Court as its second event (the greedy emperor's painter, the Gallery). Merlin's and the Dark
  Wizard's "every chapter" unlocks must not count it.
- **The Court and the Throne are playable** (Eric: "You can switch all of that on and then commit it").
  Both chapter notes written; `SaveThroughGates` walks the whole road now. `_BossProbe` 12 and 13 both
  run to the fall (the Seneschal's three flights; the procession, then him at level 21, 483 s).
  **REMIND ERIC once he has played the Throne** (section 4) still stands.
- **Every chapter wizard is to be built** (Eric: "keep working and building until all wizards are
  finished"). Order: Wayland and the Sibyl (building, `wip/wizard-wayland`, `wip/wizard-sibyl`);
  **Merlin merged** (`d923ecc6`, `.ai/merlin.md`); P2 (a second event per chapter, `wip/second-events`);
  then Baba Yaga, Paracelsus, Imhotep, Medea; Morgan; Nostradamus and Ma Liang (the Court); Nimue (the
  Throne).
- **Merlin, to look at:** the Orb of Ages' ring and star are thin smooth lines, not the 2 px world grid -
  the complaint Cyclone Slash had (2026-10-29), so it wants the same pixel-art pass; his spell trails
  Kindled Ward late (40% of his damage against Seimei's 56%); whether holding the circle feels like a
  choice on the phone stick; his sheet beside Väinämöinen's.
- **The name is The One He Missed** (Eric, later the same day), everywhere - repo, game, website, and the
  GitHub repo renamed to `eclancy/TheOneHeMissed`. **The blue-robed title figure is the player**, "the one
  he missed", story only (title, prologue), and **not Merlin**: Merlin gets a look of his own. Building:
  the rename (`wip/rename`), the title art for the new name (`wip/title-art`), Merlin's new look
  (`wip/merlin-art`); the website's half is PR eclancy/EricClancyEngineeringAndCrafts#48, held until
  the GitHub repo is renamed (old `/wizard-survivors/version.json` and `/download` keep answering for
  installed copies). Merge order: rename the GitHub repo, merge the site PR once its CI (`nginx -t`)
  passes, then the game rename.
- **Merged since:** Wayland (`56097665`, index 12) and the Sibyl (`4738a49e`, index 13, hint row 8).
  **Building:** P2, a second event per chapter (`wip/second-events`).

## 4. Waiting on Eric

- **Playtest 0.10.0** (50 HP, scarce healing, tougher chapters and bosses). Fixes from it come
  before the castle.
- **REMIND ERIC once he has played the Throne:** he kept all five of these (2026-11-01) and asked to
  be reminded to check them again after testing.
  **The Throne's departures from 6.x** (C4 agent, merged as built, each a constant to change): the
  guard stands down while a pair fights (with it, auto-aim spent the spells on the swarm and no boss
  health worked); the Muster is 18 levels, not 17 (at 17 the bot never reached the Apotheosis); elites
  and the miniboss clock are off there, chests fixed at 2 + 1 + 1; a pair's banner is the two names,
  his is his tagline, a 4 s beat before him. Its `ChapterDifficulty` row is still the Emberdeep's
  stand-in and was measured with one build: a tuning pass across wizards is wanted before the flip.
- **Decided 2026-11-01: bosses are excluded from execution abilities** (done: the Reaper's Scythe's
  execute skips a `BossEnemy`; the Morrigan's peck already spared elites and bosses; the Greataxe's
  Headsman only doubles damage, so it is not an execute). A new execute must skip bosses too.
- From 2026-11-01's pieces: whether the Court Falconer's hawk reads as a bird and the Huntsman's horn
  as a horn at game size (the Falconer throws a bolt, not the hawk: a hawk would be a new projectile
  scene); whether the Constable's many 12 s yard visits feel repetitive.
- **Decided 2026-11-01:** achievements count the moment they are met (done: `Node2DGame.RecordAchievementNow`);
  the Sketchbook stays open for saves that already played it (no change); the Dark Wizard is a
  silhouette only in the prologue (done, `tools/art/prologue.py`); Thorned Vine stays as it is until
  Eric playtests it.
- Two calls from the quests work: should a chapter's first-visit note replace its banner (it does
  now), and is the HUD's quest corner fine as outlined text with no plate behind it?
- Creeping Barrage sits at the foot of the damage band, and Solar Flare's two Ascensions measured
  under it (267-304) because the flare kills everything in one pulse; worth a playtest.
- 104 mutation and Ascension descriptions were rewritten to fit 130 characters; worth reading for
  tone. Some numbers left the cards (Anathema's, Threefold Curse's, Cascade's).
- Needs eyes in motion: the ban tear (rip, not crack?), Mjolnir's chained strikes, the cat's return
  and the Purr heart on a phone, Black Cat's casts, the Rampart dome, the whip loops, the map
  reveal's darkness edge on a phone.

## 5. What comes after

In this order. Fixes from the 0.10.0 playtest come before anything below.

1. ~~The pixel-art pass on Sleet Runes and Aegis Ward~~ and ~~C6~~: done, in 0.10.0.
2. The castle, by `.ai/endgame-castle.md` section 9.2: C5 (~~the Constable~~, ~~the Seneschal~~; the
   dark wizard after C4), C4 (in flight), C2 (**schema 21**, in flight), C8, C9; each chapter's
   `IsPlayable` flips last. The bridge's flip needs C2, the pre-flip fixes and C8 (a playable chapter
   with no map stop is a validator fault), so those land together.
3. The rest of the secrets catalogue, `.ai/achievements-and-secrets.md` section 8.

## 6. How the cloud session worked

One agent per piece, each in its own worktree on a `wip/*` branch, briefed with the design section
and the files it alone may touch; the integrator merged each finished piece into `main-ff86ni` with
a `--no-ff` merge commit, then built, ran the validator, ran `--import` for new art and looked at
screenshots before pushing. Shared docs (`.ai/spell-roster.md`, `.ai/art-replacement-manifest.md`
section 9, `scripts/FrameProfiler.cs` sections, `scripts/SpellEvolutionCatalog.cs`) conflict on
most merges: keep both sides. **Five agents at once hit the 5-hour limit within a few hours;** two
or three goes further, and agents should keep tool output short.

## 7. Loose ends

- **`_BossProbe` drops drain ticks on a loaded machine**: at its default `--timescale=8` a slow frame's
  delta covers several 0.25 s ticks but `sinceDrain = 0` grants one, so a fight measures longer under
  load (Elderbark 118 s with three agents' Godots running, 40.7 s at `--timescale=2`). Subtract the
  interval instead of zeroing, or read fight lengths at `--timescale=2` when other Godots are running.

- **An intermittent `InvalidOperationException: Handle is not initialized`** (a freed Godot object used
  from C#): seen 2026-11-01 in `SpawnEnemy` (a Throne run), inside `RegressionChecks.ValidateFear` (a phone
  validator run) and once in `_BossProbe --stage=7`; gone on rerun. Not from any one merge. Worth
  catching with a stack trace and a guard on the stale reference.

- On this PC the headless `--import` often finishes its work and then never exits: judge it by its
  log reaching "post-reimport operations", and run it under `timeout`.
- `scripts/_BossProbe.cs`'s header promises `--stage=all`, but line 62 `int.Parse`s it and the probe
  throws in `_Ready` and idles. Pass one stage at a time (the Choir is stage 2, not 3).
- The pause screen numbers chapters by stage index (the Fallow Fields shows "Chapter 10"); the map
  says "Chapter 2 of 10". `Node2DGame.cs`, around the pause screen's chapter line.
- `ImpactDebris` still fades by alpha in its last 30%, and Ice's first two ramp colours vanish on
  the Sketchbook's paper.
- `lingering_pool.gdshader` has no caller.
- CLAUDE.md says only Python 2.7 is installed; the cloud container had Python 3.13, and the
  generators run on both.
- About 4 of 60 balance-bot runs crashed in the engine with "Caller thread can't call this function
  in this node (/root)", before and after the difficulty change alike.
- A chain cast still has rare 86-149 ms spikes inside the chain code; pooling the arc nodes is the
  suspected fix (`.ai/spell-roster.md`, Shadow Bolt).
- Thorn Crystal Shell does about 600 damage a second at level 8, over the 331-465 band.
- Extinction Event covers 26% of the screen at its peak; worth watching.
