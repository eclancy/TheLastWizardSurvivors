# -*- coding: utf-8 -*-
"""Compare the actual Fields terrain painters without overwriting shipping textures.
Run: python tools/art/fields_studies.py. Output: build/visuals/fields-options.png.
"""
from __future__ import print_function
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageFont
import terrain_fields as F
import raster

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "build", "visuals")
LABELS = [("original", "CURRENT", "Dark gold soil; narrow, repeated furrows"),
          ("weathered", "A - WEATHERED EARTH", "Muted grey-green stubble / warm clay ridges"),
          ("harvest", "B - DRY HARVEST", "Lighter ochre straw / dark brown ploughed soil"),
          ("silver", "C - SILVER STUBBLE", "Cool silver-grey stalks / olive-grey earth")]


def panel(look):
    F.FIELD_LOOK = look
    tiles = []
    for kind in (F.body_stubble, F.body_furrow):
        variants = []
        for variant in range(6):
            F.T.VARIANT = variant
            c = raster.Canvas(48, 48, None)
            kind(c)
            if c.audit():
                raise ValueError("Off-palette ground: %s" % c.audit())
            variants.append(c.scaled(1))
        tiles.append(variants)
    scene = Image.new("RGBA", (576, 288))
    for y in range(6):
        for x in range(12):
            variant = (x * 5 + y * 3) % 6
            scene.paste(tiles[0][variant], (x * 48, y * 48))
    # A broad ploughed patch, using the same full body variants, over the stubble.
    for y in range(288):
        for x in range(576):
            edge = ((x - 390) / 160.0) ** 2 + ((y - 140) / 119.0) ** 2
            if edge < 0.94 or (edge < 1.06 and (x * 17 + y * 13) % 23 < 10):
                scene.putpixel((x, y), tiles[1][((x // 48) * 5 + (y // 48) * 3) % 6].getpixel((x % 48, y % 48)))
    return scene


def main():
    global LABELS
    close = "--closer" in sys.argv
    if close:
        LABELS = [("original", "CURRENT", "Original palette and texture"),
                  ("soft_umber", "D - SOFT UMBER", "Same texture; muted brown earth, clearer furrows"),
                  ("faded_gold", "E - FADED GOLD", "Original dark field; warmer, more visible soil ridges"),
                  ("cool_loam", "F - COOL LOAM", "Cooler dark earth; old gold straw highlights")]
    if not os.path.isdir(OUT): os.makedirs(OUT)
    title = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 22)
    small = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 16)
    board = Image.new("RGB", (1212, 776), "#10151E")
    d = ImageDraw.Draw(board)
    for i, (look, label, subtitle) in enumerate(LABELS):
        x, y = 20 + (i % 2) * 596, 16 + (i // 2) * 384
        d.text((x, y), label, font=title, fill="#E8DCC2")
        d.text((x, y + 30), subtitle, font=small, fill="#BCC5CD")
        image = panel(look)
        image.save(os.path.join(OUT, "fields-" + look + ".png"))
        board.paste(image, (x, y + 62))
    path = os.path.join(OUT, "fields-options-close.png" if close else "fields-options.png")
    board.save(path)
    print(path)
    return 0


if __name__ == "__main__": sys.exit(main())
