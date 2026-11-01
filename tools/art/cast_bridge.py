# -*- coding: utf-8 -*-
"""The Last Bridge's own cast (.ai/endgame-castle.md 3.5). Run: python tools/art/cast_bridge.py

"His seat is across the water." The gate's garrison: the soldiers who held the crossing, their dogs,
the engines they kept at the gate - taken, all of them, so all of them stare with his cold arcane -
and the two things on the bridge that were MADE (the gargoyles on the parapet, the barrels of
pitch), which carry his fire instead, the carve-out .ai/world-and-tone.md allows. Eleven creatures,
each a new look on the numbers of the Emberdeep creature it replaces; the module contract is
bestiary_kit.py's docstring.

NOTHING ECHOES THE CONSTABLE. The boss is a tall armoured figure with a ring of keys and a halberd,
so no creature here is tall AND plated AND carries a polearm with a blade: the pikeman is short and
quilted and his pike is a bare point; the knight is plated but crouched, and his lance is level;
nobody carries a ring, and the Porter's windlass key is a crank - a bent iron bar with a socket and
a grip - so it reads as the gate's machinery and never as a jailer's keys.

THE FLOOR DECIDES THE STOPS. The deck is stone's shade (terrain_bridge.py), the Emberdeep's floor
again, so the bodies here take the Emberdeep's lesson (cast_ember.py): every mass sits at least a
stop above its ramp's shade, with the deep stops kept for crevices and the line under a plate.

ROWS BY JOB, so the crowd separates by material as well as by shape (.ai/bestiary.md section 2):
wool for the pikemen, the most numerous (quilted jacks); linen for the pavise and the warder's
padded coat; flesh for the dog, the one living animal; gold's lower half for wood (the ram's roof,
the pitch barrel's staves, every shaft); violet for the crossbowman; bone for the carved gargoyle;
steel for the knight and for every helm; stone for the houndmaster's cloak; red only as old leather
and cloth, well under the 15% world-and-tone.md allows. The four the table rolls together - the
pikeman, the hound, the pavisier and the ram - are blue, brown-mauve, grey-green and wood: no two
share a row.

ONE ATTACHMENT EACH, no two alike on the bridge, and each survives being filled black: a pike's
point standing over a crescent-brimmed helm; a spiked collar on a long low dog; a standing shield
taller than the man behind it; a roof wider than anything on the deck, on legs; a bolt floating over
a crossbow held level; crouched wings over a block of parapet; a lance level and forward from a
crouch; a barrel on its side with a lit fuse rising from its bung; a maul's head over a broad guard;
a pole raised high with leashes hanging from it; and, on the 48 cell, a crank of iron rising off a
stooped back.

THE CHAPTER'S BOLT. Section 6 of .ai/bestiary.md gives no element to the castle chapters. The
crossbowman's floating bolt, and so its bolt, is his cold fire - the `arcane` element ramp, the light
the Court's sconces burn (court_walls.py: "it is his house") - mid #C08CFF round a #FFF0FF core, the
Mystic Ruins' pair. The gargoyle is a turret and throws fire, as every turret does.

THE PALETTE CHARACTERS. As cast_fields.py does: the player-pigment amber characters carry gold's whole
five-stop row (the roster's gold characters stop at shade, and wood needs the deep stop), and the ember
characters carry cloth red. anim_sets steps a pixel along its own character's ramp, so a hurt flash or
a death fade still walks each redefined row in the right order.
"""
from __future__ import print_function

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import bestiary_kit
import bonelight as B
import roster
import sprite_bosses as SB
from cast_sands import blob
from cast_ruins import poly
from foes_forest import _limb

G = roster.G
mass = roster.mass
fold = roster.fold
cut = roster.cut
eyes = roster.eyes
contact = roster.contact

FEET = 30

PALETTE = dict(roster.PALETTE)
for _ch, _hex in zip("O!@#$", B.MATERIALS["gold"]):
    PALETTE[_ch] = _hex
for _ch, _hex in zip("Q1234", B.MATERIALS["red"]):
    PALETTE[_ch] = _hex

GOLD_HI, GOLD_LIT, GOLD, GOLD_SH, GOLD_DP = "O", "!", "@", "#", "$"
RED_HI, RED_LIT, RED, RED_SH, RED_DP = "Q", "1", "2", "3", "4"
# Wood: the gold row's lower half, lit on its base stop. Never its hi or lit as a plane.
WOOD, WOOD_LIT, WOOD_DK = GOLD_SH, GOLD, GOLD_DP


def taken(g, y, x):
    """THE mark on everything he took: two pixels of cold arcane, hot nearer the key."""
    eyes(g, y, x, x + 1, "Y", "y")


def built(g, x, y):
    """The mark on a thing he made: two pixels of fire, core and hot."""
    g.set(x, y, "+")
    g.set(x + 1, y, "-")


def wood_shaft(g, x, y0, y1):
    """A vertical haft two pixels wide: lit on the key side, shaded on the far one, with a grain
    knot or two so a long pole is not one flat stripe."""
    for y in range(int(y0), int(y1) + 1):
        g.set(x, y, WOOD_LIT)
        g.set(x + 1, y, WOOD)
    for y in range(int(y0) + 3, int(y1), 7):
        g.set(x + 1, y, WOOD_DK)


def boot(g, x0, x1, y0=27):
    """A leather boot, lit on its instep, the sole dark."""
    g.rect(x0, y0, x1, FEET, WOOD)
    g.span(y0, x0, x1, WOOD_LIT)
    g.set(x0, y0 + 1, WOOD_LIT)
    g.span(FEET, x0, x1, WOOD_DK)


# =================================================================================================
# BULK - Gate Pikeman. A taken soldier of the gate, the pike upright, its head above the helm.
# =================================================================================================

def pikeman():
    """Short, quilted, and standing to his pike.

    The attachment is the PIKE: a bare leaf point on a long haft held upright at his side, so filled
    black he is a short figure with a line rising high over him and a point at its top. Under it a
    morion - a round crown, the brim a crescent turned up at both ends, wider than his shoulders -
    which no other helm on the bridge has, and a padded jack in wool, quilted in uneven channels. He
    is short and unplated on purpose: the Constable is the tall armoured one with a polearm.

    The first pass gave the crown a comb on a tapered bowl, and with the brim under it the helm was
    a witch's hat. The crown is a dome now, as wide at the top as at the brim.
    """
    g = G(32)
    # LEGS: hose in wool, a short stride, boots
    for (x0, x1, key) in ((11, 12, "c"), (16, 17, "b")):
        g.rect(x0, 25, x1, 27, "b")
        g.line(x0, 25, x0, 27, key)
        g.line(x1, 25, x1, 27, "a")
        boot(g, x0 - (1 if x0 < 14 else 0), x1 + (0 if x0 < 14 else 1), 28)
    # THE JACK: padded, flaring to a skirt over the hips
    mass(g, [(16, 10, 19), (19, 10, 19), (23, 9, 20), (26, 9, 20)], "b", "c", "a")
    # quilting: uneven channels, each a crease with its lit side
    fold(g, [(12, 17), (11, 25)], "a", "c")
    fold(g, [(15, 17), (15, 25)], "a", "c")
    fold(g, [(18, 18), (18, 25)], "a", None)
    for (x, y) in ((13, 18), (14, 21), (13, 24), (16, 19), (17, 23), (10, 22)):
        g.set(x, y, "d")
    g.span(26, 9, 20, "a")
    cut(g, (14, 26), (13, 26))
    # the belt, a buckle, and a scabbard hanging behind the far hip
    g.span(21, 10, 19, RED_SH)
    g.set(10, 21, RED)
    g.set(15, 21, GOLD_LIT)
    g.line(9, 22, 7, 27, RED_SH)
    g.line(8, 22, 6, 27, RED)
    g.set(9, 21, GOLD)
    g.set(7, 21, GOLD)
    g.set(8, 21, GOLD_SH)
    # THE NEAR ARM down the side and forward to the haft
    g.rect(18, 17, 19, 20, "b")
    g.line(18, 17, 18, 20, "c")
    g.line(19, 21, 23, 19, "b")
    g.line(19, 20, 23, 18, "c")
    g.set(20, 21, "a")
    # THE PIKE: a long haft held upright at his side, its butt on the deck
    wood_shaft(g, 24, 7, FEET)
    g.set(24, FEET, "q")
    g.set(25, FEET, "q")
    # the head: a bare leaf point, socket and langets - no blade, no hook
    for (y, a, b) in ((2, 24, 24), (3, 24, 25), (4, 23, 25), (5, 23, 26), (6, 24, 25)):
        g.span(y, a, b, "r")
        g.set(a, y, "t")
        g.set(b, y, "q" if b > a else "t")
    g.line(24, 7, 24, 9, "r")
    g.line(25, 7, 25, 9, "q")
    # hands: the near one gripping at the chest, the far one lower on the haft
    g.rect(23, 18, 24, 19, GOLD)
    g.set(23, 18, GOLD_LIT)
    g.set(24, 19, GOLD_SH)
    g.rect(23, 23, 24, 24, GOLD_SH)
    g.set(23, 23, GOLD)
    # THE MORION: a dome, a short stalk on top, the brim a crescent turned up at both ends
    for (y, a, b) in ((8, 12, 17), (9, 11, 18), (10, 11, 18), (11, 11, 18), (12, 11, 18)):
        g.span(y, a, b, "R")
        g.set(a, y, "r")
        g.set(b, y, "q")
    g.span(8, 13, 15, "r")
    g.set(12, 9, "t")
    g.set(13, 8, "t")
    fold(g, [(15, 9), (16, 12)], "q", "r")
    g.set(14, 7, "r")
    g.set(15, 7, "q")
    # the brim: wider than his shoulders, its points turned up front and back
    g.span(13, 8, 21, "r")
    g.span(14, 10, 19, "q")
    g.set(8, 13, "t")
    g.set(7, 12, "t")
    g.set(6, 11, "r")
    g.set(21, 13, "R")
    g.set(22, 12, "R")
    g.set(23, 11, "q")
    # the face: shadow under the brim
    g.span(15, 12, 17, "o")
    g.span(16, 13, 16, "o")
    taken(g, 15, 14)
    # a gorget of padding under the chin
    g.span(17, 12, 17, "c")
    g.set(17, 17, "a")
    contact(g, 6, 25)
    return g.rows()


# =================================================================================================
# FAST - Bridge Hound. A gaunt war-dog; also the Houndmaster's minion.
# =================================================================================================

def hound():
    """A gaunt war-dog, long and low, head level with its back, in a spiked collar.

    Built as foes_forest.wolf and the Chainhound say an animal has to be: masses first - a narrow
    haunch, a deep chest, the waist tucked up hard between them because it is starved - thin legs
    paired under each mass, the hind ones jointed backward at the hock. The ribs show and its coat is
    brindled. The COLLAR is the attachment: a broad steel band with spikes standing out of the neck's
    outline above and below, so filled black the dog's neck bristles. Flesh, the one living animal on
    the bridge, so it is the one warm body in a crowd of wool and linen and steel.

    The first pass stood it on long legs with its ears up and its head high, and it was a deer. The
    legs are shorter now, the head carried level and forward, the ears laid back.
    """
    g = G(32)
    mid, lit, dark, hi = "i", "h", "I", "f"
    # LEGS: thin and jointed - the hind pair stifle forward, hock back; the fore pair straight
    for (pts, th) in (([(8, 21), (9, 24), (6, 27), (6, FEET - 1)], 0.9),
                      ([(10, 21), (11, 24), (8, 27), (9, FEET - 1)], 1.0),
                      ([(23, 22), (24, 26), (25, FEET - 1)], 0.9),
                      ([(20, 22), (20, 26), (21, FEET - 1)], 1.0)):
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            _limb(g, x0, y0, x1, y1, th, mid, mid, dark)
        fx = pts[-1][0]
        g.span(FEET, fx - 1, fx + 1, dark)
        g.set(fx - 1, FEET, mid)
    # THE MASSES: a narrow haunch, a deep chest, the waist tucked up between
    blob(g, [(9.5, 19.5, 2.9), (8.5, 21.5, 2.2)], mid, lit, dark)
    blob(g, [(20.0, 19.5, 3.3), (21.5, 18.5, 2.8), (20.5, 22.0, 2.3)], mid, lit, dark)
    for x in range(11, 19):
        t = (x - 11) / 7.0
        top = 17.2 + 0.5 * math.sin(t * math.pi)
        bot = 20.0 - 0.8 * math.sin(t * math.pi) + 1.4 * t
        for y in range(int(round(top)), int(round(bot)) + 1):
            g.set(x, y, mid)
        g.set(x, int(round(top)), lit)
        g.set(x, int(round(bot)), dark)
    # the ribs over the chest, lit ridges over dark hollows, uneven; brindle streaks on the haunch
    for (x, y0, y1) in ((15, 18, 19), (17, 18, 20), (19, 19, 21)):
        g.line(x, y0, x, y1, dark)
        g.set(x - 1, y0 + 1, lit)
    for (x0, y0, x1, y1) in ((8, 18, 7, 20), (10, 19, 10, 21), (12, 18, 12, 19), (22, 20, 21, 22)):
        g.line(x0, y0, x1, y1, dark)
    for (x, y) in ((9, 17), (13, 17), (18, 16), (20, 16)):
        g.set(x, y, hi)
    # the tail: thin, hanging low behind
    g.line(6, 19, 4, 22, mid)
    g.line(4, 22, 4, 25, mid)
    g.line(5, 19, 3, 22, lit)
    g.set(4, 26, dark)
    # NECK, forward and a little up, and the long head carried level
    for i, x in enumerate(range(22, 26)):
        top = 16.0 - i * 0.6
        for y in range(int(round(top)), int(round(top)) + 4):
            g.set(x, y, mid)
        g.set(x, int(round(top)), lit)
        g.set(x, int(round(top)) + 4, dark)
    blob(g, [(26.5, 15.0, 2.2)], mid, lit, dark, rim=False)
    # the muzzle: long and narrow, the jaw under it, the mouth a dark line
    g.rect(28, 14, 30, 16, mid)
    g.span(14, 28, 30, lit)
    g.set(31, 15, dark)
    g.set(31, 14, mid)
    g.span(17, 26, 29, dark)
    g.set(29, 16, "o")
    g.set(28, 16, "o")
    g.set(30, 16, dark)
    # ears: laid back along the skull
    g.set(25, 12, lit)
    g.set(24, 12, mid)
    g.set(23, 13, dark)
    g.set(25, 13, dark)
    # THE COLLAR: a broad steel band at the throat, spikes standing out of the outline
    for y in range(13, 21):
        for x in (23, 24):
            if g.get(x, y) not in (".", "W"):
                g.set(x, y, "R")
    for y in range(13, 21):
        if g.get(23, y) == "R":
            g.set(23, y, "r")
    for (x, y, ch) in ((22, 12, "t"), (23, 11, "r"), (24, 12, "t"), (25, 11, "q"),
                       (23, 21, "q"), (24, 22, "R"), (22, 21, "q"), (21, 13, "r")):
        g.set(x, y, ch)
    SB.rivets(g, [(24, 16)], "t", "q")
    taken(g, 14, 26)
    contact(g, 4, 26)
    return g.rows()


# =================================================================================================
# HEAVY - Pavisier. A soldier behind a standing shield.
# =================================================================================================

def pavisier():
    """A pavise taller than the man behind it, and the man crouched behind it.

    The pavise is the attachment: a tall board, its top arched, a raised spine down its middle, rimmed
    in iron and faced in canvas and gesso gone grey-green - linen - with no device on it (his house has
    no arms), its wood showing where the facing has chipped. The soldier shows only round its key
    edge: a kettle helm, a shoulder, the hand on the board, two braced legs. The board stands clear
    above his helm, which is what says pavise rather than a shield carried.

    The first pass stood him beside a plain board and read as a man next to a door; he is behind it
    now, half hidden, and the board has an arched top and an iron rim.
    """
    g = G(32)
    # the soldier's legs, braced, the far one back
    for (x0, x1, key) in ((6, 8, "r"), (10, 12, "R")):
        g.rect(x0, 23, x1, 26, "R")
        g.line(x0, 23, x0, 26, key)
        g.line(x1, 23, x1, 26, "q")
        boot(g, x0 - (1 if x0 < 9 else 0), x1, 27)
    # the body behind the board, leaning into it: a brigandine, plates riveted over cloth
    mass(g, [(14, 7, 14), (18, 5, 15), (23, 6, 15)], "R", "r", "q")
    SB.rivets(g, [(7, 16), (10, 15), (8, 19), (11, 20)], "t", "q")
    fold(g, [(9, 15), (9, 22)], "q", "r")
    g.span(22, 6, 14, RED_SH)
    g.span(21, 6, 14, RED)
    g.set(6, 21, RED_LIT)
    # the red sleeve, reaching to the board's edge, and the hand on it
    g.line(11, 15, 13, 17, RED)
    g.line(11, 16, 13, 18, RED_SH)
    g.set(11, 14, RED_LIT)
    # THE KETTLE HELM: a round bowl with a level brim, peering past the board's edge
    blob(g, [(10.0, 10.5, 2.7)], "R", "r", "q")
    g.span(13, 6, 14, "r")
    g.span(14, 7, 14, "q")
    g.set(6, 13, "t")
    g.set(9, 8, "t")
    g.span(15, 9, 13, "o")
    taken(g, 15, 11)
    # THE PAVISE: tall, a little wider at the top, the top arched
    for y in range(3, 30):
        t = (y - 5) / 24.0
        if y == 3:
            a, b = 17, 23
        elif y == 4:
            a, b = 15, 25
        else:
            a = int(round(13 + 1.0 * t))
            b = int(round(27 - 1.0 * t))
        g.span(y, a, b, "e")
        # the far half of the board, past the spine, turned away from the key
        if y >= 5:
            g.span(y, 22, b - 1, "E")
        # the iron rim, lit on the key side
        g.set(a, y, "r" if y < 18 else "R")
        g.set(b, y, "q")
        g.set(a + 1, y, "l")
        g.set(b - 1, y, "E")
    g.span(3, 17, 23, "r")
    g.set(17, 3, "t")
    g.set(16, 4, "r")
    g.set(15, 4, "t")
    g.span(4, 17, 23, "l")
    g.set(24, 4, "q")
    g.set(25, 4, "q")
    g.set(26, 5, "q")
    g.span(29, 14, 26, "q")
    g.span(28, 15, 25, "A")
    # the spine: a raised channel down the middle, lit edge and shaded edge
    for y in range(5, 28):
        g.set(19, y, "L" if y < 8 else "l")
        g.set(20, y, "E")
        g.set(21, y, "A" if y % 5 else "E")
    # the facing chipped, the wood showing through; wear in the gesso so the plane is not flat
    for (x, y, ch) in ((24, 6, WOOD), (25, 7, WOOD_DK), (23, 6, WOOD_LIT), (16, 26, WOOD),
                       (16, 27, WOOD_DK), (17, 27, WOOD), (24, 25, WOOD_DK)):
        g.set(x, y, ch)
    fold(g, [(17, 9), (16, 14), (17, 17)], "E", "L")
    fold(g, [(24, 14), (25, 20)], "A", "e")
    g.set(23, 11, "A")
    g.set(17, 21, "E")
    g.set(23, 23, "e")
    SB.rivets(g, [(16, 6), (24, 9), (16, 24)], "t", "q")
    # the hand gripping the board's edge
    g.rect(13, 17, 14, 18, "R")
    g.set(13, 17, "r")
    # its foot spikes into the deck
    for x in (16, 24):
        g.set(x, FEET, "q")
    contact(g, 4, 27)
    return g.rows()


# =================================================================================================
# WALL - The Ram. A roofed battering ram, walked by legs under it.
# =================================================================================================

def ram():
    """A roof wider than anything on the bridge, on legs, the ram's iron head out of its front.

    A penthouse ram - a pitched roof of planks over a slung beam - walked by the men under it, of
    whom nothing shows but their legs, mid-stride and out of step, and one pair of eyes in the dark
    under the eaves. The roof is the attachment and it runs nearly the whole cell: filled black, a
    broad gabled shape on a row of legs with a snout out of its front. Planks laid in courses,
    staggered, the joints irregular; a wet hide flung over the ridge against fire; the beam's iron
    head flared and riveted.

    The first pass put eight straight legs under it at even spacing and it was a hut on a table: the
    legs stride now, in pairs that do not agree, and the head is the size of a man's chest.
    """
    g = G(32)
    # LEGS under the eaves: four men's, mid-stride and out of step
    for (hip, foot, ch) in (((4, 21), (2, FEET - 1), "e"), ((6, 21), (7, FEET - 1), "E"),
                            ((11, 21), (12, FEET - 1), "e"), ((13, 21), (11, FEET - 1), "E"),
                            ((17, 21), (15, FEET - 1), "e"), ((19, 21), (20, FEET - 1), "E"),
                            ((23, 21), (24, FEET - 1), "e"), ((25, 21), (23, FEET - 1), "E")):
        (x0, y0), (x1, y1) = hip, foot
        knee = ((x0 + x1) / 2.0 + (0.8 if x1 > x0 else -0.3), 25.0)
        for (a, b) in ((hip, knee), (knee, foot)):
            _limb(g, a[0], a[1], b[0], b[1], 0.8, "E", ch, "A")
        g.span(FEET, x1 - 1, x1 + 1, WOOD_DK)
        g.set(x1 - 1, FEET, WOOD)
        g.set(x1 + 1, FEET - 1, WOOD)
    # the dark under the roof
    g.rect(3, 19, 26, 21, "o")
    # THE BEAM, slung under the roof
    g.rect(13, 19, 26, 20, WOOD)
    g.span(19, 13, 26, WOOD_LIT)
    g.set(18, 20, WOOD_DK)
    g.line(16, 17, 16, 19, "q")
    g.line(22, 17, 22, 19, "q")
    # THE HEAD: iron, flared where it meets the beam, blunt at the front
    g.rect(26, 16, 30, 22, "R")
    g.line(26, 15, 26, 23, "r")
    g.line(27, 15, 27, 23, "R")
    g.set(26, 15, "t")
    g.span(16, 28, 30, "r")
    g.line(31, 17, 31, 21, "q")
    g.span(22, 27, 30, "q")
    g.span(23, 26, 27, "q")
    g.set(30, 16, "q")
    SB.rivets(g, [(28, 17), (30, 19), (28, 20)], "t", "q")
    g.line(29, 18, 29, 21, "q")
    # THE ROOF: a pitched roof of planks, the gable at the front
    poly(g, [(8, 5), (22, 5), (29, 17), (1, 17)], WOOD)
    # courses of planks, staggered, the joints irregular
    for (y, joints) in ((7, (12, 19)), (9, (8, 15, 22)), (11, (5, 11, 18, 24)), (13, (9, 16, 22)),
                        (15, (3, 7, 13, 20, 26))):
        for x in range(0, 32):
            if g.get(x, y) == WOOD:
                g.set(x, y, WOOD_DK)
        for x in joints:
            if g.get(x, y - 1) == WOOD:
                g.set(x, y - 1, WOOD_DK)
            if g.get(x + 1, y - 1) == WOOD:
                g.set(x + 1, y - 1, WOOD_LIT)
    # the lit edges of the roof: the ridge and the key-side slope
    g.span(5, 9, 21, WOOD_LIT)
    g.set(8, 5, GOLD_LIT)
    g.line(8, 5, 1, 17, WOOD_LIT)
    g.line(9, 6, 3, 16, WOOD)
    g.line(22, 5, 29, 17, WOOD_DK)
    # the eaves: a heavy bottom beam
    g.span(17, 1, 29, WOOD)
    g.span(17, 2, 28, WOOD_LIT)
    g.span(18, 1, 29, WOOD_DK)
    # A WET HIDE laid along the ridge against fire, ragged where it hangs over the planks
    hang = {8: 6, 9: 7, 10: 8, 11: 7, 12: 9, 13: 8, 14: 7, 15: 9, 16: 8, 17: 7, 18: 8, 19: 7, 20: 6,
            21: 6}
    for x, y1 in hang.items():
        g.line(x, 4, x, y1, "N")
        g.set(x, y1, "j")
    g.span(4, 9, 20, "p")
    g.set(9, 4, "P")
    g.set(10, 4, "P")
    g.set(8, 5, "p")
    g.set(21, 5, "j")
    for (x0, x1) in ((11, 11), (14, 14), (17, 18)):
        g.line(x0, 5, x1, 7, "j")
        g.set(x0 - 1, 5, "p")
    # one carrier's eyes in the dark under the eaves
    taken(g, 20, 8)
    contact(g, 1, 26)
    return g.rows()


# =================================================================================================
# RANGED - Crossbowman. The bolt floating at the stock.
# =================================================================================================

def crossbowman():
    """A soldier with a crossbow held level at his cheek, and over the stock, the bolt he will loose,
    floating, lit with his cold fire.

    The caster's detached orb, as a soldier carries it: a quarrel hanging in the air above the
    crossbow with daylight between, its head white-hot and its shaft the arcane ramp's mid, so the
    bolt it throws is the colour of the thing floating at the stock. The figure is upright and narrow
    - the caster's column - in a violet tabard over his jack, under a sallet whose tail sweeps back,
    which is the one helm on the bridge with a tail. The crossbow is a level bar with a bow across
    its front; the knight's lance is longer, lower and points from a crouch.
    """
    g = G(32)
    # LEGS
    for (x0, x1, key) in ((11, 12, "w"), (15, 16, "x")):
        g.rect(x0, 23, x1, 27, "x")
        g.line(x0, 23, x0, 27, key)
        g.line(x1, 23, x1, 27, "X")
        boot(g, x0 - (1 if x0 < 13 else 0), x1, 28)
    # THE BODY: a tabard over the jack, narrow, to the knee
    mass(g, [(13, 11, 16), (17, 10, 17), (22, 10, 17), (24, 9, 17)], "w", "v", "x")
    fold(g, [(13, 15), (12, 24)], "x", "v")
    fold(g, [(15, 17), (16, 24)], "X", None)
    g.span(24, 9, 17, "X")
    cut(g, (13, 24), (14, 24))
    # a strip of the red jack showing at the collar and a belt with a quiver of bolts at the hip
    g.span(13, 12, 15, RED)
    g.set(12, 13, RED_LIT)
    g.span(19, 10, 17, RED_SH)
    g.set(11, 19, RED)
    g.rect(7, 18, 9, 23, WOOD)
    g.line(7, 18, 7, 23, WOOD_LIT)
    g.line(9, 18, 9, 23, WOOD_DK)
    for x in (7, 8, 9):
        g.set(x, 17, "e" if x % 2 else "l")
    g.set(8, 16, "l")
    # THE SALLET: a round bowl, the tail sweeping back, a slit for the eyes
    blob(g, [(14.0, 9.0, 3.0)], "R", "r", "q")
    g.line(11, 10, 8, 12, "R")
    g.line(11, 11, 8, 13, "q")
    g.set(8, 12, "r")
    g.set(7, 13, "r")
    g.span(12, 11, 16, "q")
    g.span(10, 13, 17, "o")
    taken(g, 10, 15)
    g.set(15, 7, "t")
    g.set(13, 7, "q")
    # THE ARMS: both forward, at the stock
    g.line(16, 14, 18, 16, "w")
    g.line(16, 15, 18, 17, "x")
    g.rect(18, 16, 19, 17, WOOD_LIT)
    g.line(12, 15, 15, 18, "v")
    g.rect(15, 18, 16, 19, WOOD)
    # THE CROSSBOW: a stock held level from the cheek, the bow across its front
    g.span(15, 14, 25, WOOD_LIT)
    g.span(16, 15, 25, WOOD)
    g.set(26, 15, WOOD)
    g.set(26, 16, WOOD_DK)
    g.set(17, 17, WOOD_DK)                 # the trigger
    # the bow: steel, swept back at its tips
    for (x, y) in ((23, 11), (24, 12), (24, 13), (25, 14), (25, 17), (24, 18), (24, 19), (23, 20)):
        g.set(x, y, "r" if y < 15 else "q")
    g.set(25, 15, "R")
    g.set(25, 16, "q")
    # the string, back to the nut
    g.line(23, 12, 19, 15, "l")
    g.line(23, 19, 19, 16, "E")
    # the stirrup at the very front
    g.set(27, 15, "q")
    g.set(27, 16, "R")
    g.set(28, 16, "q")
    # THE BOLT, floating over the stock with daylight between: head white-hot, shaft the mid
    g.span(8, 19, 25, "y")
    g.set(26, 8, "Y")
    g.set(27, 8, "Z")
    g.set(26, 7, "z")
    g.set(26, 9, "z")
    g.set(19, 7, "z")
    g.set(19, 9, "z")
    g.set(20, 8, "z")
    contact(g, 9, 19)
    return g.rows()


# =================================================================================================
# TURRET - Gargoyle. A stone gargoyle on the parapet; built, it burns.
# =================================================================================================

def gargoyle():
    """Crouched on a block of parapet, bat's wings half raised behind it, the head thrust out over the
    edge with its jaw open.

    A built thing, so it burns: fire eyes, and the spout of its mouth glowing where the fire it throws
    comes from. Carved stone in bone's lower stops - its deep stop as the mass, its shade on the lit
    edges and its base only along the wings' leading edges and the shoulders - so the beast stands a
    stop off the plinth without being the brightest thing on the deck. Crouched on all fours with
    membranes up behind it, scalloped between the fingers, not upright and horned as the Ruins carve
    theirs; the plinth is a merlon of the parapet, laid in courses.

    The first pass drew the wings as single strokes, and filled black they were a rabbit's ears; the
    second carved the body in bone's base and it was the palest thing in the crowd, a white rabbit.
    """
    g = G(32)
    # THE PLINTH: a merlon, laid stone, wider than the beast
    g.rect(7, 22, 25, 29, "p")
    SB.courses(g, 23, 29, "p", "N", "P", high=3, wide=7)
    g.span(22, 7, 25, "P")
    g.line(7, 23, 7, 29, "P")
    g.line(25, 23, 25, 29, "N")
    g.span(29, 7, 25, "N")
    g.set(6, 23, "W")
    # THE FAR WING, behind, its tip just showing over the near one
    poly(g, [(19.5, 13), (19.5, 6), (16.5, 2), (14.5, 5), (15.5, 7), (14, 9), (17, 13.5)], "m")
    g.line(19, 12, 19, 6, "n")
    g.line(19, 6, 17, 3, "n")
    # THE NEAR WING: a membrane from the shoulder, scalloped between its fingers
    poly(g, [(16.5, 13.5), (15.5, 7), (10.5, 2), (8, 6), (10, 8), (6.5, 10), (9, 12), (6.5, 15),
             (12, 16.5)], "m")
    for (a, b) in (((16, 13), (15, 7)), ((15, 7), (10, 2))):
        g.line(a[0], a[1], b[0], b[1], "k")
        g.line(a[0] + 1, a[1], b[0] + 1, b[1], "n")
    for tip in ((8, 6), (7, 10), (7, 14)):
        fold(g, [(15, 8), tip], "n", None)
    for (x, y) in ((9, 8), (8, 12), (11, 4)):
        g.set(x, y, "o")
    # THE BODY: crouched on all fours, haunch at the back, the shoulders forward
    blob(g, [(11.5, 18.5, 3.3), (10.5, 20.0, 2.6), (15.0, 17.5, 3.0), (18.0, 16.0, 3.0)], "m", "n", "J")
    for (x, y) in ((16, 13), (17, 13), (18, 13), (12, 15), (13, 15)):
        g.set(x, y, "k")
    fold(g, [(13, 16), (12, 21)], "J", "n")
    fold(g, [(16, 17), (17, 20)], "J", "n")
    g.set(17, 14, "s")
    # the haunch's folded hind leg and the fore legs gripping the edge
    g.line(8, 21, 13, 21, "m")
    g.set(8, 20, "k")
    for (x, ch) in ((18, "k"), (20, "n")):
        g.rect(x, 18, x + 1, 21, "n")
        g.line(x, 18, x, 21, ch)
        g.set(x + 1, 21, "m")
    for x in (18, 20, 22):
        g.set(x, 22, "m")
        g.set(x + 1, 22, "n")
    # THE HEAD: thrust forward, low, the jaw open as a spout
    blob(g, [(22.5, 13.0, 2.6), (25.0, 13.5, 1.8)], "m", "n", "J", rim=False)
    g.span(11, 21, 25, "k")
    g.set(21, 11, "s")
    # ears swept back
    g.line(20, 10, 19, 8, "n")
    g.set(19, 8, "k")
    g.set(20, 11, "m")
    # the open mouth - the spout - glowing; the lower jaw hung under it
    g.rect(25, 14, 28, 15, "o")
    g.span(13, 25, 27, "n")
    g.set(28, 13, "k")
    g.span(16, 24, 27, "m")
    g.set(27, 16, "n")
    g.set(26, 15, "~")
    g.set(27, 15, "=")
    g.set(27, 14, "~")
    # a brow ridge over the fire eyes
    g.span(12, 22, 24, "m")
    built(g, 22, 13)
    contact(g, 6, 26)
    return g.rows()


# =================================================================================================
# CHARGER - Gate Knight. A knight on foot with a couched lance.
# =================================================================================================

def knight():
    """Low in a charging crouch, the lance couched under his arm, level and forward.

    The pose is the telegraph, as every charger's is: weight on the front leg, the back leg driving,
    the body pitched forward over the lance. The lance is the attachment - a long level line out past
    the front of the cell with a vamplate flaring at the hand - and his helm has a pointed visor
    jutting forward like a snout, which no other head on the bridge has. Plate in steel, the skirts of
    a red surcoat ragged under the lance. Crouched and horizontal where the Constable is tall and
    upright.
    """
    g = G(32)
    # THE BACK LEG, driving, and the front leg bent under him
    _limb(g, 11, 21, 6, 26, 1.6, "R", "r", "q")
    _limb(g, 6, 26, 5, FEET - 1, 1.2, "R", "r", "q")
    g.span(FEET, 3, 7, "q")
    g.set(3, FEET, "r")
    _limb(g, 16, 21, 20, 25, 1.7, "R", "r", "q")
    _limb(g, 20, 25, 20, FEET - 1, 1.3, "R", "r", "q")
    g.disc(20, 25, 1.4, "R")
    g.set(19, 24, "r")
    g.set(21, 26, "q")
    g.span(FEET, 19, 23, "q")
    g.set(19, FEET, "r")
    # THE BODY, pitched forward
    blob(g, [(13.0, 16.0, 4.2), (15.5, 14.5, 3.8), (12.0, 19.5, 3.4)], "R", "r", "q")
    SB.ridges(g, 11, 16, "R", "q", "r", step=3, jitter=5)
    # the surcoat's skirts under the lance, ragged at the hem
    for (y, a, b) in ((19, 9, 16), (20, 8, 16), (21, 8, 16), (22, 9, 15), (23, 10, 14)):
        g.span(y, a, b, RED)
        g.set(a, y, RED_LIT)
        g.set(b, y, RED_SH)
    fold(g, [(12, 19), (11, 23)], RED_SH, RED_LIT)
    fold(g, [(14, 20), (15, 23)], RED_DP, None)
    cut(g, (13, 23), (9, 22))
    g.set(12, 24, RED_SH)
    # a strip of the surcoat over the chest, above the lance
    g.span(15, 11, 14, RED_SH)
    g.span(16, 10, 15, RED)
    g.set(10, 16, RED_LIT)
    # pauldron over the near shoulder
    blob(g, [(17.0, 12.5, 2.4)], "R", "r", "q", rim=False)
    g.set(16, 11, "t")
    g.set(18, 13, "q")
    # THE HELM: a bascinet with a pointed visor jutting forward
    blob(g, [(19.5, 9.0, 2.8)], "R", "r", "q")
    for (x, y, ch) in ((22, 8, "r"), (23, 9, "r"), (24, 9, "R"), (25, 10, "q"), (23, 10, "R"),
                       (22, 10, "R"), (22, 11, "q"), (23, 11, "q"), (21, 11, "q"), (24, 10, "R")):
        g.set(x, y, ch)
    g.span(9, 20, 22, "o")
    g.set(18, 7, "t")
    g.line(17, 11, 15, 12, "q")
    taken(g, 9, 21)
    # the arm, couching the lance under it
    g.line(17, 14, 17, 16, "R")
    g.rect(16, 16, 18, 17, "r")
    # THE LANCE: long and level, the vamplate flaring at the hand
    g.span(17, 3, 31, WOOD_LIT)
    g.span(18, 3, 29, WOOD)
    g.set(3, 18, WOOD_DK)
    for x in range(8, 30, 6):
        g.set(x, 18, WOOD_DK)
    g.span(17, 28, 31, "r")
    g.set(31, 17, "t")
    g.span(18, 28, 29, "q")
    # the vamplate: a cone opening back toward the hand
    for (x, y0, y1) in ((19, 14, 21), (20, 15, 20), (21, 16, 19)):
        g.line(x, y0, x, y1, "R")
        g.set(x, y0, "r")
        g.set(x, y1, "q")
    g.set(19, 14, "t")
    contact(g, 3, 23)
    return g.rows()


# =================================================================================================
# EXPLODER - Pitch Barrel. Faces LEFT: the stand-in sets SpriteFacesRightByDefault = false.
# =================================================================================================

def pitch_barrel():
    """A barrel of pitch on its side, rolling, the fuse in its bung alight. Its head leads, to the left.

    Built, so it burns: the fuse's spark, and two pixels of fire looking out of a split in its head
    where the mark goes. Staves run along it, bellied out top and bottom, bound by iron hoops unevenly
    spaced; pitch has welled up round the bung and run down its side in black. Lying down and rolling,
    where the Sands' cask stands up and walks: filled black it is a low oval with a curl of fuse and a
    spark over it.

    The first pass had a flat top and a flat bottom, and hooped and gold it was a treasure chest - the
    one thing a horde creature must never look like. It is bellied both ways now, ends narrower than
    its middle, and its staves are a stop darker.
    """
    g = G(32)
    cy = 23.5

    def half(x):
        t = (x - 16.5) / 11.0
        return 6.6 - 2.4 * t * t

    # THE STAVES: a bulging cylinder on its side, bellied top and bottom
    for x in range(6, 28):
        hh = half(x)
        top, bot = int(round(cy - hh)), int(round(cy + hh))
        for y in range(top, bot + 1):
            g.set(x, y, WOOD)
        g.set(x, top, WOOD_LIT)
        g.set(x, bot, WOOD_DK)
        g.set(x, bot - 1, WOOD_DK if x % 3 else WOOD)
    # stave joints running along it, bowed with the barrel
    for (k, lit) in ((-0.62, True), (-0.2, False), (0.28, False), (0.66, False)):
        for x in range(7, 27):
            y = cy + k * half(x)
            if g.get(x, y) in (WOOD, WOOD_LIT):
                g.set(x, y, WOOD_DK)
                if lit and g.get(x, y - 1) == WOOD:
                    g.set(x, y - 1, WOOD_LIT)
    # THE HEAD, leading: the end of the barrel seen nearly edge-on, the chime round it
    for y in range(int(round(cy - half(5))) + 1, int(round(cy + half(5)))):
        g.set(5, y, WOOD)
        g.set(4, y, WOOD_LIT)
    g.set(5, int(round(cy - half(5))), WOOD_LIT)
    g.set(3, 22, WOOD_LIT)
    g.set(3, 23, WOOD)
    g.set(3, 24, WOOD_DK)
    g.set(3, 25, WOOD_DK)
    # THE HOOPS: iron, unevenly spaced, lit on top
    for x in (7, 11, 21, 25):
        col = [y for y in range(10, 31) if g.get(x, y) not in (".", "W")]
        for y in col:
            g.set(x, y, "R")
        g.set(x, col[0], "t")
        g.set(x, col[-1], "q")
        g.set(x + 1, col[0], "r")
    # THE PITCH: welled up round the bung and run down the side in black
    for (x, y0, y1) in ((14, 18, 19), (15, 17, 22), (16, 16, 20), (17, 16, 25), (18, 16, 19),
                        (19, 17, 21), (20, 17, 18)):
        g.line(x, y0, x, y1, "J")
    for (x, y) in ((15, 23), (17, 26), (19, 22)):
        g.set(x, y, "j")
    g.set(18, 16, "N")
    g.set(15, 17, "N")
    # THE BUNG and THE FUSE, curling back over the top, alight at its end
    g.rect(16, 15, 18, 16, "R")
    g.set(16, 15, "r")
    g.set(18, 16, "q")
    for (x, y, ch) in ((17, 14, "e"), (17, 13, "l"), (18, 12, "e"), (19, 11, "l"), (20, 11, "E"),
                       (21, 10, "e")):
        g.set(x, y, ch)
    for (x, y, ch) in ((22, 9, "-"), (23, 9, "="), (22, 8, "="), (23, 8, "~"), (21, 8, "~"),
                       (22, 7, "~"), (24, 10, "~"), (24, 7, "~")):
        g.set(x, y, ch)
    # a seam started under the heat
    g.set(10, 25, "~")
    g.set(9, 25, "=")
    # the split in the head, with the fire looking out of it
    g.set(4, 21, "o")
    built(g, 4, 22)
    g.set(4, 23, "o")
    g.set(5, 23, "o")
    roster.contact(g, 6, 27)
    return g.rows()


# =================================================================================================
# SLAMMER - The Warder. A gate guard bringing a maul down.
# =================================================================================================

def warder():
    """A broad guard of the gate, both arms up, a great maul raised over his head.

    The slammer's rule - the thing that will land is the highest thing in the cell - as a man would
    carry it: a wooden beetle bound in iron at both faces, held up and back on its haft in both
    fists, the arms raised in a V under it. He is broad and planted beneath it in a padded linen coat
    and a mail coif, the one head on the bridge in mail. The Emberdeep's trip-hammer was a machine;
    this is a man.

    The first pass hid the arms against the coif and gave the beetle a flag's proportions; the arms
    are raised clear of the body now and the head is as deep as it is wide.
    """
    g = G(32)
    # LEGS, planted wide
    for (x0, x1, key) in ((9, 11, "l"), (17, 19, "e")):
        g.rect(x0, 23, x1, 27, "E")
        g.line(x0, 23, x0, 27, key)
        g.line(x1, 23, x1, 27, "A")
        boot(g, x0 - (1 if x0 < 14 else 0), x1 + (0 if x0 < 14 else 1), 28)
    # THE COAT: padded linen, broad, to the knee
    mass(g, [(16, 10, 20), (19, 9, 21), (24, 8, 21)], "e", "l", "E")
    fold(g, [(13, 17), (12, 24)], "E", "l")
    fold(g, [(17, 18), (18, 24)], "A", None)
    for (x, y) in ((11, 19), (15, 22), (19, 18), (10, 22)):
        g.set(x, y, "L" if (x + y) % 3 else "E")
    g.span(24, 8, 21, "A")
    cut(g, (14, 24), (15, 24))
    # a broad belt
    g.span(20, 9, 21, RED_SH)
    g.span(21, 9, 21, RED)
    g.set(9, 20, RED_LIT)
    g.set(15, 21, GOLD_LIT)
    # THE ARMS, both raised in a V, up to the haft over his head
    _limb(g, 11, 16, 13, 9, 1.5, "e", "l", "E")
    _limb(g, 20, 16, 18, 9, 1.5, "e", "l", "E")
    g.line(12, 10, 12, 15, "A")
    g.line(19, 10, 19, 15, "E")
    # THE COIF: mail over head and shoulders, a hole for the face
    blob(g, [(15.5, 12.5, 2.9), (15.5, 15.5, 3.6)], "R", "r", "q")
    # the rings: dark links scattered by a hash, because mail laid on a regular lattice at this size
    # is a grille
    for y in range(9, 20):
        for x in range(10, 22):
            if g.get(x, y) == "R" and (x * 7 + y * 13 + x * y) % 5 in (0, 3):
                g.set(x, y, "q")
    g.span(12, 14, 17, "o")
    g.span(13, 15, 17, "o")
    taken(g, 12, 15)
    # the fists on the haft
    g.rect(12, 7, 13, 8, GOLD)
    g.set(12, 7, GOLD_LIT)
    g.rect(17, 7, 18, 8, GOLD_SH)
    g.set(17, 7, GOLD)
    # THE HAFT, up and back over his head
    g.line(19, 8, 11, 4, WOOD_LIT)
    g.line(19, 9, 11, 5, WOOD)
    g.set(20, 9, WOOD_DK)
    # THE MAUL'S HEAD: the highest and heaviest thing in the cell, its faces bound in iron
    g.rect(2, 2, 11, 8, WOOD)
    g.span(2, 3, 10, WOOD_LIT)
    g.line(3, 3, 3, 8, WOOD_LIT)
    g.span(8, 3, 11, WOOD_DK)
    g.line(10, 3, 10, 8, WOOD_DK)
    for (x, key) in ((2, "r"), (11, "q")):
        g.line(x, 2, x, 8, "R")
        g.set(x, 2, "t" if key == "r" else "r")
        g.set(x, 8, "q")
    g.line(4, 2, 4, 8, "R")
    g.line(9, 2, 9, 8, "R")
    g.set(4, 2, "t")
    g.set(9, 8, "q")
    g.set(6, 4, WOOD_DK)
    g.set(7, 6, WOOD_DK)
    g.set(6, 5, WOOD_LIT)
    contact(g, 7, 22)
    return g.rows()


# =================================================================================================
# SUMMONER - Houndmaster. Calls the Bridge Hounds.
# =================================================================================================

def houndmaster():
    """Keeper of the gate's dogs, a leash-pole raised high, its leashes hanging empty.

    The attachment is the POLE: held up steeply in one fist, higher than anything else he is, a hook
    at its head with two leashes hanging from it to two open spiked collars - the hounds' own collars,
    and the dogs he calls are out of them. A hooded figure in a heavy cloak of dark stone-grey, his
    far forearm bound thick in red leather - the handler's bite sleeve - and a horn at his hip. Where
    the Kennelmaster held a ring at hand height, this holds a pole up over the crowd.

    The first pass laid the pole at forty-five degrees with the leashes at its far tip, and it was a
    man fishing; the pole stands steep now and the collars hang close under its hook.
    """
    g = G(32)
    # THE CLOAK: heavy, to the ground, wider at the hem
    mass(g, [(13, 10, 17), (18, 9, 18), (24, 8, 19), (29, 7, 20)], "N", "p", "j")
    fold(g, [(12, 15), (10, 29)], "j", "p")
    fold(g, [(15, 16), (15, 29)], "J", None)
    fold(g, [(17, 20), (18, 29)], "j", None)
    for (x, y) in ((9, 25), (13, 22), (16, 27)):
        g.set(x, y, "P")
    g.span(FEET, 7, 20, "J")
    cut(g, (11, FEET), (12, FEET))
    g.set(11, 29, "j")
    # the hood, its peak drawn forward
    blob(g, [(13.5, 10.0, 3.4)], "N", "p", "j")
    g.set(11, 7, "P")
    g.set(16, 7, "p")
    g.set(17, 8, "N")
    g.line(10, 10, 10, 13, "p")
    for (y, a, b) in ((10, 13, 16), (11, 13, 17), (12, 14, 17)):
        g.span(y, a, b, "o")
    taken(g, 11, 15)
    # THE BITE SLEEVE: the far forearm, bound thick in red leather
    blob(g, [(9.0, 18.0, 2.0), (8.0, 20.5, 2.2)], RED, RED_LIT, RED_SH, rim=True)
    for (x, y) in ((8, 18), (9, 20), (7, 21)):
        g.set(x, y, RED_SH)
    g.set(8, 22, RED_DP)
    # a horn at his hip on a strap
    g.line(10, 14, 17, 23, WOOD_DK)
    g.rect(16, 23, 18, 24, "k")
    g.set(15, 24, "s")
    g.set(19, 24, "n")
    g.set(19, 23, "m")
    # THE POLE: steep, held high, butt at his knee, a hook at its head
    g.line(17, 25, 24, 4, WOOD)
    g.line(18, 25, 25, 4, WOOD_DK)
    g.line(16, 25, 23, 4, WOOD_LIT)
    for (x, y, ch) in ((24, 3, "r"), (25, 2, "t"), (26, 2, "r"), (27, 3, "R"), (28, 4, "q"),
                       (28, 5, "q"), (23, 3, "q")):
        g.set(x, y, ch)
    # THE NEAR ARM, raised to the pole, the fist round it
    g.line(16, 15, 20, 12, "N")
    g.line(16, 16, 20, 13, "j")
    g.set(17, 14, "p")
    g.rect(20, 11, 22, 13, WOOD)
    g.set(20, 11, WOOD_LIT)
    g.set(22, 13, WOOD_DK)
    # the leashes, hanging from the hook to two open spiked collars
    for (pts, a, b) in (([(28, 6), (28, 10)], RED, RED_SH),
                        ([(26, 3), (26, 5), (25, 8)], RED_LIT, RED_SH)):
        k = 0
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            n = int(max(abs(x1 - x0), abs(y1 - y0)))
            for i in range(n + 1):
                t = i / float(max(1, n))
                g.set(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, a if k % 3 else b)
                k += 1
    for (cx, cy) in ((28, 12), (25, 10)):
        for (dx, dy, ch) in ((-1, -1, "r"), (0, -1, "t"), (1, -1, "r"), (-1, 0, "R"), (1, 0, "q"),
                             (-1, 1, "q"), (0, 1, "q"), (-2, 0, "R"), (2, 0, "q"), (0, 2, "q")):
            g.set(cx + dx, cy + dy, ch)
    contact(g, 6, 21)
    return g.rows()


# =================================================================================================
# MINIBOSS - The Porter. 48 x 48. Keeper of the gate's windlass.
# =================================================================================================

def porter():
    """A stooped giant of a gatekeeper, the great key of the gate's windlass across his back.

    The windlass key is a crank: a squared iron shank with a socket at its foot that fits the
    windlass's axle, an elbow, and a wooden grip - strapped across his back so it rises off it, the
    elbow and the grip standing over his shoulders. That bent bar is the attachment, and it is
    machinery, not a jailer's keys: the Constable carries the ring. He is broad, bowed forward under
    it, his arms long and heavy from turning the gate, in a hooded coat of wool with a leather apron,
    a coil of the gate's chain at his belt. Hunched and unarmoured where the Constable is tall and
    plated, and lopsided toward the crank where the Chainwright's jib reached out in front of it.
    """
    g = G(48)
    F = 46
    # LEGS, short and thick, a stride apart
    for (lx0, lx1, key) in ((13, 18, "c"), (23, 28, "b")):
        g.rect(lx0, 36, lx1, 42, "b")
        g.line(lx0, 36, lx0, 42, key)
        g.line(lx1, 36, lx1, 42, "a")
        fold(g, [(lx0 + 2, 37), (lx0 + 3, 41)], "a", "c")
        g.rect(lx0 - 1, 43, lx1 + 1, F, WOOD)
        g.span(43, lx0 - 1, lx1 + 1, WOOD_LIT)
        g.span(F, lx0 - 1, lx1 + 2, WOOD_DK)
        g.set(lx1 + 2, F - 1, WOOD_DK)
        g.set(lx0, 44, WOOD_LIT)
    g.rect(19, 38, 22, 42, "o")
    # THE COAT: a broad bowed back, wool, the hem below the hips
    blob(g, [(18.0, 23.0, 8.6), (20.0, 29.0, 9.6), (19.5, 33.5, 9.0), (25.0, 20.0, 6.0)], "b", "c", "a")
    fold(g, [(11, 22), (11, 35)], "a", "c")
    fold(g, [(16, 18), (14, 36)], "a", "c")
    fold(g, [(20, 29), (21, 38)], "a", None)
    for (x, y) in ((13, 25), (18, 21), (12, 31), (17, 33), (23, 24)):
        g.set(x, y, "d")
    g.span(38, 11, 28, "a")
    g.span(39, 12, 27, "o")
    # THE APRON: old red leather, hanging from the belt in front of his belly
    for y in range(27, 39):
        a = int(round(22 + (y - 27) * 0.1))
        b = int(round(28 - (y - 27) * 0.1))
        g.span(y, a, b, RED)
        g.set(a, y, RED_LIT)
        g.set(b, y, RED_SH)
    fold(g, [(25, 28), (24, 38)], RED_SH, RED_LIT)
    g.span(38, 23, 27, RED_DP)
    # the belt, and a coil of the gate's chain hanging at his hip
    g.span(26, 11, 28, WOOD_DK)
    g.span(25, 12, 28, WOOD)
    g.set(12, 25, WOOD_LIT)
    g.rect(19, 25, 20, 26, GOLD_LIT)
    for k in range(0, 360, 30):
        a = math.radians(k)
        g.set(14.5 + 2.6 * math.cos(a), 30.0 + 2.6 * math.sin(a), "r" if (k // 30) % 2 else "q")
    g.set(14, 27, "t")
    g.set(14, 30, "o")
    g.set(15, 30, "o")
    # THE HEAD: low and forward under a hood, its peak drawn over the face
    blob(g, [(31.0, 18.0, 4.4), (33.0, 19.5, 3.6)], "b", "c", "a")
    g.set(35, 14, "c")
    g.set(36, 15, "b")
    g.set(37, 16, "a")
    for (y, a, b) in ((17, 34, 36), (18, 33, 37), (19, 33, 37), (20, 34, 37), (21, 35, 36)):
        g.span(y, a, b, "o")
    taken(g, 19, 34)
    g.set(29, 14, "d")
    g.set(28, 15, "d")
    # THE NEAR ARM: long and heavy, hanging forward, a gloved fist by his knee
    g.line(27, 22, 32, 31, "o")
    _limb(g, 27, 21, 32, 30, 2.6, "b", "c", "a")
    _limb(g, 32, 30, 34, 36, 2.3, "b", "c", "a")
    fold(g, [(29, 24), (31, 29)], "a", "c")
    g.span(36, 32, 36, "a")
    g.disc(34.5, 38.5, 2.6, WOOD)
    g.set(33, 37, WOOD_LIT)
    g.set(32, 38, WOOD_LIT)
    g.set(36, 40, WOOD_DK)
    g.set(37, 39, WOOD_DK)
    g.set(35, 39, WOOD_DK)
    g.line(32, 37, 33, 36, "o")
    # THE WINDLASS KEY: a crank strapped across his back - socket at the foot, a squared shank, an
    # elbow over the shoulders and a wooden grip
    sx0, sy0, sx1, sy1 = 7.0, 33.0, 22.0, 6.0
    n = int(max(abs(sx1 - sx0), abs(sy1 - sy0)))
    for i in range(n + 1):
        t = i / float(n)
        x, y = sx0 + (sx1 - sx0) * t, sy0 + (sy1 - sy0) * t
        g.set(x - 1, y, "t")
        g.set(x, y, "r")
        g.set(x + 1, y, "R")
        g.set(x + 2, y, "q")
    # the socket at its foot: a squared iron block with a square hole
    g.rect(3, 33, 9, 39, "R")
    g.line(3, 33, 9, 33, "t")
    g.line(3, 33, 3, 39, "r")
    g.line(9, 34, 9, 39, "q")
    g.span(39, 3, 9, "q")
    g.rect(5, 35, 7, 37, "o")
    g.set(5, 35, "J")
    SB.rivets(g, [(4, 34), (8, 38)], "t", "q")
    # the elbow and the crank arm, back toward his heel
    g.rect(19, 3, 25, 6, "R")
    g.span(3, 19, 25, "t")
    g.line(25, 3, 25, 6, "q")
    g.span(6, 19, 25, "q")
    g.rect(9, 3, 19, 5, "R")
    g.span(3, 9, 19, "r")
    g.span(5, 9, 19, "q")
    g.set(9, 3, "t")
    g.set(14, 4, "q")
    # the grip: a sleeve of wood on the crank's end, pointing down
    g.rect(9, 6, 11, 12, WOOD)
    g.line(9, 6, 9, 12, WOOD_LIT)
    g.line(11, 6, 11, 12, WOOD_DK)
    g.span(12, 9, 11, WOOD_DK)
    g.set(10, 9, WOOD_DK)
    # the strap that holds it, across his chest
    g.line(14, 15, 27, 25, WOOD_DK)
    g.line(15, 15, 28, 25, WOOD)
    SB.rivets(g, [(20, 19)], GOLD_LIT, WOOD_DK)
    roster.contact(g, 11, 38, y=F)
    return g.rows()


# =================================================================================================

CAST = [
    # stem, pose, cell, (lean, reach, knock), SpriteFrames name
    #
    # THE MINIBOSS IS LISTED FIRST: bestiary_kit.report sizes its contact sheet from the first
    # creature, and a 32 cell first crops the 48-cell Porter (see cast_sands.py).
    ("bridge-porter", porter, 48, (0.5, 4.0, 1), "BridgePorterFrames"),
    ("bridge-pikeman", pikeman, 32, (0.7, 2.6, 2), "BridgePikemanFrames"),
    ("bridge-hound", hound, 32, (1.8, 3.6, 2), "BridgeHoundFrames"),
    ("bridge-pavisier", pavisier, 32, (0.4, 2.6, 1), "BridgePavisierFrames"),
    ("bridge-ram", ram, 32, (0.4, 3.0, 1), "BridgeRamFrames"),
    ("bridge-crossbowman", crossbowman, 32, (0.8, 2.2, 3), "BridgeCrossbowmanFrames"),
    ("bridge-gargoyle", gargoyle, 32, (0.15, -2.0, 1), "BridgeGargoyleFrames"),
    ("bridge-knight", knight, 32, (1.2, 4.6, 2), "BridgeKnightFrames"),
    ("bridge-pitch-barrel", pitch_barrel, 32, (1.0, -2.2, -3), "BridgePitchBarrelFrames"),
    ("bridge-warder", warder, 32, (0.5, 4.4, 1), "BridgeWarderFrames"),
    ("bridge-houndmaster", houndmaster, 32, (0.9, 2.6, 2), "BridgeHoundmasterFrames"),
]


def build():
    return bestiary_kit.build_cast(CAST, PALETTE)


if __name__ == "__main__":
    bestiary_kit.run(CAST, PALETTE, "bridge")
