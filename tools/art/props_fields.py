# -*- coding: utf-8 -*-
"""The Fallow Fields' standing props and foliage (.ai/campaign.md section 5, .ai/terrain.md section 13).
Run: python tools/art/props_fields.py

    world/fields/stook-1.png, stook-2.png   sheaves stood together in the stubble to dry
    world/fields/fence-1.png, fence-2.png   a run of post-and-rail fence, one with a rail down
    world/fields/hedge-1..3.png             a run of overgrown hedge: the hedgerows' body
    world/fields/hedge-oak-1, -2.png        the oaks the hedges grew up round
    world/fields/field-thorn-1.png          a hawthorn grown into a small tree
    world/fields/thorn-bush-1, -2.png       hawthorn and blackthorn bushes on a hedge's fringe
    world/fields/briar-1, -2.png            low arching briars, on the fringe and out in the stubble
    world/fields/fields.json                every sprite above, in trees.json's shape

"Haycocks and fence rails are props." A STOOK rather than a haycock: a haycock is a rounded heap
of hay, and the chapter's Wall creature is a rounded rick of black hay with a man inside it - a
static prop of the same outline would be a decoy the player has to learn to ignore. A stook is
the other thing a harvested field is dotted with, sheaves leaned together into a pointed tent, and
nothing in the cast is that shape. Gold's shade and base, lit on the key side, so they read as
straw left out under no light rather than as pickups.

Both are walked through, like the bushes (.ai/terrain.md section 10, "What blocks"): knee to
waist high, a sprite and no body. Each is authored 1:1 and saved at x2, like every world sprite,
WITH ITS CONTACT SHADOW ON THE LAST ROW: the bottom edge of the PNG is the ground line and the
middle column stands on the placement point - which `fields.json` now writes down for them (ground =
height, anchor_x = width / 2), so the chapter's foliage (Foliage.FallowFields.cs) reads them like any
tree. Redraw within the convention or change the manifest rows with it.

THE FOLIAGE PASS (2026-11-03, .ai/terrain.md section 13): the Fields' size ladder. The Forest's
stands are woods; the Fields' are HEDGEROWS, the black hedges between fields gone to stubble
(campaign.md section 5), so the ladder is a hedge's, not a wood's:

  hedge-oak        200-210 px   the oaks the hedges were planted round; the only trunks that block
  field-thorn      150 px       a hawthorn let grow into a tree, in the hedge line or alone
  hedge            130-180 px   the hedge itself: long low runs of leaf, dead canes standing out
                                of its top where nobody has laid it, a few haws
  thorn-bush       60-70 px     hawthorn and blackthorn on a hedge's fringe
  briar            40-50 px     knee-high arching briars, out into the stubble

ALL IN THE DARK HALF of `verdant` (base, shade, deep, occ): "black hedgerows", and the guardrail -
clutter stays lower in contrast than the enemies and the XP orbs. Nothing here uses verdant's `lit`
lime, which the Forest's old bushes did and which made them the brightest thing on this floor. The
haws are `red`'s lit and base in twos, on the leaf, never a lone bright disc. Trees.py's painters do
the drawing (its crown, broadleaf and leafy bush), so a Fields hedge is drawn the way a Forest bush is
and only its shape and its ramp are this chapter's.
"""
from __future__ import division, print_function

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

import math

import bonelight as bl
import raster
import trees as TR

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "fields")
PREVIEW = os.path.join(ROOT, "tools", "art", "_props_fields_preview.png")
SCALE = 2


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


GO = ramp("gold")
FL = ramp("flesh")
LN = ramp("linen")
VE = ramp("verdant")
RD = ramp("red")
# The dark half of verdant, brightest first, as trees.crown takes it: the hedges' leaf.
DARK = [VE[2], VE[3], VE[4], OCC]
RES = "res://assets/bonelight/world/fields"


def sheaf(c, foot_x, foot_y, top_x, top_y, front, salt):
    """One sheaf leaned into the stook: a bundle three straws wide from its splayed butt to its
    ears, tied a third of the way down. Front sheaves catch the key; back ones are a stop darker."""
    lit, mid, dark = (GO[2], GO[3], GO[4]) if front else (GO[3], GO[4], OCC)
    n = max(abs(top_y - foot_y), 1)
    for i in range(n + 1):
        t = i / float(n)
        x = int(round(foot_x + (top_x - foot_x) * t))
        y = int(round(foot_y + (top_y - foot_y) * t))
        spread = 1 if t < 0.15 else 0                   # the butt splays a little on the ground
        c.set(x - 1 - spread, y, lit)
        c.set(x, y, mid if (i + salt) % 4 else lit)
        c.set(x + 1 + spread, y, dark)
    # the band
    bx = int(round(foot_x + (top_x - foot_x) * 0.62))
    by = int(round(foot_y + (top_y - foot_y) * 0.62))
    c.set(bx - 1, by, LN[3])
    c.set(bx, by, LN[2] if front else LN[3])
    c.set(bx + 1, by, LN[4])
    # the ears, fanned a little past the top
    for (dx, dy, tone) in ((-1, -1, GO[2] if front else GO[3]), (0, -2, GO[1] if front else GO[2]),
                           (1, -1, GO[3]), (0, -1, GO[2] if front else GO[3]), (-2, 0, GO[3]), (2, 0, GO[4])):
        c.set(top_x + dx, top_y + dy, tone)


def stook(i):
    w, h = 26, 24
    c = raster.Canvas(w, h, None)
    # The contact shadow, on the last rows. Centred on h - 2, not h - 1.5: there it reached row h,
    # one below the picture, and was sliced flat along its foot (tools/art/clip_check.py).
    c.disc(13, h - 2, 11, 1.6, OCC)
    peak = (13, 5) if i == 0 else (12, 4)
    # back sheaves first, then the front ones over them, all leaning in to the peak
    back = [(6, 21), (20, 21), (13, 20)] if i == 0 else [(5, 21), (19, 20), (11, 20), (16, 21)]
    front = [(8, 22), (17, 22), (12, 22)] if i == 0 else [(7, 22), (15, 22), (21, 22)]
    for k, (fx, fy) in enumerate(back):
        sheaf(c, fx, fy, peak[0] + (k - 1), peak[1] + 1, False, k)
    for k, (fx, fy) in enumerate(front):
        sheaf(c, fx, fy, peak[0] + (k - 1) // 2, peak[1] + 2, True, k + 3)
    # the gaps between the butts at the foot are shadow
    for x in range(8, 19, 3):
        if c.get(x, h - 3)[3]:
            c.set(x, h - 3, OCC)
    return c


def post(c, x, base, height, lean=0):
    """A split-log post: lit face left, shade right, a pale cut top."""
    for k in range(height):
        xx = x + (lean * k) // height
        c.set(xx, base - k, GO[2] if k < height - 1 else GO[1])
        c.set(xx + 1, base - k, GO[3])
        c.set(xx + 2, base - k, GO[4])
    top_x = x + lean
    c.set(top_x, base - height, FL[2])
    c.set(top_x + 1, base - height, GO[2])
    c.set(top_x + 2, base - height, GO[3])
    c.set(x + 1, base - height // 2, OCC)               # a knot
    for dx in range(-1, 4):
        c.set(x + dx, base + 1, OCC)


def rail(c, x0, y0, x1, y1):
    """A rail: a lit top, a shaded underside, and occlusion under it where it shades the ground."""
    n = max(abs(x1 - x0), 1)
    for i in range(n + 1):
        t = i / float(n)
        x = int(round(x0 + (x1 - x0) * t))
        y = int(round(y0 + (y1 - y0) * t))
        c.set(x, y, GO[2] if (i % 7) else GO[1])
        c.set(x, y + 1, GO[3])
        c.set(x, y + 2, GO[4] if i % 3 else OCC)


def fence(i):
    w, h = 36, 18
    c = raster.Canvas(w, h, None)
    base = h - 2
    for x in range(1, w - 1):
        if x % 3:
            c.set(x, h - 1, OCC)                        # the run's shadow, broken
    if i == 0:
        rail(c, 3, 5, 31, 5)
        rail(c, 3, 10, 31, 10)
        post(c, 2, base, 13)
        post(c, 17, base, 13)
        post(c, 31, base, 13)
    else:
        # a rail down: the far section's top rail fallen, one end still on its post
        rail(c, 3, 5, 18, 5)
        rail(c, 3, 10, 31, 10)
        rail(c, 19, 6, 32, 14)
        post(c, 2, base, 13)
        post(c, 17, base, 13)
        post(c, 31, base, 10, lean=1)
    return c


SPRITES = [
    ("stook-1", lambda: stook(0)),
    ("stook-2", lambda: stook(1)),
    ("fence-1", lambda: fence(0)),
    ("fence-2", lambda: fence(1)),
]


# --- the hedgerows (the foliage pass) -------------------------------------------------------------

def haws(c, rnd, n):
    """Haws in twos, on the leaf: red's lit and base over its shade, so a pair is a dull red berry under
    no light and never a lone bright disc (an XP orb is a round disc of lime, lilac or pale yellow)."""
    ab = c.art_box()
    if ab is None:
        return
    placed, tries = 0, 0
    while placed < n and tries < n * 60:
        tries += 1
        x = ab[0] + 2 + rnd.randrange(max(1, ab[2] - ab[0] - 5))
        y = ab[1] + 3 + rnd.randrange(max(1, ab[3] - ab[1] - 8))
        if not (c.get(x, y)[3] and c.get(x + 1, y + 1)[3] and c.get(x - 1, y)[3]):
            continue
        c.set(x, y, RD[1])
        c.set(x + 1, y, RD[2])
        c.set(x, y + 1, RD[2])
        c.set(x + 1, y + 1, RD[3])
        placed += 1


def dead_canes(c, rnd, n, w, base, h):
    """Bare canes standing out of the top of a hedge nobody has laid: dead wood in gold's shade with a
    thorn in its deep now and then, rising from inside the leaf and arching over. Not flesh's shade:
    that stop is mauve, and a mauve cane over a dark hedge read as a pink wire."""
    for k in range(n):
        x0 = w // 6 + rnd.randrange(max(1, w - w // 3))
        y0 = base - h * (0.5 + rnd.random() * 0.2)
        side = -1 if rnd.random() < 0.5 else 1
        a = -math.pi / 2 + side * (0.25 + rnd.random() * 0.35)
        ln = h * (0.22 + rnd.random() * 0.22)
        px, py = float(x0), float(y0)
        for j in range(int(ln)):
            px += math.cos(a)
            py += math.sin(a)
            a += side * 0.07
            x, y = int(TR.round2(px)), int(TR.round2(py))
            c.set(x, y, GO[3])
            if j % 4 == 3:
                c.set(x - side, y, GO[4])


def hedge(seed, w, h, n, rmin, rmax, canes=4, n_haws=4):
    """A run of overgrown hedge: leaf clusters along a top line that rises and falls, the whole lit from
    the upper left as one long form (trees.crown), a long contact shadow, dead canes and a few haws."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + 3)
    base = h - 1
    cx = w // 2
    c.disc(cx, base, w * 0.48, 2.6, OCC)
    phase = rnd.random() * 6.28
    clusters = []
    for k in range(n):
        x = rmax + rnd.random() * (w - 2 * rmax)
        hump = 0.5 + 0.5 * math.sin(phase + x / float(w) * 6.28 * 1.3)
        top = base - h + rmax * 0.9 + (1.0 - hump) * h * 0.25
        y = top + (rnd.random() ** 0.7) * (base - rmax * 0.55 - top)
        r = rmin + (rmax - rmin) * rnd.random()
        clusters.append((x, y, r, r * 0.8))
    clusters.sort(key=lambda k: k[1])
    TR.crown(c, clusters, DARK, cx, base - h * 0.5, w * 0.5, h * 0.5, seed, ragged=0.45, bias=-0.05)
    dead_canes(c, rnd, canes, w, base, h)
    haws(c, rnd, n_haws)
    return c


def thorn_tree(seed, w, h):
    """A hawthorn let grow into a tree: a short leaning trunk and a low, broken dark crown with haws."""
    c = TR.broadleaf(seed, w, h, crown_frac=0.58, trunk_w=7, lean=2.5, n=22, rmin=5, rmax=9, leaves=DARK, gaps=0.12)
    haws(c, TR.Rng(seed + 5), 7)
    return c


def thorn_bush(seed, w, h, n):
    c = TR.leafy_bush(seed, w, h, n, 4, 8, tones=DARK)
    rnd = TR.Rng(seed + 9)
    dead_canes(c, rnd, 1, w, h - 1, h)
    haws(c, rnd, 3)
    return c


def briar(seed, w, h):
    """A briar: a low cushion of leaf and canes arching out of it and down to the ground, a hip or two."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + 3)
    base = h - 1
    cx = w // 2
    c.disc(cx, base, w * 0.4, 1.8, OCC)
    clusters = []
    for k in range(5):
        clusters.append((cx + (rnd.random() - 0.5) * w * 0.45, base - 2 - rnd.random() * h * 0.35,
                         3.0 + rnd.random() * 1.5, 2.4 + rnd.random()))
    clusters.sort(key=lambda k: k[1])
    TR.crown(c, clusters, [VE[3], VE[4], OCC, OCC], cx, base - h * 0.4, w * 0.4, h * 0.4, seed, ragged=0.4)
    for k in range(5):
        side = -1 if k % 2 == 0 else 1
        a = -math.pi / 2 + side * (0.35 + rnd.random() * 0.45)
        px, py = float(cx + (rnd.random() - 0.5) * 4), float(base - 2)
        for j in range(int(h * 1.1)):
            px += math.cos(a)
            py += math.sin(a)
            a += side * 0.11
            x, y = int(TR.round2(px)), int(TR.round2(py))
            if y > base:
                break
            c.set(x, y, GO[3] if j % 4 else GO[4])
            if j % 3 == 1:
                c.set(x, y - 1, VE[3])
    haws(c, rnd, 2)
    return c


# (id, kind, painter, trunk footprint in WORLD px or 0, rows under the ground line) - trees.py's shape.
FOLIAGE = [
    ("hedge-1", "bush", lambda: hedge(301, 76, 30, 24, 5, 9, canes=4, n_haws=4), 0, 3),
    ("hedge-2", "bush", lambda: hedge(307, 64, 26, 19, 5, 8, canes=3, n_haws=3), 0, 3),
    ("hedge-3", "bush", lambda: hedge(311, 88, 34, 30, 5, 10, canes=5, n_haws=5), 0, 3),
    ("hedge-oak-1", "tree", lambda: TR.broadleaf(317, 96, 120, trunk_w=10, n=36, rmin=6, rmax=13, leaves=DARK), 22, TR.FOOT),
    ("hedge-oak-2", "tree", lambda: TR.broadleaf(331, 88, 110, trunk_w=9, lean=-2, n=32, rmin=6, rmax=12, leaves=DARK), 22, TR.FOOT),
    ("field-thorn-1", "tree", lambda: thorn_tree(337, 70, 84), 14, TR.FOOT),
    ("thorn-bush-1", "bush", lambda: thorn_bush(341, 34, 22, 9), 0, 3),
    ("thorn-bush-2", "bush", lambda: thorn_bush(347, 30, 19, 7), 0, 3),
    ("briar-1", "bush", lambda: briar(353, 24, 13), 0, 3),
    ("briar-2", "bush", lambda: briar(359, 20, 11), 0, 3),
]


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    ok = True
    shots = []
    rows = []
    for name, fn in SPRITES:
        c = fn()
        bad = c.audit()
        if bad:
            ok = False
            print("OFF-CONTRACT COLOURS in %s: %s" % (name, bad[:4]))
            continue
        img = c.scaled(SCALE)
        img.save(os.path.join(OUT, name + ".png"))
        shots.append(img)
        print("  %-10s %dx%d on screen" % (name, img.size[0], img.size[1]))
        # The bottom edge is the ground line and the middle column the anchor (the convention above).
        w, h = img.size
        rows.append('    {"id": "%s", "kind": "bush", "path": "%s/%s.png", "width": %d, "height": %d, '
                    '"ground": %d, "anchor_x": %d, "trunk": 0}' % (name, RES, name, w, h, h, w // 2))
    for (name, kind, fn, footprint, foot) in FOLIAGE:
        row = TR.save_grounded(fn(), name, kind, foot, footprint, OUT, RES)
        if row is None:
            ok = False
            continue
        rows.append(row)
        shots.append(Image.open(os.path.join(OUT, name + ".png")).convert("RGBA"))
    manifest = ('{\n'
                '  "_comment": "Generated by tools/art/props_fields.py: the Fallow Fields\' props and foliage, in trees.json\'s '
                'shape. ground is the contact line and anchor_x the trunk column, in pixels of the PNG; trunk is the width in '
                'world pixels of the only part that blocks (0: walk through it).",\n'
                '  "sprites": [\n%s\n  ]\n}\n' % ",\n".join(rows))
    TR.write_text(os.path.join(OUT, "fields.json"), manifest)
    print("%d sprites in fields.json%s" % (len(rows), "" if ok else " - WITH PALETTE ERRORS"))
    # A preview on the stubble's tone, x3, for looking at.
    width = sum(s.size[0] for s in shots) + 20 * len(shots)
    height = max(s.size[1] for s in shots) + 20
    sheet = Image.new("RGBA", (width, height), raster.rgb(GO[4]))
    x = 10
    for s in shots:
        sheet.alpha_composite(s, (x, 10))
        x += s.size[0] + 20
    sheet.resize((sheet.size[0] * 3, sheet.size[1] * 3), Image.NEAREST).save(PREVIEW)
    print("preview: tools/art/_props_fields_preview.png")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
