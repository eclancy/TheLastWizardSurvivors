# -*- coding: utf-8 -*-
"""Encode _DiscoveryPreview's actual rendered frames; no drawn/fabricated animation frames.
Usage: python tools/art/discovery_gif.py marks|tracks|pen
"""
from __future__ import print_function
import glob
import os
import sys
from PIL import Image, ImageChops

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else "marks"
    folder = os.path.join(os.environ["APPDATA"], "Godot", "app_userdata", "The One He Missed")
    files = sorted(glob.glob(os.path.join(folder, "discovery-%s-*.png" % mode)))
    if len(files) != 40:
        raise ValueError("Expected 40 rendered frames, got %d" % len(files))
    palette = Image.open(files[0]).convert("RGB").convert("P", palette=Image.ADAPTIVE, colors=255)
    originals = [Image.open(path).convert("RGB").quantize(palette=palette, dither=Image.NONE) for path in files]
    # Transparent delta frames preserve every changing game pixel without re-encoding the
    # static, finely textured ground forty times. Index 255 is reserved, never a game colour.
    frames = [originals[0]]
    for i in range(1, len(originals)):
        before = Image.frombytes("L", originals[i-1].size, originals[i-1].tobytes())
        after = Image.frombytes("L", originals[i].size, originals[i].tobytes())
        changed = ImageChops.difference(before, after).point(lambda p: 255 if p else 0)
        delta = Image.new("P", originals[i].size, 255)
        delta.putpalette(palette.getpalette())
        delta.paste(originals[i], (0, 0), changed)
        frames.append(delta)
    out = os.path.join(ROOT, "build", "visuals")
    if not os.path.isdir(out): os.makedirs(out)
    path = os.path.join(out, "discovery-%s.gif" % mode)
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=100, loop=0,
                   disposal=1, transparency=255, optimize=False)
    # Pillow 6 exposes transparent patches rather than compositing them on seek. Check the
    # animation exactly as a GIF viewer does, retaining the preceding frame underneath.
    decoded = Image.open(path)
    canvas = Image.new("RGBA", decoded.size)
    for i in range(len(originals)):
        decoded.seek(i)
        canvas = Image.alpha_composite(canvas, decoded.convert("RGBA"))
        if ImageChops.difference(canvas.convert("RGB"), originals[i].convert("RGB")).getbbox():
            raise ValueError("GIF compositing mismatch at frame %d" % i)
    print("%s (%d frames, %.2f MB)" % (path, len(frames), os.path.getsize(path) / 1048576.0))
    return 0


if __name__ == "__main__": sys.exit(main())
