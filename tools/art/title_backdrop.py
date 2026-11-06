# -*- coding: utf-8 -*-
"""Separate the existing wide title's forest and lettering for screens beyond 16:9.
Run: python tools/art/title_backdrop.py. No character or animation art changes.
"""
from __future__ import print_function
import os
from PIL import Image
import raster
import splash
import splash_wide as wide
import title_lettering


def main():
    layers = wide.vigil_wide(layers=True, eyes=False, wordmark=False)
    forest = layers['backA'].img.copy()
    for name in ['backB', 'fore']:
        forest = Image.alpha_composite(forest, layers[name].img)
    letters = raster.Canvas(wide.WW, wide.WH)
    title_lettering.draw(letters, title_lettering.SHIPPING, 'wide', wide.C0)
    bad = sum(len(c.audit()) for c in layers.values()) + len(letters.audit())
    if bad:
        raise ValueError('%d off-contract colors' % bad)
    folder = os.path.join(splash.ROOT, 'assets', 'bonelight', 'ui', 'title-wide')
    forest.save(os.path.join(folder, 'backdrop.png'))
    letters.save(os.path.join(folder, 'lettering.png'))
    print('Title forest + lettering: 0 off-contract colors')


if __name__ == '__main__': main()
