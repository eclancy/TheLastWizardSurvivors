# The prologue

The story-mode introduction: six illustrated panels, about 27 seconds, mostly without words. Built
2026-10-31 from Eric's request: *"When you first open up story mode, there should be an introduction
to the story, mostly told through art, introducing the final boss and his dark creatures taking over
the land, and kidnapping the other wizards."* And then: *"Overall keep text minimal."*

Read `.ai/world-and-tone.md` first: the prologue is that doc's premise put on screen, and every
choice below comes from it.

## What it shows, and why

| # | Panel (`name`) | Line | What it is |
|---|---|---|---|
| 1 | The moot (`moot`) | - | The Broken Circle before (`.ai/campaign.md` chapter 3): a ring of standing stones on the heath at night, a warded fire in the middle, and round it five of the game's own wizards (below), each staff burning his own element, each in the deep end of his own colour. The only frame with more than one warm light in it - every light the world had is still in it. |
| 2 | He comes (`throne`) | *He came here first.* | The same circle under him, and **only his silhouette**: a tall cowled shape inside a collar of points, one arm raised with the wizards' light streaming into his hand, rimmed by an eclipse of his own dark light behind his head. No face, folds, trim or keys. The stones are down, the wizards are on the ground, and the shadow running out of his hem has eyes in it. |
| 3 | The tide (`tide`) | - | His shadows over the land: a village at the foot of a hill, still lit, and coming over the crest from the tower on the horizon a flood of the things he raised, every one with his mark in its eyes. One of them, horned, in the foreground. |
| 4 | Taken (`taken`) | *He took them all.* | The wizards led along a ridge to his tower between his guards, bound wrist to wrist by a chain of his light, their own light drawn out of them toward the eclipse over the tower. |
| 5 | The one he missed (`missed`) | *You are the one he missed.* | **Animated** (below): the hiding. The broken ring's near stones in a row, and in the row, between two of them, the player - the title's figure, facing us, still as a stone, his light put out to an ember. Beyond the row his creatures search: a hound trots along it nose down, stops at his shoulder and works the air beside him - his ember goes out - and goes on; a caster in the gap watches; two small ones turn their heads; pair by pair the eyes in the dark turn away and back. They never find him. |
| 6 | The road (`road`) | - | Seen from above and behind: the player small on a road that winds over the heath toward everything he holds, the tower on the far horizon. Then the character select - the player choosing who walks it. |

**Three lines, all in the game's own voice.** "He came here first" is the Broken Circle's own card line
(`.ai/campaign.md`); "You are the one he missed" is the premise's sentence. Nothing else is said: no
title card, no narration, nothing an image is already saying (`world-and-tone.md` tone rule 2).

**Rules it keeps:**

- **He is the only source of dark light.** The darkness ramp is used as emission on him, his eclipse,
  his orb, the chain and the corona over the tower - nowhere else. What his light lands on takes the
  violet *pigment* row, as the ward's floor takes gold (`.ai/title-screen.md`, "emissive ramps belong
  on the emitter").
- **His creatures wear his mark and no colour.** Silhouettes in occlusion, one class attachment each
  (cowl, streaming rag, horns and pauldrons, flat shield, loose orb, spear, the hound's line), and one
  or two pixels of cold arcane for eyes (`world-and-tone.md`, "The enemy palette"). They echo the
  chapters' kinds; **nothing in the prologue is from the Sketchbook's cast**, which is the kids' art
  and is never derived from.
- **The wizards are the game's own, and never a secret one.** Eric, 2026-11-01, of the anonymous
  robe-and-hat figure that stood in panels 1, 2 and 4: *"In the intro, the wizard silhouettes look
  goofy. Use the silhouettes of the characters in the game."* Each is now cut from his real sprite
  (`roster_figure` / `silhouette` in the generator: the 48 cell's pixels, contact line and sparks
  dropped, smoothed onto the panel's grid, filled flat, lit only at the anchor where his own element
  sits in the sprite). Which frames: idle in the moot, the second frame of his death strip in panel 2
  (going down), his walk in panel 4. **Only the five whose silhouette a fresh save already shows** -
  the four disciplines and John Dee. Tituba, Vainamoinen and Laelaps are secret until their chapters
  open (`.ai/side-events.md`, "Secret wizards"), and the prologue plays before any chapter has, so it
  would print the answer before the question. Dee's orb is the darkness ramp in the game; here it burns
  arcane, because darkness as light is the dark wizard's alone.
- **The player is the light.** The last two panels have one warm light each, and it is his.

**One tension, and how Eric settled it (2026-11-01).** `world-and-tone.md` tone rule 5 says *"He is
never on screen until he is."* Eric asked for the prologue to introduce him, then ruled: *"The dark
wizard should be shown in at most a silhouette in the prologue."* So panel 2 shows his shape once,
filled flat with occlusion and lit only at the rim (`dark_wizard` in `tools/art/prologue.py` still cuts
every part - cowl, collar, sleeve, circlet, fingers, key ring - then flattens them), and his inside is
the Throne's to reveal. He is never named (the Throne's banner does not name him either). The shape is
the reference for whoever builds `ThroneDarkWizardBoss`'s sprite (`.ai/endgame-castle.md` 6.4): no hat
brim and no staff, which are the player's; the cowl and the collar of points; the raised hand that
takes. A later prologue panel may never show more of him than this one does.

## The art

`tools/art/prologue.py` draws everything; nothing is painted. It runs under Python 2.7 and 3 and
every random stream uses `rng.random()` only (Python 3 changed `randrange`).

```
python tools/art/prologue.py              # all six panels into assets/bonelight/ui/prologue/
python tools/art/prologue.py taken road   # just these
python tools/art/prologue.py --preview    # also tools/art/_prologue/<panel>.png (gitignored)
"$GODOT_BIN" --headless --path . --import  # then import, and commit the .import sidecars
```

Each run audits every layer against the contract palette and prints the off-contract pixel count,
which must be **0**. The preview sheet puts the PC's 640x360 view beside the Pixel 9's 360x808, at
x1, which is the quickest way to judge a change before photographing it in Godot.

**The frame.** Every layer is **960x840**, shown at a whole x2 in a 1920x1680 rect centred on the
canvas - the title's move in both directions at once (`.ai/title-screen.md`, "The landscape set").
A PC canvas is always 720 tall, so it shows frame rows 240-600 (16:9: columns 160-800); a phone canvas
is always 720 wide, so it shows columns 300-660 (16:9: rows 100-740, Pixel 9: rows 16-824). **The
360x360 square both always show (columns 300-660, rows 240-600) holds every subject**; around it the
scene carries on - sky and ground for the phone, land either side for the PC - and `finish()` falls
it to occlusion past the 16:9 edges, so a wider or taller screen meets the scene's black ground
without a seam. One set of art serves both canvases; nothing is scaled to fit.

**Layers.** Each panel is `<name>-back.png` (opaque), `-mid.png` and `-fore.png` (transparent).
`scripts/Prologue.cs` drifts them at different rates; the generator draws each layer out to the frame
edge so a drift never uncovers anything.

**What is reused.** `hero.wizard_hero` (panel 5's player, the title's own figure, with `snuffed=` for
his light put out to an ember, and a stop down every pigment row for the dark), the character sheets in
`assets/bonelight/characters/` (the wizards), `splashkit`'s moon, flame, boulder, tree and leaf clumps,
and the ward's construction from `splash.vigil`. New here: the standing stones, the creature
silhouettes and the searching hound (`sniffing_hound`), the dark wizard, the black sun, the tower and
the player from behind (`player_back`).

**The animated panel.** Panel 5's moving part is one box of its mid layer (`MISSED_BOX`, 368x112 at
296,400), drawn at 8 frames a second over the panel's 5.4 s and written as `missed-anim.png` (an atlas,
four frames a row) with `missed-anim.json` (the box, the clock, and a timeline of one atlas frame per
tick, so a held frame is stored once). Each frame is the WHOLE mid layer inside the box - the searchers
and the stones and the player standing in front of them - so laid over the still mid layer it is the
scene at that moment, and outside the box nothing moves. Every edge and size is a multiple of 8, so the
dissolve's Bayer threshold, read from the atlas's own pixel, matches the mid layer's under it: the box
dissolves with the panel, pixel for pixel, with no seam. About 7 MB of texture while the panel is up.

## The scene

`scenes/Prologue.tscn` is a bare `Control`; `scripts/Prologue.cs` builds the rest.

- **Beats** - the panel table at the top of `Prologue.cs`: name, seconds held, line, and each layer's
  drift in art pixels over the hold. The hiding's back and mid drift as one, so the searchers never
  slide on the ground they walk.
- **The animated box** - any panel the generator gives `<name>-anim.png` and `<name>-anim.json` gets
  it (`AnimSpec`, `AttachAnim`): a `TextureRect` of an `AtlasTexture`, the child of the mid layer (so
  it drifts with it, over it and under the fore), its region stepped by the panel's own clock
  (`StepAnim`), and its own copy of the dissolve whose `art_size` is the atlas's. The drift runs from -half to +half, so the picture the generator
  composed is the one on screen mid-panel. Layer positions are snapped to **whole screen pixels**.
- **Transitions** are a dissolve through the 8x8 Bayer matrix, 0.7 s, in a canvas shader built in
  code (`DissolveShaderCode`): the incoming panel's pixels appear in threshold order over the
  outgoing one, which keeps drifting underneath. Every pixel on screen is one panel's or the other's,
  never a blend - the only fade the contract allows. The first panel dissolves in from black, the last
  out to it. Thresholds come from each layer's own art pixel, so the pattern sits on the x2 grid.
- **Captions** are a `Label` in the display face (Cinzel), bone over an occlusion outline, near the
  foot of the screen: on a PC over frame rows ~560-600, on a phone ~690-790, both darkened ground. A
  line fades in after 0.9 s and is gone before the next dissolve starts, so it never sits over two
  pictures. Type on its own layer is the one thing allowed to fade, as on the title.
- **Skip is a hold, never a tap**: "Hold to skip", top right, 0.9 s, its gold bar filling as it is
  held. It is a `Button`, so `MenuNavigator.Attach` focuses it on arrival: Enter, Space and A hold it,
  a mouse or a finger holds it, and Escape / B / Start hold it from anywhere. A tap or a stray touch
  starts the bar and lets it drain. It rests at half strength so it does not compete with the art and
  comes up to full for 2.5 s after any input. `SafeArea.Apply` keeps it and the caption off a phone's
  corners.
- **Loading.** Panels are created one at a time; each one's three layers are requested on a worker
  thread a panel ahead (`RequestPanel`) and a panel is freed once the next covers it, so at most two
  are in memory (about 20 MB of lossless texture).
- **Sound.** The menu theme carries on from the title (`MusicPlayer.PlayMenuMusic`, idempotent).
  No new sound: the brief allowed one only if genuinely needed, and a sting over the dark wizard would
  spend the reveal `.ai/audio-direction.md` keeps for the Throne.

## When it plays

- **Start Run, the first time.** `MainMenu.OnStartRunPressed` asks `Prologue.IsUnseen`; if so it plays
  the prologue, which then goes on to `CharacterSelection` itself. Start Run is the only way into the
  campaign; Boss Rush is a card inside the stage select and never sees it.
- **Watch the Prologue**, in Options' Story section, plays it on demand and returns to the
  main menu (`Global.OpenMenuImmediately`, as every return to the menu does).
- **Replay intro**, a small quiet plate in the world map's bottom-left corner - **temporary, for
  Eric's testing, to be removed before the game ships for good** (2026-11-01: *"Add a button in the story
  map to replay the intro, for testing. We will remove it later."*). It plays the prologue with
  `Prologue.After.Map`, which returns to the map where he stood and never touches the flag below. Every
  piece of it says TEMPORARY in `WorldMap.cs`, `Prologue.cs` and `_PrologueProbe.cs`.
- **The save flag** is `SaveData.HasSeenPrologue`, written when the prologue ends - played through or
  skipped. Additive and defaulting to false, so **no schema bump** (it narrows nothing, and
  `FromVariant` guards the read): an existing save sees the prologue once, on its next Start Run,
  which is intended. Reset Progress clears it with everything else.
- **Never marked by a tool.** `Prologue.MarkSeen` does nothing while `SaveManager.WritesDisabled` -
  every `_` scene and every `--no-save-writes` run - so a screenshot or a harness that plays it leaves
  the player's flag alone.

## Checking it

**The flow**, through the real menus with injected keys, never writing the save:

```
"$GODOT_BIN" --path . --fixed-fps 60 scenes/_PrologueProbe.tscn
```

It checks, in order: an unseen save's Start Run opens the prologue; a tapped Enter does not skip; a
held Enter skips into the character select; the flag stays unset with writes off; a seen save's Start
Run goes straight to the character select; Options' Watch the Prologue opens it and a held Escape
returns to the main menu; left alone it plays through and goes on to the character select; and
(temporary, with the button) the world map's Replay intro opens it and a held Escape returns to the
map. It prints PASS/FAIL per step and exits non-zero on any failure. Takes about 45 s.

**The pictures:**

```
xvfb-run -a -s "-screen 0 1400x1300x24" "$GODOT_BIN" --path . --resolution 1280x720 --fixed-fps 60 \
  scenes/_UiShot.tscn -- --scene=res://scenes/Prologue.tscn --frames=396 --shot=pro-throne --no-save-writes
```

Add `--resolution 540x1212 ... --portrait --safe-area` for the Pixel 9 canvas. At 60 fps the panels sit
mid-hold at frames 138, 396, 642, 900, 1176 and 1434; frame 255 is mid-dissolve from the first to
the second, and the whole thing ends at about frame 1620. The hiding starts dissolving in at frame
1008, so its moment `t` seconds in is frame `1008 + 60t`: `--frames=1068 --burst=4,60` takes it at 1,
2, 3 and 4 s (the hound arriving, sniffing at his shoulder, standing, gone past). `--frames=138
--burst=6,258` takes all six panels in one run.

## Not done

- Only the hiding animates. The others are still but for drift (no flicker on the fire, no blinking
  eyes); the animated box would carry any of them if it is wanted.
- No pad has been held on the skip prompt and no phone has played it; both are reasoned from
  `MenuNavigator` and `BaseButton` and tested with an injected key.
