# -*- coding: utf-8 -*-
"""The freeings: the short cutscene after the run that frees a chapter's wizard, one per wizard, made once.
And the REKINDLINGS: the same cutscene for a god of the far shore, on the far shore's own ground.

    python tools/art/freed.py                  # both shared grounds and every wizard's and god's panel
    python tools/art/freed.py tituba sibyl     # both shared grounds and only these wizards (or gods)
    python tools/art/freed.py --preview        # also write tools/art/_freed/<wizard>.png (gitignored)
    python tools/art/freed.py --preview --stand-in=medea
                                               # also the god template with a wizard's sprite standing in
                                               # for a god, to tools/art/_freed/rekindled-stand-in.png only
    "$GODOT_BIN" --headless --path . --import   # then import, and commit the .import sidecars

See .ai/cutscenes.md for what it shows and when; scripts/Prologue.cs plays it (Reels, "freed_<id>") and
scripts/Freeings.cs says when. Runs under Python 2.7 and 3 (PIL only), every random stream on
rng.random(), as prologue.py's are.

A TEMPLATE, NOT SIX PICTURES. One panel, the same for everyone: the heath at night with his tower on the
horizon, the Archmage - the one he missed, the title's own figure (hero.wizard_hero) - with his light up, and
beside him the wizard he has just freed: his real sprite, every pixel of his 48 cell at a whole x2 in his own
colours (prologue.sprite_figure; Eric, 2026-11-03: "Redraw with real sprites" - the flat one-colour cut-out
this drew first made Wayland one amber shape), standing, his chain broken off him and falling, and his light
coming back to him out of the sky it was drawn into. The ground and the near heath are one pair of layers shared
by every panel (freed-back.png, freed-fore.png); only the mid layer is the wizard's (freed-<id>-mid.png).
So a wizard added to the roster is one row in WIZARDS, here, and one in Freeings.cs.

THE REKINDLING (.ai/second-half.md 6.3; plumbing F1). A god of the far shore is not freed from a chain but
rekindled: its light comes home to its seat. Same template, its own shared ground (rekindled-back.png,
rekindled-fore.png; Freeings.GroundFor picks it for a god): the far shore at night under no moon - the cliff-top
downland they stand on, its chalk brink, the white cliffs going away along the coast - and across the dark sea,
between the two of them, his rock with the tower UNLIT. On the mid layer the god, its own sprite as the wizards
are, and its light coming DOWN out of the sky into it, a column narrowing as it lands, rather than drifting back
to it; no chain. The Archmage beside it as in every freeing. A god added to the roster is one row in GODS, here,
and one in Freeings.cs. GODS is empty until the first god's piece (F2) lands; --stand-in draws the template with
a wizard's sprite in the god's place so the ground can be judged before then.

THE FRAME, THE LAYERS AND THE AUDIT ARE THE PROLOGUE'S (prologue.py's docstring): 960x840 at a whole x2, the
360x360 square both canvases always show (columns 300-660, rows 240-600) holding every subject, and zero
off-contract pixels. Feet stay above row 552: on a PC the caption sits over rows ~558-578.
"""

from __future__ import division, print_function

import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image

import hero
import prologue as pr
import merlin_concepts as merlin_art

# Merlin's established ivory cloth is part of the art contract (art-direction.md section 3).
pr.M["ivory"] = merlin_art.IVORY
import splashkit as sk

S, V, G, ST, LI = pr.S, pr.V, pr.G, pr.ST, pr.LI
LT, DK = pr.LT, pr.DK
E, M, OCC = pr.E, pr.M, pr.OCC
FW, FH = pr.FW, pr.FH

ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "assets", "bonelight", "ui", "freed")
PREVIEW = os.path.join(HERE, "_freed")

# The wizards a chapter holds and the player frees there (UnlockCatalog: a FromEvent or chapter-deed
# character with a rescue chapter; Freeings.WizardIds, in the same order). (character id, (sprite prefix,
# the ramp his light is in the sprite, the ramp it burns here).)
# Laelaps carries no light of his own (the prologue's rule): his collar's star is found as light, and it
# burns his wind.
WIZARDS = [
    ("tituba", ("tituba", "water", "water")),
    ("vainamoinen", ("vainamoinen", "grass", "grass")),
    ("seimei", ("seimei", "light", "light")),
    ("wayland", ("wayland", "metal", "metal")),
    ("sibyl", ("sibyl", "arcane", "arcane")),
    ("laelaps", ("laelaps", "light", "wind")),
    ("baba_yaga", ("yaga", "grass", "grass")),
    ("morgan", ("morgan", "ice", "ice")),
    ("paracelsus", ("paracelsus", "poison", "poison")),
    ("nostradamus", ("nostradamus", "light", "light")),
    ("imhotep", ("imhotep", "fire", "fire")),
    ("medea", ("medea", "light", "light")),
    # The one he kept, freed by his fall in the Throne (.ai/endgame-castle.md decision 11): the sword upright in her fist
    # is her light. She floats in the panel as in the game - her sprite's shadow on the ground, her feet off it.
    ("nimue", ("nimue", "light", "light")),
    ("ma_liang", ("maliang", "arcane", "arcane")),
    ("merlin", ("merlin", "light", "light")),
]

# THE GODS OF THE FAR SHORE (.ai/second-half.md 3.2, 6.3), rekindled on the far shore's ground: the same columns as
# WIZARDS (character id, (sprite prefix, the ramp its light is in the sprite, the ramp it burns here)), in the order
# Freeings.WizardIds lists them. Each god's piece replaces its own comment line once its sprite exists
# (tools/art/player_<god>.py); leave the others as they are.
GODS = [
    # F2: ("artemis", ("artemis", <her light's ramp in the sprite>, "light")),
    # (F6's line is below; leave this one as it is)
    # F6: ("manannan", ("manannan", <his light's ramp in the sprite>, "water")),
    # (F7's line is below; leave this one as it is)
    # F7: ("hecate", ("hecate", <her light's ramp in the sprite>, "arcane")),
    # (F8's line is below; leave this one as it is)
    # F8: ("ra", ("ra", <his light's ramp in the sprite>, "fire")),
    # (F9's line is below; leave this one as it is)
    # F9: ("ukko", ("ukko", <his light's ramp in the sprite>, "lightning")),
    # (F10's line is below; leave this one as it is)
    # F10: ("hephaestus", ("hephaestus", <his light's ramp in the sprite>, "metal")),
]

# Where they stand: the Archmage left of the middle, the freed wizard right of it, turned to him.
HIM_X, HIM_Y, HIM_H = 428, 548, 120
WIZ_X, WIZ_Y = 560, 542

# The far shore (p_rekindled_ground): the sea's horizon, the chalk brink of the cliff-top they stand on, and his rock
# out on the water between the two of them - in the 360x360 square both canvases show, so the phone sees it too.
SEA_Y = 398
BRINK_Y = 462
ROCK_X, ROCK_H = 496, 42
COAST_X, NEAR_X = 572, 800    # the white cliffs: their far end on the horizon, and where the nearest one stands


def p_ground():
    """The shared back and fore: the heath at night, his tower far off on the horizon under his light (he
    still holds the land), a cold ground under the two of them and his gold where his light lands."""
    back, fore = pr.frame(True), pr.frame()
    pr.sky(back, 0, 420, [OCC, S[4], V[4], V[3], V[2]])
    pr.stars(back, (0, 0, FW, 340), 320, 501)
    sk.moon(back, 360, 268, 9)
    back.radial(706, 360, 220, 96, [V[3], V[4], None])
    pr.hills(back, pr.ridge_fn(392, 11, 503, 0.9), [V[3], V[4], S[4]], 30, lit=V[2])
    pr.tower(back, 706, 396, 40, seed=21)
    pr.hills(back, pr.ridge_fn(404, 9, 505, 1.4), [S[4], V[4], OCC], 36, lit=V[3])
    back.vramp(406, FH - 1, [S[4], S[4], OCC, OCC])
    pr.heath(back, 406, FH, 1800, 507, [(OCC, S[4]), (OCC, V[4])])
    for i, (x, y, w, h, lean, br) in enumerate([(226, 434, 18, 44, -0.3, True), (316, 424, 12, 30, 0.1, False),
                                                (780, 438, 16, 40, 0.35, True)]):
        pr.standing_stone(back, x, y, w, h, 1600 + i, lean=lean, broken=br, ramp=[S[2], S[3], S[4], OCC, OCC])
    back.radial((HIM_X + WIZ_X) / 2.0, HIM_Y - 24, 300, 70, [S[3], S[3], S[4], S[4], None])
    back.radial(HIM_X, HIM_Y + 4, 190, 48, [G[3], G[3], G[4], G[4], None])
    pr.heath(fore, 604, FH, 900, 509, [(OCC, OCC), (OCC, S[4])], scale=1.7)
    for (bx, by, bw, bh, sd) in [(250, 690, 60, 22, 11), (712, 676, 52, 20, 12)]:
        sk.boulder(fore, bx, by, bw, bh, seed=sd, ramp=[S[2], S[2], S[3], S[3], S[4]])
    return back, fore


def p_freed(who, seed):
    """One wizard's mid layer: him, and the wizard he has freed."""
    mid = pr.frame()
    prefix, carried, burns = who
    halo = pr.HALO[burns]
    mid.radial(WIZ_X, WIZ_Y + 4, 64, 15, [halo[3], halo[4], halo[4], None])
    # Where his sprite puts his light and his wrist, measured on a scratch layer: the motes go behind him.
    rim = (-0.8, -0.5, G[2])
    light, box = pr.sprite_figure(pr.frame(), who, WIZ_X, WIZ_Y, strip="idle", frame=0, facing=-1, rim=rim)
    # His light coming back to him, out of the sky (the procession's motes, the other way).
    ax, ay = light or ((box[0] + box[2]) / 2.0, box[1] + 10)
    pr.motes(mid, (ax + 40, ay - 180), (ax, ay), 190, seed, [E[burns]], spread=16, bow=-24)
    # The chain off him. Drawn after him, in front: two lengths of it on the ground at his feet with the
    # light still going out of them, its two shackles open, and the last of it falling from his wrist.
    # (Dead links in occlusion on the dark ground were tried first and could not be seen at all: the
    # chain is his light, and it has to read as his chain going out, not as gravel.)
    rng = random.Random(seed + 1)
    wx, wy = pr.sprite_wrist(box, -1)
    chain = []
    for (a, b, sag) in [((WIZ_X - 64, WIZ_Y + 8), (WIZ_X - 14, WIZ_Y + 12), 3.0),
                        ((WIZ_X + 4, WIZ_Y + 11), (WIZ_X + 48, WIZ_Y + 5), 2.0)]:
        chain.append(pr._chain_pts(a, b, n=int(math.hypot(b[0] - a[0], b[1] - a[1]) / 2.4), sag=sag))
    shackles = [(WIZ_X - 66, WIZ_Y + 7), (WIZ_X + 50, WIZ_Y + 4)]
    falling = []
    for k, col in enumerate([DK[1], DK[2], DK[2], DK[3], DK[3], V[3]]):
        falling.append((wx - 2 + (rng.random() - 0.5) * 1.6 * k, wy + 5 + k * 6 + k * k * 0.7, col))
    pr.sprite_figure(mid, who, WIZ_X, WIZ_Y, strip="idle", frame=0, facing=-1, rim=rim)
    for pts in chain:
        pr._chain(mid, pts, colors=(DK[2], DK[1]))
    for (x, y) in shackles:
        pr._shackle(mid, x, y, DK[2])
    for (x, y, col) in falling:
        mid.set(x, y + 1, DK[3])
        mid.set(x, y, col)
        mid.set(x + 1, y, col)
    hero.wizard_hero(mid, HIM_X, HIM_Y, h=HIM_H, staff_ramp=LT)
    return mid


K, L = pr.K, pr.L


def chalk_cliffs(c, x0, seed):
    """The far shore's white cliffs going away along the coast, east (right) of the frame's middle: headlands of chalk
    (the skin ramp, which is bone and chalk), nearer ones taller and standing lower on the water. Each face is lit by
    the key from the upper left, darker down its seaward flank and in the gullies the rain cut, under a cap of dark
    turf, with the surf a broken line at its foot."""
    rng = random.Random(seed)
    x = float(x0)
    heads = []
    # The nearest headland stands at the PC's right edge (column 800), so a PC sees the coast come all the way in;
    # a phone (columns 300-660) sees its far end, the white cliffs low on the water behind the god.
    reach = float(NEAR_X - x0)
    while x < FW:
        near = min(1.0, (x - x0) / reach)
        w = 26 + rng.random() * 22 + near * 70
        heads.append((x, x + w))
        x += w
    for (a, b) in heads:
        crest = 0.25 + rng.random() * 0.2          # where along the headland its top stands highest
        for xx in range(int(a), int(min(FW, b + 1))):
            near = min(1.0, (xx - x0) / reach)
            u = (xx - a) / float(b - a)
            # The coast comes toward the camera as it goes right: the far end on the horizon, the near end down by
            # the brink, and taller as it nears.
            foot = SEA_Y + 3 + near ** 1.4 * (BRINK_Y - 16 - SEA_Y)
            tall = (6 + near ** 1.25 * 92) * (0.66 + 0.34 * math.sin(min(1.0, u / crest) * math.pi / 2)
                                             - 0.34 * max(0.0, u - 0.8) / 0.2)
            top = int(round(foot - tall))
            gully = (xx * 5 + int(a)) % 9 in (0, 1) and rng.random() < 0.8
            cap = 2 + int(near * 3)
            for y in range(top, int(foot) + 1):
                depth = (y - top) / max(1.0, foot - top)
                if y <= top + cap:                     # the turf cap, the down running to the edge
                    col = S[4] if y == top else (LI[4] if pr.dith(xx, y, 0.5) else S[4])
                elif y >= foot - 1 - near * 2:         # the wave-cut foot, in the water's dark
                    col = S[4] if pr.dith(xx, y, 0.5) else OCC
                elif gully or u > 0.8:                 # the seaward flank and the gullies the rain cut
                    col = K[4] if pr.dith(xx, y, 0.4 + depth * 0.5) else K[3]
                else:                                  # the lit west face, darker toward the water
                    f = 0.15 + depth * 0.75 + u * 0.35
                    col = K[3] if pr.dith(xx, y, f) else K[2]
                    if y == top + cap + 1 and u < 0.55 and rng.random() < 0.6:
                        col = K[1]
                c.set(xx, y, col)
            # The surf at its foot: a broken line of the sea's lit edge.
            if rng.random() < 0.5:
                c.set(xx, int(foot) + 1, S[2] if rng.random() < 0.5 else V[2])


def p_rekindled_ground():
    """The shared back and fore of every rekindling (.ai/second-half.md 6.3): the far shore at night, no moon. The
    cliff-top downland the two of them stand on runs to its chalk brink; past it the dark sea to the horizon, the white
    cliffs going away along the coast on the right, and out on the water between them his rock with the tower unlit -
    the light that crowned it has gone home. Cold ground under them, his gold where the Archmage's light lands."""
    back, fore = pr.frame(True), pr.frame()
    pr.sky(back, 0, SEA_Y + 2, [OCC, S[4], V[4], V[3], V[2]])
    pr.stars(back, (0, 0, FW, SEA_Y - 60), 360, 611)
    # A breath of the sky's last light along the horizon, over the water, and nothing above it: no moon has risen.
    back.radial(FW / 2.0, SEA_Y, 420, 34, [V[3], V[3], V[4], None])
    # The dark sea, from the horizon to the brink, a step darker toward the cliff, with the last light glinting on it.
    back.vramp(SEA_Y + 1, BRINK_Y + 12, [V[4], S[4], S[4], OCC])
    rng = random.Random(613)
    for _ in range(260):
        d = rng.random() ** 1.7
        y = SEA_Y + 3 + d * (BRINK_Y - SEA_Y - 8)
        x = rng.random() * FW
        n = 2 + int(rng.random() * (6 - 4 * d))
        col = V[3] if d < 0.35 else S[3]
        back.hline(x, x + n, y, col)
    # His rock out on the water, and on it the tower, unlit (prologue.tower with no light: no black sun over it).
    rock = [(ROCK_X - 30, SEA_Y + 3), (ROCK_X - 22, SEA_Y - 4), (ROCK_X - 12, SEA_Y - 9), (ROCK_X - 2, SEA_Y - 11),
            (ROCK_X + 10, SEA_Y - 8), (ROCK_X + 20, SEA_Y - 3), (ROCK_X + 30, SEA_Y + 3)]
    back.poly(rock, OCC)
    back.line(ROCK_X - 22, SEA_Y - 4, ROCK_X - 2, SEA_Y - 11, V[4])
    pr.tower(back, ROCK_X - 2, SEA_Y - 9, ROCK_H, seed=23, lit=False)
    for k in range(-34, 35, 3):                    # the sea breaking at its foot
        back.set(ROCK_X + k, SEA_Y + 4 + (k % 2), S[3])
    chalk_cliffs(back, COAST_X, 617)
    # The cliff-top downland: short turf to the chalk brink, the brink a broken white lip where the turf ends over
    # the drop, chalk and flint showing through the grass.
    brink = pr.ridge_fn(BRINK_Y, 5, 619, 1.3)
    pr.hills(back, brink, [LI[4], S[4], S[4], OCC], 70)
    lip = random.Random(621)
    for x in range(FW):
        top = int(round(brink(x)))
        # The chalk shows at the brink in runs, where the turf has broken away over the drop; between them the grass
        # hangs over the edge and only a fleck shows.
        broken = math.sin(x * 0.047 + 0.6) + 0.7 * math.sin(x * 0.131 + 2.1) > -0.35
        if broken:
            back.set(x, top, K[3] if lip.random() < 0.75 else K[2])
            if lip.random() < 0.7:
                back.set(x, top + 1, K[4])
        elif lip.random() < 0.15:
            back.set(x, top, K[4])
        if lip.random() < 0.2:
            back.set(x, top - 1, OCC)                  # a tuft against the drop
    pr.heath(back, BRINK_Y + 4, FH, 1700, 623, [(OCC, S[4]), (OCC, LI[4]), (S[4], LI[3])])
    flints = random.Random(625)
    for _ in range(80):
        d = flints.random() ** 1.3
        y = BRINK_Y + 8 + d * (FH - BRINK_Y - 8)
        x = flints.random() * FW
        r = 0.8 + d * 2.6
        if flints.random() < 0.6:                  # chalk showing through
            back.disc(x, y, r * 1.5, r * 0.7, K[4])
            back.set(x - r * 0.4, y - r * 0.3, K[3])
        else:                                      # a flint, its white rind on the lit side
            back.disc(x, y, r, r * 0.7, OCC)
            back.set(x - r * 0.6, y - r * 0.4, K[4])
    back.radial((HIM_X + WIZ_X) / 2.0, HIM_Y - 24, 300, 70, [S[3], S[3], S[4], S[4], None])
    back.radial(HIM_X, HIM_Y + 4, 190, 48, [G[3], G[3], G[4], G[4], None])
    pr.heath(fore, 604, FH, 900, 627, [(OCC, OCC), (OCC, LI[4])], scale=1.7)
    for (bx, by, bw, bh, sd) in [(248, 692, 58, 22, 13), (716, 678, 50, 20, 14)]:
        sk.boulder(fore, bx, by, bw, bh, seed=sd, ramp=[K[3], K[3], K[4], K[4], S[4]])
    return back, fore


def p_rekindled(who, seed):
    """One god's mid layer (.ai/second-half.md 6.3): the god, its light coming home to it DOWN out of the sky - a column
    of its light narrowing as it lands, never back from the side as a freed wizard's does - and the Archmage beside it.
    No chain: nobody unlocks a cell."""
    mid = pr.frame()
    prefix, carried, burns = who
    halo = pr.HALO[burns]
    ramp = E[burns]
    mid.radial(WIZ_X, WIZ_Y + 4, 74, 17, [halo[3], halo[3], halo[4], halo[4], None])
    rim = (-0.8, -0.5, G[2])
    light, box = pr.sprite_figure(pr.frame(), who, WIZ_X, WIZ_Y, strip="idle", frame=0, facing=-1, rim=rim)
    ax, ay = light or ((box[0] + box[2]) / 2.0, box[1] + 10)
    # The column: wide and thin high up, gathering as it comes down, landing in the god's own light.
    pr.motes(mid, (ax + 6, ay - 330), (ax, ay), 300, seed, [ramp], spread=44, bow=0.0)
    pr.motes(mid, (ax + 2, ay - 150), (ax, ay), 120, seed + 1, [ramp], spread=12, bow=0.0)
    pr.sprite_figure(mid, who, WIZ_X, WIZ_Y, strip="idle", frame=0, facing=-1, rim=rim)
    # Where it lands: the god's light burning bright again at its anchor.
    mid.radial(ax, ay, 7, 7, [ramp[1], ramp[2], ramp[3], None])
    mid.set(ax, ay, ramp[0])
    hero.wizard_hero(mid, HIM_X, HIM_Y, h=HIM_H, staff_ramp=LT)
    return mid


def preview(name, layers):
    flat = pr.composite(layers)
    wide = flat.crop(pr.VIEW_WIDE)
    tall = flat.crop(pr.VIEW_TALL)
    sheet = Image.new("RGBA", (wide.size[0] + tall.size[0] + 12, tall.size[1]), (40, 40, 40, 255))
    sheet.paste(wide, (0, 0))
    sheet.paste(tall, (wide.size[0] + 12, 0))
    if not os.path.isdir(PREVIEW):
        os.makedirs(PREVIEW)
    sheet.save(os.path.join(PREVIEW, name + ".png"))


def _ground(name, build):
    """A shared ground, finished, audited and written: (back, fore, off-contract pixel count)."""
    back, fore = build()
    ground = dict(back=back, fore=fore)
    pr.finish(ground, 0.9, cy=pr.CY + 60)
    bad = sum(pr.audit(ground).values())
    back.img.save(os.path.join(OUT, "%s-back.png" % name))
    fore.img.save(os.path.join(OUT, "%s-fore.png" % name))
    print("%-18s %d off-contract pixels" % ("%s (ground)" % name, bad))
    return back, fore, bad


def _mid(wid, mid, back, fore, do_preview, out=True):
    """One panel's mid layer, finished, audited, written (unless out is False) and previewed: its off-contract count."""
    pr.finish(dict(mid=mid), 0.9, cy=pr.CY + 60)
    bad = pr.audit(dict(mid=mid))
    if out:
        mid.img.save(os.path.join(OUT, "freed-%s-mid.png" % wid))
    if do_preview:
        preview(wid, dict(back=back, mid=mid, fore=fore))
    print("%-18s %d off-contract pixels%s" % (wid, sum(bad.values()),
                                               (" " + str(sorted(bad.items())[:4])) if bad else ""))
    return sum(bad.values())


def main(argv):
    want = [a for a in argv if not a.startswith("--")]
    do_preview = "--preview" in argv
    stand_in = [a.split("=", 1)[1] for a in argv if a.startswith("--stand-in=")]
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    back, fore, total_bad = _ground("freed", p_ground)
    for i, (wid, who) in enumerate(WIZARDS):
        if want and wid not in want:
            continue
        total_bad += _mid(wid, p_freed(who, 700 + i * 10), back, fore, do_preview)
    # The far shore's ground and every god rekindled on it (Freeings.GroundFor: "rekindled").
    back, fore, bad = _ground("rekindled", p_rekindled_ground)
    total_bad += bad
    for i, (gid, who) in enumerate(GODS):
        if want and gid not in want:
            continue
        total_bad += _mid(gid, p_rekindled(who, 900 + i * 10), back, fore, do_preview)
    # A wizard's sprite standing in for a god, to judge the template before any god is drawn: preview only, never
    # written to the game's art ("--stand-in=medea", or "--stand-in=medea:light:light" for its ramps).
    for spec in stand_in:
        parts = spec.split(":")
        sprite = parts[0]
        if len(parts) >= 3:
            carried, burns = parts[1], parts[2]
        else:
            known = [w[1] for w in WIZARDS if w[1][0] == sprite]
            carried, burns = (known[0][1], known[0][2]) if known else ("light", "light")
        total_bad += _mid("rekindled-stand-in", p_rekindled((sprite, carried, burns), 990), back, fore, True, out=False)
    return 1 if total_bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
