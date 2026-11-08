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
(2026-11-08, section 9, "F0, built"), and F1, the god plumbing (2026-11-08, "F1, built"): a god can be added as
data; nothing playable yet.** Section 8 is the list of decisions that are his;
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
first and alone (section 9).

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
| F2 | **Artemis**: Sagittarius's table path, The Hunter's Moon, Far-Shooting, her sheet (`tools/art/player_*.py`, 48 cell, idle / move / hurt / death), icons, reel layer, catalog rows | M | F1 | the first god, playable everywhere |
| F3 | **The White Downs**: chalk tiles and barrows, its cast, the Unstrung Hunter, music, foliage, one side event, two secrets, its difficulty row | L | F0 | the first far chapter |
| F4 | **The far sky**: `Teaching.FarSky` and its reel, tonight's recipe, the favour, card and pause lines, the Feat, `_BalanceHarness --far-sky` and the section 5 measurement | M | F0, F2 | the half's system, measured |
| F5 | **Reel E and the map**: `world_map.py` widened east, the crossing, the white cliffs and six places, the motes after Taken Back, the stop order; reel E | L | F0 | the half on the map, and announced |
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
