# -*- coding: utf-8 -*-
"""The Fallow Fields' second side event: the Tithe-Reeve's scale and his granary (.ai/chapter-wizards.md 4.2).

Run: python tools/art/tithe_scale.py

    world/granary/granary.png   the reeve's granary, three frames: barred, the bar fallen and the doors ajar, open
    world/granary/scale.png     his great scale, six frames: the beam tipping as the tithe pan fills, then dropped
    ui/poi/poi-scale.png        the arrow's pictogram for it
    tools/art/_tithe_scale_preview.png   (gitignored) for looking at

"He takes a tithe of everything that grows here." He counts Medea among what grows: she is held in the reeve's
granary against a tithe the village could never pay. TitheScaleEvent stands the two in a yard of stubble.

THE GRANARY. A raised timber granary of the old kind, up on staddle stones so the rats cannot climb to the grain:
weatherboard walls gone grey (linen's row, which reads on the gold stubble by hue), oak posts and sill, a hipped
roof of old thatch (gold's row, darker toward the eaves, ragged at its edge), a vent at the ridge her smoke leaks
from (the smoke is light, TitheScaleEvent draws it), and the double doors at the top of three plank steps, barred
by a heavy beam in iron brackets and padlocked. Beside the doors, in chalk, the reeve's tally: he counts. 96x96,
authored 1:1 and drawn at x2 by the event (as the hut is), the foot's middle on column 48, the ground on row 95.
The doors are 40 rows tall and she is about 42: she stoops through them, as she stepped out of the hut.
  frame 0  barred: the beam across both doors, the lock on it
  frame 1  the bar fallen to the steps, the doors ajar on the dark
  frame 2  open: the doors swung out, the dark inside and the sacks in it

THE SCALE. A great balance on an oak post bound with iron, on a footing of field stones: an iron beam on a pin at
the post's head, a pan hung on three chains at each end - the TITHE pan on the left, empty, and on the right the
reeve's weight, a squat iron block with his notch cut in it. Iron and oak and nothing lit: the light the pan
gathers is code (the event's glow). 64x80, column 32, row 79. The beam's geometry is TitheScaleEvent's (PIVOT,
ARM, HANG, TILTS below are Pivot, Arm, Hang and Tilts there - change one, change both): frames 0-4 tip the tithe
pan from high toward level as it fills, frame 5 is the beam dropped, the pan down and the weight up.
"""
from __future__ import division, print_function

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

import bonelight as bl
import raster

OCC = bl.OCC
RIM = bl.RIM
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "granary")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
PREVIEW = os.path.join(ROOT, "tools", "art", "_tithe_scale_preview.png")


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


GO = ramp("gold")      # oak (base / shade / deep) and thatch (lit / base / shade)
LN = ramp("linen")     # weathered boards
STL = ramp("steel")    # iron, field stones
SK = ramp("skin")      # chalk
ST = ramp("stone")     # the dark inside

# --- the granary --------------------------------------------------------------------------------

GW, GH = 96, 96
GMID = 48
FLOOR = 70            # the floor beam's top row
WALL_TOP = 30
DOOR_X0, DOOR_X1 = 38, 58
DOOR_TOP = 30
SILL_Y = 69           # TitheScaleEvent.Sill
STEP_FOOT_Y = 93      # TitheScaleEvent.StepFoot
VENT = (48, 18)       # TitheScaleEvent.Louvre


def staddle(c, x, salt):
    """One staddle stone: a tapering stem and a mushroom cap, lit toward the key."""
    for y in range(78, 93):
        hw = 2 + (y - 78) * 0.12
        for xx in range(int(round(x - hw)), int(round(x + hw)) + 1):
            u = (xx - x) / max(1.0, hw)
            c.set(xx, y, STL[2] if u < -0.3 else (STL[3] if u < 0.5 else STL[4]))
        c.set(int(round(x - hw)) - 1, y, OCC if y > 82 else STL[3])
    for xx in range(x - 6, x + 7):
        c.set(xx, 75, STL[1] if xx < x + 2 else STL[2])
        c.set(xx, 76, STL[2] if xx < x else STL[3])
        c.set(xx, 77, STL[4])
    c.set(x - 6, 76, STL[1])
    c.set(x + 6, 76, STL[4])
    c.set(x - 2 + salt % 3, 76, STL[3])                       # a chip out of the cap
    c.set(x + 1, 84 + salt % 4, STL[4])


def thatch(c):
    """The hipped roof seen from the long side: ridge short and high, the eaves long and low, old straw streaked
    from top to bottom, a stop darker toward the eaves, the edge ragged and its shadow on the boards."""
    ridge_y, eave_y = 6, 33
    for y in range(ridge_y, eave_y + 1):
        t = (y - ridge_y) / float(eave_y - ridge_y)
        x0 = int(round(26 - 20 * t))
        x1 = int(round(70 + 20 * t))
        for x in range(x0, x1 + 1):
            streak = (x * 7 + (y // 3) * 3) % 5
            if t < 0.35:
                tone = GO[1] if streak == 0 else GO[2]
            elif t < 0.75:
                tone = GO[2] if streak < 2 else GO[3]
            else:
                tone = GO[3] if streak < 3 else GO[4]
            # the hips: the two ends are turned away from the key, a stop down; the left one catches it
            if x < x0 + 3 + 6 * t:
                tone = GO[2] if streak else GO[1]
            elif x > x1 - 3 - 6 * t:
                tone = GO[4] if streak < 2 else GO[3]
            c.set(x, y, tone)
        c.set(x0 - 1, y, RIM if y < 20 else OCC)
        c.set(x1 + 1, y, OCC)
    # the ridge, bound with a twisted roll of straw
    for x in range(26, 71):
        c.set(x, ridge_y, GO[1] if x % 3 else GO[0])
        c.set(x, ridge_y - 1, GO[2] if x % 4 else GO[1])
    # the ragged eave and the shadow it throws on the wall below
    for x in range(6, 91):
        drop = (x * 5) % 3
        for k in range(drop):
            c.set(x, eave_y + 1 + k, GO[4])
        c.set(x, eave_y + 1 + drop, OCC)
    # the binding rows of the thatch (sways), two of them, and a sag patched with newer straw
    for y in (16, 25):
        for x in range(10, 87):
            if c.get(x, y)[3] and x % 2 == 0:
                c.set(x, y, GO[4])
    for (x, y) in ((60, 20), (61, 20), (62, 21), (59, 21), (60, 22)):
        c.set(x, y, GO[1])
    # the vent at the ridge's middle, under its own little hood of straw: a dark slot her smoke leaks from
    vx, vy = VENT
    c.rect(vx - 5, vy - 4, vx + 5, vy + 3, GO[3])
    for x in range(vx - 6, vx + 7):
        c.set(x, vy - 5, GO[1] if x < vx else GO[2])
    c.rect(vx - 3, vy - 2, vx + 3, vy + 2, OCC)
    for x in (vx - 2, vx, vx + 2):
        c.vline(x, vy - 2, vy + 2, GO[4])                    # the slats


def boards(c):
    """The walls: weatherboard, each board four rows with a lit lip and a dark seam, gone grey; oak corner posts,
    a midrail, and the odd board sprung or missing."""
    for y in range(WALL_TOP, FLOOR):
        board = (y - WALL_TOP) % 4
        for x in range(14, 83):
            if board == 0:
                tone = LN[1] if x < 40 else LN[2]
            elif board == 3:
                tone = LN[4]
            else:
                tone = LN[2] if (x * 3 + y) % 11 else LN[3]
            c.set(x, y, tone)
    for (x0, x1) in ((14, 16), (80, 82), (34, 36), (60, 62)):
        for y in range(WALL_TOP - 2, FLOOR):
            c.set(x0, y, GO[2] if x0 < 40 else GO[3])
            c.set(x0 + 1, y, GO[3])
            c.set(x1, y, GO[4])
    c.set(13, 40, OCC)
    for (x, y) in ((22, 46), (23, 46), (24, 46), (70, 58), (71, 58), (26, 62), (27, 62), (28, 62)):
        c.set(x, y, OCC)                                     # a missing board's gap
    for (x, y) in ((20, 37), (30, 41), (66, 37), (74, 49), (68, 61), (19, 57)):
        c.set(x, y, STL[3])                                  # nails, rusted dark
    # the reeve's tally in chalk on the boards right of the doors: fives, struck through, and more begun
    # Strokes on every other column, so a five reads as strokes and not a block of chalk.
    tx, ty = 64, 39
    for row, (groups, last) in enumerate(((1, 3), (1, 1))):
        y = ty + row * 8
        for group in range(groups + 1):
            strokes = 4 if group < groups else last
            for k in range(strokes):
                c.vline(tx + group * 9 + k * 2, y, y + 4, SK[1] if (k + row) % 2 == 0 else SK[2])
            if group < groups:
                c.line(tx + group * 9 - 1, y + 4, tx + group * 9 + 7, y, SK[0])


def sill_and_steps(c):
    """The floor beam the walls stand on, its ends on the staddles, and three plank steps up to the doors."""
    for y in range(FLOOR, FLOOR + 5):
        for x in range(10, 87):
            c.set(x, y, GO[2] if y == FLOOR else (GO[3] if y < FLOOR + 4 else GO[4]))
    c.set(10, FLOOR + 1, GO[1])
    for (x, y) in ((30, FLOOR + 2), (52, FLOOR + 1), (75, FLOOR + 3)):
        c.set(x, y, GO[4])                                   # checks in the oak
    # the steps' stringers, and the treads, worn hollow in the middle
    for k, (y, x0, x1) in enumerate(((FLOOR + 6, 39, 57), (FLOOR + 12, 38, 58), (FLOOR + 18, 37, 59))):
        for x in range(x0, x1 + 1):
            c.set(x, y, GO[1] if x < 44 else GO[2])
            c.set(x, y + 1, GO[3])
            c.set(x, y + 2, OCC if k < 2 else GO[4])
        c.set(x0 + 9, y, GO[2])
        c.set(x0 + 10, y, GO[2])
    for x in (37, 58):
        c.line(x, FLOOR + 5, x + (-1 if x < 48 else 1), STEP_FOOT_Y - 1, GO[3])
        c.line(x + 1, FLOOR + 5, x + 1 + (-1 if x < 48 else 1), STEP_FOOT_Y - 1, GO[4])


def doors(c, frame):
    """The double doors at the top of the steps, in three states."""
    x0, x1, y0, y1 = DOOR_X0, DOOR_X1, DOOR_TOP, SILL_Y
    # the frame round them, oak
    for y in range(y0 - 2, y1 + 1):
        c.set(x0 - 1, y, GO[2])
        c.set(x1 + 1, y, GO[4])
    c.hline(x0 - 2, x1 + 2, y0 - 2, GO[2])
    c.hline(x0 - 2, x1 + 2, y0 - 1, GO[3])
    if frame == 2:
        # open: the dark inside, sacks of grain slumped against the back wall, the doors swung out flat to the wall
        c.rect(x0, y0, x1, y1, OCC)
        for (sx, sy, w, h) in ((41, 54, 6, 15), (49, 57, 6, 12), (45, 61, 7, 8)):
            c.disc(sx + w / 2.0, sy + h / 2.0, w / 2.0, h / 2.0, GO[4])
            c.set(int(sx + 1), int(sy + 2), GO[3])
            c.set(int(sx + 2), int(sy + 1), GO[3])
        c.rect(x0, y0, x1, y0 + 3, ST[4])
        for (px0, px1) in ((x0 - 5, x0 - 2), (x1 + 2, x1 + 5)):
            for y in range(y0 - 1, y1 + 1):
                for x in range(px0, px1 + 1):
                    c.set(x, y, GO[2] if x == px0 else (GO[3] if x < px1 else GO[4]))
            c.hline(px0, px1, y0 + 6, STL[3])
            c.hline(px0, px1, y1 - 6, STL[3])
        return
    gap = 0 if frame == 0 else 3
    for side, (a, b) in enumerate(((x0, 47 - gap // 2), (49 + (gap + 1) // 2, x1))):
        for y in range(y0, y1 + 1):
            for x in range(a, b + 1):
                plank = (x - a) % 4
                tone = GO[2] if plank == 0 else (GO[4] if plank == 3 else GO[3])
                if side == 1 and plank == 0:
                    tone = GO[3]
                c.set(x, y, tone)
        # iron strap hinges, two a door, their nails
        for hy in (y0 + 6, y1 - 7):
            hx0, hx1 = (a, a + 7) if side == 0 else (b - 7, b)
            c.hline(hx0, hx1, hy, STL[2])
            c.hline(hx0, hx1, hy + 1, STL[3])
            c.set(hx0 + 2, hy, STL[1])
            c.set(hx1 - 2, hy, STL[1])
    if gap:
        c.rect(48 - gap // 2, y0, 48 + (gap + 1) // 2, y1, OCC)
    else:
        c.vline(48, y0, y1, OCC)
    if frame == 0:
        # the bar: a heavy oak beam across both doors in iron brackets, and the reeve's lock on it
        by = 47
        for y in range(by, by + 4):
            for x in range(x0 - 5, x1 + 6):
                c.set(x, y, GO[2] if y == by else (GO[3] if y < by + 3 else GO[4]))
        c.hline(x0 - 5, x1 + 5, by + 4, OCC)
        for bx in (x0 - 3, x1 + 2):
            c.rect(bx - 1, by - 1, bx + 1, by + 4, STL[3])
            c.set(bx - 1, by - 1, STL[1])
        c.rect(46, by + 3, 50, by + 8, STL[2])                # the lock
        c.hline(46, 50, by + 3, STL[1])
        c.set(48, by + 6, OCC)
        c.set(50, by + 7, STL[4])
        c.set(47, by + 2, STL[3])
        c.set(49, by + 2, STL[3])
    else:
        # the bar fallen: lying slant across the steps, one end on the ground
        for k in range(24):
            x = 34 + k
            y = 86 + int(k * 0.22)
            c.set(x, y, GO[2])
            c.set(x, y + 1, GO[3])
            c.set(x, y + 2, GO[4])
            c.set(x, y + 3, OCC)
        c.rect(44, 85, 47, 88, STL[2])                        # the lock, sprung
        c.set(44, 85, STL[1])


def granary(frame):
    c = raster.Canvas(GW, GH, None)
    c.disc(GMID, GH - 2.5, 44, 2.2, OCC)
    c.rect(12, 77, 84, 92, None)
    # the dark under the floor between the staddles, and the staddles themselves
    for y in range(75, 92):
        for x in range(12, 85):
            if (x + y) % 2 == 0 and y < 80:
                c.set(x, y, OCC)
    for i, x in enumerate((16, 32, 64, 80)):
        staddle(c, x, i)
    boards(c)
    thatch(c)
    sill_and_steps(c)
    doors(c, frame)
    # the key rim down the lit side of the building, where nothing stands beside it
    for y in range(34, 75):
        for x in range(0, GMID):
            if c.get(x, y)[3]:
                if not c.get(x - 1, y)[3]:
                    c.set(x - 1, y, RIM)
                break
    return c


# --- the scale ----------------------------------------------------------------------------------

SW, SH = 64, 80
SMID = 32
PIVOT = (32.0, 13.0)
ARM = 24.0
HANG = 27.0
TILTS = [-15.0, -11.5, -8.0, -4.5, -1.5, 21.0]


def chain(c, x0, y0, x1, y1):
    n = int(max(abs(x1 - x0), abs(y1 - y0)))
    for i in range(n + 1):
        t = i / float(max(1, n))
        x = int(round(x0 + (x1 - x0) * t))
        y = int(round(y0 + (y1 - y0) * t))
        c.set(x, y, STL[1] if i % 3 == 0 else (STL[3] if i % 3 == 1 else STL[2]))


def pan(c, cx, cy, weight):
    """A shallow iron dish on three chains. The tithe pan is empty and dark; the reeve's holds his weight."""
    for y in range(int(cy) - 1, int(cy) + 4):
        for x in range(int(cx) - 8, int(cx) + 9):
            nx = (x - cx) / 8.0
            ny = (y - cy - 0.5) / 3.0
            if nx * nx + ny * ny <= 1.0:
                if y <= cy:
                    tone = STL[4] if abs(nx) < 0.8 else STL[2]
                else:
                    tone = STL[2] if nx < -0.3 else (STL[3] if nx < 0.5 else STL[4])
                c.set(x, y, tone)
    c.hline(int(cx) - 8, int(cx) + 8, int(cy) - 1, STL[1])
    c.set(int(cx) - 8, int(cy), STL[1])
    c.hline(int(cx) - 5, int(cx) + 5, int(cy) + 4, OCC)
    if weight:
        # the reeve's weight: a squat iron block with a ring on top and his notch cut in its face
        c.rect(int(cx) - 4, int(cy) - 7, int(cx) + 4, int(cy) - 1, STL[3])
        c.hline(int(cx) - 4, int(cx) + 4, int(cy) - 7, STL[2])
        c.vline(int(cx) - 4, int(cy) - 7, int(cy) - 1, STL[2])
        c.vline(int(cx) + 4, int(cy) - 6, int(cy) - 1, STL[4])
        c.set(int(cx), int(cy) - 4, OCC)
        c.set(int(cx) + 1, int(cy) - 3, OCC)
        c.set(int(cx) - 1, int(cy) - 3, OCC)
        c.ring(cx, cy - 9, 2, 1.6, STL[2])


def scale(frame):
    c = raster.Canvas(SW, SH, None)
    c.disc(SMID, SH - 2.5, 16, 1.8, OCC)
    # the footing: field stones round the post's foot
    for (sx, sy, w, h, salt) in ((23, 71, 8, 6, 0), (30, 69, 6, 8, 1), (35, 71, 8, 6, 2), (27, 74, 6, 4, 3)):
        for y in range(sy, sy + h):
            for x in range(sx, sx + w):
                u = (x - sx) / float(max(1, w - 1))
                c.set(x, y, STL[1] if (u < 0.3 and y < sy + 2) else (STL[2] if u < 0.6 else STL[3]))
        c.hline(sx, sx + w - 1, sy + h, OCC)
        c.set(sx + 1 + salt, sy + 1, STL[3])
    # the post, oak, iron-bound
    for y in range(int(PIVOT[1]) + 2, 72):
        c.set(30, y, GO[2])
        c.set(31, y, GO[2])
        c.set(32, y, GO[3])
        c.set(33, y, GO[3])
        c.set(34, y, GO[4])
        c.set(29, y, RIM if y < 60 else OCC)
    for (x, y) in ((31, 24), (32, 40), (31, 56), (33, 33)):
        c.set(x, y, GO[4])                                   # checks in the grain
    for by in (28, 48, 64):
        c.hline(29, 35, by, STL[2])
        c.hline(29, 35, by + 1, STL[3])
        c.set(29, by, STL[1])
    # the fulcrum at the post's head: an iron saddle and its pin
    px, py = PIVOT
    c.rect(int(px) - 3, int(py) + 1, int(px) + 3, int(py) + 4, STL[3])
    c.hline(int(px) - 3, int(px) + 3, int(py) + 1, STL[2])
    a = math.radians(TILTS[frame])
    lx, ly = px - ARM * math.cos(a), py + ARM * math.sin(a)
    rx, ry = px + ARM * math.cos(a), py - ARM * math.sin(a)
    # the beam: iron, lit along its top, thickest at the pin
    n = 60
    for i in range(n + 1):
        t = i / float(n)
        x = lx + (rx - lx) * t
        y = ly + (ry - ly) * t
        thick = 2 if abs(t - 0.5) < 0.32 else 1
        c.set(int(round(x)), int(round(y)) - 1, STL[1])
        c.set(int(round(x)), int(round(y)), STL[2])
        if thick == 2:
            c.set(int(round(x)), int(round(y)) + 1, STL[3])
    c.set(int(px), int(py), STL[0])
    c.set(int(px), int(py) - 1, STL[1])
    # the ends: a hook each, and the chains down to the pans
    for (ex, ey, weight) in ((lx, ly, False), (rx, ry, True)):
        c.set(int(round(ex)), int(round(ey)) + 1, STL[3])
        bx, by = ex, ey + HANG
        for off in (-7, 0, 7):
            chain(c, ex, ey + 1, bx + off, by - 1)
        pan(c, bx, by, weight)
    return c


def scale_pan_positions():
    out = []
    for f in range(len(TILTS)):
        a = math.radians(TILTS[f])
        out.append((round(PIVOT[0] - ARM * math.cos(a), 2), round(PIVOT[1] + ARM * math.sin(a) + HANG, 2)))
    return out


# --- the arrow's pictogram ----------------------------------------------------------------------

GLYPH = [
    "............",
    ".....ss.....",
    ".sSSSSSSSSz.",
    ".z...gg...z.",
    ".z...gy...z.",
    "zSSz.gy.zSSz",
    ".zz..gy..zz.",
    ".....gy.....",
    ".....gy.....",
    "....sSSz....",
    "...SSzzZZ...",
    "............",
]
GLYPH_PAL = {"s": STL[1], "S": STL[2], "z": STL[3], "Z": STL[4], "g": GO[1], "y": GO[2]}


def glyph():
    c = raster.Canvas(12, 12, None)
    for y, row in enumerate(GLYPH):
        for x, ch in enumerate(row):
            if ch != ".":
                c.set(x, y, GLYPH_PAL[ch])
    return c


def main():
    for d in (OUT_WORLD, OUT_UI):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True
    sheets = []
    for name, maker, frames, w, h in (("granary", granary, 3, GW, GH), ("scale", scale, len(TILTS), SW, SH)):
        strip = Image.new("RGBA", (w * frames, h), (0, 0, 0, 0))
        for f in range(frames):
            c = maker(f)
            bad = c.audit()
            if bad:
                ok = False
                print("OFF-CONTRACT COLOURS in %s frame %d: %s" % (name, f, bad[:4]))
            strip.alpha_composite(c.img, (f * w, 0))
        strip.save(os.path.join(OUT_WORLD, name + ".png"))
        sheets.append(strip)
        print("  %-8s %d frames of %dx%d (drawn at x2)" % (name, frames, w, h))
    g = glyph()
    bad = g.audit()
    if bad:
        ok = False
        print("OFF-CONTRACT COLOURS in the scale glyph: %s" % bad[:4])
    g.scaled(2).save(os.path.join(OUT_UI, "poi-scale.png"))
    print("  pan bowls by frame (TitheScaleEvent.PanAuthored): %s" % scale_pan_positions())
    # the preview: both sheets on the Fields' stubble, at x3
    pw = max(s.size[0] for s in sheets) + 20
    ph = sum(s.size[1] for s in sheets) + 30
    sheet = Image.new("RGBA", (pw, ph), raster.rgb(GO[2]))
    y = 10
    for s in sheets:
        sheet.alpha_composite(s, (10, y))
        y += s.size[1] + 10
    sheet.resize((sheet.size[0] * 3, sheet.size[1] * 3), Image.NEAREST).save(PREVIEW)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
