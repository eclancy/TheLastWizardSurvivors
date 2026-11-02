# The Evolution Codex: one page for every spell, item, evolution and recipe

**Status: approved by Eric (2026-11-02), being built.** Section 9 records his answers; the one change is
search on a controller, through an on-screen keyboard (section 2).
Wireframe: https://claude.ai/artifact/NEHkx6sJQXjwqYrdMtuie9 (four artboards: PC in a run with
Cinderbreath's tree, PC on the Recipes filter with Sagittarius's reverse lookup, the phone grid, the phone
detail sheet).

**Where it came from.** Feedback from a player (Amy, by email to eric@ecec.dev, 2026-11-02): replace the
Ascensions menu with a single-page "Evolution Codex" - search, category filters, a collection grid with
discovery states, a detail panel showing a spell's whole evolution tree with its requirements, and
combination recipes with reverse lookup - built from the game's real data and kept in its pixel-art look.
The request is right about the problem: a player has to put the progression together from three places
(the in-run Ascensions browser, the main menu's Spell Index and the pause screen's Run tab), the browser
already scrolls on a PC, and combination spells (`.ai/combo-spells.md`) add a fourth thing to learn.

---

## 1. What it replaces, and what it keeps

| today | after |
|---|---|
| **★ Ascensions** browser in the level-up screen (`LevelUpMenu.Ascensions.cs`): a tree per equipped spell, the Apotheosis section on top | **the Codex, in-run mode**, opened from the same button (renamed **★ Codex**) |
| **Spell Index** on the main menu: every spell and where to find the missing ones | **the Codex, collection mode**, opened from the same menu slot |
| pause screen **Run** tab: each held spell's milestones | kept as it is (it answers "what did this run become"); a **Codex** button beside its heading opens in-run mode |
| **Achievements** page | kept separate: it is deeds and rewards, not progression |
| **Arcane Upgrades** (the shop) | untouched |

One screen, two modes. The mode decides only what the states mean:

- **In-run mode** (from a level-up or the pause screen): levels are this run's, "Ready" means the strip
  can offer it now, requirements count this run's tags and level, the held spells sort first.
- **Collection mode** (main menu): no run, so no levels. States are the save's: owned or not, how to
  find a missing one (`UnlockCatalog`'s line, as the Spell Index says it now), which branches have been
  seen (`SeenAscensionIds`), which recipes made.

---

## 2. The layout

### PC (1280x720 canvas, the panel the level-up screen uses, about 1180x660)

```
+--------------------------------------------------------------------------------------+
| EVOLUTION CODEX            [ search spells, items, recipes...   ]     87 / 142   Back |
| ( All )( Spells )( Items )( Recipes )                       in-run: Pyromancer, L17  |
+------------------------------+-------------------------------------------------------+
| COLLECTION grid, 6 across    |  DETAIL of the selected entry                          |
| [ic][ic][ic][ic][ic][ic]     |  [icon] Cinderbreath   Fire   Lv 7 ######.             |
|  name name name name ...     |  "Breathes a cone of fire."                            |
| [ic][ic][??][ic][ic][ic]     |                                                        |
| ...  (scrolls inside only)   |  BASE ---> MUTATION (4) ---> ASCENSION (8) ---> FINAL   |
|                              |           Backdraft  *    Dragon's Breath      Cataclysm|
|                              |           Ember Trail     Phoenix Breath  ???  (Apoth.) |
|                              |  requirement chips under each node                     |
|                              |  USED IN:  Burning Briar  (+ Thorned Vine: Thicket)    |
+------------------------------+-------------------------------------------------------+
```

- **The header never scrolls**: title, search, progress, Back; under it the four filter tabs and, in
  in-run mode, one quiet line naming the run (wizard, level).
- **The grid** on the left, 6 across, 76-pixel cells: icon, the name on one line under it, and the
  state on the cell's frame (section 3). Only the grid scrolls, and only when the filter shows more
  than fit; selecting a cell never moves the page.
- **The detail panel** on the right updates as the selection moves (no second page). Its tree reads
  left to right in columns: **base, level 4 mutations, level 8 Ascensions, the final form** (the
  Apotheosis, when this spell is the wizard's), with gold arrows into what is open and dashed meta-ink
  arrows into what is not. **Requirements sit as chips under each node** ("Spell 8", "Fire 4/4",
  "You 17/17"), the element chip in its element's colour, the shortfall on the rubric band as the
  browser draws it now. Below the tree, **Used in**: every recipe this spell is an ingredient of.

### Phone (720x1280 canvas, portrait)

```
+------------------------------+        +------------------------------+
| EVOLUTION CODEX     87 / 142 |        | < Codex      Cinderbreath    |
| [ search...                ] |        | [icon] Fire  Lv 7 ######.    |
| (All)(Spells)(Items)(Recipes)|        |        BASE                  |
| [ic][ic][ic][ic][ic]         |        |         |                    |
|  ...  5 across               |  tap   |  Backdraft    Ember Trail    |
| [ic][ic][??][ic][ic]   ----->|  ----> |      |  \     /             |
| ...                          |        |  Dragon's     Phoenix (???)  |
|                              |        |         |                    |
|                              |        |      Cataclysm (final)       |
|                    Back      |        |  Used in: Burning Briar      |
+------------------------------+        +------------------------------+
```

- The grid fills the panel, 5 across; a tap opens the detail as a full sheet over it, its tree reading
  **top to bottom** (rows instead of columns), and Back (or the system back) returns to the grid with the
  same cell selected. The search field sits under the title; the soft keyboard pushes nothing else.

### Controller

- The grid is the home: the stick moves the selection, the detail follows it (PC) or opens on A
  (phone layout). **LB / RB cycle the filter tabs.** A on the PC moves into the tree, where the stick
  walks the nodes; B steps back out, then closes.
- **Search on a pad, through our own on-screen keyboard** (Eric: "controller should have search in the
  most user friendly way possible. On screen keyboard?"). **Y** (or A on the search field) opens it: a
  pixel-art keyboard panel in the Codex's stone and gold, letters in an A-Z grid (alphabetical, which a
  pad user finds faster than QWERTY), Space, Backspace and Done. The stick or D-pad walks the keys, **A**
  types, **B** deletes a letter, **X** clears, **Start** or Done closes. The grid filters live under it
  as letters arrive, and a **suggestion row** above the keys lists the best-matching names (spells,
  evolutions, items, recipes), so two or three letters and one press usually finish the search:
  RB moves the selection into the suggestions. Godot has no on-screen keyboard on desktop, so this is
  ours; on a phone the system keyboard is used as for any `LineEdit`, and keyboard players type
  straight into the field.

---

## 3. States, drawn the same everywhere

| state | grid cell | tree node |
|---|---|---|
| **undiscovered** | the sealed glyph, "???", silhouette tint | "???", the sealed glyph, one line saying how it is found (section 4e's rules) |
| **known, not owned / not taken** | dimmed icon, plain frame | resting frame |
| **leveling** (in-run) | level pips under the icon | pips on the base node |
| **ready to evolve** (in-run) | gold frame that breathes on the Ascension button's 2.6 s clock, "Ready" tag | lit frame, gold arrow with its spark |
| **taken / maxed** | solid gold rim and a star | solid fill in the milestone's colour (the loadout strip's gold, cyan, fire) |
| **out of reach this run** | - | a lock, "Out of reach this run" |

The colours and frames are the ones the level-up screen and the results screen already use
(`.ai/level-up-screen.md` 2c, 3a, 4, 4e, 6), so the Codex teaches the same visual language the player
meets in play. Legibility by 4a: prose in the engine sans, headings in bold Cinzel, the measured
contrast floor, the stat glyph before every stat line.

---

## 4. The four filters

- **All**: everything, grouped Spells, then Items, then Recipes, each with its count.
- **Spells**: every attack spell and the passives. Detail: the evolution tree (section 2).
- **Items**: the chest relics (`ChestItemCatalog`) and the boons. Detail: the item's effect line, its
  rarity, and, for a set piece, **its set as a small recipe**: the three pieces (owned ticked) into the
  Full Set Enchantment and its bonus.
- **Recipes**: the combination spells (`ComboCatalog`) and the Full Set Enchantments. Each is drawn as
  **ingredients into result**: `[Hunter's Draw: Hunter's Mark] + [Starfall: Constellation] -> Sagittarius`,
  each ingredient with its state and requirement chips, the result sealed until made
  (`.ai/combo-spells.md` section 2's browser rules). Selecting a result, or searching its name, is the
  **reverse lookup**: the same card, ingredients first.

---

## 5. Search and sort

- **One field searches every name**: spells, mutations, Ascensions, Apotheosis forms, items, sets,
  combination results and their ingredients. A hit on an evolution selects its spell and lights that
  node; a hit on a recipe's result or ingredient opens the recipe card. Undiscovered names are not
  searchable (a search must not name a secret: `.ai/side-events.md`'s rule that a hint never names its
  answer).
- **Sort** inside every filter, no control needed: in-run, held and ready first, then near misses
  (fewest unmet requirements), then the rest; in collection mode, owned first, then by element.

---

## 6. Why no zoom and pan

Amy's request asks for zoom and pan "when evolution trees become large". Ours have a fixed shape: one
base, at most three mutations, two or three Ascensions, at most one final form, and a short Used-in list.
That fits the detail panel on both canvases **by construction**, so the Codex skips zoom and pan, which
would cost the most on a pad and a phone. A validator holds the bound (section 8), so a spell that ever
outgrows it fails a check rather than clipping.

---

## 7. Built from the data, never hardcoded

| what | from |
|---|---|
| spells, levels, element weights, taglines, icons | the `SpellData` resources (`ContentValidator.SpellResourcePaths`) |
| mutations and Ascensions, their requirements | `SpellEvolutionCatalog`, `Player.BuildAscensionPreview` (in-run), `EvaluateElementGate` |
| the Apotheosis | `ApotheosisCatalog`, `Player.BuildApotheosisPreview` |
| combination recipes | `ComboCatalog` (being built, `wip/combo-sagittarius`) |
| items, rarity, sets and set bonuses | `ChestItemCatalog` |
| boons | the boon catalog |
| owned, how to find, seen, made | `UnlockCatalog`, `SaveData` (`SeenAscensionIds`, owned spells, achievements) |

A new spell, item or recipe appears with no Codex edit. **Items have no "seen" record today**; showing
undiscovered items needs one, added the way `HasSeenPrologue` was (additive, defaulting empty, so **no
schema bump**), or items are simply always known (question 3).

---

## 8. The build, in pieces

1. **`CodexModel`**: entries, edges, requirements and states from section 7, for both modes, as plain
   data, with a probe that prints it (`_UiShot --codex-dump`). Nothing drawn yet.
2. **The PC layout**, in-run mode first (it replaces the Ascensions browser and must not lose anything it
   shows today, the Apotheosis section included).
3. **The phone layout** and the sheet.
4. **Search, filters, sort**, and the pad's LB/RB.
5. **Items and sets.**
6. **Recipes** (after `ComboCatalog` merges).
7. **Entry points**: the level-up's button, the main menu slot, the pause screen's Codex button; then
   retire `LevelUpMenu.Ascensions.cs`'s browser and the Spell Index panel.
8. **Checks**: `RegressionChecks.ValidateCodex` (every spell, item and recipe present; every tree fits the
   detail bound; every search name resolves), `_UiShot --open-codex=<id> --codex-filter=<f>` at both
   canvases, the contrast report, and a pad walk.

Pieces 2 and 3 can run in parallel after 1; 5 and 6 after 4.

---

## 9. Questions for Eric

**Answered 2026-11-02: "Approve all, but controller should have search in the most user friendly way
possible. On screen keyboard?"** So 1 Evolution Codex, 2 replace both, 3 relics and boons with an
additive seen record, 4 "???" until first met, 6 no zoom and pan; and 5 changed: a pad searches through
the on-screen keyboard in section 2.

1. **The name.** "Evolution Codex" is Amy's. "Codex" was once retired (the Arcane Codex became Arcane
   Upgrades). Keep **Evolution Codex**, or **Codex**, or keep **Spell Index** for the menu entry?
2. **Replace both** the Ascensions browser and the Spell Index with it (recommended), or keep the Spell
   Index as its own page?
3. **Items**: include relics and boons (recommended), and either always show them or track "seen" with an
   additive save field (no schema bump)?
4. **Undiscovered evolutions** stay "???" until first met, as today (recommended)?
5. **No search on a pad**, filters and LB/RB instead (recommended)?
6. **No zoom and pan**, trees that fit by construction (recommended)?
