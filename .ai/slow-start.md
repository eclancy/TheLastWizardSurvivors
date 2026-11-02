# The slow start: a thin spell book that fills, and systems that arrive one at a time

Eric, 2026-11-01:

> "I also want to introduce a slow start to the game so new players don't get overwhelmed. When
> starting the story, let's limit the number of spells you can have equipped, and have unlockables to
> increase that, like additional spell pages in the shop. In addition, we can unlock combination spells
> and ascension via achievements or story progression. Let's plan out a good pace."

**Status: answered by Eric (2026-11-02), to build.** His answers, which win over the table below
where they differ:

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
