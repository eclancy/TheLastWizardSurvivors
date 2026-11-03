# Cutscenes: keeping the Archmage at the centre of the story

Eric, 2026-11-02: *"We need to add cutscenes to help the player identify with the blue robed character.
Include a cutscene where the blue robed wizard frees the four starting wizards."*

The blue-robed wizard is the figure on the title screen, the one the dark wizard did not take - the game
is named for him (*The One He Missed*), and as a playable starter he is **the Archmage** (Arcane, opening
on Magic Missile, first on the character select; built on `wip/protagonist`). Read `.ai/prologue.md` first:
every cutscene here is a reel of that player (`scripts/Prologue.cs`), drawn on its frame, with its pieces and
under its rules. `.ai/world-and-tone.md` says why he is alone, why he is the light, and why a freed wizard
is a person and not a pickup.

## What makes a player identify with him

Three things, and each beat below uses at least one:

1. **He acts.** Until now the story shows him hiding (the prologue's panel 5) and walking away (panel 6).
   A hero the player is asked to be has to *do* something on screen: break a chain, go back for someone.
2. **He is seen with the people he saved.** The wizards are the game's own, cut from their real sprites,
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
| 6 | The break (`break`, 5.4 s) | *You went back for them.* | **Animated.** The hiding's heath, his tower on the horizon. The four disciplines - Pyromancer, Geomancer, Frostweaver, Stormcaller, cut from their real sprites (the second frame of each one's death strip: going down) - kneel in a row, bound wrist to wrist by the dark wizard's chain of light, their staves dark. The Archmage stands in the middle of them, the title's own figure, his light banked as it was in the hiding. At 1.3 s it comes up; at 2.0 s it bursts and the chain breaks - the length behind him first, then the outer two, then the shackles - link by link going out as it flies (the darkness ramp down to violet, then nothing), one link in four falling dead to the ground. From 3.2 s their own lights come back to them one after another, motes falling out of the sky they were drawn into in panel 4. |
| 7 | The five (`five`, 4.8 s) | - | The same heath and the same places: the four stand where they knelt (the dissolve is the moment they rise), each staff burning his own element again and each robe back in the deep end of his own colour - the moot's colours, the first time since the circle. The Archmage in the middle, in his warded circle as on the title. Each light's pool on the ground in the pigment it makes; the dead chain on the ground in front of them. Five lights on the heath where the last two panels had one. |

Then the road (unchanged) and the character select, where all five are playable.

**The line** is the prologue's fourth. *He came here first. He took them all. You are the one he missed. You
went back for them.* - past tense like the first two, "you" like the third. It says the one thing the
picture cannot: that the figure breaking the chain is the player.

**Rules it keeps** (`.ai/prologue.md`): the dark wizard's light is the chain and the black sun over the
tower, nothing else; the Archmage's is the light ramp and the gold it lands on; the four are flat-filled
silhouettes lit only at their own element's anchor (`roster_figure`); nothing from the Sketchbook; feet
above row 552, so the PC's caption band never crosses a figure. The old rule "the last two panels have one
warm light each, and it is his" now reads: the hiding and the road have one each; the five is the one
panel where his light gives theirs back.

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
cut from that wizard's real sprite (idle) exactly as the prologue cuts the four, filled flat in the deep end
of his own colour, rimmed by the Archmage's light - his own light burning again at its anchor, motes of it
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
- The five: whether the moot's flat colours on the four read as "their colours back" or as cut-outs.
- The freed template: whether one ground for every chapter is enough, or each wants its own place (the
  Dungeon's cell, the Waste's ice) - the template takes a per-wizard ground with one more `WIZARDS` column.
- Whether C and D are wanted, and in that form.
