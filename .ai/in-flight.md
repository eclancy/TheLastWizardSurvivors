# Work in flight (snapshot, 2026-11-01 early)

A handoff from the cloud session that ran 2026-10-30 to 11-01. Eric moved the work to a local Claude
Code session. **A snapshot, not a source of truth:** check `git log` and the branches before acting
on it, and delete this file once everything below has landed or been dropped.

## 1. Released

**0.9.2 is released** (`Release v0.9.2`, 614ad5e, tag `v0.9.2`). The next release is 0.10.0
(`bash tools/release/release.sh X.Y.Z "..."`, on the Windows PC only: Inno Setup, the Android
release keystore and the Android SDK live there; `.ai/releasing.md`).

## 2. Where the branches stand

- **`main` is at 048fe79** and releasable as 0.10.0 (achievements for every spell, schema 19, the
  nine vaults, castle groundwork). It has **not** been moved since.
- **`main-ff86ni` is the development branch** and is far ahead. Every merge into it was built,
  validated (0 errors, 6 warnings: five castle to-dos and the Test Wizard) and pushed. **Hold it
  out of `main` until Eric has played the difficulty curve**: it changes how the whole game feels.

Merged into `main-ff86ni` since `main`:

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

## 3. Branches unfinished at handoff

The cloud agents on these were told to commit their work in progress and push, then carry on. If
the cloud container was reclaimed first, the branch holds whatever was last pushed. Read the
branch's own doc section for what is left; **nothing here is merged**.

| branch | what | state |
|---|---|---|
| `wip/boss-entrances` | spells stop firing while a boss makes a themed entrance; every boss gets at least two phases; fights a few minutes longer; Boss Rush scaling | measuring. Farmed fight lengths so far: Gaoler 3.7 min (was 1.3), Ninth Stone 2.4 (1.6), Hollow Choir 6.0 (0.1; its health can come back down), Mother Rot 3.0 (0.6), Archivist 5.2 (2.1), Long Coil 4.2 (0.9); Forest fresh 2.9 (1.2). Two runs had weak bots, so a same-code baseline was planned. Still to do: Still Warden, Warden of the Deep, Boss Rush, entrance screenshots. Status in `.ai/enemy-behaviour.md` |
| `wip/quests-notes` | chapter notes in the wizard's voice (held wizard by epithet, never by name), a journal, a quest log, tracked quests on the HUD, the default quest always "free the next wizard", big spell-animated text at run start shrinking into the corner | built and validated (0/6); screenshots were next. `.ai/quests-and-notes.md` |
| `wip/evolutions2-blasts` | phase 2 cards for Arcane Explosion, Fireball, Meteor Swarm; also the Event Horizon card, which is wrong under Dual Shockwave (it pulls rather than throws) | Arcane Explosion partly done (08ba52b and after). `.ai/spell-roster.md` 2h |

Every other `wip/*` branch is fully merged and can be deleted on GitHub.

## 4. Waiting on Eric

- **Play the difficulty curve** (50 HP, scarce healing, tougher chapters and bosses) before
  `main-ff86ni` goes into `main`.
- An achievement banner shows mid-run, but quitting from the pause screen forfeits the deed (only
  the run's end pays it). Record it on the spot instead? One line.
- The Sketchbook stays open for saves that already played it. Hide it until the campaign is
  finished for everyone? One line.
- The prologue shows the Dark Wizard once, unnamed; `.ai/world-and-tone.md` tone rule 5 says he is
  never on screen until he is. Amend the rule or change the panel.
- Thorned Vine wraps and cracks on the enemy it catches in a dense pack. Keep?
- 104 mutation and Ascension descriptions were rewritten to fit 130 characters; worth reading for
  tone. Some numbers left the cards (Anathema's, Threefold Curse's, Cascade's).
- Needs eyes in motion: the ban tear (rip, not crack?), Mjolnir's chained strikes, the cat's return
  and the Purr heart on a phone, Black Cat's casts, the Rampart dome, the whip loops, the map
  reveal's darkness edge on a phone.

## 5. What comes after

1. Land the three branches above, then a playtest, then `main-ff86ni` into `main` and 0.10.0.
2. The castle, by `.ai/endgame-castle.md` section 9.2: C6, C5, C4, C2 (**schema 21**), C8, C9;
   each chapter's `IsPlayable` flips last.
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
