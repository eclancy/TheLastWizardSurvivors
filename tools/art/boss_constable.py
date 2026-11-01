# -*- coding: utf-8 -*-
"""The Last Bridge's boss: the Constable (.ai/endgame-castle.md 3.6). Run: python tools/art/boss_constable.py

    assets/bonelight/enemies/bridge-constable-{moving,attack,hurt,death}.png
    scenes/resources/BridgeConstableFrames.tres
    tools/art/_boss_constable.png             (gitignored) the pose at x4, and filled black

"He holds the gate from the wall. Get to the gate." The keeper of his castle's gate: a taken
soldier of the steel family, the tallest figure on the bridge, plated head to foot under the gate's
red livery, a ring of the gate's keys at his hip and a halberd taller than he is. Drawn on the 96
boss cell like the other chapter bosses (sprite_bosses.py), and its own module so this chapter
never edits that file.

ITS OUTLINE: A TALL POST WITH A FLAG AND A HOOP. The other bosses are a T, a bell, a dome, a slab
with tablets, a wedge, an S, an H, a figure behind a door (the Hayward) and a standing stone. This
is the narrowest of them, upright and two and a half times as tall as it is wide, with the halberd
standing beside it a head higher still - its axe blade breaking the top line toward the front like
a flag - and on the far hip a hoop with keys hanging from it, a ring with daylight through its
middle that no other figure in the game carries. Filled black, those three are the read: post,
flag, hoop. Nothing in the bridge's cast echoes it (cast_bridge.py: the pikeman is short and his
pike a bare point, the knight is crouched, nobody carries a ring).

WHAT HE IS MADE OF, by world-and-tone.md. Taken, so the mark in the slit of his sallet is the cold
arcane, two pixels. The steel sits a stop above its shade (the deck is stone's shade, as the
Emberdeep's floor was - cast_bridge.py takes the same lesson), lit on the key side by the bright
stops only where a plate turns to the light. The livery is cloth red, the gate's colour, worn as a
tabard over the plate: old cloth, under the 15% the contract allows and never on the outline, with
the gate's device on the chest - a key, in tarnished gold. The haft is wood (gold's lower half).

HOW THE FIGHT CUTS IT (BridgeConstableBoss.cs, the fall): three pieces that never overlap. The
halberd - its head above row 26 right of column 62, and its haft in columns 71-74 below - goes over
first, so the haft is drawn IN FRONT of his gauntlet: cut away, it leaves the open grip. The sallet
(columns 26-61 above row 30) bows forward on its own pivot at the neck. Everything else is the
body, which kneels. Keep those columns and rows clear if the drawing changes.
"""
from __future__ import print_function

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import anim_sets
import pixel as P
import roster
import spriteframes
import sprite_bosses as SB
from cast_bridge import PALETTE, GOLD_HI, GOLD_LIT, GOLD, GOLD_SH, GOLD_DP, RED_LIT, RED, RED_SH, RED_DP, WOOD, WOOD_LIT, WOOD_DK

G = roster.G
CELL = 96
FEET = CELL - 2
STEM = "bridge-constable"
SHEET = "BridgeConstableFrames"
OUT_ENEMIES = os.path.join("assets", "bonelight", "enemies")

# Boss feel (build.py's BOSS_FEEL): a tall man in plate barely sways as he walks, drives the
# halberd forward with his whole height, and hardly flinches.
FEEL = (0.3, 5.0, 1)

# The halberd's haft: two columns, in front of the gauntlet (see the docstring, HOW THE FIGHT CUTS IT).
HAFT_X = 72
HAFT_TOP, HAFT_BUTT = 20, 93


def halberd_haft(g):
    """The haft from the socket to the butt: wood, lit on the key side, an iron shoe at the foot."""
    for y in range(HAFT_TOP, HAFT_BUTT + 1):
        g.set(HAFT_X, y, WOOD_LIT)
        g.set(HAFT_X + 1, y, WOOD)
    for y in range(HAFT_TOP + 5, HAFT_BUTT - 6, 9):
        g.set(HAFT_X + 1, y, WOOD_DK)                   # a knot in the grain
        g.set(HAFT_X, y + 4, WOOD)
    # the langets: iron strips nailed down the haft under the head
    for y in range(HAFT_TOP, HAFT_TOP + 11):
        g.set(HAFT_X - 1, y, "r" if y % 4 else "t")
        g.set(HAFT_X + 2, y, "R")
    g.set(HAFT_X + 2, HAFT_TOP + 4, "q")
    g.set(HAFT_X + 2, HAFT_TOP + 9, "q")
    # the shoe: an iron ferrule at the butt, standing on the ground
    for y in range(HAFT_BUTT - 4, HAFT_BUTT + 1):
        g.set(HAFT_X - 1, y, "r")
        g.set(HAFT_X, y, "t" if y == HAFT_BUTT - 4 else "r")
        g.set(HAFT_X + 1, y, "R")
        g.set(HAFT_X + 2, y, "q")
    # The key rim down the haft's lit side wherever nothing stands beside it.
    for y in range(HAFT_TOP, HAFT_BUTT + 1):
        if g.get(HAFT_X - 2, y) == "." and g.get(HAFT_X - 1, y) == ".":
            g.set(HAFT_X - 1, y, "W")


def halberd_head(g):
    """The head, above the helm: a spike up, an axe blade forward, a hooked fluke back."""
    # The socket binding the head onto the haft.
    g.rect(HAFT_X - 1, 15, HAFT_X + 2, 19, "R")
    g.span(15, HAFT_X - 1, HAFT_X + 2, "t")
    g.set(HAFT_X - 1, 16, "r")
    g.span(19, HAFT_X - 1, HAFT_X + 2, "q")
    SB.rivets(g, [(HAFT_X, 17)], "t", "q")
    # THE SPIKE: straight up from the socket, tapering to a point on row 3.
    for y in range(3, 15):
        w = 0 if y < 6 else 1
        g.set(HAFT_X, y, "t")
        if w:
            g.set(HAFT_X + 1, y, "R")
    g.set(HAFT_X + 1, 9, "q")
    # THE AXE BLADE, forward of the haft: a crescent whose cutting edge bulges toward the front.
    # The face is the steel's base with the shade under the edge's bevel; the edge itself is the lit
    # stop, the one bright line on him, because a honed edge is the plane that turns to the light.
    top, bottom = 5, 22
    for y in range(top, bottom + 1):
        t = (y - top) / float(bottom - top)
        edge = int(round(78 + 7.5 * math.sin(math.pi * t)))
        x0 = HAFT_X + 2
        g.span(y, x0, edge, "r")
        g.set(x0, y, "R")
        g.set(edge, y, "t" if 0.15 < t < 0.85 else "r")
        if edge - 1 > x0:
            g.set(edge - 1, y, "R" if y % 3 else "r")
    # the blade's root, darker where it meets the socket, and a hole punched through it
    for y in range(top + 2, bottom - 1):
        g.set(HAFT_X + 3, y, "q")
    g.set(HAFT_X + 6, 13, "o")
    g.set(HAFT_X + 7, 13, "o")
    g.set(HAFT_X + 6, 14, "o")
    g.span(bottom + 1, HAFT_X + 3, HAFT_X + 8, "o")
    # THE FLUKE: a hooked beak back from the socket, curving down.
    SB.spike(g, HAFT_X - 1, 12, -8, 4, 1.8, "R", "r", "q")
    g.set(HAFT_X - 9, 17, "R")
    # a cord of the gate's red bound under the head, its tassel hanging by the haft
    g.set(HAFT_X - 1, 20, RED)
    for y in range(21, 25):
        g.set(HAFT_X - 2, y, RED_SH if y % 2 else RED)
    g.set(HAFT_X - 3, 24, RED_DP)
    g.set(HAFT_X - 2, 25, RED_DP)


def legs(g):
    """Plate legs, a short stride apart, the near one forward."""
    for (x0, x1, mid, lit, dark, knee) in ((36, 43, "R", "r", "q", 77), (48, 55, "r", "t", "R", 76)):
        g.rect(x0, 66, x1, 89, mid)
        g.line(x0, 66, x0, 89, lit)
        g.line(x1, 66, x1, 89, dark)
        SB.ridges(g, 68, 88, mid, dark, lit, step=4, jitter=6)
        # the knee cop, a round plate standing off the leg, and its wing
        g.disc((x0 + x1) / 2.0, knee, 3.6, mid)
        g.set((x0 + x1) / 2.0 - 1, knee - 2, lit)
        g.set((x0 + x1) / 2.0 - 2, knee - 1, lit)
        g.span(knee + 4, x0, x1, "o")
        g.set(x1 + 1, knee, dark)
    # the sabatons: laminated steel shoes, pointed forward, the near one a little ahead
    for (x0, x1, lit) in ((34, 45, "r"), (47, 59, "t")):
        g.rect(x0, 90, x1, FEET, "R")
        g.span(90, x0, x1 - 2, lit)
        g.span(92, x0 + 1, x1 - 1, "q")
        g.set(x1, 91, "R")
        g.set(x1 + 1, 92, "R")
        g.set(x1 + 1, FEET, "q")
        g.span(FEET, x0, x1, "q")
    g.rect(44, 70, 47, 89, "o")                         # daylight's shadow between the legs


def torso(g):
    """The breastplate and the fauld under it; the tabard covers most of the front."""
    SB.swell(g, 29, 57, [11.0, 13.0, 13.5, 12.5, 11.5, 11.0], "r", "t", "R", cx=46.0)
    SB.ridges(g, 31, 56, "r", "R", "t", step=4, jitter=5)
    # the fauld: lames of plate below the waist, each lit along its top and shaded under it
    for i, y in enumerate(range(57, 67, 3)):
        g.span(y, 33 - i, 59 + i, "R")
        g.span(y + 1, 33 - i, 59 + i, "r")
        g.span(y + 2, 33 - i, 59 + i, "q")
        g.set(33 - i, y + 1, "t")
        g.set(59 + i, y + 1, "R")
    SB.rivets(g, [(35, 61), (57, 61), (36, 64), (56, 64)], "t", "q")


def tabard(g):
    """The gate's red livery over the plate: chest to the knee-cops' tops, split at the hem, the key on it.

    Narrow, and the plate shows either side of it: a tabard as wide as the breastplate was a fifth of
    the figure in red, and the contract holds old cloth under 15% (world-and-tone.md)."""
    keys = [(33, 41.0, 51.0), (42, 40.5, 51.5), (56, 40.0, 52.0), (70, 39.5, 52.5)]
    roster.mass(g, keys, RED, RED_LIT, RED_SH, rim=False)
    for pts in ([(42, 36), (42, 55), (41, 69)], [(49, 37), (49, 56), (50, 70)]):
        roster.fold(g, pts, RED_SH, RED_LIT)
    # the split up the front of the skirt, and a ragged hem
    for y in range(61, 71):
        g.set(45, y, "o")
        g.set(46, y, RED_DP)
    for (x, d) in zip(range(40, 53), [1, 0, 1, 2, 1, 0, 1, 1, 0, 2, 1, 0, 1]):
        for k in range(1, d + 1):
            g.set(x, 70 + k, RED_SH if k < d else RED_DP)
    # the neckline, where the gorget shows above it
    g.span(33, 42, 50, RED_DP)
    # THE GATE'S DEVICE: a key in tarnished gold, bit down, on the chest.
    for (x, y) in ((45, 38), (46, 38), (47, 38), (44, 39), (48, 39), (44, 40), (48, 40), (45, 41), (47, 41)):
        g.set(x, y, GOLD)
    g.set(46, 39, RED_DP)
    g.set(46, 40, RED_DP)
    g.set(44, 39, GOLD_LIT)
    g.set(45, 38, GOLD_LIT)
    for y in range(42, 50):
        g.set(46, y, GOLD if y % 3 else GOLD_SH)
    g.set(45, 43, GOLD_LIT)
    for (x, y) in ((47, 47), (48, 47), (47, 49), (48, 49), (48, 48)):
        g.set(x, y, GOLD_SH)
    g.set(47, 48, GOLD)


def belt(g):
    """A sword belt of old leather across the hips, a gold buckle, the ring's hanger on the far hip."""
    g.span(56, 34, 58, WOOD)
    g.span(57, 34, 58, WOOD_DK)
    g.set(34, 56, WOOD_LIT)
    g.rect(44, 55, 47, 58, GOLD_SH)
    g.span(55, 44, 47, GOLD_LIT)
    g.set(45, 57, GOLD_DP)
    g.set(46, 57, GOLD_DP)
    # the hanger: a short iron chain from the belt to the ring
    for (x, y, ch) in ((33, 57, "r"), (32, 58, "R"), (31, 59, "r"), (30, 60, "R"), (29, 61, "r")):
        g.set(x, y, ch)


def far_arm(g):
    """The far arm, hanging, its gauntlet at the hip by the ring."""
    SB.limb(g, [(33, 36), (30, 47), (31, 55)], 3.0, "R", "r", "q")
    SB.ridges(g, 38, 54, "R", "q", "r", step=3, jitter=7)
    g.disc(31.5, 57.5, 3.4, "R")
    g.set(30, 55, "r")
    g.set(29, 56, "r")
    g.span(60, 30, 33, "q")
    g.set(34, 57, "q")


def ring_of_keys(g):
    """THE RING: a hoop of iron with daylight through it, the gate's keys hanging off its lower half."""
    cx, cy, r = 24.0, 66.0, 6.4
    for k in range(0, 360, 6):
        a = math.radians(k)
        x, y = cx + r * math.cos(a), cy + r * math.sin(a)
        lit = 150 <= k <= 300                            # the upper-left of the hoop faces the key
        g.set(x, y, "t" if lit and k % 24 == 0 else ("r" if lit else "R"))
        g.set(cx + (r - 1) * math.cos(a), cy + (r - 1) * math.sin(a), "q")
    # The keys: iron mostly, and one great key of the gate in brass. Each hangs from the hoop by its
    # bow, a shank down, a bit at the end - drawn with the bit facing out, so the bunch fans.
    for (bx, by, length, bit, tone, lit, dark) in (
            (18, 69, 9, -1, "R", "r", "q"),
            (21, 72, 11, -1, "r", "t", "R"),
            (25, 73, 13, 1, GOLD, GOLD_LIT, GOLD_SH),       # the gate's own key
            (28, 71, 9, 1, "R", "r", "q"),
            (30, 68, 7, 1, "r", "t", "R")):
        g.set(bx, by, lit)
        g.set(bx + 1, by, tone)
        g.set(bx, by + 1, dark)
        g.set(bx + 1, by + 1, dark)
        for y in range(by + 2, by + length):
            g.set(bx, y, tone if y % 4 else lit)
        end = by + length
        g.set(bx, end, tone)
        g.set(bx + bit, end, tone)
        g.set(bx + bit, end - 2, dark)
        g.set(bx + 2 * bit, end, dark)
        g.set(bx + 2 * bit, end - 1, tone)
    g.set(24, 59, "t")                                  # where the hanger's last link holds the hoop
    g.set(25, 59, "r")


def pauldrons(g):
    """Two great pauldrons of three lames each, the near one a little higher and lighter."""
    for (cx, y0, y1, w, mid, lit, dark) in ((32.5, 26, 39, 7.5, "R", "r", "q"), (59.5, 27, 40, 7.5, "r", "t", "R")):
        SB.swell(g, y0, y1, [w - 3.0, w, w + 0.5, w, w - 1.0], mid, lit, dark, cx=cx)
        for k, y in enumerate((y0 + 4, y0 + 8, y0 + 11)):
            for x in range(int(cx - w - 1), int(cx + w + 2)):
                if g.get(x, y) not in (".", "W"):
                    g.set(x, y, "o" if k < 2 else dark)
                    if g.get(x, y - 1) == mid:
                        g.set(x, y - 1, lit if (x + k) % 3 else mid)
        SB.rivets(g, [(int(cx - 3), y0 + 2), (int(cx + 3), y0 + 2)], lit, dark)


def helm(g):
    """A sallet: a deep rounded bowl drawn out behind into a long tail, a slit, a bevor under it."""
    SB.swell(g, 8, 21, [4.0, 7.0, 8.5, 9.0, 9.0, 8.5], "r", "t", "R", cx=48.0)
    # the comb along the crown
    g.span(7, 45, 51, "R")
    g.span(8, 44, 52, "t")
    g.set(52, 8, "r")
    # the tail, sweeping back and down over the nape
    for y in range(16, 25):
        t = (y - 16) / 8.0
        x0 = int(round(39 - 9.0 * t))
        x1 = 42
        g.span(y, x0, x1, "R" if y < 22 else "q")
        g.set(x0, y, "r")
        if y >= 18:
            g.set(x0 + 1, y, "R")
    g.span(25, 30, 36, "o")
    # a ridge where the bowl turns into the tail
    roster.fold(g, [(41, 12), (40, 18), (36, 23)], "q", "t")
    # THE VISOR SLIT, forward, and the dark under the brow of it
    g.span(15, 48, 57, "o")
    g.span(16, 49, 57, "o")
    g.span(14, 49, 56, "R")
    g.span(17, 50, 57, "q")
    # the bevor: a chin-plate rising to meet the sallet, and the gorget's lames below it
    for y in range(18, 28):
        x0 = 42 + (1 if y > 24 else 0)
        x1 = 56 - (y - 18) // 3
        g.span(y, x0, x1, "R")
        g.set(x1, y, "q")
        g.set(x0, y, "r")
    for y in (21, 24, 27):
        g.span(y, 43, 55 - (y - 18) // 3, "q")
        g.span(y - 1, 44, 52, "r" if y < 27 else "R")
    SB.rivets(g, [(44, 9), (40, 15), (53, 19)], "t", "q")


def near_arm(g):
    """The near arm, from the pauldron down and forward to the fist on the haft."""
    SB.limb(g, [(60, 38), (65, 46), (69, 50)], 3.0, "r", "t", "R")
    SB.ridges(g, 40, 49, "r", "R", "t", step=3, jitter=4)
    # the gauntlet closed round the haft (the haft is drawn over it afterwards)
    g.rect(68, 47, 76, 54, "r")
    g.span(47, 68, 75, "t")
    g.span(54, 68, 76, "q")
    g.line(76, 48, 76, 53, "R")
    for x in (69, 71, 73, 75):
        g.set(x, 51, "R")                               # the knuckles' gaps
    g.set(67, 50, "q")
    g.set(67, 51, "q")


def constable():
    g = G(CELL)
    near_arm(g)
    halberd_haft(g)                                     # in front of the fist - see HOW THE FIGHT CUTS IT
    legs(g)
    torso(g)
    tabard(g)
    belt(g)
    far_arm(g)
    ring_of_keys(g)
    pauldrons(g)
    helm(g)
    halberd_head(g)
    rim_left(g)
    roster.contact(g, 30, 62, y=FEET)
    # THE MARK, last: two points of the cold arcane in the slit, the hot one forward.
    g.set(53, 16, "y")
    g.set(55, 16, "Y")
    return g.rows()


def rim_left(g):
    """The 1 px key rim on the left edge of each row, where a form meets the empty cell."""
    for y in range(2, FEET):
        for x in range(1, CELL - 1):
            ch = g.get(x, y)
            if ch == "W":
                break
            if ch != "." and g.get(x - 1, y) == ".":
                run = 0
                while g.get(x + run, y) not in (".", "W") and x + run < CELL:
                    run += 1
                if run >= 2:
                    g.set(x - 1, y, "W")
                break


def build(root=ROOT):
    pose = constable()
    problems = P.check_anchor("%s pose" % STEM, [pose], CELL)
    lean, reach, knock = FEEL
    spec = []
    for anim, frames, loop, speed in anim_sets.full_set(pose, lean=lean, reach=reach, knock=knock):
        png = "%s-%s.png" % (STEM, anim)
        P.write_strip(frames, PALETTE, os.path.join(root, OUT_ENEMIES, png), CELL)
        spec.append((anim, png, len(frames), loop, speed))
    spriteframes.write(os.path.join(root, "scenes", "resources", SHEET + ".tres"), spec,
                       OUT_ENEMIES.replace(os.sep, "/"), cell=CELL)
    return pose, sum(a[2] for a in spec), problems


def main():
    pose, n, problems = build()
    filled, edges, ratio = roster.density(pose)
    print("%s  %dx%d  %d px  %.2f edges/px%s  %d frames -> scenes/resources/%s.tres"
          % (STEM, CELL, CELL, filled, ratio, "" if ratio >= 0.80 else "   SPARSE", n, SHEET))
    black = ["".join("o" if ch != "." else "." for ch in row) for row in pose]
    P.preview([("pose", [pose]), ("silhouette", [black])], dict(PALETTE, o="#05070C"),
              os.path.join(HERE, "_boss_constable.png"), zoom=4, bg=(22, 26, 38), cell=CELL)
    if problems:
        print("ANCHOR PROBLEMS:")
        for p in problems:
            print("   " + p)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
