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
| Medea | Rejuvenation: +20% healing | **weak** | right story (she made old Aeson young), but healing is a trickle since the difficulty pass (`difficulty-curve.md` 5), so it is worth 2-5 HP a minute |
| the Cumaean Sibyl | Prophecy: +3 rerolls | strong | other futures to choose from |
| Paracelsus | Sola Dosis: +25% damage over time | strong | the dose |
| Abe no Seimei | Exorcist: +15% damage to elites | strong | the exorcist |
| Imhotep | Surveyor: +20% pickup reach | good | the surveyor |
| Wayland | Wings: +10% speed | strong | the wings he made, on a lamed leg |
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
| 7 | Väinämöinen | keeps Bramble Seed and Song of Making, unchanged. The kantele passive (section 4) stays a proposal | "next" | kept |
| 8 | John Dee | **opens on a new spell, Folio**, and keeps Scattered Library. **Hollow Star becomes nobody's opener**. His Apotheosis, The Spirit Mirror (*pull*), moves onto Folio | "I don't think hollow star feels like a good spell for him - what spell could feel like his? can we trade hollow star to someone else, like someone who did more with science/stars, and have something that feels more like his?" / "I like your suggestions - how would john dee produce the first orb if he only fires when he picks up an orb? also, can we still keep his spirit mirror finale to work with this?" / "yes" | built `84e24368` (section 6); not yet played |
| 9 | Laelaps | keeps Cyclone Slash and Fated to Catch, unchanged. (Re-drawing the blades as a hound and fox of wind, the endless chase, stays an unasked idea) | "next" | kept |
| 10 | Abe no Seimei | keeps Kindled Ward and Exorcist, unchanged | "next" | kept |
| 11 | The Dark Wizard | keeps The Taking and Dread, unchanged. **His unlock is a mystery achievement**: The Taker Taken shows its condition (every chapter's boss on Hardcore) from the first run, and reads "???" for its name and its reward, with the seal, until done (`SealedName`/`SealedReward`, as a Conjunction's Feat reads before it is made). It was already listed under the Throne as "The Taker Taken - Frees someone": the title gave away who is taken | "do not change the dark wizard, but add his unlock condition as a mystery achievement" | built `ff5fa9a6`, validated (0 errors); the page not looked at |
| 12 | Merlin | keeps Orb of Ages and Warding Hand, unchanged | "Next" | kept |
| 13 | Baba Yaga | keeps Thorned Vine. **She moves flying in a mortar, holding a broom** (she joins Nimue among the floaters, `FLOATERS` in `tools/art/roster.py`, `anim_sets.hover`; the look only, her feet collider unchanged). **Bone Fence is in question**: her own vine pushes nothing (`Player.cs`: "Her own vine pushes nothing; it is a build's passive"), so +20% knockback does nothing until she takes a spell or card that shoves . **Fixed by option A: Thorned Vine throws what it cuts aside**, along its stroke (`ThornVine.VineShoveSpeed`, 180, a first guess), for every wizard; Bone Fence keeps +20% knockback and now works from her first cast. Entangle still roots instead | "Her typical movement can be flying in a mortar holding a broom, also does thorned vine have knockback? how often is her passive useful?" / "A: vine shoves" | vine shove built `f78d8744` (not yet validated in Godot or played); mortar and broom being drawn |
| 14 | Morgan le Fay | keeps Sleet Runes (Ice + Wind is the weather her mirage forms in: cold air over cold sea). **Avalon is replaced by Glamour: 1 in 10 blows strike a mirage of her instead** - a dodge added to the roll in `TakeDamage` step 2 (`Player.Mirage.cs`, `glamour_evasion_10`), so a turned blow shows the Blur's ghosts. Avalon needed a revive she rarely held | "i like your avalon passive suggestion" / (asked: Glamour, or Avalon as her own revive?) "glamour" | built; not yet validated in Godot or played |

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
