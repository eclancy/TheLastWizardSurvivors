# -*- coding: utf-8 -*-
"""The Frozen Waste's ground detail. Drawn by tools/art/ground_detail.py, which finds this module by its
name and appends DETAIL_SETS to its own - run ground_detail.py, never this. GroundDetail.cs's Ice block
is what asks for it, and where.

The Frozen Waste stands on snow now (tiles.py, snow()), and snow is almost featureless by design: a
ridge drawn into a fill tile repeats every cell and reads as a printed carpet. The wind, which is
what says "a waste" rather than "a pale floor", is drawn here instead and laid in world space in
drifts:

  sastrugi    ridges the wind has cut in packed snow - three to five, lying along the wind (off the
              north-west, the same lean as the floor's own odd ridge), each a lit crest of the bone
              row's shade over a body of steel's shade and a lee in steel's deep. Stepped back from
              one another like waves, not ruled like a hatch.

The ground_detail.py rules hold: material tones only, nothing emissive, nothing round, bright and
alone; the brightest stop here is the bone row's shade, which is a step under the `snow` drift set's.
"""
import math

import ground_detail
from ground_detail import SK, STL, spot


def sastrugi(c, r):
    """A patch of wind ridges: each a shallow arc rising a little to the east, crest lit over its lee."""
    cx, cy = spot(r, 14)
    n = r.i(3, 4)
    for k in range(n):
        run = r.i(12, 18)
        # staggered along the wind, not stacked: stacked, a patch read as a printed equals sign
        x0 = cx - run // 2 + (k - n // 2) * r.i(4, 6) + r.i(-3, 3)
        y0 = cy + (k - n // 2) * r.i(5, 8)
        for j in range(run):
            u = j / float(run - 1)
            x = x0 + j
            y = y0 + int(round(-0.18 * j - math.sin(u * math.pi) * (1.2 + 0.6 * (k % 2))))
            # a crest thins to nothing at both ends, which is what keeps it from reading as a stroke
            if 0.3 < u < 0.7:
                c.set(x, y - 1, SK[3])
            c.set(x, y, STL[3])
            if 0.08 < u < 0.92:
                c.set(x, y + 1, STL[4])


DETAIL_SETS = [
    ("sastrugi", sastrugi, 4),
]
