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
| Baba Yaga | Bone Fence: +20% knockback | good (since her vine shoves, 2026-11-09) | the fence keeps visitors out; it did nothing until her vine pushed |
| Medea | Rejuvenation: +20% healing (the Salve since 2026-11-09) | **weak** | right story (she made old Aeson young), but healing is a trickle since the difficulty pass (`difficulty-curve.md` 5), so it is worth 2-5 HP a minute |
| the Cumaean Sibyl | Prophecy: +3 rerolls | strong | other futures to choose from |
| Paracelsus | Sola Dosis: +25% damage over time | strong | the dose |
| Abe no Seimei | Exorcist: +15% damage to elites | strong | the exorcist |
| Imhotep | Surveyor: +20% pickup reach | good | the surveyor |
| Wayland | Wings: +10% speed (since 2026-11-09: floor traps can't touch him, water doesn't slow him) | strong | the wings he made, on a lamed leg |
| Ma Liang | Paint What Is Needed: a flask once, below 25% | strong | he paints what is needed |
| Morgan le Fay | Avalon: the first revive heals to full (Glamour since 2026-11-09) | **weak in play** | perfect story, but it does nothing unless she holds a revive (Phoenix Oath, or the cat's Nine Lives) |
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
  **Declined 2026-11-09** (Eric: "keep the area passive.").

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

Eric went through the four one at a time on 2026-11-09, and held the 0.22.0 release until all four are
built. His verdicts, as they come:

| idea | verdict | Eric's words | status |
|---|---|---|---|
| Corposant | kept as proposed: one ball at level 1, bouncing off the edges of the visible screen, piercing, each enemy hit at most every half-second; Twin Fires, Static Wake and a Water card at level 4, Storm Lantern and Saint's Crown at level 8; earned by a new Lightning feat; nobody's opener | "yes" | built `968d67ca`: earned by **Storm Petrel** (defeat 500 enemies with Lightning in one run, `LifetimeStats.BestLightningKillsInRun`); the Water card is **Spindrift** (sea-blue fire, hits slow); 1 ball at level 1, 3 per cast by level 6, 3 s life rising to 4 s. Untuned first numbers; not yet played, heard or seen on the phone. The builder made a maze wall put a ball out, which Eric did not ask for - put to him |
| Ink Ring | kept as proposed: a thin line of ink trails the walker; crossing it closes the loop and strikes everything inside, then slows it a moment; a short recharge between rings; the ink fades fast at level 1 (small rings) and lasts longer as it levels; Long Stroke, Wet Ink and Ink Pool at level 4, Ink Prison and Second Stroke at level 8; nobody's opener. **Ink spells are drawn in a style unlike the rest of the game** - wet watercolor ink, the one sanctioned departure from the Bonelight contract (`.ai/art-direction.md`, "Ink"). The name stays Ink Ring (Ensō was offered, to keep it off Iron Palisade's Iron Ring) | "i like it. make sure the ink spells are a very different style of art than other parts of the game. the art-drawn crow familiar should also look significantly different, almost watercolor style ink blots" | building; the crow repainted `370dbef3` (`tools/art/ink_wash.py`, `.ai/art-direction.md` 3a), which Ink Ring paints through |

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
| 6 | Tituba | keeps Riptide and Patience. **Her cell line reads "I have been patient for so long. Please, this way."** (was "patient so long"). **Riptide's level 4 offers a choice of which way the tide runs**: Breaker pushes what it strikes out along its run, and a new card, Undertow, drags it back in toward her; Spring Tide's current follows the way the player chose | "Tituba's Line change: I have been patient for so long" / "her spell upgrade options should include ways that the tide can drag enemies in OR push them out." / "keep everything else about tituba" | built: the line `7f86c7da`; Undertow `f34887ea` (`riptide_reactive_catalyst`, Breaker's strength pointed home, letting go a body's width short of her; Spring Tide's current runs home after Undertow). Elites are moved by Undertow exactly as Breaker already throws them; bosses only lean. Not yet seen in play |

Eric's feedback on Imhotep's build, given at Tituba's turn (2026-11-09):

- **Scorching Ray's rays are staggered**: as the count grows, the rays of one cast leave one after another,
  with a slight random spread in the gaps, rather than all at once - "giving a more chaotic feel". For
  every wizard who holds it. Built `d4b8ef4f`: the first ray at once, then about 0.095 s apart, +-30%, the
  whole spread at most half the cooldown; a ray whose target died re-picks.
- **Scorching Ray keeps three rays at level 1**: "scorching ray can start with 3 rays, we balance it on
  damage." The count is not cut to meet "spells start small"; if the ray needs balancing it is done on its
  damage.
- **The Eye of Ra's flare gets a better look**: "lets upgrade imhotep's flare to look more interesting".
  The first build's thin pale ring with spokes was faint, and a blinded body showed nothing. Built
  `71fdeae8` (`SunFlare.cs`, `sun_flare.gdshader`): a white-hot disc, a gold light front landing on the
  blind radius, 8 to 12 turning god-rays, the wedjat flashing gold at the centre, a LightPool flash in the dark
  chapters; every blinded body wears a flickering four-armed gold glint, a seared elite a burst of sun-fire.

| # | wizard | verdict | Eric's words | status |
|---|---|---|---|---|
| 7 | Väinämöinen | keeps Bramble Seed and Song of Making (+10% area), unchanged. The kantele passive (section 4) was put to him again and declined | "next" / "keep the area passive." | kept |
| 8 | John Dee | **opens on a new spell, Folio**, and keeps Scattered Library. **Hollow Star becomes nobody's opener**. His Apotheosis, The Spirit Mirror (*pull*), moves onto Folio | "I don't think hollow star feels like a good spell for him - what spell could feel like his? can we trade hollow star to someone else, like someone who did more with science/stars, and have something that feels more like his?" / "I like your suggestions - how would john dee produce the first orb if he only fires when he picks up an orb? also, can we still keep his spirit mirror finale to work with this?" / "yes" | built `84e24368` (section 6); not yet played |
| 9 | Laelaps | keeps Cyclone Slash and Fated to Catch, unchanged. (Re-drawing the blades as a hound and fox of wind, the endless chase, stays an unasked idea) | "next" | kept |
| 10 | Abe no Seimei | keeps Kindled Ward and Exorcist, unchanged | "next" | kept |
| 11 | The Dark Wizard | keeps The Taking and Dread, unchanged. **His unlock is a mystery achievement**: The Taker Taken shows its condition (every chapter's boss on Hardcore) from the first run, and reads "???" for its name and its reward, with the seal, until done (`SealedName`/`SealedReward`, as a Conjunction's Feat reads before it is made). It was already listed under the Throne as "The Taker Taken - Frees someone": the title gave away who is taken | "do not change the dark wizard, but add his unlock condition as a mystery achievement" | built `ff5fa9a6`, validated (0 errors); the page not looked at |
| 12 | Merlin | keeps Orb of Ages and Warding Hand, unchanged | "Next" | kept |
| 13 | Baba Yaga | keeps Thorned Vine. **She moves flying in a mortar, holding a broom** (she joins Nimue among the floaters, `FLOATERS` in `tools/art/roster.py`, `anim_sets.hover`; the look only, her feet collider unchanged). **Bone Fence is in question**: her own vine pushes nothing (`Player.cs`: "Her own vine pushes nothing; it is a build's passive"), so +20% knockback does nothing until she takes a spell or card that shoves . **Fixed by option A: Thorned Vine throws what it cuts aside**, along its stroke (`ThornVine.VineShoveSpeed`, 180, a first guess), for every wizard; Bone Fence keeps +20% knockback and now works from her first cast. Entangle still roots instead | "Her typical movement can be flying in a mortar holding a broom, also does thorned vine have knockback? how often is her passive useful?" / "A: vine shoves" | vine shove `f78d8744`, mortar and broom `084cc285` (twigs in the gold row's light end - red-brown birch read as a red smudge), both validated (0 errors); not played |
| 14 | Morgan le Fay | keeps Sleet Runes (Ice + Wind is the weather her mirage forms in: cold air over cold sea). **Avalon is replaced by Glamour: 1 in 10 blows strike a mirage of her instead** - a dodge added to the roll in `TakeDamage` step 2 (`Player.Mirage.cs`, `glamour_evasion_10`), so a turned blow shows the Blur's ghosts. Avalon needed a revive she rarely held | "i like your avalon passive suggestion" / (asked: Glamour, or Avalon as her own revive?) "glamour" | built `2ac39d00`, validated (0 errors); not played |
| 15 | Paracelsus | keeps Contagion and Sola Dosis, unchanged | "next" | kept |
| 16 | Nostradamus | keeps Starfall and Foresight, unchanged | "Next" | kept |
| 17 | Imhotep | keeps Scorching Ray (decided above) and Surveyor. **His rays leave from the sun on his raised palm**, not his body's centre - for every wizard, from their own light (`Player.StaffTipLocal`), as Magic Missile and Cinderbreath already do | "Implement your visual suggestion, then next" | built `6abcfc4b`, validated (0 errors); not played |
| 18 | Medea | keeps Solar Flare. **Rejuvenation is replaced by the Salve: enemy bolts deal her 30% less** - the charm she gave Jason against the bulls' fire, and a help to standing still, which her flare asks for and shooters punish. Bolts only (`EnemyProjectile` through `Player.TakeBoltDamage`, after the signal, before armour, floored at 1): burning ground is rare, so it was left out | "i like the salve idea, but how often is burning ground relevant? maybe just that ranged attacks deal 30% less, because it helps with the \"standing still\" theme she has?" | built, validated (0 errors); not played |
| 19 | The Cumaean Sibyl | keeps Prophecy. **Arcane Explosion becomes Canticle** (display name; the id and every save key stay `arcane_explosion`): every spell she casts adds a note to a chord, voiced by that spell's element; when the chord is full the burst waits for the next beat of the chapter's music and sounds the chord in the chapter's key (bursts at once where no set is playing); The Hundred Mouths' echoes land on the beats that follow. Cards: Sympathetic Blast becomes **Harmony**, and **takes the element of the spell in the slot right after Canticle's** (the HUD strip marks that slot; an empty slot leaves it Arcane) instead of the last spell cast; Static Nova **Storm Chord**; Lingering Rift **Sustain** (the note rings on the ground); Resonance kept (the Sovereign Rite names it); Event Horizon **Fermata** (a held note: time stops). Numbers unchanged until balance testing | "Are there any we could rename, like arcane explosion, or even re-theme, to fit their character. Maybe Sybil's arcane explosion actually adds to the music of the game, and we change the mutations, etc." / "\"takes the element of the last spell cast\" should pick a certain spell slot. we are trying to add more spell slot play, and i want the player to be able to purposefully select the element" / "everything else is good, pending balance testing" | built `28909de5`..`57365a8f` (see `spell-roster.md`, Canticle), validated (0 errors); balance testing pending; not yet heard |
| 20 | Wayland the Smith | keeps Iron Palisade. **He flies when he moves**: standing, his iron wings stay folded and his weight is on his good leg; moving, they open and beat and he skims just above the ground (and he keeps the flying pose while he stands over water or a floor hazard). **Wings changes from +10% speed to: floor traps never touch him and water never slows him** - the spike plates and fire vents (`StageHazard`), hazard ground (lava, `Node2DGame`'s hazard tick), the lantern-tender's burning oil (`BurningOil`) and the Swallow-Hole's mud (its slow and its sting); wading water at full speed. Not the Last Bridge's collapse front, which is the chapter's chase, not a trap. Only the look changes for collision: his feet still meet walls | "is his movement animation a flying animation?" / "I like A, but instead of extra speed, make him immune to floor traps / water slowing." | built `2dc359ce` (`over_traps_and_water`, one flag `Player.Winged` asked by StageHazard, the hazard-ground tick, BurningOil, the Swallow-Hole and the wade slow; an 8-frame wing beat, standing frames unchanged). Spike plate and water checked in Godot; lava, oil and the Swallow-Hole not yet played. He lost his +10% speed |
| 21 | Ma Liang | keeps Paint What Is Needed. **He always opens on the crow Familiar** - no choice of dog or cat at the start (`ChoosesStartingForm` off; fixed to the crow as Nimue is fixed to the Sword), so his final form, The Painted Door, is always his to reach. **His crow is drawn as an ink brush-painting come alive** - when HE casts it; every other wizard's crow stays a creature of light | "lets just not let him choose a different familiar than the crow then - he automatically starts with a crow familiar. we also implement your art suggestion, redoing the crow familiar to be brush paintings" | built `9b3a2e0d` (the ink crow, and every form it grows into: Stormcrow, Raven, Morrigan, the Griffin - so it never turns back to light at level 4) and `8aa06d3c` (always the crow; his card reads "Familiar - Crow"). No wizard chooses a starting form now, so the opening-form page is unused code, kept. The Painted Door stays in his light. Not yet judged in play: whether the crow reads as a brush painting or a grey pigeon. **Repainted as watercolor ink blots** (Eric, 2026-11-09, at the new-spell review: "the art-drawn crow familiar should also look significantly different, almost watercolor style ink blots"): building |
| 22 | Nimue | keeps the Spirit Sword and the Scabbard. **Her card names the spell and its form: "Spiritual Weapon - Sword"** (`CharacterSelection.StartingSpellName`: the spell's name and the form's `ShortName` for any wizard fixed to one form) - it said "Spirit Sword" alone. The spell keeps its name | "Spiritual weapon is a D&D reference - We can just note on her card that her starting weapon is specifically spiritual weapon - sword" | built (commit above); the card not yet photographed |
| 23 | Artemis | keeps Sagittarius and Far-Shooting, unchanged | "next" | kept |

The review of all 23 ended 2026-11-09.

---

## 6. Folio - John Dee's spell (decided 2026-11-09)

**Why.** Hollow Star's only tie to Dee was the word *pull*. What Dee is, in his life and in this game, is a
**collector**: the largest library in England, at Mortlake, looted while he was abroad; freed here by
opening 100 chests; his passive the scattered library that elites carry. His spell should be paid for by
what he gathers.

**The spell.** *Folio* (working name kept), Arcane + Darkness - the scholar's magic and his black mirror.

| axis | Folio |
|---|---|
| aim | nearest enemy (a page flies to it) |
| cadence | **what he gathers**: the book turns a page on its own every 2 s (first guess), and **every orb he picks up turns one more at once**. No other spell fires on pickups (`spell-variety.md` section 2, "What decides when it fires") |
| demand | go and gather: the orbs he collects are his rate of fire |
| payload | a page that flies at an enemy and cuts it |
| band | mid |

- **The first orb** (Eric's question): the page every 2 s is the floor that gets the first kills; gathering
  only quickens it. With no enemy in reach, turned pages wait in the book, up to 5, rather than being lost.
- **Starts small** (Eric's rule): level 1 throws one page a turn; levels add pages, edge and a faster turn.
- **His Apotheosis stays The Spirit Mirror (verb *pull*, his already)**, rebuilt on Folio: *every tenth page,
  his black mirror opens over the thickest pack and draws every orb and flask in its reach to him - and every
  orb it brings turns a page*. A storm of pages. The mirror deepens Darkness, as the form did on Hollow Star.
  A save that saw the old form keeps it seen (as Imhotep's did, `SaveData.MovedAscensions`).
- **Hollow Star** stays a spell anyone can earn and stays in Ra's Orrery (`second-half.md` 3.2); no wizard
  opens on it, so Dee may now fuse The Taking. A future star-gazer wizard could take it: **Tycho Brahe**, the
  astronomer-astrologer-alchemist who saw the new star of 1572 (and wore a metal nose) - a separate job.
- **Built `84e24368`** (`spell-roster.md` 2a, row 36): 7 damage a page to 16 at level 8; one page a turn, two at
  level 3, three at level 6; an orb turns exactly one page, a timed turn all of the level's. The cards are Torn
  Leaf, Inkblot (+Darkness), Gilt Edge, Bound Volume (+Arcane) and Commonplace Book. The Spirit Mirror's last
  sentence sits on its card's second line (the first ran past the 130-character budget). Others earn it through
  the **Magpie** Feat, open 10 chests in a single run. Pages, a small book at his shoulder and the ink are drawn
  in code on the 2 px grid. Still to see in play: the pages' size on the phone (about 10x12 px), the mirror,
  the orb-to-page loop, and whether 10 chests is the right bar.
