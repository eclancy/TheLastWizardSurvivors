# -*- coding: utf-8 -*-
"""The Mystic Ruins' side event art: the Five-Pointed Seal (.ai/chapter-wizards.md 4.5).

Run: python tools/art/five_pointed_seal.py

    world/seal/seal-floor.png     the courtyard, 208x208, two frames: sealed (the star's grooves full of
                                  his cold light) and broken (the grooves dark, cracked from the middle)
    world/seal/seal-stone.png     a guardian stone, 28x28, eight frames: its arrow pointing north, east,
                                  south, west - cut dark, then the same four lit
    world/seal/seal-plate.png     a stone's pressure plate, 20x12, two frames: raised, pressed
    world/seal/seal-seimei.png    Abe no Seimei kneeling in his own seal, 48x48, four frames of breath
    ui/poi/poi-seal.png           the arrow's pictogram for the seal
    effects/banish-seal.png       his Apotheosis's mark (The Five-Pointed Seal, banish): the small seal a
                                  ward lays on what it burns low, 24x24, four frames - forming, held,
                                  held bright, breaking
    tools/art/_five_pointed_seal_preview.png   (gitignored) for looking at

"The old wards were broken from the inside." The dark wizard turned a ward on its maker and sealed the
onmyoji inside his own five-pointed seal: a star cut into a courtyard floor, the master of wards
kneeling in the middle of it. Four guardian stones stand at its quarters, each cut with one arrow;
stepping on a stone's plate turns it a quarter, and when all four face the seal it breaks
(FivePointedSealEvent).

NOTHING LIKE THE BROKEN WARD. The Ruins' spell vault (secret_ruins.py) is five tall standing stones in
a ring, each notched, touched in order. These are four LOW SQUARE blocks at the four quarters of a star,
each with one arrow cut into its top - a state you turn, not an order you follow - and the star they
guard is on the floor, not in the air.

COLD LIGHT, BECAUSE IT IS HIS. The seal's light is the arcane ramp's violet: the dark wizard's, not the
player's (.ai/world-and-tone.md - warm light is reserved for what he does not have). An arrow that faces
the seal fills with it. Seimei himself is drawn a ramp step down, in shadow - he has been here a long
time - and nothing on him gives off light: his ward was taken. The banish mark is the opposite: the
player's ward doing the sealing now, so it burns in the light ramp.

Every sheet is authored 1:1 on its cell and drawn by the game at x2 (Sprite2D Scale 2), so a source pixel
is always two screen pixels. The floor is a ground decal (DrawLayers.Ground - 2); the stones, plates and
Seimei sort with the bodies at their feet.
"""
from __future__ import division, print_function

import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image

import anim_sets
import bonelight as bl
import pixel as P
import raster
import roster

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "seal")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
OUT_FX = os.path.join(ROOT, "assets", "bonelight", "effects")
PREVIEW = os.path.join(HERE, "_five_pointed_seal_preview.png")


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


ST = ramp("stone")
SL = ramp("steel")
LN = ramp("linen")
ARC = bl.ELEMENTS["arcane"]      # core, hot, mid, edge: the seal's light, his
LIT = bl.ELEMENTS["light"]       # the player's: the banish mark

# --- the courtyard ------------------------------------------------------------------------------

FLOOR = 208
FC = FLOOR // 2                  # its middle: FivePointedSealEvent stands the seal here
FLOOR_R = 98                     # how far the flags reach before the ground takes over
STAR_R = 72                      # the star's points, and its ring, in art pixels (x2 in the world)
RING_R = 75


def _star_points(r, cx=FC, cy=FC):
    return [(cx + r * math.cos(math.radians(-90 + 72 * k)), cy + r * math.sin(math.radians(-90 + 72 * k)))
            for k in range(5)]


def _seg_dist(px, py, a, b):
    ax, ay = a
    bx, by = b
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / float(dx * dx + dy * dy)))
    qx, qy = ax + dx * t, ay + dy * t
    return math.hypot(px - qx, py - qy)


def _groove_distance(x, y):
    """How far (art pixels) a point is from the nearest groove: the star's five lines and its ring."""
    pts = _star_points(STAR_R)
    d = min(_seg_dist(x + 0.5, y + 0.5, pts[k], pts[(k + 2) % 5]) for k in range(5))
    ring = abs(math.hypot(x + 0.5 - FC, y + 0.5 - FC) - RING_R)
    return min(d, ring)


def floor(broken):
    """The courtyard: flagstones in running bond to a ragged edge, the star and its ring cut into them.

    Seeded, so it is the same every run of the generator. The flags take stone's two light stops (the
    Ruins' slate is stone's dark end, so the courtyard reads as laid, not as more ground), with a lit
    top-left edge, a shaded bottom-right, mortar in the deep stop, and here and there a crack or a
    missing flag - the ground shows through where one is gone.
    """
    rnd = random.Random(4019)
    c = raster.Canvas(FLOOR, FLOOR)
    row_h = 14
    y = FC - FLOOR_R - 4
    band = 0
    while y < FC + FLOOR_R + 4:
        h = row_h + rnd.choice((-2, -1, 0, 1))
        x = FC - FLOOR_R - 4 - (band % 2) * 9
        while x < FC + FLOOR_R + 4:
            w = rnd.choice((16, 18, 20, 22, 24))
            fx0, fy0, fx1, fy1 = x + 1, y + 1, x + w - 1, y + h - 1
            mx, my = (fx0 + fx1) / 2.0, (fy0 + fy1) / 2.0
            r = math.hypot(mx - FC, my - FC)
            # The rim is ragged: past the middle of the last ring of flags some are simply gone.
            keep = r < FLOOR_R - 14 or (r < FLOOR_R + 2 and rnd.random() < 0.62)
            if keep:
                tone = ST[0] if rnd.random() < 0.55 else ST[1]
                for yy in range(fy0, fy1 + 1):
                    for xx in range(fx0, fx1 + 1):
                        if math.hypot(xx - FC, yy - FC) > FLOOR_R + 3:
                            continue
                        if yy == fy0 or xx == fx0:
                            col = SL[2] if tone == ST[0] else ST[0]
                        elif yy == fy1 or xx == fx1:
                            col = ST[2]
                        else:
                            col = tone
                        c.set(xx, yy, col)
                # Mortar round it, in the deep stop.
                for xx in range(fx0 - 1, fx1 + 2):
                    for yy in (fy0 - 1, fy1 + 1):
                        if c.get(xx, yy)[3] == 0 and math.hypot(xx - FC, yy - FC) <= FLOOR_R + 3:
                            c.set(xx, yy, ST[4])
                for yy in range(fy0, fy1 + 1):
                    for xx in (fx0 - 1, fx1 + 1):
                        if c.get(xx, yy)[3] == 0 and math.hypot(xx - FC, yy - FC) <= FLOOR_R + 3:
                            c.set(xx, yy, ST[4])
                # A crack across one flag in four: a short stepped line in the shade stop.
                if rnd.random() < 0.25 and fx1 - fx0 > 6:
                    cx0 = rnd.randint(fx0 + 2, fx1 - 2)
                    cy0 = fy0 + 1
                    for k in range(fy1 - fy0 - 1):
                        c.set(cx0, cy0 + k, ST[3])
                        if rnd.random() < 0.4:
                            cx0 += rnd.choice((-1, 1))
                            cx0 = max(fx0 + 1, min(fx1 - 1, cx0))
            x += w
        y += h
        band += 1

    # The grooves: three pixels wide, cut through flag and mortar alike.
    for yy in range(FLOOR):
        for xx in range(FLOOR):
            d = _groove_distance(xx, yy)
            if d > 1.8:
                continue
            if c.get(xx, yy)[3] == 0:
                continue
            if broken:
                c.set(xx, yy, OCC if d < 0.9 else ST[4])
            else:
                c.set(xx, yy, ARC[2] if d < 0.9 else ARC[3])
    if not broken:
        # Where two lines cross, and at the star's points, the light runs hottest.
        pts = _star_points(STAR_R)
        for (px, py) in pts:
            c.set(int(px), int(py), ARC[0])
            c.set(int(px) + 1, int(py), ARC[1])
            c.set(int(px), int(py) + 1, ARC[1])
        # The inner pentagon's corners, where the lines cross: the outer points' bearings turned a tenth.
        for k in range(5):
            a = math.radians(-90 + 36 + 72 * k)
            ix, iy = FC + STAR_R * 0.382 * math.cos(a), FC + STAR_R * 0.382 * math.sin(a)
            c.set(int(round(ix)), int(round(iy)), ARC[1])
    else:
        # Broken from the middle: cracks out from where he knelt, across the dead grooves.
        rnd2 = random.Random(77)
        for k in range(6):
            a = math.radians(rnd2.uniform(0, 360))
            x, y = float(FC), float(FC)
            for step in range(rnd2.randint(30, 60)):
                a += math.radians(rnd2.uniform(-25, 25))
                x += math.cos(a)
                y += math.sin(a)
                if c.get(int(x), int(y))[3] != 0:
                    c.set(int(x), int(y), OCC)
    return c


# --- the guardian stones ------------------------------------------------------------------------

STONE = 28
STONE_FOOT = 25            # the row the stone's foot stands on; the contact shadow is the row under it
TOP = (3, 3, 24, 16)       # the top face: x0, y0, x1, y1
DIRS = [(0, -1), (1, 0), (0, 1), (-1, 0)]   # north, east, south, west - the order of the frames


def _arrow_mask(d):
    """An arrow on the top face pointing d: a two-pixel shaft and a head as wide as the face allows."""
    x0, y0, x1, y1 = TOP
    cx, cy = (x0 + x1) / 2.0 + 0.5, (y0 + y1) / 2.0 + 0.5
    horizontal = d[1] == 0
    half_len = 7.5 if horizontal else 6.4
    head = 4.5 if horizontal else 3.9
    mask = set()
    for y in range(y0 + 1, y1):
        for x in range(x0 + 1, x1):
            px, py = x + 0.5 - cx, y + 0.5 - cy
            u = px * d[0] + py * d[1]           # along the arrow
            v = -px * d[1] + py * d[0]          # across it
            tip = half_len - head
            if -half_len <= u <= tip and abs(v) <= 1.0:
                mask.add((x, y))
            elif tip <= u <= half_len and abs(v) <= (half_len - u) * (head / (head + 0.4)) + 0.6:
                mask.add((x, y))
    return mask


def stone(direction, lit):
    """A low square block of the courtyard's stone: a broad top face with the arrow cut in it, a short
    front face, a contact shadow. Lit, the cut is full of the seal's light."""
    c = raster.Canvas(STONE, STONE)
    x0, y0, x1, y1 = TOP
    # The front face, under the top.
    for y in range(y1 + 1, STONE_FOOT + 1):
        for x in range(x0, x1 + 1):
            c.set(x, y, ST[2] if x > x0 + 1 else ST[1])
        c.set(x1, y, ST[3])
    c.hline(x0, x1, STONE_FOOT, ST[3])
    # A chip out of its front corner and a seam across the face: it is old.
    c.set(x1 - 1, y1 + 2, ST[4])
    c.set(x1 - 2, y1 + 3, ST[4])
    c.hline(x0 + 3, x0 + 9, y1 + 5, ST[3])
    # The top face: lit, its key edges brighter.
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            c.set(x, y, ST[0])
    c.hline(x0, x1, y0, SL[2])
    c.vline(x0, y0, y1, SL[2])
    c.hline(x0 + 1, x1, y1, ST[1])
    c.vline(x1, y0 + 1, y1, ST[1])
    # Weathering on the top: a few pits.
    for (px, py) in ((x0 + 3, y0 + 2), (x1 - 3, y1 - 2), (x0 + 5, y1 - 1), (x1 - 2, y0 + 3)):
        c.set(px, py, ST[1])
    # The arrow.
    mask = _arrow_mask(DIRS[direction])
    for (x, y) in mask:
        if lit:
            edge = any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (0, 1), (-1, 0), (0, -1)))
            c.set(x, y, ARC[3] if edge else ARC[2])
        else:
            c.set(x, y, ST[4])
    if lit:
        # The arrow's tip burns hottest.
        d = DIRS[direction]
        tips = sorted(mask, key=lambda p: -(p[0] * d[0] + p[1] * d[1]))
        for p in tips[:2]:
            c.set(p[0], p[1], ARC[1])
    # The occlusion outline round the block, and its contact shadow.
    for y in range(y0 - 1, STONE_FOOT + 2):
        for x in range(x0 - 1, x1 + 2):
            if c.get(x, y)[3] != 0:
                continue
            if any(c.get(x + dx, y + dy)[3] != 0 and c.get(x + dx, y + dy) != raster.rgb(OCC)
                   for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                c.set(x, y, OCC)
    c.hline(x0, x1 + 1, STONE_FOOT + 1, OCC)
    return c


# --- the plates ---------------------------------------------------------------------------------

PLATE_W, PLATE_H = 20, 12


def plate(pressed):
    """A square slab set in the floor, its edge cut round so it can be seen to be a thing you step on.
    Pressed, it sits a pixel down, darker, with the seal's light showing in the gap round it."""
    c = raster.Canvas(PLATE_W, PLATE_H)
    x0, y0, x1, y1 = 1, 1, PLATE_W - 2, PLATE_H - 3
    # The socket it sits in.
    c.rect(x0 - 1, y0 - 1, x1 + 1, y1 + 2, OCC)
    dy = 1 if pressed else 0
    top = ST[1] if pressed else ST[0]
    for y in range(y0 + dy, y1 + 1 + dy):
        for x in range(x0, x1 + 1):
            c.set(x, y, top)
    c.hline(x0, x1, y0 + dy, ST[1] if pressed else SL[2])
    c.vline(x0, y0 + dy, y1 + dy, ST[1] if pressed else SL[2])
    c.hline(x0, x1, y1 + dy, ST[2])
    # The carved ring on its face.
    cx, cy = (x0 + x1) / 2.0, (y0 + y1) / 2.0 + dy
    for y in range(y0 + 1 + dy, y1 + dy):
        for x in range(x0 + 1, x1):
            r = math.hypot((x + 0.5 - cx - 0.5) / 1.45, y + 0.5 - cy - 0.5)
            if 2.4 <= r <= 3.4:
                c.set(x, y, ST[3])
    if pressed:
        c.hline(x0, x1, y0, ARC[3])
        c.set(x0 - 1, y0 + 1, ARC[3])
        c.set(x1 + 1, y0 + 1, ARC[3])
    return c


# --- Seimei, kneeling ---------------------------------------------------------------------------

def seimei_kneeling():
    """Abe no Seimei kneeling in seiza at the middle of his own seal, head bowed, hands folded in his lap.

    The same man as roster.seimei(): the pale kariginu, the black eboshi, his star on his breast - but no
    wand and no ward. What he made light with was taken. Drawn with the roster's own helpers on the
    wizard cell, so he is the same pixels' size as the figure the player will choose.
    """
    g = roster.G(48)
    mid, lit, dark = roster.ROBES["seimei"]
    CX = 23
    # The robe pooled round him on the floor, knees under it, the black of the trousers at the front.
    roster.mass(g, [(31, 14, 32), (37, 11, 35), (42, 9, 37), (45, 10, 36)], mid, lit, dark)
    roster.fold(g, [(17, 33), (13, 44)], dark, lit)
    roster.fold(g, [(29, 33), (33, 44)], dark, lit)
    roster.fold(g, [(22, 38), (21, 45)], "E", lit)
    g.span(45, 16, 30, "J")
    g.span(44, 18, 28, "N")
    g.set(18, 44, "p")
    roster.tatters(g, [(46, 11, 15), (46, 31, 35)], "e", "l", "E")
    # The torso, upright.
    roster.mass(g, [(22, 18, 28), (26, 17, 29), (31, 16, 30)], mid, lit, dark)
    roster.fold(g, [(20, 24), (19, 31)], dark, lit)
    roster.fold(g, [(27, 25), (28, 31)], dark, lit)
    g.span(31, 16, 30, "E")
    g.span(32, 17, 29, "e")
    # His star.
    sx0, sy0 = CX, 24
    for i, (dx, run) in enumerate(roster.SEIMEI_STAR):
        y = sy0 + (i if i < 6 else 5)
        g.span(y, sx0 + dx, sx0 + dx + run - 1, "J")
    # The sleeves, hanging either side to the floor.
    roster.mass(g, [(22, 15, 18), (28, 12, 17), (36, 11, 16), (38, 12, 15)], mid, lit, dark)
    roster.mass(g, [(22, 28, 31), (28, 29, 34), (36, 30, 35), (38, 31, 34)], mid, lit, dark)
    roster.fold(g, [(15, 26), (13, 36)], dark, lit)
    roster.fold(g, [(31, 26), (33, 36)], dark, lit)
    # The hands, folded in his lap.
    roster.hand(g, 21, 33)
    roster.hand(g, 23, 33)
    g.set(22, 34, "i")
    # The collar and the bowed head.
    g.span(20, 20, 27, "E")
    g.span(21, 19, 28, "e")
    roster.head(g, CX + 1, 14, 3.0, brow="low", gaze=0, mouth="set", eye="shadow")
    g.set(21, 15, "J")
    g.set(27, 15, "J")
    roster.mass(g, [(7, 19, 21), (9, 20, 24), (11, 20, 26), (13, 20, 27)], "j", "N", "J")
    roster.fold(g, [(21, 10), (26, 12)], "J", "N", side=(0, -1))
    g.span(14, 20, 27, "J")
    roster.contact(g, 9, 37)
    rows = g.rows()
    # In shadow: a ramp step down. He has been here a long time.
    return anim_sets.step(rows, 1, anim_sets._PLAYER_POS)


# --- the pictogram ------------------------------------------------------------------------------

# A filled five-pointed star, lit on its left half: at twelve pixels an outlined pentagram is a tangle.
GLYPH = [
    "............",
    ".....ab.....",
    ".....ab.....",
    "....aabb....",
    ".aaaaabbbbb.",
    "..aaaabbbb..",
    "...aaabbb...",
    "...aaabbb...",
    "..aaa..bbb..",
    "..aa....bb..",
    ".a........b.",
    "............",
]
GLYPH_PAL = {"a": ARC[1], "b": ARC[2]}


def glyph():
    c = raster.Canvas(12, 12)
    for y, row in enumerate(GLYPH):
        for x, ch in enumerate(row):
            if ch != ".":
                c.set(x, y, GLYPH_PAL[ch])
    # An occlusion outline, so the violet reads inside the badge's cyan rim.
    for y in range(12):
        for x in range(12):
            if c.get(x, y)[3] == 0 and any(GLYPH[yy][xx] != "." for yy in range(max(0, y - 1), min(12, y + 2))
                                           for xx in range(max(0, x - 1), min(12, x + 2))):
                c.set(x, y, OCC)
    return c


# --- the banish mark (his Apotheosis) -----------------------------------------------------------

MARK = 24


def banish_mark(frame):
    """The seal a ward lays on what it burns below a quarter: a ring and a star, in the player's light.

    0 forming (the ring drawn, the star faint), 1 held, 2 held at its brightest (the two alternate while
    the ward burns), 3 breaking (the ring split, the star flaring) - the frame the sealed are banished on.
    """
    c = raster.Canvas(MARK, MARK)
    cx = cy = MARK / 2.0
    r_ring = 10.0
    pts = [(cx + 8.6 * math.cos(math.radians(-90 + 72 * k)), cy + 8.6 * math.sin(math.radians(-90 + 72 * k)))
           for k in range(5)]
    for y in range(MARK):
        for x in range(MARK):
            px, py = x + 0.5, y + 0.5
            ring = abs(math.hypot(px - cx, py - cy) - r_ring)
            star = min(_seg_dist(px, py, pts[k], pts[(k + 2) % 5]) for k in range(5))
            ang = math.degrees(math.atan2(py - cy, px - cx))
            if ring < 0.75:
                if frame == 3 and (int((ang + 360) / 30) % 2 == 0):
                    continue        # breaking: the ring comes apart in pieces
                c.set(x, y, LIT[3] if frame == 0 else (LIT[1] if frame == 2 else LIT[2]))
            elif star < 0.62:
                if frame == 0:
                    c.set(x, y, LIT[3])
                elif frame == 3:
                    c.set(x, y, LIT[0])
                else:
                    c.set(x, y, LIT[1] if frame == 2 else LIT[2])
    if frame in (1, 2, 3):
        for (px, py) in pts:
            c.set(int(px), int(py), LIT[0])
    # The outline, so it holds over a pale body.
    for y in range(MARK):
        for x in range(MARK):
            if c.get(x, y)[3] != 0:
                continue
            if any(c.get(x + dx, y + dy)[3] != 0 and c.get(x + dx, y + dy) != raster.rgb(OCC)
                   for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                c.set(x, y, OCC)
    return c


# --- output -------------------------------------------------------------------------------------

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


def main():
    for d in (OUT_WORLD, OUT_UI, OUT_FX):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True

    floors = [floor(False), floor(True)]
    stones = [stone(d, False) for d in range(4)] + [stone(d, True) for d in range(4)]
    plates = [plate(False), plate(True)]
    marks = [banish_mark(f) for f in range(4)]
    for name, group in (("floor", floors), ("stone", stones), ("plate", plates), ("banish", marks)):
        for i, c in enumerate(group):
            ok = _audit("%s %d" % (name, i), c) and ok
    _strip(floors).save(os.path.join(OUT_WORLD, "seal-floor.png"))
    _strip(stones).save(os.path.join(OUT_WORLD, "seal-stone.png"))
    _strip(plates).save(os.path.join(OUT_WORLD, "seal-plate.png"))
    _strip(marks).save(os.path.join(OUT_FX, "banish-seal.png"))

    kneel = seimei_kneeling()
    problems = P.check_anchor("seimei kneeling", [kneel], 48)
    if problems:
        print(problems)
    P.write_strip(roster.idle(kneel), roster.PALETTE, os.path.join(OUT_WORLD, "seal-seimei.png"), 48)

    g = glyph()
    ok = _audit("glyph seal", g) and ok
    g.scaled(2).save(os.path.join(OUT_UI, "poi-seal.png"))

    # The preview: the courtyard with a stone at each quarter, its plate outside it, and him in the middle,
    # all at x2 as the game draws them, then the sheets.
    z = 2
    O = 80
    canvas = Image.new("RGBA", (FLOOR * z + 2 * O + 300, FLOOR * z + 2 * O), raster.rgb(ST[3]))
    canvas.alpha_composite(floors[0].scaled(z), (O, O))
    for q, (dx, dy) in enumerate(DIRS):
        sx = O + (FC + dx * 90) * z - STONE
        sy = O + (FC + dy * 90) * z - STONE_FOOT * z
        canvas.alpha_composite(stones[(q + 2) % 4 + (4 if q % 2 == 0 else 0)].scaled(z), (int(sx), int(sy)))
        px = O + (FC + dx * 122) * z - PLATE_W
        py = O + (FC + dy * 122) * z - PLATE_H
        canvas.alpha_composite(plates[q % 2].scaled(z), (int(px), int(py)))
    kimg = Image.open(os.path.join(OUT_WORLD, "seal-seimei.png")).crop((0, 0, 48, 48)).resize((96, 96), Image.NEAREST)
    canvas.alpha_composite(kimg, (O + FC * z - 48, O + FC * z - 90))
    right = FLOOR * z + 2 * O
    canvas.alpha_composite(floors[1].img, (right + 20, 20))
    canvas.alpha_composite(_strip(stones), (right + 10, 250))
    canvas.alpha_composite(_strip(marks).resize((MARK * 4 * 2, MARK * 2), Image.NEAREST), (right + 10, 300))
    canvas.alpha_composite(g.scaled(4), (right + 10, 370))
    canvas.convert("RGB").save(PREVIEW)

    print("  seal floor 2, stone 8, plate 2, seimei 4, banish 4, glyph poi-seal")
    if not ok:
        return 1
    print("five-pointed seal written, palette clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
