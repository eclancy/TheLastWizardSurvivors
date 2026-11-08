# -*- coding: utf-8 -*-
"""The lamps along the Last Bridge's parapet, which Morgan le Fay's barge drifts by (.ai/unlock-puzzles.md 3.7).

Run: python tools/art/barge_lamps.py

    world/barge/lamp.png        a parapet lamp, 22x62 a frame, ten frames: dead (0), kindling (1), burning (2-5, four
                                flickers), guttering (6-7) as it burns down, and hers (8-9): the landing's lamp, already
                                lit in her lantern's cold mist-green, breathing
    tools/art/_barge_lamps_preview.png   (gitignored) for looking at

"Her barge drifts only as far as the lamps along the parapet are lit." His bridge's lamps have been dead since he
took the strait: a row of iron lamp standards on stone plinths, one every few hundred pixels along the parapet on
her side, from her mooring to her landing. Stand by one and it lights; a lit lamp burns for a while and gutters out.
She sets out from a lamp only while it burns, so each dark lamp ahead is a stop where she waits in the dark.

THE LAMPS ARE THE WIZARDS' LIGHT, AND THE LAST ONE IS HERS. A lit lamp burns in fire's ramp - warm, which in this
world belongs to what he does not have (tomb_beacons.py's beacons are the same fire) - and as it burns down the
flame sinks and the glass dims before it goes out. The landing's lamp is lit already, in wind's ramp: her lantern's
colour (barge.py, BargeEvent.TellColor), cold because he keeps her. So the row reads from where she waits to where
she is going: dead iron, the warm ones he has lit, and one cold light at the end.

THE STANDARD. A plinth of the bridge's own stone (stone's row, the parapet's masonry), an iron post with two collars
(steel's dark end: iron, not polished steel), a cup at its head and on it a four-sided lantern - corner bars and a
middle bar, panes of glass between, a pyramid roof and a ring finial. The key is from the upper left, so the left
faces are lit and the right in shade. It is upright and symmetrical in its outline, so it stands the same on either
shore; it is never mirrored (a mirror would put the light on the wrong side).

Every sheet is authored 1:1 and drawn by the game at x2 (Sprite2D Scale 2); the lamp sorts with the bodies at its
foot. Python 2.7 and PIL, as every generator here.
"""
from __future__ import division, print_function

import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image

import bonelight as bl
import raster

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT_WORLD = os.path.join(ROOT, "assets", "bonelight", "world", "barge")
PREVIEW = os.path.join(HERE, "_barge_lamps_preview.png")


def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


ST = ramp("stone")
STL = ramp("steel")
FIRE = bl.ELEMENTS["fire"]       # core, hot, mid, edge
WIND = bl.ELEMENTS["wind"]       # her light: the strait's mist

# Iron: steel's dark end. lit (the key-facing edge), body, shade.
IRON_LIT, IRON, IRON_SHADE = STL[2], STL[3], STL[4]

W, H = 22, 62
MIDDLE = 11             # the post's middle column
FOOT = 58               # the row the plinth stands on: the sprite's sort point (BargeEvent.LampFoot)
GLASS_Y0, GLASS_Y1 = 10, 18     # the panes, between the lantern's top and bottom bars
HEART = (11, 14)        # the flame's heart: where BargeEvent draws the lamp's light from (LampHeart)

DEAD, KINDLING, BURNING, GUTTER, HERS = 0, 1, 2, 6, 8
FRAMES = 10


def outline(c, skip_down=False):
    """A one-pixel occlusion edge round everything drawn, eight ways (four with skip_down: a thing standing on the
    ground has its contact shadow drawn separately). tomb_beacons.py's, the same rule."""
    w, h = c.w, c.h
    occ = raster.rgb(OCC)
    solid = [[c.get(x, y)[3] != 0 for x in range(w)] for y in range(h)]
    for y in range(h):
        for x in range(w):
            if solid[y][x]:
                continue
            for dx in (-1, 0, 1):
                for dy in ((-1, 0) if skip_down else (-1, 0, 1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and solid[ny][nx] and c.get(nx, ny) != occ:
                        c.set(x, y, OCC)
                        break
                else:
                    continue
                break


def plinth(c):
    """The bridge's stone: a square block, its top in the key light, its front in courses, the left edge lit and
    the right in shade, a chipped corner."""
    m = MIDDLE
    x0, x1 = m - 5, m + 5
    for y in (51, 52):                                       # the top face
        for x in range(x0 + 1, x1):
            c.set(x, y, ST[1] if y == 51 else ST[2])
        c.set(x0, y, ST[0] if y == 51 else ST[1])
        c.set(x1, y, ST[2])
    c.hline(x0 + 1, x1 - 1, 51, ST[1])
    c.set(x0 + 2, 51, ST[0])
    for y in range(53, FOOT + 1):                            # the front, in courses
        for x in range(x0, x1 + 1):
            c.set(x, y, ST[2] if x < m + 2 else ST[3])
        c.set(x0, y, ST[1])
        c.set(x1, y, ST[4])
    c.hline(x0 + 1, x1 - 1, 55, ST[3])                       # a bed joint
    c.set(m - 1, 54, ST[3])                                  # a head joint either side of it
    c.set(m + 2, 56, ST[4])
    c.set(m + 2, 57, ST[4])
    c.set(x0 + 1, FOOT, ST[3])
    c.set(x1, 52, ST[3])                                     # the chipped corner
    c.set(x1, 53, ST[3])


def post(c):
    """The iron post from the plinth to the cup: three columns, lit, body, shade, with two collars."""
    m = MIDDLE
    for y in range(21, 51):
        c.set(m - 1, y, IRON_LIT)
        c.set(m, y, IRON)
        c.set(m + 1, y, IRON_SHADE)
    for (y, hw) in ((49, 2), (50, 2), (35, 2), (36, 2), (24, 2)):
        for x in range(m - hw, m + hw + 1):
            c.set(x, y, IRON_LIT if x < m else (IRON if x == m else IRON_SHADE))
        c.set(m - hw, y, STL[1] if y in (35, 49) else IRON_LIT)
    # The cup at its head that the lantern stands in.
    for (y, hw) in ((20, 4), (21, 3), (22, 2)):
        for x in range(m - hw, m + hw + 1):
            c.set(x, y, IRON_LIT if x < m - 1 else (IRON if x < m + 2 else IRON_SHADE))
    c.set(m - 4, 20, STL[1])


def lantern(c, glass):
    """The four-sided lantern: a top bar and a bottom bar, two corner bars and a middle bar, panes of glass between
    (glass(x, y) gives each pane pixel), a pyramid roof and a ring finial."""
    m = MIDDLE
    x0, x1 = m - 4, m + 4
    for y in range(GLASS_Y0, GLASS_Y1 + 1):
        for x in range(x0 + 1, x1):
            if x != m:
                c.set(x, y, glass(x, y))
    for y in range(GLASS_Y0 - 1, GLASS_Y1 + 2):              # the bars
        c.set(x0, y, IRON_LIT)
        c.set(m, y, IRON)
        c.set(x1, y, IRON_SHADE)
    c.hline(x0, x1, GLASS_Y0 - 1, IRON)
    c.hline(x0, x1, GLASS_Y1 + 1, IRON)
    c.set(x0, GLASS_Y0 - 1, STL[1])
    c.set(x0, GLASS_Y1 + 1, IRON_LIT)
    # The roof: a pyramid in iron, its left slope in the key.
    for (y, hw) in ((8, 5), (7, 4), (6, 3), (5, 2), (4, 1)):
        for x in range(m - hw, m + hw + 1):
            c.set(x, y, IRON_LIT if x < m else (IRON if x == m else IRON_SHADE))
        c.set(m - hw, y, STL[1])
    c.hline(m - 5, m + 5, 8, IRON)
    c.set(m - 5, 8, IRON_LIT)
    c.set(m + 5, 8, IRON_SHADE)
    # The finial: a ring on a stem.
    c.set(m, 3, IRON)
    for (x, y, col) in ((m - 1, 1, IRON_LIT), (m, 0, STL[1]), (m + 1, 1, IRON_SHADE), (m, 2, IRON), (m - 1, 2, IRON_LIT),
                        (m + 1, 2, IRON_SHADE)):
        c.set(x, y, col)


def dead_glass(rng):
    """Dark glass, sooted from the last time anyone lit it: stone's deep end, a smear of soot near the top, the
    upper-left corner of each pane catching the key a stop up."""
    soot = set((rng.randint(MIDDLE - 3, MIDDLE + 3), rng.randint(GLASS_Y0, GLASS_Y0 + 2)) for _ in range(5))

    def glass(x, y):
        if (x, y) in soot:
            return ST[4]
        if y == GLASS_Y0 and x in (MIDDLE - 3, MIDDLE + 1):
            return ST[1]
        if x in (MIDDLE - 3, MIDDLE + 1) and y == GLASS_Y0 + 1:
            return ST[2]
        return ST[3] if x < MIDDLE else ST[4]
    return glass


def flame_glass(frame, height, ramp_):
    """Glass lit from inside: the flame on the middle bar's line (it shows through both panes, behind the bar), the
    panes glowing hot near it and to the ramp's mid and edge at their corners. height 1 is a full flame; less is
    burning down. ramp_ is fire's for his lamps, wind's for hers."""
    core, hot, mid, edge = ramp_
    lean = [0, -1, 0, 1][frame % 4]
    top = GLASS_Y1 - int(round(7 * height))                  # the flame's tip row
    hx, hy = HEART

    def glass(x, y):
        # The flame: two columns either side of the bar, tapering up to its tip, leaning by frame.
        if y >= top:
            t = (GLASS_Y1 - y) / float(max(1, GLASS_Y1 - top))
            fx = hx + int(round(lean * t))
            half = 2 if t < 0.45 else 1
            if abs(x - fx) <= half:
                if t < 0.3 and abs(x - fx) <= 1:
                    return core
                return hot if t < 0.75 else mid
        # The panes: hot round the heart, then mid, then edge; as it burns down the glow draws in toward the wick
        # and the glass at the corners goes dark again, so a guttering lamp is a smaller light, not a speckled one.
        d = abs(x - hx) + abs(y - hy) * 0.6
        s = 0.6 + 0.4 * height
        if d < 2.6 * s:
            return hot
        if d < 4.2 * s:
            return mid
        if d < 5.6 * s:
            return edge
        return ST[3] if x < MIDDLE else ST[4]
    return glass


def kindling_glass(rng):
    """Dark glass with an ember caught at the wick: the first fire, before the flame stands up."""
    dead = dead_glass(rng)

    def glass(x, y):
        hx, hy = HEART
        if y >= GLASS_Y1 - 1 and abs(x - hx) <= 1:
            return FIRE[2] if y == GLASS_Y1 else FIRE[3]
        if y == GLASS_Y1 - 2 and x == hx - 1:
            return FIRE[3]
        if y >= GLASS_Y1 - 2 and abs(x - hx) == 2:
            return FIRE[3]
        return dead(x, y)
    return glass


def lamp(frame):
    c = raster.Canvas(W, H)
    rng = random.Random(37)
    plinth(c)
    post(c)
    if frame == DEAD:
        glass = dead_glass(rng)
    elif frame == KINDLING:
        glass = kindling_glass(rng)
    elif frame < GUTTER:
        glass = flame_glass(frame - BURNING, 1.0, FIRE)
    elif frame < HERS:
        glass = flame_glass(frame - GUTTER, 0.5 if frame == GUTTER else 0.3, FIRE)
    else:
        # Hers: still, not flickering - a light held, not a fire; the two frames breathe.
        glass = flame_glass(0, 0.85 if frame == HERS else 0.7, WIND)
    lantern(c, glass)
    outline(c, skip_down=True)
    for x in range(MIDDLE - 7, MIDDLE + 8):                  # the contact shadow
        if c.get(x, FOOT + 1)[3] == 0:
            c.set(x, FOOT + 1, OCC)
    return c


# --- output ------------------------------------------------------------------------------------------

def _audit(name, c):
    bad = c.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in %s:" % name)
        for h, cnt in bad[:6]:
            print("   %s  x%d" % (h, cnt))
        return False
    return True


def _strip(canvases):
    img = Image.new("RGBA", (W * len(canvases), H), (0, 0, 0, 0))
    for i, cv in enumerate(canvases):
        img.alpha_composite(cv.img, (i * W, 0))
    return img


def main():
    if not os.path.isdir(OUT_WORLD):
        os.makedirs(OUT_WORLD)
    ok = True
    lamps = [lamp(f) for f in range(FRAMES)]
    for i, cv in enumerate(lamps):
        ok = _audit("lamp %d" % i, cv) and ok
    _strip(lamps).save(os.path.join(OUT_WORLD, "lamp.png"))

    # The preview: every frame at x4 on the bridge's dark, then five at x2 as the event stands them (dead, lit,
    # lit, guttering, hers), so the row reads as it does on the parapet.
    bg = (20, 24, 36, 255)
    z = 4
    width = W * z * FRAMES + 20
    sheet = Image.new("RGBA", (width, H * z + H * 2 + 40), bg)
    for i, cv in enumerate(lamps):
        sheet.alpha_composite(cv.scaled(z), (10 + i * W * z, 10))
    for k, f in enumerate((DEAD, BURNING, BURNING + 2, GUTTER + 1, HERS)):
        sheet.alpha_composite(lamps[f].scaled(2), (10 + k * 90, H * z + 24))
    sheet.convert("RGB").save(PREVIEW)
    print("  lamp %d frames, %dx%d, foot row %d, heart %s" % (FRAMES, W, H, FOOT, HEART))
    if not ok:
        return 1
    print("parapet lamps written, palette clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
