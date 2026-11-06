# -*- coding: utf-8 -*-
"""The Fallow Fields' secrets (.ai/achievements-and-secrets.md 8.2). Run: python tools/art/secret_fields.py

    world/fields/pinfold-{back,front,open}.png  FF1, the Pinfold (the spell vault), three separate layers:
                                         pound behind (its back and side walls, the trodden floor inside), its
                                         front wall with the barred gate shut, and the front with the gate
                                         swung open. Pinfold.cs draws the essence between back and front
    world/fields/pinfold-essence.png     eight 128x128 frames of imprisoned magical energy
    world/fields/pinfold-hound.png       the hound of warm light, fourteen 40-cell frames: lying (two, a
                                         breath), standing, running (four), facing right; then the same
                                         seven facing left
    world/fields/hurdle-broken-1.png,-2  its far tell: broken hurdles - woven fence panels, split and
    world/fields/pawprints-1.png, -2     fallen - and a dog's prints, drawn pointing right (LeanToward)
    tools/art/_secret_fields_preview.png (gitignored) on the stubble, for looking at

The Hayward's pinfold (.ai/campaign.md 5): the parish pound where strays were held until somebody
paid for them. A creature's essence is magically bound inside. Walk in and approach it to learn Familiar.

THE POUND is drystone in the stone row with lichen in its joints, its coping lit along the top, in three-
quarter view: the back wall and both side walls behind, the front wall with the gate in front, so the
essence - drawn between them - is seen through the bars and over nothing else. The gate is iron bars
in a timber frame, the one thing in it that is not stone; the latch on its right post carries the glint
once the guards are down (in code). 972 x 816 art pixels, saved at x2: 1944 x 1632 on screen.
The larger interior is walkable. Its floor and back wall sort separately from its front.

THE LEGACY HOUND strip is retained; the pen now shows essence instead. It is the Pale Hart's kind:
a light, not a body, drawn wholly from the warm rows - gold and the
Light element's ramp - with no rim and no outline. Lying, its head is on its paws; running, it is long and
low, the familiar's dog seen as the light it is (tools/art/familiar.py draws the creature it becomes).
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
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "fields")
PREVIEW = os.path.join(ROOT, "tools", "art", "_secret_fields_preview.png")
SCALE = 2


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


GO = ramp("gold")
FL = ramp("flesh")
LN = ramp("linen")
LI = ramp("lichen")
ST = ramp("stone")
SL = ramp("steel")
LT = bl.ELEMENTS["light"]


def th(x, y):
    return raster.DITHER[y % 8][x % 8] / 64.0


def hashed(k, salt):
    v = (k * 2654435761 + salt * 40503 + 977) % 4294967296
    v = (v ^ (v >> 13)) * 1274126177 % 4294967296
    return (v % 100003) / 100003.0


def blob(c, cx, cy, rx, ry, tones, salt=0):
    """A rounded mass lit from the upper left; tones run light to dark."""
    n = len(tones)
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            nx = (x - cx) / float(rx)
            ny = (y - cy) / float(ry)
            d = math.sqrt(nx * nx + ny * ny)
            if d > 1.0:
                continue
            v = (-nx - ny) / 1.4142 * 0.9 - (0.35 if d > 0.82 else 0.0) + 0.15
            t = (v + 1.0) / 2.0 * (n - 1)
            i = max(0, min(n - 2, int(t)))
            f = t - i
            lo = tones[n - 1 - i]
            up = tones[n - 2 - i]
            c.set(x, y, up if f > th(x, y) else lo)


# --- the pound --------------------------------------------------------------------------------------

PW, PH = 972, 816
PCX = PW // 2
BACK_BASE = 36
FRONT_BASE = 804
WALL_H = 26
GATE_X0, GATE_X1 = PCX - 64, PCX + 64
LATCH = (GATE_X1 + 2, FRONT_BASE - 12)


def stones(c, x0, x1, ybase, height, salt, lit=True):
    """A drystone face from ybase up `height` rows: courses of stones three rows tall, staggered, each lit
    on its top and left edge, its joints dark, lichen in a few."""
    row = 0
    y = ybase
    while y > ybase - height:
        top = max(ybase - height + 1, y - 2)
        x = x0 - (row % 2) * 3
        k = 0
        while x <= x1:
            w = 4 + int(hashed(row * 31 + k, salt) * 4)
            for yy in range(top, y + 1):
                for xx in range(max(x0, x), min(x1, x + w - 1) + 1):
                    if yy == top or xx == x:
                        tone = ST[1] if lit else ST[2]
                    elif yy == y:
                        tone = ST[3]
                    else:
                        tone = ST[2] if lit else ST[3]
                    if hashed(xx * 7 + yy * 13, salt + 5) < 0.05:
                        tone = LI[3]
                    c.set(xx, yy, tone)
            # the joint
            for yy in range(top, y + 1):
                c.set(x + w, yy, ST[4])
            x += w + 1
            k += 1
        c.hline(x0, x1, top - 1, ST[4])
        y = top - 2
        row += 1


def coping(c, x0, x1, y):
    """The wall's top, seen from above: a lit band two rows deep."""
    c.hline(x0, x1, y, ST[0])
    c.hline(x0, x1, y + 1, ST[1])
    for x in range(x0, x1 + 1, 5):
        c.set(x, y + 1, ST[2])


def pound_back():
    c = raster.Canvas(PW, PH, None)
    # the trodden floor inside: earth and stubble in the gold row's lowest stops
    for y in range(BACK_BASE, FRONT_BASE - 1):
        for x in range(12, PW - 12):
            tone = LN[4] if th(x, y) < 0.55 else GO[4]
            if hashed(x * 3 + y * 17, 41) < 0.04:
                tone = GO[3]
            c.set(x, y, tone)
    # the back wall
    stones(c, 10, PW - 11, BACK_BASE, WALL_H, 1)
    coping(c, 10, PW - 11, BACK_BASE - WALL_H - 1)
    # the side walls, seen end-on from above: a coping band running forward, the left one's outer face lit,
    # the right one's in shade
    for (x, lit) in ((6, True), (PW - 11, False)):
        for y in range(BACK_BASE - WALL_H - 1, FRONT_BASE - WALL_H + 1):
            c.hline(x, x + 4, y, ST[1] if lit else ST[2])
            c.set(x + (0 if lit else 4), y, ST[0] if lit else ST[3])
        stones(c, x, x + 4, FRONT_BASE, WALL_H, 3 if lit else 4, lit)
    # the inner shadow along the back wall's foot
    c.hline(12, PW - 13, BACK_BASE + 1, OCC)
    # A worn binding on the floor, quietly framing the essence at the enclosure's center.
    for k in range(160):
        a = k * math.pi * 2.0 / 160.0
        if k % 9 < 6:
            x = int(round(PCX + math.cos(a) * 60))
            y = int(round((BACK_BASE + FRONT_BASE) / 2.0 + math.sin(a) * 38))
            c.set(x, y, GO[3] if k % 3 else GO[2])
    # Worn stone pads and broken bands give this large enclosure its own history, without
    # stretching the old 2px stonework or filling the walking space with physical clutter.
    for k in range(52):
        x = 28 + int(hashed(k, 91) * (PW - 56))
        y = BACK_BASE + 8 + int(hashed(k, 93) * (FRONT_BASE - BACK_BASE - 16))
        width = 4 + int(hashed(k, 95) * 8)
        c.hline(x, x + width, y, ST[3])
        c.hline(x + 2, x + width - 1, y + 1, ST[4])
    # Large, worn circles and carved rune stations explain the enclosure's magical binding.
    cy = (BACK_BASE + FRONT_BASE) // 2
    for ring in (1, 2, 3):
        rx, ry = 92 * ring, 72 * ring
        for k in range(720):
            if (k + ring * 9) % 37 > 29:
                continue
            a = k * math.pi * 2.0 / 720.0
            x, y = int(round(PCX + math.cos(a) * rx)), int(round(cy + math.sin(a) * ry))
            c.set(x, y, ST[2] if k % 3 else ST[3])
            if k % 4: c.set(x, y + 1, ST[4])
    for k in range(12):
        a = k * math.pi / 6.0
        x, y = int(round(PCX + math.cos(a) * 290)), int(round(cy + math.sin(a) * 228))
        c.poly([(x-9,y),(x,y-7),(x+9,y),(x,y+7)], ST[3])
        c.line(x-5,y,x+5,y,ST[1])
        c.line(x,y-4,x,y+4,ST[2])
    return c


def pound_front(open_gate):
    c = raster.Canvas(PW, PH, None)
    # the contact line under the whole front
    c.hline(4, PW - 5, FRONT_BASE + 1, OCC)
    # the front wall either side of the gate
    stones(c, 6, GATE_X0 - 1, FRONT_BASE, WALL_H, 7)
    stones(c, GATE_X1 + 1, PW - 7, FRONT_BASE, WALL_H, 8)
    coping(c, 6, GATE_X0 - 1, FRONT_BASE - WALL_H - 1)
    coping(c, GATE_X1 + 1, PW - 7, FRONT_BASE - WALL_H - 1)
    # the gate posts, timber, taller than the wall
    for (x, lit) in ((GATE_X0 - 2, True), (GATE_X1, False)):
        for y in range(FRONT_BASE - WALL_H - 5, FRONT_BASE + 1):
            c.set(x, y, GO[2] if lit else GO[3])
            c.set(x + 1, y, GO[3] if lit else GO[4])
            c.set(x + 2, y, GO[4] if lit else OCC)
        c.set(x, FRONT_BASE - WALL_H - 6, FL[2])
        c.set(x + 1, FRONT_BASE - WALL_H - 6, GO[2])
    top = FRONT_BASE - WALL_H + 1
    if not open_gate:
        # the barred gate: a timber frame, iron bars every third column - a light behind them shows between
        c.hline(GATE_X0 + 1, GATE_X1 - 1, top, GO[2])
        c.hline(GATE_X0 + 1, GATE_X1 - 1, top + 1, GO[3])
        c.hline(GATE_X0 + 1, GATE_X1 - 1, FRONT_BASE - 1, GO[3])
        c.hline(GATE_X0 + 1, GATE_X1 - 1, FRONT_BASE, GO[4])
        for x in range(GATE_X0 + 2, GATE_X1 - 1, 3):
            for y in range(top + 2, FRONT_BASE - 1):
                c.set(x, y, SL[2] if (y + x) % 7 else SL[1])
        # a rail across the middle
        c.hline(GATE_X0 + 1, GATE_X1 - 1, (top + FRONT_BASE) // 2, SL[3])
        # the latch: an iron bar dropped in a hasp on the right post
        lx, ly = LATCH
        c.hline(lx - 4, lx, ly, SL[1])
        c.set(lx + 1, ly, SL[3])
        c.set(lx + 1, ly + 1, SL[4])
    else:
        # swung open inward to the left: the gate edge-on, a narrow frame of bars against the left post
        for y in range(top, FRONT_BASE + 1):
            c.set(GATE_X0 + 1, y, GO[2])
            c.set(GATE_X0 + 2, y - 2, SL[2])
            c.set(GATE_X0 + 3, y - 3, GO[3])
        c.line(GATE_X0 + 1, top, GATE_X0 + 3, top - 3, GO[2])
        c.line(GATE_X0 + 1, FRONT_BASE, GATE_X0 + 3, FRONT_BASE - 3, GO[3])
        # the latch hangs loose on the post
        lx, ly = LATCH
        c.set(lx, ly, SL[1])
        c.set(lx, ly + 1, SL[2])
        c.set(lx, ly + 2, SL[3])
    return c


# --- the hound ----------------------------------------------------------------------------------------

DW, DH = 40, 32
DFOOT = 29
BODY = [GO[0], LT[2], GO[1], GO[2]]


def leg(c, pts, near):
    a, b = (LT[2], GO[1]) if near else (GO[1], GO[2])
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        c.line(x0, y0, x1, y1, a)
        c.line(x0 + 1, y0, x1 + 1, y1, b)


def hound(pose):
    c = raster.Canvas(DW, DH, None)
    if pose in ("lie0", "lie1"):
        c.hline(7, 33, DFOOT + 1, OCC)
        # lying: the body a long low mass, the haunch round, forepaws out in front, the head down on them
        blob(c, 16, DFOOT - 4, 10, 4.2, BODY, salt=1)
        blob(c, 10, DFOOT - 4, 5, 4.5, BODY, salt=2)
        for x in range(25, 33):
            c.set(x, DFOOT, LT[2] if x % 3 else GO[1])
            c.set(x, DFOOT - 1, GO[1])
        hy = DFOOT - 4 - (1 if pose == "lie1" else 0)
        blob(c, 28, hy, 4, 3, BODY, salt=3)
        blob(c, 32, hy + 1, 2.4, 1.8, BODY, salt=4)
        c.set(34, hy + 1, GO[2])                    # the nose
        c.set(29, hy - 1, GO[3])                    # the eye, shut
        c.set(30, hy - 1, GO[3])
        c.set(26, hy - 3, GO[1])                    # the ear, laid back
        c.set(25, hy - 3, GO[1] if pose == "lie0" else LT[2])
        c.set(24, hy - 2, GO[2])
        # the tail curled round the haunch
        c.line(5, DFOOT - 1, 8, DFOOT, GO[1])
        return c
    if pose == "stand":
        legs = [[(11, 21), (11, 25), (11, DFOOT)], [(25, 21), (25, 25), (26, DFOOT)],
                [(9, 21), (8, 25), (9, DFOOT)], [(27, 21), (28, 25), (28, DFOOT)]]
        body, head, tail = (18, 18), (31, 11), [(6, 16), (3, 12)]
    else:
        k = int(pose[-1])
        # the gallop: gathered, reaching, stretched, landing
        legs = [
            [[(12, 20), (15, 25), (18, 27)], [(25, 20), (22, 24), (20, 27)], [(10, 20), (13, 25), (16, 28)], [(27, 20), (24, 25), (22, 28)]],
            [[(12, 20), (10, 25), (12, DFOOT)], [(25, 20), (29, 23), (32, 24)], [(10, 20), (8, 25), (9, DFOOT)], [(27, 20), (31, 22), (34, 22)]],
            [[(12, 19), (7, 22), (2, 24)], [(25, 19), (30, 22), (35, 23)], [(10, 19), (5, 21), (1, 22)], [(27, 19), (32, 21), (37, 21)]],
            [[(12, 20), (8, 23), (4, 25)], [(25, 21), (26, 25), (27, DFOOT)], [(10, 20), (6, 24), (3, 26)], [(27, 21), (29, 25), (29, DFOOT)]],
        ][k]
        body = (18, 17 if k == 2 else 18)
        head = (32, 12 if k != 2 else 13)
        tail = [(6, 15), (2, 14)]
    if pose == "stand" or pose in ("run1", "run3"):
        c.hline(8, 30, DFOOT + 1, OCC)
    leg(c, legs[0], False)
    leg(c, legs[1], False)
    bx, by = body
    blob(c, bx, by, 10, 3.6, BODY, salt=5)
    blob(c, bx - 6, by, 4.5, 4.2, BODY, salt=6)
    blob(c, bx + 6, by - 1, 4.5, 4.4, BODY, salt=7)
    hx, hy = head
    for k in range(4):
        t = k / 3.0
        blob(c, bx + 8 + (hx - 3 - bx - 8) * t, by - 2 + (hy + 2 - by + 2) * t, 2.4, 2.4, BODY, salt=8 + k)
    blob(c, hx, hy, 3.2, 2.6, BODY, salt=12)
    blob(c, hx + 3, hy + 1, 2.2, 1.6, BODY, salt=13)
    c.set(hx + 5, hy + 1, GO[2])
    c.set(hx + 1, hy - 1, GO[3])
    c.set(hx - 2, hy - 3, LT[2])
    c.set(hx - 2, hy - 2, GO[1])
    c.set(hx - 1, hy - 3, GO[1])
    (tx0, ty0), (tx1, ty1) = tail
    c.line(tx0, ty0, tx1, ty1, GO[1])
    c.set(tx1, ty1, LT[2])
    leg(c, legs[2], True)
    leg(c, legs[3], True)
    return c


HOUND_POSES = ["lie0", "lie1", "stand", "run0", "run1", "run2", "run3"]


def hound_strip():
    frames = [hound(p) for p in HOUND_POSES]
    right = [f.scaled(SCALE) for f in frames]
    left = [img.transpose(Image.FLIP_LEFT_RIGHT) for img in right]
    out = Image.new("RGBA", (DW * SCALE * len(right) * 2, DH * SCALE), (0, 0, 0, 0))
    for i, img in enumerate(right + left):
        out.alpha_composite(img, (i * DW * SCALE, 0))
    return frames, out


# --- the far tell -----------------------------------------------------------------------------------

def hurdle(k):
    """A woven hurdle, split and fallen: withies over and under three stakes, one end down in the stubble."""
    c = raster.Canvas(30, 20, None)
    base = 17
    c.hline(2, 27, base + 1, OCC)
    tilt = 6 if k == 0 else 3
    for s, x in enumerate((4, 14, 24)):
        h = 12 - (tilt * s // 2 if k == 0 else (0 if s < 2 else tilt + 4))
        for y in range(base - h, base + 1):
            c.set(x, y, GO[2])
            c.set(x + 1, y, GO[4])
    for row in range(4):
        for x in range(3, 27):
            s = (x - 3) / 24.0
            drop = int(tilt * s) if k == 0 else (int((tilt + 4) * max(0.0, s - 0.6) / 0.4))
            y = base - 10 + row * 3 + drop
            if y > base:
                continue
            if k == 1 and 15 <= x <= 17:
                continue                         # the split
            weave = (x // 3 + row) % 2
            c.set(x, y, GO[2] if weave else GO[3])
            c.set(x, y + 1, GO[3] if weave else GO[4])
    return c


def pawprints(k):
    c = raster.Canvas(30, 12, None)

    def paw(x, y):
        c.set(x, y + 1, OCC)
        c.set(x + 1, y + 1, OCC)
        c.set(x, y + 2, OCC)
        c.set(x + 1, y + 2, GO[4])
        for (dx, dy) in ((2, 0), (2, 3), (3, 1), (3, 2)):
            c.set(x + dx, y + dy, OCC)
        c.set(x - 1, y + 1, GO[3])
    # The second trail a pixel left of where it was: its last print ended on the canvas's last column
    # (tools/art/clip_check.py).
    steps = ((2, 2), (9, 6), (16, 1), (23, 5)) if k == 0 else ((2, 5), (10, 1), (18, 6), (25, 2))
    for (x, y) in steps:
        paw(x, y)
    return c


def essence(pose):
    """An imprisoned creature's light: three curling ribbons around a quiet, breathing heart.
    Eight authored frames, opaque palette stops, on the same x2 grid as the pen."""
    c = raster.Canvas(64, 64, None)
    phase = pose * math.pi / 4.0
    breath = (1.0 - math.cos(phase)) * 0.5
    # A broken pool, not a filled glowing disc.
    for k in range(44):
        a = k * math.pi * 2.0 / 44.0
        x = int(round(32 + math.cos(a) * 20))
        y = int(round(51 + math.sin(a) * 5))
        if (k + pose) % 5 < 3:
            c.set(x, y, GO[2] if k % 3 else GO[1])
    for strand in range(3):
        previous = None
        for k in range(35):
            t = k / 34.0
            a = phase + strand * math.pi * 2 / 3.0 + t * 5.8
            radius = 4 + 12 * math.sin(t * math.pi)
            x = int(round(32 + math.sin(a) * radius))
            y = int(round(50 - t * 36 + math.cos(a) * 3))
            ink = LT[2] if strand == 0 else GO[1] if strand == 1 else GO[2]
            if previous:
                c.line(previous[0], previous[1], x, y, ink)
            if (k + pose + strand) % 7 == 0:
                c.set(x + 1, y, LT[1])
            previous = (x, y)
    blob(c, 32, 33, 4 + breath, 6 + breath * 2, [LT[0], LT[1], GO[1], GO[2]])
    # Loosened sparks climb, without soft alpha or a screen-filling halo.
    for k in range(5):
        x = 23 + int(hashed(k, 81) * 19)
        y = 48 - ((pose * 3 + k * 9) % 39)
        c.set(x, y, GO[1] if k % 2 else LT[1])
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

    frames = [pound_back(), pound_front(False), pound_front(True)]
    for i, c in enumerate(frames):
        ok = audit("pinfold frame %d" % i, c) and ok
    # Separate layers stay below 2048px per texture, even on conservative Android GPUs.
    for name, c in zip(("pinfold-back", "pinfold-front", "pinfold-open"), frames):
        c.scaled(SCALE).save(os.path.join(OUT, name + ".png"))
    # Only the ignored contact sheet uses the wide strip.
    sheets.append(strip(frames, PW, PH))

    energy = [essence(p) for p in range(8)]
    for p, c in enumerate(energy):
        ok = audit("pinfold-essence %d" % p, c) and ok
    strip(energy, 64, 64).save(os.path.join(OUT, "pinfold-essence.png"))

    frames, img = hound_strip()
    for p, c in zip(HOUND_POSES, frames):
        ok = audit("pinfold-hound %s" % p, c) and ok
    img.save(os.path.join(OUT, "pinfold-hound.png"))
    sheets.append(img)

    row = []
    for name, c in (("hurdle-broken-1", hurdle(0)), ("hurdle-broken-2", hurdle(1)),
                    ("pawprints-1", pawprints(0)), ("pawprints-2", pawprints(1))):
        ok = audit(name, c) and ok
        one = c.scaled(SCALE)
        one.save(os.path.join(OUT, name + ".png"))
        row.append(one)
    tells = Image.new("RGBA", (sum(r.size[0] + 8 for r in row), max(r.size[1] for r in row)), (0, 0, 0, 0))
    x = 0
    for r in row:
        tells.alpha_composite(r, (x, 0))
        x += r.size[0] + 8
    sheets.append(tells)
    print("  pinfold      3 frames at %dx%d, hound %d frames at %dx%d, 2 hurdles, 2 prints"
          % (PW * SCALE, PH * SCALE, len(HOUND_POSES) * 2, DW * SCALE, DH * SCALE))

    # The pen composed with two distinct essence frames, then all the source sheets.
    comp = []
    for gate, pose in ((1, 0), (2, 2)):
        img = Image.new("RGBA", (PW * SCALE, PH * SCALE), (0, 0, 0, 0))
        back = sheets[0].crop((0, 0, PW * SCALE, PH * SCALE))
        front = sheets[0].crop((gate * PW * SCALE, 0, (gate + 1) * PW * SCALE, PH * SCALE))
        img.alpha_composite(back)
        img.alpha_composite(energy[pose].scaled(SCALE), ((PCX - 32) * SCALE, ((BACK_BASE + FRONT_BASE) // 2 - 51) * SCALE))
        img.alpha_composite(front)
        comp.append(img)
    pad = 10
    pw = max(max(s.size[0] for s in sheets), sum(c.size[0] + pad for c in comp)) + pad * 2
    ph = sum(s.size[1] for s in sheets) + comp[0].size[1] + pad * (len(sheets) + 2)
    sheet = Image.new("RGBA", (pw, ph), raster.rgb(GO[4]))
    x = pad
    for img in comp:
        sheet.alpha_composite(img, (x, pad))
        x += img.size[0] + pad
    y = comp[0].size[1] + pad * 2
    for s in sheets:
        sheet.alpha_composite(s, (pad, y))
        y += s.size[1] + pad
    sheet.save(PREVIEW)
    print("preview: tools/art/_secret_fields_preview.png")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
