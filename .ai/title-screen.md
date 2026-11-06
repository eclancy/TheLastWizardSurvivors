# Title screen — "Vigil"

The shipping title screen and everything you need to change a detail of it without rediscovering
how it is put together. Decided 2026-09-04 from a set of seven drawn candidates; the other six are
still in the generator and are one line away from shipping instead.

**Read `.ai/art-direction.md` first.** This screen is an application of that contract, not an
exception to it: 1:1 authoring, a whole ×2 render scale, nearest filtering, and nothing on frame
that is not one of the 50 material tones, 4 lights or 12 element ramps in §3.

## The name: THE ONE HE MISSED (2026-11-02)

The game was renamed on 2026-11-02. Eric: *"The title screen art needs to change because the
title changed."* The new name is the premise's own sentence (`.ai/world-and-tone.md`): a dark wizard
took every wizard who could have stood against him, and you are the one he missed. **The blue-robed
figure in the ward is that player** - the one wizard he did not take - and he is nobody else: not
Merlin, who is getting a look of his own (Eric, the same day). The picture already said the rest of
the premise - one figure, alone, in the only warm light in a wood full of eyes - so the composition
is unchanged above the ring, and the lettering carries the one new idea: **THE ONE is lit in the
ward's gold and HE MISSED is set in cold bone**, the one warm phrase over the colour of the world
he left. No tagline; tone rule 2 still holds.

**The lettering is `tools/art/title_lettering.py`.** It holds three layouts, each given for both
screens in the coordinates that screen is authored in, and `SHIPPING` names the one `splash.py`,
`splash_wide.py` and `anim.py` draw. `python tools/art/title_lettering.py` draws all three into the
finished art for both screens and writes the comparison sheet to
`tools/art/_title_lettering/comparison.png` (gitignored; PC 16:9 above, Pixel 9 below, at 1x):

| | Layout | Why not |
|---|---|---|
| **A, ships** | **Stack**: THE ONE over HE MISSED, scales 4 and 3 (272 and 261 wide), the same on both screens | - |
| B | **Ember**: THE, then ONE at scale 5, then HE MISSED, the small words in bone | the strongest single shape, but it says ONE first and the title second, and THE at scale 2 is a scrap over a word that big |
| C | **Line**: THE ONE HE MISSED on one line, scale 3 on a PC and 2 on a phone | the quietest, and on a phone it fits only at scale 2 with the tracking taken in - small print for a title; on a PC it runs across the canopy and right over the staff's orb |

The stack breaks where the sentence does (the one / he missed), puts the lit phrase alone on top,
and its two lines come out within eleven pixels of each other, so it reads as one justified block
the way the first title did (both its lines were 186).

What it took:

- **Five new DISPLAY glyphs** in `font.py`: T, H, E, N, M, drawn at the face's weight (two-pixel
  strokes, two-row slabs, N's and M's diagonals stepping a column every two rows as W's do). T is
  ten wide so its stem can sit on the centre line, M is W turned over. No existing glyph changed:
  `icon_concepts.py` draws the icon candidates from W and S.
- **`splashkit.lettering()`** sets lines of words, each word in its own tone ramp, every halo down
  before any glyph. **`WORD_SPACE` is 7**: a space inside one string comes to 9 (the 5-wide space
  glyph and a track either side), a whole letter, and THE ONE set that way read as two words parked
  apart; at 6 HE ran into MISSED. `wordmark()` is kept for the six unshipped candidates, which now
  carry the new name too.
- **The gold is `[g0, g0, g1, g1, g2]`**, not SURVIVORS' `[g0, g1, g2]`: on a line twice as tall the
  pale cap was a sliver and the lit line came out darker than the bone one under it.
- **Everything that stood clear of the old wordmark now reads its box** (`title_lettering.box`):
  the phone's canopy hole (`splash._clear_for`), the eyes' keep-out on both screens, and on the PC
  `splash_wide.word_box()`, which replaced `WORD_X`/`WORD_SCALES`/`WORD_HALF`/`WORD_BOTTOM` and holds
  the far trees low under the letters. Change the layout and all of it follows.
- **Where it sits.** Phone: cap line at composition row 58, bottom at 154 (157 with the shadow),
  under the old 148 by six rows and still 13 clear of Start Run at 170. PC: cap line 16 at core
  x 175, bottom 113 (116), 18 clear of the menu at 134. THE ONE at scale 5 would be 340 wide on a PC
  and run off a 16:9 screen's left edge or into the staff, which is why the PC no longer gets a
  bigger lettering than the phone (it had (5, 3) to the phone's (3, 2)).
- **The phone's canopy hole is wider** (`_clear_for`: the box plus 22 across and 26 up and down, and
  never smaller than the old (150, 66)). The old margins round a block this wide would have opened
  the hole past the frame edges; with these, the lobing lets a few clumps reach in behind the ends
  of THE ONE, which reads as the type standing in front of the leaves.

## What ships

| File | What it is |
|---|---|
| `assets/bonelight/ui/title-screen.png` | 360×840 artwork, drawn at ×2 into a fixed 720×1680 rect centred on the canvas. The 360×640 composition sits in rows 100–739; rows 0–99 and 740–839 are the tall-frame bands |
| `assets/bonelight/ui/title/*.png` | the 18 animation layers (`anim.py`), all 360×840, same rows |
| `assets/bonelight/ui/title/eyes.png`, `eyes.json` | the eyes in the dark: an 11×4 cell per blink frame (open, half, slit) for each of the two tones, and where every pair sits. See "The eyes in the dark" |
| `assets/bonelight/ui/title-prompt.png` | 360×24 transparent strip, PRESS ANY BUTTON only, at scale 3 |
| `scenes/TitleScreen.tscn` | ground + artwork + prompt |
| `scripts/TitleScreen.cs` | prompt fade and breath, music, input |
| `tools/art/splash.py` | generates both PNGs, plus the six unused candidates |
| `tools/art/hero.py` | the title-size player figure |
| `tools/art/title_eyes.py` | the eyes: a pair's shape and tones, the blink frames, where pairs may go, and the sheet and positions it writes. Shared by both sets |
| `tools/art/title_lettering.py` | THE ONE HE MISSED: the three layouts, which one ships, and the comparison sheet. See "The name" above |
| `tools/art/splashkit.py` | trees, boulders, candles, lettering, sigils, skull, moon |
| `tools/art/raster.py`, `tools/art/font.py` | drawing primitives and the two pixel faces |

Regenerate everything with **`python tools/art/splash.py`** (Python 2.7 + PIL, the only Python on
this machine). It writes the shipping PNGs *and* the previews under `tools/art/_splash/`, which is
gitignored — the candidates live as code, not as files, so a palette change re-renders all seven.

The old square title PNG the scenes once pointed at is gone: nothing references it any more.

## Why the scene is built the way it is

The bug being fixed: the old art was a 1024×1024 square on a `TextureRect` with
`stretch_mode = Keep`, which means *draw at native size*. In a 720×1280 portrait viewport it
anchored top-left, ran 304px off the right edge, and left a 256px dead band below.

The replacement deliberately does **not** anchor the artwork to the viewport. `TitleImage` is a
fixed 720×1680 rect centred on a `Ground` `ColorRect` painted `#05070C`:

- The project stretches `canvas_items` with `aspect = keep_width`, so canvas width is always 720
  but canvas *height* varies with the device. Anchoring the image full-rect would scale it by a
  fraction vertically, and fractional scaling of pixel art is the exact failure the whole art
  direction exists to prevent.
- A fixed rect means the scale is always exactly ×2. A phone shorter than the rect crops its
  ends; a phone taller than it letterboxes.

**The letterbox was not invisible, which is why the art is 840 tall.** This section used to say
it was, back when the rect was 720×1280. On a Pixel 9 (1080×2424) the canvas is 720×1616, which
left 168 device pixels of flat ground colour above and below the art - and the top of the canopy
is *not* near-black: its sky is dithered `stone.deep`, so the join was a ruled line across the
screen. Scaling up is forbidden, so the fix was **more picture**. `splash.py` still renders the
360×640 composition exactly as before and then grows it into a 360×840 frame (`_tall_frame()`,
`TOP` = `BOTTOM` = 100):

- **Above**: more sky and more canopy - boughs in from both top corners and a band of leaf mass
  that closes the break over the wordmark from above, so the hole reads as a gap in leaves rather
  than as open sky with a cut edge. The new leaves cross the seam by ~24 rows and thin out by
  dither, overgrowing the old cut edge rather than butting against it.
- **Below, the ground carries on** (`_ground_falloff()`). The first tall version left the 640
  frame's bottom alone, and on the phone that was a hard line just under the ring: `LIFT` cuts the
  floor off at composition row 591, and the fore layer's `vramp(544, ...)` to occlusion is
  straight across, so the lit grass ended on a ruler and flat black began. In the tall frame that
  ramp is skipped (`tall=True` in `vigil()`); the flagstones, the backB grass and the fore
  occlusion grass all continue to the bottom edge at the density and blade height they had reached
  at the cut, and every layer is then darkened by one falloff centred on the ward - an ellipse as
  flat as the light pool, its edge wobbled along x by three sines so it does not stop on a curve
  either, gated in over 40 rows below the ward so it never touches the treeline, plus a term that
  takes the last of the texture to occlusion at the frame edge. **The new ground has to be the same
  ground as the old just above the cut**: a sparser floor with no foreground grass came out as a
  flat grey slab with the old grass ending on a line above it, which was the bug again.
- **The lit ground has to SHOW to fall off.** At full density the continued occlusion grass was a
  black hedge from the ring to the frame edge and the falloff had nothing to fall off across, so
  below the cut it thins with depth, the floor carries a wide pool of `gold.deep` pigment from
  the ward, and the ward falloff is gentle (it tops out short of `deep`; the frame-edge term does
  the rest).
- **It thins by coverage, not by count** (`_mat_keep`). That grass mat is saturated - each tuft
  is five blades over ~46 rows, about seven blades over every pixel at the density `vigil()`
  reached - so thinning the *number* of tufts linearly (the first version: all of them at the cut,
  12% seventy rows down) changed nothing visible until the mat ran out, and then it emptied
  across about fifteen rows. On the phone that was the line where "the original grass" ended,
  around file rows 712-724. The keep chance is now solved from the coverage wanted on each row:
  0.93 at the cut, easing to 0.18 by row ~800.
- **The second line was a dark pocket.** The 16:9 prompt pocket in `_type_pocket_t` (rows
  ~680-716, nearly the full width) blacked out the side banks of bushes at full strength over a
  12-row fade, so the lit banks started on a ruled edge just under the ring. It is now weaker
  (`stone.shade`) and fades over 40 rows, and the tall-phone prompt pocket fades over twice its
  half-height. Pockets are the easiest way to put a line back: give every one a long fade.
- **The dark patch under the ring was the grass itself** (fixed 2026-11-02; Eric had flagged it
  more than once). The PC's went when its UI pockets came out from under the ward, but on the phone
  the pockets were not the cause: measured across the middle (x 100-260), the foreground layer
  covered **91-98% of rows 670-710** in flat occlusion, over ground the falloff had already taken
  most of the way to black, so the band straight under the brightest thing on screen came out at a
  mean luminance of 9-10 - `occ`, near enough. Two things made it. The foreground tufts rooted just
  below the ward's keep-out ellipse are forty-odd rows tall, so they reached straight up to the
  ring's lower arc; and the ward light died within about forty rows of the ring, because the
  falloff used the pool's own flat 58-row radius on a floor that runs toward the camera. Now:
  - `_falloff_t` reaches **84** rows below the ward, not 58, so the lit floor carries to about row
    730 before it goes (the gate keeps it off the treeline, as before);
  - `_under_ring()` weights each foreground tuft by how straight under the ring it stands (1 under
    the arc, smoothstepped to 0 past either end of it and 150 rows down) and then **drops most of
    them** (`UNDER_RING_DROP` 0.9 x weight), **shortens** them (to 6 + 0.32 per row below the arc,
    so the nearest grass is a fringe, not a hedge) and gives the one lit blade in each tuft the
    ward's light (gold pigment near the ring, lichen further off) instead of `stone.deep`. It has to
    be most of them for the same reason as `_mat_keep`: the mat is saturated, and dropping 62%
    left the patch 87% covered. vigil()'s own grass makes its keep decision from a position hash
    (`_hash01`), not from its random stream, so every tuft and bush drawn after it is unchanged.
  The pockets were checked and left alone, because they were not part of it: `_type_pocket_t`'s two
  prompt pockets (1.6 and 1.9) sit under 2, where the undergrowth's darkening scale is still two
  `None`s, so they change no pixel (only its version-label pockets do), and `_falloff_t`'s own
  prompt pocket is the largest term at 7 pixels of the frame.
  Measured after: rows 670-710 are 2-70% covered (it grows with depth) and come out at luminance
  36-20 where they were 11-9, falling to about 12 by the Pixel 9 prompt rows, which is where the
  old picture already was. The layers still composite to the flat render with zero differing
  pixels.
- **Behind the type it is dark on purpose.** The menu's Options row and preset readout sit on
  file rows 675–720, darkened enough by the falloff there. PRESS ANY BUTTON now sits near the
  bottom of the screen (below), and `_falloff_t` holds a soft-edged dark pocket centred on its
  lowest position, so on a tall phone what is behind the letters is only `occ` and `stone.deep`
  (measured: max luminance 14.8, the value of `stone.deep`, across the whole strip).
- **The undergrowth closes in from below** (`_undergrowth()`, all on `fore`). A falloff alone
  read on the phone as "just darker": what makes the clearing small is the wood pressing in from
  the foreground too. Three depths, back to front:
  - **Mid-ground bushes** banked up both sides (reaching further in the lower they stand), a
    low bank along the bottom edge, and a band pressing in under the ring from both sides,
    thinning toward the middle so the ground the ward lights still shows through the gap. That
    band sits on rows 716-760, *between* the two places the prompt can go.
  - **Ferns** among them - the fronds from the first tall version, which were the part that
    worked - from the bottom and leaning in from the sides.
  - **Big near leaves** in flat `occ`, cut off by the frame in both bottom corners and low along
    the bottom edge, with only a thread of `stone.shade`/`violet.shade` on their upper edges. The
    nearest plane gets the least light; that is what puts it in front.
- **The undergrowth is TONED, not darkened afterwards.** `lit()` measures how much ward light
  reaches a spot and picks one of three tone tiers for each bush and frond: near-blacks out in
  the dark, `lichen.shade`/`gold.shade` tops close to the ring. The first pass drew everything in
  near-blacks and then ran a falloff over it, and it came out as more of the same dark - a bush
  only reads as a bush if its lit side is lighter than the ground it stands on. After that only
  two things darken it: the frame bottom (rows 812+, to `occ`) and `_type_pocket_t`.
- **`_type_pocket_t` is the type's clearance**: soft-edged dark pockets behind PRESS ANY BUTTON
  at both places `TitleScreen.PlacePrompt` can put it (rows 756-780 tall, 686-710 on 16:9) and
  behind the version label in the bottom-left corner on either. Foliage runs behind the letters,
  but only as `stone.deep` and `occ`. Move the prompt and these have to move with it.
- `_frond()` draws fronds *lit*, in one tone. Drawn as occlusion silhouettes with an edge light
  they were invisible, and the edge pass came out as a uniform speckle. Nothing at the bottom is
  mirrored: a symmetrical pair reads as a frame put round the picture.
- **Both bands fade to pure `occ` at the frame edge**, so a phone taller than 21:9 still meets the
  `Ground` colour invisibly. The top band is darkened by the Vigil vignette's own function carried
  past the end of the frame (`_vignette_t`), so new pixels at the seam get exactly the darkness of
  the old pixels beside them.
- **The composition is untouched above the ward** - only the new leaves overgrowing its top 24
  rows, and from the bottom of the ring down, the ground falloff. Every new pass draws on a
  scratch canvas and is composited onto the layer that owns its depth (sky on `backA`, leaves and
  ground on `backB`, occlusion grass and fronds on `fore`); the falloff is darken-only and a pure
  function of position, so it runs on each layer. The animation layers still composite to the
  flat PNG with **zero** differing pixels, and `title-prompt.png` comes out byte-identical.
  (The app icon was a crop of this art until 2026-10-16; it is drawn by `icon_concepts.py` now.)
- **One trap in keeping that zero.** The flat render is one canvas, so anything `_tall_frame`
  adds to `backB` lands *in front of* whatever the layers above `backB` already hold there - the
  ward pool and the old foreground grass. The new backB ground is therefore held to the rows
  below the `LIFT` cut, where no higher layer has anything. Letting its tufts straddle the cut
  cost 31 differing pixels.

720×1680 covers a 21:9 phone exactly. On a Pixel 9 the art shows file rows 16–823; on a 16:9
screen it shows rows 100–739, which is the old picture.

`TitleScreen.cs` used to call `FantasyGuiSkin.ApplyFullscreenBackdrop` with a stock fantasy-GUI
plate at 0.96 opacity over the entire screen, plus a second plate on a `Prompt` node that did not
exist. Both calls are gone. **Do not reintroduce a skin pass on this scene.**

## Composition and draw order

Draw order in `vigil()` is load-bearing; three of the four bugs found while building this were
order bugs. Back to front:

1. Sky gradient (`vramp` 0→430) and starfield.
2. **Far wood** — eighteen trees on the horizon line at y=436. Each tree, each clump of
   undergrowth and each leaf mass picks its own pair of near-blacks from `FAR_TONES` or
   `NEAR_TONES`. A dark mass painted in one colour is a hole; six near-blacks that differ by a
   few points of *hue* read as depth, because the eye takes the variation as things at different
   distances. Far is a step lighter and cooler than near — atmospheric perspective, the only
   depth cue left once everything in shot is silhouette.
   **Then a middle depth** (`fill=True`, `MID_TONES`): twelve more trees on a baseline of 446 and
   150 loose clumps hung through rows 214–404, thickest toward the frame edges, a step darker than
   the far wood and a step lighter than the near. With only two planes the sky between them read
   as empty rather than as distance. Nothing in it rises above row ~200, so the break that holds
   the wordmark keeps its dark sky; it is all on `backA`. It draws from
   its own random stream - `tone()` shares one, and spending from it would re-colour every clump
   drawn afterwards.
3. Ground mist. (The eyes in the dark were drawn here and after the canopy. Since 2026-11-01
   they go on **after everything**, the vignette and the tall frame included - see "The eyes in
   the dark".)
4. Ground: flagstone bands that widen toward the camera, from y=428.
5. **Grass**, thickening toward the camera and thinned inside the ward circle so the runes still
   read through it; then **brush banked up both edges** of the clearing floor.
6. **The creep** — off in the shipping preset, and it is worth knowing why: filling the lawn cost the composition its depth. With the wood brought down to his feet there was no distance left between the trees and him for the eye to read, and no amount of size or tone gradient inside that band bought it back. It is foliage growing out of the treeline down onto the lawn, plus six saplings
   rooted on it. This has to be its own pass *here*, after the floor: the flagstones are drawn
   over the whole treeline block, so anything placed below y=428 up there is silently destroyed,
   which is why raising the treeline counts could never make anything encroach. It is the only
   foliage in the frame actually standing on the grass, and it keeps a hard exclusion ellipse
   around the ward.
7. Boulders.
8. **Near wood** — two trees at the frame edges in flat `occ`, mostly cropped. These do most of
   the atmospheric work: they put the viewer *inside* the wood looking out at the clearing.
9. **Overhead canopy** — six boughs reaching in from off the top corners, then a band of loose
   leaf mass from y=0 to y=344 filling the sky around them. `CLEAR` in `vigil()` is the single
   hole left in it, and it exists to hold the lettering. It is grown from the lettering's box
   (`_clear_for`), so it moves with it; it used to be a fixed tuple that had to be moved by hand.
10. **The ward circle**, on the `WARD` ramp — the brightest thing in the frame after the orb, and
   the reason the composition works. There are no candles on it any more: nine orange flames
   around a gold-white ward put two warm light sources in the same place competing for one job.
11. The figure.
12. Motes lifting off the ring, after him so some drift in front.
13. **Foreground grass and brush** in flat occlusion across the bottom strip, held out of an
    ellipse around the ward. It is the nearest thing in the frame so it gets no light at all,
    and a plane the figure stands behind is what turns a backdrop into somewhere he is.
14. Vignette.
15. Wordmark.

## The knobs

| To change | Where |
|---|---|
| Which of the seven screens ships | index into `made[]` in `splash.py: main()` |
| Tree placement, height, canopy spread | the `far` and near-pair lists in `splash.py: vigil()` |
| Canopy density and raggedness | `density` arg, and `_clump` in `splashkit.py` |
| The hole in the canopy | `_clear_for()` in `splash.py`, grown from the lettering's box; its lobing lives in `canopy()` |
| Overhead boughs | the bough list in `vigil()` — origin, angle, length and width per limb |
| Grass density and reach | the two grass loops in `vigil()`; one blade shape in `grass()` |
| **How closed-in the wood is** | `wood=` on `vigil()`. `open` **ships**; `thicket`, `rank`, `walls` and `current` are one word away. `python tools/art/woods.py` renders them |
| How broken the horizon line is | `humps` — a couple of dozen taller clumps along the treeline. **Raising `under` only moves the flat line up; a horizon is broken by a few things standing above the rest, not by density** |
| How far the wood comes down the lawn | `creep`, `reach`, `cbias`, `cedge` and `keep` in the chosen preset. **`cbias` above 1 pushes clumps up against the treeline; below 1 pushes them down into the ward keep-out, where they are thrown away and the middle of the band stays bare however high `creep` goes** |
| How enclosed the clearing feels | the brush loops in `vigil()` — treeline, floor edges, then the creep |
| How dark it is under the brim | `_hood()` in `hero.py`, and the `_hat_shadow()` call at the foot of `_face()` — move the skip rect in `wizard_hero` with them |
| **Which beard he wears** | `beard=` on `hero.wizard_hero`, and the default on `splash.vigil()`. Eight styles; `python tools/art/beards.py` renders them all into the screen |
| Hand and finger size and tone | `_fingers` / `_arm` in `hero.py`; tones from the `flesh` ramp |
| Branch forking, lean, hanging strands | `tree()` in `splashkit.py` |
| **The colour of all wizard magic** | `WARD` at the top of `vigil()` — one name moves the orb, the rings, the motes and the light on the figure together |
| Ward circle size and position | `cxp, cyp` and the radii in `vigil()` |
| How bright the ward burns | the rings, star nodes and rune ticks in `vigil()`, all on `WARD` |
| The six flames on it | `ward_flames()` in `vigil()` for height and depth-sort; `flame()` in `splashkit.py` for shape |
| How many things are watching, and where | `EYE_GROUPS` in `splash.py`: a band and a count per set, branches then treeline, with `MENU_ROWS`, `PROMPT_ROWS` and the wordmark kept clear. The PC set has its own `EYE_GROUPS`, `EYE_KEEP_OUT`, `EYE_CAPS` and `SCREEN_EDGES` in `splash_wide.py` |
| What a pair looks like | `OPEN`, `FRAMES`, `GAP` and `TONES` in `tools/art/title_eyes.py` |
| How far apart pairs stay, and how dark their ground must be | `SPACING`, `CANDIDATES`, `DARK_MEAN` / `DARK_MAX` and `FIGURE_PAD` in `title_eyes.py` |
| How often they blink | `[Export]`s on `TitleScreen.cs`: `BlinkMinGap` / `BlinkMaxGap` (each pair, 5-16 s), `BlinkCloseSeconds`, `BlinkHoldSeconds`, `BlinkOpenSeconds`, `DoubleBlinkChance`, `MaxBlinkingPairs`, `BlinkStagger` |
| How far its light spreads on the floor | the three `radial` pools, on the **violet material** row |
| How much it lights the figure | `under_ramp=` on `wizard_hero`, and `_underlight` in `hero.py` |
| Figure size | `hero.wizard_hero(c, cxp, cyp, h=150)` |
| Figure proportions | `wizard_hero` in `hero.py`, all in units of `u = h/32` |
| Robe colour | `robe="wool"` — any key in `bonelight.MATERIALS` |
| Orb / staff colour | `staff_ramp=` — any ramp in `bonelight.ELEMENTS` |
| **Which lettering ships**, and its position, scales and tone ramps | `SHIPPING` and `LAYOUTS` in `tools/art/title_lettering.py` (both screens); the glyphs in `font.DISPLAY`; the word space in `splashkit.WORD_SPACE` |
| How dark the ground is straight under the ring (phone) | `_under_ring()` and `UNDER_RING_DROP` in `splash.py` for the near grass; the `84` in `_falloff_t` for how far the ward light carries down the floor |
| Prompt wording and face | `prompt_asset()` in `splash.py` |

**The prompt says BUTTON, not KEY.** The title screen has always accepted a key, a mouse click or
a pad button - `TitleScreen._Input` tests all three - but the wording only named the one input a
controller does not have. "PRESS ANY BUTTON" is the phrasing that reads as controller-aware without
excluding anything else. It is set at **scale 3** (285 of the 360 wide, cap height 21 rows):
scale 2 read as small print on a phone, and scale 4 measures 380, wider than the frame - with the
tracking taken out to fit, BUTTON's two Ts fuse. A longer wording has about 75 px of room at
scale 3. Change it in `prompt_asset()` and re-run `python tools/art/splash.py`: only
`title-prompt.png` moves, and `title-screen.png` comes out byte-identical.
| Prompt timing and resting opacity | `[Export]`s on `TitleScreen.cs`, and the `0.42f` in `FadeInPrompt` |
| Prompt position on screen | **set at run time** by `TitleScreen.PlacePrompt`: its bottom goes 88 canvas units (more with a large safe-area inset) above the bottom of the screen, clamped to file rows 710–780. Pixel 9: rows 756–780; 16:9: rows 686–710, its old place under the ward. `PromptLowestRow` must equal `PROMPT_BOTTOM` in `splash.py`, which centres the dark pocket in the art. The scene's `-168` / `-120` offsets are only the default before it runs |
| How tall the frame is, and what fills the extra rows | `TOP`, `BOTTOM` and `_tall_frame()` in `splash.py`: the bough list and `canopy()` call above, the frond list below |

## The figure

`hero.py` is a separate construction from the 32×32 in-game sprite, on purpose: at 32px the
silhouette is the entire design, and at title size it is only the starting point. Four things
carry the read, and all four were arrived at by getting them wrong first:

1. **The staff is taller than the wizard** and ends in a forked claw, so it sets the top of the
   silhouette and is read first.
2. **Proportion**: 34 units tall, 13 wide at the hem, about 2.6:1. The first attempt was nearer
   2:1 with a hat brim wider than the body, which reads as a chess piece.
3. **Value**: he is unlit on the camera side. The fill is `base`/`shade`/`deep`; separation comes
   from the bone rim, the orb bounce and the gold staff. Filling the robe from the *bright* end of
   the ramp is most of what made the first version look like a mascot.
4. **Asymmetry**: a cloak swept off-axis, a mantle layered over the robe.
5. **A jointed arm**: shoulder, elbow, wrist, drawn as two tapering capsule runs with a slightly
   wider disc at the joint. The first version was one straight quad from body to wrist — no
   joint in it anywhere and four units thick — and it read as a plank laid across him. It is
   drawn **over the robe but under the mantle**: on top of every layer he wears, it read as an
   arm laid over the outside of his clothes.
6. **Warm light, cold world.** `WARD` is the Light element ramp, not Arcane. Everything around
   him is cold blue-grey stone and violet, so a purple ward sat in the same hue family as its
   own background and had to shout to be seen. Gold-white is the only high-contrast option the
   palette offers, and it reads as *protective* rather than merely magical. Blue and green were
   both considered and both lose to the ground they sit on.

**The face is a shadow, a nose and a beard — and nothing else.** `_hood()` puts a band of `occ`
under the brim and that band stays unlit: it is handed to `_rim` and `_bounce` as a skip rect, so
neither pass can put a bone edge or a gold bounce on the one part of the figure whose whole job
is to be dark. It reaches from the brim underside down to −21 units, well past the nose, and its
bottom two units dither through robe `deep` and `shade` onto the mantle rather than stopping on a
rule. **The skip rect and the shadow have to move together** — deepen one and leave the other and
the extra shadow starts taking an edge light again. Out of the bottom of it comes a nose in `flesh` and a grey beard in
`skin`, and that is the entire face. There are no eyes and no mouth; at this size they are three
dark specks in a void, which is what made an earlier front-facing head read as a mask.

Both are **drawn as stylised things, not simulated ones**, and that is the single lesson of the
whole face. Three earlier beards — five tapering locks, then a field of scattered tufts, then a
bushier field of the same — all tried to be the real object, and all spent their detail on
texture the eye cannot resolve at 150px while leaving the silhouette soft. What carries at this
size is shape and value break, so that is all either of them spends anything on.

The nose is **a small sharp wedge in one tone** — three points, a straight taper to an apex on
the centre line, three pixels across and five tall. It has been round twice and pointed twice; at
this size the only property that survives is whether it ends in a point or does not, because a
bulb three pixels wide has no room to read as round. It also gets **one** colour plus two edge
lines: a body disc with a lit cap and a shaded spot in it is three colours fighting over a shape
the eye reads as one. The brim's shadow supplies almost all of the modelling.

The beard is `skin`, the bone/pallor row — white hair and old bone are the same material in this
palette. It is **eight named styles sharing one renderer**, and `beard=` on `wizard_hero` picks
one. They are genuine alternatives, not one shape with knobs on: the silhouette is the whole
design at this size, so a style that differs by a couple of units of width is not a style.

| Key | Shape | |
|---|---|---|
| `cascade` | broad straight fall past the belt, 13.4u | the only one that makes him taller rather than wider |
| `storm` | blown 3.4u off the vertical, like the cloak | power in use rather than at rest |
| `bound` | gathered in two gold bands, 12.2u | the only one that puts metal on him below the staff |
| `mane` | one undivided mass, 10.6u wide | primal; sits right on the width ceiling |
| `fork` | a mass splitting into two weighted points | the most legibly arcane |
| `mane-spear` | **ships** — mane held wide, then a long taper to a point | heavy first, sharp second |
| `mane-blade` | widest highest, straight sides to a chisel point | edged, the most predatory |
| `mane-talon` | mane hooked off the vertical in its last third | a claw rather than wind |

Render every style into the screen with **`python tools/art/beards.py`** (or name a few:
`beards.py mane-spear mane-blade`). It writes to `tools/art/_beards/`, which is gitignored, and
it never touches the shipping asset — only `splash.py` does that.

`_rows()` turns a half-width profile plus an optional sweep into a stack of spans, and **both the
outline and the texture come off that same list**, so feathering always lands on the edge and
streaks always land inside it whatever the profile is doing. Everything below is enforced in
`_mass()` and applies to all eight:

- **Length is most of the read.** The shipping beard runs twelve units, past the mantle hem and
  down to the belt. The earliest versions were about half that, and a half-length beard reads as
  a full beard rather than as a wizard's.
- **Width has a ceiling, and it is the length.** Taken out until it is as wide as it is tall, any
  of these stops being a beard and becomes a ball. `mane` sits closest to that line deliberately.
- **No ruled partings down the front.** Tried at four, eleven and thirteen across, in occlusion
  and in `deep`, ragged and even; at every count and every weight they read as ruling on a
  surface. Five fat capsules instead read as dreadlocks, and a field of ringed discs as bubbles.
  The mass is one dithered ramp, and the hair is short scattered flicks — under a unit and a
  half, because **length is what turns a streak into a parting**.
- **No keyline round the outside either.** A continuous `occ` line all the way round the profile
  reads as a shield boss hung on his chest: hair has no outline, it has an edge that breaks up.
  The edge is short flicks instead, each leaving the profile by a quarter to half a unit — at a
  unit and a half they are spines and the beard is a hedgehog.
- **The moustache is one tapering `_capsule` run per side**, in the beard's own values, with its
  own feathering. Every pale, smooth version read as two slabs laid across the face; a smooth
  shape lying on a hairy one never joins it.
- **A capsule cannot come to a point**, because the smallest disc it draws is a pixel across and
  the end is always a soft cap. `tip=` on `_must_run` carries a hard needle past the end by hand,
  and `feather_span=` keeps the flicks off the outer 40% so they do not soften it back. Those two
  arguments are the whole difference between a moustache that thins and one that ends.

**The shipping beard starts low.** `mane-spear` opens at −22.35 rather than the −23.60 the rest
of the set shares, so it hangs from *under* the moustache instead of running up past it to the
cheekbones; its bottom drops the same amount, so the length is unchanged. What that buys is dark.
The band the cheeks used to fill is hood shadow now, and the face reads as a moustache and a nose
coming out of a void rather than as a head of hair filling the brim.

- **`_hat_shadow()` is a pass, not a shape.** The nose and moustache are drawn after the hood void
  and over it, so without it they come out fully lit inside a shadow meant to be swallowing the
  top of the face. It walks pixels and steps each one down its own material row, dithering the
  boundary — and its map holds only `skin` and `flesh`, which is what keeps it off the hat and
  the robe. One step, not two: at two the nose went black.
- It runs from −23.70 to −21.60 — fully dark above, clear below, dithered across two units in
  between. Both ends have moved down twice now, and the fade is deliberately long: over a short
  span the shadow is a step, and over two units it is something you watch happen across the nose
  and the top of the moustache. **Shrinking the nose is not what keeps it quiet** — lowering this
  is. Reach for these two numbers before the nose geometry.

There is also **no clasp at the throat** any more. A gold disc sat there, and under the hood
shadow with nothing else below it, a warm rounded shape at chin height reads as a chin.

**The hand is the only bare skin, and it uses `flesh`, not `skin`.** The row called `skin` in
this palette is bone — cold and moon-pale, what the skulls and the moon are made of — and a hand
painted from it reads as a gauntlet. `flesh` was added to the contract for this. Beside the gold
shaft it is one of only two warm notes on the figure, the other being the sash.

The hand is the fiddliest part. The arm, cuff and back of the hand are drawn **before** the staff
and only four fingertips and a thumb come round the near side after it — he grips the shaft
rather than holding it up in front of himself. Fingers are drawn as chains of discs (`_capsule`),
never polygons: at this size four right angles reads as a brick. They are four different lengths
and thicknesses, and the thumb crosses the stack and **stops on the shaft**: laid parallel it was
a fifth finger, and run past the far side of the fingers it was one again.

## The title screen is also the menu

Pressing a key no longer changes scene. `TitleScreen.ShowMenu` reveals its **already prepared child**
`MainMenu`, gives it focus and hides the prompt. The opening press is consumed so it cannot also
activate Start Run.

The reason is the animation. Swapping scenes tore the artwork down and rebuilt it, which restarted
every layer in `SetUpAnimation` - so although both screens showed the same picture, the handoff read
as a flicker and the flames visibly jumped. As a child, the menu arrives on top of a picture that
never stopped moving.

**Everything that used to return to the main menu now loads this scene instead**, setting
`Global.OpenMenuImmediately = true` first: `CharacterSelection` (back), `GameOverScreen`, and
`Node2DGame` (quit to menu). `_Ready` reads the flag, clears it, and opens the menu after preparation
finishes - last, deliberately, because everything the menu draws over has to exist first. Without
the flag the player would press a key to dismiss the title beat after every single run.

`MainMenu` is therefore never loaded standalone in the shipping game. `scripts/_UiShot.cs` still
loads it directly for screenshots and will show the menu against a black ground, which is correct
for a measurement tool and wrong for a player.

### Startup preparation and the first-press freeze (2026-11-05)

The first press used to synchronously load, instantiate and add `MainMenu`. A real-window probe on
Windows measured **13,952 ms** from Enter to the first menu draw: **12,048 ms in regression checks**,
682 ms in spell/character validation, and under a second in the menu's own construction. This was
development verification running on the player's main thread, not a resolution problem.

- **The full validator is opt-in:** `--validate-content` runs it once from `MainMenu` or
  `Node2DGame`. Keep using it for verification (see `CLAUDE.md`); normal launches run no regression
  suite. `--profile-startup` prints the resource/validation/menu-stage timings.
- **After the updater finishes**, `TitleScreen` displays `TitleLoadingScreen`, requests the menu
  scene with the threaded loader, and only retrieves it once loaded. It builds the hidden menu in
  seven main-thread stages (`MainMenu.PrepareInSteps`); standalone menu harnesses stay synchronous.
- The loading art reuses the generated title's ward and flame frames, cropped through
  `AtlasTexture`, drawn by `PixelArtRect` at whole screen pixels. Fire appears in two groups as
  resource/menu stages complete. No new PNGs, fabricated percentage or minimum loading duration.
- The completed menu is drawn behind the opaque loader for layout/font/material warmup, hidden,
  then the title starts. Loading inputs are consumed; failure offers **Try again**. A return from
  a run uses the same preparation and then opens the menu directly, without the title prompt.
- Removed the duplicate initial shop refresh and achievements-page rebuild. In fresh Windows
  processes, preparation measured about **1.2-1.5 s**, and the opening press **34-44 ms** with keyboard,
  mouse and pad. Portrait at 540x1212 measured **45 ms** for the press. These are desktop measurements;
  the Pixel 9 was disconnected, so physical Android timings remain unmeasured. Preparation still
  includes a roughly half-second first achievements build; the long first-press freeze is gone.

`scenes/_TitleLoadProbe.tscn -- --no-save-writes --profile-startup` measures the first rendered menu
after a real press, failing if the prepared press takes over 250 ms. `--input=mouse` / `--input=pad`
/ `--input=touch`, `--returning`, `--failed-load`, `--reset-notice` and
`--portrait --safe-area` cover input, direct returns, retry and portrait. It sends early presses
during loading and verifies they neither skip preparation nor activate Start Run. It also opens and
closes the prepared Powers, Achievements and Options panels. The only save used is an in-memory
fresh one.

### How the menu is laid out against the artwork

`MainMenu.LayoutAroundTitleArt` runs after every `GetNode` in `_Ready` - never before, because it
moves the button stack out of the scene's containers and the paths have to resolve against the
scene as authored. `LayoutUtilityRow` then places the foot of the screen on both layouts.

**Anchoring to the screen edge is the trap.** The art is 720x1680 at a whole x2 centred on a canvas
that is always 720 wide and a height that varies by device. The top of the artwork is *not* the top
of the screen. So every piece of menu furniture is anchored to the **centre** and positioned in
artwork rows by `PinToArtRows`: row R of the original 1280-tall composition sits at `R - 640` from
centre, on every device, always. The rows below are in that composition's coordinates, not the
1680-tall file's (file row = R + 200 at x2): the composition is still centred in the taller frame,
which is exactly why growing the frame needed no change to any of these numbers.

| rows | what the art has there | what sits on it |
|---|---|---|
| 340-550 | dark wood, below the wordmark and above the staff orb | Start Run, then Arcane Upgrades / Unlocks, with `N Arcane Energy` under Arcane Upgrades |
| 1168-1232 | foreground grass | the utility row: Options, Spell Index, Report a bug, 208 x 64 each |
| 1244-1304 | open ground below the grass (a tall phone) | preset and next-run gain, centred; on a 16:9 phone, whose canvas ends at row 1280, it goes above the utility row instead |

The figure, the ward, the flames and the orb are never covered. That is the whole point of the pass.

Three things it had to fix on the way:

- **The Arcane readout was one Title-sized label carrying three stacked lines**, and its third line
  ran into the top of the wordmark. The preset and the next-run multiplier moved to the bottom of
  the screen; the currency stayed up in the canopy as one line - until the player said it "awkwardly
  shows up in the upper right". It is now the caption of the Arcane Upgrades button
  (`MainMenu.EnsureMenuEnergyReadout`: the energy glyph and `4,645 Arcane Energy`, gold, outlined,
  inside the button's own column) and the purse in the shop's header (`EnsureShopHeader`), which
  had never shown it.
- **`MenuButtons` is reparented to the menu root**, out of the MarginContainer/VBox stack, because a
  control inside a container has its position overwritten every layout pass and cannot be pinned.
  That means `ShowPanel` has to raise and lower it by hand - it is no longer a child of `mainPanel`
  - and the utility buttons and the preset line with it.
- **The TopBar is gone.** The energy label used to sit on a translucent `PanelContainer` plate
  across the top of the scene; once the label moved onto the art the plate was empty, and
  `ShowPanel` switched it back on every time the root menu was shown, so a long empty bar ran
  across the canopy and the wordmark on both layouts. It is deleted from `MainMenu.tscn`, and with it
  the line that re-showed it. Nothing on the root menu is laid out by that VBox stack any more; the
  `Spacer` under it is hidden behind panels (so they get the full height) and does nothing at root.

**Controller order** is geometric, Godot's own focus search: Start Run (the initial selection), the
progression row under it, then the utility row along the foot. The Spell Index left the
progression row because it is a reference rather than a way to get stronger
(.ai/level-up-screen.md, section 4c).

The `ColorRect` that used to wash the whole screen at 28% is a **scrim** now: fully clear on the root
menu so the artwork reads as the screen, and 88% behind any sub-panel, because a wall of upgrade
rows over a forest at night is unreadable. `MainMenu`'s own `Background` - the old pre-Bonelight
title image - is hidden outright.

## Four bugs worth not reintroducing

- **The vignette ran last**, so it dithered frame-edge darkening straight over the wordmark. Two
  candidates were unreadable because of it. The vignette is scene light: it runs before the type.
- **The rim and bounce passes were silent no-ops.** Both find the silhouette edge by testing
  alpha, and the scene canvas is opaque, so every pixel passed. The figure is drawn on its own
  transparent layer and composited; anything else that needs an edge pass must do the same.
- **`poly_shade`'s gamma reads backwards.** Colours run light-to-dark along the axis, so gamma
  *below* 1 biases toward the dark end. Two rounds of "darken this" brightened it instead.
- **The staff's leather grip sat at exactly the hand's height**, so its stripes ran across the
  fingers and it read as a candy cane in a fist. The bindings are now clear of the grip.
- **A clean elliptical hole in the canopy reads as a vignette**, not as a gap in leaves. The
  clearing edge is perturbed by three sine harmonics in `canopy()`; without them the effect gives
  itself away at a glance.
- **Emissive ramps belong on the emitter, not on what it lights.** The ward's ground wash was
  first painted on `E["arcane"]` like the rings themselves, and it filled the floor with a flat
  slab of saturated purple that read as a rug. What the ward lands on is stone, so the wash is
  built from the **violet material** row and only the rings are allowed to emit.
- **A figure standing in a light still blocks it.** Painting the ward back over the near lip of
  his contact shadow — to "soften" it — punched a visible hole in the floor. The shadow stays
  `occ`; what it needed was a dithered edge, not a brighter middle.
- The ward sits at `cyp = 568`, not 578, purely so its lower arc clears the PRESS ANY BUTTON band at
  rows 616–636. Brightening the circle is what made that collision matter.
- **A flame is a body with a thin core, not a bloom with a body inside it.** The first version
  put one filled ellipse of the ramp bottom behind each flame and gave the core half the body
  width; both read as a glowing ball with a tail. The bloom is now two stages — a wide dim halo
  in *pigment* gold and a tight emissive one — and the core is 30% of the body width.
- **A shadow that takes an edge light stops being a shadow.** The hood void was a flat `occ` quad
  wider than anything around it, so in those rows it *was* the outer silhouette and both edge
  passes obligingly lit it — a bone rim and a gold bounce on the darkest part of the figure. Both
  passes now take a `skip` rect; keep it around `_hood`. Anything else meant to read as absence
  needs the same treatment.
- **Eyes are cheap and it is easy to overspend them.** 34 pairs with an amber core turned the
  wood into a firefly meadow. Nine in the treeline and seven in the branches, a step down the
  fire ramp so they read red, is the amount that reads as *watched*. (The PC set has 22, but a
  16:9 screen shows nine of them and a 3:2 one four; see "The eyes in the dark".)

## The animation

**Built.** `python tools/art/anim.py` writes the layer set to `assets/bonelight/ui/title/`;
`scenes/TitleScreen.tscn` stacks it and `scripts/TitleScreen.cs` drives the frames. The flat
`splash.py` render is untouched — `vigil()` with `layers=False` still produces the shipping
`title-screen.png` byte for byte, and it is worth keeping that true: it is the only cheap check
that the layer split has not drifted from the composition.

**To verify a change:** composite the layers in the order below, with the eye cells pasted open
at their `eyes.json` places between `back-b` and the ward, and diff against `title-screen.png`.
It must be **zero** differing pixels. That check is what caught the eye-depth bug below at 44
pixels out of 230,400 — far too few to spot by eye, and completely wrong.

Four behaviours, in the order they matter:

1. **The eyes in the dark blink.** Occasionally, and not together. **Done 2026-11-01**: every
   pair on its own clock - see "The eyes in the dark".
2. **The magic pulses.** Lightly and slowly — a breath, not a strobe.
3. **The six flames burn.** They are currently a still frame of something that should be moving,
   and they are the most obviously frozen thing on screen.
4. **The edge light on the wizard flickers with the flames.** Not on its own clock.

### The rule that governs all four

**Animate by frame, never by tint.** `modulate`, alpha fades and cross-dissolves all blend
colours, and a blended pixel is by definition not one of the 66 in the contract — the palette
audit that `splash.py` runs would stop meaning anything the moment anything on this screen
breathes with an alpha ramp. Every state of every animated thing is a *generated frame* with a
closed palette, and the scene swaps between them. That is also why the prompt is the one thing on
screen that is allowed to fade: it is bone-white type on near-black, alone on its own layer, and
nothing behind it can be dirtied.

The corollary is that `splash.py` grows a frame-emitting mode. Nobody hand-animates any of this;
the generator already knows how to draw every piece, and a flame at a different seed is a
different frame for free.

### What it costs: the screen has to come apart into layers

One baked PNG cannot animate a part of itself. The split, back to front:

| Layer | Frames | Driven by |
|---|---|---|
| `back-a` — sky, far wood, undergrowth, leaf mass | 1 | — |
| `back-b` — mist, ground, grass, creep, near trees, canopy, wordmark | 1 | — |
| `Eyes` — not a texture: one 11×4 cell per pair, from `eyes.png` at `eyes.json`'s places | 3 + shut | each pair's own clock |
| `ward-*` — pools, rings, hexagram, runes | 3 | `WardPulseSeconds`, walked 0,1,2,1 |
| `flames-far-*` | 4 | `FlameFrameSeconds` |
| `figure-*` — the whole figure, rim and bounce stepped down | 4 | **the same index as the flames** |
| `flames-near-*` — near flames and the motes | 4 | the same index again |
| `fore` — foreground grass and brush | 1 | — |

Eighteen PNGs and the eye sheet (it was thirty while the eyes were two layers of six whole-screen
frames). The pulse is carried by the **size** of the ward light pools and by whether the
hottest tone survives on the rune ticks; the flicker steps the rim from `RIM` down through two
bone tones and shifts the bounce and underlight one index toward their dark end. Nothing anywhere
is tinted or faded.

Three things that are not obvious from that table:

- **The flames straddle the figure.** `ward_flames(False)` draws the far ones, then the figure,
  then `ward_flames(True)` draws the near ones. Any animated flame layer is therefore *two*
  textures with the figure sandwiched between them, and both have to advance on the same clock or
  the near and far flames of one ring will be visibly out of step.
- **The vignette survives the split, but not for free.** It is a pure per-pixel function of
  position, so applying it independently to each layer gives the same result as applying it to
  the composite — *provided it skips transparent pixels*. It writes a colour chosen by position
  rather than multiplying what is already there, so unguarded it paints an opaque dark frame into
  the empty corners of every transparent layer. Hence `only_opaque=` on `raster.Canvas.vignette`.
  It does not work as a top overlay either, because that would need alpha. It must also only ever
  make a pixel **darker**: `stone.deep #0B0F18` is lighter than `occ #05070C`, so an unconditional
  write replaced 63% of the pixels in a corner sample with a mid-dark grey, which read as a filter
  laid over the whole image and flattened the contrast of exactly the areas meant to be deepest.
- **The two eye sets were at different depths, and the backdrop was split around them.** The
  treeline pairs were drawn early and then legitimately covered by the ground, the creep, the
  near trees and the canopy; the branch pairs were drawn after the canopy and sat in front of it.
  Lifted onto one layer above a single backdrop, the treeline pairs shone straight through the
  foliage meant to be in front of them - hence `backA` / `eyesA` / `backB` / `eyesB`. Since
  2026-11-01 no pair goes anywhere anything is drawn in front of it (a pair half under a leaf was
  a smear, not a pair of eyes), so both sets sit on one `eyes` stage above `backB` and depth is
  told by tone alone. `backA` and `backB` are still two layers; merging them is a free cleanup.
- **Every layer must be fully opaque or fully transparent, never in between.** Partial alpha
  anywhere reintroduces blending, and with it off-contract colour.

### The fade band is the whole game

Every soft edge in this project is a dithered ramp, because the palette has no intermediate
values and no alpha. `raster.Canvas.radial` spreads its stops evenly from centre to edge and
dithers between each adjacent pair, so **the number of stops decides how much of the radius is
speckle**. A two-stop radial dithers across its *entire* radius and is an exact 50/50
checkerboard at the half-way line — at ×2 render, a lattice of 2×2 blocks.

That is what put a fuzzy grid around the base of every ward flame: `flame()` drew its bloom as a
two-stop and a three-stop radial, both taller than they were wide and both centred well above the
base, so the fire stood inside a ball of checkerboard instead of on a pool of light. The fix is
not fewer dithered pixels, it is **repeating the leading stops** so the inner bands come out solid
and only the outermost one fades:

| ramp | dithered fraction of the radius |
| --- | --- |
| `[a, None]` | all of it |
| `[a, b, None]` | the outer half |
| `[a, a, b, None]` | the outer third |
| `[a, a, b, b, None]` | the outer quarter |

A glow wants the last two rows. `flame()` now uses a wide **flat** ground pool sitting on the
baseline (light landing on the floor) plus a narrow vertical air glow hugging the flame, both
solid-cored, and two small opaque discs where the flame meets the ground so the taper starts from
something. The orb aura in `hero.py` follows the same rule.

The same arithmetic is the reason the ward's floor pool was already written as
`[g2, g2, g3, g3, g4, g4, None]` rather than `[g2, g3, g4, None]`.

### The GIF preview must carry an exact palette

`tools/art/preview_anim.py` writes `tools/art/_title-anim.gif`, which is how the animation gets
looked at without launching Godot. It is worth knowing exactly how that export can lie, because
it did, and the symptoms point straight at the art rather than at the exporter.

Pillow converts an RGB frame to P mode for GIF by snapping it to the **216-colour WEB palette** —
every channel a multiple of 51 — and error-diffusion dithering the difference. Not one Bonelight
colour is in that palette. `occ #05070C` and `stone.deep #0B0F18` collapse together, the mid-tones
are pulled toward grey, and a 39-colour frame comes back with 85 in it. Measured: the old export's
frame zero differed from the shipping `title-screen.png` in **230,332 of 230,400 pixels** — 99.97%
of the frame.

The flicker is the same mechanism in the time axis. Floyd–Steinberg carries its error rightward
and downward across the whole image and is recomputed per frame, so a change confined to the six
flames re-rolls the dither of everything downstream of them:

| pixels changed by one flame phase | old export | exact palette |
| --- | --- | --- |
| frame 0 → 1 | 44,245 | 906 |
| frame 1 → 2 | 134,158 | 923 |
| frame 2 → 3 | 131,350 | 911 |

A 900-pixel change presented as a 134,000-pixel one is a screen that crawls. `indexed()` now maps
each frame onto a 39-entry palette by exact dictionary lookup — no quantiser, no dithering — and
GIF frame zero is now bit-identical to the shipping still. **That equality is the check**: if the
preview and `1-vigil.png` ever differ by a pixel at frame zero, the exporter has started
approximating again.

Only the preview was ever affected. The layers, the shipping PNG and the running game share no
code with this path.

### Per-behaviour notes

**Eyes.** Built, nearly as this note proposed: the generator emits the pair positions alongside
the art and each pair has its own timer. The difference is that a pair is drawn from a tiny
sheet of generated cells rather than in `_Draw()`, because a blink wants in-between frames (a
lid half down) and an eye is now a 4×4 disc, not one or two pixels. See "The eyes in the dark".

**Pulse.** Three or four frames of the ward stepped up and down the ward ramp, cycling slowly.
Amplitude is the whole risk: the ward is already the brightest thing in the frame after the orb,
and the note is *lightly*. If the pulse is legible as a pulse on a still comparison of two frames,
it is probably too strong in motion.

**Flames.** `flame()` already takes a `seed`, so frames are the same six flames re-drawn at
successive seeds. Two cautions: the disc-stack shape means consecutive random seeds jump rather
than flow, so the seeds want to walk a path rather than be independent draws; and the bloom should
move *less* than the body, because a halo that flickers as hard as the flame reads as the whole
screen strobing. As built, the bloom does not move at all — only the body, core and sparks are
seeded — which is deliberate and is why the ring does not strobe.

**The orb.** The staff head is the brightest thing on the screen and it drives the rim, the bounce
and the underlight on the figure, so it has to breathe with them: if those step down and the
source does not, the source reads as a decal stuck on the picture. `wizard_hero` scales the aura
radius and steps its hot stop off the same `step` the rim uses. The forked claw is drawn over the
bloom (so it is not swallowed) and then a second, sparse pass of the aura goes over the claw, so
the light lies *across* the metal instead of stopping at it. Without that pass the claw is the
one object in frame the light goes around.

**The shaft runs all the way up into the orb.** It used to stop at `yb - 34u`, a hand's width
short of the head, while the bloom is centred 3u *above* the head with a radius reaching 2.2u
*below* it — so the top ~10px of the handle sat inside the glow and dithered away, and because
that radius breathes with the flicker, the amount eaten changed every frame. Handle and head read
as two objects with a flickering gap between them.

**A collar over the joint is not the fix, and was tried.** It read as exactly what it was: a
bright patch stuck on to hide a seam, obvious and obnoxious at any size. Three things replace it,
and between them there is no seam left to hide:

- The shaft ends at `yb - 36.5u`, *inside* the orb, so the orb sits on the end of it rather than
  floating above it.
- The fork legs root into the shaft's *sides* at `yb - 31.2u`, well below the head, with a lit
  fillet where each leaves the metal. Fork and shaft are one casting, not two parts touching.
- The tone climb up the shaft is **per-column and staggered**, never a full-width `rect`. Two
  rect steps put a hard horizontal line across the metal, which is a seam again — just further
  down than the one being fixed. Staggering it by column turns the step into a diagonal that
  reads as a falloff.

**The scene is lifted 48px off the bottom, and the lift is a translation.** `LIFT` in
`splash.py` moves every finished layer up the frame just before the vignette runs, which hands
the bottom band of the screen back to the UI. The composition is authored against a floor line
at `y=428`, a ward at `y=568` and about thirty other anchors; re-deriving all of them from a new
horizon produces a *different picture*, where a translation produces the same picture higher up.

Three things make it cheap, and all three depend on where in the order it happens:

- **Before the vignette.** That pass is a pure function of position, so it crushes the newly
  exposed band *and* the floor's cut edge above it toward `occ` in the same sweep that darkens
  the rest of the frame. The seam does not read as a crop because by the time you see it, it is
  the same colour as everything around it.
- **Before the wordmark.** The lettering (the first title then, THE ONE HE MISSED now) is stamped
  afterwards and keeps its own height. Had it been lifted with the scene it would sit 30px off the
  top edge.
- **Every layer by the same amount**, via `Canvas.lift`, so the animation layers still composite
  to the flat render exactly and `TitleScreen.tscn` needs no change — the rects did not move,
  their contents did.

Measured after: the PRESS ANY BUTTON band at rows 616–636 is **100% pure occlusion**, as are the
bottom 40 rows; the lowest lit pixel anywhere in the frame is `y=560`. There are 80 authored rows
— 160 device pixels — of clean black under the art for a prompt, a version string or a menu.
**That is true of the 640 frame only.** In the shipping 840 frame that black band is gone:
on a tall phone it was a hard line under the ring, so the ground now carries on through it and
falls off to a dark, low-contrast dither behind the type (see "The letterbox was not invisible"
above).

**PRESS ANY BUTTON moved up with the art.** The `Prompt` `TextureRect` is anchored to the bottom
of the viewport, so the lift did not carry it: it stayed at device offsets `-48 .. -8` while the
picture above it climbed away. Its offsets were then `-100 .. -60`, and are `-300 .. -260` since the frame grew 100 rows at
each end (the node is anchored to the bottom of `TitleImage`, so it had to move with the new
bottom band). That put the 360x20 strip on authored rows **590-610**. **It has since moved
again**: it is bigger (scale 3, a 360x24 strip) and `TitleScreen.PlacePrompt` puts it near the
bottom of whatever the screen shows - see the knobs table. The clearance is 88 canvas units
rather than just enough for a rounded corner because the version label sits 12-40 units off the
bottom (36 more under SafeArea on a phone), and at 60 the label ended up beside the P.

**The glow draws in front of the metal.** The fork was drawn over the bloom so the claw would not
be swallowed, which put a hard gold cutout across the front of the light source — the one thing
on frame the glow went *behind*. The metal goes down first now and the halo lies over it. That
only survives because the halo is nearly all fade: the legs sit at 1.7–3.4u, out in the dithered
part, so the light stipples *across* them rather than filling over them. Reverting the halo to a
mostly-solid ramp would erase the claw entirely.

**A glow is the fade. The solid part is the lamp.** This is the one place the fade-band rule
above inverts. A flame's ground pool wants its leading stops repeated, because there the solid
*is* the subject and the dither is only its edge. A halo is the opposite: the dither is the whole
subject, so the ramp wants to be **short** and the fade wants to be most of the radius. The orb's
bloom held a solid disc out to 3.9u — a flat slab of `#FFC63C` nearly twice the radius of the orb
itself, carrying the hard polygonal edge that a banded radial gives you. That is not an effect,
it is an oversized fill, and it read exactly as what it was: a yellow blob sitting on the staff
head. Three stops (`[a, a, None]`) put the solid edge at the orb's own radius and leave everything
outside it as halo.

**Every pass that builds one light shares one centre, and every radius is equal on both axes.**
The bloom was at `-36.6u`, the orb core at `-37.0u` and the over-pass at `-35.8u` with `ry 5.4`
against `rx 4.8`. Three centres and an ellipse: the light pooled low and the glow could not come
out round however it was tuned. One `hy`, circles only.

**The head's local variable must not be called `oy`.** `wizard_hero` sets `scene, ox, oy` as the
canvas blit origin at the top and uses it in the final `scene.blit(c.img, ox, oy)`. Naming the
orb's centre `oy` shadowed it and moved the whole figure ~300px up the frame — a silent one, since
the render still succeeds and audits clean. The layer's `getbbox()` is the check that catches it.

**Motes are sparks or they are nothing.** Two rules, and satisfying only the first is worse than
satisfying neither. Their x offsets must sum to about zero, or they drag the apparent centre of
the glow sideways even when the bloom is perfectly centred. But every one must also sit *further
than the bloom's reach* from the orb, and none may wear `ramp[3]` — that is the halo's own tone.
Balancing the sum alone once moved a mote to 2.6u, inside the glow and the same colour as it,
where a disc of pale gold on a gold halo is not a spark, it is a blob stuck to the head.

**A centre computed from a float is not the centre that gets drawn.** `Canvas.rect` truncates
its bounds — `range(int(x0), int(x1) + 1)` — so a shaft asked for at `sx = 20.625` with `w = 6`
is painted on columns **20..26, true centre 23.00**, while `sx + w * 0.5` is **23.625**. The
bloom, the orb, the fork and the motes were all centred on that float, which put every one of
them 0.62px right of the shaft they belong to — 1.25 device pixels at the ×2 render, and quite
visible: the glow reads as off-centre and the head reads as not quite mounted straight. The head
centre is therefore `(int(sx) + int(sx + w)) * 0.5`, taken from the columns that actually get
painted. **This applies to anything mounted on anything else in these generators**, not just the
staff.

The motes are the other half of that complaint and are not a rounding bug. Their x offsets summed
to −0.8u, so the apparent centre of the glow sat left of the shaft even once the bloom itself was
centred. They are meant to be irregular; they are not meant to be lopsided, so the offsets now sum
to zero.

**The fork legs are backlit, so they are drawn in shade gold, not base.** They sit between the
viewer and the orb. In base gold they washed straight into the bloom and the head read as a light
bulb; in `gold.shade` with one lit edge and an `occ` inner line they read as metal silhouetted
against light, which is both correct and the only way the claw survives at that brightness.

One over-pass of the aura covers the shaft *and* the fork, outer stops only. Lighting the fork
but not the shaft is what let the eye read the unlit part as a separate object in the first
place: everything metal near the orb is inside the light, so all of it takes the same stipple.

**Rim flicker.** `hero.py` already draws the figure onto its own transparent layer so `_rim` and
`_bounce` have an edge to find — which means emitting just those two passes as their own frames is
a small change, not a rebuild. The rim colour is `RIM #DCE8FF`, which is **reserved**: flicker
means varying *which pixels* get it, not what colour they are. And it has to be phase-locked to
the flame frames — the same index into both sequences — or the screen has two unrelated
animations happening near each other instead of one light source and the thing it lights.

### Open questions

- `TextureRect` in a `Control` tree, or `Sprite2D`/`AnimatedSprite2D` on a `CanvasLayer`? Either
  works, but whatever replaces the current node must keep the **fixed 720×1680 rect at exactly
  ×2** — the whole reason the scene is built the way it is (see above) is that fractional scaling
  of pixel art is the failure the art direction exists to prevent.
- Frame rate. The contract's animation section is the place to settle this, not this doc.
- Whether the motes drift as part of the ward layer or get their own; they are the one element
  that would look better on a continuous path than on a frame cycle.

## The landscape set (PC)

A PC runs a 1280x720 landscape canvas (`DisplayProfile`; stretch aspect `keep_height`, so the
canvas is always 720 tall and a wider monitor gets more width). The portrait art centred in that
lost the wordmark off the top and the ward and prompt off the bottom, so a PC gets **a second,
separately composed set** - the same scene, drawn for the other shape. The phone keeps the tall
set untouched.

| File | What it is |
|---|---|
| `tools/art/splash_wide.py` | `vigil_wide()` - the composition; writes `title-screen-wide.png` |
| `assets/bonelight/ui/title-screen-wide.png` | 960x360 flat render |
| `assets/bonelight/ui/title-wide/*.png` | the same 18 layers and frame counts as `title/`, at 960x360, and its own `eyes.png` / `eyes.json` |

`python tools/art/anim.py` now emits both sets; `anim.py tall` or `anim.py wide` makes one.

**Geometry.** A 640x360 core drawn at x2 fills 1280x720 exactly, centred in a 960x360 frame
(1920x720 at x2), so a 21:9 monitor (canvas 1706x720) still sees picture: the same move as the
tall portrait frame, turned on its side. `edge_t()` leaves the core almost untouched and closes
the two 160-column side bands to `occ` at the frame edge.

**Layout**, in core pixels (wide x = core x + 160):

| what | where | |
|---|---|---|
| figure and ward | feet at (430, 256) | right of centre; `hero.wizard_hero` at h=150, as on the phone |
| lettering | centred x 175, cap line 16, to row ~116 | `WORD_BOX` = `word_box()`, read off `title_lettering`; scales (4, 3), see below |
| Start Run, Arcane Upgrades + Unlocks, the energy caption | (60,134)-(290,240) | `MENU_BOX`, on the dark wood under the wordmark |
| (nothing) | (430,8)-(630,34) | `ARCANE_BOX`, top right in the canopy. The Arcane Energy readout lived here; it is the shop button's caption now, and the pocket left `ui_pocket_t` when the wide set was regenerated on 2026-11-02 |
| Options | (12,292)-(126,324) | `OPTIONS_BOX` |
| Evolution Codex | (134,296)-(236,320) | no pocket of its own; a quiet button on the dark grass, clear of the ward's left rim and flame. Report a bug stood beside it until 2026-11-02 and is in Options now |
| difficulty | (470,312)-(632,352) | `DETAIL_BOX`, right-aligned; one line since the next-run gain line went (2026-11-02) |
| PRESS ANY BUTTON | rows 322-346, centred | `PROMPT_BOX`; `TitleScreen.WidePromptBottomRow` = 346 |
| version label | (4,338)-(48,356) | `VERSION_BOX`; where `AddVersionLabel` lands on a 16:9 screen |

**Every one of those boxes is a dark pocket in the art** (`ui_pocket_t`), soft-edged and only as
dark as it needs - held at full strength with a short fade, the empty ones read as black panels
when the menu was closed. **The fade has to span the darkening** (`_box_t`): `_darken_rows` does
nothing below t = 2, so a linear fade from the pocket's strength spent most of its width doing
nothing and crossed grass-to-occlusion in the last few pixels - the prompt pocket left a ruled line
through the grass under the ward until 2026-11-02 (Eric: "still has a distinct line in the grass").
Each pocket now runs from t = 2 at its edge, eased, to its strength at the box; the prompt's fades
40 along the row but only 16 up, because the ring is about 20 rows above it and a longer fade
dimmed its bottom flame. **And the pockets under the ward are light**, a dither of `stone.deep` only (strengths
2.2-2.4): with the line gone Eric still saw "a big dark patch underneath", and a render with no
pockets at all showed that the pockets alone made it - the ward lights grass and bushes right down
to the frame. PRESS ANY BUTTON carries its own `occ` drop shadow; the menu's bottom row are framed
buttons and an outlined label. Only `MENU_BOX`, on the dark wood, is still a real pocket. The boxes in `splash_wide.py` and the numbers in
`MainMenu.PinToWideArt` calls are the same rectangles: **move one, move the other.** In landscape
a core pixel (x, y) is always at `((x - 320) * 2, (y - 180) * 2)` from the screen centre, so
`PinToWideArt` takes the core box directly. Portrait keeps `PinToArtRows` exactly as before.

**How the scene picks.** `TitleScreen.tscn` is authored with the portrait set. On a landscape
screen `TitleScreen.UseLandscapeArt` resizes `TitleImage` to 1920x720 (offsets +-960/+-360),
points `back-a`, `back-b` and `fore` at `title-wide/`, and shows frame 0 of every animated layer
from it; `ArtFolder` decides which folder every frame comes from. `PlacePrompt` has a fixed
landscape row because the canvas is always exactly the art's height. The layers composite to the
flat wide PNG with zero differing pixels, like the tall set.

**Composition rules the wide one keeps:** the figure is the portrait figure; the wordmark sits in
a break in the canopy with low far trees under it; both eye sets stay out of the menu column,
the wordmark and the figure; the undergrowth is toned by ward light like `_undergrowth()` (bushes banked up both
sides, pressing in under the menu and right of the ward, fern, big near leaves in the corners);
foreground grass is held out of the ward. Composed for the wide frame rather than cropped from the
tall one because the shapes want different layouts - the menu beside the figure, not above it.

**The PC lettering is the phone's size now.** Under the old name the PC wordmark was the bigger
one: the phone had (3, 2), the wide set (5, 3) - WIZARD 310 and SURVIVORS 279, 620 and 558 canvas
units on a 1280 screen. THE ONE at 5 is 340 wide, which runs off the left edge of a 16:9 screen or
into the staff, so both screens set the stack at (4, 3): 544 and 522 canvas units on a PC, about
what the old one came to. Whole pixel scales only.

**The eyes are spread, never stacked (2026-10-29, then 2026-11-01).** Eric, of this set first: "the
main screen background awkwardly has eyes that sit on top of each other. the red eyes should be
spread out more randomly like on the mobile screen." The first fix kept pairs apart with a spacing
ellipse and capped the strip beside the staff; two days later he asked the same of both screens,
and for every pair to be round and to blink, which replaced the whole mechanism. Both sets now
place their eyes the same way (see "The eyes in the dark" below). What is particular to this one:

- **Five pairs in the treeline and four in the branches across the core**, which every screen
  shows, and more in the side bands an ultrawide shows: seven left, six right, all the same round
  pair (the old side-band ones were smaller, duller blobs). 22 in all.
- **The free wood in the core is three strips**, and `EYE_CAPS` holds each down: the gap between
  the menu column and the staff (2), the sliver left of the menu that only 16:9 shows (2), and the
  wood right of the figure up to the 16:9 edge (5). Uncapped, the twelve core pairs went seven to
  the right-hand strip, which on Eric's 3:2 Surface (x 210-750 of the frame) is a swarm in one
  corner. Measured now: nine pairs in a 16:9 view, five at 16:10, four at 3:2, nineteen at 21:9.
- **No pair straddles a screen edge** (`SCREEN_EDGES`: 160/800 for 16:9, 192/768 for 16:10,
  210/750 for 3:2, about 53/907 for 21:9). Across one, a screen of that shape shows one eye and a
  sliver - which the old set did at the left edge of a 16:9 screen.
- The slot between the wordmark and the menu is kept clear, the wordmark's width: a pair there sat
  under the S of SURVIVORS beside Start Run and read as part of the type.

`_UiShot` takes `--open-menu`, which sets `Global.OpenMenuImmediately` so the shot is the menu
over the art as a player sees it, rather than `MainMenu.tscn` on a black ground.

**x2 is whole on the canvas, not on the screen.** The window stretches the canvas by a fraction
(×1.25 in the default window, ×2.1375 full screen on 2736×1824), so the art lands on 4- and
5-pixel columns there. It is uneven, not blurry - a 2736 capture keeps the 36-colour palette - and
it cannot be fixed the way the character select's portraits were (`.ai/art-direction.md`, 6a):
the menu is pinned to rectangles in this art, so a per-window scale would move it off its
pockets.

## The eyes in the dark

**2026-11-01, Eric:** "The eyes in the background don't always look like eyes. They should all be
circular and blink, and be randomly spread out rather than some being on top of each other." All
three were true of both sets, and all three had one cause each. `tools/art/title_eyes.py` is the
whole answer; `splash.py` and `splash_wide.py` only say where each set's bands are.

**Shape.** Each eye was a radius-3 `radial` on a three-stop ramp, and a radial dithers its stops
(see "The fade band is the whole game"), so every eye came out a different blob: a square, a plus,
a bar. With the two eyes 3-5 pixels apart at radius 3 a pair often ran together into one smear.
The treeline set was drawn *behind* the leaves and the ground, so a pair half under a clump was a
crescent or a dash, and the vignette swapped pixels out of pairs near the edges. Now:

- One eye is a **4×4 disc with its corners off**, the smallest shape on this grid that reads as
  round (a 3×3 is a plus or a square, which is what half the old ones were). Both eyes level, a
  fixed 3 apart, so a pair is an 11×4 cell. A 2×2 core on a rim: `fire.mid` on `fire.edge` for
  the branch pairs, the old red note; `fire.edge` on `ember.shade` for the treeline, so they sit
  further off by tone. Both are drawn at one depth now, so tone is the only depth cue a pair has.
- Pairs are **stamped after everything** - the vignette, the wordmark and the tall frame included
  - so nothing dithers, darkens or cuts them.
- A pair goes **only where nothing in front of the eyes comes near it**: the ward, the flames, the
  figure and the foreground, grown by a pixel (`COVER_PAD`), and the figure grown by 14
  (`FIGURE_PAD` - at 8 one sat among the sparks over the orb and read as two more). That is what
  lets one stage above `backB` hold both sets, and what makes stamping them last the same picture
  as stamping them in their place: the flat PNG and the layers plus cells still agree to the pixel.
  `anim.py` checks every animation frame, not just frame 0, and prints `covered pairs: 0`.
- **Only on dark wood**: over the cell and two pixels round it, a mean luma of at most 22 and no
  pixel above 50. The mean keeps pairs off the sky round the wordmark and out of the lit
  undergrowth; the ceiling keeps them off stars and type. The first rule tried was "no pixel above
  34", and it left so little of the wood that every pair crowded into what there was.

**Spread.** Nothing kept two pairs apart, so random draws stacked. Now every pair keeps `SPACING`
(30×22, an ellipse, centre to centre) from every other on either depth, and each is the best of
five random valid spots - the one furthest from the pairs already placed (Mitchell's best
candidate) - so the set spreads out without looking laid out on a grid. Past about five
candidates it does. The phone's branch set is placed first because it has the least room (the
wordmark and the menu, `MENU_ROWS`, take the middle of its band, and the slot between them is kept
clear too); the treeline gets what is left. Positions use `rng.random()` only, so 2.7 and 3 agree.
Measured: 16 pairs on the phone, the closest two 36 apart; 22 on the PC, 35 apart.

**Blink.** Six whole-screen frames each shut one pair, so five of sixteen pairs ever blinked, and
only one at a time. Now:

- `anim.py` writes **`eyes.png`** (a row per tone, a column per frame: open, lid half down, a slit;
  35×9 with a pixel of gutter) and **`eyes.json`** (each pair's cell position, in art pixels, and
  tone) beside each layer set. Nothing in code knows a position.
- `TitleScreen.SetUpEyes` puts one `TextureRect` per pair, an `AtlasTexture` region of the sheet,
  under the `Eyes` node (between `BackB` and `Ward`), at twice its art position - the same whole
  ×2 as every layer, so a cell sits on the layers' pixel grid on both canvases. Sixteen nodes,
  six shared `AtlasTexture`s, one float per pair per frame: nothing measurable.
- `TickEyes` gives each pair its own clock: a 5-16 s wait, then half, slit, shut, slit, half at
  40/40/90/60/60 ms (the lid comes down from the top and goes back up a little slower), and one
  blink in five is followed by a second. **Never together**: at most `MaxBlinkingPairs` (2) are
  mid-blink and no two start within `BlinkStagger` (0.15 s); a pair that comes due inside either
  limit waits a moment and tries again. Across sixteen pairs that is a blink somewhere about every
  0.7 s.
- **By frame, never by tint.** A cell swaps between generated frames in contract colours, and shut
  is the cell hidden. Nothing is faded.

**To change the eyes**, edit `title_eyes.py` or the bands in the two generators and run
`python tools/art/anim.py`, then `splash.py` and `splash_wide.py` for the flat PNGs. Only
`eyes.png`, `eyes.json` and the two flat PNGs should change; every layer comes out byte-identical,
because the eyes are no longer in any of them. `preview_anim.py` blinks every pair once in its
loop and keeps frame 0 identical to `title-screen.png`.

## Verifying a change

`dotnet build` does not touch any of this. After regenerating:

1. `"$GODOT_BIN" --headless --path . --import`
2. `"$GODOT_BIN" --headless --path . scenes/TitleScreen.tscn --quit-after 260` — 260 frames runs
   past the prompt fade completing (~2.1s) and into the breath loop, so a fault in the tween chain
   actually surfaces. `--quit` alone exits on frame one and proves nothing about it.
3. The `ObjectDB instances leaked at exit` warning is pre-existing and unrelated — `MainMenu.tscn`
   prints the same.
4. `splash.py` audits every finished image against the contract palette and prints the count.
   It must be **0 off-contract colours**. `anim.py` prints the same for every layer and the eye
   sheet, and `covered pairs: 0`.
5. The eyes blink only in a real window, so a headless run says nothing about them. Burst
   `_UiShot` over a couple of seconds (`--frames=60 --burst=40,4`) at both canvases and look for a
   pair mid-blink; the blink is about 0.3 s, so a 4-frame gap catches three of its steps.
