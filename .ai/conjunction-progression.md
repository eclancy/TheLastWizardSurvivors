# Conjunction progression: working toward a recipe through the campaign

Eric, 2026-11-08: *"start brainstorming ways to help combination spells happen in a way that the player
can naturally work towards in the campaign through unlocks, etc."*

**Status: decided by Eric (2026-11-08) and built (2026-11-09)** - section 3's four rungs, every question in
section 5 answered as recommended. Section 6 says what was built and where, what the measurement found (rung III ships at
half strength by the doc's own rule 3), and what is left for Eric.

**Decided, Eric, 2026-11-08:** having read the summary (the four-rung ladder, and that the doc's other
recommendations would be taken as they stand), *"Build the ladder"*. So the shortlist (section 3) is built
rung by rung - I Know it, II Kindred Draw, III the pair shares its light, IV Nostradamus foretells - and each
question in section 5 is decided as it recommends (marked there). His rules no rung may loosen: both parents
at level 8 down both named Ascensions; one locked thing on show at a time (`.ai/slow-start.md`); every
non-starter spell from exactly one achievement (`UnlockCatalog`); and no rung adds XP, rerolls, bans or
element tags.

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

**All nine decided by Eric, 2026-11-08, as recommended** ("Build the ladder"). Each carries its decision.

1. **The element gate on the road.** The far shore waives it for tonight's recipe. May the road ease it for
   a recipe's named paths as an *earned* rule, the recipe itself unchanged (idea 8)? **Recommended: yes**,
   from the Archivist, at full strength; half if the bot then makes recipes before pace 10. **Decided: as
   recommended.**
2. **A favour on the road.** Should anything on the road deal a recipe's parents *first*, as the far sky
   does? **Recommended: only late and only chosen** - Nostradamus from the Court (idea 6). Before that, a
   lean (Kindred Draw), never "first". **Decided: as recommended.**
3. **Item 37's teacher.** When the Conjunctions lesson moves to a character, should it be the Sibyl
   (freed in the Geode Heart's chapter, a prophetess, +3 rerolls)? **Recommended: yes**, and her reel names
   the save's first recipe. **Decided: as recommended** - when item 37 lands; not part of the ladder's build.
4. **Known from one half.** Show a recipe once the save owns either parent, the missing half by its
   existing locked hint (idea 1)? **Recommended: yes**, from the lesson on. **Decided: as recommended.**
5. **The first recipe.** On the common path it is Sagittarius, which no bot has made. Build a recipe
   chapter 4 completes before F6-F10's others (idea 10)? **Recommended: Maelstrom first of the gods'
   five**; Burning Briar if F6 waits. **Decided: as recommended** - content, built with F6, not here.
6. **Kindred Draw.** A new Power, three levels at 100 / 160 / 220, shown once Conjunctions are learned?
   **Recommended: yes.** **Decided: as recommended.**
7. **Paying forward.** Should making one recipe make the next easier (idea 13)? **Recommended: no.** **Decided: no.**
8. **The castle.** Nostradamus's chosen recipe (idea 6), or each castle chapter's fixed one (idea 11)?
   **Recommended: Nostradamus**: a planner chooses. The castle's recipes if the stage-select pledge proves
   too much for a phone. **Decided: Nostradamus.**
9. **The target.** A skilled player's planned recipe by about pace 11-12 on the road, the castle allowed a
   minute more? **Recommended: yes.** **Decided: as recommended.**

---

## 6. Built

Built 2026-11-08/09 on the branch `worktree-agent-a68bd101e5d22e3c4`, cut from `main-ff86ni`, rung by rung; the trunk
was merged in twice - the far sky's element waiver before rungs III and IV, the Sovereign Rite as a two-spell Conjunction
at the end. **One save schema step: 27** (the pin).
`scripts/ConjunctionLadder.cs` is the one authority on what each rung shows and allows, as `ComboCatalog` is on the
recipes; nothing here changes `ComboCatalog.Evaluate`, the Ascension's gates' own numbers, XP, rerolls, bans or a tag.

### Rung I, know it (built)

| piece | where |
|---|---|
| **known from one half** | `ConjunctionLadder.IsShown`: today's rule (`ComboCatalog.IsKnown`, both halves owned, shown from the Apotheosis on) or, once the Conjunctions lesson is learned, either half. Read by the browser (`Player.BuildComboPreviews`), the Codex's Recipes and its level 8 "Joins" note (`CodexModel`), and the Feat's Achievements row (`AchievementVisibility.Recipe`; the Rite keeps its own rule). The missing half is its own locked hint (`ConjunctionLadder.MissingHalfLine`, the Codex ingredient's note). |
| **the result stays sealed** | `ConjunctionLadder.ShownName`: a recipe is called by its two parents ("Hunter's Draw + Starfall") until made, its name after - the existing rule of the browser and the Codex, kept on every ladder surface (decision 1 below). |
| **the pin** | `SaveData.PinnedRecipeId` (**schema 27** - built as 26, renumbered at the merge when the Sovereign Rite's branch reached the trunk with 26 first; `MigrateToPinnedRecipe`: pins nothing; `ValidatePinnedRecipeMigration`). One at a time; refused before the lesson, for the Rite and for a recipe the save knows nothing of (`ConjunctionLadder.MayPin`). Pinned from the Codex's recipe page, **Pin / Unpin** in its head (`EvolutionCodex.Detail.PinToggle`), on the main menu and over a run alike. |
| **the checklist** | `Player.BuildRecipeChecklist` (`Player.Ladder.cs`): each parent in the book and its level against 7, each named path's element count ("Pandemic: Poison 1 of 4", from `EvaluateElementGate`, the one authority), your level against 17, the level 4 path that would seal it, or why it is sealed; and the one next step. The pause screen's Run tab shows it as **Pinned Conjunction**, after the far sky's section (`Node2DGame.Ladder.cs`, one marked line in `ShowEscapeRunOverview`). |
| **the corner's one line** | `QuestTracker.SetRecipeLine`: the two linked rings and the next step ("Void Lance to level 7 · 1/7"), the last row of the tracker, on its one-second reading; "Shadow Bolt made" once made. Hidden while nothing is pinned. |
| **the level 4 mark** | `Player.NoteRecipeSeals` fills `LevelUpOption.RecipeSeals`; the level 4 card (both shapes) carries the rings and "Seals the Conjunction with Starfall" (the recipe's name once made) in the page's rubric (`LevelUpMenu.BuildRecipeSealMark`). `ConjunctionLadder.WouldSealByElement` is checked against `ComboCatalog.Evaluate`'s own fourth-element seal on every parent's every level 4 path; today three paths mark (Starfall's Frostfall, Contagion's Marsh Fever, and since the Rite became a recipe Arcane Explosion's Static Nova). |
| **a wizard's recipe** | `ConjunctionLadder.Suits`, the character select's **Suits** row (`CharacterSelection.BuildFacts`): shown recipes with a named path gated on an element his own spell carries, neither parent his own (his starting spell or one that is his alone), never the one he opens on. Derived; on a save with every spell: Merlin and Nimue all three; Medea Sagittarius and The Taking; the Dark Wizard Sagittarius and Shadow Bolt; the Archmage, the Sibyl, John Dee, Baba Yaga and Ma Liang Shadow Bolt; Laelaps, Seimei, Wayland and Morgan Sagittarius; Nostradamus and Artemis The Taking. |

### Rung II, Kindred Draw (built)

A Power in the shop's Level-ups section, **100 / 160 / 220** (`ConjunctionLadder.KindredDraw*`; icon
`tools/art/upgrade_icons.py` `kindred_draw`, two rings drawn together along a thread), **shown only once the
Conjunctions lesson is learned** (`ConjunctionLadder.KindredDrawShown`; `MainMenu.BuildShopSteps` leaves it off the
shelf before). In a run (`Player.ApplyKindredDrawLean`, after the Betrothal Hoop's lean, never in a browse): for every
recipe the save knows (owns both halves), not the one he opens on, not his own spell's, not made and not sealed - one
parent held, the other's new-spell card is **x2 / x3 / x4** as heavy among the new spells; both held, both their
upgrades that much heavier among the upgrades. `ConjunctionLadder.ApplyKindredDraw` only re-weighs cards already in a
pool, so it can never deal a card the save could not be dealt, and the group shares (`NormalizeOfferWeightsByGroup`)
keep it "which card, never whether": `UpgradeOfferChance` is untouched.

### Rung III, the pair shares its light (built)

**The rule** (`ConjunctionLadder.PairShareFor`): while both parents of a recipe are held, each named Ascension's element
gate also counts the **other parent's** tags - its current weight in every element the gate does not already count, so a
shared element is never counted twice. Zero unless earned, never for a path no recipe names on that spell (a third
Ascension), never with the partner not held, never for a sealed or made recipe (a parent down its other path, a fourth
element, the wizard's own spell). At the parents' level 7 and with no level 4 element path, the shares are: Hunter's
Mark +1 (Starfall's Light), Constellation +2 (Hunter's Draw's Metal and Wind), Cosmic Collapse +1, Pandemic +2, Black Sun
+1, Pyre +1; a deepening level 4 adds to them. **It moves no tag**: the element tiers, attunement and every other gate
read the counts as they were. `AscensionRequirement.Shared` / `SharedBy` carry it beside `Have`, which stays the player's
own count; `Met` and `Missing` read both.

**The one reading** (`Player.EvaluateElementGateInRun`): the strip's lock (`MarkUnearnedAscensions`), the browser
(`BuildAscensionPreview`), the pinned checklist and the Codex's chips all read the gate through it. The locked card says
"Requires 4 Light (Light 2, Hollow Star 1: 3/4)"; the Codex adds a "Hollow Star +1" chip; the checklist "Pyre: Light 2,
Hollow Star 1: 3 of 4".

**With the far sky's waiver** (F4's decision 10, merged into the trunk while this was built): the waiver is applied only
where the gate still stands unmet with the share (`Waived = !Met && FarSkyWaivesElementGate`), so the two never stack.
On the far shore tonight's two named paths are open anyway and the share only shows how close the player is; every
other recipe's named paths, there and on the road, get the share alone.

**Earned** by the Archivist's deed (`ruins_delver`, the Mystic Ruins, chapter 6), whose card now reads "Unlocks Spiritual
Weapon and teaches a lesson". It is a lesson (`Teaching.PairShares`, `Teachings.PairSharesLesson`, "A Conjunction's two
spells share their light"), owed once the deed is done **and** the Conjunctions lesson learned - never before the system
it eases, never on the same evening - and learned when its reel plays after a run. The reel reuses the Conjunctions
lesson's two panels with lines of its own (`Prologue.Ladder.cs`: "The Archivist kept how two pages are bound." / "Held
together, two spells share their light."): no new art. Nothing of it is shown before it is learned.

**It ships at half strength** - the partner counts at most 1 toward each named path - because question 1 said "full;
half if the bot then makes recipes before pace 10", and at full it made them at 7.3-9.4 on four of six road runs
(Measured, below). `ConjunctionLadder.PairShareHalf` is the switch; `_BalanceHarness --pair-shares=full` measures the
full rule.

### Rung IV, Nostradamus foretells (built)

**Earned** by freeing Nostradamus (the Court, chapter 12) on a save that has learned Conjunctions
(`ConjunctionLadder.ForetellEarned`). **Named on the world map's card**, over Enter (`WorldMap.Foretell.cs`): one button,
"✶ Foretold: Void Lance + Contagion" (the name once made), that turns to the next recipe that may be foretold, then to
"Nostradamus foretells nothing", then round. Only on an open campaign chapter - never the far shore, where the sky's
night wins, never the Boss Rush, never off the road - and only when there is a recipe to foretell. **What may be
foretold** is exactly what the far sky may name for that wizard (`FarSky.Candidates`): a fusion of two spells, never the
one he opens on, never one naming his own spell, both parents dealable from the save. The choice is the session's
(`ConjunctionLadder.ForetoldChoice`), and until one is made the **pin stands in** for it - so the plan the player pinned
is the one foretold unless he turns it.

**The run** (`Node2DGame.StartForetold`, after `StartFarSky`): the foretold recipe is dealt by **the far sky's favour
itself** - `Player.FarSky.cs`'s three hooks read `FavouredRecipe` (tonight's, else the foretold) and `FavourHolds` - so its
parents come first when a slot is free, their upgrades are always in the hand, the named Ascension leads the level 8
page. No fork. What stays the sky's: the Arcane Energy and the Feat (they read `FarSkyRecipe` / `FarSkyMade`, never the
foretold), and the element waiver (the far shore's alone). The pause screen's section becomes **Foretold Conjunction**
("Nostradamus foretold it: its two spells come to your hand first.") and the quest corner follows it.

### Measured (2026-11-09, section 4's protocol)

`_BalanceHarness --headless --fixed-fps 30 ... --seek-combo=<recipe> --planner --immortal --minutes=15 --stop-when-made
--timescale=3`, one or two runs at a time. Every minute is a **pace** minute. Two changes to the protocol, both forced:

- **`--planner`, not `--farmed`.** The farmed save finds the teachers but never plays their reels, so it had learned
  nothing - not the Ascension, not Conjunctions - and could make no recipe at all. `--planner` is `--farmed` as a
  planner stands there: the lessons the road behind him taught (`Teachings.Owed`, lowest first) and Fate Fracture 5 and
  Proscription 3 (the rerolls and bans section 4 says a planner buys). A planner at the Frozen Waste has beaten the
  Archivist, so **rung III is learned there naturally**: the "A" and "A'" rows for The Taking marked "III" had it, and the
  true baseline is the `--pair-shares=off` rows.
- **Sagittarius on the Blighted Swamp (`--stage=3`), not the Sunken Cave.** The Cave is where Conjunctions are taught (the
  Geode Heart is its own vault), so a save standing in it has not learned them yet; the Swamp is the chapter after.

Wizards: the Archmage (14) for Shadow Bolt, Medea (20) for The Taking and Sagittarius - each suits his recipe (rung I's
`Suits`). Cells: **A** none; **A'** `--curate=8 --give-relics=betrothal_hoop`; **B** A' + `--kindred=3`; **C** B +
`--pair-shares` (full); **C½** B + `--pair-shares=half`; **D** C + `--foretell=<recipe>` in the Court (C½ there, the
planner having beaten the Archivist); **E** C's flags, `--policy=ordinary`, no `--seek-combo`.

| recipe | ground | seed | cell | parents in the book | named paths taken | **made** |
|---|---|---|---|---|---|---|
| Shadow Bolt | Mystic Ruins | 1234 | A | 0.3 / 0.9 | 5.6 / 9.1 | **9.4** |
| Shadow Bolt | Mystic Ruins | 7 | A | 0.3 / 3.1 | 5.6 / - (Pandemic's Poison) | not by 15 |
| Shadow Bolt | Mystic Ruins | 1234 | A' | 0.2 / 2.6 | 8.2 / 13.7 | **14.0** |
| Shadow Bolt | Mystic Ruins | 1234 | B | 0.3 / 0.9 | 5.3 / 14.7 | not by 15 (met 14.7) |
| Shadow Bolt | Mystic Ruins | 7 | B | 2.0 / 0.9 | 9.7 / - (Pandemic's Poison) | not by 15 |
| Shadow Bolt | Mystic Ruins | 1234 | C | 0.2 / 0.8 | 5.4 / 6.7 | **7.3** |
| Shadow Bolt | Mystic Ruins | 7 | C | 2.0 / 1.0 | 8.8 / 6.8 | **9.4** |
| Shadow Bolt | Mystic Ruins | 1234 | C½ | 0.2 / 0.9 | 5.4 / 14.0 | **14.3** |
| Shadow Bolt | Mystic Ruins | 7 | C½ | 2.0 / 0.8 | 8.8 / 7.9 | **9.3** |
| Shadow Bolt | Mystic Ruins | 99 | C½ | 0.5 / 0.3 | 5.6 / 7.0 | **7.4** |
| The Taking | Frozen Waste | 1234 | A, III off | 0.3 / 6.0 | 6.3 / 11.2 | **11.4** |
| The Taking | Frozen Waste | 7 | A, III off | 0.4 / 3.8 | 7.2 / 11.6 | **11.8** |
| The Taking | Frozen Waste | 1234 | A, III learned | 0.2 / 6.2 | 6.5 / 9.8 | **10.0** |
| The Taking | Frozen Waste | 1234 | A', III learned | 0.5 / 0.3 | 6.8 / 5.4 | **7.2** |
| The Taking | Frozen Waste | 1234 | B, III off | 0.4 / 0.3 | 6.2 / 8.3 | **8.9** |
| The Taking | Frozen Waste | 7 | B, III off | 1.2 / 0.4 | 13.6 / 6.5 | **14.0** |
| The Taking | Frozen Waste | 1234 | C | 0.5 / 0.3 | 5.6 / 7.7 | **8.2** |
| The Taking | Frozen Waste | 7 | C | 1.3 / 0.5 | 8.0 / 6.5 | **8.3** |
| The Taking | Frozen Waste | 1234 | C½ | 0.5 / 0.3 | 6.0 / 8.0 | **8.3** |
| The Taking | Frozen Waste | 7 | C½ | 1.4 / 0.5 | 7.7 / 6.6 | **8.5** |
| The Taking | Frozen Waste | 99 | C½ | 0.2 / 0.5 | 5.1 / 6.2 | **6.6** |
| Sagittarius | Blighted Swamp | 1234 | A | 1.1 / 2.5 | 9.9 / 11.6 | **11.8** |
| Sagittarius | Blighted Swamp | 7 | A | 0.4 / 5.7 | 11.4 / 12.9 | **13.2** |
| Sagittarius | Blighted Swamp | 1234 | B | 1.0 / 2.3 | 9.0 / 10.7 | **10.9** |
| Sagittarius | Blighted Swamp | 7 | B | 0.6 / 1.5 | - / 11.8 (Hunter's Mark not reached) | not by 15 |
| Sagittarius | Blighted Swamp | 1234 | C | 1.0 / 2.4 | 9.0 / 11.1 | **11.5** |
| Sagittarius | Blighted Swamp | 7 | C | 0.6 / 1.5 | 10.6 / 11.8 | **12.1** |
| Shadow Bolt | the Court | 1234 | C½ | 0.2 / 0.7 | 6.2 / 8.0 | **8.3** |
| Shadow Bolt | the Court | 1234 | **D** | 0.3 / 0.3 | 4.8 / 5.3 | **5.5** |
| The Taking | the Court | 1234 | C½ | 0.4 / 0.2 | 5.4 / 6.9 | **7.2** |
| The Taking | the Court | 1234 | **D** | 0.3 / 0.5 | 5.4 / 5.2 | **5.9** |
| Sagittarius | the Court | 1234 | C½ | 2.3 / 0.6 | 7.7 / 6.2 | **8.2** |
| Sagittarius | the Court | 1234 | **D** | 0.3 / 0.5 | 6.5 / 5.2 | **6.8** |
| The Taking | Frozen Waste | 1234 | C½, after the Rite's merge | - | - | **8.5** |
| Shadow Bolt | the Court | 1234 | **D**, after the Rite's merge | 0.2 / 0.4 | 5.0 / 13.9 (Pandemic's Poison) | **14.3** |

The last two rows were run on the tree merged with the Sovereign Rite, as a smoke test of the merge: the favour and the
share behave as before (The Taking 8.5 against 8.3), and the foretold Shadow Bolt shows how wide one recipe's spread is
at half strength - Pandemic's four Poison held it to 13.9 where the run before the merge had it at 5.3.

**E, the guard rail: 0 of 5.** Ordinary bot with C's flags (Kindred Draw III, the share at full, the set-aside, the
hoop): the Ruins (the Archmage, 1234; Laelaps, 7), the Frozen Waste (Medea, 1234; the Archmage, 7) and the Swamp (Medea,
1234), fifteen minutes each - no recipe offered in any run (the summary's `met=[]`). A recipe stays a plan, not a drop.

**Unmoved:** the runs that went to minute 15 stood at level 42-43 (the Archmage on the Ruins, A and B alike); nothing
touched XP. No run took more than four Ascensions.

What it says:

- **The road with a planner is kinder than section 1 assumed.** Cell A - no rung at all - made Shadow Bolt at 9.4 (and
  not at all on seed 7), The Taking at 11.4 and 11.8, Sagittarius at 11.8 and 13.2. Section 1 read F4's far-shore rows
  (an every-spell pool, Laelaps, the favour off: nothing made in twenty minutes); a planner's farmed road save owns far
  fewer spells, so its deal is less diluted, and he fishes with ten rerolls and three bans. Decision rule 1 ("A' lands by
  11-12: hold II-IV") was not the question any more - Eric chose the ladder - but it is the honest reading of the A rows.
- **Kindred Draw (B) gets the late parent into the book** (Kindled Ward at 0.3 instead of 6.0; Starfall at 1.5 instead
  of 5.7) **but moves the made minute little**: for a planner fishing with rerolls the cards were no longer the brake.
  B landed alone once (The Taking, 8.9) and missed or ran late on the other five (rule 2: III is not moved to the castle).
- **The element gate is the brake, as section 1 said, and rung III takes it off.** Every A/B miss on Shadow Bolt is
  Pandemic's four Poison; with the share (C) the Archmage made it at 7.3 and 9.4. Sagittarius is held by the level clock
  and Hunter's Draw's own climb to 7, not an element - III leaves it near 11.5-12.1.
- **Rule 3 fired: C landed before pace 10 on four of six road runs**, so the share ships **at half**
  (`ConjunctionLadder.PairShareHalf = true`). At half it still lands The Taking at 6.6-8.5 (Pyre's share is 1 either way
  until Hollow Star deepens, and Kindred Draw and the set-aside carry the rest) and Shadow Bolt at 7.4-14.3. **So the
  ladder overshoots the doc's road target (11-12) for this bot**; see "For Eric" below.
- **D, the Court: every recipe by 6.8 foretold** (5.5, 5.9, 6.8), against 7.2-8.3 not foretold - section 4's 12.5 with six
  minutes to spare. The castle's farmed budget puts the level clock far ahead of the road's; the favour then does what F4
  measured it does, the parents in the book inside a minute.
- **Noise.** `--seed` fixes the ground and the chests, not the level-up roll (Player's own RNG is randomised), so two
  runs of one cell differ by up to five minutes (The Taking B: 8.9 and 14.0). Two or three runs a cell read a direction,
  not a number.

**For Eric** (not decided here): for a planner the four rungs together make a road recipe at about minute 7-9 and a
castle one at 5.5-7, two to four minutes ahead of the targets section 4 set. Three levers, smallest first: Kindred Draw at
x1.5 / x2 / x2.5 (it mattered least); rung III on the road only from the castle (rule 2's move, though B did not land);
or accept it - the targets were set from the far shore's numbers, and a person who does not fish with ten rerolls and
three bans will be slower than the bot.

### Decisions this build made (the doc left them open)

1. **A recipe stays sealed until made, on every ladder surface.** Section 2 of `combo-spells.md` hides a recipe's result
   until it is made; the doc's examples ("Seals Sagittarius", a wizard who "suits Sagittarius") named it. The ladder keeps
   the existing rule: a recipe is "Hunter's Draw + Starfall" until made, its name after (`ConjunctionLadder.ShownName`) -
   on the checklist, the corner, Suits, the foretelling, and "Seals the Conjunction with Starfall" on the level 4 card.
2. **The corner's one line is the two linked rings and the next step**, not the recipe's name (which is sealed until
   made, and long by its parents' names): "Void Lance to level 7 · 1/7", the last row of the tracker. The next step is the
   first unmet thing in a planner's order - a parent to find or take, a parent's level, your level, then an element short.
3. **The pin is pinned from the Codex** (Pin / Unpin on a recipe's page, menu and pause alike), not the level-up browser.
4. **The level 4 mark covers every shown recipe**, not only the pinned one, as the level 8 rings do; never one he cannot
   make (his own spell, the one he opens on) or one already sealed or made.
5. **Suits excludes a recipe naming any spell of his own** - his starting spell, and one that is his alone
   (`UnlockDefinition.ExclusiveTo`) - and the one he opens on; the named path's gate is read off the parent's resource.
6. **Kindred Draw's recipes are those the save knows from both halves** (it only re-weighs cards already in the deal, so a
   half not owned is never there to lean toward), not made, not sealed, not his own spell's or his opener. Effect text:
   "a held Conjunction spell's partner, then both their upgrades, are dealt more often"; stat line "Partner cards +100% as
   often" per level. Not in `--full-shop`'s caps, so older full-shop reports stay comparable.
7. **The pair's share counts the partner's tags outside the gate's own elements** (a shared element is already in the
   player's count; counting it again would double it), and only for a live recipe (not made, not sealed). It ships at
   half (rule 3).
8. **Rung III is a lesson** (`Teaching.PairShares`), because the Archivist's card says "teaches a lesson": owed only
   once Conjunctions are learned, never on the same evening, its reel the Conjunctions lesson's own two panels with two new
   lines (no new art). A save that has already beaten the Archivist is taught it at the end of its next run.
9. **The foretelling's choice is the session's, and the pin stands in until one is made.** So a player who pinned a recipe
   and has freed Nostradamus has it foretold on every campaign run unless he turns the button to "nothing". This is the
   generous reading of "the player names one known recipe"; the strict one (nothing until named each session) is one
   line (`ConjunctionLadder.Foretold`, drop the `?? Pinned(save)`).
10. **What may be foretold is exactly the far sky's candidates** (`FarSky.Candidates`), so the two never disagree on what a
    wizard can make; and the foretold run pays nothing (no Arcane Energy, no Feat, no waiver) - those read `FarSkyRecipe`.
11. **No schema step for rungs II-IV**: Kindred Draw is an `ArcaneUpgradeLevels` key like every Power, the lesson rides
    `LessonsLearned`, and the foretelling is not saved. **One for rung I: schema 27**, `PinnedRecipeId`
    (`MigrateToPinnedRecipe` pins nothing; `ValidatePinnedRecipeMigration`). Numbered 26 while the trunk was at 25; the
    Sovereign Rite's branch reached the trunk with 26 first (`MigrateRiteToConjunction`), so the pin runs as 27.
12. **The harness:** `--planner` (the farmed save with the lessons its road taught, and the rerolls and bans a planner buys),
    `--curate=N` (the last N spells in book order that are not the target's parents or his own - a stable, seedless pick;
    a real planner would choose better), `--kindred=N`, `--pair-shares[=half|full|off]`, `--foretell=ID|off`, and a `met=`
    field listing every fusion the strip offered (the guard rail).
13. **The Sovereign Rite, a recipe since 2026-11-08** (Arcane Explosion down Resonance and Gravewell down Mass Grave,
    merged into the trunk at the end of this build): the ladder treats it as any recipe - shown from one half, pinnable,
    its checklist on the pause screen, leaned on by Kindred Draw, its named paths sharing (Resonance +2, Mass Grave +1 at
    level 7), on a wizard's Suits row (John Dee, the Dark Wizard, Merlin, Nimue) - **except Nostradamus, who never
    foretells it**: what he may foretell is the far sky's candidates, which never name the capstone (`FarSky.MayName`), and
    `ValidateForetell` says so. The coordinator recommended the same split. The checklist does not list the Rite's own late
    gate (`SovereignRite.MeetsGate`: the player's level and the fuel the other spells would pour in); its "Ready above the
    cards" is the recipe's, and the strip still waits for the gate.

### Verified

- `dotnet build`: 0 errors.
- The validator (`MainMenu.tscn --quit-after 300 -- --no-save-writes --validate-content`): **0 errors and 1 warning**
  (`test_wizard`). New checks, each a warning when broken: `ValidateConjunctionLadder` (rung I: a recipe shown from one
  half only once Conjunctions are learned, never from neither half; the Feat row follows; the pin refused before the
  lesson and for an unknown recipe, one at a time, through the save's round trip, cleared by a
  story reset; Suits derived by its rule; the level 4 mark agrees with `ComboCatalog.Evaluate`'s own seal on every
  parent's every level 4 path - three paths mark today, Frostfall, Marsh Fever and the Rite's Static Nova; rung II: the shelf hidden before the
  lesson, 100 / 160 / 220, x2-x4, re-weighs only the recipe's own cards and never adds one; rung III, `ValidatePairShares`:
  never off its deed, never a third Ascension, never an unheld partner, never a sealed recipe or the wizard's own spell,
  never more than 1 at half, never stacked with the far sky's waiver, its lesson owed and shown as section 3 says; rung IV,
  `ValidateForetell`: nothing before Nostradamus is freed or the lesson learned, exactly the far sky's candidates, never
  on the far shore or a Boss Rush, never the Rite, the pin standing in, a press walking every recipe and nothing) and
  `ValidatePinnedRecipeMigration`.
- `_UiShot` at 1280x720 and 540x1212 `--portrait --safe-area`, looked at (`user://ladder-*.png`): the pause screen's
  Pinned and Foretold Conjunction sections, the quest corner's line in a run, the shop's Kindred Draw card, the character
  select's Suits row, the level 4 page's "Seals Sagittarius" on Frostfall, the world map card's foretelling (a phone's card
  first pushed Enter off the foot of the screen; the map now gives up the row once the rung is earned), and the Codex's
  recipe page known from one half (the missing Contagion by its hint, the result "???", Unpin).

### Not run, and needs a person's eyes

- **Nobody has played any rung.** Whether a person reads the checklist and plans, whether a pinned line in the corner feels
  like a chore, whether the share's "Light 2, Hollow Star 1: 3 of 4" is understood without the lesson's two lines, whether
  Nostradamus's button reads as an offer on the map card, and whether a foretold road recipe feels earned or handed over.
- **The pace (Measured, "For Eric")**: the four rungs together land a planner's recipe two to four minutes ahead of section
  4's targets.
- **The lesson reel's two lines** are new words in Eric's story voice, after his 2026-11-08 review of the others.
- **Not built:** the Sibyl as the Conjunctions teacher (question 3, item 37's work), the lesson reel naming the save's first
  recipe (idea 1's last line), a recipe chapter 4 completes (question 5, F6's), the map card's "Completes Shadow Bolt"
  (idea 3), the share in the Codex's collection-mode tree (it reads `Player.EvaluateElementGate` without a run; the in-run
  browser and the checklist show it).
- **The Sovereign Rite's two-spell Conjunction** reached the trunk at the end of this build and was merged in (decision 13);
  the validator and a post-merge smoke run passed on the merged tree, and the measurement above predates it (its recipes
  are the three built before it; the Rite's own pace on the ladder is unmeasured).
- **A trunk merge regression, fixed on the trunk:** with the Rite in `ComboCatalog.Fusions`, the far sky's waiver check
  warned that the Rite's paths are not waived (they never are); the trunk's 03ac8c05 fixed it while this was merging.
