# The light pool: the wizard is the lamp, in the chapters with no other light

Eric, 2026-11-03, after studying the genre (`.ai/genre-lessons.md` section 1, "the lesson that matters
most"): a warm pool of light on the wizard that falls off into the dark at the screen's edges, with the
enemies walking out of the dark into it. Then:

> "The light pool can possibly be used in certain levels? like the dungeons"

So it is **per chapter**, not global. It is on where the wizard's own light is the only light - under
ground and indoors - and nowhere under open sky. The premise already said it (`.ai/world-and-tone.md`):
*"You carry the only warm light on screen. Everything else glowing is something you made."* The title
screen draws it; until now the run did not.

Built 2026-11-03 on `wip/light-pool` as a **prototype**: on in the Cursed Dungeon (the pilot) and the
Sunken Cave, off everywhere else. Sections 2 and 3 say how it works; section 4 is the readability
guardrail, measured; section 7 is the proposal for the other indoor chapters; section 9 is what needs
Eric's eyes.

---

## 1. Where it is on

`StageEnvironmentProfile.Darkness` (0 to 1) in `StageEnvironmentCatalog`, one line per chapter:

| Chapter | Environment | Darkness | Why |
|---|---|---|---|
| Cursed Dungeon (`stage_1`) | `Castle` | **1** | the pilot: a gaol under his house, and nobody has lit it |
| Sunken Cave (`stage_2`) | `Cave` | **1** | its chapter note already says *"my light the only light"* |
| Forest, Fields, Heath, Swamp, Ruins, Frozen Waste, Sands, Bridge | | 0 | under open sky; they stay as they are |
| The Sketchbook (`stage_8`) | `Sketchbook` | 0 | the kids' chapter; never touched (`--light-pool=X` refuses it too) |
| The Emberdeep, the Court, the Throne | | 0 | proposed in section 7, and declined: the pool stays in two chapters (Eric, 2026-11-03: *"Keep to two"*) |

`LightPool.Validate` (run by `RegressionChecks.RunAll`) reports an open-air chapter or the Sketchbook given
any darkness, a darkness outside 0 to 1, a missing shader, and the bodies' floor falling below the room's.

## 2. How it works

`scripts/LightPool.cs`, `scenes/shaders/light_pool.gdshader`, kindled by one line in `Node2DGame._Ready`
(`LightPool.Kindle`, after the ambient overlays). **One shader, one material, one quad the size of what the
camera sees (with 30% to spare for shake and push-in), drawn by two nodes at two depths:**

| Pass | Depth | What it darkens | Floor | Warm lift |
|---|---|---|---|---|
| **The room** | `DrawLayers.RoomDark` (-18): over `Terrain` (-100) and `Decal` (-20) | the floor, the walls, the water, the decals | **0.24** | yes |
| **The bodies** | `DrawLayers.Bodies` (0), placed 4000 px below the screen so the y-sort draws it after every body | enemies, chests, XP orbs, props, trees, walking familiars | **0.66** | none |

**Two passes, because one would cost readability.** The title screen learned it first (`.ai/title-screen.md`,
"The undergrowth is TONED"): *a bush only reads as a bush if its lit side is lighter than the ground it
stands on*, and a falloff run over everything alike comes out as more of the same dark. Here the room goes
about three times darker than the bodies on it, so an enemy at the screen's edge stands out from its floor
**more** than it does with no dark at all (section 4).

**Never darkened, by depth rather than by rule:** the wizard and every boss (`Foremost`), every projectile,
blast and enemy bolt (`Air`), a boss's arrival dust and light (`ForemostAir`), every telegraph icon
(`AttackIcon`), every damage number (`FloatingText`), every ground tell, crater, pool, hazard and side-event
site (they sit at -17 and up, over the room pass), and the whole HUD (its own canvas layers).

**The blend.** `blend_premul_alpha`: what lands is `warm + dst x m`, where `m` is the multiplier - 1 in the
light, the pass's floor in the full dark - so one quad darkens and lifts at once. Nothing reads the screen:
no `hint_screen_texture`, no back-buffer copy, which on the phone's tiled GPU would force the frame out of
tile memory. Which pass a fragment belongs to rides in the quad's vertex colour (r the floor, g the warm
lift), so the two passes share every uniform and the lights are uploaded once a frame.

**The shape**, in shares of half the canvas's short side in world units (360 canvas units at the run's zoom,
424 world units at 0.85). Both canvases share that short side and their diagonal - 1280x720 is 720x1280 turned
- so the pool is the same size in the world on a PC and on a phone:

| Knob (`LightPool`) | Value | In world units | What it is |
|---|---|---|---|
| `InnerShare` | 0.55 | 233 | full light out to here: the ring a level-one swarm presses on |
| `OuterShare` | 1.75 | 741 | the floor reached here: just inside a PC's left and right edges, a phone's top and bottom |
| `WarmReachShare` | 0.75 | 318 | how far the warm lift carries |
| `RoomFloor` | 0.24 | | the room's multiplier in the full dark |
| `BodyFloor` | 0.66 | | the bodies' |
| `Warm` | (0.095, 0.066, 0.018) | | gold's deep stop (`#2E2208`) at about half, added to the room at the heart |

On a 1280x720 canvas the short edges' midpoints keep about 0.83 of the room's light and 0.93 of the bodies';
the long edges and the corners are at the floors. The size is fixed in the **world** from the run's zoom on
the first frame, so a boss's push-in brings the camera closer to the light rather than shrinking it.

**The warm lift is held to half of gold.deep on purpose.** It lifts the ground the fight stands on: at 0.8 of
gold.deep the pool's body-to-ground contrast fell from 17 L\* to 10; at half it is 14 and the pool still
reads warm. It is never on a body, so no enemy is tinted by it - the genre lesson's warning about a single
colour cast muddying the edge (`.ai/genre-lessons.md` section 1, "What not to take").

## 3. Warm spells are lights too

`LightPool.Light(at, radius, brightness)`, called from a warm effect's own `_Process` every frame it is to
show. Up to **16** reach the shader a frame (`light_0` .. `light_15`: sixteen plain uniforms, because an array
uniform is uploaded as a packed array and a Variant of one carries a finalizable disposer - MireSurface's
lesson), culled to the screen, the weakest (reach x brightness) giving way when there are more. Nothing is
registered and nothing freed: a light is one struct written into a fixed array, read and cleared once a frame
by the room's node, which runs at `ProcessPriority` 1000 so it reads after the spells. Where the chapter is
lit evenly the call is one static read. A light lifts both passes toward 1 round it and adds the warm lift
to the room under it. `LightPool.ColdLight` is the same with no warmth (the sign of the shader's w): his
light, not the wizard's - today only an arriving boss's (section 5), and the proposed sconces (section 7).
While a menu pauses the run the lights it had stay up (nothing asks for them, and clearing them would put
out every fireball under a level-up card); the pool's own node runs paused only to ease Reduce Darkness.

Wired in the prototype:

| Source | Reach | Brightness |
|---|---|---|
| Fireball in flight (`Fireball`) | 4.5 x the drawn ball (~95 world units) | 0.9 |
| Every fire burst (`FireburstLayer`: Fireball, Cluster Bombs, Hellfire, the sun and gold styles) | 2.2 x the blast | fades with the burst |
| Cinderbreath's tongues (`FlamePuff`), unless Hellbreath turned the fire black (since 2026-11-08) | 3.2 x the hit radius | 0.75, guttering |
| Kindled Ward's motes (`KindledWard`), unless Rimelight took it into the ice | 90 x the ward's growth | 0.8 |
| Solar Flare's burst (`SolarFlareVisual`) | 1.6 x the reach | fades with the burst |
| Meteor Swarm (`MeteorImpact`): the falling star, the impact's flash, the magma crater | 2 x the blast | grows as it falls, flashes, 0.45 while it burns |

**Decided 2026-11-08 (Eric: "fire and light spells will act as light sources"): wire every one below.**
~~Not wired yet: Scorching Ray, Starfall, Molten Shard, Orb of Ages, Spiritual Weapon, Sagittarius, Aegis Ward,
the ground fires (`EmberGround`, `CinderGround`, `CraterLayer`'s fire, `BrimstoneLayer`), the Dungeon's flame
vents (`StageHazard`), and The Taking's lights (`TakenLight`).~~ Wired 2026-11-08, every one from its own
`_Process`. Reach in world units; a brightness that "fades" follows what is drawn, so no light outlasts its
effect.

| Source | Reach | Brightness |
|---|---|---|
| Scorching Ray (`ScorchingRayBeam`): a string of lights down the beam, the strike, then the scorch | 64 along the beam (x1.5 at full heat, x1.5 again under Sunlance), 1.4 x that at the strike; 56 the scorch | 0.6 along, 0.85 the strike, both with the beam; 0.5 cooling with the scorch's glow |
| Starfall (`Starfall`), unless Frostfall repainted it in ice: the falling stars, every landing, a heavy star's fuse, the shooting stars, the Constellation's lit stars | 56 falling (72 heavy, 40 a skip); 2.2 x the radius a landing struck; 72 a shooting star; 48 a lit star (60 held by Sagittarius's draw) | 0.55 falling; 0.8 flashing out with the landing (0.5 a spark's); 0.5 to 0.9 as a fuse swells; 0.75 shooting; 0.45 lit (0.6 held) |
| Molten Shard (`MoltenShardLook`): in flight, lodged (Magma Core), held in the forged ring | 3.5 x the shard's drawn half-length | 0.6; a lodged core 0.5 heating to 0.9 |
| Slag Trail and Caldera (`MoltenSlag`) | 44 along the slag's live stretch; 1.8 x the pool as it wells | 0.5, dimming as the newest slag cools; 0.6, out over the pool's last second |
| Orb of Ages (`OrbOfAges`) | 72 x the orb's growth with the ward | 0.8 until it is home |
| Spiritual Weapon (`SpiritualWeapon`), unless Mjolnir traded its Light for Lightning: every weapon and the dagger, Hallowed Ground, Excalibur's line | 4 x the drawing's grip-to-middle (104, 224 colossal, 68 the dagger); 1.4 x a holy floor; 56 along the line | 0.6 with the weapon (0.5 the dagger); 0.45, out over the floor's last half second; 0.7 fading as the line breaks up |
| Sagittarius (`Sagittarius`, `HuntersArrowLook`): the string between the two lit stars it spans, and every gold arrow of the loose; its stars are Starfall's, above | 48 along the string; 64 an arrow | 0.25 rising to 0.7 with the draw; 0.6 |
| Aegis Ward (`AegisWard`): the stretch of dome a bolt breaks on, Reflective Plating's shaft (Reforged's shards are iron, and light nothing) | 64 the dome; 48 along the shaft, 62 where it lands | 0.7 fading with the flash; 0.6 and 0.75 with the shaft |
| Cinderbreath's ground (`CinderGround`), unless Hellbreath: Ember Wake's embers, Dragon's Breath's wall, Backdraft's blaze | 2.5 x an ember; 64 along the wall; 2.2 x the blaze | 0.45 guttering out; 0.7 catching and going out with the wall; 0.8 dying over 0.8 s |
| Meteor craters (`CraterLayer`): every crater, the skips' and Breakup's included, after the meteor has gone | 1.6 x the crater | 0.55 x its molten heat (`CraterGround.HeatAt`, what the bowl is drawn with) |
| Brimstone (`BrimstoneLayer`): each stone, and each spit in flight (its burst is `FireburstLayer`'s) | 90; 40 | 0.8 x the stone's glow, throbbing, flaring as it spits, cooling; 0.5 |
| The flame vents (`StageHazard.LightReach`, 120 on `FlameVent.tscn` and 0 on the spikes; `ValidateStageHazards` reports a vent at 0) | 120 | up to 0.35 with the tell, 0.8 guttering while it burns, out as it cools, nothing asleep |
| The Taking's lights (`TakenLight`); its lanes are his dark light, and light nothing | 64, or 2.5 x its pyre if wider, x the mote's swell | 0.7 x the mote's own alpha: its gutter, its flare when caught |

Found by the same sweep and wired, a line each:

| Source | Reach | Brightness |
|---|---|---|
| Cataclysm, the Pyromancer's Apotheosis (`CataclysmField`) | 1.6 x the crater | 0.5 x its heat |
| Hollow Star under Eclipse (`HollowStar`), its Light path | 1.8 x the collapse radius | 0.35 rising to 0.75 with the well's heat |
| Toxic Spore Burst under Ember Caps (`SporeMushroom`), which trades Poison for Fire | 1.6 x the cap | 0.5, out with its flames |
| Solar Flare's Sun Dogs, Solar Prominence and Dragon's Teeth (`ElementalPulse`) | 64 a sun dog; 56 a loop in flight, 2 x its burst landing; 48 a stone in flight, 1.4 x its strike landing | 0.6; 0.7; 0.6, then 0.7 fading |

How they are wired, and the rules a new one follows:

- **`_Process`, never `_PhysicsProcess`.** A light pushed from the physics step skips every frame without one and
  flickers on a fast screen. Orb of Ages, Aegis Ward, the vents and The Taking's lights move there, so each has
  a small `_Process` of its own that pushes the light from where the physics left it.
- **A line is `LightPool.LightAlong`**: up to `AlongLights` (4) lights at the middles of equal stretches, each
  reaching the stretch's length if that is more than asked, so a long beam is one band and not beads.
- **The element the effect is drawn in decides.** `LightPool.IsWarm(element)` where a path repaints it (Starfall's
  Frostfall, Hellbreath, Rimelight); `LightPool.CarriesWarm(spell)` - the spell's elements after its trades -
  where a milestone trades Light away (Mjolnir). Both read once per cast or per node, never per frame.
- **Lit evenly, nothing runs.** Every loop that shed lights is behind `LightPool.Lit`, one static read.
- **The sixteen are contested now.** A late run in the Dungeon asks for more than sixteen a frame; the weakest
  (reach x brightness) give way, so embers, spits and lit stars go first and a fireball or a beam stays.

Not lit, on purpose or not yet: `EmberGround` no longer exists (Ember Wake's embers are `CinderGround`'s;
`FlamePuff.EmberGround` is a `CinderGround`); Corona Discharge's sunbeams (drawn by `SolarFlareVisual`, which
lights only its burst); Sun Aegis's flash (`Player.SunAegisFlash`, an `ImpactDebrisLayer` burst with no node to
hold a light); the hammer's slam (the hammer lights its own landing); the Brazier's embers (`StanceGlow`, an
item set's, at his feet, inside his own pool); and `ImpactDebrisLayer`'s debris, which every element shares.

## 4. Readability, measured

`_UiShot --light-probe` (`scripts/_UiShot.LightPool.cs`): the wizard held still and silent (nothing he casts
lights anything), the field cleared, and the chapter's own enemies stood, held, in four places - in the pool
(150 from him), at the middle of the screen's short edges, at the middle of its long edges and in its
corners - with XP orbs between them. The frame is photographed twice, the tree paused between: as it is, and
with every placed body and orb hidden. The pixels that differ are the body's; the second frame is its
ground. CIE L\* (0 black, 100 white): **dL\*** is how far a body stands out from the ground it stands on, the
readability number; **hi** is the same for its brightest tenth (the rim, the eyes, steel); **eye** is the L\* of
its blue eye-light pixels.

    "$GODOT_BIN" --path . --resolution 1280x720 scenes/_UiShot.tscn -- --scene=res://scenes/node_2d_game.tscn \
        --stage=1 --character=1 --terrain-seed=4242 --frames=300 --light-probe --no-save-writes [--no-light-pool]

(and `--resolution 540x1212 -- --portrait --safe-area` for the phone). Off / on, body dL\* (hi dL\*, eye L\*):

| Zone | Dungeon, PC | Dungeon, phone | Cave, PC | Cave, phone |
|---|---|---|---|---|
| pool | 17.4 / 14.3 (60 / 58, 41 / 41) | 17.1 / 13.4 (61 / 56, 41 / 41) | 19.0 / 17.1 (61 / 60, 40 / 40) | 21.4 / 15.9 (63 / 58, 40 / 40) |
| short-edge middle | 12.9 / 13.1 (52 / 50, 46 / 42) | 14.6 / 15.5 (52 / 52, 44 / 39) | 11.5 / 13.1 (55 / 54, 38 / 35) | 17.2 / **20.1** (63 / 62, 30 / 26) n=1 |
| long-edge middle | 10.1 / **15.7** (58 / 47, 44 / 46) | 8.6 / **13.7** (50 / 41, 56 / 43) | 12.4 / **17.0** (57 / 49, 31 / 28) | 15.0 / 16.0 (55 / 44, 37 / 32) |
| corner | 3.0 / **8.8** (55 / 41, 61 / 43) | 15.6 / **19.3** (60 / 47, 50 / 46) | 7.8 / **9.3** (46 / 37, 30 / 28) | 10.8 / **13.3** (52 / 42, 31 / 30) n=1 |
| XP orb, long edge | 29.1 / **40.9** | 35.8 / **41.8** | 26.6 / **37.4** | 41.2 / **46.6** |

What it says:

- **At the edges every body reads at least as well as with no dark at all, and mostly better** - long edges
  +1 to +6 dL\*, corners +2 to +6, short edges +0 to +3 - because the room falls three times as far as the
  body standing on it. The floor on the darkness is the bodies' 0.66; the room's 0.24 is what makes it work.
- **The highlights dim but stay 37-62 L\* above their ground**, and the eyes stay 20-40 L\* above theirs (the
  ground at the long edges is L\* 2-6). Without the dark the eyes stood 12-42 above a lit floor: the eye-light
  is the cold mark the premise gives every creature, and in the dark it reads as one.
- **XP orbs gain 5-12 dL\* at the edges**: a bright gem on a floor that has gone dark.
- **The one loss is in the pool**: 2-5 dL\* from the warm lift on the floor under the fight (it stays 13-17).
  The lift was cut to half for this (section 2); cutting it further is the lever if the pool ever feels muddy.
- Where a zone says n=1 the Cave's rock refused the other placements; those cells are one body each.

## 5. The guardrails

- **The HUD is never darkened**: it is on its own canvas layers (`UIOverlay` and up); the passes are in the
  world.
- **The low-health warning stays distinct.** It is a flat red pulse on `UIOverlay`, over the dark, not under
  it; a red pulse over a dark edge reads as red, never as more dark. The damage flash the same.
  `_UiShot --light-probe-low-health` photographs it.
- **A boss never arrives invisible.** Two things, one by depth and one by light:
  - its whole node is `Foremost`, its arrival's dust and light are `ForemostAir` and its tells are on the
    ground over the room pass, so no part of a boss is ever darkened;
  - but a dark boss on a dark floor is still dark on dark - the Hollow Choir's voices, the first time they
    were shot in the Cave, stood in black - so **every arriving body carries a cold light of its own**
    (`LightPool.TickReveals`, from `BossEnemy.Arriving`): reach 280, at most 0.85, up over 1.2 s from the
    arrival's first frame and down over 1.6 s once it has landed, smoothstepped both ways. It is **cold**
    (`ColdLight`, a negative w in the shader: it lifts the dark and lays no warmth), so the only warm light on
    screen stays the wizard's.

  **It keeps the cinematic's timing and moves nothing** (Eric, 2026-11-03, the cinematic rule: hold still
  before any pan and before play resumes, never faster than the eye). The cinematic is
  `Node2DGame.BossEntrance.cs`: the bars close and the camera holds (`BossEntrancePanDelay`, 0.65 s), then a
  critically damped pan settles on the boss in about a second, then eases home when the hold lets go. The
  reveal is a fade, not a cut: about half-way up when the pan starts, full before it settles, and going out
  over the camera's ease home. The wizard's own pool is fixed to him (held still for the whole hold) and
  sized in the world, so the push-in and the pan cannot move it, resize it or snap it. Checked with
  eight-frame bursts across the Gaoler's and the Choir's whole arrivals (`boss-hold-pc-sheet.png`,
  `boss-cave-hold-pc-sheet.png`, and the phone's `boss-*-phone-on-sheet.png`).
- **Pickups and chests** are bodies and take the bodies' gentle floor; XP orbs measured above.
- **Reduce Darkness** (Options and the pause screen's Display section, `DisplaySettings.ReduceDarkness`,
  saved as `SaveData.ReduceDarkness`, kept by a story reset): the dark lifts to `ReducedDarknessScale` (0.4) of
  itself - reduced, not off, as Reduce Screen Shake is. The strength eases at 2.5 a second, so the switch on
  the pause screen fades the run being played rather than snapping it. Set the scale to 0 for a true off.

## 6. What it costs

Measured 2026-11-03 on the desktop the harness runs on (the i7-1065G7 laptop), the Cursed Dungeon at the
full swarm, two minutes each, with and without `--no-light-pool`, headless (CPU) and in a window at the
phone's canvas (draw calls):

    "$GODOT_BIN" --headless --path . scenes/_BalanceHarness.tscn -- --minutes=2 --character=1 --immortal \
        --timescale=1 --stress-swarm --profile=10 --stage=1 --seed=11 --no-save-writes [--no-light-pool]
    "$GODOT_BIN" --path . --resolution 540x1212 scenes/_BalanceHarness.tscn -- --portrait ... (the same)

| | pool on | pool off |
|---|---|---|
| the pool's own CPU (`light=` section: the camera read, the uniforms, the lights) | **24-33 us a frame**, in one call | - |
| its allocation | **0 KB a frame** | - |
| draw calls, phone canvas | 152 | 150 |
| frame mean, p95 | not comparable | not comparable |

**The frame totals are not a measurement here.** Five other sessions' Godots were running on the laptop
throughout, and the "off" runs happened to be the slower pair (21 fps headless against 46) - the load moved
by more than anything the pool could cost. The section and the draw count are what the pool itself adds:
under 0.2% of a 60 fps frame on the CPU, and two draw calls.

**The GPU side is not measured.** It is two quads the size of the screen, blended in tile memory (no
screen read, no back-buffer copy), each fragment testing the wizard's pool and up to 16 small lights, early
out at `light_count`. On the Pixel 9 that is 2 x 2.6 M fragments at its native 1080x2424. Measure it with
the release measurement build of `.ai/performance.md` section 6, `--stage=1`, with and without
`--no-light-pool`, reading `render=` against `draws=`. If it shows, the cheapest levers are, in order: the
bodies' pass only over the outer ring (it is 1.0 out to 233 units), half-resolution, fewer than 16 lights.

## 7. Proposal: the other indoor chapters

**Decided 2026-11-03: none of them.** Asked whether the Court should have the pool too, or it should stay in
the Dungeon and the Cave, Eric answered *"Keep to two"*. The table below is kept as the record of what was
proposed and why, should any of them be asked again; the Emberdeep's lava and the Throne's `DarkVeil` notes
still hold if they are.

Shot with `--light-pool=1` (the tool flag; it refuses the Sketchbook), 1280x720, four seconds in,
`pair-propose-*-pc.png` (off on the left):

| Chapter | Propose | Why, and what it needs first |
|---|---|---|
| **The Court** (`stage_12`) | **yes, 1** | His house with nobody in it: corridors and rooms, and the wizard's lantern down a dark hall is the chapter's whole mood ("Tables laid for a feast nobody comes to. Cold wax"). Nothing in it needs to glow that the pool would put out. Its sconces of his cold fire (`court_wall`) want to be **cold lights** (`ColdLight`, already there) so the halls are not uniformly black between them - one line per sconce where `CourtRules` places them. |
| **The Emberdeep** (`stage_7`) | **yes, at about 0.6, after one change** | A forge under the world, and it should be dark - but **its lava is a light and the room pass darkens it** (the shot: the lava fields at the screen's edge go dull red). Before it is on, the lava must stay lit: either its overlay layer drawn above the room pass in that chapter, or its tiles' centres fed in as cold-free warm lights (too many for 16; the layer move is the one). Its warm lift then has to be weaker, since the whole chapter is fire-lit. |
| **The Throne** (`stage_13`) | **no, or at most 0.4** | It already has its dark: the dark wizard's second phase puts the hall out but for him and the player (`DarkVeil`, `.ai/endgame-castle.md` 6.4). A pool from the start would spend that moment before it comes; the phase change only lands if the hall was lit before it. If it is ever given one, `DarkVeil` should take the pool's place in phase 2 rather than stack on it. |

Not proposed: the Bridge (the open sky over the abyss), and every open-air chapter, as asked.

## 8. Tools

| Flag | Where | What |
|---|---|---|
| `--no-light-pool` | any run | the pool kept out: the "before" of every comparison |
| `--light-pool=X` | any run | the pool at darkness X in any chapter but the Sketchbook |
| `--light-probe` | `_UiShot` | section 4's measurement, printed as `LIGHT` lines; also a `lo dL*` per body, and `LIGHT layer` lines for the detail layer, the bushes and the trees, peeled one at a time and measured the same way (the foliage pass, `.ai/terrain.md` section 13). After a `--teleport` it waits for the ground there |
| `--light-probe-low-health` | `_UiShot` | the probe, the wizard pinned at a fifth of his health |
| `--reduce-darkness` | `_UiShot` | the Reduce Darkness switch on, in memory |
| `--profile` | any run | the frame profiler; the pool's own CPU is its `light=` section |

The shots are in `_shots/light-pool/` beside the repo (not in it).

## 9. What needs Eric's eyes

**Decided 2026-11-08 (Eric):** items 1 and 4 - the depth stays as the preview pictures show it ("preview pictures
looked good as it is"), and Reduce Darkness stays at 40%, not off. And section 3's unwired warm spells are all to be
wired: every fire and light spell is a light source (done the same day; section 3's second table).

1. **How dark.** `RoomFloor` 0.24 and `BodyFloor` 0.66 were chosen for readability first. The swarm at the
   edge is deliberately brighter than the floor it stands on - lit figures coming out of a dark room, not
   silhouettes in a black one. Lowering `BodyFloor` toward 0.55 is darker and moodier and still measures at
   or above the no-dark contrast at the long edges; below 0.5 the validator objects.
2. **The warm lift.** Half of gold.deep reads as lamplight on the Dungeon's stone; on the Cave's blue rock it
   is fainter. Its cost is the 2-5 dL\* in the pool (section 4).
3. **The Dungeon's blue sconces** sit on the jail walls as plain sprites in the dark. His cold fire could
   cast a small cold light each (`ColdLight`), as proposed for the Court; not done, because where the jail
   walls put them is the wall painter's, not the pool's.
4. **Reduce Darkness keeps 40%.** A player who finds it too dark may want it gone; `ReducedDarknessScale = 0`
   is a true off.
5. **The boss reveal** (section 5): a cold pool round an arriving boss, gone after it lands, so the fight
   itself is on a dark floor with the boss above the dark. Whether it should stay with the boss through the
   fight is a design call.
6. ~~**The proposals** (section 7).~~ Decided 2026-11-03: declined, the pool stays in the Dungeon and the Cave.
7. **Not seen on the phone.** Everything here was shot on the desktop at the phone's canvas; the Pixel 9's
   own frame cost and how dark it looks on an OLED in daylight are not checked.
