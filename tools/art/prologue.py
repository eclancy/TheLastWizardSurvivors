# -*- coding: utf-8 -*-
"""The story-mode prologue: eight illustrated panels, drawn by code on the Bonelight contract.

    python tools/art/prologue.py              # every panel, written to assets/bonelight/ui/prologue/
    python tools/art/prologue.py moot throne  # only these panels
    python tools/art/prologue.py --preview    # also write tools/art/_prologue/<panel>.png

Runs under Python 2.7 and 3 (PIL only). Every random stream draws from rng.random(), never
randrange - Python 3 changed randrange, and a generator whose trees move when the interpreter
changes cannot be regenerated to fix one detail (.ai/title-screen.md, "The eyes are spread").

See .ai/prologue.md for what each panel shows and why; scripts/Prologue.cs plays them.

THE FRAME. Every layer is 960x840, drawn at a whole x2 into a 1920x1680 rect centred on the
canvas, the same move the title screen makes in each direction at once:

  - a PC canvas is 720 tall (keep_height), so it shows rows 240-600 and, at 16:9, columns
    160-800 - wider monitors more of the 960;
  - a phone canvas is 720 wide (keep_width), so it shows columns 300-660 and, at 16:9, rows
    100-740 - a Pixel 9 rows 16-824.

The 360x360 square both of them always show (columns 300-660, rows 240-600) holds every subject.
Around it the scene carries on - sky above and ground below for the phone, land either side for
the PC - and falls to occlusion toward the frame edge, so a screen wider or taller than the frame
meets the scene's black ground invisibly. Nothing is ever stretched to fit.

LAYERS. Each panel is back (opaque) + mid + fore (transparent), so Prologue.cs can drift them at
different rates. The drift is a few art pixels over the whole panel; every layer is drawn out to
the frame edge so a drift never uncovers anything. A panel may also have one animated box of its
mid layer, <name>-anim.png and <name>-anim.json (write_anim; only "missed" does).
"""

from __future__ import division, print_function

import json
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image, ImageFilter

import bonelight as bl
import hero
import raster
import splashkit as sk
import roster

# The rows the panels' pigments step along. bonelight.py's, plus the rows a player sprite wears that
# bonelight.py keeps out of its contract on purpose (art-direction.md section 3, matte.add_ramp): Thora's
# russet hair (roster.RUSSET, 2026-11-03). The freeing draws the real sprites, so without it her hair
# stayed lit while the others knelt in the dark, and audited off-contract.
M = dict(bl.MATERIALS, russet=roster.RUSSET)
E = bl.ELEMENTS
OCC = bl.OCC
RIM_SOFT = bl.RIM_SOFT
S, V, W, K, G = M["stone"], M["violet"], M["wool"], M["skin"], M["gold"]
ST, L, LI, R, AR = M["steel"], M["linen"], M["lichen"], M["red"], M["arcane"]
DK, LT = E["darkness"], E["light"]

FW, FH = 960, 840
CX, CY = FW // 2, FH // 2
# What each canvas shows, in frame pixels (x0, y0, x1, y1). The tall view is the Pixel 9's.
VIEW_WIDE = (160, 240, 800, 600)
VIEW_TALL = (300, 16, 660, 824)
SAFE = (300, 240, 660, 600)

ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "assets", "bonelight", "ui", "prologue")
PREVIEW = os.path.join(HERE, "_prologue")

TH = [[raster.DITHER[y][x] / 64.0 for x in range(8)] for y in range(8)]

_RGB = {}


def rgb(c):
    if c is None or not isinstance(c, str):
        return c
    v = _RGB.get(c)
    if v is None:
        v = _RGB[c] = raster.rgb(c)
    return v


def lum(c):
    return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]


class Cv(raster.Canvas):
    """raster.Canvas with the colour parse cached. The frames here are 806,400 pixels a layer,
    and raster's set() re-parses a hex string on every write."""

    def set(self, x, y, c):
        if c is None:
            return
        x = int(x)
        y = int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[x, y] = rgb(c)


def frame(opaque=False, w=FW, h=FH):
    return Cv(w, h, OCC if opaque else None)


def over(dst, src, x=0, y=0):
    """Composite src onto dst: opaque src pixels replace, transparent ones leave dst alone."""
    dst.img.paste(src.img, (int(x), int(y)), src.img)
    dst.px = dst.img.load()


def ri(rng, a, b):
    """randrange(a, b) from rng.random(), which is the same stream under Python 2 and 3."""
    return a + int(rng.random() * (b - a))


def pick(rng, seq):
    return seq[min(len(seq) - 1, int(rng.random() * len(seq)))]


def dith(x, y, f):
    return f > TH[y % 8][x % 8]


# --------------------------------------------------------------------------- light passes
def darken(c, t_of, colors, box=None):
    """Darken every opaque pixel through colors by a per-pixel t (splash._darken's indexing:
    None leaves a pixel alone, the last entry is reached at t = len(colors)). Only ever darker."""
    cols = [rgb(k) for k in colors]
    lums = [lum(k) if k else None for k in cols]
    n = len(cols)
    x0, y0, x1, y1 = box or (0, 0, c.w, c.h)
    px = c.px
    for y in range(max(0, y0), min(c.h, y1)):
        row = TH[y % 8]
        for x in range(max(0, x0), min(c.w, x1)):
            cur = px[x, y]
            if not cur[3]:
                continue
            t = t_of(x, y)
            if t <= 0:
                continue
            i = min(n - 1, int(t))
            idx = i if (t - i) > row[x % 8] else i - 1
            if idx >= 0 and cols[idx] is not None and lums[idx] < lum(cur):
                px[x, y] = cols[idx]


def frame_t(vig=1.0, cy=CY + 20):
    """The scene vignette plus the fall to occlusion beyond what any 16:9 screen shows.

    Inside the views it is a gentle radial darkening (the title's power-2.2 shape); past the
    16:9 edges it climbs to occlusion at the frame edge, sideways for an ultrawide monitor and
    up and down for a tall phone."""
    def t_of(x, y):
        dx = (x - CX) / 430.0
        dy = (y - cy) / 430.0
        t = (math.sqrt(dx * dx + dy * dy)) ** 2.2 * 1.6 * vig
        ex = abs(x - CX) - 296
        if ex > 0:
            t = max(t, (ex / 184.0) ** 1.15 * 2.2)
        ey = abs(y - CY) - 300
        if ey > 0:
            t = max(t, (ey / 120.0) ** 1.15 * 2.2)
        return t
    return t_of


def finish(layers, vig=1.0, cy=CY + 20):
    t_of = frame_t(vig, cy)
    for name in layers:
        darken(layers[name], t_of, [S[4], OCC])


def edge_light(c, color, toward, skip_below=None, depth=1, inner=None, box=None):
    """Light the silhouette edge of everything on c that faces `toward` (a unit-ish vector
    pointing at the light). A pixel is lit when the pixel one step toward the light is empty.
    `inner` colours the second pixel in. Run on a transparent scratch layer only: on an
    opaque canvas every pixel has a neighbour and nothing is lit (.ai/title-screen.md, bug 2)."""
    dx = 1 if toward[0] > 0.33 else (-1 if toward[0] < -0.33 else 0)
    dy = 1 if toward[1] > 0.33 else (-1 if toward[1] < -0.33 else 0)
    px = c.px
    hits = []
    bx0, by0, bx1, by1 = box or (1, 1, c.w - 1, c.h - 1)
    for y in range(max(1, int(by0)), min(c.h - 1, int(by1))):
        if skip_below is not None and y > skip_below:
            break
        for x in range(max(1, int(bx0)), min(c.w - 1, int(bx1))):
            if px[x, y][3] and not px[x + dx, y + dy][3]:
                hits.append((x, y))
    for (x, y) in hits:
        c.set(x, y, color)
        if inner:
            c.set(x - dx, y - dy, inner)


# --------------------------------------------------------------------------- land
def sky(c, y0, y1, stops):
    c.vramp(y0, y1, stops)


def stars(c, box, n, seed, colors=None):
    colors = colors or [K[0], K[1], K[2], AR[1]]
    rng = random.Random(seed)
    x0, y0, x1, y1 = box
    for _ in range(n):
        x = x0 + rng.random() * (x1 - x0)
        y = y0 + rng.random() ** 1.3 * (y1 - y0)
        col = pick(rng, colors)
        c.set(x, y, col)
        if rng.random() < 0.06:                         # a few brighter ones carry a cross
            for (ox, oy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                c.set(x + ox, y + oy, S[0])


def ridge_fn(y_base, amp, seed, freq=1.0):
    rng = random.Random(seed)
    ph = [rng.random() * 6.283 for _ in range(4)]

    def h(x):
        return y_base - amp * (0.52 * math.sin(x * 0.0105 * freq + ph[0])
                               + 0.30 * math.sin(x * 0.026 * freq + ph[1])
                               + 0.12 * math.sin(x * 0.067 * freq + ph[2])
                               + 0.06 * math.sin(x * 0.173 * freq + ph[3]))
    return h


def hills(c, h, stops, depth, lit=None, y_end=FH):
    """Fill everything below the ridge line h(x) with a dithered ramp `depth` rows deep, the
    last stop carried on to y_end. `lit` marks the crest where the slope faces the key."""
    n = len(stops)
    for x in range(c.w):
        top = int(round(h(x)))
        for y in range(max(0, top), min(c.h, y_end)):
            t = min(1.0, (y - top) / float(depth)) * (n - 1)
            i = min(n - 2, int(t))
            c.set(x, y, stops[i + 1] if dith(x, y, t - i) else stops[i])
        if lit and h(x + 1) < h(x - 1) - 0.25:
            c.set(x, top, lit)


def tuft(c, x, yb, h, rng, dark, light=None, blades=4):
    """A heather or grass tuft from one root. sk.grass draws the same thing but spends
    randrange; this one keeps to random()."""
    n = blades + ri(rng, 0, 3)
    for i in range(n):
        t = (i / float(max(1, n - 1))) * 2.0 - 1.0
        tx = x + t * h * 0.55 + (rng.random() - 0.5) * h * 0.2
        ty = yb - h * (0.5 + rng.random() * 0.55)
        c.line(x + t * h * 0.12, yb, tx, ty, dark)
    if light:
        c.line(x, yb, x + (rng.random() - 0.5) * h * 0.5, yb - h * (0.6 + rng.random() * 0.4), light)


def heath(c, y0, y1, n, seed, tones, box=None, keep_out=None, scale=1.0):
    """Heather in clumps, small and dense at the horizon, larger toward the camera. tones is
    [(dark, top), ...] picked per clump; dark sits under, top catches what light there is."""
    rng = random.Random(seed)
    x0, x1 = (box[0], box[1]) if box else (0, FW)
    for _ in range(n):
        d = rng.random() ** 1.4
        y = y0 + d * (y1 - y0)
        x = x0 + rng.random() * (x1 - x0)
        if keep_out and keep_out(x, y):
            continue
        r = (1.0 + d * 5.5 * rng.random()) * scale
        dark, top = pick(rng, tones)
        c.disc(x, y, r * 1.7, r * 0.8, dark)
        c.disc(x - r * 0.4, y - r * 0.35, r * 1.0, r * 0.45, top)


def standing_stone(c, x, yb, w, h, seed, lean=0.0, ramp=None, warm=None, toward=None,
                   broken=False):
    """A standing stone on its own scratch layer, composited onto c.

    Shaded by the key from the upper left on the stone ramp, cracked and spotted with lichen.
    warm=(ramp, strength) washes the face toward a fire in pigment (never the element ramp:
    what the fire lands on is stone); toward=(dx, dy) puts a warm edge on the side facing it."""
    rng = random.Random(seed)
    ramp = ramp or S
    pad = int(w + h * abs(lean) + 8)
    s = Cv(pad * 2 + int(w) * 2, int(h) + 12)
    ox, oy = pad + w, int(h) + 4
    j = lambda a: (rng.random() - 0.5) * a
    top_h = h * (0.55 + rng.random() * 0.2) if broken else h
    # a slanted, uneven crown: weathered stone, not a dressed headstone
    tl, tr = 0.80 + rng.random() * 0.14, 0.72 + rng.random() * 0.22
    peak = -0.25 + rng.random() * 0.5
    pts = [(ox - w * 0.5, oy),
           (ox - w * 0.52 + j(w * 0.1) + lean * top_h * 0.45, oy - top_h * 0.55),
           (ox - w * 0.46 + j(w * 0.1) + lean * top_h, oy - top_h * tl),
           (ox + w * peak + lean * top_h, oy - top_h - j(3)),
           (ox + w * 0.30 + j(w * 0.15) + lean * top_h, oy - top_h * tr),
           (ox + w * 0.46 + j(w * 0.1) + lean * top_h * 0.8, oy - top_h * 0.70),
           (ox + w * 0.52 + lean * top_h * 0.3, oy - top_h * 0.30),
           (ox + w * 0.5, oy)]
    if broken:                                        # a snapped top: a jagged cut, not a dome
        pts[3] = (ox - w * 0.05 + lean * top_h, oy - top_h * 0.82)
        pts[4] = (ox + w * 0.18 + lean * top_h, oy - top_h * 1.02)
    s.poly_shade(pts, [ramp[1], ramp[2], ramp[3], ramp[4]], ang=0.62, gamma=0.85)
    # the shadowed flank: a band down the right side, one step darker again
    for yy in range(int(oy - top_h), oy + 1):
        row = [xx for xx in range(s.w) if s.px[xx, yy][3]]
        if not row:
            continue
        for xx in range(max(row[0], row[-1] - int(w * 0.18)), row[-1] + 1):
            s.set(xx, yy, ramp[4] if (xx + yy) % 3 else OCC)
    # cracks and lichen
    for _ in range(int(h / 18) + 1):
        cy = oy - rng.random() * top_h * 0.85
        cx0 = ox - w * 0.4 + rng.random() * w * 0.3
        s.line(cx0, cy, cx0 + w * (0.25 + rng.random() * 0.35), cy + j(4), OCC)
    for _ in range(int(w * h / 90)):
        lx = ox - w * 0.45 + rng.random() * w * 0.6
        ly = oy - rng.random() * top_h * 0.8
        if s.get(int(lx), int(ly))[3]:
            s.set(lx, ly, LI[2] if rng.random() < 0.6 else LI[3])
    if warm:
        wr, strength = warm
        for yy in range(s.h):
            f = strength * (0.35 + 0.65 * ((yy - (oy - top_h)) / float(top_h)))
            for xx in range(s.w):
                if s.px[xx, yy][3] and dith(xx, yy, f):
                    s.set(xx, yy, wr[3] if dith(xx + 3, yy + 1, f * 0.5) else wr[4])
    if toward:
        edge_light(s, (toward[2] if len(toward) > 2 else G[2]), toward)
    s.hline(ox - w * 0.55, ox + w * 0.55, oy, OCC)
    s.hline(ox - w * 0.45, ox + w * 0.45, oy + 1, OCC)
    over(c, s, x - ox, yb - oy)


# --------------------------------------------------------------------------- people
# THE WIZARDS ARE THE GAME'S OWN (Eric, 2026-11-01, of the anonymous robe-and-hat figure that stood
# here: "In the intro, the wizard silhouettes look goofy. Use the silhouettes of the characters in the
# game."). Each one is cut from the sprite the player sees on the character select and in a run - the
# 48 cell's own pixels, its contact line and loose sparks dropped, smoothed onto the panel's art grid
# at whatever size the panel needs - filled flat, rimmed by the light of the panel, and lit only at
# the staff or the hand that carries his light in the game.
#
# WHICH OF THEM: the moot and the fall show the wizards a fresh save already sees - the four
# disciplines and John Dee. The procession ("He took them all") shows every one of them, the secret
# three as well: Tituba, Vainamoinen and Laelaps are secret on the character select until their
# chapters open (UnlockCatalog's secretUntilStage), and Eric chose to show them here anyway (2026-11-01,
# "Show them") - "all" has to mean all, and a dark shape on a ridge names nobody.
CHARS = os.path.join(ROOT, "assets", "bonelight", "characters")
CELL = 48
# The rows a wizard stands in on his cell: feet on row 46, the contact line on 47, the hat near row 2.
CELL_STAND = 46.0

# (sprite prefix, the ramp his light is in the sprite, the ramp it burns here). Dee's orb is the
# darkness ramp in the game, but in the prologue that ramp as light is the dark wizard's alone
# (.ai/prologue.md, rules), so here Dee burns arcane.
PYRO = ("pyromancer", "fire", "fire")
FROST = ("frostweaver", "ice", "ice")
STORM = ("stormcaller", "lightning", "lightning")
GEO = ("geomancer", "earth", "earth")
DEE = ("dee", "darkness", "arcane")
ROSTER = [PYRO, FROST, STORM, GEO, DEE]
# The three the player frees, for the procession only (above). Tituba's staff head is her water,
# Vainamoinen's his grass; Laelaps carries no light of his own, so his falls back to his middle unless
# the gold of his collar star is found.
TITUBA = ("tituba", "water", "water")
VAINAMOINEN = ("vainamoinen", "grass", "grass")
LAELAPS = ("laelaps", "light", "wind")

_CELLS = {}


def _sprite_cell(prefix, strip, frame):
    key = (prefix, strip, frame)
    if key not in _CELLS:
        img = Image.open(os.path.join(CHARS, "%s-%s.png" % (prefix, strip))).convert("RGBA")
        _CELLS[key] = img.crop((frame * CELL, 0, frame * CELL + CELL, CELL))
    return _CELLS[key]


def silhouette(prefix, strip="idle", frame=0, carried=None):
    """A wizard's shape from his sprite, and where his light is on it.

    Returns (pixels, anchor): the set of the cell's opaque pixels, less the contact line under his
    feet (occlusion, on the last row) and any loose cluster under four pixels (the sparks round a
    hand, which at panel size are noise); and the centre of the topmost cluster of his own ramp's
    pixels (the lantern, the trident's head, the raised fork, the orb), or None."""
    img = _sprite_cell(prefix, strip, frame)
    px = img.load()
    occ = rgb(OCC)[:3]
    on = set()
    for y in range(CELL):
        for x in range(CELL):
            p = px[x, y]
            if p[3] and not (y == CELL - 1 and p[:3] == occ):
                on.add((x, y))
    keep = set()
    seen = set()
    for p0 in sorted(on):
        if p0 in seen:
            continue
        comp = [p0]
        seen.add(p0)
        i = 0
        while i < len(comp):
            x, y = comp[i]
            i += 1
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    q = (x + dx, y + dy)
                    if q in on and q not in seen:
                        seen.add(q)
                        comp.append(q)
        if len(comp) >= 4:
            keep.update(comp)
    anchor = None
    if carried:
        ramp = set(rgb(c)[:3] for c in E[carried])
        pts = [(x, y) for (x, y) in keep if px[x, y][:3] in ramp]
        if pts:
            top = min(y for (_x, y) in pts)
            cl = [(x, y) for (x, y) in pts if y <= top + 5]
            anchor = (sum(x for x, _y in cl) / float(len(cl)) + 0.5, sum(y for _x, y in cl) / float(len(cl)) + 0.5)
    return keep, anchor


def _mask_at(pixels, k, facing):
    """The cell's pixels as a mask at k panel pixels per sprite pixel: blown up x8, its corners
    rounded a little, and brought down to size by area. A sprite scaled by a fraction with nearest
    neighbour is a staircase of uneven steps; this keeps the shape and gives it a clean edge."""
    m = Image.new("L", (CELL, CELL), 0)
    mp = m.load()
    for (x, y) in pixels:
        mp[x, y] = 255
    if facing < 0:
        m = m.transpose(Image.FLIP_LEFT_RIGHT)
    big = m.resize((CELL * 8, CELL * 8), Image.NEAREST).filter(ImageFilter.GaussianBlur(2.5))
    n = max(1, int(round(CELL * k)))
    return big.resize((n, n), Image.ANTIALIAS)


def roster_figure(c, who, cx, yb, h, body, strip="idle", frame=0, facing=1, lit=True, dim=False,
                  warm=None, toward=None):
    """One of the game's wizards as a silhouette, his feet on (cx, yb), h tall if he stood upright.

    who: a ROSTER entry. strip/frame: which of his frames - idle to stand, moving to walk, the death's
    third to be brought to his knees. body: [fill, unused]. lit: his light burns at its anchor in its
    own ramp; dim: it is out, a dull stone where it was. warm: a ramp to dither a little of onto him
    (the fire in the moot). toward: (dx, dy[, colour]) to rim the edges facing a light.
    Returns (light, front): where his light is, and the front of him at his waist (where a chain
    would hold him), in frame pixels; either may be None."""
    prefix, carried, burns = who
    k = h / CELL_STAND
    pixels, anchor = silhouette(prefix, strip, frame, carried)
    m = _mask_at(pixels, k, facing)
    mw, mh = m.size
    mp = m.load()
    s = Cv(mw + 4, mh + 4)
    for y in range(mh):
        for x in range(mw):
            if mp[x, y] >= 112:
                s.set(x + 2, y + 2, body[0])
    if toward:
        edge_light(s, toward[2] if len(toward) > 2 else (warm[2] if warm else G[2]), toward)
    if warm:
        lit_edge = rgb(toward[2] if toward and len(toward) > 2 else warm[2])
        for y in range(s.h):
            for x in range(s.w):
                p = s.px[x, y]
                if p[3] and p != lit_edge and dith(x, y, 0.22):
                    s.set(x, y, warm[4])
    left = int(round(cx - (CELL / 2.0) * k)) - 2
    top = int(round(yb - (CELL - 1) * k)) - 2
    # The contact line: what of him touches the ground, on the ground row, in occlusion.
    feet = [x for x in range(s.w) for y in range(max(0, s.h - 4 - int(k)), s.h) if s.px[x, y][3]]
    over(c, s, left, top)
    if feet:
        c.hline(left + min(feet) - 1, left + max(feet) + 1, yb, OCC)
    light = None
    if anchor:
        ax = cx + (anchor[0] - CELL / 2.0) * k * facing
        ay = yb + (anchor[1] - (CELL - 1)) * k
        light = (ax, ay)
        ramp = E[burns]
        if dim:
            c.disc(ax, ay, 1.1 * k, 1.1 * k, ST[3])
            c.set(ax - 0.4 * k, ay - 0.4 * k, ST[2])
        elif lit:
            c.radial(ax, ay, 3.6 * k, 3.6 * k, [ramp[3], ramp[3], None])
            c.radial(ax, ay, 1.4 * k, 1.4 * k, [ramp[0], ramp[1], ramp[2], ramp[3]])
    # His front at the waist: the furthest opaque pixel the way he faces.
    wy = int(round(yb - 0.42 * h)) - top
    row = [x for x in range(s.w) if 0 <= wy < s.h and s.px[x, wy][3]]
    front = None
    if row:
        fx = (max(row) if facing > 0 else min(row)) + left
        front = (fx, yb - 0.42 * h)
    return light, front


def light_at(who, cx, yb, h, strip="idle", frame=0, facing=1):
    """Where roster_figure would put his light, without drawing him (for what is drawn before him)."""
    prefix, carried, _burns = who
    k = h / CELL_STAND
    _pixels, anchor = silhouette(prefix, strip, frame, carried)
    if not anchor:
        return (cx, yb - h * 0.6)
    return (cx + (anchor[0] - CELL / 2.0) * k * facing, yb + (anchor[1] - (CELL - 1)) * k)


# In the moot, before the fall, each wears the deep end of their own colour - the only time the prologue
# lets them have one - so the circle reads as five different people at a glance, not five shadows.
ROBE = {"pyromancer": R[4], "frostweaver": M["azure"][4], "stormcaller": G[4], "geomancer": M["verdant"][4],
        "dee": ST[4]}


EYE = (AR[0], AR[1])


def creature(c, kind, cx, yb, h, facing=1, body=None, rim=None, eye=True, seed=0):
    """One of his, as a silhouette: the taxonomy's class attachment survives the solid fill
    (.ai/art-direction.md section 4) - the swarmer's cowl, the runner's streaming rag, the
    bruiser's horns and pauldrons, the shielder's flat plane, the caster's loose orb, the
    knight's spear - and every one wears the same mark, one or two pixels of cold arcane, the
    only light on anything he raised (.ai/world-and-tone.md). Limbs are strokes, so a large one
    has joints and weight and does not read as a box with horns on it."""
    body = body or [OCC, OCC]
    u = h / 20.0
    rng = random.Random(seed)
    s = Cv(int(34 * u) + 8, int(28 * u) + 8)
    ox, oy = s.w // 2, int(25 * u) + 3
    f = facing
    b0, b1 = body
    j = lambda a: (rng.random() - 0.5) * a

    def P(pts):
        return [(ox + f * px_ * u, oy + py_ * u) for (px_, py_) in pts]

    def limb(a, b, a2, b2, r0, r1, col=None):
        sk._stroke(s, ox + f * a * u, oy + b * u, ox + f * a2 * u, oy + b2 * u,
                   max(0.5, r0 * u), max(0.5, r1 * u), col or b0)
    stride = j(2.4)
    eyes = []
    if kind == "swarmer":
        s.poly(P([(-4.2, 0), (-2.6, -0.8), (-1.0, 0.2), (1.2, -0.6), (3.6, 0), (4.0, -6.0), (3.4, -11.0),
                  (2.2, -14.6), (3.4, -17.6), (1.0, -19.6), (-1.6, -17.2), (-3.4, -12.6), (-4.4, -6.6)]), b0)
        s.poly(P([(2.2, -14.6), (3.4, -17.6), (1.6, -16.4)]), b1)
        limb(2.6, -9.0, 5.4 + j(1), -5.0, 0.7, 0.5)
        eyes = [(1.8, -15.2)]
    elif kind == "runner":
        limb(-0.6, -7.6, -3.4 + stride, 0, 1.0, 0.7)
        limb(0.8, -7.6, 3.6 - stride, 0, 1.0, 0.7)
        s.poly(P([(-2.4, -7.0), (2.2, -7.6), (4.6, -12.8), (3.8, -15.6), (0.8, -15.0), (-2.0, -11.4)]), b0)
        s.disc(ox + f * 4.4 * u, oy - 15.6 * u, 1.9 * u, 1.7 * u, b0)
        s.poly(P([(-1.4, -13.8), (0.6, -14.6), (-6.0, -12.6), (-11.6, -14.0), (-8.6, -10.8), (-12.4, -9.4),
                  (-6.4, -9.0), (-1.8, -10.4)]), b1)
        limb(3.0, -13.0, 6.6, -9.6, 0.7, 0.5)
        eyes = [(5.2, -15.9)]
    elif kind == "bruiser":
        limb(-2.2, -7.4, -3.4 - stride * 0.4, 0, 1.6, 1.3)
        limb(2.2, -7.4, 3.4 + stride * 0.4, 0, 1.6, 1.3)
        s.poly(P([(-3.8, -6.4), (3.8, -6.4), (6.0, -12.6), (5.0, -16.2), (-5.4, -16.2), (-6.2, -12.4)]), b0)
        for sg in (-1, 1):
            s.disc(ox + f * sg * 5.2 * u, oy - 15.0 * u, 2.6 * u, 2.0 * u, b1)
            limb(sg * 6.0, -14.4, sg * 7.0, -8.0, 1.2, 1.0)
            s.disc(ox + f * sg * 7.0 * u, oy - 7.6 * u, 1.3 * u, 1.3 * u, b0)
        s.disc(ox + f * 0.8 * u, oy - 17.8 * u, 2.2 * u, 2.2 * u, b0)
        for sg in (-1, 1):
            pts = [(0.8 + sg * 1.6, -18.8), (0.8 + sg * 3.6, -20.6), (0.8 + sg * 4.0, -23.6), (0.8 + sg * 2.8, -21.2),
                   (0.8 + sg * 1.0, -19.8)]
            s.poly(P(pts), b0)
        eyes = [(1.6, -18.0), (2.6, -18.0)]
    elif kind == "shielder":
        limb(-1.4, -7.0, -2.6 - stride * 0.4, 0, 1.1, 0.9)
        limb(1.6, -7.0, 2.6 + stride * 0.4, 0, 1.1, 0.9)
        s.poly(P([(-3.2, -6.4), (3.0, -6.4), (3.6, -13.6), (2.2, -15.6), (-2.6, -15.4), (-3.6, -12.4)]), b0)
        s.disc(ox + f * 0.6 * u, oy - 17.4 * u, 2.0 * u, 2.1 * u, b0)
        s.poly(P([(0.6, -19.2), (2.8, -18.4), (0.0, -16.6)]), b0)
        s.poly(P([(3.6, -16.4), (8.6, -17.0), (8.4, -2.2), (3.8, -1.6)]), b1)      # the plane
        s.line(ox + f * 3.6 * u, oy - 16.4 * u, ox + f * 3.8 * u, oy - 1.6 * u, b0)
        eyes = [(1.6, -17.6)]
    elif kind == "caster":
        s.poly(P([(-3.8, 0), (-1.6, -0.6), (0.6, 0.2), (3.6, 0), (2.6, -9), (2.4, -14.6), (1.4, -19.6),
                  (-0.4, -23.4), (-1.6, -20.6), (-2.6, -14.0), (-3.0, -8.0)]), b0)
        limb(1.8, -15.0, 4.6, -18.6, 0.7, 0.5)
        orb_x, orb_y = ox + f * 6.0 * u, oy - 22.6 * u
        s.disc(orb_x, orb_y, 1.5 * u, 1.5 * u, AR[3])
        s.disc(orb_x - u * 0.4, orb_y - u * 0.4, 0.7 * u, 0.7 * u, AR[2])
        eyes = [(0.9, -19.0)]
    elif kind == "knight":
        limb(-1.6, -8.4, -2.4 - stride * 0.4, 0, 1.2, 1.0)
        limb(1.6, -8.4, 2.6 + stride * 0.4, 0, 1.2, 1.0)
        s.poly(P([(-3.4, -7.6), (3.4, -7.6), (4.2, -15.6), (3.0, -17.6), (-3.0, -17.6), (-4.2, -15.4)]), b0)
        s.poly(P([(-3.0, -8.6), (3.2, -8.6), (4.6, -4.6), (-4.4, -4.4)]), b1)       # tabard skirt
        s.poly(P([(-1.8, -17.4), (2.2, -17.4), (2.4, -21.4), (0.2, -23.0), (-1.8, -21.6)]), b0)
        limb(3.2, -15.6, 5.8, -11.4, 1.0, 0.8)
        s.line(ox + f * 6.0 * u, oy - 0.4 * u, ox + f * 6.4 * u, oy - 27.0 * u, b0)
        s.line(ox + f * 6.0 * u + 1, oy - 0.4 * u, ox + f * 6.4 * u + 1, oy - 27.0 * u, b0)
        s.poly(P([(6.4, -27.0), (7.4, -24.4), (6.2, -23.6), (5.4, -24.4)]), b0)
        eyes = [(1.2, -20.2)]
    elif kind == "hound":
        limb(-5.6, -6.4, -6.6 - stride * 0.5, 0, 1.0, 0.7)
        limb(-3.4, -6.4, -3.0 + stride * 0.5, 0, 1.0, 0.7)
        limb(3.6, -7.0, 2.6 + stride * 0.5, 0, 1.0, 0.7)
        limb(5.4, -7.0, 6.4 - stride * 0.5, 0, 1.0, 0.7)
        s.poly(P([(-7.4, -6.0), (-5.6, -9.4), (3.6, -10.2), (6.6, -12.0), (8.2, -10.6), (6.6, -6.6), (-5.6, -5.0)]), b0)
        s.poly(P([(6.6, -12.0), (10.6, -11.6), (11.2, -10.2), (8.2, -9.4)]), b0)
        s.poly(P([(6.8, -12.0), (7.8, -14.4), (8.2, -11.6)]), b0)
        s.poly(P([(-7.4, -6.0), (-11.6, -9.6), (-7.2, -8.0)]), b0)
        eyes = [(8.8, -11.0)]
    if rim:
        edge_light(s, rim, (-0.7, -0.7))
    if eye:
        # Small ones get the arcane row's lit stop alone: massed, its near-white hi read as a sky
        # full of stars rather than as eyes.
        small = u < 1.3
        for i, (ex, ey) in enumerate(eyes):
            s.set(ox + f * ex * u, oy + ey * u, EYE[1] if small or i else EYE[0])
            if u >= 1.6 and len(eyes) == 1:
                s.set(ox + f * ex * u + f, oy + ey * u, EYE[1])
    over(c, s, cx - ox, yb - oy)


# --------------------------------------------------------------------------- the dark wizard
def light_up(s, lx, ly, radius, ladder, power=1.4, seed=0):
    """Step pixels near a light one stop up its ladder, dithered by falloff. The light a source
    throws on cloth is the cloth's own lighter stop - pigment - never the source's ramp
    (.ai/title-screen.md: emissive ramps belong on the emitter, not on what it lights)."""
    up = dict((rgb(a), rgb(b)) for (a, b) in ladder)
    px = s.px
    for y in range(max(0, int(ly - radius)), min(s.h, int(ly + radius) + 1)):
        for x in range(max(0, int(lx - radius)), min(s.w, int(lx + radius) + 1)):
            p = px[x, y]
            if not p[3] or p not in up:
                continue
            d = math.hypot(x - lx, y - ly) / radius
            if d < 1.0 and dith(x + seed, y, (1.0 - d) ** power):
                px[x, y] = up[p]


def dark_wizard(c, cx, yb, h):
    """His first look, as a silhouette only (Eric, 2026-11-01), and the only figure in the game
    allowed to SHED dark light: the
    darkness ramp as emission is his alone (.ai/world-and-tone.md, "The dark wizard is the
    exception that proves it").

    Read from the silhouette, filled black: no hat brim and no staff, which are the player's; a
    deep cowl inside a collar that stands up round his head in points, a throne's back worn as
    a coat, with a thin iron circlet over the cowl; one arm raised, its sleeve hanging off it
    like a wing, and in that hand the light he is taking. At his belt, keys. He has no feet: the
    hem goes on into the ground as shadow, and the shadow is what serves him (panel 3).

    Lit from behind by his own black sun and from the left by the orb in his hand - never by
    the key - so the rim is on the edges those face and nowhere else. A rim all the way round
    is an outline, and the contract has none."""
    u = h / 64.0
    s = Cv(int(44 * u), int(70 * u))
    ox, oy = int(23 * u), int(66 * u)

    def P(pts):
        return [(ox + a * u, oy + b * u) for (a, b) in pts]

    def L(a, b, a2, b2, col):
        s.line(ox + a * u, oy + b * u, ox + a2 * u, oy + b2 * u, col)

    # --- the collar points, behind the cowl -----------------------------------------
    for pts in ([(-7.4, -46.6), (-9.0, -55.4), (-7.0, -53.4), (-7.6, -60.6), (-5.4, -55.0),
                 (-4.8, -58.2), (-3.6, -50.0)],
                [(7.2, -46.6), (8.4, -54.2), (6.6, -52.8), (6.4, -59.4), (4.9, -54.6),
                 (4.4, -57.0), (3.4, -50.0)]):
        s.poly_shade(P(pts), [V[4], OCC], ang=1.5708, gamma=0.8)
    # --- the cloak: shoulders to the ground, the raised arm's sleeve hanging off its left -----
    hem = []
    rng = random.Random(64)
    for i in range(23):
        t = i / 22.0
        hem.append((20.4 - 40.6 * t, (1.6 if i % 2 else -0.6) + rng.random() * 0.9))
    # Sloped shoulders and sides that bow outward to the hem: drawn as straight lines from a flat
    # shoulder line it read as a cabinet with a head on it.
    cloak = P([(-4.6, -48.4), (4.6, -48.4), (7.6, -47.2), (9.8, -45.4), (11.0, -42.6), (11.6, -38.0),
               (12.4, -30.0), (13.8, -20.0), (15.8, -10.6), (18.2, -4.4), (20.0, -1.4)] + hem +
              [(-19.0, -1.8), (-16.8, -6.8), (-14.8, -14.6), (-13.2, -23.4), (-12.0, -31.4),
               (-11.2, -38.6), (-10.6, -42.6), (-9.4, -45.4), (-7.4, -47.2)])
    s.poly_shade(cloak, [V[3], V[4], V[4], OCC], ang=0.95, gamma=0.72)
    # The sleeve of the raised arm: a wing of cloth hanging off the forearm, with clear air
    # between it and his side. Joined to the body it read as a stick with a sliver beside it.
    sleeve = P([(-9.6, -46.4), (-12.8, -48.8), (-16.6, -51.8), (-19.6, -53.0), (-20.6, -49.0),
                (-20.4, -43.0), (-19.0, -36.8), (-17.2, -32.6), (-16.4, -36.6), (-15.0, -41.4),
                (-12.6, -44.4), (-10.4, -44.0)])
    s.poly_shade(sleeve, [V[3], V[4], OCC], ang=0.4, gamma=0.8)
    for (a2, b2) in [(-19.6, -40), (-18.2, -35.4), (-16.0, -40.6)]:
        s.line(ox - 19.4 * u, oy - 51.8 * u, ox + a2 * u, oy + b2 * u, V[3])
        s.line(ox - 19.0 * u, oy - 51.4 * u, ox + (a2 + 0.6) * u, oy + b2 * u, OCC)
    s.line(ox - 12.6 * u, oy - 44.4 * u, ox - 10.6 * u, oy - 45.6 * u, OCC)
    # folds: a lit ridge with its valley beside it, fanning from the shoulders to the hem
    folds = [(-6.0, -44, -15.4, -1.6), (-3.8, -43, -9.4, 0), (-8.6, -44, -17.6, -4.4), (5.4, -43, 9.6, 0),
             (7.8, -42, 14.6, -1.0), (9.6, -38, 18.0, -2.6), (-2.6, -42, -4.8, 0.4)]
    for (a, b, a2, b2) in folds:
        L(a, b, a2, b2, V[3] if a < 0 else V[4])
        L(a + 0.45, b, a2 + 0.8, b2, OCC)
    # --- the robe under it, in the opening: darker, with a dull gold edge down each side ---
    robe = P([(-1.8, -47.6), (2.0, -47.6), (2.8, -36), (4.2, -20), (6.6, 0.6), (-4.8, 0.6),
              (-3.2, -20), (-2.4, -36)])
    s.poly_shade(robe, [V[4], OCC, OCC], ang=0.8, gamma=0.9)
    for edge in ([(-1.8, -47.2), (-2.4, -36), (-3.2, -20), (-4.8, 0.4)],
                 [(2.0, -47.2), (2.8, -36), (4.2, -20), (6.6, 0.4)]):
        for (a, b), (a2, b2) in zip(edge, edge[1:]):
            L(a, b, a2, b2, G[4])
    L(-1.8, -46.8, -2.4, -36.0, G[3])
    for (a, a2) in [(-0.6, -1.4), (1.0, 2.2), (2.4, 4.6)]:
        L(a, -38, a2, -1, V[4])
    # belt and keys
    s.poly(P([(-3.4, -31.6), (4.0, -31.6), (4.1, -30.2), (-3.5, -30.2)]), ST[4])
    s.hline(ox - 3.4 * u, ox + 4.0 * u, oy - 31.6 * u, ST[3])
    s.rect(ox - 0.4 * u, oy - 31.8 * u, ox + 0.6 * u, oy - 30.0 * u, G[3])
    # --- the cowl, peaked and leaning back, the face set deep inside it ----------------
    hood = P([(-4.2, -47.0), (4.4, -47.0), (4.9, -52.4), (3.9, -58.0), (1.6, -61.6), (-0.6, -64.0),
              (-1.6, -62.6), (-3.8, -58.6), (-4.9, -53.0)])
    s.poly_shade(hood, [V[3], V[4], OCC], ang=0.6, gamma=0.7)
    L(-3.4, -58.4, -0.8, -63.2, V[3])
    s.disc(ox + 0.2 * u, oy - 53.8 * u, 2.5 * u, 3.5 * u, OCC)
    face = P([(-1.5, -55.8), (1.5, -55.8), (1.7, -53.4), (0.9, -50.8), (0.1, -50.0), (-0.8, -50.8),
              (-1.6, -53.4)])
    s.poly(face, K[4])
    L(-1.5, -53.4, -0.7, -50.6, K[3])                                  # the edge the orb finds
    L(-0.1, -54.4, 0.0, -52.8, K[3])                                   # the ridge of the nose
    s.hline(ox - 1.5 * u, ox + 1.6 * u, oy - 55.4 * u, OCC)            # brow, deep in shadow
    s.hline(ox - 1.4 * u, ox + 1.5 * u, oy - 55.8 * u, OCC)
    s.hline(ox - 0.5 * u, ox + 0.6 * u, oy - 51.7 * u, OCC)            # mouth
    # the cowl's shadow over the top of the face, so it reads as a face in a hood and not a mask
    for yy in range(int(oy - 56.0 * u), int(oy - 53.0 * u)):
        f = (oy - 53.0 * u - yy) / (3.0 * u)
        for xx in range(int(ox - 1.8 * u), int(ox + 1.9 * u) + 1):
            if s.px[xx, yy][3] and dith(xx, yy, 0.25 + f * 0.7):
                s.set(xx, yy, OCC)
    for ex in (-0.85, 0.75):
        s.set(ox + ex * u, oy - 54.6 * u, DK[0])
        s.set(ox + ex * u + (-1 if ex < 0 else 1), oy - 54.6 * u, DK[2])
    # the circlet: iron over the cowl, three points
    for k in range(int(7.4 * u)):
        x = ox - 3.6 * u + k
        y = oy - 57.4 * u + abs(k - 3.6 * u) * 0.12
        s.set(x, y, ST[3])
        s.set(x, y + 1, OCC)
    for (a, ht) in [(-2.2, 1.4), (0.1, 2.2), (2.2, 1.2)]:
        s.poly(P([(a - 0.55, -57.3), (a + 0.55, -57.3), (a + 0.05, -57.3 - ht)]), ST[3])
        s.set(ox + (a + 0.05) * u, oy - (57.3 + ht) * u + 1, ST[2])
    # --- hands: bone pale and long -------------------------------------------------
    hx, hy = ox - 19.4 * u, oy - 56.4 * u
    s.poly(P([(-17.6, -53.0), (-18.6, -52.4), (-19.8, -55.6), (-18.9, -56.2)]), K[4])   # wrist
    s.disc(hx, hy, 1.1 * u, 0.9 * u, K[3])
    for (a, ln) in [(-2.5, 2.8), (-2.05, 3.4), (-1.6, 3.5), (-1.15, 3.0), (-0.55, 2.0)]:
        x1, y1 = hx + math.cos(a) * ln * u, hy + math.sin(a) * ln * u
        s.line(hx, hy, x1, y1, K[3])
        s.set(x1, y1, K[2])
    # the other hand, out of the cloak at his side, long fingers round a ring of keys
    kx, ky = ox + 7.4 * u, oy - 27.4 * u
    s.poly(P([(6.6, -28.0), (8.6, -28.0), (8.8, -26.2), (6.6, -26.0)]), K[4])
    for (dx, ln) in [(-0.7, 2.6), (0.2, 3.2), (1.0, 2.8)]:
        s.line(kx + dx * u, ky + 1.4 * u, kx + dx * 1.15 * u, ky + (1.4 + ln) * u, K[4])
    s.line(kx - 0.7 * u, ky + 1.0 * u, kx - 0.8 * u, ky + 3.6 * u, K[3])
    rx_, ry_ = kx + 0.2 * u, ky + 5.6 * u
    s.ring(rx_, ry_, 1.2 * u, 1.2 * u, ST[3], 1)
    s.set(rx_ - 0.9 * u, ry_ - 0.6 * u, ST[2])
    for (dx, ln) in [(-0.6, 3.6), (0.5, 4.4)]:
        x0 = rx_ + dx * u
        s.vline(x0, ry_ + 1.0 * u, ry_ + ln * u, ST[3])
        s.hline(x0, x0 + 0.9 * u, ry_ + ln * u, ST[3])
        s.hline(x0, x0 + 0.6 * u, ry_ + (ln - 1.1) * u, ST[3])
    # --- a silhouette only -------------------------------------------------------------
    # Eric, 2026-11-01: "The dark wizard should be shown in at most a silhouette in the
    # prologue." Everything above now only cuts his shape - cowl, collar points, sleeve, the
    # circlet's points, the long fingers, the key ring - and this flattens all of it to one
    # dark, so no face, fold, gold edge or bone reads. His inside is the final fight's to show.
    # The orb no longer lights his cloth for the same reason; the rims below are all that say
    # where his edges are against the black sun.
    for yy in range(s.h):
        for xx in range(s.w):
            if s.px[xx, yy][3]:
                s.set(xx, yy, OCC)
    orb = (hx - 0.2 * u, hy - 4.6 * u)
    shoulder = int(oy - 44 * u)
    edge_light(s, DK[1], (0, -1), box=(0, 0, s.w, shoulder + 2 * u), inner=DK[3])
    edge_light(s, DK[2], (-1, 0), box=(0, 0, ox + 1 * u, oy - 30 * u))
    edge_light(s, DK[2], (1, 0), box=(ox + 3 * u, 0, s.w, shoulder))
    edge_light(s, DK[3], (1, 0), box=(ox + 3 * u, shoulder, s.w, oy - 20 * u))
    over(c, s, cx - ox, yb - oy)
    return (cx - ox + orb[0], yb - oy + orb[1], u)


def black_sun(c, x, y, r, rays=0, seed=0, glow=True):
    """His light: a disc of nothing with the darkness ramp burning round its rim. The corona is
    the emitter - the only place the ramp is used as light in the game - and the sky it lands on
    takes the violet pigment row, as the ward's floor takes gold."""
    if glow:
        c.radial(x, y, r * 2.6, r * 2.6, [V[3], V[3], V[4], None])
        c.radial(x, y, r * 1.7, r * 1.7, [DK[3], DK[3], V[3], None])
    if rays:
        rng = random.Random(seed)
        for i in range(rays):
            a = i * 6.28318 / rays + rng.random() * 0.15
            ln = r * (1.5 + rng.random() * 1.5)
            for k in range(int(ln - r * 1.15)):
                if (k + i) % 2:
                    continue
                d = r * 1.15 + k
                c.set(x + math.cos(a) * d, y + math.sin(a) * d, DK[3] if k > 6 else DK[2])
    c.disc(x, y, r * 1.16, r * 1.16, DK[2])
    c.disc(x, y, r * 1.07, r * 1.07, DK[1])
    c.ring(x, y, r * 1.03, r * 1.03, DK[0], 1)
    c.disc(x, y, r, r, OCC)


def motes(c, src, dst, n, seed, ramps, spread=10, bow=0.0):
    """Light being drawn out of the wizards and into his hand: sparks along a bowed path,
    thinning and shrinking as they near him."""
    rng = random.Random(seed)
    for _ in range(n):
        t = rng.random() ** 0.8
        mx = src[0] + (dst[0] - src[0]) * t
        my = src[1] + (dst[1] - src[1]) * t - math.sin(t * math.pi) * bow
        mx += (rng.random() - 0.5) * spread * (1 - t)
        my += (rng.random() - 0.5) * spread * (1 - t)
        ramp = pick(rng, ramps)
        col = ramp[ri(rng, 0, 3)]
        c.set(mx, my, col)
        if t < 0.45 and rng.random() < 0.4:
            c.set(mx + 1, my, ramp[2])


def player_back(c, cx, yb, h):
    """The player, small and walking away: the title's figure from behind - indigo wool, the
    brimmed hat, the staff in his right hand burning on the light ramp, the ward's colour.
    Lit by the key from the upper left (soft rim, his robe is a dark surface) and from below by
    his own orb."""
    u = h / 32.0
    s = Cv(int(30 * u), int(40 * u))
    ox, oy = int(14 * u), int(37 * u)

    def P(pts):
        return [(ox + a * u, oy + b * u) for (a, b) in pts]
    c.radial(cx + 1 * u, yb + 0.6 * u, 8 * u, 2.2 * u, [OCC, OCC, None])
    cloak = P([(-3.6, -19), (3.6, -19), (5.6, -12), (7.2, -3), (6.8, 0.4), (3.0, 0), (0.6, 0.8),
               (-2.4, 0), (-6.4, 0.6), (-6.6, -3.6), (-5.2, -12)])
    s.poly_shade(cloak, [W[2], W[3], W[4], OCC], ang=0.785, gamma=0.7)
    for (a, b, a2, b2) in [(-1.6, -17, -3.2, 0), (1.2, -17, 2.6, 0), (3.2, -15, 5.4, -1)]:
        s.line(ox + a * u, oy + b * u, ox + a2 * u, oy + b2 * u, W[4])
    s.poly(P([(-3.8, -19.6), (3.8, -19.6), (4.4, -17.4), (-4.4, -17.4)]), W[3])     # shoulders
    s.disc(ox, oy - 21.4 * u, 2.4 * u, 2.2 * u, W[4])                            # back of the head
    brim = P([(-6.8, -22.6), (-3.0, -23.8), (3.2, -23.8), (6.8, -22.4), (3.0, -21.6), (-3.0, -21.6)])
    s.poly_shade(brim, [W[2], W[3], W[4]], ang=0.785, gamma=0.8)
    cone = P([(-3.2, -23.6), (3.4, -23.6), (2.0, -27.4), (0.6, -30.6), (-1.6, -32.4), (-1.2, -29.6),
              (-2.0, -27.0)])
    s.poly_shade(cone, [W[2], W[3], W[4], OCC], ang=0.785, gamma=0.7)
    s.hline(ox - 3.0 * u, ox + 3.2 * u, oy - 23.5 * u, OCC)
    # the staff in his right hand, our right
    sx = ox + 6.0 * u
    s.vline(sx, oy - 27 * u, oy, G[2])
    s.vline(sx + 1, oy - 27 * u, oy - 2 * u, G[3])
    s.disc(sx - 0.4 * u, oy - 13.4 * u, 1.0 * u, 0.9 * u, W[3])                   # his hand on it
    edge_light(s, RIM_SOFT, (-1, 0), box=(0, 0, ox + 1 * u, oy))
    light_up(s, sx, oy - 28 * u, 18 * u, [(W[4], W[3]), (W[3], W[2]), (OCC, W[4])], power=1.6)
    over(c, s, cx - ox, yb - oy)
    oxo, oyo = cx + 6.0 * u + 0.5, yb - 28.6 * u
    c.radial(oxo, oyo, 4.6 * u, 4.6 * u, [LT[3], LT[3], None])
    c.radial(oxo, oyo, 1.7 * u, 1.7 * u, [LT[0], LT[1], LT[2], LT[3]])


# --------------------------------------------------------------------------- the tower
def tower(c, x, yb, h, seed=0, lit=True):
    """His seat, far off: a black spire that thins as it climbs, crowned by his light."""
    rng = random.Random(seed)
    w = h * 0.11
    pts = [(x - w, yb), (x - w * 0.62, yb - h * 0.55), (x - w * 0.9, yb - h * 0.6),
           (x - w * 0.45, yb - h * 0.82), (x - w * 0.15, yb - h), (x + w * 0.2, yb - h * 0.9),
           (x + w * 0.5, yb - h * 0.8), (x + w * 0.85, yb - h * 0.62), (x + w * 0.6, yb - h * 0.55),
           (x + w * 1.05, yb)]
    c.poly(pts, OCC)
    c.line(x - w * 0.9, yb - h * 0.6, x - w * 0.45, yb - h * 0.82, V[4])
    for _ in range(4):
        wy = yb - h * (0.15 + rng.random() * 0.55)
        c.set(x + (rng.random() - 0.5) * w, wy, DK[2])
    if lit:
        black_sun(c, x - w * 0.15, yb - h - h * 0.08, max(2.0, h * 0.045))


# --------------------------------------------------------------------------- the ward
def ward(c, cx, cy, rx, ry, A=None, ground=None, pool=True):
    """The player's warded circle, as on the title (splash.vigil): the light ramp burning in
    rings and a hexagram, pigment gold where it lands on the ground."""
    A = A or LT
    GR = ground or G
    if pool:
        c.radial(cx, cy, rx * 1.4, ry * 1.5, [GR[2], GR[2], GR[3], GR[3], GR[4], GR[4], None])
    c.ring(cx, cy, rx, ry, A[3], 2)
    c.ring(cx, cy, rx * 0.96, ry * 0.95, A[2], 2)
    c.ring(cx, cy, rx * 0.84, ry * 0.8, A[3], 1)
    c.ring(cx, cy, rx * 0.63, ry * 0.6, A[3], 1)
    star = [(cx + math.cos(i * 1.0472 - 1.5708) * rx * 0.84,
             cy + math.sin(i * 1.0472 - 1.5708) * ry * 0.8) for i in range(6)]
    for i in range(6):
        c.line(star[i][0], star[i][1], star[(i + 2) % 6][0], star[(i + 2) % 6][1], A[2])
    for i in range(12):
        a = i * 6.28318 / 12.0 + 0.22
        x0 = cx + math.cos(a) * rx * 0.91
        y0 = cy + math.sin(a) * ry * 0.88
        c.vline(x0, y0 - 2, y0 + 2, A[1])
        c.set(x0, y0, A[0])
    return star


# =========================================================================== panels
def p_moot():
    """1. The circle where the wizards met, before. Every wizard's light is still in the world:
    a fire in the middle of a warded ring, and round it their staves, each a different element.
    The last time the screen holds more than one warm light."""
    back, mid, fore = frame(True), frame(), frame()
    sky(back, 0, 400, [OCC, S[4], V[4], V[3], V[2]])
    stars(back, (0, 0, FW, 330), 520, 3)
    hills(back, ridge_fn(366, 16, 11), [V[3], V[4], S[4]], 40, lit=V[2])
    hills(back, ridge_fn(392, 9, 23, 1.6), [S[4], V[4]], 30, lit=V[3])
    back.vramp(398, FH - 1, [V[4], S[4], S[4], OCC])
    fx, fy = 480, 478
    # A band of thicker stars across the sky. (Thin cloud was tried here and dithered into ruled
    # horizontal stripes across the phone's sky; the heath has no cloud.)
    rs = random.Random(12)
    for _ in range(700):
        t = rs.random()
        x = t * FW
        y = 40 + t * 230 + (rs.random() - 0.5) * 90 * (0.4 + rs.random())
        back.set(x, y, pick(rs, [S[2], S[1], V[2], K[2]]))
    sk.moon(back, 372, 286, 13)

    # the worn way in, from the camera to the gap in the ring: filled row by row and broken up
    # by dither, so it reads as trodden ground and not as a ladder
    def way(y):
        t = (840 - y) / float(840 - fy - 52)
        return fx + 10 + math.sin(t * 2.6) * 26 * (1 - t), 3 + (1 - t) * 30
    for y in range(fy + 52, FH):
        wx, wd = way(y)
        for x in range(int(wx - wd), int(wx + wd) + 1):
            e = 1.0 - abs(x - wx) / wd
            if dith(x, y, min(1.0, e * 2.2)):
                back.set(x, y, S[3] if dith(x * 3 + 1, y * 5, 0.32 * e) else S[4])
    heath(back, 400, 840, 2600, 5, [(S[4], V[3]), (V[4], V[3]), (OCC, V[4]), (S[4], LI[4])],
          keep_out=lambda x, y: y > fy + 52 and abs(x - way(y)[0]) < way(y)[1] * 0.8)
    back.radial(fx, fy + 4, 190, 52, [G[3], G[3], G[4], G[4], G[4], None])
    ward(mid, fx, fy, 64, 17, pool=False)
    # The ring of stones and the wizards inside it, sorted back to front together so the near
    # stones stand in front of the far wizards. No stone at the front centre: that is where the
    # way in comes through, and a stone there stood in front of the fire.
    items = []
    rng = random.Random(41)
    for i in range(14):
        a = i * 6.28318 / 14.0 + 0.11
        if math.sin(a) > 0.86:
            continue
        x = fx + math.cos(a) * 232
        y = fy - 4 + math.sin(a) * 60
        depth = (y - (fy - 64)) / 128.0
        items.append((y, "stone", x, depth, 900 + i))
    # Five of the game's wizards round the fire, each staff burning his own element. None stands at the
    # front centre, where he would cover the fire; the one behind it holds his light up over it.
    for i, (a, who) in enumerate([(0.85, PYRO), (2.2, FROST), (3.25, STORM), (4.71, DEE), (5.55, GEO)]):
        x = fx + math.cos(a) * 128
        y = fy + math.sin(a) * 32
        items.append((y, "wizard", x, who, 0))
    items.sort(key=lambda it: it[0])
    for (y, kind, x, d, seed) in items:
        if kind == "stone":
            h = 34 + d * 46 + rng.random() * 16
            w = 10 + d * 12 + rng.random() * 5
            far = y < fy
            standing_stone(mid, x, y, w, h, seed, lean=(x - fx) / 2400.0 + (rng.random() - 0.5) * 0.08,
                           warm=(G, 0.85 - d * 0.5) if far else None,
                           ramp=S if far else [S[3], S[4], S[4], OCC, OCC],
                           toward=None if far else (1 if x < fx else -1, -0.6, G[3]))
        else:
            who = d
            hgt = 42 + (y - fy + 30) * 0.28
            facing_fire = (fx - x, fy - y)
            n = math.hypot(*facing_fire) or 1
            roster_figure(mid, who, x, y, hgt, [ROBE[who[0]], OCC], facing=1 if x <= fx else -1,
                          warm=G, toward=(facing_fire[0] / n, facing_fire[1] / n))
    sk.flame(mid, fx, fy + 1, 30, LT, seed=17, halo=G)
    sk.flame(mid, fx - 7, fy + 3, 19, LT, seed=29, halo=G)
    sk.flame(mid, fx + 8, fy + 2, 22, LT, seed=31, halo=G)
    rm = random.Random(77)
    for _ in range(40):                                  # sparks going up off the fire
        mid.set(fx + (rm.random() - 0.5) * 30, fy - 30 - rm.random() ** 1.6 * 90,
                pick(rm, [LT[1], LT[2], LT[3], G[1]]))
    # The near ground: heather in occlusion, boulders along the way in, and a stone of the ring
    # right at the camera on either side.
    heath(fore, 590, 840, 700, 9, [(OCC, S[4]), (OCC, V[4])], scale=1.6,
          keep_out=lambda x, y: abs(x - fx - 10) < 20 + (y - 590) * 0.16)
    for (bx, by, bw, bh, sd) in [(392, 668, 40, 16, 1), (586, 700, 54, 20, 2), (350, 770, 70, 26, 3),
                                 (630, 800, 84, 30, 4), (430, 610, 22, 9, 5)]:
        sk.boulder(fore, bx, by, bw, bh, seed=sd, ramp=[S[1], S[1], S[2], S[2], S[3]])
    standing_stone(fore, 214, 650, 70, 210, 61, lean=0.04, ramp=[S[3], S[4], S[4], OCC, OCC],
                   toward=(1, -0.5, G[4]))
    standing_stone(fore, 770, 640, 56, 150, 63, lean=-0.05, ramp=[S[3], S[4], S[4], OCC, OCC],
                   toward=(-1, -0.4, G[4]))
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.9)
    return layers


def p_throne():
    """2. "He came here first." The same circle, under him. His first full look: the only
    figure in the game that sheds dark light. The wizards' light leaves them and goes into his
    hand; the stones go down outward from where he stands."""
    back, mid, fore = frame(True), frame(), frame()
    sky(back, 0, 420, [OCC, OCC, S[4], V[4], V[3]])
    stars(back, (0, 0, FW, 300), 160, 4, colors=[S[0], V[1], V[0]])
    sun_x, sun_y = 482, 330
    black_sun(back, sun_x, sun_y, 50, rays=40, seed=5)
    hills(back, ridge_fn(388, 12, 11), [V[4], S[4], OCC], 40, lit=V[3])
    back.vramp(400, FH - 1, [V[4], S[4], OCC, OCC])
    heath(back, 402, 840, 2200, 6, [(S[4], V[4]), (OCC, V[4]), (OCC, S[4])])
    back.radial(480, 600, 260, 66, [V[3], V[3], V[4], V[4], None])
    # stones of the ring, thrown down outward from him
    for i, (x, y, w, h, lean, br) in enumerate([(262, 470, 20, 54, -0.35, True), (338, 452, 16, 40, -0.2, False),
                                                (622, 450, 16, 42, 0.22, True), (702, 468, 21, 56, 0.4, False),
                                                (180, 520, 30, 80, -0.55, True), (790, 520, 30, 76, 0.5, True)]):
        standing_stone(back, x, y, w, h, 700 + i, lean=lean, broken=br, ramp=[S[3], S[4], S[4], OCC, OCC],
                       toward=(-0.6 if x > 480 else 0.6, -0.8, DK[3]))
    hx, hy, u = dark_wizard(mid, 480, 584, 290)
    black_sun(mid, hx, hy, 1.7 * u)
    # the wizards at his feet, their light going out of them and up into his hand
    # Going down: the second frame of each one's own fall (his death strip), facing him, either side
    # of his hem - over it, a figure in near-black was lost in his - and above row 600, where the PC's
    # view ends, clear of the caption's columns. Back to front.
    victims = [(322, 544, GEO), (646, 548, STORM), (380, 568, PYRO), (606, 588, DEE), (350, 592, FROST)]
    for i, (x, y, who) in enumerate(victims):
        hgt = 50 + (y - 590) * 0.2
        face = 1 if x < 480 else -1
        # The streams from his right pass BEHIND him to the raised hand: in front they ran across
        # his face.
        motes(fore if x < 480 else back, light_at(who, x, y, hgt, "death", 1, face), (hx, hy), 150, 400 + i,
              [E[who[2]]], spread=18, bow=40 + i * 6)
    for i, (x, y, who) in enumerate(victims):
        roster_figure(fore, who, x, y, 50 + (y - 590) * 0.2, [OCC, S[4]], strip="death", frame=1,
                      facing=1 if x < 480 else -1, dim=True, toward=(0, -1, DK[3]))
    # shadow running out of his hem across the ground: what he leaves becomes what serves him
    rng = random.Random(91)
    for i in range(26):
        a = math.pi * (0.04 + 0.92 * rng.random())
        ln = 70 + rng.random() * 230
        x0, y0 = 480 + math.cos(a) * 60, 584
        pts = []
        for k in range(24):
            t = k / 23.0
            pts.append((x0 + math.cos(a) * ln * t * 1.6 * (1 if a < 1.57 else 1),
                        y0 + math.sin(a) * ln * t * 0.34 + math.sin(t * 7 + i) * 3))
        for k in range(len(pts) - 1):
            wd = 2.4 * (1 - k / 23.0) + 0.6
            sk._stroke(mid, pts[k][0], pts[k][1], pts[k + 1][0], pts[k + 1][1], wd, wd * 0.8, OCC)
        if rng.random() < 0.55:
            ex, ey = pts[-1]
            mid.set(ex, ey - 2, EYE[0])
            mid.set(ex + 2, ey - 2, EYE[1])
    heath(fore, 640, 840, 700, 19, [(OCC, S[4]), (OCC, OCC)], scale=1.8)
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.8, cy=CY - 10)
    return layers


def p_tide():
    """3. His shadows over the land. A village at the foot of a hill, still lit; over the crest
    behind it, from the tower on the horizon, a tide of the things he raised comes down the
    slope toward it. Up close they are the chapters' kinds - cowl, horns, spear, shield, the
    caster's loose orb - and every one of them wears his mark in its eyes. Massed, they are
    a dark flood with a thousand of those eyes in it."""
    back, mid, fore = frame(True), frame(), frame()
    sky(back, 0, 400, [OCC, S[4], V[4], V[3], V[2], V[2]])
    stars(back, (0, 0, FW, 300), 300, 8, colors=[K[1], K[2], V[0]])
    tx, ty = 636, 356
    hills(back, ridge_fn(352, 7, 31, 0.8), [V[3], V[4], S[4]], 24, lit=V[2])
    tower(back, tx, ty, 72, seed=3)

    def crest(x):
        return 456 - (x - 160) * 0.12 + math.sin(x * 0.03) * 4 + math.sin(x * 0.011 + 1) * 8

    hills(back, crest, [V[2], V[3], V[3], V[4], S[4]], 150, lit=V[1])
    heath(back, 380, 600, 1600, 7, [(V[4], V[3]), (S[4], V[3])],
          keep_out=lambda x, y: y < crest(x) + 3)
    back.vramp(560, FH - 1, [S[4], S[4], OCC, OCC], x0=0)
    heath(back, 560, 840, 1400, 9, [(OCC, S[4]), (OCC, V[4])], scale=1.4)
    rng = random.Random(55)
    # the village at the hill's foot, lit; the houses nearest the tide already dark
    houses = []
    for k in range(16):
        hx = 300 + rng.random() * 190
        hy = 528 + rng.random() * 34 - (hx - 300) * 0.04
        houses.append((hy, hx))
    houses.sort()
    for (hy, hx) in houses:
        wd = 9 + rng.random() * 7
        ht = 6 + rng.random() * 3
        back.rect(hx - wd / 2, hy - ht, hx + wd / 2, hy, S[3])
        back.rect(hx + wd * 0.1, hy - ht, hx + wd / 2, hy, S[4])
        back.poly([(hx - wd / 2 - 2, hy - ht), (hx + wd / 2 + 2, hy - ht), (hx + 1, hy - ht - 7)], OCC)
        back.line(hx - wd / 2 - 2, hy - ht, hx + 1, hy - ht - 7, V[4])
        back.hline(hx - wd / 2, hx + wd / 2, hy + 1, OCC)
        if hx < 440 and rng.random() < 0.85:
            wx = hx - wd * 0.2
            back.radial(wx, hy - ht * 0.5, 7, 5, [G[3], G[4], None])
            back.set(wx, hy - ht * 0.5, G[1])
            back.set(wx + 1, hy - ht * 0.5, G[2])
    for _ in range(26):                                 # trees among the houses
        x = 280 + rng.random() * 230
        y = 520 + rng.random() * 50
        sk._clump(back, x, y, 6, 5, 22, rng, OCC, S[4])

    # THE TIDE: everything right of a leading edge that runs from the crest down to the village.
    def front_x(y):
        return 548 - (y - 380) * 0.55 + math.sin(y * 0.05) * 8

    kinds = ["swarmer", "swarmer", "swarmer", "runner", "bruiser", "shielder", "caster", "knight", "hound"]
    bodies = []
    for _ in range(1500):
        y = 372 + rng.random() ** 1.1 * 228
        x = front_x(y) + rng.random() ** 0.8 * (FW + 40 - front_x(y))
        if y < crest(x) - 4 and rng.random() < 0.85:
            continue
        bodies.append((y, x))
    for _ in range(90):                                 # the broken front, a few strides out
        y = 400 + rng.random() * 190
        bodies.append((y, front_x(y) - rng.random() * 26))
    bodies.sort()
    for (y, x) in bodies:
        hgt = 7 + (y - 372) * 0.09 + rng.random() * 3
        creature(mid, pick(rng, kinds), x, y, hgt, facing=-1, body=[OCC, S[4]], seed=ri(rng, 0, 99999))
    # the nearest, at the camera: a horned one, cut by the frame, looking where they are going
    # The front rank, nearest the village: a few of them big enough to name, in profile and rimmed
    # by the key. One giant figure in the foreground was tried twice - from behind a horned one is
    # two round shapes with no face, and cut by the frame a knight is a pole - and neither read.
    for i, (kind, dy, hgt) in enumerate([("hound", 0, 30), ("runner", 14, 44), ("swarmer", 34, 40),
                                         ("bruiser", 6, 58), ("caster", 40, 54), ("knight", 22, 62),
                                         ("shielder", 48, 50)]):
        y = 548 + dy
        x = front_x(y) + 8 + i * 30
        # Rimmed in stone hi, as the locked wizards' silhouettes are (.ai/side-events.md): against
        # the dark mass behind them the full soft rim drew an outline round each.
        creature(fore, kind, x, y, hgt, facing=-1, body=[OCC, S[4]], rim=S[0], seed=80 + i)
    heath(fore, 640, 840, 500, 23, [(OCC, OCC), (OCC, S[4])], scale=1.8)
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.75)
    return layers


def p_taken():
    """4. "He took them all." The game's wizards led away along a ridge against his light, bound
    wrist to wrist by his chain, their staves dark, their light drawn out of them toward the tower.
    Silhouettes only, of every wizard in the game, the three the player frees included (Eric)."""
    back, mid, fore = frame(True), frame(), frame()
    sky(back, 0, 560, [OCC, S[4], V[4], V[3], V[3], V[2]])
    stars(back, (0, 0, FW, 300), 140, 13, colors=[K[2], V[0]])
    sx_, sy_ = 628, 318
    back.radial(sx_, sy_ + 120, 360, 170, [V[2], V[2], V[3], None])
    black_sun(back, sx_, sy_, 22, rays=26, seed=8)
    hills(back, ridge_fn(452, 10, 41, 0.9), [V[2], V[3], V[3], V[4]], 70, lit=V[1])
    tower(back, sx_ + 6, 446, 104, seed=4, lit=False)

    # the ridge they walk, rising left to right toward him; they stand on the skyline
    def ridge(x):
        return 572 - (x - 160) * 0.09 + math.sin(x * 0.02) * 5 + math.sin(x * 0.071) * 2

    for x in range(FW):
        top = int(ridge(x))
        for y in range(top, FH):
            mid.set(x, y, OCC if (y - top) > 3 or dith(x, y, 0.7) else S[4])
        if ridge(x + 1) < ridge(x - 1):
            mid.set(x, top, V[4])
    order = [("knight", 0), ("wiz", 0), ("wiz", 1), ("bruiser", 0), ("wiz", 2), ("wiz", 3), ("shielder", 0),
             ("wiz", 4), ("wiz", 5), ("caster", 0), ("wiz", 6), ("runner", 0)]
    # Seven of the game's wizards, walking (each a different frame of his own walk), staves dark,
    # Tituba, Vainamoinen and Laelaps among the first four. Twelve figures where there were ten, so they
    # walk a little closer together; and the wizards walk near the head of the line, by the tower,
    # because the phone's canvas crops the panel's sides and shows the head of it, not the tail.
    taken = [(TITUBA, 2), (FROST, 1), (VAINAMOINEN, 6), (LAELAPS, 4), (DEE, 5), (PYRO, 3), (STORM, 7)]
    x = 600
    rng = random.Random(17)
    wrists = []
    for i, (kind, k) in enumerate(order):
        yb = ridge(x) + 1
        if kind == "wiz":
            hgt = 88 - k * 3
            who, step = taken[k]
            motes(back, light_at(who, x, yb, hgt, "moving", step), (sx_, sy_), 170, 640 + k, [E[who[2]]],
                  spread=12, bow=40 + k * 20)
            # rimmed from his light, up and to the right, so the robe reads against the hills and
            # not only the hat against the sky
            _light, front = roster_figure(mid, who, x, yb, hgt, [OCC, S[4]], strip="moving", frame=step,
                                          dim=True, toward=(0.7, -0.7, DK[3]))
            wrists.append(front or (x + 1, yb - hgt * 0.42))
        else:
            hgt = {"bruiser": 74, "knight": 86, "shielder": 66, "caster": 78, "swarmer": 56, "runner": 58}[kind]
            creature(mid, kind, x, yb, hgt, facing=1, body=[OCC, S[4]], rim=DK[3], seed=700 + i)
        x -= 44 + rng.random() * 10
    # the chain that binds them, one line of his light from wrist to wrist
    for a, b in zip(wrists, wrists[1:]):
        for k in range(48):
            t = k / 47.0
            mid.set(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t + math.sin(t * math.pi) * 10,
                    DK[2] if k % 3 else DK[1])
    heath(fore, 640, 840, 500, 29, [(OCC, OCC), (OCC, S[4])], scale=1.7)
    for i, (x0, h) in enumerate([(150, 120), (874, 140)]):
        sk.tree(fore, x0, 700, h * 2.0, h * 1.4, seed=40 + i, dark=OCC, light=OCC, vines=False)
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.75)
    return layers


# THE ONE ANIMATED PANEL (Eric, 2026-11-01: "'You are the one he missed' should show an animation of
# creatures looking for the wizard, but he's cleverly hiding."). Its moving part is a strip of frames
# over one box of the mid layer, swapped on a clock (Prologue.cs, "<name>-anim"): the title's way
# (.ai/title-screen.md, "Animate by frame, never by tint"), so every pixel on screen is still one
# frame's, opaque, in contract colours. Each frame is the WHOLE mid layer inside the box - the
# searchers and everything that stands in front of them - so a frame laid over the still mid layer
# shows exactly the scene at that moment; outside the box nothing moves.
#
# Every edge of the box is a multiple of 8, and so is its width and height: the dissolve's Bayer
# threshold is taken from the atlas's own pixel, and on that grid it matches the threshold of the
# same frame pixel in the mid layer, so the box dissolves in and out with the panel, pixel for pixel.
MISSED_BOX = (296, 400, 664, 512)
MISSED_FPS = 8
# The panel's hold in Prologue.cs (Beats, "missed"); the frames cover it, the last held after.
MISSED_SECONDS = 5.4
MISSED_COLUMNS = 4


def sniffing_hound(c, x, yb, h, facing, head, stride, twitch=0.0, rim=None):
    """His hound, the tide's (creature "hound") posed to search: head is -1 with its nose to the
    ground, 0 level, +1 lifted to the air; stride swings the legs; twitch nudges the nose."""
    u = h / 20.0
    s = Cv(int(34 * u) + 8, int(28 * u) + 8)
    ox, oy = s.w // 2, int(25 * u) + 3
    f = facing

    def Q(px_, py_):
        return (ox + f * px_ * u, oy + py_ * u)

    def limb(a, b, a2, b2, r0, r1):
        sk._stroke(s, ox + f * a * u, oy + b * u, ox + f * a2 * u, oy + b2 * u, max(0.5, r0 * u),
                   max(0.5, r1 * u), OCC)
    limb(-5.6, -6.4, -6.6 - stride * 0.5, 0, 1.0, 0.7)
    limb(-3.4, -6.4, -3.0 + stride * 0.5, 0, 1.0, 0.7)
    limb(3.6, -7.0, 2.6 + stride * 0.5, 0, 1.0, 0.7)
    limb(5.4, -7.0, 6.4 - stride * 0.5, 0, 1.0, 0.7)
    s.poly([Q(*q) for q in [(-7.4, -6.0), (-5.6, -9.4), (3.6, -10.2), (6.6, -11.4), (7.6, -9.6), (6.6, -6.6),
                            (-5.6, -5.0)]], OCC)
    # The tail hangs while it works the ground and comes up when it has something.
    ta = 0.5 if head < 0 else (-0.3 if head > 0 else 0.0)
    tail = [(-7.4, -6.0)] + [(-7.4 + rx, -6.0 + ry) for (rx, ry) in
                             [rot(-4.2, -3.6, ta), rot(0.2, -2.0, ta)]]
    s.poly([Q(*q) for q in tail], OCC)
    # The head turns about the top of the neck: down to the ground, up to the air.
    pivot = (6.8, -10.4)
    ang = 0.95 if head < 0 else (-0.4 if head > 0 else 0.0)
    drop = 2.6 if head < 0 else 0.0

    def Hd(px_, py_):
        rx, ry = rot(px_ - pivot[0], py_ - pivot[1], ang)
        return Q(pivot[0] + rx + twitch, pivot[1] + ry + drop)
    limb(5.6, -9.8, pivot[0] + 0.4, pivot[1] + drop * 0.7, 1.6, 1.3)
    s.poly([Hd(*q) for q in [(6.4, -12.0), (10.6, -11.6), (11.2, -10.2), (8.2, -9.2), (6.2, -9.6)]], OCC)
    s.poly([Hd(*q) for q in [(6.8, -12.0), (7.8, -14.4), (8.2, -11.6)]], OCC)
    if rim:
        edge_light(s, rim, (-0.7, -0.7))
    ex, ey = Hd(8.9, -11.0)
    s.set(ex, ey, EYE[0])
    s.set(ex + f, ey, EYE[1])
    over(c, s, x - ox, yb - oy)


def rot(x, y, a):
    """(x, y) turned by a radians, clockwise on screen."""
    return (x * math.cos(a) - y * math.sin(a), x * math.sin(a) + y * math.cos(a))


def _hound_at(t):
    """The hound's one walk past him, as (x, head, stride, twitch). It trots in from the right,
    nose down, stops at his shoulder, lifts its head and works the air beside him - and goes on."""
    walk = lambda d: math.sin(d * 2 * math.pi / 13.0) * 2.4
    stop = 556.0
    if t < 1.3:
        x = 630 - (630 - stop) / 1.3 * t
        return x, -1, walk(630 - x), 0.0
    if t < 1.55:
        return stop, 0, 0.0, 0.0
    if t < 2.95:
        # Working the air: the nose twitches every other frame; he does not move.
        return stop, 1, 0.0, (0.35 if int(t * MISSED_FPS) % 2 else -0.15)
    if t < 3.15:
        return stop, 0, 0.0, 0.0
    x = stop - 80 * (t - 3.15)
    return x, -1, walk(stop - x), 0.0


def _shade_down(steps, keep=(), more=None):
    """Every pigment colour mapped `steps` stops down its own row (past the deep end, occlusion),
    the rims to bone, and the colours in `keep` left alone - for a figure standing in the dark.
    more: {row: extra stops} for a row that would still shine (his staff's gold)."""
    held = set(rgb(c) for c in keep)
    m = {}
    for name, ramp in sorted(M.items()):
        n = steps + (more or {}).get(name, 0)
        for i, col in enumerate(ramp):
            key = rgb(col)
            if key in m or key in held:
                continue
            m[key] = rgb(ramp[i + n]) if i + n < len(ramp) else rgb(OCC)
    m[rgb(bl.RIM)] = rgb(K[2])
    m[rgb(RIM_SOFT)] = rgb(K[3])
    return m


def _facing_at(t, flips, first):
    f = first
    for at in flips:
        if t >= at:
            f = -f
    return f


def _ember_at(t):
    """His light, banked. He lets it go out as the hound comes, and does not breathe on it again
    until the hound has gone past."""
    if t < 1.0 or t >= 3.7:
        return 1.0
    if t < 1.35 or t >= 3.3:
        return 0.5
    return 0.0


def p_missed():
    """5. "You are the one he missed." The hiding (Eric, 2026-11-01). The broken ring at night, its
    near stones in a row across the heath, and in the row, between two of them, the player - the
    title's own figure, facing us, still as a stone, his light put out to an ember. From where they
    stand he is one more dark standing shape with a point on top; from where we stand he is
    plainly the wizard. Beyond the row his creatures search: a hound trots along it nose down,
    stops at his shoulder, lifts its head to the air beside him - he lets his ember go out - and
    goes on; a caster in the gap watches it; two small ones turn their heads; and out in the dark,
    pair by pair, the eyes turn away and back, a sweep of looks across the heath. They never find
    him. No new words: the line is the premise's."""
    back, mid, fore = frame(True), frame(), frame()
    sky(back, 0, 420, [OCC, OCC, S[4], V[4], V[3]])
    stars(back, (0, 0, FW, 340), 260, 21)
    tower(back, 694, 396, 34, seed=6)
    hills(back, ridge_fn(392, 12, 51), [V[4], S[4], OCC], 40, lit=V[3])
    back.vramp(398, FH - 1, [S[4], S[4], OCC, OCC])
    heath(back, 398, 840, 1800, 31, [(OCC, S[4]), (OCC, V[4])])
    cx, base = 482, 538
    # The ring's ground under the moon: cold, so the shapes on it read against it.
    back.radial(cx, base - 30, 330, 86, [S[3], S[3], S[4], S[4], None])
    # The far side of the ring, small, over the heath.
    for i, (x, y, w, h, lean, br) in enumerate([(232, 430, 18, 46, -0.4, True), (316, 418, 14, 34, 0.1, False),
                                                (646, 418, 15, 38, -0.3, True), (734, 436, 19, 48, 0.45, False)]):
        standing_stone(back, x, y, w, h, 800 + i, lean=lean, broken=br, ramp=[S[2], S[3], S[4], OCC, OCC])
    # Eyes out in the dark on either side, where the PC sees past the box: still ones.
    rng = random.Random(83)
    for _ in range(10):
        x = 170 + rng.random() * 120 if rng.random() < 0.5 else 672 + rng.random() * 120
        y = 402 + rng.random() * 30
        gap = 3 + ri(rng, 0, 2)
        for (gx, col) in ((0, EYE[0]), (1, EYE[1]), (gap - 1, EYE[1]), (gap, EYE[0])):
            back.set(x + gx, y, col)

    # THE ROW: two stones of the ring either side of him, and two more out at the PC's edges, and
    # him in it as one of them. Drawn once per state of his ember; the frames lay it over the
    # searchers, and the mid layer carries it whole.
    def row(ember):
        c = Cv(FW, FH)
        for (x, yb, w, h, sd, lean, br) in [(232, 548, 58, 132, 911, 0.05, True), (366, 540, 50, 112, 913, -0.03, False),
                                            (598, 544, 54, 120, 917, 0.04, False), (734, 552, 62, 140, 919, -0.05, True)]:
            standing_stone(c, x, yb, w, h, sd, lean=lean, broken=br, ramp=[S[2], S[3], S[4], OCC, OCC],
                           toward=(-1, -0.6, S[1]))
        # He stands in the dark: the title's figure a stop down every pigment row, the key's rim
        # softened to bone - plainly him to us, one more dark upright shape to them. Only the ember,
        # which is light and not pigment, keeps its own colours.
        him = Cv(FW, FH)
        hero.wizard_hero(him, cx, base, h=112, staff_ramp=LT, under_ramp=None, snuffed=ember)
        down = _shade_down(1, keep=LT, more={"gold": 1})
        hp = him.px
        for y in range(int(base - 140), int(base + 30)):
            for x in range(int(cx - 70), int(cx + 90)):
                q = hp[x, y]
                if q[3] and q in down:
                    hp[x, y] = down[q]
        over(c, him)
        return c
    rows = dict((e, row(e)) for e in (1.0, 0.5, 0.0))
    over(mid, rows[1.0])

    # The eyes in the dark beyond the row, inside the box: each pair a head that turns away (its
    # eyes go) and back, the turning passing across the heath from one side to the other.
    re = random.Random(97)
    eyes = []
    for _ in range(40):
        if len(eyes) >= 11:
            break
        x = 306 + re.random() * 346
        y = 404 + re.random() * 16
        if abs(x - cx) < 40 or any(abs(x - a) < 22 and abs(y - b) < 8 for (a, b, _g) in eyes):
            continue
        eyes.append((x, y, 3 + ri(re, 0, 2)))

    def searchers(c, t):
        for (x, y, gap) in eyes:
            ph = (t * 0.9 - (x - 300) / 420.0) % 1.4
            if ph < 0.22:
                continue                                  # turned away
            look = -1 if ph < 0.8 else 1
            for (gx, col) in ((0, EYE[0]), (1, EYE[1]), (gap - 1, EYE[1]), (gap, EYE[0])):
                c.set(x + gx + look, y, col)
        # In scale with him, by how far behind the row each one stands (the ground runs from the
        # horizon at 392 to his feet at 538): the two small ones far back, the caster in the gap, the
        # hound a few strides behind him.
        creature(c, "swarmer", 318, 458, 30, facing=_facing_at(t, [0.7, 2.4, 4.0], 1), body=[OCC, S[4]],
                 rim=S[2], seed=31)
        creature(c, "swarmer", 648, 452, 28, facing=_facing_at(t, [1.5, 3.0, 4.5], -1), body=[OCC, S[4]],
                 rim=S[2], seed=37)
        creature(c, "caster", 414, 502, 58, facing=_facing_at(t, [1.1, 3.3], -1), body=[OCC, S[4]],
                 rim=S[1], seed=41)
        hx, head, stride, twitch = _hound_at(t)
        sniffing_hound(c, hx, 510, 54, -1, head, stride, twitch, rim=S[0])

    t_of = frame_t(0.9, CY + 60)
    x0, y0, x1, y1 = MISSED_BOX
    frames = []
    ticks = int(math.ceil(MISSED_SECONDS * MISSED_FPS))
    for k in range(ticks):
        t = (k + 0.5) / MISSED_FPS
        c = Cv(FW, FH)
        searchers(c, t)
        over(c, rows[_ember_at(t)])
        darken(c, t_of, [S[4], OCC], box=MISSED_BOX)
        frames.append(c.img.crop((x0, y0, x1, y1)))

    heath(fore, 600, 840, 900, 37, [(OCC, OCC), (OCC, S[4])], scale=1.7)
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.9, cy=CY + 60)
    layers["anim"] = frames
    return layers


# --------------------------------------------------------------------------- the freeing
# THE FREEING (Eric, 2026-11-02: "We need to add cutscenes to help the player identify with the blue
# robed character. Include a cutscene where the blue robed wizard frees the four starting wizards.").
# Two panels between the hiding and the road: the Archmage - the one he missed, the playable starter -
# goes back for the four he could reach, the disciplines a fresh save plays, and breaks the dark
# wizard's chain off them (animated, BREAK_BOX); then the five of them stand together with their lights
# back. Then the road, and the character select, where all five are the player's to choose. See
# .ai/cutscenes.md.
#
# The four kneel where the hiding's row of stones stood and stand up in the same places in the next
# panel, so the dissolve between them is the one moment they rise. The order, left to right, is the
# same in both: Pyromancer, Geomancer, him, Frostweaver, Stormcaller. The fire is outermost: inside, the
# Pyromancer's lantern, held out in front of him, came back to light behind the Archmage's staff.
FREED_FOUR = [(PYRO, 342, 524, 88, 1), (GEO, 404, 536, 94, 1), (FROST, 556, 536, 94, -1), (STORM, 618, 524, 88, -1)]
# Where he stands in both: the title's figure, his feet above row 560 so the PC's caption band
# (rows ~558-578) never crosses them.
HIM_X, HIM_Y, HIM_H = 480, 548, 112

# The chain's breaking is the panel's one moving part: a box of its mid layer swapped on a clock, the
# hiding's way (write_anim). Every edge a multiple of 8, so the dissolve's Bayer threshold, taken from the
# atlas's own pixel, matches the mid layer's under it.
BREAK_BOX = (288, 384, 672, 576)
BREAK_FPS = 8
# The panel's hold in Prologue.cs (Beats, "break"); the frames cover it, the last held after.
BREAK_SECONDS = 5.4

# The pigment each element's light lands on (the lessons' halos): a pool under a wizard is the colour
# his light makes of the ground, never his element's own ramp.
HALO = {"fire": M["ember"], "ice": M["azure"], "lightning": G, "earth": M["amber"], "water": M["azure"],
        "grass": M["verdant"], "light": G, "metal": ST, "arcane": AR, "wind": LI, "darkness": V,
        # Paracelsus's dose (freed.py): a poison light lands on the ground as grass's does.
        "poison": M["verdant"]}


def _figure_box(who, strip, frame, cx, yb, h, facing):
    """Where roster_figure puts a wizard's drawn pixels, as (x0, y0, x1, y1) in frame pixels, without
    drawing him: what the chain is fastened to. A kneeling frame stands lower than his height says."""
    s = Cv(FW, FH)
    roster_figure(s, who, cx, yb, h, [OCC, OCC], strip=strip, frame=frame, facing=facing, lit=False)
    box = s.img.getbbox()
    return box or (cx - 10, yb - h, cx + 10, yb)


def _wrist(who, strip, frame, cx, yb, h, facing):
    """The point the chain holds him by: his front, a little over half way down what is drawn of him."""
    x0, y0, x1, y1 = _figure_box(who, strip, frame, cx, yb, h, facing)
    y = y0 + (y1 - y0) * 0.56
    return ((x1 - 4) if facing > 0 else (x0 + 4), y)


# --------------------------------------------------------------------------- the real sprites
# Eric, 2026-11-03, of the freeing's flat one-colour cut-outs: "Redraw with real sprites." In the break, the
# five and the freed reels a wizard is his own 48 cell, every pixel of it, at a whole x2 on the panel's grid
# (one sprite pixel is two panel pixels, four on screen): his real colours and his real light, the figure
# the character select shows. The flat silhouettes (roster_figure) stay where the story wants a shape and not
# yet a person - the moot, the fall, the procession.
SPRITE_K = 2


def _tone(steps):
    """A colour map that puts a sprite `steps` stops into the dark: every pigment that many stops down its own
    row (past the deep end, occlusion), his own light out (the element ramps to dull steel, then occlusion),
    the key's rims to stone. Empty for 0: the sprite as it is."""
    m = {}
    if steps <= 0:
        return m
    for name, ramp in sorted(M.items()):
        for i, col in enumerate(ramp):
            key = rgb(col)
            if key not in m:
                m[key] = rgb(ramp[i + steps]) if i + steps < len(ramp) else rgb(OCC)
    dull = rgb([ST[3], ST[4], OCC][min(2, steps - 1)])
    for name, ramp in sorted(E.items()):
        for col in ramp:
            m.setdefault(rgb(col), dull)
    for col in (bl.RIM, RIM_SOFT):
        m[rgb(col)] = rgb([S[2], S[3], S[4]][min(2, steps - 1)])
    return m


def sprite_figure(c, who, cx, yb, strip="idle", frame=0, facing=1, steps=0, rim=None, glow=True):
    """One of the game's wizards as he is in the game: his 48 cell at a whole SPRITE_K, turned by facing, his
    contact row on yb (where roster_figure puts it, so the two stand on the same ground).

    steps: how far into the dark (_tone) - 0 his real colours. rim=(dx, dy, colour): the sprite pixels on the
    edge facing a light lit in that colour, one SPRITE pixel deep (lit on the cell, before the scale, so the
    rim sits on his own grid and not on a finer one). glow: his own light's halo behind the anchor his element
    sits at (silhouette), only while he is in his colours.
    Returns (light, box): his light's centre, or None, and the drawn box (x0, y0, x1, y1), in frame pixels."""
    prefix, carried, burns = who
    k = SPRITE_K
    cell = _sprite_cell(prefix, strip, frame)
    if facing < 0:
        cell = cell.transpose(Image.FLIP_LEFT_RIGHT)
    s = Cv(CELL + 2, CELL + 2)
    s.img.paste(cell, (1, 1), cell)
    s.px = s.img.load()
    tone = _tone(steps)
    if tone:
        for y in range(s.h):
            for x in range(s.w):
                p = s.px[x, y]
                if p[3] and p in tone:
                    s.px[x, y] = tone[p]
    if rim:
        edge_light(s, rim[2], rim[:2])
    big = s.img.resize((s.w * k, s.h * k), Image.NEAREST)
    left = int(round(cx - (CELL / 2.0 + 1) * k))
    top = int(round(yb - CELL * k))
    _pixels, anchor = silhouette(prefix, strip, frame, carried)
    light = None
    if anchor:
        light = (cx + (anchor[0] - CELL / 2.0) * k * facing, yb + (anchor[1] - (CELL - 1)) * k)
        if glow and steps <= 0:
            ramp = E[burns]
            c.radial(light[0], light[1], 4.2 * k, 4.2 * k, [ramp[3], ramp[3], None])
    c.img.paste(big, (left, top), big)
    c.px = c.img.load()
    bx = big.getbbox() or (0, 0, big.size[0], big.size[1])
    return light, (left + bx[0], top + bx[1], left + bx[2], top + bx[3])


def sprite_wrist(box, facing):
    """The point a chain holds a sprite figure by: his front, a little over half way down what is drawn of him."""
    x0, y0, x1, y1 = box
    return ((x1 - 2 * SPRITE_K) if facing > 0 else (x0 + 2 * SPRITE_K), y0 + (y1 - y0) * 0.56)


def _chain_pts(a, b, n=40, sag=8.0):
    return [(a[0] + (b[0] - a[0]) * (k / float(n - 1)),
             a[1] + (b[1] - a[1]) * (k / float(n - 1)) + math.sin(k / float(n - 1) * math.pi) * sag)
            for k in range(n)]


def _chain(c, pts, colors=(DK[2], DK[1])):
    """One length of his chain: a line of his light, every third link brighter (the procession's), with
    its own shadow under it - one pixel was a thread at a phone's size."""
    for k, (x, y) in enumerate(pts):
        c.set(x, y + 1, DK[3])
    for k, (x, y) in enumerate(pts):
        c.set(x, y, colors[1] if k % 3 == 0 else colors[0])


def _shackle(c, x, y, col):
    c.ring(x, y, 2.2, 2.2, col, 1)
    c.set(x, y - 2, DK[0] if col == DK[1] else col)


# The breaking, in panel seconds: his light banked, then up; the length behind him goes first, then the
# outer two, then the shackles; then each of them takes his own light back, one after another.
BREAK_LIGHT_UP = 1.3
BREAK_AT = [2.0, 2.25, 2.25]          # the middle length, the left, the right
SHACKLES_AT = 2.5
RELIGHT_AT = [3.2, 3.5, 3.8, 4.1]     # FREED_FOUR's order


def _fragments(c, pts, burst, t, seed, ground):
    """A length of chain bursting at `burst` (x) at time t after it broke: each link flies away from the
    break and up, its light going out as it goes - the darkness ramp down to the violet it lit, then
    nothing - and one link in four comes down dead on the ground, in occlusion, and stays."""
    rng = random.Random(seed)
    for (x, y) in pts:
        side = 1.0 if x >= burst else -1.0
        near = max(0.0, 1.0 - abs(x - burst) / 90.0)
        vx = side * (20 + rng.random() * 50) * (0.4 + near)
        vy = -(30 + rng.random() * 60) * (0.3 + near)
        life = 0.45 + rng.random() * 0.5
        dead = rng.random() < 0.25
        px_ = x + vx * t
        py_ = y + vy * t + 140 * t * t
        if dead:
            gy = ground + rng.random() * 6
            if py_ >= gy:
                c.set(px_, gy, OCC)
                c.set(px_ + side, gy, S[4])
                continue
        if t > life:
            continue
        a = t / life
        col = DK[0] if a < 0.18 else (DK[1] if a < 0.4 else (DK[2] if a < 0.62 else (DK[3] if a < 0.82 else V[3])))
        c.set(px_, py_, col)
        if a < 0.4:
            c.set(px_ + side, py_, DK[2])


def _flare(c, x, y, t):
    """His light coming up through the panel, as an extra bloom over the staff's own: nothing while it is
    banked, a burst with rays as he breaks the chain, settling to a lantern's glow. The light ramp is
    his - the ward's colour - and the only warm light until theirs come back."""
    if t < BREAK_LIGHT_UP:
        return
    # Only the burst: once it has passed, the staff's own glow (hero.wizard_hero) is all the light there
    # is. A bloom left standing over it was a yellow ball twice the orb's size.
    peak = max(0.0, 1.0 - abs(t - (BREAK_AT[0] + 0.05)) / 0.75)
    if peak <= 0.0:
        return
    r = 6 + 20 * peak
    c.radial(x, y, r * 1.6, r * 1.6, [LT[3], LT[3], None])
    c.radial(x, y, r * 0.6, r * 0.6, [LT[1], LT[2], LT[3], None])
    if peak > 0.15:
        rng = random.Random(7)
        rays = 28
        for i in range(rays):
            a = i * 6.28318 / rays + rng.random() * 0.12
            ln = (40 + rng.random() * 70) * peak
            for d in range(int(r * 0.9), int(r * 0.9 + ln)):
                if (d + i) % 3 == 0:
                    continue
                c.set(x + math.cos(a) * d, y + math.sin(a) * d, LT[2] if d < r + ln * 0.4 else LT[3])


def _break_frame(t, him):
    """The whole mid layer inside BREAK_BOX at panel time t."""
    c = Cv(FW, FH)
    hx, hy = him["head"]
    lit = t >= BREAK_LIGHT_UP
    # The ground his light finds: gold pigment under him, growing as it comes up.
    if lit:
        k = min(1.0, (t - BREAK_LIGHT_UP) / 0.5)
        c.radial(HIM_X, HIM_Y + 2, 70 + 80 * k, 18 + 18 * k, [G[2], G[3], G[3], G[4], G[4], None])
    wrists = []
    broken = t >= BREAK_AT[0]
    for i, (who, x, y, h, f) in enumerate(FREED_FOUR):
        since = t - RELIGHT_AT[i]
        coming = -0.6 <= since < 0
        # His own sprite, kneeling (the second frame of his fall), three stops into the dark while the chain
        # holds him, and coming up into his own colours a stop at a time as his light returns (Eric:
        # "Redraw with real sprites").
        steps = 3 if since < 0 else (2 if since < 0.2 else (1 if since < 0.4 else 0))
        # Their only lit edge before the break is the dark wizard's light, his chain's, landing as the violet
        # it makes (the rule: his ramp is emission on him and the chain alone); once the chain breaks it is
        # the Archmage's gold, from his staff.
        if broken:
            toward = (HIM_X - x, (HIM_Y - 60) - (y - 50))
            n = math.hypot(*toward) or 1.0
            rim = (toward[0] / n, toward[1] / n, G[2])
        else:
            rim = (f, 0, V[2])
        light, box = sprite_figure(c, who, x, y, strip="death", frame=1, facing=f, steps=steps, rim=rim)
        # Their own light coming back to them, out of the sky it was drawn into, just before it lights.
        if light and (coming or since >= 0):
            ax, ay = light
            span = min(1.0, (since + 0.6) / 0.6)
            motes(c, (ax - f * 40, ay - 120), (ax, ay), int(70 * span) if coming else 18, 900 + i,
                  [E[who[2]]], spread=10, bow=-14)
        wrists.append(sprite_wrist(box, f))
    # The chain: three lengths, wrist to wrist, the middle one behind him.
    spans = [(wrists[0], wrists[1]), (wrists[1], wrists[2]), (wrists[2], wrists[3])]
    order = [1, 0, 2]
    for j, (a, b) in enumerate(spans):
        at = BREAK_AT[order.index(j)]
        pts = _chain_pts(a, b, n=int(math.hypot(b[0] - a[0], b[1] - a[1]) / 2.4), sag=6 if j != 1 else 12)
        if t < at:
            _chain(c, pts)
        else:
            mid_x = (a[0] + b[0]) / 2.0 if j != 1 else HIM_X - 14
            _fragments(c, pts, mid_x, t - at, 300 + j, max(a[1], b[1]) + 14)
            # Where it broke, the instant it broke: a spark of his light.
            if t - at < 0.2:
                c.radial(mid_x, (a[1] + b[1]) / 2.0 + 6, 7, 7, [LT[0], LT[1], LT[3], None])
    for k, (x, y) in enumerate(wrists):
        if t < SHACKLES_AT:
            _shackle(c, x, y, DK[1])
        else:
            _fragments(c, [(x - 2, y), (x, y - 2), (x + 2, y), (x, y + 2)], x, t - SHACKLES_AT, 400 + k, y + 22)
    over(c, him["lit"] if lit else him["banked"])
    _flare(c, hx, hy, t)
    return c


def p_break():
    """6. "You went back for them." The hiding's heath again, the tower on its horizon, and the four he
    could reach - the disciplines, each his own sprite (sprite_figure) put three stops into the dark - on
    their knees in a row, bound wrist to wrist by his chain, its violet their only lit edge, their staves
    dark. The Archmage, the one he missed, stands in the middle of them, the title's own figure, his light
    banked as it was in the hiding. It comes up (animated): his light finds the chain and it breaks, the
    length behind him first, then the others, link by link going out as it flies, and his gold is on their
    edges now; and one after another their own lights come back to them out of the sky they were drawn
    into, and each comes up into his own colours a stop at a time."""
    back, mid, fore = frame(True), frame(), frame()
    sky(back, 0, 420, [OCC, OCC, S[4], V[4], V[3]])
    stars(back, (0, 0, FW, 340), 240, 151)
    # His light over the tower, landing on the sky as violet: he is still there, and still far.
    back.radial(700, 360, 260, 120, [V[3], V[3], V[4], None])
    hills(back, ridge_fn(390, 10, 153, 0.9), [V[3], V[4], S[4]], 30, lit=V[2])
    tower(back, 700, 394, 44, seed=15)
    hills(back, ridge_fn(402, 9, 155, 1.4), [S[4], V[4], OCC], 36, lit=V[3])
    back.vramp(404, FH - 1, [S[4], S[4], OCC, OCC])
    heath(back, 404, 840, 1800, 157, [(OCC, S[4]), (OCC, V[4])])
    # Two of the ring's stones, far off: the moot is behind him, on the way he came.
    for i, (x, y, w, h, lean, br) in enumerate([(212, 432, 18, 44, -0.3, True), (760, 436, 16, 38, 0.35, False)]):
        standing_stone(back, x, y, w, h, 1500 + i, lean=lean, broken=br, ramp=[S[2], S[3], S[4], OCC, OCC])
    # The ground under the row, cold under the sky, as the hiding has it: the four are dark shapes in the
    # dark, and without it they were not there at all until his light came up behind them.
    back.radial(HIM_X, HIM_Y - 26, 300, 70, [S[3], S[3], S[4], S[4], None])

    # Him, twice: banked as he was in the hiding (a stop down every pigment, the ember in the orb), and
    # with his light up. The frames lay one or the other over the four.
    def draw_him(ember):
        c = Cv(FW, FH)
        hero.wizard_hero(c, HIM_X, HIM_Y, h=HIM_H, staff_ramp=LT, under_ramp=None, snuffed=ember)
        if ember is not None:
            down = _shade_down(1, keep=LT, more={"gold": 1})
            px = c.px
            for y in range(int(HIM_Y - 150), int(HIM_Y + 30)):
                for x in range(int(HIM_X - 80), int(HIM_X + 90)):
                    q = px[x, y]
                    if q[3] and q in down:
                        px[x, y] = down[q]
        return c
    u = HIM_H / 32.0
    sx = HIM_X - 11.6 * u
    w = max(1, int(round(1.3 * u)))
    him = {"banked": draw_him(0.6), "lit": draw_him(None),
           "head": ((int(sx) + int(sx + w)) * 0.5, HIM_Y - 37.0 * u)}

    t_of = frame_t(0.9, CY + 60)
    x0, y0, x1, y1 = BREAK_BOX
    frames = []
    for k in range(int(math.ceil(BREAK_SECONDS * BREAK_FPS))):
        t = (k + 0.5) / BREAK_FPS
        c = _break_frame(t, him)
        darken(c, t_of, [S[4], OCC], box=BREAK_BOX)
        frames.append(c.img.crop((x0, y0, x1, y1)))
    # The still mid layer holds nothing inside the box (everything there moves); the last frame stands
    # in for it in a preview.
    heath(fore, 600, 840, 900, 159, [(OCC, OCC), (OCC, S[4])], scale=1.7)
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.9, cy=CY + 60)
    layers["anim"] = frames
    return layers


def p_five():
    """7. The five of them. The same heath and the same places: the four stand up where they knelt, each
    each in their own sprite and colours, each staff burning its own element again - and in the middle, in his
    warded circle, the Archmage.
    His chain lies on the ground in front of them, dead. Five lights on the heath where there was one; no
    line - the next panel is the road, and the one after it the player choosing which of them walks it."""
    back, mid, fore = frame(True), frame(), frame()
    sky(back, 0, 420, [OCC, S[4], V[4], V[3], V[2]])
    stars(back, (0, 0, FW, 340), 300, 161)
    sk.moon(back, 392, 262, 9)
    back.radial(700, 360, 220, 100, [V[3], V[4], None])
    hills(back, ridge_fn(390, 10, 153, 0.9), [V[3], V[4], S[4]], 30, lit=V[2])
    tower(back, 700, 394, 44, seed=15)
    hills(back, ridge_fn(402, 9, 155, 1.4), [S[4], V[4], OCC], 36, lit=V[3])
    back.vramp(404, FH - 1, [S[4], S[4], OCC, OCC])
    heath(back, 404, 840, 1800, 157, [(OCC, S[4]), (OCC, V[4])])
    for i, (x, y, w, h, lean, br) in enumerate([(212, 432, 18, 44, -0.3, True), (760, 436, 16, 38, 0.35, False)]):
        standing_stone(back, x, y, w, h, 1500 + i, lean=lean, broken=br, ramp=[S[2], S[3], S[4], OCC, OCC])
    # Each light's pool on the ground, in the pigment it makes: gold under him, theirs under them.
    back.radial(HIM_X, HIM_Y + 4, 230, 56, [G[3], G[3], G[4], G[4], None])
    for (who, x, y, h, f) in FREED_FOUR:
        halo = HALO[who[2]]
        back.radial(x, y + 4, 38, 9, [halo[4], halo[4], None])
    # His ward under him, as on the title.
    ward(mid, HIM_X, HIM_Y + 2, 58, 15, pool=False)
    # The chain they wore, dead on the ground in front of them: links in occlusion, the light gone out of it.
    rng = random.Random(163)
    for _ in range(70):
        x = 330 + rng.random() * 300
        y = 560 + rng.random() * 18 + abs(x - HIM_X) * -0.02
        if abs(x - HIM_X) < 40 and y < 566:
            continue
        mid.set(x, y, OCC)
        mid.set(x + 1, y, S[4] if rng.random() < 0.6 else OCC)
        if rng.random() < 0.3:
            mid.set(x + 2, y + 1, OCC)
    # The four, standing, their lights back: each his own sprite in his own colours (Eric: "Redraw with real
    # sprites"), rimmed on the side that faces the Archmage's light. Back to front.
    for (who, x, y, h, f) in sorted(FREED_FOUR, key=lambda e: e[2]):
        toward = (HIM_X - x, -0.6 * abs(HIM_X - x))
        n = math.hypot(*toward) or 1.0
        sprite_figure(mid, who, x, y + 4, strip="idle", frame=0, facing=f, rim=(toward[0] / n, toward[1] / n, G[2]))
    hero.wizard_hero(mid, HIM_X, HIM_Y, h=HIM_H, staff_ramp=LT)
    heath(fore, 600, 840, 900, 159, [(OCC, OCC), (OCC, S[4])], scale=1.7)
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.9, cy=CY + 60)
    return layers


def p_road():
    """6. The road ahead. Seen from above and behind: the player small on a road that winds
    away across the heath toward everything he holds, the tower on the far horizon under his
    light. No line - the next screen is the player choosing who walks it."""
    back, mid, fore = frame(True), frame(), frame()
    sky(back, 0, 300, [OCC, S[4], V[4], V[3], V[2]])
    stars(back, (0, 0, FW, 250), 300, 33)
    sk.moon(back, 330, 262, 9)
    tx, ty = 596, 294
    hills(back, ridge_fn(286, 12, 61, 0.7), [V[3], V[4], S[4]], 26, lit=V[2])
    tower(back, tx, ty, 46, seed=9)
    hills(back, ridge_fn(306, 14, 63, 1.1), [S[4], V[4], S[4]], 30, lit=V[3])
    hills(back, ridge_fn(352, 22, 67, 1.4), [V[4], S[4], OCC], 40, lit=V[3])
    back.vramp(360, FH - 1, [S[4], S[4], OCC])
    heath(back, 350, 840, 2400, 41, [(OCC, S[4]), (OCC, V[4]), (S[4], V[3])])
    # far places on the way: a dark wood, a broken keep on a rise
    rng = random.Random(71)
    for _ in range(30):
        x = 220 + rng.random() * 140
        y = 330 + rng.random() * 26
        sk._clump(back, x, y, 9, 6, 30, rng, OCC, S[4])
    back.poly([(700, 352), (702, 318), (708, 316), (709, 300), (716, 302), (718, 324), (726, 326), (728, 352)], OCC)
    back.line(702, 318, 708, 316, V[3])
    # the road, an S from the bottom of the frame to the far hills
    road = [(500, 840), (488, 740), (452, 650), (500, 580), (560, 520), (520, 460), (480, 410), (520, 372),
            (560, 340), (590, 304)]
    pts = []
    for k in range(len(road) - 1):
        for s_ in range(20):
            t = s_ / 20.0
            p0 = road[max(0, k - 1)]
            p1, p2 = road[k], road[k + 1]
            p3 = road[min(len(road) - 1, k + 2)]
            tt = t * t
            ttt = tt * t
            pts.append((0.5 * (2 * p1[0] + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * tt
                               + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * ttt),
                        0.5 * (2 * p1[1] + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * tt
                               + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * ttt)))
    for (x, y) in pts:
        wd = 1.0 + max(0.0, (y - 300)) * 0.034
        back.disc(x, y, wd * 1.6, wd * 0.5, S[4])
    for (x, y) in pts:
        wd = 1.0 + max(0.0, (y - 300)) * 0.034
        back.disc(x - wd * 0.3, y, wd * 1.0, wd * 0.34, S[3])
    rr = random.Random(5)
    for (x, y) in pts[::2]:                            # ruts and stones in it
        wd = 1.0 + max(0.0, (y - 300)) * 0.034
        back.set(x + (rr.random() - 0.5) * wd * 2.4, y + (rr.random() - 0.5) * wd * 0.6,
                 S[4] if rr.random() < 0.7 else S[2])
    # waymarks along it, the heath's standing stones, small with distance
    for (k, side, h) in [(44, 1, 26), (84, -1, 16), (112, 1, 11), (140, -1, 8)]:
        wx, wy = pts[k]
        wd = 1.0 + max(0.0, (wy - 300)) * 0.034
        standing_stone(back, wx + side * wd * 2.6, wy, h * 0.4, h, 950 + k, lean=side * 0.04,
                       ramp=[S[2], S[3], S[4], OCC, OCC])
    # He stands ON the road: x from the spline at his row, not a number that drifted off it.
    py_ = 566
    px_ = min(pts, key=lambda p: abs(p[1] - py_))[0]
    back.radial(px_, py_ + 2, 80, 26, [G[3], G[3], G[4], G[4], None])
    player_back(mid, px_, py_, 50)
    heath(fore, 640, 840, 700, 43, [(OCC, OCC), (OCC, S[4])], scale=1.8)
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.8)
    return layers


PANELS = [("moot", p_moot), ("throne", p_throne), ("tide", p_tide), ("taken", p_taken),
          ("missed", p_missed), ("break", p_break), ("five", p_five), ("road", p_road)]

# The animated panels' boxes, clocks and atlas widths (write_anim): the hiding's searchers, and the chain
# breaking.
ANIMS = {"missed": (MISSED_BOX, MISSED_FPS, MISSED_COLUMNS), "break": (BREAK_BOX, BREAK_FPS, 4)}


# =========================================================================== output
def audit(layers, extra=()):
    ok = raster.allowed()
    for name, ramp in M.items():
        for i, t in enumerate(ramp):
            ok.setdefault(t.upper(), name + "." + bl.ROLES[i])
    bad = {}
    for img in [c.img for c in layers.values()] + list(extra):
        for (n, col) in img.getcolors(1 << 20) or []:
            if col[3] == 0:
                continue
            h = "#%02X%02X%02X" % col[:3]
            if h not in ok:
                bad[h] = bad.get(h, 0) + n
    return bad


def composite(layers):
    flat = layers["back"].img.copy()
    for n in ("mid", "fore"):
        flat.paste(layers[n].img, (0, 0), layers[n].img)
    return flat


def write_anim(name, frames, out=None):
    """The frames as one atlas, the panel's columns wide (ANIMS), and its manifest: where the box sits in
    the frame, its size, the clock, and the timeline - one atlas index per tick, so a frame that does not
    change from one tick to the next is stored once and held. Prologue.cs reads both."""
    out = out or OUT
    box, fps, columns = ANIMS[name]
    unique, timeline, seen = [], [], {}
    for img in frames:
        key = img.tobytes()
        if key not in seen:
            seen[key] = len(unique)
            unique.append(img)
        timeline.append(seen[key])
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    for v in (x0, y0, w, h):
        assert v % 8 == 0, "the anim box must sit on the 8-pixel Bayer grid"
    cols = min(columns, len(unique))
    rows = (len(unique) + cols - 1) // cols
    atlas = Image.new("RGBA", (cols * w, rows * h), (0, 0, 0, 0))
    for i, img in enumerate(unique):
        atlas.paste(img, ((i % cols) * w, (i // cols) * h))
    atlas.save(os.path.join(out, "%s-anim.png" % name))
    # Binary, so it is LF on Windows too (the repo's line endings), whichever Python writes it.
    with open(os.path.join(out, "%s-anim.json" % name), "wb") as fh:
        fh.write((json.dumps({"x": x0, "y": y0, "width": w, "height": h, "columns": cols, "fps": fps,
                              "timeline": timeline}, sort_keys=True) + "\n").encode("ascii"))
    return len(unique)


def preview_anim(name, layers, frames, ticks):
    """A few ticks of the animated panel, the PC's view, one above the other."""
    box = ANIMS[name][0]
    flat = composite(layers)
    shots = []
    for k in ticks:
        im = flat.copy()
        im.paste(frames[k], box[:2], frames[k])
        for n in ("fore",):
            im.paste(layers[n].img, (0, 0), layers[n].img)
        shots.append(im.crop((VIEW_TALL[0], 360, VIEW_TALL[2], 600)))
    w, h = shots[0].size
    sheet = Image.new("RGBA", (w * len(shots) + 6 * (len(shots) - 1), h), (40, 40, 40, 255))
    for i, im in enumerate(shots):
        sheet.paste(im, (i * (w + 6), 0))
    sheet = sheet.resize((sheet.size[0] * 2, sheet.size[1] * 2), Image.NEAREST)
    sheet.save(os.path.join(PREVIEW, name + "-anim.png"))


def preview(name, layers, anim=None):
    """Both views side by side at x1: the PC's 640x360 and the Pixel 9's 360x808. anim: a frame of the
    panel's animated box to lay over it (the break's still mid layer is empty inside its box)."""
    flat = composite(layers)
    if anim is not None:
        flat.paste(anim, ANIMS[name][0][:2], anim)
        fore = layers["fore"].img
        flat.paste(fore, (0, 0), fore)
    wide = flat.crop(VIEW_WIDE)
    tall = flat.crop(VIEW_TALL)
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
        frames = layers.pop("anim", None)
        bad = audit(layers, frames or ())
        total_bad += sum(bad.values())
        for ln, c in layers.items():
            c.img.save(os.path.join(OUT, "%s-%s.png" % (name, ln)))
        if frames:
            n = write_anim(name, frames)
            print("%-7s %d ticks, %d frames in %s-anim.png" % (name, len(frames), n, name))
        if do_preview:
            preview(name, layers, frames[-1] if frames else None)
            if frames:
                preview_anim(name, layers, frames, [k for k in [4, 12, 18, 22, 30, 38] if k < len(frames)])
        print("%-7s %d off-contract pixels%s" % (name, sum(bad.values()),
                                                   (" " + str(sorted(bad.items())[:4])) if bad else ""))
    return 1 if total_bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
