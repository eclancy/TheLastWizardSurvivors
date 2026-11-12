# Boss phases

**Status (2026-11-11):** Eric, on reading it: *"These look great"*. Elderbark's phases approved as written; the
other bosses confirmed one at a time as each is built (`.ai/in-flight.md`, "Week of 2026-11-16: the plan"). The three
questions at the end are answered (Eric, 2026-11-11: "Yes, no, show cracks"): the beat leaves the swarm and spells
running; no single hit skips a phase; a breakable part shows cracks on its art, never a bar of its own.

> Designed 2026-11-11 for Eric's review, **one boss at a time**. Nothing here is built. It is in-flight item 134, and it
> folds in Elderbark's items (99, 115, 116, 117, 123), the Hayward's (129, 130, 131, 133) and 132 (every boss shows its
> damage on its sprite).

Eric, 2026-11-11: *"Let's think about how each boss fight can be made more dynamic and interesting, so it's not just an
HP slog."*

## At a glance

| Boss | The change that defines the fight |
|---|---|
| Elderbark | His bark splits, dark sap falls like meteors, then he takes root |
| The Hayward | Break the hurdle and he fights without it: fast, unguarded, never still |
| The Ninth Stone | It raises a circle round itself, and you go in and out through a gap that turns |
| The Hollow Choir | Revives stop: each voice that falls passes its song to the others |
| Mother Rot | She calls her spawn home to heal her, and you race them |
| The Archivist | It writes down your path and reads it back as fire |
| The Still Warden | Crack its shell from close in, and the still thing walks |
| The Long Coil | The sand drains into a pit and drags you toward its mouth |
| The Warden of the Deep | Kept as a recap of the road. A plate of armour falls at every change |
| The Constable | He cuts the gate down behind him and makes his last stand in the yard |
| The Gaoler | His horse runs on without him, then he gets back on it |
| The Seneschal | He stops running and shuts the doors |
| The dark wizard | Already built in phases. Adds the beats and his damage |
| The Unstrung Hunter | Already built in phases. Adds the beats and his damage |

---

## 1. The framework

**What a phase is.** A phase is a point where the fight changes **what the player has to do**. A health threshold
starts it (`BuildPhaseThresholds`, as today), or a part breaking does (the Hayward's hurdle). Thresholds still only go
forward. If a phase only adds speed or damage, it is an enrage. An enrage can ride along with a last phase, but it is
never a phase on its own. Today, five bosses end on nothing but an enrage (Elderbark, the Gaoler, the Archivist, the
Still Warden, the Long Coil), and each one gets something to ask below.

**The change is a short beat, not a spike.** The beat lasts about 1.2 s. The boss plays its transformation (bark
splitting, the hurdle bursting). It is untargetable and does not attack (`SetTargetable(false)`, as in the arrival).
The beat has a shake and a sound, and no words on screen. Unlike the arrival, **the field is not held**: the swarm keeps
moving, and the spells turn on it for that second. Anything already in the air still lands where it was shown, and
nothing new starts. The new phase's first attack gets its full tell. The Gaoler's dismount `Prime()` stays, but only
after the beat.

**No hit skips a phase.** Damage past the next threshold is lost. The Seneschal already works this way
(`CourtSeneschalBoss.AdjustIncomingDamage`). It means a huge build still sees every phase, at a cost of a second or two
each. The boss bar marks every threshold with a notch, so the player knows a change is coming.

**Breakable parts.** A part has its own thin bar, under the boss bar, shown only while the part stands. Working on it is
the player's choice. Every spell aims itself, so he makes that choice by **where he stands**:

- The Hayward's hurdle takes the hits it blocks from the front.
- The Still Warden's shell takes only hits dealt from inside its claim.

Breaking a part starts its phase early, and it pays off, because the body behind the part is more exposed. A part also
breaks at a backstop health share, so a player who ignores it still sees the whole fight. A part's health is a share of
the boss's health, so the Boss Rush's 35% scales it too.

**The swarm.** As today, the field drains while a boss is here. The trickle, formations, elites, the Warden and chests
all stop (`bossFightActive`), and whatever is alive is left where it is. A boss's adds are its own: drawn from its
chapter's cast, capped at 6-8 alive, and each one exists for a phase's ask. Elderbark's saplings pull the aim of every
auto-targeting spell. Mother Rot's spawn race back to her. No add is filler.

**Eric's standing rules:**

- **Wind-ups are animations.** Either the boss's body shows it (item 133) or the attack grows into its area (item 116:
  roots creep, then spike). Never a flashing shape on the ground. A faint edge may stay where a packed swarm or the
  phone needs it. Every animation is drawn from the same `Contains()` that the hit reads.
- **Damage shows on the sprite (132).** Each boss gets one damage state per phase or part.
- **Readable in a crowd.** Every attack must read in a packed swarm on the 540x1212 phone canvas.
- **Honest damage.** All damage goes through `ScaleOutgoingDamage`. No phase heals the player or hits him without a
  mark first (he has 50 HP and almost nothing heals it back: `.ai/difficulty-curve.md` 5).

**What stays:**

- The arrival: 2.5-4 s, with the field held.
- The Boss Rush at 35% (`FightHealthShare`). Thresholds are fractions, so every phase plays in the rush.
- `ValidateBossPatterns`, which also has to check: the beat's length, the part's share, a backstop that falls between
  two thresholds, and a non-zero tell on every new attack.
- `_BossProbe`, which also has to be able to break a part.
- Endless mode: a returning warden replays its phases, with its parts whole again.
- The 3-5 minute fight length. Re-measure it after each boss, and change health, not phases.

---

## 2. The bosses, in campaign order

### Elderbark, the Treant - Enchanted Forest (chapter 1)

**Today:** a slow chaser with one ring slam (280 px, 1.3 s tell). At 0.66 a line of roots races from him through the
wizard. At 0.34 he enrages.

- **Old growth** (from the start to 66%)
  - *Changes:* the slam's tell is cracks spreading through the ground from his feet (116). The root line runs from the
    opening: roots creep along it, then spike (116). Item 115 makes both bigger again. Any hit that lands roots the
    wizard for under a second, so the slow tree catches up (117).
  - *You:* step out of the cracks and off the line. Getting caught costs time, not just health.
- **The bark splits** (66%)
  - *Beat:* he rears. Slabs of bark crack off and fall round him, each with its shadow, and the splits glow with dark
    sap.
  - *Changes:* from the splits he spits dark sap in high arcs, three to five globs at and round the wizard. You see each
    one in the air, with its shadow growing where it will land. One glob in three sprouts a sapling (123, at most six
    alive). Saplings walk at you and pull your spells off the tree.
  - *You:* keep moving between the falls while the tree closes in, and decide whether to clear the saplings.
- **He takes root** (33%)
  - *Beat:* he drives his roots into the ground and stops walking for good. The trunk splits open on the heartwood,
    which takes x1.25.
  - *Changes:* root lines break out from two or three sides of the wizard at once and race in at him. The sap falls
    faster.
  - *You:* stop kiting a walker. Stand in reach and burn him down while you dodge lines that come from every side.
- **Shown:** three sheets: whole bark, bark split with sap-lit cracks, and the heartwood open.
- **Needs:**
  - Art: the cracks-and-roots wind-up, bark slabs, a sap glob and its splash, a sapling in the Forest cast's style, two
    damaged sheets, and the two beats.
  - Code: the beat, damage states, the falling hazard, the rooted state on the player (117), the sapling add, and
    converging root lines.
- **Size: L.** Every new save meets him first, so no new attack hits harder than the slam, and the sap takes at least
  1.2 s to fall.

### The Hayward - The Fallow Fields (chapter 2)

**Today:** he keeps his hurdle facing you, and hits from the front land at x0.2. His combo is a swing, then a charge
down a lane, then he is spent and open from behind (x1.3). At 0.5 a ring swing comes in. At 0.28 his temper makes him
quicker.

- **The hurdle** (from the start until it breaks)
  - *Changes:* today's combo stays, but its wind-ups are in his body, in eight facings (129, 133). He draws back and
    braces the hurdle for the swing. He drops and paws the ground before the charge. When you are out of reach, he
    throws a hedge stake down a lane (130), so he is never idle.
  - *The hurdle's own bar:* the hits it blocks feed it. What the x0.2 takes off a hit goes into the hurdle. It cracks in
    three stages, and its cut weakens with each one: x0.2, then x0.4, then x0.6 (131). As a backstop, it breaks when he
    is down to 40% health.
  - *You:* go round to his back for full damage, or batter the hurdle from the front to end this phase sooner. Hits on
    the front are no longer wasted.
- **Unpenned** (the hurdle breaks)
  - *Beat:* the hurdle bursts into straw and withies. He throws down the haft and pulls the billhook from his belt.
  - *Changes:* he has no guard at all, and he never stands still:
    - rushes of three short lunges, each told by his crouch
    - a flurry of three quick cuts, left, right, left, when you are close
    - stakes thrown at range
  - After each flurry he is winded for 1.5 s and takes x1.3 from any side. Today's temper comes in at 25%.
  - *You:* the fight turns inside out. Stop circling a slow wall, keep away from a fast man, and punish him while he is
    winded.
- **Shown:** the hurdle's three crack states, then a second sheet without the hurdle, billhook in hand.
- **Needs:**
  - Art: both sheets in eight facings (129), the crack states, the burst, the stake, and a winded loop.
  - Code: the breakable part, the beat, and the rush, flurry and stake in `HaywardBoss`.
- **Size: L.** Most of it is items 129-133, which are already asked for.

### The Ninth Stone - The Broken Circle (chapter 3)

**Today:** about every 7 s it drops a ring of stones round the wizard with one gap, and then the middle falls. At 0.6 it
closes twice in a row. At 0.28 the closes come quicker, with stray stones falling between them.

- **The close** (from the start to 60%): as today. The stones are seen falling, using the shared falling hazard.
- **The double close** (60%): as today. The split down its face opens wider.
- **The moot** (28%, replacing today's last phase)
  - *Beat:* the ground heaves and eight stones rise in a ring round *it*, with one gap. The ring turns slowly, so the gap
    moves.
  - *Changes:* from outside the ring it takes a quarter of every hit. Inside, it takes everything, and every few seconds
    the middle falls on the old tell.
  - *You:* the chapter's verb, reversed and doubled. Find the way in, hit it, find the way out, and by then the gap has
    moved.
- **Needs:**
  - Art: the stones rising, and two damage states for the split.
  - Code: a turning ring that keeps the close's standing stones, and a damage share that reads where the wizard
    stands.
- **Size: M.**

### The Hollow Choir - Sunken Cave (chapter 4)

**Today:** three voices hold thirds of a circle round the wizard. A voice you drop is sung back up unless all three fall
together. At 0.7 comes the round, where each voice launches a note in turn. At 0.33, the last verse is quicker.

- **Three voices** (from the start) and **the round** (70%): as today.
- **The last voice** (33%, replacing the last verse)
  - *Beat:* all three fall silent together, then sing on.
  - *Changes:* the singing back stops. A voice you drop now is gone, and its song passes to the survivors: its motes
    wind out of it into them, and they grow, speed up, and add its notes to theirs.
  - *You:* the rule flips. After a fight spent spreading damage, focusing one voice is now right, and the last voice is
    the strongest.
- **Needs:**
  - Art: the song passing (reusing the revive's motes), and two grown states.
  - Code: revives off for the phase, and the survivors scaling up.
- **Size: M.**

### Mother Rot - Blighted Swamp (chapter 5)

**Today:** she sheds a pair of spawn at every fifth of her health, at 0.8, 0.6, 0.4 and 0.2. While any spawn lives she
heals 0.35% of her health a second. The last shed also enrages her.

- **The shedding** (from the start to 40%): as today.
- **She calls them home** (40%)
  - *Beat:* she sinks low and calls.
  - *Changes:* she no longer heals from spawn at a distance. Every living spawn turns and crawls back to her. Each one
    that reaches her is taken back in and heals her 4% at once. While she takes one back she swells, stays planted, and
    is open (x1.3).
  - *You:* the old choice becomes a race you can see. Get between her and them, or burst her while she feeds.
- **Last brood** (20%): she sheds everything she has left at once, a ring of six. She enrages and keeps calling them
  home.
- **Needs:**
  - Art: the call, the swell, and the bog crust sloughing off her in two damage states.
  - Code: spawn that steer back to her, and the take-back.
- **Size: M.**

### The Archivist - Mystic Ruins (chapter 6)

**Today:** it never takes a step. A wedge of light turns with it, and every so often it dives and surfaces somewhere
else. At 0.66 a second beam appears on its back. At 0.34 it turns faster and dives more often.

- **The sweep** (from the start to 66%): as today.
- **The record** (66%)
  - *Beat:* its pages lift and it starts to write.
  - *Changes:* a line of its script is written on the ground behind the wizard as he walks, covering his last few
    seconds of path. About every 8 s it reads the line back. The script lights up from its oldest end toward him, a
    little slower than he walks.
  - *You:* commit to a direction. If you double back on your own path, you walk into it, and the sweep chooses your
    direction for you.
- **Two beams** (34%): today's second beam moves here, along with today's quicker dives.
- **Needs:**
  - Art: the script's glyphs lighting up, and its pages torn in two damage states.
  - Code: a trail of the wizard's positions (one preallocated ring buffer, like the Long Coil's), and the hit when it
    reads back.
- **Size: M.**

### The Still Warden - Frozen Waste (chapter 7)

**Today:** it is frozen in place:

- ice falls on marked ground wherever the wizard stands
- its guards strike the wedge they face
- it hits the ring it stands in

At 0.66 the cold closes in. At 0.34 comes the whiteout: ice falls outside its claim, so you must be at its feet.

- **The claim** (from the start) and **the cold closes in** (66%): as today, with the ice shown by the shared falling
  hazard. From 66% its ice shell has its own bar, and only hits dealt from inside its claim crack it.
- **The thaw** (when the shell breaks; backstop at 34%)
  - *Beat:* the shell bursts in a marked ring of shards, and the dark shape inside steps out.
  - *Changes:* it walks, slowly, toward the wizard. The whiteout comes with it: the ice falls outside a claim that now
    moves. Out of its shell it takes x1.25.
  - *You:* the boss that never moved now moves. Stay inside a claim that walks, and keep clear of its ring. Fight close
    early to crack the shell, and you bring the thaw sooner, and the exposed body with it.
- **Shown:** the shell's crack states, then the shape out of the ice. This is the mirror of Elderbark: the first
  chapter's boss stops walking, and the ice's starts.
- **Needs:**
  - Art: the shell's crack states, a walking sheet for the shape, and the shards.
  - Code: the breakable part (shared with the Hayward), steering, and a claim that moves with it.
- **Size: M-L.**

### The Long Coil - Scorched Sands (chapter 8)

**Today:** it spends most of the fight under the sand and can only be hurt while it is up. At 0.66 it surfaces throwing
a lane of sand spurts. At 0.34 it enrages.

- **The ridge** (from the start) and **the spurts** (66%): as today.
- **The pit** (34%, replacing the enrage)
  - *Beat:* the sand funnels round it as it dives.
  - *Changes:* before each surfacing, a pit opens. Its rim is marked and the sand streams inward, dragging the wizard and
    the swarm toward the middle, slower than he walks. The head bursts up in the middle and takes whatever is there.
  - *You:* walk out against the drift, and be at the rim when it surfaces, close enough to land your burst in its
    window.
- **Needs:**
  - Art: the funnel's streaming sand, and torn scales in two damage states.
  - Code: an outside drift on the player's movement, and on enemies.
- **Size: M.**

### The Warden of the Deep - The Emberdeep (chapter 9)

**Today:** four phases, each a chapter boss played back: it charges, then sweeps, then burrows, then does all three
while the ceiling comes down (thresholds 0.75, 0.5 and 0.25).

- **Keep the recap.** It is the whole idea of the fight. What it lacks is a beat between modes. At each threshold a plate
  of its cold armour falls, and the seam that drives the next mode lights first. By the last phase the fire shows
  through most of it, across four damage states.
- Its tells use the same primitives as the bosses it recaps, so it picks up their animated versions as each one is
  built.
- **Size: S.**

### The Constable - The Last Bridge (chapter 10)

**Today:** he fights from the wall and can't be targeted there. He comes down when the wizard holds the foot of the
gate. At 0.66 and 0.33 he goes back up with thicker volleys, so the gate has to be taken three times.

- **The wall** (from the start) and **back up** (66%): as today.
- **The gate shut** (33%)
  - *Beat:* instead of climbing a third time, he cuts the portcullis's chains and it crashes down behind him.
  - *Changes:* he fights in the yard to the end, enraged, while the crossbows on the wall keep shooting without him. His
    sweep becomes two sweeps, left then right.
  - *You:* the ask changes from advancing under fire to duelling under it.
- **Needs:**
  - Art: the chains cut, the fallen gate, and his livery torn in damage states.
  - Code: a last phase that skips the climb.
- **Size: S-M.**

### The Gaoler - Cursed Dungeon (chapter 11)

**Today:** mounted, he charges down a lane he shows you first, and you step sideways. At 0.5 he dismounts and uses a
short cone up close; today the only sign is a colour change. At 0.34 he enrages.

- **Mounted** (from the start to 50%): as today, but the tell is in the horse's body: it rears and paws.
- **Unhorsed** (50%)
  - *Beat:* the horse goes down under him. He is thrown, rolls, and stands up.
  - *Changes:* the horse gets up without its rider and runs lanes across the cells on its own. It can't be targeted,
    and each lane is told as before. Meanwhile he fights on foot with today's close cone.
  - *You:* the chapter's two ranges at once. Sidestep the horse while you stay out of his reach.
- **Remount** (25%, replacing the enrage)
  - *Beat:* the horse wheels back and he swings up into the saddle on the run.
  - *Changes:* every charge now ends in the cone, thrown from the saddle at the end of the lane.
  - *You:* step out of the lane, then clear its end.
- **Needs:**
  - Art: an unhorsed sheet, the horse without a rider, the fall and the mount.
  - Code: the horse as a second untargetable body running the boss's lane.
- **Size: M-L.**

### The Seneschal - The Court (chapter 12)

**Today:** he fights with a staff ring and the rank, a wall of guards marching down a marked lane. At 0.75, 0.5 and 0.25
he leaves through a servants' door to another room, and you have to follow.

- **The first two flights**: as today.
- **The house is shut** (25%, replacing the third flight)
  - *Beat:* he does not leave. He brings the staff down, and every door of the room shuts behind the wizard.
  - *Changes:* ranks come from two doors at once and cross each other. The staff comes faster.
  - *You:* after a fight spent chasing him, there is nowhere to go. Read two crossing walls in a closed room.
- **Needs:**
  - Art: the doors shutting, the staff cracked, and his livery in damage states.
  - Code: the room's doors as walls for the phase, and two ranks at once.
- **Size: S-M.**

### The dark wizard - The Throne (chapter 13)

**Today:** already the most varied fight:

- **He takes:** he seals one of your spells, and you touch its orb to get it back.
- **His light (0.66):** the hall goes dark, lanes cross it, and Darkness hurts him more.
- **All he took (0.33):** he throws the tells of the wardens you beat back at you.

**Proposed:**

- Keep all three phases.
- Add the beats. The hall going dark is already a transformation, so hold it as one.
- Show his damage on his sprite: the eclipse round his head cracks, and the colours that came into him at his arrival
  drain back out.
- **Size: S.** He stays unnamed, and no words are added.

### The Unstrung Hunter - The White Downs (far shore)

**Today:**

- **The draw:** a lane that follows you, and a barrow cuts it.
- **The hunt is up (0.66):** he sets his hounds on you.
- **Unstrung (0.33):** a faster draw, and stars fall.

**Proposed:**

- Keep all three phases.
- Add the beats: the horn wound for the hunt, and the string drawn to full stretch.
- Show his damage on his sprite: the pelt torn, and the three stars of his belt going out one by one.
- The far shore is DLC, so build this separable, and last.
- **Size: S.**

---

## 3. What to build once, and in what order

### Shared pieces

1. **The phase beat.** `BossEnemy` plays one transformation per phase. Its hooks sit beside the arrival's:
   `OnPhaseBeat`, `TickPhaseBeat` and `DrawPhaseBeat`. During the beat the boss is untargetable for about 1.2 s, with a
   shake and a sound, and the field is not held. The same piece carries the threshold clamp (taken from the Seneschal's)
   and the notches on the boss bar. **Used by all fourteen. S-M.**
2. **Damage states (132).** One state per phase or part. Each is the same animation with a suffix from the boss's own
   sheet, falling back to the whole sheet. Each boss's generator draws its states. **Used by all. Code S; art per
   boss.**
3. **Body wind-ups (133).** A wind-up strip stretched to fit the tell, so the strike frame lands on the resolve frame.
   **Used by all. S.**
4. **The falling hazard (116).** The thing is seen in the air, and its shadow grows on the floor to the exact
   `Contains()` area. The hit lands when it does. It is a draw mode of `TelegraphedGroundHit`, not a new kind of hit.
   **M.** Used by:
   - Elderbark's sap and bark
   - the Still Warden's ice and shards
   - the Ninth Stone's stones
   - the Seneschal's chandeliers
   - the Hunter's stars
   - the Warden of the Deep's ceiling
5. **The breakable part.** A second bar on the boss:
   - the boss writes the rule that feeds it, in `AdjustIncomingDamage`, reading `IncomingHitFrom`
   - it has crack states and a backstop share
   - breaking it starts a phase
   - it has a thin bar under the boss bar

   **Used by the Hayward's hurdle and the Still Warden's shell. M.**
6. **Boss adds.** The helper already exists: the Seneschal and the Hunter call their cast through
   `Node2DGame.SpawnEventEnemy`, by roster role. Mother Rot and the Still Warden still instantiate their own scenes.
   What is missing is a cap on how many are alive at once. **Elderbark's saplings use it first. S.**

### Order

1. **Elderbark**, with pieces 1-4 and 6, and item 117's rooted state. Every new save meets him first, Eric named him, and
   he needs the most pieces, so the pieces get built against the fight that matters most.
2. **The Hayward**, with piece 5 and items 129-133. He is chapter two, Eric named him, and his art is being redrawn
   anyway.
3. **The presentation sweep.** Beats, damage states and body wind-ups for the bosses whose phases already ask something:
   - the Warden of the Deep
   - the Constable's first two phases
   - the Seneschal's flights
   - the dark wizard
   - the Hunter

   This is mostly art, with no new rules.
4. **New phases, in campaign order.** The Ninth Stone, the Choir, Mother Rot, the Archivist, the Still Warden (reusing
   piece 5) and the Long Coil. Then the castle: the Gaoler, and the Constable's and Seneschal's finales.
5. **After each boss:**
   - run `_BossProbe` through every phase and through the part
   - photograph it on the phone canvas
   - re-measure the fight's length with the balance bot's protocol (`.ai/enemy-behaviour.md`, "Boss entrances and
     longer fights")

   Phases add time when the boss can't be hurt, so change its health to compensate, not its phases.

### For Eric to decide

- Should the phase beat leave the field running, unlike the arrival? This doc assumes yes.
- Should no hit be able to skip a phase, so a big build loses a second or two at each one? This doc assumes yes.
- Should a part show a second bar, or only the cracks on its art?
