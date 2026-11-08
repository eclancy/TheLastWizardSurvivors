# Unlock puzzles: every rescue a puzzle you can see

Eric, 2026-11-03: *"what i actually wanted to discuss was making sure all of the character unlocks are fun
and interesting, like the laser puzzle in halls of torment"*.

**Status: designed with Eric, 2026-11-03. Built: 3.3 Baba Yaga, 3.4 Paracelsus, 3.5 Medea, 3.6 Väinämöinen and
3.7 Morgan le Fay (each its "Built" note); the other three are not.** Every rule below that says "decided" was
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

### 3.2 Ma Liang - draw the door in one stroke

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
