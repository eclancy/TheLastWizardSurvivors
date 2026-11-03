# -*- coding: utf-8 -*-
"""The Enchanted Forest's second side event: the walking hut, where Baba Yaga is kept (.ai/chapter-wizards.md 4.1).

Run: python tools/art/walking_hut.py

    world/hut/hut.png          the hut on hen's legs, 96x128 a frame, fourteen frames (WalkingHutEvent.Frame*):
                                 0-5   walking, its back to him (the windowless back wall, a shuttered gable)
                                 6-7   standing, breathing - the legs set, the body settling a pixel
                                 8     turning: the long wall side on, the hen's legs seen as legs
                                 9     its front to him, standing - the door in the gable
                                 10-11 kneeling: the legs folding under it, the body coming down
                                 12    knelt, the door ajar
                                 13    knelt, the door open, the dark inside
    world/hut/hut-prints.png   its three-toed print, 16x16 a cell: eight directions (0 east, clockwise every 45
                               degrees, the middle toe pointing that way) fresh, then the same eight old
    ui/poi/poi-hut.png         the arrow's pictogram: a hut on two legs
    tools/art/_walking_hut_preview.png   (gitignored) for looking at

"Every path in a wood once led to her door. He bent the wood so that every path leads inward, and set the
hut walking its circle for ever." In the tales the traveller says to it: turn your back to the forest and
your front to me. So the hut shows the player its BACK the whole time it walks - the windowless wall, the
shuttered gable - and only when it tires and kneels does it turn, and the door is on the side it turns to him.

WHY 96x128 AND NOT THE 96 CELL. The doc guessed the boss cell. But she has to come out of the door: she is 88
world px tall (the 48 cell at x2), and a door in a hut that fits 96 rows with its legs under it is 28 rows,
56 px - she would be half again the height of her own door. 128 rows gives the walls 44, the door 34 (she
stoops through it, as a tall old woman does through a low door) and the legs 48. Render scale is still x2, and
the extra height is a bigger cell, never a bigger scale (art-direction.md section 1).

HIS MARK IS ON IT. A horse's skull on the gable, its eye sockets lit with the one cold point every taken thing
carries (arcane's hot and mid, the swarm's eyes): the hut was enchanted by him, and it walks for him. That
is the only light on it, and WalkingHutEvent draws the far tell from it, in the same violet.

MATERIALS: old silvered timber (steel's dark end) for the logs, their end-grain at the corners lit; dry
thatch in gold's dark end with moss (linen's pale) on it; the legs bone-grey and scaled (the skin row, which
is bone in this palette), the drumsticks in dark mottled feathers (stone), the claws occlusion. Nothing
saturated: it is a thing he keeps.
"""
from __future__ import division, print_function

import math
import os
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
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "hut")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
PREVIEW = os.path.join(HERE, "_walking_hut_preview.png")

W, H = 96, 128
CX = 48
FEET = H - 2                 # the row the feet stand on (the contract's cell-2); the contact line under it
# Where the body sits when the hut stands: the sill's bottom row. The walls go up from it, the legs down.
SILL = 78
WALL = 42                    # the log walls' height
ROOF = 22                    # the thatch over them
KNEEL_DROP = 40              # how far the body comes down when it kneels: the sill then lies on its folded legs

PAL = dict(roster.PALETTE)


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


# --- the body ------------------------------------------------------------------------------------

# The logs: steel's dark end. Lit top, body, underside, and the gap between two logs in occlusion.
LOG_TOP, LOG, LOG_UNDER = "r", "R", "q"
MOSS = ("l", "e")             # linen's pale end: moss on the logs and the thatch
THATCH = ("g", "u", "J")       # gold's dark end: lit, body, shadow


def logs(g, x0, x1, y0, y1, seed, ends=True, shade=0):
    """Horizontal logs from y1 up to y0, irregular on purpose (evenly spaced courses read as a radiator,
    the warning in roster.py): each log four to six rows, bellied a pixel along its length, its ends standing
    proud of the corners at a different reach each, split and knotted at no regular interval, moss on some.
    `shade` steps the whole wall down the ramp (the side wall, out of the key light)."""
    top_c, body_c, under_c = [(LOG_TOP, LOG, LOG_UNDER), (LOG, LOG_UNDER, "o")][shade]
    heights = (5, 4, 6, 5, 4, 5, 6, 4, 5, 5)
    y = y1
    k = 0
    while y - 3 >= y0:
        hgt = heights[(seed + k * 3) % len(heights)]
        top = max(y0, y - hgt + 1)
        wob = ((seed * 7 + k * 5) % 5) - 2
        belly = (k + seed) % 2
        g.span(top, x0 + 1, x1 - 1, top_c)
        for yy in range(top + 1, y - 1):
            g.span(yy, x0, x1, body_c)
        g.span(y - 1, x0, x1, under_c)
        g.span(y, x0, x1, "o")
        if belly:
            # A log is never straight: its middle sags a row.
            g.span(top, (x0 + x1) // 2 - 6, (x0 + x1) // 2 + 8, body_c)
            g.span(top + 1, (x0 + x1) // 2 - 4, (x0 + x1) // 2 + 6, top_c)
        # Splits and knots along it, never evenly spaced.
        for j in range(3 + (k % 3)):
            sx = x0 + 3 + ((seed * 13 + k * 29 + j * 23) % max(1, x1 - x0 - 6))
            sy = top + 1 + ((j + k) % max(1, hgt - 3))
            g.set(sx, sy, under_c)
            g.set(sx + 1, sy, under_c if j % 2 else top_c)
            if j % 3 == 1:
                g.set(sx + 2, sy, under_c)
                g.set(sx + 3, sy, under_c)
        # Moss on the top of some of them, a ragged tuft.
        if (seed + k) % 3 == 0 and shade == 0:
            mx = x0 + 4 + ((seed * 5 + k * 19) % max(1, x1 - x0 - 14))
            n = 3 + (k % 4)
            g.span(top, mx, mx + n, MOSS[1])
            g.set(mx + 1, top, MOSS[0])
            g.set(mx + 2, top - 1, MOSS[1])
            g.set(mx + n, top + 1, MOSS[1])
        if ends:
            # The ends, standing proud of the corner each by its own reach: end-grain, lit on the key side.
            for side, ex in ((-1, x0 - 2 + min(0, wob)), (1, x1 + 1 + max(0, wob))):
                g.span(top, ex, ex + 2, top_c)
                for yy in range(top + 1, y - 1):
                    g.span(yy, ex, ex + 2, body_c)
                g.set(ex + 1, (top + y) // 2, under_c)
                g.span(y - 1, ex, ex + 2, under_c)
                g.set(ex + (0 if side < 0 else 2), top, "t" if side < 0 else top_c)
        y = top - 1
        k += 1


def side_wall(g, x0, x1, y0, y1, seed):
    """The side wall going back from the corner, out of the key light: the same logs a stop darker, so the
    hut has a depth and is not a card."""
    logs(g, x0, x1, y0, y1, seed + 1, ends=False, shade=1)
    for y in range(y0, y1 + 1):
        g.set(x0, y, "o")


def roof_gable(g, base_y, seed):
    """The thatch seen from its gable end: two heavy slopes of dry straw meeting at the ridge, overhanging the
    walls, moss on them, the straw's ends ragged along the eaves - and under them the gable, boarded."""
    apex = base_y - ROOF
    # The gable boards inside the triangle: vertical, irregular in width.
    for y in range(apex + 6, base_y + 1):
        t = (y - apex) / float(ROOF)
        half = int(round(4 + 22 * t))
        for x in range(CX - half, CX + half + 1):
            board = (x * 7 + seed) % 5
            g.set(x, y, LOG if board else LOG_UNDER)
        g.set(CX - half, y, LOG_TOP)
    # The two slopes, thick, from the ridge out past the walls.
    for side in (-1, 1):
        for i in range(0, ROOF + 7):
            y = apex + i
            xc = CX + side * int(round(i * 1.18))
            # A heavy slope, nine deep: lit along its top on the key side, the straw's grain in its body, its
            # underside dark where it overhangs the gable.
            for d in range(-4, 9):
                x = xc + side * d
                ch = THATCH[1]
                if d <= -3:
                    ch = THATCH[0] if side < 0 else THATCH[1]
                elif d >= 6:
                    ch = THATCH[2]
                if (x * 3 + y * 5 + seed) % 7 == 0 or (d == 2 and i % 4 == 1):
                    ch = THATCH[2]
                elif (x * 5 + y * 3 + seed) % 11 == 0 and d < 5:
                    ch = THATCH[0]
                g.set(x, y + (1 if d >= 6 else 0), ch)
            # The straw's ends: ragged under each slope.
            if i % 3 == 0:
                g.set(xc + side * 9, y + 2, THATCH[2])
                g.set(xc + side * 9, y + 3, "o")
            elif i % 3 == 1:
                g.set(xc + side * 8, y + 3, "o")
        # Moss over the thatch, in patches.
        for (i0, n) in ((6, 4), (15, 3), (24, 3)):
            for j in range(n):
                g.set(CX + side * int(round((i0 + j) * 1.18)) + side * 1, apex + i0 + j - 1, MOSS[(j + i0) % 2])
    # The ridge, bound, and the straw's ragged lower ends along both eaves.
    g.span(apex - 1, CX - 2, CX + 2, THATCH[0])
    g.span(apex, CX - 3, CX + 3, THATCH[1])
    for x in range(CX - ROOF - 10, CX + ROOF + 11, 2):
        if g.get(x, base_y + 6) == ".":
            continue
    return apex


def eave_shadow(g, x0, x1, top):
    """The thatch overhangs the walls: the top rows under it are in its shadow, dithered off."""
    darker = {"r": "R", "R": "q", "q": "o", "t": "r", "l": "e", "e": "E"}
    for y in (top, top + 1, top + 2):
        for x in range(x0, x1 + 1):
            ch = g.get(x, y)
            if ch in (".", "o"):
                continue
            if y < top + 2 or (x + y) % 2 == 0:
                g.set(x, y, darker.get(ch, ch))


def skull(g, x, y):
    """A horse skull on a pole at the gable peak, long-faced, nose down; his mark in its eye sockets - the
    swarm's own cold point (arcane hot and mid), the one light on the hut. (x, y) is its crown."""
    rows = [
        ".kks..",
        "kskknm",
        "knkknm",
        "kokkom",
        "nkkknm",
        ".knnm.",
        ".kknm.",
        ".knkm.",
        ".nkmm.",
        ".omom.",
        "..mm..",
    ]
    for dy, row in enumerate(rows):
        for dx, ch in enumerate(row):
            if ch != ".":
                g.set(x + dx - 2, y + dy, ch)
    # The eyes: his mark, the cold points every taken thing carries.
    g.set(x - 1, y + 3, "Y")
    g.set(x + 2, y + 3, "y")
    # The pole it is set on, down into the ridge.
    g.line(x, y + 11, x, y + 13, "u")
    g.set(x + 1, y + 12, "J")


def body_back(g, drop, seed=3):
    """The hut from behind: the log walls, the gable shuttered, his skull on the peak."""
    base = SILL + drop
    top = base - WALL
    side_wall(g, 74, 80, top + 2, base - 4, seed)
    logs(g, 25, 71, top, base - 4, seed)
    eave_shadow(g, 25, 80, top)
    g.span(base - 3, 22, 81, LOG_TOP)
    g.span(base - 2, 22, 81, LOG)
    g.span(base - 1, 22, 81, LOG_UNDER)
    g.span(base, 23, 81, "o")
    apex = roof_gable(g, top, seed)
    # A shuttered window in the gable: boards nailed across it.
    g.rect(CX - 4, top - 11, CX + 4, top - 4, "o")
    for yy in (top - 10, top - 7, top - 5):
        g.span(yy, CX - 4, CX + 4, LOG)
        g.set(CX - 4, yy, LOG_TOP)
        g.set(CX + 4, yy, LOG_UNDER)
    g.set(CX - 2, top - 10, "t")
    skull(g, CX - 1, apex - 12)
    return top


def body_front(g, drop, door, seed=5):
    """The hut from the front: the same walls with the door in them, under the gable, his skull over it.
    door: 0 shut, 1 ajar, 2 open on the dark inside."""
    base = SILL + drop
    top = base - WALL
    side_wall(g, 74, 80, top + 2, base - 4, seed)
    logs(g, 25, 71, top, base - 4, seed)
    eave_shadow(g, 25, 80, top)
    g.span(base - 3, 22, 81, LOG_TOP)
    g.span(base - 2, 22, 81, LOG)
    g.span(base - 1, 22, 81, LOG_UNDER)
    g.span(base, 23, 81, "o")
    apex = roof_gable(g, top, seed)
    # The door: heavy planks, a lintel over it, iron straps. 17 wide and 34 tall, the low door she stoops through.
    dx0, dx1 = CX - 8, CX + 8
    dy0 = base - 3 - 34
    g.rect(dx0 - 2, dy0 - 3, dx1 + 2, dy0 - 1, LOG_UNDER)        # the lintel
    g.span(dy0 - 3, dx0 - 2, dx1 + 2, LOG_TOP)
    g.span(dy0 - 1, dx0 - 2, dx1 + 2, "o")
    for yy in range(dy0, base - 3):
        g.set(dx0 - 1, yy, "o")
        g.set(dx1 + 1, yy, "o")
    if door == 0:
        for x in range(dx0, dx1 + 1):
            plank = (x - dx0) // 4
            for yy in range(dy0, base - 3):
                g.set(x, yy, "N" if (x - dx0) % 4 else "j")
            g.set(x, dy0, "p" if (x - dx0) % 4 else "j")
        for yy in (dy0 + 6, base - 10):                             # the straps, iron
            g.span(yy, dx0, dx1 - 2, "R")
            g.set(dx0, yy, "r")
            g.set(dx1 - 3, yy, "q")
        g.set(dx1 - 3, dy0 + 17, "t")                              # the latch
        g.set(dx1 - 3, dy0 + 18, "R")
    else:
        # Open: the dark inside, deep blue-black, its floor catching a little light at the threshold.
        g.rect(dx0, dy0, dx1, base - 4, "J")
        g.span(base - 4, dx0, dx1, "j")
        g.span(base - 5, dx0 + 2, dx1 - 1, "J")
        if door == 1:
            # Ajar: the door swung in, a narrow strip of it still across the right of the opening.
            for x in range(dx0 + 9, dx1 + 1):
                for yy in range(dy0, base - 3):
                    g.set(x, yy, "N" if (x - dx0) % 4 else "j")
            g.line(dx0 + 9, dy0, dx0 + 9, base - 4, "p")
        else:
            # Wide: the door back against the inside wall, an edge of it showing at the left.
            g.line(dx0, dy0, dx0, base - 4, "N")
            g.line(dx0 + 1, dy0 + 1, dx0 + 1, base - 5, "j")
    # A small round window in the gable, dark.
    g.disc(CX, top - 8, 2.4, "o")
    g.set(CX - 1, top - 10, LOG_UNDER)
    skull(g, CX - 1, apex - 12)
    return top


def body_side(g, drop, seed=7):
    """Turning: the long wall side on. The roof is one long slope of thatch over it, the ridge level, his skull
    at the near end of the ridge, looking the way it turns."""
    base = SILL + drop
    top = base - WALL
    logs(g, 14, 82, top, base - 4, seed)
    g.span(base - 3, 11, 85, LOG_TOP)
    g.span(base - 2, 11, 85, LOG)
    g.span(base - 1, 11, 85, LOG_UNDER)
    g.span(base, 12, 84, "o")
    # A window in the long wall, shuttered.
    g.rect(52, top + 12, 62, top + 22, "o")
    for yy in (top + 13, top + 16, top + 19):
        g.span(yy, 52, 62, LOG)
        g.set(52, yy, LOG_TOP)
    # The roof: the near slope, from the eave over the wall up to the ridge.
    ridge = top - ROOF + 6
    for y in range(ridge, top + 4):
        t = (y - ridge) / float(top + 4 - ridge)
        x0, x1 = int(round(14 - 6 * t)), int(round(82 + 6 * t))
        for x in range(x0, x1 + 1):
            ch = THATCH[1]
            if (x * 5 + y * 3 + seed) % 6 == 0:
                ch = THATCH[2]
            elif (x + y * 7) % 11 == 0:
                ch = THATCH[0]
            g.set(x, y, ch)
        g.set(x0, y, THATCH[0])
        g.set(x1, y, THATCH[2])
    g.span(ridge - 1, 14, 82, THATCH[0])
    g.span(ridge, 14, 82, THATCH[0])
    for x in range(8, 89, 3):
        g.set(x, top + 4, THATCH[2])
        g.set(x + 1, top + 5, "o")
    for (mx, my, n) in ((24, ridge + 4, 6), (58, ridge + 9, 5), (40, ridge + 15, 4), (70, ridge + 3, 4)):
        g.span(my, mx, mx + n, MOSS[1])
        g.set(mx + 1, my - 1, MOSS[0])
    skull(g, 12, ridge - 12)
    return top


# --- the legs ------------------------------------------------------------------------------------

HIPS = (38, 58)

SCALE = ("s", "k", "n")       # the shank: bone-grey, lit, body, shade
FEATHER = ("p", "N", "j")      # the drumstick: dark mottled feathers


def drumstick(g, hx, hy, out=0):
    """The thigh under the sill: a heavy feathered mass, ragged at its lower edge."""
    roster.mass(g, [(hy, hx - 6, hx + 6), (hy + 7, hx - 6 + out, hx + 6 + out), (hy + 13, hx - 3 + out, hx + 3 + out)],
                FEATHER[1], FEATHER[0], FEATHER[2], rim=False)
    for (dx, dy) in ((-4, 3), (2, 2), (-1, 6), (4, 7), (-3, 10), (1, 11)):
        g.set(hx + dx + (out if dy > 6 else 0), hy + dy, FEATHER[2])
        g.set(hx + dx + 1 + (out if dy > 6 else 0), hy + dy - 1, FEATHER[0])
    for dx in (-5, -2, 2, 5):
        g.set(hx + dx + out // 2, hy + 13 - abs(dx) // 2, "o")


def shank(g, x0, y0, x1, y1):
    """The scaled shank from the hock to the ankle, four wide, banded with scales at no even step, and the
    hock a knob at the top of it - the joint that bends the wrong way."""
    roster.shaft(g, x0 - 1, y0, x1 - 1, y1, SCALE[0], SCALE[1], SCALE[2], width=3)
    roster.shaft(g, x0 + 1, y0 + 1, x1 + 1, y1, SCALE[2], "m", "m", width=2)
    g.disc(x0, y0 + 1, 2.2, SCALE[1])
    g.set(x0 - 1, y0, SCALE[0])
    g.set(x0 + 1, y0 + 2, SCALE[2])
    n = int(max(abs(y1 - y0), abs(x1 - x0)))
    for i in range(4, n, 3 + (n % 2)):
        t = i / float(max(1, n))
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        g.set(x - 1, y, "m")
        g.set(x, y + 1, "n")


def foot(g, ax, ay, curled=False, flip=1):
    """Three toes forward and splayed, the hind toe back; the claws occlusion. Curled while it is lifted."""
    if curled:
        for (dx, dy) in ((-2, 1), (-3, 2), (0, 2), (0, 3), (2, 1), (3, 2)):
            g.set(ax + dx, ay + dy, SCALE[1])
        for (dx, dy) in ((-3, 3), (0, 4), (3, 3)):
            g.set(ax + dx, ay + dy, "o")
        g.set(ax - 1, ay + 1, SCALE[0])
        return
    toes = [(-9, 1), (-3, -2), (8 * flip, 0)]
    for (tx, ty) in toes:
        g.line(ax, ay, ax + tx, ay + ty, SCALE[1])
        g.line(ax, ay + 1, ax + tx, ay + ty + 1, SCALE[2])
        g.set(ax + tx + (1 if tx > 0 else -1), ay + ty + 1, "o")
        g.set(ax + tx // 2, ay + ty // 2, SCALE[0])
    g.set(ax + 2 * flip, ay + 2, SCALE[2])                         # the hind toe, toward us
    g.set(ax + 3 * flip, ay + 2, "o")
    g.set(ax, ay, SCALE[0])


def legs_standing(g, drop, lift=(0, 0), sway=0):
    """Both legs from the hips to the ground (back or front view): a lifted foot rises with its toes curled."""
    hy = SILL + drop + 1
    for i, hx in enumerate(HIPS):
        l = lift[i]
        ankle_y = FEET - 2 - int(round(12 * l))
        ax = hx + (-5 if i == 0 else 5) + sway
        hock_y = hy + 15
        drumstick(g, hx + sway // 2, hy)
        shank(g, hx + sway // 2 + (-2 if i == 0 else 2), hock_y, ax, ankle_y)
        foot(g, ax, ankle_y, curled=l > 0.35, flip=1 if i == 1 else -1)


def legs_folding(g, drop, fold):
    """Front view, kneeling: the hocks pushed out to the sides as the body comes down, the feet planted."""
    hy = SILL + drop + 1
    for i, hx in enumerate(HIPS):
        side = -1 if i == 0 else 1
        ax = hx + side * 5
        ay = FEET - 2
        hock_x = hx + side * int(round(6 + 10 * fold))
        hock_y = min(ay - 4, hy + 10)
        drumstick(g, hx, hy, out=side * int(round(5 * fold)))
        if ay - hock_y > 3:
            shank(g, hock_x, hock_y, ax, ay)
        foot(g, ax, ay, flip=side)


def legs_side(g, drop):
    """Side on: the legs as a hen's, the drumstick, the hock bent back, the shank forward to the foot - the far
    leg a stop darker behind the near."""
    hy = SILL + drop + 1
    for i, (hx, ax) in enumerate(((44, 52), (54, 62))):
        far = i == 0
        roster.mass(g, [(hy, hx - 6, hx + 7), (hy + 8, hx - 5, hx + 6), (hy + 14, hx - 1, hx + 3)],
                    "j" if far else FEATHER[1], "N" if far else FEATHER[0], "J" if far else FEATHER[2], rim=False)
        hock = (hx - 2, hy + 24)
        roster.shaft(g, hx + 1, hy + 14, hock[0], hock[1], "k" if far else SCALE[0], "n" if far else SCALE[1],
                     "m", width=3)
        roster.shaft(g, hock[0], hock[1], ax, FEET - 3, "k" if far else SCALE[0], "n" if far else SCALE[1], "m", width=3)
        for (tx, ty) in ((10, 1), (6, 2)):
            g.line(ax, FEET - 2, ax + tx, FEET - 2 + ty - 1, "n" if far else SCALE[1])
            g.set(ax + tx + 1, FEET - 2 + ty - 1, "o")
        g.line(ax, FEET - 2, ax - 4, FEET - 1, "n")
        g.set(ax - 5, FEET - 1, "o")


def contact(g, x0, x1):
    g.span(H - 1, x0, x1, "o")


# --- the frames ----------------------------------------------------------------------------------

def frame(kind, t=0.0):
    g = Grid(W, H)
    if kind == "walk":
        # A hen's heavy walk from behind: one foot up while the other bears, the body down on each fall and
        # leaning a pixel onto the leg it stands on.
        s = math.sin(2.0 * math.pi * t)
        lift = (max(0.0, s), max(0.0, -s))
        bob = -int(round(2 * abs(s)))
        sway = int(round(1.5 * math.cos(2.0 * math.pi * t)))
        legs_standing(g, bob, lift, sway)
        body_back(g, bob)
        contact(g, 26, 70)
    elif kind == "stand":
        legs_standing(g, int(round(t)))
        body_back(g, int(round(t)))
        contact(g, 26, 70)
    elif kind == "side":
        legs_side(g, 0)
        body_side(g, 0)
        contact(g, 30, 76)
    elif kind == "front":
        legs_standing(g, 0)
        body_front(g, 0, 0)
        contact(g, 26, 70)
    elif kind == "kneel":
        drop = int(round(KNEEL_DROP * t))
        legs_folding(g, drop, t)
        body_front(g, drop, 0)
        contact(g, 22, 74)
    elif kind == "door":
        legs_folding(g, KNEEL_DROP, 1.0)
        body_front(g, KNEEL_DROP, int(t))
        contact(g, 22, 74)
    return g.rows()


def frames():
    out = [frame("walk", i / 6.0) for i in range(6)]
    out += [frame("stand", 0), frame("stand", 1)]
    out += [frame("side")]
    out += [frame("front")]
    out += [frame("kneel", 0.5), frame("kneel", 1.0)]
    out += [frame("door", 1), frame("door", 2)]
    return out


def render(rows):
    return matte.finish(P.render(P.pad(rows, W, H), PAL))


# --- the prints ----------------------------------------------------------------------------------

PRINT = 16


def hen_print(direction, fresh):
    """A print of a great three-toed foot pressed into the moss: three toe grooves splayed from the heel the
    way it walked, the hind toe's short stab behind. Fresh, the grooves are deep (occlusion) with torn earth
    along them (gold's shade); old, they are shallow (stone) and the earth has gone."""
    c = raster.Canvas(PRINT, PRINT)
    a = math.radians(45 * direction)
    hx, hy = 7.5 - 2.4 * math.cos(a), 7.5 - 2.4 * math.sin(a)
    groove = bl.OCC if fresh else bl.tone("stone", "deep")
    lip = bl.tone("gold", "shade") if fresh else None
    rows = []
    for (da, length) in ((0.0, 7.2), (-0.72, 5.6), (0.72, 5.6), (math.pi, 2.4)):
        rows.append((a + da, length))
    for (ang, length) in rows:
        ex, ey = hx + math.cos(ang) * length, hy + math.sin(ang) * length
        for step in range(int(length * 3) + 1):
            t = step / (length * 3.0)
            px, py = hx + (ex - hx) * t, hy + (ey - hy) * t
            c.set(int(round(px)), int(round(py)), groove)
            if fresh and t < 0.8:
                c.set(int(round(px)) + (1 if math.sin(ang) < 0.3 else 0), int(round(py)) + 1, groove)
    if lip:
        # The earth turned up along the far side of each groove, and pushed up ahead of each toe.
        for (ang, length) in rows[:3]:
            ex, ey = hx + math.cos(ang) * (length + 1.2), hy + math.sin(ang) * (length + 1.2)
            if c.get(int(round(ex)), int(round(ey)))[3] == 0:
                c.set(int(round(ex)), int(round(ey)), lip)
        for y in range(PRINT):
            for x in range(PRINT):
                if c.get(x, y)[3]:
                    continue
                if c.get(x, y + 1) == raster.rgb(groove) and (x + y) % 2 == 0:
                    c.set(x, y, lip)
    return c


def prints_sheet():
    cells = [hen_print(d, True) for d in range(8)] + [hen_print(d, False) for d in range(8)]
    img = Image.new("RGBA", (PRINT * 8, PRINT * 2), (0, 0, 0, 0))
    for i, c in enumerate(cells):
        img.alpha_composite(c.img, ((i % 8) * PRINT, (i // 8) * PRINT))
    return img, cells


# --- the pictogram -------------------------------------------------------------------------------

GLYPH = [
    "............",
    ".....tt.....",
    "....tttt....",
    "...ttRRtt...",
    "..ttRRRRtt..",
    "...rRRRRr...",
    "...rRRRRr...",
    "...qqqqqq...",
    "....k..k....",
    "....k..k....",
    "...kk..kk...",
    "............",
]
GLYPH_PAL = {"t": bl.tone("gold", "base"), "R": bl.tone("steel", "shade"), "r": bl.tone("steel", "base"),
             "q": bl.tone("steel", "deep"), "k": bl.tone("skin", "base")}


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


# --- output --------------------------------------------------------------------------------------

def _audit_img(name, img):
    ok = raster.allowed()
    bad = {}
    px = img.load()
    for y in range(img.size[1]):
        for x in range(img.size[0]):
            p = px[x, y]
            if p[3] == 0:
                continue
            h = "#%02X%02X%02X" % (p[0], p[1], p[2])
            if h not in ok:
                bad[h] = bad.get(h, 0) + 1
    if bad:
        print("OFF-CONTRACT COLOURS in %s: %s" % (name, sorted(bad.items(), key=lambda kv: -kv[1])[:6]))
        return False
    return True


def main():
    for d in (OUT_WORLD, OUT_UI):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True
    fr = frames()
    sheet = Image.new("RGBA", (W * len(fr), H), (0, 0, 0, 0))
    for i, rows in enumerate(fr):
        sheet.alpha_composite(render(rows), (i * W, 0))
        lo = max(y for y, r in enumerate(rows) if r.strip("."))
        if lo != H - 1:
            print("frame %d: lowest row %d, expected the contact line on %d" % (i, lo, H - 1))
            ok = False
    ok = _audit_img("hut", sheet) and ok
    sheet.save(os.path.join(OUT_WORLD, "hut.png"))
    prints, cells = prints_sheet()
    ok = _audit_img("prints", prints) and ok
    prints.save(os.path.join(OUT_WORLD, "hut-prints.png"))
    g = glyph()
    ok = _audit_img("glyph", g.img) and ok
    g.scaled(2).save(os.path.join(OUT_UI, "poi-hut.png"))

    # The preview: the frames at x2 on the Forest's moss, the prints at x4, the glyph at x6.
    z = 2
    pw = W * z * 7 + 80
    canvas = Image.new("RGBA", (pw, H * z * 2 + 260), (34, 52, 46, 255))
    for i in range(len(fr)):
        im = render(fr[i]).resize((W * z, H * z), Image.NEAREST)
        canvas.alpha_composite(im, (10 + (i % 7) * (W * z + 10), 10 + (i // 7) * (H * z + 10)))
    canvas.alpha_composite(prints.resize((PRINT * 8 * 4, PRINT * 2 * 4), Image.NEAREST), (10, H * z * 2 + 40))
    canvas.alpha_composite(g.scaled(6), (PRINT * 8 * 4 + 40, H * z * 2 + 40))
    canvas.convert("RGB").save(PREVIEW)

    print("  hut %d frames (%dx%d), prints 16, glyph poi-hut" % (len(fr), W, H))
    if not ok:
        return 1
    print("walking hut written, palette clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
