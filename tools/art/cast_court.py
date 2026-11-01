# -*- coding: utf-8 -*-
"""The Court's own cast (.ai/endgame-castle.md 5.7). Run: python tools/art/cast_court.py

"A court with no one left in it." His household, taken - the servants who carried his platters, the
guard at his doors, his falconer, his huntsman and his hounds, the courtier who fenced in his gallery,
the usher who called his hall to order and the chamberlain who held every key - and the built things
of a great house, which burn his fire: an empty suit of armour, a standing candelabrum, a censer on
legs. Eleven creatures, every one a new look on the numbers of the Emberdeep creature it replaces;
the module contract is bestiary_kit.py's docstring.

COLD EYES ON THE TAKEN, FIRE ON THE MADE (.ai/endgame-castle.md section 2). Eight are people and
animals of the house and stare with his arcane; the armour, the candelabrum and the censer are things
he built, so they burn - two pixels of fire each, and at most a seam or a candle flame besides.

THE FLOOR DECIDES THE STOPS. The halls are the castle's flags (stone base and shade) under marble, oak
boards (gold's deep stop) and each wing's runner (red base, wool shade, lichen shade, gold deep). So no
body is carried in a row's deep stop, and no body is the runner's own row at the runner's own stop:
the Huntsman's red coat sits two stops up the red row, the Duellist's doublet a stop up wool.

ONE ROW PER JOB, so the household separates by what it is made of as well as by outline:

    Lackey       linen livery with red facings; a steel cloche     (the servants are linen)
    Court Hound  bone - a pale sighthound
    Guard        steel mail behind a red kite shield              (the garrison is steel)
    Armour       polished steel, gilt edges, fire behind the visor (built)
    Falconer     lichen hunting green; tan leather; the hawk
    Candelabrum  black iron; bone wax; fire                        (built)
    Duellist     wool doublet, a bone ruff, a violet cape; the blade
    Censer       brass (gold's full row); steel chains; fire       (built)
    Usher        a violet gown; an ivory rod with a gold knob
    Huntsman     a red riding coat; a brass horn
    Chamberlain  black velvet carried in stone's upper stops; bone fur; the steel ring of keys

NOTHING ECHOES THE SENESCHAL, a steward with a staff of office (5.6). Nobody here stands beside an
upright staff: the Usher - the one other with a staff of office - is short and gowned and swinging
his rod overhead with both hands, so filled black he is a diagonal with a ball at its top corner, and
the Chamberlain carries keys and no staff at all. NOR THE BRIDGE'S GARRISON (3.5): the Court Hound is
tall and arched where the Bridge Hound is long and low with a spiked collar; the Guard's shield is a
kite that tapers to a point, not a standing pavise; the Duellist's blade is a thread held at the
shoulder in a lunge, not a lance couched level; the Usher's rod is slender with a ball, not a maul.

ONE ATTACHMENT EACH, filled black still nameable: a dome on a plate held over a bowed head; an arched
back on stilts; a kite shield; a flat-topped helm on pauldrons wider than it; a bird on a raised fist;
branching arms with candles; a blade out in a lunge; a pierced dome hung on chains; a rod raised
overhead with a ball at its end; a curled horn raised over a cap; a ring of keys the size of a shield.

CHARACTERS. Three player-pigment character sets are re-pointed at material rows, the way the
Emberdeep, the Fields and the Heath modules do it (anim_sets steps a re-pointed listed ramp through
the hurt flash and the death fade):

    Q 1 2 3 4   ember -> red        (the Ember module's mapping)
    O ! @ # $   amber -> gold       (the full row: the roster's gold has no deep stop)
    U 5 6 7 8   azure -> lichen     (the Heath module's mapping)
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
from cast_forest import rim, inside
from cast_sands import blob, chain
from foes_forest import _limb

G = roster.G
mass = roster.mass
fold = roster.fold
cut = roster.cut
contact = roster.contact
CELL = 32
FEET = CELL - 2

PALETTE = dict(roster.PALETTE)
for _chars, _row in (("Q1234", "red"), ("O!@#$", "gold"), ("U5678", "lichen")):
    for _ch, _col in zip(_chars, B.MATERIALS[_row]):
        PALETTE[_ch] = _col

# Named stops, so a drawing says what it is made of.
RED_HI, RED_LIT, RED, RED_SH, RED_DP = "Q", "1", "2", "3", "4"
BR_HI, BR_LIT, BR, BR_SH, BR_DP = "O", "!", "@", "#", "$"            # brass, and tan leather
GR_HI, GR_LIT, GR, GR_SH, GR_DP = "U", "5", "6", "7", "8"            # lichen: hunting green
FIRE = "+-=~"


def mark(g, x, y):
    """A built thing's mark: two pixels of fire, core and hot, side by side."""
    g.set(x, y, "+")
    g.set(x + 1, y, "-")


def eyes(g, x, y):
    """A taken thing's mark: his cold arcane, hot and mid, side by side. Always drawn last."""
    g.set(x, y, "Y")
    g.set(x + 1, y, "y")


def flame(g, x, y, tall=3):
    """A candle flame of his fire: white-hot at the wick, the edge stop at the tip."""
    g.set(x, y, "+")
    g.set(x, y - 1, "-")
    if tall >= 3:
        g.set(x, y - 2, "=")
    if tall >= 4:
        g.set(x, y - 3, "~")
    g.set(x + 1, y, "=")
    g.set(x - 1, y, "~")


# =================================================================================================
# BULK - Lackey. A liveried servant, a covered salver held up.
# =================================================================================================

def lackey():
    """Bowed under the dish he is still carrying in, the dish held up and out before him.

    The attachment is the SALVER: a plate held up on his flat hand, out in front of his bowed head
    and higher than it, with a steel cloche on it - a dome on a flat line, with daylight between it
    and him - so filled black he is a small bowed figure holding up a little hill. Under it, the
    livery of the house: a linen coat with long tails, its collar, lapel and cuffs faced in the
    house's red, a pale waistcoat, and a tie-wig of bone with its queue tied in black at the nape.
    He is the lowest upright figure in the chapter, as a bulk body is, and the bow is the point: a
    servant still serving, with nobody at the table.
    """
    g = G(CELL)
    # LEGS: pale stockings carried in bone's lower stops, black shoes, a short stride.
    for (hx, fx, mid, lit, dark) in ((11, 10, "n", "k", "m"), (14, 15, "m", "n", "J")):
        mass(g, [(23, hx - 1.2, hx + 1.0), (28, fx - 0.8, fx + 0.8), (29, fx - 0.8, fx + 0.8)],
             mid, lit, dark)
        g.span(FEET, fx - 1, fx + 2, "N")
        g.set(fx - 1, FEET, "p")
        g.set(fx + 2, FEET, "j")
        g.set(fx, 29, "J")
    g.line(12, 24, 11, 28, "o")                         # the shadow between the knees
    # THE COAT TAILS, long, swinging out behind him.
    mass(g, [(18, 7, 12), (22, 6, 12), (25, 5, 10), (26, 5, 9)], "e", "l", "E")
    fold(g, [(9, 19), (7, 25)], "E", "l")
    g.set(6, 26, "A")
    g.set(10, 26, "A")
    # THE COAT: bowed forward from the hips, the shoulders out over the knees.
    mass(g, [(12, 11, 17), (15, 9, 18), (19, 9, 17), (23, 9, 16), (24, 10, 15)], "e", "l", "E")
    fold(g, [(11, 15), (10, 21)], "E", "l")
    fold(g, [(14, 14), (13, 21)], "A", None)
    g.line(9, 23, 16, 23, "o")                          # the coat's hem over the breeches
    g.span(24, 10, 15, "E")
    # THE WAISTCOAT down the front, pale, and its buttons in old gold.
    for y in range(14, 23):
        g.set(16, y, "l" if y % 4 else "L")
        g.set(17, y, "e")
    for y in (15, 18, 21):
        g.set(16, y, "u")
    # RED FACINGS: the collar standing behind the neck, the lapel down the coat's front edge.
    g.line(12, 11, 16, 11, RED_LIT)
    g.line(12, 12, 15, 12, RED)
    g.set(11, 12, RED_HI)
    g.line(15, 13, 15, 22, RED)
    g.set(15, 13, RED_LIT)
    g.set(15, 17, RED_SH)
    # THE FAR ARM, folded behind his back, a red cuff at the small of it.
    _limb(g, 10, 13, 8, 18, 1.2, "E", "e", "A")
    g.rect(8, 18, 10, 19, RED)
    g.set(8, 18, RED_LIT)
    # THE HEAD, bowed low: a tie-wig of bone, the queue tied in black at the nape.
    blob(g, [(15.0, 11.0, 2.5), (14.0, 12.0, 2.2)], "k", "s", "n")
    g.set(13, 9, "S")
    for (x, y) in ((12, 12), (13, 13), (12, 11)):
        g.set(x, y, "n")                                # the curls over the ear
    g.set(11, 11, "J")                                  # the black ribbon
    g.set(11, 12, "o")
    g.set(10, 13, "n")
    # the face, turned down: a void under the front of the wig
    g.rect(16, 11, 17, 13, "o")
    g.set(15, 14, "o")
    g.set(17, 10, "n")
    # THE NEAR ARM, raised up and out, the hand flat under the plate; a napkin over the forearm.
    _limb(g, 16, 14, 20, 11, 1.3, "e", "l", "E")
    _limb(g, 20, 11, 22, 8, 1.1, "e", "l", "E")
    g.rect(21, 8, 22, 9, RED)
    g.set(21, 8, RED_LIT)
    g.rect(22, 7, 23, 7, "k")
    for (x, y, ch) in ((18, 12, "L"), (19, 13, "l"), (18, 13, "l"), (18, 14, "e"), (19, 14, "E"),
                       (17, 14, "L")):
        g.set(x, y, ch)
    # THE SALVER: the rim of the plate seen from just below, and the cloche on it.
    g.span(6, 17, 29, "t")
    g.span(7, 18, 28, "R")
    g.set(17, 6, "r")
    g.set(29, 6, "q")
    g.set(28, 7, "q")
    g.set(17, 6, "T")
    for i, y in enumerate(range(2, 6)):
        hw = (1.6, 3.2, 4.2, 4.6)[i]
        g.span(y, 23.5 - hw, 23.5 + hw, "r")
        g.set(23.5 - hw, y, "t")
        g.set(23.5 + hw, y, "q")
        if i >= 1:
            g.set(23.5 + hw - 1, y, "R")
    g.span(5, 20, 27, "R")
    g.set(21, 3, "T")
    g.set(20, 4, "t")
    g.line(22, 4, 24, 4, "R")                           # a dent the light picks out
    g.set(23, 3, "t")
    g.set(26, 4, "q")
    rim(g, 2, 29, skip="o")
    contact(g, 5, 18)
    eyes(g, 16, 12)
    return g.rows()


# =================================================================================================
# FAST - Court Hound. Also the Huntsman's minion.
# =================================================================================================

def hound():
    """A lean sighthound of the house, pale as bone, built from masses - and arched.

    The read is THE ARCH: a deep keel of a chest at the front, a waist tucked up almost to the
    spine, and the back rising over the loin and falling to a sloping croup, all on long thin legs
    at a trot, with a narrow head held out low on a long neck and a thin tail swept down and curled
    at its tip. Filled black it is a bow on stilts, where the Chainhound was a plank and the
    Bridge's war-dog is a long low line. Bone, carried in its base and shade stops so it is the
    palest body in the chapter without being a light, with a red collar of the house.
    """
    g = G(CELL)
    mid, lit, dark, deep, crown = "n", "k", "m", "J", "s"
    # FAR LEGS, a stop darker: the hind reaching forward, the fore pushing back.
    _limb(g, 12, 19, 12, 24, 0.9, dark, mid, deep)
    _limb(g, 12, 24, 13, 29, 0.6, dark, mid, deep)
    g.span(FEET, 13, 14, deep)
    _limb(g, 21, 21, 19, 25, 0.8, dark, mid, deep)
    _limb(g, 19, 25, 18, 29, 0.6, dark, mid, deep)
    g.span(FEET, 17, 18, deep)
    # THE MASSES: a haunch, the arched loin, a deep chest.
    blob(g, [(10.5, 17.5, 3.0), (12.0, 16.0, 2.4)], mid, lit, dark)
    blob(g, [(16.0, 14.6, 2.3), (14.0, 15.0, 2.2)], mid, lit, dark)
    blob(g, [(21.0, 16.5, 3.0), (21.0, 19.0, 2.8), (19.0, 17.0, 2.4)], mid, lit, dark)
    # the tucked waist between them: a thin band under the loin, rising to it
    for x in range(13, 19):
        top = 14 if x in (15, 16) else 15
        bot = {13: 18, 14: 17, 15: 16, 16: 17, 17: 18, 18: 19}[x]
        for y in range(top, bot + 1):
            if g.get(x, y) == ".":
                g.set(x, y, mid)
        g.set(x, bot, dark)
    for x in range(10, 22):
        col = [y for y in range(8, 24) if inside(g, x, y)]
        if col:
            g.set(x, col[0], crown if 12 <= x <= 17 else lit)   # the topline catches the key
    # ribs over the chest, the shoulder blade, the thigh
    for (x0, y0, x1, y1) in ((19, 18, 18, 20), (21, 18, 20, 21), (17, 16, 17, 18)):
        g.line(x0, y0, x1, y1, dark)
    g.line(22, 15, 21, 18, lit)
    g.line(9, 16, 11, 19, dark)
    g.set(12, 16, lit)
    # feathering under the chest, the longer coat hanging
    for (x, y, ch) in ((19, 21, lit), (20, 22, mid), (21, 22, dark), (22, 22, mid), (18, 20, dark)):
        g.set(x, y, ch)
    # THE NECK, long and reaching low and forward.
    for i, x in enumerate(range(22, 26)):
        top = 13.5 + i * 0.3
        for y in range(int(top), int(top) + 3):
            g.set(x, y, mid)
        g.set(x, int(top), lit)
        g.set(x, int(top) + 3, dark)
    # THE RED COLLAR
    g.line(22, 13, 23, 17, RED_LIT)
    g.line(23, 13, 24, 17, RED)
    g.set(24, 17, RED_SH)
    # THE HEAD: a long narrow wedge, the skull small, the muzzle tapering to the nose.
    g.rect(25, 12, 27, 14, mid)
    g.span(11, 25, 27, lit)
    for (x, y, ch) in ((28, 13, mid), (28, 14, dark), (29, 13, mid), (29, 14, dark), (30, 14, "J"),
                       (28, 12, lit), (29, 12, lit), (26, 15, dark), (27, 15, deep), (25, 15, dark)):
        g.set(x, y, ch)
    g.set(25, 11, crown)
    g.set(24, 11, dark)                                 # the rose ear, folded back
    g.set(24, 12, deep)
    g.set(23, 11, dark)
    # LONG NEAR LEGS at a trot: the hind thrown back with its hock out, the fore reaching forward.
    _limb(g, 10, 19, 8, 24, 1.2, mid, lit, dark)
    _limb(g, 8, 24, 7, 29, 0.7, mid, lit, dark)
    g.span(FEET, 6, 8, dark)
    g.set(6, FEET, lit)
    _limb(g, 20, 21, 22, 25, 0.9, mid, lit, dark)
    _limb(g, 22, 25, 23, 29, 0.6, mid, lit, dark)
    g.span(FEET, 23, 25, dark)
    g.set(23, FEET, lit)
    # THE TAIL: swept down from the croup, curled up at its end, feathered.
    for (x, y, ch) in ((8, 16, mid), (7, 17, mid), (6, 18, lit), (6, 19, mid), (5, 20, lit),
                       (5, 21, mid), (5, 22, dark), (6, 23, dark), (7, 23, mid), (8, 22, lit),
                       (6, 20, dark), (6, 21, deep)):
        g.set(x, y, ch)
    rim(g, 8, 29, skip="o")
    contact(g, 5, 26)
    g.set(26, 12, "Y")
    g.set(26, 13, "y")
    return g.rows()


# =================================================================================================
# HEAVY - Household Guard. A guard behind a kite shield with no device on it.
# =================================================================================================

def guard():
    """A guard of his doors in mail and a nasal helm, behind a tall kite shield.

    The attachment is THE KITE: rounded across the top, straight down the sides and tapering to a
    point that nearly touches the floor, held before him and curved round him, so its far third
    falls into shade. The field is the house's red and PLAIN - his house has no arms but him
    (court_walls.py's banners have none either) - bound in a steel rim with rivets round it at
    uneven spacing and one gouge across it. Behind it the guard is steel, the garrison's row: a
    conical helm with a nasal, a mail coif, the hem of a linen surcoat showing under the shield,
    and a sword hung behind his leg.
    """
    g = G(CELL)
    # THE SWORD, hung behind the far leg.
    g.line(9, 20, 5, 28, "q")
    g.line(8, 20, 4, 28, "r")
    g.set(9, 19, "t")
    g.set(10, 20, "R")
    # LEGS: mail chausses, a stride apart, boots.
    for (x, lit) in ((8, "r"), (12, "R")):
        g.rect(x, 24, x + 2, FEET - 1, "R")
        g.line(x, 24, x, FEET - 1, lit)
        g.line(x + 2, 24, x + 2, FEET - 1, "q")
        for y in range(24, FEET - 1, 2):
            g.set(x + 1, y, "q")
        g.span(FEET, x - 1, x + 3, "j")
        g.set(x - 1, FEET, "p")
        g.span(28, x, x + 2, "N")
    # THE BODY: mail under a linen surcoat, broad at the shoulder.
    mass(g, [(11, 9, 15), (14, 7, 16), (20, 7, 16), (24, 7, 16)], "R", "r", "q")
    for y in range(12, 24):
        for x in range(8, 16):
            if g.get(x, y) == "R" and (x + y) % 2 == 0:
                g.set(x, y, "q" if (x + 2 * y) % 3 else "r")     # the mail's rings
    mass(g, [(16, 8, 15), (22, 7, 15), (25, 6, 15)], "e", "l", "E")
    fold(g, [(10, 17), (9, 25)], "E", "l")
    fold(g, [(13, 18), (13, 25)], "A", None)
    g.span(16, 8, 15, "o")                              # the belt
    g.set(9, 16, "u")
    for x in (7, 9, 12, 14):
        g.set(x, 25, "A")                               # the surcoat's dagged hem
    cut(g, (8, 25), (11, 25))
    # the far arm, hanging, a mailed fist
    _limb(g, 8, 12, 6, 20, 1.2, "R", "r", "q")
    g.rect(5, 20, 7, 21, "r")
    g.set(5, 20, "t")
    # THE HELM: a cone with a nasal, over a mail coif.
    for i, y in enumerate(range(4, 11)):
        hw = (0.6, 1.2, 2.0, 2.6, 3.0, 3.3, 3.4)[i]
        g.span(y, 11.5 - hw, 11.5 + hw, "r")
        g.set(11.5 - hw, y, "t")
        g.set(11.5 + hw, y, "q")
    g.set(11, 4, "T")
    g.line(10, 6, 9, 9, "t")
    g.span(10, 8, 15, "R")
    g.set(8, 10, "t")
    g.line(12, 5, 12, 9, "R")                           # the seam up the cone
    mass(g, [(11, 9, 14), (13, 8, 15)], "R", "r", "q")  # the coif round the face
    g.rect(13, 11, 14, 12, "o")
    g.line(14, 10, 14, 13, "t")                         # the nasal
    # THE KITE SHIELD
    def edge(y):
        if y < 10:
            return 15 + (10 - y) * 0.5, 24 - (10 - y) * 0.5
        if y <= 19:
            return 14.0, 25.0
        t = (y - 19) / 9.0
        return 14.0 + 5.0 * t, 25.0 - 5.5 * t
    for y in range(8, 29):
        a, b = edge(y)
        a, b = int(round(a)), int(round(b))
        if b < a:
            continue
        for x in range(a, b + 1):
            u = (x - a) / float(max(1, b - a))
            g.set(x, y, RED_HI if u < 0.22 else (RED_LIT if u < 0.66 else RED))
        g.set(a, y, "t")
        g.set(b, y, "q")
        if b - a > 3:
            g.set(b - 1, y, "R")
        if g.get(a - 1, y) == "." and y > 9:
            g.set(a - 1, y, "o")
    g.span(8, 16, 23, "t")
    g.set(15, 9, "t")
    g.set(24, 9, "q")
    g.set(19, 29, "q")
    g.set(15, 8, "T")
    # the curve of it: a crease of shade a third of the way across, and the gouge
    fold(g, [(21, 9), (21, 19), (20, 25)], RED_SH, None)
    g.line(16, 13, 20, 16, RED_DP)
    g.line(16, 12, 20, 15, RED_HI)
    g.set(17, 12, "Q")
    SB.rivets(g, [(16, 10), (20, 9), (23, 11), (15, 15), (24, 17), (16, 21), (22, 22), (19, 26)], "T", "R")
    rim(g, 3, 29, skip="o")
    contact(g, 5, 23)
    eyes(g, 13, 11)
    return g.rows()


# =================================================================================================
# WALL - Empty Armour. Plate with nobody in it; built, fire behind the visor.
# =================================================================================================

def armour():
    """A suit of parade plate walking on its own, the broadest thing in the chapter.

    The attachment is the SHOULDER LINE: a flat-topped great helm sitting low between two great
    rounded pauldrons, each a bell of lames curving round the shoulder and wider than the helm, so
    filled black it is a box between two domes on two legs. It is the brightest steel in the house -
    polished, and edged in gilt, a lord's armour off its stand - where the Guard's mail is dull.
    Nobody is in it: where the breastplate should meet the faulds there is a gap, and in the gap
    there is his fire. That and the visor slit are all that burn.
    """
    g = G(CELL)
    # LEGS: greaves, knee cops, sabatons, wide apart.
    for (x0, lit) in ((10, "t"), (17, "r")):
        g.rect(x0, 23, x0 + 3, FEET - 1, "r")
        g.line(x0, 23, x0, FEET - 1, lit)
        g.line(x0 + 3, 23, x0 + 3, FEET - 1, "R")
        g.line(x0 + 2, 27, x0 + 2, FEET - 1, "R")
        g.disc(x0 + 1.5, 25.0, 1.4, "r")               # the poleyn
        g.set(x0 + 1, 24, "T" if lit == "t" else "t")
        g.set(x0 + 2, 26, "q")
        g.span(27, x0, x0 + 3, "R")
        g.span(FEET, x0 - 1, x0 + 4, "R")
        g.span(FEET - 1, x0 + 3, x0 + 4, "r")
        g.set(x0 - 1, FEET, "t")
        g.set(x0 + 4, FEET, "q")
    cut(g, (14, 26), (15, 26), (16, 26), (14, 25), (16, 25))
    # THE FAULDS: two lames, arched, flaring over the hips.
    for (y, a, b, tone) in ((20, 11, 20, "r"), (22, 10, 21, "r")):
        g.span(y, a, b, tone)
        g.span(y + 1, a, b, "R")
        g.set(a, y, "t")
        g.set(b, y, "q")
        g.set(b, y + 1, "q")
    g.span(24, 11, 13, "o")
    g.span(24, 17, 20, "o")
    g.set(15, 22, "q")
    g.set(15, 23, "q")
    # THE GAP where the breastplate should meet the faulds - empty, and his fire in it.
    g.span(19, 12, 19, "o")
    for (x, ch) in ((13, "~"), (14, "="), (15, "-"), (16, "="), (18, "~")):
        g.set(x, 19, ch)
    # THE BREASTPLATE: rounded, a ridge down its middle, gilt at the neck.
    blob(g, [(15.5, 14.0, 4.2), (15.5, 15.5, 4.2)], "r", "t", "R")
    g.line(15, 11, 15, 18, "t")
    g.line(16, 11, 16, 18, "R")
    g.set(14, 13, "T")
    g.span(18, 13, 18, "R")
    g.line(18, 14, 19, 16, "q")
    g.span(11, 13, 18, "g")
    g.set(13, 11, "G")
    g.set(18, 11, "u")
    # ARMS hanging under the pauldrons: vambraces, elbow cops, gauntlets.
    for (x, lit, dark) in ((6, "t", "R"), (24, "r", "q")):
        g.rect(x, 15, x + 1, 22, "r")
        g.line(x, 15, x, 22, lit)
        g.line(x + 1, 15, x + 1, 22, dark)
        g.disc(x + 0.5, 18.5, 1.3, "r")
        g.set(x, 18, lit)
        g.set(x + 1, 19, dark)
        g.rect(x - 1, 22, x + 2, 24, "R")
        g.span(22, x - 1, x + 2, lit)
        g.set(x + 2, 24, "q")
        g.set(x - 1, 24, "r")
    g.line(8, 16, 8, 21, "o")
    g.line(23, 16, 23, 21, "o")
    # THE PAULDRONS: rounded bells over each shoulder, three lames curving round them.
    for (cx, side) in ((9.5, -1), (21.5, 1)):
        rx, ry = 4.8, 3.6
        blob(g, [(cx, 12.0, 3.6), (cx - 1.2 * side * -1, 12.6, 3.4), (cx, 13.2, 3.4)],
             "r", "t", "R")
        for (k, tone) in ((1.6, "o"), (3.2, "R")):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                u = (x - cx) / rx
                if abs(u) >= 1.0:
                    continue
                y = 12.6 + ry * math.sqrt(1.0 - u * u) - k + 1.2
                if inside(g, x, y):
                    g.set(x, y, tone)
                    if tone == "o" and inside(g, x, y + 1):
                        g.set(x, y + 1, "t" if x < cx else "r")
        # the gilt edge round the bottom lame
        for x in range(int(cx - rx) + 1, int(cx + rx)):
            col = [y for y in range(8, 19) if g.get(x, y) in ("r", "t", "R", "T")]
            if col:
                g.set(x, col[-1], "g" if x % 3 else "G")
    g.set(7, 9, "T")
    # THE GREAT HELM: flat-topped, sitting low between the pauldrons, the visor slit alight.
    g.rect(13, 2, 18, 10, "r")
    g.line(13, 2, 18, 2, "t")
    g.line(13, 3, 13, 10, "t")
    g.line(18, 3, 18, 10, "R")
    g.line(17, 4, 17, 10, "R")
    g.set(13, 2, "T")
    g.span(3, 14, 17, "R")                              # the crown plate's seam
    g.line(15, 4, 15, 10, "g")                          # the gilt band down its face
    g.set(15, 4, "G")
    g.line(16, 7, 16, 10, "u")
    g.span(6, 13, 18, "o")                              # the visor slit
    for (x, y) in ((14, 8), (17, 8), (14, 9)):
        g.set(x, y, "q")                                # breaths
    g.span(10, 14, 17, "R")
    # UNDER THE SHOULDERS, A STOP DOWN. Drawn whole in steel's base and lit, the suit was the
    # brightest body in a swarm it is a third of (the Wall band) and read as the player's light, not
    # his (art-direction.md 4c). The helm and the pauldrons keep the polish; everything they
    # overhang - arms, breastplate, faulds, legs - is in their shadow, steel's shade and base.
    down = {"T": "t", "t": "r", "r": "R", "R": "q"}
    for y in range(15, FEET + 1):
        for x in range(CELL):
            ch = g.get(x, y)
            if ch in down and not (y < 17 and (x < 9 or x > 22)):
                g.set(x, y, down[ch])
    rim(g, 2, 29, skip="o")
    contact(g, 8, 23)
    mark(g, 14, 6)
    g.set(17, 6, "~")
    return g.rows()


# =================================================================================================
# RANGED - Falconer. His hawk is what he throws.
# =================================================================================================

def falconer():
    """A falconer in hunting green, his fist raised out from his side with the hawk on it.

    The attachment is THE HAWK ON THE GLOVE: a bird upright on a raised fist, clear of his head, its
    wings lifted from its back and half open as a hawk's are the moment before it goes, its tail
    hanging below the fist and the jesses dangling. Filled black it is a narrow hooded figure with a
    bird held up beside it - the caster's detached orb, made a thing he keeps on his hand. The glove
    is long and tan, the hawk slate above and barred bone below, and the hawk's eye is his mark too:
    it is the thing that flies at you.
    """
    g = G(CELL)
    # LEGS: dark hose, tan boots.
    for (x, fx, mid, lit, dark) in ((12, 11, "p", "P", "N"), (15, 16, "N", "p", "j")):
        mass(g, [(22, x - 1, x + 1), (27, fx - 0.8, fx + 0.8)], mid, lit, dark)
        g.span(26, fx - 1, fx + 1, BR_SH)
        g.span(27, fx - 1, fx + 1, BR)
        g.span(28, fx - 1, fx + 1, BR)
        g.span(29, fx - 1, fx + 1, BR_SH)
        g.span(FEET, fx - 1, fx + 2, BR_DP)
        g.set(fx - 1, 27, BR_LIT)
    # THE TUNIC: belted, to the knee, flared a little.
    mass(g, [(12, 11, 16), (16, 10, 17), (21, 9, 18), (24, 8, 18)], GR, GR_LIT, GR_SH)
    fold(g, [(12, 17), (10, 24)], GR_SH, GR_LIT)
    fold(g, [(15, 18), (16, 24)], GR_DP, None)
    for (x, d) in zip(range(8, 19), [1, 0, 1, 1, 0, 2, 1, 0, 1, 1, 0]):
        for k in range(1, d + 1):
            g.set(x, 24 + k, GR_SH)
    g.span(17, 10, 17, "o")                             # the belt
    g.span(18, 10, 17, BR_SH)
    g.set(13, 18, "u")
    # THE HAWKING BAG at his hip, on its strap.
    g.line(16, 13, 10, 18, BR_SH)
    g.rect(7, 18, 10, 22, BR)
    g.span(18, 7, 10, BR_LIT)
    g.line(10, 19, 10, 22, BR_SH)
    g.set(8, 20, BR_DP)
    g.set(7, 18, BR_HI)
    g.span(23, 7, 10, "o")
    # THE HOOD with its short dagged cape over the shoulders, and the tail down his back.
    mass(g, [(5, 12, 14), (7, 11, 16), (9, 11, 16), (11, 11, 16)], GR, GR_LIT, GR_SH)
    mass(g, [(11, 10, 17), (13, 9, 18)], GR_LIT, GR_HI, GR)
    for x in range(9, 19, 2):
        g.set(x, 14, GR)
        g.set(x + 1, 14, GR_SH)
    g.line(9, 15, 17, 15, "o")
    for (x, y) in ((11, 5), (10, 6), (9, 7), (9, 8), (8, 9), (8, 10), (8, 11), (7, 12)):
        g.set(x, y, GR_LIT if y < 8 else GR)            # the liripipe, hanging off the peak
        g.set(x + 1, y, GR_SH)
    roster.void(g, 8, 11, 1.0, 1.3, cx=14.8)
    g.set(16, 8, GR_SH)
    g.set(13, 6, GR_HI)
    # THE FAR ARM, hanging behind the bag.
    _limb(g, 11, 13, 9, 17, 1.0, GR_SH, GR, GR_DP)
    # THE GLOVED ARM, out from his side, the fist raised clear of his head.
    _limb(g, 17, 13, 20, 15, 1.3, GR, GR_LIT, GR_SH)
    _limb(g, 20, 15, 23, 12, 1.4, BR, BR_LIT, BR_SH)
    g.set(19, 14, BR_LIT)                               # the gauntlet's cuff
    g.set(19, 16, BR_SH)
    g.rect(23, 11, 25, 12, BR)
    g.set(23, 11, BR_LIT)
    g.set(25, 12, BR_SH)
    # THE JESSES dangling from the fist, a bell on one.
    g.line(25, 13, 26, 16, BR_SH)
    g.line(24, 13, 24, 15, BR)
    g.set(26, 17, "G")
    g.set(26, 18, "u")
    # THE HAWK: a tail hanging under the fist, a barred breast, a slate back and wings raised open.
    g.line(26, 11, 27, 14, "R")
    g.line(27, 11, 28, 14, "q")
    g.set(27, 15, "q")
    blob(g, [(25.0, 8.0, 2.0), (25.5, 9.0, 1.8)], "k", "s", "n")
    for (x, y) in ((24, 8), (26, 8), (25, 9), (24, 10), (26, 10)):
        g.set(x, y, "n")                                # the bars
    g.set(24, 7, "S")
    # the wings, raised from the shoulders and half open - two fans, the far one behind and darker
    for (y, a, b) in ((2, 20, 21), (3, 20, 22), (4, 21, 23), (5, 21, 24), (6, 22, 24), (7, 23, 24)):
        g.span(y, a, b, "R")
        g.set(a, y, "r")
        if b - a >= 2:
            g.set(a + 1, y, "t" if y < 5 else "r")
    for (y, a, b) in ((2, 28, 29), (3, 27, 29), (4, 26, 29), (5, 26, 28), (6, 26, 27)):
        g.span(y, a, b, "q")
        g.set(a, y, "R")
    for (x, y) in ((20, 2), (21, 3), (22, 5), (29, 2), (29, 4)):
        g.set(x, y, "J")                                # the primaries' tips, split
    g.line(21, 4, 23, 6, "q")                           # the coverts' edge across the near wing
    # the head over them, the hooked beak forward
    g.rect(25, 4, 26, 6, "R")
    g.set(25, 4, "r")
    g.set(24, 6, "k")
    g.set(27, 5, "G")
    g.set(27, 6, "u")
    rim(g, 2, 29, skip="o")
    contact(g, 6, 20)
    eyes(g, 14, 9)
    g.set(26, 5, "y")                                   # the hawk's eye
    return g.rows()


# =================================================================================================
# TURRET - Candelabrum. A standing iron candelabrum; built, its candles burn his fire.
# =================================================================================================

def candelabrum():
    """A standing candelabrum of black iron as tall as a man, on three clawed feet.

    The attachment is THE BRANCHING: five arms out of one stem, the outer pair sweeping wide and low
    and the inner pair up, each ending in a drip-pan and a candle, so filled black it is a
    many-armed tree of iron. The candles are bone-pale wax, run down the iron in drips, and they burn
    his fire - built, as every turret is - and one is guttered to a stub, so the five are not a row.
    The stem is knopped twice, which is what makes it furniture and not a pole.
    """
    g = G(CELL)
    iron, lit, dark = "R", "r", "q"
    # THE FEET: three legs splaying from a knop, clawed.
    g.disc(15.5, 26.0, 1.8, iron)
    g.set(14, 25, lit)
    for (x0, x1, y1) in ((14, 9, 29), (17, 22, 29), (15, 15, 29)):
        g.line(x0, 26, x1, y1, iron)
        g.line(x0, 27, x1, y1 + 1, dark)
    g.span(FEET, 8, 10, dark)
    g.span(FEET, 21, 23, dark)
    g.set(8, FEET, lit)
    g.set(21, FEET, lit)
    g.span(FEET, 14, 16, dark)
    g.set(14, FEET, lit)
    # THE STEM, knopped twice.
    g.rect(15, 13, 16, 25, iron)
    g.line(15, 13, 15, 25, lit)
    g.line(16, 13, 16, 25, dark)
    for (y, r) in ((21.0, 1.8), (17.0, 1.5)):
        g.disc(15.5, y, r, iron)
        g.set(14, int(y) - 1, "t")
        g.set(14, int(y), lit)
        g.set(17, int(y) + 1, dark)
    # wax run down the stem
    for (x, y0, y1) in ((15, 14, 19), (16, 22, 24)):
        g.line(x, y0, x, y1, "k")
        g.set(x, y0, "s")
    # THE BOWL where the arms leave the stem
    g.span(12, 12, 19, iron)
    g.span(11, 13, 18, lit)
    g.span(13, 13, 18, dark)
    g.set(12, 12, "t")
    # THE ARMS: outer pair wide and low, inner pair up, each to a drip-pan.
    def arm(pts, pan_x, pan_y):
        for (xa, ya), (xb, yb) in zip(pts, pts[1:]):
            g.line(xa, ya, xb, yb, iron)
            g.line(xa, ya + 1, xb, yb + 1, dark)
        g.span(pan_y, pan_x - 1, pan_x + 1, lit)
        g.span(pan_y + 1, pan_x - 1, pan_x + 1, dark)
        g.set(pan_x - 2, pan_y, "t")
    arm([(13, 12), (9, 13), (6, 12), (5, 10)], 5, 10)
    arm([(18, 12), (22, 13), (25, 12), (26, 10)], 26, 10)
    arm([(14, 11), (12, 10), (11, 8)], 11, 8)
    arm([(17, 11), (19, 9), (20, 7)], 20, 7)
    # the curls where each outer arm turns up
    g.set(7, 14, iron)
    g.set(8, 14, dark)
    g.set(24, 14, iron)
    g.set(23, 14, dark)
    # THE CANDLES: bone wax, of different lengths, drips down them; and the flames.
    def candle(x, top, bottom, lit_it=True):
        for y in range(top, bottom + 1):
            g.set(x, y, "s")
            g.set(x + 1, y, "k" if y > top else "n")
        g.set(x - 1, bottom, "n")
        g.set(x + 1, bottom, "m")
        if lit_it:
            flame(g, x, top - 1, 3)
    candle(15, 6, 10)                                  # the tallest, in the middle
    candle(5, 6, 9)
    candle(26, 7, 9)
    candle(11, 5, 7)
    # the guttered one: a stub, its wick dead, a thread of smoke
    g.set(20, 6, "k")
    g.set(21, 6, "n")
    g.set(20, 5, "J")
    g.set(20, 4, "e")
    g.set(21, 3, "E")
    # drips down the outer pans
    for (x, y) in ((4, 11), (27, 11), (12, 8)):
        g.set(x, y, "k")
    rim(g, 2, 29, skip="o" + FIRE, min_run=2)
    contact(g, 8, 23)
    return g.rows()


# =================================================================================================
# CHARGER - Duellist. A courtier lunging with a rapier.
# =================================================================================================

def duellist():
    """A courtier fully out in a lunge, the blade a thread from his fist to the edge of the cell.

    The pose is the telegraph, as every charger's is: the front knee bent over the foot, the back
    leg straight behind, the body thrown forward and low, the free arm raised behind for balance,
    and the rapier out level at the shoulder. Filled black it is a long low diagonal with a line
    out of its front - nothing else in the house is that wide on two legs. A wool doublet, a ruff of
    bone at the neck, a short violet cape flying off the back shoulder, and a feather in his cap.
    """
    g = G(CELL)
    # THE BACK LEG: straight, from the hip down and back to a planted foot.
    mass(g, [(19, 12, 15), (24, 9, 11), (28, 6, 8), (29, 5, 7)], "b", "c", "a")
    g.set(5, 29, "m")
    g.set(6, 29, "n")
    g.span(FEET, 3, 7, "N")
    g.set(3, FEET, "p")
    g.set(7, FEET, "j")
    g.line(13, 21, 8, 27, "a")
    # THE FRONT LEG: thigh near level, the knee over the foot.
    mass(g, [(18, 15, 19), (21, 18, 22), (23, 20, 22)], "c", "d", "b")
    mass(g, [(23, 20, 22), (29, 20, 22)], "n", "k", "m")    # the stocking
    g.line(22, 24, 22, 28, "m")
    g.span(FEET, 20, 24, "N")
    g.set(20, FEET, "p")
    g.set(24, FEET, "j")
    g.span(23, 19, 22, "o")                             # the garter under the knee
    g.set(19, 23, RED)
    # THE TRUNK HOSE: puffed, slashed with violet.
    blob(g, [(15.5, 18.5, 2.8), (17.5, 18.0, 2.4)], "c", "d", "b")
    for (x, y) in ((14, 17), (16, 19), (18, 17), (15, 20)):
        g.set(x, y, "w")
        g.set(x, y + 1, "x")
    # THE DOUBLET: the body thrown forward, slashed sleeves, a row of buttons.
    mass(g, [(11, 16, 21), (13, 14, 21), (16, 13, 20)], "c", "d", "b")
    fold(g, [(15, 13), (14, 16)], "b", "d")
    for y in (12, 14):
        g.set(20, y, "u")
    g.span(16, 14, 20, "o")
    # THE CAPE, short, flying back off the far shoulder.
    mass(g, [(10, 13, 17), (12, 10, 16), (14, 8, 14), (16, 7, 11)], "w", "v", "x")
    fold(g, [(12, 11), (9, 15)], "x", "v")
    g.set(7, 16, "X")
    g.set(10, 16, "X")
    cut(g, (8, 16))
    # THE FREE ARM, raised behind, the hand turned forward.
    _limb(g, 14, 11, 11, 7, 1.0, "c", "d", "b")
    g.rect(10, 5, 11, 6, "k")
    g.set(10, 5, "s")
    # THE RUFF: a ring of bone at the neck, pleated.
    for (x, y, ch) in ((18, 9, "S"), (19, 9, "s"), (20, 9, "s"), (21, 9, "k"), (17, 10, "s"),
                       (18, 10, "n"), (19, 10, "k"), (20, 10, "n"), (21, 10, "k"), (22, 10, "n"),
                       (18, 11, "k"), (21, 11, "m")):
        g.set(x, y, ch)
    # THE HEAD and the cap, its feather trailing back.
    blob(g, [(20.0, 6.5, 2.0)], "x", "w", "X")
    g.rect(19, 7, 21, 8, "o")
    g.span(4, 18, 22, "x")
    g.span(5, 17, 23, "w")
    g.set(17, 5, "v")
    g.set(23, 5, "X")
    for (x, y, ch) in ((17, 4, "s"), (16, 3, "s"), (15, 3, "k"), (14, 3, "k"), (13, 4, "n"),
                       (16, 4, "n"), (14, 4, "m")):
        g.set(x, y, ch)                                 # the feather
    # THE SWORD ARM, straight out at the shoulder; the swept hilt; the blade.
    _limb(g, 20, 12, 24, 12, 1.0, "c", "d", "b")
    g.set(23, 13, "b")
    g.rect(24, 11, 25, 12, "k")
    g.set(24, 11, "s")
    for (x, y, ch) in ((25, 10, "G"), (26, 11, "g"), (26, 13, "u"), (25, 13, "g"), (26, 12, "G")):
        g.set(x, y, ch)                                 # the swept guard, old gilt
    for x in range(27, 32):
        y = 12 - (x - 27) * 0.25
        g.set(x, y, "T" if x == 31 else "t")
        g.set(x, y + 1, "R" if x < 30 else ".")
    rim(g, 3, 29, skip="o")
    contact(g, 3, 25)
    eyes(g, 20, 7)
    return g.rows()


# =================================================================================================
# EXPLODER - Censer. Faces LEFT: the stand-in sets SpriteFacesRightByDefault = false.
# =================================================================================================

def censer():
    """A great brass censer walking on three paws, swelling with smoke. It goes to the left.

    The attachment is THE PIERCED DOME AND ITS CHAINS: a domed lid pierced in staggered rows over a
    squat bowl, and chains from the bowl's rim up past the lid to one ring above it, as a censer
    hangs - so filled black it is a lamp under its own little gallows. It is swollen: the lid has
    lifted off its seat and smoke is leaking out of the seam and the top holes and streaming back
    behind it as it goes. Brass, the one gold body in the house, carried in the row's middle stops;
    built, so his fire glows in the piercings, and two of them are its mark.
    """
    g = G(CELL)
    # LEGS: three, cast as beasts' legs, the front one reaching left.
    for (x0, x1) in ((11, 8), (16, 16), (20, 23)):
        g.line(x0, 23, x1, 28, BR)
        g.line(x0 + 1, 23, x1 + 1, 28, BR_SH)
        g.set(x0, 23, BR_LIT)
        g.span(29, x1 - 1, x1 + 1, BR)
        g.span(FEET, x1 - 2, x1 + 1, BR_SH)
        g.set(x1 - 2, FEET, BR)
        g.set(x1 - 1, 29, BR_LIT)
    # THE BOWL: squat, banded with beads, a foot ring under it.
    blob(g, [(15.5, 18.0, 5.6), (15.5, 19.5, 5.0)], BR_SH, BR, BR_DP)
    g.span(16, 9, 22, BR)
    g.span(17, 9, 22, BR_SH)
    g.set(9, 16, BR_LIT)
    g.span(23, 12, 19, BR_DP)
    for x in range(10, 22, 2):
        g.set(x, 19, BR_LIT if x < 14 else BR)
        g.set(x + 1, 19, BR_DP)
    g.span(20, 11, 20, BR_DP)
    g.set(12, 22, BR_DP)
    # THE SEAM, lifted, and smoke leaking out of it behind.
    g.span(15, 9, 22, "o")
    for (x, ch) in ((19, "e"), (21, "l"), (22, "e"), (23, "E"), (24, "e"), (25, "E")):
        g.set(x, 15, ch)
    g.set(24, 14, "E")
    # THE DOME: a hemisphere, pierced in staggered rows.
    for i, y in enumerate(range(7, 15)):
        hw = (2.0, 3.2, 4.2, 4.8, 5.2, 5.4, 5.4, 5.0)[i]
        a, b = int(round(15.5 - hw)), int(round(15.5 + hw))
        g.span(y, a, b, BR)
        g.set(a, y, BR_LIT)
        if i < 4:
            g.set(a + 1, y, BR_LIT)
        g.set(b, y, BR_SH)
        g.set(b - 1, y, BR_SH if i > 2 else BR)
    g.set(13, 7, BR_HI)
    g.set(12, 8, BR_LIT)
    g.span(14, 10, 21, BR_SH)
    for (y, xs) in ((9, (13, 16, 19)), (11, (11, 14, 17, 20)), (13, (12, 15, 18))):
        for x in xs:
            g.set(x, y, "o")
            g.set(x, y + 1, BR_DP)
    for (x, y, ch) in ((16, 9, "~"), (14, 11, "="), (17, 11, "~"), (15, 13, "~"), (20, 11, "~")):
        g.set(x, y, ch)                                 # his fire inside it
    # the finial
    g.rect(15, 4, 16, 6, BR)
    g.set(15, 4, BR_LIT)
    g.set(16, 6, BR_SH)
    g.set(15, 5, BR_HI)
    # THE CHAINS from the rim up past the dome to the ring.
    chain(g, [(7, 16), (14, 3)], "t", "R")
    chain(g, [(24, 16), (17, 3)], "r", "q")
    g.disc(15.5, 2.5, 1.0, "r")
    g.set(15, 2, "t")
    g.set(16, 3, "q")
    g.set(15, 3, "o")
    # THE SMOKE out of the top holes, streaming back (right) behind it.
    for (x, y, ch) in ((19, 8, "l"), (20, 7, "e"), (21, 7, "l"), (22, 6, "e"), (23, 6, "E"),
                       (24, 5, "e"), (25, 5, "E"), (26, 6, "e"), (27, 6, "E"), (22, 9, "e"),
                       (24, 9, "E"), (25, 10, "e"), (26, 10, "E"), (27, 11, "E"), (26, 13, "e"),
                       (27, 13, "E"), (28, 12, "E")):
        g.set(x, y, ch)
    rim(g, 2, 29, skip="o" + FIRE)
    contact(g, 5, 25)
    mark(g, 11, 11)
    return g.rows()


# =================================================================================================
# SLAMMER - Usher. Brings his staff of office down.
# =================================================================================================

def usher():
    """An usher of the hall swinging his rod of office down, both fists on it over his head.

    The slammer's rule is that the thing that will land is the highest thing in the cell, so it is
    the KNOB: a gold ball on an ivory rod raised overhead at a slant, the ground showing between the
    rod and his cap. He is short and broad in a gown that falls to his shoes, its wide sleeves
    fallen back off his raised arms, a bone collar of two bands at his throat and a flat black cap.
    Filled black: a gowned lump with a diagonal out of the top of it and a ball on its end - never
    the Seneschal's steward stood beside an upright staff.
    """
    g = G(CELL)
    # THE GOWN: short and broad, to the shoes, flaring from a girdle.
    mass(g, [(13, 10, 17), (18, 9, 18), (23, 7, 19), (28, 6, 20), (29, 6, 20)], "w", "v", "x")
    fold(g, [(11, 19), (9, 28)], "x", "v")
    fold(g, [(14, 20), (14, 28)], "X", None)
    fold(g, [(17, 21), (18, 28)], "X", "v")
    for (x, y) in ((10, 24), (13, 26), (16, 23)):
        g.set(x, y, "V")
    for (x, d) in zip(range(6, 21), [1, 0, 1, 1, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0, 1]):
        if d:
            g.set(x, 29, "X")
    cut(g, (12, 29), (13, 29))
    g.span(FEET, 9, 12, "N")
    g.span(FEET, 14, 17, "j")
    g.set(9, FEET, "p")
    # a girdle and a purse
    g.line(9, 18, 18, 17, "o")
    g.line(9, 19, 18, 18, "X")
    g.rect(15, 19, 17, 21, BR_SH)
    g.set(15, 19, BR_LIT)
    g.set(17, 21, BR_DP)
    # THE HEAD: a flat black cap over a void.
    blob(g, [(13.0, 11.0, 2.3)], "x", "w", "X")
    g.span(8, 10, 16, "N")
    g.span(9, 10, 16, "j")
    g.set(10, 8, "P")
    g.set(11, 8, "p")
    g.span(7, 11, 14, "N")
    g.set(11, 7, "p")
    g.rect(13, 10, 15, 12, "o")
    # THE COLLAR: two bands of bone at the throat.
    g.rect(13, 13, 13, 15, "k")
    g.rect(15, 13, 15, 15, "n")
    g.set(14, 13, "n")
    g.set(13, 13, "s")
    # THE ARMS raised up and forward to the rod, the wide sleeves fallen back to the shoulder.
    _limb(g, 11, 14, 16, 8, 1.4, "w", "v", "x")
    _limb(g, 16, 14, 20, 6, 1.4, "x", "w", "X")
    for (x0, y0, x1, y1) in ((9, 13, 9, 18), (10, 13, 10, 19), (17, 13, 18, 17), (18, 13, 19, 16)):
        g.line(x0, y0, x1, y1, "x" if x0 in (9, 18) else "w")
    g.set(9, 18, "X")
    g.set(19, 16, "X")
    g.line(15, 9, 17, 7, "k")                           # the forearms bared by the sleeves
    g.line(19, 7, 20, 6, "n")
    # THE ROD: ivory, held overhead at a slant, both fists on it, banded in gold.
    roster.shaft(g, 14, 9, 25, 4, "s", "k", "n", width=2)
    for (x, y) in ((18, 7), (23, 5)):
        g.set(x, y, "G")
        g.set(x, y + 1, "u")
    g.rect(16, 7, 17, 8, "k")
    g.set(16, 7, "s")
    g.rect(20, 5, 21, 6, "k")
    g.set(20, 5, "s")
    g.set(21, 6, "n")
    # THE KNOB: a gold ball at the rod's head, the highest thing in the cell.
    g.disc(27.0, 3.8, 2.1, "g")
    g.set(26, 2, "G")
    g.set(25, 3, "G")
    g.set(26, 3, "H")
    g.set(28, 5, "u")
    g.set(29, 4, "u")
    g.set(27, 6, "u")
    g.set(27, 4, "G")
    g.set(28, 2, "g")
    rim(g, 2, 29, skip="o")
    contact(g, 5, 21)
    eyes(g, 14, 11)
    return g.rows()


# =================================================================================================
# SUMMONER - Huntsman. Winds a horn for the court hounds.
# =================================================================================================

def huntsman():
    """The huntsman of the house in his red coat, winding the curled horn to call the hounds.

    The horn is the attachment: brass, from his lips curling forward and up past his own cap to a
    flared bell held high, as a horn is when it is blown - so filled black he is a figure with a hook
    of brass rising out of his face. A red riding coat with long skirts (two stops up the red row, so
    it is not the red runner it walks on), a round black velvet cap, tall boots, and a whip coiled in
    the hanging hand.
    """
    g = G(CELL)
    # BOOTS, tall, black, a stride.
    for (x, fx, mid, lit, dark) in ((12, 11, "N", "p", "j"), (15, 16, "j", "N", "J")):
        mass(g, [(21, x - 1, x + 1), (29, fx - 1, fx + 1)], mid, lit, dark)
        g.span(FEET, fx - 1, fx + 2, "j")
        g.set(fx - 1, FEET, "P")
        g.span(23, fx - 1, fx + 1, "k" if fx < 14 else "n")    # the turned-down top
    # THE COAT: red, long skirts flaring behind.
    mass(g, [(11, 11, 17), (16, 10, 18), (20, 9, 18), (24, 7, 17), (25, 7, 16)], RED_LIT, RED_HI, RED)
    fold(g, [(12, 17), (10, 24)], RED, RED_HI)
    fold(g, [(15, 18), (14, 24)], RED_SH, None)
    g.line(17, 12, 17, 18, RED_SH)
    g.span(17, 10, 17, "o")                             # the belt
    g.set(14, 17, "G")
    for y in (13, 15):
        g.set(16, y, "G")                               # brass buttons
    g.set(8, 25, RED_SH)
    g.set(16, 25, RED_DP)
    cut(g, (12, 25))
    # a pale stock at the throat
    g.span(11, 14, 16, "s")
    g.set(16, 11, "n")
    # THE FAR ARM, hanging, a whip coiled in the hand.
    _limb(g, 11, 13, 9, 19, 1.1, RED, RED_LIT, RED_SH)
    g.rect(8, 19, 9, 20, "k")
    for (x, y) in ((7, 21), (6, 22), (7, 23), (8, 22), (9, 23), (8, 24), (7, 25)):
        g.set(x, y, BR_SH if (x + y) % 2 else BR)
    # THE HEAD and the round velvet cap, its peak to the front.
    blob(g, [(14.5, 7.5, 2.6)], "N", "p", "j")
    g.set(13, 5, "P")
    g.span(9, 16, 18, "j")
    g.set(18, 9, "J")
    g.rect(14, 9, 16, 10, "o")
    g.set(12, 8, "N")
    g.set(13, 10, "o")
    # THE HORN ARM, the hand at the mouth.
    _limb(g, 17, 12, 19, 11, 1.2, RED_LIT, RED_HI, RED)
    g.rect(18, 10, 19, 11, "k")
    g.set(18, 10, "s")
    # THE HORN: from the mouthpiece at his lips, curling forward and up to a flared bell held high.
    cx, cy, r = 21.0, 6.5, 3.8
    for k in range(0, 200, 4):
        a = math.radians(100 - k)                       # from below the centre, round the front, up
        x, y = cx + r * math.cos(a), cy + r * math.sin(a)
        thick = 1 if k < 90 else 2
        g.set(x, y, BR_LIT if k > 110 else BR)
        if thick > 1:
            g.set(x - math.cos(a), y - math.sin(a), BR_SH)
    g.set(20, 10, BR)
    # the bell, flared, opening up and back at the top of the curl
    for (x, y, ch) in ((17, 2, BR_HI), (18, 2, BR_LIT), (19, 2, BR_LIT), (17, 3, BR_LIT),
                       (16, 3, BR_LIT), (18, 3, "o"), (19, 3, BR_SH), (20, 3, BR), (16, 4, BR_SH)):
        g.set(x, y, ch)
    rim(g, 2, 29, skip="o")
    contact(g, 6, 20)
    eyes(g, 15, 9)
    return g.rows()


# =================================================================================================
# MINIBOSS - The Chamberlain. 48 x 48. Keeper of every room.
# =================================================================================================

def chamberlain():
    """The keeper of every room in the house, and the ring he keeps them on, held up like a shield.

    The attachment is THE RING: a hoop of iron as wide as his chest, held out before him on his near
    hand with the ground showing through it, and keys hung all round it - long and short, iron and
    old brass, every one hanging straight down as keys do, the lower ones in a fringe under the ring
    and the upper ones across its middle - so filled black he is a gowned figure holding out a
    toothed wheel. The gown is black velvet carried in stone's upper stops, fur-faced in bone at the
    collar, down the front and round the hem, under a soft cap that droops to one side; he is
    stooped, a key-keeper's stoop. No staff (the Seneschal's), no chains of office, no armour.
    """
    c = 48
    feet = c - 2
    g = G(c)
    # THE GOWN: long, to the floor, stooped at the shoulders.
    mass(g, [(14, 15, 25), (18, 12, 27), (26, 10, 27), (36, 8, 28), (44, 6, 29), (45, 6, 29)],
         "N", "p", "j")
    for pts in ([(14, 22), (11, 34), (9, 44)], [(18, 22), (17, 34), (15, 44)],
                [(24, 26), (25, 44)]):
        fold(g, pts, "j", "p")
    fold(g, [(21, 24), (21, 44)], "J", None)
    for (x, y) in ((12, 30), (16, 38), (11, 40), (23, 34)):
        g.set(x, y, "P")
    # the fur hem, and the front facing down the gown
    fur = "kksknkkskkn"                                 # fur, not a stripe: irregular tufts
    for x in range(6, 30):
        g.set(x, 44, fur[(x * 7) % len(fur)])
        g.set(x, 45, "n" if (x * 5) % 7 < 3 else "k")
    for y in range(20, 45):
        g.set(19, y, fur[(y * 5) % len(fur)])
        g.set(20, y, "n" if (y * 3) % 5 < 2 else "k")
    g.set(6, 44, "S")
    cut(g, (13, 45), (14, 45), (22, 45))
    g.span(feet, 10, 13, "J")
    g.span(feet, 22, 25, "J")
    g.set(10, feet, "N")
    # a girdle, a purse, a seal
    g.line(10, 27, 27, 26, "o")
    g.line(10, 28, 27, 27, "J")
    g.rect(12, 28, 15, 32, BR_SH)
    g.span(28, 12, 15, BR_LIT)
    g.set(15, 32, BR_DP)
    g.set(13, 30, "G")
    # THE FUR COLLAR over the shoulders, broad and soft.
    mass(g, [(13, 14, 25), (15, 11, 27), (18, 10, 28), (19, 12, 26)], "k", "s", "n")
    for (x, y) in ((12, 16), (15, 17), (18, 16), (21, 17), (24, 16), (26, 17), (13, 15), (23, 14)):
        g.set(x, y, "n")
    for (x, y) in ((14, 15), (17, 14), (20, 15), (25, 15), (11, 17)):
        g.set(x, y, "S")
    g.line(10, 19, 27, 19, "o")
    # THE FAR ARM, hanging in its sleeve, the fur cuff.
    mass(g, [(19, 9, 13), (28, 7, 12), (31, 7, 12)], "j", "N", "J")
    g.span(32, 7, 12, "k")
    g.span(33, 7, 12, "n")
    g.set(7, 32, "s")
    g.rect(8, 34, 10, 35, "n")
    # THE HEAD, stooped forward, and the soft cap drooping to one side.
    blob(g, [(21.0, 10.0, 3.6), (22.0, 11.0, 3.0)], "N", "p", "j")
    roster.void(g, 9, 13, 1.6, 1.2, cx=23.5)
    g.set(25, 13, "j")
    blob(g, [(20.5, 5.0, 2.8), (22.5, 4.6, 2.4)], "N", "p", "j")     # the soft crown, puffed
    g.set(19, 3, "P")
    g.line(21, 4, 23, 7, "j")                                       # its folds
    g.set(24, 4, "j")
    g.span(7, 17, 27, "p")                                          # the padded roll
    g.span(8, 17, 27, "N")
    g.span(9, 18, 26, "J")
    g.set(17, 7, "P")
    g.set(27, 8, "j")
    for x in (19, 22, 25):
        g.set(x, 8, "j")
    for (x, y) in ((16, 8), (15, 9), (15, 10), (14, 11), (14, 12)):  # the tail, hanging at his back
        g.set(x, y, "N")
        g.set(x + 1, y, "j")
    g.set(13, 12, "p")
    g.set(24, 7, "G")                                   # a brooch on the roll
    g.set(24, 8, "u")
    # THE RING: an iron hoop as wide as his chest, open, held out before him.
    cx, cy, r = 37.0, 25.0, 7.5
    for k in range(0, 360, 2):
        a = math.radians(k)
        facing = -(math.cos(a) + math.sin(a)) * 0.7071
        x, y = cx + r * math.cos(a), cy + r * math.sin(a)
        g.set(x, y, "T" if facing > 0.85 else ("t" if facing > 0.2 else ("r" if facing > -0.4 else "R")))
        xi, yi = cx + (r - 1.0) * math.cos(a), cy + (r - 1.0) * math.sin(a)
        g.set(xi, yi, "R" if facing > -0.2 else "q")
    # THE KEYS, each hanging straight down from the hoop: a bow, a shank, a bit to one side.
    keys = [(160, 5, "t", "R"), (140, 7, BR_LIT, BR_SH), (120, 4, "r", "q"), (100, 6, "t", "R"),
            (80, 8, BR, BR_SH), (60, 5, "r", "q"), (40, 6, "t", "R"), (20, 4, BR_LIT, BR_SH),
            (235, 3, "t", "R"), (305, 4, BR_LIT, BR_SH)]
    for (ang, length, lit, dark) in keys:
        a = math.radians(ang)
        bx, by = cx + (r + 0.6) * math.cos(a), cy + (r + 0.6) * math.sin(a)
        if math.sin(a) < 0:                             # an upper key hangs inside the ring
            bx, by = cx + (r - 1.6) * math.cos(a), cy + (r - 1.6) * math.sin(a)
        bx, by = int(round(bx)), int(round(by))
        g.set(bx, by, lit)
        g.set(bx + 1, by, dark)
        g.set(bx, by + 1, dark)
        g.set(bx + 1, by + 1, lit)
        for s in range(2, length + 1):
            g.set(bx, by + s, lit)
            g.set(bx + 1, by + s, dark if s < length else ".")
        g.set(bx + 1, by + length, lit)
        g.set(bx + 1, by + length - 1, dark)
    # THE NEAR ARM, out before him, the hand closed on the hoop.
    mass(g, [(18, 24, 28), (21, 26, 31), (23, 28, 32)], "N", "p", "j")
    g.span(22, 28, 32, "k")
    g.span(23, 28, 32, "n")
    g.rect(29, 20, 31, 23, "n")
    g.set(29, 20, "k")
    g.set(30, 21, "k")
    rim(g, 2, 45, skip="o")
    contact(g, 5, 38, y=feet)
    eyes(g, 23, 11)
    return g.rows()


# =================================================================================================

CAST = [
    # stem, pose, cell, (lean, reach, knock), SpriteFrames name
    ("court-lackey", lackey, 32, (0.7, 2.4, 2), "CourtLackeyFrames"),
    ("court-hound", hound, 32, (1.6, 3.4, 2), "CourtHoundFrames"),
    ("court-guard", guard, 32, (0.5, 2.8, 1), "CourtGuardFrames"),
    ("court-armour", armour, 32, (0.4, 3.2, 1), "CourtArmourFrames"),
    ("court-falconer", falconer, 32, (0.9, 2.2, 3), "CourtFalconerFrames"),
    ("court-candelabrum", candelabrum, 32, (0.15, -2.0, 1), "CourtCandelabrumFrames"),
    ("court-duellist", duellist, 32, (1.0, 4.2, 2), "CourtDuellistFrames"),
    # Faces left (see censer), so it leans into its blast and is knocked back the other way.
    ("court-censer", censer, 32, (1.0, -2.2, -3), "CourtCenserFrames"),
    ("court-usher", usher, 32, (0.5, 4.0, 1), "CourtUsherFrames"),
    ("court-huntsman", huntsman, 32, (0.9, 2.6, 2), "CourtHuntsmanFrames"),
    ("court-chamberlain", chamberlain, 48, (0.5, 4.0, 1), "CourtChamberlainFrames"),
]


def build():
    return bestiary_kit.build_cast(CAST, PALETTE)


if __name__ == "__main__":
    # Largest cell first: bestiary_kit.report sizes its contact sheet by the first creature's cell.
    bestiary_kit.run(sorted(CAST, key=lambda c: -c[2]), PALETTE, "court")
