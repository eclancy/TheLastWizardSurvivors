# -*- coding: utf-8 -*-
"""Original spell art: card icons, and the projectile sprites that go with them.
Run: python tools/art/spell_icons.py

Three spell icons in the game were already drawn for this project rather than taken from the third
party pack - they are the ones named ui-derived-spell-icon-*. This generator is where any further
ones come from, so the set stops growing by hand.

A spell needs two pictures and they are not the same picture. The card icon is looked at while the
game is paused, square, on a dark plate, and it can carry detail. The projectile is looked at for
half a second, in motion, rotated to its heading, with forty enemies behind it - so it is drawn
wide, facing +X (ElementalBolt sets Rotation from the travel direction), and it spends most of its
pixels on the leading edge, which is the only part the eye has time to read.

OUTPUT LOCATION, and why it is not assets/bonelight/. Every SpellData .tres points its Icon at
assets/organized/ui/, and the level-up card, the spellbook and the HUD all read that field. Writing
somewhere tidier would mean moving twenty existing icons at the same time, which is the art
replacement manifest's job and not this one. So a new icon lands beside the icons it sits next to,
under the same ui-derived-spell-icon- name the other three use.

SIZE. Authored at 32 and rendered at x2, giving a 64px file - the contract's 32x32 UI cell for an
icon that IS the content rather than a glyph labelling a control. The existing spell icons are a
mix of 32 and 64 and the card scales either, so the contract wins.

Light model per art-direction.md section 2: key from the upper left. On a wave that means the face
climbing toward the curl is lit and the trough under it is shaded, which is most of what makes a
lump of blue read as water.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bonelight as bl
import raster
# The world's tree painter (tools/art/trees.py), for Windfall's crown. Imported here, never inside a painter:
# evolution_icons.paint() swaps raster.Canvas while a painter runs, and trees subclasses it at import.
import trees as T

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
# WHERE THESE GO, and why it was moved.
#
# They used to be written into assets/organized/ui/ under a "ui-derived-spell-icon-" prefix, which
# was true once - the first ones really were cut down from a bought GUI pack - and has not been
# true since they were all redrawn. The cost of leaving them there was not tidiness: that
# directory is the bought art, all of it is licensed for use and not for redistribution, and the
# removal that has to happen before this repo can be public is `rm -r assets/organized`. Twenty-six
# pieces of our own art were sitting in the blast radius. See .ai/asset-licensing.md.
OUT_DIR = os.path.join(ROOT, "assets", "bonelight", "ui", "spells")

CELL = 32


def riptide():
    """A breaking wave, curl at the top right, undertow dragging left along the floor.

    Two shapes, not one. A wave drawn as a single mass reads as a blue hill; what makes it a wave
    is the gap under the curl - the lip throws forward and there is air beneath it. So the crest is
    drawn as a body plus a separate lip, with occlusion between them.

    Foam is water.core, the brightest tone in the element ramp, and it is used sparingly: a rim
    along the lip and a few broken flecks where the lip is about to fall. Foam everywhere would
    flatten the whole icon to one value, which at 32 pixels is the difference between a wave and a
    smear.
    """
    c = raster.Canvas(CELL, CELL, None)

    deep = bl.tone("azure", "deep")
    shade = bl.tone("azure", "shade")
    base = bl.tone("azure", "base")
    lit = bl.tone("azure", "lit")
    hi = bl.tone("azure", "hi")
    foam = bl.ELEMENTS["water"][0]

    # The body: a swell rising left to right, sitting on the floor of the cell.
    body = [
        (1, 29), (2, 24), (5, 19), (10, 14), (16, 10),
        (22, 9), (26, 11), (28, 15), (27, 20), (24, 24),
        (24, 29), (1, 29),
    ]
    c.poly_shade(body, [hi, lit, base, shade, deep], ang=-0.7854, bias=0.15)

    # The lip: the part that has already thrown forward, curling back over the trough.
    lip = [
        (16, 10), (22, 9), (27, 12), (28, 17), (25, 19),
        (26, 15), (23, 12), (18, 12), (16, 10),
    ]
    c.poly_shade(lip, [foam, hi, lit, base, shade], ang=-0.7854, bias=0.35)

    # The air under the lip. This is the shape that makes it a wave rather than a hill, so it is
    # drawn as occlusion rather than as a darker blue - a hole reads as a hole only if it is the
    # darkest thing in the icon.
    hollow = [(20, 14), (25, 15), (26, 18), (24, 20), (21, 18), (20, 14)]
    c.poly(hollow, OCC)

    # Foam along the leading edge of the lip, broken rather than continuous.
    for (x, y) in [(17, 10), (19, 9), (21, 9), (23, 10), (25, 11), (27, 13), (28, 16)]:
        c.set(x, y, foam)
    for (x, y) in [(18, 11), (22, 10), (26, 12)]:
        c.set(x, y, hi)

    # Spray: three flecks thrown clear of the curl, ahead of it and above it. They also stop the
    # icon from ending in a hard silhouette edge on the right, which at this size looks cut off.
    for (x, y) in [(29, 10), (26, 6), (21, 5)]:
        c.set(x, y, foam)
    c.set(30, 12, hi)
    c.set(23, 7, lit)

    # The undertow: two drag lines along the floor running back the other way, which is what the
    # spell actually does - it travels through the crowd rather than landing on it.
    c.hline(2, 12, 27, lit)
    c.hline(4, 9, 25, hi)
    c.hline(6, 18, 29, base)

    # Occlusion under the whole mass, one row, so the icon sits on the card rather than floating.
    c.hline(1, 24, 30, OCC)
    c.set(0, 29, OCC)
    c.set(25, 29, OCC)

    return c


def cinderbreath():
    """A cone of fire thrown from the lower left toward the upper right.

    The icon has to say "sustained" rather than "explosion", and the thing that carries that is the
    NOZZLE - a narrow bright root the flame comes out of. A cone with no root is a blast; a cone
    with a root is something being poured. So the icon spends real pixels on a tiny throat at the
    bottom left, which is most of the difference between this and the existing explosion icons.

    Colour runs the other way to intuition: white-hot at the root, orange in the body, dark red at
    the ragged leading edge, because a flame is coolest where it is thinnest.
    """
    c = raster.Canvas(CELL, CELL, None)

    edge = bl.ELEMENTS["fire"][3]
    mid = bl.ELEMENTS["fire"][2]
    hot = bl.ELEMENTS["fire"][1]
    core = bl.ELEMENTS["fire"][0]
    iron = bl.tone("steel", "shade")
    iron_lit = bl.tone("steel", "lit")

    # The envelope, widening toward the upper right. Drawn as one polygon and then eaten into by
    # the brighter layers, rather than three separate cones - overlapping cones leave seams.
    # Apex at the nozzle, widest at the far cap - a cone, not a lens. The first pass tapered at both
    # ends, which read as a leaf: the whole point is that it gets WIDER the further it is thrown.
    outer = [(7, 27), (15, 14), (22, 6), (26, 3), (30, 6), (31, 11), (29, 16), (24, 20), (16, 25), (7, 27)]
    c.poly(outer, edge)

    body = [(8, 26), (15, 15), (21, 8), (25, 6), (28, 9), (28, 14), (25, 17), (19, 21), (8, 26)]
    c.poly(body, mid)

    inner = [(8, 26), (14, 17), (19, 11), (23, 9), (24, 13), (22, 16), (17, 19), (8, 26)]
    c.poly(inner, hot)

    # The white core, short - it never reaches the leading edge.
    throat = [(8, 26), (13, 19), (17, 14), (19, 13), (18, 17), (14, 20), (8, 26)]
    c.poly(throat, core)

    # Broken flecks past the leading edge: fire sheds, and a hard silhouette edge reads as a wedge.
    for (x, y) in [(29, 2), (31, 14), (26, 22), (19, 27)]:
        c.set(x, y, edge)
    for (x, y) in [(28, 4), (28, 18)]:
        c.set(x, y, mid)

    # The nozzle, overlapping the apex so the flame is coming OUT of something rather than floating
    # near it. Two pixels of lit steel and a bright throat is the whole "sustained" read.
    c.rect(2, 26, 7, 30, iron)
    c.hline(2, 7, 26, iron_lit)
    c.vline(2, 26, 30, iron_lit)
    c.set(7, 27, core)
    c.set(7, 28, hot)
    c.set(6, 28, core)

    return c



def _ellipse(cx, cy, rx, ry, angle, n=20):
    """A turned ellipse as a polygon (raster.Canvas.disc only lies along the axes)."""
    ca, sa = math.cos(angle), math.sin(angle)
    pts = []
    for k in range(n):
        t = 2.0 * math.pi * k / n
        u, v = math.cos(t) * rx, math.sin(t) * ry
        pts.append((cx + u * ca - v * sa, cy + u * sa + v * ca))
    return pts


def _bog_print(c, cx, cy, size, angle, fresh):
    """One footprint welled up with bog, seen from above: a sole and a heel behind it along `angle`
    (the way he walked), each a hole with a lip of mud round it - the lip lit on its upper left by the
    key, the water dark at the bottom of the hole - and, in a fresh one, scum and a bubble on it."""
    mud_lit = bl.tone("gold", "base")
    mud = bl.tone("gold", "shade")
    mud_deep = bl.tone("gold", "deep")
    water = bl.tone("azure", "shade")
    water_deep = bl.tone("azure", "deep")
    water_lit = bl.tone("azure", "base")
    shine = bl.tone("azure", "lit")
    dx, dy = math.cos(angle), math.sin(angle)
    heel = (cx - dx * size * 1.25, cy - dy * size * 1.25)
    sole = (cx + dx * size * 0.45, cy + dy * size * 0.45)
    parts = ((sole, size * 0.98, size * 0.68), (heel, size * 0.52, size * 0.5))
    # The lip of mud: occlusion outside it, then the mud, lit toward the key.
    for (at, rx, ry) in parts:
        c.poly(_ellipse(at[0], at[1] + 0.6, rx + 1.4, ry + 1.4, angle), OCC)
    for (at, rx, ry) in parts:
        c.poly(_ellipse(at[0], at[1], rx + 1.0, ry + 1.0, angle), mud)
    for (at, rx, ry) in parts:
        c.poly(_ellipse(at[0] - 0.7, at[1] - 0.7, rx + 0.6, ry + 0.6, angle), mud_lit)
        c.poly(_ellipse(at[0] + 0.3, at[1] + 0.3, rx + 0.4, ry + 0.4, angle), mud)
    # The bog in the hole: dark at the bottom, the far wall (lower right) catching the key.
    for (at, rx, ry) in parts:
        c.poly(_ellipse(at[0], at[1], rx, ry, angle), mud_deep)
        c.poly(_ellipse(at[0] + 0.4, at[1] + 0.4, rx - 0.6, ry - 0.6, angle), water_lit if fresh else water)
        c.poly(_ellipse(at[0] - 0.3, at[1] - 0.3, rx - 1.0, ry - 1.0, angle), water)
        if rx > 2.5:
            c.poly(_ellipse(at[0] - 0.6, at[1] - 0.6, rx - 2.0, ry - 2.0, angle), water_deep)
    if fresh:
        # A bubble welling up in the sole, and a glint of sky on the water beside it.
        bx, by = int(round(sole[0] + dx * 1.5)), int(round(sole[1] + dy * 1.5 - 1))
        for (ox, oy) in ((0, -1), (-1, 0), (1, 0), (0, 1)):
            c.set(bx + ox, by + oy, shine)
        c.set(bx - 1, by - 1, bl.tone("azure", "hi"))
        c.set(int(round(sole[0] - dx * 2.5 - 1)), int(round(sole[1] - dy * 2.5 + 1)), shine)


def mirefoot():
    """Bog welling up in his footprints: two prints of a stride, the older one small up the way he came,
    the fresh one big and brimming, with scum on it and a reed standing out of it.

    Redrawn 2026-11-01 (Eric, playing 0.10.0: "Mirefoot icon has a random black line at the bottom.
    Make a better icon for it."). The line was a contact shadow, a row of occlusion run under the pool
    at the foot of the cell - a ground line under a hole in the ground, which said nothing. And the
    pool was a dark blue blob on a dark slot: a hole, not a puddle, and nothing in it said "footprint".
    Now the subject is the spell's own sentence - "bog wells up in your footprints" - and every print
    has a lip of mud lit by the key from the upper left (the gold row's soil, Gravewell's bank), so the
    hole reads on the dark loadout slot and on the pale page alike. Read from the top down, as the game is.
    """
    c = raster.Canvas(CELL, CELL, None)
    # He walked up the cell: the toes point the way he went, the other foot's print behind and beside.
    stride = math.radians(-78.0)
    # The older print, the other foot, smaller and gone still and dark.
    _bog_print(c, 8.0, 22.0, 3.4, stride, False)
    # The fresh one, brimming, low in the cell where the cards put their bog (evolution_icons.py tip).
    _bog_print(c, 19.0, 15.5, 6.0, stride, True)

    # Scum on the fresh one: broken patches, never a ring, or it reads as a tyre.
    scum = bl.tone("lichen", "base")
    scum_lit = bl.tone("lichen", "lit")
    for (x, y) in ((22, 15), (23, 15), (22, 16), (17, 23), (18, 23), (17, 11)):
        c.set(x, y, scum)
    for (x, y) in ((23, 14), (18, 22)):
        c.set(x, y, scum_lit)

    # A reed standing beside the fresh print, a leaf off it.
    reed = bl.tone("verdant", "shade")
    reed_lit = bl.tone("verdant", "lit")
    reed_base = bl.tone("verdant", "base")
    c.vline(28, 13, 25, reed)
    c.vline(29, 16, 25, reed_base)
    c.set(28, 13, reed_lit)
    c.set(28, 14, reed_lit)
    c.set(27, 17, reed)
    c.set(26, 16, reed_base)
    return c


def kindled_ward():
    """A made light: a bright core inside a halo, with four sparks coming off it.

    No lamp, no lantern body, no face. It is the thing the world-and-tone doc calls a ward - a piece
    of magic the player made and left burning - and giving it a housing would make it an object
    somebody built rather than something conjured. The whole icon is light.
    """
    c = raster.Canvas(CELL, CELL, None)

    edge = bl.tone("amber", "deep")
    outer = bl.tone("amber", "shade")
    mid = bl.tone("amber", "base")
    hot = bl.tone("amber", "lit")
    core = bl.ELEMENTS["light"][0]

    cx = cy = 15.5

    c.disc(cx, cy, 12.0, 12.0, edge)
    c.disc(cx, cy, 9.5, 9.5, outer)
    c.disc(cx, cy, 7.0, 7.0, mid)
    c.disc(cx, cy, 4.5, 4.5, hot)
    c.disc(cx, cy, 2.4, 2.4, core)

    # Four sparks on the diagonals, detached from the body so the halo reads as light rather than
    # as a solid ball with a rim.
    for (x, y) in [(5, 5), (26, 5), (5, 26), (26, 26)]:
        c.set(x, y, hot)
    for (x, y) in [(6, 6), (25, 6), (6, 25), (25, 25)]:
        c.set(x, y, mid)
    c.set(15, 1, hot)
    c.set(15, 30, mid)
    c.set(1, 15, mid)
    c.set(30, 15, hot)

    return c


def gravewell():
    """A pit in the ground, rim lit on the upper left, nothing but occlusion inside it.

    The inside is the darkest value in the palette and the rim is one of the lightest, because at
    this size contrast is the only thing that says "hole" rather than "disc". Clods thrown out
    around the lip stop it reading as a drawn circle.
    """
    c = raster.Canvas(CELL, CELL, None)

    # Deliberately the LIGHT end of the stone ramp. The first pass built the lip out of shade and
    # deep, which is what soil actually is, and the icon vanished: the card behind it is near-black,
    # so a dark rim around a black hole is a black square. On a dark plate the earth has to be lit
    # for the hole to be dark by comparison.
    # The EARTH element ramp, not the stone material ramp. Stone tops out at #3A4658 - it is a
    # shadow colour, built for masonry sitting in the dark - so a lip drawn from it disappeared
    # against the near-black card twice over. Earth runs warm and light and is what soil looks like
    # when something is lighting it.
    soil = bl.ELEMENTS["earth"][2]
    soil_lit = bl.ELEMENTS["earth"][1]
    soil_hi = bl.ELEMENTS["earth"][0]
    soil_shade = bl.ELEMENTS["earth"][3]
    gloom = bl.ELEMENTS["darkness"][3]

    # The lip: a squashed ellipse, because the camera looks down at an angle.
    c.disc(15.5, 17.0, 13.0, 10.0, soil)

    # Lit on the upper-left arc, shaded on the lower right - a raised bank, not a flat ring.
    for deg in range(0, 360, 3):
        a = math.radians(deg)
        facing = math.cos(a - math.radians(225.0))
        tone = soil_hi if facing > 0.6 else (soil_lit if facing > 0.0 else soil_shade)
        c.set(15.5 + math.cos(a) * 12.4, 17.0 + math.sin(a) * 9.4, tone)
        c.set(15.5 + math.cos(a) * 11.3, 17.0 + math.sin(a) * 8.5, tone)

    # The hole. One thin ring of gloom where the bank turns under, then occlusion.
    c.disc(15.5, 17.6, 9.0, 6.6, gloom)
    c.disc(15.5, 18.0, 7.8, 5.6, OCC)

    # Clods thrown clear of the lip.
    for (x, y) in [(2, 14), (29, 15), (7, 28), (24, 28), (16, 4)]:
        c.set(x, y, soil_lit)
    for (x, y) in [(3, 15), (28, 16), (8, 29), (23, 29)]:
        c.set(x, y, soil_shade)

    return c


SURGE_W = 24
SURGE_H = 14


def riptide_surge():
    """The Riptide projectile: a crescent of water travelling right, foam on its leading edge.

    Wide rather than square, because the spell pierces - it is a thing passing THROUGH a line of
    enemies, and a round bolt would read as a thing landing on one. The tail is a wake, not a body:
    it thins and breaks up toward the left so the sprite has a direction even when it is standing
    still on the first frame.
    """
    c = raster.Canvas(SURGE_W, SURGE_H, None)

    deep = bl.tone("azure", "deep")
    shade = bl.tone("azure", "shade")
    base = bl.tone("azure", "base")
    lit = bl.tone("azure", "lit")
    hi = bl.tone("azure", "hi")
    foam = bl.ELEMENTS["water"][0]

    # The crescent. Front edge bulges right, the concave back edge is what makes it a wave front
    # rather than a leaf.
    crest = [
        (10, 1), (16, 2), (20, 5), (21, 7), (20, 9), (16, 12), (10, 13),
        (14, 9), (15, 7), (14, 5), (10, 1),
    ]
    c.poly_shade(crest, [hi, lit, base, shade, deep], ang=-0.7854, bias=0.1)

    # The wake, thinning to the left. Three broken lines rather than a tapered mass: at this size a
    # taper turns to mush after the x2, and three separated rows still read as speed.
    c.hline(2, 12, 6, base)
    c.hline(4, 11, 4, shade)
    c.hline(4, 11, 9, shade)
    c.hline(0, 6, 7, shade)
    c.set(1, 5, deep)
    c.set(1, 8, deep)

    # Foam on the leading edge only, and a highlight along the upper face where the key light is.
    for (x, y) in [(15, 2), (18, 3), (20, 5), (21, 7), (20, 9), (18, 11), (15, 12)]:
        c.set(x, y, foam)
    for (x, y) in [(12, 2), (13, 3), (16, 3), (17, 4)]:
        c.set(x, y, hi)

    return c


def unknown():
    """The fallback every spell without art of its own falls through to.

    It replaces `ui-png-skills-icon-2.png`, a bought GUI-pack icon that was `DefaultSpellIcon` -
    which meant the one icon guaranteed to be on screen for any content gap was the one piece of
    art we are least allowed to publish.

    Drawn as a sealed rune rather than a question mark: a question mark says "error", and this
    appears when content is missing rather than when something has gone wrong.
    """
    c = raster.Canvas(CELL, CELL)
    core, hot, mid, edge = bl.ELEMENTS["arcane"]
    cx = cy = CELL / 2.0 - 0.5

    c.disc(cx, cy, 13.0, 13.0, bl.OCC)
    c.ring(cx, cy, 12.0, 12.0, edge, 2)
    c.ring(cx, cy, 9.0, 9.0, mid, 1)

    # Three bars and a dot: enough shape to be a mark, not enough to be a letter.
    for dy, tone in ((-4, mid), (0, hot), (4, mid)):
        c.hline(cx - 4, cx + 4, cy + dy, tone)
    c.disc(cx, cy, 1.6, 1.6, core)
    return c


def iron_palisade():
    """A row of five iron stakes driven at a slight lean, lit down their left edges.

    A fence, not a sword: the stakes are blunt-topped and of uneven height, and they stand on a
    shared ground line, so the icon reads as one barrier rather than five weapons.
    """
    c = raster.Canvas(CELL, CELL, None)
    core, hot, mid, edge = bl.ELEMENTS["metal"]
    for i, (x, top) in enumerate(((5, 9), (11, 6), (16, 8), (21, 5), (26, 9))):
        for y in range(top, 26):
            c.set(x, y, mid)
            c.set(x + 1, y, edge)
            c.set(x + 2, y, edge)
        c.set(x, top, core)
        c.set(x + 1, top, hot)
        c.set(x + 1, top - 1, hot)
        # The binding band across every stake, two thirds down.
        c.set(x, 19, edge)
        c.set(x + 1, 19, hot)
        c.set(x + 2, 19, mid)
    for x in range(3, 30):
        c.set(x, 26, edge)
        c.set(x, 27, edge if x % 3 else mid)
    return c


def contagion():
    """One sore in the middle, and the same mark already caught on the three around it.

    The spell is the spread, so the icon is four marks rather than one: a bright one that started
    it and three dimmer ones it reached, joined by thin threads.
    """
    c = raster.Canvas(CELL, CELL, None)
    core, hot, mid, edge = bl.ELEMENTS["poison"]
    cx = cy = CELL / 2.0 - 0.5
    reached = ((7.0, 9.0), (25.0, 11.0), (14.0, 26.0))
    for (x, y) in reached:
        steps = 24
        for i in range(steps + 1):
            t = i / float(steps)
            c.set(cx + (x - cx) * t, cy + (y - cy) * t, edge)
    for (x, y) in reached:
        c.disc(x, y, 3.4, 3.4, edge)
        c.disc(x, y, 2.4, 2.4, mid)
        c.set(x - 1, y - 1, hot)
    c.disc(cx, cy, 5.6, 5.6, edge)
    c.disc(cx, cy, 4.4, 4.4, mid)
    c.disc(cx, cy, 2.6, 2.6, hot)
    c.set(cx - 2, cy - 2, core)
    return c


def bramble_seed():
    """A seed split open with a thorned shoot climbing out of it.

    Growth is the whole spell, so the icon is the moment it starts: the husk at the bottom, the
    shoot rising, thorns along it getting larger toward the top.
    """
    c = raster.Canvas(CELL, CELL, None)
    core, hot, mid, edge = bl.ELEMENTS["grass"]
    husk_lit = bl.ELEMENTS["earth"][2]
    husk = bl.ELEMENTS["earth"][3]
    c.disc(15.5, 25.0, 6.0, 4.0, husk)
    c.disc(14.5, 24.0, 4.0, 2.6, husk_lit)
    c.disc(15.5, 23.0, 2.0, 1.4, OCC)
    for y in range(4, 24):
        x = 15.5 + math.sin(y * 0.45) * 2.0
        c.set(x, y, mid)
        c.set(x + 1, y, edge)
    for i, y in enumerate((20, 16, 12, 8)):
        x = 15.5 + math.sin(y * 0.45) * 2.0
        size = 2 + i
        side = 1 if i % 2 == 0 else -1
        for k in range(1, size + 1):
            c.set(x + side * (1 + k), y - k * 0.5, hot if k == size else mid)
    c.disc(15.5 + math.sin(4 * 0.45) * 2.0, 4.0, 1.6, 1.6, core)
    return c


def hollow_star():
    """A black well with three arms spiralling into it - the pull, not the hole.

    Gravewell is already a dark disc on this card, so a second dark disc would be the same icon. What
    Hollow Star does is drag things inward, and the arms are what say that: lit at the outer end where
    they start, darkening as they wind down into the core. The arms use the LIGHT end of the darkness
    ramp for the same reason Gravewell lights its soil - on a near-black plate the dark end vanishes.
    """
    c = raster.Canvas(CELL, CELL, None)
    core, hot, mid, edge = bl.ELEMENTS["darkness"]
    cx = cy = CELL / 2.0 - 0.5

    # Three arms, each a sampled spiral from radius 13 down to the core, toned by how far in it is.
    for arm in range(3):
        base = math.radians(200.0) + arm * (2.0 * math.pi / 3.0)
        steps = 60
        for i in range(steps + 1):
            t = i / float(steps)
            r = 13.0 - t * 8.5
            a = base + t * 3.4
            tone = core if t < 0.25 else (hot if t < 0.6 else mid)
            c.set(cx + math.cos(a) * r, cy + math.sin(a) * r, tone)
            if t < 0.6:
                # Thicker at the outside, where the eye picks the arm up.
                c.set(cx + math.cos(a) * (r - 0.8), cy + math.sin(a) * (r - 0.8), tone)

    # The well: a thin rim where the arms go under, then occlusion.
    c.disc(cx, cy, 5.2, 5.2, edge)
    c.disc(cx, cy, 4.2, 4.2, OCC)
    # One lit fleck on the upper-left of the rim - the key light, art-direction.md section 2.
    c.set(cx - 3, cy - 3, hot)
    return c


def folio():
    """Folio, John Dee's opener (.ai/character-fit.md section 6): an open book, and one page torn loose and flying.

    Three things, in the order the eye takes them: the loose page up and to the right - the one bright shape on the
    plate, the bone ramp lit from the upper left, two lines of ink and a violet-black edge on its shaded sides, as the
    game draws every page (scripts/Folio.cs); the open book it came from, low and wide, its pages pale over violet
    boards with a dark gutter; and between them a short trail of Arcane motes thinning into Darkness's - the spell's
    two elements, and the page's flight. Nothing on it is a mirror or a well: those were Hollow Star's, and the Spirit
    Mirror is the form's own icon.
    """
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade = bl.tone("skin", "hi"), bl.tone("skin", "lit"), bl.tone("skin", "base"), bl.tone("skin", "shade")
    vhi, vlit, vbase, vshade, vdeep = bl.MATERIALS["violet"]
    acore, ahot, amid, aedge = bl.ELEMENTS["arcane"]
    dcore, dhot, dmid, dedge = bl.ELEMENTS["darkness"]

    # The boards, under the pages: a row of violet showing all round, deepest at the spine.
    c.poly_shade([(2, 19), (15, 21), (16, 21), (29, 19), (29, 27), (16, 29), (15, 29), (2, 27)],
                 [vlit, vbase, vshade, vdeep], bias=0.1)
    # The two pages: the left facing the key and lit, the right a stop down.
    c.poly_shade([(3, 19), (15, 21), (15, 27), (3, 25)], [hi, lit, lit, base], bias=-0.05)
    c.poly_shade([(16, 21), (28, 19), (28, 25), (16, 27)], [lit, base, base, shade], bias=0.1)
    # The gutter, and the shadow the left page throws into it.
    c.vline(15, 21, 28, vdeep)
    c.vline(16, 21, 27, shade)
    # Lines of ink on both pages, following the pages' slope.
    for (x0, x1, y) in ((5, 12, 21), (5, 10, 23)):
        for x in range(x0, x1 + 1):
            c.set(x, y + (x - 3) // 6, dedge)
    for (x0, x1, y) in ((18, 26, 23), (18, 23, 25)):
        for x in range(x0, x1 + 1):
            c.set(x, y - (x - 16) // 6, dedge)

    # The loose page, tilted, up and to the right.
    leaf = [(17, 5), (25, 2), (28, 10), (20, 13)]
    c.poly_shade(leaf, [hi, hi, lit, base], bias=-0.1)
    # Its shaded sides: the lower edge and the right, in Darkness's edge.
    c.line(20, 13, 28, 10, dedge)
    c.line(25, 2, 28, 10, dedge)
    # Two lines of ink across it, along its tilt.
    c.line(19, 7, 24, 5, vdeep)
    c.line(20, 9, 24, 8, vdeep)
    # The key's fleck on its upper-left corner.
    c.set(18, 5, hi)

    # The flight: motes from the gutter up to the page, Arcane near the page and Darkness near the book.
    trail = [(16, 19, dmid), (16, 17, dhot), (17, 15, amid), (18, 14, ahot), (14, 16, dmid), (13, 13, amid)]
    for (x, y, col) in trail:
        c.set(x, y, col)
    c.set(16, 20, acore)
    # Occlusion under the book, one row, so it sits on the card.
    c.hline(3, 28, 30, OCC)
    return c


def corposant(sky=True):
    """Corposant, St Elmo's fire (scripts/Corposant.cs): a ball of pale storm-fire coming off the edge of the view.

    Three things, in the order the eye takes them: the ball low and left of the middle - an amber rim, a pale shell and a
    white-hot heart a step ahead of its middle, three tongues of storm-fire streaming back the way it came (it burns like
    a flame, not a lamp, which is what keeps it apart from Stormsweep's Ball Lightning); the crack up on the right where
    it turned, on a dashed upright that is the edge of the screen; and a dotted path into that crack from the upper left,
    the way it went before it turned. Two crackling spurs off the ball. Lightning is pale yellow and vanishes on the
    card's cream page (Starfall's and Stormsweep's lesson), so it is drawn on a night - the sea's, lichen's deep teal, a
    different sky from Stormsweep's violet. `sky=False` leaves the night off (tools/art/evolution_icons.py lays its own).
    """
    c = raster.Canvas(CELL, CELL, None)
    core, hot, mid, edge = bl.ELEMENTS["lightning"]
    if sky:
        c.disc(15.5, 15.5, 15.0, 15.0, bl.tone("lichen", "shade"))
        c.disc(15.5, 15.5, 14.0, 14.0, bl.tone("lichen", "deep"))

    # The edge of the view: a dashed upright on the right, the colour of the sea's lit swell.
    frame = bl.tone("lichen", "lit")
    for y in range(4, 27):
        if y % 3 != 2:
            c.set(27, y, frame)
    bx, by = 26.0, 9.0          # where it turned
    cx, cy = 13.0, 20.0         # the ball now

    # The way it went before it turned: dots in from the upper left, dimmest farthest back.
    for (x, y, t) in ((8, 4, edge), (12, 5, edge), (16, 6, mid), (20, 7, mid), (23, 8, hot)):
        c.set(x, y, t)

    # The tongues: three streaming back toward the crack, tapering from the shell to the ramp's edge.
    ux, uy = (bx - cx), (by - cy)
    n = math.hypot(ux, uy)
    ux, uy = ux / n, uy / n
    px, py = -uy, ux
    # Each a wedge, wide at the shell and licking to a point, with a waver along it so it reads as flame, not a ray.
    for (spread, length, wave) in ((0.0, 10.0, 0.9), (0.6, 6.5, -0.7), (-0.6, 6.5, 0.7)):
        dx, dy = ux + px * spread, uy + py * spread
        m = math.hypot(dx, dy)
        dx, dy = dx / m, dy / m
        qx, qy = -dy, dx
        steps = int(length)
        for s in range(steps):
            t = (s + 1.0) / steps
            sway = math.sin(t * math.pi * 1.5) * wave
            x = cx + dx * (3.0 + length * t) + qx * sway
            y = cy + dy * (3.0 + length * t) + qy * sway
            half = 1.6 * (1.0 - t)
            tone = hot if t < 0.35 else (mid if t < 0.7 else edge)
            k = -half
            while k <= half + 0.01:
                c.set(x + qx * k, y + qy * k, tone if abs(k) < 0.6 else (mid if t < 0.5 else edge))
                k += 0.8

    # The ball: rim, shell, body, and the heart a step ahead of its middle (down and left, the way it flies).
    c.disc(cx, cy, 5.2, 5.2, edge)
    c.disc(cx, cy, 4.2, 4.2, mid)
    c.disc(cx + 0.3, cy - 0.3, 3.1, 3.1, hot)
    c.disc(cx - 0.6, cy + 0.6, 1.7, 1.7, core)

    # Two crackling spurs off the shell, down and to the left.
    for (a, b, d) in (((8, 22), (5, 23), (4, 26)), ((12, 25), (13, 27), (11, 29))):
        c.line(a[0], a[1], b[0], b[1], hot)
        c.line(b[0], b[1], d[0], d[1], mid)

    # The crack where it turned: a white heart, four arms, the diagonals short.
    tx, ty = int(bx), int(by)
    for d in range(1, 3):
        t_ = core if d == 1 else hot
        for (dx, dy) in ((d, 0), (-d, 0), (0, d), (0, -d)):
            c.set(tx + dx, ty + dy, t_)
    for (dx, dy) in ((1, 1), (-1, -1), (1, -1), (-1, 1)):
        c.set(tx + dx, ty + dy, mid)
    c.set(tx, ty, core)
    return c


def ink_ring(opaque=False, seal=True, flood=True, sweep=330.0, r=10.5, width=(4.2, 1.6), cx=15.5, cy=16.0, seed=0,
             ink=0.62, pool=None, drips=False, second=False, hard=False):
    """Ink Ring (scripts/InkRing.cs): an enso - one stroke of the brush round a ring - closing on itself, the pale wash it
    floods with inside it, and the painter's seal. PAINTED IN INK, not drawn in the contract: Ma Liang's ink is the one
    sanctioned departure from Bonelight (.ai/art-direction.md section 3a), so this is ink_wash.paint's and ink_wash.audit
    is its check (write(), INK_ICONS) - the spell's own icon is ink on paper as the spell is ink on the ground.

    The stroke starts loaded and wide low on the left, runs clockwise round and thins to a dry tail that crosses its own
    start - the crossing is where the ring closed, and a drop of ink pooled there. Its middle is paler than its tide
    lines, so it reads on the card's cream page by its rims and on a dark plate by its wash and its middle. The seal is
    one small square of vermilion (the ember row, as on Ma Liang's crow's hat): the painter's chop, crisp and opaque.

    `opaque` lays the ink whole (ink_wash.opaque) for tools/art/evolution_icons.py, which sets it on a milestone's night,
    and the rest let those cards paint their own ring: `ink` the stroke's density, `pool` a flood that stands dark inside
    it (Inkwell), `drips` wet ink running off its foot (Wet Ink), `second` a second stroke round inside the first (Second
    Stroke), `hard` a stroke dried hard all round with no wet edge (Ink Prison).
    """
    import ink_wash as W
    layers = {}
    a0 = math.radians(200.0)
    span = math.radians(sweep)
    w0, w1 = width
    rings = [(r, a0, span, w0, w1, "ring")]
    if second:
        rings.append((r * 0.6, math.radians(20.0), math.radians(300.0), w0 * 0.85, w1, "ring2"))
    for y in range(CELL):
        for x in range(CELL):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            d = math.hypot(dx, dy)
            stack = []
            if pool is not None and d <= r - 1.4:
                stack.append(W.Layer(pool + 0.08 * (d / r), "flood", "flood", x, y, True))
            elif flood and d <= r - 2.2:
                stack.append(W.Layer(0.1 + 0.06 * (d / r), "flood", "flood", x, y, True))
            for (rr, start, arc, a, b, group) in rings:
                # Where round the stroke this pixel lies: clockwise from its start, so a dry tail comes back past it.
                ang = (math.atan2(dy, dx) - start) % (2 * math.pi)
                for turn in (ang, ang + 2 * math.pi):
                    if turn > arc:
                        continue
                    t = turn / arc
                    half = (a + (b - a) * t) / 2.0
                    if abs(d - rr) > half:
                        continue
                    # The tail runs dry: the brush skips where the paper's tooth caught it.
                    if not hard and t > 0.82 and W.hsh(x, y, 913 + seed) < (t - 0.82) * 2.6:
                        continue
                    stack.append(W.Layer(ink - 0.12 * t, group, group, turn * rr, d - rr, False))
                    break
            if drips and dy > 0 and abs(d - r) <= 2.5 + dy * 0.35 and dy > r * 0.55 and W.hsh(x, 0, 377 + seed) < 0.3:
                # Wet ink running down off the foot of the ring.
                stack.append(W.Layer(ink, "drip", "drip", x, y, False))
            if stack:
                layers[(x, y)] = stack
    # The drop that pooled where the ring closed, and the splatter the brush threw lifting off.
    ex, ey = cx + math.cos(a0) * r, cy + math.sin(a0) * r
    drops = [(ex + 0.5, ey + 0.5, 2.4)]
    splats = [(ex - 3.0, ey + 3.5, 2), (ex - 1.0, ey + 6.0, 1), (cx + r + 2.5, cy - 3.0, 1)]
    if drips:
        splats += [(cx - 4.0, cy + r + 4.0, 2), (cx + 3.0, cy + r + 4.5, 1)]
    blooms = [(cx + 3.0, cy - 2.5, 2.6)] if flood or pool is not None else []
    details = {}
    if seal:
        # The chop, low on the right: vermilion in the ember row, the paste darker where it sat thick.
        for (sx, sy) in ((26, 26), (27, 26), (26, 27), (27, 27)):
            details[(sx, sy)] = ("light", bl.MATERIALS["ember"][2] if (sx + sy) % 3 else bl.MATERIALS["ember"][3])
    c = W.paint(CELL, layers, details, blooms=blooms, drops=drops, splats=splats, seed=W.salt_of("ink-ring-icon") + seed,
                hard_groups=("ring", "ring2") if hard else ())
    return W.opaque(c) if opaque else c


def windfall_fruit(c, x, y, r=2.2, rot=False):
    """One iron apple at (x, y): steel lit from the upper left, a plane of steel's hi on that side (never a point - "Matte, not
    wet"), its dark side down and right, a stalk. `rot` spots it with Poison's green (Cider Rot)."""
    st = bl.MATERIALS["steel"]
    for yy in range(int(y - r) - 1, int(y + r) + 2):
        for xx in range(int(x - r) - 1, int(x + r) + 2):
            nx = (xx + 0.5 - x) / r
            ny = (yy + 0.5 - y) / r
            d = nx * nx + ny * ny
            if d > 1.0:
                continue
            v = -(nx * -0.62 + ny * -0.78)
            c.set(xx, yy, st[1] if v > 0.35 else (st[2] if v > -0.25 else (st[3] if d < 0.8 else st[4])))
    c.set(int(x - r * 0.45), int(y - r * 0.5), st[0])
    c.set(int(x - r * 0.45) + 1, int(y - r * 0.5), st[0])
    c.set(int(x), int(y - r) - 1, bl.tone("stone", "lit"))
    if rot:
        po = bl.ELEMENTS["poison"]
        for (dx, dy, col) in ((1, 1, po[3]), (0, 1, po[2]), (1, 0, po[2])):
            c.set(int(x) + dx, int(y) + dy, col)


def windfall(fruit=((11.5, 13.5), (19.5, 15.0), (15.5, 9.5)), falling=True, rot=False):
    """Windfall (scripts/Windfall.cs): the small iron-fruited tree, and one apple coming down off it.

    The tree as the game draws it (tools/art/windfall.py), cut down to the card: an iron trunk lit on its left, a verdant
    crown of leaf clusters lit from the upper left, and steel apples hanging in it - the brightest things on the card, as they
    are on the tree. Low on the right one apple falls, two dashes over it the way it came down, onto a scuff of grit where
    it will land. Grass above, Metal hanging in it and below: the two tags in one shape. No night: the crown is dark enough
    to hold on the card's cream page, and the apples hold on the crown.
    """
    c = raster.Canvas(CELL, CELL, None)
    st = bl.MATERIALS["steel"]
    iron = [st[1], st[2], st[3], st[4], OCC]
    # The ground and the trunk's foot.
    for x in range(8, 25):
        c.set(x, 29, OCC)
    T.trunk(c, 15, 28, 18, 4, 2.5, iron, 7, flare=1.6)
    rnd = T.Rng(7)
    clusters = T.fill_clusters(rnd, 15.5, 12.0, 9.5, 6.0, 20, 3.2, 5.0)
    T.crown(c, clusters, T.VE[1:5], 15.5, 12.0, 13.0, 8.5, 7, ragged=0.25)
    for (x, y) in fruit:
        windfall_fruit(c, x, y, rot=rot)
    if falling:
        windfall_fruit(c, 25.5, 23.0, rot=rot)
        c.set(25, 18, st[2])
        c.set(25, 19, st[3])
        c.set(26, 16, st[3])
        # The grit it lands on.
        for (x, col) in ((23, bl.tone("stone", "lit")), (28, bl.tone("stone", "base")), (24, OCC), (27, OCC)):
            c.set(x, 28, col)
    return c


def lancet_needle(c, ax, ay, bx, by, bead=True, bodkin=False):
    """One of Lancet's needles (scripts/Lancet.cs), from its eye at (ax, ay) to its point at (bx, by): the shaft in steel lit
    over a flank of steel's base on its lower side, the eye a slot of occlusion, the last of it white-hot - metal's core, as
    the game draws the point - and, `bead`, a drop of poison hanging under the shaft just behind the point. `bodkin` widens
    the point into a bodkin's square-shouldered head."""
    st = bl.MATERIALS["steel"]
    core, hot, mid, edge = bl.ELEMENTS["metal"]
    length = math.hypot(bx - ax, by - ay)
    ux, uy = (bx - ax) / length, (by - ay) / length
    # The side away from the key light (upper left): the flank of shade.
    sx, sy = -uy, ux
    if sx + sy < 0:
        sx, sy = -sx, -sy
    steps = int(length * 2) + 1
    for i in range(steps + 1):
        t = i / float(steps)
        x, y = ax + (bx - ax) * t, ay + (by - ay) * t
        k = t * length
        if 1.5 < k < length - 1.5:
            c.set(int(round(x + sx)), int(round(y + sy)), st[2])
    for i in range(steps + 1):
        t = i / float(steps)
        x, y = ax + (bx - ax) * t, ay + (by - ay) * t
        k = t * length
        tone = core if k > length - 2.5 else (hot if k > length * 0.55 else st[1])
        c.set(int(round(x)), int(round(y)), tone)
    # The eye: a slot of occlusion in the shaft's tail.
    c.set(int(round(ax + ux * 1.0)), int(round(ay + uy * 1.0)), OCC)
    if bodkin:
        # The bodkin's head: shoulders of steel either side of the point, lit on the upper one.
        for (side, tone) in ((1.0, st[0]), (-1.0, st[2])):
            for back in (1.5, 2.5):
                c.set(int(round(bx - ux * back + sx * side)), int(round(by - uy * back + sy * side)), tone)
        c.set(int(round(bx - ux * 3.5 + sx)), int(round(by - uy * 3.5 + sy)), edge)
    if bead:
        po = bl.ELEMENTS["poison"]
        hx, hy = int(round(bx - ux * 3.0 + sx * 1.6)), int(round(by - uy * 3.0 + sy * 1.6))
        c.set(hx, hy, po[1])
        c.set(hx + 1, hy, po[2])
        c.set(hx, hy + 1, po[2])
        c.set(hx + 1, hy + 1, po[3])


def lancet_skull(c, x, y, s=1.0, cracked=True, dead=False, withered=False):
    """One of the swarm, the most hurt of it: a bone skull lit upper left, two dark eyes, and `cracked` a split across its
    crown - the wound the needle is drawn to. `dead` crosses its eyes (an outright kill); `withered` takes a bite out of its
    lower right, gone to the dark (Bodkin's Wither, health taken for good)."""
    b = bl.MATERIALS["skin"]
    c.disc(x, y, 3.6 * s, 3.9 * s, b[3])
    c.disc(x - 0.5, y - 0.5, 2.8 * s, 3.0 * s, b[2])
    c.set(int(x - 1.5 * s), int(y - 1.8 * s), b[1])
    c.set(int(x - 0.5 * s), int(y - 2.2 * s), b[1])
    if withered:
        c.disc(x + 2.4 * s, y + 2.2 * s, 2.2 * s, 2.0 * s, b[4])
        c.set(int(x + 1.0 * s), int(y + 2.6 * s), bl.ELEMENTS["metal"][3])
        c.set(int(x + 2.6 * s), int(y + 0.6 * s), bl.ELEMENTS["metal"][3])
    ex, ey = int(round(x - 1.4 * s)), int(round(y + 0.4 * s))
    fx = int(round(x + 1.2 * s))
    if dead:
        # Eyes struck through, a slash each the other way: gone.
        c.set(ex, ey, OCC)
        c.set(ex + 1, ey + 1, OCC)
        c.set(fx, ey + 1, OCC)
        c.set(fx + 1, ey, OCC)
    else:
        c.set(ex, ey, OCC)
        c.set(ex + 1, ey, OCC)
        c.set(fx, ey, OCC)
        c.set(fx + 1, ey, OCC)
    if cracked:
        # A zigzag split down from the crown, dark against the bone.
        cx_, cy_ = int(round(x + 0.6 * s)), int(round(y - 3.6 * s))
        for (dx, dy) in ((0, 0), (0, 1), (1, 2), (0, 3)):
            c.set(cx_ + dx, cy_ + dy, OCC)


def lancet(sky=True, bead=True, drop=True):
    """Lancet (scripts/Lancet.cs): a thin steel needle going into the most wounded of the swarm.

    The needle crosses the card from its eye up on the left to its point low on the right, where it goes into a skull with
    a crack across its crown - the most hurt, the one it chose. Its shaft is steel lit over a flank of steel's base, its last
    pixels white-hot, and a bead of poison hangs under it just behind the point, one drop falling from it. Steel is pale and
    vanishes on the card's cream page (Starfall's lesson), so it is drawn on a night: the deep of a wound, red's shade and
    deep, a night no other spell's card uses. `sky=False` leaves the night off (tools/art/evolution_icons.py lays its own).
    """
    c = raster.Canvas(CELL, CELL, None)
    if sky:
        c.disc(15.5, 15.5, 15.0, 15.0, bl.tone("red", "shade"))
        c.disc(15.5, 15.5, 14.0, 14.0, bl.tone("red", "deep"))
    lancet_skull(c, 21.5, 21.0, 1.3)
    lancet_needle(c, 6.0, 6.0, 17.0, 17.0, bead=bead)
    lancet_glint(c, 17, 17)
    if drop:
        po = bl.ELEMENTS["poison"]
        c.set(13, 21, po[1])
        c.set(13, 22, po[2])
        c.set(14, 22, po[3])
    return c


def lancet_glint(c, x, y, arm=2):
    """Where a needle went in: a white-hot heart and four short arms of metal's hot, on the diagonals."""
    core, hot, mid, edge = bl.ELEMENTS["metal"]
    c.set(x, y, core)
    for d in range(1, arm + 1):
        tone = hot if d == 1 else mid
        for (dx, dy) in ((d, -d), (-d, d), (d, d), (-d, -d)):
            if (dx, dy) != (-d, -d):
                c.set(x + dx, y + dy, tone)


def the_taking():
    """The dark wizard's own (.ai/dark-wizard-playable.md): a hand of his dark light closing on a warm mote.

    Three things the spell is, in the order the eye takes them: the mote - the light ramp, the one warm
    thing on the plate, what he took - then the hand round it, long fingers of the darkness ramp's lit end
    reaching in from the lower left (on a near-black plate the dark end vanishes, as Hollow Star found),
    and behind both two thin lanes of his light crossing where the mote sits: what catching it throws.
    """
    c = raster.Canvas(CELL, CELL, None)
    dcore, dhot, dmid, dedge = bl.ELEMENTS["darkness"]
    lcore, lhot, lmid, ledge = bl.ELEMENTS["light"]
    mx, my = 20.0, 10.5

    # The lanes, first and faint: a cross through the mote, one along the arm's line and one across it.
    for i in range(-13, 14):
        for (dx, dy) in ((0.71, -0.71), (0.71, 0.71)):
            x, y = mx + dx * i, my + dy * i
            if abs(i) > 5:
                c.set(x, y, dedge if abs(i) > 9 else dmid)

    # The arm: a sleeve of his violet coming in from the lower left corner, lit along its upper edge.
    for i in range(0, 10):
        x, y = 3.0 + i * 0.9, 29.5 - i * 0.95
        c.disc(x, y, 2.8, 2.8, bl.tone("violet", "shade"))
    for i in range(0, 10):
        x, y = 3.0 + i * 0.9, 29.5 - i * 0.95
        c.set(x - 2, y - 1, bl.tone("violet", "lit"))
        c.set(x + 1, y + 2, bl.tone("violet", "deep"))
    # the wrist, pale bone, out of the cuff
    for i in range(0, 3):
        c.set(11.5 + i * 0.8, 20.5 - i * 0.9, bl.tone("skin", "base"))
        c.set(12.5 + i * 0.8, 20.5 - i * 0.9, bl.tone("skin", "shade"))

    # The palm under the mote, dark, with his ramp at its rim.
    c.disc(15.5, 15.5, 3.4, 3.4, dmid)
    c.disc(15.5, 15.5, 2.4, 2.4, OCC)
    c.set(13, 14, dhot)
    c.set(14, 13, dhot)

    # The mote he is taking: amber halo, light ramp heart, core to the upper left.
    c.disc(mx, my, 4.4, 4.4, bl.tone("amber", "deep"))
    c.disc(mx, my, 3.4, 3.4, ledge)
    c.disc(mx, my, 2.4, 2.4, lmid)
    c.disc(mx - 0.5, my - 0.5, 1.4, 1.4, lhot)
    c.set(mx - 1, my - 1, lcore)
    c.set(mx, my - 1, lcore)

    # Four long fingers and a thumb curling round it from the palm, tipped with his light's core.
    fingers = [((14, 12), (14, 7), (17, 4)), ((16, 12), (19, 5), (22, 4)),
               ((17, 14), (24, 9), (25, 12)), ((18, 16), (24, 14), (25, 15))]
    for (a, b, tip) in fingers:
        c.line(a[0], a[1], b[0], b[1], dhot)
        c.line(b[0], b[1], tip[0], tip[1], dmid)
        c.set(tip[0], tip[1], dcore)
    c.line(17, 18, 21, 19, dhot)
    c.set(21, 19, dcore)
    return c


# Merlin's quartz (orb_of_ages), pixel by pixel: the staff's head his own sprite carries
# (merlin_concepts.crystal), grown to the icon's cell. A tall point and two small ones leaning off it, the
# stone's facet edges in steel - its hi and lit on the key side, its shade on the far one - and the light
# inside it, white-hot up the key-side face; bound in gold wire at the seat, on the gold shaft.
# T t r R: steel hi / lit / base / shade. 0 1 2 3: light core / hot / mid / edge. H G g u: gold hi / lit /
# base / shade. The map's top-left pixel is QUARTZ_AT.
QUARTZ_AT = (9, 3)
QUARTZ = [
    "......Tr......",
    ".....T0rR.....",
    "....t001rR....",
    "....t011rR....",
    "....t012rR....",
    "..T.t112rR....",
    ".tTRt122rR.T..",
    ".t0Rt122rRtrR.",
    ".t1Rt223rRt1R.",
    "..t2Rt23rRr2R.",
    "..rRRr33rRRrR.",
    "...RRrrrRR.R..",
    "...uGHGggggu..",
    "....uGggggu...",
    "......Gg......",
]
QUARTZ_TONES = {
    "T": ("steel", 0), "t": ("steel", 1), "r": ("steel", 2), "R": ("steel", 3),
    "H": ("gold", 0), "G": ("gold", 1), "g": ("gold", 2), "u": ("gold", 3),
    "0": ("light", 0), "1": ("light", 1), "2": ("light", 2), "3": ("light", 3),
}
# The glow: a few points of the light's edge stop clear of the stone - the fade, never a disc (the
# sprite's own rule).
QUARTZ_GLOW = [(10, 4), (19, 6), (8, 9), (21, 9), (13, 2)]


def orb_of_ages(sky=True):
    """Merlin's own (tools/art/player_merlin.py): his staff's head - raw quartz with the light in it -
    inside the ward its light ricochets round.

    In the order the eye takes them: the quartz - a tall point and two small ones leaning off it, set in
    gold wire on the gold shaft, its facet edges steel and the light ramp inside it, white-hot up the
    key-side face: the light the spell sends round the ring, still in the stone (his sprite's staff head,
    merlin_concepts.crystal, grown to this cell; until 2026-11-03 it was the title figure's gold claw round
    an orb, and that claw is the player's now, .ai/merlin.md section 5); then the thin ring of light round
    it, the warded circle drawn on the ground; and inside the ring the path the orb traces bouncing off it,
    the seven-pointed star {7/3}, dimmer than the ring so the ring stays the boundary and the star reads as
    the line inside it, a point of light where it strikes the ring at each of its seven corners. The
    star's dense middle is behind the quartz, so what shows are its seven spikes.

    `sky` lays the night inside the ward and a rim of arcane round it - the spell's second element, and
    what keeps a ring of light from vanishing on the new spell's pale page. evolution_icons.py draws it
    without, because every milestone frame brings its own.
    """
    c = raster.Canvas(CELL, CELL, None)
    lcore, lhot, lmid, ledge = bl.ELEMENTS["light"]
    gold = bl.MATERIALS["gold"]
    cx = cy = 15.5

    if sky:
        c.disc(cx, cy, 15.0, 15.0, bl.ELEMENTS["arcane"][3])
        c.disc(cx, cy, 14.2, 14.2, bl.tone("arcane", "deep"))

    # The ward: one thin ring of the light's edge stop.
    c.ring(cx, cy, 13.4, 13.4, ledge)

    # The path: {7/3}, a corner at the top, each corner joined to the third one on.
    r = 12.4
    pts = [(cx + math.cos(math.radians(-90.0 + k * 360.0 / 7.0)) * r,
            cy + math.sin(math.radians(-90.0 + k * 360.0 / 7.0)) * r) for k in range(7)]
    for k in range(7):
        (x0, y0), (x1, y1) = pts[k], pts[(k + 3) % 7]
        c.line(int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1)), bl.tone("amber", "shade"))
    # Where it strikes the ring: a point of the light's hot stop at each corner.
    for (x, y) in pts:
        c.set(int(round(x)), int(round(y)), lhot)

    # The staff's head and the shaft under it. An occlusion seam round all of it first, so the stone and
    # the gold stand in front of the star's lines rather than running into them - the light's stops and
    # the star's amber are near enough to merge without one.
    ax, ay = QUARTZ_AT
    head = {}
    for j, row in enumerate(QUARTZ):
        for i, ch in enumerate(row):
            if ch != ".":
                kind, k = QUARTZ_TONES[ch]
                head[(ax + i, ay + j)] = bl.ELEMENTS["light"][k] if kind == "light" else bl.MATERIALS[kind][k]
    for y in range(ay + len(QUARTZ), 28):
        head[(15, y)] = gold[1]
        head[(16, y)] = gold[2]
    head[(16, 27)] = gold[3]
    for (x, y) in head:
        for (dx, dy) in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            if (x + dx, y + dy) not in head:
                c.set(x + dx, y + dy, OCC)
    for (x, y), tone in head.items():
        c.set(x, y, tone)
    for (x, y) in QUARTZ_GLOW:
        c.set(x, y, ledge)
    return c


def _falling_star(c, x, y, trail, light, glint=1):
    """One falling star: a white head with a glint, and a thin trail back up to the upper left,
    cooling down the Light ramp as it goes. The slant is the one the stars fall at in the game."""
    core, hot, mid, edge = light
    for i in range(1, trail + 1):
        f = i / float(trail)
        tone = hot if f < 0.3 else (mid if f < 0.65 else edge)
        c.set(x - i * 0.75, y - i, tone)
        if f < 0.6:
            # The trail is two pixels thick for its first half, where the eye picks it up.
            c.set(x - i * 0.75 + 1, y - i, mid if f < 0.3 else edge)
    for k in range(1, glint + 1):
        for (dx, dy) in ((k, 0), (-k, 0), (0, k), (0, -k)):
            c.set(x + dx, y + dy, hot if k < glint or glint == 1 else mid)
    c.set(x, y, core)
    c.set(x + 1, y, core)
    c.set(x, y + 1, core)
    c.set(x + 1, y + 1, hot)


def starfall(sky=True):
    """Stars falling at a slant, and one that has landed and is twinkling.

    Meteor Swarm's icon is burning blobs with stubby tails; these are points of light with long thin
    ones, and the landed star says what the spell does - it is a twinkle, not a crater. All Light
    ramp, no material: it is an effect (spell_icons_core.py's rule).

    `sky=False` draws the stars alone: tools/art/evolution_icons.py lays its own backdrop under them.
    """
    c = raster.Canvas(CELL, CELL, None)
    light = bl.ELEMENTS["light"]
    core, hot, mid, edge = light

    # A patch of night behind them. Drawn only in Light's ramp the icon was pale cream on the level-up
    # card's cream page and vanished there; stars need a night to be seen. The darkest cold material
    # row (Arcane cyan's deep and shade) - a sky, not an element - with a lit rim on the key side.
    if sky:
        sky_deep = bl.tone("arcane", "deep")
        sky_rim = bl.tone("arcane", "shade")
        c.disc(15.5, 15.5, 15.0, 15.0, sky_rim)
        c.disc(15.5, 15.5, 14.0, 14.0, sky_deep)
        for (x, y) in ((6, 9), (9, 6), (12, 4), (4, 12)):
            c.set(x, y, sky_rim)

    # The landed star, lower right: four long arms, four short diagonal ones, a white heart.
    cx, cy = 21, 22
    for d in range(1, 9):
        tone = core if d <= 2 else (hot if d <= 5 else mid)
        for (dx, dy) in ((d, 0), (-d, 0), (0, d), (0, -d)):
            c.set(cx + dx, cy + dy, tone)
    for d in range(1, 4):
        tone = hot if d == 1 else mid
        for (dx, dy) in ((d, d), (-d, d), (d, -d), (-d, -d)):
            c.set(cx + dx, cy + dy, tone)
    c.disc(cx, cy, 1.6, 1.6, core)
    # Light thrown off it, to the edge of where it hit.
    for (x, y) in ((12, 26), (30, 18), (27, 29), (15, 16)):
        c.set(x, y, edge)

    # Three still falling, each further up the sky and smaller.
    _falling_star(c, 10, 13, 10, light, glint=2)
    _falling_star(c, 26, 8, 7, light, glint=1)
    _falling_star(c, 5, 25, 5, light, glint=1)
    return c


def sagittarius(sky=True):
    """Sagittarius - Hunter's Draw + Starfall, the first combination (.ai/combo-spells.md section 3).

    The archer's figure in lit stars round the rim of Starfall's night, joined by Constellation's faint
    lines, and from every star a gold arrow with a white head flying INWARD, all of them converging on one
    quarry at the heart. The verb is converge, so this is the one icon in the set whose every line runs
    toward its middle: Starfall's falls across the card, Hunter's Draw's arrow flies out of it. The
    combination reads as its two parents - the same night and stars, the same arrow - put to one use.
    """
    c = raster.Canvas(CELL, CELL, None)
    light = bl.ELEMENTS["light"]
    core, hot, mid, edge = light
    gold = bl.MATERIALS["gold"]

    if sky:
        sky_deep = bl.tone("arcane", "deep")
        sky_rim = bl.tone("arcane", "shade")
        c.disc(15.5, 15.5, 15.0, 15.0, sky_rim)
        c.disc(15.5, 15.5, 14.0, 14.0, sky_deep)

    cx, cy = 16, 16
    # The figure: six lit stars round the rim, not evenly - an archer is not a clock face.
    stars = [(6, 8), (13, 4), (24, 6), (28, 17), (21, 27), (7, 23)]

    # Constellation's lines between neighbours, dotted in the ramp's edge: there, and quieter than the arrows.
    for i in range(len(stars)):
        (x0, y0), (x1, y1) = stars[i], stars[(i + 1) % len(stars)]
        steps = int(max(abs(x1 - x0), abs(y1 - y0)))
        for k in range(2, steps - 1):
            if k % 3:
                continue
            t = k / float(steps)
            c.set(int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t)), edge)

    # From every star an arrow to the quarry: a gold shaft, brightening toward the head, and a white head with
    # two barbs that stops short of the heart, so the six points ring it rather than pile on it.
    for (sx, sy) in stars:
        dx, dy = cx - sx, cy - sy
        length = math.sqrt(dx * dx + dy * dy)
        ux, uy = dx / length, dy / length
        stop = length - 4.0
        n = int(stop)
        for k in range(2, n):
            px, py = sx + ux * k, sy + uy * k
            f = k / float(n)
            c.set(int(round(px)), int(round(py)), gold[0] if f > 0.7 else gold[1] if f > 0.3 else gold[2])
        hx, hy = sx + ux * stop, sy + uy * stop
        for side in (-1, 1):
            c.set(int(round(hx - ux * 2 - uy * side * 1.4)), int(round(hy - uy * 2 + ux * side * 1.4)), hot)
        c.set(int(round(hx - ux)), int(round(hy - uy)), core)
        c.set(int(round(hx)), int(round(hy)), core)

    # The quarry, marked: a hot point the six white heads close on.
    for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1)):
        c.set(cx - 1 + dx, cy - 1 + dy, hot)
    c.set(cx - 1, cy - 1, core)

    # The stars last, over their lines: a white heart and four short arms, the brightest thing on the card.
    for (sx, sy) in stars:
        for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            c.set(sx + dx, sy + dy, hot)
        c.set(sx, sy, core)
    # Two of them twinkle: the long arms of a landing star.
    for (sx, sy) in (stars[1], stars[3]):
        for (dx, dy) in ((2, 0), (-2, 0), (0, 2), (0, -2)):
            c.set(sx + dx, sy + dy, mid)
    return c


def sovereign_rite(sky=True):
    """The Sovereign Rite (.ai/sovereign-spell.md): every spell's power poured into one.

    Five sparks round the rim of a dark plate, each in a different element's ramp, and from each a thin
    trail drawn INWARD to one white-hot point at the heart - the anointed spell - with a small gilt crown
    standing over it. Sagittarius's lines converge on a quarry; these converge on a spell, so the trails are
    the elements' own colours and the heart is light, not a target. The crown is the gold every crown in the
    game is drawn in (the conjunction plate's), and the only material on the card: the rest is effect.
    """
    c = raster.Canvas(CELL, CELL, None)
    gold = bl.MATERIALS["gold"]

    if sky:
        rim = bl.tone("violet", "shade") if "violet" in bl.MATERIALS else bl.tone("arcane", "shade")
        deep = bl.tone("arcane", "deep")
        c.disc(15.5, 15.5, 15.0, 15.0, rim)
        c.disc(15.5, 15.5, 14.0, 14.0, deep)

    cx, cy = 16, 18
    # Five of the twelve, round the lower wheel - the top is the crown's - so no two neighbours share a hue.
    sparks = [("fire", -15), ("ice", 40), ("lightning", 90), ("grass", 140), ("arcane", 195)]
    for name, deg in sparks:
        core, hot, mid, edge = bl.ELEMENTS[name]
        a = math.radians(deg)
        sx, sy = cx + math.cos(a) * 12.5, cy + math.sin(a) * 11.0
        # The trail: the element cooling from its hot stop to its edge as it nears the heart, dotted past
        # half way so the six read as six and do not fill the middle.
        n = 9
        for k in range(1, n):
            t = k / float(n)
            px, py = sx + (cx - sx) * t, sy + (cy - sy) * t
            if t > 0.55 and k % 2:
                continue
            c.set(int(round(px)), int(round(py)), hot if t < 0.3 else (mid if t < 0.6 else edge))
        # The spark itself: a white heart and its hot stop round it.
        ix, iy = int(round(sx)), int(round(sy))
        for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            c.set(ix + dx, iy + dy, hot)
        c.set(ix, iy, core)

    # The heart: the anointed spell, white-hot, with the Light ramp round it.
    lcore, lhot, lmid, ledge = bl.ELEMENTS["light"]
    c.disc(cx, cy, 3.4, 3.4, lmid)
    c.disc(cx, cy, 2.4, 2.4, lhot)
    c.disc(cx, cy, 1.3, 1.3, lcore)

    # The crown over it: a gilt band and three points, the middle one tallest, tipped in the light hot,
    # with the occlusion line under the band so it stands off the plate.
    bx0, bx1, by = cx - 5, cx + 5, cy - 6
    for x in range(bx0, bx1 + 1):
        c.set(x, by, gold[1])
        c.set(x, by + 1, gold[2])
        c.set(x, by + 2, OCC)
    for (px, h) in ((bx0, 3), (cx, 5), (bx1, 3)):
        for k in range(1, h + 1):
            c.set(px, by - k, gold[1] if k < h else gold[0])
            if k < h - 1:
                c.set(px + (1 if px < cx else -1 if px > cx else 1), by - k, gold[2])
        c.set(px, by - h - 1, lcore)
    for x in (bx0 + 2, cx - 2, cx + 2, bx1 - 2):
        c.set(x, by - 1, gold[2])
    c.set(cx - 1, by, gold[0])
    c.set(cx + 1, by, gold[0])
    return c


def moon(c, cx, cy, r):
    """A full moon, in bone (the contract's moon row), lit from the upper left: the pale disc, a brighter
    crown on the key's side, the shaded limb opposite, and three craters. Hung behind a familiar's head on
    its card (2026-10-30, "familiar icons are very muted and hard to read"), so the hat's point, the crow's
    beak and the cat's ears are dark shapes on a pale ground rather than dark shapes on a dark one."""
    bone = bl.MATERIALS["skin"]
    c.disc(cx, cy, r, r, bone[2])
    c.disc(cx - 0.6, cy - 0.6, r - 0.9, r - 0.9, bone[1])
    c.disc(cx - r * 0.35, cy - r * 0.35, r * 0.45, r * 0.45, bone[0])
    for (dx, dy, k) in ((0.35, 0.2, 1.1), (-0.15, 0.5, 0.8), (0.4, -0.35, 0.7)):
        c.disc(cx + dx * r, cy + dy * r, k, k, bone[2])


# THE MOONLIT CARDS (2026-11-01). Eric, playing 0.10.0: the Familiar's icons were "a bit too difficult to
# read". Four treatments were drawn side by side (A close-up, B moonlit, C at heel, D lantern ground) and he
# picked B: "Familiar moonlit looks good". The head is the same portrait, drawn a little smaller (0.84, the
# evolution cards' size) so the whole silhouette sits INSIDE a full moon that fills the card, with a line of
# occlusion round it - every edge of hat, ear, beak and muzzle lands on pale bone instead of on the night,
# which is what the dark heads (the crow, the black cats) and the indigo hat were lost against. The cards'
# evolutions take the same moon and outline (evolution_icons.familiar_frame).

def full_moon(c, cx, cy, r, clip=None):
    """moon() big enough to back a whole head, with its shaded limb dithered in on the lower right so a moon
    that size still turns. `clip(x, y)` keeps it inside a disc."""
    tmp = raster.Canvas(CELL, CELL, None)
    moon(tmp, cx, cy, r)
    shade = bl.MATERIALS["skin"][3]
    bayer = raster.BAYER8
    for y in range(CELL):
        for x in range(CELL):
            if not tmp.get(x, y)[3]:
                continue
            d = ((x - cx) * 0.7071 + (y - cy) * 0.7071) / r
            if d > 0.55 and bayer[y % 8][x % 8] / 64.0 < (d - 0.55) * 2.2:
                tmp.set(x, y, shade)
    for y in range(CELL):
        for x in range(CELL):
            px = tmp.get(x, y)
            if px[3] and (clip is None or clip(x, y)):
                c.set(x, y, px)


def in_disc(r, cx=15.5, cy=15.5):
    return lambda x, y: (x - cx) ** 2 + (y - cy) ** 2 <= r * r


def outlined(cv, colour=OCC):
    """A copy of `cv` with a one-pixel ring of `colour` round everything opaque (4-neighbour), on the
    transparent side: what holds a dark head's silhouette against a pale moon."""
    out = raster.Canvas(cv.w, cv.h, None)
    for y in range(cv.h):
        for x in range(cv.w):
            p = cv.get(x, y)
            if p[3]:
                out.set(x, y, p)
                continue
            for (ox, oy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if cv.get(x + ox, y + oy)[3]:
                    out.set(x, y, colour)
                    break
    return out


def shrunk(cv, s, at):
    """A finished picture drawn at `s` (under 1) round its own middle, put at `at`, by vote: every block of
    source pixels becomes its commonest opaque colour (the brighter on a tie), and a block more than half
    empty stays empty - evolution_icons.place_image's shrink, which this cannot import without a cycle."""
    xs, ys = [], []
    for y in range(cv.h):
        for x in range(cv.w):
            if cv.get(x, y)[3]:
                xs.append(x)
                ys.append(y)
    out = raster.Canvas(CELL, CELL, None)
    if not xs:
        return out
    ax, ay = (min(xs) + max(xs)) / 2.0, (min(ys) + max(ys)) / 2.0
    votes, total = {}, {}
    for y in range(cv.h):
        for x in range(cv.w):
            X, Y = at[0] + (x - ax) * s, at[1] + (y - ay) * s
            k = (int(math.floor(X + 0.5)), int(math.floor(Y + 0.5)))
            total[k] = total.get(k, 0) + 1
            p = cv.get(x, y)
            if p[3]:
                v = votes.setdefault(k, {})
                v[p] = v.get(p, 0) + 1
    for k, v in votes.items():
        if sum(v.values()) * 2 < total[k]:
            continue
        best = max(v.items(), key=lambda kv: (kv[1], raster._lum(kv[0])))[0]
        out.set(k[0], k[1], best)
    return out


def _familiar_card(head, sky=True, motes=((26, 7, 1), (28, 12, 2), (24, 4, 2), (5, 9, 2), (7, 5, 1), (27, 24, 2))):
    """A familiar's head as a cameo on a full moon (THE MOONLIT CARDS, above): Starfall's patch of night -
    the darkest cold material row with a lit rim, because a fawn head on the level-up card's cream page
    would vanish into it - a full moon filling it, the head outlined in occlusion and drawn at 0.84 inside
    the moon, and the Arcane motes round it. `sky=False` leaves the night and the moon off."""
    c = raster.Canvas(CELL, CELL, None)
    if sky:
        c.disc(15.5, 15.5, 15.0, 15.0, bl.tone("arcane", "shade"))
        c.disc(15.5, 15.5, 14.0, 14.0, bl.tone("arcane", "deep"))
        full_moon(c, 15.5, 15.0, 12.6, clip=in_disc(13.5))
    inside = in_disc(14.5)
    small = shrunk(outlined(head), 0.84, (15.5, 16.0))
    for y in range(CELL):
        for x in range(CELL):
            p = small.get(x, y)
            if p[3] and inside(x, y):
                c.set(x, y, p)
    arc = bl.ELEMENTS["arcane"]
    for (x, y, t) in motes:
        c.set(x, y, arc[t])
    if motes:
        c.set(motes[0][0], motes[0][1] - 1, arc[0])
    return c


def familiar_icon(sky=True):
    """The Familiar, before its form is chosen: the three it can be taken as, together - the cat, the
    hound and the crow, each in the wizard's hat - on the moon (familiar.trio). Each is drawn by
    tools/art/familiar.py's own renderer, so the card shows the creatures the player will see at his
    heel. The eyes are the calm ones (familiar.calm_eye; Eric, 2026-10-28: "it looks a little creepy,
    the eye is too prominent")."""
    import familiar
    return _familiar_card(familiar.trio(), sky, motes=((27, 3, 1), (29, 8, 2), (3, 4, 2), (2, 9, 1)))


def familiar_dog_icon(sky=True):
    """The Familiar taken as a dog (SpellVariant.Icon): the hound's head in the wizard's hat, its collar
    star lit - a good dog: the calm eye and a closed, easy mouth (familiar.portrait)."""
    import familiar
    return _familiar_card(familiar.portrait(), sky, motes=((26, 4, 1), (28, 26, 2), (4, 26, 2), (29, 11, 2)))


def familiar_crow_icon(sky=True):
    """The Familiar taken as a crow: its head and shoulder in the hat, the beak out to the right, a wing's
    gold edge behind it (familiar.crow_portrait)."""
    import familiar
    return _familiar_card(familiar.crow_portrait(), sky, motes=((27, 4, 1), (4, 26, 2), (28, 26, 2), (3, 12, 2)))


def familiar_cat_icon(sky=True):
    """The Familiar taken as a cat: its round face three-quarter on, ears up through the hat's brim, a
    content eye (familiar.cat_portrait)."""
    import familiar
    return _familiar_card(familiar.cat_portrait(), sky, motes=((28, 4, 1), (29, 11, 2), (3, 26, 2), (28, 26, 2)))


def _over(c, k):
    """Lays canvas k over c wherever k has a pixel."""
    for y in range(k.h):
        for x in range(k.w):
            p = k.get(x, y)
            if p[3] > 0:
                c.set(x, y, p)


def stormsweep(sky=True):
    """A whip of storm-light cracking beside a glowing sigil, over a patch of storm sky (2026-10-31).

    It was a jagged arc swept a quarter-turn round the sigil, two afterglow arcs inside it: a sweep, and
    "the electric whip spell should look more like a whip crack." Now it is the stroke the game draws
    (scripts/Stormsweep.cs, WhipStroke): out of the caster's hand at the lower left the lash lies low,
    steps up through a LOOP to the far part still lying high, and the far end snaps down and CRACKS - a
    white-hot star with sparks, speed lines beside the snap - while streaks of the band it has just swept
    hang under it. Lightning is pale yellow, and on the level-up card's cream page a yellow stroke
    vanishes (the lesson Starfall's icon learned), so it is drawn on a disc of night in violet's deep and
    shade. `sky=False` leaves the night off (tools/art/evolution_icons.py lays its own).
    """
    import spell_icons_core as sc
    c = raster.Canvas(CELL, CELL, None)
    core, hot, mid, edge = bl.ELEMENTS["lightning"]
    if sky:
        sky_deep = bl.tone("violet", "deep")
        sky_rim = bl.tone("violet", "shade")
        c.disc(15.5, 15.5, 15.0, 15.0, sky_rim)
        c.disc(15.5, 15.5, 14.0, 14.0, sky_deep)

    # The pivot: the caster's sigil, low on the left (the Thunderclap card turns copies about it).
    px, py = 7.0, 24.0
    tip = (26.0, 20.5)

    # The band just swept, under the high far part of the lash: streaks of it, broken.
    for x in range(18, 26, 2):
        for y in range(12, 20):
            if (x * 3 + y) % 5 < 2:
                c.set(x, y, edge if y > 16 else mid)

    # The snap: a speed line beside the far end's drop, the way it went.
    for y in range(9, 18):
        if y % 3 != 2:
            c.set(28, y, hot if y < 13 else mid)

    # The lash: a glow of the edge tone thrown down and right of it, then a hot body with a white-hot
    # core, two pixels at the hand and one for the rest.
    path = sc._lash_path((9.5, 22.0), (12.5, 22.5), (14.0, 19.5), (15.5, 7.5), (27.5, 5.0), tip, 3.4, +1)
    glow = [(x + 1.0, y + 1.0) for (x, y) in path]
    sc._stamp_lash(c, glow, 2.2, 1.0, lambda t, ox, oy: edge, outline=None)

    def tone(t, ox, oy):
        return core if abs(ox) + abs(oy) < 0.6 or t > 0.88 else hot

    sc._stamp_lash(c, path, 2.2, 1.0, tone, outline=None)

    # The crack at the far end: four arms, a white heart, the diagonals short, sparks thrown on.
    tx, ty = int(round(tip[0])), int(round(tip[1]))
    for d in range(1, 4):
        t_ = core if d == 1 else (hot if d == 2 else mid)
        for (dx, dy) in ((d, 0), (-d, 0), (0, d), (0, -d)):
            c.set(tx + dx, ty + dy, t_)
    for (dx, dy) in ((1, 1), (-1, -1), (1, -1), (-1, 1)):
        c.set(tx + dx, ty + dy, hot)
    c.set(tx, ty, core)
    for (dx, dy, t_) in ((4, 3, mid), (-3, 4, hot), (2, 5, mid), (5, 0, hot)):
        c.set(tx + dx, ty + dy, t_)

    # The sigil: a ring of the edge tone, a hot heart, four points - the hand the storm leaves.
    c.ring(px, py, 3.2, 3.2, mid)
    c.disc(px, py, 1.6, 1.6, core)
    for (dx, dy) in ((0, -5), (5, 0), (0, 5), (-5, 0)):
        c.set(int(px + dx), int(py + dy), hot)
    return c


def sleet_runes(sky=True):
    """Three runes of rime in flight, up and to the right, the wind behind them.

    The rune is the one the game casts (tools/art/sleet_runes.py), so the card and the field agree; the
    two beside it are smaller and a step behind, flying abreast. Pale ice on the cream card needs a
    ground under it: a disc of the wool row's night - a cold sky the blue of the runes stands off, and a
    different night from Starfall's and Stormsweep's. `sky=False` leaves the night off
    (tools/art/evolution_icons.py lays its own).
    """
    import sleet_runes as runes
    c = raster.Canvas(CELL, CELL, None)
    if sky:
        sky_deep = bl.tone("wool", "deep")
        sky_rim = bl.tone("wool", "shade")
        c.disc(15.5, 15.5, 15.0, 15.0, sky_rim)
        c.disc(15.5, 15.5, 14.0, 14.0, sky_deep)

    angle = -0.6
    ux, uy = math.cos(angle), math.sin(angle)
    # Each rune: where it flies from, and how big - a lead rune and two smaller ones a little behind it
    # and either side.
    px, py = -uy, ux
    lead = (18.5, 13.5)
    stream = ((lead[0], lead[1], 0.85),
              (lead[0] - ux * 6 + px * 8, lead[1] - uy * 6 + py * 8, 0.62),
              (lead[0] - ux * 6 - px * 8, lead[1] - uy * 6 - py * 8, 0.62))
    for (x, y, s) in reversed(stream):
        _over(c, runes.rune(angle, scale=s, size=CELL, centre=(x, y)))
    return c


def stormguard():
    """Stormguard: the storm that the blows you take bring down (in-flight item 9).

    Three things, in the order the eye takes them: the bolt - the lightning ramp, the brightest
    thing on the plate, struck down the middle - then the cloud bank it falls from, steel lit on
    its upper-left lobes (the key light), and the rain under the bank, short falling strokes of
    water that say the clap soaks as well as strikes. The bolt ends in a small burst above the
    ground rather than running off the cell: the break happens AROUND the wizard, not on the sky.
    """
    c = raster.Canvas(CELL, CELL, None)
    lcore, lhot, lmid, ledge = bl.ELEMENTS["lightning"]
    wcore, whot, wmid, wedge = bl.ELEMENTS["water"]

    # The cloud bank: three overlapping lobes across the top, shaded steel, lit upper-left.
    for (cx, cy, r) in ((8.0, 7.5, 5.2), (16.0, 6.0, 6.2), (24.0, 7.5, 5.0)):
        c.disc(cx, cy, r, r * 0.78, bl.tone("steel", "shade"))
    for (cx, cy, r) in ((7.0, 6.0, 3.4), (14.5, 4.6, 4.0)):
        c.disc(cx, cy, r, r * 0.7, bl.tone("steel", "base"))
    c.disc(6.2, 4.8, 1.8, 1.4, bl.tone("steel", "lit"))
    # The bank's dark underside, where the rain leaves it.
    for x in range(3, 29):
        c.set(x, 10, bl.tone("steel", "deep"))

    # The rain: short slanted strokes under the bank, thinning toward the ground.
    for i in range(14):
        x0 = 3.5 + (i * 2.1) % 25.0
        y0 = 12.0 + ((i * 5) % 4)
        length = 3 if i % 3 else 4
        tone_r = wmid if i % 2 else wedge
        for s in range(length):
            c.set(x0 - s * 0.4, y0 + s * 1.3, tone_r)
    for i in range(6):
        x0 = 5.0 + i * 4.3
        c.set(x0, 24.0 + (i % 3), whot)

    # The bolt: white-hot, two elbows, down the middle and brightest of all - drawn last so the
    # rain never crosses it. A one-pixel edge of the ramp's gold keeps it from bleeding into rain.
    path = [(17, 8), (16, 10), (15, 12), (16, 14), (14, 16), (15, 18), (13, 20), (14, 22)]
    for (x, y) in path:
        c.set(x + 1, y, ledge)
        c.set(x, y, lhot)
    for (x, y) in path[2:6]:
        c.set(x, y, lcore)
    # The burst where it lands: a small star of core and hot, above the cell's floor.
    bx, by = 14.0, 24.0
    c.set(bx, by, lcore)
    for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        c.set(bx + dx, by + dy, lhot)
    for (dx, dy) in ((2, 0), (-2, 0), (0, 2)):
        c.set(bx + dx, by + dy, lmid)
    return c


def gloam_veil():
    """Gloam Veil: the veil of dusk and wind that swallows one hit whole (in-flight item 9).

    The veil is drawn as what it is in play: ribbons of dusk circling a gap, nothing inside them -
    the wizard's place, kept empty so the icon reads as a guard and not a creature. The ribbons use
    the LIGHT end of the darkness ramp (on a near-black plate the dark end vanishes - Hollow Star's
    lesson), each with a pale fleck of wind at its leading tip, and one ribbon is torn: a notch on
    the lower right with its frayed ends flicked outward, the one hit the veil has already paid.
    """
    c = raster.Canvas(CELL, CELL, None)
    dcore, dhot, dmid, dedge = bl.ELEMENTS["darkness"]
    wcore, whot, wmid, wedge = bl.ELEMENTS["wind"]
    cx = cy = CELL / 2.0 - 0.5
    r = 11.0

    # Three ribbon arcs, swept clockwise, lit at the leading end and darkening along the tail.
    # The third arc stops short: the tear.
    arcs = ((math.radians(250.0), math.radians(95.0)),
            (math.radians(10.0), math.radians(95.0)),
            (math.radians(130.0), math.radians(62.0)))
    for k, (start, sweep) in enumerate(arcs):
        steps = int(sweep / 0.06)
        for i in range(steps + 1):
            t = i / float(steps)
            a = start + t * sweep
            rr = r + 0.9 * math.sin(a * 3.0 + k)
            tone_r = dhot if t < 0.3 else (dmid if t < 0.7 else dedge)
            c.set(cx + math.cos(a) * rr, cy + math.sin(a) * rr, tone_r)
            if t < 0.55:
                c.set(cx + math.cos(a) * (rr - 1.0), cy + math.sin(a) * (rr - 1.0), dmid if t < 0.3 else dedge)
        # The wind in it: a pale fleck at the leading tip of each ribbon.
        tip = start
        c.set(cx + math.cos(tip) * r, cy + math.sin(tip) * r, whot)
        c.set(cx + math.cos(tip) * (r + 1.0), cy + math.sin(tip) * (r + 1.0), wmid)

    # The tear, lower right: the third ribbon's frayed ends flicked outward from the gap.
    frays = ((math.radians(196.0), 1.5), (math.radians(204.0), 2.5), (math.radians(212.0), 1.8))
    for (a, out) in frays:
        c.set(cx + math.cos(a) * (r + out), cy + math.sin(a) * (r + out), dhot)
        c.set(cx + math.cos(a) * (r + out + 1.2), cy + math.sin(a) * (r + out + 1.2), wmid)

    # One brightest thread on the upper-left of the circle - the key light.
    a0 = math.radians(225.0)
    c.set(cx + math.cos(a0) * r, cy + math.sin(a0) * r, dcore)
    return c


def _spirit(kind):
    """Spiritual Weapon's forms, one card icon each (SpellVariant.Icon): the weapon itself on the
    wizard's violet night, drawn by tools/art/spirit_weapons.py, which draws the weapons the game swings."""
    def paint():
        import spirit_weapons
        return spirit_weapons.icon(kind)
    return paint


ICONS = [
    ("spirit-axe-arcane", _spirit("axe"), CELL),
    ("spirit-hammer-arcane", _spirit("hammer"), CELL),
    ("spirit-sword-arcane", _spirit("sword"), CELL),
    ("familiar-arcane", familiar_icon, CELL),
    ("familiar-dog-arcane", familiar_dog_icon, CELL),
    ("familiar-crow-arcane", familiar_crow_icon, CELL),
    ("familiar-cat-arcane", familiar_cat_icon, CELL),
    ("stormsweep-lightning", stormsweep, CELL),
    ("sleet-runes-ice", sleet_runes, CELL),
    ("starfall-light", starfall, CELL),
    ("sagittarius-light", sagittarius, CELL),
    ("sovereign-rite-light", sovereign_rite, CELL),
    ("riptide-water", riptide, CELL),
    ("hollow-star-dark", hollow_star, CELL),
    ("folio-arcane", folio, CELL),
    ("corposant-lightning", corposant, CELL),
    ("ink-ring-dark", ink_ring, CELL),
    ("windfall-grass", windfall, CELL),
    ("lancet-metal", lancet, CELL),
    ("iron-palisade-metal", iron_palisade, CELL),
    ("contagion-poison", contagion, CELL),
    ("bramble-seed-grass", bramble_seed, CELL),
    ("cinderbreath-fire", cinderbreath, CELL),
    ("mirefoot-bog", mirefoot, CELL),
    ("kindled-ward-light", kindled_ward, CELL),
    ("the-taking-dark", the_taking, CELL),
    ("orb-of-ages-light", orb_of_ages, CELL),
    ("gravewell-pit", gravewell, CELL),
    ("stormguard-lightning", stormguard, CELL),
    ("gloam-veil-dark", gloam_veil, CELL),
    ("unknown", unknown, CELL),
]

# The projectile sprites live with the other Bonelight art, not in the third party effects folder -
# nothing but the .tscn reads them, so there is no legacy path to be compatible with.
FX_DIR = os.path.join(ROOT, "assets", "bonelight", "effects")
FX = [
    ("riptide-surge", riptide_surge),
]


# Painted in Ma Liang's ink, the one sanctioned departure (.ai/art-direction.md section 3a): held to ink_wash.audit - an ink
# stop at an allowed alpha, or any contract colour opaque - in place of the contract's check. Nothing else is let off it.
INK_ICONS = ("ink-ring-dark",)


def write(canvas, name, cell):
    if name in INK_ICONS:
        import ink_wash
        bad = [(h, n) for (h, a, n) in ink_wash.audit(canvas.img)]
    else:
        bad = canvas.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in %s (art-direction.md section 3):" % name)
        for h, n in bad[:10]:
            print("   %s  x%d" % (h, n))
        return False

    path = os.path.join(OUT_DIR, name + ".png")
    canvas.scaled(2).save(path)
    print("  %-40s %dx%d" % (name, cell * 2, cell * 2))
    return True


def write_fx(canvas, name):
    bad = canvas.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in %s (art-direction.md section 3):" % name)
        for h, n in bad[:10]:
            print("   %s  x%d" % (h, n))
        return False

    if not os.path.isdir(FX_DIR):
        os.makedirs(FX_DIR)
    path = os.path.join(FX_DIR, name + ".png")
    canvas.scaled(2).save(path)
    print("  %-40s %dx%d" % (name, canvas.w * 2, canvas.h * 2))
    return True


def main(only=None):
    """`only`: icon names to write (--only=familiar-arcane,familiar-dog-arcane), so one icon can be redrawn
    without rewriting the rest - an unchanged painter re-run under another Python can still move a pixel."""
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)
    ok = True
    for name, fn, cell in ICONS:
        if only and name not in only:
            continue
        ok &= write(fn(), name, cell)
    for name, fn in FX:
        if only and name not in only:
            continue
        ok &= write_fx(fn(), name)
    if not ok:
        return 1
    print("")
    print("palette clean: every pixel is a colour art-direction.md defines")
    return 0


if __name__ == "__main__":
    _only = [a.split("=", 1)[1] for a in sys.argv if a.startswith("--only=")]
    sys.exit(main(only=_only[0].split(",") if _only else None))
