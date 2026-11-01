# -*- coding: utf-8 -*-
"""The Court's boss: the Seneschal (.ai/endgame-castle.md 5.6). Run: python tools/art/boss_seneschal.py

    assets/bonelight/enemies/court-seneschal-{moving,attack,hurt,death}.png
    scenes/resources/CourtSeneschalFrames.tres
    tools/art/_boss_seneschal.png           (gitignored) the pose at x4, and filled black

    --preview   writes only the preview, for drawing; nothing under assets/ or scenes/ is touched.

The steward of the house: the man who ran it for its lord, and who runs it now for him. "He leaves
when it suits him. Follow." Drawn on the 96 boss cell like the other chapter bosses
(sprite_bosses.py), in its own module so this chapter never edits that file.

ITS OUTLINE: TWO UPRIGHTS OVER A BELL. The other bosses are a T, a bell, a dome, a slab with tablets,
a wedge, an S, an H, a figure behind a door (the Hayward) and a split monolith (the Ninth Stone).
This is a tall, narrow man in a gown that spreads on the floor, a stiff tall cap on his head, and
beside him, held out at arm's length and planted, his STAFF OF OFFICE - a white wand taller than he
is. Filled black: a bell with a chimney, and a pole beside it. Nothing in his household stands beside
an upright staff (cast_court.py says so in its docstring): the Usher swings a short rod overhead with
its knob highest, the Chamberlain carries keys and no staff at all.

THE WHITE STAFF is the brightest thing on him, and on purpose. A lord's steward carried a white wand
as the sign of his office, and broke it over his lord's grave when the office ended - which is how
he falls (CourtSeneschalBoss's fall: the staff snaps where his hand holds it). Bone, lit on the key
side, plain the whole way down but for a gilt cap and a gilt foot: not a knob, a cap, flat-topped
with three short tines.

WHAT HE IS MADE OF. Linen in its deep half - he is one of the house's people, taken (.ai/world-and-tone
.md: shadow wears what it took) - under a long front of the household's red, the livery he wore
before any of them; the GOLD CHAIN OF OFFICE across his shoulders with its badge on his breast; a
tall cap of black velvet (stone's upper stops), rounded at the crown on a rolled brim, a jewel
pinned to it - a court's sugarloaf, so tall that it is his outline's chimney, and never a stovepipe,
which was the first try and read as a top hat; a long white beard, because he
has served a long time; and the far sleeve hanging empty to his knee, dagged at its end, its red
lining showing - the asymmetry that keeps the bell from being a bell. The mark is in the dark under
the cap: two points of his cold arcane, as on everything he took.
"""
from __future__ import print_function

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
from cast_court import PALETTE, RED_HI, RED_LIT, RED, RED_SH, RED_DP, BR_HI, BR_LIT, BR, BR_SH, BR_DP

G = roster.G
CELL = 96
FEET = CELL - 2
STEM = "court-seneschal"
SHEET = "CourtSeneschalFrames"
OUT_ENEMIES = os.path.join("assets", "bonelight", "enemies")

# Boss feel (build.py's BOSS_FEEL): a steward does not sway when he walks, he processes; he brings
# the staff down with his whole height behind it; and he does not flinch.
FEEL = (0.3, 4.5, 1)

# Where the staff stands, in cell pixels - CourtSeneschalBoss's fall cuts it here, so the two must
# agree: its three columns, its cap and foot, and the row his hand closes on it.
STAFF_X = 70
STAFF_TOP = 4
STAFF_FOOT = FEET
STAFF_GRIP = 50

# The gown's middle line, and the head over it.
CX = 43.0


def staff(g):
    """The white wand: bone the whole way down, lit on its key side, a gilt cap and a gilt foot."""
    for y in range(STAFF_TOP + 4, STAFF_FOOT - 3):
        # Plain, but not a ruled line: the lit column breaks where the grain catches, every few rows.
        g.set(STAFF_X, y, "S" if y % 11 == 3 else "s")
        g.set(STAFF_X + 1, y, "k")
        g.set(STAFF_X + 2, y, "n" if y % 7 else "m")
    # THE CAP: flat-topped, three short tines - a crown's worth of gilt, never a knob.
    for (x, y, ch) in ((69, 4, BR_LIT), (71, 4, BR_LIT), (73, 4, BR),
                       (68, 5, BR_LIT), (69, 5, BR), (70, 5, BR_HI), (71, 5, BR), (72, 5, BR_SH), (73, 5, BR_SH),
                       (68, 6, BR), (69, 6, BR_LIT), (70, 6, BR), (71, 6, BR_SH), (72, 6, BR_SH), (73, 6, BR_DP),
                       (69, 7, BR_SH), (70, 7, BR), (71, 7, BR_SH), (72, 7, BR_DP)):
        g.set(x, y, ch)
    g.span(8, 69, 72, "o")                                   # its lip, the shadow it throws on the wood
    # A band of gilt a hand under the cap, and the foot: a gilt ferrule shod in iron.
    for (y, ch) in ((14, BR_LIT), (15, BR_SH)):
        g.span(y, STAFF_X, STAFF_X + 2, ch)
    g.set(STAFF_X + 2, 15, BR_DP)
    for y in range(STAFF_FOOT - 4, STAFF_FOOT + 1):
        g.set(STAFF_X, y, BR_LIT if y < STAFF_FOOT - 1 else "r")
        g.set(STAFF_X + 1, y, BR if y < STAFF_FOOT - 1 else "R")
        g.set(STAFF_X + 2, y, BR_SH if y < STAFF_FOOT - 1 else "q")
    g.span(STAFF_FOOT - 5, STAFF_X, STAFF_X + 2, "o")


def far_sleeve(g):
    """The far arm's sleeve, hanging empty from the shoulder to the knee, dagged at its end."""
    roster.mass(g, [(30, 30, 33), (40, 27, 32), (56, 24, 31), (74, 22, 30), (80, 22, 30)], "A", "E", "J")
    # Its red lining, along the inner edge where the sleeve turns.
    for y in range(38, 78):
        x = 31 - (y - 38) // 14
        g.set(x, y, RED_SH if (y // 3) % 3 else RED)
        g.set(x - 1, y, RED_DP if y % 5 else RED_SH)
    # The dags: a toothed end, cut back to nothing between the teeth.
    for x in range(21, 32):
        depth = (x * 5) % 4
        for k in range(depth):
            g.set(x, 80 - k, ".")
        g.set(x, 80 - depth, "A" if depth else "o")
        g.set(x, 81 - depth, ".")
    for (x, y) in ((24, 81), (28, 81), (22, 81)):
        g.set(x, y, "A")
        g.set(x, y + 1, "J")
    roster.fold(g, [(28, 34), (26, 52), (25, 72)], "J", "E")


def gown(g):
    """The long gown: linen in its deep half, a waist, then spreading on the floor."""
    SB.swell(g, 27, FEET - 1, [11.0, 13.0, 12.0, 10.5, 11.0, 12.5, 14.5, 17.0, 18.5], "E", "e", "A", cx=CX)
    for pts in ([(35, 46), (33, 70), (29, 91)], [(39, 60), (38, 76), (36, 92)], [(52, 48), (53, 70), (57, 91)],
                [(49, 64), (51, 92)]):
        roster.fold(g, pts, "A", "e")
    # The hem where it lies on the floor: a dark lip and a lit fold, irregular, not a ruled line.
    lip = "AAoAAAoAAAAoAoAAAAoAAAoAAoAAAAoAAAoAA"
    for i, x in enumerate(range(int(CX - 18), int(CX + 19))):
        g.set(x, FEET - 1, lip[i % len(lip)])
        g.set(x, FEET, "o" if i % 3 else "A")
    for (x, y) in ((30, 86), (34, 80), (55, 82), (58, 88), (47, 84), (41, 89)):
        g.set(x, y, "e")
        g.set(x + 1, y + 1, "A")


def livery(g):
    """The household's red down his front, a long tabard of it over the gown, edged with gilt."""
    roster.mass(g, [(38, 40, 47), (58, 39, 48), (90, 37, 50)], RED, RED_LIT, RED_SH, rim=False)
    roster.fold(g, [(44, 44), (43, 66), (43, 89)], RED_DP, RED_LIT)
    roster.fold(g, [(47, 62), (48, 88)], RED_DP, None)
    for y in range(38, 91):
        t = (y - 38) / 52.0
        lx = int(round(40 - 3 * t))
        rx = int(round(47 + 3 * t))
        g.set(lx - 1, y, BR_SH if y % 4 else BR)              # the gilt edge, a stop lit and a stop dark
        g.set(rx + 1, y, BR_DP if y % 4 else BR_SH)
    g.span(90, 36, 51, BR_SH)
    g.span(91, 36, 51, BR_DP)
    for x in range(37, 51, 3):
        g.set(x, 90, BR_LIT)


def girdle(g):
    """A black girdle at the waist, a gilt buckle, and the keys he does NOT carry - a plain purse."""
    g.span(56, 33, 53, "J")
    g.span(57, 33, 53, "o")
    g.span(55, 34, 52, "j")
    g.rect(41, 55, 45, 57, BR_SH)
    g.span(55, 41, 45, BR_LIT)
    g.set(43, 56, "o")
    g.set(45, 57, BR_DP)
    # a leather purse at the far hip, on its own strap
    g.rect(33, 58, 36, 63, "J")
    g.span(58, 33, 36, "N")
    g.set(33, 59, "p")
    g.set(36, 63, "o")
    g.set(34, 64, "J")


def near_arm(g):
    """The near arm out to the staff, the sleeve turned back in red, the hand closed on the wood."""
    for (a, b) in (((53, 31), (64, 46)), ((55, 30), (66, 45)), ((52, 33), (62, 48))):
        roster.shaft(g, a[0], a[1], b[0], b[1], "e", "E", "A", width=3)
    roster.fold(g, [(57, 34), (63, 44)], "A", "e")
    # the cuff turned back: red lining, a gilt edge
    for (x, y, ch) in ((62, 45, RED), (63, 45, RED_LIT), (64, 45, RED), (65, 45, RED_SH), (66, 45, RED_SH),
                       (62, 46, RED_SH), (63, 46, RED), (64, 46, RED), (65, 46, RED_SH), (66, 46, RED_DP),
                       (62, 47, RED_DP), (63, 47, RED_SH), (64, 47, RED_SH), (65, 47, RED_DP), (66, 47, "o"),
                       (61, 44, BR_SH), (62, 44, BR), (63, 44, BR), (64, 44, BR_SH), (65, 44, BR_SH)):
        g.set(x, y, ch)
    # THE HAND, round the staff: knuckles over the near side, the thumb across the front.
    for (x, y, ch) in ((66, 48, "n"), (67, 48, "k"), (68, 48, "s"), (69, 48, "k"),
                       (67, 49, "k"), (68, 49, "k"), (69, 49, "n"),
                       (68, 50, "n"), (69, 50, "s"), (70, 50, "k"), (71, 50, "k"), (72, 50, "n"),
                       (68, 51, "m"), (69, 51, "k"), (70, 51, "s"), (71, 51, "k"), (72, 51, "n"), (73, 51, "m"),
                       (69, 52, "m"), (70, 52, "n"), (71, 52, "n"), (72, 52, "m"),
                       (70, 53, "o"), (71, 53, "o")):
        g.set(x, y, ch)


def collar_and_chain(g):
    """A standing black collar, and over the shoulders the gold chain of office with its badge."""
    roster.mass(g, [(25, 38, 50), (28, 36, 52)], "N", "p", "j", rim=False)
    g.span(28, 37, 51, "o")
    # THE CHAIN: links that alternate a lit face and an edge, sagging from shoulder to breast.
    pts = []
    for i in range(0, 25):
        t = i / 24.0
        x = 31 + 24 * t
        y = 31 + 9 * (1 - (2 * t - 1) ** 2)
        pts.append((int(round(x)), int(round(y))))
    for i, (x, y) in enumerate(pts):
        face = i % 2 == 0
        g.set(x, y, BR_LIT if face else BR_SH)
        g.set(x, y + 1, BR_DP if face else "o")
        if face and i % 4 == 0:
            g.set(x, y - 1, BR_HI if x < CX else BR)
    # THE BADGE on the breast, hung from the lowest link: a gilt plate with a boss in it.
    bx, by = int(CX) - 2, 41
    g.rect(bx, by, bx + 5, by + 5, BR)
    g.span(by, bx, bx + 5, BR_LIT)
    g.line(bx, by, bx, by + 5, BR_LIT)
    g.line(bx + 5, by + 1, bx + 5, by + 5, BR_SH)
    g.span(by + 5, bx + 1, bx + 5, BR_DP)
    g.set(bx + 2, by + 2, BR_HI)
    g.set(bx + 3, by + 3, BR_SH)
    g.set(bx + 2, by + 3, BR_SH)
    g.span(by + 6, bx + 1, bx + 4, "o")


def head(g):
    """The face in the dark under the cap, the long white beard, and the stiff cap of office."""
    # The face: a narrow void, as on everything he took.
    SB.swell(g, 16, 25, [4.0, 4.5, 4.2, 3.6], "j", "N", "J", cx=CX + 1)
    roster.void(g, 17, 24, 3.2, 2.0, cx=CX + 1.5)
    # THE BEARD: long, narrow and forked, bone, lit on the key side, over the collar to the chain.
    roster.mass(g, [(23, 41, 47), (28, 41, 47), (33, 42, 47), (38, 43, 46)], "k", "s", "n", rim=False)
    for (x, y) in ((44, 34), (44, 35), (44, 36), (44, 37), (44, 38)):
        g.set(x, y, ".")                                     # the fork
    g.set(44, 33, "m")
    roster.fold(g, [(42, 25), (42, 36)], "n", "s")
    roster.fold(g, [(46, 25), (46, 31)], "n", None)
    for (x, y) in ((41, 30), (47, 28), (43, 27), (45, 32)):
        g.set(x, y, "S" if x < CX else "m")
    # the moustache, a bar above it, so the beard does not start at the chin like a bib
    g.span(23, 41, 47, "n")
    g.set(41, 23, "s")
    # THE CAP: a court's tall sugarloaf - a stiff cone of black velvet (stone's upper stops), rounded
    # at the crown, on a rolled brim. Tall, so his outline has its chimney; tapering, so it is a
    # cap of his century and never a stovepipe.
    roster.mass(g, [(3, 41, 47), (4, 40, 48), (6, 39, 49), (12, 38, 50)], "N", "p", "j")
    g.span(3, 42, 46, "P")
    g.set(41, 4, "P")
    for (x0, y0, x1, y1) in ((42, 5, 41, 12), (46, 5, 46, 12)):
        g.line(x0, y0, x1, y1, "j")                          # its stiff pleats
    g.line(43, 5, 43, 12, "p")
    g.set(48, 7, BR_HI)                                      # a jewel pinned on it
    g.set(48, 8, BR_SH)
    # the rolled brim, a stop lit along its top, a little wider than the crown
    g.span(12, 37, 51, "p")
    g.span(13, 37, 52, "N")
    g.span(14, 37, 52, "j")
    g.span(15, 38, 51, "o")
    for x in range(39, 51, 3):
        g.set(x, 13, "j")                                    # the roll's twists
    g.set(37, 13, "p")
    g.set(52, 14, "J")


def rim_left(g):
    """The 1 px key rim on the left edge of each row, where nothing stands beside it."""
    for y in range(2, FEET):
        for x in range(1, 66):
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
    # and the staff's own, all the way up its lit side
    for y in range(STAFF_TOP + 1, STAFF_FOOT - 4):
        if g.get(STAFF_X - 1, y) == ".":
            g.set(STAFF_X - 1, y, "W")


def seneschal():
    g = G(CELL)
    staff(g)                       # behind his hand, beside him
    far_sleeve(g)
    gown(g)
    livery(g)
    girdle(g)
    collar_and_chain(g)
    head(g)
    near_arm(g)
    # The shoe, peeking under the hem toward the staff.
    g.rect(50, FEET - 1, 55, FEET, "J")
    g.span(FEET - 1, 50, 54, "N")
    g.set(50, FEET - 1, "p")
    rim_left(g)
    roster.contact(g, 22, 76, y=FEET)
    roster.eyes(g, 20, int(CX), int(CX) + 3, "Y", "y")
    return g.rows()


def build(root=ROOT):
    pose = seneschal()
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


def preview(pose):
    black = ["".join("o" if ch != "." else "." for ch in row) for row in pose]
    P.preview([("pose", [pose]), ("silhouette", [black])], dict(PALETTE, o="#05070C"),
              os.path.join(HERE, "_boss_seneschal.png"), zoom=4, bg=(30, 34, 46), cell=CELL)


def main():
    if "--preview" in sys.argv:
        pose = seneschal()
        problems = P.check_anchor("%s pose" % STEM, [pose], CELL)
        n = 0
    else:
        pose, n, problems = build()
    filled, edges, ratio = roster.density(pose)
    print("%s  %dx%d  %d px  %.2f edges/px%s  %d frames -> scenes/resources/%s.tres"
          % (STEM, CELL, CELL, filled, ratio, "" if ratio >= 0.80 else "   SPARSE", n, SHEET))
    preview(pose)
    if problems:
        print("ANCHOR PROBLEMS:")
        for p in problems:
            print("   " + p)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
