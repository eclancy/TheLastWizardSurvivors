# -*- coding: utf-8 -*-
"""The Frozen Waste's standing foliage and stone (.ai/terrain.md section 13, "The Frozen Waste").
Run: python tools/art/props_frozen.py

    world/frozen/rime-spruce-1..3.png   spruce buried in crown snow and rime: the snow ghosts, the stands
    world/frozen/snow-spruce-1, -2.png  spruce spires with snow lying on every tier: a grove's edge
    world/frozen/rimed-snag-1, -2.png   dead spruce, bare to the stubs and rimed: the fringe, and now and then alone
    world/frozen/frost-boulder-1..3.png granite the frost split and the snow capped, half buried in a drift
    world/frozen/snow-shrub-1..3.png    dwarf birch and juniper under the snow: knee-high, walked through
    world/frozen/frozen.json            every sprite above, in trees.json's shape
    tools/art/_props_frozen_preview.png (gitignored) on the snow's floor tone, x2, for looking at

THE LADDER. The Forest's stands are woods, the Fields' hedgerows, the Swamp's tree islands and the Sands'
stone. The Frozen Waste's are SNOW GHOSTS: "a wizard lies under the ice, and the cold stands guard"
(the chapter's line). In a waste this cold the spruce that grow in the shelter of a hollow are buried
standing - snow and rime bound onto every bough until the tree is a hunched white column with its top
bent over under the load (crown snow-load, tykky in Lapland: Väinämöinen is under the ice here). They
stand in groves across the wind (Foliage.FrozenWaste.cs gives the canopy that shape); between the groves
is the open waste, most of the ground, to fight on. Largest first:

  rime-spruce    200-250 px   a column of snow pillows bound round a spruce, each a knobbled lump with
                              dark needle tips showing under its hem, the top hooked over: the stands
  snow-spruce    180-220 px   a dark spire whose drooping tiers each carry a cap of snow: a grove's edge,
                              where the load is lighter
  rimed-snag     150-180 px   a dead spruce, bare to the stubs of its branches, its top snapped, a crust
                              of rime feathered off one side of everything: the fringe, and alone in the open
  frost-boulder  56-120 px    granite in rounded, split masses, a snow cap draped over the top, the foot
                              sunk in a drift: on the fringe and among the groves
  snow-shrub     36-64 px     dwarf birch twigs and a juniper cushion poking through the snow
  (the chapter's old stones and crystals, unchanged, at their old places)

Below them, the ground detail (detail_frost.py): frosted stalks out in the open, half-buried stones on a
grove's fringe, needle litter and fallen bough snow under the groves.

THE GUARDRAIL (.ai/genre-lessons.md sections 2 and 5): clutter stays lower in contrast than the cast and
the XP orbs. The floor is the bone row's deep (L* 27) and the cast stands on it as dark bodies at about
the floor's own value, read by a rim 44-52 L* over it and a dark 22-26 under it (the light probe at the
start); the orbs are 66-69 over it. So the snow on everything here is the bone row's SHADE (L* 46) in the
light and steel's shade and deep in the shadow, with the bone row's base (L* 67) only as the crown of a
lit lump a pixel or two at a time; the stone and the dead wood are in the dark rows' upper stops, a step
under the floor, never the occlusion but in a crease. Nothing is the ice element's ramp (the cast's ice
is lit edges on dark stone; a pale blue shell would be the Freeze tint), nothing is bone-white.

NONE OF IT IS A SECRET'S TELL (.ai/stage-secrets.md 6.6, .ai/achievements-and-secrets.md 8.8): no
footprint or track of any kind (I1, a line of footprints from nowhere), no flat slab and nothing cut or
marked in a stone (I2's rune-stones), no free-standing drift (I2's far tell is drifts that all lie one
way; the drift here is heaped against a foot, and every sprite is flipped at random), no fire ring, brazier
or anything warm (I1's camp, I3's watch-fire), no clear pane of ice (I4), and no stones piled on one
another (the bearing cairn, the chapter's wayfinder).

WEATHERED, NOT BUILT (the integrator on the Sands' first pass: "stacked bricks"; on another, "paving"):
every mass here is an irregular lump of its own - a snow pillow knobbled by rime, a granite mass broken
into facets, a drift - and no two are alike.

Drawn with trees.py's painters (Bleed, band, shade_pool, contact, save_grounded), imported, never edited.
Python 2.7.
"""
from __future__ import division, print_function

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

import bonelight as bl
import raster
import trees as TR

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "frozen")
PREVIEW = os.path.join(ROOT, "tools", "art", "_props_frozen_preview.png")
RES = "res://assets/bonelight/world/frozen"
TAU = math.pi * 2.0


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


SK = ramp("skin")      # bone: the snow
STL = ramp("steel")    # the snow in shadow: cold and blue
ST = ramp("stone")     # granite
VE = ramp("verdant")   # the spruce's needles, on the ramp's dark half only
FL = ramp("flesh")     # spruce bark
LN = ramp("linen")     # dead wood
GO = ramp("gold")      # a dead leaf left on a dwarf birch

# THE SNOW, brightest first, as trees.band takes it: the bone row's base only where a lump's crown meets
# the key, its shade for the lit body, steel's shade and deep for the side turned from the light.
SNOW = [SK[2], SK[3], STL[3], STL[4]]
# The snow lying on the ground at a foot: the floor's own value and a step either side of it.
DRIFT = [SK[3], SK[4], STL[3], STL[4]]
# Granite, brightest first: grey-green with old lichen, linen's base where the key finds it, its shade for
# the body and its deep in shadow, stone's base in the deep of an overhang. Not the snow's blue-greys: the
# first boulders, in the stone row, read as more snow.
ROCK = [LN[2], LN[3], LN[4], ST[2], ST[3]]
# Needles: the dark half of verdant and the occlusion. A spruce under snow is a dark thing glimpsed.
NEEDLE = [VE[3], VE[4], OCC]
# Spruce bark in trees.py's five-slot shape (trunk() bands slots 1-4).
BARK = [FL[3], FL[3], FL[3], FL[4], OCC]
# Dead wood: grey, weathered, linen's shade to its deep.
DEAD = [LN[2], LN[2], LN[3], LN[4], OCC]

LX, LY = TR.LX, TR.LY
# The key as a lump's normal sees it: upper left and toward the viewer.
KX, KY, KZ = -0.52, -0.62, 0.59


def hash2(x, y, s):
    return TR.hash2(x, y, s)


def blob_outline(rnd, n=None, rough=0.22):
    """A soft lump's outline as (angle, radius) round its centre: 9 to 13 corners at jittered radii,
    joined by arcs rather than chords, so it bulges and pinches like a pillow of snow and never draws
    the same twice."""
    n = n or 9 + rnd.randrange(5)
    pts = []
    for k in range(n):
        a = (k + (rnd.random() - 0.5) * 0.7) / float(n) * TAU - math.pi
        pts.append((a, 1.0 - rough * 0.5 + rnd.random() * rough))
    pts.sort()
    return pts


def outline_r(pts, th):
    """The outline's radius at angle th, eased between corners (a lump, not a polygon)."""
    n = len(pts)
    for k in range(n):
        a0, r0 = pts[k]
        a1, r1 = pts[(k + 1) % n]
        if k == n - 1:
            a1 += TAU
        t = th
        if t < a0:
            t += TAU
        if a0 <= t <= a1:
            f = (t - a0) / max(1e-6, a1 - a0)
            f = f * f * (3 - 2 * f)
            return r0 + (r1 - r0) * f
    return pts[0][1]


# --- snow ---------------------------------------------------------------------------------------

def snow_lump(c, x0, y0, rx, ry, seed, gcx, gcy, grx, gry, bias=0.0, hem=None, rime_side=1, owner=None):
    """One pillow of snow: a knobbled lump lit upper left as a lump of its own and as part of the whole
    (gcx, gcy, grx, gry), its lower-right rim a seam of shadow over whatever is behind it. `hem` collects
    points along its underside for the needle tips that show below it. Rime knobbles roughen the edge on
    `rime_side` (1 right, -1 left)."""
    rnd = TR.Rng(seed)
    pts = blob_outline(rnd)
    for y in range(int(y0 - ry) - 2, int(y0 + ry) + 3):
        for x in range(int(x0 - rx) - 2, int(x0 + rx) + 3):
            nx = (x - x0) / float(rx)
            ny = (y - y0) / float(ry)
            th = math.atan2(ny, nx)
            edge = outline_r(pts, th)
            # rime: the windward edge knobbled pixel by pixel
            if nx * rime_side > 0.3:
                edge += (hash2(x, y, seed + 29) - 0.55) * 0.22
            d = math.sqrt(nx * nx + ny * ny)
            if d > edge:
                continue
            dd = d / edge
            nz = math.sqrt(max(0.0, 1.0 - min(1.0, dd * dd)))
            local = (nx / edge * KX + ny / edge * KY + nz * KZ)          # -1..1
            gx = (x - gcx) / float(grx)
            gy = (y - gcy) / float(gry)
            glob = -(gx * LX + gy * LY) * 0.7 - max(0.0, gy) * 0.25
            v = 0.42 + bias + local * 0.36 + glob * 0.22
            v += (hash2(x, y, seed) - 0.5) * 0.08
            col = TR.band(SNOW, v, x, y, 0.22)
            # the crown of a lit lump keeps its bright stop as a plane, never a lone pixel: no dither into it
            if col == SNOW[0] and (dd > 0.75 or v < 0.86):
                col = SNOW[1]
            # the seam: the lump's lower-right rim over what is behind it
            if dd > 0.74 and nx * 0.55 + ny * 0.83 > 0.42:
                col = STL[4] if (dd > 0.86 or hash2(x, y, seed + 3) < 0.35) else STL[3]
            c.set(x, y, col)
            if owner is not None:
                owner[(x, y)] = v
    if hem is not None:
        for k in range(int(rx * 1.4)):
            u = -0.95 + 1.9 * rnd.random()
            th = math.atan2(0.9, u)
            r = outline_r(pts, th)
            hem.append((x0 + u * rx * r, y0 + ry * r * math.sqrt(max(0.0, 1.0 - u * u * 0.5)) * 0.92))


def needle_tips(c, hem, seed, lit_x):
    """Dark spruce bough tips hanging out under the snow: short drooping strokes of needles, a stop
    lighter on the side the key finds, each ending in the occlusion. Where they show, the snow ghost is a
    tree."""
    rnd = TR.Rng(seed + 61)
    for (hx, hy) in hem:
        if rnd.random() < 0.45:
            continue
        ln = 2 + rnd.randrange(4)
        lean = (hx - lit_x) * 0.04 + (rnd.random() - 0.5) * 0.6
        lit = hx < lit_x and rnd.random() < 0.5
        for j in range(ln):
            x = int(TR.round2(hx + lean * j))
            y = int(hy) + j
            col = NEEDLE[0] if (lit and j < ln - 1) else (NEEDLE[1] if j < ln - 1 else NEEDLE[2])
            c.set(x, y, col)
            if j == 0 and rnd.random() < 0.5:
                c.set(x + (1 if hx > lit_x else -1), y, NEEDLE[1])


def drift_foot(c, cx, base, half, seed, left=1.0, right=0.75, height=4.0):
    """The snow heaped round a foot: a low uneven mound at the floor's own value, its crest a stop lighter
    where the key finds it, fading into the floor at its ends, and the cold shadow it sits in. Heaped more
    on one side, as a wind heaps it - and the sprite is flipped at random, so no two lie the same way."""
    for x in range(int(cx - half * left), int(cx + half * right) + 1):
        u = (x - cx) / float(half * (left if x < cx else right))
        if abs(u) > 1.0:
            continue
        hgt = max(0.0, 1.0 - u * u) * (height * 0.7 + height * 0.6 * hash2(x // 3, 1, seed)) * (1.1 if x < cx else 0.85)
        for k in range(int(hgt) + 1):
            y = base - k
            top = k >= int(hgt) - 1
            if top and u < 0.35 and hash2(x, y, seed + 5) < 0.55:
                tone = DRIFT[0]
            elif top:
                tone = DRIFT[1] if u < 0.6 else DRIFT[2]
            else:
                tone = DRIFT[1] if (u < -0.2 and k > hgt * 0.5) else DRIFT[2]
            if k == 0 and abs(u) > 0.8:
                tone = DRIFT[2] if hash2(x, 3, seed) < 0.5 else DRIFT[3]
            c.set(x, y, tone)
    for x in range(int(cx - half * left * 0.6), int(cx + half * right * 0.95)):
        if hash2(x, base + 1, seed) < 0.55 and c.get(x, base + 1)[3] == 0:
            c.set(x, base + 1, STL[4])


# --- the snow ghost -----------------------------------------------------------------------------

def rime_spruce(seed, w, h, lean=0.0, hook=1.0, lumps=None):
    """A spruce buried standing: snow and rime bound onto every bough until the tree is a hunched white
    column of pillows, knobbled on the windward side, the top hooked over under the load. Dark needle tips
    show under the hem of every pillow and the lowest boughs sweep out dark under their own snow, so it
    still reads as a tree - a snow ghost, not a snowman. Sunk in its own drift."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    c.anchor_x = cx
    TR.shade_pool(c, cx + w * 0.16, base + 1, w * 0.48, 5 + w * 0.05, 0.45)
    TR.contact(c, cx, base + 1, 6)
    # a stub of trunk at the foot, mostly buried
    TR.trunk(c, cx, base, base - h * 0.10, 7, 6, BARK, seed, flare=2)
    top = 3.0
    bottom = base - h * 0.08
    hem = []
    rime = 1 if rnd.random() < 0.5 else -1
    # the lowest boughs: a dark skirt sweeping out under the column, snow along its top
    skirt_y = bottom - h * 0.06
    for k in range(int(w * 1.1)):
        side = -1 if k % 2 == 0 else 1
        reach = w * (0.30 + rnd.random() * 0.22)
        x0 = cx + side * rnd.random() * 3
        px, py = float(x0), skirt_y + rnd.random() * 3
        for j in range(int(reach)):
            px += side
            py += 0.18 + j * 0.012
            col = NEEDLE[1] if hash2(int(px), int(py), seed) < 0.75 else (NEEDLE[0] if side < 0 else NEEDLE[2])
            c.set(int(px), int(py), col)
            c.set(int(px), int(py) + 1, NEEDLE[2] if j > reach * 0.4 else NEEDLE[1])
            if j < reach * 0.75 and hash2(int(px), 9, seed + k) < 0.7:
                c.set(int(px), int(py) - 1, SNOW[1] if side < 0 else SNOW[2])
    # THE COLUMN is many pillows of snow of every size, scattered inside an envelope that bulges and pinches
    # up its height (the load is never even) and leans, its top hooked over. They are drawn back (high) to
    # front (low), as trees.crown draws leaf clusters, so a lower pillow overlaps the one above it with a seam
    # of shadow at its lower right. Two first passes were wrong the other ways: pillows on every tier, drawn
    # top down with needle tips under each, sprinkled the face with dark glyphs; one full-width pillow a tier,
    # drawn bottom up, was a stack of pancakes - the bricks again.
    mid_lean = lean * h * 0.06
    wob = rnd.random() * TAU

    def centre(t):                                  # t: 0 at the top, 1 at the foot
        return cx + mid_lean * (1 - t) + hook * (1 - t) ** 3 * w * 0.20

    def half(t):
        bulge = 1.0 + 0.16 * math.sin(t * 9.0 + wob) + 0.10 * math.sin(t * 21.0 + wob * 2)
        return w * (0.10 + 0.36 * t ** 0.75) * bulge

    n = lumps or int(h * w / 70)
    spec = []
    for k in range(n):
        # a few seeded up the top first, so the hooked head is joined to the column
        t = (k / 6.0) * 0.16 if k < 6 else rnd.random() ** 0.85
        y = top + 4 + (bottom - top - 8) * t
        hw = half(t)
        x = centre(t) + (rnd.random() * 2 - 1) * hw * 0.66
        r = max(3.0, hw * (0.24 + rnd.random() * 0.36))
        spec.append((x, y, r, r * (0.60 + rnd.random() * 0.22)))
    # the hooked top: two or three pillows drooping off the bent leader, over the side the column leans to
    tx = centre(0.0)
    for k in range(3):
        r = w * (0.11 - 0.025 * k)
        spec.append((tx + hook * w * (0.05 + 0.10 * k), top + 3 + k * 2.5 + (k * k) * 0.8, r, r * 0.7))
    spec.sort(key=lambda s: s[1] + s[2] * 0.3)
    gcy = (top + bottom) * 0.5
    body = {}
    for k, (x0, y0, rx, ry) in enumerate(spec):
        snow_lump(c, x0, y0, rx, ry, seed + 17 * k, cx, gcy, w * 0.5, (bottom - top) * 0.5,
                  bias=0.0, rime_side=rime, owner=body)
    # where the tree shows: under an overhang on the outline (a pixel of snow with nothing below it, above
    # the skirt), bough tips hang out dark - sparse, more on the side away from the light
    hem = []
    for (x, y) in body:
        if y < bottom - 6 and c.get(x, y + 1)[3] == 0 and (x, y + 1) not in body:
            if hash2(x, y, seed + 41) < (0.35 if x > cx else 0.18):
                hem.append((x, y + 1))
    needle_tips(c, hem, seed, cx - w * 0.1)
    # and a dark hollow or two deep between pillows on the shadow side
    for k in range(2 + rnd.randrange(3)):
        x0, y0, rx, ry = spec[rnd.randrange(len(spec))]
        gx, gy = int(x0 + rx * 0.5), int(y0 + ry * 0.7)
        for j in range(2 + rnd.randrange(2)):
            if c.get(gx + j, gy)[3]:
                c.set(gx + j, gy, NEEDLE[1] if j % 2 else NEEDLE[2])
                if j == 1:
                    c.set(gx + j, gy + 1, NEEDLE[2])
    drift_foot(c, cx, base, w * 0.48, seed, height=4.0 + w * 0.03)
    return c


# --- the snow-laden spruce ----------------------------------------------------------------------

def snow_spruce(seed, w, h, tiers=9):
    """A spruce spire with the snow lying on it, not yet buried: tiers of dark boughs drooping at their
    tips, each carrying a cap of snow thickest near the trunk and sliding off toward the tip, lit on the
    upper left. Narrower than the Forest's firs, and its tiers hang."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    c.anchor_x = cx
    TR.shade_pool(c, cx + w * 0.14, base + 1, w * 0.42, 5 + w * 0.04, 0.45)
    TR.contact(c, cx, base + 1, 5)
    TR.trunk(c, cx, base, base - h * 0.32, 6, 4, BARK, seed, flare=2)
    top = 3
    bottom = base - h * 0.11
    # the stem up through the tiers, so no tier floats
    for y in range(top, int(base - h * 0.32)):
        c.set(cx, y, NEEDLE[1])
        c.set(cx + 1, y, NEEDLE[2])
    # THE TIERS ARE UNEVEN (an even stack of them, tried first, was a striped Christmas tree): each starts
    # where the last left off, a little more or less, its two sides reach out by different amounts, and its
    # snow lies in clumps along it, gone here and there where a load slid off.
    starts = [0.0]
    for i in range(tiers - 1):
        starts.append(starts[-1] + (0.45 if i == 0 else 0.6 + rnd.random() * 0.8))
    span = starts[-1] + 1.0
    prev = None
    for i in range(tiers):
        t0 = starts[i] / span
        ty = top + (bottom - top) * (t0 ** 0.88)
        th = (bottom - top) / tiers * (1.45 + rnd.random() * 0.6)
        if prev is not None:
            ty = min(ty, prev[0] + prev[1] * 0.62)    # never a gap the stem shows through
        prev = (ty, th)
        reach = 3 + (w * 0.5 - 4) * ((i + 1) / float(tiers)) ** 0.95
        left = reach * (0.75 + rnd.random() * 0.4)
        right = reach * (0.75 + rnd.random() * 0.4)
        skew = (rnd.random() - 0.5) * 2.0
        slid = rnd.random() < 0.3                # one side's load gone
        # how much this tier carries: some are loaded, some have shed it (an even white chevron on every tier,
        # in the game at play scale, read as a striped Christmas tree in a row of them)
        load = 0.3 + rnd.random() * 0.85
        phase = rnd.random() * TAU
        for x in range(int(cx - left) - 2, int(cx + right) + 3):
            half = left if x < cx + skew else right
            dx = (x - cx - skew) / max(1.0, half)
            if abs(dx) > 1.0:
                continue
            # the tier's upper surface rises to the trunk and its tips droop, in separate boughs: the hem
            # scalloped where one bough's tip hangs lower than the next
            bough = abs(math.sin(dx * 8.5 + phase))
            surf = ty + th * (0.10 + 0.55 * abs(dx) ** 1.5) + math.sin(dx * 7 + i) * 0.6 + bough * 0.8
            hem = ty + th * (0.74 + 0.32 * abs(dx) ** 2) + bough * th * 0.14 - 0.12 * th * hash2(x, i, seed)
            # the snow's depth on this column: thick in by the trunk, gone at the tip, lying in clumps on the
            # boughs and bare between them
            clump = hash2(x // 4, i, seed + 7)
            depth = 0.0 if clump < 0.3 else (2.8 - 2.6 * abs(dx) ** 1.1) * (0.4 + clump) * load
            if slid and dx > 0.15:
                depth *= 0.25
            for y in range(int(surf), int(hem) + 1):
                u = (y - surf) / max(1.0, hem - surf)
                if y < surf + depth and abs(dx) < 0.86:
                    v = 0.62 - dx * 0.32 - (y - surf) * 0.12 + (hash2(x, y, seed + 3) - 0.5) * 0.1
                    col = TR.band(SNOW, v, x, y, 0.22)
                    if col == SNOW[0] and (dx > -0.25 or y > surf + 1):
                        col = SNOW[1]
                else:
                    v = 0.55 - dx * 0.30 - u * 0.45 + (hash2(x, y, seed) - 0.5) * 0.12
                    col = TR.band(NEEDLE, v, x, y, 0.3)
                    if u > 0.82:
                        col = NEEDLE[2] if hash2(x, y, seed + 1) < 0.6 else NEEDLE[1]
                c.set(x, y, col)
        # needle tips hanging off the tier's hem, most at its ends
        for k in range(int(reach)):
            if rnd.random() < 0.5:
                continue
            dx = (rnd.random() * 2 - 1)
            x = int(cx + skew + dx * (left if dx < 0 else right))
            y = int(ty + th * (0.78 + 0.32 * dx * dx)) + 1
            for j in range(1 + rnd.randrange(2)):
                c.set(x, y + j, NEEDLE[2] if j else NEEDLE[1])
    # the leader, with a knob of snow
    c.set(cx, top - 1, NEEDLE[1])
    c.set(cx, top - 2, NEEDLE[1])
    c.set(cx, top - 3, NEEDLE[0])
    c.set(cx - 1, top, SNOW[1])
    c.set(cx, top, SNOW[1])
    drift_foot(c, cx, base, w * 0.40, seed, height=3.0)
    return c


# --- the rimed snag -----------------------------------------------------------------------------

def rimed_snag(seed, w, h):
    """A dead spruce: a grey trunk bare to the stubs of its branches - short, many, drooping, longer near
    the foot - its top snapped off in splinters, and a crust of rime feathered out of one side of
    everything, the side the wind came from. Snow along the tops of the stubs."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    c.anchor_x = cx
    TR.shade_pool(c, cx + w * 0.10, base + 1, w * 0.36, 4, 0.35)
    TR.contact(c, cx, base + 1, 5)
    lean = (rnd.random() - 0.5) * 3.0
    snap = h * (0.06 + rnd.random() * 0.06)
    col_at = TR.trunk(c, cx, base, snap, 10, 3, DEAD, seed, flare=3, lean=lean)
    # the snapped top: splinters of different lengths
    tx = col_at(snap)
    for k in range(3):
        sx = int(tx) - 1 + k
        for j in range(1 + rnd.randrange(5)):
            c.set(sx, int(snap) - j, DEAD[2] if k == 0 else DEAD[3])
    rime = 1 if rnd.random() < 0.5 else -1
    trunk_px = set()
    for yy in range(int(snap), base + 1):
        for xx in range(int(col_at(yy)) - 8, int(col_at(yy)) + 9):
            if c.get(xx, yy)[3]:
                trunk_px.add((xx, yy))
    stubs = []
    y = base - h * 0.14
    while y > snap + 4:
        t = (base - y) / float(base - snap)          # 0 at the foot, 1 at the top
        for side in (-1, 1):
            if rnd.random() < 0.3:
                continue
            # a dead spruce's branches hang: long and drooping near the foot, short at the top
            ln = (w * 0.50) * (1.0 - t * 0.72) * (0.45 + rnd.random() * 0.6)
            if rnd.random() < 0.25:
                ln *= 0.3                             # broken off short
            ang = 0.10 + rnd.random() * 0.25
            droop = 0.018 + rnd.random() * 0.03
            px, py = col_at(y) + side * 2, y
            pts = []
            for j in range(int(ln)):
                a = ang + droop * j
                px += side * math.cos(a)
                py += math.sin(a)
                ix, iy = int(TR.round2(px)), int(TR.round2(py))
                c.set(ix, iy, DEAD[2] if (side < 0 and j % 3) else DEAD[3])
                if j < ln * 0.35:
                    c.set(ix, iy + 1, DEAD[4])
                pts.append((ix, iy))
                # twigs: forking off and hanging down, a few
                if j > 3 and rnd.random() < 0.10:
                    tl = 2 + rnd.randrange(4)
                    for q in range(tl):
                        c.set(ix + side * (q // 2), iy + q, DEAD[3] if q < tl - 1 else DEAD[4])
            stubs.append((side, pts))
        y -= 3 + rnd.randrange(4)
    # RIME: a crust down the windward side of the trunk, feathered out into the wind, and on the windward
    # ends of the branches; snow along the branches' tops. The bone row's shade where the key finds it, steel's
    # shade where it does not - the brightest thing on the snag, and still only the snow's own stops.
    lit_side = rime < 0
    for yy in range(int(snap) + 1, base - 1):
        row = [xx for (xx, y2) in trunk_px if y2 == yy]
        if not row:
            continue
        edge = max(row) if rime > 0 else min(row)
        if hash2(7, yy, seed + 13) < 0.82:
            c.set(edge, yy, SNOW[1] if lit_side else SNOW[2])
            if hash2(9, yy, seed + 15) < 0.55:
                c.set(edge + rime, yy, SNOW[1] if lit_side else SNOW[2])
                if hash2(11, yy, seed + 17) < 0.35:
                    c.set(edge + 2 * rime, yy - 1, SNOW[2])     # a feather grown out into the wind
    for side, pts in stubs:
        n = len(pts)
        for k, (ix, iy) in enumerate(pts):
            if c.get(ix, iy - 1)[3] == 0 and hash2(ix, iy, seed + 5) < 0.6:
                c.set(ix, iy - 1, SNOW[1] if side < 0 else SNOW[2])
            if side == rime and k > n * 0.5 and hash2(ix, iy, seed + 9) < 0.5:
                for q in range(1 + int(2 * hash2(ix, iy, seed + 11))):
                    c.set(ix + rime * (q + 1), iy - q, SNOW[1] if (q == 0 and lit_side) else SNOW[2])
    drift_foot(c, cx, base, w * 0.32, seed, height=3.5)
    return c


# --- granite --------------------------------------------------------------------------------------

def granite_mass(c, cx, cy, rx, ry, seed, snow_top=0.45, sunk=0.0, owner=None):
    """One mass of granite broken into facets (the nearest of a scatter of points, each a plane with its
    own tilted normal, a crease where two meet), rounded by the frost and the weather, never squared; a
    cap of snow draped over whatever faces the sky down to a broken line, a pixel of lit lip along it,
    and the foot sunk in the snow (`sunk`, a share of ry above the bottom the drift comes up to)."""
    rnd = TR.Rng(seed)
    pts = blob_outline(rnd, 8 + rnd.randrange(4), rough=0.30)
    nf = max(5, int(rx * ry / 22))
    facets = []
    for k in range(nf):
        a = rnd.random() * TAU
        d = rnd.random() ** 0.5 * 0.95
        fx, fy = math.cos(a) * d, math.sin(a) * d
        nz = math.sqrt(max(0.05, 1 - fx * fx - fy * fy))
        tx, ty = (rnd.random() - 0.5) * 0.6, (rnd.random() - 0.5) * 0.5
        facets.append((fx * rx + cx, fy * ry + cy, fx + tx, fy + ty, nz))
    snow_phase = rnd.random() * TAU
    for y in range(int(cy - ry * 1.3) - 2, int(cy + ry * 1.3) + 3):
        dy = (y - cy) / float(ry)
        for x in range(int(cx - rx * 1.3) - 2, int(cx + rx * 1.3) + 3):
            dx = (x - cx) / float(rx)
            th = math.atan2(dy, dx)
            edge = outline_r(pts, th) + (hash2(x // 2, y // 2, seed + 11) - 0.5) * 0.07
            rr = math.sqrt(dx * dx + dy * dy)
            if rr > edge:
                continue
            if sunk and dy > 1.0 - sunk + 0.12 * math.sin(x * 0.4 + seed) + 0.1 * hash2(x // 2, 1, seed):
                continue                                     # under the drift
            best = second = 1e9
            fbest = None
            for f in facets:
                d2 = (x - f[0]) ** 2 + ((y - f[1]) * 1.2) ** 2
                if d2 < best:
                    second, best, fbest = best, d2, f
                elif d2 < second:
                    second = d2
            nx, ny, nz = fbest[2], fbest[3], fbest[4]
            v = 0.46 + 0.5 * (nx * KX + ny * KY + nz * KZ)
            v += (hash2(x, y, seed) - 0.5) * 0.05
            if hash2(x, y, seed + 19) < 0.035:
                v -= 0.18                                     # a pit the frost opened
            # the snow lies where the surface faces the sky, down to a ragged line
            line = -1.0 + snow_top * 1.6 + 0.16 * math.sin(x * 0.33 + snow_phase) + (hash2(x // 2, 2, seed + 23) - 0.5) * 0.2
            on_snow = (dy < line) and (ny < 0.15 or dy < line - 0.25)
            if on_snow:
                sv = 0.55 - dx * 0.25 - (dy - (line - 0.3)) * 0.4 + (hash2(x, y, seed + 31) - 0.5) * 0.1
                col = TR.band(SNOW, sv, x, y, 0.22)
                if col == SNOW[0] and (dx > -0.1 or hash2(x, y, seed + 37) < 0.5):
                    col = SNOW[1]
                # the snow's lower edge: a lip of shadow where it overhangs the rock
                if dy > line - 0.12:
                    col = STL[3] if dx < 0 else STL[4]
            else:
                col = TR.band(ROCK, v, x, y, 0.22)
                if dy < line + 0.14 and v > 0.4:
                    col = ROCK[3]                             # the shadow under the snow's lip
                if math.sqrt(second) - math.sqrt(best) < 0.9 and v < 0.75 and hash2(x, y, seed + 23) < 0.7:
                    col = ROCK[4] if v > 0.3 else OCC         # a crease between facets
                if rr > edge * 0.86 and (dx * 0.55 + dy * 0.83) > 0.42 * edge:
                    col = OCC if hash2(x, y, seed + 3) < 0.55 else ROCK[4]
            c.set(x, y, col)
            if owner is not None and not on_snow:
                owner[(x, y)] = v


def frost_split(c, owner, n, seed, length):
    """Joints the frost opened: occlusion running down and wandering, a lit edge on the key side, and
    snow lodged in the top of the wider ones."""
    rnd = TR.Rng(seed)
    pts = sorted(owner.keys())
    if not pts:
        return
    for k in range(n):
        x, y = pts[rnd.randrange(len(pts))]
        ln = length // 2 + rnd.randrange(length)
        if c.get(x, y - 1)[3] and (x, y - 1) not in owner:
            pass
        for j in range(ln):
            if (x, y) not in owner:
                break
            c.set(x, y, OCC)
            if (x - 1, y) in owner and owner[(x - 1, y)] > 0.4:
                c.set(x - 1, y, ROCK[1])
            if j == 0:
                c.set(x, y - 1, SNOW[1])
            y += 1
            r = rnd.random()
            if r < 0.28:
                x += 1
            elif r < 0.5:
                x -= 1


def boulder(seed, w, h, masses, sunk=0.3, drift_half=None):
    """Frost-split granite half buried in snow: masses back to front (x, y, rx, ry as shares of the box,
    snow_top), their joints, the cold shade to the lower right, and the drift heaped round the foot."""
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    TR.shade_pool(c, cx + w * 0.14, base + 1, w * 0.46, 4 + w * 0.03, 0.45)
    c.disc(cx, base, w * 0.34, 1.8, OCC)
    owner = {}
    for k, (fx, fy, frx, fry, top) in enumerate(masses):
        granite_mass(c, w * fx, h * fy, w * frx, h * fry, seed + 13 * k, snow_top=top,
                     sunk=sunk if fy + fry > 0.8 else 0.0, owner=owner)
    frost_split(c, owner, 2 + w // 24, seed + 99, max(4, h // 7))
    drift_foot(c, cx - w * 0.04, base, drift_half or w * 0.50, seed, height=3.0 + h * 0.06)
    return c


# --- the shrubs -----------------------------------------------------------------------------------

def dwarf_birch(seed, w, h):
    """Dwarf birch under the snow: a low tangle of dark twigs poking up out of a cushion of drift, snow
    caught in the forks, a dead leaf or two still on it."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + 3)
    base = h - 1
    cx = w // 2
    c.disc(cx, base, w * 0.42, 1.8, STL[4])
    drift_foot(c, cx, base, w * 0.48, seed, height=h * 0.32)
    tips = []
    for k in range(7 + rnd.randrange(4)):
        x0 = cx + rnd.randrange(-w // 3, w // 3 + 1)
        a = -math.pi / 2 + (x0 - cx) / float(w) * 1.6 + (rnd.random() - 0.5) * 0.7
        TR.limb(c, x0, base - h * 0.22, a, h * (0.45 + rnd.random() * 0.35), 2, DEAD, 1, rnd, tips,
                rise=TR.TWIG_RISE, thin=True)
    for (px, py) in tips:
        r = rnd.random()
        if r < 0.45:
            c.set(int(px), int(py) - 1, SNOW[1] if px < cx else SNOW[2])
            c.set(int(px) + 1, int(py) - 1, SNOW[2])
        elif r < 0.55:
            c.set(int(px), int(py), GO[3])
    return c


def juniper(seed, w, h):
    """A juniper cushion: a low dark mound of scale-leaf, a cap of snow on its crown sliding off the
    lit side, a fringe of needle tips under the cap, sunk in drift."""
    c = TR.Bleed(w, h + 3)
    base = h - 1
    cx = w // 2
    c.disc(cx, base, w * 0.44, 1.8, OCC)
    rnd = TR.Rng(seed)
    clusters = []
    for k in range(8 + rnd.randrange(4)):
        a = math.pi + rnd.random() * math.pi
        d = rnd.random() ** 0.6
        x = cx + math.cos(a) * d * (w * 0.38)
        y = base - 2 + math.sin(a) * d * (h - 4)
        r = 3 + rnd.random() * 3
        clusters.append((x, y, r, r * 0.8))
    clusters.sort(key=lambda k: k[1])
    TR.crown(c, clusters, [VE[3], VE[4], OCC, OCC], cx, base - h * 0.5, w * 0.5, h * 0.5, seed, ragged=0.5)
    # the snow lies on its crown in two or three small caps, not one lid: the scale-leaf shows between them
    hem = []
    for k in range(2 + rnd.randrange(2)):
        sx = cx - w * 0.22 + k * w * 0.2 + (rnd.random() - 0.5) * 3
        snow_lump(c, sx, base - h * (0.60 + rnd.random() * 0.15), w * (0.12 + rnd.random() * 0.07), h * 0.20,
                  seed + 5 + k, cx, base - h * 0.5, w * 0.5, h * 0.5, bias=-0.02, hem=hem)
    needle_tips(c, [p for p in hem if hash2(int(p[0]), 1, seed) < 0.4], seed, cx)
    drift_foot(c, cx, base, w * 0.5, seed, height=2.5)
    return c


# (id, kind, painter, trunk footprint in WORLD px or 0, rows under the ground line) - trees.py's shape.
FOLIAGE = [
    ("rime-spruce-1", "tree", lambda: rime_spruce(501, 54, 124, lean=0.6, hook=1.0), 18, TR.FOOT),
    ("rime-spruce-2", "tree", lambda: rime_spruce(509, 46, 108, lean=-0.4, hook=-1.0), 16, TR.FOOT),
    ("rime-spruce-3", "tree", lambda: rime_spruce(517, 60, 100, lean=0.2, hook=0.6), 18, TR.FOOT),
    ("snow-spruce-1", "tree", lambda: snow_spruce(523, 50, 112, tiers=10), 14, TR.FOOT),
    ("snow-spruce-2", "tree", lambda: snow_spruce(529, 44, 94, tiers=8), 12, TR.FOOT),
    ("rimed-snag-1", "tree", lambda: rimed_snag(533, 52, 90), 14, TR.FOOT),
    ("rimed-snag-2", "tree", lambda: rimed_snag(541, 44, 76), 12, TR.FOOT),
    # masses (x, y, rx, ry as shares of the box, snow_top), back to front
    ("frost-boulder-1", "tree", lambda: boulder(551, 58, 40, [(0.40, 0.46, 0.32, 0.40, 0.55),
                                                              (0.68, 0.62, 0.24, 0.30, 0.45),
                                                              (0.30, 0.76, 0.18, 0.18, 0.40)]), 46, TR.FOOT),
    ("frost-boulder-2", "tree", lambda: boulder(557, 42, 32, [(0.48, 0.54, 0.40, 0.42, 0.50),
                                                              (0.80, 0.80, 0.14, 0.14, 0.40)]), 34, TR.FOOT),
    ("frost-boulder-3", "tree", lambda: boulder(563, 30, 18, [(0.50, 0.60, 0.42, 0.50, 0.55)], sunk=0.45), 24, TR.FOOT),
    ("snow-shrub-1", "bush", lambda: dwarf_birch(571, 30, 20), 0, 3),
    ("snow-shrub-2", "bush", lambda: juniper(577, 32, 16), 0, 3),
    ("snow-shrub-3", "bush", lambda: dwarf_birch(583, 22, 15), 0, 3),
]


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    ok = True
    rows = []
    shots = []
    for (name, kind, fn, footprint, foot) in FOLIAGE:
        row = TR.save_grounded(fn(), name, kind, foot, footprint, OUT, RES)
        if row is None:
            ok = False
            continue
        rows.append(row)
        shots.append(Image.open(os.path.join(OUT, name + ".png")).convert("RGBA"))
    manifest = ('{\n'
                '  "_comment": "Generated by tools/art/props_frozen.py: the Frozen Waste\'s foliage and stone, in trees.json\'s '
                'shape. ground is the contact line and anchor_x the trunk column, in pixels of the PNG; trunk is the width in '
                'world pixels of the only part that blocks (0: walk through it).",\n'
                '  "sprites": [\n%s\n  ]\n}\n' % ",\n".join(rows))
    TR.write_text(os.path.join(OUT, "frozen.json"), manifest)
    print("%d sprites in frozen.json%s" % (len(rows), "" if ok else " - WITH PALETTE ERRORS"))
    # A preview on the snow's floor tone (the bone row's deep), with the Forest's fir beside them for scale.
    p = os.path.join(ROOT, "assets", "bonelight", "world", "trees", "fir-1.png")
    if os.path.exists(p):
        shots.append(Image.open(p).convert("RGBA"))
    width = sum(s.size[0] for s in shots) + 16 * len(shots) + 16
    height = max(s.size[1] for s in shots) + 32
    sheet = Image.new("RGBA", (width, height), raster.rgb(SK[4]))
    x = 16
    for s in shots:
        sheet.alpha_composite(s, (x, height - 16 - s.size[1]))
        x += s.size[0] + 16
    sheet.resize((sheet.size[0] * 2, sheet.size[1] * 2), Image.NEAREST).save(PREVIEW)
    print("preview: tools/art/_props_frozen_preview.png")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
