# -*- coding: utf-8 -*-
"""The gaol's lantern (.ai/unlock-puzzles.md 3.8). Run: python tools/art/gaol_lantern.py

Writes assets/bonelight/world/dungeon/gaol-lantern.png - authored 1:1 on a 12x18 cell and saved at x2 like every world
sprite, four frames in a row: the lantern FLARING (frame 0, while a bearer's light has just fallen on him with the key,
LanternBearer.Turn) and three frames of its flame's ordinary flicker. LanternBearer draws it at the bearer's hand on the
light layer (DrawLayers.Air), so the lantern is never darkened by the Cursed Dungeon's dark (.ai/light-pool.md): it is the
point the cone of light on the floor comes from.

WHAT IT IS. A jailer's hand lantern: a ring to carry it by, a peaked steel cap, a cage of four bars round panes of glass
with the flame inside, a steel foot. The flame is HIS - the arcane row round a white core, exactly the jail's sconces'
(jail_walls.cold_flame, props_dungeon.sconce) - because the bearers are his jailers and the light they throw is the cold
light that takes the key (.ai/world-and-tone.md: the only warm light is the wizard's). The glass is the flame's light
through it, the arcane row's deep stops, so the panes read lit from the inside; the bars stand dark in front of it.

Nothing warm, nothing in an XP orb's colours. Every colour is the contract's (audited below).
"""
from __future__ import division, print_function

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
W, H = 12, 18
FRAMES = 4


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


STL = ramp("steel")
AR = ramp("arcane")

# The flame per frame: rows from its tip down, (half-width, core half-width), and which way its tip leans. Frame 0 is the
# flare: taller, wider, the core most of it.
FLAMES = [
    ([(0, 0), (1, 0), (1, 1), (2, 1), (2, 2), (2, 2), (2, 1)], 0),
    ([(0, 0), (1, 0), (1, 0), (1, 1), (2, 1), (1, 1)], 1),
    ([(0, 0), (0, 0), (1, 0), (1, 1), (2, 1), (1, 1)], 0),
    ([(0, 0), (1, 0), (1, 1), (1, 1), (2, 1), (1, 0)], -1),
]

CAGE_TOP, CAGE_BOTTOM = 6, 14
BAR_L, BAR_R = 2, 9


def frame(k):
    c = raster.Canvas(W, H)
    flared = k == 0
    # The glass: the flame's light through it, brightest round the flame, the deep stop at the cage's corners.
    for y in range(CAGE_TOP, CAGE_BOTTOM + 1):
        for x in range(BAR_L + 1, BAR_R):
            d = abs(x - 5.5) / 3.5 + abs(y - 11.0) / 5.0
            if flared:
                c.set(x, y, AR[2] if d < 0.9 else AR[3])
            else:
                c.set(x, y, AR[3] if d < 0.9 else AR[4])
    # Two thin inner bars across the far panes - behind the flame, which would otherwise be cut into a bell by them.
    for y in range(CAGE_TOP + 1, CAGE_BOTTOM):
        if y % 3 != 0:
            c.set(4, y, STL[3])
            c.set(7, y, STL[4])
    # The flame, standing on the wick at the cage's foot.
    rows, lean = FLAMES[k]
    base = CAGE_BOTTOM - 1
    tip = base - len(rows) + 1
    for i, (half, core) in enumerate(rows):
        y = tip + i
        shift = lean if i < 2 else 0
        for x in range(5 - half + shift, 7 + half + shift):
            edge = x in (5 - half + shift, 6 + half + shift)
            c.set(x, y, AR[1] if edge else AR[0])
        for x in range(6 - core + shift, 6 + core + shift):
            c.set(x, y, AR[0])
    c.set(5, base + 1, STL[3])
    c.set(6, base + 1, STL[3])
    # The cage's corner bars, steel lit on the key's side (left).
    for y in range(CAGE_TOP, CAGE_BOTTOM + 1):
        c.set(BAR_L, y, STL[1] if y > CAGE_TOP else STL[0])
        c.set(BAR_R, y, STL[3])
    # The cap: a peak over a brim, the ring above it.
    c.hline(1, 10, 5, STL[3])
    c.set(1, 5, STL[2])
    c.hline(2, 9, 4, STL[2])
    c.set(2, 4, STL[1])
    c.hline(4, 7, 3, STL[2])
    c.set(4, 3, STL[0])
    c.set(9, 4, STL[3])
    for (x, y, t) in ((5, 0, 1), (6, 0, 2), (4, 1, 1), (7, 1, 3), (4, 2, 2), (7, 2, 3)):
        c.set(x, y, STL[t])
    # The foot: a plate under the cage, its underside in occ.
    c.hline(1, 10, CAGE_BOTTOM + 1, STL[2])
    c.set(1, CAGE_BOTTOM + 1, STL[1])
    c.set(10, CAGE_BOTTOM + 1, STL[3])
    c.hline(2, 9, CAGE_BOTTOM + 2, STL[3])
    c.hline(3, 8, CAGE_BOTTOM + 3, OCC)
    # Flaring, the light spills out past the bars: a few of the glass's stops dithered either side.
    if flared:
        for y in range(CAGE_TOP + 2, CAGE_BOTTOM - 1, 2):
            c.set(BAR_L - 1, y, AR[3])
            c.set(BAR_R + 1, y + 1, AR[3])
        c.set(0, 10, AR[4])
        c.set(11, 11, AR[4])
    return c


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    strip = raster.Canvas(W * FRAMES, H)
    for k in range(FRAMES):
        one = frame(k)
        bad = one.audit()
        if bad:
            print("gaol_lantern: frame %d has colours outside the contract: %s" % (k, ", ".join("%s x%d" % kv for kv in bad)))
            return 1
        strip.blit(one.img, k * W, 0)
    strip.scaled(SCALE).save(os.path.join(OUT, "gaol-lantern.png"))
    print("dungeon/gaol-lantern: %d frames of %dx%d on screen -> assets/bonelight/world/dungeon/gaol-lantern.png"
          % (FRAMES, W * SCALE, H * SCALE))
    return 0


if __name__ == "__main__":
    sys.exit(main())
