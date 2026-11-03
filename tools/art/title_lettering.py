# -*- coding: utf-8 -*-
"""The title's lettering: THE ONE HE MISSED, in three layouts, one of which ships.

Run: python tools/art/title_lettering.py

It writes a comparison sheet to tools/art/_title_lettering/ (gitignored): each layout drawn into
the finished title art for both screens - the PC's 16:9 view of the wide set and the Pixel 9's
view of the tall one - so the three can be judged as a player sees them rather than as type on a
black ground. It never touches a shipping file; splash.py, splash_wide.py and anim.py do that, and
they draw whichever layout SHIPPING names.

Why the name changed the art (Eric, 2026-11-02: "The title screen art needs to change because the
title changed"). The game was Wizard Survivors. It is now The One He Missed, from the premise's own
sentence (.ai/world-and-tone.md): a dark wizard took every wizard who could have stood against him,
and you are the one he missed. The blue-robed figure in the ward is that player - the one wizard
he did not take - and the ward's gold is the one warm light left in the wood. The lettering says
the same thing in the only way type can without a tagline: THE ONE is lit in the ward's gold, and
everything else is set in cold bone, the colour of the world he left.

Each layout is given twice, in the coordinates its screen is authored in:
  tall  the 360x640 Vigil composition (splash.vigil), before it is grown into the 840 frame;
        the menu's buttons start at row 170 (MainMenu.PinToArtRows 340), so the lettering has to
        end above about row 162 with its shadow
  wide  the 640x360 core of the landscape set (splash_wide adds its C0 to x); the menu column is
        x 60-290 from row 134 (MainMenu.PinToWideArt), the staff stands at about x 372

A line is (scale, words, gap) as splashkit.lettering reads it. Scales are whole pixels only, and
every glyph is the DISPLAY face's own (font.py) - never a scaled-up body face.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

import bonelight as bl
import raster
import splashkit as sk

M = bl.MATERIALS
OCC = bl.OCC

# The two inks, both the old wordmark's: bone white running down to its grey-blue for the cold
# words, and the gold pigment row for the lit one. Pigment, not the Light element ramp the ward
# burns on: lettering is signage over the scene, not a light in it (.ai/title-screen.md,
# "emissive ramps belong on the emitter").
BONE = [M["skin"][0], M["skin"][0], M["skin"][1], M["skin"][2], M["skin"][3]]
# The old SURVIVORS ran gold 0-1-2 down its rows, which on a line twice as tall left the pale cap
# a sliver and the line darker than the bone one under it; doubling the first two stops gives the
# lit phrase the brighter top it needs to lead.
GOLD = [M["gold"][0], M["gold"][0], M["gold"][1], M["gold"][1], M["gold"][2]]

LAYOUTS = {
    # SHIPS. Two lines, THE ONE over HE MISSED, at scales 4 and 3: 272 and 261 wide, so the block
    # reads as one justified stack the way the old one did (WIZARD and SURVIVORS both came to 186).
    # The words break where the sentence does - the one / he missed - and the break puts the lit
    # phrase on top, alone on its line. One size on both screens: on a phone it fills the break in
    # the canopy across most of its width, on a PC it stands in the left third over the menu.
    "stack": dict(
        name="A  STACK",
        note="THE ONE over HE MISSED - scales 4 and 3",
        tall=dict(cx=180, y=58, lines=[(4, [("THE ONE", GOLD)], 9), (3, [("HE MISSED", BONE)], 0)]),
        wide=dict(cx=175, y=16, lines=[(4, [("THE ONE", GOLD)], 13), (3, [("HE MISSED", BONE)], 0)])),
    # A poster: the one word that carries the premise set big and lit, and the rest of the sentence
    # small and cold round it. The strongest single shape of the three, and the hardest to read as
    # a sentence - it says ONE first and the title second.
    "ember": dict(
        name="B  EMBER",
        note="THE, ONE, HE MISSED - scales 2, 5 and 2",
        tall=dict(cx=180, y=44, lines=[(2, [("THE", BONE)], 5), (5, [("ONE", GOLD)], 7),
                                       (2, [("HE MISSED", BONE)], 0)]),
        wide=dict(cx=175, y=8, lines=[(2, [("THE", BONE)], 5), (5, [("ONE", GOLD)], 7),
                                      (2, [("HE MISSED", BONE)], 0)])),
    # An inscription: one quiet line across the top of the picture, so the scene carries the screen
    # and the name is read rather than looked at. On a PC it runs over the clearing at scale 3; a
    # phone has room for it only at scale 2 with the tracking taken in, which is small for a title.
    "line": dict(
        name="C  LINE",
        note="One line - scale 3 on a PC, 2 on a phone",
        tall=dict(cx=180, y=92, track=1,
                  lines=[(2, [("THE", BONE), ("ONE", GOLD), ("HE MISSED", BONE)], 0)]),
        wide=dict(cx=320, y=20,
                  lines=[(3, [("THE", BONE), ("ONE", GOLD), ("HE MISSED", BONE)], 0)])),
}
ORDER = ("stack", "ember", "line")
SHIPPING = "stack"

SIZES = {"tall": (360, 640), "wide": (960, 360)}
WIDE_C0 = 160          # splash_wide.C0, the core's left edge in the wide frame


def draw(c, which, shape, dx=0):
    """Set layout `which` for `shape` ("tall" or "wide") onto canvas c, x moved by dx (the wide
    set passes its C0). Returns the bottom of the last line."""
    s = LAYOUTS[which][shape]
    return sk.lettering(c, s["cx"] + dx, s["y"], s["lines"], track=s.get("track", 2))


_BOXES = {}


def box(which, shape):
    """The lettering's bounding box, halo and drop shadow included, as (x0, y0, x1, y1) with x1/y1
    exclusive - in the tall composition's pixels, or the WIDE FRAME's (C0 already added)."""
    key = (which, shape)
    if key not in _BOXES:
        c = raster.Canvas(*SIZES[shape])
        draw(c, which, shape, WIDE_C0 if shape == "wide" else 0)
        _BOXES[key] = c.img.getbbox()
    return _BOXES[key]


# ------------------------------------------------------------------ the comparison sheet
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_title_lettering")
PIXEL9_ROWS = (16, 824)      # what a Pixel 9 shows of the 840-row tall frame
PC_COLS = (160, 800)         # what a 16:9 PC shows of the 960-column wide frame


def _label(c, text, x, y, color, scale=2):
    w = sk.caption(c, text, x, y, color, scale, 1)
    return w


def main():
    import splash
    import splash_wide
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    gutter, head = 24, 64
    colw = PC_COLS[1] - PC_COLS[0]
    tall_h = PIXEL9_ROWS[1] - PIXEL9_ROWS[0]
    sheet = raster.Canvas(gutter + len(ORDER) * (colw + gutter),
                          head + 360 + gutter + tall_h + gutter + 40, M["stone"][4])
    for i, which in enumerate(ORDER):
        lay = LAYOUTS[which]
        wide = splash_wide.vigil_wide(lettering=which)
        tall = splash.vigil(lettering=which)[0]
        for c, nm in ((wide, "wide"), (tall, "tall")):
            print("%-6s %-5s off-contract colours: %d" % (which, nm, len(c.audit())))
            c.scaled(2).save(os.path.join(OUT, "%s-%s@2x.png" % (which, nm)))
        x0 = gutter + i * (colw + gutter)
        tag = lay["name"] + ("   SHIPS" if which == SHIPPING else "")
        _label(sheet, tag, x0 + colw // 2, 14, M["gold"][1] if which == SHIPPING else M["skin"][1])
        _label(sheet, lay["note"].upper(), x0 + colw // 2, 38, M["skin"][3], 1)
        sheet.img.paste(wide.img.crop((PC_COLS[0], 0, PC_COLS[1], 360)), (x0, head))
        sheet.img.paste(tall.img.crop((0, PIXEL9_ROWS[0], 360, PIXEL9_ROWS[1])),
                        (x0 + (colw - 360) // 2, head + 360 + gutter))
    sheet.px = sheet.img.load()
    _label(sheet, "PC 16:9 ABOVE, PIXEL 9 BELOW, BOTH AT 1X. ART ONLY: THE MENU AND THE PROMPT ARE GODOT'S.",
           sheet.w // 2, sheet.h - 26, M["skin"][3], 1)
    path = os.path.join(OUT, "comparison.png")
    sheet.save(path)
    sheet.scaled(2).save(os.path.join(OUT, "comparison@2x.png"))
    print("wrote %s" % path)


if __name__ == "__main__":
    main()
