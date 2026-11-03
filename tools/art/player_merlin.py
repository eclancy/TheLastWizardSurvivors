# -*- coding: utf-8 -*-
"""Merlin, playable, in a look of his own. Run: python tools/art/player_merlin.py

    assets/bonelight/characters/merlin-{1,2,3,4}.png      the idle, frame one is the portrait
    assets/bonelight/characters/merlin-{idle,moving,hurt,death}.png
    scenes/resources/MerlinFrames.tres
    tools/art/_player_merlin.png     (gitignored) the pose at x8, filled black, and every animation

    --preview   writes only the preview; nothing under assets/ or scenes/ is touched.

THE LOOK IS ONE OF THREE, drawn in merlin_concepts.py and compared on its sheet (python
tools/art/merlin_concepts.py -> tools/art/_merlin_concepts/sheet.png); SHIPPING below says which, the way
app_icon.py ships one of icon_concepts.py's designs. To ship another: change SHIPPING, run this, and
move CharacterVisuals.GetStaffTipCell("merlin") to the orb centre this prints (the ward's orb leaves from
there and comes home to it).

HE USED TO BE THE TITLE FIGURE, redrawn on this cell from hero.wizard_hero - the blue wool robe, the
starred cone hat, the forked gold staff with the orb in its claw. Eric, 2026-11-02: "I don't actually want
the blue wizard to be Merlin... The blue robed wizard should represent player." The game is named for that
figure, the one he missed, so Merlin wears none of it now (merlin_concepts.py says what every look holds
to).

SHIPPING: A, THE CRYSTAL CAVE. The enchanter of the court, bare-headed - the only bare crown in the cast -
with a white fringe, heavy white brows and a forked beard; an ivory mantle bordered in gold over a darker
under-robe; a straight staff planted at arm's length and crowned with raw quartz, the cave he was sealed
in, with the light inside it; his free hand held out low over the ground, palm down - Warding Hand. Of the
three it is the one that is all of the brief at once (the Arthurian enchanter; Light; an old man who
protects), the clearest in colour (nobody in the cast is warm white), and its silhouette clears the cast's
own nearest pair. The Wood (B, the antlered Myrddin) is the bolder silhouette and the alternate.

HE FACES THE VIEWER, staff on the left of the sprite, so the player's left/right flip is right for him
(CharacterVisuals.SpriteFacesLeft false). His walk swings the hem, shows a toe at each contact and drags the
mantle a quarter-step behind; the staff is planted on one step and carried on the other.
"""
from __future__ import print_function

import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import anim_sets
import merlin_concepts as MC
import pixel as P
import roster
import spriteframes

# Which of merlin_concepts.DESIGNS ships (A, B or C).
SHIPPING = "A"

CELL = MC.CELL
STEM = "merlin"
SHEET = "MerlinFrames"
OUT_CHARS = os.path.join("assets", "bonelight", "characters")
PALETTE = MC.PALETTE
RED_RAMP = MC.RED_RAMP


def figure():
    """(pose function, where its light is) for the shipping look."""
    _key, _name, fn, origin = MC.design(SHIPPING)
    return fn, origin


def build(root=ROOT):
    fn, _origin = figure()
    pose = fn()
    idle = roster.idle(pose)
    problems = P.check_anchor("%s pose" % STEM, [pose], CELL) + MC.bounds_problems("%s pose" % STEM, [pose])
    P.write_frames(idle, PALETTE, os.path.join(root, OUT_CHARS, STEM + "-%d.png"), CELL)
    anims = anim_sets.player_set(pose, idle, fn)
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


def preview(pose, anims):
    black = ["".join("o" if ch != "." else "." for ch in row) for row in pose]
    sets = [("pose", [pose]), ("silhouette", [black])]
    for anim, frames, _loop, _speed in anims:
        sets.append((anim, frames))
    P.preview(sets, dict(PALETTE, o="#05070C"), os.path.join(HERE, "_player_merlin.png"),
              zoom=8, bg=(30, 34, 46), cell=CELL)


def main():
    fn, origin = figure()
    if "--preview" in sys.argv:
        pose = fn()
        anims = anim_sets.player_set(pose, roster.idle(pose), fn)
        problems = P.check_anchor("%s pose" % STEM, [pose], CELL) + MC.bounds_problems("%s pose" % STEM, [pose])
    else:
        pose, anims, problems = build()
    filled, edges, ratio = roster.density(pose)
    ox, oy = MC.light_centre(pose, origin)
    print("%s (%s, %s)  %dx%d  %d px  %.2f edges/px%s  orb centre (%.1f, %.1f) -> GetStaffTipCell (%d, %d)"
          "  -> scenes/resources/%s.tres"
          % (STEM, SHIPPING, MC.design(SHIPPING)[1], CELL, CELL, filled, ratio,
             "" if ratio >= 0.80 else "   SPARSE", ox, oy, int(round(ox)), int(round(oy)), SHEET))
    preview(pose, anims)
    if problems:
        print("ANCHOR PROBLEMS:")
        for p in problems:
            print("   " + p)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
