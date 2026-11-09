# -*- coding: utf-8 -*-
"""The Silver Hind, the White Downs' side-event quarry. Run: python tools/art/silver_hind.py

    world/downs/silver-hind.png    eight 48-cell frames in one strip, facing RIGHT, hooves on row 46
                                   (the contact line is row 47), saved 1:1 like every actor sheet and
                                   drawn at the contract's x2:
                                       0 graze        head down, cropping the turf
                                       1 graze        the head a pixel lower, the tail flicked
                                       2 alert        head up high, ears up and cupped forward
                                       3 alert-turn   head up and turned to look out at the viewer
                                       4 5 6 bound    gathered (hind feet driving), stretched in the air,
                                                      landing on the forefeet
                                       7 stand        standing proud, head high, a forefoot raised
    world/downs/silver-hind.json   the cell, the feet row, the frames by name, and the eye of frame 2
    tools/art/_downs_event/silver_hind_preview.png   (gitignored) on the night turf, at x2 and at x6

WHO SHE IS. Artemis's hind, waiting on the downs for the moon to rise. The player stalks her: comes on
while she grazes, freezes when she lifts her head, keeps the barrows between them. She is the one living
thing on the downs the dark wizard did not take, and nothing about her may say that he did.

So she is the opposite of his creatures in each of the three ways .ai/world-and-tone.md names:

  NOT FROM THE DEEP HALF OF A RAMP. The horde is unlit. She is the palest living thing on the downs,
  moon-coloured - the `skin` row, which is bone and "what the moon is made of" (bonelight.py) - carried
  in its lit and base stops, the key landing on her back and haunch and her belly and far legs falling to
  shade. Against a night turf of lichen deep and shade she is the brightest body on the ground.
  NO EYE-LIGHT. Every taken thing stares with two points of cold arcane; she has a dark eye, a point of
  occlusion with skin deep beside it, and no glint in it at all.
  NOT WARM EITHER. Warm light on the downs is the secrets' and the player's. She is cold and pale and
  serious: not a spirit made of light (the Pale Hart in the forest is that) but an animal the light
  falls on, so she carries the rim and the matte finish like any actor.

HOW SHE IS BUILT: masses first (foes_forest.py: "a table is a rectangle and four lines"). A haunch, a
barrel and a chest as three lit ellipses, the haunch and chest drawn over the barrel so each casts its
own crease; a long neck as a tapering capsule; a narrow head with a long muzzle and the big ears a hind
has instead of antlers, which are her silhouette's attachment (filled black: no rack, two tall leaves).
The legs are jointed capsule chains - the hind leg's gaskin running down and BACK to the point of the
hock, the cannon down to a small dark hoof - paired close under each mass with a wide gap between pairs.
Every form is lit by one light (key from the upper left, art-direction.md section 2) and dithered
between adjacent stops of the row, so it turns rather than stacks; a form in front darkens what lies
just below and right of it, which is the occlusion that separates overlapping forms (section 4b).
"""
from __future__ import division, print_function

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

import bonelight as bl
import matte
import raster

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "downs")
SCRATCH = os.path.join(ROOT, "tools", "art", "_downs_event")
PREVIEW = os.path.join(SCRATCH, "silver_hind_preview.png")

CELL = 48
FEET = 46            # the hooves' lowest row; the contact line is the row under it
OCC = bl.OCC
RIM = bl.RIM
SK = [bl.tone("skin", r) for r in ("hi", "lit", "base", "shade", "deep")]

# The key, from the upper left and a little toward the viewer (art-direction.md section 2).
_L = (-0.62, -0.72, 0.38)
_LN = math.sqrt(sum(c * c for c in _L))
KEY = tuple(c / _LN for c in _L)

# Light value -> a position on the skin row (0 hi .. 4 deep). The flank facing the viewer lands
# between lit and base; the crown of the back reaches hi; the belly and the far side fall to shade.
TONE_A = 2.30
TONE_B = 1.85


def th(x, y):
    return raster.DITHER[y % 8][x % 8] / 64.0


class Fig(object):
    """A figure built from lit primitives. Each primitive gets an order number; later ones are in front.
    A pixel keeps (tone position, order, kind) and is resolved to a colour at the end."""

    def __init__(self, w=CELL, h=CELL):
        self.w = w
        self.h = h
        self.px = {}
        self.n = 0
        self.kinds = {}

    def _light(self, nx, ny, nz):
        m = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
        return (nx * KEY[0] + ny * KEY[1] + nz * KEY[2]) / m

    def _put(self, x, y, t, kind):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[(x, y)] = (t, self.n, kind)

    def ellipse(self, cx, cy, rx, ry, ang=0.0, bias=0.0, kind="body", cast=0.9, depth=1.0):
        """A lit ellipsoid seen side-on. depth < 1 flattens it toward the viewer (a broad flank)."""
        self.n += 1
        self.kinds[self.n] = (kind, cast)
        ca, sa = math.cos(ang), math.sin(ang)
        R = max(rx, ry) + 1.0
        for y in range(int(math.floor(cy - R)), int(math.ceil(cy + R)) + 1):
            for x in range(int(math.floor(cx - R)), int(math.ceil(cx + R)) + 1):
                dx = x + 0.5 - cx
                dy = y + 0.5 - cy
                u = (dx * ca + dy * sa) / rx
                w = (-dx * sa + dy * ca) / ry
                d2 = u * u + w * w
                if d2 > 1.0:
                    continue
                nx = u * ca - w * sa
                ny = u * sa + w * ca
                nz = math.sqrt(max(0.0, 1.0 - d2)) / depth
                v = self._light(nx, ny, nz)
                self._put(x, y, TONE_A - TONE_B * v + bias, kind)

    def capsule(self, pts, bias=0.0, kind="limb", cast=0.9):
        """A tapering tube through pts [(x, y, r), ...]: legs, neck, muzzle, ears, tail."""
        self.n += 1
        self.kinds[self.n] = (kind, cast)
        best = {}
        for (x0, y0, r0), (x1, y1, r1) in zip(pts, pts[1:]):
            R = max(r0, r1) + 1.0
            sx, sy = x1 - x0, y1 - y0
            L2 = sx * sx + sy * sy or 1e-6
            for y in range(int(math.floor(min(y0, y1) - R)), int(math.ceil(max(y0, y1) + R)) + 1):
                for x in range(int(math.floor(min(x0, x1) - R)), int(math.ceil(max(x0, x1) + R)) + 1):
                    px, py = x + 0.5, y + 0.5
                    t = max(0.0, min(1.0, ((px - x0) * sx + (py - y0) * sy) / L2))
                    qx, qy = x0 + sx * t, y0 + sy * t
                    r = r0 + (r1 - r0) * t
                    d = math.hypot(px - qx, py - qy)
                    if d > r:
                        continue
                    k = d / r
                    if (x, y) in best and best[(x, y)][0] <= k:
                        continue
                    nx, ny = (px - qx) / r, (py - qy) / r
                    best[(x, y)] = (k, nx, ny)
        for (x, y), (k, nx, ny) in best.items():
            nz = math.sqrt(max(0.0, 1.0 - k * k))
            v = self._light(nx, ny, nz)
            self._put(x, y, TONE_A - TONE_B * v + bias, kind)

    def resolve(self):
        """Tone positions -> colours on a Canvas: casts, dither, rim. Returns (canvas, kind map)."""
        c = raster.Canvas(self.w, self.h, None)
        kinds = {}
        for (x, y), (t, order, kind) in self.px.items():
            # A form in front darkens what lies just below and right of it: the key is upper left.
            for (dx, dy) in ((-1, 0), (0, -1), (-1, -1)):
                q = self.px.get((x + dx, y + dy))
                if q is not None and q[1] > order:
                    t += self.kinds[q[1]][1] * (1.0 if (dx == 0 or dy == 0) else 0.6)
                    break
            t = max(0.0, min(4.0, t))
            i = int(t)
            f = t - i
            # A narrowed dither band: forms turn through a seam of mixed pixels, not a field of them.
            f = max(0.0, min(1.0, (f - 0.5) * 2.2 + 0.5))
            if i >= 4:
                tone = SK[4]
            else:
                tone = SK[i + 1] if f > th(x, y) else SK[i]
            c.set(x, y, tone)
            kinds[(x, y)] = kind
        # The rim: 1 px of the key on the key-facing silhouette edge of the body, never on a thin limb
        # (a rim beside a two-pixel leg doubles it into a white wire - cast_forest.rim).
        for (x, y), kind in kinds.items():
            if kind != "body":
                continue
            left = (x - 1, y) not in self.px
            up = (x, y - 1) not in self.px
            if not (left or up):
                continue
            thick = ((x + 1, y) in self.px and (x + 2, y) in self.px) if left else True
            thick = thick and (((x, y + 1) in self.px and (x, y + 2) in self.px) if up else True)
            if thick:
                c.set(x, y, RIM)
        return c, kinds


# --- the parts ---------------------------------------------------------------------------------------
#
# A pose is a dict of the parts' geometry in cell pixels (x right, y down, feet on row FEET). Legs are
# chains of (x, y, r) from the hip or shoulder to the hoof. Ears are chains from the root to the tip.

def leg(fig, pts, far):
    fig.capsule(pts, bias=1.25 if far else 0.15, kind="farleg" if far else "leg", cast=0.9)


def draw(pose):
    f = Fig()
    # Far side first: far ear, far legs.
    if "far_ear" in pose:
        f.capsule(pose["far_ear"], bias=0.8, kind="far", cast=0.6)
    leg(f, pose["far_hind"], True)
    leg(f, pose["far_fore"], True)
    # The body: barrel, then the haunch and chest over it.
    # The barrel is a lying cylinder, lit along its top and dark along the belly: as an ellipse its
    # far end turned dark too, and the flank read as two drums with a seam between them.
    bx = pose["barrel"]
    ux, uy = math.cos(bx[4]) * (bx[2] - bx[3]), math.sin(bx[4]) * (bx[2] - bx[3])
    f.capsule([(bx[0] - ux, bx[1] - uy, bx[3]), (bx[0] + ux, bx[1] + uy, bx[3])], bias=0.0, kind="body",
              cast=0.7)
    hx = pose["haunch"]
    f.ellipse(hx[0], hx[1], hx[2], hx[3], ang=hx[4], kind="body", cast=0.8, depth=0.9)
    cx = pose["chest"]
    f.ellipse(cx[0], cx[1], cx[2], cx[3], ang=cx[4], kind="body", cast=0.8, depth=0.9)
    # The neck and head.
    f.capsule(pose["neck"], bias=0.05, kind="body", cast=0.5)
    sk = pose["skull"]
    f.ellipse(sk[0], sk[1], sk[2], sk[3], ang=sk[4], kind="head", cast=0.6, depth=0.9)
    f.capsule(pose["muzzle"], bias=0.1, kind="head", cast=0.5)
    if "ear2" in pose:                       # the turned head shows both ears in front
        f.capsule(pose["ear2"], bias=0.35, kind="ear", cast=0.6)
    f.capsule(pose["near_ear"], bias=-0.15, kind="ear", cast=0.6)
    f.capsule(pose["tail"], bias=-0.1, kind="tail", cast=0.6)
    # Near legs over the body.
    leg(f, pose["near_hind"], False)
    leg(f, pose["near_fore"], False)
    return f


def finish(pose, f):
    c, kinds = f.resolve()
    # Hooves: a small dark wedge at the end of every leg - the near ones skin deep, the far ones occlusion.
    for key, far in (("far_hind", True), ("far_fore", True), ("near_hind", False), ("near_fore", False)):
        hx, hy, _r = pose[key][-1]
        for (x, y), kind in kinds.items():
            if kind not in ("leg", "farleg"):
                continue
            if math.hypot(x + 0.5 - hx, y + 0.5 - hy) <= 1.45 and y + 0.5 >= hy - 1.1:
                c.set(x, y, OCC if far else SK[4])
    # Inner ear: a fold of shade down the middle of the near ear (and of the second ear when turned).
    for key in ("near_ear", "ear2"):
        if key not in pose:
            continue
        pts = pose[key]
        (x0, y0, _), (x1, y1, _) = pts[0], pts[-2]
        n = int(max(abs(x1 - x0), abs(y1 - y0)))
        for i in range(1, n + 1):
            t = i / float(max(1, n))
            x = int(x0 + (x1 - x0) * t)
            y = int(y0 + (y1 - y0) * t)
            if (x, y) in kinds:
                c.set(x, y, SK[3])
    # The face: the eye a dark point with deep beside it (no light in it), the nose and the mouth line.
    for (ex, ey) in pose["eyes"]:
        c.set(ex, ey, OCC)
        c.set(ex - 1, ey, SK[4]) if pose.get("eye_wide") else c.set(ex, ey + 1, SK[3])
    nx, ny = pose["nose"]
    c.set(nx, ny, OCC)
    for (x, y, tone) in pose.get("face", []):
        c.set(x, y, tone)
    # The contact line under the hooves; a short detached one under her when she is in the air.
    if pose.get("air"):
        x0, x1 = pose["shadow"]
        c.hline(x0, x1, FEET + 1, OCC)
    else:
        xs = [p[-1][0] for p in (pose["far_hind"], pose["far_fore"], pose["near_hind"], pose["near_fore"])
              if p[-1][1] >= FEET - 0.5]
        x0, x1 = int(min(xs)) - 2, int(max(xs)) + 2
        c.hline(x0, x1, FEET + 1, OCC)
    matte.finish(c.img)
    return c


# --- the poses -----------------------------------------------------------------------------------------
#
# Proportions, from a hind rather than from taste: withers 22 rows above the hooves, a body 9 rows deep
# on legs 12 long (a deer stands on legs longer than its body is deep - shorter, and she was a sheep),
# a trunk about as long as she is tall, a neck half that, a head of nine, ears of five and broad.

def standing_legs(fore_dx=0.0, hind_dx=0.0, raise_near_fore=False):
    near_hind = [(14.0, 30.4, 2.8), (16.8, 35.0, 1.8), (12.8, 40.0, 1.1), (13.2, 44.6, 0.9),
                 (14.0, 46.4, 0.9)]
    far_hind = [(16.2, 31.0, 2.3), (19.0, 35.4, 1.6), (15.2, 40.2, 1.0), (15.6, 44.6, 0.85),
                (16.4, 46.4, 0.85)]
    near_fore = [(27.4, 30.4, 2.2), (27.2, 35.0, 1.5), (27.6, 40.0, 1.05), (27.9, 44.6, 0.9),
                 (28.7, 46.4, 0.9)]
    far_fore = [(30.0, 31.4, 1.9), (30.2, 35.4, 1.35), (30.6, 40.0, 1.0), (30.9, 44.6, 0.85),
                (31.7, 46.4, 0.85)]
    if raise_near_fore:
        # A forefoot lifted and folded at the knee, the hoof hanging under it: proud, and ready.
        near_fore = [(27.8, 30.4, 2.2), (29.0, 34.8, 1.5), (32.2, 38.6, 1.05), (31.4, 42.2, 0.9),
                     (32.2, 43.4, 0.9)]
    sh = lambda pts, dx: [(x + dx, y, r) for (x, y, r) in pts]
    return {"near_hind": sh(near_hind, hind_dx), "far_hind": sh(far_hind, hind_dx),
            "near_fore": sh(near_fore, fore_dx), "far_fore": sh(far_fore, fore_dx)}


BODY_STAND = {
    "haunch": (14.0, 28.8, 5.4, 4.7, 0.0),
    "barrel": (21.0, 29.4, 7.6, 3.8, 0.0),
    "chest": (28.2, 29.0, 4.6, 4.8, 0.0),
}


def pose_stand():
    p = dict(BODY_STAND)
    p.update(standing_legs(raise_near_fore=True))
    # Head high, the neck rising steep and slim to a head held level: the moon is up.
    p["neck"] = [(29.4, 26.6, 3.1), (32.6, 20.4, 2.2), (34.0, 16.4, 1.7)]
    p["skull"] = (35.6, 14.4, 3.3, 2.7, 0.3)
    p["muzzle"] = [(36.8, 15.4, 2.05), (41.4, 17.4, 1.15)]
    p["far_ear"] = [(34.4, 12.6, 0.8), (32.2, 10.2, 1.8), (30.8, 9.0, 0.6)]
    p["near_ear"] = [(35.2, 12.4, 0.9), (34.4, 9.2, 2.0), (33.8, 6.6, 0.7)]
    p["tail"] = [(9.2, 24.8, 1.2), (8.2, 28.4, 0.9)]
    p["eyes"] = [(37, 14)]
    p["nose"] = (41, 17)
    p["face"] = [(40, 18, SK[3])]
    return p


def pose_alert():
    p = dict(BODY_STAND)
    p.update(standing_legs())
    # The neck up, the head as high as it goes, the big ears up and cupped forward, spread in a V.
    p["neck"] = [(29.4, 26.6, 3.1), (31.8, 20.0, 2.3), (32.8, 16.0, 1.9)]
    p["skull"] = (34.4, 13.6, 3.3, 2.8, 0.35)
    p["muzzle"] = [(35.8, 14.6, 2.1), (39.8, 16.6, 1.2)]
    p["far_ear"] = [(33.0, 11.6, 0.8), (31.2, 8.4, 1.9), (30.2, 5.6, 0.6)]
    p["near_ear"] = [(33.8, 11.4, 0.9), (34.6, 7.8, 2.1), (35.0, 4.4, 0.7)]
    # The tail up: alarm.
    p["tail"] = [(9.0, 24.8, 1.2), (6.8, 23.0, 0.9)]
    p["eyes"] = [(36, 13)]
    p["nose"] = (40, 16)
    p["face"] = [(39, 17, SK[3])]
    return p


def pose_alert_turn():
    p = dict(BODY_STAND)
    p.update(standing_legs())
    # Head up and turned to look out of the picture, at the viewer: the face seen from the front,
    # the muzzle foreshortened down toward us, both ears spread, both eyes.
    p["neck"] = [(29.4, 26.6, 3.1), (31.4, 19.6, 2.3), (32.2, 15.4, 2.0)]
    p["skull"] = (32.8, 12.6, 3.1, 2.7, 0.0)
    p["muzzle"] = [(32.8, 13.8, 2.1), (32.8, 17.4, 1.2)]
    p["near_ear"] = [(31.0, 10.8, 0.9), (28.8, 7.8, 2.0), (27.6, 5.8, 0.7)]
    p["ear2"] = [(34.6, 10.8, 0.9), (36.8, 7.8, 2.0), (38.0, 5.8, 0.7)]
    p["tail"] = [(9.0, 24.8, 1.2), (6.8, 23.0, 0.9)]
    p["eyes"] = [(30, 12), (35, 12)]
    p["nose"] = (32, 17)
    p["face"] = [(33, 17, OCC), (32, 18, SK[3]), (33, 18, SK[3])]
    return p


def pose_graze(k):
    p = dict(BODY_STAND)
    # The back slopes a little down to the shoulders, which drop as the neck goes down.
    p["chest"] = (28.2, 29.8, 4.6, 4.8, 0.0)
    p["barrel"] = (21.0, 29.8, 7.6, 3.8, 0.05)
    p.update(standing_legs(fore_dx=0.6))
    low = 1.0 if k == 1 else 0.0
    # The neck goes forward off the shoulder and then down, AHEAD of the forelegs and slim, so the head
    # is its own shape on the turf - and the ears stand up clear of the neck, pricked forward: a grazing
    # deer listens while it eats. Laid back along the neck, they vanished into it.
    p["neck"] = [(30.4, 27.0, 3.0), (35.2, 31.0, 2.1), (38.4, 36.6 + low, 1.6)]
    p["skull"] = (39.6, 39.4 + low, 3.2, 2.6, 1.25)
    p["muzzle"] = [(40.2, 40.8 + low, 2.0), (41.2, 44.6 + low, 1.15)]
    p["far_ear"] = [(38.8, 37.8 + low, 0.8), (39.4, 34.6 + low, 1.8), (39.0, 32.4 + low, 0.6)]
    p["near_ear"] = [(40.0, 37.6 + low, 0.9), (42.0, 34.6 + low, 2.0), (43.2, 32.4 + low, 0.7)]
    # The tail hangs; in the second frame it flicks out.
    p["tail"] = ([(9.2, 24.8, 1.2), (8.2, 28.4, 0.9)] if k == 0 else [(9.0, 25.0, 1.2), (6.2, 26.6, 0.9)])
    p["eyes"] = [(41, int(39 + low))]
    p["nose"] = (41, 45 if k == 0 else 46)
    p["face"] = []
    return p


def pose_bound(k):
    p = {}
    if k == 0:
        # Gathered: the hind feet planted under her and driving, the forelegs folded up under the chest,
        # the forehand rising.
        p["haunch"] = (16.0, 30.4, 5.4, 4.7, -0.2)
        p["barrel"] = (22.4, 28.2, 7.4, 3.7, -0.32)
        p["chest"] = (29.0, 25.6, 4.6, 4.8, -0.25)
        p["near_hind"] = [(16.0, 32.0, 2.8), (20.6, 36.4, 1.8), (16.6, 40.4, 1.1), (20.2, 44.8, 0.9),
                          (21.6, 46.4, 0.9)]
        p["far_hind"] = [(18.0, 32.6, 2.3), (22.6, 36.8, 1.6), (19.0, 40.8, 1.0), (22.6, 45.0, 0.85),
                         (24.0, 46.4, 0.85)]
        p["near_fore"] = [(29.4, 27.6, 2.2), (30.6, 32.0, 1.5), (35.2, 33.6, 1.05), (33.6, 37.4, 0.9),
                          (34.4, 38.6, 0.9)]
        p["far_fore"] = [(31.4, 28.6, 1.9), (33.0, 32.2, 1.35), (37.4, 32.0, 1.0), (37.0, 35.8, 0.85),
                         (37.8, 36.8, 0.85)]
        p["neck"] = [(30.6, 23.2, 3.0), (34.0, 18.6, 2.1), (36.0, 15.8, 1.7)]
        p["skull"] = (37.6, 14.2, 3.3, 2.7, 0.4)
        p["muzzle"] = [(38.8, 15.4, 2.05), (42.8, 17.8, 1.15)]
        p["far_ear"] = [(36.4, 12.6, 0.8), (33.8, 10.8, 1.8), (31.8, 10.2, 0.6)]
        p["near_ear"] = [(37.0, 12.2, 0.9), (34.6, 9.8, 2.0), (32.6, 8.8, 0.7)]
        p["tail"] = [(10.6, 26.0, 1.2), (8.6, 24.2, 0.9)]
        p["eyes"] = [(39, 14)]
        p["nose"] = (43, 18)
        p["face"] = [(42, 19, SK[3])]
    elif k == 1:
        # Stretched out in the air, every hoof off the ground: forelegs reaching, hind legs thrown back.
        p["air"] = True
        p["shadow"] = (14, 32)
        p["haunch"] = (13.4, 27.0, 5.4, 4.5, 0.0)
        p["barrel"] = (20.8, 27.2, 7.8, 3.6, 0.0)
        p["chest"] = (28.6, 26.8, 4.6, 4.6, -0.05)
        p["near_hind"] = [(13.0, 29.0, 2.8), (14.6, 33.4, 1.8), (9.2, 35.0, 1.1), (4.6, 37.0, 0.9),
                          (2.8, 38.0, 0.9)]
        p["far_hind"] = [(15.0, 29.8, 2.3), (17.0, 34.2, 1.6), (11.8, 36.4, 1.0), (7.4, 39.0, 0.85),
                         (5.6, 40.0, 0.85)]
        p["near_fore"] = [(29.2, 28.8, 2.2), (32.6, 32.6, 1.5), (37.6, 34.4, 1.05), (42.4, 35.4, 0.9),
                          (44.2, 35.6, 0.9)]
        p["far_fore"] = [(31.0, 29.8, 1.9), (33.6, 34.2, 1.35), (38.2, 37.0, 1.0), (42.0, 38.8, 0.85),
                         (43.4, 39.4, 0.85)]
        p["neck"] = [(30.4, 24.6, 3.0), (34.6, 20.8, 2.1), (36.8, 18.6, 1.7)]
        p["skull"] = (38.4, 17.2, 3.3, 2.7, 0.45)
        p["muzzle"] = [(39.6, 18.4, 2.05), (43.4, 21.0, 1.15)]
        p["far_ear"] = [(37.2, 15.6, 0.8), (34.6, 14.0, 1.8), (32.6, 13.6, 0.6)]
        p["near_ear"] = [(37.8, 15.2, 0.9), (35.4, 12.8, 2.0), (33.4, 12.0, 0.7)]
        p["tail"] = [(8.4, 23.2, 1.2), (6.0, 21.8, 0.9)]
        p["eyes"] = [(40, 17)]
        p["nose"] = (44, 21)
        p["face"] = [(43, 22, SK[3])]
    else:
        # Landing: the forefeet down and taking her, the hind legs swinging through under the belly,
        # the haunch still high.
        p["haunch"] = (14.6, 26.4, 5.4, 4.7, 0.25)
        p["barrel"] = (21.4, 28.0, 7.6, 3.7, 0.22)
        p["chest"] = (28.6, 29.8, 4.6, 4.8, 0.15)
        p["near_hind"] = [(14.6, 28.6, 2.8), (19.6, 32.0, 1.8), (14.0, 35.0, 1.1), (16.4, 38.4, 0.9),
                          (17.8, 39.2, 0.9)]
        p["far_hind"] = [(16.4, 29.6, 2.3), (21.6, 32.6, 1.6), (16.8, 36.0, 1.0), (19.6, 39.6, 0.85),
                         (21.0, 40.4, 0.85)]
        p["near_fore"] = [(28.6, 31.4, 2.2), (30.0, 35.4, 1.5), (31.8, 40.4, 1.05), (32.8, 44.6, 0.9),
                          (33.6, 46.4, 0.9)]
        p["far_fore"] = [(30.6, 32.6, 1.9), (32.4, 36.2, 1.35), (35.4, 40.2, 1.0), (37.0, 43.2, 0.85),
                         (38.2, 44.0, 0.85)]
        p["neck"] = [(30.0, 27.4, 3.0), (33.6, 21.2, 2.1), (35.2, 17.6, 1.7)]
        p["skull"] = (36.8, 15.6, 3.3, 2.7, 0.3)
        p["muzzle"] = [(38.0, 16.6, 2.05), (42.4, 18.6, 1.15)]
        p["far_ear"] = [(35.6, 13.8, 0.8), (33.2, 11.8, 1.8), (31.4, 11.0, 0.6)]
        p["near_ear"] = [(36.2, 13.4, 0.9), (34.2, 10.8, 2.0), (32.6, 9.8, 0.7)]
        p["tail"] = [(9.6, 21.8, 1.2), (7.4, 20.4, 0.9)]
        p["eyes"] = [(38, 15)]
        p["nose"] = (42, 18)
        p["face"] = [(41, 19, SK[3])]
    return p


FRAMES = [("graze", lambda: pose_graze(0)), ("graze", lambda: pose_graze(1)),
          ("alert", pose_alert), ("alert-turn", pose_alert_turn),
          ("bound", lambda: pose_bound(0)), ("bound", lambda: pose_bound(1)), ("bound", lambda: pose_bound(2)),
          ("stand", pose_stand)]


def density(c):
    filled = edges = 0
    for y in range(c.h):
        for x in range(c.w):
            p = c.get(x, y)
            if not p[3]:
                continue
            filled += 1
            for dx, dy in ((1, 0), (0, 1)):
                q = c.get(x + dx, y + dy)
                if q[3] and q[:3] != p[:3]:
                    edges += 1
    return filled, edges / float(max(1, filled))


def turf(w, h):
    """The night turf of the downs, for the preview only: lichen deep and shade, a few chalk patches."""
    c = raster.Canvas(w, h, bl.tone("lichen", "deep"))
    for y in range(h):
        for x in range(w):
            v = 0.5 + 0.3 * math.sin(x * 0.05 + y * 0.03) + 0.2 * math.sin(x * 0.013 - y * 0.07)
            if v > 0.55 + th(x, y) * 0.4:
                c.set(x, y, bl.tone("lichen", "shade"))
    for (px, py, rx, ry) in ((w * 0.15, h * 0.7, 9, 4), (w * 0.62, h * 0.3, 12, 5), (w * 0.85, h * 0.8, 7, 3)):
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
    frames = []
    ok = True
    eye = None
    for i, (name, fn) in enumerate(FRAMES):
        pose = fn()
        c = finish(pose, draw(pose))
        bad = c.audit()
        if bad:
            print("OFF-CONTRACT COLOURS in silver-hind frame %d: %s" % (i, bad[:5]))
            ok = False
        filled, ratio = density(c)
        print("  frame %d %-10s %4d px  %.2f edges/px" % (i, name, filled, ratio))
        if i == 2:
            eye = list(pose["eyes"][0])
        frames.append(c)
    strip = Image.new("RGBA", (CELL * len(frames), CELL), (0, 0, 0, 0))
    for i, c in enumerate(frames):
        strip.alpha_composite(c.img, (i * CELL, 0))
    strip.save(os.path.join(OUT, "silver-hind.png"))
    meta = {"cell": CELL, "feet": FEET,
            "frames": {"graze": [0, 1], "alert": [2, 3], "bound": [4, 5, 6], "stand": [7]},
            "eye": eye}
    with open(os.path.join(OUT, "silver-hind.json"), "w") as fh:
        fh.write(json.dumps(meta, sort_keys=True) + "\n")
    print("  silver hind  %d frames at %dx%d -> world/downs/silver-hind.png, eye %s" % (len(frames), CELL, CELL, eye))

    # Preview: the strip at x2 (game size) and at x6, on the night turf.
    pad = 12
    big = 6
    W = CELL * len(frames) * big + pad * 2
    H = CELL * 2 + CELL * big + pad * 3
    sheet = turf(W, H).img
    small = strip.resize((strip.size[0] * 2, CELL * 2), Image.NEAREST)
    sheet.alpha_composite(small, (pad, pad))
    sheet.alpha_composite(strip.resize((strip.size[0] * big, CELL * big), Image.NEAREST), (pad, pad * 2 + CELL * 2))
    sheet.save(PREVIEW)
    print("preview: tools/art/_downs_event/silver_hind_preview.png")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
