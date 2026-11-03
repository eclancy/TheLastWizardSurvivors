# -*- coding: utf-8 -*-
"""The Cursed Dungeon's secrets (.ai/achievements-and-secrets.md 8.5). Run: python tools/art/secret_dungeon.py

    world/dungeon/cold-forge.png         D5, the Cold Forge (the spell vault), three frames: the forge cold - a
                                         stone hearth under a sooted hood, its coal bed dead - and roaring, two
                                         frames of fire filling the throat under the hood. ColdForge.cs lights
                                         the embers in the dead coals one kill at a time, in code
    world/dungeon/cold-forge-anvil.png   the anvil on its block, in front of the hearth (the glint on its horn
                                         is drawn in code)
    world/dungeon/hearth-ring.png        the hearth ring laid in the floor in front of the anvil: its edge is
                                         the cull's edge (ColdForge.RingRadius), drawn from the same number
    world/dungeon/shackles-1.png, -2     its far tell: dropped shackles and soot, and nearer, clinker from
    world/dungeon/soot-1.png, -2         the forge (SecretFarTell; the clinker only near)
    world/dungeon/clinker-1.png, -2
    world/dungeon/oubliette-grate.png    D7, the Oubliette: an iron grate set in a stone curb over a shaft, in the
                                         floor of a dead end. Oubliette.cs draws the glint far down between the
                                         bars and the cold draught rising off them, in code
    world/dungeon/puddle-1.png, -2       its far tell: standing water on the flagstones, seeping up from the
    world/dungeon/rime-1.png, -2         shaft, and nearest it the same puddles rimed by the draught's cold
    tools/art/_secret_dungeon_preview.png (gitignored) on the dungeon's floor, for looking at

"A dead smithy in the gaol: a cold hearth, an anvil, and a warm glint on the anvil's horn." The forge that
made the shackles. Every kill on its hearth sets an ember in the dead coals until it roars. Stone in the
stone row, soot in occlusion and stone's deep, iron in steel; the only warm things are the glint and the
embers as they are lit, and the fire when it comes - the Fire ramp, which the coals hold back until then.

Saved at x2. ColdForge.cs stands each piece on its own foot (the forge's and the anvil's are the middle of
their bottom rows) and the ring's middle on the vault's node, all on the world's 2-unit grid.
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
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "dungeon")
PREVIEW = os.path.join(ROOT, "tools", "art", "_secret_dungeon_preview.png")
SCALE = 2


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


GO = ramp("gold")
ST = ramp("stone")
SL = ramp("steel")
LN = ramp("linen")
FL = ramp("flesh")
FI = bl.ELEMENTS["fire"]       # core, hot, mid, edge


def th(x, y):
    return raster.DITHER[y % 8][x % 8] / 64.0


def hashed(k, salt):
    v = (k * 2654435761 + salt * 40503 + 977) % 4294967296
    v = (v ^ (v >> 13)) * 1274126177 % 4294967296
    return (v % 100003) / 100003.0


# --- the forge ---------------------------------------------------------------------------------------

FW, FH = 72, 76
FFX, FFY = 36, 74            # the middle of its foot: ColdForge.cs stands it there
BED = (17, 55, 42, 47)       # the coal bed's pit, x0..x1, y0..y1: where the embers are lit


def masonry(c, x0, x1, y0, y1, salt, lit_left=True):
    """Dressed stone in courses: each block lit on its top edge, joints dark, the face shaded toward the
    right (away from the key)."""
    row = 0
    for y in range(y0, y1 + 1, 4):
        off = 0 if row % 2 == 0 else 4
        for yy in range(y, min(y + 4, y1 + 1)):
            for x in range(x0, x1 + 1):
                u = (x - x0) / float(max(1, x1 - x0))
                joint = (x - x0 + off) % 8 == 0 or yy == y + 3
                if joint:
                    tone = ST[4]
                elif yy == y:
                    tone = ST[1] if u < 0.6 else ST[2]
                else:
                    tone = ST[2] if (u < 0.45 or (u < 0.7 and th(x, yy) > (u - 0.45) * 4)) else ST[3]
                if hashed(x * 7 + yy * 31, salt) < 0.04:
                    tone = ST[3]
                c.set(x, yy, tone)
        row += 1


def forge(frame):
    c = raster.Canvas(FW, FH, None)
    # the contact line under the hearth
    c.hline(8, FW - 9, FFY + 1, OCC)
    # the hood: a stone cowl narrowing to its flue, sooted black up its front
    for y in range(4, 38):
        t = (y - 4) / 34.0
        half = int(9 + 21 * t)
        x0, x1 = FFX - half, FFX + half
        for x in range(x0, x1 + 1):
            u = (x - x0) / float(max(1, x1 - x0))
            tone = ST[2] if u < 0.35 else (ST[3] if u < 0.8 else ST[4])
            if (x - x0) % 8 == 0 or (y % 5 == 0):
                tone = ST[4]
            # soot climbing the middle of its front
            soot = 1.0 - abs(u - 0.5) * 2.2 - t * 0.3
            if soot > th(x, y) * 0.9:
                tone = OCC if soot > 0.5 else ST[4]
            c.set(x, y, tone)
        c.set(x0, y, ST[1])
    # the lip of the hood, lit
    c.hline(FFX - 30, FFX + 30, 38, ST[1])
    c.hline(FFX - 30, FFX + 30, 39, ST[3])
    # the throat under the hood: dark, where the fire will be
    for y in range(40, 47):
        c.hline(FFX - 26, FFX + 26, y, OCC)
    # the hearth's top: a stone rim round the coal pit, lit on its far and left edges
    x0, x1, y0, y1 = BED
    for y in range(y0 - 2, y1 + 3):
        for x in range(x0 - 5, x1 + 6):
            rim = x < x0 or x > x1 or y < y0 or y > y1
            if not rim:
                continue
            tone = ST[1] if (y < y0 or x < x0) else ST[2]
            if y == y0 - 2 or x == x0 - 5:
                tone = ST[0] if th(x, y) < 0.4 else ST[1]
            c.set(x, y, tone)
    # the dead coals: black lumps with a stone-grey ash on them
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            h = hashed(x * 13 + y * 29, 5)
            c.set(x, y, OCC if h < 0.55 else (ST[4] if h < 0.85 else ST[3]))
    # the hearth's front: masonry down to the floor, its ash-hole an arch of dark
    masonry(c, x0 - 5, x1 + 5, y1 + 3, FFY, 9)
    for y in range(FFY - 8, FFY + 1):
        w = 5 if y > FFY - 6 else 3
        c.hline(FFX - w, FFX + w, y, OCC)
    if frame > 0:
        # the roar: fire up out of the coals and into the throat, white at its root, licking the hood
        for x in range(x0, x1 + 1):
            h = int(6 + 6 * hashed(x * 3 + frame * 17, 11) + 3 * math.sin(x * 0.7 + frame * 2.1))
            for k in range(h):
                y = y0 - k
                t = k / float(max(1, h))
                tone = FI[0] if t < 0.2 else FI[1] if t < 0.45 else FI[2] if t < 0.75 else FI[3]
                if k == h - 1 and th(x, y) < 0.5:
                    continue
                c.set(x, y, tone)
            c.set(x, y0, FI[1] if (x + frame) % 3 else FI[0])
        # and every coal alight
        for y in range(y0 + 1, y1 + 1):
            for x in range(x0, x1 + 1):
                h = hashed(x * 13 + y * 29, 5)
                c.set(x, y, FI[2] if h < 0.4 else FI[3] if h < 0.8 else FI[1])
    return c


def ember_spots():
    """The forty places in the coal bed an ember is lit, one a kill, in the order they light: scattered by a
    fixed hash, never two in one place. ColdForge.cs reads the same list (EmberSpots), x and y in the
    forge's own art pixels."""
    x0, x1, y0, y1 = BED
    spots = []
    k = 0
    while len(spots) < 40:
        x = x0 + int(hashed(k, 71) * (x1 - x0 + 1))
        y = y0 + int(hashed(k, 73) * (y1 - y0 + 1))
        k += 1
        if (x, y) not in spots:
            spots.append((x, y))
    return spots


# --- the anvil, the ring -------------------------------------------------------------------------------

AW, AH = 30, 22
AFX, AFY = 15, 20
HORN = (3, 6)                 # the horn's tip: the glint, and where the molten light leaves


def anvil():
    c = raster.Canvas(AW, AH, None)
    c.hline(5, 25, AFY + 1, OCC)
    # the block it stands on: a squat stump of oak, iron-banded
    for y in range(13, AFY + 1):
        for x in range(9, 22):
            u = (x - 9) / 12.0
            c.set(x, y, GO[2] if u < 0.25 else GO[3] if u < 0.75 else GO[4])
    c.hline(9, 21, 16, SL[3])
    # the anvil: a waist, the body, the face lit, the horn reaching left, the heel square to the right
    for y in range(9, 13):
        c.hline(12, 18, y, SL[3] if y > 10 else SL[2])
        c.set(12, y, SL[2])
    for y in range(4, 9):
        for x in range(7, 25):
            tone = SL[2] if x < 16 else SL[3]
            if y == 4:
                tone = SL[0] if x < 20 else SL[1]
            elif y == 5:
                tone = SL[1] if x < 18 else SL[2]
            c.set(x, y, tone)
        c.set(25, y, SL[4])
    # the horn
    c.line(7, 5, HORN[0], HORN[1], SL[1])
    c.line(7, 6, HORN[0] + 1, HORN[1] + 1, SL[2])
    c.set(HORN[0], HORN[1], SL[1])
    c.hline(7, 24, 8, SL[4])
    return c


RING_R = 90                   # ColdForge.RingRadius (180 px) in art pixels
RW = RING_R * 2 + 8


def hearth_ring():
    """A ring of hearth bricks set flush in the floor: a band eight pixels wide whose OUTER edge is the
    radius the cull counts to. Fired brick, in flesh's lower stops - the one warm-ish stone in the gaol, so
    the ring reads against the floor's cold stone - lit toward the key, soot in the joints."""
    c = raster.Canvas(RW, RW, None)
    cx = cy = RW / 2.0
    for y in range(RW):
        for x in range(RW):
            d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
            if d > RING_R or d < RING_R - 8:
                continue
            a = math.atan2(y + 0.5 - cy, x + 0.5 - cx)
            course = 0 if d > RING_R - 4 else 1
            n = (a + math.pi) / (2 * math.pi) * 64 + course * 0.5
            brick = int(n)
            if n - brick < 0.07 or abs(d - (RING_R - 4)) < 0.5:
                tone = OCC if course == 0 else ST[4]
            elif d > RING_R - 1:
                tone = ST[4]
            else:
                # lit toward the key (upper left), shaded away
                lit = -math.cos(a) * 0.7 - math.sin(a) * 0.7
                tone = FL[2] if lit > 0.55 else FL[3] if lit > -0.4 else FL[4]
                if hashed(brick * 2 + course, 3) < 0.15:
                    tone = FL[4]
            c.set(x, y, tone)
    return c


# --- the far tell -------------------------------------------------------------------------------------

def shackles(k):
    c = raster.Canvas(20, 12, None)
    # two iron cuffs and the chain between, dropped where somebody was taken out of them
    for (cx, cy) in ((4, 6), (15, 7)) if k == 0 else ((5, 4), (14, 8)):
        c.ring(cx, cy, 3, 2.2, SL[2])
        c.set(cx - 2, cy - 2, SL[1])
        c.set(cx + 2, cy + 2, SL[4])
        c.set(cx, cy + 3, OCC)
    pts = [(7, 6), (9, 7), (11, 6), (12, 7)] if k == 0 else [(8, 5), (10, 6), (11, 7)]
    for (x, y) in pts:
        c.set(x, y, SL[3])
        c.set(x, y + 1, OCC)
    return c


def soot(k):
    c = raster.Canvas(24, 12, None)
    for y in range(12):
        for x in range(24):
            d = math.hypot((x - 12) / 11.0, (y - 6) / 5.0)
            cover = (1.0 - d) * 1.8 + 0.3 * math.sin(x * 0.8 + k * 1.7 + y)
            if d <= 1.0 and cover > th(x, y):
                c.set(x, y, OCC if cover > 1.0 else ST[4])
    return c


def clinker(k):
    c = raster.Canvas(14, 10, None)
    for j, (x, y, r) in enumerate(((4, 6, 2.6), (9, 5, 2.2), (7, 7, 1.6))[: 2 + k]):
        for yy in range(int(y - r), int(y + r) + 1):
            for xx in range(int(x - r), int(x + r) + 1):
                if math.hypot(xx - x, yy - y) > r:
                    continue
                lit = (x - xx) + (y - yy)
                tone = ST[2] if lit > 0.8 else ST[3] if lit > -0.8 else ST[4]
                if hashed(xx * 5 + yy * 9 + j, 13) < 0.2:
                    tone = GO[4]
                c.set(xx, yy, tone)
        c.set(x, int(y + r) + 1, OCC)
    return c


# --- the Oubliette (D7) ----------------------------------------------------------------------------------

GW = 40                      # the grate's cell: a 36-pixel curb and two clear round it (a dither of damp there
                             # read as a dotted outline, not as wet stone, and was taken out)
GC = GW // 2                 # its middle, on a pixel corner: Oubliette.cs centres the sprite on its node
BARS = (10, 16, 22, 28)      # where each two-pixel bar starts, down and across. The gaps between them are four
                             # wide, and the middle one (18-21) is where the glint is drawn, far down


def grate():
    """An iron grate over a shaft, in a curb of dressed stone set flush with the floor: seen from above, the
    straps crossing in a lattice, the dark of the shaft between them, its far wall catching a little of the
    room's light just under the north curb. Steel for the iron, stone for the curb, occlusion for the shaft.
    Nothing warm: the glint is drawn in code, in the middle gap."""
    c = raster.Canvas(GW, GW, None)
    lo, hi = 2, GW - 3           # the curb's outer square
    ilo, ihi = 6, GW - 7         # the opening's
    # the curb: blocks lit on the north and west faces (toward the key), shaded south and east, joints dark
    for y in range(lo, hi + 1):
        for x in range(lo, hi + 1):
            if ilo <= x <= ihi and ilo <= y <= ihi:
                continue
            north, west = y < ilo, x < ilo
            south, east = y > ihi, x > ihi
            tone = ST[1] if (north or west) and not (south or east) else ST[2] if not (south and east) else ST[3]
            if south or east:
                tone = ST[2] if (west or north) else ST[3]
            # the outer edge's highlight, and the bevel down into the shaft
            if (y == lo and not east) or (x == lo and not south):
                tone = ST[0] if th(x, y) < 0.5 else ST[1]
            if (y == ilo - 1 and ilo - 1 <= x <= ihi + 1) or (x == ilo - 1 and ilo - 1 <= y <= ihi + 1):
                tone = ST[4]
            if (y == ihi + 1 and ilo - 1 <= x <= ihi + 1) or (x == ihi + 1 and ilo - 1 <= y <= ihi + 1):
                tone = ST[2]
            # joints: the curb is four long stones mitred at the corners, each cut once along its length
            if (north or south) and not (west or east) and x == GC:
                tone = ST[4]
            if (west or east) and not (north or south) and y == GC:
                tone = ST[4]
            if (x - lo == y - lo or x - lo == hi - y) and (x < ilo or x > ihi) and (y < ilo or y > ihi):
                tone = ST[4]
            c.set(x, y, tone)
    # the shaft: dark, its far (north) wall catching a little light for two rows under the curb
    for y in range(ilo, ihi + 1):
        for x in range(ilo, ihi + 1):
            tone = OCC
            if y == ilo and th(x, y) < 0.7:
                tone = ST[4]
            elif y == ilo + 1 and th(x, y) < 0.3:
                tone = ST[4]
            c.set(x, y, tone)
    # the straps: two pixels wide, lit on their north and west edges; where two cross, a rivet head
    for b in BARS:
        for t in range(ilo, ihi + 1):
            c.set(b, t, SL[2])
            c.set(b + 1, t, SL[3])
    for b in BARS:
        for t in range(ilo, ihi + 1):
            if c.get(t, b) in (SL[2], SL[3]):
                continue
            c.set(t, b, SL[2])
            c.set(t, b + 1, SL[3])
    for bx in BARS:
        for by in BARS:
            c.set(bx, by, SL[1])
            c.set(bx + 1, by, SL[2])
            c.set(bx, by + 1, SL[2])
            c.set(bx + 1, by + 1, SL[4])
    # the straps run into the curb under its lip: their ends are in shadow
    for b in BARS:
        for t in (ilo, ihi):
            c.set(b, t, SL[4])
            c.set(t, b, SL[4])
    return c


def puddle(k, rimed=False):
    """Standing water on the flags, seeped up from far below: dark (stone's deep, almost black) with its edge
    shaded into the floor and one pale streak where it catches the room's light from the north-west - steel's
    shade, never a white, since nothing in the gaol is lit but by him. Rimed: the draught's cold has frosted its
    edge, steel's hi and lit in a broken rind round the north and west and a few needles out over the water."""
    w, h = (22, 10) if k == 0 else (18, 9)
    c = raster.Canvas(w, h, None)
    cx, cy = (w - 1) / 2.0, (h - 1) / 2.0
    rx, ry = w / 2.0 - 1.0, h / 2.0 - 1.0
    lobes = ((0.3, -0.2, 0.55), (-0.35, 0.25, 0.5)) if k == 0 else ((-0.2, -0.3, 0.6), (0.4, 0.2, 0.45))
    inside = set()
    for y in range(h):
        for x in range(w):
            u, v = (x - cx) / rx, (y - cy) / ry
            d = math.hypot(u, v)
            for (lu, lv, lr) in lobes:
                d = min(d, math.hypot(u - lu, v - lv) / lr * 0.62)
            wob = 0.08 * math.sin(x * 1.3 + k * 2.0) + 0.06 * math.cos(y * 1.9 + k)
            if d + wob <= 1.0:
                inside.add((x, y))
    for (x, y) in inside:
        edge = any((x + dx, y + dy) not in inside for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        tone = ST[4]
        if edge:
            tone = ST[3]
        c.set(x, y, tone)
    # the streak of light on it, short, across the north-west of the water
    for i in range(4 if k == 0 else 3):
        x, y = int(cx - 3 + i), int(cy - 1)
        if (x, y) in inside:
            c.set(x, y, SL[3])
    if rimed:
        for (x, y) in inside:
            nw = ((x - 1, y) not in inside) or ((x, y - 1) not in inside)
            if nw and th(x, y) < 0.75:
                c.set(x, y, SL[0] if th(x, y) < 0.35 else SL[1])
        # needles of frost reaching in from the rind
        for j in range(3):
            x0 = int(cx - rx * 0.6 + j * rx * 0.55)
            y0 = min(y for (xx, y) in inside if xx == x0) if any(xx == x0 for (xx, _) in inside) else int(cy)
            for t in range(2 + (j + k) % 2):
                if (x0 + t, y0 + 1 + t) in inside:
                    c.set(x0 + t, y0 + 1 + t, SL[1])
    return c


# --- out ----------------------------------------------------------------------------------------------

def audit(name, c):
    bad = c.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in %s: %s" % (name, bad[:5]))
        return False
    return True


def strip(frames, w, h):
    img = Image.new("RGBA", (w * SCALE * len(frames), h * SCALE), (0, 0, 0, 0))
    for i, c in enumerate(frames):
        img.alpha_composite(c.scaled(SCALE), (i * w * SCALE, 0))
    return img


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    ok = True
    sheets = []

    frames = [forge(0), forge(1), forge(2)]
    for i, c in enumerate(frames):
        ok = audit("cold-forge frame %d" % i, c) and ok
    img = strip(frames, FW, FH)
    img.save(os.path.join(OUT, "cold-forge.png"))
    sheets.append(img)

    singles = [("cold-forge-anvil", anvil()), ("hearth-ring", hearth_ring())]
    singles += [("shackles-%d" % (k + 1), shackles(k)) for k in range(2)]
    singles += [("soot-%d" % (k + 1), soot(k)) for k in range(2)]
    singles += [("clinker-%d" % (k + 1), clinker(k)) for k in range(2)]
    singles += [("oubliette-grate", grate())]
    singles += [("puddle-%d" % (k + 1), puddle(k)) for k in range(2)]
    singles += [("rime-%d" % (k + 1), puddle(k, rimed=True)) for k in range(2)]
    row = []
    for name, c in singles:
        ok = audit(name, c) and ok
        one = c.scaled(SCALE)
        one.save(os.path.join(OUT, name + ".png"))
        row.append(one)
    print("  cold forge   3 frames at %dx%d, anvil, ring r=%d px, 2 shackles, 2 soot, 2 clinker"
          % (FW * SCALE, FH * SCALE, RING_R * SCALE))
    print("  ember spots  %s" % ember_spots())
    print("  oubliette    grate %dx%d px (the glint's gap at art %d-%d), 2 puddles, 2 rimed"
          % (GW * SCALE, GW * SCALE, GC - 2, GC + 1))

    # The preview: the ring with the forge behind it and the anvil, as ColdForge.cs lays them out, and the
    # rest in a row.
    ring = row[1]
    comp = Image.new("RGBA", (ring.size[0] + 40, ring.size[1] + 300), (0, 0, 0, 0))
    ox, oy = 20, 280
    comp.alpha_composite(ring, (ox, oy))
    mid = (ox + ring.size[0] // 2, oy + ring.size[1] // 2)
    fimg = sheets[0].crop((FW * SCALE, 0, FW * SCALE * 2, FH * SCALE))
    comp.alpha_composite(fimg, (mid[0] - FFX * SCALE, mid[1] - 270 - FFY * SCALE))
    comp.alpha_composite(row[0], (mid[0] - 56 - AFX * SCALE, mid[1] - 205 - AFY * SCALE))
    pad = 10
    rest = row[2:]
    pw = max(sheets[0].size[0], comp.size[0]) + sum(r.size[0] + pad for r in rest) + pad * 3
    ph = max(comp.size[1], sheets[0].size[1]) + pad * 2
    sheet = Image.new("RGBA", (pw, ph), raster.rgb(ST[3]))
    sheet.alpha_composite(comp, (pad, pad))
    x = comp.size[0] + pad * 2
    sheet.alpha_composite(sheets[0], (x, pad))
    y = sheets[0].size[1] + pad * 2
    for r in rest:
        sheet.alpha_composite(r, (x, y))
        x += r.size[0] + pad
    sheet.save(PREVIEW)
    print("preview: tools/art/_secret_dungeon_preview.png")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
