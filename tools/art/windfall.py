# -*- coding: utf-8 -*-
"""WINDFALL's orchard (scripts/Windfall.cs): the small iron-fruited tree the spell plants, and its fruit at every age.

Run: python tools/art/windfall.py [--preview=PATH]

Eric, 2026-11-09 (.ai/character-fit.md section 4): the spell plants a small iron-fruited tree where the wizard stands; its
fruit ripens over time, one at a time, swelling and turning from green to iron; an enemy bumping the tree brings one ripe
fruit down on itself, and the wizard walking back under it brings the whole crop down in a ring. Grass and Metal: a living
tree, iron apples. House Bonelight (.ai/art-direction.md), not Ma Liang's ink.

WHY A SHEET AND NOT CODE. The crown is a lumpy, turning form of thirty-odd leaf clusters, each shaded as a lump of its own
and as part of the whole crown - trees.py's crown painter, the one every tree in the world is drawn by, so the spell's tree
belongs to the same world. That is a picture, not a few rects a frame. The fruit, which change every second, are small
stamps the game lays over it (Windfall.DrawTree), so the ripening is drawn live from a handful of cells.

THE SHEET (assets/bonelight/effects/windfall.png, authored 1:1 and saved at x2 like every effect, section 1). Windfall.cs
reads exactly these numbers:

  THE TREE (row 0, TREE_W x TREE_H cells, its trunk's foot at FOOT in every cell):
    0 grown            - the tree as it stands
    1 shaken left      - the crown thrown a pixel left and its leaves tossed (drawn when it is shaken or bumped)
    2 shaken right     - and a pixel right
    3 sapling          - the second beat of a planting: a thin trunk, a small crown
    4 sprout           - the first beat: two leaves out of turned earth

  THE FRUIT (row 1, at y FRUIT_Y, FRUIT_CELL cells, the fruit's middle at FRUIT_MID in each):
    0 blossom          - what an empty slot on the crown shows: a pale bloom on a green calyx
    1 set              - a small hard green fruit
    2 green            - swelling
    3 turning          - the iron coming up through the green from its shaded side
    4 iron             - ripe: a steel apple lit from the upper left, stalk and leaf
    5 rotten iron      - Cider Rot's ripe fruit: the same apple, bruised and spotted with Poison's green
    6 fallen           - an iron apple lying on the ground, squat, its contact shadow under it
    7 rot              - Cider Rot's: a burst apple gone to pulp on the ground

THE LOOK, and why:
  - The crown is `verdant`, as every canopy is (.ai/terrain.md section 4; a crown on the ground's own lichen ramp is a smudge),
    lit from the upper left, with leaf glints on its lit side. It is the wizard's made thing, and he carries the only
    saturated greens (art-direction.md section 3): it reads as his among the world's own trees, which are three times its size.
  - The trunk is IRON: the steel ramp's lower stops over stone, a lit left edge, a dark right. Grass above, Metal below, Metal
    hanging in it: the two tags in one silhouette.
  - The ripe fruit is steel with a PLANE of steel's `hi` on its upper left (never a point: "Matte, not wet", section 2) - the
    one high-value tone on the tree, spent on the thing the player is waiting for, so a crown's ripe count can be read at a
    glance against the green. An unripe fruit is green on green, on purpose: it is not ready, and should not shout.
  - A small tree: about the wizard's height on screen (TREE_H x2 against his 80 px), so the swarm walks round it and he
    stands under it, and the world's trees still dwarf it.

--preview lays the grown tree with a crop at every age, a shaken frame and the planting beats on the floors the game's dark
and pale chapters use, at the game's x2, to look at.
"""
from __future__ import division, print_function

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import bonelight as bl
import raster
import trees as T

ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "assets", "bonelight", "effects", "windfall.png")

# --- the layout: Windfall.cs reads exactly these numbers -----------------------------------------------------------------
TREE_W, TREE_H = 48, 56
FOOT = (24, 50)                 # the trunk's foot in every tree cell: where the tree stands
TREE_FRAMES = 5                 # grown, shaken left, shaken right, sapling, sprout
FRUIT_CELL = 10
FRUIT_Y = TREE_H                # the fruit row starts under the tree row
FRUIT_MID = (5, 5)              # the fruit's middle in its cell
FRUIT_STAGES = 8
SHEET_W = TREE_W * TREE_FRAMES  # 240
SHEET_H = TREE_H + FRUIT_CELL   # 66

# Where fruit hang on the grown crown, in tree-cell pixels, front-most last - Windfall.FruitSlots holds the same points as
# offsets from FOOT. The first ones are the low, front, lit side of the crown, so a small crop hangs where it is seen best.
FRUIT_SLOTS = [(16, 25), (30, 26), (23, 20), (10, 18), (36, 19), (17, 12), (29, 11), (37, 28), (9, 27), (23, 29)]

OCC = bl.OCC
VE = T.VE                       # verdant: hi, lit, base, shade, deep
ST = T.ramp("steel")
STONE = T.ramp("stone")
SKIN = T.SK
FL = T.FL
PO = bl.ELEMENTS["poison"]      # core, hot, mid, edge
GR = bl.ELEMENTS["grass"]

# Iron bark: the steel ramp down to occlusion. trunk() lights bark[1:] across the trunk and puts bark[4] on its
# shaded edge.
IRON = [ST[1], ST[2], ST[3], ST[4], OCC]
LEAVES = VE[1:5]
LX, LY = T.LX, T.LY

SEED = 23


# --- the tree --------------------------------------------------------------------------------------------------------------

def _limbs(c, rnd, top, spread):
    """Three short iron limbs up into the crown from the head of the trunk, drawn before the leaves cover all but their roots."""
    fx, _ = FOOT
    for (s, up) in ((-1, 0.0), (1, 1.5), (-1, 3.0)):
        a = -math.pi / 2 + s * (0.55 + rnd.random() * 0.35)
        T.limb(c, fx + s, top + up, a, spread * (0.9 + rnd.random() * 0.3), 2.2, IRON, 1, rnd)


def grown(crown_dx=0, toss=0):
    """The tree as it stands. `crown_dx` throws the crown a pixel either way (a shake); `toss` reseeds its ragged edge."""
    c = raster.Canvas(TREE_W, TREE_H)
    rnd = T.Rng(SEED)
    fx, fy = FOOT
    T.shade_pool(c, fx + 3, fy, 14, 4, density=0.55)
    T.contact(c, fx, fy + 1, 4.0)
    top = 30
    T.trunk(c, fx, fy, top, 6, 3.5, IRON, SEED, flare=2.2)
    _limbs(c, rnd, top + 2, 9.0)
    ccx, ccy, crx, cry = fx + crown_dx, 19, 20, 13
    clusters = T.fill_clusters(rnd, ccx, ccy, crx - 5, cry - 4, 24, 4.0, 7.0)
    T.crown(c, clusters, LEAVES, ccx, ccy, crx, cry, SEED + toss, ragged=0.3)
    # Where the crown thins at its foot the iron limbs show through, as a real crown's do.
    for k in range(10):
        x = int(ccx - 9 + rnd.random() * 18)
        y = int(ccy + cry * (0.55 + rnd.random() * 0.25))
        if c.get(x, y)[3] and T.hash2(x, y, SEED) < 0.45:
            c.set(x, y, VE[4])
    _lit_rim(c)
    return c


def _lit_rim(c):
    """The crown's key-facing edge one stop up: where the upper-left light falls on its outermost leaves, so the silhouette
    lifts off a dark floor in the dark chapters without an outline (art-direction.md section 2, "The rim is load-bearing")."""
    snap = [[c.get(x, y) for y in range(c.h)] for x in range(c.w)]
    lit = raster.rgb(VE[1])
    base = raster.rgb(VE[2])
    hi = raster.rgb(VE[0])
    for x in range(c.w):
        for y in range(c.h):
            p = snap[x][y]
            if not p[3] or y > 30:
                continue
            # Open to the upper left: transparent up-left of it, so the key reaches it.
            ux = snap[x - 1][y] if x > 0 else (0, 0, 0, 0)
            uy = snap[x][y - 1] if y > 0 else (0, 0, 0, 0)
            if ux[3] and uy[3]:
                continue
            if p == base:
                c.set(x, y, VE[1])
            elif p == lit and T.hash2(x, y, SEED + 5) < 0.45:
                c.set(x, y, VE[0])


def sapling():
    """The second beat of a planting: a thin iron stem and a small crown half the grown one's size."""
    c = raster.Canvas(TREE_W, TREE_H)
    rnd = T.Rng(SEED + 1)
    fx, fy = FOOT
    T.shade_pool(c, fx + 2, fy, 8, 3, density=0.5)
    T.contact(c, fx, fy + 1, 2.5)
    T.trunk(c, fx, fy, 37, 3, 2, IRON, SEED + 1, flare=1.2)
    ccx, ccy, crx, cry = fx, 33, 10, 7
    clusters = T.fill_clusters(rnd, ccx, ccy, crx - 3, cry - 2, 10, 2.8, 4.2)
    T.crown(c, clusters, LEAVES, ccx, ccy, crx, cry, SEED + 1, ragged=0.3)
    _lit_rim(c)
    return c


def sprout():
    """The first beat: earth turned at his feet and a shoot with two leaves out of it."""
    c = raster.Canvas(TREE_W, TREE_H)
    fx, fy = FOOT
    # The turned earth: a low mound, lit on the left, over its contact line.
    T.contact(c, fx, fy + 1, 4.5)
    for dx in range(-4, 5):
        y = fy - (1 if abs(dx) < 3 else 0)
        c.set(fx + dx, y, STONE[1] if dx < -1 else (STONE[2] if dx < 2 else STONE[3]))
        if abs(dx) < 3:
            c.set(fx + dx, fy, STONE[3])
    # The shoot.
    for y in range(fy - 8, fy - 1):
        c.set(fx, y, VE[2])
        c.set(fx + 1, y, VE[3])
    # Two leaves, the left one lit.
    for (dx, dy, col) in ((-1, -8, VE[1]), (-2, -9, VE[1]), (-3, -9, VE[0]), (-2, -8, VE[2]),
                          (2, -7, VE[2]), (3, -8, VE[2]), (4, -8, VE[3]), (3, -7, VE[3])):
        c.set(fx + dx, fy + dy, col)
    return c


# --- the fruit -------------------------------------------------------------------------------------------------------------

def _shade_disc(c, cx, cy, r, tones, ry=None, plane=None):
    """A disc lit from the upper left through `tones` (brightest first), each pixel by its normal; `plane`, a tone, laid as a
    small lit plane on the upper left - two pixels at least, never one (a point of light reads wet)."""
    ry = ry or r
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            nx = (x + 0.5 - cx) / r
            ny = (y + 0.5 - cy) / ry
            d = nx * nx + ny * ny
            if d > 1.0:
                continue
            v = 0.55 - (nx * LX + ny * LY) * 0.45 - d * 0.12
            c.set(x, y, T.band(tones, v, x, y, 0.25))
    if plane:
        px, py = int(math.floor(cx - r * 0.45)), int(math.floor(cy - ry * 0.5))
        c.set(px, py, plane)
        c.set(px + 1, py, plane)
        if r >= 2.8:
            c.set(px, py + 1, plane)


def _stalk(c, x, y, leaf=True):
    c.set(x, y, STONE[1])
    c.set(x, y - 1, STONE[2])
    if leaf:
        c.set(x + 1, y - 1, VE[1])
        c.set(x + 2, y - 1, VE[2])
        c.set(x + 2, y - 2, VE[1])


def fruit(stage):
    c = raster.Canvas(FRUIT_CELL, FRUIT_CELL)
    mx, my = FRUIT_MID
    if stage == 0:
        # A blossom: five pale petals round a green heart.
        for (dx, dy, col) in ((0, -1, SKIN[0]), (-1, 0, SKIN[0]), (1, 0, SKIN[1]), (-1, 1, SKIN[1]), (1, 1, FL[0]),
                              (0, 0, GR[1]), (0, 1, VE[2])):
            c.set(mx - 1 + dx, my - 1 + dy, col)
    elif stage == 1:
        _shade_disc(c, mx - 0.5, my, 1.9, [VE[0], VE[1], VE[2]])
        c.set(mx - 1, my - 2, STONE[2])
    elif stage == 2:
        _shade_disc(c, mx - 0.5, my, 2.6, [VE[0], VE[1], VE[2], VE[3]], plane=VE[0])
        _stalk(c, mx - 1, my - 3, leaf=False)
    elif stage == 3:
        # Turning: the lit side still green, the shaded side gone to iron.
        _shade_disc(c, mx - 0.5, my, 3.1, [VE[0], VE[1], ST[2], ST[3]], plane=VE[0])
        _stalk(c, mx - 1, my - 3)
    elif stage in (4, 5):
        _shade_disc(c, mx - 0.5, my + 0.3, 3.7, [ST[1], ST[2], ST[3], ST[4]], plane=ST[0])
        _stalk(c, mx - 1, my - 4)
        if stage == 5:
            # Cider Rot: a bruise on its shaded side and Poison's green spotting through the iron.
            for (dx, dy, col) in ((1, 1, PO[3]), (2, 1, PO[2]), (1, 2, PO[3]), (-2, 1, PO[2]), (0, 2, ST[4]), (2, 0, PO[3])):
                c.set(mx + dx, my + dy, col)
    elif stage == 6:
        # Fallen: squat on the ground, its contact shadow under it.
        c.disc(mx - 0.5, my + 3.2, 4.0, 1.2, OCC)
        _shade_disc(c, mx - 0.5, my + 0.8, 3.6, [ST[1], ST[2], ST[3], ST[4]], ry=3.0, plane=ST[0])
        c.set(mx - 1, my - 2, STONE[2])
    elif stage == 7:
        # Rot: the apple burst into pulp, Poison's green running out of it, a shard of the iron skin left.
        for (dx, dy, col) in ((-3, 1, PO[3]), (-2, 1, PO[2]), (-1, 0, PO[2]), (0, 0, PO[1]), (1, 0, PO[2]), (2, 1, PO[3]),
                              (-2, 2, PO[3]), (-1, 2, PO[3]), (0, 1, PO[2]), (1, 1, PO[3]), (2, 2, PO[3]), (3, 2, PO[3]),
                              (-1, 1, PO[2]), (0, 2, PO[3]), (1, 2, OCC), (-3, 2, OCC)):
            c.set(mx + dx, my + dy, col)
        for (dx, dy, col) in ((-1, -1, ST[1]), (0, -1, ST[2]), (1, -1, ST[3]), (0, -2, ST[2])):
            c.set(mx + dx, my + dy, col)
    return c


# --- the sheet ---------------------------------------------------------------------------------------------------------------

def frames():
    return [grown(), grown(crown_dx=-1, toss=3), grown(crown_dx=1, toss=5), sapling(), sprout()]


def sheet():
    out = raster.Canvas(SHEET_W, SHEET_H)
    for i, f in enumerate(frames()):
        out.blit(f.img, i * TREE_W, 0)
    for s in range(FRUIT_STAGES):
        out.blit(fruit(s).img, s * FRUIT_CELL, FRUIT_Y)
    return out


def _hang(c, crop_stages, ox=0, oy=0):
    """Lays fruit on a tree drawn at (ox, oy) as the game does: a stage per slot, front-most last."""
    for i, stage in enumerate(crop_stages):
        sx, sy = FRUIT_SLOTS[i]
        c.blit(fruit(stage).img, ox + sx - FRUIT_MID[0], oy + sy - FRUIT_MID[1])


def preview(path):
    """The grown tree at three ages of crop, a shaken frame with a fruit falling, the planting beats and a fallen and a rotten
    apple, on four floors (the Forest, the Dungeon and the Cave, the Frozen Waste, the Sands), at x2."""
    floors = ["#24404A", "#1E2634", "#38404F", "#2E2208"]
    crops = [[2, 1, 0], [4, 4, 3, 1, 0], [4, 4, 4, 4, 4, 4], [5, 5, 5, 3, 0, 0]]
    w = TREE_W * 6 + 8
    h = (TREE_H + 4) * len(floors)
    out = raster.Canvas(w, h)
    for r, floor in enumerate(floors):
        y0 = r * (TREE_H + 4)
        out.rect(0, y0, w - 1, y0 + TREE_H + 3, floor)
        for k, crop in enumerate(crops):
            out.blit(grown().img, k * TREE_W, y0)
            _hang(out, crop, k * TREE_W, y0)
        shaken = TREE_W * 4
        out.blit(grown(crown_dx=1, toss=5).img, shaken, y0)
        _hang(out, [4, 4], shaken, y0)
        out.blit(fruit(4).img, shaken + 30, y0 + 38)
        out.blit(fruit(6).img, shaken + 36, y0 + 47)
        out.blit(fruit(7).img, shaken + 8, y0 + 48)
        out.blit(sapling().img, TREE_W * 5 - 10, y0)
        out.blit(sprout().img, TREE_W * 5 + 12, y0)
    out.scaled(2).save(path)
    print("preview: %s" % path)


def main(argv):
    sh = sheet()
    bad = sh.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in windfall.png (art-direction.md section 3):")
        for hx, n in bad[:10]:
            print("   %s  x%d" % (hx, n))
        return 1
    d = os.path.dirname(OUT)
    if not os.path.isdir(d):
        os.makedirs(d)
    sh.scaled(2).save(OUT)
    print("  %-40s %dx%d" % (os.path.relpath(OUT, ROOT), SHEET_W * 2, SHEET_H * 2))
    for a in argv:
        if a.startswith("--preview="):
            preview(a.split("=", 1)[1])
    print("palette clean: every pixel is a colour art-direction.md defines")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
