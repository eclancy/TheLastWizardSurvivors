# -*- coding: utf-8 -*-
"""Emit the Vigil title screen as animated layers.

Run: python tools/art/anim.py

A single baked PNG cannot animate a part of itself, so the screen comes apart into seven
layers, four of which get frame sequences, plus the eyes, which are small cells laid over the
picture. Read .ai/title-screen.md before changing any of this; the rule that governs all of it
is that every state is a GENERATED FRAME with a closed palette. Nothing is tinted, faded or cross-dissolved, because a blended pixel is not one of
the 66 colours the contract allows and the palette audit would stop meaning anything.

What animates, and off which clock:

  eyes        every pair blinks on its own clock. Not a layer: each pair is a small cell laid
              over the picture (eyes.png, three frames per row, and eyes.json, where each pair
              sits - tools/art/title_eyes.py), so sixteen pairs need three tiny frames rather
              than a whole-screen frame per pair
  ward        a slow size pulse in the light pools
  flamesA/B   the six flames, four phases - and these two MUST advance together, because
              they are the same six flames with the figure standing between them
  figure      the rim and bounce flicker, phase-LOCKED to the flames: driven separately it
              reads as two unrelated animations rather than as one light source

The flat splash.py output is unaffected - vigil() with layers=False still renders the
shipping PNG byte for byte.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import splash
import title_eyes

ROOT = splash.ROOT
OUT = os.path.join(ROOT, "assets", "bonelight", "ui", "title")
OUT_WIDE = os.path.join(ROOT, "assets", "bonelight", "ui", "title-wide")

GAINS = [1.00, 0.92, 0.84]          # ward pulse: pool radius and ring tone, never opacity
FLICKER = [1.00, 0.78, 0.55, 0.30]  # figure edge light, one per flame phase
# Four passes make every frame: the flames and the figure have four phases and the ward three, so
# pass 3 draws ward-0 again (identically). It was six while the eyes blinked by whole-screen frame.
PASSES = 4


def main():
    # Two sets: the portrait one the phone uses, and the landscape one a PC uses
    # (splash_wide.py). `python tools/art/anim.py wide` or `... tall` makes just one.
    which = sys.argv[1:] or ["tall", "wide"]
    if "tall" in which:
        emit(OUT, splash.vigil, splash.vigil_eyes)
    if "wide" in which:
        import splash_wide
        emit(OUT_WIDE, splash_wide.vigil_wide, splash_wide.wide_eyes)


def emit(out, vigil, eyes):
    if not os.path.isdir(out):
        os.makedirs(out)
    written = []
    bad = 0
    passes = []
    for k in range(PASSES):
        cv = vigil(layers=True, gain=GAINS[k % 3], phase=k % 4, flicker=FLICKER[k % 4])
        passes.append(cv)
        take = [("ward", "ward-%d" % (k % 3)),
                ("flamesA", "flames-far-%d" % (k % 4)),
                ("flamesB", "flames-near-%d" % (k % 4)),
                ("figure", "figure-%d" % (k % 4))]
        if k == 0:
            take += [("backA", "back-a"), ("backB", "back-b"), ("fore", "fore")]
        for stage, name in take:
            c = cv[stage]
            bad += len(c.audit())
            c.save(os.path.join(out, name + ".png"))
            written.append(name)
        print("pass %d: gain=%.2f phase=%d flicker=%.2f"
              % (k, GAINS[k % 3], k % 4, FLICKER[k % 4]))
    # The eyes: a sheet of cells and where each pair goes, not a layer (see title_eyes.py).
    pairs = eyes()
    bad += len(title_eyes.write(out, pairs, (passes[0]["backA"].w, passes[0]["backA"].h)))
    # Placement looked at frame 0. Every other frame of everything in front of the eyes has to
    # leave every pair alone too, or a flame or the orb's breath would bite a piece out of one.
    hit = title_eyes.covered(pairs, passes)
    for p in hit:
        print("EYES COVERED in some frame: %s" % p)
    print("wrote %d layers, eyes.png and eyes.json (%s) to %s"
          % (len(set(written)), title_eyes.report(pairs), out))
    print("off-contract colours across every layer: %d; covered pairs: %d" % (bad, len(hit)))


if __name__ == "__main__":
    main()
