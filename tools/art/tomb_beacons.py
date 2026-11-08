# -*- coding: utf-8 -*-
"""The Scorched Sands' second side event art: the dry tomb and its bronze mirrors, where Imhotep is sealed
(.ai/chapter-wizards.md 4.6; the mirrors since 2026-11-08, .ai/unlock-puzzles.md 3.1, "route the sun").

Run: python tools/art/tomb_beacons.py

    world/tomb/tomb.png         the tomb, 88x72 a frame, four frames: sealed (his mark on the slab), the slab a
                                third aside, two thirds, and open on the stair going down into the dark
    world/tomb/mirror.png       a bronze mirror on a granite stand, 32x48 a frame, eight frames: facing east,
                                south-east, south (toward the camera), south-west, west, north-west, north, north-east -
                                SunMirrors' facings 0-7, in its order
    world/tomb/socket.png       the winged sun-disc over the tomb's door, 30x14 a frame, two frames: dark, and lit
    ui/poi/poi-tomb.png         the arrow's pictogram: a mirror on its stand with the sun's ray on it
    tools/art/_tomb_beacons_preview.png   (gitignored) for looking at

"He hid the water." The builder who could find water in the desert and raise it is sealed in a dry tomb beside
the road the water left. Sunlight falls through a gap in the dunes onto one bronze mirror; more stand on the
sand between it and the tomb. Turn them so the light goes mirror to mirror onto the sun-disc over the door, and the
slab slides aside (TombBeaconsEvent, SunMirrors).

world/tomb/beacon.png is no longer written or read: the three beacons were the ask until 2026-11-08, and the
mirrors replaced them (the file and its sidecar were removed with them).

THE TOMB IS BUILT, THE SEAL IS HIS. A mastaba front of sandstone courses - gold's dark end, the colour of the
dunes' stone in this drained light, never the Sands' sand cloth (cast_sands.py), which is what his chapter's dead
are wrapped in - a cornice over the door, sand drifted against both corners. The door is one slab of dark
granite (stone's row), and on it his mark: the one cold light, in the arcane ramp, where the far tell stands up
from. When the slab slides into the wall, the mark goes with it.

THE MIRRORS CARRY THE WIZARDS' LIGHT. A granite stand (the slab's stone, as the beacons' pillars were), and on it,
on a bronze spindle it turns about, a disc of polished bronze - gold's ramp, which is a light source and never
stepped (.ai/art-direction.md 2, "Matte, not wet"). Its face catches the key from the upper left; its back is dark
bronze with a boss and a ring. Seen in the 3/4 view the disc is an upright circle: full face toward the camera, an
ellipse at the diagonals, a sliver of rim edge-on, its back when it faces away - so its facing reads from the stand
as well as from the beam the game draws out of it. The sunlight it carries is drawn by the game, not here: a warm
gold line (the light element's ramp) in the air layer, and the light is the wizards' - warm - never his.

THE SOCKET is the winged sun-disc of a temple door, set on the cornice over the slab: dark relief in the
sandstone's shade until the light is on it, then the light's ramp in the disc and gold in the wings. Its middle is
on the tomb's row 32, 32 rows over the foot, and a mirror's disc is 32 rows over its own foot: both are 64 world px
up at x2 (SunMirrors.BeamHeight), so a beam from a mirror's face lands on the disc's middle.

NOT THE SEAL. The Ruins' stones turn a quarter when a plate is stepped on; these turn to face away from wherever he
stands, eight ways.

Every sheet is authored 1:1 and drawn by the game at x2 (Sprite2D Scale 2); the tomb and the mirrors sort with the
bodies at their feet. Python 2.7 and PIL, as every generator here.
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
import raster

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "tomb")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
PREVIEW = os.path.join(HERE, "_tomb_beacons_preview.png")


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


ST = ramp("stone")
GD = ramp("gold")
FIRE = bl.ELEMENTS["fire"]       # core, hot, mid, edge
ARC = bl.ELEMENTS["arcane"]      # his mark on the slab


def outline(c, skip_down=False):
    """A one-pixel occlusion edge round everything drawn, eight ways (four with skip_down: a thing standing on the
    ground has its contact shadow drawn separately)."""
    w, h = c.w, c.h
    occ = raster.rgb(OCC)
    solid = [[c.get(x, y)[3] != 0 for x in range(w)] for y in range(h)]
    for y in range(h):
        for x in range(w):
            if solid[y][x]:
                continue
            for dx in (-1, 0, 1):
                for dy in ((-1, 0) if skip_down else (-1, 0, 1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and solid[ny][nx] and c.get(nx, ny) != occ:
                        c.set(x, y, OCC)
                        break
                else:
                    continue
                break


# --- the tomb ----------------------------------------------------------------------------------------

TOMB_W, TOMB_H = 88, 72
TOMB_FOOT = 64          # the row the front's foot stands on: the sprite's sort point
TOMB_MIDDLE = 44
DOOR_X0, DOOR_X1 = 37, 51
DOOR_Y0 = 38
SLAB_TRAVEL = 15        # how far the slab slides into the wall when open


def tomb(frame):
    """The tomb's front, seen from a little above: the flat roof, the battered front of sandstone courses with
    the joints staggered, a cornice over the door, the door's slab of granite with his mark, sand drifted against
    both corners. frame 0 sealed, 1 and 2 the slab sliding into the right-hand wall, 3 open on the stair."""
    c = raster.Canvas(TOMB_W, TOMB_H)
    rng = random.Random(61)
    # THE ROOF: the flat top, in the key light, laid in big slabs.
    for y in range(10, 24):
        inset = (23 - y) * 0.35
        x0, x1 = int(round(9 + inset)), int(round(79 - inset))
        for x in range(x0, x1 + 1):
            c.set(x, y, GD[2])
        c.set(x0, y, GD[1])
        c.set(x1, y, GD[3])
    c.hline(13, 75, 10, GD[1])
    for x in range(13, 76, 3):
        c.set(x, 10, GD[2])                                 # the roof's edge worn, not a gilded line
    for (jx, y0, y1) in ((24, 11, 23), (41, 11, 23), (58, 11, 23), (32, 11, 16), (66, 17, 23)):
        for y in range(y0, y1 + 1):
            c.set(jx + (y - 11) // 6, y, GD[3])
    for y in (16, 17):
        c.hline(14, 74, y, GD[3] if y == 16 else GD[2])
    for _ in range(26):                                     # grit blown up onto it
        c.set(rng.randint(12, 76), rng.randint(11, 22), GD[1] if rng.random() < 0.6 else GD[3])
    # THE FRONT: battered, in shade (the key is from above and the left), courses of blocks.
    for y in range(24, TOMB_FOOT + 1):
        batter = (TOMB_FOOT - y) * 0.08
        x0, x1 = int(round(7 + batter)), int(round(81 - batter))
        for x in range(x0, x1 + 1):
            c.set(x, y, GD[3])
        c.set(x0, y, GD[2])
        c.set(x1, y, GD[4])
    c.hline(8, 80, 24, GD[1])                               # the roof's lip catching the light
    c.hline(8, 80, 25, GD[4])
    course = 0
    for y in range(31, TOMB_FOOT, 6):
        c.hline(9, 79, y, GD[4])
        c.hline(9, 79, y - 1, GD[3])
        course += 1
    for (cy0, offset) in ((26, 0), (31, 7), (37, 2), (43, 9), (49, 4), (55, 11), (61, 6)):
        x = 10 + offset
        k = 0
        while x < 79:
            for y in range(cy0, min(TOMB_FOOT, cy0 + 5)):
                c.set(x, y, GD[4])
            c.set(x + 1, cy0, GD[2])                        # each block's top edge lit beside its joint
            x += 11 + (k * 5 + offset) % 7
            k += 1
    for _ in range(40):                                     # weathering: pits and lighter grains
        x, y = rng.randint(10, 78), rng.randint(27, TOMB_FOOT - 1)
        if c.get(x, y) == raster.rgb(GD[3]):
            c.set(x, y, GD[4] if rng.random() < 0.55 else GD[2])
    # THE DOOR'S FRAME: a deep reveal, a lintel and the cavetto cornice over it.
    for y in range(DOOR_Y0 - 9, TOMB_FOOT + 1):
        for x in range(DOOR_X0 - 4, DOOR_X1 + 5):
            c.set(x, y, GD[3])
    for x in range(DOOR_X0 - 6, DOOR_X1 + 7):                # the cornice: a lit lip, a shaded curve under it
        c.set(x, DOOR_Y0 - 11, GD[1])
        c.set(x, DOOR_Y0 - 10, GD[2])
        c.set(x, DOOR_Y0 - 9, GD[3])
        c.set(x, DOOR_Y0 - 8, GD[4])
    c.set(DOOR_X0 - 6, DOOR_Y0 - 11, GD[0])
    for x in range(DOOR_X0 - 4, DOOR_X1 + 5):                # the torus moulding under the cornice
        c.set(x, DOOR_Y0 - 6, GD[1] if x % 2 else GD[2])
    for x in range(DOOR_X0 - 3, DOOR_X1 + 4):                # the lintel
        for y in range(DOOR_Y0 - 5, DOOR_Y0 - 1):
            c.set(x, y, GD[2])
        c.set(x, DOOR_Y0 - 5, GD[1])
        c.set(x, DOOR_Y0 - 1, GD[4])
    for (gx, gy) in ((DOOR_X0 + 1, DOOR_Y0 - 3), (DOOR_X0 + 4, DOOR_Y0 - 4), (DOOR_X0 + 7, DOOR_Y0 - 3),
                     (DOOR_X1 - 6, DOOR_Y0 - 3), (DOOR_X1 - 2, DOOR_Y0 - 4), (DOOR_X1 + 1, DOOR_Y0 - 3)):
        c.set(gx, gy, GD[3])                                 # cut signs on the lintel, worn
    for y in range(DOOR_Y0, TOMB_FOOT + 1):                   # the jambs, a lit edge on the left one
        c.set(DOOR_X0 - 1, y, GD[1])
        c.set(DOOR_X0 - 2, y, GD[2])
        c.set(DOOR_X1 + 1, y, GD[4])
        c.set(DOOR_X1 + 2, y, GD[3])
    # BEHIND THE SLAB: the stair going down into the dark - steps in stone's shade, darker the further down.
    for y in range(DOOR_Y0, TOMB_FOOT + 1):
        for x in range(DOOR_X0, DOOR_X1 + 1):
            c.set(x, y, OCC)
    for k, y in enumerate(range(TOMB_FOOT - 1, DOOR_Y0 + 6, -4)):
        tone = ST[2] if k == 0 else (ST[3] if k < 3 else ST[4])
        for x in range(DOOR_X0, DOOR_X1 + 1):
            c.set(x, y, tone)
        c.set(DOOR_X0, y, ST[1] if k < 2 else ST[3])
    # THE SLAB, sliding into the right-hand wall: granite, a lit left edge, chisel marks, his mark in the middle.
    shift = [0, 5, 10, SLAB_TRAVEL][frame]
    sx0, sx1 = DOOR_X0 + shift, DOOR_X1 + shift
    for y in range(DOOR_Y0, TOMB_FOOT + 1):
        for x in range(sx0, min(sx1, DOOR_X1 + 1 + (0 if frame < 3 else -99)) + 1):
            if x > DOOR_X1:
                break
            c.set(x, y, ST[2])
        if sx0 <= DOOR_X1:
            c.set(sx0, y, ST[1])
    if sx0 <= DOOR_X1:
        c.hline(sx0, DOOR_X1, DOOR_Y0, ST[1])
        for (mx, my) in ((3, 6), (9, 4), (5, 14), (11, 19), (2, 22), (8, 11)):
            x = sx0 + mx
            if x <= DOOR_X1:
                c.set(x, DOOR_Y0 + my, ST[3])
        # His mark: a ring with a stroke down through it, in his cold light. Only while the slab still covers the
        # door's middle; once it has slid past, the mark is inside the wall.
        mx, my = sx0 + 7, DOOR_Y0 + 11
        if mx + 2 <= DOOR_X1:
            for (dx, dy, col) in ((-1, -2, ARC[2]), (0, -2, ARC[1]), (1, -2, ARC[2]), (-2, -1, ARC[2]), (2, -1, ARC[3]),
                                  (-2, 0, ARC[2]), (2, 0, ARC[3]), (-1, 1, ARC[3]), (1, 1, ARC[3]), (0, 1, ARC[2]),
                                  (0, 0, ARC[0] if frame == 0 else ARC[1]), (0, -4, ARC[3]), (0, -3, ARC[2]),
                                  (0, 2, ARC[2]), (0, 3, ARC[3]), (0, 4, ARC[3])):
                if mx + dx <= DOOR_X1:
                    c.set(mx + dx, my + dy, col)
    # A shadow line where the slab meets the jamb it slides into.
    for y in range(DOOR_Y0, TOMB_FOOT + 1):
        c.set(DOOR_X1 + 1, y, GD[4])
    # SAND drifted against both corners and along the foot: lit on top, grains in it.
    for (cx, width, height) in ((10, 18, 12), (78, 16, 10), (30, 10, 4), (60, 12, 5)):
        for x in range(cx - width, cx + width + 1):
            t = 1.0 - abs(x - cx) / float(width)
            top = TOMB_FOOT + 1 - int(round(height * t * t * (3 - 2 * t)))
            for y in range(top, TOMB_FOOT + 3):
                if 0 <= x < TOMB_W:
                    c.set(x, y, GD[2] if y > top else GD[1])
    for _ in range(30):
        x, y = rng.randint(0, TOMB_W - 1), rng.randint(TOMB_FOOT - 10, TOMB_FOOT + 2)
        if c.get(x, y) in (raster.rgb(GD[2]),):
            c.set(x, y, GD[1] if rng.random() < 0.5 else GD[3])
    outline(c, skip_down=True)
    for x in range(4, TOMB_W - 4):                           # the contact shadow
        if c.get(x, TOMB_FOOT + 3)[3] == 0:
            c.set(x, TOMB_FOOT + 3, OCC)
    return c


# --- the mirrors -------------------------------------------------------------------------------------

MIRROR_W, MIRROR_H = 32, 48
MIRROR_FOOT = 44        # the row its base stands on: the sort point (TombBeaconsEvent's MirrorFoot)
MIRROR_MIDDLE = 16
FACE_Y = 12             # the disc's middle: 32 rows over the foot, 64 world px at x2 (SunMirrors.BeamHeight)
FACE_R = 8              # the disc's radius: 17 px across, 34 world px
FACE_TILT = 0.5         # how much of a line on the ground the 3/4 view shows (a disc's width seen across it)
FACE_THICK = 2.0        # the disc's thickness, which is all of it that shows edge-on
FACINGS = 8             # SunMirrors.Facings: 0 east, then a quarter-turn's half at a time clockwise on screen
EDGE_HALF = 2.6         # edge-on (east, west), the half-width the disc is drawn at - see _disc_point


def _disc_point(facing, s, t, w):
    """Where a point of the disc lands on the sheet: s across its face (-1 to 1), t up it (-1 to 1), w through its
    thickness (+ toward the side it faces). The disc stands upright, its face turned toward `facing` on the ground.

    Edge-on (east or west) the true projection is a line one thickness wide, which read as a pole, not a mirror. So
    there the disc is drawn as if turned a few degrees toward the camera: a narrow upright ellipse whose polished face
    shows on the side it points to and whose rim shows on the other - east and west are told apart at a glance."""
    a = facing * math.pi / 4.0
    nx, ny = math.cos(a), math.sin(a)                  # its facing, on the ground (y down: 2 is south, the camera)
    ux, uy = -ny * FACE_R, nx * FACE_R * FACE_TILT     # its horizontal diameter, seen from above and the south
    if abs(ny) < 0.01:
        ux, uy = nx * EDGE_HALF, 0.0
    px = MIRROR_MIDDLE + s * ux + w * nx
    py = FACE_Y + s * uy - t * FACE_R + w * ny * FACE_TILT
    return px, py


def mirror(facing):
    """A bronze mirror on a granite stand, its disc turned toward `facing` (0 east, 1 south-east, 2 south - toward the
    camera - 3 south-west, 4 west, 5 north-west, 6 north, 7 north-east). The disc stands upright on a bronze spindle
    and turns about it, so the stand never changes; only the disc does."""
    c = raster.Canvas(MIRROR_W, MIRROR_H)
    m = MIRROR_MIDDLE
    # THE STAND: two steps of cut granite, a squat pillar with its left face lit, a capital.
    for (y0, y1, hw) in ((41, MIRROR_FOOT, 8), (38, 40, 6)):
        for y in range(y0, y1 + 1):
            for x in range(m - hw, m + hw + 1):
                c.set(x, y, ST[2])
            c.set(m - hw, y, ST[1])
            c.set(m + hw, y, ST[3])
        c.hline(m - hw, m + hw, y0, ST[1])
        c.set(m - hw, y0, ST[0])
    for y in range(26, 38):
        hw = 3 if y > 29 else 2
        for x in range(m - hw, m + hw + 1):
            c.set(x, y, ST[1] if x < m else (ST[2] if x < m + 2 else ST[3]))
        c.set(m - hw, y, ST[0] if y % 4 else ST[1])
        c.set(m + hw, y, ST[4])
    c.hline(m - 2, m + 3, 32, ST[3])
    c.set(m + 1, 35, ST[3])
    for y in (23, 24, 25):                                  # the capital, wider than the shaft
        hw = 5 if y < 25 else 4
        for x in range(m - hw, m + hw + 1):
            c.set(x, y, ST[2])
        c.set(m - hw, y, ST[1])
        c.set(m + hw, y, ST[3])
    c.hline(m - 5, m + 5, 23, ST[1])
    c.set(m - 5, 23, ST[0])
    # THE SPINDLE: bronze, from the capital up to the disc's foot, a collar where it goes into the stone.
    for y in range(FACE_Y + FACE_R, 23):
        c.set(m, y, GD[2])
        c.set(m - 1, y, GD[1])
    c.hline(m - 2, m + 2, 22, GD[1])
    c.set(m + 2, 22, GD[3])

    # THE DISC. Its rim first, all the way through its thickness; then the side of it the camera sees - the polished
    # face when it is turned toward the south at all (or edge-on, see _disc_point), its back when it is turned away.
    a = facing * math.pi / 4.0
    toward = math.sin(a)                                # > 0: the face is turned toward the camera
    if abs(toward) < 0.01:
        toward = 0.2
    steps = 48
    for i in range(steps + 1):
        ang = 2.0 * math.pi * i / steps
        s, t = math.cos(ang), math.sin(ang)
        for k in range(5):
            w = -FACE_THICK / 2.0 + FACE_THICK * k / 4.0
            px, py = _disc_point(facing, s, t, w)
            # The rim's lit edge is its upper-left (the key), the rest bronze in shade.
            c.set(int(round(px)), int(round(py)), GD[2] if (s * -1 + t) > 0.3 and abs(toward) < 0.9 else GD[3])
    side = FACE_THICK / 2.0 if toward > 0 else -FACE_THICK / 2.0
    n = 40
    for i in range(-n, n + 1):
        for j in range(-n, n + 1):
            s, t = i / float(n), j / float(n)
            r = math.sqrt(s * s + t * t)
            if r > 1.0:
                continue
            px, py = _disc_point(facing, s, t, side)
            x, y = int(round(px)), int(round(py))
            dx, dy = px - m, py - FACE_Y
            if toward > 0:
                # The polished face: bronze lit, a bevel round it, the key's light across its upper left and a little
                # shade at its lower right - a plane of light, never a glint.
                if r > 0.84:
                    col = GD[2]
                elif dx + dy < -0.42 * FACE_R * 1.4:
                    col = GD[0]
                elif dx + dy > 0.62 * FACE_R * 1.4:
                    col = GD[2]
                else:
                    col = GD[1]
            else:
                # The back: dark bronze, a raised ring and a boss in the middle, the boss's top-left catching the key.
                if r > 0.86 or 0.5 < r < 0.6:
                    col = GD[4]
                elif r < 0.2:
                    col = GD[1] if (dx + dy) < 0 else GD[2]
                else:
                    col = GD[3]
            c.set(x, y, col)
    outline(c, skip_down=True)
    for x in range(m - 9, m + 10):                           # the contact shadow
        c.set(x, MIRROR_FOOT + 1, OCC)
    return c


# --- the socket --------------------------------------------------------------------------------------

SOCKET_W, SOCKET_H = 30, 14
SOCKET_MIDDLE_X, SOCKET_MIDDLE_Y = 15, 7      # its middle sits on the tomb's (TOMB_MIDDLE, 32)
LIGHT = bl.ELEMENTS["light"]                  # core, hot, mid, edge: the sun on the disc


def socket(lit):
    """The winged sun-disc over the door: a disc with a ring round it, and a wing either side of it, feathered in
    bands that taper and droop toward the tips. Dark, it is relief cut in the sandstone's shade; lit, the disc burns
    in the light's ramp and the wings in gold."""
    c = raster.Canvas(SOCKET_W, SOCKET_H)
    cx, cy = SOCKET_MIDDLE_X, SOCKET_MIDDLE_Y
    for side in (-1, 1):
        for k in range(10):
            x = cx + side * (5 + k)
            top = cy - 3 + k // 4                      # the upper edge falls toward the tip
            bottom = cy + 3 - k // 3                   # the lower edge rises: the wing tapers
            for y in range(top, bottom + 1):
                band = (y - top) % 2 == 0
                if lit:
                    col = GD[0] if y == top else (GD[1] if band else GD[2])
                else:
                    col = GD[2] if y == top else (GD[3] if band else GD[4])
                c.set(x, y, col)
    for y in range(cy - 5, cy + 6):
        for x in range(cx - 5, cx + 6):
            d = math.sqrt((x - cx) ** 2 + (y - cy) ** 2)
            if d > 5.2:
                continue
            if lit:
                col = LIGHT[0] if d < 1.6 else (LIGHT[1] if d < 3.0 else (LIGHT[2] if d < 4.2 else LIGHT[3]))
            else:
                col = GD[4] if d < 1.6 else (GD[3] if d < 4.2 else GD[2])
            if not lit and d < 4.2 and (x - cx) + (y - cy) < -2:
                col = GD[2]                            # the dark disc's upper left catching the key
            c.set(x, y, col)
    outline(c)
    return c


# --- the pictogram -----------------------------------------------------------------------------------

GLYPH = [
    "r...........",
    ".r..bbbb....",
    "..rbhhhhb...",
    "...rhhhhb...",
    "...bhhhlb...",
    "...bhhllb...",
    "....bbbb....",
    "......s.....",
    "......s.....",
    "......s.....",
    ".....sss....",
    "....sssss...",
]


def glyph():
    c = raster.Canvas(12, 12)
    pal = {"r": LIGHT[3], "b": GD[2], "h": GD[0], "l": GD[1], "s": ST[1]}
    for y, row in enumerate(GLYPH):
        for x, ch in enumerate(row):
            if ch != ".":
                c.set(x, y, pal[ch])
    outline(c)
    return c


# --- output ------------------------------------------------------------------------------------------

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
    for i, cv in enumerate(canvases):
        img.alpha_composite(cv.img, (i * w, 0))
    return img


def main():
    for d in (OUT_WORLD, OUT_UI):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True
    tombs = [tomb(f) for f in range(4)]
    mirrors = [mirror(f) for f in range(FACINGS)]
    sockets = [socket(False), socket(True)]
    for i, cv in enumerate(tombs):
        ok = _audit("tomb %d" % i, cv) and ok
    for i, cv in enumerate(mirrors):
        ok = _audit("mirror %d" % i, cv) and ok
    for i, cv in enumerate(sockets):
        ok = _audit("socket %d" % i, cv) and ok
    _strip(tombs).save(os.path.join(OUT_WORLD, "tomb.png"))
    _strip(mirrors).save(os.path.join(OUT_WORLD, "mirror.png"))
    _strip(sockets).save(os.path.join(OUT_WORLD, "socket.png"))
    g = glyph()
    ok = _audit("glyph tomb", g) and ok
    g.scaled(2).save(os.path.join(OUT_UI, "poi-tomb.png"))

    # The preview: the four tomb frames at x4, the eight mirror frames and the two sockets under them, then the
    # sealed tomb at x4 with the lit socket set on its cornice where the game sets it.
    bg = (46, 34, 18, 255)
    z = 4
    W = max(TOMB_W * z * 4, MIRROR_W * z * 8) + 40
    H = TOMB_H * z * 2 + MIRROR_H * z + SOCKET_H * z + 100
    sheet = Image.new("RGBA", (W, H), bg)
    for i, cv in enumerate(tombs):
        sheet.alpha_composite(cv.scaled(z), (10 + i * TOMB_W * z, 10))
    for i, cv in enumerate(mirrors):
        sheet.alpha_composite(cv.scaled(z), (10 + i * MIRROR_W * z, TOMB_H * z + 30))
    for i, cv in enumerate(sockets):
        sheet.alpha_composite(cv.scaled(z), (10 + i * (SOCKET_W + 4) * z, TOMB_H * z + MIRROR_H * z + 50))
    y0 = TOMB_H * z + MIRROR_H * z + SOCKET_H * z + 70
    sheet.alpha_composite(tombs[0].scaled(z), (10, y0))
    sheet.alpha_composite(sockets[1].scaled(z), (10 + (TOMB_MIDDLE - SOCKET_MIDDLE_X) * z, y0 + (32 - SOCKET_MIDDLE_Y) * z))
    sheet.alpha_composite(g.scaled(8), (W - 120, H - 110))
    sheet.convert("RGB").save(PREVIEW)
    print("  tomb 4, mirror 8, socket 2, glyph poi-tomb")
    if not ok:
        return 1
    print("tomb and mirrors written, palette clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
