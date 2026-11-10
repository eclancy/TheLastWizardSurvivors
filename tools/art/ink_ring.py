# -*- coding: utf-8 -*-
"""INK RING's ink (scripts/InkRing.cs): the stamps the game lays along the wizard's path, painted as watercolour.

Run: python tools/art/ink_ring.py [--preview=PATH]

Eric, 2026-11-09: "make sure the ink spells are a very different style of art than other parts of the game". Ink Ring is
the second thing Ma Liang's brush paints (the crow was the first), so it is held to the same rules - .ai/art-direction.md
section 3a, "Ink: the one sanctioned departure" - and painted by the same hand, tools/art/ink_wash.py. Nothing here picks
a colour: every pixel is ink_wash.paint's, and ink_wash.audit is the check.

THE LINE IS DRAWN LIVE, so it cannot be one picture. The game lays STAMPS along the path the wizard walked, one every
InkRing.SampleSpacing world units, and picks each one's age and heading. So this writes one sheet of stamps
(assets/bonelight/effects/ink-ring.png, authored 1:1 and drawn at x2 like every effect, section 1):

  THE LINE (rows 0-4, 16 px cells). A dab of a straight brush stroke through the middle of the cell, in eight headings
  (22.5 degrees apart: a stroke has no direction, so half a turn covers them), each cut as a WINDOW out of the middle of a
  much longer stroke - so a dab has no ends, no tide line across it, and dabs laid 6 art pixels apart run together into
  one line rather than a string of beads. Three windows along the long stroke (each its own grain) and two boils of each
  (the same stroke, its wet margins seeded differently: InkRing swaps them to make a held ring boil). Five rows, the
  ink AGEING: 0 wet from the brush (wide, dark, most of its edge still bleeding), 1 setting, 2 dried (the tide lines
  hard at both edges, the middle paler - the stage a held ring keeps), 3 a pale wash, 4 a ghost about to go.
  Column = heading * 6 + window * 2 + boil.

  THE FLOOD (y 80, 24 px cells, 4 columns: two shapes, two boils each). What the ring floods inward with when it closes
  (InkRing.Close): a blot of ink loaded into wet paper with a cauliflower edge. Four rows: 0 the wet blot with ink
  dropped into its middle, 1 the backrun (water ran back into it: a pale middle inside a ragged dark ring), 2 the dried
  wash (Inkwell's pool holds this one), 3 its ghost.

  THE SPLAT (x 96, y 80, 16 px cells, 4 shapes). A drop and the dots flung off it, under a body the ring struck (and, in
  the air, on it). Three rows: 0 wet, 1 dried, 2 ghost.

READABLE ON EVERY FLOOR (section 3a, "What still binds ink"): on a dark floor the pale middles of the strokes, the pale
washes and the bleed carry it; on a pale floor the tide lines and the pooled blacks do. --preview lays a ring of line
stamps, a flood and splats over the five floors ink_wash.swatch uses, at the game's x2, to look at.
"""
from __future__ import division, print_function

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import ink_wash as W
import raster

ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "assets", "bonelight", "effects", "ink-ring.png")

# --- the layout: InkRing.cs reads exactly these numbers ------------------------------------------------------------------
LINE_CELL = 16
HEADINGS = 8
WINDOWS = 3
BOILS = 2
LINE_STAGES = 5
FLOOD_CELL = 24
FLOOD_Y = LINE_CELL * LINE_STAGES          # 80
FLOOD_COLS = 4
FLOOD_STAGES = 4
SPLAT_CELL = 16
SPLAT_X = FLOOD_CELL * FLOOD_COLS          # 96
SPLAT_Y = FLOOD_Y
SPLAT_COLS = 4
SPLAT_STAGES = 3
SHEET_W = LINE_CELL * HEADINGS * WINDOWS * BOILS          # 768
SHEET_H = FLOOD_Y + FLOOD_CELL * FLOOD_STAGES               # 176

# How each age of the line is painted: (width in art pixels, the stroke's density, the share of its edge still wet, a
# thin wash rather than a stroke, alpha steps faded). Wet from the brush it is wide and bleeds; it dries to hard tide
# lines and a paler middle; then it thins to a wash and a ghost. The middle never goes darker than stop 6, so it reads
# on the Cursed Dungeon's black; the tide lines are stops 9-10, so it reads on snow.
LINE_AGES = [
    (4.2, 0.46, 0.78, False, 0),   # 0 wet
    (3.8, 0.42, 0.52, False, 0),   # 1 setting
    (3.4, 0.36, 0.28, False, 0),   # 2 dried (a held ring)
    (3.0, 0.24, 0.40, True, 1),    # 3 a pale wash
    (2.4, 0.16, 0.55, True, 3),    # 4 a ghost
]

# The flood's four ages: (radius, density, wet share, ink dropped into the middle, a backrun's radius share, fade).
FLOOD_AGES = [
    (7.6, 0.42, 0.96, 3.4, 0.0, 0),    # 0 the wet blot, ink dropped into it
    (8.4, 0.32, 0.92, 0.0, 0.6, 0),    # 1 the backrun
    (8.6, 0.24, 0.82, 0.0, 0.0, 1),    # 2 the dried wash (Inkwell's pool)
    (8.6, 0.14, 0.9, 0.0, 0.0, 3),     # 3 its ghost
]

# The splat's three ages: (density, wet share, fade).
SPLAT_AGES = [(0.72, 0.6, 0), (0.5, 0.3, 1), (0.3, 0.45, 3)]

BIG = 48        # the canvas a long stroke is painted on before a dab is cut out of its middle
WINDOW_STEP = 5  # how far along the long stroke each window sits from the last


def line_dab(heading, stage, window, boil):
    """One dab of the line: a window cut out of the middle of a long stroke, so it has no ends."""
    width, density, wet, wash, fade = LINE_AGES[stage]
    a = math.radians(heading * 180.0 / HEADINGS)
    ux, uy = math.cos(a), math.sin(a)
    px, py = -uy, ux
    c = (BIG - 1) / 2.0
    salt = 31 + heading * 7
    layers = {}
    for y in range(BIG):
        for x in range(BIG):
            dx, dy = x - c, y - c
            along = dx * ux + dy * uy
            across = dx * px + dy * py
            if abs(along) > BIG / 2.0 - 5:
                continue
            # The brush's pressure wanders a little along the stroke.
            half = width / 2.0 + 0.45 * (W.vnoise(along + 40.0, 7.0, salt, 6.0) - 0.5)
            if abs(across) <= half:
                layers[(x, y)] = [W.Layer(density, "line", "line", along, across, wash)]
    seed = W.salt_of("ink-line") + heading * 101 + stage * 13 + window * 5 + boil * 977
    canvas = W.paint(BIG, layers, seed=seed, soft_share=wet, margin=1)
    W.faded(canvas, fade)
    # The window: centred WINDOW_STEP * (window - 1) along the stroke from its middle, cut to the cell.
    off = (window - 1) * WINDOW_STEP
    cx, cy = c + ux * off, c + uy * off
    x0, y0 = int(round(cx - (LINE_CELL - 1) / 2.0)), int(round(cy - (LINE_CELL - 1) / 2.0))
    return canvas.img.crop((x0, y0, x0 + LINE_CELL, y0 + LINE_CELL))


def blob_layers(cell, cx, cy, r, density, salt, wash=True, group="flood"):
    """An irregular blot: its radius wanders round it, so its edge is a cauliflower, never a disc."""
    layers = {}
    for y in range(cell):
        for x in range(cell):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            ang = math.atan2(dy, dx)
            # Smooth round the blot: noise sampled on a circle, so the edge has no seam.
            rr = r * (0.72 + 0.5 * W.vnoise(3.0 + 2.2 * math.cos(ang), 3.0 + 2.2 * math.sin(ang), salt, 1.2))
            if dx * dx + dy * dy <= rr * rr:
                layers[(x, y)] = [W.Layer(density, group, group, x + salt * 3, y, wash)]
    return layers


def flood_blot(stage, shape, boil):
    r, density, wet, drop, bloom, fade = FLOOD_AGES[stage]
    c = FLOOD_CELL / 2.0
    salt = 211 + shape * 37
    layers = blob_layers(FLOOD_CELL, c, c, r, density, salt)
    drops = [(c - 0.6, c + 0.4, drop)] if drop > 0 else []
    # A backrun in one shape of the two, and only while the flood is still wet enough for water to run back into it: a
    # flood of them is then a wash with a bloom here and there, never a field of cobbles, and the dried pool is clean.
    blooms = [(c + 1.2, c - 0.8, r * bloom)] if bloom > 0 and stage == 1 and shape == 1 else []
    splats = []
    if stage == 0:
        for k in range(3):
            ang = W.hsh(shape, k, 41) * math.pi * 2.0
            d = r + 1.5 + W.hsh(shape, k, 43) * 1.5
            splats.append((c + math.cos(ang) * d, c + math.sin(ang) * d, 1 + (k == 0)))
    seed = W.salt_of("ink-flood") + stage * 17 + shape * 5 + boil * 977
    canvas = W.paint(FLOOD_CELL, layers, blooms=blooms, drops=drops, splats=splats, seed=seed, soft_share=wet, margin=1)
    return W.faded(canvas, fade).img


def splat(stage, shape):
    density, wet, fade = SPLAT_AGES[stage]
    c = SPLAT_CELL / 2.0
    salt = 401 + shape * 53
    layers = blob_layers(SPLAT_CELL, c, c, 2.8 + 0.4 * (shape % 2), density, salt, wash=False, group="splat")
    splats = []
    for k in range(4):
        ang = (W.hsh(shape, k, 61) + k) * math.pi * 0.5
        d = 4.2 + W.hsh(shape, k, 67) * 2.2
        splats.append((c + math.cos(ang) * d, c + math.sin(ang) * d, 2 if k % 2 == 0 else 1))
    seed = W.salt_of("ink-splat") + stage * 19 + shape * 7
    canvas = W.paint(SPLAT_CELL, layers, splats=splats, seed=seed, soft_share=wet, margin=1)
    return W.faded(canvas, fade).img


def sheet():
    from PIL import Image
    im = Image.new("RGBA", (SHEET_W, SHEET_H), (0, 0, 0, 0))
    for stage in range(LINE_STAGES):
        for h in range(HEADINGS):
            for w in range(WINDOWS):
                for b in range(BOILS):
                    col = h * WINDOWS * BOILS + w * BOILS + b
                    im.paste(line_dab(h, stage, w, b), (col * LINE_CELL, stage * LINE_CELL))
    for stage in range(FLOOD_STAGES):
        for v in range(FLOOD_COLS):
            im.paste(flood_blot(stage, v // 2, v % 2), (v * FLOOD_CELL, FLOOD_Y + stage * FLOOD_CELL))
    for stage in range(SPLAT_STAGES):
        for v in range(SPLAT_COLS):
            im.paste(splat(stage, v), (SPLAT_X + v * SPLAT_CELL, SPLAT_Y + stage * SPLAT_CELL))
    return im


# --- the preview ----------------------------------------------------------------------------------------------------------

FLOORS = [(11, 15, 24), (30, 38, 52), (36, 64, 74), (56, 64, 79), (221, 213, 192)]


def preview(im, path, radius=44, zoom=2):
    """A ring of line stamps aged from wet (its head) to a ghost (its tail), a closing flood inside a second ring and a
    few splats, over each floor, at the game's x2 - what the eye gets in a run, to look at."""
    from PIL import Image
    tile = 2 * radius + 40
    out = Image.new("RGBA", (tile * 3 * zoom, tile * len(FLOORS) * zoom), (0, 0, 0, 255))

    def stamp(dst, src_box, x, y):
        piece = im.crop(src_box)
        dst.alpha_composite(piece, (int(round(x - piece.size[0] / 2.0)), int(round(y - piece.size[1] / 2.0))))

    for fi, floor in enumerate(FLOORS):
        for col in range(3):
            t = Image.new("RGBA", (tile, tile), floor + (255,))
            cx = cy = tile / 2.0
            n = int(2 * math.pi * radius / 6.0)
            if col == 2:
                # The flood: blots inside the ring, the oldest (dried) at the rim, the wet ones reaching the middle.
                for gy in range(-4, 5):
                    for gx in range(-4, 5):
                        x, y = cx + gx * 14 + (gy % 2) * 7, cy + gy * 12
                        d = math.hypot(x - cx, y - cy)
                        if d > radius - 8:
                            continue
                        st = 0 if d < radius * 0.3 else (1 if d < radius * 0.6 else 2)
                        v = (gx * 3 + gy * 5) % FLOOD_COLS
                        x0, y0 = v * FLOOD_CELL, FLOOD_Y + st * FLOOD_CELL
                        stamp(t, (x0, y0, x0 + FLOOD_CELL, y0 + FLOOD_CELL), x, y)
            for k in range(n):
                a = k * 2 * math.pi / n
                x, y = cx + math.cos(a) * radius, cy + math.sin(a) * radius
                heading = (a + math.pi / 2) % math.pi
                h = int(round(heading / (math.pi / HEADINGS))) % HEADINGS
                if col == 0:
                    st = min(LINE_STAGES - 1, int(k / float(n) * LINE_STAGES))
                    st = LINE_STAGES - 1 - st
                else:
                    st = 2
                w = (k * 7) % WINDOWS
                cidx = h * WINDOWS * BOILS + w * BOILS
                stamp(t, (cidx * LINE_CELL, st * LINE_CELL, cidx * LINE_CELL + LINE_CELL, st * LINE_CELL + LINE_CELL), x, y)
            if col == 1:
                for k, (x, y) in enumerate(((cx - 10, cy - 6), (cx + 12, cy + 4), (cx, cy + 14))):
                    x0, y0 = SPLAT_X + k * SPLAT_CELL, SPLAT_Y
                    stamp(t, (x0, y0, x0 + SPLAT_CELL, y0 + SPLAT_CELL), x, y)
            out.paste(t.resize((tile * zoom, tile * zoom), Image.NEAREST), (col * tile * zoom, fi * tile * zoom))
    out.save(path)


def main(argv):
    im = sheet()
    bad = W.audit(im)
    if bad:
        print("OFF-INK COLOURS in ink-ring.png (.ai/art-direction.md section 3a):")
        for h, a, n in bad[:10]:
            print("   %s a=%d  x%d" % (h, a, n))
        return 1
    for a in argv:
        if a.startswith("--preview="):
            preview(im, a.split("=", 1)[1])
            print("preview", a.split("=", 1)[1])
    if "--no-write" not in argv:
        im.save(OUT)
        print("  %-40s %dx%d" % (os.path.relpath(OUT, ROOT), SHEET_W, SHEET_H))
    print("ink clean: every pixel an ink stop at an allowed alpha (ink_wash.audit)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
