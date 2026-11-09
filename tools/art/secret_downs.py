# -*- coding: utf-8 -*-
"""The White Downs' secrets: the two hill figures and their tells. Run: python tools/art/secret_downs.py

    world/downs/figure-hound.png         the Hound in the Hill: Laelaps, the hound that never failed to catch
                                         what it ran after, cut in the chalk at full stretch. Its eye is a
                                         round hollow in the chalk, dark inside: the near tell (the warm
                                         glint) is drawn there in code
    world/downs/figure-hart-grown.png    the Hart in the Hill as the downs have it: the turf crept back over
                                         the cut, the strokes broken into short dim lengths
    world/downs/figure-hart-scoured.png  the same hart scoured clean, the strokes whole and pale. The two are
                                         pixel-registered: one is drawn exactly over the other
    world/downs/figure-hart-knot.png     a clump of turf lying across a line of the hart: the code draws one
                                         on every knot still to be scoured, centred on its point
    world/downs/figures.json             sizes, the hound's eye and the hart's knots, in art pixels
    world/downs/tell-chalk-1..3.png      the far tell: small fresh chalk spills where the turf has been cut
    world/downs/tell-near-1..2.png       the near band: a sod cut and turned over, a fresh cut line
    tools/art/_downs_event/secret_downs_preview.png  (gitignored) on the night turf, for looking at

WHAT A HILL FIGURE IS. The Uffington White Horse, and the giants and horses like it: a picture cut through
the turf of a chalk down into the white beneath, a few long flowing strokes, so abstract that it reads as
an animal only as a whole - a body that is one sweep, legs that do not always meet it, a head that is
mostly an eye. It lasts only while it is scoured: left alone, the turf creeps back over it in a few
decades and it is gone. That is the whole of both secrets. The hound is cut and kept, and its eye is the
light he missed; the hart has been let go, and the player scours it back by treading out each clump of
turf that lies across its lines.

HOW IT IS DRAWN. Each stroke is a Catmull-Rom spline through a few points with a width at each, stamped
as discs along its length, so a stroke swells and tapers as a cut line does. Chalk at night is the bone
row's base, quiet against turf in lichen's deep and shade; the cut's upper-left edge is a line of its
shade, the shadow the turf lip throws into the trench under a key from the upper left; worn patches in
shade and a few flints in deep go through it. Nothing is lit and nothing is warm: the figure is the
palest thing on that ground, and the glint in the hound's eye must be the only warm point near it.

Saved at x2 like every world sprite: a PNG pixel pair is one art pixel. Every coordinate in figures.json
is in ART pixels from the image's top-left, so it is doubled for the PNG and for world units. They lie
flat under everything (ground decals): a figure has no contact line and casts nothing.
"""
from __future__ import division, print_function

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

import bonelight as bl
import raster

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "downs")
SCRATCH = os.path.join(ROOT, "tools", "art", "_downs_event")
PREVIEW = os.path.join(SCRATCH, "secret_downs_preview.png")
SCALE = 2
OCC = bl.OCC


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


SK = ramp("skin")      # chalk: bone, what the moon is made of
LN = ramp("linen")     # dry grass, the turf creeping back
LI = ramp("lichen")    # turf and moss
ST = ramp("stone")     # the thin dark soil over the chalk


def th(x, y):
    return raster.DITHER[y % 8][x % 8] / 64.0


def hashed(k, salt):
    v = (k * 2654435761 + salt * 40503 + 977) % 4294967296
    v = (v ^ (v >> 13)) * 1274126177 % 4294967296
    return (v % 100003) / 100003.0


# --- strokes -------------------------------------------------------------------------------------------

def catmull(pts, steps):
    """Points along a Catmull-Rom spline through pts [(x, y, w), ...], width interpolated with them."""
    ext = [pts[0]] + list(pts) + [pts[-1]]
    out = []
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        for k in range(steps):
            t = k / float(steps)
            t2, t3 = t * t, t * t * t
            v = []
            for j in range(2):
                v.append(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2
                                + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3))
            # width: smooth between the two points
            s = t * t * (3 - 2 * t)
            v.append(p1[2] + (p2[2] - p1[2]) * s)
            out.append(tuple(v))
    out.append(tuple(pts[-1]))
    return out


class Cut(object):
    """A figure's strokes rasterised: for each pixel, which stroke, how far along it (art px) and how far
    from its middle (0 at the centre line, 1 at the edge)."""

    def __init__(self, w, h):
        self.w = w
        self.h = h
        self.px = {}
        self.lengths = []
        self.lines = []

    def stroke(self, pts):
        sid = len(self.lengths)
        samples = catmull(pts, 60)
        # arc length along the samples
        s = 0.0
        prev = None
        line = []
        for (x, y, w) in samples:
            if prev is not None:
                s += math.hypot(x - prev[0], y - prev[1])
            prev = (x, y)
            line.append((x, y, w / 2.0, s))
        self.lengths.append(s)
        self.lines.append(line)
        for (x, y, r, sp) in line:
            for py in range(int(math.floor(y - r)) - 1, int(math.ceil(y + r)) + 1):
                for px in range(int(math.floor(x - r)) - 1, int(math.ceil(x + r)) + 1):
                    if not (0 <= px < self.w and 0 <= py < self.h):
                        continue
                    d = math.hypot(px + 0.5 - x, py + 0.5 - y)
                    if d > r:
                        continue
                    k = d / max(r, 0.5)
                    cur = self.px.get((px, py))
                    if cur is None or k < cur[2]:
                        self.px[(px, py)] = (sid, sp, k)
        return sid

    def point(self, sid, frac):
        """The centre line's point a fraction of the way along a stroke, in whole art pixels."""
        line = self.lines[sid]
        want = self.lengths[sid] * frac
        for (x, y, r, sp) in line:
            if sp >= want:
                return (int(round(x - 0.5)), int(round(y - 0.5)))
        x, y, r, sp = line[-1]
        return (int(round(x - 0.5)), int(round(y - 0.5)))

    def clear(self, pred):
        for key in [k for k in self.px if pred(k)]:
            del self.px[key]


def edge_ul(px, x, y):
    return (x - 1, y) not in px or (x, y - 1) not in px or (x - 1, y - 1) not in px


def chalk(cut, salt=0, worn=0.025):
    """The scoured chalk: base, its upper-left edge in shade (the turf lip's shadow in the cut), worn
    patches of shade and a few flints of deep through it."""
    c = raster.Canvas(cut.w, cut.h, None)
    for (x, y), (sid, sp, k) in cut.px.items():
        tone = SK[2]
        if edge_ul(cut.px, x, y):
            tone = SK[3]
        else:
            h = hashed(x * 7 + y * 131, salt)
            # worn: clumps rather than single pixels, from a coarse cell hash
            hc = hashed((x // 3) * 17 + (y // 2) * 293, salt + 11)
            if hc < worn * 3 and h < 0.55:
                tone = SK[3]
            elif h < 0.010:
                tone = SK[4]                       # a flint
        c.set(x, y, tone)
    return c


# --- the Hound in the Hill -------------------------------------------------------------------------------

HW, HH = 300, 140


def hound_cut():
    cut = Cut(HW, HH)
    # The body, neck and head are ONE sweep, as the White Horse's are: from the rump along a back flexed
    # up by the run, thinning at the loin, deepening at the chest (a sighthound is all chest), up the neck
    # and out along the head to the nose. The head is wide at the eye and long in the muzzle.
    cut.stroke([(60, 78, 7), (96, 60, 9), (132, 52, 8), (168, 54, 12), (194, 60, 15), (212, 56, 11),
                (224, 46, 10), (236, 40, 16), (252, 42, 11), (266, 47, 7), (280, 52, 3)])
    # The jaw, a short line under the muzzle, close: the mouth just open on the run.
    cut.stroke([(246, 53, 5), (258, 57, 4), (266, 59, 2)])
    # The ear: pricked, a blade rising off the back of the skull and swept back - the one stroke that
    # says hound and not bird.
    cut.stroke([(230, 36, 8), (226, 26, 6), (219, 15, 2)])
    # The forelegs, streaming forward; the far one does not meet the body (the chalk figures' way).
    cut.stroke([(212, 72, 7), (232, 82, 6), (254, 88, 6), (268, 86, 4), (274, 80, 2)])
    cut.stroke([(198, 82, 6), (216, 98, 5), (238, 108, 5), (252, 108, 3), (256, 103, 2)])
    # The hind legs, thrown back: the near one from the rump, the far one apart from it.
    cut.stroke([(70, 82, 8), (52, 94, 6), (30, 100, 6), (16, 98, 4), (11, 92, 2)])
    cut.stroke([(92, 86, 6), (76, 104, 5), (52, 118, 5), (38, 120, 3), (32, 115, 2)])
    # The tail: a long thin stroke sweeping up and back from the rump.
    cut.stroke([(58, 70, 6), (42, 56, 5), (30, 42, 4), (16, 34, 3), (6, 32, 1)])
    return cut


# The eye: a hollow centred on a pixel corner, so the code's 2x2 glint sits exactly in it. The hollow is
# the twelve pixels within 2 of the corner; a ring of chalk round it; a ring of turf round that, which is
# what makes it an eye cut into the head rather than a dark spot on it.
HOUND_EYE = (237, 39)


def eye_hole(cut, c, ex, ey):
    for y in range(ey - 7, ey + 7):
        for x in range(ex - 7, ex + 7):
            d = math.hypot(x + 0.5 - ex, y + 0.5 - ey)
            if d <= 2.0:
                # the pit: its upper-left wall in the dark, its floor and lower-right wall a stop up
                c.set(x, y, OCC if (x + 0.5 - ex) + (y + 0.5 - ey) < 0.6 else SK[4])
            elif d <= 2.9:
                c.set(x, y, SK[3])               # the lip of the pit, in shadow
            elif d <= 4.4:
                c.set(x, y, SK[2] if (x + 0.5 - ex) + (y + 0.5 - ey) > -3.6 else SK[3])
            elif d <= 5.6:
                if 0 <= x < c.w and 0 <= y < c.h:
                    c.px[x, y] = (0, 0, 0, 0)        # the ring of turf left standing round the eye
            elif d <= 6.4 and (x, y) in cut.px:
                # the head's chalk round the turf ring: its edge toward the eye in shadow on the far side
                if (x + 0.5 - ex) + (y + 0.5 - ey) > 0:
                    c.set(x, y, SK[3])


def hound():
    cut = hound_cut()
    c = chalk(cut, salt=3)
    ex, ey = HOUND_EYE
    eye_hole(cut, c, ex, ey)
    return c


# --- the Hart in the Hill --------------------------------------------------------------------------------

AW, AH = 340, 220


def hart_cut():
    cut = Cut(AW, AH)
    s = {}
    # The body and neck in one sweep: leaping, rising from the haunch along the back to the chest, and
    # turning up the neck to the head.
    s["body"] = cut.stroke([(108, 148, 8), (132, 126, 11), (170, 110, 12), (204, 104, 13), (226, 100, 11),
                            (240, 86, 10), (250, 72, 9), (256, 64, 10)])
    # The head, the muzzle out to the right and ending blunt.
    s["head"] = cut.stroke([(250, 66, 11), (264, 62, 12), (280, 66, 9), (293, 73, 6)])
    # The antlers: two beams sweeping back from the crown, tines rising off them. The near beam:
    s["beam"] = cut.stroke([(254, 58, 7), (246, 40, 6), (232, 24, 6), (212, 14, 5), (190, 12, 3)])
    s["tine1"] = cut.stroke([(244, 38, 5), (254, 26, 4), (258, 14, 2)])
    s["tine2"] = cut.stroke([(226, 21, 5), (230, 10, 3), (229, 4, 2)])
    s["tine3"] = cut.stroke([(206, 13, 4), (200, 6, 2)])
    # The far beam, lower and shorter, sweeping further back.
    s["beam2"] = cut.stroke([(246, 60, 6), (232, 50, 5), (212, 44, 5), (192, 42, 4), (176, 46, 2)])
    s["tine4"] = cut.stroke([(222, 47, 4), (218, 36, 3), (216, 30, 2)])
    s["tine5"] = cut.stroke([(198, 42, 4), (190, 32, 2)])
    # The forelegs folded under the chest as it clears the ground; the far one apart.
    s["fore"] = cut.stroke([(232, 118, 8), (254, 130, 7), (264, 148, 6), (258, 162, 5), (248, 166, 2)])
    s["fore2"] = cut.stroke([(216, 128, 6), (234, 146, 6), (238, 166, 5), (230, 178, 2)])
    # The hind legs driving back and down off the ground.
    s["hind"] = cut.stroke([(112, 156, 9), (90, 174, 7), (64, 194, 6), (46, 204, 4), (38, 200, 2)])
    s["hind2"] = cut.stroke([(132, 160, 7), (116, 184, 6), (96, 204, 5), (82, 212, 3), (76, 208, 2)])
    # A short tail flicked up off the haunch.
    s["tail"] = cut.stroke([(104, 138, 6), (92, 126, 4), (84, 120, 2)])
    return cut, s


# The knots: places along the lines where a clump of turf lies across them. Spread over the whole figure,
# so scouring it is a walk round the hart and not a stand in one place.
KNOTS = [("body", 0.34), ("body", 0.84), ("beam", 0.52), ("fore", 0.45), ("hind", 0.5), ("hind2", 0.62),
         ("beam2", 0.45)]
HART_EYE = (265, 61)


def hart_knots(cut, s):
    return [cut.point(s[name], frac) for (name, frac) in KNOTS]


def small_eye(c, ex, ey, grown):
    """The hart's eye: a smaller hollow, the same cut as the hound's, with no light to give."""
    for y in range(ey - 4, ey + 4):
        for x in range(ex - 4, ex + 4):
            d = math.hypot(x + 0.5 - ex, y + 0.5 - ey)
            if d <= 1.5:
                c.set(x, y, OCC if not grown else LN[4])
            elif d <= 2.4:
                c.set(x, y, SK[3] if not grown else SK[4])


def hart_scoured():
    cut, s = hart_cut()
    c = chalk(cut, salt=5)
    small_eye(c, HART_EYE[0], HART_EYE[1], False)
    return c, cut, s


def hart_grown(cut, s, knots):
    """The turf back over the cut: strokes eaten narrow from their edges and broken along their length into
    short dim lengths of skin deep and linen shade, with a little chalk still showing in their cores. Every
    pixel is one the scoured figure has, so the two register exactly. Under each knot the line is kept,
    so the clump the code draws lies across a line and not in a gap."""
    c = raster.Canvas(cut.w, cut.h, None)
    near_knot = {}
    for (kx, ky) in knots:
        for y in range(ky - 9, ky + 10):
            for x in range(kx - 12, kx + 13):
                near_knot[(x, y)] = True
    for (x, y), (sid, sp, k) in cut.px.items():
        # How much of the line's width is still open here: three waves of different length along it,
        # phased per stroke, so it is whole in places, a thread in others, gone in others - never a
        # dash pattern (a regular break reads as a dashed line someone drew, not as turf growing back).
        a = hashed(sid, 61) * 6.283
        b = hashed(sid, 63) * 6.283
        cc = hashed(sid, 65) * 6.283
        n = 0.52 + 0.30 * math.sin(sp * 0.19 + a) + 0.20 * math.sin(sp * 0.47 + b) + 0.12 * math.sin(sp * 1.1 + cc)
        open_ = max(0.0, min(1.0, (n - 0.26) * 1.9))
        if near_knot.get((x, y), False):
            open_ = max(open_, 0.8)
        if k > open_ * 0.9 + (th(x, y) - 0.5) * 0.18:
            continue
        h = hashed(x * 13 + y * 197, 73)
        if k < 0.4 and open_ > 0.55 and h < 0.30:
            tone = SK[3]                           # chalk still showing in the core
        elif h < 0.30 + 0.45 * k + 0.25 * (1.0 - open_):
            tone = LN[3]                           # turf over it
        else:
            tone = SK[4]
        c.set(x, y, tone)
    small_eye(c, HART_EYE[0], HART_EYE[1], True)
    return c


# --- the knot: a clump of turf lying across a line -----------------------------------------------------

KW, KH = 24, 16


def knot():
    """A tussock of turf, rounded, lit on its upper left: dry grass in linen over moss in lichen, its blades
    standing out of the top, a little chalk crumbled out from under its edges, a dark seam where it sits.
    Not directional: the code lays it on lines at every angle."""
    c = raster.Canvas(KW, KH, None)
    cx, cy, rx, ry = 11.5, 9.5, 9.0, 5.0
    top_of = {}
    # The tussock: three lobes of dry grass in linen, the back two first, each lit on its upper left, so
    # it is a clump and not a dome. A column steps a stop now and then - the blades running up it - so it
    # is grass and not a stone. Its top edge is ragged column by column.
    lobes = [(8.0, 8.4, 5.6, 4.0), (15.0, 8.0, 5.4, 4.4), (11.5, 10.2, 8.6, 3.4)]
    owner = {}
    for li, (lx, ly, lrx, lry) in enumerate(lobes):
        for y in range(KH):
            for x in range(KW):
                nx = (x + 0.5 - lx) / lrx
                ny = (y + 0.5 - ly) / lry
                d = nx * nx + ny * ny
                rag = 0.18 * hashed(x * 3 + li, 5) if ny < 0 else 0.0
                if d <= 1.0 - rag:
                    owner[(x, y)] = (li, nx, ny, d)
    for (x, y), (li, nx, ny, d) in owner.items():
        v = (-nx - ny) * 0.62 + (1.0 - d) * 0.3 - (0.25 if li < 2 else 0.0)
        i = 1 if v > 0.5 else (2 if v > -0.1 else 3)
        if hashed(x, 7 + li) > 0.68 and ny > -0.6:
            i = min(4, i + 1)                         # a dark gap between blades
        c.set(x, y, LN[i])
    for x in range(KW):
        col = [y for y in range(KH) if (x, y) in owner]
        if not col:
            continue
        top_of[x] = col[0]
        # the moss at the foot, two rows of lichen under the grass
        for y in col[-2:]:
            nx = (x + 0.5 - cx) / rx
            c.set(x, y, LI[2] if nx < 0.1 else LI[3])
    # a few blades standing above the tufts, the tallest catching the key
    for (x, h) in ((7, 2), (10, 2), (15, 1)):
        if x in top_of:
            for j in range(1, h + 1):
                c.set(x + (1 if j == h and x > 11 else 0), top_of[x] - j, LN[1] if x < 12 else LN[2])
    # the seam where it sits: occlusion under its lower edge, heavier to the right
    for x in range(4, 21):
        nx = (x + 0.5 - cx) / rx
        y = int(cy + ry * 0.55 * math.sqrt(max(0.0, 1.0 - nx * nx))) + 1
        c.set(x, y, OCC)
        if nx > 0.3:
            c.set(x, y - 1, LI[4])
    # chalk showing at its two ends, where the line runs in under it
    for (x, y, t) in ((2, 12, SK[2]), (3, 12, SK[3]), (1, 11, SK[3]), (21, 12, SK[2]), (22, 11, SK[3]),
                      (20, 13, SK[3])):
        c.set(x, y, t)
    return c


# --- the tells -----------------------------------------------------------------------------------------

def bordered(c):
    """A clear pixel added on every side: a decal drawn to the edge of its box would end on the box's line
    when the code centres it (secret_circle.bordered)."""
    out = raster.Canvas(c.w + 2, c.h + 2, None)
    out.img.paste(c.img, (1, 1))
    out.px = out.img.load()
    return out


def spill(k):
    """The far tell: a little fresh chalk where the turf has been cut - a scrape with its lip of turf turned
    back, lumps thrown out of it, a dusting of chalk on the grass. Three sizes; thickening toward a figure
    is the code's business (a gradient, never a ring)."""
    sizes = [(16, 10), (22, 12), (30, 14)]
    w, h = sizes[k]
    c = raster.Canvas(w, h, None)
    cx, cy = w * 0.36, h * 0.48
    rx, ry = w * 0.24, h * 0.27
    # The scrape: an irregular gouge of bare chalk, wider along the stroke of whatever cut it, its edge
    # ragged; its upper-left wall in the turf's shadow, the floor base, a few crumbs of fresh white.
    inside = {}
    for y in range(h):
        for x in range(w):
            nx = (x + 0.5 - cx) / rx
            ny = (y + 0.5 - cy) / ry
            ang = math.atan2(ny, nx)
            r = 1.0 + 0.22 * math.sin(ang * 3 + k * 1.7) + 0.12 * math.sin(ang * 5 + 0.4 + k)
            if math.hypot(nx, ny) <= r:
                inside[(x, y)] = True
    for (x, y) in inside:
        if (x - 1, y) not in inside or (x, y - 1) not in inside:
            tone = SK[3]
        elif hashed(x * 11 + y * 7, 13 + k) < 0.16:
            tone = SK[1]
        else:
            tone = SK[2]
        c.set(x, y, tone)
    # The cut turf's lip along the upper left of the scrape: a fringe of soil with moss lit on top of it.
    for (x, y) in inside:
        if (x, y - 1) not in inside and (x - 1, y) in inside and hashed(x, 29 + k) < 0.7:
            c.set(x, y - 1, ST[4])
            if hashed(x, 31 + k) < 0.6:
                c.set(x, y - 2, LI[1])
    # Chalk thrown out to the lower right, the way the spade went: crumbs, and a lump or two lit on top.
    for j in range(5 + k * 4):
        a = -0.6 + hashed(j, 17 + k) * 2.0
        d = 1.15 + hashed(j, 19 + k) * 0.9
        x = int(cx + math.cos(a) * rx * d)
        y = int(cy + math.sin(a) * ry * d)
        c.set(x, y, SK[2] if j % 3 else SK[3])
    for j in range(1 + k):
        a = 0.1 + hashed(j, 37 + k) * 1.1
        x = int(cx + math.cos(a) * (rx + 2.5 + j * 2))
        y = int(cy + math.sin(a) * (ry + 1.5))
        c.set(x, y, SK[1])
        c.set(x + 1, y, SK[2])
        c.set(x, y + 1, SK[2])
        c.set(x + 1, y + 1, SK[3])
        c.set(x + 1, y + 2, OCC)
    return c


def sod(k):
    """The near band: turf freshly cut. 1: a sod cut out and turned over beside its hole, the chalk white
    in the hole and the sod's dark soil and roots upward. 2: a fresh cut line, the turf along it lifted
    and the chalk showing in the slit."""
    if k == 0:
        w, h = 28, 16
        c = raster.Canvas(w, h, None)
        # The hole: a spade's cut, straight sides a little out of square. The far (upper) wall is in shadow
        # and steep, the chalk floor is bare, the near lip catches the key.
        quad = [(3.0, 5.0), (13.5, 3.6), (14.6, 11.4), (4.0, 12.4)]
        hole = raster.Canvas(w, h, None)
        hole.poly(quad, SK[2])
        hp = hole.img.load()
        cells = [(x, y) for y in range(h) for x in range(w) if hp[x, y][3]]
        cell = set(cells)
        for (x, y) in cells:
            if (x, y - 1) not in cell:
                tone = SK[4]
            elif (x, y - 2) not in cell:
                tone = SK[3]
            elif (x - 1, y) not in cell:
                tone = SK[3]
            elif (x, y + 1) not in cell:
                tone = SK[1]
            else:
                tone = SK[2] if hashed(x * 3 + y * 29, 41) > 0.14 else SK[1]
            c.set(x, y, tone)
            # the turf's cut face: a line of soil just outside the top and left of the hole
            if (x, y - 1) not in cell:
                c.set(x, y - 1, ST[4])
            if (x - 1, y) not in cell:
                c.set(x - 1, y, ST[4])
        # chalk trodden out of the hole onto the grass
        for (x, y) in ((15, 10), (16, 12), (3, 14), (8, 14), (11, 14), (6, 13)):
            c.set(x, y, SK[2])
        c.set(9, 14, SK[3])
        # The sod, turned over beside it and lying aslant: a slab of the downs' thin black soil uppermost,
        # mottled in clumps (soil does not dither), chalk crumbs clinging, pale root threads; its grass
        # showing as a fringe along the under edge; a contact line under all of it.
        slab = [(17.0, 6.4), (25.4, 7.6), (25.0, 13.4), (16.4, 12.4)]
        sc = raster.Canvas(w, h, None)
        sc.poly(slab, ST[3])
        spx = sc.img.load()
        scells = set((x, y) for y in range(h) for x in range(w) if spx[x, y][3])
        for (x, y) in scells:
            corner = sum(1 for n in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)) if n not in scells) >= 2
            if corner:
                continue                                      # a slab of turf has no square corners
            if (x, y + 1) not in scells or (x + 1, y) not in scells:
                tone = LI[2] if (x + y) % 3 else LI[3]       # the grass, now underneath, showing at its edges
            elif (x, y - 1) not in scells or (x - 1, y) not in scells:
                tone = ST[1]                                  # the cut edge, lit
            else:
                blot = hashed((x // 2) * 7 + (y // 2) * 53, 43)
                tone = ST[3] if blot > 0.35 else ST[4]
            c.set(x, y, tone)
        # tufts of its grass sticking out past the under edge and the far side
        for (x, y) in ((26, 9), (26, 11), (25, 14), (21, 14), (18, 13)):
            c.set(x, y, LI[2])
        for (x, y) in ((19, 9), (23, 9), (21, 11)):
            c.set(x, y, SK[3])
        for (x0, y0, x1, y1) in ((18, 10, 20, 11), (22, 8, 24, 10)):
            c.line(x0, y0, x1, y1, LN[1])
        for (x, y) in scells:
            if (x, y + 1) not in scells and (x, y + 1) not in cell:
                c.set(x, y + 1, OCC)
        return c
    w, h = 26, 14
    c = raster.Canvas(w, h, None)
    # a fresh straight cut through the turf, a little aslant
    pts = [(3 + i, int(round(9 - i * 0.22))) for i in range(20)]
    for i, (x, y) in enumerate(pts):
        c.set(x, y, OCC)
        c.set(x, y + 1, SK[2] if 3 < i < 17 else SK[3])     # chalk in the slit's near wall
        if 5 < i < 15 and i % 3:
            c.set(x, y + 2, SK[3])
    # the turf along it lifted, a flap peeled back above the cut: moss lit on top, its soil edge under
    for i in range(6, 16):
        x, y = pts[i]
        c.set(x, y - 1, ST[4])
        c.set(x, y - 2, LI[1] if i % 2 else LI[2])
        if 8 <= i <= 13:
            c.set(x, y - 3, LI[2])
    # chalk crumbs kicked out of it
    for (x, y) in ((22, 6), (24, 8), (5, 11), (13, 11), (17, 10)):
        c.set(x, y, SK[2])
    c.set(23, 7, SK[3])
    return c


# --- out ----------------------------------------------------------------------------------------------

def audit(name, c):
    bad = c.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in %s: %s" % (name, bad[:5]))
        return False
    warm = warm_pixels(c)
    if warm:
        print("WARM PIXELS in %s: %s" % (name, warm[:5]))
        return False
    return True


WARM_ROWS = ("gold", "flesh", "red", "ember", "amber")


def warm_pixels(c):
    """Anything from a warm row or a warm element: the glint must be the only warm point on these."""
    warm = set()
    for row in WARM_ROWS:
        warm |= set(t.upper() for t in bl.MATERIALS[row])
    for el in ("fire", "light", "earth", "lightning"):
        warm |= set(t.upper() for t in bl.ELEMENTS[el])
    bad = {}
    for y in range(c.h):
        for x in range(c.w):
            p = c.get(x, y)
            if p[3]:
                hx = "#%02X%02X%02X" % p[:3]
                if hx in warm:
                    bad[hx] = bad.get(hx, 0) + 1
    return sorted(bad.items())


def turf(w, h):
    """The night turf of the downs, for the preview: lichen deep and shade, a few pale chalk patches."""
    c = raster.Canvas(w, h, bl.tone("lichen", "deep"))
    for y in range(h):
        for x in range(w):
            v = 0.5 + 0.25 * math.sin(x * 0.021 + y * 0.013) + 0.2 * math.sin(x * 0.007 - y * 0.031)
            if v > 0.5 + th(x, y) * 0.5:
                c.set(x, y, bl.tone("lichen", "shade"))
    for (px, py, rx, ry) in ((w * 0.07, h * 0.2, 14, 6), (w * 0.93, h * 0.55, 18, 7), (w * 0.5, h * 0.96, 12, 5)):
        for y in range(int(py - ry), int(py + ry) + 1):
            for x in range(int(px - rx), int(px + rx) + 1):
                d = math.hypot((x - px) / rx, (y - py) / ry)
                if d < 1.0 and (1.0 - d) * 2.0 > th(x, y):
                    c.set(x, y, SK[3])
    return c


def main():
    for d in (OUT, SCRATCH):
        if not os.path.isdir(d):
            os.makedirs(d)
    ok = True
    sheets = []

    h = hound()
    ok = audit("figure-hound", h) and ok
    h.scaled(SCALE).save(os.path.join(OUT, "figure-hound.png"))

    scoured, cut, s = hart_scoured()
    knots = hart_knots(cut, s)
    grown = hart_grown(cut, s, knots)
    ok = audit("figure-hart-scoured", scoured) and ok
    ok = audit("figure-hart-grown", grown) and ok
    # registration: every grown pixel lies on the scoured figure
    sp = scoured.img.load()
    gp = grown.img.load()
    stray = sum(1 for y in range(AH) for x in range(AW) if gp[x, y][3] and not sp[x, y][3])
    if stray:
        print("grown hart has %d pixels off the scoured figure" % stray)
        ok = False
    scoured.scaled(SCALE).save(os.path.join(OUT, "figure-hart-scoured.png"))
    grown.scaled(SCALE).save(os.path.join(OUT, "figure-hart-grown.png"))

    k = knot()
    ok = audit("figure-hart-knot", k) and ok
    k.scaled(SCALE).save(os.path.join(OUT, "figure-hart-knot.png"))

    meta = {
        "hound": {"file": "figure-hound.png", "w": HW, "h": HH, "eye": list(HOUND_EYE)},
        "hart": {"grown": "figure-hart-grown.png", "scoured": "figure-hart-scoured.png", "w": AW, "h": AH,
                 "knots": [list(p) for p in knots], "knot": "figure-hart-knot.png", "knot_w": KW, "knot_h": KH},
    }
    with open(os.path.join(OUT, "figures.json"), "w") as fh:
        fh.write(json.dumps(meta, sort_keys=True) + "\n")
    print("  hound %dx%d art px, eye %s; hart %dx%d art px, %d knots %s"
          % (HW, HH, HOUND_EYE, AW, AH, len(knots), knots))

    tells = []
    for i in range(3):
        c = bordered(spill(i))
        ok = audit("tell-chalk-%d" % (i + 1), c) and ok
        c.scaled(SCALE).save(os.path.join(OUT, "tell-chalk-%d.png" % (i + 1)))
        tells.append(("tell-chalk-%d" % (i + 1), c))
    for i in range(2):
        c = bordered(sod(i))
        ok = audit("tell-near-%d" % (i + 1), c) and ok
        c.scaled(SCALE).save(os.path.join(OUT, "tell-near-%d.png" % (i + 1)))
        tells.append(("tell-near-%d" % (i + 1), c))
    for name, c in tells:
        print("  %-12s %dx%d art px" % (name, c.w, c.h))

    # The previews, at x2 (game size) on the night turf. The hound with a stand-in for the code's glint in
    # its eye (preview only); the hart grown with a knot on every point, and scoured; the tells at x2 and
    # again, with the knot, at x6.
    pad = 24
    hsheet = turf(HW * SCALE + pad * 2, HH * SCALE + pad * 2).img
    hsheet.alpha_composite(h.scaled(SCALE), (pad, pad))
    gx, gy = pad + HOUND_EYE[0] * SCALE, pad + HOUND_EYE[1] * SCALE
    glint = [(dx, dy, "#FFF0C0") for dx in (-1, 0) for dy in (-1, 0)]
    glint += [(dx, dy, "#D8B04A") for (dx, dy) in ((-2, -1), (-2, 0), (1, -1), (1, 0), (-1, -2), (0, -2), (-1, 1), (0, 1))]
    for (dx, dy, col) in glint:
        for yy in range(2):
            for xx in range(2):
                hsheet.putpixel((gx + dx * 2 + xx, gy + dy * 2 + yy), raster.rgb(col))
    hsheet.save(PREVIEW.replace(".png", "_hound.png"))

    asheet = turf(AW * SCALE * 2 + pad * 3, AH * SCALE + pad * 2).img
    asheet.alpha_composite(grown.scaled(SCALE), (pad, pad))
    kimg = k.scaled(SCALE)
    for (kx, ky) in knots:
        asheet.alpha_composite(kimg, (pad + kx * SCALE - KW, pad + ky * SCALE - KH))
    asheet.alpha_composite(scoured.scaled(SCALE), (pad * 2 + AW * SCALE, pad))
    asheet.save(PREVIEW.replace(".png", "_hart.png"))

    row2 = sum(c.w * SCALE + 16 for _n, c in tells)
    row6 = sum(c.w * 6 + 16 for _n, c in tells + [("knot", k)])
    tsheet = turf(max(row2, row6) + pad * 2, pad * 3 + 40 + 6 * 20).img
    x = pad
    for name, c in tells:
        tsheet.alpha_composite(c.scaled(SCALE), (x, pad))
        x += c.w * SCALE + 16
    x = pad
    for name, c in tells + [("knot", k)]:
        tsheet.alpha_composite(c.scaled(6), (x, pad * 2 + 40))
        x += c.w * 6 + 16
    tsheet.save(PREVIEW.replace(".png", "_tells.png"))
    print("previews: tools/art/_downs_event/secret_downs_preview_{hound,hart,tells}.png")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
