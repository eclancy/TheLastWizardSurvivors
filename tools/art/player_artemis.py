# -*- coding: utf-8 -*-
"""Artemis, the huntress: the first god of the far shore, on the player's cell. Run: python tools/art/player_artemis.py

    assets/bonelight/characters/artemis-{1,2,3,4}.png      the idle, frame one is the portrait
    assets/bonelight/characters/artemis-{idle,moving,hurt,death}.png
    scenes/resources/ArtemisFrames.tres
    tools/art/_player_artemis.png     (gitignored) the pose at x8, filled black, and every animation

    --preview   writes only the preview; nothing under assets/ or scenes/ is touched.

WHO SHE IS (.ai/second-half.md 3.3). The huntress and the moon, named in the legend of Laelaps - she gave the hound
to Procris (Ovid) - and rekindled on the White Downs when its warden falls. She opens on Sagittarius, a bow and the
stars as archers. The look the doc asks for: "a tall figure in a short chiton and a dark mantle, the bow across her
back, a thin crescent at her brow carrying the light ramp. No hound: Laelaps is his own."

A GOD, NOT ONE MORE WIZARD, AT 48 PIXELS. Every wizard in the cast is a robe to the ground with his light held out in
a hand or on a staff (the comparison sheet in the preview shows them). She is drawn against every one of those:

  1. HER LIGHT IS ON HER, NOT IN HER HAND: the crescent at her brow, horns up, in the light ramp - the brightest
     thing on her and the only thing on her that gives off light. It lights her hair and her brow from above (a
     bounce in the ramp's edge stop), so she is lit by herself, which nobody in the cast is.
  2. NO ROBE. A short chiton belted at the waist, to the knee, and bare legs in hunting boots, mid-stride: the one
     figure besides the Pyromancer whose legs are seen, and the only one walking out of the frame rather than
     standing in it. A god of the wild, not of a library.
  3. TALL. Crown to heel she is the tallest figure on the cell (the knot of her hair at row 4, her heel on the
     anchor), about eight heads, where the wizards are six or seven: the proportion of a statue, not of a person.
  4. THE BOW ACROSS HER BACK, gold - Homer's "of the golden bow" - its upper limb curling up over her far shoulder
     above her head and its lower limb out past her near hip, the string a dark line across her chest. A shape
     no wizard has, and it survives being filled black (the silhouette in the preview).
  5. THE DARK MANTLE, night-violet (the `violet` row from its deep end), pinned at the near shoulder and falling
     behind her to the calf, swept off-axis away from the way she walks: the dark mass the contract wants most of
     a player figure to be, so the chiton and the light are what separate her from the ground.

The chiton is BONE (`skin`), the row the contract says the moon is made of, from its base down: moon-pale in the
light, cold grey in the shade. Her hair is stone's dark end gathered in a knot behind; her skin living flesh; the
belt, the brooch and the bow gold; the boots leather (gold's shade and flesh's deep, as Wayland's apron is).

SHE FACES RIGHT, three-quarters, the way she walks, so the player's flip is right for her
(CharacterVisuals.SpriteFacesLeft false). Her walk is a real stride (roster.leg): the legs trade places, the
chiton's hem swings and the mantle drags a quarter-step behind; the bow and the crescent ride the bob.

Two limits from the derived animations (roster.py's): nothing above row 2 (the walk bobs the top up two) and nothing
left of column 4 (the hurt knocks the cell two left and shears the top two more).

CharacterVisuals.GetStaffTipCell("artemis") is the crescent's centre, which this prints: a spell she casts from
herself leaves from her light, as every wizard's leaves from his.
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
import pixel as P
import roster
import spriteframes

G = roster.G
CELL = roster.WIZARD_CELL
FEET = CELL - 2
STEM = "artemis"
SHEET = "ArtemisFrames"
OUT_CHARS = os.path.join("assets", "bonelight", "characters")

LT0, LT1, LT2, LT3 = roster.LT0, roster.LT1, roster.LT2, roster.LT3
PALETTE = dict(roster.PALETTE)

# The mantle: the `violet` row from its deep end (roster's V v w x X), the night she hunts under.
MID, LIT, DARK = "x", "w", "X"
# The chiton: bone (roster's S s k n m), from its base down - the moon's own material.
CH_LIT, CH_MID, CH_DARK = "k", "n", "m"
# Her legs and arms (living flesh), and the boots (gold's shade and base over flesh's deep: leather).
SKIN = ("h", "f", "i")
SKIN_FAR = ("i", "h", "I")
BOOT = ("u", "g", "I")

# The crescent's centre, which CharacterVisuals.GetStaffTipCell("artemis") is: the bowl at her brow.
CRESCENT = (27, 5)
# The head: her face's middle column and its brow row.
HX, HY = 27, 8
# Where her hips stand, and how far a stride carries each ankle from under them.
HIP_FAR, HIP_NEAR = (24, 31), (28, 31)


def set_if_empty(g, x, y, ch):
    """Paint only where nothing is: for what is BEHIND the figure (the bow) and round it (the halo)."""
    if g.get(x, y) == ".":
        g.set(x, y, ch)


# --- the mantle -----------------------------------------------------------------------------------

def mantle(g, phase=None):
    """The mantle, behind everything: pinned at her near shoulder, over the far one and down her back, streaming
    behind her to a trailing corner at the thigh - swept off the vertical, away from the way she walks, so her
    outline is a figure moving and not a cone standing. Night-violet: deep in its folds, its key-side edge a stop
    lighter, its lining a stop lighter again where the corner turns."""
    roster.mass(g, [(14, 22, 26), (17, 18, 25), (21, 14, 24), (25, 11, 23), (28, 8, 22), (30, 7, 21), (31, 9, 20),
                    (33, 14, 21)], MID, LIT, DARK, rim=False)
    roster.fold(g, [(21, 17), (15, 25), (10, 29)], "o", LIT)
    roster.fold(g, [(23, 19), (19, 26), (16, 31)], DARK, LIT)
    roster.fold(g, [(18, 21), (14, 26)], DARK, None)
    # The lining where the corner turns back: the one place the violet's lit stop is a plane.
    for (y, x0, x1) in ((29, 8, 10), (30, 8, 12), (31, 10, 14), (32, 13, 16)):
        g.span(y, x0, x1, "w")
        g.set(x0, y, "v")
    roster.cut(g, (7, 30), (9, 31), (17, 33), (20, 33))       # worn at the corner, not cut square
    if phase is not None:
        # It streams: a quarter-step behind the stride, more toward the corner.
        roster.sway(g, 22, 1.5 * math.sin(2.0 * math.pi * phase - 1.3), 33)


# --- the legs -------------------------------------------------------------------------------------

def boot(g, ax, ay, far):
    """A laced hunting boot to mid-calf, the toe out forward: leather, its cuff turned down, the lacing a stop
    lighter up its front."""
    mid, lit, dark = BOOT
    top = ay - 2
    for y in range(top, ay + 3):
        if y > FEET:
            break
        lo, hi = ax - 1, ax + 1
        if y >= ay + 1:
            hi = ax + 3                                    # the foot, out forward
        if y == ay + 2:
            lo = ax - 2
        g.span(y, lo, hi, dark if far else mid)
        g.set(lo, y, mid if far else lit)
    g.span(top, ax - 1, ax + 1, "I" if far else "g")       # the turned-down cuff
    if not far:
        g.set(ax + 1, top + 1, "G")                        # the lacing
        g.set(ax + 1, top + 2, "u")
    g.set(ax + 3, min(FEET, ay + 2), "o")


def leg(g, hip, ankle, far):
    """One bare leg from the hip to the boot, the knee bent forward when the ankle comes in under it."""
    cloth = SKIN_FAR if far else SKIN
    hx, hy = hip
    ax, ay = ankle
    d = math.hypot(ax - hx, ay - hy)
    length = 14.0
    if d >= length:
        kx, ky = (hx + ax) / 2.0, (hy + ay) / 2.0
    else:
        a = math.acos(min(1.0, d / length))
        ang = math.atan2(ay - hy, ax - hx) - a
        kx, ky = hx + 7.0 * math.cos(ang), hy + 7.0 * math.sin(ang)
    roster.mass(g, [(hy, hx - 1.7, hx + 1.5), (int(round(ky)), kx - 1.3, kx + 1.2), (ay - 2, ax - 1.0, ax + 1.0)],
                cloth[0], cloth[1], cloth[2], rim=False)
    g.line(kx + 1.2, ky, ax + 1.0, ay - 2, cloth[2])         # the shin's far plane, so the knee reads as a joint
    boot(g, int(round(ax)), int(round(ay)), far)


def ankles(phase):
    """(far ankle, near ankle) for a stride phase; None is the pose: the far foot pushing off behind, the near one
    planted out in front - a long step, the weight between them."""
    if phase is None:
        return (17, 44), (34, 44)
    c, lift_near, lift_far = roster.stride(phase)
    return (26 - 8.0 * c, 44 - 3 * lift_far), (26 + 8.0 * c, 44 - 3 * lift_near)


# --- the chiton -----------------------------------------------------------------------------------

def chiton(g, phase=None):
    """The short chiton: the bodice, the overfold over the belt, and the skirt to the knee in folds, swinging with
    the stride. Bone from its base down - moon-pale on the planes turned to the key, cold grey in the shade."""
    hem = 0.0
    if phase is not None:
        c, _ln, _lf = roster.stride(phase)
        hem = 1.2 * c
    # The bodice: narrow, the near side turned to her own light.
    roster.mass(g, [(15, 23, 31), (18, 24, 31), (22, 25, 30)], CH_MID, CH_LIT, CH_DARK, rim=False)
    roster.fold(g, [(26, 16), (26, 21)], CH_DARK, CH_LIT)
    roster.fold(g, [(29, 17), (29, 21)], CH_DARK, None)
    # The skirt, to the knee, flared by the stride; its folds run with the swing.
    roster.mass(g, [(23, 24, 30), (27, 22, 31), (30, 21 + hem, 32 + hem), (33, 20 + hem, 33 + hem)],
                CH_MID, CH_LIT, CH_DARK, rim=False)
    roster.fold(g, [(24, 26), (23 + hem, 32)], CH_DARK, CH_LIT)
    roster.fold(g, [(27, 26), (27 + hem, 33)], CH_DARK, CH_LIT)
    roster.fold(g, [(30, 26), (31 + hem, 32)], CH_DARK, None)
    for x in range(int(round(20 + hem)), int(round(34 + hem))):
        if (x * 5) % 7 < 2 and g.get(x, 33) != ".":
            g.set(x, 33, CH_DARK)                               # the hem's wave
    # The overfold over the belt: a lit lip and its shadow.
    g.span(24, 24, 30, CH_LIT)
    g.set(24, 24, "s")
    g.span(25, 23, 30, CH_DARK)
    # The belt: gold, high, knotted at the front, its ends hanging.
    g.span(23, 25, 30, "g")
    g.set(25, 23, "G")
    g.set(30, 23, "u")
    g.set(28, 24, "G")
    g.set(28, 25, "u")


# --- the arm --------------------------------------------------------------------------------------

def arm(g):
    """The near arm, bare, down her side, the hand closed on the string where it comes to her hip - her thumb hooked
    under it, as an archer carries a bow on the march. A gold armlet above the elbow. The far arm is under the
    mantle."""
    roster.mass(g, [(15, 31, 33), (19, 32, 33), (22, 32, 34)], "h", "f", "i", rim=False)
    g.set(31, 21, "o")                                      # the arm's shadow on the bodice
    g.set(31, 22, "o")
    g.set(32, 23, "i")
    g.set(33, 23, "h")
    g.set(33, 24, "i")
    roster.hand(g, 30, 25)
    g.set(30, 27, "o")
    g.span(19, 32, 33, "G")                                 # the armlet
    g.set(34, 19, "u")
    # The brooch that pins the mantle at the near shoulder.
    g.set(29, 15, "G")
    g.set(30, 15, "u")


# --- the head -------------------------------------------------------------------------------------

def head(g):
    """Her face, level and unsmiling, turned the way she walks; her hair, dark, drawn back from the brow into a knot
    behind the crown - the knot breaks her outline where a hat breaks a wizard's."""
    roster.head(g, HX, HY, 2.2, brow="flat", gaze=1, mouth="set", eye="open")
    g.set(HX - 2, HY + 3, "i")                              # the cheek
    g.span(14, 26, 27, "i")                                 # the neck, in the jaw's shadow
    g.set(26, 14, "h")
    # The hair: a cap over the crown, drawn back over the far side of her head.
    for (y, lo, hi) in ((6, 25, 29), (7, 24, 30), (8, 24, 25), (9, 24, 25), (10, 24, 24), (11, 24, 25), (12, 25, 25)):
        g.span(y, lo, hi, "j")
        g.set(lo, y, "N")
    g.set(30, 7, "J")
    g.set(29, 8, "J")
    # The knot at the back of her head, a gold band through it: it breaks her outline where a hat breaks a wizard's.
    for (y, lo, hi) in ((6, 22, 23), (7, 21, 24), (8, 21, 24), (9, 22, 23)):
        g.span(y, lo, hi, "j")
        g.set(lo, y, "N")
        g.set(hi, y, "J")
    g.set(22, 7, "g")
    g.set(22, 8, "u")


# --- the light and the rim ------------------------------------------------------------------------

def rim(g):
    """The bone rim on every row's key-side edge, as on every actor - never on occlusion. Laid before the bow and
    the crescent, so the bow's limbs and the light's halo are not themselves rimmed (on the Archmage a rimmed staff
    read as a glowing rod)."""
    for y in range(2, FEET):
        row = g.g[y]
        for x in range(1, CELL):
            if row[x] != "." and row[x - 1] == ".":
                if row[x] != "o":
                    g.set(x - 1, y, "W")
                break


def stave(g, pts, lit, mid, dark):
    """A bow limb two pixels thick along a polyline, every step joined (a curve of single steps reads as a dotted
    line at game size): its key side lit, its far side a stop down, drawn only where nothing else is."""
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = int(max(abs(x1 - x0), abs(y1 - y0)))
        for i in range(n + 1):
            t = i / float(max(1, n))
            x, y = int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t))
            set_if_empty(g, x, y, lit)
            set_if_empty(g, x + 1, y, mid)
            set_if_empty(g, x + 2, y, dark if dark else ".")


# The bow's two limbs (the grip is behind her back): the upper one rising behind her far shoulder past her head to
# a tip turned back, the lower one out past her near hip to a tip turned back - and the nocks the string runs between.
BOW_UPPER = [(22, 15), (20, 12), (19, 9), (18, 6), (18, 4), (19, 3)]
BOW_LOWER = [(33, 23), (34, 25), (35, 27), (35, 30), (35, 32), (34, 34)]


def bow(g):
    """The bow slung across her back, gold - Homer's "of the golden bow" - and its string from nock to nock: pale
    against the night above her shoulder, dark over the chiton across her chest, out over the skirt's edge to the
    lower nock, so the two limbs read as one bow and not as two loose curls of gold. Behind her: drawn only where
    nothing else is."""
    stave(g, BOW_UPPER, "G", "g", None)
    stave(g, BOW_LOWER, "G", "u", None)
    set_if_empty(g, 20, 3, "H")                             # the upper tip catches the key
    set_if_empty(g, 34, 35, "g")                            # the lower tip, turned back
    set_if_empty(g, 33, 35, "u")
    for k in range(11):
        set_if_empty(g, int(round(20.5 + k * 0.35)), 4 + k, "n")
    g.line(24, 16, 33, 34, "J")


def crescent(g):
    """The crescent at her brow: the moon lying on its back, a shallow boat a little wider than her face, resting on
    her hair - white at the bowl, the light ramp's hot and mid stops out to the horns. Lying, not standing: two horns
    stood up on a head read as horns. Her crown shows dark in its cup, which is what makes it a crescent and not a
    band; one point of its edge stop clear of her, since a light is the fade round it and not a ring."""
    cx, cy = CRESCENT
    g.span(cy + 1, cx - 1, cx + 1, LT0)
    for sgn in (-1, 1):
        g.set(cx + 2 * sgn, cy + 1, LT1)
        g.set(cx + 3 * sgn, cy, LT2)
    g.set(cx + 1, cy + 1, LT1)
    g.span(cy, cx - 2, cx + 2, "J")                         # the crown in its cup
    g.set(cx - 2, cy, "j")
    g.set(cx - 1, cy, "N")
    set_if_empty(g, cx + 5, cy - 2, LT3)


# --- the figure -----------------------------------------------------------------------------------

def artemis(phase=None):
    """Sagittarius. The huntress, rekindled: the moon at her brow, the bow on her back. `phase` (0..1) is the walk;
    None is the pose the idle and the portrait are built from."""
    g = G(CELL)
    far, near = ankles(phase)
    mantle(g, phase)
    # Both legs under the chiton, the near one over the far: the skirt covers the thighs, as a skirt does.
    leg(g, HIP_FAR, far, far=True)
    leg(g, HIP_NEAR, near, far=False)
    chiton(g, phase)
    arm(g)
    head(g)
    rim(g)
    bow(g)
    crescent(g)
    roster.contact(g, 16, 36)
    return g.rows()


def build(root=ROOT):
    pose = artemis()
    idle = roster.idle(pose)
    problems = P.check_anchor("%s pose" % STEM, [pose], CELL)
    P.write_frames(idle, PALETTE, os.path.join(root, OUT_CHARS, STEM + "-%d.png"), CELL)
    anims = anim_sets.player_set(pose, idle, artemis)
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


def crescent_centre(rows):
    """The centroid of the crescent's own pixels (the light ramp's core three stops near CRESCENT) - what
    CharacterVisuals.GetStaffTipCell is measured as."""
    pts = [(x, y) for y, row in enumerate(rows) for x, ch in enumerate(row)
           if ch in (LT0, LT1, LT2) and abs(x - CRESCENT[0]) <= 3 and abs(y - CRESCENT[1]) <= 3]
    n = float(len(pts))
    return sum(p[0] for p in pts) / n, sum(p[1] for p in pts) / n


def preview(pose, anims):
    black = ["".join("o" if ch != "." else "." for ch in row) for row in pose]
    sets = [("pose", [pose]), ("silhouette", [black])]
    for anim, frames, _loop, _speed in anims:
        sets.append((anim, frames))
    P.preview(sets, dict(PALETTE, o="#05070C"), os.path.join(HERE, "_player_artemis.png"),
              zoom=8, bg=(30, 34, 46), cell=CELL)


def main():
    if "--preview" in sys.argv:
        pose = artemis()
        anims = anim_sets.player_set(pose, roster.idle(pose), artemis)
        problems = P.check_anchor("%s pose" % STEM, [pose], CELL)
    else:
        pose, anims, problems = build()
    filled, edges, ratio = roster.density(pose)
    cx, cy = crescent_centre(pose)
    print("%s  %dx%d  %d px  %.2f edges/px%s  crescent centre (%.1f, %.1f)  -> scenes/resources/%s.tres"
          % (STEM, CELL, CELL, filled, ratio, "" if ratio >= 0.80 else "   SPARSE", cx, cy, SHEET))
    preview(pose, anims)
    if problems:
        print("ANCHOR PROBLEMS:")
        for p in problems:
            print("   " + p)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
