# -*- coding: utf-8 -*-
"""The White Downs' boss: the Unstrung Hunter (.ai/second-half.md 4.2). Run: python tools/art/boss_unstrung_hunter.py

    assets/bonelight/enemies/downs-unstrung-hunter-{moving,attack,hurt,death}.png
    scenes/resources/DownsUnstrungHunterFrames.tres
    tools/art/_boss_unstrung_hunter.png      (gitignored) the pose at x4, and filled black
    tools/art/_downs_boss/                   (gitignored) every strip at x3, and the figure at x2 on floors

"The hunter the sky lost: it draws on you along a line that follows you - put a barrow between you."
Orion's shadow. The sky's hunter was lost when the gods' seats went dark, and what walks the downs
in his clothes is the dark wizard's shadow: a taken thing, so its body is shadow and its light is
his one cold mark. Drawn on the 96 boss cell like the other chapter bosses (sprite_bosses.py), and
its own module so this chapter never edits that file.

ITS OUTLINE: A TALL LEAN FIGURE BESIDE A ")" . The other bosses are a T, a bell, a dome, a slab with
tablets, a wedge, an S, an H, a figure behind a door (the Hayward), a standing stone, a post with a
flag and a hoop (the Constable). This is a narrow hunter and, on the side he faces, a great bow
taller than he is, held upright at arm's length with daylight between him and it - an arc, curving
away from him, its tips flicking forward in two horn ears. Filled black the read is a man and a
bracket. The Hayward also carries a thing before him, but his is a solid panel against his body;
this is a thin curve standing clear of it, and nothing in the game is shaped like it.

THE BOW IS UNSTRUNG. It has no gut string: what spans it tip to tip is a single thread of his cold
light (arcane's edge stop at rest, the dimmest of the ramp, a hair of violet), and that is what he
draws. The draw is the attack (`attack`, below): he goes down on one knee - a bow taller than he is
cannot be drawn standing inside the cell, and an archer's kneel is the clearest tell the fight can
give - raises the bow, takes the thread at the grip and pulls it back to his ear, and the thread
brightens up the ramp as it comes (edge, mid, hot, and the core at his fingers). At full draw a bolt
of the same light lies on it. Then the loose: the thread snaps forward past straight, his hand flies
open behind his head.

WHAT HE IS MADE OF, by world-and-tone.md. A shadow, so the body is wool's deep indigo - the colour
the shadows are in this game - in the clothes of what he took: a lion's pelt (Orion's) worn as a
hood, its upper jaw over his brow, its mane round his shoulders, its forelegs knotted at his chest
with the paws hanging, its hide down his back to the knee, its tail behind him. The pelt is grave
linen from its deep half: a tawny hide gone the grey of the downs at night, never gold (a warm glint
on these downs is a secret's tell, .ai/stage-secrets.md). A hunter's leather kilt in strips (steel's
deep half), high laced boots (stone), an archer's bracer on the bow arm. The bow is horn, bone's
middle stops: the one pale thing on him, because it is the thing to read. His mark is ORION'S BELT -
three cold points in a line across his waist - and the cold eye-light under the lion's jaw, where his
face is a void. That and the thread are all the light he has.
"""
from __future__ import division, print_function

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

G = roster.G
CELL = 96
FEET = CELL - 2
STEM = "downs-unstrung-hunter"
SHEET = "DownsUnstrungHunterFrames"
OUT_ENEMIES = os.path.join("assets", "bonelight", "enemies")
SCRATCH = os.path.join(HERE, "_downs_boss")
PALETTE = dict(SB.PALETTE)

# Boss feel (build.py's BOSS_FEEL), for the two derived strips: a lean hunter hardly flinches.
# `moving` and `attack` are drawn, not derived (see `animations`), so only `knock` is read.
FEEL = (0.3, 4.0, 1)

# Materials: (mid, lit, dark) and a deep stop where one is wanted.
PELT = ("e", "l", "E")
PELT_DEEP = "A"
FLESH = ("b", "c", "a")             # the shadow's own body: wool's deep indigo
FLESH_FAR = ("a", "b", "J")         # the far limbs, a stop down, so two limbs never merge
LEATHER = ("R", "r", "q")
THONG = ("N", "P", "j")
BONE = ("k", "s", "n", "m")         # the horn of the bow, the claws and the fangs
ARCANE = "ZYyz"

# Where the upper body sits when he kneels to draw, relative to where it is when he stands.
KNEEL = (1, 16)

# The arrow's row when he kneels (the jaw, under the eye), the bow's grip on that row, and his
# anchor at full draw - the knuckles at his ear. The `attack` report in main() reads these.
DRAW_ROW = 47
DRAW_GRIP = (80, DRAW_ROW)
DRAW_ANCHOR = (45, DRAW_ROW)
BOW_HALF = 44


class Off(object):
    """A grid seen through a translation, so the upper body can be drawn once and placed anywhere
    (lifted on the walk's bob, lowered into the kneel). Everything the helpers call goes through
    set/get/span/rect/line/disc, so roster's and sprite_bosses' tools work through it unchanged."""

    def __init__(self, g, dx=0, dy=0):
        self.g, self.dx, self.dy, self.cell = g, dx, dy, g.cell

    def set(self, x, y, ch):
        self.g.set(x + self.dx, y + self.dy, ch)

    def get(self, x, y):
        return self.g.get(x + self.dx, y + self.dy)

    def span(self, y, x0, x1, ch):
        self.g.span(y + self.dy, x0 + self.dx, x1 + self.dx, ch)

    def rect(self, x0, y0, x1, y1, ch):
        self.g.rect(x0 + self.dx, y0 + self.dy, x1 + self.dx, y1 + self.dy, ch)

    def line(self, x0, y0, x1, y1, ch):
        self.g.line(x0 + self.dx, y0 + self.dy, x1 + self.dx, y1 + self.dy, ch)

    def disc(self, cx, cy, r, ch):
        self.g.disc(cx + self.dx, cy + self.dy, r, ch)


def _hash(x, y, seed=0):
    h = (x * 73856093) ^ (y * 19349663) ^ (seed * 83492791)
    return (h & 0xFFFF) / 65535.0


# --- the shape toolkit ------------------------------------------------------------------------

def capsule(g, p0, p1, r0, r1, mid, lit, dark):
    """A limb or a strap at ANY angle: the pixels within r of the segment, lit on the side that
    faces the key (upper left) and shaded on the side away from it. SB.limb spans rows, so a
    level forearm - the drawing arm - came out one row thick; this does not care which way it runs."""
    (x0, y0), (x1, y1) = p0, p1
    vx, vy = x1 - x0, y1 - y0
    L2 = vx * vx + vy * vy
    L = math.sqrt(L2) if L2 else 1.0
    nx, ny = (-vy / L, vx / L) if L2 else (-0.707, -0.707)
    if nx + ny > 0:                                     # the normal that points up and left
        nx, ny = -nx, -ny
    rm = max(r0, r1)
    for y in range(int(math.floor(min(y0, y1) - rm - 1)), int(math.ceil(max(y0, y1) + rm + 1)) + 1):
        for x in range(int(math.floor(min(x0, x1) - rm - 1)), int(math.ceil(max(x0, x1) + rm + 1)) + 1):
            t = ((x - x0) * vx + (y - y0) * vy) / L2 if L2 else 0.0
            t = max(0.0, min(1.0, t))
            cx, cy = x0 + vx * t, y0 + vy * t
            r = r0 + (r1 - r0) * t
            dx, dy = x - cx, y - cy
            if dx * dx + dy * dy > r * r + 0.3:
                continue
            off = dx * nx + dy * ny
            g.set(x, y, lit if off > r * 0.42 else (dark if off < -r * 0.42 else mid))


def joint_of(a, c, la, lb, side=1):
    """The middle joint of a two-bone limb from a to c, bent toward +x (side 1) or -x (side -1).
    Knees bend forward; that is all a walk needs from it."""
    (ax, ay), (cx, cy) = a, c
    dx, dy = cx - ax, cy - ay
    d = math.hypot(dx, dy)
    if d >= la + lb - 1e-6:
        t = la / (la + lb)
        return (ax + dx * t, ay + dy * t)
    ang = math.acos(max(-1.0, min(1.0, (la * la + d * d - lb * lb) / (2.0 * la * d))))
    base = math.atan2(dy, dx)
    k1 = (ax + la * math.cos(base - ang), ay + la * math.sin(base - ang))
    k2 = (ax + la * math.cos(base + ang), ay + la * math.sin(base + ang))
    return k1 if (k1[0] - k2[0]) * side >= 0 else k2


# --- the bow and its thread ------------------------------------------------------------------

def bow_centre(gy, gx, y, bend=0.0, half=BOW_HALF):
    """The bow's centre line at row y: a deep arc away from him, with recurved horn ears at both
    tips. `bend` pulls the tips toward the archer as the thread is drawn."""
    u = (y - gy) / float(half)
    a = abs(u)
    x = gx + 1 - (16.0 + bend) * u * u
    if a > 0.84:
        x += 4.5 * ((a - 0.84) / 0.16) ** 2              # the ear: the limb turns forward again
    return x


def bow(g, gx, gy, bend=0.0, half=BOW_HALF):
    """The great bow, upright, its grip at (gx, gy). Returns (top nock, bottom nock, grip's inner
    edge x). Horn in bone's middle stops, lit on the belly (toward him and the key), the sinew
    backing dark along its back, a horn ridge every few rows, a wrapped grip, capped tips."""
    top, bot = int(round(gy - half)), int(round(gy + half))
    inner = None
    for y in range(top, bot + 1):
        u = (y - gy) / float(half)
        a = abs(u)
        xc = bow_centre(gy, gx, y, bend, half)
        w = 2.4 if a > 0.86 else (5.0 if a < 0.12 else 5.0 - 2.4 * (a - 0.12) / 0.74)
        x0, x1 = int(round(xc - w / 2.0)), int(round(xc + w / 2.0)) - 1
        x1 = max(x0 + 1, x1)
        mid, lit, dark, deep = BONE
        # The upper limb's belly faces up and left, into the key; the lower limb's faces down and
        # away from it. Lit the same, the bow was a white bracket - a light source, which nothing
        # he holds may be. Modelled, it is horn: the upper limb pale, the lower in its shade.
        if u > 0.15:
            mid, lit = dark, mid
        g.span(y, x0, x1, mid)
        g.set(x0, y, lit)
        g.set(x1, y, deep if x1 - x0 >= 2 else dark)
        if x1 - x0 >= 3:
            g.set(x1 - 1, y, dark)
        # the horn's growth ridges: a dark stop across the belly with a lit one above it, at an
        # uneven pitch so the limb is horn and not a ladder
        if a < 0.86 and (y * 5 + y // 7) % 9 == 0:
            for x in range(x0 + 1, x1):
                g.set(x, y, dark)
            g.set(x0 + 1, y - 1, lit)
        if y == gy:
            inner = x0
    # THE GRIP: bound in old linen, the binding's turns in alternate stops.
    for y in range(gy - 6, gy + 7):
        xc = bow_centre(gy, gx, y, bend, half)
        x0, x1 = int(round(xc - 3.0)), int(round(xc + 2.0))
        for x in range(x0, x1 + 1):
            g.set(x, y, "E" if (y + (x - x0) // 2) % 2 else PELT_DEEP)
        g.set(x0, y, "e" if y % 2 else "E")
        g.set(x1, y, "o")
    g.span(gy - 7, int(round(bow_centre(gy, gx, gy - 7, bend, half) - 2)),
           int(round(bow_centre(gy, gx, gy - 7, bend, half) + 2)), "m")
    g.span(gy + 7, int(round(bow_centre(gy, gx, gy + 7, bend, half) - 2)),
           int(round(bow_centre(gy, gx, gy + 7, bend, half) + 2)), "m")
    # THE TIPS: capped in dark horn, and the nock where the thread is made fast.
    tx_top = int(round(bow_centre(gy, gx, top, bend, half)))
    tx_bot = int(round(bow_centre(gy, gx, bot, bend, half)))
    g.set(tx_top, top - 1, "n")
    g.set(tx_top + 1, top - 1, "m")
    g.set(tx_bot, bot + 1, "n")
    g.set(tx_bot + 1, bot + 1, "m")
    nock_top = (tx_top - 1, top + 3)
    nock_bot = (tx_bot - 1, bot - 3)
    return nock_top, nock_bot, inner


def thread_line(g, a, b, tone):
    (x0, y0), (x1, y1) = a, b
    n = int(max(abs(x1 - x0), abs(y1 - y0)))
    for i in range(n + 1):
        t = i / float(max(1, n))
        g.set(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, tone(t) if callable(tone) else tone)


def thread(g, top, bot, nock=None, heat=0, sway=0.0):
    """The string of light. Straight tip to tip at rest; through `nock` when drawn. `heat` 0..3
    climbs arcane's ramp (edge, mid, hot) as the pull grows, and the stretch nearest his fingers
    is a stop hotter than the rest - the light gathers where it is held. `sway` bows it, for the
    thread still singing after the loose."""
    ramp = ["z", "y", "Y", "Z"]
    base = ramp[min(heat, 3)]
    if nock is None:
        n = bot[1] - top[1]
        for i in range(n + 1):
            y = top[1] + i
            t = i / float(max(1, n))
            g.set(top[0] + sway * math.sin(math.pi * t), y, base)
        return
    hot = ramp[min(heat + 1, 3)]
    for end in (top, bot):
        thread_line(g, nock, end, lambda t: hot if t < 0.22 else base)


# --- the pelt ---------------------------------------------------------------------------------
# The first pass hung a hide as wide as the man down his back and a mane like a haystack round
# his head, textured all over with speckle: filled black it was a blob beside a bow, and in colour
# a moss heap - a ghillie suit, not a hunter. The pelt is the clothes, not the figure. So the hood
# is a head's size, the mane is a ruff round the neck, the hide hangs a hand's width behind his
# back, and the man - shoulders, a bare chest, long legs - is the shape.

def fur(g, x0, y0, x1, y1, seed, only=None, rate=0.06):
    """Hide texture: short strands of the deep and the lit stop through what is already pelt, a
    strand rather than a speck, and sparse - speckle everywhere is what made it moss."""
    only = only or ("e", "E", "l")
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if g.get(x, y) not in only:
                continue
            h = _hash(x, y, seed)
            if h < rate and g.get(x, y + 1) in only and g.get(x, y + 2) in only:
                g.set(x, y, PELT_DEEP)
                g.set(x, y + 1, "E")
            elif h > 1.0 - rate * 0.5 and g.get(x - 1, y) in only:
                g.set(x, y, "e")


# The pelt's hind legs: (from, to, paw). Hanging when he stands; when he kneels the hide is on the
# ground round him and they lie back along it.
HIND_HANGING = (((30, 70), (28, 79), (28, 81)), ((36, 70), (37, 77), (37, 79)))
HIND_SINKING = (((30, 70), (26, 76), (25, 78)), ((36, 70), (35, 76), (34, 78)))
HIND_KNEELING = (((30, 70), (24, 73), (22, 73)), ((36, 70), (33, 74), (31, 74)))
HIDE = ("E", "e", "A")               # the hide hangs in the shade: mid, lit, dark


def pelt_back(g, tail, hind=HIND_HANGING):
    """The hide down his back to the knee, its ragged hem, the hind legs, and the tail. `tail` is
    the tail's polyline (it hangs when he stands; when he kneels tail_on_ground lays the rest)."""
    mid, lit, dark = HIDE
    keys = [(28, 37, 42), (36, 33, 41), (48, 31, 40), (60, 29, 39), (69, 28, 38)]
    roster.mass(g, keys, mid, lit, dark, rim=False)
    # the ragged hem: each column a different length, torn
    hem = [2, 3, 1, 4, 2, 0, 1, 3, 2, 1, 3]
    for i, x in enumerate(range(28, 39)):
        for k in range(1, hem[i] + 1):
            g.set(x, 69 + k, mid if k < hem[i] else dark)
    # the fall of the hide from the shoulder: a crease, and the fur
    roster.fold(g, [(38, 30), (35, 48), (33, 68)], dark, lit)
    fur(g, 27, 28, 42, 74, 7)
    # THE HIND LEGS, from the hem, each ending in a paw with its claws
    for (a, b, paw) in hind:
        capsule(g, a, b, 1.9, 1.4, mid, lit, dark)
        g.disc(paw[0], paw[1], 1.7, mid)
        g.set(paw[0] - 1, paw[1] - 1, lit)
        g.set(paw[0] - 1, paw[1] + 2, "n")
        g.set(paw[0] + 1, paw[1] + 2, "m")
    # THE TAIL, and its tuft
    for a, b in zip(tail, tail[1:]):
        capsule(g, a, b, 1.0, 1.0, mid, lit, dark)
    tx, ty = tail[-1]
    g.disc(tx, ty, 1.9, dark)
    g.set(tx - 1, ty - 1, mid)
    g.set(tx, ty + 2, dark)


def mane(g):
    """The lion's mane: round the back of the lion's head, where it framed the lion's face, and
    down into a ruff round his neck and over his shoulders, shaggy at its edge. With the muzzle
    alone in front of it the hood read as a crocodile; the mane behind is what makes it a lion."""
    mid, lit, dark = "E", "e", PELT_DEEP
    keys = [(13, 40, 45), (17, 36, 47), (23, 34, 48), (29, 34, 49), (34, 36, 52), (37, 39, 51)]
    roster.mass(g, keys, mid, lit, dark, rim=False)
    for (x, y, dx, dy) in ((39, 13, -3, 2), (36, 17, -3, 3), (34, 22, -3, 4), (34, 28, -3, 5),
                           (36, 34, -2, 4), (43, 37, -1, 3), (51, 35, 1, 4)):
        SB.spike(g, x, y, dx, dy, 1.5, mid, lit, dark)
    # locks: strands down the ruff, lit at their roots
    for (x, y, n) in ((38, 18, 8), (41, 25, 9), (44, 30, 6), (36, 26, 7), (48, 31, 4)):
        for k in range(n):
            xx = x - k // 4
            if g.get(xx, y + k) in (mid, lit, dark):
                g.set(xx, y + k, lit if k == 0 else (dark if k % 3 else mid))


def head(g):
    """The lion's head worn as a hood - crown, ears, brow, muzzle, the upper fangs - and under the
    jaw, where his face should be, the void."""
    SB.swell(g, 15, 27, [2.5, 4.8, 5.8, 5.8, 5.2, 4.0], "e", "l", "E", cx=47.0)
    # the ears, round, standing out of the mane, the inside deep
    for (cx, cy, r) in ((43.0, 15.0, 1.8), (48.5, 14.5, 1.6)):
        g.disc(cx, cy, r, "e")
        g.set(cx - 1, cy - 1, "l")
        g.set(cx, cy, PELT_DEEP)
        g.set(cx + 1, cy + 1, "E")
    # the brow and the muzzle over his face: short and blunt, a cat's, not a long snout
    ends = {18: 51, 19: 53, 20: 55, 21: 56, 22: 56, 23: 56, 24: 55, 25: 54, 26: 52}
    for y, x1 in sorted(ends.items()):
        g.span(y, 48, x1, "e")
        g.set(x1, y, "E")
        if y >= 24:
            g.set(x1 - 1, y, "E")
    g.span(18, 47, 50, "l")
    g.span(19, 50, 52, "l")                             # the bridge of the nose catches the key
    g.span(20, 52, 54, "l")
    g.set(56, 21, PELT_DEEP)                            # the nose pad
    g.set(56, 22, PELT_DEEP)
    g.set(55, 21, "E")
    for (x, y) in ((53, 23), (54, 24), (52, 25), (51, 23)):
        g.set(x, y, "E")                                # the whisker pads
    g.span(26, 47, 52, PELT_DEEP)                       # the upper lip
    # the pelt's own eye: an empty slot under a heavy lit brow, dead - not his
    g.set(50, 21, "o")
    g.set(51, 21, PELT_DEEP)
    g.set(49, 20, "l")
    g.set(50, 20, "l")
    # the seam where the skin was cut, down the back of the crown, against the mane
    roster.fold(g, [(44, 17), (42, 21), (42, 26)], PELT_DEEP, "l")
    fur(g, 41, 15, 48, 27, 3, rate=0.05)
    # HIS FACE: a void under the lion's jaw, narrowing to a gaunt chin
    for y in range(27, 34):
        g.span(y, 45 + (y - 27) // 3, 54 - (y - 27) // 2, "o")
    g.set(51, 33, "a")                                  # the jaw's edge, a breath of the shadow's body
    g.set(50, 34, "a")
    # the upper fangs hanging over it
    for (x, tone) in ((49, "k"), (53, "n")):
        g.set(x, 27, "s")
        g.set(x, 28, tone)


def eye(g):
    """His mark in the face: two pixels of the cold arcane, the hot one forward."""
    g.set(50, 29, "y")
    g.set(51, 29, "Y")


def forepaws(g):
    """The lion's forelegs, knotted at his throat and hanging down his chest, paws and claws. In
    the hide's shade, not the hood's light: lit like the hood they read as a bib."""
    mid, lit, dark = HIDE
    capsule(g, (45, 32), (51, 35), 1.5, 1.5, dark, mid, PELT_DEEP)  # the near foreleg over the shoulder
    g.disc(51.5, 35.5, 1.8, mid)                                       # the knot
    g.set(50, 34, lit)
    g.set(52, 36, PELT_DEEP)
    for (a, b, paw) in (((50, 37), (49, 42), (49, 44)), ((53, 37), (54, 43), (54, 45))):
        capsule(g, a, b, 1.4, 1.3, mid, lit, dark)
        g.disc(paw[0], paw[1], 1.6, mid)
        g.set(paw[0] - 1, paw[1] - 1, lit)
        g.set(paw[0] + 1, paw[1] + 1, dark)
        for k in (-1, 0, 1):                            # the claws, bone, hooked
            g.set(paw[0] + k, paw[1] + 2, "s" if k < 1 else "n")
        g.set(paw[0], paw[1] + 3, "m")


# --- the body ---------------------------------------------------------------------------------

def torso(g):
    """The shadow's own body, bare under the pelt: a lean chest, the ribs under it, a long waist.
    Wool's deep indigo, the colour shadows are in this game."""
    mid, lit, dark = FLESH
    keys = [(31, 42, 50), (35, 40, 53), (43, 40, 54), (49, 41, 53), (53, 40, 52)]
    roster.mass(g, keys, mid, lit, dark, rim=False)
    roster.fold(g, [(46, 40), (50, 41), (53, 40)], "a", "c")          # under the chest
    for (y, x0, x1) in ((43, 47, 52), (45, 48, 52), (47, 47, 51)):       # the ribs, gaunt
        for x in range(x0, x1 + 1):
            if g.get(x, y) == mid:
                g.set(x, y, "a" if (x + y) % 3 else "c")
    roster.fold(g, [(44, 36), (43, 44), (44, 52)], "a", "c")          # the flank turning away


def kilt(g, sway=0):
    """A hunter's kilt of leather strips from the belt to mid-thigh, each strip split from the next
    by a dark seam. `sway` swings the hem with the stride."""
    mid, lit, dark = LEATHER
    hems = [62, 64, 63, 65, 62, 64, 63, 65]
    for i, x in enumerate(range(39, 55, 2)):
        hem = hems[i % len(hems)]
        tone = mid if i % 2 else dark
        for y in range(53, hem + 1):
            t = (y - 53) / float(hem - 53)
            xx = x + (x - 46) * t * 0.4 + sway * t * t
            g.set(xx, y, tone)
            g.set(xx + 1, y, "o" if y > 55 else tone)
            if y == hem:
                g.set(xx, y, lit if i % 2 else mid)
        g.set(x, 55, lit)
    for x in range(40, 54, 4):
        g.set(x, 56, "t")                               # a stud at the top of every other strip
        g.set(x, 57, "q")


def belt_top(x):
    """The belt's top row at column x: one row lower every four columns toward his back, which
    sets the three stars (44, 48, 52) on a straight slant, as the belt stands in the sky."""
    return int(round(51.5 + (52 - x) / 4.0))


STARS = (44, 48, 52)


def belt(g):
    """The belt, low on the hip and sloping to the back, and on it ORION'S BELT: three cold points
    in a line. They and his eye are his whole mark."""
    for x in range(39, 55):
        top = belt_top(x)
        g.set(x, top, "P")
        g.set(x, top + 1, "N")
        g.set(x, top + 2, "j")
        g.set(x, top + 3, "o")
    for x in STARS:
        g.set(x, belt_top(x) + 2, "z")                  # each star's light on the leather under it
    belt_stars(g)


def belt_stars(g):
    """The three points. Last, with the eye: marks are drawn after everything that could cover them."""
    for x in STARS:
        g.set(x, belt_top(x) + 1, "Y")


def far_arm(g, shoulder, elbow, hand):
    """The bow arm, a stop darker than the near one, an archer's bracer laced on the forearm."""
    mid, lit, dark = FLESH_FAR
    capsule(g, shoulder, elbow, 2.5, 2.2, mid, lit, dark)
    capsule(g, elbow, hand, 2.2, 1.9, mid, lit, dark)
    (ex, ey), (hx, hy) = elbow, hand
    a = (ex + (hx - ex) * 0.3, ey + (hy - ey) * 0.3)
    b = (ex + (hx - ex) * 0.75, ey + (hy - ey) * 0.75)
    capsule(g, a, b, 2.4, 2.3, "R", "r", "q")
    for k in range(5):
        t = 0.36 + 0.09 * k
        g.set(ex + (hx - ex) * t, ey + (hy - ey) * t, "q" if k % 2 else "t")


def fist(g, x, y, mid="b", lit="c", dark="a"):
    """A hand closed round a grip: the fingers in bands, the knuckles lit."""
    g.rect(x - 2, y - 3, x + 2, y + 3, mid)
    for yy in (y - 1, y + 1):
        g.span(yy, x - 2, x + 1, dark)
    g.span(y - 3, x - 2, x + 1, lit)
    g.set(x - 2, y, lit)
    g.set(x - 2, y + 2, lit)
    g.span(y + 3, x - 1, x + 2, dark)


def near_arm(g, shoulder, elbow, hand, open_hand=False, drawing=False):
    """The near arm, the shadow's own indigo, a thong wrapped at the wrist. Its shadow falls on what
    is behind it first, so arm and body - the same indigo - never merge."""
    mid, lit, dark = FLESH
    capsule(g, (shoulder[0] + 1, shoulder[1] + 1), (elbow[0] + 1, elbow[1] + 1), 2.6, 2.3, "o", "o", "o")
    capsule(g, (elbow[0] + 1, elbow[1] + 1), (hand[0] + 1, hand[1] + 1), 2.3, 1.9, "o", "o", "o")
    capsule(g, shoulder, elbow, 2.6, 2.3, mid, lit, dark)
    capsule(g, elbow, hand, 2.3, 1.9, mid, lit, dark)
    g.disc(elbow[0], elbow[1], 2.1, mid)
    g.set(elbow[0] - 1, elbow[1] - 1, lit)
    g.set(elbow[0] + 1, elbow[1] + 1, dark)
    (ex, ey), (hx, hy) = elbow, hand
    for t in (0.70, 0.78):
        g.set(ex + (hx - ex) * t, ey + (hy - ey) * t, "N")
        g.set(ex + (hx - ex) * t - 1, ey + (hy - ey) * t, "P")
    x, y = int(round(hx)), int(round(hy))
    if open_hand:
        # the loose: the hand flung back past the ear, fingers spread
        g.rect(x - 1, y - 1, x + 1, y + 1, mid)
        g.set(x - 1, y - 1, lit)
        for (dx, dy, ch) in ((-2, -3, lit), (-1, -4, mid), (-3, -2, mid), (-4, -1, lit),
                             (-3, 1, dark), (-4, 2, mid), (0, -3, mid)):
            g.set(x + dx, y + dy, ch)
    elif drawing:
        # three fingers hooked on the thread, the back of the hand toward us
        g.rect(x - 2, y - 2, x + 1, y + 2, mid)
        g.span(y - 2, x - 2, x, lit)
        g.set(x + 1, y - 1, dark)
        g.set(x + 1, y + 1, dark)
        g.set(x - 1, y + 2, dark)
    else:
        g.rect(x - 1, y - 1, x + 1, y + 2, mid)
        g.set(x - 1, y - 1, lit)
        g.set(x + 1, y + 2, dark)
        g.set(x, y + 3, dark)


def leg(g, hip, ankle, near, thigh=18.0, shin=18.0, toe=True):
    """A long leg from hip to ankle, the knee bent forward: the shadow's thigh, a high laced boot
    (an endromis, the hunter's boot) from the knee down, its cuff a turned-down flap of hide."""
    knee = joint_of(hip, ankle, thigh, shin, side=1)
    fl = FLESH if near else FLESH_FAR
    capsule(g, hip, knee, 3.0, 2.5, *fl)
    boot = THONG if near else ("j", "N", "J")
    capsule(g, knee, ankle, 2.4, 1.9, *boot)
    (kx, ky), (ax, ay) = knee, ankle
    n = int(math.hypot(ax - kx, ay - ky) // 3)
    for i in range(2, n):                               # the lacing: crossing thongs up the shin
        t = i / float(n)
        cx, cy = kx + (ax - kx) * t, ky + (ay - ky) * t
        g.set(cx - 1, cy, "P" if near else "N")
        g.set(cx, cy + (1 if i % 2 else -1), "J")
    g.disc(kx + 0.5, ky + 2.0, 2.3, "E" if near else PELT_DEEP)
    g.set(kx - 1, ky + 1.0, "e" if near else "E")
    g.set(kx + 1, ky + 4.0, PELT_DEEP)
    if toe:
        capsule(g, (ax - 1, ay + 1), (ax + 4, ay + 2), 1.4, 1.2, *boot)
        g.set(ax + 5, ay + 2, boot[2])
    return knee


# --- the poses --------------------------------------------------------------------------------

HIP_NEAR, HIP_FAR = (41.0, 56.0), (46.0, 55.0)
SHOULDER_NEAR, SHOULDER_FAR = (43, 33), (50, 33)


def standing(phase=0.0):
    """The stalk. phase 0 and 0.5 are the two contacts (near foot forward, then the far one); a
    long stride, the body lifting at the passing positions. The bow rides with the body."""
    a = 2.0 * math.pi * phase
    c, lift_near, lift_far = roster.stride(phase)
    bob = int(round(1.6 * abs(math.sin(a))))
    g = G(CELL)
    U = Off(g, 0, -bob)
    S = 11.0
    hip_n = (HIP_NEAR[0], HIP_NEAR[1] - bob)
    hip_f = (HIP_FAR[0], HIP_FAR[1] - bob)
    ank_n = (HIP_NEAR[0] + c * S, 90.0 - 5.0 * lift_near)
    ank_f = (HIP_FAR[0] - c * S, 90.0 - 5.0 * lift_far)
    swing = -c * 1.0                                    # the bow arm swings a little against the near leg
    gx, gy = int(round(78 + swing)), 48

    pelt_back(U, [(33, 70), (32, 76), (30 - c, 82), (27 - 2 * c, 87)])
    top, bot, _inner = bow(U, gx, gy)
    thread(U, top, bot, heat=0)
    far_arm(U, SHOULDER_FAR, (63 + swing * 0.5, 41), (gx - 2, gy - 1))
    fist(U, gx - 2, gy, *FLESH_FAR)
    leg(g, hip_f, ank_f, near=False)
    leg(g, hip_n, ank_n, near=True)
    torso(U)
    kilt(U, sway=-c * 1.5)
    belt(U)
    # the near arm swings against the near leg - back as it strides, forward as it passes - and
    # never so far forward that the hand covers the belt's three points
    # It hangs at the back of the body, over the hide: indigo against the hide's grey reads as an
    # arm, where indigo against the indigo chest was not there at all.
    near_arm(U, (SHOULDER_NEAR[0] - 1, SHOULDER_NEAR[1]), (38 - c * 2.0, 44), (37 - c * 2.5, 54))
    mane(U)
    forepaws(U)
    head(U)
    finish(g)
    eye(U)
    belt_stars(U)
    roster.contact(g, 22, 66, y=FEET)
    return g.rows()


# The draw, frame by frame: how deep the kneel (0..1), the bow's grip, the drawing arm's elbow and
# hand, how far the bow bends, how hot the thread, and what the hand is doing. Frame 4 is the full
# draw and frame 5 the loose.
DRAW = [
    (0.6, (79, 53), (52, 50), (66, 52), 0.0, 0, "reach"),
    (1.0, DRAW_GRIP, (55, 51), (68, DRAW_ROW), 0.0, 1, "draw"),
    (1.0, DRAW_GRIP, (48, 51), (60, DRAW_ROW), 1.0, 1, "draw"),
    (1.0, DRAW_GRIP, (38, 49), (52, DRAW_ROW), 2.0, 2, "draw"),
    (1.0, DRAW_GRIP, (32, 47), DRAW_ANCHOR, 3.0, 2, "full"),
    (1.0, DRAW_GRIP, (30, 46), (37, 42), 0.0, 1, "loose"),
]


def kneeling(stage):
    """One frame of the draw: down on the near knee, the far foot planted forward, the bow raised
    at arm's length and the thread pulled back to the ear."""
    k, grip, elbow, hand, bend, heat, state = DRAW[stage]
    dx, dy = int(round(KNEEL[0] * k)), int(round(KNEEL[1] * k))
    g = G(CELL)
    U = Off(g, dx, dy)
    hip_n = (HIP_NEAR[0] + dx, HIP_NEAR[1] + dy)
    hip_f = (HIP_FAR[0] + dx, HIP_FAR[1] + dy)
    ank_f = (63.0, 90.0)
    ank_n = (22.0 + (1 - k) * 8, 91.0 - (1 - k) * 2)
    shoulder_n = (SHOULDER_NEAR[0] + dx, SHOULDER_NEAR[1] + dy)
    shoulder_f = (SHOULDER_FAR[0] + dx, SHOULDER_FAR[1] + dy)
    # the hide falls round him onto the ground, its hind legs and tail lying out behind him
    if k < 1:
        pelt_back(U, [(33, 70), (31, 75), (28, 79)], HIND_SINKING)
    else:
        pelt_back(U, [(33, 70), (31, 73)], HIND_KNEELING)
        tail_on_ground(g)
    top, bot, inner = bow(g, grip[0], grip[1], bend=bend)
    far_arm(g, shoulder_f, ((shoulder_f[0] + grip[0]) / 2.0, (shoulder_f[1] + grip[1]) / 2.0 + 0.5),
            (grip[0] - 2, grip[1] - 1))
    fist(g, grip[0] - 2, grip[1], *FLESH_FAR)
    leg(g, hip_f, ank_f, near=False)
    leg(g, hip_n, ank_n, near=True, toe=False)
    near_toe(g, ank_n)
    torso(U)
    kilt(U, sway=0)
    belt(U)
    mane(U)
    forepaws(U)
    head(U)
    # the thread: at rest straight; drawn through his fingers; after the loose, still singing
    if state == "reach":
        thread(g, top, bot, heat=heat)
    elif state == "loose":
        thread(g, top, bot, heat=heat, sway=2.0)
        thread(g, (top[0] - 1, top[1] + 5), (bot[0] - 1, bot[1] - 5), heat=0, sway=-1.0)
    else:
        thread(g, top, bot, nock=(hand[0] + 1, hand[1]), heat=heat)
    near_arm(g, shoulder_n, elbow, hand, open_hand=(state == "loose"), drawing=(state in ("draw", "full")))
    if state == "full":
        arrow(g, (hand[0] + 2, hand[1]), inner)
    if state in ("draw", "full"):
        g.set(hand[0] + 1, hand[1], "Z" if state == "full" else "Y")
    if state == "loose":
        for (x, tone) in ((inner + 7, "Y"), (inner + 9, "y"), (inner + 11, "z")):
            g.set(x, grip[1], tone)                     # the bolt's wake, just gone
    finish(g)
    eye(U)
    belt_stars(U)
    roster.contact(g, 10, 70, y=FEET)
    return g.rows()


def tail_on_ground(g):
    """When he kneels the tail lies out behind him on the turf, its tuft at the end."""
    pts = [(31, 88), (25, 91), (18, 92), (13, 91)]
    for a, b in zip(pts, pts[1:]):
        capsule(g, a, b, 1.0, 1.0, *HIDE)
    g.disc(12, 91, 1.9, PELT_DEEP)
    g.set(11, 90, "E")


def near_toe(g, ank):
    """The kneeling leg's foot, toes dug into the ground behind him."""
    ax, ay = ank
    capsule(g, (ax, ay - 1), (ax - 3, ay + 2), 1.6, 1.4, *THONG)
    g.set(ax - 4, ay + 2, "j")


def arrow(g, nock, inner):
    """At full draw a bolt of his light lies on the thread, from his fingers past the grip: the
    shaft at the mid stop, the head at the hot and the core."""
    x0, y = nock
    for x in range(x0, inner + 7):
        g.set(x, y, "y")
    hx = inner + 7
    g.set(hx, y - 1, "Y")
    g.set(hx, y + 1, "Y")
    g.set(hx, y, "Z")
    g.set(hx + 1, y, "Z")
    g.set(hx + 2, y, "Y")
    g.set(x0 + 1, y - 1, "z")                           # the fletching, two barbs of the edge stop
    g.set(x0 + 1, y + 1, "z")


# --- the finish -------------------------------------------------------------------------------

def finish(g):
    """The 1 px key rim on every left edge that faces open air, where the form is at least two
    pixels across (a one-pixel run is the thread, which carries its own light)."""
    for y in range(2, FEET + 1):
        x = 1
        while x < CELL:
            ch = g.get(x, y)
            if ch not in (".", "W") and ch not in ARCANE and g.get(x - 1, y) == ".":
                run = 0
                while x + run < CELL and g.get(x + run, y) not in (".", "W"):
                    run += 1
                if run >= 2:
                    g.set(x - 1, y, "W")
                x += max(1, run)
                continue
            x += 1


# --- writing ----------------------------------------------------------------------------------

def animations():
    """The contract's four, as (name, frames, loop, speed). `moving` and `attack` are drawn,
    `hurt` and `death` derived from the rest pose (anim_sets) as every boss's are."""
    pose = standing(0.0)
    walk = [standing(i / 8.0) for i in range(8)]
    draw = [kneeling(i) for i in range(len(DRAW))]
    lean, reach, knock = FEEL
    return pose, [
        ("moving", walk, True, 8.0),
        # 4 a second: the full draw (frame 4) lands 1.0 s in and the loose (frame 5) at 1.25 s.
        ("attack", draw, False, 4.0),
        ("hurt", anim_sets.hurt(pose, knock), False, 12.0),
        ("death", anim_sets.death(pose), False, 10.0),
    ]


def build(root=ROOT):
    pose, anims = animations()
    problems = P.check_anchor("%s pose" % STEM, [pose], CELL)
    for name, frames, _loop, _speed in anims:
        if name in ("moving", "attack"):
            problems += P.check_anchor("%s %s" % (STEM, name), frames, CELL)
    spec = []
    for anim, frames, loop, speed in anims:
        png = "%s-%s.png" % (STEM, anim)
        P.write_strip(frames, PALETTE, os.path.join(root, OUT_ENEMIES, png), CELL)
        spec.append((anim, png, len(frames), loop, speed))
    spriteframes.write(os.path.join(root, "scenes", "resources", SHEET + ".tres"), spec,
                       OUT_ENEMIES.replace(os.sep, "/"), cell=CELL)
    return pose, anims, sum(a[2] for a in spec), problems


def previews(pose, anims):
    from PIL import Image
    black = ["".join("o" if ch != "." else "." for ch in row) for row in pose]
    full = anims[1][1][4]
    full_black = ["".join("o" if ch != "." else "." for ch in row) for row in full]
    P.preview([("pose", [pose, full]), ("silhouette", [black, full_black])], dict(PALETTE, o="#05070C"),
              os.path.join(HERE, "_boss_unstrung_hunter.png"), zoom=4, bg=(150, 156, 160), cell=CELL)
    if not os.path.isdir(SCRATCH):
        os.makedirs(SCRATCH)
    P.preview([(n, f) for n, f, _l, _s in anims], PALETTE, os.path.join(SCRATCH, "strips.png"),
              zoom=3, bg=(30, 38, 52), cell=CELL)
    # At play size (x2) on three floors: a dark one, the teal of a lit night, and pale chalk.
    floors = [(30, 38, 52), (36, 64, 74), (150, 156, 160)]
    frames = [pose, anims[0][1][2], anims[1][1][0], anims[1][1][4], anims[1][1][5]]
    sheet = Image.new("RGBA", (len(frames) * CELL * 2, len(floors) * CELL * 2), (0, 0, 0, 255))
    for r, col in enumerate(floors):
        sheet.paste(col + (255,), (0, r * CELL * 2, sheet.size[0], (r + 1) * CELL * 2))
        for c, rows in enumerate(frames):
            im = P.frame(rows, PALETTE, CELL).resize((CELL * 2, CELL * 2), Image.NEAREST)
            sheet.alpha_composite(im, (c * CELL * 2, r * CELL * 2))
    sheet.save(os.path.join(SCRATCH, "ground_x2.png"))


def main():
    pose, anims, n, problems = build()
    filled, edges, ratio = roster.density(pose)
    full = anims[1][1][4]
    f2, e2, r2 = roster.density(full)
    print("%s  %dx%d  %d px  %.2f edges/px%s  (full draw %.2f)  %d frames -> scenes/resources/%s.tres"
          % (STEM, CELL, CELL, filled, ratio, "" if ratio >= 0.80 else "   SPARSE", r2, n, SHEET))
    for name, frames, loop, speed in anims:
        print("  %-7s %d frames  %s  %.1f/s" % (name, len(frames), "loop" if loop else "once", speed))
    previews(pose, anims)
    if problems:
        print("ANCHOR PROBLEMS:")
        for p in problems:
            print("   " + p)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
