# -*- coding: utf-8 -*-
"""The White Downs' barrows: the `barrow_wall` set. Drawn by tools/art/tiles.py (wall_set_modules) - run
tiles.py, never this. Part of piece F3 (.ai/second-half.md 4.2): "Open, the barrows solid."

The downs are open ground, and the only solid things on them are the barrows - the long and round burial
mounds of people older than the wizards, grassed over, cropped by the sheep, lying on the turf like sleepers.
They are walls: the chapter's generator places them as `wall` cells (a round barrow a disc about five cells
across, a long barrow two or three cells wide and five to eight long), so they collide, block a shot and
are routed round like every wall in the game, and in the boss fight the player puts one between himself and
the Unstrung Hunter's line. This set is only how those cells are DRAWN on the downs
(StageEnvironmentProfile.WallSet = "barrow_wall"), in the jail set's shape (jail_walls.py): every autotile
role, the three stubs the spawn clearing leaves, plain variants and dressed ones carrying a weight.

THREE-QUARTER VIEW, SO SOUTH DECIDES, as in the jail and the cave. Floor to the south is the mound's FACE:
its southern flank, turf rolling down away from the key into shade, chalk showing where the turf has slipped,
an `occ` contact line at its foot (and the jail's shadow, thrown on the turf below it by the painter). Every
other cell is the mound's TOP seen from above: rounded, cropped turf, a little lumpy, its crest turning down
out of sight - pale where the chalk under the turf shows on the north and west, which face the key, dark on
the east, which faces away.

THE ORDER OF VALUES IS THE RUINS', NOT THE CAVE'S, because a barrow stands under the open sky. A dome lit
from the upper left is lighter than the flat turf round it on the shoulders that look at the key and darker
on the flank that looks away. Measured from the tiles (detail_contrast's L*, mean of the plain variants):

  chalk crest (bone's deep, a fleck of its shade)   L* 27-46   the brightest thing a barrow owns, a broken line
  the lit shoulders, north and west                  L* 23-26   turf a stop up (lichen's and verdant's shade)
  the crown                                          L* 18      the weave with its lumps lit; leaning green
  the floor (downs_turf)                             L* 14.5
  the east flank                                     L* 15      the weave, falling to linen's deep and occ
  the face                                           L* 11-12   the weave sinking through linen's deep to occ

A mound reads as raised, not as a hole, because of where its light is: a hole lit from the upper left is
dark on its near (north-west) lip and lit on its far wall, and a barrow is the other way round - pale crest
to the north-west, dark flank to the south and east, a contact line and a thrown shadow at its foot. It is
told from the floor by hue as well (turf_tone: the barrow's sward leans green), and the top stays within a
stop and a half of the floor so the mounds frame the fight rather than becoming the subject.

ROUNDED, NOT SQUARE (cave_walls.py's method). The autotiler resolves a disc to a staircase of outer and
inner corners. Every outer corner here is cut round (transparent, so the turf under the cell shows: every
cell carries a ground tile, .ai/terrain.md section 1) - the north-west one right across the tile (CHAMFER_R),
so a staircase's lit side lines up into one curve - the faces' feet curve up at their ends, and every rim
wanders along its length, pinned at the tile's corners so any variant meets any other. The collision stays
the full cell (LevelTilePainter.AddWallCollision), so it overhangs the drawn mound where a corner is cut: by
about 27 px on the diagonal of a north-west corner, 13 on the north-east, 12 and 8 at a face's two feet.

NOTHING WARM, NOTHING GLOWING, NO WAY IN. No doors, no passages, no stones standing at an entrance: a barrow
here is closed and grassed. Its dressings are what a downland mound has - a sarsen kerb-stone at its foot, a
rabbit hole with its spoil of chalk, a hawthorn seedling, a slip where the turf has come away, flints the
rain has washed out. Nothing warm and nothing that glints (a warm glint is a secret's tell,
.ai/stage-secrets.md), nothing long and pale (the downs' chalk figures are secrets), nothing in an XP orb's
colours, and nothing animates.
"""
from __future__ import division

import math

import bonelight as bl
import jail_walls as jw
from jail_walls import BAYER, SIZE, _get, _set, hashed

OCC = bl.OCC
CLEAR = (0, 0, 0, 0)

TERRAIN = "barrow_wall"


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


LI = ramp("lichen")
VE = ramp("verdant")
LN = ramp("linen")
SK = ramp("skin")
ST = ramp("stone")
STL = ramp("steel")

SIDE_ROLES = ("edge_left", "edge_right")

# Dressing: incidents on a barrow, never wallpaper. The painter rolls a dressed face on about one face in
# three (never two side by side), a dressed top far more rarely.
FACE_DRESSINGS = [
    ("kerb", 3),        # a sarsen kerb-stone half sunk at the foot
    ("rabbit", 3),      # a rabbit hole in the flank, its spoil of chalk spilt below it
    ("slip", 3),        # the turf come away down the flank, bare chalk above the slumped sod
    ("capping", 2),     # the barrow's old chalk capping breaking through under the crest
    ("thorn", 2),       # a hawthorn seedling rooted in the flank
    ("flints", 2),      # flints the rain has washed out of the mound, lying at its foot
]
SIDE_DRESSINGS = [("scrape", 2), ("thorn", 1)]
CAP_DRESSINGS = [("scrape", 1), ("thorn", 1), ("flints", 1)]

# The one-tile stubs the spawn clearing leaves (LevelGenerator.ClearSpawn): the jail's three.
STUB_SIDES = jw.STUB_SIDES

PLAIN_VARIANTS = {
    "edge_bottom": 4, "corner_bl": 2, "corner_br": 2,
    "edge_left": 3, "edge_right": 3,
    "edge_top": 4, "fill": 4,
}


def variants(role):
    """[(dressing or None, plain index, weight)] for a role, plain first, in manifest order."""
    plain = PLAIN_VARIANTS.get(role, 2)
    out = [(None, k, 1) for k in range(plain)]
    if role == "edge_bottom":
        out += [(d, 0, w) for d, w in FACE_DRESSINGS]
    elif role in SIDE_ROLES:
        out += [(d, 0, w) for d, w in SIDE_DRESSINGS]
    elif role in ("edge_top", "fill"):
        out += [(d, 0, w) for d, w in CAP_DRESSINGS]
    return out


def seed_for(dressing, plain_index):
    names = [d for d, _ in FACE_DRESSINGS] + [d for d, _ in SIDE_DRESSINGS + CAP_DRESSINGS]
    return 71 + plain_index * 19 + (names.index(dressing) * 7 + 3 if dressing in names else 0)


# --- the field every barrow tile shares --------------------------------------------------------------------
#
# Turf is drawn from a value noise on a lattice periodic on the tile, as terrain_downs.py's weave is: the
# lattice points ON the tile's edges are the same in every tile of the set (whatever its role or variant), the
# points inside are the tile's own. Interpolation is continuous, so the sward runs from any barrow tile into
# any other with no seam, a top into the face below it included.

def field(x, y, salt, seed, lattice):
    n = SIZE // lattice
    gx, gy = x / float(lattice), y / float(lattice)
    i0, j0 = int(math.floor(gx)), int(math.floor(gy))
    fx, fy = gx - i0, gy - j0
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)

    def pt(i, j):
        i, j = i % n, j % n
        return hashed(i, j, salt) if (i == 0 or j == 0) else hashed(i, j, salt + seed * 131)
    a = pt(i0, j0) + (pt(i0 + 1, j0) - pt(i0, j0)) * fx
    b = pt(i0, j0 + 1) + (pt(i0 + 1, j0 + 1) - pt(i0, j0 + 1)) * fx
    return a + (b - a) * fy


def grain(x, y, salt):
    """A per-pixel nudge the same in every tile, so a boundary between two tones is crumbly."""
    return hashed(x, y, salt) - 0.5


def wander(i, salt, seed, amp):
    """How far a rim wanders at pixel i along a side: zero at both ends of the tile (tiles.wander's trick),
    so any variant of any role meets any other exactly at the corners."""
    a1 = hashed(seed, 1, salt) * 2.0 - 1.0
    a2 = hashed(seed, 2, salt) * 2.0 - 1.0
    a3 = hashed(seed, 3, salt) * 2.0 - 1.0
    t = math.pi * i / float(SIZE - 1)
    return amp * (0.6 * a1 * math.sin(t) + 0.35 * a2 * math.sin(2 * t) + 0.25 * a3 * math.sin(3 * t))


# --- the outline ------------------------------------------------------------------------------------------
#
# A barrow tile is cut to its outline by a signed distance from it (positive inside the mound), with the
# outward normal of the nearest piece of outline - which is what decides whether a rim faces the key.

RIM = 3.0          # how far in from an open side the outline sits, at the tile's corners
RIM_EAST = 0.8     # less on the east, so the shadow the painter throws there meets the mound's dark flank
RIM_AMP = 2.0      # how far it wanders either way in the middle of a side (the east and the foot, less)
FOOT_IN = 1.5      # a face's foot sits this far in from the tile's bottom
TUSSOCK = 12       # the lattice of the top's lumps
# The radius each outer corner is cut round to. Small on the north-east and the faces' feet, because the painter
# throws the jail's square shadow east of a wall and south of a face (jail_walls.shadow, shared by every set):
# a corner cut deep there leaves that shadow standing beside nothing.
CORNER_R = {("t", "l"): 40.0, ("t", "r"): 24.0, ("b", "l"): 22.0, ("b", "r"): 16.0}

# THE NORTH-WEST CORNER IS CUT ACROSS THE WHOLE TILE. A round barrow is a disc of cells, which the autotiler
# resolves to a staircase, and a staircase of rounded corners is a quatrefoil (a disc of radius 2.5 cells came
# out as a plus with round arms). A corner_tl tile's outline instead runs from the far end of its top side to
# the far end of its left side along a shallow arc of CHAMFER_R - and since the corner tiles of a staircase step
# one cell down and one cell left, their arcs line up on one diagonal, with only a three-pixel dimple at each
# inner corner between them. So the lit north-west of a barrow is one smooth curve, a quarter of an octagon
# bulging toward a circle. Only the north-west: it is the side the key lights and the eye reads a mound by,
# and the one side where the painter throws no shadow that a deep cut would leave standing alone.
CHAMFER_R = 58.0


def _chamfer():
    """The centre of the north-west chamfer's circle: through (SIZE, RIM) and (RIM, SIZE), inside the tile."""
    ax, ay, bx, by = float(SIZE), RIM, RIM, float(SIZE)
    mx, my = (ax + bx) / 2.0, (ay + by) / 2.0
    half = math.hypot(bx - ax, by - ay) / 2.0
    k = math.sqrt(max(0.0, CHAMFER_R * CHAMFER_R - half * half)) / math.sqrt(2.0)
    return mx + k, my + k


CHAMFER_C = _chamfer()

KEY = (-0.7071, -0.7071)   # the key, upper left, as the direction a lit surface faces


def side_depth(side, x, y):
    if side == "t":
        return y + 0.5, x
    if side == "b":
        return SIZE - 0.5 - y, x
    if side == "l":
        return x + 0.5, y
    return SIZE - 0.5 - x, y


NORMAL = {"t": (0.0, -1.0), "b": (0.0, 1.0), "l": (-1.0, 0.0), "r": (1.0, 0.0)}


def outline(sides, x, y, seed):
    """(signed distance, outward normal) at pixel (x, y) for a tile whose open sides are `sides`."""
    if not sides:
        return 99.0, (0.0, 0.0)
    es = {}
    for s in sides:
        d, i = side_depth(s, x, y)
        inset = FOOT_IN if s == "b" else (RIM_EAST if s == "r" else RIM)
        amp = 1.2 if s == "b" else (0.6 if s == "r" else RIM_AMP)
        es[s] = d - (inset + wander(i, 6100 + "tblr".index(s), seed, amp))
    s_min = min(es, key=lambda s: es[s])
    e, n = es[s_min], NORMAL[s_min]
    if tuple(sorted(sides)) == ("l", "t"):
        # corner_tl: the chamfer (see CHAMFER_R), with a wander along it pinned at both ends
        px, py = x + 0.5 - CHAMFER_C[0], y + 0.5 - CHAMFER_C[1]
        dist = math.hypot(px, py) or 1.0
        ang = math.atan2(-py, -px)                    # about pi/4 across the arc
        u = max(0.0, min(1.0, (ang - math.pi / 4.0) / (math.pi / 2.0) * 2.4 + 0.5))
        ee = CHAMFER_R - dist + 1.6 * math.sin(math.pi * u) * (hashed(seed, 5, 6105) * 2.0 - 1.0)
        if ee < e:
            return ee, (px / dist, py / dist)
        return e, n
    for a, b in (("t", "l"), ("t", "r"), ("b", "l"), ("b", "r")):
        if a in es and b in es:
            r = CORNER_R[(a, b)]
            if es[a] < r and es[b] < r:
                ua, ub = r - es[a], r - es[b]
                ee = r - math.hypot(ua, ub)
                if ee < e:
                    e = ee
                    na, nb = NORMAL[a], NORMAL[b]
                    ln = math.hypot(ua, ub) or 1.0
                    n = ((na[0] * ua + nb[0] * ub) / ln, (na[1] * ua + nb[1] * ub) / ln)
    return e, n


def facing(n):
    return n[0] * KEY[0] + n[1] * KEY[1]


# --- the top ------------------------------------------------------------------------------------------------

def turf_tone(v, x, y, seed):
    """A value in 0..1 to the barrow's turf: occ, linen's deep, the floor's two greens woven (lichen's and
    verdant's deep), and the same two a stop up (their shade). Flat steps, dithered only near a boundary
    (trees.band's squeeze), so a slope reads as a few clear tones and not as static.

    The weave leans green (two parts verdant to one of lichen, where the floor is half and half): nobody
    ploughs a barrow and the sheep crop it less, so its turf is the ranker, greener sward - which separates a
    mound from the floor by hue as well as by its light."""
    hue = field(x, y, 6205, seed, 3) + grain(x, y, 6207) * 0.15
    teal = hue < GREEN_AT
    levels = (OCC, LN[4], LI[4] if teal else VE[4], LI[3] if teal else VE[3])
    p = max(0.0, min(0.999, v)) * (len(levels) - 1)
    i = int(p)
    f = p - i
    f = max(0.0, min(1.0, (f - 0.5) / 0.45 + 0.5))
    # Dithered in hashed 2x2 blocks, not by the Bayer matrix: the tussocks' lit sides are diagonal bands,
    # and a Bayer screen across a diagonal band prints as hatching.
    th = hashed(x // 2, y // 2, 6251) * 0.9 + 0.05
    return levels[i + 1] if f > th else levels[i]


def tussock(x, y, seed):
    """How squarely the turf's lumps face the key at a pixel, about -1..1: the slope of a coarse field."""
    e = 0.9
    gx = field(x + e, y, 6201, seed, TUSSOCK) - field(x - e, y, 6201, seed, TUSSOCK)
    gy = field(x, y + e, 6201, seed, TUSSOCK) - field(x, y - e, 6201, seed, TUSSOCK)
    return max(-1.0, min(1.0, -(gx + gy) * 2.6))


CROWN = 0.79        # the crown's value: a little over the floor's, lit on the tops of its lumps
GREEN_AT = 0.36     # where the weave turns from lichen's teal to verdant's green (the floor's is 0.5)
SHOULDER = 28.0     # how far in from the rim a shoulder's light (or shadow) reaches


def top(c, seed, sides, inner):
    for y in range(SIZE):
        for x in range(SIZE):
            e, n = outline(sides, x, y, seed)
            if e < 0:
                _set(c, x, y, CLEAR)
                continue
            f = facing(n) if e < SHOULDER else 0.0
            near = max(0.0, 1.0 - e / SHOULDER) ** 1.3
            # the dome: lit on the shoulders that look at the key, falling into shade on the one that does not
            v = CROWN + f * near * (0.42 if f > 0 else 0.55) + tussock(x, y, seed) * 0.13 + grain(x, y, 6203) * 0.08
            tone = turf_tone(v, x, y, seed)
            if e < 1.0:
                # the crest itself, where the turf turns down out of sight: chalk on the lit side, broken
                if f > 0.3:
                    r = hashed(x // 2, y // 2, 6211 + seed)
                    tone = SK[4] if r < 0.66 else (SK[3] if r < 0.78 else LI[3])
                elif f < -0.3:
                    tone = OCC
                else:
                    tone = LN[4] if hashed(x, y, 6215) < 0.5 else LI[3]
            elif e < 2.0 and f < -0.3:
                tone = OCC if hashed(x, y, 6217) < 0.6 else LN[4]
            elif e < 2.2 and f > 0.3 and hashed(x // 2, y // 2, 6219 + seed) < 0.3:
                tone = SK[4]                    # the chalk under the crest, here and there
            _set(c, x, y, tone)
    if inner:
        notch(c, inner, seed)


def notch(c, inner, seed):
    """An inner corner: the floor beyond the diagonal pokes into the outline at this corner. A quarter-disc
    of it (RIM across) is cut, rimmed as the two neighbours' rims are; and on the south side, where the
    neighbour is a face, the west or east shoulder of this part of the mound runs down past it."""
    v, h = inner
    cx = 0.0 if h == "l" else float(SIZE)
    if v == "t":
        rx = RIM if h == "l" else RIM_EAST
        for y in range(0, 6):
            for x in range(SIZE):
                d = math.hypot((x + 0.5 - cx) / rx, (y + 0.5) / RIM)
                if d < 1.0:
                    _set(c, x, y, CLEAR)
                elif d < 1.0 + 1.0 / RIM:
                    lit = h == "l" or abs(x + 0.5 - cx) > 1.0
                    _set(c, x, y, SK[4] if lit else OCC)
        return
    # South inner corners. The face beside this tile belongs to the part of the mound one step further
    # north; this tile is the top of the part that runs a step further south, whose west (or east) shoulder
    # is seen against that face: lit on the west, where it looks at the key, in shade on the east. A soft
    # band, not a drawn line - a chalk crease here read as a seam down the mound. At the very corner the
    # floor beyond the diagonal pokes in under both feet, so a sliver is cut there and the contact runs on.
    for y in range(8, SIZE):
        t = (y - 8) / float(SIZE - 1 - 8)
        band = 2.0 + 4.0 * math.sin(t * math.pi * 0.9)
        for k in range(int(band) + 2):
            x = k if h == "l" else SIZE - 1 - k
            here = _get(c, x, y)
            if here is None:
                continue
            th = (BAYER[y % 4][x % 4] + 0.5) / 16.0
            fade = 1.0 - k / (band + 1.0)
            if h == "l":
                if th < fade * 0.8 and here in (LI[4], VE[4], LN[4]):
                    _set(c, x, y, LI[3] if (x + y) % 3 else VE[3])
            elif th < fade * 0.9:
                _set(c, x, y, OCC if k == 0 or th < fade * 0.35 else LN[4])
    rx = RIM if h == "l" else RIM_EAST
    for y in range(SIZE - 5, SIZE):
        for x in range(SIZE):
            d = math.hypot((x + 0.5 - cx) / rx, (SIZE - 0.5 - y) / FOOT_IN)
            if d < 1.0:
                _set(c, x, y, CLEAR)
            elif d < 1.0 + 2.0 / FOOT_IN and abs(x + 0.5 - cx) < rx + 3:
                _set(c, x, y, OCC)


# --- the face -----------------------------------------------------------------------------------------------

FOOT = 42          # where the contact begins, before the wander


def face(c, seed, sides):
    """The mound's southern flank: the top's turf rolling over a crest and down into shade, linen's deep and
    then `occ` toward the foot, a tussock or two standing proud of it, with the contact line under it. Its ends, where the floor is beside it, curve up into the top's rim."""
    for y in range(SIZE):
        for x in range(SIZE):
            e, n = outline(sides, x, y, seed)
            if e < 0:
                _set(c, x, y, CLEAR)
                continue
            # down the flank: the crest band (rows 0-6) carries the top's turf, then the slope sinks
            crest = 6.0 + wander(x, 6401, seed, 1.5)
            sink = max(0.0, (y - crest) / (FOOT - crest))
            th = (BAYER[(y // 2) % 4][(x // 2) % 4] + 0.5) / 16.0
            if y < crest - 2:
                v = CROWN - 0.04 + tussock(x, y, seed) * 0.13
            elif y < crest:
                # the brow, where the top rolls over into the flank: the key grazes it. It fades out toward an
                # open end, where the flank turns away round the end of the mound, so the steps of a round
                # barrow's staircase are not each underlined.
                end = min([99.0] + [side_depth(sd, x, y)[0] for sd in sides if sd in "lr"])
                v = CROWN - 0.04 + (0.86 - CROWN + 0.04) * max(0.0, min(1.0, (end - 6.0) / 18.0)) + grain(x, y, 6407) * 0.2
            else:
                # the flank in shade: from just under the brow down through linen's deep to occ
                v = 0.56 - (sink ** 1.4) * 0.5 + tussock(x, y, seed) * 0.1 + grain(x, y, 6409) * 0.06
            tone = turf_tone(v, x, y, seed)
            # the ends: the west end turns toward the key, the east away
            f = facing(n)
            if e < 1.0 and n[1] < 0.6:
                tone = (SK[4] if hashed(x // 2, y // 2, 6411 + seed) < 0.55 else LI[3]) if f > 0.2 else OCC
            elif e < 4.0 and n[0] < -0.5 and y < FOOT and tone in (LN[4], LI[4], VE[4]):
                tone = LI[3] if (x + y) % 3 else VE[3]
            elif e < 3.0 and n[0] > 0.5:
                tone = OCC if th < 0.6 else LN[4]
            # the foot: the contact line the mound stands on, following the outline round its ends
            if e < 2.5 and n[1] > 0.3:
                tone = OCC
            _set(c, x, y, tone)
    # Tussocks on the flank: low lumps of the sward standing a little proud of the slope, each lit along its
    # top and dark under it. Kept seven pixels off both sides, so a lump is never cut by a neighbour.
    for j in range(2 + int(hashed(seed, 9, 6421) * 3)):
        tx = 8 + int(hashed(seed, j, 6425) * 32)
        ty = 13 + int(hashed(seed, j, 6423) * 22)
        w = 2 + int(hashed(seed, j, 6427) * 3)
        if _get(c, tx, ty) is None:
            continue
        for dx in range(-w, w + 1):
            hgt = 1 if abs(dx) == w else 2
            for dy in range(hgt):
                lit = dy == hgt - 1 and dx < w - 1 and hashed(tx + dx, ty, 6429 + seed) < 0.6
                _set(c, tx + dx, ty - dy, (LI[3] if (tx + dx) % 3 else VE[3]) if lit else
                     (LI[4] if (dx + j) % 2 else VE[4]))
            _set(c, tx + dx + 1, ty + 1, LN[4] if abs(dx) < w else OCC)
    # Grass hanging over the foot, and chalk crumbs fallen there.
    for k in range(9):
        x = 4 + int(hashed(k, seed, 6431) * 40)
        for yy in range(SIZE - 1, 30, -1):
            if _get(c, x, yy) == OCC and _get(c, x, yy - 1) not in (OCC, None):
                _set(c, x, yy, LI[4] if k % 2 else VE[4])
                if k % 3 == 0:
                    _set(c, x + 1, yy + 1, SK[4] if _get(c, x + 1, yy + 1) == OCC else _get(c, x + 1, yy + 1))
                break


# --- dressing -------------------------------------------------------------------------------------------------

def blob(cx, cy, rx, ry, seed, wob=0.18):
    """A lumpy outline's pixels: {(x, y): (nx, ny)}."""
    harm = [(k, wob / k * (0.5 + hashed(seed, k, 6501)), hashed(seed, k + 9, 6503) * 6.283) for k in (2, 3, 5)]
    pts = {}
    for y in range(int(cy - ry) - 2, int(cy + ry) + 3):
        for x in range(int(cx - rx) - 2, int(cx + rx) + 3):
            dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            a = math.atan2(dy, dx)
            r = 1.0 + sum(am * math.sin(k * a + ph) for (k, am, ph) in harm)
            d = math.hypot(dx, dy) / r
            if d <= 1.0:
                pts[(x, y)] = (dx / r, dy / r)
    return pts


def sarsen(c, cx, cy, rx, ry, seed):
    """A sarsen - a grey wether - lying half sunk: a low rounded boulder, pale lichened grey (the bone row's
    deep and shade over the stone row), lit on its upper left, turning into occ underneath, with the turf grown
    up round its foot."""
    pts = blob(cx, cy, rx, ry, seed)
    for (x, y), (nx, ny) in pts.items():
        v = 0.42 + (-0.55 * nx - 0.85 * ny) * 0.42 + grain(x // 2, y // 2, 6511 + seed) * 0.16
        tone = SK[3] if v > 0.74 else (SK[4] if v > 0.46 else (ST[1] if v > 0.26 else ST[2]))
        if (x + 1, y) not in pts or (x, y + 1) not in pts:
            if nx * 0.5 + ny * 0.86 > 0.25:
                tone = OCC
        _set(c, x, y, tone)
    # lichen crusts on its top and weather side, in lichen's base and shade
    for k in range(3):
        lx = int(cx - rx * 0.45 + hashed(seed, k, 6513) * rx * 0.9)
        ly = int(cy - ry * 0.55 + hashed(seed, k, 6515) * ry * 0.5)
        for (dx, dy, t) in ((0, 0, 2), (1, 0, 3), (0, 1, 3), (-1, 0, 3), (1, -1, 3)):
            if (lx + dx, ly + dy) in pts:
                _set(c, lx + dx, ly + dy, LI[t])
    # a hollow worn in its top, as a sarsen's are
    hx, hy = int(cx + rx * 0.15), int(cy - ry * 0.3)
    if (hx, hy) in pts and (hx + 1, hy) in pts:
        _set(c, hx, hy, ST[2])
        _set(c, hx + 1, hy, ST[1])
    bottom = {}
    for (x, y) in pts:
        bottom[x] = max(bottom.get(x, -99), y)
    for x in sorted(bottom):
        _set(c, x + 1, bottom[x] + 1, OCC)
        if hashed(x, seed, 6517) < 0.45:
            _set(c, x, bottom[x], VE[4] if x % 2 else LI[4])          # the turf grown up round its foot
            if hashed(x, seed, 6519) < 0.4:
                _set(c, x, bottom[x] - 1, LI[3] if x % 3 else VE[3])


def chalk_patch(c, cx, cy, rx, ry, seed, shaded=True, dim=False):
    """Bare chalk where the turf has gone: bone's deep, lumps in it catching the key in its shade stop, its
    lower side in steel's deep, the torn sod's edge dark along its top and the turf a pixel proud over it.
    `dim`: on the flank in shade, where the key reaches only the tops of the lumps."""
    pts = blob(cx, cy, rx, ry, seed, 0.25)
    for (x, y), (nx, ny) in pts.items():
        if _get(c, x, y) is None:
            continue
        tone = SK[4]
        if ny > (0.25 if dim else 0.6) or (shaded and nx > 0.7):
            tone = STL[4]
        lump = field(x, y, 6523, seed, 4)
        gx = field(x + 1, y, 6523, seed, 4) - field(x - 1, y, 6523, seed, 4)
        gy = field(x, y + 1, 6523, seed, 4) - field(x, y - 1, 6523, seed, 4)
        light = -(gx + gy) * 4.0
        if light > (0.8 if dim else 0.45) and tone == SK[4]:
            tone = SK[3]
        elif light < -0.6 and lump < 0.45:
            tone = ST[2]
        if (x, y - 1) not in pts and ny < 0:
            tone = OCC
        _set(c, x, y, tone)
    for (x, y), (nx, ny) in pts.items():
        if (x, y - 1) not in pts and ny < 0 and _get(c, x, y - 1) is not None:
            _set(c, x, y - 1, LI[3] if (x + y) % 3 else VE[3])
    return pts


def scar(c, cx, cy, rx, ry, seed):
    """Chalk bared on the flank, where the turf has come away: the scar is the slope itself, so it has no
    outline - bone's deep where it still faces up, sinking to steel's deep and dithering into the turf below,
    a lump or two catching the key - and only its top edge is drawn, the torn sod: a lit lip of turf over a
    line of occ where it overhangs."""
    pts = blob(cx, cy, rx, ry, seed, 0.3)
    for (x, y), (nx, ny) in pts.items():
        if _get(c, x, y) is None:
            continue
        th = (BAYER[y % 4][x % 4] + 0.5) / 16.0
        down = (ny + 1.0) / 2.0                     # 0 at the top of the scar, 1 at its foot
        if down > 0.7 and th < (down - 0.7) / 0.3:
            continue                                # the foot of the scar dithers into the turf
        tone = SK[4] if th > down * 0.9 else STL[4]
        lump = field(x, y, 6525, seed, 4)
        gx = field(x + 1, y, 6525, seed, 4) - field(x - 1, y, 6525, seed, 4)
        gy = field(x, y + 1, 6525, seed, 4) - field(x, y - 1, 6525, seed, 4)
        light = -(gx + gy) * 4.0
        if light > 0.85 and down < 0.6:
            tone = SK[3]
        elif light < -0.8 and lump < 0.4:
            tone = ST[2]
        _set(c, x, y, tone)
    for (x, y), (nx, ny) in pts.items():
        if (x, y - 1) not in pts and ny < 0.2 and _get(c, x, y) is not None:
            _set(c, x, y, OCC)
            if _get(c, x, y - 1) is not None:
                _set(c, x, y - 1, LI[3] if (x + y) % 3 else VE[3])


def flint_at(c, x, y, k, size=1.0):
    """A flint nodule, as terrain_downs.flint draws them: knobbly, black, the pale cortex rind lit on its upper
    left, its shadow down and to the right."""
    lobes = [(x, y, 1.2 * size), (x + 1.1 * size, y + 0.3, 0.9 * size)]
    if k % 2:
        lobes.append((x - 0.6, y + 0.9 * size, 0.8 * size))
    pts = set()
    for (lx, ly, rr) in lobes:
        for yy in range(int(ly - rr) - 1, int(ly + rr) + 2):
            for xx in range(int(lx - rr) - 1, int(lx + rr) + 2):
                if (xx + 0.5 - lx) ** 2 + (yy + 0.5 - ly) ** 2 <= rr * rr:
                    pts.add((xx, yy))
    knapped = k % 3 == 1
    for (xx, yy) in pts:
        ul = (xx - 1, yy) not in pts or (xx, yy - 1) not in pts
        lr = (xx + 1, yy) not in pts or (xx, yy + 1) not in pts
        if knapped and (xx - x) + (yy - y) > 0:
            # broken: the black glass inside, a facet of steel's shade where the key finds it
            tone = OCC if lr else (STL[3] if (xx + yy) % 3 == 0 else STL[4])
        else:
            # the cortex: the pale crust flint carries out of the chalk
            tone = SK[3] if ul else (ST[2] if lr else SK[4])
        _set(c, xx, yy, tone)
    for (xx, yy) in pts:
        if (xx, yy + 1) not in pts:
            _set(c, xx + 1, yy + 1, OCC)


def thorn_bush(c, x, y, seed, from_above=False, size=1.0):
    """A hawthorn seedling, a few years grown: a dense knot of small dark leaf clumps (verdant's shade and
    deep, never its lit) on a grey stem, lit on its upper left, its shadow down and to the right. No haws: a
    red berry on the downs is a warm fleck, and a warm fleck is a secret's tell."""
    cy = y if from_above else y - int(6 * size)
    clumps = []
    for k in range(7):
        a = hashed(seed, k, 6531) * 6.283
        d = hashed(seed, k, 6533) ** 0.7 * 5.5 * size
        clumps.append((x + math.cos(a) * d, cy + math.sin(a) * d * 0.8, (2.4 + hashed(seed, k, 6535) * 1.8) * size))
    clumps.sort(key=lambda q: q[1])
    if not from_above:
        for k in range(int(7 * size)):
            _set(c, x + (k // 4), y - k, LN[3] if k % 2 else LN[4])
    owned = set()
    for (qx, qy, r) in clumps:
        for py in range(int(qy - r) - 1, int(qy + r) + 2):
            for px in range(int(qx - r) - 1, int(qx + r) + 2):
                nx, ny = (px + 0.5 - qx) / r, (py + 0.5 - qy) / r
                d = nx * nx + ny * ny
                if d > 1.0 - hashed(px, py, 6537) * 0.3:
                    continue
                glob = -((px - x) + (py - cy)) / (8.0 * size)
                # each clump lit only on its upper-left rim, on the lit half of the bush; the body is the
                # deep stop with occ in it, so the bush is a dark knot of leaf and never a green leaf-shape
                rim = d > 0.45 and nx + ny < -0.7
                tone = VE[3] if (rim and glob > -0.1 and hashed(px, py, 6538) < 0.8) else VE[4]
                if hashed(px, py, 6539 + seed) < 0.18 or (glob < -0.4 and hashed(px, py, 6540) < 0.4):
                    tone = OCC
                if d > 0.6 and nx * 0.6 + ny * 0.8 > 0.5:
                    tone = OCC
                _set(c, px, py, tone)
                owned.add((px, py))
    for (px, py) in owned:
        if (px + 1, py + 1) not in owned and (px, py + 1) not in owned and hashed(px, py, 6541) < 0.6:
            if _get(c, px + 2, py + 2) is not None:
                _set(c, px + 2, py + 2, OCC)


def dress_kerb(c, seed):
    """A sarsen kerb-stone at the foot of the mound, half sunk where the people who raised it set it."""
    sarsen(c, 17 + hashed(seed, 1, 6601) * 10, FOOT - 6, 10.0, 7.0, seed)
    if hashed(seed, 2, 6603) < 0.7:
        sarsen(c, 35 + hashed(seed, 3, 6605) * 3, FOOT - 3, 5.5, 4.0, seed + 7)


def dress_rabbit(c, seed):
    """A rabbit hole in the flank: a dark mouth with its chalk lip, and its spoil of chalk spilt down the
    slope below it in a fan, the crumbs largest at the mouth."""
    cx, cy = 20 + int(hashed(seed, 1, 6611) * 7), 23
    # the mouth: a half-moon dug into the slope, wider than it is tall, flat along its floor
    mouth = dict((p, q) for (p, q) in blob(cx, cy, 6.0, 3.6, seed, 0.12).items() if q[1] < 0.55)
    for (x, y), (nx, ny) in mouth.items():
        _set(c, x, y, OCC if ny > -0.6 else LN[4])
    for (x, y) in mouth:
        if (x, y - 1) not in mouth:
            _set(c, x, y - 1, SK[4] if (x + y) % 3 else STL[4])     # the cut chalk over it
    # The spoil: crumbled chalk thrown out of the mouth and spread down the slope in a fan, wide from the
    # start, its surface lumpy (lit lumps, steel's deep between), thinning into crumbs at its edge.
    top_y, foot_y = cy + 3, FOOT - 1
    for y in range(top_y, foot_y + 1):
        t = (y - top_y) / float(foot_y - top_y)
        half = 2.5 + t * 13.0
        for x in range(int(cx - half) - 1, int(cx + half) + 2):
            u = abs(x + 0.5 - cx) / half
            keep = (1.0 if u < 0.35 else max(0.0, 1.0 - (u - 0.35) / 0.7)) * (0.95 - t * 0.35)
            if hashed(x, y, 6613 + seed) > keep:
                continue
            light = -((x - cx) * 0.4 + (y - cy) * 0.1) / 8.0 + (hashed(x // 2, y // 2, 6615 + seed) - 0.5) * 1.2
            tone = SK[3] if light > 0.45 else (SK[4] if light > -0.35 else STL[4])
            _set(c, x, y, tone)
    for k in range(5):
        x = cx + int((hashed(k, 1, 6617 + seed) - 0.5) * 18)
        y = foot_y - int(hashed(k, 2, 6617 + seed) * 6)
        _set(c, x, y, SK[3])
        _set(c, x + 1, y, SK[4])
        _set(c, x + 1, y + 1, OCC)


def dress_slip(c, seed):
    """The turf come away down the flank: a scar of bare chalk under the crest, the slumped sod lying in a
    roll at the foot below it."""
    cx = 21 + hashed(seed, 1, 6621) * 6
    scar(c, cx, 20, 12.5, 8.0, seed)
    sod = blob(cx + 1, FOOT - 4, 11.0, 4.0, seed + 3, 0.15)
    for (x, y), (nx, ny) in sod.items():
        if ny < -0.45:
            tone = LI[3] if (x + y) % 4 == 0 else (LI[4] if (x + y) % 2 else VE[4])
        else:
            tone = VE[4] if ny < 0.45 else OCC
        if ny < -0.3 and hashed(x, y, 6623) < 0.2:
            tone = SK[4]                       # chalk still clinging to the sod
        _set(c, x, y, tone)


def dress_capping(c, seed):
    """The barrow's old chalk capping breaking through under the crest: lumps of chalk in a ragged run, the
    turf torn back over them."""
    for k in range(3):
        scar(c, 10 + k * 13 + hashed(seed, k, 6631) * 4, 12 + hashed(seed, k, 6633) * 4,
             5.0 + hashed(seed, k, 6635) * 3.0, 3.2 + hashed(seed, k, 6637) * 1.2, seed + k * 5)


def dress_thorn(c, seed):
    thorn_bush(c, 22 + int(hashed(seed, 1, 6641) * 5), 36, seed, size=1.25)


def dress_flints(c, seed):
    """Flints the rain has washed out of the mound, lying in a spill at its foot."""
    for k in range(7):
        x = 8 + int(hashed(k, seed, 6651) * 32)
        y = FOOT - 5 + int(hashed(k, seed, 6653) * 5)
        flint_at(c, x, y, k, 1.2 + hashed(k, seed, 6655) * 0.9)


FACE_PAINTERS = {
    "kerb": dress_kerb,
    "rabbit": dress_rabbit,
    "slip": dress_slip,
    "capping": dress_capping,
    "thorn": dress_thorn,
    "flints": dress_flints,
}


def dress_top(c, dressing, seed, role):
    """A top's incident, seen from above. Kept inside the tile (a neighbour of any variant must not cut it),
    and on a side top, toward the open side."""
    x = 24 if role not in SIDE_ROLES else (17 if role == "edge_left" else 30)
    y = 24
    if dressing == "scrape":
        # a rabbit scrape: a hollow dug through the turf to the chalk, its spoil thrown out to one side
        pts = chalk_patch(c, x, y, 7.0, 4.5, seed)
        for (px, py), (nx, ny) in pts.items():
            if nx * nx + ny * ny < 0.3:
                _set(c, px, py, OCC if ny < 0.1 else STL[4])
        for k in range(14):
            t = hashed(k, seed, 6701)
            px = x + 5 + int(t * 8)
            py = y - 3 + int(hashed(k, seed, 6703) * 7)
            _set(c, px, py, SK[4] if k % 3 else SK[3])
            _set(c, px + 1, py + 1, STL[4])
    elif dressing == "thorn":
        thorn_bush(c, x, y, seed, from_above=True)
    elif dressing == "flints":
        for k in range(5):
            flint_at(c, x - 9 + int(hashed(k, seed, 6711) * 18), y - 7 + int(hashed(k, seed, 6713) * 14), k,
                     1.1 + hashed(k, seed, 6715) * 0.5)


# --- the tile -------------------------------------------------------------------------------------------------

class _Shift(jw._Shift):
    """The jail's view squeezed into the rows below `oy`, for a stub's short face."""
    pass


def paint(c, role, dressing, plain_index, sides, inner):
    """Draw one barrow tile. `sides` are the sides with floor beyond them (t, b, l, r); `inner` is the
    floored diagonal of an inner corner, as tiles.INNER gives it."""
    seed = seed_for(dressing, plain_index)
    if role == "patch":
        # A lone stub (only ever left by the spawn clearing): a top above, a short face below.
        top(c, seed, ("t", "l", "r"), None)
        f = _Shift(c, 22)
        face(f, seed, ("b", "l", "r"))
        return
    if "b" in sides:
        face(c, seed, sides)
        if dressing in FACE_PAINTERS:
            FACE_PAINTERS[dressing](c, seed)
        return
    top(c, seed, sides, inner)
    if dressing:
        dress_top(c, dressing, seed, role)
