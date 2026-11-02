# -*- coding: utf-8 -*-
"""The lessons: the short cutscenes that teach a system after the run that found its teacher.

    python tools/art/lessons.py                  # every panel, written to assets/bonelight/ui/lessons/
    python tools/art/lessons.py ascension-rise   # only these panels
    python tools/art/lessons.py --preview        # also write tools/art/_lessons/<panel>.png (gitignored)
    "$GODOT_BIN" --headless --path . --import     # then import, and commit the .import sidecars

See .ai/slow-start.md ("The lessons") for what each shows and why; scripts/Prologue.cs plays them (its
Reels), and scripts/Teachings.cs says when. Runs under Python 2.7 and 3 (PIL only), every random stream
on rng.random(), as prologue.py's are.

THE FRAME, THE LAYERS AND THE AUDIT ARE THE PROLOGUE'S (prologue.py's docstring): 960x840 at a whole x2,
the 360x360 square both canvases always show (columns 300-660, rows 240-600) holding every subject, back
(opaque) + mid + fore (transparent), drifted by Prologue.cs, and zero off-contract pixels. The pieces are
the prologue's too - its heath, standing stones, ward, motes and the player from behind - so a lesson
reads as the story going on, not as a tutorial pasted over it.

WHAT A LESSON IS NOT. The dark wizard is not in any of them, and nothing here sheds dark light: a lesson
is what the wizards knew, found where he never looked. The light in every panel is the player's own (the
light ramp), with the element ramps only where a spell's own light burns - the system is about spells.
"""

from __future__ import division, print_function

import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image

import hero
import prologue as pr
import splashkit as sk

S, V, G, R, W = pr.S, pr.V, pr.G, pr.R, pr.W
LT, AR = pr.LT, pr.AR
E, M, OCC = pr.E, pr.M, pr.OCC
FW, FH = pr.FW, pr.FH

ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "assets", "bonelight", "ui", "lessons")
PREVIEW = os.path.join(HERE, "_lessons")


# --------------------------------------------------------------------------- shared ground
def night(back, horizon, seed, moon=None):
    """The heath at night under the stars, as the prologue's road and moot have it: the sky, a far ridge
    and a near one, and heather from the horizon down."""
    pr.sky(back, 0, horizon, [OCC, S[4], V[4], V[3], V[2]])
    pr.stars(back, (0, 0, FW, horizon - 60), 420, seed)
    if moon:
        sk.moon(back, moon[0], moon[1], moon[2])
    pr.hills(back, pr.ridge_fn(horizon - 22, 14, seed + 1, 0.8), [V[3], V[4], S[4]], 36, lit=V[2])
    pr.hills(back, pr.ridge_fn(horizon, 9, seed + 2, 1.5), [S[4], V[4]], 30, lit=V[3])
    back.vramp(horizon + 4, FH - 1, [V[4], S[4], S[4], OCC])
    pr.heath(back, horizon + 4, FH, 2200, seed + 3, [(S[4], V[3]), (V[4], V[3]), (OCC, V[4]), (S[4], pr.LI[4])])


def near_heath(fore, seed, y0=640, keep_out=None):
    pr.heath(fore, y0, FH, 700, seed, [(OCC, S[4]), (OCC, V[4])], scale=1.7, keep_out=keep_out)


def glow(c, x, y, r, ramp, halo=None):
    """A light that burns: its own ramp at the core, the pigment it lands on round it (halo)."""
    if halo:
        c.radial(x, y, r * 3.0, r * 2.2, [halo[2], halo[2], halo[3], halo[3], None])
    c.radial(x, y, r * 1.9, r * 1.9, [ramp[3], ramp[3], None])
    c.radial(x, y, r, r, [ramp[0], ramp[1], ramp[2], ramp[3]])


def sigil(c, x, y, r, ramp):
    """A spell's form drawn small: the ward's ring and star, in the spell's own ramp."""
    pr.ward(c, x, y, r, r, A=ramp, pool=False)
    c.radial(x, y, r * 0.34, r * 0.34, [ramp[0], ramp[1], ramp[2]])


def arc(c, src, dst, n, seed, ramps, bow, spread=8):
    """Light travelling from src to dst along a bowed path, thickest at its source."""
    pr.motes(c, src, dst, n, seed, ramps, spread=spread, bow=bow)


def crown(c, x, y, w):
    """The Sovereign's crown (LoadoutHud's anointed slot, drawn large): three gold points on a band."""
    u = w / 24.0
    for i in (-1, 0, 1):
        px_ = x + i * 8 * u
        tall = (8 if i == 0 else 6) * u
        c.poly([(px_ - 2.4 * u, y), (px_ + 2.4 * u, y), (px_, y - tall)], G[1])
        c.line(px_, y - tall, px_ + 1.4 * u, y - 1.5 * u, G[2])
        c.disc(px_, y - tall - 1.2 * u, 1.2 * u, 1.2 * u, LT[1])
    c.rect(x - 12 * u, y, x + 12 * u, y + 3 * u, G[1])
    c.hline(x - 12 * u, x + 12 * u, y + 3 * u, G[3])
    for i in (-2, 0, 2):
        c.disc(x + i * 4 * u, y + 1.5 * u, 0.9 * u, 0.9 * u, R[0])


def wax_seal(c, x, y, r):
    """A sealed spell (LoadoutHud's silenced slot): the band across it and a blot of red wax on the band."""
    c.rect(x - r * 1.6, y - r * 0.3, x + r * 1.6, y + r * 0.3, S[4])
    c.disc(x, y, r, r * 0.9, R[2])
    c.disc(x - r * 0.15, y - r * 0.15, r * 0.7, r * 0.6, R[1])
    c.set(x - r * 0.4, y - r * 0.4, R[0])
    c.hline(x - r * 0.4, x + r * 0.4, y + r * 0.1, R[3])


def stone_light(c, x, yb, w, h, seed, ramp, halo, lit=True, ramp_stone=None):
    """A standing stone with a spell's light burning on its crown; returns the light's centre."""
    pr.standing_stone(c, x, yb, w, h, seed, lean=0.0, ramp=ramp_stone or [S[2], S[3], S[4], OCC, OCC],
                      toward=(0, -1, halo[2]))
    top = (x, yb - h - 10)
    if lit:
        glow(c, top[0], top[1], 7, ramp, halo=halo)
    else:
        c.disc(top[0], top[1] + 6, 3, 2.5, pr.ST[3])
        c.set(top[0] - 1, top[1] + 5, pr.ST[2])
    return top


# =========================================================================== the Ascension
def p_ascension_gift():
    """1. "What he missed remembers what the wizards knew." The find that taught it, whichever of the three
    landmarks it was: a warm point of light on the heath, the one he missed giving up what it kept, rising
    off the ground into a spell's sigil - and the player before it, from behind, small, looking up."""
    back, mid, fore = pr.frame(True), pr.frame(), pr.frame()
    night(back, 404, 201, moon=(352, 266, 10))
    # far stones on the ridge: the heath's own, small with distance
    for i, (x, y, w, h, br) in enumerate([(236, 420, 14, 34, True), (322, 412, 11, 26, False),
                                          (640, 414, 12, 28, False), (726, 424, 15, 38, True)]):
        pr.standing_stone(back, x, y, w, h, 610 + i, lean=(x - 480) / 3000.0, broken=br,
                          ramp=[S[2], S[3], S[4], OCC, OCC])
    fx, fy = 480, 486
    back.radial(fx, fy + 6, 200, 52, [G[3], G[3], G[4], G[4], None])
    # the light it kept, coming up out of the ground and gathering into a sigil over it
    sx, sy = 480, 318
    glow(mid, fx, fy, 6, LT, halo=G)
    arc(mid, (fx, fy - 4), (sx, sy + 30), 340, 207, [LT, LT, AR], bow=0, spread=34)
    arc(mid, (fx - 30, fy), (sx - 36, sy + 10), 120, 208, [LT], bow=18, spread=10)
    arc(mid, (fx + 30, fy), (sx + 36, sy + 10), 120, 209, [LT], bow=18, spread=10)
    mid.radial(sx, sy, 70, 70, [G[3], G[3], G[4], None])
    sigil(mid, sx, sy, 38, LT)
    # the player before it, from behind, his own staff answering
    pr.player_back(mid, 480, 592, 66)
    near_heath(fore, 211, keep_out=lambda x, y: abs(x - 480) < 40 + (y - 600) * 0.2 and y < 700)
    pr.standing_stone(fore, 206, 660, 64, 196, 213, lean=0.05, ramp=[S[3], S[4], S[4], OCC, OCC], toward=(1, -0.5, G[4]))
    pr.standing_stone(fore, 770, 650, 52, 150, 214, lean=-0.05, ramp=[S[3], S[4], S[4], OCC, OCC], toward=(-1, -0.4, G[4]))
    layers = dict(back=back, mid=mid, fore=fore)
    pr.finish(layers, 0.85)
    return layers


def p_ascension_rise():
    """2. "A spell at level 7 may ascend: choose one of its paths." A standing stone with seven cup-marks
    up its face, each lit from the bottom up - the spell's seven levels - and at its crown the light
    leaving it and branching three ways, each branch ending in a form of its own. The middle one burns
    brightest: the one he chooses. One per spell, and the rest go dark."""
    back, mid, fore = pr.frame(True), pr.frame(), pr.frame()
    night(back, 430, 221)
    x, base, h = 480, 594, 178
    back.radial(x, base, 230, 54, [V[3], V[3], V[4], None])
    pr.standing_stone(mid, x, base, 66, h, 223, lean=0.0, ramp=[S[2], S[3], S[4], OCC, OCC], toward=(0, -1, G[3]))
    # the seven cup-marks, lit bottom to top: pigment gold below, the light itself at the top
    cups = [(x - 4 + (i % 2) * 8, base - 20 - i * 22) for i in range(7)]
    for i, (cx, cy) in enumerate(cups):
        mid.radial(cx, cy, 13, 10, [G[2], G[3], None])
        mid.disc(cx, cy, 5, 3.6, OCC)
        ramp = LT if i >= 5 else [G[0], G[1], G[2], G[3]]
        mid.radial(cx, cy, 4, 2.8, [ramp[0], ramp[1], ramp[2], ramp[3]])
    # at the crown the eighth: the light leaving the stone and branching
    top = (x, base - h - 14)
    glow(mid, top[0], top[1], 9, LT, halo=G)
    paths = [((362, 318), E["fire"], M["ember"], 0.6), ((480, 290), LT, G, 1.0), ((598, 318), E["ice"], M["azure"], 0.6)]
    for i, (end, ramp, halo, strength) in enumerate(paths):
        arc(mid, top, end, int(240 * strength), 231 + i, [ramp], bow=40 if end[0] != 480 else 8, spread=10)
        if strength < 1.0:
            mid.radial(end[0], end[1], 32, 32, [halo[3], halo[3], halo[4], None])
            sigil(mid, end[0], end[1], 18, ramp)
        else:
            mid.radial(end[0], end[1], 52, 52, [G[3], G[3], G[4], None])
            sigil(mid, end[0], end[1], 28, ramp)
    near_heath(fore, 241)
    for (bx, by, bw, bh, sd) in [(330, 690, 46, 18, 1), (640, 712, 60, 22, 2), (420, 780, 80, 28, 3)]:
        sk.boulder(fore, bx, by, bw, bh, seed=sd, ramp=[S[2], S[2], S[3], S[3], S[4]])
    layers = dict(back=back, mid=mid, fore=fore)
    pr.finish(layers, 0.85)
    return layers


# =========================================================================== the Apotheosis
def p_apotheosis_circle():
    """1. "Your wizard's own spell has a final form." The Broken Circle, where the wizards met, at night: the
    ring of stones, broken, and in the middle of it the player alone, his own spell standing up out of his
    staff as a pillar of his light - the one spell every wizard has had from the start."""
    back, mid, fore = pr.frame(True), pr.frame(), pr.frame()
    night(back, 400, 251, moon=(610, 270, 9))
    cx, cy = 480, 520
    back.radial(cx, cy, 260, 66, [G[3], G[3], G[4], G[4], None])
    items = []
    for i in range(13):
        a = i * 6.28318 / 13.0 + 0.2
        if math.sin(a) > 0.82:
            continue
        items.append((cy - 4 + math.sin(a) * 62, cx + math.cos(a) * 236, i))
    items.sort()
    rng = random.Random(253)
    for (y, x, i) in items:
        d = (y - (cy - 66)) / 128.0
        h = 30 + d * 44 + rng.random() * 14
        w = 10 + d * 11 + rng.random() * 5
        far = y < cy
        pr.standing_stone(mid, x, y, w, h, 260 + i, lean=(x - cx) / 2400.0, broken=rng.random() < 0.45,
                          warm=(G, 0.38 - d * 0.25) if far else None,
                          ramp=S if far else [S[3], S[4], S[4], OCC, OCC],
                          toward=None if far else (1 if x < cx else -1, -0.6, G[3]))
    # the pillar: his staff's light standing up into the sky
    px_, py_ = cx, 548
    u = 60 / 32.0
    staff = (px_ + 6.0 * u + 0.5, py_ - 28.6 * u)
    for y in range(int(staff[1]), 238, -1):
        t = (staff[1] - y) / float(staff[1] - 238)
        wd = 3.0 + t * 5.0
        for xx in range(int(staff[0] - wd * 2.2), int(staff[0] + wd * 2.2) + 1):
            e = 1.0 - abs(xx - staff[0]) / (wd * 2.2)
            col = LT[0] if e > 0.8 else (LT[1] if e > 0.55 else (LT[2] if e > 0.3 else LT[3]))
            if pr.dith(xx, y, min(1.0, e * 2.6) * (1.0 - t * 0.55)):
                back.set(xx, y, col)
    back.radial(staff[0], staff[1] - 20, 40, 30, [G[3], G[4], None])
    pr.player_back(mid, px_, py_, 60)
    near_heath(fore, 261, keep_out=lambda x, y: abs(x - cx) < 26 + (y - 590) * 0.16)
    pr.standing_stone(fore, 212, 652, 66, 200, 263, lean=0.04, ramp=[S[3], S[4], S[4], OCC, OCC], toward=(1, -0.5, G[4]))
    pr.standing_stone(fore, 768, 646, 54, 150, 264, lean=-0.05, broken=True, ramp=[S[3], S[4], S[4], OCC, OCC],
                      toward=(-1, -0.4, G[4]))
    layers = dict(back=back, mid=mid, fore=fore)
    pr.finish(layers, 0.9)
    return layers


def p_apotheosis_form():
    """2. "Ascend it and raise it to its last level, and it becomes that form." Close: the player from
    behind, large, the title's own figure, and over him his spell opened into its final form - a great
    ward of his light with rays going out of it, the biggest thing a wizard has."""
    back, mid, fore = pr.frame(True), pr.frame(), pr.frame()
    pr.sky(back, 0, 500, [OCC, S[4], V[4], V[3], V[2]])
    pr.stars(back, (0, 0, FW, 420), 260, 271)
    pr.hills(back, pr.ridge_fn(486, 10, 272, 1.2), [S[4], V[4], OCC], 40, lit=V[3])
    back.vramp(490, FH - 1, [V[4], S[4], OCC, OCC])
    pr.heath(back, 492, FH, 1600, 273, [(OCC, S[4]), (OCC, V[4]), (S[4], V[3])])
    fx, fy = 516, 340
    # the form: the sky round it takes gold (pigment), the rays and the ward are his light
    back.radial(fx, fy, 250, 210, [G[4], G[4], V[3], V[3], None])
    rng = random.Random(277)
    for i in range(48):
        a = i * 6.28318 / 48 + rng.random() * 0.05
        ln = 70 + rng.random() * 80 * (1.0 if i % 2 else 0.6)
        for k in range(int(ln)):
            if (k + i) % 3 == 0:
                continue
            d = 70 + k
            col = LT[2] if k < ln * 0.4 else LT[3]
            back.set(fx + math.cos(a) * d, fy + math.sin(a) * d * 0.92, col)
    mid.radial(fx, fy, 84, 84, [LT[3], LT[3], None])
    pr.ward(mid, fx, fy, 74, 74, A=LT, pool=False)
    pr.ward(mid, fx, fy, 44, 44, A=LT, pool=False)
    mid.radial(fx, fy, 26, 26, [LT[0], LT[0], LT[1], LT[2], LT[3]])
    # him, the title's figure from behind, his staff's light rising to it
    hero.wizard_hero(mid, 452, 640, h=134, staff_ramp=LT)
    arc(mid, (404, 520), (fx - 30, fy + 50), 220, 281, [LT], bow=-16, spread=12)
    near_heath(fore, 283, y0=680, keep_out=lambda x, y: abs(x - 450) < 80)
    layers = dict(back=back, mid=mid, fore=fore)
    pr.finish(layers, 0.8, cy=pr.CY - 30)
    return layers


# =========================================================================== Conjunctions
def p_conjunctions_two():
    """1. "Two ascended spells may become one." Two standing stones, each with a spell's own light on its
    crown - ice on one, fire on the other - and each light leaning out toward the other over the gap."""
    back, mid, fore = pr.frame(True), pr.frame(), pr.frame()
    night(back, 420, 301)
    back.radial(480, 600, 300, 60, [V[3], V[3], V[4], None])
    left = stone_light(mid, 376, 604, 54, 164, 303, E["ice"], M["azure"])
    right = stone_light(mid, 584, 604, 54, 164, 304, E["fire"], M["ember"])
    meet = (480, 318)
    arc(mid, left, (meet[0] - 22, meet[1] + 6), 220, 305, [E["ice"]], bow=36, spread=10)
    arc(mid, right, (meet[0] + 22, meet[1] + 6), 220, 306, [E["fire"]], bow=36, spread=10)
    near_heath(fore, 311)
    for (bx, by, bw, bh, sd) in [(300, 700, 50, 18, 4), (668, 690, 46, 18, 5), (480, 792, 90, 30, 6)]:
        sk.boulder(fore, bx, by, bw, bh, seed=sd, ramp=[S[2], S[2], S[3], S[3], S[4]])
    layers = dict(back=back, mid=mid, fore=fore)
    pr.finish(layers, 0.85)
    return layers


def p_conjunctions_one():
    """2. "Paths marked with linked rings have a partner." The two lights met over the gap and made one, and
    round it the mark the level-up cards carry for a path that has a partner: two rings, linked. The stones'
    crowns have gone dark; what they held is up there now."""
    back, mid, fore = pr.frame(True), pr.frame(), pr.frame()
    night(back, 420, 301)
    back.radial(480, 600, 300, 60, [V[3], V[3], V[4], None])
    left = stone_light(mid, 376, 604, 54, 164, 303, E["ice"], M["azure"], lit=False)
    right = stone_light(mid, 584, 604, 54, 164, 304, E["fire"], M["ember"], lit=False)
    cx, cy = 480, 330
    back.radial(cx, cy, 130, 110, [G[4], G[4], V[3], None])
    # the linked rings, gold, large, each rimmed inside with its own spell's light; where they cross, the
    # right one passes over the left at the top and the left over the right at the foot - linked, not laid
    # one on the other
    rings = ((cx - 30, E["ice"]), (cx + 30, E["fire"]))
    for (rx, ramp) in rings:
        ring_band(mid, rx, cy, 54, 0.0, 2 * math.pi, ramp)
    ring_band(mid, rings[1][0], cy, 54, math.radians(-124 - 16), math.radians(-124 + 16), rings[1][1], edge=True)
    ring_band(mid, rings[0][0], cy, 54, math.radians(56 - 16), math.radians(56 + 16), rings[0][1], edge=True)
    glow(mid, cx, cy, 16, LT, halo=G)
    mid.radial(cx - 6, cy, 14, 14, [E["ice"][2], E["ice"][3], None])
    mid.radial(cx + 6, cy, 14, 14, [E["fire"][2], E["fire"][3], None])
    mid.radial(cx, cy, 8, 8, [LT[0], LT[0], LT[1], LT[2]])
    arc(mid, left, (cx - 40, cy + 50), 60, 321, [E["ice"]], bow=20, spread=6)
    arc(mid, right, (cx + 40, cy + 50), 60, 322, [E["fire"]], bow=20, spread=6)
    near_heath(fore, 311)
    for (bx, by, bw, bh, sd) in [(300, 700, 50, 18, 4), (668, 690, 46, 18, 5), (480, 792, 90, 30, 6)]:
        sk.boulder(fore, bx, by, bw, bh, seed=sd, ramp=[S[2], S[2], S[3], S[3], S[4]])
    layers = dict(back=back, mid=mid, fore=fore)
    pr.finish(layers, 0.85)
    return layers


def ring_band(c, x, y, r, a0, a1, ramp, edge=False):
    """A gold ring's band from angle a0 to a1: gold, its inner pixel the spell's light. edge puts occlusion
    either side of it, so a segment drawn over another ring reads as passing over it."""
    steps = int(abs(a1 - a0) * r * 2.2) + 1
    for k in range(steps + 1):
        t = a0 + (a1 - a0) * k / float(steps)
        ct, st = math.cos(t), math.sin(t)
        if edge:
            for d in (-3, 2.5):
                c.set(x + ct * (r - d), y + st * (r - d), OCC)
        for d, col in ((-2, G[2]), (-1, G[1]), (0, G[1]), (1, G[2]), (2, ramp[3])):
            c.set(x + ct * (r - d), y + st * (r - d), col)


# =========================================================================== the Sovereign Rite
RING = [("fire", "ember"), ("ice", "azure"), ("grass", "verdant"), ("lightning", "gold")]


def _rite_ground(seed):
    back, mid, fore = pr.frame(True), pr.frame(), pr.frame()
    night(back, 404, seed, moon=(330, 270, 9))
    back.radial(480, 520, 270, 70, [V[3], V[3], V[4], V[4], None])
    return back, mid, fore


def _ring_points():
    # Two each side of the anointed one and none behind it, where its own stone would hide them.
    return [(480 + math.cos(math.pi * (1 + a)) * 160, 552 - math.sin(math.pi * a) * 40) for a in (0.06, 0.27, 0.73, 0.94)]


def p_rite_crown():
    """1. "One spell may take the strength of all the rest." Six spells' lights on low stones, five in an arc
    and one in the middle on a tall one; the five pour their light into the sixth, and over it the crown the
    HUD puts on an anointed spell."""
    back, mid, fore = _rite_ground(401)
    cx, top_y = 480, 430
    back.radial(cx, top_y, 120, 90, [G[4], G[4], V[3], None])
    centre = stone_light(mid, cx, 590, 50, 150, 403, LT, G)
    for i, ((el, halo), (x, y)) in enumerate(zip(RING, _ring_points())):
        light = stone_light(mid, x, y + 36, 26, 42, 410 + i, E[el], M[halo])
        arc(mid, light, (centre[0] + (x - cx) * 0.06, centre[1] + 4), 150, 420 + i, [E[el]], bow=60, spread=8)
    glow(mid, centre[0], centre[1], 13, LT, halo=G)
    crown(mid, centre[0], centre[1] - 34, 40)
    near_heath(fore, 431)
    layers = dict(back=back, mid=mid, fore=fore)
    pr.finish(layers, 0.85)
    return layers


def p_rite_seal():
    """2. "The others fall silent for the run, and their levels feed it." The five are out, each sealed
    with red wax across its stone, as the HUD seals a silenced slot; the sixth burns crowned, bigger."""
    back, mid, fore = _rite_ground(401)
    cx = 480
    back.radial(cx, 420, 150, 120, [G[3], G[4], G[4], V[3], None])
    centre = stone_light(mid, cx, 590, 50, 150, 403, LT, G, lit=False)
    for i, ((el, halo), (x, y)) in enumerate(zip(RING, _ring_points())):
        stone_light(mid, x, y + 36, 26, 42, 410 + i, E[el], M[halo], lit=False)
        wax_seal(mid, x, y + 36 - 22, 7)
    glow(mid, centre[0], centre[1] - 6, 22, LT, halo=G)
    pr.ward(mid, centre[0], centre[1] - 6, 40, 40, A=LT, pool=False)
    crown(mid, centre[0], centre[1] - 52, 56)
    near_heath(fore, 431)
    layers = dict(back=back, mid=mid, fore=fore)
    pr.finish(layers, 0.85)
    return layers


PANELS = [
    ("ascension-gift", p_ascension_gift), ("ascension-rise", p_ascension_rise),
    ("apotheosis-circle", p_apotheosis_circle), ("apotheosis-form", p_apotheosis_form),
    ("conjunctions-two", p_conjunctions_two), ("conjunctions-one", p_conjunctions_one),
    ("rite-crown", p_rite_crown), ("rite-seal", p_rite_seal),
]


def preview(name, layers):
    flat = pr.composite(layers)
    wide = flat.crop(pr.VIEW_WIDE)
    tall = flat.crop(pr.VIEW_TALL)
    sheet = Image.new("RGBA", (wide.size[0] + tall.size[0] + 12, tall.size[1]), (40, 40, 40, 255))
    sheet.paste(wide, (0, 0))
    sheet.paste(tall, (wide.size[0] + 12, 0))
    if not os.path.isdir(PREVIEW):
        os.makedirs(PREVIEW)
    sheet.save(os.path.join(PREVIEW, name + ".png"))


def main(argv):
    want = [a for a in argv if not a.startswith("--")]
    do_preview = "--preview" in argv
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    total_bad = 0
    for name, fn in PANELS:
        if want and name not in want:
            continue
        layers = fn()
        bad = pr.audit(layers)
        total_bad += sum(bad.values())
        for ln, c in layers.items():
            c.img.save(os.path.join(OUT, "%s-%s.png" % (name, ln)))
        if do_preview:
            preview(name, layers)
        print("%-18s %d off-contract pixels%s" % (name, sum(bad.values()),
                                                   (" " + str(sorted(bad.items())[:4])) if bad else ""))
    return 1 if total_bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
