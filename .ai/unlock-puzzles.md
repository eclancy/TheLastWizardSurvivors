# Unlock puzzles: every rescue a puzzle you can see

> **2026-11-09:** the Sketchbook (`stage_8`) with its whole cast - the kids' drawings, Elim's boss and ghost, the dog - and the Test Wizard were removed at Eric's request; git history holds them, and schema 28 takes both out of saves. Where this doc still names them it is the record of what was, not the game. Stage index 8 and roster index 0 are retired, never reused. A command here with `--character=0` named the Test Wizard: `--spell` / `--start-spell` now open any wizard on the spell, so say `--character=1`.

Eric, 2026-11-03: *"what i actually wanted to discuss was making sure all of the character unlocks are fun
and interesting, like the laser puzzle in halls of torment"*.

**Status: designed with Eric, 2026-11-03. Built: all eight - 3.1 Imhotep, 3.2 Ma Liang, 3.3 Baba Yaga, 3.4 Paracelsus,
3.5 Medea, 3.6 Väinämöinen, 3.7 Morgan le Fay and 3.8 Tituba (each its "Built" note).** Every rule below that says "decided" was
chosen by him that day, one question at a time. Eight of the twelve place-held wizards get a new ask; four
keep theirs. Numbers marked *first guess* are for the build to tune.

Read first: `.ai/side-events.md` ("Rules every event inherits", "Hints that something is out there"),
`.ai/chapter-wizards.md` (each event's Built note, which this doc replaces the ask of), `.ai/stage-secrets.md`
section 8 (the anti-frustration rules), `.ai/light-pool.md` (Tituba's), `.ai/endgame-castle.md` section 3
(Morgan's bridge). The comparison that led here, with Vampire Survivors and Halls of Torment, is summarised in
section 1; its sources are listed at the end.

---

## 1. The bar: what the laser puzzle does

Halls of Torment's Chambers of Dissonance hide a puzzle behind a Lost Pages altar. Bulbs ("Dissonators") each
throw a beam. Walk into one and it turns a quarter. When a beam lands on the next bulb, that bulb lights and
throws its own, and the chain ends at a monolith. A dead end means going back and turning an earlier bulb. The
layout is generated per run, it takes from one minute to several, and the reward is a turret that fights for
you in the last battle (Steam guide, below).

What makes it good, and what this doc holds every redesign to:

1. **The state is drawn in the world.** The beam shows where the light goes and whether it arrives. Nobody
   reads a rule; they watch it.
2. **One action, learned by doing it.** Touch a bulb, it turns. No text explains anything.
3. **A wrong move costs time, never progress.** Backtracking is the puzzle, not a punishment.
4. **It is generated.** No guide can give the answer, because the answer differs every run.
5. **It is solved under the horde.** The puzzle and the fight are the same minute.

Its one weakness is being found at all: one player had not found the beams after ten runs (Steam thread,
below). Ours already answer that with the hint ladder (the wizard's voice from his side, the far tell, the
wayfinder, the quest log line), so this doc changes the asks and leaves the finding alone.

Our own limits on top of those five, all inherited from `.ai/side-events.md` and `.ai/stage-secrets.md`:

- **No button.** Everything is the feet, where the player stands, or where his damage lands.
- **Readable on a phone in a crowd**, at a glance, with the sound off.
- **Nothing placed joins `"enemies"`** unless it is an enemy, or the loadout aims at it.
- **Event enemies are elites**, so they are never recycled to the spawn ring.
- **Nothing is timed by the pace clock.** A rhythm (a lamp burning, a phrase sung) is real seconds.
- **A failure resets and never locks**, and progress survives walking away.
- **Once freed, later wins pay a chest.** Deed ids, site ids, hints and rewards are unchanged by any of this.

---

## 2. The scorecard

| wizard | chapter | the ask today | a real puzzle? | decided |
|---|---|---|---|---|
| Abe no Seimei | Mystic Ruins | four stones; a press turns it and its two neighbours | **yes**: a state puzzle, the nearest to the laser | keep |
| Wayland the Smith | Emberdeep | bait the Trip-Hammers' slams onto his four chains | **yes**: the best "aha" in the game | keep |
| the Cumaean Sibyl | Sunken Cave | stand in the mouth her voice comes from first | yes, if the sound carries | keep |
| Nostradamus | the Court | follow a voice room to room, three times | partly: mostly walking | keep |
| Imhotep | Scorched Sands | three beacons, all burning at once | weak: the answer is "run a triangle fast" | **redesign (3.1)** |
| Ma Liang | the Court | walk fifteen numbered dots | no: its own note doubts it is an ask | **redesign (3.2)** |
| Baba Yaga | Enchanted Forest | stay 420-520 px from the hut for 30 s | no: holding a distance | **redesign (3.3)** |
| Paracelsus | Blighted Swamp | escort him to the ferry | no: guarding him | **redesign (3.4)** |
| Medea | Fallow Fields | fill a pan with a level's XP | no: a cost, then waiting | **redesign (3.5)** |
| Väinämöinen | Frozen Waste | damage near the ice, fire triple | no | **redesign (3.6)** |
| Morgan le Fay | the Last Bridge | keep pace with her barge | no: its note asks if it is a formality | **redesign (3.7)** |
| Tituba | Cursed Dungeon | carry the key home; three hits drop it | no: a carry under pressure | **redesign (3.8)** |

Laelaps, John Dee, Merlin, the Dark Wizard and Nimue are achievements by design, not places, and are not part
of this. The four kept are the ones Eric's playtest should try as they stand (section 5).

---

## 3. The eight

Each block: what it was, the puzzle, how it reads, how the swarm is in it, failing, how the seed makes it,
what stays, what is new, clashes, and what is still open.

### 3.1 Imhotep - route the sun

**Was:** `TombBeaconsEvent` - three beacons on a 700 px triangle round his tomb; stand by one 1.5 s to light it,
each burns 25 s, all three alight at once opens the slab.

**The puzzle (decided).** Sunlight falls through a gap in the dunes onto one bronze mirror: the source. Between
it and the tomb stand four to six more mirrors on stone stands. **A mirror swings to face away from wherever he
stands** while he is within reach of it (*first guess* 140 px): walk round it and its face follows, and its
light goes the other way. A mirror the light reaches shines where it faces. Bring the light, mirror to mirror,
onto the sun-disc socket over the tomb's door and the slab slides (the existing win beat). It is the laser
puzzle in his own legend, and his form is already the Eye of Ra.

- **Facing snaps to eight directions** (decided: diagonals too, for more routes), so the beam is a readable
  line and a solution is exact.
- **The beam passes through bodies and burns them lightly** (decided, Eric: *"the beam should not be stopped
  by the bodies, just burn them lightly as it passes through them"*). *First guess* 3 damage every 0.5 s to
  each body on a lit leg, through `Enemy.TakeDamage` with every argument passed. Terrain stops it; nothing else.
- **How it reads:** a warm gold line from mirror to mirror, drawn in `Air` (never under the bodies); a lit
  mirror's face catches the light; the socket glows when the light is on it. The wizards' light is warm and
  his is cold (`.ai/world-and-tone.md`), so the sunlight reads as a thing on the player's side.
- **The swarm:** the light is a weapon while you work, which pays for the minute it takes.
- **Failing:** nothing to fail. A wrong facing just sends the light somewhere useless; turn it back.
- **The seed:** a solution path is drawn first (three or four legs, each at least 300 px, on open ground the
  whole way, `IsExploreGroundClear` sampled along each leg), then one or two decoy mirrors off it, then every
  mirror is set to a wrong facing. The generator must refuse a layout solvable with fewer mirrors than it placed
  on the path.
- **Stays:** the tomb, its site and band, the hint ("Something knocks, stone on stone, under the sand."), the
  deed, the wayfinder. **Goes:** the beacons, the 25 s burn, the six dead their light drew.
- **New:** a mirror sprite at eight facings, the light shaft, the socket, the beam; a mirror-turning sound and
  a sound for the light arriving. Code: the beam recomputed only when a facing changes (a few segments marched
  against the streamer's ground data), the burn through `EnemyRegistry.ForEachNear` with a struct visitor on a
  0.5 s tick (`.ai/performance.md`).
- **Clashes:** Seimei's stones also turn, but by a plate and a quarter at a time; these follow where he stands.
  The Fulgurite (the Sands' vault) is a storm drawn to a glass tree, not a beam.
- **Open:** how many mirrors; whether a leg may cross another.
- **Size:** M-L.

**Built (2026-11-08, `worktree-agent-a9156a2847e34170c`).** `SunMirrors` (new: the layout, the rule, the light and the proof,
pure, in `scripts/SunMirrors.cs`) and `TombBeaconsEvent` (rewritten; the class keeps its name as the site keeps its id,
`tomb_beacons`, so its deed, hint, reward, the tomb, the slab's opening, his climb out of the stair and the hand-over to the
run are as they were). The mirror at eight facings, the winged sun-disc dark and lit, and the arrow's pictogram are
`tools/art/tomb_beacons.py`'s (`mirror.png`, `socket.png`, `poi-tomb.png`); `beacon.png` is retired with its sidecar. The
light and its shaft are drawn by code. Sounds `mirror_turn` and `sun_arrive` (`tools/audio/sfx.py`). Schema: none.

- **What plays.** Sunlight falls from the upper left, where the key light is, in a wide warm shaft onto one bronze mirror
  on a granite stand - the source - and pools on the sand under it. Four or five more mirrors stand round the tomb, 340 px
  or more apart. Each disc stands upright on a bronze spindle and faces one of eight ways; its face (polished, catching the
  key) shows toward the camera, its back (dark bronze, a boss and a ring) away, a narrow ellipse edge-on, so a facing reads
  from the stand as well as from the light. Within **140 px** of a mirror its disc swings, an eighth at a time, to face
  away from his feet, and a faint dashed ring on the sand shows the reach while he is near; the objective line says "The
  mirror turns away from you - walk round it to aim it" while he is inside one. A mirror the light reaches shines where it
  faces: a warm gold line in the air (the light element's ramp - edge, mid, a hot heart), motes running along it from the
  source toward where it ends, so its direction reads at a glance; it ends on the next mirror's disc (which flares), on
  terrain (a small splash), or runs out after **760 px**, fading over its last 140. The light passes through bodies and
  burns each one on a lit leg for **3 every 0.5 s**. Brought onto the winged sun-disc on the cornice over the door, the
  disc lights, the slab slides (the beacons' 0.5, 1.0, 1.5 s) and he climbs out into the column of light. The mirrors stay
  standing as he left them, the disc lit. The arrow points at the tomb from far off and, among the mirrors, at the mirror
  the light ends on - never which way to turn it.
- **The Open line, decided in the build.** *How many mirrors:* five or six in all - the source and four or five more
  (3.1's "four to six more", where "three or four legs" plus "one or two decoys" would allow three): three legs take two
  decoys, four legs one or two. *Whether a leg may cross another:* no. No leg of the solution crosses another or comes
  within **110 px** of a leg it does not meet or of a mirror not at its ends, so the route reads as one line across the
  sand; a decoy keeps 110 px off every leg too.
- **What else the build decided.**
  1. **Legs 340-540 px**, not "at least 300": with mirrors 340 px apart a player walking in to one from just outside its
     reach (170 px out) is still 30 px outside the next one's, so turning one never turns another.
  2. **The light is one table, worked out at placement** (`SunMirrors.HitFrom`): for every mirror and facing, where the beam
     ends, cast against the same ground test a body is held to (`Node2DGame.IsGroundBlockedAt`, every 8 px). The run never
     casts a beam; it re-traces the chain through the table only when a facing changes, and the proof reads the same table.
  3. **What stops the light.** Another mirror (passing within 18 px of its foot), which lights; the disc, within 22 px of
     the tomb's foot and **only from the front** (travelling north, north-east or north-west: it faces south); the tomb's body
     (168 x 110 px behind its foot), from behind and the sides; terrain. Not bodies. A beam that falls on a mirror already
     lit ends there.
  4. **The turning holds by 7 degrees** past the half-way line between two facings, so standing on the line does not set a
     disc chattering; nearer than 16 px to the stand it holds (the stand is solid anyway: a trunk's footprint on the
     blocking layer, as the beacons' pillars had).
  5. **The burn goes through `EnemyRegistry.CollectNear`, not `ForEachNear`** as 3.1 wrote: a burn can kill, a kill can
     start another walk of the registry (a Frost Shard), and `ForEachNear`'s visitor shares one index buffer that walk
     would clear. `CollectNear` fills the event's own list, allocation-free once warm, on the 0.5 s tick, as Sleet Runes
     and the Aegis Ward burn a segment. A body's feet within 24 px of a lit leg's line on the ground are in the light; a
     body on two legs is burned once a tick. Through `Enemy.TakeDamage(3, false)`, both arguments.
  6. **The light's height is 64 px**: the mirror's disc is 32 art rows over its foot and the sun-disc's middle is on the
     tomb's row 32, so a beam leaves one disc and lands on the other at the same height (`SunMirrors.BeamHeight`).
  7. **The start.** Every path mirror faces wrong, and where a decoy hangs off the source the source faces into it: from the
     first moment the light is seen going mirror to mirror - into a dead end.
  8. **He arrives** at the tomb (420 px, as before) or within 260 px of any mirror, so a player who follows the light to the
     source has arrived there, before his feet can turn anything.
  9. **Decor keeps off** every mirror (90 px) and the solution's legs (46 px every 80); the mirrors keep off the caravan's
     footprints through a new `ExplorePlacer.IsOffOtherSites`. The tomb is swept for through the band where a layout fits
     (`EventSiteInBand`'s `also`, 16 rolls a candidate), on a 200 px footprint, no longer the 494 px the beacons' triangle
     took. `Generate` itself allows 80 rolls.

**Verified:** `dotnet build` (0 errors); the validator with `-- --no-save-writes` (0 errors, the known `test_wizard`
warning), which now runs `RegressionChecks.ValidateTombBeacons` as the mirrors' proof. It covers four things:
- **The rule.** Standing behind a mirror sets every facing from every start (72 cases). Out of reach, or on the stand, it
  holds. The hysteresis holds a facing to 1.5 degrees inside its 29.5 and gives way 1.5 past it.
- **The light.** It lands on the disc from the front and on both diagonals, and is stopped by the tomb's body from behind
  and from the side. Along the foot it never lands on the disc. A mirror in the way catches it, a ridge 200 px ahead stops
  it at 208 px, and with nothing in its way it runs its 760 px.
- **The seed, 80 tombs** - 40 on open ground, 24 by a lake 180-300 px across, 16 by a 1,400 px ridge of rock - each
  rolled twice. All laid, every one the same twice; 52 of three legs and 28 of four, 65 of five mirrors and 15 of six;
  the slowest kept on roll 28. Every one keeps its shape and is proved: solvable with exactly its path's mirrors, its
  start dark, no single wrong facing of a path mirror reaching the disc, every decoy able to catch light. The walk -
  in from just outside each path mirror's reach to behind it and out, 6 px a step, every mirror answering his feet -
  brings the light to the disc on every one. 52 ms.
- **The rest.** The art at its sizes, the two sounds allowlisted, the hint unchanged.

Audio: `python tools/audio/build.py` (112 files, every other one byte-identical) and `analyse.py --confuse`, 0 confusable
pairs (`.ai/audio-manifest.md`, 2026-11-08).

`_ExploreProbe --stage=6` now AIMS. It works out the fewest mirrors to the disc from what a player sees (the event's beams,
`SunMirrors.ShortestRoute`), walks round to just outside each mirror's reach on the line behind it, in until it faces the
right way and back out, and walks round every other mirror's reach on the way, at his own walk. Its final runs:

| seed | save | passed, failed | route | on the disc after arriving | walked | burns |
|---|---|---|---|---|---|---|
| 4242 | `--fresh-save` | 49, 0 | 3 mirrors, 3 turns, all meant | 11.7 s | 2,651 px | 51 |
| 4242 | `--all-unlocked` (the chest) | 43, 0 | 3 mirrors, 3 turns, all meant | 11.8 s | 2,654 px | 65 |
| 7 | `--fresh-save` | 49, 0 | 3 mirrors, 3 turns, all meant | 13.6 s | 3,075 px | 61 |
| 11 | `--fresh-save` | 49, 0 | 3 mirrors, 3 turns, all meant | 10.5 s | 2,279 px | 37 |
| 2026 | `--fresh-save` | 49, 0 | 3 mirrors, 3 turns, all meant; the light began in a decoy off the source | 10.9 s | 2,416 px | 49 |
| 31 | `--fresh-save --decoy-first` | 51, 0 | 3 mirrors, 4 turns, all meant | 15.6 s | 3,519 px | 37 |

- **On every run:** it began dark, the route was walked once (no re-plan), the swarm was cleared only every 6 s, and
  Imhotep was freed in memory, his deed recorded by its site, on every fresh save.
- **The decoy leg:** with `--decoy-first` the source was first turned into decoy 4 - the light went there and stopped, the
  disc dark - then turned back, and the route finished.
- **The probe's first walk failed on seed 31,** and the failure was the probe's, not the rule's. It went round other mirrors
  toward a point beside them, and a chord between two points 40 px outside a reach dips inside it: it swung the source on
  its way to the next mirror and the light was lost. It now walks round on the circle. It re-plans if the disc is dark
  at the end, as a player watching the light would; that never happened on the final runs. Its new check - every facing
  change was one it meant - holds it to that.
- **`_UiShot`** (windowed), `--stage=6 --terrain-seed=4242 --event-index=1`. Mid-solve with `--arrive --mirrors-aimed=2
  --goto=event:1 --hold --frames=150` at 1280x720 (`--goto-offset=420,80`) and at 540x1212 `--portrait --safe-area`
  (`420,0`): the sunlight's shaft, two lit legs, mirror 2 sending it west, the arrow over mirror 2, the reach rings.
  The win with `--win-event --fx-lead=130 --frames=300` (a lead past two thirds of the frames fires before the event
  starts): "Wizard freed - Imhotep", the slab open, the disc lit, the whole route drawn onto it as it fades.
  `--mirrors-aimed=N` is new (it replaces `--beacons-lit`).

**Not verified:**
- **Whether it is fun**, or whether a player finds "it turns away from me" from the turning alone and the objective line.
- **Mirrors disturbed in passing.** A player walking straight from one mirror to the next will sometimes pass through a
  third's reach and swing it. The probe walks round them; a person may not. The light shows it at once, and the turn is
  heard, but it could read as the puzzle undoing itself - the first thing to watch in a playtest.
- **The read on a phone in a real crowd:** the two canvases were photographed on a PC, with the chapter's arrival card
  over a third of the screen, and the light is a thin line among the bodies.
- **The sounds by ear.** Nobody has heard the turn or the arrival.
- **The burn's 3 every 0.5 s against real play.**
- **Four-leg layouts live:** every probed seed rolled three legs. Live, four-leg layouts look rarer than on paper -
  the Sands' ground and the band's 16 rolls a site favour the shorter path - though the proof walks 28 of them.
- **Light over water.** Water stops the light because it stops a body (`IsGroundBlockedAt`). If the Sands' oases should
  let it across, the ground test is the one line to change.

**Was:** `DottedDoorEvent` - a door sketched on the Gallery floor with fifteen numbered dots; walk them in order
within 30 px. Its own Built note: "whether tracing is a real ask".

**The puzzle (decided).** The door is a figure of five to seven charcoal dots joined by seven to ten faint
lines - its frame, its arch, a brace, its ring. **Ink every line exactly once.** Walking a line from dot to dot,
within the existing 30 px, inks it (`DottedDoorEvent.Step`'s rule, kept). **Walk along a line already inked and
the whole drawing smudges** back to charcoal (decided), and the stroke starts again anywhere. Stepping off a
line to dodge only pauses it, as now. The insight is where to begin: at a dot where an odd number of lines meet.

- **Finished lines become walls the household cannot cross** (decided): his brush makes what it paints real, so
  the drawing shelters him as it grows. The player crosses them freely - he is walking on them. When the door is
  whole and opens, or the drawing smudges, the walls go with the ink.
- **How it reads:** charcoal lines grey on the oak, ink black with a wet head where the stroke is, a dot lit
  when every line into it is inked. The smudge runs and fades over about a second, so a mistake is seen.
- **The seed:** the figure is drawn from a family of door-like graphs, keeping only those with an unbroken
  stroke through every line (an Euler path), and of those **only ones with exactly two odd dots**, so that
  starting in the wrong place fails and the start is the thing to see.
- **Stays:** the Gallery, the site under the first portrait, the household's six, the hint, the deed.
  **Goes:** the numbers, the fixed order.
- **New:** the generator and its proof; the smudge; the ink-walls. **The walls are a collision question:**
  nothing today stops enemies and not the player (enemies mask layers 1, 4 and 5; the player's body meets 1 and
  5, his feet 4). Either a new layer that only enemy masks include (`Enemy.ConfigureEntityCollision`, and every
  mask checked, as `CLAUDE.md` asks), or steering - the household's `AdjustSteering` turned off the ink. A layer
  is the honest wall; steering is cheaper and leaks. **Decided: a new collision layer** that only enemy masks
  include; the player's body and feet never meet it.
- **Clashes:** none. No other ask draws.
- **Open:** the figure family; how long a line counts as "walked along" before it smudges (*first guess* 40% of it).
- **Size:** M.

**Built (2026-11-07/08, `feat/ma-liang-door`).** `DoorFigure` (the family and the geometry) and `DoorStroke` (the
rule, pure, one step a frame) in `scripts/DoorFigure.cs`; `DottedDoorEvent` keeps its site, deed, hint, reward,
household and ending. **The family** is found at first use from a pool of 22 lines over eight dots (the jambs'
feet, the springs, the crown, a centred ring, the sill's middle, a keystone): the four outline lines always, three
to six more, five to seven dots, connected, exactly two odd dots, no line within 44 px of a dot it does not end at,
no two lines overlapping or leaving a dot under 30 degrees apart - **317 figures, evenly 7 to 10 lines** (a sweep
chose the ring centred and the keystone added; off-centre it was 30 figures of nearly all 7 lines), mirrored or
not by the seed. Pair and line-dot conflicts are precomputed, so a candidate is bit tests and there is no table to
drift from the rules. **The rule:** walking a line dot to dot within 30 px inks it; the stroke goes on only from
the pen's dot; walking along an inked line for 40% of it (away from its dots - a fifth of the line at most, or the
80 px lines could never smudge) smudges everything back to charcoal; a stroke begins only out of the dot his feet
last stood on (a walk out of one end of a short line could otherwise read as the other), and until its first line
is inked it can be begun afresh elsewhere, so a line brushed on the way to the chosen start costs nothing. **The
walls:** each inked line raises thin segments on a new layer, `LevelTilePainter.EnemyOnlyCollisionLayer` (7) -
enemies mask it, the player never does; not the water's layer 6, which the wade check queries at his boots. They
go with a smudge, a stand-down and the door. **Drawn** by code on the event's ground layer (charcoal, ink, the wet
head, dots lit when every line into them is inked, the smudge sliding and thinning over 1.1 s); the old numbered
sketch sheet is no longer drawn, the painted door still is. **Proof:** `ValidateDottedDoor` over all 634 figures
on paper (the true stroke from an odd dot inks all with no smudge, no even start covers one, every wrong start ends
stuck and smudges walked back, stepping off pauses, crossing is not walking); three bugs it caught before any run
(Hierholzer's spliced output from a wrong start, a line begun from its far end, the short-line smudge guard).
`_ExploreProbe --stage=12` plays it as a player who has seen the insight - re-planning from the pen if a line was
inked on the way, walking the ink on purpose from a dead end - and `--wrong-start` / `--step-off` exercise the
smudge and the pause live. Still for eyes: whether the odd dots are findable by counting at a glance under the
household's pressure, and whether the ink-walls' shelter reads as his brush making the drawing real.

### 3.3 Baba Yaga - herd the hut into her yard

**Was:** `WalkingHutEvent` - the hut walks its circle; within 420 px it bolts; stay within 520 px for 30 s and
it tires. Its Built note: whether a player finds the 420-520 band at all.

**The puzzle (decided).** Near the hut's circle stands **her yard: a fence of bones round a clearing, with one
way in** (*first guess* a 260 px yard, a 120 px gap). The hut always keeps its back to him, as in the tales, and
only turns its door to him when it has nowhere left to run. **Drive it through the gap**: once it is in the yard
it stops, turns, kneels and the door opens (the existing win beat). The 30 s bar goes (decided).

- **The physics are already there:** `FleeingMover` flees straight away from him and slides along whatever
  stops it. Herding is that rule plus a goal - stand on the far side and it runs where you want. Trees, water
  and the fence itself are the walls he herds with.
- **How it reads:** the yard is lit by the skull lanterns of the tales, in the violet of the hut's far tell, with
  the gap between the two brightest. The hut's back is always to him, so its heading is always readable.
- **Failing:** lose it and it calms and stands where it is, as now; its prints lead back.
- **The seed:** the yard is placed with the circle, its gap facing away from the circle (*first guess* the hut
  must be driven 600-900 px), on ground where the approach is open.
- **Stays:** the hut, its walk and bolt, the prints, the birds, the hint, the deed. **Goes:** the bar and the band.
- **New:** the yard (bone-fence segments, the lantern posts), the "cornered" test, a sound as it is driven in.
  `ValidateWalkingHut` gains: on paper, a herding chase drives it in under two minutes on open ground, and a
  straight chase does not.
- **Clashes:** the Pale Hart (the Forest's vault) is followed and waits for you; the caravan is caught. Nothing
  else is herded.
- **Size:** M.

**Built (2026-11-07, `feat/baba-yaga-hut`).** As decided, with these particulars: the yard is a 260 px ring of
bone posts (fence collision on the blocking ground's layer, so boots and the swarm meet it; the hut's own
ground probe tests each step as a SEGMENT against the ring, so the thin fence can never be probed past), its
120 px gap facing away from the circle, the two gap posts carrying skull lanterns lit in the hut's violet
(`DrawLight`). Entering `CorneredRadius` (the ring less 34 px) fires the existing tired beat unchanged - stop,
turn, kneel, the door, her. The bar, the band and `KeepRadius`/`HuntSeconds` are gone; `mover.Tire` now rises
with `DriveProgress`, so its bolts dull as it is pressed. The hut also got the rest of item 59: four-sided art
(front and side walk cycles appended to the sheet, facing picked from its heading with hysteresis) and a solid
feet box - and it still falls facing the camera, door in view, as the kneel frames always did.
`ValidateWalkingHut` now herds it on paper into a yard 750 px out (must corner it under two minutes) and runs
the same straight chase (must never corner it); `_ExploreProbe --stage=0` herds it live - 42 checks green, the
`--lose-hut` leg checks it calms and stands where it was left. Not built: the driven-in sound (reuses the
Earth impact for now; a bespoke creak-and-settle is a tools/audio ask for later).

### 3.4 Paracelsus - choose his path

**Was:** `GibbetCageEvent` - hold the winch 2 s; he follows your steps at 75% of your walk to the ferry
1,450-1,750 px off; every plain chaser within 250 px goes for him and bites; every 5 s three Mired rise across
his way.

**The puzzle (decided).** He still walks in the player's footsteps. Between the gibbet and the ferry the bog is
cut by **rot pools that hurt only him** (*first guess* six to ten, 120-260 px across). The player can walk
through them; he cannot, so **the route the player walks is the route he takes**, and the puzzle is finding a
way through the gaps. **The Mired still rise across his way** every 5 s, closing gaps, so the safe line keeps
changing. **Ordinary chasers stop going for him** (decided): the puzzle is the route, not guarding him.

- **How it reads:** the player's steps glow on the ground for the stretch he has not walked yet - his path,
  seen ahead. The pools bubble a sickly yellow-green when he is near one, and he coughs and his health shows when
  he steps in. They must not look like the Swamp's own hazard, which hurts the player.
- **Failing:** as now - he sinks where he stands and is back in the cage, the winch to hold again.
- **The seed:** the pools are placed along the line to the ferry with at least one open route through them,
  checked on paper.
- **Stays:** the gibbet, the winch, the ferry, the Mired, the hint, the deed. **Goes:** the 250 px hunt and the
  chasers' bites.
- **New:** the pools and the step trail. Small: mostly a removal.
- **Clashes:** the lanterns (the Swamp's other event) explore; the Swallow-Hole (its vault) lures. No route puzzle.
- **Open:** a pool's bite; whether the trail ahead is shown (*recommended:* yes, it is the whole read).
- **Size:** S-M.

**Built (2026-11-04, `claude/sweet-kapitsa-3cbab8`).** `GibbetCageEvent`, `RotPoolLayout` (new), `LedEscort` (was
`HuntedEscort`); the pools are `rot-pools.png` from `tools/art/gibbet.py` (`rot_pool`), his cough `escort_cough.wav` from
`tools/audio/sfx.py`. Site, deed, hint, reward, the winch, the landing, the punt and the fall are as they were.

- **What plays.** Hold the winch, the cage comes down, he steps out and follows your own steps at 75% of your walk to
  the ferry. Between the cage and the landing lie three walls of rot, each a chain of merged pools with one gap and one
  pool beyond it, the gaps on alternate sides of the straight line (`RotPoolLayout.Roll`). The rot is a warm brown
  sludge lying on the peat; while he is within 150 px of a pool, or the steps ahead of him run into it, it stirs: the
  skin goes the poison ramp's sickly yellow-green and bubbles, with a faint glow. Your steps he has not walked yet glow on
  the ground, pale gold, a shimmer running along them toward you; where they lie in a pool they turn the rot's green.
  In a pool he coughs (`escort_cough`, at most every 1.1 s), goes the rot's colour, rings roll out round his feet, and
  his bar appears (it is hidden until the rot has had some of him). Every 5 s three Mired rise in the next gap ahead of
  you both; they chase you, not him. Nothing hunts or bites him: the 250 px hunt, `Enemy.ClassHuntsEscort` and the
  bites are gone, and `Enemy`'s chase branch is as it was before 4.4. When the punt takes him, the rot fades off the bog
  over 2 s.
- **The Open line, decided in the build.** *A pool's bite:* 5 of his 60 the moment his feet are in one and every 0.25 s
  they stay - a brush is one or two, a full crossing of a mid-sized pool four or five, so two or three crossings take
  him. *The trail ahead:* shown, as recommended.
- **Going back costs time, not him.** Walk back over your steps and the loop you close is one he leaves out
  (`LedEscort`: a crumb laid within 40 px of one he has not reached, at least four crumbs on, and he steps across from
  the one to the other), unless the step across would cross a pool or water, when he walks it as you did. That is
  section 1's "a wrong move costs time, never progress" for a walk: see your steps run into a pool ahead of him, go back
  and round, and he goes round. The drawn steps and the walked steps are one list (`CopyAhead`), so the trail shows the
  loop left out.
- **The proof on paper.** `RotPoolLayout.Generate` rolls until the layout keeps every rule and a flood over a 24 px grid
  along the line finds a way that keeps 50 px off every pool's edge and a body's half-width off water; failing twelve
  rolls it takes pools away, largest first, until there is one. `RegressionChecks.ValidateGibbetCage` lays out 24 at
  every launch (both ends of the walk's length, three bearings, half with a lake) and checks the count, the clear ends,
  no squeeze, the straight line cut, a way through that never enters a bite or water; and walks `LedEscort` round a
  lake, up a dead end and back (left out), and up it with the step refused (walked).

**What moved from the block above, and why.**

1. **Six to twelve pools, not six to ten, and the walls reach across the line.** The first build's chains stopped 60 px
   past the straight line, and a sweep showed the way through was often two legs: one straight line a step to the side
   threaded every wall's end and the next wall's gap - no weave. Each chain now reaches past its neighbours' gaps by
   twice the route's clearance, which on the shortest walks (where there is room only for pools up to 176 px) takes
   eight to twelve pools; chains are drawn from the larger half of the sizes, so the 120 px size is drawn but not used.
   Ways through are now mostly four to six legs; going round all three walls' ends is open too, and longer.
2. **No squeezes.** Two pools are merged (overlapping by 40-60 px, enough that their bites meet too) or at least 120 px
   apart; a 60 px slot that looks passable on a phone and puts him in the rot is never laid.
3. **The Mired rise in the next gap,** not only "across his way": three across its width every 5 s, so the gap you were
   heading for is shut for a while. They are the plain spawn as before (not elites); past the last wall they rise
   across his way as they did.
4. **The pools must not look like the Swamp's hazard.** The Swamp has no ground hazard (`.ai/terrain.md` section 6's
   table); what hurts there is Mother Rot's bright rot green and the bodies. So the pools are told apart from the two
   things they could be mistaken for: the Swamp's own grey-green water (which blocks the player - a pool that looked
   like it would read as a wall) and anything outlined like lava or a pit. Brown, edgeless, soaked into the peat; the
   poison green only when stirred, near him.
5. **His bar is hidden until the rot bites,** and the objective line says why the first time it does ("The rot is
   choking him - lead him round it"), then warns below 40%.
6. **The walls are laid across the line from the winch, not the cage door.** The winch is where the player stands to
   lower the cage and walks on from, 110 px to one side of the door; laid across the door's line, a probe walking
   straight from the winch slipped through two walls' gaps and took two bites. Across the winch's line, the same
   straight walk took six (30 of his 60, two pools). `RotPoolLayout.StartClear` went from 240 to 280 so the rot stays
   180 px off the cage.

**Verified:** `dotnet build`; the validator with `-- --no-save-writes` (0 errors, the known `test_wizard` warning);
`ValidateGibbetCage` as above, silent (the 24 take 40-140 ms of a debug build; all of `RunAll` took 31 s on the same
machine). A one-off sweep of the same rules over 3,000 layouts (lengths 1,466-1,798, random bearings; open ground, one
lake, two lakes): every one had a way through; 4% needed a second roll; three two-lake layouts fell back to taking
pools away and their straight line then missed the rot; ways mostly 4-6 legs (eight of 3,000 two legs), 1.19x the
straight line; 354 px of the straight line in a bite on average. `_ExploreProbe --stage=3`, which now plans its own way
through the gaps from where it stands each time he steps out (`RotPoolLayout.FindRoute` over the pools and the ground)
and walks it at the player's walk: seeds 4242, 31 and 7 `--fresh-save`, 56 passed and 0 failed each - ways of 4, 6 and
4 legs, 1,754-2,142 px against 1,541-1,748 straight; he walked 1,721-2,042 px and reached the landing with 60 of 60,
never in a pool, while bodies were within a touch of him on 20-119 frames and took nothing; 6-9 Mired risen, nearly
all in the gaps; Paracelsus freed. 4242 `--all-unlocked` (the chest): 50 passed, 0 failed, 60 of 60. 31 `--let-him-fall`:
58 passed, 0 failed - led into a 232 px pool he took 12 bites and sank, was in the cage again, was lowered again and
led round in 6 legs with no bite. 7 `--straight`: 56 passed, 0 failed - walked straight from the winch he took 6 bites
in two pools and reached the landing with 30 of 60.
`_UiShot --scene=res://scenes/node_2d_game.tscn --stage=3 --win-event --event-index=1 --escort-at=F --terrain-seed=4242`
(`--escort-at` now stands him on the proved way, with the steps ahead laid along it; `--escort-straight` lays them
straight at the landing instead): mid-walk at 1280x720 and at 540x1212 `--portrait --safe-area`, him beside a stirred
pool and the steps round it; the wrong line at 1280x720 (him in a stirred pool, tinted, his bar up, the swarm on the
player); and the win at both canvases ("Wizard freed - Paracelsus", the punt, the rot fading). A known windowed-GL crash
in a C# finalizer (`gchandle.is_released()`, `.ai/releasing.md`) took some `_UiShot` launches before any of this ran,
with the new check switched off as well; each was retried.

**Not verified:** whether it is fun; whether the steps, the stirring and the brown sludge read at a glance in a real
crowd on a phone (the phone canvas was photographed on a PC, and the 6 px steps are small there); whether a player
finds "go back over your steps" without being told; the cough by ear; whether eight to twelve pools clutter the
Swamp's screen; the bite's 5-of-60 against real play. The proof covers water, walls and fences, not trees: each gap is
kept clear of decor, but elsewhere a trunk is the player's to walk round, as it always was. The probe walks its planned
way exactly; a person's line wobbles, and the 50 px the way keeps off every pool is all the room for it.

### 3.5 Medea - balance the scale

**Was:** `TitheScaleEvent` - every XP orb that lands within 320 px of the reeve's scale goes into its pan until
the pan holds one level's XP at that moment of the run.

**The puzzle (decided).** The scale gets a second pan; the reeve's iron weight goes. **XP that lands on the
left of the scale's post goes to the left pan, on the right to the right**, so where he fights decides which
pan fills. The beam tips toward the heavier. **Once the two pans hold a level's worth between them, keep the
beam level for 3 s** (decided) and the bar falls from the granary door. Overshoot one pan and the only answer
is to fight on the other side, which costs more: the reeve takes the difference too.

- **How it reads:** the beam's angle, which is readable from a phone's length; the 320 px ring split by a line
  down from the post; a level mark on the post that lights while the beam is inside it.
- **Failing:** nothing locks; the 3 s only count while it is level.
- **Stays:** the granary, the yard, the price (one level's XP at arrival), the hint, the deed.
- **New:** the second pan's frames and the tilt. Small.
- **Clashes:** none. It is still the only ask that costs the player something.
- **Open:** how wide "level" is (*first guess* within 10% of the price); whether the total paid is capped
  (*first guess* 1.6 levels), so a bad start cannot cost a run.
- **Size:** S-M.

**Built (2026-11-07, `feat/medea-scale`).** As decided, both open questions at their first guesses
(`LevelBand` 10%, `CapInLevels` 1.6). The scale's sheet is seven frames (tools/art/tithe_scale.py): left-heavy by
three steps, level, right-heavy by three - level has its own frame, so it reads as a state, never a near miss - and
the reeve's weight is gone for a second empty pan. A needle stands square to the beam and leans to the heavier pan;
a forked iron cap on the post head holds the level mark, which lights (and runs a ring round itself as the 3 s
fill) once the price is met and the beam is inside the band. An orb takes its side as it LANDS (`GranaryTithe.SideOf`,
the line down through the post) and flies to that pan; the ground ring is split by a dashed line through the post,
each half filling from the bottom toward half the price. At the cap the scale claims nothing more and the beam
eases level over 1.2 s while a smoke of light lifts off the heavier pan - the reeve keeping the difference. The
hold counts only while level and starts over otherwise. `FrameForDifference` is shared by the beam and the
validator, so the picture and the rule cannot disagree. `ValidateTitheScale` checks the frames run left- to
right-heavy with level truly level, the binning, sides, the cap, and closed-scale silence; `_ExploreProbe
--stage=9` plays it as a reader of the beam would - fight on the lighter side (killing only there), step out of
the reach to hold once level.

### 3.6 Väinämöinen - sing it back

**Was:** `ThawEvent` - damage dealt near the block on the frozen lake thaws it; fire counts triple.

**The puzzle (decided: it replaces the thaw).** Over the lake round the block are stretched **five great strings
of ice - a kantele, the instrument he made in the legend - each with its own note** (decided: strings, not
stones). The ice sings a phrase: strings light and sound in turn. **Step on them in the same order** (a string
counts where his feet cross it). Three phrases, of
three, four and five notes (*first guess*: each one the last with a note added, which is kinder to memory). A
wrong stone replays the phrase; nothing is lost but the time. Each phrase sung back takes the block one thaw
stage further (it already has four), and the last frees him. The legend's singer, who sang his rivals into the
bog; and his hint was always "Someone is singing under the ice."

- **How it reads:** each string lights in its own colour as it sings, and again under his feet when he gets it
  right, so it works with the sound off.
- **The swarm:** the strings fan out from the block (*first guess* 200-260 px long), so the phrase is sung on the
  run through the crowd. An enemy on a string does not count; only the feet.
- **The seed:** the phrases.
- **Stays:** the lake and its shader, the block and its stages, the hint, the deed. **Goes:** the heat, fire's
  triple, the window of clear ice as the place where fighting counts.
- **New:** five ice strings drawn on the lake, their lit frames, five plucked notes in one voice, checked with
  `python tools/audio/analyse.py --confuse`.
- **Clashes: the Broken Ward** (the Ruins' vault) also asks for things touched in order. It is read from its
  stones' notches; this is remembered from a phrase, on strings that look nothing like stones - which is why
  strings were chosen. Never write the order on the strings.
- **Open:** whether a phrase extends the last.
- **Size:** M.

**Built (2026-11-07, `claude/bold-cray-4910f6`).** `ThawEvent` (rewritten), `Kantele` (new: where the strings lie, the
songs, `StringFeet` - the feet on them - and `SingBack` - the answer on paper), `ThawLake` (lays the strings over the
lake). The strings at rest are `thaw-strings.png` from `tools/art/kantele.py` (new; it reads the lake's geometry from
`thaw_lake.py` without re-running it); the notes are `ice_string_1`-`5` and `ice_string_wrong` from `tools/audio/sfx.py`
(`.ai/audio-manifest.md`, 2026-11-07). Site, deed, hint, reward, the lake and its shader, the block and its four stages,
the guard (four every 5 s while he is at the lake) and the win beat are as they were. Schema: none. The heat, fire's
triple and `StrikeListener` are gone from the thaw (the listener stays for the breakables).

- **What plays.** Five strings of ice fan out from the block to the shore at fixed bearings (lower left, upper left,
  upper right, lower right, straight down - the two upper ones flank the block's top), low to high clockwise: A3 D4 F4
  C5 A5, in fire, lightning, grass, water and arcane - each string its colour end to end at rest (the body in the
  note's dark edge tone under a pale ice highlight, its tuning pin by the block and the bead on its ice stake at the
  shore in the note's bright tones), the two lowest the thickest. Once he is within 380 px the strings shimmer once (a breath), then
  the ice sings the phrase: each string lights in its colour from its pin out, shakes, and sounds, 0.6 s apart. A row
  of hollow pips under the block counts the notes, each going white as it is sung. Then the ice listens: a string
  counts when his feet touch it, and the one he touches lights in its colour from his feet outward, sounds, and fills
  the next pip in its colour. Right to the end of the phrase and the strings it used ring together, rising, the block
  cracks a stage and a glint crosses it; 2 s later the next phrase is sung. A wrong string goes dull and rattles
  (`ice_string_wrong`), the pips empty, and 1.3 s later the phrase is sung again; phrases sung back stay sung. The
  third sung back strums all five and the old win beat runs - the crack, the chips, the column.
- **The Open line, decided in the build.** *Whether a phrase extends the last:* yes, the first guess - one song of
  five notes, sung back three, four and five long.
- **What else the build decided.**
  1. **The strings are 145-194 px, not 200-260.** The lake is 640x512 and the block in its middle; from 80 px out
     (clear of the block's foot, so its pins show) to 0.88 of the ellipse (the shore's bays come in to 0.92) is what
     there is. The two flanking the block's top and the one straight down are the short ones.
  2. **Only the phrases are seeded** (3.6: "the seed: the phrases"). The strings always lie where they lie, so the art
     is one drawn sheet and the code reads the same three numbers (`Kantele` and `kantele.py`, change one, change both;
     `ValidateThawStrings` samples the texture along every string the code lays down). One roll in `PlaceSites`, after
     the site, as the seal rolls its stones, so the lake never moves for it.
  3. **The rules of a song** (`Kantele.Fault`): no string twice running (stepping off and back on is not something a
     player reads as a note); the first phrase three different strings with a **leap** in it - two strings not side by
     side - so walking round the lake never sings a phrase back; at least four of the five strings in the song. 600 of
     the 3,125 five-note songs keep them; a roll picks one.
  4. **A string counts where his feet touch it**: the feet's own way since the last frame, tested as a segment against
     the string (12 px either side), so one long frame over it is still a touch and a slow machine tunnels nothing; a
     move of more than 600 px in one frame is a teleport and touches nothing. A touched string is spent until the feet
     are 30 px off it, so standing on one is one note. Only while the ice listens: during the song a touch is ice
     underfoot.
  5. **The way between two strings that are not side by side is the hub**: the strings stop 80 px short of the block,
     and that ring of open ice reaches every gap. `Kantele.Route` is the way a player who has seen them walks - into the
     middle of the gap he is in, in to the block, out beside the next string and across it - and both the probe and
     the validator walk it.
  6. **When it sings.** Within 380 px of the block; past 640 it falls silent and forgets the phrase under way (never a
     phrase sung back). Listening with nothing sung back for 8 s, or with part of it and no right note for 14 s, it
     sings again. All real seconds; nothing reads the pace clock.
  7. **The notes are spread, not stacked.** Stacked thirds, D4 F4 A4 C5 E5, were built first: every step a band of the
     audit and the notes 0.35-0.6 apart, but A4 and E5 measured 0.064-0.089 from three UI tones on their pitches
     (`ui_pause`, `ui_open`'s bell on E5, `ui_back`) through four constructs. A3 D4 F4 C5 A5 is the same D minor seventh
     the hint's song lives in, opened over two octaves; every step is three semitones or more.
  8. **The order is never written.** The pips count and fill with the colours he sang back, never the ones to come.
  9. **The strings are coloured along their length at rest**, not only at their pins: with steel bodies and coloured
     pins the five read as one pale thing on the phone canvas (compared side by side in the build). The body is the
     note's dark edge tone, so the lit string - core, mid, a glow on the light layer, shaking - is far the brighter.
  10. **Drawn layers.** The strings at rest are the lake's child at `GroundTell - 2`, the strings as they sound the
      event's at `GroundTell - 1`: over a spell's craters and pools (they are on the ice; the strings are stretched over
      it), under every body and every slam's tell. The pips are drawn with the sounding strings.

**Verified:** `dotnet build`; the validator with `-- --no-save-writes` (0 errors, the known `test_wizard` warning),
which now runs `RegressionChecks.ValidateThawStrings`: the strings inside the shore, 120 px or longer, pins far enough
apart to pass between; `thaw-strings.png` the lake's size and drawn where `Kantele` lays every string (nine points along
each); the six notes allowlisted and on disk; at least 100 songs keep the rules (600 do) and every one offered keeps
them; 64 rolled songs each sung back by walking `Kantele.Route` in 6 px steps through `StringFeet` - every note touched
exactly once, nothing else touched, all three phrases sung back, the longest whole song under 7,000 px; a wrong first
string empties the phrase and a wrong string in the second keeps the first sung back; a walk round the block at
mid-string, both ways, two turns, sings no phrase back for any of the 64; shuffling on a string rings it once; a
teleport touches nothing; one 400 px step over a string still touches it. Audio: `python tools/audio/analyse.py
--confuse` 0 confusable pairs, and the six against the whole registry 0.111 at nearest (`.ai/audio-manifest.md`,
2026-11-07). `_ExploreProbe --stage=5`, which learns each phrase only from the strings as they light
(`ThawEvent.SungStringAt`) and walks `Kantele.Route` at the player's own walk, feet first: seed 4242 `--fresh-save` 26
passed, 0 failed, twice (song 0 4 1 2 0, 43 s of game time from arrival, 5,298-5,843 px walked, no wrong string, Väinämöinen freed);
seed 7 `--fresh-save` 26/0 (0 2 3 0 4); seed 4242 `--all-unlocked` 25/0 (the chest); seed 31 `--fresh-save
--wrong-string` 27/0 - with the first phrase sung back and one note of the second, string 2 out of turn: the pips
emptied, the ice sang the second phrase again 1.3 s later with the first still sung back, and all three were sung back
(4 singings). `_UiShot --scene=res://scenes/node_2d_game.tscn --stage=5 --terrain-seed=4242` (windowed): the song
mid-phrase (`--goto=event --goto-offset=-120,100 --arrive --hold --frames=150`), mid-solve at 1280x720
(`--sing-pose=1,2`: the second phrase, two of four sung back, the bottom string lit under his feet) and at 540x1212
`--portrait --safe-area` (`--sing-pose=2,3`), and the win (`--win-event --fx-lead=50`: all five strings struck as the
block falls, the line "The ice gives", no arrow). `--sing-pose=P,N` is new for this.

**Not verified:** whether it is fun; **the notes by ear** - nobody has heard the five plucks, the beating doubled
course, the lake's falling chirp after each (`_ICE_CHIRP_AT`, quiet, and the one part that could read as a laser), or
whether the wrong string sounds wrong rather than like a sixth note; whether A3 to A5 (two octaves, an A at each end)
is easy to sing back by ear, or the stacked thirds would have been better despite the audit; whether the five colours
part in a real crowd on a phone, and for a red-green colour-blind player fire, lightning and grass (position and
thickness are the other channels); whether a player finds "step on the strings in that order" from the objective line
and the first wrong string's rattle without being told; the 0.6 s beat, the 8 s and 14 s waits and the 380/640 px reach
against real play; how crowded the hub gets with the guard spawning four every 5 s at 280 px - the probe clears the
swarm every 6 s and teleports through bodies, so it never had to weave. The harness's `--explore` walker stands on an
event's arrow and no longer wins the thaw.

### 3.7 Morgan le Fay - light the lamps ahead

**Was:** `BargeEvent` - meet her barge level under the span; it drifts 2,000 px at 40 px a second while he
keeps within 260 px of it along the bridge; out of reach 6 s and it drifts back.

**The puzzle (decided).** Her barge **drifts only as far as the lamps along the parapet are lit.** Each unlit
lamp ahead is a stop where the barge waits in the dark. **Stand by a lamp to light it** (*first guess* 1 s,
within 60 px). The rhythm is the puzzle: a lit lamp burns for a while (*first guess* 20 s) and goes out, so
lighting every lamp at once fails - light the next two ahead, not the next five, and come back to the barge
when the gate's dogs come down the deck at its level. The bridge's collapse is the clock that already exists:
the barge waits 560 px ahead of the fall.

- **How it reads:** a lamp burns warm and gutters for its last seconds, as the beacons did; the water under
  the barge is lit as far as the lamps reach, so the stretch she can drift is drawn on the strait.
- **Failing:** a lamp that goes out before she reaches it only stops her there until it is lit again.
- **Stays:** the barge, the mooring, the dogs, the landing, the hint, the deed.
- **New:** the lamps (about five along the drift), their states, a lighting sound.
- **Clashes: the Span Lantern**, a planned Bridge secret (`.ai/endgame-castle.md` 3.9), was "linger 2 s, on a
  bastion: light it" - the same act on the same bridge. **Decided: a flame is carried to it instead** - a coal
  taken from a guard's brazier and carried out to the dead lantern on its bastion, three hits knocking it loose
  (`SecretSeed`, the carry primitive), and the collapse then waits 30 s. Not struck, which was chosen first: the
  Gargoyle's Perch already strikes on the Bridge. Carry is free there, and left the events with Tituba's key.
  `.ai/endgame-castle.md` 3.9 and `.ai/achievements-and-secrets.md` 8.12 say so.
- **Honest limit:** of the eight this is the least an insight and the most a rhythm. If it plays flat, the
  lever is lamps that light in pairs across the deck, so which side she drifts is chosen.
- **Size:** S-M.

**Built (2026-11-07, `claude/compassionate-hellman-1d6e08`).** As decided, every first guess taken: a lamp lights after
1 s with his feet within 60 px of its foot, burns 20 s and gutters for its last 5. The particulars:

- **The row.** Five lamps 400 px apart along the parapet on her side, the first beside her mooring, and the sixth is
  her landing - its lamp already lit in her own cold light (wind's ramp, her lantern's colour), so the row reads from
  where she waits, through the warm ones he has lit, to one cold light where she is going. A lamp stands on the deck
  40 px in from the shore, never in a gatehouse's narrows, never within 120 px of a piece's end, and never where a
  broken span's hole is beside the parapet (its foot and the deck he lights it from both clear the hole by its radius
  and 100 px); a lamp that would stand there steps on along the bridge (`BargeEvent.LayLamps`, `LampMayStand`), and a
  mooring whose row would then land her past the span's end is drawn back along the span. Over 64 seeds 23 lamps of
  the drift stepped past a hole; a stretch is then longer than 400 px, which only changes when to light its lamp.
- **The rule** is one pure class, `BargeDrift`, that the event and the validator both step. She sets out from her
  stop the moment its lamp burns (that counts) and drifts on to the next at 40 px a second; at a dark lamp she waits in
  the dark. Lighting is only for the lamps of her stretch (her stop to before her landing), and only a dark one: a
  burning lamp is not topped up, so lighting early is never free. After five lit set-outs she is in at the next lamp
  and comes ashore when he is level with her (within 260 px along the bridge, as before). The first lamp is beside
  her as he arrives, so the one action is learned on the first try: stand there, it lights, she moves.
- **The fall (open in the design, decided here).** She never lets the span's fall within 560 px of her (the old
  `RestLead`). Waiting at a dark lamp with it that near, she is **borne on** past the lamp at the fall's pace; that
  lamp does not count, so her landing moves on one lamp, and every set-out she had stays counted - a wrong move costs
  time, never progress, and nothing locks. Under way she outruns it. Before he comes the same rule moves her off a
  mooring the fall reached. So the row runs on to the castle wall (only her stretch is ever drawn; a lamp the landing
  moves onto fades up out of the mist), and at least five lamps stand past the furthest the fall can bear her.
- **How it reads.** The lamp: an iron standard on a stone plinth with a four-sided lantern, ten frames
  (`tools/art/barge_lamps.py`: dead, kindling, four burning, two guttering, two of hers). On the floor round each lamp
  of her stretch, a ring that fills as he lights it, and round a burning one its clock running down, hot then the
  edge's red once it gutters. On the light layer, each burning lamp's flame and its pools on the deck and the water,
  and **the lit water**: from her to the first dark lamp at or past her stop, the stretch she can drift, drawn on the
  strait in warm light - a dark lamp visibly ends it. Her lantern's ring fills toward her landing. The reach's two
  dashed lines are drawn only once she is in and he is not level with her.
- **Its words.** "Light the lamp beside the barge" / "Light the lamps ahead of the barge (n more)" / "The barge waits
  in the dark - light its lamp" / "The fall drives the barge on - light the lamp ahead of it" / "The barge has come in
  - meet it at its landing". The arrow points at the lamp she waits at, at her landing, or at her when he is far
  from her - never at the next dark lamp while she is under way, which is the puzzle. The deed keeps `the_barge` and
  is called *The Way Lit*. Schema: none.
- **Stays:** the barge, its art, its track and mooring, the dogs (a pair from 1,050 px ahead of her every 7 s while
  she is out), the hint, the reward. **Goes:** the reach she had to be kept within, the 6 s grace, the drift back.
- **After the win** the lamps stay standing, embers where one burned, and go with the deck when the fall reaches them
  (`BridgeRules.KeepWithDeck`), as the parapets do.
- **Proven.** `ValidateBarge` lays 64 seeds' rows and plays each on paper with `BargeDrift`: the next two ahead
  (never one she would not reach before it burnt out) lands her in 69 s at most and never leaves her in the dark;
  the whole stretch lit in one run ahead, then waiting at her landing, never lands her - a lamp goes out before she
  reaches it; the rhythm from the tightest arrival lands her and the fall never comes nearer than 560 px; and left
  idle after her first lamp, the fall bears her past dark lamps, her landing moves on one for each and her one set-out
  is kept. Breaking the rhythm on purpose (a 32 s burn) raised both warnings. `_ExploreProbe --stage=11` plays it
  walking (the final runs, on the final code): `--fresh-save` 25/25 (in at lamp 5 52 s after arriving, never in the
  dark, Morgan freed), `--all-unlocked` 25/25 (the chest), `--light-all` 26/26 (the row lit by 11.5 s; lamps 3 and 4
  went out ahead of her; she waited in the dark at lamp 3 with three set-outs, kept when he relit), `--let-fall
  --bridge-front=-14000` 26/26 (borne past two dark lamps, landing lamp 5 to 7 past the gatehouse, the fall's nearest
  560 px), `--arrive-late --bridge-front=-6000` 27/27 (borne three lamps before he came, met at lamp 3, in at lamp 8).
  The probe allows her lead one frame of the fall (it reads the fall on its own frame, which can come before the event
  has pushed her on that frame: 554 px was read once at a 0.2 s frame), and `--seed=2` 25/25 (moored on span 2's north-west
  side, its fourth lamp stepped 740 px past the hole onto open deck, never in the dark). The first `--arrive-late` run
  is why the probe waits a stride short of a lamp she will not reach before it burns out: lighting "the next two" lit
  the lamp past a gatehouse too early and it went out ahead of her - the puzzle working, and now the paper player's
  rule too. `_UiShot --stage=11 --terrain-seed=4242 --notes-seen=all --arrive --lamps-lit=1 --goto=barge:280,60
  --goto-offset=0,0 --hold --frames=150` photographs it mid-solve at 1280x720 and at the phone's 540x1212 `--portrait
  --safe-area` (the lit band was first drawn as a dotted line of glows and was made one band); the arrival (the arrow
  over the lamp beside her) was photographed too. The win beat is the old barge's, unchanged, and was not retaken.
- **Not built:** a bespoke lighting sound (a lamp catching is the beacons' fire cast, going out their wind cast; a
  tools/audio ask), and the honest limit's lever (lamps in pairs across the deck).
- **Needs eyes:** whether the lit water reads as "she can go this far" at a glance in a crowd, above all on a phone,
  where the barge and the lamp ahead are rarely on one narrow canvas; whether the guttering is seen in time to matter;
  and whether it plays as a rhythm or as busywork - nobody has walked it under the bridge's horde.

### 3.8 Tituba - slip past the lanterns

**Was:** `GaolersKeyEvent` - reaching the cell wakes a Warden beyond it carrying the key; kill it, carry the key
back; three hits while carrying knock it loose.

**The puzzle (decided).** The Warden and the key stay. **The way home is swept by lantern light**: the gaol's
lantern-bearers walk their rounds of the corridors, each throwing a cone of cold light ahead. **Caught in a cone
while carrying the key, the key drops where he stands and that bearer turns toward him** (decided); go back for
it. The three-hit rule goes. The insight is watching the rounds, waiting in a side passage, and slipping through.

- **How it reads:** the Dungeon is dark (`.ai/light-pool.md`), so the cones are the brightest thing on screen:
  cold, his colour, drawn in `Air`. The cone is a `TelegraphShape.Cone`, and `GroundSlamAttack.Contains()` is
  the single authority on what it covers, so what is drawn is what catches (`CLAUDE.md`, "Enemy attack
  patterns"). Walls stop the light (`MazeNavigation.IsSolidAt`).
- **The problem to settle first:** an auto-shooter kills whatever its spells reach, and a killable lantern-bearer
  is a puzzle the loadout solves for you. Either the bearers are **untargetable** (`Enemy.SetTargetable(false)`,
  the primitive every burrower rides on), his own jailers under his ward; or **the lanterns hang on the walls and
  sweep**, with no body at all, and a catch wakes the nearest Warden instead. **Decided: untargetable bearers**,
  which keeps "that bearer turns toward you". They are never in `"enemies"` while they walk their rounds, so no
  spell aims at them, and they deal no contact damage; the bearer who turns is the one thing that comes for him.
- **The seed:** the rounds are corridor loops on the maze grid between the Warden's site and the cell, placed so
  a gap in the sweep always exists.
- **Stays:** the cell, the cry, the Gaoler's map, the Warden and its key, the deed. **Goes:** the three hits.
- **New:** a patrol on the maze grid, the cone with wall occlusion, the bearers' lanterns (a sprite variant or a
  prop), the catch sound.
- **Clashes:** the Oubliette (untouched on a grate) and the planned Alarm Bell are different acts. Carry
  disappears from the events; the First Tree secret still carries.
- **Size:** M-L.

**Built (2026-11-08, `worktree-agent-ad4f992193e0573e9`).** `GaolersKeyEvent` (rewritten), `GaolRounds.cs` (new: `GaolGround`
the lattice as the rounds read it, `GaolRound` one round, `GaolLayout` the seed and its proof, `GaolWalk` the walk graph,
`GaolSweep` what a round lights, `GaolSlip` the time-stepped search, `KeyCarry` the key's rule), `LanternCone` (new: the
cone), `LanternBearer` (new: the bearer). The lantern is `gaol-lantern.png` from `tools/art/gaol_lantern.py` (new); the
catch is `lantern_catch.wav` from `tools/audio/sfx.py` (`.ai/audio-manifest.md`, 2026-11-08). Site, deed id, hint,
reward, the Gaoler's map, the cell's art and its win beat are as they were; the deed's line now reads "Slip the Gaoler's
key past his lantern-bearers to the locked cell in the Cursed Dungeon." Schema: none. The three hits and the
`DamageTaken` listener are gone.

- **What plays.** Reaching the cell wakes the Warden at its post, four to six cells' walk away (1,536-2,304 px of
  corridor), on the far side of the cell from the stage's start where the maze allows. Between the post and the cell,
  one to three lantern-bearers walk their rounds: each a loop of four to twelve cells crossing or running beside the way
  home, walked centre to centre at 80 px/s, its lantern held out in front and throwing a cone of cold light 300 px ahead,
  30 degrees either side, stopped by the walls. Its lantern turns into a corner half a second before its feet do
  (`GaolRound.LookAhead`), so a turn is seen coming. The bearers walk from the chapter's first seconds - dormant too - so
  their lights are moving in the corridors round the cell before anyone comes for it; their light catches nothing until
  the key is carried. Kill the Warden; the key falls where it fell. Carry it home: caught in a cone, the key drops where
  he stands, the catch sounds (`lantern_catch`), and THAT bearer turns on him. Take it back when the light has passed
  and slip home; the lock turns as before (the unlock, the gate, Tituba). As the key turns, every lantern gutters out.
- **The Warden keeps its post (decided in the build).** A Warden that chased him, as the old one did, would come to him
  at the cell and drop the key at the door, with no rounds between. So it holds its post (`Enemy.ScriptedHeading`'s
  hold), leaves it for him only once he is within 640 px of it (`GuardRadius`), never follows him past 520 px from it
  (`LeashRadius`) and walks back by the corridors when he draws it further. The key always lies on the far side of the
  gaol. In every probe run it fell 8-110 px from its post.
- **The cone: what is drawn is what catches.** `LanternCone`'s shape is a `GroundSlamAttack` with `TelegraphShape.Cone`,
  and `Contains()` is asked first, always. The walls are laid on once, as a reach table: 25 rays fanned edge to edge,
  each marched out in 12 px steps to the first wall. The light layer draws that fan (a triangle a pair of rays, bright at
  the lantern and thinning to the far end, a brighter rim where the light lands on the floor or meets a wall), and
  `Covers` reads the same table, taking the shorter of the two rays either side of a point. So nothing is caught outside
  what is drawn; where a wall's corner falls between two rays the fan reaches a sliver further than the catch (one ray's
  gap at most, 13 px at the far end), and the sliver is the player's. A catch is tested at his feet
  (`Player.FootPosition`) against the light he saw - the event reads the cones before the bearers move that frame.
- **Untargetable, by not being an enemy at all (decided: untargetable; how, decided in the build).** A bearer is a plain
  `Node2D` the event walks: no group, no health, no collision shape, nothing on any physics layer. No spell can aim at it,
  no area finds it, and touching it is not contact damage - and no other code path can ever make it a target, which
  `Enemy.SetTargetable(false)` on an enemy would only promise as long as everything respected it. The probe checks every
  frame that none is in `"enemies"` or the registry.
- **The bearer who turns - "the one thing that comes for him" (decided in the build).** Its round stops; its lantern
  flares white-hot (frame 0 of the sheet) and swings onto him; while its light is on him it comes after him down the
  corridors - the swarm's own flow field, which is built toward him - at seven tenths of his walk, so walking away always
  loses it, and if it reaches him (60 px, feet to feet, its light on him) it strikes him with the lantern, 4 damage
  before the chapter's scale (5 in the Dungeon), once a second, the first only 0.7 s after it turned. **It does not see
  round corners:** the moment he is out of its light it walks to where it last saw him and stands there swinging its
  lantern, looking; catch him in it again and it comes on. Out of its light 2 s, or 10 s after it turned whatever he
  does, it gives up and walks back by the corridors to the nearest cell of its round at one and a half times its round's
  pace, its light thrown ahead of it and live again (it is on duty: a key carried into it is caught), and takes its
  round up there. So the way to lose it is the puzzle's own move: step out of the light into the dark, and wait. The
  first build turned its lantern on him wherever he was, through walls; a corner never lost it, only outwalking its
  300 px reach did, at 44 px a second, and the probe's `--into-cone` on seed 31 ran its ten seconds out doing that. Even
  seeing only what its light falls on, in that seed's long straight corridors with no side passage near the catch,
  outwalking it took 8.6-9.8 s against the ten-second cap, so it walks at seven tenths of his pace now (was four fifths)
  and gives up after 2 s out of its light (was 2.5).
  Nothing is ever lost but the key's place and the time. While a bearer is turned the line under the timer says
  "A lantern-bearer has seen you - get out of its light".
  Not being an enemy, it has no Weaken to scale its blow (nothing can hit it to put one there): the chapter's damage
  scale is its only multiplier, and `Player.TakeDamage` runs the dodge and every mitigation as for any hit.
- **Taking a lost key up.** A key that fell in the light cannot be taken up until he has stepped 70 px off it
  (`KeyCarry.RearmRadius`); taken up again inside the light, it falls again on the same frame. Without the first rule,
  standing on it in a cone would pick it up and lose it every frame.
- **The seed (`GaolLayout.Generate`).** The post: a cell four to six steps from the cell, on the far side from the start
  where one exists. The rounds: the shortest loop through each open passage near the way (four to twelve cells, never
  through the cell or the post), those that cross the way first and short before long; up to three that share no cell.
  Each roll is kept only if the cautious walker below slips through at four phase sets (its own and three a turned
  bearer could come back in); of those, the roll that bites a straight walk home hardest, a second round counting for a
  quarter of a bite. Over 40 layouts of real maze ground: 12 with one round, 18 with two, 10 with three; none with none;
  a straight walk home at a random moment caught 57% of the time on average, and 10 of the 40 (a quarter) under the
  50% target - the maze round those cells has no second loop clear of the first. Built at 60-90 ms a layout on a desktop
  release build, off the main thread (below).
- **The proof (`RegressionChecks.ValidateGaolersKey`).** The walker of `GaolSlip` walks the walk graph - cell centres and
  a point every 24 px along each open passage, the middle of the corridors - one point every 0.12 s (200 px/s against his
  220), may stand still wherever it is dark, picks the key up only in the dark, and is home once it has stood at the
  cell's door, dark, for the lock's 1.2 s. It plans against a lantern 24 px longer and 4 degrees wider than the bearers'
  (`GaolSlip.PlanningCone`), so a way it finds keeps a margin of dark. The bearers' places on their loops are quantised
  to their 9.6 px tick, 40 to a cell, so every loop is a whole number of ticks. The validator: (1) the numbers - a bearer
  well under his walk, turned and walking back too, the proof's walker under it, the ticks whole, the dwell as long as
  the lock; (2) on real maze ground, up to 160 lanterns aimed every way and 120 points round each: nothing caught outside the drawn fan, nothing
  drawn lit and more than a ray's gap inside it spared, nothing caught behind a wall; (3) a straight walk through a cone
  with the key drops it on the first lit step, where he stood, not to be taken up until he steps off; taken up again in
  the light it falls again at once; a walk behind the lantern, or behind a wall it lights up to, drops nothing; (4)
  twelve gaols laid on maze ground exactly as the event lays them (walls baked, then nothing else read - any question
  outside the bake is counted, and must be none), each a closed set of disjoint loops off the cell and the post, the post
  four to six steps out, and each searched again at eight phase sets the generator never tried, along the way home and
  its side passages ONLY (`GaolLayout.WayHeKnows` - the shortest way, and each cell beside it as a dead end to stand in,
  nothing round): every one must slip through. 96 of 96 do, 49 of them by standing in the dark with the key while a light
  passes. A one-off sweep of the same over 40 layouts and 12 phase sets each: 480 of 480, 238 standing in the dark, 54 of
  those in a side passage. And three quarters of the gaols must bite a straight walk home at least half the time.
- **Off the main thread.** All in one frame the gaol cost 313 ms on a desktop debug build. Now the walls round the cell
  (the cells within eight of it and one round them, about 23,000 tiles) are read a slice a frame (`GaolGround.BakeStep`,
  3,500 tiles, seven frames, 9-112 ms in all, at most a chunk or two of the streamer's data made in any one frame), and
  the rest runs on a worker that reads nothing but that array (`GaolGround.BakedOnly`); the bearers step out when it is
  done, about a quarter to two and a half seconds into the run, five chunks from the player. A player who arrived first
  would wait for it in `Begin`; no real one can. The lanterns' cones aim against the same baked array every frame.
- **Light.** The cones are drawn additively on `DrawLayers.Air` (`ZAsRelative` false), in his arcane cyan, the turned one
  whiter; over every body and under only icons and numbers, so in the Dungeon's dark they are the brightest thing on
  screen. Two of `LightPool`'s cold lights a bearer (at the lantern, and halfway down the cone) lift the room's dark under
  them so the floor is lit, not only washed. A dropped key has a faint pool of the event's own cold round it - never
  warm, which is a secret's tell.
- **The bearer's body.** The Warden's own sheet (`WardenEnemyFrames`, the chapter's jailer) at x2 against the Warden's
  2.7 and its elite size, tinted cold - a lesser turnkey of the same gaol - with the lantern (a steel cage round the
  jail's cold flame, four frames: a flare and three flickers) held at its hip on the light layer.

**Verified:** `dotnet build` (0 errors, none of the warnings in these files); the validator with `-- --no-save-writes`
(see the report); `ValidateGaolersKey` as above; `python tools/audio/analyse.py --confuse` 0 confusable pairs.
`_ExploreProbe --stage=1` hunts the Warden from beside it at its post, then plays the slip as a player who has watched the
rounds - each bearer's loop and its place on it, both on screen, never more - planning on the way he knows (his own way to
the key and from it home, and the side passages off them; the rest of the region only if that holds no way) with the
planner's cautious lantern, walking it feet first at 200 px/s, the swarm cleared only every 5 s. On the final code: seed
4242 `--fresh-save` 27/0 (two rounds of six; slipped home in 9.5 s, no catch), seed 31 `--fresh-save` 27/0 (rounds of ten
and four, kept as the best of its rolls at a 46% bite; 9.7 s, no catch), seed 7 `--fresh-save` 27/0 (three rounds of four;
stood 9.6 s in the dark with the key while the lights passed, then slipped through; no catch); each frees Tituba. Seed 7
again at `--timescale=1`, 27/0 (8.8 s standing in the dark with the key, no catch). Seed 4242 `--all-unlocked` 26/0 (the chest; no catch). `--into-cone` (with the key
in hand, into the nearest lit cone on purpose): seed 4242 31/0 - the key fell exactly where he stood, that bearer's lantern
swung to within 2 degrees of him, it left its round and followed him 797 px (to 50 px of him), lost him round a corner and
gave up 6.8 s after the catch, was back on its round at 14.7 s, and he took the key back and slipped home with no second
catch; seed 31 31/0 - caught in a long straight corridor with no side passage near, it followed him and its ten seconds
ran out before the probe was out of its light 2 s (a traced run caught elsewhere on the same seed lost it round a corner
in 6.3 s), back on its round 20.7 s after the catch, the key taken back and slipped home with no second catch. Which of
its two endings ends a chase depends on where it is: the probe's retreat (out of the hunter's light and sight, never past
it) is a heuristic, and a player may well do better or worse.

The probe found three things only it could, each fixed in the probe and none in the rule: at timescale 4 a frame is up to
0.4 s of game time, and it stepped over the key without ever standing within its 30 px (the frame that passes a planned
pickup now stands on it); it stood him for the end of the frame while the event tests the cones aimed on the frame
before, one frame ahead of the light - and at 24 fps under load seed 7 was caught once that way (it now stands him where
the plan says for the moment the bearers are at); and its retreat would not cross the light it stood in, so it fled
straight down a lit corridor. And it found one thing in the rule: the first turned bearer tracked him through walls, and
on seed 31 a corner never lost it - so it no longer does (above).

**Not verified - needs eyes:** whether it is fun, and whether a player finds "wait for the light to pass" from the line
under the timer ("keep out of the lanterns' light") and the first catch without being told; whether the cones read at a
glance in a full swarm on a phone, and whether the bearers - the Warden's sheet, smaller and colder, with a lantern - are
ever taken for the Warden while he hunts it; the catch by ear; the blow's 5 against real play; whether a one-round gaol
(about three in ten) plays as a puzzle or as a timing; the cost of three cones' 25 triangles each and their marches on a
phone (the marches read the baked array). Photographed only standing: `_UiShot --scene=res://scenes/node_2d_game.tscn
--stage=1 --terrain-seed=4242 --fresh-save --goto=event --goto-offset=500,120 --arrive --hold --frames=300`, at 1280x720
and at 540x1212 `--portrait --safe-area` - two bearers on their rounds between the cell and the post, each cone the
brightest thing on the dark floor, one cut off by the wall beside it; neither the catch, the chase nor the won cell was
photographed, and there is no `_UiShot` pose for them yet.

---

## 4. The asks after the change

No two events share an ask, and no event shares one with a secret in its chapter.

| chapter | event | ask before | ask after |
|---|---|---|---|
| Enchanted Forest | the grove | stand still | stand still |
| Enchanted Forest | the walking hut | hunt | **herd** |
| Fallow Fields | the well | defend | defend |
| Fallow Fields | the Tithe-Reeve's scale | pay | **weigh** |
| Broken Circle | the moot | follow | follow |
| Sunken Cave | the nine mouths | listen | listen |
| Blighted Swamp | the lanterns | explore | explore |
| Blighted Swamp | the gibbet's cage | escort | **lead** |
| Mystic Ruins | the Five-Pointed Seal | solve | solve |
| Frozen Waste | the thaw | fight here | **sing back** |
| Scorched Sands | the caravan master | catch | catch |
| Scorched Sands | the dry tomb | kindle | **aim** |
| The Emberdeep | the great anvil | bait | bait |
| The Last Bridge | the barge | keep pace | **light ahead** |
| Cursed Dungeon | the Gaoler's key | carry | **slip past** |
| The Court | the seer's voice | seek | seek |
| The Court | the dotted door | trace | **draw in one stroke** |
| The White Downs (far shore) | the Silver Hind | - | **stalk** (built with the chapter, F3: `.ai/second-half.md` 9, "F3, built") |

---

## 5. What stays, and the playtest

Seimei's stones, Wayland's bait, the Sibyl's mouths and Nostradamus's voice already meet most of section 1's
bar. Their open question is the one in every Built note: whether a player finds the rule without being told.
Eric is arranging a playtest with someone who has never seen them, and **those four, with Wayland, Seimei and
the Sibyl first, are what it should try** - not Baba Yaga or Imhotep, which this doc replaces.

---

## 6. Order and cost

Smallest change, and most doubt about the old ask, first:

1. **Paracelsus** (S-M): mostly a removal, and a pilot for "a hazard that hurts only him".
2. **Medea** (S-M): one more pan of frames and the tilt.
3. **Ma Liang** (M): the new enemy-only collision layer is the risky part; check every mask.
4. **Baba Yaga** (M): the yard and the cornered test on the existing mover.
5. **Väinämöinen** (M): five ice strings, five notes, the phrases.
6. **Morgan** (S-M): the Span Lantern is carried to now, so the two no longer meet.
7. **Imhotep** (M-L): the flagship, the laser puzzle in our world.
8. **Tituba** (M-L): the patrol, the occluded cone and the untargetable bearers.

Each is one branch and one worktree, built and verified as the events were (`.ai/chapter-wizards.md` section 7):
`dotnet build`; the validator with `-- --no-save-writes`; `_ExploreProbe --stage=N` solving it on fixed seeds,
fresh and `--all-unlocked`, taught the new rule; a regression check that every generated layout is solvable on
paper and that the wrong approach fails (a straight chase for the hut, the wrong start for the door); `_UiShot`
of the puzzle mid-solve and won, at both canvases.

**Schema: none.** The sites, deeds, rewards and hints keep their ids, so a save that freed a wizard keeps him,
and the event that freed him simply asks something else of a new save.

**What no harness checks:** whether each is fun; whether it reads at a glance in a real crowd on a phone;
whether a player finds the rule from the objective line and the world alone.

---

## 7. Decided by Eric after the first draft (2026-11-03)

1. **Imhotep:** mirrors face eight directions.
2. **Ma Liang:** the ink-walls are a new collision layer, met by enemies only.
3. **Morgan:** the Span Lantern stays a secret, but a flame is carried to it from a guard's brazier, not
   lingered at (struck was chosen first and dropped: the Gargoyle's Perch already strikes on the Bridge).
4. **Tituba:** the lantern-bearers are untargetable.
5. **Väinämöinen:** kantele strings, not stones.

What is left open is small and in each block's **Open** line: counts, widths and first-guess numbers for the
build to tune.

## Sources

- How each Halls of Torment stage puzzle works, the Chambers of Dissonance's included:
  https://steamcommunity.com/app/2218750/discussions/5/7179373038239950353
- A player who had not found the beams after ten runs:
  https://steamcommunity.com/app/2218750/discussions/0/4692279345903701999
- The wider comparison (Vampire Survivors' coffins, relics and secrets; Halls of Torment's quests and rescues)
  was made in chat on 2026-11-03 and is not kept here; what came of it is section 2 and
  `.ai/passives-and-items.md` 8k (John Dee and the chests).
