# Conjunction progression: working toward a recipe through the campaign

Eric, 2026-11-08: *"start brainstorming ways to help combination spells happen in a way that the player
can naturally work towards in the campaign through unlocks, etc."*

**Status: a brainstorm for Eric to choose from (2026-11-08). Nothing here is built and no code changed.**
Section 3 is the recommended shortlist and section 5 the questions.

**The rule no idea here loosens** (Eric, `.ai/combo-spells.md` section 0): both parents at level 8, each
down the one Ascension its recipe names. What the ideas may touch is everything around that rule: what
the player knows, what the deal hands him, and the Ascension's *own* gates on a named path (player
level 17 and four of the path's element). On the far shore Eric has chosen to waive the element gate for
tonight's recipe; this doc is about the road and the castle, where that waiver does not apply, so each
idea here is **earned** - by a lesson, a boss, a freed wizard or the shop.

Read with: `.ai/combo-spells.md` (sections 2 and 6), `.ai/second-half.md` (section 5 and "F4, built"),
`.ai/slow-start.md` (the ladder of lessons, one locked thing on show at a time), `.ai/gameplay-design.md`
("How many Ascensions a run gets"), `.ai/passives-and-items.md` (3a, 3d), `.ai/new-items.md` (section 4,
the Gimmel Ring), `.ai/stage-secrets.md` (section 3, what a find may pay), `.ai/run-pacing.md` (10.4).

---

## 1. The problem

On the far shore the favour puts both parents in the book inside 1.2 minutes and keeps every upgrade in
the hand, and still the planned bot made its recipe by about minute 10 in only 3 of 10 runs - all Shadow
Bolt - and never made Sagittarius (`.ai/second-half.md`, F4's table). Every miss was held by the
Ascension's own gates: player level 17, and four of each named path's element. **The element gate is the
structural one.** A recipe's two parents almost never share an element, so it asks for two commitments of
four at once, and at level 7 the parents bring about two each. The other four tags must come from three
spare pages, two to four boons (one tag each, and no boon carries Poison, Arcane, Fire or Lightning) and
whatever the chests deal. **On the road there is no favour at all**, so the cards are a brake too
(`combo-spells.md` 6: the deal, not a gate, held Sagittarius back in its first runs; on the far shore, with
the favour off, no run made its recipe in twenty minutes). Sixteen picks on two spells that are not the wizard's own
compete with his own Apotheosis. **And nothing in the campaign teaches the plan:**

- The Conjunctions lesson (the Geode Heart, chapter 4) says only *"Two ascended spells may become one"*.
  It names no recipe.
- A recipe is invisible until the save owns **both** parents (`ComboCatalog.IsKnown`), and so is its Feat.
- The two linked rings appear on the **level 8** page. By then the element plan is made or missed, and the
  level 4 path that would make a fourth element (and seal the recipe) carries no mark.
- On the common path the first recipe a save *can* know at chapter 4 is **Sagittarius**, the one no bot
  has made. Shadow Bolt follows in chapter 5, The Taking in chapter 6.

| recipe | parents come from | named paths' element gates | what held it (F4, favoured) |
|---|---|---|---|
| Sagittarius | Hunter's Draw: the Pale Hart (Forest secret). Starfall: the Fallen Star (Circle secret, ch 3) | Hunter's Mark: 4 Metal or Wind. Constellation: 4 Light | never made. Wind 2/4, Light 2/4, level 14-15 of 17 at minute 20 |
| Shadow Bolt | Void Lance: Full Grimoire (Feat). Contagion: the Lanterns Out (Swamp, ch 5) | Cosmic Collapse: 4 Arcane or Darkness. Pandemic: 4 Poison | made 10.3-10.9 on 3 runs; once held by Poison 2/4 |
| The Taking | Hollow Star: Survivor (Feat). Kindled Ward: the Broken Ward (Ruins secret, ch 6) | Black Sun: 4 Darkness. Pyre: 4 Light | made 12.4 and 13.7; once held by Light 3/4 |

**The road's level clock is kinder than the far shore's.** A build that keeps up reaches level 17 at
6.4-6.9 minutes on the Forest and the Ruins (`.ai/run-pacing.md` 10.4), so on the road the level gate
holds only a slow-killing pair. What holds a road recipe is, in order: not knowing it, the cards, the two
element gates, and the pick budget.

---

## 2. Ideas

Thirteen, in six groups. Sizes as `.ai/second-half.md` 9: **S** a day or less, **M** a few days, **L** a
week or more. "Eases" is a first guess, to be measured (section 4).

### A. Knowing the recipe

#### 1. Known from one half (S)

- **Sees and does.** Once Conjunctions are learned, a recipe shows the moment the save owns **either**
  parent: in the browser's Conjunctions section, the Codex's Recipes tab and the Achievements row. The
  missing half reads as its existing locked hint ("Starfall - hidden somewhere in the Broken Circle"). The
  lesson reel's last line names the save's first recipe.
- **Earned.** The Conjunctions lesson, then each parent as it is found.
- **Eases.** Sealing and planning. Taken blind, two named forms match about one time in four
  (`combo-spells.md` 2); a known plan matches every time. No gate moves.
- **Costs and risks.** It shows a goal two chapters before it can be met. It is not a second locked
  *system* on show (Conjunctions are learned by then), so it keeps `slow-start.md`'s rule. `IsKnown`
  gains a weaker sibling; the Feat row's "???" rule is unchanged.

#### 2. The plan on the page (M)

- **Sees and does.** Pin one known recipe (from the Codex, or the browser). The pause screen's Run tab
  shows its checklist, in the shape F4 gave the far sky's section: each parent in the book and its level,
  each named path's element count ("Light 2 of 4"), player level against 17, sealed or not. One line in
  `QuestTracker` (lower left) says the next step. The two linked rings also mark the **level 4** path that
  would make a fourth element: "Seals Sagittarius".
- **Earned.** The Conjunctions lesson.
- **Eases.** Sealing (both kinds), and the element plan, which today is invisible until level 8.
- **Costs and risks.** Screen room: the lower-left corner is `QuestTracker`'s and must stay one line.
  A checklist can read as a chore (a playtest question, not a harness one).

#### 3. Where the other half is (S)

- **Sees and does.** The world map card lists "Completes Shadow Bolt" beside a chapter reward that is a
  known recipe's missing parent (the Swamp's Lanterns Out, for a save that owns Void Lance).
- **Earned.** Ideas 1 and the parent's own deed.
- **Eases.** Planning across runs: which chapter to replay.
- **Costs and risks.** Card room. F4 had to cut the far sky's card line to the recipe's name alone, and a
  road card with four rewards is as full. A suffix on the existing reward line may be all that fits.

### B. Characters

#### 4. A wizard's recipe (S)

- **Sees and does.** The character select names the known recipes a wizard **suits**: those whose named
  paths are gated on an element his own spell carries, and which name neither of his own spells. Derived,
  never authored. Today: Merlin (Orb of Ages, Light and Arcane, freed in chapter 3) suits all three; Medea
  (Solar Flare, Light) Sagittarius and The Taking; the Archmage and the Sibyl (Arcane) Shadow Bolt; Laelaps
  (Wind) Sagittarius.
- **Earned.** Freeing the wizard.
- **Eases.** One element gate by one or two, and the pick budget: the picks on his own spell (his
  Apotheosis) also pay part of the recipe. Medea's Solar Flare brings Light 1 from her first second and up to
  2 with a Light milestone, so Pyre needs little beyond Kindled Ward's own.
- **Costs and risks.** Another line on a full card. Not a goal track for a freed wizard (rejected
  2026-11-03): nothing is listed to complete, only a fact about his spell.

#### 5. The Sibyl teaches it (S-M)

- **Sees and does.** `.ai/in-flight.md` item 37 already moves the lessons to freed characters. Make the
  Sibyl the Conjunctions teacher: she is freed in the Sunken Cave, the Geode Heart's own chapter, she is a
  prophetess, and her passive (Prophecy, +3 rerolls a run) is already the planner's. Her reel names the
  save's first recipe (idea 1).
- **Earned.** Freeing her (the nine mouths, chapter 4).
- **Eases.** Nothing by itself; it is how ideas 1 and 2 are introduced.
- **Costs and risks.** Item 37's own work (`Teachings` with a character teacher, the out-of-order rule of
  the ladder, `ValidateTeachers`). Two new reel panels. The Geode Heart keeps Frost Shard.

#### 6. Nostradamus foretells (M)

- **Sees and does.** From the Court on, the stage select lets the player name one known recipe before a
  campaign run. That run deals its parents **with the far sky's favour**: dealt first, every upgrade in the
  hand, the named Ascension first on the level 8 page (`Player.FarSky.cs`'s three hooks, pointed at the
  foretold recipe). On the far shore the sky's night wins. No Arcane bonus: that stays the sky's.
- **Earned.** Freeing Nostradamus (the seer's voice, the Court, chapter 12).
- **Eases.** The deal, as fully as F4 measured: parents in the book inside about a minute, against 8-13
  minutes or never without it.
- **Costs and risks.** It is most of the far sky on the road, which takes some novelty from the far shore;
  it comes late so the far shore still adds a recipe you did not choose, its reward and its waiver. The
  Sovereign Rite competes from the Emberdeep on (nothing fuses after it). Stage-select room on a phone.

### C. The shop

#### 7. Kindred Draw (S)

- **Sees and does.** A Power, three levels (first guess 100 / 160 / 220), in Level-ups. While you hold one
  parent of a known recipe, the other parent's new-spell card is dealt x2 / x3 / x4 as often; while you
  hold both, their upgrades are that much likelier among the upgrades dealt - the Betrothal Hoop's rule
  ("which upgrade, never whether"), made permanent and pointed at a recipe.
- **Earned.** The shop, shown once Conjunctions are learned.
- **Eases.** The deal. At III, a guess of 3-5 minutes to both parents in the book where the road takes
  8-13 or never, and both to level 7 two or three minutes sooner.
- **Costs and risks.** It leans away from the wizard's own spell, so it can only make his Apotheosis later.
  No XP, charge or tag, so the level clock and the Apotheosis floor are untouched
  (`stage-secrets.md` 3). It competes with Powers for Arcane, which is the point.

### D. The gate, eased by a deed

#### 8. The pair shares its light (S code, M wording)

- **Sees and does.** While both parents of a known recipe are held, each named path's element gate also
  counts the **other parent's** weight. Pyre reads "4 Light (Light 2, Hollow Star 2)". One change in the one
  authority, `Player.EvaluateElementGate`, so the browser, the locked card and the strip agree.
- **Earned.** A mid-campaign deed. Recommended: the Archivist's (the Mystic Ruins, chapter 6), the keeper
  of the books, who "teaches how two pages are bound"; it reaches every save as The Taking becomes known.
- **Eases.** The element gate, nearly whole: two parents at level 7 bring about 2 + 2, which meets both
  paths for all three built recipes. Every F4 miss on an element (two of four short) closes. A **half**
  variant (the partner counts at most 1) leaves one tag to find for each path.
- **Costs and risks.** It is the far shore's waiver's cousin: Eric may read it as too close (question 1).
  It touches only named paths of non-own spells, so the Apotheosis is unmoved. Ascensions rise only by the
  recipe's two, which a planned run spends its picks on anyway.

#### 9. The named paths open sooner (S)

- **Sees and does.** A named path of a known recipe is offered from player level 15 while its partner is
  held. The level 8 card says why.
- **Earned.** A castle deed. Suggested: the Constable's (the Last Bridge, chapter 10), where the clock slows.
- **Eases.** The level gate, by two levels: about a minute on the road (a level every 25-33 s there) and
  more in the castle.
- **Costs and risks.** Small on the road, where 17 rarely binds; it matters on slow pairs only. Keeps the
  Ascension apart from the Apotheosis (21) by construction, since the own spell is never a parent.

### E. Content and the castle

#### 10. A recipe the teaching chapter completes (M-L)

- **Sees and does.** Build one recipe whose parents a save holds by chapter 4, so the lesson does not name
  Sagittarius first. **Maelstrom** (F6's, `combo-spells.md` 4): Riptide is the Hollow Choir's, the lesson
  chapter's own boss, and Cyclone Slash is Elementalist's. Or **Burning Briar**: Thorned Vine (Elderbark)
  and Cinderbreath (a starter), held from chapter 1.
- **Earned.** The chapter 4 boss (Maelstrom), or chapter 1 (Burning Briar).
- **Eases.** The first experience, which today is the hardest recipe. A design rule for later recipes goes
  with it: **let the two parents share one element where the verb allows** (Reprisal's do: Light), since a
  shared element halves the element plan.
- **Costs and risks.** Content. Maelstrom is M-L and F6 builds it anyway; building it first of F6-F10 only
  reorders. Burning Briar is M and not the Pyromancer's to make.

#### 11. The castle's recipes (M)

- **Sees and does.** Each castle chapter holds one recipe, named on its map card, and favours it there:
  the Cursed Dungeon **Shadow Bolt** (the Gaoler paid it before it was a Conjunction), the Throne **The
  Taking** (his own, made in his hall), the Last Bridge **Hailfront** once built (`combo-spells.md` 4: the
  spell the Bridge is built for).
- **Earned.** Reaching the castle.
- **Eases.** The deal, in the four chapters where the clock is slowest.
- **Costs and risks.** The far sky in miniature, fixed per place. The recipe still works anywhere, so it is
  not the rejected world altar (`combo-spells.md` 2). An alternative to idea 6, not a partner.

### F. The run's own deal

#### 12. Chests and boons lean to the plan (S)

- **Sees and does.** With a recipe pinned (idea 2) and a named path short, the boon dealt leans to the
  missing element (the `BoonOwnElementWeight` rule pointed at the deficit, not the deepest element), and
  chest relics carrying it are weighted up, the Gimmel Ring's pieces with them.
- **Earned.** With the pin.
- **Eases.** An element gate by about one where a carrier exists. Not for Poison, Arcane, Fire or
  Lightning through boons, and The Taking has no Darkness-and-Light relic (a new one would be M).
- **Costs and risks.** Small. Rarity is unchanged: it decides which, never whether.

#### 13. Conjunction Feats pay forward (S)

- **Sees and does.** Each recipe's own Feat also pays a Kindred Draw level, or Arcane Energy.
- **Earned.** Making a recipe.
- **Eases.** Every later recipe, a little.
- **Costs and risks.** A snowball: the first is as hard as today and only the rest ease, which is the shape
  of the rejected "one rescue makes the next easier" (2026-11-03). Listed so it is decided, not drifted into.

### At a glance

| # | idea | earned | eases | size |
|---|---|---|---|---|
| 1 | known from one half | the lesson | sealing, plan | S |
| 2 | the plan on the page | the lesson | sealing, plan | M |
| 3 | where the other half is | the parent's deed | plan | S |
| 4 | a wizard's recipe | freeing him | element, picks | S |
| 5 | the Sibyl teaches it | the Cave (item 37) | - | S-M |
| 6 | Nostradamus foretells | the Court | deal, fully | M |
| 7 | Kindred Draw | the shop | deal | S |
| 8 | the pair shares its light | the Archivist | element | S-M |
| 9 | named paths open sooner | the Constable | level | S |
| 10 | a recipe chapter 4 completes | ch 4 (or 1) | first recipe | M-L |
| 11 | the castle's recipes | the castle | deal | M |
| 12 | chests and boons lean | the pin | element, a little | S |
| 13 | Feats pay forward | a recipe made | the next | S |

### Considered, and not proposed

- **The far sky's waiver on the road.** Eric's choice is the far shore's alone.
- **A Muster.** F4: landing the slow recipes by minute 10 would take 16-20 levels, and a level buys nothing
  for the element gates.
- **More XP, rerolls or bans from a find.** `stage-secrets.md` 3 forbids them; the Apotheosis floor and the
  level clock were measured with the shop at its cap.
- **An item as the recipe's key.** A second gate on a system Eric gave one (`new-items.md` 11).
- **Any-Ascension recipes, automatic fusion, a world altar, the own spell as a parent** (`combo-spells.md`
  0 and 2).

---

## 3. The shortlist: a ladder across the campaign

Four pieces, each removing one measured brake in the order the campaign meets it.

| rung | when | piece | the brake it removes |
|---|---|---|---|
| I | chapter 4 | **Know it**: ideas 1 and 2, introduced by the lesson (5, if item 37 lands), with idea 4's line on the character select | not knowing; sealing |
| II | chapters 4-8 | **Kindred Draw** (7), bought | the cards |
| III | chapter 6 | **The pair shares its light** (8), the Archivist's deed | the element gates |
| IV | chapter 12 | **Nostradamus foretells** (6), the castle's slower clock | the cards, fully |

**Why these four together.**

- **Each is earned by a different kind of progress**: a lesson, the shop, a boss, a freed wizard. That is
  Eric's "naturally work towards": the player meets one at a time, as he meets the systems.
- **Each is strictly less than the far sky's package**, so the far shore still adds something: a recipe
  you did not choose, every night, with its reward and the waiver.
- **None touches XP, charges or tags.** The level clock (`run-pacing.md` 10.4) and the Apotheosis floor stay
  where they were measured; III changes only how a named path's existing gate counts.
- **One locked thing on show at a time.** Rung I appears with the lesson. II is a shop row shown from then.
  III shows only as "teaches a lesson" on the Archivist's card, the one locked thing on show in chapters
  5-6 (the Rite comes with the Emberdeep). IV arrives with its wizard.
- **It is cheap.** I is S + M, II S, III S-M, IV M; IV reuses F4's favour code.

**Its companion is idea 10.** Without a recipe chapter 4 completes, rung I names Sagittarius to most saves,
and the first recipe a player plans is the one no bot has made (question 5).

---

## 4. How to measure it

The bot already plans (`--seek-combo`), so **rung I is a playtest question**: does a person read the
checklist and plan? Rungs II-IV are harness questions. Model a skilled player
(`wizard-survivors-measure-like-a-skilled-player`): **`--farmed` buys no rerolls or bans**
(`difficulty-curve.md` 1.2's list), and a planner would, so give the planner's farmed save Fate Fracture
and Proscription, and read `--all-unlocked` as the ceiling.

**The protocol** (`run-pacing.md` 10.5; every minute is a pace minute):

```sh
"$GODOT_BIN" --headless --fixed-fps 30 --path . scenes/_BalanceHarness.tscn -- --stage=4 --character=14 --seed=1234 --seek-combo=shadow_bolt --farmed --immortal --minutes=15 --stop-when-made --no-save-writes
```

**The grounds** are the chapter after each recipe becomes knowable, so `--farmed` owns both parents:
Sagittarius on the Sunken Cave (`--stage=2`), Shadow Bolt on the Mystic Ruins (`--stage=4`), The Taking on
the Frozen Waste (`--stage=5`), and all three in the Court (`--stage=12`) for the castle, where F4 has
favoured rows to compare. **The wizards** are a suiting one (idea 4) and Laelaps (8, no recipe names his
spell): the Archmage (14) for Shadow Bolt, Medea (20) for The Taking and Sagittarius. Indices are
`CharacterRoster.cs`'s on this tip. Seeds 1234, 7 and 99.

**New flags**, each set by name so an older tree refuses rather than misbuilds: `--curate=N` (sets N
spells aside in the Spell Index, as N bought Redactions allow and never below the pool's floor of 12 - a
planner does this and no measurement has), `--kindred=N` (rung II), `--pair-shares` (rung III),
`--foretell=ID` (rung IV). `--give-relics=betrothal_hoop` exists.

| cell | flags beyond the protocol | what it answers |
|---|---|---|
| A | none | the road today, never measured since `combo-spells.md` 6 |
| A' | `--curate=8 --give-relics=betrothal_hoop` | what the game already has, used well |
| B | A' + `--kindred=3` | rung II alone |
| C | B + `--pair-shares` | rungs II and III |
| D | C + `--foretell=<id>`, the Court | rung IV |
| E | C's flags, `--policy=ordinary`, no `--seek-combo` | the guard rail: still a plan, not a drop |

**Report each run in F4's shape**, plus the wizard's own form:

| recipe | ground | wizard | seed | cell | parents in the book | named paths taken | **made** | level at 10 / 15 | own form |
|---|---|---|---|---|---|---|---|---|---|

**Targets:**

- **C:** Shadow Bolt and The Taking made by about pace 11-12 on two of three seeds each; Sagittarius by 13
  on one at least (its draw wants walking). Three minutes and the boss left to use it, as
  `combo-spells.md` 2 hoped.
- **D:** every built recipe by 12.5 in the Court (F4's favoured Court: Shadow Bolt 10.9, The Taking 13.7).
- **E:** a recipe met in at most one run in five.
- **Unmoved:** level at 15:00 within a level of A (nothing touched XP); at most four Ascensions on a planned
  run; one `--seek-apotheosis` run per wizard with the form where it was (level 21 holds it).

**Decision rules, in order:**

1. **A' lands by 11-12:** the tools exist and only teaching is missing. Build rung I, teach the set-aside in
   its checklist, and hold II-IV.
2. **B lands without III:** III moves to the castle or is dropped.
3. **C lands before pace 10 on most seeds, or E fails:** III at half strength.
4. **C misses:** read the row's miss. Cards: Kindred Draw at x5. Element: III is right and the miss is
   elsewhere. Level: the pair is a slow killer, and idea 9 is the lever, not the XP curve.

**What no harness checks:** whether a person plans from the checklist or ignores it; whether a pin feels
like a chore; whether the Sibyl's reel lands; whether a recipe made on the road feels earned or handed over.

---

## 5. Questions for Eric

1. **The element gate on the road.** The far shore waives it for tonight's recipe. May the road ease it for
   a recipe's named paths as an *earned* rule, the recipe itself unchanged (idea 8)? **Recommended: yes**,
   from the Archivist, at full strength; half if the bot then makes recipes before pace 10.
2. **A favour on the road.** Should anything on the road deal a recipe's parents *first*, as the far sky
   does? **Recommended: only late and only chosen** - Nostradamus from the Court (idea 6). Before that, a
   lean (Kindred Draw), never "first".
3. **Item 37's teacher.** When the Conjunctions lesson moves to a character, should it be the Sibyl
   (freed in the Geode Heart's chapter, a prophetess, +3 rerolls)? **Recommended: yes**, and her reel names
   the save's first recipe.
4. **Known from one half.** Show a recipe once the save owns either parent, the missing half by its
   existing locked hint (idea 1)? **Recommended: yes**, from the lesson on.
5. **The first recipe.** On the common path it is Sagittarius, which no bot has made. Build a recipe
   chapter 4 completes before F6-F10's others (idea 10)? **Recommended: Maelstrom first of the gods'
   five**; Burning Briar if F6 waits.
6. **Kindred Draw.** A new Power, three levels at 100 / 160 / 220, shown once Conjunctions are learned?
   **Recommended: yes.**
7. **Paying forward.** Should making one recipe make the next easier (idea 13)? **Recommended: no.**
8. **The castle.** Nostradamus's chosen recipe (idea 6), or each castle chapter's fixed one (idea 11)?
   **Recommended: Nostradamus**: a planner chooses. The castle's recipes if the stage-select pledge proves
   too much for a phone.
9. **The target.** A skilled player's planned recipe by about pace 11-12 on the road, the castle allowed a
   minute more? **Recommended: yes.**
