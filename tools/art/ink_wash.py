# -*- coding: utf-8 -*-
"""INK: the one sanctioned departure from Bonelight - Ma Liang's ink, painted as watercolour.

Eric, 2026-11-09: "make sure the ink spells are a very different style of art than other parts of the game. the
art-drawn crow familiar should also look significantly different, almost watercolor style ink blots". So what Ma Liang's
brush makes is not lit by the room's key and is not drawn in the contract's ramps: it is ink on wet paper, in one ink of
its own, with real alpha. The rules are .ai/art-direction.md, "Ink: the one sanctioned departure"; this is the one copy of
them a generator runs. Used by the familiar's ink looks (tools/art/familiar.py, "MA LIANG'S CROW"), and written for the
next ink spell (Ink Ring) to paint through as well, so the two are one hand.

WHAT A GENERATOR GIVES IT. A frame as LAYERS: for every pixel, the washes laid on it from first to last, each a pigment
DENSITY (0 is bare paper, 1 is ink pooled as deep as it goes), the GROUP it belongs to (one brush stroke: a wing, the
body, the hat) and the part-local coordinates it rides with. Plus the crisp things that are not ink (DETAILS: the seal,
the eye, an element's light), and where the painter dropped extra ink into the wet (DROPS), where water ran back into a
drying wash (BLOOMS), the splatter flung off the brush (SPLATS), and the flyer's shadow. paint() does the rest:

  - OVERLAPPING WASHES. A wash is transparent: what lies under it shows through at SHOW_THROUGH of its density, edges
    included, so a wing behind the body is a darker ghost inside the body's wash, as two glazes are.
  - THE TIDE LINE. Pigment in a drying wash runs to its edge, so every stroke is paler in the middle and dark at its rim,
    and darkest on its far side (away from the key, where it pooled): TIDE_HARD at a hard edge, TIDE_INNER where one
    stroke lies over another, TIDE_PART for the faint lines inside one stroke (the feathers of a wing).
  - THE BLEED. Elsewhere the edge was still wet and the ink feathered OUT past the shape: a pale, translucent fringe one or
    two pixels wide, and no tide line there. Which edges bled and which dried hard is noise (SOFT_SHARE of them bleed).
  - THE BOIL. That noise is seeded per frame, so the wet margins creep from frame to frame - a living painting - while
    the inside of each stroke (its granulation) is keyed to the stroke's own coordinates and rides with it.
  - GRANULATION. Pigment settling in the paper's tooth: a scatter of darker grains and a soft mottle through every wash.
  - BLOOMS and DROPS: a backrun (a pale centre inside a ragged dark ring) and a wet-in-wet drop (a dark feathered blot).
  - SPLATTER: a few dots of ink flung off the brush, beside the stroke.

THE PALETTE. One ink, INK_TONES: twelve stops from wet paper to ink pooled black, nearly neutral - a breath warm in the
thin washes, a breath blue in the pooled black - which is exactly what Bonelight forbids ("nothing dark is allowed to be
grey") and so exactly what makes it read as a painting and not as a lit thing. Alpha comes in ALPHA_STEPS (eighths):
the bleed is translucent, the thin washes nearly opaque, the strokes and the tide lines opaque. Anything crisp on top
(the vermilion seal, the amber eye, a Stormcrow's lightning) stays a contract colour and opaque: the ink is the creature,
the element is the spell. audit() holds a frame to exactly that, and nothing else in the project is let off the contract.
"""
from __future__ import division, print_function

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import bonelight as B
import raster

# --- the ink ----------------------------------------------------------------------------------------------------------
# Wet paper to ink pooled black. Index = round(density * 11). A breath warm at the paper end (rice paper under a thin
# wash), a breath blue at the black end (dried sumi), and grey between: Bonelight's ramps are all chromatic and this is
# not, which is the point. None of these is a reserved colour (#DCE8FF, #A8B8DC, #232C46) and none is in the contract.
INK_TONES = [
    "#E6E0D4",   # 0  wet paper: only ever the thinnest bleed
    "#D2CDC3",   # 1
    "#BCB8B1",   # 2  a thin wash
    "#A4A19E",   # 3
    "#8C8A8B",   # 4
    "#757479",   # 5  a mid wash; the middle of a stroke
    "#5F5E66",   # 6
    "#4B4A54",   # 7
    "#393843",   # 8  a tide line
    "#282733",   # 9
    "#191823",   # 10
    "#0D0C15",   # 11 ink pooled as deep as it goes - the only black
]

# Alpha in eighths, from the outer bleed (2/8) to opaque. A pixel that is not opaque must be ink, never a contract colour.
ALPHA_STEPS = (64, 96, 128, 160, 192, 224, 255)

# How much of a wash shows through the one laid over it (a glaze is transparent; a little of the lower stroke's body
# and its tide line show, so two overlapping washes read as two).
SHOW_THROUGH = 0.34
# THE TIDE LINE (the coffee ring): pigment in a drying wash runs to its edge. Added to a stroke's density on its outer
# edge where it dried hard (and a third of that one pixel further in), where it lies over another stroke inside the
# figure, and - faintly, on some pixels - between the parts of one stroke (the feathers of a wing). A stroke's middle,
# three pixels and more from any edge, gives up MIDDLE_LIFT to its rim: it is paler for it.
TIDE_HARD = 0.4
TIDE_INNER = 0.26
TIDE_PART = 0.08
MIDDLE_LIFT = 0.05
# Extra on the far side of an edge (away from the key, down and right): where the pigment pooled as it dried.
POOL = 0.12
# The share of the silhouette's edge that was still wet and bled outward instead of drying to a tide line.
SOFT_SHARE = 0.5
# The boil's grain, in pixels: how big a run of edge bleeds or dries together.
BOIL_SCALE = 2.6
# WET-IN-WET: a slow, wide wander of density through each stroke (ink running where the paper was wettest), in the
# stroke's own coordinates, and the passes of smoothing that turn the key's five tones into a wash's gradient.
WANDER = 0.12
WANDER_SCALE = 4.0
SMOOTH_PASSES = 2
# Granulation: pigment settled in the paper's tooth - a sparse scatter of distinctly darker grains.
GRAIN_SHARE = 0.09
GRAIN = 0.1


def _rgb(h):
    return (int(h[1:3], 16), int(h[3:5], 16), int(h[5:7], 16))


INK_RGB = [_rgb(h) for h in INK_TONES]
_INK_SET = set(h.upper() for h in INK_TONES)


def hsh(x, y, salt=0):
    """A stable hash of a lattice point: 0..1."""
    h = (int(x) * 374761393 + int(y) * 668265263 + int(salt) * 1442695041) & 0xffffffff
    h = ((h ^ (h >> 13)) * 1274126177) & 0xffffffff
    return ((h ^ (h >> 16)) & 0xffff) / 65535.0


def salt_of(name):
    """A stable integer salt for a stroke's name (a group), so its grain does not move when parts are added before it."""
    s = 0
    for ch in str(name):
        s = (s * 131 + ord(ch)) & 0xffffff
    return s


def vnoise(x, y, seed, scale):
    """Smooth value noise: 0..1, coherent over `scale` pixels."""
    fx, fy = x / scale, y / scale
    x0, y0 = int(math.floor(fx)), int(math.floor(fy))
    tx, ty = fx - x0, fy - y0
    tx = tx * tx * (3 - 2 * tx)
    ty = ty * ty * (3 - 2 * ty)
    a = hsh(x0, y0, seed)
    b = hsh(x0 + 1, y0, seed)
    c = hsh(x0, y0 + 1, seed)
    d = hsh(x0 + 1, y0 + 1, seed)
    return (a * (1 - tx) + b * tx) * (1 - ty) + (c * (1 - tx) + d * tx) * ty


def tone(d):
    """The ink stop for a density."""
    return max(0, min(len(INK_TONES) - 1, int(round(d * (len(INK_TONES) - 1)))))


def alpha_step(a):
    """The nearest allowed alpha at or under `a` (0..1), or 0 below the first step."""
    v = a * 255.0
    best = 0
    for s in ALPHA_STEPS:
        if s <= v + 1e-6:
            best = s
    return best


N4 = ((1, 0), (-1, 0), (0, 1), (0, -1))
N8 = ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1))


class Layer(object):
    """One wash on one pixel: its density, the stroke it belongs to, and the stroke-local coordinates it rides with."""
    __slots__ = ("d", "group", "part", "lx", "ly", "wash")

    def __init__(self, d, group, part, lx, ly, wash=False):
        self.d = d
        self.group = group
        self.part = part
        self.lx = lx
        self.ly = ly
        # A thin wash (the body) rather than a stroke: where water runs back (BLOOMS) and where a drop spreads widest.
        self.wash = wash


def paint(cell, layers, details=None, blooms=(), drops=(), splats=(), shadow=(), seed=0, hard_groups=(), margin=1,
          soft_share=None):
    """One frame of ink, as a raster.Canvas (RGBA, off the Bonelight contract by design - audit() below is its check).

    layers   {(x, y): [Layer, ...]}, first laid to last
    details  {(x, y): ("ink", density) | ("light", "#RRGGBB")}: crisp, opaque, over everything (the top of each pixel)
    blooms   [(cx, cy, r)]: backruns in the thin washes - a pale centre and a ragged dark ring
    drops    [(cx, cy, r)]: ink dropped into the wet - a dark blot feathering out
    splats   [(x, y, size)]: dots of ink flung off the brush
    shadow   {(x, y)}: the flyer's detached shadow, laid as a soft puddle of thin ink
    seed     the frame's boil: which edges bled and which dried hard
    hard_groups  strokes whose edges never bleed (the hat, the beak: the read that must stay crisp)
    margin   the clear border the bleed and the splatter keep inside the cell
    soft_share  the share of the edge still wet (SOFT_SHARE when None). Ink Ring's line (tools/art/ink_ring.py) dries
             as it ages: fresh from the brush most of its edge is wet and bleeds; dried, most of it is a tide line.
    """
    details = details or {}
    share = SOFT_SHARE if soft_share is None else soft_share
    canvas = raster.Canvas(cell, cell)
    px = canvas.img.load()

    gmask = {}
    pmask = {}
    for p, ls in layers.items():
        for L in ls:
            gmask.setdefault(L.group, set()).add(p)
            pmask.setdefault(L.part, set()).add(p)
    body = set(layers)
    for p, v in details.items():
        if v[0] == "ink":
            body.add(p)
    hard = set(hard_groups)

    def soft(x, y):
        return vnoise(x + 0.5, y + 0.5, seed * 7 + 3, BOIL_SCALE) < share

    # --- every pixel inside the figure: the washes laid on it, glazed --------------------------------------------------
    top_of = dict((p, ls[-1]) for p, ls in layers.items())

    # The top stroke's own ink at each pixel, before its edges: the key's tone, the wet-in-wet wander, then smoothed
    # within the stroke so the five tones run together as a wash does (never across into another stroke).
    base = {}
    for p, L in top_of.items():
        base[p] = L.d + (vnoise(L.lx + 40, L.ly + 40, salt_of(L.group) + 5, WANDER_SCALE) - 0.5) * 2 * WANDER
    for _ in range(SMOOTH_PASSES):
        nxt = {}
        for p, v in base.items():
            x, y = p
            grp = top_of[p].group
            tot, wt = v * 2.0, 2.0
            for dx, dy in N8:
                q = (x + dx, y + dy)
                if q in base and top_of[q].group == grp:
                    tot += base[q]
                    wt += 1.0
            nxt[p] = tot / wt
        base = nxt

    def edge_ink(L, x, y):
        """What a stroke's edges add at (x, y): the tide line, by how far the pixel is from the stroke's edge."""
        gm = gmask[L.group]
        g_edge = [(dx, dy) for (dx, dy) in N4 if (x + dx, y + dy) not in gm]
        if g_edge:
            out = [(dx, dy) for (dx, dy) in g_edge if (x + dx, y + dy) not in body]
            if out:
                # The silhouette. Dried hard to a tide line, or still wet and bled outward (no line: the fringe outside
                # carries it) - unless it is a stroke that must stay crisp.
                if L.group in hard or not soft(x, y):
                    return TIDE_HARD + (POOL if any(dx > 0 or dy > 0 for (dx, dy) in out) else 0.0)
                return 0.0
            return TIDE_INNER + (POOL * 0.5 if any(dx > 0 or dy > 0 for (dx, dy) in g_edge) else 0.0)
        # One pixel in: a third of the hard tide, where the edge beside it dried hard.
        near = [(x + dx, y + dy) for (dx, dy) in N8 if (x + dx, y + dy) not in gm]
        if not near:
            ring2 = any((x + dx * 2, y + dy * 2) not in gm for (dx, dy) in N4)
            if not ring2:
                return -MIDDLE_LIFT
        else:
            if any(q not in body for q in near) and (L.group in hard or not soft(x, y)):
                return TIDE_HARD * 0.33
        if any((x + dx, y + dy) not in pmask[L.part] for (dx, dy) in N4) and hsh(L.lx, L.ly, 97) < 0.6:
            return TIDE_PART
        return 0.0

    D = {}
    for p, ls in layers.items():
        x, y = p
        under = 0.0
        for L in ls[:-1]:
            under = max(under, L.d + edge_ink(L, x, y))
        L = ls[-1]
        top = base[p] + edge_ink(L, x, y)
        # Granulation, keyed to the stroke's own coordinates so it rides with it.
        if hsh(L.lx, L.ly, salt_of(L.group)) < GRAIN_SHARE:
            top += GRAIN
        D[p] = top + SHOW_THROUGH * under * (1.0 - min(1.0, top) * 0.5)

    # --- backruns and drops --------------------------------------------------------------------------------------------
    for k, (cx, cy, r) in enumerate(blooms):
        for p in D:
            if not top_of[p].wash:
                continue
            x, y = p
            ddx, ddy = x + 0.5 - cx, y + 0.5 - cy
            dist = math.sqrt(ddx * ddx + ddy * ddy)
            a = math.atan2(ddy, ddx)
            # A ragged rim: the radius wanders round the bloom, as a backrun's cauliflower edge does.
            bucket = int((a + math.pi) / (2 * math.pi) * 9) % 9
            rr = r * (0.78 + 0.44 * hsh(bucket, k, 61))
            if dist < rr - 0.9:
                D[p] -= 0.07
            elif dist < rr + 0.6:
                D[p] += 0.22
    for k, (cx, cy, r) in enumerate(drops):
        for p in D:
            x, y = p
            ddx, ddy = x + 0.5 - cx, y + 0.5 - cy
            dist = math.sqrt(ddx * ddx + ddy * ddy)
            # Feathered: the reach wanders with the noise, so the drop's edge is soft and uneven, never a disc.
            reach = r * (0.75 + 0.5 * vnoise(x, y, 71 + k, 1.6))
            if dist < reach:
                D[p] += 0.3 * (1.0 - dist / reach) ** 0.6

    out = {}
    for p, d in D.items():
        d = max(0.06, min(1.0, d))
        out[p] = (tone(d), alpha_step(min(1.0, 0.62 + 0.62 * d)))

    # --- the bleed: outside the figure, where the edge was wet ---------------------------------------------------------
    lo, hi = margin, cell - 1 - margin
    ring1 = {}
    for p in D:
        x, y = p
        for dx, dy in N8:
            q = (x + dx, y + dy)
            if q in body or q in shadow or q in ring1:
                continue
            if not (lo <= q[0] <= hi and lo <= q[1] <= hi):
                continue
            if not soft(q[0], q[1]):
                continue
            src = [D[(q[0] + ex, q[1] + ey)] for (ex, ey) in N8 if (q[0] + ex, q[1] + ey) in D
                   and top_of[(q[0] + ex, q[1] + ey)].group not in hard]
            if len(src) < 2:
                continue
            ring1[q] = sum(src) / len(src)
    for q, d in ring1.items():
        out[q] = (tone(d * 0.62), 128 if d > 0.55 else 96)
    ring2 = {}
    for q, d in ring1.items():
        for dx, dy in N4:
            r2 = (q[0] + dx, q[1] + dy)
            if r2 in body or r2 in shadow or r2 in ring1 or r2 in ring2:
                continue
            if not (lo <= r2[0] <= hi and lo <= r2[1] <= hi):
                continue
            if vnoise(r2[0] + 0.5, r2[1] + 0.5, seed * 7 + 3, BOIL_SCALE) < share * 0.55:
                ring2[r2] = d
    for q, d in ring2.items():
        out[q] = (tone(d * 0.45), 64)

    # --- splatter -----------------------------------------------------------------------------------------------------
    for k, (sx, sy, size) in enumerate(splats):
        x0, y0 = int(round(sx)), int(round(sy))
        pts = [(x0, y0)]
        if size >= 2:
            # A drop of two or three pixels, its tail boiling from frame to frame.
            pts.append((x0 + 1, y0))
            tail = N4[int(hsh(k, seed, 83) * 4)]
            pts.append((x0 + tail[0], y0 + tail[1]))
        for q in pts:
            if lo <= q[0] <= hi and lo <= q[1] <= hi and q not in body:
                out[q] = (tone(0.88 if q == (x0, y0) else 0.7), 255 if q == (x0, y0) else 192)

    # --- the shadow: a thin puddle on the floor, its ends drying ragged ------------------------------------------------
    if shadow:
        xs = [p[0] for p in shadow]
        x_lo, x_hi = min(xs), max(xs)
        mid = (x_lo + x_hi) / 2.0
        half = max(1.0, (x_hi - x_lo) / 2.0)
        for p in shadow:
            if p in out:
                continue
            t = abs(p[0] + 0.5 - mid) / half
            if t > 0.8 and hsh(p[0], p[1], seed + 91) < 0.4:
                continue
            # Dark ink, thin: it must darken every floor it lies on, the palest and the darkest alike.
            out[p] = (tone(0.92 - 0.1 * t), 128 if t < 0.55 else 96)

    for (x, y), (t, a) in out.items():
        if a <= 0:
            continue
        r, g, b = INK_RGB[t]
        px[x, y] = (r, g, b, a)
    # The crisp things over everything: the seal, the eye, the element's light, and pooled ink dots.
    for (x, y), v in details.items():
        if not (0 <= x < cell and 0 <= y < cell):
            continue
        if v[0] == "ink":
            r, g, b = INK_RGB[tone(v[1])]
        else:
            r, g, b = _rgb(v[1])
        px[x, y] = (r, g, b, 255)
    return canvas


def faded(canvas, steps):
    """The same ink `steps` alpha steps thinner: a wash drying away to a ghost of itself (Ink Ring's old ink). Every pixel
    stays an ink stop at an allowed alpha, and one that falls under the first step is gone - so a fade is still ink."""
    if steps <= 0:
        return canvas
    px = canvas.img.load()
    for y in range(canvas.h):
        for x in range(canvas.w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            k = ALPHA_STEPS.index(a) if a in ALPHA_STEPS else len(ALPHA_STEPS) - 1
            k -= steps
            px[x, y] = (r, g, b, ALPHA_STEPS[k]) if k >= 0 else (0, 0, 0, 0)
    return canvas


def opaque(canvas, floor=160):
    """The ink laid opaque: a pixel at `floor` alpha or more kept whole, any thinner one dropped. For ink laid OVER an
    opaque picture (Ink Ring's card icons, on their milestone's night): a translucent wash there would blend into a colour
    that is neither an ink stop nor the night's, so the bleed is let go and the stroke kept."""
    px = canvas.img.load()
    for y in range(canvas.h):
        for x in range(canvas.w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            px[x, y] = (r, g, b, 255) if a >= floor else (0, 0, 0, 0)
    return canvas


def audit(img):
    """Off-rule pixels in an ink frame: [(hex, alpha, count)]. An opaque pixel may be any ink stop or any contract colour
    (the seal, the eye, an element's light); a translucent one must be an ink stop at an allowed alpha. The reserved
    lights (#DCE8FF, #A8B8DC, #232C46) are pigment nowhere, here included."""
    ok = raster.allowed()
    reserved = set(h.upper() for h in (B.RIM, B.RIM_SOFT, B.AMBIENT))
    bad = {}
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            hx = "#%02X%02X%02X" % (r, g, b)
            if hx in reserved:
                good = False
            elif a == 255:
                good = hx in _INK_SET or hx in ok
            else:
                good = hx in _INK_SET and a in ALPHA_STEPS
            if not good:
                bad[(hx, a)] = bad.get((hx, a), 0) + 1
    return sorted(((k[0], k[1], n) for k, n in bad.items()), key=lambda t: -t[2])


def swatch(path, zoom=12):
    """The palette and its alphas over the darkest and the palest floors, for looking at."""
    from PIL import Image
    floors = [(11, 15, 24), (30, 38, 52), (36, 64, 74), (56, 64, 79), (221, 213, 192)]
    w = len(INK_TONES) * zoom
    im = Image.new("RGBA", (w, len(floors) * len(ALPHA_STEPS) * zoom), (0, 0, 0, 255))
    for fi, f in enumerate(floors):
        for ai, a in enumerate(ALPHA_STEPS):
            y0 = (fi * len(ALPHA_STEPS) + ai) * zoom
            for ti, rgb in enumerate(INK_RGB):
                tile = Image.new("RGBA", (zoom, zoom), f + (255,))
                tile.alpha_composite(Image.new("RGBA", (zoom, zoom), rgb + (a,)))
                im.paste(tile, (ti * zoom, y0))
    im.save(path)


if __name__ == "__main__":
    out = os.path.join(HERE, "_ink_swatch.png")
    swatch(out)
    print("wrote", out)
