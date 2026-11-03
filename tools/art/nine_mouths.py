# -*- coding: utf-8 -*-
"""The Sunken Cave's side event art: the nine mouths (.ai/chapter-wizards.md 4.3).

Run: python tools/art/nine_mouths.py

    world/mouths/mouths-cliff.png the whole crescent of rock, 300x116, its nine mouths cut in it - one painting,
                                  which NineMouthsEvent cuts into nine strips (strip_bounds), each y-sorted at
                                  its own mouth's foot, so the cliff hides whoever walks behind it as rock does
                                  and never shows a seam
    ui/poi/poi-mouths.png         the arrow's pictogram for the cliff
    tools/art/_nine_mouths_preview.png   (gitignored) the cliff at x2 with its strips and thresholds marked

"She sees what is coming. He keeps her where nobody comes to ask, under the water, in a jar." The cave at
Cumae had a hundred mouths and spoke with a hundred voices; here there are nine, a crescent of rock with an
opening at the foot of each, and her voice comes out of all of them, one a beat after another - the first
is hers (NineMouthsEvent).

THE CAVE'S OWN ROCK. Drawn in cave_walls.py's three values in the same order, so the cliff is the
caverns' stone and not a prop set down on it: the floor round it is the lightest, the face sits under it
in the stone row's shade with wandering fractures, and the top of the rock is the darkest thing on screen
- lumps in the row's deep, each lit on its upper left. A lit lip where the top turns down. Each mouth is a
ragged, lopsided round-headed opening at the foot of the face, worn by water rather than cut: black inside
(occlusion) with the throat's far wall a step up from black, its crown and right-hand jamb catching the key
and its left in shadow, and a worn step of pale stone across its sill where the water ran out.

NOTHING HERE IS LIGHT. The voice and the leaves are the event's (code: the cold breath out of a mouth on
its beat, the written leaves from hint-cues.png, and the warm glint on the true mouth's leaves). The rock
itself never glows, so a lit mouth is always a mouth speaking.

Every mouth faces the viewer. The crescent bows away to the north in the middle, so the nine stand at nine
depths and the end mouths step forward. A first draft drew one block of rock per mouth and stood nine of
them side by side: each block's lit west flank and shadowed east one made a seam between every pair, and
with their lumpy crowns they read as a row of gatehouses. One painting cut into strips has no seams.
Authored 1:1 and drawn by the game at x2.
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
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "mouths")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
PREVIEW = os.path.join(HERE, "_nine_mouths_preview.png")


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


ST = ramp("stone")
WATER = bl.ELEMENTS["water"]

BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]

# THE CRESCENT, in world px from the site's middle (NineMouthsEvent.MouthAt - change one, change both):
# mouth i at x = (i - 4) * SPACING, its foot at y = -DEPTH * (1 - u^2), u = (i - 4) / 4. The painting is at
# half that (art px, drawn x2).
SPACING = 60
DEPTH = 120
COUNT = 9

IW, IH = 300, 116
OX, OY = IW // 2, 112      # the site's middle in the painting; the end mouths' feet are on row OY
ROCK_H = 44                # how tall the cliff stands over its foot, art px
LIP_DOWN = 15              # how far below the top the lip is
MOUTH_HALF = 7             # the opening: about 15 art px wide at its sill
MOUTH_H = 22               # and 22 tall
END = 4 * SPACING // 2 + 20      # art px either side of the middle where the rock ends


def hashed(x, y, salt=0):
    h = (x * 73856093) ^ (y * 19349663) ^ (salt * 83492791)
    h = (h ^ (h >> 13)) * 1274126177
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF) / float(0x1000000)


def mouth_at(i):
    """Mouth i's foot, in world px from the cliff's origin: on the 2-unit grid, so its sill is a whole art
    row (NineMouthsEvent.MouthOffset rounds the same way)."""
    u = (i - 4) / 4.0
    return (i - 4) * SPACING, -2.0 * round(DEPTH / 2.0 * (1.0 - u * u))


def foot_row(ix):
    """The cliff's foot under painting column ix: the crescent, carried a little past the end mouths."""
    u = (ix - OX) * 2.0 / (4.0 * SPACING)
    return OY - (DEPTH / 2.0) * (1.0 - u * u)


def top_row(ix):
    """The crown of the rock above column ix: lumpy, low-frequency, slumping down to the floor at the ends."""
    d = abs(ix - OX)
    wob = 2.6 * math.sin(ix * 0.21) + 1.8 * math.sin(ix * 0.47 + 1.3) + 1.1 * math.sin(ix * 1.07)
    h = ROCK_H + wob
    if d > END - 14:
        h -= (d - (END - 14)) * 1.6
    return foot_row(ix) - h


def lip_row(ix):
    return top_row(ix) + LIP_DOWN + 1.3 * math.sin(ix * 0.53) + 0.8 * math.sin(ix * 1.31 + 0.4)


def mouth_shape(ix, iy):
    """Which mouth's opening (ix, iy) is in, or -1. Ragged, lopsided round-headed holes, wider at the sill,
    their heads leaning a pixel toward the middle of the crescent - worn by water, not cut."""
    for i in range(COUNT):
        mx, my = mouth_at(i)
        cx = OX + mx / 2.0
        fy = OY + my / 2.0
        dy = fy - (iy + 0.5)                  # height above the sill
        if dy < 0 or dy > MOUTH_H + 1:
            continue
        lean = (4 - i) * 0.25 * (dy / MOUTH_H)
        half = MOUTH_HALF + 0.9 * (1.0 - dy / MOUTH_H) + 0.7 * (hashed(int(iy), i, 601) - 0.5)
        dx = (ix + 0.5) - (cx + lean)
        head = MOUTH_H - half
        if dy <= head:
            if abs(dx) <= half:
                return i
        elif dx * dx + ((dy - head) * 1.15) ** 2 <= half * half:
            return i
    return -1


def dome(c, rock, cx, cy, r):
    """A lump of the rock's top, seen from above: lit on its upper left, the deep of the stone row, falling
    into occlusion on its lower right (cave_walls.dome's shading)."""
    for y in range(int(cy - r) - 1, int(cy + r) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            if (x, y) not in rock:
                continue
            nx = (x + 0.5 - cx) / r
            ny = (y + 0.5 - cy) / (r * 0.85)
            d2 = nx * nx + ny * ny
            if d2 > 1.0:
                continue
            light = (-nx - ny) * 0.55 + math.sqrt(max(0.0, 1.0 - d2)) * 0.45
            th = (BAYER[y % 4][x % 4] + 0.5) / 16.0
            if d2 > 0.82 and light < 0.1:
                tone = OCC
            elif light > 0.55 + th * 0.25:
                tone = ST[3]
            elif light < -0.15 - th * 0.2:
                tone = OCC if d2 > 0.6 else ST[4]
            else:
                tone = ST[4]
            c.set(x, y, tone)


def cliff():
    """The whole crescent, one painting: the event cuts it into nine strips, one per mouth."""
    c = raster.Canvas(IW, IH)
    rock = set()
    for ix in range(OX - END, OX + END + 1):
        t, f = top_row(ix), foot_row(ix)
        for iy in range(int(math.ceil(t)), int(f) + 1):
            rock.add((ix, iy))
    top = set(p for p in rock if p[1] < lip_row(p[0]))

    # THE TOP: the stone row's deep, lumps on it, its north edge lit where it turns out of sight.
    for p in top:
        c.set(p[0], p[1], ST[4])
    lumps = []
    for k in range(26):
        lx = OX - END + 4 + hashed(k, 1, 611) * (2 * END - 8)
        r = 4.0 + hashed(k, 2, 612) * 4.0
        ly = top_row(int(lx)) + 3 + hashed(k, 3, 613) * (LIP_DOWN - 6)
        lumps.append((lx, ly, r))
    for (lx, ly, r) in sorted(lumps, key=lambda q: q[1]):
        dome(c, top, lx, ly, r)
    for (x, y) in top:
        if (x, y - 1) not in rock:
            c.set(x, y, ST[1] if hashed(x // 2, 1, 621) < 0.55 else ST[2])

    # THE FACE: the stone row's shade, sinking to its deep toward the foot, strata and wandering fractures.
    for (x, y) in rock:
        if (x, y) in top:
            continue
        f = foot_row(x)
        sink = max(0.0, (y - (f - 16)) / 16.0)
        th = (BAYER[(y // 2) % 4][(x // 2) % 4] + 0.5) / 16.0
        c.set(x, y, ST[4] if th < sink * 0.8 else ST[3])
    for k in range(2):
        for x in range(OX - END, OX + END + 1):
            y = int(round(foot_row(x) - 12 - 11 * k + 1.5 * math.sin(x * 0.17 + k)))
            if (x, y) in rock and (x, y) not in top and mouth_shape(x, y) < 0:
                h = hashed(x // 3, k, 631)
                if h < 0.5:
                    c.set(x, y, ST[4])
                elif h > 0.86:
                    c.set(x, y, ST[2])
    for j in range(17):
        x = OX - END + 6 + (j + hashed(j, 1, 641)) * ((2 * END - 12) / 17.0)
        start = int(lip_row(int(x)) + 2 + hashed(j, 2, 642) * 4)
        end = int(foot_row(int(x)) - 3 - hashed(j, 3, 643) * 14)
        for y in range(start, end):
            x += (hashed(y // 2, j, 644) - 0.5) * 1.3
            ix = int(round(x))
            if any(mouth_shape(ix + d, y) >= 0 for d in (-1, 0, 1)) or (ix, y) not in rock:
                continue
            c.set(ix, y, OCC)
            if (ix + 1, y) in rock:
                c.set(ix + 1, y, ST[2] if (y - start) % 6 else ST[1])
            if (ix - 1, y) in rock:
                c.set(ix - 1, y, ST[4])

    # THE LIP: the top turning down into the face, lit, a shadow under it.
    for x in range(OX - END, OX + END + 1):
        y = int(round(lip_row(x)))
        if (x, y) in rock:
            c.set(x, y, ST[1] if hashed(x // 2, 2, 651) < 0.5 else ST[2])
        if (x, y + 1) in rock:
            c.set(x, y + 1, ST[2])
        if (x, y + 2) in rock and mouth_shape(x, y + 2) < 0:
            c.set(x, y + 2, OCC)

    # THE ENDS: the west one catching the key down its slump, the east one in shadow.
    for y in range(IH):
        xs = [x for x in range(IW) if (x, y) in rock]
        if not xs:
            continue
        if (xs[0], y) not in top:
            c.set(xs[0], y, ST[2])
        c.set(xs[-1], y, OCC)

    # THE MOUTHS: black inside, the crown and the right-hand jamb catching the key, the left in shadow, the
    # throat's far wall a step up from black so each is a passage and not a hole in the picture.
    holes = {}
    for (x, y) in rock:
        i = mouth_shape(x, y)
        if i >= 0:
            holes[(x, y)] = i
            c.set(x, y, OCC)
    for (x, y) in rock:
        if (x, y) in holes:
            continue
        near = [holes[(x + dx, y + dy)] for dx in (-1, 0, 1) for dy in (-1, 0, 1) if (x + dx, y + dy) in holes]
        if not near:
            continue
        mx, my = mouth_at(near[0])
        cx = OX + mx / 2.0
        if (x, y + 1) in holes:
            c.set(x, y, ST[1] if hashed(x, y, 661) < 0.6 else ST[2])
        elif x > cx:
            c.set(x, y, ST[1] if y % 3 == 0 else ST[2])
        else:
            c.set(x, y, ST[4])
    for i in range(COUNT):
        mx, my = mouth_at(i)
        cx, fy = OX + int(mx / 2), OY + int(my / 2)
        for y in range(fy - MOUTH_H + 9, fy - 2):
            for x in range(cx - 2, cx + 3):
                if holes.get((x, y)) == i and BAYER[y % 4][x % 4] < 3:
                    c.set(x, y, ST[4])
        # THE SILL: a worn step of pale stone across the foot of the opening, where the water ran out.
        for x in range(cx - MOUTH_HALF - 1, cx + MOUTH_HALF + 2):
            c.set(x, fy, ST[2] if x < cx + MOUTH_HALF - 1 else ST[3])
            c.set(x, fy - 1, ST[1] if x < cx - 2 else ST[2])

    # The contact line the whole cliff stands on, and grit at its foot.
    for x in range(OX - END, OX + END + 1):
        f = int(foot_row(x))
        if (x, f) in rock:
            c.set(x, f + 1, OCC)
    for k in range(30):
        gx = OX - END + 3 + int(hashed(k, 1, 671) * (2 * END - 6))
        gy = int(foot_row(gx))
        if mouth_shape(gx, gy - 1) >= 0 or mouth_shape(gx, gy - 3) >= 0:
            continue
        c.set(gx, gy, ST[2])
        c.set(gx + 1, gy, ST[4])
    return c


def strip_bounds(i):
    """Mouth i's strip of the painting: (x0, x1) art columns, halfway to its neighbours, the ends out to the
    rock's ends. NineMouthsEvent cuts the same strips (StripRect)."""
    x0 = OX - END if i == 0 else OX + (i - 4) * SPACING // 2 - SPACING // 4
    x1 = OX + END + 1 if i == COUNT - 1 else OX + (i - 4) * SPACING // 2 + SPACING // 4
    return x0, x1


# --- the pictogram ------------------------------------------------------------------------------

def glyph():
    """A band of rock with three round-headed mouths, the middle one deepest: "the mouths"."""
    c = raster.Canvas(12, 12)
    lit, mid = WATER[1], WATER[2]
    mask = set()
    for y in range(2, 11):
        for x in range(0, 12):
            if y == 2 and (x < 2 or x > 9):
                continue
            if y == 3 and (x < 1 or x > 10):
                continue
            mask.add((x, y))
    for (mx, top) in ((2, 6), (5, 4), (8, 6)):
        for y in range(top, 11):
            for x in (mx, mx + 1):
                mask.discard((x, y))
    for (x, y) in mask:
        c.set(x, y, lit if (x, y - 1) not in mask or (x - 1, y) not in mask else mid)
    for y in range(12):
        for x in range(12):
            if (x, y) in mask:
                continue
            if any((x + dx, y + dy) in mask for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
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


def main():
    for d in (OUT_WORLD, OUT_UI):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True
    c = cliff()
    ok = _audit("cliff", c) and ok
    c.save(os.path.join(OUT_WORLD, "mouths-cliff.png"))
    g = glyph()
    ok = _audit("glyph mouths", g) and ok
    g.scaled(2).save(os.path.join(OUT_UI, "poi-mouths.png"))

    # The preview: the cliff at x2 over the cave floor, the strip cuts marked under its foot, a threshold
    # marked in front of each mouth (NineMouthsEvent.ThresholdAhead), and the pictogram.
    z = 2
    canvas = Image.new("RGBA", (IW * z + 120, IH * z + 160), raster.rgb(ST[2]))
    canvas.alpha_composite(c.scaled(z), (40, 40))
    for i in range(COUNT):
        x0, x1 = strip_bounds(i)
        for yy in range(6):
            canvas.putpixel((40 + x0 * z, 40 + IH * z + 4 + yy), raster.rgb(WATER[3]))
        mx, my = mouth_at(i)
        tx, ty = 40 + (OX * z + int(mx)), 40 + (OY * z + int(my)) + 26
        for a in range(0, 360, 20):
            px = tx + int(26 * math.cos(math.radians(a)))
            py = ty + int(26 * math.sin(math.radians(a)) * 0.6)
            canvas.putpixel((px, py), raster.rgb(WATER[2]))
    canvas.alpha_composite(g.scaled(4), (IW * z + 60, 20))
    canvas.convert("RGB").save(PREVIEW)

    print("  mouths cliff %dx%d, glyph poi-mouths" % (IW, IH))
    if not ok:
        return 1
    print("nine mouths written, palette clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
