# -*- coding: utf-8 -*-
"""The Broken Circle's standing things (.ai/campaign.md section 6). Run: python tools/art/props_heath.py

Writes assets/bonelight/world/heath/<id>.png (authored 1:1, saved at x2 like every world sprite) and
heath.json beside them, in trees.json's shape: each sprite's ground line and anchor column in pixels
of the PNG, and the width in world pixels of the only part that blocks. The chapter's foliage
(scripts/Foliage.BrokenCircle.cs) names the manifest and scatters them through the same grounded-decor
path as the trees, so they sort at their feet and stop the wizard's boots, not his chest
(.ai/terrain.md 10). Since the foliage pass (.ai/terrain.md section 13) it also draws the heath's size
ladder: tors, wind-cut thorns, boulders, gorse thickets and heather clumps (below, "the foliage pass"),
and writes tools/art/_props_heath_preview.png, every sprite stood on the heather.

THE STONES OF THE MOOT. "There were nine stones. There are eight, and one that walks." The heath is
littered with what is left of the circles the wizards met in: stones standing, leaning, broken off at
the stump, and fallen flat in the heather. They are granite - speckled, the one pale stone on the
heath - lit on the face the key reaches, with lichen crusting their weather side. None of them has a
split face or a mark: that is the Ninth Stone's, and a decor stone that carried either would be the
boss standing still.

GORSE. The heath's own bush: dark, spined, low, starred with flowers in gold's lower stops (pigment,
not light - gold flowers come several to a bush, never one bright dot alone).

Like world_props.py: seen a little from above, never the subject, so no actor rim, a heavy contact
shadow, and nothing above two stops brighter than the floor except the stone's lit edge.
"""
from __future__ import print_function

import io
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import bonelight as bl
import raster
import trees as T

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "heath")
SCALE = 2
FOOT = 4          # authored rows below the ground line: room for the contact shadow

ST = T.ST
SK = T.SK
LI = T.ramp("lichen")
VE = T.VE
VI = T.VI
GO = T.GO

# Granite, lit to dark: the bone row's shade and deep stops for the pale lit face, then stone's hi
# to its base. In stone's own row, the first pass, a standing stone on the heather was a dark post.
GRANITE = [SK[3], SK[4], ST[0], ST[1], ST[2]]


def hash2(x, y, s):
    return T.hash2(x, y, s)


def stone_body(c, outline, seed, top_rows=2):
    """Fill a standing stone from its outline - a list of (y, left, right) rows - lit on the left
    face, shaded on the right, with granite's speckle, a lit sliver of its top surface, weathering
    cracks and lichen. Returns nothing; draws into c."""
    rnd = T.Rng(seed)
    rows = {}
    for (y, l, r) in outline:
        rows[y] = (l, r)
    ys = sorted(rows)
    y0, y1 = ys[0], ys[-1]
    for y in ys:
        l, r = rows[y]
        w = max(1, r - l)
        # Three faces in three-quarter view: the lit face on the left, the front, and the shaded
        # face on the right, with an arris (a lit stop over a dark one) where two faces meet.
        a1 = l + max(1, int(w * 0.34))
        a2 = l + max(2, int(w * 0.72))
        for x in range(l, r + 1):
            if x < a1:
                tone = GRANITE[1] if (x + y) % 4 else GRANITE[0]
            elif x < a2:
                tone = GRANITE[2] if (x * 3 + y) % 5 else GRANITE[1]
            else:
                tone = GRANITE[3] if (x + y * 2) % 4 else GRANITE[4]
            h = hash2(x, y, seed)
            if h < 0.08:
                tone = GRANITE[0] if x < a2 else GRANITE[2]        # feldspar
            elif h < 0.15:
                tone = ST[3] if x < a2 else ST[4]                   # mica
            c.set(x, y, tone)
        if a2 < r:
            c.set(a2, y, GRANITE[1] if y % 5 else GRANITE[2])
            c.set(a2 + 1, y, ST[3])
        c.set(l, y, GRANITE[0] if y < y1 - 2 else GRANITE[1])   # the lit edge
        c.set(r, y, ST[4])
    # The top surface, seen a little from above: a lit sliver along the first rows.
    for y in ys[:top_rows]:
        l, r = rows[y]
        for x in range(l + 1, r):
            c.set(x, y, GRANITE[1] if (x + y) % 3 else GRANITE[0])
    # Weathering: two or three short cracks that change angle, in the stone's deepest stop.
    for k in range(rnd.randrange(2, 4)):
        y = rnd.randrange(y0 + 4, y1 - 3)
        l, r = rows[y]
        x = rnd.randrange(l + 2, max(l + 3, r - 1))
        for j in range(rnd.randrange(3, 7)):
            x += rnd.choice((1, 1, 0))
            y += rnd.choice((0, 1, 1, -1))
            if y in rows and rows[y][0] < x < rows[y][1]:
                c.set(x, y, OCC if j % 3 == 0 else ST[4])
    # Lichen on the weather (right) side and a crust on the lit face.
    for k in range(rnd.randrange(3, 6)):
        y = rnd.randrange(y0 + 2, y1 - 2)
        l, r = rows[y]
        cx = rnd.choice((r - 2, r - 3, l + 2, (l + r) // 2))
        for (dx, dy, tone) in ((0, 0, LI[2]), (1, 0, LI[3]), (0, 1, LI[3]), (-1, 0, LI[2]),
                               (0, -1, LI[1]), (1, 1, GO[3])):
            if (y + dy) in rows and rows[y + dy][0] < cx + dx < rows[y + dy][1]:
                c.set(cx + dx, y + dy, tone)


def heather_at_foot(c, x0, x1, base, seed):
    """Heather grown up round a stone's foot: a few low violet-topped sprigs over the contact."""
    rnd = T.Rng(seed)
    for x in range(x0, x1 + 1):
        if rnd.random() < 0.55:
            h = rnd.randrange(1, 4)
            for j in range(h):
                c.set(x, base - j, VE[4] if j < h - 1 else (VI[1] if x % 2 else VI[2]))


def outline_rows(y0, y1, widths, cx, lean=0.0, jag=None):
    """(y, left, right) rows from a hand-written width profile, top to bottom, leaning toward +x at
    the top by `lean` pixels. `jag` breaks the top row edge for a broken stone."""
    n = len(widths) - 1
    out = []
    for y in range(y0, y1 + 1):
        t = (y - y0) / float(max(1, y1 - y0)) * n
        i = min(n - 1, int(t))
        hw = widths[i] + (widths[i + 1] - widths[i]) * (t - i)
        sh = lean * (1.0 - (y - y0) / float(max(1, y1 - y0)))
        out.append((y, int(T.round2(cx - hw + sh)), int(T.round2(cx + hw + sh))))
    if jag:
        out = [(y, l, r) for (y, l, r) in out if not (y < y0 + jag[0] and jag[1] <= (l + r) // 2)]
    return out


def standing(w, h, widths, lean, seed, jag=None):
    c = T.Bleed(w, h + FOOT)
    base = h - 1
    cx = w / 2.0
    T.contact(c, cx + 1, base + 1, w * 0.42)
    T.shade_pool(c, cx + 3, base + 1, w * 0.55, 3.0, density=0.5)
    rows = outline_rows(2, base, widths, cx - 0.5, lean, jag)
    stone_body(c, rows, seed)
    if jag:
        # The break: a ragged top, its fresh face a little paler (the stone was broken, not worn).
        top = rows[0][0]
        for (y, l, r) in rows[:3]:
            for x in range(l + 1, r):
                if hash2(x, y, seed + 9) < 0.5:
                    c.set(x, y, GRANITE[0] if y == top else GRANITE[1])
    heather_at_foot(c, int(cx - w * 0.42), int(cx + w * 0.42), base, seed + 3)
    c.anchor_x = int(cx)
    return c


def fallen(w, h, seed):
    """A stone lying in the heather, half sunk: its long face lit along the top, its end toward the
    viewer showing the speckled section."""
    c = T.Bleed(w, h + FOOT)
    base = h - 1
    cx = w / 2.0
    T.contact(c, cx + 1, base + 1, w * 0.46)
    rows = []
    for y in range(3, base + 1):
        t = (y - 3) / float(base - 3)
        hw = w * 0.42 * (0.78 + 0.22 * math.sin(t * math.pi * 0.9))
        rows.append((y, int(T.round2(cx - hw)), int(T.round2(cx + hw))))
    # Drawn as a stone on its side: lit from the TOP (its upper face), so swap the light to rows.
    rnd = T.Rng(seed)
    for (y, l, r) in rows:
        t = (y - 3) / float(base - 3)
        for x in range(l, r + 1):
            if t < 0.35:
                tone = GRANITE[1] if (x + y) % 4 else GRANITE[0]
            elif t < 0.75:
                tone = GRANITE[2] if (x * 3 + y) % 5 else GRANITE[1]
            else:
                tone = GRANITE[3] if (x + y) % 3 else GRANITE[4]
            if x > r - 3:
                tone = GRANITE[3]
            hh = hash2(x, y, seed)
            if hh < 0.08:
                tone = GRANITE[0] if t < 0.75 else GRANITE[2]
            elif hh < 0.14:
                tone = ST[3]
            c.set(x, y, tone)
        c.set(l, y, GRANITE[1])
        c.set(r, y, ST[4])
    for x in range(rows[0][1] + 1, rows[0][2]):
        c.set(x, 3, GRANITE[0] if x % 3 else GRANITE[1])
    for k in range(3):
        x = rnd.randrange(int(cx - w * 0.3), int(cx + w * 0.3))
        y = rnd.randrange(5, base - 1)
        for (dx, dy, tone) in ((0, 0, LI[2]), (1, 0, LI[3]), (-1, 0, LI[1]), (0, 1, LI[3])):
            c.set(x + dx, y + dy, tone)
    heather_at_foot(c, 1, w - 2, base, seed + 5)
    c.anchor_x = int(cx)
    return c


def gorse(seed, w, h):
    """Gorse: a low dark mound of spines, starred with gold flowers in clumps."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + 3)
    base = h - 1
    cx = w // 2
    c.disc(cx, base, w * 0.42, 2.0, OCC)
    for x in range(1, w - 1):
        t = (x - cx) / (w * 0.5)
        top = base - int(T.round2((h - 2) * math.sqrt(max(0.0, 1.0 - t * t)))) + rnd.randrange(0, 2)
        for y in range(top, base + 1):
            hv = hash2(x, y, seed)
            if y == top:
                tone = VE[3] if t < 0 else VE[4]
            elif hv < 0.16:
                tone = OCC
            elif hv < 0.72:
                tone = VE[4]
            elif t < -0.1 and y < top + 3:
                tone = VE[2]
            else:
                tone = VE[3]
            c.set(x, y, tone)
        # spines standing off the top
        if rnd.random() < 0.5:
            c.set(x, top - 1, VE[3] if t < 0 else VE[4])
    gorse_flowers(c, rnd, rnd.randrange(4, 7), 3, w - 3, base, h)
    c.anchor_x = cx
    return c


def gorse_flowers(c, rnd, clumps, x0, x1, base, h):
    """Gorse's flowers, in clumps of three to five: gold's base and shade, pigment under no light. They
    were gold's lit and base until the foliage pass put four times the gorse on the heath, and gorse-2's
    brightest tenth stood +56 L* over the heather, as bright as a creature's rim; a stop down, the bush
    is starred rather than lit."""
    for k in range(clumps):
        fx = rnd.randrange(x0, x1)
        fy = base - rnd.randrange(2, h - 2)
        for j in range(rnd.randrange(3, 6)):
            x, y = fx + rnd.randrange(-2, 3), fy + rnd.randrange(-1, 2)
            if c.get(x, y)[3]:
                c.set(x, y, GO[2] if (x + y) % 3 else GO[3])


# --- the foliage pass (2026-11-04, .ai/terrain.md section 13) ------------------------------------
#
# The heath's stands are not woods. Its canopy (Foliage.BrokenCircle.cs) is a field of DRIFTS combed
# out along the wind, and a drift is a granite rise: tors on its crown, gorse thickets clothing it,
# boulders and wind-cut thorns on its flanks, heather thickening at its edge. Between the drifts the
# moor is open, as it was. Everything here is low and wide - nothing on the heath is taller than a
# thorn, and nothing is a tall single slab (that is the Ninth Stone's, .ai/campaign.md section 6).

LN = T.LN
# Leaf under no light: the dark half of verdant, as the Fields' hedges (the gorse's own spines too).
DARK = [VE[2], VE[3], VE[4], OCC]
# A thorn's crown, a stop darker again: shade on the lit side, deep and occ beyond - on the night moor
# verdant's base read as a lit green, the brightest leaf on the heath.
THORN = [VE[3], VE[3], VE[4], OCC]
# A thorn's bark: linen's grey, a stop under the shared trees' so the trunk is not the brightest thing.
BARK = [LN[1], LN[2], LN[3], LN[4], OCC]


def pillow(c, cx, cy, rx, ry, seed, tilt=0.0, p=None):
    """One woolpack of granite: a pillow the weather has rounded out of a jointed block, the way a
    Dartmoor tor is built - wider than it is tall, its top turned to the sky, its outline wandering so
    that no two are alike (three low harmonics of the seed), sheared by `tilt` so a pile can lean. Lit
    from the upper left: the top and the left shoulder catch the key, the underside turns away into
    occ where it rests on the pillow below. Granite's feldspar and mica speckle it, and a mottle three
    pixels across keeps a face from being one flat tone. Returns its pixels, {(x, y): (nx, ny)}.

    It replaced a rounded rectangle (granite_block), which read as a laid brick: a joint is now
    wherever two pillows meet, never a straight line."""
    rnd = T.Rng(seed)
    harm = [(k, (0.13 / k) * (0.5 + rnd.random()), rnd.random() * 6.283) for k in (2, 3, 5, 7)]
    # a little flatter on top and underneath than an ellipse: a pillow, not a ball, and not a box
    p = p or (2.0 + rnd.random() * 0.5)
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
        sky = max(0.0, -ny - 0.3)                               # the top, turned up to the key
        v = 0.31 + (-0.55 * nx - 0.45 * ny) * 0.5 + sky * 0.55 + nz * 0.08
        v += (T.hash2(x // 3, y // 3, seed + 5) - 0.5) * 0.14
        hv = T.hash2(x, y, seed)
        if hv < 0.06:
            v += 0.2                                            # feldspar
        elif hv < 0.11:
            v -= 0.2                                            # mica
        tone = T.band(GRANITE, v, x, y, 0.3)
        if any((x + ex, y + ey) not in pts for (ex, ey) in ((1, 0), (-1, 0), (0, 1), (0, -1))):
            if nx * 0.5 + ny * 0.86 > 0.2:
                tone = OCC if ny > 0.3 else GRANITE[4]          # turning under, away from the key
            elif -nx * 0.6 - ny * 0.8 > 0.35 and (x + y) % 3:
                tone = GRANITE[0]                               # the lit shoulder
        c.set(x, y, tone)
    return dict((k, (v[0], v[1])) for (k, v) in pts.items())


def overhang(c, upper, below):
    """The shadow an overhanging pillow casts on the top of the one it rests on: the two rows under its
    underside, a pixel to the right of it (the key is upper left), wherever they fall on `below`."""
    lowest = {}
    for (x, y) in upper:
        if y > lowest.get(x, -999):
            lowest[x] = y
    for x in sorted(lowest):
        y = lowest[x]
        for (dy, tone) in ((1, OCC), (2, GRANITE[4])):
            at = (x + 1, y + dy)
            if at in below and at not in upper and (dy == 1 or (x + y) % 2):
                c.set(at[0], at[1], tone)


def lichen_crust(c, rnd, cx, cy, n=1):
    """A crust of lichen on a lit top: lichen's base and shade, a fleck of its lit."""
    for k in range(n):
        x, y = cx + rnd.randrange(-2, 3), cy + rnd.randrange(-1, 1)
        for (dx, dy, tone) in ((0, 0, LI[2]), (1, 0, LI[3]), (0, 1, LI[3]), (-1, 0, LI[2]),
                               (-1, -1, LI[1]), (2, 0, LI[3])):
            if c.get(x + dx, y + dy)[3]:
                c.set(x + dx, y + dy, tone)


def clitter_stone(c, rnd, cx, base, size, seed):
    """One stone of the clitter, the frost-broken granite that spills off a tor down its flanks: a
    small pillow, half sunk, with heather grown up round its foot."""
    rx = size * (0.9 + rnd.random() * 0.5)
    ry = size * (0.6 + rnd.random() * 0.3)
    pillow(c, cx, base - ry * 0.6, rx, ry, seed, tilt=(rnd.random() - 0.5) * 0.4)
    heather_at_foot(c, int(cx - rx), int(cx + rx), base, seed + 1)


def tor(seed, w, h, stacks):
    """A tor, the Dartmoor kind: granite weathered along its joints into pillows, stacked in one or more
    piles on a hump of bedrock, each pile leaning and overhanging its own way, crowning a drift.

    `stacks` is each pile as (centre as a share of the width, base width share, pillows, lean). Each
    pillow is narrower than the one under it, or now and then WIDER - it overhangs, and casts its shadow
    on the top it rests on - and sits a little off it the way the pile leans. Under the piles a low hump
    of bedrock breaks the heather, and the clitter spills off it down both flanks. A low, broad thing on
    purpose: the only tall single stone on the heath is the Ninth."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + FOOT)
    base = h - 1
    cx = w / 2.0
    T.shade_pool(c, cx + w * 0.1, base + 1, w * 0.52, 4.0, density=0.4)
    c.disc(cx + 1, base + 1, w * 0.44, 2.2, OCC)
    # the bedrock the piles stand on: one long, low hump and a shoulder of it
    hump = pillow(c, cx - w * 0.04, base - h * 0.1, w * 0.42, h * 0.13, seed + 1, tilt=0.05, p=2.0)
    hump.update(pillow(c, cx + w * 0.22, base - h * 0.08, w * 0.2, h * 0.1, seed + 2, p=2.2))
    for i, (sx, bw, n, lean) in enumerate(stacks):
        x = w * sx
        rx = w * bw / 2.0
        ry = h * (0.1 + rnd.random() * 0.08)
        top = base - h * 0.14
        below = hump
        for k in range(n):
            cy = top - ry * 0.62
            tilt = lean * 0.25 + (rnd.random() - 0.5) * 0.3
            # Now and then a course has weathered into two along a vertical joint: two pillows side by
            # side, of different widths and a pixel or two apart in height.
            if rx > 7 and rnd.random() < 0.4:
                split = 0.35 + rnd.random() * 0.3
                lx, rx1 = x - rx * (1 - split), rx * split * 1.05
                this = pillow(c, lx, cy + rnd.randrange(0, 2), rx * (1 - split) * 1.05, ry, seed + 31 * i + 7 * k,
                              tilt=tilt)
                this.update(pillow(c, x + rx * split, cy - rnd.randrange(0, 2), rx1, ry * (0.8 + rnd.random() * 0.3),
                                   seed + 31 * i + 7 * k + 3, tilt=tilt))
            else:
                this = pillow(c, x, cy, rx, ry, seed + 31 * i + 7 * k, tilt=tilt)
            overhang(c, this, below)
            if rnd.random() < 0.65:
                lichen_crust(c, rnd, int(x - rx * 0.3), int(cy - ry * 0.55), rnd.randrange(1, 3))
            top = cy - ry * 0.78
            below = this
            # the next: off-centre the way the pile leans, and narrower - or, now and then, wider, so it
            # overhangs - and of its own thickness: a tor's courses are not laid to a gauge
            x += lean * (1.0 + rnd.random() * 1.5) + (rnd.random() - 0.5) * 3.0
            rx *= (0.6 + rnd.random() * 0.5) if rnd.random() < 0.75 else (1.05 + rnd.random() * 0.2)
            rx = max(3.5, rx)
            ry = max(2.4, ry * (0.7 + rnd.random() * 0.55))
    # clitter down both flanks: bigger near the tor and smaller as it spills out into the heather, a few
    # lying up on the bedrock, the farthest half sunk
    for k in range(rnd.randrange(12, 17)):
        side = -1 if k % 2 == 0 else 1
        out = 0.3 + rnd.random() * 0.34
        size = 5.6 - out * 6.0 + rnd.random() * 1.4
        drop = int(out * 8) - 2 + rnd.randrange(0, 3)
        clitter_stone(c, rnd, cx + side * w * out, base + drop, max(1.3, size), seed + 101 + k)
    heather_at_foot(c, int(cx - w * 0.3), int(cx + w * 0.3), base, seed + 3)
    c.anchor_x = int(cx)
    return c


def boulder(seed, w, h):
    """A boulder off a tor, lying in the heather on a drift: a pillow come off its pile, half sunk,
    sometimes with a smaller one beside it, lichened on top, heather grown up round its foot."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + FOOT)
    base = h - 1
    cx = w / 2.0
    T.contact(c, cx + 1, base + 1, w * 0.46)
    pillow(c, cx - w * 0.04, base - h * 0.42, w * 0.42, h * 0.46, seed, tilt=(rnd.random() - 0.5) * 0.5)
    if rnd.random() < 0.6:
        pillow(c, cx + w * 0.34, base - h * 0.18, w * 0.16, h * 0.2, seed + 3)
    lichen_crust(c, rnd, int(cx) - 3, int(base - h * 0.78), 2)
    heather_at_foot(c, 1, w - 2, base, seed + 5)
    c.anchor_x = int(cx)
    return c


def outcrop(seed, w, h, n):
    """Bedrock breaking the heather on a drift: `n` low humps of granite run together, so much of them
    still under the turf that heather grows over their edges. Walked over, like the turf they show
    through. The crown's rock, thickest where the tor stands, in place of a floor of slabs (which read
    as paving)."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + 3)
    base = h - 1
    cx = w / 2.0
    c.disc(cx + 1, base, w * 0.44, 1.6, OCC)
    for k in range(n):
        x = w * (0.18 + 0.64 * (k + rnd.random() * 0.6) / max(1, n))
        rx = w * (0.16 + rnd.random() * 0.14)
        ry = h * (0.3 + rnd.random() * 0.25)
        pillow(c, x, base - ry * 0.55, rx, ry, seed + 13 * k, tilt=(rnd.random() - 0.5) * 0.3, p=2.0)
        if rnd.random() < 0.7:
            lichen_crust(c, rnd, int(x - rx * 0.3), int(base - ry * 1.1), 1)
    # the turf over its edges: sprigs along the foot and a few up its flanks
    heather_at_foot(c, 1, w - 2, base, seed + 7)
    for k in range(rnd.randrange(3, 6)):
        x = rnd.randrange(2, w - 2)
        y = base - rnd.randrange(1, max(2, h // 2))
        if c.get(x, y)[3]:
            c.set(x, y, VE[4])
            c.set(x, y - 1, VI[2] if k % 2 else VE[3])
    c.anchor_x = int(cx)
    return c


def windthorn(seed, w, h, bare=False):
    """A hawthorn the wind has had its way with: a short, leaning trunk and a crown shorn flat and low,
    swept to one side and thin on the other. Not a tree of the shared set - the heath grows nothing
    taller than a man and a half, so the moor's long sightlines stay long. Bare, it is the same thorn
    dead: its limbs still streaming the way the crown went."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + FOOT)
    base = h - 1
    cx = int(w * 0.34)
    c.anchor_x = cx
    if not bare:
        T.shade_pool(c, cx + w * 0.2, base, w * 0.42, 6)
    T.contact(c, cx, base + 1, 5)
    top = base - h * 0.44
    lean = 3.2
    T.trunk(c, cx, base, top, 7, 4, BARK, seed, flare=2.5, lean=lean)
    tx = cx + lean * (base - top) * 0.1
    tips = []
    for k in range(3 if not bare else 4):
        # Streaming leeward and low: a limb that rose would stand out of the shorn top.
        a = -0.28 - k * 0.22 + (rnd.random() - 0.5) * 0.2
        T.limb(c, tx, top + 3 + k, a, w * (0.26 if k < 2 else 0.16), 3, BARK, 1 if not bare else 3, rnd, tips,
               droop=0.1)
    if bare:
        # one limb back into the wind, broken short
        T.limb(c, tx, top + 3, -math.pi + 0.5, w * 0.12, 2, BARK, 0, rnd)
        return c
    ccx, ccy = tx + w * 0.16, top - h * 0.06
    rx, ry = w * 0.42, h * 0.2
    clusters = T.fill_clusters(rnd, ccx, ccy, rx - 5, ry - 3, 28, 4, 8)
    flat = ccy - ry * 0.7                                   # the shorn top
    lee = ccx + rx * 0.3
    kept = []
    for (x, y, crx, cry) in clusters:
        if x < tx - w * 0.02 and rnd.random() < 0.6:
            continue                                        # the windward side, thin
        if y - cry < flat:
            y = flat + cry
        if x > lee:
            y += (x - lee) * 0.35                           # the lee end streams down
        kept.append((x, y, crx, cry))
    kept.sort(key=lambda k: k[1] + k[2] * 0.3)
    T.crown(c, kept, THORN, ccx, ccy, rx, ry, seed, ragged=0.45, gaps=0.1, bias=0.0)
    return c


def gorse_thicket(seed, w, h, humps):
    """A gorse thicket: the stands' body. Several domes of spines run together into one long low mass,
    lit as one from the upper left, gorse's flowers in clumps, a dead grey stem or two standing out of
    it. Walked through, like every bush; as tall as a man's waist and three times as long."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + 3)
    base = h - 1
    cx = w / 2.0
    T.shade_pool(c, cx + 4, base + 1, w * 0.5, 3.0, density=0.45)
    c.disc(cx, base, w * 0.47, 2.4, OCC)
    domes = []
    for k in range(humps):
        dx = w * (k + 0.5) / humps + (rnd.random() - 0.5) * w * 0.12
        domes.append((dx, w * (0.40 + rnd.random() * 0.14) / humps * 1.4, (h - 2) * (0.7 + rnd.random() * 0.3)))
    for x in range(1, w - 1):
        top_h = 0.0
        for (dx, half, hh) in domes:
            t = (x - dx) / half
            if abs(t) < 1.0:
                top_h = max(top_h, hh * math.sqrt(1.0 - t * t))
        g = (x - cx) / (w * 0.5)
        top_h *= min(1.0, (1.0 - abs(g)) * 3.0 + 0.2)
        if top_h < 1:
            continue
        top = base - int(T.round2(top_h)) + rnd.randrange(0, 2)
        for y in range(top, base + 1):
            hv = T.hash2(x, y, seed)
            t = (y - top) / float(max(1, base - top))
            # Darker than the old gorse: a thicket is dense, and verdant's base, which the old bushes
            # carried on their lit shoulder, would make a drift of them a lit green band at night.
            if y == top:
                tone = VE[3] if g < 0.2 else VE[4]
            elif hv < 0.24:
                tone = OCC
            elif t > 0.6:
                tone = VE[4] if hv < 0.75 else OCC
            elif g < 0.1 and t < 0.4 and hv > 0.7:
                tone = VE[3]
            elif hv < 0.86:
                tone = VE[4]
            else:
                tone = VE[3]
            c.set(x, y, tone)
        if rnd.random() < 0.5:
            c.set(x, top - 1, VE[3] if g < 0 else VE[4])
    # dead stems standing out of it, grey
    for k in range(rnd.randrange(1, 3)):
        sx = int(rnd.randrange(int(w * 0.2), int(w * 0.8)))
        ln = rnd.randrange(4, 8)
        y0 = base - int(h * 0.55)
        for j in range(ln):
            c.set(sx + (j // 3) * (1 if k % 2 else -1), y0 - j, LN[2] if j % 3 else LN[3])
    gorse_flowers(c, rnd, rnd.randrange(5, 9), 4, w - 4, base, h)
    c.anchor_x = int(cx)
    return c


def heather_clump(seed, w, h):
    """Heather grown into a clump you could sit on: wiry sprigs from one root, the bloom most of what
    shows - violet's lit on the key side, its base and shade beyond - over stems in verdant's deep. A
    rung between the detail layer's tufts and the gorse, and the heath's own colour, so it is the moor
    thickening rather than something set on it."""
    rnd = T.Rng(seed)
    c = T.Bleed(w, h + 3)
    base = h - 1
    cx = w / 2.0
    c.disc(cx, base, w * 0.44, 1.6, OCC)
    for x in range(1, w - 1):
        t = (x - cx) / (w * 0.5)
        hh = int(T.round2((h - 2) * max(0.0, 1.0 - t ** 4) ** 0.5)) + rnd.randrange(-1, 2)
        if hh < 1:
            continue
        top = base - hh
        bloom = 1 + rnd.randrange(1, 3)
        for y in range(top, base + 1):
            if y <= top + bloom:
                if t < -0.25:
                    tone = VI[1] if (x + y) % 3 else VI[2]
                elif t < 0.35:
                    tone = VI[2] if (x * 3 + y) % 4 else VI[1]
                else:
                    tone = VI[3] if (x + y) % 2 else VI[2]
            else:
                tone = VE[4] if (x + y) % 2 else OCC
            c.set(x, y, tone)
    for k in range(rnd.randrange(2, 4)):
        sx = int(cx + rnd.randrange(-int(w * 0.35), int(w * 0.35) + 1))
        st = base - h - rnd.randrange(0, 2)
        c.set(sx, st + 1, VE[4])
        c.set(sx, st, VI[1] if sx < cx else VI[2])
    c.anchor_x = int(cx)
    return c


# (id, kind, painter, footprint in WORLD px or 0 for none) - as trees.SPRITES.
SPRITES = [
    ("standing-stone-1", "tree", lambda: standing(22, 46, [3.6, 5.2, 6.0, 6.4, 6.8, 7.4], 1.0, 211), 26),
    ("standing-stone-2", "tree", lambda: standing(28, 38, [5.0, 7.0, 8.4, 9.0, 9.6], 3.0, 223), 34),
    ("standing-stone-3", "tree", lambda: standing(24, 26, [6.4, 7.6, 8.2, 8.6], 0.0, 227, jag=(3, 0)), 30),
    ("fallen-stone-1", "tree", lambda: fallen(40, 14, 229), 60),
    ("fallen-stone-2", "tree", lambda: fallen(30, 12, 233), 44),
    ("gorse-1", "bush", lambda: gorse(241, 30, 16), 0),
    ("gorse-2", "bush", lambda: gorse(251, 22, 13), 0),
    # The foliage pass: the ladder, largest first. Tors crown the drifts (a third again as wide as they
    # block: the rounded flanks and the clitter are walked past, the foot is not); wind-cut thorns and
    # boulders stand on their flanks; gorse thickets are the drifts' body and heather clumps their edge;
    # outcrops of bedrock break the heather toward the tor. Each tor's piles are (centre, base width,
    # pillows, lean): tor-1 two piles leaning apart, tor-2 one tall pile leaning over a low one.
    ("tor-1", "tree", lambda: tor(401, 72, 40, [(0.32, 0.4, 3, -0.6), (0.62, 0.46, 4, 0.8)]), 92),
    ("tor-2", "tree", lambda: tor(409, 58, 44, [(0.42, 0.62, 4, 0.7), (0.76, 0.34, 2, -0.5)]), 76),
    ("windthorn-1", "tree", lambda: windthorn(441, 64, 64), 14),
    ("windthorn-2", "tree", lambda: windthorn(449, 52, 54), 12),
    ("windthorn-bare-1", "tree", lambda: windthorn(457, 50, 56, bare=True), 12),
    ("boulder-1", "tree", lambda: boulder(421, 22, 14), 30),
    ("boulder-2", "tree", lambda: boulder(431, 17, 12), 22),
    ("gorse-thicket-1", "bush", lambda: gorse_thicket(461, 58, 22, 3), 0),
    ("gorse-thicket-2", "bush", lambda: gorse_thicket(467, 46, 24, 2), 0),
    ("heather-clump-1", "bush", lambda: heather_clump(471, 20, 10), 0),
    ("heather-clump-2", "bush", lambda: heather_clump(479, 15, 8), 0),
    ("outcrop-1", "bush", lambda: outcrop(481, 56, 14, 3), 0),
    ("outcrop-2", "bush", lambda: outcrop(487, 36, 11, 2), 0),
    ("outcrop-3", "bush", lambda: outcrop(491, 20, 9, 1), 0),
]

PREVIEW = os.path.join(HERE, "_props_heath_preview.png")


# Sprites the heath scatters but trees.py draws, listed in heath.json BY REFERENCE - trees.json's own row,
# copied, its path still trees.py's - so a set of the heath's foliage (Foliage.BrokenCircle.cs) names one
# manifest even where it mixes the heath's sprites with a shared one. Nothing is drawn for them here.
BORROWED = ["bramble-2"]


def borrowed_rows():
    """trees.json's rows for BORROWED, in trees.json's own words."""
    path = os.path.join(ROOT, "assets", "bonelight", "world", "trees", "trees.json")
    rows = []
    with io.open(path, encoding="utf-8") as f:
        lines = f.read().splitlines()
    for sid in BORROWED:
        found = [l.rstrip(",") for l in lines if ('"id": "%s"' % sid) in l]
        if len(found) != 1:
            raise SystemExit("props_heath: trees.json has %d rows for '%s' - run tools/art/trees.py" % (len(found), sid))
        rows.append(found[0].encode("utf-8") if not isinstance(found[0], str) else found[0])
        print("  %-18s from trees.json" % sid)
    return rows


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    ok = True
    rows = []
    for (sid, kind, draw, footprint) in SPRITES:
        # Drawn on trees.Bleed and fitted by trees.save_grounded, so a shadow or a spine drawn past
        # the box grows the canvas instead of being cut off at it (trees.py, "NOTHING IS CUT").
        row = T.save_grounded(draw(), sid, kind, FOOT if kind == "tree" else 3, footprint, OUT,
                              "res://assets/bonelight/world/heath")
        if row is None:
            ok = False
        else:
            rows.append(row)
    rows.extend(borrowed_rows())
    manifest = ('{\n'
                '  "_comment": "Generated by tools/art/props_heath.py - the Broken Circle\'s standing stones '
                'and gorse, in trees.json\'s shape: ground is the contact line and anchor_x the column '
                'that stands on the placement point, in pixels of the PNG; trunk is the width in world '
                'pixels of the only part that blocks (0: walk through it).",\n'
                '  "sprites": [\n%s\n  ]\n}\n' % ",\n".join(rows))
    T.write_text(os.path.join(OUT, "heath.json"), manifest)
    print("%d heath sprites%s" % (len(rows), "" if ok else " - WITH PALETTE ERRORS"))
    write_preview()
    return 0 if ok else 1


def write_preview():
    """Every heath sprite stood on the heather floor at its ground line, at the game's own size, x2 for
    looking at: tools/art/_props_heath_preview.png (ignored by git)."""
    from PIL import Image
    floor = Image.open(os.path.join(ROOT, "assets", "bonelight", "tiles", "floor_heather_0.png")).convert("RGBA")
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
    sheet.resize((width * 2, height * 2), Image.NEAREST).save(PREVIEW)
    print("preview: tools/art/_props_heath_preview.png")


if __name__ == "__main__":
    sys.exit(main())
