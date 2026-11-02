# The Sovereign Rite: every spell's power poured into one

**Status: approved by Eric (2026-11-02) with one change, and built (section 8):** it is a **combination**:
the Rite is one ingredient and the spell you point it at is the other (section 3, as revised).

Eric, 2026-11-02: *"I want a very powerful ability that you can't unlock until late in the game that
prevents any other non-passive spell from activating, but gets power based on the number of other
non-passive spell levels you have. Use the deus ex machina spell from magic survival as inspiration.
There should be a way to choose the spell you use it on, and whatever spell you choose should be affected
visually in some way that signifies that it is being affected by this very powerful ability."*

Magic Survival is the inspiration for the **mechanic only** (one capstone that feeds on the rest of the
build). The name, the look and every word here are ours (`.ai/art-direction.md`; the project's
original-content rule).

---

## 1. What it is

A **rite**, not a spell: it takes no slot and casts nothing itself. Taken once, late in a run:

- **You choose one of your active spells**: the *anointed* spell.
- **Every other active spell stops firing** for the rest of the run. Passives (`SpellData.IsPassive`:
  wards, auras, the defensive spells) are untouched.
- **The anointed spell gains power from every level the silenced spells hold**: their levels are its
  fuel. Five other spells at level 8 is 40 levels of fuel.
- **It never undoes.** The rest of the run is that one spell.

The fiction: the dark wizard took every wizard's magic by drawing it into one place; the rite is the
player doing the same thing, once, to one spell. It is the most "his" thing the player can do, which is
why it comes late and costs everything else.

---

## 2. Name

**Recommended: the Sovereign Rite** (the anointed spell is "Sovereign <spell>": "Sovereign Cinderbreath").
Alternates: *Unison*, *The Lodestar*. Not "Deus Ex Machina" (Magic Survival's), not anything in
`.ai/combo-spells.md`'s taken list (Conjunction is the combination spells' word).

---

## 3. Unlock and offer: a combination you point at a spell

Eric (2026-11-02): *"How does the player get sovereign rite? Randomly in level up? It should be a
combination spell that you select another spell to use on."* So it is not dealt at random. It is a
**Conjunction** (`.ai/combo-spells.md`), with one difference: the Rite is one ingredient, and the second
ingredient is **whichever of your active spells you choose**.

- **Unlocked** by a Road deed, **beating the Warden of the Deep** (the end of the road, chapter 10),
  through `UnlockCatalog`. Nothing on a new save can reach it early. **For now:** Eric has since decided
  (`.ai/slow-start.md`, top, 2026-11-02) that every new system - the Ascension, the Apotheosis, the
  Conjunctions, and after them the Rite - is unlocked by an achievement and taught in a post-run cutscene.
  That is the slow start's to build; until it chooses the Rite's achievement and cutscene, the Warden's
  deed stays its unlock.
- **Offered exactly where the Conjunctions are**: in the Ascension strip above the level-up cards, on the
  Conjunction plate, reading **"✶ CONJUNCTION  Sovereign Rite"**, once **all** of these hold:
  - player level **28** or more (25 in the first draft; moved by section 6's measurement, section 8);
  - at least **three active spells** held;
  - their levels sum to at least **16** besides whichever one is chosen.
  Like every strip offer it stays on each level-up until taken or the run ends, so it is never dealt
  away or lost to luck.
- **Choosing the spell**: taking the plate opens the Conjunction's one-card page, with your active spells
  in a row as the second ingredient: "Sovereign Rite + [ Cinderbreath ]  ->  Sovereign Cinderbreath",
  each showing the power it would gain now ("+38 fuel: x3.7 damage"). The pad walks the row, A fuses,
  Back cancels without spending the pick.
- **In the Codex and the Achievements list** it appears as a recipe whose second ingredient reads "any
  active spell you hold", and its derived Feat is "Conjunction: Sovereign Rite".

## 4. The power

Let **F** = the sum of the levels of every *other* active spell held when the rite is taken, plus every
level those spells would gain afterwards (they still level: an upgrade card for a silenced spell now reads
"+1 fuel to Sovereign Cinderbreath", so the build keeps growing). Then the anointed spell gets:

| what | per level of fuel | at F = 16 (the gate) | at F = 40 |
|---|---|---|---|
| damage | +0.5% (first draft +7%) | x1.08 | x1.2 |
| cooldown | -0.8% (floor -30%) | -13% | -30% |
| area / reach | +1% (cap +40%) | +16% | +40% |
| amount (projectiles, strikes, tentacles, where it has one) | +1 per 20 (first draft per 10) | +0 | +2 |

Measured and tuned by section 6 (section 8, "Tuned"): the first draft's +7% damage made the build's main
spell, at the gate's 16 fuel, x2.3 to x3.0 the loadout - the cooldown, area and amount steps carry most of
the gain on their own. **The target**: the anointed spell alone deals about
**1.3 times what the whole loadout dealt** before the rite, so it is a real gain for a real loss, not a
trap and not a must-take. Bosses take its full damage but none of it ever bypasses the boss
crowd-control rule (`.ai/in-flight.md` 3a item 4) or the no-execute rule.

**Interactions:** an Apotheosis form or a Conjunction can be anointed (the strongest late builds). A spell
already anointed cannot fuse into a Conjunction. Chest items and boons keep working.

---

## 5. How it looks

The anointed spell has to read as *changed* at a glance, in a crowd, on both canvases:

- **Its casts are crowned**: every projectile, strike or zone it makes gains a thin white-gold corona
  drawn in its own element's ramp (the element's hot stop and core, Bayer-dithered, never a translucent
  wash), plus a slow trail of motes, the "warm light" of `.ai/world-and-tone.md` gathered onto it.
- **The wizard wears it**: a small crown-ring of the anointed spell's element over his head, turning.
- **The loadout strip says it**: the anointed slot gets a gold crown frame and a fuel number; every
  silenced slot is greyed with a closed seal across it (the same seal the dark wizard's taking uses,
  `.ai/endgame-castle.md` 6.4, so the visual language agrees).
- **The moment it is taken**: a short beat (no pause): each silenced spell's icon flies from its slot
  into the anointed one, and the anointed spell's next cast fires with a bright flash.

---

## 6. Measuring it

`_BalanceHarness --seek-sovereign --all-unlocked --immortal`: when the strip offers it, and the loadout's
damage per second for the 60 s before and the 60 s after (target ratio about 1.3), across at least three
wizards and two builds each. The level 25 gate moves until the offer lands around **minute 11-12** for a
planned build.

---

## 7. Questions for Eric

**Answered 2026-11-02:** the recommendations stand (the Sovereign Rite; the Warden of the Deep;
level 25 and three actives; silenced spells keep levelling as fuel; about 1.3 times the loadout), and it
is offered as a Conjunction whose second ingredient the player chooses (section 3).

1. **Name**: the Sovereign Rite (recommended), Unison, or The Lodestar?
2. **Unlock**: beating the Warden of the Deep (recommended), or later (the Throne)?
3. **Offer gate**: player level 25 with three actives (recommended)?
4. **Do silenced spells keep levelling** as fuel (recommended), or is the fuel fixed at the moment it is taken?
5. **The target**: about 1.3 times the whole loadout (recommended), or stronger still?

---

## 8. Built (2026-11-02, `wip/sovereign-rite`)

Sections 1-6 as written, with the decisions below. Where to look:

- **`scripts/SovereignRite.cs`** is the one authority on the numbers and the rules: the gate (level 28, three
  active spells, 16 fuel behind the chosen one), the power per level of fuel (+0.5% damage, -0.8% cooldown
  to -30%, +1% area to +40%, +1 amount per 20, as tuned below), who counts as passive, who may be anointed, and the card line.
- **It is a recipe in `ComboCatalog`** (`IsRite`, second ingredient `AnyActiveSpell`, read "any active spell
  you hold"). `Evaluate` never calls it Ready and `ComboCatalog.Fusions` leaves it out; the strip offer is
  `Player.TryBuildSovereignOffer` (`scripts/Player.Sovereign.cs`), on the conjunction plate, "✶ CONJUNCTION
  Sovereign Rite". The selection is `sovereign_rite:<spell id>`; `TryAddOrLevelSpell` hands it to `TryAnoint`,
  which re-checks the offer and the target before it does anything.
- **The page** is the conjunction page with one card per active spell ("Sovereign Fireball", a "+24 fuel"
  chip, "x1.1 damage, -19% cooldown, +24% area, +1 amount"), narrower cards when there are more than three,
  the focus spotlight on, Back cancelling without spending the pick. A spell the Rite will not take, or one
  with less than 16 fuel behind it, is shown locked with the reason.
- **The power is a milestone the anointed spell carries**, `SpellData.SovereignBoon`, the fourth entry of
  `CollectActive` after the Apotheosis. So every stat reader sees the fuel through the one path the
  milestones use, and none names the Rite. `RefreshSovereignFuel` rewrites it in place whenever a level or a
  spell is added. A Conjunction anointed hands the same boon to its frozen parents (Sagittarius reads them).
- **Silence** (`Player.IsSilenced`, mirrored on `SpellData.SilencedBySovereign`): the fire loop skips the
  spell, its standing nodes go (`DropStandingNodes`: the persistent instances, the four on the wizard by type,
  the bow), Frost Shard's shatter and the Black Cat leave it alone. What it already put on the ground burns
  out on its own clock. A silenced spell is offered on a plain card to level 8 (`CardLevelCap` returns
  `MaxLevel` for it), "✶ +1 fuel to Sovereign X", with no mutation page and no Ascension or Apotheosis; a
  spell taken after the Rite is silenced as it arrives.
- **Unlock**: the Warden of the Deep's Road deed, `emberdeep_ended` ("Unlocks the Sovereign Rite and 300
  Arcane Energy"). A save that already beat him is handed it by `AchievementManager.Reconcile` at the end of
  its next run; **no schema bump**. Its Feat "Conjunction: Sovereign Rite" (`conjunction_sovereign_rite`,
  listed as a recipe once the Rite is owned) completes the first time the strip offers it, through the
  combinations' existing `Node2DGame.RecordCombinationsMet`, and pays 250 Arcane Energy. Not counted by The
  Grimoire Restored (`GrimoireSpellIds` leaves out every combination).
- **The look** (section 5), all code-drawn: `SovereignCorona` on every node a cast of the anointed spell makes
  through `Player.ApplyLegendaryVisual` (the ring in the element's core and hot stop, Bayer-thinned, gilt
  glints at the eight points, one cached texture per element and radius, a slow trail of motes; the first
  cast after the Rite flashes); `SovereignHits`, one pooled layer in the arena that crowns every strike of
  the anointed spell where it lands (`Player.DealDamageToEnemy`) - which is what crowns the spells that draw
  their own strikes and make no node to crown (Starfall, the Familiar, the Spiritual Weapon, Stormsweep,
  Sleet Runes, Frost Shard); `SovereignCrown`, the turning crown-ring over the wizard's head; and on
  `LoadoutHud`, the gold crown frame with the fuel in gold, the greyed slots under a wax seal on a band, and
  the silenced icons flying into the anointed slot when play resumes.
- **The validator**, `RegressionChecks.ValidateSovereignRite`: the gate and per-fuel numbers are these, and
  the gate can be met; one rite, registered, terminal, elementless, paid by a Road deed no earlier than the
  first gate chapter, never in the Grimoire's count; a passive or defensive spell is never silenced, fuel or
  anointed; for every spell form, the boon moves the spell's own damage, cooldown, area or scale and count by
  exactly its numbers and they come back without it; the boon carries nothing else (no effect, rider, slow,
  knockback, pierce, chain, element or execute), so it opens no crowd-control or execute path on a boss.
- **Tools**: `_BalanceHarness --seek-sovereign [--anoint=top|own|ID] [--anoint-wait=S] [--policy=ordinary]`
  and `_UiShot --demo-sovereign[=SPELL]` (the scripts' headers say how). Shots: add `--force-levelup
  --keep-levelup` for the strip, `--press-late=Ascend_sovereign_rite` for the page, and `--spell-stage=8
  --cast-on-stage` with `=SPELL` for the crowned casts and the loadout strip.
- **For the Evolution Codex** (section 3, "a recipe whose second ingredient reads 'any active spell you
  hold'"): `ComboCatalog.All` holds the Rite (`ComboCatalog.Rite`, `IsRite`). Its `First` is the Rite itself
  (`SpellId` `sovereign_rite`, no Ascension; `Label` reads "Sovereign Rite"), its `Second` has
  `AnyActiveSpell` set, no spell id, and reads `ComboCatalog.AnyActiveSpellName`. `Template` is its
  SpellData (name, line, icon), `IsKnown` is "the save owns the Rite", `IsMet` its Feat, `FeatId` gives
  `conjunction_sovereign_rite`. A screen that loads a parent's resource or its level 8 options must skip an
  `AnyActiveSpell` ingredient; `ComboCatalog.Fusions` is every recipe but the Rite.

**Decided while building** (for Eric to overrule):

1. **"Passive" is Aegis Ward.** `SpellData.IsPassive` is set on nothing since boons replaced the passive
   spells, so the doc's "wards, auras, the defensive spells" would have meant no spell at all. The one shield
   (`SovereignRite.DefensiveSpellIds`) is never silenced, never fuel, never anointed, and not one of the three.
2. **Two spells cannot be anointed**: Arcane Explosion (charged by the casts the Rite silences) and Frost
   Shard (shatters only what other spells kill). Either would be a dead build the moment it was taken.
3. **Nothing fuses after the Rite**, not only the anointed spell: two silenced parents would trade sixteen
   fuel for one level.
4. **Area or scale, never both**: a spell whose script reads area grows its area; one that has none but is
   drawn at a scale (the bolts) grows its scale instead, which is its reach. Cooldown and amount are given
   only to a spell that reads them, so a card never promises a number its spell ignores.
5. **The anointed spell cannot be cut by the Scribe's Penknife** (the Rite never undoes). A silenced one can,
   and its fuel goes with it.
6. **The Rite's own Feat pays 250 Arcane Energy.** The Warden of the Deep's deed pays the Rite itself, so its
   "Conjunction: Sovereign Rite" Feat (met the first time the strip offers it) could not pay the spell, and a
   deed needs reward words.
7. **A silenced spell is offered no Ascension and no Apotheosis**: it levels to 8 on plain "+1 fuel" cards
   (`CardLevelCap`), since a form it never fires would change nothing but its name.

Silenced spells also keep their element tags: they are still held, so no tier moves.

**Tuned (2026-11-02), as section 6 asks: +0.5% damage a level of fuel, +1 amount per 20, the gate at level
28.** Same harness, seed and timescale as the first table below, at most two runs at a time, the planned
policy anointing the build's main spell the moment it first opens (`--anoint=top`):

| wizard | build | offered (min, level) | taken (min, level) | anointed (its level), share | fuel at taking -> 15:00 | dmg/s before -> after | ratio |
|---|---|---|---|---|---|---|---|
| Pyromancer | planned | 10.4, L28 | 10.7, L29 | Cinderbreath (8), 0.84 | 16 -> 27 | 1129 -> 1579 | **x1.40** |
| Frostweaver | planned | 11.5, L28 | 12.6, L30 | Cone of Cold (8), 0.71 | 16 -> 25 | 947 -> 1367 | **x1.44** |
| Stormcaller | planned | 12.2, L28 | 12.8, L29 | Chain Lightning (8), 0.94 | 16 -> 19 | 868 -> 1160 | **x1.34** |
| Pyromancer | ordinary | 10.6, L28 | 10.6, L28 | Cinderbreath (8), 0.83 | 18 -> 33 | 886 -> 1500 | x1.69 |

Mean of the three planned runs **x1.39** (1.34-1.44); the ordinary build, taking it the moment it opened
with 18 fuel, x1.69. The gate at 28 puts the planned build's first offer at **10.4-12.2 minutes** (11.0,
10.3 and 11.5-11.7 in the other runs at 28), so three actives and 16 fuel are unchanged.

How it got there, on the same seed: at +1% damage and +1 amount per 10 the Pyromancer was x1.67 and the
Frostweaver x1.47. Taking damage from 1% to 0.5% moved the Frostweaver (whose cone reads no amount) only to
x1.44: with damage at nothing the cooldown and area steps alone hold the main spell near x1.3, and a spell
that reads an amount (Cinderbreath's tongues) well over it - so the amount step went to one per 20 as well,
and the Pyromancer came to x1.40. A third pass halved the area step too (+0.5% to +20%): x1.61 and x1.19, a
mean no different from the pass before. **The runs scatter by about 0.2 either way**, so a cut finer than
this one cannot be told from the noise with single runs; the area step was put back as drafted. If Eric
wants it nearer 1.3, damage at nothing (about x1.3 on these figures) is the next step, then cooldown.

**First measured (2026-11-02), at the draft's numbers (+7%, per 10, level 25): far stronger than 1.3 once it
is pointed at the spell the build carries, and the offer comes about two minutes early.** `_BalanceHarness --seek-sovereign --all-unlocked --immortal`,
seed 11, timescale 3, three runs at a time. The ratio is the loadout's damage a second (every source) over
the 60 s after the Rite against the 60 s before; "share" is the anointed spell's part of the minute before.

| wizard | build | offered (min, level) | taken (min, level) | anointed (its level) | fuel at taking -> 15:00 | dmg/s before -> after | ratio |
|---|---|---|---|---|---|---|---|
| Pyromancer | planned, best spell | 9.1, L25 | 10.9, L29 | Cinderbreath (8) | 16 -> 32 | 1197 -> 3474 | **x2.90** |
| Frostweaver | planned, best spell | 9.3, L25 | 11.8, L30 | Cone of Cold (8) | 16 -> 22 | 1082 -> 2990 | **x2.76** |
| Stormcaller | planned, best spell | 11.2, L25 | 13.5, L29 | Chain Lightning (8) | 16 -> 19 | 941 -> 2160 | **x2.30** |
| Frostweaver | ordinary, best spell | 9.2, L25 | 9.2, L25 | Obsidian Spike (8), share 0.70 | 16 -> 30 | 584 -> 1323 | **x2.26** |
| Stormcaller | ordinary, best spell | 9.2, L25 | 9.2, L25 | Meteor Swarm (7), share 0.35 | 17 -> 32 | 428 -> 734 | **x1.71** |
| Pyromancer | ordinary, best spell | 9.1, L25 | 9.7, L26 | Cinderbreath (8), share 0.83 | 16 -> 24 | 943 -> 2824 | **x2.99** |
| Pyromancer | planned, first offer | 9.2, L25 | 9.2, L25 | Fireball (1) | 19 -> 28 | 653 -> 690 | x1.06 |
| Frostweaver | planned, first offer | 9.2, L25 | 9.2, L25 | Mirefoot (1) | 18 -> 31 | 802 -> 282 | x0.35 |
| Stormcaller | planned, first offer | 8.3, L25 | 8.3, L25 | Magic Missile (3) | 17 -> 25 | 496 -> 543 | x1.09 |

What the table says:

- **Pointed at the spell the build is carrying, it is x1.7 to x3.0 at the gate's minimum fuel (16-17)**,
  against section 4's target of about 1.3. On the numbers, the anointed spell alone gains about x3 at 16
  fuel (Frostweaver's Obsidian Spike: share 0.70, ratio 2.26) and about x5 with an amount it can use (the
  Stormcaller's Meteor Swarm: share 0.35, ratio 1.71). To land near 1.3 at the gate the damage per fuel
  wants to be far lower, or the target read at the end of a run (fuel 30-40) rather than at the offer. This
  was the starting point of the tuning above.
- **The gate makes the obvious target the last to open.** It counts the OTHER spells' levels, so the spell a
  build pours its levels into is the one with the least fuel behind it: at its first offer every run's best
  spell (level 8, beside spells at 1-3) was locked, and only a spell the run had just picked up could be
  anointed - which is the "first offer" rows, a sidegrade or a loss (x0.35 for a level 1 Mirefoot). A
  player who waits for his spell takes it 1.7 to 2.5 minutes later.
- **The offer lands at minute 9.1-9.4 at level 25** (8.3 and 11.2 the outliers), before section 6's 11-12.
  These bots reached level 29-31 by minute 11-12, so that is where the gate would have to move for the same
  bots - but they are immortal, own everything and walk to every orb, and `.ai/run-pacing.md`'s planned bot
  is level 26 at 10.7 and 29 at 12.2. The tuning above moved it to 28.
- **Read the ratio as power, not kill speed.** The bots kill what the spawner sends from about minute 8
  (kills ≈ spawns, 420-450 a minute), and the damage a source is credited with is every hit's full amount,
  overkill included. Against three times the enemies' health (`--enemy-hp=3`), to get off that ceiling, the
  bots levelled too slowly to say: the Frostweaver was first offered it at 13.6 and did not take it before
  the end, and the Stormcaller never reached level 25.
- `--seek-sovereign --policy=ordinary` is the ordinary build; with neither, no policy takes the Rite, so a
  report without the flag plays as it did before the Rite existed.
