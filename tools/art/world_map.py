# -*- coding: utf-8 -*-
"""The world map: the drawn world the wizard walks between chapters. Run: python tools/art/world_map.py

Writes, under assets/bonelight/ui/worldmap/:

    world.png         the world, 3424x840 at 1:1 (the screen draws it at a whole number of screen
                      pixels per art pixel, x2 on a 1:1 canvas - .ai/art-direction.md section 6a): the
                      road's land, his castle on its rock, and across the dark sea the far shore
    tower-unlit.png   his tower once he has fallen, cut to where it differs, laid over the world after
                      Taken Back (the light going home, .ai/world-map.md section 19)
    sketchbook.png    the Sketchbook's page: its own sheet, a map apart from the world, with a short
                      pencil road on it to where the wizard stands
    shroud-<stop>.png the bank of his fog laid over a locked place, one per stop, sized to it
    shadow.png        the wizard's contact shadow on the map
    ring-open.png, ring-locked.png, ring-current.png   the mark under each stop's stand point
    mark-*.png        the compass strip's marks (16x16 art, saved x2 like every UI icon)
    secret.png, keepsake.png   the card's icons for a stage secret and a keepsake (32x32, saved x2)
    arrow-left.png, arrow-right.png   the step plates' chevrons (saved x2)
    worldmap.json     the stops, their stand points, focus points and shrouds, the route, and the light
                      going home (the unlit tower's place, the motes' path)

    python tools/art/world_map.py --preview-far   only the east (columns 1800 on), to
                                                  tools/art/_worldmap/far.png, in about a minute;
                                                  writes none of the game's art

The design is .ai/world-map.md. The geometry the screen uses comes from worldmap.json and nowhere
else, so moving a place is a line in PLACES or WAYPOINTS below and a re-run.

THE KIDS' ART IS NEVER OPENED HERE. The Sketchbook's page is drawn by this file; the drawings on it
are theirs, laid on the page at runtime from their own files (WorldMapView), untouched. The page's
JSON names those files; this generator does not read a pixel of them.

Python 2.7 and 3 both. Every random choice comes from rng.random() on a seeded Random, so a re-run
writes the same bytes.
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
OUT_DIR = os.path.join(ROOT, "assets", "bonelight", "ui", "worldmap")
PREVIEW_DIR = os.path.join(ROOT, "tools", "art", "_worldmap")

W, H = 3424, 840
# The land's width before the castle: the road's world ended here, at the Emberdeep's cliffs, until the
# castle grew it east across the dark sea (.ai/endgame-castle.md 8). The seeded scatters that spread over
# the land read this, not W, so widening the world moved none of their trees.
LAND_W = 1680
# The world's width before the far shore (piece F5, .ai/second-half.md 4.1): his rock stood at the east edge and
# the dark sea ran off the picture. Everything west of here is drawn exactly as it was; past it the dark sea goes
# on east to the white cliffs, and the far shore runs west to east from them. 3424 stays well inside a phone's
# texture limit (4096 is the floor any GLES3 device reports).
CASTLE_W = 2112
# West of here nothing of the far shore's ground is grown (its seeds are searched only east of it).
FAR_X0 = 2170
# Per-pixel passes start here. 0 for the art; --preview-far sets it so a far-shore change can be judged in a
# fraction of the run, writing only tools/art/_worldmap/far.png (never the game's art).
X_FROM = 0
OCC = bl.OCC
E = bl.ELEMENTS
DITHER = raster.BAYER8


def T(mat, role):
    return bl.tone(mat, role)


# ---------------------------------------------------------------------------------------------
# THE PLACES. Each stop: its id (a StageCatalog id, or boss_rush),
# the stand point on the road where the wizard waits (his feet), the focus the camera frames (the
# landmark's middle), and the radii of the fog laid over it while it is locked. West to east in
# stop order; .ai/world-map.md section 1 says why each is where it is.
#
# THE ORDER IS THE ROAD'S (WorldMap.RoadOrder, .ai/endgame-castle.md 8): the campaign up to its gate
# chapter, the Boss Rush on the gate's summit, then the rest of the campaign - the castle across the
# water - so the road never runs back from the tower to the summit. The Cursed Dungeon is no longer the
# keep on the river's crag: since piece C2 it is the castle's cells, and its stop is at the castle's foot.
# ---------------------------------------------------------------------------------------------
PLACES = [
    # id,          stand,        focus,        shroud rx, ry
    ("stage_0",  (204, 486), (150, 424), 96, 72),   # Enchanted Forest - the west wood
    ("stage_9",  (370, 574), (362, 646), 92, 56),   # The Fallow Fields - south of the road
    ("stage_10", (522, 364), (520, 298), 74, 52),   # The Broken Circle - up on the heath
    ("stage_2",  (664, 532), (690, 466), 62, 50),   # Sunken Cave - under the scarp
    ("stage_3",  (976, 600), (1004, 670), 88, 56),  # Blighted Swamp - the delta bog
    ("stage_4",  (1060, 428), (1066, 360), 78, 56), # Mystic Ruins - the plateau
    ("stage_5",  (1200, 218), (1206, 150), 92, 60), # Frozen Waste - under the northern peaks
    ("stage_6",  (1340, 542), (1378, 626), 90, 58), # Scorched Sands - past the pass
    ("stage_7",  (1470, 532), (1508, 470), 70, 56), # The Emberdeep - the black gate
    ("boss_rush", (1572, 350), (1576, 300), 46, 30),# the summit, over his forge
    # THE CASTLE, across the dark sea.
    ("stage_11", (1720, 456), (1796, 432), 70, 34), # The Last Bridge - from the cliffs to his rock
    ("stage_1",  (1874, 404), (1934, 428), 46, 18), # Cursed Dungeon - the cells in the rock, under his walls
    ("stage_12", (1926, 364), (1978, 334), 50, 28), # The Court - the halls inside the walls
    ("stage_13", (1999, 298), (2000, 236), 30, 58), # The Throne - his tower, and the dark light at its top
    # THE FAR SHORE (.ai/second-half.md 4.1-4.2, piece F5), in its own order (StageCatalog.FarShoreOrder): the road
    # goes on past the Throne - out of his east postern, down the rock to the sea-gate, over the old causeway to
    # the cove under the white cliffs - and runs west to east from them. Nothing of it is shown before Taken Back
    # (GlobalStatsManager.IsChapterRevealed); then the first stop, and each later one once it is earned.
    ("stage_14", (2268, 350), (2320, 292), 74, 50), # The White Downs - the barrows, the hound and the hart
    ("stage_15", (2440, 560), (2478, 644), 90, 56), # The Drowned Shore - the seabed the tide left
    ("stage_16", (2600, 430), (2642, 370), 72, 48), # The Three Ways - roads that fork in three
    ("stage_17", (2800, 560), (2852, 624), 86, 52), # The Twelfth Hour - the avenue by the still river
    ("stage_18", (2930, 300), (2976, 228), 80, 52), # The Thunder Fell - granite under the storm
    ("stage_19", (3196, 432), (3268, 414), 64, 46), # The Bronze Isle - the god's cold forge
    # The Sketchbook is not a stop on this road (Eric, 2026-10-30: "sketchbook level should not be
    # attached to the map ... it shows up as its own separate map"). It is the page below, its own
    # sheet, reached from the world map once the campaign is finished (.ai/world-map.md, section 4).
]

# The road between the stand points, in order. Each list runs from one stand point to the next
# (exclusive of both); the spline goes through every point. A list past the last stop would be a TAIL:
# drawn, walked by no one (the road off the summit was one, until the castle joined it).
WAYPOINTS = [
    [(258, 518), (312, 548)],                               # forest -> fields
    [(424, 540), (452, 474), (484, 410)],                   # fields -> up onto the heath
    [(566, 402), (602, 460), (628, 506)],                   # heath -> down the scarp to the cave
    # cave -> over the river's bridge to the bog. The keep's stand point (800, 642) is a plain point of
    # the road now: the same points, so the same road, past the crag with nothing on it.
    [(704, 562), (744, 604), (772, 632), (800, 642), (852, 632), (900, 612), (938, 602)],
    [(1000, 560), (1022, 508), (1042, 462)],                # bog -> up onto the plateau
    [(1090, 372), (1122, 318), (1152, 270), (1176, 240)],   # plateau -> north into the ice
    [(1230, 262), (1250, 330), (1268, 400), (1298, 466), (1320, 512)],  # ice -> the pass -> sands
    [(1392, 548), (1432, 544)],                             # sands -> the gate
    [(1496, 520), (1510, 494), (1515, 462), (1540, 434), (1522, 408), (1550, 386), (1564, 366)],  # through the gate -> the summit
    [(1600, 384), (1632, 410), (1660, 432), (1680, 444)],   # the summit -> down to the cliffs, the bridge foot
    [(1760, 443), (1800, 429), (1840, 415)],                # over the Last Bridge -> the gate on his rock
    [(1888, 396), (1902, 384), (1914, 374)],                # the gate -> up the yard to the hall door
    [(1926, 346), (1936, 322), (1952, 308), (1972, 300), (1988, 298)],  # round the hall -> the tower door
    # THE CROSSING (piece F5): from the tower door east across the yard to his postern in the east wall, down a
    # stair cut in the rock's south-east face to the sea-gate's quay, over the old causeway to the cove under the
    # white cliffs, and up the coombe onto the downs. It leaves the tower EAST, never back through the yard: a road
    # that doubled back would lie under the castle's lit road and the reveal's dark would fall on the castle.
    [(2014, 304), (2028, 312), (2040, 322), (2048, 334), (2053, 346), (2058, 356), (2068, 363), (2094, 370),
     (2122, 377), (2150, 384), (2178, 392), (2204, 400), (2222, 394), (2236, 381), (2248, 368), (2258, 358)],
    # the downs -> east along the top, down the cutting in the scarp to the flats
    [(2300, 368), (2336, 390), (2370, 416), (2396, 448), (2410, 480), (2418, 512), (2428, 540)],
    # the flats -> east across the sand, up into the country of the three ways
    [(2470, 560), (2500, 550), (2530, 530), (2556, 500), (2576, 470), (2590, 446)],
    # the three ways -> down onto the black sand by the river
    [(2640, 446), (2680, 466), (2716, 490), (2748, 516), (2774, 540)],
    # the black sand -> north, up onto the fell
    [(2830, 528), (2850, 488), (2866, 446), (2884, 404), (2900, 364), (2916, 330)],
    # the fell -> east, down to the coast and over the bronze bridge to the isle
    [(2966, 318), (3006, 340), (3046, 366), (3080, 392), (3108, 412), (3132, 424), (3160, 428), (3182, 431)],
]

# THE CROSSING'S PIECES (castle_crossing(), causeway()): his postern in the east wall, the stair's foot on the
# sea-gate's quay, and the beach in the cove where the causeway lands.
POSTERN = (2040, 322)
QUAY = (2066, 363)
COVE = (2206, 400)
# The bronze bridge to the isle: from the far shore's east coast to the isle's landing (the Bronze Isle's stand).
BRONZE_FROM = (3132, 424)
BRONZE_TO = (3196, 432)

# THE CASTLE (.ai/endgame-castle.md 8), drawn by castle() below. The black cliffs end the land a little
# east of where the world used to end; the Last Bridge leaves them at the headland, where the road comes
# down off the summit, and lands at the gate on his rock.
BRIDGE_FROM = (1720, 456)   # the bridge foot, on the cliffs (the Last Bridge's stand point)
BRIDGE_TO = (1874, 404)     # the gate on the rock (the Dungeon's stand point, where the cells go down)
# The rock's top, the plateau the castle stands on, clockwise from the gate. Its south edge is the top
# of the cliff that drops to the sea; the cells are cut into that cliff, under the south wall.
ISLE_TOP = [(1864, 410), (1858, 384), (1866, 346), (1884, 306), (1910, 270), (1944, 242), (1980, 222),
            (2018, 214), (2044, 228), (2056, 262), (2054, 304), (2044, 344), (2030, 380), (2010, 404),
            (1972, 414), (1918, 416)]
ISLE_CLIFF = 30             # how far the rock's south face drops to the water, in map pixels
DECK_HEIGHT = 15            # how far the bridge's deck stands above the water

# THE PAGE: the Sketchbook's own sheet, a map of its own with nothing of the world on it. Every
# coordinate below is in the page image's own pixels (it used to be placed in map space, off the
# world's east edge, with the road running onto it as a pencil line).
PAGE_SIZE = (204, 246)
# The sheet inside that image; the margin round it holds its shadow and the tape.
PAGE_RECT = (8, 8, 192, 236)
# Where the wizard stands on it (his feet), at the end of its own short pencil road.
PAGE_STAND = (82, 142)
# The kids' drawings laid on the page at runtime, unmodified, from their own files.
PAGE_DRAWINGS = [
    # file, top-left in PAGE space
    ("res://assets/kidsart/doodle-sun.png", (90, 16)),
    ("res://assets/kidsart/kid-wizard-moving.png", (28, 60)),
]


def rng_for(seed):
    return random.Random(seed)


# ---------------------------------------------------------------------------------------------
# A FAST GRID. raster.Canvas converts a hex string on every write, and the world is 1.4 million
# pixels written several times over; this caches the conversion and writes the image once.
# ---------------------------------------------------------------------------------------------
_RGB = {}


def rgba(c):
    if c is None:
        return (0, 0, 0, 0)
    if isinstance(c, tuple):
        return c
    v = _RGB.get(c)
    if v is None:
        h = c.lstrip("#")
        v = (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)
        _RGB[c] = v
    return v


def lum(c):
    c = rgba(c)
    return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]


class Grid(object):
    def __init__(self, w, h, fill=None):
        self.w = w
        self.h = h
        self.pix = [rgba(fill)] * (w * h)

    def set(self, x, y, c):
        x = int(x)
        y = int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            self.pix[y * self.w + x] = rgba(c)

    def get(self, x, y):
        x = int(x)
        y = int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.pix[y * self.w + x]
        return (0, 0, 0, 0)

    def image(self):
        img = Image.new("RGBA", (self.w, self.h))
        img.putdata(self.pix)
        return img

    def rect(self, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.set(x, y, c)

    def disc(self, cx, cy, rx, ry, c):
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                nx = (x - cx) / float(max(rx, 0.5))
                ny = (y - cy) / float(max(ry, 0.5))
                if nx * nx + ny * ny <= 1.0:
                    self.set(x, y, c)

    def line(self, x0, y0, x1, y1, c):
        x0, y0, x1, y1 = int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))
        dx = abs(x1 - x0)
        dy = -abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx + dy
        while True:
            self.set(x0, y0, c)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def poly(self, pts, c):
        ys = [p[1] for p in pts]
        n = len(pts)
        for y in range(int(min(ys)), int(max(ys)) + 1):
            xs = []
            for i in range(n):
                ax, ay = pts[i]
                bx, by = pts[(i + 1) % n]
                if ay == by:
                    continue
                if min(ay, by) <= y < max(ay, by):
                    xs.append(ax + (y - ay) * (bx - ax) / float(by - ay))
            xs.sort()
            for i in range(0, len(xs) - 1, 2):
                for x in range(int(math.floor(xs[i])), int(math.ceil(xs[i + 1])) + 1):
                    self.set(x, y, c)


def dith(x, y, f):
    """True where an ordered dither of strength f (0..1) puts the second tone."""
    return f > DITHER[int(y) % 8][int(x) % 8] / 64.0


# ---------------------------------------------------------------------------------------------
# NOISE. Value noise on a seeded lattice, summed over octaves.
# ---------------------------------------------------------------------------------------------
class Noise(object):
    def __init__(self, seed, cell):
        r = rng_for(seed)
        self.cell = float(cell)
        self.n = 97
        self.v = [r.random() for _ in range(self.n * self.n)]

    def at(self, x, y):
        fx = x / self.cell
        fy = y / self.cell
        ix = int(math.floor(fx))
        iy = int(math.floor(fy))
        tx = fx - ix
        ty = fy - iy
        tx = tx * tx * (3 - 2 * tx)
        ty = ty * ty * (3 - 2 * ty)
        n = self.n
        a = self.v[(iy % n) * n + (ix % n)]
        b = self.v[(iy % n) * n + ((ix + 1) % n)]
        c = self.v[((iy + 1) % n) * n + (ix % n)]
        d = self.v[((iy + 1) % n) * n + ((ix + 1) % n)]
        return (a + (b - a) * tx) + ((c + (d - c) * tx) - (a + (b - a) * tx)) * ty


class Fbm(object):
    def __init__(self, seed, cell, octaves=3):
        self.layers = [Noise(seed + i * 101, cell / (2 ** i)) for i in range(octaves)]

    def at(self, x, y):
        total = 0.0
        amp = 1.0
        norm = 0.0
        for layer in self.layers:
            total += layer.at(x, y) * amp
            norm += amp
            amp *= 0.5
        return total / norm


class Sampled(object):
    """A smooth field sampled on a coarse grid and read back bilinearly: the broad noises vary over
    tens of pixels, so evaluating every octave at every pixel is wasted work."""

    def __init__(self, field, step, w, h):
        self.step = step
        self.cols = w // step + 2
        self.rows = h // step + 2
        self.v = [field.at(c * step, r * step) for r in range(self.rows) for c in range(self.cols)]

    def at(self, x, y):
        fx = x / float(self.step)
        fy = y / float(self.step)
        ix = min(self.cols - 2, max(0, int(fx)))
        iy = min(self.rows - 2, max(0, int(fy)))
        tx = fx - ix
        ty = fy - iy
        c = self.cols
        a = self.v[iy * c + ix]
        b = self.v[iy * c + ix + 1]
        d = self.v[(iy + 1) * c + ix]
        e = self.v[(iy + 1) * c + ix + 1]
        top = a + (b - a) * tx
        return top + ((d + (e - d) * tx) - top) * ty


WARP_X = Fbm(11, 90)
WARP_Y = Fbm(23, 90)
GRAIN = Fbm(37, 14, 2)
BROAD = Sampled(Fbm(41, 60), 6, W, H)

# ---------------------------------------------------------------------------------------------
# THE ROUTE. A Catmull-Rom spline through every stand point and waypoint, sampled about every two
# pixels. The screen walks the wizard along these points by arc length.
# ---------------------------------------------------------------------------------------------


def catmull(p0, p1, p2, p3, t):
    t2 = t * t
    t3 = t2 * t
    return (
        0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2
               + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3),
        0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2
               + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3),
    )


def build_route():
    """(points, stop_index): the sampled road, and the sample index of every stop's stand point."""
    control = []
    stop_at = {}
    for i, place in enumerate(PLACES):
        stop_at[place[0]] = len(control)
        control.append(place[1])
        if i < len(WAYPOINTS):
            control.extend(WAYPOINTS[i])
    points = []
    stop_index = {}
    marks = dict((v, k) for k, v in stop_at.items())
    for i in range(len(control) - 1):
        p0 = control[max(0, i - 1)]
        p1 = control[i]
        p2 = control[i + 1]
        p3 = control[min(len(control) - 1, i + 2)]
        if i in marks:
            stop_index[marks[i]] = len(points)
        seg = math.hypot(p2[0] - p1[0], p2[1] - p1[1])
        steps = max(2, int(seg / 2.0))
        for s in range(steps):
            points.append(catmull(p0, p1, p2, p3, s / float(steps)))
    # The last control point is a stop's stand point only when no tail follows the last stop.
    if len(control) - 1 in marks:
        stop_index[marks[len(control) - 1]] = len(points)
    points.append(control[-1])
    return points, stop_index


ROUTE, ROUTE_STOPS = build_route()


def near_route(x, y, radius, upto_x=None):
    """True if (x, y) is within radius of the road. Coarse: every fourth sample."""
    r2 = radius * radius
    for i in range(0, len(ROUTE), 2):
        px, py = ROUTE[i]
        if upto_x is not None and px > upto_x:
            continue
        dx = px - x
        dy = py - y
        if dx * dx + dy * dy <= r2:
            return True
    return False


# A coarse occupancy of the road, so per-pixel tests do not walk the whole route.
ROAD_CELL = 16
_road_cells = {}
for _i, (_x, _y) in enumerate(ROUTE):
    _road_cells.setdefault((int(_x) // ROAD_CELL, int(_y) // ROAD_CELL), []).append((_x, _y))


def road_distance(x, y):
    best = 1e9
    cx, cy = int(x) // ROAD_CELL, int(y) // ROAD_CELL
    for gy in range(cy - 2, cy + 3):
        for gx in range(cx - 2, cx + 3):
            for (px, py) in _road_cells.get((gx, gy), ()):
                d = (px - x) * (px - x) + (py - y) * (py - y)
                if d < best:
                    best = d
    return math.sqrt(best)


# ---------------------------------------------------------------------------------------------
# THE LAND. Each pixel belongs to the nearest biome seed, measured through a warp so borders
# wander. Sea is the south and east coast, by its own line.
# ---------------------------------------------------------------------------------------------
SEEDS = [
    ("forest", [(40, 300), (110, 460), (60, 640), (190, 360), (230, 270), (50, 150), (160, 190),
                (150, 600), (240, 430), (90, 770), (220, 700), (290, 200)]),
    ("fields", [(330, 650), (410, 660), (300, 720), (392, 600), (450, 720), (300, 590)]),
    ("heath", [(470, 290), (560, 320), (520, 230), (600, 370), (430, 380), (480, 470), (380, 300),
               (420, 200)]),
    ("hills", [(690, 420), (730, 520), (640, 570), (760, 450), (700, 330), (620, 470), (560, 520),
               (500, 560), (700, 230), (850, 290), (950, 240), (790, 370), (900, 410), (600, 180),
               (1010, 150)]),
    ("crag", [(836, 572), (872, 618)]),
    ("lowland", [(905, 560), (760, 700), (880, 700), (930, 480), (820, 470), (980, 520)]),
    ("swamp", [(1000, 676), (1068, 700), (940, 700), (1110, 650), (1000, 740)]),
    ("plateau", [(1066, 360), (1000, 420), (1130, 430), (990, 320), (1150, 380)]),
    ("snow", [(1206, 150), (1150, 210), (1290, 190), (1100, 130), (1240, 250), (1330, 120)]),
    ("rock", [(820, 70), (640, 60), (1000, 80), (440, 60), (320, 60), (1400, 90), (1200, 50),
              (1220, 360), (1300, 390), (1400, 350), (1460, 240), (1160, 300)]),
    ("desert", [(1372, 610), (1300, 590), (1420, 700), (1300, 700), (1240, 640), (1440, 560)]),
    ("basalt", [(1520, 450), (1580, 350), (1620, 550), (1500, 620), (1620, 250), (1560, 700),
                (1660, 400)]),
]

SEED_LIST = []
for _name, _pts in SEEDS:
    for _p in _pts:
        SEED_LIST.append((_p[0], _p[1], _name))

# THE FAR SHORE'S GROUND (.ai/second-half.md 4.2), searched only east of FAR_X0 (far_biome_pair), so the land's
# borders are exactly what they were. Chalk downland on top of the white cliffs; the seabed the tide left, south of
# the downs' scarp; the dry country of the three ways; black sand by the still river; the granite fell under the far
# north's peaks; and plain hill country between them. The Bronze Isle is drawn, not grown, as his rock is.
FAR_SEEDS = [
    ("downs", [(2260, 120), (2330, 180), (2290, 260), (2380, 250), (2340, 340), (2270, 420), (2410, 400),
               (2430, 150), (2470, 300), (2250, 330)]),
    ("flats", [(2300, 560), (2380, 620), (2460, 700), (2540, 640), (2330, 720), (2470, 560), (2580, 740),
               (2400, 780), (2280, 640)]),
    ("ways", [(2580, 360), (2650, 320), (2700, 400), (2620, 450), (2560, 290), (2690, 260), (2540, 430)]),
    ("blacksand", [(2780, 620), (2860, 660), (2920, 600), (2800, 700), (2950, 710), (2760, 560), (2880, 560)]),
    ("fell", [(2920, 230), (2990, 170), (3060, 250), (2950, 320), (3040, 330), (2880, 160), (3090, 180),
              (2860, 280)]),
    ("hills", [(2520, 200), (2510, 470), (2740, 460), (2820, 470), (3010, 450), (3080, 560), (2740, 180),
               (3020, 620), (2660, 540), (2600, 180), (3090, 680), (2680, 700)]),
    ("rock", [(2800, 60), (2960, 50), (3100, 70), (2650, 60), (2500, 60)]),
]

FAR_SEED_LIST = []
for _name, _pts in FAR_SEEDS:
    for _p in _pts:
        FAR_SEED_LIST.append((_p[0], _p[1], _name))

# The downs end in the south on a chalk scarp; below it lies the seabed the tide left (the flats). The scarp is the
# border between the two, so it is drawn on the line rather than wherever the seeds happen to meet.
DOWNS_SCARP = [(2214, 490), (2250, 494), (2290, 488), (2330, 492), (2370, 486), (2400, 482), (2428, 490),
               (2452, 502), (2476, 518), (2500, 532)]


def coast_y(x):
    """The south coast: sea below this line. The forest runs to the bottom edge in the west."""
    if x < 250:
        return 9999
    t = min(1.0, (x - 250) / 120.0)
    base = 860 - t * 120
    return base + (WARP_X.at(x * 2, 9000) - 0.5) * 40


def east_edge(y):
    """Where the land ends on row y: the Emberdeep's black cliffs, and past them the dark sea his rock
    stands in. South of the headland the old coast; north of it the cliffs stand a little further out;
    and the headland itself, where the road comes down off the summit and the Last Bridge leaves the
    land, furthest. The step where the two meet is a south-facing cliff, drawn by black_cliffs()."""
    wob = (WARP_Y.at(9000, y * 2) - 0.5) * 30
    if y > 470:
        return 1650 + wob
    head = max(0.0, 1.0 - abs(y - 450) / 36.0)
    return 1702 + wob * 0.6 + head * 40


def is_sea(x, y):
    if x >= FAR_X0:
        return not far_land(x, y)
    if y > coast_y(x):
        return True
    return x > east_edge(y)


# THE FAR SHORE'S COASTS (.ai/second-half.md 4.1): "white chalk cliffs facing the Emberdeep's black ones". The west
# coast is headlands of chalk, each reaching further west as it runs south and ending in a face where the coast
# steps back east - a face seen from the south, which is how this map shows a cliff (black_cliffs' rule). The cove
# where the causeway lands is the notch between the fourth and fifth. South of the downs the coast is the flats'
# edge, where the sea stands off; east of the flats an ordinary coast; and the east coast looks over a strait to
# the Bronze Isle.
# (the row a headland starts at, its x there, its x at its south end)
WHITE_HEADS = [(0, 2238, 2214), (110, 2248, 2206), (230, 2246, 2200), (330, 2238, 2206), (386, 2224, 2223),
               (420, 2223, 2210)]
FLATS_TOP = 490     # below this row the far shore's west edge is the standing sea's, not the cliffs'
FLATS_EAST = 2600   # east of this the south coast is an ordinary shore, not the held-off sea


def far_west_edge(y):
    if y >= FLATS_TOP:
        # The bay's mouth, where the Weir holds the sea out: an arc bowing out south-east from the downs' corner.
        t = (y - FLATS_TOP) / 310.0
        return 2236 + t * t * 120 + (WARP_Y.at(9100, y * 2) - 0.5) * 6
    for i in range(len(WHITE_HEADS) - 1, -1, -1):
        y0, xa, xb = WHITE_HEADS[i]
        if y >= y0:
            y1 = WHITE_HEADS[i + 1][0] if i + 1 < len(WHITE_HEADS) else FLATS_TOP
            t = (y - y0) / float(y1 - y0)
            return xa + (xb - xa) * t + (WARP_Y.at(9200, y * 3) - 0.5) * 5
    return WHITE_HEADS[0][1]


def far_east_edge(y):
    """The far shore's east coast: bays and points, looking over the strait to the Bronze Isle."""
    return (3104 + math.sin(y / 88.0 + 1.0) * 26 + math.sin(y / 29.0) * 9
            + (WARP_Y.at(9300, y * 2) - 0.5) * 30)


def far_south_edge(x):
    """The south coast: the flats run off the picture under the held-off sea; east of them an ordinary shore in
    bays and points, rising a little to the east."""
    flats = 860.0
    coast = (754 - (x - FLATS_EAST) * 0.04 + math.sin(x / 64.0) * 22 + math.sin(x / 23.0 + 2.0) * 6
             + (WARP_X.at(x * 2, 9500) - 0.5) * 24)
    if x < FLATS_EAST - 60:
        return flats
    if x > FLATS_EAST + 60:
        return coast
    t = (x - (FLATS_EAST - 60)) / 120.0
    return flats + (coast - flats) * t * t * (3 - 2 * t)


def far_land(x, y):
    return far_west_edge(y) < x < far_east_edge(y) and y < far_south_edge(x)


def scarp_y(x):
    """The downs' south scarp at column x (DOWNS_SCARP), or None off its ends."""
    return _y_on(DOWNS_SCARP, x)


def far_biome_pair(x, y):
    """biome_pair on the far shore's own seeds; the downs and the flats meet on the scarp, not where the seeds say."""
    b, o, m = biome_pair(x, y, FAR_SEED_LIST)
    edge = scarp_y(x)
    if edge is not None:
        if b == "downs" and y > edge:
            b, o, m = "flats", "downs", 0.0
        elif b == "flats" and y <= edge:
            b, o, m = "downs", "flats", 0.0
    return b, o, m


def biome_pair(x, y, seeds=None):
    """(nearest biome, the next nearest OTHER biome, how far toward it 0..0.5): the second lets a
    border be a dithered band rather than a ruled line between two territories."""
    wx = x + (WARP_X.at(x, y) - 0.5) * 70
    wy = y + (WARP_Y.at(x, y) - 0.5) * 70
    best = None
    bd = 1e18
    near = {}
    for sx, sy, name in (seeds or SEED_LIST):
        d = (sx - wx) * (sx - wx) + (sy - wy) * (sy - wy)
        if d < near.get(name, 1e18):
            near[name] = d
        if d < bd:
            bd = d
            best = name
    other = None
    od = 1e18
    for name, d in near.items():
        if name != best and d < od:
            od = d
            other = name
    gap = math.sqrt(od) - math.sqrt(bd)
    band = 14.0
    mix = max(0.0, 0.5 - gap / (2 * band))
    return best, other, mix


def biome_at(x, y):
    if is_sea(x, y):
        return "sea"
    return biome_pair(x, y)[0]


# ---------------------------------------------------------------------------------------------
# GROUND. Every biome has a ramp of four of its own tones, dark to light, and a pixel's place on it
# is the LIGHT on the land there: a height field, hill-shaded from the key's direction (upper left).
# Without it the regions were flat patches of colour - a map of territories, not a picture of land.
# Everything sits in the deep half of each material; the places light themselves.
# ---------------------------------------------------------------------------------------------
GROUND = {
    "forest": [OCC, T("stone", "deep"), T("lichen", "deep"), T("lichen", "shade")],
    "fields": [T("stone", "deep"), T("flesh", "deep"), T("gold", "deep"), T("gold", "shade")],
    "heath": [T("stone", "deep"), T("violet", "deep"), T("violet", "shade"), T("violet", "base")],
    "hills": [T("stone", "deep"), T("stone", "shade"), T("lichen", "deep"), T("lichen", "shade")],
    "crag": [T("stone", "deep"), T("stone", "shade"), T("stone", "base"), T("stone", "lit")],
    "lowland": [T("stone", "deep"), T("lichen", "deep"), T("linen", "deep"), T("linen", "shade")],
    "swamp": [T("stone", "deep"), T("linen", "deep"), T("linen", "shade"), T("linen", "base")],
    "plateau": [T("stone", "deep"), T("stone", "shade"), T("stone", "base"), T("skin", "deep")],
    "snow": [T("stone", "shade"), T("steel", "shade"), T("steel", "base"), T("skin", "shade")],
    "rock": [T("stone", "deep"), T("stone", "shade"), T("stone", "base"), T("stone", "lit")],
    "desert": [T("flesh", "deep"), T("gold", "deep"), T("gold", "shade"), T("gold", "base")],
    "basalt": [OCC, T("stone", "deep"), T("violet", "deep"), T("violet", "shade")],
    "sea": [T("stone", "deep"), T("wool", "deep"), T("wool", "deep"), T("wool", "shade")],
    # THE FAR SHORE (piece F5), all night and no moon. The downs are pale - grey-green turf with the chalk under
    # it - so the White Downs read white beside the hill country; the flats are wet sand the sea left; the three
    # ways' country is dry; the black sand is the darkest ground in the world but the basalt; the fell is granite.
    "downs": [T("linen", "shade"), T("skin", "deep"), T("linen", "base"), T("skin", "shade")],
    "flats": [T("stone", "deep"), T("gold", "deep"), T("linen", "shade"), T("linen", "base")],
    "ways": [T("stone", "deep"), T("gold", "deep"), T("linen", "shade"), T("gold", "shade")],
    "blacksand": [OCC, T("stone", "deep"), T("stone", "shade"), T("stone", "base")],
    "fell": [T("stone", "shade"), T("stone", "base"), T("steel", "deep"), T("steel", "shade")],
}

# base height, relief amplitude, and where on its ramp flat ground of the biome sits (0..1)
RELIEF = {
    "sea": (0.00, 0.00, 0.30),
    "swamp": (0.10, 0.03, 0.40),
    "lowland": (0.20, 0.10, 0.50),
    "fields": (0.22, 0.05, 0.50),
    "forest": (0.30, 0.14, 0.45),
    "desert": (0.26, 0.10, 0.42),
    "hills": (0.42, 0.34, 0.50),
    "heath": (0.55, 0.20, 0.50),
    "crag": (0.60, 0.25, 0.45),
    "plateau": (0.62, 0.06, 0.48),
    "basalt": (0.40, 0.18, 0.40),
    "snow": (0.72, 0.16, 0.55),
    "rock": (0.85, 0.40, 0.42),
    "downs": (0.52, 0.16, 0.50),
    "flats": (0.06, 0.02, 0.48),
    "ways": (0.36, 0.08, 0.48),
    "blacksand": (0.16, 0.05, 0.42),
    "fell": (0.70, 0.30, 0.48),
}

HILL = Fbm(53, 46, 3)
RELIEF_STEP = 4


def height_field(biomes):
    """Heights on a 4-pixel grid, blurred so a biome border is a slope rather than a step."""
    cols = W // RELIEF_STEP + 2
    rows = H // RELIEF_STEP + 2
    hgt = [0.0] * (cols * rows)
    for r in range(rows):
        for c in range(cols):
            x = min(W - 1, c * RELIEF_STEP)
            y = min(H - 1, r * RELIEF_STEP)
            base, amp, _ = RELIEF[biomes[y * W + x]]
            hgt[r * cols + c] = base + (HILL.at(x, y) - 0.5) * 2 * amp
    for _ in range(3):
        out = list(hgt)
        for r in range(1, rows - 1):
            for c in range(1, cols - 1):
                acc = 0.0
                for dr in (-1, 0, 1):
                    for dc in (-1, 0, 1):
                        acc += hgt[(r + dr) * cols + c + dc]
                out[r * cols + c] = acc / 9.0
        hgt = out
    return hgt, cols, rows


def sample(hgt, cols, rows, x, y):
    fx = x / float(RELIEF_STEP)
    fy = y / float(RELIEF_STEP)
    ix = min(cols - 2, max(0, int(fx)))
    iy = min(rows - 2, max(0, int(fy)))
    tx = fx - ix
    ty = fy - iy
    a = hgt[iy * cols + ix]
    b = hgt[iy * cols + ix + 1]
    c = hgt[(iy + 1) * cols + ix]
    d = hgt[(iy + 1) * cols + ix + 1]
    top = a + (b - a) * tx
    return top + ((c + (d - c) * tx) - top) * ty


def paint_ground(g, biomes, others, mixes):
    hgt, cols, rows = height_field(biomes)
    for y in range(H):
        row = y * W
        for x in range(X_FROM, W):
            b = biomes[row + x]
            # Near a border, some pixels take the neighbour's ramp: a dithered band, not a line.
            if mixes[row + x] > 0 and others[row + x] is not None and dith(x + 3, y + 5, mixes[row + x]):
                b = others[row + x]
            tones = GROUND[b]
            flat = RELIEF[b][2]
            if b == "sea":
                light = flat + (BROAD.at(x, y) - 0.5) * 0.3
                # The dark sea: past the Emberdeep's cliffs the water goes black toward his rock.
                if x > 1640:
                    light -= 0.26 * min(1.0, (x - 1640) / 220.0)
            else:
                # The key comes from the upper left: ground that faces it is lit.
                slope = sample(hgt, cols, rows, x - 3, y - 3) - sample(hgt, cols, rows, x + 3, y + 3)
                light = flat + slope * 5.0 + (BROAD.at(x, y) - 0.5) * 0.25 + (GRAIN.at(x, y) - 0.5) * 0.22
            t = max(0.0, min(2.999, light * 3.0))
            i = int(t)
            c = tones[i + 1] if dith(x, y, t - i) else tones[i]
            if b == "sea" and GRAIN.at(x, y) > (0.68 if x < 1700 else 0.74) and (x + int(y * 1.7)) % 9 == 0:
                c = tones[3] if x < 1700 or dith(x, y, 0.5) else tones[1]
            g.pix[row + x] = rgba(c)


def shore(g, biomes):
    """A dark line where land meets sea, and a paler lip on the land side lit from above."""
    for y in range(1, H - 1):
        for x in range(max(1, X_FROM), W - 1):
            if biomes[y * W + x] != "sea":
                continue
            up = biomes[(y - 1) * W + x]
            left = biomes[y * W + x - 1]
            if up != "sea" or left != "sea":
                g.set(x, y, OCC)
                if up != "sea":
                    g.set(x, y - 1, T("stone", "base") if up not in ("snow",) else T("skin", "base"))


# ---------------------------------------------------------------------------------------------
# WATER: the heath's stream into the cave, the river out of the hills past the crag into the bog.
# ---------------------------------------------------------------------------------------------
STREAM = [(575, 270), (600, 292), (628, 330), (650, 372), (668, 410), (682, 436), (689, 452)]
# Out of the hills below the scarp - the cave's water coming back up - west of the crag,
# under the road's one bridge, and out across the bog to the sea.
RIVER = [(758, 470), (766, 510), (772, 552), (774, 592), (778, 628), (790, 662), (826, 684),
         (880, 692), (930, 700), (968, 712), (996, 734), (1012, 772), (1020, 840)]
DRY_RIVER = [(1250, 520), (1290, 560), (1330, 600), (1350, 650), (1336, 700), (1350, 760)]
WATER = T("wool", "deep")
WATER_LIT = T("wool", "shade")


def spline(pts, step=1.0):
    out = []
    for i in range(len(pts) - 1):
        p0 = pts[max(0, i - 1)]
        p1 = pts[i]
        p2 = pts[i + 1]
        p3 = pts[min(len(pts) - 1, i + 2)]
        seg = math.hypot(p2[0] - p1[0], p2[1] - p1[1])
        n = max(2, int(seg / step))
        for s in range(n):
            out.append(catmull(p0, p1, p2, p3, s / float(n)))
    out.append(pts[-1])
    return out


def draw_water_path(g, pts, w0, w1, water=WATER, lit=WATER_LIT, bank=OCC):
    path = spline(pts)
    n = len(path)
    # Banks first, then water over them, so the bank is a rim and never inside the stream.
    for pass_ in (0, 1):
        for i, (x, y) in enumerate(path):
            w = w0 + (w1 - w0) * i / float(max(1, n - 1))
            r = w / 2.0 + (1.0 if pass_ == 0 else 0.0)
            for yy in range(int(y - r) - 1, int(y + r) + 2):
                for xx in range(int(x - r) - 1, int(x + r) + 2):
                    if (xx - x) ** 2 + (yy - y) ** 2 <= r * r:
                        if pass_ == 0:
                            g.set(xx, yy + 1, bank)
                        else:
                            c = lit if ((xx * 3 + yy * 5 + i) % 11 == 0) else water
                            g.set(xx, yy, c)
    return path


def lake(g, cx, cy, rx, ry, seed):
    r = rng_for(seed)
    g.disc(cx + 1, cy + 2, rx + 1, ry + 1, OCC)
    g.disc(cx, cy, rx, ry, WATER)
    for _ in range(int(rx * ry / 12) + 2):
        x = cx + (r.random() - 0.5) * rx * 1.4
        y = cy + (r.random() - 0.5) * ry * 1.2
        g.set(x, y, WATER_LIT)
        g.set(x + 1, y, WATER_LIT)


# ---------------------------------------------------------------------------------------------
# THE ROAD. Drawn after the land and the water, before anything that stands on it.
# ---------------------------------------------------------------------------------------------
def draw_road(g, biomes):
    on_map = [p for p in ROUTE if p[0] < W - 2]
    for (x, y) in on_map:
        for yy in range(int(y) - 3, int(y) + 4):
            for xx in range(int(x) - 3, int(x) + 4):
                d = math.hypot(xx - x, yy - y)
                if d > 2.6:
                    continue
                cur = g.get(xx, yy)
                b = biomes[min(H - 1, max(0, yy)) * W + min(W - 1, max(0, xx))]
                if cur == rgba(WATER) or cur == rgba(WATER_LIT):
                    # A bridge: planks across, lit on their top.
                    g.set(xx, yy, T("gold", "shade") if (xx + yy) % 3 else T("gold", "deep"))
                    continue
                bright = b in ("snow",)
                # On snow the road is the trodden dark of it, not a pale line on a pale field.
                core = T("stone", "base") if bright else T("linen", "base")
                edge = T("steel", "shade") if bright else T("linen", "shade")
                if b == "downs":
                    # On the downs a chalk track: the turf worn through to the white under it, as such roads are.
                    core, edge = T("skin", "shade"), T("linen", "shade")
                    if d <= 0.7 and dith(xx, yy, 0.4):
                        g.set(xx, yy, T("skin", "base"))
                        continue
                if d <= 0.7 and not bright and dith(xx, yy, 0.45):
                    g.set(xx, yy, T("linen", "lit"))
                elif d <= 1.5:
                    g.set(xx, yy, core)
                else:
                    g.set(xx, yy, edge)
    # The lit lip on the road's upper-left side and its shadow on the lower-right, after the fill.
    road_tones = set(rgba(c) for c in (T("linen", "lit"), T("linen", "base"), T("linen", "shade"),
                                       T("steel", "shade"), T("stone", "base"), T("gold", "shade"),
                                       T("gold", "deep")))
    chalk = set(rgba(c) for c in (T("skin", "shade"), T("skin", "base")))
    for (x, y) in on_map:
        sx, sy = int(round(x + 1.8)), int(round(y + 3.0))
        cur = g.get(sx, sy)
        if cur in chalk and biomes[min(H - 1, sy) * W + min(W - 1, sx)] == "downs" and road_distance(sx, sy) < 2.7:
            continue
        if cur not in road_tones and lum(cur) > lum(T("stone", "deep")):
            g.set(sx, sy, T("stone", "deep"))
    # Dust in the ruts: a lit fleck now and then, never a dashed line.
    r = rng_for(77)
    for (x, y) in on_map[::3]:
        if r.random() < 0.18:
            c = g.get(x, y)
            if c == rgba(T("linen", "base")):
                g.set(x, y, T("linen", "lit"))


# ---------------------------------------------------------------------------------------------
# SMALL FURNITURE: trees, bushes, stones. Lit from the upper left, contact shadow lower right.
# ---------------------------------------------------------------------------------------------
_LOBE_RNG = rng_for(4242)


def crown(g, cx, cy, r, mat="lichen", dark=False):
    """A tree from above and a little south: a crown of clumped lobes, each lit on its own upper
    left, so a wood reads as foliage. One smooth disc per tree read as a ball pit."""
    ramp = bl.MATERIALS[mat]
    rnd = _LOBE_RNG
    lobes = [(cx, cy, r * 0.62)]
    n = 3 + int(rnd.random() * 3)
    for k in range(n):
        a = rnd.random() * 2 * math.pi
        d = r * (0.35 + rnd.random() * 0.25)
        lobes.append((cx + math.cos(a) * d, cy + math.sin(a) * d * 0.85, r * (0.4 + rnd.random() * 0.22)))
    for (lx, ly, lr) in lobes:
        g.disc(lx + 2, ly + 2, lr, lr * 0.9, OCC)
    lobes.sort(key=lambda l: l[1] + l[0] * 0.3)
    body = ramp[4] if dark else ramp[3]
    mid = ramp[3] if dark else ramp[2]
    top = ramp[2] if dark else ramp[1]
    for (lx, ly, lr) in lobes:
        for yy in range(int(ly - lr) - 1, int(ly + lr) + 2):
            for xx in range(int(lx - lr) - 1, int(lx + lr) + 2):
                nx = (xx - lx) / lr
                ny = (yy - ly) / (lr * 0.9)
                d = nx * nx + ny * ny
                if d > 1.0:
                    continue
                k = -(nx + ny) * 0.75 + (GRAIN.at(xx * 5, yy * 5) - 0.5) * 0.7
                c = body
                if k > 0.15:
                    c = mid if dith(xx, yy, 0.85) else body
                if k > 0.75 and d > 0.15:
                    c = top if dith(xx, yy, 0.7) else mid
                if d > 0.72 and (nx + ny) > 0.4:
                    c = ramp[4]
                g.set(xx, yy, c)


def bare_tree(g, x, y, h, seed, mat="stone"):
    r = rng_for(seed)
    g.line(x + 1, y + 1, x + 1 + h // 3, y + 1, OCC)
    g.line(x, y, x, y - h, T(mat, "shade"))
    g.set(x - 1, y - h // 2, T(mat, "base"))
    for k in range(3):
        by = y - h + k * (h // 4) + 1
        side = -1 if r.random() < 0.5 else 1
        ln = 2 + int(r.random() * 3)
        g.line(x, by, x + side * ln, by - ln // 2 - 1, T(mat, "shade"))


def standing_stone(g, x, y, h, w=3):
    """A granite slab, pale under the moon: lit left face, body, shaded right edge, a dark foot and
    occlusion on the ground lower right. Drawn in stone it vanished into the heather."""
    g.rect(x + 1, y + 1, x + w + 2, y + 2, OCC)
    g.rect(x, y - h, x + w - 1, y, T("skin", "shade"))
    g.rect(x, y - h, x, y, T("skin", "base"))
    g.rect(x + w - 1, y - h + 1, x + w - 1, y, T("skin", "deep"))
    g.rect(x + 1, y - 2, x + w - 1, y, T("skin", "deep"))
    g.set(x, y - h, T("skin", "lit"))
    g.set(x + 1, y - h, T("skin", "base"))
    g.set(x + w - 1, y - h, T("skin", "shade"))
    g.set(x + 1, y - h // 2, T("lichen", "shade"))


def bush(g, x, y, r, mat="lichen"):
    g.disc(x + 1, y + 1, r, r * 0.7, OCC)
    g.disc(x, y, r, r * 0.7, T(mat, "shade"))
    g.set(x - 1, y - 1, T(mat, "base"))
    if r > 1.5:
        g.set(x - 1, y - 2, T(mat, "base"))
        g.set(x, y - 1, T(mat, "base"))


def _ridge(r, ax, ay, bx, by, rough, depth=4):
    """Midpoint displacement between two points: a jagged ridge line, as a list of points. The
    displacement is sideways only, so the line stays monotone in y and a row crosses it once."""
    pts = [(ax, ay), (bx, by)]
    amp = rough
    for _ in range(depth):
        out = [pts[0]]
        for i in range(len(pts) - 1):
            (x0, y0), (x1, y1) = pts[i], pts[i + 1]
            out.append(((x0 + x1) / 2.0 + (r.random() - 0.5) * amp, (y0 + y1) / 2.0))
            out.append((x1, y1))
        pts = out
        amp *= 0.55
    return pts


def _x_at(pts, y):
    """Where a monotone-in-y polyline crosses row y."""
    for i in range(len(pts) - 1):
        (x0, y0), (x1, y1) = pts[i], pts[i + 1]
        if min(y0, y1) <= y <= max(y0, y1) and y0 != y1:
            return x0 + (y - y0) * (x1 - x0) / (y1 - y0)
    return None


def peak(g, cx, by, w, h, snow=0.35, seed=0):
    """A mountain from the south, as overworld maps draw them: a jagged ridge on each side of the
    summit, the face toward the key lit and the other in shade, split by a crooked spine, gullies
    on the lit face, a snow cap with a ragged foot, contact dark where it meets the ground."""
    r = rng_for(seed)
    tx = cx + (r.random() - 0.5) * w * 0.25
    ty = by - h
    lx = cx - w / 2.0
    rx = cx + w / 2.0
    left = _ridge(r, tx, ty, lx, by, h * 0.22)
    right = _ridge(r, tx, ty, rx, by, h * 0.22)
    spine = _ridge(r, tx, ty, tx + w * 0.14, by, w * 0.18, 3)
    gullies = [_ridge(r, tx - w * (0.08 + 0.1 * k), ty + h * (0.25 + 0.12 * k), tx - w * (0.2 + 0.08 * k), by,
                      w * 0.12, 3) for k in range(2)]
    for y in range(int(ty), int(by) + 1):
        xl = _x_at(left, y)
        xr = _x_at(right, y)
        xs = _x_at(spine, y)
        if xl is None or xr is None:
            continue
        if xs is None:
            xs = (xl + xr) / 2.0
        t = (y - ty) / float(max(1, by - ty))
        gx = [_x_at(gl, y) for gl in gullies]
        for x in range(int(math.floor(xl)), int(math.ceil(xr)) + 1):
            lit = x < xs
            cap = snow + (GRAIN.at(x * 6, y * 2) - 0.5) * 0.22
            if t < cap:
                c = (T("skin", "lit") if dith(x, y, 0.5 - t) else T("skin", "base")) if lit else T("skin", "shade")
                if not lit and x > xs + 2 and dith(x, y, 0.4):
                    c = T("steel", "shade")
            elif lit:
                c = T("stone", "lit") if dith(x, y, 0.5 - t * 0.6) else T("stone", "base")
                if x <= xl + 1.2:
                    c = T("stone", "lit")
                for gxx in gx:
                    if gxx is not None and abs(x - gxx) < 0.8:
                        c = T("stone", "shade")
            else:
                c = T("stone", "shade") if dith(x, y, 0.75 - t * 0.5) else T("stone", "deep")
                if x >= xr - 1.2 or t > 0.88:
                    c = OCC
            g.set(x, y, c)
    for (x, y) in right[len(right) // 2:]:
        g.set(x + 1, y + 1, OCC)


# ---------------------------------------------------------------------------------------------
# THE PLACES' LANDMARKS
# ---------------------------------------------------------------------------------------------
def scatter_forest(g, biomes):
    """The wood: crowns packed back to front, kept off the road. Elderbark's clearing at the focus."""
    r = rng_for(101)
    trees = []
    for _ in range(2600):
        x = r.random() * 330
        y = r.random() * H
        if x >= W or y >= H:
            continue
        if biomes[int(y) * W + int(x)] != "forest":
            continue
        if road_distance(x, y) < 7:
            continue
        if math.hypot(x - 150, (y - 430) * 1.3) < 34:
            continue
        trees.append((y, x, 4 + r.random() * 4.5))
    # A few outliers into the fields and lowland: hedgerow trees.
    for _ in range(260):
        x = r.random() * LAND_W
        y = r.random() * H
        b = biomes[int(y) * W + int(x)]
        if b not in ("lowland", "hills", "fields") or road_distance(x, y) < 8:
            continue
        if r.random() < (0.25 if b == "fields" else 0.6):
            trees.append((y, x, 3 + r.random() * 2.5))
    trees.sort()
    for y, x, rad in trees:
        # Mostly the dark crown: the wood is his, and a canopy of lit crowns read as bubbles.
        dark = x < 90 or r.random() < 0.6
        crown(g, x, y, rad, "lichen", dark=dark)


def elderbark(g):
    """Elderbark: one crown far larger than the wood, gnarled, darker, and his mark in it."""
    cx, cy = 150, 418
    # Roots in the clearing.
    for k in range(7):
        a = k / 7.0 * 2 * math.pi
        g.line(cx, cy + 14, cx + math.cos(a) * 30, cy + 16 + math.sin(a) * 14, T("stone", "shade"))
    g.rect(cx - 3, cy + 2, cx + 3, cy + 16, T("stone", "shade"))
    g.rect(cx - 3, cy + 2, cx - 2, cy + 16, T("stone", "base"))
    for i, (dx, dy, rr) in enumerate([(-12, -4, 15), (12, -2, 14), (0, -14, 16), (-2, 4, 15), (10, -16, 10),
                                       (-14, -16, 10)]):
        crown(g, cx + dx, cy + dy, rr, "lichen", dark=(i % 2 == 0))
    # Two points of his mark in the dark of the crown.
    g.set(cx - 3, cy - 4, T("arcane", "lit"))
    g.set(cx + 3, cy - 4, T("arcane", "lit"))


def fields(g, biomes):
    """Hedged fields, furrows, stooks, a millpond, the well, an empty farmhouse."""
    r = rng_for(202)
    # Field plots on a loose grid, each its own tone and furrow direction.
    for gy in range(560, 840, 30):
        for gx in range(250, 520, 38):
            x0 = gx + int((r.random() - 0.5) * 10)
            y0 = gy + int((r.random() - 0.5) * 8)
            x1 = x0 + 30 + int(r.random() * 12)
            y1 = y0 + 22 + int(r.random() * 8)
            pick = r.random()
            # Mostly dark: a harvested field under no light (campaign.md, "gold and linen from the
            # deep half"). One plot in five is stubble still catching a little light.
            tone = (T("gold", "shade") if pick < 0.2 else T("gold", "deep") if pick < 0.6
                    else T("flesh", "deep") if pick < 0.85 else T("linen", "deep"))
            furrow = T("flesh", "deep") if tone != T("flesh", "deep") else T("stone", "deep")
            vertical = r.random() < 0.5
            for y in range(y0, y1):
                for x in range(x0, x1):
                    if not (0 <= x < W and 0 <= y < H) or biomes[y * W + x] != "fields":
                        continue
                    if road_distance(x, y) < 4:
                        continue
                    on = (x if vertical else y) % 3 == 0
                    g.set(x, y, furrow if on and dith(x, y, 0.8) else tone)
            # Hedgerows along the bottom and right edges: a dark line with a lit top, and a tree now
            # and then, never a row of evenly spaced dots.
            for x in range(x0, x1 + 1):
                if 0 <= x < W and 0 <= y1 < H and biomes[y1 * W + x] == "fields" and road_distance(x, y1) > 5:
                    g.set(x, y1 + 1, OCC)
                    g.set(x, y1, T("lichen", "deep"))
                    g.set(x, y1 - 1, T("lichen", "shade") if dith(x, y1, 0.5) else T("lichen", "deep"))
            for y in range(y0, y1 + 1):
                if 0 <= x1 < W and 0 <= y < H and biomes[y * W + x1] == "fields" and road_distance(x1, y) > 5:
                    g.set(x1 + 1, y, OCC)
                    g.set(x1, y, T("lichen", "deep"))
                    g.set(x1 - 1, y, T("lichen", "shade") if dith(x1, y, 0.4) else T("lichen", "deep"))
            if r.random() < 0.5:
                tx = x0 + int(r.random() * (x1 - x0))
                if 0 <= tx < W and 0 <= y1 < H and biomes[y1 * W + tx] == "fields" and road_distance(tx, y1) > 7:
                    crown(g, tx, y1 - 2, 3 + r.random() * 1.5, "lichen")
            # Stooks in a stubble field.
            if tone == T("gold", "shade") and r.random() < 0.6:
                for k in range(4):
                    sx = x0 + 5 + int(r.random() * (x1 - x0 - 10))
                    sy = y0 + 5 + int(r.random() * (y1 - y0 - 10))
                    if road_distance(sx, sy) > 6 and biomes[min(H - 1, sy) * W + min(W - 1, sx)] == "fields":
                        g.set(sx + 1, sy + 1, OCC)
                        g.set(sx, sy, T("gold", "base"))
                        g.set(sx, sy - 1, T("gold", "lit"))
                        g.set(sx + 1, sy, T("gold", "shade"))
    # The millpond.
    lake(g, 300, 668, 17, 9, 203)
    # The farmstead, empty: a yard, the house and a barn, dark windows, a gate left open.
    yx, yy = 300, 604
    for y in range(yy, yy + 36):
        for x in range(yx, yx + 54):
            if road_distance(x, y) > 5:
                g.set(x, y, T("flesh", "deep") if dith(x, y, 0.5) else T("gold", "deep"))
    for x in range(yx, yx + 54, 2):
        g.set(x, yy, T("linen", "shade"))
        g.set(x, yy + 35, T("linen", "shade"))
    for y in range(yy, yy + 36, 2):
        g.set(yx, y, T("linen", "shade"))

    def house(hx, hy, w, h, roof):
        g.rect(hx + 2, hy + 2, hx + w + 2, hy + h + 2, OCC)
        g.rect(hx, hy, hx + w, hy + h, T("linen", "base"))
        g.rect(hx, hy, hx + 1, hy + h, T("linen", "lit"))
        g.rect(hx + w, hy, hx + w, hy + h, T("linen", "shade"))
        # Roof: a pitched gable, lit on the near slope.
        peak_h = h * 0.9
        g.poly([(hx - 2, hy + 1), (hx + w / 2.0, hy - peak_h), (hx + w + 2, hy + 1)], T(roof, "shade"))
        g.poly([(hx - 2, hy + 1), (hx + w / 2.0, hy - peak_h), (hx + w / 2.0, hy + 1)], T(roof, "base"))
        g.line(hx - 2, hy + 1, hx + w / 2.0, hy - peak_h, T(roof, "hi"))
        for wx in range(hx + 3, hx + w - 1, 5):
            g.rect(wx, hy + 3, wx + 1, hy + 4, OCC)
        g.rect(hx + w // 2 - 1, hy + h - 4, hx + w // 2 + 1, hy + h, OCC)

    house(306, 616, 20, 11, "red")
    house(334, 622, 14, 9, "linen")
    # The well, in the middle of the fields: a stone ring round a dark shaft, a roof on two posts.
    wx, wy = 362, 652
    g.disc(wx + 1, wy + 2, 10, 5, OCC)
    g.disc(wx, wy, 9, 5, T("stone", "base"))
    g.disc(wx, wy, 6, 3, OCC)
    for k in range(-9, 0):
        g.set(wx + k, wy - int(math.sqrt(max(0, 1 - (k / 9.0) ** 2)) * 5), T("stone", "lit"))
    g.line(wx - 7, wy, wx - 7, wy - 12, T("linen", "shade"))
    g.line(wx + 7, wy, wx + 7, wy - 12, T("linen", "deep"))
    g.poly([(wx - 10, wy - 11), (wx, wy - 17), (wx + 10, wy - 11)], T("red", "shade"))
    g.poly([(wx - 10, wy - 11), (wx, wy - 17), (wx, wy - 11)], T("red", "base"))
    g.line(wx - 10, wy - 11, wx, wy - 17, T("red", "hi"))
    g.line(wx - 5, wy - 9, wx + 5, wy - 9, T("linen", "deep"))


def broken_circle(g, biomes):
    """The heath: heather tufts, granite, the tarn, eight stones and a gap where the ninth stood."""
    r = rng_for(303)
    for _ in range(1600):
        x = 360 + r.random() * 340
        y = 180 + r.random() * 360
        if biomes[int(y) * W + int(x)] != "heath" or road_distance(x, y) < 5:
            continue
        g.set(x, y, T("violet", "base") if r.random() < 0.7 else T("violet", "lit"))
        g.set(x + 1, y + 1, T("violet", "deep"))
    for _ in range(50):
        x = 380 + r.random() * 300
        y = 200 + r.random() * 320
        if biomes[int(y) * W + int(x)] != "heath" or road_distance(x, y) < 8:
            continue
        rr = 2 + r.random() * 3
        g.disc(x + 1, y + 1, rr, rr * 0.6, OCC)
        g.disc(x, y, rr, rr * 0.6, T("stone", "base"))
        g.set(x - 1, y - 1, T("stone", "lit"))
    lake(g, 575, 268, 15, 8, 304)
    cx, cy = 520, 300
    # The moot's floor: the turf inside the ring is worn.
    g.disc(cx, cy, 34, 18, T("violet", "deep"))
    g.disc(cx, cy, 30, 16, T("violet", "shade"))
    for yy in range(cy - 16, cy + 17):
        for xx in range(cx - 30, cx + 31):
            if ((xx - cx) / 30.0) ** 2 + ((yy - cy) / 16.0) ** 2 <= 1 and dith(xx, yy, 0.3):
                g.set(xx, yy, T("violet", "deep"))
    g.disc(cx, cy, 7, 4, T("stone", "shade"))
    g.disc(cx, cy - 1, 6, 3, T("stone", "base"))
    g.set(cx - 3, cy - 2, T("stone", "lit"))
    stones = []
    for k in range(9):
        if k == 2:
            continue  # the ninth stone is gone - it walks
        a = -math.pi / 2 + k / 9.0 * 2 * math.pi
        stones.append((cy + math.sin(a) * 16, cx + math.cos(a) * 30))
    stones.sort()
    for sy, sx in stones:
        standing_stone(g, int(sx) - 2, int(sy) + 4, 11, 4)
    # A fallen stone where the gap is.
    a = -math.pi / 2 + 2 / 9.0 * 2 * math.pi
    fx, fy = cx + math.cos(a) * 42, cy + math.sin(a) * 22 + 6
    g.rect(fx, fy + 1, fx + 8, fy + 1, OCC)
    g.rect(fx, fy - 2, fx + 7, fy, T("stone", "base"))
    g.rect(fx, fy - 2, fx + 7, fy - 2, T("stone", "lit"))


def cave(g):
    """The scarp the heath ends on, and the cave mouth the stream goes into."""
    # The scarp: where the heath ends, a rock face drops to the cave country. Its top is a ragged
    # lip, its face is vertical rock - striated, lit where it turns toward the key - and its foot
    # is in shadow. A face, not a band: its height varies along it.
    r = rng_for(321)
    lip = spline([(600, 382), (626, 404), (648, 424), (668, 436), (700, 440), (728, 434), (752, 422),
                  (772, 410)], 1.0)
    cols = {}
    for (x, y) in lip:
        xi = int(round(x))
        if xi not in cols or y < cols[xi]:
            cols[xi] = y
    for xi in sorted(cols):
        top = cols[xi] + (GRAIN.at(xi * 6, 50) - 0.5) * 3
        height = 16 + (GRAIN.at(xi * 2, 90) - 0.5) * 14
        for k in range(int(height) + 1):
            y = top + k
            t = k / height
            stria = (xi * 7 + int(GRAIN.at(xi * 9, y * 0.5) * 9)) % 5 == 0
            if t < 0.15:
                c = T("stone", "lit")
            elif stria:
                c = T("stone", "deep")
            elif t < 0.55:
                c = T("stone", "base") if dith(xi, y, 0.65 - t) else T("stone", "shade")
            else:
                c = T("stone", "shade") if dith(xi, y, 1.0 - t) else T("stone", "deep")
            g.set(xi, y, c)
        g.set(xi, top + height + 1, OCC)
        g.set(xi, top + height + 2, OCC if dith(xi, 0, 0.5) else T("stone", "deep"))
        g.set(xi, top - 1, T("stone", "base"))
    # Scree at the foot.
    for _ in range(60):
        x = 610 + r.random() * 160
        y0 = cols.get(int(x), 430) + 18 + r.random() * 12
        g.set(x, y0, T("stone", "base"))
        g.set(x + 1, y0 + 1, OCC)
    # The mouth: an arch cut into the face, dark all the way in, its lit rim on the upper left.
    mx, my = 690, 452
    for yy in range(my - 12, my + 10):
        for xx in range(mx - 13, mx + 14):
            nx = (xx - mx) / 13.0
            ny = (yy - my) / 12.0
            if ny > 0.7:
                ny = 0.7
            d = nx * nx + ny * ny
            if d <= 1.0:
                g.set(xx, yy, T("stone", "base") if (nx + ny) < -0.4 else T("stone", "shade"))
            if d <= 0.62 and yy >= my - 8:
                g.set(xx, yy, OCC)
    for k in range(-10, 1):
        g.set(mx + k, my - 9 + (k * k) // 14, T("stone", "lit"))
    # Drips of the stream going in.
    g.set(mx - 1, my + 3, WATER_LIT)
    g.set(mx + 1, my + 5, WATER_LIT)
    # The stream falls over the lip on its way in, so it is drawn again over the cliff face.
    draw_water_path(g, STREAM[2:], 2, 3)


def tower(g, tx, ty, w, h):
    """A square tower seen from the south: lit left face, shaded right, crenels, a slit window."""
    g.rect(tx + 1, ty + 1, tx + w + 1, ty + h + 1, OCC)
    g.rect(tx, ty, tx + w - 1, ty + h, T("stone", "base"))
    g.rect(tx, ty, tx, ty + h, T("stone", "lit"))
    g.rect(tx + w - 1, ty + 1, tx + w - 1, ty + h, T("stone", "shade"))
    g.rect(tx + 1, ty + h - 2, tx + w - 1, ty + h, T("stone", "shade"))
    for x in range(tx, tx + w, 2):
        g.set(x, ty - 1, T("stone", "lit") if x < tx + w // 2 else T("stone", "base"))
    g.set(tx + w // 2, ty + h // 3, OCC)
    g.set(tx + w // 2, ty + h // 3 + 1, OCC)


def crag(g):
    """The crag where the river comes out of the hills. The Cursed Dungeon's keep stood on it until the
    chapter moved into the castle (piece C2, .ai/endgame-castle.md 8): the keep came off, the rock stays,
    and the road passes it to the river's bridge, which stands unguarded - the river still has to be
    crossed. His jail is drawn under his own walls now (castle())."""
    cx, cy = 834, 580
    # A knoll of bare rock with a ragged edge that frays into the hill ground round it - not a disc:
    # with the keep gone, a clean outline read as something set down on the land.
    for yy in range(cy - 34, cy + 36):
        for xx in range(cx - 60, cx + 60):
            dx = (xx - cx) / 50.0
            dy = (yy - cy) / 28.0
            d = math.hypot(dx, dy)
            ang = math.atan2(dy, dx)
            edge = 0.72 + 0.42 * GRAIN.at(500 + math.cos(ang) * 46, 500 + math.sin(ang) * 46)
            if d > edge:
                continue
            t = d / edge
            if t > 0.78 and not dith(xx, yy, (1.0 - t) * 4.5):
                continue
            # The river and the road keep their own pixels: the knoll stands beside them, not over them.
            if g.get(xx, yy) in (rgba(WATER), rgba(WATER_LIT)) or road_distance(xx, yy) < 3.6:
                continue
            k = -(dx + dy) * 1.1 + (GRAIN.at(xx * 4, yy * 4) - 0.5) * 0.9 + (1.0 - t) * 0.3
            if k > 0.75:
                c = T("stone", "lit") if dith(xx, yy, 0.45) else T("stone", "base")
            elif k > 0.1:
                c = T("stone", "base") if dith(xx, yy, 0.3 + k * 0.5) else T("stone", "shade")
            elif k > -0.6:
                c = T("stone", "shade") if dith(xx, yy, 0.6 + k * 0.4) else T("stone", "deep")
            else:
                c = T("stone", "deep") if dith(xx, yy, 0.6) else OCC
            # Fissures in the rock.
            if (xx * 3 + yy * 7) % 23 == 0 and GRAIN.at(xx * 7, yy * 7) > 0.55:
                c = OCC
                g.set(xx + 1, yy + 1, OCC)
            g.set(xx, yy, c)
    # Where the keep stood, a tor: weathered blocks of the crag's own rock, the biggest at the back,
    # each lit on its upper left with its shadow to the lower right. Nothing built.
    for (bx, by, rw, rh) in ((cx - 6, cy - 14, 12, 8), (cx + 10, cy - 6, 8, 6), (cx - 18, cy - 2, 7, 5),
                             (cx + 2, cy + 2, 5, 4)):
        g.disc(bx + 2, by + 2, rw, rh, OCC)
        g.disc(bx, by, rw, rh, T("stone", "shade"))
        for yy in range(int(by - rh), int(by + rh) + 1):
            for xx in range(int(bx - rw), int(bx + rw) + 1):
                nx = (xx - bx) / float(rw)
                ny = (yy - by) / float(rh)
                d = nx * nx + ny * ny
                if d > 1.0:
                    continue
                k = -(nx + ny * 1.2)
                if k > 0.35:
                    g.set(xx, yy, T("stone", "lit") if k > 0.95 and dith(xx, yy, 0.6) else T("stone", "base"))
                elif k < -0.8:
                    g.set(xx, yy, T("stone", "deep"))
        # A crack down the block's face.
        g.line(bx + rw // 3, by - rh // 2, bx + rw // 3 + 1, by + rh // 2, T("stone", "deep"))


def swamp(g, biomes):
    """The delta bog: black pools, reeds, drowned trees, and his lanterns in the reeds."""
    r = rng_for(404)
    for _ in range(80):
        x = 880 + r.random() * 260
        y = 600 + r.random() * 200
        if biomes[int(y) * W + int(x)] != "swamp" or road_distance(x, y) < 10:
            continue
        # A pool is a few ellipses run together, black water with a skin of the sky on it - never
        # one clean ellipse, which reads as a coin.
        parts = []
        for k in range(2 + int(r.random() * 3)):
            parts.append((x + (r.random() - 0.5) * 14, y + (r.random() - 0.5) * 6,
                          3 + r.random() * 7, 1.5 + r.random() * 3))
        for (px, py, prx, pry) in parts:
            g.disc(px, py + 1, prx, pry, OCC)
        for (px, py, prx, pry) in parts:
            g.disc(px, py, prx, pry, T("stone", "deep"))
        for (px, py, prx, pry) in parts:
            for yy in range(int(py - pry), int(py + pry) + 1):
                for xx in range(int(px - prx), int(px + prx) + 1):
                    nx = (xx - px) / prx
                    ny = (yy - py) / pry
                    if nx * nx + ny * ny < 0.55 and dith(xx, yy, 0.5):
                        g.set(xx, yy, WATER)
            g.set(px - prx * 0.4, py - pry * 0.3, WATER_LIT)
    for _ in range(900):
        x = 880 + r.random() * 260
        y = 580 + r.random() * 220
        if biomes[int(y) * W + int(x)] != "swamp" or road_distance(x, y) < 4:
            continue
        if g.get(x, y) == rgba(WATER):
            continue
        g.set(x, y, T("linen", "base"))
        g.set(x, y - 1, T("linen", "lit") if r.random() < 0.3 else T("linen", "base"))
        g.set(x + 1, y + 1, T("linen", "deep"))
    for _ in range(14):
        x = 900 + r.random() * 230
        y = 600 + r.random() * 160
        if biomes[int(y) * W + int(x)] == "swamp" and road_distance(x, y) > 8:
            bare_tree(g, int(x), int(y), 6 + int(r.random() * 5), int(r.random() * 1e6), "linen")
    # The lanterns: cold light, his. A point, a cross of its own colour, and a little lit ground.
    for (lx, ly) in [(966, 650), (1012, 640), (1046, 690), (990, 700), (1080, 664), (936, 676),
                     (1024, 724)]:
        g.set(lx, ly + 1, OCC)
        g.line(lx, ly, lx, ly - 3, T("stone", "shade"))
        g.set(lx, ly - 4, T("arcane", "hi"))
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            g.set(lx + dx, ly - 4 + dy, T("arcane", "lit"))
        for dx, dy in ((-2, 0), (2, 0), (0, -2)):
            g.set(lx + dx, ly - 4 + dy, T("arcane", "base"))


def ruins(g, biomes):
    """The plateau: a broken ring wall, spires snapped off, fallen columns, the old ward's lines."""
    cx, cy = 1066, 362
    r = rng_for(505)
    # The ward circle cut in the ground, broken.
    for k in range(120):
        a = k / 120.0 * 2 * math.pi
        if 1.1 < a < 1.7 or 3.9 < a < 4.4:
            continue
        g.set(cx + math.cos(a) * 34, cy + math.sin(a) * 18, T("gold", "shade"))
        g.set(cx + math.cos(a) * 30, cy + math.sin(a) * 16, T("gold", "deep"))
    # Rubble.
    for _ in range(260):
        x = cx + (r.random() - 0.5) * 120
        y = cy + (r.random() - 0.5) * 80
        if biomes[int(y) * W + int(x)] != "plateau" or road_distance(x, y) < 5:
            continue
        g.set(x, y, T("skin", "deep"))
        if r.random() < 0.3:
            g.set(x - 1, y, T("skin", "shade"))
    # The ring wall, in broken arcs.
    for k in range(70):
        a = k / 70.0 * 2 * math.pi
        if math.sin(a * 3 + 1) > 0.55:
            continue
        x = cx + math.cos(a) * 46
        y = cy + math.sin(a) * 26
        g.rect(x, y - 3, x + 2, y, T("skin", "shade"))
        g.set(x, y - 3, T("skin", "base"))
        g.set(x + 1, y + 1, OCC)
    # Spires, snapped at different heights.
    for (sx, sy, h) in [(1048, 352, 40), (1080, 344, 26), (1062, 378, 16), (1096, 370, 33), (1034, 380, 12),
                        (1070, 340, 20)]:
        g.rect(sx + 1, sy + 1, sx + 7, sy + 2, OCC)
        g.rect(sx, sy - h, sx + 4, sy, T("skin", "shade"))
        g.rect(sx, sy - h, sx + 1, sy, T("skin", "base"))
        g.rect(sx + 4, sy - h + 1, sx + 4, sy, T("skin", "deep"))
        # Snapped: a ragged top, lit on the side the key comes from.
        g.set(sx, sy - h, T("skin", "lit"))
        g.set(sx + 1, sy - h - 1, T("skin", "base"))
        g.set(sx + 3, sy - h + 1, T("skin", "deep"))
        g.set(sx + 4, sy - h + 2, T("skin", "deep"))
        g.set(sx + 2, sy - h + 5, OCC)
        g.set(sx + 2, sy - h + 6, OCC)
    # Fallen columns.
    for (fx, fy, ln) in [(1018, 388, 14), (1102, 384, 10)]:
        g.rect(fx, fy + 1, fx + ln, fy + 1, OCC)
        g.rect(fx, fy - 2, fx + ln, fy, T("skin", "shade"))
        g.rect(fx, fy - 2, fx + ln, fy - 2, T("skin", "base"))


def frozen(g, biomes):
    """The ice field under the peaks, wind ridges, and somebody under the ice."""
    cx, cy = 1206, 152
    r = rng_for(606)
    # The ice: a frozen lake, its far rim a lip of snow, its near edge a step down in shadow.
    g.disc(cx + 2, cy + 3, 49, 24, OCC)
    g.disc(cx, cy, 48, 23, T("skin", "base"))
    g.disc(cx, cy + 1, 45, 21, T("steel", "shade"))
    for yy in range(cy - 21, cy + 22):
        for xx in range(cx - 45, cx + 46):
            nx = (xx - cx) / 45.0
            ny = (yy - cy - 1) / 21.0
            if nx * nx + ny * ny > 1:
                continue
            k = -(nx + ny) + (GRAIN.at(xx * 2, yy * 2) - 0.5) * 0.6
            if k > 0.2 and dith(xx, yy, 0.55):
                g.set(xx, yy, T("steel", "base"))
            if k > 0.9 and dith(xx, yy, 0.35):
                g.set(xx, yy, T("steel", "lit"))
    # Cracks that change angle.
    for k in range(7):
        x = cx + (r.random() - 0.5) * 60
        y = cy + (r.random() - 0.5) * 26
        ang = r.random() * 2 * math.pi
        for seg in range(3):
            ang += (r.random() - 0.5) * 1.6
            run = 3 + int(r.random() * 5)
            for j in range(run):
                px = x + math.cos(ang) * j
                py = y + math.sin(ang) * j * 0.6
                if ((px - cx) / 40.0) ** 2 + ((py - cy) / 20.0) ** 2 < 0.9:
                    g.set(px, py, T("steel", "lit") if j % 3 else T("steel", "deep"))
            x += math.cos(ang) * run
            y += math.sin(ang) * run * 0.6
    # A wizard lies under the ice: a figure laid out flat, seen through it - his robe the player's
    # wool, his pointed hat, his staff beside him. Dim: there is ice between him and the light.
    fx, fy = cx - 8, cy + 2
    g.rect(fx, fy - 2, fx + 13, fy + 2, T("wool", "deep"))
    g.rect(fx + 2, fy - 2, fx + 11, fy - 2, T("wool", "shade"))
    g.rect(fx + 14, fy - 1, fx + 16, fy + 1, T("skin", "deep"))
    g.line(fx + 17, fy, fx + 22, fy - 2, T("wool", "deep"))
    g.set(fx + 22, fy - 2, T("wool", "shade"))
    g.line(fx - 2, fy + 4, fx + 18, fy + 4, T("steel", "deep"))
    g.set(fx - 3, fy + 4, T("steel", "lit"))
    # Drifts: long low banks the wind has laid, lit along their tops with their lee in shadow, and
    # rocks breaking through with snow on their heads.
    for k in range(46):
        x = 1080 + r.random() * 280
        y = 80 + r.random() * 190
        if biomes[int(y) * W + int(x)] != "snow" or road_distance(x, y) < 8:
            continue
        if ((x - cx) / 46.0) ** 2 + ((y - cy) / 26.0) ** 2 < 1:
            continue
        ln = 12 + int(r.random() * 22)
        bow = 1.5 + r.random() * 2.5
        for j in range(ln):
            t = j / float(ln)
            yy = y - math.sin(t * math.pi) * bow
            if road_distance(x + j, yy) < 4:
                continue
            g.set(x + j, yy, T("skin", "base"))
            g.set(x + j, yy - 1, T("skin", "lit") if (0.2 < t < 0.6) else T("skin", "shade"))
            g.set(x + j, yy + 1, T("steel", "shade"))
            if dith(x + j, yy + 2, 0.5):
                g.set(x + j, yy + 2, T("stone", "shade"))
    for k in range(18):
        x = 1090 + r.random() * 260
        y = 90 + r.random() * 170
        if biomes[int(y) * W + int(x)] != "snow" or road_distance(x, y) < 8:
            continue
        rr = 2 + r.random() * 3
        g.disc(x + 1, y + 1, rr, rr * 0.7, OCC)
        g.disc(x, y, rr, rr * 0.7, T("stone", "base"))
        g.disc(x - 0.5, y - rr * 0.35, rr * 0.8, rr * 0.35, T("skin", "base"))
        g.set(x - 1, y - rr * 0.5, T("skin", "lit"))


def mountains(g, biomes):
    """The northern wall of peaks, and the range between the ice and the sands with its pass."""
    r = rng_for(707)
    peaks = []

    def chain(x0, x1, base_y, wander, hmin, hmax, rows, skip=None):
        # A range is ranks of peaks, the back rank higher and smaller, each peak a different size
        # and spacing: evenly spaced anything reads as a row of stamps.
        for rank in range(rows):
            x = x0 + r.random() * 20
            while x < x1:
                if skip is None or abs(x - skip[0]) > skip[1]:
                    by = base_y(x) + rank * 16 + (r.random() - 0.5) * wander
                    h = hmin + r.random() * (hmax - hmin) - rank * 8
                    w = h * (1.0 + r.random() * 0.8)
                    peaks.append((by, x, w, h))
                x += 16 + r.random() * 34

    # The north wall, where the land rises into the dark.
    chain(260, 1520, lambda x: 52 + math.sin(x / 90.0) * 14, 22, 40, 84, 3)
    # The range between the ice and the sands, with the pass the road takes.
    chain(1120, 1470, lambda x: 350 + math.sin(x / 50.0) * 18, 22, 32, 64, 2, skip=(1262, 30))
    peaks.sort()
    for by, x, w, h in peaks:
        if road_distance(x, by) < 22 or road_distance(x, by - h * 0.5) < 18:
            continue
        snow = 0.32 if (by < 160 or x > 1100) else 0.2
        peak(g, x, by, w, h, snow, seed=int(x * 7 + by))


def groves(g, biomes):
    """Woods in the hill country and the lowland: clusters of crowns, so the land between places
    has things standing on it, and lone trees where the clusters thin out."""
    r = rng_for(717)
    centres = []
    for _ in range(40):
        x = 420 + r.random() * 1060
        y = 120 + r.random() * 640
        b = biomes[int(y) * W + int(x)]
        if b in ("hills", "lowland", "heath") and road_distance(x, y) > 26:
            centres.append((x, y, 8 + r.random() * 18))
    trees = []
    for (cx, cy, rad) in centres:
        n = int(rad * rad / 9)
        for _ in range(n):
            a = r.random() * 2 * math.pi
            d = math.sqrt(r.random()) * rad
            x = cx + math.cos(a) * d
            y = cy + math.sin(a) * d * 0.7
            b = biomes[min(H - 1, int(y)) * W + min(W - 1, int(x))]
            if b in ("hills", "lowland", "heath") and road_distance(x, y) > 7:
                trees.append((y, x, 3 + r.random() * 2.5, b == "heath"))
    trees.sort()
    for (y, x, rad, heath) in trees:
        crown(g, x, y, rad, "lichen", dark=heath or r.random() < 0.4)


def sands(g, biomes):
    """Dunes, a dry riverbed where he hid the water, cracked earth, a broken cart on the trade road."""
    r = rng_for(808)
    draw_water_path(g, DRY_RIVER, 3, 5, water=T("flesh", "deep"), lit=T("gold", "deep"), bank=OCC)
    # Crescent dunes: a long windward back lit from the upper left, a sharp crest, and the slip
    # face below it falling into shadow. Sorted back to front so a nearer dune covers a farther one.
    dunes = []
    for _ in range(60):
        x = 1220 + r.random() * 260
        y = 470 + r.random() * 300
        if biomes[int(y) * W + int(x)] != "desert" or road_distance(x, y) < 12:
            continue
        dunes.append((y, x, 16 + r.random() * 22, 2 + r.random() * 3))
    dunes.sort()
    for (y, x, ln, bow) in dunes:
        for j in range(int(ln)):
            t = j / ln
            xx = x + j
            yy = y - math.sin(t * math.pi) * bow + math.sin(t * 6.0) * 0.6
            if road_distance(xx, yy) < 4 or biomes[min(H - 1, int(yy)) * W + min(W - 1, int(xx))] != "desert":
                continue
            # The windward back, lit, fading up into the sand; the crest; the slip face below.
            for k in range(1, 5):
                if dith(xx, yy - k, 0.9 - k * 0.22):
                    g.set(xx, yy - k, T("gold", "shade"))
            g.set(xx, yy, T("gold", "base") if t < 0.7 else T("gold", "shade"))
            g.set(xx, yy + 1, T("flesh", "deep"))
            if dith(xx, yy + 2, 0.6 - t * 0.3):
                g.set(xx, yy + 2, OCC)
    # Cracked earth: short angular cracks in occlusion.
    for _ in range(40):
        x = 1300 + r.random() * 160
        y = 560 + r.random() * 160
        if biomes[int(y) * W + int(x)] != "desert" or road_distance(x, y) < 5:
            continue
        ang = r.random() * 2 * math.pi
        for seg in range(3):
            ang += (r.random() - 0.5) * 1.8
            run = 2 + int(r.random() * 4)
            for j in range(run):
                g.set(x + math.cos(ang) * j, y + math.sin(ang) * j, T("gold", "deep"))
            x += math.cos(ang) * run
            y += math.sin(ang) * run
    # The oasis he dried: a cracked pan where the water was, dead palms round it.
    ox, oy = 1384, 628
    g.disc(ox + 1, oy + 2, 28, 12, OCC)
    g.disc(ox, oy, 27, 11, T("flesh", "shade"))
    for yy in range(oy - 11, oy + 12):
        for xx in range(ox - 27, ox + 28):
            nx = (xx - ox) / 27.0
            ny = (yy - oy) / 11.0
            if nx * nx + ny * ny > 1:
                continue
            if -(nx + ny) < -0.3 and dith(xx, yy, 0.6):
                g.set(xx, yy, T("flesh", "deep"))
            # The mud cracked into plates.
            if ((xx + int(GRAIN.at(xx * 3, yy * 9) * 7)) % 7 == 0) or ((yy * 3 + int(GRAIN.at(xx * 9, yy * 3) * 9)) % 8 == 0):
                g.set(xx, yy, T("gold", "deep"))
    g.disc(ox - 3, oy + 1, 5, 2, T("stone", "deep"))
    for (px, py, lean) in ((ox - 30, oy - 4, -1), (ox - 18, oy - 12, 1), (ox + 8, oy - 13, -1), (ox + 26, oy - 6, 1),
                           (ox + 20, oy + 10, 1)):
        g.line(px + 2, py + 1, px + 8, py + 1, OCC)
        for k in range(12):
            g.set(px + (k * lean) // 5, py - k, T("linen", "shade") if k % 3 else T("linen", "deep"))
        tx, ty = px + (12 * lean) // 5, py - 12
        for (dx, dy) in ((-5, 2), (-3, -1), (0, -3), (3, -1), (5, 2), (-6, 3), (6, 3)):
            g.line(tx, ty, tx + dx, ty + dy, T("linen", "deep"))
        g.set(tx, ty, T("linen", "base"))
    # The caravan road: a faint track west to east across the basin.
    for k in range(0, 190, 3):
        tx = 1270 + k
        ty = 676 - k * 0.2 + math.sin(k / 17.0) * 4
        if road_distance(tx, ty) > 6 and dith(tx, ty, 0.7):
            g.set(tx, ty, T("gold", "shade"))
            g.set(tx + 1, ty, T("gold", "shade"))
    # The broken cart on it.
    cx, cy = 1320, 668
    g.rect(cx + 1, cy + 1, cx + 16, cy + 4, OCC)
    g.rect(cx, cy - 6, cx + 14, cy, T("gold", "shade"))
    g.rect(cx, cy - 6, cx + 14, cy - 6, T("gold", "base"))
    g.rect(cx + 2, cy - 4, cx + 12, cy - 4, T("gold", "deep"))
    g.disc(cx + 3, cy + 1, 3, 3, T("stone", "shade"))
    g.disc(cx + 3, cy + 1, 1, 1, OCC)
    g.line(cx + 14, cy - 3, cx + 21, cy - 7, T("gold", "deep"))
    g.line(cx + 11, cy + 2, cx + 14, cy + 4, T("stone", "shade"))
    # The dead still walk the trade road: a file of bent figures and a pack beast, each with his mark.
    for k in range(5):
        fx = 1404 + k * 11
        fy = 664 - k * 3
        if k == 2:
            # The beast: a long back, a hung head, a load.
            g.rect(fx - 2, fy - 6, fx + 7, fy - 3, T("linen", "deep"))
            g.rect(fx, fy - 9, fx + 5, fy - 6, T("gold", "deep"))
            g.set(fx + 8, fy - 5, T("linen", "deep"))
            g.set(fx + 9, fy - 4, T("linen", "deep"))
            for lx in (fx - 1, fx + 1, fx + 4, fx + 6):
                g.line(lx, fy - 2, lx, fy, T("linen", "deep"))
            g.set(fx + 9, fy - 5, T("arcane", "lit"))
            g.line(fx - 2, fy + 1, fx + 8, fy + 1, OCC)
            continue
        g.rect(fx, fy - 7, fx + 2, fy, T("linen", "deep"))
        g.set(fx + 1, fy - 8, T("linen", "shade"))
        g.set(fx + 2, fy - 6, T("linen", "shade"))
        g.set(fx + 2, fy - 7, T("arcane", "lit"))
        g.line(fx + 1, fy + 1, fx + 4, fy + 1, OCC)


def lava_runnels(g, seed, cx, top, count, width):
    """Lava down the cone and onto the plain: angular runs (a crust splitting, tiles.py's rule),
    an edge of fire's edge tone, a hot thread, and a little of its light on the ground beside it."""
    r = rng_for(seed)
    for k in range(count):
        x = cx + (r.random() - 0.5) * 26
        y = top + 10.0
        ang = math.pi / 2 + (r.random() - 0.5) * 1.1
        for seg in range(10):
            ang += (r.random() - 0.5) * 0.7
            ang = max(math.pi / 2 - 0.7, min(math.pi / 2 + 0.7, ang))
            run = 8 + int(r.random() * 12)
            wide = width * (1.0 - seg / 14.0)
            for j in range(run):
                px = x + math.cos(ang) * j
                py = y + math.sin(ang) * j
                if px >= W - 4 or py >= H - 4:
                    break
                if road_distance(px, py) < 5:
                    continue
                for dx in range(-2, 3):
                    for dy in range(-2, 3):
                        d = math.hypot(dx, dy)
                        if d <= wide:
                            g.set(px + dx, py + dy, E["fire"][2] if d < wide * 0.45 else E["fire"][3])
                        elif d <= wide + 1.5 and dith(px + dx, py + dy, 0.3):
                            cur = g.get(px + dx, py + dy)
                            if lum(cur) < lum(E["fire"][3]):
                                g.set(px + dx, py + dy, E["fire"][3])
                if j % 5 == 2:
                    g.set(px, py, E["fire"][1])
            x += math.cos(ang) * run
            y += math.sin(ang) * run


def emberdeep(g, biomes):
    """Black ground and lava; the gate at the foot; the volcano; the forge's fire at the summit."""
    r = rng_for(909)
    cx, top, base = 1576, 292, 446
    # Lava on the plain first, then the cone over its upper reaches, then lava down the cone.
    lava_runnels(g, 910, cx, top + 40, 3, 1.8)
    # The cone: a broad mountain of black rock, its flank toward the key catching it in violet -
    # the Emberdeep's own ground (tiles.py's basalt: dark stone with violet in it).
    left_edge = _ridge(r, cx - 22, top + 6, cx - 112, base, 26)
    right_edge = _ridge(r, cx + 22, top + 6, cx + 112, base, 26)
    spine = _ridge(r, cx - 4, top + 8, cx + 18, base, 30, 3)
    for y in range(top + 6, base + 1):
        xl = _x_at(left_edge, y)
        xr = _x_at(right_edge, y)
        xs = _x_at(spine, y)
        if xl is None or xr is None or xs is None:
            continue
        t = (y - top) / float(base - top)
        for x in range(int(xl), int(xr) + 1):
            if road_distance(x, y) < 3.6:
                continue
            if x < xs:
                c = T("violet", "base") if dith(x, y, 0.45 - t * 0.5) else T("violet", "shade")
                if dith(x, y, t * 0.9 - 0.1):
                    c = T("stone", "shade")
                if x <= xl + 1.5:
                    c = T("violet", "lit") if t < 0.5 else T("violet", "base")
            else:
                c = T("stone", "deep") if dith(x, y, 0.75 - t * 0.4) else OCC
            g.set(x, y, c)
    # The foot of the cone, ragged: it meets the plain in scree and its own shadow.
    for x in range(cx - 112, cx + 113):
        fy = base + int((GRAIN.at(x * 5, 77) - 0.5) * 10)
        for y in range(base - 4, fy + 1):
            if road_distance(x, y) < 3.6:
                continue
            g.set(x, y, T("stone", "deep") if dith(x, y, 0.5) else OCC)
        if dith(x, fy, 0.4) and road_distance(x, fy + 1) > 3.6:
            g.set(x, fy + 1, T("violet", "shade"))
    lava_runnels(g, 911, cx, top, 4, 1.5)
    # The summit: the caldera's lip, and inside it the forge's fire. It held the world's one dark light
    # until the castle was drawn; that light is his, and it burns on his tower now (castle()), so the
    # summit keeps only its fire - the forge every shackle came from.
    g.disc(cx, top + 6, 30, 12, T("stone", "shade"))
    for xx in range(cx - 30, cx + 31):
        nx = (xx - cx) / 30.0
        yy = top + 6 - int(math.sqrt(max(0, 1 - nx * nx)) * 12)
        g.set(xx, yy, T("violet", "lit") if nx < 0.1 else T("stone", "base"))
    g.disc(cx, top + 8, 25, 8, OCC)
    for yy in range(top - 2, top + 18):
        for xx in range(cx - 26, cx + 27):
            nx = (xx - cx) / 25.0
            ny = (yy - top - 8) / 8.0
            d = nx * nx + ny * ny
            if d > 1:
                continue
            # A lava lake under a crust: hot in the middle, crusting dark toward the rim.
            crust = GRAIN.at(xx * 6, yy * 6) > 0.62
            if d < 0.12:
                g.set(xx, yy, E["fire"][1] if dith(xx, yy, 0.55) else E["fire"][2])
            elif d < 0.42:
                g.set(xx, yy, OCC if crust and dith(xx, yy, 0.5) else (E["fire"][2] if dith(xx, yy, 0.64 - d) else E["fire"][3]))
            elif d < 0.82 and dith(xx, yy, 0.82 - d):
                g.set(xx, yy, E["fire"][3] if not crust else T("violet", "deep"))
    g.set(cx, top + 8, E["fire"][0])
    g.set(cx - 1, top + 8, E["fire"][0])
    # Sparks off it, a few above the rim.
    for (mx, my) in ((cx - 8, top - 6), (cx + 5, top - 10), (cx - 2, top - 15), (cx + 11, top - 3)):
        g.set(mx, my, E["fire"][2])
    # The gate at the foot: two black pillars that burn at the top - built, so fire, not his mark -
    # and a lintel across the road.
    gx, gy = 1511, 496
    for px in (gx - 15, gx + 11):
        g.rect(px + 2, gy + 1, px + 8, gy + 3, OCC)
        g.rect(px, gy - 26, px + 5, gy, T("stone", "deep"))
        g.rect(px, gy - 26, px + 1, gy, T("violet", "shade"))
        g.rect(px + 5, gy - 25, px + 5, gy, OCC)
        for k in range(gy - 22, gy, 6):
            g.rect(px, k, px + 5, k, OCC)
        # The brazier on top.
        g.rect(px - 1, gy - 28, px + 6, gy - 27, T("stone", "shade"))
        g.rect(px + 1, gy - 31, px + 4, gy - 29, E["fire"][2])
        g.set(px + 2, gy - 32, E["fire"][1])
        g.set(px + 3, gy - 33, E["fire"][0])
        g.set(px + 1, gy - 31, E["fire"][3])
        g.set(px + 4, gy - 30, E["fire"][3])
    g.rect(gx - 16, gy - 27, gx + 17, gy - 25, T("stone", "deep"))
    g.rect(gx - 16, gy - 27, gx + 17, gy - 27, T("violet", "shade"))
    # Basalt boulders.
    for _ in range(60):
        x = 1430 + r.random() * 250
        y = 200 + r.random() * 520
        if biomes[int(y) * W + int(x)] != "basalt" or road_distance(x, y) < 6:
            continue
        rr = 1.5 + r.random() * 3
        g.disc(x + 1, y + 1, rr, rr * 0.7, OCC)
        g.disc(x, y, rr, rr * 0.7, T("violet", "deep"))
        g.set(x - 1, y - 1, T("violet", "shade"))
        g.set(x - 1, y, T("violet", "shade"))


# ---------------------------------------------------------------------------------------------
# THE CASTLE (.ai/endgame-castle.md 8): past the Emberdeep's black cliffs the dark sea; the Last Bridge
# from the cliffs to a rock standing in it; on the rock his castle - the cells cut into the rock under
# the south wall (the Cursed Dungeon, whose keep this jail was, drawn under his walls now), the halls of
# the Court inside them, and the tower (the Throne), at whose top the world's one dark light burns.
# ---------------------------------------------------------------------------------------------
CLIFF_FACE = 16   # how far the mainland's black cliffs drop to the water where they turn south


def black_cliffs(g, biomes):
    """Where the land ends in the east (east_edge). Seen from the south, a stretch that turns south
    drops into the sea in courses of black rock under a lit rim; one that turns east is seen edge on,
    a dark band. Surf is a fleck of the sea's own light at the foot."""
    r = rng_for(1717)
    for y in range(40, H - 40):
        for x in range(1600, LAND_W + 120):
            i = y * W + x
            if biomes[i] != "sea" or y > 760:
                continue
            north = biomes[i - W] != "sea"
            west = biomes[i - 1] != "sea"
            if north and y < 560:
                g.set(x, y - 1, T("violet", "shade") if dith(x, y, 0.6) else T("violet", "lit"))
                face = CLIFF_FACE + int((GRAIN.at(x * 4, 31) - 0.5) * 6)
                for k in range(face):
                    yy = y + k
                    if biomes[min(H - 1, yy) * W + x] != "sea":
                        break
                    t = k / float(face)
                    course = (k + int(GRAIN.at(x * 3, 7) * 3)) % 5 == 0
                    joint = (x * 5 + (k // 5) * 3) % 7 == 0
                    if t > 0.86:
                        c = OCC
                    elif course or joint:
                        c = OCC if dith(x, yy, 0.7) else T("stone", "deep")
                    elif t < 0.2:
                        c = T("violet", "shade") if dith(x, yy, 0.5) else T("stone", "deep")
                    else:
                        c = T("stone", "deep") if dith(x, yy, 0.8 - t * 0.5) else T("violet", "deep")
                    g.set(x, yy, c)
                if r.random() < 0.35:
                    g.set(x, y + face, T("wool", "shade"))
            elif west and not north:
                g.set(x - 1, y, T("violet", "shade") if dith(x, y, 0.4) else T("stone", "deep"))
                g.set(x, y, OCC)
                g.set(x + 1, y, OCC if dith(x, y, 0.6) else T("stone", "deep"))
                if r.random() < 0.12:
                    g.set(x + 2, y, T("wool", "shade"))


def _poly_span(pts, x):
    """(top, bottom) where the closed polygon pts crosses column x, or None."""
    ys = []
    n = len(pts)
    for i in range(n):
        (ax, ay), (bx, by) = pts[i], pts[(i + 1) % n]
        if ax == bx:
            continue
        if min(ax, bx) <= x < max(ax, bx):
            ys.append(ay + (x - ax) * (by - ay) / float(bx - ax))
    if len(ys) < 2:
        return None
    return min(ys), max(ys)


def _route_between(a, b):
    """The road's samples from stop a's stand point to stop b's, in order."""
    return ROUTE[ROUTE_STOPS[a]:ROUTE_STOPS[b] + 1]


def _y_on(path, x):
    """The y of a path that runs west to east, at column x (None off its ends)."""
    for i in range(len(path) - 1):
        (x0, y0), (x1, y1) = path[i], path[i + 1]
        if min(x0, x1) <= x <= max(x0, x1) and x0 != x1:
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0)
    return None


def _jagged(pts, seed, amp):
    """A closed outline with a midpoint pushed out between every pair of points, by a seeded amount:
    a rock's edge rather than a smooth curve. Only ever outward, so what stands inside it stays inside,
    and never on the south edge, which the south wall stands on."""
    r = rng_for(seed)
    cx = sum(p[0] for p in pts) / float(len(pts))
    cy = sum(p[1] for p in pts) / float(len(pts))
    out = []
    for i in range(len(pts)):
        (ax, ay), (bx, by) = pts[i], pts[(i + 1) % len(pts)]
        out.append((ax, ay))
        mx, my = (ax + bx) / 2.0, (ay + by) / 2.0
        d = math.hypot(mx - cx, my - cy)
        push = r.random() * amp
        # Not along the south edge: the south wall stands on it and the cells are cut under it.
        if (my - cy) / d > 0.55:
            push = 0.0
        out.append((mx + (mx - cx) / d * push, my + (my - cy) / d * push))
    return out


ISLE_OUTLINE = _jagged(ISLE_TOP, 2121, 7.0)


def isle(g):
    """His rock: a plateau standing out of the dark sea, its south face dropping sheer to the water in
    black stone, lit violet where it turns west to the key, its shadow on the sea to the lower right,
    and surf at its foot."""
    xs = [p[0] for p in ISLE_OUTLINE]
    x0, x1 = int(min(xs)), int(max(xs))
    # The shadow on the water first, down and to the right of the whole rock.
    for x in range(x0, x1 + 1):
        span = _poly_span(ISLE_OUTLINE, x)
        if span is None:
            continue
        top, bottom = span
        for y in range(int(top) + 8, int(bottom) + ISLE_CLIFF + 8):
            if dith(x + 8, y, 0.8):
                g.set(x + 8, y, OCC)
    # The south face, column by column under the plateau's edge: buttresses of rock between gullies,
    # each buttress lit on its left, the whole face lighter toward the west where it meets the key.
    for x in range(x0, x1 + 1):
        span = _poly_span(ISLE_OUTLINE, x)
        if span is None:
            continue
        top, bottom = span
        drop = ISLE_CLIFF + int((GRAIN.at(x * 3, 400) - 0.5) * 10)
        west = 1.0 - min(1.0, (x - x0) / 70.0)
        rib = (x + int(GRAIN.at(x * 2, 9) * 6)) % 11
        for k in range(drop):
            y = int(bottom) + k
            t = k / float(drop)
            ledge = (k + int(GRAIN.at(x * 2, 77) * 4)) % 8 == 0
            if t > 0.9 or rib == 10:
                c = OCC
            elif rib == 0:
                c = T("violet", "base") if west > 0.2 or dith(x, y, 0.5) else T("violet", "shade")
            elif ledge:
                c = T("violet", "shade") if dith(x, y, 0.5 + west * 0.4) else T("stone", "deep")
            elif rib < 4:
                c = T("violet", "shade") if dith(x, y, 0.35 + west * 0.5 - t * 0.3) else T("violet", "deep")
            else:
                c = T("violet", "deep") if dith(x, y, 0.55 + west * 0.3 - t * 0.4) else T("stone", "deep")
            g.set(x, y, c)
        foot = int(bottom) + drop
        g.set(x, foot, T("wool", "shade") if dith(x, 3, 0.45) else T("wool", "deep"))
        if dith(x, 5, 0.25):
            g.set(x + 1, foot + 1, T("wool", "shade"))
    # The plateau: black ground under the walls, mottled, its rim lit where it faces the key.
    g.poly(ISLE_OUTLINE, T("stone", "deep"))
    for x in range(x0, x1 + 1):
        span = _poly_span(ISLE_OUTLINE, x)
        if span is None:
            continue
        top, bottom = span
        for y in range(int(top), int(bottom) + 1):
            if dith(x * 3, y * 5, 0.3 + (GRAIN.at(x * 4, y * 4) - 0.5)):
                g.set(x, y, T("violet", "deep"))
        g.set(x, int(top), T("violet", "lit") if x < (x0 + x1) // 2 else T("violet", "shade"))
        g.set(x, int(bottom), T("violet", "base") if dith(x, 1, 0.5) else T("violet", "shade"))
    n = len(ISLE_OUTLINE)
    for i in range(n):
        (ax, ay), (bx, by) = ISLE_OUTLINE[i], ISLE_OUTLINE[(i + 1) % n]
        # The rim that faces the key - running up the west and along the north-west - is lit.
        if by < ay and bx >= ax - 2 and ax < 2000:
            g.line(ax, ay, bx, by, T("violet", "lit"))


def east_boulders(g, biomes):
    """Basalt boulders on the land the world grew into, between the volcano and the cliffs: the
    Emberdeep's own (emberdeep() scatters them west of here, on a seed of its own)."""
    r = rng_for(1919)
    for _ in range(40):
        x = 1640 + r.random() * 110
        y = 120 + r.random() * 600
        if biomes[int(y) * W + int(x)] != "basalt" or road_distance(x, y) < 6 or x > east_edge(int(y)) - 6:
            continue
        rr = 1.5 + r.random() * 3
        g.disc(x + 1, y + 1, rr, rr * 0.7, OCC)
        g.disc(x, y, rr, rr * 0.7, T("violet", "deep"))
        g.set(x - 1, y - 1, T("violet", "shade"))
        g.set(x - 1, y, T("violet", "shade"))


def last_bridge(g):
    """The Last Bridge: a long stone causeway on piers, from the headland's cliffs to the gate on his
    rock. Its deck is the road itself - the wizard walks it - between two parapets; under it the arches
    and the piers stand in the dark water, and its shadow lies on the sea to the lower right."""
    path = _route_between("stage_11", "stage_1")
    ax = int(path[0][0]) - 6
    bx = int(BRIDGE_TO[0]) - 2
    span = float(bx - ax)
    pier_every = 17
    # Its shadow on the water, offset from the key.
    for x in range(ax, bx + 1):
        y = _y_on(path, x)
        if y is None or x <= east_edge(int(y)):
            continue
        t = (x - ax) / span
        h = CLIFF_FACE + (ISLE_CLIFF - CLIFF_FACE) * t
        for k in range(3, 6):
            g.set(x + 6, int(y + h + k), OCC if dith(x, y + k, 0.7) else T("stone", "deep"))
    for x in range(ax, bx + 1):
        y = _y_on(path, x)
        if y is None:
            continue
        yi = int(round(y))
        t = (x - ax) / span
        over_sea = x > east_edge(yi) + 1
        h = int(CLIFF_FACE + (ISLE_CLIFF - CLIFF_FACE) * t)
        if over_sea:
            # Between the piers an arch: the side face hangs deep beside a pier and shallow at the crown.
            f = ((x - ax) % pier_every) / float(pier_every)
            pier = (x - ax) % pier_every < 4
            depth = h if pier else int(5 + (h - 5) * abs(2 * f - 1) ** 5)
            for k in range(3, depth + 1):
                yy = yi + k
                if pier:
                    first = (x - ax) % pier_every == 0
                    last = (x - ax) % pier_every == 3
                    c = T("stone", "base") if first else (T("stone", "deep") if last else T("stone", "shade"))
                    if k >= h - 1:
                        c = OCC
                    elif (k + x) % 5 == 0 and not first:
                        c = T("stone", "deep")
                else:
                    c = T("stone", "shade") if k < 5 else (T("stone", "deep") if k < depth else OCC)
                    if k == depth:
                        c = OCC
                g.set(x, yy, c)
            if (x - ax) % pier_every < 4 and dith(x, yi + h, 0.6):
                g.set(x, yi + h + 1, T("wool", "shade"))
        # The deck: north parapet lit from the key, the paved way, the south parapet a step down.
        g.set(x, yi - 4, T("stone", "lit") if (x % 3) else T("stone", "base"))
        g.set(x, yi - 3, T("stone", "base"))
        for k in range(-2, 2):
            c = T("stone", "base") if k > -2 else T("stone", "lit")
            if (x + k * 2) % 6 == 0:
                c = T("stone", "shade")
            g.set(x, yi + k, c)
        g.set(x, yi + 2, T("stone", "base"))
        g.set(x, yi + 3, T("stone", "shade") if (x % 3) else T("stone", "deep"))
        if (x % 3) == 0:
            g.set(x, yi - 5, T("stone", "base"))


def castle(g, tower_lit=True):
    """The castle on the rock: the curtain wall with its towers and the gate the bridge comes to, the
    cells under the south wall with one window lit, the Court's halls, and his tower - and, since the far
    shore, his postern in the east wall and the stair down to the sea-gate (castle_crossing)."""
    flag = [T("stone", "deep"), T("stone", "shade")]
    # The yard inside the walls: laid flags, a step lighter than the rock.
    inner = [(1874, 398), (1872, 380), (1878, 350), (1894, 316), (1918, 284), (1948, 258), (1982, 238),
             (2014, 230), (2036, 240), (2046, 266), (2044, 302), (2034, 338), (2020, 372), (2002, 394),
             (1970, 402), (1920, 404)]
    g.poly(inner, flag[0])
    for x in range(1870, 2050):
        span = _poly_span(inner, x)
        if span is None:
            continue
        for y in range(int(span[0]) + 1, int(span[1])):
            if (x % 6 == 0 or (y + (x // 6) * 3) % 5 == 0) and dith(x, y, 0.55):
                g.set(x, y, OCC)
            elif dith(x + 2, y + 1, 0.22):
                g.set(x, y, flag[1])
    # The yard's way, from the gate up to the hall door and round to the tower: worn flags, lit. Since the far
    # shore it goes on past the tower door, east across the yard to his postern (castle_crossing) - drawn here, under
    # the walls and the tower, so neither is painted over.
    yard_way = ROUTE[ROUTE_STOPS["stage_1"]:ROUTE_STOPS["stage_13"] + 1]
    yard_way += ROUTE[ROUTE_STOPS["stage_13"] + 1:_route_index_near(POSTERN) + 1]
    for (px, py) in yard_way:
        for yy in range(int(py) - 2, int(py) + 3):
            for xx in range(int(px) - 2, int(px) + 3):
                if (xx - px) ** 2 + (yy - py) ** 2 <= 4.2:
                    g.set(xx, yy, T("stone", "base") if dith(xx, yy, 0.7) else T("stone", "shade"))
        g.set(px, py, T("stone", "lit") if dith(px, py, 0.4) else T("stone", "base"))

    # THE NORTH AND EAST WALLS, behind everything: the walk along their top, and their inner face, which
    # faces the viewer.
    back = [(1880, 352), (1896, 314), (1920, 282), (1950, 256), (1984, 236), (2016, 228), (2038, 238),
            (2048, 266), (2046, 302), (2036, 340), (2022, 374)]
    for i in range(len(back) - 1):
        (x0, y0), (x1, y1) = back[i], back[i + 1]
        steps = int(max(abs(x1 - x0), abs(y1 - y0)))
        for s in range(steps + 1):
            x = x0 + (x1 - x0) * s / float(max(1, steps))
            y = y0 + (y1 - y0) * s / float(max(1, steps))
            east = x > 2030
            g.set(x, y - 3, T("stone", "lit") if (int(x) % 3) and not east else T("stone", "base"))
            g.set(x, y - 2, T("stone", "base"))
            g.set(x, y - 1, T("stone", "base") if not east else T("stone", "shade"))
            for k in range(0, 5):
                c = T("stone", "shade") if k < 4 else OCC
                if (k == 2 and int(x) % 4 == 0):
                    c = T("stone", "deep")
                g.set(x, y + k, c)
    for (tx, ty) in ((1888, 318), (1942, 248), (2028, 222), (2040, 300)):
        tower(g, tx, ty, 9, 16)

    keep_tower(g, tower_lit)
    halls(g)

    # THE SOUTH WALL, in front: its face toward the viewer standing on the rock's south edge, the walk
    # along its top, merlons on the outer lip.
    for x in range(1878, 2016):
        span = _poly_span(ISLE_TOP, x)
        if span is None:
            continue
        foot = int(span[1]) - 1
        top = foot - 10
        if (x % 3) != 2:
            g.set(x, top - 1, T("stone", "lit") if (x % 3) == 0 else T("stone", "base"))
        g.set(x, top, T("stone", "lit"))
        g.set(x, top + 1, T("stone", "base"))
        for y in range(top + 2, foot + 1):
            k = y - top - 2
            c = T("stone", "base") if dith(x, y, 0.55 - k * 0.04) else T("stone", "shade")
            if (k % 4 == 3) or ((x + (k // 4) * 4) % 9 == 0):
                c = T("stone", "shade") if dith(x, y, 0.6) else T("stone", "deep")
            g.set(x, y, c)
        g.set(x, foot + 1, OCC)
    for (tx, ty) in ((1930, 392), (1994, 384)):
        tower(g, tx, ty, 9, 22)
    # The gatehouse the bridge comes to: two towers and the dark arch between them, and his cold light
    # in a sconce either side - the jail's own flame (jail_walls.cold_flame), so the door says whose it is.
    gx, gy = 1866, 404
    tower(g, gx - 2, gy - 30, 9, 26)
    tower(g, gx + 14, gy - 32, 9, 28)
    g.rect(gx + 7, gy - 22, gx + 13, gy - 18, T("stone", "base"))
    g.rect(gx + 7, gy - 18, gx + 13, gy - 4, OCC)
    for k in range(gx + 7, gx + 14):
        g.set(k, gy - 18 - (1 if gx + 9 <= k <= gx + 11 else 0), T("stone", "lit"))
    for bx in (gx + 8, gx + 10, gx + 12):
        g.line(bx, gy - 17, bx, gy - 8, T("steel", "shade"))
    for sx in (gx + 5, gx + 15):
        g.set(sx, gy - 16, E["arcane"][1])
        g.set(sx, gy - 15, E["arcane"][2])
        g.set(sx, gy - 17, E["arcane"][3])

    cells(g)
    castle_crossing(g)


def cells(g):
    """The Cursed Dungeon: his jail, cut into the rock under the south wall - a band of cut stone in
    the cliff, barred windows, and one window lit, because somebody is kept here (it was the keep's
    window, on the river's crag, until the chapter moved in). At the waterline, the sea-gate."""
    x0, x1 = 1896, 1990
    band = 13
    for x in range(x0, x1 + 1):
        span = _poly_span(ISLE_TOP, x)
        if span is None:
            continue
        top = int(span[1]) + 2
        g.set(x, top - 1, T("stone", "lit") if dith(x, 0, 0.5) else T("stone", "base"))
        for k in range(band):
            y = top + k
            # Courses of dressed stone, lit toward the west, darker as they go down into the rock.
            c = T("stone", "base") if dith(x, y, 0.62 - k * 0.04 - (x - x0) * 0.002) else T("stone", "shade")
            if k % 4 == 3 or (x + (k // 4) * 5) % 9 == 0:
                c = T("stone", "deep")
            if x == x0:
                c = T("stone", "lit")
            elif x == x1:
                c = OCC
            if k == band - 1:
                c = OCC
            g.set(x, y, c)
    # Barred windows along the band, and the one that is not dark.
    for i, wx in enumerate(range(x0 + 6, x1 - 5, 11)):
        span = _poly_span(ISLE_TOP, wx)
        if span is None:
            continue
        wy = int(span[1]) + 5
        lit = i == 4
        g.rect(wx, wy, wx + 4, wy + 4, OCC)
        if lit:
            # Light from the cell: the light ramp's warmest, a candle's, the only warm thing on the rock.
            g.rect(wx + 1, wy + 1, wx + 3, wy + 3, E["light"][2])
            g.set(wx + 2, wy + 2, E["light"][1])
            g.set(wx + 2, wy + 5, E["light"][3])
            g.set(wx + 1, wy + 6, E["light"][3])
        for bx in (wx + 1, wx + 3):
            g.line(bx, wy, bx, wy + 4, T("steel", "base") if lit else T("steel", "shade"))
        g.line(wx - 1, wy - 1, wx + 5, wy - 1, T("stone", "lit"))
    # The sea-gate: an arch at the foot of the rock, a grating across it, steps down into the water.
    sx = 1946
    span = _poly_span(ISLE_TOP, sx)
    if span is not None:
        foot = int(span[1]) + ISLE_CLIFF - 3
        for yy in range(foot - 11, foot + 1):
            for xx in range(sx - 7, sx + 8):
                nx = (xx - sx) / 7.0
                ny = (yy - foot + 5) / 7.0
                if ny < 0 and nx * nx + ny * ny > 1:
                    continue
                g.set(xx, yy, OCC)
        for xx in range(sx - 8, sx + 9):
            ny = -math.sqrt(max(0.0, 1 - ((xx - sx) / 8.0) ** 2))
            g.set(xx, int(foot - 5 + ny * 8) - 1, T("stone", "lit") if xx < sx else T("stone", "base"))
        for bx in range(sx - 5, sx + 6, 2):
            g.line(bx, foot - 9 + abs(bx - sx) // 3, bx, foot - 3, T("steel", "shade"))
        for k, step in enumerate(range(foot - 2, foot + 3, 2)):
            g.line(sx - 6 + k, step, sx + 6 - k, step, T("stone", "base") if k == 0 else T("stone", "shade"))


def halls(g):
    """The Court: the great hall laid across the yard, east to west, its slate roof lit on the north
    slope and a step down on the south, its front wall with tall dark windows; and the west range. A
    court with no one left in it, so no window is lit."""
    def gabled(xa, xb, ridge, eave, front):
        # North slope (lit), ridge, south slope (a step down), eaves, the front wall to the ground.
        for x in range(xa, xb + 1):
            west_end = x - xa < 2
            east_end = xb - x < 2
            for y in range(ridge - 4, front + 1):
                if y < ridge:
                    c = T("steel", "base") if dith(x, y, 0.5) else T("steel", "shade")
                elif y == ridge:
                    c = T("steel", "lit") if x % 2 else T("steel", "base")
                elif y < eave:
                    c = T("steel", "shade") if dith(x, y, 0.55 - (y - ridge) * 0.03) else T("steel", "deep")
                    if (y - ridge) % 3 == 0 and dith(x, y, 0.5):
                        c = T("steel", "deep")
                elif y == eave:
                    c = OCC
                else:
                    k = y - eave
                    c = T("stone", "base") if dith(x, y, 0.6 - k * 0.04) else T("stone", "shade")
                    if k > 1 and (x - xa) % 7 in (3, 4) and y < front - 1:
                        c = OCC
                if west_end:
                    c = T("stone", "lit") if y > ridge else T("steel", "lit")
                elif east_end:
                    c = T("stone", "deep") if y > ridge else T("steel", "deep")
                g.set(x, y, c)
            g.set(x + 2, front + 1, OCC)
            g.set(x + 1, front + 2, OCC if dith(x, 0, 0.5) else T("stone", "deep"))
    # The west range first (it is further back), then the great hall in front of the tower's foot.
    gabled(1890, 1914, 306, 322, 334)
    gabled(1946, 2024, 324, 338, 352)
    # The great hall's door, on the yard.
    g.rect(1958, 344, 1962, 352, OCC)
    g.set(1960, 343, T("stone", "lit"))


def keep_tower(g, lit=True):
    """The Throne: his tower, the tallest thing in the world, black stone lit on its left from the key,
    and at its top the throne room's windows burning with the one dark light there is - his - with its
    motes going up off the spire. Unlit (lit=False, the tower once he has fallen: tower-unlit.png, laid over
    the world at runtime after Taken Back) the windows are dark glass and nothing burns round the top."""
    x0, x1, foot, top = 1990, 2009, 300, 190
    # Its shadow over the yard and the wall behind it, to the lower right.
    for y in range(top + 12, foot + 4):
        for x in range(x1 + 1, x1 + 8):
            if dith(x, y, 0.7):
                g.set(x, y, OCC)
    for y in range(top, foot + 1):
        for x in range(x0, x1 + 1):
            u = (x - x0) / float(x1 - x0)
            if x == x0:
                c = T("stone", "lit")
            elif x == x1:
                c = OCC
            elif u < 0.5:
                c = T("stone", "base") if dith(x, y, 0.7 - u) else T("stone", "shade")
            else:
                c = T("stone", "shade") if dith(x, y, 0.9 - u) else T("stone", "deep")
            if (y - top) % 7 == 0 and x != x0:
                c = T("stone", "deep") if dith(x, y, 0.6) else OCC
            g.set(x, y, c)
    # Arrow slits up the face, and the door at its foot where the road ends.
    for sy in range(top + 34, foot - 14, 13):
        g.rect(x0 + 6, sy, x0 + 6, sy + 3, OCC)
        g.rect(x0 + 13, sy + 6, x0 + 13, sy + 9, OCC)
    g.rect(1996, foot - 9, 2003, foot, OCC)
    g.set(1997, foot - 10, T("stone", "lit"))
    g.set(1998, foot - 10, T("stone", "lit"))
    g.set(2002, foot - 10, T("stone", "base"))
    # The throne room: a belt of tall windows under the battlements, burning with his light - or, once he has
    # fallen, dark glass with the key's last catch on the sill.
    for wx in (x0 + 3, x0 + 8, x0 + 13):
        for wy in range(top + 9, top + 21):
            if not lit:
                g.set(wx, wy, OCC)
                g.set(wx + 1, wy, OCC if wy < top + 19 else T("stone", "deep"))
                continue
            c = E["darkness"][2] if dith(wx, wy, 0.75) else E["darkness"][3]
            if top + 12 <= wy <= top + 15:
                c = E["darkness"][1]
            if wx == x0 + 8 and wy in (top + 13, top + 14):
                c = E["darkness"][0]
            g.set(wx, wy, c)
            g.set(wx + 1, wy, E["darkness"][2] if top + 11 < wy < top + 18 else E["darkness"][3])
        g.set(wx, top + 8, OCC)
        g.set(wx + 1, top + 8, OCC)
        if not lit:
            g.set(wx, top + 21, T("stone", "base"))
    # Machicolations and battlements, a little wider than the shaft.
    g.rect(x0 - 2, top + 4, x1 + 2, top + 6, T("stone", "shade"))
    for x in range(x0 - 2, x1 + 3, 2):
        g.set(x, top + 7, OCC)
    g.rect(x0 - 2, top + 3, x1 + 2, top + 3, T("stone", "lit"))
    for x in range(x0 - 2, x1 + 3):
        if (x - x0) % 3 != 2:
            g.set(x, top + 1, T("stone", "lit") if x < (x0 + x1) // 2 else T("stone", "base"))
            g.set(x, top + 2, T("stone", "base"))
    # The spire: a black cone, its left flank catching the key in violet.
    cx = (x0 + x1) / 2.0
    for y in range(top - 26, top + 1):
        half = (y - (top - 26)) / 26.0 * 10.0
        for x in range(int(cx - half), int(cx + half) + 1):
            left = x < cx
            c = (T("violet", "shade") if dith(x, y, 0.55) else T("violet", "deep")) if left else (T("stone", "deep") if dith(x, y, 0.5) else OCC)
            if left and x <= cx - half + 1:
                c = T("violet", "base")
            g.set(x, y, c)
    # His light round the top, and going up off it: the world's one dark light (it was on the Emberdeep's
    # summit until the castle was drawn). A halo dithered out from the windows over whatever is darker
    # than it - the sea, the night - and motes rising off the spire.
    if not lit:
        return
    for y in range(top - 46, top + 34):
        for x in range(x0 - 22, x1 + 23):
            d = math.hypot((x - cx) / 26.0, (y - top - 14) / 30.0)
            if d >= 1.0:
                continue
            cur = g.get(x, y)
            if lum(cur) >= lum(E["darkness"][3]):
                continue
            if dith(x, y, (1.0 - d) * 0.75):
                g.set(x, y, E["darkness"][3])
    for (mx, my, k) in ((cx - 5, top - 31, 2), (cx + 4, top - 37, 3), (cx - 1, top - 45, 2), (cx + 8, top - 29, 3),
                        (cx - 9, top - 23, 3), (cx + 1, top - 53, 3), (cx - 6, top - 41, 3), (cx + 6, top - 47, 2),
                        (cx + 11, top - 18, 3), (cx - 12, top - 12, 3)):
        g.set(mx, my, E["darkness"][k])
    g.set(cx, top - 27, E["darkness"][1])
    g.set(cx, top - 28, E["darkness"][0])


# ---------------------------------------------------------------------------------------------
# THE FAR SHORE (.ai/second-half.md 4.1-4.2, piece F5). Across the dark sea east of his rock: the road goes on
# past the Throne out of his east postern, down to the sea-gate, over the old causeway - the way across that was
# here before his castle, which he sat down in the middle of - and lands in a cove under white chalk cliffs facing
# the Emberdeep's black ones. Six seats run west to east from there, each a god's, each held by his shadow: no
# window lit, no fire, no light anywhere on the far shore. His mark (arcane eye-light) is in each, as in every
# place he holds. All of it is under the dark until Taken Back, and each seat after the first until it is earned
# (WorldMapView's reveal), so nothing here is a spoiler on the map before its time.
# ---------------------------------------------------------------------------------------------
def _route_index_near(pt, after="stage_13"):
    """The index of the road's sample nearest pt, past stop `after`: where a piece of the crossing sits on it."""
    best, at = 1e18, ROUTE_STOPS[after]
    for i in range(ROUTE_STOPS[after], len(ROUTE)):
        d = (ROUTE[i][0] - pt[0]) ** 2 + (ROUTE[i][1] - pt[1]) ** 2
        if d < best:
            best, at = d, i
    return at


def _eyes(g, x, y):
    """His mark: two points of cold arcane light in the dark of something he holds (Elderbark's, the lanterns')."""
    g.set(x, y, T("arcane", "lit"))
    g.set(x + 2, y, T("arcane", "lit"))


def castle_crossing(g):
    """His postern in the east wall and the way down to the water: the road leaves the tower east, out through a
    low door in the curtain wall - his cold flame either side, as at the gatehouse, so the door says whose it is -
    and down a stair cut into the rock's south-east face to the east sea-gate's quay, where the old causeway
    begins. Drawn last, over the wall and the face."""
    px, py = POSTERN
    g.rect(px - 2, py - 5, px + 2, py + 3, OCC)
    for x in range(px - 2, px + 3):
        g.set(x, py - 6 - (1 if x == px else 0), T("stone", "lit") if x <= px else T("stone", "base"))
    g.set(px - 3, py - 5, T("stone", "lit"))
    g.set(px + 3, py - 4, T("stone", "deep"))
    for sx in (px - 4, px + 4):
        g.set(sx, py - 3, E["arcane"][1])
        g.set(sx, py - 2, E["arcane"][2])
        g.set(sx, py - 4, E["arcane"][3])
    # The stair: treads cut down the face, each lit on its top, the cut's shadow on its east side.
    first = _route_index_near(POSTERN) + 3
    last = _route_index_near(QUAY)
    for (sx, sy) in ROUTE[first:last]:
        tread = int(sy) % 3 == 0
        for k in range(-2, 3):
            c = (T("stone", "lit") if k < 1 else T("stone", "base")) if tread else T("stone", "shade")
            g.set(sx + k, sy, c)
        g.set(sx + 3, sy, OCC)
        g.set(sx + 3, sy + 1, OCC if dith(sx, sy, 0.6) else T("stone", "deep"))
    # The sea-gate: an arch in the face at the stair's foot, its grating up, and the quay in front of it.
    qx, qy = QUAY
    ax, ay = qx - 6, qy - 4
    for yy in range(ay - 7, ay + 1):
        for xx in range(ax - 4, ax + 5):
            nx = (xx - ax) / 4.0
            ny = (yy - ay + 3) / 4.0
            if ny < 0 and nx * nx + ny * ny > 1:
                continue
            g.set(xx, yy, OCC)
    for xx in range(ax - 5, ax + 6):
        ny = -math.sqrt(max(0.0, 1 - ((xx - ax) / 5.0) ** 2))
        g.set(xx, int(ay - 3 + ny * 5) - 1, T("stone", "lit") if xx < ax else T("stone", "base"))
    for bx in range(ax - 3, ax + 4, 2):
        g.set(bx, ay - 7 + abs(bx - ax) // 2, T("steel", "shade"))
    g.rect(qx - 7, qy - 1, qx + 7, qy + 2, T("stone", "base"))
    g.rect(qx - 7, qy - 2, qx + 7, qy - 2, T("stone", "lit"))
    g.rect(qx - 7, qy + 3, qx + 8, qy + 3, OCC)
    g.rect(qx + 8, qy - 1, qx + 8, qy + 2, OCC)
    for xx in range(qx - 8, qx + 10, 2):
        if dith(xx, qy, 0.5):
            g.set(xx, qy + 4, T("wool", "shade"))


def causeway(g):
    """The old way across the strait, from his sea-gate's quay to the cove under the white cliffs: a low causeway of
    pale dressed stone - the far shore's chalk-stone, not his black - older than his castle and lower than the Last
    Bridge, the sea washing over it where blocks have gone. Its deck is the road; surf on both sides, its shadow
    on the water to the lower right."""
    path = ROUTE[_route_index_near(QUAY):_route_index_near(COVE) + 1]
    x0, x1 = int(QUAY[0]) + 8, int(COVE[0]) + 14
    r = rng_for(2207)
    washed = [(2104, 2112), (2146, 2151), (2183, 2190)]
    for x in range(x0, x1 + 1):
        y = _y_on(path, x)
        if y is None or not is_sea(x, int(round(y))):
            continue
        for k in range(4, 6):
            if dith(x + 5, y + k, 0.7):
                g.set(x + 5, int(y + k), OCC)
    for x in range(x0, x1 + 1):
        y = _y_on(path, x)
        if y is None or not is_sea(x, int(round(y))):
            continue
        yi = int(round(y))
        wash = any(a <= x <= b for (a, b) in washed)
        joint = (x + yi) % 6 == 0
        for k in range(-2, 3):
            if wash:
                c = T("wool", "shade") if dith(x, yi + k, 0.45) else T("skin", "deep")
            elif k == -2:
                c = T("skin", "base") if dith(x, yi, 0.5) else T("skin", "shade")
            else:
                c = T("skin", "shade") if dith(x, yi + k, 0.6 - k * 0.12) else T("skin", "deep")
                if joint:
                    c = T("stone", "base")
            g.set(x, yi + k, c)
        g.set(x, yi + 3, T("skin", "deep") if not wash else T("wool", "deep"))
        g.set(x, yi + 4, OCC)
        if r.random() < (0.55 if wash else 0.3):
            g.set(x, yi - 3, T("wool", "shade"))
        if r.random() < 0.35:
            g.set(x, yi + 5, T("wool", "shade"))


WHITE_FACE = 22   # how far the white cliffs drop to the water where the coast steps east


def white_cliffs(g, biomes):
    """The far shore's west coast (far_west_edge): white chalk, facing the Emberdeep's black cliffs across the dark
    sea (black_cliffs' rule, in chalk). Where the coast steps east it is seen from the south - a chalk face dropping
    to the water in pale courses with flint bands, lit toward the headland's tip where it turns to the key, a turf
    lip on top and surf at its foot. Where it runs north and south it faces the key and shows as a lit chalk edge.
    The cove where the causeway lands is a shingle beach instead. Chalk is the skin ramp, the palette's bone."""
    r = rng_for(2219)
    for y in range(40, FLATS_TOP + WHITE_FACE + 2):
        for x in range(FAR_X0, 2300):
            i = y * W + x
            if biomes[i] != "sea":
                continue
            north = biomes[i - W] != "sea" and y <= FLATS_TOP + 1
            east = biomes[i + 1] != "sea"
            cove = 386 <= y <= 420
            if north:
                g.set(x, y - 1, T("linen", "base") if dith(x, y, 0.5) else T("skin", "shade"))
                face = WHITE_FACE + int((GRAIN.at(x * 4, 61) - 0.5) * 6)
                tip = 1.0 - min(1.0, max(0.0, (x - far_west_edge(y - 1)) / 22.0))
                for k in range(face):
                    yy = y + k
                    if biomes[min(H - 1, yy) * W + x] != "sea":
                        break
                    t = k / float(face)
                    band = (k + int(GRAIN.at(x * 3, 13) * 3)) % 5 == 0
                    if t > 0.88:
                        c = OCC
                    elif t > 0.72:
                        c = T("skin", "deep") if dith(x, yy, 0.6) else OCC
                    elif band:
                        c = T("stone", "base") if dith(x, yy, 0.55) else T("skin", "deep")
                    elif t < 0.15:
                        c = T("skin", "lit") if dith(x, yy, 0.45 + tip * 0.4) else T("skin", "base")
                    else:
                        c = T("skin", "base") if dith(x, yy, 0.25 + tip * 0.5 - t * 0.35) else T("skin", "shade")
                    g.set(x, yy, c)
                if r.random() < 0.45:
                    g.set(x, y + face, T("wool", "shade"))
            elif east and y >= FLATS_TOP:
                continue
            elif east and cove:
                # The shingle beach: pale stones running down to the water, a line of surf on it.
                for k in range(1, 9):
                    if dith(x + k, y, 0.55 - k * 0.05):
                        g.set(x + k, y, T("skin", "shade") if (x + k + y) % 3 else T("skin", "base"))
                g.set(x, y, T("wool", "shade") if dith(x, y, 0.6) else T("skin", "deep"))
            elif east:
                # Seen a little from the west: the face's top lit, chalk down to the water, a flint band in it.
                g.set(x + 1, y, T("linen", "base") if dith(x, y, 0.5) else T("skin", "shade"))
                g.set(x, y, T("skin", "lit") if dith(x, y, 0.5) else T("skin", "base"))
                g.set(x - 1, y, T("skin", "base") if dith(x, y, 0.55) else T("skin", "shade"))
                g.set(x - 2, y, T("skin", "shade") if (y + int(GRAIN.at(31, y * 3) * 4)) % 5 else T("stone", "base"))
                g.set(x - 3, y, T("skin", "deep") if dith(x, y, 0.5) else OCC)
                g.set(x - 4, y, OCC)
                if r.random() < 0.3:
                    g.set(x - 5, y, T("wool", "shade"))


def downs_scarp(g):
    """The downs' south edge: a chalk scarp dropping to the seabed the tide left, cut where the road goes down."""
    pts = spline(DOWNS_SCARP, 1.0)
    cols = {}
    for (x, y) in pts:
        xi = int(round(x))
        if xi not in cols or y < cols[xi]:
            cols[xi] = y
    for xi in sorted(cols):
        top = cols[xi] + (GRAIN.at(xi * 6, 70) - 0.5) * 2
        height = 13 + (GRAIN.at(xi * 2, 95) - 0.5) * 8
        if road_distance(xi, top + height / 2.0) < 5:
            continue
        g.set(xi, top - 1, T("linen", "base") if dith(xi, 0, 0.5) else T("skin", "shade"))
        for k in range(int(height) + 1):
            y = top + k
            t = k / height
            band = (xi * 5 + int(GRAIN.at(xi * 9, y * 0.5) * 9)) % 6 == 0
            if t < 0.15:
                c = T("skin", "lit") if dith(xi, y, 0.4) else T("skin", "base")
            elif band:
                c = T("skin", "deep")
            elif t < 0.6:
                c = T("skin", "base") if dith(xi, y, 0.6 - t) else T("skin", "shade")
            else:
                c = T("skin", "shade") if dith(xi, y, 1.0 - t) else T("skin", "deep")
            g.set(xi, y, c)
        g.set(xi, top + height + 1, OCC)
        g.set(xi, top + height + 2, OCC if dith(xi, 0, 0.5) else T("stone", "deep"))


def barrow(g, cx, cy, rx, ry):
    """A burial mound on the downs, turfed over: its ditch round it, its own shadow lower right, its crown lit on
    the upper left where the turf is thin and the chalk shows."""
    for yy in range(int(cy - ry) - 4, int(cy + ry) + 5):
        for xx in range(int(cx - rx) - 4, int(cx + rx) + 5):
            nx = (xx - cx) / float(rx + 2.5)
            ny = (yy - cy - 0.5) / float(ry + 2)
            d = nx * nx + ny * ny
            if 0.62 < d <= 1.0:
                g.set(xx, yy, T("linen", "shade") if dith(xx, yy, 0.55) else T("stone", "base"))
    g.disc(cx + 2, cy + 2, rx, ry, OCC)
    for yy in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for xx in range(int(cx - rx) - 1, int(cx + rx) + 2):
            nx = (xx - cx) / float(rx)
            ny = (yy - cy) / float(ry)
            d = nx * nx + ny * ny
            if d > 1.0:
                continue
            k = -(nx + ny * 1.3)
            if k > 0.75 and d > 0.1:
                c = T("skin", "shade") if dith(xx, yy, 0.55) else T("skin", "base")
            elif k > 0.0:
                c = T("linen", "base") if dith(xx, yy, 0.6 - k * 0.4) else T("skin", "shade")
            elif k > -0.7:
                c = T("linen", "base") if dith(xx, yy, 0.6 + k * 0.5) else T("linen", "shade")
            else:
                c = T("linen", "shade") if dith(xx, yy, 0.5) else T("stone", "base")
            g.set(xx, yy, c)


# The hill figures cut in the downs' turf (.ai/second-half.md 4.2, "a hound and a hart cut in the hillsides"): chalk
# lines, drawn as the old figures are - long, few strokes, a running thing. Segments from the figure's origin.
HOUND = [((0, 4), (16, 3)), ((1, 5), (15, 4)), ((16, 3), (21, 1)), ((21, 1), (25, 2)), ((19, 1), (18, -2)),
         ((0, 4), (-5, 1)), ((-5, 1), (-6, 2)), ((15, 5), (20, 9)), ((13, 5), (14, 9)), ((3, 5), (-1, 9)),
         ((5, 5), (7, 9))]
HART = [((4, 8), (14, 8)), ((4, 9), (14, 9)), ((4, 8), (2, 4)), ((2, 4), (0, 5)), ((2, 4), (1, 0)), ((1, 1), (-1, 0)),
        ((2, 3), (4, 0)), ((3, 1), (5, -1)), ((5, 10), (5, 14)), ((7, 10), (8, 14)), ((12, 10), (12, 14)),
        ((14, 10), (15, 14)), ((14, 8), (16, 7))]


def hill_figure(g, ox, oy, segments, flip=False):
    for ((ax, ay), (bx, by)) in segments:
        if flip:
            ax, bx = -ax, -bx
        g.line(ox + ax + 1, oy + ay + 1, ox + bx + 1, oy + by + 1, T("linen", "shade"))
    for ((ax, ay), (bx, by)) in segments:
        if flip:
            ax, bx = -ax, -bx
        g.line(ox + ax, oy + ay, ox + bx, oy + by, T("skin", "lit"))


def downs(g, biomes):
    """The White Downs (stage_14): chalk downland on top of the white cliffs, its turf pale, chalk and flint showing
    through it; the barrows, round and long, around the seat; and the hound and the hart cut in the hillside. His
    mark is in the long barrow's mouth."""
    r = rng_for(2314)
    for _ in range(900):
        x = 2210 + r.random() * 300
        y = 60 + r.random() * 440
        if biomes[int(y) * W + int(x)] != "downs" or road_distance(x, y) < 5:
            continue
        if r.random() < 0.7:
            g.set(x, y, T("skin", "shade") if r.random() < 0.8 else T("skin", "base"))
        else:
            g.set(x, y, OCC)
            g.set(x - 1, y - 1, T("skin", "shade"))
    hill_figure(g, 2266, 236, HOUND)
    hill_figure(g, 2362, 324, HART, flip=True)
    for (bx, by, rx, ry) in ((2282, 270, 5, 3), (2372, 264, 6, 4), (2330, 268, 10, 6), (2304, 282, 8, 5),
                             (2354, 288, 8, 5), (2340, 308, 6, 4)):
        barrow(g, bx, by, rx, ry)
    # The long barrow, its mouth to the east: two stones either side of a dark door, and his eyes in the dark.
    barrow(g, 2296, 310, 16, 5)
    g.rect(2310, 308, 2314, 312, OCC)
    g.rect(2309, 306, 2309, 312, T("skin", "base"))
    g.rect(2315, 307, 2315, 312, T("skin", "deep"))
    _eyes(g, 2311, 310)


def far_water(g, biomes):
    """The far shore's water, before the road: the tide channels across the flats, standing pools left by the
    tide, and the still river by the avenue - still, so it carries no glint but the stars'."""
    # Each channel runs in from a sluice in the Weir (drowned()), where the sea would come down it if let in.
    for pts, w0, w1 in (([(2290, 572), (2330, 566), (2370, 584), (2404, 600), (2430, 614)], 4, 1),
                        ([(2306, 680), (2350, 700), (2398, 712), (2446, 700), (2498, 716), (2552, 706)], 5, 1),
                        ([(2360, 760), (2400, 770), (2440, 762), (2480, 772)], 4, 1),
                        ([(2290, 634), (2320, 624), (2346, 630)], 3, 1)):
        y0 = pts[0][1] - 4
        draw_water_path(g, [(int(far_west_edge(y0)) + 3, y0)] + pts, w0, w1)
    for (lx, ly, rx, ry, seed) in ((2362, 644, 7, 3, 2401), (2524, 604, 6, 3, 2402), (2566, 760, 8, 3, 2403),
                                   (2300, 606, 4, 2, 2404), (2420, 740, 5, 2, 2405), (2586, 662, 4, 2, 2406)):
        lake(g, lx, ly, rx, ry, seed)
    still = T("wool", "deep")
    draw_water_path(g, [(3014, 462), (2994, 512), (2966, 566), (2950, 616), (2956, 676), (2944, 726), (2934, 800)],
                    5, 10, water=still, lit=still, bank=OCC)
    r = rng_for(2409)
    for _ in range(26):
        t = r.random()
        y = 470 + t * 320
        x = 3010 - t * 70 + (r.random() - 0.5) * 6
        if g.get(x, y) == rgba(still):
            g.set(x, y, T("wool", "shade") if r.random() < 0.7 else T("skin", "shade"))


def wreck(g, cx, cy, ln, facing, seed):
    """A ship the sea left on the sand: a hull on its side, planks gone between its ribs, its mast snapped."""
    r = rng_for(seed)
    half = ln / 2.0
    g.disc(cx + 2, cy + 2, half + 1, 3, OCC)
    for xx in range(int(cx - half), int(cx + half) + 1):
        u = (xx - cx) / half
        depth = int(round(4 * math.sqrt(max(0.0, 1 - u * u))))
        for k in range(depth + 1):
            yy = cy - k
            rib = (xx - int(cx)) % 3 == 0
            if k == depth:
                c = T("gold", "shade") if u * facing < 0.4 else T("gold", "deep")
            elif rib:
                c = T("flesh", "shade")
            else:
                c = OCC if r.random() < 0.35 else T("gold", "deep")
            g.set(xx, yy, c)
    mx = int(cx + facing * 2)
    g.line(mx, cy - 4, mx + facing * 7, cy - 10, T("gold", "shade"))
    g.set(mx + facing * 7, cy - 10, T("gold", "base"))
    g.line(mx + 1, cy - 3, mx + facing * 7 + 1, cy - 9, OCC)


def drowned(g, biomes):
    """The Drowned Shore (stage_15): the seabed the tide left, south of the downs' scarp - wet sand ribbed by the
    sea that is gone, the channels it ran in, wrecks and a whale's ribs on the sand - and the sea standing off at
    the bay's mouth, held out by the Weir: a line of stakes and sluice gates along the flats' edge, and past it the
    water standing up in a ridge it cannot come over. His mark on a wreck."""
    r = rng_for(2475)
    for y in range(FLATS_TOP + 6, 800):
        for x in range(2236, FLATS_EAST + 40):
            if biomes[y * W + x] != "flats":
                continue
            if (y + int(GRAIN.at(x * 2, y * 2) * 5)) % 4 != 0 or (x + (y // 4) * 3) % 9 > 5:
                continue
            if road_distance(x, y) < 4 or g.get(x, y) in (rgba(WATER), rgba(WATER_LIT)):
                continue
            # A ridge of the sand the sea ribbed, wet, catching what light there is; its trough under it.
            g.set(x, y, T("skin", "deep") if dith(x, y, 0.6) else T("gold", "deep"))
            if g.get(x, y + 1) not in (rgba(WATER), rgba(WATER_LIT)):
                g.set(x, y + 1, T("stone", "deep"))
    wreck(g, 2474, 642, 22, 1, 2471)
    wreck(g, 2392, 726, 15, -1, 2472)
    wreck(g, 2550, 690, 11, 1, 2473)
    _eyes(g, 2470, 639)
    # The whale's ribs: a spine along the sand and the ribs standing up off it, bone white, lit on their tops.
    sx, sy = 2506, 676
    g.line(sx, sy, sx + 34, sy + 3, T("skin", "shade"))
    g.line(sx + 1, sy + 1, sx + 35, sy + 4, OCC)
    for k in range(7):
        bx = sx + 3 + k * 5
        by = sy + int(k * 0.4)
        h = 8 - abs(k - 2)
        for j in range(h):
            g.set(bx - j // 3, by - j, T("skin", "base") if j < h - 1 else T("skin", "lit"))
        g.set(bx + 1, by + 1, OCC)
    # The sea held off: across the bay's mouth, from the downs' corner off the bottom of the picture, the Weir - a
    # wall of stakes and dressed stone on the sand side with a sluice gate in it every so often - and on the sea
    # side the water standing up in a ridge it cannot come over, a crest of foam along its top.
    for i, y in enumerate(range(FLATS_TOP + 2, H)):
        ex = int(far_west_edge(y))
        gate = i % 46 < 5
        if not gate:
            g.set(ex + 1, y, T("stone", "lit") if y % 3 == 0 else T("stone", "base"))
            g.set(ex + 2, y, T("stone", "base") if y % 3 else T("stone", "shade"))
            g.set(ex + 3, y, OCC if y % 3 == 0 else T("stone", "deep"))
        elif i % 46 in (1, 3):
            g.set(ex + 1, y, T("steel", "shade"))
            g.set(ex + 2, y, T("steel", "base"))
        for k in range(0, 7):
            c = T("wool", "base") if k in (1, 2) else (T("wool", "shade") if k < 5 else T("wool", "deep"))
            if k == 0 and dith(ex, y, 0.6):
                c = T("skin", "shade")
            if k == 3 and dith(ex, y * 3, 0.3):
                c = T("skin", "deep")
            g.set(ex - k, y, c)


def _lane_track(g, path):
    """One of the three ways' roads: a pale dry track, two pixels wide, its ruts darker."""
    for (x, y) in path:
        g.set(x, y, T("linen", "base") if dith(x, y, 0.55) else T("gold", "shade"))
        g.set(x + 1, y, T("gold", "shade") if dith(x + 1, y, 0.5) else T("gold", "deep"))
        g.set(x, y + 1, T("gold", "deep"))


def _lane_hedges(g, path, open_at):
    """Yew hedge along both sides of a lane: dark, clipped and close, lit a little on top, open at every fork."""
    crowns = []
    for i in range(2, len(path) - 2, 3):
        (x0, y0), (x1, y1) = path[i - 2], path[i + 2]
        dx, dy = x1 - x0, y1 - y0
        n = math.hypot(dx, dy) or 1.0
        px_, py_ = -dy / n, dx / n
        for side in (-1, 1):
            hx, hy = path[i][0] + px_ * 4.5 * side, path[i][1] + py_ * 4.0 * side
            if road_distance(hx, hy) < 5.5 or any(math.hypot(hx - fx, hy - fy) < 8 for (fx, fy) in open_at):
                continue
            crowns.append((hy, hx))
    return crowns


def torch_stand(g, x, y):
    """A cold torch-stand at a fork: a post and its cup, unlit."""
    g.line(x, y, x, y - 5, T("stone", "lit"))
    g.set(x + 1, y + 1, OCC)
    g.set(x + 1, y, OCC)
    g.rect(x - 1, y - 6, x + 1, y - 6, T("skin", "deep"))
    g.set(x, y - 7, T("stone", "deep"))


def three_ways(g, biomes):
    """The Three Ways (stage_16): a dry night country of roads that fork in three, yew hedge between them, a
    three-faced pillar where they meet and a cold torch-stand at every fork - the hedged lanes branching three and
    three again, which is the chapter's own maze. His mark: the black dogs' eyes in a hedge."""
    r = rng_for(2642)
    for _ in range(700):
        x = 2520 + r.random() * 240
        y = 240 + r.random() * 260
        if biomes[int(y) * W + int(x)] != "ways" or road_distance(x, y) < 4:
            continue
        g.set(x, y, T("gold", "shade") if r.random() < 0.6 else T("stone", "base"))
    cx, cy = 2642, 370
    lanes = [spline([(2622, 433), (2627, 414), (2634, 392), (cx, cy + 3)], 1.0)]
    forks = [(cx, cy)]

    def branch(x, y, ang, ln, depth):
        bend = (r.random() - 0.5) * 0.5
        mx = x + math.cos(ang + bend) * ln * 0.5
        my = y + math.sin(ang + bend) * ln * 0.5 * 0.72
        ex = x + math.cos(ang) * ln
        ey = y + math.sin(ang) * ln * 0.72
        lanes.append(spline([(x, y), (mx, my), (ex, ey)], 1.0))
        if depth > 0:
            forks.append((ex, ey))
            for d in (-0.8, 0.0, 0.8):
                branch(ex, ey, ang + d, ln * 0.62, depth - 1)

    for ang in (-2.45, -0.72, 0.42):
        branch(cx, cy, ang, 50, 1)
    crowns = []
    for path in lanes:
        crowns += _lane_hedges(g, path, forks)
    # A hedge never stands on another lane's track: near a fork two lanes' hedges would close each other's way.
    track = set((int(round(x)), int(round(y))) for path in lanes for (x, y) in path)
    crowns = sorted(c for c in crowns
                    if not any((int(round(c[1])) + dx, int(round(c[0])) + dy) in track
                               for dx in (-2, -1, 0, 1, 2) for dy in (-2, -1, 0, 1, 2)))
    for path in lanes:
        _lane_track(g, path)
    for (hy, hx) in crowns:
        g.disc(hx + 1, hy + 1.5, 2.6, 2.0, OCC)
    for (hy, hx) in crowns:
        g.disc(hx, hy, 2.6, 2.1, T("lichen", "deep"))
        g.set(hx - 1, hy - 1, T("lichen", "shade"))
        g.set(hx, hy - 2, T("lichen", "shade") if dith(hx, hy, 0.5) else T("lichen", "deep"))
    for (fx, fy) in forks[1:]:
        torch_stand(g, int(fx) + 3, int(fy) + 1)
    # The pillar: three faces under one cap, pale stone, lit on the left, on a plinth.
    g.rect(cx - 3, cy + 1, cx + 3, cy + 2, T("skin", "deep"))
    g.rect(cx - 2, cy + 3, cx + 4, cy + 3, OCC)
    g.rect(cx - 1, cy - 9, cx + 1, cy, T("skin", "shade"))
    g.rect(cx - 1, cy - 9, cx - 1, cy, T("skin", "base"))
    g.rect(cx + 1, cy - 8, cx + 1, cy, T("skin", "deep"))
    g.rect(cx - 2, cy - 10, cx + 2, cy - 10, T("skin", "base"))
    for fy in (cy - 7, cy - 4):
        g.set(cx, fy, OCC)
    torch_stand(g, cx - 5, cy + 1)
    torch_stand(g, cx + 6, cy + 1)
    _eyes(g, 2667, 351)


def sphinx(g, x, y, facing):
    """A small crouched sphinx on the avenue: its body along the way, its head up, turned in to the way."""
    g.rect(x + 1, y + 1, x + 5, y + 1, OCC)
    g.rect(x, y - 1, x + 4, y, T("gold", "deep"))
    g.set(x, y - 1, T("gold", "shade"))
    hx = x if facing < 0 else x + 4
    g.rect(hx, y - 3, hx + (1 if facing > 0 else 1), y - 2, T("gold", "shade"))
    g.set(hx, y - 3, T("gold", "base"))


def obelisk(g, x, y, h):
    g.rect(x + 1, y + 1, x + 4, y + 1, OCC)
    for k in range(h):
        w = 1 if k > h - 4 else 2
        g.set(x, y - k, T("gold", "shade"))
        if w > 1:
            g.set(x + 1, y - k, T("gold", "deep"))
    g.set(x, y - h, T("gold", "base"))


def pylon(g, x, y):
    """A pylon gate, facing west down the avenue: two tapered towers and the dark door between them."""
    for (tx, w) in ((x, 9), (x + 14, 9)):
        for k in range(18):
            inset = k // 6
            for xx in range(tx + inset, tx + w - inset):
                c = T("gold", "shade") if xx <= tx + inset + 1 else T("gold", "deep")
                if k % 4 == 3 and xx > tx + inset + 1:
                    c = T("flesh", "deep")
                g.set(xx, y - k, c)
        g.rect(tx + 3, y - 18, tx + w - 4, y - 18, T("gold", "base"))
        g.rect(tx + w - 1, y - 14, tx + w, y + 1, OCC)
    g.rect(x + 9, y - 12, x + 13, y, OCC)
    g.rect(x + 8, y - 14, x + 14, y - 13, T("gold", "shade"))
    _eyes(g, x + 10, y - 6)


def twelfth_hour(g, biomes):
    """The Twelfth Hour (stage_17): black sand by the still river, an avenue of sphinxes running east to a pylon gate
    with an obelisk either side of it - in the dark: the sun did not rise. His mark is in the gate's door."""
    r = rng_for(2846)
    for _ in range(700):
        x = 2740 + r.random() * 230
        y = 530 + r.random() * 230
        if biomes[int(y) * W + int(x)] != "blacksand" or road_distance(x, y) < 4:
            continue
        g.set(x, y, T("stone", "shade") if r.random() < 0.8 else T("gold", "deep"))
    ax0, ax1, ay = 2818, 2900, 624
    for x in range(ax0, ax1):
        y = ay + (x - ax0) * 0.02
        g.set(x, y - 2, T("gold", "shade") if x % 4 else T("gold", "deep"))
        for k in (-1, 0, 1):
            g.set(x, y + k, T("flesh", "deep") if (x + k) % 5 == 0 else T("gold", "deep"))
        g.set(x, y + 2, OCC)
    for k, x in enumerate(range(ax0 + 4, ax1 - 6, 9)):
        y = ay + (x - ax0) * 0.02
        sphinx(g, x, int(y) - 5, 1)
        sphinx(g, x, int(y) + 7, 1)
    obelisk(g, 2898, 614, 18)
    obelisk(g, 2898, 640, 18)
    pylon(g, 2904, 630)


def cairn(g, x, y):
    g.disc(x + 2, y + 1, 4, 2, OCC)
    for (k, rr) in enumerate((3.4, 2.6, 1.8, 1.0)):
        g.disc(x, y - k * 2.5, rr, rr * 0.65, T("steel", "deep") if k % 2 == 0 else T("stone", "lit"))
        g.set(x - rr * 0.5, y - k * 2.5 - 1, T("steel", "shade"))
    g.set(x, y - 9, T("steel", "base"))


def tor(g, x, y, s, seed):
    """A granite tor: weathered blocks piled on the fell, the biggest at the back, each lit on its upper left with
    its shadow lower right (the crag's tor, in the fell's granite)."""
    r = rng_for(seed)
    blocks = [(x - 4 * s, y - 6 * s, 7 * s, 5 * s), (x + 5 * s, y - 2 * s, 6 * s, 4 * s), (x - 9 * s, y, 5 * s, 3.5 * s),
              (x + 1 * s, y + 2 * s, 5 * s, 3 * s)]
    for (bx, by, rw, rh) in blocks:
        bx += (r.random() - 0.5) * 2
        g.disc(bx + 2, by + 2, rw, rh, OCC)
        for yy in range(int(by - rh), int(by + rh) + 1):
            for xx in range(int(bx - rw), int(bx + rw) + 1):
                nx = (xx - bx) / float(rw)
                ny = (yy - by) / float(rh)
                d = nx * nx + ny * ny
                if d > 1.0:
                    continue
                k = -(nx + ny * 1.2)
                if k > 0.9 and dith(xx, yy, 0.6):
                    c = T("steel", "base")
                elif k > 0.2:
                    c = T("steel", "shade") if dith(xx, yy, 0.4 + k * 0.4) else T("steel", "deep")
                elif k > -0.8:
                    c = T("steel", "deep") if dith(xx, yy, 0.6 + k * 0.4) else T("stone", "lit")
                else:
                    c = T("stone", "base")
                if (int(yy - by + rh) % 4 == 0) and k < 0.6:
                    c = T("stone", "base")
                g.set(xx, yy, c)
        g.line(bx + rw * 0.3, by - rh * 0.6, bx + rw * 0.3 + 1, by + rh * 0.6, T("stone", "base"))


def split_pine(g, x, y, h):
    """A pine the lightning found: a bare trunk riven at the top into two, the wound pale."""
    g.line(x + 1, y + 1, x + 5, y + 1, OCC)
    g.line(x + 1, y, x + 1, y - h + 2, T("stone", "base"))
    g.line(x, y, x, y - h, T("linen", "base"))
    g.line(x, y - h, x - 3, y - h - 5, T("linen", "base"))
    g.line(x + 1, y - h, x + 3, y - h - 6, T("linen", "shade"))
    g.set(x, y - h + 1, T("skin", "shade"))
    g.set(x, y - h + 2, T("skin", "deep"))
    for k in range(3, h - 2, 3):
        g.line(x - 1, y - k, x - 3, y - k - 2, T("linen", "shade"))


def rune_pole(g, x, y):
    g.line(x, y, x, y - 8, T("linen", "base"))
    g.line(x - 2, y - 7, x + 2, y - 7, T("linen", "shade"))
    for k in (2, 4, 5):
        g.set(x, y - k, OCC)
    g.set(x + 1, y + 1, OCC)


def thunder_fell(g, biomes):
    """The Thunder Fell (stage_18): bare granite above the treeline under the far north's peaks, lichen on it,
    cairns, pines the lightning split, rune-poles; and over it the storm he told never to break - a cloud bank
    sitting on the fell, its shadow on the stone, and no rain and no lightning out of it. His mark by a cairn."""
    r = rng_for(2976)
    for _ in range(600):
        x = 2850 + r.random() * 260
        y = 110 + r.random() * 240
        if biomes[int(y) * W + int(x)] != "fell" or road_distance(x, y) < 4:
            continue
        g.set(x, y, T("lichen", "base") if r.random() < 0.6 else T("steel", "base"))
    for (x, y, s, seed) in ((2950, 232, 1.0, 2981), (3016, 214, 1.3, 2982), (3052, 278, 0.9, 2983),
                            (2902, 216, 0.8, 2984)):
        tor(g, x, y, s, seed)
    for (x, y) in ((2976, 252), (2992, 206), (3030, 246), (2924, 248), (3074, 230), (2962, 290)):
        cairn(g, x, y)
    _eyes(g, 2990, 220)
    for (x, y, h) in ((2940, 270, 13), (3038, 296, 15), (2898, 256, 12), (3080, 266, 14), (2986, 306, 11),
                      (3006, 270, 12)):
        split_pine(g, x, y, h)
    for (x, y) in ((2962, 224), (3000, 238), (2932, 236)):
        rune_pole(g, x, y)
    # The storm that will not break: a bank of cloud sitting on the fell's north side, under the peaks. Its shadow
    # first, on the stone.
    lobes = [(2944, 160, 20, 9), (2972, 150, 26, 11), (3004, 154, 24, 10), (3034, 160, 20, 9), (2988, 166, 28, 8),
             (2918, 168, 14, 7), (3060, 170, 16, 7), (2958, 172, 18, 6), (3020, 174, 20, 6)]
    for (lx, ly, rx, ry) in lobes:
        for yy in range(int(ly + 14), int(ly + 14 + ry * 1.2)):
            for xx in range(int(lx - rx), int(lx + rx)):
                if dith(xx, yy, 0.5) and ((xx - lx) / float(rx)) ** 2 + ((yy - ly - 14) / (ry * 1.2)) ** 2 < 1:
                    g.set(xx, yy, OCC)
    for (lx, ly, rx, ry) in sorted(lobes, key=lambda l: l[1]):
        for yy in range(int(ly - ry), int(ly + ry) + 1):
            for xx in range(int(lx - rx), int(lx + rx) + 1):
                nx = (xx - lx) / float(rx)
                ny = (yy - ly) / float(ry)
                d = nx * nx + ny * ny
                if d > 1.0:
                    continue
                k = -(nx * 0.6 + ny)
                if k > 0.7:
                    c = T("steel", "shade") if dith(xx, yy, 0.6) else T("stone", "lit")
                elif k > 0.0:
                    c = T("stone", "lit") if dith(xx, yy, 0.4 + k * 0.4) else T("stone", "base")
                elif ny > 0.55:
                    c = T("stone", "deep") if dith(xx, yy, 0.5) else OCC
                else:
                    c = T("stone", "base") if dith(xx, yy, 0.5 + k * 0.5) else T("stone", "shade")
                g.set(xx, yy, c)


def far_mountains(g, biomes):
    """The far north's peaks behind the fell and the downs, as the land's north wall is drawn."""
    r = rng_for(2707)
    peaks = []
    x = 2420 + r.random() * 20
    while x < 3110:
        for rank in range(2):
            by = 64 + math.sin(x / 80.0) * 12 + rank * 16 + (r.random() - 0.5) * 18
            h = 34 + r.random() * 40 - rank * 8 + (16 if 2880 < x < 3080 else 0)
            peaks.append((by, x + rank * 9, h * (1.0 + r.random() * 0.7), h))
        x += 18 + r.random() * 30
    peaks.sort()
    for by, px_, w, h in peaks:
        if road_distance(px_, by) < 22 or by > 140:
            continue
        peak(g, px_, by, w, h, 0.28, seed=int(px_ * 7 + by))


def far_groves(g, biomes):
    """Woods in the far shore's hill country, so the land between the seats has things standing on it."""
    r = rng_for(2717)
    trees = []
    for _ in range(24):
        cx = 2440 + r.random() * 680
        cy = 160 + r.random() * 600
        if biomes[int(cy) * W + int(cx)] != "hills" or road_distance(cx, cy) < 24:
            continue
        rad = 8 + r.random() * 16
        for _ in range(int(rad * rad / 9)):
            a = r.random() * 2 * math.pi
            d = math.sqrt(r.random()) * rad
            x = cx + math.cos(a) * d
            y = cy + math.sin(a) * d * 0.7
            if 0 <= x < W and 0 <= y < H and biomes[int(y) * W + int(x)] == "hills" and road_distance(x, y) > 7:
                trees.append((y, x, 3 + r.random() * 2.5))
    trees.sort()
    for (y, x, rad) in trees:
        crown(g, x, y, rad, "lichen", dark=r.random() < 0.6)


# THE BRONZE ISLE (stage_19): an island off the far shore's east coast, drawn as his rock is (an outline, a south
# face, the plateau), a god's workshop gone cold on it.
BRONZE_TOP = [(3200, 428), (3204, 404), (3220, 384), (3246, 372), (3276, 370), (3304, 378), (3326, 394),
              (3334, 418), (3328, 444), (3306, 458), (3272, 462), (3236, 458), (3210, 446)]
BRONZE_CLIFF = 16


def bronze_isle(g):
    """The Bronze Isle (stage_19): the god's workshop island gone cold - walled yards of bronze gone to verdigris,
    anvils as tall as houses, dead bellows, a row of his wheeled tripods, and Talos on the rim, a bronze giant with
    the cold mark in his eye (taken, as everything here is: the art ruling of .ai/second-half.md 4.2). No fire: this
    is a god's forge put out, not his. And the bronze bridge over the strait, its deck the road."""
    outline = _jagged(BRONZE_TOP, 3191, 5.0)
    xs = [p[0] for p in outline]
    x0, x1 = int(min(xs)), int(max(xs))
    for x in range(x0, x1 + 1):
        span = _poly_span(outline, x)
        if span is None:
            continue
        top, bottom = span
        for y in range(int(top) + 6, int(bottom) + BRONZE_CLIFF + 6):
            if dith(x + 6, y, 0.8):
                g.set(x + 6, y, OCC)
    for x in range(x0, x1 + 1):
        span = _poly_span(outline, x)
        if span is None:
            continue
        top, bottom = span
        drop = BRONZE_CLIFF + int((GRAIN.at(x * 3, 500) - 0.5) * 6)
        west = 1.0 - min(1.0, (x - x0) / 60.0)
        for k in range(drop):
            y = int(bottom) + k
            t = k / float(drop)
            streak = (x * 7 + int(GRAIN.at(x * 5, 3) * 7)) % 9 == 0
            if t > 0.86:
                c = OCC
            elif streak and t < 0.7:
                c = T("lichen", "shade") if dith(x, y, 0.6) else T("lichen", "base")
            else:
                c = T("stone", "lit") if dith(x, y, 0.3 + west * 0.4 - t * 0.4) else T("stone", "base")
            g.set(x, y, c)
        g.set(x, int(bottom) + drop, T("wool", "shade") if dith(x, 3, 0.45) else T("wool", "deep"))
    g.poly(outline, T("stone", "base"))
    for x in range(x0, x1 + 1):
        span = _poly_span(outline, x)
        if span is None:
            continue
        top, bottom = span
        for y in range(int(top), int(bottom) + 1):
            if dith(x * 3, y * 5, 0.3 + (GRAIN.at(x * 4, y * 4) - 0.5)):
                g.set(x, y, T("lichen", "deep"))
        g.set(x, int(top), T("stone", "lit") if x < (x0 + x1) // 2 else T("stone", "base"))
    # The yards' floors: bronze plates laid like flags, gone dark and green, between the walls.
    for x in range(3214, 3324):
        for y in range(392, 442):
            span = _poly_span(outline, x)
            if span is None or not (span[0] + 3 < y < span[1] - 2):
                continue
            if (x % 7 == 0 or y % 5 == 0) and dith(x, y, 0.6):
                g.set(x, y, T("stone", "shade"))
            elif dith(x * 3, y * 7, 0.22):
                g.set(x, y, T("gold", "deep"))
    # The yards' walls: bronze gone green on its top, its face toward us, a door gap in each.
    def wall(ax, ay, bx, by):
        steps = int(max(abs(bx - ax), abs(by - ay)))
        upright = abs(bx - ax) < abs(by - ay)
        for s in range(steps + 1):
            x = ax + (bx - ax) * s / float(max(1, steps))
            y = ay + (by - ay) * s / float(max(1, steps))
            if 0.42 < s / float(max(1, steps)) < 0.54:
                continue
            if upright:
                g.set(x, y, T("lichen", "lit") if int(y) % 4 else T("lichen", "base"))
                g.set(x + 1, y, T("lichen", "shade"))
                g.set(x + 2, y, OCC)
                continue
            g.set(x, y - 3, T("lichen", "lit") if int(x) % 3 else T("lichen", "base"))
            g.set(x, y - 2, T("lichen", "base") if dith(x, y, 0.5) else T("lichen", "shade"))
            g.set(x, y - 1, T("lichen", "shade"))
            g.set(x, y, T("gold", "deep") if dith(x, y, 0.5) else T("lichen", "deep"))
            g.set(x, y + 1, OCC)
    wall(3256, 394, 3256, 440)
    wall(3292, 394, 3292, 438)
    wall(3214, 444, 3322, 440)
    wall(3220, 392, 3316, 390)
    # Anvils, as tall as houses: an iron block on a waist, its horn to the west, lit along its face.
    for (ax, ay) in ((3236, 420), (3272, 416), (3306, 424)):
        g.rect(ax + 1, ay + 1, ax + 12, ay + 2, OCC)
        g.rect(ax + 3, ay - 3, ax + 8, ay, T("stone", "shade"))
        g.rect(ax, ay - 7, ax + 11, ay - 4, T("stone", "base"))
        g.rect(ax, ay - 7, ax + 11, ay - 7, T("stone", "lit"))
        g.line(ax - 3, ay - 6, ax, ay - 6, T("stone", "base"))
        g.set(ax - 3, ay - 7, T("stone", "lit"))
    # Dead bellows, and the row of his wheeled tripods by the north wall.
    for (bx, by) in ((3246, 404), (3300, 404)):
        g.poly([(bx, by), (bx + 7, by - 2), (bx + 7, by + 2)], T("flesh", "deep"))
        g.line(bx + 7, by - 2, bx + 10, by - 3, T("gold", "deep"))
    for k in range(6):
        tx, ty = 3262 + k * 7, 386
        g.line(tx, ty, tx - 2, ty + 4, T("gold", "deep"))
        g.line(tx, ty, tx + 2, ty + 4, T("gold", "deep"))
        g.line(tx, ty, tx, ty + 4, T("gold", "deep"))
        g.rect(tx - 2, ty - 2, tx + 2, ty - 1, T("gold", "shade") if k % 2 else T("lichen", "base"))
        g.set(tx - 2, ty - 2, T("gold", "base"))
        g.set(tx + 3, ty + 5, OCC)
    # Talos on the south-east rim: legs apart, the bronze lit on his left, green in his joints, his eye taken.
    tx, ty = 3318, 452
    g.line(tx + 2, ty + 1, tx + 9, ty + 1, OCC)
    g.line(tx, ty, tx, ty - 6, T("gold", "shade"))
    g.line(tx + 4, ty, tx + 4, ty - 6, T("gold", "deep"))
    g.rect(tx - 1, ty - 13, tx + 5, ty - 7, T("gold", "deep"))
    g.rect(tx - 1, ty - 13, tx, ty - 7, T("gold", "shade"))
    g.set(tx + 2, ty - 10, T("lichen", "base"))
    g.rect(tx + 1, ty - 17, tx + 3, ty - 14, T("gold", "shade"))
    g.set(tx + 3, ty - 16, T("arcane", "lit"))
    g.line(tx - 2, ty - 12, tx - 3, ty - 6, T("gold", "shade"))
    g.line(tx + 6, ty - 12, tx + 7, ty - 6, T("gold", "deep"))
    bronze_bridge(g)


def bronze_bridge(g):
    """The bridge to the isle: bronze girders gone green on squat piers, a lit rail on its north side, its deck the
    road; its shadow on the strait."""
    path = ROUTE[_route_index_near(BRONZE_FROM, "stage_18"):ROUTE_STOPS["stage_19"] + 1]
    for x in range(int(BRONZE_FROM[0]) - 4, int(BRONZE_TO[0]) + 4):
        y = _y_on(path, x)
        if y is None or not is_sea(x, int(round(y))):
            continue
        yi = int(round(y))
        pier = (x - int(BRONZE_FROM[0])) % 14 < 3
        for k in range(3, 6):
            if dith(x + 5, yi + k + 4, 0.7):
                g.set(x + 5, yi + k + 4, OCC)
        if pier:
            for k in range(3, 11):
                g.set(x, yi + k, T("lichen", "deep") if k < 10 else OCC)
            g.set(x, yi + 3, T("lichen", "base"))
        g.set(x, yi - 3, T("gold", "shade") if x % 2 else T("lichen", "lit"))
        g.set(x, yi - 2, T("lichen", "base"))
        for k in range(-1, 2):
            g.set(x, yi + k, T("gold", "deep") if (x + k) % 4 else T("lichen", "shade"))
        g.set(x, yi + 2, T("lichen", "shade"))
        g.set(x, yi + 3, OCC if not pier else T("lichen", "deep"))


# THE LIGHT GOING HOME (.ai/second-half.md 6.2): after Taken Back his tower's dark light is out and warm motes stream
# east off it over the dark sea. Both are the screen's, laid over the world at runtime for a save that has won the
# Throne (WorldMapView): tower-unlit.png, the tower as keep_tower(lit=False) draws it, cropped to what differs; and
# this path, which the motes follow from his crown out over the sea, over the white cliffs and on east over the seats
# his shadow still holds - into the dark past whatever the save has seen.
LIGHT_PATH = [(2000, 168), (2036, 146), (2090, 136), (2150, 142), (2206, 164), (2256, 196), (2300, 226),
              (2352, 236), (2420, 222), (2520, 210), (2640, 236), (2760, 250), (2880, 236), (3000, 200),
              (3120, 228), (3240, 300), (3330, 340)]


def tower_patch(lit, dark):
    """The box where the unlit tower differs from the lit one, and that box cut from the unlit picture."""
    xs, ys = [], []
    for y in range(100, 340):
        for x in range(1940, 2080):
            if lit.pix[y * W + x] != dark.pix[y * W + x]:
                xs.append(x)
                ys.append(y)
    x0, y0, x1, y1 = min(xs), min(ys), max(xs), max(ys)
    patch = Grid(x1 - x0 + 1, y1 - y0 + 1)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            patch.pix[(y - y0) * patch.w + (x - x0)] = dark.pix[y * W + x]
    return patch, (x0, y0)


# ---------------------------------------------------------------------------------------------
# THE EDGE OF THE WORLD: every side falls into the dark, by the vignette's darken-only rule.
# ---------------------------------------------------------------------------------------------
FALL = [T("stone", "deep"), OCC]


def edge_falloff(g):
    band = 40
    for y in range(H):
        for x in range(X_FROM, W):
            d = min(x, y, W - 1 - x, H - 1 - y)
            if d >= band:
                continue
            wob = (WARP_X.at(x * 3, y * 3) - 0.5) * 16
            t = 1.0 - (d + wob) / float(band)
            if t <= 0:
                continue
            t = min(1.0, t)
            want = OCC if t > 0.7 or dith(x, y, t * 1.6 - 0.35) else (T("stone", "deep") if dith(x, y, t * 1.5) else None)
            if want is None:
                continue
            cur = g.pix[y * W + x]
            if lum(want) < lum(cur):
                g.pix[y * W + x] = rgba(want)


# ---------------------------------------------------------------------------------------------
# THE PAGE: the Sketchbook's own sheet. Nothing joins it to the world any more - no road runs off
# the world's edge onto it - so its pencil road starts at its own torn edge.
# ---------------------------------------------------------------------------------------------
def page():
    pw, ph = PAGE_SIZE
    g = Grid(pw, ph)
    x0, y0, x1, y1 = PAGE_RECT
    # Its shadow on the dark, offset down and right.
    g.rect(x0 + 4, y0 + 5, x1 + 4, y1 + 5, OCC)
    paper = T("skin", "lit")
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            # A sheet lit from the upper left: the far corner falls a step.
            t = ((x - x0) / float(x1 - x0) + (y - y0) / float(y1 - y0)) / 2.0
            c = paper if not dith(x, y, (t - 0.72) * 1.6) else T("skin", "base")
            # Squared ruling, faint: grey-blue on the white, a step darker where the page turns away.
            if (x - x0) % 8 == 0 or (y - y0) % 8 == 0:
                c = T("skin", "base") if c == paper else T("skin", "shade")
            g.set(x, y, c)
    # The margin, in red.
    g.rect(x0 + 18, y0, x0 + 18, y1, T("red", "hi"))
    # Torn left edge: the page came out of a workbook.
    r = rng_for(1001)
    for y in range(y0, y1 + 1):
        bite = int(r.random() * 3)
        for x in range(x0, x0 + bite):
            g.set(x, y, None)
    # The curl at the lower right corner, and the tape at the top.
    for k in range(10):
        for j in range(10 - k):
            g.set(x1 - j, y1 - k, None if j < 10 - k - 1 else T("skin", "shade"))
    g.poly([(x1 - 10, y1), (x1, y1 - 10), (x1 - 9, y1 - 9)], T("skin", "base"))
    for (tx, ty) in ((x0 + 60, y0 - 4), (x1 - 40, y0 - 3)):
        g.rect(tx, ty, tx + 18, ty + 7, T("gold", "lit"))
        g.rect(tx, ty, tx + 18, ty, T("gold", "hi"))
        g.rect(tx, ty + 7, tx + 18, ty + 7, T("gold", "base"))
    # The pencil road: in from the torn edge, a little downhill, to where the wizard stands - a road
    # somebody drew on the paper. Graphite, hand-wobbled.
    sx, sy = PAGE_STAND
    pts = [(x0 + 2, sy + 22), (x0 + 30, sy + 16), (sx - 26, sy + 6), (sx, sy)]
    for i in range(len(pts) - 1):
        ax, ay = pts[i]
        bx, by = pts[i + 1]
        steps = int(max(1, math.hypot(bx - ax, by - ay)))
        for s in range(steps):
            t = s / float(steps)
            x = ax + (bx - ax) * t
            y = ay + (by - ay) * t + math.sin((x) / 9.0) * 0.6
            on_paper = x0 <= x <= x1 and y0 <= y <= y1
            ink = T("stone", "shade") if on_paper else T("skin", "shade")
            g.set(x, y, ink)
            if int(x) % 5 != 0:
                g.set(x, y + 1, ink)
    # A little pencil lying on the page, sharpened.
    px, py = x0 + 120, y1 - 30
    for k in range(26):
        g.set(px + k, py - k // 3, T("gold", "base"))
        g.set(px + k, py - k // 3 + 1, T("gold", "shade"))
        g.set(px + k + 1, py - k // 3 + 2, OCC)
    g.set(px - 1, py + 1, T("stone", "deep"))
    g.set(px - 2, py + 1, T("stone", "deep"))
    g.set(px, py, T("flesh", "lit"))
    return g


# ---------------------------------------------------------------------------------------------
# THE SMALL SPRITES
# ---------------------------------------------------------------------------------------------
def shroud(rx, ry, seed):
    """His fog over a locked place: a bank of dark, its edge broken by dither, lit a little on top."""
    w = int(rx * 2 + 8)
    h = int(ry * 2 + 8)
    g = Grid(w, h)
    r = rng_for(seed)
    blobs = []
    for _ in range(14):
        a = r.random() * 2 * math.pi
        d = math.sqrt(r.random()) * 0.6
        blobs.append((w / 2.0 + math.cos(a) * rx * d, h / 2.0 + math.sin(a) * ry * d,
                      rx * (0.35 + r.random() * 0.3), ry * (0.35 + r.random() * 0.3)))
    noise = Fbm(seed, 10, 2)
    for y in range(h):
        for x in range(w):
            dens = 0.0
            for (bx, by, brx, bry) in blobs:
                nx = (x - bx) / brx
                ny = (y - by) / bry
                dens = max(dens, 1.0 - math.sqrt(nx * nx + ny * ny))
            dens += (noise.at(x, y) - 0.5) * 0.5
            if dens <= 0:
                continue
            # A MIST, NOT A HOLE: it never covers more than about three pixels in four, so the place
            # shows through it, held. Solid occlusion here read as a pit in the map.
            if not dith(x, y, min(0.78, dens * 1.8)):
                continue
            # The top of the bank catches the key a little; its heart is the darkest.
            lit = (y < h * 0.55) and dith(x + 3, y + 1, 0.55 - dens)
            c = T("stone", "shade") if lit else (T("stone", "deep") if dens < 0.5 or dith(x, y, 0.5) else OCC)
            g.set(x, y, c)
    return g


def ring(state):
    """The mark under a stand point: a small worn circle on the ground."""
    g = Grid(20, 10)
    if state == "open":
        out, inner = T("gold", "shade"), T("gold", "base")
    elif state == "current":
        out, inner = T("gold", "base"), T("gold", "lit")
    else:
        out, inner = T("stone", "deep"), T("stone", "shade")
    for y in range(10):
        for x in range(20):
            nx = (x - 9.5) / 9.5
            ny = (y - 4.5) / 4.5
            d = nx * nx + ny * ny
            if 0.55 <= d <= 1.0:
                g.set(x, y, inner if (ny < 0 or nx < -0.6) else out)
    return g


def shadow():
    g = Grid(22, 7)
    for y in range(7):
        for x in range(22):
            nx = (x - 10.5) / 10.5
            ny = (y - 3) / 3.0
            if nx * nx + ny * ny <= 1:
                g.set(x, y, OCC)
    return g


def mark(kind):
    """The compass marks, 16x16: a lozenge in the stop's state, or the seat and the page."""
    g = Grid(16, 16)
    if kind in ("open", "cleared", "locked"):
        fill = {"open": T("gold", "base"), "cleared": T("gold", "lit"), "locked": T("stone", "base")}[kind]
        lit = {"open": T("gold", "lit"), "cleared": T("gold", "hi"), "locked": T("stone", "lit")}[kind]
        dark = {"open": T("gold", "shade"), "cleared": T("gold", "base"), "locked": T("stone", "shade")}[kind]
        for y in range(16):
            for x in range(16):
                d = abs(x - 7.5) + abs(y - 7.5)
                if d <= 7.6:
                    g.set(x, y, OCC)
                if d <= 6.2:
                    g.set(x, y, lit if (x + y) < 15 else dark)
                if d <= 4.6:
                    g.set(x, y, fill)
        if kind == "cleared":
            # A tick, dark in the light.
            for (x, y) in ((5, 8), (6, 9), (7, 10), (8, 9), (9, 8), (10, 7), (11, 6)):
                g.set(x, y, OCC)
        if kind == "locked":
            g.rect(6, 7, 9, 10, T("stone", "deep"))
            g.set(6, 6, T("stone", "deep"))
            g.set(9, 6, T("stone", "deep"))
            g.rect(6, 5, 9, 5, T("stone", "deep"))
    elif kind == "rush":
        # The seat: a dark crown on a lit plinth.
        for y in range(16):
            for x in range(16):
                d = abs(x - 7.5) + abs(y - 7.5)
                if d <= 7.6:
                    g.set(x, y, OCC)
                if d <= 6.2:
                    g.set(x, y, E["darkness"][3])
        for x in range(4, 12):
            g.set(x, 10, E["darkness"][1])
            g.set(x, 9, E["darkness"][2])
        for x in (4, 7, 8, 11):
            g.set(x, 6, E["darkness"][1])
            g.set(x, 7, E["darkness"][2])
            g.set(x, 8, E["darkness"][2])
    elif kind == "page":
        g.rect(2, 1, 13, 14, OCC)
        g.rect(3, 2, 12, 13, T("skin", "lit"))
        for y in range(3, 13, 3):
            g.rect(4, y, 11, y, T("wool", "lit"))
        g.rect(5, 2, 5, 13, T("red", "hi"))
        g.set(12, 13, T("skin", "base"))
        g.set(11, 13, T("skin", "base"))
        g.set(12, 12, T("skin", "base"))
    return g


def secret_icon(keepsake):
    """The card's icon for a stage secret (a warm glint, the tell of section 2 of stage-secrets.md)
    or a keepsake (a small locket on its chain). 32x32."""
    g = Grid(32, 32)
    if not keepsake:
        cx, cy = 15.5, 15.5
        for y in range(32):
            for x in range(32):
                dx = abs(x - cx)
                dy = abs(y - cy)
                # A four-point glint.
                if (dx < 1.6 and dy < 12) or (dy < 1.6 and dx < 12) or (dx + dy < 5.5):
                    g.set(x, y, OCC)
                if (dx < 0.6 and dy < 10.5) or (dy < 0.6 and dx < 10.5) or (dx + dy < 4):
                    g.set(x, y, E["light"][3] if dx + dy > 6 else E["light"][2])
                if dx + dy < 2.2:
                    g.set(x, y, E["light"][0])
        for (x, y) in ((7, 7), (24, 7), (7, 24), (24, 24)):
            g.set(x, y, E["light"][3])
    else:
        # The chain, then the locket: gold, lit from the upper left, a stone set in it.
        for k in range(10):
            a = math.pi * (0.15 + 0.7 * k / 9.0)
            x = 16 + math.cos(a) * 11
            y = 12 - math.sin(a) * 9
            g.set(x, y, T("gold", "base") if k % 2 else T("gold", "shade"))
        g.disc(16, 20, 8, 8, OCC)
        g.disc(16, 20, 7, 7, T("gold", "shade"))
        for y in range(13, 28):
            for x in range(9, 24):
                nx = (x - 16) / 7.0
                ny = (y - 20) / 7.0
                if nx * nx + ny * ny <= 1 and -(nx + ny) > 0.2:
                    g.set(x, y, T("gold", "base"))
                if nx * nx + ny * ny <= 1 and -(nx + ny) > 0.9:
                    g.set(x, y, T("gold", "lit"))
        g.disc(16, 20, 3, 3, T("red", "lit"))
        g.set(15, 19, T("red", "hi"))
        g.set(16, 11, T("gold", "lit"))
        g.rect(15, 11, 17, 13, T("gold", "base"))
    return g


def arrow(direction):
    """The step plates' chevron, 12x20."""
    g = Grid(12, 20)
    for y in range(20):
        t = abs(y - 9.5) / 9.5
        for x in range(12):
            u = x if direction == "right" else 11 - x
            edge = 2 + (1 - t) * 7
            if edge - 3.2 <= u <= edge:
                lit = y < 10
                g.set(x, y, T("gold", "lit") if lit else T("gold", "base"))
            elif edge < u <= edge + 1.2:
                g.set(x, y, OCC)
    return g


# ---------------------------------------------------------------------------------------------
# OUTPUT
# ---------------------------------------------------------------------------------------------
def save(g, name, scale=1):
    img = g.image()
    if scale != 1:
        img = img.resize((g.w * scale, g.h * scale), Image.NEAREST)
    path = os.path.join(OUT_DIR, name)
    img.save(path)
    return path


def audit(g, name):
    ok = raster.allowed()
    bad = {}
    for p in g.pix:
        if p[3] == 0:
            continue
        h = "#%02X%02X%02X" % (p[0], p[1], p[2])
        if h not in ok:
            bad[h] = bad.get(h, 0) + 1
    if bad:
        print("  %s: %d off-contract colours: %s" % (name, len(bad), sorted(bad.items(), key=lambda kv: -kv[1])[:6]))
        sys.exit(1)


def arc_lengths(points):
    out = [0.0]
    for i in range(1, len(points)):
        out.append(out[-1] + math.hypot(points[i][0] - points[i - 1][0], points[i][1] - points[i - 1][1]))
    return out


def write_json(tower_at=None, tower_size=None):
    stops = []
    for (sid, stand, focus, rx, ry) in PLACES:
        stops.append({
            "id": sid,
            "route_index": ROUTE_STOPS[sid],
            "stand": [stand[0], stand[1]],
            "focus": [focus[0], focus[1]],
            "shroud": [rx, ry],
            "shroud_image": ("shroud-%s.png" % sid) if rx > 0 else "",
            "on_map": stand[0] < W,
        })
    data = {
        "_generated_by": "tools/art/world_map.py - do not edit by hand; edit the generator and re-run",
        "size": [W, H],
        "route": [[round(p[0], 1), round(p[1], 1)] for p in ROUTE],
        "stops": stops,
        # The Sketchbook's page, its own sheet: every coordinate in the page image's own pixels.
        "page": {
            "image": "sketchbook.png",
            "size": [PAGE_SIZE[0], PAGE_SIZE[1]],
            "rect": [PAGE_RECT[0], PAGE_RECT[1], PAGE_RECT[2], PAGE_RECT[3]],
            "stand": [PAGE_STAND[0], PAGE_STAND[1]],
            "drawings": [{"texture": f, "at": [p[0], p[1]]} for (f, p) in PAGE_DRAWINGS],
        },
        # The light going home (.ai/second-half.md 6.2), laid over the world after Taken Back: the tower unlit, at
        # its top-left in world pixels, and the path the warm motes stream east along off his crown.
        "light_home": {
            "tower_image": "tower-unlit.png",
            "tower_at": [tower_at[0], tower_at[1]] if tower_at else [0, 0],
            "tower_size": [tower_size[0], tower_size[1]] if tower_size else [0, 0],
            "motes": [[p[0], p[1]] for p in LIGHT_PATH],
        },
    }
    # One key per line and one stop per line, the route on one line of its own: readable in a diff
    # where it matters (the stops) and not 2500 lines of coordinates where it does not.
    lines = ["{"]
    keys = sorted(data.keys())
    for i, k in enumerate(keys):
        tail = "," if i < len(keys) - 1 else ""
        v = data[k]
        if k == "stops":
            lines.append(' "stops": [')
            for j, s in enumerate(v):
                lines.append("  " + json.dumps(s, sort_keys=True) + ("," if j < len(v) - 1 else ""))
            lines.append(" ]" + tail)
        else:
            lines.append(" " + json.dumps(k) + ": " + json.dumps(v, sort_keys=True, separators=(",", ":")) + tail)
    lines.append("}")
    # Binary, so it is LF on Windows too (the repo's line endings), whichever Python writes it.
    with open(os.path.join(OUT_DIR, "worldmap.json"), "wb") as fh:
        fh.write(("\n".join(lines) + "\n").encode("ascii"))


def check_places():
    """The wizard waits at the door, never inside the fog: every stand point outside its shroud."""
    ok = True
    for (sid, stand, focus, rx, ry) in PLACES:
        if rx <= 0:
            continue
        d = ((stand[0] - focus[0]) / float(rx)) ** 2 + ((stand[1] - focus[1]) / float(ry)) ** 2
        if d < 0.8:
            print("  %s: the stand point is inside the shroud (%.2f)" % (sid, d))
            ok = False
    if not ok:
        sys.exit(1)


def main(argv=()):
    global X_FROM
    preview_far = "--preview-far" in argv
    if preview_far:
        X_FROM = 1800
    check_places()
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)
    print("world_map: land")
    # Decided on a 2-pixel grid: the nearest-seed search is the slow part, and a border that steps
    # in pairs of pixels is still a border drawn at the art's own grain.
    biomes = ["sea"] * (W * H) if preview_far else [None] * (W * H)
    others = [None] * (W * H)
    mixes = [0.0] * (W * H)
    for y in range(0, H, 2):
        for x in range(X_FROM, W, 2):
            # Past the cliffs it is all sea (the rock is drawn, not grown from seeds): no search there, until
            # the far shore, whose ground is grown from its own seeds.
            if x >= FAR_X0 and far_land(x, y):
                b, o, m = far_biome_pair(x, y)
            elif x > LAND_W + 90:
                b, o, m = "sea", None, 0.0
            else:
                b, o, m = biome_pair(x, y)
            for yy in (y, y + 1):
                for xx in (x, x + 1):
                    if xx < W and yy < H:
                        biomes[yy * W + xx] = b
                        others[yy * W + xx] = o
                        mixes[yy * W + xx] = m
    # The sea's line is cheap, so it is exact.
    for y in range(H):
        for x in range(X_FROM, W):
            if is_sea(x, y):
                biomes[y * W + x] = "sea"
                mixes[y * W + x] = 0.0
            elif biomes[y * W + x] == "sea":
                # Land the 2-pixel search called sea at a coast: the far shore's own ground there.
                b, o, m = far_biome_pair(x, y) if x >= FAR_X0 else biome_pair(x, y)
                biomes[y * W + x] = b
                others[y * W + x] = o
                mixes[y * W + x] = m
    g = Grid(W, H, OCC)
    paint_ground(g, biomes, others, mixes)
    shore(g, biomes)
    print("world_map: water")
    draw_water_path(g, STREAM, 2, 3)
    draw_water_path(g, RIVER, 4, 8)
    # Where the river comes up: a spring pool under the scarp, the cave's water returning.
    lake(g, 758, 468, 8, 4, 99)
    far_water(g, biomes)
    print("world_map: mountains")
    mountains(g, biomes)
    groves(g, biomes)
    print("world_map: road")
    draw_road(g, biomes)
    print("world_map: places")
    scatter_forest(g, biomes)
    elderbark(g)
    fields(g, biomes)
    broken_circle(g, biomes)
    cave(g)
    crag(g)
    swamp(g, biomes)
    ruins(g, biomes)
    frozen(g, biomes)
    sands(g, biomes)
    emberdeep(g, biomes)
    print("world_map: the far shore")
    # After every place on the land: crown() draws its lobes from one shared stream (_LOBE_RNG), so a far tree drawn
    # earlier would reshape every tree in the west wood. Drawn here, the land is the picture it was, pixel for pixel.
    far_mountains(g, biomes)
    far_groves(g, biomes)
    white_cliffs(g, biomes)
    downs_scarp(g)
    downs(g, biomes)
    drowned(g, biomes)
    three_ways(g, biomes)
    twelfth_hour(g, biomes)
    thunder_fell(g, biomes)
    bronze_isle(g)
    print("world_map: the castle")
    black_cliffs(g, biomes)
    east_boulders(g, biomes)
    isle(g)
    last_bridge(g)
    causeway(g)
    # The castle twice: as it stands, his tower burning, and as it stands after Taken Back, his tower unlit. The
    # second is cut down to where the two differ (tower_patch) and laid over the first at runtime for a save that
    # has won the Throne (WorldMapView), so one picture serves both and the light going out is a picture, not a tint.
    dark = Grid(W, H)
    dark.pix = list(g.pix)
    castle(g)
    castle(dark, tower_lit=False)
    tower, tower_at = tower_patch(g, dark)
    dark = None
    edge_falloff(g)
    audit(g, "world")
    audit(tower, "tower-unlit")
    if preview_far:
        if not os.path.isdir(PREVIEW_DIR):
            os.makedirs(PREVIEW_DIR)
        g.image().crop((X_FROM, 0, W, H)).save(os.path.join(PREVIEW_DIR, "far.png"))
        print("world_map: preview only, tools/art/_worldmap/far.png (columns %d-%d); nothing written" % (X_FROM, W))
        return
    save(g, "world.png")
    save(tower, "tower-unlit.png")

    p = page()
    audit(p, "sketchbook")
    save(p, "sketchbook.png")

    for (sid, stand, focus, rx, ry) in PLACES:
        if rx <= 0:
            continue
        s = shroud(rx, ry, 1200 + len(sid) * 31 + int(focus[0]))
        audit(s, "shroud-" + sid)
        save(s, "shroud-%s.png" % sid)
    save(shadow(), "shadow.png")
    for state in ("open", "locked", "current"):
        save(ring(state), "ring-%s.png" % state)
    for kind in ("open", "cleared", "locked", "rush", "page"):
        m = mark(kind)
        audit(m, "mark-" + kind)
        save(m, "mark-%s.png" % kind, 2)
    for keep_ in (False, True):
        icon = secret_icon(keep_)
        audit(icon, "secret")
        save(icon, "keepsake.png" if keep_ else "secret.png", 2)
    for d in ("left", "right"):
        save(arrow(d), "arrow-%s.png" % d, 2)
    write_json(tower_at, (tower.w, tower.h))

    if not os.path.isdir(PREVIEW_DIR):
        os.makedirs(PREVIEW_DIR)
    # A preview with the road's stops ringed, for checking placement by eye (gitignored dir).
    prev = g.image().resize((W, H), Image.NEAREST)
    prev.save(os.path.join(PREVIEW_DIR, "world-1x.png"))
    print("world_map: %d route points, %d stops" % (len(ROUTE), len(PLACES)))


if __name__ == "__main__":
    main(sys.argv[1:])
