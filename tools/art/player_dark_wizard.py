# -*- coding: utf-8 -*-
"""The dark wizard, playable (.ai/dark-wizard-playable.md). Run: python tools/art/player_dark_wizard.py

    assets/bonelight/characters/dark-wizard-{1,2,3,4}.png      the idle, frame one is the portrait
    assets/bonelight/characters/dark-wizard-{idle,moving,hurt,death}.png
    scenes/resources/DarkWizardFrames.tres
    tools/art/_player_dark_wizard.png     (gitignored) the pose at x8, filled black, and every animation

    --preview   writes only the preview; nothing under assets/ or scenes/ is touched.

HIS SHAPE IS THE BOSS'S (tools/art/boss_dark_wizard.py, itself the prologue's), REDRAWN ON THE 48 CELL -
not scaled down. A cowled spire in a collar of iron points, ONE ARM RAISED with its sleeve hanging off the
forearm like a wing, the warm light he took in that hand, keys at his belt, and NO FEET: the hem goes on
into the ground as shadow. Every row of the 96 boss was halved and then redrawn at the size it lands at,
because halving a 96 cell by averaging is a smear and by dropping rows is a stutter.

He is the player now, but he is still the one figure in the game who sheds DARK light
(.ai/world-and-tone.md): the darkness ramp as emission on the eclipse rim behind his head, the two points
of his eyes and the edge of the shadow running out of his hem, and the violet pigment row (his cloak's
deep half) for what it lands on. The light in his hand is the light ramp - not his - as on the boss.

HIS WALK has no legs to move. The hem and the shadow under it do the walking: the bottom of the cloak
swings toward the way he goes (roster.sway) and the shadow's tendrils run out ahead and draw in behind,
so a figure carried by his shadow reads as carried rather than sliding.

Two limits from the derived animations (roster.py's): nothing above row 2 (the idle and the walk bob
the top up) and nothing left of column 4 (the hurt knocks the cell two left and shears the top two more).
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

G = roster.G
PALETTE = roster.PALETTE
CELL = roster.WIZARD_CELL
FEET = CELL - 2
STEM = "dark-wizard"
SHEET = "DarkWizardFrames"
OUT_CHARS = os.path.join("assets", "bonelight", "characters")

DK0, DK1, DK2, DK3 = roster.DK0, roster.DK1, roster.DK2, roster.DK3
LT0, LT1, LT2, LT3 = roster.LT0, roster.LT1, roster.LT2, roster.LT3

# The cloak's middle line, the eclipse behind the head, the light in the raised hand, the belt.
# CharacterVisuals.GetStaffTipCell("dark_wizard") is ORB: The Taking's hands leave from there.
CX = 25.0
ECLIPSE = (25, 11)
ORB = (11, 5)
WAIST = 29


def eclipse(g):
    """The ring of his dark light behind his head, burning to its core at the upper left (toward the key
    light), drawn first so the collar and the cowl cover most of it - the prologue's black sun, worn."""
    cx, cy = ECLIPSE
    for y in range(cy - 9, cy + 10):
        for x in range(cx - 9, cx + 10):
            d = math.hypot(x - cx, y - cy)
            if 6.1 <= d < 8.2:
                ch = DK3 if d >= 7.3 else DK2
                if d < 6.8:
                    ch = DK0 if (x < cx - 2 and y < cy - 2) else DK1
                g.set(x, y, ch)


def collar(g):
    """The collar of iron points behind the cowl: two fans of spikes off his shoulders leaning out from his
    head, the middle ones tallest, a throne's back worn as a coat. Stone, lit on the key side."""
    # (tip x, tip y, base x, base half-width): each spike leans from its base out to its tip.
    spikes = [(15, 11, 19, 1.3), (18, 5, 21, 1.6), (21, 8, 22.5, 1.1),
              (30, 8, 28.5, 1.1), (33, 4, 30, 1.6), (36, 10, 32, 1.3)]
    base = 17
    for (tx, ty, bx, half) in spikes:
        for y in range(ty, base + 1):
            t = (y - ty) / float(base - ty)
            c = tx + (bx - tx) * t
            w = 0.2 + half * t
            lo, hi = int(round(c - w)), int(round(c + w))
            if g.get(lo - 1, y) == ".":
                g.set(lo - 1, y, "W")
            g.span(y, lo, hi, "j")
            g.set(lo, y, "p")
            if hi - lo >= 2:
                g.set(hi, y, "J")
        g.set(tx, ty, "P")
        # A seam a stop darker down the longer ones, so a spike is a blade and not a bar.
        if base - ty >= 8:
            for y in range(ty + 3, base - 1, 2):
                t = (y - ty) / float(base - ty)
                g.set(int(round(tx + (bx - tx) * t)), y, "N")
    # The stiff band the points stand from, round the shoulders under the cowl.
    roster.mass(g, [(15, 17, 33), (17, 16, 34)], "j", "p", "J")
    for x in range(18, 33, 2):
        g.set(x, 15, "J" if x % 4 else "N")
    g.span(17, 16, 34, "o")


def sleeve(g):
    """The raised arm's sleeve: a wing of cloth hanging off the forearm from the wrist back to the
    shoulder, its hanging edge clear of his side and cut into dags."""
    roster.mass(g, [(8, 12, 14), (11, 10, 15), (15, 8, 17), (18, 7, 17), (21, 7, 15), (24, 8, 13), (26, 9, 11)],
                "x", "w", "X")
    roster.fold(g, [(12, 9), (10, 16), (9, 22)], "X", "w")
    roster.fold(g, [(14, 12), (12, 18), (11, 24)], "X", "w")
    # the lining where the cloth turns at its inner edge: a stop darker
    for y in range(16, 24):
        x = 15 - (y - 16) // 3
        if g.get(x, y) not in (".", "W"):
            g.set(x, y, "X")
    # the dags at its hanging end
    for (x, depth) in ((8, 1), (9, 0), (10, 2), (11, 0), (12, 1)):
        for k in range(depth):
            roster.cut(g, (x, 26 - k))
        g.set(x, 26 - depth, "X" if depth else "o")
    # its key-side edge, catching the orb's light
    for y in (9, 12, 15, 18):
        x = 13 - (y - 8) * 5 // 10
        if g.get(x, y) not in (".", "W"):
            g.set(x, y, "v")


def raised_arm(g):
    """The wrist out of the sleeve, bone pale and long, and the hand open under the light he took."""
    roster.shaft(g, 14, 9, 12, 7, "s", "k", "n", width=2)
    g.span(9, 13, 15, "x")
    # the hand: a palm under the orb and three long fingers curled up round its sides, lit from it
    g.set(11, 8, "k")
    g.set(12, 8, "s")
    g.set(10, 8, "n")
    for (x, y, ch) in ((8, 7, "k"), (8, 6, "s"), (14, 6, "k"), (14, 5, "s"), (13, 7, "k")):
        g.set(x, y, ch)


def orb(g):
    """The warm light he took, in his hand: the light ramp, its core up and to the left. Not his own."""
    ox, oy = ORB
    g.disc(ox, oy, 2.4, LT3)
    g.disc(ox, oy, 1.6, LT2)
    g.set(ox, oy, LT1)
    g.set(ox - 1, oy - 1, LT1)
    g.set(ox - 1, oy, LT0)
    g.set(ox, oy - 1, LT0)


def cloak(g):
    """The cloak from the collar to the ground: violet in its deep half, folds fanning to the hem."""
    widths = [5.0, 6.0, 6.5, 7.0, 7.8, 8.8, 9.8, 10.8, 11.6]
    y0, y1 = 16, FEET - 1
    n = len(widths) - 1
    for y in range(y0, y1 + 1):
        t = (y - y0) / float(y1 - y0) * n
        i = min(n - 1, int(t))
        hw = widths[i] + (widths[i + 1] - widths[i]) * (t - i)
        lo, hi = int(round(CX - hw)), int(round(CX + hw))
        # No rim in the gap between the wing and the body above the belt: a lit line there, beside the
        # face, read as a staff he does not carry.
        if g.get(lo - 1, y) == "." and y > WAIST - 3:
            g.set(lo - 1, y, "W")
        g.span(y, lo, hi, "x")
        g.set(lo, y, "w")
        g.set(lo + 1, y, "w")
        g.set(hi, y, "X")
        g.set(hi - 1, y, "X")
    for pts in ([(21, 18), (19, 30), (16, 45)], [(23, 19), (22, 32), (20, 45)],
                [(27, 18), (28, 31), (31, 45)], [(29, 20), (31, 33), (34, 44)],
                [(19, 22), (17, 35), (14, 44)]):
        roster.fold(g, pts, "X", "w")
    # A few lit ridges on the key side: one stop up, never the light end of the ramp.
    for (x, y) in ((15, 42), (16, 38), (18, 31), (17, 34), (19, 26), (14, 44), (20, 23)):
        if g.get(x, y) not in (".", "W"):
            g.set(x, y, "v")
    # The weave of heavy cloth: a sparse, irregular break of deep into the shade, thicker down the
    # shadowed side and toward the hem, so the mass has an inside without banding.
    for y in range(19, FEET - 2):
        for x in range(12, 38):
            if g.get(x, y) != "x":
                continue
            side = (x - CX) / 11.0
            h = (x * 37 + y * 61 + (x * y) % 7) % 23
            if h < 2 + int(3 * max(0.0, side)) + (y - 19) // 10:
                g.set(x, y, "X")


def robe(g):
    """The robe in the cloak's opening: near-black stone, a dull gold edge down each side."""
    for y in range(18, FEET - 2):
        t = (y - 18) / float(FEET - 20)
        lx = int(round(CX - 1.0 - 2.0 * t))
        rx = int(round(CX + 1.0 + 2.2 * t))
        g.span(y, lx, rx, "J")
        if rx - lx >= 3:
            g.set(lx + 1, y, "j" if y % 5 else "N")
        g.set(lx - 1, y, "g" if y % 4 else "G")
        g.set(rx + 1, y, "u")


def belt_and_keys(g):
    """A black belt, a gilt buckle, and at his far hip his other hand round a ring of keys."""
    g.span(WAIST, 18, 32, "J")
    g.span(WAIST + 1, 18, 32, "o")
    g.span(WAIST - 1, 19, 31, "j")
    g.rect(24, WAIST - 1, 26, WAIST, "u")
    g.set(25, WAIST - 1, "G")
    # the hand out of the cloak at his side, long fingers down round the ring
    g.span(WAIST + 1, 30, 32, "k")
    g.set(30, WAIST + 1, "s")
    for (x, ln) in ((30, 2), (31, 3), (32, 2)):
        g.line(x, WAIST + 2, x, WAIST + 1 + ln, "n" if x % 2 else "m")
    # the ring and three keys on it, iron, the middle one catching the light
    for (dx, dy) in ((-1, 0), (0, -1), (1, 0), (0, 1)):
        g.set(31 + dx, WAIST + 5 + dy, "R" if dy < 1 else "q")
    for (kx, ln, bit) in ((30, 3, -1), (31, 5, 1), (33, 3, 1)):
        g.line(kx, WAIST + 6, kx, WAIST + 6 + ln, "r")
        g.set(kx + bit, WAIST + 6 + ln, "R")
    g.set(31, WAIST + 7, "T")


def cowl(g):
    """The cowl, peaked and leaning back, the face set deep inside it, the iron circlet over it."""
    roster.mass(g, [(5, 26, 27), (7, 24, 28), (9, 22, 29), (12, 21, 30), (15, 21, 30), (18, 20, 30)],
                "x", "w", "X")
    roster.fold(g, [(26, 6), (23, 11), (22, 16)], "X", "w")
    roster.fold(g, [(28, 8), (29, 13), (29, 17)], "X", None)
    g.set(26, 5, "v")
    g.set(25, 6, "v")
    # The opening and the dark inside it.
    roster.void(g, 11, 17, 2.4, 1.6, cx=CX + 0.3)
    for y in range(11, 17):
        g.set(int(CX + 0.3 - 2.4 + (y - 11) * 0.12), y, "X")
    # What the orb finds of his face: the pale edge of a cheek and the chin, deep in the hood.
    for (x, y, ch) in ((23, 14, "m"), (23, 15, "n"), (24, 16, "m")):
        g.set(x, y, ch)
    # The circlet: a thin line of iron across the brow of the cowl, three points.
    for x in range(22, 29):
        g.set(x, 10, "p" if x < 25 else "N")
    for (x, h) in ((23, 1), (25, 2), (27, 1)):
        for k in range(1, h + 1):
            g.set(x, 10 - k, "P" if k == h else "p")


def hem_into_shadow(g, phase):
    """No feet: the hem goes on into the ground as shadow. The last rows of cloak go out to occlusion in
    a dither, and tendrils of the shadow run off along the floor either side, his dark light at their
    edges. Walking (`phase`), the tendrils on the side he goes run out and the ones behind draw in."""
    for y in range(FEET - 7, FEET + 1):
        f = (y - (FEET - 7)) / 7.0
        for x in range(4, 46):
            ch = g.get(x, y)
            if ch in (".", "W"):
                continue
            if ((x * 7 + y * 3) % 10) / 10.0 < f * 0.85:
                g.set(x, y, "o")
    c = 0.0 if phase is None else math.cos(2.0 * math.pi * phase)
    ahead = int(round(2.5 * (1.0 + c)))      # 0..5: the tendril the way he goes runs out
    behind = int(round(2.5 * (1.0 - c)))
    tendrils = [(7 - behind // 2, 15, FEET), (35, 41 + ahead // 2, FEET),
                (10, 14, FEET - 1), (36, 39 + ahead // 3, FEET - 1)]
    for (x0, x1, y) in tendrils:
        for x in range(max(4, x0), min(46, x1) + 1):
            g.set(x, y, "o")
    for (x, y, ch) in ((max(4, 7 - behind // 2), FEET, DK3), (min(45, 41 + ahead // 2), FEET, DK3),
                       (10, FEET - 1, DK3), (min(45, 39 + ahead // 3), FEET - 1, DK3),
                       (12, FEET, DK2), (38, FEET, DK2)):
        g.set(x, y, ch)


def dark_wizard(phase=None):
    """The Taking. The dark wizard, taken on the hardest road: the boss's silhouette on the player's cell.
    `phase` (0..1) is the walk; None is the approved pose the idle and the portrait are built from."""
    g = G(CELL)
    eclipse(g)
    collar(g)
    sleeve(g)
    cloak(g)
    robe(g)
    belt_and_keys(g)
    if phase is not None:
        # The hem swings toward the way he goes and back: a pendulum hung from the belt.
        roster.sway(g, WAIST + 3, 1.6 * math.sin(2.0 * math.pi * phase), FEET - 1)
    cowl(g)
    raised_arm(g)
    orb(g)
    hem_into_shadow(g, phase)
    roster.contact(g, 9, 40)
    # THE EYES, last: two points of his own dark light in the void, as the prologue lit them.
    g.set(24, 13, DK0)
    g.set(26, 13, DK0)
    g.set(24, 14, DK3)
    g.set(26, 14, DK3)
    return g.rows()


def build(root=ROOT):
    pose = dark_wizard()
    idle = roster.idle(pose)
    problems = P.check_anchor("%s pose" % STEM, [pose], CELL)
    P.write_frames(idle, PALETTE, os.path.join(root, OUT_CHARS, STEM + "-%d.png"), CELL)
    anims = anim_sets.player_set(pose, idle, dark_wizard)
    spec = []
    for anim, frames, loop, speed in anims:
        png = "%s-%s.png" % (STEM, anim)
        P.write_strip(frames, PALETTE, os.path.join(root, OUT_CHARS, png), CELL)
        spec.append((anim, png, len(frames), loop, speed))
        if anim in ("idle", "moving"):
            problems += P.check_anchor("%s %s" % (STEM, anim), frames, CELL)
    spriteframes.write(os.path.join(root, "scenes", "resources", SHEET + ".tres"), spec,
                       OUT_CHARS.replace(os.sep, "/"), cell=CELL)
    return pose, anims, problems


def preview(pose, anims):
    black = ["".join("o" if ch != "." else "." for ch in row) for row in pose]
    sets = [("pose", [pose]), ("silhouette", [black])]
    for anim, frames, _loop, _speed in anims:
        sets.append((anim, frames))
    P.preview(sets, dict(PALETTE, o="#05070C"), os.path.join(HERE, "_player_dark_wizard.png"),
              zoom=8, bg=(30, 34, 46), cell=CELL)


def main():
    if "--preview" in sys.argv:
        pose = dark_wizard()
        anims = anim_sets.player_set(pose, roster.idle(pose), dark_wizard)
        problems = P.check_anchor("%s pose" % STEM, [pose], CELL)
    else:
        pose, anims, problems = build()
    filled, edges, ratio = roster.density(pose)
    print("%s  %dx%d  %d px  %.2f edges/px%s  -> scenes/resources/%s.tres"
          % (STEM, CELL, CELL, filled, ratio, "" if ratio >= 0.80 else "   SPARSE", SHEET))
    preview(pose, anims)
    if problems:
        print("ANCHOR PROBLEMS:")
        for p in problems:
            print("   " + p)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
