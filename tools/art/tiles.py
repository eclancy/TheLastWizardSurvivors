# -*- coding: utf-8 -*-
"""The ground. Run: python tools/art/tiles.py

Every tile every stage is fought on: the autotile terrains at fourteen roles each, the plain floor
materials, the Cursed Dungeon's jail walls (`jail_walls.py`, with the shadows they throw) and the
chapters' own wall sets (`<name>_walls.py`, wall_set_modules below). About eight hundred 48x48 tiles
and a `tiles.json` manifest, which `CuratedTileCatalog.LoadMany` merges alongside the
Sketchbook's. The small things lying ON the ground - flowers, lilypads, reeds, pebbles - are
`ground_detail.py`, not this. Water is not drawn here either: its shape is computed
(scripts/WaterShape.cs) and its surface comes from `water.py`; tiles.json keeps one `water_fill`
tile so the manifest still says water blocks.

It replaces the bought dungeon-floor pack - the single largest piece of art in the game we could
not publish, and the last of it. See `.ai/asset-licensing.md`.

THE ROLES ARE DERIVED, NOT DRAWN. An autotiler asks for fourteen variants of every terrain: the
body, four edges, four outer corners, four inner corners, and an isolated patch. Drawing them by
hand would be hundreds of chances for one edge to disagree with the body it belongs to. So each
terrain supplies ONE painter for its body and one edge PROFILE, and every role is that body with
a coverage mask cut out of it - an edge tile is the fill tile, by construction, and they can never
drift apart.

THE GROUND IS THE QUIETEST THING ON SCREEN and has to stay that way. It covers every pixel the
player is not looking at, so all of it is drawn from the deep half of its material row, the
contrast inside a tile never exceeds two stops, and nothing on it is emissive except lava - which
is a hazard, and is supposed to pull the eye. `.ai/art-direction.md` section 4c is the rule; the
practical test is whether a skeleton standing on it still reads, and a ground tile that competes
is a ground tile that has to go darker.

THEY MUST ALSO TILE, AND AGAINST EACH OTHER. The base layer mixes a terrain's fill variants freely
(LevelTilePainter hashes one per cell), so a feature that crossed a tile edge would be cut off
wherever the neighbour is a different variant - a straight seam. Every feature here is therefore
kept off the tile's outer pixels (`inside()`), and the only things that touch an edge are
per-pixel specks, which have no shape to cut.

THE NATURAL-GROUND PASS (2026-10, `.ai/terrain.md` section 8). The Forest was reported as having
"large dark patches of something unrecognisable that don't blend with the grass". They were
`mossy_rock`: cobbles in `stone.deep` with an `occ` outline, two value steps under the grass,
stamped in staircase blobs with square corners. Three changes, all in this file:

  1. NO OUTLINES ON WALKABLE GROUND. Every terrain you can walk across now fades into whatever it
     lies on through a dithered coverage mask (the trick water already used): only the two
     hazards keep a crisp `occ` lip, because a lava edge that faded would lie about where the
     damage starts.
  2. ROUNDED, WANDERING EDGES. Corners are arcs, not right angles, and every edge wanders by a
     few pixels along its length - with two or three variants of every edge and corner, chosen
     per cell, so a long shore is not one scallop repeated every 48 px.
  3. BODIES YOU CAN NAME, AT A VALUE NEAR THE GROUND. Moss cushions and leaf litter instead of
     black cobble; a lighter meadow; bare earth that has pebbles in it; and three terrains whose
     bodies are partly transparent (overgrowth, scree) so they lie ON a floor rather than
     replacing it. Per-pixel static - the old 20% `occ` speckle on every floor - is gone too: a
     field of independent pixels reads as sandpaper, which section 3 of terrain.md found for
     water long before it was noticed on the ground.
"""

import io
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bonelight as bl
import jail_walls
import raster
import tile_atlas
import water

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "assets", "bonelight", "tiles")

SIZE = 48
TAU = math.pi * 2.0
CLEAR = (0, 0, 0, 0)


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


# Which variant is being drawn. Every painter reads it through hashed(), so bumping it reshuffles
# all of a terrain's features without any painter needing to know variants exist.
#
# IT EXISTS BECAUSE ONE TILE PER ROLE IS A GRID. With a single fill, every feature lands in exactly
# the same place in every cell and a floor of them reads as wallpaper with a 48-pixel repeat; with
# a single edge, a straight shore is one wiggle repeated. CuratedTileCatalog keeps every variant of
# a (terrain, role) and LevelTilePainter hashes one per cell.
VARIANT = 0

# Variants per role. Fill is what covers most of a stage, so it gets the most.
FILL_VARIANTS = 6
EDGE_VARIANTS = 3
CORNER_VARIANTS = 2


def hashed(x, y, salt=0):
    """A stable value in [0,1) for a cell. Deterministic so a tile is the same every run."""
    h = (x * 73856093) ^ (y * 19349663) ^ ((salt + VARIANT * 977) * 83492791)
    h &= 0x7FFFFFFF
    return ((h >> 7) % 10007) / 10007.0


def inside(k, salt, margin):
    """A coordinate for feature k that keeps `margin` pixels clear of both tile edges."""
    return margin + int(hashed(k, salt, salt * 3 + 1) * (SIZE - 2 * margin))


# --- drawing helpers -----------------------------------------------------------------------------
# Small vocabulary, used by every body.
#
# THE MARGIN PROBLEM, AND THE SHARED LAYER. A feature that crosses a tile edge is cut wherever the
# neighbouring cell is a different variant - a seam. Keeping every feature inside the tile avoids
# that, and makes a different failure: a bare margin round every tile, which over a patch of moss
# or overgrowth is a visible grid of rosettes. So a share of each body's features is SHARED - drawn
# as variant 0 whichever variant is being painted, at any position, wrapping round the tile - and
# the rest are the variant's own, kept inside. Shared features are identical in every variant, so
# where one wraps off the right edge, every possible neighbour has the rest of it on its left.


class Wrap(object):
    """A canvas view that wraps coordinates round the tile, for the shared layer."""

    def __init__(self, c):
        self.c = c

    def set(self, x, y, col):
        self.c.set(int(x) % SIZE, int(y) % SIZE, col)

    def get(self, x, y):
        return self.c.get(int(x) % SIZE, int(y) % SIZE)


def scatter(c, n, salt, margin, draw, shared=0.4):
    """Call draw(canvas, x, y, k) for n features: `shared` of them in the shared layer, the rest
    the variant's own and kept `margin` clear of the edges. See the note above."""
    global VARIANT
    own = VARIANT
    for k in range(n):
        if k < int(round(n * shared)):
            VARIANT = 0
            x = int(hashed(k, salt, salt * 3 + 1) * SIZE)
            y = int(hashed(k + 50, salt + 1, salt * 3 + 4) * SIZE)
            draw(Wrap(c), x, y, k)
            VARIANT = own
        else:
            draw(c, inside(k, salt, margin), inside(k + 50, salt + 1, margin), k)

def specks(c, density, tone, salt):
    """Single pixels. The only thing allowed to touch a tile edge - it has no shape to cut."""
    for y in range(SIZE):
        for x in range(SIZE):
            if hashed(x, y, salt) < density:
                c.set(x, y, tone)


def clumps(c, n, rmin, rmax, tone, salt, fill=0.55, squash=1.0, shared=0.4):
    """Soft blotches: a disc whose pixels are kept with probability `fill`, thinning at the rim.
    Scattered through scatter(), so some sit across the tile edges without making a seam."""
    def one(cv, cx, cy, k):
        r = rmin + hashed(k, 1, salt) * (rmax - rmin)
        ry = r * squash
        for y in range(int(cy - ry), int(cy + ry) + 1):
            for x in range(int(cx - r), int(cx + r) + 1):
                nx = (x - cx) / max(0.5, r)
                ny = (y - cy) / max(0.5, ry)
                d = nx * nx + ny * ny
                if d > 1.0:
                    continue
                # Solid in the middle, broken only towards the rim: a blotch with a dithered
                # edge reads as a patch of shade; one speckled all through read as mould.
                keep = min(1.0, (1.0 - d) * 2.2) * (0.55 + fill * 0.8)
                if hashed(x % SIZE, y % SIZE, salt + 4) < keep:
                    cv.set(x, y, tone)
    scatter(c, n, salt + 2, int(rmax) + 2, one, shared)


def tuft(c, x, y, dark, mid, light):
    """A grass tuft seen from above and a little in front: three blades and a dark root."""
    # Two blades and a lit tip, with the shadow on ONE side only. A symmetric tuft with a dark
    # root under it read as a printed glyph, and a field of them as a patterned carpet.
    c.set(x, y, mid)
    c.set(x - 1, y - 1, mid)
    c.set(x + 1, y - 2, light)
    c.set(x + 1, y - 1, mid)
    if dark:
        c.set(x + 1, y, dark)


def pebble(c, x, y, r, dark=OCC):
    """A stone lying on the ground, lit from the upper left, with its contact shadow."""
    st = ramp("stone")
    c.set(x, y + 1, dark)
    c.set(x + 1, y + 1, dark)
    c.set(x, y, st[2])
    c.set(x + 1, y, st[3])
    c.set(x, y - 1, st[1])
    if r > 1:
        c.set(x - 1, y, st[2])
        c.set(x - 1, y + 1, dark)
        c.set(x + 1, y - 1, st[2])
        c.set(x - 1, y - 1, st[0] if r > 2 else st[1])


def leaf(c, x, y, body, vein, turn):
    """A fallen leaf: a 2x3 lozenge with a dark midrib, turned one of four ways."""
    pts = [(0, -1), (0, 0), (0, 1), (1, 0)] if turn % 2 else [(-1, 0), (0, 0), (1, 0), (0, -1)]
    for dx, dy in pts:
        c.set(x + dx, y + dy, body)
    c.set(x, y, vein)


def crack(c, x, y, length, tone, salt):
    """A short angular crack, turning at each joint - a crack changes angle, a hatch does not."""
    ang = hashed(x, y, salt) * TAU
    px, py = float(x), float(y)
    for j in range(length):
        if j % 3 == 0:
            ang += (hashed(j, x, salt + 1) - 0.5) * 1.8
        px += math.cos(ang)
        py += math.sin(ang)
        ix, iy = int(px), int(py)
        if 1 <= ix < SIZE - 1 and 1 <= iy < SIZE - 1:
            c.set(ix, iy, tone)


# --- terrain bodies ------------------------------------------------------------------------------
# Each takes a canvas and fills it. Opaque unless it says otherwise.

def body_grass(c):
    """The Forest's ground. Calm teal turf with tufts, and no static.

    The old body was 16% `deep` and 10% `base` pixels hashed independently over `shade`: a
    field of television static that read as sandpaper at every zoom. This is one tone with soft
    blotches of its neighbours and a scatter of drawn tufts - the texture is in shapes the eye can
    name, not in noise. Variants 3-5 carry one extra thing each (clover, a fallen leaf or two,
    a pebble), so a long run of turf has incidents in it without the detail layer.
    """
    g = ramp("lichen")
    v = ramp("verdant")
    c.rect(0, 0, SIZE - 1, SIZE - 1, g[3])
    specks(c, 0.018, g[2], 111)
    specks(c, 0.012, g[4], 113)
    scatter(c, 9, 121, 3, lambda cv, x, y, k: tuft(cv, x, y, None if k % 3 else g[4], g[2], g[1] if k % 4 == 0 else g[2]))
    if VARIANT == 3:
        for k in range(4):
            x, y = inside(k, 131, 4), inside(k + 9, 133, 4)
            c.set(x, y, v[3])
            c.set(x + 1, y, v[3])
            c.set(x, y - 1, v[3])
            c.set(x + 1, y + 1, v[4])
    elif VARIANT == 4:
        f = ramp("flesh")
        gd = ramp("gold")
        for k in range(2):
            leaf(c, inside(k, 141, 4), inside(k + 5, 143, 4), gd[3] if k else f[3], f[4], k)
    elif VARIANT == 5:
        pebble(c, inside(1, 151, 5), inside(2, 153, 5), 2)


def body_meadow(c):
    """A sunlit clearing in the Forest: the grass one step lighter, with clover in it.

    The light patch to the moss's dark one, so the woodland floor has both directions of
    variation and neither is the only thing that is not grass.
    """
    g = ramp("lichen")
    v = ramp("verdant")
    c.rect(0, 0, SIZE - 1, SIZE - 1, g[2])
    specks(c, 0.02, g[1], 203)
    specks(c, 0.015, g[3], 205)
    scatter(c, 10, 211, 3, lambda cv, x, y, k: tuft(cv, x, y, None if k % 3 else g[3], g[1], g[1]))
    for k in range(5):
        x, y = inside(k, 221, 4), inside(k + 7, 223, 4)
        c.set(x, y, v[3])
        c.set(x - 1, y, v[3])
        c.set(x, y - 1, v[2] if k % 2 else v[3])
        c.set(x, y + 1, v[4])


def body_moss(c):
    """Forest floor under the trees: moss cushions and leaf litter on dark loam.

    THIS REPLACES THE FOREST'S `mossy_rock`, which is what "large dark patches of something
    unrecognisable" were. It is still the darker patch - a wood needs shade - but darker by one
    step rather than two, and full of things a player can name: rounded moss cushions lit on
    their upper-left, fallen leaves in three autumn tones, a twig. On `verdant` for the moss,
    because `.ai/terrain.md` section 4 found that anything living drawn on the ground's own ramp
    is a smudge.
    """
    g = ramp("lichen")
    v = ramp("verdant")
    f = ramp("flesh")
    gd = ramp("gold")
    rd = ramp("red")
    # On the grass tone itself, with the darkness carried by the cushions and the hollows between
    # them: a `deep` bed under everything was the first try, and in the game it was still a dark
    # patch - just a softer-edged one.
    c.rect(0, 0, SIZE - 1, SIZE - 1, g[3])
    clumps(c, 10, 1, 3, g[4], 301, fill=0.5)
    specks(c, 0.02, g[4], 303)
    # Moss cushions: a dark mound, a lit crescent upper-left, a contact pixel lower-right.
    def cushion(cv, cx, cy, k):
        r = 2 + int(hashed(k, 1, 311) * 3)
        for y in range(cy - r, cy + r + 1):
            for x in range(cx - r, cx + r + 1):
                dx, dy = x - cx, y - cy
                d2 = dx * dx + dy * dy
                if d2 > r * r + 1:
                    continue
                tone = v[4]
                if dx + dy < -r * 0.6:
                    tone = v[3]
                if dx + dy < -r * 1.2 and d2 > (r - 1) * (r - 1):
                    tone = v[2]
                if dx + dy > r * 1.1 and d2 > (r - 1) * (r - 1):
                    tone = g[4]
                cv.set(x, y, tone)
    scatter(c, 10, 313, 6, cushion)
    # Leaf litter, in the three muted autumn rows.
    scatter(c, 7, 321, 3, lambda cv, x, y, k: leaf(cv, x, y, (gd[3], f[3], rd[2])[k % 3], f[4], k))
    # One twig.
    x, y = inside(0, 331, 8), inside(1, 333, 8)
    for j in range(5 + int(hashed(0, 2, 335) * 4)):
        c.set(x + j, y + (j // 3), gd[4])
    c.set(x + 2, y - 1, gd[4])


def body_dirt(c):
    """Bare earth: a worn patch in the Forest, grey-brown, with stones in it.

    On `linen` with clods of `flesh` and `gold`, not on `gold` itself: a saturated brown patch on
    teal turf was exactly the kind of jump this pass exists to remove. Earth under a cold key
    light is a muted grey-brown, and that keeps it within a step of the grass round it.
    """
    gd = ramp("gold")
    f = ramp("flesh")
    ln = ramp("linen")
    c.rect(0, 0, SIZE - 1, SIZE - 1, ln[3])
    clumps(c, 9, 1, 3, ln[4], 401, fill=0.3)
    clumps(c, 3, 2, 3, f[4], 403, fill=0.0)
    clumps(c, 3, 1, 2, gd[4], 404, fill=0.2)
    specks(c, 0.015, ln[2], 405)
    specks(c, 0.01, OCC, 407)
    for k in range(4):
        pebble(c, inside(k, 411, 4), inside(k + 3, 413, 4), 1 + (k % 2))


def body_dark_dirt(c):
    """Darker soil - peat in the Swamp, earth between the stones in the Ruins and the Sands.

    Was `stone.deep` with 30% `occ` pixels: a black, faintly purple static that read as a hole
    rather than as ground. Now it is loam with clods and stones in it.
    """
    f = ramp("flesh")
    gd = ramp("gold")
    ln = ramp("linen")
    # On `linen.deep` with the warm rows only in the clods: on `flesh.deep` it was a purple patch,
    # and on the Swamp's grey-green mud that read as a stain rather than as peat.
    c.rect(0, 0, SIZE - 1, SIZE - 1, ln[4])
    clumps(c, 6, 2, 5, f[4], 501, fill=0.45)
    clumps(c, 5, 1, 3, gd[4], 502, fill=0.5)
    clumps(c, 4, 1, 2, ln[3], 503, fill=0.45)
    specks(c, 0.012, OCC, 505)
    specks(c, 0.01, f[3], 507)
    for k in range(3):
        pebble(c, inside(k, 511, 4), inside(k + 3, 513, 4), 1 + (k % 2))


def body_overgrowth(c):
    """Grass and moss growing over a floor - PARTLY TRANSPARENT, so it lies on what is under it.

    The Ruins and the Cave stamped opaque `grass` - the Forest's turf, a full value step above
    their black stone - as hard-edged rectangles. A lawn on a dungeon floor. This replaces it
    there: tufts and moss clumps over clear pixels, so the stone shows between the stems and the
    patch belongs to the floor it grew on.
    """
    g = ramp("lichen")
    v = ramp("verdant")
    clumps(c, 11, 3, 7, g[4], 601, fill=0.8, shared=0.5)
    clumps(c, 8, 2, 5, v[4], 603, fill=0.75, shared=0.5)
    clumps(c, 6, 2, 4, g[3], 605, fill=0.7, shared=0.5)
    scatter(c, 18, 611, 3, lambda cv, x, y, k: tuft(cv, x, y, g[4], g[3], g[2] if k % 3 else v[3]), 0.5)


def body_scree(c):
    """Loose stones - PARTLY TRANSPARENT. This is `rocky_pit`, which is walkable and not a hazard.

    It used to be the pit body (flat `occ`) with rocks on it, outlined like a pit: a hole you
    could walk across, which is the one thing a terrain must never look like. Now it is a spill
    of stones lying on whatever floor it is stamped on.
    """
    st = ramp("stone")
    for k in range(16):
        x, y = inside(k, 701, 2), inside(k + 40, 703, 2)
        r = 1 + int(hashed(k, 2, 705) * 3)
        pebble(c, x, y, r)
        if r > 2:
            c.set(x + 1, y, st[2])
            c.set(x, y + 1, st[3])
            c.set(x + 2, y + 1, OCC)
    specks(c, 0.025, st[3], 707)


def body_sand(c):
    s = ramp("gold")
    f = ramp("flesh")
    c.rect(0, 0, SIZE - 1, SIZE - 1, s[4])
    clumps(c, 5, 3, 6, f[4], 801, fill=0.4)
    specks(c, 0.02, s[3], 803)
    ripples(c, s[3], f[4], 805, 3)


def ripples(c, crest, trough, salt, n=6):
    """Wind ripples: short shallow arcs, never a full-width line - those lined up into stripes."""
    for k in range(n):
        x0, y0 = inside(k, salt, 6), inside(k + 13, salt + 1, 4)
        run = 6 + int(hashed(k, 3, salt + 2) * 9)
        for j in range(run):
            x = x0 - run // 2 + j
            if not (1 <= x < SIZE - 1):
                continue
            y = y0 + int(round(math.sin(j / float(run) * math.pi) * -1.5))
            c.set(x, y, crest)
            c.set(x, y + 1, trough)


def body_cobble(c):
    """Old paving - PARTLY TRANSPARENT between the stones, so the floor it was laid on shows.

    The joints used to be painted `stone.deep`, which on the Scorched Sands made every patch of
    paving a near-black slab on bright sand. Now the gaps are clear: on sand the sand runs between
    the setts, on the Ruins' stone the stone does.
    """
    global VARIANT
    st = ramp("stone")
    # running bond, six across, with the courses offset and every stone jittered
    for row in range(6):
        oy = row * 8
        off = 4 if row % 2 else 0
        for col in range(7):
            ox = col * 8 - off
            # A stone that wraps over the tile edge must be the same in every variant, or it is cut
            # wherever the neighbour is a different one - so those are hashed as variant 0.
            wraps = ox < 0 or ox + 8 > SIZE
            keep, VARIANT = VARIANT, (0 if wraps else VARIANT)
            jx = int(hashed(col, row, 6) * 2) - 1
            jy = int(hashed(col, row, 7) * 2) - 1
            # A few stones missing: broken paving reads as old, a perfect grid as new.
            missing = hashed(col, row, 9) < 0.14
            VARIANT = keep
            if missing:
                continue
            for y in range(1, 7):
                for x in range(1, 7):
                    if (x in (1, 6) and y in (1, 6)):
                        continue
                    px, py = (ox + x + jx) % SIZE, (oy + y + jy) % SIZE
                    tone = st[2] if (x <= 2 and y <= 2) else (st[3] if y < 5 else st[4])
                    c.set(px, py, tone)


def body_mossy_rock(c):
    """Flat stones half sunk in moss - the Swamp's stepping ground.

    Still cobble underneath, but with the moss over it in `verdant` rather than in the ground's
    own `lichen`, on a `lichen.deep` bed rather than on `stone.deep`, so the patch is a step from
    the mud round it rather than a black hole in it.
    """
    g = ramp("lichen")
    v = ramp("verdant")
    st = ramp("stone")
    c.rect(0, 0, SIZE - 1, SIZE - 1, g[4])
    for k in range(6):
        r = 3 + int(hashed(k, 1, 901) * 3)
        cx, cy = inside(k, 903, r + 2), inside(k + 9, 905, r + 2)
        for y in range(cy - r, cy + r + 1):
            for x in range(cx - r - 1, cx + r + 2):
                dx, dy = x - cx, (y - cy) * 1.3
                if dx * dx + dy * dy > r * r:
                    continue
                tone = st[2]
                if dx + dy < -r * 0.7:
                    tone = st[1]
                elif dx + dy > r * 0.7:
                    tone = st[3]
                c.set(x, y, tone)
        c.set(cx + r, cy + r // 2 + 1, OCC)
    clumps(c, 6, 2, 5, v[4], 911, fill=0.6)
    clumps(c, 3, 1, 3, v[3], 913, fill=0.5)


def body_water(c):
    """Water's one tile, a crop of the forest surface from water.py.

    WATER IS NOT DRAWN FROM TILES ANY MORE (2026-10, `.ai/terrain.md` section 3): its shape comes
    from scripts/WaterShape.cs and its surface from tools/art/water.py through a shader. This tile
    exists so tiles.json still says that water is a terrain and that it BLOCKS - which is what
    TerrainStreamer's flags, the flow field and every placement check read - and nothing paints it.
    """
    src = water.fill_tile()
    for y in range(SIZE):
        for x in range(SIZE):
            c.set(x, y, src.get(x, y))


# --- the Emberdeep: basalt that cracks, and lava that is still liquid ------------------------------
#
# Reported from 0.10.0: "Emberdeep doesn't look like cracked volcanic earth." It stood on `basalt`,
# which was the `natural` floor in the stone row with violet clumps - a calm navy slate that said
# nothing about where you were - and its LAVA was black crust with thin orange cracks: what cracked
# volcanic earth looks like. The hazard had the look the floor wanted. Both are redrawn here.
#
# THE PROBLEM WITH PLATES. A plate of basalt is wider than a tile, and the ground layer mixes a
# material's variants freely, so a crack that crossed a tile edge would be cut wherever the
# neighbour is a different variant. The answer is the old Wang-tile trick: EVERY variant crosses
# each tile edge at the same places, with a short stub through the edge that is identical in all of
# them (it wraps, so the stub at the top of one cell continues at the bottom of the one above), and
# only what happens inside the cell changes (crack_net below). Every variant comes out identical on
# its outer three pixels, so any variant meets any other with no seam.
#
# TWO NETWORKS THAT FAILED FIRST. One crossing per edge and two three-way junctions inside is the
# topology of a honeycomb, which is what columnar basalt is from above - and a field of it was
# interlocking jigsaw pieces of one size: the crossings sit on a 48 px lattice and the eye found it.
# Dropping legs at random to merge plates left the stubs as dead-end dashes everywhere and no plates
# at all. What works is two crossings a side, each at its own slant, and junctions scattered per
# variant and joined by their relative neighbourhood graph: plates of every size, no lattice.
# (The Frozen Waste's ice uses the same network for its fractures, one crossing a side.)


def fixed_hash(x, y, salt):
    """hashed() as variant 0, whatever variant is being drawn: for anything every variant must share."""
    h = (x * 73856093) ^ (y * 19349663) ^ (salt * 83492791)
    h &= 0x7FFFFFFF
    return ((h >> 7) % 10007) / 10007.0


def seg_dist(px, py, ax, ay, bx, by):
    """Distance from (px, py) to segment a-b, the nearest point on it, and how far along it (0-1)."""
    vx, vy = bx - ax, by - ay
    l2 = vx * vx + vy * vy
    t = 0.0 if l2 == 0 else max(0.0, min(1.0, ((px - ax) * vx + (py - ay) * vy) / l2))
    nx, ny = ax + vx * t, ay + vy * t
    return math.hypot(px - nx, py - ny), nx, ny, t


def nearest_seam(segs, px, py):
    """(clearance, distance, nearest x, nearest y, segment index, t, half-width) on the torus.

    Clearance is the distance less the seam's half-width there, so a bold seam wins over a hairline
    beside it. The tile wraps, so each segment is also tried one tile over in every direction: a
    shared stub that runs off the top of the cell is found again at its bottom."""
    best = (1e9, 0.0, 0.0, 0.0, -1, 0.0, 0.0)
    for i, s in enumerate(segs):
        ax, ay, bx, by, w0, w1 = s[0], s[1], s[2], s[3], s[4], s[5]
        for ox in (-SIZE, 0, SIZE):
            if min(ax, bx) + ox > px + 4 or max(ax, bx) + ox < px - 4:
                continue
            for oy in (-SIZE, 0, SIZE):
                if min(ay, by) + oy > py + 4 or max(ay, by) + oy < py - 4:
                    continue
                d, nx, ny, t = seg_dist(px, py, ax + ox, ay + oy, bx + ox, by + oy)
                hw = w0 + (w1 - w0) * t
                if d - hw < best[0]:
                    best = (d - hw, d, nx - ox, ny - oy, i, t, hw)
    return best


def leg(points, w0, w1, heat=None):
    """Segments (ax, ay, bx, by, w at a, w at b, heat) along a polyline, the width running w0 -> w1.
    heat is None, or (t0, t1, peak) in the polyline's own length - see basalt()."""
    out = []
    lens = [math.hypot(points[k + 1][0] - points[k][0], points[k + 1][1] - points[k][1])
            for k in range(len(points) - 1)]
    total = sum(lens) or 1.0
    run = 0.0
    for k in range(len(points) - 1):
        u0, u1 = run / total, (run + lens[k]) / total
        run += lens[k]
        (ax, ay), (bx, by) = points[k], points[k + 1]
        part = None
        if heat:
            lo, hi = max(heat[0], u0), min(heat[1], u1)
            if hi > lo:
                span = u1 - u0 or 1.0
                # this segment's share of the hot stretch, and where in the whole stretch it sits
                part = ((lo - u0) / span, (hi - u0) / span, heat[2],
                        (lo - heat[0]) / (heat[1] - heat[0]), (hi - heat[0]) / (heat[1] - heat[0]))
        out.append((ax, ay, bx, by, w0 + (w1 - w0) * u0, w0 + (w1 - w0) * u1, part))
    return out


def bent(a, b, h, salt, amount=3.0, bends=1):
    """a to b through midpoints pushed sideways, so no seam is a ruled line."""
    (ax, ay), (bx, by) = a, b
    dx, dy = bx - ax, by - ay
    ln = math.hypot(dx, dy) or 1.0
    pts = [a]
    for j in range(bends):
        f = (j + 1) / float(bends + 1)
        push = (h(salt + j) - 0.5) * 2.0 * amount
        pts.append((ax + dx * f - dy / ln * push, ay + dy * f + dx / ln * push))
    pts.append(b)
    return pts


def stroke(c, x0, y0, x1, y1, tone):
    """A one-pixel line, stepped at half a pixel so a diagonal never breaks into dots."""
    n = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
    for j in range(n + 1):
        t = j / float(n)
        c.set(int(x0 + (x1 - x0) * t), int(y0 + (y1 - y0) * t), tone)


HAIR = 0.5      # a hairline: one pixel
SEAM = 1.05     # a seam: two pixels, three where it smoulders


def crack_net(h, top, left, stub, nodes, width, spacing=11.0):
    """An irregular crack network for one variant of a tile, that meets every other variant at the edge.

    `top` and `left` are the shared crossings, (position, angle in degrees from the edge's normal):
    the seams that cross the top (and so the bottom) edge and the left (and so the right). Each gets a
    stub of `stub` pixels either side of the edge, identical in every variant and wrapping. Inside the
    cell `nodes` junctions are scattered (the variant's own, never nearer the edge than 10 px) and the
    junctions and the stubs' inner ends are joined by their RELATIVE NEIGHBOURHOOD GRAPH - a pair is
    joined unless some third point is nearer to both - which is the graph a drying or cooling crust
    actually cracks along: no crossings, junctions of three, cells of every size. Returns
    (segments, edges), the edges as (a, b) point pairs for anyone who wants to hang more off them."""
    segs = []
    ends = []
    for pos, ang in top:
        dx, dy = math.sin(math.radians(ang)) * stub, math.cos(math.radians(ang)) * stub
        segs += leg([(pos - dx, -dy), (pos + dx, dy)], width, width)
        ends.append(((pos + dx, dy), "t"))
        ends.append(((pos - dx, SIZE - dy), "b"))
    for pos, ang in left:
        dy, dx = math.sin(math.radians(ang)) * stub, math.cos(math.radians(ang)) * stub
        segs += leg([(-dx, pos - dy), (dx, pos + dy)], width, width)
        ends.append(((dx, pos + dy), "l"))
        ends.append(((SIZE - dx, pos - dy), "r"))
    pts = []
    for k in range(60):
        if len(pts) >= nodes:
            break
        q = (10 + h(100 + k * 2) * (SIZE - 20), 10 + h(101 + k * 2) * (SIZE - 20))
        if all(math.hypot(q[0] - o[0], q[1] - o[1]) >= spacing for o in pts) and                 all(math.hypot(q[0] - e[0][0], q[1] - e[0][1]) >= spacing * 0.7 for e in ends):
            pts.append(q)
    allp = [(q, "n") for q in pts] + ends
    edges = []
    for i in range(len(allp)):
        for j in range(i + 1, len(allp)):
            (a, sa), (b, sb) = allp[i], allp[j]
            if sa != "n" and sb != "n":
                continue      # two stubs are never joined directly: that line would hug the edge
            dab = math.hypot(a[0] - b[0], a[1] - b[1])
            blocked = False
            for k in range(len(allp)):
                if k in (i, j):
                    continue
                c = allp[k][0]
                if max(math.hypot(a[0] - c[0], a[1] - c[1]), math.hypot(b[0] - c[0], b[1] - c[1])) < dab:
                    blocked = True
                    break
            if not blocked:
                edges.append((a, b))
    # A stub the graph left alone still has to go somewhere: to its nearest junction.
    for e, side in ends:
        if not any(e in ab for ab in edges) and pts:
            edges.append((e, min(pts, key=lambda q: math.hypot(q[0] - e[0], q[1] - e[1]))))
    return segs, edges


# The basalt's shared crossings: two seams through the top edge and two through the left, each at its
# own slant - so the crossings neither sit on a square lattice nor all cross the edge square-on.
BASALT_TOP = ((11, 25), (33, -30))
BASALT_LEFT = ((17, -20), (37, 30))
BASALT_STUB = 6


def basalt_network(seed):
    """The seams of one basalt variant (see leg() for the tuple)."""
    h = lambda k: hashed(k, seed, 3100)
    segs, edges = crack_net(h, BASALT_TOP, BASALT_LEFT, BASALT_STUB, 4 + seed % 2, SEAM, 12.0)
    # Which seam smoulders, and how much (basalt() says what each peak looks like): the longest
    # interior one, so the glow is a stretch of seam and never a speck.
    peak = (3, 0, 1, 0, 2, 0)[seed % 6]
    order = sorted(range(len(edges)), key=lambda k: -math.hypot(edges[k][0][0] - edges[k][1][0],
                                                                edges[k][0][1] - edges[k][1][1]))
    hot = order[int(h(30) * 3) % len(order)] if peak else None
    for k, (a, b) in enumerate(edges):
        heat = None
        if k == hot:
            t0 = 0.05 + h(31) * 0.25
            heat = (t0, min(0.95, t0 + 0.4 + h(32) * 0.45), peak)
        wide = SEAM
        segs += leg(bent(a, b, h, 40 + k * 3, 1.6, 1), wide, wide, heat)
    # A hairline off a seam into the plate, dying out: the crack that has not finished.
    if edges and seed % 3 != 0:
        a, b = edges[int(h(60) * len(edges)) % len(edges)]
        u = 0.35 + h(62) * 0.3
        sx, sy = a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u
        ang = math.atan2(b[1] - a[1], b[0] - a[0]) + (1.0 if h(64) < 0.5 else -1.0) * (1.1 + h(66) * 0.6)
        ln = 5 + h(68) * 4
        ex = min(SIZE - 8, max(8, sx + math.cos(ang) * ln))
        ey = min(SIZE - 8, max(8, sy + math.sin(ang) * ln))
        segs += leg(bent((sx, sy), (ex, ey), h, 70, 1.2), HAIR, HAIR)
    return segs


def basalt(c, seed):
    """The Emberdeep's floor: dark basalt plates, split by seams that smoulder here and there.

    Stone row, on `shade` like every open floor, so a steel-deep Chainhound or a Slag Thrall still
    stands off it by its rim; each plate catches the key on its upper-left lip (`base`) and drops
    into `deep` on its lower-right, which is what makes it a raised slab rather than a painted line.
    The seams are `occ`. In half the variants one seam smoulders along part of its length: a
    one-pixel core of the RED row (a maroon, not an emissive) with the lips either side caught red
    and the plate beside them stained a step warmer - and in one variant in six the core of that
    stretch is the fire ramp's `edge`, the darkest fire there is. That is the most the walkable
    ground ever glows: this chapter's cast wear fire eyes, and the floor must never outshine two
    pixels of them. (Four variants in six glowed at first, and on a full screen the red ticks were
    a sprinkle.) The lava is what blazes, and has to stay unmistakably the thing that does.

    The plates tile because the network does: crack_net. Every variant is identical on its outer
    three pixels - checked, not assumed - so any variant meets any other with no seam.

    Nothing else is drawn INTO the plates but a little violet glass and the odd gas bubble frozen as
    a pit. The first version pressed ropes of pahoehoe and drifts of ash into them, and in a field of
    cells the ropes read as a printed C and the ash as grey squares: anything with a shape repeats
    every few cells here. The ropes, the ash and the cinders are the detail layer's
    (ground_detail.py: lava_rope, ash, cinders), which places them in world space, in drifts.
    """
    st = ramp("stone")
    vi = ramp("violet")
    rd = ramp("red")
    fire = bl.ELEMENTS["fire"]
    h = lambda k: hashed(k, seed, 3200)
    segs = basalt_network(seed)

    c.rect(0, 0, SIZE - 1, SIZE - 1, st[3])
    # The shared layer: the same in every variant and allowed to wrap, so it is free to sit across a
    # tile edge - quiet mottling a stop down, and a little violet glass.
    clumps(c, 6, 1, 3, st[4], 3301, fill=0.25, shared=1.0)
    clumps(c, 6, 2, 4, vi[4], 3303, fill=0.45, shared=1.0)
    # The variant's own, kept off the edges: a violet sheen, and gas bubbles frozen as pits.
    for k in range(2):
        cx, cy = 8 + int(h(20 + k) * 32), 8 + int(h(30 + k) * 32)
        for y in range(cy - 2, cy + 3):
            for x in range(cx - 3, cx + 4):
                if (x - cx) ** 2 * 0.5 + (y - cy) ** 2 <= 4 and hashed(x, y, 3305 + seed) < 0.55:
                    c.set(x, y, vi[3])
    for k in range(2 + seed % 3):
        c.set(6 + int(h(40 + k) * 35), 6 + int(h(50 + k) * 35), st[4])

    field = {}
    for y in range(SIZE):
        for x in range(SIZE):
            field[(x, y)] = nearest_seam(segs, x + 0.5, y + 0.5)

    def warm_at(i, t):
        part = segs[i][6]
        return part[2] if part and part[0] - 0.06 <= t <= part[1] + 0.06 else 0

    # The bevel, then the seams over it.
    for y in range(SIZE):
        for x in range(SIZE):
            clear, d, nx, ny, i, t, hw = field[(x, y)]
            heat = warm_at(i, t)
            if clear <= 0:
                c.set(x, y, OCC)
                continue
            if clear <= 1.0 and hw >= 0.8:
                toward = (nx - (x + 0.5)) + (ny - (y + 0.5))
                if heat >= 2:
                    # both lips of a smouldering seam are lit by it, not by the key
                    c.set(x, y, rd[2] if heat >= 3 and toward < 0 else rd[3])
                elif toward < 0:
                    # the plate's upper-left lip, facing the key - broken, so it is not an outline
                    if hashed(x, y, 3320) < 0.75:
                        c.set(x, y, st[2])
                else:
                    c.set(x, y, st[4])
            elif heat >= 2 and clear <= 2.6 and (x + y) % 2:
                # the plate stained by the heat beside it, a step warmer for a pixel or two
                c.set(x, y, rd[4])
    # The smoulder: a one-pixel core down the middle of a warm stretch, hottest in its middle.
    for (ax, ay, bx, by, w0, w1, part) in segs:
        if not part:
            continue
        t0, t1, peak, s0, s1 = part
        n = max(2, int(math.hypot(bx - ax, by - ay) * (t1 - t0)))
        for j in range(n):
            u0 = t0 + (t1 - t0) * j / float(n)
            u1 = t0 + (t1 - t0) * (j + 1) / float(n)
            s = s0 + (s1 - s0) * (j + 0.5) / n
            mid = abs(s - 0.5) * 2.0          # 0 in the middle of the whole stretch, 1 at its ends
            if peak >= 3:
                tone = fire[3] if mid < 0.5 else (rd[0] if mid < 0.85 else rd[1])
            elif peak == 2:
                tone = rd[0] if mid < 0.55 else rd[1]
            else:
                tone = rd[1] if mid < 0.6 else rd[2]
            stroke(c, ax + (bx - ax) * u0, ay + (by - ay) * u0, ax + (bx - ax) * u1, ay + (by - ay) * u1, tone)


# The lava's shared crossings, as the basalt's (crack_net): its channels are the crust's cracks.
LAVA_TOP = ((8, -20), (29, 25))
LAVA_LEFT = ((12, 25), (33, -25))
LAVA_STUB = 6
LAVA_CHANNEL = 1.6


def body_lava(c):
    """Molten rock under a crust: dark rafts, and glowing melt in the channels between them.

    It used to be black crust with thin orange cracks - which is what the Emberdeep's FLOOR should
    have looked like, and did not. With the basalt now cracked and smouldering, the lava has to be
    the other thing, unmistakably: rock that is still liquid. The same crack network as the basalt
    (crack_net), so it tiles the same way, but the cracks are channels four pixels wide and every
    one of them runs hot: the fire ramp's `mid` down the middle, its `edge` banked against the
    rafts, and the rafts themselves the red row's deep and `occ`, rimmed red where each rides on the
    melt. The basalt's seams are two pixels of `occ` and glow along one stretch a cell at most; the
    lava's are twice the width and glow everywhere, on red-black rather than slate. Nobody mistakes
    one for the other from across the screen, and that is the only job a hazard's look has.

    This is the one emissive ground in the game, and that is deliberate: lava is a hazard, so it is
    allowed - required, really - to pull the eye that everything else on the floor gives up. It
    stays mostly crust all the same: the Emberdeep's casters throw fire, and a bolt has to read
    against the pool it flies over.
    """
    st = ramp("stone")
    rd = ramp("red")
    f = bl.ELEMENTS["fire"]
    v = VARIANT
    h = lambda k: hashed(k, v, 3400)
    segs, edges = crack_net(h, LAVA_TOP, LAVA_LEFT, LAVA_STUB, 4 + v % 2, LAVA_CHANNEL, 11.0)
    for k, (a, b) in enumerate(edges):
        w = LAVA_CHANNEL - 0.3 * (k % 2)
        segs += leg(bent(a, b, h, 40 + k * 3, 2.0, 1), w, w)
    for y in range(SIZE):
        for x in range(SIZE):
            clear, d, nx, ny, i, t, hw = nearest_seam(segs, x + 0.5, y + 0.5)
            if d <= hw - 1.1:
                tone = f[2]
            elif clear <= 0:
                tone = f[3]
            elif clear <= 1.0:
                # the raft's rim where it rides on the melt, lit by it from below
                tone = rd[1] if (nx - x - 0.5) + (ny - y - 0.5) > 0 else rd[2]
            elif clear <= 2.0:
                tone = rd[3]
            else:
                # the crust: red-black, with `occ` and stone in it - per pixel, so it may touch an edge
                w = fixed_hash(x, y, 3412)
                tone = OCC if w < 0.25 else (st[4] if w < 0.32 else rd[4])
            c.set(x, y, tone)
    # Where channels meet the melt runs fastest: a fleck of `hot` at a junction, inside the cell.
    for k, (a, b) in enumerate(edges[:3]):
        x, y = int(a[0]), int(a[1])
        if 6 <= x < SIZE - 6 and 6 <= y < SIZE - 6 and k % 2 == v % 2:
            c.set(x, y, f[1])


# The ice's shared crossings: one fracture through each pair of edges, so a sheet of it is crazed into
# panes about a cell across.
ICE_TOP = ((30, 15),)
ICE_LEFT = ((14, -20),)
ICE_STUB = 7


def body_ice(c):
    """A frozen pool on the Frozen Waste: dark glassy ice with white fractures in it.

    On the old slate floor the ice was the light patch, steel's deep under blotches of its shade.
    With the floor now snow (snow() below, a step paler), ice is what it is on a real snowfield: the
    darker, smoother thing - the matte snow, then the ice a step under it, then the black open water
    (whose frozen shore is water.py's). So the blotches went (on snow they read as spots) and what
    says "ice" is the crazing: hairline fractures in steel's shade, the same crack network as the
    Emberdeep's basalt (crack_net), so they cross from cell to cell and never stop at a tile edge;
    and at the odd junction where two fractures meet, one pixel of steel's base catching the key.
    (Milky patches were tried and went: in a field of cells, a dithered blotch reads as a glyph.)
    """
    stl = ramp("steel")
    v = VARIANT
    h = lambda k: hashed(k, v, 3600)
    segs, edges = crack_net(h, ICE_TOP, ICE_LEFT, ICE_STUB, 2 + v % 2, HAIR, 14.0)
    for k, (a, b) in enumerate(edges):
        segs += leg(bent(a, b, h, 40 + k * 3, 2.0, 2), HAIR, HAIR)
    c.rect(0, 0, SIZE - 1, SIZE - 1, stl[4])
    for y in range(SIZE):
        for x in range(SIZE):
            if nearest_seam(segs, x + 0.5, y + 0.5)[0] <= 0:
                c.set(x, y, stl[3])
    for k, (a, b) in enumerate(edges):
        x, y = int(a[0]), int(a[1])
        if 8 <= x < SIZE - 8 and 8 <= y < SIZE - 8 and k % 2 == 0:
            c.set(x, y, stl[2])


def body_pit(c):
    st = ramp("stone")
    c.rect(0, 0, SIZE - 1, SIZE - 1, OCC)
    for y in range(SIZE):
        for x in range(SIZE):
            if hashed(x, y, 26) > 0.955:
                c.set(x, y, st[4])


# The maze walls are not here: they are tools/art/jail_walls.py, drawn per role with their dressing
# (emit_walls below).


# --- plain floor materials -------------------------------------------------------------------
# No autotile roles: the base layer paints these straight, several variants each so a floor does
# not visibly repeat.

def flagstone(c, r, seed):
    """The Cursed Dungeon floor: laid flags, one full step LIGHTER than every other floor.

    Every other floor is built on the material's `deep` tone, which is right for open ground and
    wrong for a maze. Its walls are `void` (below), and a `deep` floor sits two points of value
    above `occ` - the corridors and the rock between them were the same near-black, and the stage
    was reported as hard to read. So this one stands on `base`, with the mortar in `deep` and no
    `occ` anywhere, which leaves the darkest thing on the floor visibly lighter than the wall it
    runs into. Still the stone row and still no more than two steps either side of the body, so a
    skeleton on it reads exactly as the contract wants.

    Courses are 24 px and sit at the SAME rows in every variant - only the joints and the speckle
    move - because the ground layer mixes variants freely, and a course that shifted per variant
    drew a seam down every tile boundary. The joints shift per course and per variant, so the
    flags do not line up into a grid.
    """
    c.rect(0, 0, SIZE - 1, SIZE - 1, r[2])
    for y in range(SIZE):
        for x in range(SIZE):
            v = hashed(x, y, 140 + seed)
            if v < 0.10:
                c.set(x, y, r[3])
            elif v > 0.985:
                c.set(x, y, r[1])
    for row in range(2):
        top = row * 24
        # mortar along the top of the course, a shaded lip above it and a lit lip below it: the
        # key is upper-left, so each flag catches light on its top edge and drops into its joint
        for x in range(SIZE):
            c.set(x, top, r[4])
            c.set(x, (top - 1) % SIZE, r[3])
            if hashed(x, row, 150 + seed) < 0.6:
                c.set(x, (top + 1) % SIZE, r[1])
        first = (row * 12 + int(hashed(row, seed, 151) * 10)) % SIZE
        for jx in (first, (first + 24) % SIZE):
            for y in range(1, 24):
                py = (top + y) % SIZE
                c.set(jx, py, r[4])
                c.set((jx + 1) % SIZE, py, r[1] if y > 1 else r[2])
    # Wear: one variant in three has a cracked flag, one in three a seam of moss in a joint.
    if seed % 3 == 1:
        crack(c, 10 + int(hashed(seed, 1, 152) * 28), 6 + int(hashed(seed, 2, 153) * 12), 9, r[4], 154)
    elif seed % 3 == 2:
        g = ramp("lichen")
        x = (int(hashed(seed, 3, 155) * 10) + 12) % SIZE
        for y in range(26, 26 + 6 + int(hashed(seed, 4, 156) * 8)):
            c.set(x, y, g[4] if y % 2 else g[3])


def natural(c, r, seed, accent, base=3):
    """An open floor that reads as ground rather than as noise.

    THE OLD FLOOR WAS STATIC: `deep` with 20% `occ` and 10% `shade` pixels hashed independently,
    and on the Ruins, the Cave and the Frozen Waste that came out as a black sandpaper with the
    blob terrains stamped on it as bright rectangles. This is one tone with soft blotches a step
    either side, a few drawn incidents (a crack, a pebble) and almost no single pixels.

    `accent` names one extra row for the blotches, which is all that separates the floors:
    sand is gold with flesh in it, mud is linen with peat, basalt is stone with violet. `base`
    is which stop of the row the floor stands on - `shade` for most, `deep` for sand, whose
    shade is a full step brighter than any other floor's.
    """
    a = ramp(accent) if accent else r
    other = r[3] if base == 4 else r[4]
    # Sand is the bright floor, so it gets half the incident: at the same counts it read as
    # sandpaper again, just yellow.
    calm = base == 4
    c.rect(0, 0, SIZE - 1, SIZE - 1, r[base])
    clumps(c, 5 if calm else 10, 1, 3, other, 40 + seed, fill=0.15 if calm else 0.35)
    clumps(c, 3 if calm else 6, 1, 2, a[4] if accent else r[2], 50 + seed, fill=0.3)
    specks(c, 0.004 if calm else 0.012, r[2], 60 + seed)
    specks(c, 0.008, OCC, 62 + seed)
    # Incidents on some variants only. One on every tile is a pattern with a 48 px period, which
    # is the failure this whole file keeps re-learning. Sand does not crack.
    if seed % 3 == 0 and not calm:
        crack(c, inside(seed, 70, 8), inside(seed + 3, 71, 8), 5 + int(hashed(seed, 1, 72) * 5), OCC, 73 + seed)
    if seed % 2 == 1:
        pebble(c, inside(seed * 3, 74, 5), inside(seed * 3 + 1, 75, 5), 1 + seed % 3 // 2)


def snow(c, seed):
    """The Frozen Waste's floor: wind-packed snow under no light.

    It stood on `graystone`, the Ruins' slate, with ice stamped on it - the darkest floor in the game
    under a chapter that is a snowfield, and a player read "night" before "frozen" (0.10.0 ground
    pass). Snow is the one ground that should be PALE, so this floor stands on the bone row's deep
    (`skin.deep`, #38404F) - a full step above every other open floor, and still the deep half of a
    row, so a steel-and-wool thane or a whalebone hound is the darker thing standing on it, ringed
    by its rim. The order on screen is snow, then the ice a step under it, then the black water:
    matte, glassy, open.

    ALMOST NOTHING IS DRAWN ON IT, on purpose. The first version cut sastrugi - wind ridges - into
    every cell, and a field of them was a printed carpet with a 48 px repeat: a ridge has a shape,
    and a shape in a fill tile is a pattern. The wind is the detail layer's now (ground_detail.py's
    `sastrugi`, and the drifts and frost already there), placed in world space in drifts. What is
    left here is grain a step either side, one long low ridge in one variant in three, and the odd
    stone or dead stalk through the crust.
    """
    sk = ramp("skin")
    stl = ramp("steel")
    st = ramp("stone")
    ln = ramp("linen")
    c.rect(0, 0, SIZE - 1, SIZE - 1, sk[4])
    specks(c, 0.006, stl[3], 3505)
    specks(c, 0.005, st[1], 3507)
    if seed % 3 == 2:
        # one long low ridge, lying along the wind (off the north-west), lit crest over its lee
        run = 18 + int(hashed(seed, 1, 3521) * 12)
        x0 = 4 + int(hashed(seed, 2, 3522) * (SIZE - 8 - run))
        y0 = 12 + int(hashed(seed, 3, 3523) * (SIZE - 24))
        for j in range(run):
            u = j / float(run - 1)
            y = y0 + int(round(-0.18 * j - math.sin(u * math.pi) * 1.5))
            c.set(x0 + j, y, stl[3])
            if 0.15 < u < 0.85:
                c.set(x0 + j, y + 1, st[1])
    # Incidents, one variant in six each: a stone the wind has bared, a few dead stalks through the
    # crust. A dark thing on snow is the most visible thing here, and at one variant in three each
    # they laid out the grid. The rest is snow, which is the point.
    if seed == 0:
        pebble(c, inside(seed, 3531, 7), inside(seed + 3, 3532, 7), 1)
    elif seed == 4:
        x, y = inside(seed, 3533, 8), inside(seed + 5, 3534, 8)
        for k in range(3):
            for j in range(2 + k % 2):
                c.set(x + k * 2, y - j - (k % 2), ln[3] if j else ln[4])


def floor_variant(mat, style, seed, accent=None):
    def draw():
        global VARIANT
        VARIANT = 0
        c = raster.Canvas(SIZE, SIZE, None)
        r = ramp(mat)
        if style == "void":
            # Maze walls. Flat occlusion and nothing else: rock the player cannot enter, drawn as
            # the absence of a floor. `occ` rather than #000 because the contract has no pure black
            # - occ is its darkest tone and the one every contact shadow already uses.
            c.rect(0, 0, SIZE - 1, SIZE - 1, OCC)
            return c
        if style == "flagstone":
            flagstone(c, r, seed)
            return c
        if style == "basalt":
            basalt(c, seed)
            return c
        if style == "snow":
            snow(c, seed)
            return c
        if style == "natural":
            natural(c, r, seed, accent, 4 if mat == "gold" else 3)
            if mat == "gold":
                # Two, one stop up: six in the base tone read as a printed carpet of brackets.
                ripples(c, r[3], r[4], 90 + seed, 2)
            return c
        # The old darktile. No palette paints it any more (Volcanic is basalt and the Swamp is mud
        # now), but StageEnvironmentCatalog still names its PNGs as a background texture.
        c.rect(0, 0, SIZE - 1, SIZE - 1, r[4])
        for y in range(SIZE):
            for x in range(SIZE):
                v = hashed(x, y, 40 + seed)
                if v < 0.20:
                    c.set(x, y, OCC)
                elif v > 0.90:
                    c.set(x, y, r[3])
        if style == "tiled":
            for k in (0, 24):
                for x in range(SIZE):
                    c.set(x, (k + seed * 3) % SIZE, OCC)
                for y in range(SIZE):
                    c.set((k + seed * 3) % SIZE, y, OCC)
            for k in (1, 25):
                for x in range(SIZE):
                    c.set(x, (k + seed * 3) % SIZE, r[3])
        return c
    return draw


# --- coverage: how much of a role tile is terrain --------------------------------------------------
#
# Every role is the body with a coverage mask cut out of it. Coverage is a function of a signed
# distance `e` from the terrain's boundary inside the tile (positive = inside the terrain), and
# the edge profile decides where that boundary sits, how wide the fade across it is, how round the
# corners are and how much the line wanders.

ROLES = [
    "fill",
    "edge_top", "edge_bottom", "edge_left", "edge_right",
    "corner_tl", "corner_tr", "corner_bl", "corner_br",
    "inner_corner_tl", "inner_corner_tr", "inner_corner_bl", "inner_corner_br",
    "patch",
]

SIDES = {
    "fill": (),
    "edge_top": ("t",), "edge_bottom": ("b",), "edge_left": ("l",), "edge_right": ("r",),
    "corner_tl": ("t", "l"), "corner_tr": ("t", "r"),
    "corner_bl": ("b", "l"), "corner_br": ("b", "r"),
    "patch": ("t", "b", "l", "r"),
}

INNER = {"inner_corner_tl": ("t", "l"), "inner_corner_tr": ("t", "r"),
         "inner_corner_bl": ("b", "l"), "inner_corner_br": ("b", "r")}

# b: where the boundary (half coverage) sits, in pixels in from the exposed side.
# w: how wide the fade across it is; 0 is a hard edge.
# r: outer-corner radius.  amp: how far the line wanders either way along an edge.
PROFILES = {
    # Ground lying on ground. Wide and soft: these are the transitions that were jarring.
    "soft": dict(b=15.0, w=22.0, r=30.0, amp=6.0),
    # Hazards. Crisp, with an `occ` lip - a lava edge that faded into the grass would be lying
    # about where the damage starts. Rounded all the same: the staircase was the other half of
    # what made every blob look stamped.
    "hard": dict(b=5.0, w=0.0, r=14.0, amp=1.5),
}

# Terrain -> profile. Anything not listed is "soft".
PROFILE_OF = {"lava": "hard", "pit": "hard"}

# Terrain -> a function edge(c, role) that cuts and draws the role's edge itself, in place of a
# profile's mask. For ground somebody LAID - a marble floor, oak boards, a carpet runner (the Court's,
# terrain_court.py) - whose edge is a straight, square, drawn border rather than a fade: a carpet that
# dithered into the flags under it would read as a stain. A chapter module adds its own entries here
# (`tiles.EDGE_OF[name] = fn`); every terrain not listed keeps its profile.
EDGE_OF = {}


def wander(side, i, amp):
    """How far the boundary wanders at position i along a side, in pixels.

    Sums of sin(pi * n * i / 48): every term is ZERO at both ends of the tile, so any variant of
    any edge meets any variant of the next one exactly - the joins are pinned and only the middle
    of each tile wanders. Phase and mix change per variant, which is what lets a long straight
    shore be three different wiggles rather than one wiggle repeated.
    """
    si = "tblr".index(side)
    a1 = hashed(si, 1, 1700) * 2.0 - 1.0
    a2 = hashed(si, 2, 1701) * 2.0 - 1.0
    a3 = hashed(si, 3, 1702) * 2.0 - 1.0
    t = math.pi * i / float(SIZE - 1)
    return amp * (0.6 * a1 * math.sin(t) + 0.3 * a2 * math.sin(2 * t) + 0.25 * a3 * math.sin(3 * t))


def side_depth(side, x, y):
    """Pixels in from `side`, and the position along it."""
    if side == "t":
        return y + 0.5, x
    if side == "b":
        return SIZE - 0.5 - y, x
    if side == "l":
        return x + 0.5, y
    return SIZE - 0.5 - x, y


ADJACENT = [("t", "l"), ("t", "r"), ("b", "l"), ("b", "r")]


def signed_distance(role, x, y, prof):
    """Signed distance from the terrain boundary at (x, y); positive is inside the terrain."""
    if role in INNER:
        v, h = INNER[role]
        dv, iv = side_depth(v, x, y)
        dh, ih = side_depth(h, x, y)
        # The ground is a quarter-disc round the corner, of the same radius the edges either side
        # of it put their boundary at - so at the top row it meets the tile above's left edge,
        # and at the left column the tile beside it's top edge.
        bv = prof["b"] + wander(v, iv, prof["amp"])
        bh = prof["b"] + wander(h, ih, prof["amp"])
        # Near the top row the neighbour that matters is the tile above, whose boundary is the
        # h side's; near the left column it is the v side's. Blend between them by which is near.
        wsum = dv + dh
        b = (bv * dv + bh * dh) / wsum if wsum > 0 else prof["b"]
        return math.hypot(dv, dh) - b

    sides = SIDES[role]
    if not sides:
        return 99.0
    es = {}
    for s in sides:
        d, i = side_depth(s, x, y)
        es[s] = d - (prof["b"] + wander(s, i, prof["amp"]))
    e = min(es.values())
    r = prof["r"]
    for a, b in ADJACENT:
        if a in es and b in es and es[a] < r and es[b] < r:
            e = min(e, r - math.hypot(r - es[a], r - es[b]))
    return e


def apply_mask(c, role, prof, rim_tone):
    """Cut the role's coverage out of a painted body.

    Soft profiles DITHER, in 2x2 blocks: per pixel the fade came out furry and sand-blasted, and
    the same coverage in blocks reads as an irregular edge. Hard profiles cut at the boundary and
    draw a lip inside it.
    """
    w = prof["w"]
    for y in range(SIZE):
        for x in range(SIZE):
            e = signed_distance(role, x, y, prof)
            if e >= w * 0.5 + 0.5 and w > 0:
                continue
            if w <= 0:
                if e < 0:
                    c.set(x, y, CLEAR)
                elif e < 1.2:
                    c.set(x, y, OCC)
                elif e < 2.6:
                    c.set(x, y, rim_tone)
                continue
            cover = (e + w * 0.5) / w
            if cover <= 0:
                c.set(x, y, CLEAR)
                continue
            # The threshold is per 2x2 block, so the edge breaks up in blocks, not in pixels.
            if hashed(x // 2, y // 2, 1800) * 0.92 + 0.04 > cover:
                c.set(x, y, CLEAR)


def variants_for(role):
    if role == "fill":
        return FILL_VARIANTS
    if role.startswith("edge_"):
        return EDGE_VARIANTS
    return CORNER_VARIANTS


def emit_terrain(name, painter, rim_tone, roles):
    global VARIANT
    out = []
    for role in roles:
        for variant in range(variants_for(role)):
            VARIANT = variant
            c = raster.Canvas(SIZE, SIZE, None)
            painter(c)
            if role != "fill":
                if name in EDGE_OF:
                    EDGE_OF[name](c, role)
                else:
                    prof = PROFILES[PROFILE_OF.get(name, "soft")]
                    apply_mask(c, role, prof, rim_tone)
            out.append((role, variant, c))
            if name == "water":
                break
    return out


# terrain -> (painter, rim tone for a hard edge)
TERRAINS = [
    ("grass", body_grass, None),
    ("meadow", body_meadow, None),
    ("moss", body_moss, None),
    ("dirt", body_dirt, None),
    ("overgrowth", body_overgrowth, None),
    ("sand", body_sand, None),
    ("cobble", body_cobble, None),
    ("dark_dirt", body_dark_dirt, None),
    ("mossy_rock", body_mossy_rock, None),
    ("ice", body_ice, None),
    ("water", body_water, None),
    ("lava", body_lava, ramp("stone")[4]),
    ("pit", body_pit, ramp("stone")[4]),
    ("rocky_pit", body_scree, None),
]

# The maze walls: the `wall` terrain, every role, plain variants and dressed ones (jail_walls.py).
WALL_TERRAIN = "wall"
# The shadow a wall throws on the floor beside it: transparent tiles the painter lays on floor cells.
WALL_SHADOW_TERRAIN = "wall_shadow"

HAZARDS = {"lava", "pit"}

# Terrains the player cannot walk into. A hazard hurts you for standing in it; a blocker stops you
# entering at all, and the two are independent - water is not a hazard and lava is not a blocker.
# LevelTilePainter reads this out of the manifest and gives the matching tiles a collision polygon.
BLOCKERS = {"water"}

# material -> (row, style, seeds, accent row)
MATERIALS = [
    ("sand", "gold", "natural", 6, "flesh"),
    ("stonetile", "stone", "flagstone", 6, None),
    ("graystone", "stone", "natural", 6, None),
    # Swamp ground: grey-green peat with darker hollows. Was `darktile`, a near-black square grid.
    ("mud", "linen", "natural", 6, "flesh"),
    # Emberdeep ground: basalt plates split by seams that smoulder (basalt() above). Was `darktile`,
    # then the `natural` floor with violet in it - a calm slate that said nothing about the place.
    ("basalt", "stone", "basalt", 6, None),
    # Frozen Waste ground: wind-packed snow on the bone row's deep (snow() above). It stood on
    # `graystone`, the darkest floor in the game under a chapter that is a snowfield.
    ("snow", "skin", "snow", 6, None),
    # No palette paints darktile now; kept because StageEnvironmentCatalog names its PNGs.
    ("darktile", "stone", "tiled", 4, None),
    # The old maze wall: a flat `occ` tile. The jail walls (jail_walls.py) replaced it on the Cursed
    # Dungeon; LevelTilePainter still paints it if the `wall` terrain is ever missing a role, so a
    # wall is never painted as floor.
    ("void", "stone", "void", 1, None),
]


# A CHAPTER'S GROUND IN ITS OWN FILE (.ai/campaign.md). A chapter added after the first nine draws
# its terrains and floor materials in tools/art/terrain_<chapter>.py, which may `import tiles` for
# the helpers above and defines TERRAINS and/or MATERIALS in the same shapes as the lists here. They
# are appended below, so two chapters drawn in parallel never edit this file. A chapter may not add a
# hazard or a blocker: either has to agree with three other places (.ai/terrain.md) that no chapter
# owns, so a module that sets HAZARDS or BLOCKERS stops the run.
def chapter_ground_modules():
    """The chapter ground modules present, tools/art/terrain_<chapter>.py, in name order."""
    import glob
    here = os.path.dirname(os.path.abspath(__file__))
    return sorted(os.path.splitext(os.path.basename(p))[0]
                  for p in glob.glob(os.path.join(here, "terrain_*.py")))


def _add_chapter_ground():
    # Run as a script this module is __main__; a chapter module's `import tiles` must get THIS copy
    # (the one whose lists are being extended), not a second one loaded from disk.
    sys.modules.setdefault("tiles", sys.modules[__name__])
    names = set([t[0] for t in TERRAINS] + [m[0] for m in MATERIALS])
    for mod_name in chapter_ground_modules():
        mod = __import__(mod_name)
        if getattr(mod, "HAZARDS", None) or getattr(mod, "BLOCKERS", None):
            raise ValueError("tools/art/%s.py adds a hazard or blocker; see .ai/campaign.md" % mod_name)
        for entry in list(getattr(mod, "TERRAINS", [])) + list(getattr(mod, "MATERIALS", [])):
            if entry[0] in names:
                raise ValueError("tools/art/%s.py redefines ground %r" % (mod_name, entry[0]))
            names.add(entry[0])
        TERRAINS.extend(getattr(mod, "TERRAINS", []))
        MATERIALS.extend(getattr(mod, "MATERIALS", []))


_add_chapter_ground()


# A CHAPTER'S WALLS IN THEIR OWN FILE (.ai/endgame-castle.md 11.3). The grid's wall cells are always the
# `wall` terrain - collision, navigation and the chunk flags read that - and a chapter chooses only how
# they are DRAWN (StageEnvironmentProfile.WallSet). The Cursed Dungeon's jail is jail_walls.py, drawn as
# the `wall` terrain itself. Any other set is tools/art/<name>_walls.py, found here by its name like the
# ground modules above, and drawn under the terrain id its module names as TERRAIN (cave_walls.py ->
# `cave_wall`, ruin_walls.py -> `ruin_wall`, court_walls.py -> `court_wall`). A set module has the jail's
# shape: STUB_SIDES, variants(role) and paint(c, role, dressing, plain_index, sides, inner), every role
# the jail has (RegressionChecks.ValidateGroundArt holds a set to all of them the moment it exists). The
# shadow a wall throws stays the jail's, one set for every wall.
def wall_set_modules():
    """[(terrain id, module)]: the jail's first, then every tools/art/*_walls.py in name order."""
    import glob
    here = os.path.dirname(os.path.abspath(__file__))
    sets = [(WALL_TERRAIN, jail_walls)]
    for path in sorted(glob.glob(os.path.join(here, "*_walls.py"))):
        name = os.path.splitext(os.path.basename(path))[0]
        if name == "jail_walls":
            continue
        mod = __import__(name)
        terrain = getattr(mod, "TERRAIN", None)
        if not terrain or terrain == WALL_TERRAIN:
            raise ValueError("tools/art/%s.py names no TERRAIN of its own" % name)
        sets.append((terrain, mod))
    return sets


def emit_walls(frames, only=None):
    """The wall sets - the Cursed Dungeon's jail (jail_walls.py) and every chapter's own (wall_set_modules)
    - and the shadows walls throw on the floor.

    Every one of the fourteen roles, because the maze's two-tile walls produce inner corners as
    well as edges and the painter asks for the role the autotiler resolved - plus three for the
    one-tile stubs the spawn clearing leaves (jail_walls.STUB_SIDES). A dressed variant
    carries its dressing's name and weight in the manifest: LevelTilePainter paints a role's plain
    variants unless a cell rolls a dressed one, never two dressed side by side, and picks among the
    dressed ones by weight. Wall tiles carry no collision of their own - the painter gives each one
    the full-cell square on the terrain layer, as it did the void tile - and are not `blocking`
    terrain: walls are their own flag (TerrainStreamer.FlagWall), not water.

    `only`: on a partial run, the wall sets (terrain ids) to redraw; None redraws them all.

    Returns (tiles written, any failed the palette audit). Tiles left over from an older set (the
    outlined `wall` terrain had different variant counts) are deleted with their .import, so the
    atlas and the manifest never pick up a stale one.
    """
    n = 0
    bad_any = False
    for terrain, module in wall_set_modules():
        if only and terrain not in only:
            continue
        k, bad = emit_wall_set(frames, terrain, module, shadows=(terrain == WALL_TERRAIN))
        n += k
        bad_any = bad_any or bad
    return n, bad_any


def emit_wall_set(frames, terrain, module, shadows):
    """One wall set: every role, plain and dressed, under `terrain`; with `shadows`, the floor shadows."""
    written = set()
    bad_any = False
    n = 0

    def write(tid, c, terrain, role, dressing=None, weight=1):
        bad = c.audit()
        if bad:
            print("OFF-CONTRACT COLOURS in %s:" % tid)
            for h, k in bad[:4]:
                print("   %s  x%d" % (h, k))
            return False
        c.save(os.path.join(OUT_DIR, tid + ".png"))
        written.add(tid + ".png")
        extra = ''
        if dressing:
            extra = ',\n   "dressing": "%s", "weight": %d' % (dressing, weight)
        frames.append(
            '  {"id": "%s", "fileName": "%s.png", "path": "assets/bonelight/tiles/%s.png",\n'
            '   "placement": "floor", "usageClass": "tile", "collision": "decoration",\n'
            '   "hazard": false, "blocking": false, "width": 48, "height": 48,\n'
            '   "autotile": {"terrain": "%s", "role": "%s"}%s}'
            % (tid, tid, tid, terrain, role, extra))
        return True

    dressed = 0
    for role in ROLES + sorted(module.STUB_SIDES):
        sides = module.STUB_SIDES.get(role) or SIDES.get(role, ())
        for v, (dressing, plain_index, weight) in enumerate(module.variants(role)):
            c = raster.Canvas(SIZE, SIZE, None)
            module.paint(c, role, dressing, plain_index, sides, INNER.get(role))
            tid = "%s_%s" % (terrain, role) if v == 0 else "%s_%s_%d" % (terrain, role, v)
            if write(tid, c, terrain, role, dressing, weight):
                n += 1
                dressed += 1 if dressing else 0
            else:
                bad_any = True
    shadow_count = 0
    if shadows:
        for role in jail_walls.SHADOW_ROLES:
            c = raster.Canvas(SIZE, SIZE, None)
            jail_walls.shadow(c, role)
            if write("%s_%s" % (WALL_SHADOW_TERRAIN, role), c, WALL_SHADOW_TERRAIN, role):
                n += 1
                shadow_count += 1
            else:
                bad_any = True

    # The jail's prefix `wall_` is also the start of `wall_shadow_`, which is why the shadows are drawn
    # with the jail and are in `written` then; a chapter's prefix (`cave_wall_`) is its own.
    stale = [f for f in os.listdir(OUT_DIR)
             if f.startswith(terrain + "_") and f.endswith(".png") and f not in written]
    for f in stale:
        os.remove(os.path.join(OUT_DIR, f))
        if os.path.exists(os.path.join(OUT_DIR, f + ".import")):
            os.remove(os.path.join(OUT_DIR, f + ".import"))
    print("  %-12s %d tiles over %d roles (%d dressed)%s%s"
          % (terrain, n - shadow_count, len(ROLES) + len(module.STUB_SIDES), dressed,
             (", %d shadows" % shadow_count) if shadows else "",
             (", %d stale removed" % len(stale)) if stale else ""))
    return n, bad_any


def main(only=None):
    """`only`: names of terrains/materials to redraw while iterating. The manifest is written only
    on a full run, so a partial one can never drop the rest of the ground out of tiles.json."""
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)

    ok = True
    frames = []
    count = 0

    wall_terrains = [t for t, _ in wall_set_modules()]
    if not only or any(t in only for t in wall_terrains):
        n, bad_walls = emit_walls(frames, only)
        ok = ok and not bad_walls
        count += n

    for name, painter, rim in TERRAINS:
        if only and name not in only:
            continue
        # Water keeps one fill tile only: see body_water.
        roles = ["fill"] if name == "water" else ROLES
        n = 0
        for role, variant, c in emit_terrain(name, painter, rim, roles):
            bad = c.audit()
            if bad:
                ok = False
                print("OFF-CONTRACT COLOURS in %s/%s:" % (name, role))
                for h, k in bad[:4]:
                    print("   %s  x%d" % (h, k))
                continue
            tid = "%s_%s" % (name, role) if variant == 0 else "%s_%s_%d" % (name, role, variant)
            c.save(os.path.join(OUT_DIR, tid + ".png"))
            frames.append(
                '  {"id": "%s", "fileName": "%s.png", "path": "assets/bonelight/tiles/%s.png",\n'
                '   "placement": "floor", "usageClass": "tile", "collision": "%s",\n'
                '   "hazard": %s, "blocking": %s, "width": 48, "height": 48,\n'
                '   "autotile": {"terrain": "%s", "role": "%s"}}'
                % (tid, tid, tid,
                   "blocking" if name in BLOCKERS else "decoration",
                   "true" if name in HAZARDS else "false",
                   "true" if name in BLOCKERS else "false",
                   name, role))
            count += 1
            n += 1
        print("  %-12s %d tiles over %d roles" % (name, n, len(roles)))

    for mat, row, style, seeds, accent in MATERIALS:
        if only and mat not in only:
            continue
        for seed in range(seeds):
            c = floor_variant(row, style, seed, accent)()
            bad = c.audit()
            if bad:
                ok = False
                print("OFF-CONTRACT COLOURS in %s_%d" % (mat, seed))
                for h, k in bad[:4]:
                    print("   %s  x%d" % (h, k))
                continue
            tid = "floor_%s_%d" % (mat, seed)
            c.save(os.path.join(OUT_DIR, tid + ".png"))
            frames.append(
                '  {"id": "%s", "fileName": "%s.png", "path": "assets/bonelight/tiles/%s.png",\n'
                '   "placement": "floor", "materialFamily": "%s", "usageClass": "tile",\n'
                '   "collision": "decoration", "hazard": false, "width": 48, "height": 48}'
                % (tid, tid, tid, mat))
            count += 1
        print("  %-12s %d floor variants" % (mat, seeds))

    if only:
        print("partial run (%s): tiles.json left alone" % ", ".join(only))
        # The tiles it did redraw are in the atlas the game draws from, so it is repacked anyway.
        tile_atlas.main()
        return 0 if ok else 1

    manifest = ('{\n'
                '  "_comment": "Generated by tools/art/tiles.py - the ground every stage is '
                'fought on. Ours, replacing the bought dungeon-floor pack. One line: a raw '
                'newline inside a JSON string is invalid and JsonDocument.Parse throws on it.",\n'
                '  "frames": [\n%s\n  ]\n}\n' % (",\n".join(frames)))
    # A str on Python 3, bytes on 2.7: tile_atlas.py and ground_detail.py take both the same way.
    io.open(os.path.join(OUT_DIR, "tiles.json"), "w", encoding="utf-8",
            newline="\n").write(manifest if isinstance(manifest, type(u"")) else manifest.decode("utf-8"))
    # The game draws the ground from one packed texture, not from these files one by one.
    tile_atlas.main()

    if not ok:
        return 1
    print("")
    print("%d tiles across %d terrains, %d wall sets and %d materials, palette clean"
          % (count, len(TERRAINS), len(wall_terrains), len(MATERIALS)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:] or None))
