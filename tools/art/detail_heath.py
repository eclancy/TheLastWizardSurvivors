# -*- coding: utf-8 -*-
"""The Broken Circle's ground detail (.ai/campaign.md section 6). Drawn by tools/art/ground_detail.py,
which finds this module by its name and appends DETAIL_SETS to its own - run ground_detail.py, never
this. GroundDetail.cs's Heath block is what asks for these sets, and where.

Three things lie on a high heath, and they are what make the violet floor read as heather rather
than as stone:

  heather_tufts   the heather itself: low wiry bushes, dark green under a crown of violet bloom,
                  three or four to a clump, laid in drifts by GroundDetail's clustering.
  cotton_grass    the bog's flag: a tuft of thin blades with white cotton heads nodding on stalks.
                  It grows where the ground is wet - on the peat and at the tarns' edges.
  lichen_stones   loose granite, speckled, with a crust of lichen on the lit side.

The foliage pass (2026-11-04, .ai/terrain.md section 13) added five more, laid as clutter by
scripts/Foliage.BrokenCircle.cs rather than by GroundDetail's table:

  moor_grass      grass combed east by the wind - the one place the wind is drawn, since the detail
                  layer is never mirrored.
  bilberry        low leaves and dusky berries under the gorse at a drift's edge.
  lichen_specks   flat lichen rosettes in the open and on the granite.
  clitter         broken granite of every size, rounded by the weather, spilt down a drift from its tor.
  rock_hump       bedrock breaking the heather, from a knuckle to most of a cell, thicker toward the tor:
                  the crown's rock, since a floor of slabs under it read as paving.

The ground_detail.py rules hold: material tones only, nothing emissive, the brightest stop a `lit`
and never a `hi` plane, and nothing round, bright and alone - the XP orbs are lime, lilac and pale
yellow discs, so the heather's bloom is violet's dull material row in spikes, never a lilac dot, and
the cotton heads come several to a stalk-tuft, never one.
"""
import math

import ground_detail
from ground_detail import GO, LI, LN, OCC, SK, ST, STL, VE, VI, spot


def heather_bush(c, x, y, w, r):
    """One heather bush: a low rounded mound of wiry sprigs, mostly bloom on top. Sprigs, not a
    dome - every column is its own height, which is what heather looks like from a few yards and
    what keeps it from reading as moss. The bloom is most of what shows: the first pass, a green
    mound with a violet rim, read as grass, so the green is now only the dark of the stems under the
    flowers, and the flowers are violet's lit stop, with a fleck of its hi where the key catches the
    top and its base and shade on the far side: on a floor of violet's shade, bloom in violet's base
    was the floor again (photographed in the game, 2026-10-25). Its hi is a fleck, never a plane, and
    nowhere near the lilac of an XP orb."""
    hmax = 4.0 + w * 0.45
    for dx in range(-w, w + 1):
        t = dx / float(w + 0.5)
        h = int(round(hmax * math.sqrt(max(0.0, 1.0 - t * t)) + r.i(-1, 1)))
        if h < 1:
            continue
        top = y - h
        bloom = max(1, min(h - 1, 2 + r.i(0, 1)))
        for j in range(h):
            yy = y - j
            if yy <= top + bloom:
                if t < -0.3:
                    tone = VI[0] if (yy == top + 1 and (dx + yy) % 3 == 0) else VI[1]
                elif t > 0.45:
                    tone = VI[2] if (dx + yy) % 2 else VI[3]
                else:
                    tone = VI[1] if (dx * 3 + yy) % 4 else VI[2]
            else:
                tone = VE[4] if (dx + j) % 2 else VE[3]
            c.set(x + dx, yy, tone)
        c.set(x + dx, y + 1, OCC if abs(dx) < w else VI[4])
    # A few sprigs standing proud of the mound, bloom at their tips.
    for k in range(r.i(1, 3)):
        sx = x + r.i(-w + 2, w - 2)
        top = y - int(hmax) - 1 - r.i(1, 2)
        c.set(sx, top + 1, VE[4])
        c.set(sx, top, VI[1] if sx < x else VI[2])


def heather_tufts(c, r):
    cx, cy = spot(r, 12)
    bushes = [(cx + r.i(-7, 7), cy + r.i(-4, 6), r.i(7, 11)) for _ in range(r.i(1, 2))]
    for x, y, w in sorted(bushes, key=lambda b: b[1]):
        heather_bush(c, x, y, w, r)


def cotton_grass(c, r):
    """Cotton-grass: thin blades from one root and four to six white heads on taller stalks, each
    head a 2x2 tuft lit on its upper left, nodding a little to one side."""
    cx, cy = spot(r, 12)
    for k in range(r.i(1, 2)):
        x0, y0 = cx + r.i(-6, 6), cy + r.i(-3, 5)
        # the blades
        for side in (-1, 0, 1):
            for j in range(6):
                c.set(x0 + side * (j // 3), y0 - j, LN[2] if j < 4 else LN[1])
        c.set(x0 - 1, y0 + 1, OCC)
        c.set(x0, y0 + 1, OCC)
        c.set(x0 + 1, y0 + 1, OCC)
        # the stalks and their heads
        for s in range(r.i(4, 6)):
            sx = x0 + r.i(-4, 4)
            h = r.i(8, 13)
            lean = 1 if sx > x0 else (-1 if sx < x0 else 0)
            for j in range(h):
                c.set(sx + (lean if j > h * 0.6 else 0), y0 - j, LN[3] if j % 3 else LN[2])
            hx, hy = sx + lean, y0 - h
            c.set(hx, hy - 1, SK[1])
            c.set(hx + 1, hy - 1, SK[2])
            c.set(hx, hy, SK[2])
            c.set(hx + 1, hy, SK[3])
            if r.f() < 0.4:
                c.set(hx - 1, hy, SK[3])


def lichen_stones(c, r):
    """Two or three granite stones lying in the heather: speckled, lit upper-left, on a contact
    shadow, with a crust of lichen on the lit side."""
    cx, cy = spot(r, 12)
    for k in range(r.i(2, 3)):
        x, y = cx + r.i(-8, 8), cy + r.i(-6, 6)
        w = r.i(4, 6) if k == 0 else r.i(2, 4)
        hgt = 4 if k == 0 else 3
        for dx in range(-w, w + 1):
            c.set(x + dx, y + 1, OCC)
        for dy in range(-hgt, 1):
            for dx in range(-w, w + 1):
                if abs(dx) == w and dy in (-hgt, 0):
                    continue
                tone = ST[1]
                if dy == -hgt or dx == -w:
                    tone = ST[0]
                elif dy == 0 or dx == w:
                    tone = ST[2]
                elif r.f() < 0.22:
                    tone = SK[4]                          # feldspar
                c.set(x + dx, y + dy, tone)
        # the lichen crust, on the lit top-left
        for (dx, dy, t) in ((-w + 1, -hgt, 2), (-w + 2, -hgt, 1), (-w + 1, -hgt + 1, 3), (-w + 3, -hgt, 3)):
            if dx <= w - 1:
                c.set(x + dx, y + dy, LI[t])


# --- the foliage pass (2026-11-04, .ai/terrain.md section 13): the bottom rungs ----------------------
#
# Clutter, laid by Foliage.BrokenCircle.cs's rules by the drifts' canopy. All of it within about ten L*
# of the heather, nothing lit above a `lit` stop and nothing round and bright and alone; and nothing
# black and nothing in a line, because a dotted line of char across the heather is the Fallen Star's
# trail (BC1), the one thing on the moor that is straight.

def moor_grass(c, r):
    """Moor grass, combed by the wind: two or three tufts of fine blades, each rising from its root and
    bending over the same way, its tips near level. EVERY tuft leans east, on every variant - the detail
    layer is never mirrored, so this is the one place on the heath the wind can be drawn and stay one wind.
    Linen's shade with its base on the lit blades: grey-green, a moor in no season."""
    cx, cy = spot(r, 12)
    for k in range(r.i(2, 3)):
        x0, y0 = cx + r.i(-9, 7), cy + r.i(-5, 6)
        for b in range(r.i(6, 9)):
            bx = x0 + r.i(-3, 2)
            ln = r.i(8, 14)
            bend = 0.35 + r.f() * 0.35
            px, py = float(bx), float(y0)
            for j in range(ln):
                t = j / float(ln)
                # up, then over: the slope flattens toward the tip
                px += bend * (0.4 + t * 1.6)
                py -= max(0.15, 1.0 - t * 1.4)
                x, y = int(round(px)), int(round(py))
                if j == ln - 1:
                    tone = LN[2]
                elif b < 2 and t < 0.6:
                    tone = LN[2]
                elif (b + j) % 4 == 0:
                    tone = VE[4]
                else:
                    tone = LN[3]
                c.set(x, y, tone)
        c.set(x0 - 1, y0 + 1, OCC)
        c.set(x0, y0 + 1, OCC)
        c.set(x0 + 1, y0 + 1, OCC)


def bilberry(c, r):
    """Bilberry, low under the gorse at a drift's edge: wiry stems with small leaves in verdant's shade
    and deep, and berries in twos and threes - dusky blue-black, steel's shade and deep, never a lone
    round point (an XP orb is a lone round point)."""
    cx, cy = spot(r, 12)
    for k in range(r.i(1, 2)):
        x0, y0 = cx + r.i(-7, 7), cy + r.i(-4, 5)
        w = r.i(6, 8)
        for dx in range(-w, w + 1):
            hgt = max(1, int((w - abs(dx)) * 0.8) + r.i(0, 1))
            for j in range(hgt):
                lit = dx < 0 and j >= hgt - 2
                if (dx + j) % 3 == 0:
                    tone = VE[4]
                else:
                    tone = VE[3] if lit or (dx * 5 + j) % 7 == 0 else VE[4]
                c.set(x0 + dx, y0 - j, tone)
            c.set(x0 + dx, y0 + 1, OCC)
        for b in range(r.i(2, 3)):
            bx, by = x0 + r.i(-w + 1, w - 2), y0 - r.i(0, 2)
            c.set(bx, by, STL[3])
            c.set(bx + 1, by, STL[4])
            c.set(bx, by + 1, STL[4])
            c.set(bx + 1, by + 1, OCC)


def lichen_specks(c, r):
    """Lichen lying flat on the peat and the stones: two to four ragged rosettes, lichen's shade round its
    base. Flat, so no contact shadow. No gold: a warm fleck on the heath is a secret's glint
    (.ai/stage-secrets.md section 2), and a small rosette drawn round came out as a plus - a sparkle."""
    cx, cy = spot(r, 10)
    for k in range(r.i(2, 4)):
        x, y = cx + r.i(-10, 10), cy + r.i(-8, 8)
        rad = r.i(2, 3)
        for dy in range(-rad, rad + 1):
            for dx in range(-rad - 1, rad + 2):
                d = (dx / float(rad + 1.2)) ** 2 + (dy / float(rad + 0.4)) ** 2
                if d > 1.0 - r.f() * 0.35:
                    continue
                c.set(x + dx, y + dy, LI[3] if d < 0.45 else LI[4])
        c.set(x - 1, y - 1, LI[2])


# Granite lit to dark, as the standing stones and the tors are drawn (props_heath.GRANITE).
GRANITE = [SK[3], SK[4], ST[0], ST[1], ST[2]]


def stone(c, r, x, y, rx, ry, sunk=0.0):
    """One weathered stone lying in the heather: an outline that wanders (no two alike), lit on its
    upper-left shoulder and its top, turning into shade and occ underneath, with granite's feldspar
    fleck. `sunk` buries that share of its height under the turf, so a crown's rock is half in the
    ground rather than set on it. Heather sprigs grow up round its foot."""
    wob = [(k, 0.12 / k * (0.4 + r.f()), r.f() * 6.283) for k in (2, 3, 5)]
    pts = {}
    cut = y + ry * (1.0 - 2.0 * sunk)
    for yy in range(int(y - ry) - 1, int(y + ry) + 2):
        if yy > cut:
            continue
        for xx in range(int(x - rx) - 2, int(x + rx) + 3):
            dx, dy = (xx + 0.5 - x) / rx, (yy + 0.5 - y) / ry
            a = math.atan2(dy, dx)
            rad = 1.0 + sum(am * math.sin(k * a + ph) for (k, am, ph) in wob)
            d = math.sqrt(dx * dx + dy * dy) / rad
            if d <= 1.0:
                pts[(xx, yy)] = (dx / rad, dy / rad)
    for (xx, yy) in sorted(pts):
        nx, ny = pts[(xx, yy)]
        v = 0.3 + (-0.55 * nx - 0.45 * ny) * 0.5 + max(0.0, -ny - 0.3) * 0.5
        tone = GRANITE[max(0, min(4, int((1.0 - v) * 4.0 + 0.5)))]
        if (xx * 7 + yy * 3) % 11 == 0:
            tone = SK[4] if nx < 0.3 else ST[0]                  # feldspar
        if (xx + 1, yy) not in pts or (xx, yy + 1) not in pts:
            if nx * 0.5 + ny * 0.86 > 0.2:
                tone = OCC if ny > 0.3 else ST[2]
        c.set(xx, yy, tone)
    # the contact, and the turf round the foot
    bottom = {}
    for (xx, yy) in pts:
        bottom[xx] = max(bottom.get(xx, -99), yy)
    for xx in sorted(bottom):
        c.set(xx + 1, bottom[xx] + 1, OCC)
        if r.f() < 0.3:
            c.set(xx, bottom[xx], VE[4] if r.f() < 0.6 else VI[2])


def clitter(c, r):
    """Clitter: the granite the frost has prised off a tor, spilt down the drift below it - two to five
    broken stones of very different sizes, rounded by the weather, the smaller ones half sunk. It was
    a run of flat angular chips, which read as laid setts."""
    cx, cy = spot(r, 12)
    for k in range(r.i(2, 5)):
        x, y = cx + r.i(-10, 10), cy + r.i(-8, 8)
        big = k == 0
        rx = (3.5 + r.f() * 3.0) if big else (1.6 + r.f() * 2.2)
        stone(c, r, x, y, rx, rx * (0.6 + r.f() * 0.25), sunk=0.0 if big else 0.3 * r.f())


def rock_hump(c, r):
    """Bedrock breaking the heather on a drift: a hump of granite, sometimes two run together, still
    mostly under the turf - from a knuckle the size of a fist to one most of a cell across - lichened,
    with heather over its edges. The crown's rock, thickest toward the tor, in place of a floor of
    slabs (which read as paving)."""
    cx, cy = spot(r, 14)
    size = r.f()
    rx = 4.0 + size * size * 13.0
    ry = rx * (0.38 + r.f() * 0.18)
    stone(c, r, cx, cy, rx, ry, sunk=0.25 + r.f() * 0.15)
    if size > 0.4 and r.f() < 0.6:
        side = 1 if r.f() < 0.5 else -1
        stone(c, r, cx + side * rx * 0.8, cy + r.i(0, 2), rx * 0.5, ry * 0.7, sunk=0.35)
    for k in range(r.i(1, 3)):
        lx, ly = cx + r.i(-int(rx * 0.5), int(rx * 0.3)), cy - int(ry * 0.4) + r.i(-1, 1)
        if c.get(lx, ly)[3]:
            c.set(lx, ly, LI[2])
            c.set(lx + 1, ly, LI[3])
            c.set(lx, ly + 1, LI[3])


DETAIL_SETS = [
    ("heather_tufts", heather_tufts, 4),
    ("cotton_grass", cotton_grass, 4),
    ("lichen_stones", lichen_stones, 3),
    ("moor_grass", moor_grass, 4),
    ("bilberry", bilberry, 3),
    ("lichen_specks", lichen_specks, 3),
    ("clitter", clitter, 4),
    ("rock_hump", rock_hump, 5),
]
