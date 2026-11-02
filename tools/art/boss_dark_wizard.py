# -*- coding: utf-8 -*-
"""The Throne's boss: the dark wizard (.ai/endgame-castle.md 6.4). Run: python tools/art/boss_dark_wizard.py

    assets/bonelight/enemies/throne-dark-wizard-{moving,attack,hurt,death}.png
    scenes/resources/ThroneDarkWizardFrames.tres
    tools/art/_boss_dark_wizard.png       (gitignored) the pose at x4, and filled black

    --preview   writes only the preview, for drawing; nothing under assets/ or scenes/ is touched.

Never named on screen. Drawn on the 96 boss cell like every chapter boss, in its own module so the
Throne never edits sprite_bosses.py.

HIS OUTLINE IS THE PROLOGUE'S (.ai/prologue.md, panel 2; tools/art/prologue.py, dark_wizard): the one
shape the player has already seen, filled black, now with its inside. A tall cowled spire inside a
COLLAR OF POINTS - a throne's back worn as a coat - and ONE ARM RAISED, its sleeve hanging off the
forearm like a wing with air between it and his side; in that hand, the light he is taking. No hat brim
and no staff: those are the player's. At his belt, keys. NO FEET: the hem goes on into the ground as
shadow, and the shadow is what serves him. Filled black against the other bosses' T, bell, dome, slab,
wedge, S and H, it is a spire with a wing: nothing else on the road has an arm up.

THE ONE SOURCE OF DARK LIGHT (.ai/world-and-tone.md, "The dark wizard is the exception that proves it").
The darkness ramp is used as EMISSION on him and on nothing else in the game: the eclipse round his head,
the eyes in the cowl, the shadow running out of his hem. Everything it lands on takes the violet PIGMENT
row - his cloak, in its deep half - as the ward's floor takes gold on the title (emissive ramps belong
on the emitter). The light in his raised hand is not his: it is the warm light he took, in the light
ramp, and it is what leaves his body as a storm of motes when he falls (ThroneDarkWizardBoss's fall).

THE FACE is set deep in the cowl and mostly not there: a void, the two points of his dark light in it,
and under them only the pale edge of a gaunt cheek and chin that the orb in his hand finds. The prologue
showed no face at all; the Throne is where his inside is revealed, and a little of it is enough.
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

G = roster.G
PALETTE = roster.PALETTE
CELL = 96
FEET = CELL - 2
STEM = "throne-dark-wizard"
SHEET = "ThroneDarkWizardFrames"
OUT_ENEMIES = os.path.join("assets", "bonelight", "enemies")

DK0, DK1, DK2, DK3 = roster.DK0, roster.DK1, roster.DK2, roster.DK3
LT0, LT1, LT2, LT3 = roster.LT0, roster.LT1, roster.LT2, roster.LT3

# Boss feel: he does not walk, he is carried by his shadow - a small lean; he throws his hand with his
# whole height; he barely flinches.
FEEL = (0.4, 3.5, 1)

# The middle line of the cloak and the head over it. ThroneDarkWizardBoss's arrival and fall cut the
# body at these rows, so the two must agree: the raised hand's orb, the eclipse's centre, the waist.
CX = 47.0
ORB = (19, 7)
ECLIPSE = (47, 20)
WAIST = 56


def eclipse(g):
    """The ring of his own dark light behind his head: edge outside, mid, a hot inner rim, the core only
    where the ring is lit hardest (upper left, toward the key). Drawn first, so the collar and the cowl
    cover most of it and it shows as a rim round them - the prologue's black sun, worn."""
    cx, cy = ECLIPSE
    for y in range(cy - 18, cy + 19):
        for x in range(cx - 18, cx + 19):
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
            if 14.6 <= d < 17.0:
                ch = DK3 if d >= 16.0 else DK2
                # The inner rim is hot all the way round and burns to its core at the upper left.
                if d < 15.4:
                    ch = DK0 if (x < cx - 4 and y < cy - 4) else DK1
                g.set(x, y, ch)


def collar(g):
    """The collar of points, behind the cowl: two fans of iron-dark spikes standing off his shoulders and
    leaning out from his head, the middle ones tallest, so the head sits in a crown of them - a throne's
    back worn as a coat. Stone, lit on the key side."""
    # (tip x, tip y, base x, base half-width): each spike leans from its base out to its tip.
    spikes = [(29, 17, 35, 2.4), (33, 8, 38, 2.8), (38, 13, 41, 2.2),
              (56, 14, 53, 2.2), (61, 7, 57, 2.8), (65, 16, 60, 2.4)]
    for (tx, ty, bx, half) in spikes:
        for y in range(ty, 31):
            t = (y - ty) / float(31 - ty)
            c = tx + (bx - tx) * t
            w = 0.3 + half * t
            lo, hi = int(round(c - w)), int(round(c + w))
            if g.get(lo - 1, y) == ".":
                g.set(lo - 1, y, "W")
            g.span(y, lo, hi, "j")
            g.set(lo, y, "p")
            if hi - lo >= 2:
                g.set(hi, y, "J")
        g.set(tx, ty, "P")
        # a seam down each, a stop darker, so a spike is a blade and not a bar
        for y in range(ty + 3, 29, 2):
            t = (y - ty) / float(31 - ty)
            g.set(int(round(tx + (bx - tx) * t)), y, "N")
    # The band they stand from: a stiff collar round the shoulders, under the cowl.
    roster.mass(g, [(27, 32, 62), (32, 30, 64)], "j", "p", "J")
    for x in range(33, 62, 3):
        g.set(x, 29, "J" if x % 2 else "N")
    g.span(32, 31, 63, "o")


def sleeve(g):
    """The raised arm's sleeve: a wing of cloth hanging off the forearm, from the wrist back to the
    shoulder, its hanging edge clear of his side, dagged."""
    roster.mass(g, [(13, 20, 24), (20, 16, 27), (28, 13, 31), (33, 12, 29), (40, 12, 25), (47, 14, 21), (50, 16, 19)],
                "x", "w", "X")
    for pts in ([(22, 15), (18, 30), (16, 45)], [(26, 21), (22, 34), (20, 47)], [(29, 27), (25, 36), (23, 42)]):
        roster.fold(g, pts, "X", "w")
    # the lining, where the cloth turns at the inner edge: his deep violet, a stop darker
    for y in range(30, 46):
        x = 28 - (y - 30) // 4
        if g.get(x, y) not in (".", "W"):
            g.set(x, y, "X")
    # the dags at its hanging end, cut back to nothing between the teeth
    for x in range(13, 22):
        depth = (x * 3) % 4
        for k in range(depth):
            roster.cut(g, (x, 50 - k))
        g.set(x, 50 - depth, "X" if depth else "o")
    # its edge on the key side, catching the orb's light
    for y in range(14, 46, 3):
        x = 20 - (y - 13) * 8 // 27 if y < 40 else 12
        if g.get(x, y) not in (".", "W"):
            g.set(x, y, "v")
    # the forearm under the cloth: a ridge from the wrist toward the shoulder
    roster.fold(g, [(23, 14), (27, 20), (31, 27)], "X", "v", side=(0, -1))


def raised_arm(g):
    """The wrist out of the sleeve, bone pale and long, the hand open round the light he took."""
    roster.shaft(g, 23, 14, 21, 11, "s", "k", "n", width=3)
    g.span(14, 20, 25, "x")
    g.span(15, 20, 25, "X")
    # the hand: a palm and five long fingers curled round the orb, lit from it
    g.disc(20.5, 10.5, 1.6, "k")
    for (x0, y0, x1, y1) in ((19, 10, 15, 6), (20, 9, 17, 4), (21, 9, 20, 3), (22, 9, 23, 4), (22, 11, 25, 8)):
        g.line(x0, y0, x1, y1, "k")
        g.set(x1, y1, "s")
    g.set(18, 11, "n")
    g.set(22, 12, "n")


def orb(g):
    """The warm light he took, in his hand: the light ramp, core in the middle. Not his own."""
    ox, oy = ORB
    g.disc(ox, oy, 3.2, LT3)
    g.disc(ox, oy, 2.4, LT2)
    g.disc(ox - 0.4, oy - 0.4, 1.4, LT1)
    g.set(ox - 1, oy - 1, LT0)
    g.set(ox, oy - 1, LT0)


def cloak(g):
    """The cloak from the collar to the ground: violet in its deep half, folds fanning to the hem."""
    SB.swell(g, 30, FEET - 2, [10.5, 12.5, 13.0, 14.0, 15.5, 17.5, 19.5, 21.5, 23.0], "x", "w", "X", cx=CX)
    folds = [((41, 36), (37, 60), (31, 90)), ((44, 38), (42, 64), (39, 91)), ((52, 36), (55, 62), (60, 90)),
             ((55, 40), (59, 66), (65, 89)), ((38, 44), (32, 70), (27, 88)), ((49, 60), (50, 90))]
    for pts in folds:
        roster.fold(g, list(pts), "X", "w")
    # A few lit ridges on the key side: one stop up, never the light end of the ramp.
    for (x, y) in ((27, 84), (29, 76), (33, 62), (31, 68), (35, 52), (26, 89), (36, 46)):
        if g.get(x, y) not in (".", "W"):
            g.set(x, y, "v")
    for (x, y) in ((40, 70), (39, 80), (57, 72), (62, 82), (54, 52)):
        if g.get(x, y) not in (".", "W"):
            g.set(x, y, "w")
    # The weave of heavy cloth: a sparse, irregular break of deep into the shade, thicker down the
    # shadowed side and toward the hem, so the mass has an inside without banding.
    for y in range(36, FEET - 4):
        for x in range(24, 72):
            if g.get(x, y) != "x":
                continue
            side = (x - CX) / 22.0
            h = (x * 37 + y * 61 + (x * y) % 7) % 23
            if h < 2 + int(3 * max(0.0, side)) + (y - 36) // 20:
                g.set(x, y, "X")


def robe(g):
    """The robe in the cloak's opening: near-black stone, a dull gold edge down each side."""
    for y in range(34, FEET - 3):
        t = (y - 34) / float(FEET - 37)
        lx = int(round(CX - 2.0 - 4.0 * t))
        rx = int(round(CX + 2.0 + 4.5 * t))
        g.span(y, lx, rx, "J")
        g.set(lx + 1, y, "j" if y % 5 else "N")
        g.set(rx - 1, y, "J")
        g.set(lx - 1, y, "g" if y % 4 else "G")
        g.set(rx + 1, y, "u")
        if y % 7 == 3 and rx - lx > 4:
            g.set(lx + 2 + (y // 7) % 3, y, "j")


def belt_and_keys(g):
    """A black belt, a gilt buckle, and at his far hip his other hand round a ring of keys."""
    g.span(WAIST, 35, 59, "J")
    g.span(WAIST + 1, 35, 59, "o")
    g.span(WAIST - 1, 36, 58, "j")
    g.rect(45, WAIST - 1, 49, WAIST + 1, "u")
    g.span(WAIST - 1, 45, 49, "G")
    g.set(47, WAIST, "o")
    # The hand out of the cloak at his side, long fingers down round the ring.
    roster.mass(g, [(52, 58, 62), (56, 59, 63)], "n", "k", "m", rim=False)
    for (x, ln) in ((59, 4), (60, 5), (61, 5), (62, 4)):
        g.line(x, 57, x, 57 + ln, "n" if x % 2 else "m")
    g.set(58, 57, "k")
    # The ring and three keys on it, iron, one of them catching the light.
    for (dx, dy) in ((-2, 0), (-1, -1), (0, -1), (1, -1), (2, 0), (2, 1), (1, 2), (0, 2), (-1, 2), (-2, 1)):
        g.set(61 + dx, 64 + dy, "R" if dy < 1 else "q")
    for (kx, ln, bit) in ((59, 7, -1), (61, 9, 1), (63, 6, 1)):
        g.line(kx, 66, kx, 66 + ln, "r")
        g.set(kx + 1, 66, "q")
        g.set(kx + bit, 66 + ln, "R")
        g.set(kx + bit, 66 + ln - 1, "r")
        g.set(kx, 67, "T" if kx == 61 else "r")


def cowl(g):
    """The cowl, peaked and leaning back, the face set deep inside it, the iron circlet over it."""
    roster.mass(g, [(9, 49, 50), (12, 46, 52), (16, 42, 54), (22, 40, 55), (29, 39, 55), (34, 38, 56)], "x", "w", "X")
    roster.fold(g, [(48, 11), (44, 20), (42, 31)], "X", "w")
    roster.fold(g, [(52, 14), (53, 24), (54, 32)], "X", None)
    g.set(49, 9, "v")
    g.set(48, 10, "v")
    # The opening and the dark inside it.
    roster.void(g, 18, 30, 4.4, 3.0, cx=CX + 0.5)
    for y in range(19, 30):
        g.set(int(CX + 0.5 - 4.4 + (y - 18) * 0.12), y, "X")
    # What the orb finds of his face: the pale edge of a cheek and the chin, deep in the hood.
    for (x, y, ch) in ((44, 24, "J"), (44, 25, "m"), (44, 26, "n"), (45, 27, "m"), (46, 28, "J")):
        g.set(x, y, ch)
    # The circlet: a thin line of iron across the brow of the cowl, three points.
    for x in range(42, 54):
        g.set(x, 17, "p" if x < 47 else "N")
        g.set(x, 18, "J")
    for (x, h) in ((44, 1), (47, 2), (50, 1)):
        for k in range(1, h + 1):
            g.set(x, 17 - k, "P" if k == h else "p")


def hem_into_shadow(g):
    """No feet: the hem goes on into the ground as shadow. The last rows of cloak go out to occlusion
    in a dither, and tendrils of the shadow run off along the floor either side, his dark light at their
    edges - the shadow that serves him."""
    for y in range(FEET - 10, FEET):
        f = (y - (FEET - 10)) / 10.0
        for x in range(10, 86):
            ch = g.get(x, y)
            if ch in (".", "W"):
                continue
            if ((x * 7 + y * 3) % 10) / 10.0 < f * 0.85:
                g.set(x, y, "o")
    # the tendrils, along the ground row and the one above it
    for (x0, x1, y) in ((14, 30, FEET), (62, 80, FEET), (20, 28, FEET - 1), (66, 76, FEET - 1), (8, 16, FEET)):
        for x in range(x0, x1 + 1):
            g.set(x, y, "o")
    for (x, y) in ((14, FEET), (8, FEET), (80, FEET), (20, FEET - 1), (76, FEET - 1), (24, FEET), (72, FEET)):
        g.set(x, y, DK3)
    g.set(11, FEET, DK2)
    g.set(78, FEET, DK2)


def dark_wizard():
    g = G(CELL)
    eclipse(g)
    collar(g)
    sleeve(g)
    cloak(g)
    robe(g)
    belt_and_keys(g)
    cowl(g)
    raised_arm(g)
    orb(g)
    hem_into_shadow(g)
    roster.contact(g, 8, 84, y=FEET)
    # THE EYES, last: two points of his own dark light each, core and mid, as the prologue lit them.
    for (x, ch) in ((44, DK2), (45, DK0), (49, DK0), (50, DK2)):
        g.set(x, 22, ch)
    g.set(45, 23, DK3)
    g.set(49, 23, DK3)
    return g.rows()


def animations(pose):
    """The contract's four, derived (anim_sets), with the darkness and light ramps stepped too: the
    player ramps' positions, since the enemy table has neither and his light would stay full-bright
    through a flinch and a fall."""
    lean, reach, knock = FEEL
    pos = anim_sets._PLAYER_POS
    return [
        ("moving", anim_sets.moving(pose, lean), True, 8.0),
        ("attack", anim_sets.attack(pose, reach), False, 10.0),
        ("hurt", anim_sets.hurt(pose, knock, pos), False, 12.0),
        ("death", anim_sets.death(pose, pos), False, 10.0),
    ]


def build(root=ROOT):
    pose = dark_wizard()
    problems = P.check_anchor("%s pose" % STEM, [pose], CELL)
    spec = []
    for anim, frames, loop, speed in animations(pose):
        png = "%s-%s.png" % (STEM, anim)
        P.write_strip(frames, PALETTE, os.path.join(root, OUT_ENEMIES, png), CELL)
        spec.append((anim, png, len(frames), loop, speed))
    spriteframes.write(os.path.join(root, "scenes", "resources", SHEET + ".tres"), spec,
                       OUT_ENEMIES.replace(os.sep, "/"), cell=CELL)
    return pose, sum(a[2] for a in spec), problems


def preview(pose):
    black = ["".join("o" if ch != "." else "." for ch in row) for row in pose]
    P.preview([("pose", [pose]), ("silhouette", [black])], dict(PALETTE, o="#05070C"),
              os.path.join(HERE, "_boss_dark_wizard.png"), zoom=4, bg=(30, 34, 46), cell=CELL)


def main():
    if "--preview" in sys.argv:
        pose = dark_wizard()
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
