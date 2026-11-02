# -*- coding: utf-8 -*-
"""The Cursed Dungeon's stair foot (.ai/endgame-castle.md 4.1). Run: python tools/art/props_dungeon.py

Writes assets/bonelight/world/dungeon/stair.png - authored 1:1 and saved at x2 like every world sprite -
and stair.json beside it: where its foot and its back stand in pixels of the PNG, and the parts of it
that block, in world pixels from the foot. DungeonRules (scripts/DungeonRules.cs) stands it in the spawn
clearing, just north of where the wizard appears.

WHY IT EXISTS. Since piece C2 the Cursed Dungeon is the cells under his castle, entered from the bridge's
gate: "the transition is the bridge's gate and the stair at the start" (4.2). The walls stay the jail's
(jail_walls.py); this is the only new art the move asked for. It is the way he came down - a flight of
cut stone rising north out of the clearing between two cheek walls into an arch in a taller back wall,
and the arch is dark: the stair goes on up into it, a few treads you can just see and then nothing.

THE SHAPE, in three-quarter view (a point on the ground at height z is drawn at y - z; only faces turned
toward the viewer are drawn, as in props_bridge.py):

  back wall    across the top, 40 tall: its cap, then its south face in cut courses, the arch in it.
  the flight   five steps of 4 rise and 6 going, the top tread at the arch's sill.
  cheek walls  either side, 24 tall - the maze wall's height, one tile - cap along their length and a
               face at their south end, where the flight meets the floor.

THE LIGHT IS THE JAIL'S. Key upper left: tops lit, south faces a step down, east edges in occ, the
left cheek's shadow on the treads beside it. The lowest tread is the floor's own value (the flagstone's
`base`) and each one up is a step darker, into the arch. Two sconces burn his cold light either side of
the arch - the arcane row round a white core, exactly the jail's (jail_walls.cold_flame) - so the first
thing the chapter shows is whose house it is. Nothing warm, nothing in an XP orb's colours, nothing that
animates; every colour is from the contract (audited below).
"""
from __future__ import division, print_function

import io
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import bonelight as bl
import raster

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "dungeon")
SCALE = 2


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


ST = ramp("stone")
STL = ramp("steel")
AR = ramp("arcane")
SK = ramp("skin")
LI = ramp("lichen")

BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]

# --- the geometry, in art pixels on the ground -------------------------------------------------------
W = 64            # across: 128 world px, a little under three tiles
CHEEK = 9         # each cheek wall's width
BACK_DEPTH = 6    # the back wall's thickness (its cap, seen from above)
BACK_H = 40       # the back wall's height
CHEEK_H = 24      # the cheek walls' height: one tile, the maze wall's
STEPS = 5
GOING = 6         # a tread's depth
RISE = 4          # a riser's height
FOOT = BACK_DEPTH + STEPS * GOING      # the ground line of the bottom riser: 36
ZOFF = BACK_H                          # image row = ground y - z + ZOFF
H = FOOT + ZOFF + 2                    # two rows under the foot for the contact shadow
ARCH_X0, ARCH_X1 = 22, 41              # the arch's jambs, inclusive
ARCH_SILL = STEPS * RISE               # it opens at the top tread's height
ARCH_TOP = 36                          # and rises to here, round-headed


def row(ground_y, z):
    return ground_y - z + ZOFF


def dither(x, y, t):
    """True on a fraction t of pixels, in the 4x4 Bayer order."""
    return (BAYER[y % 4][x % 4] + 0.5) / 16.0 < t


def hashed(x, y, salt=0):
    h = (x * 73856093) ^ (y * 19349663) ^ (salt * 83492791)
    h &= 0x7FFFFFFF
    return ((h >> 7) % 10007) / 10007.0


def courses(c, x0, x1, y0, y1, height, salt):
    """A south face of cut stone between rows y0 and y1: courses `height` tall in occ mortar, the joints
    staggered course by course, a lit arris along the top and a chip here and there."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            c.set(x, y, ST[2])
    course = 0
    for top in range(y0, y1 + 1, height):
        bottom = min(y1, top + height - 1)
        if top > y0:
            c.hline(x0, x1, top, OCC)
            c.hline(x0, x1, top + 1, ST[3])
        offset = 3 + (course * 7 + salt * 5) % 9
        for x in range(x0 + offset, x1, 11):
            for y in range(top + (1 if top > y0 else 0), bottom + 1):
                c.set(x, y, OCC)
            # the stone right of a joint catches the key on its edge
            c.set(x + 1, top + 2 if top > y0 else top + 1, ST[1])
        # wear: the odd lighter stone and the odd dark pit
        for x in range(x0, x1 + 1):
            for y in range(top + 2, bottom + 1):
                v = hashed(x, y, salt + course)
                if v < 0.04:
                    c.set(x, y, ST[3])
                elif v > 0.975:
                    c.set(x, y, ST[1])
        course += 1
    c.hline(x0, x1, y0, ST[1])


def cap(c, x0, x1, y0, y1, salt, lit_west=True):
    """The top of a wall from above: rough cobbles in the deep end of the stone row, a lit bevel on the
    sides that face the key (north, west), occ on the side away from it (east)."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            v = hashed(x // 2, y // 2, salt)
            c.set(x, y, ST[3] if v < 0.62 else ST[4])
            if hashed(x, y, salt + 9) < 0.06:
                c.set(x, y, OCC)
    c.hline(x0, x1, y0, ST[2])
    if lit_west:
        c.vline(x0, y0, y1, ST[2])
    c.vline(x1, y0, y1, OCC)


def sconce(c, cx, cup_y):
    """His fire on an iron bracket: the light pooled on the stone round it, then the iron, then the flame -
    the jail's sconce at this cell's scale."""
    # the pool: arcane's shade and deep dithered into the stone, densest at the flame
    for y in range(cup_y - 9, cup_y + 6):
        for x in range(cx - 6, cx + 8):
            d = ((x - cx - 0.5) ** 2) / 36.0 + ((y - cup_y + 3) ** 2) / 56.0
            if d >= 1.0 or c.get(x, y)[:3] == raster.rgb(OCC)[:3]:
                continue
            if dither(x, y, (1.0 - d) * 0.55):
                c.set(x, y, AR[3] if d < 0.45 else AR[4])
    # the bracket: a plate, a stem, a cup lit on its left
    c.rect(cx - 1, cup_y + 3, cx + 2, cup_y + 5, STL[3])
    c.set(cx - 1, cup_y + 3, STL[2])
    c.hline(cx - 1, cx + 3, cup_y + 6, OCC)
    c.set(cx, cup_y + 2, STL[2])
    c.set(cx + 1, cup_y + 2, STL[3])
    c.hline(cx - 2, cx + 3, cup_y + 1, STL[3])
    c.hline(cx - 2, cx + 3, cup_y, STL[2])
    c.set(cx - 2, cup_y, STL[1])
    c.set(cx + 3, cup_y + 1, STL[4])
    c.set(cx + 4, cup_y + 1, OCC)
    # the flame: a teardrop leaning, white core
    flame = [(0, 0), (0, 0), (1, 0), (1, 1), (2, 1), (2, 1), (1, 0)]
    tip = cup_y - len(flame)
    for k, (half, core) in enumerate(flame):
        y = tip + k
        lean = 1 if k < 3 else 0
        for x in range(cx - half + lean, cx + half + lean + 2):
            edge = x in (cx - half + lean, cx + half + lean + 1)
            c.set(x, y, AR[2] if edge else AR[1])
        if k >= 3:
            for x in range(cx - core + lean + 1, cx + core + lean + 1):
                c.set(x, y, AR[0])


def tread_tone(i):
    """Tread i (1 the lowest): a top, so lit - a step over the floor's base at the bottom - and a step
    darker each two up, into the arch."""
    return [ST[1], ST[1], ST[2], ST[2], ST[3]][i - 1]


def riser_tone(i):
    """A south face: a step under its tread, always, so every step reads as a step."""
    return [ST[3], ST[3], ST[3], ST[4], ST[4]][i - 1]


def nosing_tone(i):
    return [ST[0], ST[0], ST[1], ST[1], ST[2]][i - 1]


def draw():
    c = raster.Canvas(W, H)
    left = (0, CHEEK - 1)
    right = (W - CHEEK, W - 1)
    flight = (CHEEK, W - CHEEK - 1)

    # 1. The back wall: its cap, then its south face from its top down to the ground (the flight and the
    # cheeks are drawn over the lower part).
    cap(c, 0, W - 1, row(0, BACK_H), row(BACK_DEPTH - 1, BACK_H), salt=3)
    face_top = row(BACK_DEPTH, BACK_H)
    face_bottom = row(BACK_DEPTH, 0) - 1
    courses(c, 0, W - 1, face_top, face_bottom, 6, salt=1)
    c.hline(0, W - 1, face_top, ST[1])
    # the coping's shadow on the face
    c.hline(0, W - 1, face_top + 1, ST[3])

    # 2. The arch: dark, the flight going on up into it, its jambs and voussoirs cut stone.
    sill = row(BACK_DEPTH, ARCH_SILL)
    top = row(BACK_DEPTH, ARCH_TOP)
    radius = (ARCH_X1 - ARCH_X0 + 1) / 2.0
    cx = (ARCH_X0 + ARCH_X1 + 1) / 2.0
    spring = top + int(radius * 0.7)
    for y in range(top, sill):
        for x in range(ARCH_X0, ARCH_X1 + 1):
            if y < spring:
                dy = (spring - y) / (radius * 0.7)
                dx = (x + 0.5 - cx) / radius
                if dx * dx + dy * dy > 1.0:
                    continue
            c.set(x, y, OCC)
    # the treads beyond the sill, just seen: two lines of deep stone, narrowing into the dark
    for k, y in enumerate((sill - 3, sill - 6)):
        inset = 2 + k * 3
        c.hline(ARCH_X0 + inset, ARCH_X1 - inset, y, ST[4] if k == 0 else ST[4])
        if k == 0:
            c.hline(ARCH_X0 + inset + 1, ARCH_X1 - inset - 1, y - 1, ST[4])
    # voussoirs: a ring of lit stone round the head, a keystone, jambs lit on the left
    for y in range(top - 2, sill):
        for x in range(ARCH_X0 - 2, ARCH_X1 + 3):
            if c.get(x, y)[:3] == raster.rgb(OCC)[:3]:
                continue
            near = any(0 <= x + dx < W and c.get(x + dx, y + dy)[:3] == raster.rgb(OCC)[:3]
                       and top <= y + dy < sill and ARCH_X0 <= x + dx <= ARCH_X1
                       for dx in (-2, -1, 0, 1, 2) for dy in (-1, 0, 1, 2))
            if near:
                c.set(x, y, ST[0] if x < cx - 3 else (ST[1] if x < cx + 3 else ST[2]))
    for y in range(top - 3, top + 1):
        for x in range(int(cx) - 2, int(cx) + 2):
            c.set(x, y, ST[0] if y < top - 1 else ST[1])
    c.vline(int(cx) + 2, top - 3, top, OCC)
    c.vline(int(cx) - 3, top - 3, top, OCC)
    # the ring's joints, radiating from the head's centre
    for (jx, jy) in ((ARCH_X0 + 1, spring - 2), (ARCH_X0 + 4, top + 1), (ARCH_X1 - 4, top + 1), (ARCH_X1 - 1, spring - 2)):
        ox = -1 if jx < cx else 1
        c.set(jx + ox, jy - 1, OCC)
        c.set(jx + 2 * ox, jy - 2, OCC)
    for y in range(spring, sill):
        c.set(ARCH_X0 - 1, y, ST[0] if y < sill - 1 else ST[1])
        c.set(ARCH_X1 + 1, y, ST[3])

    # 3. The flight, from the top tread down, so each riser covers the tread behind it.
    for i in range(STEPS, 0, -1):
        g0 = FOOT - i * GOING              # the tread's back
        g1 = FOOT - (i - 1) * GOING        # the riser's ground line
        z = i * RISE
        t0, t1 = row(g0, z), row(g1, z) - 1
        tone = tread_tone(i)
        for y in range(t0, t1 + 1):
            for x in range(flight[0], flight[1] + 1):
                c.set(x, y, tone)
                if hashed(x, y, i) < 0.03:
                    c.set(x, y, ST[4] if tone != ST[4] else OCC)
        # the nosing: the front edge of the tread, where the key catches it
        for x in range(flight[0], flight[1] + 1):
            c.set(x, t1, nosing_tone(i) if hashed(x, i, 7) > 0.08 else tone)
        # the left cheek's shadow across the tread beside it
        for y in range(t0, t1 + 1):
            for x in range(flight[0], flight[0] + 3):
                c.set(x, y, OCC if x == flight[0] else (ST[4] if dither(x, y, 0.6) else tone))
        # the riser under it, a step darker, its joints staggered, occ where it meets the tread below
        r0, r1 = row(g1, z), row(g1, z - RISE) - 1
        rtone = riser_tone(i)
        for y in range(r0, r1 + 1):
            for x in range(flight[0], flight[1] + 1):
                c.set(x, y, rtone)
        for x in range(flight[0] + 5 + (i * 7) % 9, flight[1], 13):
            c.vline(x, r0, r1, OCC)
        c.hline(flight[0], flight[1], r1, OCC)
        for x in range(flight[0], flight[0] + 2):
            c.vline(x, r0, r1, OCC)

    # a crack across the second riser and a chip off the bottom nosing
    r0 = row(FOOT - GOING, 2 * RISE)
    for k in range(6):
        c.set(40 + k, r0 + (k // 2), OCC)
    c.set(18, row(FOOT, RISE) - 1, ST[3])
    c.set(19, row(FOOT, RISE) - 1, ST[3])
    # a bone or two kicked against the bottom riser, never the bone row's brightest stop
    by = row(FOOT, 0) - 2
    for x in range(44, 49):
        c.set(x, by, SK[2])
    c.set(43, by - 1, SK[2])
    c.set(49, by - 1, SK[2])
    c.set(44, by + 1, OCC)
    c.hline(45, 48, by + 1, SK[3])
    c.set(24, by, SK[3])
    c.set(25, by, SK[2])
    # a seep down the right of the back wall, lichen on the wet stone
    for y in range(face_top + 4, face_top + 14):
        if dither(W - CHEEK - 3, y, 0.7):
            c.set(W - CHEEK - 3, y, LI[3])
        if dither(W - CHEEK - 4, y, 0.35):
            c.set(W - CHEEK - 4, y, LI[4])

    # 4. The cheek walls: cap along their length, then the face at their south end.
    for (x0, x1), salt in ((left, 11), (right, 13)):
        cap(c, x0, x1, row(0, CHEEK_H), row(FOOT - 1, CHEEK_H), salt=salt)
        courses(c, x0, x1, row(FOOT, CHEEK_H), row(FOOT, 0) - 1, 6, salt=salt)
        c.hline(x0, x1, row(FOOT, CHEEK_H), ST[1])
        c.vline(x1, row(FOOT, CHEEK_H), row(FOOT, 0) - 1, ST[3])
    # the left cheek's west edge catches the key all the way down
    c.vline(0, row(FOOT, CHEEK_H), row(FOOT, 0) - 1, ST[1])
    # the cheeks meet the back wall: its face shows above them, in occ where they abut it
    c.hline(0, CHEEK - 1, row(0, CHEEK_H) - 1, OCC)
    c.hline(W - CHEEK, W - 1, row(0, CHEEK_H) - 1, OCC)
    # an iron ring with a length of chain on the right cheek's face
    rx, ry = W - CHEEK + 3, row(FOOT, 0) - 15
    for (dx, dy) in ((0, 0), (1, 0), (2, 1), (2, 2), (1, 3), (0, 3), (-1, 2), (-1, 1)):
        c.set(rx + dx, ry + dy, STL[2] if dx <= 0 else STL[3])
    for k in range(4):
        c.set(rx + 1, ry + 4 + k, STL[3] if k % 2 == 0 else STL[4])

    # 5. His light either side of the arch.
    sconce(c, ARCH_X0 - 8, row(BACK_DEPTH, 27))
    sconce(c, ARCH_X1 + 7, row(BACK_DEPTH, 27))

    # 6. The contact line at the foot of everything, and a soft shadow under it on the floor.
    foot = row(FOOT, 0)
    c.hline(0, W - 1, foot, OCC)
    for x in range(W):
        if dither(x, foot + 1, 0.55):
            c.set(x, foot + 1, OCC)
    return c


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    c = draw()
    bad = c.audit()
    if bad:
        print("props_dungeon: colours outside the contract: %s" % ", ".join("%s x%d" % kv for kv in bad))
        return 1
    big = c.scaled(SCALE)
    big.save(os.path.join(OUT, "stair.png"))

    foot_x, foot_y = W // 2, row(FOOT, 0)
    # What blocks, in world px from the foot (x right, y down; the foot is the middle of the bottom
    # riser's ground line): the back wall's footprint and each cheek's. The flight itself is walked on.
    def rect(x0, g0, x1, g1):
        return [(x0 - foot_x) * SCALE, (g0 - FOOT) * SCALE, (x1 - x0 + 1) * SCALE, (g1 - g0 + 1) * SCALE]

    manifest = {
        "_comment": "Generated by tools/art/props_dungeon.py - the Cursed Dungeon's stair foot. foot is the "
                    "middle of the bottom riser's ground line and back the back wall's ground line, in pixels "
                    "of the PNG; blocks are [x, y, w, h] in world pixels from the foot.",
        "texture": "res://assets/bonelight/world/dungeon/stair.png",
        "width": W * SCALE,
        "height": H * SCALE,
        "foot": [foot_x * SCALE, foot_y * SCALE],
        "back": row(BACK_DEPTH, 0) * SCALE,
        "blocks": [
            rect(0, 0, W - 1, BACK_DEPTH - 1),
            rect(0, BACK_DEPTH, CHEEK - 1, FOOT - 1),
            rect(W - CHEEK, BACK_DEPTH, W - 1, FOOT - 1),
        ],
    }
    text = json.dumps(manifest, indent=2, sort_keys=True, separators=(",", ": "))
    with io.open(os.path.join(OUT, "stair.json"), "w", encoding="utf-8", newline="\n") as f:
        f.write(text.decode("utf-8") if isinstance(text, bytes) else u"%s" % text)
        f.write(u"\n")
    print("dungeon/stair %dx%d on screen, foot (%d, %d), back row %d"
          % (W * SCALE, H * SCALE, foot_x * SCALE, foot_y * SCALE, row(BACK_DEPTH, 0) * SCALE))
    return 0


if __name__ == "__main__":
    sys.exit(main())
