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


def _familiar_card(head, sky=True, motes=((26, 7, 1), (28, 12, 2), (24, 4, 2), (5, 9, 2), (7, 5, 1), (27, 24, 2)),
                   moon_at=None):
    """A familiar's head on a patch of night, the Arcane motes round it, and (moon_at: x, y, r) a moon behind
    the head. The night is Starfall's: the darkest cold material row with a lit rim, because a fawn head on
    the level-up card's cream page would vanish into it. `sky=False` leaves the night and the moon off, for
    tools/art/evolution_icons.py, which lays its own."""
    c = raster.Canvas(CELL, CELL, None)
    if sky:
        sky_deep = bl.tone("arcane", "deep")
        sky_rim = bl.tone("arcane", "shade")
        c.disc(15.5, 15.5, 15.0, 15.0, sky_rim)
        c.disc(15.5, 15.5, 14.0, 14.0, sky_deep)
        if moon_at:
            moon(c, *moon_at)
    c.blit(head.img, 0, 0)
    arc = bl.ELEMENTS["arcane"]
    for (x, y, t) in motes:
        c.set(x, y, arc[t])
    if motes:
        c.set(motes[0][0], motes[0][1] - 1, arc[0])
    return c


def familiar_icon(sky=True):
    """The Familiar, before its form is chosen: the three it can be taken as, together - the cat, the
    hound and the crow, each in the wizard's hat - on a patch of night (familiar.trio). Each is drawn by
    tools/art/familiar.py's own renderer, so the card shows the creatures the player will see at his
    heel. The eyes are the calm ones (familiar.calm_eye; Eric, 2026-10-28: "it looks a little creepy,
    the eye is too prominent")."""
    import familiar
    return _familiar_card(familiar.trio(), sky, motes=((27, 3, 1), (29, 8, 2), (3, 4, 2), (2, 9, 1)), moon_at=(10.0, 8.5, 7.0))


def familiar_dog_icon(sky=True):
    """The Familiar taken as a dog (SpellVariant.Icon): the hound's head in the wizard's hat, its collar
    star lit - a good dog: the calm eye and a closed, easy mouth (familiar.portrait)."""
    import familiar
    return _familiar_card(familiar.portrait(), sky, moon_at=(21.0, 8.5, 7.5))


def familiar_crow_icon(sky=True):
    """The Familiar taken as a crow: its head and shoulder in the hat, the beak out to the right, a wing's
    gold edge behind it (familiar.crow_portrait)."""
    import familiar
    return _familiar_card(familiar.crow_portrait(), sky, motes=((27, 5, 1), (5, 10, 2), (28, 25, 2), (24, 2, 2)),
                          moon_at=(21.0, 9.0, 8.0))


def familiar_cat_icon(sky=True):
    """The Familiar taken as a cat: its round face three-quarter on, ears up through the hat's brim, a
    content eye (familiar.cat_portrait)."""
    import familiar
    return _familiar_card(familiar.cat_portrait(), sky, motes=((27, 6, 1), (28, 12, 2), (26, 26, 2), (4, 8, 2)),
                          moon_at=(25.0, 7.0, 6.0))


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
    ("riptide-water", riptide, CELL),
    ("hollow-star-dark", hollow_star, CELL),
    ("iron-palisade-metal", iron_palisade, CELL),
    ("contagion-poison", contagion, CELL),
    ("bramble-seed-grass", bramble_seed, CELL),
    ("cinderbreath-fire", cinderbreath, CELL),
    ("mirefoot-bog", mirefoot, CELL),
    ("kindled-ward-light", kindled_ward, CELL),
    ("gravewell-pit", gravewell, CELL),
    ("unknown", unknown, CELL),
]

# The projectile sprites live with the other Bonelight art, not in the third party effects folder -
# nothing but the .tscn reads them, so there is no legacy path to be compatible with.
FX_DIR = os.path.join(ROOT, "assets", "bonelight", "effects")
FX = [
    ("riptide-surge", riptide_surge),
]


def write(canvas, name, cell):
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
