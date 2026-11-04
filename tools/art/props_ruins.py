# -*- coding: utf-8 -*-
"""The Mystic Ruins' standing ruin and growth (.ai/terrain.md section 13, "The Mystic Ruins").
Run: python tools/art/props_ruins.py

    world/ruins/spire-stump-1.png       the stump of a round tower, broken off high on one side, hollow
    world/ruins/wall-run-1, -2.png      lengths of fallen wall left standing: a pier, broken courses, ivy
    world/ruins/colonnade-1.png         one column still standing on its broken step, a stump beside it
    world/ruins/root-wall-1.png         a tree grown out of the top of a wall, its roots down the face
    world/ruins/plinth-1.png            a statue's plinth, empty: only the feet are left, broken at the ankle
    world/ruins/drums-1, -2.png         a column that fell, lying in its drums, the drums rolled apart
    world/ruins/capital-1.png           a capital fallen from its column, sunk in the ground
    world/ruins/rubble-1, -2.png        heaps of broken blocks, every one at its own angle, weeds through them
    world/ruins/ivy-mound-1.png         rubble the ivy has taken, a corner of a block showing
    world/ruins/ruins.json              every sprite above, in trees.json's shape
    tools/art/_props_ruins_preview.png  (gitignored) on the ruins' floor tone, x2, for looking at

THE LADDER. The Forest's stands are woods, the Fields' hedgerows, the Sands' stone the wind carved. The
Ruins' are the RUIN ITSELF where it has fallen in and the growth has taken it (Foliage.MysticRuins.cs
gives the canopy that shape: overgrown quarters, thickest along the lattice the walls stood on). Between
them, the open flags of the old seat of learning, "dust and cold ash", to fight on. Largest first:

  spire-stump   252 px   the stump of a round tower, the tallest stone left: "broken spires"
  root-wall     236 px   a tree that grew out of a wall's top and holds it together with its roots
  wall-run      150-170  a fallen wall's last pier, its courses stepping down into rubble
  colonnade     196 px   one column still standing on a broken stylobate, a stump and a drum beside it
  (the gnarled and dead trees of trees.py, unchanged: "a few gnarled trees")
  plinth        96 px    a statue's plinth with only the feet left on it, broken at the ankles
  drums         128-152  a fallen column lying in its drums, rolled apart
  capital       76 px    a capital lying where it fell, sunk to its neck
  rubble, ivy   56-88    heaps of broken blocks, and rubble the ivy has taken
  (the brambles of trees.py, unchanged)

Below them, the ground detail (detail_ruins.py): cold ash and dust in the open, weeds in the cracks, moss
on stone chips, ivy runners and roots across the flags under the stands.

RUINED, NEVER BUILT (2026-11-04; the integrator sent the Sands back for "stacked bricks", and a paved
ground for reading as paving). Ruins are worked stone, so there is masonry here - but every course is its
own height, every block its own width with joints that never line up two courses running, and every block
is shaded on its own with a chipped arris, so a wall face is a few dozen stones and never a brick pattern.
Tops are broken off block by block, high on one side and stepping down; whole blocks are missing from the
face, sunk, or pushed out; the ends crumble into the rubble at their foot; ivy pours from every ledge and
moss sits in the lower joints. A column's drums no longer line up; a fallen one lies in pieces.

THE STONE IS THE WALLS' OWN, AND QUIETER THAN THE CAST'S. ruin_walls.py lights a wall's TOP (the stone
row's base, its arris lit) over a FACE in the row's shade, on a floor of the row's shade and deep; this
draws the same stone the same way, so the fragments belong to the walls they fell from. The cast's stone
creatures (cast_ruins.py) carry their planes in stone.hi and stone.lit, and its carved ones in the bone
row's deep half. So nothing here is bone-pale, `hi` is never a plane, and `lit` is an arris or the sunlit
top of a broken course - the cast stays the lighter stone on screen. The growth is verdant's dark half
(shade and deep, its base a leaf at a time), the moss lichen's shade and deep, the roots grave linen like
the gnarled trees'.

NONE OF IT IS THE CAST, AND NONE OF IT IS A TELL. The chapter's horde is its own statuary, so nothing here
stands alone the height of a body: no lone pillar on a base (the Breach Obelisk), no slab on an upright
(the Lintel-Bearer), no urn (the Cracked Urn), no statue (the Warded Statue - its plinth is empty, which
is the chapter's story: the statues stepped down), no paper anywhere (the Loose Folio, the Misfiled
Page). And for the secrets (.ai/stage-secrets.md 6.5, .ai/achievements-and-secrets.md 8.7): no notched or
pale stone and no carved fragment on the ground (the Broken Ward's far tell is exactly that), no statue's
head (the Statue's Eye), no lone door frame or arch and no dial (the Sundial Vault), no benches or
lectern (the Lecture Hall), and nothing warm, lit or glinting: a ruin wall's wards are dead grooves, and
a window slit is dark.

Drawn with trees.py's painters (Bleed, band, crown, fill_clusters, trunk, limb, shade_pool, contact,
save_grounded), imported, never edited. Python 2.7 and 3 draw the same sprites: trees.Rng only.
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
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "ruins")
PREVIEW = os.path.join(ROOT, "tools", "art", "_props_ruins_preview.png")
RES = "res://assets/bonelight/world/ruins"
TAU = math.pi * 2.0


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


ST = ramp("stone")
LI = ramp("lichen")
VE = ramp("verdant")
LN = ramp("linen")

# The stone's stops, brightest first: lit for an arris and a sunlit ledge, base for a top and a proud
# block's upper face, shade for a face, deep for a joint and a hole, occlusion for the deepest.
STONE = [ST[1], ST[2], ST[3], ST[4], OCC]
# The growth: verdant's shade and deep, never its lit lime, and its base a leaf at a time (L* over this floor:
# shade +22, base +42 - the base as a plane photographed as a lime beacon on the first draft).
IVY = [VE[3], VE[4], OCC]
# A crown's tones, for the crown painter: its base only where a cluster's top meets the key.
CROWN = [VE[2], VE[3], VE[4], OCC]
# Roots and the wood of a tree grown in the stones: grave linen, as the gnarled trees' (trees.gnarled).
ROOT_BARK = LN
# The key, as a surface's normal sees it: upper left and toward the viewer.
KX, KY, KZ = -0.55, -0.55, 0.63

hash2 = TR.hash2


def stop(v):
    """v (1 lit .. 0 occlusion) to the nearest stone stop, with no dither: a block is a few flat tones with
    hard edges between them, which is what makes it a cut stone and not a pebble."""
    return STONE[max(0, min(4, int(round((1.0 - v) * 4))))]


# --- masonry ----------------------------------------------------------------------------------------

def courses_up(rnd, base, top, lo=6, hi=9):
    """Course beds from the base up, each its own height: [(bottom, top)] with top < bottom."""
    out = []
    y = base + 1
    while y > top:
        h = lo + rnd.randrange(hi - lo + 1)
        out.append((y, y - h))
        y -= h
    return out


def masonry(c, x0, x1, base, profile, seed, ledge=4, owner=None, holes=0.07, proud=0.12):
    """A ruined wall's south face and broken top, block by block.

    `profile(x)` is how high the wall still stands above `base` at column x (0: nothing). Courses rise from
    the base, each its own height; in each course, blocks of their own widths, the joints moved on every
    course. A block stands where the profile reaches most of the way up it - so the top breaks off whole
    blocks at a time, stepping down - and the topmost block in a column has its top bitten. Every block is
    shaded on its own (a lit arris along its top and left where it is proud of the next, its right and foot
    in shade, an occlusion joint under it); some are missing (a dark hole with a lit sill), some pushed out
    (lit), some sunk (dark). Over the face, a LEDGE: the wall's top seen from above, base stone lit at its
    arris, as deep as the wall is thick. Returns {x: top y} of the face, for the ivy and the moss."""
    rnd = TR.Rng(seed)
    tallest = max(profile(x) for x in range(x0, x1 + 1))
    beds = courses_up(rnd, base, base - tallest - 2)
    top_at = {}
    stand = {}
    for k, (bot, top) in enumerate(beds):
        x = x0 - rnd.randrange(6)
        while x <= x1:
            w = 7 + rnd.randrange(10)
            bx0, bx1 = x, min(x1, x + w - 1)
            x += w
            mid = (bx0 + bx1) // 2
            need = base - top - 2 + rnd.randrange(4)
            if profile(mid) < need and not (k == 0 and profile(mid) > 0):
                continue
            kind = rnd.random()
            missing = kind < holes and k > 0 and bot - top > 5
            pushed = (not missing) and kind > 1.0 - proud
            sunk = (not missing) and holes <= kind < holes + 0.08
            bias = (rnd.random() - 0.5) * 0.12 + (0.1 if pushed else 0.0) - (0.1 if sunk else 0.0)
            # a bite out of a corner or two: no block keeps four square corners
            bite_l = rnd.randrange(3)
            bite_r = rnd.randrange(3)
            for bx in range(bx0, bx1 + 1):
                if bx < x0 or bx > x1 or profile(bx) <= 0:
                    continue
                for by in range(top, bot):
                    if by > base:
                        continue
                    edge_l = bx - bx0
                    edge_r = bx1 - bx
                    row = by - top
                    if (edge_l + row < bite_l) or (edge_r + row < bite_r):
                        continue                                   # the chipped corner
                    if by == bot - 1:
                        col = OCC if hash2(bx, by, seed) < 0.75 else ST[4]    # the bed joint under it
                    elif edge_r == 0:
                        col = ST[4] if hash2(bx, by, seed + 1) < 0.8 else OCC  # the head joint
                    elif missing:
                        col = OCC if row < bot - top - 3 else ST[4]
                        if by == bot - 2:
                            col = ST[2]                                 # the sill of the hole, lit from above
                    else:
                        v = 0.52 + bias
                        if row == 0:
                            v += 0.22                                   # the arris along its top
                        elif edge_l == 0:
                            v += 0.12
                        elif edge_r == 1 or row >= bot - top - 2:
                            v -= 0.14
                        if by > base - 6:
                            v -= 0.08                                   # damp near the ground
                        if hash2(bx, by, seed + 7) < 0.05:
                            v -= 0.22                                   # a pit
                        col = stop(v)
                    c.set(bx, by, col)
                    stand[(bx, by)] = True
                    if owner is not None:
                        owner[(bx, by)] = col
                    if by < top_at.get(bx, 10 ** 6):
                        top_at[bx] = by
    # the broken top: lay the ledge (the wall's top seen from above) over each column's topmost block,
    # its depth wandering a pixel so the top's back edge is broken too
    for x in range(x0, x1 + 1):
        if x not in top_at:
            continue
        t = top_at[x]
        lt = t - ledge - (1 if hash2(x // 3, 5, seed) < 0.3 else 0)
        for y in range(lt, t):
            row = y - lt
            v = 0.74
            if row == 0:
                v = 0.98 if hash2(x, y, seed + 9) < 0.55 else 0.8       # the arris at its back edge, lit
            elif row == ledge - 1 or y == t - 1:
                v = 0.84 if hash2(x, 1, seed) < 0.5 else 0.7            # the front arris, worn
            if hash2(x // 2, y // 2, seed + 13) < 0.08:
                v = 0.45                                                # a hollow in the top
            c.set(x, y, stop(v))
            if owner is not None:
                owner[(x, y)] = stop(v)
        top_at[x] = lt
        # the face's top course in the shadow of the ledge's lip
        if (x, t) in stand and hash2(x, t, seed + 3) < 0.6:
            c.set(x, t, ST[3])
    return top_at


def moss_on(c, owner, seed, share, low, high):
    """Moss in the joints and on the faces between rows `high` and `low`: lichen's shade and deep, thickest
    in the joints, where the water runs."""
    for (x, y), col in owner.items():
        if not (high <= y <= low):
            continue
        p = share * (2.2 if col in (OCC, ST[4]) else 1.0)
        if hash2(x // 2, y, seed + 21) < p:
            c.set(x, y, LI[4] if hash2(x, y, seed + 22) < 0.6 else LI[3])


def ivy(c, rnd, anchors, length, density=0.7, lit_side=True):
    """Ivy pouring from ledges: strands that hang from each anchor, wander and thin as they fall, leaves
    along them in verdant's shade with a lit leaf on the key side now and then, and a pixel of shadow under
    each leaf. A strand ends short of the ground, unless it is a long one."""
    for (ax, ay) in anchors:
        n = 1 + rnd.randrange(3)
        for s in range(n):
            x = float(ax + rnd.randrange(-2, 3))
            y = ay
            ln = int(length * (0.4 + rnd.random() * 0.8))
            drift = (rnd.random() - 0.5) * 0.5
            for j in range(ln):
                y += 1
                x += drift + (rnd.random() - 0.5) * 0.8
                ix = int(TR.round2(x))
                c.set(ix, y, VE[4])
                if rnd.random() < density * (1.0 - j / float(ln + 4)):
                    side = -1 if rnd.random() < 0.5 else 1
                    lx = ix + side
                    c.set(lx, y, VE[3])
                    c.set(lx + side, y, VE[4])
                    c.set(lx, y - 1, VE[2] if (lit_side and side < 0 and rnd.random() < 0.08) else VE[3])
                    c.set(lx + side, y + 1, OCC if rnd.random() < 0.5 else VE[4])


def ivy_mat(c, seed, cx, cy, rx, ry, n, rmin=2, rmax=4):
    """A mat of ivy lying over a top or a heap: small clusters, dark, lit on the key side by the crown
    painter (trees.crown) with verdant's dark half only."""
    rnd = TR.Rng(seed)
    clusters = TR.fill_clusters(rnd, cx, cy, rx, ry, n, rmin, rmax)
    TR.crown(c, clusters, CROWN, cx, cy, rx + rmax, ry + rmax, seed, ragged=0.5, bias=-0.24)


def weeds(c, rnd, x, base, n):
    """A few weeds where a joint meets the ground: blades in lichen's shade and verdant's shade."""
    for k in range(n):
        wx = x + rnd.randrange(-3, 4)
        h = 2 + rnd.randrange(4)
        lean = rnd.choice((-1, 0, 1))
        for j in range(h):
            c.set(wx + (lean if j > h // 2 else 0), base - j, LI[3] if j < h - 1 else VE[3])


def block3(c, cx, cy, w, h, depth, ang, seed):
    """A loose block in three-quarter view, turned by `ang` so a heap of them is never a course: its top a
    rotated, foreshortened quad (base, lit along the two edges toward the key), the two sides that face the
    viewer hanging below it - the one turned toward the key in base, the other in shade - the whole ringed
    in occlusion where it lies on or against what is behind it (one dark pixel between two stones is what
    lets a heap read as stones), and a corner knocked off its top."""
    ca, sa = math.cos(ang), math.sin(ang)
    hw, hd = w / 2.0, depth / 2.0
    corners = [(-hw, -hd), (hw, -hd), (hw, hd), (-hw, hd)]
    pts = [(cx + px * ca - py * sa, cy + (px * sa + py * ca) * 0.55) for (px, py) in corners]
    low = max(range(4), key=lambda i: pts[i][1])
    faces = []
    for nb in ((low - 1) % 4, (low + 1) % 4):
        a, b = pts[low], pts[nb]
        faces.append(([a, b, (b[0], b[1] + h), (a[0], a[1] + h)], b[0] < a[0]))
    # the occlusion ring: every part drawn one pixel out, first
    for (dx, dy) in ((-1, 0), (1, 0), (0, -1), (0, 1), (1, 1)):
        for (f, _) in faces:
            c.poly([(p[0] + dx, p[1] + dy) for p in f], OCC)
        c.poly([(p[0] + dx, p[1] + dy) for p in pts], OCC)
    for (f, west) in faces:
        c.poly(f, ST[2] if west else ST[3])
        bottom = int(max(p[1] for p in f))
        for x in range(int(min(p[0] for p in f)), int(max(p[0] for p in f)) + 1):
            if hash2(x, bottom, seed) < 0.6:
                c.set(x, bottom, ST[4])
    c.poly(pts, ST[2])
    # the arris on the key side of the top
    ul = min(range(4), key=lambda i: pts[i][0] + pts[i][1])
    p0 = pts[ul]
    for i in ((ul - 1) % 4, (ul + 1) % 4):
        q = pts[i]
        steps = int(max(abs(q[0] - p0[0]), abs(q[1] - p0[1]))) + 1
        for s in range(steps):
            t = s / float(max(1, steps - 1))
            x, y = int(TR.round2(p0[0] + (q[0] - p0[0]) * t)), int(TR.round2(p0[1] + (q[1] - p0[1]) * t))
            if hash2(x, y, seed + 2) < 0.8:
                c.set(x, y, ST[1])
    # a knocked corner: a notch out of the far corner of the top, the break in deep
    knock = int(hash2(int(cx), int(cy), seed) * 4)
    if knock:
        k = pts[(ul + 2) % 4]
        for dy in range(2):
            for dx in range(1 + knock // 2):
                c.set(int(k[0]) - dx, int(k[1]) - dy, ST[4] if dy == 0 else ST[3])


def rubble_at(c, rnd, cx, base, half, n, seed, size=(3, 7)):
    """Broken blocks spilled at a foot: block3 at every angle, back to front, some half buried."""
    pieces = []
    for k in range(n):
        x = cx + (rnd.random() - 0.5) * 2 * half
        y = base - rnd.random() * 4
        w = size[0] + rnd.randrange(size[1] - size[0] + 1)
        pieces.append((y, x, w))
    pieces.sort()
    for (y, x, w) in pieces:
        block3(c, x, y - 1, w, max(2, w // 2), max(2, w * 0.7), (rnd.random() - 0.5) * 1.6, seed + int(x * 7))


def chips(c, rnd, cx, base, half, n):
    """Chips of stone round a foot, a pixel or two each, lit on top, on a pixel of shadow."""
    for k in range(n):
        x = int(cx + (rnd.random() - 0.5) * 2 * half)
        y = base - rnd.randrange(3) + 1
        w = 1 + rnd.randrange(2)
        for dx in range(w):
            c.set(x + dx, y, ST[3])
        c.set(x, y - 1, ST[2])
        c.set(x + w, y + 1, OCC)


# --- the pieces --------------------------------------------------------------------------------------

# How tall a wall still standing must be, in authored pixels, before it blocks: 14 is 28 on screen, a third
# of the wizard's 80 - his knee. Lower than that is a course or two and rubble, waded through.
STANDS_FROM = 14

def wall_run(seed, w, h, pier_at, pier_w, low, drop=0.45):
    """A length of fallen wall left standing: one PIER still near its full height at `pier_at` (a share of
    the width), the courses either side of it broken off block by block and stepping down - steeper on one
    side - into a run of one or two courses at `low`, and that into the rubble it shed. Ivy pours off the
    pier's ledges; moss in the lower joints; the rubble at its foot is its own fallen blocks."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    px = int(w * pier_at)
    c.anchor_x = px
    TR.shade_pool(c, px + w * 0.10, base + 1, w * 0.42, 6 + w * 0.02, 0.45)
    TR.contact(c, px, base + 1, pier_w * 0.55)
    tall = h - 8
    x0, x1 = 3, w - 4
    noise = [rnd.random() for _ in range(w + 8)]

    def profile(x):
        if x < x0 or x > x1:
            return 0
        d = (x - px) / float(pier_w * 0.5)
        if abs(d) <= 1.0:
            return tall - int(noise[x] * 3)
        # stepping down from the pier, faster toward the short end, in blocks
        side = (x1 - px) if d > 0 else (px - x0)
        t = (abs(x - px) - pier_w * 0.5) / max(1.0, side - pier_w * 0.5)
        steep = drop if d > 0 else drop * 0.6
        hgt = tall * (1.0 - t / max(0.2, steep)) if t < steep else 0
        return int(max(low * (1.0 - max(0.0, t - 0.8) * 4), hgt) + noise[x] * 4)

    owner = {}
    tops = masonry(c, x0, x1, base, profile, seed, ledge=4, owner=owner)
    # WHAT BLOCKS IS WHAT STANDS: every column of wall taller than the wizard's knee (STANDS_FROM), so he never
    # walks through masonry that is up to his waist; what is lower - the last course or two and the rubble -
    # he wades through, as through a bramble. The anchor goes to the middle of that span.
    # (a column's height is its face: the ledge over it is the wall's thickness seen from above, not height)
    standing = [x for x in range(x0, x1 + 1) if base - tops.get(x, base) - 5 > STANDS_FROM]
    c.anchor_x = (min(standing) + max(standing) + 1) // 2
    c.trunk_world = (max(standing) - min(standing) + 1) * TR.SCALE
    moss_on(c, owner, seed, 0.10, base, base - 16)
    moss_on(c, owner, seed + 5, 0.03, base - 16, 0)
    # ivy off the pier's top and its higher ledges, and a mat on its top
    anchors = sorted(((x, y) for (x, y) in tops.items() if abs(x - px) < pier_w * 0.9), key=lambda p: p[1])[:6]
    ivy(c, rnd, anchors[::2], tall * 0.7, 0.75)
    ivy_mat(c, seed + 3, px + pier_w * 0.1, min(tops.values()) + 2, pier_w * 0.45, 3, 7)
    # rubble at the foot, more on the side the wall fell to
    rubble_at(c, rnd, px + w * 0.18, base + 1, w * 0.36, 7 + w // 16, seed + 40)
    chips(c, rnd, px, base, w * 0.5, 10)
    weeds(c, rnd, px - pier_w // 2, base, 3)
    weeds(c, rnd, x1 - 6, base, 3)
    return c


def spire_stump(seed, w, h):
    """The stump of a round tower: coursed blocks round a drum, the joints bowing down across its front as
    a ring seen from above does, every block its own width; broken off BLOCK BY BLOCK, high on its left
    (the key side) and falling away in ragged steps to the right, a bite out of its front where a run of
    blocks fell; along every broken top the wall's thickness shows as a lit rim, and over the low side you
    see into the hollow - the inside of the far wall in shade, its own broken rim above. One window slit,
    dark, its sill lit; blocks missing from the face; ivy up the right flank; its fallen stones round its
    foot."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    c.anchor_x = cx
    r = w * 0.42
    ry = r * 0.32                                    # how far a ring bows, seen from above
    TR.shade_pool(c, cx + w * 0.14, base + 1, w * 0.5, 7, 0.5)
    TR.contact(c, cx, base + 1, r * 0.95)

    def stands(u):
        """How high the wall still stands at u (-1 left .. 1 right), above the base: a broken ramp."""
        hi = h - 10
        lo = h * 0.36
        t = (u + 1.0) * 0.5
        f = hi - (hi - lo) * (t ** 1.4)
        if 0.05 < u < 0.42:
            f -= h * 0.14                                # a run of blocks fallen out of the front
        return f

    beds = courses_up(rnd, base, 2, 7, 10)
    owner = {}
    top_at = {}
    for k, (bot, top) in enumerate(beds):
        # block edges round the ring, in angle, each block its own width
        edges = []
        a = -math.pi / 2 - rnd.random() * 0.4
        while a < math.pi / 2 + 0.4:
            edges.append(a)
            a += 0.26 + rnd.random() * 0.30
        for i in range(len(edges) - 1):
            a0, a1 = edges[i], edges[i + 1]
            um = math.sin(max(-1.5, min(1.5, (a0 + a1) / 2.0)))
            if base - top > stands(um) + (hash2(i, k, seed + 31) - 0.5) * 9:
                continue                                 # this block fell
            hole = hash2(i, k, seed + 33) < 0.06 and k > 1 and base - top < stands(um) - 10
            bias = (hash2(i, k, seed) - 0.5) * 0.16
            for x in range(int(cx + math.sin(max(-1.5707, a0)) * r) - 1, int(cx + math.sin(min(1.5707, a1)) * r) + 2):
                u = (x - cx) / r
                if abs(u) > 1.0:
                    continue
                th = math.asin(u)
                if not (a0 <= th < a1):
                    continue
                nz = math.sqrt(max(0.0, 1.0 - u * u))
                bow = ry * nz
                ytop, ybot = int(top + bow), int(bot + bow)
                edge_l = th - a0 < 0.05
                for y in range(ytop, ybot):
                    if y > base:
                        continue
                    row = y - ytop
                    if y == ybot - 1:
                        col = OCC if hash2(x, y, seed) < 0.7 else ST[4]
                    elif th > a1 - 0.06 * (1.0 + abs(u)):
                        col = ST[4]
                    elif hole:
                        col = OCC if row < ybot - ytop - 3 else (ST[2] if y == ybot - 2 else ST[4])
                    else:
                        v = 0.30 + 0.44 * (u * KX + nz * KZ) + bias
                        if row == 0:
                            v += 0.14
                        elif edge_l:
                            v += 0.08
                        if hash2(x, y, seed + 7) < 0.05:
                            v -= 0.22
                        if y > base - 8:
                            v -= 0.06
                        col = stop(v)
                    c.set(x, y, col)
                    owner[(x, y)] = col
                    if ytop < top_at.get(x, 10 ** 6):
                        top_at[x] = ytop
    # over the broken top: the hollow, then the near rim. Behind the near wall the far wall's inside face, in
    # shade and deep, stands to its own broken top (higher than the near wall's on the low side, since it
    # is the back of the ring and further up the screen), and its rim is lit too.
    for x in range(int(cx - r), int(cx + r) + 1):
        if x not in top_at:
            continue
        u = (x - cx) / r
        nz = math.sqrt(max(0.0, 1.0 - u * u))
        near = top_at[x]
        wall = 3                                         # the wall's thickness, seen from above
        if abs(u) < 0.88:
            # the far wall broken off in its own blocks: a step every few pixels, a notch now and then
            seg = int((x - cx + r) // 5)
            drop = (0, 3, 7, 4, 0, 10)[int(hash2(seg, 9, seed) * 6)]
            far = int(base - stands(u * 0.6 - 0.25) * 0.92 - 2 * ry * nz + drop)
            for y in range(far, near - wall):
                col = ST[4] if hash2(x // 2, y // 3, seed + 12) < 0.75 else ST[3]
                if (y - far) % 9 == 8:
                    col = OCC
                c.set(x, y, col)
            for k in range(2):
                c.set(x, far - 1 - k, stop(0.9 - k * 0.15) if hash2(x, k, seed + 14) < 0.8 else ST[2])
        for k in range(wall):
            v = 0.98 - k * 0.14 if hash2(x, k, seed + 11) < 0.8 else 0.72
            c.set(x, near - 1 - k, stop(v))
    # a window slit, dark, its sill lit
    sx = int(cx - r * 0.32)
    sy0, sy1 = int(base - h * 0.62), int(base - h * 0.47)
    for y in range(sy0, sy1):
        if y < top_at.get(sx, 0) + 3:
            continue
        c.set(sx, y, OCC)
        c.set(sx + 1, y, OCC)
        c.set(sx - 1, y, ST[3])
        c.set(sx + 2, y, ST[4])
    c.set(sx, sy1, ST[2])
    c.set(sx + 1, sy1, ST[2])
    moss_on(c, owner, seed, 0.09, base, base - 22)
    moss_on(c, owner, seed + 3, 0.03, base - 22, 0)
    # ivy climbing the right flank, and pouring off the high rim on the left
    ivy_mat(c, seed + 5, cx + r * 0.66, base - h * 0.30, r * 0.30, h * 0.24, 22, 2, 4)
    hx = int(cx - r * 0.7)
    ivy(c, rnd, [(hx, top_at.get(hx, 20) - 2), (hx + 6, top_at.get(hx + 6, 20) - 2)], h * 0.45, 0.7)
    rubble_at(c, rnd, cx + r * 0.5, base + 1, r * 1.1, 8, seed + 40, (4, 9))
    chips(c, rnd, cx, base, r * 1.3, 12)
    weeds(c, rnd, int(cx - r), base, 4)
    return c


def column(c, cx, base, top, r, seed, owner=None, flutes=True, shift=None, jag_px=5):
    """A standing column from `base` up to its broken `top`: a drum shaded by its round normal, fluted, its
    drums no longer quite in line (`shift`: the drums' offsets, base up), the joints between them dark, its
    top broken off jagged with the break lit from above."""
    rnd = TR.Rng(seed)
    hgt = base - top
    joints = []
    y = base
    while y > top + 6:
        y -= 11 + rnd.randrange(7)
        joints.append(y)
    shift = shift or [rnd.choice((-1, 0, 0, 1)) for _ in range(len(joints) + 1)]
    # the break: a few chunks bitten out of the top, not a sawtooth
    jag = [int((hash2(i // 3, 1, seed) * 0.7 + hash2(i, 2, seed) * 0.3) * jag_px) for i in range(int(r * 2) + 4)]
    for y in range(top, base + 1):
        drum = sum(1 for j in joints if y < j)
        off = shift[min(drum, len(shift) - 1)]
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            u = (x - off - cx) / r
            if abs(u) > 1.0:
                continue
            i = int(x - cx + r) + 1
            if y < top + jag[max(0, min(len(jag) - 1, i))]:
                continue
            nz = math.sqrt(max(0.0, 1.0 - u * u))
            v = 0.34 + 0.48 * (u * KX + nz * KZ)
            if flutes and abs(u) < 0.92 and (int(TR.round2(u * r * 1.6)) % 3 == 0):
                v -= 0.13
            if y in joints or y - 1 in joints and hash2(x, y, seed) < 0.3:
                col = OCC if hash2(x, y, seed + 1) < 0.75 else ST[4]
            else:
                if hash2(x // 2, y // 2, seed + 4) < 0.05:
                    v -= 0.2                                    # weathered out
                if y > base - 5:
                    v -= 0.06
                col = TR.band(STONE, v, x, y, 0.2)
            c.set(x, y, col)
            if owner is not None:
                owner[(x, y)] = col
    # the break, seen from above: lit chips along the jagged top
    for x in range(int(cx - r), int(cx + r) + 1):
        i = int(x - cx + r) + 1
        t = top + jag[max(0, min(len(jag) - 1, i))]
        c.set(x, t, ST[1] if hash2(x, 2, seed) < 0.7 else ST[2])
        if hash2(x, 3, seed) < 0.5:
            c.set(x, t + 1, ST[2])
    return joints


def step_block(c, x0, x1, base, rise, depth, seed):
    """One step of a stylobate in three-quarter view: its tread (base, its arris lit) over its riser (shade),
    the corners knocked off, broken along its length."""
    for x in range(x0, x1 + 1):
        if hash2(x // 3, 1, seed) < 0.1 and x0 + 3 < x < x1 - 3:
            continue                                            # a piece gone out of the step
        end = min(x - x0, x1 - x)
        for y in range(base - rise - depth + max(0, 2 - end), base + 1):
            if y <= base - rise:
                v = 0.98 if y == base - rise - depth + max(0, 2 - end) else 0.75
            else:
                v = 0.5 if y > base - rise else 0.75
                if y == base:
                    v = 0.25
            if hash2(x, y, seed + 3) < 0.05:
                v -= 0.25
            c.set(x, y, stop(v))


def colonnade(seed, w, h):
    """What is left of a colonnade: a broken stylobate, one column still standing on it (broken off two
    thirds up), a second cut down to a stump, and a drum of it lying in front - never two uprights of a
    height, so it is never a gateway, and never one pillar alone on a base (the Breach Obelisk)."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    tall_x = int(w * 0.32)
    c.anchor_x = tall_x
    TR.shade_pool(c, w * 0.55, base + 1, w * 0.48, 6, 0.45)
    TR.contact(c, w * 0.5, base + 1, w * 0.42)
    step_block(c, 2, w - 3, base, 3, 3, seed)
    step_block(c, 6, w - 9, base - 6, 3, 3, seed + 1)
    sb = base - 12
    owner = {}
    r = 8.5
    column(c, tall_x, sb, sb - int(h * 0.74), r, seed + 2, owner, jag_px=10)
    stump_x = int(w * 0.74)
    column(c, stump_x, sb + 1, sb - int(h * 0.26), r, seed + 3, owner, jag_px=7)
    moss_on(c, owner, seed, 0.06, sb, sb - 30)
    # ivy wound up the standing column and spilling over the step
    ivy(c, rnd, [(tall_x - 3, sb - int(h * 0.70)), (tall_x + 4, sb - int(h * 0.55))], h * 0.45, 0.8)
    ivy_mat(c, seed + 4, tall_x + 2, sb - 2, 9, 3, 6)
    ivy_mat(c, seed + 6, stump_x - 1, sb - int(h * 0.26) + 2, 6, 2, 4, 2, 3)
    # a drum lying in front, broken, and the rubble
    shaft(c, w * 0.50, base + 1, 22, 6.0, -0.06, seed + 7)
    rubble_at(c, rnd, w * 0.3, base + 1, w * 0.2, 4, seed + 40, (3, 5))
    chips(c, rnd, w * 0.5, base, w * 0.5, 10)
    weeds(c, rnd, int(w * 0.12), base - 3, 3)
    return c


def plinth(seed, w, h):
    """A statue's plinth with no statue: a stepped base, the die, a moulded top a little wider than the die,
    and on the top the two feet it stood on, broken off above the ankle - the breaks lit. Whatever stood
    there has gone. Widest at the foot, so it is a pedestal and never a slab on an upright (the
    Lintel-Bearer). Moss in its joints, ivy up one corner, a crack down the die, a corner knocked off."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    c.anchor_x = cx
    TR.shade_pool(c, cx + w * 0.12, base + 1, w * 0.5, 5, 0.45)
    TR.contact(c, cx, base + 1, w * 0.45)
    owner = {}

    def slab(x0, x1, yb, rise, tread, sd, knock=0):
        for x in range(x0, x1 + 1):
            end = min(x - x0, x1 - x)
            for y in range(yb - rise - tread, yb + 1):
                if end < 1 and y < yb - rise - tread + 2:
                    continue
                if knock and x1 - x < knock and y < yb - rise - tread + (knock - (x1 - x)) + 1:
                    continue                                   # the knocked corner
                if y <= yb - rise:
                    v = 0.98 if y == yb - rise - tread else 0.76
                else:
                    v = 0.52 if x - x0 > 1 else 0.66
                    if y == yb:
                        v = 0.26
                    if x1 - x < 2:
                        v -= 0.14
                if hash2(x, y, sd) < 0.05:
                    v -= 0.24
                col = stop(v)
                c.set(x, y, col)
                owner[(x, y)] = col

    slab(1, w - 2, base, 4, 4, seed)                         # the lower step, widest
    slab(4, w - 5, base - 8, 4, 3, seed + 3, knock=3)       # the upper step
    dx0, dx1 = 7, w - 8
    die_top = int(base - h * 0.62)
    for x in range(dx0, dx1 + 1):                              # the die: one block, its face in shade
        for y in range(die_top, base - 15):
            v = 0.52 if x > dx0 + 1 else 0.66
            if dx1 - x < 2:
                v -= 0.14
            if hash2(x // 2, y // 3, seed + 5) < 0.06:
                v -= 0.22
            col = stop(v)
            c.set(x, y, col)
            owner[(x, y)] = col
    x = dx0 + (dx1 - dx0) * 0.6
    for y in range(die_top + 2, base - 16):                    # a crack down the die
        x += (rnd.random() - 0.5) * 1.2
        c.set(int(x), y, OCC)
        c.set(int(x) - 1, y, ST[3])
    slab(5, w - 6, die_top, 3, 6, seed + 1)                   # the moulded top
    # the feet: one ahead of the other, broken above the ankle; the breaks lit from above
    ft = die_top - 3 - 4
    for (fx, fy, fl) in ((cx - 9, ft - 1, 9), (cx + 1, ft + 1, 10)):
        for x in range(fx, fx + fl):
            toe = x - fx
            top = fy + (1 if toe > fl - 4 else 0)                # the instep falls to the toes
            for y in range(top, fy + 4):
                if x == fx + fl - 1 and y == top:
                    continue
                v = 0.84 if y == top else 0.6
                if x > fx + fl - 3:
                    v -= 0.1
                c.set(x, y, stop(v))
            c.set(x, fy + 4, OCC)
        for t in range(fx + fl - 4, fx + fl, 2):
            c.set(t, fy + 2, ST[4])                             # the toes
        # the shin, broken off below the knee: round, lit on its left, a joint of occlusion between it
        # and the foot; its break slanting, lit from above, a chip out of it
        shin = 11 if fx < cx else 8
        for y in range(fy - shin, fy):
            c.set(fx, y, OCC)
            c.set(fx + 1, y, ST[2])
            c.set(fx + 2, y, ST[2] if y > fy - shin + 3 else ST[3])
            c.set(fx + 3, y, ST[3])
            c.set(fx + 4, y, ST[4])
            c.set(fx + 5, y, OCC)
        for k in range(4):
            c.set(fx + 1 + k, fy - shin - 1 + (k // 2), ST[1] if k < 2 else ST[2])
        c.set(fx + 4, fy - shin, ST[4])
    moss_on(c, owner, seed, 0.09, base, base - 14)
    moss_on(c, owner, seed + 2, 0.06, die_top + 3, die_top - 9)
    ivy(c, rnd, [(dx1 - 1, die_top - 3)], h * 0.5, 0.85)
    rubble_at(c, rnd, cx - w * 0.25, base + 1, w * 0.22, 3, seed + 40, (4, 6))
    chips(c, rnd, cx, base, w * 0.55, 8)
    return c


def root_wall(seed, w, h):
    """A tree that seeded on top of a wall and grew: a stretch of broken masonry - half the tree's height -
    with a twisted trunk out of its top, and its roots poured down over the face, hugging it, running along
    a joint and down again, prising the stones apart, into the ground at its foot. A dark, ragged crown in
    two or three masses, as the ruins' gnarled trees wear theirs."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    c.anchor_x = cx
    TR.shade_pool(c, cx + w * 0.10, base + 1, w * 0.44, 7, 0.5)
    TR.contact(c, cx, base + 1, w * 0.3)
    wall_h = int(h * 0.42)
    hw = int(w * 0.22)
    noise = [rnd.random() for _ in range(w + 4)]

    def profile(x):
        d = (x - cx) / float(hw)
        if abs(d) > 1.0:
            return 0
        f = wall_h - noise[x] * 3
        if d > 0.55:
            f -= 10                                         # the right end broken down a course
        if d < -0.7:
            f -= 5
        return int(f)

    owner = {}
    tops = masonry(c, cx - hw, cx + hw, base, profile, seed, ledge=4, owner=owner, holes=0.05)
    moss_on(c, owner, seed, 0.12, base, base - wall_h)
    wall_top = tops.get(cx, min(tops.values()))
    # the roots: from the trunk's foot they run out along the wall's top, over its edge, and down the face to
    # the ground, hugging it - now and then along a joint for a few pixels - prising the stones apart beside
    # them; thick where they leave the trunk and thinning as they go
    tx = cx + 1
    bark = [LN[2], LN[3], LN[3], LN[4], OCC]

    def root_px(ix, iy, wd, down):
        n = max(1, int(round(wd)))
        for j in range(n):
            col = LN[2] if (j == 0 and n > 1) else (LN[4] if (j == n - 1 and n > 1) else LN[3])
            if down:
                c.set(ix + j - n // 2, iy, col)
            else:
                c.set(ix, iy + j - n // 2, LN[3] if j < n - 1 else LN[4])

    for k in range(6):
        side = -1 if k % 2 == 0 else 1
        reach = min(hw - 4, hw * (0.2 + 0.6 * ((k // 2) / 2.0)) + rnd.randrange(3))
        sx = int(tx + side * reach)
        wd = 3.8 - k * 0.35
        # along the top
        x = tx + side
        y = wall_top + 2
        while (x - sx) * side < 0:
            root_px(x, int(y), wd, False)
            x += side
            y += 0.12
            wd = max(1.2, wd - 0.03)
        # down the face
        fx = float(sx)
        yy = int(y)
        jog = 0
        phase = rnd.random() * TAU
        # two in six end on the face, in a joint, short of the ground
        stop_at = base + 1 if k not in (2, 5) else int(y + (base - y) * (0.45 + rnd.random() * 0.3))
        while yy < stop_at:
            if jog:
                fx += side
                jog -= 1
            else:
                yy += 1
                fx += math.sin(yy * 0.22 + phase) * 0.45 + (rnd.random() - 0.5) * 0.4
                fx = max(cx - hw + 2.0, min(cx + hw - 2.0, fx))        # on the face, not in the air
                if rnd.random() < 0.04 and yy < base - 5:
                    jog = 2 + rnd.randrange(3)
            ix = int(TR.round2(fx))
            root_px(ix, yy, wd, not jog)
            edge = ix + int(round(wd)) // 2 + 1
            if (edge, yy) in owner and not jog:
                c.set(edge, yy, OCC)                         # the stone prised apart beside it
            wd = max(1.0, wd - 0.02)
        if stop_at <= base:
            c.set(int(fx), stop_at, OCC)                     # into a joint
            continue
        c.set(int(fx) - 1, base + 1, LN[3])
        c.set(int(fx), base + 1, LN[4])
        c.set(int(fx) + 1, base + 2, OCC)
    # the trunk, out of the wall's top, twisting
    fork = h * 0.30
    TR.trunk(c, tx, wall_top + 2, fork, 11, 6, bark, seed, flare=5, lean=2.5)
    lean_x = tx + 2.5 * (wall_top - fork) * 0.1
    TR.limb(c, lean_x, fork + 4, -math.pi / 2 - 1.0, h * 0.16, 3, bark, 1, rnd)
    masses = [(lean_x + w * 0.10, h * 0.16, w * 0.30, h * 0.12), (lean_x - w * 0.20, h * 0.24, w * 0.20, h * 0.10),
              (lean_x + w * 0.28, h * 0.28, w * 0.14, h * 0.08)]
    clusters = []
    for (mx, my, mrx, mry) in masses:
        TR.limb(c, lean_x, fork + 3, math.atan2(my - fork, mx - lean_x), math.hypot(mx - lean_x, my - fork), 3, bark, 0, rnd)
        clusters += TR.fill_clusters(rnd, mx, my, mrx * 0.8, mry * 0.8, int(mrx * 0.55), 5, 9)
    clusters.sort(key=lambda k: k[1])
    # dark leaf: the crown painter's glints are its first tone, so they are verdant's shade here, and the
    # base is laid a leaf at a time on the lit tops of the clusters after it
    TR.crown(c, clusters, [VE[3], VE[3], VE[4], OCC], lean_x, h * 0.22, w * 0.45, h * 0.18, seed, bias=0.06)
    for (qx, qy, qr, _) in clusters:
        if (qx - lean_x) * TR.LX + (qy - h * 0.22) * TR.LY > 0 and hash2(int(qx), int(qy), seed) < 0.5:
            c.set(int(qx - qr * 0.4), int(qy - qr * 0.55), VE[2])
    ivy(c, rnd, [(cx - hw + 3, tops.get(cx - hw + 3, wall_top)), (cx + hw - 5, tops.get(cx + hw - 5, wall_top))],
        wall_h * 0.7, 0.8)
    rubble_at(c, rnd, cx + w * 0.16, base + 1, w * 0.3, 6, seed + 40, (4, 8))
    chips(c, rnd, cx, base, w * 0.45, 10)
    return c


def shaft(c, x0, base, length, r, tilt, seed, west_end=True, broken_east=True):
    """A length of column shaft lying on its side along the ground: round across (lit on top, dark below),
    its flutes running along it, its west end - turned toward the key - a worn disc, its east end broken
    off ragged; on its shadow. `tilt` turns it a little off the row."""
    x1 = int(x0 + length)
    jag = [int(hash2(i, 5, seed) * 4) for i in range(int(2 * r) + 4)]
    for x in range(int(x0) - 1, x1 + 3):
        c.set(x, int(base + (x - x0) * tilt), OCC)
        c.set(x, int(base + 1 + (x - x0) * tilt), OCC)
    for x in range(int(x0), x1 + 1):
        yc = base - r + (x - x0) * tilt
        for y in range(int(yc - r), int(yc + r) + 1):
            ny = (y - yc) / r
            if abs(ny) > 1.0:
                continue
            if broken_east and x > x1 - jag[max(0, min(len(jag) - 1, int(y - yc + r)))]:
                continue
            nz = math.sqrt(max(0.0, 1.0 - ny * ny))
            v = 0.30 + 0.56 * (ny * KY + nz * KZ)
            if abs(ny) < 0.8 and int(TR.round2((ny + 1.0) * r * 1.35)) % 3 == 0:
                v -= 0.09                                      # a flute, along the shaft
            if hash2(x // 2, y // 2, seed + 4) < 0.05:
                v -= 0.22
            col = TR.band(STONE, v, x, y, 0.18)
            if ny > 0.88:
                col = ST[4] if hash2(x, y, seed) < 0.6 else OCC
            c.set(x, y, col)
    if broken_east:
        # the break: lit chips along the ragged east end where it faces up
        for i in range(int(2 * r) + 1):
            y = int(base - 2 * r + (x1 - x0) * tilt) + i
            x = x1 - jag[max(0, min(len(jag) - 1, i))]
            if i < r:
                c.set(x, y, ST[2])
    if west_end:
        yc = base - r + 0 * tilt
        erx = max(2.0, r * 0.42)
        for y in range(int(yc - r), int(yc + r) + 1):
            for x in range(int(x0 - erx) - 1, int(x0) + 1):
                d = ((x - x0) / erx) ** 2 + ((y - yc) / r) ** 2
                if d > 1.0:
                    continue
                col = ST[2] if d < 0.72 else ST[3]
                if (y - yc) > r * 0.4:
                    col = ST[3]
                if 0.30 < d < 0.42 and hash2(x, y, seed) < 0.55:
                    col = ST[3]
                if d < 0.05:
                    col = OCC
                c.set(x, y, col)
        c.set(int(x0 - erx) + 1, int(yc - r * 0.55), ST[1])
        c.set(int(x0 - erx) + 2, int(yc - r * 0.75), ST[1])


def drums(seed, w, h, n):
    """A column that fell and broke: its shaft lying where it fell in two or three long pieces, roughly in
    line, rolled a little apart and askew, each broken off ragged at one end; moss along their tops, weeds
    in the gaps, chips about."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + 3)
    base = h - 2
    TR.shade_pool(c, w * 0.55, base + 1, w * 0.48, 4, 0.4)
    r = h * 0.40
    x = 3.0 + r * 0.42
    total = w - 6 - r * 0.42
    lens = [0.8 + rnd.random() * 0.6 for _ in range(n)]
    scale = (total - 4 * (n - 1)) / sum(lens)
    tops = []
    for k in range(n):
        ln = lens[k] * scale
        tilt = (rnd.random() - 0.5) * 0.10
        dy = rnd.randrange(3)
        shaft(c, x, base - dy, ln, r * (0.94 + rnd.random() * 0.06), tilt, seed + k * 5)
        tops.append((x + ln * 0.5, base - dy - 2 * r, ln))
        x += ln + 2 + rnd.randrange(4)
    for (tx, ty, ln) in tops:
        for k in range(int(ln * 0.5)):
            mx = int(tx + (rnd.random() - 0.5) * ln * 0.8)
            my = int(ty + 1 + rnd.random() * 2)
            if c.get(mx, my)[3]:
                c.set(mx, my, LI[4] if rnd.random() < 0.6 else LI[3])
        weeds(c, rnd, int(tx + ln * 0.55), base, 2)
    ivy_mat(c, seed + 9, w * 0.70, base - r * 0.7, w * 0.10, r * 0.4, 5, 2, 3)
    chips(c, rnd, w * 0.5, base, w * 0.5, 8)
    return c


def capital(seed, w, h):
    """An Ionic capital fallen from its column and sunk to its neck in the ground: the abacus across the
    top (a slab, its tread lit), the two volutes curled under its ends - each a spiral groove cut into a
    boss, its outer turn lit - and the egg moulding between them; a corner broken away, moss on the top."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + 3)
    base = h - 2
    cx = w // 2
    TR.shade_pool(c, cx + 3, base + 1, w * 0.46, 4, 0.45)
    TR.contact(c, cx, base + 1, w * 0.40)
    at = int(base - h * 0.92)
    tread = 4
    ey = at + tread + 3
    # the echinus: a cushion between the volutes, its eggs worn
    for x in range(int(cx - w * 0.28), int(cx + w * 0.28) + 1):
        u = (x - cx) / (w * 0.28)
        for y in range(ey, base + 1):
            v = 0.56 - u * 0.16 - (y - ey) / float(base - ey) * 0.22
            if y < ey + 5 and ((x - cx) % 4 == 0 or y == ey + 4):
                v -= 0.16
            c.set(x, y, stop(v))
    # the abacus: a slab across the top, its tread lit, a corner gone
    ax0, ax1 = int(cx - w * 0.47), int(cx + w * 0.47)
    for x in range(ax0, ax1 + 1):
        for y in range(at, ey):
            if x > ax1 - 5 and y < at + (x - (ax1 - 5)):
                continue
            row = y - at
            v = 0.76 if row < tread else 0.5
            if row == 0:
                v = 0.98 if hash2(x, 0, seed) < 0.65 else 0.8
            if row == tread + 2:
                v = 0.26
            if x - ax0 < 1:
                v += 0.1
            c.set(x, y, stop(v))
    # the volutes: a boss at each end, a spiral groove cut into it, its outer turn lit on the key side
    for side in (-1, 1):
        vx, vy = cx + side * w * 0.34, ey + 4
        rad = 5.2
        for y in range(int(vy - rad) - 1, int(vy + rad) + 2):
            for x in range(int(vx - rad) - 1, int(vx + rad) + 2):
                d = math.hypot(x - vx, y - vy)
                if d > rad:
                    continue
                v = 0.62 - ((x - vx) * 0.5 + (y - vy) * 0.6) / rad * 0.3
                c.set(x, y, stop(v))
        for k in range(70):
            t = k / 70.0 * TAU * 1.75
            rr = rad * 0.92 * (1.0 - k / 82.0)
            x, y = int(TR.round2(vx + math.cos(t) * rr * side)), int(TR.round2(vy + math.sin(t) * rr))
            c.set(x, y, ST[4])
        c.set(int(vx), int(vy), OCC)
        c.set(int(vx - rad * 0.7), int(vy - rad * 0.6), ST[1])
        c.set(int(vx - rad * 0.5), int(vy - rad * 0.8), ST[1])
    for k in range(int(w * 0.6)):
        mx = int(cx + (rnd.random() - 0.5) * w * 0.75)
        if c.get(mx, at + 1)[3]:
            c.set(mx, at + 1 + rnd.randrange(2), LI[4] if rnd.random() < 0.6 else LI[3])
    chips(c, rnd, cx, base, w * 0.55, 6)
    weeds(c, rnd, int(cx - w * 0.42), base, 2)
    return c


def rubble_heap(seed, w, h, n):
    """A heap of broken blocks: each at its own angle and size, back to front, the far ones higher on the
    heap, corners knocked; chips and grit between them, weeds and a little ivy through it."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + 3)
    base = h - 2
    cx = w / 2.0
    TR.shade_pool(c, cx + 2, base + 1, w * 0.48, 3, 0.5)
    c.disc(cx, base, w * 0.44, 2.2, OCC)
    pieces = []
    for k in range(n):
        a = rnd.random() * math.pi
        d = rnd.random() ** 0.6
        x = cx + math.cos(a) * d * w * 0.36 * (1 if rnd.random() < 0.5 else -1)
        lift = (1.0 - abs(x - cx) / (w * 0.5)) * (h * 0.55)
        y = base - lift * (0.4 + rnd.random() * 0.6)
        bw = 7 + rnd.randrange(8)
        pieces.append((y, x, bw))
    pieces.sort()
    for (y, x, bw) in pieces:
        block3(c, x, y, bw, max(3, bw // 2 + rnd.randrange(2)), bw * (0.55 + rnd.random() * 0.35),
               (rnd.random() - 0.5) * 2.2, seed + int(x * 13 + y))
    chips(c, rnd, cx, base, w * 0.5, 8)
    weeds(c, rnd, int(cx - w * 0.3), base, 2)
    weeds(c, rnd, int(cx + w * 0.25), base - 1, 2)
    ivy_mat(c, seed + 5, cx + w * 0.22, base - h * 0.35, w * 0.12, h * 0.15, 3, 2, 3)
    return c


def ivy_mound(seed, w, h):
    """Rubble the ivy has taken: a low mound of leaf over broken stone, a corner of a block showing through,
    dark - verdant's shade and deep, a leaf of its base on the key side."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + 3)
    base = h - 2
    cx = w / 2.0
    c.disc(cx, base, w * 0.44, 2.0, OCC)
    block3(c, cx + w * 0.18, base - 2, 9, 4, 6, 0.4, seed + 1)
    block3(c, cx - w * 0.25, base - 1, 6, 3, 4, -0.6, seed + 2)
    clusters = []
    for k in range(int(w * 0.5)):
        a = math.pi + rnd.random() * math.pi
        d = rnd.random() ** 0.6
        x = cx + math.cos(a) * d * (w * 0.5 - 4)
        y = base - 2 + math.sin(a) * d * (h - 5)
        r = 2 + rnd.random() * 2.5
        if abs(x - (cx + w * 0.18)) < 4 and y > base - 7:
            continue                                        # the block's corner shows through
        clusters.append((x, y, r, r * 0.8))
    clusters.sort(key=lambda k: k[1])
    TR.crown(c, clusters, IVY, cx, base - h * 0.5, w * 0.5, h * 0.5, seed, ragged=0.5, bias=-0.08)
    ivy(c, rnd, [(int(cx - w * 0.3), base - 4)], 4, 0.9)
    return c


# (id, kind, painter, trunk footprint in WORLD px or 0, rows under the ground line) - trees.py's shape.
FOLIAGE = [
    ("spire-stump-1", "tree", lambda: spire_stump(501, 62, 122), 88, TR.FOOT),
    ("root-wall-1", "tree", lambda: root_wall(503, 92, 116), 78, TR.FOOT),
    # the trunk given here is replaced by the span measured off the wall (STANDS_FROM)
    ("wall-run-1", "tree", lambda: wall_run(507, 100, 76, 0.34, 24, 10, drop=0.3), 0, TR.FOOT),
    ("wall-run-2", "tree", lambda: wall_run(509, 84, 66, 0.62, 22, 9, drop=0.36), 0, TR.FOOT),
    ("colonnade-1", "tree", lambda: colonnade(511, 58, 96), 36, TR.FOOT),
    ("plinth-1", "tree", lambda: plinth(513, 38, 44), 56, TR.FOOT),
    ("drums-1", "bush", lambda: drums(521, 92, 20, 2), 0, 3),
    ("drums-2", "bush", lambda: drums(523, 66, 18, 2), 0, 3),
    ("capital-1", "bush", lambda: capital(527, 34, 20), 0, 3),
    ("rubble-1", "bush", lambda: rubble_heap(531, 44, 22, 7), 0, 3),
    ("rubble-2", "bush", lambda: rubble_heap(533, 32, 18, 5), 0, 3),
    ("ivy-mound-1", "bush", lambda: ivy_mound(537, 38, 18), 0, 3),
]


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    ok = True
    rows = []
    shots = []
    only = set(sys.argv[1:])
    for (name, kind, fn, footprint, foot) in FOLIAGE:
        if only and name not in only:
            continue
        art = fn()
        # a wall's blocking span is measured off the wall it drew (wall_run), not guessed
        footprint = getattr(art, "trunk_world", footprint)
        row = TR.save_grounded(art, name, kind, foot, footprint, OUT, RES)
        if row is None:
            ok = False
            continue
        rows.append(row)
        shots.append(Image.open(os.path.join(OUT, name + ".png")).convert("RGBA"))
    if not only:
        manifest = ('{\n'
                    '  "_comment": "Generated by tools/art/props_ruins.py: the Mystic Ruins\' standing ruin and growth, in '
                    'trees.json\'s shape. ground is the contact line and anchor_x the trunk column, in pixels of the PNG; '
                    'trunk is the width in world pixels of the only part that blocks (0: walk through it).",\n'
                    '  "sprites": [\n%s\n  ]\n}\n' % ",\n".join(rows))
        TR.write_text(os.path.join(OUT, "ruins.json"), manifest)
        print("%d sprites in ruins.json%s" % (len(rows), "" if ok else " - WITH PALETTE ERRORS"))
    # A preview on the ruins' floor, with the chapter's old trees and brambles beside the new, x2.
    for old in ("gnarled-1", "dead-tree-1", "bramble-1"):
        p = os.path.join(ROOT, "assets", "bonelight", "world", "trees", old + ".png")
        if os.path.exists(p):
            shots.append(Image.open(p).convert("RGBA"))
    floor = Image.open(os.path.join(ROOT, "assets", "bonelight", "tiles", "floor_ruinstone_0.png")).convert("RGBA")
    width = sum(s.size[0] for s in shots) + 16 * len(shots) + 16
    height = max(s.size[1] for s in shots) + 32
    sheet = Image.new("RGBA", (width, height), (0, 0, 0, 255))
    for y in range(0, height, floor.size[1]):
        for x in range(0, width, floor.size[0]):
            sheet.paste(floor, (x, y))
    x = 16
    for s in shots:
        sheet.alpha_composite(s, (x, height - 16 - s.size[1]))
        x += s.size[0] + 16
    sheet.resize((sheet.size[0] * 2, sheet.size[1] * 2), Image.NEAREST).save(PREVIEW)
    print("preview: tools/art/_props_ruins_preview.png")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
