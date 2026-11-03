# -*- coding: utf-8 -*-
"""The Court's second side event and Ma Liang's final form: the doors he paints (.ai/chapter-wizards.md 4.8).

Run: python tools/art/painted_door.py

    world/door/door-sketch.png    the door he drew himself on the Gallery's floor at its west end and was never let
                                  finish: an arched door sketched in charcoal - a dashed outline, the faint lines of its
                                  planks and its ring - and fifteen dots along the outline, numbered, as a child's
                                  dot-to-dot is. 104x172, one frame. The ink that follows the player's feet along it is
                                  drawn by DottedDoorEvent over this, in the world's 2 px grid.
    world/door/door-inked.png     the same door when the last dot is reached: inked in his light all round and PAINTED
                                  - a door of red lacquer with gold studs and a ring - then opening onto his light:
                                  shut, ajar, half open, open. 104x172, four frames.
    effects/painted-door.png      The Painted Door, his Apotheosis (Familiar.PaintedDoorId): the small door the crow
                                  paints where it strikes, and the second one across the field the struck step out of.
                                  Drawn in the arcane ramp only, as every spell is light: two frames of it being drawn
                                  (half, whole), shut, ajar and open. 20x30, five frames.
    ui/poi/poi-door.png           the arrow's pictogram: an arched door in dots
    tools/art/_painted_door_preview.png   (gitignored) for looking at

THE TRACE IS ONE PATH, AND THIS FILE AND DottedDoorEvent AGREE ON IT. Up the left jamb from its foot, over the arch,
down the right jamb to its foot: HALF_W, HALF_H and ARCH_R below in sheet pixels, which the game draws at x2
(DottedDoorEvent.HalfWidth, HalfHeight, ArchRadius in world pixels; RegressionChecks.ValidateDottedDoor holds the two to
each other and the sheet to its size). The dots stand at equal steps along it, the first at the left jamb's foot and the
last at the right's, and each number sits outside the outline beside its dot.

CHARCOAL ON THE BOARDS. The house's floors are dark (oak boards, gold's dark end), so the charcoal is the bone row's
greys - pale against them, as a sketch on a dark ground reads - and the ink is his light, the arcane ramp, the colour the
crow lifting off his brush is drawn in (roster.maliang). The painted door when it comes alive is the only pigment here:
the red row's lacquer and gold's studs, a real door in a house of his.
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
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "door")
OUT_FX = os.path.join(ROOT, "assets", "bonelight", "effects")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
PREVIEW = os.path.join(HERE, "_painted_door_preview.png")
SHOTS = os.path.join(os.path.dirname(ROOT), "_shots", "wizard-maliang")
TILES = os.path.join(ROOT, "assets", "bonelight", "tiles")
HIM = os.path.join(ROOT, "assets", "bonelight", "characters", "maliang-1.png")


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


SK = ramp("skin")     # bone: the charcoal's greys on a dark floor
RD = ramp("red")      # the lacquer
GD = ramp("gold")     # the studs and the ring; the house's oak at its dark end
AR = bl.ELEMENTS["arcane"]   # core, hot, mid, edge: his light, the ink

# --- the door's path, in sheet pixels (the game draws it at x2) ---------------------------------------
W, H = 104, 172
CX, CY = 52, 92          # the door's middle
HALF_W = 40              # the jambs stand this far either side of the middle
HALF_H = 75              # the foot of the jambs this far below it, the top of the arch this far above
ARCH_R = 40              # a round arch: its radius is the half-width
DOTS = 15

# A 4x4 ordered dither, thresholds 0..1.
BAYER4 = [[0 / 16.0, 8 / 16.0, 2 / 16.0, 10 / 16.0], [12 / 16.0, 4 / 16.0, 14 / 16.0, 6 / 16.0],
          [3 / 16.0, 11 / 16.0, 1 / 16.0, 9 / 16.0], [15 / 16.0, 7 / 16.0, 13 / 16.0, 5 / 16.0]]

JAMB = 2 * HALF_H - ARCH_R          # each jamb, foot to the spring of the arch
ARC = math.pi * ARCH_R
LENGTH = 2 * JAMB + ARC


def along(s):
    """The point s sheet pixels along the path from the left jamb's foot, relative to the middle, and the outward
    normal there."""
    s = max(0.0, min(LENGTH, s))
    if s <= JAMB:
        return (-HALF_W, HALF_H - s), (-1.0, 0.0)
    if s <= JAMB + ARC:
        a = math.pi + (s - JAMB) / ARCH_R
        spring = -HALF_H + ARCH_R
        return (ARCH_R * math.cos(a), spring + ARCH_R * math.sin(a)), (math.cos(a), math.sin(a))
    return (HALF_W, -HALF_H + ARCH_R + (s - JAMB - ARC)), (1.0, 0.0)


def dot_s(k):
    return k * LENGTH / (DOTS - 1)


# --- digits: a 3x5 hand ------------------------------------------------------------------------------
DIGITS = {
    "0": ["###", "#.#", "#.#", "#.#", "###"],
    "1": [".#.", "##.", ".#.", ".#.", "###"],
    "2": ["##.", "..#", ".#.", "#..", "###"],
    "3": ["##.", "..#", ".#.", "..#", "##."],
    "4": ["#.#", "#.#", "###", "..#", "..#"],
    "5": ["###", "#..", "##.", "..#", "##."],
    "6": [".##", "#..", "###", "#.#", "###"],
    "7": ["###", "..#", ".#.", ".#.", ".#."],
    "8": ["###", "#.#", ".#.", "#.#", "###"],
    "9": ["###", "#.#", "###", "..#", "##."],
}


def number(c, x, y, text, col):
    """`text` with its middle at (x, y), a pixel between digits."""
    w = len(text) * 4 - 1
    x0 = int(round(x - w / 2.0))
    y0 = int(round(y - 2))
    for i, ch in enumerate(text):
        for r, row in enumerate(DIGITS[ch]):
            for k, px in enumerate(row):
                if px == "#":
                    c.set(x0 + i * 4 + k, y0 + r, col)


def sketch():
    """The dot-to-dot: a dashed charcoal outline, the faint lines of the door it is to be, the dots and their numbers."""
    c = raster.Canvas(W, H)
    # The door it is to be, barely there: the planks' joints, a ring, and the sill - construction lines.
    for x in (-26, -13, 0, 13, 26):
        for y in range(-HALF_H + ARCH_R - 18 + (abs(x) // 2), HALF_H - 4, 1):
            if (y * 7 + x) % 9 < 4:
                c.set(CX + x, CY + y, SK[4])
    for k in range(18):
        a = 2.0 * math.pi * k / 18.0
        if k % 3 != 1:
            c.set(int(round(CX + 18 + 3.5 * math.cos(a))), int(round(CY + 6 + 3.5 * math.sin(a))), SK[3])
    for x in range(-HALF_W - 4, HALF_W + 5):
        if x % 5 < 3:
            c.set(CX + x, CY + HALF_H + 1, SK[4])
    # The outline, dashed: three on, two off, along the path, every pixel a grey that catches the light on its top edge.
    steps = int(LENGTH * 3)
    for i in range(steps + 1):
        s = LENGTH * i / steps
        if int(s) % 5 >= 3:
            continue
        (x, y), _n = along(s)
        c.set(int(round(CX + x)), int(round(CY + y)), SK[2])
    # The dots, and their numbers outside the outline.
    for k in range(DOTS):
        (x, y), (nx, ny) = along(dot_s(k))
        px, py = int(round(CX + x)), int(round(CY + y))
        for (dx, dy, col) in ((-1, -1, SK[1]), (0, -1, SK[1]), (1, -1, SK[2]), (-1, 0, SK[1]), (0, 0, SK[0]),
                              (1, 0, SK[2]), (-1, 1, SK[2]), (0, 1, SK[2]), (1, 1, SK[3])):
            c.set(px + dx, py + dy, col)
        off = 7.5 if k not in (0, DOTS - 1) else 6.0
        tx, ty = CX + x + nx * off, CY + y + ny * off
        if k in (0, DOTS - 1):
            ty += 1
        number(c, tx, ty, str(k + 1), SK[1])
    return c


def outline(c, inks=(AR[2], AR[1], AR[3]), width=2):
    """The arch inked all round: the mid stop, the hot one on its key side, the edge stop under it."""
    steps = int(LENGTH * 3)
    for i in range(steps + 1):
        (x, y), (nx, ny) = along(LENGTH * i / steps)
        px, py = CX + x, CY + y
        c.set(int(round(px)), int(round(py)), inks[0])
        c.set(int(round(px - nx)), int(round(py - ny)), inks[0] if width > 1 else None)
        c.set(int(round(px + nx)), int(round(py + ny)), inks[2])
        c.set(int(round(px - nx * 2 + (-0.5 if nx > 0 else 0))), int(round(py - ny * 2)), None)
    # The hot stop along the outline's lit (upper-left) side.
    for i in range(steps + 1):
        (x, y), (nx, ny) = along(LENGTH * i / steps)
        if nx < -0.2 or ny < -0.6:
            c.set(int(round(CX + x - nx)), int(round(CY + y - ny)), inks[1])
    for x in range(-HALF_W - 2, HALF_W + 3):
        c.set(CX + x, CY + HALF_H + 1, AR[3])
        c.set(CX + x, CY + HALF_H, AR[2])


def inside(x, y):
    """True for a point (relative to the middle) inside the arch."""
    if abs(x) > HALF_W - 2 or y > HALF_H - 1:
        return False
    spring = -HALF_H + ARCH_R
    if y >= spring:
        return True
    return math.hypot(x, y - spring) <= ARCH_R - 2


def door_panel(c, open_frac):
    """The painted door: red lacquer planks in an arched leaf, gold studs, a ring - swung `open_frac` of the way in
    on its left hinge, so the leaf narrows toward the hinge and the doorway behind it shows his light."""
    leaf_w = (2 * (HALF_W - 2)) * (1.0 - open_frac)
    hinge = -HALF_W + 2
    # Behind the leaf: the doorway - dark at its edges, his light in it, brightest low in the middle.
    for y in range(-HALF_H, HALF_H):
        for x in range(-HALF_W, HALF_W + 1):
            if not inside(x, y):
                continue
            if open_frac == 0:
                continue
            # Ordered-dithered (raster.BAYER's 4x4), so the light falls off in a grain and not in rings.
            d = math.hypot(x / float(HALF_W), (y - HALF_H * 0.45) / float(HALF_H))
            t = d / 1.1 * 4.0 + (BAYER4[(y + 64) % 4][(x + 64) % 4] - 0.5)
            col = [AR[0], AR[1], AR[2], AR[3], OCC][max(0, min(4, int(t)))]
            c.set(CX + x, CY + y, col)
    if leaf_w < 1.0:
        return
    for y in range(-HALF_H, HALF_H):
        for x in range(-HALF_W, HALF_W + 1):
            if not inside(x, y):
                continue
            u = (x - hinge) / float(2 * (HALF_W - 2))         # 0 at the hinge, 1 at the latch, across the shut leaf
            if x - hinge > leaf_w:
                continue
            # Planks: five, each lit on its left edge, its joint dark; foreshortened toward the hinge as it swings.
            pu = (x - hinge) / max(1.0, leaf_w)
            plank = int(pu * 5)
            edge = abs(pu * 5 - round(pu * 5)) * leaf_w / 5.0
            col = RD[2]
            if edge < 0.6:
                col = RD[4]
            elif (pu * 5) % 1.0 < 0.18:
                col = RD[1]
            if (y + plank * 3) % 23 == 0:
                col = RD[3]                                   # the grain
            c.set(CX + x, CY + y, col)
    # The studs: three rows of gold heads across the leaf, and the ring on the latch side.
    for row_y in (-HALF_H + ARCH_R + 4, 2, HALF_H - 14):
        for k in range(5):
            sx = hinge + (k + 0.5) * leaf_w / 5.0
            if not inside(sx - 0, row_y):
                continue
            c.set(int(round(CX + sx)), CY + row_y, GD[1])
            c.set(int(round(CX + sx)) + 1, CY + row_y, GD[2])
            c.set(int(round(CX + sx)), CY + row_y + 1, GD[3])
    if leaf_w > 20:
        rx, ry = hinge + leaf_w * 0.82, 8
        for k in range(16):
            a = 2.0 * math.pi * k / 16.0
            c.set(int(round(CX + rx + 3 * math.cos(a))), int(round(CY + ry + 3 * math.sin(a))), GD[1] if a > math.pi else GD[2])
        c.set(int(round(CX + rx)), CY + ry - 4, GD[3])
    # The leaf's latch edge, where it swings away from the frame: a dark line, the thickness of the door.
    if open_frac > 0:
        ex = int(round(CX + hinge + leaf_w))
        for y in range(-HALF_H, HALF_H):
            if inside(ex - CX, y):
                c.set(ex, CY + y, RD[4])
                c.set(ex + 1, CY + y, OCC)


def inked(frame):
    """Shut, ajar, half open, open."""
    c = raster.Canvas(W, H)
    door_panel(c, [0.0, 0.25, 0.6, 0.96][frame])
    outline(c)
    # The dots stay, inked: where the trace went.
    for k in range(DOTS):
        (x, y), _n = along(dot_s(k))
        px, py = int(round(CX + x)), int(round(CY + y))
        c.set(px, py, AR[0])
        c.set(px - 1, py, AR[1])
        c.set(px, py - 1, AR[1])
    return c


# --- The Painted Door, the form's small door ----------------------------------------------------------
FW, FH = 20, 30
FCX, FTOP, FFOOT = 10, 3, 27     # its middle column, the top of its arch, the row its foot stands on
FHW = 7


def small_inside(x, y):
    spring = FTOP + FHW
    if abs(x - FCX) > FHW - 1 or y > FFOOT - 1 or y < FTOP + 1:
        return False
    return y >= spring or math.hypot(x - FCX, y - spring) <= FHW - 1


def small_door(frame):
    """0-1 being drawn (half the outline, then all of it), 2 shut, 3 ajar, 4 open. Light only: the arcane ramp."""
    c = raster.Canvas(FW, FH)
    spring = FTOP + FHW
    pts = []
    for y in range(FFOOT, spring - 1, -1):
        pts.append((FCX - FHW, y))
    for k in range(1, 24):
        a = math.pi + math.pi * k / 24.0
        pts.append((FCX + FHW * math.cos(a), spring + FHW * math.sin(a)))
    for y in range(spring, FFOOT + 1):
        pts.append((FCX + FHW, y))
    drawn = len(pts) // 2 if frame == 0 else len(pts)
    open_frac = {2: 0.0, 3: 0.4, 4: 0.92}.get(frame, 0.0)
    if frame >= 2:
        leaf = (2 * FHW - 2) * (1.0 - open_frac)
        for y in range(FTOP, FFOOT):
            for x in range(FCX - FHW, FCX + FHW + 1):
                if not small_inside(x, y):
                    continue
                if x - (FCX - FHW + 1) <= leaf:
                    # The leaf: a door of his light's dark end, its planks' joints a stop up - never dark bars on a
                    # dark ground, which read as a cage's.
                    c.set(x, y, AR[2] if (x - FCX + 1) % 5 == 0 else AR[3])
                else:
                    d = math.hypot((x - FCX) / float(FHW), (y - FFOOT + 4) / 14.0)
                    c.set(x, y, AR[0] if d < 0.4 else AR[1] if d < 0.75 else AR[2])
        if frame == 2:
            c.set(FCX + 4, FCX + 8, AR[1])       # the ring
    for i, (x, y) in enumerate(pts[:drawn]):
        c.set(int(round(x)), int(round(y)), AR[2] if i % 7 else AR[1])
    if frame >= 1:
        for x in range(FCX - FHW - 1, FCX + FHW + 2):
            c.set(x, FFOOT + 1, AR[3])           # the threshold, drawn on the ground
    # The wet head of the stroke on the frames it is being drawn.
    if frame <= 1 and drawn:
        hx, hy = pts[drawn - 1]
        c.set(int(round(hx)), int(round(hy)), AR[0])
    return c


# --- the pictogram -------------------------------------------------------------------------------------
GLYPH = [
    "....aaaa....",
    "..aa....aa..",
    ".a..hhhh..a.",
    ".a.h....h.a.",
    "a..h....h..a",
    "a..h....h..a",
    "a..h..c.h..a",
    "a..h....h..a",
    "a..h....h..a",
    "a..h....h..a",
    "a..hhhhhh..a",
    "aaaaaaaaaaaa",
]
GLYPH_PAL = {"a": AR[1], "h": AR[3], "c": AR[0]}


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


# --- output --------------------------------------------------------------------------------------------

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


def _boards(w, h, zoom):
    img = Image.new("RGBA", (w, h), raster.rgb(GD[4]))
    path = os.path.join(TILES, "court_boards_fill.png")
    if os.path.exists(path):
        tile = Image.open(path).convert("RGBA")
        t = tile.resize((tile.size[0] * zoom // 2, tile.size[1] * zoom // 2), Image.NEAREST)
        for y in range(0, h, t.size[1]):
            for x in range(0, w, t.size[0]):
                img.alpha_composite(t.crop((0, 0, min(t.size[0], w - x), min(t.size[1], h - y))), (x, y))
    return img


def main():
    for d in (OUT_WORLD, OUT_FX, OUT_UI):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True
    sk = sketch()
    ok = _audit("sketch", sk) and ok
    sk.img.save(os.path.join(OUT_WORLD, "door-sketch.png"))
    ink = [inked(f) for f in range(4)]
    for f, c in enumerate(ink):
        ok = _audit("inked %d" % f, c) and ok
    _strip(ink).save(os.path.join(OUT_WORLD, "door-inked.png"))
    small = [small_door(f) for f in range(5)]
    for f, c in enumerate(small):
        ok = _audit("painted door %d" % f, c) and ok
    _strip(small).save(os.path.join(OUT_FX, "painted-door.png"))
    g = glyph()
    ok = _audit("glyph door", g) and ok
    g.scaled(2).save(os.path.join(OUT_UI, "poi-door.png"))

    # The preview at x4 (x2 of the world) on the Court's boards: the sketch, the inked door shut and open with him in
    # the doorway, and the form's small door's frames large.
    z = 4
    PW, PH = 1300, 760
    sheet = _boards(PW, PH, z)
    sheet.alpha_composite(sk.scaled(z), (10, 10))
    sheet.alpha_composite(ink[0].scaled(z), (10 + W * z + 10, 10))
    sheet.alpha_composite(ink[3].scaled(z), (10 + 2 * (W * z + 10), 10))
    if os.path.exists(HIM):
        him = Image.open(HIM).convert("RGBA").resize((48 * z, 48 * z), Image.NEAREST)
        sheet.alpha_composite(him, (10 + 2 * (W * z + 10) + (CX - 24) * z, 10 + (CY + HALF_H - 46) * z))
    for f, c in enumerate(small):
        sheet.alpha_composite(c.scaled(6), (10 + f * (FW * 6 + 10), 10 + H * z + 10 - 200))
    sheet.alpha_composite(g.scaled(8), (PW - 110, PH - 110))
    sheet.convert("RGB").save(PREVIEW)
    if os.path.isdir(SHOTS):
        sheet.convert("RGB").save(os.path.join(SHOTS, "door_art.png"))
    print("  door sketch %dx%d, inked %dx%d x4, painted door %dx%d x5, glyph poi-door; path %.1f px, %d dots" % (
        W, H, W, H, FW, FH, LENGTH, DOTS))
    if not ok:
        return 1
    print("painted door written, palette clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
