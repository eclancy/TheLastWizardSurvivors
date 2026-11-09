# Chapter wizards: one held in every place

**Latest placement, 2026-11-06 (Eric): Merlin belongs to the Broken Circle.** Restore the Moot's
shattered council wards to free him and unlock Orb of Ages, keeping Gilded Mote. Medea's granary
returns to the Fields, alongside the separate Laelaps rescue. This supersedes the interim move below.

**Earlier placement override, 2026-11-06:** Laelaps is freed with the three companions in the
Fallow Fields' Pinfold, alongside Familiar. Medea's existing tithe-scale rescue moves to the Broken
Circle as its second side event, replacing that chapter's former Laelaps unlock. Coursing becomes
a 50 Arcane Energy challenge played as Laelaps. Baba Yaga remains in the Enchanted Forest, but her
hut is now 6-8 chunks out in a walkable clearing with mature woodland around at least half a 440 px
ring. Existing unlocks, deed IDs and campaign-gate membership are preserved. Historical placements
in the design sections below are superseded by this note.

**2026-11-03: six of the ten asks below are to become puzzles** - Baba Yaga's, Medea's, Paracelsus's,
Imhotep's, Ma Liang's and Morgan's, with Tituba's and Väinämöinen's in `.ai/side-events.md` -
`.ai/unlock-puzzles.md`, designed, not built. Seimei, Wayland, the Sibyl and Nostradamus keep theirs.

Eric, 2026-11-01: *"Let's begin adding a character unlockable on every level, with a unique unlockable
character theme on each level that is unique to that level."*

**Status: approved by Eric, 2026-11-01** (section 0's decisions below). **Built: P1, the pilot Abe no Seimei** (section 4.5, "Built") **Wayland the Smith** (4.7, "Built") **the Cumaean Sibyl** (4.3, "Built") **Baba Yaga, the first second event** (4.1, "Built"), **Morgan le Fay** (4.9, "Built"), **Paracelsus, the first escort** (4.4, "Built"), **Nostradamus, the Court's first event** (4.10, "Built"), **Imhotep, the beacons** (4.6, "Built"), **Medea, the Fields' second event** (4.2, "Built"), **Ma Liang, the Court's second** (4.8, "Built"), **and P2, two events in one chapter** (6.3, "Built"); every one of the ten is built. It covers the ten
chapters that hold nobody today: the Enchanted Forest, the Fallow Fields, the Sunken Cave, the Blighted
Swamp, the Mystic Ruins, the Scorched Sands, the Emberdeep, the Last Bridge and the Court (which holds
two: Nostradamus, and Ma Liang since the Sketchbook was emptied - 4.8). Tituba (Cursed Dungeon),
Väinämöinen (Frozen Waste), Laelaps (Broken Circle) and Nimue, the Lady of the Lake (the Throne,
`.ai/endgame-castle.md` 10.1 item 11) are spoken for; the Throne's kept wizard was named Merlin until
2026-11-02, when the name moved to a wizard earned by the whole road (`.ai/merlin.md`). John Dee comes from
chests, not a place. **The Sketchbook holds nothing to earn** (Eric, 2026-11-02: "the sketchbook should
not have any unlocks or any achievements. move that to other maps";
`RegressionChecks.ValidateSketchbookHoldsNothing`). The dark wizard himself is playable too, held nowhere
and earned by beating every chapter's boss on Hardcore: `.ai/dark-wizard-playable.md` (he claims the verb
**take**). Merlin, the old enchanter, is earned by beating every chapter's boss on Default or harder (he
claims **rewind**). He is not the one he missed: that is the player, the blue-robed title figure - playable from the
first run as **The Archmage**, the fifth starter, on Magic Missile (he claims **count**; `.ai/protagonist.md`).

Read first: `.ai/world-and-tone.md` (why the rescued are named people), `.ai/side-events.md` (the
five events, hints, secret wizards), `.ai/achievements-and-secrets.md` (every unlock is one
achievement), `.ai/decisions/2026-10-10-apotheosis.md` (one verb per form).

---

## 0. What Eric must decide first

**Decided by Eric, 2026-11-01.** "Only default starting wizards that are unlocked on a new save file need
to have mono element spells. Unlockable wizards can have multi-element starting spells." So question 1
is settled that way: the one-element rule binds only the starters a new save holds; a rescued wizard
may open on a two-element spell, and may share an element with another wizard when their starting
spells differ (`ValidateStartingCharacters` is relaxed to match when the first of these is built).
Question 4: **the Sketchbook's wizard is Ma Liang** (the kids' boss there is now Elim, below) - moved to
the Court on 2026-11-02, when the Sketchbook was emptied of every unlock (4.8). "All else
approved": questions 2, 3, 5 and 6 as recommended - the new wizards count only at the Throne's gate, a
second side event where a chapter already has one, Baba Yaga and Medea drawn serious, Nostradamus takes
Starfall and the Throne's kept wizard the sword - Nimue since 2026-11-02 (Eric: "i like that the lady of
the lake keeps spiritual weapon"); the Throne's kept wizard was named Merlin until 2026-11-02, when the name moved to the wizard earned by the whole road (`.ai/merlin.md`).

The questions as they were put:

| # | question | options | recommended |
|---|---|---|---|
| 1 | **The one-element rule.** `ValidateStartingCharacters` lets no two characters open on one element, and every opener must be pure. Eight elements are claimed; only Arcane, Light, Poison and Metal are left, so at most **four** more wizards fit, and Merlin (now Nimue) needs one of them | **R1** two wizards may share an element if they open on different spells; **R2** a rescued wizard may also open on a hybrid; **R3** keep the rule and build a new pure spell per wizard | **R1 + R2.** It is what "one wizard per offensive spell" needs anyway: 18 of the 34 offensive spells are hybrids. The four disciplines keep distinct pure elements |
| 2 | **Which gate counts them.** The Emberdeep counts every wizard whose rescue comes before it. Six of these are on the road, so a new save would need **eight** rescues before chapter 9 | **G1** as written: the Emberdeep counts them (no code); **G2** the Emberdeep counts today's six, and every chapter wizard is counted at the Throne, where he is | **G2.** The fiction's sentence ("he is reachable only when every wizard is free") is true at the Throne either way. G1 turns the road into a checklist of eight events |
| 3 | **Where the prison goes in a chapter that already has an event** (Forest, Fields, Swamp, Sands) | a **second side event** per chapter; or re-point the existing event to pay the wizard | **A second event.** Re-pointing moves Bramble Seed, Contagion, Iron Palisade and Wishing Coin, and `Reconcile` would hand the wizard to every save that ever won the old event |
| 4 | **The Sketchbook.** It is outside the fiction and its secrets pay "no meta reward but the page mark" | Ma Liang, drawn by us; or a wizard **the kids draw and name**; or none | **Ask the kids first.** If they want to, theirs; otherwise Ma Liang |
| 5 | **Tone calls** | Baba Yaga (feared, not comic) and Medea (Euripides' Medea killed her children) | Both, drawn serious. Alternates are listed for each |
| 6 | **Starfall** suits two seers: Nostradamus here and Merlin later (now Nimue) | Nostradamus takes it, Merlin (now Nimue) takes Spiritual Weapon's sword; or the reverse | Nostradamus. The sword in the stone is Merlin's better story (the Lady of the Lake's, since 2026-11-02) |

---

## 1. What is fixed already

| rule | where | what it means here |
|---|---|---|
| Starters are disciplines; the rescued are named historic or legendary magic-workers | `world-and-tone.md` | every proposal is a person |
| Each Apotheosis owns a verb no other does | `ValidateApotheosisCatalog` | taken: **burn, stop, link, hold, flood, swarm, pull, reflect, banish** (Seimei), **take** (the Dark Wizard), **rewind** (Merlin, earned by the whole road, `.ai/merlin.md`, 2026-11-02), **temper** (Wayland), **echo** (the Sibyl), **count** (The Archmage, the fifth starter, `.ai/protagonist.md`), **double** (Morgan le Fay), **displace** (Ma Liang, on the Familiar's crow only); **sever** (Nimue, the Lady of the Lake: Excalibur, decided by Eric 2026-11-02, built 2026-11-03 - `.ai/endgame-castle.md` decision 11, its Built note). Unclaimed in the ADR: contagion |
| No path on his spell trades his element away | `ValidateApotheosisCatalog` | since 2026-11-02 the form names no path - the spell at max level, ascended down any branch, never combined - so every level 4 and level 8 option on a wizard's spell leads to it, and none may trade away the element the form deepens (it would come back as a third). The old rule, a required path that never leads away (`CheckRequiredPath`), went with the named paths |
| A secret never pays a wizard | `achievements-and-secrets.md` 6.2 | every rescue below is a **side event** (a Road deed), never a secret |
| ~~One side event per chapter~~ A chapter's events are a list (P2, built) | `SideEventDirector.EventsFor` | question 3; 6.3 "Built" says how a second is added |
| A secret wizard is revealed by the chapter whose event frees him | `ValidateSecret` | the Forest is open on a new save, so its wizard **cannot be secret**: a silhouette from the first run |
| The kids' art is never altered or extended, and nothing new goes out under their name | `CLAUDE.md`, `stage-secrets.md` 6.9 | nothing is added to the Sketchbook: it holds no wizard, deed or secret (`ValidateSketchbookHoldsNothing`) |
| Bosses are immune to crowd control that stops them reaching the player, and to executes | `in-flight.md` 3a item 4, section 4 | every verb below that moves, turns, blinds, seals or interrupts skips a `BossEnemy` |
| A hint never names what it is about | `ValidateStageHints` | no caption below names a wizard, a spell or what to do |
| The rescued wear no player pigment; only the dark wizard spends the darkness ramp as light | `side-events.md`, `world-and-tone.md` | sprites in `tools/art/roster.py`, staff head the only saturated thing |

---

## 2. The roster

| # | chapter | wizard | element (opener) | starting spell | verb | form | how freed | rule needed |
|---|---|---|---|---|---|---|---|---|
| 1 | Enchanted Forest | **Baba Yaga** | Grass (Grass+Poison) | Thorned Vine | **feed** | The Hungry Hedge | **hunt** the walking hut | R2 |
| 2 | Fallow Fields | **Medea** | Fire (Light+Fire) | Solar Flare | **turn** | Dragon's Teeth | **pay** the reeve's tithe | R2 |
| 3 | Sunken Cave | **the Cumaean Sibyl** | Arcane | Arcane Explosion | **echo** | The Hundred Mouths | **listen** for the true mouth | none (R1 if Ma Liang too) |
| 4 | Blighted Swamp | **Paracelsus** | Poison | Contagion | **distil** | The Dose | **escort** him to the ferry | none |
| 5 | Mystic Ruins | **Abe no Seimei** | Light | Kindled Ward | **banish** | The Five-Pointed Seal | **solve** the four stones | none |
| 6 | Scorched Sands | **Imhotep** | Fire (Fire+Arcane) | Scorching Ray (Fireball until 2026-11-09, `.ai/character-fit.md`) | **blind** | Eye of Ra | **aim** bronze mirrors to bring the sun to his door (was kindle three beacons; `.ai/unlock-puzzles.md` 3.1) | R1 |
| 7 | The Emberdeep | **Wayland the Smith** | Metal | Iron Palisade | **temper** | The Tempered Line | **bait** the hammers onto his chains | none |
| 8 | The Court (second event; was the Sketchbook) | **Ma Liang** | Arcane | Familiar (crow) | **displace** | The Painted Door | **trace** the dotted door | R1; P2 |
| 9 | The Last Bridge | **Morgan le Fay** | Ice (Ice+Wind) | Sleet Runes | **double** | Fata Morgana | **light the lamps ahead** of her barge (was keep pace; `.ai/unlock-puzzles.md` 3.7) | R2 (R1 on Stormsweep) |
| 10 | The Court | **Nostradamus** | Light | Starfall | **foresee** | The Quatrains | **seek** his voice room to room | R1 |

Four open on a spell found in their own chapter (Thorned Vine, Contagion, Kindled Ward, Fireball), so
finding the spell and freeing its wizard tell one story. Under the strict rule as it stands, only
**Paracelsus, Seimei, Wayland** and one Arcane wizard can be built. Nimue (the Throne's kept wizard, Merlin
until 2026-11-02) keeps Spiritual Weapon's sword; Merlin, earned by the whole road, opens on a spell of his
own (Orb of Ages, `.ai/merlin.md`).

Ten new verbs, none taken: feed, turn, echo, distil, banish, blind, temper, displace, double, foresee.
Spares not used: silence, lead, hide.

---

## 3. What each prison asks

The five existing events ask: stand still (grove), carry (key), explore (lanterns), fight here (thaw),
catch (caravan), and the early chapters defend (well) and follow (moot). None of the ten repeats those,
and none is "a harder fight".

| ask | chapter | in one line | nearest thing already in the game, and the difference |
|---|---|---|---|
| **hunt** | Forest | a hut on hen's legs walks away from you; keep it close until it tires | the Pale Hart waits for you; this flees |
| **pay** | Fields | XP that falls near the reeve's scale goes into its pan, not to you | nothing: the only ask that costs the player something |
| **listen** | Cave | one of nine cave mouths is hers; the voice reaches you from it first | the moot lights its next stone; here nothing lights, you hear |
| **escort** | Swamp | he follows you to the ferry; the bog goes for him | the key is a thing you carry; he is a person they hunt |
| **solve** | Ruins | turn four stones to face the seal | the Broken Ward is an order; this is a state (keep it apart in art) |
| **aim** | Sands | turn bronze mirrors - each faces away from where he stands - until the sun goes mirror to mirror onto the disc over the tomb's door (since 2026-11-08, `.ai/unlock-puzzles.md` 3.1; it was kindle three beacons at once) | the seal's stones turn a quarter when a plate is stepped on, and face the seal; these follow his feet, eight ways, and pass the light on |
| **bait** | Emberdeep | only his own forge's hammers break his chains; bring their slams to the stakes | nothing: the only ask that uses the enemy's attacks |
| **trace** | Court (the Gallery) | walk a door sketched in charcoal dot to dot | nothing else is a drawn line |
| **light ahead** | Last Bridge | she drifts only as far as the parapet's lamps are lit, and a lamp burns 20 s: light the next ones ahead of her, not the whole row (since 2026-11-07, `.ai/unlock-puzzles.md` 3.7; it was keep pace) | the moot's stones are lit in its order against a clock; these are lit in turn, ahead of something moving, and lit too early they are out by the time she gets there |
| **seek** | Court | his voice comes from a room; reach it, and it comes from another | the hart is seen; he is only heard, through walls |

Every one inherits `side-events.md`'s rules: placed from the seed 4-6 chunks out (the castle's two by
their chapter's own rules), dormant until the player arrives, nothing timed by the pace clock, every
event enemy an elite, nothing placed joining `"enemies"`, a failure that resets and never locks, and
once freed, later wins pay a chest.

---

## 4. The ten

Each block: who, why he keeps them there, the spell, the form (card of one or two sentences and a
tagline of 34 characters or fewer), the passive (an axis no other wizard owns), the ask with first-guess
numbers, the clue, and an alternate where one is worth having.

### 4.1 Enchanted Forest - Baba Yaga

- **Who.** The witch of the deep wood in Slavic tales: a hut that walks on hen's legs behind a fence
  of bone; she tests whoever comes to her door and pays the ones who pass. Drawn tall, gaunt and
  iron-grey, never a cackling hag.
- **Why here.** Every path in a wood once led to her door. He bent the wood so that every path leads
  inward (the chapter's own line) and set the hut walking its circle for ever.
- **Spell.** **Thorned Vine** (Grass+Poison), Elderbark's deed, the Forest's own: a vine that sweeps the
  densest pack and poisons it. Pure alternative: none is free (Bramble Seed is Väinämöinen's); R3 means a
  new pure Grass spell.
- **Form: The Hungry Hedge (feed).** *"Every enemy the vine kills feeds it: it sweeps further and wider
  for the rest of the run."* Tagline: *"Feed it early. It remembers."* Growth capped (first guess +60%
  length, +40% width). Path: the level 4 that deepens Grass, the untagged level 8.
- **Passive: Bone Fence**, +20% knockback.
- **Freed by: hunt.** The hut walks the deep wood. Within 420 px it turns and strides away, quicker
  than his walk for 2 s, then slower. A bar fills while he is within 520 px and holds while he is not;
  at 30 s it tires, kneels, the door turns to him and she steps out. Water or trunks in its way stop it
  early. Losing it costs nothing: its prints lead back.
- **Clue.** A slow two-beat tread shakes the ground from her side; prints of a great three-toed foot
  appear in the moss from two chunks out; birds lift out of the canopy toward her. Sound
  `hint_hut_step` (two heavy falls, a timber creak). Lines: *"Something heavy walks the deep wood, on two
  legs."* / *"The steps again. Too heavy for a man."* / *"Still walking, round and round."*
- **Locked hint:** "Kept walking in the Enchanted Forest."
- ~~Alternate: Nimue~~ - **taken by the Throne** (2026-11-02): she is the Throne's kept wizard, the Lady of
  the Lake with Spiritual Weapon's sword (`.ai/endgame-castle.md` decision 11), so she is never offered here.

**Built (2026-11-03, `wip/wizard-babayaga`).** The first chapter wizard held where an event already stood: the walking
hut is the Forest's second event, after the grove, through P2's API exactly as 6.3 wrote it (one line in
`SideEventDirector`, a hint by site, deed words, `FromEvent`, a prisoner row). Nothing in P2 had to change.

- **Her.** `CharacterData_BabaYaga.tres`, appended to `CharacterRoster`. Name "Baba Yaga", id `baba_yaga`. `roster.yaga()`:
  tall, gaunt and iron-grey - the narrowest figure in the cast, stooped a little at the shoulders - in a long coat of
  linen's dark end (the moss-grey of the deep wood) to the ground, an iron-grey shawl fringed at its point, a kerchief of
  stone's near-black, her own iron-grey hair long down her back on the far side (the break in her outline that is nobody
  else's). Her near arm reaches down, long fingers spread over a thorned shoot coming up out of the earth - the only
  figure whose spell comes from below - and her far hand plants the pestle she steers her mortar with, the club of its
  head at the top, a thorned vine climbing it with its living tip curling over. The tip and the shoot are grass's ramp,
  one bead of poison on a thorn: the only light on her. Bone-Leg, as the tales call her: the far foot under the hem is
  bone. Face set and level, never a grin. 0.88 edges/px. Sheets written by `build.py`'s wizard loop for this name only.
  **Since 2026-11-09 she flies in her mortar** (Eric: "Her typical movement can be flying in a mortar holding a broom";
  `084cc285`, `.ai/character-fit.md`): a mortar carved from one log hides her from the hips down, two rows above a shadow
  that narrows as she rises, and she joins Nimue among the floaters (`FLOATERS`, `anim_sets.hover`: a hover idle, a drift
  for a walk; her Feet collider is unchanged). The pestle became a broom whose twigs stream back as she moves; the vine
  climbs its handle, its tip and poison bead her only light (`CharacterVisuals` staff tip (14, 4)); the shoot from the
  earth went (a floater has no ground under her hand) and Bone-Leg is hidden in the mortar. Her wood is the gold row from
  its deep end, on her own palette bytes (`WOOD_RAMP`): no new colours. 0.94 edges/px. `WalkingHutEvent` still says she
  "steps out" of the door; she now comes out in her mortar.
- **Her passive, Bone Fence** (`knockback_20`): +20% to every shove and pull the player's side gives -
  `Enemy.PlayerKnockbackScale`, read by `Enemy.ApplyKnockback` and set back to 1 by every `Player._Ready`. The two shoves
  that are the place's, not his (the Broken Ward pushing the swarm out of its ring, the Court's rooms), now go through
  `Enemy.ApplyWorldShove`, which it never touches. Her own vine pushed nothing, so it was a build's passive - until
  2026-11-09, when Thorned Vine began throwing what it cuts aside (`ThornVine.VineShoveSpeed`; Eric chose it, `.ai/character-fit.md`).
- **The event** (`WalkingHutEvent`, `tools/art/walking_hut.py`, `world/hut/`). The hut stands on a 280 px open disc swept
  for through the band (`EventSiteInBand`, dense) and walks a 200 px circle round it at 45 px/s whenever someone is
  within 3200 px - "round and round". It begins when he comes within 480 px of the HUT (or of the circle's middle).
  Within **420 px** it turns its back and **bolts at 1.5x his walk for 2 s** (`Player.WalkingSpeed`, so a wizard with
  Wings is still outrun), easing to 1.15x as it tires; while he stays that close it strides on at 0.75x; further off it
  treads its own way at 60 px/s, turning off from him by degrees. A bar fills while he is within **520 px** and holds
  while he is not; at **30 s** it stops, turns (the side frame), shows its door, kneels, the door opens and she steps
  out of it toward him (2.35-3.1 s after it tires), and is gone into the column of light. **Water, a wall, a fence or a
  trunk across the way it wants cuts a bolt short**: it slides off along the first open way at the stride, or stands at
  bay. Left alone past **1100 px for 3 s** it calms and stands where it is, the bar held - losing it costs nothing, the
  gold arrow stays on it, and its prints lead back. The circle's middle is marked done once the hunt begins, so the only
  arrow is the one on the hut. Nothing here is a body: the hut is a sprite y-sorted at its feet, never in "enemies",
  with no collision layer; it is stopped by the streamer's data (`Node2DGame.IsGroundBlockedAt`), the navigation
  grid's prop clusters and a one-point physics query on the blocking layer for lone trunks - what stops the player's
  boots stops its feet. Each fall of a foot plays `impact_earth` there.
- **The primitive** (section 7): `FleeingMover`, SecretMover inverted - a plain class (`Step(delta, chaser,
  chaserSpeed)`) so the validator chases it on paper with the game's own rules.
- **The prints.** Every fall lays a fresh three-toed print (a ring of 96); from two chunks out, three old trails spiral
  in through the moss to the worn ring of its circle (laid from the placer, so they are the run's; each tested against
  the ground the first time it is near enough to draw). **Laid toes-back** (`PrintDirection`): a bird's print is an
  arrow pointing back along its walk, and a player reads an arrow before he reads a foot, so every print points the way
  it went - they lead to it. Fresh prints carry turned earth and go old after 50 s.
- **Her unlock** is the site's derived Road deed, "The Hut Run Down", counted at the Throne only. **Not secret**
  (section 1): a silhouette from the first run, locked hint "Kept walking in the Enchanted Forest." Quest-log prisoner
  "the one in the walking hut", held line "Something heavy walks the deep wood, round and round." - so the Forest's note
  carries it from a new save.
- **Her clue.** `HintCue.Birds`, a new last row of `hint-cues.png` (every row before it unchanged, byte for byte): a bird
  beating up out of the canopy, four beats near and four further off, in his violet (the hut's far tell, the eye-light in
  the horse skull on its gable), lifting at the edge on her side and flying out toward her on each beat of
  `hint_hut_step` - two heavy falls 0.64 s apart and the hut's timbers creaking (`sfx._HUT_FALLS`, `_HUT_CREAK` =
  `StageHintCue.HutBeats`). Nearest sound in the whole set 0.187 (`spell_explosion`). The three lines above, unchanged.
  The ground does not shake: the sound's falls carry the tread.
- **Her form**, The Hungry Hedge (feed, `ThornVine.HungryHedgeId`, `Player.HungryHedge.cs`). A kill counts when a hit whose
  source is the vine in this form takes the last of an enemy's health - the lash, the Thicket's brambles and the Barbed
  Lash's thorns all hand over the spell; its poison ticks are the enemy's own and do not. Each cast reads the growth as it
  comes up (`ThornVine.Feed`): up to **+60% length and +40% arc**, full at **60 kills**, never shrinking. The vine is drawn
  out of its own length and arc (and its width follows its length), so a fed vine is seen to be bigger. Its own
  evolution icon (the vine grown over the whole frame, bones at its root). No card on Thorned Vine trades Grass away
  (Barbed Lash deepens it, Thicket adds Poison beside it), so every route leads to the form.

**What moved, and why.**

1. **The bolt is 1.5x his walk, not 1.3x.** At 1.3 a 2 s bolt gains 132 px, so from just inside the startle radius it
   never left the 520 px reach: the probe walked straight at it and ran it down in 30.5-33 s on four seeds, a bar of 30.
   The bolt had no teeth. Modelled at 1.5: rushing it on open ground costs about 8 s out of reach (38 s), rushing it
   through a crowd at 80% of his walk about 71 s - and shadowing it in the 100 px band between the radii, so it never
   bolts, is still 30 s. The band is the skill.
2. **Full growth at 60 kills, not a first guess of 400.** The vine's own last hits are a small share of a run's kills (one
   spell of six, the poison's not counted): the seek bot's ate 73 in the five minutes after taking the form and ended the
   run 18% grown. At 60 it ate 25 then 26 then 24 a minute (seed 1234) and 36 then 43 (seed 77) - the bigger it grows the
   more it eats, which is the verb - and was full by about 13.5 and 11.5 minutes.
3. **A 96x128 cell, not the boss's 96.** She has to come out of the door, and she is 88 px tall; in a 96-row hut with its
   legs under it the door is 56 px. 128 rows give the walls 42, the door 34 (68 px: she stoops through it) and the legs
   48. Render scale is still x2.
4. **Its back to him the whole time it walks**, whatever way it goes - the tales' "turn your back to the forest, your front
   to me" - so the walk is drawn from behind only and the door appears when it turns to kneel.
5. **Lost, it stands** where it is rather than going back to its circle: a hut that walked home across ground nobody has
   painted would be stopped by nothing, and one that waits is one the prints lead to.
6. The doc's "Path: the level 4 that deepens Grass, the untagged level 8" went with the named paths on 2026-11-02: the
   gate is max level, ascended down any branch, never combined, player level 21.

**Verified:** build; the validator (0 errors, main's 1 warning); `RegressionChecks.ValidateWalkingHut` (the radii nest;
on paper on open ground a straight chase and a band-holding chase both fill the bar inside two minutes and the straight
one startles it repeatedly; run at a shore it never walks into the water and a bolt is cut short; left 1700 px behind it
calms and stands; every print column points the way it went; the hint's beats are the sound's; the sheets' sizes).
`_ExploreProbe --stage=0`, which HUNTS at his own walking speed and never teleports onto it, holding just inside the
startle radius so it bolts whenever it can: seeds 4242 and 31 `--fresh-save`, 7 `--all-unlocked` (the chest), 1001
`--fresh-save --wayfinder-first`, and 4242 `--all-unlocked` again on the final build - 40 to 48 passed, 0 failed; both
events found and won every time, the two sites 133 to 170 degrees apart, the hut 4.6 to 5.5 chunks out; 10 to 12 bolts,
2 to 6 cut short by the ground, run down 33.5 to 41.0 s after arriving; Baba Yaga freed in memory and her deed recorded by
her site; the hints alternated grove and hut, then the hut had every slot after the grove was won. `--lose-hut` (seed
31): at 40% he walked 1600 px off and waited - the bar held at 40.2%, the hut calmed and stood; he walked back, found it
and ran it down (48.6 s after arriving). `_UiShot` of the hut on its circle, the hunt begun, the clue and the win at both
canvases, and the character select locked (her silhouette and hint on a new save) and freed at both.
`_BalanceHarness --character=N --stage=0 --minutes=15 --all-unlocked --seek-apotheosis --immortal` on seeds 1234 and 77:
the spell gate met at 7.9 and 7.0 minutes (player level 17), the form offered and taken at 10.9 and 9.7 minutes (level
21); the hedge ate 88 and 181 and was full by the end of both; the vine 18.7% of seed 77's damage.

**Not verified: whether hunting it is fun** - section 7's own question - nor whether the hut reads at a glance through a
real crowd (the probe clears the swarm round itself), nor whether a player finds the 420-520 band without being told (the
objective says only "keep up"); the sound, measured and never heard; the birds at a glance mid-fight; the Hungry Hedge's
effect on a real run (kills a minute are held by the spawn economy at 205-285 either side of the form, so the harness
cannot see it, and the bot's frame rates were 2-36 fps with other sessions' runs beside it); Bone Fence, which no build
the bot makes as her uses; nothing on the phone.

### 4.2 Fallow Fields - Medea

- **Who.** The sorceress of Colchis, granddaughter of the Sun, who gave Jason the charm to yoke the
  fire-breathing bulls and plough the field of Ares, and set the soldiers sown there on each other with
  one thrown stone.
- **Why here.** "He takes a tithe of everything that grows here." He counts her among what grows: she
  is held in the reeve's granary against a tithe the village could never pay.
- **Spell.** **Solar Flare** (Light+Fire): her grandfather's sun round her, growing while she stands
  still. Pure alternative: none free (R3).
- **Form: Dragon's Teeth (turn).** *"At full growth the flare throws a stone of light into the crowd:
  what it strikes turns on its neighbours for three seconds."* Tagline: *"One stone, and they fight."*
  Elites are struck, not turned; bosses as the rule says. Needs a new enemy state (attack the nearest
  other enemy), the largest code cost in this doc.
- **Passive: Rejuvenation**, +20% healing from every source.
- **Freed by: pay.** The Tithe-Reeve's great scale stands in the granary yard. From arrival, every XP orb
  that falls within 320 px of it drifts into its pan instead of to him. When the pan holds one level's XP
  at that moment of the run, the beam drops and the granary opens. Walking off keeps what is paid. No
  failure, only a price: a level's XP for a wizard. (Not the Tithe Barn, FF4, which is a keepsake.)
- **Clue.** She speaks, as Tituba does, but does not plead. A thin cold herb-smoke drifts low over the
  stubble from her side. Sound `hint_granary_voice`. Lines: *"You there. The granary, past the
  furrows."* / *"He counts me with the grain. Settle his count."* / *"Come, or go. I have waited out
  worse men."*
- **Alternate: Circe**, also the Sun's child, who turned men to beasts. The same spell and verb, a gentler
  legend, no tie to a field.

**Built (2026-11-03, `wip/wizard-medea`).** The Fields' second event, after the well, through P2's API as 6.3 wrote it
(one line in `SideEventDirector`, a hint by site, deed words, `FromEvent`, a prisoner row, a freeing reel). Nothing in
P2 had to change. Section 1 holds: the Fields are the campaign's second chapter, opened by Elderbark, so on a new save
nobody can know she is there - she is **secret until `stage_9`**, counted at the Throne's gate only (`finalGateOnly`).

- **Her.** `CharacterData_Medea.tres`, appended to `CharacterRoster`. Name "Medea", id `medea`. `roster.medea()`, drawn
  serious: upright and square, face level and unsmiling, the near arm out level from the shoulder, palm up, a small sun
  burning over it in the light ramp (Solar Flare, her grandfather's light, the only thing on her that gives off light);
  the far hand at her side with the bronze sickle she cut her herbs with, its crescent turned back behind her - the one
  curve in the cast's outlines. A long peplos of lichen's middle (the sea-green of the water at Colchis), belted high and
  folded to the hip; a mantle of the red row's dark end over the back of her head, falling behind her to the ground and
  across her in one diagonal to the near hip; a thin bronze diadem with the Sun's disc on it over black hair; dried herbs
  at her girdle. 0.98 edges/px. New bytes: lichen as `\x8b-\x8f`; the red row is merlin_concepts' `\x14-\x18`, reused
  byte for byte.
- **Her passive, Rejuvenation** (`healing_20`): +20% to every heal that comes through `Player.Heal` - the flask, the
  Light tier's banked share, the Grass tier, every regeneration line (a point at a time), the relics, the boons, the
  cat's purr, the events' heals - with the fraction **carried** (`rejuvenationCarry`), as `AddXp` carries XP, because
  most of what heals a run now is one-point regeneration ticks and 1.2 rounds back to 1. Not a revive (a life is not a
  heal). Against the healing ledger (`.ai/difficulty-curve.md` 5.1): it scales the budget's trickle, it adds no source,
  and the Light tier's cap of a point a second (her spell carries Light) becomes 1.2 for her, since the cap is on
  what is banked and Rejuvenation acts where it is paid.
- **The event** (`TitheScaleEvent`, `GranaryTithe`, `tools/art/tithe_scale.py`, `world/granary/`). A yard of stubble swept
  for through the band (`EventSiteInBand`, 250 px, dense): the reeve's raised granary on staddle stones (weatherboard
  gone grey, a hipped thatch, double doors at the top of three plank steps, barred by an oak beam in iron brackets and
  padlocked, the reeve's tally in chalk beside them; 96x96 at x2) and in front of it, to the right, his great scale (an
  oak post bound with iron, an iron beam, the empty tithe pan on the left and his iron weight on the right; 64x80 at x2,
  six frames). From arrival every XP orb that comes into the world within **320 px** of the scale is the reeve's
  (`GranaryTithe.Claims`, asked once as it lands): it lifts and drifts up into the pan, slow and quickening, over every
  body; his magnet, his feet, the Gleaning Bell and the Spirit Mirror do not touch it, and the far-orb fold leaves it
  alone. The pan weighs each at what it was worth to him the moment it got there (`Player.XpWorthOf`: every XP bonus and
  the run's pace). **The price is his bar at the level he arrived at** (`XPToNextLevel`), fixed then: "one level's XP at
  that moment of the run". As it fills the beam tips the pan down frame by frame; paid, the beam drops, the bar falls from
  the doors (0.45 s), they open (1.05 s), and she comes out and down the steps toward him (1.35-2.2 s) and is gone into the
  column of light. Anything still in the air falls back to the ground his. A dashed ring at 320 px fills with the pan,
  brighter while he is inside it. **No failure**: walking off keeps what is paid, nothing drains, nothing comes for him;
  and **no ring of enemies** - what pays the tithe is the field's own crowd, killed where the reeve can count it. Nothing
  here is a body: two sprites sorted at their feet, no collision, never in "enemies".
- **Her unlock** is the site's derived Road deed, "The Tithe Paid", secret until `stage_9`, counted at the Throne only;
  locked hint "Counted with the grain in the Fallow Fields." Quest-log prisoner "the one in the granary", held line
  "Someone is kept in the reeve's granary, counted with the grain."; freed line "{0} is free from her prison." (Eric, 2026-11-08; was "{0} has walked out of the granary.") Her
  freeing reel (`freed.py` `WIZARDS`, `Freeings.WizardIds`): `freed-medea-mid.png`, she turned to the Archmage, her sun
  burning again over her palm.
- **Her clue.** `HintCue.Smoke`, a new last row of `hint-cues.png` (every row before it unchanged, byte for byte): wisps
  of a thin cold herb-smoke in the wind ramp's pale body (the far tell's colour - not the well's water, the Fields' other
  event), lying low and flat, drifting in slowly from her side, curling and thinning to a thread, one on each syllable of
  `hint_granary_voice`: a woman's voice through plank walls, an alto falling from B-flat 3 to F-sharp 3, two words, a
  breath, then the longer line, almost no vibrato - said, not called (the cell's cry rises into a question), with a few
  soft crackles of herbs smouldering under the last phrase (`sfx._GRANARY_SYLLABLES` = `StageHintCue.GranaryVoices`).
  `HintVoice.Speech`. The doc's three lines, unchanged. Nearest in the whole set 0.164 (`pickup_health`), then 0.179
  (`hint_cell_cry`). In the world her smoke leaks from the vent at the granary's ridge and drifts off over the yard: the
  far tell, from the first second. The well still has no hint (`SitesWithoutHints`), so every slot is hers.
- **Her form**, Dragon's Teeth (turn, `ElementalPulse.DragonsTeethId`, `Enemy.Turned.cs`). Every pulse that goes off at
  **full growth** (he has stood the whole four seconds) throws one stone of light, lobbed high, onto the densest knot
  within 2.6 times this pulse's reach (of the first 32 bodies in reach, the one with most within the strike). Where it
  lands it strikes everything within **76 px** for **a fifth of the pulse** (his spell, through `DealDamageToEnemy`), then turns
  the ordinary ones nearest it, **up to 8 a stone and 16 in the arena** (`Enemy.MaxTurnedAtOnce`), for **3 s**. A turned
  body looks for the nearest other through the swarm's grid every 0.3 s, goes at it a little faster than it walks,
  stands, and strikes it every 0.6 s for **34% of its own health** - three blows kill one of its own kind. It never
  touches the player (the contact loop passes over it, and `TargetPlayer` is null, so a caster draws no bolt and a
  slammer's wind-up resolves against nobody). Elites (and the Boo) are struck, not turned; bosses are never turned. Lit
  warm while it lasts (`TurnedLook`), with a small mark of the stone's light over its head and a short run of light a third
  of the way toward the one it is going for (`ElementalPulse.DrawTurned`), its blows' numbers in the stone's gold, not his
  red. Fire, the one of Solar Flare's
  two the form deepens; no card on Solar Flare trades either away, so every route leads to it. Its own evolution icon.
- **Where the blows go** (the brief asked for a decision): straight into the target's `TakeDamage` (`TakeTurnedBlow`),
  **never** through `Player.DealDamageToEnemy` - no crit, no multiplier, no Vulnerable, no lifesteal, no chalice, no
  Hungry Hedge feeding, no spell tally, no `EnemyDamagedAt` (the thaw is not warmed by it) - and not through
  `ScaleOutgoingDamage`, which is what an enemy deals the PLAYER. **A death it causes counts as his kill and pays its
  orb**, as a poison tick's does (`Enemy.StartDeath` is the one place a kill is recorded, whatever did it): a form that
  cost the run its XP would be a form nobody took.

**What moved, and why.**

1. **"At that moment of the run" is read twice**: the price is fixed at arrival (his bar at that level), and each orb is
   weighed at its worth when it reaches the pan (the pace multiplier in it). A live price that rose with every level he
   took elsewhere would be a pan that never filled for a player who kept fighting outside the ring.
2. **The granary blocks nothing.** A building that stopped bodies would be terrain, and four places must agree about
   that (`.ai/terrain.md`); the hut is the precedent. It is drawn on its staddles, and what walks behind it is hidden.
3. **No ring of enemies at the scale.** Every other event applies pressure; this one's pressure is the price. The swarm
   that follows him is what pays.
4. **The stone strikes for a fifth of the pulse, not all of it.** At the first guess it killed most of what it struck - a
   level 8 flare with its Ascension one-shots the crowd - and so turned 0.43 a stone: standing still after the form the bot
   threw 178 stones in six minutes and turned 77, whose 45 blows killed 23. At a fifth, on the same seed: 180 stones, 211
   turned (1.17 a stone), 223 blows, 74 killed by their own. What it strikes has to live to turn.
5. **The turned are marked.** The first tint (1.36 / 1.20 / 0.74) was lost on the Fields' dark bodies in the photographs;
   it is 1.6 / 1.34 / 0.6, with the mark and the run of light toward the foe. At most 16, nothing allocated, and the walk of
   the registry is skipped while nobody is turned.
6. Path: the doc's form named no path, and none is needed (the gate is max level, ascended, not combined, level 21).

**Verified:** build; the validator (0 errors, main's 1 warning - the Test Wizard's missing passive);
`RegressionChecks.ValidateTitheScale` (the Fields' line is the well then the scale; the pan comes down frame by frame and
stays in its cell; the tithe on paper - claims inside the reach and not outside, weighs, a closed scale takes nothing; the
clue's beats are the sound's; the sheets' sizes; Dragon's Teeth's numbers and its catalog entry). `_ExploreProbe
--stage=9`, which stands in the yard and kills what comes (`--tithe-level=N` puts him at level N as he arrives, since the
probe comes in the first two minutes at level 1-2, where a level is 10-55 XP): seeds 4242 fresh (level 10: 415 paid in 124
orbs, 23.4 s after arriving), 31 fresh (level 12: 505 in 154, 27.6 s), 1001 fresh `--wayfinder-first` (begun while the well
ran: 55 in 24, 10.3 s), 7 `--all-unlocked` (the chest: 325 in 115, 20.6 s) and 4242 `--all-unlocked --tithe-walk-off` (he
walked 1,500 px off at 40.0% and the pan stood at 42.4% after five seconds away - what was in the air arrived; 415 in 146,
34.4 s): 40 to 46 passed, 0 failed; both events won every time, the sites 98 to 172 degrees apart, the scale 4.2 to 6.0
chunks out; Medea freed in memory and her deed recorded by her site. (Three first runs failed and one hung while six other
Godot runs shared the machine - physics capped at eight steps a frame fell behind the probe's clock, and the well was fouled
in the teleport's twenty-frame wait; run alone, all passed.) `_UiShot` of the scale part-paid (`--event-progress`, new), the
open granary as she comes out, the clue, the form turning a crowd, and the character select locked and freed, at both
canvases. `_BalanceHarness --character=16 --stage=9 --all-unlocked --seek-apotheosis --immortal`, seed 1234: the spell gate
at 9.4 min (level 17) and the form at 12.1 (level 21) on a loaded machine, and the bot - which walks to every orb - threw no
stone in the whole run; with `--still-after-apotheosis` (new: it stands still once the form is taken) and `--timescale=1
--profile=10`, the gate at 7.0 min and the form at 9.3, the numbers in 4 above; kills a minute 320-348 before the form, 412
and 400 the two minutes after. **Frame cost**: the spells section about 205 us a physics step before the form (8 min) and
234 after (13 min), 0.3-0.75 KB a step either side; the late frames slowed to 10-50 fps because a bot that never walks
leaves a thousand orbs on the ground (the orbs section 1.3-1.9 ms a frame), not because of the form. Healing offered
under Rejuvenation 1.9-2.5 HP a minute on those runs.

**Not verified: whether paying a level feels like a fair price** - section 7's own question - nor how long it takes a real
player, whose spells kill outside the ring as well as in it (the probe kills only inside it); whether the pan and the ring
read at a glance in a crowd; the voice, measured and never heard; the smoke at a glance mid-fight; whether a player stands
the four seconds the stone needs, and what Dragon's Teeth is worth against a moving player's flare (the harness either never
stands or never stops standing); Rejuvenation on a real run; nothing on the phone. The tutorial tip on a fresh save ("Stay
moving and kite enemies") is drawn over the objective line in the same place - seen in the photographs, not this work's, and
not fixed here. (Fixed since, 2026-11-03: the band under the timer, `HudBand`, stacks the tip under the line.)

### 4.3 Sunken Cave - the Cumaean Sibyl

- **Who.** The prophetess of Cumae, whose cave had a hundred mouths and spoke with a hundred voices; she
  wrote her prophecies on leaves the wind scattered. Granted long life without youth, she shrank until
  she hung in a jar.
- **Why here.** She sees what is coming. He keeps her where nobody comes to ask, under the water, in a
  jar.
- **Spell.** **Arcane Explosion** (Arcane): it charges off every other cast and bursts out of her, the
  god's voice breaking out. Strict-rule compatible unless Ma Liang is built too.
- **Form: The Hundred Mouths (echo).** *"Each burst sounds your other spells again from where you
  stand, at 40% strength."* Tagline: *"Everything you cast, twice."* Echoes queued a few a frame, never
  echoed again.
- **Passive: Prophecy**, +3 rerolls a run.
- **Freed by: listen.** A cliff of nine cave mouths. Her voice comes out of all of them, each a beat
  later than the last; the first is hers. Stand in it 2 s. A wrong mouth answers with a flight of
  Gloomwings and the order shifts. With the sound off: written leaves blow out of every mouth, and the
  true mouth's carry the warm glint.
- **Clue.** Written leaves on a cold draught, out of place in a cave, drift in from her side. Sound
  `hint_sibyl_voice`, many voices a beat apart. Lines: *"Leaves on a cold draught, every one written
  on."* / *"A woman's voice from far down, in more places than one."* / *"The leaves again, written
  faster now."*
- **Alternate: Mother Shipton**, the Yorkshire prophetess born in a cave by the petrifying well.

**Built (2026-11-02, `wip/wizard-sibyl`).** The design above, with the changes listed under "What moved".

- **Her.** `CharacterData_Sibyl.tres`, appended to `CharacterRoster` after everyone before her (Merlin and
  Wayland were built beside her; the roster's order is the merge's). Name "The Cumaean Sibyl", id `sibyl`. `roster.sibyl()`: old,
  stooped over her own reach, in a hooded palla from the bone row's deep end (a dusty grey-blue in shade)
  over a deep violet stola, leaning on a plain bough whose crook carries a small bronze jar on a cord - the
  jar she was kept in - with the arcane ramp breaking out of its mouth and one crack, the one thing on her
  that glows. Her near arm is flung out low, palm up, written leaves tearing off it. 0.84 edges/px. Passive
  **Prophecy** (`rerolls_3`): +3 rerolls in the run's pool, on top of Fate Fracture's.
- **The event** (`NineMouthsEvent`, `tools/art/nine_mouths.py`, `world/mouths/`). A crescent of the
  caverns' own rock (cave_walls' three values: the top darkest, the face in shade, a lit lip), 560 px wide,
  bowing 120 px away in the middle, with nine ragged, water-worn mouths at its foot, all facing the viewer.
  It is ONE painting cut into nine strips, each y-sorted at its own mouth's foot: no seams, and whoever
  walks behind the rock is hidden by it. Not terrain (it blocks nothing and joins no group, like the
  seal's stones). A dashed threshold ring in front of each mouth. Every **5.5 s** a call: her voice out of
  all nine, **0.24 s** apart, hers first, the rest in a seeded shuffle. Standing in hers (the nearest
  threshold within 26 px of a point halfway to his feet) fills a hold over **2 s**, which drains at a
  quarter of that rate out of it; standing in another for **0.6 s** makes it answer - a flight of **five
  Gloomwings** (the chapter's Fast role, ordinary bodies) out of it, a flare of his colour - and hers moves
  at least two mouths over; the next call (2.6 s later) says where. The ring under a wrong mouth fills
  exactly as hers does until it answers, so a ring cannot be used to test a mouth for free. Placed on a
  300 px open disc (dense) through `ExplorePlacer.EventSiteInBand`: on four seeds 3.6 to 5.3 chunks out.
  When she is free she steps out of her mouth and aside, and is gone with the column of light.
- **The primitive** (section 7): `PlacedSoundSchedule`, a fixed queue of positioned sounds each played
  after its delay through `SfxPlayer.AtPosition`, with a hook on the frame each plays. The hook spawns that
  mouth's breath of cold light and its written leaves, so what is heard and what is seen cannot drift.
- **Fair on a phone speaker.** Three tells point at one mouth, and the first two need no sound: on each
  voice its mouth breathes out light and a puff of written leaves, in the call's order, so the first puff is
  hers; only her leaves carry the warm glint (`SecretSite.DrawGlint`, the secrets' near tell); and, for the
  ear, hers is a person's voice, near and dry (`sibyl_voice`), while the other eight are the same line from
  deep in the rock, dark and hollow (`sibyl_echo`) - so the order and the timbre agree.
- **Her unlock** is the site's derived Road deed, "The True Mouth", secret until `stage_2`, counted at the
  Throne only (G2); locked hint "Heard behind the rock, far down in the Sunken Cave."; quest-log prisoner
  "the voice in the rock", held line "A woman's voice comes out of the rock, from more than one place."
- **Her clue.** `HintCue.Writ`, a new last row of `hint-cues.png` (every row before it unchanged, byte for byte):
  written leaves blown in flat on a cold draught, the first on the line to her, one on each voice of
  `hint_sibyl_voice` (her line, then six echoes a beat apart, `sfx._SIBYL_VOICES` = `StageHintCue.SibylVoices`).
  The three lines above, unchanged.
- **Her form**, The Hundred Mouths (echo), `ArcaneExplosion.HundredMouthsId` and `Player.Echo.cs`. Each
  burst queues her other spells and the player casts them two a physics frame, after the fire loop, through
  `CastSpellNow` - exactly her own cast, so only the spells whose cast puts something new out (see 2 below)
  - with the damage the cast hands its projectiles at 40%
  (`castDamageScale`, folded into `GetSpellDamageMultiplier`). An echo is not a cast: no timer, no tally,
  no `NotifyArcaneCharge` - it never charges the burst and is never echoed again, and a spell already
  waiting is not queued twice. A ring of arcane motes leaves her on each echo. No card on Arcane Explosion
  trades Arcane away (Static Nova adds Lightning beside it).

**What moved, and why.**

1. **Alone, Arcane Explosion keeps its own time.** It charges only off the OTHER spells' casts, and until
   her nobody opened on it, so a Sibyl at level 1 burst once on spawn and never again until her first
   level-up - which she could not reach. With nothing else held that can charge it (`Player.HasArcaneCharger`:
   any other equipped spell not silenced for the run or sealed), its own cooldown sets it off (1.5 s, the old
   timed pulse); the moment a second spell is held it is charge-driven again. It touches no run in which
   Arcane Explosion is taken beside another spell, which is every run before her.
2. **"Your other spells" means the ones whose cast puts something new into the arena**
   (`Player.IsEchoable`): the Black Cat's list (`Player.BlackCatCastable`), plus Scorching Ray and Cone of
   Cold (`EchoOnlyCastable`) - a fresh ray or cone every cast, anchored on the wizard, which is why the cat
   may not cast them and why an echo, coming from where she stands, may. Never echoed: whatever keeps one
   node standing and only refreshes it (an orbit, a breath, an aura, a trail, the Spiritual Weapon, the
   Familiar's pack, the Starfall rain, the facing pair), Aegis Ward, Hunter's Draw and Frost Shard. The first
   build used the cat's list alone, and the seek bot's Arcane loadout (Spiritual Weapon, Magic Missile,
   Scorching Ray, the Familiar, Aegis Ward) echoed Magic Missile and nothing else - 60 to 77 echoes a minute,
   and Arcane Explosion's own blast 90% of the run's damage. "Everything you cast, twice" is true of the
   casts; a loadout of summons and auras gets little from it, and that is the form's build pressure.
3. **The cliff is a crescent, not a wall of nine.** A cliff that blocked would be terrain, which four places
   must agree on (`.ai/terrain.md`); a mouth facing sideways is a sliver at this size. So all nine face the
   viewer along a crescent that bows away, and it blocks nothing. A first draft drew one block of rock per
   mouth; the blocks' flanks made a seam between every pair and their crowns read as gatehouses.
4. **A second tell for the ear** (hers dry, the eight hollow), and **the leaves are paper** - the water
   ramp's pale end with its dark end for the ink. In the ramp's body stop they read as blue gems, and the
   caverns' walls already hold unlit crystals.
5. **The numbers are first guesses**: 5.5 s calls, 0.24 s beat, 2 s hold, 0.6 s to answer, five
   Gloomwings, a shift of at least two mouths.

**Verified:** build; the validator (0 errors, 1 warning - the Test Wizard's missing passive, which main
has); `RegressionChecks.ValidateNineMouths` (hers first on 400 seeded calls, the shift rule, thresholds
that never overlap, the strips tiling the painting, the hint's beat equal to the call's);
`_ExploreProbe --stage=2`, which LISTENS (it stands in the mouth heard first on the last call, never reads
the answer): seeds 4242 and 31 on a fresh save (won 2.6-2.7 s after arriving, her freed in memory), 4242
`--all-unlocked` (a chest), 7 fresh and 1001 `--all-unlocked` with `--wrong-mouth` (the wrong mouth
answered with 5 Gloomwings, hers moved 3 to 7 and 7 to 3, won 5.4-6.0 s after arriving); 24 to 27 passed,
0 failed. `_UiShot` of the call, the hold, the win and the clue at both canvases, and the character select
(freed, locked, phone list). `_BalanceHarness` as the Sibyl, `--stage=2 --all-unlocked --seek-apotheosis
--immortal`: see "The form, measured" below. **Not verified: whether the nine can be told apart by ear,**
on a PC's stereo or a phone's speaker. The sounds were measured, not heard (`analyse.py`: the three are
declared twins, 0.026-0.059 apart, one voice by design; the nearest other sound in the whole set is 0.139,
`hint_lantern_flare`). Whether a 0.24 s beat is long enough to hear "first" through a swarm, whether the
dry/hollow difference survives a phone speaker, and whether the glint reads at a glance mid-fight, are
playtest questions; so is whether listening is fun.

**The form, measured** (`_BalanceHarness` as the Sibyl, `--stage=2 --minutes=15 --all-unlocked
--seek-apotheosis --immortal`, one run each, seed 1234):

| run | her spell ascended | form taken | echoes by 15:00 | Arcane Explosion's share of damage |
|---|---|---|---|---|
| timescale 6, the cat's list only (first build) | 8.7 min (level 17) | 10.5 min | 290 | 90% |
| timescale 6 | 9.5 (17) | 11.1 | 490 | 88% |
| `--timescale=1 --profile=10` | 6.8 (17) | **9.0** (level 21) | 910 | - |

All three take the form between 9.0 and 11.1 minutes, past the 7.5 floor, the level-21 gate
(`ApotheosisCatalog.MinimumPlayerLevel`) setting the time as it does for everyone. The seek bot pours every
pick into Arcane and leaves its other spells at level 1 (Spiritual Weapon, Scorching Ray, Aegis Ward), so
the echoes - 40% of a level-1 spell - are a small share of its damage; what a player's echoes are worth
depends on a loadout of cast spells, and has not been measured by hand.

**Frame cost** (`--timescale=1 --profile=10`, headless, the laptop the harness runs on). The spells section,
where the echoes run, cost 172 us a physics step before the form (8.3 min: 69 us/frame, 0.5 KB/frame) and
222 us after it (14.2 min: 89 us/frame, 0.7 KB/frame), with the loadout also grown meanwhile; frames held at
6.9-7.4 ms either side. One stretch at 13.2-13.9 minutes ran at 23-42 fps, but every section slowed by the
same factor there (enemy steering 8.5 to 29 us a call, the spells 172 to 600 us a step), which is the
machine, not the form: other sessions' Godot runs were going beside it (performance.md section 5 warns of
exactly this). Not measured on the phone.

### 4.4 Blighted Swamp - Paracelsus

- **Who.** The Swiss physician and alchemist (1493-1541) who wrote that the dose makes the poison, and
  named the spirits of the four elements, the marsh lights among them.
- **Why here.** Mother Rot heals herself; he is the one who could cure the rot. So the cure hangs in a
  cage over her mire, breathing it.
- **Spell.** **Contagion** (Poison), the lanterns' spell. Strict-rule compatible.
- **Form: The Dose (distil).** *"Each jump of the plague reaches half as many and doubles its dose. The
  last carrier dies hardest."* Tagline: *"The last one dies hardest."* Four generations at most.
- **Passive: Sola Dosis**, +25% damage over time.
- **Freed by: escort.** His cage hangs from a gibbet over a rot pool; hold the winch 2 s and it comes
  down. He follows at 75% of the player's speed toward the ferry landing, about 1,600 px off, which the
  objective arrow shows. Enemies within 250 px of him go for him; he has a health bar scaled like the
  event enemies. If he falls, the bog takes him back to the cage and the walk starts again. At the
  landing the Ferryman's empty punt takes him. The first ask in the game where someone is hunted who is
  not the player.
- **Clue.** Pale vapour threads drift in low from his side, the only thing in the bog moving against
  the wind (not the lanterns' poison green, which is the other event's far tell). Sound
  `hint_flask_chime`, a tapped flask and a cough. Lines: *"A man coughing, far out on the bog."* /
  *"Glass rings across the water, like a tapped flask."* / *"The coughing again. Weaker."*

**Built (2026-11-03, `wip/wizard-paracelsus`).** The Swamp's second event, after the lanterns, through P2's API as 6.3
wrote it (one line in `SideEventDirector`, a hint by site, deed words, `FromEvent`, a prisoner row); nothing in P2 changed.

> **Its ask has changed since (2026-11-04): `.ai/unlock-puzzles.md` 3.4, "choose his path", and its Built note.** The
> escort below - the 250 px hunt, `Enemy.ClassHuntsEscort`, the bites, the "who goes for him" check - is gone; rot pools
> that hurt only him are on his way instead. `HuntedEscort` is `LedEscort` now, the walking half only. Everything else in
> this note (him, his passive, the gibbet, the landing, the punt, his clue, his form) still stands.

- **Him.** `CharacterData_Paracelsus.tres`, appended to `CharacterRoster`. Name "Paracelsus", id `paracelsus`.
  `roster.paracelsus()`: the one figure EXAMINING his spell - his near arm raised, a round-bellied flask held up to the
  light at the height of his eyes, his face turned up to it and narrowed, reading the dose; the liquid is the poison
  ramp, the only light on him, with a thread of pale vapour off the neck. His far hand rests on the pommel of a long
  sword planted point-down at his side (Azoth, the sword the stories say he kept his remedy in), so the far side of his
  silhouette is a cross, which nobody else's is. Stocky and square, heavy-jawed and clean-shaven, in a wide flat beret
  - the broadest head in the cast - over a physician's gown open down the front, its collar dark fur; under it a
  near-black doublet and a belt with a pouch and two corked vials. The gown is the contract's `red` row from its base
  down (a doctor's scarlet faded to wine), on the bytes the Archmage's sash already holds for that row (`\x14-\x18`,
  in `anim_sets.PLAYER_RAMPS`), so no new material was needed. 0.98 edges/px. Sheets written by `build.py`'s wizard
  loop for this name only.
- **His passive, Sola Dosis** (`dot_25`): +25% to every poison tick on an enemy - `Enemy.PlayerPoisonScale`, read at the
  tick (the one place poison takes health) and rounded stochastically as `ScaleOutgoingDamage` is, set back to 1 by
  every `Player._Ready`. Every poison on an enemy is the player's, so it reaches every spell, rider and item that lays
  one. "Damage over time" is poison in this game: nothing else takes health on a clock.
- **The event** (`GibbetCageEvent`, `tools/art/gibbet.py`, `world/gibbet/`). A gibbet on a cairn of bog stones, its arm
  out over a pool of Mother Rot's rot, a pulley at its end and the anvil's chain (`anvil-link.png`, hung) down to an
  iron cage cut to a man's shape, him inside it standing with his hands on the bars, coughing now and then; a winch at
  the post's foot. Placed on a 180 px open disc through the band (`EventSiteInBand`, dense). **Hold the winch 2 s**
  (within 60 px of its foot; it drains over 2 s when let go) and the cage comes down (1.3 s), the door swings open and
  he steps out onto the bank. He **follows at 75% of the player's walk** (`Player.WalkingSpeed`) to the **ferry
  landing, 1,450-1,750 px off** - the objective arrow is on it, and a faint column stands over it while he walks.
  Every plain chaser within **250 px** of him goes for him; each body within **46 px** of his feet bites every
  **0.35 s** through `ScaleOutgoingDamage` (the chapter's x1.15) off a bar of **60** drawn over his head, which flashes
  as he is bitten; and every **5 s** while he walks the bog raises **three of the chapter's Mired** (Bulk role, 3x the
  run's spawn health) **220-300 px ahead of him, across his way**. If he falls the bog takes him: he sinks where he
  stands (his own sheet, eaten from the feet up), and he is in the cage again, hoisted and coughing, the winch to hold
  again and the walk to start again from the cage, his bar full. At the landing - he is within 90 px of its foot, or
  the player is and he is close behind - he walks the jetty and steps into the Ferryman's empty punt, which drifts off
  into the fog with him, and he is gone into the column of light. Nothing here is a body: he is a sprite y-sorted at
  his feet, never in "enemies", with no collision layer, never a target; the gibbet, the cage, the pool, the landing
  and the punt are drawings that block nothing. On a save that already has him free the walk is the same and the
  landing pays the chest (as the Sibyl's voice still calls once she is out).
- **The primitive** (section 7): `HuntedEscort`, two halves. What the swarm sees is three statics (whether anyone is
  led, where he stands, the 250 px radius), read in `Enemy._PhysicsProcess`'s chase branch - one flag test while nobody
  is led, one distance while someone is - and a plain chaser inside it chases HIM with everything it would have chased
  the player with (the lead, the ring slot, the stand-off at its touch distance), re-aiming on the frame it switches.
  How he walks is the leader's own steps: a crumb every 20 px of the player's walk in a ring of 512, walked in order,
  stopping 64 px short of him - so he goes round the lake the player went round, and never anywhere the player's boots
  could not go. Bites are one `EnemyRegistry.ForEachNear` a bite interval with a struct visitor: no allocation per frame.
- **Who goes for him** (`Enemy.ClassHuntsEscort`, measured once per class): a body whose steering is the plain chase -
  the base `AdjustSteering`. Not one with a behaviour of its own, because that behaviour is aimed at the player: a
  caster would hold range from him and fire past him at the player, a lunger's dash and a slammer's ring are measured
  against the player, an exploder goes off on the player. And every boss overrides it (`BossEnemy` does), so **no boss
  ever retargets**. The Swamp's walkers (the Mired, Striders, Mudbacks, Broodlings) and the Ferryman go for him; its
  Lamplighters, Frog Idols, Bog Hounds, Blister Toads, Peat Cutters and Reed Pipers do not.
- **His unlock** is the site's derived Road deed, "The Ferry Reached", secret until `stage_3`, counted at the Throne
  only; locked hint "Hung in a cage over the rot of the Blighted Swamp."; quest-log prisoner "the man in the gibbet's
  cage", held line "A man is coughing in a cage somewhere over the rot." (so the Swamp's note carries it), freed line
  "{0} has crossed the water.", and his freeing reel (`freed.py`, `Freeings.WizardIds`; `prologue.HALO` gained
  `poison`, verdant as grass's).
- **His clue.** `HintCue.Vapour`, a new last row of `hint-cues.png` (every row before it unchanged, byte for byte):
  threads of pale vapour - the arcane ramp's two pale stops, never the lanterns' poison green - curling as they drift
  in slowly and low from his side, against the wind, and thinning out before the line; a few on each beat of
  `hint_flask_chime`: a glass flask tapped twice, a man coughing twice, a last faint tap (`sfx._FLASK_TAPS`,
  `_FLASK_COUGHS` = `StageHintCue.FlaskBeats`). Nearest in the whole set 0.113 (`cast_metal`). The three lines above,
  unchanged.
- **His form**, The Dose (distil, `Contagion.TheDoseId`). Every carrier has a generation, the cast's 0, and each jump is
  one more: the first jump reaches the **8** nearest, the next **4**, then **2**, then **1**, each generation dosed twice
  the last (the infecting hit and the poison both: x2, x4, x8, x16). The quota is the generation's, shared by every
  carrier of the one before it, so a plague reaches fifteen bodies past its first and the one at the end carries
  sixteen times the first dose and spreads nothing - it is where the plague ends, and it goes down in a heavy, dark
  burst. Within a jump the nearest go first (a selection, not a sort: nothing allocated). Each generation's mark is a
  grid step wider than the last, and the last carrier's is ringed twice in the hot tone. Every card still works on it in
  its generation: a fever cloud infects into the next generation of the carrier that left it, a Pandemic beat bursts
  each carrier as its own generation, Outbreak's burst is the first jump, the Plague Wind infects as the first jump does.
  No card on Contagion trades Poison away (Marsh Fever adds Water beside it), so every route leads to the form. Card:
  "Each jump of the plague reaches half as many and doubles its dose: 8, 4, 2, then 1. The last carrier dies hardest."
  Synergy line: "Distil: the last one dies hardest." Its own evolution icon. No visual scale. **A boss is dosed at
  most x4** (`Contagion.DoseBossGeneration`; 8 below).

**What moved, and why.**

1. **"A health bar scaled like the event enemies" is a fixed bar of 60, not one grown with the run's spawn health.**
   An event enemy's health is chewed by the player's damage, which grows over a run; his is chewed by the swarm's
   touches, which do not - one touch is the same at minute 3 and minute 13 (only the chapter's x1.15 is on it). What
   grows is how many reach him, so the walk gets harder through the run by itself. "Scaled like" is kept in the sense
   that matters: it is a number of touches, so it means the same wherever in the run he is walked.
2. **A bite is 46 px and every 0.35 s, not a first guess of 38 px and 0.5 s.** With eight Mired raised round him and
   nothing killing them, at 38/0.5 they took 20 s to bring him down: the swarm's own spacing (`EnemySpacingRadius`)
   spreads a ring of bodies past 38, so one or two of eight were ever biting. At 46/0.35 the same ring brought him down
   in about 8 s of biting.
3. **The Mired rise ahead of him, across his way, not round him, at 3x spawn health.** Nothing in the swarm walks half
   his pace, so a ring raised round him only ever fell behind: on the first probe runs six were risen and none came
   within 250 px. At the run's own health the player's opening loadout burned them down before they turned on him.
4. **The bog raises them where it raises them.** `SpawnEventEnemy` moves anything it cannot prove reachable to the spawn
   ring, asking the navigation window - which, measured, did not cover him a few seconds into the walk, so every riser
   went to the ring 400-1,100 px off. A riser is put back where the bog raised it unless that ground is water or a wall.
5. **He boards when the player is at the landing and he is close behind, not only when he is within 90 px of its
   foot.** On seed 7 the bank in front of the landing was the player's to find round water, and he stood at 93% of the
   way, 64 px behind a player who was standing on the landing, while the risers piled up until he fell - three times.
   The landing is also placed on ground open over its whole disc now (`ExplorePlacer.IsClearDense`, new), so its bank
   is never half water.
6. **No new material.** The doc left `\x9a-\x9e` free for one; the gown is the contract's `red` row on the bytes the
   Archmage's sash already holds.
7. The doc's four generations are read as four JUMPS (8, 4, 2, 1), the cast's carrier being the source: "the last
   carrier" is one body, as the sentence has it.

8. **A boss is dosed at most x4** (`Contagion.DoseBossGeneration`), in the infection and in a burst's hit. The last
   carrier is meant to die of its dose, and a boss does not: it stands in its adds for a whole fight, and under
   Pandemic a beat re-doses it every two seconds without anything dying, so uncapped it would hold the last carrier's
   x16 for the fight. It is still infected and still passes the plague on. The synergy line says so.

**Verified:** build; the validator (0 errors, main's 1 warning); `RegressionChecks.ValidateGibbetCage` (the numbers keep
their shape; a plain chaser hunts him and no boss or behaviour class does, over every `Enemy` type in the assembly; led
round a lake on paper he never enters it, never strays more than a crumb off the walked path, never outpaces 75%, and
ends close behind; the hint's vapour on the sound's beats; the sheets' sizes). `_ExploreProbe --stage=3`, which ESCORTS:
it holds the winch, then walks toward the landing at the player's own walking speed, round water and walls (its feet
test the ground a stride ahead), waiting whenever he is more than a few strides behind, and clears the swarm only every
5 s so the Mired and the trickle reach him: seeds 4242, 31 and 7 `--fresh-save`, 4242 `--all-unlocked` (the chest) and 1001 `--fresh-save --wayfinder-first` - 47 to 55 passed, 0 failed, both Swamp events found and won every time, the two sites 122 to 156 degrees apart; he walked 1,545 to 2,189 px behind the player (the probe turned off water 0 to 51 times on the way), 6 to 34 bodies turned on him, 3 to 17 bites, and he reached the landing with 39 to 57 of his 60. Paracelsus freed in memory and his deed recorded by his site on the
fresh saves; the hints alternated lanterns and cage, then the cage had every slot after the lanterns were won.
`--let-him-fall` (seed 31): once he was out, the player's spells stopped and eight Mired raised on him - he fell about 9 s later (48 bites),
sank, was in the cage again, was lowered again and led out again from the cage, and reached the landing. `_UiShot` of the
cage dormant, the winch (him stepping out), the walk, the punt and the clue at both canvases, and the character select
locked (his silhouette and hint once the Swamp is open) and freed at both. `_BalanceHarness --character=N --stage=3
--minutes=15 --all-unlocked --seek-apotheosis --immortal --timescale=6`, seeds 1234 and 77: the spell gate met at 6.8
and 6.9 minutes (player level 17), the form offered and taken at 9.9 and 9.1 (level 21). In the five minutes after it
the plague's carriers by generation came to 151/620/472/263/127 and 169/728/538/300/146 - the first jump reaching about
four of its eight, the last carrier reached on 84-86% of casts - and 126 and 146 last carriers were finished. Contagion
was 97-98% of each run's damage, but the seek bot pours every pick into its own spell and leaves the rest at level 1-4,
so that says nothing of a player's loadout; kills a minute are held by the spawn economy (310-330 before the form,
350-470 after), so the harness cannot see the form's worth either. Both runs ended on the boss's arrival, so the boss
cap is not measured.

**Not verified: whether escorting him is fun,** or readable through a real crowd - the probe clears the swarm every 5 s
and walks a teleported player through ground nobody has fought over - nor whether a player reads that he follows the
player's own steps (the objective says only "Lead him to the ferry landing"), nor how often a real player loses him;
the bite numbers were tuned against eight Mired raised on him, not against a run's swarm. The sound, measured (0.113
from `cast_metal`, the nearest in the whole set; above the 0.10 line, but the lowest of any chapter hint) and never
heard; the vapour at a glance mid-fight; The Dose against a boss; Sola Dosis, which no build the bot makes as him
measures; nothing on the phone. One probe run, before the board-and-placement fixes, spun without output for eight
minutes and was killed; it did not recur in the eleven runs after (29 to 42 s each), and its cause was not found.

### 4.5 Mystic Ruins - Abe no Seimei

- **Who.** The court onmyōji of Heian Kyoto (921-1005): diviner, exorcist, master of wards and paper
  talismans, Japan's most famous sorcerer.
- **Why here.** "The old wards were broken from the inside." A school of wards is his kind of house;
  the dark wizard turned a ward on its maker and sealed him inside his own five-pointed seal.
- **Spell.** **Kindled Ward** (Light), the Ruins' vault: a mote of light that drifts at the nearest
  enemy and burns it out, as his paper wards flew. Strict-rule compatible.
- **Form: The Five-Pointed Seal (banish).** *"A ward that burns an enemy below a quarter of its health
  seals it where it stands, harmless. When the ward goes out, everything it sealed is gone."* Tagline:
  *"Sealed now. Gone later."* Sealing is `Enemy.SetTargetable(false)` plus a hold; elites take a heavy
  hit instead; bosses are never sealed (the execute rule). Not an execute card: the sealed stop
  fighting at once, and the kills land together.
- **Passive: Exorcist**, +15% damage to elites.
- **Freed by: solve.** The seal is a star cut in a courtyard floor, Seimei kneeling inside. Four
  guardian stones stand at its quarters, each cut with one arrow. Stepping on a stone's plate turns it a
  quarter. All four facing the seal, it breaks. About twelve steps. Draw the stones nothing like the
  Broken Ward's notched ring.
- **Clue.** Strips of written paper blow in from his side and char at the edges as they fall. Sound
  `hint_paper_wind`. Lines: *"Strips of paper on the wind, in a hand you do not know."* / *"More paper,
  burning at the edges as it falls."* / *"Still the paper comes, from the same quarter."*
- **Alternate: Nicolas Flamel**, the Paris scrivener of the Philosopher's Stone; a book-man for the
  Archivist's school. Or Seimei moves to the Court (he served one) and Flamel takes the Ruins.

**Built (2026-11-01, `wip/wizard-seimei`).** The pilot, with P1. P2 was not needed: the Ruins had no side
event, so `SideEventDirector.ForStage("stage_4")` simply gained its line.

- **P1.** `ValidateStartingCharacters` holds the one-element and one-owner-per-element rules to the
  STARTERS only; a freed wizard may open on a hybrid (never past two elements) and share an element; no
  two characters may share an opening spell in one form. G2 is `UnlockDefinition.CountedAtFinalGateOnly`
  (`FromEvent(..., finalGateOnly: true)`): `GlobalStatsManager.CountsTowardGate` counts such a wizard at
  `StageCatalog.FinalGateChapter` (the Throne) and nowhere else, so the Emberdeep's list did not grow;
  `ValidateGateReachability` checks the flag is only on a freed character with a rescue chapter and that
  no earlier gate counts him. The ADR and `gameplay-design.md` say "never leads away".
- **Him.** `CharacterData_Seimei.tres`, roster index 9 (appended). `roster.seimei()`: the pale kariginu
  over black sashinuki, the tall black eboshi, his star on his breast, a purification wand with zigzag
  paper taller than he is, and a paper ward held up in two fingers whose tip is the kindled light - the
  one emissive thing on him, in the light ramp (new `LIGHT_RAMP`). Passive **Exorcist**
  (`elite_damage_15`): +15% to everything `IsMiniBoss` marks, bosses included (it is not an execute).
- **The event** (`FivePointedSealEvent`, `tools/art/five_pointed_seal.py`). The courtyard - a star and
  ring cut in flagstones, full of the arcane ramp's violet - with him kneeling in it a ramp step into
  shadow; four low square stones at its quarters, each with one arrow on its top; a plate outside each.
  A step ONTO a plate turns its stone a quarter clockwise; standing does nothing more until he steps off
  (52 px). **Coupled since 2026-11-01** (Eric: "Yes I like including actual puzzles in the game"): a press
  also turns the two stones beside it on the ring. Over Z4 that is the circulant (1 1 0 1), determinant
  -3, a unit mod 4, so every one of the 256 states has exactly one answer in press counts 0-3 a plate,
  and that answer is the fewest presses. The start is made from the answer - four seeded rolls say how
  often each plate must be pressed, raised to at least six, and the stones are turned back from the
  solved seal by legal presses - so every seed is six to twelve presses away. `ValidateSealPuzzle` proves
  it (all 256 states reachable, the solver equal to the breadth-first distance, 1,016 roll sets in
  range). The tell: the three stones turn and flare on one beat and the seal's light runs along its
  ring from the pressed stone to the two it moved; the objective counts "n of 4" facing, and the arrow
  points at the courtyard only while he is away from it. Nothing fails or locks: four presses of one
  plate are none. A ring of four of the chapter's enemies every 6 s while he is in the courtyard. The courtyard is
  the event's sibling and stays, broken and lit, after the win. It needs 290 px of open ground, which on
  the Ruins' lattice is only a court: `ExplorePlacer.EventSiteInBand` sweeps the whole band for one.
- **His unlock** is the site's derived Road deed, "The Seal Broken", secret until `stage_4`; the locked
  hint is "Sealed inside a star cut in the floor of the Mystic Ruins." (not "courtyard":
  `ValidateStageRewards` read "Court" in it as the Court's). Quest-log prisoner "the master of wards";
  his held line, the chapter note's clue: "Someone here was sealed in by his own wards."
- **His clue.** `HintCue.Paper` (a sixth row of `hint-cues.png`): violet strips of written paper
  tumbling in from his side, charring at both ends in their second half. `hint_paper_wind`: five gusts,
  a strip snapping in each, crackle under the last three, one thin whistle of wind through the courtyard
  (without the whistle it was 0.074 from `cast_wind`; with it, 0.125 from `ui_open`, whole set). The
  section 4.5 lines, unchanged.
- **His form**, The Five-Pointed Seal (banish): Flare then Pyre, then `KindledWard.FivePointedSealId`. A
  strike that leaves an ordinary body below a quarter seals it (`Enemy.SetTargetable(false, hideBody:
  false)` and `SetHeld`), under a small seal of the ward's light; when the ward goes out every sealed body
  is let go and finished by the ward on one beat. Elites take one 4-strike blow instead; bosses nothing;
  24 a ward at most.

**Verified:** build; the validator (0 errors, main's 4 warnings); `_ExploreProbe --stage=4` on four seeds
(won in 7 to 11 steps, him freed in memory; coupled, on six seeds, solved by the probe itself in 6 or 9
presses); `_BalanceHarness --character=9 --all-unlocked
--seek-apotheosis --immortal` (form offered and taken at 9.9 minutes, player level 23); `_UiShot` of the
prison, the broken seal, the three card states, the hint and the form, at both canvases. **Not
verified:** whether the stones are fun or the courtyard reads at a glance mid-fight; the sound, which was
measured and not heard; and the form's numbers - the bot took it at 9.9 minutes, under the 15-minute floor
`ApotheosisCatalog.MinimumPlayerLevel` was tuned for.

### 4.6 Scorched Sands - Imhotep

- **Who.** Chancellor to the pharaoh Djoser, architect of the first pyramid, physician, High Priest of
  Ra at Heliopolis; revered as a sage and healer for two thousand years after.
- **Why here.** "He hid the water." The builder who could find water and raise it is sealed in a dry
  tomb beside the road the water left.
- **Spell.** **Scorching Ray** (Fire+Arcane) since 2026-11-09: the sun focused through his bronze mirror onto
  one target, the act that frees him (Eric: "I like your imhotep suggestion, implement that as well";
  `.ai/character-fit.md`). It was **Fireball** (Fire), the Long Coil's deed, which stays in the game as the
  Sands boss's spell. Shares Fire with the Pyromancer (R1). He can no longer fuse Dark Lens, whose parent
  the ray is (`ComboSeal.OwnSpell`).
- **Form: Eye of Ra (blind)**, `scorching_ray_eye_of_ra` (`fireball_eye_of_ra` until 2026-11-09; a save
  that saw the old one still has it seen, `SaveData.MovedAscensions`). *"Each cast, the hottest ray flares
  like the sun. Whatever sees it loses you for two seconds and wanders. Hotter rays flare wider."* Tagline:
  *"What sees it forgets you."* One flare a cast where the hottest ray first lands, 120 px cold to 240 px at
  full heat (untuned); a body already blind does not see the next flare, or a held target's crowd would stay
  blind for good. A wander state through `Enemy.AdjustSteering`; bosses immune.
- **Passive: Surveyor**, +20% pickup reach.
- **Freed by: kindle.** Three signal beacons on the dunes round his tomb, about 700 px apart. Stand by
  one 1.5 s to light it; a lit beacon burns 25 s before the dry wind puts it out. All three alight at
  once and the tomb slides open. The order is the player's; the route through the horde is the puzzle.
- **Clue.** A thin stream of sand lifts into the air from his side, against the wind. Sound
  `hint_tomb_knock`, slow stone on stone under hissing sand. Lines: *"Far off in the dunes, a slow
  knocking, stone on stone."* / *"The knocking again, from under the sand."* / *"Slower now. It has not
  stopped."*

> **Its ask has changed since (2026-11-08): `.ai/unlock-puzzles.md` 3.1, "route the sun", and its Built note.** The
> beacons are gone - the kindling, the 25 s burn and its gutter, the six of the dead their light drew, `--let-beacon-die` -
> and bronze mirrors stand round the tomb instead: each faces away from where he stands, a lit one shines where it faces,
> and the sun brought mirror to mirror onto the winged disc over the door slides the slab (`SunMirrors`). The class keeps
> its name and the site its id (`tomb_beacons`); its deed is *The Sun Let In*. Everything else in this note (him, his
> passive, the tomb and its opening, his clue, his form) still stands.

**Built (2026-11-03, `wip/wizard-imhotep`).** The Sands' second event, after the caravan, through P2's API exactly as
6.3 wrote it (one line in `SideEventDirector`, a hint by site, deed words, `FromEvent`, a prisoner row). Nothing in P2
had to change.

- **Him.** `CharacterData_Imhotep.tres`, appended to `CharacterRoster`. Name "Imhotep", id `imhotep`. `roster.imhotep()`:
  the one figure HOLDING UP A SUN - his near arm raised high, the hand open and palm up under a ball of fire (Fireball,
  the only light on him), his face turned up to it - upright and still, the opposite figure to Aedan beside him, who
  strides forward in a red coat with his fire levelled like a spear. A priest's starched white kilt (linen's pale end)
  to the shin with its triangular front panel standing out, finely pleated; a bare brown chest and arms; the broad
  collar of his rank, gold beads between rows of lapis (the wool row's deep end); the leopard's pelt a High Priest of Ra
  wore, over the far shoulder to the near hip, its paw at his hip and its tail behind him; a close lapis cap; and the
  was-sceptre in his far hand, forked at the foot, the beast's head bent at the top - a staff shaped like no other in
  the cast. Drawn serious and ALIVE: nothing on him is wrapped, and he is not in ivory or the Sands' sand cloth
  (`cast_sands.py`), which is what his chapter's dead wear - a priest in their colours would read as one of them, and
  as the films' mummy. 1.06 edges/px. Sheets written by `build.py`'s wizard loop for this name only. No new material
  (`\x9a-\x9e` stay free).
- **His passive, Surveyor** (`pickup_reach_20`): every orb, flask and pickup starts coming to him a fifth further off.
  `Player.PickupReach` - (the pickup's own reach + every magnet bonus) x 1.2 - is now the one reader both `XPOrb` and
  `PickupBase` ask, so a reach bonus cannot reach one and miss the other. A share of the whole reach, so it stays a fifth
  however far magnets and the shop have grown it.
- **The event** (`TombBeaconsEvent`, `tools/art/tomb_beacons.py`, `world/tomb/`). The tomb - a mastaba front of
  sandstone courses in gold's dark end, a cornice over the door, sand drifted against both corners, a granite slab with
  his mark on it, the one cold light, where the far tell stands up from - is the site, swept for through the band on
  open ground over a 494 px disc. Three beacons on a triangle round it, **700 px a side (404 px out)**, the triangle
  turned by the site and each walked in or out along its spoke to open ground: a granite pillar on a stepped base with
  a bronze fire-basket on it. Stand within **100 px** of a cold one for **1.5 s** and it catches (kindling he steps off
  drains back at half the rate it fills); a lit one burns **25 s**, its flames sinking and guttering for the last **5**,
  a ring on the ground draining with it, and goes out on the wind - cold again, to be lit again as often as it takes.
  With all three alight at once the slab slides into the wall (0.5, 1.0, 1.5 s), he climbs up out of the stair
  (1.7-2.7 s) and is gone into the column of light. The beacons' fire is warm, the wizards' light: a column over each
  fire, sinking with it, and a faint cold glow on a cold basket so the three can be found before they burn. The arrow
  points at the tomb while he is away from it, at the nearest cold beacon once he is there (never an order), and at the
  tomb again while it opens; the objective counts "n of 3 alight" and the seconds left on the soonest to go out. Nothing
  is a target: the beacons have a trunk's footprint on the blocking layer, as the moot's stones; the tomb blocks
  nothing (a facade on the sand, like the nine mouths' rock). Both are handed to the run after - the tomb open, the
  fires burnt to embers. The slab sliding is `impact_earth`, a beacon catching `cast_fire`, one going out `cast_wind`.
- **His unlock** is the site's derived Road deed, "The Beacons Lit", secret until `stage_6`, counted at the Throne only.
  Locked hint "Sealed in a dry tomb out on the Scorched Sands."; quest-log prisoner "the builder in the dry tomb", held
  line "Something knocks, stone on stone, under the sand." His freeing reel: `freed.py` and `Freeings.WizardIds`, only
  his panel ("Imhotep walks in the sun again.").
- **His clue.** `HintCue.Sand`, a new last row of `hint-cues.png` (every row before it unchanged, byte for byte): a thin
  stream of sand lifting off the ground and rising, against the wind, its foot a ribbon breaking into grains as it
  climbs, with a fainter one beside it - sand, so in earth's pale stops, over his violet glow (violet grains would read
  as a spell). One on each knock of `hint_tomb_knock`: three knocks of dead stone on stone (a block's low modes, gone in
  a tenth of a second - not the anvil's ringing steel), the gaps lengthening, under a hiss of running sand that swells
  after each (`sfx._TOMB_KNOCKS` = `StageHintCue.TombKnocks`). Nearest in the whole set 0.176 (`spell_orbit`). The three
  lines above, unchanged.
- **His form**, Eye of Ra (blind, `Fireball.EyeOfRaId`, `Enemy.ApplyBlind`, `SunFlare`). The ball's own burst flares out
  past its reach to **1.8x** it (**380 px** at most) and every ordinary body in the flare is blinded for **2 s**: it walks
  on the way it was facing at **45%** of its pace, turning by degrees, under a sun-struck pallor and a stumbling walk, and
  begins no attack - every attacking subclass asks `IsBlinded` before a wind-up (one already begun still lands:
  `AttackTelegraph`'s promise), and a planted hold stays planted. Stacking-resistant (the longer, never the sum). Not
  fear, the dark wizard's Dread: a frightened body runs from him, which still says where he is; a blinded one has no
  idea. **Elites are struck, not blinded**, as the Quatrains strike rather than stop them: one in the flare but outside
  the blast is seared for half the hit. **Bosses are neither** (the crowd-control rule). Only the ball's own burst
  flares - not the Cluster's bomblets, not Brimstone's spit - so it comes on the spell's own beat and blindness is never
  held on a crowd for good. The flare is drawn to exactly the radius it blinds: a white heart, a ring running out from
  the blast's edge to the flare's, rays between. No card on Fireball trades Fire away (Iron Comet adds Metal beside it),
  so every route leads to the form. Its own evolution icon (the ball flaring like the sun over the eye of Ra).

**What moved, and why.**

1. **The light draws the dead** - not in the first guess. "The route through the horde is the puzzle" needs a horde
   between the fires, and the swarm round a player standing still to kindle is wherever he already was. So when a
   beacon catches, **six** of the chapter's own come in at it from **420 px** round it (not on the third: then the tomb
   opens), and the beacon just lit is the worst place to stand.
2. **Kindling drains back at half its rate** when he steps off, rather than resetting or holding: a body that shoves him
   out of the reach for a moment costs a moment, not the 1.5 s.
3. **Elites are seared and bosses untouched**, the question 4.6 left open: an elite that lost him would be a miniboss
   that stopped (the Fear rule), and "struck, not stopped" is the Quatrains' answer to the same question.
4. **The flare is capped at 380 px.** At 1.8x an Armageddon Sun's swollen blast (x1.6 area) would blind more than the
   whole height of a PC screen (about 420 px either side of him).
5. **A blinded body keeps its contact damage.** It has lost him; it is still a body, and one that blunders into him
   still hurts. What it loses is every aimed thing: no wind-up, no bolt, no fuse, no summoning.
6. **The 25 s burn is the first guess, kept.** On paper, the best order through a crowd that leaves him 60% of his walk
   takes 15 s (`ValidateTombBeacons`), and the probe, walking at his own speed with the swarm cleared, 13.2-13.5 s from
   arriving to the win; the clock bites only a player held to under a third of his walk, or one who wanders. Whether it
   has teeth through a real crowd is a playtest question (below).

**Verified:** build; the validator (0 errors, main's 1 warning); `RegressionChecks.ValidateTombBeacons` and
`ValidateEyeOfRa` (an ordinary body takes a 2 s blind, a second flare does not stack it, an elite or boss takes none; the
card's "two seconds" is the code's; the flare reaches past the blast). `_ExploreProbe --stage=6`, which KINDLES at his own
walking speed from beacon to beacon and never teleports onto one: seeds 4242, 31 and 7 `--fresh-save`, 4242
`--all-unlocked` (the chest), 1001 `--fresh-save --wayfinder-first` - 39 to 45 passed, 0 failed, both Sands events won
every time, the two sites 149 to 172 degrees apart and 14,151-15,153 px apart, the beacons' nearest pair 700 px, the tomb
won 12.2-13.5 s after arriving, twelve of the dead drawn to the light, Imhotep freed in memory and his deed recorded by
his site; the hints alternated caravan and tomb, then the tomb had every slot after the caravan was won.
`--let-beacon-die` (seeds 31 fresh and 7 `--all-unlocked`): the first beacon went out on the wind 27.8-28.0 s after
arriving and went cold, and was lit again with the other two (four kindled, eighteen of the dead drawn), won 40.5-41.4 s
after arriving. (The first seed-7 `--all-unlocked --let-beacon-die` run failed the CARAVAN's furnished-ground comparison,
"7 then 0", while three other Godot runs of this branch and other sessions' ran beside it; run again alone, 41 passed, 0
failed - the streamer had not settled, which the check's 2,000-frame wait does not cover under that load.)
`_UiShot` of the beacons cold and two of them lit, the tomb opening and him climbing out, the clue, the Eye of Ra's flare
over a stage crowd (`--spell-stage`, the bodies taking the pallor after it), and the character select - "Not yet
discovered." on a fresh save, his locked hint once the Sands open, freed - at both canvases. `_BalanceHarness --character=N --stage=6
--minutes=15 --all-unlocked --seek-apotheosis --immortal` (index 16 on this branch), two seeds:

| run | spell gate met | form taken | flares by 15:00 | blinded | elites seared | damage taken a minute, before -> after |
|---|---|---|---|---|---|---|
| seed 1234, timescale 6 | 8.9 min (level 17) | 11.9 (level 21) | 40 (13 a minute) | 2,059 (44-53 a flare) | 6 | 914, 628, 927 -> 529, 791, 773 |
| seed 77, `--timescale=1 --profile=10` | 7.2 (17) | 9.8 (21) | 61 (12 a minute) | 2,348 (about 39 a flare) | 1 | 960, 875 -> 708, 775, 759, 852, 893 |

The flare takes about two in five of the crowd round him each time (alive 120-139, near him 55-83) and holds them two
seconds of every five; the immortal bot, standing its ground, took 10-20% less a minute after it. Kills a minute fell on
seed 77 (221 before, 166-212 after): a blinded body wanders out of his auras and the reach of his other spells, which is
the price of the verb (Fear pays it too). Brimstone was the Ascension on both, so the Cluster's bomblets never flared.

**Frame cost** (seed 77, `--profile=10`, headless, the laptop the harness runs on, with seven Godot processes of other
sessions on it). In the quiet windows before the form (7.5-9.2 min: 87-115 fps) an enemy step cost 12.2-14.9 us a call
and the spells section 181-310 us a physics step; after it (14.5-15 min: 70-95 fps) 13.4-15.1 us and 246-294 us, at
0.4-0.5 KB a frame either side - the blind wander (a turn and a scale, no allocation) costs nothing measurable per body.
From 9.5 to 13 minutes every section slowed together, to 2-10 fps (an enemy step 73-80 us a call), beginning before the
form came at 9.8: the machine, as `performance.md` section 5 warns. Not measured on the phone.

**Not verified: whether kindling is fun**, nor whether the 25 s has teeth through a real crowd (the probe clears the swarm
round itself and walks unhindered), nor whether a player reads that the three must burn TOGETHER before the first one
goes out on him (the objective says "together" and counts the seconds); the sound, measured and never heard; the sand
at a glance mid-fight; whether a crowd of wandering, blinded bodies reads as blind rather than as a glitch (the pallor
and the stumble are the only tells); Surveyor, which the bot's builds never feel; nothing on the phone.

### 4.7 The Emberdeep - Wayland the Smith

- **Who.** The master smith of Germanic legend (Völundr), lamed by a king and set on an island to forge
  for him, who escaped on wings he made.
- **Why here.** The Emberdeep is the forge "where every shackle came from". Every chain that holds the
  others was struck by Wayland, chained to his own anvil. The Chainwright is his jailer.
- **Spell.** **Iron Palisade** (Metal): stakes he forged. Strict-rule compatible.
- **Form: The Tempered Line (temper).** *"Every blow the stakes take hardens the line. Struck enough,
  it rings and cuts everything along it."* Tagline: *"The more they strike, the harder."* First guess:
  +5% per contact to +100%, then one cut along the whole line and a reset.
- **Passive: Wings**, +10% move speed.
- **Freed by: bait.** He is chained to the great anvil by four chains staked in the floor. No spell
  breaks his own iron; only his forge's hammers can. On arrival the bellows wake four Trip-Hammers (the
  chapter's Slammer). Stand by a stake as one winds up, step out of the tell, and a slam that lands on
  the stake breaks its chain (`GroundSlamAttack.Contains`, the one authority on the shape). One killed
  before it strikes is replaced, four at most. He is never asked to take a hit.
- **Clue.** Sparks drift in on hot air from his side. Sound `hint_anvil`, a tired hammer on an anvil.
  Lines: *"Hammer on anvil, slow and steady, somewhere below."* / *"The hammer again. Whoever swings it
  is very tired."* / *"It does not stop. It is not allowed to."*
- **Gate.** He is freed in the gate chapter, so only the Throne counts him (either G).
- **Alternate: Ilmarinen**, the Kalevala's eternal hammerer, but a second Kalevala hero.

**Built (2026-11-02, `wip/wizard-wayland`).** No P2: the Emberdeep had no event, so `SideEventDirector.ForStage("stage_7")`
gained its line.

- **Him.** `CharacterData_Wayland.tres`, roster index 12 (appended after Merlin, who took 11 the same day). `roster.wayland()`: square to
  the front over the thing he has just struck, a long forge hammer stood upright in his near hand (its head the top of
  him on that side, taller than he is), an iron-grey shirt rolled at the sleeves, a leather apron scorched at the hem,
  a short dark beard - and his **wings**, hammered iron feathers folded on his back and rising above his shoulders,
  which no other figure has: the top of his silhouette. The lameness is the far leg, an iron brace at the knee and a
  shackle at the ankle with its chain broken off. The one emissive thing is the hammer's polished face, in the metal
  ramp (new `METAL_RAMP`, control characters `\x10-\x13`: `\x09-\x0D` are whitespace). 0.90 edges/px. Passive
  **Wings** (`move_speed_10`): +10% move speed, the one passive about his feet.
- **The event** (`GreatAnvilEvent`, `tools/art/great_anvil.py`). A round forge floor of sooted flags with the hearth's
  joints still lit; the great anvil on its iron-banded block a little south of the middle and him behind it, from the
  waist up, striking slowly (a 1.9 s blow: raised only to the shoulder, down, struck, resting) with a burst of sparks
  off each strike; four chains lying link by link from the anvil's rings to four squat iron stakes on the diagonals,
  140 px out, each in an eye let into the floor. On arrival the bellows wake **four Trip-Hammers** (the chapter's
  Slammer role, elites at 12x spawn health) 460 px out, one each side. **A slam that lands on a stake breaks its
  chain**: `SlammerEnemy.SlamLanded` (new, raised on the frame a slam resolves) and `SlamCovers`, which is
  `GroundSlamAttack.Contains` measured where the hammer stands, tested against the point of the stake nearest it
  (12 px of iron) - so the ring the player was shown is the ring that breaks the chain. **First guess changed:** a
  stock Trip-Hammer begins its wind-up at 1.5 times its ring, while the player is still coming, so standing BY a stake
  never put the stake under the ring (it plants 144 px off, the ring reaches 96). The event's hammers start only once
  the player is inside 0.85 of a 104 px ring (`HammerEngage`, `HammerSlamRadius`, `SlammerEnemy.RebuildSlam`): the tell
  then always covers him and the stake he stands by, and he has the whole 0.75 s wind-up to step out of it - which is
  the design's sentence exactly, and he is never asked to take a hit. **"One killed before it strikes is replaced, four
  at most"** is read as: four on the floor at most, and while chains remain and he is at the forge the bellows wake
  another every 3.5 s on the far side of the forge from him; walk away and none come. Nothing locks, and a strong
  loadout that melts them costs time, never the wizard. Freed, he lowers the hammer and fades into the column of
  light; the forge stays, the stakes burst and the chains in two lengths. The objective: "Bring the hammers down on the
  chains (n of 4)", with the dashed track round each stake filling as its chain goes.
- **His unlock** is the site's derived Road deed, "The Chains Struck", secret until `stage_7`, counted at the Throne only;
  the locked hint is "Chained to his own anvil in the Emberdeep." Quest-log prisoner "the smith at the anvil"; held line
  "Someone below is chained to the anvil and cannot stop forging." (2026-11-08, Eric: clearer that he cannot stop,
  that he is a slave there; it was "Someone below is still at the anvil. He may not stop.")
- **His clue.** `HintCue.Sparks`, a seventh row of `hint-cues.png`: sparks rising in on the forge's hot air,
  white-hot with a fire tail and cooling to embers in the last third of their life, a few on each strike. The far tell
  is fire's hot stop, not his cold violet: the forge is built, and built things burn (world-and-tone.md, the one
  carve-out). `hint_anvil`: four tired blows, the ring of struck steel at a free bar's modes over the hammer's thud and
  the rebound tap a smith lets it make, the gaps lengthening and each softer; 2.50 s; nearest in the whole set 0.180
  (`ui_hover`). The three lines above, unchanged.
- **His form**, The Tempered Line (temper, `IronPalisade.TemperedLineId`), as the first guess: every cut a wall lands
  adds 5% to its cuts, to +100%; full, the wall rings - one cut along its whole length and 72 px either side (or that
  far either side of an Iron Ring's circle) at the full-tempered cut - and starts again. **Per wall, not per spell**: a
  wall is what the crowd strikes, and a wall you can see heating is one you can read - its stakes go from steel to the
  metal ramp's white as it hardens, and white for a moment when it rings. Lightning Rod adds Lightning and trades
  nothing, so every route leads to the form. Card: "Every blow the stakes take hardens the line, +5% a blow to +100%.
  Struck enough, it rings and cuts all along it." Its own evolution icon (white-hot stakes, the ring either side).
- **The character select.** "Wayland the Smith" was a few pixels too wide for a PC card at Micro, and wrapped onto a
  second line cut off by the frame (`FitOneLine` measured before the card was in the tree, so it took the phone's
  floor). The name is now fitted once it is in the tree and may run 3 px into each side margin.

**Verified:** build; the validator (0 errors, main's 1 warning); `_ExploreProbe --stage=7` on seeds 4242, 31 and 7 with
`--fresh-save` (won in 8.1 to 10.7 s of game time, every chain broken by a hammer, no slam on him, Wayland freed in
memory) and on 4242 and 31 with `--all-unlocked` (the chest path); `--kill-hammers=6` (six killed before they struck,
six replacements, won in 32.6 s); `_BalanceHarness --character=11 --stage=7 --all-unlocked --seek-apotheosis --immortal` (index 11 on his branch; he is 12 since the merge)
on two seeds (form offered and taken at 12.1 and 13.0 minutes, player level 21; the line rang 51 times in the two
minutes after; kills a minute 331-337 before it, 393-409 after). `_UiShot` of the forge, the clue, the win, the form
and the character select at both canvases. **Not verified:** whether baiting a hammer onto a stake is readable in a
real crowd (the probe teleports and steps out on the frame a wind-up starts); whether players find the rule without
being told it (the objective line says "Bring the hammers down on the chains"); the sound, measured and not heard;
and the form's numbers beyond one harness pair. The Apotheosis comes late on the Emberdeep (12-13 minutes) because the
chapter's health (x3.8) slows the levels - the gate, not the form.

### 4.8 The Court (second event) - Ma Liang

**Moved from the Sketchbook, 2026-11-02** (Eric: "the sketchbook should not have any unlocks or any
achievements. move that to other maps"). The Court was chosen for him: the tale's villain is a greedy
emperor who kept the painter to paint for him, and the Court is the one chapter with a Gallery (5.4).
He is the Court's second event, so he waits on P2 and the Court's flip, with Nostradamus.

- **Who.** The boy of the Chinese tale whose brush made real whatever he painted; he painted for the
  poor, and drowned a greedy emperor in a painted sea. Drawn as a young painter, serious, not a mascot.
- **Why here.** He keeps him to paint: the Gallery's portraits (5.4) are his work, done under orders, and
  his brush paints nothing real while it is held. He drew himself a door in the Gallery's end wall and
  was never let finish it.
- **Spell.** **Familiar** (Arcane): painted creatures that come alive. The catalog gives one Apotheosis
  per form, each with its own verb, so he gets one, on the **crow** (the tale's painted birds). Taken
  as the dog or the cat, he has none; the character card should say so.
- **Form: The Painted Door (displace).** *"Where the crow strikes it paints a door, and whatever stands
  on it steps out of a second door far across the field."* Tagline: *"Move the knot, not the kill."*
- **Passive: Paint What Is Needed**, once a run, below a quarter of his health, a flask is painted at
  his feet.
- **Freed by: trace.** A door sketched in dotted charcoal on the Gallery's end wall, about 300 px tall,
  its dots numbered. Walk the line dot to dot, within 30 px; stepping off pauses, never resets; the last
  dot inks it in and he steps out. Generated with the Court's art, never in `kids_art.py` (the kids'
  chapter holds nothing now).
- **Clue.** A line of wet paint draws itself along the walls toward his room. Sound `hint_brush`. Lines:
  *"A brushstroke drawing itself along the wall."* / *"Another line, wet, running the same way."* /
  *"The paint is still running."*
- **Gate.** Counted at the Throne's gate with every chapter wizard (G2).
- The kids'-own-wizard alternate went with the Sketchbook.

**Built (2026-11-03, `wip/wizard-maliang`).** The Court's second event, through P2's API as 6.3 wrote it: the second entry
on the Court's line in `SideEventDirector` (`"dotted_door"`, after the seer's voice), a hint by site, deed words ("The
Door Inked In"), `FromEvent(..., secretUntilStage: "stage_12", finalGateOnly: true)`, a prisoner row, and his freeing
reel. Nothing in P2 changed; the one thing two building-placed events needed is below (item 8).

- **Him.** `CharacterData_MaLiang.tres`, appended to `CharacterRoster`. Name "Ma Liang", id `ma_liang`. `roster.maliang()`:
  the youngest figure in the cast and the slightest, drawn at the cast's proportion and never a child's - clean-shaven,
  the brow level, the mouth set, his black hair drawn back hard and tied in a knot at the crown with a red cord. Caught
  mid-stroke: his near arm out and up, a long bamboo brush held upright in the fingers, and lifting off its wet tip a crow
  of his light, wings up - the Familiar's crow, the tale's painted birds - in the arcane ramp, the only light on him. On his
  back a rolled painting slung on a cord, its end standing up behind his far shoulder (nobody else carries anything on
  his back); his far hand holds a small ink-stone at his hip. A short crossed jacket of steel's dark end (indigo cotton
  washed to blue-grey: a cloth gone grey, not the Archmage's blue pigment), patched; a sash of the red row knotted at the
  hip; wide trousers of stone gathered at the calf, bare ankles, straw sandals. 0.89 edges/px. No new material (steel,
  stone, red and gold rows, all already the roster's; `\xb5-\xb9` stay free). Sheets written by `build.py`'s wizard loop
  for this name only. Arcane, shared with the Archmage and the Sibyl on other spells (R1).
- **The form he opens in is chosen each run** (`Node2DGame.OpeningForm.cs`, `Player.MaLiang.cs`). A starting spell used
  to get its first form, the dog, without a word; he is the first wizard who opens on a spell with forms, so his spell
  is OWED until the run's first screen answers it - the ordinary page of forms on the ordinary level-up screen
  (`LevelUpMenu.ShowOpeningForms`, "Choose your familiar"), with no Back, the tree paused under it, ahead of every other
  screen in the queue (the Throne's Muster deals its cards to a wizard who already holds his spell). Only the crow has
  a final form, so the character card's Apotheosis line says "As the crow; the dog and the cat have none", and the
  card's spell icon is the Familiar's own, not one form's (`Player.ChoosesOpeningForm`). Every tool answers the page as
  it answers any page of forms (`LevelUpOption.AutoChoice`); `_BalanceHarness --seek-apotheosis` takes the form its
  target names.
- **His passive, Paint What Is Needed** (`paint_flask`): once a run, the first blow that leaves him below a quarter of
  his health and still standing paints a flask at his feet - four strokes of his light drawn on the ground beside him in
  0.55 s (`PaintedFlask`, stamped on the world's 2 px grid, over the bodies), and where the last closes it is the
  ordinary `HealthPickup`, 8 HP, which comes to him as any flask does. After the shield, on the HP that actually left
  (step 8 of `TakeDamage`); a blow from above a quarter to nothing paints nothing. On the healing ledger
  (`.ai/difficulty-curve.md` 5.1) as 8 HP once a run, the rarest drop's size.
- **The event** (`DottedDoorEvent`, `tools/art/painted_door.py`, `world/door/`). The door is on the Gallery's floor, 3
  cells in from its west wall and on its middle row, under the first portrait of the row (his own, below). An arch
  160 px wide and 300 px tall - jambs 220 px, a round arch of radius 80 - sketched in charcoal (the bone row's greys,
  pale on the dark oak): a dashed outline, the faint lines of its planks and its ring, and **fifteen dots** at equal steps
  along the 691 px of line, from the left jamb's foot over the arch to the right jamb's foot, each numbered outside it.
  Arriving is the Gallery and 440 px of the door (the hall behind its end wall is nearer than that, so the room is asked
  too). From dot 1 (within 30 px of it), wherever his feet stay within **30 px** of the line - measured from a little
  behind the ink to 120 px ahead of it, so the line must be followed in order and a step can never jump the arch (its
  chord sags 21 px) - his own light follows them as ink, dot after dot lighting as it passes (`DottedDoorEvent.Step`, a
  pure rule the validator traces on paper). Off the line the ink only waits where it got to, its wet head glowing where
  he must pick it up, and the line under the timer says "Back to the line - dot n". Nothing resets and nothing times
  out. The last dot inks it in: a short shake through Reduce Shake, the brush's sound, the ink running round the whole
  door; it is PAINTED - a door of red lacquer with gold studs and a ring, the only pigment in the event - and opens on his
  light in three steps (0.55-1.1 s); he steps out of the doorway (1.15-2.05 s, his own walking sheet) and is gone into
  the column of light. The door stays open after the win.
- **The pressure is the house's.** Once the trace has begun, every 7 s while it is unfinished and he is in the Gallery,
  two of the household come in at each of the Gallery's three doors - ordinary bodies of the chapter's table, not elites:
  nothing must be killed, they are what pushes him off the line.
- **The Gallery** hangs his portrait in its **first frame** (`GalleryRoom.FirstFrameWizardId`), at the west end over his
  door, as the seer's is always the last: the portraits are his work, done under orders, and the first is his own face.
  So in the Court the frames still covered at either end of the row are the two this house holds.
- **His unlock** is the site's derived Road deed, "The Door Inked In", secret until `stage_12`, counted at the Throne's
  gate only. Locked hint "Kept painting the portraits in the Court's Gallery." Quest-log prisoner "the painter in the
  gallery", held line "Along the walls a line of wet paint keeps running." (on the Court's note beside the seer's), freed
  line "Ma Liang has stepped out of his door." His freeing reel: `freed.py` WIZARDS and `Freeings.WizardIds`, his panel only.
- **His clue.** `HintCue.Paint`, a new last row of `hint-cues.png` (rows 0-11 byte-identical): dabs of wet paint - round,
  an oval, a tapered flick, a small one with a speck - and a drop running off the end of a stroke into a teardrop. On each
  of the three strokes of `hint_brush` a line of six dabs is laid one after another from deeper in toward the edge he is
  beyond - the line runs toward his room - three lines side by side, and a drop hangs off the last on the fourth beat;
  dabs are round, so a line of them is a stroke whichever way the edge runs, and a drop always runs down the screen.
  `hint_brush`: three strokes of a loaded brush on plaster (a soft wet press, the drag of the bristles in a band rising as
  the load runs out, a smear of wet under it, lifted off) and a drop running off the last, twice; 2.40 s; nearest in the
  whole set 0.131 (`spell_beam_loop`), the nearest hint 0.273 (`sfx._BRUSH_STROKES`, `_BRUSH_DRIP` =
  `StageHintCue.BrushBeats`). His far tell is the arcane ramp's hot stop, a stop paler than the seer's violet in the same
  house. The doc's three lines, unchanged.
- **His form**, The Painted Door (displace, `Familiar.PaintedDoorId`, `Familiar.PaintedDoor.cs`), on the crow only
  (`ApotheosisDefinition.VariantId`). Where the crow's peck lands - the stoop's strike, or the griffin's swoop at the
  first body it strikes - a door is painted on the ground in its light (two strokes, shut, ajar, open:
  `effects/painted-door.png`), and stands open 1.2 s. Whatever ORDINARY body stands within **54 px** of its foot (times
  the spell's area) while it is open is taken through it, **8 a door at most**, and steps out of a second door painted
  **820 px** from him across the field - on the far side of him from the knot, turned off that line by up to 63 degrees,
  on ground a body can stand on, never nearer him than 600 - spread round it, at a standstill. A body moved is not moved
  again for 4 s. The crow paints at most one door every **0.9 s** and keeps at most **3** open. It deals nothing of its
  own; elites, minibosses and bosses are never moved (counted, ignored). Card: "Where the crow strikes it paints a door,
  and whatever stands on it steps out of a second door far across the field." Synergy line: "Displace: move the knot,
  not the kill. Elites and bosses never go through a door." Its own evolution icon (the crow small over two doors, a
  skull going into one and out of the other, on the Familiar's moon). No card on the crow trades Arcane away.

**What moved, and why.**

1. **He chooses his form on the run's first screen.** "Taken as the dog or the cat, he has none" needed a choice that did
   not exist: a starting spell got its first form (the dog) silently, and the code's comments assumed a wizard opened in
   his Apotheosis's form. A fixed crow would have made the sentence false and the dog and the cat unreachable for him, so
   the choice the forms ADR puts "on the level-up screen, in the run" is made there, first.
2. **The door is drawn on the floor in front of the end wall, standing up the screen.** In a top-down house an end wall
   is only its top: nothing can be walked on it. The drawing lies on the boards where a door in that wall would be walked
   through, upright as every door in this projection stands up its wall.
3. **The line is measured near the ink, not anywhere on it.** "Within 30 px" alone would let a player stand by dot 15
   and finish; the window (30 px behind the ink, 120 ahead) makes "dot to dot" an order, and the validator shows a walk
   straight across from dot 1 to dot 15 inks nothing.
4. **The household comes in at the Gallery's doors.** The design named no pressure, and the probe walks the whole line in
   3-4 s; the house is what makes stepping off a thing that happens.
5. **A crow on his brush, not a stroke.** A crescent of light beside the shaft read as a sickle; an S-stroke as a question
   mark. A bird reads as a bird at 48 px, and it is the spell.
6. **His portrait is the first frame** - "if it fits cleanly": it does, beside the seer's last-frame rule, and costs one
   constant.
7. **His far tell is the arcane hot stop**, so the house's two events are not one violet.
8. **Two events a building places are exempt from the 90 degrees.** The band's rule (each later event 90 degrees round the
   start from the first) is about rolls; the seer's rooms and the Gallery are where the house has them. The validator
   (`CheckSideEventSeparation`) and the probe skip a pair both of whose events are `PlacedByChapterRules`.
9. **The brush's strokes shortened.** A first draft (0.62, 0.48, 0.44 s strokes, one drop) was 0.111 from
   `spell_beam_loop`, a sustained beam; shorter strokes and a second drop put it at 0.131.
10. **The Gallery's frames will run out at the merge.** Eleven wizards he took hang in its twelve frames on this branch;
    Imhotep and Medea make thirteen, and `ValidateSeersVoice` will say the Gallery must hang more (`court_layout.py`).

**Verified:** build (each of the three code commits on its own too); the validator (0 errors, main's 1 warning);
`RegressionChecks.ValidateDottedDoor` (the line on the Gallery's open floor with the tolerance round it, and nothing of
the house on the drawing; on paper, walked at his pace it inks all fifteen dots, stepped off half way it waits and picks
up where it was, walked straight across from dot 1 to dot 15 it inks nothing, begun at the last dot it never begins;
the look-ahead's chord inside the tolerance; the sheets' sizes and the generator's geometry; the hint on the sound's
beats; his portrait the first frame and no wizard framed twice; his opening page and his Apotheosis on the crow; a door
takes an ordinary body and never an elite or a boss). `_ExploreProbe --stage=12`, which TRACES at his own walking speed,
feet first, never teleported along the line: seeds 4242 and 1001 `--fresh-save`, 7 `--all-unlocked` (the chest), and
`--step-off` on 31 and 2026 fresh and 77 `--all-unlocked` - 42 to 50 passed, 0 failed, both Court events found and won
every time (the seer first, by seeking, then the door); the door begun 0.4-1.1 s and inked 3.8-4.5 s after arriving
(7.4-9.6 s with the step-off, the ink held at 278-284 px through 3.9-4.5 s off the line and nothing reset); six of the
household came in each time; Ma Liang freed in memory and his deed recorded by its site. `_UiShot` of the door half
traced (`--door-trace=0.45 --hold`), its ending (`--door-open=1.7`), the clue (`--hint-preview=dotted_door`, from the east
and the west), the opening page, the flask being painted (`--paint-flask`), the freed panel and the character select
(locked, his silhouette and hint once the Court is open; freed) at both canvases: `_shots/wizard-maliang/`.

**The form, measured** (`_BalanceHarness --character=N --stage=0 --minutes=15 --all-unlocked --seek-apotheosis
--immortal`, N his roster index; the bot takes the crow on the opening page):

| run | spell ascended | form taken | doors / bodies moved a minute after | damage taken a minute, before -> after | kills a minute, before / after |
|---|---|---|---|---|---|
| seed 1234, `--timescale=1 --profile=10`, alone | 5.9 min (level 17) | **7.3** (21) | 41-54 / 37-144 (698 moved by 15:00, 39 strong ones ignored) | 465-599 -> 188, then 13-95 | 429-446 / 342-494 |
| seed 77, timescale 6 | 7.7 (17) | 10.4 (21) | 25-39 / 105-209 | 523-669 -> 154-306 | 205-233 / 248-341 |
| seed 1234, timescale 6, beside the screenshot runs | 9.6 (17) | 12.7 (21) | - | - | - |

The last row is not a measurement: beside four windowed screenshot runs the harness ran at 5-7 fps, and at timescale 6 a
frame was more than a second of the crow's climb-stoop-peck cycle, so it pecked a few times a minute (5 doors in two
minutes). At real time, alone, the crow paints about fifty doors a minute and moves a hundred-odd bodies while the crowd
round him is thick (`near` 68-81 before the form, 34-51 after it), and fewer as it thins. The form comes at 7.3 minutes
on seed 1234, a hair under the 7.5-minute floor `ApotheosisCatalog.MinimumPlayerLevel` was tuned to: the crow's kills
level him fast, and level 21 is the gate that sets the time. On the Court itself the bot never meets the gate (the
chapter's health, as for Nostradamus).

**Frame cost** (`--timescale=1 --profile=10`, headless, alone): `FrameProfiler` gained a `familiar` section (the whole
node's process and the doors' draw), which it had never had. It cost 29-47 us a frame (mean 36) in the 90 s before the
form, 32-67 (mean 48) in the 90 s after, and 24-98 (mean 36-39) for the rest of the run; whole frames 7.2-10.9 ms before
and 7.2-11.7 after, allocation 8.2-11.2 KB a frame before and 6.9-11.0 after (the harness's own). The doors are a fixed
array and the candidates the registry's shared buffer. Not measured on the phone.

**Not verified: whether tracing is a real ask.** The probe walks the whole line in 3-4 s and clears the swarm round
itself every frame; the household's six come once in that time. Whether a player under the Court's crowd, on a phone's
floating stick, finds the line hard to hold within 30 px - or too easy to be worth a wizard - needs a hand, and if it is
too slight the levers are the household (more, and from the first dot) or a dot that must be stood on, not passed. Nor
verified: whether a player finds the Gallery from the paint at the screen's edge without the wayfinder; whether the
numbers on the dots read on a phone (3x5 digits at x2 are small there); the sound, measured and never heard; the
Painted Door in a real crowd (whether a player reads that the knot went through and came out across the field, and
whether a second door off a phone's narrow canvas is ever seen); its exit on the Last Bridge, where ground a body can
stand on next to the drop has not been driven; Paint What Is Needed in a run (photographed through `--paint-flask`, never
met by the bot, which cannot be hurt); nothing on the phone.

### 4.9 The Last Bridge - Morgan le Fay

- **Who.** The enchantress of Arthurian legend: healer of Avalon, shape-changer, the queen in the barge
  who carried the dying king across the water. The mirage over the straits bears her name.
- **Why here.** "His seat is across the water." She alone could carry anyone over without the bridge,
  so her barge is moored under its last arch.
- **Spell.** **Sleet Runes** (Ice+Wind): cold sleet off the strait, cast the way she faces. Pure
  alternative under R1: **Stormsweep** (the storm on the water); the form works for either facing spell.
- **Form: Fata Morgana (double).** *"A mirage of you walks beside you facing the other way and casts
  behind you. Enemies near it chase it."* Tagline: *"Half of them chase a ghost."*
- **Passive: Glamour** since 2026-11-09: 1 in 10 blows strike a mirage of her instead (it was **Avalon**, the first
  fall of a run healing to full, which waited on a revive she rarely held; `.ai/character-fit.md`).
- **Freed by: keep pace.** The barge drifts under the span toward the gate, its one lantern seen through
  gaps in the deck. Stay within 260 px of it for its drift (about 1,200 px, 50 s: slower than him, ahead
  of the collapse). Fall behind or run ahead and the lantern dims; out of reach 6 s and it drifts back to
  its mooring. Placed by the bridge's band rules (`ChapterRules`), not the explore bands.
- **Clue.** Mist rolls over the parapet from her side, and a shimmering barge hangs above the water
  there. Sound `hint_hull_knock`. Lines: *"A boat on the water below, where no boat could be."* / *"The
  boat again, nearer the bridge."* / *"Its lantern is still lit."*

**Built (2026-11-03, `wip/wizard-morgan`).** The design above, with the changes listed under "What moved".

> **Its ask has changed since (2026-11-07): `.ai/unlock-puzzles.md` 3.7, "light the lamps ahead", and its Built note.**
> Keeping pace is gone - the reach she must be kept within, the 6 s grace, the drift back to the mooring and the two
> dashed lines across the deck - and a row of dead lamps on the parapet on her side is the ask instead: she drifts
> only as far as they are lit, and sets out from a lamp only while it burns. Her deed keeps its id (`the_barge`) and is
> called *The Way Lit* now. "Out of reach 6 s and it drifts back" (What moved, 2) and its dashed lines are history;
> everything else in this note - her, Avalon, the barge, its placement and track, the slipped mooring (now: she is
> borne on ahead of the fall), her clue, her form - still stands.

- **Her.** `CharacterData_Morgan.tres`, appended to `CharacterRoster`. Name "Morgan le Fay", id `morgan`.
  `roster.morgan()`: tall and upright, a queen standing INTO the wind off the strait - her near arm raised level
  with the hand open and the spell's own rune (the diamond sigil of `tools/art/sleet_runes.py`) cut in the air
  before the palm, the one thing on her that gives off light, in ice's ramp; everything loose on her streamed back
  the other way - long slate hair (the stone row) and a mantle in the contract's lichen row, its tail torn into
  three tongues - so she is widest on the side she is not looking at. A gown of violet's deep end (the black samite
  of the barge), an old-gold girdle and a thin circlet. Lichen is new to the roster's palette, on `\x95-\x99`
  (`roster.LICHEN_RAMP`, in `anim_sets.PLAYER_RAMPS`). 0.92 edges/px. Her rune is not a staff head: nothing holds
  it, which keeps her apart from the Frostweaver's trident (a first draft's raised-arm stave read as one).
  Sheets written by `build_roster`'s wizard loop for her name only; her freeing reel is `freed.py`'s row and
  `Freeings.WizardIds`.
- **Replaced 2026-11-09 by Glamour** (`glamour_evasion_10`, `Player.Mirage.cs`: 10% added to the dodge roll in
  `TakeDamage` step 2; Eric: "glamour", over making Avalon her own revive; `.ai/character-fit.md`). What follows is
  the record of Avalon as it was. **Avalon** (`first_fall_full`): the first extra life a run spends heals to full, not half (`Player.ExtraLifeHeal`,
  read in `TakeDamage`'s step 10). After the Archmage's Overlooked in the pipeline, so a blow Overlooked turns
  aside never spends a life - but a character has one passive, so the two never meet in a run. On the healing
  ledger as two lines: "an extra life" (every revive's half, which the ledger never counted) and "Avalon" (the
  other half, once). `.ai/difficulty-curve.md` 5.1 has its row: +25 HP once a run at 50 max HP, and **nothing at
  all without a revive to spend** - Phoenix Oath from the shop or the Familiar's Nine Lives. By the castle most
  saves own a level of Phoenix Oath; whether a passive that waits on the shop is fair is Eric's call.
- **The event** (`BargeEvent`, `tools/art/barge.py`, `world/barge/`). The black barge drawn lying along the
  bridge's diagonal - its bow up and right, on the one angle a pixel grid draws without a stair - a long low hull of
  black wood with an old-gold line under each rail, a pall of black samite heaped amidships, a plain stern post
  and a tall prow post curled forward with the one lantern hung from the curl, in wind's ramp (the strait's mist,
  its far-tell colour). She stands toward the stern in it (her own sprite, the rune taken from her hand, so she
  reaches out over the bow toward his castle). 88 cell, four frames (the lantern breathing, ripples under the
  hull, the swell). It rides on the water hard under the shore on its side, sorted at its waterline - in front of
  the south-east parapet, behind the north-west one - and through a gatehouse it passes outside the towers.
  Dormant, its far tell is the usual light plus its own image hung upside down above the water, shimmering (the
  fata morgana, on the light layer). Running: its lantern, a pool of its light thrown across the deck level with
  it, two dashed lines across the deck at the edges of its reach (bright while he is between them), and a ring
  round the lantern that fills toward the landing. Freed, she fades into the column and the barge thins to mist.
- **The rules.** Come within `ArrivalAlong` (400 px) of it along the bridge, and 760 px of it, and it begins;
  come LEVEL with it - within `Reach` (260 px) along the bridge, **anywhere across the deck** - and it casts off,
  drifting 2,000 px at 40 px a second. Out of reach 6 s at a stretch and it drifts back (220 px a second) to its
  mooring and waits to be met again; at its landing it waits for him to be level, and she comes ashore. While it
  drifts, two of the gate's dogs (the chapter's Fast role, ordinary bodies) come down the deck at its level from
  1,050 px ahead every 7 s. Objective: "Come level with the barge" / "Keep level with the barge to its landing
  (n s)" / "The lantern is going out - keep level with the barge" / "The barge drifts back to its mooring".
- **Her unlock** is the site's derived Road deed, "Kept Pace", secret until `stage_11`, counted at the Throne only
  (G2); locked hint "Moored on the dark water under the Last Bridge."; quest-log prisoner "the woman in the barge",
  held line "A lantern on the water under the bridge, where no boat could be.", freed line "{0} has come ashore."
- **Her clue.** `HintCue.Mist`, the next row of `hint-cues.png` (every row before it byte for byte the same): banks
  of sea mist rolling in low (four shapes), and with the first knock the barge hung shimmering above the water
  among them (four frames), in wind's ramp, on the knocks of `hint_hull_knock` (`sfx._HULL_KNOCKS` =
  `StageHintCue.HullKnocks`: 0.06, 0.98, 1.62 s). The sound: a wooden hull knocking on the stone of a pier far
  below, a boomy box, water slapping between, one creak of rope, an echo off the arch; 2.40 s at -10 dB, nearest in
  the whole set `impact_darkness` at 0.224, the nearest hint `hint_grove_whisper` at 0.327 (a first draft was 0.142
  from `impact_water`, its second box mode in that sound's bands; moved from 352 to 262 Hz). The three lines above,
  unchanged.
- **Her form**, Fata Morgana (double, `SleetRunes.FataMorganaId`, `FataMorganaMirage`, `Player.Mirage.cs`). A
  **second casting origin**: a mirage of the player - his own sprite on the frame he is on, turned the other way,
  drawn as light (additive, overbright, in ice's pale cyan), shimmering in bands - walks 150 px beside him, square
  to his facing, and every Sleet Runes cast is let go twice: his stream from his hand along his facing, and the
  same runes at the same strength from its hand along its own, behind him (Turning Gale brings its runes back to
  it). **A lure**: every 0.15 s whatever is within 190 px of it, and nearer it than him, chases it for 0.6 s
  (`Enemy.ApplyLure`: the newest point, the longest time, never added) - a planted wind-up stays planted; a boss is
  never lured. Card: "A mirage of you walks beside you facing the other way and casts behind you. Enemies near it
  chase it." Its own evolution icon (the rune and its mirrored mirage back to back, a shimmer through the copy).
  Great Rune adds Ice and Turning Gale Wind; nothing on Sleet Runes trades Ice away.

**What moved, and why.**

1. **The drift outruns the fall: 2,000 px in 50 s, not 1,200.** The first guess was 24 px a second and the span
   falls at 31.3 (`BridgeRules.FrontSpeed`): keeping pace walked him into the drop from any arrival with the fall a
   few hundred pixels behind him, so whether it could be won depended on when he got there - the pace clock, which
   no event may be timed by. At 40 px a second every second of the drift puts 9 px between him and the fall, so from
   any arrival he is ahead of, keeping pace keeps him ahead. Still slower than his walk (220). `ValidateBarge`
   holds it over the fall's speed by a tenth and under half his walk.
2. **Reach is measured ALONG the bridge, anywhere across the deck.** Section 3's word is "level with", and "fall
   behind or run ahead" are along-the-bridge words; a 260 px circle round a barge in the water would have pinned him
   to within 200 px of the parapet, under a horde that comes down the deck from ahead.
3. **"Seen through gaps in the deck" is its light thrown across the deck.** The barge cannot be under the deck
   and seen; it rides on the water under the span's flank, and what reaches the deck is its lantern's pool and the
   two lines of its reach.
4. **A mooring the fall has taken is slipped.** The barge is on the water, but the deck beside a fallen mooring is
   gone: it waits `RestLead` (560 px) ahead of the fall instead, and its arrow and hints go with it
   (`PointOfInterest.DriftTo`, the one placed thing that floats). A failed drift drifts back to the same place.
5. **Placed by the bridge's band**: under its second or third span, on one side, along it so the whole drift fits
   (three rolls from the event's own stream). `ExplorePlacer.PlacedByChapter` records its bearing;
   `SideEvent.PlacedByChapterRules` takes it out of the explore-band checks, and `ValidateBarge` holds it to its own.
6. **The wayfinder** goes on the deck 1.5 to 2.5 chunks ahead of the start (`ChapterRules` hook 10,
   `WayfinderSite`): on a band the usual bearing is mostly water, and the search could settle on the bridgehead that
   falls in the first minute and a half. Nothing on a band can be kept off the event's bearing; it is on the way, as
   everything on the bridge is.
7. **It arrives at 400 px along, not the usual radius.** A player at the far parapet passes the barge 950 px
   across; the arrival is measured along the bridge too, and at 400 (a first build's 480) the barge is on a phone's
   narrow canvas when its line comes up.
8. **The bridge's strip under the timer steps aside while the event speaks** (`SideEvent.LineInUse`): they share
   the slot, and hook 7's comment said no castle chapter had an event.
9. **The mirage keeps its side.** A first build stood it on his right hand as he faced, and every reversal of a
   kite sent it through him to the other side; it now stays on whichever side it is, and a quarter turn walks it a
   quarter of the way round him. And it is drawn as light: multiplied, a wizard in deep cloth came out as a dark
   smudge nobody could find.

**Verified:** build; the validator (0 errors, 1 warning - the Test Wizard's missing passive, which main has);
`ValidateBarge` (64 seeds: moored under both spans and on both sides, its drift inside its span and on the water all
the way, a slipped mooring with room for a whole drift, the hint's beats inside the sound). `_ExploreProbe
--stage=11`, which KEEPS PACE (level with it, 90 px behind it on its side of the deck, every frame): seventeen runs
on twelve seeds, all won (the first twelve before arrival moved from 480 to 400 px and the light was brightened,
seeds 4242, 7, 2, 55 and 99 again on the final build) - seeds 4242, 31, 1, 2, 3, 99, 500 and 2026 on a fresh save
(Morgan freed in memory) and 7 `--all-unlocked` (a chest), the barge under span 2 on four seeds and span 3 on eight,
on both sides, every one landing 49.4-49.9 s after arriving; `--fall-behind=4` on 1001 and 2 (out of
reach 4.0 s, forgiven, one drift); `--fall-behind=9` on 55 (the drift failed at 45%, drifted back, won from the
mooring 76 s after arriving); `--arrive-late --bridge-front=-6000` on 8 and 99 (the fall 200 px past the mooring,
the barge waiting 560 ahead of it with its arrow, won, he never nearer the fall than 345 px). Each probe run also
checks the wayfinder on the deck ahead of the start and taking it marks the barge. Two probe faults found and fixed
on the way: `AutoPickLevelUp` held the first level-up's freed screen and left the second open for ever (the tree
paused), and a fall-behind that only stood still never left its 260 px reach. `_UiShot` of the dormant barge (its
mirage over the water), keeping pace, the win, the clue, the form and the character select (freed, and her
silhouette with her hint) at both canvases: `_shots/wizard-morgan/`. `_BalanceHarness` reached the form: see "The
form, measured" below.

**The form, measured** (`_BalanceHarness` as Morgan, `--minutes=15 --all-unlocked --seek-apotheosis --immortal
--aim-facing`, seed 1234). `--aim-facing` is needed: a facing spell in the hands of a bot that stands still fires
wherever its last step pointed, and without it the bot killed about one enemy a minute.

| run | her spell ascended | form taken | after the form | kills a minute, before / after |
|---|---|---|---|---|
| the Forest, timescale 6 | 7.2 min (level 17) | 9.7 min | - | 278-365 / 265-371 (another harness beside it) |
| the Forest, timescale 6, alone | 7.2 (17) | **9.2** (level 21) | the mirage let go 850-900 runes and laid 560-650 lures a minute | 316-324 / 368-455 |
| the Forest, `--timescale=1 --profile=10` | - | **9.1** | about 910 runes and 720 lures a minute | 359-378 / 429-470 |
| the Emberdeep, timescale 6 | never: level 16 at 15:00, under the Ascension's 17 | - | - | - |
| the Last Bridge, timescale 6 | never: level 8 at 15:00 | - | - | - |

On the Forest the form comes at 9.2-9.7 minutes, past the 7.5 floor, the level-21 gate setting the time as it does
for everyone, and Sleet Runes did three quarters of the damage. On her own chapter the harness cannot measure it:
`--immortal` holds its ground, and on the bridge the ground it holds falls in at 1:21 - it stood in the drop for
fourteen minutes, where everything that reaches it falls too. The bridge's own measurements (`.ai/endgame-castle.md`
3.3) used `--survivor`, which walks. On the Emberdeep (x3.8 health, the bridge's) the standing bot never reached
the Ascension's level; a player who walks the bridge's crowd gathers far more than a bot that stands (3.3: 97-100%
of what drops).

**Frame cost** (`--timescale=1 --profile=10`, headless, alone on the laptop): the runes' section (`FrameProfiler`'s
`runes`, where both streams fly) went from 89 us a frame over the minute and a half before the form to 116 after it,
the spells' from 54 to 91 and the player's from 91 to 127, with the loadout also grown meanwhile; whole frames held
at 7.9-8.0 ms either side and allocation at 3.9-4.6 KB a frame. The mirage's lure (a grid read every 0.15 s) is in no
section of its own. Two timescale-6 runs beside other Godot runs read frames of 100-300 ms in the same minutes,
which is the machine (performance.md section 5), not the form. Not measured on the phone.

**Not verified:** whether keeping pace is a decision under a real horde or a formality (the probe stands level with
it every frame; the bridge's crowd comes from ahead, and its dogs at its level, but nobody has walked it); **on a
phone the barge is off the right of a 540-wide canvas once he trails it by more than about 150 px** - the lines
across the deck and its light carry the reach there, and whether that is enough needs a phone in a hand; the sound,
measured and not heard; whether a mirage of the player reads as his and as false at a glance in a crowd; and Avalon,
which no run here spent a revive on.

### 4.10 The Court - Nostradamus

- **Who.** Michel de Nostredame (1503-1566), physician and seer, astrologer to Catherine de' Medici's
  court, author of the Prophecies.
- **Why here.** A court needs a seer. He keeps him in the empty house to read what is coming, and what
  is coming is the player.
- **Spell.** **Starfall** (Light). Shares Light with Seimei (R1). See question 6.
- **Form: The Quatrains (foresee).** *"A star falls on any enemy that begins a wind-up or draws a
  bolt, before it can finish."* Tagline: *"It falls before they strike."* Needs a cancel on
  `AttackTelegraph` (restart its cooldown; it may never skip a wind-up). Bosses are struck, not
  interrupted.
- **Passive: Foresight**, enemy wind-ups show 15% longer.
- **Freed by: seek.** His voice reads from one of the Court's rooms (`endgame-castle.md` 5.4), chosen by
  the seed. Reach the room and he is gone: an open book, a candle still lit. The next reading comes from
  another room. The third time he is at his desk. The house's walls bend the route the sound promises.
- **Clue.** He speaks, in lines written for the game in the shape of his quatrains (never his own).
  Sound `hint_quatrain`. Lines: *"'Out of the west one light, on foot, alone.'"* / *"'The one he missed
  walks every road he made.'"* / *"'When the last door opens, the house goes dark.'"* The Gallery's
  portraits (5.4) uncover each wizard freed; his is the last covered frame.
- **Alternate: Michael Scot**, court astrologer to Frederick II, the wizard Dante put in hell.

**Built (2026-11-03, `wip/wizard-nostradamus`).** The Court's first event, through P2's API as 6.3 wrote it: one line in
`SideEventDirector` (`"stage_12"`, the Compass), a hint by site, deed words ("The Last Reading"), `FromEvent(...,
secretUntilStage: "stage_12", finalGateOnly: true)`, a prisoner row. Ma Liang's dotted door will be the second entry on
the same line (4.8); the Gallery is kept out of the seer's rooms for it.

- **Him.** `CharacterData_Nostradamus.tres`, appended to `CharacterRoster`. Name "Nostradamus", id `nostradamus`.
  `roster.nostradamus()`: upright and looking up, the square four-cornered doctor's cap of his portraits (its corners
  out sideways - pointed up they read as horns), a sable tippet over a long gown of the contract's lichen row (a new
  `LICHEN_RAMP`, since the merge the same lichen row and bytes as Morgan le Fay's mantle, `\x95-\x99`), an iron-grey forked beard, a written roll hanging from his
  near hand nearly to the knee, and his far arm raised, the sleeve fallen back, holding a brass armillary sphere by its
  stem - its rings open so the ground shows through, and the star at its centre, in the light ramp, the only light on
  him. Against John Dee, the other bearded scholar: no staff, no ruff, standing straight, an open sphere held up rather
  than a disc on a pole, a flat-topped head. 0.92 edges/px. Sheets written by `build.py`'s wizard loop for this name
  only. Starfall, Light; he shares Light with Seimei and Merlin on different spells (R1).
- **His passive, Foresight** (`windup_15`): every ordinary enemy's wind-up shows 15% longer -
  `AttackTelegraph.ForesightScale`, read on the frame a wind-up starts, put back to 1 by every `Player._Ready`. It reaches
  only clocks marked `Foreseeable`: the caster's bolt, the lunger's crouch, the slammer's ring (so the Rime Guard's cone
  and the anvil's hammers too), the exploder's fuse, the summoner's call. Not a boss's.
- **The event** (`SeersVoiceEvent`, `tools/art/seers_voice.py`, `world/seer/`). Seven rooms may hold a reading: the Great
  Hall, the Kitchens, the Chapel, the Library, the Kennels, the Map Room and the Well Court. The seed's choice
  (`ChooseReadings`, every roll drawn before anything is tested): a reading place in each room, then a shuffle of the
  rooms, then the first ordered three whose first place is at least **3000 px** from the stair and each next at least
  **2400 px** from the last. A reading place stands at least **4 cells** in from its room's walls, **130 px** off its
  furniture, and where nothing walkable but the room and its doorways comes within the **220 px** reach
  (`ReachedOnlyFromInside`) - so it is found only by coming into the room. The first is the event's one point of
  interest: dormant, it has the far tell any event has (his violet) and the wayfinder's arrow. Reach it and he is gone -
  a lectern, a heavy book lying open, a candle still lit (warm: the only lit candle in a house of cold ones) - and **4 s**
  later the voice reads from the second room, then every **8 s** until that room's place is reached; gone again; then
  the third, where he stands at his tall writing desk, the armillary sphere raised; reach it and he is free, into the
  column of light. The next place is laid when the voice moves to it, in a room far off screen. Nothing fails, nothing
  times out, no event enemy is spawned: the household is the pressure.
- **Heard, never pointed at.** Every reading is `hint_quatrain` played **300 px toward the room** from where he stands (as
  the chapter hints are, so a far room is as loud as a near one - a sound at the room itself would be lost past a
  screen or two), the stars of his cue drifting in from that edge (`StageHintCue.Play`), and the line he reads set
  beside them (`StageHintCaption`, waiting its turn on the screen). Two lines a leg, written for the game in the shape
  of his quatrains: "He comes too late, and finds the candle lit." / "A book left open; the reader has gone on.", then
  "Twice to an empty chair. The third, the seer." / "The third room waits, and one who waits in it." No arrow points at
  the second or third room, and they are not points of interest. With the sound off the stars and the line still say
  which side.
- **The primitive** (section 7, "a wind-up cancel") is the form's, below; the event needed none - it is the Court's own
  building (`CourtLayout`), its rooms and its doors.
- **His unlock** is the site's derived Road deed, "The Last Reading", secret until `stage_12`, counted at the Throne's
  gate only. Locked hint "Heard reading aloud, room to room, in the Court." Quest-log prisoner "the reader in the house",
  held line "Somewhere in the house a voice is reading aloud." (on the Court's note), freed line "Nostradamus has closed
  his book." His freeing reel: `freed.py` WIZARDS and `Freeings.WizardIds`, his panel only.
- **His clue.** `HintCue.Stars`, a new last row of `hint-cues.png` (rows 0-8 byte-identical): four-pointed stars
  twinkling in, a near one and a far one on each line of the verse, in the arcane ramp's violet. `hint_quatrain`: an old
  man reading four lines of four syllables, two couplets with a breath between, the fourth lower with its last syllable
  held, in a dry room (`sfx._QUATRAIN_LINES` = `StageHintCue.QuatrainLines`, 0.06 / 0.78 / 1.74 / 2.46 s; 3.30 s; nearest
  in the whole set 0.149, `hint_ice_song`; the Sibyl's three and Tituba's cry all 0.21 or more). The doc's three lines,
  unchanged.
- **The Gallery.** His portrait is always the **last frame** of the row (`GalleryRoom.LastFrameWizardId`,
  `WizardInFrame`): every frame is uncovered as its wizard is freed, so on a road walked in order the frame still covered
  at the end is his, in the house where he is. The others hang in catalog order from the first.
- **His form**, The Quatrains (foresee, `Starfall.QuatrainsId`). Whenever an enemy within **560 px** begins a wind-up -
  `Enemy.WindUpBegan`, the rising edge of the same reading the icon over its head pops in on - a star of the rain falls
  on it, **0.3 s**, following it down, and lands before the shortest ordinary wind-up there is (0.7 s, a lunger's) can
  finish. It strikes as a star of the rain does and stops the attack: `Enemy.TryInterruptWindUp` -> the class's
  `CancelWindUp` -> `AttackTelegraph.Cancel`, which ends the wind-up UNLANDED and starts its whole cooldown over. The
  icon over the head is cleared rather than flashed (a strike that never came must not be shown). An exploder's fuse is
  put out and its heat goes; one still in reach lights a whole new fuse. Elites and bosses are struck, never stopped. It
  is a star like any other: Skipping Stars skips it, a Constellation kindles it; the Starburst does not make it heavy (a
  fuse would land too late). No card on Starfall trades Light away (Frostfall adds Ice beside it). Card: "A star falls on
  any enemy that begins a wind-up or draws a bolt, before it can finish. What it stops must wait its cooldown again."
  Its own evolution icon (a swarm skull's wind-up icon struck by a falling star).

**What moved, and why.**

1. **Elites are struck, never stopped - not only bosses.** An elite's wind-up is often an event's: the anvil's hammers
   must land on the stakes to break Wayland's chains (4.7), and a form that stopped them would lock his event for the
   one wizard who plays a Light rain. So `TryInterruptWindUp` refuses every `IsMiniBoss` (elites, minibosses, bosses).
2. **Foresight reaches ordinary bodies and elites, not bosses.** A boss's tells are its fight's timing: a burrow that must
   outlast the tell it places, a charge whose tell is a share of its run (`ValidateBossPatterns`), the arrival's 2.5-4 s.
   A 15% longer boss tell would move those numbers under the validator, not just the player's margin.
3. **Reach the reading place, not the room.** The rooms run to 2,700 px; "reach the room and he is gone" on the room's
   rectangle would find it empty off screen, the book never seen. 220 px of the lectern, which is inset so it cannot be
   met through a wall (the first draft's check found the Kitchens' back stair within reach of one).
4. **Heard, not pointed at.** The objective arrow would be the answer, so there is none after the first room; the
   voice is played toward the room from near him, not from the room, and the stars and the line carry the side for a
   player without sound.
5. **The rooms.** Seven of the thirteen: not the Gallery (Ma Liang's), the Armoury (beside the stair), the Treasury (its
   Chamberlain comes out the moment anyone steps in), the Solar, the Minstrels' Gallery or the Keepsake Doors (each a way
   of its own to reach). The distances keep every leg a walk across the house.
6. **He stands at a tall desk** rather than sitting, so his own sheet is the figure (no second drawing of him to keep
   in step); the desk's front hides him to the waist.
7. **The Court's wayfinder is in the building.** The director now places the wayfinder with
   `ExplorePlacer.AtBearingInBandAvoiding`: exactly `AtBearing`'s spot wherever its first search finds ground (every open
   chapter, unchanged), else the band swept for floor as far round from the events as it allows. The Court's building
   fills only the quarter of the band north-east of the stair, and a bearing out over the rock had found no floor
   within the search's reach - the compass would have stood in the wall.
8. **The Gallery hangs sixteen frames, every 3 cells, never in a doorway** (`court_layout.py`). At the old 6-cell pitch the
   eighth frame hung across the Gallery's east door, and nine frames could not hold the wizards he took once every
   chapter holds one. The 4-cell pitch after it hung twelve, one short once Ma Liang made the wizards he took
   thirteen (2026-11-03, at the merge); a frame is one 48 px cell wide, so 3 cells still leaves two of wall between.
9. **The event's line goes under the Court's.** The Court is the first chapter with both an objective line of its own
   ("Rooms found n / 13") and a side event, so a side event's line moves one line down wherever its chapter has rules.
   (Since 2026-11-03 that is the band's rule rather than this exception: `HudBand` stacks the event's line under any
   chapter line that is showing, and a tip under both - `.ai/technical-architecture.md`, "The band under the timer".)

**Verified:** build; the validator (0 errors, main's 1 warning), with `ValidateAttackPatternEnemies` and
`ValidateBossPatterns` still clean with the cancel in; `RegressionChecks.ValidateSeersVoice` (no reading room is another
room's to reach; every room has a place; on 400 seeds three different rooms, the distances kept, every place reached
only from inside; the hint's stars on the sound's four lines; the reading lines' length; the Gallery's last frame his
and every wizard he took framed; the sheets) and `ValidateQuatrains` (a clock cancelled mid wind-up never lands it, waits
its whole 2 s cooldown and then shows its whole 0.8 s wind-up; Foresight lengthens a foreseeable 0.8 s wind-up to 0.92
and leaves the rest; the shortest wind-up of any ordinary body, defaults and every scene's override, longer than the
star's fall). `_ExploreProbe --stage=12`, which SEEKS at his own walking speed and never teleports to a reading - it
waits for the voice, then walks the shortest way through the halls to where it read from: seeds 4242, 31, 1001 and 77
`--fresh-save` and 7 and 4242 `--all-unlocked` (the chest), 26 to 27 passed, 0 failed; all three readings to the desk
every time, 7 to 11 readings heard, found 59 to 88 s after arriving; each leg's walk through the halls 1.22 to 1.59 times
the straight line the sound promised (4,567 to 10,044 px), 24 to 48 s a leg; Nostradamus freed in memory. `_UiShot` of a
reading (the empty lectern, the stars and his line), the desk, the win, the clue and the character select (locked on a
save with the Court open; freed) at both canvases.

**The form, measured** (`_BalanceHarness --character=N --stage=0 --minutes=15 --all-unlocked --seek-apotheosis
--immortal`, N his roster index):

| run | his spell ascended | form taken | stars foreseen / attacks stopped by 15:00 | on elites | damage taken a minute, before -> after | Starfall's share |
|---|---|---|---|---|---|---|
| seed 1234, timescale 6 | 7.2 min (level 17, Starburst) | 9.4 (level 21) | 846 / 801 | 4 | 125-183 -> 7-67 | 52% |
| seed 77, timescale 6 | 7.7 (17, Starburst) | 9.9 (21) | 988 / 932 | 2 | 156-163 -> 43-76 | 74% |
| seed 1234, `--timescale=1 --profile=10` | 6.4 (17) | **8.5** (21) | 791 / 730 | 4 | 102-207 -> 29-92 | 43% |

The form is taken at 8.5-9.9 minutes, the level-21 gate setting the time as it does for everyone. Once taken it stops
93-95% of what it falls on - 140 to 190 attacks a minute - and the damage the bot takes falls by half to nine tenths,
though some of that fall was coming anyway (Aegis Ward and the levels were already cutting it). It is the strongest
defensive form there is against a crowd of casters and lungers, and nothing against contact, elites or the boss; whether
that is too much is a playtest question (below). Kills a minute were 375-393 in the two minutes before it and 435-461
after. **On the Court itself** (`--stage=12`, seeds 1234 and 77) the bot reached only level 12 and 10 by 15:00 and
never met the gate - the chapter's health, not the form; every chapter wizard's harness run is on an early chapter for
that reason.

**Frame cost** (`--timescale=1 --profile=10`, headless, the laptop the harness runs on): the Starfall section cost 66-88 us
a frame in the minute before the form and 77-101 us in the minute after, 0.1-0.2 KB/frame either side; an enemy's step
6.8-7.0 us a call either side (the wind-up edge is a bool compare a body a step, the star only on an edge); frames held at
6.9 ms (144 fps) either side. From 13.5 minutes every section slowed together (the player 72 to 112-171 us, the game
110 to 172-264, an enemy's step to 12-15 us) - other sessions' Godot runs beside it, which `performance.md` section 5
warns of, not the form. Not measured on the phone.

**Not verified:** whether seeking is fun, and whether a player hears the side a voice comes from through a swarm - the
probe knows the house and walks its shortest way, which is the best a player can do, not what one does; whether the
stars and the line are enough without sound on a phone; the sound, measured and never heard; whether the last covered
frame is read as a clue; the Quatrains in a real crowd (how often it stops something a player would have dodged, and
whether a cancelled cast reads as cancelled); Foresight, which nothing measures; nothing on the phone.

---

## 5. Clues: how each is heard and seen

Eric's note (`in-flight.md` 3a item 6): each wizard's hint becomes a cue **in the world, from his
direction** - the singer's notes drifting in from his side, his song heard - and the line **fades in
on screen** as a hint, not in a speech bubble. Every block above follows it:

| part | what | built from |
|---|---|---|
| the drift | something of his crossing the screen from his side: leaves, paper, ink, sparks, mist, sand | a pooled particle band entered at the screen edge on the bearing to the site; no allocation per frame (`performance.md`) |
| the sound | his own generated cue, played 300 px toward the site as today | `tools/audio/sfx.py`, category `hint`, -9 or -10, checked with `analyse.py --confuse` against the whole set |
| the line | fades in near the edge he is beyond, no band and no tail | `StageHintCaption` restyled once for all wizards |
| when | the existing schedule: pace 300, then every 150, none after 810, never in the boss, only while the save lacks him | `StageHintDirector`, unchanged |

With two events in one chapter, the director alternates them, one hint per slot, never two at once.
Captions stay under 60 characters and never name anyone.

---

## 6. Rules this bends, and the options

1. **One element per character** (question 1). R1 changes `ValidateStartingCharacters` to warn on two
   characters with the same **spell** (and form), keeping distinct elements only for the four
   disciplines. R2 lets a non-starter open on a hybrid; he claims one of its two elements, and his
path deepens that one at one milestone and takes the untagged branch at the other.
   `gameplay-design.md` "Starting Characters" and the ADR's "every required path deepens" sentence are
   rewritten to "never leads away".
2. **Gates** (question 2). G1 needs no code: `GateWizardIds` already counts by rescue chapter. Saves that
   opened the Emberdeep keep it (it is written into the opened list), so nothing is confiscated either
   way. G2 needs one field on the unlock entry (counted at the final gate only) and
   `ValidateGateReachability` taught it. The Emberdeep's locked hint names those still held; with secret
   names hidden it must name their chapters instead.
3. **A second event in a chapter** (question 3), plumbing piece P2: `SideEventDirector.ForStage`
   returns a list; the second site is placed in the same 4-6 chunk band at least 90 degrees off the first;
   the wayfinder marks both; `StageHintCatalog` is keyed by site; `ValidateSecret`'s reveal rule reads
   the wizard's own site; `StageRewards` lists both on the map card.

   **Built (2026-11-02, `wip/second-events`).** As written, and `.ai/side-events.md` "Two events in one
   chapter" has the whole of it. **To add a chapter's second event:** put it after the first on the chapter's
   line in `SideEventDirector` (`new SideEventEntry("my_site", () => new MyEvent())`); give the site a hint in
   `StageHintCatalog` (keyed by `SiteId`), deed words in `AchievementDefinitions.SideEventWords`, and what it
   pays in `UnlockCatalog` (`FromEvent(..., "my_site", ...)`); a wizard needs his `QuestLog.Prisoners` row (his
   held line is his clue), anything else a `ChapterNotes.SiteClues` line. Nothing else: placement (90 degrees
   off the first, in the band), the wayfinder, the alternating hints, the map card, the deed, `EventsWon`,
   the quest log and `ValidateSecret` all read the list. `ValidateSideEventTable` holds it right. Prove it
   with `_ExploreProbe --stage=N --fresh-save` (the two-event order runs whenever a chapter holds two);
   `--second-event-test` is the stand-in that drove it before any chapter had two.

   **Verified:** build; the validator (0 errors, main's 1 warning); `_ExploreProbe --second-event-test` on the
   Forest (grove + stand-in), the Swamp (lanterns + stand-in) and the Sands (caravan + stand-in), seeds 4242,
   31 and 7 fresh, 4242 and 31 `--all-unlocked`, and `--wayfinder-first` on 1001 fresh and 7 `--all-unlocked`
   (21 runs, 0 failed: both events found and won every time, main sites 122 to 172 degrees apart, the hints
   one each and then both the second's, the line under the timer passed between two running events);
   the Ruins, the Emberdeep, the Cave and the Dungeon unchanged on 4242 fresh and 31 `--all-unlocked` - the
   same checks, counts and coordinates as main. `_UiShot` of the Forest's map card and chapter note with the
   stand-in at both canvases. **Left for the wizards:** each event's own hint sound and cue; Medea's chapter
   hints only for her, since the well has no hint (`SitesWithoutHints`); the Court's note will carry two held
   lines (allowed: `MaxLinesFor`), not yet photographed with two wizards.
4. **The Sketchbook** pays nothing at all since 2026-11-02, not even page marks: no wizard, no deed, no
   secret (Eric). Ma Liang moved to the Court (4.8).
5. **Secret until the chapter opens** works for all but Baba Yaga, who shows as a silhouette from the
   first run (the Forest is open on a new save).
6. **CC on bosses.** Turn, blind, displace, banish, double's lure and foresee's interrupt all skip a
   `BossEnemy`; turn and banish also skip elites.
7. **Real people.** Paracelsus, Nostradamus, Seimei and Imhotep are historic, as Tituba and John Dee are.
   Seimei is enshrined in Kyoto and Imhotep was deified; both appear widely in games. None is given a
   verb that agrees with an accusation, the line Tituba's spell was chosen by.
8. **The character select** grows from nine cards to nineteen. Its grid and its scroll on the phone
   canvas need checking (`_UiShot --resolution 540x1212 --portrait --safe-area`).

**Schema.** None. Each rescue is a new site's derived Road deed, granted at once (`CompleteNow`); the
reveals are derived; `LifetimeStats.EventsWon` takes new site ids as it is. A one-time reveal beat
would need `SeenRevealedCharacterIds` and a schema step (`side-events.md` already says so).

---

## 7. What each costs, and the order

Sizes as `achievements-and-secrets.md` uses them: **S** reuses what exists, **M** needs one primitive or a
set of art, **L** needs a system. Every wizard also needs a 48-cell sprite (M), a `CharacterData_*.tres`,
an `UnlockCatalog` entry, an `ApotheosisCatalog` entry, a passive id, a hint sound and a pictogram.

| wizard | prison art | event code | form code | new primitive | rule | P2 |
|---|---|---|---|---|---|---|
| Seimei | S-M | S | M | none (`SetTargetable`) | none | no |
| Wayland | S-M | M | S-M | a slam landing on a point | none | no |
| the Sibyl | M (cliff of mouths) | M | M | per-mouth delayed audio | none | no |
| Baba Yaga | L (the hut on the 96 cell, walking) | M | M | a mover that flees (`SecretMover` inverted) | R2 | yes |
| Paracelsus | S | L | M | an escorted person enemies hunt (`HuntedEscort`, built) | none | yes |
| Imhotep | S-M | S | M | a wander steering state | R1 | yes |
| Medea | S | S-M | **L** | orbs drawn to a point; enemies attacking enemies | R2 | yes |
| Morgan | S-M | S-M | M | a second casting origin | R2 | no |
| Nostradamus | S | S | M | a wind-up cancel | R1 | no |
| Ma Liang | S | S | M | moving an enemy across the field | R1 | no |

**Order.**

1. **Decide** questions 1-4.
2. **In parallel:** P1, the rule changes in the validators (S); P2, the second event and the clue form of
   section 5 (M); and **Seimei** as the pilot: no event in the Ruins today, no rule change, a small ask,
   a form on an existing primitive. It proves sprite, prison, form and clue end to end.
3. **Wayland, then the Sibyl.** Both chapters have no event, so neither waits for P2.
4. **Baba Yaga**, once P2 and R2 are in: the first rescue a new player can make, at chapter 1.
5. **Paracelsus** (the escort primitive is the largest piece of event code), then **Imhotep** and
   **Medea** (whose form needs the enemy-on-enemy state).
6. **Morgan** and **Nostradamus**, each after its castle chapter flips.
7. **Ma Liang**, with Nostradamus: the Court's second event, so after P2 and the Court's flip.

Each piece verifies as the side events did: `dotnet build`, the validator run with `-- --no-save-writes`,
`_ExploreProbe --stage=N` winning the event on a fixed seed fresh and `--all-unlocked`, `_UiShot` of the
prison and the clue at both canvases, and `_BalanceHarness --seek-apotheosis --all-unlocked --immortal
--character=N` for the form. **What no harness checks:** whether a hut that runs is fun to chase, whether
nine mouths can be told apart by ear on a phone, whether paying a level feels like a fair price.
