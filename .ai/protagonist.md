# The Archmage: the one he missed, playable

Eric, 2026-11-02:

> "Let's also make the blue robed character the fifth starting character - his starting tag alignment is
> arcane. If no one's starting spell is magic missile, that will be his."
>
> "He should be the first one on the list on the character select screen."
>
> "Magic missile should also change to pure arcane if it's not."

His name is **The Archmage** (Eric's, the same day). The game is *The One He Missed*; the wizard has a name of
his own, and the story still calls him the one he missed.

**Status: built on `wip/protagonist` for Eric's review.** Section 7 says what was measured, section 8 what is
open.

Read with: `.ai/world-and-tone.md` (the premise, and the player on the 48 cell), `.ai/merlin.md` (who had this
figure for an afternoon, and why he does not), `.ai/decisions/2026-10-10-apotheosis.md`, `.ai/slow-start.md`,
`.ai/spell-roster.md` (Magic Missile's section, 2h).

---

## 1. Who he is

The blue-robed figure in the warded circle on the title screen, the one hiding among the stones in the
prologue: the player, the one wizard the dark wizard did not take. *"You are the one he missed"*
(`.ai/world-and-tone.md`). He was to stay story only - title, prologue, lessons - and Eric reversed that the same
day: he is playable from the first run, the fifth starter, first on the character select.

He is **not** a discipline (the Pyromancer, the Frostweaver, the Stormcaller, the Geomancer are roles the player
steps into) and **not** a prisoner (the rescued are named people with a cell somewhere). Nobody took him, so
nobody frees him: he is simply yours from the start. He is **not Merlin** either: Merlin was given this figure
for an afternoon (`bfada4df`), Eric took it back ("The blue robed wizard should represent player"), and Merlin
has a look of his own and keeps it (`.ai/merlin.md` section 5).

The story reels of him - the prologue, and him freeing the four starters - are another branch's
(`scripts/Prologue.cs`, `tools/art/prologue.py`, `.ai/prologue.md`); nothing here touches them.

## 2. Where he lives

| piece | where |
|---|---|
| him | `CharacterData_Archmage.tres`, id **`archmage`** (not `the_one_he_missed`, which is Merlin's deed, The King-Maker), name "The Archmage", health and speed x1.0 |
| the roster | `CharacterRoster`, **index 14, appended** - a saved index and every tool's `--character=N` keep their wizard |
| first on the screen | `CharacterRoster.SelectionOrder()` (`ShownFirst = { "archmage" }`, then roster order) lays out the character select; `CharacterSelection.FirstSelectableIndex` walks it, so a fresh launch opens on him. A display order only: nothing stores a position in it |
| every save holds him | `UnlockCatalog`: `Starter("archmage", Character)`. A starter is unlocked by the catalog, not the save (`GlobalStatsManager.IsCharacterUnlocked`), so an old save has him the moment it loads: **no schema step**. Every gate counts a starter (no rescue chapter), and every save holds him, so no gate moves |
| his look | `tools/art/player_archmage.py` -> `assets/bonelight/characters/archmage-*.png`, `scenes/resources/ArchmageFrames.tres` (section 5) |
| his staff | `CharacterVisuals.GetStaffTipCell("archmage")` = **(13, 6)**, the orb in the gold claw; untinted (`GetCharacterTint`) |
| his passive | `missed_once`, Overlooked: `Player.CharacterPassiveBonusIds`, `ApplyCharacterPassiveBonus`, the hook in `Player.TakeDamage` (section 3) |
| his form | `ApotheosisCatalog` (verb **count**), `MagicMissile.NotOneMissedId` and the count's numbers, the count itself in `Player.CastSpellNow`'s missile loop (section 4) |
| the harness | `_BalanceHarness` prints `HARNESS COUNTS` and `HARNESS OVERLOOKED` once either has happened |

The starter rules hold without a change to any validator: he opens on one element, Arcane, which no other
starter claims (`ValidateStartingCharacters`), on a spell no other character opens on (the Test Wizard is
exempt), and a starter needs no epithet (`QuestLog.Prisoners` names the freed). The slow start reads no wizard:
his book is three pages like anyone's, Magic Missile one of them.

## 3. His passive: Overlooked

*"Once a run, the blow that would end you misses."*

The axis is **one death, refused**, and nobody else stands on it. The thirteen others: spell damage (the
Pyromancer), regeneration (the Frostweaver), cooldowns (the Stormcaller), armour (the Geomancer), duration
(Tituba), area (Väinämöinen), a relic more in chests (John Dee), bolts caught (Laelaps), damage to the strong
(Seimei), fear (the Dark Wizard), a shove (Merlin), move speed (Wayland), rerolls (the Sibyl). It is not armour
(every hit lighter), not evasion (any hit, by chance) and not a heal (`.ai/difficulty-curve.md` holds healing
scarce). It is the premise as a rule: he was missed once, and he is missed once more.

How it works (`Player.TakeDamage`): after armour and the shield, so only a blow that would really kill is turned;
before any HP leaves, so he keeps the little he had - nothing is healed. Then two seconds of grace, drawn white
(`OverlookedGraceSeconds`), because the crowd that landed the blow is still on him and the next touch would end
the run anyway. The dodge's ghosting and sound mark it. It comes before an extra life, which is kept for later.
The `DamageTaken` signal has already gone out: he was struck at, as a dodge is not. The character select's line
takes the evasion glyph (`StatIcons.KeyFor`: " misses").

## 4. His spell and his form

### Magic Missile, pure Arcane

The homing violet dart at the nearest enemy (`.ai/spell-roster.md` 2h). Nobody opened on it - the Test Wizard
is a sandbox - so it is his, per Eric. One dart every half second at level 1, and still one at level 8 (its
levels add damage, a pierce at 5 and speed at 7; more darts come only from Amount, the shop's and the relics'):
the fewest things on screen of any opener, which a new player's first minute wants. The dart now leaves **from the light on his staff** (`StaffTipLocal`), as
Cinderbreath is poured from it - for every wizard who holds it, the body's centre for one without a staff head,
the cat itself when the Black Cat casts it.

It was **Arcane + Lightning**; it is **Arcane** (`SpellData.tres`). What that moved, everywhere it was read:

| what | before | now |
|---|---|---|
| the tag a held Magic Missile counts | Arcane 1, Lightning 1 | Arcane 1 |
| attunement (`Player.GetAttunementMultiplier`) | none: a hybrid is owed none | **attuned to Arcane**, +20 / 45 / 80% damage at Arcane tier 2 / 4 / 6, until a branch adds Lightning. The largest balance change here; measured in section 7 |
| the level 4 element path | none (a hybrid deepens its halves) | **Twin Volley +Lightning** - a pure spell's level 4 must offer a branch into a second element (`ValidateElementPaths`), and the fork is forked lightning. Its card forks "like lightning" and its chip row shows the Lightning count, it wears Lightning's tint, its icon was redrawn in Lightning (`evolution_icons.py`). The card's chip reads "attunement lost" |
| Unstable Detonation (+Arcane) | +12% cooldown, the price a hybrid pays for a tag (`AttunementGainCooldownCost`) | **free**: a pure spell's deepening is its compensation; the chip reads "stays attuned" |
| Overcharge (L8, +Lightning) | deepened Lightning | unchanged, and now **the one level 8 on a pure spell that may add an element**: it deepens the path's Lightning after Twin Volley and branches into it after the other two. Never three elements. Its icon's dart is re-lit in Lightning, as the icon rules re-light a branch |
| the Ascension's element gate | Overcharge 4 Lightning; Arcane Orb (untagged) 4 Arcane **or** 4 Lightning | Overcharge 4 Lightning; Arcane Orb 4 Arcane unbranched, Arcane or Lightning after Twin Volley (`Player.GetAscensionGateElements` reads the spell's tags as they stand) |
| Sympathetic Blast (Arcane Explosion's L4) | a blast Magic Missile set off went off in Lightning (staggers) | in Arcane (30% harder) unbranched, Lightning once branched (`ArcaneExplosion.SympathyElement` reads the charger's tags as they stand) |
| Conjunctions, achievements, taglines | - | no recipe names Magic Missile, no deed counts its Lightning, its tagline names no element: nothing to move |
| the cast sound (`SfxCatalog.DominantElement`) | Arcane (ties went to it) | Arcane |
| `Player.CreateFallbackSpellData` | Arcane, Lightning | Arcane |
| `ValidateElementReachability` | Lightning's best from it: 2 (base and Overcharge) | 2 (Twin Volley and Overcharge): Lightning's capstone is as reachable as it was |
| `tools/art/evolution_catalog.json` | Arcane, Lightning | refreshed by `_AscensionAudit --json` (it also picked up Twin Orbs' current wording, which had drifted) |

No level 4 or level 8 option trades Arcane away (`ReplacesElement`), so every route ends in the form and the form's
+1 Arcane never makes a third element (`ValidateApotheosisCatalog`).

### Not One Missed (verb: count)

*"Every 4th volley is a count: a missile at every enemy in reach, up to 16, one each."*
Synergy: *"Count: the bigger the crowd, the bigger the count. Nobody in reach is left out."*

He was the one left out of the dark wizard's count. His final form leaves nobody out of his: every fourth volley
that flies is a **count** - a dart at every enemy in the spell's reach, nearest first, one each and none twice, up
to **16** (`EnemyTargeting.MaxPick`, the most a nearest-first pick returns), or **6** under Arcane Orb (sixteen
two-second orbs at once buried the field). The darts leave fanned 40 degrees off their marks, alternately to
either side, and curve in, so the count opens like a hand before it closes; a burst of arcane sparks leaves the
staff with it. The first volley after the form is taken is a count, so the pick pays where it is seen. The clock
only moves on a volley that flew.

**Why count.** Taken: burn, stop, link, hold, flood, swarm, pull, reflect, banish, take, temper, echo; held:
rewind (Merlin), sever (Nimue); planned: feed, turn, distil, blind, displace, double, foresee; and the prison asks.
Count is none of them. It is a **targeting** verb - it changes who is struck, never what a strike does - so it
pulls nothing, links nothing, echoes nothing and doubles nothing, and every card on the spell rides it unchanged:
a count of Twin Volley darts forks, a threaded count stitches, an Overcharged count sparks every body it touches.
A boss is struck once, like anyone. Considered and passed over: *find* (too near Threaded Needle's ricochet),
*free* (light coming home from kills is The Taking's form), *gather* (cosmetic over the same volley).

The gate is everyone's (`ApotheosisCatalog.SpellMeetsGate`): Magic Missile at level 8, ascended down either
branch, never combined (no recipe takes it), and player level 21. The card: +25% damage, +1 Arcane, the usual
1.15 lift (Scale reaches the dart, `EvolutionStatReaders`), tint (0.86, 0.80, 1.0). Its icon,
`magic_missile_not_one_missed.png`, is six darts leaving the light in the middle for six marks
(`evolution_icons.py`).

## 5. Art

**The title figure on the 48 cell, recovered, not redrawn.** `tools/art/player_archmage.py` is
`bfada4df:tools/art/player_merlin.py` - the generator that drew this figure for Merlin from `hero.wizard_hero` at
title defaults: the wool robe deep and in shade, the cone hat with its gold band and swept tip, the scalloped
mantle, the mane-spear beard, the red sash, the cloak swept off-axis, and the tall gold forked staff with the
Light orb in its claw setting the top of the silhouette. Only the names changed, and the sash's control
characters: `bfada4df` put the `red` row on `\x0e-\x12`, and `\x10-\x13` have been Wayland's metal since, so a hurt
flash would have walked the sash along the hammer's ramp. It uses `\x14-\x18`, the bytes `merlin_concepts.RED_RAMP`
holds for the same row, already in `anim_sets.PLAYER_RAMPS`. **Nothing new is spoken for**; `\x8b-\x8f` stay free.

Checked: every one of the eight PNGs it writes is **pixel-identical** to `bfada4df`'s `merlin-*.png` (0 differing
pixels). 786 px, **0.85 edges/px** (the floor is 0.8), the orb's centre (13.4, 5.8). Idle (frame one is the
portrait; the orb pulses), an eight-frame walk, hurt and death. `hero.py` and the title art are untouched.

## 6. Shots

`_shots/protagonist/` (copies of `user://`):

- `protagonist-select-pc.png` (`--resolution 1280x720`) and `protagonist-select-phone.png` (`--resolution
  540x1212 -- --portrait --safe-area`), both `--fresh-save`: him first and selected - the detail pane open on him
  on the PC, the focus ring on his row on the phone.
- `protagonist-run-pc[-1..3].png`: level 1, `--character=14 --spell-stage=6 --cast-on-stage --burst=4,5`, one dart
  leaving the staff's orb.
- `protagonist-form-pc[-1..5].png`: level 8 down Twin Volley and Overcharge with the form, a 16-strong stage; the
  first frame is the count leaving.
- `protagonist-form-orb-pc[-1..3].png`: down Threaded Needle and Arcane Orb: a count of six orbs.
- `protagonist-mutation-pc.png` (`--show-mutation`): his level 4 - Twin Volley with its Lightning, Threaded
  Needle, Unstable Detonation. The three cards scroll on the 1280x720 canvas (489 of 439): Unstable Detonation's
  two-line name and six-line text make it the tallest, and this branch did not change its size (its chip row was
  there before, priced "+12% cooldown"; it reads "stays attuned" now). Twin Volley's text was cut to keep it
  from growing past it.

## 7. Measured (2026-11-02)

`_BalanceHarness`, the Enchanted Forest, seed 1234 unless marked, `--no-save-writes` on every run.

**The plain 8 minutes** (`--minutes=8 --character=14`, the machine's save, the kiting bot): alive at 8:00, level 17
(L10 at 5.3, L17 at 7.8), 76 HP lost, 11,211 damage a minute, Magic Missile at level 3 doing 55.6% of it (Scorching
Ray 21.8%). The bot gathered no XP for two minutes - his kills fall 500 units off, where the kiting bot never walks
- and caught up from minute 3.

**A new player** (`--fresh-save`: three pages, nothing owned, no Ascension). The mortal kiting bot dies on a fresh
save whoever it plays (the harness says so of itself), so the four were run beside him the same way:

| run | The Archmage | beside him |
|---|---|---|
| kiting bot, mortal, 8 min | died at 2.44 (level 4; Overlooked turned a blow at 2:24); 1,294 dmg/min | the Pyromancer died at 1.70 (618 dmg/min), the Stormcaller at 1.98 (827) |
| `--survivor`, mortal, 8 min | died at 3.17 (level 6; Overlooked at 3:06); 1,619 dmg/min | the Pyromancer died at 2.84 (1,506) |

The measure that can say whether a new player keeps up is the slow start's cell A (`.ai/slow-start.md` 10.4:
`--fixed-fps 30 --minutes=19 --fresh-save --survivor --immortal --policy=ordinary --stop-overrun=60`): the bot
plays the whole Forest and the shadow ledger says how much health it would have needed. No run overran, every
page opened at 10.2-10.4. "Need" is that health multiple; "HP lost" what the immortal bot lost:

| wizard, seed | boss down after | level at 15:00 | HP lost | need | Marginalia | his own spell's share |
|---|---|---|---|---|---|---|
| **The Archmage**, 1234 | 0.61 | 44 | 518 | **x7.7** | **10** | Magic Missile 49.9% (book: MM, Arcane Explosion, Cone of Cold, Glacial Spike) |
| **The Archmage**, 99 | 0.52 | 38 | 653 | **x12.1** | **5** | (book: MM, Arcane Explosion, Cinderbreath, Aegis Ward) |
| The Archmage, 1234, **Magic Missile back to Arcane + Lightning** (its resource only) | 0.81 | 35 | 960 | x17.9 | 0 | Magic Missile 29.9% (book: MM, Chain Lightning, Cone of Cold, Hollow Star, Cinderbreath) |
| the Stormcaller, 1234 | 1.24 | 34 | 1,427 | x26.3 | 1 | Chain Lightning 52.1% |
| the Pyromancer, 1234 | 1.46 | 32 | 920 | x18.9 | 0 | - |

So he survives and levels on the three-page book better than the starters beside him: he lasts longest wherever the
bot can die, and needs about a third of the Stormcaller's health to live through the Forest. The dart's reach is
why - what is dead at 500 units never touches him. **The ten Marginalia break the slow start's rule 1** ("deals
more than three Marginalia"): he wrote his book out by about level 36, and a gleaning bell at minute 13 took him
from 30 to 38 in one minute (his kills leave their orbs far off, so a bell gathers more of them). Eric's call
(section 8).

**The Apotheosis** (`--minutes=15 --seek-apotheosis --all-unlocked --immortal`): Magic Missile ascended down Arcane
Orb at 6.1 min (level 17), **Not One Missed was offered and taken at 8.2 min** - over the 7.5 floor - and the run
ended at level 32, Arcane 9 (attunement +80%), with no script errors. 507 counts in the 6.8 minutes after it, 5.8
missiles a count (orbs, capped at 6). The two references on the same tree and flags (run side by side; he ran
alone):

| wizard | form taken | damage a minute | his own spell's share |
|---|---|---|---|
| **The Archmage** | **8.2** | **33,087** | Magic Missile 89.8% |
| the Cumaean Sibyl (the other pure Arcane opener) | 9.1 | 23,695 | Arcane Explosion 85.3% |
| the Pyromancer | 9.3 | 28,229 | Cinderbreath 82.9% |

His worst frame was 100 ms; theirs are not comparable, two Godots at once.

He is the strongest of the three by a sixth over the Pyromancer and two fifths over the Sibyl, and the earliest to
his form. The bot always took Arcane Orb (it plays an Arcane focus, and Overcharge asks 4 Lightning), so the
sixteen-dart count down Overcharge is measured only by the shots (section 6) - and its cost in a crowd of 140 is not
measured at all.

**What pure Arcane did to Magic Missile itself** (the third row above): with its resource put back to Arcane +
Lightning and everything else as it is, the same run lost 960 HP rather than 518, needed x17.9 rather than x7.7,
ended at level 35 rather than 44 with no Marginalia, and Magic Missile did 101,553 rather than 191,433. Not all of
that is the attunement: the tags also steer the bot's picks, and pure Arcane drew Arcane Explosion into the book
where the hybrid drew Chain Lightning. But the direction is plain - **pure Arcane is what lifts him from the
starters' band (x18-26, Marginalia 0-1) to above it**, and as a hybrid he would sit in it.

## 8. Open for Eric

1. **The passive.** Overlooked is the premise as a rule. Its alternates, each also on an axis nobody holds:
   **+10% experience** (the ember that catches: he was too small to be worth taking, and grows fastest - but XP
   moves the slow start's pages and the level 21 gate), or **pickup range** (he gathers the light the others lost).
2. **Strength.** The strongest opener on a fresh save (need x7.7-12.1 against the starters' x18-26) with five and
   ten Marginalia against the slow start's three, and the strongest of the three Apotheosis runs (section 7). The
   A/B says the fresh-save lift is pure Arcane's attunement, not his kit. Levers, smallest first: Magic Missile's
   base damage 10 -> 8 (everyone's dart); for the form alone, a count every 5th volley, or a cap of 12. None
   touched without your word.
3. **Magic Missile is stronger for everyone in an Arcane build** - attuned, +20 / 45 / 80% at tier 2 / 4 / 6 - and
   Unstable Detonation 12% faster. Twin Volley now costs that attunement for its Lightning tag.
4. **Overcharge is the one level 8 on a pure spell that adds an element** (`.ai/spell-roster.md` 2b says "Level 8
   only deepens"). Kept because the card IS lightning, and the rule given with this change lets a level 4 or
   level 8 option add an element so long as none trades Arcane away; the alternative is Overcharge +Arcane, which
   would leave Lightning reachable from the spell only through Twin Volley.
5. **Every wizard's missiles now leave from his staff's head**, not his chest. It reads better on all of them; say
   if it should be his alone.
6. **Names.** The Archmage beside the Feat **"Archmage Training"** (reach level 20, pays Solar Flare): two names a
   word apart. The passive's name, Overlooked; the form's, Not One Missed (the title, answered).
7. **Index 14** is his. A chapter wizard merged before this one would take it; whichever lands second moves to 15
   (`CharacterRoster`, and this doc's `--character=14`).
8. **Magic Missile's level 4 screen scrolls** on a 1280x720 PC (489 of 439): it did before this branch too (shot at
   `6ceeaf20`'s catalog: the same 489), Unstable Detonation's card being the tallest. Not this change's; noted.
9. **Eyes:** the count over a real crowd on a phone (the fan, the spark flare, sixteen darts with Twin Volley's
   forks and Overcharge's bursts on top - the first form shot is busy); whether a whitened two seconds reads as
   "missed"; the form icon at 24 px.
