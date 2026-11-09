# Character fit: every wizard's starting spell and passive

Started 2026-11-09 with Eric, in a design conversation about new spell ideas and about making sure
**every playable character opens on a spell that suits who they are**. This note holds the review,
Eric's decisions in his own words, and what is still open. Each wizard's story is in
`.ai/chapter-wizards.md` (the ten chapter wizards), `.ai/side-events.md` (Tituba, Väinämöinen),
`.ai/second-half.md` section 3 (the gods), `.ai/protagonist.md` (the Archmage), `.ai/merlin.md` and
`.ai/dark-wizard-playable.md`; the forms and verbs are `scripts/ApotheosisCatalog.cs`.

A fit is judged on four things: the character's **story**, their **look**, their **element**, and the
**verb** of their Apotheosis.

---

## 1. Decisions

| date | decision | Eric's words | status |
|---|---|---|---|
| 2026-11-09 | **Imhotep opens on Scorching Ray, not Fireball.** A ray that heats up on one target is the sun through his bronze mirror - the same act that frees him (`.ai/unlock-puzzles.md` 3.1). His Apotheosis, the Eye of Ra (verb *blind*), moves onto the ray: a ray that lands at full heat flares like the sun, and whatever sees it loses him for two seconds. Fireball stays in the game as the Scorched Sands boss's deed spell (`sands_unwound`); only his opener changes. Fire stays his element (Scorching Ray is Fire + Arcane, and none of its paths trades Fire away: Flashpoint deepens Fire, Prism adds Arcane). He can no longer fuse Dark Lens, whose parent the ray is - as Tituba cannot fuse Maelstrom | "I like your imhotep suggestion, implement that as well" | building |

---

## 2. The starting spells

| who | spell | fit | why |
|---|---|---|---|
| The Archmage | Magic Missile | strong | the one he missed fires the dart that never misses (*count*) |
| Aedan | Cinderbreath | strong | a pyromancer breathing fire, up close (*burn*) |
| Isbrand | Cone of Cold | good | right spell, but it still aims at the nearest enemy, not the way he faces (`spell-variety.md` phase 1) |
| Levina | Chain Lightning | strong | the storm leaping foe to foe (*link*) |
| Gabbro | Obsidian Spike | good | volcanic stone for a man named after one (*hold*); plain to play |
| Baba Yaga | Thorned Vine | strong | the Forest's own; the hedge eats (*feed*) |
| Medea | Solar Flare | strong | her grandfather the Sun; the sown men (*turn*) |
| the Cumaean Sibyl | Arcane Explosion | strong | a hundred voices breaking out (*echo*) |
| Paracelsus | Contagion | strong | the dose makes the poison (*distil*) |
| Abe no Seimei | Kindled Ward | strong | his flying paper wards (*banish*) |
| Imhotep | ~~Fireball~~ Scorching Ray | was weak | Fireball was generic, Aedan's element, and said nothing of the builder or his mirrors. Decided above |
| Wayland | Iron Palisade | good | stakes he forged (*temper*); a smith's wall rather than his blow, and Hephaestus's Anvil Line needs it |
| Ma Liang | Familiar | strong | painted creatures come alive (*displace*) |
| Morgan le Fay | Sleet Runes | ok | her form (a mirage of you) is all hers; sleet runes are not. No existing spell is closer, and an illusion spell would take her form's *double* |
| Nostradamus | Starfall | strong | the astrologer's stars (*foresee*) |
| Tituba | Riptide | good | the sea she was taken over, chosen on purpose over a curse spell |
| Väinämöinen | Bramble Seed | ok | he sowed the first forests, but he is the singer first, and the game frees him by song |
| John Dee | Hollow Star | good | the collector pulls; his black obsidian mirror (*pull*) |
| Laelaps | Cyclone Slash | good | the hound that never stops running (*reflect*) |
| Merlin | Orb of Ages | strong | made for him; he lives backwards (*rewind*) |
| Nimue | Spiritual Weapon (the Sword) | strong | Excalibur (*sever*) |
| the Dark Wizard | The Taking | strong | his own hand (*take*) |
| Artemis | Sagittarius | strong | the bow and the stars (*hunt*) |

## 3. The passives

| who | passive | fit | why |
|---|---|---|---|
| The Archmage | Overlooked: a killing blow misses, once | strong | he was missed once already |
| Aedan | Kindled Focus: +10% spell damage (+15% close by since 2026-11-09) | good | a plain stat for the glass cannon |
| Isbrand | Rimeblood: +9 HP a minute | ok | sustain for the tank; nothing frosty in it |
| Levina | Quickened Cadence: -10% cooldowns | strong | lightning-quick, with her speed |
| Gabbro | Stoneskin: -10% damage taken | strong | stone skin on the heaviest body |
| Baba Yaga | Bone Fence: +20% knockback | good | the fence keeps visitors out |
| Medea | Rejuvenation: +20% healing | **weak** | right story (she made old Aeson young), but healing is a trickle since the difficulty pass (`difficulty-curve.md` 5), so it is worth 2-5 HP a minute |
| the Cumaean Sibyl | Prophecy: +3 rerolls | strong | other futures to choose from |
| Paracelsus | Sola Dosis: +25% damage over time | strong | the dose |
| Abe no Seimei | Exorcist: +15% damage to elites | strong | the exorcist |
| Imhotep | Surveyor: +20% pickup reach | good | the surveyor |
| Wayland | Wings: +10% speed | strong | the wings he made, on a lamed leg |
| Ma Liang | Paint What Is Needed: a flask once, below 25% | strong | he paints what is needed |
| Morgan le Fay | Avalon: the first revive heals to full | **weak in play** | perfect story, but it does nothing unless she holds a revive (Phoenix Oath, or the cat's Nine Lives) |
| Nostradamus | Foresight: wind-ups show 15% longer | strong | foresight |
| Tituba | Patience: +15% duration | good | patience |
| Väinämöinen | Song of Making: +10% area | ok | a flat stat; the song is not in it |
| John Dee | Scattered Library: +20% elite chest chance | strong | the scattered library |
| Laelaps | Fated to Catch: 1 in 4 bolts caught | strong | fated to catch |
| Merlin | Warding Hand: whatever touches him is thrown back | strong | the protector |
| Nimue | The Scabbard: contact lands 20% less often | strong | Malory's scabbard |
| the Dark Wizard | Dread: Darkness frightens twice as often | strong | the horde knows him |
| Artemis | Far-Shooting: spells reach 15% farther | strong | far-shooting |

Two patterns. **Twelve of the 23 are flat stat lines**, which suits the four starters (disciplines, not
people with a legend); among the rescued, the newer story-built passives make the older flat ones
(Tituba, Väinämöinen, Baba Yaga, Imhotep) read as filler, though none is wrong. **Three are about nearly
dying** (Overlooked, Avalon, Paint What Is Needed): a new passive should stay off that axis.

---

## 4. Open proposals (not decided)

**Passives**

- **Medea** - keep the name *Rejuvenation*, change the effect: if no blow lands for 4 s, half of the last
  wound closes. It rewards dodging and can never give back more than was taken. The half is a first
  guess for the harness.
- **Morgan** - make it work without a revive: either Avalon is one revive of her own (blurs into the
  Archmage's Overlooked), or a new *Glamour*: 1 in 10 blows strikes a mirage of her instead (no wizard
  owns dodge, and it is her form's mirage). Recommended: Glamour.
- **Väinämöinen** - put the song in *Song of Making*: when he levels up, ordinary enemies near him stop
  for 1.5 s to listen (the Kalevala's kantele, which every creature came to hear; his rescue is already
  the kantele puzzle). Elites and bosses unaffected.

**Spells**

- **Väinämöinen to Mirefoot** (Water + Grass, keeps his Grass): he sang his rival Joukahainen into a bog
  to the chin. Needs a new form (Wildwood is Bramble Seed's). Keeping Bramble Seed is also fine.
- **Isbrand's Cone of Cold aims the way he faces**, as `spell-variety.md` phase 1 said in October.

**Renames and re-themes** (Eric, 2026-11-09: *"Are there any we could rename, like arcane explosion, or
even re-theme, to fit their character. Maybe Sybil's arcane explosion actually adds to the music of the
game, and we change the mutations, etc."*) - to be gone through wizard by wizard (section 5).

**New spells**, each on a row the roster still lacks and a thin element (Lightning, Water, Metal and
Poison carry four spells each, Arcane seven), none on a verb or shape a Conjunction has claimed:

| idea | element | what is new |
|---|---|---|
| Corposant | Lightning | aims at nothing: a ball of storm-fire that caroms off the screen's edges ("Ball Lightning" is Stormsweep's level 4) |
| Ink Ring | Water + Darkness | walk a loop round a crowd; closing it strikes everything inside |
| Windfall | Grass + Metal | fires on the orbs you pick up, not a timer ("Gleaning" is the Gleaning Bell's) |
| Lancet | Metal + Poison | always goes for the most wounded enemy in reach; a poison build's finisher |

---

## 5. Wizard by wizard

Eric, 2026-11-09: *"Let's now go through every wizard one by one, listing their starting spell and
passive, the reasoning behind the choices, any suggestions that you would make, and give me the
opportunity for feedback."* His feedback on each is recorded here as it comes.

| # | wizard | verdict | Eric's words | status |
|---|---|---|---|---|
| 1 | The Archmage | keeps Magic Missile and Overlooked; **Overlooked's 10 s grace also makes him semi-transparent** (on top of the white he already turns), so it reads that he cannot be touched | "his passive should also make him semi-transparent for those 10 seconds, to make it clear that he is untouchable. with that small change, go ahead and move onto the next wizard" | built `8a7e382d` (opacity 0.45 on the body sprite, on the white's clock); not yet seen rendered |
| 2 | Aedan, the Pyromancer | keeps Cinderbreath; **Kindled Focus becomes +15% damage to enemies close to you** (was +10% spell damage everywhere), so the passive asks what his spell asks: fight up close. Close is a little past Cinderbreath's reach | "implement your suggestion, then next" | built `c7ecd205` at **250 px**, not 200: Cinderbreath reaches 230 px at level 1 since 2026-11-04 (300 by level 8), so 250 keeps "a little past its reach". New id `close_damage_15` |
| 3 | Isbrand, the Frostweaver | **Cone of Cold aims the way the player faces**, not at the nearest enemy, for every wizard who holds it (`spell-variety.md` phase 1, the facing family); keeps Rimeblood, his steady trickle of health, the only starter sustain now that healing is scarce | "implement your suggestion." and, of the whole pass: "you do not need to run the balance bots for these changes" | built `b99ffbc8`: every cone takes the facing (Arctic Blast's half-moon, Zero Point's relayed cone); tagline "A freezing cone where you face." Walking away fires it away from the chase: needs a playtest |
| 4 | Levina, the Stormcaller | keeps Chain Lightning and Quickened Cadence, unchanged | "next" | kept |
| 5 | Gabbro, the Geomancer | keeps Obsidian Spike and Stoneskin, unchanged | "next" | kept |
| 6 | Tituba | keeps Riptide and Patience. **Her cell line reads "I have been patient for so long. Please, this way."** (was "patient so long"). **Riptide's level 4 offers a choice of which way the tide runs**: Breaker pushes what it strikes out along its run, and a new card, Undertow, drags it back in toward her; Spring Tide's current follows the way the player chose | "Tituba's Line change: I have been patient for so long" / "her spell upgrade options should include ways that the tide can drag enemies in OR push them out." / "keep everything else about tituba" | line changed; Undertow building |

Eric's feedback on Imhotep's build, given at Tituba's turn (2026-11-09):

- **Scorching Ray's rays are staggered**: as the count grows, the rays of one cast leave one after another,
  with a slight random spread in the gaps, rather than all at once - "giving a more chaotic feel". For
  every wizard who holds it. Building.
- **Scorching Ray keeps three rays at level 1**: "scorching ray can start with 3 rays, we balance it on
  damage." The count is not cut to meet "spells start small"; if the ray needs balancing it is done on its
  damage.
- **The Eye of Ra's flare gets a better look**: "lets upgrade imhotep's flare to look more interesting".
  The first build's thin pale ring with spokes was faint, and a blinded body showed nothing. Building.
