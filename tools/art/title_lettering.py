# -*- coding: utf-8 -*-
"""The title's lettering: THE LAST WIZARD: SURVIVORS, in three layouts, one of which ships.

Run: python tools/art/title_lettering.py

It writes a comparison sheet to tools/art/_title_lettering/ (gitignored): each layout drawn into
the finished title art for both screens - the PC's 16:9 view of the wide set and the Pixel 9's
view of the tall one - so the three can be judged as a player sees them rather than as type on a
black ground. It never touches a shipping file; splash.py, splash_wide.py and anim.py do that, and
they draw whichever layout SHIPPING names.

Why the name changed the art (Eric, 2026-11-02: "The title screen art needs to change because the
title changed"). The game was renamed. It is now The One He Missed, from the premise's own
sentence (.ai/world-and-tone.md): a dark wizard took every wizard who could have stood against him,
and you are the one he missed. The blue-robed figure in the ward is that player - the one wizard
he did not take - and the ward's gold is the one warm light left in the wood. The lettering says
the same thing in the only way type can without a tagline: THE ONE is lit in the ward's gold, and
everything else is set in cold bone, the colour of the world he left.

Renamed again on 2026-11-08 (Eric: "on the game, the name should be 'The Last Wizard: Survivors'"),
for a name that says what the game is to someone scrolling a store. The premise reads the same:
the last wizard is the one he missed. So the rule carries over word for word - the phrase that
names the player, THE LAST, is lit in the ward's gold, and the rest is cold bone - and the genre
word SURVIVORS is the subtitle, set smallest. The colon is never set; its own line does its work.

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

# Widths at track 2, in source pixels before the scale: THE LAST 80, WIZARD 62, SURVIVORS 93,
# THE LAST WIZARD on one line 149. So at scales 3, 4 and 2 the stack's lines come to 240, 248 and
# 186 - the first two within eight pixels of each other, and SURVIVORS the first title's own 186.
LAYOUTS = {
    # SHIPS. Three lines: THE LAST in gold at scale 3, WIZARD in bone at 4, SURVIVORS in bone at 2.
    # The name proper is the justified pair on top and the genre word sits under it as a subtitle,
    # the way the first title's SURVIVORS sat under WIZARD. One layout on both screens. A third line
    # makes the block 124 rows on a PC, where the menu column starts at row 134 (MainMenu.PinToWideArt)
    # and the old two-line block ended at 116; so there the gaps close to 5 and the cap line rises
    # to 6, which leaves the box ending at about 126 - eight rows clear of Start Run.
    "stack": dict(
        name="A  STACK",
        note="THE LAST, WIZARD, SURVIVORS - scales 3, 4 and 2",
        tall=dict(cx=180, y=30, lines=[(3, [("THE LAST", GOLD)], 8), (4, [("WIZARD", BONE)], 9),
                                       (2, [("SURVIVORS", BONE)], 0)]),
        wide=dict(cx=175, y=6, lines=[(3, [("THE LAST", GOLD)], 5), (4, [("WIZARD", BONE)], 5),
                                      (2, [("SURVIVORS", BONE)], 0)])),
    # A poster: WIZARD set big, THE LAST lit and small over it, SURVIVORS small under it. The
    # strongest single shape, and the one a store thumbnail reads first - but THE LAST at scale 2
    # is a scrap over a word that big, which is what sank the same shape for THE ONE.
    "crown": dict(
        name="B  CROWN",
        note="THE LAST, WIZARD, SURVIVORS - scales 2, 5 and 2",
        tall=dict(cx=180, y=34, lines=[(2, [("THE LAST", GOLD)], 6), (5, [("WIZARD", BONE)], 8),
                                       (2, [("SURVIVORS", BONE)], 0)]),
        wide=dict(cx=175, y=8, lines=[(2, [("THE LAST", GOLD)], 5), (5, [("WIZARD", BONE)], 6),
                                      (2, [("SURVIVORS", BONE)], 0)])),
    # An inscription: the name on one line with SURVIVORS under it, so the scene carries the screen
    # and the name is read rather than looked at. A phone fits THE LAST WIZARD only at scale 2; on
    # a PC it runs at 3 across the clearing and over the staff's orb.
    "line": dict(
        name="C  LINE",
        note="THE LAST WIZARD on one line - scale 3 on a PC, 2 on a phone",
        tall=dict(cx=180, y=80, lines=[(2, [("THE", BONE), ("LAST", GOLD), ("WIZARD", BONE)], 7),
                                       (2, [("SURVIVORS", BONE)], 0)]),
        wide=dict(cx=320, y=14, lines=[(3, [("THE", BONE), ("LAST", GOLD), ("WIZARD", BONE)], 8),
                                       (2, [("SURVIVORS", BONE)], 0)])),
}
ORDER = ("stack", "crown", "line")
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
