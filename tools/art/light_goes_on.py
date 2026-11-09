# -*- coding: utf-8 -*-
"""Reel E, "The light goes on": the cutscene that announces the far shore, after the Throne is won.

    python tools/art/light_goes_on.py             # both panels, written to assets/bonelight/ui/story/
    python tools/art/light_goes_on.py --preview   # also tools/art/_story/<panel>.png (gitignored): PC and Pixel 9 views
    "$GODOT_BIN" --headless --path . --import      # then import, and commit the .import sidecars

See .ai/second-half.md 6.3 for what it is and .ai/cutscenes.md for the rules it keeps; scripts/Prologue.cs plays it
(Reels, "light_goes_on") and scripts/LightGoesOn.cs says when. Runs under Python 2.7 and 3 (PIL only), every random
stream on rng.random(), as prologue.py's are.

THE STORY. When the dark wizard falls the light he took leaves him as a storm of motes - the ending - and goes home:
most of it down onto the land, to the wizards. Some of it goes on, east over the dark sea, because it was never a
wizard's. Two panels, the Archmage at the centre of both:

    lightgoes-gate  His sea-gate, on the rock's east side, from inside the passage: the Archmage on the threshold
                    where the steps go down to the water, his back to us, his own light up, and the light streaming
                    out over him through the arch, away over the dark sea toward the horizon. No line.
    lightgoes-sea   Wide and low over the water from the quay at the rock's foot: the stream running on east, smaller
                    and smaller, toward a line of white cliffs on the horizon, and on past the edge of the picture.
                    The Archmage small on the quay, watching it go. "Some of it was older than any of them."

THE RULES IT KEEPS. The frame, the layers, the drift and the audit are the prologue's (prologue.py's docstring):
960x840 at a whole x2, the 360x360 square both canvases always show (columns 300-660, rows 240-600) holding every
subject, back (opaque) + mid + fore (transparent), zero off-contract pixels. Nothing here sheds dark light: the dark
wizard has fallen and his tower is unlit. The light is the light ramp - emission on the motes and the Archmage's
staff alone - and where it lands, on stone and water, it is the gold pigment row, never the ramp. The motes ride the
mid layer, which drifts east against the back (Prologue.cs), so the stream is seen to go. Feet stay above row 552 on
the panel with a line: on a PC the caption sits over rows ~558-578.
"""

from __future__ import division, print_function

import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image

import prologue as pr

S, V, G, K = pr.S, pr.V, pr.G, pr.K
LT = pr.LT
E, M, OCC = pr.E, pr.M, pr.OCC
FW, FH = pr.FW, pr.FH

ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "assets", "bonelight", "ui", "story")
PREVIEW = os.path.join(HERE, "_story")


def stream(c, pts, n, seed, near=3, spread0=60.0, spread1=4.0, trail=10):
    """The light going on: motes along a path that recedes from pts[0] (near the camera: big, spread wide, long
    streaks) to pts[-1] (far: single sparks close together), each one a head of the light ramp - white-hot when it is
    near and big, its gold edge far off - with a streak behind it the way it came, so the stream reads as going and
    not as a field of stars. Never a blend: every pixel one of the ramp's four."""
    rng = random.Random(seed)
    segs = []
    total = 0.0
    for i in range(len(pts) - 1):
        d = math.hypot(pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1])
        segs.append((total, d, pts[i], pts[i + 1]))
        total += d
    for _ in range(n):
        t = rng.random() ** 0.85
        s = t * total
        for (s0, d, a, b) in segs:
            if s <= s0 + d or (s0, d, a, b) == segs[-1]:
                u = (s - s0) / max(0.001, d)
                x = a[0] + (b[0] - a[0]) * u
                y = a[1] + (b[1] - a[1]) * u
                dx, dy = (b[0] - a[0]) / max(0.001, d), (b[1] - a[1]) / max(0.001, d)
                break
        spread = spread0 + (spread1 - spread0) * t
        x += (rng.random() - 0.5) * spread
        y += (rng.random() - 0.5) * spread * 0.55
        size = max(1, int(round((1.0 - t) * near + rng.random() * 0.7)))
        tail = int((1.0 - t) * trail * (0.5 + rng.random())) + 1
        for k in range(1, tail + 1):
            c.set(x - dx * k * 1.4, y - dy * k * 1.4, LT[2] if k == 1 and size > 1 else LT[3])
        # Warm: the gold edge of the ramp is most of the stream, its pale cores only where a mote is near and big.
        heat = 1.2 + t * 1.6 + rng.random() * 1.2 - (size - 1) * 0.7
        col = LT[max(0, min(3, int(heat)))]
        if size >= 2:
            c.rect(x, y, x + size - 1, y + size - 1, LT[2] if col == LT[3] else col)
            c.set(x, y, LT[0])
        else:
            c.set(x, y, col)


def water(c, y0, y1, seed, glints, x0=0, x1=FW):
    """The dark sea at night: a step darker toward the camera, flecks of the sky's last light on it, longer nearer."""
    c.vramp(y0, y1, [V[4], S[4], S[4], OCC], x0, x1)
    rng = random.Random(seed)
    for _ in range(glints):
        d = rng.random() ** 1.6
        y = y0 + 2 + d * (y1 - y0 - 4)
        x = x0 + rng.random() * (x1 - x0)
        ln = 1 + int(rng.random() * (2 + 9 * d))
        c.hline(x, x + ln, y, V[3] if d < 0.3 else S[3])


def gold_on_water(c, x, y0, y1, w0, w1, seed, n):
    """The light landing on the water under the stream: a broken column of gold pigment, dense at the horizon under
    the light and thinning toward the camera, its edges ragged - the swell, not a ruled shape."""
    rng = random.Random(seed)
    for _ in range(n):
        t = rng.random() ** 1.8
        y = y0 + t * (y1 - y0)
        w = (w0 + (w1 - w0) * t) * (0.6 + 0.8 * rng.random())
        xx = x + (rng.random() - 0.5) * w + math.sin(y * 0.37) * w * 0.15
        ln = 1 + int(rng.random() * (2 + 7 * t))
        c.hline(xx, xx + ln, y, G[2] if rng.random() < 0.35 - t * 0.25 else G[3])


# --------------------------------------------------------------------------- the sea-gate
GATE_HZ = 404          # the horizon, eye height in the passage
ARCH_CX, ARCH_RX = 480, 196
ARCH_SPRING, ARCH_RY = 332, 236
THRESHOLD = 540
HIM_X, HIM_Y, HIM_H = 470, 574, 92


def in_arch(x, y):
    """Inside the sea-gate's opening, from the threshold up."""
    if y > THRESHOLD:
        return False
    if abs(x - ARCH_CX) > ARCH_RX:
        return False
    if y >= ARCH_SPRING:
        return True
    nx = (x - ARCH_CX) / float(ARCH_RX)
    ny = (y - ARCH_SPRING) / float(ARCH_RY)
    return nx * nx + ny * ny <= 1.0


def p_gate():
    """1. His sea-gate, from inside: the Archmage on the threshold, his back to us, and the light streaming out over him
    through the arch and away over the dark water. The arch is his: black stone, its inner edge caught by the light
    passing outside, and gold where the Archmage's own light lands on the floor and the steps."""
    back, mid, fore = pr.frame(True), pr.frame(), pr.frame()
    pr.sky(back, 0, GATE_HZ + 2, [OCC, S[4], V[4], V[3], V[2]])
    pr.stars(back, (0, 0, FW, GATE_HZ - 50), 380, 811)
    # Where the stream is going, low in the east: the sky lifted a stop by it, nothing more (no moon has risen there).
    back.radial(610, GATE_HZ, 300, 60, [V[3], V[3], V[4], None])
    water(back, GATE_HZ + 1, FH, 813, 320)
    back.hline(0, FW, GATE_HZ + 1, V[3])
    gold_on_water(back, 612, GATE_HZ + 3, 500, 6, 70, 815, 200)

    # The light, going out over the water: in through the top of the opening from behind us, away to the horizon - its
    # far end inside the square both canvases show, so the phone sees it reach the sky over the sea too.
    stream(mid, [(372, 30), (440, 140), (530, 270), (612, GATE_HZ - 6)], 620, 821, near=4, spread0=170.0, spread1=6.0,
           trail=14)
    stream(mid, [(300, 110), (470, 250), (600, GATE_HZ - 10)], 200, 823, near=2, spread0=80.0, spread1=8.0, trail=8)

    # The arch, his black stone: courses in the jambs, voussoirs round the opening, the inner edge lit warm where the
    # stream passes close beyond it and cold-dark where it does not.
    for y in range(0, THRESHOLD + 1):
        for x in range(FW):
            if in_arch(x, y):
                continue
            c = OCC
            if x < ARCH_CX - ARCH_RX or x > ARCH_CX + ARCH_RX or y >= ARCH_SPRING:
                course = (y % 26) == 0 or ((x + (y // 26) * 37) % 64 == 0 and y % 26 < 26)
                if course:
                    c = S[4]
            else:
                ang = math.atan2(y - ARCH_SPRING, (x - ARCH_CX) * ARCH_RY / float(ARCH_RX))
                if int(abs(ang) * 18) % 2 == 0 and abs(((abs(ang) * 18) % 1.0) - 0.5) > 0.46:
                    c = S[4]
            fore.set(x, y, c)
    # The intrados: two pixels round the opening, warm on the side the stream passes, stone elsewhere.
    for y in range(0, THRESHOLD):
        for x in range(ARCH_CX - ARCH_RX - 4, ARCH_CX + ARCH_RX + 5):
            if in_arch(x, y) or not (in_arch(x + 2, y) or in_arch(x - 2, y) or in_arch(x, y + 2)):
                continue
            near = math.hypot(x - 470, y - 150) < 210
            warm = near and pr.dith(x, y, 0.75)
            fore.set(x, y, G[3] if warm else S[3])
            if near and in_arch(x + 1, y + 1) is False and pr.dith(x + 1, y, 0.4):
                fore.set(x, y, G[2])
    # A grating, raised into the arch's head: its teeth hanging just inside the opening.
    for gx in range(ARCH_CX - 150, ARCH_CX + 151, 14):
        top = None
        for y in range(0, ARCH_SPRING):
            if in_arch(gx, y):
                top = y
                break
        if top is None:
            continue
        fore.vline(gx, top, top + 14, S[3])
        fore.set(gx, top + 15, S[2])
    fore.hline(ARCH_CX - 156, ARCH_CX + 156, 0, OCC)

    # The steps going down beyond the threshold to the water, and the passage floor coming toward us.
    for k, (ya, yb) in enumerate([(502, 512), (512, 524), (524, 540)]):
        for y in range(ya, yb):
            for x in range(ARCH_CX - ARCH_RX, ARCH_CX + ARCH_RX + 1):
                lit = y == ya
                c = (S[2] if pr.dith(x, y, 0.5) else S[3]) if lit else (S[4] if pr.dith(x, y, 0.6 - k * 0.15) else OCC)
                fore.set(x, y, c)
    for y in range(THRESHOLD, FH):
        depth = (y - THRESHOLD) / float(FH - THRESHOLD)
        joint = (y - THRESHOLD) % int(8 + depth * 40) == 0
        for x in range(FW):
            c = S[4] if pr.dith(x, y, 0.55 - depth * 0.3) else OCC
            if joint:
                c = OCC
            fore.set(x, y, c)
    fore.hline(ARCH_CX - ARCH_RX, ARCH_CX + ARCH_RX, THRESHOLD, S[2])
    # His light on the floor round him, as gold pigment.
    fore.radial(HIM_X + 14, HIM_Y + 4, 150, 46, [G[3], G[3], G[4], G[4], None])
    fore.radial(HIM_X + 14, 506, 120, 10, [G[3], G[4], None])
    # The title's figure from behind (the road's), watching it go: his back to us, his staff's light up.
    pr.player_back(fore, HIM_X, HIM_Y, HIM_H)

    layers = dict(back=back, mid=mid, fore=fore)
    pr.finish(layers, 0.85, cy=pr.CY + 40)
    return layers


# --------------------------------------------------------------------------- the far sea
SEA_HZ = 372
QUAY_Y = 548
MAN_X, MAN_Y, MAN_H = 352, 548, 54


def far_cliffs(c, x0, seed):
    """The far shore's white cliffs on the horizon: a long low line of chalk headlands from x0 to past the right edge,
    each lit on its west face by what light there is and dark on its seaward flank, the surf a broken line under it."""
    rng = random.Random(seed)
    x = float(x0)
    while x < FW + 20:
        w = 18 + rng.random() * 46
        if rng.random() < 0.25:
            x += 4 + rng.random() * 10              # a cove: the line drops to the water for a moment
        tall = 3 + rng.random() * 9 + (x - x0) / float(FW - x0) * 4
        crest = 0.2 + rng.random() * 0.3
        for xx in range(int(x), int(min(FW, x + w))):
            u = (xx - x) / w
            h = tall * (0.7 + 0.3 * math.sin(min(1.0, u / crest) * math.pi / 2) - 0.5 * max(0.0, u - 0.82) / 0.18)
            top = int(round(SEA_HZ - h))
            for y in range(top, SEA_HZ + 1):
                depth = (y - top) / max(1.0, h)
                if y == top:
                    col = S[4]
                elif u > 0.82 or (xx * 7 + int(x)) % 11 == 0:
                    col = K[4] if pr.dith(xx, y, 0.6) else S[4]
                else:
                    # Far off at night: the lit west face mostly the chalk's shade, its brightest only near the top.
                    col = K[2] if pr.dith(xx, y, 0.55 - depth * 0.6 - u * 0.4) else (K[3] if pr.dith(xx, y, 0.8 - depth * 0.4) else K[4])
                c.set(xx, y, col)
            if rng.random() < 0.5:
                c.set(xx, SEA_HZ + 1, S[2])
        x += w


def p_sea():
    """2. Wide and low over the water from the quay at the rock's foot: the stream running on east toward the white
    cliffs on the horizon and on past the edge of the picture; the Archmage small on the quay below his rock."""
    back, mid, fore = pr.frame(True), pr.frame(), pr.frame()
    pr.sky(back, 0, SEA_HZ + 2, [OCC, S[4], V[4], V[3], V[2]])
    pr.stars(back, (0, 0, FW, SEA_HZ - 40), 420, 831)
    back.radial(640, SEA_HZ, 380, 44, [V[3], V[3], V[4], None])
    water(back, SEA_HZ + 1, FH, 833, 520)
    far_cliffs(back, 452, 835)
    gold_on_water(back, 612, SEA_HZ + 4, 480, 4, 90, 837, 260)

    # The light: in from over the rock behind him, high, and away east down toward the cliffs and over them, thinning
    # to single sparks - and on past the edge of the picture (past the phone's at column 660 and the PC's at 800).
    stream(mid, [(110, 20), (280, 120), (470, 250), (612, SEA_HZ - 16), (760, SEA_HZ - 24), (FW + 10, SEA_HZ - 32)],
           760, 841, near=4, spread0=190.0, spread1=5.0, trail=14)
    stream(mid, [(240, 60), (500, 230), (660, SEA_HZ - 20), (FW + 10, SEA_HZ - 36)], 240, 843, near=2,
           spread0=90.0, spread1=6.0, trail=8)

    # His rock at the left, black, rising out of frame, and the quay at its foot with the sea-gate's steps.
    for y in range(0, FH):
        edge = 236 + (y / float(FH)) * 40 + math.sin(y / 23.0) * 6 + (14 if y > 420 else 0)
        for x in range(0, int(edge)):
            c = OCC
            # Bedding in his black rock, and the face catching a little of the light along its edge.
            if (y + int(math.sin(x / 31.0) * 4)) % 23 == 0 or (x > edge - 6 and pr.dith(x, y, 0.3)):
                c = S[4]
            fore.set(x, y, c)
        fore.set(int(edge), y, S[4])
        fore.set(int(edge) + 1, y, S[3] if pr.dith(int(edge), y, 0.4) else S[4])
    for y in range(QUAY_Y, QUAY_Y + 14):
        for x in range(0, 420 - (y - QUAY_Y)):
            top = y == QUAY_Y
            c = (S[2] if pr.dith(x, y, 0.5) else S[3]) if top else (S[4] if pr.dith(x, y, 0.6 - (y - QUAY_Y) * 0.04) else OCC)
            if not top and x % 18 == 0:
                c = OCC
            fore.set(x, y, c)
    fore.hline(0, 406, QUAY_Y + 14, OCC)
    rng = random.Random(845)
    for x in range(0, 430, 2):                     # the sea at the quay's foot
        if rng.random() < 0.5:
            fore.set(x, QUAY_Y + 15 + int(rng.random() * 3), S[2])
    fore.radial(MAN_X + 8, MAN_Y + 2, 90, 14, [G[3], G[4], None])
    pr.player_back(fore, MAN_X, MAN_Y, MAN_H)
    # Near water at the bottom, for the tall screen: long swells, catching the light a little.
    for k in range(40):
        y = 640 + k * 5 + int(rng.random() * 3)
        x = rng.random() * FW
        fore.hline(x, x + 10 + rng.random() * 30, y, S[3] if rng.random() < 0.5 else V[3])

    layers = dict(back=back, mid=mid, fore=fore)
    pr.finish(layers, 0.85, cy=pr.CY + 20)
    return layers


PANELS = [("lightgoes-gate", p_gate), ("lightgoes-sea", p_sea)]


def preview(name, layers):
    flat = pr.composite(layers)
    wide = flat.crop(pr.VIEW_WIDE)
    tall = flat.crop(pr.VIEW_TALL)
    sheet = Image.new("RGBA", (wide.size[0] + tall.size[0] + 12, tall.size[1]), (40, 40, 40, 255))
    sheet.paste(wide, (0, 0))
    sheet.paste(tall, (wide.size[0] + 12, 0))
    if not os.path.isdir(PREVIEW):
        os.makedirs(PREVIEW)
    sheet.save(os.path.join(PREVIEW, name + ".png"))


def main(argv):
    want = [a for a in argv if not a.startswith("--")]
    do_preview = "--preview" in argv
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    total_bad = 0
    for name, fn in PANELS:
        if want and name not in want:
            continue
        layers = fn()
        bad = pr.audit(layers)
        total_bad += sum(bad.values())
        for ln, c in layers.items():
            c.img.save(os.path.join(OUT, "%s-%s.png" % (name, ln)))
        if do_preview:
            preview(name, layers)
        print("%-16s %d off-contract pixels%s" % (name, sum(bad.values()),
                                                  (" " + str(sorted(bad.items())[:4])) if bad else ""))
    return 1 if total_bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
