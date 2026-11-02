# -*- coding: utf-8 -*-
"""The settings controls: the on/off switch and the slider knob.

Run: python tools/art/ui_controls.py

Writes four files to assets/bonelight/ui/ and nothing else:

  ui-switch-off.png, ui-switch-on.png   the CheckButton icons (SettingsRows.StyleSwitch)
  ui-slider-knob.png, ui-slider-knob-lit.png   the HSlider grabber, resting and selected

WHY A FILE OF ITS OWN. These four were Godot's defaults until the menu pass of 2026-11-01 (Eric:
"update old menu styling to fit the rest of the game"): a switch drawn as a grey dot the size of a
full stop, and a white disc on the volume bars. They are small and fixed-size - an icon, never a
nine-slice - so they share nothing with ui_frames.py but the light model, and keeping them out of
it means re-drawing a switch never re-rolls the torn pages that generator seeds.

THE LIGHT MODEL is ui_frames.py's (art-direction.md section 2): key from the upper left, so a
recessed slot is shaded on its top and left and lit on its bottom and right, and a raised knob is
the inverse. ON is said in the one warm note every frame in the game carries, the gold inlay: the
slot fills with gold and the knob crosses to it. OFF is bare stone with an iron knob - present and
legible, not greyed out, because "off" is a setting, not a disabled control.

Authored at half size and written at x2, like every Bonelight asset, so a pixel here is the size of
a pixel in the frames around it.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bonelight as bl
import raster

OCC = bl.OCC
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "assets", "bonelight", "ui")

# The switch: 24x12 authored, 48x24 written. Tall enough to read beside a 20-24px label, short
# enough that a row of them stays a list and not a column of buttons.
SW_W = 24
SW_H = 12

# A pixel circle 12 across and 10 across, as the inset of each row from the shape's side. Rounded
# ends are what make it read as a switch rather than a small button.
PILL_12 = [4, 2, 1, 1, 0, 0, 0, 0, 1, 1, 2, 4]
PILL_10 = [3, 1, 1, 0, 0, 0, 0, 1, 1, 3]
KNOB_8 = [2, 1, 0, 0, 0, 0, 1, 2]


def _shape(c, x0, y0, width, insets, colour):
    """Fills a rounded shape: row i of `insets` spans x0 + inset .. x0 + width - 1 - inset."""
    for i, inset in enumerate(insets):
        c.hline(x0 + inset, x0 + width - 1 - inset, y0 + i, colour)


def _edge(c, x0, y0, width, insets, top, bottom):
    """The rim of a rounded shape, lit or shaded by side: the upper half takes `top`, the lower
    half `bottom` - the whole of a bevel at this size."""
    rows = len(insets)
    for i, inset in enumerate(insets):
        colour = top if i < rows // 2 else bottom
        left = x0 + inset
        right = x0 + width - 1 - inset
        if i == 0 or i == rows - 1:
            c.hline(left, right, y0 + i, colour)
        else:
            c.set(left, y0 + i, colour)
            c.set(right, y0 + i, colour)
            # The step of a rounded corner: where this row is wider than the one beyond it, the
            # pixels it adds are rim too, or the curve has holes in it.
            beyond = insets[i - 1] if i < rows // 2 else insets[i + 1]
            for x in range(left + 1, left + (beyond - inset)):
                c.set(x, y0 + i, colour)
            for x in range(right - (beyond - inset) + 1, right):
                c.set(x, y0 + i, colour)


def _knob(c, x0, y0, lit):
    """An iron stud, 8 across, raised: lit on its upper left, shaded on its lower right, with one
    glint pixel. `lit` raises it a step up the steel ramp, for the ON position."""
    hi = bl.tone("steel", "hi" if lit else "lit")
    face = bl.tone("steel", "lit" if lit else "base")
    shade = bl.tone("steel", "base" if lit else "shade")
    _shape(c, x0, y0, 8, KNOB_8, face)
    _edge(c, x0, y0, 8, KNOB_8, hi, shade)
    # A contact shadow under the stud, so it sits on the slot rather than floating over it.
    c.hline(x0 + 2, x0 + 5, y0 + 8, OCC)
    c.set(x0 + 2, y0 + 2, bl.tone("steel", "hi"))


def switch(on):
    """The on/off switch. Same geometry both ways; only the slot's fill and the knob's side move,
    so a row never shifts when it is flipped."""
    c = raster.Canvas(SW_W, SW_H, None)

    # The outer contact line, the whole pill, then the slot recessed one pixel inside it.
    _shape(c, 0, 0, SW_W, PILL_12, OCC)
    if on:
        slot = bl.tone("gold", "base")
        rim_top = bl.tone("gold", "shade")    # recessed: shaded above...
        rim_bottom = bl.tone("gold", "lit")   # ...lit below
    else:
        slot = bl.tone("stone", "deep")
        rim_top = OCC
        rim_bottom = bl.tone("stone", "hi")
    _shape(c, 1, 1, SW_W - 2, PILL_10, slot)
    _edge(c, 1, 1, SW_W - 2, PILL_10, rim_top, rim_bottom)

    # The knob: left when off, right when on, one pixel inside the slot's ends.
    _knob(c, SW_W - 11 if on else 3, 2, on)
    return c


def slider_knob(lit):
    """The volume sliders' grabber: the same iron stud as the switch, ringed in the gold inlay when
    the slider is selected or under the pointer, so a pad user sees which bar the stick moves."""
    size = 12
    c = raster.Canvas(size, size, None)
    _shape(c, 0, 0, size, PILL_12, OCC)
    ring = bl.tone("gold", "hi") if lit else bl.tone("steel", "shade")
    _shape(c, 1, 1, size - 2, PILL_10, ring)
    if lit:
        _edge(c, 1, 1, size - 2, PILL_10, bl.tone("gold", "hi"), bl.tone("gold", "lit"))
    _knob(c, 2, 2, lit)
    return c


def write(canvas, name):
    bad = canvas.audit()
    if bad:
        print("OFF-CONTRACT COLOURS in %s (art-direction.md section 3):" % name)
        for h, n in bad[:10]:
            print("   %s  x%d" % (h, n))
        return False
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)
    canvas.scaled(2).save(os.path.join(OUT_DIR, name))
    print("  %-24s %dx%d" % (name, canvas.w * 2, canvas.h * 2))
    return True


def main():
    ok = True
    print("settings controls:")
    ok &= write(switch(False), "ui-switch-off.png")
    ok &= write(switch(True), "ui-switch-on.png")
    ok &= write(slider_knob(False), "ui-slider-knob.png")
    ok &= write(slider_knob(True), "ui-slider-knob-lit.png")
    if not ok:
        return 1
    print("palette clean: every pixel is a colour art-direction.md defines")
    return 0


if __name__ == "__main__":
    sys.exit(main())
