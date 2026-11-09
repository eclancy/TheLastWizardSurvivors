# -*- coding: utf-8 -*-
"""The White Downs' ground (.ai/second-half.md 4.2, F3). Drawn by tools/art/tiles.py, which finds this
module by its name and appends TERRAINS to its own - run tiles.py, never this:

    python tools/art/tiles.py downs_turf downs_chalk downs_flint    # redraw only these while iterating
    python tools/art/tiles.py                                       # the full run that writes tiles.json

"Chalk downland at night under no moon: white turf, flint, barrows." The first chapter across the dark
sea, on the downs above the white cliffs: sheep-cropped turf over chalk, the chalk breaking through it
wherever the turf has gone, and flint everywhere in the chalk. Three pieces of ground:

  downs_turf   the floor. A TERRAIN USED AS THE GROUND (LevelGenerator's GroundTerrain), as the Cave's
               `cave_floor` and the Fields' `stubble` are, not a MATERIAL: a material is one of tiles.py's
               fixed floor styles, and the `natural` style on any row is a floor some chapter already
               stands on - on lichen it is the Forest's turf, on linen the Swamp's mud. So the turf brings
               its own painter.
  downs_chalk  bare chalk where the turf has gone: a scar, a bank the rabbits have scraped, a worn place.
  downs_flint  chalk rubble littered with flint nodules - the stony ground of a ploughed-out field or a
               spoil heap, every flint knobbly, pale-rinded, some broken to its dark glassy heart.

All three are walkable and none is a hazard: this module sets no HAZARDS and no BLOCKERS (tiles.py
refuses one that does). The downs' one solid thing is the barrows, which are walls (downs_walls.py).

THE TURF IS A WEAVE OF TWO ROWS AT ONE VALUE. Sheep-cropped turf at night is a cold dark green that is
not the Forest's teal, not the Swamp's grey mud and not the heath's violet. No row of the contract is that
colour, but two of its deep stops sit at the same lightness either side of it: lichen's deep (#16282E, a
teal-black, L* 14.9) and verdant's deep (#122A0A, a green-black, L* 14.5). Woven in patches a few pixels
across, they read from any distance as a cold green and up close as a sward of two greens - and because
their lightness is the same, the weave has no value in it at all: it is as quiet as a flat floor, and the
detail layer (detail_downs.py) carries the grass, the thyme and the flints, placed in drifts in world
space. That makes the turf about as dark as the heath (L* 12) and the stubble (L* 14): the downs' cast are
linen and steel from the deep halves of their rows (L* 12 to 52 and their rims), so they stand on it as the
heath's cast stand on the heather - by hue first and by their rims.

THE CHALK IS THE PALE THING, AND ONLY A STEP OR TWO. "White turf" is a name for the downs, not a value:
under no moon the chalk is the bone row's deep (#38404F, L* 27 - the Frozen Waste's snow, the one pale
floor), lumpy where the snow is smooth, its lumps catching the key on their upper left in the bone row's
shade (L* 46, a stop and a half under a body's soft rim) and turning into the stone row underneath. Never
the bone row's base or lit: chalk that bright would outshine the cast's rims. Flints in it, turf creeping
over its edge. What tells it from snow at a glance is that it is broken - lumps, cracks, flints, grass.

NOTHING WARM AND NOTHING THAT GLINTS (.ai/world-and-tone.md, the enemy palette; .ai/stage-secrets.md): a
warm fleck on the ground is a secret's tell. No gold, no flesh, no red; no lone `hi`. And nothing long and
pale: two of the downs' secrets are giant figures cut in the chalk of a hillside (a hound and a hart), so
the chalk here is blobs and crumbs, never a line, a rut or a stroke.
"""
from __future__ import division

import math

import tiles
from tiles import OCC, SIZE, clumps, fixed_hash, hashed, inside, ramp, scatter, specks

LI = ramp("lichen")
VE = ramp("verdant")
LN = ramp("linen")
SK = ramp("skin")
ST = ramp("stone")
STL = ramp("steel")

# The weave's lattice: a value noise on points WEAVE px apart, periodic on the tile. The points ON the tile's
# edge are the same in every variant (fixed_hash) and every other point is the variant's own. Interpolation
# between lattice points is continuous, so the field crosses from any variant into any other along the
# shared edge points with no seam - and nothing but those points repeats from cell to cell. (Sharing a band
# of points either side of the edge, the first try, printed the same patches along every tile's border: a
# grid.)
WEAVE = 3


def weave_value(x, y, salt, lattice=WEAVE):
    """The weave's noise at pixel (x, y), in [0, 1): bilinear between lattice points, smoothstepped.
    `lattice` must divide the tile (3, 4, 6, 8...)."""
    n = SIZE // lattice
    gx, gy = x / float(lattice), y / float(lattice)
    i0, j0 = int(math.floor(gx)), int(math.floor(gy))
    fx, fy = gx - i0, gy - j0
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)

    def pt(i, j):
        i, j = i % n, j % n
        if i == 0 or j == 0:
            return fixed_hash(i, j, salt)
        return hashed(i, j, salt)
    a = pt(i0, j0) + (pt(i0 + 1, j0) - pt(i0, j0)) * fx
    b = pt(i0, j0 + 1) + (pt(i0 + 1, j0 + 1) - pt(i0, j0 + 1)) * fx
    return a + (b - a) * fy


def relief(c, salt, lattice, tones, lift=0.0, grain=0.0, lit_at=0.42, shade_at=0.30):
    """A broken surface lit from the upper left, out of the weave's field read as a height: each pixel's
    tone is how squarely its slope faces the key. `tones` run lit to dark: (facing the key, flat, turned
    away, a crevice). The slope is measured one-sided at the tile's edge pixels, inward, so a lump the
    edge cuts is shaded from this tile's side only and no seam line is drawn where a different variant
    meets it. `lift` biases toward the lit tones, `grain` adds a per-pixel nudge (the same in every
    variant) so a facet's edge is crumbly rather than a contour."""
    field = [[weave_value(x, y, salt, lattice) for x in range(SIZE)] for y in range(SIZE)]
    for y in range(SIZE):
        for x in range(SIZE):
            x0, x1 = max(0, x - 1), min(SIZE - 1, x + 1)
            y0, y1 = max(0, y - 1), min(SIZE - 1, y + 1)
            gx = (field[y][x1] - field[y][x0]) / float(x1 - x0)
            gy = (field[y1][x] - field[y0][x]) / float(y1 - y0)
            # the key is upper left: a slope that rises toward it (falls toward the lower right) faces it
            light = -(gx + gy) * lattice * 1.2 + lift
            light += (fixed_hash(x, y, salt + 11) - 0.5) * grain
            h = field[y][x]
            if light > lit_at:
                tone = tones[0]
            elif light > -shade_at:
                tone = tones[1]
            elif h < 0.30 and light < -shade_at * 2.0:
                tone = tones[3]
            else:
                tone = tones[2]
            c.set(x, y, tone)


def turf_weave(c, salt, green, teal, hollow, hollow_at=0.22):
    """Lay the sward: `green` where the weave is high, `teal` where it is low, `hollow` (a step darker) in
    its lowest dips. The boundary between the two greens is broken a pixel or two by a hash, so a patch is
    a tussock's edge and not a contour line."""
    for y in range(SIZE):
        for x in range(SIZE):
            v = weave_value(x, y, salt)
            v += (fixed_hash(x, y, salt + 7) - 0.5) * 0.16
            if v < hollow_at:
                tone = hollow
            elif v < 0.5:
                tone = teal
            else:
                tone = green
            c.set(x, y, tone)


def cropped_tuft(cv, x, y, k, root, blade, tip):
    """Turf the sheep have cropped: three or four blades a pixel or two tall, the root dark on one side.
    Short and squat - grass that is never let grow - so a field of them is a nap, not a lawn of tufts."""
    cv.set(x, y, blade)
    cv.set(x + 1, y, blade if k % 2 else root)
    cv.set(x - 1, y, root)
    cv.set(x, y - 1, tip)
    if k % 3 == 1:
        cv.set(x + 2, y - 1, blade)
    if k % 4 == 0:
        cv.set(x - 1, y - 1, blade)


def chalk_crumb(cv, x, y, k):
    """A crumb of chalk the turf has not covered: two or three pixels of bone's deep, its lower right in
    the stone row, the odd one with a pixel of bone's shade where the key finds it. Faint, here and there."""
    cv.set(x, y, SK[4])
    cv.set(x + 1, y, SK[4] if k % 2 else ST[2])
    cv.set(x, y + 1, ST[2])
    if k % 3 == 0:
        cv.set(x - 1, y, SK[4])
        cv.set(x - 1, y - 1, SK[3] if k % 2 else SK[4])


def body_downs_turf(c):
    """The floor: sheep-cropped turf on chalk, a cold dark green woven of two rows at one value (see the
    module note), with a cropped nap of linen-grey blade tips, hollows a step down in linen's deep, and
    chalk showing through it in crumbs on some cells and not others."""
    turf_weave(c, 5101, VE[4], LI[4], LN[4])
    # The nap: blade tips in linen's shade, the only thing in the tile above the weave, and a little of it.
    # Few in the shared layer: a tuft shared by every variant is at the same place in every cell, and half
    # of them shared printed a lattice of specks 48 px apart across a field of turf.
    scatter(c, 10, 5111, 3, lambda cv, x, y, k: cropped_tuft(cv, x, y, k, LN[4], LI[3] if k % 3 else LN[3],
                                                             LN[3]), shared=0.2)
    # Chalk through the turf: two variants in six carry a crumb or two, one a little scrape of them.
    v = tiles.VARIANT
    if v in (1, 4):
        for k in range(1 + v % 2):
            chalk_crumb(c, inside(k, 5131 + v, 6), inside(k + 4, 5133 + v, 6), k + v)
    elif v == 3:
        cx, cy = inside(0, 5135, 10), inside(1, 5137, 10)
        for k in range(4):
            chalk_crumb(c, cx + int((hashed(k, 1, 5139) - 0.5) * 8), cy + int((hashed(k, 2, 5139) - 0.5) * 5), k)


# --- the chalk -------------------------------------------------------------------------------------------

def chalk_lump(cv, cx, cy, k, salt):
    """One lump of the chalk's broken surface: bone's deep, its upper left lit in bone's shade, its lower
    right turning into the stone row - so a field of them is a crumbled surface, not a flat sheet (the snow
    is the flat sheet)."""
    r = 1.6 + hashed(k, 1, salt) * 2.4
    ry = r * (0.6 + hashed(k, 2, salt) * 0.25)
    pts = {}
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            nx, ny = (x + 0.5 - cx) / r, (y + 0.5 - cy) / ry
            d = nx * nx + ny * ny + (hashed(x, y, salt + 5) - 0.5) * 0.3
            if d <= 1.0:
                pts[(x, y)] = (nx, ny)
    for (x, y), (nx, ny) in pts.items():
        # a lit cap on the upper left, the body in bone's deep, the turned-away side in the stone row
        light = -(nx * 0.7 + ny)
        if light > 0.25:
            tone = SK[3]
        elif light < -0.55:
            tone = ST[2]
        else:
            tone = SK[4]
        cv.set(x, y, tone)
    # its shadow on the chalk, down and to the right, and the contact under it
    for (x, y) in pts:
        if (x, y + 1) not in pts:
            cv.set(x + 1, y + 1, OCC if (x + y) % 3 == 0 else ST[2])


def flint(cv, cx, cy, k, salt, size=1.0, knapped=None):
    """A flint nodule, knobbly: two or three lobes run together, black inside, with the pale cortex rind
    that flint carries out of the chalk - lit on its upper left in bone's shade, bone's deep round the rest.
    Knapped (broken), one face shows the glassy dark grey heart in steel's deep and shade instead of rind.
    Matte: the brightest pixel is the rind's shade stop, never a glint on the glass."""
    lobes = []
    n = 2 + int(hashed(k, 3, salt) * 2)
    for j in range(n):
        a = hashed(k, 10 + j, salt) * math.pi * 2.0
        rr = (1.2 + hashed(k, 20 + j, salt) * 1.0) * size
        off = (0.6 + hashed(k, 30 + j, salt) * 0.8) * size if j else 0.0
        lobes.append((cx + math.cos(a) * off, cy + math.sin(a) * off * 0.7, rr))
    if knapped is None:
        knapped = hashed(k, 4, salt) < 0.4
    pts = set()
    for (lx, ly, rr) in lobes:
        for y in range(int(ly - rr) - 1, int(ly + rr) + 2):
            for x in range(int(lx - rr) - 1, int(lx + rr) + 2):
                if (x + 0.5 - lx) ** 2 + ((y + 0.5 - ly) / 0.8) ** 2 <= rr * rr:
                    pts.add((x, y))
    for (x, y) in pts:
        edge = any((x + ex, y + ey) not in pts for (ex, ey) in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        upper_left = (x - 1, y) not in pts or (x, y - 1) not in pts
        lower_right = (x + 1, y) not in pts or (x, y + 1) not in pts
        if knapped and (x - cx) + (y - cy) > -0.5:
            # the broken face: dark glass, a facet of steel's shade on its upper left, the rest its deep
            tone = STL[3] if (x - cx) + (y - cy) < 1.5 and (x + y) % 2 == 0 else (STL[4] if not lower_right else OCC)
        elif edge and upper_left:
            tone = SK[3]
        elif edge and lower_right:
            tone = ST[3]
        elif edge:
            tone = SK[4]
        else:
            tone = ST[4]
        cv.set(x, y, tone)
    # the contact, down and to the right
    for (x, y) in pts:
        if (x, y + 1) not in pts and (x + 1, y + 1) not in pts:
            cv.set(x + 1, y + 1, OCC)


def turf_tongue(cv, x, y, k):
    """Turf creeping out over bare chalk: a short tongue of the sward with its cropped tips."""
    w = 3 + int(hashed(k, 1, 5251) * 4)
    for j in range(w):
        cv.set(x + j, y, VE[4] if (j + k) % 2 else LI[4])
        if 0 < j < w - 1:
            cv.set(x + j, y - 1, LI[4] if j % 2 else VE[4])
    cv.set(x + 1, y - 2, LN[3])
    cv.set(x + w - 2, y - 1, LI[3])


def body_downs_chalk(c):
    """Bare chalk: bone's deep, broken into lumps that catch the key, hairline cracks in the stone row, a
    flint or two in it, and the sward creeping over it in tongues. A step and a half over the turf, which
    is as pale as the downs get."""
    # The surface: a slow undulation, mostly flat bone's deep, a slope looking at the key in bone's shade
    # and one turned away in steel's deep - broad and few, so a scar is a sheet of chalk and not stucco.
    relief(c, 5201, 8, (SK[3], SK[4], STL[4], ST[2]), grain=0.5, lit_at=0.95, shade_at=0.85)
    # Lumps and crumbs lying on it, each lit on its cap and shadowed down and to the right.
    scatter(c, 5, 5211, 5, lambda cv, x, y, k: chalk_lump(cv, x, y, k, 5213), shared=0.5)
    # Hairline cracks, short and turning, kept inside: a crack has a shape, so it is the variant's own.
    v = tiles.VARIANT
    if v % 2 == 0:
        tiles.crack(c, inside(v, 5221, 9), inside(v + 2, 5223, 9), 5 + int(hashed(v, 1, 5225) * 5), ST[2], 5227 + v)
    # Flints weathered out of it.
    scatter(c, 1 + v % 2, 5231, 7, lambda cv, x, y, k: flint(cv, x, y, k + v, 5233, 0.9), shared=0.0)
    # The sward creeping back over it.
    scatter(c, 2, 5241, 7, turf_tongue, shared=0.5)


# --- the flint ground -------------------------------------------------------------------------------------

def body_downs_flint(c):
    """Chalk rubble littered with flints: a bed of crumbled chalk and soil - linen's shade with chalk in
    clumps and crumbs through it - and on it flint nodules of every size, knobbly, pale-rinded, some broken
    to the dark glass inside. Stony ground you can walk over, darker than bare chalk and lighter than the
    turf, and speckled where the chalk is plain."""
    # The bed: rubble, finer than the bare chalk's lumps - chalk on the faces that catch the key, soil
    # (linen's shade and deep) between and under them.
    relief(c, 5301, 6, (SK[4], LN[3], LN[4], OCC), grain=0.6, lit_at=0.45, shade_at=0.55)
    scatter(c, 4, 5311, 4, lambda cv, x, y, k: chalk_lump(cv, x, y, k, 5313), shared=0.5)
    # The flints: nodules of every size, and one or two big knobbly ones the variant owns.
    scatter(c, 8, 5321, 5, lambda cv, x, y, k: flint(cv, x, y, k, 5323, 0.7 + hashed(k, 9, 5325) * 0.9),
            shared=0.45)
    v = tiles.VARIANT
    scatter(c, 1 + (v % 3 == 0), 5331, 9, lambda cv, x, y, k: flint(cv, x, y, k + 40 + v, 5333, 1.6 + 0.4 * k),
            shared=0.0)


TERRAINS = [
    ("downs_turf", body_downs_turf, None),
    ("downs_chalk", body_downs_chalk, None),
    ("downs_flint", body_downs_flint, None),
]
