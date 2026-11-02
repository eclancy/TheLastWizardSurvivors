# -*- coding: utf-8 -*-
"""The Emberdeep's ground detail. Drawn by tools/art/ground_detail.py, which finds this module by its
name and appends DETAIL_SETS to its own - run ground_detail.py, never this. GroundDetail.cs's Volcanic
block is what asks for these sets, and where.

Reported from 0.10.0: "Emberdeep doesn't look like cracked volcanic earth." The floor itself is now
basalt plates split by seams that smoulder (tiles.py, basalt()); this is what lies ON it, and it is
the half of "volcanic" that a 48 px floor tile cannot carry - anything with a shape repeats every few
cells there, so the things with shapes are placed here, in world space, in drifts:

  lava_rope   pahoehoe: a lobe of flow that skinned over and stopped, its surface wrinkled into
              ropes. Cold, in the stone row, a step proud of the plates it ran over.
  cinders     a scatter of coals still alive: the red row, with the fire ramp's `edge` - the darkest
              fire there is - at the heart of the biggest. Never brighter: this chapter's cast wear
              fire eyes, and two pixels of them must out-burn anything on the floor.
  ash_drift   a long drift of ash banked by the hot wind: the palest thing on the ground, the stone
              row's lit stop with bone-row flakes, dithered into the plates at its rim.
  sulphur     a fumarole - a hole in the basalt crusted round with sulphur (the gold row's base and
              shade, an ochre, never its lit yellow), a warm glint of the red row down the throat.
              The chapter note smells it before the player sees it.

The ground_detail.py rules hold: material tones only (the cinders' one fire pixel is the exception,
and is the darkest stop of the darkest element), the brightest stop a `lit`, nothing round, bright
and alone. The XP orbs are lime, lilac and pale yellow: nothing here is any of those.
"""
import math

import ground_detail
from ground_detail import GO, OCC, RD, SK, ST, spot

import bonelight as bl

FIRE = bl.ELEMENTS["fire"]
SIZE = ground_detail.SIZE


def lava_rope(c, r):
    """A lobe of pahoehoe: an oval of cooled flow, proud of the floor, its skin pulled into ropes that
    bow the way it was moving. Each rope is a lit crest over a shadowed trough; the lobe's toe drops
    to the floor in `occ` on its lower-right, its upper-left catches the key."""
    cx, cy = spot(r, 16)
    rx, ry = r.i(11, 15), r.i(6, 8)
    ang = r.f() * math.pi * 2.0
    ca, sa = math.cos(ang), math.sin(ang)
    # the flow ran along `ang`; the ropes bow forward of an origin behind the lobe
    ox, oy = cx - ca * rx * 1.3, cy - sa * rx * 1.3 * 0.7
    spacing = 2.6 + r.f() * 0.6
    for y in range(cy - ry - 2, cy + ry + 3):
        for x in range(cx - rx - 2, cx + rx + 3):
            dx, dy = x - cx, (y - cy) * float(rx) / ry
            d = (dx * dx + dy * dy) / float(rx * rx)
            # a lumpy rim, not an ellipse drawn with a compass
            d += 0.09 * math.sin(math.atan2(dy, dx) * 5.0 + ang * 3.0)
            if d > 1.0:
                continue
            if d > 0.82:
                # the toe: lit where it faces the key, dropping to occ where it faces away
                face = dx + dy
                tone = ST[1] if face < -rx * 0.35 else (OCC if face > rx * 0.25 else ST[3])
                c.set(x, y, tone)
                continue
            rope = math.hypot(x - ox, (y - oy) / 0.7) / spacing
            frac = rope - math.floor(rope)
            if frac < 0.28:
                tone = ST[1]
            elif frac > 0.72:
                tone = ST[4]
            else:
                tone = ST[2]
            c.set(x, y, tone)


def cinders(c, r):
    """Three to six coals within a hand's breadth: dark red with a lit top, an `occ` contact under
    each, and at the heart of the biggest one or two the fire ramp's `edge`. A little ash round them."""
    cx, cy = spot(r, 12)
    for k in range(r.i(3, 6)):
        x, y = cx + r.i(-8, 8), cy + r.i(-6, 6)
        big = k < 2
        w = 2 if big else 1
        for dx in range(w + 1):
            c.set(x + dx, y + 1, OCC)
        for dx in range(w):
            c.set(x + dx, y, RD[2])
            if big:
                c.set(x + dx, y - 1, RD[1])
        if big and r.f() < 0.75:
            c.set(x + (1 if w > 1 else 0), y, FIRE[3])
        else:
            c.set(x, y, RD[1])
    for k in range(r.i(6, 10)):
        x, y = cx + r.i(-10, 10), cy + r.i(-7, 7)
        if c.get(x, y)[3] == 0:
            c.set(x, y, ST[2])


def ash_drift(c, r):
    """A drift of ash, banked long by the wind: three overlapping lobes on one axis, lit on the
    upper-left, dithered out at the rim so it settles into the plates rather than lying on them."""
    cx, cy = spot(r, 15)
    ang = (r.f() - 0.5) * 0.9
    ca, sa = math.cos(ang), math.sin(ang)
    for k in range(3):
        off = (k - 1) * r.i(6, 9)
        lx, ly = cx + ca * off, cy + sa * off + r.i(-1, 1)
        rx, ry = r.i(6, 9), r.i(3, 4)
        for dy in range(-ry - 1, ry + 2):
            for dx in range(-rx - 1, rx + 2):
                # in the drift's own frame
                u = dx * ca + dy * sa
                v = -dx * sa + dy * ca
                d = (u / float(rx)) ** 2 + (v / float(ry)) ** 2
                if d > 1.0:
                    continue
                x, y = int(round(lx + dx)), int(round(ly + dy))
                if d > 0.5 and (x + y + k) % 2:
                    continue
                if d > 0.8 and (x * 3 + y) % 3:
                    continue
                tone = ST[2]
                if d < 0.55 and u + v < 0:
                    tone = ST[1]
                c.set(x, y, tone)
    for k in range(r.i(4, 7)):
        x, y = cx + r.i(-12, 12), cy + r.i(-3, 3)
        if c.get(x, y)[3]:
            c.set(x, y, SK[4])


def sulphur(c, r):
    """A fumarole: a ragged hole in the basalt, a warm glint down its throat, and round its mouth a
    crust of sulphur crystals - the gold row's base on the lit side, its shade on the far one, in
    clusters, never a ring drawn with a compass and never the gold row's lit yellow."""
    cx, cy = spot(r, 13)
    # the throat
    for dy in range(-1, 2):
        for dx in range(-2, 3):
            if abs(dx) == 2 and dy != 0:
                continue
            c.set(cx + dx, cy + dy, OCC)
    c.set(cx, cy, RD[2])
    c.set(cx + 1, cy, RD[3])
    # the crust: crystals in a broken ring, thicker on one side where the gas blows
    lean = r.f() * math.pi * 2.0
    for k in range(r.i(14, 20)):
        a = r.f() * math.pi * 2.0
        rad = 3.2 + r.f() * 3.5 + 1.5 * max(0.0, math.cos(a - lean))
        x, y = int(round(cx + math.cos(a) * rad)), int(round(cy + math.sin(a) * rad * 0.7))
        lit = math.cos(a) + math.sin(a) < 0
        c.set(x, y, GO[2] if lit else GO[3])
        if r.f() < 0.5:
            c.set(x + (1 if lit else -1), y, GO[3] if lit else GO[4])
        if r.f() < 0.3:
            c.set(x, y + 1, OCC)
    # a stain of it washed down the plate
    for k in range(r.i(3, 6)):
        c.set(cx + r.i(-6, 6), cy + r.i(3, 6), GO[4])


DETAIL_SETS = [
    ("lava_rope", lava_rope, 4),
    ("cinders", cinders, 4),
    ("ash_drift", ash_drift, 3),
    ("sulphur", sulphur, 3),
]
