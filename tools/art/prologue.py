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
mid layer, <name>-anim.png and <name>-anim.json (write_anim; the four lifted one by one, the burning street's
rush, the hiding's searchers and the break's chain).
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
# bonelight.py keeps out of its contract on purpose (art-direction.md section 3, matte.add_ramp): Levina's
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
# WHICH OF THEM: the moot and the fall show the four disciplines, the wizards a fresh save already sees
# and the four the Archmage frees in the break. The procession ("He took every wizard who might rival
# him", once "He took them all") shows the secret three as well: Tituba, Vainamoinen and Laelaps are
# secret on the character select until their chapters open (UnlockCatalog's secretUntilStage), and Eric
# chose to show them here anyway (2026-11-01, "Show them") - a dark shape on a ridge names nobody.
# NEVER JOHN DEE (Eric, 2026-11-08, "remove the purple wizard, since he is the wizard that you are, and you
# do not get captures by him", then "Yes, remove him" for the moot and the procession): his grey robe and
# violet light read as the player, and the player is never taken.
CHARS = os.path.join(ROOT, "assets", "bonelight", "characters")
CELL = 48
# The rows a wizard stands in on his cell: feet on row 46, the contact line on 47, the hat near row 2.
CELL_STAND = 46.0

# (sprite prefix, the ramp his light is in the sprite, the ramp it burns here).
PYRO = ("pyromancer", "fire", "fire")
FROST = ("frostweaver", "ice", "ice")
STORM = ("stormcaller", "lightning", "lightning")
GEO = ("geomancer", "earth", "earth")
ROSTER = [PYRO, FROST, STORM, GEO]
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
                  warm=None, toward=None, contact=True, shade=None):
    """One of the game's wizards as a silhouette, his feet on (cx, yb), h tall if he stood upright.

    who: a ROSTER entry. strip/frame: which of his frames - idle to stand, moving to walk, the death's
    third to be brought to his knees. body: [fill, unused]. lit: his light burns at its anchor in its
    own ramp; dim: it is out, a dull stone where it was. warm: a ramp to dither a little of onto him
    (the fire in the moot). toward: (dx, dy[, colour]) to rim the edges facing a light. contact: False
    for a figure off the ground (the throne's lifted four), which has no contact line under him.
    shade(s, left, top): a pass over his own pixels, on his scratch layer, before he is laid down.
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
    if shade:
        shade(s, left, top)
    # The contact line: what of him touches the ground, on the ground row, in occlusion.
    feet = [x for x in range(s.w) for y in range(max(0, s.h - 4 - int(k)), s.h) if s.px[x, y][3]]
    over(c, s, left, top)
    if feet and contact:
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
# lets them have one - so the circle reads as four different people at a glance, not four shadows.
ROBE = {"pyromancer": R[4], "frostweaver": M["azure"][4], "stormcaller": G[4], "geomancer": M["verdant"][4]}


EYE = (AR[0], AR[1])


# HIS CREATURES. Eric, 2026-11-03, of the procession: "the villains in the silhouette look blocky. give them
# better detail". Each was one solid fill whose outline carried its class attachment and nothing else - a
# straight-edged mass with nothing inside it, so the guard on the ridge read as a tall flat rectangle with a
# spear and the bruiser as a slab with horns. They are still silhouettes in occlusion with no colour of their
# own, drawn now the way .ai/art-direction.md 4b asks of any actor: limbs with joints and a stance, hems torn
# rather than cut, horns that curve and taper, and inside the shape the breaks a near-occlusion tone can carry
# - a pauldron's lames, a helm's slit, armour seams, cloth folds - each form parted from the one it overlaps
# by occlusion. The only light on them is his: a rim on the edges that face his eclipse or his tower
# (rim_light), the same light caught one stop down along the plate edges that face it (catch_light), and the
# mark in their eyes. How much of that a body gets goes by its size on the panel (_Fig.lod): the flood's
# hundreds stay shapes with eyes, the procession's guards and the tide's front rank get all of it.


class _Inside(Cv):
    """A view of a figure's canvas that draws only where the figure already is, so whatever goes inside a
    silhouette - a plate, a seam, a fold - can never widen it."""

    def __init__(self, base):
        self.w, self.h, self.img, self.px = base.w, base.h, base.img, base.px

    def set(self, x, y, c):
        if c is None:
            return
        x = int(x)
        y = int(y)
        if 0 <= x < self.w and 0 <= y < self.h and self.px[x, y][3]:
            self.px[x, y] = rgb(c)


_N8 = ((-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1))


def _unit(v):
    d = math.hypot(v[0], v[1]) or 1.0
    return (v[0] / d, v[1] / d)


def rim_light(s, color, toward, inner=None, cut=0.3, strong=0.72):
    """Light the edge of a figure that faces a light. A pixel is on the edge when a neighbour is empty, and
    its outward normal is the sum of the directions to its empty neighbours, so a curved edge is lit as far
    round as it faces the light and no further - edge_light tests one neighbour, and lit a curve in steps.
    inner: the pixel inside the edge too, where the edge faces the light squarely (dot over `strong`)."""
    tx, ty = _unit(toward)
    px = s.px
    w, h = s.w, s.h
    hits = []
    for y in range(h):
        for x in range(w):
            if not px[x, y][3]:
                continue
            nx = ny = 0
            for (dx, dy) in _N8:
                qx, qy = x + dx, y + dy
                if not (0 <= qx < w and 0 <= qy < h) or not px[qx, qy][3]:
                    nx += dx
                    ny += dy
            if nx or ny:
                d = math.hypot(nx, ny)
                dot = (nx * tx + ny * ty) / d
                if dot > cut:
                    hits.append((x, y, nx / d, ny / d, dot))
    edge = set((x, y) for (x, y, _a, _b, _d) in hits)
    col = rgb(color)
    for (x, y, _nx, _ny, _dot) in hits:
        px[x, y] = col
    if inner:
        icol = rgb(inner)
        for (x, y, nx, ny, dot) in hits:
            if dot > strong:
                qx, qy = int(round(x - nx)), int(round(y - ny))
                if 0 <= qx < w and 0 <= qy < h and px[qx, qy][3] and (qx, qy) not in edge:
                    px[qx, qy] = icol


def catch_light(s, plates, toward, color, base=OCC):
    """The plates inside a figure catch the light along the edge that faces it: a plate pixel whose
    neighbour toward the light is the occlusion between forms steps up to `color`. It is what turns a
    pauldron's lames, a helm's brow and a shield's border into raised edges instead of flat patches."""
    tx, ty = _unit(toward)
    dx = 1 if tx > 0.38 else (-1 if tx < -0.38 else 0)
    dy = 1 if ty > 0.38 else (-1 if ty < -0.38 else 0)
    want = set(rgb(c) for c in plates)
    occ = rgb(base)
    col = rgb(color)
    px = s.px
    hits = []
    for y in range(1, s.h - 1):
        for x in range(1, s.w - 1):
            p = px[x, y]
            if p[3] and p in want and px[x + dx, y + dy] == occ:
                hits.append((x, y))
    for (x, y) in hits:
        px[x, y] = col


def form_light(s, plates, toward, centre, radius, deep=OCC):
    """The side of a figure turned from the light goes back to occlusion: plate pixels past the middle of
    the body, measured along the light's direction, dither down to `deep`, all of them by the far edge.
    So the breaks inside a figure gather on the side his light finds and the rest stays a silhouette."""
    tx, ty = _unit(toward)
    cx, cy = centre
    want = set(rgb(c) for c in plates)
    d = rgb(deep)
    px = s.px
    for y in range(s.h):
        row = TH[y % 8]
        for x in range(s.w):
            p = px[x, y]
            if p[3] and p in want:
                t = ((x - cx) * tx + (y - cy) * ty) / radius
                if 0.2 - t * 1.1 > row[x % 8]:
                    px[x, y] = d


class _Fig(object):
    """One of his creatures on its scratch canvas: its feet at (ox, oy), u canvas pixels to the unit (a body
    stands about 20 units), x mirrored by facing, and how much detail its size on the panel can carry."""

    def __init__(self, h, facing, seed, tall=38.0, wide=36.0):
        self.u = u = h / 20.0
        self.s = Cv(int(wide * u) + 8, int(tall * u) + 8)
        self.ox = self.s.w // 2
        self.oy = int((tall - 4.0) * u) + 4
        self.f = facing
        self.rng = random.Random(seed)
        # 0: the flood's hundreds, a shape and its eyes; 1: the joints, a torn hem and a few breaks inside;
        # 2: the procession's guards, the tide's front rank and the searchers, everything.
        self.lod = 0 if u < 1.25 else (1 if u < 2.2 else 2)
        self.inside = _Inside(self.s)

    def T(self, a, b):
        return (self.ox + self.f * a * self.u, self.oy + b * self.u)

    def j(self, a):
        return (self.rng.random() - 0.5) * a

    def _c(self, inside):
        return self.inside if inside else self.s

    def poly(self, pts, col, inside=False):
        self._c(inside).poly([self.T(a, b) for (a, b) in pts], col)

    def stroke(self, a, b, a2, b2, r0, r1, col, inside=False):
        x0, y0 = self.T(a, b)
        x1, y1 = self.T(a2, b2)
        sk._stroke(self._c(inside), x0, y0, x1, y1, max(0.5, r0 * self.u), max(0.5, r1 * self.u), col)

    def limb(self, pts, radii, col, inside=False):
        """A jointed limb, joint to joint, tapering through radii (one per point)."""
        for k in range(len(pts) - 1):
            self.stroke(pts[k][0], pts[k][1], pts[k + 1][0], pts[k + 1][1], radii[k], radii[k + 1], col, inside)

    def line(self, pts, col, inside=True):
        c = self._c(inside)
        for (a, b), (a2, b2) in zip(pts, pts[1:]):
            x0, y0 = self.T(a, b)
            x1, y1 = self.T(a2, b2)
            c.line(x0, y0, x1, y1, col)

    def disc(self, a, b, rx, ry, col, inside=False):
        x, y = self.T(a, b)
        self._c(inside).disc(x, y, max(0.5, rx * self.u), max(0.5, ry * self.u), col)

    def dot(self, a, b, col, inside=True):
        x, y = self.T(a, b)
        self._c(inside).set(x, y, col)

    def bend(self, p0, p1, p2, n):
        """n + 1 points along the quadratic curve p0 -> p2 drawn toward p1."""
        out = []
        for k in range(n + 1):
            t = k / float(n)
            out.append(((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
                        (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]))
        return out

    def ribbon(self, spine, w0, w1):
        """A strip of cloth along a spine, w0 wide where it is tied and w1 at its torn end, as a polygon."""
        n = len(spine)
        left, right = [], []
        for k, (a, b) in enumerate(spine):
            pa, pb = spine[max(0, k - 1)]
            qa, qb = spine[min(n - 1, k + 1)]
            ta, tb = _unit((qa - pa, qb - pb))
            w = (w0 + (w1 - w0) * k / float(n - 1)) * 0.5
            left.append((a - tb * w, b + ta * w))
            right.append((a + tb * w, b - ta * w))
        return left + right[::-1]

    def tatter(self, a0, a1, b, n, deep):
        """A hem from a0 to a1 along row b, torn into n tongues of uneven length."""
        pts = []
        for i in range(2 * n + 1):
            a = a0 + (a1 - a0) * i / (2.0 * n) + self.j(abs(a1 - a0) / (3.0 * n))
            if i % 2:
                pts.append((a, b + deep * (0.45 + 0.75 * self.rng.random())))
            else:
                pts.append((a, b - deep * 0.3 * self.rng.random()))
        return pts


def _swarmer(F, mass, plate, pose):
    """Small, hunched, narrow: the pointed cowl, the back bowed under it, one clawed hand out of the cloak."""
    hem = F.tatter(4.5, -4.7, -0.4, 4 if F.lod else 2, 1.0)
    F.poly([(1.2, -20.4), (2.6, -19.0), (3.8, -17.3), (3.3, -16.5), (2.5, -15.4), (2.7, -14.0), (3.4, -12.6),
            (4.2, -10.0), (4.4, -6.6), (4.2, -2.8)] + hem +
           [(-4.8, -2.8), (-5.2, -6.8), (-5.1, -10.4), (-4.4, -13.4), (-3.0, -15.9), (-1.2, -18.0)], mass)
    F.limb([(2.4, -11.0), (4.6, -8.8), (6.1, -6.8)], [1.05, 0.75, 0.45], mass)
    if F.lod:
        for (a, b) in [(7.2, -6.0), (6.9, -5.1), (6.1, -5.0)]:
            F.line([(6.1, -6.8), (a, b)], mass, inside=False)
        # the cowl a shape of its own over the cloak, its edge round the face, the face a hollow in it
        F.poly([(1.2, -20.4), (2.6, -19.0), (3.8, -17.3), (3.3, -16.5), (2.6, -14.4), (3.4, -12.8), (1.6, -12.2),
                (-0.6, -12.8), (-2.6, -12.0), (-4.6, -12.8), (-4.4, -13.4), (-3.0, -15.9), (-1.2, -18.0)],
               plate, inside=True)
        F.line([(-4.4, -12.9), (-2.6, -12.2), (-0.6, -13.0), (1.6, -12.4), (3.3, -12.9)], mass)
        F.disc(2.5, -15.6, 0.95, 1.15, mass, inside=True)
        F.line([(1.4, -19.6), (2.7, -18.4), (3.1, -17.2)], mass)
        folds = [(-2.8, -11.2, -3.6, -1.0), (0.2, -11.6, -0.4, -0.6)]
        if F.lod > 1:
            folds += [(2.6, -10.8, 3.2, -1.6), (-4.4, -9.0, -4.2, -2.6)]
        for (a, b, a2, b2) in folds:
            F.line([(a, b), (a2, b2)], plate)
    return [(2.7, -15.7)], None


def _runner(F, mass, plate, pose):
    """Lean and pitched forward, all legs, a rag streaming off its shoulders behind it, wide and low."""
    st = F.j(2.4)
    F.limb([(-0.4, -7.6), (-1.8 + st * 0.4, -4.0), (-3.8 + st, -1.0), (-3.0 + st, 0.0)], [1.05, 0.8, 0.55, 0.45], mass)
    F.limb([(0.8, -7.6), (3.0 - st * 0.4, -4.6), (2.6 - st, -0.8), (3.8 - st, 0.0)], [1.05, 0.8, 0.55, 0.45], mass)
    # The rag: three torn strips from the shoulders, each streaming on its own wave, with air between them.
    spines = [([(1.4, -15.0), (-2.0, -15.6), (-5.2, -14.8 + F.j(0.6)), (-8.4, -15.3), (-11.8, -14.6 + F.j(1.0))], 2.6),
              ([(1.0, -13.8), (-2.4, -13.2), (-5.6, -12.2 + F.j(0.6)), (-8.6, -12.5), (-10.8, -11.6 + F.j(1.0))], 2.2),
              ([(0.6, -12.8), (-2.2, -11.4), (-4.8, -10.3 + F.j(0.6)), (-7.2, -10.0 + F.j(0.8))], 1.8)]
    for (sp, w) in spines:
        F.poly(F.ribbon(sp, w, 0.6), mass if F.lod else plate)
    F.poly([(-1.8, -6.8), (1.4, -7.8), (3.0, -9.6), (4.6, -12.0), (5.0, -13.8), (4.0, -15.2), (2.0, -15.0),
            (0.0, -12.8), (-1.6, -9.8)], mass)
    F.stroke(3.8, -14.4, 5.0, -15.0, 0.9, 0.8, mass)
    F.disc(5.6, -15.6, 1.8, 1.6, mass)
    F.poly([(5.8, -15.2), (7.8, -14.9), (7.3, -13.8), (5.6, -13.9)], mass)
    F.limb([(1.8, -13.6), (0.0, -11.2), (-0.8, -9.4)], [0.75, 0.6, 0.45], mass)
    F.limb([(3.6, -13.4), (5.8, -11.6), (7.4, -10.0)], [0.75, 0.6, 0.45], mass)
    if F.lod:
        for (a, b) in [(8.4, -9.6), (8.0, -8.8)]:
            F.line([(7.4, -10.0), (a, b)], mass, inside=False)
        for (sp, w) in spines:
            F.poly(F.ribbon(sp[:-1] + [((sp[-2][0] + sp[-1][0]) / 2.0, (sp[-2][1] + sp[-1][1]) / 2.0)], w - 1.2, 0.3),
                   plate, inside=True)
        F.line([(-1.6, -8.0), (1.6, -8.8)], plate)                   # the cord at its hips
        F.line([(0.6, -13.4), (4.2, -14.6)], plate)                  # the knot at its throat
        if F.lod > 1:
            F.line([(-1.0, -9.4), (0.6, -12.4), (2.8, -14.6)], plate)  # the spine through the wrapping
            F.line([(6.0, -14.6), (7.4, -14.4)], plate)              # the jaw
    return [(6.2, -15.9)], None


def _bruiser(F, mass, plate, pose):
    """Wide, upright, heavy: a horned helm and pauldrons that break the line of its shoulders."""
    st = F.j(2.4) * 0.4
    knees = [(-3.2 - st, -3.8), (3.0 + st, -3.8)]
    for (hip, knee, ank) in [((-2.4, -7.4), knees[0], (-3.4 - st, -0.8)), ((2.2, -7.4), knees[1], (3.4 + st, -0.8))]:
        F.limb([hip, knee, ank], [1.8, 1.5, 1.2], mass)
        a = ank[0]
        F.poly([(a - 1.3, -1.6), (a + 1.6, -1.2), (a + 2.2, 0.0), (a - 1.4, 0.0)], mass)
    arms = []
    for sg in (-1, 1):
        sh, el = (sg * 5.6, -14.0), (sg * 7.0 + (0.4 if sg > 0 else 0.0), -10.6)
        fist = (sg * 6.8 + (0.9 if sg > 0 else 0.0), -7.6)
        F.limb([sh, el, fist], [1.4, 1.2, 1.05], mass)
        F.disc(fist[0], fist[1] + 0.3, 1.45, 1.35, mass)
        arms.append((sh, el, fist))
    torso = [(-3.6, -7.2), (3.8, -7.2), (4.4, -9.0), (5.6, -12.0), (5.8, -14.6), (4.4, -16.4), (2.6, -17.0),
             (-2.2, -17.0), (-4.2, -16.4), (-5.6, -14.6), (-5.4, -12.0), (-4.2, -9.0)]
    F.poly(torso, mass)
    hem = F.tatter(3.2, -3.0, -4.0, 3 if F.lod else 1, 0.9)
    F.poly([(-3.4, -7.8), (3.6, -7.8)] + hem, mass)
    lames = [(6.4, -13.2, 1.8, 1.0), (6.1, -14.2, 2.2, 1.25), (5.5, -15.4, 2.6, 1.55)]
    for sg in (-1, 1):
        for (a, b, rx, ry) in lames:
            F.disc(sg * a, b, rx, ry, mass)
    if F.lod:
        F.poly([(4.6, -16.6), (5.6, -19.0), (6.4, -16.4)], mass)     # a spike on the near shoulder
        F.poly([(-4.8, -16.4), (-5.4, -18.0), (-6.2, -16.2)], mass)  # and a broken one on the far
    F.disc(0.8, -18.0, 2.4, 2.3, mass)
    F.poly([(1.0, -17.2), (3.3, -17.4), (2.9, -15.4), (1.4, -15.6)], mass)
    horns = []
    for sg in (-1, 1):
        pts = F.bend((0.8 + sg * 1.6, -19.2), (0.8 + sg * 5.8, -19.2), (0.8 + sg * 4.4, -24.6), 10 if F.lod else 5)
        F.limb(pts, [1.0 - 0.8 * k / float(len(pts) - 1) for k in range(len(pts))], mass)
        horns.append(pts)
    if F.lod:
        # the breastplate, its seam off-centre where the body is turned, the belt under it
        F.poly([(-3.0, -9.0), (3.4, -9.0), (4.6, -11.6), (4.8, -14.4), (3.4, -15.8), (-2.6, -15.8), (-4.6, -14.4),
                (-4.4, -11.6)], plate, inside=True)
        F.line([(1.4, -15.6), (1.2, -9.2)], mass)
        F.line([(-4.4, -12.4), (-1.0, -11.8)], mass)
        F.poly([(-4.0, -8.3), (4.2, -8.3), (4.2, -7.5), (-4.0, -7.5)], mass, inside=True)
        F.disc(1.2, -7.9, 0.6, 0.5, plate, inside=True)
        F.poly([(-2.8, -7.2), (3.0, -7.2)] + [(a, b - 0.9) for (a, b) in hem[1:-1]], plate, inside=True)
        F.line([(0.4, -7.0), (0.2, -4.4)], mass)
        # the arms in vambraces below the pauldrons, the gauntlets with their knuckles
        for (sh, el, fist) in arms:
            F.limb([el, fist], [0.75, 0.65], plate, inside=True)
            F.disc(fist[0], fist[1] + 0.3, 1.0, 0.9, plate, inside=True)
            F.line([(fist[0] - 1.0, fist[1] + 0.7), (fist[0] + 1.0, fist[1] + 0.7)], mass)
        # the pauldrons in lames, each over the one below it with occlusion between
        for sg in (-1, 1):
            for i, (a, b, rx, ry) in enumerate(lames):
                if F.lod == 1 and i < 2:
                    continue
                F.disc(sg * a, b - 0.2, rx + 0.1, ry + 0.2, mass, inside=True)
                F.disc(sg * a, b - 0.1, rx - 0.45, ry - 0.25, plate, inside=True)
        # the helm: a dome, the brow band under it, the visor's slit, the cheek guard
        F.disc(0.6, -18.8, 2.0, 1.4, plate, inside=True)
        F.line([(-1.6, -17.9), (3.2, -17.9)], mass)
        F.poly([(1.3, -16.9), (3.1, -17.0), (2.7, -15.6), (1.6, -15.7)], plate, inside=True)
        F.line([(0.4, -17.3), (3.2, -17.3)], mass)
        for (a, b) in knees:
            F.disc(a, b, 1.0, 0.9, plate, inside=True)
        if F.lod > 1:
            for pts in horns:
                for k in (2, 4, 6):
                    a, b = pts[k]
                    a2, b2 = pts[k + 1]
                    F.line([(a, b), ((a + a2) / 2.0, (b + b2) / 2.0)], plate)
    return [(1.8, -17.3), (2.8, -17.3)], None


def _shielder(F, mass, plate, pose):
    """Medium, rounded, behind a flat plane held to one side: straight edges against a curved body."""
    st = F.j(2.4) * 0.4
    for (hip, knee, ank) in [((-1.4, -7.0), (-2.0 - st, -3.6), (-2.6 - st, -0.6)),
                             ((1.6, -7.0), (2.4 + st, -3.6), (2.6 + st, -0.6))]:
        F.limb([hip, knee, ank], [1.15, 0.95, 0.8], mass)
        a = ank[0]
        F.poly([(a - 0.9, -1.0), (a + 1.2, -0.8), (a + 1.6, 0.0), (a - 1.0, 0.0)], mass)
    F.limb([(-3.2, -14.2), (-4.2, -11.0), (-3.8, -8.4)], [1.0, 0.85, 0.7], mass)
    F.disc(-3.8, -7.8, 1.0, 1.0, mass)
    F.poly([(-2.8, -7.6), (2.6, -7.6), (3.4, -9.4), (3.8, -12.2), (3.2, -14.6), (1.8, -15.8), (-2.0, -15.8),
            (-3.4, -14.6), (-4.0, -12.0), (-3.6, -9.4)], mass)
    skirt = F.tatter(3.4, -3.2, -4.6, 4 if F.lod else 2, 0.6)
    F.poly([(-2.8, -8.2), (2.8, -8.2)] + skirt, mass)
    F.disc(-2.4, -15.0, 1.5, 1.1, mass)
    F.disc(0.6, -17.4, 1.9, 2.0, mass)
    F.poly([(-0.6, -18.8), (0.6, -20.2), (2.4, -19.6), (2.0, -18.4)], mass)
    if F.lod:
        F.poly([(-2.4, -9.0), (2.4, -9.0), (3.2, -12.0), (2.6, -14.4), (1.4, -15.2), (-1.8, -15.2), (-3.0, -14.2),
                (-3.4, -11.8)], plate, inside=True)
        F.line([(-3.4, -11.6), (-0.4, -10.8), (3.4, -11.6)], mass)
        F.line([(-3.0, -8.5), (3.0, -8.5)], mass)
        F.poly([(-2.6, -8.0), (2.8, -8.0)] + [(a, b - 0.7) for (a, b) in skirt[1:-1]], plate, inside=True)
        F.disc(-2.4, -15.2, 1.1, 0.7, plate, inside=True)
        F.disc(0.5, -18.0, 1.5, 1.2, plate, inside=True)
        F.line([(-1.4, -17.3), (2.5, -17.3)], mass)
        F.line([(2.1, -17.3), (2.2, -15.8)], plate)
    # The plane, last and nearest: its top corner bitten out at size, a border, a boss, a split plank.
    top = [(3.6, -16.6), (7.2, -16.9), (7.8, -15.8), (8.8, -15.9)] if F.lod else [(3.6, -16.6), (8.8, -17.0)]
    shield = top + [(8.6, -2.2), (3.8, -1.6)]
    F.poly(shield, plate)
    F.line([(3.6, -16.6), (3.8, -1.6)], mass, inside=False)
    if F.lod:
        F.line([(4.5, -15.6), (7.0, -15.8), (7.6, -14.8), (7.9, -14.8), (7.8, -3.0), (4.6, -2.6), (4.5, -15.6)], mass)
        F.disc(6.2, -9.2, 1.25, 1.25, mass, inside=True)
        F.disc(6.1, -9.4, 0.6, 0.6, plate, inside=True)
        if F.lod > 1:
            F.line([(6.2, -15.6), (6.2, -10.6)], mass)
            F.line([(6.2, -7.8), (6.1, -2.8)], mass)
    return [(1.7, -17.0)], None


def _caster(F, mass, plate, pose):
    """Tall and narrow, a hood leaning back off its head, one arm up, the orb loose above the hand."""
    hem = F.tatter(4.0, -5.2, -0.4, 4 if F.lod else 2, 1.1)
    F.poly([(-0.8, -24.0), (0.6, -22.0), (1.6, -20.0), (1.0, -19.0), (1.5, -17.8), (2.2, -16.6), (2.6, -13.0),
            (2.7, -8.4), (3.4, -3.6)] + hem +
           [(-5.0, -2.2), (-4.0, -5.6), (-3.4, -10.0), (-3.2, -13.6), (-2.8, -17.4), (-2.2, -20.4), (-1.8, -22.4)], mass)
    F.limb([(1.6, -15.4), (3.4, -14.8), (4.8, -18.6)], [0.85, 0.7, 0.5], mass)
    if F.lod:
        # the sleeve falling off the raised forearm, torn
        F.poly([(2.4, -15.8), (4.2, -17.2), (4.8, -16.8), (4.6, -14.4), (4.2, -11.8 + F.j(0.8)), (3.7, -13.0),
                (3.1, -11.0 + F.j(0.8)), (2.7, -13.2)], mass)
        for (a, b) in [(4.4, -20.0), (5.0, -20.4), (5.6, -20.0)]:
            F.line([(4.8, -18.6), (a, b)], mass, inside=False)
        F.poly([(-0.8, -24.0), (0.6, -22.0), (1.6, -20.0), (1.0, -19.0), (1.5, -17.8), (0.2, -16.8), (-1.6, -17.0),
                (-2.8, -17.4), (-2.2, -20.4), (-1.8, -22.4)], plate, inside=True)
        F.disc(1.0, -19.1, 0.85, 1.2, mass, inside=True)
        F.line([(-0.6, -23.2), (0.6, -21.6), (1.2, -20.2)], mass)
        F.line([(-2.8, -17.2), (-1.0, -16.8), (1.0, -16.9), (2.2, -16.4)], mass)
        F.line([(-3.0, -11.6), (2.6, -12.4)], plate)
        folds = [(-1.6, -11.0, -3.0, -0.8), (1.0, -11.2, 1.4, -0.6)]
        if F.lod > 1:
            folds += [(2.2, -9.0, 3.0, -1.4), (-0.6, -11.8, -1.0, -7.4), (-3.0, -8.0, -4.4, -1.6)]
            F.line([(2.9, -14.6), (3.6, -12.6)], plate)
        for (a, b, a2, b2) in folds:
            F.line([(a, b), (a2, b2)], plate)

    def orb():
        F.disc(6.2, -22.8, 1.5, 1.5, AR[3])
        F.disc(5.8, -23.2, 0.7, 0.7, AR[2])
    return [(1.1, -19.2)], orb


def _knight(F, mass, plate, pose):
    """A guard in plate under a torn cloak, a great helm with a slit in it, and the spear: a long haft, a
    leaf blade over its lugs, a pennon under them."""
    st = F.j(2.4) * 0.4
    # the cloak behind him, off both shoulders, swinging back as he walks
    cloak_hem = F.tatter(-7.6, -1.6, -1.6, 3 if F.lod else 1, 1.0)
    cloak = [(-2.6, -17.0), (-4.6, -16.2), (-5.4, -13.4), (-5.8, -10.6), (-6.8, -6.4)] + cloak_hem + \
            [(-1.2, -5.4), (-0.6, -12.0), (-1.0, -16.0)]
    F.poly(cloak, mass)
    legs = [((-1.4, -8.4), (-1.9 - st, -4.4), (-2.4 - st, -0.8)), ((1.4, -8.4), (2.1 + st, -4.4), (2.6 + st, -0.8))]
    for (hip, knee, ank) in legs:
        F.limb([hip, knee, ank], [1.2, 1.0, 0.85], mass)
        a = ank[0]
        F.poly([(a - 0.9, -1.2), (a + 1.4, -0.9), (a + 1.9, 0.0), (a - 1.0, 0.0)], mass)
    F.limb([(-3.4, -15.4), (-4.2, -11.8), (-3.8, -8.8)], [1.0, 0.85, 0.75], mass)
    F.disc(-3.8, -8.2, 0.95, 0.95, mass)
    # chest broad and the waist drawn in, the tabard flaring under the belt
    F.poly([(-2.4, -8.6), (2.4, -8.6), (2.7, -10.2), (3.6, -12.8), (4.2, -15.2), (3.6, -16.9), (-3.4, -16.9),
            (-4.2, -15.0), (-3.4, -12.6), (-2.7, -10.2)], mass)
    skirt = F.tatter(3.8, -3.4, -4.2, 3 if F.lod else 1, 0.9)
    F.poly([(-2.6, -9.0), (2.8, -9.0)] + skirt, mass)
    for (a, b, rx, ry) in [(-3.0, -16.0, 1.8, 1.3), (2.6, -16.2, 2.2, 1.5), (3.0, -15.0, 1.9, 1.3)]:
        F.disc(a, b, rx, ry, mass)
    helm = [(-1.7, -17.0), (2.1, -17.0), (2.6, -20.6), (1.6, -22.6), (0.0, -23.2), (-1.6, -22.2), (-2.2, -19.4)]
    F.poly(helm, mass)
    F.poly([(-0.6, -23.0), (0.0, -23.9), (1.2, -23.5), (1.4, -22.8)], mass)   # the comb along the crown
    # The spear, sloped back at the march: upright, its head stood in front of the tower on the ridge
    # behind him and was lost in it. Along the haft (d) and across it (p): the socket, the lugs, the blade.
    foot, top = (7.0, 0.2), (3.6, -28.4)
    d = _unit((top[0] - foot[0], top[1] - foot[1]))
    p = (-d[1], d[0])

    def at(t, w=0.0):
        return (top[0] + d[0] * t + p[0] * w, top[1] + d[1] * t + p[1] * w)
    grip = (foot[0] + (top[0] - foot[0]) * 12.8 / 28.6, -12.6)
    F.limb([(3.2, -15.4), (4.0, -11.6), grip], [1.0, 0.85, 0.8], mass)
    F.line([foot, top], mass, inside=False)
    x0, y0 = F.T(*foot)
    x1, y1 = F.T(*top)
    F.s.line(x0 + F.f, y0, x1 + F.f, y1, mass)
    F.stroke(at(-1.4)[0], at(-1.4)[1], at(0.2)[0], at(0.2)[1], 0.45, 0.45, mass)
    F.line([at(-0.4, -1.0), at(-0.4, 1.0)], mass, inside=False)
    F.poly([at(0.0), at(1.2, 0.95), at(4.0), at(1.2, -0.85)], mass)
    F.disc(grip[0], grip[1], 1.0, 1.0, mass)
    if F.lod:
        a, b = at(-0.6)
        F.poly([(a, b), (a - 3.4, b + 0.4 + F.j(0.4)), (a - 6.2, b + 0.6 + F.j(0.6)), (a - 4.4, b + 1.5),
                (a - 6.4, b + 2.8 + F.j(0.6)), (a - 3.0, b + 2.4), at(-2.4)], mass)
        # the cloak's folds, falling from the shoulder
        for (a, b, a2, b2) in [(-4.4, -15.0, -6.2, -3.0), (-2.6, -15.4, -3.4, -4.8)]:
            F.line([(a, b), (a2, b2)], plate)
        # the breastplate and its seam, the belt, the tabard below it torn at one corner
        F.poly([(-2.2, -9.4), (2.2, -9.4), (2.8, -11.4), (3.6, -14.0), (3.0, -16.0), (-2.6, -16.0), (-3.6, -14.6),
                (-2.8, -11.4)], plate, inside=True)
        F.line([(0.9, -15.8), (0.7, -9.6)], mass)
        F.line([(-3.2, -13.2), (-0.4, -12.6), (3.4, -13.0)], mass)
        F.poly([(-3.0, -9.6), (3.0, -9.6), (3.0, -8.8), (-3.0, -8.8)], mass, inside=True)
        F.poly([(-2.2, -8.4), (2.6, -8.4)] + [(a, b - 0.8) for (a, b) in skirt[1:-1]], plate, inside=True)
        F.line([(0.6, -8.4), (0.4, -4.8)], mass)
        # the near pauldron's lames, the helm's crown and face plate with the slit between
        F.disc(2.6, -16.4, 1.7, 1.0, plate, inside=True)
        F.disc(3.0, -15.0, 1.6, 0.9, mass, inside=True)
        F.disc(3.0, -15.2, 1.3, 0.6, plate, inside=True)
        F.poly([(-1.4, -21.0), (2.3, -21.0), (1.5, -22.3), (0.0, -22.8), (-1.3, -22.0)], plate, inside=True)
        F.poly([(0.3, -19.9), (2.4, -19.9), (2.1, -17.5), (0.5, -17.5)], plate, inside=True)
        F.line([(-1.8, -20.4), (2.6, -20.4)], mass)
        for (_hip, (a, b), _ank) in legs:
            F.disc(a, b, 0.75, 0.7, plate, inside=True)
        F.line([at(0.4, -0.3), at(3.0, -0.2)], plate)
        F.line([(a, b + 0.8), (a - 3.6, b + 1.2)], plate)
        if F.lod > 1:
            F.line([(0.9, -19.2), (0.9, -18.8)], mass)
            F.line([(1.7, -19.2), (1.7, -18.8)], mass)
            for (_hip, (a, b), (a2, b2)) in legs:
                F.line([(a - 0.2, b + 1.0), (a2, b2 - 0.6)], plate)
    return [(1.2, -20.4), (2.0, -20.4)], None


def _hound_parts(F, mass, plate, pose):
    """His hound: a deep chest and a high tuck, hind legs that bend back at the hock, hackles along the back,
    a long head and a sickle tail. pose (sniffing_hound): stride, the head's turn and drop, the tail's angle
    and the nose's twitch; otherwise it walks."""
    pose = pose or {}
    st = pose.get("stride", F.j(2.4))
    ang, drop, ta, tw = pose.get("head", 0.0), pose.get("drop", 0.0), pose.get("tail", 0.0), pose.get("twitch", 0.0)
    pivot = (6.8, -10.4)

    def H(a, b):
        rx, ry = rot(a - pivot[0], b - pivot[1], ang)
        return (pivot[0] + rx + tw, pivot[1] + ry + drop)
    hr = [1.3, 0.8, 0.55, 0.5]
    fr = [1.0, 0.7, 0.55, 0.5]
    F.limb([(-5.2, -7.6), (-3.8 - st * 0.3, -4.8), (-6.0 - st * 0.5, -2.2), (-5.4 - st * 0.5, 0.0)], hr, mass)
    F.limb([(3.8, -8.0), (3.6, -5.4), (3.8 + st * 0.5, -1.4), (4.6 + st * 0.5, 0.0)], fr, mass)
    F.limb([(-5.8, -7.4), (-4.6 + st * 0.3, -4.6), (-7.0 + st * 0.5, -2.0), (-6.2 + st * 0.5, 0.0)], hr, mass)
    F.limb([(4.6, -8.0), (4.6, -5.4), (4.6 - st * 0.5, -1.4), (5.4 - st * 0.5, 0.0)], fr, mass)
    base = (-7.6, -8.0)
    tail = [base] + [(base[0] + rx, base[1] + ry) for (rx, ry) in [rot(-2.8, -1.6, ta), rot(-4.4, 0.6, ta)]]
    n = 6 if F.lod else 3
    F.limb(F.bend(tail[0], tail[1], tail[2], n), [0.7 - 0.45 * k / float(n) for k in range(n + 1)], mass)
    back = [(-7.8, -8.4), (-5.6, -9.2), (-3.0, -10.0), (0.0, -9.8), (3.0, -10.6), (5.4, -11.6)]
    F.poly(back + [(6.8, -12.0), (7.6, -10.8), (6.6, -9.0), (5.6, -6.6), (3.4, -6.0), (0.6, -7.0), (-2.4, -8.2),
                   (-4.4, -7.4), (-6.2, -6.2), (-7.8, -6.8)], mass)
    if F.lod:
        # the hackles: a short rough ridge of fur up the back from the loin to the withers
        for k in range(9):
            t = (k + 0.5) / 9.0
            a = -2.6 + 7.6 * t
            b = -9.9 - 1.6 * t
            F.poly([(a - 0.35, b + 0.4), (a - 0.7 + F.j(0.3), b - 0.35 - 0.35 * F.rng.random()), (a + 0.35, b + 0.3)],
                   mass)
    F.limb([(5.6, -9.8), (pivot[0] + 0.4, pivot[1] + drop * 0.7)], [1.6, 1.3], mass)
    F.poly([H(a, b) for (a, b) in [(6.2, -12.6), (7.6, -13.0), (9.0, -12.4), (11.6, -11.4), (11.4, -10.4),
                                   (9.6, -10.0), (8.0, -9.4), (6.4, -9.8)]], mass)
    F.poly([H(a, b) for (a, b) in [(7.2, -12.8), (7.0, -14.4), (8.2, -12.6)]], mass)
    if F.lod:
        F.line([(3.4, -10.2), (4.2, -8.4), (3.8, -6.6)], plate)            # the shoulder
        F.line([(-4.4, -9.4), (-3.6, -7.6), (-4.0, -5.6)], plate)          # the thigh
        F.line([H(8.0, -12.2), H(11.2, -11.2)], plate)                    # the bridge of the muzzle
        if F.lod > 1:
            F.line([(-0.6, -9.2), (0.4, -7.6)], plate)                    # the flank's fur
            F.line([(1.2, -9.6), (2.0, -7.4)], plate)
            F.line([H(11.0, -10.6), H(9.0, -10.2)], plate)                # the lip
    return [H(8.9, -11.0)], None


_KINDS = {"swarmer": _swarmer, "runner": _runner, "bruiser": _bruiser, "shielder": _shielder, "caster": _caster,
          "knight": _knight, "hound": _hound_parts}


def creature_fig(kind, h, facing, body, rim, eye, seed, toward, pose=None):
    """The drawn figure of one creature on its own scratch canvas (a _Fig), apart from where it stands:
    what creature() composites, split out so the tide's rush can render a body once and paste it on
    every frame it walks through (its cache keys on everything this takes)."""
    mass, plate = body or [OCC, OCC]
    F = _Fig(h, facing, seed)
    eyes, after = _KINDS[kind](F, mass, plate, pose)
    if F.lod and plate != mass:
        # Caught edges first, then the far side let go: the other way round, every hole the dither
        # leaves in a plate would catch the light along its own edge.
        caught = V[3] if F.lod > 1 else V[4]
        catch_light(F.s, [plate], toward, caught, base=mass)
        form_light(F.s, [plate, caught], toward, F.T(0, -11.0), 11.0 * F.u, deep=mass)
    if rim:
        # One pixel, as the wizards beside them are rimmed: a second stop inside it made the guards
        # brighter than the wizards they lead, and the caster in the hiding brighter than the player.
        rim_light(F.s, rim, toward)
    if after:
        after()
    if eye:
        # Small ones get the arcane row's lit stop alone: massed, its near-white hi read as a sky
        # full of stars rather than as eyes.
        small = F.u < 1.3
        for i, (ex, ey) in enumerate(eyes):
            F.dot(ex, ey, EYE[1] if small or i else EYE[0], inside=False)
            if F.u >= 1.6 and len(eyes) == 1:
                x, y = F.T(ex, ey)
                F.s.set(x + F.f, y, EYE[1])
    return F


def creature(c, kind, cx, yb, h, facing=1, body=None, rim=None, eye=True, seed=0, light=None, pose=None):
    """One of his: the taxonomy's class attachment (.ai/art-direction.md section 4) - the swarmer's cowl,
    the runner's streaming rag, the bruiser's horns and pauldrons, the shielder's flat plane, the caster's
    loose orb, the knight's spear, the hound's line - on a body drawn with joints, weight and torn edges,
    and every one wearing the same mark, one or two pixels of cold arcane, the only light it has of its
    own (.ai/world-and-tone.md). body: [the mass, the plates inside it]. light: the point in the frame his
    light comes from (the eclipse, the tower); the rim and the plates' caught edges face it."""
    toward = (-0.7, -0.7)
    if light is not None:
        toward = (light[0] - cx, light[1] - (yb - h * 0.6))
    F = creature_fig(kind, h, facing, body, rim, eye, seed, toward, pose)
    over(c, F.s, cx - F.ox, yb - F.oy)


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
    # The hem torn into tongues of uneven length that run on into the ground as his shadow (Eric,
    # 2026-11-03, of his creatures and this shape with them: "blocky"); a zigzag of even teeth read as a
    # sawn edge.
    hem = []
    rng = random.Random(64)
    for i in range(23):
        t = i / 22.0
        tongue = i % 3 == 1 or (i % 2 and rng.random() < 0.4)
        hem.append((20.4 - 40.6 * t + (rng.random() - 0.5) * 0.8,
                    (2.0 + rng.random() * 1.6) if tongue else (-0.8 + rng.random() * 0.9)))
    # Sloped shoulders and sides that bow outward to the hem: drawn as straight lines from a flat
    # shoulder line it read as a cabinet with a head on it. Drawn as one smooth bow it still read as a
    # cone; the sides now step out where the cloth falls in folds, so the rim breaks into lengths down
    # them as it does down a hanging cloak.
    cloak = P([(-4.6, -48.4), (4.6, -48.4), (7.6, -47.2), (9.8, -45.4), (11.0, -42.6), (11.6, -38.0),
               (12.0, -33.0), (12.6, -27.6), (13.4, -22.0), (14.6, -17.2), (14.2, -14.8), (15.6, -11.2),
               (16.6, -7.8), (16.3, -6.2), (18.0, -3.8), (20.0, -1.4)] + hem +
              [(-19.0, -1.8), (-17.2, -4.6), (-17.4, -6.4), (-15.8, -9.8), (-15.0, -14.6), (-14.4, -16.8),
               (-13.6, -20.6), (-13.2, -23.4), (-12.0, -31.4), (-11.2, -38.6), (-10.6, -42.6), (-9.4, -45.4),
               (-7.4, -47.2)])
    s.poly_shade(cloak, [V[3], V[4], V[4], OCC], ang=0.95, gamma=0.72)
    # A mantle over the shoulders, torn along its lower edge into points - the collar's points again,
    # lower down - standing out past the cloak on his open side and only a little under the raised arm,
    # where the air between sleeve and side has to stay clear.
    for pts in ([(4.0, -48.0), (7.6, -47.4), (10.4, -45.8), (12.6, -43.0), (13.8, -39.6), (14.8, -35.2),
                 (13.4, -37.2), (13.0, -33.6), (11.8, -36.4), (11.2, -34.8), (10.2, -38.0), (6.0, -40.0)],
                [(-4.0, -48.0), (-7.4, -47.4), (-9.6, -45.6), (-11.0, -43.0), (-11.8, -40.2), (-12.6, -36.8),
                 (-11.4, -38.4), (-10.8, -36.0), (-10.0, -39.0), (-6.0, -40.0)]):
        s.poly_shade(P(pts), [V[3], V[4], OCC], ang=0.95, gamma=0.72)
    # The sleeve of the raised arm: a wing of cloth hanging off the forearm, with clear air
    # between it and his side. Joined to the body it read as a stick with a sliver beside it.
    sleeve = P([(-9.6, -46.4), (-12.8, -48.8), (-16.6, -51.8), (-19.6, -53.0), (-20.6, -49.0),
                (-20.4, -43.0), (-20.0, -38.4), (-19.8, -33.4), (-18.8, -36.2), (-17.6, -31.2), (-16.8, -35.2),
                (-16.0, -33.6), (-15.4, -38.6), (-15.0, -41.4), (-12.6, -44.4), (-10.4, -44.0)])
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
    # Closed gaps inside the shape - between the mantle's points and the cloak - filled in, so the rims below
    # light only the true outline. Drawn large (the throne puts him in the foreground, 2026-11-08) the rim round
    # two of those gaps was a pair of violet eyes looking out of his back at us.
    outside = set()
    todo = [(xx, yy) for xx in range(s.w) for yy in (0, s.h - 1)] + [(xx, yy) for yy in range(s.h) for xx in (0, s.w - 1)]
    while todo:
        q = todo.pop()
        if q in outside or s.px[q[0], q[1]][3]:
            continue
        outside.add(q)
        for (ddx, ddy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = q[0] + ddx, q[1] + ddy
            if 0 <= nx < s.w and 0 <= ny < s.h and (nx, ny) not in outside:
                todo.append((nx, ny))
    for yy in range(s.h):
        for xx in range(s.w):
            if not s.px[xx, yy][3] and (xx, yy) not in outside:
                s.set(xx, yy, OCC)
    orb = (hx - 0.2 * u, hy - 4.6 * u)
    shoulder = int(oy - 44 * u)
    edge_light(s, DK[1], (0, -1), box=(0, 0, s.w, shoulder + 2 * u), inner=DK[3])
    edge_light(s, DK[2], (-1, 0), box=(0, 0, ox + 1 * u, oy - 30 * u))
    edge_light(s, DK[2], (1, 0), box=(ox + 3 * u, 0, s.w, shoulder))
    edge_light(s, DK[3], (1, 0), box=(ox + 3 * u, shoulder, s.w, oy - 20 * u))
    # The black sun's last reach, its dimmest stop, down both sides to the knee: without it the folds his
    # sides step out at were lost against the dark ground, and below the waist he was a cone again.
    edge_light(s, DK[3], (1, 0), box=(ox + 3 * u, oy - 20 * u, s.w, oy - 8 * u))
    edge_light(s, DK[3], (-1, 0), box=(0, oy - 30 * u, ox - 3 * u, oy - 8 * u))
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
    a fire in the middle of a warded ring, and round it the four disciplines' staves, each a different element.
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
    # Four of the game's wizards round the fire, each staff burning his own element: the disciplines, the four
    # he takes in the next panel and the Archmage frees in the break. Dee stood behind the fire until Eric,
    # 2026-11-08, of his grey robe and violet light: "Yes, remove him" - he reads as the player, who is never
    # taken. The four stand square round the fire, a quarter turn apart and half a quarter off the way in, each
    # close to where he stood among five, so the ring holds no gap where Dee was. None stands at the front
    # centre, where he would cover the fire.
    for i, (a, who) in enumerate([(0.79, PYRO), (2.36, FROST), (3.93, STORM), (5.50, GEO)]):
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


# HE LIFTS THEM, ONE BY ONE. Eric, 2026-11-08, of the panel as it first was: "'he came here first' it's hard to see
# what he's doing in the background, it should show him lifting each of the wizards into the air to capture them" -
# his silhouette filled the frame and the wizards were dark specks at his feet behind the caption. The first answer
# lifted five of them round his head in their robes' colours; his second pass, the same day: "the animation for 'he
# came here first' should have the silhouette of 4 wizards, cast in shadow, in the background, with the dark wizard in
# the foreground. remove the purple wizard, since he is the wizard that you are, and you do not get captures by him".
#
# So FOUR: the starters the Archmage frees in the break, in the break's order left to right (FREED_FOUR) - never Dee,
# whose grey robe and violet light read as the player. And his third pass: "when the dark wizard lifts the wizards into
# the air, they should still be at the stone circles. you can also show the colored dark silhouettes when he lifts
# them." So they stand IN THE MOOT'S RING, round its cold ward, the ring's far stones standing tall behind them and
# leaning outward, its near ones thrown down flat toward us - the place of panel 1, seen from behind him. Standing,
# they are CAST IN SHADOW against the glow of his sun: occlusion shapes told apart by their outlines - the
# Pyromancer's lantern and cape, the Geomancer's bulk and fork, the Frostweaver's crown and trident, the Stormcaller's
# hair and raised fork - with nothing on them in colour but their own lights. HE is in the FOREGROUND, the nearest
# thing to the camera, between us and them, with his back to us. A silhouette has no front, so the picture says it:
# the black sun is beyond him, behind his head, the only light on him comes from there, and his raised hand reaches up
# into the scene, toward them. Then he takes them one at a time, from the far one to the nearest: his reach leaves
# his hand, a hoop of his violet closes on one - and in that light his own colour comes up on him, dark and shaded,
# the moot's red, green, blue or gold (THRONE_LIFTED) - and lifts him out of the ring into the sky over it, where he
# hangs in a glow of the dark wizard's light while his own light streams back into the hand.
#
# The box is the break's: every frame is the whole mid layer inside it, and the still mid layer is empty there. Every
# edge and size a multiple of 8, so the dissolve's Bayer threshold matches pixel for pixel.
THRONE_BOX = (296, 240, 560, 472)
# Twelve a second, not the other panels' eight: a lift carries a whole figure a hundred art pixels and more, and at
# eight frames a second it went up in jumps the eye could not follow as one rise (rule 5).
THRONE_FPS = 12
# The panel's hold in Prologue.cs (Beats, "throne"); the frames cover it, the last held after.
THRONE_SECONDS = 5.9
THRONE_COLUMNS = 8
# Him: (x, the foot of his hem, height). In the foreground on the right, cut by the frame's foot, his raised hand up
# and to our left over the circle. His black sun is behind his head, an eclipse seen from behind him, at the size
# the moot's panel gave it against him (eleven of his 64ths), so its corona rings his collar of points.
THRONE_HIM = (604, 700, 300)
THRONE_SUN = (606, 437, 52)
# The moot's ring in the middle distance, beyond him and to our left: its centre and radii. Its stones stand round
# this ellipse, the cold ward lies in its middle, and the four stand inside it.
THRONE_RING = (404, 462, 196, 44)
THRONE_WARD = (404, 474, 60, 11)
# Where the ring's far stones stand, by x on its far arc: two to the left of the four, three in the gaps between
# them, none behind his head, where a stone would break the eclipse.
THRONE_FAR_STONES = [228, 274, 353, 403, 453]

# The four, in the order he takes them, far to near: (who, standing in the ring (x, yb), held in the air (x, yb),
# height, facing, the moment the hoop closes). Facing him, on the ward's far side, each a step apart so no two
# outlines touch and merge. The first hoop closes at 1.25 s, so the circle stands whole and still for a beat after
# the dissolve in (his reach, THRONE_REACH, leaves his hand 0.15 s before it); then one every 0.65 s. The last comes
# to rest at 4.8 s, and the dissolve to the tide begins at 5.2.
THRONE_FOUR = [(PYRO, (328, 456), (332, 372), 48, 1, 1.25),
               (GEO, (378, 452), (376, 336), 48, 1, 1.9),
               (FROST, (428, 452), (428, 326), 48, 1, 2.55),
               (STORM, (478, 456), (478, 350), 48, 1, 3.2)]
# Each one's colour once his hoop has him: (the row, the stop he is filled with, the stop his lit side takes). Dark and
# shaded, as Eric asked ("the colored dark silhouettes"), not the moot's flat fill: a stop of his own row whose
# brightness is near the other three's (the red row is the darkest, so the Pyromancer sits two stops above the
# rest), his lit side one stop up toward the hand, his far side let back to occlusion.
THRONE_LIFTED = {"pyromancer": (R, 2, 1), "geomancer": (M["verdant"], 4, 3), "frostweaver": (M["azure"], 4, 3),
                 "stormcaller": (G, 4, 3)}
# Seconds his colour takes to come up once the hoop has closed: half of him at once, all of him after this.
THRONE_COLOUR_IN = 0.12
# The hoop closing: seconds from its flash on the ground to his feet leaving it.
THRONE_GRIP = 0.3
# A lift eases in and out (a smoothstep, steepest at 1.5 times its average) and never climbs faster than this,
# art pixels a second - under ten a frame at THRONE_FPS - nor takes less than THRONE_LIFT_MIN.
THRONE_LIFT = 120.0
THRONE_LIFT_MIN = 1.3
# Seconds a light takes from his staff to the hand.
THRONE_STREAM = 1.5


def _throne_ground(entry):
    """Where a wizard stands in the ring before he is taken: (x, his feet)."""
    return entry[1]


def _lifted_shade(row, stop, lit, toward, held, colour):
    """A wizard's own pixels in the throne, before he is laid down (roster_figure's shade). Standing, cast in shadow:
    occlusion with a rim of violet on the edge that faces his sun. Held, his colour comes up (`colour` 0 to 1, half
    his pixels and then all): his row's `stop`, a band of its `lit` stop three pixels deep behind the violet rim on
    the side toward the hand, and two pixels of occlusion along the far side, so he reads as a turned form and not
    a flat fill."""
    def shade(s, left, top):
        px = s.px
        if held and colour > 0.0:
            dx = 1 if toward[0] > 0.33 else (-1 if toward[0] < -0.33 else 0)
            dy = 1 if toward[1] > 0.33 else (-1 if toward[1] < -0.33 else 0)
            fill, up, occ = rgb(row[stop]), rgb(row[lit]), rgb(OCC)

            def depth(x, y, sx, sy, most):
                for d in range(1, most + 1):
                    qx, qy = x + sx * d, y + sy * d
                    if not (0 <= qx < s.w and 0 <= qy < s.h) or not px[qx, qy][3]:
                        return d
                return most + 1
            paint = []
            for y in range(s.h):
                for x in range(s.w):
                    if not px[x, y][3] or not dith(x + left, y + top, colour):
                        continue
                    if depth(x, y, dx, dy, 4) <= 4:
                        paint.append((x, y, up))
                    elif depth(x, y, -dx, -dy, 2) <= 2:
                        paint.append((x, y, occ))
                    else:
                        paint.append((x, y, fill))
            for (x, y, col) in paint:
                px[x, y] = col
        edge_light(s, V[1], toward)
    return shade


def _throne_lift(entry):
    """When a wizard's feet leave the ground, and how long the lift takes to bring him to where he hangs."""
    g = _throne_ground(entry)
    a, at = entry[2], entry[5]
    dist = math.hypot(a[0] - g[0], a[1] - g[1])
    return at + THRONE_GRIP, max(THRONE_LIFT_MIN, 1.5 * dist / THRONE_LIFT)


def _throne_still():
    """The moment the last of them comes to rest: from then on nothing in the box moves."""
    return max(sum(_throne_lift(e)) for e in THRONE_FOUR)


def _throne_pose(entry, t):
    """(x, yb, u, v) of a wizard at panel time t: u the lift's progress 0-1, v its speed in art px/s."""
    gx, gy = _throne_ground(entry)
    ax, ay = entry[2]
    lift, dur = _throne_lift(entry)
    u = min(1.0, max(0.0, (t - lift) / dur))
    s = u * u * (3.0 - 2.0 * u)
    dist = math.hypot(ax - gx, ay - gy)
    return gx + (ax - gx) * s, gy + (ay - gy) * s, u, 6.0 * u * (1.0 - u) * dist / dur


def _hoop(c, x, y, rx, ry, front, near, far):
    """Half of a hoop of his light round a wizard: the front half (front=True), over him, or the back half,
    under him. One art pixel thick, every fourth pixel of the front a stop brighter."""
    n = int(max(24, (rx + ry) * 3))
    seen = set()
    for i in range(n):
        a = i * 6.28318 / n
        if (math.sin(a) > 0) != front:
            continue
        px_, py_ = int(round(x + math.cos(a) * rx)), int(round(y + math.sin(a) * ry))
        if (px_, py_) in seen:
            continue
        seen.add((px_, py_))
        c.set(px_, py_, near if (front and len(seen) % 4 == 0) else far)


def _staff_light(c, light, h, ramp, level):
    """A wizard's own light at its anchor, `level` 1 as in the moot and less as it is drawn out of him."""
    if not light:
        return
    k = h / CELL_STAND
    ax, ay = light
    if level > 0.7:
        c.radial(ax, ay, 3.6 * k, 3.6 * k, [ramp[3], ramp[3], None])
        c.radial(ax, ay, 1.4 * k, 1.4 * k, [ramp[0], ramp[1], ramp[2], ramp[3]])
    elif level > 0.45:
        c.radial(ax, ay, 2.4 * k, 2.4 * k, [ramp[3], None])
        c.radial(ax, ay, 1.1 * k, 1.1 * k, [ramp[1], ramp[2], ramp[3]])
    else:
        c.disc(ax, ay, 0.8 * k, 0.8 * k, ramp[3])
        c.set(ax, ay, ramp[2])


def _stream(c, src, dst, t0, t, seed, ramp, level, bow):
    """His taking of one wizard's light: motes leaving the staff from t0 on, one after another, each along
    a bowed path into his hand over THRONE_STREAM seconds, fewer as the light runs out of the staff.
    Every random draw is made whether or not a mote shows, so each mote keeps its own path and colour."""
    rng = random.Random(seed)
    n = 36
    for j in range(n):
        lag = j * THRONE_STREAM / n
        side = rng.random() - 0.5
        keep = rng.random()
        ci = ri(rng, 0, 3)
        big = rng.random() < 0.35
        if t - t0 < lag or keep > 0.35 + 0.65 * level:
            continue
        u = ((t - t0 - lag) / THRONE_STREAM) % 1.0
        mx = src[0] + (dst[0] - src[0]) * u
        my = src[1] + (dst[1] - src[1]) * u - math.sin(u * math.pi) * bow
        mx += side * 10 * (1 - u)
        my += side * 6 * (1 - u)
        col = ramp[min(3, ci + (1 if u > 0.6 else 0))]
        c.set(mx, my, col)
        if big and u < 0.45:
            c.set(mx + 1, my, ramp[2])


# His reach: a line of his light from the orb in his hand to the one he takes, for a moment either side of the
# hoop closing, so the eye goes from his hand to the one it lands on - the cause before the effect.
THRONE_REACH = (0.15, 0.2)


def _reach(c, src, dst, t, at):
    """The reach at panel time t: a straight line of his light, the darkness ramp's hot end while it lands and
    its mid after, gone THRONE_REACH[1] after the hoop closes."""
    if not (at - THRONE_REACH[0] <= t < at + THRONE_REACH[1]):
        return
    hot = t < at + 0.08
    n = int(math.hypot(dst[0] - src[0], dst[1] - src[1]))
    for k in range(n):
        x = src[0] + (dst[0] - src[0]) * k / float(n)
        y = src[1] + (dst[1] - src[1]) * k / float(n)
        c.set(x, y + 1, DK[3])
        c.set(x, y, (DK[0] if k % 3 else DK[1]) if hot else (DK[1] if k % 3 else DK[2]))


def _throne_frame(t, him, statics):
    """The mid layer at panel time t, in three passes: (ground, four, front). The ground is the shadows out of
    his hem and the marks the hoops leave in the ring; the four are the wizards - in the ring, in his hoop,
    rising, or hung in the sky with their light streaming to his hand - and his grip on them; the front is him,
    the nearest thing to the camera, the orb in his hand and his reach out of it. p_throne darkens the ground
    and the front by the panel's vignette and the four by none, and lays them in that order."""
    c = Cv(FW, FH)
    four = Cv(FW, FH)
    front = Cv(FW, FH)
    over(c, statics)
    ox, oy, u = him["orb"]
    poses = []
    for i, e in enumerate(THRONE_FOUR):
        x, y, lu, v = _throne_pose(e, t)
        # How much of his light is left in him: all of it until the hoop closes, then drawn out of him - to half,
        # which still burns in its own colour, so each is still known by his light when the panel ends.
        level = 1.0 - 0.5 * min(1.0, max(0.0, (t - e[5] - 0.3) / 2.4))
        poses.append((i, e, x, y, lu, v, level))
    lights = {}
    for (i, e, x, y, lu, v, level) in poses:
        who, h, f, at = e[0], e[3], e[4], e[5]
        lights[i] = light_at(who, x, y, h, "idle" if t < at else "death", 0, f)
        # The ground where each was taken: the mark the hoop left closing.
        if t >= at:
            gx, gy = _throne_ground(e)
            _hoop(c, gx, gy + 1, 0.42 * h, 0.1 * h, True, V[3], V[3])
            _hoop(c, gx, gy + 1, 0.42 * h, 0.1 * h, False, V[3], V[3])
    # His grip, behind all four so no glow is ever laid over a wizard: a glow of his light behind each one he holds -
    # the dark light that is his alone, on the only figures he has ever lit with it.
    for (i, e, x, y, lu, v, level) in poses:
        h, at = e[3], e[5]
        if t >= at:
            four.radial(x, y - 0.5 * h, 0.66 * h, 0.72 * h, [DK[3], DK[3], DK[3], V[3], None])
    for (i, e, x, y, lu, v, level) in poses:
        who, h, f, at = e[0], e[3], e[4], e[5]
        held = t >= at
        cyw = y - 0.5 * h
        if held:
            # The hoop: it flashes on the ground round his feet, climbs to his waist and takes him up.
            grip = min(1.0, (t - at) / THRONE_GRIP)
            hy_ = (y + 1) + (y - 0.42 * h - (y + 1)) * (grip * grip * (3 - 2 * grip))
            rx, ry = 0.5 * h * (1.3 - 0.3 * grip), 0.12 * h
            flash = t - at < 0.18
            _hoop(four, x, hy_, rx, ry, False, DK[1], DK[2])
        if held and lu < 1.0 and v > 20.0:
            # The rise said under him as well as by the figure (rule 1's speed lines, in his light): streaks
            # falling away under the feet, longer the faster he goes.
            ln = min(22.0, v * 0.16)
            for k, dx in enumerate((-0.22, 0.0, 0.2)):
                sx_ = x + dx * h
                for d in range(2, int(ln), 2):
                    if (d // 2 + k) % 3 == 0:
                        continue
                    four.set(sx_, y + d, DK[3] if d < ln * 0.5 else V[3])
        # Standing, cast in shadow, his outline caught in violet on the edge that faces his sun beyond the ring; once
        # the hoop has him, his own colour comes up in its light, dark and shaded, lit toward the hand (a stop above
        # the sky band, the rim, or it is lost against it).
        to = ((ox - x), (oy - cyw)) if held else ((THRONE_SUN[0] - x), (THRONE_SUN[1] - 40 - cyw))
        n = math.hypot(*to) or 1.0
        row, stop, lit = THRONE_LIFTED[who[0]]
        colour = 0.0 if not held else (0.5 if t - at < THRONE_COLOUR_IN else 1.0)
        roster_figure(four, who, x, y, h, [OCC, OCC], strip="death" if held else "idle", frame=0, facing=f,
                      lit=False, contact=not held or lu <= 0.0,
                      shade=_lifted_shade(row, stop, lit, (to[0] / n, to[1] / n), held, colour))
        if held:
            for dy in (0, 1):
                _hoop(four, x, hy_ + dy, rx, ry, True, DK[0] if flash else DK[1], DK[1] if flash else DK[2])
        _staff_light(four, lights[i], h, E[who[2]], level)
        if t >= at + 0.15:
            _stream(four, lights[i], (ox, oy), at + 0.15, t, 510 + i, E[who[2]], level, 22 + i * 6)
    over(front, him["body"])
    taken = sum(1 for (i, e, x, y, lu, v, level) in poses if t >= e[5] + 0.15 + THRONE_STREAM)
    black_sun(front, ox, oy, 1.7 * u * (1.0 + 0.16 * taken))
    for (i, e, x, y, lu, v, level) in poses:
        gx, gy = _throne_ground(e)
        _reach(front, (ox, oy), (gx, gy - 0.1 * e[3]), t, e[5])
    return c, four, front


def p_throne():
    """2. "He came here first." The moot's circle, its fire out, and over it his first full look: a silhouette in the
    foreground with his back to us, the only figure in the game that sheds dark light, his black sun behind his head.
    Beyond him, in the moot's ring of stones, the four disciplines cast in shadow against the sky - until he lifts
    them out of it one after another (animated: THRONE_BOX), each in a hoop of his violet that brings his own colour
    up on him, dark and shaded, and draws their light into his hand."""
    back, mid, fore = frame(True), frame(), frame()
    sky(back, 0, 470, [OCC, OCC, S[4], V[4], V[3]])
    stars(back, (0, 0, FW, 320), 160, 4, colors=[S[0], V[1], V[0]])
    sun_x, sun_y, sun_r = THRONE_SUN
    # The sky over the circle lit by his sun, a band of violet pigment the four stand against: without it they were
    # occlusion on the night.
    back.radial(sun_x - 150, 440, 330, 130, [V[2], V[2], V[3], V[3], V[4], None])
    black_sun(back, sun_x, sun_y, sun_r, rays=40, seed=5)
    # The heath beyond the ring, and the ring's ground. The land falls away behind him, so his sun is not cut to a
    # dome by it and its corona still rings his head.
    far_land = ridge_fn(420, 7, 11, 0.9)
    hills(back, lambda x: far_land(x) + max(0.0, x - 500) * 0.55, [V[4], S[4], S[4]], 50, lit=V[2])
    back.vramp(470, FH - 1, [S[4], S[4], OCC, OCC])
    heath(back, 424, 840, 1800, 6, [(S[4], V[4]), (OCC, V[4]), (OCC, S[4])])
    rcx, rcy, rrx, rry = THRONE_RING
    # The ring's floor, lit by his sun beyond it: violet pigment, cold, where the moot's was gold.
    back.radial(rcx, rcy + 6, rrx + 30, rry + 22, [V[3], V[3], V[4], V[4], None])
    # The ring of standing stones (the moot's, panel 1): the far ones still standing behind the four, leaning outward
    # from him; the near ones thrown down flat, outward toward us. None behind his head, where they would break the
    # eclipse; drawn far to near round the cold ward.
    # The far ones are placed, not spaced round: a stone standing straight behind one of the four ran on out of his
    # outline like a tall hat, so the tall ones stand to the left of them and the rest rise in the gaps between
    # their heads.
    stones = []
    rng = random.Random(707)
    for i in range(12):
        a = i * 6.28318 / 12.0 + 0.13
        if math.sin(a) >= 0:
            stones.append((rcy + math.sin(a) * rry, rcx + math.cos(a) * rrx, False, i, rng.random(), rng.random()))
    for i, x in enumerate(THRONE_FAR_STONES):
        y = rcy - rry * math.sqrt(max(0.0, 1.0 - ((x - rcx) / float(rrx)) ** 2))
        stones.append((y, x, True, 20 + i, rng.random(), rng.random()))
    wx, wy, wrx, wry = THRONE_WARD
    stones.append((wy, None, None, None, 0, 0))
    for (y, x, far, i, r1, r2) in sorted(stones, key=lambda s: s[0]):
        if x is None:
            # The moot's ward, where the fire burned: gone cold, its lines in stone, the fire's ash in the middle.
            ward(back, wx, wy, wrx, wry, A=[S[2], S[3], S[3], S[3]], pool=False)
            back.disc(wx, wy + 1, 11, 3.5, S[4])
            back.disc(wx - 2, wy, 6, 2.2, OCC)
            continue
        out = 1 if x > rcx else -1
        if far:
            # A head taller than the four, as the moot's stones stand over its wizards; the ones in the gaps between
            # them a little less, so they rise between the heads without crowding them.
            tall = (64 + r2 * 16) if x < 310 else (54 + r2 * 8)
            standing_stone(back, x, y, 15 + r1 * 5, tall, 700 + i, lean=out * (0.06 + r1 * 0.1),
                           broken=r2 < 0.3, ramp=[S[2], S[3], S[4], OCC, OCC], toward=(1, -0.5, V[2]))
        else:
            standing_stone(back, x, y, 18 + r1 * 6, 46 + r2 * 12, 700 + i, lean=out * (0.7 + r1 * 0.2),
                           broken=True, ramp=[S[3], S[4], S[4], OCC, OCC], toward=(1, -0.6, V[3]))

    # Him, once: the frames lay him and his orb over the four.
    cx, yb, hh = THRONE_HIM
    body = Cv(FW, FH)
    hx, hy, u = dark_wizard(body, cx, yb, hh)
    him = {"body": body, "orb": (hx, hy, u)}
    # Shadow running out of his hem across the near ground: what he leaves becomes what serves him.
    statics = Cv(FW, FH)
    rng = random.Random(91)
    for i in range(26):
        a = math.pi * (0.04 + 0.92 * rng.random())
        ln = 60 + rng.random() * 200
        x0, y0 = cx + math.cos(a) * 50, yb - 14
        pts = []
        for k in range(24):
            t = k / 23.0
            pts.append((x0 + math.cos(a) * ln * t * 1.6, y0 + math.sin(a) * ln * t * 0.3 + math.sin(t * 7 + i) * 3))
        for k in range(len(pts) - 1):
            wd = 2.6 * (1 - k / 23.0) + 0.6
            sk._stroke(statics, pts[k][0], pts[k][1], pts[k + 1][0], pts[k + 1][1], wd, wd * 0.8, OCC)
        if rng.random() < 0.55:
            ex, ey = pts[-1]
            statics.set(ex, ey - 2, EYE[0])
            statics.set(ex + 2, ey - 2, EYE[1])

    # The ground and he take the panel's vignette, as finish() gives the rest of the mid layer, so they meet the
    # still layer at the box's edge without a seam. The four take none of it: they never leave the middle of the
    # frame, and the vignette's dither at any strength set a lattice of stone dots over flat shapes.
    t_of = frame_t(0.8, CY - 10)
    x0, y0, x1, y1 = THRONE_BOX

    def mid_at(t):
        c, four, front = _throne_frame(t, him, statics)
        darken(c, t_of, [S[4], OCC], box=THRONE_BOX)
        darken(front, t_of, [S[4], OCC], box=THRONE_BOX)
        over(c, four)
        over(c, front)
        return c

    still = _throne_still()
    frames = []
    for k in range(int(math.ceil(THRONE_SECONDS * THRONE_FPS))):
        # Nothing moves once the last of them hangs where he put him: the frames from there on are one frame,
        # held under the dissolve to the tide.
        t = min((k + 0.5) / THRONE_FPS, still + 0.5 / THRONE_FPS)
        frames.append(mid_at(t).img.crop((x0, y0, x1, y1)))
    # The still mid layer: the first frame's whole scene outside the box, nothing inside it.
    over(mid, mid_at(0.0))
    mid.img.paste((0, 0, 0, 0), (x0, y0, x1, y1))
    mid.px = mid.img.load()
    heath(fore, 660, 840, 600, 19, [(OCC, S[4]), (OCC, OCC)], scale=1.8)
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.8, cy=CY - 10)
    layers["anim"] = frames
    return layers


# THE TOWN OVERRUN, FROM ITS STREET (Eric, 2026-11-10, of the panel as it was - a town at the foot of a hill seen
# from far off, the flood pouring over the crest into it: "when the town is attacked in the opening sequence, i want
# that scene to be different cinematically. the 'camera' should be from within the town, looking up at the houses
# burning, people running, and monsters attacking."). So the camera is down IN the street now, a child's height
# off the cobbles and tilted up: the houses on both sides tower over it, their walls leaning in as they climb (the
# tilt, TIDE_TILT), their jettied storeys stepping out over the street, and they burn - fire out of the windows,
# roofs ablaze against a sky of smoke lit red from under. Its people run down the street toward us, growing as
# they come. Behind them his creatures pour over the rise at the street's end, out of the burning square, and
# along the roofs; one drops off an eave into the street among them - the panel's impact beat (TIDE_LAND), cold
# speed lines, a shake and his violet. Between the roofs at the street's end, small and far, the tower stands
# with its eye burning (Eric, 2026-11-08, "the tower should be in the distant background with the glowing eye").
#
# HOW IT IS BUILT. The street is drawn in world metres through one camera (_sp: a one-point perspective plus the
# keystone of a tilted camera), into materials rather than colours (_Scene: a material, a depth and a shading
# bonus per pixel), and the fire's light (_Light, the burning windows and roofs) decides every colour at the end
# (_resolve). The fire is the land's own light going up, so it is EMISSION only in the flames (the fire ramp, in
# the animated box); what it lands on - plaster, timber, cobbles, the undersides of the smoke - takes pigment, the
# russet and red rows (.ai/title-screen.md, "emissive ramps belong on the emitter"). The depth hides a body that
# comes up behind the street's rise or behind an eave until it clears it, which is what makes the rise a horizon.
# Everything that moves is the animated box (TIDE_BOX): flames, smoke out of the windows, embers, the flood, the
# roof creatures, the one that drops and the people; the still layers hold the street. Back and mid drift as one,
# so nothing slides on the street; the nearest two houses are the fore as well, and drift further, so the slow
# tilt up (Prologue.cs, Beats) opens them past the rest - the camera looking up.
TIDE_BOX = (288, 176, 672, 584)
TIDE_FPS = 8
# The panel's hold in Prologue.cs (Beats, "tide"); the frames cover it, the last held after.
TIDE_SECONDS = 5.6
TIDE_COLUMNS = 5      # 45 frames in nine full rows, nothing wasted (about 28 MB while the panel is up)
# The one that drops off the eave lands at this, and the panel's impact beat (Prologue.cs) fires its cold speed
# lines, shake and flash at TIDE_LAND_ART: the burning street for a breath first, then the flood over the rise, then
# the drop.
TIDE_LAND = 2.0
TIDE_HZ = 500.0         # the horizon row: low, because the camera is low and looks up
TIDE_VPX = 480.0        # the street's vanishing point
TIDE_F = 520.0          # art px per metre at a metre
TIDE_CAM = 0.55         # the camera's height over the cobbles, metres
TIDE_TILT = -760.0      # the row the walls' verticals meet at, far above the frame: the camera tilted up
TIDE_HALF = 3.6         # half the street's width, metres
TIDE_JETTY = 0.38       # each storey over the ground steps out over the street this far
TIDE_CREST = (27.0, 0.8)  # the street's rise (depth, height): past it the street falls away into the square
TIDE_SQUARE = 34.0      # where the street's houses end
TIDE_FAR = 52.0         # the square's far side, burning
TIDE_SMOKE_Z = 1e8
TIDE_SKY_Z = 1e9
RU = M["russet"]
FI = E["fire"]


def _tk(y):
    """The tilted camera's keystone at row y: 1 at the horizon, less above it, so walls lean in as they climb."""
    return (y - TIDE_TILT) / (TIDE_HZ - TIDE_TILT)


def _sp(X, Y, d):
    """A street point (X across, Y up, d away, metres) on the frame."""
    s = TIDE_F / d
    y = TIDE_HZ + (TIDE_CAM - Y) * s
    return (TIDE_VPX + X * s * _tk(y), y)


def _ground(d):
    """The street's height at depth d: up to its rise, and falling away into the square past it."""
    cd, ch = TIDE_CREST
    if d <= cd:
        u = d / cd
        return ch * u * u
    return ch - (d - cd) * 0.16


def _plane_z(X):
    """Depth across a wall standing at X, from where a pixel of it falls on the frame."""
    def z(x, y):
        dx = x - TIDE_VPX
        if dx * X <= 0.5:
            return 900.0
        return X * TIDE_F * _tk(y) / dx
    return z


# The materials the street is drawn in, and the rows they climb as the fire's light on them rises (darkest first).
T_SKY, T_SMOKE, T_PLASTER, T_TIMBER, T_SOFFIT, T_ROOF, T_WINDOW, T_COBBLE, T_GAP, T_LAMP, T_HEARTH, T_FAR, \
    T_END = range(1, 14)
TIDE_RAMPS = {
    T_SKY: [OCC, S[4], V[4], V[3], R[3], R[2], R[1], R[0]],
    T_SMOKE: [OCC, S[4], S[3], V[4], R[3], R[2], R[1]],
    T_PLASTER: [S[4], S[3], S[2], RU[4], RU[3], RU[2], RU[1], RU[0]],
    T_END: [OCC, S[4], S[3], RU[4], RU[3], RU[2]],
    T_TIMBER: [OCC, OCC, S[4], RU[4], RU[3], RU[2]],
    T_SOFFIT: [OCC, S[4], RU[4], RU[3], RU[2], RU[1]],
    T_ROOF: [OCC, S[4], S[3], RU[4], RU[3]],
    T_WINDOW: [OCC, OCC, S[4], RU[4]],
    T_COBBLE: [S[4], S[3], S[2], RU[4], RU[3], RU[2], RU[1]],
    T_GAP: [OCC, OCC, S[4], RU[4], RU[3]],
    T_FAR: [OCC, S[4], R[3], R[2], RU[3], RU[2]],
    T_LAMP: [G[4], G[3], G[2]],
    T_HEARTH: [FI[3], FI[3], FI[2], FI[1]],
}
# Stops of light every material has before the fire adds any (the night's own), and how much of the fire's light
# it takes: smoke swallows it, the undersides of the jetties over the burning street drink it in.
TIDE_BASE = {T_SKY: 0.0, T_SMOKE: 0.2, T_PLASTER: 0.9, T_END: 0.5, T_TIMBER: 0.5, T_SOFFIT: 0.4, T_ROOF: 0.5,
             T_WINDOW: 0.2, T_COBBLE: 0.6, T_GAP: 0.3, T_FAR: 0.4, T_LAMP: 0.6, T_HEARTH: 0.4}
TIDE_GAIN = {T_SKY: 0.6, T_SMOKE: 0.42, T_PLASTER: 1.0, T_END: 0.7, T_TIMBER: 0.55, T_SOFFIT: 1.1, T_ROOF: 0.45,
             T_WINDOW: 0.3, T_COBBLE: 0.75, T_GAP: 0.45, T_FAR: 0.8, T_LAMP: 0.2, T_HEARTH: 0.5}


class _Scene(object):
    """The street as materials: a material id, a depth in metres and a shading bonus (sixteenths of a stop) per
    pixel, depth-tested, so draws can come in any order and what is nearer wins."""

    def __init__(self):
        self.mat = Image.new("L", (FW, FH), 0)
        self.mp = self.mat.load()
        self.bon = Image.new("L", (FW, FH), 128)
        self.bp = self.bon.load()
        self.zb = [TIDE_SKY_Z * 2] * (FW * FH)

    def put(self, x, y, m, z, b=0):
        x = int(x)
        y = int(y)
        if 0 <= x < FW and 0 <= y < FH:
            i = y * FW + x
            if z <= self.zb[i]:
                self.zb[i] = z
                self.mp[x, y] = m
                self.bp[x, y] = max(0, min(255, 128 + int(b)))

    def poly(self, pts, m, z, b=0):
        """Scanline fill: material m at depth z (metres, or z(x, y)), shading b (or b(x, y))."""
        ys = [p[1] for p in pts]
        n = len(pts)
        zf = z if callable(z) else None
        bf = b if callable(b) else None
        for y in range(max(0, int(min(ys))), min(FH, int(max(ys)) + 1)):
            xs = []
            for i in range(n):
                ax, ay = pts[i]
                bx, by = pts[(i + 1) % n]
                if ay == by:
                    continue
                if min(ay, by) <= y < max(ay, by):
                    xs.append(ax + (y - ay) * (bx - ax) / float(by - ay))
            xs.sort()
            for i in range(0, len(xs) - 1, 2):
                for x in range(max(0, int(math.floor(xs[i]))), min(FW - 1, int(math.ceil(xs[i + 1]))) + 1):
                    self.put(x, y, m, zf(x, y) if zf else z, bf(x, y) if bf else b)

    def line(self, x0, y0, x1, y1, m, z, b=0):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        for k in range(n + 1):
            t = k / float(n)
            x = x0 + (x1 - x0) * t
            y = y0 + (y1 - y0) * t
            self.put(x, y, m, z(x, y) if callable(z) else z, b)

    def disc(self, cx, cy, rx, ry, m, z, b=0):
        for y in range(int(cy - ry), int(cy + ry) + 1):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                nx = (x - cx) / float(rx)
                ny = (y - cy) / float(ry)
                if nx * nx + ny * ny <= 1.0:
                    self.put(x, y, m, z, b(x, y) if callable(b) else b)

    def z_at(self, x, y):
        if 0 <= x < FW and 0 <= y < FH:
            return self.zb[int(y) * FW + int(x)]
        return TIDE_SKY_Z


class _Light(object):
    """The fire's light on the street, in stops, summed from every burning window and roof on a grid a quarter
    the frame's size and read back smoothly (bilinear) at full size."""

    STEP = 4

    def __init__(self):
        self.gw = FW // self.STEP + 2
        self.gh = FH // self.STEP + 2
        self.g = [[0.0] * self.gw for _ in range(self.gh)]
        self.px = None

    def add(self, x, y, rx, ry, strength):
        st = float(self.STEP)
        for gy in range(max(0, int((y - ry) / st)), min(self.gh, int((y + ry) / st) + 2)):
            row = self.g[gy]
            dy = (gy * st - y) / ry
            for gx in range(max(0, int((x - rx) / st)), min(self.gw, int((x + rx) / st) + 2)):
                dx = (gx * st - x) / rx
                q = 1.0 - (dx * dx + dy * dy)
                if q > 0.0:
                    row[gx] += strength * q * q

    def done(self):
        im = Image.new("F", (self.gw, self.gh), 0.0)
        p = im.load()
        for gy in range(self.gh):
            for gx in range(self.gw):
                p[gx, gy] = self.g[gy][gx]
        self.img = im.resize((self.gw * self.STEP, self.gh * self.STEP), Image.BILINEAR)
        self.px = self.img.load()

    def at(self, x, y):
        return self.px[int(x), int(y)]


def _resolve(scene, light, c, box=None, keep=None):
    """Every material pixel of the scene onto c in its row, by the fire's light on it plus its own bonus and base,
    dithered between stops. keep(z): only pixels at depths it accepts."""
    x0, y0, x1, y1 = box or (0, 0, FW, FH)
    mp, bp, zb, lp = scene.mp, scene.bp, scene.zb, light.px
    ramps = dict((m, [rgb(k) for k in r]) for (m, r) in TIDE_RAMPS.items())
    for y in range(y0, y1):
        row = TH[y % 8]
        for x in range(x0, x1):
            m = mp[x, y]
            if not m:
                continue
            if keep is not None and not keep(zb[y * FW + x]):
                continue
            ramp = ramps[m]
            t = lp[x, y] * TIDE_GAIN[m] + (bp[x, y] - 128) / 16.0 + TIDE_BASE[m]
            n = len(ramp)
            if t <= 0.0:
                c.px[x, y] = ramp[0]
                continue
            if t >= n - 1:
                c.px[x, y] = ramp[-1]
                continue
            i = int(t)
            c.px[x, y] = ramp[i + 1] if (t - i) > row[x % 8] else ramp[i]


# THE HOUSES. Each side, near to far: (d0, d1, storeys, top, burn) - from d0 to d1 metres down the street, its
# storeys (each over the ground jettied out over the street), a gable to the street or an eave along it, and how it
# burns: 0 lit only by the fires across the way, 1 a window or two alight downstairs, 2 its windows and its roof, 3
# alight upstairs, over our heads. The two nearest tower over the camera on both sides and are not yet alight; the
# burning starts a door down, where the fire stays inside the animated box. Not every house burns: the dark ones
# between are what the flames read against. Lower toward the square, so the sky opens between the roofs at the end of
# the street and the tower can stand in it.
TIDE_HOUSES = {
    -1: [(2.4, 8.4, 4, "eave", 0), (8.4, 12.8, 3, "gable", 3), (12.8, 17.4, 3, "eave", 1), (17.4, 21.6, 2, "gable", 2),
         (21.6, 26.4, 3, "gable", 0), (26.4, 30.4, 2, "eave", 1), (30.4, 34.0, 2, "gable", 0)],
    1: [(2.4, 7.2, 4, "gable", 0), (7.2, 11.8, 3, "eave", 3), (11.8, 16.2, 2, "eave", 2), (16.2, 21.4, 3, "gable", 0),
        (21.4, 25.4, 2, "eave", 2), (25.4, 30.0, 2, "gable", 1), (30.0, 34.0, 2, "eave", 0)],
}
# How high a fire may reach above its root, at most: kept inside the animated box (its top, inset), or a tall one
# under a high roof would be cut flat by the box's edge.
TIDE_FIRE_TOP = TIDE_BOX[1] + 16


def _fire_fits(x, y, h, w):
    """A fire rooted at (x, y): its tallest lick, cut to what the animated box holds above it, or 0 when it would not
    keep half its height or sits too near the box's sides - then the opening only glows."""
    bx0, by0, bx1, by1 = TIDE_BOX
    if x - w < bx0 + 12 or x + w > bx1 - 12 or y > by1 - 12:
        return 0.0
    room = y - TIDE_FIRE_TOP
    return h if room >= h else (room if room >= h * 0.5 else 0.0)
TIDE_STOREY0 = 3.2
TIDE_STOREY = 2.8


def _storey_y(k):
    return 0.0 if k <= 0 else TIDE_STOREY0 + TIDE_STOREY * (k - 1)


def _house_geo(side, h):
    """A house's numbers: its ground, its storeys' wall planes (X) and floors (Y), its top, and its gable."""
    d0, d1, n, top, burn = h
    base = _ground((d0 + d1) * 0.5)
    planes = [side * (TIDE_HALF - TIDE_JETTY * k) for k in range(n)]
    floors = [base + _storey_y(k) for k in range(n + 1)]
    gable = (d1 - d0) * 0.62 if top == "gable" else 0.0
    return dict(d0=d0, d1=d1, n=n, top=top, burn=burn, base=base, planes=planes, floors=floors, gable=gable,
                dm=(d0 + d1) * 0.5)


def _windows(g, rng):
    """Each storey's openings, in (d from, d to, Y from, Y to, kind): a door and a window on the ground, one or two
    windows above."""
    out = []
    d0, d1 = g["d0"], g["d1"]
    for k in range(g["n"]):
        yb = g["floors"][k]
        span = d1 - d0
        if k == 0:
            dd = d0 + span * (0.22 + rng.random() * 0.1)
            out.append((k, dd, dd + 1.1, yb, yb + 2.3, "door"))
            dw = d0 + span * (0.62 + rng.random() * 0.1)
            out.append((k, dw, dw + 1.0, yb + 1.0, yb + 2.3, "window"))
        else:
            m = 2 if span > 4.4 else 1
            for j in range(m):
                c0 = d0 + span * ((j + 0.5) / m) - 0.5 + (rng.random() - 0.5) * 0.4
                out.append((k, c0, c0 + 1.0, yb + 0.8, yb + 2.1, "window"))
    return out


def _draw_house(sc, side, g, rng, fires, lights, lamps=True):
    """One house into the scene (side -1 left, 1 right): its end wall and roof behind its front, the front storey by
    storey with its timbers, the undersides of its jetties, its openings, its top. Burning openings go into fires
    (the animated box draws their flames) and lights; returns the eave or gable points a creature may stand on."""
    d0, d1, n = g["d0"], g["d1"], g["n"]
    X0, top_plane = g["planes"][0], g["planes"][-1]
    H = g["floors"][n]
    base = g["base"]
    out_x = side * (TIDE_HALF + 9.0)
    # The end wall facing us and the roof over it, out to the side: what shows above a lower nearer neighbour.
    if g["top"] == "gable":
        dm, apex = g["dm"], H + g["gable"]
        sc.poly([_sp(top_plane, H, d0), _sp(top_plane, apex, dm), _sp(out_x, apex, dm), _sp(out_x, H, d0)],
                T_ROOF, (d0 + dm) * 0.5, lambda x, y: ((x + y * 2) % 7 == 0) * -8)
        sc.poly([_sp(X0, base, d0), _sp(top_plane, H, d0), _sp(out_x, H, d0), _sp(out_x, base, d0)], T_END, d0 + 0.05)
    else:
        ridge = H + 3.0
        sc.poly([_sp(X0, base, d0), _sp(top_plane, H, d0), _sp(side * (TIDE_HALF + 4.0), ridge, d0),
                 _sp(out_x, H, d0), _sp(out_x, base, d0)], T_END, d0 + 0.05)
        a, b2 = _sp(top_plane, H, d0), _sp(side * (TIDE_HALF + 4.0), ridge, d0)
        sc.line(a[0], a[1], b2[0], b2[1], T_ROOF, d0, 4)
    # The front, storey by storey: plaster, then the timbers on it.
    for k in range(n):
        X, yb, yt = g["planes"][k], g["floors"][k], g["floors"][k + 1]
        z = _plane_z(X)
        sc.poly([_sp(X, yb, d0), _sp(X, yt, d0), _sp(X, yt, d1), _sp(X, yb, d1)], T_PLASTER, z,
                lambda x, y: (-6 if (x * 7 + y * 3) % 23 == 0 else 0))
        zt = lambda x, y, z=z: z(x, y) - 0.02
        for (Y,) in [(yb + 0.05,), (yt - 0.12,), (yb + (yt - yb) * 0.42,)]:
            a, b2 = _sp(X, Y, d0), _sp(X, Y, d1)
            sc.line(a[0], a[1], b2[0], b2[1], T_TIMBER, zt)
            a, b2 = _sp(X, Y - 0.08, d0), _sp(X, Y - 0.08, d1)
            sc.line(a[0], a[1], b2[0], b2[1], T_TIMBER, zt)
        posts = max(2, int((d1 - d0) / 1.5) + 1)
        for j in range(posts + 1):
            dd = d0 + (d1 - d0) * j / float(posts)
            for w in (0.0, 0.12):
                a, b2 = _sp(X, yb, min(d1, dd + w)), _sp(X, yt, min(d1, dd + w))
                sc.line(a[0], a[1], b2[0], b2[1], T_TIMBER, zt)
            if k and j < posts and (j + k) % 2 == 0:
                e = d0 + (d1 - d0) * (j + 1) / float(posts)
                a, b2 = _sp(X, yb + 0.1, dd), _sp(X, yb + (yt - yb) * 0.42, e)
                sc.line(a[0], a[1], b2[0], b2[1], T_TIMBER, zt)
        # The jetty's underside: the floor of this storey seen from below, between its wall and the one under it.
        if k:
            Xl = g["planes"][k - 1]
            zs = _plane_z((X + Xl) * 0.5)
            sc.poly([_sp(Xl, yb, d0), _sp(X, yb, d0), _sp(X, yb, d1), _sp(Xl, yb, d1)], T_SOFFIT, zs,
                    lambda x, y: 8 if (x + y) % 5 == 0 else 0)
    # The top: a gable to the street or the eave along it, its edges in timber.
    zt = _plane_z(top_plane)
    perches = []
    if g["top"] == "gable":
        dm, apex = g["dm"], H + g["gable"]
        pts = [_sp(top_plane, H, d0), _sp(top_plane, apex, dm), _sp(top_plane, H, d1)]
        sc.poly(pts, T_PLASTER, zt, -4)
        for (a, b2) in [(pts[0], pts[1]), (pts[1], pts[2])]:
            for w in (0, 1, 2):
                sc.line(a[0] + side * w, a[1], b2[0] + side * w, b2[1], T_ROOF, lambda x, y: zt(x, y) - 0.03)
        a, b2 = _sp(top_plane, H, d0), _sp(top_plane, H, d1)
        sc.line(a[0], a[1], b2[0], b2[1], T_TIMBER, lambda x, y: zt(x, y) - 0.02)
        a, b2 = _sp(top_plane, H, dm), _sp(top_plane, apex - 0.3, dm)
        sc.line(a[0], a[1], b2[0], b2[1], T_TIMBER, lambda x, y: zt(x, y) - 0.02)
        # On the ridge a step back from the gable's peak (the ridge runs out from it, level, away from the street):
        # the near slope of the roof hides a body coming up behind it, as the gable alone, seen this edge-on, would not.
        perches.append((top_plane + side * 1.1, apex, dm, "apex"))
        roofline = [(top_plane, H + g["gable"] * (1 - abs(2 * (dd - d0) / (d1 - d0) - 1)), dd)
                    for dd in [d0 + (d1 - d0) * (j + 0.5) / 6.0 for j in range(6)]]
    else:
        # the eave: its fascia, and the underside of its overhang lit from the street
        Xo = top_plane - side * 0.55
        sc.poly([_sp(top_plane, H, d0), _sp(Xo, H, d0), _sp(Xo, H, d1), _sp(top_plane, H, d1)], T_SOFFIT,
                _plane_z((top_plane + Xo) * 0.5), 10)
        sc.poly([_sp(Xo, H, d0), _sp(Xo, H + 0.4, d0), _sp(Xo, H + 0.4, d1), _sp(Xo, H, d1)], T_ROOF, _plane_z(Xo))
        for j in range(3):
            dd = d0 + (d1 - d0) * (0.25 + 0.25 * j)
            perches.append((Xo, H + 0.4, dd, "eave"))
        roofline = [(Xo, H + 0.4, d0 + (d1 - d0) * (j + 0.5) / 6.0) for j in range(6)]
        # a chimney stack on the wall's head
        if rng.random() < 0.6:
            dc = d0 + (d1 - d0) * (0.2 + rng.random() * 0.5)
            zc = _plane_z(Xo)
            sc.poly([_sp(Xo, H, dc), _sp(Xo, H + 2.0, dc), _sp(Xo, H + 2.0, dc + 0.9), _sp(Xo, H, dc + 0.9)],
                    T_END, lambda x, y: zc(x, y) - 0.05)
            sc.poly([_sp(Xo, H + 2.0, dc - 0.1), _sp(Xo, H + 2.3, dc - 0.1), _sp(Xo, H + 2.3, dc + 1.0),
                     _sp(Xo, H + 2.0, dc + 1.0)], T_ROOF, lambda x, y: zc(x, y) - 0.06)
    # The openings: dark, lamp-lit (the town's own light, not yet out) or alight inside.
    opens = _windows(g, rng)
    burning = []
    if g["burn"] == 1:
        burning = [o for o in opens if o[0] <= 1 and rng.random() < 0.5]
        if not burning:
            burning = [opens[1]]
    elif g["burn"] == 3:
        # alight upstairs: the fire has climbed, over our heads
        burning = [o for o in opens if o[0] >= 1 and rng.random() < 0.7]
    elif g["burn"] == 2:
        burning = [o for o in opens if rng.random() < 0.6]
    # Lamps only far down the street: near, at this angle, a lit window was a flat yellow sliver.
    lamps = lamps and d0 >= 16.0
    for o in opens:
        k, a0, a1, y0, y1, kind = o
        X = g["planes"][k]
        z = _plane_z(X)
        zo = lambda x, y, z=z: z(x, y) - 0.04
        lamp = rng.random() < 0.18
        fh = 0.0
        if o in burning:
            tx, ty = _sp(X, y1, (a0 + a1) * 0.5)
            s = TIDE_F / ((a0 + a1) * 0.5)
            fh = _fire_fits(tx, ty, s * (1.0 + rng.random() * 0.7), max(3.0, s * 0.5))
        # Alight inside only where its flames can be drawn: an opening at the box's edge glowing with no fire coming
        # out of it read as a lit panel.
        fire = fh > 0.0
        if fire:
            mat = T_HEARTH
        elif kind == "window" and lamps and lamp:
            mat = T_LAMP
        else:
            mat = T_WINDOW
        sc.poly([_sp(X, y0, a0), _sp(X, y1, a0), _sp(X, y1, a1), _sp(X, y0, a1)], mat, zo)
        # the frame round it: a lintel and a sill
        for (yy,) in [(y0,), (y1,)]:
            a, b2 = _sp(X, yy, a0 - 0.1), _sp(X, yy, a1 + 0.1)
            sc.line(a[0], a[1], b2[0], b2[1], T_TIMBER, lambda x, y, z=z: z(x, y) - 0.05)
        if fire:
            # the flames come out of its head and climb; the light it throws on the street and the walls
            tx, ty = _sp(X, y1, (a0 + a1) * 0.5)
            ox, _oy = _sp(X - side * 0.9, y1, (a0 + a1) * 0.5)
            s = TIDE_F / ((a0 + a1) * 0.5)
            fires.append(dict(x=tx, y=ty, out=ox - tx, h=fh, w=max(3.0, s * 0.5), kind="window",
                              seed=ri(rng, 0, 1 << 20), bottom=_sp(X, y0, (a0 + a1) * 0.5)[1]))
            cx, cy = _sp(X - side * 1.2, y1, (a0 + a1) * 0.5)
            lights.append((cx, cy, s * 2.8, s * 2.6, 2.4))
    if g["burn"] == 2:
        # The roof ablaze: a few great crowns of fire along it, not a curtain - each its own height, gaps between.
        s = TIDE_F / g["dm"]
        for j, (Xr, Yr, dr) in enumerate(roofline):
            if j % 2 and rng.random() < 0.6:
                continue
            x, y = _sp(Xr, Yr, dr)
            fh = _fire_fits(x, y, s * (1.4 + rng.random() * 2.2), s * 0.8)
            if fh:
                fires.append(dict(x=x, y=y, out=0.0, h=fh, w=s * (0.7 + rng.random() * 0.5), kind="roof",
                                  seed=ri(rng, 0, 1 << 20), bottom=y))
        x, y = _sp(top_plane, H + g["gable"] * 0.5, g["dm"])
        lights.append((x, y, s * 4.6, s * 4.0, 2.4))
    return perches


def _street(sc, rng):
    """The cobbles, from under the camera to the rise: stone by stone where they are big enough to be stones, a
    grain past that, with the gutter down the middle."""
    # Depth of the street at each row up to its rise.
    rows = {}
    d = 0.6
    while d <= TIDE_CREST[0]:
        y = int(_sp(0.0, _ground(d), d)[1])
        if y not in rows:
            rows[y] = d
        d += 0.005
    ys = sorted(rows)
    crest_y = ys[0]
    last = rows[ys[-1]]
    for y in range(crest_y, FH):
        dz = rows.get(y)
        if dz is None:
            dz = last if y > ys[-1] else min((rows[k] for k in ys if k >= y), key=lambda v: v)
        last = dz
        for x in range(FW):
            sc.put(x, y, T_GAP, dz, ((x * 13 + y * 7) % 11 == 0) * 10)
    # The stones: rows across the street, each stone its own size and its corners worn round, flattened by the
    # angle; a few missing, a few sunk.
    d = 0.7
    row = 0
    while d < 15.0:
        ln = (0.22 + d * 0.004) * (0.85 + rng.random() * 0.3)
        X = -TIDE_HALF - 1.0 - rng.random() * 0.3
        while X < TIDE_HALF + 1.0:
            wd = 0.22 + rng.random() * 0.16
            if rng.random() < 0.05:
                X += wd
                continue
            gx = 0.025 + d * 0.0015
            cn = min(wd, ln) * (0.18 + rng.random() * 0.22)
            a0, a1 = X + gx, X + wd - gx
            e0, e1 = d + gx, d + ln - gx
            loc = [(a0 + cn, e0), (a1 - cn, e0), (a1, e0 + cn), (a1, e1 - cn), (a1 - cn, e1), (a0 + cn, e1),
                   (a0, e1 - cn), (a0, e0 + cn)]
            # every corner knocked about a little: worn setts, not tiles
            jw, jl = wd * 0.17, ln * 0.17
            loc = [(a + (rng.random() - 0.5) * jw * 2, e + (rng.random() - 0.5) * jl * 2) for (a, e) in loc]
            pts = [_sp(a, _ground(e), e) for (a, e) in loc]
            lift = int((rng.random() - 0.5) * 12) + (5 if abs(X + wd * 0.5) < 0.35 else 0)
            sc.poly(pts, T_COBBLE, d, lift)
            # the stone's far edge caught by the fire up the street
            a, b2 = pts[5], pts[4]
            if abs(b2[0] - a[0]) > 4:
                sc.line(a[0], a[1], b2[0], b2[1], T_COBBLE, d - 0.001, lift + 14)
            X += wd
        d += ln
        row += 1
    # past the stones: a grain of them
    for y in range(crest_y, int(_sp(0.0, _ground(15.0), 15.0)[1]) + 1):
        for x in range(FW):
            hsh = ((x * 73856093) ^ (y * 19349663) ^ (x * y * 83492791)) & 0xFFFF
            if hsh % 3 == 0:
                sc.put(x, y, T_COBBLE, rows.get(y, 20.0), (hsh >> 4) % 9 - 2)
    return crest_y


def _far_side(sc, fires, lights, rng):
    """The square's far side, burning: house fronts facing us across it, their bases under the street's rise."""
    x = -15.0
    while x < 15.0:
        w = 3.6 + rng.random() * 2.6
        H = 5.0 + rng.random() * 4.0
        gb = 2.0 + rng.random() * 1.6 if rng.random() < 0.6 else 0.0
        g0 = _ground(TIDE_FAR)
        pts = [_sp(x, g0, TIDE_FAR), _sp(x, g0 + H, TIDE_FAR)]
        if gb:
            pts.append(_sp(x + w * 0.5, g0 + H + gb, TIDE_FAR))
        pts += [_sp(x + w, g0 + H, TIDE_FAR), _sp(x + w, g0, TIDE_FAR)]
        sc.poly(pts, T_FAR, TIDE_FAR + rng.random())
        for j in range(int(w / 1.6)):
            wx = x + 0.6 + j * 1.6
            for (y0, y1) in [(g0 + 3.6, g0 + 4.8), (g0 + 6.4, g0 + 7.4)]:
                if y1 < g0 + H - 0.3:
                    lit = rng.random() < 0.5
                    sc.poly([_sp(wx, y0, TIDE_FAR), _sp(wx, y1, TIDE_FAR), _sp(wx + 0.7, y1, TIDE_FAR),
                             _sp(wx + 0.7, y0, TIDE_FAR)], T_HEARTH if lit else T_WINDOW, TIDE_FAR - 0.5)
        top = _sp(x + w * 0.5, g0 + H + gb, TIDE_FAR)
        s = TIDE_F / TIDE_FAR
        # The square burns hardest: behind the rise the flood comes over, so its bodies come over it black against
        # fire.
        for j in range(3):
            u = 0.17 + 0.33 * j + (rng.random() - 0.5) * 0.14
            fx, fy = _sp(x + w * u, g0 + H + (gb * (1 - abs(u - 0.5) * 2)), TIDE_FAR)
            fh = _fire_fits(fx, fy, s * (2.4 + rng.random() * 3.2), s * 1.2)
            if fh and rng.random() < 0.9:
                fires.append(dict(x=fx, y=fy, out=0.0, h=fh, w=s * (1.0 + rng.random() * 0.6), kind="far",
                                  seed=ri(rng, 0, 1 << 20), bottom=fy))
        lights.append((top[0], top[1] + 10, 70, 46, 2.2))
        x += w + 0.3


def _smoke(sc, x0, y0, r0, n, lean, seed, z=TIDE_SMOKE_Z):
    """A column of smoke off a fire: billows rising, growing and leaning down the wind, each lit from under (by the
    fire it rose from, through the light) and dark on top, drawn upward so each one's lit belly lies over the dark
    crown of the one below."""
    rng = random.Random(seed)
    x, y, r = x0, y0, r0
    for k in range(n):
        rr = r * (0.8 + rng.random() * 0.4)
        cy = y

        def bon(px_, py_, cy=cy, rr=rr):
            return int(((py_ - cy) / rr) * 14) - 6
        sc.disc(x + (rng.random() - 0.5) * r * 0.6, cy, rr, rr * 0.82, T_SMOKE, z - k * 10, bon)
        y -= r * (0.7 + rng.random() * 0.25)
        x += lean * r * (0.6 + rng.random() * 0.5)
        r *= 1.09 + rng.random() * 0.06


# HIS CREATURES ON THE ROOFS: where each comes up from behind an eave or a gable and what it does there. (side, the
# house's index on that side, which perch of its top, kind, seconds it starts to rise, and what it does: "watch",
# "crawl" along the eave toward us, or "leap" - the one that drops into the street at TIDE_LAND). They come up behind
# the roof - the eave hides them until they clear it - just before the first heads over the rise, and stand among
# the roofs' flames, black against them; another comes up on the eave the first one leapt from.
TIDE_ROOFERS = [(1, 4, 1, "bruiser", 0.95, "leap"), (1, 5, 0, "caster", 1.1, "watch"),
                (-1, 4, 0, "runner", 1.3, "watch"), (-1, 3, 0, "swarmer", 1.6, "watch"),
                (-1, 5, 2, "hound", 2.3, "crawl"), (1, 4, 0, "runner", 2.4, "watch"),
                (1, 3, 0, "swarmer", 2.7, "watch"), (1, 6, 2, "runner", 3.2, "crawl")]
TIDE_RISE_S = 0.4      # seconds a body takes to come up behind a roof
TIDE_CROUCH = 1.36     # the leaper on its haunches at the eave's edge, from this to its leap
TIDE_LEAP = TIDE_LAND - 0.42
# Each kind's height on the street, in its figure's units of a metre (a body stands about 20 of them).
TIDE_TALL = {"swarmer": 1.7, "runner": 1.75, "bruiser": 1.95, "shielder": 1.85, "caster": 1.75, "knight": 1.85,
             "hound": 1.6}

_TIDE_CACHE = {}


def _tide_fig(kind, h, seed, facing, light, rim=None, snap=False):
    """One of his, drawn once per (kind, size, seed, facing, light) and pasted wherever it stands (creature_fig).
    snap: the flood's bodies grow every frame they come on, so their sizes are taken in steps of about a twelfth -
    a step the eye does not see at 8 frames a second - or the 45 frames would draw thousands of them."""
    hi = max(6, int(round(h)))
    if snap and hi > 14:
        q_ = max(1, hi // 12)
        hi = int(round(hi / float(q_))) * q_
    nrm = math.hypot(light[0], light[1]) or 1.0
    q = (0 if abs(light[0]) < nrm * 0.33 else (1 if light[0] > 0 else -1),
         0 if abs(light[1]) < nrm * 0.33 else (1 if light[1] > 0 else -1))
    key = (kind, hi, seed % 5, facing, q, rim)
    F = _TIDE_CACHE.get(key)
    if F is None:
        F = _TIDE_CACHE[key] = creature_fig(kind, hi, facing, [OCC, S[4]], rim, True, seed % 5, q)
    return F


def _paste_clipped(c, img, ox, oy, sc, z):
    """A figure's image onto the frame where nothing in the street stands nearer than z: what makes the rise and the
    eaves hide a body until it clears them."""
    px = img.load()
    cp = c.px
    zb = sc.zb
    w, h = img.size
    for yy in range(h):
        fy = oy + yy
        if fy < 0 or fy >= FH:
            continue
        base = fy * FW
        for xx in range(w):
            p = px[xx, yy]
            if not p[3]:
                continue
            fx = ox + xx
            if 0 <= fx < FW and zb[base + fx] > z:
                cp[fx, fy] = p


def _tongue(c, x, yb, w, h, t, ph, lean=0.0):
    """One tongue of fire standing on (x, yb), w wide at its root and h tall, licking with t: four nested bands of
    the fire ramp, the edge outermost and a thin white-hot core at its root (.ai/art-direction.md, "Elements are
    emissive")."""
    for (wf, hf, col) in [(1.0, 1.0, FI[3]), (0.72, 0.8, FI[2]), (0.46, 0.52, FI[1]), (0.2, 0.26, FI[0])]:
        hh = h * hf
        if hh < 1.5 or w * wf < 1.0:
            c.set(x, yb - 1, col)
            continue
        m = max(3, int(hh / 3.0))
        left, right = [], []
        for i in range(m + 1):
            uu = i / float(m)
            u = uu * hf
            prof = (1.0 - uu) ** 0.65 * (0.8 + 0.35 * math.sin(uu * 3.1416))
            r = max(0.5, w * 0.5 * wf * prof)
            sway = math.sin(u * 4.2 - t * 9.0 + ph) * w * 0.32 * u + lean * u
            cx, cy = x + sway, yb - h * u
            left.append((cx - r, cy))
            right.append((cx + r, cy))
        # a ragged root, where it comes out of the window or the roof
        c.poly(left + right[::-1] + [(x + w * 0.5 * wf, yb + 1), (x - w * 0.5 * wf, yb + 1)], col)


def _flames(c, f, t, fr, box):
    """A fire's tongues at time t: a window's lick out of its head and up the wall, a roof's row along the roof, the
    far side's crowns - their heights breathing on their own clocks, a little jitter each frame, and never past the
    animated box (inset), whose edge would cut them."""
    rng = random.Random(f["seed"])
    bx0, by0, bx1, by1 = box
    n = 2
    for i in range(n):
        ph = rng.random() * 6.283
        om = 5.0 + rng.random() * 4.0
        u = (i + 0.5) / n
        x = f["x"] + f["out"] * (0.25 + 0.75 * u) + (rng.random() - 0.5) * f["w"] * 0.6
        y = f["y"] + (rng.random() - 0.3) * 2.0
        # the second tongue of a pair the smaller, so a fire is a crown and its lick, not two equal posts
        tall = 1.0 if i == 0 else 0.55 + rng.random() * 0.25
        h = f["h"] * tall * (0.6 + 0.4 * abs(math.sin(t * om * 0.5 + ph))) * (0.9 + 0.1 * fr.random())
        w = f["w"] * (0.7 + rng.random() * 0.5) * (1.0 if i == 0 else 0.8)
        h = min(h, f["h"], y - (by0 + 10))
        if h < 2 or x - w < bx0 + 8 or x + w > bx1 - 8:
            continue
        _tongue(c, x, y, w, h, t, ph, lean=f["out"] * 0.25)


def _fleer(c, x, yb, h, kind, stride, facing, rim):
    """One of the town's people running from it, toward us and to one side: a dark figure leaning into the run, in
    one of two strides (legs apart, or passing), the fire behind caught along the back and the crown of the head,
    one pixel. kind: "man", "woman" (skirt, hair flying), "cloak", "pack", "arms" (hands up), "carry" (a child in
    the arms), "lead" (an arm back to the child it pulls along), "child", "led" (a child's hand up to the one who
    leads it). Never a hood, a point or an eye: those are his."""
    child = kind in ("child", "led")
    u = h / 20.0
    W, Hh = int(h * 1.6) + 12, int(h * 1.35) + 12
    s = Cv(W, Hh)
    ox, oy = W // 2, int(h * 1.12) + 6
    f = facing
    bob = 0.5 if stride else -0.3

    def P(a, b):
        return (ox + f * a * u, oy + (b + bob) * u)

    def limb(pts, radii):
        for k in range(len(pts) - 1):
            x0, y0 = P(*pts[k])
            x1, y1 = P(*pts[k + 1])
            sk._stroke(s, x0, y0, x1, y1, max(0.6, radii[k] * u), max(0.6, radii[k + 1] * u), OCC)
    hip = (0.3, -9.6)
    neck = (1.7, -15.9)
    sh = (1.5, -15.2)
    # legs: apart, or one under him and the other coming through
    if stride:
        legs = [[hip, (2.8, -5.4), (4.6, -0.7)], [hip, (-1.3, -4.9), (-4.0, -2.4)]]
        feet = [((4.6, -0.7), 1), ((-4.0, -2.4), -1)]
    else:
        legs = [[hip, (1.0, -5.0), (0.3, -0.4)], [hip, (2.0, -6.2), (-0.6, -3.6)]]
        feet = [((0.3, -0.4), 1), ((-0.6, -3.6), -1)]
    if kind not in ("woman",):
        for lg in legs:
            limb(lg, [1.3, 1.0, 0.75] if not child else [1.2, 1.0, 0.8])
        for ((a, b), d_) in feet:
            limb([(a - 0.3, b + 0.2), (a + 1.2, b + 0.5)], [0.6, 0.45])
    else:
        for ((a, b), d_) in feet:
            limb([(a - 0.6, b - 0.8), (a, b), (a + 1.0, b + 0.4)], [0.6, 0.55, 0.45])
        flap = 1.2 if stride else 0.4
        s.poly([P(1.8, -10.8), P(-1.2, -10.4), P(-3.6 - flap, -2.6), P(-2.0 - flap, -1.6), P(0.2, -2.4),
                P(2.0, -1.6), P(3.8, -2.2)], OCC)
    # the body, leaning into the run, and its head
    limb([hip, (1.0, -12.6), neck], [2.0, 2.2, 1.8])
    hr = 2.5 if child else 1.85
    hx, hy = (2.1, -17.6) if child else (2.4, -18.0)
    x_, y_ = P(hx, hy)
    s.disc(x_, y_, hr * u, hr * 1.05 * u, OCC)
    # arms
    if kind == "arms":
        arms = [[sh, (3.4, -17.8), (4.4, -20.8)], [sh, (0.4, -18.4), (-0.2, -21.2)]]
    elif kind == "carry":
        arms = [[sh, (3.0, -12.8), (4.4, -14.6)], [sh, (2.2, -12.4), (3.8, -13.0)]]
    elif kind == "lead":
        arms = [[sh, (3.4, -12.6), (4.8, -13.8)], [sh, (-1.4, -12.2), (-4.4, -10.6)]]
    elif kind == "led":
        arms = [[sh, (3.0, -14.2), (4.8, -15.8)], [sh, (0.0, -12.6), (-1.0, -10.6)]]
    elif stride:
        arms = [[sh, (3.4, -12.4), (5.0, -13.8)], [sh, (-0.6, -12.6), (-1.6, -10.2)]]
    else:
        arms = [[sh, (2.6, -12.2), (3.8, -10.8)], [sh, (0.6, -12.2), (1.4, -10.0)]]
    for a in arms:
        limb(a, [0.8, 0.7, 0.55])
    if kind == "woman":
        # her hair loose and flying back
        s.poly([P(hx - 0.4, hy - 1.6), P(hx - 3.6, hy - 0.6), P(hx - 5.2, hy + 1.6 + (0.8 if stride else 0)),
                P(hx - 3.2, hy + 1.2), P(hx - 1.2, hy + 1.8)], OCC)
    elif kind == "cloak":
        flap = 1.0 if stride else 0.0
        s.poly([P(1.6, -16.4), P(-0.6, -15.8), P(-5.6, -12.2 - flap), P(-7.6, -8.8 - flap), P(-5.4, -9.6),
                P(-6.2, -6.8), P(-3.4, -8.6), P(-0.8, -11.2)], OCC)
    elif kind == "pack":
        x_, y_ = P(-1.3, -13.4)
        s.disc(x_, y_, 2.6 * u, 2.3 * u, OCC)
        x_, y_ = P(-0.6, -16.0)
        s.disc(x_, y_, 1.2 * u, 0.9 * u, OCC)
    elif kind == "carry":
        # the child held to the chest: its head over the arm, its legs hanging
        x_, y_ = P(4.0, -16.8)
        s.disc(x_, y_, 1.5 * u, 1.55 * u, OCC)
        limb([(3.6, -15.2), (4.0, -12.4)], [1.4, 1.2])
        limb([(3.8, -12.4), (4.8, -10.2)], [0.6, 0.5])
    elif kind == "man":
        # a flat cap: a brim forward, nothing that points up
        x_, y_ = P(hx + 0.2, hy - 1.2)
        s.disc(x_, y_, 2.0 * u, 0.8 * u, OCC)
        limb([(hx + 0.6, hy - 1.0), (hx + 2.6, hy - 0.6)], [0.5, 0.35])
    # The fire behind them: one pixel along the back and the crown, nowhere else.
    rim_light(s, rim, (-f * 0.55, -0.85), cut=0.25)
    over(c, s, int(round(x)) - ox, int(round(yb)) - oy)


# THE TOWN'S PEOPLE, every one already running toward us down the street when the panel opens: (kind, height in
# metres, depth and across where each is when the panel ends, speed m/s, drift across m/s, stride phase) - placed by
# where they finish, and run back up the street to where they start, none of them past the rise. They keep to the
# two sides of the street - along the walls, as people fleeing do - so the middle stays open down to the rise and
# the flood can be seen coming behind them. Pairs share a lane: one leads, one is led. The nearest end a long
# stride short of the camera, big at the frame's foot.
TIDE_PEOPLE = [
    ("cloak", 1.74, 6.8, -2.1, 2.3, -0.08, 0.35), ("carry", 1.70, 9.0, -2.6, 2.0, -0.06, 0.25),
    ("lead", 1.68, 11.5, -1.9, 2.2, -0.05, 0.1), ("led", 1.12, 11.6, -1.25, 2.2, -0.05, 0.6),
    ("woman", 1.60, 14.0, -2.7, 2.3, 0.0, 0.8), ("man", 1.76, 16.0, -1.6, 1.9, -0.04, 0.15),
    ("arms", 1.72, 7.4, 2.2, 2.2, 0.10, 0.0), ("woman", 1.62, 9.8, 2.7, 2.1, 0.06, 0.5),
    ("pack", 1.70, 12.4, 1.8, 2.2, 0.08, 0.45), ("child", 1.10, 12.9, 2.5, 2.2, 0.06, 0.9),
    ("man", 1.75, 15.2, 2.8, 2.0, 0.0, 0.75), ("arms", 1.66, 17.0, 1.7, 1.75, 0.02, 0.55),
    ("woman", 1.64, 8.2, -0.9, 1.7, -0.12, 0.65),
]
# Where the one off the eave lands (across, depth), and how far its fall puts the people near it to one side.
TIDE_LANDING = (0.6, 14.5)
TIDE_FLINCH = 1.7


def tide_land_art():
    """The art pixel the panel's impact beat throws its speed lines from (Prologue.cs, Beats, "tide"): the middle of
    the one that drops, as it lands. Printed by every run that draws the tide, to keep the two in step."""
    lx, ld = TIDE_LANDING
    x, y = _sp(lx, _ground(ld), ld)
    return (int(round(x)), int(round(y - TIDE_TALL["bruiser"] * TIDE_F / ld * 0.4)))


def _person_at(p, t):
    """Where one of the town's people is at panel time t: (across, depth). After the landing anyone behind it in its
    lane swerves wide of it - and so does anyone beside it, who flinches away."""
    kind, hm, d_end, x_end, v, vx, ph = p
    d = d_end + v * (TIDE_SECONDS - t)
    X = x_end - vx * (TIDE_SECONDS - t)
    lx, ld = TIDE_LANDING
    if t > TIDE_LAND:
        d_at = d_end + v * (TIDE_SECONDS - TIDE_LAND)
        x_at = x_end - vx * (TIDE_SECONDS - TIDE_LAND)
        if d_at > ld - 1.5 and abs(x_at - lx) < TIDE_FLINCH:
            u = min(1.0, (t - TIDE_LAND) / 0.6)
            u = u * u * (3 - 2 * u)
            side = -1.0 if x_at < lx else 1.0
            X += side * (TIDE_FLINCH - abs(x_at - lx)) * u
    # never wider than the animated box holds at this depth, body and stride and all
    s = TIDE_F / d
    room = (TIDE_BOX[2] - TIDE_BOX[0]) * 0.5 - 14.0
    lim = min(3.0, room / s - 0.45 * hm)
    return max(-lim, min(lim, X)), d


def _tide_people():
    return TIDE_PEOPLE


def _tide_flood():
    """The flood, body by body: (kind, across, the second it comes over the rise, depth it starts from in the square,
    depth it is closing on, speed m/s, seed). Each starts hidden in the square past the rise, comes up over it head
    first, and runs down the street toward us at a rush that slows as the mass bunches up behind the front; the
    first over go furthest."""
    rng = random.Random(77)
    kinds = ["swarmer", "swarmer", "swarmer", "runner", "bruiser", "shielder", "caster", "knight", "hound"]
    out = []
    N = 170
    for i in range(N):
        u = i / float(N - 1)
        X = (rng.random() * 2.0 - 1.0) * 3.1
        ts = 1.1 + 3.9 * u ** 0.95 + (rng.random() - 0.5) * 0.2
        d_start = 37.0 + rng.random() * 5.0
        d_halt = 18.5 + 14.0 * u ** 0.75 + rng.random() * 2.5
        v = 6.0 + rng.random() * 2.5
        out.append((pick(rng, kinds), X, ts, d_start, d_halt, v, ri(rng, 0, 1 << 16)))
    return out


def _flood_d(b, t):
    kind, X, ts, d_start, d_halt, v, seed = b
    if t < ts:
        return None
    tau = (d_start - d_halt) / v
    return d_halt + (d_start - d_halt) * math.exp(-(t - ts) / tau)


def _squash(F, ky, kx=1.0):
    """A figure's scratch canvas squashed (landing) or stretched, feet kept on the ground: nearest neighbour, so every
    pixel is still one of its own."""
    w, h = F.s.w, F.s.h
    nw, nh = max(1, int(round(w * kx))), max(1, int(round(h * ky)))
    img = F.s.img.resize((nw, nh), Image.NEAREST)
    return img, int(round(F.ox * kx)), int(round(F.oy * ky))


def _roofer_at(r, perches, t):
    """Where a roof creature is at time t: (x, feet, height px, rise 0-1, depth, facing, squash) or None before it
    comes up. A leaper's flight and fall are its own (_leaper_at)."""
    side, hi, pi, kind, t0, act = r
    if t < t0:
        return None
    g, ps = perches[side][hi]
    X, Y, d, _kind = ps[min(pi, len(ps) - 1)]
    if act == "crawl":
        d = max(g["d0"] + 0.4, d - max(0.0, t - t0 - TIDE_RISE_S) * 1.1)
    s = TIDE_F / d
    x, y = _sp(X, Y, d)
    h = TIDE_TALL[kind] * s
    rise = min(1.0, (t - t0) / TIDE_RISE_S)
    rise = rise * rise * (3 - 2 * rise)
    squash = 1.0
    if act == "leap" and t >= TIDE_CROUCH:
        squash = 0.86
    return (x, y + (1.0 - rise) * h, h, rise, d, -side, squash)


def _leaper_at(r, perches, t):
    """The one that drops: off the eave at TIDE_LEAP, a short hop up and the fall into the street at TIDE_LANDING,
    landing at TIDE_LAND on its haunches; it rises and comes on toward us, slow and heavy. (x, feet, height, depth,
    facing, squash) or None while it is still on the roof."""
    side, hi, pi, kind, t0, act = r
    if t < TIDE_LEAP:
        return None
    g, ps = perches[side][hi]
    X0, Y0, d0, _k = ps[min(pi, len(ps) - 1)]
    lx, ld = TIDE_LANDING
    if t < TIDE_LAND:
        u = (t - TIDE_LEAP) / (TIDE_LAND - TIDE_LEAP)
        X = X0 + (lx - X0) * u
        d = d0 + (ld - d0) * (1 - (1 - u) ** 1.6)
        Y = Y0 + (_ground(ld) - Y0) * u * u + 1.4 * math.sin(math.pi * u) * (1 - u)
        squash = 1.08
    else:
        a = t - TIDE_LAND
        X = lx - min(0.25, a * 0.08)
        d = ld - max(0.0, a - 0.5) * 0.62
        Y = _ground(d)
        squash = 0.78 if a < 0.125 else (0.9 if a < 0.375 else 1.0)
    s = TIDE_F / d
    x, y = _sp(X, Y, d)
    if t >= TIDE_LAND + 0.5:
        y += round(math.sin(t * 7.0) * 1.2)
    return (x, y, TIDE_TALL[kind] * s, d, -side, squash)


def _ember_set(fires):
    """Sparks off the fires, each on its own clock: (fire, phase, life, rise px/s, drift px/s, sway)."""
    rng = random.Random(31)
    pool = [f for f in fires if f["kind"] in ("roof", "window")] or fires
    out = []
    for _ in range(170):
        f = pick(rng, pool)
        out.append((f, rng.random() * 2.0, 1.1 + rng.random() * 0.9, 18.0 + rng.random() * 26.0,
                    4.0 + rng.random() * 9.0, rng.random() * 6.283))
    return out


def _tide_frame(t, k, ctx):
    """The animated box at panel time t: the whole frame's moving part, everything that is not the still street.
    Returns (c, front): c under the vignette (the fires, the smoke out of the windows, the embers, his creatures),
    front laid over it after (the town's people, whose one-pixel rims the vignette's dither would eat)."""
    sc, fires, perches = ctx["sc"], ctx["fires"], ctx["perches"]
    box = TIDE_BOX
    bx0, by0, bx1, by1 = box
    inset = (bx0 + 6, by0 + 6, bx1 - 6, by1 - 6)
    c = Cv(FW, FH)
    fr = random.Random(9000 + k)
    eye = ctx["eye"]

    def inside(x, y):
        return inset[0] <= x < inset[2] and inset[1] <= y < inset[3]
    # The fire and what it gives off go on their own canvas, kept out of the vignette: its dither laid a lattice of
    # dark dots over every flame (a light is not darkened by the frame's edge; it stays inside the box's inset).
    glow = Cv(FW, FH)
    creatures = c
    c = glow
    # Smoke pouring out of the burning windows, rising up the wall and thinning out by Bayer steps, under the flames.
    for f in fires:
        if f["kind"] != "window":
            continue
        prng = random.Random(f["seed"] + 5)
        for j in range(3):
            per = 1.5
            a = (t + j * per / 3.0 + prng.random() * 0.3) % per
            life = a / per
            px_ = f["x"] + f["out"] * 0.7 + a * 7.0 + math.sin(a * 2.0 + j) * 2.0
            py_ = f["y"] - f["h"] * 0.55 - a * 26.0
            r = (2.0 + a * 6.0) * min(1.6, f["w"] / 6.0 + 0.6)
            dens = 0.85 * (1.0 - life)
            for yy in range(int(py_ - r), int(py_ + r) + 1):
                for xx in range(int(px_ - r), int(px_ + r) + 1):
                    nx, ny = (xx - px_) / r, (yy - py_) / (r * 0.85)
                    if nx * nx + ny * ny <= 1.0 and inside(xx, yy) and dith(xx, yy, dens):
                        c.set(xx, yy, R[2] if ny > 0.35 else (V[4] if ny > -0.2 else S[4]))
    # The fires.
    for f in fires:
        _flames(c, f, t, fr, inset)
    # Embers, flying up off the fires and down the wind.
    for (f, ph, life, rise, drift, sw) in ctx["embers"]:
        a = (t + ph) % 2.0
        if a > life:
            continue
        x = f["x"] + drift * a + math.sin(a * 3.0 + sw) * 3.0
        y = f["y"] - f["h"] * 0.5 - rise * a
        if inside(x, y):
            c.set(x, y, FI[1] if a < life * 0.35 else (FI[2] if a < life * 0.75 else FI[3]))
    c = creatures
    # His creatures, far to near: the flood over the rise and down the street, hidden by the rise until they clear
    # it (the street's depth); the ones on the roofs, hidden by their eaves until they come up over them; the one
    # that drops.
    draws = []
    for b in ctx["flood"]:
        d = _flood_d(b, t)
        if d is None:
            continue
        draws.append((d, "flood", b))
    for r in TIDE_ROOFERS:
        if r[5] == "leap":
            lp = _leaper_at(r, perches, t)
            if lp is not None:
                draws.append((lp[3], "leaper", (r, lp)))
                continue
        rp = _roofer_at(r, perches, t)
        if rp is not None:
            draws.append((rp[4], "roof", (r, rp)))
    draws.sort(key=lambda q: -q[0])
    for (d, what, b) in draws:
        if what == "flood":
            kind, X, ts, d_start, d_halt, v, seed = b
            s = TIDE_F / d
            x, y = _sp(X, _ground(d), d)
            h = TIDE_TALL[kind] * s
            fast = d > d_halt + 1.5
            if fast:
                y += round(math.sin(t * 9.0 + seed) * min(1.4, h * 0.04))
            facing = 1 if X > 0.15 else (-1 if X < -0.15 else (1 if seed % 2 else -1))
            light = (eye[0] - x, eye[1] - (y - h * 0.6))
            F = _tide_fig(kind, h, seed, facing, light, V[2] if h >= 26 else None, snap=True)
            ox, oy = int(round(x)) - F.ox, int(round(y)) - F.oy
            if d > TIDE_CREST[0] - 2.0:
                _paste_clipped(c, F.s.img, ox, oy, sc, d)
            else:
                if h >= 12:
                    c.hline(x - h * 0.16, x + h * 0.16, y + 1, OCC)
                over(c, F.s, ox, oy)
        elif what == "roof":
            r, (x, y, h, rise, d, facing, squash) = b
            light = (eye[0] - x, eye[1] - (y - h * 0.6))
            F = _tide_fig(r[3], h, 300 + TIDE_ROOFERS.index(r), facing, light, V[2])
            if squash != 1.0:
                img, ox, oy = _squash(F, squash, 1.06)
            else:
                img, ox, oy = F.s.img, F.ox, F.oy
            _paste_clipped(c, img, int(round(x)) - ox, int(round(y)) - oy, sc, d + 0.5)
            if r[3] == "caster" and rise >= 1.0:
                # its loose orb, burning his arcane over the roof: the caster's attack, coming
                ox_, oy_ = x + facing * 0.30 * h, y - 1.18 * h + math.sin(t * 5.0) * 1.2
                c.disc(ox_, oy_, max(1.5, h * 0.09), max(1.5, h * 0.09), AR[2])
                c.disc(ox_, oy_, max(0.8, h * 0.045), max(0.8, h * 0.045), AR[1])
        else:
            r, (x, y, h, d, facing, squash) = b
            light = (eye[0] - x, eye[1] - (y - h * 0.6))
            F = _tide_fig(r[3], h, 300 + TIDE_ROOFERS.index(r), facing, light, V[2])
            if squash != 1.0:
                img, ox, oy = _squash(F, squash, 2.0 - squash)
            else:
                img, ox, oy = F.s.img, F.ox, F.oy
            if t >= TIDE_LAND:
                c.hline(x - h * 0.2, x + h * 0.2, y + 1, OCC)
            c.img.paste(img, (int(round(x)) - ox, int(round(y)) - oy), img)
            c.px = c.img.load()
            # the street it lands on thrown up round its feet, a ring of grit spreading and gone
            a = t - TIDE_LAND
            if 0.0 <= a < 0.5:
                drng = random.Random(77 + k)
                rad = h * (0.35 + a * 1.4)
                for _q in range(int(70 * (1.0 - a * 1.6))):
                    ang = drng.random() * 6.283
                    rr = rad * (0.7 + drng.random() * 0.4)
                    gx_, gy_ = x + math.cos(ang) * rr, y - abs(math.sin(ang)) * rr * 0.34 - drng.random() * h * 0.2
                    if inside(gx_, gy_):
                        c.set(gx_, gy_, RU[2] if drng.random() < 0.55 else (S[1] if drng.random() < 0.5 else RU[3]))
    # The town's people, far to near, after the vignette.
    front = Cv(FW, FH)
    ppl = []
    for p in ctx["people"]:
        X, d = _person_at(p, t)
        ppl.append((d, X, p))
    ppl.sort(key=lambda q: -q[0])
    for (d, X, p) in ppl:
        kind, hm, d_end, x_end, v, vx, ph = p
        s = TIDE_F / d
        x, y = _sp(X, _ground(d), d)
        h = hm * s
        vx_screen = X * TIDE_F * v / (d * d) + vx * s
        facing = 1 if vx_screen > 0 else -1
        stride = int(t * TIDE_FPS + ph * 2) % 2 == 0
        front.hline(x - h * 0.16, x + h * 0.16, y + 1, OCC)
        _fleer(front, x, y, h, kind, stride, facing, RU[1])
    return glow, c, front


def p_tide():
    """4. "There was no one left to defend them." The town overrun, from inside it (Eric, 2026-11-10): low in its
    street and looking up, the houses towering on both sides and burning against a sky of smoke, its people running
    down the street toward us, and behind them his creatures pouring over the rise from the burning square and along
    the roofs; one drops off an eave into the street among them. Small at the street's end, between the roofs, the
    tower with its eye burning. Up close his creatures are the chapters' kinds - cowl, horns, spear, shield, the
    caster's loose orb - and every one of them wears his mark in its eyes."""
    back, mid, fore = frame(True), frame(), frame()
    rng = random.Random(55)
    sc = _Scene()
    light = _Light()
    fires = []
    lights = []
    # THE SKY: night at the top, his violet lower down, red over the burning square; smoke over most of it.
    for y in range(FH):
        b = int(min(2.6, max(0.0, (y - 40) / 170.0)) * 16)
        for x in range(FW):
            sc.put(x, y, T_SKY, TIDE_SKY_Z, b)
    # THE SQUARE'S FAR SIDE, burning, and over it the tower with its eye (drawn on the back after the resolve).
    _far_side(sc, fires, lights, rng)
    lights.append((TIDE_VPX, 446, 150, 70, 2.6))
    crest_y = _street(sc, rng)
    # THE HOUSES, both sides.
    perches = {-1: [], 1: []}
    near = []
    for side in (-1, 1):
        for i, h in enumerate(TIDE_HOUSES[side]):
            g = _house_geo(side, h)
            hr = random.Random(400 + side * 40 + i)
            p = _draw_house(sc, side, g, hr, fires, lights)
            perches[side].append((g, p))
            if i == 0:
                near.append((side, g))
    # The street lit by the fires along it, more on the side the walls burn.
    for (cx, cy, rx, ry, st) in list(lights):
        light.add(cx, cy, rx, ry, st)
    for d in (9.0, 13.0, 18.0, 24.0):
        x, y = _sp(0.0, _ground(d), d)
        light.add(x, y, 3.4 * TIDE_F / d, 1.4 * TIDE_F / d, 1.0)
    # the square's fire on its far side and over the rise, the bright ground the flood comes over
    light.add(TIDE_VPX, crest_y - 18, 120, 34, 2.4)
    # and the street's fires down the inner edges of the two nearest walls, which would otherwise be flat black
    for xs in (-1, 1):
        light.add(TIDE_VPX + xs * 175, 430, 120, 190, 1.3)
    light.done()
    # THE SMOKE: a column off every burning roof and off the square, leaning down the wind to our right, and a pall
    # of it overhead. Behind every house (its depth), lit from under by the light.
    srng = random.Random(91)
    for f in fires:
        if f["kind"] in ("roof", "far") and srng.random() < 0.45:
            _smoke(sc, f["x"], f["y"] - f["h"] * 0.9, 6.0 + f["h"] * 0.12, 9, 0.45, ri(srng, 0, 9999))
    for k in range(26):
        _smoke(sc, srng.random() * FW, 60 + srng.random() * 150, 16 + srng.random() * 14, 4, 0.6, 600 + k,
               z=TIDE_SMOKE_Z + 50)
    _resolve(sc, light, back)
    # His tower at the end of the street, between the roofs, its eye burning - over the sky and smoke, under the
    # square's far side and everything nearer (redrawn through the depth below).
    tx, tbase = 491, 452
    eye = (tx - 74 * 0.11 * 0.15, tbase - 74 * 1.08)
    tower(back, tx, tbase, 74, seed=3, lit=False)
    black_sun(back, eye[0], eye[1], 6.0, rays=16, seed=12)
    _resolve(sc, light, back, keep=lambda z: z < TIDE_SMOKE_Z - 1e6)
    # THE NEAREST TWO HOUSES on the fore as well: nearest the camera, they drift furthest (the tilt up).
    nsc = _Scene()
    for (side, g) in near:
        _draw_house(nsc, side, g, random.Random(400 + side * 40), [], [])
    _resolve(nsc, light, fore)
    # THE ANIMATED BOX: every frame the whole moving part of the street, under the panel's own vignette so it meets
    # the still layers at the box's edge without a seam, and the town's people over it.
    ctx = dict(sc=sc, fires=fires, perches=perches, eye=eye, flood=_tide_flood(), people=_tide_people(),
               embers=_ember_set(fires))
    t_of = frame_t(0.75)
    frames = []
    for k in range(int(math.ceil(TIDE_SECONDS * TIDE_FPS))):
        t = (k + 0.5) / TIDE_FPS
        glow, c, front = _tide_frame(t, k, ctx)
        darken(c, t_of, [S[4], OCC], box=TIDE_BOX)
        over(glow, c)
        over(glow, front)
        frames.append(glow.img.crop(TIDE_BOX))
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.75)
    layers["anim"] = frames
    return layers


def p_taken():
    """4. "He took every wizard who might rival him." The game's wizards led away along a ridge against his light, bound
    wrist to wrist by his chain, their staves dark, their light drawn out of them toward the tower.
    Silhouettes only, the three the player frees included (Eric), and never Dee, who reads as the player."""
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
             ("wiz", 4), ("caster", 0), ("wiz", 5), ("runner", 0)]
    # Six of the game's wizards, walking (each a different frame of his own walk), staves dark,
    # Tituba, Vainamoinen and Laelaps among the first four; and the wizards walk near the head of the line,
    # by the tower, because the phone's canvas crops the panel's sides and shows the head of it, not the
    # tail. Dee walked fifth until Eric, 2026-11-08: "Yes, remove him" (his grey robe and violet light read
    # as the player, who is never taken); his place closed up, the line one figure shorter at its tail,
    # which is off the left of every screen, so no gap shows where he walked.
    taken = [(TITUBA, 2), (FROST, 1), (VAINAMOINEN, 6), (LAELAPS, 4), (PYRO, 3), (STORM, 7)]
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
            creature(mid, kind, x, yb, hgt, facing=1, body=[OCC, S[4]], rim=DK[3], seed=700 + i, light=(sx_, sy_))
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
# Out to the PC's left edge (2026-11-08), so the hound walks in from it rather than appearing where the box began,
# and down to the feet of his stone and the hound beside it.
MISSED_BOX = (168, 400, 664, 552)
MISSED_FPS = 8
# The panel's hold in Prologue.cs (Beats, "missed"); the frames cover it, the last held after.
MISSED_SECONDS = 5.4
MISSED_COLUMNS = 4
# His light in the hiding: the black sun over the tower on the horizon (p_missed, tower(back, 694, 396, 34)).
# The searchers are rimmed from it and nothing else; the row of stones and the player stand in the key.
MISSED_LIGHT = (694, 359)
# Where he hides: (x, feet, height), pressed against the right of the row's biggest stone (centred at 456, 86
# wide), and where the hound stops at its left edge, nose to the stone.
MISSED_HIM = (536, 546, 108)
MISSED_HOUND_STOP = 380.0


def sniffing_hound(c, x, yb, h, facing, head, stride, twitch=0.0, rim=None, light=None):
    """His hound, the tide's (creature "hound") posed to search: head is -1 with its nose to the
    ground, 0 level, +1 lifted to the air; stride swings the legs; twitch nudges the nose."""
    # The tail hangs while it works the ground and comes up when it has something. The head turns about
    # the top of the neck: down to the ground, up to the air.
    pose = {"stride": stride, "twitch": twitch,
            "tail": 0.5 if head < 0 else (-0.3 if head > 0 else 0.0),
            "head": 0.95 if head < 0 else (-0.4 if head > 0 else 0.0),
            "drop": 2.6 if head < 0 else 0.0}
    creature(c, "hound", x, yb, h, facing=facing, body=[OCC, S[4]], rim=rim, seed=53, light=light, pose=pose)


def rot(x, y, a):
    """(x, y) turned by a radians, clockwise on screen."""
    return (x * math.cos(a) - y * math.sin(a), x * math.sin(a) + y * math.cos(a))


def _hound_at(t):
    """The hound's one search past his stone, as (x, head, stride, twitch, facing). It trots in from the left
    along the stones, nose down, stops at the far side of the stone he hides behind, lifts its head and works
    the air at its edge - a stone's width from him - and turns back the way it came."""
    walk = lambda d: math.sin(d * 2 * math.pi / 13.0) * 2.4
    start, stop = 180.0, MISSED_HOUND_STOP
    if t < 1.3:
        x = start + (stop - start) / 1.3 * t
        return x, -1, walk(x - start), 0.0, 1
    if t < 1.55:
        return stop, 0, 0.0, 0.0, 1
    if t < 2.95:
        # Working the air: the nose twitches every other frame; he does not move.
        return stop, 1, 0.0, (0.35 if int(t * MISSED_FPS) % 2 else -0.15), 1
    if t < 3.15:
        return stop, 0, 0.0, 0.0, -1
    x = stop - 80 * (t - 3.15)
    return x, -1, walk(stop - x), 0.0, -1


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
    """5. "You are the one he missed." The hiding (Eric, 2026-11-01; hiding properly since 2026-11-08). The
    broken ring at night, its near stones in a row across the heath, and pressed against the far side of the
    biggest of them, in its shadow behind a low rock, the player - the title's own figure, still as the stone,
    his light put out to an ember. His creatures search: a hound trots along the row nose down to the stone's
    other edge, lifts its head and works the air there, a stone's width from him - he lets his ember go out -
    and turns back; a caster further along watches; two small ones turn their heads; and out in the dark, pair
    by pair, the eyes turn away and back. They never find him. No new words: the line is the premise's."""
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

    # THE ROW, AND HIS STONE (Eric, 2026-11-08: "the wizard is standing out in the open. he should be hiding
    # properly" - he stood in the gap between two stones, in full view of anything beyond the row). Now the
    # biggest stone of the row stands between him and the search: he is pressed flat against its far side from
    # the hound, in its shadow, a low rock in front of his legs, and the hound comes along the row to the
    # stone's other edge - a stone's width from him - and works the air there. We see both of them, and the
    # stone between; nothing on the hound's side can. Drawn once per state of his ember; the frames lay it over
    # the searchers, and the mid layer carries it whole.
    hx_, hb_, hh_ = MISSED_HIM

    def row(ember):
        c = Cv(FW, FH)
        for (x, yb, w, h, sd, lean, br) in [(232, 548, 58, 132, 911, 0.05, True), (456, 544, 86, 160, 913, -0.02, False),
                                            (652, 546, 56, 124, 917, 0.04, False), (772, 552, 62, 140, 919, -0.05, True)]:
            standing_stone(c, x, yb, w, h, sd, lean=lean, broken=br, ramp=[S[2], S[3], S[4], OCC, OCC],
                           toward=(-1, -0.6, S[1]))
        # He stands in the stone's shadow: the title's figure two stops down every pigment row, the key's rim
        # softened to bone - plainly him to us, and on the hound's side of the stone nothing at all. Only the
        # ember, which is light and not pigment, keeps its own colours.
        him = Cv(FW, FH)
        hero.wizard_hero(him, hx_, hb_, h=hh_, staff_ramp=LT, under_ramp=None, snuffed=ember)
        down = _shade_down(2, keep=LT, more={"gold": 1})
        hp = him.px
        for y in range(int(hb_ - 140), int(hb_ + 30)):
            for x in range(int(hx_ - 70), int(hx_ + 90)):
                q = hp[x, y]
                if q[3] and q in down:
                    hp[x, y] = down[q]
        over(c, him)
        # The low rock he crouches behind, over his legs: lit by the key as the moot's near boulders are, so it
        # reads against his robe in the dark.
        sk.boulder(c, hx_ + 6, hb_ + 8, 70, 36, seed=921, ramp=[S[1], S[1], S[2], S[2], S[3]])
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
        # horizon at 392 to his feet at 546): the two small ones far back, the caster further along the
        # row, watching, the hound just behind the row, at the far edge of his stone.
        creature(c, "swarmer", 318, 458, 30, facing=_facing_at(t, [0.7, 2.4, 4.0], 1), body=[OCC, S[4]],
                 rim=V[3], seed=31, light=MISSED_LIGHT)
        creature(c, "swarmer", 648, 452, 28, facing=_facing_at(t, [1.5, 3.0, 4.5], -1), body=[OCC, S[4]],
                 rim=V[3], seed=37, light=MISSED_LIGHT)
        creature(c, "caster", 336, 470, 50, facing=_facing_at(t, [1.1, 3.3], 1), body=[OCC, S[4]],
                 rim=V[2], seed=41, light=MISSED_LIGHT)
        hx, head, stride, twitch, hf = _hound_at(t)
        sniffing_hound(c, hx, 534, 54, hf, head, stride, twitch, rim=V[2], light=MISSED_LIGHT)

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
# Between the hiding and the road: the Archmage - the one he missed, the playable starter - goes back for
# the four he could reach, the disciplines a fresh save plays, and breaks the dark wizard's chain off them
# (animated, BREAK_BOX); their lights come back, they stand with him, and the dark wizard's creatures close
# in. Then the five of them on the road (the five's own panel, which stood them together, was folded into it,
# Eric, 2026-11-08), and the character select, where all five are the player's to choose. See
# .ai/cutscenes.md.
#
# The four kneel where the hiding's row of stones stood and stand up in the same places. The order, left to
# right: Pyromancer, Geomancer, him, Frostweaver, Stormcaller. The fire is outermost: inside, the
# Pyromancer's lantern, held out in front of him, came back to light behind the Archmage's staff.
FREED_FOUR = [(PYRO, 342, 524, 88, 1), (GEO, 404, 536, 94, 1), (FROST, 556, 536, 94, -1), (STORM, 618, 524, 88, -1)]
# Where he stands in both: the title's figure, his feet above row 560 so the PC's caption band
# (rows ~558-578) never crosses them.
HIM_X, HIM_Y, HIM_H = 480, 548, 112

# The chain's breaking, the four coming back to their colours and his creatures closing in are the panel's moving
# part: a box of its mid layer swapped on a clock, the hiding's way (write_anim). Every edge a multiple of 8, so the
# dissolve's Bayer threshold, taken from the atlas's own pixel, matches the mid layer's under it. As wide as a PC
# shows (2026-11-08), so the creatures closing in from the sides walk in from its edges rather than appearing
# where a narrower box began.
BREAK_BOX = (168, 384, 792, 584)
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
# outer two, then the shackles; then each of them takes his own light back, one after another, close behind the
# shackles (Eric, 2026-11-08, "you can see the wizards": they used to stay three stops into the dark until 3.2 s,
# so a look at the panel mid-hold found four dark shapes), and stands up, turning out to face what is coming.
BREAK_LIGHT_UP = 1.3
BREAK_AT = [2.0, 2.25, 2.25]          # the middle length, the left, the right
SHACKLES_AT = 2.5
RELIGHT_AT = [2.6, 2.75, 2.9, 3.05]   # FREED_FOUR's order
# Seconds after his light is back that he is up: half risen, then standing.
RISE_AT = (0.5, 0.625)

# HIS CREATURES CLOSE IN (Eric, 2026-11-08: "show enemies closing in on the wizards after he breaks the chains").
# The moment the chain is broken the dark wizard's things come for them: down off the ridge behind them, growing
# as they come, to loom over the four's heads, and in from either side along the ground, a hound and a runner
# and two small ones, until they stand close round the five. Each creeps in and stops (an ease out), rimmed now by
# the Archmage's gold, his creatures' eyes in their heads. (kind, from (x, feet, height), to (x, feet, height),
# when it starts, how long it takes).
BREAK_CREATURES = [("caster", (360, 414, 18), (368, 474, 60), 2.3, 2.2),
                   ("bruiser", (436, 412, 18), (436, 470, 64), 2.5, 2.2),
                   ("knight", (528, 412, 20), (528, 468, 68), 2.4, 2.3),
                   ("shielder", (600, 414, 18), (594, 474, 58), 2.6, 2.1),
                   ("hound", (140, 540, 30), (298, 536, 32), 2.35, 2.2),
                   ("runner", (820, 534, 40), (662, 530, 42), 2.45, 2.2),
                   ("swarmer", (150, 508, 26), (294, 502, 28), 2.8, 2.0),
                   ("swarmer", (810, 506, 26), (668, 500, 28), 2.9, 2.0)]


def _break_creature(entry, t):
    """(x, feet, height, moving) of one of his creatures at panel time t, or None before it starts."""
    kind, (x0, y0, h0), (x1, y1, h1), t0, dur = entry
    if t < t0:
        return None
    u = min(1.0, (t - t0) / dur)
    s = 1.0 - (1.0 - u) * (1.0 - u)
    return x0 + (x1 - x0) * s, y0 + (y1 - y0) * s, h0 + (h1 - h0) * s, u < 1.0


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
    broken = t >= BREAK_AT[0]
    # His creatures, far to near with the four and the chain: the ones off the ridge behind them first, under
    # everything; the ones along the ground at their own depth, after the four.
    beasts = []
    for k, e in enumerate(BREAK_CREATURES):
        pose = _break_creature(e, t)
        if pose is not None:
            beasts.append((pose[1], k, e[0], pose))
    beasts.sort()

    def beast(k, kind, pose):
        x, yb, h, moving = pose
        bob = round(math.sin(t * 9.0 + k * 1.3)) if moving else 0
        c.hline(x - h * 0.18, x + h * 0.18, yb + 1, OCC)
        creature(c, kind, x, yb + bob, int(round(h)), facing=1 if x < HIM_X else -1, body=[OCC, S[4]],
                 rim=G[3], seed=820 + k, light=(HIM_X, HIM_Y - 60))
    for (yb, k, kind, pose) in beasts:
        if yb < 500:
            beast(k, kind, pose)
    for i, (who, x, y, h, f) in enumerate(FREED_FOUR):
        since = t - RELIGHT_AT[i]
        coming = -0.6 <= since < 0
        # His own sprite, kneeling (the second frame of his fall), three stops into the dark while the chain
        # holds him, and coming up into his own colours a stop at a time as his light returns (Eric:
        # "Redraw with real sprites"); then up off his knees and turned out, back to back with the Archmage,
        # facing what is closing in.
        steps = 3 if since < 0 else (2 if since < 0.2 else (1 if since < 0.4 else 0))
        if since < RISE_AT[0]:
            strip, fr, face = "death", 1, f
        elif since < RISE_AT[1]:
            strip, fr, face = "death", 0, f
        else:
            strip, fr, face = "idle", 0, -f
        # Their only lit edge before the break is the dark wizard's light, his chain's, landing as the violet
        # it makes (the rule: his ramp is emission on him and the chain alone); once the chain breaks it is
        # the Archmage's gold, from his staff.
        if broken:
            toward = (HIM_X - x, (HIM_Y - 60) - (y - 50))
            n = math.hypot(*toward) or 1.0
            rim = (toward[0] / n, toward[1] / n, G[2])
        else:
            rim = (f, 0, V[2])
        light, box = sprite_figure(c, who, x, y, strip=strip, frame=fr, facing=face, steps=steps, rim=rim)
        # Their own light coming back to them, out of the sky it was drawn into, just before it lights.
        if light and (coming or since >= 0):
            ax, ay = light
            span = min(1.0, (since + 0.6) / 0.6)
            motes(c, (ax - f * 40, ay - 120), (ax, ay), int(70 * span) if coming else 18, 900 + i,
                  [E[who[2]]], spread=10, bow=-14)
    # The chain held them kneeling, and its pieces stay where it broke: its points from where their wrists were,
    # however they stand now.
    wrists = him["wrists"]
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
    for (yb, k, kind, pose) in beasts:
        if yb >= 500:
            beast(k, kind, pose)
    return c


def p_break():
    """6. "You rescued four - they now stand with you." The hiding's heath again, the tower on its horizon, and the four
    he could reach - the disciplines, each his own sprite (sprite_figure) put three stops into the dark - on
    their knees in a row, bound wrist to wrist by his chain, its violet their only lit edge, their staves
    dark. The Archmage, the one he missed, stands in the middle of them, the title's own figure, his light
    banked as it was in the hiding. It comes up (animated): his light finds the chain and it breaks, the
    length behind him first, then the others, link by link going out as it flies, and his gold is on their
    edges now; one after another their own lights come back to them out of the sky they were drawn into,
    each comes up into his own colours a stop at a time and stands, turned out, back to back with him - as the
    dark wizard's creatures close in on the five of them, down off the ridge and in from either side."""
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
    # Where the chain held each of them: his front at the wrist, kneeling.
    wrists = []
    for (who, x, y, h, f) in FREED_FOUR:
        _light, box = sprite_figure(Cv(FW, FH), who, x, y, strip="death", frame=1, facing=f, steps=3)
        wrists.append(sprite_wrist(box, f))
    him = {"banked": draw_him(0.6), "lit": draw_him(None),
           "head": ((int(sx) + int(sx + w)) * 0.5, HIM_Y - 37.0 * u), "wrists": wrists}

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


def p_road():
    """7. "The rest still wait for you." The road ahead. Seen from above and behind: the five of them - the
    Archmage and the four he freed - small on a road that winds away across the heath toward everything the dark
    wizard holds, past the campaign's places, the tower on the far horizon under his light. Then the character
    select, the player choosing who walks it."""
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

    # THE JOURNEY'S LANDMARKS (Eric, 2026-11-05: "more landmarks instead of the big, flat, empty
    # plain"). The places the campaign visits, passed in campaign order, nearest first, so the road
    # he looks down IS the game ahead of him (.ai/world-map.md's places, .ai/campaign.md's order):
    # the west wood the road leaves (the trees on the fore below), the Fields' hedged strips and
    # barn, the Broken Circle's ring on its knoll - eight stones and the walker's gap - the scarp
    # over the Sunken Cave's mouth, the Swamp's pools and dead snags, the Ruins' fallen colonnade
    # (the broken keep beyond it), the Waste's pale peaks, the Sands' crescent dunes, and last the
    # Emberdeep's black bluffs, which the road threads just under his tower. Dark places in a dark
    # land: silhouette and stone, never a lit window - the player's light is the only warm light
    # on the road (.ai/prologue.md, "The player is the light"). All on the back layer, the road's
    # own, so nothing slides on the ground; only the wood's near trees ride the fore's parallax.

    # The Fallow Fields, south of the road as the map has them: three hedged strips and the barn.
    for (fx, fy, fw_, fh_) in [(334, 522, 92, 13), (352, 541, 104, 12), (336, 557, 84, 11)]:
        drop = -0.05
        back.poly([(fx, fy), (fx + fw_, fy + fw_ * drop), (fx + fw_, fy + fw_ * drop + fh_),
                   (fx, fy + fh_)], V[4])
        for r_ in range(2, fh_, 3):
            back.line(fx + 1, fy + r_, fx + fw_ - 1, fy + fw_ * drop + r_, S[4])
        rngh = random.Random(77 + fx)
        for e_ in range(0, fw_, 6):                     # the hedge round each strip, in bumps
            back.disc(fx + e_, fy + e_ * drop - 1 + rngh.random(), 1.6, 1.0, OCC)
            back.disc(fx + e_ + 3, fy + fh_ + (e_ + 3) * drop + 1 + rngh.random(), 1.6, 1.0, OCC)
    bx_, by_, bw = 447, 538, 16                         # the farmstead, empty: no lit window here
    back.rect(bx_ - bw / 2, by_ - 7, bx_ + bw / 2, by_, S[4])
    back.rect(bx_ + bw * 0.08, by_ - 7, bx_ + bw / 2, by_, OCC)
    back.poly([(bx_ - bw / 2 - 2, by_ - 7), (bx_ + bw / 2 + 2, by_ - 7), (bx_ + 1, by_ - 13)], OCC)
    back.line(bx_ - bw / 2 - 2, by_ - 7, bx_ + 1, by_ - 13, V[4])
    back.hline(bx_ - bw / 2, bx_ + bw / 2, by_ + 1, OCC)

    # The Broken Circle, up on its knoll: eight stones and the gap where the ninth stood (it walks).
    back.disc(599, 468, 36, 11, V[4])
    back.disc(597, 466, 30, 9, S[4])
    ring = []
    for k in range(9):
        if k == 2:
            continue
        ang = -math.pi / 2 + k * 2 * math.pi / 9.0
        ring.append((466 + math.sin(ang) * 7.5, 598 + math.cos(ang) * 24, k))
    for (sy_, sx_, k) in sorted(ring):
        hh = max(6.0, 10 + (k * 7 % 5) - (sy_ - 466) * 0.35)
        standing_stone(back, sx_, sy_, max(3.5, hh * 0.42), hh, 510 + k,
                       lean=((k * 11 % 7) - 3) * 0.05, ramp=[S[2], S[3], S[4], OCC, OCC],
                       broken=k in (4, 7))

    # The scarp over the Sunken Cave: a pale broken lip, the face under it, the mouth a black notch.
    lip = lambda x: 429 + math.sin(x * 0.13) * 1.5 - (x - 352) * 0.03
    for x in range(352, 430):
        yt = int(lip(x))
        back.set(x, yt - 1, S[3])
        for d_ in range(0, 9):
            back.set(x, yt + d_, S[4] if dith(x, yt + d_, 0.55) else V[4])
    back.poly([(384, 438), (386, 432), (391, 430), (396, 433), (398, 438)], OCC)

    # The Blighted Swamp: two cold pools, the moon on the near one, and the dead snags over them.
    back.disc(543, 414, 11, 3, S[4])
    back.disc(541, 413, 7, 2, S[3])
    back.disc(568, 408, 8, 2.5, S[4])
    back.disc(567, 408, 5, 1.5, S[3])
    back.hline(538, 545, 413, S[2])
    for (bx2, bh2, arm) in [(527, 9, 1), (536, 13, -1), (551, 10, 1), (562, 14, -1), (575, 9, 1), (583, 12, -1)]:
        yb2 = 419 - (583 - bx2) * 0.04
        back.vline(bx2, yb2 - bh2, yb2, OCC)
        back.line(bx2, yb2 - bh2 + 3, bx2 + arm * 3, yb2 - bh2 + 1, OCC)
        if bh2 > 10:
            back.line(bx2, yb2 - bh2 + 6, bx2 - arm * 2, yb2 - bh2 + 5, OCC)

    # The Mystic Ruins, this side of their keep: a fallen colonnade, one lintel still up.
    for i_, (cx2, ch2) in enumerate([(613, 9), (621, 12), (630, 7), (639, 11), (647, 5)]):
        yb2 = 394 - i_ * 0.6
        back.rect(cx2, yb2 - ch2, cx2 + 2, yb2, S[4])
        back.vline(cx2, yb2 - ch2, yb2, S[3])
        back.set(cx2, yb2 - ch2, S[2])
        back.hline(cx2 - 1, cx2 + 3, yb2 + 1, OCC)
    back.line(613, 384, 622, 382, S[3])
    back.line(613, 385, 622, 383, S[4])
    back.disc(635, 396, 3, 1.3, S[4])
    back.disc(634, 396, 2, 0.9, S[3])

    # The Frozen Waste's peaks, pale over the western hills; the snow flank catches the key.
    for (px2, pw2, ph2) in [(346, 15, 12), (368, 21, 17), (396, 13, 10)]:
        back.poly([(px2 - pw2 * 0.5, 361), (px2 - pw2 * 0.08, 361 - ph2),
                   (px2 + pw2 * 0.14, 361 - ph2 + 2), (px2 + pw2 * 0.5, 361)], S[4])
        back.line(px2 - pw2 * 0.08, 361 - ph2, px2 - pw2 * 0.5, 361, S[2])
        back.set(px2 - pw2 * 0.08, 360 - ph2, S[1])

    # The Scorched Sands: three crescent dunes, their slip faces falling into shadow.
    for (dx2, dl2, db2, dy2) in [(626, 22, 3.0, 351), (642, 26, 3.6, 355), (660, 17, 2.4, 348)]:
        for j_ in range(dl2):
            t_ = j_ / float(dl2)
            xx2 = dx2 + j_
            yy2 = dy2 - math.sin(t_ * math.pi) * db2
            back.set(xx2, yy2, S[3] if t_ < 0.65 else S[4])
            back.set(xx2, yy2 + 1, OCC)

    # The Emberdeep's gate: two black basalt bluffs either side of the road's last bend, violet in
    # their stone as the mountain has (never an ember - no warm light on the road but his).
    for pts_ in [[(534, 331), (538, 312), (547, 304), (556, 308), (562, 319), (562, 331)],
                 [(592, 342), (597, 325), (604, 317), (614, 313), (623, 320), (629, 334), (630, 342)]]:
        back.poly(pts_, OCC)
        xs_ = [p[0] for p in pts_]
        ys_ = [p[1] for p in pts_]
        for yy2 in range(int(min(ys_)), int(max(ys_)) + 1):
            for xx2 in range(int(min(xs_)), int(max(xs_)) + 1):
                if back.get(xx2, yy2)[:3] == rgb(OCC)[:3] and dith(xx2, yy2, 0.22):
                    back.set(xx2, yy2, V[4])
        back.line(pts_[1][0], pts_[1][1], pts_[2][0], pts_[2][1], V[4])
        back.set(pts_[2][0], pts_[2][1] - 1, V[3])

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
    # THE FIVE OF THEM ON THE ROAD (Eric, 2026-11-08: the five's own panel "remove this, combine with the last
    # panel", and of this one "make the wizard smaller on the map"). The Archmage and the four he freed walk it
    # together, small among the landmarks and up the road, under "The rest still wait for you." - the five's
    # standing together folded into the journey they start. Each ON the road (x from the spline at his row), the
    # four in their own colours with their own lights, the Archmage among them with his: five lights on the road,
    # where the hiding left one. All above row 548, clear of the line on a PC.
    party = [(FROST, 458), (PYRO, 476), (None, 494), (GEO, 512), (STORM, 530)]
    him_at = min(pts, key=lambda p: abs(p[1] - 494))
    back.radial(him_at[0], 497, 36, 14, [G[3], G[3], G[4], G[4], None])
    # Free, the four wear the middle of their own rows, not the moot's deep end (the Stormcaller's deep gold was lost on
    # the gold of the Archmage's light), rimmed by that light in the pigment it makes.
    walking = {"pyromancer": R[1], "frostweaver": M["azure"][2], "geomancer": M["verdant"][2], "stormcaller": G[2]}
    # the wood's edge either side of the road where it leaves the frame: the Enchanted Forest is
    # the first chapter, so the journey starts at the trees
    rngw = random.Random(79)
    for _ in range(14):
        sk._clump(back, 300 + rngw.random() * 118, 598 + rngw.random() * 64, 11, 7, 34, rngw, OCC, S[4])
    for _ in range(11):
        sk._clump(back, 574 + rngw.random() * 104, 606 + rngw.random() * 62, 11, 7, 34, rngw, OCC, S[4])
    # Far to near, so the nearer stand in front; each a little smaller the further up the road he is. The road
    # climbs away up and to the left here, so the four walk facing left, rimmed on the side toward his light. On
    # their own canvas, laid over the mid layer after the vignette: they walk in the middle of the frame, and the
    # vignette's dither set a lattice of dots over their flat robes.
    walkers = Cv(FW, FH)
    for k_, (who, py_) in enumerate(party):
        px_, _ = min(pts, key=lambda p: abs(p[1] - py_))
        x_ = px_ + (5 if k_ % 2 else -5)
        hgt = 24 + (py_ - 458) * 0.1
        if who is None:
            player_back(walkers, x_, py_, hgt)
        else:
            to = (him_at[0] - x_, (him_at[1] - 18) - (py_ - hgt * 0.5))
            n = math.hypot(*to) or 1.0
            roster_figure(walkers, who, x_, py_, hgt, [walking[who[0]], OCC], strip="moving",
                          frame=(py_ // 12) % 4, facing=-1, toward=(to[0] / n, to[1] / n, G[1]))
    heath(fore, 640, 840, 700, 43, [(OCC, OCC), (OCC, S[4])], scale=1.8)
    # the wood's near boughs on the fore, trunks well off the road, crowns under the player's row
    for i_, (tx2, ty2, th2, sp2) in enumerate([(334, 748, 88, 52), (408, 710, 48, 30),
                                               (596, 716, 52, 33), (668, 752, 92, 56)]):
        sk.tree(fore, tx2, ty2, th2 * 2.0, sp2 * 2.0, seed=70 + i_, dark=OCC, light=OCC, vines=False)
    layers = dict(back=back, mid=mid, fore=fore)
    finish(layers, 0.8)
    over(mid, walkers)
    return layers


# The story's order (Eric, 2026-11-08): the tide after the procession - "He took every wizard who might rival him.",
# then "There was no one left to defend them." - and the five folded into the road ("remove this, combine with the
# last panel"), so seven panels where there were eight.
PANELS = [("moot", p_moot), ("throne", p_throne), ("taken", p_taken), ("tide", p_tide),
          ("missed", p_missed), ("break", p_break), ("road", p_road)]

# The animated panels' boxes, clocks and atlas widths (write_anim): the four lifted one by one, the
# tide's rush over the crest, the hiding's searchers, and the chain breaking.
ANIMS = {"missed": (MISSED_BOX, MISSED_FPS, MISSED_COLUMNS), "break": (BREAK_BOX, BREAK_FPS, 4),
         "tide": (TIDE_BOX, TIDE_FPS, TIDE_COLUMNS), "throne": (THRONE_BOX, THRONE_FPS, THRONE_COLUMNS)}


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


# The moments a panel's preview strip shows, in seconds, and the rows it crops, where the ticks and rows below
# do not suit it: the throne's action fills the square both screens show, from its top.
PREVIEW_AT = {"throne": ([0.8, 1.6, 2.3, 3.0, 3.8, 5.4], (240, 600))}


def preview_anim(name, layers, frames, ticks, rows=(360, 600)):
    """A few ticks of the animated panel, the PC's view, one above the other."""
    box = ANIMS[name][0]
    flat = composite(layers)
    shots = []
    for k in ticks:
        im = flat.copy()
        im.paste(frames[k], box[:2], frames[k])
        for n in ("fore",):
            im.paste(layers[n].img, (0, 0), layers[n].img)
        shots.append(im.crop((VIEW_TALL[0], rows[0], VIEW_TALL[2], rows[1])))
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
        if name == "tide":
            print("tide    the drop lands at art %s, %.1f s: Prologue.cs's impact for the tide must match"
                  % (tide_land_art(), TIDE_LAND))
        if do_preview:
            preview(name, layers, frames[-1] if frames else None)
            if frames and name in PREVIEW_AT:
                at, rows = PREVIEW_AT[name]
                fps = ANIMS[name][1]
                preview_anim(name, layers, frames, [min(len(frames) - 1, int(s * fps)) for s in at], rows)
            elif frames:
                preview_anim(name, layers, frames, [k for k in [4, 12, 18, 22, 30, 38] if k < len(frames)])
        print("%-7s %d off-contract pixels%s" % (name, sum(bad.values()),
                                                   (" " + str(sorted(bad.items())[:4])) if bad else ""))
    return 1 if total_bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
