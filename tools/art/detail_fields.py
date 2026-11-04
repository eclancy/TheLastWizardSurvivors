# -*- coding: utf-8 -*-
"""The Fallow Fields' ground detail (.ai/campaign.md section 5). Not run on its own: ground_detail.py
finds this module (ground_detail.chapter_detail_modules) and appends its DETAIL_SETS to its own,
so a full `python tools/art/ground_detail.py` draws them and lists them in detail.json, and
GroundDetail.cs's Fields rules ask for them by name.

Four small things a harvested field has lying in it, each a THING at the size ground_detail.py
settled on ("SIZE, TWICE") and none of them emissive:

  stubble_tufts   a clump of stalks the reaper missed, standing taller than the cut stubble
  thistle         a thistle gone over: a rosette of spined grey leaves and two or three purple heads
  poppy           field poppies, three to five cups on thin stems in their own leaves
  chaff           husks and broken straw lying flat where the threshing blew it

THE POPPIES ARE THE RED ROW, not ember. Ember is player pigment and the colour of fire spells; a
poppy that bright would be the brightest thing on the floor. `red` is old cloth's maroon - a
poppy under no light - and its brightest stop is the petal's lit edge only. No head is a lone
disc: a poppy is a cup with a dark heart, in a clump.
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
VI = GDT.VI
RD = GDT.RD
FL = GDT.FL
spot = GDT.spot
stem = GDT.stem
leaf_on = GDT.leaf_on


def stubble_tufts(c, r):
    """Stalks the reaper missed: a few uncut straws standing together, two with their ears still on,
    leaning as straw does - never quite upright, never all one way."""
    cx, cy = spot(r, 12)
    for k in range(r.i(5, 7)):
        x = cx + r.i(-5, 5)
        h = r.i(6, 11)
        lean = r.f() * 0.6 - 0.3
        for j in range(h):
            xx = int(round(x + lean * j))
            c.set(xx, cy - j, GO[3] if j < h - 2 else GO[2])
        top = (int(round(x + lean * (h - 1))), cy - h + 1)
        if k % 3 == 0:
            # an ear: three pixels, lit on its key side
            c.set(top[0], top[1] - 1, GO[2])
            c.set(top[0], top[1] - 2, GO[1])
            c.set(top[0] + 1, top[1] - 1, GO[3])
        c.set(x + 1, cy + 1, OCC)
    for dx in range(-4, 5, 2):
        c.set(cx + dx, cy + 1, GO[4])


def thistle(c, r):
    """A thistle gone over: spined leaves flat in a rosette, a stem or two, purple heads on top of
    a spiny green cup - the head is violet's lit and base, never its hi."""
    cx, cy = spot(r, 13)
    # the rosette: spined leaves splayed flat, grey-green
    for (dx, dy, side) in ((-5, 2, -1), (4, 2, 1), (-3, 4, -1), (3, 4, 1), (0, 5, 1)):
        for k in range(4):
            x = cx + dx + side * k
            y = cy + dy + (k // 2)
            c.set(x, y, LN[2] if k < 3 else LN[1])
            if k % 2 == 1:
                c.set(x, y - 1, LN[1])                  # a spine
            c.set(x, y + 1, LN[4])
    for (dx, h) in ((-2, r.i(8, 10)), (2, r.i(6, 8)), (0, r.i(10, 12)))[:r.i(2, 3)]:
        x = cx + dx
        stem(c, x, cy + 1, h, LI[3])
        leaf_on(c, x, cy - h // 2, 1 if dx >= 0 else -1, 2)
        top = cy + 1 - h
        # the spiny cup, then the purple head
        c.set(x - 1, top + 1, LI[2])
        c.set(x, top + 1, LI[3])
        c.set(x + 1, top + 1, LI[4])
        c.set(x - 1, top, VI[1])
        c.set(x, top, VI[0])
        c.set(x + 1, top, VI[2])
        c.set(x, top - 1, VI[1])
        c.set(x - 1, top - 1, VI[2])
    c.set(cx, cy + 6, OCC)


def poppy(c, r):
    """Field poppies: three to five cups on thin bending stems, each a dark heart in red petals lit
    on the key side, standing in a few lobed leaves."""
    cx, cy = spot(r, 14)
    for k in range(r.i(4, 6)):
        dx = r.i(-6, 6)
        c.set(cx + dx, cy + 6 + r.i(-1, 1), VE[3])
        c.set(cx + dx + 1, cy + 6, VE[4])
    for k in range(r.i(3, 5)):
        x = cx + r.i(-7, 7)
        y = cy + r.i(-5, 2)
        h = r.i(4, 7)
        for j in range(h):
            c.set(x + (1 if j > h // 2 and k % 2 else 0), y + j, VE[3])
        # the cup: a bowl of four petals five across, its rim lit upper-left, a dark heart. Three
        # across photographed as a red speck.
        for (dx, dy, t) in ((-2, -1, 0), (-1, -2, 0), (0, -2, 0), (1, -2, 1), (2, -1, 1),
                            (-2, 0, 1), (-1, -1, 1), (0, -1, 1), (1, -1, 2), (2, 0, 2),
                            (-1, 0, 2), (0, 0, 4), (1, 0, 2), (-1, 1, 3), (0, 1, 3), (1, 1, 3)):
            c.set(x + dx, y + dy, OCC if t == 4 else RD[t])
    c.set(cx, cy + 8, OCC)


def chaff(c, r):
    """Husks and broken straw lying flat where the threshing blew it: short strokes at every angle,
    in gold's shade with a lit straw here and there, and husks as single base-stop pixels."""
    cx, cy = spot(r, 12)
    for k in range(r.i(7, 10)):
        x = cx + r.i(-9, 9)
        y = cy + r.i(-7, 7)
        dx, dy = ((1, 0), (1, 1), (0, 1), (1, -1))[r.i(0, 3)]
        n = r.i(2, 4)
        for j in range(n):
            c.set(x + dx * j, y + dy * j, GO[2] if (j == 0 and k % 3 == 0) else GO[3])
        c.set(x + dx * (n - 1), y + dy * (n - 1) + 1, GO[4])
    for k in range(r.i(4, 6)):
        c.set(cx + r.i(-8, 8), cy + r.i(-6, 6), GO[2])


# --- THE FOLIAGE PASS (2026-11-03, .ai/terrain.md section 13): the bottom rungs of the Fields' ladder ----
#
# A field left fallow is not bare stubble: the grass comes back through it, docks and nettles crowd the
# hedge banks, and the plough's clods lie where it turned them. Three sets, laid as CLUTTER by the
# chapter's canopy (Foliage.FallowFields.cs): the grass across the open field and thickest at a hedge's
# fringe, the weeds on the fringe and the bank, the clods out in the open and on the ploughed patches.
#
# THE GUARDRAIL (genre-lessons 2 and 5): within about ten L* of the stubble (L* 14), so they are told
# from it by hue and shape, never by brightness - verdant's shade and deep and linen's shade for grass
# and leaves, gold's and linen's deep for clods. No verdant base or lit (a lime blade is the old Forest
# bushes' mistake), nothing round, bright and alone, and no straight line: a furrow ruled dead straight
# across the others is the Plough in the Furrow's tell (FF3), and chaff drifting one way is the Tithe
# Barn's (FF4), so neither is drawn here.


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


def fallow_grass(c, r):
    """Grass coming back through the stubble: three to five low clumps of thin blades, verdant's deep and
    linen's shade with verdant's shade at the tips - darker and drier than the Forest's turf grass, told
    from the stubble by its green, not its value."""
    for (x, y) in scatter_points(r, r.i(3, 5), 5, 12):
        y = max(y, 10)
        n = r.i(3, 5)
        h = r.i(3, 6)
        for k in range(n):
            side = k - (n - 1) / 2.0
            ln = max(2, int(round(h - abs(side))))
            col = VE[4] if k % 2 else LN[3]
            for j in range(ln + 1):
                xx = int(round(x + side * (j + 1) / float(ln + 1) * 2.0))
                c.set(xx, y - j, VE[3] if j == ln else col)
        c.set(x, y + 1, OCC)


def dock(c, x, y, r):
    """A dock: four or five broad leaves splayed from one root, their upper edge verdant's shade over its
    deep (no lit stop: a lit leaf tip measured hi +34 over the stubble, above the cast's rims), and a rusty
    seed spike - red's shade, which reads as a dock gone to seed and not as a flower."""
    for (dx, dy, ln) in ((-1, 0, 6), (1, 0, 6), (-1, 1, 5), (1, 1, 5), (0, -1, 4))[:r.i(4, 5)]:
        for k in range(ln):
            px = x + dx * k
            py = y + dy * (k // 2) - (1 if dy < 0 else 0) * k // 2
            c.set(px, py - 1, VE[3])
            c.set(px, py, VE[4])
    if r.f() < 0.7:
        h = r.i(6, 9)
        for j in range(1, h):
            c.set(x, y - j, VE[4] if j < h // 2 else RD[2])
            if j > h // 2 and j % 2 == 0:
                c.set(x + 1, y - j, RD[3])
                c.set(x - 1, y - j + 1, RD[2])
    c.set(x, y + 1, OCC)
    c.set(x + 1, y + 1, OCC)


def nettle(c, x, y, r):
    """A nettle: an upright stem with its leaves in pairs, each pair a stop darker than the one above."""
    h = r.i(7, 10)
    for j in range(h):
        c.set(x, y - j, VE[4])
    for j in range(2, h, 3):
        tone = VE[3] if j > h // 2 else VE[4]
        c.set(x - 1, y - j, tone)
        c.set(x - 2, y - j + 1, tone)
        c.set(x + 1, y - j, tone)
        c.set(x + 2, y - j + 1, LN[3])
    c.set(x, y + 1, OCC)


def weeds(c, r):
    """The hedge bank's weeds: a dock or two and a nettle, the bank's living things on the saturated ramp's
    dark half."""
    for k, (x, y) in enumerate(scatter_points(r, r.i(2, 3), 8, 16)):
        y = max(y, 14)
        if k % 2 == 0:
            dock(c, x, y, r)
        else:
            nettle(c, x, y, r)


def clods(c, r):
    """Clods the plough turned and nobody harrowed: four to seven lumps lying flat, gold's and linen's deep
    with one lit pixel on the key side and occlusion under the other, and now and then a stone."""
    for (x, y) in scatter_points(r, r.i(4, 7), 5, 7):
        w = r.i(2, 4)
        body = (GO[4], LN[4], FL[4])[r.i(0, 2)]
        for dx in range(w):
            c.set(x + dx, y, body)
            c.set(x + dx, y + 1, body if dx else LN[4])
        c.set(x, y - 1, GO[3])
        c.set(x + w, y + 1, OCC)
        c.set(x + w - 1, y + 2, OCC)
    if r.f() < 0.3:
        x, y = GDT.spot(r, 9)
        c.set(x, y, LN[2])
        c.set(x + 1, y, LN[3])
        c.set(x, y + 1, LN[3])
        c.set(x + 1, y + 1, LN[4])
        c.set(x + 2, y + 1, OCC)


# set name -> (draw, variants), in ground_detail.SETS' shape.
DETAIL_SETS = [
    ("stubble_tufts", stubble_tufts, 4),
    ("thistle", thistle, 3),
    ("poppy", poppy, 3),
    ("chaff", chaff, 3),
    ("fallow_grass", fallow_grass, 5),
    ("weeds", weeds, 4),
    ("clods", clods, 4),
]
