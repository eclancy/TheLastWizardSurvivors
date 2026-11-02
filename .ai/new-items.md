# New items: a batch of relics, sets and boons

**Status: proposal for Eric (2026-11-01). Nothing here is built.**

Eric: *"a bunch of new items"*. Today there are 34 chest relics in 11 Full Set Enchantments
(`scripts/ChestItemCatalog.cs`) and 15 boons (`scripts/BoonCatalog.cs`). Two written-down sources
were unbuilt:

- **Issue #69**: chest items that interact with Vulnerable, Wither and Weaken. Its other bullets are
  done: the tell on a debuffed enemy (`StatusEffectLayer`) and Fear (`gameplay-design.md`,
  "Status effects"). `SpellEffect.SpawnMinions` is a spell question and is not answered here.
- **`.ai/spell-variety.md` section 6**: what to take from the genre: rule-changers, a curse, partial
  sets, items as keys. Section 11 below says which were taken and which were not.

This proposes **17 items**: 12 relics making up **four new Full Set Enchantments**, and 5 boons.

---

## 0. Questions for Eric

1. **Every relic must be a piece of an object** (#63, `ValidateFullSetObjects`), so twelve new
   relics come as **four** new sets, not one or two. Keep that rule, or let a few relics stand
   alone? *Recommended: keep it. It is the rule that makes two pieces tell you what the third is.*
2. **Five new boons** take the boon draw from 15 to 20, so each existing boon is dealt about a
   quarter less often (Water and Grass builds lean on theirs). Build two now (Waywiser, Bone
   Rattle) and the rest later? Each also needs a source. *Recommended: Waywiser is a starter,
   Bone Rattle and Guttering Candle are chapter deeds, Straw Skep and Tally Stick are sold in
   Powers.*
3. **Twelve more relics make every set harder to finish**: any one piece is offered about a quarter
   less often. Add a small nudge toward sets you have started (x1.5) to every chest, with the
   Gaming Board's x3 on top of that? *Recommended: yes, after the chest simulation (section 10).*
4. **Damage avoided against "damage stays taken".** Binding Thread (Weakens whatever touches you)
   and Bone Rattle (frightens what is near when you are struck) are the first new answers to chip
   damage since 0.10.0. Ship them at 20% and once a second and then measure HP lost, or start them
   weaker?
5. **The Poppet** is a cursing doll: pins, binding thread and a singed lock of hair. Does that fit
   the tone, or should the debuff set be a different object?
6. **The Eagle Ampulla** names the Sovereign Rite on its card. Hide that sentence until the save
   has unlocked the Rite? *Recommended: yes. Until then the card shows "???", as the Codex does.*
7. **First wave**: the Poppet, the Gimmel Ring, Waywiser and Bone Rattle (eight items, section 10)?

---

## 1. What this batch is written against

- **Every relic is a piece of one real object, and the set is that object assembled** (#63,
  `passives-and-items.md` 5). A piece is named as a part of the object, so holding two pieces tells
  you what the third is. The only exceptions are the two tome relics. Each new set has three
  pieces, a partial bonus at two pieces worth about a third of the full one, and a full effect that
  stacks on the partial and is never refunded.
- **Rarity does three jobs that move together**: how often a relic is offered (1.0 / 0.55 / 0.22
  / 0.08), how big its effect is, and its tags. **Common and Uncommon relics carry no tag. A Rare
  carries one, and a Relic one or two.** A completed set grants two tags. Neither counts toward
  `ValidateElementReachability`, so the floor is spells and boons only.
- **Boons**: there are six slots, a boon never levels so it must be worth taking the moment it
  is offered, it has two tags and never the same element twice, it has exactly one source
  (`ValidateBoonSources`), and its icon needs an `ELEMENT` entry in `boon_icons.py`. The draw is a
  chance (`BoonOfferChance` 0.22) and then a choice among the boons, leaning toward the player's
  deepest element, so **every new boon dilutes the others**.
- **Healing is scarce on purpose** (`difficulty-curve.md` 5). **No item here heals**, and none
  touches the flask, regeneration, Light lifesteal, the overheal shield or the Gleaning Bell.
- **Bosses**: no execute ever hits a `BossEnemy`. Crowd control on a boss is clamped: a slow stops
  at 0.5, a hold lasts at most 0.75 s, a shove is at most 0.4 of its walk. Fear never lands on an
  elite or a boss. Wither stops at 8% and Weaken at 25% on a boss, and Vulnerable at +60% on
  anything.
- **One definition each**: "standing still" is `Player.IsStandingStill`, which is intent, not
  displacement. Reactions share `BoonReactionCooldownSeconds` (1 s) and `BoonReactionRadius`
  (130). Statuses go through `Enemy.Apply*`, and an element's status is `ElementPaths.RiderFor`.
- **Armor never takes a 1-damage touch below 1**, so no defensive item here is Armor. Against chip
  damage the answers are Weaken (rounded stochastically, so some touches land for 0), evasion,
  Fear and a shield.
- **Names are ours.** Borrowed mechanics take their shape only, never a name or a picture. Every
  name was checked against the code and `.ai/` and is unused. ("The Adder" is a Broken Circle
  enemy, which is why the low-health boon is not a fang.)

---

## 2. The batch at a glance

"The pair" means **your two highest-level spells, not counting your wizard's own** (section 4).

| # | name | kind | rarity | tags | card | cost |
|---|---|---|---|---|---|---|
| 1 | Pricking Pin | relic, the Poppet | Uncommon | - | Critical hits make their target Vulnerable: +20% damage taken for 3 s. | S |
| 2 | Binding Thread | relic, the Poppet | Uncommon | - | An enemy that touches you is Weakened: it deals 20% less for 3 s. | S |
| 3 | Singed Lock | relic, the Poppet | Rare | Fire | Every kill Withers the enemies within 90 of it by 10% of their max health. | S |
| 4 | Betrothal Hoop | relic, the Gimmel Ring | Uncommon | - | Level-ups offer upgrades to your two highest-level spells (besides your wizard's own) twice as often. | M |
| 5 | Fede Hoop | relic, the Gimmel Ring | Uncommon | - | Your two highest-level spells (besides your wizard's own) deal +30% to an enemy the other hit in the last second. | S |
| 6 | Garnet Bezel | relic, the Gimmel Ring | Rare | Lightning | Each cast of your two highest-level spells (besides your wizard's own) takes a fifth of its cooldown off the other's. | S |
| 7 | Gaming Board | relic, the Gaming Box | Common | - | Chests offer pieces of a set you have begun three times as often. | S |
| 8 | Bone Dice | relic, the Gaming Box | Uncommon | - | +3 rerolls and +1 ban for the rest of the run. | S |
| 9 | Dice Cup | relic, the Gaming Box | Rare | Wind | Once in every chest, shake the cup: its relics are dealt again. | M |
| 10 | Anointing Spoon | relic, the Ampulla | Uncommon | - | Your wizard's own spell deals +20% damage. Once it has taken its Apotheosis, your other spells do instead. | S |
| 11 | Ampulla Beak | relic, the Ampulla | Rare | Water | Damage past a kill pours into the nearest enemy within 120. | S |
| 12 | Eagle Ampulla | relic, the Ampulla | Relic | Wind, Fire | Your highest-level spell deals +15% damage. Once the Sovereign Rite anoints a spell, it carries the status of every element its silenced spells carried. | M |
| 13 | Waywiser | boon | - | Wind, Lightning | While you walk, whichever of your spells is furthest from ready recharges 50% faster. | S |
| 14 | Guttering Candle | boon | - | Fire, Wind | Below 35% health, all your spells recharge 20% faster. | S |
| 15 | Straw Skep | boon | - | Poison, Grass | Your spells deal 2% more damage for every enemy within 160 of you, up to 24%. | M |
| 16 | Bone Rattle | boon | - | Darkness, Lightning | When you are struck, every enemy close around you is frightened (at most once a second). | S |
| 17 | Tally Stick | boon | - | Arcane, Lightning | Collect 20 XP orbs in an unbroken run: your spells recharge 30% faster for 4 s (at most every 10 s). | M |

| new set | pieces | two pieces | all three | tags |
|---|---|---|---|---|
| **The Poppet** | Pricking Pin, Binding Thread, Singed Lock | Your debuffs last 20% longer. | Your debuffs last 50% longer, and an enemy carrying two different ones is also Vulnerable +15%. | Poison, Grass |
| **The Gimmel Ring** | Betrothal Hoop, Fede Hoop, Garnet Bezel | Your two highest-level spells (besides your wizard's own) deal +8% damage. | They deal +25% damage, and a Conjunction is always one of them. | Water, Lightning |
| **The Gaming Box** | Gaming Board, Bone Dice, Dice Cup | One chest in three holds a piece of a set you have begun. | Every chest holds at least one piece of a set you have begun, while one is left to find. | Wind, Lightning |
| **The Ampulla** | Anointing Spoon, Ampulla Beak, Eagle Ampulla | Your Ascended spells deal +6% damage. | Your Ascended spells deal +15% damage; an Apotheosis or a Conjunction deals +30% instead. | Light, Water |

What each kind of item asked for is covered by:

| kind | items |
|---|---|
| debuffs (#69) | the Poppet (Vulnerable, Weaken, Wither, and the full set joins them), Bone Rattle (Fear), Eagle Ampulla (every status at once) |
| rule-changers | Ampulla Beak (overkill spills), Fede Hoop and Garnet Bezel (two spells that answer each other), Pricking Pin (crits expose), Dice Cup and the Gaming Box (what a chest may hold) |
| conditional power | Waywiser (walking), Guttering Candle (low health), Straw Skep (a crowd near). Standing still is already the Brazier's and Taproot's |
| economy and pickup | the Gaming Box (chests and level-ups), Tally Stick (the orb streak) |
| Conjunctions, the Apotheosis, the Sovereign Rite | the Gimmel Ring (Conjunctions), Anointing Spoon (the Apotheosis), Eagle Ampulla (the Rite), the Ampulla set (all the final forms) |

---

## 3. The Poppet: the debuffs (#69)

A cursing doll of rag and straw: pins through it, red thread round its wrists, a lock of singed
hair tied at the crown. Each piece is one of the three things you do to a poppet, and each is one
of the three statuses #66 added and #69 asked items for.

**Pricking Pin** (`pricking_pin`) - Uncommon, no tag.
- Card: *Critical hits make their target Vulnerable: +20% damage taken for 3 s.*
- Mechanic: in `Player.DealDamageToEnemy`, after a hit lands with `isCrit`, call
  `Enemy.ApplyVulnerable(0.20f, 3f)`. The crit itself is not raised; the hits after it are. Because
  Vulnerable is stacking-resistant, it never weakens a stronger mark such as Shadow Bolt's.
- Why: it ties crit builds (Copper Coil, Rod Tip, Luck, the Lightning Rod) into the debuff family
  without any Darkness spell. Paired with the **Whetstone**, which always crits an enemy at full
  health, every enemy you open on is marked. That is the strongest combination in this batch, and
  the +60% cap and the 20% size bound it.
- Art: a long iron pin with a bone bead for a head, point darkened. It shares the doll's red thread
  wound below the bead, which is the family mark.

**Binding Thread** (`binding_thread`) - Uncommon, no tag.
- Card: *An enemy that touches you is Weakened: it deals 20% less for 3 s.*
- Mechanic: every contact hit an enemy resolves against the player (the HurtBox contact set,
  `overlappingEnemies`, which Iron Fang already reads) calls `ApplyWeaken(0.20f, 3f)` on it.
  `ScaleOutgoingDamage` rounds stochastically, so a fifth of that enemy's 1-damage touches land for
  nothing and skip `DamageTaken` as a dodge does. A boss is capped at 25% by `MaxWeakenFraction`.
- Why: it is the only new answer to chip damage, which Armor cannot touch, for a player who lets
  the crowd reach him (Taproot, the Brazier, Straw Skep, Iron Fang). Light's Weaken leaves the
  Solar Flare build for the first time.
- Art: a twist of waxed red thread knotted three times round a pair of cloth wrists.

**Singed Lock** (`singed_lock`) - Rare, **Fire** (the ember at its end).
- Card: *Every kill Withers the enemies within 90 of it by 10% of their max health.*
- Mechanic: on a kill (`OnChestEnemyKilled`), walk `EnemyRegistry.Snapshot` once by squared
  distance and call `ApplyWither(0.10f)`. Wither already does the rest: the strongest wins, it
  never kills, and it stops at 25% (8% on a boss). Molten Shard's 12% supersedes it.
- Why: it gives a build without Molten Shard or a Metal branch a Wither source, and it is the half
  of the Poppet that works on a crowd: the ash mark spreads through a pack as it dies. It is worth
  most on elites and tough bodies, as Wither always is.
- Art: a lock of black hair tied with the red thread, its end burned to a glowing ember (a Fire
  ramp stop at the tip only).

**The set** (`poppet`) - tags **Poison, Grass** (nightshade in the straw stuffing).
- Two pieces: *Your debuffs last 20% longer.* All three: *Your debuffs last 50% longer, and an
  enemy carrying two different ones is also Vulnerable +15%.*
- "Debuffs" means Slow (and a root), Poison, Vulnerable and Weaken. **Fear and stagger are
  excluded**: Fear is never extended, by its own rule, and a stagger is a hold. Wither has no
  duration to lengthen, but it does count as carried.
- Mechanic: one duration scale, set by `RefreshChestSetEffects` and read by `ApplySlow`,
  `ApplyPoison`, `ApplyVulnerable` and `ApplyWeaken`. Only the player's side ever debuffs an enemy,
  so it is safe to read there, and the boss clamps act after it. For the +15%: before the
  Vulnerable multiply in `DealDamageToEnemy`, a target carrying two or more of {slowed, poisoned,
  vulnerable, weakened, withered} gets `ApplyVulnerable(0.15f, 0.5f)`. Routing it through the
  status means it wears the bracket mark and shares the +60% cap.
- Why: Eric's debuffs become one build, from Shadow Bolt, Solar Flare, Molten Shard, the element
  paths, Ice and Poison. The Poppet is what makes two of them worth more than either.
- No new enemy art is needed: every Poppet effect lands as a status that already has a mark (#69's
  tell). There are two marks per enemy at most, in their existing priority order.

---

## 4. The Gimmel Ring: the pair, and the Conjunction

A gimmel ring is two hoops that lock together into one band, worn apart by a betrothed couple and
joined at the wedding, often with clasped hands over a stone. It is the right picture for two
spells that become one.

**The pair** is your two highest-level spells, **not counting your wizard's own**. That spell's
last form is its Apotheosis and it can never be fused (`combo-spells.md` 2). Ties go to the earlier
slot. **A Conjunction is always one of the pair** (it ranks as level 8). One helper,
`Player.GetPairSpellIds()`, is cached on `RaiseLoadoutChanged` and on level-up, so a per-hit check
reads two ids and never sorts anything.

**Betrothal Hoop** (`betrothal_hoop`) - Uncommon, no tag.
- Card: *Level-ups offer upgrades to your two highest-level spells (besides your wizard's own) twice
  as often.*
- Mechanic: `GetLevelUpOptions` doubles the weight of those two spells' upgrade candidates inside
  the upgrade group. **It changes which upgrade is dealt, never whether one is**
  (`UpgradeOfferChance` is untouched).
- Why: the Sagittarius measurement (`combo-spells.md` 6) found that the bottleneck was the cards:
  fourteen upgrades spread over two spells that are not the wizard's own. This puts a planned
  Conjunction in the player's pocket, and it serves a two-Ascension build that never fuses just as
  well. It leans away from the wizard's own spell, so it can only make the Apotheosis later.
- Art: a plain gold hoop with a half-heart cut into its face.

**Fede Hoop** (`fede_hoop`) - Uncommon, no tag.
- Card: *Your two highest-level spells (besides your wizard's own) deal +30% to an enemy the other
  hit in the last second.*
- Mechanic: `Enemy` keeps the id and time of the last hit from a pair spell (two fields, cleared in
  `ResetForRespawn`). `DealDamageToEnemy` compares them. Nothing is allocated.
- Why: it rewards a pair that overlaps, like a zone and a striker on the same body (Gravewell and
  Meteor Swarm, Mirefoot and Cone of Cold). The section 4 Conjunction recipes in `combo-spells.md`
  are pairs of exactly that kind. It is limited to the pair because "any two spells" would be +30%
  on most hits of a crowded loadout.
- Art: the second hoop, with the two clasped hands raised on it.

**Garnet Bezel** (`garnet_bezel`) - Rare, **Lightning** (the spark that jumps between the hoops).
- Card: *Each cast of your two highest-level spells (besides your wizard's own) takes a fifth of its
  cooldown off the other's.*
- Mechanic: in the one firing loop, when pair spell A casts, `spellFireTimers[B]` moves 0.2 of A's
  cooldown toward ready. **It is bounded by construction**: A takes 0.2 x its own cooldown every
  cooldown, so B gains 0.2 s a second whatever A's speed. It amounts to +20% recharge for each
  spell while both fire, but you can see each cast tick the other.
- Art: the garnet set where the hoops meet, with a Lightning glint in its facets.

**The set** (`gimmel_ring`) - tags **Water, Lightning** (two streams joining; the spark).
- Two pieces: *Your two highest-level spells (besides your wizard's own) deal +8% damage.* All three:
  *They deal +25% damage, and a Conjunction is always one of them.*
- Why: the pieces build the pair, and the set pays it off. After a fusion the Conjunction keeps the
  bonus, so the set does not lose its value at the moment it was built toward. In a run that never
  fuses, the set is a strong two-spell focus.
- Set icon: the ring closed, two hoops interlocked into one band, the hands clasped over the garnet.
  It sits on the gold row, the Regalia's metal.

---

## 5. The Gaming Box: chance you can argue with

A folding gaming box: a board inside, bone dice, and a leather cup. Section 8h of
`passives-and-items.md` said what was left untried when runs felt samey: *"a nudge toward pieces of
a set the player has started ... a reroll or banish on the chest screen."* This set is those ideas
as objects.

**Gaming Board** (`gaming_board`) - Common, no tag.
- Card: *Chests offer pieces of a set you have begun three times as often.*
- Mechanic: in `GetChestItemOptions`, an unowned piece of a set you hold at least one piece of (and
  have not completed) gets x3 offer weight. The fade for cards already shown (`ShownOfferFade`)
  still applies on top.
- Why: twelve new relics dilute every set by about a quarter (question 3). This is the player's
  answer to that.
- Art: the board folded shut, its inlaid points showing at the hinge, in bone and ebony.

**Bone Dice** (`bone_dice`) - Uncommon, no tag.
- Card: *+3 rerolls and +1 ban for the rest of the run.*
- Mechanic: when claimed, `RerollsRemaining += 3` and `BansRemaining += 1`. Both have been pools
  for the run since 2026-10-31.
- Why: it is a third of the shop's reroll cap, found in the run, for a player fishing for a recipe's
  parents or an Apotheosis path.
- Art: two pale dice with their pips burned in, one showing a single eye.

**Dice Cup** (`dice_cup`) - Rare, **Wind** (the turn of fortune).
- Card: *Once in every chest, shake the cup: its relics are dealt again.*
- Mechanic: a button on `ChestItemSelectionMenu`, focusable through `MenuNavigator` and placed
  inside `SafeArea`. The re-deal calls `GetChestItemOptions` with the run's `ChestOfferMemory`, so
  the first hand sits the second one out and is faded. The Penknife's picker is the precedent for a
  chest screen that changes in place. The cup can be used once per chest and is never spent.
- Why: it suits set chasers and John Dee. It is Rare because one re-deal in every chest is worth
  more than the Reliquary Key's extra card (Common), and **a fourth relic per chest is not on the
  table**: a third source of that would put six pages on a phone (8d).
- Art: a leather cup bound with a brass band, one die tumbling from its lip.

**The set** (`gaming_box`) - tags **Wind, Lightning** (fortune and weather turn alike).
- Two pieces: *One chest in three holds a piece of a set you have begun.* All three: *Every chest
  holds at least one piece of a set you have begun, while one is left to find.*
- Mechanic: after the weighted draw, if no card is a piece of a begun set and one exists, the
  lowest-keyed card is swapped for the best-keyed piece. Tome relics are in no set, so they are
  never affected.
- Set icon: the box open, the board inside, the dice and cup on it.

---

## 6. The Ampulla: the final forms

The coronation ampulla is a golden eagle that holds the anointing oil. Its head unscrews, and the
oil pours from its beak into a spoon. The Sovereign Rite **anoints** a spell (`sovereign-spell.md`),
so the vessel and the spoon are the objects for the run's last forms: the Apotheosis, the
Conjunction and the Rite.

**Anointing Spoon** (`anointing_spoon`) - Uncommon, no tag.
- Card: *Your wizard's own spell deals +20% damage. Once it has taken its Apotheosis, your other
  spells do instead.*
- Mechanic: `DealDamageToEnemy` compares `source.Id` with `Player.SignatureSpellId`. "Has taken its
  Apotheosis" means that spell's milestone 9 is active (`ApotheosisCatalog`).
- Why: the first two thirds of a run are about the wizard's own spell and the last third about
  everything else, and the spoon follows that. It adds damage, not levels or tags, so no gate moves.
- Art: a gilt spoon with its bowl split by a ridge into two channels, and pearls at the knop.

**Ampulla Beak** (`ampulla_beak`) - Rare, **Water** (what pours).
- Card: *Damage past a kill pours into the nearest enemy within 120.*
- Mechanic: in `DealDamageToEnemy`, when the target dies, the excess (the final damage minus the
  health it had) is dealt to the nearest enemy, found by one `EnemyRegistry.Snapshot` walk and only
  on a kill. There is no crit reroll and no chain, and **a spill never spills again**. A spill that
  kills is a kill, so it pays the Singed Lock and the Chalice.
- Why: a Fireball, an Obsidian Spike or a Void Lance wastes most of each hit on a swarm that dies to
  a fraction of it, and this turns heavy single hits into crowd damage. It is a rule, not a
  percentage. **It is not an execute**: a spill into a boss is ordinary damage.
- Art: the eagle's head and hooked beak, unscrewed, with a bead of oil at the tip (Water gleam).

**Eagle Ampulla** (`eagle_ampulla`) - Relic, **Wind, Fire** (the eagle; the oil a lamp burns).
- Card: *Your highest-level spell deals +15% damage. Once the Sovereign Rite anoints a spell, it
  carries the status of every element its silenced spells carried.*
- Mechanic: the +15% reads the highest-level held spell. After the Rite, each hit of the anointed
  spell applies `ElementPaths.RiderFor(e)` for every element any silenced spell named: Poison, the
  Ice and Water slow, Darkness's Vulnerable, Light's Weaken, Metal's Wither and Lightning's stagger.
  Fire, Arcane, Wind, Earth and Grass have none. Each status's own cap and every boss clamp apply.
  **This depends on the Rite**, which is being built.
- Why: the Rite pours the silenced spells' levels into one spell, and this pours in their elements
  too, so the anointed spell wears what the whole book was. Before the Rite it is a plain Relic,
  about as strong as the Brazier Tripod.
- Art: the golden eagle body, wings folded and headless; the Beak is its head. Seeing two of the
  pieces tells you the third is a spoon.

**The set** (`ampulla`) - tags **Light, Water** (holy oil, poured).
- Two pieces: *Your Ascended spells deal +6% damage.* All three: *Your Ascended spells deal +15%
  damage; an Apotheosis or a Conjunction deals +30% instead.*
- "Ascended" means the spell holds its level 8 milestone. The Sovereign spell is left to the Eagle
  Ampulla on purpose: the Rite's target is 1.3 times the loadout, and the set should not compound
  on top of it.
- **A late set by design.** On a new save there is no Ascension before the Hayward
  (`difficulty-curve.md` 6), so in the first chapters the Spoon and the Beak carry it.
- Set icon: the whole eagle with its head on, and the spoon laid before it.

---

## 7. Boons

Each new boon is an object that "still has the giving in it" (`passives-and-items.md` 3). Each card
gives a flavour sentence and the effect said plainly (`BoonCatalog.ExplainEffect`), with a stat line
for the level-up card (`DescribeEffect`). Each needs a `BoonEffect` value.

**Waywiser** (`waywiser`) - **Wind, Lightning**. Recommended source: **starter**.
- Flavour: *A wheel that counts the road, and pays out for every turn.*
- Effect: *While you walk, whichever of your spells is furthest from ready recharges 50% faster.*
  Stat line: "Walking: slowest spell recharges +50%".
- Mechanic: `BoonEffect.WalkingRecharge`. While `IsStandingStill` is false (intent, the Brazier's
  definition), the largest remaining `spellFireTimers` entry ticks 1.5x. That is a max over at most
  seven floats each frame.
- Why: **walking is the one condition nothing pays.** This is Taproot's mirror: Taproot pays a stand
  and Waywiser pays a kite, and holding both means whichever you are doing pays. It suits a build
  with one big slow spell (Meteor Swarm, Starfall, Fireball). It is a starter because Lightning has
  the thinnest floor in the game, three spells and one boon. The pause screen says which spell it
  is winding.
- Art: a wooden measuring wheel on a handle with a brass dial. The inlay is Wind and the dial
  carries a Lightning glint.

**Guttering Candle** (`guttering_candle`) - **Fire, Wind** (a flame in a draught). Recommended
source: a chapter deed.
- Flavour: *It burns brightest just before it goes out.*
- Effect: *Below 35% health, all your spells recharge 20% faster.* Stat line: "Below 35% HP: -20%
  Cooldowns".
- Mechanic: `BoonEffect.DesperateRecharge`, read wherever cooldowns are scaled, against current
  health over max health. The shield is not health.
- Why: since 0.10.0 a hurt player stays hurt, and this pays him for being there instead of asking
  him to heal. It recharges rather than adding damage so that it is not another Wrath Amulet. 20% is
  2.5 times Quicksilver Bead's always-on 8%, the conditional premium Taproot set.
- Art: a candle burned to a stub on an iron pricket, its flame guttering sideways, wax running down.

**Straw Skep** (`straw_skep`) - **Poison, Grass** (bees, and what they feed on). Recommended source:
sold in Powers.
- Flavour: *The closer they crowd, the louder it hums.*
- Effect: *Your spells deal 2% more damage for every enemy within 160 of you, up to 24%.* Stat line:
  "+2% Spell Damage per enemy near (max +24%)".
- Mechanic: `BoonEffect.CrowdDamage`. A count is taken four times a second over
  `EnemyRegistry.Snapshot` by squared distance, cached, and read by the spell-damage bonus. It is
  never counted per hit.
- Why: the crowd condition. It is for the player who plants (Taproot, the Brazier, Binding Thread,
  Iron Fang) and pays him for the danger of it. The cap is 2.4 times Lantern Oil's always-on 10%.
- Tell: up to twelve bee motes circling him, one per two enemies counted, so the bonus can be seen.
- Art: a coiled-straw dome with a dark entrance and two bees as Poison-green motes.

**Bone Rattle** (`bone_rattle`) - **Darkness, Lightning** (the fright; the crack of it). Recommended
source: a chapter deed (the Gaoler's, since a jailer carries a rattle).
- Flavour: *Shake it, and whatever is close remembers it is afraid.*
- Effect: *When you are struck, every enemy close around you is frightened (at most once a second).*
  Stat line: "Frightens nearby enemies when struck".
- Mechanic: `BoonEffect.Frighten`, the **third reactive boon**, in `TriggerBoonReactions` with the
  same 1 s cooldown and the same 130 radius, and its ring drawn at that radius.
  `Enemy.ApplyFear(Player.FearSeconds)` keeps Fear's three rules: elites and bosses are immune, a
  fright is never extended, and four seconds of immunity follow.
- Why: Saltbound Chain hurts, Rimebriar slows and this scatters, so a reactive build gets three
  answers on one pulse. It gives #69's Fear a carrier that is not a Darkness spell.
- Art: a bundle of small bones bound to a handle with sinew. The inlay is Darkness, with a Lightning
  glint where the bones knock together.

**Tally Stick** (`tally_stick`) - **Arcane, Lightning** (the XP element; the burst). Recommended
source: sold in Powers.
- Flavour: *Every notch is one more you did not leave behind.*
- Effect: *Collect 20 XP orbs in an unbroken run: your spells recharge 30% faster for 4 s (at most
  every 10 s).* Stat line: "20-orb streak: -30% Cooldowns, 4 s".
- Mechanic: `BoonEffect.StreakHaste`. "Unbroken" is **the run the pickup sound climbs on**: a gap
  longer than 0.6 s breaks it (`SfxPlayer.XpStreakResetMs`). The count moves into `Player`, and the
  sound reads it there, so the sound and the boon cannot disagree.
- Why: a pickup item that makes collecting a fight decision: sweep the orbs a pack left and hit
  harder for it. The 10 s lockout makes a Gleaning Bell haul one burst, not a minute of one.
- Art: a split hazel stick (a tally's two halves), its notches lit Arcane.

---

## 8. Element tags: before and after

Carriers per element, counted from the 34 `SpellData_*.tres` (Sagittarius left out, since a
Conjunction carries its parents' tags), `BoonCatalog`, `GetElementTags` and the sets'
`ElementTags`. The **floor** is spells plus boons, which is what `ValidateElementReachability`
counts; items and sets sit on top of it.

| element | spells | boons | items | sets | total | floor |
|---|---|---|---|---|---|---|
| Fire | 6 | 1 -> 2 | 1 -> 3 | 1 | 9 -> 12 | 7 -> 8 |
| Ice | 4 | 3 | 2 | 1 | 10 | 7 |
| Arcane | 5 | 2 -> 3 | 2 | 2 | 11 -> 12 | 7 -> 8 |
| Darkness | 4 | 2 -> 3 | 4 | 3 | 13 -> 14 | 6 -> 7 |
| Light | 5 | 4 | 1 | 3 -> 4 | 13 -> 14 | 9 |
| Grass | 4 | 4 -> 5 | 1 | 1 -> 2 | 10 -> 12 | 8 -> 9 |
| Earth | 5 | 3 | 1 | 2 | 11 | 8 |
| Wind | 4 | 1 -> 3 | 1 -> 3 | 1 -> 2 | **7 -> 12** | 5 -> 7 |
| Lightning | 3 | 1 -> 4 | 2 -> 3 | 1 -> 3 | **7 -> 13** | **4 -> 7** |
| Poison | 5 | 1 -> 2 | 1 | 1 -> 2 | 8 -> 10 | 6 -> 7 |
| Metal | 4 | 4 | 1 | 4 | 13 | 8 |
| Water | 3 | 4 | 1 -> 2 | 2 -> 4 | 10 -> 13 | 7 |

The spread narrows from 7-13 to 10-14. **Wind and Lightning**, the two thinnest elements, take most
of the new tags, and Lightning's floor (three spells and one boon) rises the most. No new tag goes to
Metal, and Light and Darkness each gain only one. Water gets more set tags but still has three
spells, which is a content gap, not a tagging one (Riptide was the answer to that last time).

---

## 9. Balance notes

**The bands.** Relic peers in today's pool:

- **Common**: one stat, +8-15% (Brazier Coals +12% spell damage, Barrow Breastplate +8% defence).
- **Uncommon**: a condition or a rule worth +15-25% when it is on (Scythe Snath +25% for 4 s after a
  hit, Barrow Greaves).
- **Rare**: a rule worth about +25-30% where it applies, plus a tag (Hoarfrost Rivets, Prism Jewel).
- **Relic**: build-defining.

The QA pass (`passives-and-items.md` 8g) handed each set over at 0:00 and found damage dealt from
x0.90 to x1.40 against holding nothing, with **no set mandatory**. Boons at face value are +8-10%
unconditional, or about 2.5 times that behind a condition (Taproot).

| item | where it sits | risk, and what would move it |
|---|---|---|
| Pricking Pin | Uncommon; +20% Vulnerable behind a crit | With the Whetstone, every opening hit marks its target. Bounded by the +60% cap; 20% leaves room under Shadow Bolt |
| Binding Thread | Uncommon; about 20% fewer touches from a toucher | **Healing scarcity**: chip damage is what 0.10.0 made stay taken. Measure `RunHpLost` with Binding Thread, Bone Rattle, Rimebriar and Darkness 6 held together (question 4) |
| Singed Lock | Rare; about +10% damage taken by crowds, once each | Worth most on elites. Never kills; 8% cap on a boss |
| The Poppet | upper middle; the +15% Vulnerable is on most bodies once Ice and Poison are both in play | Sits near the Scythe's +25%. It is bounded by the Vulnerable cap and is visible on every enemy |
| Betrothal Hoop | Uncommon; no damage | More Ascensions in a run. Re-run `--seek-combo=sagittarius` with and without it (the payoff) and `--seek-ascensions` (the count). It cannot make the Apotheosis earlier |
| Fede Hoop | Uncommon; +30% on overlap | Limited to the pair, or it is +30% everywhere |
| Garnet Bezel | Rare; +20% recharge for each pair spell, by construction | none beyond its band |
| The Gimmel Ring | middle; +25% on two spells | Weaker than the Scythe's +25% on everything. Its value is the Conjunction it carries through |
| Gaming Board, the Gaming Box | no damage; more completed sets | **Sets are the strongest thing in the pool** (x1.1-1.4). Simulate completed sets per run before and after (section 10). The Box can only steer toward a set already begun, never pick for you |
| Bone Dice | no damage | +3 rerolls is a third of the shop's cap. Confirm the Apotheosis floor (7.5 min, planned bot, every charge at cap) with `--seek-apotheosis` |
| Dice Cup | Rare | One re-deal per chest, faded by the shown memory. It cannot fish forever |
| Anointing Spoon | Uncommon; +20% on one spell, then on all the others | none beyond its band |
| Ampulla Beak | Rare; large for heavy single-hit spells | Damage only, **not an execute**. A spill never spills again |
| Eagle Ampulla | Relic; Brazier Tripod-sized before the Rite | After the Rite, up to six statuses on one spell, each capped and clamped. Measure with the Rite's `--seek-sovereign` run |
| The Ampulla | middle-late; +15% on Ascended spells | Excludes the Sovereign spell so it never compounds on the Rite's 1.3 target |
| Waywiser | boon; one big spell much faster for a kiter | With the Rite, it always winds the one anointed spell. Measure a Meteor Swarm build |
| Guttering Candle | boon, conditional; 2.5x Quicksilver Bead | Pays for being low, never for healing |
| Straw Skep | boon, conditional; +24% cap | Pulls a player into the crowd. That is the point, and the Brazier already makes that trade |
| Bone Rattle | boon, reactive | The same as Rimebriar's control with the fear rules on top. Each enemy is frightened at most 1.5 s in every 5.5 s |
| Tally Stick | boon; a burst | 10 s lockout. It cannot stack on a Gleaning Bell haul |

**Healing.** No item heals. Three change how much damage is taken or what it costs: Binding Thread,
Bone Rattle, and Guttering Candle (which rewards it). The healing ledger (`difficulty-curve.md` 5)
needs no new row. `RunHpLost` with the defensive stack is the measurement that matters.

**Bosses.** There is no execute anywhere, and the Beak's spill and the Singed Lock's Wither (which
never kills) are not executes. Every slow, hold and stagger the Poppet lengthens is still clamped
downstream. Fear stays off elites and bosses, Weaken stays at 25% on a boss, Wither at 8% and
Vulnerable at +60%. The Eagle Ampulla's riders pass through the same `Apply*` calls and so through
the same clamps.

**Pool size.** Relics go from 34 to 46: 10 Common, 15 Uncommon, 15 Rare, 6 Relic. The new ones lean
Uncommon and Rare, so cards drift toward rarer ones (a little more power at the margin, as 8h found),
and each existing piece is offered about a quarter less often. Question 3 covers that.

**Performance** (`.ai/performance.md`). The Singed Lock and the Beak walk the enemy snapshot only on
a kill. Straw Skep counts four times a second. The Fede Hoop compares two fields per hit. The
Poppet's check reads five bools per hit. Nothing allocates per frame.

---

## 10. Build cost, and the first wave

S is an afternoon. M needs new UI, a new helper or a probe. Every relic also carries the shared
chores below, and every boon carries its own.

**Recommended first wave: eight items.**

| item | cost | why first |
|---|---|---|
| The Poppet: Pricking Pin, Binding Thread, Singed Lock + set | S, S, S + M | It closes #69, and each piece reads a status that already exists and already has a tell |
| The Gimmel Ring: Betrothal Hoop, Fede Hoop, Garnet Bezel + set | M (the pair helper), S, S + S | Conjunctions are Eric's current work, and the Sagittarius run measured the cards as the bottleneck |
| Waywiser | S | the walking condition; Lightning's floor |
| Bone Rattle | S | #69's Fear; the third reactive boon |

**Wave 2**: the Gaming Box (the Dice Cup is the M: chest-screen UI, pad and phone), Guttering
Candle, Straw Skep (the count and the bee tell) and Tally Stick (moving the streak out of
`SfxPlayer`). **Wave 3**: the Ampulla, once the Sovereign Rite has landed, because the Eagle
Ampulla reads it.

**Chores for every relic**: `ChestItemCatalog` (const, `AllItemIds`, rarity, tags, display name,
description, icon path, the set's `ChestSetDefinition`), `Player.ApplyChestItemEffect` and
`RefreshChestSetEffects` (the `ChestSetEffect` labels mirror them by hand: no check reads both),
**`ChestItemIntegrationTest`'s hard-coded counts**, and the icons in `tools/art/relic_icons.py`. The
pieces of one object share a material row, a metal and an edge treatment, `_rim()` lights them,
and the set icon is the object whole. Then run `--import` and commit the `.import` sidecars. The
validators `ValidateFullSetObjects`, `ValidateChestRarity` and `ValidateChestSetPresentation` pass
by construction once the data is filled in.

**Chores for every boon**: `BoonEffect`, the `BoonCatalog` entry, `ExplainEffect` and
`DescribeEffect` (with a stat glyph, `StatIcons.KeyFor`), `Player.ApplyBoon`, the source in
`UnlockCatalog` (`ValidateBoonSources`), an `ELEMENT` entry in `tools/art/boon_icons.py` (and
`INLAYS` if the object is all material), and, for the conditional ones, the pause screen's state
line as Taproot has ("Winding: Meteor Swarm", "Humming: 9 near, +18%", "Streak 12 of 20").

**Measure, per wave:**

1. **The chest simulation from 8h**, re-run with 46 relics. It was a Python re-implementation of
   `GetChestItemOptions`, 10,000 runs of 15 chests. Measure relics shown, the most-shown relic, and
   **completed sets per run**, with and without the Gaming Board, the Box, and question 3's nudge.
2. **The 8g sweep** for each new set: the Pyromancer, seeds 11 and 23, `--give-relics=<pieces>`,
   `--immortal` for 8 minutes and `--survivor` for 12, against holding nothing. Every set should
   fall inside x0.9-1.4 dealt, and none should be mandatory.
3. **`RunHpLost` with the defensive stack** (question 4), against the 10.2 table in
   `difficulty-curve.md`.
4. `--seek-combo=sagittarius` with the Betrothal Hoop handed over; `--seek-apotheosis` with the Bone
   Dice (the floor must hold).
5. Photographs: `_UiShot --give-relics` for each new set's assembly moment on 1280x720 and on
   540x1212 `--portrait --safe-area`; the chest screen with the Dice Cup's button on a phone; the
   longest new card (the Eagle Ampulla) on a five-relic chest with John Dee.

`dotnet build` validates none of this.

---

## 11. Considered, and not proposed

- **Items as evolution keys** (`spell-variety.md` 6, idea 1). Conjunctions already gate on named
  Ascensions, and Eric settled that rule. An item key would add a second gate to a system that was
  deliberately given one. The Betrothal Hoop is the soft version: it plans toward a recipe without
  gating it.
- **A second curse.** The Tithe of Wormwood has not been playtested, and two curses would multiply
  the spawn clock between them. Revisit after 0.10.0 is played.
- **Bitter Draught** (damage over time ticks twice as fast for half as long). Poison is
  stacking-resistant and gets reapplied, so it rarely runs out. Twice the ticks would then be twice
  the damage: a hidden +100%.
- **Gravebloom** (deaths in a zone leave a zone). That is Cataclysm's verb, and Apotheosis verbs are
  unique.
- **Widow's Lens** (aim at the weakest). It needs `spell-variety.md` phase 2's targeting modes to be
  load-bearing, and a global override would fight the spells whose aim is their identity
  (`spell-roster.md`).
- **Another stand-still item.** The Brazier and Taproot already cover it, and Waywiser is the gap.
- **One more relic per chest.** The Reliquary Key and John Dee already give one, and a third source
  puts six pages on a phone (8d).
- **Anything that raises the Gleaning Bell, a flask, regeneration or a heal**, or that lets Skip pay
  a full bar (a skip would then loop straight into the next level-up).
