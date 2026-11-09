# The second half: the far shore and its gods

Eric, 2026-11-06/07 (`.ai/in-flight.md` item 53): once every spell and character is unlocked and the main
boss beaten, further chapters are *"excuses to keep trying new spell combinations on new maps so it feels
fresh"* - achievements welcome, unlocks not required. The gods arrive here, each starting with a combination
spell as their default. Themes still to design.

And item 43 (2026-11-06): consider **gods as the playable characters earned during the extended story**.
They can start with powerful existing combination spells, Apotheosis forms or other advanced spell forms;
it does not require new spells - reuse the current roster and its evolved forms, then balance each god's
starting power, passive and unlock around that later story.

**Status: design for Eric's review, 2026-11-07; accepted 2026-11-07/08 (section 8). F0, the plumbing, is built
(2026-11-08, section 9, "F0, built"), F1, the god plumbing (2026-11-08, "F1, built"): a god can be added as
data; F2, Artemis (2026-11-08, "F2, built"): the first god, whole; F3, the White Downs (2026-11-08, "F3,
built"): the first far chapter, playable behind the far shore's gate; F5, reel E and the map (2026-11-08,
"F5, built"): the far shore is drawn across the dark sea and announced; and F4, the far sky (2026-11-08, "F4,
built"): tonight's Conjunction, its favour, its lesson and its measurement.**
Section 8 is the list of decisions that are his;
every other choice below is a recommendation he can overturn.

Read first: `.ai/world-and-tone.md` (the premise this extends, the enemy palette, why the player is alone),
`.ai/combo-spells.md` (the Conjunctions, sections 2, 4 and 6), `.ai/dark-wizard-playable.md` section 6 (the
one wizard who opens on a Conjunction), `.ai/endgame-castle.md` 6.4 and 7.3 (the ending, and four chapter
shapes nobody built), `.ai/slow-start.md` (one locked system at a time), `.ai/achievements-and-secrets.md`
2.5 (why no gate stands behind luck), `.ai/cutscenes.md` (the reels, and the Archmage at their centre).

---

## 0. In one paragraph

When the dark wizard falls in the Throne, the light he took leaves him and goes home. Most of it comes down
on the land, to the wizards. Some of it goes on, east over the dark sea, because it was never a wizard's:
it was the gods', whose seats on **the far shore** went dark when he took the light the wizards carried.
His shadow still holds those seats - nothing it was told has been taken back - so the light cannot land.
The far shore is **six new chapters**, each a god's seat with its own ground, shape, cast and warden. Beat
a seat's warden and its god is rekindled and joins the roster, opening on **a Conjunction at level 1** as
the Dark Wizard opens on The Taking. The half's one new system is **the far sky**: every far-shore run, the
sky names one Conjunction the save has not yet made, and the deal favours its two parents - the excuse to
try a new combination, every run, on a new map.

---

## 1. What the design has to sit on

Each line is a rule already in the game, where it lives, and what it decides here.

| rule | where | what it decides for the half |
|---|---|---|
| Everyone unlocked after the start is a historic or legendary person, *"a life the player can look up"* | `world-and-tone.md`, "Why the rescued are historic and legendary people" | the gods are real gods of real legends - and of **this** world's legends (3.1) |
| The horde is one colour: shadow wears what it took, and every creature carries his one eye-light | `world-and-tone.md`, "The enemy palette" | **no second faction on the far shore**. Every cast is still his shadow, in the clothes of the people it took there. No god raises the dead to fight for the player: the dead have no faction colour, so they cannot have a side |
| He is the only figure who sheds dark light | `world-and-tone.md`; `endgame-castle.md` 6.1 | no god emits the `darkness` ramp. A god's spells may use it (Void Lance does); a god's body never |
| *"He is never on screen until he is"* - and he has fallen | tone rule 5; `endgame-castle.md` 6.4 | **no successor villain.** The far shore's antagonist is the order he left behind, impersonal, as the horde always was |
| You are alone: everyone who could help is in a cell | `world-and-tone.md`, "Why you are alone" | after the Throne the wizards are free and have gone home with their light (`cutscenes.md` D, the homecoming). The far shore is nobody's home but a god's, and the gods are out. The player crosses alone |
| Each Apotheosis owns a verb no other form or Conjunction owns | `ApotheosisCatalog`, `RegressionChecks.ValidateApotheosisCatalog` / `ValidateComboCatalog` | every god's form claims a new verb (3.2) |
| One wizard opens on a Conjunction, at level 1 of its own table | `ComboDefinition.StartsFusedFor` (`scripts/ComboCatalog.cs`), `Player.AddStartingConjunction`, `ApotheosisCatalog.OnStartingConjunction`, `ValidateOpeners` (was `ValidateStartingConjunction`; every opener since F1) | the gods follow that exact precedent (3.4) |
| Every chapter has its own cast | `StageBestiary.<Chapter>.cs`, `ValidateBestiary`, `.ai/bestiary.md` | six new casts |
| One locked system on show at a time; a system is learned when its post-run reel plays | `Teachings` (`IsShown`, `Ladder`, `Rite`), `slow-start.md` | the far shore is invisible until the Throne is won; its one system is taught by its first god (6.2) |
| Every character has exactly one source | `UnlockCatalog`, `ValidateUnlockCatalog` | each god is paid by one deed (6.1) |
| A gate behind luck is a bad gate | `achievements-and-secrets.md` 2.5 | Eric's "every spell" needs a guard, since nine spells are in secret vaults (6.1, question 6) |
| The campaign list is measured by deeds | `GlobalStatsManager.ChaptersClearedAt` and `AchievementDefinitions.CampaignChapterCount` both count `StageCatalog.Campaign` | **the far shore cannot be appended to `CampaignOrder`** (section 7) |

---

## 2. The premise: what changes after the castle

The ending already sets it up. When he falls, *"the warm light he took leaves his body as a storm of motes
that fly off the screen in every direction: the magic going home"* (`endgame-castle.md` 6.4). The second
half follows the motes that did not stop.

**Recommended:**

> Every wizard's light was a little of a god's, held in a mortal hand. When he took the wizards, he took
> that light into himself, and on the far shore the gods' seats went dark one by one. He did not have to
> fight them: he starved them. He set his seat on the rock between the land and the far shore, so the
> light could never cross back, and sent his shadow over the water to hold the dark seats.
>
> When he fell, the light went home. The wizards' came down on the land. The gods' went on, east over the
> dark sea, to seats his shadow still holds. It circles over each one and cannot land.

What that decides:

- **Why the castle stands where it does.** `world-map.md` puts it on a rock in the dark sea at the end of
  the road. It was a door, and he sat in it.
- **Why no god answered when he came for the wizards.** They were going out as the wizards fell.
- **Why the far shore is occupied, not ruined.** Tone rule 3. His shadow was told to hold the seats, and
  nobody is left to tell it otherwise. *"The order outlived the one who gave it"* is the whole of the
  half's antagonist.
- **Why the gods open on Conjunctions.** A wizard carries one part of a god's light; a god holds two as
  one. A Conjunction is a mortal joining two parts, which is what a god's magic looks like whole.
- **Why a god starts at level 1.** Its light has only just come home (3.4).
- **What the player does.** Clears a seat. When its warden falls, the light comes down and the god is
  **rekindled** (the half's word for freed: nobody unlocks a cell).

It keeps every line of the prologue true: *"He came here first"* (the moot), *"He took every wizard who
might rival him"*. Nothing in it says he fought a god, which would make him larger than the story has
shown him.

**The lighter alternative** (question 1): the gods had slept since long before him, and the light going
home wakes them. Fewer claims about the past, but it gives up the castle's reason and the Conjunction's.

---

## 3. The gods

### 3.1 How they are chosen

**Every god is named in the legend of a wizard the player has already freed.** The wizards of this world
are people from many legends (`chapter-wizards.md`); the gods behind them are the gods those legends name.
So the far shore is not one pantheon, and each god arrives with someone on the roster who already answers
to them - which is also how each chapter's clue can be spoken in a familiar voice (`side-events.md`, the
wizard clues).

Rejected:

- **One pantheon** (the Olympians, say). It would make the far shore one culture's, in a world built from
  many.
- **A god per element.** The elements are the spells', not the people's.
- **Gods of living faiths** - and so no god for Seimei, Ma Liang, Tituba or John Dee, whose traditions are
  still prayed in (question 3). A god someone worships today is not ours to starve and rekindle.
- **A god who raises the dead.** See section 1, the enemy palette.

### 3.2 The roster

Six gods, each opening on a Conjunction (`ComboCatalog` for the built ones, `combo-spells.md` section 4 for
the designed ones), each with an Apotheosis on a verb nobody owns. Taken: burn, stop, link, hold, flood,
swarm, pull, reflect, banish, temper, echo, feed, distil, foresee, blind, turn, take, rewind, count, double,
sever, displace (`ApotheosisCatalog`); converge, fester, seize, anoint, and the proposals orbit, fling,
advance, refract, funnel, answer, ricochet, fuse, slip (`ComboCatalog`, `combo-spells.md` section 4).

| god | named in the legend of | opens on | its parents, at level 8 down the named Ascension | elements | god's element | Apotheosis (verb) | passive, first guess | health / speed |
|---|---|---|---|---|---|---|---|---|
| **Artemis**, the huntress | **Laelaps** - she gave the hound to Procris (Ovid) | **Sagittarius** (converge) - **built** | Hunter's Draw - Hunter's Mark (`hunters_draw_infinite_horizon`) + Starfall - Constellation (`starfall_constellation`) | Metal, Wind, Light | Light | **The Hunter's Moon (hunt)** | *Far-Shooting*: spells find targets 15% farther | 0.90 / 1.08 |
| **Manannán mac Lir**, who walks the waves | **Morgan le Fay** - her Avalon is the Isle of Apples, and in the older Irish telling the Isle of Apples (Emain Ablach) is his | **Maelstrom** (fling) - designed | Riptide - Tidal Wall (`riptide_titan_cataclysm`) + Cyclone Slash - Vortex Reaper (`cyclone_slash_vortex_reaper`) | Water, Wind | Water | **Wave-Sweeper (ebb)** | *Mist-Cloak*: for a second after a hit, ordinary enemies lose you | 1.00 / 1.00 |
| **Hecate**, of the three ways | **Medea** - her priestess, by whom she swears | **Dark Lens** (refract) - designed | Scorching Ray - Prism (`scorching_ray_titan_cataclysm`) + Void Lance - Witching Hour (`void_lance_annihilation_beam`) | Fire, Arcane, Darkness | Arcane | **Trivia (cross)** | *Keybearer*: +2 bans a run | 0.95 / 1.00 |
| **Ra**, the sun | **Imhotep** - High Priest of Ra at Heliopolis (`CharacterData_Imhotep.tres`); his form is already the Eye of Ra | **Orrery** (orbit) - designed | Solar Flare - Daybreak Avatar (`solar_flare_daybreak`) + Hollow Star - Black Sun (`hollow_star_titan_cataclysm`) | Light, Fire, Darkness | Fire | **The Barque of Millions of Years (rise)** | *Daystar*: your light reaches 30% farther in the dark chapters | 1.05 / 0.98 |
| **Ukko**, the old man of the sky | **Väinämöinen** - who calls on him in the Kalevala | **Hailfront** (advance) - designed | Stormsweep - Thunderclap (`stormsweep_thunderclap`) + Sleet Runes - Sigil Fan (`sleet_runes_sigil_fan`) | Lightning, Ice, Wind | Lightning | **The Driven Storm (drive)** | *Weather-Wise*: pushes move you half as far | 1.15 / 0.93 |
| **Hephaestus**, the smith of the gods | **Wayland** - the smith of the north to his of the south, both lamed, both forging for kings and gods; and **Medea**, whose legend holds his Talos and his bronze bulls | **Anvil Line** (ricochet) - designed | Molten Shard - Forge Swarm (`molten_shard_infinite_horizon`) + Iron Palisade - Iron Ring (`iron_palisade_titan_cataclysm`) | Metal, Fire | Metal | **The Bronze Net (snare)** | *Forge-Born*: a Full Set Enchantment completes with two of its three pieces | 1.25 / 0.88 |

Every named Ascension id above was found in the scripts on this tip. Six gods, six elements (a form adds
+1 of its god's element and nothing else, `gameplay-design.md`, and each element here is one its
Conjunction already carries, so no form makes a fourth). Orrery shares Black Sun with The Taking; the
validator forbids two recipes sharing a pair *and* both Ascensions, not one Ascension. Passives are on axes
no wizard owns (reach, losing pursuit, bans, light in the dark, push, sets) and none adds damage, which was
Dread's rule (`dark-wizard-playable.md` 3); every number is a first guess.

The ties are not equally strong. Laelaps, Medea, Imhotep and Väinämöinen are named outright in their
legends' texts. Morgan's runs through Avalon, and Wayland's is a parallel rather than a naming - Hephaestus
leans on Medea's legend for the textual tie (question 2).

### 3.3 Each god

**Artemis.** The huntress and the moon. Sagittarius is already her spell in all but name: a bow, and the
stars as archers. **The Hunter's Moon (hunt):** *"When your quarry falls, the mark passes to the strongest
in reach and every lit star looses again."* While the bow stays drawn the volley runs quarry to quarry
(each star at most twice a stop). The look: a tall figure in a short chiton and a dark mantle, the bow
across her back, a thin crescent at her brow carrying the light ramp. No hound: Laelaps is his own.

**Manannán mac Lir.** The sea-god of the Otherworld isle, ferryman to it. Maelstrom's whirl that holds
what it catches is his sea taking the drowned. **Wave-Sweeper (ebb):** *"The wall comes back. What it
left standing is drawn back on the ebb into the whirl, to be thrown again."* Elites and bosses are struck,
never moved (`in-flight.md` 3a item 4). The look: a sea-grey cloak whose hem frays into spray - lit spray,
never shadow, so it cannot be read as the dark wizard's hem going into the ground - a spear, a fisher's
beard.

**Hecate.** The torch-bearer at the crossroads, the keybearer, Medea's goddess. Dark Lens - rays split
where they cross a turning lance - is a crossroads drawn in light. **Trivia (cross):** *"Where a ray met
the turning lance, a crossroads stays lit; every ray that passes it splits there too."* The look: veiled,
a torch in each hand (the one figure with two warm lights), keys at her belt; the three faces suggested by
the veil's three folds, never three heads on a 48 cell.

**Ra.** The sun who sails the night and rises. Orrery's sun that draws things into orbit is his, and its
collapse is the night. **The Barque of Millions of Years (rise):** *"When the orbit falls in, the sun rises
again where it fell and gathers an orbit of its own until it sets."* Nothing in it blinds: blind is
Imhotep's, in Ra's own name. The look: falcon-headed under the sun-disc (the disc is his light source),
white kilt and broad collar - Imhotep's costume language, which already wears Ra's colours.

**Ukko.** The Kalevala's god of sky and thunder, whom Väinämöinen calls on. Hailfront's front that walks
out the way he faces is weather driven across a fell. **The Driven Storm (drive):** *"A front you walk
behind does not break. It keeps walking ahead of you, shoving and cutting, until you stop or turn."* The
look: an old man in a cloud-grey mantle, a stone hammer, a beard to the belt, the hammer-head's crackle his
light. (Kalevala also has Louhi hiding the sun and moon, and Ukko striking fire to make new ones - the
story of this game told small. A clue line can use it.)

**Hephaestus.** The lamed smith of the gods. Anvil Line - shards loosed into an iron ring, glancing stake
to stake - is his forge as a weapon. **The Bronze Net (snare):** *"Every shard leaves a strand from stake
to stake. When the ring is strung, the net closes on everything inside."* (The net he caught Ares in; held
2 s, bosses cut and never held.) The look: broad, a bronze brace on one leg and a stick, leather apron,
bare forearms, hammer and tongs, the cold forge's last coal in the tongs his light.

### 3.4 How a god plays

The Dark Wizard's model, applied to six more (`dark-wizard-playable.md` 6, `ComboDefinition.StartsFusedFor`):

- **The god opens on the Conjunction, made, at level 1 of its own level table**, and cards level it to 8.
  It has no milestones: a Conjunction never ascends again.
- **Its Apotheosis** opens at its level 8 and player level 21 (`ApotheosisCatalog.OnStartingConjunction`,
  `MinimumPlayerLevel`): "ascended" is its max level, "not combined" is true of a spell that is never
  fused again.
- **Everyone else fuses it as any recipe**, through its derived Feat ("Conjunction: Maelstrom"), and gets
  it whole at its level 8 (`Player.TryFuse`). So every god the half adds also adds **a recipe to every
  wizard's pool** - four new Conjunctions to try on the road as well as the far shore, which is item 53's
  point. The god's deed pays the god alone; the spell stays its Feat's, as The Taking's does.
- **A god may fuse every other recipe.** Its starting spell is a Conjunction, which no recipe names as a
  parent, so `ComboCatalog.Evaluate` never seals a recipe as its own. A god opens holding one Conjunction
  and has the whole run to build a second: the natural plan for a god, and the half's.
- **It needs its own level table**, as The Taking has (`SpellData_TheTaking.tres`, levels 2-8), because a
  god never held the parents a fused copy freezes (`SpellData.ComboParents`). Sagittarius is built on
  frozen parents today ("a Starfall on the frozen Starfall", `combo-spells.md` 6), so it needs a second path
  that reads a table. The five unbuilt ones should be built table-first, with a fused copy simply their
  level 8 - The Taking's departure from "read once from both parents", taken as the rule.
- **Target power:** the Dark Wizard's measured band - his Conjunction about 29% of his damage at level 2 by
  minute 8, "powerful, inside the starters' band" (`dark-wizard-playable.md` 6, Measured).

### 3.5 Alternates

| god | named in the legend of | opens on | why not first |
|---|---|---|---|
| **Apollo** | **the Cumaean Sibyl** (his prophetess; Ovid and Virgil) | **Shadow Bolt** (fester) - built | the cheapest god: the spell exists. Homer's Apollo *"came like the night"* with the plague-arrows, which suits a bolt of void. But it needs an Apotheosis verb the doc could not make crisp, and it would be a third Greek god of six |
| **Morana** | **Baba Yaga** (Slavic winter and death, drowned in the river each spring) | **Black Ice** (slip) - designed | the tie is folkloric, not textual, and Black Ice waits on enemy pathing (`combo-spells.md` 4) |
| **Nemesis** | none on the roster | **Reprisal** (answer) - designed | breaks the 3.1 rule |

Swapping Apollo in for Hephaestus saves building one Conjunction (question 2).

---

## 4. The far shore: six chapters

### 4.1 Where

Across the dark sea, east of his rock: the road goes on past the Throne, down to his sea-gate, over the
water, and lands under **white chalk cliffs facing the Emberdeep's black ones**. Forward is still east, as
the road and Eric's left/right controls agree (`world-map.md` 1). The far shore runs west to east from the
cliffs.

### 4.2 The six

Ids are appended, never renumbered (`campaign.md` 3); `stage_14` onward is free on this tip.

| # | id | chapter | god | ground, and its shape | warden, and what it asks | his shadow wears | corruption line |
|---|---|---|---|---|---|---|---|
| 1 | `stage_14` | **The White Downs** | Artemis | chalk downland at night under no moon: white turf, flint, barrows, a hound and a hart cut in the hillsides. Open, the barrows solid | **the Unstrung Hunter**, the hunter the sky lost: it draws on you along a line that follows you - **put a barrow between you** (cover; no boss on the road asks for it) | shepherds and warreners (linen); the hunt's beaters and its hounds; the cliff-watch (steel) | *No moon has risen here since he came.* |
| 2 | `stage_15` | **The Drowned Shore** | Manannán | the seabed the tide left: ribbed wet sand, weed, wrecks, whale ribs, standing pools; the sea stands off at the horizon. The maze lattice drawn as tide channels between sandbars - `endgame-castle.md` 7.3's "islands and causeways", built for no chapter | **the Weir**, which holds the sea out: it opens sluices that send the sea down the channels in telegraphed lanes - **keep to the high ground** | net-menders, the oar-crews in their rows (linen); the shore-watch with boat-hooks (steel) | *The sea was told to stay out.* |
| 3 | `stage_16` | **The Three Ways** | Hecate | a dry night country of roads that fork in three, yew hedge between them, a three-faced pillar and a cold torch-stand at every fork. The lattice drawn as hedge: corridors are roads, junctions are crossroads - 7.3's "paths through dense wood", wrong for chapter one, right here | **the Three-Faced**, three cones turning at once - **stand where no face looks** | pilgrims with offerings, torch-bearers, key-keepers (linen); the black dogs of the roads; the shrine-guard (steel) | *Every torch at every crossing is out.* |
| 4 | `stage_17` | **The Twelfth Hour** | Ra | an avenue of sphinxes, obelisks and pylon gates on black sand by a still river, **in the dark** (`StageEnvironmentCatalog`'s `Darkness`, the Cave's and Dungeon's light pool) - and **the dawn comes with the pace clock**: the dark lifts across the run, the boss fought in first light | **the Eclipse**, untargetable until lit (`Enemy.SetTargetable`): motes of the sun rise off the pylons and follow you as The Taking's light does (`TakenLight`) - **bring it the light, then strike**. An unlit disc rimmed by the sun's own light ramp: no dark light | priests with sistra; the barque's haulers on their rope, one moving line (linen); the knife-bearers of the gates of the hours (steel) | *The sun did not rise.* |
| 5 | `stage_18` | **The Thunder Fell** | Ukko | bare granite above the treeline: lichen, cairns, lightning-split pines, rune-poles. **Storm fronts cross it on the pace clock** - 7.3's Sands "storm fronts", built nowhere - dimming sight and pushing everything with them for 30 s; lightning finds whatever stands tallest | **the Still Eye**, the storm's drifting eye; outside it the storm cuts - **stay in the eye as it moves** (the Still Warden holds you still; this keeps you walking) | woodsmen and rune-singers (linen); the hail-men (steel); the fell's wolves and its bear | *The storm was told never to break.* |
| 6 | `stage_19` | **The Bronze Isle** | Hephaestus | a god's workshop island gone cold: bronze yards in verdigris, anvils as tall as houses, dead bellows, rows of his wheeled tripods; walled yards with doors (the lattice drawn as bronze walls). **No lava**: the Emberdeep is the dark wizard's forge, this is a god's forge put out | **Talos**, the bronze giant who walks the island's rim throwing stones; his one vein is closed by a nail at his heel - **get to the heel** (a weak point behind him: `in-flight.md` item 28 asks for weak spots) | the god's apprentices (linen); the yard-guards (steel); the golden handmaidens and the bronze bulls - made things, taken | *The god's forge is cold.* |

The road and the castle already have open fields, caverns, broken walls, a maze, a travel band, a fixed
building and a closed hall; the far shore adds two shapes they lack (tide channels, hedged roads), two
moments they lack (the dawn, the fronts), cover as a demand, and a boss weak point.

**The made things on the Bronze Isle need an art ruling** (question in `art-direction.md`'s court, not
Eric's list): `world-and-tone.md` gives fire eyes to what *he* built and his cold arcane mark to what was
taken. The god's automata were built by a god and taken by the shadow. The recommendation is his mark -
taken is what they are now - which keeps the carve-out his alone.

### 4.3 What every far chapter holds

- **Its god**, paid by its boss's Road deed and rekindled at the run's end (6.3). That is the one unlock a
  chapter carries: Eric's "unlocks not required" is read as *no spells, boons or wizards to find* - every
  spell is earned by the time the far shore opens - and the gods are what item 43 says the extended story
  earns.
- **One side event**, held to `unlock-puzzles.md`'s rule (the state drawn in the world, one action, a wrong
  move costs time, solved under the horde), paying Arcane Energy and a keepsake - an achievement, not an
  unlock.
- **Two or three secrets**, paying what `stage-secrets.md` allows (never a spell, wizard or boon; found
  levels inside its budget).
- **Its own music, foliage (`Foliage.<Chapter>.cs`), cast of about five (`StageBestiary.<Chapter>.cs`) and a
  boss** (`BossEnemy`'s five hooks and a themed entrance, `ValidateBossPatterns`).
- **A 15-minute run on the pace clock**, as every chapter (`run-pacing.md`; `TimerVictorySeconds` 900).
- **A difficulty row** in `ChapterDifficultyCatalog`, starting from the castle's (3.80 / 1.35 / 0.75 / 1.65,
  which is the Emberdeep's) and, recommended, held there across the half: it is a place to try builds, and
  Hardcore is the climb for those who want one (question 11).
- **A place on the world map** (`tools/art/world_map.py`'s `PLACES`; `ValidateWorldMap` reports one
  without).

Half-wide Feats, Arcane Energy only: *Under the Far Sky* (make tonight's Conjunction, section 5),
*Every Seat Lit* (all six gods rekindled), *Every Conjunction Known* (every recipe made once, a Collection
like the Grimoire, measured live against `ComboCatalog.Fusions`), *All the Gods' Forms* (each god's
Apotheosis taken).

---

## 5. The far sky: tonight's Conjunction

The half's one new system, and its answer to *"excuses to keep trying new spell combinations"*.

**Why it is needed.** A recipe is a plan made at two level 8 screens, and it does not land easily:
Sagittarius did not land inside a 15-minute run for the planned bot in four tries (`combo-spells.md` 6,
"Measured") - held back by the cards, not the gates. A far shore that only *allows* combinations would
mostly not see them.

**What it is.**

- **Every far-shore run, the sky names one recipe** - the far shore's own sky, never the road's. Chosen
  from `ComboCatalog.Fusions` whose two parents the save owns; never one the wizard opens on
  (`StartsFused`), never one that names the wizard's own starting spell (`ComboSeal.OwnSpell`); **weighted
  toward recipes the save has never made**. Rerolled after every far-shore run, won or lost.
- **It is named before the run**, on the world map's card for every far-shore stop ("Tonight: Maelstrom -
  Riptide (Tidal Wall) and Cyclone Slash (Vortex Reaper)"), and on the pause screen.
- **The deal favours it; the recipe is unchanged.** When a slot is free its parents are offered before
  other new spells; while held, their upgrade cards are always in the hand (as if `Player.UpgradeOfferChance`
  were 1 for those two); at level 8 the named Ascension is shown first, under the two linked rings it
  already carries (`stat-conjunction`). Both parents at 8 down both named Ascensions, as every recipe asks
  (`combo-spells.md` 0) - Eric's rule is not loosened.
- **Making it pays**: a bonus of Arcane Energy at the run's end, and the *Under the Far Sky* Feat.

**Measured before it ships:** `_BalanceHarness --stage=14 --seek-combo=<tonight's> --all-unlocked --immortal`
with the favour on, against the same run with it off. The target is the planned bot meeting the recipe
**by about minute 10**, leaving five minutes and the boss to use it. If it cannot, the fallback is a short
opening Muster of a few levels on far-shore runs - the Throne's machinery (`endgame-castle.md` 6.2,
`Node2DGame`'s `queuedLevelUps`) - sized by the same measurement (question 10).

---

## 6. How the half unlocks and is taught

### 6.1 The gate

Eric's words are *"once every spell and character is unlocked and the main boss beaten"*. Recommended,
as a new gate kind for the far shore's first chapter:

- **Taken Back** - the Throne won (`throne_taken_back`, which also frees Nimue).
- **The Grimoire Restored** - every spell recovered (`grimoire_restored`; it already leaves the Conjunctions
  out, `UnlockCatalog.GrimoireSpellIds`).
- **Every wizard a place holds** - which the Throne's gate already asks (`StageGate.PreviousBossAndWizards`),
  plus Merlin and Laelaps. Those two keep `outsideCampaignGate` so that their sources could move without
  closing a road a save already had open (`UnlockCatalog`'s comments on both); no save has the far shore
  open, so counting them here closes nothing.
- **Not the Dark Wizard** (every chapter's boss on Hardcore, `the_taker_taken`) **and not John Dee** (150
  chests over all runs, `mortlake_library`): a difficulty and a grind, neither a place (question 5).

Every later far chapter opens on the one before it (`PreviousBoss`, in the far shore's own order).

**The Grimoire is nine secrets deep**, and `achievements-and-secrets.md` 2.5 narrowed the Throne's gate to
wizards for exactly that reason. Recommended guard (question 6): once Taken Back is won - and only then -
the light going home points at what is still hidden. A chapter whose vault is unfound says so on its map
card, and the in-run wayfinder marks the vault's direction. Before the Throne the vaults stay as subtle as
`in-flight.md` items 38 and 49 make them.

**Conjunctions are always learned by then.** The Grimoire includes the three teaching vaults (the Pinfold,
the Fallen Star, the Geode Heart, `Teachings.Ladder`), so a save that meets the gate has played the
Conjunctions lesson. If item 37 moves the ladder to character interactions, the gate must ask
`Teachings` for Conjunctions directly instead of relying on that.

### 6.2 One locked thing at a time

| the save | on show | taught |
|---|---|---|
| before Taken Back | **nothing of the far shore**: no map stop, no god card, no achievement row. The Throne is the locked thing on show | - |
| Taken Back won | after the ending's row, **reel E** (6.3). On the world map the tower's dark light is out and warm motes stream east off it over the dark sea; the far shore is drawn past the water, fogged, its first stop carrying the gate's line ("Recover every spell") | the story only |
| the gate met | **the White Downs open.** Artemis's card appears on the character select as a silhouette with her held line, as a chapter's wizard does once his chapter opens (`secretUntilStage`). The other five gods are not shown | - |
| Artemis rekindled | **her rekindling reel, then the far sky's lesson** - story before systems, as `PlayOwedFreeingThen` already orders them. From the next run every far-shore card names tonight's Conjunction | **the far sky** |
| each next chapter opened | its god's silhouette, then the god rekindled by its warden | - |
| Hephaestus rekindled | the closing reel (6.3) | - |

The far sky is taught by a god, not a vault: Eric's latest word on teaching is that characters teach
systems (`in-flight.md` item 37), and the huntress who relights the night sky is the one to teach reading
it. On the White Downs the sky is empty until she is rekindled - the moon has not risen - so the chapter's
first runs have no far sky in the fiction as well as the code. Its lesson is taught by a deed, as the
Sovereign Rite's is (`Teachings.Rite`, an empty `TeacherSecretId`): a new `Teaching.FarSky`, a fifth
lesson, owed when Artemis's deed completes.

### 6.3 The reels

Every one a reel of the prologue's player (`Prologue.cs`), under `cutscenes.md`'s cinematic rules, the
Archmage at its centre, one line or none.

- **E. The light goes on** (new). After the ending's row, the first time the Throne is won - and for a save
  that won it before this ships, the first time the world map opens: a flag that defaults to unseen and
  plays only for a save with Taken Back, so no migration. The dark sea from his sea-gate at night, the
  Archmage on the steps; the ending's motes going east over the water toward a line of white cliffs, past
  the edge of the picture. *"Some of it was older than any of them."* It follows D, the homecoming, which
  is planned and unbuilt (`cutscenes.md` D); E does not need D to exist.
- **Rekindled** (the Freed template, `tools/art/freed.py`). One panel per god: a far-shore ground pair - the
  white cliffs, and across the water his rock with the tower unlit - and the god's mid layer, the Archmage
  beside them, and the light coming *down* out of the sky into the god rather than back to the Archmage.
  The line is the god's rekindled line (`QuestLog.Prisoners`, below). Owed through `Freeings.RecordFreed`
  as any freed wizard, replayable from Options > Story; `Freeings.WizardIds` and `ValidateFreeings` take the
  gods.
- **The far sky** (a lesson reel, `tools/art/lessons.py`): two stars meeting over the downs. *"Each night
  the far sky names a Conjunction."*
- **Every seat lit** (after Hephaestus): the far shore from the sea, six lights on it. No line.

Quest log rows (`QuestLog.Prisoners`; a locked god is never named, as a locked wizard is not) - epithet,
held line, rekindled line:

| god | epithet | held | rekindled |
|---|---|---|---|
| Artemis | the huntress | *No moon rises over the White Downs.* | *{0} hunts the White Downs again.* |
| Manannán | the one who walks the waves | *The sea stands off the Drowned Shore and will not come in.* | *{0} has let the sea back in.* |
| Hecate | the one at the three ways | *Every torch at the Three Ways is cold.* | *{0} holds her torches up again.* |
| Ra | the sun that did not rise | *It has been the twelfth hour here since he came.* | *{0} has risen.* |
| Ukko | the old man of the sky | *The storm on the Fell will not break.* | *{0} has let the storm break.* |
| Hephaestus | the smith of the gods | *The forge on the Bronze Isle is cold.* | *{0} has lit his forge.* |

### 6.4 The save

**No schema step is expected.** The deeds are derived from the catalogs, the opened list takes new ids,
`LessonsLearned` and `FreeingsOwed` take new entries, and reel E's flag defaults to the quiet state - every
one `HasSeenPrologue`'s additive pattern. Nothing a save holds is narrowed. The tip is at schema 24 and 25
is claimed by the boon rework (the shared notes); a piece that finds it needs a step takes the next free
number and says so there. **The story epoch is not raised.**

---

## 7. Where it plugs in

Cross-checked on this tip (`main-ff86ni`, merged 2026-11-07).

| need | today | the change | size |
|---|---|---|---|
| **an order of its own** | `StageCatalog.CampaignOrder`. `GlobalStatsManager.ChaptersClearedAt` counts every chapter in `Campaign` for the Dark Wizard's Hardcore deed and The Road Restored; `HasFinishedCampaign` reads `LastPlayableChapter`, which opens the Sketchbook | **a second list (`FarShoreOrder`), never in `Campaign`.** Appending to `CampaignOrder` would quietly add six Hardcore clears to the Dark Wizard's deed and keep the Sketchbook closed to a new save until the far shore was done. `PreviousChapter` and `ChapterOpenedBy` read the order a chapter is in; `QuestLog`, `StageRewards` and `WorldMap` iterate both. A new `ValidateFarShore` holds that no far chapter is in `Campaign` | M |
| **the gate** | `StageGate` is `Open`, `PreviousBoss`, `PreviousBossAndWizards`; the Sketchbook's `AfterCampaign` is the precedent for "after the road" | a `StageGate.FarShore` kind (6.1) read by `GlobalStatsManager.IsStageEarned`; `ValidateGateReachability` proves a save can reach it on paper | S |
| **a god opens on a Conjunction** | `ComboDefinition.StartsFusedFor`, one character per recipe; `Player.AddStartingConjunction` / `LevelStartingConjunction`; `ApotheosisCatalog.OnStartingConjunction`; `ValidateStartingConjunction` (one recipe) | already general in shape; each god's recipe sets the field, each needs its own level table, and the validator walks every opener, not only The Taking | S a god, M for Sagittarius's table path |
| **a god's unlock and reveal** | `UnlockCatalog.FromAchievementCharacter(id, deed, hint, secretUntilStage, outsideCampaignGate, sealedUntilFreed)` | one entry a god, its deed the chapter's boss, `secretUntilStage` its chapter | S |
| **the character** | `CharacterData` (`StartingSpellResource`, `StartingPassiveId/Name/Description`, `HealthModifier`, `SpeedModifier`); `CharacterData_DarkWizard.tres` appended at roster index 10 | `CharacterData_<God>.tres` appended; the passives are new `StartingPassiveId`s read where their axis lives (reach, aggro, bans, light radius, push, sets) | S each, the passive S-M |
| **a chapter's own rules** | `ChapterRules` hooks (`RunStarted`, `TryPickSpawnPoint`, `IsPlacementBlocked`, `BossSpawnPoint`, `PinnedPaceSeconds`, `CreateObjective`, `WayfinderSite`), `ChapterRulesCatalog` (the castle's four) | one each where the shape needs it: the Weir's sluices, the fronts, the dawn | per chapter |
| **the dawn** | `StageEnvironmentCatalog`'s `Darkness`, a constant (1 on the Dungeon and the Cave) | the Twelfth Hour's falls with `PaceSeconds` | S-M |
| **difficulty** | `ChapterDifficultyCatalog`; `ValidateChapterDifficulty` (a row per playable chapter, damage never easing) | a row each; the validator's "never easing" reads the far order after the campaign's | S |
| **the far sky** | `Teachings` (`Ladder`, `Rite`, `IsShown`); `Player.GetLevelUpOptions` and `UpgradeOfferChance`; `ComboCatalog.Evaluate` and `IsKnown`; the linked-rings mark | `Teaching.FarSky`; the recipe chosen per far run; the favour in the deal; card and pause lines; the Feat; a `--far-sky` switch on `_BalanceHarness` | M |
| **the map** | `world_map.py` (2112 x 840 art px since the castle), `worldmap.json`, stops in the order `[campaign to the gate] + [Boss Rush] + [rest of the campaign] + [outside]` (`endgame-castle.md` 8) | widen `W` east past the rock; `+ [far shore]` before `[outside]`; the motes overlay after Taken Back; `ValidateWorldMap`. The map is one PNG: check its width against the phone's texture limit as it grows | L |
| **Boss Rush** | `BossRushCatalog.RoadOrder`, its own list, never derived from the campaign | unchanged (question 14 asks if a far-shore rush is wanted) | - |

---

## 8. Open decisions for Eric

**Decided 2026-11-07 (Eric: "Accept them" - every recommendation below taken as written).** Where a
question carried no "(recommended)": **2** keeps the six as proposed (Artemis, Manannán mac Lir, Hecate, Ra,
Ukko, Hephaestus); **15** names the half and its stretch of the map **The Far Shore**. **10 stays open** -
whether far-shore runs open with a Muster depends on how late tonight's recipe lands, which is measured in
F4 (`_BalanceHarness --far-sky`, section 5) before anyone decides it. So the half is greenlit to build, F0
first and alone (section 9). **Measured 2026-11-08** (section 9, "F4, built"): on the White Downs, with the favour,
the planned bot made Shadow Bolt at 10.7 and The Taking at 12.4 and never made Sagittarius; across ten favoured runs
three met it by about minute 10, all Shadow Bolt, and without the favour none made it at all. What holds the rest is the Ascension's own gates
(player level 17, four of the named path's element), not the cards, and a Muster that bought the level gate back would
be 16-20 levels - the Throne's whole Muster, not a few - and would not touch the element gates. Still Eric's to
decide.

1. **The premise.** The gods lived on the light the wizards carried and went dark when he took it, and his
   castle stands between the land and the far shore so it could never cross back (recommended) - or the
   gods had slept since long before him, and the light going home wakes them?
2. **The six.** Artemis, Manannán mac Lir, Hecate, Ra, Ukko and Hephaestus, each named in the legend of a
   wizard you have freed. Keep them, or swap any for an alternate - Apollo on Shadow Bolt (already built,
   the Sibyl's god), Morana on Black Ice (Baba Yaga's), Nemesis on Reprisal?
3. **Living faiths.** Keep the gods to traditions no longer widely practised (recommended)? It means no god
   for Seimei, Ma Liang, Tituba or John Dee.
4. **New spells.** Five of the six open on Conjunctions designed in `combo-spells.md` section 4 but not
   built, and item 43 said the gods should not need new spells. Build them (recommended - each also joins
   every wizard's recipe pool), or hold the half to the Conjunctions already built?
5. **The gate.** Taken Back, The Grimoire Restored and every wizard a place holds, but not the Dark Wizard
   (every chapter on Hardcore) or John Dee (150 chests) (recommended) - or literally every character?
6. **The last hidden spells.** Once the Throne is won, should a chapter with an unfound spell vault say so
   on its map card, so the Grimoire never strands a player short of the far shore (recommended), or stay as
   subtle as today?
7. **A god's start.** At level 1 of its Conjunction, levelling to 8 as the Dark Wizard does (recommended),
   or the Conjunction whole from the first second?
8. **How a god is rekindled.** By its chapter's warden (recommended), by a side-event puzzle as the road's
   wizards are freed, or by making a Conjunction in its seat?
9. **Tonight's Conjunction.** A new recipe each far-shore run (recommended) or one a day - and should the
   deal favour its parents (recommended), or only name it?
10. **If tonight's recipe still lands too late** - after minute 12 for the planned bot - should far-shore
    runs open with a short Muster of a few levels, or is late acceptable?
11. **Difficulty.** Hold the castle's difficulty flat across the far shore, with Hardcore as the climb
    (recommended), or keep climbing chapter by chapter?
12. **The map.** Widen the one world east across the dark sea (recommended - one contiguous drawing, as you
    first asked for), or give the far shore a sheet of its own like the Sketchbook's?
13. **An ending.** A closing reel when the sixth god is rekindled (recommended), or a seventh chapter
    where all six lights meet in a Throne-style fight?
14. **Boss Rush.** Leave it the road's (recommended), or add a far-shore rush once all six wardens fall?
15. **The name.** "The Far Shore" for the half and its stretch of the map?

---

## 9. Build order

The castle's pattern (`endgame-castle.md` 9): **plumbing first, alone; then one piece per worktree**, each
creating its own files and editing only blocks the plumbing marked as its own. Sizes: **S** a day's work or
less, **M** a few days, **L** a week or more.

| # | piece | size | needs | done when |
|---|---|---|---|---|
| F0 | **Far-shore plumbing**: `FarShoreOrder`, `StageGate.FarShore`, six stand-in chapters `IsPlayable = false`, `PreviousChapter` per order, `QuestLog` / `StageRewards` / `ChapterNotes` / `WorldMap` reading both orders, `ValidateFarShore` | L | - | the far shore exists, nobody can enter it, and the Dark Wizard's and The Road Restored's targets have not moved. **Built 2026-11-08** ("F0, built", below) |
| F1 | **God plumbing**: any recipe as an opener with its own table; `OnStartingConjunction` and `ValidateStartingConjunction` over every opener; the god's card lines; the rekindled reel's far-shore ground in `freed.py` | M | F0 | a god can be added as data. **Built 2026-11-08** ("F1, built", below) |
| F2 | **Artemis**: Sagittarius's table path, The Hunter's Moon, Far-Shooting, her sheet (`tools/art/player_*.py`, 48 cell, idle / move / hurt / death), icons, reel layer, catalog rows | M | F1 | the first god, playable everywhere. **Built 2026-11-08** ("F2, built", below); reachable once F3 makes her seat playable |
| F3 | **The White Downs**: chalk tiles and barrows, its cast, the Unstrung Hunter, music, foliage, one side event, two secrets, its difficulty row | L | F0 | the first far chapter. **Built 2026-11-08** ("F3, built", below), **playable** the same day once F2 and F5 merged (the far shore's gate still keeps it shut) |
| F4 | **The far sky**: `Teaching.FarSky` and its reel, tonight's recipe, the favour, card and pause lines, the Feat, `_BalanceHarness --far-sky` and the section 5 measurement | M | F0, F2 | the half's system, measured. **Built 2026-11-08** ("F4, built", below) |
| F5 | **Reel E and the map**: `world_map.py` widened east, the crossing, the white cliffs and six places, the motes after Taken Back, the stop order; reel E | L | F0 | the half on the map, and announced. **Built 2026-11-08** ("F5, built", below) |
| F6 | **Manannán, Maelstrom and the Drowned Shore** | Maelstrom M-L, god M, chapter L | F1; the lattice-as-water look | the second slice |
| F7 | **Hecate, Dark Lens and the Three Ways** | Dark Lens M, god M, chapter L | F1 | |
| F8 | **Ra, Orrery and the Twelfth Hour** | Orrery M, god M, chapter L (the dawn S-M) | F1 | |
| F9 | **Ukko, Hailfront and the Thunder Fell** | Hailfront M, god M, chapter L (the fronts M) | F1 | |
| F10 | **Hephaestus, Anvil Line and the Bronze Isle**, and the closing reel (S) | Anvil Line M, god M, chapter L | F1 | every seat lit |

**The first slice is F0 to F5**: a far shore with one chapter, one god and the far sky, shippable on its
own, built on the one Conjunction that already exists. The five Conjunctions in F6 to F10 can be built
ahead of their chapters - they are ordinary recipes for every wizard first and a god's opener second - which
also feeds the far sky more recipes to name.

**Each piece verifies** as the castle's did: `dotnet build`; the validator run with `-- --no-save-writes`;
for a chapter `_BossProbe --stage=<id>` and `_BalanceHarness --stage=<id>` on a farmed save; for a
Conjunction `_BalanceHarness --seek-combo=<id> --all-unlocked --immortal` against the 1.2x-parents target
and `_UiShot --demo-combo=<id>:fuse`; for a god `--seek-apotheosis` (the form after the 7.5-minute floor);
`_UiShot` of every new screen at 1280x720 and at 540x1212 `--portrait --safe-area`.

**What no harness checks:** whether a god reads as a god and not one more wizard at 48 pixels; whether the
far sky feels like an invitation or a chore; whether each far chapter feels fresh, which is Eric's own word
for the point of the half; whether the favour makes the planned run too easy to still be a plan; and
whether a rekindling reads as the light coming *home* rather than as a second freeing.

### F0, built (2026-11-08, branch `worktree-agent-a299769b7da06a5b5`, on `main-ff86ni` at v0.20.0)

The far shore exists, nobody can enter it, and the Dark Wizard's and The Road Restored's targets have not
moved (both still 13, the campaign's count). All six chapters are `IsPlayable = false`, so no save, screen or
run reaches them; every chapter of the road plays exactly as it did. **No save schema step** (still 25), and
the story epoch is not raised.

**Where it lives.**

| piece of F0 | where |
|---|---|
| the six chapters | `StageCatalog`: `stage_14` the White Downs to `stage_19` the Bronze Isle, appended, with 4.2's names, ground lines and corruption lines. Each borrows a road chapter's `EnvironmentKind` as its stand-in ground (Heath, Swamp, Forest, Desert, Ice, Castle) |
| the second order | `StageCatalog.FarShoreOrder` (a line a chapter), read through `FarShore`, `FarShoreOrderIds`, `FarShorePositionOf`, `IsOnFarShore`, `IsInEitherOrder` and `OrderedPositionOf` (the campaign's chapters, then the far shore's). `InDisplayOrder` is campaign, far shore, outside. `ChapterPlace` says "Far Shore N" |
| `PreviousChapter` / `ChapterOpenedBy` | read the order a chapter stands in, skipping an unbuilt one as before. The far shore's first chapter has no previous chapter; the Throne's fall opens nothing |
| the gate | `StageGate.FarShore` (first far chapter only; the other five are `PreviousBoss` in the far order). `GlobalStatsManager`: `IsStageEarned` (rules 1 and 2 as a gate chapter keeps them, then `IsFarShoreGateMet`), `HasPassedInOrder` (was `HasPassedInCampaign`, now never across the two orders), the far shore chained like the castle in `HasClearedPreviousChapter`, `HasTakenBackTheThrone`, `HasRestoredGrimoire`, `FarShoreWizardIds` / `CountsTowardFarShore`, `FarShoreGateMissing`, `RecordMetFarShore` (called from `AchievementManager` after `RecordMetGates`, playable chapters only) |
| the reveal (6.2) | `IsChapterRevealed`: nothing of the far shore before Taken Back; then the first stop; every later one once earned |
| the lock line | `StageCatalog.FarShoreLockedHint` and `FarShoreLockedHintFor(save)`, which names only what is missing ("Sealed — recover 3 more spells"), wizards by epithet |
| the readers | `WorldMap` (`RoadOrder` is `[campaign to the gate] + [rush] + [the castle] + [far shore] + [outside]`; reveal, compass caption and break, "Far Shore N of M" on the card), `QuestLog` (chain rank, frontier and `IsOffered` across both orders), `StageRewards` (built in that order), `ChapterNotes` (journal in that order), `AchievementDefinitions` (far deeds sort after the campaign's) |
| notes, music, difficulty | `ChapterNotes`: a two-line note for each far chapter, a first draft from 4.2 that its piece may rewrite. `MusicCatalog`: six planned sets, `downs`, `drowned`, `ways`, `twelfth`, `fell`, `bronze`, each on a road set until rendered (Heath, Cave, Ruins, Sands, Frozen, Emberdeep), because `ValidateMusic` wants a row for every chapter. `ChapterDifficultyCatalog`: the castle's row for all six (question 11, held flat), and `ValidateChapterDifficulty` walks the far order after the campaign's |
| null lines | `SideEventDirector`: `"stage_14" => null` to `"stage_19" => null`. `ChapterRulesCatalog`: the same six, with `stage_15`, `stage_17` and `stage_18` added to `Designed` (section 7's tide and sluices, dawn, fronts) |
| the validators | `RegressionChecks.FarShore.cs` (new): `ValidateFarShore` in `RunAll`, and `ValidateFarShoreGateReachability`, called at the end of `ValidateGateReachability`. `ValidateCampaign` lets a far chapter be gated other than `Open` outside the campaign, and refuses `FarShore` inside it. `ValidateWorldMap`'s off-picture check covers both orders |

**The gate as built.** "Every wizard a place holds" is `CountsTowardFarShore`: a character whose source deed
is done in a campaign chapter by its warden, its side event or a secret there. That is every chapter wizard,
Tituba, Väinämöinen, Merlin, Laelaps, and Nimue (whom Taken Back frees anyway). It is not the Dark Wizard (his
deed is a difficulty, done nowhere), John Dee (a Feat), a starter, or anyone freed on the far shore. **One thing
the doc did not know:** in the code the Throne's gate already counts John Dee (every gate counts a Feat's
wizard), so a save that has won the Throne has him anyway. The far shore's own rule leaves him out as 6.1 says,
and the difference never shows. Taken Back reads the deed or his fall in the lifetime record, and the Grimoire
reads the deed or every spell owned, so a save is not held back by a deed not yet evaluated.

**Decisions F0 made.**

1. **No world map places.** The map is F5's. `ValidateWorldMap` already lets an unplayable chapter stand off
   the map (`.ai/endgame-castle.md` 11.4, decision 1) and `WorldMap.BuildStops` leaves it out quietly, so
   `tools/art/world_map.py` was not touched. A missing place is on each chapter's far-shore to-do line, and a
   chapter that flips without one is a fault.
2. **No new `StageEnvironmentKind`.** Every value of that enum is held to a profile and by five ground
   validators, so a kind with no ground behind it would be a fault. Each chapter borrows a road kind until its
   piece adds its own, in a slot marked for it.
3. **No bosses, casts or deeds.** A boss needs a deed that pays it, and a far warden's deed pays its god, which
   is F1 and after. A stand-in boss with an Arcane deed would have been a deed to rewrite later. Their slots
   are marked.
4. **The far-shore to-do is printed, not warned.** One `RegressionChecks: far shore to-do` line a chapter (warden,
   god's deed, cast, borrowed ground, music, side event, rules where designed, secrets, map place). It becomes a
   **warning** the moment a chapter is playable, so no far chapter can flip with a stand-in left. Six chapters
   are a long build, and six standing warnings would hide real ones, so the validator stays at its baseline
   (0 errors, 1 warning: `test_wizard`).
5. **Notes and difficulty rows now.** The validator asks for them only of playable chapters, but writing them now
   means no flip can lose them, and `ValidateFarShore` holds every far chapter to both.

**Who owns what (the marked blocks).** Every slot is a comment line naming its piece, with a "leave this one as
it is" line between neighbours, so two pieces' edits never touch adjacent lines.

| piece | edits only its own block in |
|---|---|
| F1 | god plumbing is its own to mark: `ComboCatalog` openers, `ApotheosisCatalog` forms, `UnlockCatalog` god entries, `Freeings.WizardIds`, `CharacterRoster`. **Gods are appended to `CharacterRoster` in the order they merge, never into pre-marked slots**: a roster index is what a save and `--character=N` keep |
| F2 (Artemis), and the god in F6-F10 | `QuestLog.Prisoners`, its god's line (epithet, held, rekindled from 6.3) |
| F3, F6-F10 (each chapter) | `StageCatalog`, its own entry (its `EnvironmentKind`, then `IsPlayable = true` last); `StageEnvironmentKind` and the profile slot in `StageEnvironmentCatalog`; `LevelGenerator` palette; `GroundDetail`; `Node2DGame.BuildDecorProps` (only if needed); `BossCatalog` `[14]`-`[19]`; `AchievementDefinitions`, its warden's deed and its side event's words; `StageBestiary.All`; `SecretCatalog.Defined`; `Foliage.All`; `StageHintCatalog`; `ChapterNotes`, its note and its site clue; `SideEventDirector` and `ChapterRulesCatalog`, its null line; `MusicCatalog`, its set and row; `ChapterDifficultyCatalog`, its row |
| F4 | `AchievementDefinitions`: Under the Far Sky and Every Conjunction Known, in the slots marked F4 (`Teachings` and the far sky are its own files) |
| F5 | `tools/art/world_map.py` alone. `RoadOrder` already places the far shore, and the card and compass already read it |
| F10 | `AchievementDefinitions`: Every Seat Lit and All the Gods' Forms. Section 9 named no piece for these two, so they are given to the piece that can first measure them |

**Verified.**

- `dotnet build`: 0 errors, 49 warnings (the baseline's count).
- The validator (`MainMenu.tscn --quit-after 300 -- --no-save-writes --validate-content`): **0 errors and 1
  warning** (`test_wizard`), the baseline's, with `ValidateFarShore` and the far-shore gate reachability inside
  it. The walk proves a brand-new save that does everything on the campaign earns the White Downs' gate. Each
  half is proved needed (no Throne; one spell short; each counted wizard held), and the Dark Wizard and John Dee
  are proved not needed. `ValidateGateReachability` still passes for the Emberdeep and the Throne.
- **Shown to fire** by breaking what they guard, in a scratch build that was not committed:
  - `stage_15` added to `CampaignOrder`: "in CampaignOrder too", "FinalChapter is a far chapter", and the whole
    road no longer finishable.
  - The Bronze Isle flipped playable: "playable but still has stand-ins" and "no playable far chapter before it".
  - The Grimoire dropped from the gate: "opens with 'arcane_explosion' still unrecovered".
  - The first stop revealed without Taken Back: "a new save is shown The White Downs".
- `_UiShot --scene=res://scenes/WorldMap.tscn --all-unlocked --map-at=stage_13` at 1280x720, windowed. On a save
  with everything, whose opened list holds `stage_14`-`stage_19`, the map still ends at the Throne ("Chapter 13 of
  13", compass 10-13). No far stop, no mark, no error. The old `StageSelection` list hides any chapter that
  `IsStageAvailable` refuses, and an unplayable chapter is always refused.

**Not run.** No far chapter as a run (`--stage=14` would play the borrowed ground with the Cursed Dungeon's cast,
as the castle's stand-ins did). No phone canvas, because F0 drew nothing. `_BalanceHarness` was not run either:
no code on a road run's path changed except the order-aware lookups, which answer as before for every campaign
chapter.

### F1, built (2026-11-08, branch `worktree-agent-acd7f256fda5321a7`, on `main-ff86ni` after F0)

A god can be added as data. No god is in the game yet (F2 adds Artemis), nothing a player sees has changed
except one card line for the dark wizard (below), the Dark Wizard opens exactly as before, **no save schema step**
(still 25), and the story epoch is not raised.

**What a god is, in the code.** A character whose `UnlockCatalog` line is `FromGod(id, seat, held line)`. The seat
(`UnlockDefinition.SeatStageId`) is the one declaration; `UnlockCatalog.IsGod`, `Gods` and `GodSeatedAt` read it,
and everything a god does differently from a wizard asks `IsGod` rather than keeping a list. `FromGod` sets what
every god shares: its source is its seat's warden deed (`AchievementDefinitions.SeatDeedId(seat)`, "seat_stage_14"),
it is secret until the seat opens, and it is outside every gate (`OutsideCampaignGate`; the far shore's own gate
never counts anything paid on the far shore, `CountsTowardFarShore`).

**The opener, any recipe.** The machinery was already general in shape (`StartsFusedFor`, `AddStartingConjunction`,
`LevelStartingConjunction`, `TryFuse` handing a fused copy the table's last level, `OnStartingConjunction`); F1 made
the rest of it so. `ComboCatalog.Openers` and `OpenerFor(character)`; `ComboDefinition.IsOpener`;
`SpellData.ReadsOwnTable` (a Conjunction carrying no frozen parents - the copy a character opened on - whose script
must read its own table: Sagittarius's table path, F2's code, branches on it). `ApotheosisCatalog.SpellMeetsGate`
and `OnStartingConjunction` take the recipe as an argument too (the old signatures look it up), so a god built outside
the catalog is judged by the same rule. `ValidateStartingConjunction` became `ValidateOpeners`
(`RegressionChecks.Gods.cs`), over every opener: its character exists, starts with it and opens on one spell; it
levels 1-8 with a line at every level and the cards take it all the way; at most three elements; its form is its
opener's alone (checked against the whole roster, not the Pyromancer), deepens an element it carries, opens at its
max level and not one short. And the other direction, which nothing held: **a character whose starting spell is a
Conjunction must be its opener**, or `Player.ApplySelectedCharacter` asks to fuse it, is refused, and he holds
nothing. `ValidateStartingCharacters` lets an opener's Conjunction carry three elements (Sagittarius, Dark Lens,
Orrery and Hailfront do); every other freed wizard keeps the two-element ceiling.

**The god's card lines** (one wording, `QuestLog`): the quest card "Rekindle the huntress" / "Artemis is rekindled"
(`FreeTitle`, `FreedTitle`), the run's paid line "God rekindled - Artemis" (`PaidLine`, `Node2DGame.PaidLine`), a
locked god's deed row "Rekindles someone" (`HiddenRewardLine`, `CodexRows`), Options > Story "Rekindled: Artemis"
(`ReplayLabel`), and a deed's reward verb `RekindlesVerb`. The character select leaves a god **off the screen** until
its seat opens (`CharacterSelection.IsHiddenUntilRevealed`, the dark wizard's rule widened: 6.2, nothing of the far
shore on show), then shows its silhouette with its held line (`FromGod`'s third argument, held word for word to its
`QuestLog.Prisoners` held line). The line naming what a character opens on reads **"Conjunction"** rather than
"Spell" for every opener on a save that has learned Conjunctions (`CharacterSelection.OpeningKey`) - the dark
wizard's card included, the one visible change. A god renders untinted by rule (`CharacterVisuals`), and never stands
in the Throne ending's row of the wizards he took (`ThroneEnding.Wizards`).

**The rekindling reel.** `tools/art/freed.py` draws a second shared ground, `rekindled-back.png` /
`rekindled-fore.png` (assets/bonelight/ui/freed/, imported): the far shore at night under no moon, the cliff-top
downland with its broken chalk brink, the white cliffs going away along the coast to the right (the nearest at the
PC's right edge, their far end low behind the god on a phone), and out on the dark sea between the two figures his
rock with the tower **unlit**. `p_rekindled` is the god's mid layer: the god's own sprite, its light coming **down**
out of the sky in a column that narrows as it lands, no chain, the Archmage beside it. `GODS` is empty and marked;
`--stand-in=medea` draws the template with a wizard standing in, preview only. `Freeings.GroundFor` picks the ground
for a god, and `Prologue.FreedReel` reads it, so a god's reel is its row in `Freeings.WizardIds` and in `GODS`.

**The seat deed.** `AchievementDefinitions.SeatClear(seat, name, boss id, god's name)` builds a far warden's deed: id
`SeatDeedId(seat)`, "Defeat the boss of the White Downs.", "Rekindles Artemis", the god it pays read from the
catalog (`GodSeatedAt`). **A god's piece and its chapter's piece may merge in either order**: a chapter before its
god pays nobody and keeps its to-do line ("no god seated here", new in `ReportFarShoreToDo`); a god before its
chapter's deed is let through `ValidateUnlockCatalog` (its deed not yet written) and `ValidateSecret` (revealed by a
chapter not yet playable - which is exactly what keeps it off the character select) while its seat is unbuilt, and
`ValidateGods` makes the missing deed a fault the day the seat is playable.

**What a god's piece writes now (the recipe F2 follows).** Data, plus the spell and passive code that is that god's
own:

1. `CharacterData_<God>.tres` (Id, Name, `StartingSpellResource` = its Conjunction's SpellData, passive id, name and
   description, health and speed, Portrait and Frames) and its sheet (`tools/art/player_<god>.py`).
2. `CharacterRoster`: its path appended at the end in merge order, the line saying the index it took (23 for the first).
3. `ComboCatalog`: `StartsFusedFor = "<god>"` on its recipe - F2 in Sagittarius's block, F6-F10 in their own recipe
   block in their slot.
4. Its Conjunction's resource levelled 1-8 with a line at every level, and its script reading that table when
   `SpellData.ReadsOwnTable` (Sagittarius's table path is F2's code; the five unbuilt ones are built table-first).
5. `ApotheosisCatalog`: its form in its slot (a verb nobody owns, +1 of the god's element, which its Conjunction
   carries), the form's behaviour in the spell's script, and its `EvolutionStatReaders` reach.
6. `UnlockCatalog`: `FromGod("<god>", "<seat>", "<held line>")` in its slot.
7. `QuestLog.Prisoners`: its row in F0's slot - epithet, the same held line, the rekindled line with `{0}`, 46
   characters at most with the name in it.
8. `Freeings.WizardIds`: its id in its slot; `tools/art/freed.py`: its row in `GODS`; run it and import.
9. `CharacterVisuals.GetStaffTipCell`: its light's cell in its slot.
10. Its passive: a `StartingPassiveId` in `Player.CharacterPassiveBonusIds`, read where its axis lives.

The seat's deed is the chapter's piece's line: `SeatClear("<seat>", "<name>", "<boss id>", "<God name>")` in F0's
F3/F6-F10 slot in `AchievementDefinitions`. What a god's piece does **not** write: any character-select, quest,
results, reel or gate code - all of it asks `IsGod`.

**Who owns what (the blocks F1 marked).** A comment slot a piece, a "leave this one as it is" line between
neighbours, as F0's.

| piece | edits only its own line in |
|---|---|
| F2 | `ComboCatalog` (Sagittarius's block: its `StartsFusedFor`), `ApotheosisCatalog` (the gods' forms), `UnlockCatalog` (the gods' lines), `Freeings.WizardIds`, `freed.py` `GODS`, `CharacterVisuals.GetStaffTipCell` |
| F6-F10 | the same five, and in `ComboCatalog` its own recipe block in the gods' Conjunctions slots (between The Taking and the Sovereign Rite, which stays last) |
| every god | `CharacterRoster`: **no slots** - appended at the end in the order the gods merge (a roster index is what a save and `--character=N` keep); the comment there says so |

`CharacterVisuals` was not on section 9's list; it is marked because its staff-tip switch is a per-god line and two
gods' pieces appending to it would collide.

**Verified.**

- `dotnet build`: 0 errors, 49 warnings (the baseline's count).
- The validator (`MainMenu.tscn --quit-after 300 -- --no-save-writes --validate-content`): **0 errors and 1
  warning** (`test_wizard`), the baseline's, with `ValidateOpeners`, `ValidateGods` and `ValidateGodsAsData` in it.
  It prints `gods as data - a complete god built from data alone passes on The White Downs (0 complaints), and 16 of
  16 broken ones are refused`: a scratch god, built in `RegressionChecks` and never in a shipped catalog (a `FromGod`
  line, a CharacterData, a copy of The Taking's recipe naming it as its opener, a form, a Prisoners row, a reel on the
  far shore's ground, a seat deed), passes every god and opener check, passes with no seat deed while the seat is
  unbuilt, and is refused with any of sixteen parts missing or wrong (no row, a card line that is not its held line,
  a rekindled line without its name or too long, no reel, a reel on the heath, a recipe naming another opener, a
  Conjunction with no table or a level line missing, a starting spell that is not its Conjunction, no form, a form
  on an element its Conjunction lacks, a seat on the road, inside a gate, on show before its seat, a seat deed
  rekindling someone else). The scratch objects are disposed as they are made: left to the finalizer they were
  reported as leaked at exit, which the first run showed and the second does not.
- **Shown to fire on the real catalogs**, in a scratch build that was not committed: Sagittarius given
  `StartsFusedFor = "artemis"`, F2's `FromGod` line written with nothing else of her, and The Taking's
  `StartsFusedFor` taken away. New lines: "Combination 'sagittarius' is opened on by 'artemis', who does not start
  with it", "Combination 'sagittarius' levels to 1; a Conjunction a character opens on levels to 8 off its own
  table", "Openers: 'dark_wizard' starts with the Conjunction 'the_taking', which does not name him ... hold
  nothing", and "God 'artemis' has no CharacterData in CharacterRoster / no Apotheosis on its Conjunction / no
  QuestLog.Prisoners row / no rekindling reel"; the White Downs' to-do dropped "no god seated here"; and no "points at
  achievement 'seat_stage_14', which does not exist". That run also showed `ValidateSecret` refusing a god whose seat
  is unbuilt, which would have stopped F2 merging before F3; it now lets a god through, as above.
- The dark wizard (`_BalanceHarness --minutes=3 --character=10 --all-unlocked --no-save-writes`, before and after):
  all three runs (one before, two after) open "Equipped spell The Taking at level 1 (a Conjunction he opens on,
  levelling to 8)" and take Gale Blade with the first card. How long the bot then lives varied - the clock at 3.0
  minutes before, dead at 2.7 and 1.9 after - which is the default bot without `--immortal` on this seed
  (`dark-wizard-playable.md` 6: it "dies inside two minutes ... so that run says nothing") on a machine shared with
  other agents' Godot runs. Nothing on a run's path changed but the wording of the paid line and a tint lookup.
- `freed.py`: 0 off-contract pixels on both grounds and the stand-in; the twelve freeings' layers regenerate
  byte-identical. Looked at in both views (PC 1280x720 crop, Pixel 9 tall crop).

**Not run, and for whom.** No god exists, so nothing a god does was played: its card, its quest, its reel and its
run are F2's to see. `_UiShot` of the character select was not taken, so the dark wizard's "Conjunction" line on a
save that has learned them is unseen on screen (a build and the validator only prove it compiles and is reached).
The rekindled ground has been judged with a wizard standing in for a god, not with a god.

### F2, built (2026-11-08, branch `worktree-agent-a86098d68c99af091`, on `main-ff86ni` after F1)

Artemis is in the game as data and code, whole: her card, her run, her form, her passive, her reel. **Nobody can reach
her yet** - she is rekindled by the White Downs' warden, and the White Downs are F3's and still `IsPlayable = false`, so
a new save never sees her and a save with everything sees her silhouette only once her seat is played. Nothing on the
road changed except Sagittarius's level number and every spell's reach for her alone (below). **No save schema step**
(still 25), and the story epoch is not raised.

**Where it lives** - F1's ten steps, in its slots:

| step | where |
|---|---|
| her and her sheet | `CharacterData_Artemis.tres`: Sagittarius, Far-Shooting (`reach_15`), 0.90 health, 1.08 speed (the table's first guesses, kept). `tools/art/player_artemis.py` -> `artemis-{1..4,idle,moving,hurt,death}.png`, `ArtemisFrames.tres`; 1.01 edges/px; `anim_sets.WALK_BOB` gives her the striders' two-row bob |
| roster | `CharacterRoster`, appended: **index 23** (`--character=23`) |
| opener | `ComboCatalog`, Sagittarius's block: `StartsFusedFor = "artemis"` |
| her table | `SpellData_Sagittarius.tres`, levels 1-8; the table path in `Player.FireSagittarius` (`SpellData.ReadsOwnTable`), `Sagittarius.Cloudburst`, `Starfall.Linked` |
| her form | `ApotheosisCatalog`, the gods' slot: The Hunter's Moon (hunt), Light; its behaviour in `Sagittarius` (`HuntersMoonId`, `Hunt`, `Volley`) and `Starfall` (`Lit.Shots`, `LooseLit`, `FlareLoosed`); the `sagittarius` row of `EvolutionStatReaders` names both paths; icon `assets/bonelight/ui/evolutions/sagittarius_hunters_moon.png` (`evolution_icons.py`, a card of its own and Sagittarius in `SPELLS`; `evolution_catalog.json` regenerated by `_AscensionAudit`) |
| her unlock | `UnlockCatalog`: `FromGod("artemis", "stage_14", "No moon rises over the White Downs.")` |
| her words | `QuestLog.Prisoners`: "the huntress", the held line above, "{0} hunts the White Downs again." (36 characters) |
| her reel | `Freeings.WizardIds` and `freed.py`'s `GODS` (`("artemis", ("artemis", "light", "light"))`) -> `freed-artemis-mid.png`, 0 off-contract pixels; both grounds regenerated byte-identical |
| her light | `CharacterVisuals.GetStaffTipCell("artemis")` = (27, 6), the crescent's bowl (the generator prints 27.0, 5.7) |
| her passive | `Player.CharacterPassiveBonusIds` `reach_15` -> `SpellData.ReachScale` 1.15, read through `SpellData.GetReachAtLevel` |

**Sagittarius, opened on.** Her copy carries no parents, so it reads its own table, and the fused path is untouched. The
**rain is the Conjunction itself**: `FireSagittarius` hands the spell in as the rain, so the stars' hit, the cloudburst,
the count, the width and each burst are her levels, and her form's x1.25 reaches them through `GetDamageAtLevel`. The
**lines are on from level 1** (`Starfall.Linked`): the archer's figure is the recipe, not a level. **Each arrow is 0.7
of one star's hit** (`Sagittarius.TableArrowShare`), scaled by the draw as Hunter's Draw's is (x0.45 to x2.2). The
**bow is Hunter's Draw down Hunter's Mark at her Sagittarius's level** (`Player.SagittariusTableBow`): its pierce, reach
and nock grow with her, and Hunter's Mark's own pierce and quicker nock count from her level 8 (an Ascension's numbers
start at 8); its size and speed are whole from the first second (`GetScaleMultiplier` reads every milestone). The
draw, the mark and the loose are one code path for both copies.

| level | the line | a star | cloudburst | stars | rain |
|---|---|---|---|---|---|
| 1 | the bow, the held stars, the loose, the mark, the lines | 5 | 1.00 s | 4 | 240 |
| 2 | Damage +20%, Cooldown -6% | 6 | 0.94 | 4 | 240 |
| 3 | Stars +1, Wider area (+3 px a burst) | 6 | 0.94 | 5 | 240 |
| 4 | Damage +50%, Cooldown -6%, Range +6% | 9 | 0.88 | 5 | 255 |
| 5 | Stars +1, Wider area | 9 | 0.88 | 6 | 255 |
| 6 | Damage +44%, Cooldown -11% | 13 | 0.78 | 6 | 255 |
| 7 | Stars +1, Range +6%, Wider area | 13 | 0.78 | 7 | 270 |
| 8 | Damage +69%, Cooldown -13%, Stars +1 - the full figure | 22 | 0.68 | 8 | 270 |

Level 8 is a plain fused copy's numbers (Starfall's level 8 down Constellation: 22, 0.68 s, 8, 270, +9 px; an arrow
0.7 x 22 = 15.4 against the fused 19 x 0.8 = 15.2), as The Taking's level 8 is its fused copy. The big level 8 is the
parent's own shape: Constellation doubles Starfall's hit at 8. **A fused copy now arrives at level 8 of 8** (`TryFuse`
does that for any opener's recipe; `Freeze` still writes its numbers from the frozen rain and clears the table), so the
HUD shows "8" where it showed "1". Its card noun is "Stars" (`Player.SpellProjectileNoun`).

**The Hunter's Moon (hunt).** *"When your quarry falls, the mark passes to the strongest in reach and every lit star
looses again."* As built: under the form **every volley marks what it was loosed at** (Vulnerable +25%, 5 s, as Hunter's
Mark's), and a loose **does not put its stars out**: each stays lit, unheld, for a lit star's linger (2.5 s, with the
player's Duration) with one shot left. When the marked quarry dies - to her arrows or anything else - while still
marked, the mark passes to the strongest enemy within the bow's reach of her, and every star with a shot left looses at
it with the draw its stop was loosed with, then goes out. **Each star looses at most twice a stop**
(`HuntShotsPerStar`), so a stop is at most 2 x 16 arrows; a star kept from one stop that is still lit at the next looses
its second shot there. With no star left the mark still passes, so the next stop looses at it - the volley runs quarry
to quarry. Bosses are marked and struck, never held. `_BalanceHarness` prints `HARNESS HUNT` (volleys, passes,
re-volleys).

**Far-Shooting** (Homer's word for her). *"Spells find their targets 15% farther off."* `SpellData.ReachScale` is a
run's (reset in `Player._Ready`, as Bone Fence's scale is) and is read only through `GetReachAtLevel`, at every place a
range is a **reach**: the 21 casts in `Player` that look for a target within it, and the scripts whose shot must fly as
far as it found one (`MagicMissile`, `ElementalBolt` - so the bolts and both bows' arrows -, `Fireball`, `ChainLightning`,
`ScorchingRayBeam`, and the hunting `Familiar` and `SpiritualWeapon`); Sagittarius's mark reach too. Never where a range
is a **size** - a cone's or a jet's length, a burst's radius, the rain's width, Stormsweep's band, Gloam Veil's walk -
so it is never an area bonus in disguise, and the cards and validators still read the range as written.

**Her sheet** (`.ai/art-direction.md`, `.ai/world-and-tone.md`). Drawn against the cast, as 3.3 asks: her light is **on
her**, a crescent lying on its back at her brow, white at the bowl (standing horns read as horns); **no robe** - a short
bone chiton (the row the contract says the moon is made of), bare legs in laced boots, mid-stride, the only figure but
the Pyromancer whose legs show and the only one walking out of her cell; **tall and slender**, about eight heads; the
**gold bow across her back**, its upper limb hooked over her far shoulder and its lower limb out past her near hip, its
string pale against the sky and dark across her chest, so the two limbs read as one bow; a night-violet mantle swept
off-axis behind her; dark hair in a knot. Faces right (`SpriteFacesLeft` false). The silhouette survives filling black:
the stride, the bow's two hooks and the knot.

**Verified.**

- `dotnet build`: 0 errors (no warning in any file F2 touched).
- The validator (`MainMenu.tscn --quit-after 300 -- --no-save-writes --validate-content`): **0 errors and 1 warning**
  (`test_wizard`), with `ValidateOpeners`, `ValidateGods` and `ValidateGodsAsData` (16 of 16) passing for her, no
  exemption; the White Downs' to-do lost "no god seated here" and keeps "no deed frees its god" (F3's `SeatClear`).
  The one `ObjectDB ... leaked` line at exit is the menu music's stream (`--verbose`), not F2's.
- `_BalanceHarness --minutes=15 --character=23 --all-unlocked --immortal --seek-apotheosis --seed=1234 --timescale=3`:
  opens "Equipped spell Sagittarius at level 1 (the Conjunction Artemis opens on, levelling to 8)"; its cards take it to
  8 at **5.3 minutes** (player level 13), the gate met there; **The Hunter's Moon offered and taken at 8.2 minutes**,
  over the 7.5 floor; Sagittarius **82.0%** of 27,047 damage a minute; level 31 at 15; 56.6 health lost a minute; the
  hunt 318 volleys, 293 passes, 157 re-volleys. (The Dark Wizard's run in `.ai/dark-wizard-playable.md` 6, on an older
  tip: his form at 11.3 minutes, 92%; Seimei's 27,353 a minute.)
- `_BalanceHarness --minutes=8 --character=23 --all-unlocked --immortal --seed=1234 --timescale=3` (the default bot that
  spreads its picks, the Dark Wizard's measure): Sagittarius at **level 3, 38.1%** of 11,030 a minute, second to Gloam
  Veil's 38.5%. His was 29.2% at level 2. The first draft was 58.9% (the bow whole from level 1, 6 a star) and 45.5%
  with the bow levelling with her; a star of 5 at level 1 is the 38.1%.
- `_UiShot` (`user://`): `f2-artemis-select-fresh-pc` (a new save: she is not on the screen);
  `f2-artemis-select-silhouette-{pc,phone}` (`--fresh-save --open-stages=stage_14`, the PC with
  `--hover-late=CharacterButton24`: her silhouette and "No moon rises over the White Downs." - **taken in a scratch build
  with the White Downs flipped playable, not committed**, since an unplayable seat reveals nobody);
  `f2-artemis-select-unlocked-pc` (`--all-unlocked`: "Conjunction - Sagittarius, Apotheosis - The Hunter's Moon,
  Passive - Spells find their targets 15% farther off"); `f2-artemis-reel-{pc,phone}` (`--reel=freed_artemis
  --frames=200`, **with `--fixed-fps 60`** - without it the reel's shot never came); `f2-artemis-hunt2-pc-{0..7}`
  (`--demo-spell-level=8 --demo-evolutions=sagittarius_hunters_moon --spell-stage=14,200,0 --stage-mortal --walk=0,1
  --walk-until=148 --frames=152 --burst=8,5`: the volley converging on the crowd, the kills, the mark passed);
  `f2-fused-sagittarius-pc-{0..3}` (`--character=8 --demo-combo=sagittarius:fuse`: a fused copy still fires, at "8").

**Decisions F2 made** (the doc left them open): the rain is the Conjunction itself and the bow levels with her (above);
an arrow is 0.7 of a star; the lines from level 1; every volley under the form marks; a quarry "falls" to anything; the
next quarry is the strongest in the bow's reach of her, not of the fallen; the kept stars' window is a lit star's
linger; Far-Shooting is reach, not size, and lives on `SpellData` as Bone Fence's lives on `Enemy`; the crescent lies on
its back; her chiton is bone, her mantle violet, her bow gold; her passive's words.

**Needs a person's eyes.** Whether she reads as a god and not one more wizard at game size, beside the cast (the
crescent could read as a tiara; the bow's two hooks as two things); the Hunter's Moon in play - whether the mark passing
reads, and that the mark can pass to a quarry off the screen (the bow's reach is 740 at level 8, 851 with Far-Shooting,
as a fused copy's Hunter's Mark already reaches 740); the icon at 24 px; whether 38% at level 3 is "inside the
starters' band" (his 29% was at level 2) and whether her level 8 at 5.3 minutes is early for a bot that seeks it; and
every number above, which the bot has met twice.

### F5, built (2026-11-08, branch `worktree-agent-a0ae452ba0fa3fe7c`, on `main-ff86ni` after F1)

The far shore is on the map, across the dark sea, and announced by reel E. Nothing is playable that was not, every
far stop is still `IsPlayable = false` and under the unbuilt mist, the Throne is the end of the road for any save that
has not taken it back, and the Dark Wizard's and The Road Restored's targets are untouched (nothing here reads or
writes the campaign). **No save schema step** (still 25): reel E's flag is additive and owed only with Taken Back. The
story epoch is not raised.

**The map** (`tools/art/world_map.py`; `.ai/world-map.md` section 19 is the full record). The world is 3424 wide, one
picture; west of the castle it is byte-for-byte what it was. The road goes on past the Throne east out of his postern,
down to his east sea-gate, over an old causeway of pale stone to a cove under the white cliffs, and through the six
seats in `FarShoreOrder` (`RoadOrder` already placed them; `PLACES` and `WAYPOINTS` now draw them). Each seat has its
ground, its landmark and his mark: the White Downs' barrows and the hound and the hart cut in the turf; the Drowned
Shore's ribbed seabed, channels, wrecks and whale ribs behind the Weir; the Three Ways' hedged lanes forking three and
three again round a three-faced pillar; the Twelfth Hour's avenue of sphinxes to a pylon by the still river; the
Thunder Fell's tors, cairns and split pines under a storm that does not break; the Bronze Isle's verdigris yards,
anvils, tripods and Talos, over a bronze bridge. No light burns anywhere on the far shore.

**What a save sees** (the reveal, F0's `IsChapterRevealed`, unchanged): before Taken Back nothing past the rock - the
dark begins at the castle's east wall, with the tower's dark light burning; after it, the tower unlit, the light
streaming east off its spire, the crossing and the White Downs drawn out of the dark under the mist with its lock line,
and nothing more until each later seat is earned. **Decided: the far shore's places are drawn in the picture whatever
the save**, as the road's are, and the reveal covers them; 6.2's "the far shore is drawn past the water, fogged" is the
first stop under its mist, the rest under the dark. The compass marks the crossing with three waves.

**Reel E** (`tools/art/light_goes_on.py` into `assets/bonelight/ui/story/`; `Prologue.Reels["light_goes_on"]`;
`.ai/cutscenes.md` E). Two panels, the Archmage at the centre of both, calm:

1. `lightgoes-gate`, 5.6 s, no line: his east sea-gate from inside, the Archmage on the threshold with his back to us
   and his own light up, the light streaming out over him through the arch and away over the dark water, its gold on
   the sea.
2. `lightgoes-sea`, 6.4 s, *"Some of it was older than any of them."*: wide and low from the quay at the rock's foot,
   the stream running on east to a line of white cliffs on the horizon and past the edge of the picture, the Archmage
   small on the quay.

It plays after the ending on the run that wins the Throne - after the freeing of the one he kept and before any lesson
(`Node2DGame.PlayOwedReelsThen`, `PlayOwedLightGoesOnThen`) - and, for a save that won the Throne before it existed (or
quit before it played), the first time the world map opens (`WorldMap._Ready`), which comes back with the White Downs
drawn out of the dark. Options > Story lists "The Light Goes On" once seen.

**Where it lives.**

| piece | where |
|---|---|
| the map's generator and art | `tools/art/world_map.py` (`CASTLE_W`, `FAR_X0`, `FAR_SEEDS`, the far coasts, `castle_crossing`, `causeway`, `white_cliffs`, `downs_scarp`, `downs`, `far_water`, `drowned`, `three_ways`, `twelfth_hour`, `thunder_fell`, `far_mountains`, `far_groves`, `bronze_isle`, `LIGHT_PATH`, `tower_patch`, `--preview-far`); `world.png`, `tower-unlit.png`, `shroud-stage_14..19.png`, `worldmap.json`'s `light_home` |
| reading it | `WorldMapLayout` (`TowerUnlitImage`, `TowerUnlitRect`, `MotePath`); `WorldMapView.ShowLightGoneHome`, `DrawMotes`, `RoadSearch` (the reveal's search, exact and about three times quicker); `WorldMap` (the light after Taken Back, the map-open reel, `RoadOrder`'s comment); `WorldMapCompass.Mark.SeaBefore` |
| the reel | `tools/art/light_goes_on.py`; `scripts/LightGoesOn.cs` (`IsOwed`, `IsWatchable`, `MarkSeen`); `SaveData.HasSeenLightGoesOn`; `Prologue` (`StoryFolder`, `LightGoesOnBeats`, marks it seen when it ends); `Node2DGame.Lessons.cs`; `MainMenu.Options.cs` |
| the validators | `RegressionChecks.ValidateWorldMapFarShore` (in `ValidateWorldMap`: every far chapter placed across the sea east of the Throne, the light-home data and patch, the motes leaving the crown eastward, the reveal before and after Taken Back); `ValidateLightGoesOn` (in `ValidatePrologueReel`: its panels, one line, and its debt on sample saves including one written before the flag existed) |
| the harness | `_UiShot --unbeaten=<stage,...>` (a boss not beaten, Taken Back undone with the Throne's) and `--light-goes-on=owed|seen`; `--all-unlocked` and `--campaign-complete` mark reel E seen so the map opens as the map; `_PrologueProbe` steps 22-29 |

**Decisions F5 made** (the doc left them open).

1. **The crossing leaves the tower east**, by a postern and a second sea-gate on the rock's east side, rather than back
   down through the yard to the cells' sea-gate in the south face: a road doubled under the castle's own would have
   let the reveal's dark onto the castle. "His sea-gate" in 6.3 is this one.
2. **The way over the water is an old causeway**, pale and low and older than him, not a ferry or a dotted line: the
   wizard walks the road, and the castle as a door he sat down in (section 2) wants a road through it.
3. **The far shore is drawn whole and the reveal covers it** (above).
4. **Reel E's light is the light ramp with streaks behind it**, so a still panel reads as light going and not as a
   field of stars; where it lands it is gold pigment. Two panels, close then wide, one line on the second.
5. **Reel E plays after the freeing of the one he kept**, which is the road's last beat, and before any lesson.
6. **Its flag is marked in memory even when writes are off**, as a freeing's debt is, so a tool run's map cannot loop
   on it; `--all-unlocked` and `--campaign-complete` start it seen.
7. **The motes are runtime, the unlit tower is art**: the tower going dark is a picture swap (one generated patch), and
   the stream is 120 seeded single pixels the view draws over the map and the dark alike.
8. **The reveal's search was rewritten** (`RoadSearch`) because the wider world made it 2.6 times the work; it returns
   the same distances.

**Verified.**

- `dotnet build`: 0 errors, 53 warnings, none of them in a file this piece touched.
- The validator (`MainMenu.tscn --quit-after 300 -- --no-save-writes --validate-content`): **0 errors and 1 warning**
  (`test_wizard`), with `ValidateWorldMap` passing for all 19 chapters of both orders and the six far-shore to-do lines
  no longer listing "no place on the world map". One run of three printed Godot's "1 resources still in use at exit";
  a verbose rerun did not, so it is GC timing at quit, not a validator.
- `world_map.py`: 0 off-contract pixels (world, tower patch, shrouds, marks); compared with the committed picture, every
  pixel west of x 1984 is identical. `light_goes_on.py`: 0 off-contract pixels on both panels.
- `_UiShot` at 1280x720 and 540x1212 `--portrait --safe-area`, looked at: the map at the Throne before Taken Back
  (`--fresh-save --campaign-complete --unbeaten=stage_13`), after it at the Throne and at the White Downs
  (`--campaign-complete`: Taken Back, every spell and wizard, so the gate is met - the Downs still show "still being
  built", being a stand-in), every far place on `--all-unlocked` (PC), and reel E's two panels mid-hold, line up.
- `_PrologueProbe`: every new step passes - the map plays reel E first on a save that won the Throne, it returns to the
  map with the light gone home, owes nothing, does not play again, and Options lists and plays it. Seven failures in
  the probe's older Start Run steps (1-14) are a timing of the title's Start Run, which this piece did not touch.
- The run that wins the Throne (`_UiShot --scene=res://scenes/node_2d_game.tscn --stage=13 --campaign-complete
  --unbeaten=stage_13 --light-goes-on=owed --dismiss-levelups=99 --force-victory --ending-through=120`, windowed): the
  ending's row and credits, then the freeing of the one he kept (`freed_nimue`), then reel E played to its end, then
  the lesson that save owed (`sovereign_rite`), then the results - story before systems, E after the road's last beat.

**Not run, and for whom.** No phone. No far chapter, since none is playable. The reel and the motes were seen still,
never in motion at speed on a real screen; whether the stream reads as the light going home moving, and whether reel
E's line lands, is Eric's call. The far places' landmarks are under the unbuilt mist on every save today, so each
chapter's piece (F3, F6-F10) is the first to see its own place uncovered: if the place on the map does not match the
chapter it became, the landmark function is that chapter's to adjust in `world_map.py` (its own function and its
`PLACES`/`WAYPOINTS` line).

### F3, built (2026-11-08, branch `worktree-agent-a02f5b4c89ec986ae`, on `main-ff86ni` at 8cd0e007)

The White Downs is built in full but for its flag. **`IsPlayable` stays false**, and must until F2 and F5 merge: F0's
rule turns a far chapter's to-do into a warning the day it is playable, and two of its lines are not F3's to close -
"no god seated here" (Artemis, F2; "no deed frees its god" is the same line seen from the seat deed, which is written
and pays her the moment her `FromGod` line exists) and "no place on the world map" (F5). Flipping it is one line in its
`StageCatalog` block once both are in. **No save schema step** (still 25); the story epoch is not raised.

**The chapter as built.**

| part | what | where |
|---|---|---|
| ground | cropped turf at night (`downs_turf`, a terrain laid as the Cave lays its floor: the lichen row on `tiles.py`'s natural style is the Forest's turf), bare chalk scars and flint rubble, rare dew ponds. **The barrows are walls**: one candidate a 16-tile super-cell - a bowl barrow (radius 3.2-3.6), a great round one (4.15-4.45) or a long barrow (a stadium 3 or 5 wide, 9-13 long) - placed from (seed, super-cell), one too close to a neighbour standing down to the higher priority roll, none within 9 tiles of the start; radii held to the bands whose last row is three tiles wide (the first photographs had a nub on every crown). Walled, as the Ruins are | `LevelGenerator.Downs.cs` (`StageTopology.Barrows`), the palette and profile blocks, `GroundDetail`; art `terrain_downs.py`, `downs_walls.py` (`barrow_wall`), `detail_downs.py` |
| foliage | clumps, not woods: beeches on the crowns, hawthorn and juniper on the flanks, sarsens lying in loose flocks (knee-high: they stop feet, never a sightline), flint heaps and chalk; harebells, thyme and rabbit scrapes. Salts 0x5F12-4; measured shares inside the validator's 3-20% stand, 55-88% open | `Foliage.WhiteDowns.cs`, `props_downs.py` (`downs.json`) |
| cast | eleven, every role (the castle's precedent, and `ValidateBestiary` wants every role filled or absent with a reason): the hunt (Beater, Coursing Hound, Huntsman, Riderless Horse), the people of the chalk (Night Shepherd, Flint-Slinger, Warrener), the Downland Tup, the Giant Puffball, the cliff-watch (its Beacon, built and burning; its Captain, the miniboss). Stand-ins the Emberdeep's; table mean 1.266; the far shore's bolt is metal (silver) | `StageBestiary.WhiteDowns.cs`, `cast_downs.py`; `.ai/bestiary.md` status |
| warden | **the Unstrung Hunter** (`downs_unstrung_hunter`, 420,000, bonus 400): Orion's shadow, a tall hunter beside a bow taller than himself, three cold stars for a belt. Below | `UnstrungHunterBoss.cs` / `.tscn`, `boss_unstrung_hunter.py`, `BossCatalog` [14], `SeatClear("stage_14", "The Hunter Brought Down", ...)` |
| side event | **the Silver Hind** (`silver_hind`): stalk. Below | `SilverHindEvent.cs`, `silver_hind.py` (and the `hind` pictogram) |
| secrets | **the Hound in the Hill** (anchor, linger 3 s in its eye: 25% faster for a minute, every find) and **the Hart in the Hill** (keepsake, gather: lift its seven knots of turf in any order - healed by half, 150 Arcane Energy once). No found level: all four of issue #20's are spoken for on the road | `SecretCatalog.WhiteDowns.cs`, `HoundInTheHill.cs`, `HartInTheHill.cs`, `secret_downs.py` |
| music | `downs`: E with no F (E G A B C D), 12/8 at a dotted quarter of 140, 27.4 s - a key, metre and tempo no other set has; a hunting horn that never sounds a third, three high voices answering; the beaters' line in low strings; hoofbeats in the pulse. Not yet heard | `music_downs.py`; `MusicCatalog`'s planned set switched itself on |
| sounds | `hint_hunt_horn` (the event's hint, and the Hunter's horn), `hunter_draw`, `hunter_loose`, `hind_bolt`; `analyse.py --confuse`: 0 confusable pairs | `sfx.py`, `SfxCatalog`, `.ai/audio-manifest.md` |
| words | the event's deed *The Hind at Gaze*, its hint (the hunt's horn far off, birds lifting), its clue in the note; the boss's tagline "He draws on you from far off. Put a barrow between you." | `AchievementDefinitions`, `StageHintCatalog`, `ChapterNotes` |
| difficulty | the castle's row, unchanged (decision 11) | `ChapterDifficultyCatalog` |

**The Unstrung Hunter.** His one attack is the draw: a lane (`TelegraphShape.Line`, 60 px wide) from his body out over
the open down. While he draws it follows the wizard at 80 degrees a second - faster than a running wizard sweeps round
him at any range he keeps, so it cannot be outrun - and for its last 0.45 s it holds still and whitens: the sidestep
`GroundSlamAttack.Facing` promises (`TuningProblems` holds it to the 0.26 s a wizard takes to step out). Every frame
the lane is cut where it first meets a barrow, and that length is the `Radius` the outline and the hit both read, so
what the player is shown stopping at a mound is what stops there; a loose into a barrow strikes the turf. He keeps his
range (backs off inside 360 px, circles to 680, comes on beyond), only begins a draw with a clear line, and comes round a
barrow by the swarm's own flow field - so cover is a place for a draw or two, then to leave. **0.66, the hunt is up**:
his horn, and three coursing hounds slipped at his side every 11 s, which run round a barrow - hold it while they come
or go. **0.33, unstrung**: the lane turns at 120 degrees a second and holds for 0.36 s, and after every loose four
stars fall round the wizard (one on his spot) out of the sky, over any barrow: the cover is marked now and then too.
He arrives as three cold stars falling out of the empty sky to his belt and the dark gathering round them into him,
the thread springing taut at the climax (3.2 s); he falls as they rise out of him and leave, and he sinks into the dark.

**The Silver Hind** (the ask: **stalk**; `.ai/unlock-puzzles.md` 4's table). Artemis's hind grazes on the down with his
hunt out after her. Her head comes up (0.6 s, the tell); she looks (1.4-2.3 s), her gaze a pale fan on the turf 640 px
deep, cut short at the barrows - the rays drawn are the rays that see (`Sees`). Anything that moves in it (feet carried
over 10 px in 0.15 s) she sees: she bolts to the furthest of her six seeded places, and two of the hunt's coursing hounds
are slipped at him. Still in her gaze, or moving behind a barrow, he is not seen. Come to her side unseen and she lets
him near, then goes on, calm, to the next place; the third time she is won. Every look has its tell, the one she gives
as she lands from a bound too (a look with none would catch a man still walking after her). Nothing she is joins
`"enemies"`; her rhythm is real seconds; nothing locks. It teaches the chapter's cover before the warden asks it.

**What it pays: the far shore's keepsake event, built here.** F0 left "paying Arcane Energy and a keepsake" unbuilt, and
a site with nothing in `UnlockCatalog` read as exhausted from the start (`DiscoveryRewards.IsSiteExhausted` looped over
nothing): it would have paid a chest the first time and never recorded its deed. Now `AchievementDefinitions.
SideEventCurrency(site)` (a switch, one slot a far chapter) gives the event's deed its Arcane Energy; `IsSiteExhausted`
reads such a site as exhausted once its deed is done; the hint, the note's clue and the map card read the same rule
(`IsKeepsakeEvent`; `StageRewards` lists the Arcane Energy). The first win: the deed, 200 Arcane Energy and a chest;
every win after, a chest.

**Other shared lines, each the smallest that would do:** `StageTopology.Barrows` and its partial declaration in
`LevelGenerator.cs`; `SfxCatalog`'s four; `ExploreArt.Glyphs` "hind"; `bestiary_kit.PREFIXES` "downs-";
`ValidateBossPattern`'s case; the "grants nothing" check lets a seat deed wait for its god while the seat is unplayable
(F1 meant the two pieces to merge in either order; this check had not yet met a seat deed); `_BalanceHarness --farmed`
on a far chapter stands behind the whole road and the far chapters before it (it read the campaign position, -1, and
built a new save with 300 Arcane); `_ExploreProbe`'s stalk (`StalkStep`, `--into-gaze`) and arrival from 820 px;
`_BossProbe`'s draws line.

**Decisions F3 made** (the docs left these open):

1. **The barrows are walls**, not props or landmarks: the one way the four places that must agree about a blocker
   already do, and the shot test (`MazeNavigation.IsWallAt`) is what cuts the Hunter's lane and the Hind's gaze. So a
   barrow is cover against every bolt in the chapter, and the chapter plays by walled rules (every body steers by the
   flow field; sites snap to the lattice's cell centres).
2. **Eleven creatures, not "about five"**: every role, at the castle's stand-ins and a table a step under the
   Emberdeep's mean.
3. **The warden's fight**: the lane that follows then holds (the contract's sidestep, short), the hounds at 0.66, the
   stars over the barrows at 0.33; 420,000 health between the Constable's and the Seneschal's, a first guess for its
   own measurement.
4. **The side event's ask, stalk**, and three approaches (one was a ten-second walk on the probe); its keepsake is its
   deed, with 200 Arcane Energy - a castle deed's half.
5. **Two secrets, both chalk figures** (the hound and the hart of 4.2), anchor and keepsake; the keepsake pays Arcane
   Energy, the only keepsake reward left.
6. **No flip.** See the opening paragraph.

**Verified.**

- `dotnet build`: 0 errors (the warnings are the tip's own; none in a file of F3's).
- The validator: **0 errors and 1 warning** (`test_wizard`). Its far-shore line for the White Downs reads "no god seated
  here; no deed frees its god when its warden falls; no place on the world map" - F2's and F5's.
- `_BossProbe --stage=14`: arrived, both phases entered, fell cleanly (2.1-2.5 s fall). `--cc=none` (the wizard standing,
  kept whole): 11 draws in 45 s, the Hunter holding 455-513 px. On the plain probe the kiting bot dies at castle
  difficulty and the Hunter loosed once, into a barrow.
- `_ExploreProbe --stage=14 --seed=4242 --fresh-save`: 25 passed, 0 failed - placed in the band on open ground, dormant,
  the wayfinder, the arrows; stalked three times, never seen, won in 26 s of game time (10 s of it standing still while
  she looked); the first win paid the deed. `--seed=31 --into-gaze`: 25 passed, 0 failed - seen once, the hounds
  slipped, stalked again and won in 45 s.
- `python tools/audio/analyse.py --confuse`: 0 confusable pairs.
- `_BalanceHarness --stage=14 --farmed --survivor --immortal --policy=ordinary --stop-overrun=60 --minutes=19
  --character=3 --seed=1234 --timescale=1`, `--fixed-fps 30` (the difficulty curve's protocol, 1.1), on the far shore's
  farmed save - the whole road behind it, 35 spells and 8 boons from deeds, 8,525 of 8,750 Arcane on Powers: **no
  overrun**, swarm pressure at most 0.27, L15 **45** (L17 at 5.5 min, L21 at 7.3), **37 HP lost all run** (need x0.1),
  6.1 HP a minute offered by heals and 25.1 by regeneration; the Hunter at 15:00, **down at 18.1 min - a 3.1-minute
  fight**, inside the lengthened fights' three to five. So at the castle's row the first far chapter is a place to try
  builds for a save that has walked the road, which is decision 11's reading; Hardcore is the climb. The first run of
  it, before the harness knew the far shore, stood on a near-new save (300 Arcane, 16 spells) and was overrun at minute
  five - a new save is never there, the gate keeps it out, but it says the row is a wall without the road behind it.
- Photographed (`_UiShot`, seed 4242): the chapter at 1:50 at 1280x720 and 540x1212 `--portrait --safe-area`; the
  Hunter's arrival (the stars landed on his belt, the dark gathering; then gathered); his draw in the fight, the locked
  lane cut short at a barrow beside the wizard and the arrow in flight; the Hind's gaze on the turf ("She is looking") on
  the PC canvas, and grazing under her dimmed column on the phone's; the Hound in the Hill beside the spawn, and its eye
  stood in until found (the find logged; its toast waits behind the chapter's note); the Hart in the Hill where the seed
  put it, 11.6 chunks out, its knots glinting.

**Not run, and needs eyes.** No real player has stalked the hind under a full swarm, so whether "be still when she looks
up" is found from the line and one bolt, and whether freezing in the castle's swarm is a fair price, are open; whether the
Hunter's lane reads at a glance on a phone in a crowd; whether a barrow reads as a mound and not a hedge (the long ones in
particular), and the beeches as beeches; the music, the four sounds, the Hart's gather (not driven by a probe: it is
`SecretGather`, as the Fallen Star's is) and the Hound's linger in a real run; the boss's health against a person's build.

### F4, built (2026-11-08, branch `worktree-agent-a747fe911f345a8bb`, on `main-ff86ni` after F2)

The far sky is in the game, whole: the recipe named, the favour in the deal, the card and pause lines, the payout,
both Feats, `Teaching.FarSky` and its reel, its validator and `_BalanceHarness --far-sky`. It is taught when Artemis is
rekindled - the Unstrung Hunter's deed, F3's, which merged into the trunk while F4 was building: on F4's branch alone
the White Downs is still unplayable, and once merged a save meets the sky on its next far-shore run after her reel.
**No save schema step** (still 25: the night is derived, below), and the story epoch is not raised. **The Muster
(decision 10) is not built**; the measurement below is for Eric to decide it on.

**Where it lives.**

| piece | where |
|---|---|
| what is named | `scripts/FarSky.cs` (new). `MayName` holds section 5's rules in one place: a fusion of two spells (`ComboCatalog.Fusions`, never the Rite); never the one the wizard opens on (`StartsFused`); never one naming his own starting spell (`ComboSeal.OwnSpell`'s rule; the Test Wizard has none); both parents dealable from the save - owned, not set aside, not another wizard's alone (`UnlockCatalog.MayBeOffered`). `PickForNight` weighs a recipe never made **3** to a made one's **1**, and never names the same recipe two nights running while there is another. `Tonight` adds "learned"; `ForRun` adds "on the far shore" and the tools' `ToolOverride` |
| the night | **the save's run count** (`FarSky.Night` = `LifetimeStats.TotalRuns`). The pick is a hash of the night (SplitMix32's finaliser, not Godot's RNG), walked from night 0 so each night can exclude the one before. So the sky turns after every run - won, lost or quit, far shore or road - and the card, the run and the pause screen name the same recipe with nothing stored |
| the favour | `scripts/Player.FarSky.cs` (new), three hooks. `TakeFarSkyFavoured` in `GetLevelUpOptions`: a parent's new-spell card when a slot is free, and a held parent's upgrade card always, are taken out of both pools and dealt first - ahead of the roll that decides whether held upgrades are in the hand, so it is `UpgradeOfferChance` 1 for those two and more (in the hand, not only in the pool); a boon dealt in never replaces one; bans and set-aside spells are obeyed. `FavourNamedAscension` in `GetAscensionOffers`: the named Ascension first on the parent's level 8 page, under the linked rings it carries. `NoteFarSkyFusion` in `TryFuse`. `FarSkyFavourHolds` ends it the moment the recipe is made or sealed (a parent down its other path, a fourth element, the Rite). It changes no gate: player level 17, four of the named path's element, both parents at 8 |
| the run | `scripts/Node2DGame.FarSky.cs` (new): `StartFarSky` (one marked line at the end of `_Ready`), the pause screen's **The Far Sky** section on the Run tab after Run Summary (one marked line in `ShowEscapeRunOverview`): tonight's recipe, then made / sealed / "Its two spells come to your hand first. Make it before the run ends." |
| the payout | **100 Arcane Energy** every run that makes it (`FarSky.ArcaneBonus`; one marked line in `CalculateArcaneReward`), flat after every multiplier as a boss's bonus is, its own line on the results' breakdown ("Tonight's Conjunction made - Shadow Bolt", `ArcaneRewardBreakdown.FarSkyBonus`). `RunResult.FarSkyRecipeId` / `FarSkyMade` |
| the Feats | `AchievementDefinitions`, F0's two F4 slots: **Under the Far Sky** (Feat, 300; met mid-run by the achievement watch, which reads `RunResult.FarSkyMade` on its one-second snapshot) and **Every Conjunction Known** (Collection, 600; every fusion's own Feat counted live against `Fusions`, so F6-F10's recipes raise the target). Both under a new `AchievementVisibility.FarSky` - listed once the far sky is learned (6.2: no row of the far shore before) - which `ValidateAchievementShape` lets wait, as a recipe's Feat waits |
| the card | `WorldMap.RebuildCard`: **one marked line** (`FarSky.CardLine`, the `conjunction` glyph, the place ink) on every far-shore stop, open or not: **"Tonight: The Taking"** - the recipe's name alone. Section 5's example line ("Tonight: Maelstrom - Riptide (Tidal Wall) and Cyclone Slash (Vortex Reaper)") was built first and photographed on F3's and F5's White Downs: the card has one line to spare there (four rewards, a fallen warden, a longest stand), and the full line wrapped to three on a PC and two on a phone and pushed Enter off the foot of the screen on both. The pause screen names it in full; `ValidateFarSky` holds every recipe's card line to 30 characters |
| the lesson | `Teachings`: `Teaching.FarSky` and `FarSkyLesson` ("far_sky", "The Far Sky", "Each night the far sky names a Conjunction"), owed when `FarSkyTeacherId` - the god seated at the far shore's first chapter (`UnlockCatalog.GodSeatedAt`), Artemis - is the save's; learned when its reel ends, after her rekindling reel (`PlayOwedLessonsThen` already puts freeings first); shown only once learned; `SealedLine` "Learned on the far shore". Its beats in `scripts/Prologue.FarSky.cs` (new; a method, so a partial class's static order cannot bite) and one line after the freeings in `Prologue.BuildReels`. `Player.ItemRules.HasLearnedForRun` asks `Teachings` for any system past the ladder rather than assuming the Rite. A row in `.ai/slow-start.md`'s table of systems |
| the reel | `tools/art/lessons.py`, `p_far_sky_meet` and `p_far_sky_favour` -> `assets/bonelight/ui/lessons/far-sky-{meet,favour}-{back,mid,fore}.png`, 0 off-contract pixels. The White Downs at night under the moon she has put back - the rekindling ground's turf, chalk and flint, seen inland as rolling downs with a chalk track and a long barrow. **1.** Two stars, ice and fire (the Conjunctions lesson's pair), leaning together across the sky: "Each night the far sky names a Conjunction." **2.** Their light falling onto two stones, the linked rings overhead, the player between: "Its two spells come to your hand first." |
| the tools | `_BalanceHarness --far-sky[=ID|off]` and `--stop-when-made`; the summary's `farSky=` fields (`inBook`, `ascended`, `offeredPace`, `madePace` - **pace** minutes, `Node2DGame.PaceSeconds` - and `favoured`); `_UiShot --far-sky=ID|off` |
| the validator | `RegressionChecks.FarSky.cs`, `ValidateFarSky` after `ValidateTeachers` in `RunAll` (below) |

**Measured** (section 5's protocol). `_BalanceHarness --stage=14 --character=8 --seek-combo=<recipe> --all-unlocked
--immortal --timescale=3 --minutes=20`, `--far-sky` (favour on) against `--far-sky=off`; `--stop-when-made` on the later
favoured runs. Laelaps (Cyclone Slash, so no recipe names his spell); the planned bot (`--seek-combo`: rerolls and bans
spent fishing, level 4 paths kept inside three elements, the named Ascension only, the gate's element sought). Every
minute is the run's **pace** minute (15-minute runs, so the clock). Three grounds, in the order they became available:
**the White Downs' stand-ins** while F3 was unmerged (`--stage=14` on the Cursed Dungeon's cast, the Heath's ground,
the castle's difficulty row); **the Court** (`--stage=12`), the castle cross-check the brief asked for; and, once F3
merged into `main-ff86ni` mid-piece, **the White Downs itself** - its cast, its ground, the Unstrung Hunter - measured
in a scratch checkout of this branch merged with the trunk (`git merge-tree`'s tree, never committed; the branch was
restored to its own commits after).

| recipe | ground | bot | seed | favour | parents in the book | named Ascensions taken | **made** | level at 10 / 15 |
|---|---|---|---|---|---|---|---|---|
| Shadow Bolt | **the White Downs** | orbs | 1234 | on | 0.5 / 1.0 | 8.9 / 10.2 | **10.7** (level 23) | 20 / (ended when made) |
| Shadow Bolt | **the White Downs** | orbs | 1234 | off | 0.4 / 8.3 | 12.7 / - (Contagion at level 4 at 20) | not by 20 | 14 / 21 |
| The Taking | **the White Downs** | orbs | 1234 | on | 0.5 / 1.0 | 10.5 / 12.2 | **12.4** (level 24) | 15 / (ended when made) |
| Sagittarius | **the White Downs** | orbs | 1234 | on | 0.5 / 1.0 | - / - (both at 7; Wind 2 and Light 2 of 4, level 15 of 17 at 20) | not by 20 | 10 / 12 |
| Shadow Bolt | Downs (stand-in) | orbs | 1234 | on | 0.6 / 1.1 | 8.7 / 9.9 | **10.3** (level 23) | 22 / 37 |
| Shadow Bolt | Downs (stand-in) | orbs | 1234 | off | 0.5 / 9.6 | 12.8 / - | not by 20 | 13 / 20 |
| Shadow Bolt | Downs (stand-in) | orbs | 7 | on | 0.6 / 1.0 | 12.5 / - (Pandemic: Poison 2 of 4) | not by 20 | 15 / 26 |
| The Taking | Downs (stand-in) | orbs | 1234 | on | 0.5 / 1.1 | 13.7 / - (Pyre: Light 3 of 4) | not by 20 | 11 / 21 |
| The Taking | Downs (stand-in) | orbs | 1234 | off | 0.5 / never | - / - | not by 20 | 10 / 14 |
| Sagittarius | Downs (stand-in) | orbs | 1234 | on | 0.5 / 1.1 | - / - (Hunter's Mark: Wind 2 of 4, level 14 of 17) | not by 20 | 10 / 14 |
| Sagittarius | Downs (stand-in) | circle 180 | 1234 | on | 1.3 / 1.6 | - / - (level 10 of 17) | not by 20 | 8 / 10 |
| Sagittarius | Downs (stand-in) | circle 180 | 1234 | off | 2.0 / 1.6 | - / - | not by 20 | 8 / 10 |
| Shadow Bolt | the Court | orbs | 1234 | on | 0.6 / 1.2 | 9.1 / 10.6 | **10.9** (level 23) | 20 / (ended when made) |
| Shadow Bolt | the Court | orbs | 1234 | off | 0.5 / 12.8 | - / - (level 16 of 17 at 20) | not by 20 | 11 / 14 |
| The Taking | the Court | orbs | 1234 | on | 0.6 / 1.2 | 11.4 / 13.4 (Pyre's Light met at 13.4) | **13.7** (level 24) | 13 / (ended when made) |

The made column is the pace minute the fusion was taken; "not by 20" ran the whole twenty. The three grounds keep much
the same level clock (Shadow Bolt 20-22 at minute 10, The Taking 11-15); the real White Downs pays a little better than
its stand-in for the slower builds (The Taking at level 15 at minute 10 against 11), and The Taking met Pyre's Light
there, as it never did on the stand-in.

What it says:

- **The favour does what it is for.** With it on, both parents were in the book inside the first 1.2 minutes in every
  run; without it, Contagion came at 8.3, 9.6 and 12.8 minutes and Kindled Ward never. The parents then rode to level
  7 on every level-up. The cards were the brake `combo-spells.md` 6 found; with the favour they are not.
- **What holds the recipe now is the Ascension's own gates**, which the favour does not touch and Eric's rule keeps:
  **player level 17** (`Player.AscensionMinimumPlayerLevel`) and **four of the named path's element**
  (`AscensionElementRequirement`). Every miss above is one of them, named in its row. The bot reaches level 17 at about
  minute 8.5-9 with Shadow Bolt's parents (10.7 on the stand-in's seed 7), 10.5-13.7 with The Taking's, and with
  Sagittarius's not inside the run at all (level 12-14 at minute 15, on every ground). The level clock is the build's
  own kill rate: the favour puts two spells to 7 early, which is strong when the two are Void Lance and Contagion in a
  crowd (level 37 at 15 against 20 without, on the stand-in) and weak when they are Hollow Star and Kindled Ward, or
  Hunter's Draw and Starfall walking to orbs.
- **The circle bot is no measure here**: it leaves the XP it does not pass over and stood at level 10 at minute 15 with
  the favour or without, so it never met the level gate. `combo-spells.md` 6 asked for `--circle` for Hunter's Draw's
  draw; making the recipe needs the level clock more than the draw, and the orb bot is the fairer player.

**Decision 10, plainly: no - with the favour alone the planned bot does not meet tonight's recipe by about minute 10,
except when its two parents are themselves a fast-killing build.** On the White Downs itself: Shadow Bolt at 10.7 -
about minute 10, as section 5 hoped; The Taking at 12.4, with two and a half minutes and the boss left; Sagittarius not
at all. Across all ten favoured runs: three met it by about 10, all Shadow Bolt (10.3, 10.7, 10.9); two made it at
12.4 and 13.7, both The Taking; five never made it in twenty minutes (Shadow Bolt on another seed, held by Pandemic's
Poison; The Taking on the stand-in, held by Pyre's Light; Sagittarius on every ground and both bots, held by the level
gate and by Hunter's Mark's Wind and Constellation's Light). Without the favour no run made its recipe at all.

**And a short Muster would not close it.** The XP curve is linear in the cost of a level (`10 + 45 x (level - 1)`), so a
level granted at the start is the cheapest level there is: a Muster of K levels is worth `C(1 + K)` XP later - 4 levels
310 XP, 8 levels 1,340 - where `C(L) = 10(L - 1) + 45(L - 1)(L - 2)/2` is the XP from level 1 to L. In their tenth
minute the slow builds earned 0-1,100 XP and Shadow Bolt's 2,300-3,400. Read off the ledgers (the
`lvByMin` levels: the XP between the level held at minute 10 and the level the recipe was made at, or would be at the
earliest - 19: the gate at 17, two Ascension picks, the fusion):

| run | level at 10 | made at (or would be) | XP short at minute 10 | Muster levels that buy it |
|---|---|---|---|---|
| Shadow Bolt, **the White Downs**, 1234 (made at 10.7) | 20 | 23 | 2,730 | **12** |
| The Taking, **the White Downs**, 1234 (made at 12.4) | 15 | 24 | 7,380 | **19** |
| Sagittarius, **the White Downs**, 1234 (never) | 10 | 19 at the earliest | 5,355 | **16** at the least |
| Shadow Bolt, Downs stand-in, 1234 (made at 10.3) | 22 | 23 | 955 | **7** (it was 0.3 minutes late) |
| Shadow Bolt, the Court, 1234 (made at 10.9) | 20 | 23 | 2,730 | **12** |
| The Taking, the Court, 1234 (made at 13.7) | 13 | 24 | 8,525 | **20** |
| The Taking, Downs stand-in, 1234 (never) | 11 | 19 at the earliest | 4,940 | **16** at the least |
| Sagittarius, Downs stand-in, 1234, orbs (never) | 10 | 19 at the earliest | 5,355 | **16** at the least |

So the Muster that lands the slow recipes by minute 10 is **16 to 20 levels - the Throne's whole Muster (17), not a
few**; a recipe that already lands near 10 (Shadow Bolt) needs none, since it is "about minute 10" already, and even
the 0.7 minutes the White Downs' run was late (0.9 in the Court) would cost 12 levels, because by then that build earns
2,300-3,000 XP a minute. And a Muster buys only the level gate. The element gates (Pyre's Light, Hunter's Mark's Wind,
Constellation's Light, Pandemic's Poison - two of four short in most rows) want picks of the right element, which the
Muster's extra cards could pay for only if something steered them; the favour as section 5 wrote it does not. Two levers that are not
a Muster, for Eric to weigh and not built: count tonight's named Ascensions' element gates as met (the recipe is still
both parents at 8 down both named paths), or let the favour deal one card carrying the named path's element when the
gate is all that is missing. Either changes what "the recipe is unchanged" means, which is why neither was done.

**Decisions F4 made** (the doc left them open):

1. **The night is the save's run count**, so the sky also turns after a road run. Turning "after every far-shore run"
   and only then needs a count of far-shore runs - a save field, a schema step, and a collision with F3 or F5 if either
   bumps; this needs none. Nobody on the road sees the sky.
2. **Never the same recipe two nights running** while there is another ("a new recipe each far-shore run"). With two
   to name they alternate, whatever their weights.
3. **3 : 1** for never made against made. **"Made" is the recipe's own Feat** (`ComboCatalog.IsMet`), the code's one
   record of it - completed the first time the strip offers the recipe, so a save that was offered it and passed it by
   counts as having made it. Every Conjunction Known counts the same thing.
4. **"Always in the hand" is in the hand**, not only in the pool: the parents' cards are dealt first, every hand, the
   new spell's card as well as the upgrades'. `UpgradeOfferChance` 1 for the two alone would still have let the draw
   leave them out of a three-card hand.
5. **The favour ends when the recipe is sealed or made**, and does not steer a parent's level 4 path or its element.
6. **100** Arcane Energy a run that makes it, **300** for Under the Far Sky, **600** for Every Conjunction Known (the
   Grimoire, the nearest Collection, pays 500).
7. **The card line is on every far-shore stop, open or locked** - section 5's "every far-shore stop", read literally -
   **and is the recipe's name alone**, for want of room (the card row above); the full recipe is the pause screen's.
8. **The pause line is its own section** on the Run tab, with where the recipe stands this run.
9. **A new `AchievementVisibility.FarSky`** keeps both Feats off the page until the sky is learned, rather than
   `RequiredCharacterId = "artemis"`, which reads as her challenge.
10. **The lesson's two stars are ice and fire**, the Conjunctions lesson's pair, so the far sky reads as that lesson
    going on; the downs are drawn inland, the rekindling reel's ground being the cliff edge.

**Verified.**

- `dotnet build`: 0 errors; no warning in any file F4 touched.
- The validator (`MainMenu.tscn --quit-after 300 -- --no-save-writes --validate-content`): **0 errors and 1 warning**
  (`test_wizard`). `ValidateFarSky` prints "the far sky over 240 nights for 24 characters on 4 saves - 23040 nights
  named, 3 of 3 fusions named at all, 0 broken rules" and, for the weighting, "'pyromancer' over 600 nights with
  'sagittarius' made - sagittarius 116, shadow_bolt 243, the_taking 241". It checks, on a save with every spell, one
  without a parent, one with a parent set aside and one that has made everything, for every character: never a recipe
  he opens on, never one naming his own spell, never one he cannot be dealt, the same night the same recipe, never two
  nights running; the weighting; nothing before the sky is learned, nothing off the far shore, the tool override; the
  lesson owed by Artemis rekindled and by no other god, learned once taught; both Feats hidden until learned, Under the
  Far Sky met by a run that made tonight's and not by one that did not, Every Conjunction Known met at every fusion and
  not one short. `ValidateTeachers` holds the reel's panels and its 46-character lines.
- **Shown to fire**, in a scratch build that was not committed: openers allowed, the own spell allowed, ownership not
  checked, the weights turned round and repeats allowed - 12,830 broken-rule lines across every kind (585 openers,
  1,301 own spells, 1,992 unmakeable parents, 8,952 repeats), the weighting's two, and the starters-only save's.
- `_UiShot` (`user://`, looked at): `f4-reel-meet-{pc,phone}` and `f4-reel-favour-{pc,phone}` (`--scene=res://scenes/
  Prologue.tscn --reel=far_sky --fixed-fps 60`, frames 200 and 520; the moon moved inside the phone's columns after the
  first phone shot cut it in half); `f4-pause-{pc,phone}` (`--scene=res://scenes/node_2d_game.tscn --stage=14
  --character=8 --all-unlocked --open-pause --scroll=150/120`: "Tonight: Sagittarius - Hunter's Draw (Hunter's Mark)
  and Starfall (Constellation)" and its standing line, both canvases - on the stand-in ground, the line being the same
  code on the real one); `f4-map-card-real-{pc,phone}` (`--scene=res://scenes/WorldMap.tscn --all-unlocked
  --character=8 --map-at=stage_14`, **in the scratch checkout of this branch merged with the trunk**, on F5's map and
  F3's chapter): "Tonight: The Taking" under the longest stand, above What he keeps here, Enter whole on both canvases.
  Before that, `f4-map-card-{pc,phone}` (a scratch with the White Downs flipped playable and a stop added to
  `worldmap.json`, before F5's map existed) and the first `f4-map-card-real-pc` showed the full line wrapping into Enter.
- `git merge-tree` against `main-ff86ni` after F5's and F3's merges: the code is clean (once the far sky's beats left
  `Prologue.cs` for `Prologue.FarSky.cs` - both pieces had added their beats beside `RiteBeats`). **This file is not**:
  the trunk changed the same places (the status line, the F3 and F5 rows either side of F4's, the end of the file). F4's
  copy was written on the trunk's, with F3's and F5's notes in it, and differs from it only by F4's lines - so the
  merge takes F4's side of `.ai/second-half.md` whole, unless the trunk's copy has moved again since (2026-11-08).

**Not run, and needs a person's eyes.** The far sky has never been taught, named on a map stop or made by a person:
Artemis is rekindled by F3's warden, and nobody has played that run with F4 in. The measurement's real-chapter rows
ran in a scratch merge; after the real merge it re-runs as written (`--stage=14 ... --far-sky` / `--far-sky=off`),
nothing to change. Whether the favour
reads as an invitation or a chore, whether two parents' cards in every hand crowd out the rest of a build (the bot
leaned into it; a player may not), whether 100 Arcane Energy is worth chasing, whether a player who passed tonight's
recipe by should be named it again, and whether the reel's two stars read as two spells rather than two stars. The
results-screen line and the Under the Far Sky moment were never photographed: no run on this branch made a recipe with
the screen on.
