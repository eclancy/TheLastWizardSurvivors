# Combination spells: two spells made into one

Eric, 2026-11-01: *"Let's begin adding some spell combinations. Hunter's Draw plus Constellation can be
combined to Sagittarius, a new spell that combines the abilities of both into an even more powerful
spell. Let's start making plans and theory craft some good, thematic combo spells."* And, having read
the first draft: *"combination spells should require two fully ascended other spells, using specific
ascensions."*

**Status: sections 0, 2 and 3 are built (2026-11-01, `wip/combo-sagittarius`) - the system and
Sagittarius; section 6 says how. Section 7 is built too (2026-11-07): Shadow Bolt, once an ordinary
chain spell, converted whole into the third fusion recipe (in-flight item 17). Section 8 is built
(2026-11-08): the Sovereign Rite, rebuilt as a recipe of two spells. Section 4's nine are
still proposals.** There is no spell called
Constellation: it is one of **Starfall's** two level 8 Ascensions (the stars stay lit where they fell,
joined by lines of light that cut). So Sagittarius is **Hunter's Draw + Starfall**, and its recipe names
Constellation.

Read first: `.ai/spell-roster.md` section 2 (aim, cadence, demand, payload) and section 1 (the three
collisions), `.ai/spell-variety.md` section 6 (what to take from the genre), `.ai/gameplay-design.md`
(element tiers, attunement, the two-element ceiling), `.ai/decisions/2026-10-10-apotheosis.md`.

---

## 0. Decisions

**Approved by Eric, 2026-11-01** ("All else approved"): the plan as recommended, with the rule below.

**Decided by Eric, 2026-11-01: every recipe names one specific Ascension on each parent.** Both parents
must be fully ascended (level 8) and must have taken those two Ascensions. A recipe never accepts "any
Ascension". Each recipe below names its two by display name and id, from `SpellEvolutionCatalog`.

Still open:

| # | question | recommended | the main alternative |
|---|---|---|---|
| 1 | where a combination is offered | **the Ascension strip** above the level-up cards, where the Apotheosis already appears | a relic chest; a world altar |
| 2 | does it free a slot | **yes**: one spell in two slots becomes one spell in one | it fills both, and must be stronger to pay for that |
| 3 | element tags | **the combination carries both parents' weights exactly**, and may name up to three elements: the one exception to the two-element ceiling | only pairs whose elements fit in two (rules out Sagittarius) |
| 4 | can a wizard fuse his own starting spell | **no**: its last form is the Apotheosis. Eric's "two fully ascended *other* spells" reads as this; confirm | yes, and fusing it gives up the Apotheosis (the card says so) |
| 5 | how a save learns a recipe | **a Feat derived from the recipe**, completed and paid the first time the recipe is met in a run | a recipe found in the world (a star chart, a forge) per combination |

---

## 1. How the genre does it (mechanics only, in our words)

| pattern | how it works | gates | what it does to slots |
|---|---|---|---|
| **key and weapon** | a weapon at max, plus one particular passive item held, becomes its evolved form | three: max level, the item, a boss chest | none; the item stays |
| **union** | two particular weapons, both at max, fuse into one | two: both at max, then a pick | frees one |
| **merge** | two copies of one weapon at the same tier become the next tier | owning the copies | frees one |
| **special pick** | when two particular weapons are both at max, the level-up offers their fusion as a card | one | frees one |

We take the union, offered as a special pick in the strip. Eric's rule adds what none of the four has:
the fusion depends on the **forms** the two weapons took, so the same pair can fuse in one run and not
the next, and the player who planned for it knows why.

---

## 2. The system (recommended)

**When it appears.** In the Ascension strip, on every level-up while both parents are in the loadout at
level 8 **with the two Ascensions the recipe names**. Taking it spends the pick, as the Apotheosis does.
It never appears on the ordinary cards. If either parent took its other Ascension, **the strip never
offers the recipe that run**: an Ascension is permanent, so the recipe is sealed until the next run.

**What the browser shows.** The Ascensions browser (`LevelUpMenu.Ascensions.cs`) lists every recipe the
save knows (below) as *"Hunter's Draw - Hunter's Mark + Starfall - Constellation"*, with each named
Ascension marked when this run holds it, and the row reads **"sealed this run"** once a parent held in
the loadout has taken the other one. On the level 8 screen, an Ascension card that a known recipe names
carries a small mark (two linked rings), so the choice that opens or closes a recipe is made knowingly.

**What happens.** Both parents leave the loadout. The combination takes the first parent's slot; the
second slot is empty. It is **terminal**: no levels, no milestones, never offered again, never fused
again. Neither parent returns to the level-up pool that run. A spell is in one fusion at a time; a
spell may appear in several recipes, with different partners or different Ascensions.

**Its numbers.** Read once from both parents at the moment of fusion (levels, cooldowns, area, and the
named Ascensions' own numbers, which the combination's behaviour is built from) and frozen, times the
combination's own multipliers. Target: about **1.2 times the two parents' combined damage**, plus the
freed slot. As the Apotheosis puts it, the behaviour is the reward, not a bigger number.

**Element tags.** The combination carries the **sum of both parents' current weights**, evolution gains
included. So fusing can never move an element tier, in either direction, and can never drop a wizard out
of the tier his Apotheosis needs (the ADR's rug-pull trap). It is the one spell allowed to name three
elements: the two-element ceiling protects the pure-or-hybrid trade at levels 4 and 8, and a combination
is past both. Every recipe below names at most three with its named Ascensions. A pure parent's level 4
**element path** can still bring a fourth (Starfall's Frostfall brings Ice); **the strip does not offer a
fusion that would name four**, and the browser's row says why. Each recipe lists the path that does it.
A combination names two or more elements, so it is never attuned; the 1.2 target already counts the
attunement a pure parent loses.

**The Apotheosis.** A wizard's own starting spell is never an ingredient for him: its final form is his
Apotheosis. Any other wizard may fuse it. A recipe that names a wizard's own spell (Tidal Wall on Tituba's
Riptide, Black Sun on John Dee's Hollow Star, Dragon's Breath on the Pyromancer's Cinderbreath) is simply
not his to make. Since 2026-11-02 the Apotheosis itself asks that the spell was "not combined with another
spell" (`ApotheosisCatalog.SpellMeetsGate`), so this rule is what keeps every wizard's form reachable.

**The economy.** A combination needs two Ascended spells that are not the wizard's own: sixteen picks
(eight per Ascension), and two forms chosen for it. At `XpCostPerLevel` 45 the planned bot
(`gameplay-design.md`, "How many Ascensions a run gets") took its Ascensions at about 5.5-6.6, 6.7-7.7,
9.7-12.6 and, for two wizards in six, 14.5 minutes, the first usually its starting spell's. So **a
planned run reaches one recipe at about minute 10 to 13**, with two or three minutes and the boss left to
use it; a second is out of reach. An ordinary run meets one rarely: its second and third Ascensions come
at 8.5-14.9 minutes, and taken blind, two named forms match about one time in four. That is the rule's
intent: a combination is a plan, not a drop. Measure with `_BalanceHarness --seek-combo=<id>
--all-unlocked --immortal` (a new flag, like `--seek-apotheosis`, that steers both parents to their
named Ascensions); Hunter's Draw's recipe needs `--circle`, as Hunter's Draw does. If minute 13 proves too
late to enjoy, that is a pacing question for `.ai/run-pacing.md`, not a reason to loosen the recipe.

**Unlocks and the save.** Eric's rule is that every non-starter spell comes from exactly one
achievement. Each recipe derives one **Feat**, *"Conjunction: Sagittarius"*, completed through
`AchievementManager.CompleteNow` the first time both parents meet the recipe in a run, which grants the
combination there and then, so the strip offers it on that same level-up. Nothing is gated that the
player has not already built. In the Achievements list a recipe is a hidden row until the save owns
both parents; then it shows the two parents with their named Ascensions and a sealed result, and the
name once made. **No schema bump:** the deeds are derived and their completion is stored as every
deed's is; "owns both parents" is derived from the save.

**What `dotnet build` cannot see**, held by a new `RegressionChecks.ValidateComboCatalog`: both parents
exist and are distinct; each names exactly one Ascension, and that id is one of the parent's level 8
options (for a spell taken in forms, the named form's); no two recipes share a pair and both
Ascensions; no recipe names more than three elements with its named Ascensions; every verb is unique
across combinations and Apotheoses; every combination has its derived deed, its
`ContentValidator.SpellResourcePaths` entry and an `EvolutionStatReaders` row.

**Rules every combination inherits.** Bosses are immune to anything that moves, holds or turns them
(`in-flight.md` 3a item 4). Per-frame work walks `EnemyRegistry.Snapshot` and allocates nothing.
Ground effects on `DrawLayers.Ground`, what flies on `Air`. The animation is the hit area.

**Rejected.**

- *Any Ascension.* Decided against (section 0).
- *A relic chest offers it.* A chest is luck and comes a few times a run; the strip already guarantees
  the Apotheosis is never skipped, and should guarantee this.
- *A world altar per chapter.* Content per chapter, and a recipe that only works in one place.
- *An ordinary level-up card.* Competes with picks, can be dealt away, and looks like any other card.
- *Automatic at max.* No decision; the player may want both spells.

---

## 3. Sagittarius - Hunter's Draw + Starfall

**Needs:** Hunter's Draw at level 8 with **Hunter's Mark** (`hunters_draw_infinite_horizon`, no element)
and Starfall at level 8 with **Constellation** (`starfall_constellation`, no element).

**Why those two.** Constellation is the lit stars and the lines: the archer's figure, and Eric's word.
Hunter's Mark is the volley that **seeks one quarry**: a full draw marks the strongest enemy in reach (an
elite first), it takes more from everything, and every arrow homes on it. Put together, every star
becomes a bow aimed at the mark, and the verb is **converge**. With Starburst there are no lit stars and
no lines; with Rain of Arrows the volley falls from the sky, not from the stars. Either seals the recipe.

| | Sagittarius |
|---|---|
| **aim** | **from the ground he has walked, inward, on the mark.** A full draw marks the strongest enemy in reach, as Hunter's Mark does; every lit star looses at it, so the arrows come in from every side and pierce whatever stands between. With nothing marked, the enemy nearest the wizard |
| **cadence** | the rain falls as always; while he moves, every star that lands **stays lit for as long as the bow is drawn** (up to 16, the oldest going out first); when he stops, the loose |
| **demand** | walk the rain round the crowd, then stop. Moving pays twice: it lights the stars (how many arrows) and draws the bow (how hard each one hits, Hunter's Draw's full draw) |
| **payload** | a converging volley: one piercing arrow from each lit star, crossing the crowd on its way to the mark; the mark's Vulnerable (+25%, 5 s) stays; the lines between the stars still cut while he walks |
| **look** | the lit stars as white-gold points on the ground, joined by Constellation's lines; the mark's four arrowheads turn round the quarry; as the draw fills, a thin arc of light pulls taut between the two stars furthest apart; on the loose every star flares, a gold arrow with a white head streaks from each to the mark, the lines flash once and go out. No new sheet: drawn from Starfall's star arrays and Hunter's Draw's arrow and mark |
| **sound** | a bowstring and a glass chord (Light's voice), one per loose, not per arrow |
| **elements** | Metal 1, Wind 1, Light 1, plus their level 4s: for example Metal 2, Wind 1, Light 2 after Heavy Draw and Shooting Stars. **Frostfall** (Starfall's Ice path) would make four: not offered |

**More than the sum.** Neither parent fires from anywhere but the wizard or the sky. Sagittarius makes
his path the aim: a loop round the pack puts archers on every side of it, and the arrows cross it to
the mark, piercing as they go. The net of lines cuts while he walks and the arrows land when he stops,
so both halves of a kite pay. Hunter's Mark alone fires one volley from one place; Constellation alone
mostly hits nothing; this is crossfire on a chosen target.

**First-guess numbers.** Each arrow is Hunter's Draw's level 8 arrow at 0.8 with Hunter's Mark's pierce,
scaled by the draw as today; up to 16 arrows; lines at Constellation's 55% every 0.5 s. Tune to 1.2 times
the two parents' damage on the circle bot.

**Card:** *"Stars that fall while you walk stay lit and join into an archer. Stop, and every lit star
looses at the quarry you have marked."* **Tagline:** *"Walk the stars. Stop. Loose."*

**Cost: M.** `Sagittarius.cs` holds Starfall's star arrays with a lifetime set by the draw state, reads
`Player.TickHuntersDraw`'s meter and Hunter's Mark's marking; arrows on `ElementalBolt`. One icon.

---

## 4. Nine more

Each is a pair whose fusion makes a **new verb, grown out of the two named Ascensions**. None of these
verbs is an Apotheosis's (burn, stop, link, hold, flood, swarm, pull, reflect, rewind, nor the ten
`.ai/chapter-wizards.md` proposes). Between them they touch all twelve elements. Names were checked
against the codebase: Eclipse (Hollow Star's Light path), Prism (Scorching Ray's Ascension, and a chest
item), Lodestone (a chest item) and Whiteout (the Still Warden) are taken. Element counts are with the
named Ascensions' gains; "four" names the level 4 path that would make one, which the strip refuses.

### Orrery - Solar Flare + Hollow Star (orbit)

- **Needs:** Solar Flare **Daybreak Avatar** (`solar_flare_daybreak`, +Light: a flare that fills the
  clearing and burns away every enemy bolt in it) and Hollow Star **Black Sun**
  (`hollow_star_titan_cataclysm`, +Darkness: the star pulls and collapses several times before it fades).
- **Aim / cadence / demand / payload:** self / grows while he stands still / **hold still to gather,
  step to release** / enemies held circling.
- Daybreak's flare is wide enough to be a sky; Black Sun's pulls, repeated, are what keep a body
  wheeling instead of arriving. While he stands still, each pulse catches what is within the well's
  reach into **orbit** at the flare's rim, unable to close, burnt each time the pulse passes. When he
  steps, Black Sun's last collapse takes the whole orbit to one point before him.
- **Elements:** Light, Fire, Darkness. Hollow Star's Eclipse path adds Light: still three. Never four.
  Hollow Star is John Dee's starting spell (and Black Sun his path): not his to fuse.
- **Card:** *"While you stand still, the sun draws what is near into orbit round its rim. Step away and
  the orbit falls in."* **Tagline:** *"Hold still. They circle."*

### Maelstrom - Riptide + Cyclone Slash (fling)

- **Needs:** Riptide **Tidal Wall** (`riptide_titan_cataclysm`, +Water: the wave swells into a wall,
  a third as often) and Cyclone Slash **Vortex Reaper** (`cyclone_slash_vortex_reaper`, no element: the
  whirlwind drags the crowd round it into the blades).
- **Aim / cadence / demand / payload:** the wall's line / the whirl gathers continuously, the wall
  throws on its cooldown / **run to fill the whirl, then aim the wall at the crowd** / enemies thrown as
  projectiles.
- Vortex Reaper drags bodies round him; the Tidal Wall, when it goes, takes everything the whirl is
  holding up on its crest and **flings** it out along its line, and a body that lands among others
  strikes them by its weight.
- **Elements:** Water, Wind. One level 4 path (Brackish Tide +Poison, Razor Gale +Metal) makes three;
  **both** make four: not offered. Elites and bosses are struck, not thrown. Riptide is Tituba's
  starting spell and Cyclone Slash Laelaps's: not theirs to fuse.
- **Card:** *"The whirlwind holds what it catches, and the wall throws it all into the crowd ahead."*
  **Tagline:** *"They are the ammunition."*

### Hailfront - Stormsweep + Sleet Runes (advance)

- **Needs:** Stormsweep **Thunderclap** (`stormsweep_thunderclap`, +Lightning: every arc sweeps down
  and back, ending in thunder at both ends) and Sleet Runes **Sigil Fan** (`sleet_runes_sigil_fan`, no
  element: every cast leaves at once, fanned wide).
- **Aim / cadence / demand / payload:** facing / timer / **face where you want ground, then follow** / a
  moving wall.
- Sigil Fan makes the stream a front; Thunderclap's down-and-back becomes a step out: the fan of runes
  **advances** the way he faces in two claps of thunder, shoving and cutting what it meets, then breaks.
  Fronts cast the same way stack. The escape spell, and the one the Last Bridge is built for.
- **Elements:** Lightning, Ice, Wind. Stormsweep's Iron Current path (+Metal) makes four: not offered.
  Bosses are cut, not shoved.
- **Card:** *"A front of sleet and lightning walks out the way you face, shoving and cutting what it
  meets."* **Tagline:** *"Face it. Walk in behind."*

### Dark Lens - Scorching Ray + Void Lance (refract)

- **Needs:** Scorching Ray **Prism** (`scorching_ray_titan_cataclysm`, +Arcane: each ray splits into
  rays on the enemies round its target) and Void Lance **Witching Hour** (`void_lance_annihilation_beam`,
  +Arcane: the lance stays in his hands and sweeps round him like the hand of a clock).
- **Aim / cadence / demand / payload:** the ray's targets, through the lance / timer / **stand where the
  clock hand's sweep crosses the ray** / one beam split into a fan that turns.
- Prism splits at the target; here it splits at the **lens**: where a ray crosses Witching Hour's
  turning hand it **refracts** into Prism's fan, each new ray ramping on its own target, so the fan turns
  with the hand across the field. The crossing burns hottest.
- **Elements:** Fire, Arcane, Darkness (Arcane deep). Both parents hybrids: never four.
- **Card:** *"Where your rays cross the turning lance they split into fans that sweep round with it."*
  **Tagline:** *"Put the lance between you."*

### Pitfall - Meteor Swarm + Gravewell (funnel)

- **Needs:** Meteor Swarm **Creeping Barrage** (`meteor_orbital_bombardment`, no element: the volley
  walks in lines through the thickest pack) and Gravewell **Grave Line** (`gravewell_infinite_horizon`,
  no element: graves dug in a line behind him as he walks).
- **Aim / cadence / demand / payload:** down his line of graves / the barrage, on a trip / **dig the
  line where they will cross it** / a barrage walking a path he laid.
- Creeping Barrage already walks a line; Grave Line already digs one. The barrage stops choosing its own
  and is **funnelled** down his, falling hardest on the graves that have tripped and on what they hold.
- **Elements:** Fire, Earth, Darkness. Both parents hybrids: never four. Neither named Ascension deepens
  an element, so a tag-chasing planner passes both by: the one recipe here that asks for a build against
  the grain.
- **Card:** *"Your barrage walks down your line of graves, falling hardest on what they hold."*
  **Tagline:** *"Set the ground. Call the sky."*

### Reprisal - Aegis Ward + Spiritual Weapon (answer)

- **Needs:** Aegis Ward **Thorn Crystal Shell** (`aegis_ward_crystal_spikes`, no element: a breaking
  shield bursts into crystal shards) and Spiritual Weapon taken as the **hammer**, with **Hammerfall**
  (`spiritual_weapon_hammer_hammerfall`, +Arcane: two more hammers, falling in turn).
- **Aim / cadence / demand / payload:** **whatever last hit you** / **on being hit** / **be hit while
  the shield holds** / a blow from above.
- Hammerfall's three hammers hang over him. Every hit the shield takes is **answered**: one falls on
  whoever struck, the shooter of a bolt included. When the shell breaks, it bursts as Thorn Crystal Shell
  does and all three fall round him.
- **Elements:** Metal, Light, Arcane. **Mjolnir** was the other hammer, but it trades Light for
  Lightning, which would make four; the axe and the sword take no part.
- **More than the sum:** two axes the roster lacks (`spell-variety.md` section 2) in one spell: the
  target is whoever hurt you, the cadence is being hurt. Not Laelaps's *reflect*, which returns the bolt.
- **Card:** *"Every hit your shield takes brings a hammer down on whoever struck it. When the shell breaks,
  they all fall."* **Tagline:** *"Strike me. See what answers."*

### Anvil Line - Molten Shard + Iron Palisade (ricochet)

- **Needs:** Molten Shard **Forge Swarm** (`molten_shard_infinite_horizon`, no element: shards gather
  round him and launch together, each at its own enemy) and Iron Palisade **Iron Ring**
  (`iron_palisade_titan_cataclysm`, +Metal: the stakes close into a ring round the nearest pack, penning
  it).
- **Aim / cadence / demand / payload:** the pen / timer / **pen the pack, then keep it penned** /
  ricochet, stake to stake.
- Iron Ring makes the pen; Forge Swarm fills the air. The forged ring launches into the pen and every
  shard **ricochets** off the stakes inside it until spent, withering each body it passes.
- **Elements:** Metal, Fire; Iron Palisade's Lightning Rod path makes three. Never four.
- **Card:** *"Your shards are loosed into the iron ring and glance stake to stake, withering all inside."*
  **Tagline:** *"Every stake an anvil."*

### Burning Briar - Thorned Vine + Cinderbreath (fuse)

- **Needs:** Thorned Vine **Thicket** (`thorn_vine_titan_cataclysm`, +Poison: brambles grow over the
  arc it swept) and Cinderbreath **Dragon's Breath** (`cinderbreath_titan_cataclysm`, +Fire: a longer,
  wider breath that raises a wall of fire where each burn ends).
- **Aim / cadence / demand / payload:** the densest pack, lit from where he stands / timer, then the
  flame / **lay the thicket, bring the breath to one end** / fire that runs along laid lines.
- Thicket leaves the fuse lying on the ground; Dragon's Breath leaves fire where its burn ends. Where
  that fire touches a thicket it **runs its whole arc**, burning everything in it.
- **Elements:** Grass, Poison, Fire. Cinderbreath's Hellbreath path (+Darkness) makes four: not
  offered. Cinderbreath is the Pyromancer's starting spell (Dragon's Breath is on his path): not his.
- **Card:** *"Your breath sets the thicket alight, and the fire runs the whole of it."* **Tagline:**
  *"Lay the fuse. Light one end."*

### Black Ice - Mirefoot + Cone of Cold (slip)

- **Needs:** Mirefoot **Great Bog** (`mirefoot_titan_cataclysm`, +Grass: stand still and the pools
  spread into a marsh that slows all to a crawl) and Cone of Cold **Glacier Tide** (`cone_glacier_tide`,
  no element: the blast goes out as a wall of ice that throws everything it hits away from him).
- **Aim / cadence / demand / payload:** the marsh, then the way he faces / distance and stillness, then
  the cone / **spread the bog, face them, loose the tide** / loss of footing.
- The Glacier Tide freezes the Great Bog it crosses into black ice and throws what it hits out across
  it. Whatever is on the ice **slips**: it slides on the way it was going until it leaves the ice,
  unable to turn, shoving what it meets.
- **Elements:** Water, Grass, Ice; Cone of Cold's Frost Shrapnel path adds Water: still three. Never
  four. Bosses do not slip. Cone of Cold is the Frostweaver's starting spell. Build after
  `wip/enemy-pathing`, which owns enemy movement.
- **Card:** *"Your tide freezes the marsh and throws them onto it. What is on the ice slides on and
  cannot turn."* **Tagline:** *"They follow you onto the ice."*

---

## 5. Ranked by value against cost

The stricter gate changes the value column more than the cost one. Every recipe is now a plan made at
two level 8 screens, met at about minute 10 to 13 of a planned run and seldom by chance (section 2). A
recipe whose named Ascensions are ones a planner takes anyway is reached more often; one that asks for
the untagged forms, which a tag-chasing build passes by, is a deliberate detour.

| rank | combination | named Ascensions | verb | how often it is met | cost | why here |
|---|---|---|---|---|---|---|
| 1 | **Sagittarius** | Hunter's Mark + Constellation | converge | planned only (both untagged) | M | Eric's; both parents' code reused |
| 2 | **Reprisal** | Thorn Crystal Shell + Hammerfall (hammer) | answer | planned; also needs the hammer form | **S** | two missing axes at once; a shield build becomes an offence |
| 3 | **Orrery** | Daybreak Avatar + Black Sun | orbit | **often**: both deepen an element | M | scripted steering exists (`AdjustSteering`) |
| 4 | **Hailfront** | Thunderclap + Sigil Fan | advance | fairly often (Thunderclap deepens) | M | the travel spell for the Last Bridge |
| 5 | **Maelstrom** | Tidal Wall + Vortex Reaper | fling | fairly often (Tidal Wall deepens) | M-L | an enemy as a projectile is new |
| 6 | **Pitfall** | Creeping Barrage + Grave Line | funnel | rarely (both untagged) | **S** | cheap, but asks for a build against the grain |
| 7 | **Black Ice** | Great Bog + Glacier Tide | slip | fairly often (Great Bog deepens) | M | waits on the pathing branch |
| 8 | **Anvil Line** | Forge Swarm + Iron Ring | ricochet | fairly often (Iron Ring deepens) | M | bounce geometry inside a moving ring |
| 9 | **Burning Briar** | Thicket + Dragon's Breath | fuse | **often**: both deepen; never for the Pyromancer; Hellbreath refuses it | M | the thicket's arc is already drawn |
| 10 | **Dark Lens** | Prism + Witching Hour | refract | **often**: both deepen Arcane | M | beam crossing on a turning hand; closest to an Ascension that already exists (Prism) |

Pitfall drops from third: still the cheapest after Reprisal, but its two untagged forms make it the
recipe fewest players will ever see. Orrery rises: both its forms deepen, so a Light or Darkness build
meets it on the way to its tiers.

**Build order:** the system first (the strip offer with its named Ascensions, the browser's "sealed this
run", the mark on the level 8 card, the fusion, the derived Feat, the validator, the `--seek-combo`
flag), proved by **Sagittarius**; then Reprisal and Orrery; then the rest by rank. Each verifies like an
Ascension: `dotnet build`, the validator run with `-- --no-save-writes`, the harness band (damage a second
against the two parents' sum, and the minute the recipe was met), `_UiShot` of the strip card, the
browser row sealed and open, and the spell in a crowd at both canvases. **What no harness checks:**
whether a combination reads at a glance as its two parents, whether a player sees the mark on the level 8
card and plans for it, and whether two or three minutes of a combination before the boss feels like
enough.

---

## 6. Built (2026-11-01)

Sections 0, 2 and 3, as written. Where to look:

- **`scripts/ComboCatalog.cs`** is the one authority, as `ApotheosisCatalog` is: each recipe names one
  level 8 Ascension per parent (and the parents' and Ascensions' names, held to the resources by the
  validator, so the derived Feat needs no resource loaded). `ComboCatalog.Evaluate` says where a recipe
  stands against a loadout - each parent met, sealed by its other Ascension, the wizard's own starting
  spell (never the Test Wizard's), or a pair naming four elements - and the strip offers exactly what it
  calls `Ready`. `MilestoneLevel` 10 tells a combination's offer from the Apotheosis's 9; every screen
  asks `ComboCatalog.IsComboOption` first.
- **The strip and the fusion** (`scripts/Player.Combinations.cs`): `GetAscensionOffers` adds the offer
  after every spell's own, "✶ CONJUNCTION  Sagittarius" on the **conjunction plate** - bronze glass, runes
  of starlight (`CardFamily.Conjunction`, `tools/art/ui_frames.py`, inks measured in `ui_contrast.py`) -
  opening a one-card page ("Hunter's Draw and Starfall become one spell. One slot is freed."). The
  selection is `sagittarius:sagittarius`; `TryAddOrLevelSpell` hands a combination's id to `TryFuse`,
  which re-checks the recipe, freezes both parents (`SpellData.ComboParents`, a plain C# array - an
  exported `Array<SpellData>` on `SpellData` held its own script past teardown), sums their current
  weights onto the combination, puts it in the first parent's slot, frees the second, and adds both to
  `WasFusedAway`, which `GetLevelUpOptions` reads. A combination is never in the spell catalog and has no
  milestones (`SpellEvolutionCatalog.EnsureEvolutionCoverage` returns early for one).
- **The Feat** (`AchievementDefinitions.CombinationDeeds`, `AchievementVisibility.Recipe`): "Conjunction:
  Sagittarius", completed through `CompleteNow` by `Node2DGame.RecordCombinationsMet` the first time the
  strip offers the recipe - before the browser reads the save - with the moment's banner, granting the
  spell there and then. Its measure is its own record: no schema bump. Not on the Achievements page until
  the save owns both parents; then "Conjunction: ???" / "Unlocks ???" (`CodexRows.DeedEntry`) until made.
  `UnlockCatalog.FromCombination` is the spell's entry; the Spell Index hides the name as it does every
  page not found.
- **The browser** (`LevelUpMenu.Ascensions.cs`, `BuildConjunctionTree`): a "✶ Conjunctions" section after
  the spells' own trees, one tree per recipe the save knows or this run holds - the two parents on the
  node, each named Ascension ticked when held, "took the other path: sealed this run", a lock line for
  the wizard's own spell or a fourth element - and the result on the conjunction plate, "???" until made
  or offered. A level 8 path a known recipe names carries **two linked rings** (`stat-conjunction`) and
  "Joins Starfall - Constellation", on the fork and on the browser's plate; the result is not named.
- **The validator**, `RegressionChecks.ValidateComboCatalog`, holds every line of section 2's list, plus:
  the resource is terminal (one level, no level-ups, no milestones), its tags are the parents' base tags
  summed, and a spell paid by a Conjunction Feat has a recipe. `ValidateElementTags` and
  `ValidateElementPaths` leave combinations to it; `ValidateAchievements` knows a combination's Feat waits
  and completes in the run.
- **`_BalanceHarness --seek-combo=ID`** and **`_UiShot --demo-combo=ID[:fuse]`** (the scripts' headers say
  how). `--seek-combo` switches on `--seek-ascensions`, keeps slots for the parents, fishes for their
  cards, keeps their level 4 inside three elements and never takes the other Ascension; the summary line
  carries `combo=`, `comboOffered=`, `comboTaken=` and `comboDps=`.

**Sagittarius** (`scripts/Sagittarius.cs`, `scenes/Sagittarius.tscn`, `SpellData_Sagittarius.tres`): a
Starfall on the frozen Starfall, credited to the combination (`Starfall.DamageSource`), so its stars,
Constellation's lines and its level 4 (Shooting Stars, Skipping Stars) fall as they did. Hunter's Draw's
draw runs in the node off the player's own tunables; while the bow is drawn every star that lands is held
lit (`Starfall.HoldKindled`, `MaxLitStars` 16, the oldest out first). The loose - stopping, or the arms
giving out at a held full draw, as Hunter's Draw's do - sends one of the frozen Hunter's Draw's arrows from
every lit star (`ArrowShare` 0.8 of its hit, scaled by the draw, its pierce and Hunter's Mark's size, its
level 4 riding on it: Heavy Draw's stagger, Barbed's bleed) at the mark, or at the enemy nearest the
wizard; a full draw marks the strongest in reach (Vulnerable +25%, 5 s, `HuntersMark`). The arrows are
gilded (`HuntersArrowLook`); every star flares and goes out (`Starfall.Flare`) and the lines flash once; a
thin arc pulls taut between the two lit stars furthest apart as the draw fills. One sound a loose,
`spell_sagittarius` (a bowstring, then a G major chord rolled upward in glass partials, -11 dBFS). Held,
never loosed, while a boss arrives. Running Shot does not carry: it fires from the wizard.

**Since F2 of `.ai/second-half.md` (2026-11-08) Artemis opens on it** (`StartsFusedFor = "artemis"`), at level 1
of a table in `SpellData_Sagittarius.tres` that her copy reads because it carries no parents
(`SpellData.ReadsOwnTable`): the rain is a Starfall on the Conjunction's own numbers, the lines are on from level 1
(`Starfall.Linked`), and the arrows are 0.7 of a star's hit, loosed from Hunter's Draw down Hunter's Mark at her level.
Its level 8 is this section's plain fused copy. A fused copy is unchanged except that it now arrives at level 8 of 8, The
Taking's rule. Her Apotheosis on it is The Hunter's Moon (hunt); `.ai/second-half.md` section 9, "F2, built".

**Measured (2026-11-01): the recipe did not land inside a 15-minute run.** `_BalanceHarness
--seek-combo=sagittarius --all-unlocked --immortal --circle=180`, Laelaps (Cyclone Slash, so neither parent
is his own), timescale 2-3:

| run | Hunter's Draw down Hunter's Mark | Starfall down Constellation | offered |
|---|---|---|---|
| seed 1234 | 12.6 min (player level 24) | level 2 at 15:00 | - |
| seed 1234 (the circle bot stuck from 1.5 min - why `--circle` now re-lays itself) | level 5 at 15:00 | 12.0 min (level 17) | - |
| seed 7, `--full-shop` | 5.9 min (level 17) | level 7, ready, at 15:00 | - |
| seed 7, `--full-shop --upgrade-chance=1` (left at 6.5 min: the orbs the circle bot leaves had it at 1 fps) | not yet in the book | 6.5 min (level 17) | - |

Section 2's "a planned run reaches one recipe at about minute 10 to 13" did not hold for the bot. What held
it back was never a gate but the cards: fourteen upgrades across two spells that are not the wizard's own,
on a roll that deals held spells' upgrades only some level-ups (`Player.UpgradeOfferChance`), with ten
rerolls a run to fish with. The circle bot also leaves the XP it does not pass over, so it levels more
slowly than a player who walks to orbs. Whether that is too late is the pacing question section 2 already
names (`.ai/run-pacing.md`), not a reason to loosen the recipe. The whole path - the strip offering it, the
Feat's banner ("Achievement - Conjunction: Sagittarius / Spell recovered - Sagittarius"), the page, the
fusion and the spell firing - was taken through the menu by pad presses in `_UiShot --demo-combo=sagittarius
--force-levelup --keep-levelup --pad=release,a,a`. Its damage against the parents' (section 3's 1.2 target)
is unmeasured: no harness run reached it, and `ArrowShare` 0.8 and `MaxLitStars` 16 are still section 3's
first guesses.

**The Sovereign Rite is in `ComboCatalog` too** (2026-11-02, `.ai/sovereign-spell.md` section 8). Until
2026-11-08 it was a recipe whose second ingredient was "any active spell you hold"; since then it is an
ordinary recipe of two named Ascensions (section 8 below). After it is made nothing fuses.

**The Taking - Hollow Star + Kindled Ward (seize)** (2026-11-02, `.ai/dark-wizard-playable.md` section 6). Needs
Hollow Star down **Black Sun** (`hollow_star_titan_cataclysm`, +Darkness) and Kindled Ward down **Pyre**
(`kindled_ward_titan_cataclysm`, +Light): the dark wizard's darkness and the light he took. A hand of dark light
seizes the strongest in reach and the tear collapses on what stands round it (Black Sun); the light it tears out
drifts home searing what it passes (Pyre); caught, two lanes of his light cross the field. Darkness 2, Light 2
with the named Ascensions; Rimelight makes three. Not John Dee's to make, nor Seimei's.

**The one Conjunction with levels.** `ComboDefinition.StartsFusedFor`: the dark wizard OPENS on it, made, at
level 1, and cards level it to 8 off its own table (Eric: "should be at level 1, and level up, even though
combination spells wouldn't have levels normally"); his Apotheosis treats its level 8 as ascended. Anyone else
fuses it as any recipe and gets it whole, at level 8, with nothing left to take - a fused Conjunction still never
levels. Its numbers are its own table's at the level held, not two frozen parents' (he never held any): the
departure from section 2's "read once from both parents", and the reason its fused copy is simply its level 8.
`RegressionChecks.ValidateOpeners` holds the exception, for every opener (since F1 of `.ai/second-half.md`, the
gods open on their own Conjunctions the same way).

---

**The Conjunction ladder (2026-11-09, `.ai/conjunction-progression.md` section 6)** changes how a save comes to know a
recipe and works toward it, and nothing of the recipe itself - both parents at 8 down both named paths still. Once the
Conjunctions lesson is learned a recipe is shown from **either** half (`ConjunctionLadder.IsShown`; section 2's "owns both
parents" stays the rule before it, and the Feat row still reads "Conjunction: ???" until made), the missing half by its
locked hint; one recipe may be pinned, its checklist on the pause screen and its next step in the quest corner; a level 4
path that would name a fourth element is marked "Seals ..." beside section 2's level 8 rings. Kindred Draw (a Power) leans
the deal toward a held parent's partner; the Archivist's lesson lets a held pair's named paths count each other's tags
toward their element gates; Nostradamus, freed, foretells one recipe a campaign run, dealt by the far sky's favour.

## 7. Shadow Bolt - Void Lance + Contagion (fester). Built 2026-11-07

Eric, 2026-11-05 (in-flight item 17): *"Shadow Bolt should become a combination spell"* - move it
into the Conjunction roster, rebalance the element tags it takes out of the ordinary pool, scale its
power for the new role, and make its visuals much more interesting. This section is the conversion.

**Needs:** Void Lance at level 8 with **Cosmic Collapse** (`void_lance_cosmic_collapse`, no element)
and Contagion at level 8 with **Pandemic** (`contagion_titan_cataclysm`, +Poison).

**Why those two.** Void Lance is the roster's bolt of shadow already, and Cosmic Collapse ends it in
a black hole; Contagion is the spell that moves body to body, and Pandemic is the whole infected
crowd going off on one beat. Fused, the void is given the plague's hunger: the bolt itself leaps
from body to body - the old Shadow Bolt's chain silhouette, kept - and what it curses caves in on
Pandemic's beat, spreading the curse as it goes. With Witching Hour the lance never ends, so there
is no hole to seed; with Plague Wind the plague drifts instead of beating: either seals the recipe.

| | Shadow Bolt |
|---|---|
| **aim** | the thickest pack in reach: of the enemies in cast range, the one with the most neighbours within a leap |
| **cadence** | the bolt on the frozen lance's cooldown; the cave-ins on Pandemic's shared 2 s beat, all at once |
| **demand** | feed it density. The curse spreads only through the cave-ins' reach, so a scattered line starves it and a pack sustains it - Contagion's inheritance |
| **payload** | the bolt leaps through up to 6 bodies, each struck and cursed (poisoned, slowed 0.72x, +18% damage taken - the old spell's identity, now the curse's); on each beat every cursed body caves in, striking itself and all round it and festering onto the 2 nearest clean bodies |
| **look** | jagged strokes of the Darkness ramp body to body (SpellStrokes); a dark ring with three motes of rot at every carrier's feet, all swelling together before the beat (Pandemic's tell); each cave-in a disc of void that swells and falls in, its rim white-hot as it closes (Cosmic Collapse's own timing), with thin rot strokes out to the newly cursed |
| **sound** | none yet, as the old spell had none: its voice belongs to the audio pass (`.ai/audio-manifest.md`) |
| **elements** | Arcane 1, Darkness 1, Poison 1 - the old Darkness + Poison kept, Arcane joining. Pandemic deepens Poison and Dread Darkness: still three. **Marsh Fever** (Contagion's Water path) would make four: not offered |

**More than the sum.** The lance hits one line once; the plague needs deaths to move and a pack to
die in. Fused, neither limit holds: the bolt seeds six bodies in a breath, and the beat keeps the
field erupting - wave after wave of small collapses walking outward through the crowd - without a
single kill. The payoff moment is the whole screen blinking dark on one heartbeat.

**Bounded**, because the spell it replaces once made 9,841 arcs from one cast (`.ai/spell-roster.md`,
"a cast is capped"): no enemy is struck twice by one cast's bolt; at most 24 bodies carry the curse
at once (`MaxCursed`, Contagion's own MaxInfected - the cap is the epidemic's intensity, not a total,
so the oldest curse ends early when it is full); a beat hits each enemy on the field at most once
(Pandemic's rule); each cave-in festers onto at most 2. The beat and the casts walk
`EnemyRegistry.Snapshot`; per-frame work is drawing alone.

**First-guess numbers** (`scenes/ShadowBolt.tscn`): each leap 0.75 of the frozen lance's strike
(Cosmic Collapse's x2.4 folded in); each cave-in 0.5 of it across 0.55 of the lance's collapse burst;
the curse's poison the frozen plague's dose for the plague's mark length. Unmeasured against section
2's 1.2x target: measure with `_BalanceHarness --seek-combo=shadow_bolt --all-unlocked --immortal`
and `_UiShot --demo-combo=shadow_bolt:fuse`, as Sagittarius was.

**The conversion.** What moving an owned spell into the roster touched, beyond section 6's recipe
checklist:

- **The unlock moved.** The Gaoler's deed (`castle_conqueror`) pays only Bone Rattle now;
  `UnlockCatalog.FromCombination("shadow_bolt")` is the spell's one source, its derived Feat
  "Conjunction: Shadow Bolt". Paracelsus cannot make it (Contagion is his starting spell).
- **Schema 23** (`SaveData.MigrateShadowBoltToConjunction`): a save that owned Shadow Bolt - the
  Gaoler's deed done, or the spell granted outright by the pre-campaign grandfathering - has the
  recipe's Feat recorded done, quietly, as schema 19 recorded owned facts. It keeps the spell, its
  Spell Index page keeps its name, and no Arcane Energy changes hands.
  `RegressionChecks.ValidateShadowBoltMigration` holds it.
- **The old kit went with it.** The five milestone cards (Black Sigil, Creeping Rot, Spreading Dark,
  Anathema, Threefold Curse) left `SpellEvolutionCatalog` - a Conjunction is terminal - and the new
  `scripts/ShadowBolt.cs` replaced `ChainLightning.cs` as the spell's script. The shadow-bolt hooks
  in ChainLightning and ShadowCurse are inert, kept rather than torn out of the tuned chain path; a
  save's `SeenAscensionIds` may hold the old ids, which nothing reads as live options.
- **The element pool.** The ordinary pool loses its only Darkness+Poison hybrid. What remains:
  Darkness on Hollow Star (pure, deepens to 3), Void Lance, Gravewell and Obsidian Spike's branch;
  Poison on Contagion (pure, deepens), Thorn Vine, Toxic Spore Burst and Black Tentacles - and the
  boons and relics (Bone Rattle, Wormwood Tithe and the rest). `ValidateElementReachability` still
  holds the floor: every element reaches its tier-6 capstone, on the road, after the move. Before a
  fusion is available nothing waits on the lost tags - since 2026-11-02 no Ascension or Apotheosis
  gate reads element tiers (the gate is the wizard's own spell at max level, plus player level 21).
- **The Test Wizard** lost it as an opener and the balance bot's Black Cat no longer casts it (a
  Conjunction without frozen parents cannot fire); `_BossProbe`'s darkness probe is Hollow Star now.

**What no harness checks:** whether the fused spell reads at a glance as its two parents, and
whether the beat's swell-then-collapse stays readable in a 140-body swarm at both canvases -
`_UiShot --demo-combo=shadow_bolt:fuse` photographs it, but judging it still needs eyes.

---

## 8. The Sovereign Rite - Arcane Explosion + Gravewell (anoint). Built 2026-11-08

Eric, 2026-11-08: *"the soverign rite should be a combination spell itself - pick two spells that make
sense to combine to create this"*, and *"soveriegn rite also doesn't need an animation to learn. it just
unlocks with combination spells"*. The whole design, its gates, its fuel and its measurement are
`.ai/sovereign-spell.md` section 9; this is its row in the roster.

- **Needs:** Arcane Explosion at level 8 down **Resonance** (`arcane_explosion_supernova`, no element: every
  blast goes off again where the wizard's other spells last struck) and Gravewell at level 8 down **Mass
  Grave** (`gravewell_titan_cataclysm`, +Darkness: one great grave a cast under the front of the swarm,
  draining what it holds) - **and**, unlike every other recipe, the Rite's late gate: player level 28 and 16
  levels across the other active spells (`SovereignRite.MeetsGate`).
- **Aim / cadence / demand / payload:** the thickest pack in reach / the frozen Gravewell's cooldown, one
  great grave a cast / stand where packs form / a crowned blast that throws nothing, a great grave opened
  under it already armed, and the blast again inside the grave as it springs (Resonance's echo, made the
  grave's).
- **What it costs:** every other active spell falls silent for the rest of the run and its levels become the
  Rite's fuel (the verb, ANOINT). Passives - Aegis Ward - are untouched.
- **Elements:** Arcane, Earth, Darkness: three, the ceiling. **Static Nova** (+Lightning) on Arcane Explosion
  makes four and refuses the fusion; Lingering Rift keeps the pair inside three. Arcane Explosion is the
  Cumaean Sibyl's own spell, so the recipe is never hers (`ComboSeal.OwnSpell`).
- **Its neighbours.** Neither parent is in another BUILT recipe. Section 4's **Pitfall** (proposed) names
  Gravewell too, but down **Grave Line**: the two would seal each other through Gravewell's one Ascension,
  which is the system working as section 2 means it to.
- **Never tonight's recipe:** `FarSky.MayName` refuses `IsRite`, so the far sky never names the capstone.
- Where to look: `ComboCatalog` (the recipe, last), `SovereignRite` (the gate and the fuel table),
  `SovereignRiteCaster` (`scenes/SovereignRite.tscn`), `Player.Sovereign.cs` (`Crown`, the silence),
  `SpellData_SovereignRite.tres`; held by `ValidateComboCatalog` like any recipe and by `ValidateSovereignRite`
  and `ValidateRiteMigration` for what is its own.
