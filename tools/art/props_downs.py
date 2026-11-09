# -*- coding: utf-8 -*-
"""The White Downs' standing things (.ai/second-half.md 4.2, F3). Run: python tools/art/props_downs.py

Writes assets/bonelight/world/downs/<id>.png (authored 1:1, saved at x2 like every world sprite) and
downs.json beside them, in trees.json's shape: each sprite's ground line and anchor column in pixels of the
PNG, and the width in world pixels of the only part that blocks (`trunk`; 0 walks through). The chapter's
foliage (scripts/Foliage.WhiteDowns.cs) names the manifest and scatters them through the grounded-decor
path, so they sort at their feet and stop the wizard's boots, not his chest (.ai/terrain.md 10). Also
writes tools/art/_downs/props_preview.png, every sprite stood on the downs' turf (ignored by git).

THE DOWNS ARE OPEN, AND THEIR SIGHTLINES LONG. So the ladder is short at the top: the tallest thing on them
is a beech of a hilltop clump, 190-230 px - two thirds of the Forest's 300 px oaks - and everything else is
at or under a man's height:

  beech-1, -2           a hilltop clump's beeches: a smooth grey bole that forks, a high dark dome of leaf,
                        roots spread over the chalk at its foot                                   blocks
  hawthorn-1, -2        a downland thorn: a short knot of stems under a dense, rounded crown with a flat
                        underside where the sheep have browsed it to one height (the BROWSE LINE) - which is
                        what tells it from the heath's wind-cut thorns, shorn flat on TOP         blocks
  juniper-1, -2         dark juniper: a ragged column of blue-green needle, and a spreading one    walk through
  sarsen-1, -2, -3      grey wethers: low rounded sarsen boulders lying in the turf like resting sheep,
                        pale lichened grey, knee-high, nothing upright                            blocks
  flint-heap-1, -2      flints picked off a field and heaped at its edge                           walk through
  chalk-outcrop-1..3    the chalk breaking through the turf in low humps                          walk through

THE CONTRACT, AND THE DOWNS' OWN RULES. Seen a little from above, never the subject: no actor rim, a heavy
contact shadow, leaf on verdant's dark half (on lichen's for the juniper's blue-green), nothing above a
`shade` stop brighter than the bone row's for the pale stone. Nothing warm and nothing that glints - no haws
on the thorns, no gold lichen on the stones (a warm fleck is a secret's tell, .ai/stage-secrets.md). Nothing
deer-shaped or hound-shaped, and nothing long and pale lying on the turf: the downs' two chalk figures are
secrets, and a line of chalk anywhere else would be a false trail to them.
"""
from __future__ import print_function

import io
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import bonelight as bl
import raster
import trees as T

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "downs")
RES = "res://assets/bonelight/world/downs"
FOOT = 4          # authored rows below a tree's ground line: room for the contact shadow

ST = T.ST
SK = T.SK
LN = T.LN
LI = T.LI
VE = T.VE
STL = T.ramp("steel")

# Beech bark: smooth and silver-grey - the bone row's shade and deep on the lit side, the stone row's lit and
# base beyond - so the bole is the pale grey of a beech and still a stop under the sarsens' lit tops.
BEECH_BARK = [SK[3], SK[3], SK[4], ST[1], ST[2]]
# Leaf at night: verdant's dark half. A beech's dome a stop darker than the Forest's oaks, which carry its lit.
BEECH_LEAF = [VE[2], VE[3], VE[4], OCC]
# A thorn's crown, darker again: a dense thing, and on a downland at night nearly black.
THORN_LEAF = [VE[3], VE[3], VE[4], OCC]
# Thorn wood: dark, grey-brown, gnarled - linen's lower stops.
THORN_BARK = [LN[2], LN[2], LN[3], LN[4], OCC]
# Juniper: blue-green needle, so lichen's row, its shade and deep, with verdant's deep in the hollows.
JUNIPER = [LI[2], LI[3], LI[4], OCC]
# Sarsen, lit to dark: bone's shade and deep (pale grey), then the stone row's lit and base underneath.
SARSEN = [SK[3], SK[4], ST[1], ST[2], OCC]
# Chalk, lit to dark: bone's shade and deep, steel's deep, the stone row's base.
CHALK = [SK[3], SK[4], STL[4], ST[2], OCC]


def hash2(x, y, s):
    return T.hash2(x, y, s)


def crown(c, clusters, tones, ccx, ccy, crx, cry, seed, ragged=0.35, gaps=0.0, bias=-0.04):
    """trees.crown, lit from the upper left.

    trees.crown mixes each pixel's brightness from -(n . L), where L = (trees.LX, trees.LY) already points
    TOWARD the key (up and left): so it lights each cluster and the whole crown from the lower right, and only
    its glints and seams (which test the sign the other way) sit on the upper left. Every crown drawn with it
    so far carries that (the Forest's oaks are brightest on their lower right). It is shared and not this
    chapter's to change, so the downs' trees use this copy, which differs only in that sign - a beech or a
    thorn here is lit as the contract says, on its upper left."""
    LX, LY = T.LX, T.LY
    rim = tones[-1]
    for (x0, y0, rx, ry) in clusters:
        for y in range(int(y0 - ry) - 1, int(y0 + ry) + 2):
            for x in range(int(x0 - rx) - 1, int(x0 + rx) + 2):
                nx = (x - x0) / float(rx)
                ny = (y - y0) / float(ry)
                d = nx * nx + ny * ny
                if d > 1.0 - ragged * hash2(x, y, seed + 7) * 0.5:
                    continue
                if gaps and hash2(x // 2, y // 2, seed + 11) < gaps * d:
                    continue
                local = (nx * LX + ny * LY) * (1.0 - d * 0.35)
                gx = (x - ccx) / float(crx)
                gy = (y - ccy) / float(cry)
                glob = (gx * LX + gy * LY) * 0.8 - max(0.0, gy) * 0.35
                v = 0.5 + bias + local * 0.30 + glob * 0.34
                v += (hash2(x, y, seed) - 0.5) * 0.10
                col = T.band(tones, v, x, y)
                if d > 0.62 and nx * 0.6 + ny * 0.8 > 0.55:
                    col = rim if hash2(x, y, seed + 3) < 0.7 else tones[-2]
                c.set(x, y, col)
        if rx >= 4 and (x0 - ccx) * LX + (y0 - ccy) * LY > -crx * 0.15:
            gx0 = int(x0 - rx * 0.35)
            gy0 = int(y0 - ry * 0.45)
            c.set(gx0, gy0, tones[0])
            c.set(gx0 + 1, gy0, tones[0] if rx > 7 else tones[1])


def turf_at_foot(c, x0, x1, base, seed, density=0.55):
    """The downs' cropped turf grown up round a foot: short blades in the floor's two greens, a tip lit."""
    rnd = T.Rng(seed)
    for x in range(int(x0), int(x1) + 1):
        if rnd.random() < density:
            h = rnd.randrange(1, 3)
            for j in range(h):
                c.set(x, base - j, (LI[4] if x % 2 else VE[4]) if j < h - 1 else (LI[3] if rnd.random() < 0.5 else VE[3]))


# --- the beech -----------------------------------------------------------------------------------------------

def beech(seed, w, h, trunk_w, fork=0.5):
    """A beech of a hilltop clump: a smooth grey bole that forks into two or three limbs under a high dark
    dome, roots spread over the chalk at its foot. Built on trees.broadleaf's crown and trunk painters with
    the beech's bark and a darker leaf, the dome set high (the bole is half the tree)."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + FOOT)
    base = h - 1
    cx = w // 2
    crown_h = h * 0.6
    ccy = 2 + crown_h * 0.5
    crx = w * 0.5 - 3
    cry = crown_h * 0.5
    T.shade_pool(c, cx + w * 0.08, base, w * 0.34, 6 + w * 0.03, density=0.55)
    T.contact(c, cx, base + 1, trunk_w * 0.9)
    # Roots spread over the chalk: a beech on thin chalk soil holds the ground with its roots on top of it.
    for s in (-1, 1, -1, 1):
        a = (math.pi if s < 0 else 0.0) + s * (0.15 + rnd.random() * 0.25)
        T.limb(c, cx + s * trunk_w * 0.3, base - 1, a, trunk_w * (0.7 + rnd.random() * 0.6), 3, BEECH_BARK, 0, rnd,
               droop=0.25 * s)
    top = ccy + cry * 0.2
    trunk_x = T.trunk(c, cx, base, top, trunk_w, trunk_w * 0.6, BEECH_BARK, seed, flare=trunk_w * 0.3)
    # The fork: two or three limbs from the top of the bole, smooth and grey, up into the dome.
    fy = base - (base - top) * fork
    for s in (-1, 1) + ((0,) if rnd.random() < 0.5 else ()):
        a = -math.pi / 2 + s * (0.35 + rnd.random() * 0.25)
        T.limb(c, trunk_x(fy), fy, a, (fy - ccy) * 0.95, trunk_w * 0.5, BEECH_BARK, 1, rnd)
    clusters = T.fill_clusters(rnd, cx, ccy, crx - 9, cry - 7, int(w * 0.55), 6, 12, top_bias=0.2)
    crown(c, clusters, BEECH_LEAF, cx, ccy, crx, cry, seed, ragged=0.35, gaps=0.03, bias=-0.14)
    # The dome's underside, where the limbs show through the thinning leaf.
    for k in range(int(w * 0.5)):
        x = int(cx - crx * 0.6 + rnd.random() * crx * 1.2)
        y = int(ccy + cry * (0.55 + rnd.random() * 0.3))
        if c.get(x, y)[3] and hash2(x, y, seed) < 0.5:
            c.set(x, y, VE[4] if hash2(x, y, seed + 1) < 0.6 else OCC)
    # A little chalk turned up among the roots, and the turf round them.
    for k in range(5):
        x = cx + rnd.randrange(-int(trunk_w * 1.4), int(trunk_w * 1.4) + 1)
        y = base + rnd.randrange(-1, 2)
        if not c.get(x, y)[3] or c.get(x, y)[:3] == raster.rgb(OCC)[:3]:
            c.set(x, y, SK[4])
            c.set(x + 1, y + 1, OCC)
    turf_at_foot(c, cx - trunk_w * 2, cx + trunk_w * 2, base - 1, seed + 3, 0.3)
    c.anchor_x = cx
    return c


# --- the hawthorn -----------------------------------------------------------------------------------------------

def hawthorn(seed, w, h, lean=1.0):
    """A downland hawthorn: two or three gnarled stems from one root, leaning a little, under a dense rounded
    crown whose underside is cut level - the browse line, as high as a sheep can reach. Rounded on top, flat
    beneath: the heath's wind-cut thorn is the other way up (flat on top, streaming to leeward)."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + FOOT)
    base = h - 1
    cx = int(w * 0.46)
    c.anchor_x = cx
    T.shade_pool(c, cx + w * 0.14, base, w * 0.4, 5, density=0.5)
    T.contact(c, cx, base + 1, 5)
    browse = base - h * 0.34                      # the level the sheep have cropped it to
    # the stems: a main one and a second splaying off it, gnarled (the trunk's lean, a limb's droop)
    top = browse - h * 0.08
    tx = T.trunk(c, cx, base, top, 6, 4, THORN_BARK, seed, flare=2.0, lean=lean)
    T.limb(c, cx + 1, base - h * 0.12, -math.pi / 2 + 0.55, h * 0.3, 3, THORN_BARK, 0, rnd)
    if rnd.random() < 0.6:
        T.limb(c, cx - 1, base - h * 0.18, -math.pi / 2 - 0.6, h * 0.24, 2, THORN_BARK, 0, rnd)
    # the crown: a dome of clusters wider than it is tall, everything below the browse line cut away
    ccx, ccy = tx(top) + w * 0.03, browse - h * 0.24
    rx, ry = w * 0.46, h * 0.32
    clusters = T.fill_clusters(rnd, ccx, ccy, rx - 5, ry - 3, int(w * 0.55), 4, 8)
    kept = []
    for (x, y, crx, cry) in clusters:
        if y + cry > browse:
            y = browse - cry                      # held to the browse line
        kept.append((x, y, crx, cry))
    kept.sort(key=lambda k: k[1] + k[2] * 0.3)
    crown(c, kept, THORN_LEAF, ccx, ccy, rx, ry, seed, ragged=0.4, gaps=0.05, bias=-0.22)
    # The browse line itself: level, a little ragged, the leaf over it dark where it overhangs.
    for x in range(int(ccx - rx) - 2, int(ccx + rx) + 3):
        for y in range(int(browse) - 1, int(browse) + 4):
            p = c.get(x, y)
            if p[3] and y >= browse + (1 if hash2(x, 0, seed) < 0.5 else 0):
                if p[:3] != raster.rgb(THORN_BARK[1])[:3] and p[:3] != raster.rgb(THORN_BARK[2])[:3] \
                        and p[:3] != raster.rgb(THORN_BARK[3])[:3]:
                    c.set(x, y, (0, 0, 0, 0))
        y = int(browse) - (0 if hash2(x, 1, seed) < 0.6 else 1)
        if c.get(x, y)[3]:
            c.set(x, y, OCC)
    turf_at_foot(c, cx - 7, cx + 8, base - 1, seed + 5, 0.45)
    return c


# --- the juniper ------------------------------------------------------------------------------------------------

def juniper(seed, w, h, spread=False):
    """Juniper: dark blue-green needle in a ragged column, or, spread, a low mound wider than tall. Lichen's
    row (its shade and deep) where the thorns and beeches are verdant's, so a juniper among them is the
    bluer, colder dark. Short strokes of needle on its lit side, never a glint."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + 3)
    base = h - 1
    cx = w / 2.0
    c.disc(cx, base, w * 0.4, 2.0, OCC)
    clusters = []
    if spread:
        for k in range(int(w * 0.6)):
            a = math.pi + rnd.random() * math.pi
            d = rnd.random() ** 0.6
            clusters.append((cx + math.cos(a) * d * (w * 0.5 - 4), base - 3 + math.sin(a) * d * (h - 5),
                             3 + rnd.random() * 3.0, 0))
    else:
        # a column: lobes stacked up a leaning axis, narrowing to a ragged point
        lean = (rnd.random() - 0.5) * 3.0
        for k in range(int(h * 0.9)):
            t = rnd.random() ** 0.8
            y = base - 3 - t * (h - 6)
            half = (w * 0.5 - 3) * (1.0 - t * 0.7)
            x = cx + lean * t + (rnd.random() - 0.5) * 2.0 * half
            clusters.append((x, y, 2.5 + rnd.random() * 2.5 * (1.0 - t * 0.5), 0))
    clusters = [(x, y, r, r * 1.1) for (x, y, r, _z) in clusters]
    clusters.sort(key=lambda k: k[1])
    crown(c, clusters, JUNIPER, cx, base - h * 0.5, w * 0.5, h * 0.5, seed, ragged=0.5, gaps=0.1, bias=-0.1)
    # needle: short diagonal strokes on the lit side, a stop up from the cluster under them
    for k in range(int(w * h * 0.04)):
        x = int(cx - w * 0.4 + rnd.random() * w * 0.5)
        y = int(base - rnd.random() * (h - 3))
        if c.get(x, y)[3] and c.get(x + 1, y - 1)[3]:
            c.set(x, y, LI[3])
            c.set(x + 1, y - 1, LI[2] if hash2(x, y, seed) < 0.4 else LI[3])
    # the hollows, where verdant's deep shows through
    for k in range(int(w * h * 0.03)):
        x = int(cx - w * 0.2 + rnd.random() * w * 0.6)
        y = int(base - rnd.random() * (h - 3))
        if c.get(x, y)[3]:
            c.set(x, y, VE[4])
    c.anchor_x = int(cx)
    return c


# --- stone and chalk --------------------------------------------------------------------------------------------

def lump(c, cx, cy, rx, ry, seed, ramp, tilt=0.0, p=2.2, lit=0.0):
    """One rounded lump - a sarsen, a hump of chalk: an outline that wanders by three low harmonics of its
    seed, a little flatter than an ellipse, lit from the upper left on its top and shoulder and turning into
    occ underneath, with a mottle three pixels across so a face is not one flat tone. props_heath.pillow's
    method, on a ramp of the caller's. Returns its pixels, {(x, y): (nx, ny)}."""
    rnd = T.Rng(seed)
    harm = [(k, (0.11 / k) * (0.5 + rnd.random()), rnd.random() * 6.283) for k in (2, 3, 5)]
    pts = {}
    for y in range(int(cy - ry * 1.3) - 1, int(cy + ry * 1.3) + 2):
        for x in range(int(cx - rx * 1.4) - 2, int(cx + rx * 1.4) + 3):
            dy = (y + 0.5 - cy) / ry
            dx = (x + 0.5 - cx) / rx - tilt * dy
            a = math.atan2(dy, dx)
            r = 1.0 + sum(amp * math.sin(k * a + ph) for (k, amp, ph) in harm)
            d = (abs(dx) ** p + abs(dy) ** p) ** (1.0 / p) / r
            if d <= 1.0:
                pts[(x, y)] = (dx / r, dy / r, d)
    for (x, y) in sorted(pts):
        nx, ny, d = pts[(x, y)]
        nz = math.sqrt(max(0.0, 1.0 - d * d))
        sky = max(0.0, -ny - 0.3)
        v = 0.3 + lit + (-0.55 * nx - 0.45 * ny) * 0.5 + sky * 0.5 + nz * 0.08
        v += (hash2(x // 3, y // 3, seed + 5) - 0.5) * 0.14
        tone = T.band(ramp[:4], v, x, y, 0.3)
        if any((x + ex, y + ey) not in pts for (ex, ey) in ((1, 0), (-1, 0), (0, 1), (0, -1))):
            if nx * 0.5 + ny * 0.86 > 0.2:
                tone = OCC if ny > 0.3 else ramp[3]
        c.set(x, y, tone)
    return dict((k, (v[0], v[1])) for (k, v) in pts.items())


def lichen(c, pts, rnd, cx, cy, n):
    """Crusts of lichen on a stone's top and weather side: lichen's base and shade, grey-green. No gold."""
    for k in range(n):
        x, y = cx + rnd.randrange(-4, 5), cy + rnd.randrange(-2, 3)
        for (dx, dy, tone) in ((0, 0, LI[2]), (1, 0, LI[3]), (0, 1, LI[3]), (-1, 0, LI[2]), (1, 1, LI[3])):
            if (x + dx, y + dy) in pts:
                c.set(x + dx, y + dy, tone)


def sarsen(seed, w, h, second=True):
    """A grey wether: a sarsen boulder lying in the turf like a resting sheep - long, low and rounded,
    smoother than the heath's granite (sarsen is a sandstone the weather has polished, not a jointed rock),
    pale lichened grey, knee-high. Now and then a second, smaller one lies against it. Nothing upright."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + FOOT)
    base = h - 1
    cx = w / 2.0
    T.shade_pool(c, cx + w * 0.12, base + 1, w * 0.5, 3.0, density=0.45)
    T.contact(c, cx + 1, base + 1, w * 0.45)
    main = lump(c, cx - w * 0.05, base - h * 0.44, w * 0.44, h * 0.48, seed, SARSEN,
                tilt=(rnd.random() - 0.5) * 0.3, p=2.0, lit=0.0)
    lichen(c, main, rnd, int(cx - w * 0.15), int(base - h * 0.62), 3 + rnd.randrange(0, 2))
    lichen(c, main, rnd, int(cx + w * 0.18), int(base - h * 0.4), 1 + rnd.randrange(0, 2))
    # the hollows weathered in its top, as a sarsen's are
    for k in range(rnd.randrange(1, 3)):
        hx, hy = int(cx - w * 0.1 + rnd.randrange(-3, 4)), int(base - h * 0.7)
        if (hx, hy) in main and (hx + 1, hy) in main:
            c.set(hx, hy, ST[2])
            c.set(hx + 1, hy, ST[1])
    if second and rnd.random() < 0.8:
        side = 1 if rnd.random() < 0.6 else -1
        small = lump(c, cx + side * w * 0.36, base - h * 0.2, w * 0.17, h * 0.24, seed + 3, SARSEN, p=2.0)
        lichen(c, small, rnd, int(cx + side * w * 0.36), int(base - h * 0.3), 1)
    turf_at_foot(c, 1, w - 2, base, seed + 5, 0.5)
    c.anchor_x = int(cx)
    return c


def flint_nodule(c, x, y, size, seed, knapped):
    """One picked flint, from the side: a knobbly nodule in its pale cortex, or knapped, the black glass inside
    showing on its broken face (steel's deep, a facet of its shade) - never a glint on it."""
    rnd = T.Rng(seed)
    pts = set()
    for j in range(2 + rnd.randrange(0, 2)):
        lx = x + (rnd.random() - 0.5) * size * 1.4
        ly = y + (rnd.random() - 0.5) * size * 0.8
        r = size * (0.6 + rnd.random() * 0.5)
        for yy in range(int(ly - r) - 1, int(ly + r) + 2):
            for xx in range(int(lx - r) - 1, int(lx + r) + 2):
                if (xx + 0.5 - lx) ** 2 + ((yy + 0.5 - ly) / 0.85) ** 2 <= r * r:
                    pts.add((xx, yy))
    for (xx, yy) in pts:
        ul = (xx - 1, yy) not in pts or (xx, yy - 1) not in pts
        lr = (xx + 1, yy) not in pts or (xx, yy + 1) not in pts
        if knapped and (xx - x) + (yy - y) > 0:
            tone = OCC if lr else (STL[3] if (xx + yy) % 3 == 0 else STL[4])
        else:
            tone = SK[3] if ul and (xx * 3 + yy) % 4 else (ST[2] if lr else SK[4])
        c.set(xx, yy, tone)
    return pts


def flint_heap(seed, w, h, n):
    """Flints picked off a field and thrown in a heap at its edge: a low pile of nodules, the back ones higher,
    the front ones spilt, some broken. Walked over: it is knee-high at most."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + 3)
    base = h - 1
    cx = w / 2.0
    c.disc(cx + 1, base, w * 0.46, 1.8, OCC)
    items = []
    for k in range(n):
        t = rnd.random()
        u = (rnd.random() - 0.5) * 2.0
        hump = (1.0 - u * u) * (h - 4)
        items.append((cx + u * (w * 0.42), base - 2 - t * hump, 1.4 + rnd.random() * 1.6, k))
    items.sort(key=lambda q: q[1])
    for (x, y, size, k) in items:
        pts = flint_nodule(c, x, y, size, seed + 13 * k, knapped=(k % 3 == 1))
        for (xx, yy) in pts:
            if (xx, yy + 1) not in pts and not c.get(xx + 1, yy + 1)[3]:
                c.set(xx + 1, yy + 1, OCC)
    turf_at_foot(c, 1, w - 2, base, seed + 7, 0.35)
    c.anchor_x = int(cx)
    return c


def chalk_outcrop(seed, w, h, n):
    """The chalk breaking through the turf: `n` low humps run together, most of them still under the sward,
    which grows over their edges. Pale - bone's deep with its shade on the tops the key finds - and broken,
    a flint or two weathering out of it. Walked over, like the turf it shows through. Low and wide on purpose,
    and lumpy: never a line of chalk (the downs' chalk figures are secrets)."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + 3)
    base = h - 1
    cx = w / 2.0
    c.disc(cx + 1, base, w * 0.45, 1.2, OCC)
    # Flat, broken slabs of it at the turf's level - not boulders (that is the sarsens, which stand up out of
    # the turf, smooth and rounded): wider than they are high, squarer than an ellipse, several run together
    # and the gaps between them crumbling. Whiter than a sarsen: lit on the whole of its flat top.
    # one broad slab in the middle and the rest round it in two dimensions, so an outcrop is a clump and
    # never a row (a row of chalk slabs read as a kerb, and a line of chalk is the secrets')
    spots = [(0.0, 0.0, 0.26, 0.5)]
    for k in range(n + 2):
        a = rnd.random() * 6.283
        d = 0.55 + rnd.random() * 0.45
        spots.append((math.cos(a) * d * 0.36, math.sin(a) * d * 0.3, 0.08 + rnd.random() * 0.08,
                      0.22 + rnd.random() * 0.18))
    spots.sort(key=lambda q: q[1])
    humps = []
    for k, (ox, oy, sx, sy) in enumerate(spots):
        rx, ry = w * sx, h * sy
        humps.append(lump(c, cx + ox * w, base - h * 0.42 + oy * h - ry * 0.2, rx, ry, seed + 13 * k, CHALK,
                          tilt=(rnd.random() - 0.5) * 0.4, p=2.6, lit=0.26))
    # crumbled chalk round and between the slabs, and a hairline crack across the largest
    for k in range(int(w * 0.5)):
        x = int(w * 0.06 + rnd.random() * w * 0.88)
        y = base - rnd.randrange(0, max(2, h // 3))
        if not c.get(x, y)[3] or rnd.random() < 0.3:
            c.set(x, y, SK[4] if k % 3 else SK[3])
            if not c.get(x + 1, y + 1)[3]:
                c.set(x + 1, y + 1, OCC)
    big = max(humps, key=len)
    if big:
        x, y = sorted(big)[len(big) // 3]
        for j in range(rnd.randrange(4, 8)):
            x += 1
            y += rnd.choice((0, 0, 1, -1))
            if (x, y) in big:
                c.set(x, y, ST[2])
    # flints weathering out of the chalk
    for k in range(rnd.randrange(1, 3)):
        hp = humps[rnd.randrange(len(humps))]
        if not hp:
            continue
        x, y = sorted(hp)[len(hp) // 2]
        flint_nodule(c, x + rnd.randrange(-2, 3), y + rnd.randrange(-1, 2), 1.1, seed + 41 + k, knapped=(k == 0))
    # the turf over its edges: blades along the foot and a few up its flanks
    turf_at_foot(c, 1, w - 2, base, seed + 9, 0.6)
    for k in range(rnd.randrange(4, 8)):
        x = rnd.randrange(2, w - 2)
        y = base - rnd.randrange(1, max(2, h // 2))
        if c.get(x, y)[3]:
            c.set(x, y, VE[4] if k % 2 else LI[4])
            c.set(x, y - 1, LI[3] if k % 3 else VE[3])
    c.anchor_x = int(cx)
    return c


# (id, kind, painter, footprint in WORLD px or 0 for none) - as trees.SPRITES.
SPRITES = [
    ("beech-1", "tree", lambda: beech(601, 104, 116, 9, fork=0.55), 22),
    ("beech-2", "tree", lambda: beech(613, 88, 98, 8, fork=0.5), 20),
    ("hawthorn-1", "tree", lambda: hawthorn(621, 60, 62, lean=1.6), 14),
    ("hawthorn-2", "tree", lambda: hawthorn(633, 50, 52, lean=-1.2), 12),
    ("juniper-1", "bush", lambda: juniper(641, 22, 34), 0),
    ("juniper-2", "bush", lambda: juniper(647, 32, 20, spread=True), 0),
    ("sarsen-1", "tree", lambda: sarsen(651, 36, 18), 56),
    ("sarsen-2", "tree", lambda: sarsen(659, 26, 15), 42),
    ("sarsen-3", "tree", lambda: sarsen(667, 18, 11, second=False), 30),
    ("flint-heap-1", "bush", lambda: flint_heap(671, 22, 10, 14), 0),
    ("flint-heap-2", "bush", lambda: flint_heap(677, 16, 8, 9), 0),
    ("chalk-outcrop-1", "bush", lambda: chalk_outcrop(681, 56, 16, 3), 0),
    ("chalk-outcrop-2", "bush", lambda: chalk_outcrop(687, 38, 13, 2), 0),
    ("chalk-outcrop-3", "bush", lambda: chalk_outcrop(691, 24, 10, 1), 0),
]

PREVIEW = os.path.join(HERE, "_downs", "props_preview.png")


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    ok = True
    rows = []
    for (sid, kind, draw, footprint) in SPRITES:
        # Drawn on trees.Bleed and fitted by trees.save_grounded, so nothing drawn past the box is cut off.
        row = T.save_grounded(draw(), sid, kind, FOOT if kind == "tree" else 3, footprint, OUT, RES)
        if row is None:
            ok = False
        else:
            rows.append(row)
    manifest = ('{\n'
                '  "_comment": "Generated by tools/art/props_downs.py - the White Downs\' beeches, thorns, '
                'junipers, sarsens, flint heaps and chalk outcrops, in trees.json\'s shape: ground is the contact '
                'line and anchor_x the column that stands on the placement point, in pixels of the PNG; trunk is '
                'the width in world pixels of the only part that blocks (0: walk through it).",\n'
                '  "sprites": [\n%s\n  ]\n}\n' % ",\n".join(rows))
    T.write_text(os.path.join(OUT, "downs.json"), manifest)
    print("%d downs sprites%s" % (len(rows), "" if ok else " - WITH PALETTE ERRORS"))
    write_preview()
    return 0 if ok else 1


def turf_tile():
    """A fill tile of the downs' turf: the generated one if tiles.py has run, else drawn here."""
    path = os.path.join(ROOT, "assets", "bonelight", "tiles", "downs_turf_fill.png")
    from PIL import Image
    if os.path.exists(path):
        return Image.open(path).convert("RGBA")
    import tiles
    for (n, painter, rim) in tiles.TERRAINS:
        if n == "downs_turf":
            return tiles.emit_terrain(n, painter, rim, ["fill"])[0][2].img
    raise SystemExit("props_downs: no downs_turf terrain (tools/art/terrain_downs.py)")


def write_preview():
    """Every downs sprite stood on the turf at its ground line, at the game's own size, x2 for looking at."""
    from PIL import Image
    floor = turf_tile()
    shots = [(sid, Image.open(os.path.join(OUT, sid + ".png")).convert("RGBA")) for (sid, _k, _d, _f) in SPRITES]
    width = sum(s.size[0] for _i, s in shots) + 16 * (len(shots) + 1)
    height = max(s.size[1] for _i, s in shots) + 32
    sheet = Image.new("RGBA", (width, height))
    for y in range(0, height, floor.size[1]):
        for x in range(0, width, floor.size[0]):
            sheet.paste(floor, (x, y))
    x = 16
    for _sid, s in shots:
        sheet.alpha_composite(s, (x, height - 16 - s.size[1]))
        x += s.size[0] + 16
    if not os.path.isdir(os.path.dirname(PREVIEW)):
        os.makedirs(os.path.dirname(PREVIEW))
    sheet.resize((width * 2, height * 2), Image.NEAREST).save(PREVIEW)
    print("preview: tools/art/_downs/props_preview.png")


if __name__ == "__main__":
    sys.exit(main())
