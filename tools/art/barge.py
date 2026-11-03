# -*- coding: utf-8 -*-
"""The barge under the Last Bridge - Morgan le Fay's prison, and the Last Bridge's side event (BargeEvent).

Run: python tools/art/barge.py

Writes:
  world/barge/barge-full.png    88x88 cell, 4 frames: the black barge on the water with her standing in it
  world/barge/barge-empty.png   the same barge, empty, for after she has come ashore (it thins to mist)
  ui/poi/poi-barge.png          the arrow's pictogram: a barge with its one lantern lit

See .ai/chapter-wizards.md 4.9. "His seat is across the water": she is the one person who could take anyone over
without his bridge - the queen in the black barge who carried the dying king to Avalon - so he keeps her moored
under it, its one lantern lit.

ON THE BRIDGE'S DIAGONAL. The Last Bridge runs south-west to north-east, up and right on the screen at exactly 45
degrees, and the barge drifts along beside it toward the gate: so it is drawn lying along that diagonal, its bow
up and right, on the one angle a pixel grid draws without a stair (1:1 steps). Two axes, in diagonal steps from
the hull's middle: u along it (+1 = one pixel right and one up), v across it (+1 = one right and one down, toward
the viewer). Its near side - the planks under the near rail - drops straight down the screen, as every wall in this
game's three-quarter view does.

WHAT IT IS. A long low hull of black wood (the stone row, from its dark end), an old-gold line under each rail, a
pall of black samite (violet's deep end) heaped amidships with a gold fringe, a plain stern post, and a tall prow
post curled forward at its head, with the lantern hung from the curl on a short chain - an iron cage and in it the
one light, in wind's ramp, the strait's mist-green (BargeEvent.TellColor). She stands toward the stern: her own
sprite (roster.morgan), the rune gone from her hand, so she reaches out over the bow toward his castle across the
water. Below the hull, its reflection on the abyss - dark, broken into ripples, with the lantern's light in it.

THE FOUR FRAMES: the lantern breathing (its heart a stop brighter on two), the ripples under the hull sliding a
pixel along the hull each frame, and the whole barge lifting a pixel on the swell on frames 1 and 2. The node sorts
at the waterline under its middle, (44, 63) - BargeEvent.Anchor - and the lantern's heart is (76, 15), BargeEvent's
LanternCell; main() prints both, measured from what it drew.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image

import bonelight as bl
import pixel as P
import raster
import roster

ROOT = os.path.dirname(os.path.dirname(HERE))
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "barge")
OUT_UI = os.path.join(ROOT, "assets", "bonelight", "ui", "poi")
PREVIEW = os.path.join(HERE, "_barge_preview.png")

CELL = 88
CX, CY = 44, 50            # the hull's middle, on the deck
HALF = 27                  # half the hull's length, in diagonal steps: five of her heights end to end
BEAM = 6.0                 # half its beam at the middle, in diagonal steps
SIDE = 7                   # how far the near side drops below the rail, px: it hides her to the knee
ANCHOR = (44, 63)          # where it sorts: the waterline under its middle
LANTERN = (76, 15)         # the lantern's heart

# The lantern's light: wind's ramp, the strait's mist. Local to this sheet (it is not a player's sprite, so it
# is never in anim_sets' ramps), on characters the roster's palette does not use.
WD0, WD1, WD2, WD3 = "\xb0", "\xb1", "\xb2", "\xb3"
PAL = dict(roster.PALETTE)
PAL.update(dict(zip(WD0 + WD1 + WD2 + WD3, bl.ELEMENTS["wind"])))


def at(u, v):
    """The pixel u steps along the hull and v across it from its middle."""
    return CX + u + v, CY - u + v


def width(u):
    """Half the beam at u: full amidships, drawn in to a point at the bow and to a blunter end at the stern."""
    t = u / float(HALF)
    if t >= 0:
        return BEAM * max(0.0, 1.0 - t ** 1.6)
    return BEAM * max(0.0, 1.0 - (-t) ** 2.4)


def hull(g, back):
    """The hull: with back=True the far half and the deck (drawn behind her); with back=False the near rail, the
    near side, the pall and the posts (drawn over her). Every pixel decided from (u, v)."""
    for y in range(CELL):
        for x in range(CELL):
            du, dv = x - CX, y - CY
            # Each pixel is on a half-step lattice; u and v of its centre.
            u = (du - dv) / 2.0
            v = (du + dv) / 2.0
            if abs(u) > HALF:
                continue
            w = width(u)
            if w <= 0.3:
                continue
            if back:
                if -w <= v <= w - 0.5:
                    if v < -w + 1.2:
                        ch = "P"                                                   # the far rail's top, lit
                    elif v < -w + 2.2:
                        ch = "g" if int(u) % 6 else "G"                            # the gold line inside it
                    elif v < -w + 3.2:
                        ch = "j"                                                   # its inner face, in shadow
                    else:
                        # The deck: planks along the hull, a seam every other step across it.
                        ch = "j" if int(round(v * 2)) % 3 == 0 else "N"
                    g.set(x, y, ch)
            else:
                # The near rail: the top edge of the near side.
                if w - 0.5 < v <= w + 0.5:
                    g.set(x, y, "P")
                    # And the side under it, straight down the screen, to the water.
                    for k in range(1, SIDE + 1):
                        if k == 1:
                            ch = "G" if (x // 3) % 4 == 0 else "g"     # the gold line under the rail
                        elif k == 2:
                            ch = "p"
                        elif k < SIDE:
                            ch = "j" if (x + k) % 7 else "J"            # a plank seam now and then
                        else:
                            ch = "J"
                        if g.get(x, y + k) in (".", "o") or k > 1:
                            g.set(x, y + k, ch)
    if not back:
        # The bow's cutwater, and the stern's square end, closed down to the water.
        bx, by = at(HALF - 0.5, 0)
        for k in range(SIDE + 1):
            g.set(bx, by + k, "N" if k < 2 else "j")
        sx, sy = at(-HALF + 0.5, 0)
        for k in range(SIDE + 1):
            g.set(sx - 1, sy + k, "j")
            g.set(sx, sy + k, "N" if k < 2 else "J")


def pall(g):
    """A pall of black samite heaped amidships and forward, its hem along the near rail with a gold fringe."""
    for u in range(-1, 12):
        w = width(u)
        top = 4 + (1 if 1 < u < 9 else 0) + (1 if 3 < u < 7 else 0)
        for v in range(int(-w) + 1, int(w) + 1):
            x, y = at(u, v)
            h = top if abs(v) < w - 1 else top - 1
            for k in range(h):
                ch = ("V" if (u + v) % 4 == 0 else "w") if k == h - 1 else ("x" if k > 0 else "X")
                g.set(x, y - k, ch)
        # The fringe, where its hem lies over the near rail.
        x, y = at(u, w)
        g.set(x, y, "g" if u % 2 else "u")
    # A fold or two in it, the light along their tops.
    for (u, v) in ((3, -1), (4, -1), (7, 0), (8, 0)):
        x, y = at(u, v)
        g.set(x, y - 3, "X")
        g.set(x, y - 4, "w")


def posts(g):
    """The stern post, plain; the prow post rising tall and curling forward at its head, and the lantern hung
    from the curl on a short chain."""
    sx, sy = at(-HALF + 1, 0)
    for k in range(7):
        g.set(sx, sy - k, "N" if k % 3 else "p")
        g.set(sx + 1, sy - k, "j")
    g.set(sx, sy - 7, "u")
    bx, by = at(HALF - 1, 0)
    for k in range(15):
        g.set(bx, by - k, "p" if k % 4 == 1 else "N")
        g.set(bx + 1, by - k, "j")
    # The curl at its head, forward and over: carved, with old gold along it.
    for (dx, dy, ch) in ((0, -15, "N"), (1, -16, "p"), (2, -17, "p"), (3, -17, "N"), (4, -16, "N"), (5, -15, "j"),
                         (5, -14, "j"), (1, -15, "u"), (2, -16, "g"), (3, -16, "u"), (4, -15, "j")):
        g.set(bx + dx, by + dy, ch)
    # The chain, and the lantern: an iron cage with the light in it.
    lx, ly = LANTERN
    g.set(lx, ly - 5, "R")
    g.set(lx, ly - 4, "q")
    g.span(ly - 3, lx - 1, lx + 1, "R")
    for yy in (ly - 2, ly - 1, ly, ly + 1):
        g.set(lx - 2, yy, "R" if yy < ly else "q")
        g.set(lx + 2, yy, "q")
    g.span(ly + 2, lx - 1, lx + 1, "q")
    g.set(lx, ly + 3, "R")
    for yy in (ly - 2, ly - 1, ly, ly + 1):
        g.set(lx - 1, yy, WD2)
        g.set(lx, yy, WD1)
        g.set(lx + 1, yy, WD2 if yy < ly + 1 else WD3)
    g.set(lx, ly, WD0)
    g.set(lx, ly - 1, WD0)


def water(g, frame):
    """Its reflection on the abyss under the near side: a dark band broken into ripples that slide along the hull,
    and the lantern's light in the water under it."""
    for u in range(-HALF, HALF + 1):
        w = width(u)
        if w <= 0.3:
            continue
        x, y = at(u, w)
        base = y + SIDE + 1
        for k in range(3):
            if g.get(x, base + k) != ".":
                continue
            ripple = (u + frame + k * 2) % 5
            if k == 0:
                g.set(x, base, "o")
            elif ripple < 2:
                g.set(x, base + k, "J" if k == 1 else "j")
    # The lantern in the water, broken.
    lx, _ = LANTERN
    for (dx, dy) in ((0, 0), (1, 2), (-1, 4)):
        yy = LANTERN[1] + 22 + dy + (frame % 2)
        if 0 <= yy < CELL and g.get(lx - 4 + dx, yy) == ".":
            g.set(lx - 4 + dx, yy, WD3)


def her():
    """Her own sprite, as the character select shows her, without the rune: in the barge she is kept, not
    casting - reaching out over the bow toward his castle across the water."""
    rows = roster.morgan()
    out = []
    for line in rows:
        out.append("".join("." if ch in "<>?/" else ch for ch in line))
    return out


def frame(n, with_her):
    g = roster.G(CELL)
    lift = 1 if n in (1, 2) else 0
    hull(g, back=True)
    if with_her:
        rows = her()
        # Her feet (column 24, row 46 of her cell) on the deck amidships-aft.
        fx, fy = at(-11, 0)
        ox, oy = fx - 24, fy + 1 - 46
        for y, line in enumerate(rows):
            for x, ch in enumerate(line):
                # Her contact shadow is the deck's, not the floor's: dropped.
                if ch != "." and not (y >= 46):
                    g.set(x + ox, y + oy, ch)
    hull(g, back=False)
    pall(g)
    posts(g)
    if n in (1, 2):
        lx, ly = LANTERN
        g.set(lx - 1, ly, WD1)
        g.set(lx + 1, ly - 1, WD1)
    water(g, n)
    rows = g.rows()
    if lift:
        # The swell: everything above the waterline lifts a pixel; the reflection stays on the water.
        cut_row = ANCHOR[1] + 4
        rows = [rows[min(y + 1, CELL - 1)] if y < cut_row else rows[y] for y in range(CELL)]
    return rows


def glyph():
    """The pictogram: a barge low on the water, its prow post, and its one lantern lit."""
    art = [
        "............",
        ".........ab.",
        ".........a.l",
        ".........a.L",
        ".........a..",
        "......cc.a..",
        "....ccccca..",
        ".a.cccccccd.",
        ".dddddddddd.",
        "..bbbbbbbb..",
        "...w..w..w..",
        "............",
    ]
    pal = {"a": bl.tone("stone", "hi"), "b": bl.tone("gold", "lit"), "c": bl.tone("violet", "lit"),
           "d": bl.tone("stone", "lit"), "l": bl.ELEMENTS["wind"][0], "L": bl.ELEMENTS["wind"][2],
           "w": bl.ELEMENTS["wind"][3]}
    c = raster.Canvas(12, 12)
    for y, row in enumerate(art):
        for x, ch in enumerate(row):
            if ch != ".":
                c.set(x, y, pal[ch])
    for y in range(12):
        for x in range(12):
            if c.get(x, y)[3] == 0 and any(art[yy][xx] != "." for yy in range(max(0, y - 1), min(12, y + 2))
                                           for xx in range(max(0, x - 1), min(12, x + 2))):
                c.set(x, y, bl.OCC)
    return c


def main():
    for d in (OUT_WORLD, OUT_UI):
        if not os.path.isdir(d):
            os.makedirs(d)
    full = [frame(n, True) for n in range(4)]
    empty = [frame(n, False) for n in range(4)]
    P.write_strip(full, PAL, os.path.join(OUT_WORLD, "barge-full.png"), CELL)
    P.write_strip(empty, PAL, os.path.join(OUT_WORLD, "barge-empty.png"), CELL)
    g = glyph()
    bad = g.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in the glyph: %s" % bad[:4])
        return 1
    g.scaled(2).save(os.path.join(OUT_UI, "poi-barge.png"))

    filled, edges, ratio = roster.density(full[0])
    print("  barge-full 4 frames, barge-empty 4 frames, %d px, %.2f edges/px; anchor %s, lantern %s"
          % (filled, ratio, ANCHOR, LANTERN))

    # The preview: both strips at x4 on the abyss's near-black, and the full one at x2 beside a strip of deck.
    z = 4
    sheet = Image.open(os.path.join(OUT_WORLD, "barge-full.png"))
    sheet2 = Image.open(os.path.join(OUT_WORLD, "barge-empty.png"))
    w, h = sheet.size
    canvas = Image.new("RGBA", (w * z + 40, h * z * 2 + 60), raster.rgb(bl.tone("stone", "deep"))[:3] + (255,))
    canvas.alpha_composite(sheet.resize((w * z, h * z), Image.NEAREST), (20, 20))
    canvas.alpha_composite(sheet2.resize((w * z, h * z), Image.NEAREST), (20, 40 + h * z))
    canvas.alpha_composite(g.scaled(8), (20, 20))
    canvas.convert("RGB").save(PREVIEW)
    print("barge written -> %s" % os.path.relpath(OUT_WORLD, ROOT))
    return 0


if __name__ == "__main__":
    sys.exit(main())
