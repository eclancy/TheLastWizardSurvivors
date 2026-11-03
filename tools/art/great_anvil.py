# -*- coding: utf-8 -*-
"""The Emberdeep's side event art: the great anvil, where Wayland the Smith is chained (.ai/chapter-wizards.md 4.7).

Run: python tools/art/great_anvil.py

    world/anvil/anvil-floor.png     the forge floor, 200x200: heavy flags blackened with soot, cinders in the
                                    joints, and the hearth's cracks still lit in fire's edge
    world/anvil/anvil-anvil.png     the great anvil on its iron-banded block, 56x36, one frame - its four
                                    chain rings on the block's corners
    world/anvil/anvil-smith.png     Wayland behind it, 48x48, four frames of a tired hammer: raised (only to the
                                    shoulder - he is tired), coming down, struck, resting on the face
    world/anvil/anvil-stake.png     a stake of his own iron driven into the floor, 16x28, two frames: holding its
                                    chain by a ring, and the ring burst open
    world/anvil/anvil-link.png      one chain link, 8x8, two frames: face on, edge on - GreatAnvilEvent lays
                                    them along each chain, alternating, as a chain lies
    ui/poi/poi-anvil.png            the arrow's pictogram: an anvil
    tools/art/_great_anvil_preview.png   (gitignored) for looking at

"Every shackle came from here." The Emberdeep is the forge at the end of the road, and the man who struck
every chain that holds the others is chained to his own anvil with four of them, staked out into the floor
at the corners. No spell breaks his own iron: only the forge's hammers can. GreatAnvilEvent wakes four
Trip-Hammers and the player brings their slams down on the stakes.

BUILT THINGS BURN. The forge is the one place whose things were made rather than taken, so its light is
fire (world-and-tone.md, the carve-out): the hearth's cracks in the floor and the sparks off his hammer are
fire's ramp, and the far tell is fire's hot stop. He himself wears nothing that glows - the light on him is
the forge's - and he is drawn a ramp step into shadow: he has been here a long time.

THE STAKES ARE NOTHING LIKE THE PALISADE. Iron Palisade's stakes are a fence of thin points in a row; these
are four squat iron pins with a ring through the head, alone at the four corners, each with one chain.

Every sheet is authored 1:1 and drawn by the game at x2 (Sprite2D Scale 2). The floor is a ground decal; the
anvil, the smith and the stakes sort with the bodies at their feet; the links lie on the floor.
"""
from __future__ import division, print_function

import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image

import anim_sets
import bonelight as bl
import pixel as P
import raster
import roster

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "anvil")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
PREVIEW = os.path.join(HERE, "_great_anvil_preview.png")


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


ST = ramp("stone")
SL = ramp("steel")
GD = ramp("gold")
FIRE = bl.ELEMENTS["fire"]       # core, hot, mid, edge: the hearth's light
METAL = bl.ELEMENTS["metal"]


# --- the forge floor ----------------------------------------------------------------------------

FLOOR = 200
FC = FLOOR // 2
FLOOR_R = 96


def floor():
    """Heavy square flags laid in a round, black with soot toward the middle where the anvil stands,
    cinders and slag in the joints, and here and there a joint still lit from the hearth under it."""
    rnd = random.Random(7071)
    c = raster.Canvas(FLOOR, FLOOR)
    size = 18
    y = FC - FLOOR_R - 6
    band = 0
    while y < FC + FLOOR_R + 6:
        h = size + rnd.choice((-2, 0, 1))
        x = FC - FLOOR_R - 6 - (band % 2) * 8
        while x < FC + FLOOR_R + 6:
            w = size + rnd.choice((-3, -1, 0, 2, 4))
            fx0, fy0, fx1, fy1 = x + 1, y + 1, x + w - 1, y + h - 1
            mx, my = (fx0 + fx1) / 2.0, (fy0 + fy1) / 2.0
            r = math.hypot(mx - FC, my - FC)
            keep = r < FLOOR_R - 14 or (r < FLOOR_R + 2 and rnd.random() < 0.55)
            if keep:
                # Sooted toward the middle: the flags under the anvil are a stop darker than the rim's.
                soot = r < 46
                top = ST[2] if soot else ST[1]
                for yy in range(fy0, fy1 + 1):
                    for xx in range(fx0, fx1 + 1):
                        if math.hypot(xx - FC, yy - FC) > FLOOR_R + 3:
                            continue
                        if yy == fy0 or xx == fx0:
                            col = ST[1] if soot else ST[0]
                        elif yy == fy1 or xx == fx1:
                            col = ST[3]
                        else:
                            col = top
                        c.set(xx, yy, col)
                # Pitting and slag spatter: a few dark pixels and one of steel per flag.
                for _ in range(rnd.randint(1, 4)):
                    px, py = rnd.randint(fx0 + 1, max(fx0 + 1, fx1 - 1)), rnd.randint(fy0 + 1, max(fy0 + 1, fy1 - 1))
                    if c.get(px, py)[3]:
                        c.set(px, py, ST[3])
                if rnd.random() < 0.35:
                    px, py = rnd.randint(fx0 + 2, max(fx0 + 2, fx1 - 2)), rnd.randint(fy0 + 2, max(fy0 + 2, fy1 - 2))
                    if c.get(px, py)[3]:
                        c.set(px, py, SL[3])
                        c.set(px + 1, py, SL[4])
                # The joints round it: cinder-black.
                for xx in range(fx0 - 1, fx1 + 2):
                    for yy in (fy0 - 1, fy1 + 1):
                        if c.get(xx, yy)[3] == 0 and math.hypot(xx - FC, yy - FC) <= FLOOR_R + 3:
                            c.set(xx, yy, ST[4])
                for yy in range(fy0, fy1 + 1):
                    for xx in (fx0 - 1, fx1 + 1):
                        if c.get(xx, yy)[3] == 0 and math.hypot(xx - FC, yy - FC) <= FLOOR_R + 3:
                            c.set(xx, yy, ST[4])
            x += w
        y += h
        band += 1
    # The hearth under the floor: three joints near the middle still lit, a line of fire's edge with one hot
    # pixel - the forge is never cold.
    for (x0, y0, x1, y1) in ((FC - 30, FC + 12, FC - 12, FC + 13), (FC + 14, FC - 18, FC + 15, FC - 4),
                             (FC + 8, FC + 22, FC + 26, FC + 23)):
        for k in range(max(abs(x1 - x0), abs(y1 - y0)) + 1):
            t = k / float(max(1, max(abs(x1 - x0), abs(y1 - y0))))
            px, py = int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t))
            if c.get(px, py)[3]:
                c.set(px, py, FIRE[3] if k % 5 else FIRE[2])
    # A ring of four iron eyes let into the floor where the stakes go in, so the stakes stand ON something.
    for q in range(4):
        a = math.radians(45 + 90 * q)
        ex, ey = FC + 70 * math.cos(a), FC + 70 * math.sin(a)
        c.ring(ex, ey, 6, 4, SL[3], thick=1)
        c.set(int(ex) - 5, int(ey) - 2, SL[2])
    return c


# --- the anvil ----------------------------------------------------------------------------------

ANVIL_W, ANVIL_H = 56, 36
ANVIL_FOOT = 33          # the row its block stands on; the contact shadow is the row under it


def anvil():
    """The great anvil: a long horned face on a waisted body, on a squat block banded with iron and with a
    ring at each corner, where the four chains are fastened. Steel's dark end, the face's working edge lit
    by the hearth - a line of fire's edge along its far lip."""
    c = raster.Canvas(ANVIL_W, ANVIL_H)
    # The block: a squat stump of iron-banded stone.
    for y in range(22, ANVIL_FOOT + 1):
        for x in range(10, 46):
            c.set(x, y, ST[2] if x > 12 else ST[1])
        c.set(45, y, ST[3])
    c.hline(10, 45, 22, ST[1])
    c.hline(10, 45, ANVIL_FOOT, ST[3])
    for y in (25, 31):
        c.hline(10, 45, y, SL[3])
        c.hline(10, 45, y - 1, SL[2])
        c.set(10, y - 1, SL[1])
    # The rings at its corners, where the chains are fastened.
    for (rx, ry) in ((9, 28), (46, 28)):
        c.ring(rx, ry, 2, 2, SL[2], thick=1)
        c.set(rx - 1, ry - 2, SL[1])
    # The body: waisted, in steel's shade, its feet spread on the block.
    for y in range(13, 22):
        t = (y - 13) / 8.0
        hw = 9 - 4 * math.sin(t * math.pi) + (2 if y >= 20 else 0)
        x0, x1 = int(round(28 - hw)), int(round(28 + hw))
        for x in range(x0, x1 + 1):
            c.set(x, y, SL[3])
        c.set(x0, y, SL[2])
        c.set(x1, y, SL[4])
    # The face: long and flat, the horn tapering off to the right, the heel square on the left.
    for y in range(8, 13):
        for x in range(12, 46):
            c.set(x, y, SL[3])
        c.set(12, y, SL[2])
    for k in range(9):
        y0 = 8 + k * 0.45
        for x in range(46 + k, 47 + k):
            for y in range(int(round(y0)), 12 - k // 3):
                c.set(x, y, SL[3])
    c.hline(12, 46, 8, SL[1])          # the working face, catching the key light
    c.hline(13, 44, 9, SL[2])
    c.set(20, 8, SL[0])
    c.set(30, 8, SL[0])
    c.hline(12, 45, 12, SL[4])         # its underside
    # The hardy hole and the pritchel hole in the heel.
    c.set(16, 9, OCC)
    c.set(19, 9, OCC)
    # The forge's light on the far lip.
    for x in range(36, 46):
        c.set(x, 9, FIRE[3] if x % 3 else FIRE[2])
    # Outline and contact shadow.
    for y in range(ANVIL_H):
        for x in range(ANVIL_W):
            if c.get(x, y)[3] != 0:
                continue
            if any(c.get(x + dx, y + dy)[3] != 0 and c.get(x + dx, y + dy) != raster.rgb(OCC)
                   for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                c.set(x, y, OCC)
    c.hline(9, 47, ANVIL_FOOT + 1, OCC)
    return c


# --- Wayland at the anvil -----------------------------------------------------------------------

def smith(frame):
    """Wayland behind his anvil, both hands on the hammer, the same man as roster.wayland(): the iron-grey
    shirt rolled at the sleeves, the leather apron, the short dark beard and the wings - but the wings hang
    folded and slack, and the hammer only comes up as far as his shoulder. He has been striking for a long
    time and is not allowed to stop.

    0 raised (to the shoulder), 1 coming down, 2 struck on the face, 3 resting there. He is drawn from the
    waist up: the anvil, drawn in front, hides the rest. A shackle on each wrist.
    """
    g = roster.G(48)
    mid, lit, dark = roster.ROBES["wayland"]
    CX = 20
    # The wings, folded and hanging: lower than his own, the tips at his shoulders.
    roster.mass(g, [(10, 9, 11), (14, 7, 13), (20, 7, 14), (28, 8, 14), (34, 10, 13)], "q", "R", "o")
    for (x0, y0, x1, y1) in ((12, 14, 10, 33), (10, 12, 8, 27)):
        g.line(x0, y0, x1, y1, "r")
        g.line(x0 + 1, y0 + 1, x1 + 1, y1, "o")
    g.set(10, 10, "t")
    # The body: the shirt, the apron's bib over it.
    roster.mass(g, [(19, 13, 27), (22, 11, 28), (30, 13, 27), (40, 13, 27)], mid, lit, dark)
    roster.mass(g, [(23, 15, 24), (30, 14, 25), (40, 14, 26)], "u", "g", "J", rim=False)
    roster.fold(g, [(17, 25), (16, 39)], "J", None)
    roster.fold(g, [(22, 27), (23, 39)], "J", None)
    g.line(15, 23, 17, 19, "u")
    g.line(24, 23, 22, 19, "u")
    g.span(31, 13, 27, "J")
    g.span(32, 14, 26, "u")
    # The head, bowed a little toward the work: brow low, eyes down on the anvil.
    roster.head(g, CX, 11, 3.2, brow="low", gaze=1, mouth="set", eye="shadow")
    g.span(10, 17, 23, "N")
    g.span(9, 18, 22, "J")
    g.set(17, 10, "p")
    roster.moustache(g, CX, 15, 2.4, "N", "p", "J")
    roster.hair(g, CX, 16, 19, 3.2, 1.6, "N", "p", "J")
    g.span(18, 17, 23, "e")
    # The hammer and the arms, by frame. Hands together on the haft; the head in iron.
    #   (hands, head centre, head is horizontal)
    poses = [
        ((27, 15), (31, 7), False),    # raised to the shoulder, head up
        ((29, 21), (36, 15), True),    # coming down
        ((30, 27), (37, 33), True),    # struck: the head on the anvil's face
        ((29, 28), (36, 34), True),    # resting on it
    ]
    (hx, hy), (kx, ky), level = poses[frame]
    # Arms from both shoulders to the hands: shirt to the elbow, bare forearms.
    for (sx, sy) in ((14, 21), (26, 21)):
        ex, ey = (sx + hx) / 2.0 + (2 if sx < 20 else 3), (sy + hy) / 2.0 + 3
        roster.shaft(g, sx, sy, ex, ey, lit, mid, dark, width=3)
        roster.shaft(g, ex, ey, hx, hy, "f", "h", "i", width=3)
    # The haft, from the hands to the head and a little past the hands.
    dxh, dyh = kx - hx, ky - hy
    n = math.hypot(dxh, dyh)
    ux, uy = dxh / n, dyh / n
    roster.shaft(g, hx - ux * 3, hy - uy * 3, kx - ux * 2, ky - uy * 2, "g", "u", "J", width=2)
    roster.hand(g, hx - 1, hy - 1)
    roster.hand(g, int(round(hx - ux * 3)), int(round(hy - uy * 3)))
    # The shackles: an iron band on each wrist.
    g.set(hx - 2, hy + 1, "r")
    g.set(hx - 2, hy + 2, "R")
    # The head.
    if level:
        g.rect(kx - 2, ky - 4, kx + 2, ky + 3, "R")
        g.span(ky - 4, kx - 2, kx + 2, "r")
        g.span(ky + 3, kx - 2, kx + 2, "q")
        g.set(kx - 2, ky - 3, "t")
    else:
        g.rect(kx - 4, ky - 2, kx + 3, ky + 2, "R")
        g.span(ky - 2, kx - 4, kx + 3, "r")
        g.span(ky + 2, kx - 4, kx + 3, "q")
        g.set(kx - 4, ky - 2, "t")
    rows = g.rows()
    # In shadow: a ramp step down. The light on him is the forge's, and the event draws it.
    return anim_sets.step(rows, 1, anim_sets._PLAYER_POS)


# --- the stake and the chain --------------------------------------------------------------------

STAKE_W, STAKE_H = 16, 28
STAKE_FOOT = 25


def stake(burst):
    """A squat pin of his own iron, driven into its eye in the floor: a thick square shank, a mushroomed
    head where it was struck down, and a ring through the head that the chain hangs from. Burst, the ring is
    sprung open and a broken link lies at its foot."""
    c = raster.Canvas(STAKE_W, STAKE_H)
    cx = 8
    # The shank, tapering into the floor.
    for y in range(10, STAKE_FOOT + 1):
        hw = 2 if y < 20 else 1
        for x in range(cx - hw, cx + hw + 1):
            c.set(x, y, SL[3])
        c.set(cx - hw, y, SL[2])
        c.set(cx + hw, y, SL[4])
    # The head, mushroomed by the hammer that drove it.
    for y in range(6, 10):
        for x in range(cx - 4, cx + 5):
            c.set(x, y, SL[3])
    c.hline(cx - 4, cx + 4, 6, SL[1])
    c.hline(cx - 3, cx + 3, 5, SL[2])
    c.set(cx - 4, 7, SL[2])
    c.hline(cx - 4, cx + 4, 9, SL[4])
    c.set(cx - 1, 5, SL[0])
    # Where it goes into the floor: its eye, a ring of iron let into the flag.
    c.hline(cx - 4, cx + 4, STAKE_FOOT, SL[3])
    c.set(cx - 4, STAKE_FOOT, SL[2])
    # The ring through the head.
    if not burst:
        c.ring(cx + 3, 13, 3, 3, SL[2], thick=1)
        c.set(cx + 1, 10, SL[1])
        c.set(cx + 6, 13, SL[3])
    else:
        # Sprung open: the ring a broken C, and a link lying at the foot of the stake, snapped.
        for (x, y) in ((cx + 1, 10), (cx + 2, 10), (cx + 3, 10), (cx + 5, 11), (cx + 6, 13), (cx + 5, 15)):
            c.set(x, y, SL[2])
        c.set(cx + 1, 10, SL[1])
        for (x, y, col) in ((cx + 3, 23, SL[2]), (cx + 4, 22, SL[2]), (cx + 5, 22, SL[3]), (cx + 6, 23, SL[3]),
                            (cx + 3, 24, SL[3])):
            c.set(x, y, col)
    for y in range(STAKE_H):
        for x in range(STAKE_W):
            if c.get(x, y)[3] != 0:
                continue
            if any(c.get(x + dx, y + dy)[3] != 0 and c.get(x + dx, y + dy) != raster.rgb(OCC)
                   for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                c.set(x, y, OCC)
    c.hline(cx - 4, cx + 5, STAKE_FOOT + 1, OCC)
    return c


LINK = 8


def link(edge_on):
    """One link of his chain, lying on the floor: face on a ring three pixels across, edge on a short bar.
    Laid alternately along a chain, which is how a chain looks lying down."""
    c = raster.Canvas(LINK, LINK)
    if not edge_on:
        c.ring(4, 4, 2.5, 1.8, SL[2], thick=1)
        c.set(2, 3, SL[1])
        c.set(3, 2, SL[1])
        c.set(6, 5, SL[3])
    else:
        c.hline(2, 5, 4, SL[2])
        c.set(2, 4, SL[1])
        c.set(5, 4, SL[3])
    for y in range(LINK):
        for x in range(LINK):
            if c.get(x, y)[3] != 0:
                continue
            if any(c.get(x + dx, y + dy)[3] != 0 and c.get(x + dx, y + dy) != raster.rgb(OCC)
                   for dx in (-1, 0, 1) for dy in (0, 1)):
                c.set(x, y, OCC)
    return c


# --- the pictogram ------------------------------------------------------------------------------

GLYPH = [
    "............",
    "............",
    "............",
    ".aaaaaaaabb.",
    "..aaaaaabbbb",
    "...aaaabbb..",
    "....aabb....",
    "....aabb....",
    "...aaabbb...",
    "..aaaabbbb..",
    "............",
    "............",
]
GLYPH_PAL = {"a": SL[1], "b": SL[2]}


def glyph():
    c = raster.Canvas(12, 12)
    for y, row in enumerate(GLYPH):
        for x, ch in enumerate(row):
            if ch != ".":
                c.set(x, y, GLYPH_PAL[ch])
    # The forge's light on the face: two pixels of fire's hot stop.
    c.set(4, 3, FIRE[1])
    c.set(5, 3, FIRE[2])
    for y in range(12):
        for x in range(12):
            if c.get(x, y)[3] == 0 and any(GLYPH[yy][xx] != "." for yy in range(max(0, y - 1), min(12, y + 2))
                                           for xx in range(max(0, x - 1), min(12, x + 2))):
                c.set(x, y, OCC)
    return c


# --- output -------------------------------------------------------------------------------------

def _audit(name, c):
    bad = c.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in %s:" % name)
        for h, cnt in bad[:6]:
            print("   %s  x%d" % (h, cnt))
        return False
    return True


def _strip(canvases):
    w, h = canvases[0].w, canvases[0].h
    img = Image.new("RGBA", (w * len(canvases), h), (0, 0, 0, 0))
    for i, c in enumerate(canvases):
        img.alpha_composite(c.img, (i * w, 0))
    return img


def main():
    for d in (OUT_WORLD, OUT_UI):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True

    fl = floor()
    av = anvil()
    stakes = [stake(False), stake(True)]
    links = [link(False), link(True)]
    for name, c in (("floor", fl), ("anvil", av), ("stake 0", stakes[0]), ("stake 1", stakes[1]),
                    ("link 0", links[0]), ("link 1", links[1])):
        ok = _audit(name, c) and ok
    fl.img.save(os.path.join(OUT_WORLD, "anvil-floor.png"))
    av.img.save(os.path.join(OUT_WORLD, "anvil-anvil.png"))
    _strip(stakes).save(os.path.join(OUT_WORLD, "anvil-stake.png"))
    _strip(links).save(os.path.join(OUT_WORLD, "anvil-link.png"))
    frames = [smith(f) for f in range(4)]
    P.write_strip(frames, roster.PALETTE, os.path.join(OUT_WORLD, "anvil-smith.png"), 48)

    g = glyph()
    ok = _audit("glyph anvil", g) and ok
    g.scaled(2).save(os.path.join(OUT_UI, "poi-anvil.png"))

    # The preview: the floor at x2 with the anvil and him at its middle, a stake at each corner and a chain
    # of links to each; then the four frames of him at x4.
    z = 2
    O = 60
    canvas = Image.new("RGBA", (FLOOR * z + 2 * O + 420, FLOOR * z + 2 * O), (40, 22, 18, 255))
    canvas.alpha_composite(fl.scaled(z), (O, O))
    cxw, cyw = O + FC * z, O + FC * z
    smith_img = Image.open(os.path.join(OUT_WORLD, "anvil-smith.png")).crop((0, 0, 48, 48)).resize((96, 96), Image.NEAREST)
    anvil_img = av.scaled(z)
    link_imgs = [l.scaled(z) for l in links]
    for q in range(4):
        a = math.radians(45 + 90 * q)
        sx, sy = cxw + 140 * math.cos(a), cyw + 140 * math.sin(a)
        ax, ay = cxw + (-38 if math.cos(a) < 0 else 38), cyw + 30
        n = int(math.hypot(sx - ax, sy - ay) / 10)
        for k in range(n):
            t = k / float(n)
            px, py = ax + (sx - ax) * t, ay + (sy - ay) * t
            canvas.alpha_composite(link_imgs[k % 2], (int(px) - 8, int(py) - 8))
        canvas.alpha_composite(stakes[q % 2].scaled(z), (int(sx) - 16, int(sy) - STAKE_FOOT * z))
    canvas.alpha_composite(smith_img, (cxw - 48 - 12, cyw + 20 - 96))
    canvas.alpha_composite(anvil_img, (cxw - ANVIL_W, cyw + 40 - ANVIL_FOOT * z))
    right = FLOOR * z + 2 * O
    strip = Image.open(os.path.join(OUT_WORLD, "anvil-smith.png"))
    canvas.alpha_composite(strip.resize((48 * 4 * 2, 48 * 2), Image.NEAREST), (right + 10, 20))
    canvas.alpha_composite(anvil_img, (right + 10, 140))
    canvas.alpha_composite(_strip(stakes).resize((STAKE_W * 2 * 3, STAKE_H * 3), Image.NEAREST), (right + 150, 140))
    canvas.alpha_composite(g.scaled(6), (right + 10, 240))
    canvas.convert("RGB").save(PREVIEW)

    print("  anvil floor 1, anvil 1, smith 4, stake 2, link 2, glyph poi-anvil")
    if not ok:
        return 1
    print("great anvil written, palette clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
