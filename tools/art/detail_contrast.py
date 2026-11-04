# -*- coding: utf-8 -*-
"""How far each ground-detail set and each bush stands out from the ground it lies on, in CIE L*.
Run: python tools/art/detail_contrast.py [set_or_sprite ...]

The guardrail of the foliage pass (.ai/genre-lessons.md sections 2 and 5, .ai/terrain.md section 13):
clutter stays lower in contrast than the enemies and the XP orbs, because reading the swarm is the
game. `_UiShot --light-probe` measures that on screen, layer by layer, against the bodies and orbs it
stands in the same frame; this measures the art itself, set by set, so a new set can be held to it
before it is ever placed. Same numbers as the probe's layer lines:

  dL*     the set's mean L* minus the ground's
  |dL*|   the mean distance from the ground, either way
  hi, lo  its brightest and darkest tenth minus the ground

against the median L* of the ground each set is laid on (the Forest's turf, moss and meadow fill
tiles). For reference, the Forest's cast measured on screen at dL* -2 to -14 (dark silhouettes) with
a rim of hi +8 to +42, and the XP orbs at dL* +18 to +37, hi +68 to +76.
"""
from __future__ import print_function

import glob
import json
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DETAIL = os.path.join(ROOT, "assets", "bonelight", "tiles", "detail")
TILES = os.path.join(ROOT, "assets", "bonelight", "tiles")
TREES = os.path.join(ROOT, "assets", "bonelight", "world", "trees")

GROUNDS = ("grass", "moss", "meadow")
BUSHES = ("bush-small-1", "bush-small-2", "bush-small-3", "bush-round-1", "bush-round-2", "bush-wide-1",
          "bush-berry-1", "bush-berry-2", "bush-large-1", "bush-large-2")


def lightness(r, g, b):
    def lin(c):
        v = c / 255.0
        return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    y = 0.2126 * lin(r) + 0.7152 * lin(g) + 0.0722 * lin(b)
    f = y ** (1.0 / 3.0) if y > 0.008856 else 7.787 * y + 16.0 / 116.0
    return 116.0 * f - 16.0


def opaque_lightness(paths):
    out = []
    for p in paths:
        img = Image.open(p).convert("RGBA")
        for (r, g, b, a) in img.getdata():
            if a >= 128:
                out.append(lightness(r, g, b))
    return out


def ground_median(name):
    vals = sorted(opaque_lightness(glob.glob(os.path.join(TILES, name + "_fill*.png"))))
    return vals[len(vals) // 2] if vals else None


def stats(vals, ground):
    vals = sorted(vals)
    n = len(vals)
    tenth = max(1, n // 10)
    mean = sum(vals) / n
    return (mean - ground, sum(abs(v - ground) for v in vals) / n,
            sum(vals[-tenth:]) / tenth - ground, sum(vals[:tenth]) / tenth - ground, n)


def main(argv):
    with open(os.path.join(DETAIL, "detail.json")) as f:
        frames = json.load(f)["frames"]
    sets = {}
    for fr in frames:
        sets.setdefault(fr["set"], []).append(os.path.join(ROOT, fr["path"]))
    grounds = dict((g, ground_median(g)) for g in GROUNDS)
    print("ground median L*: " + ", ".join("%s %.1f" % (g, grounds[g]) for g in GROUNDS if grounds[g] is not None))
    want = argv[1:] or sorted(sets) + list(BUSHES)
    print("%-16s %-7s %7s %7s %7s %7s %7s" % ("set", "on", "dL*", "|dL*|", "hi", "lo", "px"))
    for name in want:
        if name in sets:
            paths = sets[name]
        elif os.path.exists(os.path.join(TREES, name + ".png")):
            paths = [os.path.join(TREES, name + ".png")]
        else:
            print("%-16s not found" % name)
            continue
        vals = opaque_lightness(paths)
        if not vals:
            continue
        for g in ("grass", "moss") if name in ("litter",) else ("grass",):
            d, a, hi, lo, n = stats(vals, grounds[g])
            print("%-16s %-7s %7.1f %7.1f %7.1f %7.1f %7d" % (name, g, d, a, hi, lo, n // len(paths)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
