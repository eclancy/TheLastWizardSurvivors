# -*- coding: utf-8 -*-
"""The Court's side event art: the seer's voice, where Nostradamus is kept (.ai/chapter-wizards.md 4.10).

Run: python tools/art/seers_voice.py

    world/seer/seer-lectern.png   where he was reading, and is not: a reading stand of the house's black oak
                                  with a heavy book lying open on its slope - a quatrain on the left page, a
                                  horoscope wheel on the right - and a candle in a brass dish on an arm from
                                  its side, STILL LIT; his quill dropped on the floor at its foot. 36x44,
                                  four frames of the flame (steady, leaning, guttering low, leaning back and
                                  tall); in the second the right page's corner lifts in the draught
    world/seer/seer-desk.png      his desk, the third room: a tall writing desk of the same oak, its front
                                  panelled in linenfold to the floor, and on its top a stack of three books
                                  with the candle burning on it, a book lying open, an inkwell and its quill,
                                  an hourglass, a brass armillary sphere on its stand, and a loose sheet of
                                  verses hanging over the front edge. 48x56, four frames of the same flame.
                                  He is NOT in it: his own sprite (characters/nostradamus-*.png) stands
                                  behind it, so the desk hides him from the waist down and is the same desk
                                  after he has gone
    ui/poi/poi-seer.png           the arrow's pictogram: a candle burning in an open book, a star over it
    tools/art/_seers_voice_preview.png   (gitignored) for looking at

"A court with no one left in it" (.ai/endgame-castle.md 5): every candle in the house is cold and unlit
(props_court.py, WHAT THE HOUSE HOLDS BACK). So one burning candle is the whole tell that someone was
here a moment ago, and it is what the player finds in each room the voice read from.

THE CANDLE IS WARM, AND IN THE LIGHT RAMP. world-and-tone.md: "every point of warm light is a piece of it
he does not have". This is not the house's light - the house's candles are cold - it is the seer's own,
the light he is kept for, burning where the dark wizard has no light at all; and Light is his element
(Starfall). So the flame is the light ramp, white at its root, and the near page and the brass under it
catch it in bone's and gold's lit stops. It is pigment-lit, not a glow: SeersVoiceEvent puts the glow on.
Never the fire ramp - built things burn (the Emberdeep), and this is not a built thing burning.

NOTHING HERE IS A CANDELABRUM. A standing iron candlestand on the floor would be the Court's Turret
(the Candelabrum, 5.7) to a player who has learned to read the cast - so the candle stands on the
furniture, in a dish on the lectern's arm and on the desk's books, never on a stand of its own.

Every sheet is authored 1:1 and drawn by the game at x2 (Sprite2D Scale 2), sorted with the bodies at its
foot. The desk's foot row is its front edge: he stands DESK_HIM_BEHIND rows behind it (north), so the desk
sorts in front of him and its top hides him from DESK_TOP_BACK rows above its foot down.
"""
from __future__ import division, print_function

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image

import bonelight as bl
import raster

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "seer")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
PREVIEW = os.path.join(HERE, "_seers_voice_preview.png")
SHOTS = os.path.join(os.path.dirname(ROOT), "_shots", "wizard-nostradamus")
HIM = os.path.join(ROOT, "assets", "bonelight", "characters", "nostradamus-1.png")
TILES = os.path.join(ROOT, "assets", "bonelight", "tiles")


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


GD = ramp("gold")     # the house's black oak is gold's dark end (props_court.py); brass is its lit end
SK = ramp("skin")     # bone: paper, vellum, wax
RD = ramp("red")      # old red leather, and the rubric's red ink
VI = ramp("violet")
STL = ramp("steel")
LIGHT = bl.ELEMENTS["light"]   # core, hot, mid, edge: his candle
FRAMES = 4

# --- the flame -------------------------------------------------------------------------------------
#
# Four shapes of one small flame, five wide and six tall over its wick, keyed: e edge, m mid, h hot,
# c core. Steady; leaning left in a draught; guttering low; leaning back and standing tall. The wick is
# the row under the last.

FLAMES = [
    ["..e..",
     "..m..",
     ".mhm.",
     ".hch.",
     ".hch.",
     "..h.."],
    [".e...",
     ".m...",
     ".mh..",
     ".hcm.",
     "..ch.",
     "..h.."],
    [".....",
     ".....",
     "..e..",
     ".mhm.",
     ".hch.",
     "..h.."],
    ["...e.",
     "..mm.",
     "..hm.",
     ".mch.",
     ".hch.",
     "..h.."],
]
FLAME_TONE = {"e": LIGHT[3], "m": LIGHT[2], "h": LIGHT[1], "c": LIGHT[0]}


def flame(c, wick_x, wick_y, frame):
    """The flame standing on the wick at (wick_x, wick_y); the wick itself a pixel of black."""
    rows = FLAMES[frame % FRAMES]
    top = wick_y - len(rows)
    for r, row in enumerate(rows):
        for k, ch in enumerate(row):
            if ch != ".":
                c.set(wick_x - 2 + k, top + r, FLAME_TONE[ch])
    c.set(wick_x, wick_y, OCC)


def candle(c, x, top, bottom, frame, melt=0):
    """A wax candle three wide from row `top` (its rim) to `bottom`, lit from its own flame: the near
    side bone's hi, the far side in shade, a rim melted down a pixel and a drip or two run down it."""
    for y in range(top, bottom + 1):
        c.set(x - 1, y, SK[0])
        c.set(x, y, SK[1])
        c.set(x + 1, y, SK[2])
    c.set(x, top, SK[2])                 # the well the flame has melted in the top
    c.set(x - 1, top + 2 + melt, SK[0])  # a run of wax down the lit side
    c.set(x + 1, top + 1, SK[1])
    c.set(x + 1, top + 3, SK[1])
    flame(c, x, top - 1, frame)


# --- the lectern ---------------------------------------------------------------------------------

LECTERN_W, LECTERN_H = 36, 44
LECTERN_FOOT = 41      # the row its base stands on
LECTERN_MIDDLE = 18    # the post's middle column
LECTERN_FLAME = (3, 5)  # the top of the candle's flame (frame 0), for the glow


def lectern(frame):
    c = raster.Canvas(LECTERN_W, LECTERN_H)
    F = LECTERN_FOOT
    # Contact: the base's shadow on the floor, south and east of it.
    c.disc(19.5, F + 1.0, 11.0, 1.6, OCC)
    # The base: a cross foot of the house's oak - its two arms seen end on and across - and a plinth on it.
    c.rect(9, F - 1, 27, F, GD[4])
    c.hline(9, 27, F - 1, GD[3])
    c.hline(9, 27, F, OCC)
    for x in (9, 10):
        c.set(x, F - 2, GD[3])           # the arms' ends, carved up into a scroll
    for x in (26, 27):
        c.set(x, F - 2, GD[4])
    c.set(27, F - 2, OCC)
    c.rect(13, F - 4, 23, F - 2, GD[4])
    c.hline(13, 23, F - 4, GD[2])
    c.vline(13, F - 4, F - 2, GD[3])
    c.vline(23, F - 4, F - 2, OCC)
    # The post, turned: four wide, lit on the candle's side, a knop and a collar on it.
    for y in range(27, F - 4):
        c.set(16, y, GD[2] if y < 31 else GD[3])
        c.set(17, y, GD[3])
        c.set(18, y, GD[4])
        c.set(19, y, OCC)
    for y in (32, 33):
        c.hline(15, 20, y, GD[4])
        c.set(15, y, GD[2] if y == 32 else GD[3])
        c.set(16, y, GD[3])
        c.set(20, y, OCC)
    c.hline(15, 20, 28, GD[3])
    c.set(15, 28, GD[2])
    c.set(20, 28, OCC)
    # The slope: a board leaning toward whoever reads, a ledge along its foot to hold the book.
    c.rect(8, 12, 29, 25, GD[4])
    c.hline(8, 29, 12, GD[3])
    c.vline(8, 12, 25, GD[3])
    c.vline(29, 12, 25, OCC)
    c.hline(7, 30, 26, GD[2])            # the ledge, caught by the candle
    c.hline(7, 30, 27, OCC)
    c.set(30, 26, GD[3])
    # The book, open on it: red leather boards a pixel proud of the pages all round.
    c.rect(9, 11, 28, 25, RD[3])
    c.hline(9, 28, 11, RD[2])
    c.vline(9, 11, 25, RD[2])
    c.hline(10, 27, 25, RD[4])
    # The page block's thickness at the foot: the leaves' edges, a stop down.
    for x in range(10, 28):
        c.set(x, 24, SK[3] if x % 2 else SK[2])
    # The pages: the near page (left, under the candle) in bone's lit stop, the far one a stop down,
    # bowed up from the gutter to the outer corners.
    c.rect(10, 13, 18, 23, SK[1])
    c.rect(19, 13, 27, 23, SK[2])
    for x in (10, 11, 12):
        c.set(x, 12, SK[1])
    for x in (25, 26, 27):
        c.set(x, 12, SK[2])
    c.set(10, 13, SK[0])
    c.set(11, 13, SK[0])
    c.vline(18, 13, 23, SK[2])           # the gutter, in the fold's shade
    c.vline(19, 13, 23, SK[3])
    # The left page: a quatrain, four lines of a hand in faded ink, the first word a red capital.
    c.rect(11, 15, 12, 16, RD[1])
    for y, (a, b) in zip((15, 17, 19, 21), ((14, 17), (11, 16), (11, 17), (11, 15))):
        for x in range(a, b + 1):
            if (x * 7 + y * 3) % 5 != 0:
                c.set(x, y, SK[3])
    # The right page: a horoscope's wheel - a ring cut in four - and two lines under it.
    c.set(22, 14, SK[3])
    c.set(23, 14, SK[3])
    c.set(21, 15, SK[3])
    c.set(24, 15, SK[3])
    c.set(21, 16, SK[3])
    c.set(24, 16, SK[3])
    c.set(22, 17, SK[3])
    c.set(23, 17, SK[3])
    c.vline(22, 15, 16, SK[4])
    c.set(23, 16, RD[1])                 # a planet marked in red
    for y, (a, b) in zip((19, 21), ((20, 26), (20, 24))):
        for x in range(a, b + 1):
            if (x * 5 + y) % 4 != 0:
                c.set(x, y, SK[4])
    # The marker ribbon, out from under the far page and down over the ledge, its tail cut in a fork.
    for y in range(24, 30):
        c.set(23, y, RD[1] if y < 27 else RD[2])
    c.set(22, 30, RD[2])
    c.set(24, 30, RD[2])
    # Frame 1: the far page's corner lifts in the draught that moves the flame.
    if frame == 1:
        c.set(27, 13, SK[3])
        c.set(26, 11, SK[1])
        c.set(27, 11, SK[1])
        c.set(27, 10, SK[0])
        c.set(26, 12, SK[1])
    # The candle: a brass arm out of the board's side, bracketed under, a dish on it, the candle in the dish.
    c.hline(4, 7, 21, GD[1])
    c.hline(4, 7, 22, GD[2])
    c.hline(5, 7, 23, OCC)
    c.line(5, 23, 7, 25, GD[3])          # the bracket under the arm, back to the board
    c.rect(1, 19, 6, 20, GD[2])
    c.hline(1, 6, 19, GD[1])
    c.set(1, 20, GD[1])
    c.hline(2, 6, 21, OCC)
    c.set(1, 21, OCC)
    c.set(5, 18, SK[1])                  # wax spilled over the dish's lip
    c.set(5, 19, SK[0])
    c.set(5, 20, SK[1])
    candle(c, 3, 12, 18, frame)
    # His quill, dropped on the boards at the stand's foot.
    c.line(26, LECTERN_FOOT - 1, 32, LECTERN_FOOT - 4, SK[1])
    c.set(32, LECTERN_FOOT - 4, SK[0])
    c.set(31, LECTERN_FOOT - 4, SK[0])
    c.set(31, LECTERN_FOOT - 3, SK[2])
    c.set(30, LECTERN_FOOT - 3, SK[1])
    c.set(29, LECTERN_FOOT - 2, SK[2])
    c.set(26, LECTERN_FOOT, OCC)
    c.hline(27, 31, LECTERN_FOOT, OCC)
    return c


# --- the desk ------------------------------------------------------------------------------------

DESK_W, DESK_H = 48, 56
DESK_FOOT = 52         # the row its front stands on
DESK_MIDDLE = 24
DESK_TOP_FRONT = 35    # the front edge of its top (17 rows above the foot)
DESK_TOP_BACK = 29     # the back edge of its top (23 rows above the foot)
# Where he stands: his feet this many rows behind (north of) the desk's foot, on its middle column -
# then his waist (about 18 rows over his feet, on row 46 of the 48 cell) is three rows over the top's back edge, and the
# desk sorts in front of him.
DESK_HIM_BEHIND = 8
DESK_FLAME = (39, 9)   # the top of the candle's flame on the books (frame 0)


def desk(frame):
    """His desk. He stands behind it with his armillary sphere raised on the left (roster.py), so what
    stands up off the top keeps to the ends and low in the middle: the brass bowl on its tripod at the
    left end, under his raised hand; the open book and the sheet of verses in the middle, flat; the
    books with the candle on them at the right end, clear of his head."""
    c = raster.Canvas(DESK_W, DESK_H)
    F = DESK_FOOT
    T = DESK_TOP_FRONT
    B = DESK_TOP_BACK
    x0, x1 = 3, 44
    # Contact along its whole front, and its shadow thrown south-east.
    c.rect(x0 + 2, F, x1 + 2, F + 1, OCC)
    c.vline(x1 + 1, T + 2, F, OCC)
    # The front: one face of black oak to the floor, so nothing of him shows below the top - two fielded
    # panels either side of a pilaster with his star cut in it, and a plinth along the foot.
    c.rect(x0, T + 1, x1, F, GD[4])
    c.vline(x0, T + 1, F, GD[3])
    c.vline(x1, T + 1, F, OCC)
    c.rect(x0, F - 2, x1, F, GD[4])
    c.hline(x0, x1, F - 2, GD[2])
    c.vline(x0, F - 2, F, GD[3])
    c.hline(x0, x1, F, OCC)
    for (p0, p1) in ((x0 + 3, 20), (27, x1 - 3)):
        py0, py1 = T + 4, F - 5
        # The sunk frame round the panel: shade on its top and left, caught light on its foot and right.
        c.hline(p0, p1, py0, OCC)
        c.vline(p0, py0, py1, OCC)
        c.hline(p0 + 1, p1, py1, GD[3])
        c.vline(p1, py0 + 1, py1, GD[3])
        # The raised field: lit on its top and left bevels, shaded under and right.
        f0, f1, g0, g1 = p0 + 2, p1 - 2, py0 + 2, py1 - 2
        c.rect(f0, g0, f1, g1, GD[4])
        c.hline(f0, f1, g0, GD[2])
        c.vline(f0, g0, g1, GD[3])
        c.hline(f0, f1, g1, OCC)
        c.vline(f1, g0, g1, OCC)
        # A lozenge cut in the field's middle.
        mx, my = (f0 + f1) // 2, (g0 + g1) // 2
        for d in range(0, 3):
            c.set(mx - 2 + d, my - d, GD[3])
            c.set(mx + 2 - d, my + d, OCC)
            c.set(mx - 2 + d, my + d, GD[3])
            c.set(mx + 2 - d, my - d, OCC)
    # The pilaster between them, and on it his eight-pointed star.
    px0, px1 = 22, 25
    c.vline(px0, T + 2, F - 3, GD[3])
    c.vline(px1, T + 2, F - 3, OCC)
    sx, sy = 23, (T + F) // 2
    for (ox, oy) in ((0, -2), (0, 2), (-2, 0), (2, 0)):
        c.set(sx + ox, sy + oy, GD[2])
    for (ox, oy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        c.set(sx + ox, sy + oy, GD[3])
    c.set(sx, sy, GD[1])
    # The top: its moulded front edge, which the candle catches, and the board going back - the writing
    # surface seen from above, lit warm toward the candle at the right end.
    c.hline(x0 - 1, x1 + 1, T, GD[2])
    c.hline(x0 - 1, x1 + 1, T + 1, GD[3])
    c.set(x1 + 1, T + 1, OCC)
    c.set(x0 - 1, T + 1, GD[2])
    c.rect(x0, B, x1, T - 1, GD[3])
    c.hline(x0, x1, B, GD[4])
    for x in range(x0 + 2, x1, 6):
        c.set(x, B + 2, GD[4])
        c.set(x + 3, B + 4, GD[4])
    for x in range(30, x1 + 1):
        c.set(x, T - 1, GD[2])
    # The open book lying flat in the middle - what he reads when he is here.
    bx0, bx1, by0, by1 = 16, 29, B + 1, T - 1
    gx = (bx0 + bx1) // 2
    c.rect(bx0, by0, bx1, by1, RD[3])
    c.hline(bx0, bx1, by1, RD[4])
    c.rect(bx0 + 1, by0, gx, by1 - 1, SK[2])
    c.rect(gx + 1, by0, bx1 - 1, by1 - 1, SK[1])
    c.vline(gx, by0, by1 - 1, SK[3])
    for y in range(by0 + 1, by1, 2):
        c.hline(bx0 + 2, gx - 1, y, SK[3])
        c.hline(gx + 2, bx1 - 2, y, SK[3])
    c.set(bx0 + 2, by0 + 1, RD[1])
    # A loose sheet of verses hanging over the front edge, two quatrains on it.
    sx0, sx1 = 25, 31
    c.rect(sx0, T - 2, sx1, T + 6, SK[1])
    c.vline(sx1, T - 2, T + 6, SK[2])
    c.hline(sx0, sx1, T + 7, SK[3])
    c.set(sx0, T + 7, SK[2])
    c.hline(sx0 + 1, sx1 + 1, T + 8, OCC)
    c.vline(sx1 + 1, T + 1, T + 7, OCC)
    for y in (T, T + 2, T + 4):
        for x in range(sx0 + 1, sx1):
            if (x + y) % 3:
                c.set(x, y, SK[3])
    c.hline(sx0, sx1, T - 1, SK[0])
    # The bowl at the left end: a brass basin of water on a low tripod - what he looked into at night.
    wx = 8
    # Three splayed legs, the near two seen either side and the back one between them, each on a foot.
    c.line(wx - 2, T - 6, wx - 4, T - 2, GD[2])
    c.line(wx + 2, T - 6, wx + 4, T - 2, GD[3])
    c.vline(wx, T - 6, T - 3, GD[4])
    for fx in (wx - 4, wx + 4):
        c.set(fx, T - 1, OCC)
    c.set(wx, T - 2, OCC)
    # The basin, a round bowl seen from above and in front: the lit lip, the water inside it, the belly
    # narrowing under it.
    c.hline(wx - 3, wx + 3, T - 11, GD[1])
    c.set(wx - 4, T - 10, GD[1])
    c.set(wx + 4, T - 10, GD[3])
    c.hline(wx - 3, wx + 3, T - 10, STL[3])         # the water, dark, one glint of the candle on it
    c.set(wx + 1, T - 10, SK[0])
    c.set(wx + 2, T - 10, STL[2])
    c.hline(wx - 4, wx + 4, T - 9, GD[2])           # the front of the lip
    c.set(wx + 4, T - 9, GD[3])
    c.hline(wx - 3, wx + 3, T - 8, GD[3])
    c.set(wx - 3, T - 8, GD[2])
    c.hline(wx - 2, wx + 2, T - 7, GD[4])
    c.set(wx + 3, T - 8, GD[4])
    # The hourglass beside the bowl, the sand run half through.
    hx = 15
    hy0, hy1 = T - 8, T - 1
    c.hline(hx - 2, hx + 2, hy0, GD[3])
    c.hline(hx - 2, hx + 2, hy1, GD[3])
    c.set(hx - 2, hy0, GD[2])
    for y in range(hy0 + 1, hy1):
        c.set(hx - 2, y, GD[4])
        c.set(hx + 2, y, GD[4])
        neck = y == (hy0 + hy1) // 2
        c.set(hx, y, STL[1] if not neck else GD[1])
        if not neck:
            c.set(hx - 1, y, STL[2])
            c.set(hx + 1, y, STL[1])
    for x in (hx - 1, hx, hx + 1):
        c.set(x, hy1 - 1, GD[1])                    # the sand run down
        c.set(x, hy0 + 2, GD[1])                    # what is left above
    c.set(hx, hy1 - 2, GD[1])
    c.hline(hx - 2, hx + 3, hy1 + 1, OCC)
    # The inkwell and its quill, leaning back toward the book.
    c.rect(32, T - 4, 34, T - 2, STL[3])
    c.hline(32, 34, T - 4, STL[2])
    c.set(33, T - 4, OCC)
    c.hline(32, 35, T - 1, OCC)
    c.line(33, T - 5, 30, T - 11, SK[1])
    c.set(30, T - 11, SK[0])
    c.set(29, T - 12, SK[0])
    c.set(31, T - 10, SK[0])
    c.set(30, T - 9, SK[2])
    # The stack of three books at the right end, the candle burning on top of it.
    books = [(35, 44, T - 4, T - 1, RD[3], RD[2]),
             (36, 43, T - 7, T - 5, VI[3], VI[2]),
             (35, 42, T - 10, T - 8, GD[3], GD[2])]
    for (ax, bx, ay, by, body, lit) in books:
        c.rect(ax, ay, bx, by, body)
        c.hline(ax, bx, ay, lit)
        c.vline(ax, ay, by, lit)
        c.vline(bx, ay + 1, by, SK[2])               # the page edges
        c.set(bx, ay, lit)
    c.hline(35, 45, T, OCC)
    c.rect(37, T - 12, 41, T - 11, GD[2])            # the candle's dish on the books
    c.hline(37, 41, T - 12, GD[1])
    c.set(41, T - 11, GD[3])
    c.set(40, T - 13, SK[1])                         # wax run over its lip
    candle(c, 39, T - 19, T - 13, frame, melt=1)
    return c


# --- the arrow's pictogram -----------------------------------------------------------------------

GLYPH = [
    "............",
    "....f....s..",
    "...fhf..sSs.",
    "...hch...s..",
    "....h.......",
    "....w.......",
    "...wwW......",
    ".pp.wW.qq...",
    "pppPwWqqQq..",
    "pPPPpqQQQq..",
    "bbbbbbbbbbb.",
    "............",
]
GLYPH_PAL = {"f": LIGHT[2], "h": LIGHT[1], "c": LIGHT[0], "s": LIGHT[2], "S": LIGHT[0],
             "w": SK[1], "W": SK[2], "p": SK[1], "P": SK[3], "q": SK[2], "Q": SK[3], "b": RD[2]}


def glyph():
    c = raster.Canvas(12, 12)
    for y, row in enumerate(GLYPH):
        for x, ch in enumerate(row):
            if ch != ".":
                c.set(x, y, GLYPH_PAL[ch])
    for y in range(12):
        for x in range(12):
            if c.get(x, y)[3] == 0 and any(GLYPH[yy][xx] != "." for yy in range(max(0, y - 1), min(12, y + 2))
                                           for xx in range(max(0, x - 1), min(12, x + 2))):
                c.set(x, y, OCC)
    return c


# --- output --------------------------------------------------------------------------------------

def _audit(name, c):
    bad = c.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in %s:" % name)
        for h, cnt in bad[:6]:
            print("   %s  x%d" % (h, cnt))
        return False
    return True


def _strip(canvases):
    w, h = canvases[0].w, canvases[0].h
    img = Image.new("RGBA", (w * len(canvases), h), (0, 0, 0, 0))
    for i, c in enumerate(canvases):
        img.alpha_composite(c.img, (i * w, 0))
    return img


def _floor(w, h, zoom):
    """The Court's oak boards and its marble side by side, at the preview's zoom, for a ground."""
    img = Image.new("RGBA", (w, h), raster.rgb(GD[4]))
    for name, x_from in (("court_boards_fill.png", 0), ("court_marble_fill.png", w // 2)):
        path = os.path.join(TILES, name)
        if not os.path.exists(path):
            continue
        tile = Image.open(path).convert("RGBA")
        t = tile.resize((tile.size[0] * zoom // 2, tile.size[1] * zoom // 2), Image.NEAREST)
        for y in range(0, h, t.size[1]):
            for x in range(x_from, w if x_from else w // 2, t.size[0]):
                img.alpha_composite(t.crop((0, 0, min(t.size[0], (w if x_from else w // 2) - x), min(t.size[1], h - y))), (x, y))
    return img


def _stand_in():
    """A silhouette of a figure on the 48 cell, feet on row 45 and waist on row 28, while his sprite is
    still being drawn."""
    c = raster.Canvas(48, 48)
    c.disc(24, 8, 4, 4.5, SK[2])
    c.rect(18, 13, 30, 45, VI[3])
    c.rect(16, 15, 32, 26, VI[3])
    c.hline(18, 30, 28, VI[1])
    return c.img


def main():
    for d in (OUT_WORLD, OUT_UI):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True
    lecterns = [lectern(f) for f in range(FRAMES)]
    desks = [desk(f) for f in range(FRAMES)]
    for f in range(FRAMES):
        ok = _audit("lectern %d" % f, lecterns[f]) and ok
        ok = _audit("desk %d" % f, desks[f]) and ok
    _strip(lecterns).save(os.path.join(OUT_WORLD, "seer-lectern.png"))
    _strip(desks).save(os.path.join(OUT_WORLD, "seer-desk.png"))
    g = glyph()
    ok = _audit("glyph seer", g) and ok
    g.scaled(2).save(os.path.join(OUT_UI, "poi-seer.png"))

    # The preview at x4 (x2 of the world) on the Court's boards and marble: the lectern, the desk with him
    # standing behind it (his feet DESK_HIM_BEHIND rows north of its foot), the desk empty, then each sheet's
    # four frames and the pictogram large.
    z = 4
    W, H = 900, 520
    sheet = _floor(W, H, z)
    him = Image.open(HIM).convert("RGBA") if os.path.exists(HIM) else _stand_in()
    if him.size[0] > 48:
        him = him.crop((0, 0, 48, 48))
    foot_y = 300
    lx = 40
    sheet.alpha_composite(lecterns[0].scaled(z), (lx, foot_y - LECTERN_FOOT * z))
    dx = 240
    him_feet_row = 46   # a wizard sheet stands on row 46 of its 48 cell (WalkingHutEvent)
    sheet.alpha_composite(him.resize((48 * z, 48 * z), Image.NEAREST),
                          (dx + (DESK_MIDDLE - 24) * z, foot_y - (DESK_HIM_BEHIND + him_feet_row) * z))
    sheet.alpha_composite(desks[0].scaled(z), (dx, foot_y - DESK_FOOT * z))
    sheet.alpha_composite(desks[2].scaled(z), (dx + 220, foot_y - DESK_FOOT * z))
    y2 = foot_y + 30
    strip_l = _strip(lecterns)
    sheet.alpha_composite(strip_l.resize((strip_l.size[0] * 2, strip_l.size[1] * 2), Image.NEAREST), (20, y2))
    strip_d = _strip(desks)
    sheet.alpha_composite(strip_d.resize((strip_d.size[0] * 2, strip_d.size[1] * 2), Image.NEAREST), (330, y2 - 10))
    sheet.alpha_composite(g.scaled(8), (W - 110, 30))
    sheet.convert("RGB").save(PREVIEW)
    if os.path.isdir(SHOTS):
        sheet.convert("RGB").save(os.path.join(SHOTS, "seer_art.png"))

    print("  seer lectern %dx%d x%d, desk %dx%d x%d, glyph poi-seer" % (LECTERN_W, LECTERN_H, FRAMES, DESK_W, DESK_H, FRAMES))
    if not ok:
        return 1
    print("seer's voice written, palette clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
