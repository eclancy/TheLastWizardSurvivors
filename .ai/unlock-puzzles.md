# Unlock puzzles: every rescue a puzzle you can see

Eric, 2026-11-03: *"what i actually wanted to discuss was making sure all of the character unlocks are fun
and interesting, like the laser puzzle in halls of torment"*.

**Status: designed with Eric, 2026-11-03; nothing built.** Every rule below that says "decided" was
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

- **Facing snaps to eight directions** (*first guess*), so the beam is a readable line and a solution is exact.
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
- **Open:** eight directions or four; how many mirrors; whether a leg may cross another.
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
  is the honest wall; steering is cheaper and leaks. *Recommended:* the layer.
- **Clashes:** none. No other ask draws.
- **Open:** the figure family; how long a line counts as "walked along" before it smudges (*first guess* 40% of it).
- **Size:** M.

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

### 3.6 Väinämöinen - sing it back

**Was:** `ThawEvent` - damage dealt near the block on the frozen lake thaws it; fire counts triple.

**The puzzle (decided: it replaces the thaw).** Round the block stand **five stones, each with its own note**.
The ice sings a phrase: stones light and sound in turn. **Step on them in the same order.** Three phrases, of
three, four and five notes (*first guess*: each one the last with a note added, which is kinder to memory). A
wrong stone replays the phrase; nothing is lost but the time. Each phrase sung back takes the block one thaw
stage further (it already has four), and the last frees him. The legend's singer, who sang his rivals into the
bog; and his hint was always "Someone is singing under the ice."

- **How it reads:** each stone lights in its own colour as it sings, and again under his feet when he gets it
  right, so it works with the sound off.
- **The swarm:** the stones stand round the block (*first guess* 200-260 px out), so the phrase is sung on the
  run through the crowd. An enemy on a stone does not count; only the feet.
- **The seed:** the phrases.
- **Stays:** the lake and its shader, the block and its stages, the hint, the deed. **Goes:** the heat, fire's
  triple, the window of clear ice as the place where fighting counts.
- **New:** five stones (or strings: he made the first kantele), their lit frames, five notes in one voice,
  checked with `python tools/audio/analyse.py --confuse`.
- **Clashes: the Broken Ward** (the Ruins' vault) also asks for stones touched in order. It is read from their
  notches; this is remembered from a phrase. Draw them nothing alike, and keep the order from ever being written
  on the stones.
- **Open:** stones or kantele strings; whether a phrase extends the last.
- **Size:** M.

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
- **Clashes: the Span Lantern**, a planned Bridge secret (`.ai/endgame-castle.md` 3.9: "linger 2 s, on a
  bastion: light it and the collapse waits for 30 s") is the same act on the same bridge. It is not built; it
  must change its verb or go before this is built.
- **Honest limit:** of the eight this is the least an insight and the most a rhythm. If it plays flat, the
  lever is lamps that light in pairs across the deck, so which side she drifts is chosen.
- **Size:** S-M.

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
  sweep**, with no body at all, and a catch wakes the nearest Warden instead. *Recommended:* untargetable bearers,
  which keeps "that bearer turns toward you" as decided.
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
3. **Ma Liang** (M): after the collision-layer question (3.2) is settled.
4. **Baba Yaga** (M): the yard and the cornered test on the existing mover.
5. **Väinämöinen** (M): five stones, five notes, the phrases.
6. **Morgan** (S-M): after the Span Lantern's fate is decided.
7. **Imhotep** (M-L): the flagship, the laser puzzle in our world.
8. **Tituba** (M-L): after the targeting question (3.8) is settled.

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

## 7. Open questions for Eric

1. **Imhotep:** mirrors facing eight directions or four.
2. **Ma Liang:** the ink-walls as a new collision layer (an honest wall) or as steering (cheaper, leaks).
3. **Morgan:** the Span Lantern secret changed or dropped, since it is the same act.
4. **Tituba:** untargetable lantern-bearers, or lanterns that hang on the walls and sweep.
5. **Väinämöinen:** stones or kantele strings.

## Sources

- How each Halls of Torment stage puzzle works, the Chambers of Dissonance's included:
  https://steamcommunity.com/app/2218750/discussions/5/7179373038239950353
- A player who had not found the beams after ten runs:
  https://steamcommunity.com/app/2218750/discussions/0/4692279345903701999
- The wider comparison (Vampire Survivors' coffins, relics and secrets; Halls of Torment's quests and rescues)
  was made in chat on 2026-11-03 and is not kept here; what came of it is section 2 and
  `.ai/passives-and-items.md` 8k (John Dee and the chests).
