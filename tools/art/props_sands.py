# -*- coding: utf-8 -*-
"""The Scorched Sands' standing foliage and stone (.ai/terrain.md section 13, "The Scorched Sands").
Run: python tools/art/props_sands.py

    world/sands/tor-1, -2.png          wind-carved outcrops of red stone, undercut at the foot: the stands
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

  tor           180-210 px   red stone in stacked blocks, undercut where the blown sand grinds hardest
  hoodoo        180 px       a cap on a thin neck: the one shape in the chapter nothing else has
  dead-palm     220 px       the rarest: a palm that grew over water and died when the water went
  camelthorn    170-190 px   dead acacias, flat-topped and black, where a wadi ran along an outcrop
  boulder       60-90 px     wind-worn, undercut, on an outcrop's fringe and now and then alone
  thornbush     44-64 px     low mounds of thorn scrub on the fringe, walked through
  (the old dead bushes and dry scrub, unchanged, scattered by habitat now)

Below them, the ground detail (detail_sands.py): sand ripples and drift tails in the open, gravel and
bunch grass on the fringe, rock chips on the scree.

THE GUARDRAIL (.ai/genre-lessons.md sections 2 and 5): clutter stays lower in contrast than the cast and
the XP orbs. The floor is gold's deep (L* 14) and the cast, in sand cloth and steel, stands ABOVE it (bodies
dL* +6 to +16, their brightest tenth +50 to +61), so the stone and the wood here live on the DARK half of
`flesh` - deep for the mass, shade for the faces the key light finds, base only on the lit lip of a top -
and the palm's fronds on gold's shade. Nothing in stone, steel or bone: the beacons round the dry tomb are
granite pillars (tools/art/tomb_beacons.py) and must not have twins on the dunes; and the Fulgurite is "the
one cold thing on a warm floor" (secret_sands.py), a pale branching tree of glass, so no tree here is pale
or cold. The camelthorns are black, which is what replaced the chapter's sun-bleached dead trees (bone's
ramp, hi +55 over this floor, and the nearest thing on it to the glass).

NONE OF IT IS A SECRET'S TELL (.ai/stage-secrets.md 6.7): no wheel or cart (the Buried Cart), no carved or
upright marker stone in a line (Milestones; a hoodoo is a natural cap, never squared, never numbered), no
reed or green thing (the Hidden Water's gradient runs cracked earth to scrub to reeds), nothing that
shimmers (the Mirage), and no glass.

Drawn with trees.py's painters (Bleed, fit, limb, trunk, shade_pool, save_grounded), imported, never edited.
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

def edge_noise(i, seed, amp, period=3):
    """A ragged edge: a value that wanders by up to `amp` pixels along an edge, changing every `period`
    pixels and blended between, so a weathered outline steps rather than buzzes."""
    k, f = divmod(i, period)
    a = hash2(k, 0, seed) * 2 - 1
    b = hash2(k + 1, 0, seed) * 2 - 1
    t = f / float(period)
    return (a + (b - a) * t) * amp


def block(c, bx, by, rx, ry, seed, top=0.34, waist=0.0, strata=1, seam=True, corner=0.3):
    """One block of stone in three-quarter view: a box with worn corners whose upper `top` of its height
    is the flat top the key light falls on and whose rest is its face.

    FACETS, NOT A LUMP. The top is one plane, lit, its front lip a stop brighter along the key side; the
    face below it is two planes, the left in the light and the right turned away, split by a dithered
    seam; the far edge and the shadow under the lip are deep. Vertical joints (cracks) and one or two
    lines of bedding cut the face. A first pass shaded each block as a rounded lump with a lip all round
    and grooves every five rows: stacked loaves. `waist` pinches the face in toward the foot - the
    undercut the blown sand grinds into every desert rock - and the undercut is out of the light."""
    x_lo, x_hi = int(bx - rx), int(bx + rx)
    y_top, y_bot = by - ry, by + ry
    depth = 2.0 * ry * top
    cr = max(2.0, corner * min(rx, ry))
    # where the lit face turns into the dark one, a little right of the middle
    turn = bx + rx * (0.05 + 0.2 * (hash2(1, 2, seed) - 0.5))
    for x in range(x_lo - 2, x_hi + 3):
        i = x - x_lo
        top_y = y_top + edge_noise(i, seed + 1, 1.6)
        for y in range(int(top_y) - 1, int(y_bot) + 2):
            ny = (y - by) / float(ry)
            pinch = 0.0
            if waist and ny > 0.25:
                pinch = waist * rx * ((ny - 0.25) / 0.75) ** 1.3
            left = x_lo + pinch + edge_noise(y - int(y_top), seed + 2, 1.2, 4)
            right = x_hi - pinch - edge_noise(y - int(y_top), seed + 3, 1.2, 4)
            if x < left or x > right or y < top_y or y > y_bot:
                continue
            # worn corners: a quarter circle off each corner of the outline
            dxc = max(left + cr - x, x - (right - cr), 0)
            dyc = max(top_y + cr - y, 0) if y < top_y + cr else max(y - (y_bot - cr * 0.6), 0)
            if dxc and dyc and dxc * dxc + dyc * dyc > cr * cr:
                continue
            # STONE's stops by band(): base above v 0.83, shade 0.5-0.83, deep 0.17-0.5, occlusion under.
            if y < top_y + depth:
                # the top plane: shade, warming to base toward the key-side corner
                v = 0.76 - (x - bx) / rx * 0.10 - (y - top_y) / max(1.0, depth) * 0.04
                if y >= top_y + depth - 1.2 and x < turn and hash2(x // 2, y, seed + 5) < 0.8:
                    v = 0.92                                   # the lit lip, on the key side only
                elif y < top_y + 1.2 and x > turn:
                    v = 0.55                                   # the far rim of the top, turned away
            else:
                # the face: the lit plane left of the turn in shade, fading to deep toward the foot; the
                # plane turned away in deep; dithered only where one becomes the other
                u = (x - turn) / float(max(1.0, rx))
                v = 0.64 - max(0.0, ny) * 0.22 if u < -0.08 else (0.30 if u > 0.08 else 0.48)
                if y < top_y + depth + 1.6:
                    v -= 0.22                                  # the shadow under the lip
                if strata and int(y - by + 2.0 * math.sin(x * 0.17 + seed)) % max(4, int(ry * 0.9)) == 0 \
                        and u < 0.0 and hash2(x // 4, y, seed) < 0.7:
                    v -= 0.20                                  # a line of bedding, where the light shows it
                if waist and ny > 0.5:
                    v -= 0.22 + 0.4 * (ny - 0.5)               # the undercut
            v += (hash2(x, y, seed) - 0.5) * 0.05
            col = TR.band(STONE, v, x, y, 0.2)
            if seam and (x >= right - 0.5 or y >= y_bot - 0.5):
                col = OCC if hash2(x, y, seed + 3) < 0.7 else FL[4]
            c.set(x, y, col)


def cracks(c, x0, y0, n, seed, length=6):
    """Joints running down a face: short occlusion lines, a pixel of the face's lit stop on their key side."""
    rnd = TR.Rng(seed)
    for k in range(n):
        x = x0[0] + rnd.randrange(max(1, x0[1] - x0[0]))
        y = y0[0] + rnd.randrange(max(1, y0[1] - y0[0]))
        ln = length // 2 + rnd.randrange(length)
        for j in range(ln):
            if c.get(x, y)[3] == 0:
                break
            c.set(x, y, OCC)
            if c.get(x - 1, y)[3]:
                c.set(x - 1, y, FL[3] if j % 3 else FL[4])
            y += 1
            if rnd.random() < 0.35:
                x += 1 if rnd.random() < 0.6 else -1


def tor(seed, w, h, blocks, foot):
    """An outcrop: blocks stacked back to front, the lowest undercut, on its scree and its shadow.
    `blocks` are (x, y, rx, ry, waist) as fractions of the box; `foot` the width of the undercut foot."""
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    TR.shade_pool(c, cx + w * 0.12, base + 1, w * 0.42, 6 + w * 0.03, 0.55)
    c.disc(cx, base + 1, foot * 0.62, 2.0, OCC)
    for k, (fx, fy, frx, fry, waist) in enumerate(blocks):
        block(c, w * fx, h * fy, w * frx, h * fry, seed + k * 13, waist=waist, top=0.20 if waist else 0.26)
    cracks(c, (int(w * 0.2), int(w * 0.8)), (int(h * 0.3), int(h * 0.8)), 4 + w // 30, seed + 99)
    scree(c, cx, base, w * 0.48, seed)
    return c


def ridge(seed, w, h, foot):
    """A yardang: a ridge the wind carved out of soft stone and left lying along itself - high and blunt at
    the end the wind comes from, tapering away down it to the sand. Its top is a long lit plane fluted
    along its length (the flutes run WITH the wind, as everything here does), its face undercut."""
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    # the trunk's column under the high end's shoulder, where the ridge's weight is
    c.anchor_x = int(w * 0.36)
    TR.shade_pool(c, cx + w * 0.10, base + 1, w * 0.46, 7, 0.55)
    c.disc(c.anchor_x + 4, base + 1, foot * 0.7, 2.0, OCC)
    head = 0.26                                     # where the blunt end stands, as a share of the width
    x0, x1 = 2, w - 3
    for x in range(x0, x1 + 1):
        t = (x - x0) / float(x1 - x0)
        if t < head:
            prof = math.sqrt(max(0.0, 1.0 - ((head - t) / head) ** 2.2))
        else:
            prof = (1.0 - (t - head) / (1.0 - head)) ** 0.75
        hgt = (h - 6) * prof + edge_noise(x, seed, 1.4)
        if hgt < 3:
            continue
        top = base - hgt
        depth = 4 + hgt * 0.22
        for y in range(int(top), base + 1):
            ny = (y - top) / float(max(1.0, hgt))          # 0 at the crest, 1 at the foot
            under = 0.0
            if ny > 0.78:
                under = (ny - 0.78) / 0.22                 # the undercut along the foot
                if hash2(x, y, seed + 9) < under * 0.55 and (x < x0 + 4 or x > x1 - 6):
                    continue
            if y < top + depth:
                v = 0.62 - t * 0.12
                if y >= top + depth - 1.5 and t < head * 1.4 and hash2(x // 2, y, seed + 5) < 0.75:
                    v = 0.80                               # the lit lip, along the high end only
            else:
                # the face: lit toward the blunt end, which turns its shoulder to the key
                v = 0.44 - t * 0.22 - ny * 0.10
                if y < top + depth + 2:
                    v -= 0.16
                if t < head * 0.5:
                    v += 0.06
                # the bedding, level along the ridge whatever its crest does: the flutes the wind cut
                if (y + int(1.2 * math.sin(x * 0.09 + seed))) % 7 == 0 and hash2(x // 5, y, seed) < 0.7:
                    v -= 0.18
                v -= under * 0.45
            v += (hash2(x, y, seed) - 0.5) * 0.05
            col = TR.band(STONE, v, x, y, 0.2)
            if y >= base - 0.5 or (x == x1 and y > top):
                col = OCC
            c.set(x, y, col)
    cracks(c, (int(w * 0.08), int(w * 0.55)), (int(h * 0.35), int(h * 0.75)), 3 + w // 40, seed + 77, 5)
    scree(c, cx, base, w * 0.5, seed)
    return c


def scree(c, cx, base, rx, seed):
    """Fallen chips at the foot: a few small stones in the rock's own stops, on the ground line."""
    rnd = TR.Rng(seed + 41)
    for k in range(int(rx * 0.5)):
        x = int(cx + (rnd.random() * 2 - 1) * rx)
        y = base - 1 + rnd.randrange(4)
        if c.get(x, y)[3] and c.get(x, y) != raster.rgb(OCC) + (255,):
            continue
        c.set(x, y, FL[3] if rnd.random() < 0.4 else FL[4])
        c.set(x + 1, y, FL[4])
        c.set(x + 1, y + 1, OCC)


def hoodoo(seed, w, h):
    """A cap of harder stone left on a neck the blown sand ground thin, on a mound of its own rubble: lit
    on the left like everything, the cap's underside throwing the neck below it into shadow."""
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    TR.shade_pool(c, cx + w * 0.16, base + 1, w * 0.4, 6, 0.5)
    c.disc(cx, base + 1, w * 0.24, 2.0, OCC)
    # the neck: softer stone, broad on its rubble and ground thinnest under the cap, its two sides ragged
    # apart (a wavy neck read as a spring, a smooth one as a lamp's foot), in faces like the blocks: the lit
    # side in shade, the far side deep, a line of bedding every few rows on the lit one
    neck_top = h * 0.28
    neck_bot = base - 1
    for y in range(int(neck_top), int(neck_bot) + 1):
        t = (y - neck_top) / float(neck_bot - neck_top)       # 0 under the cap, 1 at the ground
        half = w * (0.10 + 0.04 * t + 0.20 * t ** 3)
        x0 = int(TR.round2(cx - half - edge_noise(y, seed + 11, 1.8, 5)))
        x1 = int(TR.round2(cx + half + edge_noise(y, seed + 12, 1.8, 5)))
        turn = x0 + (x1 - x0) * 0.48
        for x in range(x0, x1 + 1):
            v = 0.62 - t * 0.14 if x < turn - 1 else (0.30 if x > turn + 1 else 0.46)
            if t < 0.10:
                v -= 0.32                                       # under the cap
            if (y + int(1.2 * math.sin(x * 0.4 + seed))) % 7 == 0 and x < turn and hash2(x // 3, y, seed) < 0.7:
                v -= 0.2
            col = TR.band(STONE, v, x, y, 0.2)
            if x == x1:
                col = OCC
            c.set(x, y, col)
    cracks(c, (int(cx - w * 0.12), int(cx + w * 0.05)), (int(h * 0.45), int(h * 0.8)), 2, seed + 3, 7)
    # the cap: wider than the neck, a top in the light and a dark underside
    block(c, cx + w * 0.04, h * 0.17, w * 0.44, h * 0.14, seed + 5, top=0.42, corner=0.4)
    scree(c, cx, base, w * 0.4, seed)
    return c


def boulder(seed, w, h, waist=0.18):
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    TR.shade_pool(c, cx + w * 0.14, base + 1, w * 0.46, 4, 0.5)
    c.disc(cx, base + 1, w * 0.3, 1.6, OCC)
    # a little off the middle and leaning, so two never look turned on a lathe (a deep waist read as a pot)
    block(c, cx - w * 0.03, base - h * 0.48, w * 0.46, h * 0.48, seed, top=0.38, waist=waist, corner=0.62)
    cracks(c, (int(w * 0.25), int(w * 0.7)), (int(h * 0.35), int(h * 0.7)), 1 + w // 30, seed + 5, 4)
    scree(c, cx, base, w * 0.5, seed)
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
    # blocks (x, y, rx, ry, waist) as shares of the box, back to front
    ("tor-1", "tree", lambda: tor(401, 92, 100, [(0.36, 0.30, 0.22, 0.20, 0.0), (0.64, 0.44, 0.26, 0.24, 0.0),
                                                 (0.48, 0.76, 0.42, 0.22, 0.36)], 56), 60, TR.FOOT),
    ("tor-2", "tree", lambda: tor(409, 76, 108, [(0.56, 0.22, 0.24, 0.17, 0.0), (0.42, 0.48, 0.32, 0.20, 0.0),
                                                 (0.54, 0.78, 0.40, 0.20, 0.34)], 50), 50, TR.FOOT),
    ("ridge-1", "tree", lambda: ridge(413, 104, 64, 64), 64, TR.FOOT),
    ("hoodoo-1", "tree", lambda: hoodoo(415, 52, 92), 28, TR.FOOT),
    ("boulder-1", "tree", lambda: boulder(421, 42, 30), 34, TR.FOOT),
    ("boulder-2", "tree", lambda: boulder(427, 34, 24, waist=0.14), 26, TR.FOOT),
    ("camelthorn-1", "tree", lambda: camelthorn(431, 96, 90), 14, TR.FOOT),
    ("camelthorn-2", "tree", lambda: camelthorn(437, 84, 80), 12, TR.FOOT),
    ("dead-palm-1", "tree", lambda: dead_palm(443, 64, 112), 12, TR.FOOT),
    ("thornbush-1", "bush", lambda: thornbush(451, 32, 18, 70), 0, 3),
    ("thornbush-2", "bush", lambda: thornbush(457, 24, 14, 44), 0, 3),
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
