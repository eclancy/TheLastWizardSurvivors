# -*- coding: utf-8 -*-
"""The Scorched Sands' second side event art: the dry tomb and its three beacons, where Imhotep is sealed
(.ai/chapter-wizards.md 4.6).

Run: python tools/art/tomb_beacons.py

    world/tomb/tomb.png         the tomb, 88x72 a frame, four frames: sealed (his mark on the slab), the slab a
                                third aside, two thirds, and open on the stair going down into the dark
    world/tomb/beacon.png       a signal beacon, 28x52 a frame, eight frames: cold, kindling (embers), burning
                                (four flickers), and guttering (two) as the dry wind gets into it
    ui/poi/poi-tomb.png         the arrow's pictogram: a beacon burning on its pillar
    tools/art/_tomb_beacons_preview.png   (gitignored) for looking at

"He hid the water." The builder who could find water in the desert and raise it is sealed in a dry tomb beside
the road the water left. Three signal beacons stand on the dunes round it, about 700 px apart, cold. Light all
three at once and the slab slides aside (TombBeaconsEvent).

THE TOMB IS BUILT, THE SEAL IS HIS. A mastaba front of sandstone courses - gold's dark end, the colour of the
dunes' stone in this drained light, never the Sands' sand cloth (cast_sands.py), which is what his chapter's dead
are wrapped in - a cornice over the door, sand drifted against both corners. The door is one slab of dark
granite (stone's row), and on it his mark: the one cold light, in the arcane ramp, where the far tell stands up
from. When the slab slides into the wall, the mark goes with it.

THE BEACONS ARE THE WIZARDS' LIGHT. A pillar of the same granite, a bronze fire-basket on it. Cold, the basket
holds charred wood and nothing else. Lit, it burns in fire's ramp - warm light, which in this world belongs to
what he does not have - and as the wind gets into it the flames sink and gutter before they go out.

NOT THE MOOT. The Broken Circle's stones are a ring of nine lit one at a time in his order against a clock;
these are three fires on posts, any order, each its own clock, and what matters is that they burn together.

Every sheet is authored 1:1 and drawn by the game at x2 (Sprite2D Scale 2); the tomb and the beacons sort with the
bodies at their feet. Python 2.7 and PIL, as every generator here.
"""
from __future__ import division, print_function

import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image

import bonelight as bl
import raster

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "tomb")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
PREVIEW = os.path.join(HERE, "_tomb_beacons_preview.png")


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


ST = ramp("stone")
GD = ramp("gold")
FIRE = bl.ELEMENTS["fire"]       # core, hot, mid, edge
ARC = bl.ELEMENTS["arcane"]      # his mark on the slab


def outline(c, skip_down=False):
    """A one-pixel occlusion edge round everything drawn, eight ways (four with skip_down: a thing standing on the
    ground has its contact shadow drawn separately)."""
    w, h = c.w, c.h
    occ = raster.rgb(OCC)
    solid = [[c.get(x, y)[3] != 0 for x in range(w)] for y in range(h)]
    for y in range(h):
        for x in range(w):
            if solid[y][x]:
                continue
            for dx in (-1, 0, 1):
                for dy in ((-1, 0) if skip_down else (-1, 0, 1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and solid[ny][nx] and c.get(nx, ny) != occ:
                        c.set(x, y, OCC)
                        break
                else:
                    continue
                break


# --- the tomb ----------------------------------------------------------------------------------------

TOMB_W, TOMB_H = 88, 72
TOMB_FOOT = 64          # the row the front's foot stands on: the sprite's sort point
TOMB_MIDDLE = 44
DOOR_X0, DOOR_X1 = 37, 51
DOOR_Y0 = 38
SLAB_TRAVEL = 15        # how far the slab slides into the wall when open


def tomb(frame):
    """The tomb's front, seen from a little above: the flat roof, the battered front of sandstone courses with
    the joints staggered, a cornice over the door, the door's slab of granite with his mark, sand drifted against
    both corners. frame 0 sealed, 1 and 2 the slab sliding into the right-hand wall, 3 open on the stair."""
    c = raster.Canvas(TOMB_W, TOMB_H)
    rng = random.Random(61)
    # THE ROOF: the flat top, in the key light, laid in big slabs.
    for y in range(10, 24):
        inset = (23 - y) * 0.35
        x0, x1 = int(round(9 + inset)), int(round(79 - inset))
        for x in range(x0, x1 + 1):
            c.set(x, y, GD[2])
        c.set(x0, y, GD[1])
        c.set(x1, y, GD[3])
    c.hline(13, 75, 10, GD[1])
    for x in range(13, 76, 3):
        c.set(x, 10, GD[2])                                 # the roof's edge worn, not a gilded line
    for (jx, y0, y1) in ((24, 11, 23), (41, 11, 23), (58, 11, 23), (32, 11, 16), (66, 17, 23)):
        for y in range(y0, y1 + 1):
            c.set(jx + (y - 11) // 6, y, GD[3])
    for y in (16, 17):
        c.hline(14, 74, y, GD[3] if y == 16 else GD[2])
    for _ in range(26):                                     # grit blown up onto it
        c.set(rng.randint(12, 76), rng.randint(11, 22), GD[1] if rng.random() < 0.6 else GD[3])
    # THE FRONT: battered, in shade (the key is from above and the left), courses of blocks.
    for y in range(24, TOMB_FOOT + 1):
        batter = (TOMB_FOOT - y) * 0.08
        x0, x1 = int(round(7 + batter)), int(round(81 - batter))
        for x in range(x0, x1 + 1):
            c.set(x, y, GD[3])
        c.set(x0, y, GD[2])
        c.set(x1, y, GD[4])
    c.hline(8, 80, 24, GD[1])                               # the roof's lip catching the light
    c.hline(8, 80, 25, GD[4])
    course = 0
    for y in range(31, TOMB_FOOT, 6):
        c.hline(9, 79, y, GD[4])
        c.hline(9, 79, y - 1, GD[3])
        course += 1
    for (cy0, offset) in ((26, 0), (31, 7), (37, 2), (43, 9), (49, 4), (55, 11), (61, 6)):
        x = 10 + offset
        k = 0
        while x < 79:
            for y in range(cy0, min(TOMB_FOOT, cy0 + 5)):
                c.set(x, y, GD[4])
            c.set(x + 1, cy0, GD[2])                        # each block's top edge lit beside its joint
            x += 11 + (k * 5 + offset) % 7
            k += 1
    for _ in range(40):                                     # weathering: pits and lighter grains
        x, y = rng.randint(10, 78), rng.randint(27, TOMB_FOOT - 1)
        if c.get(x, y) == raster.rgb(GD[3]):
            c.set(x, y, GD[4] if rng.random() < 0.55 else GD[2])
    # THE DOOR'S FRAME: a deep reveal, a lintel and the cavetto cornice over it.
    for y in range(DOOR_Y0 - 9, TOMB_FOOT + 1):
        for x in range(DOOR_X0 - 4, DOOR_X1 + 5):
            c.set(x, y, GD[3])
    for x in range(DOOR_X0 - 6, DOOR_X1 + 7):                # the cornice: a lit lip, a shaded curve under it
        c.set(x, DOOR_Y0 - 11, GD[1])
        c.set(x, DOOR_Y0 - 10, GD[2])
        c.set(x, DOOR_Y0 - 9, GD[3])
        c.set(x, DOOR_Y0 - 8, GD[4])
    c.set(DOOR_X0 - 6, DOOR_Y0 - 11, GD[0])
    for x in range(DOOR_X0 - 4, DOOR_X1 + 5):                # the torus moulding under the cornice
        c.set(x, DOOR_Y0 - 6, GD[1] if x % 2 else GD[2])
    for x in range(DOOR_X0 - 3, DOOR_X1 + 4):                # the lintel
        for y in range(DOOR_Y0 - 5, DOOR_Y0 - 1):
            c.set(x, y, GD[2])
        c.set(x, DOOR_Y0 - 5, GD[1])
        c.set(x, DOOR_Y0 - 1, GD[4])
    for (gx, gy) in ((DOOR_X0 + 1, DOOR_Y0 - 3), (DOOR_X0 + 4, DOOR_Y0 - 4), (DOOR_X0 + 7, DOOR_Y0 - 3),
                     (DOOR_X1 - 6, DOOR_Y0 - 3), (DOOR_X1 - 2, DOOR_Y0 - 4), (DOOR_X1 + 1, DOOR_Y0 - 3)):
        c.set(gx, gy, GD[3])                                 # cut signs on the lintel, worn
    for y in range(DOOR_Y0, TOMB_FOOT + 1):                   # the jambs, a lit edge on the left one
        c.set(DOOR_X0 - 1, y, GD[1])
        c.set(DOOR_X0 - 2, y, GD[2])
        c.set(DOOR_X1 + 1, y, GD[4])
        c.set(DOOR_X1 + 2, y, GD[3])
    # BEHIND THE SLAB: the stair going down into the dark - steps in stone's shade, darker the further down.
    for y in range(DOOR_Y0, TOMB_FOOT + 1):
        for x in range(DOOR_X0, DOOR_X1 + 1):
            c.set(x, y, OCC)
    for k, y in enumerate(range(TOMB_FOOT - 1, DOOR_Y0 + 6, -4)):
        tone = ST[2] if k == 0 else (ST[3] if k < 3 else ST[4])
        for x in range(DOOR_X0, DOOR_X1 + 1):
            c.set(x, y, tone)
        c.set(DOOR_X0, y, ST[1] if k < 2 else ST[3])
    # THE SLAB, sliding into the right-hand wall: granite, a lit left edge, chisel marks, his mark in the middle.
    shift = [0, 5, 10, SLAB_TRAVEL][frame]
    sx0, sx1 = DOOR_X0 + shift, DOOR_X1 + shift
    for y in range(DOOR_Y0, TOMB_FOOT + 1):
        for x in range(sx0, min(sx1, DOOR_X1 + 1 + (0 if frame < 3 else -99)) + 1):
            if x > DOOR_X1:
                break
            c.set(x, y, ST[2])
        if sx0 <= DOOR_X1:
            c.set(sx0, y, ST[1])
    if sx0 <= DOOR_X1:
        c.hline(sx0, DOOR_X1, DOOR_Y0, ST[1])
        for (mx, my) in ((3, 6), (9, 4), (5, 14), (11, 19), (2, 22), (8, 11)):
            x = sx0 + mx
            if x <= DOOR_X1:
                c.set(x, DOOR_Y0 + my, ST[3])
        # His mark: a ring with a stroke down through it, in his cold light. Only while the slab still covers the
        # door's middle; once it has slid past, the mark is inside the wall.
        mx, my = sx0 + 7, DOOR_Y0 + 11
        if mx + 2 <= DOOR_X1:
            for (dx, dy, col) in ((-1, -2, ARC[2]), (0, -2, ARC[1]), (1, -2, ARC[2]), (-2, -1, ARC[2]), (2, -1, ARC[3]),
                                  (-2, 0, ARC[2]), (2, 0, ARC[3]), (-1, 1, ARC[3]), (1, 1, ARC[3]), (0, 1, ARC[2]),
                                  (0, 0, ARC[0] if frame == 0 else ARC[1]), (0, -4, ARC[3]), (0, -3, ARC[2]),
                                  (0, 2, ARC[2]), (0, 3, ARC[3]), (0, 4, ARC[3])):
                if mx + dx <= DOOR_X1:
                    c.set(mx + dx, my + dy, col)
    # A shadow line where the slab meets the jamb it slides into.
    for y in range(DOOR_Y0, TOMB_FOOT + 1):
        c.set(DOOR_X1 + 1, y, GD[4])
    # SAND drifted against both corners and along the foot: lit on top, grains in it.
    for (cx, width, height) in ((10, 18, 12), (78, 16, 10), (30, 10, 4), (60, 12, 5)):
        for x in range(cx - width, cx + width + 1):
            t = 1.0 - abs(x - cx) / float(width)
            top = TOMB_FOOT + 1 - int(round(height * t * t * (3 - 2 * t)))
            for y in range(top, TOMB_FOOT + 3):
                if 0 <= x < TOMB_W:
                    c.set(x, y, GD[2] if y > top else GD[1])
    for _ in range(30):
        x, y = rng.randint(0, TOMB_W - 1), rng.randint(TOMB_FOOT - 10, TOMB_FOOT + 2)
        if c.get(x, y) in (raster.rgb(GD[2]),):
            c.set(x, y, GD[1] if rng.random() < 0.5 else GD[3])
    outline(c, skip_down=True)
    for x in range(4, TOMB_W - 4):                           # the contact shadow
        if c.get(x, TOMB_FOOT + 3)[3] == 0:
            c.set(x, TOMB_FOOT + 3, OCC)
    return c


# --- the beacon --------------------------------------------------------------------------------------

BEACON_W, BEACON_H = 28, 52
BEACON_FOOT = 48
BEACON_MIDDLE = 14
BASKET_Y = 22           # the basket's rim: where the fire sits (TombBeaconsEvent draws its light from here)


def flame(c, frame, height):
    """Tongues of fire rising out of the basket: white at the root, hot, then mid, the edge at the tips, each
    tongue leaning and forking by frame so four frames flicker. height 1 is a full fire, less is guttering."""
    rng = random.Random(400 + frame)
    tongues = [(-4, 0.62), (-1, 1.0), (2, 0.86), (5, 0.55)]
    for i, (dx, h) in enumerate(tongues):
        lean = [-1, 0, 1, 0][(frame + i) % 4]
        top = int(round(17 * h * height * (0.88 + 0.12 * ((frame * 3 + i) % 4) / 3.0)))
        for k in range(top):
            y = BASKET_Y - 1 - k
            t = k / float(max(1, top))
            x = BEACON_MIDDLE + dx + int(round(lean * t * t * 3))
            half = max(0, int(round((1.0 - t) * 2.2)))
            for xx in range(x - half, x + half + 1):
                if t < 0.25:
                    col = FIRE[0] if abs(xx - x) < half - 1 and i in (1, 2) else FIRE[1]
                elif t < 0.55:
                    col = FIRE[1] if abs(xx - x) < half else FIRE[2]
                elif t < 0.8:
                    col = FIRE[2]
                else:
                    col = FIRE[3]
                if c.get(xx, y)[3] == 0 or t < 0.5:
                    c.set(xx, y, col)
    for _ in range(int(3 * height) + 1):                     # sparks lifting off it
        sx = BEACON_MIDDLE + rng.randint(-5, 5)
        sy = BASKET_Y - int(18 * height) - rng.randint(1, 6)
        if sy > 1:
            c.set(sx, sy, FIRE[2] if rng.random() < 0.5 else FIRE[3])


def beacon(frame):
    """A signal beacon: a square pillar of granite on a stepped base, and on it a bronze fire-basket of bars.
    frame 0 cold (charred wood in the basket), 1 kindling (embers under the wood), 2-5 burning, 6-7 guttering."""
    c = raster.Canvas(BEACON_W, BEACON_H)
    m = BEACON_MIDDLE
    # THE BASE: two steps of cut stone.
    for (y0, y1, hw) in ((44, BEACON_FOOT, 9), (40, 43, 7)):
        for y in range(y0, y1 + 1):
            for x in range(m - hw, m + hw + 1):
                c.set(x, y, ST[2])
            c.set(m - hw, y, ST[1])
            c.set(m + hw, y, ST[3])
        c.hline(m - hw, m + hw, y0, ST[1])
        c.set(m - hw, y0, ST[0])
    # THE PILLAR: square, tapering a little, its left face lit and its right in shade, a joint or two.
    for y in range(26, 40):
        hw = 4 if y > 30 else 3
        for x in range(m - hw, m + hw + 1):
            c.set(x, y, ST[1] if x < m - 1 else (ST[2] if x < m + 2 else ST[3]))
        c.set(m - hw, y, ST[0] if y % 5 else ST[1])
        c.set(m + hw, y, ST[4])
    for (jy) in (30, 35):
        c.hline(m - 3, m + 4, jy, ST[3])
    c.set(m + 1, 33, ST[3])
    c.set(m - 2, 37, ST[2])
    # THE BASKET: a bronze bowl of bars on the pillar's head, its rim lit.
    for y in range(BASKET_Y, 26):
        t = (y - BASKET_Y) / 3.0
        hw = int(round(7 - 3 * t))
        for x in range(m - hw, m + hw + 1):
            c.set(x, y, GD[3] if (x - m) % 3 else GD[2])
        c.set(m - hw, y, GD[1])
        c.set(m + hw, y, GD[4])
    c.hline(m - 7, m + 7, BASKET_Y, GD[1])
    c.set(m - 7, BASKET_Y, GD[0])
    c.set(m + 7, BASKET_Y, GD[2])
    # What is in it.
    if frame == 0:
        for (x, y, col) in ((m - 4, 21, ST[4]), (m - 3, 21, ST[3]), (m - 1, 20, ST[4]), (m, 21, ST[3]), (m + 2, 20, ST[3]),
                            (m + 3, 21, ST[4]), (m + 5, 21, ST[4]), (m + 1, 21, ST[4]), (m - 2, 19, ST[3]), (m + 1, 19, ST[4])):
            c.set(x, y, col)
    elif frame == 1:
        for (x, y, col) in ((m - 4, 21, ST[4]), (m - 3, 21, FIRE[3]), (m - 1, 20, ST[4]), (m, 21, FIRE[2]), (m + 2, 20, ST[3]),
                            (m + 3, 21, FIRE[3]), (m + 5, 21, ST[4]), (m + 1, 21, FIRE[3]), (m - 2, 19, ST[3]),
                            (m + 1, 19, FIRE[3]), (m, 18, FIRE[3]), (m - 1, 17, FIRE[3])):
            c.set(x, y, col)
    else:
        burning = frame <= 5
        flame(c, (frame - 2) % 4, 1.0 if burning else (0.5 if frame == 6 else 0.34))
        for (x, y, col) in ((m - 4, 21, FIRE[3]), (m + 4, 21, FIRE[3]), (m - 2, 21, FIRE[2]), (m + 2, 21, FIRE[2])):
            c.set(x, y, col)
    outline(c, skip_down=True)
    for x in range(m - 10, m + 11):
        c.set(x, BEACON_FOOT + 1, OCC)
    return c


# --- the pictogram -----------------------------------------------------------------------------------

GLYPH = [
    ".....f......",
    "....ff.f....",
    "....fhff....",
    "...fhhhf....",
    "...fhhhf....",
    "..bbbbbbb...",
    "...bbbbb....",
    ".....s......",
    ".....s......",
    ".....s......",
    "....sss.....",
    "...sssss....",
]


def glyph():
    c = raster.Canvas(12, 12)
    pal = {"f": FIRE[2], "h": FIRE[1], "b": GD[1], "s": ST[1]}
    for y, row in enumerate(GLYPH):
        for x, ch in enumerate(row):
            if ch != ".":
                c.set(x, y, pal[ch])
    outline(c)
    return c


# --- output ------------------------------------------------------------------------------------------

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
    for i, cv in enumerate(canvases):
        img.alpha_composite(cv.img, (i * w, 0))
    return img


def main():
    for d in (OUT_WORLD, OUT_UI):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True
    tombs = [tomb(f) for f in range(4)]
    beacons = [beacon(f) for f in range(8)]
    for i, cv in enumerate(tombs):
        ok = _audit("tomb %d" % i, cv) and ok
    for i, cv in enumerate(beacons):
        ok = _audit("beacon %d" % i, cv) and ok
    _strip(tombs).save(os.path.join(OUT_WORLD, "tomb.png"))
    _strip(beacons).save(os.path.join(OUT_WORLD, "beacon.png"))
    g = glyph()
    ok = _audit("glyph tomb", g) and ok
    g.scaled(2).save(os.path.join(OUT_UI, "poi-tomb.png"))

    # The preview: the four tomb frames and the eight beacon frames at x4 on the Sands' dark ground, then the
    # tomb at x2 with a beacon either side as the event stands them (not to the 404 px ring, which is too big).
    bg = (46, 34, 18, 255)
    z = 4
    W = max(TOMB_W * z * 4, BEACON_W * z * 8) + 40
    H = TOMB_H * z + BEACON_H * z + 60
    sheet = Image.new("RGBA", (W, H), bg)
    for i, cv in enumerate(tombs):
        sheet.alpha_composite(cv.scaled(z), (10 + i * TOMB_W * z, 10))
    for i, cv in enumerate(beacons):
        sheet.alpha_composite(cv.scaled(z), (10 + i * BEACON_W * z, TOMB_H * z + 30))
    sheet.alpha_composite(g.scaled(8), (W - 120, H - 110))
    sheet.convert("RGB").save(PREVIEW)
    print("  tomb 4, beacon 8, glyph poi-tomb")
    if not ok:
        return 1
    print("tomb and beacons written, palette clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
