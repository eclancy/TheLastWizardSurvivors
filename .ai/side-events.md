# Side events: what the dark wizard keeps in each place

**Latest 2026-11-06 update (Eric):** the Broken Circle's existing `the_moot` frees **Merlin** and
grants Orb of Ages as well as Gilded Mote. The six-stone ward-restoration route and restart rules
stay intact. Merlin is visibly bound at its center and receives a freeing reel. Medea's `tithe_scale`
returns to the Fields after `the_well`. Baba Yaga retains the deeper wooded placement described below.

**Earlier 2026-11-06 placement:** Medea's `tithe_scale` was the Broken Circle's second event, after
`the_moot`; the Fields retain `the_well` and their Pinfold now frees Laelaps. Its existing price,
art and rescue stay the same; her clue now points beyond standing stones. Baba Yaga's Forest hut
uses a deeper 6-8 chunk band with wooded surroundings, while keeping the grove/event separation
and open chase clearing. Earlier tables below describe the historical placement.

**2026-11-03: eight of these asks are to be replaced by puzzles** - the walking hut, the Tithe-Reeve's
scale, the gibbet's cage, the thaw, the dry tomb, the barge, the Gaoler's key and the dotted door
(`.ai/unlock-puzzles.md`, designed). Until each is built, what is written here is what plays. **Built: the gibbet's
cage** (3.4, 2026-11-04) - its row below says what plays now.

Five chapters each hold one optional encounter, out in the world from the first second of a run,
for the player to find. Winning it carries out something he took: a wizard in a cell, or a spell.
These are the game's first **Discovery** unlocks, the source `UnlockCatalog` declared from the
start and never used.

Read `.ai/world-and-tone.md` first. Every chapter is somewhere he holds a prisoner, and the map
card's one line (`StageCatalog.CorruptionText`) is what each event is built from.

## The five

| chapter | event | the line it comes from | what it asks | reward |
|---|---|---|---|---|
| Enchanted Forest | **Hold the grove** (`GroveEvent`) | "Every path now leads inward." | **stand still** in a ring for 20 s; leaving drains it at half speed | Bramble Seed |
| Enchanted Forest (second) | **The walking hut** (`WalkingHutEvent`) | "Every path now leads inward." | **herd** (unlock-puzzles.md 3.3, built 2026-11-07): a hut on hen's legs walks its circle in the deep wood; within 420 px it turns its back and bolts, quicker than you for 2 s, then slower - and fleeing you is how you steer it. Near its circle stands her yard, a bone fence with one gap between the two brightest skull lanterns, facing away from the circle. Drive it through the gap and it has nowhere left to run: it kneels and opens its door. The 30 s bar and its band are gone; losing it still costs nothing (it calms and stands, its prints lead back) | Baba Yaga |
| Fallow Fields (second) | **The Tithe-Reeve's scale** (`TitheScaleEvent`) | "He takes a tithe of everything that grows here." | **balance** (unlock-puzzles.md 3.5, built 2026-11-07): from arrival every XP orb that falls within 320 px of the reeve's great scale drifts into a pan instead of to you - the left pan if it fell left of the post, the right if right - and the beam tips toward the heavier. Once the two pans hold one level's XP between them, hold the beam level (within 10% of the price) for 3 s and the bar falls from the granary door and she steps out. Overshoot one side and you fight on the other, which costs more; past 1.6 levels the reeve takes nothing more and the beam settles itself. Walking off keeps what is paid; nothing fails | Medea |
| Cursed Dungeon | **The Gaoler's key** (`GaolersKeyEvent`) | "He keeps a wizard in one of these cells." | **carry**: kill the Warden, take its key across the map; three hits knock it loose | Tituba |
| Sunken Cave | **The nine mouths** (`NineMouthsEvent`) | "His shadow followed the water down into the dark." | **listen**: a crescent of rock with nine mouths; her voice comes out of all of them a beat apart, hers first; stand in hers 2 s. A wrong mouth answers with a flight of Gloomwings and hers moves | the Cumaean Sibyl |
| Blighted Swamp | **Snuff the lanterns** (`BogLanternsEvent`) | "Nothing living lights them." | **explore**: four lanterns spread over the map, each releasing wisps, then the lantern-tender comes and is fought on its own terms (below) | Contagion |
| Blighted Swamp (second) | **The gibbet's cage** (`GibbetCageEvent`) | "Nothing living lights them." | **lead** (`.ai/unlock-puzzles.md` 3.4): a cage hangs from a gibbet over a pool of rot; hold the winch 2 s and it comes down. He follows your own steps at 75% of your walk to the ferry landing, 1,450-1,750 px off, which the arrow shows. Between them, three walls of rot pools, each with one gap (`RotPoolLayout`), that hurt only him - 5 of his 60 the moment his feet are in one and every 0.25 s they stay; your steps glow on the ground ahead of him, and a pool stirs sickly yellow-green when he nears it. Walk back over your steps and he leaves the loop out. Every 5 s the Mired rise in the next gap. Nothing hunts or bites him; if the rot takes him the bog takes him back to the cage and the walk starts again | Paracelsus |
| Mystic Ruins | **The Five-Pointed Seal** (`FivePointedSealEvent`) | "The old wards were broken from the inside." | **solve**: four guardian stones round a star cut in a courtyard floor; a step onto a stone's plate turns it and the two beside it a quarter (a puzzle, six to twelve presses, always solvable); all four facing the seal breaks it | Abe no Seimei |
| Frozen Waste | **The thaw** (`ThawEvent`) | "A wizard lies under the ice." | **sing back** (unlock-puzzles.md 3.6, built 2026-11-07): over the frozen lake, fanned out from the block to the shore, five strings of ice - a kantele - each with its own note (A3 D4 F4 C5 A5, low to high clockwise) and its own colour along it. Once he is on the lake the ice sings a phrase: the strings light and sound in turn. Step on them in the same order (his feet, never an enemy's; the open ice round the block is the way between two that are not side by side). Three phrases, of three, four and five notes, each the last with a note added; each sung back takes the block a thaw stage further and the last frees him. A wrong string replays the phrase and loses nothing sung back; walking off forgets only the phrase under way. The heat, fire's triple and the window as the place where fighting counts are gone | Väinämöinen |
| Scorched Sands | **The caravan master** (`CaravanMasterEvent`) | "The dead still walk the trade road." | **catch**: a walker on a fixed road that never turns toward you and escapes at the end | Iron Palisade |
| Scorched Sands (second) | **The dry tomb** (`TombBeaconsEvent`) | "He hid the water." | **kindle**: three cold beacons on the dunes 700 px apart round a sealed tomb; stand by one 1.5 s to light it, a lit one burns 25 s before the wind puts it out, and the light draws six of the dead in at it. All three alight at once and the slab slides aside. Any order | Imhotep |
| The Emberdeep | **The great anvil** (`GreatAnvilEvent`) | "His forge, at the end of the road." | **bait**: four chains staked out from the anvil Wayland is chained to; only the forge's own Trip-Hammers break them - a slam that lands on a stake breaks its chain | Wayland the Smith |
| The Court | **The seer's voice** (`SeersVoiceEvent`) | "A court with no one left in it." | **seek**: a voice reads aloud from one of the house's rooms (the seed's); reach the reading place and he is gone - an open book, a candle still lit - and the voice reads from another room; the third time he is at his desk. Heard, never pointed at: the voice, the stars of its cue and the line it reads come from the room's side, and the walls bend the way there | Nostradamus |
| The Court (its second) | **The dotted door** (`DottedDoorEvent`) | "A court with no one left in it." | **trace**: on the Gallery's floor at its west end, under the first portrait, a door sketched in charcoal with fifteen numbered dots; from dot 1, walk the line dot to dot within 30 px and his light follows the feet as ink - stepping off pauses it, nothing resets - and the last dot inks the door in: painted, it opens on his light and he steps out. The household comes in at the Gallery's doors while it is traced | Ma Liang |

Each asks for something different, and none of them is "a harder fight". The Ruins' is the first of
the chapter wizards' (`.ai/chapter-wizards.md`, section 4.5 and its "Built" note), added 2026-11-01; the Emberdeep's
the second (4.7, 2026-11-02), and the only one that uses the enemy's own attack; the Cave's the third (4.3,
2026-11-02). That was the brief: an
event that is only a bigger elite is not an encounter.

## The thaw's lake (2026-11-01)

Eric, playing 0.10.0: *"Wizard trapped in ice should be in the center of a large, magically animated
lake. Current art is corny looking."* It was two rectangles and a circle from `_Draw`, on bare ground. Now
Väinämöinen's own idle sprite, recoloured into the ice's tones (his staff head one cold point of light),
stands in a block of ice at the centre of a frozen lake 640x512 px with a dithered bank of drifted snow
(`tools/art/thaw_lake.py`: `world/thaw/`). The block has four thaw stages - cracks, a chipped corner,
meltwater - each with a glint crossing its face. `scenes/shaders/thaw_lake.gdshader` animates the lake from
generated textures only (the water's rule, `.ai/terrain.md` 3): **rings of cold light run out under the ice
from him**, light wanders slowly under the rest, and **frost ferns creep in from the shore and draw back**
over 22 s. Round him is a window of clear dark ice about the size of the heat's reach (`HeatRadius` 150),
and the light is brightest there, so where fighting counts is where the light is - no ring on the floor.
The light grows with the heat, a warm glow grows in the block, and once he is out the song dies away.

- **Walkable, not terrain.** The fight is on it, so it blocks nothing, is in no group, and is drawn on the
  ground (`ThawLake`, z `Ground - 2`). It is the event's sibling, so it outlasts the event.
- **Placed on open ground, all of it.** The footprint is 360 px; eight points on a ring that wide are 280
  px apart, so the thaw alone places through `IsExploreGroundClearDense` (rings and points every 80 px,
  opt-in, so no other site moved). `_ExploreProbe --stage=5`: 22 passed on seeds 4242 and 31.
- **Its rules were unchanged by the lake**: damage near the block thawed it, fire counted triple, the ice is
  never a target. **Since 2026-11-07 it is sung back instead** (`.ai/unlock-puzzles.md` 3.6): the kantele's five
  strings lie over this lake (`tools/art/kantele.py`, a child sprite of `ThawLake`), the light under the ice grows
  with the phrases sung back rather than with the heat, and the window of clear ice is only where the light is. The objective arrow points at the top of the block, so its chevron is not on his face
  (`PoiPointer` now treats an objective within 140 px of a place as on it).
- Checked by `RegressionChecks.ValidateThawArt`. Not checked: the shader's cost on the phone.

## How it is built

- **`SideEvent`** is the base: where its sites are, the dormant wait and the arrival, the far
  tell, the objective line under the run timer, the edge-of-screen pointer (`SideEventPointer`),
  the success and failure beats, and standing down when the boss arrives, for the same reason
  chests and the Warden do. A subclass is only its own rules. Each one is a code-created `Node2D`
  at the run's origin that draws itself on the ground (`ZIndex` -5, y-sorted) with an additive
  light layer over the actors; the only one with a sprite is the bog lantern.
- **`SideEventDirector`** is the whole mapping from chapter to events to Discovery sites to
  wayfinder, and it **places** the events and the wayfinder at stage start, from the terrain seed
  (section "Finding the event"). A chapter's line is a list, its original event first, and may hold
  a second (section "Two events in one chapter"). A chapter not in its table has no event and no
  wayfinder.
- **`DiscoveryRewards`** pays out a site: everything in `UnlockCatalog` with
  `Source = Discovery` and that site as `SourceId`. **Granted and saved the moment the event is
  won**, not at the end of the run, because a wizard you carried out does not go back in when you
  die three minutes later. The game over screen lists it via `Node2DGame.RecordDiscovery`.
- **Once a site is exhausted the event still runs and pays a chest.** It stays worth doing.
- `Node2DGame` exposes the short list an event may use: `SpawnEventEnemy` (the stage's own spawn
  table and health curve, so an event can never put something on the field the stage would not),
  `FindEventSite`, `ReachablePoint`, `ClampToStage`, `SpawnRewardChestAt`.

## How a win shows (2026-10-20)

A player held the grove and asked what it had done. The honest answer was: on a first win it
unlocked Bramble Seed and saved it, said so in one small line under the timer for five seconds, and
played `meta_unlock`; on every later win it put a chest at the grove's **centre** - where the player
was standing - on the same frame the grove stopped drawing, so the chest usually opened under his
feet before he had seen it. Nothing marked the win.

All five events resolve through `SideEvent.Succeed`, so all five now get the same beat:

- **Where.** The reward spot is measured from the player, toward the event's site, clamped to
  80-200 px and passed through `ReachablePoint`: never underfoot, never off screen (the caravan
  master and the lantern tender can die anywhere a spell reaches them). Standing on the site, as in
  the grove and the thaw, it goes a step up-screen.
- **A phantasmal column** (`PhantasmRise`) stands there for 2.2 s: a pale Arcane-cyan beam, motes
  drifting up it, a ring running out across the floor. Not gold, which is the elite rim.
- **The chest rises as a ghost** (`ChestReward.MaterialiseSeconds`): translucent and flickering,
  lifting out of the ground, then taking on colour and settling, and it cannot be collected until it
  is solid. Its sound is `event_reward`, a breath swelling into one glass tone. A discovery keeps
  `meta_unlock`, because that is the save changing.
- **A heading-sized line** replaces the objective under the timer: bold Cinzel, pale gold, wrapped
  for the phone canvas, fading in and out over its five seconds. A discovery reads "Spell
  recovered - Bramble Seed", "Boon found - ..." or "Wizard freed - Tituba", the kind first and the
  name after (Eric, 2026-10-31); a chest reads in the event's own words
  (`ChestLine`). The label is now inset by `SafeArea.ApplyTo`; it never was, and on a phone the
  objective line sat on the XP bar.
- **A site with a state shows it before it pays** (in-flight 58, the well's pattern: a 1-2 s beat
  at the site on `SecretSeen`, then `Succeed(spot, exact: true)` at the exact spot the beat aimed
  at): the well's water climbs clean and the glint arcs out; the grove blooms - the crowns leaf out
  one by one round the ring under a green wave, and the bloom lingers three seconds after instead
  of vanishing on the winning frame; the Gaoler's key turns in the lock, the barred gate swings
  open on its hinge and the cell's gloom lightens, its light spilling out; the thaw's ice falls
  away - a crack, chips falling and melting, meltwater running out as the block goes. The scale
  settling level is Medea's redesign; the hut's kneel is its own beat already.

| event | first win pays | every later win pays | chest line |
|---|---|---|---|
| Hold the grove | Bramble Seed (spell) | a chest | "The grove holds - a chest rises from it" |
| The Gaoler's key | Tituba (wizard) | a chest | "The cell stands empty - a chest was left in it" |
| Snuff the lanterns | Contagion (spell) | a chest | "The lanterns are dark - the tender's chest is yours" |
| The thaw | Väinämöinen (wizard) | a chest | "The ice gives up a chest" |
| The caravan master | Iron Palisade (spell) | a chest | "The caravan master falls - his chest is yours" |
| The dry tomb | Imhotep (wizard) | a chest | "The tomb stands open - a chest was left on its stair" |
| The Five-Pointed Seal | Abe no Seimei (wizard) | a chest | "The seal is broken - a chest was left inside it" |
| The great anvil | Wayland the Smith (wizard) | a chest | "The chains are broken - a chest was left at the anvil" |
| The nine mouths | the Cumaean Sibyl (wizard) | a chest | "Her mouth falls quiet - a chest was left in it" |
| The walking hut | Baba Yaga (wizard) | a chest | "The hut kneels - a chest was left at its door" |
| The barge | Morgan le Fay (wizard) | a chest | "The barge comes ashore empty - a chest was left in it" |
| The gibbet's cage | Paracelsus (wizard) | a chest | "The punt has taken him - a chest was left on the landing" |
| The seer's voice | Nostradamus (wizard) | a chest | "He left something on the desk - a chest" |
| The Tithe-Reeve's scale | Medea (wizard) | a chest | "The scale is level - a chest was left in the granary" |
| The dotted door | Ma Liang (wizard) | a chest | "He left something in the doorway - a chest" |

To photograph any of them: `_UiShot --scene=res://scenes/node_2d_game.tscn --stage=N --win-event`
(add `--event-chest` for the chest path on a save that lacks the discovery). It sets
`SaveManager.WritesDisabled`, because a won event grants and saves on the spot. `--win-event` now
begins the event where it was placed (`SideEvent.Arrive`) and stands the player on it.

## Finding the event (2026-10-22)

Reported: "it is on the map when the game begins - they are all further away and hidden at first
... so the events are already populated and it's up to the player to adventure, rather than wait
for timers." Until then every event started itself at 90 pace seconds, a few hundred pixels from
wherever the player was. Now nothing about an event is timed.

### Where it is

`SideEventDirector._Ready` runs before the first chunk is furnished and lays the chapter out with
`ExplorePlacer`, a pure function of **(terrain seed, stage, stream)** on the ground the generator
will produce - the rules `.ai/stage-secrets.md` section 4 wrote for secrets, applied first to the
events:

| what | band (chunks of 1536 px from the start) | bearing | footprint |
|---|---|---|---|
| the event's main site | **4 to 6** (6,100 to 9,200 px) | seeded | the event's own (grove ring + 70, the thaw's lake and bank, 360, the cell's 110, a lantern's pool + 40, the camp 140) |
| the bog lanterns' other three | a chain: each **760 to 1,100 px** from the last, never within 650 of another, all within 2,200 of the first | seeded | |
| the caravan's road | 2,000 px from the camp, the first of twelve bearings whose whole length is open ground | seeded | a 72 px strip, reserved |
| the wayfinder | **1.5 to 2.5** (2,300 to 3,800 px) | seeded, at least **75 degrees** off the event's | 90 |

Every roll is drawn before its position is tested; a spot that is water, wall, hazard, fence,
landmark or an island (a 24-cell flood from its centre) is replaced by a fixed outward search
(96 px rings to 1,600 px; on the maze the search runs over corridor-cell centres, 384 px apart,
and the event's site snaps to one). `Node2DGame.IsExploreGroundClear` answers from the streamer's
chunk **data**, generating it for far ground without painting it, so the answer is what that ground
will be when the player gets there. The footprints, and the road, are then reserved
(`ReserveExploreGround`): every furnishing pass (decor, trees by a further 140 px, curated props,
stage hazards) keeps out of them on every visit, so the furnishing stays a pure function of
(seed, chunk) and a chunk forgotten and repainted comes back with the same things round the same
site. The site itself never moves - it is a position on a node that is never freed.

Measured on seed 4242 (`_ExploreProbe`, all nine stages): sites landed 4.1 to 4.7 chunks out, the
wayfinders 1.5 to 2.2, 66 to 106 degrees apart; every site on open ground with nothing on its
footprint, identical after its chunks were forgotten and reloaded. At 220 px/s with no swarm the
main site is 28 to 42 s of straight walking; a real walk through the horde is longer, and the
distance is the price of the trip, which is what the band is for. Stages without an event
(Sketchbook) place nothing. The Emberdeep has had the great anvil since 2026-11-02, placed the seal's way
(open ground over its whole 230 px disc, so the anvil never stands in lava): seeds 4242, 31 and 7 put it 5.9, 5.1 and
4.3 chunks out. The Cave has had the nine mouths since 2026-11-02, a 300 px open disc found by
`ExplorePlacer.EventSiteInBand` in a chamber or a hall (3.6 to 5.3 chunks out on four seeds). The Ruins has had
the Five-Pointed Seal since 2026-11-01: its
courtyard needs 290 px of open ground, which on the Ruins' walled lattice is only a court (one block in
eight, `.ai/terrain.md` section 11), so it is placed by `ExplorePlacer.EventSiteInBand`, which sweeps the
whole band when the ordinary search near the roll finds none. Seeds 4242, 31, 7 and 1001 put it 3.8 to
6.4 chunks out.

### How it is seen, and when it starts

The event stands **dormant**: no announcement, no objective line, no arrow. It is drawn and lit
from the start, and gives off a **far tell** on an additive light layer: a wide faint glow (480 px),
a tighter pool, and a thin column of light 500 px tall standing up out of the site, so from a
third of a screen past the edge the glow reaches in, and from further below it the column shows.
It is **cold** (the event's own colour: poison green for the lanterns, ice for the thaw, arcane for
the cell, lichen grey-green for the grove, pale steel for the caravan), because it is his, and it
**flickers**, because found things breathe (`.ai/stage-secrets.md` section 2). Light is only drawn
for a site within 2,600 px of the player, and an event with nobody near costs one distance test
per site per frame.

The event **begins when the player arrives** - `ArrivalRadius`, 380 to 560 px depending on the
event, so the thing is on screen when the line comes up - and from then on runs exactly as it did:
the announcement, the objective, the gold objective arrow, the win beat. The thaw's hit listener
goes on at arrival, not at placement.

A site that comes onto the screen is **seen** (`PointsOfInterest.UpdateSeen`, the camera's view
grown by 80 px), and a seen site keeps a point-of-interest arrow (`PoiPointer`) at the screen edge
until it is done, so a lantern glimpsed at the edge of the view is not lost by walking on. The
bog lanterns carry **no objective arrow** at all: finding them is the event; the gold arrow only
comes up for the tender.

### The wayfinder

Each event chapter hides one object much nearer the start, which marks every point of interest on
the map - the event and each of its parts - with an arrow for the rest of the run
(`PointsOfInterest.Reveal`). Walk onto it (`WayfinderPickup`, with the feet, no prompt) and it is
taken: `bearing_found` plays (a needle swinging and settling, then an open fifth), a line under the
timer says what was marked, and the object lifts and is gone. It is **something he missed**, so it
carries the secrets' near tell and not the events': a 2x2 gold glint with a one-pixel halo drawn
into its four frames, **breathing** on a 2.4 s period, with a thin warm column and a wide faint
warm glow from further off. Warm against a world whose light is all cold.

| chapter | wayfinder | sprite |
|---|---|---|
| Enchanted Forest | **the Arcane Compass**, a brass compass open on a flat stone, its needle wavering with the glint | `arcane-compass` |
| Cursed Dungeon | **the Gaoler's map**, his own map of the cells nailed to a post, one cell marked in red, the glint on the nail | `gaolers-map` |
| Blighted Swamp | the Arcane Compass | `arcane-compass` |
| Frozen Waste | **a bearing cairn**, four snow-capped stones with a brass bearing plate on the top one | `bearing-cairn` |
| Scorched Sands | the Arcane Compass | `arcane-compass` |
| Mystic Ruins | the Arcane Compass | `arcane-compass` |
| Sunken Cave | the Arcane Compass | `arcane-compass` |

`SideEventDirector`'s table is the list (`EventsFor`, `WayfinderFor`): one wayfinder a chapter,
however many events it holds. A chapter without an event has no wayfinder, since there is nothing
for it to mark. Once secrets exist, what a wayfinder marks is a decision for that
system (`PointsOfInterest.Add` is the hook); today it marks the event's sites only.

### The arrows

`PoiPointer` draws a **diamond** badge with the place's pictogram in it and a short tail toward
it, pinned to the screen edge on the line from the centre, inset 66 px at the sides, 132 at the
top (under the HUD band and the objective line) and 76 at the bottom, plus the safe-area insets on
a phone. (The seal's pictogram, a violet star, is `tools/art/five_pointed_seal.py`'s, the anvil's `great_anvil.py`'s, as the well's is
`explore_well.py`'s.) Three promises, three shapes: the objective's gold triangle (what to do now), the chest's
round bubble (a reward), and this (a place). His places wear an arcane-cyan rim; a wayfinder seen
and not yet taken wears gold. The badge and the eight pictograms (lantern, grove, cell, ice,
caravan, compass, map, cairn) are generated by `tools/art/explore_props.py` into
`assets/bonelight/ui/poi/` and drawn at x2 on whole pixels; only the tail turns. Nothing is drawn
for a point on screen, for the point the objective arrow is already on, or during the boss.

### The bog lanterns, redrawn

Reported: "the area around the lanterns in the bog that show the boundaries are too opaque, you
can't see the lanterns. make the lanterns more prominent, and glowing brightly, the area is bigger
and more subtle." The lantern was a 4 px line and a 12 px box drawn over the actors, with a solid
black ring at 45% round it - the ring was the most visible thing there, and the wizard standing on
the lantern hid the rest. Now the lantern is a generated 64x64 sprite (a crook of dead wood out of a
tussock, an iron cage, a cold flame; three lit frames and one dark), sorted with the actors at its
foot; its flame is light drawn above the actors - a core, a halo, a pool on the ground, a column -
in the poison ramp, flickering and guttering as it is snuffed, and the others flare for 2.6 s when
one goes out. The area is its light's pool, **120 px** (was 36), marked by a faint dashed ring at
20% that brightens to 42% while the player stands in it, with the progress filling along it.
Because a 240 px pool is far easier to stand in than a 72 px spot, **snuffing takes 3 s** (was 1.2)
and progress drains over 4 s outside it. Every event's progress track is now that dashed ring
rather than the black one.

### The lantern-tender (2026-10-28)

Eric: *"lantern tender should have a glowing lantern and be bigger so it's much easier to see him. he
should fire animated fire strikes and have a few different special abilities that the player needs to
deal with".* Until then the tender was `SpawnEventEnemy(null, ...)`: a roll on the swamp's table at
elite scale, the same body as the wisps, which walked at the player and did nothing else.

It is now **`LanternTenderEnemy`** (`scenes/SwampLanternTender.tscn`), the event's own creature, in
no roster slot. `BogLanternsEvent.TenderScenePath` names it; the health is unchanged - the run's spawn
health times `TenderHealthMultiplier` (22) - so it is as hard as the run wherever the mire is found,
and the win, the discovery and the chest are unchanged.

**How it is found.** On the 96 cell at x2 (`tools/art/swamp_tender.py`, the Boo's precedent for a
miniboss on the boss cell): a stooped keeper in a wet linen coat, a violet cape over a hunched back, a
sodden hat with bog weed off the brim, wading in its own wake - about 144 px of body, twice a swamp
creature, under a crook that reaches about 180. From the crook hangs **one of the bog lanterns**,
cage and all, burning their cold poison flame, and it carries their far tell: a glow and a column of
light from the flame (drawn additively on its own layer, `LanternLight`), so it is found past the edge
of the screen the way the lanterns were. The bestiary keeps the Lamplighter's flame uncaged so no
ordinary caster looks like an event lantern; the tender is the event, and is meant to.

**What it does.** One thing at a time (a second wind-up never starts during one), and the three
specials also wait for a thrown volley to land, so two tells are never read at once.

| move | when | the tell | what it asks | numbers |
|---|---|---|---|---|
| **Fire strikes** (`LanternFireStrike`) | every 3.4 s, within 820 px | the lantern raised and brightening for 0.5 s; then each flame arcs out of it onto a ring marked on the ground for its whole 1.1 s flight, and bursts there into an animated column with a crown of tongues standing on the ring | **change course**: aimed at him, then one and two steps along his walk (0.45 s of it each); standing still, the line runs off to one side at 80 px spacing | 3 flames (5 at half health, the extra two either side of the first step), 0.16 s apart, radius 64, 3 damage each |
| **Spilt oil** (`BurningOil`) | every 5.5 s, within 210 px | it plants and a wedge is marked in front of it for 0.85 s | **step sideways, and not back**: the splash lands on the wedge, and the wedge stays alight - the one ground in the fight that is still dangerous after it has hit | 230 px, 38 degrees either side; splash 3; burns 4.5 s, 1 damage per 0.6 s spent in it |
| **The dark** | first at 8 s, then every 14 s, only at someone 360-620 px away, never while warded | it plants and lifts the lantern: its light pools on the ground round it to a ring at 280 px, and the dark closes over everything outside it for 2.2 s; the ring flares white when it falls | **come into its light** - the one ring in the game where inside is safe. The anti-kite move, and the event inverted: it was won by standing in the lanterns' light to put it out | 4 damage to anyone outside 280 px; reaching the light from 620 asks 155 px/s |
| **Rekindle** | once, at half health, if a snuffed lantern is within 1500 px | a thread of cold fire crawls from its lantern to the nearest dark one for 1.6 s and lights it (`hint_lantern_flare` plays there) | **put it out again**: while that lantern burns it is warded - a ring of motes round it, the thread to the lantern - and takes a quarter of every hit. The objective line and the gold arrow move to the lantern, which is snuffed by the event's own rule, 3 s in its pool | ward 0.25 of damage taken, rounded stochastically as Weaken is |

Body: radius 30, speed 84 (times the pace multiplier), contact 2, knockback resistance 0.7, slow floor
0.35, `IsMiniBoss` (a blocker, never recycled, immune to fear), stands still once the player is within
150 px - its lantern reaches, it does not need to touch him. Every hit it deals goes through
`ScaleOutgoingDamage` (the strikes and the burn scale theirs when thrown, as `TelegraphedGroundHit`'s
callers do). Its rings, the oil's wedge and the dark's edge are drawn through the inverse of its scale,
so the elite's 1.06 and the hit punch never draw a shape bigger than the one tested.

**What passes between the event and the tender** is `ILanternKeeper`, four calls: the nearest snuffed
lantern, where its flame is, rekindle it, is it lit. The event owns the lanterns and the tender owns
its ward. Snuffing a relit lantern spawns no wisps.

**The flame is the lanterns' cold fire, not the fire ramp**, because what it throws is theirs:
"nothing living lights them". It is a judgement call, and a one-colour change if Eric wants orange -
`swamp_tender.FLAME` (then re-run it) and `LanternFireStrike.FlameCore/Hot/Mid`. Worth knowing before
deciding: the common XP orb is grass `#9BD455`, a near neighbour of poison's mid `#B4E04A`, so a
flame in flight and an orb on the ground are close in hue (the swamp's poison bolts already are).
The ring and the arc separate them; orange would too.

**Checked and photographed**: `RegressionChecks.ValidateLanternTender` (zero wind-ups, a strike flight
under 0.6 s, a wedge that cannot reach what it aims at, a dark whose light cannot be reached in its
tell, a ward that is no ward, the strips its fire draws from) - shown to fire by breaking three values
in the scene. `_ExploreProbe --stage=3` wins the event; with `--slow-bleed` the tender lives to
rekindle (at 0.41 health, the wind-up having waited for a volley), the probe follows the arrow to the
lantern, the ward breaks when it is out, and the event is won (39.6 s of game time after arriving).
`_UiShot --tender[=strikes|oil|dark|rekindle]` calls it through the event's own path and stands it
beside him (`--tender-offset`, `--tender-crowd`; the rekindle needs `--goto=site:3`); pair with
`--fixed-fps 60` on the Godot command line, or a slow software render runs several physics steps a
frame and `--fx-lead` stops meaning frames of the tell. **Not checked: whether it is fun, or the
numbers.** Every value above is a first guess, it was never fought by hand, and nobody has heard it.

### Probes and flags

- `_ExploreProbe` (`scenes/_ExploreProbe.tscn -- --stage=N [--seed=N] [--no-complete]`): placed at
  start and in band, deterministic, hidden until seen or revealed, the wayfinder reveals, the
  site and its furnishing survive unload and reload, dormant at 900 px and begun on arrival, and
  (bleeding its elites) finishable. Twenty-two to twenty-eight checks a stage; it renders nothing.
  `--slow-bleed` bleeds an elite at 4% of its health a second and gives back whatever the player's
  spells took, so a fight with a phase lives to reach it (the lantern-tender's rekindle, above).
- `_BalanceHarness --explore`: the bot walks to the wayfinder, then along the arrows to every
  site, following the objective once the event has begun, and logs each leg's time and distance.
  Without the flag the immortal bot holds its ground at the origin and **never meets the event**,
  which is deliberate: the gate measurements are made without it, as before.
- `_UiShot`: `--goto=wayfinder|event|site:N` (with `--goto-offset=DX,DY`), `--take-wayfinder`,
  `--arrive`; `--explore-log` on any run prints the placement.

## Hints that something is out there (2026-10-25)

Reported: "give hints part way through the runs of the unlockables on maps, such as a woman yelling
for help on the cursed dungeon map so the player identify that there are secrets to find". Since
2026-10-22 every event waits four to six chunks out and says nothing until the player arrives -
right for the hunt, wrong for a player who does not know there is anything to hunt. So, partway
through a run, while the chapter still holds something this save has not carried out, a cue comes
**from its direction**: a short generated sound panned that way, marks in the site's own language
drifting in from that edge of the screen on the sound's beat, and the line fading in beside them
(2026-11-01, below; it was a caption in a bubble with a tail).

### When, and when not

- **On the pace clock** (`Node2DGame.PaceSeconds`): first at **300** pace seconds (a third of the
  reference run, when the build has its shape and a detour is affordable), then every **150**, none
  due after **810** (13:30 - too late to walk there and back before the boss). Four a run at most.
  A shortened run hears them proportionally earlier.
- **Only while the save lacks something the site gives** (`DiscoveryRewards.IsSiteExhausted`, checked
  at the moment of playing). Once Tituba is free nobody calls from the cell again; on a save that owns
  every discovery the system is silent.
- **Only while the event is dormant and none of its sites is marked.** Arriving starts the event's
  own announcement and objective; a site that has been **seen**, or **revealed by the wayfinder**,
  has a point-of-interest arrow (`PoiPointer`), which says where it is better than a voice can. The
  hints retire for good the moment either happens.
- **Never during the boss**; a line and cue up when it arrives fade out at once. Never while the tree is
  paused (the director does not process), so a level-up only delays one.
- **It writes nothing.** It reads the unlock state; no tool scene can mark anything found through it.

### How it relates to the wayfinder and the arrows

It comes **before** them and never alongside them. The far tell is light you see from about a third
of a screen past the edge; the wayfinder is an object 1.5 to 2.5 chunks out that marks everything;
the hint is the only one of the three that reaches a player who has seen neither, from anywhere on
the map, and it stops as soon as either has done its job. It draws no arrow: the edge of the screen
already carries three promises (the objective's gold triangle, the chest's round bubble, the
place's diamond). Its cue wears the event's far-tell colour (`SideEvent.FarTellColor`), so the light
the player finds is the colour the hint told him to look for.

### The cue and the line (2026-11-01)

Eric, playing 0.10.0: *"The clues for each wizard should not just be text and an arrow. For example, for
the singing wizard, once in a while, the player should see musical notes on screen in the direction of the
wizard, and should hear the singing as well. The clue text should show up on the screen, not in a text
bubble, but fading in as another hint."*

**The cue** (`StageHintCue`, `tools/art/hint_cues.py`): a few 16 px marks drawn at x3 (the voice's arcs at
x4) in the far tell's emissive ramp with an occlusion outline and a soft glow, coming in along a band at
the edge of the safe rectangle on the side of the source, spread along it, drifting a little way in and
fading - each on its sound's beat (the timings are the generators'). Drawn in the world's layers, over the
bodies and under the HUD, but held to the view: left on the ground they slid under the line whenever the
player walked toward the sound. Fixed slots, no allocation.

| site | cue | on the beat of |
|---|---|---|
| the grove | leaves tumbling in, though there is no wind | the whisper's breaths |
| the cell | arcs of a voice, each with its echo, coming down the line to the source | the cry's syllables |
| the lanterns | cold flames rising, flickering, guttering | the flare |
| the thaw | **the song's notes**, laid along the edge in the order sung, higher notes further in, each leaving a frost glint | each sung note (`sfx._ICE_SONG` = `StageHintCue.IceSongNotes`) |
| the caravan | the bell walking along the edge, swinging, ringing out either side | each strike |
| the tomb | a thin stream of sand lifting off the ground and rising against the wind, its foot a ribbon breaking into grains, and a fainter one beside it - sand, so in earth's pale stops, over his violet glow | each knock of stone on stone (`sfx._TOMB_KNOCKS` = `StageHintCue.TombKnocks`) |
| the seal | strips of written paper, violet, tumbling in and falling, charring at both ends in their second half | each gust (`sfx._PAPER_GUSTS` = `StageHintCue.PaperGusts`) |
| the mouths | written leaves, pale with blue ink, blown in flat and fast on a cold draught, the first on the line to her | each voice of the call (`sfx._SIBYL_VOICES` = `StageHintCue.SibylVoices`) |
| the hut | birds lifting out of the canopy in his violet light, beating up and away out toward her, near ones and far smaller ones | each fall of its feet and its timbers' creak (`sfx._HUT_FALLS`, `_HUT_CREAK` = `StageHintCue.HutBeats`) |
| the barge | sea mist rolling in low along the edge, and from the first knock a small barge hung shimmering above the water among it, in wind's ramp | each knock of the hull (`sfx._HULL_KNOCKS` = `StageHintCue.HullKnocks`) |
| the gibbet | threads of pale vapour, the arcane ramp's two pale stops (never the lanterns' poison green), drifting in slowly and low against the wind, curling and thinning | each tap of the flask and each cough (`sfx._FLASK_TAPS`, `_FLASK_COUGHS` = `StageHintCue.FlaskBeats`) |
| the granary | wisps of a thin cold herb-smoke in the wind ramp's pale body, lying low and flat and drifting in slowly from her side, curling and thinning to a thread | each syllable of her voice (`sfx._GRANARY_SYLLABLES` = `StageHintCue.GranaryVoices`) |

`hint_ice_song` was rebuilt to be heard as singing (six notes, A G F G E D, `.ai/audio-manifest.md`).

**The line** (`StageHintCaption`): no band, rim or tail. The words fade in 0.45 s after the cue starts (0.8
s, eased; held 4.4 s; out over 1 s), the engine sans at the Body role with a 4 px occlusion outline, over
a soft plate: a capsule of occlusion at 86%, solid behind the words, dissolving through the Bayer matrix
over 26 px - a shadow the words cast, not a box. Outline-only text was unreadable on the Fallow Fields;
on the Sketchbook's paper the plate measured 12.5:1 behind the speech ink. One line on a PC wherever it
fits (640 px), two on a phone. It is set `CueBand` (58) inside the band the cue comes in on, so the marks
are never under it. The placement rules are unchanged: the safe rectangle (under the HUD; on a phone
under the touch band and out of the bottom 30%, the thumb's), never over the wizard, held by the plate's
solid part rather than the words, and `MouseFilter.Ignore` throughout.

**Tone.** `.ai/world-and-tone.md` rule 2 says no copy carries the mood. These are not taglines: the
cry is a person's words, and the rest are captions of a sound, the way a subtitle captions one -
which also makes them the only form of the cue a player with the sound off gets, as most phone
players have. **A hint never names what it is about**: a rescued wizard's name is his reward
("Secret wizards" above), and a found spell's name in a caption would be the answer printed on the
question. `RegressionChecks.ValidateStageHints` warns on any line that contains one.

### The hints

| chapter | site | voice | sound (cue) | lines, in order (the last repeats) |
|---|---|---|---|---|
| Enchanted Forest | `the_grove` | sound | `hint_grove_whisper` (leaves) | "Leaves whispering, though there is no wind." / "The whisper again. Something out there still grows." / "Fainter now, the whispering. It has not stopped." |
| Cursed Dungeon | `gaolers_key` | **speech** (Tituba, unnamed) | `hint_cell_cry` (echo) | "Help! Is someone there?" / "Please - I can hear you. I am down here!" / "I have been patient so long. Please, this way." / "Do not leave me here. Not again." |
| Blighted Swamp | `bog_lanterns` | sound | `hint_lantern_flare` (wisp) | "Far off, a cold light flares and gutters." / "The cold light again. Nothing living lit it." / "It is still burning out there." |
| Frozen Waste | `the_thaw` | sound (Väinämöinen, unnamed) | `hint_ice_song` (song) | "The ice groans. Under it, faintly, someone is singing." / "The song again, slow and deep, from under the ice." / "The singing is weaker now." |
| Scorched Sands | `caravan_master` | sound | `hint_caravan_bell` (bell) | "A caravan bell, somewhere out on the sand." / "The bell again. Something walks the old road." / "The bell is still moving. It does not stop." |
| Scorched Sands | `tomb_beacons` | sound (Imhotep, unnamed) | `hint_tomb_knock` (sand) | "Far off in the dunes, a slow knocking, stone on stone." / "The knocking again, from under the sand." / "Slower now. It has not stopped." |
| Mystic Ruins | `five_pointed_seal` | sound (Abe no Seimei, unnamed) | `hint_paper_wind` (paper) | "Strips of paper on the wind, in a hand you do not know." / "More paper, burning at the edges as it falls." / "Still the paper comes, from the same quarter." |
| The Emberdeep | `great_anvil` | sound (Wayland the Smith, unnamed) | `hint_anvil` (sparks) | "Hammer on anvil, slow and steady, somewhere below." / "The hammer again. Whoever swings it is very tired." / "It does not stop. It is not allowed to." |
| Sunken Cave | `nine_mouths` | sound (the Cumaean Sibyl, unnamed) | `hint_sibyl_voice` (writ) | "Leaves on a cold draught, every one written on." / "A woman's voice from far down, in more places than one." / "The leaves again, written faster now." |
| Enchanted Forest | `walking_hut` | sound (Baba Yaga's hut, unnamed) | `hint_hut_step` (birds) | "Something heavy walks the deep wood, on two legs." / "The steps again. Too heavy for a man." / "Still walking, round and round." |
| The Last Bridge | `the_barge` | sound (Morgan le Fay, unnamed) | `hint_hull_knock` (mist) | "A boat on the water below, where no boat could be." / "The boat again, nearer the bridge." / "Its lantern is still lit." |
| Blighted Swamp | `gibbet_cage` | sound (Paracelsus, unnamed) | `hint_flask_chime` (vapour) | "A man coughing, far out on the bog." / "Glass rings across the water, like a tapped flask." / "The coughing again. Weaker." |
| The Court | `seers_voice` | **speech** (Nostradamus reading, unnamed) | `hint_quatrain` (stars) | "Out of the west one light, on foot, alone." / "The one he missed walks every road he made." / "When the last door opens, the house goes dark." |
| Fallow Fields | `tithe_scale` | **speech** (Medea, unnamed; she does not plead) | `hint_granary_voice` (smoke) | "You there. The granary, past the furrows." / "He tallies me like a sack of grain. Balance his scale." / "Come, or go. I have outlasted kings." |
| The Court | `dotted_door` | sound (Ma Liang's brush, unnamed) | `hint_brush` (paint) | "A brushstroke drawing itself along the wall." / "Another line, wet, running the same way." / "The paint is still running." |

Tituba speaks in her own character: a prisoner taken from Barbados and jailed on an accusation,
patient (her passive is *Patience*) and nobody's witch - so she asks for help and says she has
waited, and never says why she is there. Väinämöinen cannot speak through the ice; he is heard
singing, because he is a singer. Seimei, a master of paper wards, is not heard at all: his wards blow
in on the wind. Wayland is heard working - he is not allowed to stop - and the sparks off his hammer come in
on the forge's hot air, in fire's ramp, because the forge is built and built things burn. The other three are not people, so nothing speaks. A line says
something is there and roughly where; none says what to do (no key, no snuffing, no catching).

The sounds are generated (`tools/audio/sfx.py`, "chapter hints"), registered in `SfxCatalog`, and
described in `.ai/audio-manifest.md`, "Chapter hints". Played 300 px toward the source, inside
`SfxPlayer`'s 900 px audible radius, so the 2D listener pans them that way.

### Adding one

1. An entry in `StageHintCatalog` keyed by its event's site (`ForSite`; since P2 there is no stage id
   on a hint - the site's chapter is the one that plays it, and the hint comes from that event's
   place), a voice, a sound, a cue (`HintCue`; a new one is a row in `hint_cues.py` and its beats in
   `StageHintCue`), and its lines (60 characters at most: two lines on a phone). Every event needs one
   (`ValidateSideEventTable`), save the well and the moot (`StageHintCatalog.SitesWithoutHints`).
2. A sound in `tools/audio/sfx.py` beside the five, registered at -9 or -10 in category `hint`; run
   `python tools/audio/build.py hint_` and `analyse.py --confuse`, then check it against the whole
   set, not only its category (the first three drafts collided outside it). Add it to `SfxCatalog`,
   import it, and give it a manifest row.
3. Nothing else. `SideEventDirector` starts the director for any chapter one of whose events has a
   hint, and the director alternates them; `RegressionChecks.ValidateStageHints` checks the site, its
   one chapter, the sound, the line lengths and the no-names rule.

A hint about something that is not a side event - a stage secret, once they exist - needs
`StageHintDirector` taught where that thing is, and needs `.ai/stage-secrets.md` section 2 revisited
first: it says a secret gets no text and no pointer, and a hint is both a line and a direction.

### Photographing and driving it

- `_UiShot --force-hint` plays the next hint `--fx-lead` frames before the shutter instead of on the
  pace clock; every other rule holds, so pair it with `--fresh-save` (in memory, writes off), e.g.
  `--scene=res://scenes/node_2d_game.tscn --stage=1 --fresh-save --force-hint --frames=200 --fx-lead=40`.
  With `--take-wayfinder`, or `--free-wizards=tituba`, it reports that no hint played - both checked.
- `_BalanceHarness --fresh-save` runs on a brand-new in-memory save, so the hints play on their real
  schedule: `--stage=1 --minutes=14.5 --immortal --fresh-save` logged `StageHint: stage_1 hint 1 at
  pace 300s` and then 450, 601 and 750, each line in order.
- Every hint prints one `StageHint:` line with its pace time and distance.
- `_UiShot --hint-preview=STAGE_ID[,DEG]` plays that chapter's hint (sound, cue, line) on whatever stage
  is running, from 3000 px at DEG (0 east, 90 south), touching no schedule or save - for grounds no hint
  plays on: `--stage=9` (the Fallow Fields), `--stage=8` (the Sketchbook's paper). Add `--notes-seen=all`
  so the chapter note is not up beside it.

**Not verified: whether anyone hears them as intended**, or whether the cues read at a glance mid-fight
(photographed, never played). The sounds were measured (levels, `hf`,
distances to every other sound), not auditioned, and a formant cry is the hardest thing in the set
to judge by numbers. Whether 300/150 is the right rhythm, and whether a caption at the edge pulls a
player off his kiting line or is ignored, needs a playtest.

## Two events in one chapter (P2, 2026-11-02)

Five chapter wizards are held in chapters that already had an event (`.ai/chapter-wizards.md` 6.3), so a
chapter's line in `SideEventDirector` is a list, its original event first:

```
"stage_0" => new(WayfinderKind.Compass, new SideEventEntry("the_grove", () => new GroveEvent()),
                                         new SideEventEntry("walking_hut", () => new WalkingHutEvent())),
```

- **Placement** (`SideEventDirector.LayOut`, shared with the validator and the probe). Event 0 is placed
  exactly as before, on stream 1. Each later one has its own stream (3 to 6; `EventStream`), in the same 4-6
  chunk band, through the same `EventSite`/`EventSiteInBand`, with `ExplorePlacer.Avoiding`: the roll is drawn
  inside the arcs at least **90 degrees** (`MinSiteSeparationDegrees`) round from every earlier event's main
  site, and every spot the search or the band sweep tries is held to the same rule and kept off every earlier
  footprint, so the site it settles on keeps the rule, not only the roll.
- **The wayfinder** is one a chapter. It keeps 75 degrees off every event's bearing
  (`ExplorePlacer.BearingAvoiding`; with one event, exactly the bearing it always had) and marks every site of
  every event. The secrets keep off every event's bearing too.
- **The hints** are keyed by site (`StageHintCatalog.ForSite`). One `StageHintDirector` holds the chapter's
  events that have one, and each slot of the schedule (still four a run) plays ONE: the next event, round the
  chapter's order, that is still dormant, unmarked and paying something the save lacks. Two hidden events
  alternate; a found, marked or owned one drops out and the other has every slot after; each event's lines
  run in their own order. One caption and one cue, and a hint asked for while the last one's line is up is
  refused, so never two at once (before P2 a forced hint could start on top of another: the director held
  the screen's turn and Announcements handed it straight back).
- **The line under the timer.** Each event keeps its own objective line and arrow (`SideEventObjective`,
  `SideEventObjective2`), but only one speaks: the event most recently arrived at, or one whose win line is
  showing (`SideEvent.HoldsObjectiveLine`). A player who leaves one running and walks to the other sees the
  other's.
- **Everything else is per site, never per chapter**: the derived Road deed (`event_<site>`, one per event),
  `LifetimeStats.EventsWon`, `UnlockCatalog.ForDiscoverySite`, `DiscoveryRewards`, the map card
  (`StageRewards` lists every event's rewards), the chapter note's clues (`ChapterNotes.CluesFor`: each
  event's site clue, and every wizard held there by his held line - a note may now carry two of the place and
  one a wizard held there, `ChapterNotes.MaxLinesFor`), the quest log and the reveal rule (`ValidateSecret`
  reads the wizard's own site).
- **Checked** by `RegressionChecks.ValidateSideEventTable` (`RegressionChecks.SideEvents.cs`): no two events of
  a chapter on one site, no site in two chapters, at most five events, every event with a hint, and the
  90 degrees with every main site in band on 32 sampled layouts a chapter (lakes, and a maze lattice) - for
  every chapter that holds two, and for every chapter with a second appended, the rule its next event will
  meet. Shown to fire by taking `Avoiding` out (every chapter reported pairs as close as 0 degrees), and by
  four faults put in at once and taken out: two Forest events on one site, a Cave event with no hint, the
  stand-in's site written into the table (reported as a LEAK), and Seimei revealed by the Swamp
  (`ValidateSecret` now names the chapter that holds him and the reveal chapter's events).
- **The stand-in** (`SecondEventStandIn`): `--second-event-test[=STAGE]` on a tool scene only appends a copy
  of the grove at site `standin_second_event`, with a hint, a clue, deed words and a prize (the Quicksilver
  Bead, a shop boon a new save lacks), each answered by the lookup that would find a real one and never
  listed in a catalog. The game itself never gets it (`SaveManager.IsToolSceneLaunch`); the validator says so
  on the tool run that asked, and calls it a leak anywhere else. `_ExploreProbe --second-event-test` drives
  the two-event order (two hints, one each; the first event won without the wayfinder; the next two hints both
  the second's, though the rotation would have given the first of them back; no fifth; the wayfinder marks
  what is left; the second won). `--wayfinder-first` takes it first instead (both events marked, no hint
  after), and begins the second while the first runs, so the line under the timer is seen passing to the one
  begun last, back to the first for its win line, and on again. `_UiShot` takes the flag too, with
  `--event-index=N` and `--goto=event:N`.
- **A chest win records nothing**, as before P2: on a save that already owns what a site pays, `SideEvent.Succeed`
  pays the chest without `DiscoveryRewards`, so neither `EventsWon` nor the deed is written. Normally the deed
  was done when the thing was first won; a save that owns it some other way never records the event.

## Rules every event inherits

- **Nothing an event places joins `"enemies"` unless it is an enemy.** Every targeting path
  iterates that group, and an ice block or a lantern in it would pull the whole loadout off the
  swarm. This is the breakables rule, and **the thaw was built around it**: the ice is never a
  target. It listened to `Player.EnemyDamagedAt` and warmed from hits landed near it, which is how
  "only area damage and fire break it" worked without a breakable-object system (`StrikeListener` keeps
  that for every breakable). Since 2026-11-07 it is sung back on strings of ice (`.ai/unlock-puzzles.md`
  3.6) and listens to nothing: the strings are drawn, and only the player's feet are read.
- **Anything an enemy drops for the player to reach goes through `ReachablePoint`.** Enemies are
  not held to the bounds the player is. The first Gaoler's key fell where the Warden died, outside
  the player's clamp, and could not be picked up.
- **Nothing an event does is timed, and nothing it places moves.** Where it is comes from the
  seed (`PlaceSites`), when it starts from the player's feet (`ArrivalRadius`). A new event
  overrides `PlaceSites` only if one site is not its shape, and must reserve any ground it needs
  kept clear.
- **Event enemies are elites** (`IsMiniBoss`), so `RespawnEnemy` never recycles a key-carrier or
  the caravan master to the far side of the map.
- **`Enemy.ScriptedHeading`** overrides the chase. The caravan master walks its road with it.

## The rewards

### Two wizards, named people

`.ai/world-and-tone.md`: the starters are disciplines, and everyone rescued is a person.

- **Tituba**, taken from Barbados and jailed at Salem on an accusation. She opens on **Riptide**
  (the sea she was taken from), deliberately *not* a darkness or curse spell: she was a real woman
  wrongly accused, and the game does not agree with her accusers. Passive *Patience*: +15% duration.
- **Väinämöinen**, the singer of the Kalevala, who sowed the first forests. Opens on **Bramble
  Seed**. Passive *Song of Making*: +10% area.
- **Abe no Seimei** (2026-11-01), the onmyoji of Heian Kyoto, in the pale hunting robe over black. Opens on
  **Kindled Ward**, the Ruins' own vault spell. Passive *Exorcist*: +15% damage to elites, minibosses and
  bosses. The first chapter wizard (`.ai/chapter-wizards.md`): counted at the Throne's gate only.
- **Wayland the Smith** (2026-11-02), the master smith of the Germanic tales, in an iron-grey shirt and a leather
  apron, his hammered-iron wings folded on his back, a brace and a broken shackle on his lamed leg. Opens on
  **Iron Palisade**, the stakes he forged. Passive *Wings*: +10% move speed. Freed in the Emberdeep, the gate
  chapter, so only the Throne counts him.
- **Morgan le Fay** (2026-11-03), the enchantress of Avalon, the queen in the black barge: upright into the wind
  off the strait, her hair and a lichen-green mantle streamed back, the spell's rune of rime before her raised
  palm. Opens on **Sleet Runes** (Ice+Wind, R2). Passive *Avalon*: the first extra life a run spends heals to
  full, not half. Freed by keeping pace with her barge under the Last Bridge (`.ai/chapter-wizards.md` 4.9);
  counted at the Throne's gate only.
- **The Cumaean Sibyl** (2026-11-02), the prophetess of Cumae, old, hooded, a bronze jar of arcane light on
  her crook. Opens on **Arcane Explosion** - which, alone, keeps its own time until she holds a second spell.
  Passive *Prophecy*: +3 rerolls a run. Freed at the nine mouths; counted at the Throne's gate only.
- **Baba Yaga** (2026-11-03), the witch of the deep wood, tall, gaunt and iron-grey, a pestle in her hand with a
  thorned vine climbing it. Opens on **Thorned Vine** (two elements, R2), Elderbark's deed. Passive *Bone Fence*:
  +20% to every shove and pull her spells give. Freed by herding her walking hut into the bone yard, the Forest's second event;
  counted at the Throne's gate only. **Never secret**: the Forest is open on a new save, so she is a silhouette
  from the first run.
- **Paracelsus** (2026-11-03), the Swiss physician who wrote that the dose makes the poison: stocky, heavy-jawed, a
  wide flat beret, a wine-dark physician's gown, a flask held up to the light in one hand and his sword Azoth planted
  under the other. Opens on **Contagion**, the lanterns' spell. Passive *Sola Dosis*: +25% to every poison tick.
  Freed by leading him out of the gibbet's cage to the ferry landing, the Swamp's second event; secret until the
  Swamp opens, counted at the Throne's gate only.
- **Nostradamus** (2026-11-03), Michel de Nostredame, the physician and seer, in the square doctor's cap and a long
  lichen gown with a sable tippet, a brass armillary sphere raised in his hand and a written roll in the other. Opens
  on **Starfall**, Light (R1: Seimei and Merlin hold Light on other spells). Passive *Foresight*: every ordinary
  enemy's wind-up shows 15% longer. Freed by seeking his voice through the Court, its first event; secret until the
  Court opens, counted at the Throne's gate only. The last frame of the Gallery's portraits is always his.
- **Imhotep** (2026-11-03), architect of the first pyramid, physician and High Priest of Ra: a priest's white kilt, a
  bare brown chest under a broad collar of gold and lapis, a leopard's pelt over one shoulder, a close lapis cap, the
  was-sceptre planted, and the sun lifted on his palm. Opens on **Fireball**, the Long Coil's deed (Fire, shared with
  the Pyromancer: R1, their spells differ). Passive *Surveyor*: +20% pickup reach. Freed by lighting the three beacons
  round his dry tomb at once, the Sands' second event; secret until the Sands open; counted at the Throne's gate only.
- **Medea** (2026-11-03), the sorceress of Colchis, granddaughter of the Sun, drawn serious: upright, a madder mantle
  over a sea-green peplos, a bronze diadem, a small sun in her open hand and a bronze sickle in the other. Opens on
  **Solar Flare** (two elements, R2). Passive *Rejuvenation*: +20% healing from every source. Freed by paying the
  Tithe-Reeve's scale a level's XP, the Fallow Fields' second event; secret until the Fields open (`stage_9`),
  counted at the Throne's gate only.
- **Ma Liang** (2026-11-03), the boy of the Chinese tale whose brush made real whatever he painted, drawn young and
  serious: a short crossed jacket of washed indigo, a red sash, a rolled painting slung on his back, his brush held up
  and a crow of his light lifting off its tip. Opens on the **Familiar**, Arcane (R1: the Archmage and the Sibyl hold
  Arcane on other spells), and chooses its form on his run's first screen - only the crow has a final form. Passive
  *Paint What Is Needed*: once a run, below a quarter of his health, a flask is painted at his feet. Freed by tracing
  the door he sketched in the Gallery, the Court's second event; secret until the Court opens, counted at the Throne's
  gate only. The first frame of the Gallery's portraits is always his own.

Both wear **no player pigment**. The four saturated rows are the starters'. The rescued wear what
they were held in, so the only saturated thing on them is their staff head. Sprites are in
`tools/art/roster.py`; `head()` gained a `tone` argument so Tituba is not drawn in the light skin
tone the starters share.

`Player.GetLevelUpOptions` always offers a character its **own starting spell**, unlocked or not.
Both open on spells a save may not own yet.

### Secret wizards: sealed, then a silhouette, then free (2026-10-22)

A wizard's card on the character select is in one of three states (`CharacterSelection.CardState`):

| state | when | portrait | name | line under it |
|---|---|---|---|---|
| **Secret** | locked, and his reveal condition not met | a wax seal with a question struck in it (`sealed-portrait.png`) | ??? | "Not yet discovered." |
| **Locked** | locked, revealed | his **silhouette**, animated with his idle | ??? | his `LockedHint` |
| **Unlocked** | owned | the wizard | his name | spell and passive |

**The rescued wizards are secret until the chapter that holds them opens**: Tituba until the
Cursed Dungeon (`stage_1`, opened by beating Elderbark), Väinämöinen until the Frozen Waste
(`stage_5`), Abe no Seimei until the Mystic Ruins (`stage_4`), Wayland the Smith until the Emberdeep (`stage_7`), the Cumaean Sibyl until the Sunken Cave (`stage_2`), Morgan le Fay until the Last Bridge (`stage_11`). The reveal is data: `UnlockDefinition.IsSecret` and `RevealStageId`, set through
(`stage_5`), Abe no Seimei until the Mystic Ruins (`stage_4`), Wayland the Smith until the Emberdeep (`stage_7`), the Cumaean Sibyl until the Sunken Cave (`stage_2`), Imhotep until the Scorched Sands (`stage_6`). The reveal is data: `UnlockDefinition.IsSecret` and `RevealStageId`, set through
`DiscoveryCharacter(..., secretUntilStage:)`. Why that condition:

- Before the chapter opens the player cannot act on anything about him, and the stage list does
  not show a closed chapter at all (`StageSelection`: "a chapter the player cannot enter is not
  listed"). A silhouette with "Freed from a cell in the Cursed Dungeon." on a fresh save names a
  place the game has not shown yet.
- The chapter's card says it the moment it opens - "He keeps a wizard in one of these cells.",
  "A wizard lies under the ice" - so the character select now reveals him on the same unlock that
  puts that line on screen.
- **It is derived, not stored.** `GlobalStatsManager.IsCharacterRevealed` reads
  `IsStageAvailable` for the reveal chapter, so there is no "revealed" flag, no schema bump and
  nothing to migrate. A save that already owns him shows him unlocked whatever is open.

**The name stays hidden while he is locked.** The silhouette is the tease and the name is the
reward: the rescue line reads "Wizard freed - Tituba", and the rescued are people
(`.ai/world-and-tone.md`), not a class a player could read off the coat. The starters never lock,
so this only ever hides a rescued name.

**The silhouette** is built at runtime from the sprite's alpha (`CharacterVisuals.BuildSilhouetteFrames`):
every opaque pixel filled with occlusion `#05070C`, the edge pixels lit as a rim in floor stone -
`hi #3A4658` where the open side faces the key light (up or left), `lit #2A3444` elsewhere. No
pigment, no staff glow, no face. The rim is what makes it read: the old locked card multiplied the
wizard to near-black, about 1.2:1 against the card, and read as a hole.

**The seal** is generated (`tools/art/props_fx.py`, `sealed_portrait`): the mutation card's red
wax at portrait size, on the 48 cell at 1:1 so it sits on the same pixel grid as the wizards. It is
deliberately not the hooded unknown figure, which is a silhouette of *somebody*.

`RegressionChecks.ValidateUnlockCatalog` (`ValidateSecret`) warns when a secret has no reveal
chapter, names one `StageCatalog` does not define or that is not playable, is revealed by the
campaign-gated finale (which needs every wizard first), is a spell or a starter (nothing reads
`IsSecret` there), reads as revealed on a brand-new save, or - for a Discovery wizard - is revealed
by a chapter whose side event is not the one that pays him out. All of these were checked by
pointing the two at a missing chapter and at the wrong one.

**Not built: a once-only reveal moment.** A beat the first time a sealed card opens needs to know
it has been seen, which is a save field, a schema bump and a migration - and schema 11 is already
planned for secrets in the world (`.ai/stage-secrets.md`). If it is wanted, add a
`SeenRevealedCharacterIds` list in that bump rather than a separate one.

Photograph the three states without touching the real save (`_UiShot`, in this order):
`--fresh-save` (new save, writes off), `--open-stages=stage_1,...` (opens chapters, which reveals),
`--free-wizards=tituba` (unlocks), and `--hover-late=CharacterButton6` to put that card in the PC
detail pane - a locked card cannot take focus, so a hover is the only way to show one there.

### Three spells, one per element that had no pure spell

Metal, Poison and Grass had no pure spell, and only a pure spell can carry an element to its
capstone (see the element-tag rules in `.ai/gameplay-design.md`). Each new spell closes one, and each is a
shape the roster did not have:

- **Iron Palisade** (Metal): a line of stakes between you and the nearest pack, which shoves back
  and cuts whatever walks into it. The **wall** #64 listed as missing. It is not a physics body,
  which would need a new collision layer and would also stop the player. Like Hollow Star it rides
  `ApplyKnockback`, so boss resistance applies.
- **Contagion** (Poison): one carrier; when a marked enemy dies, the plague jumps to everything
  within reach. **It spreads on death, by itself.** It is bounded by a per-cast cap of 24 infected,
  and by marks lapsing.
- **Bramble Seed** (Grass): planted at your feet, it **grows** from 16 px to 110 px over six seconds.
  Every other zone is at full size the moment it appears. At most three stand at once. It is drawn
  as thorned roots writhing out of the ground, laid out from the same radius the damage reads, so
  it cannot be drawn wider than it hurts (`.ai/spell-roster.md`, section 2a).

## The final chapter's gate

**Superseded at schema 19** (`.ai/achievements-and-secrets.md` 2.5 and section 11): the Emberdeep opens
on the Long Coil beaten and every wizard whose rescue comes before it, not on spells; and each event's
unlock is now the reward of that event's Road deed (`event_<site>`), granted the moment it is won as
before. What follows is the history.

The Emberdeep opens on "every spell recovered and every wizard freed", evaluated live, so the five
discoveries are now part of it. All five events sit in chapters before it. **Schema 10** credits
the discoveries to any save that had already met the gate, so nobody loses a chapter they had
opened. The schema-8 grant of "every wizard" now skips Discovery wizards, which did not exist when
everything was free.

## Verified, and not

Driven headless by a throwaway probe that walked to the pointer and killed what was near. All
five ran to completion and granted the right unlock, with the user's save backed up and restored
around every run. The thaw took 26 s and the grove 22 s. Each found spell dealt damage in a live run. A
frame of four events was captured and every one draws. The migration was checked on in-memory
saves.

**Not verified: whether any of it is fun, or the numbers.** Heat needed for the thaw (900), the
grove's 20 s, the master's health (26 times ordinary spawn health) and walking speed, and the
three-hit drop are first guesses. The probe hit for 400, so its times say the logic completes, not
how long a real run takes.
