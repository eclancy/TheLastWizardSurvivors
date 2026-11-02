# -*- coding: utf-8 -*-
"""The thaw's lake and the block of ice in the middle of it. Run: python tools/art/thaw_lake.py

See .ai/side-events.md, "The thaw". Eric, playing 0.10.0: "Wizard trapped in ice should be in the
center of a large, magically animated lake. Current art is corny looking." It was a blue rectangle
with a darker rectangle and a circle in it, drawn by _Draw, on bare ground.

Now the wizard stands frozen in a block of ice at the centre of a frozen lake, and the lake is alive:
cold light rises under the ice from where he is, in rings that run out across it the way his song
carries, and drifts in slow patches further out; frost ferns creep in from the shore and draw back
again. Everything moving is in the shader (scenes/shaders/thaw_lake.gdshader); this file draws what
it picks its colours from, so nothing off the contract can appear (the water's rule, .ai/terrain.md
section 3):

  world/thaw/thaw-lake.png         the lake as it lies, 352x288, authored 1:1, drawn at x2 (704x576)
  world/thaw/thaw-lake-lit.png     the same lake with the light under it one step up
  world/thaw/thaw-lake-bright.png  ... and two steps up, at the crest of a ring
  world/thaw/thaw-lake-data.png    R how much light the ice lets through (clear ice all, frost half,
                                   snow none), G when a frost fern's pixel grows in (0..250 of the
                                   cycle; 255 never), B a soft noise tiling every 32 px that the
                                   wandering light is read from; A opaque throughout
  world/thaw/thaw-block.png        the block, 64x64 cells at 1:1, drawn at x2: four stages of
                                   thawing (columns 0-3, 4-7, 8-11, 12-15), each with four frames of
                                   a glint sweeping across its face

THE LAKE IS WALKABLE. It is frozen: the fight is on it (the event asks for exactly that), so it is
not terrain and blocks nothing. So it must not look like the Frozen Waste's open water, which does
block - black-blue with a pale ice shelf for a shore. This is opaque, frosted, mid-value ice with
snow blown across it in streaks, a bank of drifted snow dithered out into the ground, and only one
window of clear dark ice: round him, about where fighting warms the block (HeatRadius 150, the
window 160x128 in the world). The light shows through that window brightest, so the reach of the
heat is where the light is - without a ring drawn on the floor.

THE WIZARD IS HIS OWN SPRITE (roster.py's vainamoinen-idle frame 0), seen through the ice: every
pixel recoloured by its value into the ice's own tones, the staff's head kept as one cold point of
light. Not his name (a rescued wizard's name is the reward), but the silhouette the character
select already shows while he is locked.
"""
from __future__ import division, print_function

import json
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

import bonelight as bl
import raster

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "thaw")
FIGURE = os.path.join(ROOT, "assets", "bonelight", "characters", "vainamoinen-idle.png")
PREVIEW = os.path.join(ROOT, "tools", "art", "_thaw_lake_preview.png")

W, H = 352, 288           # multiples of 32, so the B channel's noise tiles across the texture
CX, CY = W / 2.0, H / 2.0
A, B = 160.0, 128.0       # the shore's semi-axes in art pixels (320 x 256 in the world)
BANK = 10.0               # the drifted snow outside the shore, dithered out into the ground
WIN_A, WIN_B = 80.0, 64.0  # the window of clear ice round him
NOISE_CELL = 32

T = bl.tone
E = bl.ELEMENTS
OCC = bl.OCC

# Through the ice, by how much light reaches it: base -> one step -> two steps.
LIGHT = {
    T("arcane", "deep"): (T("arcane", "shade"), E["ice"][3]),
    T("arcane", "shade"): (T("arcane", "base"), T("arcane", "lit")),
    T("steel", "deep"): (T("arcane", "shade"), T("arcane", "base")),
    T("steel", "shade"): (T("arcane", "base"), T("arcane", "lit")),
    T("steel", "base"): (T("arcane", "lit"), E["ice"][2]),
    T("skin", "shade"): (T("steel", "base"), T("arcane", "lit")),
    T("skin", "base"): (E["ice"][2], E["ice"][1]),
    T("skin", "lit"): (E["ice"][1], E["ice"][0]),
}

BAYER = raster.BAYER8


def bayer(x, y):
    return (BAYER[y % 8][x % 8] + 0.5) / 64.0


# --- noise ---------------------------------------------------------------------------------------

class Noise(object):
    """Value noise on a lattice, smooth, optionally wrapping every (wx, wy) cells."""

    def __init__(self, seed, cell, wx=None, wy=None):
        self.r = random.Random(seed)
        self.cell = float(cell)
        self.wx, self.wy = wx, wy
        self.v = {}

    def lat(self, i, j):
        if self.wx:
            i %= self.wx
        if self.wy:
            j %= self.wy
        k = (i, j)
        if k not in self.v:
            self.v[k] = self.r.random()
        return self.v[k]

    def __call__(self, x, y):
        fx, fy = x / self.cell, y / self.cell
        i, j = int(math.floor(fx)), int(math.floor(fy))
        tx, ty = fx - i, fy - j
        sx, sy = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        a = self.lat(i, j) + (self.lat(i + 1, j) - self.lat(i, j)) * sx
        b = self.lat(i, j + 1) + (self.lat(i + 1, j + 1) - self.lat(i, j + 1)) * sx
        return a + (b - a) * sy


rng = random.Random(5150)
PH = [rng.uniform(0, 2 * math.pi) for _ in range(4)]


def shore(theta):
    """The shore's radius as a fraction of the ellipse: a few slow bays and points."""
    return (1.0 + 0.04 * math.sin(2 * theta + PH[0]) + 0.03 * math.sin(3 * theta + PH[1])
            + 0.01 * math.sin(5 * theta + PH[2]))


def depth(x, y):
    """Signed distance from the shore in art pixels, positive on the ice; and the outward normal."""
    px, py = x + 0.5 - CX, y + 0.5 - CY
    q = math.sqrt((px / A) ** 2 + (py / B) ** 2)
    theta = math.atan2(py / B, px / A)
    edge = shore(theta)
    phi = math.atan2(py, px)
    rphi = 1.0 / math.sqrt((math.cos(phi) / A) ** 2 + (math.sin(phi) / B) ** 2)
    return (edge - q) * rphi, (math.cos(phi), math.sin(phi))


WIN_NOISE = Noise(71, 18)


def window(x, y):
    """How far out of the clear window a pixel is: under 1 inside it."""
    px, py = x + 0.5 - CX, y + 0.5 - CY
    return math.sqrt((px / WIN_A) ** 2 + (py / WIN_B) ** 2) * (0.92 + 0.16 * WIN_NOISE(x, y))


# --- the lake ------------------------------------------------------------------------------------

def draw_lake():
    base = raster.Canvas(W, H)
    transmit = [[0.0] * W for _ in range(H)]
    cover = [[0] * W for _ in range(H)]
    streak = Noise(23, 1.0)
    blotch = Noise(29, 22)
    murk = Noise(31, 16)
    drifts = Noise(37, 40)
    wind = math.radians(-18.0)
    cw, sw = math.cos(wind), math.sin(wind)

    for y in range(H):
        for x in range(W):
            d, n = depth(x, y)
            if d < -BANK:
                continue
            b = bayer(x, y)
            # Which way this bit of shore faces the key (upper left): a bank up-left of the ice throws
            # its shadow on the ice; a bank down-right of it is lit, side on.
            facing = -(n[0] * -0.7071 + n[1] * -0.7071)   # +1: the bank is down-right of the ice
            if d < 0:
                cov = (1.0 + d / BANK) ** 0.8
                if b >= cov:
                    continue
                if d > -3.0 and facing > 0.2:
                    c = T("skin", "base") if b < 0.55 else T("skin", "shade")
                elif d > -5.0:
                    c = T("skin", "shade")
                else:
                    c = T("skin", "shade") if b < 0.5 else T("steel", "shade")
                base.set(x, y, c)
                cover[y][x] = 255
                continue

            cover[y][x] = 255
            u = (x * cw + y * sw)
            v = (-x * sw + y * cw)
            if d < 2.5 or (d < 5.0 and b > (d - 2.5) / 2.5):
                # the shore ice: a pale shelf dithered into the lake (a solid band read as an outline
                # round the lake, the fault .ai/terrain.md section 8 took out of every pond), shadowed
                # under the up-left bank
                if d < 1.2 and facing < -0.25:
                    c = T("steel", "deep")
                elif d < 1.5 and facing > 0.25 and b < 0.5:
                    c = T("skin", "base")
                else:
                    c = T("skin", "shade")
                base.set(x, y, c)
                transmit[y][x] = 0.25
                continue

            w = window(x, y)
            # the window of clear ice, dithered into the frost round it over its last 15%
            if w < 0.85 or (w < 1.15 and b > (w - 0.85) / 0.3):
                c = T("arcane", "deep")
                if murk(x, y) > 0.56 and BAYER[y % 8][x % 8] < 24:
                    c = T("arcane", "shade")
                base.set(x, y, c)
                transmit[y][x] = 1.0
                continue

            # frosted ice, with snow blown across it in long streaks
            # Streaks, not patches: thin across the wind, long along it, and only where a drift lies
            # (solid patches of a second tone read as camouflage printed on the ice - the water's
            # fourth thrown-away render). Thinner ice is a half-tone, never a patch.
            s = streak(u / 46.0, v / 2.6)
            drift = drifts(x, y)
            c = T("steel", "shade")
            t = 0.5
            if blotch(x, y) > 0.64 and BAYER[y % 8][x % 8] < 16:
                c = T("steel", "deep")
                t = 0.6
            if drift > 0.5 and s > 0.8 - 0.25 * (drift - 0.5):
                c = T("steel", "base")
                t = 0.35
                if s > 0.9 and drift > 0.7:
                    c = T("skin", "shade")
                    t = 0.25
            base.set(x, y, c)
            transmit[y][x] = t
    return base, transmit, cover



def join(points):
    """Every pixel on the straight runs between successive points, so a path has no gaps."""
    out = []
    for i in range(len(points) - 1):
        (x0, y0), (x1, y1) = points[i], points[i + 1]
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for k in range(n):
            out.append((int(round(x0 + (x1 - x0) * k / float(n))), int(round(y0 + (y1 - y0) * k / float(n)))))
    if points:
        out.append(points[-1])
    return out


def crack_paths(seed):
    """Cracks from near the block out across the lake: jagged random walks, a few branching."""
    r = random.Random(seed)
    paths = []
    for k in range(7):
        ang = k * 2 * math.pi / 7 + r.uniform(-0.3, 0.3)
        x, y = CX + math.cos(ang) * 16, CY + math.sin(ang) * 13
        length = r.uniform(70, 150)
        path = []
        walked = 0.0
        a = ang
        while walked < length:
            a += r.uniform(-0.35, 0.35)
            a = ang + max(-0.5, min(0.5, a - ang))
            x += math.cos(a)
            y += math.sin(a) * 0.82
            walked += 1.0
            path.append((int(x), int(y)))
            if r.random() < 0.012 and walked > 25:
                # a branch
                ba = a + r.choice([-1, 1]) * r.uniform(0.6, 1.0)
                bx, by = x, y
                branch = []
                for _ in range(int(r.uniform(10, 26))):
                    ba += r.uniform(-0.3, 0.3)
                    bx += math.cos(ba)
                    by += math.sin(ba) * 0.82
                    branch.append((int(bx), int(by)))
                paths.append(join(branch))
        paths.append(join(path))
    # two arcs of a ring crack, part way round, where the clear window gives way to frost
    for (frac, a0, a1) in [(1.12, -2.6, -1.1), (1.55, 0.3, 1.5)]:
        path = []
        steps = int((a1 - a0) * WIN_A * frac)
        for i in range(steps):
            a = a0 + (a1 - a0) * i / float(steps)
            rr = frac * (1.0 + 0.03 * math.sin(i * 0.7))
            path.append((int(CX + math.cos(a) * WIN_A * rr), int(CY + math.sin(a) * WIN_B * rr)))
        paths.append(join(path))
    return paths


def draw_cracks(base, transmit):
    pts = set()
    for path in crack_paths(611):
        for p in path:
            pts.add(p)
    for (x, y) in pts:
        if not (0 <= x < W and 0 <= y < H):
            continue
        d, _ = depth(x, y)
        if d < 5.0:
            continue
        if transmit[y][x] >= 1.0:
            # in clear ice a crack is a pale fracture plane
            base.set(x, y, T("steel", "base"))
        else:
            # in frost, a dark line (a lit lip beside it stair-stepped every diagonal into a saw)
            base.set(x, y, T("steel", "deep"))
        transmit[y][x] = 1.0
    return pts


def draw_bubbles(base, transmit):
    """Frozen bubbles in the clear ice: short stacks of pale discs, the thing that says 'ice' about a
    dark patch."""
    r = random.Random(907)
    placed = 0
    tries = 0
    while placed < 10 and tries < 400:
        tries += 1
        x = int(CX + r.uniform(-WIN_A, WIN_A) * 0.85)
        y = int(CY + r.uniform(-WIN_B, WIN_B) * 0.85)
        if window(x, y) > 0.8 or math.hypot(x - CX, (y - CY) * 1.25) < 26:
            continue
        placed += 1
        n = r.randint(3, 6)
        bx, by = x, y
        for i in range(n):
            big = r.random() < 0.45
            if big:
                for (dx, dy, c) in [(0, 0, T("skin", "lit")), (1, 0, T("skin", "base")),
                                    (0, 1, T("skin", "base")), (1, 1, T("steel", "base"))]:
                    base.set(bx + dx, by + dy, c)
                    if 0 <= by + dy < H and 0 <= bx + dx < W:
                        transmit[by + dy][bx + dx] = 0.8
                by -= 3
            else:
                base.set(bx, by, T("skin", "base"))
                if 0 <= by < H and 0 <= bx < W:
                    transmit[by][bx] = 0.8
                by -= 2
            bx += r.choice([-1, 0, 0, 1])


def frost_ferns(base_cover, cracks, transmit):
    """Frost ferns: a trunk from the shore (and from the cracks in the frost) growing inward, with
    branches at about 55 degrees. Each pixel records WHEN in the cycle it grows in."""
    r = random.Random(1213)
    grow = {}

    def fern(x, y, ang, length, t0, span):
        """A crystal: a wandering trunk, and short side shoots off it at irregular steps, one side
        or the other, never both at once (a pair either side read as an arrowhead pointing in)."""
        cx_, cy_ = float(x), float(y)
        a = ang
        next_shoot = r.randint(2, 4)
        for i in range(int(length)):
            a += r.uniform(-0.3, 0.3)
            cx_ += math.cos(a)
            cy_ += math.sin(a) * 0.85
            px, py = int(cx_), int(cy_)
            t = t0 + span * i / max(1.0, length)
            if 0 <= px < W and 0 <= py < H:
                grow[(px, py)] = min(grow.get((px, py), 1.0), t)
            if i == next_shoot:
                next_shoot += r.randint(2, 5)
                side = r.choice([-1, 1])
                ba = a + side * math.radians(r.uniform(60, 95))
                bl_ = r.uniform(2, 5) * (1.0 - i / max(1.0, length) * 0.5)
                bx, by = cx_, cy_
                for j in range(int(bl_)):
                    ba += r.uniform(-0.25, 0.25)
                    bx += math.cos(ba)
                    by += math.sin(ba) * 0.85
                    qx, qy = int(bx), int(by)
                    tt = t + span * 0.5 * (j + 1) / max(1.0, length)
                    if 0 <= qx < W and 0 <= qy < H:
                        grow[(qx, qy)] = min(grow.get((qx, qy), 1.0), tt)

    # From the shore, every few degrees, growing in toward the middle.
    n_roots = 34
    for k in range(n_roots):
        theta = k * 2 * math.pi / n_roots + r.uniform(-0.04, 0.04)
        edge = shore(math.atan2(math.sin(theta), math.cos(theta)))
        x = CX + math.cos(theta) * A * edge * 0.965
        y = CY + math.sin(theta) * B * edge * 0.965
        inward = math.atan2(CY - y, CX - x) + r.uniform(-0.7, 0.7)
        fern(x, y, inward, r.uniform(8, 22), r.uniform(0.02, 0.12), r.uniform(0.55, 0.85))
    # From the cracks out in the frost.
    crack_list = sorted(cracks)
    r.shuffle(crack_list)
    used = 0
    for (x, y) in crack_list:
        if used >= 14:
            break
        if not (0 <= x < W and 0 <= y < H) or transmit[y][x] < 1.0:
            continue
        if window(x, y) < 1.15 or depth(x, y)[0] < 8:
            continue
        used += 1
        fern(x, y, r.uniform(0, 2 * math.pi), r.uniform(6, 14), r.uniform(0.2, 0.4), r.uniform(0.4, 0.6))

    # Keep frost to the ice: not on the bank, not in the clear window, not on the shore shelf.
    out = {}
    for (x, y), t in grow.items():
        d, _ = depth(x, y)
        if d < 3.0 or window(x, y) < 1.0 or base_cover[y][x] == 0:
            continue
        out[(x, y)] = t
    return out


# --- the block -----------------------------------------------------------------------------------

CELL = 64
FRONT = [(15, 58), (13, 21), (19, 11), (38, 8), (47, 13), (49, 57)]
SIDE = [(47, 13), (53, 17), (54, 53), (49, 57)]
TOP = [(19, 11), (29, 4), (45, 5), (53, 17), (47, 13), (38, 8)]


def inside(poly, x, y):
    px, py = x + 0.5, y + 0.5
    c = False
    n = len(poly)
    for i in range(n):
        ax, ay = poly[i]
        bx, by = poly[(i + 1) % n]
        if (ay > py) != (by > py):
            xi = ax + (py - ay) * (bx - ax) / float(by - ay)
            if px < xi:
                c = not c
    return c


def figure_tones(img):
    """His idle frame, by value, into the ice's own tones; the staff's head a cold point of light."""
    fig = {}
    w, h = img.size
    for y in range(h):
        for x in range(w):
            p = img.getpixel((x, y))
            if p[3] < 128:
                continue
            lum = (0.2126 * p[0] + 0.7152 * p[1] + 0.0722 * p[2]) / 255.0
            sat = (max(p[:3]) - min(p[:3])) / 255.0
            if p[1] > 150 and sat > 0.4 and p[1] > p[2]:
                c = E["ice"][1]          # the staff's head, frozen but still lit
            elif lum < 0.12:
                c = T("arcane", "deep")
            elif lum < 0.28:
                c = T("stone", "deep") if lum < 0.18 else T("steel", "deep")
            elif lum < 0.45:
                c = T("arcane", "shade")
            elif lum < 0.65:
                c = T("steel", "shade")
            else:
                c = T("steel", "base")
            fig[(x, y)] = c
    return fig


def block_frame(fig, stage, glint, r):
    c = raster.Canvas(CELL, CELL)
    if stage >= 2:
        # meltwater pooling at the foot, wider as it goes; a few lit ripples on it
        rx = 20.0 + 3.0 * (stage - 2)
        for x in range(4, 62):
            for y in range(54, 64):
                if ((x - 33.5) / rx) ** 2 + ((y - 59.5) / 3.6) ** 2 <= 1.0:
                    c.set(x, y, T("arcane", "deep"))
        for (x, y) in [(14, 60), (15, 60), (50, 61), (51, 61), (24, 62), (44, 62), (45, 62)]:
            if c.get(x, y)[3]:
                c.set(x, y, T("arcane", "base"))
    # contact shadow and snow at the foot
    for x in range(14, 54):
        c.set(x, 59, OCC)
    for x in range(16, 52):
        c.set(x, 58, OCC)

    lit_edge = E["ice"][1]
    for y in range(CELL):
        for x in range(CELL):
            if inside(TOP, x, y):
                col = T("skin", "base") if (x + y) % 7 else T("skin", "lit")
                if y > 10 and x > 44:
                    col = T("steel", "base")
                c.set(x, y, col)
            elif inside(SIDE, x, y):
                col = T("arcane", "deep") if (x * 3 + y) % 5 else T("steel", "deep")
                c.set(x, y, col)
            elif inside(FRONT, x, y):
                col = T("arcane", "shade")
                if (x - 13) + (y - 10) * 0.3 < 9:
                    col = T("arcane", "base")      # the key side of the face, catching the light
                c.set(x, y, col)
    # the figure, seen through the front face (feet on row 54)
    for (fx, fy), col in fig.items():
        x, y = fx + 8, fy + 8
        if inside(FRONT, x, y):
            if stage >= 3 and col in (T("arcane", "deep"), T("stone", "deep")):
                col = T("steel", "deep")         # thinner ice: he shows more
            c.set(x, y, col)
    # facet lines and the lit edges: the key is upper left
    for (a, b) in [((13, 21), (19, 11)), ((19, 11), (38, 8)), ((19, 11), (29, 4)), ((29, 4), (45, 5))]:
        c.line(a[0], a[1], b[0], b[1], lit_edge)
    for (a, b) in [((38, 8), (47, 13)), ((47, 13), (49, 57)), ((47, 13), (53, 17))]:
        c.line(a[0], a[1], b[0], b[1], T("arcane", "lit"))
    c.line(13, 22, 15, 57, T("arcane", "lit"))
    c.line(53, 18, 54, 52, T("steel", "deep"))
    # two internal fracture planes, faint
    c.line(22, 24, 30, 34, T("arcane", "base"))
    c.line(41, 30, 36, 44, T("arcane", "base"))

    # the glint: a band of light sweeping across the face, frame by frame (3 = at rest)
    if glint < 3:
        off = 6 + glint * 16
        for y in range(CELL):
            for x in range(CELL):
                if not (inside(FRONT, x, y) or inside(TOP, x, y)):
                    continue
                k = x + (y - 30) * 0.55 - off
                if 0 <= k < 3:
                    p = c.get(x, y)
                    h = "#%02X%02X%02X" % p[:3]
                    up = LIGHT.get(h)
                    if up:
                        c.set(x, y, up[0])
                    elif h in (T("arcane", "base"),):
                        c.set(x, y, T("arcane", "lit"))
                    elif h in (T("arcane", "lit"),):
                        c.set(x, y, E["ice"][2])

    # thawing: cracks, a chipped corner, meltwater at the foot
    cr = random.Random(77)
    crack_sets = [
        [],
        [((14, 30), (22, 36), (20, 44)), ((48, 20), (42, 26))],
        [((14, 30), (22, 36), (20, 44), (26, 52)), ((48, 20), (42, 26), (44, 34)), ((30, 9), (32, 18), (27, 24))],
        [((14, 30), (22, 36), (20, 44), (26, 52)), ((48, 20), (42, 26), (44, 34), (39, 40)),
         ((30, 9), (32, 18), (27, 24), (31, 31)), ((49, 46), (43, 49), (40, 56)), ((16, 16), (24, 20))],
    ][stage]
    for chain in crack_sets:
        for i in range(len(chain) - 1):
            c.line(chain[i][0], chain[i][1], chain[i + 1][0], chain[i + 1][1], E["ice"][0])
    if stage >= 2:
        # the top corner gone
        for y in range(0, 14):
            for x in range(40, 56):
                if (x - 40) + (13 - y) > 17:
                    c.px[x, y] = (0, 0, 0, 0)
    if stage >= 3:
        # the face slumping: its lower corners rounded off, drips
        for (x, y) in [(15, 57), (15, 56), (16, 57), (49, 56), (48, 57), (49, 55)]:
            c.px[x, y] = (0, 0, 0, 0)
        for x in (22, 31, 43):
            c.set(x, 58, T("arcane", "lit"))
            c.set(x, 59, T("arcane", "base"))
    # snow lumps at the foot (after melt, fewer)
    lumps = [(17, 57), (20, 58), (25, 58), (44, 58), (47, 57), (51, 56)][: 6 - stage]
    for (x, y) in lumps:
        c.set(x, y, T("skin", "base"))
        c.set(x + 1, y, T("skin", "shade"))
    return c


# --- output --------------------------------------------------------------------------------------

def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)

    base, transmit, cover = draw_lake()
    cracks = draw_cracks(base, transmit)
    draw_bubbles(base, transmit)
    ferns = frost_ferns(cover, cracks, transmit)

    lit = raster.Canvas(W, H)
    bright = raster.Canvas(W, H)
    data = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dpx = data.load()
    wander = Noise(41, NOISE_CELL, W // NOISE_CELL, H // NOISE_CELL)
    for y in range(H):
        for x in range(W):
            p = base.get(x, y)
            if p[3] == 0:
                # B still carries the noise outside the lake, so a shifted read inside it never
                # falls on a hole.
                # A is opaque everywhere, so the importer never rewrites a data texel (fix_alpha_border);
                # the lake texture says where the lake is.
                dpx[x, y] = (0, 255, int(255 * wander(x, y)), 255)
                continue
            h = "#%02X%02X%02X" % p[:3]
            up = LIGHT.get(h, (h, h))
            lit.set(x, y, up[0])
            bright.set(x, y, up[1])
            g = ferns.get((x, y))
            dpx[x, y] = (int(round(255 * max(0.0, min(1.0, transmit[y][x])))),
                         255 if g is None else int(round(250 * max(0.0, min(1.0, g)))),
                         int(255 * wander(x, y)), 255)

    ok = True
    for name, canvas in [("lake", base), ("lit", lit), ("bright", bright)]:
        bad = canvas.audit()
        if bad:
            ok = False
            print("OFF-CONTRACT COLOURS in %s: %s" % (name, bad[:5]))
    base.img.save(os.path.join(OUT, "thaw-lake.png"))
    lit.img.save(os.path.join(OUT, "thaw-lake-lit.png"))
    bright.img.save(os.path.join(OUT, "thaw-lake-bright.png"))
    data.save(os.path.join(OUT, "thaw-lake-data.png"))

    fig_img = Image.open(FIGURE).convert("RGBA").crop((0, 0, 48, 48))
    fig = figure_tones(fig_img)
    sheet = Image.new("RGBA", (CELL * 16, CELL), (0, 0, 0, 0))
    r = random.Random(3)
    for stage in range(4):
        for glint in range(4):
            c = block_frame(fig, stage, glint, r)
            bad = c.audit()
            if bad:
                ok = False
                print("OFF-CONTRACT COLOURS in block %d/%d: %s" % (stage, glint, bad[:5]))
            sheet.alpha_composite(c.img, ((stage * 4 + glint) * CELL, 0))
    sheet.save(os.path.join(OUT, "thaw-block.png"))

    # Preview: the lake at x2 on the Frozen Waste's stone with the block on it, then lit and bright.
    ground = raster.rgb(T("stone", "base"))
    pv = Image.new("RGBA", (W * 2 * 3 + 24, H * 2 + 8), ground)
    for i, cv in enumerate([base, lit, bright]):
        big = cv.img.resize((W * 2, H * 2), Image.NEAREST)
        pv.alpha_composite(big, (4 + i * (W * 2 + 8), 4))
    blk = sheet.crop((0, 0, CELL, CELL)).resize((CELL * 2, CELL * 2), Image.NEAREST)
    pv.alpha_composite(blk, (4 + int(CX * 2) - CELL, 4 + int(CY * 2) - 118))
    pv.convert("RGB").save(PREVIEW)
    big_sheet = sheet.resize((CELL * 16 * 2, CELL * 2), Image.NEAREST)
    sp = Image.new("RGBA", big_sheet.size, raster.rgb(T("steel", "shade")))
    sp.alpha_composite(big_sheet)
    sp.convert("RGB").save(PREVIEW.replace(".png", "_block.png"))

    print("lake %dx%d (x2 in the world), %d frost pixels, %d crack pixels; block 16 frames"
          % (W, H, len(ferns), len(cracks)))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
