# -*- coding: utf-8 -*-
"""The thaw's kantele: five strings of ice stretched over the frozen lake. Run: python tools/art/kantele.py

See .ai/unlock-puzzles.md 3.6, "sing it back". The ice sings a phrase - its strings light and sound in
turn - and he steps on them in the same order. A kantele is the instrument Vainamoinen made in the legend;
the old five-string one is what lies here, one string to a note, fanned out from the block he stands in to
the shore.

  world/thaw/thaw-strings.png   352x288, authored 1:1 over thaw-lake.png and drawn at x2 like it (ThawLake
                                lays it over the lake, centred on the same point). Transparent but for the
                                five strings, their shadows on the ice, the tuning pin at each string's
                                inner end and the ice cleat it is tied to at the shore.

THE STRINGS AT REST ARE STRETCHED OVER THE ICE: a pale steel highlight on the key-facing (upper-left) edge
and a 1 px shadow on the ice below and right, so each reads as raised over the lake rather than cut into it -
the lake's own cracks are cut in, dark and jagged; these are straight, raised and lit along one edge.

EACH STRING SHOWS ITS NOTE at rest, with the sound off: its body under the highlight is the note's own edge
tone, and its tuning pin and its stake's bead are the note's colour - five emissive ramps from the contract
(fire, lightning, grass, water, arcane - low to high, as light's own frequency runs). Only the edge tone, dark
and unlit, so the string lit by the song (core, mid and a glow, drawn by ThawEvent) is still far the brighter.
With the body steel and only the pins coloured, five pale lines read as one thing on a phone (2026-11-07,
compared side by side). The lowest strings are the thickest, as on any stringed instrument.
NEVER THE ORDER: nothing here or in the game marks which string comes first in a phrase (3.6's clash with
the Ruins' Broken Ward, which IS read from its stones).

What lights them while they sing - the string in its colour, shaking, and a glow on the light layer - is
drawn by ThawEvent from the same numbers, so this file only has to agree with it on where they lie:

  ANGLES   Kantele.AnglesDeg      one per string, degrees, screen space (y down), low to high clockwise
  INNER    Kantele.InnerRadius    the tuning pins, 80 px from the block in the world (40 here)
  OUTER    Kantele.OuterFraction  the cleats, 0.88 of the lake's ellipse (the shore is never under 0.92)

Change one, change both. RegressionChecks.ValidateThawStrings samples this texture along every string the
code lays down and says if one is not there.
"""
from __future__ import division, print_function

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

import bonelight as bl
import raster
import thaw_lake as lake

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "thaw")
PREVIEW = os.path.join(ROOT, "tools", "art", "_kantele_preview.png")

W, H = lake.W, lake.H
CX, CY = lake.CX, lake.CY
A, B = lake.A, lake.B          # the shore's semi-axes here (320 x 256 in the world)

# Kantele.AnglesDeg, Kantele.InnerRadius / 2, Kantele.OuterFraction - change one, change both.
ANGLES = [162.0, 234.0, 306.0, 18.0, 90.0]
INNER = 40.0
OUTER = 0.88

# Low to high: thickness in art pixels, and the note's emissive ramp (core, hot, mid, edge).
WIDTHS = [3, 3, 2, 2, 2]
NOTES = ["fire", "lightning", "grass", "water", "arcane"]

T = bl.tone
E = bl.ELEMENTS
OCC = bl.OCC

HI = T("steel", "hi")
LIT = T("steel", "lit")
BASE = T("steel", "base")
SHADOW = T("arcane", "deep")


def ellipse_r(theta):
    """The lake's ellipse radius along a bearing, in art pixels."""
    c, s = math.cos(theta), math.sin(theta)
    return A * B / math.sqrt((B * c) ** 2 + (A * s) ** 2)


def ends(i):
    """String i's inner and outer ends, in art pixels."""
    t = math.radians(ANGLES[i])
    d = (math.cos(t), math.sin(t))
    r_out = OUTER * ellipse_r(t)
    return ((CX + d[0] * INNER, CY + d[1] * INNER), (CX + d[0] * r_out, CY + d[1] * r_out))


def points(p0, p1):
    """The pixels of a line from p0 to p1 (Bresenham, as raster.Canvas.line draws it)."""
    x0, y0, x1, y1 = int(math.floor(p0[0])), int(math.floor(p0[1])), int(math.floor(p1[0])), int(math.floor(p1[1]))
    out = []
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    err = dx + dy
    while True:
        out.append((x0, y0))
        if x0 == x1 and y0 == y1:
            break
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy
    return out


def string_pixels(i):
    """{(x, y): colour} for string i's body: rows across it from the key-facing edge (upper left) down."""
    p0, p1 = ends(i)
    line = points(p0, p1)
    w = WIDTHS[i]
    horizontal = abs(p1[0] - p0[0]) >= abs(p1[1] - p0[1])
    # Across a mostly horizontal line the rows step in y, across a mostly vertical one in x; either way the
    # first row is the one toward the key light.
    step = (0, 1) if horizontal else (1, 0)
    # The key-facing edge is ice; the body under it is the note's own edge tone, so each string is its colour
    # end to end at rest, not only at its pin - darker and unlit, so the lit string (core, mid and glow) is
    # still the brighter thing by far.
    edge = E[NOTES[i]][3]
    rows = {3: [HI, edge, edge], 2: [HI, edge], 1: [HI]}[w]
    first = -(w // 2)
    body = {}
    for (x, y) in line:
        for k, c in enumerate(rows):
            o = first + k
            body[(x + step[0] * o, y + step[1] * o)] = c
    return body


def draw_pin(c, x, y, note):
    """The tuning pin at a string's inner end: a round bead in the note's colour, lit from the upper left."""
    ramp = E[note]
    c.disc(x, y, 3.2, 3.2, OCC)
    c.disc(x, y, 2.4, 2.4, ramp[3])
    c.disc(x - 0.6, y - 0.6, 1.5, 1.5, ramp[2])
    c.set(int(math.floor(x)) - 1, int(math.floor(y)) - 1, ramp[1])
    c.set(int(math.floor(x)) - 1, int(math.floor(y)) - 2, ramp[0])


# The stake at the shore a string is tied to: a shard of ice driven into the lake's edge, standing up out of
# it, its foot where the string ends. Offsets from that foot, outline first; lit on its key (left) face.
STAKE = [(-3, 1), (-3, -3), (-1, -8), (1, -9), (3, -4), (3, 1)]
STAKE_FILL = [(-2, 0), (-2, -3), (-1, -7), (1, -8), (2, -4), (2, 0)]


def draw_cleat(c, x, y, note):
    """The ice stake at the shore a string is tied to, with a bead of the note's colour where it is tied."""
    xi, yi = int(math.floor(x)), int(math.floor(y))
    # Its contact shadow on the ice, the shard, its lit face, then the bead at its foot.
    c.hline(xi - 2, xi + 4, yi + 2, OCC)
    c.poly([(xi + dx, yi + dy) for dx, dy in STAKE], OCC)
    c.poly([(xi + dx, yi + dy) for dx, dy in STAKE_FILL], BASE)
    c.line(xi - 2, yi - 1, xi - 2, yi - 3, LIT)
    c.line(xi - 1, yi - 4, xi - 1, yi - 7, HI)
    c.line(xi, yi - 7, xi, yi - 8, HI)
    c.line(xi + 1, yi - 2, xi + 1, yi - 5, LIT)
    ramp = E[note]
    c.disc(xi + 0.5, yi + 0.5, 1.8, 1.8, OCC)
    c.set(xi, yi, ramp[2])
    c.set(xi + 1, yi, ramp[3])
    c.set(xi, yi + 1, ramp[3])
    c.set(xi + 1, yi + 1, ramp[3])
    c.set(xi, yi - 1, ramp[1])


def draw():
    c = raster.Canvas(W, H)
    bodies = [string_pixels(i) for i in range(len(ANGLES))]
    taken = set()
    for b in bodies:
        taken.update(b.keys())
    # Shadows first, a pixel down and right of the body, wherever no string lies.
    for b in bodies:
        for (x, y) in b:
            if (x + 1, y + 1) not in taken:
                c.set(x + 1, y + 1, SHADOW)
    for b in bodies:
        for (x, y), col in b.items():
            c.set(x, y, col)
    for i in range(len(ANGLES)):
        p0, p1 = ends(i)
        draw_cleat(c, p1[0], p1[1], NOTES[i])
        draw_pin(c, p0[0], p0[1], NOTES[i])
    return c


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    c = draw()
    bad = c.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in the strings: %s" % bad[:5])
    c.save(os.path.join(OUT, "thaw-strings.png"))

    # Preview: the lake with the strings over it and the block in the middle, at x2.
    base = Image.open(os.path.join(OUT, "thaw-lake.png")).convert("RGBA")
    block = Image.open(os.path.join(OUT, "thaw-block.png")).convert("RGBA").crop((0, 0, 64, 64))
    pv = Image.new("RGBA", (W, H), raster.rgb(T("stone", "base")))
    pv.alpha_composite(base)
    pv.alpha_composite(c.img)
    pv.alpha_composite(block, (int(CX) - 32, int(CY) - 58))
    pv.resize((W * 2, H * 2), Image.NEAREST).convert("RGB").save(PREVIEW)

    for i in range(len(ANGLES)):
        p0, p1 = ends(i)
        print("string %d (%s, %d px): (%.1f, %.1f) -> (%.1f, %.1f), %.0f px long in the world"
              % (i, NOTES[i], WIDTHS[i], p0[0], p0[1], p1[0], p1[1],
                 2.0 * math.hypot(p1[0] - p0[0], p1[1] - p0[1])))
    return 0 if not bad else 1


if __name__ == "__main__":
    sys.exit(main())
