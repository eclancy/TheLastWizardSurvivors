# -*- coding: utf-8 -*-
"""The White Downs' own cast (.ai/second-half.md 4.2, F3). Run: python tools/art/cast_downs.py

"No moon has risen here since he came." Artemis's downs, the first chapter across the dark sea, where
his shadow runs a hunt by night: the beaters walking their line, the coursing hounds, the huntsman
with his horn, the hunt's horse that lost its rider - and the people who kept the chalk (the
shepherd, the slinger, the warrener), the flock's tup, the puffballs of the sheep-cropped turf, and
the cliff-watch that kept the white cliffs facing the sea. Taken, all of them, so all of them stare
with his cold arcane - all but the watch's beacon, the one thing on the downs he BUILT, which burns
(the carve-out .ai/world-and-tone.md allows). Eleven creatures, each a new look on the numbers of the
Emberdeep creature it replaces (scripts/StageBestiary.WhiteDowns.cs); the module contract is
bestiary_kit.py's docstring.

NOTHING ECHOES THE UNSTRUNG HUNTER. The boss is a tall figure behind a great bow taller than himself,
a lion-pelt over his shoulders and three cold stars across his belt. So nothing here carries a bow
or an arrow, nothing wears a pelt, and no belt carries a row of studs: the slinger throws, the
huntsman calls, the beater beats, and the captain's boat-hook is carried slanted across him, never
stood upright beside him, so it can never be a stave.

THE FLOOR DECIDES THE STOPS. The downs at night are a cold dark grey-green turf (about the lichen
row's deep half) torn by pale chalk scars (bone's shade). A body drawn from its ramp's deep end is a
hole in the turf, and one drawn in bone's shade is a hole in the chalk; so, as on the heath
(cast_heath.py), every body carries its weight a stop or two up its ramp and leans on the rim, the
deep stops kept for crevices and the lines under a plate.

ROWS BY JOB, so the crowd separates by material as well as by shape (.ai/bestiary.md section 2). The
four the table rolls together share no row: the beater in grey-green linen, the coursing hound in
flesh (a brindled red hound, the one living coat here), the tup in bone (a downland fleece is
grey-white), the night shepherd in violet (a heather-dyed cloak) - olive, red-brown, white and
purple, four reads at a glance. Around them: the slinger in linen under a bone sheepskin, the horse
in stone carried up its ramp (a black horse, the riderless horse of a funeral) with a white blaze, the
puffball in bone with linen in its tear, the warrener in stone with a rabbit-fur cap and steel on his
spade, the huntsman in hunt green (lichen) with a brass horn, the captain in steel under a black
watch-cloak, and the beacon in iron and wood. Red is only ever old cloth (a kerchief, a saddle-cloth,
a collar), well under the 15% the tone rules allow; gold only brass, leather, horn and wood.

TWO ROWS MOVED FROM THE BRIEF, both on looking. The tup was briefed in wool, and wool is a dyed blue -
a flock of saturated blue sheep read as toys, and the Archmage owns blue; the hound was first drawn in
bone and, with the tup in bone, the two table bodies would have shared a row, so the hound went to
flesh. Flesh's base all over was a hairless pink dog; it is carried in its shade with the base on
the back and a brindle laid in the deep stop.

ONE ATTACHMENT EACH, no two alike on the downs, and each survives being filled black: a stick held
level across the body with a rag tied to its end; long legs with daylight under a deep chest; a coil
of horn on the side of a head over a lumpy fleece; a crook's hook above a hood, leant on; a closed
ring whirled level over the head with a flint at its edge; a basket of iron straps on a post; an empty
saddle with a stirrup swinging under the belly; the only round swell on the turf, cracked into plates;
a spade blade raised highest of all; a horn at the mouth, its bell high and forward; and, on the 48
cell, a boat-hook slanted across a sallet and a cloak.

THE CHAPTER'S BOLT. Metal (.ai/bestiary.md section 6, the far shore's silver; flint is silica): the
slinger's flint is drawn in the metal element ramp, mid #C4D4E8 round a white core, and its bolt is
the same. The beacon is a turret and throws fire, as every turret does.

THE PALETTE CHARACTERS. As cast_bridge.py does: the player-pigment amber characters carry gold's whole
five-stop row (the roster's gold characters stop at shade, and wood and leather need the deep stop),
the ember characters carry cloth red, the azure characters lichen (as cast_heath.py), and the ice
characters - no ice on the downs - the metal element ramp, core / hot / mid / edge. anim_sets steps a
pixel along its own character's ramp, so a hurt flash or a death fade walks each redefined row in the
right order, the flint included.
"""
from __future__ import print_function

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import bestiary_kit
import bonelight as B
import roster
import sprite_bosses as SB
from cast_forest import blob, rim, twig, inside
from foes_forest import _limb

G = roster.G
mass = roster.mass
fold = roster.fold
cut = roster.cut
eyes = roster.eyes
contact = roster.contact

CELL = 32
FEET = CELL - 2

PALETTE = dict(roster.PALETTE)
for _ch, _hex in zip("O!@#$", B.MATERIALS["gold"]):
    PALETTE[_ch] = _hex
for _ch, _hex in zip("Q1234", B.MATERIALS["red"]):
    PALETTE[_ch] = _hex
for _ch, _hex in zip("U5678", B.MATERIALS["lichen"]):
    PALETTE[_ch] = _hex
for _ch, _hex in zip("<>?/", B.ELEMENTS["metal"]):
    PALETTE[_ch] = _hex

GOLD_HI, GOLD_LIT, GOLD, GOLD_SH, GOLD_DP = "O", "!", "@", "#", "$"
RED_HI, RED_LIT, RED, RED_SH, RED_DP = "Q", "1", "2", "3", "4"
MOSS_HI, MOSS_LIT, MOSS, MOSS_SH, MOSS_DP = "U", "5", "6", "7", "8"
FLINT_CORE, FLINT_HOT, FLINT_MID, FLINT_EDGE = "<", ">", "?", "/"
FLINT = FLINT_CORE + FLINT_HOT + FLINT_MID + FLINT_EDGE
# Wood and leather: the gold row's lower half, lit on its base stop. Never its hi or lit as a plane.
WOOD, WOOD_LIT, WOOD_DK = GOLD_SH, GOLD, GOLD_DP


# --- local helpers ------------------------------------------------------------------------------

def taken(g, y, x):
    """THE mark on everything he took: two pixels of cold arcane, hot nearer the key."""
    eyes(g, y, x, x + 1, "Y", "y")


def built(g, x, y):
    """The mark on the one thing he made here: two pixels of fire, core and hot."""
    g.set(x, y, "+")
    g.set(x + 1, y, "-")


def pole(g, x0, y0, x1, y1, lit=WOOD_LIT, dark=WOOD_DK, knots=()):
    """A two-pixel haft at any angle: lit on the key side, dark on the far one."""
    roster.shaft(g, x0, y0, x1, y1, lit, WOOD, dark, width=2)
    for (x, y) in knots:
        g.set(x, y, WOOD)


def boot(g, x0, x1, y0, tone="N", lit="p", sole="J"):
    """A boot from row y0 to the feet row: lit on the instep, the sole dark."""
    g.rect(x0, y0, x1, FEET, tone)
    g.span(y0, x0, x1, lit)
    g.span(FEET, x0, x1, sole)
    g.set(x1 + 1, FEET, sole)


def hand(g, x, y):
    """Two by two, bone, the lit knuckle top-left: the hand that makes a pole HELD."""
    g.set(x, y, "k")
    g.set(x + 1, y, "n")
    g.set(x, y + 1, "n")
    g.set(x + 1, y + 1, "m")


def poly_pts(pts):
    """The pixels inside a polygon, sampled at pixel centres."""
    out = set()
    ys = [p[1] for p in pts]
    n = len(pts)
    for y in range(int(min(ys)) - 1, int(max(ys)) + 2):
        sy = y + 0.5
        xs = []
        for i in range(n):
            (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]
            if (y0 <= sy < y1) or (y1 <= sy < y0):
                xs.append(x0 + (sy - y0) * (x1 - x0) / float(y1 - y0))
        xs.sort()
        for a, b in zip(xs[::2], xs[1::2]):
            x = int(math.floor(a))
            while x + 0.5 < b:
                if x + 0.5 >= a:
                    out.add((x, y))
                x += 1
    return out


def lay(g, cells, mid, lit, dark):
    """Fill a set of pixels and light it from its own outline: lit where the key (upper left) meets
    empty, dark where the far side does."""
    for (x, y) in cells:
        g.set(x, y, mid)
    for (x, y) in cells:
        if (x, y - 1) not in cells or (x - 1, y) not in cells:
            if g.get(x, y - 1) in (".", "W") or g.get(x - 1, y) in (".", "W"):
                g.set(x, y, lit)
        elif (x, y + 1) not in cells or (x + 1, y) not in cells:
            g.set(x, y, dark)


# =================================================================================================
# BULK - Beater. One of the hunt's beaters walking his line, his stick held level across him.
# =================================================================================================

def beater():
    """A beater in a smock walking his line, the beating-stick held level across his body in both
    fists and a white rag knotted to its end.

    The stick is the attachment, and it is held ACROSS - level, rising a little toward its front end
    - so it runs out past the body on both sides, wider than anything else he is: filled black he is a
    walking figure crossed by a bar with a flag at one end. Nothing else on the downs holds anything
    level. Under it a smock-frock in grey-green linen, gathered in uneven smocking across the chest, a
    red kerchief at the throat (the hunt's, the only colour on him), a round felt hat with a soft
    brim (a flat crown and a turned brim made him a cowboy), gaiters.
    """
    g = G(CELL)
    # LEGS: a walking stride under the smock, gaitered, the far leg a stop darker.
    _limb(g, 13, 24, 11, 28, 1.3, "A", "E", "J")
    _limb(g, 17, 24, 19, 28, 1.4, "E", "e", "A")
    boot(g, 9, 11, 28, "j", "N", "J")
    boot(g, 18, 21, 28, "N", "p", "J")
    # THE SMOCK: knee-length, flaring a little to a ragged hem.
    mass(g, [(11, 13, 18), (13, 11, 20), (17, 10, 20), (21, 9, 21), (25, 8, 22)], "e", "l", "E")
    for (x, d) in zip(range(8, 23), [1, 1, 2, 1, 0, 1, 2, 1, 1, 0, 2, 1, 1, 2, 1]):
        for k in range(1, d + 1):
            g.set(x, 25 + k, "E" if k < d else "A")
    cut(g, (12, 26), (17, 26))
    # The smocking: a band of short gathered stitches across the chest, not evenly spaced.
    for (x, y0) in ((12, 14), (14, 13), (15, 14), (17, 13), (18, 14)):
        g.set(x, y0, "E")
        g.set(x, y0 + 1, "A")
        g.set(x - 1, y0, "L" if x == 14 else "l")
    # Folds below it, a crease with its lit side.
    fold(g, [(12, 18), (11, 22), (10, 25)], "E", "l")
    fold(g, [(16, 18), (17, 25)], "A", "e")
    fold(g, [(20, 19), (21, 24)], "A", None)
    g.set(13, 21, "L")
    # A satchel strap and a scrip at the back hip.
    g.line(18, 12, 10, 19, "A")
    g.rect(7, 19, 9, 22, WOOD)
    g.span(19, 7, 9, WOOD_LIT)
    g.set(9, 22, WOOD_DK)
    # THE KERCHIEF at the throat: old red, knotted, a tail down the chest.
    g.span(11, 13, 18, RED)
    g.set(13, 11, RED_LIT)
    g.set(18, 11, RED_SH)
    g.set(16, 12, RED)
    g.set(16, 13, RED_SH)
    # THE HEAD under a round felt hat: the face in the brim's shadow.
    roster.void(g, 8, 10, 2.0, 1.6, cx=16.0)
    for (y, a, b) in ((3, 14, 17), (4, 13, 18), (5, 13, 18), (6, 13, 18)):
        g.span(y, a, b, WOOD)
        g.set(a, y, WOOD_LIT)
        g.set(b, y, WOOD_DK)
    g.span(3, 14, 16, WOOD_LIT)
    g.set(14, 4, WOOD_LIT)
    g.span(6, 13, 18, GOLD_DP)                          # the band
    g.span(7, 12, 19, WOOD)
    g.span(7, 12, 14, WOOD_LIT)
    g.set(19, 7, WOOD_DK)
    g.set(11, 8, WOOD_DK)                               # the soft brim drooping at back and front
    g.set(20, 8, WOOD_DK)
    rim(g, 2, 29, skip="o")
    # THE ARMS forward to the stick: the near one bent, the far one reaching across behind.
    _limb(g, 12, 13, 12, 16, 1.0, "E", "e", "A")
    _limb(g, 19, 12, 21, 15, 1.2, "e", "l", "E")
    # THE STICK, after the rim so it is not doubled into a white wire: level across him, rising to
    # its front end, longer than he is wide on both sides.
    pole(g, 3, 18, 29, 12)
    hand(g, 11, 15)
    hand(g, 21, 14)
    # THE RAG knotted to its front end, white linen streaming back in the walk.
    for (x, y, ch) in ((28, 11, "s"), (29, 11, "k"), (28, 13, "k"), (27, 13, "s"), (26, 14, "k"),
                       (27, 14, "n"), (25, 15, "s"), (26, 15, "k"), (27, 15, "n"), (24, 16, "k"),
                       (25, 16, "n"), (26, 16, "m"), (25, 17, "n"), (23, 17, "n")):
        g.set(x, y, ch)
    contact(g, 7, 23)
    taken(g, 9, 15)
    return g.rows()


# =================================================================================================
# FAST - Coursing Hound. The huntsman's minion: a brindled hound running flat out.
# =================================================================================================

def ik(a, b, l1, l2, bend):
    """The joint of a two-bone limb from a to b. bend +1 puts it behind the line a->b (an elbow,
    which points back), -1 in front of it (a stifle, which points forward). Past full reach the limb
    straightens along the line."""
    ax, ay = a
    bx, by = b
    dx, dy = bx - ax, by - ay
    d = math.hypot(dx, dy)
    if d < 0.01:
        return a
    ux, uy = dx / d, dy / d
    dd = min(d, l1 + l2 - 0.01)
    x = (l1 * l1 - l2 * l2 + dd * dd) / (2.0 * dd)
    h = math.sqrt(max(0.0, l1 * l1 - x * x))
    return (ax + ux * x - bend * uy * h, ay + uy * x + bend * ux * h)


def rot(vx, vy, deg):
    a = math.radians(deg)
    return (vx * math.cos(a) - vy * math.sin(a), vx * math.sin(a) + vy * math.cos(a))


def hound_hind(g, hip, foot, tones, thick):
    """A hound's hind leg: a heavy thigh to the stifle, which points forward, the gaskin back to the
    hock, which points back, and the cannon down to the foot. The cannon is laid off the line from
    the foot to the hip, turned back, so the hock is always behind the foot."""
    mid, lit, dark = tones
    fx, fy = foot
    ux, uy = hip[0] - fx, hip[1] - fy
    n = math.hypot(ux, uy) or 1.0
    back = max(0.0, min(1.0, (hip[0] - fx - 3.0) / 5.0))   # flung back, the cannon falls into line
    cx, cy = rot(ux / n, uy / n, -25 + 30 * back)
    hock = (fx + cx * 3.2, fy + cy * 3.2)
    stifle = ik(hip, hock, 5.5, 5.5, -1)
    _limb(g, hip[0], hip[1], stifle[0], stifle[1], thick * 1.7, mid, lit, dark)
    _limb(g, stifle[0], stifle[1], hock[0], hock[1], thick * 1.0, mid, lit, dark)
    _limb(g, hock[0], hock[1], fx, fy, thick * 0.7, mid, lit, dark)
    g.set(fx + 1, fy, dark)


def hound_fore(g, shoulder, foot, tones, thick):
    """A hound's foreleg: the upper arm back to the elbow, which points back, the forearm down to
    the wrist, and a short pastern to the foot."""
    mid, lit, dark = tones
    fx, fy = foot
    ux, uy = shoulder[0] - fx, shoulder[1] - fy
    n = math.hypot(ux, uy) or 1.0
    cx, cy = rot(ux / n, uy / n, 12)
    wrist = (fx + cx * 2.6, fy + cy * 2.6)
    elbow = ik(shoulder, wrist, 5.5, 6.0, 1)
    _limb(g, shoulder[0], shoulder[1], elbow[0], elbow[1], thick * 1.5, mid, lit, dark)
    _limb(g, elbow[0], elbow[1], wrist[0], wrist[1], thick * 0.9, mid, lit, dark)
    _limb(g, wrist[0], wrist[1], fx, fy, thick * 0.7, mid, lit, dark)
    g.set(fx + 1, fy, dark)


# THE GALLOP. A coursing hound is fast and it is everywhere (the table's second band and the huntsman's
# call), and a bobbed pose slides across the turf with its legs locked - the hare on the heath taught
# that. So its `moving` is DRAWN: the double-suspension gallop of a greyhound, eight frames, from the
# same masses and tones as the pose. Stretched out in the air; the lead fore lands; the body passes
# over the fores while the hinds swing forward; gathered in the air, the back arched, the hinds crossing
# ahead of the fores; the hinds land ahead; they drive; the body extends off them; it leaves the ground.
#
# One key per frame: (stretch, rise, arch, near fore, far fore, near hind, far hind), each foot an
# (x, lift) - lift 0 is planted on the feet row. Stretch spreads the haunch and the chest apart (+) or
# folds them together (-); rise lifts the whole body; arch bows the loin up.
COURSER_RUN = [
    (1.5, 1.0, -0.4, (29.5, 4.0), (28.0, 5.5), (2.0, 5.0), (4.5, 3.5)),
    (0.8, 0.5, 0.0, (27.5, 0.0), (29.0, 3.0), (6.5, 5.5), (8.5, 5.0)),
    (-0.3, 0.0, 0.8, (22.5, 0.0), (25.5, 0.0), (12.0, 5.5), (14.0, 5.0)),
    (-1.8, 0.8, 2.0, (17.0, 4.0), (19.0, 3.0), (21.0, 4.0), (23.0, 4.5)),
    (-2.0, 0.3, 1.8, (14.0, 5.5), (16.0, 4.5), (19.0, 0.0), (21.0, 1.5)),
    (-0.6, 0.3, 0.8, (24.0, 4.0), (22.0, 5.0), (16.0, 0.0), (18.5, 0.0)),
    (0.6, 0.8, 0.0, (27.5, 3.5), (26.0, 5.0), (11.0, 0.0), (13.5, 0.0)),
    (1.3, 1.2, -0.3, (29.5, 3.0), (28.0, 4.5), (4.0, 2.5), (7.0, 0.0)),
]


def courser_frame(key):
    """One frame of the coursing hound from a COURSER_RUN key: the masses placed by stretch, rise
    and arch, the four legs reaching to their feet."""
    s, r, a, nf, ff, nh, fh = key
    g = G(CELL)
    mid, lit, dark, deep, hi = "i", "h", "I", "o", "f"
    hip = (10.5 - s * 0.6, 17.0 - r + a * 0.3)
    sh = (21.0 + s * 0.4, 17.0 - r)

    def ground(f):
        return (f[0], FEET - f[1])

    # THE FAR LEGS, a stop darker.
    far = (dark, mid, dark)
    hound_hind(g, (hip[0] + 0.8, hip[1] + 0.5), ground(fh), far, 0.8)
    hound_fore(g, (sh[0] - 0.8, sh[1] + 0.5), ground(ff), far, 0.8)
    # THE TAIL: long and thin, sweeping down off the croup and curling up at its tip.
    tx, ty = hip[0] - 2.6, hip[1] - 2.6
    twig(g, [(tx, ty), (tx - 2.0, ty + 1.4 - a * 0.4), (tx - 3.2, ty + 4.4), (tx - 2.9, ty + 7.4),
             (tx - 1.8, ty + 8.4)], mid, dark)
    # THE MASSES: a small haunch, a deep chest, and the loin between them arched over a waist tucked
    # up hard - a greyhound is its chest.
    blob(g, hip[0] - 0.2, hip[1] - 1.8, 3.3, 3.2, mid, lit, mid, deep=dark)
    blob(g, sh[0] + 0.2, sh[1] + 0.2, 3.5, 4.7, mid, lit, mid, deep=dark)
    top0, top1 = hip[1] - 4.4, sh[1] - 4.2
    bot0, bot1 = hip[1] - 0.8, sh[1] + 4.2
    span = float(max(1.0, sh[0] - hip[0]))
    for x in range(int(round(hip[0])), int(round(sh[0])) + 1):
        t = (x - hip[0]) / span
        top = top0 + (top1 - top0) * t - (1.3 + a) * math.sin(min(1.0, t * 1.15) * math.pi)
        bot = bot0 + (bot1 - bot0) * (max(0.0, t) ** 2.6) - 0.5 * math.sin(t * math.pi)
        y0, y1 = int(round(top)), int(round(bot))
        for y in range(y0, y1 + 1):
            if g.get(x, y) in (".", "W"):
                g.set(x, y, mid)
        g.set(x, y0, lit)
        if x < sh[0] - 2:
            g.set(x, y0 + 1, lit if x % 3 else mid)
        g.set(x, y1, dark)
    # BRINDLE: dark streaks laid down the flank and the haunch, uneven, the coat's own pattern.
    for (dx, dy, n) in ((0.5, -2.0, 2), (3.0, -3.5, 3), (6.0, -3.0, 2)):
        for k in range(n):
            x, y = hip[0] + dx - k * 0.4, hip[1] + dy + k
            if g.get(x, y) == mid:
                g.set(x, y, dark)
    # The last rib, and the shoulder's edge: creases with their lit side.
    rx, ry = int(round(sh[0] - 3.0)), int(round(sh[1] - 2.0))
    g.line(rx, ry, rx - 1, ry + 3, dark)
    g.set(rx - 1, ry, lit)
    g.set(int(round(sh[0])), int(round(sh[1] - 3.0)), hi)
    # NECK, reaching forward; the head long and narrow, carried low and level with the back.
    hd = (sh[0] + 6.0 + s * 0.2, sh[1] - 5.0 + a * 0.5)
    _limb(g, sh[0] + 0.6, sh[1] - 2.0, hd[0] - 1.2, hd[1] + 0.4, 2.2, mid, lit, dark)
    blob(g, hd[0], hd[1], 2.0, 1.7, mid, lit, dark)
    for i in range(4):
        x, y = hd[0] + 1.4 + i, hd[1] + 0.2 + i * 0.4
        g.set(x, y, mid if i < 3 else dark)
        g.set(x, y - 1, lit if i < 2 else mid)
        g.set(x, y + 1, dark)
    g.set(hd[0] + 4.8, hd[1] + 1.5, deep)               # the nose
    g.set(hd[0] + 3.4, hd[1] + 2.0, dark)
    # The ears: rose ears folded back off the skull.
    g.set(hd[0] - 1.4, hd[1] - 1.9, dark)
    g.set(hd[0] - 2.4, hd[1] - 1.5, dark)
    g.set(hd[0] - 0.4, hd[1] - 2.0, mid)
    rim(g, 4, 26, skip="o", min_run=3)
    # THE NEAR LEGS, after the rim so they are not doubled into white wires.
    near = (mid, lit, dark)
    hound_hind(g, hip, ground(nh), near, 1.0)
    hound_fore(g, sh, ground(nf), near, 1.0)
    planted = [f[0] for f in (nf, ff, nh, fh) if f[1] == 0]
    if planted:
        contact(g, int(round(min(planted) - 2)), int(round(max(planted) + 2)))
    else:
        contact(g, int(round(hip[0])), int(round(sh[0] + 1)))
    taken(g, int(round(hd[1] - 0.4)), int(round(hd[0] - 0.4)))
    return g.rows()


def courser():
    """A coursing hound at full stretch: deep chest, waist tucked up hard, the loin arched, legs
    flung out fore and aft, a long thin head and a long thin tail.

    The read is LEGS AND DAYLIGHT: the only creature on the downs whose body is held clear of the
    ground on four long lines. Built from masses (a deep chest, a small haunch, the arched loin between)
    because a greyhound is its chest, and the legs are jointed - the hinds at stifle and hock, the fores
    at the elbow - because four straight sticks are a table. A brindled red hound, in flesh (the one
    living coat on the downs), carried in its shade with the base stop on the back and the brindle laid
    down its flank in the deep stop: in flesh's base all over it was a hairless pink dog. The pose is
    the first frame of its gallop, which is drawn (COURSER_RUN).
    """
    return courser_frame(COURSER_RUN[0])


def courser_run(t):
    """One frame of the gallop at phase t (0..1)."""
    return courser_frame(COURSER_RUN[int(round(t * len(COURSER_RUN))) % len(COURSER_RUN)])


# bestiary_kit draws the hound's `moving` from this instead of bobbing the pose (write_creature).
courser.cycle = courser_run
courser.cycle_fps = 14.0


# =================================================================================================
# HEAVY - Downland Tup. The flock's ram: low, heavy, horned.
# =================================================================================================

def tup():
    """The flock's ram, head lowered, great horns curled round the ears wider than the head.

    The horns are the attachment: a coil of ridged horn round the side of the head, thick at the poll
    and thinning to a tip that turns forward past the eye, so filled black the head is a ram's and
    nothing else's. It is drawn SOLID, a flat spiral, each turn lying on the last: a thin band round a
    hole was a ring through the ear. Under them the body is fleece - a low heavy mass whose outline is
    all LUMPS, every lock a small curl lit on its upper left - because a fleece drawn as a smooth tone
    is a boulder and one drawn in rows is a radiator. A downland fleece is grey-white, so it is bone,
    carried in its shade and base with the lit stop only on the curls of the back; the face and legs
    are a Down breed's dark, and the horns old amber (wood's stops), so they stand out from both.
    """
    g = G(CELL)
    # LEGS: short and stout, dark, paired close under each mass.
    for (lx, tone, lit) in ((7, "j", "N"), (10, "N", "p"), (19, "j", "N"), (22, "N", "p")):
        g.rect(lx, 24, lx + 1, 29, tone)
        g.line(lx, 24, lx, 29, lit)
        g.span(FEET, lx - 1, lx + 1, "J")
        g.set(lx - 1, FEET, "N")
    # THE FLEECE: one low mass, lumpy at every edge.
    for (cx, cy, r) in ((9.0, 18.5, 5.4), (15.0, 17.0, 6.0), (19.5, 18.0, 5.0), (13.0, 21.0, 5.0),
                        (7.0, 21.5, 3.6), (18.5, 22.0, 3.6)):
        g.disc(cx, cy, r, "n")
    for (cx, cy, r) in ((4.6, 18.5, 1.6), (6.0, 14.6, 1.8), (10.0, 12.4, 1.8), (14.0, 10.8, 1.9),
                        (18.0, 11.6, 1.8), (21.6, 14.2, 1.5), (5.6, 24.2, 1.5), (10.5, 25.4, 1.4),
                        (15.5, 25.6, 1.5), (20.0, 25.0, 1.3)):
        g.disc(cx, cy, r, "n")
    cut(g, (8, 12), (12, 11), (16, 10), (20, 12), (4, 21), (13, 26), (18, 26))
    pts = [(x, y) for y in range(CELL) for x in range(CELL) if g.get(x, y) == "n"]
    for (x, y) in pts:
        if g.get(x, y - 1) == "." or g.get(x - 1, y) == ".":
            g.set(x, y, "k")
        elif g.get(x, y + 1) == "." or g.get(x + 1, y) == ".":
            g.set(x, y, "m")
    # THE LOCKS: little curls all over, each a lit crescent over a dark one - brighter on the back,
    # darker toward the belly, uneven, never in rows.
    for (x, y) in ((7, 16), (11, 14), (15, 13), (18, 15), (9, 19), (13, 17), (16, 19), (20, 18),
                   (6, 21), (11, 22), (14, 21), (17, 23), (8, 24), (21, 21), (12, 25), (4, 19),
                   (13, 12), (9, 14), (17, 12)):
        if inside(g, x, y) and inside(g, x + 1, y + 1):
            top = y < 18
            g.set(x, y, "s" if (top and (x + y) % 3 == 0) else "k")
            g.set(x + 1, y, "k" if top else "n")
            g.set(x, y + 1, "m")
            g.set(x + 1, y + 1, "m" if (x + y) % 2 else "n")
    rim(g, 9, 29, skip="o")
    # THE HEAD, lowered and pushed forward: a dark Down face, the nose blunt.
    blob(g, 25.8, 20.8, 3.0, 3.4, "N", "p", "j", deep="J")
    for (x, y, ch) in ((27, 23, "N"), (28, 23, "j"), (28, 22, "N"), (27, 24, "J"), (26, 24, "j"),
                       (28, 21, "p"), (29, 22, "N"), (29, 23, "J"), (24, 23, "j")):
        g.set(x, y, ch)
    g.set(29, 24, "o")                                  # the mouth
    g.set(28, 24, "J")
    # A wool topknot over the poll.
    for (x, y, ch) in ((22, 14, "k"), (23, 14, "n"), (21, 15, "k"), (22, 15, "n"), (23, 15, "m")):
        g.set(x, y, ch)
    # THE FAR HORN: only its outer turn, showing above and behind the poll.
    for k in range(196, 286, 5):
        a = math.radians(k)
        g.set(21.4 + 4.6 * math.cos(a), 18.0 + 4.6 * math.sin(a), GOLD_SH)
        g.set(21.4 + 3.8 * math.cos(a), 18.0 + 3.8 * math.sin(a), GOLD_DP)
    # THE NEAR HORN: a flat spiral from the poll back, down behind the eye, forward under it and up,
    # each turn lying on the last. A pixel is horn where it falls inside the band at its angle; the
    # band's outer edge is lit on the key side, its inner edge is the groove, and ridges cross it.
    cx, cy, sweep = 22.6, 18.6, 330.0
    for y in range(11, 26):
        for x in range(16, 30):
            dx, dy = x - cx, y - cy
            rr = math.hypot(dx, dy)
            th = math.degrees(math.atan2(dy, dx)) % 360.0
            phi0 = (265.0 - th) % 360.0
            best = None
            for n in (0, 1):
                phi = phi0 + 360.0 * n
                if phi > sweep:
                    continue
                ro = 4.7 - 3.0 * phi / sweep
                w = 3.1 - 1.7 * phi / sweep
                if ro - w <= rr <= ro:
                    best = (phi, ro - rr, w)
            if best is None:
                continue
            phi, d, w = best
            if d < 0.8:
                ch = GOLD_LIT if (dy < 0.5 and dx < 1.0) else GOLD
            elif d > w - 0.8:
                ch = GOLD_DP
            else:
                ch = GOLD_SH if int(phi / 26.0) % 2 else GOLD
            g.set(x, y, ch)
    contact(g, 4, 29)
    taken(g, 21, 26)
    return g.rows()


# =================================================================================================
# WALL - Night Shepherd. A shepherd in a heavy hooded cloak, leaning on his crook.
# =================================================================================================

def shepherd():
    """The shepherd of the night fold, broad in a heavy hooded cloak, leaning forward on his crook.

    The crook is the attachment: a long staff planted before him and leant on with both hands, its
    horn hook curled over at the top higher than his hood, so filled black he is a broad hooded
    shape and a hook. The cloak is heather-dyed wool in violet, carried in violet's lit and hi stops
    (its deep end on the night turf is nothing), with a shoulder cape over it parted from the cloak by
    a line of occ - two layers of one cloth only read as two with something dark between - and a
    leather scrip at the hip.
    """
    g = G(CELL)
    # THE CLOAK: broad, heavy, falling nearly to the ground, leaning forward into the crook.
    mass(g, [(11, 9, 18), (14, 7, 20), (19, 5, 21), (24, 4, 22), (28, 4, 22)], "v", "V", "w")
    for (x, d) in zip(range(4, 23), [1, 2, 1, 1, 0, 2, 1, 2, 1, 0, 1, 2, 1, 1, 0, 1, 2, 1, 1]):
        for k in range(1, d + 1):
            g.set(x, 28 + k, "w" if k < d else "x")
    fold(g, [(8, 20), (6, 28)], "w", "V")
    fold(g, [(12, 21), (11, 29)], "x", "v")
    fold(g, [(16, 21), (17, 29)], "w", "V")
    fold(g, [(20, 21), (21, 28)], "x", None)
    for (x, y) in ((9, 24), (14, 26), (18, 25), (5, 25)):
        g.set(x, y, "V")
    # Boots showing under the hem.
    for (bx, tone, lit) in ((8, "j", "N"), (15, "N", "p")):
        g.span(FEET, bx, bx + 3, tone)
        g.set(bx, FEET, lit)
    # THE SCRIP: a leather bag on a strap at the back hip.
    g.rect(3, 21, 6, 25, WOOD)
    g.span(21, 3, 6, WOOD_LIT)
    g.set(3, 22, WOOD_LIT)
    g.span(25, 4, 6, WOOD_DK)
    g.set(5, 23, WOOD_DK)
    # THE SHOULDER CAPE: a short second layer, ending in an uneven hem with occ under it.
    mass(g, [(10, 9, 18), (12, 7, 20), (16, 5, 22), (19, 5, 22)], "V", "v", "w")
    for (x, y) in ((6, 20), (9, 20), (13, 21), (16, 20), (20, 20), (21, 20)):
        g.set(x, y, "v")
    g.line(5, 21, 22, 21, "o")
    for (x, y) in ((7, 20), (11, 20), (15, 21), (19, 20)):
        g.set(x, y, "w")
    fold(g, [(9, 13), (7, 19)], "w", "V")
    fold(g, [(15, 14), (14, 19)], "x", None)
    g.line(18, 12, 6, 20, "x")                          # the scrip's strap under the cape
    # THE HOOD, deep, pulled forward over a void.
    mass(g, [(4, 12, 15), (5, 10, 17), (7, 9, 19), (10, 9, 20), (12, 10, 19)], "v", "V", "w")
    g.set(16, 3, "V")
    g.set(17, 4, "v")
    roster.void(g, 7, 11, 1.6, 1.4, cx=17.0)
    fold(g, [(12, 5), (11, 10)], "w", "V")
    g.set(19, 6, "w")
    rim(g, 2, 29, skip="o")
    # THE ARMS forward to the crook, both hands on it.
    _limb(g, 18, 14, 22, 15, 1.2, "v", "V", "w")
    _limb(g, 17, 17, 22, 18, 1.0, "w", "v", "x")
    # THE CROOK, after the rim: a staff from the ground before him up past the hood; the horn hook
    # curled over at its top, open toward him.
    pole(g, 24, FEET, 23, 6)
    for (x, y, ch) in ((23, 5, "k"), (23, 4, "s"), (24, 3, "s"), (25, 2, "k"), (26, 2, "k"), (27, 3, "n"),
                       (28, 4, "n"), (28, 5, "m"), (27, 6, "n"), (26, 6, "m"), (24, 4, "k"), (25, 3, "n"),
                       (26, 3, "n"), (27, 4, "m"), (23, 6, "n"), (24, 5, "n")):
        g.set(x, y, ch)
    hand(g, 22, 14)
    hand(g, 22, 17)
    contact(g, 4, 25)
    taken(g, 9, 16)
    return g.rows()


# =================================================================================================
# RANGED - Flint-Slinger. A shepherd whirling a sling over his head, a flint at its edge.
# =================================================================================================

def slinger():
    """A shepherd with a sling, the arm straight up and the sling whirling round over his head, a
    flint in its pouch - the thing he throws, in the chapter's metal.

    The whirl is the attachment: a CLOSED ring of cord spun level over his head, brightest just behind
    the pouch, with the flint glinting out at its leading edge - filled black, a figure under a hoop.
    It is a closed ring, level, with no straight cord in it, on purpose: a sling drawn beside the body
    was an arc and a string, and a cord drawn from the hand to the pouch was an arrow nocked on it -
    a bow, which the Unstrung Hunter owns. The flint is the caster's detached orb (art-direction
    section 4). He is tall and narrow, a linen tunic under a sheepskin (bone, its curls lit), a close
    linen hood, and a bag of flints at the hip.
    """
    g = G(CELL)
    # LEGS, bare under the tunic's hem, a step forward; rough shoes.
    _limb(g, 12, 26, 11, 28, 1.0, "A", "E", "J")
    _limb(g, 15, 26, 16, 28, 1.1, "E", "e", "A")
    boot(g, 9, 11, 29, "j", "N", "J")
    boot(g, 15, 17, 29, "N", "p", "J")
    # THE TUNIC: narrow, belted, to the knee.
    mass(g, [(15, 10, 16), (19, 9, 17), (24, 9, 17), (27, 8, 18)], "e", "l", "E")
    fold(g, [(11, 22), (10, 27)], "E", "l")
    fold(g, [(15, 22), (15, 27)], "A", "e")
    cut(g, (13, 27))
    g.span(21, 9, 17, "o")                              # the belt
    g.set(9, 21, WOOD_DK)
    # THE BAG OF FLINTS at the hip, sagging with them.
    g.rect(5, 22, 8, 26, WOOD)
    g.span(22, 5, 8, WOOD_LIT)
    g.set(5, 23, WOOD_LIT)
    g.span(26, 6, 8, WOOD_DK)
    g.set(7, 24, "j")
    g.set(6, 25, "p")
    # THE SHEEPSKIN over the shoulders: bone, the curls lit, a ragged edge.
    mass(g, [(14, 9, 16), (16, 8, 18), (19, 8, 18), (20, 9, 17)], "k", "s", "n")
    for (x, y) in ((9, 15), (12, 14), (15, 16), (10, 17), (13, 17), (16, 18)):
        if inside(g, x, y):
            g.set(x, y, "s")
            g.set(x + 1, y, "k")
            g.set(x, y + 1, "n")
            g.set(x + 1, y + 1, "m")
    for (x, y) in ((8, 20), (11, 20), (14, 20), (17, 20)):
        g.set(x, y, "n")
    # THE HOOD: close to the head, a short tail hanging behind.
    mass(g, [(9, 11, 14), (10, 10, 16), (12, 9, 16), (14, 10, 16), (15, 10, 15)], "e", "l", "E")
    g.set(9, 13, "E")
    g.set(8, 14, "A")
    g.set(8, 15, "E")
    roster.void(g, 11, 13, 1.2, 1.0, cx=14.6)
    rim(g, 8, 29, skip="o")
    # THE ARM straight up to the sling; the far one down at the bag.
    _limb(g, 15, 15, 16, 8, 1.2, "e", "l", "E")
    hand(g, 15, 6)
    _limb(g, 10, 16, 9, 21, 0.9, "E", "e", "A")
    # THE WHIRL: a closed ring of cord, level over his head. Its near half (the lower arc) is the
    # brighter; the stretch just behind the pouch is brightest, where the cord has only now passed.
    cx, cy, rx, ry = 16.0, 4.6, 7.0, 1.9
    lead = 18
    for k in range(0, 360, 3):
        a = math.radians(k)
        lag = (lead - k) % 360                          # degrees behind the pouch
        near = math.sin(a) > 0
        tone = "L" if lag < 60 else ("l" if near else "e")
        g.set(cx + rx * math.cos(a), cy + ry * math.sin(a), tone)
    # Blur off the far side of the ring.
    for (x, y, ch) in ((8, 3, "E"), (24, 3, "E"), (11, 2, "E")):
        g.set(x, y, ch)
    # THE FLINT in the pouch at the ring's leading edge, the far shore's silver: white at its core.
    a = math.radians(lead)
    fx, fy = int(round(cx + rx * math.cos(a) + 0.6)), int(round(cy + ry * math.sin(a)))
    for (dx, dy, ch) in ((0, 0, FLINT_CORE), (1, 0, FLINT_HOT), (0, -1, FLINT_HOT), (-1, 0, FLINT_MID),
                         (1, 1, FLINT_MID), (0, 1, FLINT_MID), (1, -1, FLINT_MID), (-1, 1, FLINT_EDGE),
                         (2, 0, FLINT_EDGE), (-1, -1, FLINT_EDGE)):
        g.set(fx + dx, fy + dy, ch)
    contact(g, 6, 19)
    taken(g, 12, 14)
    return g.rows()


# =================================================================================================
# TURRET - Cliff Beacon. The watch's iron fire-basket on its post: built, so it burns.
# =================================================================================================

def beacon():
    """The cliff-watch's beacon: an iron fire-basket on a post, banked coals in it, standing in a
    cairn of flints. BUILT, so it burns - the one fire on the downs.

    The basket is the attachment: a cup of iron straps flaring out from a collar at the post's top,
    with the night showing between the straps, so filled black it is a cup on a stick - nothing else
    here is. The fire is BANKED, not blazing: dark coals heaped in the cage with fire glowing through
    the gaps and two short tongues over the rim, and the two hottest pixels where a taken thing
    would stare - the mark of a thing he made. Iron in steel's shade and base (steel's deep end is
    nothing on the night turf), the post grey weathered wood bound with iron, braced to the ground.
    """
    g = G(CELL)
    # THE CAIRN it stands in: flints and lumps of chalk piled round the post's foot.
    for (cx, cy, r, mid, lit, dark) in ((12.0, 28.0, 2.6, "N", "p", "j"), (20.5, 28.0, 2.4, "n", "k", "m"),
                                        (16.0, 27.0, 2.8, "k", "s", "n"), (9.0, 29.0, 1.6, "n", "k", "m"),
                                        (23.5, 29.0, 1.7, "N", "p", "j"), (16.5, 29.0, 2.0, "N", "p", "j")):
        blob(g, cx, cy, r, r * 0.8, mid, lit, dark)
    for (x, y) in ((11, 27), (21, 29), (18, 28)):
        g.set(x, y, "o")
    g.set(14, 26, "S")
    # THE BRACES: two struts from the post to the cairn.
    twig(g, [(15, 19), (10, 27)], WOOD_LIT, WOOD_DK)
    twig(g, [(17, 19), (22, 27)], WOOD, WOOD_DK)
    # THE POST: weathered wood, three wide, bound with iron.
    for y in range(12, 27):
        g.set(15, y, WOOD_LIT)
        g.set(16, y, WOOD)
        g.set(17, y, WOOD_DK)
    for y in (16, 22):
        g.span(y, 15, 17, "R")
        g.set(15, y, "r")
        g.set(14, y, "q")
        g.set(18, y, "q")
    g.set(16, 14, WOOD_DK)
    g.set(16, 19, WOOD_DK)
    g.set(15, 24, WOOD)
    # THE COLLAR at the post's head, and the basket's floor.
    g.span(12, 13, 19, "R")
    g.span(11, 12, 20, "r")
    g.set(12, 11, "t")
    g.set(20, 11, "q")
    # THE BASKET: straps flaring from the collar to an upper ring, coals heaped inside.
    top, bot = 5, 11
    for y in range(top, bot):
        t = (y - top) / float(bot - top)
        lo, hi = int(round(8.0 + 4.0 * t)), int(round(24.0 - 4.0 * t))
        for x in range(lo, hi + 1):
            g.set(x, y, "J" if (x + y) % 3 else "o")    # coals, dark, in the cage
    # Fire through the coals: banked, a glow in the gaps, hottest low in the middle.
    for (x, y, ch) in ((13, 9, "~"), (14, 8, "="), (17, 9, "="), (19, 8, "~"), (12, 7, "~"), (20, 6, "~"),
                       (16, 7, "="), (15, 10, "~"), (18, 6, "=")):
        g.set(x, y, ch)
    # The straps, iron, over the coals: the near ones lit on the key side.
    for (x0, x1, lit) in ((8, 12, "r"), (12, 14, "r"), (16, 16, "R"), (20, 18, "R"), (24, 20, "q")):
        g.line(x0, top, x1, bot - 1, lit)
    g.span(top, 8, 24, "r")                             # the upper ring
    g.span(top - 1, 9, 23, "R")
    g.set(8, top, "t")
    g.set(9, top - 1, "t")
    g.set(24, top, "q")
    g.set(23, top - 1, "q")
    # Two short tongues of flame over the rim, leaning off the sea wind.
    for (x, y, ch) in ((13, 3, "="), (12, 2, "~"), (19, 3, "~"), (14, 3, "-"), (18, 2, "~")):
        g.set(x, y, ch)
    rim(g, 2, 29, skip="o=~-+")
    contact(g, 7, 25)
    built(g, 15, 8)
    return g.rows()


# =================================================================================================
# CHARGER - Riderless Horse. The hunt's horse that lost its rider, its neck out to charge.
# =================================================================================================

def hunt_horse():
    """The hunt's horse with nobody on it: saddled, a stirrup swinging, the reins trailing, its neck
    stretched out and its head driven forward as it comes at you.

    The empty saddle is the attachment - a seat with a high cantle on a horse's back and nothing in it,
    and a stirrup swinging free under the belly with daylight round it - so filled black it is a horse
    with a saddle's dip on its back and a stirrup under it, never the hound (long, light, off the
    ground) or the tup (low, lumpy, horned). A black horse, as the riderless horse at a funeral is:
    stone, carried in its lit and hi stops, with a white blaze down its face and two white socks so its
    head and its stride read on the night turf (on the chalk, whose value is stone's top stop, it is
    the rim, the socks and the tack that part it from the ground). Built from masses - haunch, barrel,
    chest, a neck drawn as one tapering wedge and a head as another, parted at the jaw - in a galloping
    stride: the hind legs paired at the back and the fore pair at the front with the belly's daylight
    between, the near fore reaching. The first pass hung its head straight down and it grazed; the
    second stood its four legs at even spacing and it was a table.
    """
    g = G(CELL)
    mid, lit, dark, deep = "p", "P", "N", "j"
    # THE FAR LEGS, darker: the far fore planted ahead, the far hind swinging forward under the haunch.
    for pts, th in (([(19, 16), (21, 21), (22, 25), (23, 29)], 1.1),
                    ([(11, 15), (12, 19), (9, 22), (10, 26)], 1.2)):
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            _limb(g, x0, y0, x1, y1, th, dark, mid, deep)
        fx, fy = pts[-1]
        g.rect(fx - 1, fy, fx + 1, fy + (FEET - fy if fy > 27 else 1), "J")
    # THE TAIL: long, streaming back off the croup.
    _limb(g, 6, 10, 4, 14, 1.7, mid, lit, dark)
    _limb(g, 4, 14, 4, 21, 1.2, dark, mid, deep)
    g.set(5, 22, deep)
    # THE MASSES: haunch, barrel, chest.
    blob(g, 9.5, 13.0, 4.2, 4.2, mid, lit, dark, deep=deep)
    blob(g, 19.5, 13.6, 3.6, 4.1, mid, lit, dark, deep=deep)
    for x in range(10, 20):
        t = (x - 10) / 9.0
        top = 9.6 + 0.7 * math.sin(t * math.pi)
        bot = 17.6 + 0.3 * math.sin(t * math.pi)
        for y in range(int(round(top)), int(round(bot)) + 1):
            g.set(x, y, mid)
        g.set(x, int(round(top)), lit)
        g.set(x, int(round(bot)), dark)
        g.set(x, int(round(bot)) - 1, dark if x % 3 else deep)
    fold(g, [(11, 11), (13, 14), (12, 17)], dark, lit)   # the thigh's line
    fold(g, [(18, 11), (17, 15)], dark, lit)             # the shoulder's
    # THE NECK: a tapering wedge from the withers and the chest up and forward to the poll.
    lay(g, poly_pts([(18.5, 9.5), (23.5, 5.5), (25.5, 6.5), (24.5, 9.5), (22.0, 15.5), (19.5, 15.0)]),
        mid, lit, dark)
    # THE HEAD: a long narrow wedge driven forward and down from the poll, parted from the neck at the jaw.
    lay(g, poly_pts([(23.8, 4.6), (26.0, 4.4), (30.8, 9.6), (30.8, 11.8), (29.0, 12.4), (25.0, 8.8)]),
        lit, "P", mid)
    blob(g, 25.2, 7.8, 1.6, 1.6, mid, lit, dark)       # the round of the jaw
    g.line(24, 9, 28, 12, deep)                         # the jaw's edge, parting the head from the neck
    g.set(29, 11, deep)                                 # the nostril
    g.span(12, 28, 29, dark)                            # the lip
    # THE BLAZE: white down the face.
    for (x, y, ch) in ((25, 5, "k"), (26, 6, "s"), (27, 7, "k"), (28, 8, "k"), (29, 9, "n"), (30, 10, "n")):
        g.set(x, y, ch)
    # THE EARS, laid back, and the mane flying off the top of the neck.
    for (x, y, ch) in ((23, 4, mid), (22, 3, dark), (24, 3, mid), (24, 2, dark)):
        g.set(x, y, ch)
    for (x, y, ch) in ((19, 8, dark), (20, 7, deep), (21, 6, dark), (22, 5, deep), (18, 8, deep),
                       (20, 6, lit), (22, 4, dark), (17, 9, dark), (21, 7, lit)):
        g.set(x, y, ch)
    # THE SADDLE-CLOTH, old red, and on it the saddle: a high cantle, a dipped seat, the pommel.
    for y in range(10, 14):
        g.span(y, 10, 17, RED)
    g.span(13, 10, 17, RED_SH)
    g.line(10, 10, 10, 13, RED_LIT)
    g.set(17, 12, RED_DP)
    g.set(13, 13, RED_DP)
    g.span(9, 11, 17, WOOD)
    g.span(8, 13, 16, WOOD_DK)                          # the seat, dipped
    for (x, y, ch) in ((11, 6, WOOD_LIT), (11, 7, WOOD_LIT), (12, 7, WOOD), (11, 8, WOOD), (12, 8, WOOD),
                       (17, 7, WOOD_LIT), (17, 8, WOOD), (16, 8, WOOD), (10, 7, WOOD_DK), (10, 6, WOOD_DK)):
        g.set(x, y, ch)
    g.span(9, 11, 13, WOOD_LIT)
    g.line(16, 13, 16, 17, WOOD_DK)                     # the girth
    # THE BRIDLE, and the reins hanging slack in a loop under the neck.
    g.line(24, 5, 28, 10, WOOD_DK)
    g.line(25, 8, 27, 7, WOOD_DK)
    twig(g, [(28, 11), (27, 15), (25, 17), (22, 16)], WOOD, WOOD_DK)
    rim(g, 2, 28, skip="o" + RED + RED_SH + RED_LIT + RED_DP)
    # THE STIRRUP swinging free under the belly: the leather, then the iron, daylight round it.
    twig(g, [(14, 12), (14, 16), (15, 19)], WOOD, WOOD_DK)
    for (x, y, ch) in ((14, 20, "t"), (15, 20, "T"), (16, 20, "r"), (14, 21, "r"), (16, 21, "R"),
                       (14, 22, "R"), (15, 22, "r"), (16, 22, "q")):
        g.set(x, y, ch)
    g.set(15, 21, ".")
    # THE NEAR LEGS: the foreleg reaching out ahead off the ground, the hind leg driving back off it.
    for pts, th in (([(21, 15), (25, 18), (28, 22), (29, 26)], 1.4),
                    ([(8, 15), (9, 19), (6, 23), (5, 28)], 1.6)):
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            _limb(g, x0, y0, x1, y1, th, mid, lit, dark)
    g.rect(28, 26, 30, 27, "J")                         # the reaching hoof
    g.set(28, 26, "j")
    g.rect(4, 29, 6, FEET, "J")                         # the driving hoof
    g.set(4, 29, "j")
    for (x, y, ch) in ((5, 26, "k"), (6, 26, "n"), (5, 27, "k"), (6, 27, "n"), (5, 28, "n"),
                       (28, 23, "k"), (29, 24, "k"), (28, 24, "n"), (29, 25, "n")):
        g.set(x, y, ch)                                 # two white socks, near hind and near fore
    contact(g, 2, 26)
    taken(g, 7, 25)
    return g.rows()


# =================================================================================================
# EXPLODER - Giant Puffball. Swollen, cracking, about to burst. Faces left like its stand-in.
# =================================================================================================

def puffball():
    """A giant puffball of the sheep-cropped turf, swollen past its skin, the skin cracked into plates
    and torn open down its front, shuffling on the threads it grew from.

    The swell is the attachment - the only round thing on the downs, wider than it is tall and lumpy
    on its crown - and it is CRACKED: the pale skin broken into uneven plates (each lit on its upper
    edge) with the darker flesh in the joints, which is what tells it from a stone or an egg. No cap and
    no stem: a ball. Bone for the skin, carried in its base with the lit stop as a plane on the crown;
    the spore-flesh olive-dark through one long ragged tear (linen's lower stops and old wood); a puff
    of spores already leaking. The mark looks out of the tear. It FACES LEFT, like every exploder,
    because its stand-in says SpriteFacesRightByDefault = false. The first pass split it with a round
    hole and it was a skull; the tear is long and narrow now, and the plates run over the whole crown.
    """
    g = G(CELL)
    # THE THREADS it walks on: short root-strands splayed out under it.
    for (x0, x1) in ((9, 6), (12, 11), (17, 18), (21, 24), (14, 15)):
        twig(g, [(x0, 26), (x1, FEET)], "m", "J")
    # THE BALL: a union of discs, wider than tall, lumpy on the crown.
    ball = set()
    for (cx, cy, r) in ((16.0, 19.5, 8.0), (10.0, 20.0, 6.2), (22.0, 20.0, 6.2), (12.8, 14.4, 4.6),
                        (19.6, 14.0, 4.8), (16.2, 12.8, 3.2)):
        for y in range(int(cy - r) - 1, int(cy + r) + 2):
            for x in range(int(cx - r) - 1, int(cx + r) + 2):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r + 0.3 and y <= 27:
                    ball.add((x, y))
    # Shade it as one round form lit from the upper left: a lit plane on the crown, base round the
    # middle, shade on the far side and lower down where it sags onto the turf.
    for (x, y) in ball:
        nx, ny = (x - 16.0) / 11.0, (y - 18.0) / 9.0
        k = -(nx * 0.7 + ny)
        if k > 0.55:
            ch = "s"
        elif k > -0.2:
            ch = "k"
        elif k > -0.75:
            ch = "n"
        else:
            ch = "m"
        if (x, y - 1) not in ball and y < 22:
            ch = "s" if x < 22 else "k"
        g.set(x, y, ch)
    # THE PLATES: the skin cracked into uneven polygons - a pixel is a crack where it lies about as
    # near two plate centres as one. Seeds placed by hand, so no two plates are the same size.
    seeds = [(9, 15), (13, 12), (18, 11), (22, 14), (25, 18), (11, 19), (16, 16), (20, 19), (14, 22),
             (21, 23), (8, 22), (17, 25), (24, 23), (6, 18)]
    for (x, y) in ball:
        if y > 25:
            continue
        ds = sorted((x - sx) ** 2 + (y - sy) ** 2 for (sx, sy) in seeds)
        if math.sqrt(ds[1]) - math.sqrt(ds[0]) < 0.75:
            here = g.get(x, y)
            g.set(x, y, {"s": "n", "k": "m", "n": "m", "m": "J"}[here])
            if (x + 1, y + 1) in ball and g.get(x + 1, y + 1) in ("k", "n"):
                g.set(x + 1, y + 1, {"k": "s", "n": "k"}[g.get(x + 1, y + 1)])
    # THE TEAR down its front (its left), ragged, the spore-flesh bulging olive out of it.
    for (y, a, b) in ((11, 9, 9), (12, 8, 9), (13, 8, 9), (14, 7, 9), (15, 7, 8), (16, 6, 8), (17, 6, 8),
                      (18, 6, 8), (19, 7, 8), (20, 7, 8), (21, 7, 7), (22, 8, 8), (23, 8, 8)):
        g.span(y, a, b, "A")
        g.set(b + 1, y, "o")
    for (x, y, ch) in ((8, 13, "E"), (7, 15, "E"), (7, 19, "E"), (7, 20, "e"), (8, 22, GOLD_DP)):
        g.set(x, y, ch)
    for (x, y) in ((8, 11), (7, 13), (6, 15), (5, 17), (6, 19), (6, 21), (7, 23)):
        g.set(x, y, "s" if y < 17 else "k")             # the torn lip, pale
    # Spores leaking from the crown.
    for (x, y, ch) in ((15, 8, "e"), (13, 7, "E"), (17, 6, "e"), (12, 4, "E"), (16, 3, "E"), (19, 5, "E")):
        g.set(x, y, ch)
    rim(g, 7, 27, skip="o")
    # THE TURF it pushed up through, torn round its foot.
    for (x, y, ch) in ((5, 29, MOSS_LIT), (6, 28, MOSS), (25, 29, MOSS_LIT), (26, 28, MOSS),
                       (25, 28, MOSS_SH), (7, 29, MOSS_SH), (20, 29, MOSS), (13, 29, MOSS_LIT)):
        g.set(x, y, ch)
    contact(g, 5, 27)
    taken(g, 17, 6)
    return g.rows()


# =================================================================================================
# SLAMMER - The Warrener. His long warren-spade raised to bring down on the turf.
# =================================================================================================

def warrener():
    """The warrener who kept the downs' rabbit-warrens, his long-handled warren-spade raised over his
    head in both hands, the blade wound back to be brought down on the turf.

    The spade's blade is the highest thing in the cell, a broad steel blade on the line of a haft that
    runs from his hands back over his head - so filled black he is a short square figure under a blade
    raised high, with daylight between each arm and his head. He is drawn short and stout to leave the
    spade room above him: a coat of black stone carried up its ramp (the warrener's moleskin), the
    linen sleeves of his shirt bared by the raised arms, a cap of rabbit fur with its flaps down (bone
    - the pale thing on a dark man), and a coil of purse-nets at his belt.
    """
    g = G(CELL)
    # THE SPADE: the haft from the grip over his near hand, back and up to the socket; the blade on
    # its line, lit on its upper edge, its cutting edge catching the key.
    pole(g, 22, 12, 12, 6)
    ux, uy = -0.857, -0.514
    px, py = -uy, ux                                    # across the blade, toward its upper edge
    ex, ey = 12.0 + ux * 5.8, 6.0 + uy * 5.8
    blade = poly_pts([(12.0 + px * 1.4, 6.0 + py * 1.4), (ex + px * 1.5, ey + py * 1.5),
                      (ex - px * 1.5, ey - py * 1.5), (12.0 - px * 1.4, 6.0 - py * 1.4)])
    lay(g, blade, "r", "t", "R")
    for (x, y) in blade:
        if (x - 1, y) not in blade and (x, y - 1) not in blade:
            g.set(x, y, "T")
    g.set(12, 6, "q")                                   # the socket
    g.set(13, 7, "R")
    g.line(22, 11, 22, 13, WOOD_DK)                     # the grip's crosspiece
    g.set(22, 11, WOOD)
    # LEGS, short and planted wide, in gaiters.
    for (lx, tone, lit) in ((10, "A", "E"), (17, "E", "e")):
        g.rect(lx, 26, lx + 2, 28, tone)
        g.line(lx, 26, lx, 28, lit)
        boot(g, lx - 1, lx + 2, 29, "j", "N", "J")
    # THE COAT: square, stout, to the thigh; stone carried up.
    SB.swell(g, 17, 27, [5.2, 5.4, 5.2, 5.6, 6.2], "N", "p", "j", cx=15.0)
    fold(g, [(12, 19), (11, 23), (12, 27)], "j", "p")
    fold(g, [(17, 19), (18, 27)], "J", None)
    for (x, y) in ((11, 20), (13, 25), (16, 22)):
        g.set(x, y, "P")
    g.rect(16, 23, 19, 25, "j")                          # a deep pocket and its flap
    g.span(23, 16, 19, "p")
    g.span(22, 9, 20, "o")                               # the belt
    g.set(13, 22, WOOD_LIT)
    # A coil of purse-nets hanging at the back hip: pale cord in loops.
    for (x, y, ch) in ((8, 23, "n"), (9, 23, "k"), (7, 24, "n"), (10, 24, "m"), (7, 25, "k"), (10, 25, "n"),
                       (8, 26, "n"), (9, 26, "k"), (8, 24, "o"), (9, 25, "o")):
        g.set(x, y, ch)
    # THE HEAD: a cap of rabbit fur with its flaps down, the face in its shadow.
    for (y, a, b) in ((11, 13, 17), (12, 12, 18), (13, 12, 18), (14, 12, 13), (15, 12, 13), (14, 17, 18),
                      (15, 18, 18)):
        g.span(y, a, b, "k")
        g.set(a, y, "s")
        g.set(b, y, "n")
    g.span(11, 14, 16, "s")
    for (x, y) in ((14, 12), (16, 13), (13, 13)):
        g.set(x, y, "n")                                # the fur's tufts
    g.set(13, 15, "m")
    g.set(12, 16, "n")
    roster.void(g, 14, 16, 1.6, 1.4, cx=15.6)
    g.span(17, 13, 18, "e")                              # the shirt's collar
    # ARMS: both up from the shoulders, past the head, the linen sleeves bared.
    _limb(g, 11, 19, 14, 9, 1.2, "e", "l", "E")
    _limb(g, 20, 19, 20, 12, 1.2, "e", "l", "E")
    rim(g, 2, 29, skip="o")
    hand(g, 14, 7)
    hand(g, 19, 10)
    contact(g, 8, 21)
    taken(g, 15, 14)
    return g.rows()


# =================================================================================================
# SUMMONER - Huntsman. The horn at his mouth, calling the hounds on.
# =================================================================================================

def huntsman():
    """The hunt's huntsman with his horn raised to his mouth, calling the hounds on.

    The horn is the attachment: a straight brass horn from the void of his face up and forward, its
    bell flared at the top of the silhouette, so filled black he is an upright figure with a line rising
    off his face to a flare. Hunt green - lichen, carried in its lit and base stops - for a coat with
    flared skirts, a white stock at the throat, a red collar, a round velvet cap with a short peak, tall
    boots with brown tops, and a short crop in the hand at his side.
    """
    g = G(CELL)
    # LEGS in breeches and tall boots, a stride.
    _limb(g, 13, 23, 11, 26, 1.1, "E", "e", "A")
    _limb(g, 16, 23, 17, 26, 1.2, "e", "l", "E")
    for (x0, x1, tone, lit) in ((9, 11, "j", "N"), (16, 18, "N", "p")):
        g.rect(x0, 26, x1, FEET, tone)
        g.span(26, x0, x1, WOOD)                         # the brown tops
        g.set(x0, 26, WOOD_LIT)
        g.line(x0, 27, x0, 29, lit)
        g.span(FEET, x0, x1 + 1, "J")
    # THE COAT: fitted at the waist, the skirts flaring behind.
    mass(g, [(11, 12, 17), (15, 11, 18), (19, 12, 18)], MOSS, MOSS_LIT, MOSS_SH)
    mass(g, [(19, 11, 18), (22, 8, 19), (25, 6, 19)], MOSS, MOSS_LIT, MOSS_SH)
    cut(g, (19, 24), (19, 25), (18, 25))
    for (x, d) in zip(range(6, 18), [1, 2, 1, 1, 2, 1, 0, 1, 1, 0, 0, 0]):
        for k in range(1, d + 1):
            g.set(x, 25 + k, MOSS_SH if k < d else MOSS_DP)
    fold(g, [(9, 22), (7, 26)], MOSS_SH, MOSS_LIT)
    fold(g, [(13, 20), (12, 26)], MOSS_DP, MOSS_LIT)
    fold(g, [(16, 14), (16, 19)], MOSS_SH, None)
    g.set(12, 14, MOSS_HI)
    g.set(11, 23, MOSS_HI)
    g.span(19, 11, 18, MOSS_DP)                          # the waist seam
    for y in (15, 17):
        g.set(17, y, GOLD_LIT)                           # two brass buttons down the front
    # THE STOCK, white at the throat, and the collar.
    g.rect(15, 11, 17, 13, "k")
    g.set(15, 11, "s")
    g.set(17, 13, "n")
    g.set(16, 14, "k")
    g.span(11, 12, 14, RED)
    g.set(12, 11, RED_LIT)
    g.set(13, 12, RED_SH)
    # THE CAP: a round velvet crown, a short peak; the face in its shadow; the ribbons at the back.
    for (y, a, b) in ((4, 14, 16), (5, 13, 17), (6, 12, 18), (7, 12, 18)):
        g.span(y, a, b, "N")
        g.set(a, y, "p")
        g.set(b, y, "j")
    g.span(4, 14, 15, "p")
    g.set(13, 5, "P")
    g.set(14, 5, "p")
    g.span(8, 15, 19, "J")                              # the peak
    g.set(19, 8, "j")
    g.set(11, 8, "j")                                   # the ribbons
    g.set(11, 9, "J")
    roster.void(g, 8, 10, 1.6, 1.4, cx=16.0)
    g.set(13, 9, "N")
    rim(g, 3, 29, skip="o")
    # THE ARM raised to the horn at his mouth; the far arm down with the crop.
    _limb(g, 16, 13, 20, 12, 1.2, MOSS, MOSS_LIT, MOSS_SH)
    hand(g, 19, 10)
    _limb(g, 12, 13, 10, 20, 1.0, MOSS_SH, MOSS, MOSS_DP)
    g.rect(9, 20, 10, 21, "n")
    g.set(9, 20, "k")
    twig(g, [(10, 21), (8, 27)], WOOD, WOOD_DK)
    g.set(8, 28, WOOD_DK)
    # THE HORN, after the rim: from the mouth up and forward, brass, the bell flared at its end.
    g.line(18, 9, 22, 5, GOLD)
    g.line(18, 10, 22, 6, GOLD_SH)
    g.set(19, 8, GOLD_LIT)
    for (x, y, ch) in ((23, 3, GOLD_LIT), (24, 2, GOLD_LIT), (25, 2, GOLD), (23, 4, GOLD), (24, 3, GOLD_DP),
                       (25, 3, GOLD_DP), (26, 3, GOLD_SH), (24, 4, GOLD_SH), (25, 4, GOLD_SH), (22, 4, GOLD_LIT),
                       (23, 5, GOLD_SH)):
        g.set(x, y, ch)
    contact(g, 7, 20)
    taken(g, 9, 15)
    return g.rows()


# =================================================================================================
# MINIBOSS - Cliff-Watch Captain (48 cell). His jailer on the downs.
# =================================================================================================

def watch_captain():
    """The captain of the watch that kept the white cliffs, his jailer on the downs: steel and mail
    under a heavy watch-cloak, a long boat-hook slanted across him, manacles hung at his belt.

    The boat-hook is the attachment: a pole longer than he is tall, carried slanted across his body
    from low behind to high before, its head a steel spike with a hook swept back from it - a slant,
    never upright, so it can never be read as the Unstrung Hunter's bow. Over it a sallet, its long
    tail swept back and its visor's slit the only face, over a bevor at the chin; a mail coat under a
    steel breast; the cloak plain and black, from the shoulders to the calves (no pelt, no paws). The
    manacles on their chain are what make him the jailer; the belt carries one buckle and no studs.
    The first pass drew the helm as one round dome and he was a ghost in armour; the sallet is a crown,
    a slit, a bevor and a tail now, each parted from the next by a line of shade. The cloak was wool,
    and a broad field of blue on the miniboss was the Archmage's colour on his jailer: it is stone now,
    carried up its ramp, so the steel is the brightest thing on him.
    """
    c = 48
    feet = c - 2
    g = G(c)
    cl, cl_lit, cl_dk, cl_hi = "N", "p", "j", "P"
    # THE CLOAK behind him: black, heavy, from the shoulders to the calves, its hem ragged.
    mass(g, [(14, 15, 25), (20, 11, 26), (30, 8, 27), (40, 6, 27), (43, 6, 27)], cl, cl_lit, cl_dk)
    for (x, d) in zip(range(6, 22), [1, 2, 2, 1, 0, 1, 2, 3, 1, 1, 2, 0, 1, 2, 1, 1]):
        for k in range(1, d + 1):
            g.set(x, 43 + k, cl if k < d else cl_dk)
    for pts in ([(13, 20), (10, 30), (8, 42)], [(16, 22), (14, 33), (13, 43)], [(20, 28), (19, 43)]):
        fold(g, pts, cl_dk, cl_lit)
    for (x, y) in ((11, 24), (9, 34), (12, 38), (10, 25), (8, 35)):
        g.set(x, y, cl_hi)
    # LEGS: greaves over mail, sabatons, a long stance.
    for (x0, x1, key) in ((19, 22, "r"), (27, 30, "R")):
        g.rect(x0, 36, x1, 43, "R")
        g.line(x0, 36, x0, 43, key)
        g.line(x1, 36, x1, 43, "q")
        g.span(39, x0 + 1, x1 - 1, "q")
        g.set(x0 + 1, 37, "t")
        g.rect(x0 - 1, 44, x1 + 1, feet, "R")
        g.span(44, x0 - 1, x1, "r")
        g.set(x0 - 1, 44, "t")
        g.span(feet, x0 - 1, x1 + 2, "q")
    # THE MAIL COAT, to the knee: rings as a checker of two stops with dark between.
    mass(g, [(22, 17, 31), (30, 16, 32), (36, 15, 33), (37, 15, 33)], "R", "r", "q", rim=False)
    for y in range(22, 38):
        for x in range(15, 34):
            if g.get(x, y) == "R" and (x + y) % 2 == 0:
                g.set(x, y, "r" if (x + 2 * y) % 4 else "q")
    for x in range(15, 34):
        if g.get(x, 37) != ".":
            g.set(x, 37, "q" if x % 2 else "R")
    # THE BREAST: a steel plate over the mail, ridged down its middle, a fauld at its foot.
    for y in range(17, 28):
        t = (y - 17) / 10.0
        lo, hi = int(round(18 + 1.0 * t)), int(round(31 - 0.5 * t))
        g.span(y, lo, hi, "r")
        g.set(lo, y, "t")
        g.set(lo + 1, y, "t" if y < 22 else "r")
        g.set(hi, y, "q")
        g.set(hi - 1, y, "R")
        g.set(24, y, "T" if y < 20 else "t")            # the ridge
        g.set(25, y, "R")
    # The breast turned from the key on its far side, and a plackart's edge across its lower half.
    for y in range(17, 28):
        for x in range(27, 30):
            if g.get(x, y) == "r":
                g.set(x, y, "R")
    g.span(23, 19, 29, "q")
    g.span(22, 20, 23, "t")
    for (y, a, b) in ((30, 18, 31), (32, 17, 32)):
        g.span(y, a, b, "R")
        g.span(y + 1, a, b, "q")
        g.set(a, y, "t")
    # THE BELT, one buckle.
    g.span(28, 18, 31, WOOD)
    g.span(29, 18, 31, WOOD_DK)
    g.set(18, 28, WOOD_LIT)
    g.rect(20, 28, 21, 29, GOLD_LIT)
    g.set(21, 29, GOLD_SH)
    # THE MANACLES on their chain at the near hip: two open cuffs.
    for (x, y, ch) in ((31, 29, "r"), (32, 30, "R"), (32, 31, "t"), (33, 32, "R"), (33, 33, "t"),
                       (34, 34, "R"), (34, 35, "t")):
        g.set(x, y, ch)
    for (cx, cy) in ((33.5, 37.5), (37.0, 37.0)):
        for k in range(0, 360, 20):
            a = math.radians(k)
            g.set(cx + 1.6 * math.cos(a), cy + 1.4 * math.sin(a), "t" if 150 < k < 300 else "R")
    g.line(35, 36, 36, 36, "q")
    # THE CLOAK over the far shoulder, its edge falling in front of the arm.
    g.rect(14, 15, 18, 20, cl)
    g.line(14, 15, 14, 20, cl_lit)
    g.span(15, 15, 18, cl_lit)
    g.line(18, 16, 17, 27, cl_dk)
    g.line(17, 18, 16, 30, cl)
    g.set(15, 16, cl_hi)
    # THE GORGET, and the pauldron on the near shoulder, parted from the breast by a line of occ.
    g.span(14, 19, 29, "R")
    g.span(15, 19, 30, "r")
    g.set(19, 15, "t")
    g.span(16, 18, 30, "q")
    for (y, a, b) in ((15, 28, 33), (16, 27, 34), (17, 28, 34), (18, 29, 34)):
        g.span(y, a, b, "r")
        g.set(a, y, "t")
        g.set(b, y, "q")
    g.span(17, 30, 32, "t")
    g.span(19, 29, 34, "o")
    # THE SALLET: a rounded crown, the visor's slit, a bevor at the chin, and the tail swept back.
    for (y, a, b) in ((3, 23, 27), (4, 21, 29), (5, 20, 30), (6, 19, 31), (7, 19, 31), (8, 19, 31),
                      (9, 18, 31)):
        g.span(y, a, b, "r")
        g.set(a, y, "t")
        g.set(b, y, "q")
        g.set(b - 1, y, "R")
    g.span(3, 23, 26, "t")
    g.span(4, 22, 25, "T")
    g.set(21, 5, "T")
    fold(g, [(27, 4), (30, 8)], "R", "t")               # the crown's crease
    for (y, a, b) in ((10, 15, 23), (11, 13, 19), (12, 12, 15)):
        g.span(y, a, b, "R")                            # the tail
        g.set(a, y, "r")
        g.set(b, y, "q")
    g.span(9, 18, 22, "R")
    g.span(10, 24, 31, "o")                              # the visor's slit
    for (y, a, b) in ((11, 23, 31), (12, 23, 30), (13, 24, 29)):
        g.span(y, a, b, "r")                            # the bevor
        g.set(a, y, "t")
        g.set(b, y, "q")
    g.span(11, 24, 30, "t")
    g.set(31, 11, "R")
    g.span(13, 25, 28, "R")
    rim(g, 2, 44, skip="o")
    # THE ARMS on the hook: the near arm down and forward, its hand high on the pole; the far arm
    # across the body to its hand low on the pole.
    _limb(g, 32, 19, 34, 24, 1.8, "R", "r", "q")
    _limb(g, 34, 24, 33, 18, 1.5, "R", "t", "q")
    g.line(31, 20, 32, 25, "o")                         # the arm parted from the breast
    _limb(g, 21, 19, 24, 25, 1.6, "R", "r", "q")
    # THE BOAT-HOOK, slanted across him, after the rim: a pole from low behind to high before.
    roster.shaft(g, 7, 44, 38, 11, WOOD_LIT, WOOD, WOOD_DK, width=2)
    for (x, y) in ((12, 39), (21, 29), (29, 21)):
        g.set(x, y, WOOD_DK)
    # Its head: an iron socket, a spike on the pole's line, and a hook swept back and down from it.
    for (x, y, ch) in ((38, 10, "R"), (39, 9, "r"), (39, 10, "q"), (40, 8, "r"), (40, 9, "R")):
        g.set(x, y, ch)
    SB.spike(g, 41, 7, 3, -3, 1.2, "r", "t", "q")
    for (x, y, ch) in ((41, 9, "r"), (42, 10, "R"), (43, 11, "r"), (43, 12, "q"), (42, 13, "R"),
                       (41, 13, "q"), (42, 11, "q"), (44, 11, "q")):
        g.set(x, y, ch)
    # Gauntlets on the pole.
    for (x, y) in ((32, 17), (23, 25)):
        g.rect(x, y, x + 1, y + 1, "r")
        g.set(x, y, "t")
        g.set(x + 1, y + 1, "q")
    contact(g, 5, 40, y=feet)
    taken(g, 10, 27)
    return g.rows()


CAST = [
    # stem, pose, cell, (lean, reach, knock), SpriteFrames name
    #
    # THE MINIBOSS IS LISTED FIRST: bestiary_kit.report sizes its contact sheet from the first
    # creature, and a 32 cell first crops the 48-cell captain (see cast_sands.py).
    ("downs-watch-captain", watch_captain, 48, (0.5, 4.2, 1), "DownsWatchCaptainFrames"),
    ("downs-beater", beater, 32, (0.6, 2.6, 2), "DownsBeaterFrames"),
    ("downs-courser", courser, 32, (1.8, 3.6, 2), "DownsCourserFrames"),
    ("downs-tup", tup, 32, (0.5, 3.0, 1), "DownsTupFrames"),
    ("downs-shepherd", shepherd, 32, (0.5, 3.0, 1), "DownsShepherdFrames"),
    ("downs-slinger", slinger, 32, (0.9, 2.2, 3), "DownsSlingerFrames"),
    ("downs-beacon", beacon, 32, (0.15, -2.0, 1), "DownsBeaconFrames"),
    ("downs-hunt-horse", hunt_horse, 32, (1.2, 4.6, 2), "DownsHuntHorseFrames"),
    # Faces left (see puffball), so it leans into its burst and is knocked back the other way.
    ("downs-puffball", puffball, 32, (1.0, -2.2, -3), "DownsPuffballFrames"),
    ("downs-warrener", warrener, 32, (0.5, 4.4, 1), "DownsWarrenerFrames"),
    ("downs-huntsman", huntsman, 32, (0.9, 2.6, 2), "DownsHuntsmanFrames"),
]


def build():
    return bestiary_kit.build_cast(CAST, PALETTE)


if __name__ == "__main__":
    bestiary_kit.run(CAST, PALETTE, "downs")
