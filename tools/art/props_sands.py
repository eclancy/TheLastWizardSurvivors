# -*- coding: utf-8 -*-
"""The Scorched Sands' standing foliage and stone (.ai/terrain.md section 13, "The Scorched Sands").
Run: python tools/art/props_sands.py

    world/sands/tor-1, -2.png          weathered outcrops of ochre and rust sandstone, undercut: the stands
    world/sands/ridge-1.png            a yardang, high at its windward end and tapering down the wind
    world/sands/hoodoo-1.png           a cap of stone left standing on a neck the wind ground thin
    world/sands/boulder-1, -2.png      wind-worn boulders, on an outcrop's fringe and alone in the open
    world/sands/camelthorn-1, -2.png   dead thorn trees, flat-topped, black: where a wadi ran
    world/sands/dead-palm-1.png        a palm that stood over water once, its dead fronds hanging
    world/sands/thornbush-1, -2.png    low mounds of thorn scrub on an outcrop's fringe
    world/sands/sands.json             every sprite above, in trees.json's shape
    tools/art/_props_sands_preview.png (gitignored) on the sands' floor tone, x3, for looking at

THE LADDER. The Forest's stands are woods and the Fields' are hedgerows; the Sands' are STONE. "He hid the
water" (the chapter's line): nothing grows thick here, so what masses is rock - outcrops the wind has
carved, in belts that run with the wind (Foliage.ScorchedSands.cs gives the canopy that shape), on aprons of
their own scree. Between the belts is open sand, most of the ground, to fight on. Largest first:

  tor, ridge    144-236 px   weathered masses run together - a pinnacle, an overhanging cap, a ridge -
                             undercut where the blown sand grinds hardest, sand drifted at the foot
  hoodoo        196 px       a cap on a narrowing, eroded neck: the one shape nothing else here has
  dead-palm     220 px       the rarest: a palm that grew over water and died when the water went
  camelthorn    170-190 px   dead acacias, flat-topped and black, where a wadi ran along an outcrop
  boulder       60-90 px     wind-worn, undercut, on an outcrop's fringe and now and then alone
  thornbush     44-64 px     low mounds of thorn scrub on the fringe, walked through
  (the old dead bushes and dry scrub, unchanged, scattered by habitat now)

Below them, the ground detail (detail_sands.py): sand ripples and drift tails in the open, gravel and
bunch grass on the fringe, rock chips on the scree.

THE GUARDRAIL (.ai/genre-lessons.md sections 2 and 5): clutter stays lower in contrast than the cast and
the XP orbs. The floor is gold's deep (L* 14) and the cast, in sand cloth and steel, stands ABOVE it (bodies
dL* +6 to +16, their brightest tenth +50 to +61), so the stone lives in the chapter's own sand row - base
on the lit tops and faces, shade and deep for the rest, lit only a pixel at a time on a lip - and the wood
on the dark half of `flesh`, the palm's fronds on gold's shade ("WEATHERED, NOT BUILT", below). Nothing in stone, steel or bone: the beacons round the dry tomb are
granite pillars (tools/art/tomb_beacons.py) and must not have twins on the dunes; and the Fulgurite is "the
one cold thing on a warm floor" (secret_sands.py), a pale branching tree of glass, so no tree here is pale
or cold. The camelthorns are black, which is what replaced the chapter's sun-bleached dead trees (bone's
ramp, hi +55 over this floor, and the nearest thing on it to the glass).

NONE OF IT IS A SECRET'S TELL (.ai/stage-secrets.md 6.7): no wheel or cart (the Buried Cart), no carved or
upright marker stone in a line (Milestones; a hoodoo is a natural cap, never squared, never numbered), no
reed or green thing (the Hidden Water's gradient runs cracked earth to scrub to reeds), nothing that
shimmers (the Mirage), and no glass.

Drawn with trees.py's painters (Bleed, fit, limb, trunk, shade_pool, save_grounded), imported, never edited;
the rocks are cut to the boxes the first pass committed (save_fixed), so redrawing them moved nothing.
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
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "sands")
PREVIEW = os.path.join(ROOT, "tools", "art", "_props_sands_preview.png")
RES = "res://assets/bonelight/world/sands"
SCALE = 2
TAU = math.pi * 2.0


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


FL = ramp("flesh")
GO = ramp("gold")
LN = ramp("linen")
# The red stone, brightest first, as trees.band takes it: base only where a top's lip meets the key.
STONE = [FL[2], FL[3], FL[4], OCC]
# Dead wood, in trees.py's five-slot bark shape (trunk() bands slots 1-4, limb() strokes 2-4): the lit
# side flesh's shade, the body its deep, the far side occlusion. Black wood on a dark floor, read by the
# shape of the crown and not by brightness.
DEADWOOD = [FL[3], FL[3], FL[3], FL[4], OCC]

LX, LY = TR.LX, TR.LY


def hash2(x, y, s):
    return TR.hash2(x, y, s)


# --- stone ----------------------------------------------------------------------------------------
#
# WEATHERED, NOT BUILT (2026-11-04, the integrator on the first pass: "stacked bricks ... square, uniform
# blocks piled in towers, a flat pinkish-lavender, with straight horizontal seams"; Eric had just asked for
# less blocky art). The first stone was boxes with worn corners in flesh's dark half. It is now MASSES:
# each an irregular lobe whose outline is a few slow harmonics plus grit, never two alike, flattened where
# the cap rock held and pinched in where the blown sand scoured the foot; shaded by its own rounded normal
# against the key (upper left), with a lit top where the cap is flat, a cast-shadow side, the underside of
# every overhang in the dark, wavy broken bedding, pits, joints that run down from the top and a seam
# where one mass sits against another. Sand drifts against the foot and scree lies round it; a fallen
# piece or two lies off it.
#
# THE SAND ROW, the chapter's own (cast_sands.py, art-direction.md section 3: "Sand cloth (Scorched Sands
# only)"; its lower stops are already the chapter's wood). Ochre at its base, rust at its shade, a dark
# maroon at its deep: the sun-bleached warm range the flesh row's lavender was not. Its lit stop is the
# sun on a lip, a pixel at a time; the mass lives in base, shade and deep, so the stone stays under the
# cast in brightness (the cast's sand cloth is its base and lit, with a cold rim, standing ABOVE it).
# The row is defined in cast_sands.py, not bonelight.py, so it is registered here for this generator's
# audit (trees.save_grounded checks every colour against bonelight's materials).
SAND = ["#D8CCA4", "#A88C62", "#7C6040", "#54362E", "#341E24"]
bl.MATERIALS.setdefault("sand", SAND)
# band()'s stops, brightest first: lit above v 0.875, base 0.625-0.875, shade 0.375-0.625, deep
# 0.125-0.375, occlusion under.
ROCK = [SAND[1], SAND[2], SAND[3], SAND[4], OCC]
# The key, as the rock's normals see it: upper left and toward the viewer.
KX, KY, KZ = -0.52, -0.62, 0.59


def lobe_outline(rnd, n=None):
    """A mass's outline as a polygon round its centre: 7 to 11 corners at jittered angles and radii, so it
    breaks in planes and corners like stone rather than swelling like a sack (a smooth outline, tried
    first, drew eggs), and no two are alike."""
    n = n or 7 + rnd.randrange(5)
    pts = []
    for k in range(n):
        a = (k + (rnd.random() - 0.5) * 0.6) / float(n) * TAU - math.pi
        pts.append((a, 0.86 + rnd.random() * 0.30))
    pts.sort()
    return pts


def outline_radius(pts, th):
    """The polygon's radius at angle th: straight between corners (interpolated in angle)."""
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
            # the chord between two corners, not the arc, so each side is a straight face
            return r0 * r1 * math.sin(a1 - a0) / max(1e-6, r0 * math.sin(f * (a1 - a0)) + r1 * math.sin((1 - f) * (a1 - a0)))
    return pts[0][1]


def lobe(c, cx, cy, rx, ry, seed, flat=0.55, scour=0.0, taper=0.0, lit_top=True, bed=True, seam=True, owner=None):
    """One weathered mass, in FACETS. `flat`: where the cap rock was planed flat, as a share of ry above the
    centre (1 or more: no flat top). `scour`: how hard the foot is undercut. `taper`: how much narrower the
    top is than the foot (a neck, a pinnacle). `owner` collects (x, y) -> v for joints cut afterwards.

    The mass is broken into facets (the nearest of a scatter of points), each a plane with its own normal -
    the rounded normal at its point, tilted at random - so the light falls on it in chips and planes, with a
    crease where two meet; the facets along the flat top face the sky. A smooth normal, tried first, shaded
    every mass as an egg."""
    rnd = TR.Rng(seed)
    pts = lobe_outline(rnd)
    nf = max(4, int(rx * ry / 26))
    facets = []
    for k in range(nf):
        a = rnd.random() * TAU
        d = rnd.random() ** 0.5 * 0.95
        fx, fy = math.cos(a) * d, math.sin(a) * d
        nz = math.sqrt(max(0.05, 1 - fx * fx - fy * fy))
        tx, ty = (rnd.random() - 0.5) * 0.55, (rnd.random() - 0.5) * 0.45
        facets.append((fx * rx + cx, fy * ry + cy, fx + tx, fy + ty, nz))
    beds = sorted(-0.5 + rnd.random() * 1.2 for _ in range(2 + rnd.randrange(2)))
    bed_phase = rnd.random() * TAU
    top_cut = -flat
    for y in range(int(cy - ry * 1.25) - 2, int(cy + ry * 1.25) + 3):
        dy = (y - cy) / float(ry)
        squeeze = 1.0
        if scour and dy > 0.15:
            squeeze -= scour * ((dy - 0.15) / 0.85) ** 1.2 * (0.8 + 0.4 * hash2(y // 3, 7, seed))
        if taper:
            squeeze *= 1.0 - taper * (1.0 - dy) * 0.5
        for x in range(int(cx - rx * 1.25) - 2, int(cx + rx * 1.25) + 3):
            dx = (x - cx) / float(rx) / max(0.15, squeeze)
            th = math.atan2(dy, dx)
            edge = outline_radius(pts, th) + (hash2(x // 2, y // 2, seed + 11) - 0.5) * 0.08
            rr = math.sqrt(dx * dx + dy * dy)
            cut = top_cut + 0.16 * math.sin(x * 0.11 + seed) + (hash2(x // 3, 0, seed + 13) - 0.5) * 0.16
            if rr > edge or dy < cut:
                continue
            # the facet this pixel is on, and how near it is to the next one (a crease)
            best = second = 1e9
            fbest = None
            for f in facets:
                d2 = (x - f[0]) ** 2 + ((y - f[1]) * 1.25) ** 2
                if d2 < best:
                    second, best, fbest = best, d2, f
                elif d2 < second:
                    second = d2
            nx, ny, nz = fbest[2], fbest[3], fbest[4]
            on_top = lit_top and flat < 1.0 and dy < cut + 0.24
            if on_top:
                nx, ny, nz = nx * 0.25, -0.55, 0.83                 # the planed cap faces the sky
            v = 0.35 + 0.48 * (nx * KX + ny * KY + nz * KZ)
            if on_top and dy < cut + 0.07 and dx < 0.2 and hash2(x, y, seed + 17) < 0.3:
                v = 0.92                                            # the sun on its lip, a pixel at a time
            elif lit_top and flat < 1.0 and not on_top and dy < cut + 0.34:
                v -= 0.20                                           # the shadow under the cap's lip
            if scour and dy > 0.5:
                v -= 0.30 + 0.5 * (dy - 0.5)                        # the overhang's underside
            if bed and not on_top:
                for b in beds:
                    wave = b + 0.07 * math.sin(x * 0.19 + bed_phase) + 0.05 * math.sin(x * 0.47 + seed)
                    if abs(dy - wave) * ry < 0.55 and hash2(x // 4, int(b * 50), seed) < 0.6:
                        v -= 0.15                                   # a wavering band of softer stone
            if hash2(x, y, seed + 19) < 0.04:
                v -= 0.16                                           # a pit
            v += (hash2(x, y, seed) - 0.5) * 0.05
            col = TR.band(ROCK, v, x, y, 0.22)
            # a crease where two facets meet, on the side turned from the light
            if math.sqrt(second) - math.sqrt(best) < 0.9 and v < 0.7 and hash2(x, y, seed + 23) < 0.7:
                col = SAND[4] if v > 0.3 else OCC
            # the seam where this mass sits on whatever is behind it: its lower-right rim, dark
            if seam and rr > edge * 0.88 and (dx * 0.55 + dy * 0.83) > 0.45 * edge:
                col = OCC if hash2(x, y, seed + 3) < 0.6 else SAND[4]
            c.set(x, y, col)
            if owner is not None:
                owner[(x, y)] = v


def joints(c, owner, n, seed, length):
    """Cracks: from a point on the rock, a few pixels of occlusion running mostly down and wandering, each
    with the lit stop's neighbour on its key side so it reads cut into the face, not drawn on it. Some
    fork."""
    rnd = TR.Rng(seed)
    pts = sorted(owner.keys())
    if not pts:
        return
    for k in range(n):
        x, y = pts[rnd.randrange(len(pts))]
        ln = length // 2 + rnd.randrange(length)
        for j in range(ln):
            if (x, y) not in owner:
                break
            c.set(x, y, OCC)
            if (x - 1, y) in owner and owner[(x - 1, y)] > 0.45:
                c.set(x - 1, y, SAND[3] if j % 3 else SAND[2])
            y += 1
            r = rnd.random()
            if r < 0.3:
                x += 1
            elif r < 0.5:
                x -= 1
            if j == ln // 2 and rnd.random() < 0.35:
                fx = x + (1 if rnd.random() < 0.5 else -1) * 2
                for jj in range(3):
                    if (fx, y + jj) in owner:
                        c.set(fx, y + jj, OCC)


def drift(c, cx, base, half, seed, left=1.0, right=0.6):
    """Sand drifted against the foot: a low mound in the floor's own gold, its crest lit a stop where the
    key finds it, fading out into the floor at its ends - deeper on the side the wind piles it."""
    rnd = TR.Rng(seed + 31)
    for x in range(int(cx - half * left), int(cx + half * right) + 1):
        u = (x - cx) / float(half * (left if x < cx else right))
        if abs(u) > 1.0:
            continue
        hgt = max(0.0, 1.0 - u * u) * (3.4 + 2.0 * hash2(x // 4, 1, seed)) * (1.15 if x < cx else 0.8)
        for k in range(int(hgt) + 1):
            y = base - k
            tone = GO[3] if k < hgt - 0.8 else (GO[2] if (u < 0.3 and hash2(x, y, seed) < 0.35) else GO[3])
            if k == 0 and abs(u) > 0.85:
                tone = GO[4] if hash2(x, 3, seed) < 0.5 else GO[3]
            c.set(x, y, tone)
        if hgt > 0.5 and rnd.random() < 0.08:
            c.set(x, base - int(hgt) - 1, GO[3])
    # the shadow it sits in
    for x in range(int(cx - half * left * 0.7), int(cx + half * right * 0.9)):
        if hash2(x, base + 1, seed) < 0.6 and c.get(x, base + 1)[3] == 0:
            c.set(x, base + 1, OCC)


def scree(c, cx, base, rx, seed, n=None):
    """Fallen chips round the foot: angular flakes of the rock's own stops, a lit pixel on top of the
    larger, each on its pixel of shadow - more on the shadowed side, where they rolled."""
    rnd = TR.Rng(seed + 41)
    for k in range(n if n is not None else int(rx * 0.45)):
        side = 1 if rnd.random() < 0.62 else -1
        x = int(cx + side * (0.35 + rnd.random() * 0.55) * rx)
        y = base - 1 + rnd.randrange(4)
        w = 1 + rnd.randrange(3)
        for dx in range(w):
            c.set(x + dx, y, SAND[3] if dx < w - 1 else SAND[4])
        if w > 1:
            c.set(x, y - 1, SAND[2])
        c.set(x + w, y + 1, OCC)
        c.set(x + w - 1, y + 1, OCC)


def outcrop(seed, w, h, lobes, foot, fallen=(), drift_half=None):
    """A weathered outcrop: masses back to front (x, y, rx, ry as shares of the box, then flat and scour),
    its joints, the shade it throws to the lower right, the sand against its foot, its scree, and any
    fallen pieces lying off it ((x, y, r) as shares of the box)."""
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    TR.shade_pool(c, cx + w * 0.10, base + 1, w * 0.40, 6 + w * 0.03, 0.55)
    c.disc(cx, base + 1, foot * 0.6, 2.0, OCC)
    owner = {}
    for k, (fx, fy, frx, fry, flat, scour) in enumerate(lobes):
        lobe(c, w * fx, h * fy, w * frx, h * fry, seed + 13 * k, flat=flat, scour=scour,
             taper=0.45 if flat > 0.9 else 0.12, owner=owner)
    joints(c, owner, 3 + w // 22, seed + 99, max(5, h // 9))
    for k, (fx, fy, fr) in enumerate(fallen):
        lobe(c, w * fx, h * fy, w * fr, w * fr * 0.75, seed + 200 + k, flat=0.5, bed=False, owner=None)
    drift(c, cx - w * 0.05, base, drift_half or w * 0.42, seed)
    scree(c, cx, base, w * 0.5, seed)
    return c


def ridge(seed, w, h, foot):
    """A yardang: a ridge the wind carved out of soft stone and left lying along itself - high and blunt at
    the end the wind comes from, tapering away down it to the sand, in a run of masses that fall away in
    size, each fluted along its length and undercut, the tail breaking into pieces."""
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    c.anchor_x = int(w * 0.36)
    TR.shade_pool(c, cx + w * 0.10, base + 1, w * 0.46, 7, 0.55)
    c.disc(c.anchor_x + 4, base + 1, foot * 0.7, 2.0, OCC)
    owner = {}
    # the long body, low and tapering away down the wind, then the head over its windward end; a broken
    # piece of the tail lies off it. Two masses that run into each other, not a row of them.
    lobe(c, w * 0.58, h * 0.68, w * 0.36, h * 0.26, seed + 1, flat=0.55, scour=0.32, taper=0.0, owner=owner)
    lobe(c, w * 0.80, h * 0.80, w * 0.16, h * 0.15, seed + 2, flat=0.6, scour=0.3, owner=owner)
    lobe(c, w * 0.28, h * 0.50, w * 0.22, h * 0.42, seed + 3, flat=0.62, scour=0.36, taper=0.25, owner=owner)
    joints(c, owner, 4, seed + 77, 6)
    lobe(c, w * 0.94, base - 3, 3.5, 2.6, seed + 300, flat=0.5, bed=False)   # a piece broken off the tail
    drift(c, cx - w * 0.08, base, w * 0.38, seed, left=1.0, right=0.9)
    scree(c, cx, base, w * 0.5, seed)
    return c


def hoodoo(seed, w, h):
    """A cap of harder stone left on a neck of softer rock the blown sand ground thin: the neck narrowing
    and eroded in uneven rings up to the cap, the cap overhanging it unevenly and throwing it into shadow,
    on a mound of its own rubble and drifted sand."""
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    TR.shade_pool(c, cx + w * 0.16, base + 1, w * 0.4, 6, 0.5)
    c.disc(cx, base + 1, w * 0.26, 2.0, OCC)
    owner = {}
    # the rubble mound it stands on
    lobe(c, cx - 1, base - h * 0.06, w * 0.34, h * 0.08, seed + 1, flat=0.6, scour=0.0, bed=False, owner=owner)
    # the neck: softer stone, broad on its rubble and ground narrowest under the cap - a tapering mass in
    # facets like the rest (a turned column, tried first, read as a lamp's foot), with a second, smaller
    # mass bulging from its foot on one side
    lobe(c, cx, h * 0.60, w * 0.25, h * 0.34, seed + 3, flat=1.2, scour=0.0, taper=0.55, owner=owner)
    lobe(c, cx + w * 0.10, h * 0.80, w * 0.16, h * 0.12, seed + 4, flat=0.7, scour=0.2, owner=owner)
    joints(c, owner, 3, seed + 9, 8)
    # the cap: wider than the neck, its own shape, overhanging further on one side
    lobe(c, cx + w * 0.04, h * 0.19, w * 0.40, h * 0.17, seed + 21, flat=0.7, scour=0.32)
    lobe(c, cx - w * 0.14, h * 0.13, w * 0.20, h * 0.09, seed + 23, flat=0.55, scour=0.35, bed=False)
    drift(c, cx, base, w * 0.40, seed)
    scree(c, cx, base, w * 0.45, seed)
    return c


def boulder(seed, w, h, scour=0.3):
    """A wind-worn boulder: one rounded mass, undercut, cracked, a chip lying off it, sand against it."""
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    TR.shade_pool(c, cx + w * 0.14, base + 1, w * 0.46, 4, 0.5)
    c.disc(cx, base + 1, w * 0.3, 1.6, OCC)
    owner = {}
    lobe(c, cx - w * 0.02, base - h * 0.47, w * 0.40, h * 0.47, seed, flat=0.78, scour=scour, owner=owner)
    joints(c, owner, 1 + w // 20, seed + 5, 5)
    lobe(c, cx + w * 0.40, base - 2, 2.5, 2, seed + 8, flat=0.5, bed=False)
    drift(c, cx - w * 0.06, base, w * 0.36, seed)
    scree(c, cx, base, w * 0.5, seed, n=4)
    return c


# --- dead wood ------------------------------------------------------------------------------------

def spread_limb(c, x, y, ang, length, width, depth, rnd, ceiling, tips):
    """A camelthorn's limb: it climbs at its angle until it nears the crown's flat top (`ceiling`), then
    turns out level, the way an acacia spreads; forks twice or three times toward its tips. Strokes in
    DEADWOOD's stops, lit on the upper-left side."""
    px, py = float(x), float(y)
    steps = max(2, int(length))
    side = 1 if math.cos(ang) >= 0 else -1
    for k in range(steps):
        t = k / float(steps)
        if py < ceiling + 6:
            # level out toward the flat top, never above it
            target = 0.0 if side > 0 else math.pi
            ang += (math.atan2(math.sin(target - ang), math.cos(target - ang))) * 0.25
        px += math.cos(ang)
        py += math.sin(ang)
        if py < ceiling:
            py = ceiling + (ceiling - py) * 0.3
        wd = max(1.0, width * (1.0 - t * 0.55))
        for j in range(int(-wd // 2), int(wd // 2) + 1):
            col = DEADWOOD[2] if j < 0 else (DEADWOOD[3] if j == 0 else DEADWOOD[4])
            if wd < 1.6:
                # the fine twigs mostly in the deep stop: in the lit stop, a crown of them read mauve
                col = DEADWOOD[3] if hash2(int(px), int(py), 5) < 0.72 else DEADWOOD[2]
            c.set(int(TR.round2(px - math.sin(ang) * j)), int(TR.round2(py + math.cos(ang) * j)), col)
    tips.append((px, py))
    if depth <= 0:
        return
    for s in (-1, 1):
        if rnd.random() < 0.9:
            a = ang + s * (0.35 + rnd.random() * 0.5)
            spread_limb(c, px, py, a, length * (0.55 + rnd.random() * 0.2), width * 0.6, depth - 1, rnd, ceiling, tips)


def camelthorn(seed, w, h):
    """A dead camelthorn: a short, leaning, split trunk and a flat umbrella of bare limbs, black against
    the sand - an acacia that outlived the water by a year and then stood. Thorns along the twigs (a
    pixel of occlusion off the stroke here and there) and the dark pool of a crown's shade below."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2 + rnd.randrange(-3, 4)
    c.anchor_x = cx
    TR.shade_pool(c, cx + w * 0.08, base + 1, w * 0.34, 5 + w * 0.02, 0.35)
    TR.contact(c, cx, base + 1, 5)
    lean = (rnd.random() - 0.5) * 6
    fork = base - h * 0.34
    TR.trunk(c, cx, base, fork, 8, 6, DEADWOOD, seed, flare=3, lean=lean)
    tx = cx + lean * 0.1 * (base - fork) * 0.1
    ceiling = 2 + h * 0.06
    tips = []
    n = 4 + rnd.randrange(2)
    for k in range(n):
        side = -1 if k % 2 == 0 else 1
        a = -math.pi / 2 + side * (0.35 + 0.55 * (k // 2) / max(1.0, (n - 1) / 2.0)) + (rnd.random() - 0.5) * 0.2
        spread_limb(c, tx + side, fork + 2 + rnd.randrange(4), a, h * (0.30 + rnd.random() * 0.12), 4, 2, rnd, ceiling, tips)
    # the twig mat along the top: short level strokes off the tips, so the crown reads flat
    for (px, py) in tips:
        for k in range(rnd.randrange(2, 5)):
            ln = 3 + rnd.randrange(6)
            dx = 1 if rnd.random() < 0.5 else -1
            yy = int(py) - rnd.randrange(3)
            for j in range(ln):
                xx = int(px) + dx * j
                c.set(xx, yy - (1 if j > ln * 0.6 and rnd.random() < 0.4 else 0), DEADWOOD[3] if j % 4 else DEADWOOD[2])
            if rnd.random() < 0.5:
                c.set(int(px) + dx * (ln // 2), yy - 1, OCC)             # a thorn
    return c


def dead_frond(c, x, y, ang, length, droop, rnd, lit):
    """A dead palm frond: a midrib arching out of the crown and hanging under its own weight, its leaflets
    hanging off it in gold's shade, lit only on the fronds that face the key."""
    px, py = float(x), float(y)
    a = ang
    for k in range(int(length)):
        a += droop
        px += math.cos(a)
        py += math.sin(a)
        ix, iy = int(TR.round2(px)), int(TR.round2(py))
        c.set(ix, iy, GO[3] if lit else GO[4])
        if k > 2 and k % 2 == 0:
            ln = 3 + int(5 * math.sin(math.pi * k / length)) + rnd.randrange(2)
            for side in (-1, 1):
                # leaflets hang: they leave the rib and fall
                lx, ly = float(ix), float(iy)
                for j in range(ln):
                    lx += side * 0.6
                    ly += 0.9
                    tone = GO[3] if (lit and side < 0) else GO[4]
                    if j == ln - 1 and rnd.random() < 0.5:
                        tone = OCC
                    c.set(int(TR.round2(lx)), int(TR.round2(ly)), tone)
    return px, py


def dead_palm(seed, w, h):
    """A palm that stood over water once: a tall ringed trunk, curving, and a crown of dead fronds - a few
    still arching out, most hanging down against the trunk in the skirt a dead palm wears."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    c.anchor_x = cx
    TR.shade_pool(c, cx + w * 0.12, base + 1, w * 0.3, 5, 0.35)
    TR.contact(c, cx, base + 1, 4)
    crown_y = h * 0.20
    bend = 7.0 + rnd.random() * 3
    top_x = cx
    rings = set()
    y = base - 2
    while y > crown_y:
        rings.add(y)
        y -= 3 + rnd.randrange(3)                            # leaf scars, unevenly spaced
    for y in range(int(crown_y), base + 1):
        t = (base - y) / float(base - crown_y)               # 0 at the foot, 1 at the crown
        mid = cx + bend * t * t
        half = 4.0 - 1.4 * t + (1.0 if t < 0.06 else 0.0)
        x0, x1 = int(TR.round2(mid - half)), int(TR.round2(mid + half))
        ring = y in rings
        for x in range(x0, x1 + 1):
            u = (x - x0) / float(max(1, x1 - x0))
            col = FL[3] if u < 0.4 else FL[4]
            if ring:
                col = FL[4] if u < 0.4 else OCC
            if x == x1:
                col = OCC
            c.set(x, y, col)
        top_x = mid
    tx, ty = int(top_x), int(crown_y)
    # the skirt: dead fronds hanging straight down round the top of the trunk
    for k in range(15):
        sx = tx - 8 + k * 1.15 + (rnd.random() - 0.5)
        ln = h * (0.14 + rnd.random() * 0.12) * (1.0 - abs(k - 7) / 14.0)
        flare = (k - 7) * 0.05
        for j in range(int(ln)):
            x = int(TR.round2(sx + flare * j + math.sin(j * 0.3 + k) * 0.6))
            y = ty + 2 + j
            col = GO[3] if (k < 6 and j % 3) else GO[4]
            if k > 10 and j % 2:
                col = OCC
            if j > ln - 3:
                col = OCC if j % 2 else GO[4]
            c.set(x, y, col)
    # the fronds still out: arching up and over, then hanging, the key-side ones a stop lighter
    for k in range(9):
        side = -1 if k % 2 == 0 else 1
        a = -math.pi / 2 + side * (0.45 + 0.30 * (k // 2)) + (rnd.random() - 0.5) * 0.2
        lit = side < 0 or k < 2
        dead_frond(c, tx, ty, a, w * (0.50 + rnd.random() * 0.14), 0.042 * side, rnd, lit)
    # the crown's knot
    c.disc(tx, ty, 3.5, 2.6, FL[4])
    c.set(tx - 2, ty - 1, FL[3])
    c.set(tx - 1, ty - 2, GO[3])
    c.set(tx + 2, ty + 1, OCC)
    return c


# --- scrub ----------------------------------------------------------------------------------------

def thornbush(seed, w, h, n):
    """A low mound of thorn scrub: short twigs crossed every way inside a dome, lit on its upper-left,
    a few grey leaves (linen's shade) where any are left, and its contact shadow. Knee-high, walked
    through, and nothing like a ball: its top is ragged."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + 3)
    base = h - 1
    cx = w // 2
    c.disc(cx, base, w * 0.44, 2.0, OCC)
    for k in range(n):
        a = math.pi + rnd.random() * math.pi
        d = rnd.random() ** 0.55
        x = cx + math.cos(a) * d * (w * 0.46)
        y = base - 1 + math.sin(a) * d * (h - 3)
        ang = rnd.random() * math.pi
        ln = 3 + rnd.randrange(4)
        lit = (x - cx) * LX + (y - (base - h * 0.5)) * LY > 0
        for j in range(ln):
            xx = int(TR.round2(x + math.cos(ang) * j))
            yy = int(TR.round2(y - abs(math.sin(ang)) * j * 0.6))
            if yy > base:
                continue
            col = DEADWOOD[2] if (lit and j < ln - 1) else DEADWOOD[3]
            if yy > base - 2:
                col = OCC
            c.set(xx, yy, col)
        if rnd.random() < 0.18 and y < base - 3:
            c.set(int(x), int(y) - 1, LN[3])
    # the underside, in shadow
    for x in range(int(cx - w * 0.38), int(cx + w * 0.38)):
        if c.get(x, base - 1)[3] and hash2(x, base, seed) < 0.6:
            c.set(x, base - 1, OCC)
    return c


# (id, kind, painter, trunk footprint in WORLD px or 0, rows under the ground line) - trees.py's shape.
FOLIAGE = [
    # masses (x, y, rx, ry, flat, scour) as shares of the box, back to front; fallen pieces (x, y, r)
    ("tor-1", "tree", lambda: outcrop(401, 92, 100, [(0.62, 0.46, 0.30, 0.34, 0.60, 0.10),
                                                     (0.34, 0.38, 0.20, 0.34, 0.95, 0.15),
                                                     (0.50, 0.76, 0.42, 0.22, 0.50, 0.40),
                                                     (0.80, 0.80, 0.15, 0.14, 0.55, 0.30)], 56,
                                      fallen=[(0.10, 0.95, 0.06)]), 60, TR.FOOT),
    ("tor-2", "tree", lambda: outcrop(409, 76, 108, [(0.46, 0.62, 0.36, 0.28, 0.60, 0.10),
                                                     (0.56, 0.34, 0.24, 0.30, 1.20, 0.12),
                                                     (0.62, 0.14, 0.20, 0.10, 0.45, 0.50),
                                                     (0.48, 0.83, 0.40, 0.15, 0.50, 0.40)], 50,
                                      fallen=[(0.88, 0.96, 0.07)]), 50, TR.FOOT),
    ("ridge-1", "tree", lambda: ridge(413, 104, 64, 64), 64, TR.FOOT),
    ("hoodoo-1", "tree", lambda: hoodoo(415, 52, 92), 28, TR.FOOT),
    ("boulder-1", "tree", lambda: boulder(421, 42, 30), 34, TR.FOOT),
    ("boulder-2", "tree", lambda: boulder(427, 34, 24, scour=0.22), 26, TR.FOOT),
    ("camelthorn-1", "tree", lambda: camelthorn(431, 96, 90), 14, TR.FOOT),
    ("camelthorn-2", "tree", lambda: camelthorn(437, 84, 80), 12, TR.FOOT),
    ("dead-palm-1", "tree", lambda: dead_palm(443, 64, 112), 12, TR.FOOT),
    ("thornbush-1", "bush", lambda: thornbush(451, 32, 18, 70), 0, 3),
    ("thornbush-2", "bush", lambda: thornbush(457, 24, 14, 44), 0, 3),
]


# THE STONE KEEPS ITS FOOTPRINT. Redrawn on 2026-11-04, the rocks keep the sprite boxes, ground lines, anchors
# and trunk widths the first pass committed, so nothing the placement or the footing rule reads moves: each
# is cut to exactly that box (the committed manifest's numbers, below) instead of trees.fit's, and refused if
# its art reaches the box's clear border.
FIXED = {
    # id: (width, height, ground, anchor_x) in pixels of the PNG
    "tor-1": (188, 220, 200, 93),
    "tor-2": (156, 236, 216, 77),
    "ridge-1": (228, 144, 128, 83),
    "hoodoo-1": (108, 196, 184, 53),
    "boulder-1": (90, 74, 62, 45),
    "boulder-2": (78, 64, 52, 37),
}


def save_fixed(c, sid, kind, foot, footprint):
    """trees.save_grounded, cut to the box in FIXED rather than fitted. Returns the manifest row (the same
    numbers as before), or None, having said why: off-contract colours, or art reaching the box's edge."""
    W, H, ground, anchor = FIXED[sid]
    anchor_box = getattr(c, "anchor_x", c.w // 2)
    ox = (anchor - 1) // TR.SCALE - anchor_box
    oy = ground // TR.SCALE - (c.h - foot)
    x0, y0 = -ox, -oy
    x1, y1 = x0 + W // TR.SCALE, y0 + H // TR.SCALE
    ab = c.art_box()
    if ab[0] < x0 + TR.MARGIN or ab[1] < y0 + TR.MARGIN or ab[2] > x1 - TR.MARGIN or ab[3] > y1 - TR.MARGIN:
        print("ART PAST ITS BOX in %s: art %r, box %r" % (sid, ab, (x0, y0, x1, y1)))
        return None
    out = raster.Canvas(x1 - x0, y1 - y0, None)
    out.img = c.img.crop((x0 + TR.BLEED, y0 + TR.BLEED, x1 + TR.BLEED, y1 + TR.BLEED))
    out.px = out.img.load()
    bad = out.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in %s: %s" % (sid, bad[:4]))
        return None
    out.scaled(TR.SCALE).save(os.path.join(OUT, sid + ".png"))
    print("  %-18s %-4s %3dx%-3d on screen, blocks %d px (the committed box)" % (sid, kind, W, H, footprint))
    return ('    {"id": "%s", "kind": "%s", "path": "%s/%s.png", '
            '"width": %d, "height": %d, "ground": %d, "anchor_x": %d, "trunk": %d}'
            % (sid, kind, RES, sid, W, H, ground, anchor, footprint))


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    ok = True
    rows = []
    shots = []
    for (name, kind, fn, footprint, foot) in FOLIAGE:
        row = (save_fixed(fn(), name, kind, foot, footprint) if name in FIXED
               else TR.save_grounded(fn(), name, kind, foot, footprint, OUT, RES))
        if row is None:
            ok = False
            continue
        rows.append(row)
        shots.append(Image.open(os.path.join(OUT, name + ".png")).convert("RGBA"))
    manifest = ('{\n'
                '  "_comment": "Generated by tools/art/props_sands.py: the Scorched Sands\' foliage and stone, in trees.json\'s '
                'shape. ground is the contact line and anchor_x the trunk column, in pixels of the PNG; trunk is the width in '
                'world pixels of the only part that blocks (0: walk through it).",\n'
                '  "sprites": [\n%s\n  ]\n}\n' % ",\n".join(rows))
    TR.write_text(os.path.join(OUT, "sands.json"), manifest)
    print("%d sprites in sands.json%s" % (len(rows), "" if ok else " - WITH PALETTE ERRORS"))
    # A preview on the sands' floor tone, with the chapter's old dead tree and bush beside the new, x3.
    for old in ("bleached-tree-1", "dead-bush-1", "dry-scrub-1"):
        p = os.path.join(ROOT, "assets", "bonelight", "world", "trees", old + ".png")
        if os.path.exists(p):
            shots.append(Image.open(p).convert("RGBA"))
    width = sum(s.size[0] for s in shots) + 16 * len(shots) + 16
    height = max(s.size[1] for s in shots) + 32
    sheet = Image.new("RGBA", (width, height), raster.rgb(GO[4]))
    x = 16
    for s in shots:
        sheet.alpha_composite(s, (x, height - 16 - s.size[1]))
        x += s.size[0] + 16
    sheet.resize((sheet.size[0] * 2, sheet.size[1] * 2), Image.NEAREST).save(PREVIEW)
    print("preview: tools/art/_props_sands_preview.png")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
