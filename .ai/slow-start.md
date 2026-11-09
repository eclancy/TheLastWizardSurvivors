# The slow start: a thin spell book that fills, and systems that arrive one at a time

Eric, 2026-11-01:

> "I also want to introduce a slow start to the game so new players don't get overwhelmed. When
> starting the story, let's limit the number of spells you can have equipped, and have unlockables to
> increase that, like additional spell pages in the shop. In addition, we can unlock combination spells
> and ascension via achievements or story progression. Let's plan out a good pace."

**Status: answered by Eric (2026-11-02), built on `wip/slow-start` (section 10).** His answers, which win
over the table below where they differ:

- **New systems are unlocked by an achievement and taught in a post-run cutscene.** *"I think new
  systems should be unlocked in a post-run cutscene, after fulfilling an achievement, like finding a
  landmark. The cutscene can then explain the new system."* So questions 4 and 5 are answered
  otherwise: the Ascension, the Apotheosis and Conjunctions (and the Sovereign Rite after them) each
  open when the save completes a chosen achievement - finding a landmark is his example, which
  points at the stage secrets (`.ai/stage-secrets.md`) - and the next time a run ends, a short
  cutscene plays before the results to show and explain what was learned. The cutscene is the
  prologue's kind (`.ai/prologue.md`, `Prologue.cs`: generated panels, a line each), one per
  system, played once per save and replayable from Options beside the prologue. Which achievement
  teaches which system is still to be chosen and put to Eric with the build (a design pass first).
- **Pages 5 and 6 from the shop** (question 3 as recommended); the 4th from the Survivor Feat
  (question 2 as recommended).
- **The next release resets everyone's progress** (question 11): it raises
  `SaveData.CurrentProgressEpoch` to 2, so every save starts the slow start and no schema step is
  needed to keep six pages for played saves. Raise the epoch **at release time**, not on a branch: a
  Godot run of a branch with the new epoch restarts the developer's real save (backed up first, but
  still). The 0.10.0 updater does not know to warn before it downloads (`.ai/releasing.md`); the
  backup is still written on first launch.
- **Questions 1, 3, 6, 7, 8, 9 and 10: yes, as recommended.**

### Systems, and what teaches each (chosen by the integrator for Eric's review, 2026-11-02)

Built on `wip/slow-start` (section 10). One line each, with why:

| system | taught by | chapter | why |
|---|---|---|---|
| **the Ascension** | **the Pinfold** (`secret_pinfold`, the Fields' spell vault, pays the Familiar) | 2, the Fallow Fields | A landmark, as Eric's example asked: in every run of the chapter, five to seven chunks out, with a far tell (`achievements-and-secrets.md` 6.4) - findable by design, and the chapter the doc already gave the Ascension. The Hayward's pound: the first thing he kept from us. |
| **the Apotheosis** | **the Fallen Star** (`secret_fallen_star`, the Circle's vault, pays Starfall) | 3, the Broken Circle | The circle where the wizards met, and a star that fell there: a wizard's final form is learned where the wizards were last whole. The doc's chapter for it. |
| **Conjunctions** | **the Geode Heart** (`secret_geode_heart`, the Cave's vault, pays Frost Shard) | 4, the Sunken Cave | A crystal grown round a light: two things grown into one stone. The doc's chapter for it, and the first chapter with no side event, so the vault is the chapter's one find. |
| **the Sovereign Rite** | **the Warden of the Deep** (`emberdeep_ended`), unchanged | 10, the Emberdeep | Eric's instruction: it keeps its deed, and gets its lesson reel. |
| **the pair shares its light** | **the Archivist** (`ruins_delver`, the Mystic Ruins' boss deed, "Unlocks Spiritual Weapon and teaches a lesson") | 6, the Mystic Ruins | Added 2026-11-09 by rung III of the Conjunction ladder (`.ai/conjunction-progression.md` section 6): the keeper of the books teaches how two pages are bound. Owed only once Conjunctions are learned, so it is never taught before the system it eases, nor on the same evening; its reel is the Conjunctions lesson's two panels with lines of its own. Nothing of it is shown before it is learned - the deed's "teaches a lesson" is its one locked thing on show. The ladder's other rungs are not lessons: rung I (a recipe known from one half, the pin, the level 4 mark, a wizard's Suits) comes with the Conjunctions lesson, rung II (Kindred Draw) is a shop row shown from then, rung IV (Nostradamus foretells) arrives with his freeing. |
| **the far sky** | **Artemis rekindled** (`Teachings.FarSkyTeacherId`: whoever is seated at the far shore's first chapter) | Far Shore 1, the White Downs | Added 2026-11-08 by piece F4 of `.ai/second-half.md` (section 9, "F4, built"): a fifth lesson, owed as the Rite's is - by a deed, not a vault - and played after her rekindling reel. Nothing of it is shown before it is learned. |

**The three are a ladder, not three fixed pairs.** Whichever teacher a save finds first teaches the
Ascension, the second the Apotheosis, the third Conjunctions (`Teachings.Owed`). In campaign order - the
common path - each landmark teaches its own row above. Out of order (he walked past the Pinfold and
found the Fallen Star) the find teaches the lowest system not yet learned, so nobody is taught a system
he cannot use yet (the Apotheosis needs an ascended spell, a Conjunction two) and nobody is taught two
at once. The achievement cards say "Frees X and teaches a lesson" for that reason, and the sealed line
names the chapter of the next teacher not yet found ("Learned from a secret of the Fallow Fields"),
never the secret, which is sealed until seen.

On 2026-10-31 he asked for four slots as a difficulty
lever and withdrew it ("let's stick with 6 spells available to select", `.ai/difficulty-curve.md`). This
does not reopen that: the full book stays six. It is about how a new save *gets* to six.

Read with: `.ai/difficulty-curve.md` (the chapter targets this must keep), `.ai/run-pacing.md` (the level
clock), `.ai/achievements-and-secrets.md` (how unlocks are earned), `.ai/combo-spells.md`,
`.ai/sovereign-spell.md`, `.ai/new-items.md`, `.ai/releasing.md` ("Releasing a story reset").

---

## 0. Questions for Eric

| # | question | recommended | the main alternative |
|---|---|---|---|
| 1 | how many spells a new save holds | **3**: the wizard's own and two picks (section 2.2) | **4**, if the fresh Forest misses its target at 3 (section 8, rule 1). 2 runs out of level-ups by minute 8 |
| 2 | where the three missing pages come from | **the Survivor Feat (survive 10 minutes) gives the 4th, in the run that earns it; the shop sells the 5th and 6th** | the 4th from Elderbark instead - but his fall ends the run, so the run that clears the Forest is played on three pages and runs dry from minute 12 |
| 3 | the shop pages | **"Bound Pages", +1 spell slot a level, 2 levels at 200 then 400, its own section at the top of Powers** | cheaper (150 / 300) if the farmed Circle stops passing (section 8, rule 3) |
| 4 | the Apotheosis | **taught by the Ninth Stone (chapter 3)**, one chapter after the Ascension | keep it riding the Ascension (the Hayward) |
| 5 | Conjunctions | **taught by the Hollow Choir (chapter 4)** | a Feat, "ascend two spells in one run"; or no gate (each recipe's own Feat already waits for the build) |
| 6 | boons, chests, relics, sets | **open from run 1, unchanged**; boon slots stay six. The small book needs boons to fill its hands, and the Forest's balance counts on its chests | hold boons back until Elderbark |
| 7 | rerolls and bans | **no new gate** (a new save owns none until it buys them); **hide the Reroll button while the run has none**, as Ban already is | seal the shop's Level-ups section until the Hayward |
| 8 | the Apotheosis on show (`.ai/in-flight.md` 3a item 9 made it visible everywhere) | **one locked thing visible at a time**: the browser and character select show the Apotheosis, sealed, once the Ascension is learned; Conjunctions once the Apotheosis is | show every sealed line from run 1, as the Ascension's is now |
| 9 | relics that name an untaught system (`.ai/new-items.md`: the Ampulla, the Gimmel Ring's full set) | **chests do not deal them until the save has learned what they name** - the rule behind new-items Q6, applied to every card | deal them, with "???" in place of the name |
| 10 | Full Grimoire ("carry six spells", pays Void Lance) | **keep six**: Void Lance comes with the full book | "fill your book", which a new save does in run 1 |
| 11 | existing saves | **a schema step keeps everything a played save uses** (six pages, and the Apotheosis and Conjunctions for a save that has the Ascension). Not an epoch bump on its own | ride an epoch bump so returning players get the slow start too - **recommended only if the castle release resets the story anyway** (section 6) |

---

## 1. Today

| system | gated today | how |
|---|---|---|
| spell slots | **no** | six from run 1 (`Player.MaxSpellSlots`), seven with the Blank Folio for its run. Every held spell takes one: no `SpellData` is `IsPassive` any more (the passives went at schema 9), so wards, the Familiar and Aegis Ward all take a slot. Boons have their own six (`MaxBoonSlots`) |
| a full book's level-up | - | no new spell (spells are permanent). The hand is the held spells' upgrades to level 7 (8 is the Ascension), a boon (`BoonOfferChance` 0.22, and always when nothing else is left), and last **Marginalia**: heal 30% of max HP, +2% spell damage |
| level 4 mutation, element chips | no | every run |
| boons | starters only | five starters (Iron Rivets, Tidewater Flask, Lantern Oil, Mossgrown Charm, Taproot); the rest are deeds or three in the shop |
| chests, relics, Full Set Enchantments | no | the Forest's elites come more often *so that* their relics carry a starters-only build out of the crowd (`difficulty-curve.md` 4.2, round 3) |
| rerolls, bans | by purchase | Fate Fracture (40 + 25) and Proscription (90 + 70), pools for the run; a new save has none. Ban is hidden at zero; Reroll shows "Reroll (0)", disabled. The 5:00 onboarding tip says to use rerolls |
| Ascension | **yes** | taught by the Hayward, chapter 2 (`GlobalStatsManager.HasLearnedAscension`) |
| Apotheosis | with the Ascension | it needs its spell's Ascension path, so it is sealed with it |
| Conjunctions | with the Ascension | both parents must be ascended down named paths; each recipe's derived Feat completes the first time it is met |
| Sovereign Rite | **yes** | the Warden of the Deep, chapter 10 (being built) |

---

## 2. The spell book

### 2.1 The ladder

| pages | from | when, for a typical new player (section 4) |
|---|---|---|
| **3** | a new save | run 1 |
| **4** | **Survivor** (survive 10 minutes in one run), the Feat that already pays Hollow Star | at 10:00 of the first good run, usually run 1 or 2. Feats complete mid-run (`Node2DGame.RecordAchievementNow`), so the page opens **at the next level-up of that run**, and Hollow Star joins the pool at the same moment |
| **5** | **Bound Pages** I, 200 Arcane Energy | after run 2 |
| **6** | **Bound Pages** II, 400 | after run 4, the first Fields clear - before the first wall |
| (+1) | the Blank Folio | unchanged: a relic, one run, one more page on whatever the book holds |

The three sources **add, in any order**. A player who dies before minute 10 twice has 260 Arcane and can
buy the 4th page in the shop before he has earned the Feat; the Feat then gives the 5th.

### 2.2 Why three, and why the fourth at minute ten

A full book is offered upgrades, then boons, then Marginalia. So the book's size decides when a run
**writes out its book** - every level-up after that point is one Marginalia card. Counted against the
Forest's level clock (`run-pacing.md` 10.4: level 26 at about 11.5-12 minutes, 29 at 13.5-14; a fresh
six-page Forest reaches 34 by 15:00, `difficulty-curve.md` 10), with the Ascension not yet learned
(spells stop at 7) and the five starter boons:

| book | new spells | upgrades to 7 | boons | real picks | written out at | Marginalia by the boss |
|---|---|---|---|---|---|---|
| 2 | 1 | 12 | 5 | 18 | level 19, ~minute 8 | 15 or more |
| **3 all run** | 2 | 18 | 5 | 25 | level 26, ~minute 12 | **6-10** |
| **3, the 4th at 10:00** | 3 | 24 | 5 | 32 | level 33, ~minute 16 | **0-3** |
| 4 | 3 | 24 | 5 | 32 | level 33 | 0-3 |
| 6 | 5 | 36 | 5 | 46 | never | 0 |

A written-out book is the empty level-up screen the brief worries about, and it is also a fountain: six
Marginalia heal 90 HP on a 50-HP bar the difficulty curve meant to stay hurt. **The 4th page at minute
10 lands at about level 22, four levels before a three-page book runs dry**, which is why it is
Survivor's and not Elderbark's. Three pages for the first ten minutes is the whole of the slow start's
window: it is when a new player is learning the screen.

**Is a full three-page hand enough?** It is "which of my three spells do I deepen", one card per spell,
plus the level 4 mutation's choice and the odd boon - the decision the 0:30 tip already teaches
("Prioritize one or two core spells"). Thin, but it lasts from about level 4 to the page at 10:00.

### 2.3 Rules

- **Every held spell takes a page**; boons keep their own six, unchanged.
- A full small book follows the full book's rule exactly: no new spell, no swap.
- **`Player.MaxSpellSlots` stays 6 and means the full book**: the element reachability floor
  (`RegressionChecks`, still six), Full Grimoire's target, and the HUD's widest strip. A new constant
  holds the new save's three.
- The book is the save's in every mode, the Boss Rush included. `--all-unlocked` and the other
  own-everything tools get the full book; Playtest mode leaves it as the save has it.
- The Scribe's Penknife is dealt only to a full book, so a three-page book can be offered it from about
  minute 2. Watched in measurement (section 8), not changed.

---

## 3. What else unfolds, and when

One new system per boss, on the first four chapters, and never two on one results screen.

| system | today | proposed | lands |
|---|---|---|---|
| level-up cards, the level 4 mutation, element chips, Skip | run 1 | **unchanged** | run 1 |
| boons | run 1 (starters) | **unchanged** | run 1 |
| chests, relics, sets | run 1 | **unchanged**, but a chest never deals a relic whose card needs a system the save has not learned (Q9) | run 1 |
| rerolls, bans | bought | **unchanged**; Reroll hidden while the run has none; the 5:00 tip only for a save that owns rerolls | when bought |
| **Ascension** | the Hayward | **unchanged** | chapter 2 |
| **Apotheosis** | with the Ascension | **taught by the Ninth Stone** | chapter 3, the first wall's prize |
| **Conjunctions** | with the Ascension | **taught by the Hollow Choir** | chapter 4 |
| Sovereign Rite | the Warden of the Deep | **unchanged** | chapter 10 |

**Why these two bosses.** The Broken Circle is "the circle where the wizards met" - the place to learn
your own wizard's final form, and the first wall, so the reward for climbing it is the biggest thing a
wizard has. The Hollow Choir is three voices singing as one, the fiction of two spells made one. Moving
the Apotheosis a chapter later costs a played save nothing (section 6) and costs a new save the runs of
chapter 3 before the Stone falls - runs a fresh save loses anyway, and in which a farmed one meets an
Apotheosis only if it planned for one (an Ascension on his own spell is a plan, not a drop).

**Why not leave Conjunctions to their recipes' own Feats.** With one recipe built, they are late on
their own. With section 4 of `.ai/combo-spells.md` built, Burning Briar's parents are Thorned Vine
(Elderbark) and Cinderbreath (a starter), so a recipe would be "known" - and its browser section and the
linked-ring marks on level 8 cards shown - the moment the Hayward teaches the Ascension: two systems
at once.

**How each is held**, copying the Ascension's rule (`HasLearnedAscension`): a save flag, **or** the
chapter the teacher's boss opens is earned, **or** that boss beaten. Derived, so a save already past the
teacher is taught with no step of its own, and the Dungeon's move (C2) cannot break it - the "next
chapter" is read from `StageCatalog.ChapterOpenedBy`. Enforced where the Ascension is: the Apotheosis
offer in `Player.GetAscensionOffers` and `BuildAscensionPreview`, the Conjunction offer in
`Player.BuildComboOffers`, and the browser sections in `LevelUpMenu.Ascensions.cs`.

**One locked thing on show at a time** (Q8). The browser lists Ascension paths sealed "Learned by
defeating the Hayward", as now. The Apotheosis section, and the "Apotheosis:" line on character select,
appear once the Ascension is learned, sealed "Learned by defeating the Ninth Stone". The Conjunctions
section appears once the Apotheosis is learned. The Evolution Codex (`wip/evolution-codex`) follows the
same rule. This partly walks back `.ai/in-flight.md` 3a item 9 for a save's first two chapters, which
is why it is a question.

---

## 4. The pace

**Income** (`Node2DGame.CalculateArcaneReward`; the numbers `difficulty-curve.md` 1.2 builds its farmed
save on): a chapter clear pays about 200 plus its boss's bonus (Elderbark 150, the Hayward 160, the
Ninth Stone 175, the Hollow Choir 190); a failure at minute 8 about 130; a Circle overrun at minute 8,
about 100. Achievements and secrets add more, unevenly; they are left out, so the timeline below is
the slow end.

An illustrative new player, who clears each early chapter at the second try:

| run | plays | pages | what it pays | Arcane after | what is new |
|---|---|---|---|---|---|
| 1 | Forest, dies at ~8:00 | 3 | 130 | 130 | the base game: cards, the level 4 mutation, element chips, boons, chests |
| 2 | Forest, clears | 3, **4 at 10:00** | Survivor: Hollow Star and a page; Elderbark: Thorned Vine, the Fields; 350 | 480 | a page mid-run; the first boss |
| shop | buys **Bound Pages I** | **5** | | 280 | |
| 3 | Fields, dies | 5 | 130 | 410 | |
| 4 | Fields, clears | 5 | the Hayward: Thornseed, **the Ascension**; 370 | 780 | |
| shop | buys **Bound Pages II** | **6** | | 380 | the full book |
| 5 | Broken Circle, overrun at ~8:00 | 6 | 100 | 480 | the first wall; the first Ascensions |
| 6-8 | farms the Forest and Fields, tries the Circle | 6 | ~350 a clear | Powers | |
| ~9 | Circle, clears | 6 | the Ninth Stone: Shadegrease, **the Apotheosis** | | |
| ~10-12 | Sunken Cave | 6 | the Hollow Choir: Riptide, **Conjunctions** | | |
| late | the Emberdeep | 6 | the Warden of the Deep: **the Sovereign Rite** | | |

- **The pages are bought where the shop is first visited.** Bound Pages is the top card, a new player
  can afford the first page after his second run, and a fifth spell is likely worth more than the
  Powers it displaces (cell E in section 8 checks) - so the shop teaches itself with its first purchase.
- **A player who never opens the shop stays at four pages.** No story page rescues him, on purpose: the
  walls from chapter 3 are tuned for a farmed save (`difficulty-curve.md` 3), and farming *is* the shop.
- **The cost lands at the first wall.** Today a player reaches the Circle with six pages for nothing;
  under this he has paid 600 for them, which is 600 less in Powers - about two farming runs. Section 8
  checks the farmed Circle still passes, and says what gives first if it does not.

---

## 5. What a small book does to the gates

The small book and the tag gates **never meet on a typical path**: by the time the Ascension is taught
the book holds 4-6, and by the Apotheosis 6. Element tiers count held spells, boons, relics and sets
(`Player.ComputeElementInstanceCounts`).

- **Ascension** (4 of the branch's element, from chapter 3): the wizard's own pure spell at level 7 down
  its path carries 2; two more from three other spells and up to six boons. Reachable on four pages.
- **Apotheosis** (since 2026-11-02: the own spell at max level, ascended and not combined, and level 21;
  it was 6 of the wizard's element and level 23). It asks no element count of its own any more - only
  what the spell's Ascension asks - so it no longer needs six pages, and it is not the reason to buy
  them that this section was written around: on four pages it is as reachable as the Ascension.
- **The Feats that pay spells** slide by about a run, never close. Elementalist (4 of anything) is
  reachable on three pages through the Earth starter boons (Iron Rivets, Mossgrown Charm, Taproot). The
  Fire Adept for a Pyromancer is not, short of a Fire relic: Cinderbreath and its level 4 path (2) and
  Lantern Oil (1) make 3, and no other spell a new save owns carries Fire; the fourth wants a page and
  Meteor Swarm (Veteran). Full Grimoire waits for six (Q10).
- **Conjunctions** need two ascended spells that are not the wizard's own: three pages at least, and the
  fusion frees one. **The Sovereign Rite** needs three actives whose other levels sum to 16: six pages
  by then.
- `ValidateElementReachability` is unchanged: it asks what the full book can reach.

---

## 6. Existing saves

**A schema step, not an epoch bump.** The slow start adds to what progress stores (a page reward, a shop
row, two "learned" flags and a kept-book flag) and changes when systems open, not the story: no
chapter, wizard or chapter gate moves. The story-reset ADR
(`.ai/decisions/2026-11-02-story-resets.md`) is explicit that the epoch is never raised to avoid writing
a migration. The step, `MigrateToSlowStart`:

1. **`FullBookKept`** for every save with a run on record (`Lifetime.TotalRuns` or the telemetry ring -
   schema 20's population). Every such save played every run on six pages; it keeps six. The shop's
   Bound Pages row reads as complete for it, at no charge and with nothing refunded.
2. **`ApotheosisLearned` and `ConjunctionsLearned`** for every save that has learned the Ascension (the
   flag or the Hayward's derivation). Until now both rode it.

Nothing paid, nothing taken. Held by a `ValidateSlowStartMigration` on sample saves: a played save keeps
six and both systems; an unplayed save and a new save start on three, unlearned; a second migration
changes nothing.

**The number.** The save is at 20, and **21 is held by the castle's Dungeon move** (C2,
`wip/dungeon-move`, `.ai/in-flight.md`). Steps run in order, so a save migrated past a number skips a
step that lands under it later. This takes the next number free *when it merges*: 22 after C2, or 21
if it merges first and C2 renumbers.

**What this means for returning players:** they keep the full book and never see the slow start. If
Eric wants them to, the honest way is a story reset - and the castle's release, which moves the Dungeon
into the castle (a chapter reorder, the ADR's own example), may be one. **Recommended: if that release
resets the story, ship the slow start in it**, so the restarted story opens paced. The migration is
written either way, for any save that is not reset. The first reset still comes one release after the
reset code ships (`.ai/releasing.md`).

**Developer's save:** any Godot run on this branch migrates the real save (CLAUDE.md), so pass
`-- --no-save-writes`.

---

## 7. Telling the player

Words kept to what each moment needs. Everything in a run goes through `Announcements` (one at a
time; a boss banner never waits).

| moment | where | what it says |
|---|---|---|
| Survivor at 10:00 | the achievement plate, in the run (`Announcements.Kind.Achievement`) | **Survivor** - "Unlocks Hollow Star and a spell page" |
| the page opens | the HUD's spell strip | a new empty frame beside the held ones, at the next level-up. `LoadoutHud` already draws only open slots, so nothing else changes |
| a page or system earned in the run | the results screen, one card each (the Ascension's pattern: learned at the end, not at the start) | **New spell page** - "Your book holds 4 spells"; **Apotheosis revealed** - "Your wizard's own spell may take its final form"; **Conjunctions revealed** - "Two ascended spells may become one" |
| the teachers' deeds | the Achievements list and the deed plate | the Ninth Stone: "Unlocks Shadegrease and teaches the Apotheosis"; the Hollow Choir: "Unlocks Riptide and teaches Conjunctions" |
| what a chapter holds | the world map card (`StageRewards`) | the page and each teaching listed with the chapter's other rewards (two new reward kinds) |
| the shop | Powers, a "Spell book" section at the top | **Bound Pages** - "+1 spell slot in every run"; now / next: "4 spells" / "5 spells" |

**Locked pages are never drawn in a run.** The strip shows the pages the book has, held or empty, and
nothing for the ones it lacks; the shop card's pips are where the missing pages show. The Blank Folio's
card loses "hold 7 spells instead of 6" for "+1 spell slot for this run".

---

## 8. What to measure

The protocol of `difficulty-curve.md` 1.1, so its tables compare:

```sh
"$GODOT_BIN" --headless --fixed-fps 30 --path . scenes/_BalanceHarness.tscn -- --stage=0 --minutes=19 --character=N --seed=1234 --fresh-save --survivor --immortal --policy=ordinary --stop-overrun=60 --timescale=1
```

**What the harness needs:** `--slots=N`, overriding the run's book for an A/B on one tree; `--fresh-save`
reading the new save's three, and marking Survivor done at any chapter past the Forest (whoever stands
there has survived ten minutes); `--farmed` buying Bound Pages before any Power, with the row added to
the harness's mirrored shop caps; the page opening mid-run when Survivor completes. It already prints
`slots=`, `dead level-ups` and `marginalia taken`; add the minute the book was first full and the
Penknife taken.

| cell | save, chapter | book | wizards, seeds | passes when |
|---|---|---|---|---|
| A | fresh, Forest | 3, the 4th at 10:00 | the four starters at 1234; Stormcaller and Frostweaver at 99 too | chapter 1's target on every wizard: no overrun, siege 3 minutes or less, boss down inside 1.5. **And Marginalia 3 or fewer** |
| A0 | fresh, Forest | `--slots=6` | the same | the before, on the same tree |
| B | fresh, Forest | `--slots=4` | only if A fails | the fallback for Q1 |
| C | fresh, Fields | 4 (Survivor done, nothing bought) | Stormcaller, Frostweaver | chapter 2's target: no overrun, siege 4-8, boss inside 3 |
| D | farmed, Broken Circle | 6, pages first, 1,000 on Powers | Stormcaller, Frostweaver | today's farmed pass: no overrun, siege 5 or less, boss inside 3 |
| E | farmed, Broken Circle | `--slots=4`, all 1,600 on Powers | Stormcaller | a comparison: whether pages are the right first buy |

**Decision rules, in order:**

1. **A misses on any wizard, or deals more than three Marginalia:** run B. If B passes, a new save starts
   on four. Do not reopen the Forest's `ChapterDifficulty` row first: it took four rounds to set.
2. **C overruns:** Bound Pages I to 150, so the typical player holds five at the Fields. (A siege under
   four is fine - the six-page Fields measured 1-3, under its own target.)
3. **D overruns where today's farmed save passes:** pages to 150 / 300 before the Circle's row moves.
4. **E beats D:** the pages cost more than they are worth; cheaper, or the 6th from a boss.

**What only a person can answer** (Eric, a new save, runs 1-5): whether three spells read as calm
rather than weak; whether the page at 10:00 lands as a moment or goes unnoticed; whether the Forest is
still cleared in a try or two; whether Bound Pages is the first thing bought; whether the Apotheosis
sealed until chapter 3 reads as something to earn.

---

## 9. Building it

For the agent who does. Each line is where the change lives, not how.

- **`scripts/SpellBook.cs`** (new, static): the one authority on a save's pages - 3, plus every done
  deed's `PageReward`, plus the Bound Pages level, or six when `FullBookKept`; capped at six.
  `Player.SpellSlots` reads it at run start and again when a deed completes mid-run, plus the Folio.
  `Player.NewSaveSpellSlots = 3`; `MaxSpellSlots` stays 6.
- **`AchievementDefinition.PageReward`** (int), set to 1 on Survivor, with its `RewardText`. Derived from
  done deeds, so `Reconcile` needs nothing and the page can move to another deed later without a step.
- **`MainMenu.BuildUpgradeDefinitions`**: `RegisterUpgrade("pages", "Bound Pages", "+1 spell slot in every
  run", 200, 200, 2)`, and a first `UpgradeSections` entry; a kept book (`FullBookKept`) shows it complete
  at no charge. Mirror the caps in `_BalanceHarness`.
- **`GlobalStatsManager.HasLearnedApotheosis` / `HasLearnedConjunctions`**, teacher stages `stage_10` and
  `stage_2`, with `SaveData.ApotheosisLearned`, `ConjunctionsLearned` and `FullBookKept`. The two deeds'
  `alsoReward`. Enforced at the four places in section 3; character select's Apotheosis line; the Codex.
- **Results and map:** `RunUnlockKind` gains a page and Conjunctions (with `GameOverScreen.KindLine` and an
  icon); `StageRewardKind` gains a page and a teaching.
- **`LevelUpMenu`**: Reroll hidden at zero. **`Node2DGame.OnboardingTips`**: the 5:00 line only with
  rerolls owned. **`ChestItemCatalog`**: the Blank Folio's card text; a relic's required system, read by
  the chest offer.
- **The schema step** (section 6) and **validators**: `ValidateSpellBook` (3 + deed pages + Bound Pages'
  levels = `MaxSpellSlots`; every page deed is a Feat or a Road deed before the first gate, the
  `ValidateGateReachability` rule); `ValidateTeachers` (the Ascension's, the Apotheosis's and the
  Conjunctions' teachers playable and in that campaign order); `ValidateSlowStartMigration`.
- `dotnet build` sees none of the wiring. The validator run, the harness cells and `_UiShot` of the
  results card, the shop's top section and the three-page strip (both canvases) are the checks.

---

## 10. What was built (2026-11-02, `wip/slow-start`)

### 10.1 Where it lives

| piece | where |
|---|---|
| **the book** | `scripts/SpellBook.cs`, the one authority: 3 (`NewSavePages`), plus every done deed's `AchievementDefinition.PageReward` (Survivor's 1), plus the Bound Pages levels, capped at `Player.MaxSpellSlots` (still 6, still the full book); 6 for a kept book or no save. `Player.SpellSlots` = `BookPages` + the Folio; `BookPages` is read in `ApplyArcaneUpgrades` and grown mid-run by `Player.RefreshSpellBook`, which `Node2DGame.RecordAchievementNow` calls when a page deed completes. |
| **the shop** | `MainMenu.BuildUpgradeDefinitions` registers `pages`, "Bound Pages", 200 + 200, 2 levels, in a first "Spell book" section of Powers (`MainMenu.Shop.cs`). Its stat line is the save's book ("Spells held 3 -> 4", Survivor's page included). A kept book reads complete at no charge (`SpellBook.ShopLevelShown`). Icon: `pages()` in `tools/art/upgrade_icons.py`. |
| **what is taught** | `scripts/Teachings.cs`, the one authority on "has learned X" (`HasLearned`), what may be shown (`IsShown`, Q8), what is owed (`Owed`) and where a sealed system says it is learned (`SealedLine`). `GlobalStatsManager.HasLearnedAscension` and its Hayward rule are gone. `Player` reads `AscensionLearned`, `ApotheosisLearned`, `ConjunctionsLearned` once, at the run's start. |
| **where it is enforced** | the Ascension offer and preview (as before, `Player.GetAscensionOffers` / `BuildAscensionPreview`); the Apotheosis offer (`TryBuildApotheosisOffer`) and preview (`BuildApotheosisPreview`: null until the Ascension is learned, sealed until its own); the Conjunction offers (`TryBuildComboOffer`) and previews (`BuildComboPreviews`: empty until the Apotheosis is learned, so no linked rings on a level 8 page either); the Codex (`CodexModel`: the finals, the recipes, the Rite and the "Joins" line, each by `Teachings.IsShown`; a sealed system's chip names where it is learned); character select's Apotheosis line. |
| **the lessons** | `scripts/Prologue.cs` plays reels now: the prologue and one two-panel reel per system (`Prologue.Reels`), from `assets/bonelight/ui/lessons/`, drawn by `tools/art/lessons.py` on the prologue's frame and pieces. `Node2DGame.Lessons.cs` gathers what the run's end owes (`GatherOwedLessons`, also a results card, `RunUnlockKind.Lesson` "Learned") and plays each over the paused run before the results (`ShowGameOver` asks first; the defeat's own sequence plays before it), embedded on canvas layer 60 (`Prologue.CreateEmbedded`). The save learns it when the reel ends, played through or skipped. Headless runs teach without the reel. Options > Story lists "Lesson: <name>" for every lesson the save has been given, beside Watch the Prologue. |
| **telling the player** | Survivor's achievement plate gains "Spell page - your book holds 4"; a teacher's secret toast gains "Lesson - The Ascension, when this run ends". Both are the plates' own lines, so they wait in the `Announcements` queue with them and are never on screen beside another message. The results list "New spell page" and "Learned". The HUD's strip shows the pages the full book has and this one lacks as ghost frames with a small lock (`LoadoutHud`, `SlotMark.Locked`). |
| **Q7** | Reroll is hidden while the run has none, on the level-up (`LevelUpMenu.UpdateRerollState`) as on the chest screen (which already hid it). The 5:00 reroll tip is skipped for a run with none to spend. |
| **Q9** | `ChestItemCatalog.TeachingNamedBy` reads a relic's name, line and sets for a system's name; `Player.GetChestUnavailableItems` keeps any that names an untaught one out of the chest. No relic names one today, so it holds nothing back yet. |
| **the Blank Folio** | its card reads "+1 spell slot for this run." |
| **schema 22** | `SaveData.MigrateToSlowStart`: `FullBookKept` for every save with a run on record; the three ladder lessons learned (no reels) for every save that had the Ascension by the old rule (the flag, the Hayward beaten, or a chapter past the Fields opened, read from frozen lists); the Rite's lesson for a save that owns it. Written as the step after 21, which is `wip/dungeon-move`'s and not on this branch: 21 must merge before a build carrying 22 ships, or a save migrated here skips it. |
| **validators** | `RegressionChecks.SlowStart.cs`: `ValidateSpellBook` (3 + deed pages + Bound Pages = 6; a page deed is a Feat or a Road deed before the first gate; the Folio's card), `ValidateTeachers` (each rung's teacher a built vault in a playable chapter, in campaign order; every reel's panels on disk with a one-line caption; the Rite still the Warden's; the ladder's out-of-order rule on paper), `ValidateSlowStartMigration` (sample saves at 21). `ValidateDifficultyMigration` lost its two Hayward checks; `ValidateCodex` looks for the recipes on a save that may be shown them, and checks a fresh one is shown none. |
| **tools** | `_BalanceHarness`: `--slots=N` (`SpellBook.ToolOverride`), `--fresh-save` past the Forest marks Survivor done, `--farmed` buys Bound Pages first, `--all-unlocked` owns the full book and every lesson, and the report's "spell book" line (the book it started with, when a page opened, when it was first full). `_UiShot`: `--learned=a,b|all`, `--reel=<id>` (with `--scene=res://scenes/Prologue.tscn`), `--find-secret=<id>`; the own-everything save owns every lesson and the full book. |

### 10.2 Where it departs from this doc

- **The teachers are landmarks, not bosses** (Eric's answer), and a ladder rather than fixed pairs (the
  table at the top). The Hayward no longer teaches anything; his deed reads "Unlocks Thornseed".
- **A system is learned when its reel plays, not when its teacher is found.** The run that found it is
  played as it began, so nothing is usable before it has been explained. Since 2026-11-06,
  **Quit Run** also plays owed lessons before its results. Closing the app leaves the lesson owed
  to the next finished run.
- **Locked pages are shown on the strip** (the brief's "locked slots show quietly"), where section 7 said
  never to draw them.
- **Environmental discovery (2026-11-06; replaces the 2026-11-05 golden guidance).** Unfound map
  silhouettes still open gentle landmark clues with mouse, touch or controller, but no longer
  advertise a major lesson or tell the player to follow lights. The opening note uses an environmental
  clue. All three teachers have lost the spawn-to-upgrade lights and trail-triggered gold POI arrow.
  The Star and Geode retain their existing local decor gradients. `TeachingTrail` now serves only
  Familiar: intermittent drag furrows, splayed paws and claw scratches, faintly pulsing in grounded
  gold stops. The later polish uses six procedurally varied families (partial/staggered paws, claw
  fans, short/long drag furrows and sideways scuffs), stronger gold and separated toes, with irregular
  300-520-unit gaps rather than the old 144-unit repetition. A bounded, one-off walkable-cell search grows OUT from the pen toward the outskirts,
  keeping the first evidence at least 1,488 world units from spawn. It never registers a POI. Visible
  clusters redraw at 10 Hz, with no traveling motes, bloom, alpha fade or per-frame terrain search.
  Tiny decor reserves keep newly streamed props off the actual marks.
- **The Pinfold is walkable and holds essence (2026-11-06, enlarged again at Eric's request).** Its
  generated enclosure is now 1944x1632, over four times both dimensions of the preceding 448x352
  version (originally 200x168). Three separate textures stay under 2048px each; pixel size stays x2.
  Back/floor, energy and front wall have separate sort origins so entering never
  hides the wizard beneath its floor. Only the walls stamp navigation; the 256-unit entrance has
  a separate physical gate. The existing two guards still bind it; defeat them and touch its latch
  to break the animated seal. Opening the gate grants nothing. Approach the animated center essence
  within 52 units to claim Familiar immediately, saved through the existing secret/deed path; its
  light curls into the wizard. Repeat runs pay the usual chest. The enclosure center is the rolled
  five-to-seven-chunk site; its front sits 768 units south. A reserved foundation is cleared in the
  chunk data before painting, and again on regeneration: no pond, hidden fence or procedural flower
  pen under the larger floor. Twenty-four blue/violet wisps circle its walls and eight circle the
  carved inner ward; the closed binding dissolves when the gate opens. Wisps cull against the view
  and draw on the 2-unit grid. Save schema and post-run system teaching rules stay as before;
  moving system unlocks to character interactions is item 37.
- **The Codex's Recipes tab is still there on a fresh save**: it holds the Full Set Enchantments too,
  which are open from run 1 (Q6). Only its combinations wait for the Apotheosis.
- **The Penknife is never taken by the harness bot** (it is kept out of every chest the bot deals), so the
  section 8 count of it is not measured.

### 10.3 Checked, and not

- **Animation/scale polish:** `_DiscoveryPreview.tscn -- --mode=marks|tracks|pen --no-save-writes`
  captures 40 actual rendered frames with hidden HUD and a steady camera. `tools/art/discovery_gif.py`
  encodes and verifies transparent delta GIFs, including pixel-exact recomposition. `marks` arranges
  all six variants for comparison; `tracks` uses actual world placement/gaps; `pen` zooms out to
  show the enlarged enclosure and wisps. Build/validator and seed 1234 desktop / seed 17 portrait
  walks passed, including foundation clearance, gate physics and player movement via real Feet
  constraints (`Player.MovePlayer`), opening versus claiming, and Familiar's next-level-up availability.

- **2026-11-06 discovery revision:** build and full validator passed (0 errors, existing Test Wizard
  warning). Rendered `_UiShot --secret-probe --fresh-save --stage=9` passed with default terrain,
  seed 1234, seed 17 in portrait/safe-area, and seed 2026 on a repeat visit. Checks include no
  spawn-visible marks/arrow, walkable track cells, physical gate collision/removal, no reward for
  gate opening, actual body motion into the center, immediate Familiar ownership/pool access,
  and one repeat chest. Stage 2/10 vault walkthroughs passed without golden guidance. Track routing
  measured 55-116 ms once per run here (bounded at 12,000 searched cells); no per-frame route work.
  `--goto=tracks --goto-offset=0,0` photographs the ground evidence. These screenshot probes need
  a rendered window because they advance on `FramePostDraw`; use headless mode for the content
  validator instead. No physical Android device was connected. Older checks below are historical.

- `dotnet build` clean; the validator: 0 errors and 4 warnings, the same four as before.
- `_UiShot`, both canvases: the three-page strip (one held, two empty, three locked), the shop's Spell
  book section, a panel of each reel, Survivor's plate with the page line and the strip grown to four, the
  Pinfold's toast with the lesson line, the Codex fresh (the Ascension sealed "Learned from a secret of
  the Fallow Fields", no Apotheosis) and with the Ascension learned (the Apotheosis sealed), character
  select without and with the Apotheosis line, the level-up with no Reroll and the Ascension strip only
  once learned, and a forced victory after the Pinfold: the reel over the run, then the results.
- **Needs eyes:** the eight panels at full size on a real screen (they are generated, and judged here only
  in screenshots); whether a ghost lock on the strip reads as "more to come" or as clutter; whether the
  Survivor page lands as a moment on a phone. No pad has skipped a lesson; the hold is the prologue's.

### 10.4 The measurement (section 8's protocol)

`--fixed-fps 30 --stage=0 --minutes=19 --fresh-save --survivor --immortal --policy=ordinary --stop-overrun=60`,
two Godots at a time. "Boss" is minutes from the boss's arrival at 15:00 to his fall; "siege" the minutes
at the cap at least half the time; "Marg" the Marginalia taken (the heal-only card); "full" the first
level-up that found the book full. **No run overran and no run had a dead level-up.**

| cell | wizard, seed | boss | siege | Marg | full | page opened |
|---|---|---|---|---|---|---|
| A (3, the 4th at 10:00) | Pyromancer 1234 | 0.87 | 0 | 0 | 1.0 min (L4) | 10.4 |
| A | Frostweaver 1234 | 1.26 | 0 | 0 | 1.1 (L4) | 10.7 |
| A | Stormcaller 1234 | 0.54 | 0 | 3 | 1.2 (L4) | 10.1 |
| A | Geomancer 1234 | 0.94 | 0 | 0 | 1.2 (L4) | 10.2 |
| A | Frostweaver 99 | 0.75 | 2 | 0 | 1.3 (L4) | 10.4 |
| A | Stormcaller 99 | **2.27** | 0 | 0 | 1.2 (L4) | 10.2 |
| A0 (`--slots=6`) | Pyromancer 1234 | 0.53 | 0 | 0 | 3.1 (L8) | - |
| A0 | Frostweaver 1234 | 0.86 | 2 | 0 | 3.1 (L8) | - |
| A0 | Stormcaller 1234 | 0.50 | 0 | 0 | 3.7 (L8) | - |
| A0 | Geomancer 1234 | **2.00** | 0 | 0 | 3.0 (L8) | - |
| A0 | Frostweaver 99 | **2.87** | **10** | 0 | 2.8 (L7) | - |
| A0 | Stormcaller 99 | 1.09 | 1 | 0 | 2.8 (L7) | - |
| B (`--slots=4`) | Stormcaller 99 | 0.47 | 0 | 0 | 1.7 (L5) | - |
| B | Stormcaller 1234 | **1.62** | 0 | 1 | 1.7 (L5) | - |
| C (Fields, 4: Survivor done) | Frostweaver 1234 | 2.32 | 0 | 6 | 1.7 (L5) | - |
| C | Stormcaller 1234 | 2.68 | 0 | 1 | 1.7 (L5) | - |

**Against the rules:**

1. **A meets chapter 1's target on five of six; Marginalia are 3 or fewer on all six.** The one miss
   (Stormcaller at 99, the boss down in 2.27) is the boss fight's spread, not the book's: the six-page
   book misses on two of the same six (the Geomancer at 2.00; the Frostweaver at 99 with a ten-minute
   siege, the worst run measured), and B, four pages, passes the Stormcaller at 99 (0.47) and misses her
   at 1234 (1.62), where three pages took 0.54. Rule 1 asks for B on a miss and for four pages if B
   passes; B does not pass any better than A, so **a new save stays on three**. Eric's call if the spread
   says otherwise to a person: section 8's "only a person can answer".
2. **C does not overrun** (boss inside 3 on both; siege 0, under the 4-8 target, which section 8 calls
   fine). Bound Pages stays 200 / 400. The Frostweaver's six Marginalia are the Fields without the
   Ascension: a fresh save that walked past the Pinfold has every spell stopped at 7 by minute 12.
3. and 4. (the farmed Circle, D and E) were not run.

**What three pages does to the hand:** the book is full at the fourth level-up, about a minute in,
against about three minutes on six. From there to the page at 10:00 every hand is upgrades of the three
held spells and boons, which lasted on five of the six runs (Marginalia 0; the harness does not say
when the Stormcaller's three came). The page opened at 10.1-10.7, at the next level-up after 10:00.

**The Archmage, the fifth starter (2026-11-02, `.ai/protagonist.md` section 7), run through cell A:** the boss
down in 0.61 and 0.52 (seeds 1234 and 99), no overrun, the page at 10.2-10.4 - but **Marginalia 10 and 5**,
which is rule 1's trigger. He writes his three pages out sooner than the four because he kills faster: Magic
Missile went pure Arcane with him and is attuned, and as Arcane + Lightning the same run took none. Left for Eric
with the levers listed there; the four's rows above are unchanged by him.
