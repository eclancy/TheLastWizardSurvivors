# -*- coding: utf-8 -*-
"""The Frozen Waste's ground detail. Drawn by tools/art/ground_detail.py, which finds this module by its
name and appends DETAIL_SETS to its own - run ground_detail.py, never this. GroundDetail.cs's Ice block
is what asks for it, and where.

The Frozen Waste stands on snow now (tiles.py, snow()), and snow is almost featureless by design: a
ridge drawn into a fill tile repeats every cell and reads as a printed carpet. The wind, which is
what says "a waste" rather than "a pale floor", is drawn here instead and laid in world space in
drifts:

  sastrugi    ridges the wind has cut in packed snow - three to five, lying along the wind (off the
              north-west, the same lean as the floor's own odd ridge), each a lit crest of the bone
              row's shade over a body of steel's shade and a lee in steel's deep. Stepped back from
              one another like waves, not ruled like a hatch.

The ground_detail.py rules hold: material tones only, nothing emissive, nothing round, bright and
alone; the brightest stop here is the bone row's shade, which is a step under the `snow` drift set's.

THE FOLIAGE PASS'S BOTTOM RUNGS (2026-11-04, .ai/terrain.md section 13, "The Frozen Waste"). Laid as CLUTTER
by the chapter's canopy (Foliage.FrozenWaste.cs) - the open waste, a grove's fringe, the grove:

  frost_stalks    dead grass and sedge through the snow, three to six crooked stalks, frost on their tips:
                  the open waste and a fringe
  buried_stones   the rounded tops of two to four stones showing through the snow, each with its cap and
                  the scoop the wind cut in its lee: a fringe and the groves
  needle_litter   what a snow-loaded spruce drops: needles, a twig, a cone, dark on the snow under a grove
  bough_snow      clumps of snow fallen off the boughs, pocking the floor under a grove

All of it in the snow's own stops and the dark rows beside them, within about twenty L* of the floor, the
stone in the boulders' grey-green granite (props_frozen.py). NOTHING HERE IS A SECRET'S TELL: no footprint
and no track (I1), no flat or cut stone (I2's rune-stones are flat slabs; these are rounded and plain), no
drift (I2's far tell), nothing warm (I1, I3), no clear ice (I4). The clumps of bough snow lie in a scatter,
never in a line.
"""
import math

import ground_detail
from ground_detail import SK, STL, LN, VE, FL, GO, OCC, spot


def sastrugi(c, r):
    """A patch of wind ridges: each a shallow arc rising a little to the east, crest lit over its lee."""
    cx, cy = spot(r, 14)
    n = r.i(3, 4)
    for k in range(n):
        run = r.i(12, 18)
        # staggered along the wind, not stacked: stacked, a patch read as a printed equals sign
        x0 = cx - run // 2 + (k - n // 2) * r.i(4, 6) + r.i(-3, 3)
        y0 = cy + (k - n // 2) * r.i(5, 8)
        for j in range(run):
            u = j / float(run - 1)
            x = x0 + j
            y = y0 + int(round(-0.18 * j - math.sin(u * math.pi) * (1.2 + 0.6 * (k % 2))))
            # a crest thins to nothing at both ends, which is what keeps it from reading as a stroke
            if 0.3 < u < 0.7:
                c.set(x, y - 1, SK[3])
            c.set(x, y, STL[3])
            if 0.08 < u < 0.92:
                c.set(x, y + 1, STL[4])


def inside(x, y, m=2):
    return m <= x < ground_detail.SIZE - m and m <= y < ground_detail.SIZE - m


def scattered(r, cx, cy, n, spread_x, spread_y, gap):
    """Up to n points round (cx, cy), no two within `gap` of each other - so nothing here piles up on itself
    (two stones one over the other would be a cairn, the chapter's wayfinder)."""
    out = []
    for k in range(n * 6):
        if len(out) >= n:
            break
        x = cx + r.i(-spread_x, spread_x)
        y = cy + r.i(-spread_y, spread_y)
        if all((x - a) ** 2 + (y - b) ** 2 >= gap * gap for (a, b) in out):
            out.append((x, y))
    return out


def frost_stalks(c, r):
    """Dead grass and sedge through the snow: a tuft of five to nine crooked stalks fanned from one root, six to
    twelve pixels tall, linen's shade and gold's deep, bent where the wind broke them, a pixel of the bone row's
    shade where frost caught a tip, and the scoop of steel's deep the wind cut at the root. One or two tufts."""
    for k in range(r.i(1, 2)):
        x, y = spot(r, 12)
        y = max(y, 18)
        n = r.i(5, 9)
        for b in range(n):
            side = b - (n - 1) / 2.0
            ln = max(4, r.i(6, 12) - int(abs(side)))
            bend = (1 if side >= 0 else -1) * r.i(0, 2)
            for j in range(ln):
                t = j / float(ln)
                xx = x + int(round(side * (0.4 + 1.6 * t))) + (bend if t > 0.65 else 0)
                yy = y - j
                if not inside(xx, yy):
                    break
                col = LN[3] if (b + j // 2) % 3 else GO[4]
                if j == ln - 1 and r.f() < 0.5:
                    col = SK[3]
                c.set(xx, yy, col)
        for dx in range(-2, 4):
            if inside(x + dx, y + 1):
                c.set(x + dx, y + 1, STL[4] if dx < 3 else STL[3])


def buried_stones(c, r):
    """The rounded tops of two or three stones showing through the snow: grey-green granite lit on its upper
    left, a cap of the bone row's shade lying on it, the snow coming up round its foot, and a scoop of steel's
    deep in its lee. Rounded, uneven and plain - never a slab, never cut."""
    cx, cy = spot(r, 13)
    for k, (x0, y0) in enumerate(scattered(r, cx, cy, r.i(2, 3), 10, 8, 11)):
        rx = r.i(3, 5) + 0.5
        ry = r.i(2, 3) + 0.5
        tilt = (r.f() - 0.5) * 0.6
        for dy in range(-int(ry) - 1, 1):
            for dx in range(-int(rx) - 1, int(rx) + 2):
                d = (dx / rx) ** 2 + ((dy - tilt * dx) / ry) ** 2
                if d > 1.0 or (d > 0.8 and (dx * 7 + dy * 3 + k) % 3 == 0):
                    continue
                xx, yy = int(x0 + dx), int(y0 + dy)
                if not inside(xx, yy):
                    continue
                lit = -dx * 0.6 - dy * 0.8
                col = LN[2] if lit > 1.5 else (LN[3] if lit > -1.0 else LN[4])
                if dy - tilt * dx < -ry * 0.45 and r.f() < 0.85:
                    col = SK[3] if dx < rx * 0.3 else STL[3]       # the snow lying on its top
                c.set(xx, yy, col)
        # the snow round its foot, and the scoop in its lee
        for dx in range(-int(rx), int(rx) + 3):
            xx, yy = int(x0 + dx), int(y0 + 1)
            if inside(xx, yy):
                c.set(xx, yy, STL[4] if dx > -1 else STL[3])


def needle_litter(c, r):
    """What a snow-loaded spruce drops on the snow under it: two or three sprigs - a twig of flesh's deep with
    its needles in verdant's deep paired along it - and now and then a cone, gold's deep and the occlusion."""
    cx, cy = spot(r, 12)
    for k in range(r.i(2, 3)):
        x = cx + r.i(-9, 7)
        y = cy + r.i(-8, 8)
        ln = r.i(6, 10)
        slope = (r.f() - 0.5) * 0.9
        for j in range(ln):
            xx, yy = x + j, y + int(round(j * slope))
            if not inside(xx, yy, 3):
                break
            c.set(xx, yy, FL[4])
            if j % 2 == 1 and j < ln - 1:
                c.set(xx, yy - 1, VE[4])
                c.set(xx + 1, yy + 1, VE[4] if r.f() < 0.7 else OCC)
            if j == ln - 1:
                c.set(xx + 1, yy, VE[4])
    if r.f() < 0.6:
        x, y = cx + r.i(-6, 6), cy + r.i(-5, 5)
        if inside(x - 1, y) and inside(x + 2, y + 4):
            for (dx, dy, col) in ((0, 0, GO[4]), (1, 0, FL[4]), (0, 1, FL[4]), (1, 1, GO[4]), (0, 2, GO[4]),
                                  (1, 2, FL[4]), (0, 3, FL[4]), (1, 3, OCC), (2, 3, OCC), (2, 2, OCC)):
                c.set(x + dx, y + dy, col)


def bough_snow(c, r):
    """Clumps of snow that fell off the boughs: three to five lumps in a scatter, each four to seven pixels
    across, lit on its top in the bone row's shade, its body steel's shade, a line of steel's deep under it -
    and never in a line."""
    cx, cy = spot(r, 13)
    for (x, y) in scattered(r, cx - 3, cy, r.i(3, 5), 11, 9, 9):
        w = r.i(4, 7)
        if not (inside(x, y - 2) and inside(x + w + 1, y + 2)):
            continue
        for dx in range(w):
            edge = dx == 0 or dx == w - 1
            if not edge:
                c.set(x + dx, y - 1, SK[3] if dx < w * 0.6 else STL[3])
            c.set(x + dx, y, SK[3] if dx < w * 0.45 else STL[3])
            c.set(x + dx, y + 1, STL[3] if not edge else STL[4])
        for dx in range(1, w + 1):
            c.set(x + dx, y + 2, STL[4])


DETAIL_SETS = [
    ("sastrugi", sastrugi, 4),
    ("frost_stalks", frost_stalks, 4),
    ("buried_stones", buried_stones, 4),
    ("needle_litter", needle_litter, 3),
    ("bough_snow", bough_snow, 3),
]
