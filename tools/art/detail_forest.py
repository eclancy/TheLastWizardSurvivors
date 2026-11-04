# -*- coding: utf-8 -*-
"""The Enchanted Forest's ground clutter, the foliage pass (2026-11-03, .ai/genre-lessons.md section 2,
.ai/terrain.md section 13). Not run on its own: ground_detail.py finds this module
(ground_detail.chapter_detail_modules) and appends its DETAIL_SETS to its own, so a full
`python tools/art/ground_detail.py` draws them and lists them in detail.json, and GroundDetail.cs's
Forest rules ask for them by name.

The bottom two rungs of the size ladder - grass and specks - that the Forest did not have. A wood's
floor is never bare turf: short grass everywhere the light reaches, long grass where a stand's edge
lets it in, wildflower specks and seed heads out in the clearings, and leaf litter and needles under
the trees. Four sets, drawn MANY to a cell and laid on far more cells than the flowers are, because
this layer is the one that makes the ground stop reading as flat:

  grass_short   three to five low clumps of blades, the turf's own grass, across the whole cell
  grass_tall    two to four long arching tufts: the fringe of a stand
  specks        tiny flower heads and seed heads on hair stems among a few blades: the clearings
  litter        fallen leaves, needles and a twig or two lying flat: the floor under a stand

THE GUARDRAIL (genre-lessons 2 and 5): clutter stays lower in contrast than the enemies and the XP
orbs, because reading the swarm is the game. The Forest's cast are DARK silhouettes with a rim (their
mean L* is 5 to 14 under the turf's 25, measured with `_UiShot --light-probe`) and the orbs are discs
30 L* over it. So all of this sits within about ten L* of the turf on average (|dL*| 5 to 9, its
brightest tenth +9 to +18, measured): `verdant` shade and `lichen` base and lit for blades, `lichen`
and `verdant` deep only as a root's contact pixels, `gold`'s shade and `linen` for litter - never a
`lit` stop of a warm row, never `verdant` base or lit (the bushes' lime), nothing round, bright and
alone. The specks are the one set with a
brighter stop, and only as a two-pixel head, a few to a cell; a flower you can see is the flowers'
sets, which this pass did not make denser. Measured per set by `python tools/art/detail_contrast.py`.

Nothing here is a bent fern: bracken bent one way is the Pale Hart's far tell (PaleHart.cs), and a
floor already full of bracken would hide it.
"""
from __future__ import division

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import ground_detail as GDT

OCC = GDT.OCC
SIZE = GDT.SIZE
LI = GDT.LI
VE = GDT.VE
GO = GDT.GO
FL = GDT.FL
VI = GDT.VI
SK = GDT.SK
RD = GDT.RD
LN = GDT.LN


def blades(c, x, y, n, h, body, tip, root=None, spread=1.0, alt=None):
    """A clump: `n` blades fanning from one root at (x, y) - a V, the way ground_detail's `tuft` is
    drawn, so it reads as grass and not as a sapling. Blade k leaves the root already leaning, one
    pixel out for every 1/spread it climbs, and the outer blades are shorter. Every other blade is
    `alt` when given (a green blade among the turf's), and the top pixel of each blade is `tip`,
    with the top two on the key side (upper left), so a clump has a lit side, not a lit rim."""
    mid = (n - 1) / 2.0
    for k in range(n):
        side = k - mid
        length = max(2, int(round(h - abs(side) * 1.2)))
        col = alt if (alt and k % 2) else body
        for j in range(length + 1):
            xx = int(round(x + side * spread * (j + 1) / float(length + 1) * 2.0))
            lit = j == length or (j == length - 1 and side < 0)
            c.set(xx, y - j, tip if lit else col)
    if root:
        for dx in (-1, 0, 1):
            c.set(x + dx, y + 1, root)


def scatter_points(r, n, margin, min_gap):
    """`n` points across the whole cell, `margin` in from its edges, kept `min_gap` apart where the
    tries allow: low grass covers a cell rather than sitting in the middle of it."""
    pts = []
    tries = 0
    while len(pts) < n and tries < n * 20:
        tries += 1
        x, y = r.i(margin, SIZE - 1 - margin), r.i(margin + 3, SIZE - 1 - margin)
        if all(abs(x - px) + abs(y - py) >= min_gap for px, py in pts):
            pts.append((x, y))
    return pts


def grass_short(c, r):
    """The turf's own grass: three to five clumps of four to six blades, five to seven pixels tall,
    green (`verdant` shade, at the turf's own L*) with the turf's lit stop at the tips and every other
    blade the turf's base in most of them: told from the turf by hue far more than by value. Fewer and
    taller than the first draw, whose five to eight small clumps a cell were, at play scale, a sprinkle
    of dots rather than grass; and green, because in the turf's teal they were noise in the turf."""
    for (x, y) in scatter_points(r, r.i(3, 5), 5, 13):
        y = max(y, 11)
        blades(c, x, y, r.i(4, 6), r.i(5, 7), VE[3], LI[1], LI[4] if r.f() < 0.6 else None,
               spread=1.0, alt=LI[2] if r.f() < 0.6 else None)


def grass_tall(c, r):
    """The fringe of a stand: two to four tufts of long grass, seven to twelve pixels, arching out,
    dark green with pale tips, each on its own small contact shadow."""
    for (x, y) in scatter_points(r, r.i(2, 4), 6, 12):
        y = max(y, 14)
        h = r.i(7, 12)
        n = r.i(5, 7)
        blades(c, x, y, n, h, VE[3], LI[1], VE[4], spread=1.1, alt=VE[4] if r.f() < 0.5 else None)
        # a second, shorter rank in front, a stop lighter, so a tuft has depth
        blades(c, x + r.i(-2, 2), y + 1, r.i(3, 4), max(3, h // 2), LI[2], LI[1], None, spread=0.9)
        for dx in (-3, -1, 1, 3):
            c.set(x + dx, y + 2, VE[4])


# Muted heads: each a stop or two over the turf, never a warm row's `lit` and never a lone disc of an
# orb's lime, lilac or pale yellow (ground_detail.py, "SIZE, TWICE").
SPECK_HEADS = [
    (VI[0], VI[1]),   # violet: a self-heal head
    (SK[3], SK[4]),   # bone shade: a seed clock gone grey
    (FL[3], FL[4]),   # flesh shade: a sorrel spike
    (GO[2], GO[3]),   # gold base: a buttercup closed for the night
    (RD[0], RD[1]),   # red hi: a campion
]


def specks(c, r):
    """Wildflower specks: three to six heads of two pixels on hair stems, in two or three kinds, among
    a couple of short clumps - the clearings' small change, seen as specks and never as flowers."""
    kinds = [SPECK_HEADS[r.i(0, len(SPECK_HEADS) - 1)] for _ in range(r.i(2, 3))]
    for (x, y) in scatter_points(r, r.i(2, 3), 4, 10):
        blades(c, x, y, 3, r.i(3, 4), LI[2], LI[1], spread=0.8)
    for (x, y) in scatter_points(r, r.i(3, 6), 4, 7):
        head, shade = kinds[r.i(0, len(kinds) - 1)]
        h = r.i(2, 4)
        for j in range(1, h + 1):
            c.set(x, y - j, VE[3])
        c.set(x, y - h - 1, head)
        c.set(x + 1, y - h - 1, shade)
        if r.f() < 0.4:
            c.set(x, y - h - 2, shade)


def fallen_leaf(c, x, y, body, edge, turn):
    """A leaf lying flat, 5x3 or 3x5, pointed at both ends, its far edge a stop darker."""
    pts = [(0, 0), (1, 0), (2, 0), (-1, 1), (0, 1), (1, 1), (2, 1), (3, 1), (0, 2), (1, 2), (2, 2)]
    for dx, dy in pts:
        if turn:
            dx, dy = dy, dx
        c.set(x + dx, y + dy, edge if dy == 2 or dx == 3 else body)


def litter(c, r):
    """The floor under a stand: four to seven fallen leaves, brown (gold's shade) and grey-green
    (linen), a scatter of needles, and sometimes a twig - all lying flat, within a stop of the moss.
    Not flesh's shade: that stop is mauve, and a mauve leaf reads as a petal."""
    tones = [(GO[3], GO[4]), (GO[3], FL[4]), (LN[2], LN[3]), (GO[3], LN[3])]
    for (x, y) in scatter_points(r, r.i(4, 7), 4, 7):
        body, edge = tones[r.i(0, len(tones) - 1)]
        fallen_leaf(c, x, y, body, edge, r.i(0, 1))
    for (x, y) in scatter_points(r, r.i(3, 6), 3, 5):
        # a needle: three or four pixels on a slant
        dx = 1 if r.f() < 0.5 else -1
        for k in range(r.i(3, 4)):
            c.set(x + dx * k, y + k // 2, GO[3] if k % 2 else FL[4])
    if r.f() < 0.35:
        x, y = r.i(8, SIZE - 20), r.i(10, SIZE - 10)
        for k in range(r.i(8, 12)):
            c.set(x + k, y + k // 4, GO[3])
            c.set(x + k, y + k // 4 + 1, GO[4])


DETAIL_SETS = [
    ("grass_short", grass_short, 6),
    ("grass_tall", grass_tall, 4),
    ("specks", specks, 5),
    ("litter", litter, 5),
]
