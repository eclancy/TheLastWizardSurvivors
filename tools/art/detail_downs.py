# -*- coding: utf-8 -*-
"""The White Downs' ground detail (.ai/second-half.md 4.2, F3). Drawn by tools/art/ground_detail.py, which
finds this module by its name and appends DETAIL_SETS to its own - run ground_detail.py, never this. The
chapter's GroundDetail rules and its foliage's clutter (scripts/Foliage.WhiteDowns.cs) ask for these sets.

The bottom rungs of the downs' ladder, the small things lying on sheep-cropped turf over chalk at night:

  downs_grass     the sward itself: cropped clumps a few pixels high, and now and then a longer tussock the
                  sheep have left. What makes the woven floor read as grass rather than as cloth.
  harebells       harebells nodding on hair-thin stems, two or three to a clump: violet's middle stops, so at
                  night they are barely there - a dim blue-violet a step over the turf, never a lilac dot.
  downs_thyme     wild thyme, flat cushions of tiny dark leaf hugging the turf, a dusk of violet bloom on top.
  flints          flints lying on the turf, washed out of the chalk: knobbly, pale-rinded, some broken to the
                  black glass inside.
  chalk_crumbs    crumbs and lumps of chalk turned up through the turf: by rabbits, moles, a hoof.
  rabbit_scrape   a rabbit scrape: a small hollow dug through the turf to the chalk, the spoil thrown out of it.

The ground_detail.py rules hold, and the downs add two. Material tones only, nothing emissive; the brightest
stop on any of them is the bone row's shade (L* 46) or a `lit`, never a `hi` plane; nothing round, bright and
alone (the XP orbs are lime, lilac and pale yellow discs), so the bells come in clumps on stems and the chalk in
crumbs of several sizes. And: nothing warm (no gold, flesh or red - a warm fleck is a secret's tell,
.ai/stage-secrets.md), and nothing in a line (the downs' chalk figures are secrets drawn in lines of chalk, and
no crumb, scrape or flint here may lie in a row or a stroke).
"""
import math

import ground_detail
from ground_detail import LI, LN, OCC, SK, ST, STL, VE, VI, spot


def blade_clump(c, r, x0, y0, n, hmin, hmax, lean=0.0):
    """A clump of grass blades from one root: each a short stroke rising and bending a little, linen's shade or
    lichen's on the stems, the floor's own greens at the root, a lit tip on some. The root's contact on one side."""
    for b in range(n):
        bx = x0 + r.i(-2, 2)
        ln = r.i(hmin, hmax)
        bend = (r.f() - 0.5) * 0.5 + lean
        px, py = float(bx), float(y0)
        for j in range(ln):
            t = j / float(max(1, ln - 1))
            px += bend * t
            py -= 1.0
            x, y = int(round(px)), int(round(py))
            if j == ln - 1:
                tone = LI[3] if b % 3 else LN[3]
            elif j == 0:
                tone = VE[4] if b % 2 else LI[4]
            else:
                tone = LN[3] if (b + j) % 3 == 0 else (LI[4] if b % 2 else VE[4])
            c.set(x, y, tone)
    c.set(x0, y0 + 1, OCC)
    c.set(x0 + 1, y0 + 1, OCC)


def downs_grass(c, r):
    """Cropped turf: three to five low clumps across the cell, and on some cells a longer tussock the sheep
    have left - taller, its blades bending over, a stop up at the tips."""
    cx, cy = spot(r, 12)
    for k in range(r.i(3, 4)):
        x0, y0 = cx + r.i(-11, 11), cy + r.i(-8, 9)
        blade_clump(c, r, x0, y0, r.i(4, 6), 3, 5)
        blade_clump(c, r, x0 + 2, y0 + 1, r.i(2, 4), 2, 4)
    if r.f() < 0.5:
        x0, y0 = cx + r.i(-6, 6), cy + r.i(-3, 6)
        for side in (-1, 1):
            blade_clump(c, r, x0 + side * 2, y0, r.i(4, 5), 7, 11, lean=side * 0.3)
        blade_clump(c, r, x0, y0 + 1, 4, 5, 8)


def harebells(c, r):
    """Harebells: two or three nodding bells on hair-thin stems from a wisp of leaf, the bells hanging from
    the stems' bent tips. Violet's base with a fleck of its lit on the side toward the key, its shade inside
    the mouth: a bell, not a disc, and dim - at night they are barely there."""
    cx, cy = spot(r, 11)
    for k in range(r.i(1, 2)):
        x0, y0 = cx + r.i(-7, 7), cy + r.i(-3, 6)
        # the wisp of leaf at the root
        for j in range(r.i(3, 5)):
            c.set(x0 + r.i(-2, 2), y0 - r.i(0, 1), VE[4] if j % 2 else LI[4])
        c.set(x0, y0 + 1, OCC)
        for s in range(r.i(2, 3)):
            sx = x0 + r.i(-3, 3)
            h = r.i(7, 11)
            lean = 1 if r.f() < 0.5 else -1
            for j in range(h):
                c.set(sx + (lean if j > h - 3 else 0), y0 - j, LN[3] if j % 2 else LI[4])
            # the bell, hanging from the bent tip: two pixels wide, three deep, its mouth down and dark
            bx, by = sx + lean * 2, y0 - h + 2
            c.set(bx, by, VI[1])
            c.set(bx + 1, by, VI[2])
            c.set(bx, by + 1, VI[2])
            c.set(bx + 1, by + 1, VI[2] if lean > 0 else VI[3])
            c.set(bx, by + 2, VI[3])
            c.set(bx + 1, by + 2, OCC)


def downs_thyme(c, r):
    """Wild thyme: one or two flat cushions of tiny dark leaf hugging the turf, a low mound lit along its
    upper-left edge, with a dusk of bloom over the top in violet's base and shade - dark, a texture rather than
    flowers at night."""
    cx, cy = spot(r, 12)
    for k in range(r.i(1, 2)):
        x0, y0 = cx + r.i(-6, 6), cy + r.i(-4, 5)
        rx, ry = r.i(5, 8), r.i(2, 3)
        for dy in range(-ry, ry + 1):
            for dx in range(-rx, rx + 1):
                d = (dx / float(rx)) ** 2 + (dy / float(ry + 0.5)) ** 2
                if d > 1.0 - r.f() * 0.25:
                    continue
                if dx + dy * 2 < -rx * 0.55 and d > 0.4:
                    tone = VE[3] if (dx + dy) % 2 else LI[4]       # the lit edge of the mat
                elif d > 0.75 and dx + dy > 0:
                    tone = OCC
                else:
                    tone = VE[4] if (dx * 3 + dy) % 4 else OCC
                c.set(x0 + dx, y0 + dy, tone)
        # the bloom: tiny flecks over the top half of the mat, never more than two together
        for b in range(r.i(5, 9)):
            bx, by = x0 + r.i(-rx + 2, rx - 2), y0 + r.i(-ry, 0)
            c.set(bx, by, VI[2] if b % 3 else VI[1])
            if b % 2:
                c.set(bx + 1, by, VI[3])
        for dx in range(-rx + 1, rx):
            if r.f() < 0.7:
                c.set(x0 + dx + 1, y0 + ry + 1, OCC)


def flint_stone(c, r, x, y, size, knapped):
    """One flint lying on the turf, as the downs' terrain draws them: two or three lobes run together, the pale
    cortex lit on the upper left, or broken to the black glass inside; its contact down and to the right."""
    pts = set()
    for j in range(2 + (1 if r.f() < 0.5 else 0)):
        lx = x + (r.f() - 0.5) * size * 1.6
        ly = y + (r.f() - 0.5) * size * 0.9
        rr = size * (0.55 + r.f() * 0.45)
        for yy in range(int(ly - rr) - 1, int(ly + rr) + 2):
            for xx in range(int(lx - rr) - 1, int(lx + rr) + 2):
                if (xx + 0.5 - lx) ** 2 + ((yy + 0.5 - ly) / 0.8) ** 2 <= rr * rr:
                    pts.add((xx, yy))
    for (xx, yy) in pts:
        ul = (xx - 1, yy) not in pts or (xx, yy - 1) not in pts
        lr = (xx + 1, yy) not in pts or (xx, yy + 1) not in pts
        if knapped and (xx - x) + (yy - y) > 0:
            tone = OCC if lr else (STL[3] if (xx + yy) % 3 == 0 else STL[4])
        else:
            tone = SK[3] if ul and (xx * 3 + yy) % 4 else (ST[2] if lr else SK[4])
        c.set(xx, yy, tone)
    for (xx, yy) in pts:
        if (xx, yy + 1) not in pts and (xx + 1, yy + 1) not in pts:
            c.set(xx + 1, yy + 1, OCC)


def scatter_points(r, cx, cy, n, spread, min_gap=4.0):
    """n points round (cx, cy), no two within min_gap of each other and never three in a line: each new point
    is tried until it sits off the line through any two already placed (a flint or a crumb in a row would be
    a stroke of chalk, the secrets' mark)."""
    out = []
    for k in range(n):
        for tries in range(20):
            p = (cx + r.i(-spread, spread), cy + r.i(-int(spread * 0.75), int(spread * 0.75)))
            if any(math.hypot(p[0] - q[0], p[1] - q[1]) < min_gap for q in out):
                continue
            inline = False
            for i in range(len(out)):
                for j in range(i + 1, len(out)):
                    (ax, ay), (bx, by) = out[i], out[j]
                    cross = (bx - ax) * (p[1] - ay) - (by - ay) * (p[0] - ax)
                    if abs(cross) < 1.5 * math.hypot(bx - ax, by - ay):
                        inline = True
            if not inline:
                out.append(p)
                break
    return out


def flints(c, r):
    """Two to five flints lying on the turf, of different sizes, every other one broken to its black heart -
    which is what tells a flint from a crumb of chalk at a glance."""
    cx, cy = spot(r, 12)
    pts = scatter_points(r, cx, cy, r.i(2, 5), 11, 5.0)
    for k, (x, y) in enumerate(pts):
        flint_stone(c, r, x, y, (2.4 + r.f() * 1.0) if k == 0 else (1.4 + r.f() * 1.0), knapped=(k % 2 == 0))


def chalk_lump(c, r, x, y, size):
    """A crumb of chalk: bone's deep, its upper left in bone's shade, its lower right in steel's deep, and its
    contact. Irregular - a crumb, not a pebble and never a disc."""
    pts = {}
    rx, ry = size, size * (0.6 + r.f() * 0.3)
    for yy in range(int(y - ry) - 1, int(y + ry) + 2):
        for xx in range(int(x - rx) - 1, int(x + rx) + 2):
            dx, dy = (xx + 0.5 - x) / rx, (yy + 0.5 - y) / ry
            if dx * dx + dy * dy <= 1.0 - r.f() * 0.35:
                pts[(xx, yy)] = (dx, dy)
    for (xx, yy), (dx, dy) in pts.items():
        tone = SK[3] if dx + dy < -0.6 else (STL[4] if dx + dy > 0.5 else SK[4])
        c.set(xx, yy, tone)
    for (xx, yy) in pts:
        if (xx, yy + 1) not in pts and (xx + 1, yy + 1) not in pts:
            c.set(xx + 1, yy + 1, OCC)


def chalk_crumbs(c, r):
    """Chalk turned up through the turf: one lump and a scatter of crumbs round it, of several sizes, and a
    few single grains - never in a row."""
    cx, cy = spot(r, 11)
    pts = scatter_points(r, cx, cy, r.i(3, 6), 10, 3.5)
    for k, (x, y) in enumerate(pts):
        chalk_lump(c, r, x, y, (2.4 + r.f() * 0.8) if k == 0 else (0.9 + r.f() * 1.1))
    for k in range(r.i(3, 6)):
        x, y = cx + r.i(-12, 12), cy + r.i(-8, 8)
        c.set(x, y, SK[4])
        c.set(x + 1, y + 1, STL[4])


def rabbit_scrape(c, r):
    """A rabbit scrape: a small hollow dug through the turf to the chalk - its near (upper-left) wall in shadow,
    its far wall catching the key, chalk in the bottom - with the spoil thrown out of it in a spray on one
    side, and the torn turf's edge round its lip."""
    cx, cy = spot(r, 13)
    rx, ry = r.i(5, 7), r.i(3, 4)
    side = 1 if r.f() < 0.5 else -1
    hole = {}
    for dy in range(-ry - 1, ry + 2):
        for dx in range(-rx - 1, rx + 2):
            d = (dx / float(rx)) ** 2 + (dy / float(ry)) ** 2
            if d <= 1.0 - r.f() * 0.15:
                hole[(cx + dx, cy + dy)] = (dx / float(rx), dy / float(ry), d)
    for (x, y), (nx, ny, d) in hole.items():
        if d > 0.55:
            # the walls: the near one in the hollow's own shadow, the far one lit
            tone = OCC if nx + ny < -0.2 else (SK[4] if nx + ny > 0.5 else LN[4])
        else:
            tone = STL[4] if nx + ny < 0 else SK[4]          # the chalk in its bottom
        c.set(x, y, tone)
    # the torn turf round the lip: a ring of the floor's greens a stop up, broken
    for (x, y), (nx, ny, d) in hole.items():
        for (ex, ey) in ((0, -1), (-1, 0)):
            if (x + ex, y + ey) not in hole and r.f() < 0.6:
                c.set(x + ex, y + ey, LI[3] if (x + y) % 2 else VE[3])
    # the spoil, thrown out to one side in a spray of crumbs, thinning with distance
    for k in range(r.i(10, 16)):
        t = r.f()
        x = cx + side * (rx + 1 + int(t * 9))
        y = cy + r.i(-ry - 1, ry + 1) + int(t * 2)
        if (x, y) in hole:
            continue
        c.set(x, y, SK[3] if k % 4 == 0 else SK[4])
        if t < 0.5:
            c.set(x + 1, y, SK[4])
            c.set(x + 1, y + 1, STL[4])
        c.set(x, y + 1, OCC if k % 3 == 0 else STL[4])


DETAIL_SETS = [
    ("downs_grass", downs_grass, 4),
    ("harebells", harebells, 4),
    ("downs_thyme", downs_thyme, 3),
    ("flints", flints, 4),
    ("chalk_crumbs", chalk_crumbs, 4),
    ("rabbit_scrape", rabbit_scrape, 3),
]
