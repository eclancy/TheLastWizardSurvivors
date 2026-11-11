# Work in flight

> **2026-11-09:** the Sketchbook (`stage_8`) with its whole cast - the kids' drawings, Elim's boss and ghost, the dog - and the Test Wizard were removed at Eric's request; git history holds them, and schema 28 takes both out of saves. Where this doc still names them it is the record of what was, not the game. Stage index 8 and roster index 0 are retired, never reused. A command here with `--character=0` named the Test Wizard: `--spell` / `--start-spell` now open any wizard on the spell, so say `--character=1`.

> **0.22.0 is released (2026-11-09), and it is a story reset:** release commit `11f528a2`, tag `v0.22.0`, story
> epoch 2 -> 3 (`c5505536`, Eric: "This release should reset the game for players"). All nine downloads are
> published; the three version.json addresses serve 0.22.0 with `progressEpoch` 3; the APK reads
> `com.ecec.wizardsurvivors` 2200 / 0.22.0, labelled *The Last Wizard: Survivors*. It carries the far shore
> (the White Downs, Artemis), 20-minute far-shore runs, endless, the Conjunction ladder, the two-spell Sovereign
> Rite, the redrawn prologue, the character-fit walk-through, the four new spells (Corposant, Ink Ring, Windfall,
> Lancet: merge `f0af5110`) and the Sketchbook and Test Wizard removal. Validator 0 errors, 0 warnings at
> epoch 3. **Not played by hand:** the four new spells' feel and numbers, the reset on a real installed copy.

> **0.24.0 is released (2026-11-10), a story reset:** release commit `85ffc052`, tag `v0.24.0`, epoch 4 -> 5 (`5561a9fb`). All nine downloads are `TheLastWizardSurvivors-*`; the three version.json addresses serve 0.24.0 with `progressEpoch` 5 and `program` "TheLastWizardSurvivors"; the APK reads com.ecec.wizardsurvivors 2400 / 0.24.0. It carries every item built on 2026-11-10 (the four batches, the follow-ups and the icon refresh). Not played yet. Still on the list: 63, 64, 77, 78.

## The next release: Eric's list of changes (started 2026-11-09)

Eric, 2026-11-09: *"Let's just start compiling a list of changes and we'll determine another release soon."*
Collected, not built; he decides when it becomes a release. Each line points at its full entry under "Next week".

| # | change | kind | size |
|---|---|---|---|
| 63 | A historical gallery: each unlocked character's real history | new screen, needs design | M-L |
| 64 | Plan the far shore as a DLC (one of six gods built; Artemis free today - keep as taster?) | plan, no code | - |
| 65 | Download files renamed in the next release (decided); the program's own rename later | release tooling + updater | done |
| 66 | Reward review done: five fixes decided (Veteran/Ninth Stone swap, Fireball to the Emberdeep, a Quarry boon for The Coursing, Pheasant asks 4 Light, Smith's Toll at the Cold Forge) | achievements | done |
| 67 | Canticle's notes draw under the wizard (one line: `Air` -> `ForemostAir`) | bug | done |
| 68 | Waywiser: reword to "the spell with the longest cooldown" (decided), and make the code pick exactly that | text + one line | done |
| 69 | Remove the Twinstar Sigil from the shop (decided); that release resets progress (epoch 5) | shop | done |
| 70 | The chapter note (the stage intro with its hints) is huge on a phone and covers the wizard: condense, move clear, shorter | UI | done |
| 71 | Consolidate the Powers pages (six today, the first holds one card) into about three | UI | done |
| 72 | The "Dandelion Grove" (flower pen): only in the Forest (decided), and one or two a run there | world gen | done |
| 73 | Chalice Cup: heals 3 HP every 20 s, waiting until you are hurt - **built** | item rework | done |
| 74 | The wizard's feet sink when he wades; Baba Yaga and Nimue neither sink nor slow in water (decided) | visual + trait | done |
| 75 | The Fenced Bloom's garden heal wears off after 20 HP - **built** | secret | done |
| 76 | Show Aegis Ward recharging on the spell bar; fix its wrong tagline | HUD | done |
| 77 | Balance pass on every spell (Aegis Ward's recharge looks too fast; Twinstar folded in) | balance | L |
| 78 | Finish the far shore's five remaining gods and their chapters (F6-F10) | content | XL |
| 79 | Baba Yaga's hut heads for its yard whenever you are near (too hard to herd) | puzzle | done |
| 80 | The hut flips facing too often: smooth its heading for drawing, hold each facing | visual | done |
| 81 | Buckler Boss gives its own small shield (decided) | item rework | done |
| 82 | The hut kneels only in the middle of the bone yard | puzzle | done |
| 83 | Every unlocked character appears on screen for a moment (as Baba Yaga leaves her hut) | presentation | done |
| 84 | Each wizard's opener gets a new deed, sealed until the wizard is freed and done as them (not the Familiar, Thorn Vine or Spiritual Weapon) | unlocks | done |
| 85 | Status-effect marks still draw on enemies hidden for a boss's arrival (one line) | bug | done |
| 86 | The stage card's Start button is still off the bottom of the phone: grow the card up, scroll its body | UI bug | done |
| 87 | Wayland's Wings also gives movement speed (about +10%) | character | done |
| 88 | The opening's town attack seen from inside the town, looking up at burning houses | prologue art | done |
| 89 | The Archmage's passive card: "Avoids death once per run." | text | done |
| 90 | Lancet's icon looks like a tool, not magic: give the needle a spell's light | icon art | done |
| 91 | Backdraft: two flames on different enemies; Fire Fan reworked into a wide sweeping arc (decided) | spell | done |
| 92 | The level-up Codex button sits off the heading's line (raised before, item 55) | UI | done |
| 93 | Aegis Ward: a more detailed icon; a smaller, quieter animation (decided) | art + VFX | done |
| 94 | Ink Ring becomes Ma Liang's starting spell; The Painted Door moves onto it | character | done |
| 95 | An enemy's attack circles (and their hits) vanish the moment it dies | bug | done |
| 96 | The Scythe set's card: "...kills it outright (except bosses)." in place of "...kills it outright. Not bosses." (`ChestItemCatalog.cs` 413; Eric, 2026-11-10) | text | done |
| 97 | Remove attunement altogether - its damage bonus for pure spells, its cooldown price, every word of it (decided; rebalance in 77) | mechanic | done |
| 98 | A full build's level-up offers 10 Arcane Energy beside Marginalia (decided) | level-up | done |
| 99 | Elderbark's slam and roots much bigger in area (not the other bosses) | boss tuning | done |
| 100 | Thorn Vine's shove always away from the player (today along the swing, sometimes toward him) | spell | done |
| 101 | The Pinfold's guards stay by the gate, glow and are marked; the latch lights once they fall | event | done |
| 102 | Aegis Ward's new icon is lopsided: redraw it symmetrical | icon art | S |
| 103 | Land still flaps over the water's edge: find where, then make the drawing match the water | terrain | S-M |

**Built 2026-11-10, on `main-ff86ni` (2a05416f, eee41dbd, 64e7ecb4), not released:** 66 (First Blood pays Lancet; Shrike removed), 67, 68, 72 (pens only in the Forest: one guaranteed in the ring of chunks round the start, then `LandmarkChance` 0.04, never in neighbouring chunks), 79 + 82 (the hut runs round the fence for its yard's gap and kneels within 28 px of the middle), 80, 84 (twelve openers on deeds done as their wizard, sealed until freed - `ValidateWizardOpeners`), 85, 87 (Wings +10% move speed). Build 0 errors, validator clean, a 3-minute bot run threw nothing. **None played in Godot yet.** Open for Eric: (decided 2026-11-10, Eric: *"100 energy is fine"*) the three events and the Cave boss pay 100 Arcane Energy once now that their spell moved; still open: the three secret-vault deeds name the chapter, not the secret. **The next release is a story reset** (Eric, 2026-11-10: *"assume we will just reset progress after this update"*): raise `SaveData.CurrentProgressEpoch` to 4 in its own commit right before `release.sh`, as 0.22.0 did.

**The next release resets progress again (epoch 5)** - Eric, 2026-11-10, choosing a reset over a refund when the Twinstar Sigil is removed (item 69).

**Built 2026-11-10, second batch, on `main-ff86ni`, not released** (cd074d7c, ef9a8fb3, d713c382, 434858e1, ef5d4b12, 925d41e1, 5593144c): 65 first half (downloads named `TheLastWizardSurvivors-*`, the program keeps `TheOneHeMissed`), 69 (Twinstar Sigil removed), 71 (three Powers sections, cards spread evenly; 8 pages on the Pixel 9, 11 on desktop), 76a (Aegis Ward's text), 81 (Buckler Boss: a 4-point shield every 25 s, keeps +15% defense while a shield holds), 84 (the vault deeds name their secret), 86 (the phone's stage card grows up and scrolls; Start always on screen), 89, 93 (Aegis icon redrawn; ring 64 px across, thinner, quieter; dome 52 -> 44; Reforged 6 shards at 0.8), 95 (every boss passes its owner; corpses drop their warnings), 96, 98 (+10 Arcane Energy card beside Marginalia), 99 (Elderbark: slam 210 -> 280 with a 1.3 s tell, roots 66 -> 120), 100, 101 (Pinfold guards leashed to posts by the gate, blue glow and seal mark, the latch lights). Build 0 errors, validator clean, boss probe and function probe pass, release dry run names all eight downloads. **Not played yet.** **Before the next release:** ecec.dev's download page takes the program name and the Linux folder from the download's file name (its `data-base` spans), so it would say TheLastWizardSurvivors.exe - **fixed and live 2026-11-10** (eclancy/EricClancyEngineeringAndCrafts#55: the program's name is written in the page, no longer read from the file names). That release resets progress (epoch 5, item 69).

**Built 2026-11-10, third batch, on `main-ff86ni`, not released:** 66's five reward fixes and the Quarry boon (ab544d52); 88, the town attack from inside the town (d52799a1); 65's second half, the program renamed `TheLastWizardSurvivors.exe` from 0.24 with every installed copy still updating (merge 867fbe96, tested from a real 0.23.0 copy; existing installs keep `TheOneHeMissed.exe` until the setup is run over them); and a latent validator race fixed (1a82089c: `ValidateSpellData` discarded each loaded spell, the `gchandle.is_released()` crash the tide's timing exposed). The site's PR 55 was reverted (PR 56, live): program and downloads share a name again from 0.24. Open: the Quarry adds about 1% damage in a swarm (strong only on elites and bosses) and may want a stronger rule - renamed **Hare's Foot** (Eric, 2026-11-10).

**Built 2026-11-10, fourth batch, merged on `main-ff86ni` (5bcfffe4), not released:** 97 attunement removed (bonus, cooldown price and every word; the set_collector deed renamed "Fully Enchanted"; Gloam Veil's walk price gone too); 91 Backdraft two flames at 60% at least 90 degrees apart, Fire Fan one flame sweeping 60 degrees each way (+140%); 92 the Codex button centred by its real height; 74 feet sink 8 rows in water with rings, `CharacterData.RidesOverWater` for Baba Yaga and Nimue (not slowed, not sunk), the Swallow-Hole's mud too; 76b ward gauges on the spell bar (Aegis, Stormguard, Gloam Veil); 70 the phone's chapter note shows the heading, one line and a count of the clues, held 4 s; 94 Ink Ring is Ma Liang's opener, The Painted Door on it (Water), Cormorant his deed, ink 2.0 -> 2.5 s; 83 a freed wizard steps out beside the player (John Dee, Laelaps, Tituba, Artemis and later ones; the twelve event scenes keep their own); 90 Lancet's icon a needle of light. Build 0 errors, validator 2 of 2 clean. **Not played.** Open for Eric: the phone note's clues as a count only (or first clue + count); Water for The Painted Door; Ma Liang's weaker first minute; the door still the crow's violet sprite; Backdraft + Fire Fan both sweeping; stale Backdraft/Fire Fan evolution icons and `tools/art/evolution_catalog.json` card text; Wings' card still says water does not slow.

**Follow-ups to the fourth batch, 2026-11-10 (46c2ff90, a6d6c18b, 5aebf28d, 3c7290aa), not released:** Ink Ring closes when he comes back within 28 units of his own line and its strike also bites what stands on the line (Ma Liang's first minute: kills match the crow's, damage taken 81-113 against the crow's 0-13, better than the crow from minute 2); The Painted Door repainted in ink with Water's light; Backdraft's and Fire Fan's icons redrawn and `evolution_catalog.json` regenerated; the phone's chapter note shows the first clue plus "and N more". Validator clean twice. Noted, not done: 45 other evolution icons are stale against their generator (a full run rewrites them); the icon generator cannot rotate a drawn shape cleanly.

**A snapshot, not a source of truth:** check `git log` and the branches before acting on it.

## Next week (list started 2026-11-04, after the 0.13.0 release)

**0.13.0 is released** (`Release v0.13.0`, 2143dbe8, tag `v0.13.0`; story epoch still 2, so no reset; both
feeds serve it; `main` level with it). It carries everything in section 0 below plus the 2026-11-04 work: the
prologue's six lines and redrawn villains, Thora's flowing cape, foliage in five chapters, the Forest with no
shooters, the card text and spell reaches, the footing rule's wide-trunk margin, John Dee at 100 chests, and
Paracelsus's rot-path puzzle. Stopped for Eric's weekly limit (5% left), not finished. **Add to this list as
things come up; strike what lands.**

**Fixes and changes to start:**

- **Released 0.13.2 (2026-11-06):** release commit `35e9bdc8`, tag `v0.13.2`; all nine Windows,
  Linux and signed Android downloads published and verified against the local artifacts. Both
  update feeds serve 0.13.2 with story epoch 2, so progress is preserved. Carries the title-loading
  fix and the four quick fixes, plus bounded results/spell cards (18/24), inspectable unlocks (35)
  and quit-run rewards/animations (36). Rehearsal and full content validator passed; packaged Windows
  checks passed startup, crowded/compact results, unlock input, long spell cards and rendered owed
  reels on quit. Final Windows title check: 1,916.3 ms preparation, 52.9 ms Enter-to-menu in this run.
  Portrait/safe-area/injected-touch checks passed; no physical Android device was connected.
  Items 39-41 are new next-week notes, including the clarified two starting + two purchasable boon slots.

- **Ascension discovery (2026-11-05, `codex/ascension-guidance`):** built from the archived playtest
  request first. The world-map's silhouettes open hints, teaching chapters advertise their hidden
  lesson, the opening note points out golden lights near the start, and encountering those lights
  gives a persistent gold marker toward the teacher. Applies to all three teaching landmarks and
  keeps the existing puzzles and post-run lesson reels. Build and content validator passed (0 content
  errors, the existing Test Wizard warning); all three teacher walkthroughs passed, including lights
  near spawn, the real landmark bearing and retiring its marker on completion. PC and phone layouts
  inspected; controller A opens a hint, B closes it. **Released in 0.13.1**, tag `v0.13.1`, release
  commit `993092ad`; all nine downloads published. Story epoch remains 2, so no reset. The signed
  APK passed the release rehearsal's certificate and version checks before publication.

1. **Finish the foliage pass: the Frozen Waste and the Mystic Ruins.** Both builders were stopped mid-work;
   resume each in its worktree, on `.ai/terrain.md` section 13's recipe (salts assigned: Frozen 0x5CF2-4,
   Ruins 0x5E02-4). Merge each with `ground_detail.py` re-run for the atlas.
   - `ws-art-foliage-frozen` (`art/foliage-frozen`): the move (f072c727) and the snow-ghost groves (71d09d5b,
     "work in progress, not yet measured"), one uncommitted file. Nothing tested.
     **Built (2026-11-07), ready for the integrator to merge:** the left-over tuning committed as found (5fcd94ba);
     main merged (1e078335 - the atlas the only conflict, `ground_detail.py` re-run in full); imported, and
     `props_frozen.py` reproduces every committed sprite byte for byte. Measured and written up in `.ai/terrain.md`
     13, "The Frozen Waste: snow ghosts, not woods". Validator 0 errors, 1 warning (test_wizard); footing census on
     seeds 4242 and 11, 0 trunks under 72 px, no pocket or narrow room; `--check-feet` three minutes walking and
     kiting, 0 frames in the ground, 0 stuck checks; `_ExploreProbe --stage=5` 23 passed, 0 failed; light probe: the
     cast and the orbs read as before, the new art tops out at +24 over the snow. Close-ups checked for bricks and
     paving: the ghosts are pillowed lumps, the boulders split rounded granite. Shots: `_shots/foliage-frozen/`
     (`composite-pc.png`, `composite-phone.png`, before | after on the same build). The old Ice case in
     `BuildDecorProps` is left for the integrator. **Found, not this pass's:** the Buried Runes' vault walk fails its
     stillness beats (the stones show on arrival) with or without this foliage, and failed on the move commit too.
   - `ws-art-foliage-ruins` (`art/foliage-ruins`): the move (93844a88, hash proven unchanged) and the
     overgrown quarters (68ed6cc2), one uncommitted file. Not yet validated, probed, censused or shot.
     **Built (2026-11-07), ready to merge.** The uncommitted file was the moved-only version swapped in for the
     before photographs, and was dropped. Main merged in (the atlas re-run by `ground_detail.py`); the quarters
     made denser after a zoom-0.3 look showed scattered pieces, not massed ruin (standing ruin 1500 -> 2100,
     rubble 1300 -> 2000 in fours, the quarters' ivy and roots up a third). Validator 0 errors and 1 warning;
     footing census 0 trunks under 72 px and no pockets on two seeds; `--check-feet` 0 frames in the ground;
     `_ExploreProbe` 25/0 on seeds 4242 and 31; the Broken Ward's `--secret-probe` all 33 pass. `.ai/terrain.md`
     section 13, "The Mystic Ruins", has the numbers and what still needs eyes (does a quarter read as ruin with
     stone at the floor's value; are the piers broken walls or a brick stack -
     `_shots/foliage-ruins/closeup.png`). **Not this pass:** `clip_check.py` flags the Fields' Pinfold art (two
     cut edges, `pinfold-front.png` and `pinfold-open.png`, from main).
   - Both were told what got the Sands and the Heath sent back (rocks as bricks, crowns as paving): check
     their close-ups for it before merging.
2. **Play what 0.13.0 shipped unplayed:** Paracelsus's rot-path puzzle (it replaced a working event); the
   foliage in the Forest, Fields, Swamp, Sands and Broken Circle (does a stand pin you, do the hedges read as
   hedges, are the Heath's crowns too sparse); the spell reaches; the Forest without shooters; John Dee's chest
   economy (every other wizard has about a third of a set fewer chests a run).
3. **The unlock puzzles** in `.ai/unlock-puzzles.md` (designed and chosen by Eric): **all eight built.**
   Paracelsus's (the pilot) first, then **2026-11-07/08**: **Baba Yaga's herded hut** (`feat/baba-yaga-hut`, the
   Built note in 3.3), together with item 59's hut asks (solid, four-sided, falls facing the camera); **Medea's
   balanced scale** (`feat/medea-scale`, 3.5); **Väinämöinen's song sung back** (3.6); **Morgan's lamps lit
   ahead** (3.7); **Ma Liang's door in one stroke** (`feat/ma-liang-door`, 3.2); and on 2026-11-08 **Imhotep's
   mirrors routing the sun** (3.1) and **Tituba's key slipped past the lanterns** (3.8), both merged and first
   released in 0.21.0. What every one still needs is a person playing it: whether the rule is found unaided.
   - **More hints for unlockables** (Eric, 2026-11-04). His example: in the first chapter, Baba Yaga's walking
     hut walks a visible path, and that path is drawn on the map, so the player finds it by following tracks.
     Do this for every unlock: each wizard's event and each secret leaves traces in the world that lead to it
     (tracks, trails, signs) and something on the map or minimap once seen. Start from what exists - the hint
     cues and lines (`StageHintCatalog`), the warm-glint and decor-gradient tells (`.ai/stage-secrets.md`), the
     wayfinder and the quest log epithets - and from the per-event asks in `.ai/unlock-puzzles.md` (Baba Yaga's
     there is a herded hut), and fit the new traces to each.
4. **Decisions waiting on Eric:** crits in the damage colours come out near-white and alike (the element's own
   colour, crits marked only by "!" and size, is the alternative); the light pool's depth and Reduce Darkness
   (40% or off); Nimue's and Ma Liang's open items (section 0 below); the Oubliette's voice, never heard.
   **Decided 2026-11-08 (Eric):** crits take the element's own colour, marked only by "!" and size; the light
   pool's depth stays as the previews show it, Reduce Darkness stays at 40%, and every fire and light spell is a
   light source (`.ai/light-pool.md` section 3's unwired list, all of it). To build: the decisions batch.
5. **[BUILT 2026-11-07, merged for 0.19.0: one shared `ResponsiveLayout.NoWidow` rule binds a wrapping
   line's last two words - chapter note, quest tracker, taglines, stat lines, banner, tips, the plate.]**
   **No single word left alone on a line, on PC and on the phone** (Eric, 2026-11-04: "any UI that shows up
   for text we should try to avoid wrapping single words onto next line. I saw that a number of times in the
   descriptions of levels when you first start the levels"). First the chapter's opening card (the note shown
   as a run starts, `ChapterNoteCard` / `ChapterNotes`), then every wrapped text: level-up and modification
   cards, quest lines, tips, banners. Photograph each at both canvases (`_UiShot`, 1280x720 and 540x1212
   `--portrait --safe-area`) and fix by width or wording, or by a shared rule (balance the lines, or keep the
   last two words together) rather than one string at a time; a check in the validator or `_UiShot` would keep
   it from coming back.
   - **The Archmage's spell line on the character select** (Eric, 2026-11-04): on a phone his starting spell's
     description runs past one line on the character description page. Shorten it to one line at 540x1212
     `--portrait --safe-area`. Find which string the details pane shows (Magic Missile's `Description` or
     `Tagline` in `SpellData.tres`, or the select's own line in `CharacterSelection`), then check the other
     wizards' lines the same way.
6. **The title screen's freeze, and its prompt on a phone** (Eric, 2026-11-04). Read `.ai/title-screen.md` first.
   - ~~**The freeze.**~~ **Fixed, 2026-11-05:** measured about 14 seconds from the first press to a
     menu draw on Windows, including 12 seconds of developer regression checks. Those checks now
     require `--validate-content`. The menu is loaded and built before the title prompt, behind
     the existing art's animated ritual ward/fire, with a first draw to warm its layout and fonts.
     Press-to-menu now measures about 34-44 ms in the test checkout (45 ms in desktop portrait),
     105-106 ms in the main checkout. Preparation varied from 1.2-1.5 s in the test checkout to
     4.1-8.7 s in the main checkout; the loader covers that work before accepting title input.
     Keyboard/mouse/pad opening, early loading input, direct returns
     and failed-load retry passed `_TitleLoadProbe`. See `.ai/title-screen.md` for the timings and
     verification command. **Physical Pixel 9 timing remains pending: no device connected.** The
     phone prompt and composition tasks below remain open.
   - **On a phone, "Tap to start"** in place of "press any button" (`OS.HasFeature("mobile")`, as
     `TouchControls` decides it).
   - **The phone title's empty middle** (Eric, 2026-11-04): on Android the eyes in the dark are spread as if
     making room for the title, but the title takes much less room than they leave, so there is a big blank
     area in the middle of the screen with no eyes. Lower the title and spread the eyes to fill the space.
     Then, when the player presses start, the title moves up the screen and the menu's buttons fill the room
     it leaves (Eric) - one motion from the title to the menu, not the menu dropped over the art.
     The portrait art and its eye field are generated (`tools/art/splash.py`, `.ai/title-screen.md`): change the
     generator, re-run it, check it at 540x1212 `--portrait --safe-area`, never edit the PNG.
7. **[BUILT 2026-11-07, merged for 0.19.0: the plate reads "Achievement - name / the deed's ask / reward".]**
   **The achievement banner says what you did** (Eric, 2026-11-04): when an achievement comes up on screen,
   show the line for what the player actually did to earn it (its condition, as the achievements menu words
   it), not only its name and reward ("Achievement - First Blood / Spell recovered - Arcane Explosion"). Find
   the banner (`Node2DGame.RecordAchievementNow` and the banner it shows, `AchievementDefinition`'s text),
   keep it to the lines a phone can hold, and check it at both canvases over a run.
8. **[MOSTLY BUILT 2026-11-07, merged for 0.19.0: honest percentages from the cast's own getters, close/far
   words, the Reliquary Key's exact line, the dropped verbs. Left: Legendary->Empowered, the Gimmel Ring's slot
   wording, the chip shimmer, the tag lines, and the Archmage's phone spell line.]**
   **Card and item wording** (Eric, 2026-11-04):
   - **The Reliquary Key:** its description should just say that your pickup radius applies to chests (what
     `feat/john-dee-chests` made it do), nothing longer. `ChestItemCatalog`.
   - **Rename Legendary to Empowered, and make it obvious** (Eric, 2026-11-04). The random chance for a new spell
     to come stronger (2%, up to 10% with luck; `Player.GetLegendaryChance`) gets a different word, such as
     **Empowered**, everywhere the player reads it. Today it is only a star after the name ("Solar Flare ★",
     where it rolls in `Player`) and a gold tint, and the star is easy to read as one of the screen's other
     stars (Ascension, the Codex). Put the word in the name ("Empowered Solar Flare"), or a gold word and frame
     on its card, and say once what it means (its element tags count double). Rename what the player sees,
     not the code or the save: `IsLegendary` and `IsLegendaryStart` stay, since a save and `.tres` files carry
     them. Search the player-facing text for "Legendary" (cards, the Codex, the character select's
     "starts Legendary", achievements, the docs' player words). Check the level-up card, the loadout strip, the
     pause screen and the character select for a wizard who starts Empowered.
   - **"Leaves are whispering, though there is no wind."** (Eric, 2026-11-04): the grove's hint line reads
     "Leaves whispering, though there is no wind." (`StageHintCatalog.cs` line 137). While there, read every
     hint and chapter line for the same dropped verb.
   - **The Gimmel Ring set is confusing** (Eric, 2026-11-04: "highest level spells? What happens when all same
     level?"). How it works (`Player.ItemRules.cs`, `RebuildPair`): the two equipped spells with the highest
     level, a Conjunction always counting highest, and on a tie the EARLIER SLOT wins - so with every spell at
     one level it is the first two slots. Nothing shows which two.
     **Eric's change:** make it simply **the first two slots**, and highlight those two slots wherever the
     loadout is shown (the HUD strip, the pause screen, the level-up and chest screens' loadout) while the set or
     any of its pieces is held. Every Gimmel card then says "your first two spell slots". `RebuildPair` becomes
     slots 0 and 1, with no ranking. Check what the first slot holds: usually the wizard's own spell. It is a
     damage bonus only, which moves no level, so the Apotheosis is not pulled earlier, but confirm it. Also
     check how a fused Conjunction or a cut spell shifts the slots. The Betrothal Hoop leans the other way (the
     lowest-level spell): check its card reads as clearly beside the new rule.
   - **No distances in numbers** (Eric, 2026-11-04): the Singed Lock says "within 90". Never show a distance as a
     number; say close or far ("nearby", "close to you", "far away") and keep the numbers out of the player's
     sight. Search every player-facing text for pixel distances: item and boon descriptions
     (`ChestItemCatalog`, `BoonCatalog`), spell cards and level-up lines (`SpellEvolutionCatalog`,
     `KeywordText`), relics, element tier text, tooltips. If a validator can catch a number followed by a
     distance word, add it so they cannot come back.
   - **Percentages, never flat numbers** (Eric, 2026-11-04: instead of things like "damage +2", always speak in
     percentages). Every player-facing increase becomes a percentage: the level-up lines a spell's
     `SpellLevelUpgrade` produces (`DamageBonus = 2` reads "+2 damage"; it should read "+17% damage" against
     what the spell does at that level), and the same for boons, relics and anything else that prints a flat
     gain. Decide what the percentage is of (the spell's damage before the pick is the honest one) and do it in
     the one place the lines are built (`Player.EnsureRelevantLevelUps` and the card builders), not string by
     string. `RegressionChecks.ValidateLevelUpReaders` reads those lines; keep it passing.
   - **Clickable element tags look clickable** (Eric, 2026-11-04): give every tag chip that opens its
     element's card (`ElementChip`, `CodexRows.BuildChip`) a small animation that says so, such as a shimmer
     passing over it now and then. Keep it subtle and cheap: many chips can be on one screen, it must not pull
     the eye from the cards' text, and it must still read on a phone (`.ai/level-up-screen.md` section 4a's
     contrast rules).
   - **Item tags on a line of their own** (Eric, 2026-11-04): on an item set's card, the tags it adds go on
     their own line at the bottom, not split across the text the way the Thorned Buckler's "Metal / Earth"
     breaks on a phone. `ChestItemSelectionMenu` and the set rows (`CodexRows`); check at 540x1212
     `--portrait --safe-area` with `_UiShot --open-chest`, and at 1280x720.
9. **Bring back some defensive spells, on Aegis Ward's rules** (Eric, 2026-11-04). The passive defensive spells
   (Thornmail, Stormguard, Frozen Bulwark and the rest) were removed; Aegis Ward stayed because it is an active
   thing - a cooldown and a break, costing a slot like any other spell - not a number sitting on the player
   (`UnlockCatalog`'s note at `aegis_ward`). Any defensive spell that comes back must be the same: a real spell
   in a slot, with a visible cooldown and a way to fail or break, its own aim, cadence and demand
   (`.ai/spell-roster.md`), the element-tag rules, a `SpellData` `.tres` and the new-spell recipe in
   `CLAUDE.md`, and one achievement to earn it (every non-starter spell comes from exactly one). Design first:
   which old passives' ideas are worth keeping as actives, and how each differs from Aegis Ward and from each
   other (`.ai/passives-and-items.md` has why the passives went, `.ai/spell-variety.md` the axes).
   - **Built, 2026-11-07** (`.ai/spell-roster.md` "The defensive returns"): two spells, both on Aegis Ward's
     no-scene shape (a code-made arena node, the spell's state on Player's pipeline). **Stormguard**
     (Lightning+Water, the old aura's idea): the cooldown gathers a storm, and three blows taken - counted
     pre-mitigation beside the reactive boons - break it as a thunderclap, shove and slowing rain; it fails
     while regathering and over a player nobody touches. **Gloam Veil** (Darkness+Wind, Blur's idea): swallows
     the next hit whole, whatever its size, like a dodge (no signal, no reactions); the tear's gust shoves,
     lashes and carries the wizard free, and the veil is RE-WOVEN BY WALKING (its Range is the walk, worded as
     a share, never a distance); it fails standing still, and a touch spends it as fully as a boss blow. Each
     has five milestones of its own (both halves deepened, `ValidateElementPaths`), ten generated evolution
     icons and two spell icons, and each is paid by a new element-adept Feat in the five's exact shape:
     **Lightning Adept** and **Darkness Adept** (no save change - `BestElementCount` already carries both).
     Verified: build 0 errors; full validator 0 errors, the Test Wizard warning only; harness runs opening on
     each (storm: 7 breaks from 22 blows, 93 clap hits; veil: 4 hits swallowed, 3 walked re-weaves, 30 gust
     hits); the level-4 screen photographed at 1280x720. Not yet verified: no eyes-on crowd check of either
     effect in a real windowed run, no phone-canvas pass, numbers are first guesses, and the two adept feats
     have not been earned in real play.
9a. **Play with spell slots** (Eric, 2026-11-04; the first is built 2026-11-09: Canticle's Harmony takes the element of the spell in the slot after Canticle's, marked on the strip, `.ai/spell-roster.md`): items and boons that empower particular slots (the first,
   the last, the slot beside another) and ways to switch which spell sits in which slot during a run. The
   Gimmel Ring's move to "your first two slots" (item 8) is the first of these; design the family together
   so the slot is a thing the player arranges on purpose. Read `.ai/passives-and-items.md` (Full Set
   Enchantments, rarity), `.ai/spell-variety.md` and `.ai/slow-start.md` (a new save holds three slots,
   `SpellBook`) first; a slot effect must still read on the HUD strip at both canvases, with the slot it
   touches highlighted.
10. **Art and sound fixes** (Eric, 2026-11-04):
   - **Magic Missile's sound is annoying.** It plays on every dart, every 0.8 s, all run long for the Archmage,
     and `.ai/audio-direction.md` says a sound's loudness is set by how often it plays. Find which cast and
     impact sounds it uses (`SfxCatalog`, `SfxPlayer`), then make it quieter, softer and less sharp, with some
     variation between plays, in `tools/audio/sfx.py` (`python tools/audio/build.py`, then `analyse.py
     --confuse`). Listen to it in a run before and after.
   - **Minibosses glow, so they stand out in a crowd** (Eric, 2026-11-04). First check what already marks them
     (elites' gold markers; `Enemy.IsMiniBoss`; the Boo) and how they read inside a 140-body swarm. Then add a
     glow or rim: cheap per body (`.ai/performance.md`), above the crowd and still lit under the light pool
     (`.ai/light-pool.md`), in a colour that is not the player's warm gold or his dark wizard's violet
     (`.ai/world-and-tone.md`'s enemy palette), and distinct from the bosses' own look.
   - **Aedan's lantern hangs from his staff**, not sitting on top of it: a hook or crook at the staff's head,
     the lantern swinging below it on its bail. `tools/art/roster.py`, `pyromancer()`; regenerate his sheets and
     the prologue panels that show him, keep his spell anchor on the lantern (`CharacterVisuals`), and check the
     select, a run and the prologue at both canvases.
11. **More large landmarks unique to every map** (`.ai/genre-lessons.md` 3; expanded by Eric,
   2026-11-05), after the foliage pass. Each map should have its own distinctive large scenery,
   giving the landscape places to notice and recognize. Eric's examples: **large stones with
   mysterious, unidentifiable carvings, stone circles, and much larger fairy circles**. Give the
   features substantial scale and a presence in the landscape; design a set suited to each chapter's
   own terrain and atmosphere. The specific landmarks per map remain to be chosen.
   - Build on the scenery-only landmark plan: these can enrich the world without every feature
     needing to be an unlock, a puzzle or a reward. Keep actual secret/event clues readable among them.
   - Check existing art and generators first (`.ai/art-inventory.md`); draw/generate larger features
     at their intended scale under `.ai/art-direction.md`. Place them through the terrain/landmark
     system and respect `.ai/terrain.md`'s footing and clearance rules so a landmark does not trap
     the player or obstruct an event. Review their scale, variety and navigation in PC and phone runs.
12. **The Play Store listing** as *The One He Missed: Survivors* (Eric's Play Console; the copy is in the
   launch-plan doc, now carrying the rival motive).
13. **Housekeeping:**
   - the README's image and sound counts are stale again after the foliage and Paracelsus (recount from the
     tree, same methods);
   - delete the dead decor cases in `Node2DGame.BuildDecorProps` for every chapter that now has foliage (the
     recipe leaves that to the integrator);
   - the Heath's thorns are mirrored both ways (shared sprite mirroring) and its granite blob keeps the slab
     tile (`tiles.py`, shared);
   - on this PC's Python 2.7, `build.py` and the icon generators rewrite 43-87 files the committed versions
     differ from: find out which interpreter wrote the committed ones and settle it;
   - five old folders beside the repo that git no longer lists as worktrees (`ws-fix-touch-input`,
     `ws-wip-combo-sagittarius`, `ws-wip-familiar-icons`, `ws-wip-progress-reset`, `ws-wip-spell-visuals`):
     Eric's call whether to delete.

14. **Optional cross-device saves: Android and Windows** (Eric, 2026-11-05; discussion captured for next
   week, provider and implementation still to be decided).
   - **Goal:** the same player's permanent progress follows them between the Android app and the
     downloadable Windows build. Both already use `SaveData` and `SaveManager`'s `user://savegame.json`.
     Proposed first scope: unlocks, currency, upgrades, achievements, campaign progress and lessons,
     synced between runs. Resuming halfway through a run is a separate, larger feature. Keep device
     settings local (`SaveData.SettingsKeys`).
   - **Identity:** automatic cloud saves need shared player identity and online storage. A managed
     sign-in service can handle accounts; players could enable cloud saves optionally and keep playing
     offline. Device pairing codes are another option, with account recovery to design. Manual save
     export/import is the smaller alternative and needs no cloud account or hosting bill.
   - **Protect progress:** keep local saves and backups, add save revisions and detect conflicts when
     both devices were played offline. Initially offer a clear choice of conflicting saves, preserving
     both copies. Do not silently merge currency or purchases: spending on the phone must not be undone
     by an older PC upload. Handle schema migrations, different game versions, and `ProgressEpoch` so an
     older device cannot overwrite a newer save or revive progress from before a story reset.
   - **Costs checked 2026-11-05, not a promise of free hosting indefinitely:**
     [Supabase](https://supabase.com/pricing) has a $0 tier with 50,000 monthly active auth users, 500 MB
     database storage and 5 GB outgoing traffic; free projects pause after a week of inactivity. Pro
     starts at $25/month. [PlayFab](https://developer.microsoft.com/en-us/games/products/playfab/pricing/)
     has a $0 fixed monthly fee on pay-as-you-go, with metered usage charges; its $99/month Standard
     subscription is optional. Neither provider has been selected or configured. Aim to fit a free tier
     initially, then estimate costs from actual save size, active players and sync frequency before
     committing to a service. Avoid uploading on every local save write; batch meaningful checkpoints.
   - **Rough engineering estimate from this discussion:** a few days for a prototype, around one to
     two weeks for a thoroughly tested implementation; subject to provider and sign-in integration.
   - **First stage built, 2026-11-07 (manual export/import; no accounts, no service):** Options has
     a Save section - "Export save" copies the save file where the player chooses, "Import save"
     reads one back (`SaveTransfer`, native file pickers via `FileDialog.UseNativeDialog`; Android's
     SAF picker arrived with Godot 4.5 and still needs a device test). Import inspects the file
     first - not a save, a newer build's schema or epoch, or an earlier story epoch are refused in
     player words before any confirm - then `SaveImportConfirm` shows the file against this device
     (playtime, victories, wizards, chapters, achievements, Arcane Energy), selection resting on
     "Keep my save". A confirmed import keeps the replaced save beside the new one
     (`savegame.before-import.json`, never overwritten), writes the file's progress with THIS
     device's settings (`SaveData.SettingsKeys` stay local), reloads through `SaveManager.LoadFrom`
     so an older file migrates exactly as a local save would, and rebuilds the menu. Export reads
     the save FILE, and both buttons sit out a `SaveManager.WritesDisabled` launch, so a tool run's
     fabricated save is never exportable. `RegressionChecks.ValidateSaveTransfer` proves the whole
     disk roundtrip against scratch files; `--save-file=<path>` (SaveManager) points a windowed run
     at a throwaway save for rehearsals; `_UiShot --import-confirm` photographs the confirm screen.
     Still owed: one hands-on pass through the Windows file dialogs, and the Android device test.
     The cloud sync above (accounts, providers, conflicts) remains open.

15. **Opening cinematic: the invading army must rush over the horizon** (Eric, 2026-11-05).
   **Built, 2026-11-07** (`.ai/prologue.md`): the tide is the prologue's third animated panel
   (`TIDE_BOX`, 45 ticks at 8 fps over a 5.6 s hold). The slope starts empty under the lit village;
   heads come up BEHIND the crest, clipped by the ridge line; the wave bursts over it at 1.4 s with
   the panel's new impact beat (cold speed lines, a shake, his violet - the one impact that is his);
   the flood pours down the slope, every body on its own lane with contact shadows, dust and stride
   bob, growing with perspective; the seven-strong front rank strides up by the village as the panel
   ends, and the final frame is the old still. Back and mid now drift as one, so the crowd never
   slides on its ground (the old bug: the whole army was one layer drifting across a still picture).
   Bodies render once per (kind, size, seed, light) and are pasted along their lanes; the atlas is
   37 unique frames, 446 KB on disk, ~23 MB while the panel is up. Build, import and full validator
   passed (0 errors, the test_wizard warning); PC/portrait screenshots taken. **The moving rush at
   full speed still needs Eric's eyes** - fps, burst timing and whether the shake lands.
   Original ask: the army looked as if its silhouettes hovered above a picture; rework the `tide`
   beat so the bodies belong to the landscape and advance from beyond the crest onto the ground.
16. **Opening cinematic: give the road toward the dark tower more landmarks** (Eric, 2026-11-05).
   **Built, 2026-11-07** (`.ai/prologue.md`): the road now passes the places the campaign visits, in
   campaign order, nearest first, so the pan reads as the whole game ahead (the world map's places
   were the vocabulary): the west wood's near boughs flanking where the road leaves the frame, the
   Fallow Fields' three hedged strips and empty barn, the Broken Circle's ring on its knoll (eight
   stones and the walker's gap, as the map draws it), the scarp over the Sunken Cave's mouth, the
   Blighted Swamp's cold pools and dead snags, the Mystic Ruins' fallen colonnade before their keep,
   the Frozen Waste's pale peaks, the Scorched Sands' crescent dunes, and the Emberdeep's two black
   basalt bluffs that the road threads just under the tower. All silhouette and stone on the back
   layer (nothing slides on the ground; only the wood's near trees ride the fore's parallax), no lit
   window among them - the player's pool stays the road's only warm light. Everything but the dunes
   and far keep sits inside the 360-wide square the phone shows. The hold grew to 5.4 s so the eye
   can walk the road. Build, import and full validator passed (0 errors, the test_wizard warning);
   PC/portrait screenshots taken. **Composition still needs Eric's eyes** - whether the landmarks
   crowd the road and whether each reads as its chapter at x2.
   Original ask: fill the big, flat, empty plain around the road with landmarks, keeping the wizard,
   the route and the distant tower readable; `five` and `road` and their transition inspected.

17. ~~**Shadow Bolt should become a combination spell**~~ (Eric, 2026-11-05).
   **Done, 2026-11-07** (`.ai/combo-spells.md` section 7): Shadow Bolt is the third Conjunction -
   Void Lance at 8 down Cosmic Collapse + Contagion at 8 down Pandemic, verb *fester*. A bolt of
   void leaps the thickest pack laying the curse (poisoned, slowed, exposed, the old identity); on
   Pandemic's beat every cursed body caves in and the curse spreads, all of it bounded (no enemy
   twice a cast, 24 cursed at once, one hit an enemy a beat). New `scripts/ShadowBolt.cs` draws it
   (SpellStrokes arcs, feet rings with rot motes that swell on the beat, imploding void discs); the
   old five-card chain kit left the catalog, `castle_conqueror` pays only Bone Rattle, schema 23
   keeps the spell for every save that owned it (`MigrateShadowBoltToConjunction`), and
   `ValidateElementReachability` holds the Darkness/Poison floor after the tags left the pool.
   Numbers are first guesses against section 2's 1.2x target - the harness band
   (`--seek-combo=shadow_bolt`) and an eyes-on crowd check at both canvases remain. Original ask:
   move Shadow Bolt into the Conjunction roster; parents and recipe to be chosen; as one piece:
   - **Rebalance element-tag availability** around the change. Account for its tags moving out of the
     ordinary spell pool, and check that builds can still reach the required element tiers and
     Ascensions before a combination is available. Review spells, boons and items together.
   - **Scale Shadow Bolt's power** for its new role and the requirements to obtain it, balancing it
     against the other combination spells.
   - **Make its visuals much more interesting**, with a distinct combination-spell identity and a
     payoff that matches the other combinations. Review the moving effect in a crowded run at PC and
     phone sizes. Update the generators/code that produce the art, not generated PNGs by hand.
   Start with `.ai/combo-spells.md`, `.ai/gameplay-design.md`, `.ai/spell-roster.md` and
   `.ai/art-direction.md`; include the existing unlock and save compatibility in the conversion plan.

18. ~~**Defeat screen runs off the bottom on Windows**~~ (Eric, 2026-11-05).
   **Done, 2026-11-06:** reproduced the unbounded results layout. Header, summary and expanded
   details now share a bounded scroll area above a pinned Retry / Main menu footer. Resizing
   rebuilds the responsive layout while preserving the open detail; very narrow canvases stack
   the footer buttons and summary tiles. `_RecoveryProbe --part=results` passed crowded results,
   scrolling and footer bounds at 1280x720, portrait with safe areas, and a 480x720 logical canvas.
   Resolution/monitor settings remain a separate task in item 20. Original report: the defeat/results screen
   trails below the bottom of his screen. Reproduce in `GameOverScreen` at the Windows window sizes,
   including fullscreen and a smaller window, with enough results/unlocks to exercise the tall case.
   Keep the content reachable and the Retry / Main menu footer on screen; review the portrait layout
   as well. Check with `_UiShot --force-death` and a real defeat. Read `.ai/level-up-screen.md` and
   `.ai/controller-and-focus.md` before changing the layout or its scrolling.
   - **Resolution/scaling investigation (Eric, 2026-11-05):** the resolution appears incorrect to him.
     Record the selected monitor, desktop resolution, actual window size, logical viewport size and
     Windows DPI scaling when reproducing. Code inspection suggests a possible layout issue as well:
     `DisplayProfile` already requests borderless fullscreen; the PC design canvas is 1280x720 with
     `KeepHeight`. `GameOverScreen.BuildShell` centres a folded panel at its content height, with no
     overall scroll/height cap around the summary and footer. A minimum height exceeding the available
     canvas could push the bottom out of view. This layout failure is now reproduced and fixed;
     a separate Windows DPI/monitor problem has not been established.
19. ~~**Defeat screen: Right Arrow from Retry does not reach Main menu**~~ (Eric, 2026-11-05).
   **Done, 2026-11-05:** `GameOverScreen.BuildFooter` explicitly links Retry's right neighbor to
   Main menu and Main menu's left neighbor to Retry. `_RecoveryProbe --part=footer` checks real
   keyboard/pad input while paused, in landscape and portrait; removing the links with
   `--automatic-focus` reproduces the stuck selection. Item 18's overflow fix followed on 2026-11-06.

20. **Display options and correct fullscreen startup resolution** (Eric, 2026-11-05). Add resolution
   options to the game and detect the correct display resolution when launching fullscreen; investigate
   alongside the defeat-screen overflow in item 18.
   **Core implemented 2026-11-06 (item 46):** Eric chose native fullscreen plus selectable window sizes.
   Saved mode/window-size controls, display fitting, non-16:9 expansion and the aligned menu footer
   are built. Native 2736x1824 fullscreen and 16:9/16:10/4:3/ultrawide windows were checked.
   The broader explicit monitor picker, timed-revert UI and separate OS-DPI sweeps below remain
   follow-up work; the current menu uses the monitor containing its window.
   **Monitor picker and timed Keep / Revert built 2026-11-07:** a Monitor dropdown in Options'
   Display section, shown only with more than one monitor ("Automatic (current monitor)" is -1,
   the OS's own placement, which is what every save had); `SaveData.MonitorIndex` is a settings
   key, no schema bump, applied at startup *before* fullscreen (fullscreen binds to the window's
   current screen) and ignored when stale. Every change made from those controls - mode, window
   size, monitor - now applies and then opens `DisplayConfirm`, a 10-second Keep / Revert step
   over the menu: Keep closes it; Revert, B/Escape or the clock running out restores the prior
   mode, size and screen (`DisplayProfile.Snapshot`/`Restore`), so a change that leaves the screen
   unreadable undoes itself. F11/Alt+Enter stays immediate. `_UiShot --fake-monitors=N`
   photographs the row on a one-monitor machine and `--show-display-confirm` the step. Still
   hardware checks only a real desk can run: the OS-DPI sweeps (100/125/150%) and a real second
   monitor (this machine has one).
   - Offer windowed and borderless fullscreen modes, resolution/window-size choices, and an
     **Automatic / native display** option. Include monitor selection where multiple monitors exist.
     Start fullscreen at the selected monitor's desktop resolution by default, and remember the
     player's chosen mode and window size as device-local settings.
   - `DisplayProfile` currently requests fullscreen and has an F11 / Alt+Enter toggle, but has no saved
     resolution selector or monitor preference. Its 1600x900 window override is a windowed default;
     its 1280x720 `ContentScaleSize` is the UI's design canvas, not the physical monitor resolution.
     Borderless fullscreen already follows the monitor's desktop size. Keep physical resolution,
     logical UI size and any UI scaling option distinct.
   - Check DPI scaling, fullscreen transitions, monitor changes and window decorations/taskbar bounds.
     Clamp windowed sizes to the chosen monitor's usable area. Reflow menus after size/mode changes;
     changing resolution alone must not leave the defeat panel or its footer outside the viewport.
     Provide a timed Keep / Revert step when applying display changes.
   - Start with `scripts/DisplayProfile.cs`, `project.godot`, the shared Options rows and
     `ResponsiveLayout`. Preserve Android's portrait layout and the tool scenes' exact screenshot
     resolutions. Verify smaller Windows windows, native fullscreen, non-16:9 displays and Windows
     scaling at 100%, 125% and 150%; check the title, gameplay HUD and defeat screen.
   - Godot reference: [multiple resolutions](https://docs.godotengine.org/en/4.5/tutorials/rendering/multiple_resolutions.html)
     and [DisplayServer](https://docs.godotengine.org/en/4.5/classes/class_displayserver.html).

21. ~~**Use pages for crowded menus; start with Powers**~~ (Eric, 2026-11-05). For menus with many options,
   consider pages instead of one long scrollbar. In Powers, move the existing sections onto separate
   pages so a player can go straight to the category they want. Make each option more compact to
   conserve space: review padding, icon sizes and the arrangement of the description, cost and purchase
   controls together, while keeping the text readable and the touch targets usable. Keep page controls
   visible, show which page/category is selected, and support mouse, touch, keyboard and controller.
   Review PC and phone layouts; assess other long menus individually rather than replacing every list
   automatically. Start with `scripts/MainMenu.Shop.cs`, `.ai/level-up-screen.md`'s shop section and
   `.ai/controller-and-focus.md`.
   **Implemented for 0.14.0:** fixed category selector, Previous/Next and page count; two cards per
   page, smaller icons and padding, with the purse and Back outside the scroll.
   **0.14.2 follow-up (Eric, 2026-11-07):** move Previous/Next to the bottom and make them obvious:
   large equal-width Previous page / Next page controls and a gold page count; Back sits below on
   phones and alongside on desktop, leaving room for the cards.
   **Follow-up done** (49b522b0, 2026-11-07): the pager row sits under the cards outside the scroll -
   equal-width Previous page / Next page (ExpandFill, 200x72 minimum) around the gold page count
   (PurseInk) - and `LayoutShopPager` reparents Back into the row on desktop and below it on a
   phone. Eric confirmed the bottom page controls look right on the phone (item 47). Nothing of
   item 21 remains; the page strip pattern is `EnsureShopPager`/`ShowShopPage` in
   `MainMenu.Shop.cs` for the next crowded menu to copy.

22. **[2026-11-08: Eric asked to brainstorm it; down to **Arcana** (recommended - pairs with Arcane Energy, unused
   in the game's vocabulary) or **Talents**. Ruled out as already used: Attunement, Discipline, Kindling, Teaching,
   Insight. Awaiting his pick.]**
   **Rethink the name "Powers"** (Eric, 2026-11-05). He suggested something like "Powerups", but wants
   a more magic-themed name. The menu sells permanent upgrades and unlockable boons, so the name and a
   short explanation should make its purpose clear. No replacement has been selected. Candidates from
   this discussion: **Arcane Mastery**, **Arcane Enhancements**, or **Spellcraft**. Keep the naming
   distinct from Full Set Enchantments and the planned "Empowered" label for stronger spell rolls.
   When chosen, update the main-menu entry, shop heading, hints and any other player-facing references
   consistently; coordinate the wording with item 21's redesign.

23. ~~**Show the protection after a level-up selection with a small shield animation**~~ (Eric,
   2026-11-05). When the player chooses a level-up and the run resumes, briefly form a small magical
   shield around the wizard to show that he cannot be hurt yet. Animate it away when that protection
   ends, so its visible lifetime matches the actual invulnerability rather than an independent timer.
   The existing protection is `Player.PostMenuInvincibilitySeconds` (currently 2 seconds), granted in
   `Node2DGame` through `GrantInvincibility(..., whiten: false)`; do not confuse it with the separate
   post-level-up spawn grace. Keep the effect small, readable amid spells, and distinct from Aegis
   Ward so it does not imply the player gained a shield spell. Check the animation at PC and phone
   sizes, following `.ai/art-direction.md` and `.ai/performance.md`. This is a visual cue for the
   existing protection, with its duration and gameplay behavior preserved.
   **Done, 2026-11-07:** `PostPickWard`, a code-created Node2D child of the player, spawned at both
   quiet-grace sites (level-up close and chest close in `Node2DGame`). A thin violet ring on the
   Arcane ramp, radius 22 against Aegis Ward's 52 dome, drawn with `PixelInk` on the 2 px grid at
   `ForemostAir`, forming over 0.3 s with two orbiting glints. It reads
   `Player.InvincibilitySecondsRemaining` every frame - no clock of its own - so its dissolve ends
   on the frame the grace does, and a chest straight after a level-up extends the one ward rather
   than stacking a second. Photographed at both canvases through `--force-levelup` (the forced
   level-up's auto-dismissal resumes the run wearing it). Duration and gameplay unchanged.

24. ~~**Spell descriptions are clipped below their containers on Windows**~~ (Eric, 2026-11-05).
   **Done, 2026-11-06:** reproduced tall level-up cards overflowing the wide layout. Wide cards
   now use the same bounded options scroll as portrait cards, keeping the action footer visible.
   A tall four-card deal now scrolls 469 units of content inside a 400-unit band at 1280x720;
   mouse wheel, controller scrolling and Page Up / Page Down reach the lower text. Existing
   Codex/pause detail scrolls remain in place. Display settings/DPI investigation stays in item 20.
   Original report:
   Some descriptions extend below the bottom of their container, leaving the lower text unreadable.
   The exact spell(s) and menu have not been identified yet; inspect the level-up cards, spell/Codex
   details, pause-screen spell details and character-selection spell descriptions when reproducing.
   Check text wrapping and minimum heights, fixed-height parents, clipping, scroll reach and layout
   updates after resizing. Make the whole description readable/reachable while preserving the text's
   legibility. Investigate alongside the defeat-screen overflow (18) and resolution/display options
   (20); a shared resolution/DPI cause is suspected by Eric but remains unconfirmed. Photograph long
   descriptions in Windows fullscreen and smaller windows at different DPI scales, then check portrait.

25. **Add straightforward stat-boost items; simplify confusing effects where it fits** (Eric,
   2026-11-05). The item pool should include basic, immediately understandable bonuses such as
   **"Area +X%"**. Other possible examples are damage, movement speed or pickup radius increases;
   choose the stats and values during the design/balance pass. Review existing confusing item effects
   and replace them with simple stat increases when that better suits the item. Eric is open to
   repurposing existing items, so this can improve the current pool as well as add new choices.
   Keep the percentage wording clear and consistent with item 8, preserve coherent item/set identities,
   and rebalance the bonus for its rarity and stacking behavior. Start with `ChestItemCatalog`,
   `Player.ItemRules.cs`, `.ai/passives-and-items.md` and `.ai/spell-variety.md`; keep the cards, Codex
   descriptions and actual stat effects in agreement. Specific replacements remain to be chosen.

26. ~~**"Freezing" mouse-hover tooltip is extremely wide and shallow**~~ (Eric, 2026-11-05).
   **Done, 2026-11-05:** the native status tooltip has a 420-unit card width, constrained to the
   viewport; its wrapped sentence gets its width before the popup's first size measurement, avoiding
   an oversized initial height too. All eight status hover/click cards passed `_RecoveryProbe` on
   PC; Freezing also passed and was visually checked in portrait. The focused-card notes are unchanged.

27. ~~**Keyboard input hides the mouse and gives the keyboard control until the mouse is used again**~~
   (Eric, 2026-11-05). When playing/navigating with the keyboard, hide the cursor and deactivate mouse
   hover/selection behavior so a stationary pointer cannot steal the selection, highlight another
   option or leave a hover tooltip over the keyboard's choice. Restore the cursor and normal mouse
   interaction when the player intentionally moves or clicks the mouse again; ignore small jitter.
   This is an in-game input-mode switch, not disabling the operating system's mouse.
   `CursorVisibility` currently tracks controller input and mouse movement/clicks but has no keyboard
   case. Coordinate it with `MenuNavigator` and `MenuFocusHighlight.PointerActive` so cursor visibility,
   focus highlights, hover effects and tooltips agree about the active input. Preserve controller
   behavior, keyboard text entry and Android touch controls. Test keyboard -> mouse -> keyboard on
   main-menu lists, level-up/chest choices, the defeat footer and status tooltips (including item 26).

   **Implemented for 0.14.0:** shared keyboard/controller/pointer state, cursor hiding, stale-hover
   and tooltip suppression, deliberate movement/click/touch recovery, and dynamic-control handling.
   **Verified complete 2026-11-07:** `CursorVisibility.ObserveInput` covers the keyboard case
   (`InputEventKey` beside pad input hides the pointer; a 3 px mouse move, click or touch restores
   it), filters are restored per control, and `.ai/controller-and-focus.md` records the behaviour.
   Nothing of item 27 remains.

28. **Magic Missile should fly behind a boss's shield; explore other boss weak-spot interactions**
   (Eric, 2026-11-05). Against the boss that raises a shield, Magic Missile should travel all the way
   around and behind it to reach him. Make the route and rear hit visible so the spell's seeking
   behavior has a satisfying interaction with the defense. The Hayward's frontal hurdle in the
   Fallow Fields appears to match this report; confirm the boss when reproducing.
   - **The damage rule matters as well as the flight path:** `HaywardBoss.AdjustIncomingDamage`
     currently reduces hits according to `PlayerInFront`, using the player's position rather than
     where a projectile arrives from. Curving the missile behind him alone would still leave its
     damage guarded while the player stands in front. Design the incoming-hit/guard interaction so
     actually reaching the exposed side counts, and review the effect on the boss's circling mechanic.
   - **Review the other bosses' weak spots** for similarly fun Magic Missile interactions. Consider
     how it finds and reaches an exposed target, and make those choices readable in the missile's
     motion. Choose interactions per boss, with deliberate handling of their phases and targetability.
     Parent/branch-specific behavior and any balance changes remain to be designed.
   Start with `MagicMissile.cs`, `HaywardBoss.cs`, the incoming-damage hook in `Enemy.cs`,
   `.ai/spell-roster.md` and `.ai/enemy-behaviour.md`. Test actual damage from different player and
   projectile positions, alongside the visible trajectory, at PC and phone sizes.
   **Built 2026-11-07 (`claude/nostalgic-lalande-4db33d`, with item 57), the Hayward first:**
   - **The damage rule:** `Enemy` carries a one-hit origin channel (`NoteIncomingHitOrigin` →
     `IncomingHitFrom`, armed and cleared per hit so nothing stale leaks onto the next);
     `Player.DealDamageToEnemy` takes an optional `hitFrom` and arms it. The Hayward's
     `AdjustIncomingDamage` judges a hit by its arrival when the dealer named one, else by the
     player's position exactly as before - so circling is unchanged for the auto-aimed roster,
     and only a projectile that genuinely reached the open side is paid for it.
   - **The flight:** `IGuardedBoss` (facing, half-angle, `GuardCovers`) is the read Magic Missile
     steers by (`FlankAim`): inside the guarded front plus an 18 deg margin the dart chases a
     waypoint stepped 55 deg the shorter way round a 120 px ring, then dives in - the visible bend
     round the hurdle. Its lock is not dropped at range mid-detour (duration still bounds it), and
     every Magic Missile hit (dart, splash, grind, orb burst) names its origin.
   - **Still open:** the other bosses' weak spots (the item's second half) - which fights want a
     Magic Missile interaction of their own, per-phase handling, and any Ascension-specific
     behaviour - remain to be designed; nothing but the Hayward implements `IGuardedBoss` yet.
     Damage from different positions verified by the rule's construction and the probe's drain
     (fallback path); the visible trajectory at PC and phone sizes still needs eyes.

29. **[Decided 2026-11-08 (Eric): "remove it, just make sure the numbers are still visible" - no background, an
   outline or shadow on the digits instead, checked over bright and dark chapters on both canvases.]**
   **Reconsider the top timer's plain black background** (Eric, 2026-11-05). Explore styling the
   background to fit the game's art, or removing it entirely. This is a visual decision to consider;
   neither option has been selected. Compare both at play size over bright and dark chapters and a
   crowded fight, on PC and phone, keeping the timer readable and clear of the surrounding HUD.
   Start with `RunTimerPanel` / `runTimerStyle` in `scripts/Node2DGame.cs`, `.ai/art-direction.md` and
   the HUD/contrast rules in `.ai/level-up-screen.md`.

30. **Magic Missile and other projectile attacks render above the player sprite** (Eric,
   2026-11-05). Give projectile visuals priority over the player character so they remain visible
   when crossing or overlapping his sprite. This revises the earlier rule that the player always
   draws above spells; update the implementation and the corresponding draw-layer documentation
   together. `DrawLayers` currently puts ordinary airborne effects at `Air` (6), below the player
   and boss bodies sharing `Foremost` (10). Review the layer split and projectile scene/script Z
   settings rather than raising only Magic Missile. The request concerns projectiles above the
   player; review boss layering explicitly when separating the currently shared body layer.
   Start with `scripts/DrawLayers.cs`, `MagicMissile.cs`, the other projectile visuals and
   `.ai/art-direction.md` / `.ai/godot-engine.md`. Check casts and projectiles crossing the wizard
   in motion and in a crowded fight, at PC and phone sizes.

31. ~~**Taproot wording: "Stand still"**~~ (Eric, 2026-11-05). **Done, 2026-11-05:** both
   `BoonCatalog` text builders omit the activation duration. The card says "Stand still: +1.5 HP
   per second"; the underlying 1.5-second activation delay and regeneration are unchanged.

32. **Walking hut footprints face backward in every direction** (Eric, 2026-11-05; clarified after
   the initial report). The chicken toes point opposite the hut's direction of travel however it is
   running; northbound tracks pointing south were one example. Check the artwork's authored forward axis
   against the placement rotation/mirroring and the hut's actual direction of travel. Make the tracks
   point the way it walked; verify north, south, east, west and turns in the moving sequence. Start
   with `scripts/WalkingHutEvent.cs` and `tools/art/walking_hut.py`; change the generator if the art
   needs correcting, then regenerate. Keep the footprints useful as a trail players can follow.

33. **Unlocked wizard's sprite must draw in front of the shine** (Eric, 2026-11-05). Unlocking a
   character currently puts a shining light over their sprite. Draw the wizard above that light so
   the character remains clearly visible, with the celebratory shine behind them. Locate the actual
   unlock presentation and check its sprite/effect draw order or Z layers; if the overlap is in a
   generated freeing panel, fix the generator's layer order and regenerate. Review `Freeings`,
   `Prologue`, `.ai/cutscenes.md` and the relevant unlock/rescue effect as needed, then check the
   animation with several wizard sprites at PC and phone sizes.

34. ~~**The Archmage's passive should give time to escape after it triggers**~~ (Eric, 2026-11-05;
   called "archwizard" in the playtest note). **Done, 2026-11-05:** Overlooked grants **10 seconds**
   of invincibility, with the existing white visual lasting through it. The character description
   and `.ai/protagonist.md` explain it; post-menu grace stays two seconds. `_RecoveryProbe
   --part=overlooked` verified the killing blow is refused without healing, immunity at 9.8 seconds,
   protection and visual expiry after 10 seconds, resumed damage and the once-per-run limit.
   Eric can now playtest whether ten seconds feels right for escaping a crowded situation.

35. ~~**"Newly unlocked" icons cannot be inspected on Windows**~~ (Eric, 2026-11-05).
   **Done, 2026-11-06:** reward icons are focusable buttons opening a named detail with the actual
   unlock's description (including the earned Ascension branch or Apotheosis form). Back returns
   focus to its icon; More opens the complete reward list. Mouse, keyboard, controller and injected
   touch passed, including 24 rewards, wrapped icons, portrait and pinned footer navigation.
   Original report: he cannot click
   the icons or reach them with the arrow keys to find out what they are. Make each reward icon a
   usable selection: clicking it, or focusing it with keyboard arrows and activating it, should show
   the unlocked thing's name and description. Give a focused icon a clear highlight and support
   controller selection/activation and touch as well.
   Start with `GameOverScreen.BuildUnlockStrip`, its icon wells and the corresponding `RunUnlockEntry`
   / `BuildUnlockCard` detail data. Check mouse filters, focus stops, focus neighbors and input routing;
   the current strip is an `HFlowContainer` with `MouseFilter.Ignore`. Keep Retry and Main menu reachable
   when moving through the reward icons (items 18-19), and coordinate with the keyboard/mouse switch
   in item 27. Test a results screen with several different newly unlocked rewards, including an
   overflowing/wrapped icon row, at Windows and phone layouts.

36. ~~**Quitting a run should still show its results and unlock celebrations**~~ (Eric, 2026-11-05).
   **Done, 2026-11-06:** pause's **Quit Run** now uses the guarded run-finishing path with a
   distinct `Quit` outcome and neutral **Run ended** title. It banks rewards once, plays owed
   character freeings and system lessons, then shows inspectable results. Existing Defeat/Victory
   enum values and story epoch 2 are preserved. Real rendered freeing + Ascension reels passed;
   headless checks passed both owed-reel and empty runs, including a repeated quit/payout check.
   Original request:
   When the player voluntarily quits a run, take them through the results/unlocks screen and any
   accompanying unlock animations before returning to the menu. Earned unlocks must remain visible
   and their presentation must not be skipped by the quit path. Check quitting after earning a
   character, familiar or system unlock, as well as a run with no new unlocks; coordinate with item
   35's inspectable rewards. This concerns leaving a run through the game's quit action.

37. **Unlocking characters teaches Ascension, Apotheosis and combination spells** (Eric,
   2026-11-05; corrected from the earlier familiar note). **Ascension should unlock from a character
   unlock:** in an animation, that newly unlocked character shares special magical knowledge with
   the player, explaining and unlocking Ascension. **Apotheosis and combination spells should each
   unlock through a similar character interaction.** Which characters teach each system and the
   details of their animations remain to be chosen. Familiar discovery is a separate event (item 38).
   Eric reports using GPT to change how familiar and Ascension unlocking works; inspect those
   changes before implementing this follow-up. Review the current unlock flow, `Teachings`,
   `Freeings`, `.ai/slow-start.md` and `.ai/cutscenes.md` together: move these system unlocks to the
   character interactions, update their hints/lesson text and keep the presentations consistent
   with the unlock rules. Play the owed animations after a run, including a voluntarily quit run
   (item 36).

38. ~~**Familiar discovery: subtle tracks and a larger magically sealed pen**~~ (Eric, 2026-11-05;
   refined 2026-11-06). **Done, 2026-11-06:** ground-level drag furrows, splayed paws and claw
   scratches suggest a dog resisted being dragged. They pulse faintly in opaque gold stops and
   begin outside the opening view (at least 1,488 world units from spawn). A bounded, one-off
   search lays the intermittent marks through reachable ground toward the actual pen, with tiny
   decor reserves to keep props off them. No traveling golden lights, near-spawn route or
   trail-triggered POI arrow remains at any of the three teaching landmarks. Unfound map hints
   describe the landscape without advertising an upgrade.
   - **Later polish (2026-11-06):** six varied mark families, more golden and irregularly spaced
     300-520 units along the route. Animated studies and actual-spacing captures are generated with
     `_DiscoveryPreview` and `tools/art/discovery_gif.py`.
   - The pen was enlarged to 448x352, then to **1944x1632** at Eric's request for at least 4x again.
     Its foundation clears water/hidden fences before painting and on regeneration. Twenty-four
       blue/violet wisps circle the walls, eight the carved center ward. It has a binding across its gate.
       **Updated 2026-11-06:** the center energy pickup is replaced by a drained, ghostly hound, cat and
       bird. They run around and try the walls. Defeat the existing guards, touch the latch, then walk up
       to each companion inside. Each regains color and dissolves toward the player; all three are
       required to unlock **Familiar and Laelaps**. Opening the gate or reaching the center pays nothing.
     Its interior/entrance are walkable in both physics and navigation, with separate draw origins
     keeping the floor behind the player. Repeat visits pay the existing chest, once.
   - Desktop and portrait/safe-area walkthroughs passed, with default terrain and seeds 17/2026,
     including first unlock, center approach, no premature grant, repeat chest, and retired tracks.
     The Star and Geode walkthroughs passed without a golden route. Full content validator:
     0 errors, the existing Test Wizard warning. Art palette audit passed. System teaching after a
     run stays intact; item 37's character-taught redesign remains separate.
     - **Wizard placement update (2026-11-06):** Laelaps moves from the Broken Circle's Coursing deed
       to this rescue. Coursing now asks Laelaps' hound to catch 100 Black Hares in a Circle run and
       pays 50 Arcane Energy. Medea's existing tithe-scale event moves from the Fields to the Circle;
       its hint refers to standing stones. Baba Yaga's hut stays in the Forest but is placed 6-8 chunks
       out, with mature woodland around at least half its surrounding ring. Existing ownership and
       completed deeds are retained; a previously completed Pinfold also reconciles Laelaps.
     - **Validation of the companion update:** build passed with the existing 45 warnings; full
       content validator returned 0 errors and its existing Test Wizard warning. Rendered portrait
       (seed 17) and desktop repeat (seed 2026) Pinfold walkthroughs passed: three separate rescues,
       no gate-only reward, Familiar + Laelaps on completion, exactly one repeat chest. Broken Circle
       seed 1234 completed both events and freed Medea (46 checks); Forest seeds 17/2026 passed
       wooded placement and the entire hut rescue (47 checks each). All saves stayed in memory.

39. **Clarify Familiar spell-card descriptions** (Eric, 2026-11-06). Familiar descriptions on the
   cards are confusing and sometimes read like run-on sentences. Rewrite them as short, clear
   sentences that distinguish the familiar's behavior from upgrade effects. Inspect the composed
   card text, not just the base description, at Windows and phone widths so appended effects do
   not turn back into a run-on sentence.

   **Implemented for 0.14.0:** shorter form descriptions/traits and mutation/Ascension descriptions;
   the hound card explicitly states that only Littermates adds pups.

40. **Littermates is the only way to gain multiple hound familiars** (Eric, 2026-11-06). A hound
   Familiar should otherwise remain one hound. **Littermates grants two additional hounds** (three
   total from the normal single hound); other upgrades/effects must not independently add hounds.
   Audit the hound's count sources, upgrade paths and stacking interactions, and rebalance its
   damage and other relevant stats around this rule. Keep the card wording explicit about the
   two additional hounds and coordinate with item 39's description cleanup. This is next-week
   work, now implemented for 0.14.0: the level-three second hound is removed. One hound takes over
   the former pair's damage budget from level three (48, rising to 88 at level eight).
   Littermates adds two pups and reduces every hound's bite damage by 55%: 1.35 times the single
   hound's total before overkill/travel, close to the former pack's budget. Crow/cat counts stay unchanged.

41. **Two starting boon slots, two purchasable slots, and levelable boons with fewer tags** (Eric,
   2026-11-06, clarified). Keep the initial limit at **two boons**, and let the player **buy up to
   two additional boon slots**, for a maximum of four. Reduce the number of element tags on each
   boon to compensate for the higher eventual capacity, and make **every boon level up** so boon
   upgrades diversify the level-up choice pool. Rebalance
   each boon's starting effect and per-level gains around those upgrades, together with the
   two-to-four-slot tag budget. Review slot purchases, offer weighting, level caps, full-slot behavior,
   saved loadouts and the boon/level-up UI at Windows and phone sizes. This is next-week design/balance work,
   not implemented in 0.13.2.

   **Built 2026-11-07** (`.ai/passives-and-items.md` 3d is the full design note). Slots:
   `Player.BaseBoonSlots` 2, `MaxBoonSlots` 4; the shop's **Reliquary Shelf** (Powers → Level-ups,
   150 then 300 AE) sells the third and fourth. Levels: picking an owned boon levels it, to
   `BoonCatalog.MaxLevel` 3; **level 2 of almost every boon is the old flat value** (Bone Rattle
   starts at it instead - a shorter fright reads as a flicker), Chill's per-level is negative
   (55% → 45% → 35% speed). Tags: one per boon, each keeping its FIRST tag so no icon moved;
   `ValidateElementReachability` still passes against the four-slot cap. Cards: a held boon's card
   is "Level up · Boon" with pips and the delta line; the pause screen names the level. Rarity
   untouched (one boon card at most, `BoonOfferChance` 0.22). **Schema 25**
   (`MigrateToBoonRework`, proved by `ValidateBoonSlotMigration`): a played save is granted both
   shop slots - its runs all held six. Verified: build 0 errors; validator 0 errors / the
   test_wizard warning; an 8-minute `--all-unlocked --immortal` bot filled both slots and levelled
   both boons to 2; `_UiShot` shots of the new-boon card, the level card and the phone layout all
   read. Not verified: a real played save's migration in-game, and feel at the four-slot endgame.

42. ~~**Fields terrain palette options**~~ (Eric, 2026-11-06). **Applied: E — Faded Gold.**
   Eric selected the original dark field with clearer furrows. Regenerated the 36 stubble and 36
   furrow tiles plus the shared atlas, keeping the original texture patterns, edge masks and layout.
   Palette generation, tile/atlas mask checks, in-game inspection and full content validation passed
   (0 errors, the existing Test Wizard warning).
   The tilled soil and dirt looked dark,
   boring and too similar. First studies A/B/C were too far from the original. Closer variants
   D/E/F keep the original fine stubble and row patterns: **D Soft Umber**, **E Faded Gold**
   (closest to the original, clearer furrows), **F Cool Loam** (cooler/desaturated). Compare with
   `python tools/art/fields_studies.py --closer` in `build/visuals/fields-options-close.png`.
   `terrain_fields.FIELD_LOOK` is now `faded_gold`; the other studies and original remain available.
   Regenerate only stubble and furrow with `python tools/art/tiles.py stubble furrow`. All palette stops
   are existing contract colours, including the already approved sand-cloth row reused as soil.

43. **Gods as extended-story unlocks** (Eric, 2026-11-06). Consider gods as the playable characters
    earned during the extended story. They can start with powerful **existing combination spells,
    Apotheosis forms, or other advanced spell forms**. This does **not require new spells**: reuse
    the current spell roster and its evolved forms, then balance each god's starting power, passive
    and unlock around that later story. This is a future design note, not part of the current Merlin swap.
    **Designed with item 53 (2026-11-07): `.ai/second-half.md`**, docs only, for Eric's review - six gods,
    each named in a freed wizard's legend, opening on a Conjunction at level 1 as the Dark Wizard does.

44. ~~**Merlin at the great meeting place**~~ (Eric, 2026-11-06). **Done.** Restore the Broken Circle's existing
    Moot wards to free Merlin and unlock his existing Orb of Ages, retaining Gilded Mote. Show him
    bound at the center and play his freeing reel afterward. Medea's granary belongs back in the
    Fields, alongside the separate Laelaps rescue. Keep the campaign-completion deed and its saved
    progress as The Road Restored, with a 1,000 Arcane Energy reward. Preserve existing ownership.

45. ~~**Main menu Quit and loading fixes**~~ (Eric, 2026-11-06). **Done.** Add a lower-right Quit button with
    mouse, keyboard and controller access. Fit the complete ward artwork on the loading screen
    (the portrait source itself was clipped at both sides). Break resource caching, shop cards and
    achievement-page construction into small main-thread steps so loading animation keeps drawing.
    - Build: 0 errors, the existing 45 warnings. Content validator: 0 errors, the existing Test Wizard
      warning. Circle seed 1234 freed Merlin (23 checks); Fields seed 2026 freed Medea (46 checks).
      Merlin's freeing art passed the palette audit with no off-contract pixels.
    - Real-window title probes passed desktop, portrait/safe-area and 4:3, including retry, return
      to the menu, early input suppression, all achievement filters, Quit navigation and actual exit.
      On this PC, the old 3,456 ms loading-frame gap fell to 97-109 ms in the final fresh-process
      checks; preparation took about 2.2 s. These are desktop measurements, not phone timings.
      Every loading frame's crop has padding on all four sides. All test saves stayed in memory.

46. ~~**Aligned bottom buttons, saved window sizes and native fullscreen without bars**~~ (Eric,
    2026-11-06). **Done.** Options, Evolution Codex and Quit share a baseline, button height and
    bottom inset. Options starts with Display: fullscreen toggle, window-size dropdown and native
    resolution readout. Fullscreen always uses the current monitor's native size; the dropdown is
    available in windowed mode and choices fit the usable desktop. Mode and window-size preferences
    survive saves and story resets; F11/Alt+Enter use the same settings. Desktop expands the canvas
    instead of letterboxing taller displays. The title's generated forest and lettering are separate
    layers, allowing the forest to cover the extra area while its core composition stays placed.
    - Build and content validation passed (0 errors, existing 45 build warnings / Test Wizard warning).
      Real-window checks cover native 2736x1824 fullscreen, 1280x720, 1280x800, 1024x768 and 1600x720,
      F11 round trips, saved preferences, aligned/in-bounds buttons, and full viewport/art coverage.
      Portrait/safe-area controller navigation and Quit still pass. A 4:3 gameplay render fills the
      window. Generated title layers have zero off-contract colors. All test saves stayed in memory.

47. **Android 0.14.0 fails to reach the menu** (Eric, 2026-11-06). **Cause identified on the Pixel 9:**
    UnlockCatalog's Laelaps/Orb of Ages initializers called AchievementDefinitions methods, whose
    static list enumerates the still-unassigned unlock definitions. Mono initialized achievements
    on the method call; Windows' runtime deferred it and masked the cycle. Use the existing const
    deed prefixes instead, keeping IDs/rewards unchanged. `_UnlockStartupProbe` uses a MainLoop
    without autoloads to check both cold initialization orders in separate processes; the phone-free
    script now includes both. Release-signed 0.14.1 updated the Pixel 9 in place and reached the
    menu, Powers, Achievements and Options; 33 Arcane Energy, earned deeds and bought pages remain.
    Eric spotted the title version/prompt overlap and requested more prominent bottom paging.
    Both layout fixes pass desktop/portrait title and input probes, including short/tall phone
    layouts. The final signed APK is installed on the Pixel 9 and starts a normal Forest run;
    the phone is back under Eric's control. Windows release-engine menu checks also pass.
    Eric confirmed that the upper-left version and bottom page controls look right on the phone.
    The phone shows "The menu could not be loaded" and Try again. Windows clean/existing-save
    launches and a real 0.13.2-to-0.14.0 update did not reproduce it. APK menu assets and script
    registration are present. Phone-free release-engine/portrait tests with APK assets, GC stress,
    and both injected resource/construction failures pass. These do not validate Android's runtime.
    Added stage-specific errors, bounded native startup-error capture and Copy error details, so
    the next failed launch can be reported without connecting a phone. Fixed failure cleanup
    disconnecting a viewport signal before it had been connected. No save schema or epoch changes.

48. **Clarify Rampart's description** (Eric, 2026-11-07). "Enemy bolts" means shots fired by
    enemies. Each intercepted shot drains shield points equal to half that shot's damage (rounded
    up), not half the shield's capacity. The card now states that cost explicitly and calls out the
    20% reduction in shield recharge time. Replaced the Bolts tag and the vague "twice as long"
    synergy line with plain wording. Mechanics and balance are unchanged.

## Chat-only notes recovered 2026-11-07 (Eric: "Don't allow any to slip through")

**2026-11-07, Eric: "all large pieces have my go."** Every held large piece is cleared to build -
the water pass, the ground biomes, Baba Yaga's four-sided solid hut, the remaining unlock puzzles,
the per-chapter landmarks, Shadow Bolt as a Conjunction, the boon-slot rework (41), the tag/slot
suite (52), the second-half campaign (53), cross-device saves (14; a provider sign-up still needs
his account, so the manual export/import half goes first), the opening-cinematic reworks (15, 16),
the side-event payoff beats (58) and the teacher-discovery amplification (49, without waiting on
his playtest). Sequencing is the integrator's; release whenever a piece is finished and validated.

The 2026-11-06/07 playtest notes Eric gave in chat after asking for chat-only notes, swept back out
of the session transcript on 2026-11-07 at his ask - chat notes do not survive context compaction
(Prismatic Spray was nearly lost). Already released in 0.15.0: Magic Missile's fan of three faster,
curving darts; launched bodies dying on landing; Taproot's roots; the hut's toes pointing the way it
walked; the Laelaps/Ascension unlock texts; the grove hint's verb.

49. **Finding the teachers: "subtle until found, obvious once noticed"** (Eric, 2026-11-06/07). The
    hurdle/track clues "don't strike me unique... the clue should look magical, or out of place, and
    be more obvious"; and **a short cinematic at the start of every chapter until its major secret
    is found - above all the Fallow Fields' Ascension teacher**. Fold with 0.13.1's guidance (the
    map hints, the retired golden route) and pick the amplification after Eric plays 0.15.0 - e.g. a
    persistent marker or map pin once any track has been touched.
50. **The Sovereign Rite activates both Ascension paths on the crowned spell** (Eric, 2026-11-07;
    building, `feat/haste-merge-sovereign-twin`). At crowning - and at the Ascension pick, if
    crowned before level 8 - the spell gains its unchosen level-8 branch too. **Test note (Eric):
    check this does not break anything on any spell** - every spell crowned with both branches
    live, by regression check and by play; some branch pairs were designed as either/or.
    **Removed 2026-11-08:** the Rite became a two-spell Conjunction (`.ai/sovereign-spell.md` section 9),
    so the crowned spell is the Rite itself, which never ascends - the twin had nothing left to double.
51. **Merge cooldown and attack speed in the Powers** (Eric, 2026-11-07; building, same branch).
    Chronoweave (-5% cooldown) and Tempest Rhythm (+6% attack speed) read as the same lever twice.
    Retire Chronoweave, convert bought levels into Tempest Rhythm up to its cap, refund the rest
    (schema 23). Per-spell cooldown stats and card lines stay - only the global pair merges.
52. **Mechanics that play on tags and slots** (Eric, 2026-11-06/07): items and spells that reward
    different tags in every slot; **Prismatic Spray** - bonus effects for all the different tags
    across your spells; **Sympathetic Blast takes the element of your leftmost spell**; spells that
    specifically work around critting. Design as one family with 9a's slot play.
53. **The second half of the campaign** (Eric, 2026-11-06/07, expanding item 43): once every spell
    and character is unlocked and the main boss beaten, further chapters are "excuses to keep trying
    new spell combinations on new maps so it feels fresh" - achievements welcome, unlocks not
    required. The gods (item 43) arrive here, each starting with a combination spell as their
    default. Themes still to design.
    **Designed (2026-11-07): `.ai/second-half.md`**, docs only, nothing built - the far shore across the
    dark sea, six chapters each a god's seat, the far sky naming a Conjunction every run, the gate,
    the teaching order, and fifteen questions for Eric (its section 8) before any of it is built.
    **Decided 2026-11-07: Eric accepted every recommendation** (the six gods kept, "The Far Shore" the name;
    question 10, a Muster, stays open until F4 measures it). Greenlit to build in `second-half.md` section 9's
    order: F0's plumbing first and alone, then the god plumbing, then the slices; the five new Conjunctions
    (Maelstrom, Dark Lens, Orrery, Hailfront, Anvil Line) can be built ahead of their chapters.
    **F0 built and merged 2026-11-08** (f36a6b73; `second-half.md` section 9, "F0, built"): `FarShoreOrder`,
    `StageGate.FarShore`, six stand-in chapters (`stage_14`-`stage_19`) nobody can enter, every reader taught
    both orders, `ValidateFarShore`, and a marked block in each shared registry for F1-F10 so the slices can be
    built in parallel. **F1 built and merged 2026-11-08** (df82ecb5, squashed: the agent branch's commit carried
    another session's message through a shared scratchpad; "F1, built" in `second-half.md`): a god is data -
    `UnlockCatalog.FromGod`, any recipe an opener, `SeatClear`, the rekindled reel's far-shore ground. **F2 built
    and merged 2026-11-08, after 0.21.0** (Artemis, roster index 23; "F2, built"): hidden until the White Downs
    open. **Building now, in parallel: F3 (the White Downs), F5 (reel E and the map), F4 (the far sky).**
    **0.21.0 released 2026-11-08** (c29952c2): the new name, Imhotep's and Tituba's puzzles, the new update
    address; all three `version.json` addresses checked serving it.
    Agents now each get a scratch folder of their own: sharing one, two rewrote each other's `commit-msg.txt`
    and `godot.sh`.
54. **Wording and card fixes** (Eric, 2026-11-06): Waywiser - say what "slowest spell" means
    (longest cooldown?); Reforged - "iron shards cut enemies as it is rebuilt" (it read as cutting
    the player); Chalice Cup confusing - and is it useful at all after 15 kills?; the granary's
    "counted with grain" and "settle his count" lines reworded; "Kills wither enemies" - should it
    be "withered"?; Skipstone's "for 90% more" - 90% more of what?; the Handoff description is
    confusing (find which card he means); Attunement is confusing - revisit the mechanic or its
    words; a tag that would pass six slots just says "Max"; an item set's card always shows every
    set effect (Eric: "Just show all effects always, we can save room in other ways").
    **Checked 2026-11-09** (Eric: *"I thought we reworded reforged?"*): the granary lines, Wither, Skipstone, Handoff
    and "Max" had landed; **Reforged had not** - its card still said shards "fly in to you, cutting what they pass".
    Reworded that day: *"As the ward is rebuilt, iron shards fly in and cut the enemies they pass."* (107 of 130).
    Waywiser and the Chalice Cup were reworded here but still read as confusing, and came back as items 68 and 73.
55. **Visual and HUD fixes** (Eric, 2026-11-06): Aegis Ward's shield bigger than the player (it
    hovers at the chest); the Archmage's sprite leans back - stand him straight; the Raven's
    highlight is too bright; the level-up Codex button is misaligned vertically; spell icons brought
    up to the spells' own art quality (Meteor Swarm first); on the phone the element tags sit under
    the timer - move them, and the spell icons up near the tags; the top health bars touch (boss vs
    player); Hollow Star draws over foliage and over Black Tentacles (fold into item 30's layer
    pass); unlocked portraits sort above locked on the character select; the chapter-opening scroll
    says how to dismiss it, works on a controller, goes away by itself after a few moments, and is
    smaller.
56. **Gameplay fixes** (Eric, 2026-11-06): XP orbs never fail to catch the player - they speed up
    until they arrive; Meteor Swarm always targets different enemies, and with one in range the rest
    scatter; a boss entrance clears the field - lingering rifts stayed up, Cinderbreath's fire
    stayed on screen, and enemies and XP orbs should not draw during the cinematic; boons stop
    filling level-ups once every spell is maxed, with the two-boon cap (fold into item 41);
    Elderbark wants more reach and harder hits; spending a revive gets a quick, dramatic animation;
    the Codex must not take item/recipe clicks while a spell's page is open; a tracked mission that
    cannot be done in this chapter is not shown there.
57. **The Hayward's fight** (Eric, 2026-11-06; fold into item 28): combo attacks - a swing, a
    charge, then his back vulnerable; hitting the shield shows a shield mark and plays a different
    sound, so the player knows the damage is cut; slowed he turns slower; stunned he cannot turn.
    **Built 2026-11-07 (`claude/nostalgic-lalande-4db33d`, with item 28):** the fight is the combo
    now. The cone swing's resolve primes a charge (`AttackTelegraph.Prime`, so it chains without
    skipping its tell): a lane aimed once at the player, planted for 45% of a 1.5 s wind-up, then
    driven down at 4.5x his walk, hurting where it passes (the Gaoler's run rule); its resolve
    leaves him SPENT for 2.6 s - planted, no turn, no attack, the whole open side drawn as a
    pulsing gold arc with chevrons at his back, paying x1.3 (`SpentRearMultiplier`). A cut hit
    stamps a little woven-panel mark where it arrived and plays the new `boss_block` (wood, not
    the element, −13 dBFS, throttled 90 ms); his tracking turn is scaled by `Enemy.PaceShare`, so
    slowed he turns slower and held or frozen he cannot turn. Phase 3 quickens the charge x0.85
    and cuts the spent window x0.7 (floored at 1.6 s). `ValidateBossPatterns` holds every beat's
    number (tell fraction inside 0-1, charge over 1x, a spent window that exists), and the probe
    now prints the combo's beats (`HaywardBoss.SwingsBegun/ChargesBegun/SpentWindowsEntered/
    HitsTurned`, `_BossProbe` "the combo"). Measured 2026-11-07: at `--drain=6` he dies through
    both thresholds with every swing chained (2 swings, 2 charges, 2 spent) and 115 hits turned;
    at `--cc=none` against a standing wizard, 6 swings, 6 charges, 6 spent windows in the test's
    45 s - the generic "12 attacks begun" is exactly those pairs. The default probe still runs
    out of clock, as the doc says it always has (the hurdle cuts the probe's own drain). Whether
    the lane and the gold open arc READ still needs eyes - `_UiShot` has no flag that reaches a
    boss fight, so no photograph exists.
58. **Side events pay off at their own site** (Eric, 2026-11-06, "Yes, both"): the well's win plays
    at the well - the fouled water turns clean and rises with light, the coin (on later wins, the
    chest) is drawn visibly up out of it and flies to the player, with a brief hold so it registers.
    Then check every side event for the same missing moment at its own site.
    **Built 2026-11-07 (`feat/event-payoffs`):** the well's win beat - the water climbs clean and lit
    out of the mouth, the fouled pips wash clean, and the drawn-up glint arcs to the exact spot the
    reward column rises at (SideEvent.Succeed takes an exact spot now). **Audited:** all fourteen
    events already raise their column at their own site, so the remaining polish is per-site state
    changes at the win, each its own small beat when its site has a state to show: the grove could
    visibly bloom, the Gaoler's cell door swing, the thaw's ice visibly fall away, the scale settle
    level. Worth one beat per chapter pass, not a blocker.
    **Built 2026-11-07 (`claude/optimistic-merkle-56681f`):** the three named beats, each the well's
    shape (a short beat at the site on SecretSeen, then `Succeed(spot, exact: true)`). The grove
    blooms before it pays - the crowns leaf out in turn round the ring under a green wave, and the
    bloom still lingers after. The Gaoler's key turns in the lock (a pause before anything moves),
    then the barred gate swings open on its bottom hinge, the gloom lightens, the sitter is gone and
    the cell's light spills out. The thaw's ice falls away - a crack and a held breath, then chips
    fall off the block and melt, meltwater runs out at its foot and the block fades with them.
    `_UiShot --win-event` now runs the real path for the cell and the thaw too (photo hooks beside
    the tomb's). Re-audited the rest: the seal keeps its turned stones, the anvil its burst chains,
    the tomb opens its slab, the mouths/door/moot/gibbet have their own endings - no other site is
    missing a state at the win (the scale's settle is Medea's redesign, the hut's kneel its own
    branch). Verified: build 0 errors; validator 0 errors / 1 known warning; `_ExploreProbe` stages
    0, 1, 5 all green through the beats; all three photographed. Not verified: feel at speed, sound
    mix in a real run.
59. **World and terrain** (Eric, 2026-11-06/07): Baba Yaga's hut is solid - the player cannot walk
    through it; the farm patch's large dandelion hides a secret (a certain spell cast near it?); the
    Dandelion Grove is too common - why is it in more than one map?; a plant overhanging water drew
    a jagged shoreline; ground patches bigger on average, so the light patches read as biomes or
    fields; water bodies rounder, and walkable - the player just slows heavily (the four
    blocking-terrain authorities in `.ai/terrain.md` must move together); the hut's art gets four
    sides so it can turn as it runs and always fall facing the camera, door in view (fold into item
    3's Baba Yaga ask).
    **Built 2026-11-07 (`feat/water-pass`):** the water half. Wading - water's shore polygons moved
    to their own layer (`WaterCollisionLayer` 6), which the feet don't mask; the player wades at
    0.45x speed (`IsWading`), the swarm still stops and routes round, placement and the flow field
    unchanged, and the Bridge's abyss opts out (`StageEnvironmentProfile.WaterWalkable = false`).
    Rounder bodies - water blobs take a smooth two-lobe rim wobble instead of the per-tile hash
    jitter. Overhang - trees keep 48 px and bushes 24 px of water clearance in `IsPropAvoidedAt`.
    **Built 2026-11-07 (`feat/baba-yaga-hut`):** the hut asks, with item 3's herded-yard puzzle -
    solid (a feet box on the blocking layer), four-sided (front and side walk cycles, facing from
    its heading), and it falls facing the camera as the kneel frames always did.
    Still open here: the Grove's spread - now item 72 (the "dandelion" is the flower pen's moonflower, and its
    secret was built as the Fenced Bloom).
60. **Audio** (Eric, 2026-11-06): the shield-break noise is annoying - deeper, like cracking glass
    (`tools/audio/sfx.py`); battle music per chapter still reads to Eric as the menu's music - check
    what section 3a item 7's per-chapter stems actually play in a run.
61. **"Pay the tithe" shows the payment** (Eric, 2026-11-06): XP visibly sucked into the granary's
    machine, so what to do is legible without the text.
62. **Rename the game "The Last Wizard: Survivors"** (Eric, 2026-11-08: "update the website, the repo, the folders
    locally, this chat name, and on the game, the name should be 'The Last Wizard: Survivors'"). **The game and the
    repo's files: built 2026-11-08** (`.ai/game-name.md` lists what changed and what was kept as a key;
    `.ai/releasing.md`, "The second rename"). The USPTO knockout search came back clear the same day
    (`.ai/game-name.md`). **The repo: renamed 2026-11-08** to `eclancy/TheLastWizardSurvivors` (Eric: "Yes, change
    the GitHub repo and the website"); both old slugs redirect, `version.json` and the API checked by curl. The
    game asks `ecec.dev/the-last-wizard-survivors/version.json` from 0.21.0. **The website: moved 2026-11-08**
    (website PR #54, deployed before 0.21.0 as it had to be - `.ai/releasing.md`, "The second rename"): new pages
    at `/the-last-wizard-survivors/` and `/games/the-last-wizard-survivors`, the old `version.json` and `download`
    pairs answering directly forever, every other old address a 301. Still to do: the Play listing (Eric's), the
    local folders (last). A second rename;
    read `.ai/releasing.md` "The rename" and `.ai/game-name.md` first - the first one's list of what keeps the old
    name on purpose ALL still holds, and now "The One He Missed" joins it wherever changing it would strand a copy
    or a save. The pieces, in order:
    - **The save folder must not move - so `config/name` stays "The One He Missed".** Godot names the desktop
      `user://` folder after `config/name` (`%APPDATA%\Godot\app_userdata\<name>`), and Windows forbids ":" in a
      folder name. NOT `use_custom_user_dir`: Godot puts a custom dir directly under `%APPDATA%`, which would move
      every save (corrected 2026-11-08 before anything was built). Instead `config/name` becomes an internal
      identifier, kept like the Android package id, and the name is shown everywhere a player sees one: the
      window title set at startup (`GetWindow().Title`), the Android label (`package/name`), the Windows product
      name and file description (the export preset), the Linux `.desktop` Name (`install.sh`), the installer's
      display name (`installer.iss`, never its `AppId`).
    - **The game:** every player-facing line (the update screen, the bug report's confirmation, the startup error
      report, the save-export filter; NOT the story's own "the one he missed" - the Court's quatrain and the
      ending's line stay, they are the premise), the title screen's lettering (`tools/art/splash.py`,
      `splash_wide.py`, `hero.py`), `README.md`, `CLAUDE.md`, `.ai/game-name.md`. Internal identifiers
      (`TheOneHeMissed` namespace, assembly, solution, release file names, env vars, the keystore, the bug report's
      `game` field if the intake keys on it) keep the old name where changing them would break updates; decide each
      against `.ai/releasing.md`.
    - **Updates:** `ecec.dev/the-one-he-missed/version.json` is baked into every copy since 0.12.0 and must answer
      forever, as `/wizard-survivors/` still does. Any new address is added beside it, never instead.
    - **The repo** (`eclancy/TheOneHeMissed`): GitHub redirects a renamed repo's URLs, release downloads included,
      but confirm the feed and the download page still resolve after; outward-facing - ask Eric right before.
    - **The website** (EricClancyEngineeringAndCrafts): new pages and paths, the old ones kept answering;
      outward-facing - ask right before.
    - **The Play listing:** Eric's, in the Play Console.
    - **Local folders:** the checkout and its sibling worktrees - LAST, when no session is running, since renaming
      them under a session breaks it. Eric's to do, or with every session stopped.
    - **This chat's name:** done 2026-11-08.
    - Before the public steps: the USPTO check of the name (classes 9 and 41), and an icon and screenshots that look
      nothing like the "Wizard Survivors" Play app's, per the 2026-11-07 store-title discussion.
63. **A historical gallery** (Eric, 2026-11-09, for the week of 2026-11-16: *"we should have a historical gallery,
    with a short description of the history of all the characters unlocked in the game. we will work on that next
    week."*). A page that shows each character the save has unlocked with a short, factual account of the real
    history or legend behind them (Tituba, Väinämöinen, Abe no Seimei, Wayland, the Cumaean Sibyl, Laelaps, Baba Yaga,
    Morgan le Fay, Paracelsus, Nostradamus, Imhotep, Medea, Ma Liang, Nimue, Merlin, John Dee, and the gods as they
    come - Artemis first). Not designed yet. Start from what exists: each wizard's "Who." line in
    `.ai/chapter-wizards.md` (the history is already researched there), the gods' in `.ai/second-half.md` section 3,
    the Evolution Codex and the Achievements page as the nearest menus, and the Court's in-world Gallery of covered
    portraits (`.ai/endgame-castle.md` 5.4) - a different thing, but its uncovering is a possible tie-in. Open for
    Eric: where it lives (main menu, the Codex, the character select), whether a locked character shows a sealed
    frame (the "one locked thing on show at a time" rule, `.ai/slow-start.md`), the starters (original characters,
    no history - leave out, or give them in-world lore), and the real people's entries kept respectful and sourced
    (`.ai/chapter-wizards.md` 1618, "Real people").
64. **The far shore as a DLC** (Eric, 2026-11-09: *"make a note that 'far shores' will be designed as a 'DLC' when we
    release this as an actual game"*). Not now: when the commercial release is planned. Until then every far-shore
    piece keeps itself separable from the base game (its own order, files and marked blocks; nothing on the road
    needing it) - `.ai/second-half.md`, the note under its status, lists what a DLC will need designing then (an
    ownership check beside `StageGate.FarShore`, a store product per platform, what a player without it sees past
    the Throne, and which shared systems stay in the base game).
    **Week of 2026-11-16: plan it** (Eric, 2026-11-09: *"Do we have all of the far shores characters created? I want
    to plan far shores to be a dlc when we release the game"*). Where the half stands on 0.22.0: **one of six gods is
    built** - Artemis (F2), with her chapter the White Downs (F3), the far sky (F4) and the map and reel E (F5); the
    stage select reaches her seat once the far shore's gate is met. The other five gods - Manannán mac Lir, Hecate, Ra,
    Ukko, Hephaestus - are designed only (`.ai/second-half.md` 3.2), and so are their opening Conjunctions (Maelstrom,
    Dark Lens, Orrery, Hailfront, Anvil Line), their chapters (stand-ins `stage_15`-`stage_19`, `IsPlayable = false`)
    and each chapter's cast and boss: F6-F10, each "Conjunction M, god M, chapter L". The DLC plan has to answer what
    64 lists, and also whether Artemis and the White Downs - already free in 0.21/0.22 - stay in the base game as a
    taster or move behind the DLC with the other five.

65. **The executable still says "The One He Missed"** (Eric, 2026-11-09: *"The file was called 'the one he missed'
    still, let's fix the name of the executable"*). The player-facing files are `TheOneHeMissed.exe` (Windows),
    `TheOneHeMissed.x86_64` (Linux) and the downloads `TheOneHeMissed-Setup.exe`, `-windows.zip`, `-android.apk` and
    so on. They were kept deliberately at the 2026-11-08 rename (CLAUDE.md "Repo gotchas"); Eric now wants them changed.
    A likely name is `TheLastWizardSurvivors` - no colon, which Windows forbids. What it touches: `export_presets.cfg`
    (three `export_path`s), `package.py` `NAME` (every download's name, the patch's file list) and `install.sh` `APP`,
    which must stay equal to it, `installer.iss` (the program, shortcuts, uninstall icon, and a delete of the old
    `TheOneHeMissed.exe` beside the one that already removes `WizardSurvivors.exe`), and `release.sh`'s file list. **The
    hard part is the copies already installed**: since 0.12 the updater "keeps an install's own program name"
    (`package.py` BRIDGE comment, `UpdateScreen`), so an existing install would keep running `TheOneHeMissed.exe`
    unless the updater learns to rename itself. The `.pck` must match the program's name, but the C# assembly
    (`TheOneHeMissed.dll`, `data_TheOneHeMissed_*`) need not. ecec.dev finds downloads by how their names end, so a new
    prefix is safe there. Read `.ai/releasing.md`, "The rename" and "The second rename", first, and rehearse with
    `--dry-run` plus an update from an installed 0.22.0.
    **Raised again 2026-11-10 after 0.23.0** (Eric: *"the downloaded file is still created as 'the one he missed-android'"*).
    A cheap first half: rename only the DOWNLOADS (`TheLastWizardSurvivors-android.apk`, `-Setup.exe`, `-windows.zip`,
    the patches, the Linux tar) by giving `package.py` a download prefix apart from `NAME`, which stays the program's
    and the `.pck`'s name inside them. Safe because every updater follows version.json's addresses, not file names,
    and ecec.dev finds downloads by their endings. The program's own rename stays the harder second half.
    **Decided (Eric, 2026-11-10): the download rename goes in the next release** - build it then, and rehearse it with
    `--dry-run` (check the endings ecec.dev looks for and version.json's addresses) before publishing.
66. **Rearrange the achievements: Canticle should not come from First Blood** (Eric, 2026-11-09: *"Canticle doesn't
    seem like a spell that sounds be rewarded by 'first blood'. Rearrange some of the achievements"*). Today First
    Blood ("Defeat 1 enemy", `AchievementDefinition.cs` 446) unlocks Canticle (`arcane_explosion`, `UnlockCatalog.cs`
    214). Canticle is the music's spell - sung on the beat, a chord gathered from every spell's element - so its deed
    should be about rhythm or harmony (casting on the beat, holding several elements' notes at once), not a first
    kill. Review the whole Feat and Progress list for other mismatches at the same time, and give First Blood
    something that suits a first kill. Moving a spell between deeds narrows nothing a save holds, but check
    `SaveData.Migrate` step 19 (`MigrateToAchievementUnlocks`) and the validator's `ValidateAchievementMigration`,
    which reconcile done deeds with owned spells. Save schema is 28 on main.
    **Asked 2026-11-09 ("Did we pick a better first spell to unlock with first blood achievement?") - not yet.**
    Proposal for Eric: **First Blood pays Lancet** - a lancet is the tool that draws first blood, and a needle at the
    most wounded enemy reads at once, which a first unlock should (`.ai/slow-start.md`: the first deed teaches how
    spells are earned). **Canticle moves to a new harmony Feat** - e.g. carry spells of four different elements at once
    (the chord it sings is gathered from its casters' elements). That leaves Shrike (500 poisoned enemies killed) with
    no spell: retire it or give it another. A spell a save already owns is never taken back.
    **Decided (Eric, 2026-11-09): *"I like first blood unlocking lancet"*.** Canticle is the Sibyl's opener, so under
    item 84 it waits for her anyway; its new deed (the harmony Feat) and Shrike's fate are still open.
    **Reward review, 2026-11-10** (91 paying deeds read; 83 fit; report in the integrator's scratchpad,
    `reward-review.md`). Eric decided all five fixes, one at a time:
    1. **Swap Veteran and Out of the Ring (the Ninth Stone):** Veteran pays Shadegrease, the Ninth Stone pays Meteor
       Swarm. (Veteran finished in the same run as Forest Cleared and Survivor, three spells at once; the Circle, the
       first wall, paid a +6% boon.) And make Veteran's "without falling" true: a win saved by an extra life does
       not count.
    2. **Fireball to the Emberdeep's boss deed; the Emberdeep's 300 Arcane Energy to the Sunken Cave's boss** (which
       paid 100, the smallest boss reward). Unwound (the Sands) keeps Sunsteel Filament only. The final gate counts
       wizards now (`.ai/achievements-and-secrets.md` 2.5), so a later chapter may pay a spell.
    3. **The Coursing pays a new hunting boon, not 50 Arcane Energy** (Eric: *"should be something special, not
       just energy"*; he chose a boon over a Teumessian Fox familiar or a starlit hound look). Proposed: **the
       Quarry** - the nearest wounded enemy is marked and takes extra damage; numbers set when built, against the
       other boons.
    4. **Pheasant (Medea's opener deed) asks for 4 Light held at once**, not level 20 - Solar Flare's own element.
    5. **The Smith's Toll (Wayland's opener deed) moves to the Cold Forge** (the forge that made the shackles), off
       the caravan master; finding the forge again as Wayland must still complete it.
    Also from the review: the six element-Adept locked hints in `UnlockCatalog.cs` still say "End a run with 4",
    but the deeds count the most held at any point in a run - reword them.
84. **A wizard's opener is unlockable only once that wizard is** (Eric, 2026-11-09: *"I also want spells that are
    another wizard's default spell to only be unlockable once that wizard is unlocked"* - *"Except for familiar - since
    you need that to unlock Laelaps"*). The openers and how each is earned today (`CharacterData_*.tres`
    `StartingSpellResource` against `UnlockCatalog`): Baba Yaga - Thorn Vine (the Forest's boss); Imhotep - Scorching
    Ray (Fire Adept); John Dee - Folio (Magpie); Laelaps - Cyclone Slash (Elementalist); Medea - Solar Flare (level 20);
    Morgan - Sleet Runes (the Frozen Waste's secret); Nimue - Spiritual Weapon (the Ruins' boss); Nostradamus - Starfall
    (the Broken Circle's secret); Paracelsus - Contagion (the bog lanterns); Seimei - Kindled Ward (the Ruins' secret);
    the Sibyl - Canticle (First Blood, moving - item 66); Tituba - Riptide (the Cave's boss); Väinämöinen - Bramble Seed
    (the grove); Wayland - Iron Palisade (the caravan master); Ma Liang - the Familiar (**exempt**). Not affected: the
    starters' openers (Cinderbreath, Cone of Cold, Obsidian Spike, Chain Lightning, Magic Missile - every save has
    them), and the Conjunction openers (Artemis's Sagittarius, the Dark Wizard's The Taking, Merlin's Orb of Ages - made
    in a run, not unlocked; check Merlin's). **Decide while building:** if the deed is done first, is the spell held
    and granted the moment the wizard is freed (the gentler reading, and nothing done is wasted), or can the deed not
    be done? The deed's card should say "after <wizard> is freed" either way. **Watch the slow start:** Thorn Vine is
    the FIRST boss's reward and Baba Yaga comes late (`finalGateOnly`), so the first boss would pay nothing visible for
    a long time - give the Forest's boss another spell, or accept it (`.ai/slow-start.md`, `ValidateGateReachability`).
    Build it in `UnlockCatalog` (a rule read from the roster, so a new wizard's opener is covered by itself) and
    `GlobalStatsManager`; `RegressionChecks.ValidateUnlockCatalog` should hold it (every non-starter opener but the
    Familiar requires its wizard). A save that already owns such a spell keeps it - and checked 2026-11-10, nothing
    reconciling deeds ever removes an owned spell (`GlobalStatsManager` only grants), so that needs no schema step.
    **Decided (Eric, 2026-11-09):** *"Thorn Vine can be an exception"* - it stays the Forest boss's, which also settles
    the slow-start worry. And *"We block the achievement until that wizard is unlocked. In fact, those achievements
    should require being completed with that wizard"* - the deed cannot be done before the wizard is freed, and only
    counts in a run played AS that wizard (Fire Adept as Imhotep, Magpie as John Dee, level 20 as Medea, and so on).
    **The catch to design around:** several of these deeds are not only spell rewards. The chapter boss deeds
    (`cave_choir_silenced` for Tituba's Riptide, `ruins_delver` for Nimue's Spiritual Weapon) are campaign
    progression, the `event_<site>` and `secret_<id>` deeds are the events' and secrets' own records, and requiring a
    wizard for those would block the road. So the opener moves to a NEW deed of its own - "Silence the Choir as Tituba",
    "Find the Buried Runes as Morgan", "Hold the bog lanterns as Paracelsus" - and the original deed keeps whatever
    else it pays. Nimue is freed only at the Throne, so hers is a post-campaign deed. The Achievements page shows a
    sealed deed for a wizard not yet freed (one locked thing on show at a time, `.ai/slow-start.md`).
    **Decided (Eric, 2026-11-09): *"Yes it should, and Nimue can also be an exception"*** - every opener moves to a new
    deed of its own, done as its wizard; Spiritual Weapon stays the Ruins boss's, as Thorn Vine stays the Forest's. The
    exceptions are therefore the Familiar, Thorn Vine and Spiritual Weapon. Name each new deed in the house style (the
    Feats are birds; a chapter's are its own) and write its card with the wizard's name.
    **Decided (Eric, 2026-11-10):** the three secret-vault opener deeds **name the secret** ("Find the Buried Runes as Morgan"), not just the chapter as built.
85. **Status effects stay drawn on enemies hidden for a boss's arrival** (Eric, 2026-11-09: *"When enemies derender for
    the boss spawning animation, the animations for the cc effects on them should also derender"*). During a boss's
    entrance every ordinary enemy is held and hidden (`Node2DGame.BossEntrance.HoldOrdinaryBodies`: `SetHeld(true)`,
    `Visible = false`) but stays targetable, and `StatusEffectLayer` skips only an enemy that is not targetable (a
    burrower, `StatusEffectLayer.cs` 261) - so the slow, freeze, poison and other marks keep drawing over the empty
    spots. Fix, one line: skip `!e.Visible` there as well (or `e.IsHeld`). Check the other things drawn for an enemy
    outside its own node do the same: the attack icon (`TelegraphIcon`), elite marks, the mire's wader rings.
86. **The stage card's Start button is still off the bottom of the phone** (Eric, 2026-11-09). On a phone the world
    map's card (`WorldMap.cs` 433-440) is a fixed `NarrowCardHeight` (456) anchored to the screen's foot, Start its last
    row. When its rows need more than that - the card body has grown (the far sky's line, Endless's vigil, rewards) and
    the foretelling button adds one (`WorldMap.Foretell.MakeRoomForForetell` patches only its own row) - the panel's
    minimum size wins and, growing the default way (down), it pushes Start under the bottom edge and the gesture bar.
    Fix: size the card to its content and grow it UP (`GrowVertical = Begin`) to a cap, with the card body in a
    ScrollContainer (`TouchScroll` already prepares those) so Start is always pinned on screen inside the safe area;
    the map view's bottom follows the card's real top on every rebuild. Check with `_UiShot --resolution 540x1212
    --portrait --safe-area` on the stop with the most on its card (a far-shore stop with the far sky, Endless and the
    foretelling earned) and on the Pixel 9 itself.
87. **Wayland gets a movement speed buff as well** (Eric, 2026-11-09: *"Wayland can also have a movement speed
    buff"*). Wings (`Player.Wings.cs`) was +10% move speed (`move_speed_10`) until it became "floor traps never touch
    him and water never slows him" on 2026-11-09; with item 74 giving the water to Baba Yaga and Nimue too, his flight
    gets speed back: e.g. +10% while he flies (he flies when he moves). Card in player words, with percentages
    (`.ai/character-fit.md` row 20, the Wings section); the one-verb rule for passives is about his feet, which this
    keeps.
88. **The town's attack in the opening, seen from inside the town** (Eric, 2026-11-10: *"when the town is attacked in
    the opening sequence, i want that scene to be different cinematically. the 'camera' should be from within the town,
    looking up at the houses burning, people running, and monsters attacking"*). Today it is the prologue's `tide`
    panel (`tools/art/prologue.py` `p_tide`, "A TOWN OVERRUN", Eric 2026-11-08): a distant view from the hill, the
    flood pouring over the crest into the town while its people flee out of frame, the tower far off. Redraw it as a
    low angle from a street inside the town: houses towering and burning against the sky, people running past the
    camera, monsters breaking in among them; keep the tower's glowing eye somewhere on the skyline if it still reads.
    Its animated box (`write_anim`) and `Prologue.cs`'s drift carry the motion; the memory on cinematics asks for shake
    and speed lines where they fit and pauses before and after. Read `.ai/prologue.md` and `.ai/cutscenes.md` first;
    edit the generator and re-run it, never the PNG.
89. **The Archmage's passive, said simply** (Eric, 2026-11-10: *"archmage's passive can just be something like 'Avoids
    death once per run' - simplify the description"*). Overlooked's card (`CharacterData_Archmage.tres`
    `StartingPassiveDescription`) says *"Once a run, a killing blow misses. Invincible for 10s."* - make it *"Avoids
    death once per run."* (or as close as reads). Text only; check the character select and pause screen show it.
90. **Lancet's icon should look more magical** (Eric, 2026-11-10: *"Lancet's icon should look a bit more magical in
    nature"*). Its spell icon (`assets/bonelight/ui/spells/lancet-metal.png`, drawn by `lancet()` in
    `tools/art/spell_icons.py`) is a plain steel surgeon's needle going into a skull, which reads as a tool, not a
    spell. Give the needle a spell's light - a glow along it, runes or a sigil on the steel, a trail of light behind
    the point - in its elements' ramps (Metal, Poison), and keep the evolution icons that reuse `lancet_needle` /
    `lancet_skull` (`tools/art/evolution_icons.py`) in step. Judge it at icon size beside the other spells; edit the
    generator and re-run it, never the PNG.
91. **Cinderbreath's Backdraft makes two flames** (Eric, 2026-11-10: *"update cinderbreath's 'backdraft' so it
    produces two flames, and balance the damage. they should never face the same direction / target the same enemy -
    we want to feel like there are two different flames around him"*). Today Backdraft (level 4,
    `SpellEvolutionCatalog.cs` ~3069, `Flamethrower.cs` `BackdraftDamageShare` 3) is one jet whose far end explodes
    for 3x when each breath ends, +35% damage. Make it two jets: the first on the nearest enemy, the second on a
    DIFFERENT enemy at a wide angle from the first (say at least 90 degrees; with no such enemy it points opposite
    the first), each re-aiming on its own clock so they never line up. Damage: two jets at about 60% each (some gain
    over one, not double) in place of the explosion and the +35% - measure with `_BalanceHarness --spell=cinderbreath`
    against the other level-4 branches. **The overlap to settle:** Fire Fan (level 8) is already "three jets, each
    turned on its own enemy" (`Flamethrower.SpreadAims`, reuse it for the second jet). Decide what Backdraft then Fire
    Fan gives (three jets in all? Fire Fan reworked?) so the two do not collide (the spell-collision rule in memory).
    Reword the card in player words.
    **Decided (Eric, 2026-11-10):** Fire Fan is reworked into a **wide sweeping arc** - one broad flame sweeping back and forth across a wide cone in front of him - so it never reads as Backdraft's second flame.
92. **The level-up screen's Codex button is not level with the heading** (Eric, 2026-11-10: *"codex button on level up
    is not vertically aligned with the header"*; first raised 2026-11-06 in item 55 and never marked done). The button
    (`LevelUpMenu.EnsureAscensionButton`, ~2355) is pinned to the panel's top-right with a first-guess offset, and
    `AlignAscensionButton` re-centres it on the heading band whenever the heading or the column moves - yet it still
    sits off. Photograph the cases before touching it (`_UiShot --force-levelup`, and `--show-mutation`, on
    `--resolution 1280x720` and `540x1212 --portrait --safe-area`; with and without the charges row) and find which
    one is wrong - most likely the band's height (MinTouchTarget + 8) against the button's, or the align pass not
    running after the last layout. `.ai/level-up-screen.md` holds the screen's rules.
93. **Aegis Ward's icon and animation** (Eric, 2026-11-10: *"now that aegis ward is larger, increase the fidelity of
    its icon and make its animation less obtuse"*). The icon (`assets/bonelight/ui/spells/aegis-ward-light.png`, from
    `aegis_ward()` in `tools/art/spell_icons_core.py`; the in-world art is `tools/art/aegis_ward.py`) needs the detail density the other spell icons have now
    (the sprite-detail memory). The in-world ward (`AegisWard.cs`: the dome, its flash on a blocked bolt, Reforged's
    shards, the reflect's shaft; pixel-art pass e5e07bee) reads as heavy-handed at its new size - make its motion
    lighter and more legible: a quieter idle, a brief clean flash on a hit, a clear break. Ask Eric what "obtuse"
    points at first if a shot of it does not make it obvious. Edit the generators, never the PNGs.
    **Decided (Eric, 2026-11-10):** "obtuse" means **too big and bulky, and too flashy or noisy** - a smaller, lighter dome, and quieter flashes, shards and shafts.
94. **Ink Ring becomes Ma Liang's starting spell** (Eric, 2026-11-10: *"would it make sense for ink ring to be his
    starting spell instead?"* - then *"Yes, add it"*; asked after he earned Ink Ring without Ma Liang freed, since it
    was nobody's opener). His brush, walked: drawing a line that becomes real is his legend, and it makes the
    watercolour ink his signature. What moves: `CharacterData_MaLiang.tres` `StartingSpellResource` to Ink Ring (his
    card no longer reads "Familiar - Crow"); **his Apotheosis, The Painted Door, moves from the crow Familiar to Ink
    Ring and keeps its verb (*displace*)** - closing a ring paints a door and sends everything inside it away
    (`ApotheosisCatalog` ~381-393; `ValidateApotheosisCatalog`'s one-verb and element rules; Ink Ring is Water +
    Darkness); Ink Ring's deed becomes his under item 84 (Cormorant, done as Ma Liang, sealed until he is freed) and
    the Familiar leaves item 84's exceptions list for good (it comes only from the Pinfold). The ink crow stays his
    look whenever he takes the Familiar later. Check Ink Ring carries an opening alone - it asks the player to walk
    loops; measure with `_BalanceHarness --character=<Ma Liang> --circle` and nudge its level-1 numbers if he cannot
    hold the first minutes. Update `.ai/character-fit.md` (row 21, and the Ma Liang lines) with the decision.
95. **An enemy's attack circles vanish the moment it dies** (Eric, 2026-11-10: *"as soon as an enemy dies, the attack
    circles it produced should disappear"*). Two kinds outlive their maker today. (a) `TelegraphedGroundHit.Place(arena,
    at, tell, radius, damage, colour)` is parented to the arena with no owner, so a warning a boss placed away from
    itself (`BossEnemy` roots ~267, `ArchivistBoss` ~198, `BridgeConstableBoss` ~297-400) keeps drawing and still LANDS
    after the boss falls; give `Place` an optional owner, and when that owner starts dying or is freed, remove the
    warning and land nothing. The world's own hazards (`BridgeRules`' murder holes and volleys) pass no owner and are
    unchanged. (b) A telegraph drawn on its owner (`GroundSlamAttack`, the wind-up's ring, cone or line) can still be
    painted through the death animation - cancel it in `Enemy.StartDeath` (`GroundSlamAttack.Cancel`) so it goes on the
    same frame the body stops being targetable. Check every telegraphed enemy (`ValidateAttackPatternEnemies` lists
    them) and every boss's phase hits.
97. **Remove attunement altogether** (Eric, 2026-11-10: *"remove 'attunement' altogether, and all references to it. we
    will rebalance around that later."*, after asking what it meant; supersedes the attunement wording in item 54). It
    is three things, about 106 references in `scripts/` (SpellEvolutionCatalog 49, Player 23, Node2DGame 14, LevelUpMenu
    6, RegressionChecks 3, MainMenu.Shop 3, and single ones elsewhere) and eight docs (`gameplay-design.md`,
    `level-up-screen.md`, `combo-spells.md`, `conjunction-progression.md`, `protagonist.md`, `endless-mode.md`,
    `achievements-and-secrets.md`): (1) the damage bonus - a pure one-element spell deals x1.20 / x1.45 / x1.80 at its
    element's tier 2 / 4 / 6 (`Player.AttunementTier*Multiplier`, `GetAttunementMultiplier`, `GetAttunedElement`, in
    the damage product ~2178); (2) the cooldown price on a level-4/8 branch that adds a second element
    (`SpellEvolutionCatalog.AttunementGainCooldownCost` 1.12, every `CooldownMultiplier = AttunementGainCooldownCost`);
    (3) the words - "stays attuned" / "attunement lost" on the level-up cards, the pause screen's line
    (`Node2DGame.DescribeAttunement`), "Attuned" in the shop's codex rows, the element popover. Remove all three and
    their validators' rules; the element tiers' own bonuses stay. **Balance warning:** pure-element builds lose up to
    80% of their damage at tier 6 and hybrid branches get 12% faster, so runs will shift until the balance pass
    (item 77) - Eric knows ("we will rebalance around that later"). Measure a pure and a hybrid build with
    `_BalanceHarness` before and after so the pass starts from numbers. Element tags themselves are unchanged.
98. **A full build's level-up offers Arcane Energy beside Marginalia** (Eric, 2026-11-10: *"when levelled to full and
    offered marginalia, offer a second choice for arcane energy if they decide not to take the marginalia"*). Today a
    level-up with nothing left to deal holds one card, Marginalia (`Player.BuildMarginaliaOption`, ~8789: heal 30% of
    max HP and +2% spell damage, repeatable). Add a second card, a small Arcane Energy payment, so the hand is a
    choice. **Note:** Marginalia's own comment (~8912) records why AE was rejected here - the run's AE is paid at its
    end from how it went, and a per-level-up drip could make late levels a currency farm. Eric has now asked for it,
    so size it small and flat (counted into the run's end-of-run total, `ArcaneRewardScale` applies), and check it
    against Endless, where a night can deal many of these (`.ai/endless-mode.md`: only the first night reaches the
    save, which limits it). Card in player words; routed like Marginalia (`Node2DGame` handles the option id).
    **Decided (Eric, 2026-11-10):** the Arcane Energy card pays **10**.
99. **Elderbark's attacks much bigger** (Eric, 2026-11-10: *"Elderbark's attacks can be much bigger in area"*). The
    chapter-one boss is `BossEnemy` used bare: its slam (`SlamRadius` 150, `BossEnemy.cs` 43) and the roots it sends
    up under the player (`RootRadius` 54, ~60, placed by `TelegraphedGroundHit`). Grow both - the roots most, at 54
    they are a small tile - and check the scene does not override the exports, the telegraph tells still give time
    to step out (`ValidateBossPatterns`), and that the other seven bosses, which inherit these defaults, are not
    grown by accident (override in their own subclasses or set the new sizes on Elderbark only). Drive it with
    `_BossProbe --stage=0` and look at it.
100. **Thorn Vine always knocks enemies away from the player** (Eric, 2026-11-10: *"thorn vine should always knock the
    enemies away from the player"*). Today the lash's shove (`ThornVine.cs` ~826-830, `VineShoveSpeed` 180) goes along
    the stroke - the arc's tangent at the body, the other way on Snapback's return - so on one side of the swing it
    throws enemies toward him. Make it the direction from the player to the enemy, keep the speed and the
    `ApplyKnockback` route (Baba Yaga's Bone Fence adds to it), and leave Entangle, which roots instead. Update the
    comment and the Baba Yaga line in `.ai/character-fit.md` section 5 ("thrown aside along the stroke").
101. **The Pinfold's guards stay by the gate, glowing and marked** (Eric, 2026-11-10: *"the guards should stay near the
    gate, and be glowing and marked so you notice them"*, after asking how to get into the pen). `Pinfold.Wake`
    (~156) spawns two guards (`SpawnEventEnemy`, 10x health) 300 px either side when he comes within 520 px; they
    then wander off with the swarm and are easy to lose, and the gate opens only once both are down and he touches
    the latch at its right end (`LatchReach` 64). Leash them to the gate (they hold a post beside it and return if
    drawn away), give them a glow and a mark over the head so they read as the gate's keepers (reuse the elite or
    event-enemy marking if there is one), and light the latch once both are down so the last step is seen.
102. **Aegis Ward's new icon is irregular and asymmetrical - redo it** (Eric, 2026-11-10, after 0.24.0). The icon
    redrawn under item 93 (`aegis_ward()` in `tools/art/spell_icons_core.py`: a steel heater shield, shaded face,
    bevelled rim, strapped with gold rivets, a lit dome, the ward's arc above) reads lopsided. Redraw it mirrored about
    its vertical axis - draw one half and mirror it, the generator's own way of keeping a shape even - and re-run the
    five evolution icons that reuse it (`evolution_icons.py`, `sc.aegis_ward`) through that script's checks.
103. **Land still flaps over water** (Eric, 2026-11-10: *"There are still flaps of land on top of water"*). Pieces of
    ground drawn over the water's edge, after the 2026-11-07 water pass (item 59: rounder bodies, overhang clearances).
    Find where first: photograph a shore in each water chapter (`_UiShot`, the Swamp, Forest and Cave styles) and ask
    Eric for a screenshot if none shows it. Likely suspects (`.ai/terrain.md`): the shore's ground overlay drawn past
    the water polygon's rounded edge (the corner cut and superellipse of section ~166 against the blob's two-lobe rim
    wobble), or ground-detail clutter and lily pads allowed up to 8 px over the bank (~268). The water polygon is also
    the wade and collision authority, so fix the drawing to match it, not the other way round.
67. **The Canticle's notes draw under the wizard** (Eric, 2026-11-09: *"Canticle song notes should render on a layer
    above the player"*). Cause: the chord's quavers (`ArcaneExplosion.EnsureMoteCanvas`, the `ChordMotes` canvas) are on
    `DrawLayers.Air` (6), and since 2026-11-01 the wizard's body is on `Foremost` (10), over every spell. Fix: put that
    canvas on `DrawLayers.ForemostAir` (11), the layer already holding the bow at his shoulder. Check the notes stay
    under `AttackIcon` (12) and the floating numbers, and that the blast itself stays on `Air`.
68. **Waywiser's description is very confusing** (Eric, 2026-11-09). The boon (`BoonCatalog.cs` 307, a Wind starter)
    makes whichever spell has the longest wait until its next cast recharge 40% faster (+10% a level) while the wizard
    walks. It is explained four ways, none plainly. The flavour line is a riddle: *"A wheel that counts the road, and
    pays out for every turn."* The rule line says *"While you walk, whichever of your spells is furthest from ready
    recharges 40% faster."* (`BoonCatalog.cs` 406). The card's stat line and the level-up line say *"the spell
    furthest from ready **winds** 40% faster"* (444, 479). The pause screen says *"Winding: X."* / *"Not winding - walk
    to wind the spell furthest from ready."* (`Node2DGame.cs` 4793). "Winds" and "furthest from ready" are code
    words (the player-words rule), and which spell it helps changes from moment to moment, invisibly. Options: (a)
    reword every line the same plain way, e.g. *"While you move, the spell with the longest wait recharges 40%
    faster."*, with a flavour line that says the same in the wheel's voice; (b) simplify the rule itself - *every*
    spell recharges a smaller amount faster while moving - which needs no explaining, but loses the "suits one big slow
    spell" design (`BoonCatalog.cs` 301) and needs a balance check against Taproot, its mirror.
    **Decided (Eric, 2026-11-09): reword - *"the spell with the longest cooldown"*.** Every line (flavour, rule, card
    stat, level-up, pause) says it that way, e.g. *"While you walk, the spell with the longest cooldown recharges 40%
    faster."* **Make the code match the words:** today the firing loop picks the spell with the most *seconds left*
    (`Player.cs` 4578-4583, `remaining`), which is usually the longest-cooldown spell but hands the boon to a fast
    spell that has just fired whenever the slow one is nearly ready. Compare `interval` instead of `remaining` there
    and the words are exactly true, the pause line ("Winding: X") stops flickering between spells, and the "one big
    slow spell" design is what it always meant. With two spells of equal cooldown, keep the first equipped.
69. **Is the Twinstar Sigil's added projectile overpowered?** (Eric, 2026-11-09: *"Two star sigil - is the added
    projectiles OP"*). The shop Power (`MainMenu.cs` 913): +1 projectile for every spell that fires them, **4 levels**,
    60 / 100 / 140 / 180 Arcane Energy (480 for all four). About 26 spells read it (`EvolutionStatReaders`,
    `EvolutionStat.Amount`), most starting at 1 projectile (`BaseProjectileCount = 1`), and it adds to the Split Lens's
    +1. **On paper, yes.** Level 1 alone, for 60, doubles a one-projectile spell's hits where Runes of Ruin's first level
    gives +8% for 30; all four take a level-1 spell from 1 projectile to 5, which also breaks "spells start small at
    level 1 and ramp with level" (Eric's rule). Vampire Survivors' Duplicator, the model, stops at 2 ranks. Options:
    cap it at 1 or 2 levels and refund the rest through `SaveData.RepricedPowers` (the pattern used for Fate Fracture
    and Emberheart, schema 20); or keep 4 levels with each extra projectile weaker; or make it the last unlock on the
    Power page. Measure before and after with `_BalanceHarness --all-unlocked` on a projectile wizard (the Archmage's
    Magic Missile, the Frostweaver's Frost Shard) - a save change, so check `CurrentSchemaVersion` on main (28) first.
    **Decided (Eric, 2026-11-10):** *remove the Twinstar Sigil* from the shop altogether (the "amount" Power: `MainMenu.cs` RegisterUpgrade, `MainMenu.Shop.cs` Power section, `Player.ApplyArcaneUpgrades` amountLevel; the Split Lens relic's +1 stays), and the release that removes it **resets progress again** (epoch 5) rather than refunding.
70. **The chapter note covers the player at the start of a run on a phone** (Eric, 2026-11-09: *"the stage description
    that shows up on the screen on mobile still covers the player right at the beginning which is annoying. We need to
    condense that and get it out of the way more"*). `ChapterNoteCard` (layer 7): on a phone it spans the width less 40
    a side and hangs from just under the HUD band (`TouchControls.DefaultHudBandHeight` 176 + 20 = y 196), growing
    down with its text - heading, two or three lines and the clues - while the wizard stands at the centre of the
    1280-tall canvas (y 640), so a note much over 440 tall is on him. It writes in at 0.028 s a letter and then holds
    7.5 s. To do: condense the phone version (the heading and one line, the clues shorter or as a count), cap its
    height well clear of the centre, and shorten the hold; a tap already puts it away. Photograph it before and after
    with `_UiShot --resolution 540x1212 --portrait --safe-area` on a first-visit save, in the chapter with the longest
    note.
    **Raised again 2026-11-10** (Eric: *"I meant the stage introduction that shows hints as to what is hidden in the
    level. it is huge on mobile and covers the wizard"* - he first said "the list of objectives", which was briefly
    recorded as a quest-tracker item; it is this note, its clues included).
71. **Consolidate the Powers pages** (Eric, 2026-11-09: *"The power ups pages can be consolidated more - for example
    the first page with just one power up"*). The shop pages by section (`MainMenu.Shop.cs` 144, `UpgradeSections`):
    Spell book (1: Bound Pages), Power (6), Survival (4), Fortune (4), Level-ups (5), then Boons - six pages, the
    first holding a single card. Bound Pages sits first on purpose (`.ai/slow-start.md` 4: the first buy teaches the
    shop), so keep it the first card, not its own page. A likely shape: **Spell book + Power** (7), **Survival +
    Fortune** (8), **Level-ups + Boons** - three pages - if a page holds that many cards at the phone's width; check
    both canvases with `_UiShot` and the pad's focus order across the merged pages (`MenuNavigator`).
72. **The "Dandelion Grove" is still much too common in the Forest** (Eric, 2026-11-09; first raised in item 59,
    2026-11-06/07: *"too common - why is it in more than one map?"*). It is the **flower pen** - the one landmark kind
    (`Landmark.cs`, `FlowerPenLayout`, art in `tools/art/ground_detail.py`): a fenced garden, 20 x 14 cells, round one
    pale moonflower, which since then carries the Forest's Fenced Bloom secret (walk a lap, `FencedBloom.cs`,
    `SecretCatalog.EnchantedForest`). Why it is everywhere (`Node2DGame.cs` 2663-2735): every 1536 px chunk rolls
    `LandmarkChance = 0.25`, and `LandmarkAttempts` went from 8 to 24 on 2026-10-28 when the pen grew, so nearly every
    chunk that rolls now places one - about one chunk in four, several a run, against the comment's "rare enough to be
    a find". And `HasLandmarks` is on for five chapters (`StageEnvironmentCatalog`: the Forest, the Swamp, the Ruins,
    the Cave, the Fallow Fields), though the secret is hooked only in the Forest, so in the other four it is an empty
    pen. To do: the Forest only (the other four off, or their own landmark later), and one or two a run - a far lower
    chance with a minimum spacing between pens, or one pen at a seeded anchor ring as `.ai/stage-secrets.md` places
    secrets. Keep at least one reachable: the First Tree's dead clearing waits for the moonflower's seed. Check with
    `_ExploreProbe` on the Forest that the secret still opens, and that `ValidateSecrets` / the decor margins hold.
    **Decided (Eric, 2026-11-09): *"Dandelion Grove should be only shown in one chapter"*** - the Forest, whose secret
    it carries: `HasLandmarks` off for the Swamp, the Ruins, the Cave and the Fallow Fields. And far rarer there, per
    his first line today.
73. **The Chalice Cup's description is confusing, and the cup goes useless** (Eric, 2026-11-09: *"Chalice cup
    description is confusing. Maybe it can heal you every period of time so it doesn't just become useless?"*). The
    Rare chest relic (`essence_chalice`, Water; `ChestItemCatalog.cs` 602, 702) heals 1 HP for each of the first 15
    kills after it is taken (`Player.EssenceChaliceKillHeals`, `Player.cs` 4149) - spent within seconds of the pickup,
    after which it is only a piece of the Tithe Chalice set (with the Heart of Renewal and Wormwood Tithe; the set heals
    1 HP every 15th kill). Its card says *"Each of the run's first 15 kills heals 1 HP. After them, it serves its set."*,
    which is wrong as well as unclear (the 15 count from the pickup, not the run's start) and "serves its set" is not a
    player's phrase. Eric's direction: **a heal on a timer**. Size it inside the healing budget (`.ai/difficulty-curve.md`
    5.1 - every relic or boon heal there sits near 9 HP a minute, the Heart of Renewal at 0.15 a second): e.g. *"Every
    20 seconds the cup fills and heals you 3 HP."* - a sip, which also keeps it apart from the Heart of Renewal's steady
    trickle in the same set (the Familiar's Purr is the same shape, 3 every 20 s). Show it filling (a small HUD or
    relic-icon fill) so the wait reads. Add its line to the healing ledger (`GameStats.TrackHealing`) and re-run the
    5.1 measurement; reword the set's card to match. A changed relic needs no save step - nothing a save holds moves.
    **Built 2026-11-09 (Eric: "Make the changes to the cup you suggested"), on `main-ff86ni`:** `Player.TickEssenceChalice`
    - the cup fills over 20 s (`EssenceChaliceSeconds`) and, once full, heals 3 HP (`EssenceChaliceHeal`) the next
    moment he is below full health; a full cup waits rather than spilling, so it never feeds the Vial's overheal and
    never pays more than 9 a minute. A "+3" in the Water ink rises over him when it is drunk (shown even with damage
    numbers off - it is the cup's only tell), and the pause screen's relic list says "Filling: N% full" or "Full - you
    drink it the next time you are hurt" (`ChestChaliceFillShare`). Card: *"Fills every 20 seconds, then heals 3 HP the
    next time you are hurt."* The clock stands still during a boss's arrival. The set's card (every 15th kill) needed no
    change. Verified: build 0 errors; validator 0 errors, 0 warnings; `_BalanceHarness --minutes=4 --give-relics=
    essence_chalice` shows `heal: Player.TickEssenceChalice 1.5/min` against 0.7 lost a minute - it drinks only when
    hurt. Not verified by eye: the "+3" in a real run, the pause line. No sound (once every 20 s at most; the audio
    contract would want a quiet one if any).
74. **The wizard's feet should sink when he wades** (Eric, 2026-11-09: *"When walking over water, we should make the
    feet of the wizard sink in (if they walk on the ground)"*). Today wading only slows him (`Player.IsWading`, a point
    query at his boots against water's layer 6 each physics frame, `Player.cs` 7763; `WaterWadeSpeedMultiplier` 0.45)
    and nothing changes in how he looks - only the swamp's mire rolls rings round a wader's feet (`MireSurface`); the
    ordinary water (`WaterSurface`) draws none. To do: while `IsWading`, sink the body sprite - drop it a few pixels and
    cut it off at a waterline (a `sink` amount in the body's shader, so the clipped part is never drawn, with a pale
    line where it meets the water), eased in and out over about a sixth of a second, the shadow and the stance glow
    hidden while he is in, and rings round him on every water style. How deep: about the boots and shins on the 48
    cell (`.ai/art-direction.md` proportions). **Who does not sink** ("if they walk on the ground"): Wayland whenever
    his wings hold him (`Player.Wings.cs`, `HoldsFlight` already keeps his flight over water); Baba Yaga, who rides
    her mortar (does a mortar float or sink? Eric's call); and Nimue, the Lady of the Lake, who could walk on the water
    rather than in it - Eric's call. The Swallow-Hole's mud (`SwallowHole.cs`, its own `IsWading`) should sink him
    too. Not the abyss: the Bridge's water is not walkable (`WaterWalkable = false`). Check that it reads under the
    `Foremost` layer's rule that the wizard is never lost under an effect, with `_UiShot` in a chapter with water.
    **Decided (Eric, 2026-11-09): Baba Yaga and Nimue both stay on top** - her mortar floats, and the Lady of the Lake
    walks on the water (rings round her feet, no sinking). Wayland, flying, stays on top too.
    **And neither is slowed by water** (Eric, 2026-11-09: *"Baba Yaga and Nimue should also ignore being slowed by
    water"*). Today only Wings skips the wade's slow (`Player.cs` 7765, `IsWading && !Winged`). Give the two a trait,
    not a passive - each keeps her own - e.g. a `CharacterData` flag *rides over water* read beside `Winged` there and
    by the sinking (one rule for "does not sink" and "is not slowed"); the Swallow-Hole's mud should ask the same.
    **Mind Wayland:** water was half of his passive, Wings (water and floor traps); after this his floor-trap half is
    his alone, so check his card in `.ai/character-fit.md` still sells what is unique to him.
75. **The Fenced Bloom's ("Dandelion Grove") heal wears off** (Eric, 2026-11-09: *"the dandelion Grove effect should
    wear off after it heals you for a while"*). Once its lap was walked, the garden healed 0.5 HP a second to anyone
    inside it for the rest of the run - 30 a minute, against the budget's 9-a-minute sources, and with a pen in every
    other chunk. **Built 2026-11-09, on `main-ff86ni`:** `FencedBloom.SanctuaryBudget` = 20 HP a pen (forty seconds of
    standing), counted in the pen's own `SiteState.Step` so a pen forgotten with its chunk remembers; the open flower's
    glow shrinks as it is spent and goes out with it. The opening's half-bar heal is unchanged. The secret's reward
    line now says *"the garden heals you for a while as you stand in it"*; `.ai/stage-secrets.md`'s sanctuary row says a
    healing sanctuary wears off. Build 0 errors, validator clean. Not verified by eye: the fading light.
76. **Show Aegis Ward recharging on the spell bar** (Eric, 2026-11-09: *"Show aegis ward recharging on the spell bar at
    the top of the screen"*). The equipped-spell row (`LoadoutHud`, the 48-pixel slots under the health bar) shows no
    cooldowns today. Give Aegis Ward's slot a recharge sweep (its `spellFireTimers` against its interval) and a
    full / broken state, so the player can see when the shield is back; consider the same for the other defensive
    spells built on its rules (Stormguard, Gloam Veil). The row redraws on `Player.LoadoutChanged` only, so this needs
    a cheap per-frame redraw of one slot, not the whole row (`.ai/performance.md`). **Also: its tagline is wrong** -
    *"Soaks hits; recharges once broken."* - the cast branch (`Player.cs` 4793) tops the shield back up to full every
    cooldown whether it broke or not.
77. **Is Aegis Ward's recharge too fast? A balance pass on every spell** (Eric, 2026-11-09: *"Is it's recharge too
    fast? Mark that we need a balance pass on all spells next week"*). Aegis Ward on paper: an 8-point shield refilled
    every 9 s at level 1 (up to ~53 HP a minute absorbed, against a 50 HP bar), 31 points every 6.5 s at level 8 (~286
    a minute), and its cooldown also shortens with attack speed (Tempest Rhythm, up to +128% from the shop) - against
    a healing budget that pays about 9 HP a minute a source, it can erase most chip damage, so probably yes. **The
    pass, for the week of 2026-11-16:** every active spell's damage, cooldown, count and reach measured the same way
    (`_BalanceHarness --spell=<id>` on an ordinary wizard, `--all-unlocked`, fixed seeds), against each other and
    against the spells-start-small rule; the defensive spells against the healing budget (`.ai/difficulty-curve.md`
    5); fold in item 69 (Twinstar Sigil). `.ai/spell-roster.md` is the roster's design.
78. **Finish all of the far shore's characters** (Eric, 2026-11-09: *"We should also finish making all the
    characters for far shores next week"*). F6-F10 in `.ai/second-half.md` section 9: Manannán mac Lir (Maelstrom,
    the Drowned Shore), Hecate (Dark Lens, the Three Ways), Ra (Orrery, the Twelfth Hour), Ukko (Hailfront, the
    Thunder Fell) and Hephaestus (Anvil Line, the Bronze Isle) - each a Conjunction (M), a god (M) and a chapter with
    its cast and boss (L), plus the closing reel with F10. Built on F1's plumbing, as Artemis was (F2/F3); gods are
    appended to `CharacterRoster` in the order they merge. Keep it separable for the DLC (item 64). Large: five
    slices, best as parallel worktrees, one per god.
79. **Baba Yaga's hut is too hard to herd** (Eric, 2026-11-09: *"Baba Yaga's hut should head toward the yard if you're
    near it at all, it's too hard to herd it"*). Today (`FleeingMover`, `WalkingHutEvent`) it flees straight AWAY from
    the wizard - a bolt within `StartleRadius` (420), a stride after it, an unhurried turn-off out to `LoseRadius`
    (1100) - so the player must get round its far side and press it, through a yard whose one gap faces away from its
    circle (`.ai/unlock-puzzles.md` 3.3, Eric's choice of 2026-11-03). Change: whenever he is near at all (it is
    `Fleeing`), its way is TOWARD THE YARD - the gap's mouth first, then the middle - with "away from him" only a
    small push so it never walks through him; it still bolts, tires and is cornered as now. The validator encodes the
    old rule and must change with it: `RegressionChecks.WalkingHut` warns if a straight chase drives it in ("only
    herding should") - that becomes the expected case. Update 3.3 in `unlock-puzzles.md` with Eric's words.
80. **The hut changes facing too often** (Eric, 2026-11-09: *"it freaks out and changes facing too often"*). Its wall
    (back, front, side) is chosen every frame from the mover's raw `Heading` with only a 1.15 ratio of hysteresis, and
    `FlipH` follows the sign of the heading's x whenever it is past 0.2 (`WalkingHutEvent.cs` 664-682); the heading
    itself snaps to whichever probe direction is open (`FleeingMover`'s `Turns`) and to "away from him" as he moves. So
    every dodge round a tree or turn away from him shows as a flip. Fix: face a smoothed heading (low-pass it for the
    drawing only, never for the movement), hold a wall for at least about half a second before changing it, and give
    the flip the same hold and a wider dead band. Item 79's steady heading for the yard will calm it as well.
81. **Buckler Boss: "while a shield holds" means nothing to a player** (Eric, 2026-11-09: *"Buckler boss - while a
    shield holds? What does that mean?"*). The relic (`protective_ward`, `ChestItemCatalog.cs` 597, 695; a piece of the
    Thorned Buckler set) adds 15% defense - damage reduction - while the wizard has any shield points (`Player.cs`
    2346, `TakeDamage` step 5). Shield points come from only four places: Aegis Ward, the Vial of Vitality (healing at
    full health becomes shield), the Reliquary set (a 6-point shield per chest) and one of the Court's rooms. Without
    one of them it does nothing, and chests offer it regardless (`GetChestUnavailableItems` has no rule for it). To do:
    say it in player words - e.g. *"Take 15% less damage while you have a shield (from Aegis Ward or the Vial of
    Vitality)."* - and either offer it only once a shield source is held, or give it a small shield of its own so it is
    never dead. Check the card's length on the phone.
    **Eric, 2026-11-09: *"Let's rebalance Buckler Boss so it does something that makes more sense"*** - a rework, not
    just a reword. A buckler's boss is the metal dome at its centre, used to punch and to turn blows aside. Candidates:
    (a) **its own small shield** - a few points, raised again every so often - so it is a shield source by itself and
    feeds its set (the Thorned Buckler) and Aegis Ward builds alike; (b) **turns bolts aside** - an enemy bolt that
    reaches him is knocked away every few seconds (close to Medea's Salve and Aegis Ward's Reflective Plating, so
    check those); (c) **a punch** - enemies that touch him are shoved back hard (the set's Iron Fang already cuts
    them). (a) is the plainest and never dead; Basalt Carapace's "ignores one hit every 10 s" is the one to keep it
    clear of. Size it against the healing budget's shield lines (`.ai/difficulty-curve.md` 5).
    **Decided (Eric, 2026-11-10):** (a) - Buckler Boss gives its own small shield, a few points rebuilt every so often, so it is a shield source by itself; reword its card to say so.
82. **The hut kneels only in the middle of the bone yard** (Eric, 2026-11-09: *"Baba Yaga's hut should only kneel
    down once it's in the center of the bone yard"*). Today it is cornered, stops and kneels as soon as it is inside
    `CorneredRadius` - 96 of the yard's 130 px (`WalkingHutEvent.cs` 53, 561) - which is just past the gap. Make it
    walk on to the middle (a few tens of px) before the kneel; with item 79's heading for the yard it walks there
    itself. `RegressionChecks.WalkingHut` checks `CorneredRadius` against the yard; keep its herding chase passing.
83. **Every unlocked character appears on screen for a moment** (Eric, 2026-11-09: *"When we unlock a character, they
    should appear on the screen for a moment, like Baba Yaga coming out of her hut"*). Eleven sites already have a
    step-out beat for the wizard they free (the hut, the barge, the gibbet, the seer's voice, the tomb beacons, the
    tithe scale, the dotted door, the five-pointed seal, the great anvil, the nine mouths, the Court's Seneschal - grep
    `StepOut`). The ones freed any other way get no moment in the run, only the freeing reel afterwards
    (`Freeings`, `.ai/cutscenes.md`): John Dee (an achievement), Laelaps (the Pinfold secret), the Dark Wizard (Taker
    Taken), Nimue (the Throne's fall), Artemis and the gods (their rekindling) - and any other achievement unlock. To
    do: one shared appearance beat - the wizard's own sprite stepping out into a column of light beside the player
    with their name, a couple of seconds, then gone - played at the moment the deed is done, never while a boss
    arrives or a menu is up (`Announcements` holds the screen's turn). Check the eleven existing beats read the same
    way, and that none plays twice with the post-run reel. Follow `.ai/cutscenes.md`'s pacing (pause before and after).

## 0. Then (2026-11-03, after the 0.12.0 release)

**0.12.0 is released** (`Release v0.12.0`, 31233f2a, tag `v0.12.0`), with **story epoch 2**
(15d35a38): every player's progress restarts the first time it runs, their old save kept as
`savegame.before-epoch-2.json`. It is the first release under the new name, and carries the bridges
for copies from before the rename. Rehearsed first with `--dry-run` on a throwaway local branch, so
`main-ff86ni` never held epoch 2 before the release commit. Both update feeds
(`ecec.dev/the-one-he-missed/version.json` and the old `ecec.dev/wizard-survivors/version.json`) serve
0.12.0 with `progressEpoch` 2. `main` was fast-forwarded to the release.

What it carries beyond 0.11.0: the rename; the Archmage (the fifth starter, first on the select) and
the four's names (Aedan, Isbrand, Thora, Gabbro); the castle open to the end (the Bridge, the Dungeon
moved into it, the Court, the Throne, the Dark Wizard, the ending, the Dark Wizard playable); **every
one of the ten chapter wizards** (`.ai/chapter-wizards.md`), Merlin in a look of his own and Nimue
(Excalibur, the Throne's kept wizard); P2, two side events in a chapter; the slow start; the
cutscenes and freeing reels; the cinematic rules; the light pool in the Dungeon and the Cave only
(Eric: *"Keep to two"*); Boss Rush's shorter arrivals; the chest lean (#44).

**Merged since the release** (2026-11-03, each built in its own worktree, three at a time - section 6:
five at once ran into the usage limit; not in any release yet):

| Branch | Merge | What |
|---|---|---|
| `art/merlin-ring` | 5f198e1a | the Orb of Ages' ward, star and orb on the 2 px grid; its 7 icons in his quartz (`.ai/merlin.md` 7.6) |
| `fix/tip-overlap` | 53d400f7 | `HudBand`: the rows under the timer stack, so a tip is never drawn over an objective line; chapters numbered by their place on the road |
| `feel/damage-colours` | fe26e592 | damage numbers wear the element that dealt them, outlined (`DamageNumberInk`, `.ai/art-direction.md`) |
| `art/forest-foliage` | e228232c | the Forest pilot: stands, fringes and clearings (`Woodland`), three bush sizes, grass and specks (`.ai/terrain.md` 13) |
| `castle/oubliette` | c1fc933b | D7, the Oubliette: a grate in a dead-end Dungeon cell, a voice below, Nimue's card turned before the Throne |

Not started: scenery-only landmarks per chapter (after Eric judges the foliage pilot, same painter);
the foliage pass on the other chapters (likewise); the Play Store listing as *The One He Missed:
Survivors* (`.ai/game-name.md`, Eric's Play Console).

**Waiting on Eric's eyes** (none of it measurable by a harness):
- The foliage pilot (`_shots/forest-foliage/composite-pc.png`): do the stands frame the fight or trap
  it (the bot walked into one and was pinned among trunks); the moss under a stand is about 4x the
  turf's speckle; a crown at the screen edge can hide a gem; on the phone the new grass reads as texture.
- Damage colours: crits are one stop hotter and come out near-white and alike (Arcane, Metal, Light) -
  the alternative is the element's own colour with crits marked only by "!" and size; the turned blow
  is bone grey now; Poison and Lightning sit close; Wind is the dimmest on the Forest.
- Merlin's ring: the stroke the orb flies is twice as bold; the orb grows with Great Circle; the quartz
  icon at 32 px.
- The Oubliette: the voice is measured, never heard; the motes are small; 5 s unstruck against the
  Dungeon's swarm has not been played; her hint names the Throne before the map shows it.
- Nimue: the float and its shadow at game size; the Scabbard's size; the sever line in motion; "the one
  he kept" and her freed line.
- Ma Liang: whether tracing the door under a crowd on a phone stick is a challenge; the dot numbers on
  a phone; the brush sound; the flask; the Painted Door in a crowd. His Apotheosis came at 7.3 min,
  under the 7.5 floor.
- The light pool's depth, and Reduce Darkness at 40% or truly off (`.ai/light-pool.md` 9).
- Every boss arrival's length in real play.
- #44's chest playtest (the only open GitHub issue).
- Nothing from 2026-11-03 has been seen on the Pixel 9.

Everything below is the history from 2026-11-01 and 11-02. **Sections 4 and 7 still list open
items**; check each against the code before acting on it.

---

# Work in flight (snapshot, 2026-11-01, updated after the 0.10.0 release)

A handoff from the cloud session that ran 2026-10-30 to 11-01. Eric moved the work to a local Claude
Code session.

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
   **Checked 2026-11-07 (the audio batch; this is item 60's stems question, answered)**: the stems play
   exactly as designed and the menu-ish read is by construction, not a bug. A headless Forest run logs
   `set 'forest' started at bar one, 4 stems locked (forest_bed 0 dB, forest_pulse 0 dB, forest_lead
   0 dB, forest_boss -80 dB), in over 0 s, solo out over 1 s` - battle percussion from the first
   downbeat, the theme gone in a second - and then `run music lead out at 00:24`. That second line is
   the diagnosis: the lead is the only stem that carries a chapter's identity (the flute, the brass,
   the shawm, the music box), and it rests from 0:24 until an elite or pace minute 5, the longest
   unbroken stretch of a run. What remains is the bed - which is still the menu theme's own
   instruments (strings, drone, choir; `analyse.py --music` puts every bed's centroid at 584-681 Hz
   against the menu theme's 614, same bands, same seven notes) - plus drums. So minutes ~0:30-5:00
   read as "the menu theme with drums", in every chapter. Three fixes, Eric's pick, none made because
   each needs ears: (a) code only - shorten the lead's rest (`Node2DGame.MusicLeadLateRunPaceMinutes`,
   `MusicOpeningLeadSeconds`), or let the hook return one section per loop; (b) re-voice each bed away
   from the menu's palette (scoring + re-render of eleven beds); (c) re-voice the MENU theme instead -
   one piece, one render - so the strings-drone-choir bed reads as the game's sound, not the menu's.
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
  he missed", and **not Merlin**: Merlin gets a look of his own. He was to be story only (title, prologue);
  later the same day Eric made him **the fifth starter, The Archmage**, first on the character select, on Magic
  Missile made pure Arcane (`.ai/protagonist.md`, `wip/protagonist`). Building:
  the rename (`wip/rename`), the title art for the new name (`wip/title-art`), Merlin's new look
  (`wip/merlin-art`); the website's half is PR eclancy/EricClancyEngineeringAndCrafts#48, held until
  the GitHub repo is renamed (old `/wizard-survivors/version.json` and `/download` keep answering for
  installed copies). Merge order: rename the GitHub repo, merge the site PR once its CI (`nginx -t`)
  passes, then the game rename.
- **Merged since:** Wayland (`56097665`, index 12) and the Sibyl (`4738a49e`, index 13, hint row 8).
  **Building:** P2, a second event per chapter (`wip/second-events`).
- **Done, 2026-11-02/03: the rename is live.** GitHub `eclancy/TheOneHeMissed` (the old address
  redirects), ecec.dev's site PR #48 deployed (`/wizard-survivors/version.json` and `/download` still
  answer; checked with curl), the game merged as `0aae133a` (namespace `TheOneHeMissed.scripts`,
  `TheOneHeMissed.sln`). **Not yet released:** the first renamed release must ship the bridge
  (`.ai/releasing.md`, "The rename") so 0.11.0 copies update. **On Eric's first real launch** of a
  renamed build, `UserDataCarryOver` copies his old user folder (752 MB, mostly dev shots) into the
  new one - about 8 s, once. **Any branch cut before `0aae133a`** needs the namespace command from
  `e2898cc2` re-applied at merge (`git grep -l -a -z 'WizardSurvivors\.scripts' -- '*.cs' | xargs -0
  sed -i 's/\bWizardSurvivors\.scripts\b/TheOneHeMissed.scripts/g'`).
- **Release: once, when the whole pipeline is finished** (Eric, 2026-11-03: "I only want to release once
  we've finished every change we have in the pipeline"). That release is also the first renamed one
  (the bridge) and the story reset (epoch 2).
- **Eric's answers, 2026-11-03:** nerf Magic Missile (pure Arcane made the Archmage clear the slow
  start's measured run far above the starters'); redraw the four in the freeing with their real sprites
  (they were flat colour cut-outs); "You went back for them." stays.
- **Merged:** Merlin's own look (`fef6641b`, the Crystal Cave; B and C on the sheet), the title art
  (`70581528`, A the stack, Eric's pick), P2 (`6ceeaf20`). **Building:** the fifth starter **The
  Archmage** (Eric's name; the blue-robed player figure, Arcane, Magic Missile made pure Arcane, first
  on the character select; `wip/protagonist`), the cutscenes (he frees the four; `wip/cutscenes`),
  Baba Yaga (`wip/wizard-babayaga`). Then Morgan, Paracelsus, Imhotep, Medea, Nostradamus, Ma Liang,
  Nimue.

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
