# ADR: The Apotheosis - a wizard's ultimate is the final form of its starting spell

**Date:** 2026-10-10 · **Issues:** #59 (design), #60 (systems), #61 (forms) · **Status:** accepted, built;
**amended 2026-11-02** - the gate is now the spell at max level, ascended and not combined (see the
amendment at the end). Items 2 and 3 below are history.

## Decision

Every wizard has one **Apotheosis**: the final form of the spell they start with, offered in the
Ascension strip above the level-up cards (milestone 9) when four things hold on that spell at once:

1. it is at max level (8);
2. the named level 4 and level 8 evolutions were taken;
3. the wizard's element is at tier 6;
4. the wizard is the one the entry names. The gate is a (character, spell) pair.

Taking it spends the level-up pick. It is terminal: it never levels, it is never re-offered, and
`SpellData.ApplyEvolution` refuses a second one.

`scripts/ApotheosisCatalog.cs` is the single authority on gates and forms.
`RegressionChecks.ValidateApotheosisCatalog` holds the invariants `dotnet build` cannot see.

## Rejected: a hand-cast ultimate on its own button

This game has no reaction inputs. You steer, and everything else fires itself. One button that
matters more than every other input would change what kind of game it is, and it would need its
own meter, its own accessibility toggle and its own auto-cast fallback. A transformation keeps the
auto-shooter contract and puts the decision on a level-up card, where every other build decision
already lives.

## One wizard, one spell, one verb

Each form claims a verb no other form owns, so the six do not collapse into "the big one that does
more damage". The validator enforces it.

| wizard | spell | form | verb |
|---|---|---|---|
| Pyromancer | Cinderbreath | Cataclysm | burn |
| Frostweaver | Cone of Cold | The Long Winter | stop |
| Stormcaller | Chain Lightning | The Lattice | link |
| Geomancer | Obsidian Spike | Sundering | hold |
| Tituba | Riptide | Spring Tide | flood |
| Väinämöinen | Bramble Seed | Wildwood | swarm |
| John Dee | Hollow Star | The Spirit Mirror | pull |

Unclaimed, for wizards still to come: banish, contagion, rewind, displace, reflect. (Pull went to
John Dee on 2026-10-22: Hollow Star is the roster's one spell about pulling, and the collector's
form pulls the loot in with the swarm - `.ai/passives-and-items.md` section 8.)

## Where the build departed from the issues, and why

- **The Pyromancer's form is Cinderbreath's, not Fireball's.** The covenant is the wizard's
  *starting* spell, and the starter rework moved the Pyromancer onto Cinderbreath after #61 was
  written. Cataclysm keeps its verb and its crater chain.
- **Every required path never leads away from the wizard's own element.** #61 named some
  paths that did not follow it: Chain Lightning's `forked_surge` adds no Lightning, and its level 8
  `overcharge` no longer exists. #61's own rule is that a required path must never branch away
  from the wizard's element, so the paths follow the rule rather than the stale ids. (2026-11-01,
  `.ai/chapter-wizards.md` R2: a freed wizard may open on a two-element spell. He claims one of the
  two; his path deepens it at one milestone and takes the untagged branch at the other, which the
  check has always accepted. A pure opener's path still deepens at both.)
- **Sundering grants armour, not flat reduction.** Flat reduction left the game, and armour is one
  percentage stat (`Player.GetArmorPercent`). The ring adds 20% inside it, under the same cap.
- **Two forms were added for wizards #61 did not know about**: Spring Tide (flood) and Wildwood
  (swarm), both verbs from #59's unclaimed list.
- **Level 8 is "Ascension", the final form is "Apotheosis".** Level 8 used to say "ULTIMATE
  ASCENSION", which made two "ultimate" tiers one level apart.

## Traps, and what holds each one

- **The element rug-pull.** A form only adds +1 to the wizard's own element (validated), so
  accepting it can never drop the player out of the tier that qualified them.
- **The two-element ceiling.** The validator counts base, path and form together - over every level 4 x
  level 8 pairing since the 2026-11-02 amendment, when any path can come before the form.
- **A dead offer.** It sits in the strip above the cards on every level-up once earned, and the
  strip also guarantees the level 8 Ascension the path needs can never be skipped by levelling fast. Whether tier 6 is
  reachable at all was measured, not assumed (below).
- **Chain reactions in a swarm.** Cataclysm's craters and The Long Winter's shatters are queued
  and resolved a few per frame, never inside the kill that caused them. Craters are pooled. A shard
  can freeze an enemy at most once per 2.5 s, so a wave cannot become a permanent freeze lock. The
  Lattice caps edges per node, not total nodes.

## Measured

See `.ai/gameplay-design.md`, "The Apotheosis", for the clock times the harness recorded
(`_BalanceHarness --seek-apotheosis --all-unlocked --immortal`).

## Amendment (2026-11-02): max level, ascended, not combined

Eric: "Apotheosis should require the spell to be at Max level, not combined with another spell, and
ascended."

The form is offered when, on the wizard's own starting spell (item 4 above is unchanged):

1. it is at its max level (8);
2. it is **ascended** - it took a level 8 Ascension, **any** of them;
3. it was **not combined** with another spell - it never went into a Conjunction (`ComboCatalog`), and
   a Conjunction's own spell is never eligible;

and the player is at level 21 (`ApotheosisCatalog.MinimumPlayerLevel`, below; it was 23). The named level 4 and
level 8 paths (item 2 of the decision) and the element at tier 6 (item 3) are gone, with the fields
that carried them (`RequiredLevel4EvolutionId`, `RequiredLevel8EvolutionId`, `RequiredTier`);
`RequiredElement` is now `Element`, the element the form deepens, which only the validator reads.
`ApotheosisCatalog.SpellMeetsGate` is the predicate; `Player.TryBuildApotheosisOffer` adds the level.

**Why.** A named path made the form something one click could lose for the rest of the run: take the
other mutation at level 4 and the final form was gone, minutes before a player could know it existed.
Tier 6 asked for a whole book of one element on top of the Ascension's own element gate, and it is
what put two wizards' forms in the last minutes of the before-runs below. Now every route through the
spell's tree ends in its form, and the decisions at level 4 and level 8 are about the spell again.

**What moved with it.**

- **"Every required path never leads away from the wizard's own element"** (the departure above)
  retired with the paths. What it protected is held another way: no level 4 or level 8 option on a
  wizard's spell may trade his element away (`ReplacesElement`) - the form would put it back as a
  third - and the two-element ceiling is checked over every pairing with the form on top.
- **Not combined is guaranteed twice.** A wizard's own spell is never a Conjunction ingredient for him
  (`ComboCatalog.Evaluate`, `ComboSeal.OwnSpell`), so today it is never fused. The gate asks anyway, so
  a change to that rule cannot hand a fused spell its form. `ValidateApotheosisCatalog` checks both,
  and that the gate opens at max level down every Ascension and stays shut a level short, unascended,
  or fused.
- **On screen:** the Codex's Apotheosis node reads **Spell 8**, **Ascended**, **Not combined** and
  **You 21** (in a run, measured: "Spell 7/8", "You 19/21"); the L4/L8 path chips and the element chip
  are gone. The ember line runs along every open route into the form, not one named path.
- **A branched spell keeps its branch's colour under the form** (`SpellData.GetModulateColor`): the
  form can now follow an element path, and the element the player chose should not vanish.

**A player level stays, at 21 rather than 23.** Eric did not name it, and it was to go unless the form
then came before the floor (7.5 minutes for the bot, `.ai/gameplay-design.md`). It does: the spell's
half of the gate is met when the spell ascends, the form would come on the next level-up, and the
Ascension's own gate (level 17) puts that at 6.5-8.0 minutes. With the level now the only thing
setting when, it was re-tuned to the floor by the run-pacing recipe (the lowest level whose fastest
arrival clears it): level 20 came as early as 7.8, 21 at 8.6 at the earliest, 23 at 9.5-12.1 - the
economy has slowed since 23 was set against a 7.8-9.1 arrival. `.ai/run-pacing.md` section 11 has the
runs; in short (`_BalanceHarness --seek-apotheosis --all-unlocked --immortal --minutes=15`,
`--fixed-fps 30 --timescale=1`, the Forest, minutes the form was taken):

| wizard | before (named path, tier 6, level 23) | after (max level, ascended, not combined, level 21) |
|---|---|---|
| Pyromancer | 10.3 | 8.7 |
| Frostweaver | 14.3 | 10.8 |
| Stormcaller | 13.7 | 14.2 (his spell ascended at 13.3 under both: the upgrade roll) |
| Geomancer | 10.6 | 8.6 |
| Tituba | - | 8.6 |
| Väinämöinen | - | 9.0 |
| Seimei | 10.5 | 8.7 |
| Pyromancer, planned bot | 10.1 | 9.0 |

With no level gate at all, six of the eight would have taken it at 6.5-8.0, five under the floor.
