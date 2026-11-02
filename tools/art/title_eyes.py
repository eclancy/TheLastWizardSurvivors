# -*- coding: utf-8 -*-
"""The eyes in the dark on the title screen: what a pair looks like, where the pairs go, and the
frames a pair blinks through. Shared by splash.vigil (the phone) and splash_wide.vigil_wide (a PC);
anim.py writes the cell sheet and the positions beside each layer set, and scripts/TitleScreen.cs
lays the cells over the picture and blinks them. Read .ai/title-screen.md, "The eyes in the dark".

2026-11-01, Eric: "The eyes in the background don't always look like eyes. They should all be
circular and blink, and be randomly spread out rather than some being on top of each other."
Three faults, one per sentence:

  - SHAPE. Each eye was a radius-3 `radial` on a three-stop ramp, and a radial dithers its stops,
    so every eye came out a different blob - a square, a plus, a bar - and with the two eyes 3-5
    apart at radius 3 a pair often ran together into one smear. The treeline set was then drawn
    BEHIND the leaves and the ground, so a pair half under a clump was a crescent or a dash, and
    the vignette swapped pixels out of the ones near the edges. Now: one fixed disc, both eyes
    level and GAP apart, stamped after every other pass, and only where nothing drawn in front of
    the eyes (the ward, the flames, the figure, the foreground) comes near it. Nothing dithers,
    darkens or covers a pair, so every pair on screen is the same shape.
  - BLINK. Six whole-screen frames each shut one pair, so five of sixteen pairs ever blinked, and
    only one at a time. Now each pair is its own small cell laid over the still picture, with its
    own clock (TitleScreen.cs). Still by frame, never by tint: a cell steps through generated
    frames in contract colours, and shut is simply no cell.
  - SPREAD. Nothing kept two pairs apart, so random draws landed on top of one another. Now every
    pair keeps SPACING from every other, and each is the best of a few random spots - the one
    furthest from the rest (Mitchell's best candidate) - so the set spreads out without looking
    laid out on a grid.

Positions come from rng.random() only, never randrange, which Python 3 changed: the pairs land
in the same places under 2.7 and 3.
"""

import json
import math
import os
import random

from PIL import ImageChops, ImageFilter

import bonelight as bl
import raster

M = bl.MATERIALS
E = bl.ELEMENTS

# One eye: a 4x4 disc with its corners off, the smallest shape on this grid that reads as round.
# A 3x3 is a plus or a square, which is what half the old eyes were. `r` is the rim, `c` the core.
OPEN = (".rr.",
        "rccr",
        "rccr",
        ".rr.")
# The blink, frame by frame: open, the lid half down, a slit, then shut (no cell at all), and back.
# The lid comes down from the top, so the lower edge of the eye is the last of it to go - a squint
# on the way, which is the right note for something watching from the trees.
FRAMES = (OPEN,
          ("....",
           "....",
           "rccr",
           ".rr."),
          ("....",
           "....",
           "....",
           ".rr."))
FRAME_NAMES = ("open", "half", "slit")
EYE_W = 4
GAP = 3                                   # between the two eyes of a pair; every pair the same
CELL = (EYE_W * 2 + GAP, len(OPEN))       # 11 x 4

# Two tones, one shape. The pairs up in the branches keep the old note - a step down the fire ramp
# so they read red, not amber: amber cores once turned the wood into a firefly meadow. The pairs in
# the treeline go a step further down, to ember, so they sit further off by colour alone. Both are
# drawn at the same depth (see the layers below), so the tone is the only depth cue a pair has.
DEPTHS = ("far", "near")
TONES = {"far": {"r": M["ember"][3], "c": E["fire"][3]},
         "near": {"r": E["fire"][3], "c": E["fire"][2]}}

# The layers drawn in front of the eyes (splash.STAGES order). No pair may come within COVER_PAD of
# anything on them, nor within FIGURE_PAD of the figure: a pair is never on the wizard, and one at
# 8 sat among the sparks over the orb and read as two more of them.
ABOVE = ("ward", "flamesA", "figure", "flamesB", "fore")
COVER_PAD = 1
FIGURE_PAD = 14

# Every pair keeps this far (an ellipse, x and y, centre to centre) from every other. A crowded
# region relaxes it rather than hanging, and says so - which means the count wants lowering.
SPACING = (30.0, 22.0)
# How many valid spots are drawn for each pair before the one furthest from the rest is kept.
# More spreads the set more evenly; past about five it starts to look laid out on a grid.
CANDIDATES = 5
# A pair goes only on dark wood. Over its cell and DARK_PAD round it, the mean luma (0-255) must be
# at most DARK_MEAN and no pixel lighter than DARK_MAX. The mean keeps a pair off the sky round the
# wordmark (about 70 there) and out of the lit undergrowth; the ceiling keeps it off a star, the
# type and the brightest leaf tops, while still letting it sit among leaves with a lit edge or two
# (stone.base and the violet and lichen tops are 37-50) - wood with no edge light near it at all
# is too little of the frame, and the pairs all crowded into what there was.
DARK_MEAN = 22
DARK_MAX = 50
DARK_PAD = 2
# Keep-out boxes (the wordmark, the menu, the prompt) are kept this far clear of the cell.
KEEP_PAD = 6


def luma(p):
    return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]


def stamp(c, x, y, depth, frame=0):
    """Draw one pair, cell top-left at (x, y), in `depth`'s tones, in blink frame `frame`."""
    tones = TONES[depth]
    for ox in (0, EYE_W + GAP):
        for j, row in enumerate(FRAMES[frame]):
            for i, ch in enumerate(row):
                if ch != ".":
                    c.set(x + ox + i, y + j, tones[ch])


def stamp_all(c, pairs, dy=0):
    """Stamp every pair, open, onto the `eyes` canvas (in a flat render, the one canvas). A pair
    touches nothing drawn in front of it, so stamping it last gives the same picture as stamping
    it in its place in the draw order."""
    for p in pairs:
        stamp(c, p["x"], p["y"] + dy, p["depth"])


def composite(cv, names):
    """The given layers of a layered render, composited in order."""
    out = None
    for n in names:
        im = cv[n].img
        if out is None:
            out = im.copy()
        else:
            out.paste(im, (0, 0), im)
    return out


def _alpha(cv, names):
    out = None
    for n in names:
        a = cv[n].img.split()[3]
        out = a if out is None else ImageChops.lighter(out, a)
    return out


def _grow(mask, pad):
    return mask.filter(ImageFilter.MaxFilter(2 * pad + 1)) if pad else mask


def blocked(cv):
    """Where no pixel of a pair may go: everything in front of the eyes, grown by COVER_PAD, and the
    figure grown by FIGURE_PAD."""
    return ImageChops.lighter(_grow(_alpha(cv, ABOVE), COVER_PAD),
                              _grow(_alpha(cv, ["figure"]), FIGURE_PAD))


def _box_hits(x0, y0, x1, y1, boxes, pad):
    for b in boxes:
        if x0 - pad <= b[2] and b[0] <= x1 + pad and y0 - pad <= b[3] and b[1] <= y1 + pad:
            return True
    return False


class Field(object):
    """What placement needs to know about a finished render: how light it is everywhere behind the
    eyes, where it is blocked, which boxes the UI keeps, and where a screen's edge can fall.

    `seams` are columns where some screen shape cuts the art off (the boundary is just left of the
    column). No cell may straddle one, or that screen shows one eye and a half - a dot at its
    edge, not a pair."""

    def __init__(self, under, block, keep_out, seams=()):
        self.w, self.h = under.size
        lum = under.convert("RGB").load()
        bpx = block.load()
        # One number per pixel: its luma, or 999 where a pair may not go at all.
        self.v = [[999 if bpx[x, y] else luma(lum[x, y]) for x in range(self.w)]
                  for y in range(self.h)]
        self.keep_out = keep_out
        self.seams = seams

    def ok(self, x, y):
        cw, ch = CELL
        if x < 0 or y < 0 or x + cw > self.w or y + ch > self.h:
            return False
        if any(x <= s <= x + cw for s in self.seams):     # a pixel's margin either side
            return False
        if _box_hits(x, y, x + cw - 1, y + ch - 1, self.keep_out, KEEP_PAD):
            return False
        for yy in range(y, y + ch):           # blocked: the cell itself (the pads are in the mask)
            row = self.v[yy]
            for xx in range(x, x + cw):
                if row[xx] >= 999:
                    return False
        total = 0.0
        count = 0
        for yy in range(max(0, y - DARK_PAD), min(self.h, y + ch + DARK_PAD)):
            row = self.v[yy]
            for xx in range(max(0, x - DARK_PAD), min(self.w, x + cw + DARK_PAD)):
                v = row[xx]
                if v >= 999:
                    continue                  # blocked pixels in the pad: the cell itself is clear
                if v > DARK_MAX:
                    return False
                total += v
                count += 1
        return count == 0 or total / count <= DARK_MEAN


def place(field, groups, seed, spacing=SPACING, caps=()):
    """Choose a spot for every pair.

    `groups` is a list of (depth, (x0, y0, x1, y1), n): n pairs in `depth`'s tones whose cells lie
    wholly inside the box. `caps` is a list of ((x0, y0, x1, y1), most): a region that may hold at
    most `most` pairs across every group, counting a pair whose centre is inside it. Returns a list
    of {"x", "y", "depth"}, (x, y) the cell's top-left, sorted so the file is stable."""
    rng = random.Random(seed)
    cw, ch = CELL
    pairs = []

    def centre(p):
        return p["x"] + cw * 0.5, p["y"] + ch * 0.5

    def inside(b, c):
        return b[0] <= c[0] <= b[2] and b[1] <= c[1] <= b[3]

    for depth, (bx0, by0, bx1, by1), n in groups:
        sx, sy = spacing
        for k in range(n):
            got = None
            while got is None:
                found = []
                tries = 0
                while len(found) < CANDIDATES and tries < 4000:
                    tries += 1
                    p = {"x": bx0 + int(rng.random() * (bx1 - cw + 2 - bx0)),
                         "y": by0 + int(rng.random() * (by1 - ch + 2 - by0)), "depth": depth}
                    if not field.ok(p["x"], p["y"]):
                        continue
                    c = centre(p)
                    near = min([math.sqrt(((c[0] - o[0]) / sx) ** 2 + ((c[1] - o[1]) / sy) ** 2)
                                for o in [centre(q) for q in pairs]] or [99.0])
                    if near < 1.0:
                        continue
                    if any(inside(b, c) and sum(1 for q in pairs if inside(b, centre(q))) >= most
                           for b, most in caps):
                        continue
                    found.append((near, p["x"], p["y"]))
                if found:
                    best = max(found)
                    got = {"x": best[1], "y": best[2], "depth": depth}
                elif sx < 8:
                    raise ValueError("title_eyes: no room for %s pair %d of %d in %s"
                                     % (depth, k + 1, n, (bx0, by0, bx1, by1)))
                else:
                    sx, sy = sx * 0.85, sy * 0.85
                    print("title_eyes: relaxed the spacing to %.0f x %.0f for %s pair %d of %d"
                          % (sx, sy, depth, k + 1, n))
            pairs.append(got)
    return sorted(pairs, key=lambda p: (DEPTHS.index(p["depth"]), p["y"], p["x"]))


def report(pairs):
    """One line: how many, and how close the closest two are, centre to centre."""
    best = None
    for i, a in enumerate(pairs):
        for b in pairs[i + 1:]:
            d = math.hypot(a["x"] - b["x"], a["y"] - b["y"])
            best = d if best is None else min(best, d)
    return "%d pairs (%d far, %d near), closest two %.1f px apart" % (
        len(pairs), sum(1 for p in pairs if p["depth"] == "far"),
        sum(1 for p in pairs if p["depth"] == "near"), best or 0.0)


def covered(pairs, cvs):
    """Pairs that something in front of the eyes touches in ANY of the given layered renders (one
    per animation pass: the flames, the ward pools and the orb all move). Placement looked at frame
    0 only; this checks that every other frame agrees. Empty is the only right answer."""
    cw, ch = CELL
    bad = []
    for p in pairs:
        hit = False
        for cv in cvs:
            for n in ABOVE:
                px = cv[n].px
                for yy in range(max(0, p["y"] - COVER_PAD), min(cv[n].h, p["y"] + ch + COVER_PAD)):
                    for xx in range(max(0, p["x"] - COVER_PAD),
                                    min(cv[n].w, p["x"] + cw + COVER_PAD)):
                        if px[xx, yy][3]:
                            hit = True
        if hit:
            bad.append(p)
    return bad


def write(out_dir, pairs, art_size):
    """eyes.png - one row per depth (far, near), one column per frame (open, half, slit), each cell
    CELL with a one-pixel clear gutter - and eyes.json, where every pair sits. TitleScreen.cs reads
    both; nothing in code knows a position. Returns the sheet's off-contract colours."""
    cw, ch = CELL
    pw, ph = cw + 1, ch + 1
    sheet = raster.Canvas(pw * len(FRAMES) - 1, ph * len(DEPTHS) - 1)
    for row, depth in enumerate(DEPTHS):
        for f in range(len(FRAMES)):
            stamp(sheet, f * pw, row * ph, depth, f)
    sheet.save(os.path.join(out_dir, "eyes.png"))
    lines = ['{',
             ' "generator": "tools/art/title_eyes.py, via tools/art/anim.py - do not edit by hand",',
             ' "art": [%d, %d],' % art_size,
             ' "cell": [%d, %d],' % (cw, ch),
             ' "pitch": [%d, %d],' % (pw, ph),
             ' "frames": [%s],' % ", ".join('"%s"' % n for n in FRAME_NAMES),
             ' "rows": [%s],' % ", ".join('"%s"' % d for d in DEPTHS),
             ' "pairs": [']
    for i, p in enumerate(pairs):
        lines.append('  {"x": %d, "y": %d, "depth": "%s"}%s'
                     % (p["x"], p["y"], p["depth"], "," if i < len(pairs) - 1 else ""))
    lines += [' ]', '}', '']
    text = "\n".join(lines)
    json.loads(text)                      # it must parse: TitleScreen reads it with System.Text.Json
    with open(os.path.join(out_dir, "eyes.json"), "wb") as f:
        f.write(text.encode("ascii"))     # LF endings, and the same bytes under 2.7 and 3
    return sheet.audit()
