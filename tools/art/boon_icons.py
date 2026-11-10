# -*- coding: utf-8 -*-
"""Boon icons. Run: python tools/art/boon_icons.py

Eighteen 32x32 icons, one per boon, rendered at x2 to the 32x32 UI cell in art-direction.md
section 1 - the same cell the spell icons use, because a boon is offered on the same card as a
spell and has to survive the comparison.

Until now boons had no icon at all. LevelUpOption fell through to DefaultSpellIcon and tinted it by
the boon's dominant element, so all fourteen were the same picture in slightly different colours -
which is worse than no icon, because it reads as a spell the player has already seen.

WHAT MAKES A BOON ICON DIFFERENT FROM A SPELL ICON. A spell icon is an effect: emissive, white-hot
core, identity in the mid and edge. A boon is an OBJECT the player is carrying - a bead, a flask, a
coin, a scrap of cloth. So these are drawn on the MATERIAL rows, lit from the upper left like any
other object in the game, and they use the element ramps only where the object is genuinely glowing.
That difference is deliberate and it is the main thing telling the player, at a glance, that this
card is not a spell.

AND NOT AN ITEM EITHER. Each object then gets its element's light baked over it ("the enchantment",
below), and this script also writes the aura atlas BoonAura.cs turns behind it. See
.ai/level-up-screen.md section 2b.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bonelight as bl
import raster

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "assets", "bonelight", "ui", "boons")

CELL = 32


def _ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


def _lit_disc(c, cx, cy, r, mat):
    """A round object with the key light on its upper left, the house lighting model."""
    hi, lit, base, shade, deep = _ramp(mat)
    c.disc(cx, cy, r, r, base)
    for deg in range(0, 360, 3):
        a = math.radians(deg)
        facing = math.cos(a - math.radians(225.0))
        tone = hi if facing > 0.72 else (lit if facing > 0.1 else (deep if facing < -0.5 else shade))
        c.set(cx + math.cos(a) * (r - 0.4), cy + math.sin(a) * (r - 0.4), tone)
    c.set(cx - r * 0.38, cy - r * 0.38, hi)


# A CAST SHADOW HAS TO KNOW IT MIGHT LAND ON PAPER.
#
# This was a solid bar of pure occlusion, which is correct on a dark card and was correct for as
# long as the level-up cards were carved stone. They are torn parchment now, and on cream a solid
# OCC bar stops reading as a shadow under the object and starts reading as a black line ruled
# beneath it - which is exactly how it looked in the first screenshot of the new cards.
#
# Partial alpha instead. The RGB is still occlusion, so the palette audit is satisfied, and the
# shadow now darkens whatever it is over rather than replacing it.
#
# relic_icons.py borrows _shadow too. Re-run it (or build.py) after changing this: the first time
# round only the boons were regenerated, and the 29 relics and the Emberline set icon kept the
# solid bar until a full build rewrote them.
SHADOW_ALPHA = 110


def _shadow(c, cx, w):
    r, g, b, _ = raster.rgb(OCC)
    c.hline(int(cx - w), int(cx + w), 29, (r, g, b, SHADOW_ALPHA))


# --- the fifteen -----------------------------------------------------------------

def iron_rivets():
    """A plate with four rivets through it. Metal and Earth, armour."""
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("steel")
    c.poly([(7, 7), (25, 8), (26, 24), (8, 25), (7, 7)], base)
    c.hline(7, 25, 7, lit)
    c.vline(7, 7, 25, lit)
    c.hline(8, 26, 25, deep)
    c.vline(26, 8, 24, shade)
    for (x, y) in [(11, 11), (21, 11), (11, 21), (21, 21)]:
        c.disc(x, y, 2.2, 2.2, shade)
        c.set(x - 1, y - 1, hi)
        c.set(x + 1, y + 1, deep)
    _shadow(c, 16, 9)
    return c


def quicksilver_bead():
    """A bead of running metal that never sits still. Metal and Lightning, cooldowns."""
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("skin")
    _lit_disc(c, 15.5, 15.0, 8.0, "skin")
    # It is a liquid, so it sheds droplets rather than holding an edge.
    for (x, y, t) in [(24, 9, lit), (26, 12, base), (7, 22, base), (5, 19, shade)]:
        c.set(x, y, t)
    c.disc(24, 21, 2.0, 2.0, base)
    c.set(23, 20, hi)
    _shadow(c, 16, 8)
    return c


def sunsteel_filament():
    """A drawn thread of warm metal, coiled. Light and Metal, armour."""
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("gold")
    glow = bl.ELEMENTS["light"][1]
    for r in (10.5, 7.5, 4.5):
        c.ring(16, 16, r, r, base, 1)
    for deg in range(180, 330, 6):
        a = math.radians(deg)
        for r in (10.5, 7.5, 4.5):
            c.set(16 + math.cos(a) * r, 16 + math.sin(a) * r, lit)
    c.set(16, 16, glow)
    c.set(9, 9, hi)
    c.set(23, 23, deep)
    _shadow(c, 16, 9)
    return c


def tidewater_flask():
    """Salt water in a stoppered bottle. Water and Ice, regeneration."""
    c = raster.Canvas(CELL, CELL, None)
    g_hi, g_lit, g_base, g_shade, g_deep = _ramp("linen")
    water = bl.ELEMENTS["water"][2]
    water_hi = bl.ELEMENTS["water"][1]
    c.poly([(12, 7), (19, 7), (19, 12), (23, 18), (23, 26), (8, 26), (8, 18), (12, 12), (12, 7)], g_base)
    c.poly([(10, 18), (21, 18), (21, 24), (10, 24), (10, 18)], water)
    c.hline(10, 21, 18, water_hi)
    c.vline(9, 19, 25, g_lit)
    c.vline(22, 19, 25, g_deep)
    c.rect(11, 5, 20, 7, g_shade)
    c.hline(11, 20, 5, g_hi)
    c.set(12, 20, water_hi)
    _shadow(c, 16, 8)
    return c


def saltbound_chain():
    """Three crusted links. Water and Metal, it lashes back."""
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("steel")
    salt = bl.tone("linen", "hi")
    for (cx, cy) in [(9, 10), (16, 16), (23, 22)]:
        c.ring(cx, cy, 4.2, 3.4, base, 2)
        c.set(cx - 3, cy - 2, lit)
        c.set(cx + 3, cy + 2, deep)
    for (x, y) in [(7, 7), (18, 13), (25, 19), (12, 19)]:
        c.set(x, y, salt)
    _shadow(c, 18, 7)
    return c


def deepwater_pearl():
    """A pearl that draws things toward it. Water and Arcane, pickup range."""
    c = raster.Canvas(CELL, CELL, None)
    _lit_disc(c, 16, 16, 7.5, "linen")
    pull = bl.ELEMENTS["water"][2]
    for r in (11.0, 13.5):
        for deg in range(0, 360, 30):
            a = math.radians(deg)
            c.set(16 + math.cos(a) * r, 16 + math.sin(a) * r, pull)
    _shadow(c, 16, 7)
    return c


def lantern_oil():
    """A flask of oil that burns hotter than it should. Light and Fire, spell damage."""
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("amber")
    flame = bl.ELEMENTS["fire"][1]
    flame_core = bl.ELEMENTS["fire"][0]
    c.poly([(11, 13), (20, 13), (23, 19), (23, 26), (8, 26), (8, 19), (11, 13)], base)
    c.vline(9, 19, 25, lit)
    c.vline(22, 19, 25, deep)
    c.hline(9, 22, 26, deep)
    c.rect(13, 10, 18, 13, shade)
    # The wick, lit.
    c.poly([(15, 4), (17, 4), (18, 8), (16, 10), (14, 8), (15, 4)], flame)
    c.set(16, 7, flame_core)
    c.set(16, 6, flame_core)
    _shadow(c, 16, 8)
    return c


def gilded_mote():
    """A fleck of a warded circle, still alight. Light and Arcane, maximum health."""
    c = raster.Canvas(CELL, CELL, None)
    edge = bl.ELEMENTS["light"][3]
    mid = bl.ELEMENTS["light"][2]
    hot = bl.ELEMENTS["light"][1]
    core = bl.ELEMENTS["light"][0]
    c.disc(16, 16, 8.0, 8.0, edge)
    c.disc(16, 16, 5.5, 5.5, mid)
    c.disc(16, 16, 3.2, 3.2, hot)
    c.disc(16, 16, 1.6, 1.6, core)
    for (x, y) in [(16, 4), (16, 28), (4, 16), (28, 16)]:
        c.set(x, y, hot)
    for (x, y) in [(8, 8), (24, 8), (8, 24), (24, 24)]:
        c.set(x, y, mid)
    return c


def mossgrown_charm():
    """An old charm the wood has taken back. Grass and Earth, maximum health."""
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("gold")
    moss = bl.tone("lichen", "base")
    moss_lit = bl.tone("lichen", "lit")
    c.ring(16, 18, 7.5, 7.5, base, 2)
    c.set(12, 13, hi)
    c.set(21, 24, deep)
    c.vline(16, 5, 11, shade)
    c.hline(13, 19, 5, lit)
    for (x, y) in [(10, 21), (11, 23), (13, 24), (20, 13), (22, 15), (9, 17)]:
        c.set(x, y, moss)
    for (x, y) in [(11, 22), (21, 14)]:
        c.set(x, y, moss_lit)
    _shadow(c, 16, 7)
    return c


def thornseed():
    """A seed that puts out roots wherever it lands. Grass and Poison, spell area."""
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("verdant")
    husk = bl.tone("amber", "shade")
    c.disc(16, 15, 6.0, 7.0, husk)
    c.set(13, 11, bl.tone("amber", "lit"))
    c.set(19, 20, bl.tone("amber", "deep"))
    for (x0, y0, x1, y1) in [(16, 9, 16, 4), (11, 12, 6, 8), (21, 12, 26, 8),
                             (11, 19, 6, 23), (21, 19, 26, 23)]:
        c.line(x0, y0, x1, y1, base)
    for (x, y) in [(16, 4), (6, 8), (26, 8), (6, 23), (26, 23)]:
        c.set(x, y, lit)
    _shadow(c, 16, 6)
    return c


def shadegrease():
    """A pot of something nothing gets hold of. Darkness and Wind, movement."""
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("stone")
    slick = bl.ELEMENTS["darkness"][2]
    slick_hi = bl.ELEMENTS["darkness"][1]
    c.poly([(9, 14), (23, 14), (22, 26), (10, 26), (9, 14)], base)
    c.vline(10, 15, 25, lit)
    c.vline(22, 15, 25, deep)
    c.hline(9, 23, 13, shade)
    c.poly([(11, 12), (21, 12), (20, 15), (12, 15), (11, 12)], slick)
    c.hline(12, 19, 12, slick_hi)
    for (x, y) in [(24, 18), (25, 21)]:
        c.set(x, y, slick)
    _shadow(c, 16, 7)
    return c


def wishing_coin():
    """Thrown into still water by someone who is not coming back. Water and Light, luck."""
    c = raster.Canvas(CELL, CELL, None)
    _lit_disc(c, 16, 15, 8.5, "gold")
    mark = bl.tone("gold", "deep")
    c.vline(16, 11, 19, mark)
    c.hline(13, 19, 15, mark)
    ripple = bl.ELEMENTS["water"][2]
    c.hline(6, 26, 27, ripple)
    c.hline(9, 23, 29, ripple)
    return c


def umbral_veil():
    """A hand's width of somewhere else. Darkness and Ice, evasion."""
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("violet")
    c.poly([(7, 6), (25, 6), (24, 20), (20, 26), (16, 21), (12, 26), (8, 20), (7, 6)], base)
    c.hline(7, 25, 6, lit)
    c.vline(8, 7, 20, lit)
    c.vline(24, 7, 20, deep)
    for y in range(9, 22, 3):
        c.hline(10, 22, y, shade)
    c.set(11, 8, hi)
    return c


def rimebriar():
    """Frost-blackened thorns that close on whatever reaches you. Ice and Grass."""
    c = raster.Canvas(CELL, CELL, None)
    stem = bl.tone("verdant", "deep")
    frost = bl.ELEMENTS["ice"][1]
    frost_core = bl.ELEMENTS["ice"][0]
    c.line(6, 26, 14, 14, stem)
    c.line(14, 14, 24, 7, stem)
    c.line(26, 25, 18, 15, stem)
    for (x0, y0, x1, y1) in [(10, 20, 6, 17), (17, 11, 14, 6), (21, 10, 25, 12), (22, 20, 26, 19)]:
        c.line(x0, y0, x1, y1, stem)
        c.set(x1, y1, frost)
    for (x, y) in [(9, 21), (14, 14), (20, 10), (24, 7), (21, 18)]:
        c.set(x, y, frost)
    c.set(14, 13, frost_core)
    c.set(24, 6, frost_core)
    _shadow(c, 16, 9)
    return c


def taproot():
    """A root that only feeds once it is in the ground. Earth and Grass, healing while still.

    A parsnip-shaped taproot, pulled whole: three leaves at the crown, a body that tapers to a
    point, ring creases down it and rootlets reaching out of both sides. The flesh row because a
    root is alive (art-direction.md: flesh for anything alive, skin for anything that is not),
    which also makes it the one warm pale object in the boon set and keeps it apart from the
    Mossgrown Charm, the other Grass-and-Earth thing on the shelf. The glint's clear corner is
    the upper right, so the leaves lean left.
    """
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("flesh")
    leaf = bl.tone("verdant", "base")
    leaf_lit = bl.tone("verdant", "lit")
    leaf_deep = bl.tone("verdant", "shade")

    # The crown: three leaves fanning up and to the left from the shoulder of the root.
    for (x0, y0, x1, y1, tone) in [(14, 10, 8, 4, leaf), (15, 10, 13, 3, leaf_lit), (16, 10, 18, 5, leaf_deep)]:
        c.line(x0, y0, x1, y1, tone)
    for (x, y) in [(9, 4), (10, 6), (13, 4), (14, 6), (17, 6)]:
        c.set(x, y, leaf_lit if x < 15 else leaf)

    # The body, broad at the shoulder and running to a point, lit from the upper left. Drawn a
    # step DOWN the flesh row (shade for the mass, base for the lit side): a boon is silhouetted
    # against its element's backlight (BoonAura), and Earth's backlight is the same warm tan as the
    # flesh row's upper stops - a pale root vanished into it in the first cut.
    body = [(10, 11), (20, 11), (21, 12), (20, 15), (18, 19), (17, 22), (16, 25), (15, 28),
            (14, 25), (13, 22), (12, 19), (10, 15), (9, 12), (10, 11)]
    c.poly(body, shade)
    c.hline(11, 19, 11, base)
    c.hline(12, 18, 10, lit)
    c.vline(10, 12, 14, lit)
    c.line(11, 15, 12, 18, base)
    c.line(13, 19, 14, 24, base)
    c.vline(20, 13, 15, deep)
    c.line(19, 16, 17, 21, deep)
    c.line(16, 23, 15, 27, deep)
    # Ring creases, the way a root grows in steps.
    for (x0, x1, y) in [(12, 18, 14), (13, 17, 18), (14, 16, 22)]:
        c.hline(x0, x1, y, deep)
        c.set(x0, y, base)

    # Rootlets out of both sides, in the lit stop so they read on night glass, darker at the tip.
    for (x0, y0, x1, y1) in [(10, 15, 6, 17), (20, 16, 24, 18), (12, 20, 8, 23), (17, 22, 21, 25), (14, 25, 11, 27)]:
        c.line(x0, y0, x1, y1, base)
        c.set(x1, y1, shade)
    _shadow(c, 15, 6)
    return c


def waywiser():
    """A wheel that counts the road. Wind and Lightning, a spell wound faster while walking.

    A surveyor's measuring wheel: a wooden wheel with six spokes, a brass dial at the hub, and its
    long handle running up to the right, as it is pushed along. Wood is the flesh row a step down
    (the root's trick, so it holds against Wind's pale backlight); the brass is gold.
    """
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("flesh")
    g_hi, g_lit, g_base, g_shade, g_deep = _ramp("gold")
    cx, cy, r = 12.5, 19.0, 8.5
    # the handle first, so the wheel sits in front of its foot
    c.line(13, 18, 27, 4, shade)
    c.line(14, 18, 28, 4, base)
    c.line(14, 19, 28, 5, deep)
    c.rect(26, 2, 29, 4, deep)
    c.hline(26, 29, 2, base)
    # the rim, lit on its upper-left arc, and the tyre's iron band on its foot
    c.ring(cx, cy, r, r, shade, 2)
    for deg in range(0, 360, 3):
        a = math.radians(deg)
        facing = math.cos(a - math.radians(225.0))
        if facing > 0.3:
            c.set(cx + math.cos(a) * (r - 0.4), cy + math.sin(a) * (r - 0.4), base)
        elif facing < -0.5:
            c.set(cx + math.cos(a) * (r - 0.4), cy + math.sin(a) * (r - 0.4), deep)
    # six spokes
    for k in range(6):
        a = math.radians(30 + 60 * k)
        c.line(cx, cy, cx + math.cos(a) * (r - 1.5), cy + math.sin(a) * (r - 1.5), shade)
    # the brass dial at the hub, its needle
    c.disc(cx, cy, 2.6, 2.6, g_base)
    c.set(cx - 1, cy - 1, g_hi)
    c.line(cx, cy, cx + 1, cy - 2, g_deep)
    _shadow(c, 13, 8)
    return c


def bone_rattle():
    """Whatever is close remembers it is afraid. Darkness and Lightning, a fright when struck.

    A bundle of small bones bound to a short handle with sinew, fanned at the top where they knock
    together. Each bone has a knuckle at BOTH ends - the first cut gave them one, and a fan of
    shafts with a knob at the tip read as a bunch of flowers. Bone is the skin row
    (art-direction.md: skin for anything that is not alive); the handle is the flesh row a step
    down, as the waywiser's wood is, and the sinew its lit stop.
    """
    c = raster.Canvas(CELL, CELL, None)
    hi, lit, base, shade, deep = _ramp("skin")
    f_hi, f_lit, f_base, f_shade, f_deep = _ramp("flesh")
    # the handle, down to the lower left
    c.line(6, 27, 13, 19, f_shade)
    c.line(7, 27, 14, 19, f_base)
    c.line(7, 28, 14, 20, f_deep)
    c.disc(6, 27, 1.6, 1.6, f_shade)
    # three bones fanned wide out of the binding, each a shaft with a double knuckle at either
    # end - four, closer, filled in to one fan-shaped mass
    tie = (15.0, 17.0)
    for deg in (-108, -58, -8):
        a = math.radians(deg)
        ux, uy = math.cos(a), math.sin(a)
        x0, y0 = tie[0] + ux * 4.0, tie[1] + uy * 4.0
        x1, y1 = tie[0] + ux * 12.5, tie[1] + uy * 12.5
        c.line(x0, y0, x1, y1, base)
        c.line(x0 + 1, y0, x1 + 1, y1, shade)
        px, py = -uy, ux
        for (ex, ey) in ((x1, y1), (x0, y0)):
            for side in (-1, 1):
                c.disc(ex + px * side * 1.1, ey + py * side * 1.1, 1.2, 1.2, lit)
            c.set(ex - 1, ey - 1, hi)
    # the sinew binding where the bones meet the handle
    for (x, y) in ((13, 17), (14, 16), (15, 15), (14, 18), (15, 17), (16, 16), (16, 18)):
        c.set(x, y, f_lit)
    c.set(15, 19, f_shade)
    _shadow(c, 15, 7)
    return c


def _shaded_limb(c, a, b, r0, r1, tones):
    """A tapered capsule from a to b, shaded as a rounded body under the house key (upper left).

    Each pixel's normal is read off the nearest point of the axis, so the limb is lit on whichever
    side faces the key whatever way it runs - a straight-sided toning by "left of the axis" lights
    the wrong side of anything that is not upright. Later limbs paint over earlier ones.
    """
    hi, lit, base, shade, deep = tones
    ax, ay = a
    bx, by = b
    length = math.sqrt((bx - ax) ** 2 + (by - ay) ** 2)
    ux, uy = ((bx - ax) / length, (by - ay) / length) if length > 1e-6 else (0.0, 0.0)
    norm = math.sqrt(1.0 + 1.0 + 1.4 * 1.4)
    lx, ly, lz = -1.0 / norm, -1.0 / norm, 1.4 / norm
    for y in range(CELL):
        for x in range(CELL):
            t = 0.0
            if length > 1e-6:
                t = max(0.0, min(1.0, ((x - ax) * ux + (y - ay) * uy) / length))
            r = r0 + (r1 - r0) * t
            ox, oy = x - (ax + ux * t * length), y - (ay + uy * t * length)
            if ox * ox + oy * oy > r * r:
                continue
            nx, ny = ox / r, oy / r
            nz = math.sqrt(max(0.0, 1.0 - nx * nx - ny * ny))
            lum = nx * lx + ny * ly + nz * lz
            c.set(x, y, hi if lum > 0.9 else (lit if lum > 0.68 else (base if lum > 0.35 else (shade if lum > 0.05 else deep))))


def the_quarry():
    """The foot of the first black hare the hound ran down. Wind, the marked enemy takes more.

    In-flight 66 (Eric, 2026-11-10): The Coursing's boon. A hare's-foot charm, the trophy of a course:
    the long hind foot hangs from a gold cap and ring and turns at the heel into the toes, with bone
    claws showing through the fur. The fur is the violet row - the Black Hares of the Broken Circle
    are dark, and the boon window's backlight is what keeps a dark relic readable. It hangs from the
    upper left and the toes point down to the right, so the upper right stays clear for the glint.
    """
    c = raster.Canvas(CELL, CELL, None)
    fur = _ramp("violet")
    gold = _ramp("gold")
    hi, lit, base, shade, deep = fur
    claw = bl.tone("skin", "lit")
    claw_shade = bl.tone("skin", "shade")

    # the shank, then the foot turning at the heel, then the toes at its end
    _shaded_limb(c, (12.0, 9.0), (13.5, 19.0), 2.6, 3.2, fur)
    _shaded_limb(c, (13.5, 19.0), (20.5, 24.5), 3.2, 2.6, fur)
    for (x, y) in [(23.0, 22.0), (24.0, 25.0), (22.0, 27.5)]:
        _shaded_limb(c, (x, y), (x, y), 1.6, 1.6, fur)
    # the clefts between the toes
    for (x, y) in [(22, 24), (23, 23), (22, 26), (23, 26)]:
        c.set(x, y, deep)
    # the lie of the fur: hairs down the shank in shade, a parting along the foot, a fleck of sheen
    for (x0, y0, x1, y1) in [(13, 11, 14, 16), (14, 18, 15, 21), (16, 22, 19, 25)]:
        c.line(x0, y0, x1, y1, shade)
    for (x, y) in [(10, 13), (11, 17)]:
        c.set(x, y, hi)
    # the claws, bone, through the fur at the toes' tips
    for (x, y) in [(25, 21), (26, 24), (24, 28)]:
        c.set(x, y, claw)
    for (x, y) in [(25, 22), (26, 25), (23, 28)]:
        c.set(x, y, claw_shade)

    # the gold cap the foot is set in, and the ring it hangs by
    _shaded_limb(c, (11.5, 6.5), (12.0, 8.5), 2.4, 2.4, gold)
    c.ring(10, 2, 1.8, 1.8, gold[2], 1)
    c.set(9, 1, gold[0])
    c.set(11, 3, gold[4])
    _shadow(c, 17, 7)
    return c


# --- the enchantment -------------------------------------------------------------
#
# A BOON IS AN OBJECT, BUT IT IS NOT AN ITEM. The material-row rule above did its job - nobody
# mistakes a boon for a spell - and then did it too well: drawn as plain steel and glass, Iron
# Rivets read as something off a workbench, the same kind of thing as the relics a chest pays out
# (relic_icons.py borrows these very helpers). The player asked for boons to "always look
# magical". A relic is a thing you found; a boon is a thing that was GIVEN, and still has the
# giving in it.
#
# So every boon carries its first element as light, in three marks, all from the emissive ramps
# (art-direction.md section 3, "Elements are emissive, not pigment"), never a new colour:
#
#   1. an inlay - a small glyph cut into the object that glows (INLAYS, below). Only the objects
#      that were all material get one; the mote, the filament and the briar already burn;
#   2. an aura fringe - one ring of the element's `edge` tone at part alpha on the transparent
#      pixels touching the silhouette, so the object sits in its own light on any ground;
#   3. one four-point glint, core and hot, in the clear corner nearest the upper right.
#
# The animated half - the halo that breathes and the rune ring that turns behind the relic - is
# not baked: it is aura_atlas() below, drawn by scripts/BoonAura.cs. This half is what makes a
# static thumbnail read as magic on its own.

# The element each boon's magic shows in: the FIRST tag in BoonCatalog.ElementWeights. BoonAura.cs
# reads the same first tag from the catalog, so the baked fringe and the live aura agree.
ELEMENT = {
    "iron_rivets": "metal",
    "quicksilver_bead": "metal",
    "sunsteel_filament": "light",
    "tidewater_flask": "water",
    "saltbound_chain": "water",
    "deepwater_pearl": "water",
    "lantern_oil": "light",
    "gilded_mote": "light",
    "mossgrown_charm": "grass",
    "thornseed": "grass",
    "shadegrease": "darkness",
    "wishing_coin": "water",
    "rimebriar": "ice",
    "umbral_veil": "darkness",
    "taproot": "earth",
    "waywiser": "wind",
    "bone_rattle": "darkness",
    "the_quarry": "wind",
}

_ROLE = {"core": 0, "hot": 1, "mid": 2, "edge": 3}

# (x, y, role) in the 32-cell, painted in the boon's element ramp over the finished object.
INLAYS = {
    # The one the player named. A ward-diamond scored into the plate between the rivets, its
    # centre white-hot, and a spark of it on each rivet's upper-left - the rivets are what is
    # holding the magic in.
    "iron_rivets": [
        (16, 13, "mid"), (17, 14, "mid"), (18, 15, "mid"), (19, 16, "mid"), (18, 17, "mid"),
        (17, 18, "mid"), (16, 19, "mid"), (15, 18, "mid"), (14, 17, "mid"), (13, 16, "mid"),
        (14, 15, "mid"), (15, 14, "mid"),
        (16, 16, "core"), (16, 15, "hot"), (16, 17, "hot"), (15, 16, "hot"), (17, 16, "hot"),
        (10, 10, "core"), (20, 10, "core"), (10, 20, "core"), (20, 20, "core"),
    ],
    "quicksilver_bead": [
        (13, 15, "mid"), (13, 16, "mid"), (14, 18, "mid"), (16, 18, "mid"), (18, 17, "mid"),
        (15, 14, "hot"), (16, 15, "hot"), (16, 16, "core"),
    ],
    # A wave written on the water, the way a ward is written on a floor.
    "tidewater_flask": [
        (12, 21, "hot"), (13, 20, "hot"), (14, 21, "hot"), (15, 22, "core"), (16, 21, "hot"),
        (17, 20, "hot"), (18, 21, "hot"), (19, 22, "hot"),
    ],
    # The salt crust is what holds the charge: its flecks take the light.
    "saltbound_chain": [
        (7, 7, "hot"), (18, 13, "core"), (25, 19, "hot"), (12, 19, "hot"), (16, 16, "mid"),
    ],
    "deepwater_pearl": [
        (17, 17, "mid"), (16, 18, "mid"), (18, 16, "mid"), (16, 17, "hot"), (17, 16, "hot"),
        (16, 16, "core"),
    ],
    # A sun-cross on the flask - the oil is what is burning, not only the wick.
    "lantern_oil": [
        (15, 18, "mid"), (15, 22, "mid"), (13, 20, "mid"), (17, 20, "mid"),
        (15, 19, "hot"), (15, 21, "hot"), (14, 20, "hot"), (16, 20, "hot"), (15, 20, "core"),
    ],
    # A leaf-rune in the hollow of the charm.
    "mossgrown_charm": [
        (16, 14, "mid"), (15, 15, "mid"), (17, 15, "mid"), (16, 15, "hot"), (16, 16, "hot"),
        (15, 17, "mid"), (17, 17, "mid"), (16, 17, "core"), (16, 18, "hot"), (16, 19, "mid"),
        (16, 20, "edge"),
    ],
    # The vein the roots come out of.
    "thornseed": [
        (16, 11, "mid"), (16, 12, "mid"), (16, 13, "hot"), (16, 14, "hot"), (16, 15, "core"),
        (15, 16, "hot"), (17, 16, "hot"), (14, 17, "mid"), (18, 17, "mid"),
    ],
    # A crescent on the pot.
    "shadegrease": [
        (15, 18, "mid"), (14, 19, "mid"), (14, 20, "mid"), (14, 21, "mid"), (15, 22, "mid"),
        (16, 23, "mid"), (17, 19, "hot"), (17, 21, "hot"), (16, 20, "core"),
    ],
    # The struck cross on the coin is the wish, still lit.
    "wishing_coin": [
        (16, 11, "mid"), (16, 12, "mid"), (16, 13, "mid"), (16, 17, "mid"), (16, 18, "mid"),
        (16, 19, "mid"), (13, 15, "mid"), (14, 15, "mid"), (18, 15, "mid"), (19, 15, "mid"),
        (16, 14, "hot"), (16, 16, "hot"), (15, 15, "hot"), (17, 15, "hot"), (16, 15, "core"),
    ],
    # An eye-diamond woven into the cloth.
    "umbral_veil": [
        (16, 11, "hot"), (15, 12, "mid"), (17, 12, "mid"), (14, 13, "mid"), (18, 13, "mid"),
        (16, 13, "core"), (15, 14, "mid"), (17, 14, "mid"), (16, 15, "hot"),
    ],
    # A root-rune scored down the body - a stem that forks at its foot, as the thing it is cut
    # into does - lit from inside, so the root reads as feeding.
    "taproot": [
        (15, 13, "mid"), (15, 14, "hot"), (15, 15, "hot"), (15, 16, "core"), (15, 17, "hot"),
        (15, 18, "mid"), (14, 19, "mid"), (16, 19, "mid"), (13, 20, "edge"), (17, 20, "edge"),
    ],
    # The road the wheel has counted, written round its rim: three marks of wind in the wood, at the
    # points the rim faces the key. The dial carries the second tag, Lightning, as a glint (EXTRA_GLINTS).
    "waywiser": [
        (6, 15, "mid"), (6, 16, "hot"), (7, 13, "mid"), (9, 11, "hot"), (10, 11, "mid"),
        (13, 27, "edge"), (14, 27, "mid"),
    ],
    # A fright-rune scored into the bones: one mark on each knuckle, the middle bone's lit hottest.
    "bone_rattle": [
        (12, 9, "mid"), (13, 10, "edge"), (19, 10, "core"), (18, 11, "hot"), (20, 9, "edge"),
        (23, 16, "mid"), (22, 16, "edge"),
    ],
    # The wind of the chase written across the fur: three short streaks blown down the foot, the
    # middle one lit hottest - the Wind of the boon's one tag, and the speed of the thing it hunts.
    "the_quarry": [
        (11, 12, "edge"), (12, 12, "mid"), (13, 11, "hot"),
        (12, 17, "mid"), (13, 16, "hot"), (14, 16, "core"), (15, 15, "mid"),
        (16, 22, "edge"), (17, 21, "mid"), (18, 21, "hot"),
    ],
}

# A second element's glint on the object itself, where the boon's card puts its second tag in the
# picture: the waywiser's brass dial and the spark where the rattle's bones knock together. Painted
# after the inlay, in the second element's ramp (x, y, element, role).
EXTRA_GLINTS = {
    "waywiser": [(12, 18, "lightning", "core"), (13, 19, "lightning", "hot"), (11, 19, "lightning", "mid")],
    "bone_rattle": [(16, 6, "lightning", "core"), (17, 6, "lightning", "hot"), (16, 7, "lightning", "hot"),
                    (25, 10, "lightning", "mid")],
}

# The fringe is part alpha for the reason the shadow is (SHADOW_ALPHA): it has to read as light
# over whatever ground it lands on - the pale reliquary glass, the stone of the pause screen - and
# a solid edge-tone line would read as an outline, which art-direction.md keeps off objects.
FRINGE_ALPHA = 150
# Rows from here down belong to the contact shadow, which should not glow.
FRINGE_LAST_ROW = 27


def _ramp_tone(element, role):
    return bl.ELEMENTS[element][_ROLE[role]]


def _opaque(c, x, y):
    return c.get(x, y)[3] == 255


def _enchant(c, name):
    element = ELEMENT[name]
    for (x, y, role) in INLAYS.get(name, []):
        c.set(x, y, _ramp_tone(element, role))
    for (x, y, other, role) in EXTRA_GLINTS.get(name, []):
        c.set(x, y, _ramp_tone(other, role))

    # The fringe. Only around pixels that belong to a MASS (two or more opaque neighbours), so a
    # stray single-pixel spark does not sprout a little cross of its own.
    def massed(x, y):
        if not _opaque(c, x, y):
            return False
        n = 0
        for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if _opaque(c, x + dx, y + dy):
                n += 1
        return n >= 2

    r, g, b, _ = raster.rgb(_ramp_tone(element, "edge"))
    fringe = []
    for y in range(0, FRINGE_LAST_ROW + 1):
        for x in range(CELL):
            if c.get(x, y)[3] != 0:
                continue
            for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if massed(x + dx, y + dy):
                    fringe.append((x, y))
                    break
    for (x, y) in fringe:
        c.set(x, y, (r, g, b, FRINGE_ALPHA))

    # The glint: the clear 3x3 nearest the upper right, so it never sits on the object.
    best = None
    for y in range(2, 14):
        for x in range(16, 30):
            clear = True
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    if c.get(x + dx, y + dy)[3] != 0:
                        clear = False
            if clear:
                d = (x - 27) ** 2 + (y - 4) ** 2
                if best is None or d < best[0]:
                    best = (d, x, y)
    if best is not None:
        _, gx, gy = best
        hot = _ramp_tone(element, "hot")
        c.set(gx, gy, _ramp_tone(element, "core"))
        for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            c.set(gx + dx, gy + dy, hot)
    return c


# --- the aura atlas ----------------------------------------------------------------
#
# What turns behind the relic. scripts/BoonAura.cs draws, at one integer scale, the backlight,
# then the breath over it, then one frame of the ring, then the icon on top - so the relic stays on
# exact pixels and the ring cannot drift off it.
#
# The cell is the 32 icon cell with a 6-pixel band round it (44), which is where the ring lives:
# the objects fill most of their own cell, so a ring inside it would be hidden behind the very
# thing it is meant to be around. On a level-up card the band is taken from inside the reliquary
# window, never from the plaque around it - on a phone the text starts twelve pixels from the
# window, and the aura sits behind the icon, not behind the words.
#
# THE RELIC IS BACKLIT, AND THAT IS WHAT LET THE WINDOW GO DARK. The window used to be pale glass,
# because several relics (the flask, the seed, the veil, the chain) are dark or mid-value and
# vanished on enamel. A glow cannot show on a pale ground, though - the first cut of this aura
# drew Iron Rivets, the boon the player named, in a halo of white on white. So the window is night
# glass now (ui_frames.py:boon_window), and the pale ground the dark relics need is supplied by the
# BACKLIGHT: a disc of the element's core and hot tones, always at full strength, that the relic is
# silhouetted against. Only the BREATH - the outer rings, mid and edge - pulses, so how readable the
# relic is never depends on where in its breath the aura happens to be.
#
# ROTATION IS FRAMES, NOT A ROTATED TEXTURE. A pixel-art ring rotated by a transform at an
# arbitrary angle and drawn nearest-neighbour crawls - every pixel re-rounds every frame. Here each
# frame is drawn on the grid: the runes stay upright and move round the circle a pixel at a time,
# and the dashed track turns the other way. The runes repeat every 60 degrees and the dashes every
# 15, so twelve frames of 5 degrees is a seamless loop.
#
# Layout: one row per element, in the order of the C# Element enum (AURA_ORDER - a Python 2 dict
# has no order, so it is spelled out). Columns 0-11 are the ring frames, 12 the backlight, 13 the
# breath. Every ground it is drawn on is dark - the night glass, the pause screen's stone - so the
# marks take the light end of the ramp.
AURA_CELL = 44
AURA_FRAMES = 12
AURA_BACKLIGHT_COLUMN = 12
AURA_BREATH_COLUMN = 13
AURA_ORDER = ["fire", "ice", "arcane", "darkness", "light", "grass", "earth", "wind",
              "lightning", "poison", "metal", "water"]
AURA_PATH = os.path.join(ROOT, "assets", "bonelight", "ui", "ui-boon-aura.png")

# Six runes, 5x5. Invented, not borrowed from any real script - they have to read as writing
# rather than as any particular writing. NONE MAY BE A LATIN LETTER: the first set had an S, an R
# and an X in it, and a ring that spells letters reads as a label rather than as a ward.
RUNES = [
    ["..#..",
     ".###.",
     "#.#.#",
     "..#..",
     "..#.."],
    [".#.#.",
     ".###.",
     "..#..",
     ".###.",
     ".#.#."],
    ["..#..",
     ".#.#.",
     "..#..",
     "#.#.#",
     ".###."],
    ["..#..",
     ".#.#.",
     "#...#",
     ".#.#.",
     "..#.."],
    ["#.#.#",
     "#.#.#",
     "#####",
     "..#..",
     "..#.."],
    ["#..#.",
     ".#.#.",
     "..##.",
     "...#.",
     "...#."],
]
RUNE_RADIUS = 18.0
TRACK_RADIUS = 20.6
TRACK_PERIOD = 15.0
TRACK_ON = 7.0
FRAME_STEP = 60.0 / AURA_FRAMES
TRACK_ALPHA = 170

# Bands, as (outer radius, role, alpha), innermost first; a role of None leaves the band clear.
# Stepped rather than smooth so it is still pixel art, and round inside the square window.
BACKLIGHT = [(8.0, "core", 200), (12.0, "hot", 180), (15.0, "hot", 115)]
BREATH = [(15.0, None, 0), (17.5, "mid", 110), (19.8, "edge", 95), (21.6, "edge", 50)]


def _aura_ring(element, frame):
    c = raster.Canvas(AURA_CELL, AURA_CELL, None)
    centre = (AURA_CELL - 1) / 2.0
    rune_tone = _ramp_tone(element, "hot")
    r, g, b, _ = raster.rgb(_ramp_tone(element, "mid"))
    track = (r, g, b, TRACK_ALPHA)

    # The track turns against the runes.
    turn = -frame * FRAME_STEP
    seen = set()
    for tenth in range(0, 3600):
        deg = tenth / 10.0
        if (deg - turn) % TRACK_PERIOD >= TRACK_ON:
            continue
        a = math.radians(deg)
        px = int(round(centre + math.cos(a) * TRACK_RADIUS))
        py = int(round(centre + math.sin(a) * TRACK_RADIUS))
        if (px, py) not in seen:
            seen.add((px, py))
            c.set(px, py, track)

    offset = frame * FRAME_STEP
    for i, glyph in enumerate(RUNES):
        a = math.radians(-90.0 + i * 60.0 + offset)
        x0 = int(round(centre + math.cos(a) * RUNE_RADIUS - 2))
        y0 = int(round(centre + math.sin(a) * RUNE_RADIUS - 2))
        for gy, row in enumerate(glyph):
            for gx, ch in enumerate(row):
                if ch == "#":
                    c.set(x0 + gx, y0 + gy, rune_tone)
    return c


def _aura_bands(element, bands):
    c = raster.Canvas(AURA_CELL, AURA_CELL, None)
    centre = (AURA_CELL - 1) / 2.0
    for y in range(AURA_CELL):
        for x in range(AURA_CELL):
            d = math.sqrt((x - centre) ** 2 + (y - centre) ** 2)
            for (outer, role, alpha) in bands:
                if d < outer:
                    if role is not None:
                        r, g, b, _ = raster.rgb(_ramp_tone(element, role))
                        c.set(x, y, (r, g, b, alpha))
                    break
    return c


def aura_atlas():
    columns = AURA_BREATH_COLUMN + 1
    sheet = raster.Canvas(columns * AURA_CELL, len(AURA_ORDER) * AURA_CELL, None)
    for row, element in enumerate(AURA_ORDER):
        y = row * AURA_CELL
        for f in range(AURA_FRAMES):
            sheet.img.paste(_aura_ring(element, f).img, (f * AURA_CELL, y))
        sheet.img.paste(_aura_bands(element, BACKLIGHT).img, (AURA_BACKLIGHT_COLUMN * AURA_CELL, y))
        sheet.img.paste(_aura_bands(element, BREATH).img, (AURA_BREATH_COLUMN * AURA_CELL, y))
    sheet.px = sheet.img.load()
    return sheet


ICONS = [
    ("iron_rivets", iron_rivets),
    ("quicksilver_bead", quicksilver_bead),
    ("sunsteel_filament", sunsteel_filament),
    ("tidewater_flask", tidewater_flask),
    ("saltbound_chain", saltbound_chain),
    ("deepwater_pearl", deepwater_pearl),
    ("lantern_oil", lantern_oil),
    ("gilded_mote", gilded_mote),
    ("mossgrown_charm", mossgrown_charm),
    ("thornseed", thornseed),
    ("shadegrease", shadegrease),
    ("wishing_coin", wishing_coin),
    ("umbral_veil", umbral_veil),
    ("rimebriar", rimebriar),
    ("taproot", taproot),
    ("waywiser", waywiser),
    ("bone_rattle", bone_rattle),
    ("the_quarry", the_quarry),
]


def main():
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)

    ok = True
    for name, fn in ICONS:
        canvas = _enchant(fn(), name)
        bad = canvas.audit()
        if bad:
            ok = False
            print("OFF-CONTRACT COLOURS in %s:" % name)
            for h, n in bad[:6]:
                print("   %s  x%d" % (h, n))
            continue
        canvas.scaled(2).save(os.path.join(OUT_DIR, name + ".png"))
        print("  %-20s 64x64" % name)

    # Authored 1:1 and drawn at an integer scale by BoonAura.cs, unlike the icons above, which
    # were saved at x2 before the grid rule was written down.
    atlas = aura_atlas()
    bad = atlas.audit()
    if bad:
        ok = False
        print("OFF-CONTRACT COLOURS in the aura atlas:")
        for h, n in bad[:6]:
            print("   %s  x%d" % (h, n))
    else:
        atlas.save(AURA_PATH)
        print("  %-20s %dx%d" % ("ui-boon-aura", atlas.w, atlas.h))

    if not ok:
        return 1
    print("")
    print("%d boon icons and the aura atlas, palette clean" % len(ICONS))
    return 0


if __name__ == "__main__":
    sys.exit(main())
