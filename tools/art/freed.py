# -*- coding: utf-8 -*-
"""The freeings: the short cutscene after the run that frees a chapter's wizard, one per wizard, made once.

    python tools/art/freed.py                  # the shared ground and every wizard's panel
    python tools/art/freed.py tituba sibyl     # the shared ground and only these wizards
    python tools/art/freed.py --preview        # also write tools/art/_freed/<wizard>.png (gitignored)
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
    ("ma_liang", ("maliang", "arcane", "arcane")),
]

# Where they stand: the Archmage left of the middle, the freed wizard right of it, turned to him.
HIM_X, HIM_Y, HIM_H = 428, 548, 120
WIZ_X, WIZ_Y = 560, 542


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


def main(argv):
    want = [a for a in argv if not a.startswith("--")]
    do_preview = "--preview" in argv
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    back, fore = p_ground()
    total_bad = 0
    ground = dict(back=back, fore=fore)
    pr.finish(ground, 0.9, cy=pr.CY + 60)
    bad = pr.audit(ground)
    total_bad += sum(bad.values())
    back.img.save(os.path.join(OUT, "freed-back.png"))
    fore.img.save(os.path.join(OUT, "freed-fore.png"))
    print("%-18s %d off-contract pixels" % ("freed (ground)", sum(bad.values())))
    for i, (wid, who) in enumerate(WIZARDS):
        if want and wid not in want:
            continue
        mid = p_freed(who, 700 + i * 10)
        pr.finish(dict(mid=mid), 0.9, cy=pr.CY + 60)
        bad = pr.audit(dict(mid=mid))
        total_bad += sum(bad.values())
        mid.img.save(os.path.join(OUT, "freed-%s-mid.png" % wid))
        if do_preview:
            preview(wid, dict(back=back, mid=mid, fore=fore))
        print("%-18s %d off-contract pixels%s" % (wid, sum(bad.values()),
                                                   (" " + str(sorted(bad.items())[:4])) if bad else ""))
    return 1 if total_bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
