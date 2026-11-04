# -*- coding: utf-8 -*-
"""The Blighted Swamp's foliage: its size ladder (.ai/terrain.md section 13, .ai/genre-lessons.md 2 and 6).
Run: python tools/art/props_swamp.py

    world/swamp/stilt-tree-1, -2.png      bog alders up on arching stilt roots, hung with moss: the islands' trees
    world/swamp/drowned-giant-1.png       a great dead tree the water killed, broken-topped, bracket fungus
    world/swamp/reed-bed-1 .. -3.png      wide beds of reed and bulrush: the swamp's mass, ringing every island
    world/swamp/hummock-1 .. -3.png       tussock hummocks, a peat mound under a shock of sedge
    world/swamp/swamp.json                every sprite above, in trees.json's shape

"Reeds, fog, and lanterns nobody living lit. Rot and standing water." The Forest's stands are woods and the
Fields' are hedgerows; the Swamp's are TREE ISLANDS - hummocks of moss standing out of an open fen, each a
knot of drowned wood ringed with reed beds, with the fen between them mud, pools and rush (the field is in
Foliage.BlightedSwamp.cs). So the ladder is a fen's, never a wood's:

  drowned-giant   300 px      a dead tree the size of the willows, alone at an island's heart or in the fen
  stilt-tree      220-250 px  the island's trees: a dark ragged crown over roots that arch down into the bog,
                              so the root skirt is the silhouette nothing else in the game has
  (willow, snag)  200-260 px  the chapter's old trees, trees.py's, kept: willows and moss-hung snags
  reed-bed        120-170 px  long low masses of reed and bulrush, waded through; the island's fringe
  hummock         50-70 px    a tussock of sedge on a peat mound, out on the fringe and in the fen
  (reed-tuft)     45-60 px    trees.py's sedge tussocks, kept
  detail          one cell    rushes, duckweed on the pools, peat clods, cypress knees, sodden litter and
                              rotwood (tools/art/detail_swamp.py)

THE PALETTE. Leaf on the dark half of `verdant` (base, shade, deep, occ) as every bush and tree is - the
crowns darker still, mostly shade and deep, because a drowned wood in fog is a dark mass and never a green
beacon. Wood is `linen`, the grey-green the chapter's willows and snags already wear. Hanging moss is
`lichen`'s lit and base, the beards the snags carry. Cattail heads are `gold`'s shade and deep, the same
browns as the shore's reeds (ground_detail.reeds). Nothing is emissive, nothing is lit by `lit` lime, and no
berry or flower is drawn at all: the Swamp's planned secrets are a flower on a lily pad (S1), a warm
lantern (S2) and a gold light in the reeds (S3), so nothing here is a warm or bright point.

WHAT THE SECRETS NEED LEFT ALONE (.ai/achievements-and-secrets.md 8.6). The Swallow-Hole's far tell is the
bog's sedge LEANING toward it, more of it nearer, the ground going to black mud (secret_swamp.py's
reed-lean and reed-mire). So every reed and blade here leans either way at random, never all one way - a
bed is upright as a whole - and nothing here is a puddle of black mud. The Peat Bank's tell is "a straight
cut face in the bog, the only straight thing there" (S6): nothing here is straight - no cut bank, no
plank, no post, and the knees in the detail layer are cones.

Trees.py's painters do the drawing (its Bleed canvas, fit, crown, fill_clusters, trunk, snag and
save_grounded), imported and never edited, so a swamp crown is drawn the way a Forest crown is and only the
shapes and the ramps are this chapter's. Python 2.7, deterministic (trees.Rng).
"""
from __future__ import division, print_function

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

import bonelight as bl
import raster
import trees as TR

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "assets", "bonelight", "world", "swamp")
PREVIEW = os.path.join(ROOT, "tools", "art", "_props_swamp_preview.png")
RES = "res://assets/bonelight/world/swamp"


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


VE = ramp("verdant")
LN = ramp("linen")
LI = ramp("lichen")
GO = ramp("gold")
# The crowns: verdant's dark half, brightest first, as trees.crown takes it - and a drowned wood's
# crown is a step darker again (shade over deep), with base only where the key catches a cluster's top.
CROWN = [VE[2], VE[3], VE[4], OCC]
# Hanging moss, root to tip: linen's grey-green, the colour of moss hung in fog. (Lichen's lit, which the
# chapter's old snags wear, photographed on these crowns as blue drips.)
BEARD = (LN[2], LN[3], LN[4])


def curve(c, p0, p1, p2, w0, w1, tones):
    """A quadratic stroke from p0 through the pull of p1 to p2, w0 wide at p0 thinning to w1 at p2, lit on
    its left (tones: lit, mid, dark). Used for the stilt roots, which arch out of the trunk and down."""
    n = int(max(abs(p2[0] - p0[0]), abs(p2[1] - p0[1])) * 1.6) + 2
    for k in range(n + 1):
        t = k / float(n)
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        wd = w0 + (w1 - w0) * t
        half = wd / 2.0
        for j in range(int(math.floor(-half + 0.5)), int(math.floor(half + 0.5)) + 1):
            u = (j + half) / max(1.0, wd)
            col = tones[0] if u < 0.34 else (tones[1] if u < 0.72 else tones[2])
            c.set(int(TR.round2(x + j)), int(TR.round2(y)), col)


def beards(c, rnd, x0, x1, y_of, n, salt):
    """Moss hanging off the crown's underside: ragged strands two pixels wide at the root, thinning to a
    pixel, swaying a little. y_of(x) is the lowest leaf at that column (or None where there is none)."""
    for k in range(n):
        x = int(x0 + rnd.random() * (x1 - x0))
        y = y_of(x)
        if y is None:
            continue
        ln = rnd.randrange(5, 15)
        for j in range(ln):
            sx = x + (1 if (j // 4 + k + salt) % 3 == 0 else 0)
            col = BEARD[0] if j < 2 else (BEARD[1] if (j + k) % 3 else BEARD[2])
            c.set(sx, y + j, col)
            if j < ln * 0.5:
                c.set(sx + 1, y + j, BEARD[2])


def lowest_leaf(c, x, top, bottom):
    """The lowest opaque crown pixel in column x between top and bottom, or None."""
    for y in range(int(bottom), int(top) - 1, -1):
        p = c.get(x, y)
        if p[3] and p[:3] in LEAF_RGB:
            return y
    return None


LEAF_RGB = set(raster.rgb(t)[:3] if len(raster.rgb(t)) > 3 else raster.rgb(t) for t in CROWN)


def stilt_tree(seed, w, h, roots=7, trunk_w=8):
    """A bog alder up on stilt roots: a short trunk that splits, a third of the way up, into roots arching
    out and down into the bog in a wide skirt - the swamp's own silhouette - under a low, ragged dark crown
    with moss hanging from it. Only the trunk's foot, under the middle of the skirt, blocks (the manifest's
    trunk width); the arches are drawn, and the wizard's boots pass between them as they would in a
    mangrove's."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    TR.shade_pool(c, cx + w * 0.05, base, w * 0.4, 7 + w * 0.03, 0.45)
    knee = base - h * 0.22                       # where the trunk splits into roots
    spread = w * 0.34
    # The roots behind the trunk first, a stop darker, then the trunk, then the roots in front of it.
    feet = []
    for k in range(roots):
        f = (k + 0.5) / roots * 2.0 - 1.0          # -1 .. 1 across the skirt
        gx = cx + f * spread * (0.75 + rnd.random() * 0.35) + (rnd.random() - 0.5) * 5
        behind = k % 2 == 1
        feet.append((gx, behind, f))
    for (gx, behind, f) in feet:
        if behind:
            sx = cx + f * 2.5
            kn = knee + rnd.random() * 6
            curve(c, (sx, kn), (sx + f * spread * (0.5 + rnd.random() * 0.5), kn - 4 - rnd.random() * 5), (gx, base - 1),
                  4.0, 2.4, (LN[3], LN[4], OCC))
            c.set(int(gx), base, OCC)
            c.set(int(gx) + 1, base, OCC)
    TR.contact(c, cx, base + 1, trunk_w * 0.8)
    top = h * 0.4
    TR.trunk(c, cx, base, top, trunk_w, trunk_w * 0.65, LN, seed, flare=trunk_w * 0.3, lean=(rnd.random() - 0.5) * 2)
    for (gx, behind, f) in feet:
        if not behind:
            sx = cx + f * 3.0
            kn = knee + 1 + rnd.random() * 7
            curve(c, (sx, kn), (sx + f * spread * (0.55 + rnd.random() * 0.5), kn - 5 - rnd.random() * 5), (gx, base),
                  4.6, 2.8, (LN[2], LN[3], LN[4]))
            c.disc(gx, base + 1, 2.6, 1.1, OCC)
    # limbs into the crown
    for s in (-1, 1, -1, 1):
        a = -math.pi / 2 + s * (0.55 + rnd.random() * 0.5)
        TR.limb(c, cx, top + h * 0.06 + rnd.random() * 4, a, h * 0.18, trunk_w * 0.45, LN, 1, rnd)
    # the crown: low and wide, ragged, with gaps the limbs show through
    crown_h = h * 0.56
    ccy = 2 + crown_h * 0.5
    crx = w * 0.5 - 2
    cry = crown_h * 0.5
    clusters = TR.fill_clusters(rnd, cx, ccy, crx - 8, cry - 6, int(w * 0.42), 5, 11)
    TR.crown(c, clusters, CROWN, cx, ccy, crx, cry, seed, ragged=0.45, gaps=0.14, bias=-0.06)
    # the underside, where the limbs show
    for k in range(int(w * 0.5)):
        x = int(cx - crx * 0.6 + rnd.random() * crx * 1.2)
        y = int(ccy + cry * (0.5 + rnd.random() * 0.35))
        if c.get(x, y)[3] and TR.hash2(x, y, seed) < 0.5:
            c.set(x, y, OCC)
    beards(c, rnd, cx - crx * 0.85, cx + crx * 0.85,
           lambda x: lowest_leaf(c, x, 0, ccy + cry + 4), int(w * 0.22), seed)
    return c


def drowned_giant(seed, w, h):
    """A great tree the rising water killed: a thick trunk broken off two thirds up, its jagged top
    pale where it split, three or four heavy limbs bare and forking, moss on the bark, beards hanging off
    the limbs, roots flared out over the bog, and shelves of bracket fungus up the trunk in gold's shade
    over linen's - a dull brown shelf, never a pale one. Heavier than the chapter's snags (trees.snag), so
    a dead tree is a rung of its own and not a big twig."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + TR.FOOT)
    base = h - 1
    cx = w // 2
    TR.shade_pool(c, cx + 5, base, w * 0.3, 7, 0.35)
    TR.contact(c, cx, base + 1, 11)
    top = h * 0.3
    lean = (rnd.random() - 0.5) * 3
    mid = TR.trunk(c, cx, base, top, 17, 11, LN, seed, flare=6, lean=lean)
    # the break: torn, never cut - one long splinter standing up on the lit side and the rest of the top
    # in ragged teeth falling away from it, so nothing about the top is a straight line (a straight cut
    # face is the Peat Bank's tell, S6). The torn wood is a stop paler where the key catches it.
    tx = mid(top)
    for x in range(int(tx - 6), int(tx + 7)):
        u = (x - (tx - 3)) / 9.0
        jag = int(2 + 13 * max(0.0, 1.0 - abs(u) * (1.6 if u > 0 else 3.0)) + 4 * TR.hash2(x, 1, seed))
        for y in range(int(top) - jag, int(top) + 1):
            c.set(x, y, LN[2] if x < tx else LN[3])
        c.set(x, int(top) - jag, LN[1] if (x < tx - 1 and TR.hash2(x, 2, seed) < 0.5) else LN[2])
        c.set(x + 1, int(top) - jag + 1, LN[4] if x >= tx else LN[3])
    # roots over the bog either side of the foot
    for s_ in (-1, 1, -1):
        rx = cx + s_ * (6 + rnd.random() * 3)
        curve(c, (cx + s_ * 4, base - 6), (rx + s_ * 5, base - 5), (rx + s_ * 10, base), 3.4, 1.6, (LN[2], LN[3], LN[4]))
    tips = []
    for k in range(4):
        side = -1 if k % 2 == 0 else 1
        y0 = top + h * (0.08 + 0.1 * k)
        a = -math.pi / 2 + side * (0.75 + rnd.random() * 0.4)
        TR.limb(c, mid(y0), y0, a, h * (0.2 - 0.025 * k), 5, LN, 2, rnd, tips)
    for k in range(int(h * 1.1)):
        x = cx + int((rnd.random() - 0.5) * 16)
        y = int(top + rnd.random() * (base - top))
        if c.get(x, y)[3] and TR.hash2(x, y, seed) < 0.5:
            c.set(x, y, VE[3] if x < cx else VE[4])
    for (px, py) in tips:
        if rnd.random() < 0.35:
            continue
        for strand in range(3):
            ln = rnd.randrange(6, 20) - strand * 3
            sx = int(px) + strand - 1
            for j in range(max(3, ln)):
                col = BEARD[0] if j < 2 else (BEARD[1] if (j + strand) % 3 else BEARD[2])
                c.set(sx + (1 if j > ln * 0.6 and strand == 2 else 0), int(py) + j, col)
    for k in range(3):
        y = int(base - h * (0.12 + 0.13 * k) - rnd.randrange(0, 4))
        side = 1 if k % 2 == 0 else -1
        x = int(mid(y))
        while c.get(x + side, y)[3] and abs(x - mid(y)) < 14:
            x += side
        n = rnd.randrange(4, 7)
        for j in range(n):
            c.set(x + side * (j + 1), y, GO[3] if j < n - 1 else GO[4])
            c.set(x + side * (j + 1), y + 1, LN[4] if j < n - 2 else OCC)
        c.set(x + side, y - 1, GO[3])
    return c


def reed_bed(seed, w, h, cattails=4):
    """A bed of reed and bulrush: blades rising from a long low foot, taller toward the middle so the bed is
    a long dome, each blade bending a little either way at random (a bed is upright as a whole: the bog's
    reeds leaning one way is the Swallow-Hole's far tell). Back blades a stop darker than the front ones,
    dry tips in linen, the odd lit blade on the key side, and a few cattails in gold's dark browns."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + 3)
    base = h - 1
    cx = w // 2
    c.disc(cx, base, w * 0.5, 2.4, OCC)
    n = int(w * 1.5)
    blades = []
    for k in range(n):
        x0 = cx + (rnd.random() - 0.5) * (w - 6)
        edge = abs(x0 - cx) / (w * 0.5)
        y0 = base - rnd.randrange(0, 4)
        ln = h * (0.55 + 0.4 * (1 - edge ** 2)) * (0.6 + rnd.random() * 0.45)
        bend = (rnd.random() - 0.5) * 2.4
        back = y0 < base - 1
        blades.append((y0, x0, ln, bend, back, k))
    blades.sort(key=lambda b: b[0])
    for (y0, x0, ln, bend, back, k) in blades:
        lit = (x0 < cx - w * 0.15) and k % 4 == 0 and not back
        for j in range(int(ln)):
            t = j / ln
            x = int(TR.round2(x0 + bend * t * t * 3.0))
            if back:
                col = VE[4] if j % 3 else OCC
            elif k % 3 == 1:
                # a dead blade: the blight's straw, linen's shade with its deep down the shaded side
                col = LN[3] if (j + k) % 4 else LN[4]
            else:
                col = VE[3] if (j + k) % 4 else VE[4]
            if lit and 0.3 < t < 0.75:
                col = VE[2]
            if t > 0.84:
                col = LN[3] if not back else LN[4]
            c.set(x, y0 - j, col)
    for k in range(cattails):
        x = int(cx + (rnd.random() - 0.5) * w * 0.7)
        stem_top = int(base - h + 3 + rnd.randrange(0, 5))
        for y in range(stem_top + 6, base - 2):
            c.set(x, y, LN[3])
        for j in range(5):
            c.set(x, stem_top + j, GO[3] if j else GO[4])
            c.set(x + 1, stem_top + j, GO[4])
        c.set(x, stem_top - 1, LN[3])
        c.set(x, stem_top - 2, LN[4])
    # the wet foot of the bed: a dark seam where the stems go into the water
    for x in range(int(cx - w * 0.46), int(cx + w * 0.46)):
        if c.get(x, base - 1)[3] and TR.hash2(x, base, seed) < 0.55:
            c.set(x, base - 1, VE[4])
    return c


def hummock(seed, w, h):
    """A tussock hummock: a low mound of peat, lit on its upper left, under a shock of sedge fanning out
    of its crown and arching over - dry at the tips. The tussocks the lanterns grow out of are these
    without the crook and the cold flame."""
    rnd = TR.Rng(seed)
    c = TR.Bleed(w, h + 3)
    base = h - 1
    cx = w // 2
    c.disc(cx, base, w * 0.44, 2.0, OCC)
    mh = h * 0.34
    mrx = w * 0.36
    my = base - mh * 0.45
    for y in range(int(base - mh), base + 1):
        for x in range(int(cx - mrx), int(cx + mrx) + 1):
            nx = (x - cx) / mrx
            ny = (y - my) / (mh * 0.6)
            if nx * nx + ny * ny > 1.0 or y > base:
                continue
            # Linen's base on the lit shoulder down to its deep: a mound of peat and old sedge, lit like
            # everything else. Its first draw ran shade to occ and photographed as a black puddle under the
            # grass - a hole in the bog, which is the Swallow-Hole's far tell.
            v = 0.62 - nx * 0.35 - ny * 0.3 + (TR.hash2(x, y, seed) - 0.5) * 0.15
            c.set(x, y, TR.band([LN[2], LN[3], LN[4]], v, x, y))
    top = base - mh * 0.8
    for k in range(int(w * 1.5)):
        x0 = cx + (rnd.random() - 0.5) * mrx * 1.1
        a = -math.pi / 2 + (x0 - cx) / mrx * 0.9 + (rnd.random() - 0.5) * 0.7
        ln = h * (0.7 + rnd.random() * 0.5)
        px, py = x0, top + rnd.random() * 2
        lit = x0 < cx and k % 3 == 0
        for j in range(int(ln)):
            t = j / ln
            px += math.cos(a)
            py += math.sin(a)
            a += 0.06 if a > -math.pi / 2 else -0.06          # the blade arches over, away from the middle
            col = (VE[2] if lit and 0.25 < t < 0.6 else (VE[3] if (j + k) % 3 else VE[4]))
            if t > 0.78:
                col = LN[3] if k % 2 else LN[2]
            c.set(int(TR.round2(px)), int(TR.round2(py)), col)
    return c


# (id, kind, painter, trunk footprint in WORLD px or 0, rows under the ground line) - trees.py's shape.
FOLIAGE = [
    ("stilt-tree-1", "tree", lambda: stilt_tree(401, 104, 122, roots=7, trunk_w=9), 24, TR.FOOT),
    ("stilt-tree-2", "tree", lambda: stilt_tree(409, 90, 108, roots=6, trunk_w=8), 22, TR.FOOT),
    ("drowned-giant-1", "tree", lambda: drowned_giant(419, 84, 144), 28, TR.FOOT),
    ("reed-bed-1", "bush", lambda: reed_bed(421, 72, 36, cattails=4), 0, 3),
    ("reed-bed-2", "bush", lambda: reed_bed(431, 58, 30, cattails=3), 0, 3),
    ("reed-bed-3", "bush", lambda: reed_bed(433, 84, 42, cattails=5), 0, 3),
    ("hummock-1", "bush", lambda: hummock(441, 30, 22), 0, 3),
    ("hummock-2", "bush", lambda: hummock(443, 24, 18), 0, 3),
    ("hummock-3", "bush", lambda: hummock(449, 36, 24), 0, 3),
]


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    ok = True
    shots = []
    rows = []
    for (name, kind, fn, footprint, foot) in FOLIAGE:
        row = TR.save_grounded(fn(), name, kind, foot, footprint, OUT, RES)
        if row is None:
            ok = False
            continue
        rows.append(row)
        shots.append(Image.open(os.path.join(OUT, name + ".png")).convert("RGBA"))
    manifest = ('{\n'
                '  "_comment": "Generated by tools/art/props_swamp.py: the Blighted Swamp\'s foliage, in trees.json\'s '
                'shape. ground is the contact line and anchor_x the trunk column, in pixels of the PNG; trunk is the width in '
                'world pixels of the only part that blocks (0: walk through it).",\n'
                '  "sprites": [\n%s\n  ]\n}\n' % ",\n".join(rows))
    TR.write_text(os.path.join(OUT, "swamp.json"), manifest)
    print("%d sprites in swamp.json%s" % (len(rows), "" if ok else " - WITH PALETTE ERRORS"))
    # A preview on the mud's tone (linen's shade), x2, for looking at; the old willow and snag beside them.
    for old in ("willow-1", "swamp-snag-1", "reed-tuft-1"):
        shots.append(Image.open(os.path.join(ROOT, "assets", "bonelight", "world", "trees", old + ".png")).convert("RGBA"))
    width = sum(s.size[0] for s in shots) + 16 * len(shots) + 16
    height = max(s.size[1] for s in shots) + 32
    sheet = Image.new("RGBA", (width, height), raster.rgb(LN[3]))
    x = 16
    for s in shots:
        sheet.alpha_composite(s, (x, height - 16 - s.size[1]))
        x += s.size[0] + 16
    sheet.resize((sheet.size[0] * 2, sheet.size[1] * 2), Image.NEAREST).save(PREVIEW)
    print("preview: tools/art/_props_swamp_preview.png")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
