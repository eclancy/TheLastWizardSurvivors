# -*- coding: utf-8 -*-
"""Three drawn looks for Merlin, to choose between. Run: python tools/art/merlin_concepts.py

    tools/art/_merlin_concepts/sheet.png      (gitignored) the comparison: each look at x6, filled
                                               black, its idle and walk, its numbers, and all three at
                                               the end of the cast as the character select lines them up
    tools/art/_merlin_concepts/<key>.png      each look's pose at x8 on its own

    --debug A    the pose at x14 on a numbered grid, for placing pixels
    --lineup AB  the cast with the named looks on the end, in colour and filled black

The cast is read from the shipped portraits (assets/bonelight/characters/<stem>-1.png); a wizard on a
branch not merged yet is read from _merlin_concepts/ref/<stem>-1.png if a copy is put there - the Sibyl's
was, for the sheet of 2026-11-02 (git show wip/wizard-sibyl:assets/bonelight/characters/sibyl-1.png).

`player_merlin.py` ships one of these (its SHIPPING), the way `app_icon.py` ships one of
`icon_concepts.py`'s designs; nothing here writes under assets/.

WHY HE NEEDS A LOOK OF HIS OWN. Eric, 2026-11-02: "I don't actually want the blue wizard to be Merlin.
Let's come up with new art for Merlin. The blue robed wizard should represent player." The game is
named for that figure - the one he missed - so the blue robe, the tall starred cone hat and the forked
staff with the orb in its claw are the player's, and Merlin may wear none of them. He keeps his kit
(Orb of Ages, Warding Hand, The Once and Future) and his unlock; only his body changes.

WHAT EVERY LOOK HOLDS TO

  The player cell and its proportion rules (.ai/world-and-tone.md, "The player at 48x48"): about 2.6
  tall to 1 wide, an off-axis sweep so nothing is mirrored, the staff taller than he is and setting the
  top of the silhouette, mostly the middle and deep of each row with the rim and the light separating
  him. An old man (Eric: "one that uses his magic to protect others"), drawn serious.

  No player pigment and no indigo wool. The four saturated rows are the starters' and the wool is the
  player's own robe (.ai/art-direction.md section 3); Merlin wears material rows, as every wizard after
  the four does, and his one light is Light - his spell is Light and Arcane, and the ward's orb leaves
  from whatever holds it (CharacterVisuals.GetStaffTipCell is that pixel; ORIGIN below, per look).

  Clear of the roster in BOTH silhouette and colour, measured rather than argued: main() prints, for
  each look, its largest filled-silhouette overlap with any wizard in the cast and its nearest wizard by
  colour, so a look that only differs in hue (or only in shape) shows as one.

THE THREE

  A  The Crystal Cave   the enchanter of the court, bare-headed (the one bare crown in the cast): a white
                        fringe, heavy white brows, a forked beard, an ivory mantle with a gold border over
                        a darker under-robe, a straight staff planted at arm's length and crowned with raw
                        quartz - the cave he was sealed in, and the light in it. His free hand held out
                        low over the ground, palm down: Warding Hand. SHIPS (player_merlin.SHIPPING).
  B  The Wood           Myrddin Wyllt, the wild Merlin of the Welsh poems, who lived in the Caledonian
                        wood and rode a stag (the Vita Merlini): a stag's antlers bound on over wild white
                        hair, a wolf's pelt over his shoulders, a russet robe belted with rope, a gnarled
                        thorn staff with the light caught in its knot, his free hand held out low. The
                        boldest silhouette of the three. (The Forest's effigy also wears an antler rack.)
  C  The Counsellor     the king-maker: a wine-red cope with a high standing collar behind his head, a
                        gold chain of office, a charcoal robe, close white hair and a long beard, his free
                        hand raised palm out, and a staff crowned with a gold seven-pointed star, his
                        spell's star ({7/3}), with the light at its heart. Wine sits nearest the
                        Pyromancer's red of the three; the numbers say how near.

TWO NEW MATERIAL ROWS, ivory (A) and russet (B), each a five-tone row by the contract's rule (hi
rotated toward cyan, shade toward indigo with its chroma held, nothing dark below 0.20 saturation),
listed in art-direction.md section 3 and registered with matte so their highlights are settled like
every other row's. Both are registered whichever look ships, so switching SHIPPING needs nothing else.
C needs none: it is the `red` row, which Merlin's sash already had.

THEIR CHARACTERS. Every printable character is a colour already (roster.PALETTE), so a new row lives on
single bytes no other row uses - past \\x18, where Merlin's red ends (anim_sets.PLAYER_RAMPS lists every
one, so the hurt flash and the death fade step each along its own row). Skipped on purpose: \\x19-\\x1f
and \\x7f, because \\x1c-\\x1f are line boundaries to Python's unicode splitlines() and the run is too
short for a row anyway, and \\x85, which is whitespace (NEL) to unicode. So ivory is \\x80-\\x84 and
russet \\x86-\\x8a.

Two limits from the derived animations (roster.py): nothing above row 2 and nothing left of column 4.
"""
from __future__ import print_function

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

from PIL import Image, ImageDraw

import anim_sets
import bonelight as B
import matte
import pixel as P
import roster

G = roster.G
CELL = roster.WIZARD_CELL
FEET = CELL - 2
OUT = os.path.join(HERE, "_merlin_concepts")

LT0, LT1, LT2, LT3 = roster.LT0, roster.LT1, roster.LT2, roster.LT3

# --- the rows -----------------------------------------------------------------------------------

# The `red` row (bonelight), hi / lit / base / shade / deep: the Counsellor's cope. Merlin's sash had it
# first, on these same five characters.
RED0, RED1, RED2, RED3, RED4 = "\x14", "\x15", "\x16", "\x17", "\x18"
RED_RAMP = RED0 + RED1 + RED2 + RED3 + RED4

# IVORY: undyed wool, warm - the Crystal Cave's mantle. Hue 46 -> 41 -> 37 -> 13 -> 325 degrees, so the
# shade turns toward indigo through the warm side as sand cloth's does (cast_sands.py), and nothing dark
# drops under 0.20 saturation (0.30 base, 0.22 shade and deep); L* 93 / 83 / 67 / 42 / 23, the bone row's
# spacing. Warmer than bone (cold: the Sibyl's palla and every beard) and paler than sand cloth (the
# Sands' travellers), so it is neither.
IVORY = ["#F2ECD8", "#DCCDAC", "#B4A07E", "#74605A", "#40323A"]
IV0, IV1, IV2, IV3, IV4 = "\x80", "\x81", "\x82", "\x83", "\x84"
IVORY_RAMP = IV0 + IV1 + IV2 + IV3 + IV4

# RUSSET: madder-dyed wool - the Wood's robe. Hue 21 -> 19 -> 14 -> 345 -> 327, saturation 0.49 to 0.68,
# L* 68 / 51 / 35 / 21 / 10: redder and deeper than flesh (so a face beside it stays a face), browner and
# darker than ember (the Pyromancer's red is crimson and reaches L* 70 on its lit stop).
RUSSET = ["#DA9670", "#B4643E", "#84402A", "#52222E", "#2C1220"]
RU0, RU1, RU2, RU3, RU4 = "\x86", "\x87", "\x88", "\x89", "\x8a"
RUSSET_RAMP = RU0 + RU1 + RU2 + RU3 + RU4

PALETTE = dict(roster.PALETTE)
PALETTE.update(dict(zip(RED_RAMP, B.MATERIALS["red"])))
PALETTE.update(dict(zip(IVORY_RAMP, IVORY)))
PALETTE.update(dict(zip(RUSSET_RAMP, RUSSET)))
matte.add_ramp("ivory", IVORY)
matte.add_ramp("russet", RUSSET)


# --- shared pieces ------------------------------------------------------------------------------

def rim(g, skip=("o",)):
    """The bone rim on every row's key-side edge (art-direction.md section 2), on the figure only -
    run before the staff, or the staff wears it and reads as a glowing rod. Never over occlusion: a
    shadow with a rim on it becomes a black object."""
    for y in range(3, FEET):
        row = g.g[y]
        for x in range(1, CELL):
            if row[x] != "." and row[x - 1] == ".":
                if row[x] not in skip and row[x] != "W":
                    g.set(x - 1, y, "W")
                break


def weave(g, x0, x1, y0, y1, base, into, rate=19, bias_x=None):
    """A sparse, irregular break of one stop into a mass, so it has an inside without banding."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if g.get(x, y) != base:
                continue
            h = (x * 37 + y * 61 + (x * y) % 7) % rate
            if h < 2 + (1 if bias_x is not None and x < bias_x else 0):
                g.set(x, y, into)


def hem_walk(g, phase, y0, toe_xs, cx, shoe=("N", "j", "o"), amount=1.4):
    """A robed walk, as the current figures do it: the hem swings toward the leading foot and that
    foot's toe shows under it at each contact."""
    if phase is None:
        return
    c, _ln, _lf = roster.stride(phase)
    hem = amount * c
    roster.sway(g, y0, hem, FEET)
    if abs(c) > 0.5:
        roster.toe(g, (toe_xs[0] if c > 0 else toe_xs[1]) + int(round(hem)), w=3, shoe=shoe)


def lift_of(phase):
    """The staff is planted on one step and carried through the other."""
    return 0 if phase is None else (1 if math.sin(2.0 * math.pi * phase) > 0.3 else 0)


def beard_mass(g, cx, y0, y1, prof, lean=0.0, lit="s", base="k", shade="n", deep="m"):
    """A beard from a half-width profile [(t, w)], lit on the key side and darkening down and right."""
    for y in range(y0, y1 + 1):
        t = (y - y0) / float(max(1, y1 - y0))
        w = lerp(prof, t)
        c = cx + lean * t
        lo, hi = int(round(c - w)), int(round(c + w - 0.2))
        for x in range(lo, hi + 1):
            d = (x - c) / max(1.0, w) + 1.3 * t
            g.set(x, y, base if d < 0.7 else (shade if d < 1.5 else deep))
        g.set(lo, y, lit if t < 0.75 else base)
        if hi - lo >= 2:
            g.set(hi, y, deep if t > 0.4 else shade)


def lerp(profile, t):
    t = min(1.0, max(0.0, t))
    for i in range(len(profile) - 1):
        (t0, w0), (t1, w1) = profile[i], profile[i + 1]
        if t <= t1:
            return w0 + (w1 - w0) * (t - t0) / max(1e-6, (t1 - t0))
    return profile[-1][1]


def streaks(g, runs, on=("k", "s", "n", "m", "S")):
    for (x, ya, yb, ch) in runs:
        for y in range(ya, yb + 1):
            if g.get(x, y) in on:
                g.set(x, y, ch)


def wood_staff(g, x0, top, bot, knots=()):
    """A two-column shaft of wood (gold's lower stops, which is what wood is in this palette - Wayland's
    haft), lit on the key side, with the grain broken by a few knots so it is not a rule."""
    for y in range(top, bot + 1):
        g.set(x0, y, "g" if (y * 7) % 11 else "G")
        g.set(x0 + 1, y, "u" if (y * 5) % 9 else "J")
    for (y, ch) in knots:
        g.set(x0, y, ch)
    g.set(x0 + 1, bot, "J")


def staff_hand(g, x, y):
    """Fingers closed round a two-column shaft at column x: the back of the hand behind it, the
    knuckles over its near column, flesh so it is a hand and not a gauntlet."""
    g.rect(x + 2, y, x + 3, y + 2, "i")
    g.set(x + 3, y, "h")
    for k, ch in enumerate(("f", "h", "h")):
        g.set(x, y + k, ch)
        g.set(x + 1, y + k, "h" if k else "f")
        g.set(x - 1, y + k, "o" if k else ".")
    g.set(x + 1, y + 3, "I")
    g.set(x, y + 2, "i")


# =================================================================================================
# A - THE CRYSTAL CAVE
# =================================================================================================

A_CX = 26
A_STAFF = 9
A_ORIGIN = (9.5, 7.0)


def crystal(g, lift=0):
    """Raw quartz on the staff's head: a tall point and two small ones leaning off it, set in gold wire.
    The stone is steel's cold stops at its facet edges - clear rock - with the light ramp inside it, its
    core up the key-side face, so it is a lamp seen through a prism and not a flame."""
    x0 = A_STAFF
    t = 3 - lift
    # (y, [(x, ch), ...]) from the tip down to the seat
    body = [
        (t, [(x0 + 1, LT1)]),
        (t + 1, [(x0, "T"), (x0 + 1, LT0)]),
        (t + 2, [(x0, "T"), (x0 + 1, LT0), (x0 + 2, "t")]),
        (t + 3, [(x0 - 1, "t"), (x0, LT0), (x0 + 1, LT1), (x0 + 2, "r")]),
        (t + 4, [(x0 - 1, "t"), (x0, LT0), (x0 + 1, LT1), (x0 + 2, "r")]),
        (t + 5, [(x0 - 1, "t"), (x0, LT1), (x0 + 1, LT2), (x0 + 2, "R")]),
        (t + 6, [(x0 - 1, "r"), (x0, LT1), (x0 + 1, LT2), (x0 + 2, "R")]),
        (t + 7, [(x0 - 1, "R"), (x0, LT2), (x0 + 1, LT3), (x0 + 2, "q")]),
    ]
    for y, px in body:
        for (x, ch) in px:
            g.set(x, y, ch)
    # the left point, leaning out from the seat, and the right one, shorter
    for (x, y, ch) in ((x0 - 2, t + 6, "t"), (x0 - 2, t + 5, LT1), (x0 - 3, t + 4, "T"), (x0 - 3, t + 5, "r"),
                       (x0 - 2, t + 7, "R"),
                       (x0 + 3, t + 6, LT2), (x0 + 3, t + 7, "R"), (x0 + 4, t + 5, "t"), (x0 + 4, t + 6, "q")):
        g.set(x, y, ch)
    # the seat: gold wire bound round the base of the points, and the wood flaring up into it
    g.span(t + 8, x0 - 1, x0 + 2, "g")
    g.set(x0 - 1, t + 8, "G")
    g.set(x0 + 2, t + 8, "u")
    g.span(t + 9, x0, x0 + 1, "u")
    g.set(x0, t + 9, "g")
    # the glow: a few points of the edge stop clear of the stone - the fade, never a disc
    for (x, y, ch) in ((x0 - 3, t + 1, LT3), (x0 + 4, t + 2, LT3), (x0 - 5, t + 5, LT3), (x0 + 6, t + 4, LT3),
                       (x0 + 2, t - 1, LT3)):
        if g.get(x, y) == "." and y >= 2:
            g.set(x, y, ch)


def a_head(g):
    """Bare-headed: a bald crown lit from the upper left, a fringe of white hair over the ears, heavy white
    brows over eyes in their shade, and a nose."""
    cx = A_CX
    # THE HAIR: only a fringe, white, round the back of the skull and over the ears - so the bare crown is
    # the top of him and his shoulders step out from under it in the silhouette.
    for (x, y0, y1, ch) in ((22, 10, 14, "k"), (21, 12, 15, "s"), (30, 10, 14, "n"), (31, 12, 15, "m")):
        for y in range(y0, y1 + 1):
            g.set(x, y, ch)
    g.set(22, 15, "n")
    g.set(30, 15, "m")
    # THE CROWN: a dome, bare, the key on its upper left; the face below it.
    for (y, lo, hi) in ((8, 24, 28), (9, 23, 29), (10, 23, 29), (11, 23, 29), (12, 23, 29), (13, 23, 29),
                        (14, 23, 29)):
        g.span(y, lo, hi, "h")
        g.set(hi, y, "i")
    for (x, y) in ((24, 8), (25, 8), (23, 9), (24, 9), (23, 10), (23, 11)):
        g.set(x, y, "f")
    g.set(25, 9, "F")
    g.set(26, 8, "F")
    g.set(28, 8, "i")
    g.set(29, 9, "I")
    # the ears, and the fringe of white hair round the back of the skull above them
    g.set(22, 12, "h")
    g.set(22, 13, "i")
    g.set(30, 12, "i")
    g.set(30, 13, "I")
    for (x, y, ch) in ((22, 10, "s"), (22, 11, "k"), (30, 10, "n"), (30, 11, "m")):
        g.set(x, y, ch)
    # the BROWS: heavy and white, the near one lit, a crease of shadow between them
    g.span(11, 23, 25, "k")
    g.set(23, 11, "s")
    g.span(11, 27, 29, "n")
    g.set(26, 11, "i")
    # the eyes, set back under them; a lid of shade over each
    g.set(24, 12, "I")
    g.set(28, 12, "I")
    g.set(25, 12, "i")
    g.set(27, 12, "i")
    # the nose down the middle and the cheekbones, the near one lit
    g.set(26, 12, "f")
    g.set(26, 13, "h")
    g.set(27, 13, "i")
    g.set(24, 13, "f")
    g.set(23, 13, "f")


def a_beard(g):
    """The forked beard: from under the moustache, held to mid-chest, then parting into two points that
    hang to the cord at his waist - the one beard in the cast that splits."""
    cx = A_CX
    beard_mass(g, cx, 14, 26, [(0.0, 2.6), (0.15, 3.4), (0.5, 3.6), (1.0, 3.0)])
    # the two points: each its own taper, the gap between them opening downward
    for (c0, c1, w0, near) in ((cx - 1.4, cx - 2.6, 1.8, True), (cx + 1.6, cx + 2.4, 1.6, False)):
        for y in range(26, 35):
            t = (y - 26) / 8.0
            c = c0 + (c1 - c0) * t
            w = w0 * (1.0 - t) + 0.4
            lo, hi = int(round(c - w)), int(round(c + w - 0.2))
            for x in range(lo, hi + 1):
                if near:
                    g.set(x, y, "k" if x <= c else "n")
                else:
                    g.set(x, y, "n" if x <= c else "m")
            g.set(lo, y, "s" if near and t < 0.7 else ("k" if near else "n"))
    for y in range(27, 35):
        g.set(cx, y, "." if y > 28 else "m")
    streaks(g, [(24, 17, 21, "n"), (26, 16, 22, "n"), (28, 18, 23, "m"), (23, 20, 24, "s"), (25, 22, 25, "n"),
                (27, 21, 25, "m"), (24, 28, 31, "n")])
    # its edge breaks into flicks rather than a ruled line
    for (x, y, ch) in ((22, 18, "k"), (30, 20, "n"), (22, 22, "s"), (30, 23, "m")):
        if g.get(x, y) != ".":
            g.set(x, y, ch)
    # THE MOUSTACHE over it, out and down from under the nose, occlusion under it so it lies over
    my = 14
    for sgn in (-1, 1):
        g.set(cx + sgn, my, "k" if sgn < 0 else "n")
        g.set(cx + 2 * sgn, my, "S" if sgn < 0 else "k")
        g.set(cx + 3 * sgn, my + 1, "s" if sgn < 0 else "n")
        g.set(cx + 3 * sgn, my + 2, "k" if sgn < 0 else "m")
        g.set(cx + sgn, my + 1, "o")
        g.set(cx + 2 * sgn, my + 1, "n" if sgn < 0 else "m")
    g.set(cx, my, "I")
    g.set(cx, my + 1, "m")


def a_mantle(g, phase=None):
    """The ivory mantle over both shoulders and down his back to the floor, swept off-axis toward his
    free side: lit on the staff side, base across its middle, its far third in shade with the folds the
    sweep makes, the far edge deep."""
    keys = [(16, 21, 31), (17, 18, 34), (22, 18, 34), (30, 19, 34), (38, 19, 35), (44, 19, 36), (46, 20, 36)]
    roster.mass(g, keys, IV2, IV1, IV3, rim=False)
    for y in range(18, FEET + 1):
        row = g.g[y]
        xs = [x for x in range(CELL) if row[x] in IVORY_RAMP]
        if not xs:
            continue
        lo, hi = min(xs), max(xs)
        for x in xs:
            d = (x - lo) / float(max(1, hi - lo)) + 0.25 * (y - 18) / 28.0
            if d > 0.70 and row[x] == IV2:
                row[x] = IV3
            if x == hi:
                row[x] = IV4
    roster.fold(g, [(32, 24), (34, 33), (35, 44)], IV4, IV3)
    roster.fold(g, [(30, 33), (32, 45)], IV4, None)
    roster.fold(g, [(20, 30), (19, 38), (19, 45)], IV3, IV1)
    weave(g, 18, 37, 24, 45, IV3, IV4, rate=23)
    weave(g, 18, 37, 24, 45, IV2, IV3, rate=29, bias_x=A_CX)
    for x in (20, 25, 34, 35):
        roster.cut(g, (x, FEET))


def a_robe(g, phase=None):
    """The under-robe, between the mantle's fronts: two stops darker, so the mantle reads as a garment
    laid over another; the mantle's front edges bordered in gold; a plaited cord at the waist."""
    keys = [(24, 23, 29), (34, 22, 30), (40, 22, 31), (46, 21, 32)]
    roster.mass(g, keys, IV3, IV2, IV4, rim=False)
    for (bx, tx) in ((-2.6, -1.4), (0.6, 0.3), (3.4, 1.8)):
        g.line(A_CX + bx, FEET - 1, A_CX + tx, 37, IV4)
    for (x, y) in ((23, 40), (23, 44), (24, 37)):
        if g.get(x, y) == IV3:
            g.set(x, y, IV2)
    for x in range(21, 33):
        if g.get(x, FEET) != ".":
            g.set(x, FEET, "o")
    # THE BORDER down the mantle's two front edges, from the shoulders to the hem: the near one catches
    # the key, the far one is the border's shade. A dark stop inside each so the edge is a lap of cloth.
    for (x0, y0, x1, y1, a, b) in ((22, 17, 21, 45, "G", "g"), (30, 17, 32, 45, "g", "u")):
        n = y1 - y0
        for i in range(n + 1):
            t = i / float(n)
            x = int(round(x0 + (x1 - x0) * t))
            y = y0 + i
            g.set(x, y, a if (y * 3) % 7 else b)
            g.set(x + (1 if x0 < A_CX else -1), y, IV4)
    # THE CORD at the waist, plaited, its tassel down the far side.
    g.span(33, 22, 30, "u")
    for x in range(23, 31, 2):
        g.set(x, 33, "g")
    g.set(29, 34, "u")
    g.set(29, 35, "g")
    g.set(30, 36, "u")
    hem_walk(g, phase, 37, (27, 20), A_CX)


def a_arms(g, lift=0):
    """The staff arm in its wide sleeve, down to the grip; and the free arm held out low, the hand
    palm down with the fingers spread over the ground - Warding Hand."""
    # The staff arm reaches out to the side: the staff is planted at arm's length, not leant on.
    roster.mass(g, [(18, 18, 21), (21, 15, 20), (24, 12, 18), (27, 12, 17), (30, 13, 16)], IV2, IV1, IV3)
    roster.fold(g, [(18, 20), (14, 26)], IV3, IV1)
    g.span(30, 13, 16, IV4)
    g.span(31, 14, 16, "o")
    for y in range(23, 28):
        g.set(12, y, "G" if y % 2 else "g")
    # THE FREE ARM, held out from the far shoulder and down, the wide sleeve hanging from the forearm to
    # a point below the wrist - the one limb in the cast that reaches out over the ground rather than
    # holding something up. Its top lit (the key is above it), its underside deep.
    roster.mass(g, [(18, 32, 34), (21, 33, 37), (24, 34, 39), (27, 34, 39), (30, 35, 38), (33, 36, 37)],
                IV3, IV2, IV4, rim=False)
    g.line(34, 19, 38, 23, IV2)
    g.line(35, 19, 39, 23, IV1)
    roster.fold(g, [(35, 24), (37, 31)], IV4, IV2)
    roster.fold(g, [(34, 27), (35, 31)], IV4, None)
    g.set(36, 33, IV4)
    # the cuff, bordered in gold like the mantle
    for y in range(23, 28):
        g.set(39, y, "g" if y % 2 else "G")
    g.set(39, 27, "u")
    # THE HAND: its back toward us, out of the cuff, the fingers spread down over the ground - Warding Hand.
    for (x, y, ch) in ((40, 23, "f"), (41, 23, "h"), (40, 24, "h"), (41, 24, "f"), (42, 24, "h"),
                       (40, 25, "h"), (41, 25, "h"), (42, 25, "i"), (40, 26, "i"), (42, 26, "h"),
                       (43, 25, "h"), (43, 26, "i"), (40, 27, "I"), (42, 27, "i"), (41, 26, "i"),
                       (44, 26, "i"), (42, 22, "f")):
        g.set(x, y, ch)


def merlin_a(phase=None):
    """The Crystal Cave. `phase` (0..1) is the walk; None the pose."""
    g = G(CELL)
    lift = lift_of(phase)
    a_mantle(g, phase)
    if phase is not None:
        roster.sway(g, 34, 1.6 * math.sin(2.0 * math.pi * phase - 1.2), FEET)
    a_robe(g, phase)
    a_arms(g, lift)
    a_head(g)
    a_beard(g)
    rim(g)
    wood_staff(g, A_STAFF, 12 - lift, FEET - lift, knots=((22 - lift, "u"), (37 - lift, "u")))
    crystal(g, lift)
    staff_hand(g, A_STAFF, 24 - lift)
    roster.contact(g, 8, 37)
    return g.rows()


# =================================================================================================
# B - THE WOOD
# =================================================================================================

B_CX = 25
B_STAFF = 10
B_ORIGIN = (11.0, 7.0)


def antlers(g):
    """A stag's antlers bound on over his hair: two beams sweeping up and out from the brow band, three
    tines each, the brow tine low and forward and the crown points highest - bone, lit on the key side,
    the far beam a stop down. Wider than his shoulders, which no one else in the cast is above the head."""
    # (points along the beam, tines as (from-index, to-point))
    for (beam, tines, lit, mid, far) in (
            ([(23, 9), (21, 7), (19, 5), (18, 3)], [(1, (19, 8)), (2, (16, 5))], "s", "k", "n"),
            ([(28, 9), (30, 7), (32, 5), (33, 3)], [(1, (32, 8)), (2, (35, 5))], "s", "k", "n")):
        for (x0, y0), (x1, y1) in zip(beam, beam[1:]):
            g.line(x0, y0, x1, y1, mid)
            g.line(x0 + 1, y0, x1 + 1, y1, far)
            g.set(x0, y0, lit)
        g.set(beam[-1][0], beam[-1][1], lit)
        for (i, (tx, ty)) in tines:
            bx, by = beam[i]
            g.line(bx, by, tx, ty, mid)
            g.set(tx, ty, lit)
    # the burr where each beam leaves the band, rough
    for (x, y, ch) in ((22, 10, "n"), (23, 10, "k"), (28, 10, "n"), (29, 10, "m")):
        g.set(x, y, ch)


def b_head(g):
    """A weathered face under a band of leather, wild white hair in two hanks down onto the pelt - kept off
    the middle, so the beard lies on the grey of the pelt and not on more white - and a long beard,
    ragged at its end."""
    cx = B_CX
    # THE HAIR: a hank either side of the face, down onto the shoulders, its ends flicking out.
    # In the beard's shade and deep, a stop or two under it: the hair is behind the face, the beard in
    # front of it, and the light says so.
    roster.mass(g, [(10, 20, 22), (14, 19, 22), (18, 19, 21)], "n", "k", "m", rim=False)
    roster.mass(g, [(10, 29, 31), (14, 29, 32), (18, 30, 32)], "m", "n", "o", rim=False)
    for (x, y, ch) in ((18, 18, "n"), (18, 19, "k"), (33, 18, "m"), (33, 19, "m"),
                       (20, 16, "m"), (21, 13, "m"), (31, 15, "o"), (19, 15, "k"), (20, 12, "k")):
        g.set(x, y, ch)
    # THE FACE
    roster.head(g, cx, 11, 3.0, brow="low", gaze=0, mouth="set", eye="shadow")
    # An old man's face: the forehead clear under the band, and the brows white and bushy over eyes set
    # back in their shade - head()'s brow is a dark bar, which under a dark band shut the face.
    g.span(11, cx - 3, cx + 3, "h")
    g.set(cx - 3, 11, "f")
    g.set(cx - 2, 11, "f")
    g.set(cx + 3, 11, "i")
    g.span(12, cx - 3, cx - 1, "k")
    g.set(cx - 3, 12, "s")
    g.set(cx, 12, "f")
    g.span(12, cx + 1, cx + 3, "n")
    # THE BAND: leather round the brow, the antlers bound to it, and the white hair over it
    g.span(10, 22, 28, "u")
    g.set(22, 10, "g")
    g.set(23, 10, "g")
    g.set(28, 10, "J")
    g.span(9, 23, 27, "k")
    g.set(23, 9, "s")
    g.set(27, 9, "n")
    g.set(25, 8, "k")
    # THE BEARD: long, narrower than the face at its middle, ending in strands
    beard_mass(g, cx, 15, 31, [(0.0, 2.8), (0.2, 3.3), (0.5, 3.0), (0.8, 1.9), (1.0, 0.7)], lean=0.6)
    for (x, y) in ((22, 17), (29, 17), (21, 19), (29, 20)):
        if g.get(x, y) in ("n", "m"):
            g.set(x, y, "o")                     # where the beard lies over the hair
    streaks(g, [(23, 17, 23, "n"), (25, 16, 27, "n"), (27, 18, 22, "m"), (24, 24, 29, "s"), (26, 25, 30, "m")])
    for (x, y) in ((22, 25), (28, 26), (23, 29), (27, 30)):
        if g.get(x, y) in ("k", "n", "m", "s"):
            roster.cut(g, (x, y))
    roster.moustache(g, cx, 15, 2.5, "k", "S", "n")


def b_fur(g):
    """A wolf's pelt over his shoulders, a short cape no lower than the elbow, in steel's dark end - the
    grey of a winter coat under no light, two stops under the white of his beard so the beard lies on it -
    its edge in tufts, never a cut line, its hair laid in short strokes lit on the key side."""
    roster.mass(g, [(16, 20, 30), (18, 17, 33), (22, 16, 34), (25, 17, 34)], "R", "r", "q")
    roster.tatters(g, [(26, 17, 19), (27, 18, 18), (26, 22, 25), (27, 23, 24), (26, 28, 30), (26, 32, 34),
                       (27, 33, 33)], "R", "r", "q")
    for (x, y) in ((18, 20), (21, 23), (31, 19), (32, 23), (25, 24), (28, 22), (19, 25)):
        g.line(x, y, x + 1, y + 2, "q")
        g.set(x - 1, y, "r")
    for (x, y) in ((17, 21), (18, 19), (16, 23)):
        g.set(x, y, "t")
    g.set(33, 21, "o")
    g.set(34, 24, "o")


def b_robe(g, phase=None):
    """The russet robe, from the shoulders to the ankle, swept a little off the staff side, belted with a
    rope; worn ragged at the hem. Most of it in its base and shade, the key side lit."""
    keys = [(17, 20, 30), (24, 18, 32), (32, 18, 33), (40, 17, 35), (46, 16, 36)]
    roster.mass(g, keys, RU2, RU1, RU3)
    for y in range(24, FEET + 1):
        row = g.g[y]
        xs = [x for x in range(CELL) if row[x] in RUSSET_RAMP]
        if xs:
            hi = max(xs)
            for x in xs:
                if x > hi - 4 and row[x] == RU2:
                    row[x] = RU3
            row[hi] = RU4
    for (bx, tx) in ((-6.0, -3.0), (-2.0, -0.8), (2.0, 1.0), (6.0, 3.0)):
        g.line(B_CX + bx, FEET - 1, B_CX + tx, 35, RU3)
    roster.fold(g, [(29, 27), (31, 35), (33, 44)], RU4, RU2)
    roster.fold(g, [(20, 28), (19, 36)], RU3, RU1)
    weave(g, 16, 36, 34, 45, RU2, RU3, rate=17)
    weave(g, 16, 36, 27, 45, RU3, RU4, rate=23)
    for x in range(16, 37):
        if (x * 3) % 5 == 0:
            roster.cut(g, (x, FEET))
        elif g.get(x, FEET) != ".":
            g.set(x, FEET, RU4)
    # the rope belt, knotted on the near hip, its end hanging
    g.span(32, 18, 33, "e")
    for x in range(19, 33, 2):
        g.set(x, 32, "A")
    g.set(18, 32, "l")
    g.set(21, 33, "e")
    g.set(21, 34, "E")
    g.set(22, 35, "e")
    g.set(22, 36, "A")
    hem_walk(g, phase, 37, (26, 18), B_CX, shoe=("h", "i", "I"))


def thorn_staff(g, lift=0):
    """A gnarled thorn staff: the shaft kinked where it grew, and its head a burl with a hollow worn
    through it, the light caught in the hollow, two dead twigs sprouting from it."""
    x0 = B_STAFF
    for y in range(13 - lift, FEET - lift + 1):
        kink = 1 if 24 <= y + lift <= 31 else 0
        g.set(x0 + kink, y, "g" if (y * 7) % 11 else "G")
        g.set(x0 + 1 + kink, y, "u" if (y * 5) % 9 else "J")
    g.set(x0, 23 - lift, "u")
    g.set(x0 + 1, 32 - lift, "u")
    for (x, y) in ((x0 - 1, 35), (x0 + 2, 19)):
        g.set(x, y - lift, "u")                 # thorns
    # the burl: a ring of wood five across, thick, its far side in shade
    t = 4 - lift
    for (y, lo, hi) in ((t, x0, x0 + 2), (t + 1, x0 - 1, x0 + 3), (t + 2, x0 - 1, x0 + 3), (t + 3, x0 - 1, x0 + 3),
                        (t + 4, x0, x0 + 3), (t + 5, x0, x0 + 2), (t + 6, x0, x0 + 2)):
        g.span(y, lo, hi, "u")
        g.set(lo, y, "g")
        g.set(hi, y, "J")
    g.set(x0, t, "G")
    g.set(x0 - 1, t + 1, "G")
    # the hollow, and the light in it
    g.set(x0 + 1, t + 2, LT0)
    g.set(x0, t + 2, LT1)
    g.set(x0 + 1, t + 3, LT1)
    g.set(x0 + 2, t + 2, LT2)
    g.set(x0, t + 3, LT2)
    g.set(x0 + 2, t + 3, LT3)
    g.set(x0 + 1, t + 1, LT2)
    # twigs off the burl, and the light thrown off it
    g.line(x0 + 3, t + 1, x0 + 5, t - 1, "u")
    g.set(x0 + 6, t - 1, "J")
    g.line(x0 - 1, t, x0 - 2, t - 1, "g")
    for (x, y, ch) in ((x0 - 3, t + 3, LT3), (x0 + 5, t + 4, LT3), (x0 + 2, t - 2, LT3)):
        if g.get(x, y) == "." and y >= 2:
            g.set(x, y, ch)


def merlin_b(phase=None):
    """The Wood. `phase` (0..1) is the walk; None the pose."""
    g = G(CELL)
    lift = lift_of(phase)
    b_robe(g, phase)
    b_fur(g)
    b_head(g)
    antlers(g)
    # the staff arm: out from under the pelt in its russet sleeve to the grip
    roster.mass(g, [(22, 15, 18), (26, 13, 17), (28, 14, 17)], RU2, RU1, RU3, rim=False)
    g.span(28, 14, 17, RU4)
    rim(g)
    thorn_staff(g, lift)
    staff_hand(g, B_STAFF, 26 - lift)
    # THE FREE ARM, out from under the pelt in its sleeve and held out low, the hand palm down with the
    # fingers spread over the ground - Warding Hand.
    roster.mass(g, [(26, 32, 34), (28, 33, 37), (30, 34, 38), (32, 35, 37)], RU3, RU2, RU4, rim=False)
    g.line(33, 27, 37, 29, RU2)
    g.span(32, 35, 37, RU4)
    for (x, y, ch) in ((38, 28, "f"), (39, 28, "h"), (38, 29, "h"), (39, 29, "f"), (40, 29, "h"),
                       (39, 30, "h"), (40, 30, "h"), (41, 30, "i"), (38, 30, "i"), (39, 31, "i"),
                       (41, 31, "I"), (38, 31, "I"), (40, 31, "i"), (42, 31, "i")):
        g.set(x, y, ch)
    roster.contact(g, 9, 36)
    return g.rows()


# =================================================================================================
# C - THE COUNSELLOR
# =================================================================================================

C_CX = 26
C_STAFF = 12
C_ORIGIN = (12.5, 6.0)


def star_head(g, lift=0):
    """A gold seven-pointed star on the staff's head - his spell's star, {7/3} - the light at its heart."""
    cx, cy = C_STAFF + 0.5, 6.0 - lift
    pts = []
    for i in range(7):
        a = -math.pi / 2 + 2 * math.pi * i / 7.0
        pts.append((cx + 4.2 * math.cos(a), cy + 4.2 * math.sin(a)))
    for i in range(7):
        x0, y0 = pts[i]
        x1, y1 = pts[(i + 3) % 7]
        g.line(x0, y0, x1, y1, "g")
    for (x, y) in pts:
        g.set(x, y, "G" if x < cx else "u")
    # the heart, in light
    g.set(C_STAFF, int(cy), LT0)
    g.set(C_STAFF + 1, int(cy), LT1)
    g.set(C_STAFF, int(cy) + 1, LT1)
    g.set(C_STAFF + 1, int(cy) + 1, LT2)
    g.set(C_STAFF, int(cy) - 1, LT2)
    g.set(C_STAFF - 1, int(cy), LT2)


def c_collar(g):
    """The high standing collar of the cope, behind his head: wine, rising past his crown on both sides and
    flaring outward at the top, its rim stitched in gold - the frame round his head that nobody else in
    the cast has."""
    roster.mass(g, [(6, 22, 30), (7, 20, 32), (9, 19, 33), (13, 20, 32), (17, 21, 31)], RED3, RED2, RED4, rim=False)
    for y in range(7, 18):
        for x in range(21, 32):
            if g.get(x, y) in RED_RAMP:
                g.set(x, y, RED4)                # its inside, in his shadow
    roster.mass(g, [(7, 20, 21), (9, 19, 21), (13, 20, 22), (17, 21, 22)], RED2, RED1, RED3, rim=False)
    # the gold rim over its top and down its outer edges
    for (x, y) in ((22, 6), (24, 6), (26, 6), (28, 6), (30, 6), (20, 7), (19, 9), (19, 11), (20, 13),
                   (21, 15), (32, 7), (33, 9), (33, 11), (32, 13), (31, 15)):
        g.set(x, y, "G" if x < C_CX else "g")


def c_head(g):
    cx = C_CX
    roster.head(g, cx, 10, 3.0, brow="low", gaze=0, mouth="set", eye="shadow")
    # close white hair over the crown
    g.span(8, 24, 28, "k")
    g.span(9, 23, 29, "k")
    g.set(24, 8, "s")
    g.set(23, 9, "s")
    g.set(28, 8, "n")
    g.set(29, 9, "n")
    g.set(23, 10, "k")
    g.set(29, 10, "n")
    beard_mass(g, cx, 15, 32, [(0.0, 2.8), (0.2, 3.2), (0.6, 2.8), (0.85, 1.8), (1.0, 0.5)])
    streaks(g, [(25, 17, 26, "n"), (27, 18, 24, "m"), (24, 21, 27, "s")])
    roster.moustache(g, cx, 14, 2.2, "k", "S", "n")


def c_cope(g, phase=None):
    keys = [(15, 21, 31), (18, 18, 34), (24, 18, 35), (32, 18, 38), (40, 18, 41), (46, 19, 43)]
    roster.mass(g, keys, RED2, RED1, RED3, rim=False)
    for y in range(18, FEET + 1):
        row = g.g[y]
        xs = [x for x in range(CELL) if row[x] in RED_RAMP]
        if xs:
            hi = max(xs)
            row[hi] = RED4
    roster.fold(g, [(32, 24), (36, 34), (40, 45)], RED4, RED2)
    roster.fold(g, [(34, 30), (37, 38), (39, 45)], RED4, None)
    roster.fold(g, [(19, 26), (18, 36), (18, 45)], RED3, RED1)
    weave(g, 17, 43, 24, 45, RED2, RED3, rate=21)
    # the charcoal robe down the front
    roster.mass(g, [(26, 23, 29), (36, 22, 30), (46, 21, 32)], "N", "p", "J", rim=False)
    for (bx, tx) in ((-2.6, -1.4), (0.6, 0.3), (3.4, 1.8)):
        g.line(C_CX + bx, FEET - 1, C_CX + tx, 36, "J")
    # the cope's gold orphrey down each front edge
    for (x0, x1, a, b) in ((22, 20, "G", "g"), (30, 32, "g", "u")):
        for i in range(30):
            y = 17 + i
            x = int(round(x0 + (x1 - x0) * i / 29.0))
            g.set(x, y, a if (y * 3) % 7 else b)
    for x in range(20, 34):
        if g.get(x, FEET) != ".":
            g.set(x, FEET, "o")
    hem_walk(g, phase, 37, (27, 20), C_CX)


def c_chain(g):
    """The chain of office: gold links in a shallow loop from shoulder to shoulder over the beard."""
    for (x, y) in ((21, 18), (21, 20), (22, 22), (23, 24), (29, 24), (30, 22), (31, 20), (31, 18)):
        g.set(x, y, "G" if x < C_CX else "g")
        g.set(x, y + 1, "u")
    g.rect(25, 25, 27, 27, "g")
    g.set(25, 25, "H")
    g.set(26, 26, LT2)
    g.set(27, 27, "u")


def merlin_c(phase=None):
    """The Counsellor. `phase` (0..1) is the walk; None the pose."""
    g = G(CELL)
    lift = lift_of(phase)
    c_collar(g)
    c_cope(g, phase)
    # the staff arm
    roster.mass(g, [(18, 18, 21), (22, 16, 20), (27, 15, 19)], RED2, RED1, RED3)
    g.span(27, 15, 19, "g")
    # the free arm, raised beside him to the shoulder, the palm turned out: stay.
    roster.mass(g, [(18, 32, 35), (22, 34, 37), (25, 35, 38), (27, 36, 38)], RED3, RED2, RED4, rim=False)
    roster.fold(g, [(34, 20), (36, 25)], RED4, RED2)
    g.span(18, 37, 38, "g")
    for (x, y, ch) in ((37, 14, "f"), (38, 14, "h"), (37, 15, "f"), (38, 15, "h"), (39, 15, "h"),
                       (36, 16, "f"), (37, 16, "h"), (38, 16, "h"), (39, 16, "i"), (37, 17, "h"),
                       (38, 17, "i"), (36, 15, "f"), (39, 14, "i"), (36, 17, "i")):
        g.set(x, y, ch)
    roster.mass(g, [(19, 36, 38), (21, 36, 38)], RED3, RED2, RED4, rim=False)
    c_head(g)
    c_chain(g)
    rim(g)
    for y in range(11 - lift, FEET - lift + 1):
        g.set(C_STAFF, y, "p" if (y * 7) % 11 else "N")
        g.set(C_STAFF + 1, y, "J")
    for y in (20, 36):
        g.span(y - lift, C_STAFF, C_STAFF + 1, "g")
    star_head(g, lift)
    staff_hand(g, C_STAFF, 25 - lift)
    roster.contact(g, 11, 43)
    return g.rows()


# =================================================================================================
# the designs, the checks and the sheet
# =================================================================================================

DESIGNS = [
    ("A", "The Crystal Cave", merlin_a, A_ORIGIN),
    ("B", "The Wood", merlin_b, B_ORIGIN),
    ("C", "The Counsellor", merlin_c, C_ORIGIN),
]


def design(key):
    for d in DESIGNS:
        if d[0] == key:
            return d
    raise SystemExit("no Merlin design %r" % key)


def debug(key, path):
    """The pose at x14 with a grid and every fourth row and column numbered - for placing pixels."""
    _k, _name, fn, origin = design(key)
    rows = fn()
    z = 14
    im = P.frame(rows, PALETTE, CELL).resize((CELL * z, CELL * z), Image.NEAREST)
    bg = Image.new("RGBA", im.size, (30, 34, 46, 255))
    bg.alpha_composite(im)
    d = ImageDraw.Draw(bg)
    for i in range(CELL + 1):
        col = (70, 76, 92, 255) if i % 4 else (110, 118, 140, 255)
        d.line((i * z, 0, i * z, CELL * z), fill=col)
        d.line((0, i * z, CELL * z, i * z), fill=col)
        if i % 4 == 0 and i < CELL:
            d.text((i * z + 2, 1), str(i), fill=(200, 200, 90, 255))
            d.text((1, i * z + 2), str(i), fill=(200, 200, 90, 255))
    ox, oy = origin
    d.ellipse((ox * z + z / 2 - 4, oy * z + z / 2 - 4, ox * z + z / 2 + 4, oy * z + z / 2 + 4), outline=(255, 0, 255, 255))
    bg.convert("RGB").save(path)


CARD = (36, 42, 56, 255)       # the character select's card, near enough
ROSTER = ["pyromancer", "frostweaver", "stormcaller", "geomancer", "tituba", "vainamoinen", "dee",
          "laelaps", "seimei", "dark-wizard", "wayland", "sibyl"]


def roster_frames():
    """Every wizard's portrait (frame one of the idle), from the shipped sheets - and, for a wizard on a
    branch not merged yet (the Sibyl), from _merlin_concepts/ref/ if a copy was put there."""
    out = []
    for stem in ROSTER:
        for p in (os.path.join(ROOT, "assets", "bonelight", "characters", stem + "-1.png"),
                  os.path.join(OUT, "ref", stem + "-1.png")):
            if os.path.exists(p):
                out.append((stem, Image.open(p).convert("RGBA")))
                break
    return out


def lineup(imgs, z=3, black=False):
    """The roster in a row at x`z` on the card colour, `imgs` last - or all of them filled black."""
    if not isinstance(imgs, list):
        imgs = [imgs]
    cast = roster_frames() + [("merlin", im) for im in imgs]
    w = len(cast) * (CELL * z + 6) + 6
    sheet = Image.new("RGBA", (w, CELL * z + 12), (232, 234, 238, 255) if black else CARD)
    for i, (_stem, im) in enumerate(cast):
        if black:
            im = silhouette(im)
        sheet.alpha_composite(im.resize((CELL * z, CELL * z), Image.NEAREST), (6 + i * (CELL * z + 6), 6))
    return sheet


def mask(im):
    px = im.load()
    return set((x, y) for y in range(im.size[1]) for x in range(im.size[0]) if px[x, y][3])


def overlap(a, b):
    """Filled-silhouette overlap (intersection over union) in the shared cell - feet on one row, as every
    wizard stands - taking the better of b as drawn and b mirrored, since a sprite flips with his walk."""
    best = 0.0
    for flip in (False, True):
        bb = set((CELL - 1 - x, y) for (x, y) in b) if flip else b
        best = max(best, len(a & bb) / float(max(1, len(a | bb))))
    return best


def _lab(rgb):
    def lin(v):
        v = v / 255.0
        return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    r, g, b = [lin(v) for v in rgb[:3]]
    X = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    Y = 0.2126 * r + 0.7152 * g + 0.0722 * b
    Z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883
    f = lambda t: t ** (1.0 / 3.0) if t > 0.008856 else 7.787 * t + 16.0 / 116.0
    return (116.0 * f(Y) - 16.0, 500.0 * (f(X) - f(Y)), 200.0 * (f(Y) - f(Z)))


IGNORE = set([B.OCC.upper(), B.RIM.upper(), B.RIM_SOFT.upper()])


def colour_print(im):
    """Where a figure's colour sits: a histogram over 16 cells of the a*b* plane (hue and how strong) plus
    3 of lightness, each pixel counted once - the rim and the occlusion left out, since everyone has them.
    Two figures in one dress land in the same cells whatever their shapes."""
    px = im.load()
    hist = {}
    n = 0
    for y in range(im.size[1]):
        for x in range(im.size[0]):
            p = px[x, y]
            if not p[3] or ("#%02X%02X%02X" % p[:3]) in IGNORE:
                continue
            L, a, b = _lab(p)
            c = math.hypot(a, b)
            hue = int(((math.degrees(math.atan2(b, a)) + 360.0) % 360.0) // 45) if c > 8 else -1
            key = (hue, 0 if c <= 8 else (1 if c < 30 else 2), int(L // 34))
            hist[key] = hist.get(key, 0) + 1
            n += 1
    return dict((k, v / float(n)) for k, v in hist.items())


def colour_overlap(h1, h2):
    """Histogram intersection, 0 (nothing alike) to 1 (one palette in the same proportions)."""
    return sum(min(v, h2.get(k, 0.0)) for k, v in h1.items())


def measure(img):
    """(largest silhouette overlap, with whom), (largest colour overlap, with whom) against the cast."""
    m, h = mask(img), colour_print(img)
    sil = max((overlap(m, mask(im)), stem) for stem, im in roster_frames())
    col = max((colour_overlap(h, colour_print(im)), stem) for stem, im in roster_frames())
    return sil, col


def silhouette(im):
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    src, dst = im.load(), out.load()
    for y in range(im.size[1]):
        for x in range(im.size[0]):
            if src[x, y][3]:
                dst[x, y] = (12, 14, 20, 255)
    return out


def light_centre(rows, near, reach=4):
    """The centroid of the light's own pixels (core, hot and mid) near `near` - what
    CharacterVisuals.GetStaffTipCell is measured as, and where the ward's orb leaves from."""
    pts = [(x, y) for y, row in enumerate(rows) for x, ch in enumerate(row)
           if ch in (LT0, LT1, LT2) and abs(x - near[0]) <= reach and abs(y - near[1]) <= reach]
    n = float(len(pts))
    return sum(p[0] for p in pts) / n, sum(p[1] for p in pts) / n


def bounds_problems(key, frames):
    """roster.py's two limits on a POSE: the walk and the idle bob its top up two rows, and the hurt knocks
    the cell two left and shears its top two more - so the pose keeps clear of both edges."""
    out = []
    for i, rows in enumerate(frames):
        b = P.content_bounds(rows)
        if b[0] < 2:
            out.append("%s frame %d reaches row %d (nothing above row 2)" % (key, i, b[0]))
        if b[2] < 4:
            out.append("%s frame %d reaches column %d (nothing left of column 4)" % (key, i, b[2]))
    return out


INK = (214, 218, 228, 255)
DIM = (128, 134, 150, 255)
SHEET_BG = (24, 27, 36, 255)


def build_sheet(shipping=None):
    """Each look: the pose at x6, filled black at x6, its idle and its walk at x3, and its numbers; then the
    whole cast in a row with the three on the end, in colour and filled black, at the select's scale."""
    rows_h = 48 * 6 + 64
    width = max(1720, (len(roster_frames()) + len(DESIGNS)) * (CELL * 3 + 6) + 18)
    results = []
    for key, name, fn, origin in DESIGNS:
        pose = fn()
        idle = roster.idle(pose)
        anims = anim_sets.player_set(pose, idle, fn)
        img = P.frame(pose, PALETTE, CELL)
        results.append((key, name, pose, idle, dict((a[0], a[1]) for a in anims), img, origin))
    cast_h = (48 * 3 + 12) * 2
    sheet = Image.new("RGBA", (width, 40 + rows_h * len(results) + 30 + cast_h + 10), SHEET_BG)
    d = ImageDraw.Draw(sheet)
    d.text((12, 12), "Merlin - three looks.  Left: the pose x6 and filled black.  Right: the idle (4) and the walk (8)"
                     " at x3, and what each measures against the cast.  Bottom: the cast at x3 with the three"
                     " on the end.", fill=INK)
    for i, (key, name, pose, idle, anims, img, origin) in enumerate(results):
        y0 = 40 + i * rows_h
        d.line((0, y0, width, y0), fill=(54, 58, 70, 255))
        tag = "   << SHIPPING (player_merlin.SHIPPING)" if key == shipping else ""
        d.text((12, y0 + 8), "%s   %s%s" % (key, name, tag), fill=INK)
        big = img.resize((48 * 6, 48 * 6), Image.NEAREST)
        card = Image.new("RGBA", big.size, CARD)
        card.alpha_composite(big)
        sheet.alpha_composite(card, (12, y0 + 28))
        light = Image.new("RGBA", big.size, (232, 234, 238, 255))
        light.alpha_composite(silhouette(img).resize((48 * 6, 48 * 6), Image.NEAREST))
        sheet.alpha_composite(light, (12 + 300, y0 + 28))
        x = 12 + 610
        d.text((x, y0 + 28), "idle", fill=DIM)
        for f in idle:
            fr = P.frame(f, PALETTE, CELL).resize((144, 144), Image.NEAREST)
            bg = Image.new("RGBA", fr.size, CARD)
            bg.alpha_composite(fr)
            sheet.alpha_composite(bg, (x, y0 + 44))
            x += 150
        x = 12 + 610
        d.text((x, y0 + 194), "walk (every other frame)", fill=DIM)
        for f in anims["moving"][::2]:
            fr = P.frame(f, PALETTE, CELL).resize((144, 144), Image.NEAREST)
            bg = Image.new("RGBA", fr.size, CARD)
            bg.alpha_composite(fr)
            sheet.alpha_composite(bg, (x, y0 + 210))
            x += 150
        (so, sw), (co, cw) = measure(img)
        filled, edges, ratio = roster.density(pose)
        ox, oy = light_centre(pose, origin)
        lines = ["%.2f edges/px (floor 0.80)" % ratio,
                 "silhouette overlap %.2f, with %s" % (so, sw),
                 "colour overlap %.2f, with %s" % (co, cw),
                 "the orb leaves from (%.1f, %.1f)" % (ox, oy)]
        for j, ln in enumerate(lines):
            d.text((12 + 1230, y0 + 50 + j * 18), ln, fill=INK if j else DIM)
    y = 40 + rows_h * len(results) + 10
    d.text((12, y), "the cast as the character select shows it (x3), the three on the end - and filled black", fill=DIM)
    imgs = [r[5] for r in results]
    a, b = lineup(imgs), lineup(imgs, black=True)
    sheet.alpha_composite(a.crop((0, 0, min(a.size[0], width - 12), a.size[1])), (6, y + 20))
    sheet.alpha_composite(b.crop((0, 0, min(b.size[0], width - 12), b.size[1])), (6, y + 20 + a.size[1]))
    return sheet


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    if "--lineup" in sys.argv:
        key = sys.argv[sys.argv.index("--lineup") + 1]
        img = [P.frame(design(k)[2](), PALETTE, CELL) for k in key]
        a, b = lineup(img), lineup(img, black=True)
        both = Image.new("RGBA", (a.size[0], a.size[1] * 2), CARD)
        both.alpha_composite(a, (0, 0))
        both.alpha_composite(b, (0, a.size[1]))
        p = os.path.join(OUT, "lineup_%s.png" % key)
        both.convert("RGB").save(p)
        print("wrote %s" % p)
        return 0
    if "--debug" in sys.argv:
        key = sys.argv[sys.argv.index("--debug") + 1]
        p = os.path.join(OUT, "debug_%s.png" % key)
        debug(key, p)
        print("wrote %s" % p)
        return 0
    try:
        import player_merlin
        shipping = player_merlin.SHIPPING
    except (ImportError, AttributeError):
        shipping = None
    problems = []
    for key, name, fn, origin in DESIGNS:
        rows = fn()
        img = P.frame(rows, PALETTE, CELL)
        img.resize((48 * 8, 48 * 8), Image.NEAREST).save(os.path.join(OUT, "%s.png" % key))
        (so, sw), (co, cw) = measure(img)
        filled, edges, ratio = roster.density(rows)
        ox, oy = light_centre(rows, origin)
        print("%s  %-17s %.2f edges/px%s   silhouette overlap %.2f (%s)   colour overlap %.2f (%s)   orb (%.1f, %.1f)%s"
              % (key, name, ratio, "" if ratio >= 0.80 else " SPARSE", so, sw, co, cw, ox, oy,
                 "   <- shipping" if key == shipping else ""))
        anims = anim_sets.player_set(rows, roster.idle(rows), fn)
        problems += bounds_problems(key + " pose", [rows])
        for anim, frames, _l, _s in anims:
            if anim in ("idle", "moving"):
                problems += P.check_anchor("%s %s" % (key, anim), frames, CELL)
    # the cast against itself, for scale: a look is clear of the cast if it is no nearer anyone than the
    # nearest two of them already are to each other
    cast = roster_frames()
    pairs = []
    for i, (s1, i1) in enumerate(cast):
        for s2, i2 in cast[i + 1:]:
            pairs.append((overlap(mask(i1), mask(i2)), colour_overlap(colour_print(i1), colour_print(i2)), s1, s2))
    sp, cp = max(pairs, key=lambda p: p[0]), max(pairs, key=lambda p: p[1])
    print("   the cast against itself: nearest silhouettes %s/%s %.2f, nearest colours %s/%s %.2f (%d wizards)"
          % (sp[2], sp[3], sp[0], cp[2], cp[3], cp[1], len(cast)))
    p = os.path.join(OUT, "sheet.png")
    build_sheet(shipping).convert("RGB").save(p)
    print("wrote %s" % p)
    if problems:
        print("PROBLEMS:")
        for pr in problems:
            print("   " + pr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
