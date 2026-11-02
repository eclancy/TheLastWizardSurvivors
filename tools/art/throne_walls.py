# -*- coding: utf-8 -*-
"""The Throne's walls (.ai/endgame-castle.md 6.1). Drawn by tools/art/tiles.py (tiles.wall_set_modules) as
the `throne_wall` terrain, which the Throne's StageEnvironmentProfile.WallSet names:

    python tools/art/tiles.py throne_wall     # redraw only these while iterating
    python tools/art/tiles.py                  # the full run that writes tiles.json

THE ONE ROOM THAT IS HIS. Everything jail_walls.py says about how a wall tile knows what it is holds
here unchanged - two tiles thick, the autotiler's role saying which sides are floor, and in three-quarter
view floor to the SOUTH makes the wall's FACE and anything else its CAP. The same fourteen roles, the
same three stubs, the same plain-and-dressed variants, and the same three values in one order: the
floor the lightest thing, the face a step under it, the cap the darkest. In this hall only the north
wall and the eight pillars show a face (throne_room.py), so the face is where the room is said.

    the face   a coping with its lit arris; a string course; great dressed blocks, two courses of
               them, larger than any wall on the road (somebody built this to be looked up at); a
               plinth course at the foot. Unlit stone: the stone row's darker half throughout.
    the cap    the Court's dressed slabs (court_walls.cap): his house is one house.

HIS LIGHT, AND ONLY HIS. The one place in the game the `darkness` ramp is spent as emission
(.ai/world-and-tone.md: "Holding that back across eight chapters is what makes it land"). The sconces
here burn with it - a flame of the darkness row round its pale core - and the light they throw on the
stone is the darkness row too, dithered by distance, so the room's light is violet-black and the
player's own light is the only warm thing in it. Every sconce in the hall is lit: it is his, and he
is home. Nothing else on these walls glows.

WHAT ELSE HANGS HERE. The household's banners, long and red with no device (his house has no arms but
him), and empty niches. No frames, no tapestries, no shields, no chains: an audience hall is bare so
that what sits at its end is the only thing to look at.

Nothing on a wall is warm (the player's light, a secret's glint), nothing is an XP orb's colour, nothing
animates, and no dressing is drawn as a way through.
"""

import math

import bonelight as bl
import court_walls as CW
import jail_walls as J

# The tiles.json terrain this set is drawn under (tiles.wall_set_modules reads TERRAIN).
TERRAIN = "throne_wall"

OCC = bl.OCC
SIZE = 48

ST = J.ST
STL = J.STL
GD = J.ramp("gold")
RD = J.ramp("red")
# His light: core, hot, mid, edge (bonelight.ELEMENTS).
DK = list(bl.ELEMENTS["darkness"])

hashed = J.hashed
_set = J._set
_get = J._get

# The roles and stubs are the jail's, so the painter asks for the same keys of either set.
SIDE_ROLES = J.SIDE_ROLES
STUB_SIDES = J.STUB_SIDES
PLAIN_VARIANTS = J.PLAIN_VARIANTS

FACE_DRESSINGS = [
    ("sconce", 5),        # his dark light, pooled on the stone round it
    ("banner", 3),        # the household's banner, red, with no device
    ("niche", 1),         # an empty arched niche
    ("crack", 1),         # a crack through one block
]
# The side walls show only their tops; in this hall they carry nothing.
SIDE_DRESSINGS = []


def variants(role):
    """[(dressing or None, plain index, weight)] for a role, plain first, in manifest order."""
    plain = PLAIN_VARIANTS.get(role, 2)
    out = [(None, k, 1) for k in range(plain)]
    if role == "edge_bottom":
        out += [(d, 0, w) for d, w in FACE_DRESSINGS]
    elif role in SIDE_ROLES:
        out += [(d, 0, w) for d, w in SIDE_DRESSINGS]
    return out


# --- the face -------------------------------------------------------------------------------------
#
# Rows, fixed in every variant so a face is one wall whatever stands beside it:
COPING_SHADOW = 4
STRING_TOP = 5            # the string course, a moulding under the coping
BLOCK_TOPS = (9, 25)      # two courses of great blocks, 16 rows each
BLOCK_H = 16
PLINTH = 41               # the plinth course at the foot
FOOT = 44
BLOCK_W = 24              # two blocks a tile, offset half a block course to course


def face(c, seed):
    # The coping, as the jail's: the lit arris is what says "the top edge of something tall".
    for x in range(SIZE):
        _set(c, x, 0, ST[1])
        _set(c, x, 1, ST[1] if hashed(x // 3, 1, 6099) < 0.3 else ST[2])
        _set(c, x, 2, ST[2])
        _set(c, x, 3, ST[3])
        _set(c, x, COPING_SHADOW, OCC)
    # The string course: a rounded moulding, lit on its upper lip, its shadow under it.
    for x in range(SIZE):
        _set(c, x, STRING_TOP, ST[2])
        _set(c, x, STRING_TOP + 1, ST[3] if x % 8 else ST[2])
        _set(c, x, STRING_TOP + 2, ST[4])
        _set(c, x, STRING_TOP + 3, OCC)
    # The great blocks: each lit along its top and its left joint, the field in the stone's shade, the
    # joints `occ`. A few hairline flaws in the field, the variant's own, inside the tile.
    for ci, top in enumerate(BLOCK_TOPS):
        off = BLOCK_W // 2 if ci % 2 else 0
        for y in range(top, top + BLOCK_H):
            ly = y - top
            for x in range(SIZE):
                lx = (x + off) % BLOCK_W
                if ly == BLOCK_H - 1 or lx == BLOCK_W - 1:
                    tone = OCC
                elif ly == 0:
                    tone = ST[2]
                elif lx == 0:
                    tone = ST[2] if ly < BLOCK_H // 2 else ST[3]
                elif ly == BLOCK_H - 2:
                    tone = ST[4]
                else:
                    tone = ST[3]
                _set(c, x, y, tone)
        # Tooling on the field: a few stippled stops, so a block is dressed stone and not paint.
        for k in range(10):
            sx = int(hashed(k, ci, 6110 + seed) * SIZE)
            sy = top + 2 + int(hashed(k, ci, 6111 + seed) * (BLOCK_H - 5))
            if _get(c, sx, sy) == ST[3]:
                _set(c, sx, sy, ST[4] if hashed(k, ci, 6112 + seed) < 0.6 else ST[2])
    # The plinth course: a projecting base, lit on its top, then the contact line the wall stands on.
    for x in range(SIZE):
        _set(c, x, PLINTH, ST[2])
        _set(c, x, PLINTH + 1, ST[3])
        _set(c, x, PLINTH + 2, ST[4])
        _set(c, x, FOOT, OCC)
        for y in range(FOOT + 1, SIZE):
            _set(c, x, y, OCC)
    # Wear on some variants: a chip out of the coping, a spalled corner of a block.
    if hashed(seed, 3, 6132) < 0.4:
        px = 6 + int(hashed(seed, 4, 6133) * 34)
        _set(c, px, 1, OCC)
        _set(c, px + 1, 1, OCC)
        _set(c, px + 1, 2, ST[3])
    if hashed(seed, 5, 6134) < 0.4:
        px = 4 + int(hashed(seed, 6, 6135) * 38)
        py = BLOCK_TOPS[1] + BLOCK_H - 4
        _set(c, px, py, OCC)
        _set(c, px + 1, py, ST[4])
        _set(c, px, py - 1, ST[4])


def face_end_left(c):
    # The west end of a face: its corner catches the key light down to the foot.
    for y in range(0, FOOT):
        _set(c, 0, y, ST[1])
        if _get(c, 1, y) in (ST[3], ST[4]):
            _set(c, 1, y, ST[2])


def face_end_right(c):
    for y in range(0, SIZE):
        _set(c, SIZE - 1, y, OCC)
        if _get(c, SIZE - 2, y) in (ST[1], ST[2], ST[3]):
            _set(c, SIZE - 2, y, ST[4])


# --- his light ------------------------------------------------------------------------------------

# The stone's tones shifted into the darkness row round a flame: near, the mid and edge stops; far,
# the edge alone. Only stone is touched - the flame and the iron drawn after it are not.
NEAR = {ST[4]: DK[3], ST[3]: DK[3], ST[2]: DK[2], ST[1]: DK[2], ST[0]: DK[1], OCC: ST[4]}
FAR = {ST[4]: ST[3], ST[3]: DK[3], ST[2]: DK[3], ST[1]: DK[2]}


def light_pool(c, cx, cy, r):
    for y in range(int(cy - r), int(cy + r) + 1):
        for x in range(int(cx - r), int(cx + r) + 1):
            d = math.hypot(x - cx, (y - cy) * 0.9) / float(r)
            if d >= 1.0:
                continue
            here = _get(c, x, y)
            if here is None:
                continue
            th = (J.BAYER[y % 4][x % 4] + 0.5) / 16.0
            if d < 0.55 and th < 1.0 - d * 1.2:
                if here in NEAR:
                    _set(c, x, y, NEAR[here])
            elif th < (1.0 - d) * 0.95:
                if here in FAR:
                    _set(c, x, y, FAR[here])


def dark_flame(c, cx, base_y):
    """His light: the darkness row round its pale core. A teardrop 9 x 15 like the jail's cold fire,
    but upright - nothing in this room leans."""
    rows = [(0, -1), (0, -1), (1, -1), (1, -1), (1, 0), (2, 0), (2, 0), (3, 1), (3, 1),
            (4, 1), (4, 2), (4, 2), (4, 1), (3, 1), (2, -1)]
    tip = base_y - len(rows) + 1
    for k, (half, core) in enumerate(rows):
        y = tip + k
        for x in range(cx - half, cx + half + 2):
            edge = x in (cx - half, cx + half + 1)
            _set(c, x, y, DK[2] if edge else DK[1])
        if core >= 0:
            for x in range(cx - core, cx + core + 2):
                _set(c, x, y, DK[0])
    # The outermost skin: the edge stop, so the flame stands off the violet pool behind it.
    for k, (half, _) in enumerate(rows):
        y = tip + k
        for x in (cx - half - 1, cx + half + 2):
            if _get(c, x, y) not in (DK[0], DK[1], DK[2]):
                _set(c, x, y, DK[3])


def dress_sconce(c, cx=23, cup_y=22):
    # Kept under the string course: light spilling over the coping would read as a lamp beyond the wall.
    light_pool(c, cx + 1, cup_y - 3, 17)
    J.sconce_body(c, cx, cup_y)
    dark_flame(c, cx, cup_y - 2)


def dress_banner(c):
    """The household's banner: red cloth from a gold rod under the string course, a swallowtail at its
    foot, lit on its left edge and folded once down its length. No device: his house has no arms but
    him."""
    x0, x1, top, foot = 16, 31, STRING_TOP + 4, 38
    for x in range(x0 - 2, x1 + 3):
        _set(c, x, top - 1, GD[3])
        _set(c, x, top, OCC)
    _set(c, x0 - 2, top - 1, GD[2])
    _set(c, x1 + 2, top - 1, GD[4])
    fold = (x0 + x1) // 2
    for y in range(top + 1, foot + 1):
        for x in range(x0, x1 + 1):
            tone = RD[3]
            if x == x0:
                tone = RD[2]
            elif x == x1:
                tone = RD[4]
            elif x == fold + 1:
                tone = RD[4]
            _set(c, x, y, tone)
    # The swallowtail: a notch up the middle of the foot, the wall's stone showing through it.
    for k in range(5):
        for x in range(fold - (4 - k), fold + (5 - k)):
            _set(c, x, foot - k, ST[3])
    for x in list(range(x0, fold - 4)) + list(range(fold + 5, x1 + 1)):
        _set(c, x, foot + 1, GD[4] if x % 2 else OCC)
    # Its shadow on the wall to its right.
    for y in range(top + 2, foot + 1):
        _set(c, x1 + 1, y, OCC)


def dress_niche(c):
    """An arched niche cut into the blocks, empty: whatever stood in it was taken."""
    x0, x1, y1 = 16, 31, PLINTH - 3
    mid = (x0 + x1) / 2.0
    half = (x1 - x0) / 2.0
    for x in range(x0 - 2, x1 + 3):
        dx = (x - mid) / (half + 2)
        if abs(dx) > 1.0:
            continue
        top = int(round(BLOCK_TOPS[0] + 8 - math.sqrt(1.0 - dx * dx) * 6))
        _set(c, x, top - 1, ST[2])
        for y in range(top, y1 + 1):
            inner = x0 <= x <= x1 and y > top
            _set(c, x, y, (ST[4] if x > mid else OCC) if inner else ST[3])
    for x in range(x0 - 2, x1 + 3):
        _set(c, x, y1 + 1, ST[2])
        _set(c, x, y1 + 2, ST[4])


def dress_crack(c):
    x, y = 30, BLOCK_TOPS[0] + 2
    for k in range(BLOCK_H * 2 - 4):
        x += 1 if hashed(k, 1, 6150) < 0.35 else (-1 if hashed(k, 2, 6151) < 0.4 else 0)
        _set(c, x, y + k, OCC)
        if hashed(k, 3, 6152) < 0.3:
            _set(c, x - 1, y + k, ST[2])


FACE_PAINTERS = {
    "sconce": dress_sconce,
    "banner": dress_banner,
    "niche": dress_niche,
    "crack": dress_crack,
}


# --- the tile -------------------------------------------------------------------------------------

def seed_for(dressing, plain_index):
    names = [d for d, _ in FACE_DRESSINGS]
    return 59 + plain_index * 17 + (names.index(dressing) * 11 + 3 if dressing in names else 0)


def paint(c, role, dressing, plain_index, sides, inner):
    """Draw one wall tile; the arguments are jail_walls.paint's."""
    seed = seed_for(dressing, plain_index)
    if role == "patch":
        CW.cap(c, seed)
        J.rim_top(c)
        J.rim_left(c)
        J.rim_right(c)
        f = J._Shift(c, 18)
        face(f, seed)
        face_end_left(f)
        face_end_right(f)
        return

    if "b" in sides:
        face(c, seed)
        if dressing in FACE_PAINTERS:
            FACE_PAINTERS[dressing](c)
        if "l" in sides:
            face_end_left(c)
        if "r" in sides:
            face_end_right(c)
        return

    CW.cap(c, seed)
    if "t" in sides:
        J.rim_top(c)
    if "l" in sides:
        J.rim_left(c)
    if "r" in sides:
        J.rim_right(c)
    if inner:
        v, h = inner
        if (v, h) == ("t", "l"):
            for k in range(3):
                _set(c, k, 0, ST[1])
                _set(c, 0, k, ST[1])
            _set(c, 1, 1, ST[2])
        elif (v, h) == ("t", "r"):
            for k in range(3):
                _set(c, SIZE - 1, k, OCC)
                _set(c, SIZE - 2, k, OCC)
        elif (v, h) == ("b", "l"):
            J.rim_left(c)
        elif (v, h) == ("b", "r"):
            J.rim_right(c)
