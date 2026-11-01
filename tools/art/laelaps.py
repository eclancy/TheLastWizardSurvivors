# -*- coding: utf-8 -*-
"""Laelaps, the hound that always caught what it hunted - a playable character. Run: python tools/art/laelaps.py

WHO HE IS. In the Greek story Laelaps was a hound fated never to fail to catch what it hunted, set on
the Teumessian fox, which was fated never to be caught; the chase could not end, so both were turned to
stone where they ran. Here the dark wizard did the same to him on the high heath, among the standing
stones of the Broken Circle, and the black hares that run there are what he was chasing. A player frees
him by finishing the chase for him: the coursing deed (AchievementDefinitions "coursing" - a hound of
the player's own light running down the hares). .ai/world-and-tone.md asks that everyone unlocked after
the four disciplines be a named legend with a life before this, held somewhere by the dark wizard; he is
one - the one who is not a wizard, which is why he is outside the final chapter's gate (UnlockCatalog).

WHAT HE LOOKS LIKE, and why. Drawn by tools/art/familiar.py's hound renderer - the same skeleton, light,
passes and matte finish as the Familiar's hound (Fig.render(finish=True)) - so the two dogs in the game
are one family of drawing.

THE WIZARD OUTFIT (Eric, 2026-11-01: "When playing Laelaps, you should have the wizard outfit."). He
wears what the familiar wears, as the familiar wears it: the small pointed hat in the wizard's indigo
wool with its gold band and star, the mantle over the shoulders with its gold hem (the Great Hound's),
and the gold star on the collar. He was first drawn bare - the hat read as the wizard's mark on the
wizard's creatures, and Laelaps belonged to no one. But once he is played he is on the player's side,
and the outfit is how the game says so: it is the silhouette-breaking attachment art-direction.md
section 4 asks for, and the player has to find himself at a glance in a swarm that holds four kinds of
dark dog (the Dire Wolf, the Bog Hound, the Sled Hound, the Chainhound). Filled solid black, a hound in
a pointed hat is still the player's, and no enemy wears one. The legend survives the costume: his
story is the chase, and the outfit is only whose side he runs on now.

What still tells him from the familiar at a glance:

  - A BRONZE COAT. Warm - the player's cast is the one thing allowed to be bright - but a saturated
    bronze, not the familiar's pale fawn: amber and flesh in the light, the gold row's dark stops in
    the shade (never its light ones, which the matte finish treats as a light source and would leave
    glinting on a coat).
  - STONE FEET. His paws are still the bone-grey of the stone he was held in.
  - THE MANTLE. The base familiar runs bare-shouldered; only its level 4 Great Hound wears one, and
    that is a heavier dog on the 64 cell.

His eye is the calm eye (familiar.calm_eye; Eric, 2026-10-28, of the familiar's icon: "the eye is too
prominent"), because his first frame is also his portrait on the character select, shown at x5. When
he turns to stone at the end of his death, the outfit turns with him (Skin.outfit): a wool hat left on
a grey statue would read as something set on it afterwards.

THE CELL. The player is drawn on the 48 cell (world-and-tone.md, "The player at 48x48"); a hound fills
it lengthwise, at the familiar hound's 1.3, paws on the contract's feet row. He faces right, as the
wizards do (CharacterVisuals.SpriteFacesLeft is false for him).

THE PLAYER'S ANIMATION SET (art-direction.md section 5): idle 4, moving 8, hurt 2, death 6 - written as
laelaps-idle/-moving/-hurt/-death.png and scenes/resources/LaelapsFrames.tres, with the idle's frames
also written one by one (laelaps-1..4.png), which is what the character select animates from Portrait.
His death is the story's end: he folds down and turns back to stone.
"""
from __future__ import print_function, division

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import bonelight as B
import familiar as F
import spriteframes

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT_DIR = os.path.join(ROOT, "assets", "bonelight", "characters")
OUT_REL = "assets/bonelight/characters"
FRAMES = os.path.join(ROOT, "scenes", "resources", "LaelapsFrames.tres")
PREVIEW = os.path.join(HERE, "_laelaps.png")

M = B.MATERIALS
CELL = 48
SCALE = 1.3

# The bronze: amber and flesh in the light, the gold row's dark stops in the shade.
BRONZE = [M["flesh"][1], M["amber"][2], M["amber"][3], M["gold"][3], M["gold"][4]]
# The stone he was held in, still on his feet, and all of him as he dies.
STONE_FEET = [M["skin"][1], M["skin"][2], M["skin"][3], M["stone"][0], M["stone"][2]]

# The outfit: one head, wearing the hat; the mantle; the star on the collar. Lean and long-legged like the
# familiar hound, a little deeper in the chest; the eye calm.
LAELAPS = F.Skin(BRONZE, leap=0.85, head_scale=1.15, heavy=0.2, paws=STONE_FEET, mantle=True,
                 collar_star=True, calm=True, depth=0.4)
# Turning to stone: the same hound in the stone's grey, and the outfit with him.
# Its eye is stone too: a lit eye left in a grey head reads as something still looking out of it.
STONE = F.Skin(STONE_FEET, eye=(M["stone"][2], M["stone"][3]), leap=0.85, head_scale=1.15, heavy=0.2,
               mantle=True, collar_star=True, calm=True, depth=0.2,
               outfit=(STONE_FEET, STONE_FEET, STONE_FEET[1]))

P = F._pose


def _gallop8():
    return [F.gallop(k, 8) for k in range(8)]


# The hat sits back a little, as the familiar's does, and lifts with the breath; the mantle stirs (flap).
IDLE = [P(tail=0.1, breathe=0.0, hat=-0.08), P(tail=0.3, breathe=0.6, ear=0.1, hat=-0.08, flap=0.6),
        P(tail=0.45, breathe=1.0, ear=0.2, hat=-0.06, flap=1.2), P(tail=0.3, breathe=0.5, ear=0.1, hat=-0.08, flap=0.6)]
# Struck: he flinches down and away, head pulled back - then half recovers.
HURT = [P(crouch=1.6, head=(-0.6, 0.6, -0.25), jaw=0.3, tail=-0.4, ear=-0.6, swing=(-0.3, -0.2, 0.3, 0.2), hat=-0.3,
          flap=1.0),
        P(crouch=0.7, head=(-0.3, 0.3, -0.1), jaw=0.1, tail=-0.1, ear=-0.3, hat=-0.15, flap=0.5)]
# Falling: he folds down onto his legs, lays his head down - and the stone comes back over him.
DEATH = [P(crouch=1.4, head=(0.2, 1.4, 0.2), tail=-0.3, ear=-0.4),
         P(crouch=3.2, head=(0.4, 2.8, 0.35), tail=-0.5, ear=-0.6),
         P(crouch=5.0, head=(0.6, 4.2, 0.5), tail=-0.6, ear=-0.7),
         P(crouch=6.2, head=(0.8, 5.0, 0.55), tail=-0.7, ear=-0.8),
         P(crouch=6.2, head=(0.8, 5.0, 0.55), tail=-0.7, ear=-0.8),
         P(crouch=6.2, head=(0.8, 5.0, 0.55), tail=-0.7, ear=-0.8)]

ANIMS = [
    ("idle", [(LAELAPS, p) for p in IDLE], True, 6.0),
    ("moving", [(LAELAPS, p) for p in _gallop8()], True, 12.0),
    ("hurt", [(LAELAPS, p) for p in HURT], False, 12.0),
    # The last two frames of the fall are the stone.
    ("death", [(LAELAPS, p) for p in DEATH[:4]] + [(STONE, p) for p in DEATH[4:]], False, 8.0),
]


def frames():
    out = []
    for name, poses, loop, speed in ANIMS:
        out.append((name, [F.hound(CELL, skin, p, SCALE).render(finish=True) for skin, p in poses], loop, speed))
    return out


def main(write=True):
    anims = frames()
    bad = 0
    for name, cvs, _l, _s in anims:
        for c in cvs:
            off = c.audit()
            if off:
                bad += 1
                print("OFF-CONTRACT COLOURS in laelaps %s: %s" % (name, off[:4]))
    filled, ratio = F.density(anims[0][1][0])
    print("  laelaps 48x48  idle %4d px  %.2f edges/px" % (filled, ratio))
    F.preview([("laelaps", CELL, anims)], PREVIEW, zoom=5)
    if bad:
        return 1
    if not write:
        return 0
    spec = []
    for name, cvs, loop, speed in anims:
        png = "laelaps-%s.png" % name
        F.write_strip(cvs, os.path.join(OUT_DIR, png), CELL)
        spec.append((name, png, len(cvs), loop, speed))
    for i, c in enumerate(anims[0][1]):
        c.img.save(os.path.join(OUT_DIR, "laelaps-%d.png" % (i + 1)))
    spriteframes.write(FRAMES, spec, OUT_REL, cell=CELL)
    print("  laelaps  %d frames -> scenes/resources/LaelapsFrames.tres; portrait laelaps-1..4.png"
          % sum(a[2] for a in spec))
    return 0


if __name__ == "__main__":
    sys.exit(main(write="--preview" not in sys.argv))
