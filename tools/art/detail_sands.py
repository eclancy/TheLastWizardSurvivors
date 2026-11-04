# -*- coding: utf-8 -*-
"""The Scorched Sands' ground detail: the bottom rungs of its foliage pass (.ai/terrain.md section 13, "The
Scorched Sands"). Not run on its own: ground_detail.py finds this module (chapter_detail_modules) and appends
its DETAIL_SETS to its own, so a full `python tools/art/ground_detail.py` draws them and lists them in
detail.json, and the chapter's clutter rules (Foliage.ScorchedSands.cs) ask for them by name.

Five small things a stony desert has lying on it, laid as CLUTTER by the chapter's canopy - the open sand,
an outcrop's fringe, its scree:

  sand_ripples    ripple marks the wind leaves in loose sand, out in the open
  drift_tail      a pebble with the tail of sand the wind piled behind it, all pointing one way
  desert_gravel   a patch of small dark stones: the desert's pavement, where the sand has blown off
  rock_chips      angular chips of the outcrops' red stone, on their scree and their fringe
  bunchgrass      a low dry tussock or two, on a fringe where the rock keeps a little shade

THE WIND HAS ONE DIRECTION. The outcrops lie in belts along it (Foliage.ScorchedSands.cs, WindAngle), the
ripples cross it and the drift tails trail down it, so the ground agrees about where the wind comes from. The
crests run at right angles to the belts; the tails point along them. WIND_DX/WIND_DY are that direction.

THE GUARDRAIL (genre-lessons 2 and 5). The floor is gold's deep (L* 14) with gold's shade and occlusion in its
own marks, and the cast stands ABOVE it (the light probe: bodies dL* +4 to +18, their brightest tenth +48 to
+65). So everything here is drawn in the floor's own stops and the ones beside them - gold's shade, flesh's
shade and deep, linen's shade, occlusion - within about ten L* of the sand; gold's base only as a single grain
on a crest, never a plane, and nothing bone-pale or cold (the Fulgurite's glass beads are the far tell of a
secret, and are the only pale cold grains on this floor). No cracked earth added (the Hidden Water's
gradient starts there), no green, no reed.
"""
from __future__ import division

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import ground_detail as GDT

OCC = GDT.OCC
GO = GDT.GO
FL = GDT.FL
LN = GDT.LN
spot = GDT.spot

# The wind, as a unit vector in the image (x right, y down): along the outcrop belts, rising to the right.
# Foliage.ScorchedSands.cs turns its field by the same angle (WindAngle, -0.32 rad).
WIND_ANGLE = -0.32
WIND_DX, WIND_DY = math.cos(WIND_ANGLE), math.sin(WIND_ANGLE)
# Across the wind: the line a ripple's crest runs along.
CREST_DX, CREST_DY = -WIND_DY, WIND_DX


def inside(x, y, m=2):
    return m <= x < GDT.SIZE - m and m <= y < GDT.SIZE - m


def sand_ripples(c, r):
    """Ripple marks: four to six short crests side by side, each a wavering line across the wind in gold's
    shade with its lee - the side away from the key - a pixel of flesh's deep. A patch of them, never a
    whole cell: ripples cover a scoop of loose sand and stop."""
    cx, cy = spot(r, 14)
    n = r.i(4, 6)
    for k in range(n):
        off = (k - (n - 1) / 2.0) * 3.4
        bx = cx + WIND_DX * off
        by = cy + WIND_DY * off
        half = r.i(4, 7) - abs(k - (n - 1) / 2.0)
        phase = r.f() * 6.28
        for j in range(-int(half), int(half) + 1):
            wob = math.sin(j * 0.7 + phase) * 0.7
            x = int(round(bx + CREST_DX * j + WIND_DX * wob))
            y = int(round(by + CREST_DY * j + WIND_DY * wob))
            if not inside(x, y):
                continue
            c.set(x, y, GO[3])
            # the lee, on the far side of the crest from the key light
            c.set(x + 1, y, FL[4])
        if r.f() < 0.4:
            x = int(round(bx - CREST_DX * half * 0.4))
            y = int(round(by - CREST_DY * half * 0.4))
            if inside(x, y):
                c.set(x, y, GO[2])                      # one grain catching the key


def drift_tail(c, r):
    """Two or three pebbles, each with the tail of sand the wind piled in its lee: a pebble of flesh's deep
    lit on its key side, and a narrowing streak of gold's shade trailing down the wind from it."""
    for k in range(r.i(2, 3)):
        x, y = spot(r, 12)
        ln = r.i(6, 11)
        for j in range(1, ln):
            w = 1.0 if j < ln * 0.5 else 0.0
            px = x + WIND_DX * (j + 1)
            py = y + WIND_DY * (j + 1)
            for s in (0, 1) if w else (0,):
                xx, yy = int(round(px + CREST_DX * s * 0.8)), int(round(py + CREST_DY * s * 0.8))
                if inside(xx, yy) and (j < ln - 2 or j % 2):
                    c.set(xx, yy, GO[3])
        # the pebble
        c.set(x, y, FL[4])
        c.set(x + 1, y, FL[4])
        c.set(x, y - 1, FL[3])
        c.set(x + 1, y - 1, FL[4])
        c.set(x + 1, y + 1, OCC)
        c.set(x + 2, y, OCC)


def desert_gravel(c, r):
    """Desert pavement: a patch of seven to twelve small stones gone dark in the sun, a pixel or two each on
    a pixel of occlusion, now and then one lit on its key side."""
    cx, cy = spot(r, 13)
    for k in range(r.i(7, 12)):
        a = r.f() * 6.28
        d = r.f() ** 0.7 * 10
        x = int(round(cx + math.cos(a) * d))
        y = int(round(cy + math.sin(a) * d * 0.7))
        if not inside(x, y, 3):
            continue
        body = (FL[4], LN[3], FL[4], GO[3])[r.i(0, 3)]
        c.set(x, y, body)
        if r.f() < 0.5:
            c.set(x + 1, y, FL[4] if body != FL[4] else LN[3])
        c.set(x + 1, y + 1, OCC)
        if r.f() < 0.25:
            c.set(x, y - 1, FL[3])


def rock_chips(c, r):
    """Chips off the outcrops: three to five angular flakes of the red stone, their top a pixel of flesh's
    shade and their body its deep, each on its contact shadow - what the tors shed on their scree."""
    cx, cy = spot(r, 12)
    for k in range(r.i(3, 5)):
        x = cx + r.i(-9, 9)
        y = cy + r.i(-7, 7)
        w = r.i(2, 4)
        h = r.i(1, 2)
        lean = r.i(-1, 1)
        for dy in range(h + 1):
            for dx in range(w - dy):
                col = FL[3] if dy == 0 and dx < w - 1 else FL[4]
                c.set(x + dx + (lean if dy == 0 else 0), y + dy, col)
        for dx in range(w + 1):
            c.set(x + dx, y + h + 1, OCC)


def bunchgrass(c, r):
    """A low dry tussock or two: short blades fanned from one root, linen's shade and flesh's deep with gold's
    shade at the tips - lower and darker than the old dry grass, so a fringe can carry more of it."""
    for k in range(r.i(1, 2)):
        x, y = spot(r, 12)
        y = max(y, 16)
        n = r.i(5, 7)
        h = r.i(5, 8)
        for b in range(n):
            side = b - (n - 1) / 2.0
            ln = max(2, int(round(h - abs(side) * 0.8)))
            col = LN[3] if b % 2 else FL[4]
            for j in range(ln):
                xx = int(round(x + side * (j + 1) / float(ln) * 2.6))
                c.set(xx, y - j, GO[3] if j == ln - 1 else col)
        for dx in range(-2, 3):
            c.set(x + dx, y + 1, OCC)


# set name -> (draw, variants), in ground_detail.SETS' shape.
DETAIL_SETS = [
    ("sand_ripples", sand_ripples, 4),
    ("drift_tail", drift_tail, 3),
    ("desert_gravel", desert_gravel, 4),
    ("rock_chips", rock_chips, 3),
    ("bunchgrass", bunchgrass, 4),
]
