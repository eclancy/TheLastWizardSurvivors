# Combination spells: two spells made into one

Eric, 2026-11-01: *"Let's begin adding some spell combinations. Hunter's Draw plus Constellation can be
combined to Sagittarius, a new spell that combines the abilities of both into an even more powerful
spell. Let's start making plans and theory craft some good, thematic combo spells."*

**Status: a proposal. Nothing here is built or decided.** There is no spell called Constellation: it is
one of **Starfall's** two level 8 Ascensions (the stars stay lit where they fell, joined by lines of light
that cut; `Starfall.ConstellationId`). So Sagittarius is read here as **Hunter's Draw + Starfall**, with
the question of whether it needs Constellation laid out in section 3.

Read first: `.ai/spell-roster.md` section 2 (aim, cadence, demand, payload) and section 1 (the three
collisions), `.ai/spell-variety.md` section 6 (what to take from the genre), `.ai/gameplay-design.md`
(element tiers, attunement, the two-element ceiling), `.ai/decisions/2026-10-10-apotheosis.md`.

---

## 0. What Eric must decide first

| # | question | recommended | the main alternative |
|---|---|---|---|
| 1 | where a combination is offered | **the Ascension strip** above the level-up cards, where the Apotheosis already appears | a relic chest; a world altar |
| 2 | does it free a slot | **yes**: one spell in two slots becomes one spell in one | it fills both, and must be stronger to pay for that |
| 3 | element tags | **the combination carries both parents' weights exactly**, and may name up to three elements: the one exception to the two-element ceiling | only pairs whose elements fit in two (rules out Sagittarius) |
| 4 | can a wizard fuse his own starting spell | **no**: its last form is the Apotheosis | yes, and fusing it gives up the Apotheosis (the card says so) |
| 5 | how a save learns a recipe | **a Feat derived from the recipe**, completed and paid the first time the recipe is met in a run | a recipe found in the world (a star chart, a forge) per combination |
| 6 | Sagittarius needs Constellation | **yes** | any Starfall at level 8 |

---

## 1. How the genre does it (mechanics only, in our words)

| pattern | how it works | gates | what it does to slots |
|---|---|---|---|
| **key and weapon** | a weapon at max, plus one particular passive item held, becomes its evolved form | three: max level, the item, a boss chest | none; the item stays |
| **union** | two particular weapons, both at max, fuse into one | two: both at max, then a pick | frees one |
| **merge** | two copies of one weapon at the same tier become the next tier | owning the copies | frees one |
| **special pick** | when two particular weapons are both at max, the level-up offers their fusion as a card | one | frees one |

We already have the first one's lesson in `spell-variety.md` section 6: *"ours should be one gate"*. Our
items are not weapons' keys, and a boss-chest gate would put the reward behind a fight that comes once
a run. So: **a union, offered as a special pick in the strip.**

---

## 2. The system (recommended)

**When it appears.** In the Ascension strip, on every level-up while both parents are in the loadout at
level 8 (so both have their Ascension), and any Ascension the recipe names was the one taken. Taking it
spends the pick, as the Apotheosis does. It never appears on the ordinary cards, where it would compete
with picks and be dealt away.

**What happens.** Both parents leave the loadout. The combination takes the first parent's slot; the
second slot is empty. It is **terminal**: no levels, no milestones, never offered again, never fused
again. Neither parent returns to the level-up pool that run. A spell is in one fusion at a time; a
spell may appear in several recipes with different partners.

**Its numbers.** Read once from both parents at the moment of fusion (their levels, cooldowns, area,
and which Ascension each took) and frozen, times the combination's own multipliers. Target: about
**1.2 times the two parents' combined damage**, plus the freed slot. As the Apotheosis puts it, the
behaviour is the reward, not a bigger number. A combination may read a parent's Ascension for one small
switch (Sagittarius reads Hunter's Mark), never for a whole second design.

**Element tags.** The combination carries the **sum of both parents' current weights**, evolution gains
included. So fusing can never move an element tier, in either direction, and can never drop a wizard out
of the tier his Apotheosis needs (the ADR's rug-pull trap). It is the one spell allowed to name three
elements: the two-element ceiling protects the pure-or-hybrid trade at levels 4 and 8, and a combination
is past both. Recipes are chosen so none names four on their base tags. Tags are counted as they stand
at fusion, though, and a pure parent that branched at level 4 brings its second element (Starfall's
Frostfall brings Ice, which would make Sagittarius four): **recommended, the strip does not offer a
fusion that would name four**, and the browser's row says why. It names two or more, so it is never
attuned; the 1.2 target already counts the attunement a pure parent loses.

**The Apotheosis.** A wizard's own starting spell is never an ingredient for him: its final form is his
Apotheosis. Any other wizard may fuse it. (The alternative, a choice between the two in the strip, is
richer and harder to read; worth trying only once both exist.)

**The economy.** A combination needs two Ascended spells that are not the wizard's own, sixteen
picks (`gameplay-design.md`: eight per Ascension). Runs were measured at four or five Ascensions when
planned, so at most one or two combinations a run, late; the harness says how late. The freed slot is
then filled from level 1. Measure with `_BalanceHarness --seek-combo=<id> --all-unlocked --immortal` (a new flag, like
`--seek-apotheosis`); Hunter's Draw's recipes need `--circle`, as Hunter's Draw does.

**Unlocks and the save.** Eric's rule is that every non-starter spell comes from exactly one
achievement. Each recipe derives one **Feat**, *"Conjunction: Sagittarius"*, completed through
`AchievementManager.CompleteNow` the first time both parents meet the recipe in a run, which grants the
combination there and then, so the strip offers it on that same level-up. Nothing is gated that the
player has not already built. In the Achievements list a recipe is a hidden row until the save owns
both parents, then shows the two parents and a sealed result, and the name once made. The Ascensions
browser lists made recipes. **No schema bump:** the deeds are derived and their completion is stored as
every deed's is; "owns both parents" is derived from the save.

**What `dotnet build` cannot see**, held by a new `RegressionChecks.ValidateComboCatalog`: both parents
exist and are distinct; no pair has two recipes; no combination names more than three elements; every
verb is unique across combinations and Apotheoses; every recipe's Ascension id exists on its parent;
every combination has its derived deed, its `ContentValidator.SpellResourcePaths` entry and an
`EvolutionStatReaders` row.

**Rules every combination inherits.** Bosses are immune to anything that moves, holds or turns them
(`in-flight.md` 3a item 4). Per-frame work walks `EnemyRegistry.Snapshot` and allocates nothing.
Ground effects on `DrawLayers.Ground`, what flies on `Air`. The animation is the hit area.

**Rejected.**

- *A relic chest offers it.* A chest is luck and comes a few times a run; the strip already guarantees
  the Apotheosis is never skipped, and should guarantee this.
- *A world altar per chapter.* Content per chapter, and a recipe that only works in one place.
- *An ordinary level-up card.* Competes with picks, can be dealt away, and looks like any other card.
- *Automatic at max.* No decision; the player may want both spells.

---

## 3. Sagittarius - Hunter's Draw + Starfall

**The parents.** Hunter's Draw (Metal+Wind): the bow draws while he moves and looses a volley at the
nearest when he stops. Starfall (Light): weather; small stars fall at random over a circle round him, aimed
at nothing. Constellation, its level 8: every star stays lit 2.5 s, joined to the nearest lit star within
140 px by a line that cuts every 0.5 s.

| | Sagittarius |
|---|---|
| **aim** | **from the ground he has walked, inward.** Every lit star is an archer; each looses at the enemy nearest the wizard, so the arrows come in from every side of the crowd |
| **cadence** | the rain falls as always; while he moves, every star that lands **stays lit for as long as the bow is drawn** (up to 16, the oldest going out first); when he stops, the loose |
| **demand** | walk the rain round the crowd, then stop. Moving pays twice: it lights the stars (how many arrows) and draws the bow (how hard each one hits, Hunter's Draw's full draw) |
| **payload** | a converging volley: one piercing arrow from each lit star, crossing the crowd on its way in; the lines between the stars still cut while he walks |
| **look** | the lit stars as white-gold points on the ground, joined by Constellation's lines; as the draw fills, a thin arc of light pulls taut between the two stars furthest apart; on the loose every star flares, a gold arrow with a white head streaks inward from each, the lines flash once and go out. No new sheet: drawn from Starfall's star arrays and Hunter's Draw's arrow |
| **sound** | a bowstring and a glass chord (Light's voice), one per loose, not per arrow |
| **elements** | Hunter's Draw's and Starfall's weights summed: for example Metal 2, Wind 2, Light 1 after Heavy Draw and Rain of Arrows |

**More than the sum.** Neither parent fires from anywhere but the wizard or the sky. Sagittarius makes
his path the aim: a loop round the pack puts archers on every side of it, and the arrows cross it
inward, piercing as they go. The net of lines cuts while he walks and the arrows land when he stops, so
the two halves of a kite (moving and stopping) both pay. Hunter's Draw alone shoots one way; Starfall
alone mostly hits nothing; this is crossfire.

**The Constellation question.**

| option | what it means | for | against |
|---|---|---|---|
| (a) any Starfall at 8 | the stars stay lit through the draw whatever its Ascension | easier to reach | with Starburst there are no lines and no constellation: the name means nothing |
| **(b) Constellation required** | the recipe names it | the lit stars and the lines **are** the archer; it is Eric's word | one fewer way to reach it |
| (c) both, two forms | with Starburst each arrow bursts where it ends | more to find | a second design to build and tune |

Recommended (b), with (c) as a later addition if Sagittarius plays well.

**Hunter's Draw's Ascension** is read for one switch: with **Hunter's Mark**, every star looses at the
marked quarry (an elite killer); with **Rain of Arrows**, at the enemy nearest the wizard (a crowd
spell).

**First-guess numbers.** Each arrow is Hunter's Draw's level 8 arrow at 0.8, pierce 3, scaled by the
draw as it is today; up to 16 arrows; lines at Constellation's 55% every 0.5 s. Tune to 1.2 times the two
parents' damage on the circle bot.

**Card:** *"Stars that fall while you walk stay lit and join into an archer. Stop, and every lit star
looses an arrow at the enemy nearest you."* **Tagline:** *"Walk the stars. Stop. Loose."*

**Cost: M.** `Sagittarius.cs` holds Starfall's star arrays with a lifetime set by the draw state and
reads `Player.TickHuntersDraw`'s meter; arrows on `ElementalBolt`. One icon.

---

## 4. Nine more

Each is a pair whose fusion makes a **new verb**. None of these verbs is an Apotheosis's (burn, stop,
link, hold, flood, swarm, pull, reflect, rewind, nor the ten `.ai/chapter-wizards.md` proposes). Every
pair names three elements at most, and between them they touch all twelve. Names were checked against
the codebase: Eclipse (Hollow Star's Light path), Prism and Lodestone (chest items) and Whiteout (the
Still Warden) are taken, so they are not used.

### Orrery - Solar Flare + Hollow Star (orbit)

- **Aim / cadence / demand / payload:** self / grows while he stands still, as Solar Flare / **hold still
  to gather, step to release** / enemies held circling.
- While he stands still, each pulse catches what is within the well's reach into **orbit**: held at the
  flare's rim, wheeling round him, unable to close, burnt each time the pulse passes. When he steps, the
  orbit falls in on itself to one point before him, Hollow Star's collapse.
- **Look:** a pale sun with a black core; enemies wheel on faint orbit lines; the collapse a black flash.
- **More than the sum:** Solar Flare cannot keep anything off him; Hollow Star pulls to a point he does not
  choose. This keeps the crowd at a fixed distance until he decides.
- Light, Fire, Darkness. Elites slowed, not held.
- **Card:** *"While you stand still, the sun draws what is near into orbit round its rim. Step away and
  the orbit falls in."* **Tagline:** *"Hold still. They circle."*

### Maelstrom - Riptide + Cyclone Slash (fling)

- **Aim / cadence / demand / payload:** the way he runs / continuous while running / **run so the throw
  lands in the crowd** / enemies thrown as projectiles.
- The whirlwind takes on water. What it touches is caught, spun once round him and **flung** out along
  the way he is running; a body that lands among others strikes them, by its weight.
- **More than the sum:** neither parent uses the enemy as the weapon.
- Water, Wind. Elites and bosses are struck, not thrown.
- **Card:** *"The whirlwind catches what it touches, spins it once round you and throws it the way you
  run."* **Tagline:** *"They are the ammunition."*

### Hailfront - Stormsweep + Sleet Runes (advance)

- **Aim / cadence / demand / payload:** facing / timer / **face where you want ground, then follow** / a
  moving wall.
- Each cast raises a front of sleet and lightning across the way he faces, which **walks out** at
  walking pace for 2 s, shoving and cutting what it meets. Fronts cast the same way stack.
- **More than the sum:** both parents cover a side; this clears a lane. The escape spell, and the one
  the Last Bridge is built for.
- Lightning, Ice, Wind. Bosses are cut, not shoved.
- **Card:** *"A front of sleet and lightning walks out the way you face, shoving and cutting what it
  meets."* **Tagline:** *"Face it. Walk in behind."*

### Dark Lens - Scorching Ray + Void Lance (refract)

- **Aim / cadence / demand / payload:** the ray's targets, through the lance / timer / **stand so the
  lance lies between you and the crowd** / one beam split into three.
- Where the ray crosses the slowly turning lance, it **refracts** into three thinner rays, each ramping
  on its own target; the crossing burns hottest.
- **More than the sum:** Scorching Ray holds one target; Void Lance aims at nothing. Together the lance
  becomes a lens he positions.
- Fire, Arcane, Darkness.
- **Card:** *"Where the ray crosses the lance it splits in three, each ramping on its own target."*
  **Tagline:** *"Put the lance between you."*

### Pitfall - Meteor Swarm + Gravewell (funnel)

- **Aim / cadence / demand / payload:** where his wells are tripped / the barrage, on a trip / **let
  them come to the wells** / concentrated barrage on held knots.
- Meteors no longer fall on a random enemy: each is **funnelled** into a tripped well, onto whatever its
  root is holding.
- **More than the sum:** the barrage gains an aim the player sets with his feet.
- Fire, Earth, Darkness.
- **Card:** *"Your meteors fall into your tripped wells, onto whatever the wells are holding."*
  **Tagline:** *"Set the ground. Call the sky."*

### Reprisal - Aegis Ward + Spiritual Weapon (answer)

- **Aim / cadence / demand / payload:** **whatever last hit you** / **on being hit** / **be hit while
  the shield holds** / a blow from above.
- Every hit the shield takes is **answered** at once: the weapon falls on whoever struck it, the
  shooter of a bolt included. When the shield breaks, it falls on everything in reach.
- **More than the sum:** Aegis deals nothing; the Weapon picks by its form. Two axes the roster lacks
  (`spell-variety.md` section 2) in one spell: the target is whoever hurt you, the cadence is being hurt.
  Not Laelaps's *reflect*, which returns the bolt itself.
- Metal, Light, Arcane.
- **Card:** *"Every hit your shield takes brings the weapon down on whoever struck it. When the shield
  breaks, it falls on everything near."* **Tagline:** *"Strike me. See what answers."*

### Anvil Line - Molten Shard + Iron Palisade (ricochet)

- **Aim / cadence / demand / payload:** the palisade / timer / **stand so the line backs the pack** /
  ricochet, stake to stake.
- Shards are fired at the stakes, not at enemies, and **ricochet** off into the pack, stake to stake
  along the line until spent, withering each body they pass.
- **More than the sum:** ricochet is a payload the genre has and we do not (`spell-variety.md`).
- Metal, Fire.
- **Card:** *"Shards strike the palisade and glance into the pack, stake to stake, withering all they
  pass."* **Tagline:** *"Every stake an anvil."*

### Burning Briar - Thorned Vine + Cinderbreath (fuse)

- **Aim / cadence / demand / payload:** the densest pack, lit from where he stands / timer, then the
  flame / **lay the lines, bring the flame to one end** / fire that runs along laid lines.
- Every sweep leaves its vine lying where it passed, a **fuse**. When the flame touches any of it, fire
  runs its whole length, burning everything it lies under.
- **More than the sum:** Cinderbreath reaches only close; the fuse carries it across the field.
  Distinct from *burn*, which spreads by kills; this spreads by geometry the player laid.
- Grass, Poison, Fire.
- **Card:** *"The vine leaves itself lying where it swept. Touch any of it with flame and the fire runs
  its length."* **Tagline:** *"Lay the fuse. Light one end."*

### Black Ice - Mirefoot + Cone of Cold (slip)

- **Aim / cadence / demand / payload:** behind him, then the way he faces / distance travelled, then
  the cone / **lay a trail, turn and freeze it as they follow** / loss of footing.
- The cone freezes the pools behind him into black ice. Whatever runs onto it **slips**: it slides on
  the way it was going until it leaves the ice, unable to turn, shoving what it meets.
- **More than the sum:** a new kind of control: they are not stopped, they cannot steer.
- Water, Grass, Ice. Bosses do not slip. Build after `wip/enemy-pathing`, which owns enemy movement.
- **Card:** *"Your cold freezes the pools behind you. Whatever runs onto the ice slides on and cannot
  turn."* **Tagline:** *"They follow you onto the ice."*

---

## 5. Ranked by value against cost

| rank | combination | pair | verb | value | cost | why here |
|---|---|---|---|---|---|---|
| 1 | **Sagittarius** | Hunter's Draw + Starfall (Constellation) | converge | high | M | Eric's; both parents' code reused |
| 2 | **Reprisal** | Aegis Ward + Spiritual Weapon | answer | high | **S** | two missing axes at once; a shield build becomes an offence |
| 3 | **Pitfall** | Meteor Swarm + Gravewell | funnel | medium | **S** | a retarget of meteors to trap positions |
| 4 | **Orrery** | Solar Flare + Hollow Star | orbit | high | M | scripted steering exists (`AdjustSteering`) |
| 5 | **Maelstrom** | Riptide + Cyclone Slash | fling | high | M-L | an enemy as a projectile is new |
| 6 | **Hailfront** | Stormsweep + Sleet Runes | advance | medium-high | M | the travel spell for the Last Bridge |
| 7 | **Black Ice** | Mirefoot + Cone of Cold | slip | medium-high | M | waits on the pathing branch |
| 8 | **Burning Briar** | Thorned Vine + Cinderbreath | fuse | medium | M | the vine's path is already drawn |
| 9 | **Anvil Line** | Molten Shard + Iron Palisade | ricochet | medium | M | bounce geometry off moving stakes |
| 10 | **Dark Lens** | Scorching Ray + Void Lance | refract | medium | M | beam crossing and three targets |

**Build order:** the system first (the strip offer, the fusion, the derived Feat, the validator, the
harness flag), proved by **Sagittarius**; then Reprisal and Pitfall, the two cheap ones; then the rest by
rank. Each verifies like an Ascension: `dotnet build`, the validator run with `-- --no-save-writes`, the
harness band (damage a second against the two parents' sum), `_UiShot` of the strip card and of the spell
in a crowd at both canvases. **What no harness checks:** whether a combination reads at a glance as its
two parents, and whether a player notices the strip offering it.
