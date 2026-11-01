# Work in flight (snapshot, 2026-11-01, updated after the 0.10.0 release)

A handoff from the cloud session that ran 2026-10-30 to 11-01. Eric moved the work to a local Claude
Code session. **A snapshot, not a source of truth:** check `git log` and the branches before acting
on it, and delete this file once everything below has landed or been dropped.

## 1. Released

**0.10.0 is released** (`Release v0.10.0`, 6b3af6e, tag `v0.10.0`, 2026-11-01, from the Windows PC
by the local session at Eric's request; dry run first; `ecec.dev/wizard-survivors/version.json` serves
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
4. **Boss crowd control, and the Hollow Choir** (after 3, which owns `Enemy.cs` movement). Bosses are
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
6. **Wizard clues and the ice lake**: every wizard's hint becomes a cue in the world from its direction
   (the singer: musical notes drifting in from his side, and his singing heard), its line fading in on
   screen as a hint rather than in a speech bubble; all wizards' clues this way. The wizard trapped in
   ice sits in the centre of a large, magically animated lake (the current art reads corny).
7. **Music**: the menu's music should give way to more interesting, thematic battle music for each level.
   And **the Elder God Maw opens with a deep, scary crash** (a new generated sound, by
   `.ai/audio-direction.md`).
8. **Ground per chapter**: every chapter's ground should read as its place; "Emberdeep doesn't look like
   cracked volcanic earth." A ground pass through `tools/art/tiles.py` and the chapters' terrain
   modules (`.ai/terrain.md` first), the Emberdeep first.
11. **Done:** the Test Wizard is off character selection (69986af); the Sketchbook's boss is **Elim**
   (b3695cf). **Waiting on Eric:** Elim's "it's wizarding time" drawing, to show when he arrives - it is
   not in the repo or this PC's Downloads or Pictures.
9. **Make the Apotheosis visible** ("make it clearer that apotheosis is an option, showing in
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
