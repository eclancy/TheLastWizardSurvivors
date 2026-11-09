# -*- coding: utf-8 -*-
"""Aegis Ward's Reforged shard. Run: python tools/art/aegis_ward.py

One shard of iron flying in to the wizard, drawn at 32 headings, written to assets/bonelight/effects/:

  aegis-shard.png    24x24 cell, 32 cells in one strip: cell i points at i x 11.25 degrees, measured
                     the way Godot measures an angle (0 = +X, clockwise on screen, since y is down)

The cell count is an API: scripts/AegisWard.cs reads it as ShardDirections and picks the cell nearest
the shard's heading - which is always straight in to him, from wherever round him it started.

WHY A SHEET (the pixel-art pass, 2026-11-01, .ai/art-replacement-manifest.md section 9). Reforged's
shards were a DrawColoredPolygon diamond with a DrawLine spine and a DrawLine streak at 60% alpha, all
of it fading by alpha: a smooth vector shape on a rotated grid at every heading but the four square
ones. Here the shard is rasterised per heading, as the Sleet Runes rune is (tools/art/sleet_runes.py,
whose reasoning this follows): every pixel on the world's 2-unit cell, every one an opaque ramp stop.

WHAT IT IS. "The ward's light, the plate's metal": a splinter of iron in Metal's ramp - the flank the
key light reaches (upper left, art-direction.md section 2) in its hot stop, a ridge of its mid down the
middle, the far flank in its edge - with a white-hot point (the contract: an element's core is white),
ringed in occlusion so it holds on any floor. Behind it a streak of the ward's Light, mid cooling to
edge, walked pixel by pixel and left bare, as the rune's wind is. Authored 1:1 and shown x2: 18 world
px long and 7 wide, about the old shard's 12 by 6, and well inside the swath it cuts (ShardHalfWidth
and a body either side of its line).
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bonelight as bl
import raster

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "assets", "bonelight", "effects")

TAU = math.pi * 2.0
OCC = bl.OCC
CELL = 24
DIRECTIONS = 32

LIGHT_CORE, LIGHT_HOT, LIGHT_MID, LIGHT_EDGE = bl.ELEMENTS["light"]
METAL_CORE, METAL_HOT, METAL_MID, METAL_EDGE = bl.ELEMENTS["metal"]

# The key light, in screen space: from the upper left.
KEY = (-1.0 / math.sqrt(2.0), -1.0 / math.sqrt(2.0))

# Shard space: u along the flight (it flies toward +u, in to him), v across it. The node's position is
# the middle of the shard.
TIP_U = 5.0                  # the point
SHOULDER_U, SHOULDER_HALF = 1.0, 1.7
TAIL_U, TAIL_HALF = -4.0, 0.6
POINT_FROM = 3.4             # past this, the white-hot point
RIDGE_HALF = 0.45            # the ridge down the middle

# The streak of light behind it: (u from, u to, colour), each walked as a one-pixel line.
STREAK = ((-5.5, -7.5, LIGHT_MID), (-8.5, -11.0, LIGHT_EDGE))

SUB = 4                      # 4x4 samples a pixel
COVER = 0.45                 # a pixel is in if this much of it is


def half_width(u):
    if SHOULDER_U <= u <= TIP_U:
        return SHOULDER_HALF * (TIP_U - u) / (TIP_U - SHOULDER_U)
    if TAIL_U <= u < SHOULDER_U:
        return TAIL_HALF + (SHOULDER_HALF - TAIL_HALF) * (u - TAIL_U) / (SHOULDER_U - TAIL_U)
    return -1.0


def inside(u, v):
    return abs(v) <= half_width(u)


def _stroke(x0, y0, x1, y1):
    """The pixels of a one-pixel line between two pixels, ends included (Bresenham)."""
    pts = []
    dx, dy = abs(x1 - x0), abs(y1 - y0)
    sx = 1 if x0 < x1 else -1
    sy = 1 if y0 < y1 else -1
    err = dx - dy
    x, y = x0, y0
    while True:
        pts.append((x, y))
        if x == x1 and y == y1:
            return pts
        e2 = 2 * err
        if e2 > -dy:
            err -= dy
            x += sx
        if e2 < dx:
            err += dx
            y += sy


def shard(angle, size=CELL):
    """One shard flying along `angle` (radians, Godot's convention), centred on a size x size canvas."""
    c = raster.Canvas(size, size, None)
    ca, sa = math.cos(angle), math.sin(angle)
    na = (-sa, ca)
    cx = cy = size / 2.0
    filled = {}
    for py in range(size):
        for px in range(size):
            n = 0
            for sy in range(SUB):
                for sx in range(SUB):
                    x = px + (sx + 0.5) / SUB - cx
                    y = py + (sy + 0.5) / SUB - cy
                    if inside(x * ca + y * sa, x * na[0] + y * na[1]):
                        n += 1
            if n < COVER * SUB * SUB:
                continue
            x = px + 0.5 - cx
            y = py + 0.5 - cy
            u = x * ca + y * sa
            v = x * na[0] + y * na[1]
            if u >= POINT_FROM:
                colour = LIGHT_CORE
            elif abs(v) <= RIDGE_HALF:
                colour = METAL_MID
            else:
                # Which flank, as a screen-space normal, against the key.
                side = 1.0 if v > 0.0 else -1.0
                normal = (na[0] * side, na[1] * side)
                lit = normal[0] * KEY[0] + normal[1] * KEY[1] > 0.0
                colour = METAL_HOT if lit else METAL_EDGE
            filled[(px, py)] = colour

    # Occlusion round the iron, one pixel, so it holds on pale ground as on the dungeon's
    # stone. Not round the streak behind it (sleet_runes.py says why).
    for (px, py) in list(filled):
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                q = (px + dx, py + dy)
                if q not in filled:
                    c.set(q[0], q[1], OCC)
    for (px, py), colour in filled.items():
        c.set(px, py, colour)

    def at(u):
        return int(math.floor(u * ca + cx)), int(math.floor(u * sa + cy))

    for (u0, u1, colour) in STREAK:
        a, b = at(u0), at(u1)
        for (x, y) in _stroke(a[0], a[1], b[0], b[1]):
            if (x, y) not in filled and c.get(x, y)[3] == 0:
                c.set(x, y, colour)
    return c


def strip():
    sheet = raster.Canvas(CELL * DIRECTIONS, CELL, None)
    for i in range(DIRECTIONS):
        k = shard(i * TAU / DIRECTIONS)
        for y in range(CELL):
            for x in range(CELL):
                p = k.get(x, y)
                if p[3] > 0:
                    sheet.set(i * CELL + x, y, p)
    return sheet


def main():
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)
    sheet = strip()
    bad = sheet.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in aegis-shard (art-direction.md section 3):")
        for h, n in bad[:10]:
            print("   %s  x%d" % (h, n))
        return 1
    path = os.path.join(OUT_DIR, "aegis-shard.png")
    sheet.scaled(2).save(path)
    print("  %-40s %dx%d (%d headings)" % ("aegis-shard", CELL * DIRECTIONS * 2, CELL * 2, DIRECTIONS))
    return 0


if __name__ == "__main__":
    sys.exit(main())
