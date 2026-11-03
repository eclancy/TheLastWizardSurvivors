# -*- coding: utf-8 -*-
"""The Archmage, playable: the title screen's wizard on the player's cell. Run: python tools/art/player_archmage.py

    assets/bonelight/characters/archmage-{1,2,3,4}.png      the idle, frame one is the portrait
    assets/bonelight/characters/archmage-{idle,moving,hurt,death}.png
    scenes/resources/ArchmageFrames.tres
    tools/art/_player_archmage.png     (gitignored) the pose at x8, filled black, and every animation

    --preview   writes only the preview; nothing under assets/ or scenes/ is touched.

WHO HE IS (.ai/protagonist.md). The blue-robed figure in the ward on the title screen and in the prologue:
the player, the one wizard the dark wizard did not take - "You are the one he missed" - and since
2026-11-02 the fifth starter, named The Archmage (Eric). This sheet was first drawn for Merlin, in commit
bfada4df (tools/art/player_merlin.py there), when Merlin was briefly given this figure; Eric took it back the
same day ("The blue robed wizard should represent player"), Merlin got a look of his own
(merlin_concepts.py), and this file is that first generator recovered, unchanged in every pixel it draws.
Only the names, the sash's control characters and this note are new. hero.py and the title art are
untouched, as they were then.

HE IS hero.wizard_hero AT TITLE DEFAULTS (robe "wool", beard "mane-spear"), REDRAWN ON THE 48 CELL - not
the title render scaled down. The title figure is 150 px to the shoulders; scaled to this cell every
one of its passes (the dithered shades, the feathered beard, the per-column climb of light up the
staff) lands under a pixel and averages to mud. So each part was measured off hero.py in its own
units (u = h/32, feet on yb) and placed again at the size it lands at, keeping what the eye takes
first on the title, in its order:

  1. the staff, taller than he is: gold, two red bindings, a forked claw cradling the orb, and the
     orb in the LIGHT ramp (the splash's ward is E["light"]) - the brightest thing on him;
  2. the cone hat with its gold band and buckle and its tip swept over to the staff's side;
  3. the mane-spear beard: wide at the cheeks, held wide to mid-chest, then one long taper to a
     point at the sash, the moustache swept out and down to needles;
  4. the scalloped shoulder mantle over the robe - two garments, not one cone;
  5. the outer cloak swept OFF-AXIS, away from the staff, so nothing about him is mirrored;
  6. the red sash, the one warm pigment note on the cloth, showing either side of the beard's point;
  7. a hand on the staff, in flesh.

VALUE is the title's: mostly wool shade and deep, the cloak a stop darker than the robe, wool base only
on the planes turned to the key, the brim's shadow over the face so the face is a nose and a beard
coming out of the dark under the hat. What separates him from the ground is the bone rim on the key
edge (upper left) and the orb's light landing on what faces it - the cone's staff-side face (a line of
it, as on the title), the hook of the hat, the brim's tip, the mantle's shoulder and the hand. The orb
stands on the key side, so the bounce and the rim share that edge, as they do on the title. That bounce is drawn in the light ramp, so the
idle's pulse brightens it in step with the orb (roster.idle): a light that pulses while what it lights
holds still reads as a decal.

THE SASH is the `red` material row, which the roster palette never needed, so it is added here on
control characters (RED0..RED4), the way roster.py added darkness and light, and anim_sets lists it so
the hurt flash and the death fade step it along its own ramp. They are the bytes merlin_concepts.RED_RAMP
holds for the same row (\x14-\x18, already in anim_sets.PLAYER_RAMPS): bfada4df put red on \x0e-\x12,
and \x10-\x13 have been Wayland's metal since, so a hurt flash would have stepped the sash along the
hammer's ramp. Nothing new is spoken for; \x8b-\x8f stay free for him if he ever needs a row of his own.

HE FACES THE VIEWER, staff on the left of the sprite as on the title, so the player's left/right flip is
right for him (CharacterVisuals.SpriteFacesLeft false). His walk moves the hem, a toe at each contact
and the cloak (which drags a little behind the robe); the staff is planted on one step and carried on
the other, so it lifts off the ground and bobs.

Two limits from the derived animations (roster.py's): nothing above row 2 (the idle and the walk bob
the top up) and nothing left of column 4 (the hurt knocks the cell two left and shears the top two more).
"""
from __future__ import print_function

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import anim_sets
import bonelight as B
import pixel as P
import roster
import spriteframes

G = roster.G
CELL = roster.WIZARD_CELL
FEET = CELL - 2
STEM = "archmage"
SHEET = "ArchmageFrames"
OUT_CHARS = os.path.join("assets", "bonelight", "characters")

LT0, LT1, LT2, LT3 = roster.LT0, roster.LT1, roster.LT2, roster.LT3

# The `red` row, hi / lit / base / shade / deep. anim_sets.PLAYER_RAMPS carries the same five, and
# merlin_concepts.RED_RAMP names the same bytes for the same row (the docstring says why these).
RED0, RED1, RED2, RED3, RED4 = "\x14", "\x15", "\x16", "\x17", "\x18"
RED_RAMP = RED0 + RED1 + RED2 + RED3 + RED4
PALETTE = dict(roster.PALETTE)
PALETTE.update(dict(zip(RED_RAMP, B.MATERIALS["red"])))

# The body's middle line, and the orb. CharacterVisuals.GetStaffTipCell("archmage") is ORB: Magic Missile
# leaves his staff from there. The shaft is the two columns STAFF_X and STAFF_X + 1, so the orb, the
# fork and the motes are centred between them (hero.py: "the head's centre comes from the drawn
# columns, not from the float").
CX = 26
STAFF_X = 13
ORB = (13.5, 6.0)
GRIP = 28            # the row the hand closes round the shaft
SASH = 33


# --- the cloth ----------------------------------------------------------------------------------

def cloak(g):
    """The outer cloak, behind everything: wool deep, swept AWAY from the staff - it falls from the
    shoulders, hugs his left side and flares out to his right, so the silhouette is a wedge leaning
    off the vertical rather than a cone. The title shades it deep into occlusion; here it is deep with
    shade on the planes the key reaches and occlusion in its folds."""
    roster.mass(g, [(21, 21, 31), (24, 20, 32), (31, 18, 33), (36, 17, 35), (41, 17, 37),
                    (45, 17, 38), (46, 18, 38)], "a", "b", "o", rim=False)
    # The folds the sweep makes, running down and out toward the far hem (hero.py's three OCC lines).
    roster.fold(g, [(31, 28), (33, 36), (36, 45)], "o", "b")
    roster.fold(g, [(32, 31), (35, 38), (37, 44)], "o", None)
    roster.fold(g, [(19, 33), (18, 39), (18, 45)], "o", "b")
    # The heavy cloth's weave: a sparse, irregular break of shade into the deep, thicker toward the
    # lit edge, so the mass has an inside without banding.
    for y in range(27, FEET):
        for x in range(16, 40):
            if g.get(x, y) != "a":
                continue
            h = (x * 37 + y * 61 + (x * y) % 7) % 19
            if h < 2 + (1 if x < CX else 0):
                g.set(x, y, "b" if (x + y) % 3 else "o")
    # The hem goes into the ground: a wave, not a rule.
    for x in (18, 23, 24, 33, 34):
        roster.cut(g, (x, FEET))


def deepen(g, keys, chars, at):
    """hero.py shades every garment as one ramp across a diagonal - wool base at the upper left, shade,
    deep, occlusion at the lower right - which is why the title figure is mostly dark. `mass` only lights
    two edge columns, so this takes the same keyframes and pushes everything of `chars` past `at` along
    that diagonal down to wool deep."""
    y0, y1 = keys[0][0], keys[-1][0]
    for (ya, la, ra), (yb, lb, rb) in zip(keys, keys[1:]):
        for y in range(ya, yb + 1):
            t = (y - ya) / float(max(1, yb - ya))
            lo, hi = la + (lb - la) * t, ra + (rb - ra) * t
            for x in range(int(round(lo)), int(round(hi)) + 1):
                d = (x - lo) / max(1.0, hi - lo) + 0.35 * (y - y0) / float(max(1, y1 - y0))
                if g.get(x, y) in chars and d > at:
                    g.set(x, y, "a")


def robe(g, phase=None):
    """The robe between the cloak's edges: wool shade, lit on the key side, the folds the title draws
    from the hem up to the waist, and the occlusion band where the hem meets the floor."""
    keys = [(22, 21, 31), (34, 22, 30), (40, 20, 32), (45, 19, 33)]
    roster.mass(g, keys, "b", "c", "a", rim=False)
    deepen(g, keys, ("b",), 0.58)
    for (bx, tx) in ((-4.4, -1.8), (-2.0, -0.8), (1.0, 0.5), (3.6, 1.8), (5.4, 2.8)):
        g.line(CX + bx, 44, CX + tx, 35, "a")
    # A lit stop beside the two folds on the key side, so a crease is a pair of stops and not a line.
    for (x, y) in ((22, 42), (23, 39), (24, 37), (24, 43), (25, 40)):
        if g.get(x, y) == "b":
            g.set(x, y, "c")
    # The hem: a shallow wave, and occlusion under it where the robe meets the floor.
    for x in range(19, 34):
        if (x * 5) % 7 < 2:
            g.set(x, 45, "a")
        g.set(x, FEET, "o")
    if phase is not None:
        # A stately walk, as the Frostweaver's and Dee's: the hem swings a pixel or two toward the
        # leading foot and the toe of that foot shows under it at each contact.
        c, _ln, _lf = roster.stride(phase)
        hem = 1.4 * c
        roster.sway(g, 37, hem, FEET)
        if abs(c) > 0.5:
            roster.toe(g, (28 if c > 0 else 20) + int(round(hem)), w=3, shoe=("N", "j", "o"))


def sash(g):
    """The red sash at the waist - the one warm pigment note on the cloth, as on the title - with its
    knot's tail hanging on the far side. The beard's point covers its middle."""
    g.span(SASH, 21, 31, RED2)
    g.span(SASH, 21, 24, RED1)
    g.span(SASH + 1, 21, 31, RED3)
    g.set(31, SASH + 1, RED4)
    g.span(SASH + 2, 21, 31, "o")
    # the knot's tail, down the right hip
    g.line(29, SASH + 2, 30, SASH + 5, RED3)
    g.set(29, SASH + 2, RED2)
    g.set(30, SASH + 5, RED4)


def arm(g, lift=0):
    """The staff arm: out from under the mantle, down to the elbow at the cloak's edge and back up to
    the grip - a joint, not a plank. Wool deep with a shade top, the flared cuff a stop lighter."""
    gy = GRIP - lift
    roster.shaft(g, 21, 25, 19, 31, "b", "a", "o", width=2)
    roster.shaft(g, 19, 31, 17, gy + 1, "b", "a", "o", width=2)
    # the cuff, flared across the end of the forearm
    g.span(gy, 16, 17, "c")
    g.span(gy + 1, 16, 18, "b")
    g.span(gy + 2, 16, 17, "a")
    g.set(18, gy + 2, "o")


def mantle(g):
    """The shoulder mantle over the robe: wool shade, lit upper left, its hem SCALLOPED - seven
    points alternating a pixel and a half deep, with occlusion under each, as hero.py cuts it."""
    keys = [(18, 23, 29), (21, 20, 32), (25, 19, 33), (30, 18, 34)]
    roster.mass(g, keys, "b", "c", "a", rim=False)
    deepen(g, keys, ("b",), 0.72)
    # the scallops: the odd points hang a row lower, the even ones are cut back a row higher
    pts = [18 + 16 * i / 6.0 for i in range(7)]
    for i, px in enumerate(pts):
        x = int(round(px))
        if i % 2:
            g.span(31, x - 1, x + 1, "a")
            g.set(x - 1, 31, "b")
            g.span(32, x - 1, x + 1, "o")
        else:
            roster.cut(g, (x, 30))
            g.set(x, 31, "o")
    # a fold off each shoulder, and the shoulder's crown turned to the key
    roster.fold(g, [(21, 23), (20, 29)], "a", "c")
    roster.fold(g, [(31, 23), (32, 29)], "a", None)
    g.span(20, 22, 24, "c")
    g.set(21, 21, "c")


# --- the head -----------------------------------------------------------------------------------

def face(g):
    """The brim's shadow and what comes out of it: a nose catching the light, a moustache swept out
    and down to needles, and the beard (hero._b_mane_spear) - held wide to mid-chest, then one long
    taper to a point at the sash. No eyes: on the title the brim swallows them, and two dark specks
    under it read as a mask. The cheeks are there, in the deep end of flesh, so the shadow is a face
    in shadow and not a hole."""
    # the hood's dark: under the brim, a band of occlusion fading to wool deep at its foot
    for y, (lo, hi) in ((18, (21, 31)), (19, (22, 30)), (20, (22, 30))):
        g.span(y, lo, hi, "o")
    for x in range(22, 31):
        if (x + 22) % 3 == 0:
            g.set(x, 20, "a")
    g.set(23, 19, "I")
    g.set(29, 19, "I")
    g.span(20, 23, 25, "I")
    g.span(20, 27, 29, "I")
    g.set(24, 20, "i")
    # the nose: one pixel in shadow under the brim, the tip catching the light
    g.set(CX, 19, "i")
    g.set(CX, 20, "h")
    g.set(CX - 1, 20, "f")

    # THE BEARD. hero._b_mane_spear's half-width profile over its own rows (22.35u..10.2u above the
    # feet, here rows 21..36, to just past the sash), with the stops placed rather than dithered.
    # Held a little narrower than the title's against its length: "width has a ceiling and the
    # ceiling is the length" (hero.py), and on this cell a beard as wide as the title's is a ball.
    prof = [(0.00, 2.8), (0.08, 3.9), (0.20, 4.5), (0.34, 4.7), (0.48, 4.45), (0.62, 3.9),
            (0.74, 3.1), (0.85, 2.2), (0.94, 1.2), (1.00, 0.4)]
    y0, y1 = 21, 36
    for y in range(y0, y1 + 1):
        t = (y - y0) / float(y1 - y0)
        w = hero_lerp(prof, t)
        c = CX + 0.15 * t
        lo, hi = int(round(c - w)), int(round(c + w - 0.2))
        # The title's beard is one ramp from base at the upper left to deep at the lower right
        # (bone k, n, m); only its key-side edge reaches the lit stop.
        for x in range(lo, hi + 1):
            d = (x - c) / max(1.0, w) + 1.4 * t
            g.set(x, y, "k" if d < 0.75 else ("n" if d < 1.55 else "m"))
        g.set(lo, y, "s" if t < 0.8 else "k")
        if hi - lo >= 2:
            g.set(hi, y, "m")
    # Streaks: short runs down the mass, irregular - hair, not a bib.
    for (x, ya, yb_, ch) in ((24, 24, 27, "n"), (26, 25, 29, "n"), (28, 23, 25, "n"), (27, 30, 33, "m"),
                             (23, 27, 29, "s"), (25, 31, 34, "n"), (29, 26, 28, "m"), (22, 25, 26, "s"),
                             (30, 23, 24, "n"), (24, 30, 31, "k"), (28, 29, 30, "k")):
        for y in range(ya, yb_ + 1):
            if g.get(x, y) in ("k", "s", "n", "m"):
                g.set(x, y, ch)
    # Its edge breaks up into flicks rather than a ruled line (hero.py: "hair has an edge that
    # breaks up"): a few pixels a stop darker just outside the outline.
    for (x, y, ch) in ((21, 26, "n"), (31, 27, "m"), (21, 29, "n"), (31, 30, "m"), (22, 32, "n"),
                       (30, 33, "m")):
        if g.get(x, y) not in (".",):
            g.set(x, y, ch)
    # THE MOUSTACHE: out from under the nose and DOWN to needles either side, in the beard's values
    # with occlusion under it so it lies over the beard rather than in it.
    my = 21
    for sgn in (-1, 1):
        far = "s" if sgn < 0 else "n"
        g.set(CX + sgn * 1, my, "k")
        g.set(CX + sgn * 2, my, far if sgn < 0 else "k")
        g.set(CX + sgn * 3, my + 1, far)
        g.set(CX + sgn * 4, my + 1, "k" if sgn < 0 else "n")
        g.set(CX + sgn * 5, my + 2, "s" if sgn < 0 else "n")
        g.set(CX + sgn * 6, my + 3, "k" if sgn < 0 else "m")
        g.set(CX + sgn * 2, my + 1, "o")
        g.set(CX + sgn * 3, my + 2, "o" if sgn < 0 else "m")
    g.set(CX, my, "o")
    g.set(CX - 1, my, "S")


def hero_lerp(profile, t):
    t = min(1.0, max(0.0, t))
    for i in range(len(profile) - 1):
        (t0, w0), (t1, w1) = profile[i], profile[i + 1]
        if t <= t1:
            return w0 + (w1 - w0) * (t - t0) / (t1 - t0)
    return profile[-1][1]


def hat(g):
    """The cone hat: a wide brim (inside his shoulders, the proportion rule), a dark band with the
    gold buckle, and the cone leaning back with its tip swept over toward the staff, a beak at the top
    as hero.py draws it. Wool, base on the planes turned to the key, deep on the far side."""
    # THE BRIM: a shallow lens, its top lit on the key side, its underside the hood's dark.
    for (y, lo, hi) in ((15, 21, 31), (16, 20, 32), (17, 19, 33), (18, 20, 32)):
        g.span(y, lo, hi, "b")
    g.span(15, 21, 24, "c")
    g.span(16, 20, 22, "c")
    g.span(17, 30, 33, "a")
    g.span(16, 31, 32, "a")
    g.span(18, 20, 32, "o")
    g.set(20, 18, "a")
    g.set(32, 18, "a")
    g.span(17, 23, 29, "a")         # where the brim turns down over the brow
    # THE CONE: (y, left, right), leaning back, the tip a beak pointing at the staff. hero.py's cone
    # stands 7.5u over the brim against 34.8u of figure, so it takes a fifth of his height here too:
    # any more and the hat is the subject, which is the mascot.
    for (y, lo, hi) in ((13, 22, 30), (12, 23, 29), (11, 23, 28), (10, 24, 28), (9, 24, 27),
                        (8, 23, 26), (7, 21, 25)):
        g.span(y, lo, hi, "b")
        g.set(lo, y, "c")
        if hi - lo >= 3:
            g.set(lo + 1, y, "c" if y > 8 else "b")
        g.set(hi, y, "a")
        if hi - lo >= 5:
            g.set(hi - 1, y, "a")
    g.set(20, 7, "b")               # the beak's point
    g.span(6, 22, 24, "b")
    g.set(22, 6, "c")
    g.set(24, 6, "a")
    # a crease where the cone bends back, and one down its far side
    roster.fold(g, [(25, 8), (27, 10)], "a", "c", side=(0, -1))
    roster.fold(g, [(27, 11), (28, 13)], "a", None)
    # THE BAND and the buckle (hero.py: a gold rect on the band, its lit core up and left).
    g.span(14, 22, 30, "a")
    g.set(22, 14, "b")
    g.rect(25, 13, 26, 14, "g")
    g.set(25, 13, "G")
    g.set(26, 14, "u")


# --- the light ----------------------------------------------------------------------------------

def rim_and_bounce(g):
    """The two edge passes, on the figure only, before the staff (hero.py: run after the staff they
    rim the staff, and it reads as a glowing rod). The bone rim on every row's key-side edge; then the
    orb's light where the figure faces it - a few stops of the light ramp just inside that edge, on
    what is nearest the orb."""
    for y in range(4, FEET):
        row = g.g[y]
        for x in range(1, CELL):
            if row[x] != "." and row[x - 1] == ".":
                # ...but never on the hood's dark: a shadow with a bone rim on it stops being a
                # shadow and becomes a black object (hero._hood).
                if row[x] != "o":
                    g.set(x - 1, y, "W")
                break
    # The bounce: the cone's staff-side face - on the title a line of the orb's light all the way up
    # it - the beak, the brim's tip, the mantle's shoulder and the cloak below it.
    # Unbroken: dashed, it read as stars sewn on the hat.
    for y in range(8, 14):
        row = g.g[y]
        for x in range(1, CELL):
            if row[x] not in (".", "W"):
                g.set(x, y, LT3)
                break
    for (x, y, ch) in ((19, 17, LT3), (20, 16, LT3), (20, 7, LT3),
                       (20, 21, LT3), (19, 25, LT3), (18, 30, LT3)):
        if g.get(x, y) not in (".", "W"):
            g.set(x, y, ch)


def staff(g, lift=0):
    """The staff, taller than he is: gold, lit on the key side and brighter toward the orb, two red
    bindings clear of the hand, and the forked claw that grows out of the shaft's sides and cradles
    the orb. `lift` carries it off the floor on the walk."""
    oy = int(ORB[1]) - lift
    x0 = STAFF_X
    bot = FEET - lift
    # THE SHAFT runs up into the claw's seat - no seam between handle and head (hero.py). Brightest
    # toward the orb, the climb staggered per column so it never draws a rule across the gold.
    for y in range(oy + 3, bot + 1):
        g.set(x0, y, "H" if y < oy + 6 else ("G" if y < 40 or y % 3 else "g"))
        g.set(x0 + 1, y, "G" if y < oy + 8 else ("g" if y < 41 else "u"))
    g.set(x0 + 1, bot, "u")
    # the two red bindings, clear of the hand either side of it
    for by in (21 - lift, 36 - lift):
        g.span(by, x0 - 1, x0 + 2, RED2)
        g.set(x0 - 1, by, RED1)
        g.span(by + 1, x0 - 1, x0 + 2, RED3)
        g.set(x0 + 2, by + 1, RED4)
    # THE FORK: a cup out of the shaft's sides, two legs up round the orb's flanks, their tips
    # turned in over its shoulders - a claw holding it, not a ball on a stick. Lit on the key side.
    g.span(oy + 3, x0 - 1, x0 + 2, "g")
    g.set(x0 - 1, oy + 3, "G")
    g.set(x0 + 2, oy + 3, "u")
    for y in range(oy - 1, oy + 3):
        g.set(x0 - 2, y, "G" if y < oy + 2 else "g")
        g.set(x0 + 3, y, "g" if y < oy + 1 else "u")
    g.set(x0 - 2, oy - 1, "H")
    g.set(x0 - 1, oy - 2, "G")              # the tips, turned in
    g.set(x0 + 2, oy - 2, "g")
    g.set(x0 - 1, oy + 2, "u")              # the seat under the orb, in the orb's shadow
    g.set(x0 + 2, oy + 2, "u")
    # THE ORB: the light ramp, four across and five high, its core up and to the left.
    for (y, lo, hi) in ((oy - 2, x0, x0 + 1), (oy - 1, x0 - 1, x0 + 2), (oy, x0 - 1, x0 + 2),
                        (oy + 1, x0 - 1, x0 + 2), (oy + 2, x0, x0 + 1)):
        g.span(y, lo, hi, LT2)
    g.span(oy - 1, x0 - 1, x0 + 1, LT1)
    g.span(oy, x0 - 1, x0, LT1)
    g.set(x0, oy - 1, LT0)
    g.set(x0, oy - 2, LT1)
    g.set(x0 + 2, oy + 1, LT3)
    g.set(x0 + 1, oy + 2, LT3)
    # The halo: a few points of the edge stop just clear of the claw - a glow is the fade, the
    # solid is the lamp - and motes thrown clear of it, never in it.
    for (x, y, ch) in ((x0 - 3, oy - 2, LT3), (x0 + 4, oy - 1, LT3), (x0 + 1, oy - 4, LT3),
                       (x0 - 3, oy + 2, LT3), (x0 - 5, oy - 1, LT2), (x0 + 5, oy - 3, LT1),
                       (x0 - 2, oy - 4, LT2)):
        if g.get(x, y) == ".":
            g.set(x, y, ch)


def hand(g, lift=0):
    """The hand closed on the shaft: the back of it behind, and the fingertips that come round the
    near side over the gold - in flesh, the warm row, so it is a hand and not a gauntlet."""
    gy = GRIP - lift
    g.rect(14, gy, 15, gy + 2, "i")
    g.set(15, gy, "h")
    g.set(14, gy - 1, "h")
    for k, (x, ch) in enumerate(((12, "f"), (12, "h"), (12, "h"))):
        g.set(x, gy + k, ch)
        g.set(x + 1, gy + k, "h" if k else "f")
        g.set(x - 1, gy + k, "o" if k else ".")
    g.set(13, gy + 3, "I")
    g.set(12, gy + 2, "i")
    # the thumb across the fingers, and the orb's light on the knuckle
    g.set(14, gy, "f")
    g.set(12, gy, LT3)


# --- the figure ---------------------------------------------------------------------------------

def archmage(phase=None):
    """Magic Missile. The title screen's wizard: the one the dark wizard missed. `phase` (0..1) is the
    walk; None is the pose the idle and the portrait are built from."""
    g = G(CELL)
    lift = 0
    if phase is not None:
        # The staff is planted on one step and carried through the other.
        lift = 1 if math.sin(2.0 * math.pi * phase) > 0.3 else 0
    cloak(g)
    if phase is not None:
        # The cloak drags: it swings a quarter-step behind the robe, more toward its hem.
        roster.sway(g, 34, 1.6 * math.sin(2.0 * math.pi * phase - 1.2), FEET)
    robe(g, phase)
    sash(g)
    arm(g, lift)
    mantle(g)
    face(g)
    hat(g)
    rim_and_bounce(g)
    staff(g, lift)
    hand(g, lift)
    roster.contact(g, 11, 39)
    return g.rows()


def build(root=ROOT):
    pose = archmage()
    idle = roster.idle(pose)
    problems = P.check_anchor("%s pose" % STEM, [pose], CELL)
    P.write_frames(idle, PALETTE, os.path.join(root, OUT_CHARS, STEM + "-%d.png"), CELL)
    anims = anim_sets.player_set(pose, idle, archmage)
    spec = []
    for anim, frames, loop, speed in anims:
        png = "%s-%s.png" % (STEM, anim)
        P.write_strip(frames, PALETTE, os.path.join(root, OUT_CHARS, png), CELL)
        spec.append((anim, png, len(frames), loop, speed))
        if anim in ("idle", "moving"):
            problems += P.check_anchor("%s %s" % (STEM, anim), frames, CELL)
    spriteframes.write(os.path.join(root, "scenes", "resources", SHEET + ".tres"), spec,
                       OUT_CHARS.replace(os.sep, "/"), cell=CELL)
    return pose, anims, problems


def orb_centre(rows):
    """The centroid of the orb's own pixels (the light ramp's core three stops inside the fork) -
    what CharacterVisuals.GetStaffTipCell is measured as."""
    pts = [(x, y) for y, row in enumerate(rows) for x, ch in enumerate(row)
           if ch in (LT0, LT1, LT2) and abs(x - ORB[0]) <= 3 and abs(y - ORB[1]) <= 3]
    n = float(len(pts))
    return sum(p[0] for p in pts) / n, sum(p[1] for p in pts) / n


def preview(pose, anims):
    black = ["".join("o" if ch != "." else "." for ch in row) for row in pose]
    sets = [("pose", [pose]), ("silhouette", [black])]
    for anim, frames, _loop, _speed in anims:
        sets.append((anim, frames))
    P.preview(sets, dict(PALETTE, o="#05070C"), os.path.join(HERE, "_player_archmage.png"),
              zoom=8, bg=(30, 34, 46), cell=CELL)


def main():
    if "--preview" in sys.argv:
        pose = archmage()
        anims = anim_sets.player_set(pose, roster.idle(pose), archmage)
        problems = P.check_anchor("%s pose" % STEM, [pose], CELL)
    else:
        pose, anims, problems = build()
    filled, edges, ratio = roster.density(pose)
    ox, oy = orb_centre(pose)
    print("%s  %dx%d  %d px  %.2f edges/px%s  orb centre (%.1f, %.1f)  -> scenes/resources/%s.tres"
          % (STEM, CELL, CELL, filled, ratio, "" if ratio >= 0.80 else "   SPARSE", ox, oy, SHEET))
    preview(pose, anims)
    if problems:
        print("ANCHOR PROBLEMS:")
        for p in problems:
            print("   " + p)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
