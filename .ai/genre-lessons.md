# What two other games in the genre teach us

Eric, 2026-11-03: *"lets look at the atmosphere from the game of the steam listing for Wizard Survivors. Look at
the density and variation of foliage, the art for landmarks in game, the dynamics in the lighting"*, then their
videos, then *"the trailers and menu and design direction of vampire survivors"*.

**Looked at:**
- **Wizard Survivors** (Jam Studios, [Steam 3146730](https://store.steampowered.com/app/3146730/Wizard_Survivors/)):
  six screenshots (two of play), its 1:27 trailer, and a 4-second clip in its description.
- **Vampire Survivors** (poncle, [Steam 1794680](https://store.steampowered.com/app/1794680/Vampire_Survivors/)):
  seven screenshots, the Refreshed trailer (about a minute), and its store capsule.

All of it was watched in the built-in browser, nothing downloaded. **Principles only:** every pixel of ours stays
original and generated (`CLAUDE.md`, the original-art rule). Nothing here is to be traced or matched by eye.

**Our game today**, for comparison: the Forest at 1280x720 (`_shots/merlin/merlin-orb-pc-2.png`), lit evenly from
edge to edge, a flat dark-teal ground with soft blotches, about eight bushes, one birch stand and a pond.

## 1. Light: the player is the lamp (Wizard Survivors)

**The lesson that matters most.** Prototyped 2026-11-03 for the Cursed Dungeon and the Sunken Cave only
(Eric: *"The light pool can possibly be used in certain levels? like the dungeons"*): `.ai/light-pool.md`.

- **What they do.** A large warm pool of light sits on the wizard and moves with him. It falls to near-black at
  the screen's edges. Enemies walk out of the dark into it.
- **Spells are lights too.** Each fireball carries its own warm halo and stays visible out in the dark.
- **Why it fits us.** Our premise already says it: *"You carry the only warm light on screen. Everything else
  glowing is something you made"* (`.ai/world-and-tone.md`). The title screen draws exactly this, gold ward light
  on a blue night with eyes in the dark. The run does not.
- **How it would work.** One full-screen pass between the world and the HUD: a warm pool centred on the player,
  falling off into the cold Bonelight dark (`.ai/art-direction.md` section 2). Warm spells and the ward add small
  pools of their own. No per-object `PointLight2D`, so it stays cheap on the Pixel 9 (`.ai/performance.md`).
- **Guardrails:**
  - a floor on the darkness, so enemies at the edge stay readable;
  - enemy eyes stay lit in the dark, as the title's are;
  - the low-health vignette (`Node2DGame`) must stay distinct from it.
- **What not to take:** their single yellow cast tints everything one hue and muddies the enemies at the edge.
  Our cold world with one warm light is the stronger version.

## 2. Foliage: a size ladder, packed dense (Wizard Survivors)

- **What they do.** Four sizes at once: canopy trees, round bushes in two or three sizes, grass tufts, and specks of
  flowers and pebbles. Trees mass into dark stands at the edges, framing open clearings to fight in. The ground
  never reads as flat.
- **What to take.**
  - Raise the small clutter in the ground-detail layer (`LevelTilePainter`'s per-chunk `TileMapLayer`). It draws
    in batched quadrants, one node a chunk, so hundreds of tufts cost little.
  - Cluster the bushes, and let the trees mass into stands with clearings between them.
- **Guardrail:** clutter stays lower in contrast than enemies and XP gems. The silhouette is how the swarm is read.

## 3. Landmarks that are only scenery (Wizard Survivors)

- **What they do.** A stone post, a stone ring, mossy gravestones. Grey worked stone against organic green breaks up
  the repetition. A run opens beside the ring, so the first minute has a place.
- **What to take.** Non-interactive ruins and set dressing per chapter, so not every landmark means a secret. The
  interactive ones keep the warm glint and stand out by it (`.ai/stage-secrets.md`).

## 4. The scale of the chaos (Vampire Survivors)

- **What they do.** Late in a run the screen holds hundreds of enemies, a carpet of XP gems, and weapons that fill
  it: arcs, lightning lines, pools. The excess is the product. In its trailer a player calls it *"the most pixel
  vomit I've ever seen"*, and it is meant as praise.
- **What it means for us.** Our cap is 140 on screen (`Node2DGame.MaxEnemies` x `WideViewEnemyCapScale`) for the
  phone's sake.
  - The late-run power fantasy may want more on screen than that: cheaper fodder, or the gem carpet itself as the
    spectacle.
  - Measure before moving the cap (`.ai/performance.md`, `.ai/run-pacing.md`).

## 5. Readable without any lighting (Vampire Survivors)

- **What they do.** Flat light everywhere. Readability comes from three things:
  - dark enemy silhouettes;
  - bright blue gems;
  - a clear ring the player's weapons keep cut around him.
- **The warning for section 1.** Lighting is not what makes a hit in this genre; reading the swarm is. The light
  pool must never cost readability.

## 6. Stage identity from tiles, not light (Vampire Survivors)

- **What they do.** Each stage is told apart at a glance by its ground and walls. The library is a wall of
  bookshelves; the forest has dirt paths through grass.
- **What it means for us.** We already do this per chapter (`.ai/terrain.md`). Keep each chapter's floor and
  edges distinct before adding anything else to it.

## 7. Menus (Vampire Survivors)

- **Chunky framed panels with big, high-contrast buttons:** START in green, BACK in red, OPEN in blue. There is
  always one obvious next press.
- **Painted key art behind the menus,** larger and more detailed than the play sprites: the villain and the hero.
- **Counts as motivation.** The collection screen reads *Collected: 124 of 127*. Our Codex and achievements already
  carry counts (`CodexRows`); a total on the top-level menu entry would put the chase in front of the player.
- **The stage select shows what the run will be:** its time limit, its bonuses, toggles for modes, and the stage's
  music.

## 8. Payoff moments (Vampire Survivors)

- **What they do.** "Treasure Found!" stops the run for one big chest and one big OPEN button. The chest is a
  jackpot moment, built to be felt.
- **What to take.** Our chests and level-ups should each have one moment that lands (sound, motion, a pause), not
  only a menu (`.ai/level-up-screen.md`, `.ai/audio-direction.md`).

## 9. Trailers and the store

| | Wizard Survivors | Vampire Survivors | for ours (`launch plan` doc) |
|---|---|---|---|
| opening | wave 1, a few skeletons | an extreme close-up of a sprite in a wall of fire | open in a full fight |
| cards | "Unleash diverse spells", "Choose your wizard" | short feature cards ("New local co-op") | the story line instead of a feature list |
| proof | none | player review quotes over the chaos | quotes once the closed test has testers |
| menus shown | about a third of it | a level-up card, briefly | at most one beat |
| ending | a skill-tree screen | painted villain key art, logo, every platform badge | the payoff, then the title |

- **Their store capsules differ:**
  - Vampire Survivors' is a bright painted illustration, not pixel art: the hero large, glowing blue weapons, a red
    moon.
  - The villain is the face of its brand: his glowing red eyes fill the end card.
- **For our feature graphic,** consider a larger composition with the Dark Wizard looming behind the Archmage,
  generated in our own pipeline at high resolution, beside the title art.

## 10. What not to take

- **Vampire Survivors' humour** (roast chicken in the walls, jokey stage text). Ours is epic and serious by decision.
- **Wizard Survivors' single colour cast and slow trailer.**
- **Anyone's art.** Principles only.

## 11. Recommended order

1. The light pool, with warm spells as their own small lights.
2. A foliage density pass on the Forest as the pilot.
3. Non-interactive landmarks per chapter.
4. Element-coloured damage numbers (Wizard Survivors tints them per spell; ours are red whatever the spell).
5. Measure the late-run enemy count against the phone before deciding on more.
