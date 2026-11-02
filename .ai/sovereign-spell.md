# The Sovereign Rite: every spell's power poured into one

**Status: approved by Eric (2026-11-02) with one change, being built:** it is a **combination**: the
Rite is one ingredient and the spell you point it at is the other (section 3, as revised).

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
  through `UnlockCatalog`. Nothing on a new save can reach it early.
- **Offered exactly where the Conjunctions are**: in the Ascension strip above the level-up cards, on the
  Conjunction plate, reading **"✶ CONJUNCTION  Sovereign Rite"**, once **all** of these hold:
  - player level **25** or more;
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

| what | per level of fuel | at F = 40 |
|---|---|---|
| damage | +7% | x3.8 |
| cooldown | -0.8% (floor -30%) | -30% |
| area / reach | +1% (cap +40%) | +40% |
| amount (projectiles, strikes, tentacles, where it has one) | +1 per 10 | +4 |

Starting numbers, to be measured (section 6). **The target**: the anointed spell alone deals about
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
