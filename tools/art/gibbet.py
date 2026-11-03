# -*- coding: utf-8 -*-
"""The Blighted Swamp's second side event: the gibbet's cage, where Paracelsus is kept (.ai/chapter-wizards.md 4.4).

Run: python tools/art/gibbet.py

    world/gibbet/gibbet.png        the gibbet, 96x120 a frame, four frames: the winch's crank a quarter-turn on each
                                   (GibbetCageEvent turns them while the winch is held and while the cage comes down)
    world/gibbet/gibbet-cage.png   the cage, 32x54 a frame, four frames (GibbetCageEvent.Cage*):
                                     0  him in it, standing, gripping the bars
                                     1  him in it, coughing - bent over his fist
                                     2  empty, the door shut
                                     3  empty, the door swung open
    world/gibbet/gibbet-pool.png   the rot pool under it, 84x40 a frame, three frames of its bubbles (a ground decal)
    world/gibbet/landing.png       the ferry landing: black water, its reeds, and the jetty out over it, 120x84 (a decal)
    world/gibbet/landing-punt.png  the Ferryman's punt, 54x56 a frame: 0 empty, its pole laid in it; 1 him standing in it
    ui/poi/poi-gibbet.png          the arrow's pictogram: a gibbet with its cage
    tools/art/_gibbet_preview.png  (gitignored) for looking at

"Mother Rot heals herself; he is the one who could cure the rot. So the cure hangs in a cage over her mire, breathing
it." A gibbet: a post on a cairn of bog stones, an arm out over a pool of her rot, a pulley at its end and a chain down
to an iron cage cut to a man's shape - the old gibbet cage, the one meant to hang a body in. A winch at the post's foot
lowers it: an iron-bound drum on a frame, its crank on the near side, the rope run up the post and out along the arm.

HIS MARK IS ON IT. The cage's lock carries the one cold point every taken thing carries (the arcane ramp's hot and mid):
he hung it there, and GibbetCageEvent draws the far tell from it, in the same violet. Nothing else on any of it gives
off light - the pool's bubbles are scum, not poison's glow (that green is the lanterns', the Swamp's other event).

HIM, IN THE CAGE AND IN THE PUNT. In the punt it is his own sprite, every pixel of roster.paracelsus() - the real one,
standing, the flask up. In the cage there is no room for the sword or the raised arm: a narrow iron cage holds a man
standing with his hands on the bars, and that is drawn from the same palette and the same head (the beret, the heavy
jaw, the fur collar, the wine-dark gown), so the man who steps out is the man who was in it.

MATERIALS: old silvered timber (steel's dark end) for the post, the arm and the winch frame, iron the steel row's
shade and deep with its lit edge; the cairn and the bank stone; rope gold's dark end; the rot and the black water
linen's and stone's deep ends with lichen's scum; the punt old black wood (stone) with a gold-shade pole. Render scale
x2 everywhere, as every world sprite.
"""
from __future__ import division, print_function

import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image

import bonelight as bl
import matte
import pixel as P
import raster
import roster

ROOT = os.path.dirname(os.path.dirname(HERE))
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "gibbet")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
PREVIEW = os.path.join(HERE, "_gibbet_preview.png")

# The cells and the points on them that GibbetCageEvent names - change one, change both.
GW, GH = 96, 120              # the gibbet
POST_X = 14                   # the post's middle column, which stands on the site
G_FOOT = GH - 2               # its foot row; the contact line under it
PULLEY = (78, 20)             # where the chain hangs from
CRANK = (37, 102)             # the winch's axle
CW, CH = 32, 54               # the cage
C_MID, C_FOOT = 16, 52
LOCK = (23, 33)               # his mark on the cage's lock
PW, PH = 84, 40               # the pool
LW, LH = 120, 84              # the landing
LANDING_FOOT = (58, 78)       # the jetty's foot on the bank
UW, UH = 54, 56               # the punt: short, so it lies in the water beside the jetty and not across it
U_MID, U_FOOT = 27, 46

PAL = dict(roster.PALETTE)
# Lichen, for the scum on the rot and the ripples on the black water: the contract's row, on bytes of this file's own
# (a world drawing never goes through the player ramps' hurt and death steps, so nothing else reads them).
LI0, LI1, LI2, LI3, LI4 = "\xa1", "\xa2", "\xa3", "\xa4", "\xa5"
PAL.update({LI0: bl.MATERIALS["lichen"][0], LI1: bl.MATERIALS["lichen"][1], LI2: bl.MATERIALS["lichen"][2],
            LI3: bl.MATERIALS["lichen"][3], LI4: bl.MATERIALS["lichen"][4]})
RD = roster.RD0, roster.RD1, roster.RD2, roster.RD3, roster.RD4


class Grid(roster.G):
    """roster.G at any size: the helpers (mass, fold, shaft) only ever set, get and span."""

    def __init__(self, w, h):
        self.cell = max(w, h)
        self.w, self.h = w, h
        self.g = [["."] * w for _ in range(h)]

    def set(self, x, y, ch):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h and ch != ".":
            self.g[y][x] = ch

    def get(self, x, y):
        x, y = int(round(x)), int(round(y))
        return self.g[y][x] if 0 <= x < self.w and 0 <= y < self.h else "."

    def rows(self):
        return ["".join(r) for r in self.g]


# --- the gibbet ---------------------------------------------------------------------------------------------

TIMBER = ("r", "R", "q")       # steel's dark end: lit, body, shadow - old wood gone silver in the wet


def timber_v(g, x0, x1, y0, y1, seed):
    """An upright of old timber: lit on its key edge, grain running down it, split and checked at no regular interval."""
    lit, body, dark = TIMBER
    for y in range(y0, y1 + 1):
        g.span(y, x0, x1, body)
        g.set(x0, y, lit)
        g.set(x1, y, dark)
        g.set(x1 + 1, y, "o")
    rnd = random.Random(seed)
    for k in range(int((y1 - y0) / 5) if x1 - x0 >= 2 else 0):
        gx = rnd.randint(x0 + 1, x1 - 1)
        gy = rnd.randint(y0 + 1, y1 - 3)
        n = rnd.randint(2, 5)
        for j in range(n):
            g.set(gx, gy + j, dark if (k + j) % 3 else "o")
        if rnd.random() < 0.35:
            g.set(gx - 1, gy, lit)


def timber_h(g, x0, x1, y0, y1, seed):
    """A beam of the same wood: lit top, body, underside, the occlusion under it, checks along its length."""
    lit, body, dark = TIMBER
    for y in range(y0, y1 + 1):
        g.span(y, x0, x1, body)
    g.span(y0, x0, x1, lit)
    g.span(y1, x0, x1, dark)
    g.span(y1 + 1, x0 + 1, x1, "o")
    rnd = random.Random(seed)
    for k in range(int((x1 - x0) / 6)):
        gx = rnd.randint(x0 + 2, x1 - 5)
        gy = rnd.randint(y0 + 1, y1 - 1)
        for j in range(rnd.randint(2, 6)):
            g.set(gx + j, gy, dark if (k + j) % 4 else "o")
    # The cut end, end-grain lit.
    g.set(x1, y0 + 1, lit)
    g.set(x1, y0 + 2, "R")
    g.set(x1 - 1, y0 + 2, "q")


def stones(g, spec):
    """A cairn of bog stones from hand-placed (cx, cy, rx, ry): each lit at its top left, dark under, a crack or two."""
    for (cx, cy, rx, ry) in spec:
        for y in range(cy - ry, cy + ry + 1):
            for x in range(cx - rx, cx + rx + 1):
                if ((x - cx) / (rx + 0.4)) ** 2 + ((y - cy) / (ry + 0.4)) ** 2 > 1.0:
                    continue
                u, v = (x - cx) / (rx + 0.4), (y - cy) / (ry + 0.4)
                ch = "p" if u + v < -0.7 else "N" if u + v < 0.5 else "j"
                if v > 0.72:
                    ch = "J"
                g.set(x, y, ch)
        g.set(cx - rx + 1, cy - ry + 1, "P")
        g.set(cx + 1, cy, "J")
        g.set(cx + 2, cy + 1 if ry > 1 else cy, "j")


def rope(g, pts):
    """Rope along a polyline: gold's dark end, twisted - the two stops alternating every pixel."""
    k = 0
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = int(max(abs(x1 - x0), abs(y1 - y0)))
        for i in range(n + 1):
            t = i / float(max(1, n))
            g.set(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, "u" if k % 2 else "J")
            k += 1


def gibbet(crank):
    """The gibbet with its winch's crank at quarter-turn `crank` (0-3)."""
    g = Grid(GW, GH)
    # The cairn the post is footed in.
    stones(g, [(8, 113, 4, 3), (15, 115, 5, 2), (22, 113, 4, 3), (5, 108, 3, 2), (19, 108, 3, 3), (12, 109, 3, 2),
               (26, 115, 3, 2), (2 + 3, 116, 2, 1)])
    # The post, the arm and the brace.
    timber_v(g, POST_X - 3, POST_X + 3, 6, 109, seed=11)
    for band in (34, 78):
        g.span(band, POST_X - 3, POST_X + 3, "t")                  # iron bands round it, lit
        g.span(band + 1, POST_X - 3, POST_X + 3, "R")
        g.set(POST_X + 3, band, "R")
        g.set(POST_X - 1, band, "T")
    timber_h(g, POST_X - 6, 86, 6, 12, seed=17)
    for i in range(30):
        t = i / 29.0
        x = POST_X + 4 + t * 26
        y = 42 - t * 29
        g.set(x, y, "r")
        g.set(x + 1, y, "R")
        g.set(x + 2, y, "R")
        g.set(x + 3, y, "q")
        g.set(x + 1, y + 1, "o")
    g.set(POST_X + 4, 42, "o")
    # Iron spikes along the arm's top, against roosting crows.
    for sx in (30, 44, 57, 68):
        g.set(sx, 5, "R")
        g.set(sx, 4, "t")
    # The pulley block at the arm's end: an iron cheek and its wheel, the chain's hook under it.
    px, py = PULLEY
    for y in range(13, 19):
        g.span(y, px - 4, px + 4, "R")
        g.set(px - 4, y, "t")
        g.set(px + 4, y, "q")
    g.disc(px, 15.5, 2.2, "q")
    g.set(px - 1, 14, "t")
    g.set(px, 15, "J")
    g.span(19, px - 3, px + 3, "o")
    g.set(px, py, "R")
    g.set(px, py - 1, "q")
    # The winch: two uprights, the drum between them wound with rope, the crank on the near end.
    timber_v(g, 20, 21, 95, 116, seed=23)
    timber_v(g, 34, 35, 95, 116, seed=29)
    for y in range(99, 106):
        g.span(y, 22, 33, "u" if (y % 2) else "J")              # the rope wound round it
        g.set(22, y, "R")
        g.set(33, y, "q")
    g.span(98, 22, 33, "R")                                       # its iron hoops
    g.span(106, 22, 33, "q")
    g.span(107, 22, 33, "o")
    g.set(23, 98, "t")
    g.set(27, 98, "t")
    # The rope off the drum's top, up the post's near side and out along the arm's underside to the pulley.
    rope(g, [(24, 98), (19, 90), (18, 14), (24, 13), (px - 4, 13)])
    # The crank: an axle boss, an arm a quarter-turn on each frame, its grip.
    cx, cy = CRANK
    g.disc(cx, cy, 1.3, "R")
    g.set(cx, cy, "t")
    ang = math.pi / 2 * crank - math.pi / 4
    ex, ey = cx + 6 * math.cos(ang), cy + 6 * math.sin(ang)
    g.line(cx, cy, ex, ey, "R")
    g.line(cx, cy + 1, ex, ey + 1, "q")
    g.rect(ex - 1, ey - 2, ex, ey + 1, "J")                       # the grip, old wood
    g.set(ex - 1, ey - 2, "g")
    # The ground under it all.
    g.span(G_FOOT + 1, 1, 38, "o")
    return g.rows()


# --- the cage ------------------------------------------------------------------------------------------------

def caged_man(g, cough):
    """Him in the cage: the beret, the heavy jaw and the fur collar of roster.paracelsus, standing in the wine-dark
    gown with his hands on the bars - or bent over his fist, coughing."""
    mid, lit, dark = RD[2], RD[1], RD[3]
    drop = 2 if cough else 0
    cx = 16
    # The gown, standing, to the cage's floor.
    roster.mass(g, [(17 + drop, 10, 22), (30, 9, 23), (46, 9, 23), (49, 10, 22)], mid, lit, dark, rim=False)
    roster.fold(g, [(13, 20 + drop), (12, 48)], dark, lit)
    roster.fold(g, [(19, 21 + drop), (20, 48)], dark, lit)
    for y in range(18 + drop, 49):
        g.set(cx, y, "j")
        g.set(cx + 1, y, "J" if y % 3 else "N")
    g.span(30, 10, 22, "u")                                       # the belt
    g.set(cx, 30, "G")
    # The collar.
    roster.mass(g, [(15 + drop, 11, 21), (18 + drop, 10, 22)], "N", "p", "j", rim=False)
    for (tx, ty) in ((12, 19), (15, 19), (19, 19), (21, 18)):
        g.set(tx, ty + drop, "j")
    # The head, and the beret.
    roster.head(g, cx, 9 + drop, 2.6, brow="low", gaze=0 if cough else -1, mouth="open" if cough else "set",
                eye="shadow" if cough else "narrow")
    g.span(15 + drop, cx - 1, cx + 2, "i")
    roster.mass(g, [(5 + drop, 12, 20), (7 + drop, 10, 22)], "N", "p", "j", rim=False)
    g.span(8 + drop, 11, 21, "J")
    g.set(13, 6 + drop, "P")
    if cough:
        # Bent over his fist at his mouth, the other hand on a bar.
        roster.hand(g, cx - 1, 13 + drop)
        g.set(cx + 1, 14 + drop, "i")
        roster.hand(g, 22, 24)
    else:
        # Both hands up on the bars.
        roster.hand(g, 8, 22)
        roster.hand(g, 23, 22)


def cage(frame):
    g = Grid(CW, CH)
    if frame in (0, 1):
        caged_man(g, frame == 1)
    # The floor plate, and the bottom hoop.
    g.span(50, 7, 25, "R")
    g.span(51, 8, 24, "q")
    g.span(52, 9, 23, "o")
    g.set(7, 50, "t")
    # Its shape: a man's - a domed head-cage, shoulders, a body narrowing a little to the feet. (y, half-width.)
    outline = [(3, 3), (6, 6), (10, 7), (13, 7), (15, 11), (18, 11), (30, 10), (44, 9), (50, 9)]

    def half_at(y):
        for (ya, ha), (yb, hb) in zip(outline, outline[1:]):
            if ya <= y <= yb:
                return ha + (hb - ha) * (y - ya) / float(max(1, yb - ya))
        return 0
    door_open = frame == 3
    # The vertical bars, irregularly spaced across the shape.
    for off in (-1.0, -0.74, -0.42, -0.14, 0.14, 0.42, 0.72, 1.0):
        on_door = -0.2 < off < 0.5
        if door_open and on_door:
            continue
        prev = None
        for y in range(4, 50):
            h = half_at(y)
            x = int(round(C_MID + off * h))
            g.set(x, y, "R")
            if off < 0:
                g.set(x - 1 if abs(off) > 0.9 else x, y, "t" if abs(off) > 0.9 else "R")
            if prev is not None and abs(prev - x) > 1:
                for xx in range(min(prev, x) + 1, max(prev, x)):
                    g.set(xx, y, "R")
            prev = x
            if off > 0.9:
                g.set(x + 1, y, "q")
    # The bands: the head hoop, the shoulders, two round the body, all rimmed with occlusion under.
    for (by, ch) in ((6, "r"), (15, "t"), (27, "r"), (39, "r"), (49, "r")):
        h = half_at(by)
        g.span(by, int(round(C_MID - h)), int(round(C_MID + h)), ch)
        g.span(by + 1, int(round(C_MID - h)) + 1, int(round(C_MID + h)), "q")
        g.set(int(round(C_MID - h)), by, "T")
    # The ring at the top for the chain.
    g.set(C_MID, 0, "R")
    g.set(C_MID - 1, 1, "t")
    g.set(C_MID + 1, 1, "q")
    g.set(C_MID, 2, "R")
    g.span(3, C_MID - 2, C_MID + 2, "R")
    g.set(C_MID - 2, 3, "t")
    # The door: its hinge bars down the near side of it, the lock on its far edge - his mark on it.
    if door_open:
        # Swung out to the far side, seen edge on: a narrow panel of bars standing off the cage.
        for y in range(20, 47):
            g.set(27, y, "R")
            g.set(28, y, "q" if y % 4 else "t")
            g.set(29, y, "o")
        for by in (20, 33, 46):
            g.span(by, 25, 28, "r")
        lx, ly = 28, LOCK[1]
    else:
        for y in range(20, 47):
            if y % 9 == 2:
                g.set(13, y, "t")                                 # the hinges
                g.set(12, y, "R")
        lx, ly = LOCK
    g.rect(lx - 1, ly - 2, lx + 1, ly + 2, "R")
    g.set(lx - 1, ly - 2, "t")
    g.set(lx + 1, ly + 2, "q")
    g.set(lx, ly, "Y")                                            # his mark: the cold point
    g.set(lx, ly - 1, "y")
    g.set(lx, ly + 1, "z")
    return g.rows()


# --- the pool ------------------------------------------------------------------------------------------------

def pool(frame):
    """Her rot under the cage: a mud rim, murk, a skin of scum and slow bubbles - three frames of them."""
    g = Grid(PW, PH)
    cx, cy = PW / 2.0 - 0.5, PH / 2.0 - 0.5
    rnd = random.Random(41)
    wob = [rnd.uniform(-1.4, 1.4) for _ in range(16)]
    for y in range(PH):
        for x in range(PW):
            a = math.atan2((y - cy) * 2.0, x - cx)
            k = int((a + math.pi) / (2 * math.pi) * 16) % 16
            rx, ry = 39 + wob[k], 17.5 + wob[(k + 5) % 16] * 0.5
            d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if d > 1.0:
                continue
            if d > 0.78:
                ch = "J" if y > cy else "j"                       # the mud rim, its far side lit a little
                if y < cy - 8 and (x + y) % 3 == 0:
                    ch = "u"
            elif d > 0.62:
                ch = LI4
            else:
                ch = "A" if (x * 3 + y * 7) % 11 else LI4
            g.set(x, y, ch)
    # The skin of scum floating on it, in lichen's dark stops: irregular, never a stripe.
    for (sx, sy, n) in ((20, 14, 9), (36, 22, 12), (52, 17, 7), (28, 27, 6), (58, 26, 8), (14, 22, 5)):
        for i in range(n):
            g.set(sx + i, sy + (1 if (i * 5) % 7 == 0 else 0), LI3 if i % 3 else LI2)
    # The bubbles: three slow ones, rising and breaking at different moments.
    for (bx, by, ph) in ((26, 20, 0), (47, 13, 1), (60, 22, 2), (38, 28, 1)):
        s = (frame + ph) % 3
        if s == 0:
            g.set(bx, by, LI2)
        elif s == 1:
            g.set(bx, by, LI1)
            g.set(bx + 1, by, LI2)
            g.set(bx, by - 1, LI2)
        else:
            g.set(bx - 1, by, LI3)
            g.set(bx + 2, by, LI3)
            g.set(bx, by + 1, LI3)
    # Reeds at its far edge, and one bone.
    for (rx, ry, h) in ((10, 9, 5), (12, 8, 7), (70, 8, 6), (73, 10, 4), (66, 6, 5)):
        for j in range(h):
            g.set(rx + (1 if j > h // 2 else 0), ry - j, "e" if j % 2 else "E")
    g.span(33, 44, 48, "s")
    g.set(44, 33, "S")
    g.set(48, 33, "n")
    return g.rows()


# --- the landing ---------------------------------------------------------------------------------------------

def landing():
    """The ferry landing: a reach of black water, reeds at its edge, and a jetty of rotten planks from the bank out to
    a mooring post at its end. The punt is moored beside the post (landing-punt.png, its own sprite)."""
    g = Grid(LW, LH)
    cx, cy = 60, 28
    rnd = random.Random(53)
    wob = [rnd.uniform(-3, 3) for _ in range(20)]
    for y in range(LH):
        for x in range(LW):
            a = math.atan2((y - cy) * 1.6, x - cx)
            k = int((a + math.pi) / (2 * math.pi) * 20) % 20
            rx, ry = 56 + wob[k], 30 + wob[(k + 7) % 20] * 0.6
            d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if d > 1.0:
                continue
            ch = "J" if d < 0.82 else "j"
            if d > 0.9:
                ch = "u" if y > cy and (x + y) % 2 else "J"        # the muddy lip of the bank
            g.set(x, y, ch)
    # Ripples on it, in lichen's and stone's stops, longer the further off.
    for (rx, ry, n) in ((18, 18, 6), (30, 10, 9), (78, 14, 7), (90, 26, 8), (24, 34, 5), (84, 38, 6), (42, 44, 4),
                        (72, 46, 5), (14, 26, 4), (98, 18, 5)):
        for i in range(n):
            g.set(rx + i, ry, LI3 if i in (1, n - 2) else "N")
        g.set(rx + n // 2, ry, LI2)
    # Reeds along the water's edge, in tufts, linen's stops.
    for (tx, ty, h) in ((8, 40, 6), (11, 42, 8), (14, 41, 5), (100, 44, 7), (104, 42, 9), (107, 45, 5), (36, 56, 5),
                        (39, 57, 7), (82, 56, 6), (86, 55, 4), (4, 30, 5), (113, 30, 6)):
        for j in range(h):
            g.set(tx + (1 if j > h * 0.6 else 0), ty - j, "e" if j % 2 else "E")
        g.set(tx + 1, ty - h, "l")
    # The jetty: planks across two stringers from the bank out over the water, some gone, the water showing between.
    jx0, jx1 = 52, 64
    for y in range(12, LH - 4, 3):
        if y in (30, 51):
            continue                                              # rotten through
        sh = 1 if (y // 3) % 3 == 0 else 0
        # The same silvered wood as the gibbet: the lit top of each board, its body, the gap under it.
        g.span(y, jx0 + sh, jx1 - (1 - sh), "R")
        g.span(y + 1, jx0, jx1, "q")
        g.set(jx0, y, "r")
        g.set(jx0 + 1, y, "r")
        g.set(jx1, y + 1, "o")
        g.span(y + 2, jx0 + 1, jx1 - 1, "o")
        if (y * 7) % 5 == 0:
            g.set(jx0 + 4, y, "q")                               # a split board
            g.set(jx0 + 5, y + 1, "o")
        if (y * 11) % 7 == 3:
            g.set(jx1 - 3, y, LI3)                               # moss in the grain
    for y in range(12, LH - 4):
        if g.get(jx0 - 1, y) in ("J", "j", "N", "."):
            g.set(jx0 - 1, y, "o")
    # Its stakes, standing up out of the water at the sides, and the mooring post at its end with the rope on it.
    for (sx, sy) in ((jx0 - 2, 22), (jx1 + 1, 22), (jx0 - 2, 44), (jx1 + 1, 44), (jx0 - 2, 64), (jx1 + 1, 64)):
        g.rect(sx, sy - 4, sx + 1, sy, "N")
        g.set(sx, sy - 4, "p")
        g.set(sx + 1, sy, "J")
        g.set(sx, sy + 1, LI3)
    mx, my = jx1 + 2, 10
    g.rect(mx, my - 8, mx + 2, my + 2, "N")
    g.line(mx, my - 8, mx, my + 2, "p")
    g.set(mx + 2, my + 2, "J")
    rope(g, [(mx + 3, my - 4), (mx + 7, my - 1), (mx + 10, my + 1)])
    # The bank at the jetty's foot: trodden mud.
    for y in range(LH - 6, LH):
        g.span(y, jx0 - 6 + (LH - y) // 2, jx1 + 6 - (LH - y) // 2, "u" if (y % 2) else "J")
    return g.rows()


def punt(aboard):
    """The Ferryman's punt: long, flat and black, lying alongside the jetty's end. Empty, its pole laid along it; or
    with him standing in it - his own sprite, every pixel of roster.paracelsus() - the gunwale over his shins."""
    g = Grid(UW, UH)
    # The hull: the far side's inner planking, the floor, and the near gunwale - a long low trough, square at both
    # ends as a punt is.
    for y in range(40, 52):
        t = (y - 40) / 11.0
        x0, x1 = int(3 + 2 * t), int(50 - 2 * t)
        g.span(y, x0, x1, "N" if y < 44 else "j")
    g.span(40, 3, 50, "p")                                        # the far gunwale, lit
    g.span(41, 4, 49, "j")
    for x in range(7, 48, 7):
        g.set(x, 42, "J")                                         # the ribs
        g.set(x, 43, "J")
    if aboard:
        rows = roster.paracelsus()
        for y, row in enumerate(rows):
            for x, ch in enumerate(row):
                if ch != "." and y < 47:
                    g.set(x - 24 + U_MID, y - 46 + U_FOOT, ch)
    else:
        # The pole laid in it, end to end.
        g.line(6, 44, 48, 43, "u")
        g.line(6, 45, 48, 44, "J")
        g.set(6, 44, "g")
    # The near gunwale and the near side, over everything in it.
    g.span(46, 4, 49, "p")
    g.set(4, 46, "P")
    for y in range(47, 52):
        g.span(y, 5 + (y - 47) // 2, 48 - (y - 47) // 2, "j" if y < 50 else "J")
    g.span(52, 8, 45, "o")
    g.set(49, 46, "N")
    g.set(3, 47, "o")
    # The water lapping along it.
    for x in range(6, 48, 5):
        g.set(x, 53, LI3)
    g.set(20, 53, LI2)
    return g.rows()


# --- the pictogram -------------------------------------------------------------------------------------------

GLYPH = [
    "............",
    ".rrrrrrrrr..",
    ".rr......k..",
    ".r.r.....k..",
    ".r..r...RRR.",
    ".r.....RRRRR",
    ".r.....R.R.R",
    ".r.....RRRRR",
    ".r.....R.R.R",
    ".r......RRR.",
    "rrr.........",
    "............",
]
GLYPH_PAL = {"r": bl.tone("steel", "base"), "R": bl.tone("steel", "lit"), "k": bl.tone("gold", "shade")}


def glyph():
    c = raster.Canvas(12, 12)
    for y, row in enumerate(GLYPH):
        for x, ch in enumerate(row):
            if ch != ".":
                c.set(x, y, GLYPH_PAL[ch])
    for y in range(12):
        for x in range(12):
            if c.get(x, y)[3] == 0 and any(GLYPH[yy][xx] != "." for yy in range(max(0, y - 1), min(12, y + 2))
                                           for xx in range(max(0, x - 1), min(12, x + 2))):
                c.set(x, y, bl.OCC)
    return c


# --- output --------------------------------------------------------------------------------------------------

def render(rows, w, h, finish=True):
    img = P.render(P.pad(rows, w, h), PAL)
    return matte.finish(img) if finish else img


def strip(frames, w, h, finish=True):
    sheet = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, rows in enumerate(frames):
        sheet.alpha_composite(render(rows, w, h, finish), (i * w, 0))
    return sheet


def _audit_img(name, img):
    ok = raster.allowed()
    bad = {}
    px = img.load()
    for y in range(img.size[1]):
        for x in range(img.size[0]):
            p = px[x, y]
            if p[3] == 0:
                continue
            hx = "#%02X%02X%02X" % (p[0], p[1], p[2])
            if hx not in ok:
                bad[hx] = bad.get(hx, 0) + 1
    if bad:
        print("OFF-CONTRACT COLOURS in %s: %s" % (name, sorted(bad.items(), key=lambda kv: -kv[1])[:6]))
        return False
    return True


def main():
    for d in (OUT_WORLD, OUT_UI):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True
    sheets = [
        ("gibbet", strip([gibbet(k) for k in range(4)], GW, GH)),
        ("gibbet-cage", strip([cage(k) for k in range(4)], CW, CH)),
        # The decals lie on the ground under everything: no rim light (matte.finish puts the actors' rim on).
        ("gibbet-pool", strip([pool(k) for k in range(3)], PW, PH, finish=False)),
        ("landing", strip([landing()], LW, LH, finish=False)),
        ("landing-punt", strip([punt(False), punt(True)], UW, UH)),
    ]
    for name, sheet in sheets:
        ok = _audit_img(name, sheet) and ok
        sheet.save(os.path.join(OUT_WORLD, name + ".png"))
    g = glyph()
    ok = _audit_img("glyph", g.img) and ok
    g.scaled(2).save(os.path.join(OUT_UI, "poi-gibbet.png"))

    # The preview: everything at x3 on the Swamp's bog ground.
    z = 3
    canvas = Image.new("RGBA", (1280, 900), (26, 34, 32, 255))
    x = 10
    for name, sheet in sheets:
        im = sheet.resize((sheet.size[0] * z, sheet.size[1] * z), Image.NEAREST)
        if x + im.size[0] > 1280:
            x = 10
        y = 10 if name in ("gibbet", "gibbet-cage") else 400 if name != "landing-punt" else 640
        if name == "landing":
            x = 10
        canvas.alpha_composite(im, (x, y))
        x += im.size[0] + 20
    canvas.alpha_composite(g.scaled(6), (1180, 820))
    canvas.convert("RGB").save(PREVIEW)
    print("  gibbet 4, cage 4, pool 3, landing 1, punt 2, glyph poi-gibbet")
    if not ok:
        return 1
    print("gibbet written, palette clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
