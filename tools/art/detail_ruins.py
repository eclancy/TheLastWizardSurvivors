# -*- coding: utf-8 -*-
"""The Mystic Ruins' ground detail: the bottom rungs of its foliage pass (.ai/terrain.md section 13, "The
Mystic Ruins"). Not run on its own: ground_detail.py finds this module (chapter_detail_modules) and appends
its DETAIL_SETS to its own, so a full `python tools/art/ground_detail.py` draws them and lists them in
detail.json, and the chapter's clutter rules (Foliage.MysticRuins.cs) ask for them by name.

Five small things lying on the flags of a ruined seat of learning, laid as CLUTTER by the chapter's canopy -
the open flags, the edge of an overgrown quarter, the quarter itself:

  cold_ash      "Dust and cold ash" (the chapter's note): a soft drift of it in the lee of nothing, in the open
  crack_weeds   a crack across a flag with weeds come up along it, everywhere and thickest at an edge
  moss_chips    chips of the walls' stone, mossed over, where the ruin has started to crumble into its growth
  ivy_runners   ivy creeping flat across the flags from the overgrown quarters, leaf pairs on a wandering stem
  root_runs     a root from the trees in the stones running over the flags and prising one up

THE GUARDRAIL (genre-lessons 2 and 5). The floor is the stone row's shade (L* 9) and the cast stands well
ABOVE it - their stone in stone.hi and stone.lit, their carved stone in the bone row - so everything here
is in the floor's own row and the two stops over it (stone base and lit: +6 and +12), lichen's shade and
deep, verdant's shade and deep with its base a leaf at a time, and grave linen's dark half: within about
twenty L* of the floor at its brightest pixel.

NOT A TELL. No carved or notched fragment and nothing pale (the Broken Ward's far tell is pale fragments of
ward stone with notches cut in them, commoner toward the ring), no paper (the Misfiled Page, the Loose
Folio), nothing warm or glinting (a warm glint is a secret's), and no flag laid square: what lies here is
loose and broken, never paving (the integrator sent a ground of evenly tiled stones back for reading as
paving).
"""
from __future__ import division

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import ground_detail as GDT

OCC = GDT.OCC
ST = GDT.ST
LI = GDT.LI
VE = GDT.VE
LN = GDT.LN
spot = GDT.spot
TAU = math.pi * 2.0


def inside(x, y, m=2):
    return m <= x < GDT.SIZE - m and m <= y < GDT.SIZE - m


def put(c, x, y, col, m=2):
    if inside(x, y, m):
        c.set(x, y, col)


def cold_ash(c, r):
    """A drift of dust and cold ash: two or three soft lobes, dense in the middle and dithered out at the
    rim, in the stone row's base with lit grains on the side the key finds, and grave linen's shade through
    it - grey ash on blue stone, low and flat, gone at its edge into the floor."""
    cx, cy = spot(r, 14)
    for k in range(r.i(2, 3)):
        lx, ly = cx + r.i(-6, 6), cy + r.i(-3, 3)
        rx, ry = r.i(5, 9), r.i(2, 4)
        for dy in range(-ry, ry + 1):
            for dx in range(-rx, rx + 1):
                d = (dx / float(rx)) ** 2 + (dy / float(ry)) ** 2
                if d > 1.0:
                    continue
                if d > 0.35 and (dx + 2 * dy + k) % 2:
                    continue
                if d > 0.7 and (dx * 3 + dy) % 3:
                    continue
                tone = ST[2]
                if r.f() < 0.22:
                    tone = LN[3]
                if d < 0.3 and dx + dy < -1 and r.f() < 0.4:
                    tone = ST[1]
                put(c, lx + dx, ly + dy, tone)


def crack_weeds(c, r):
    """A crack across a flag, occlusion with the stone row's deep on its lip, and weeds come up along it in
    two or three places: short blades in lichen's shade and verdant's shade, a darker one behind."""
    x, y = spot(r, 12)
    ang = r.f() * TAU
    px, py = float(x), float(y)
    path = []
    for j in range(r.i(12, 20)):
        if j % 3 == 0:
            ang += (r.f() - 0.5) * 1.4
        px += math.cos(ang)
        py += math.sin(ang) * 0.7
        ix, iy = int(px), int(py)
        if not inside(ix, iy, 3):
            break
        path.append((ix, iy))
        c.set(ix, iy, OCC)
        put(c, ix, iy - 1, ST[4])
    if not path:
        return
    for k in range(r.i(2, 3)):
        wx, wy = path[r.i(0, len(path) - 1)]
        for b in range(r.i(4, 6)):
            side = b - 2
            h = r.i(3, 6) - abs(side) // 2
            for j in range(h):
                xx = wx + (side * (j + 1)) // 3
                put(c, xx, wy - j - 1, VE[3] if (b % 2 == 0 and j == h - 1) else (LI[3] if b % 2 else VE[4]))
        put(c, wx + 1, wy, OCC)


def moss_chips(c, r):
    """Three to five chips of the walls' stone, angular, each lit along its top-left arris on a pixel of
    occlusion, and mossed: a cap of lichen's shade on its top, its deep down one side."""
    cx, cy = spot(r, 13)
    for k in range(r.i(3, 5)):
        x = cx + r.i(-9, 9)
        y = cy + r.i(-7, 7)
        w = r.i(3, 5)
        h = r.i(2, 3)
        cut = r.i(0, 1)
        for dy in range(h):
            for dx in range(w - (cut if dy == 0 else 0)):
                col = ST[2]
                if dy == 0:
                    col = ST[1] if dx < w - 2 else ST[2]
                elif dx == w - 1 or dy == h - 1:
                    col = ST[3]
                put(c, x + dx, y + dy, col)
        for dx in range(w + 1):
            put(c, x + dx, y + h, OCC)
        put(c, x + w, y + h - 1, OCC)
        # the moss on it
        for dx in range(r.i(2, w)):
            put(c, x + dx, y - 1 if dx % 3 == 1 else y, LI[3])
        put(c, x, y + 1, LI[4])
        if r.f() < 0.5:
            put(c, x - 1, y + 1, LI[4])


def ivy_runners(c, r):
    """Ivy creeping flat across the flags: a stem in verdant's deep wandering over two thirds of the cell,
    pairs of leaves along it in verdant's shade with their shadow under them, one leaf now and then catching
    the key in verdant's base."""
    x, y = spot(r, 10)
    ang = r.f() * TAU
    px, py = float(x), float(y)
    for j in range(r.i(16, 26)):
        if j % 4 == 0:
            ang += (r.f() - 0.5) * 1.2
        px += math.cos(ang)
        py += math.sin(ang) * 0.6
        ix, iy = int(px), int(py)
        if not inside(ix, iy, 4):
            break
        put(c, ix, iy, VE[4])
        if j % 3 == 1:
            for side in (-1, 1):
                if r.f() < 0.75:
                    lx, ly = ix + side, iy - 1 if side < 0 else iy + 1
                    put(c, lx, ly, VE[3])
                    put(c, lx + side, ly, VE[3] if r.f() < 0.6 else VE[4])
                    put(c, lx, ly - 1, VE[2] if (side < 0 and r.f() < 0.12) else VE[3])
                    put(c, lx + side, ly + 1, OCC)


def root_runs(c, r):
    """A root out of the trees in the stones, running over the flags and thinning: grave linen's shade, its
    lit edge linen's base, its far side deep, on a line of occlusion; where it crosses a joint it has lifted
    the flag's edge, a lip of stone lit along the break."""
    x, y = spot(r, 8)
    ang = r.f() * TAU
    px, py = float(x), float(y)
    wd = 2.6
    lifted = r.i(5, 10)
    for j in range(r.i(18, 28)):
        if j % 3 == 0:
            ang += (r.f() - 0.5) * 0.9
        px += math.cos(ang)
        py += math.sin(ang) * 0.6
        ix, iy = int(px), int(py)
        if not inside(ix, iy, 4):
            break
        n = max(1, int(round(wd)))
        for k in range(n):
            col = LN[2] if k == 0 and n > 1 else (LN[4] if k == n - 1 and n > 1 else LN[3])
            put(c, ix, iy + k, col)
        put(c, ix, iy + n, OCC)
        if j == lifted:
            for d in range(-3, 4):
                put(c, ix + d, iy - 2, ST[1] if d < 0 else ST[2])
                put(c, ix + d, iy - 1, ST[3])
        wd = max(1.0, wd - 0.07)


# set name -> (draw, variants), in ground_detail.SETS' shape.
DETAIL_SETS = [
    ("cold_ash", cold_ash, 4),
    ("crack_weeds", crack_weeds, 4),
    ("moss_chips", moss_chips, 3),
    ("ivy_runners", ivy_runners, 4),
    ("root_runs", root_runs, 3),
]
