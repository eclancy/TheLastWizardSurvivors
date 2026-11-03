# Cutscenes: keeping the Archmage at the centre of the story

Eric, 2026-11-02: *"We need to add cutscenes to help the player identify with the blue robed character.
Include a cutscene where the blue robed wizard frees the four starting wizards."*

The blue-robed wizard is the figure on the title screen, the one the dark wizard did not take - the game
is named for him (*The One He Missed*), and as a playable starter he is **the Archmage** (Arcane, opening
on Magic Missile, first on the character select; built on `wip/protagonist`). Read `.ai/prologue.md` first:
every cutscene here is a reel of that player (`scripts/Prologue.cs`), drawn on its frame, with its pieces and
under its rules. `.ai/world-and-tone.md` says why he is alone, why he is the light, and why a freed wizard
is a person and not a pickup.

## The cinematic rules (every cutscene and cinematic beat)

Eric, 2026-11-03: *"Make sure we use things like speed lines, screen shake, etc to really bring out the
cinematics when appropriate in cutscenes. Always give a pause before panning, or beginning play after a
cutscene, and don't move too fast, so the player can keep up visually."* These bind the prologue and its
reels, the freed reels, the lesson reels, boss entrances, the defeat and the ending.

1. **Punctuate the impact beats.** A landing, a chain breaking, a killing blow, a door slamming gets speed lines,
   a short screen shake, a flash or a hit-stop: whichever the moment calls for, and only where it does. Calm
   beats stay calm.
2. **Shake always goes through Reduce Shake** (`DisplaySettings.ShakeScale`): a player who turned it down never
   gets a full shake from a cutscene.
3. **Hold before any pan.** The picture is still for a beat before the camera moves (the boss entrance's
   `BossEntrancePanDelay` is the model, `.ai/enemy-behaviour.md`).
4. **Hold before play resumes.** After a cutscene or an entrance, a beat passes before the player has control
   and before the swarm moves again.
5. **Never faster than the eye.** Pans and zooms ease in and out and keep under a speed the eye can follow; no
   snap cuts of the camera mid-beat.

### How each beat keeps them (2026-11-03, `wip/cinematics`)

Two reusable pieces, both code-drawn, allocation-free per frame and pixel-art honest (opaque ramp stops on a
grid, never a blend): **`SpeedLines`** (a ring of streaks out of a subject or in on it, stepped one rect per
grid row on the world's 2 px or the panel's x2, two stops of a ramp, gone by getting shorter and fewer) and
**`DitherFlash`** (a screen of opaque light pixels through the 4x4 Bayer matrix, thinning level by level).
Every shake is a `ScreenShake.Kick`, which reads Reduce Screen Shake; `ScreenShake.Step` gives a camera-less
offset for the prologue's panels. Every hold is a named constant, and `RegressionChecks.Cinematics.cs`
(`ValidateCinematicRules`, `ValidateArrivalClimaxFramed`) says when one drops under its floor.

| beat | impact (rule 1) | holds (rules 3, 4) | speed (rule 5) |
|---|---|---|---|
| **Boss arrival** (`Node2DGame.BossEntrance.cs`) | the climax: speed lines out of the body in `EntranceFlareElement`'s ramp (violet for the dark wizard), with the shake, bar jolt, push-in kick and flare it had | 0.65 s before the pan (`BossEntrancePanDelay`); on the landed boss 0.5 s after it lands and 0.45 s after the pan rests, whichever is later (`BossEntranceLandedHold`, `RestHold`); the camera goes back with the field still held; home, the bars go out and 0.6 s later the field is let go (`BossEntrancePlayHold`) | pan capped at 220 px/s and 600 px/s2 (`BossEntrancePanMaxSpeed`, `MaxAccel`), the zoom on a capped spring too; both ways |
| **The landed boss** (`BossEnemy.IsAwaitingFight`) | - | stands in its own sprite, untargetable with its clock stopped, until the field is let go (`LetLandedBodiesFight`); the Long Coil and the kids' wizard lead their arrivals (`EntranceDelaySeconds` 0.6, 0.75) so their reveal lands with the camera there | - |
| **The prologue's break** (`Prologue`, `Beat.Impact`) | at 2.0 s: warm speed lines out of the Archmage's staff, a shake of the stage, a warm dithered flash | every panel still 0.9 s at its start and 0.35 s at its end; the black holds 0.6 s after the last panel (`EndHoldSeconds`) before the next screen - the prologue's, every lesson's, every freeing's | the drift a smoothstep, at most 23 canvas px/s (`MaxDriftSpeed` 28) |
| **Lessons and freeings** (embedded reels) | calm: none | the same end hold before the results | the same drift |
| **The defeat** (`DefeatSequence`) | violet speed lines rush in on him through the zoom; the four jolts it had | freeze held 0.55 s; a 0.3 s close-up before he falls; 0.6 s between his landing and the word | the zoom 0.6 s, eased in and out (it was 0.32 s, fastest on its first frame) |
| **The dark wizard's fall** (`ThroneDarkWizardBoss`) | the storm: warm speed lines out of his chest, a warm flash; the killing blow's shake | - | - |
| **The ending** (`ThroneEnding`) | calm: none | the row dims and holds 0.9 s before the credits move (`RollHoldSeconds`) | the roll eases in over 1.6 s, then its steady pace |

Measured on `_BossProbe --timescale=1` with `--fixed-fps 60` (its "beats", "holds" and "speeds" lines), before
and after: the pan's peak fell from 247-308 to 220 px/s and comes 0.4-0.5 s after it starts instead of 0.25 s;
the field used to be let go 1.4 s before the camera was home and is now let go 0.62 s after; a whole arrival
holds the field 6.0-6.7 s where it held 3.1-3.8. `_UiShot --cinematic-log` prints the camera, beat,
bars and shake at every shutter; `--reduce-shake` turns Reduce Screen Shake on in memory.

## What makes a player identify with him

Three things, and each beat below uses at least one:

1. **He acts.** Until now the story shows him hiding (the prologue's panel 5) and walking away (panel 6).
   A hero the player is asked to be has to *do* something on screen: break a chain, go back for someone.
2. **He is seen with the people he saved.** The wizards are the game's own, drawn from their real sprites,
   so the one standing beside him in a cutscene is the one the player picks on the next screen.
3. **"You."** The prologue already says *"You are the one he missed."* One more line in that voice makes
   what he does the player's own act. Never more than one line a beat (Eric: *"Overall keep text minimal."*).

## The set

| # | beat | when | what | status |
|---|---|---|---|---|
| A | **The freeing** | inside the prologue, after *"You are the one he missed."* | two panels: he breaks the chain off the four disciplines (animated), and the five of them stand together | **built** |
| B | **Freed** | after the run that frees a wizard a place holds, before the results | one panel, a template: the Archmage beside the wizard he just freed, that wizard's light coming back to him | **built** |
| C | **The doors** | before the first run of the Throne | the freed wizards at his back at the castle door; he goes in alone | planned |
| D | **The homecoming** | after the dark wizard falls, before `ThroneEnding`'s row | the Broken Circle re-lit: the moot of panel 1 again, with him in it | planned |

---

## A. The freeing (built)

### Inside the prologue, not a reel of its own

The two panels sit between `missed` and `road` in the prologue's own beats. Why there and not as a reel
played straight after:

- **The order is the story's.** He frees them, *then* takes the road. A reel after the prologue would show
  the road and then the freeing; a reel between the two would split the prologue into two halves that
  have to agree about everything.
- **One skip.** A player who holds to skip the opening skips one thing. Two reels back to back is two
  prompts, and "never play two reels at once" (the brief) is easiest to keep by not having two.
- **One flag, one replay.** It is gated by `HasSeenPrologue` and replayed by Watch the Prologue, which
  already exist. Nothing new in the save.
- **The cost:** a save that has already seen the old prologue never sees the freeing on its own - only
  from Options. That is the developer's save today and nobody else's: the next release raises
  `CurrentProgressEpoch` (`.ai/slow-start.md`), a story reset clears `HasSeenPrologue`, and every player
  meets the new prologue on his next Start Run.

### What it shows

| # | panel | line | what |
|---|---|---|---|
| 6 | The break (`break`, 5.4 s) | *You went back for them.* | **Animated.** The hiding's heath, his tower on the horizon. The four disciplines - Aedan the Pyromancer, Gabbro the Geomancer, Isbrand the Frostweaver, Thora the Stormcaller, each his own real sprite (the second frame of his death strip: going down) put three stops into the dark - kneel in a row, bound wrist to wrist by the dark wizard's chain of light, their staves dark, the chain's violet their only lit edge. The Archmage stands in the middle of them, the title's own figure, his light banked as it was in the hiding. At 1.3 s it comes up; at 2.0 s it bursts and the chain breaks - the length behind him first, then the outer two, then the shackles - link by link going out as it flies (the darkness ramp down to violet, then nothing), one link in four falling dead to the ground, and the Archmage's gold is on their edges now. From 3.2 s their own lights come back to them one after another, motes falling out of the sky they were drawn into in panel 4, and each comes up into his own colours a stop at a time. |
| 7 | The five (`five`, 4.8 s) | - | The same heath and the same places: the four stand where they knelt (the dissolve is the moment they rise), each in their own sprite and colours, each staff burning his own element again, rimmed in the Archmage's gold on the side that faces him. The Archmage in the middle, in his warded circle as on the title. Each light's pool on the ground in the pigment it makes; the dead chain on the ground in front of them. Five lights on the heath where the last two panels had one. |

Then the road (unchanged) and the character select, where all five are playable.

**The line** is the prologue's fourth. *He came here first. He took them all. You are the one he missed. You
went back for them.* - past tense like the first two, "you" like the third. It says the one thing the
picture cannot: that the figure breaking the chain is the player.

**Rules it keeps** (`.ai/prologue.md`): the dark wizard's light is the chain and the black sun over the
tower, nothing else; the Archmage's is the light ramp and the gold it lands on; the four are their real
sprites (below); nothing from the Sketchbook; feet
above row 552, so the PC's caption band never crosses a figure. The old rule "the last two panels have one
warm light each, and it is his" now reads: the hiding and the road have one each; the five is the one
panel where his light gives theirs back.

**Real sprites, not cut-outs** (Eric, 2026-11-03: *"Redraw with real sprites"*). The first build drew the
four, and the freed wizards, as `roster_figure`'s flat one-colour silhouettes - right for the moot, the
fall and the procession, where the story wants a shape and not yet a person, and wrong here, where the point
is the person the player picks next. `sprite_figure` draws a wizard's 48 cell, every pixel, at a whole x2 on
the panel's grid (`SPRITE_K`: one sprite pixel is two panel pixels, four on screen), turned by `facing`,
with a rim lit one SPRITE pixel deep (on the cell, before the scale, so it sits on his grid) and, while he is
in his colours, his own light's halo behind his element's anchor. `steps` puts him into the dark (`_tone`):
every pigment that many stops down its own row, his element ramps to dull steel and then occlusion, the key's
rims to stone. In the break the four are at 3 until their light returns, then 2, 1, 0 a fifth of a second
apart. Before the chain breaks their only lit edge is its light landing as violet pigment (the rule: the
darkness ramp is emission on him and his chain alone); from the break on it is the Archmage's gold.

**The animation** is the hiding's machinery (`write_anim`, `<name>-anim.png` + `.json`, Prologue.cs's
`AnimSpec`), now per panel: `ANIMS` in `tools/art/prologue.py` holds each animated panel's box, clock and
atlas width. The break's box is `BREAK_BOX` (288,384 - 672,576, every edge on the 8-pixel Bayer grid), 8 fps
over 5.4 s, 35 unique frames of 44 ticks. Its still mid layer is empty inside the box: everything there moves.

**Length:** the prologue is about 36 s now (eight panels; `Prologue.ReelSeconds("prologue")`).

## B. Freed (built)

### What it shows

One panel, 6 s, made once as a template (`tools/art/freed.py`): the heath at night, the moon, his tower
far off on the horizon (he still holds the land). The Archmage left of the middle, the title's figure with
his light up and the gold it makes on the ground; right of it, turned to him, the wizard he has just freed -
his real sprite (idle) in his own colours, drawn as the break and the five draw the four (`sprite_figure`),
rimmed by the Archmage's light - his own light burning again at its anchor, motes of it
falling back to him out of the sky. The chain is off him: two lengths on the ground at his feet with the
light going out of them, the shackles open, the last of it falling from his wrist.

**The line is his freed line** from `QuestLog.Prisoners`, his name in it: *Tituba's cell stands open.*,
*Väinämöinen sings above the ice now.*, *Abe no Seimei keeps his own wards again.*, *Wayland the Smith has
put his hammer down.*, *The Cumaean Sibyl speaks with one voice again.*, *Laelaps has run his chase out.*
Those lines were written in the game's voice for the chapter notes; the reel says them once, at the moment
they become true. A locked wizard's name is his reward, and this is the moment he is paid it.

**The art:** the ground is one pair of layers shared by every panel (`freed-back.png`, `freed-fore.png`);
only `freed-<id>-mid.png` is the wizard's. A `Prologue.Beat` names a shared ground with `Ground:`
(`Prologue.LayerPathFor`). Six mid layers, about 20 KB each.

### Who

The wizards a place holds and the player frees there (`Freeings.WizardIds`): Tituba, Väinämöinen, Seimei,
Wayland, the Sibyl, and Laelaps, set in stone on the heath. **Not** John Dee, freed by collecting and never
standing anywhere to be seen freed; **not** Merlin or the dark wizard, who were never held. The four
disciplines are freed in the prologue. `RegressionChecks.ValidateFreeings` reports a prisoner with a held
line who has no reel, so a new chapter wizard is caught: give him a row in `freed.py`'s `WIZARDS` and in
`Freeings.WizardIds`.

### When, and the gate

- **Owed when a deed frees him.** `AchievementManager.GrantUnlocks`, the one place a deed grants a wizard,
  calls `Freeings.RecordFreed`, which writes his id into `SaveData.FreeingsOwed`. A side event's deed is
  completed and saved the moment the cell opens (`CompleteNow`), so **a run quit after the freeing still
  owes the reel**, and pays it at the end of the next run.
- **Played at the run's end, before the results and before any lesson** (`Node2DGame.Lessons.cs`,
  `PlayOwedFreeingThen`): embedded over the paused run on the lessons' canvas layer, one at a time, the
  story before the systems. Each one, played through or skipped, pays its debt (`Freeings.MarkShown`) and
  hands back to `ShowGameOver`, which asks again - so two wizards freed in one run play one after the
  other, then the lessons, then the results. Headless runs pay it without the reel.
- **Replayable from Options > Story**, "Freed: <name>", for every wizard with a reel the save has freed
  (`Freeings.Watchable`). Watching it there pays and owes nothing.

**Why a debt and not a "seen" flag.** A field that defaults to "not seen" needs no migration - but "freed
and not seen" is true of every wizard an existing save has already freed, so its next run would end in six
reels in a row unless a schema step marked them all seen. A list that only a new freeing writes defaults to
"owes nothing", needs no step, and is kept across a quit. `HasSeenPrologue`'s pattern (additive, defaults
to the quiet state, `FromVariant` guards the read) is otherwise followed exactly; a story reset clears it
with everything else (it is not in `SaveData.SettingsKeys`).

## C. The doors (planned)

**What:** one panel before the first run of the Throne. The castle's door at night, the Archmage before it
from behind (`player_back`, the road's figure, larger), and on the steps below him every wizard he freed -
lit, in their own colours, cut from their sprites - stopping there. He goes in alone ("its doors shut
behind you", the chapter's own card line). No line, or *He is waiting.* - the chapter's corruption text is
already *He is here.*, which is the better one to leave for the card.

**Why not built now:** it needs a trigger the reel system does not have - before a run, not after one
(`Prologue.After` goes to the character select, the menu or the map; this needs an `After.Run` that starts
the chosen run) - and the castle's door is not drawn yet (`.ai/endgame-castle.md`). It must not show the dark
wizard at all (tone rule 5; the Throne's entrance is his reveal). Gate: a `HasSeenThroneDoors` flag, false by
default, no migration (a save that has entered the Throne already has seen the fight it introduces - mark it
true for any save with a Throne run on record, if that matters, in the same schema step as anything else).

## D. The homecoming (planned)

**What:** two panels after the dark wizard falls, before `ThroneEnding`'s row of named wizards: the Broken
Circle (the prologue's moot, panel 1, redrawn by its own function with a new cast) with its stones put back
up, the warded fire lit, and round it every wizard the save freed - and in the middle, where the fire was in
the moot, the Archmage. Then the row and the credits, which already exist (`scripts/ThroneEnding.cs`). The
prologue's first picture, answered.

**Why not built now:** it belongs to the ending, which is `ThroneEnding`'s and still moving; and the cast
depends on the save (who was freed), so it is a template like B, with a variable number of figures, which
wants its own layout pass. Gate: none - it plays every time the dark wizard falls, as the row does.

## Not built, and why

- **A reel for John Dee.** He is freed by collecting thirty chests; there is no place and no moment to draw.
  If one is wanted, his chests' last page is the moment, and B's template takes him as it is.
- **The four freed from their own places.** The disciplines are a fresh save's; their freeing is the
  prologue's, and the prologue is where it stays.
- **Sound.** None added, as the prologue added none: a sting on the chain breaking is the obvious one, and
  it is `.ai/audio-direction.md`'s call.

## Checking it

```
python tools/art/prologue.py break five --preview    # the freeing; tools/art/_prologue/break.png, break-anim.png, five.png
python tools/art/freed.py --preview                  # the ground and every freeing; tools/art/_freed/<id>.png
"$GODOT_BIN" --headless --path . --import            # then commit the .import sidecars
```

Both audit every layer against the contract palette and must print **0** off-contract pixels.

**The pictures.** At 60 fps the prologue's panels start dissolving in at 0, 3.9, 8.4, 12.3, 16.8, 21.5,
26.2 and 30.3 s, and it ends at about 35.8 s. The break starts at frame 1290, so its moment `t` is frame
`1290 + 60t`: `--frames=1356 --burst=5,42` takes it at 1.1, 1.8, 2.5, 3.2 and 3.9 s (banked, his light up,
the chain flying, the first lights back, three lights back). The five is mid-hold at 1716, the road at 1962.

```
"$GODOT_BIN" --path . --resolution 1280x720 --fixed-fps 60 scenes/_UiShot.tscn -- \
  --scene=res://scenes/Prologue.tscn --frames=1356 --burst=5,42 --shot=cut-break-pc --no-save-writes
"$GODOT_BIN" --path . --resolution 1280x720 --fixed-fps 60 scenes/_UiShot.tscn -- \
  --scene=res://scenes/Prologue.tscn --reel=freed_tituba --frames=200 --shot=cut-freed-tituba-pc --no-save-writes
```

Add `--resolution 540x1212 ... --portrait --safe-area` for the Pixel 9.

**After a real run**, through the real deed: `--fresh-save --scene=res://scenes/node_2d_game.tscn --stage=1
--free-in-run=tituba --force-victory --frames=360 --burst=2,420` completes her event's deed a quarter of the
way in (`AchievementManager.CompleteNow`, as the Gaoler's key does - without the event's own plate), wins,
plays her freeing over the paused run (frame 360) and then the results (frame 780). `--fresh-save` matters:
on a save that has freed her already, nothing is owed.

**The flow**, `"$GODOT_BIN" --path . --fixed-fps 60 scenes/_PrologueProbe.tscn`, from a brand-new save in
memory: Start Run plays the prologue, the break follows the hiding, the five follow the break, it plays on
into the character select; a second Start Run goes straight to the character select; Watch the Prologue
plays it and returns; a new save lists no freeing in Options; once Tituba is free, Options lists "Freed:
Tituba", plays it and returns, owing nothing.

**The validator** (`RegressionChecks.Cutscenes.cs`): `ValidatePrologueReel` (every layer of every panel
imported, an animated box's atlas beside its manifest, one line each, four lines at most);
`ValidateFreeings` (every freeing a real wizard with a reel on disk and a one-line freed line with his name
in it; every held prisoner has one; on sample saves the deed owes it, a save and a load keep it, a second
completion does not owe it twice, the reel pays it, and a wizard freed before the reels existed owes
nothing).

## Needs Eric's eyes

- The line *You went back for them.* - the only new copy in the prologue.
- The break at full size on a real screen: whether the four read as kneeling wizards before the light comes
  up (they are dark shapes on cold ground for the first 1.3 s), and whether the burst is too much.
- The real sprites at x2 beside the Archmage, who is drawn on the panel's finer grid and stands half a
  head taller: whether the two scales sit together, and whether the freed wizard reads as small beside him.
- The freed template: whether one ground for every chapter is enough, or each wants its own place (the
  Dungeon's cell, the Waste's ice) - the template takes a per-wizard ground with one more `WIZARDS` column.
- Whether C and D are wanted, and in that form.
- The cinematic rules in motion (2026-11-03): whether an arrival's whole hold - now 6.0-6.7 s, up from 3.1-3.8,
  with the camera going back before play - is a beat or a wait, above all in a Boss Rush (ten of them); whether
  220 px/s is slow enough on the phone's narrower picture; the speed lines' density over a crowded arrival; the
  break's flash and lines at full size; the defeat's slower crash zoom (0.6 s) and the 0.85 s it adds before the
  word; the dark wizard's fall, where the flash covers part of the HUD for a third of a second.
