# -*- coding: utf-8 -*-
"""The Blighted Swamp's ground detail: the bottom rungs of its foliage ladder (.ai/terrain.md section 13).
Not run on its own: ground_detail.py finds this module (ground_detail.chapter_detail_modules) and appends
its DETAIL_SETS to its own, so a full `python tools/art/ground_detail.py` draws them and lists them in
detail.json, and the Swamp's foliage (scripts/Foliage.BlightedSwamp.cs) lays them as CLUTTER by its canopy.

Six small things a fen has lying in it, each a THING at the size ground_detail.py settled on ("SIZE, TWICE"),
none of them emissive:

  rushes      soft rush: tight fans of thin dark stems, a brown flower tuft off the side - the fen's grass,
              across the open mud and thickest at an island's fringe
  duckweed    a green scum of tiny pads on still water, gathered where the islands shade the pools
  peat_clods  lumps of turned peat and a stone, out in the open fen and on the dark peat patches
  knees       cypress knees: two to four knobbed cones of root standing up out of the bog round the trees
  bog_litter  sodden leaves and a twig lying flat on the islands' black peat
  rotwood     a rotten stick sunk in the mud, crumbling, a shelf of dull fungus on it

THE GUARDRAIL (genre-lessons 2 and 5): within about ten L* of the mud (L* 22) and the peat (12), told from them
by hue and shape - verdant's shade and deep, linen's shade and deep, gold's and flesh's deep - never by
brightness. No verdant lit, no `hi` anywhere, nothing round, bright and alone (an XP orb is a lime, lilac or
pale yellow disc). The old swamp rules' white flowers, mushrooms and clover are bright, and this adds no more
of them.

WHAT IT KEEPS CLEAR (.ai/achievements-and-secrets.md 8.6): no flower on a pad (S1's tell), no warm or gold
point (S2, S3), no puddle of black mud and no reed bent one way (the Swallow-Hole's far tell, S5: the
rushes fan both ways), and no straight line - the knees are cones and the clods lumps, because a straight
cut face is the Peat Bank's (S6).
"""
from __future__ import division

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import ground_detail as GDT

OCC = GDT.OCC
GO = GDT.GO
LN = GDT.LN
LI = GDT.LI
VE = GDT.VE
FL = GDT.FL
ST = GDT.ST
spot = GDT.spot


def scatter_points(r, n, margin, min_gap):
    """`n` points across the whole cell, `margin` in from its edges, kept `min_gap` apart where the tries
    allow, so low growth covers a cell rather than sitting in its middle."""
    pts = []
    tries = 0
    while len(pts) < n and tries < n * 20:
        tries += 1
        x, y = r.i(margin, GDT.SIZE - 1 - margin), r.i(margin + 3, GDT.SIZE - 1 - margin)
        if all(abs(x - px) + abs(y - py) >= min_gap for px, py in pts):
            pts.append((x, y))
    return pts


def rush(c, x, y, r):
    """One clump of soft rush: five to eight thin stems from one foot, fanned both ways and straight - a
    rush does not droop - verdant's shade with its deep down every other stem, and a brown flower tuft
    a third of the way down one stem, the way a rush flowers out of its side."""
    n = r.i(6, 9)
    h = r.i(10, 15)
    for k in range(n):
        side = k - (n - 1) / 2.0
        ln = max(4, h - int(abs(side) * 1.2) - r.i(0, 2))
        col = VE[3] if k % 2 else VE[4]
        for j in range(ln + 1):
            xx = int(round(x + side * j / float(ln) * 1.6))
            c.set(xx, y - j, LN[3] if j == ln and k % 3 == 0 else col)
    fy = y - h + h // 3
    c.set(x + 1, fy, FL[4])
    c.set(x + 2, fy, GO[4])
    c.set(x + 1, fy + 1, GO[4])
    c.set(x, y + 1, OCC)
    c.set(x + 1, y + 1, OCC)


def rushes(c, r):
    """Two or three rush clumps across the cell."""
    for (x, y) in scatter_points(r, r.i(2, 3), 7, 14):
        rush(c, x, max(y, 18), r)


def duckweed(c, r):
    """Duckweed on still water: drifts of tiny pads, one and two pixels, in verdant's shade with its deep
    under them and the odd base-stop pad where the key catches one - a scum, never a lily pad, and
    never one bright pad alone."""
    cx, cy = spot(r, 12)
    for k in range(r.i(3, 4)):
        lx, ly = cx + r.i(-8, 8), cy + r.i(-7, 7)
        rx, ry = r.i(4, 8), r.i(2, 5)
        for j in range(rx * ry * 2):
            dx = r.i(-rx, rx)
            dy = r.i(-ry, ry)
            if (dx / float(rx)) ** 2 + (dy / float(ry)) ** 2 > 1.0:
                continue
            x, y = lx + dx, ly + dy
            c.set(x, y, VE[3] if (dx * 3 + dy + k) % 11 else VE[2])
            if r.f() < 0.35:
                c.set(x + 1, y, VE[3])
            c.set(x, y + 1, VE[4])


def peat_clods(c, r):
    """Lumps of turned peat lying in the mud: gold's and flesh's deep with linen's deep, one lit pixel on
    the key side and occlusion under the other, and now and then a stone."""
    for (x, y) in scatter_points(r, r.i(4, 6), 6, 8):
        w = r.i(3, 5)
        body = (GO[4], FL[4], LN[4])[r.i(0, 2)]
        for dx in range(w):
            c.set(x + dx, y, body)
            c.set(x + dx, y + 1, body if dx else LN[4])
        c.set(x + r.i(0, w - 1), y - 1, LN[3])
        c.set(x + w, y + 1, OCC)
        c.set(x + w - 1, y + 2, OCC)
    if r.f() < 0.3:
        x, y = spot(r, 10)
        c.set(x, y, ST[1])
        c.set(x + 1, y, ST[2])
        c.set(x, y + 1, ST[2])
        c.set(x + 1, y + 1, ST[3])
        c.set(x + 2, y + 1, OCC)


def knee(c, x, y, h):
    """One cypress knee: a cone of root, lit on its left, knobbed at the top, standing out of the bog."""
    for j in range(h):
        half = max(0, int((h - j) * 0.45))
        for dx in range(-half, half + 1):
            tone = LN[2] if dx < 0 else (LN[3] if dx == 0 or half == 0 else LN[4])
            c.set(x + dx, y - j, tone)
    c.set(x, y - h, LN[3])
    c.set(x - 1, y - h + 1, LN[2])
    for dx in range(-int(h * 0.45) - 1, int(h * 0.45) + 2):
        c.set(x + dx, y + 1, OCC)


def knees(c, r):
    """Two to four cypress knees in a loose group, of different heights, round where a tree stands."""
    cx, cy = spot(r, 15)
    for k in range(r.i(2, 4)):
        knee(c, cx + r.i(-9, 9), cy + r.i(-4, 6), r.i(5, 10))


def bog_litter(c, r):
    """Sodden leaves lying flat on the islands' black peat: gold's and linen's shade and verdant's, a stop
    over the peat so a leaf is a leaf on it, with its deep down the shaded edge - and a short black twig."""
    for (x, y) in scatter_points(r, r.i(4, 7), 6, 7):
        body = (GO[3], LN[3], VE[3], GO[4])[r.i(0, 3)]
        for dx in range(-1, 2):
            c.set(x + dx, y, body)
        c.set(x, y - 1, body)
        c.set(x + (1 if r.f() < 0.5 else -1), y + 1, LN[4])
        c.set(x + 1, y + 1, OCC)
    if r.f() < 0.6:
        x, y = spot(r, 12)
        run = r.i(5, 8)
        slope = r.f() * 0.8 - 0.4
        for j in range(run):
            c.set(x + j, int(y + j * slope), LN[4])
            c.set(x + j, int(y + j * slope) + 1, OCC)


def rotwood(c, r):
    """A rotten stick half sunk in the mud: broken into crumbling lengths along a line that wanders, linen's
    shade over its deep, with a shelf of dull fungus on it - gold's shade, a brown shelf, never a pale cap."""
    x, y = spot(r, 14)
    x -= 9
    run = r.i(14, 19)
    slope = r.f() * 0.4 - 0.2
    wob = 0.0
    for j in range(run):
        if j % 6 == 5:
            continue                                      # a gap where it has rotted through
        wob += (r.f() - 0.5) * 0.5
        yy = int(round(y + j * slope + wob))
        c.set(x + j, yy, LN[3] if j % 4 else LN[2])
        c.set(x + j, yy + 1, LN[4])
        c.set(x + j, yy + 2, OCC if j % 2 == 0 else LN[4])
    fx = x + r.i(3, run - 5)
    fy = int(round(y + (fx - x) * slope + wob)) - 1
    for dx in range(3):
        c.set(fx + dx, fy, GO[3])
    c.set(fx + 1, fy - 1, GO[3])
    c.set(fx + 3, fy, GO[4])


# set name -> (draw, variants), in ground_detail.SETS' shape.
DETAIL_SETS = [
    ("rushes", rushes, 5),
    ("duckweed", duckweed, 4),
    ("peat_clods", peat_clods, 4),
    ("knees", knees, 4),
    ("bog_litter", bog_litter, 4),
    ("rotwood", rotwood, 3),
]
