# -*- coding: utf-8 -*-
"""Chapter hint cues - what drifts in from the edge of the screen when something out there calls.

Run: python tools/art/hint_cues.py

One sheet, assets/bonelight/effects/hint-cues.png: one row per cue, each of eight 16x16 cells,
authored 1:1 and drawn at x3 by StageHintCue (48 world units, the attack icons' size - x2 was a
speck on the phone). See .ai/side-events.md, "Hints that something is out there".

WHY (Eric, playing 0.10.0): "The clues for each wizard should not just be text and an arrow. For
example, for the singing wizard, once in a while, the player should see musical notes on screen in
the direction of the wizard, and should hear the singing as well." So a hint is now a thing seen as
well as heard: a few marks in the site's own language come in from the edge of the screen on the
side the sound came from, drift a little way in and fade, on the beat of the sound. The words fade
in after them.

THE ROW ORDER IS AN API: HintCue's order (scripts/StageHintCatalog.cs).

  leaves   the grove      a leaf tumbling, eight frames of one turn - "leaves whispering, though
                          there is no wind", so the leaves move and nothing blows them
  echo     the cell       one arc of a voice, in eight directions: the arc bulges the way it
                          travels, toward the player, so three in a row read as a call coming down
                          a corridor (column = direction, 0 = east, then every 45 degrees clockwise)
  wisp     the lanterns   a cold flame, eight frames of a flicker - one of his lights, far off
  song     the thaw       a quaver, a pair beamed, a crotchet, a minim (0-3), then a frost glint
                          in four sizes (4-7) that trails each note
  bell     the caravan    the caravan bell swung two pixels left to two right (0-4), then the
                          ring of a strike - one arc moving out in three steps (5-7) - beside the bell
                          on the right and mirrored on the left
  paper    the seal       a strip of written paper - one of his wards - turning end over end in four
                          steps (0-3), then the same four burning at both ends (4-7): "more paper,
                          burning at the edges as it falls"
  sparks   the anvil      a spark off a hammer, rising on the forge's hot air: four flickers of a white-hot
                          point trailing fire down behind it (0-3), then the same four cooling to an ember
                          (4-7) - "sparks drift in on hot air from his side"
  writ     the mouths     a written leaf on a cold draught - the grove's leaf turning, a quarter-turn
                          ahead of it, with two lines of ink down it instead of a vein; paper in a cold
                          light, from the water ramp's pale end (NineMouthsEvent also blows them out
                          of the mouths in the world)
  birds    the hut        a bird lifting out of the canopy, four beats of its wings (0-3: up, half, down,
                          half), then the same four smaller - one further off (4-7): "birds lift out of
                          the canopy toward her" on each fall of the hut's feet
  stars    the seer       a star twinkling - a four-pointed star with concave sides, small, swelling to its
                          longest spikes, then turned a quarter-turn as a twinkle turns (0-3), then the same
                          four smaller and dimmer, one further off (4-7): a few come in on each phrase of
                          his reading in the Court - an astrologer's stars, never a falling one (Starfall's
                          stars fall at their slant; these hang and turn), and no trail, since they come in
                          from whichever edge he is beyond

Each wears its event's far-tell colour (SideEvent.FarTellColor) as its body, so the cue is the
colour of the light the player will find: wind's ramp for the grove's lichen grey-green, arcane
cyan for the cell, poison for the lanterns, ice for the thaw, metal for the caravan's pale steel.
They are light, not things, so they come from the emissive ramps (.ai/art-direction.md section 3,
"Elements are emissive"): a bright stop on the key-facing edge, the tell colour in the body, the
ramp's dark end on the far edge. Every glyph has a 1 px occlusion outline, eight ways, which is
what keeps it readable over the Fallow Fields' pale ground, the Sketchbook's paper and the swarm.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bonelight as bl
import raster

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "assets", "bonelight", "effects", "hint-cues.png")
PREVIEW = os.path.join(ROOT, "tools", "art", "_hint_cues_preview.png")

CELL = 16
COLS = 8
OCC = bl.OCC
SS = 4   # supersampling for the drawn shapes: a pixel is in a shape if half its subsamples are


def ramp_of(name):
    """(lit, body, shade, core) for a cue."""
    if name == "leaves":
        w = bl.ELEMENTS["wind"]            # white, #EAFCF6, #B4EEE0, #4FAE9E
        return (w[1], w[2], w[3], w[0])
    if name == "echo":
        return (bl.tone("arcane", "hi"), bl.tone("arcane", "lit"), bl.tone("arcane", "base"), bl.tone("arcane", "hi"))
    if name == "wisp":
        p = bl.ELEMENTS["poison"]
        return (p[1], p[2], p[3], p[0])
    if name == "song":
        i = bl.ELEMENTS["ice"]
        return (i[1], i[2], i[3], i[0])
    if name == "bell":
        m = bl.ELEMENTS["metal"]
        return (m[1], m[2], m[3], m[0])
    if name == "paper":
        # The seal's far tell is the arcane ramp's violet (FivePointedSealEvent.TellColor): his light.
        a = bl.ELEMENTS["arcane"]
        return (a[1], a[2], a[3], a[0])
    if name == "sparks":
        # The anvil's far tell is fire's hot stop (GreatAnvilEvent.TellColor): the forge is built, and built
        # things burn (world-and-tone.md, the carve-out).
        f = bl.ELEMENTS["fire"]
        return (f[1], f[2], f[3], f[0])
    if name == "birds":
        # The walking hut's far tell is the arcane ramp's mid (WalkingHutEvent.TellColor): his mark, the eye-light in
        # the horse skull on its gable. Birds put up by its tread, seen against his light.
        a = bl.ELEMENTS["arcane"]
        return (a[1], a[2], a[3], a[0])
    if name == "stars":
        # The seer's far tell is the arcane ramp's mid (SeersVoiceEvent.TellColor), as the seal's and the hut's:
        # he is held in the house of the one who took him, and the light he is heard through is that one's.
        a = bl.ELEMENTS["arcane"]
        return (a[1], a[2], a[3], a[0])
    if name == "writ":
        # The nine mouths' far tell is the water ramp's blue (NineMouthsEvent.TellColor): the drowned dark.
        # From the ramp's PALE end, a stop up from the others: paper in a cold light, with the ramp's dark
        # end kept for the ink (paint_writ). In the body stop they were blue gems, and the caverns' walls
        # already hold unlit crystals - a leaf must not read as one.
        w = bl.ELEMENTS["water"]
        return (w[0], w[1], w[2], w[0])
    raise ValueError(name)


# --- masks ---------------------------------------------------------------------------------------

def from_rows(rows):
    assert len(rows) == CELL, len(rows)
    mask = set()
    for y, row in enumerate(rows):
        assert len(row) == CELL, (y, row)
        for x, ch in enumerate(row):
            if ch == "#":
                mask.add((x, y))
    return mask


def sampled(inside):
    """A mask from a point test in cell coordinates (0..16), supersampled."""
    mask = set()
    for y in range(CELL):
        for x in range(CELL):
            hits = 0
            for sy in range(SS):
                for sx in range(SS):
                    if inside(x + (sx + 0.5) / SS, y + (sy + 0.5) / SS):
                        hits += 1
            if hits * 2 >= SS * SS:
                mask.add((x, y))
    return mask


def leaf(frame):
    """A leaf turning over: its long axis rotates and it foreshortens as it goes edge-on."""
    angle = frame * math.pi / 4.0
    # The width breathes with the turn so it reads as flipping, not just spinning.
    width = 0.66 + 0.30 * abs(math.cos(frame * math.pi / 4.0 * 1.5))
    ca, sa = math.cos(angle), math.sin(angle)
    cx, cy = 8.0, 8.0

    def inside(x, y):
        u = (x - cx) * ca + (y - cy) * sa
        v = -(x - cx) * sa + (y - cy) * ca
        if u < -5.6 or u > 5.6:
            # the stalk, one pixel thick, off the back end
            return -7.0 <= u < -5.6 and abs(v) < 0.55
        half = width * 3.6 * math.sqrt(max(0.0, 1.0 - (u / 5.6) ** 2)) * (1.0 + 0.15 * (u / 5.6))
        return abs(v) <= half
    mask = sampled(inside)
    # the midrib: the axis, marked by the shade tone in paint()
    rib = set()
    for t in range(-9, 10):
        u = t * 0.5
        px = int(math.floor(cx + u * ca))
        py = int(math.floor(cy + u * sa))
        if (px, py) in mask:
            rib.add((px, py))
    return mask, rib


def echo(direction):
    """One arc of a voice, bulging toward travel: direction 0 = east, clockwise in 45 degree steps."""
    a = direction * math.pi / 4.0
    dx, dy = math.cos(a), math.sin(a)
    # The arc is part of a circle centred BEHIND the cell, so its convex side faces the travel.
    ox, oy = 8.0 - dx * 4.6, 8.0 - dy * 4.6
    r_out, r_in = 8.0, 6.1

    def inside(x, y):
        px, py = x - ox, y - oy
        d = math.sqrt(px * px + py * py)
        if d < r_in or d > r_out:
            return False
        cosang = (px * dx + py * dy) / max(1e-6, d)
        return cosang >= math.cos(math.radians(50.0))
    return sampled(inside), set()


def wisp(frame):
    """A cold flame: a teardrop on its side flickering - the tip sways, the body breathes."""
    sway = [0.0, 0.8, 1.4, 0.9, 0.0, -0.8, -1.3, -0.7][frame]
    tall = [6.6, 7.2, 6.4, 7.6, 6.9, 6.2, 7.4, 6.8][frame]
    wide = [3.4, 3.2, 3.6, 3.1, 3.4, 3.6, 3.2, 3.5][frame]
    base_y = 12.5

    def inside(x, y):
        h = base_y - y          # height above the foot of the flame
        if h < -2.6:
            return False
        if h < 0:
            # the round bottom of the drop
            return (x - 8.0) ** 2 / (wide * wide) + (h / 2.6) ** 2 <= 1.0
        if h > tall:
            return False
        f = h / tall
        cx = 8.0 + sway * f * f
        half = wide * (1.0 - f) ** 0.8
        return abs(x - cx) <= half
    mask = sampled(inside)
    # The core: the bottom middle of the flame, white-hot (marked as "rib" so paint() gives it the core).
    core = set(p for p in mask if abs(p[0] + 0.5 - 8.0) <= 1.0 and 9 <= p[1] <= 12)
    return mask, core


NOTES = {
    "quaver": [
        "................",
        "........#.......",
        "........##......",
        "........###.....",
        "........#.##....",
        "........#..##...",
        "........#...#...",
        "........#...#...",
        "........#..#....",
        "........#.......",
        "........#.......",
        "....#####.......",
        "...######.......",
        "...#####........",
        "....###.........",
        "................",
    ],
    "beamed": [
        "................",
        "................",
        ".......########.",
        ".......########.",
        ".......#......#.",
        ".......#......#.",
        ".......#......#.",
        ".......#......#.",
        ".......#......#.",
        "...#####..#####.",
        "..######.######.",
        "..#####..#####..",
        "...###....###...",
        "................",
        "................",
        "................",
    ],
    "crotchet": [
        "................",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".....#####......",
        "....######......",
        "....#####.......",
        ".....###........",
        "................",
        "................",
    ],
    "minim": [
        "................",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".........#......",
        ".....#####......",
        "....##...#......",
        "....#...##......",
        ".....###........",
        "................",
        "................",
    ],
}


def note(name):
    mask = from_rows(NOTES[name])
    # The head is the part that wears the core: the bottom four rows.
    head = set(p for p in mask if p[1] >= 10 and p[0] <= 9) if name != "beamed" else set(p for p in mask if p[1] >= 9)
    return mask, head


def glint(size):
    """A four-point frost glint, 1 to 4: a cross whose arms grow, with a core in the middle."""
    arm = [1, 2, 3, 5][size]
    mask = set()
    for k in range(-arm, arm + 1):
        mask.add((8 + k, 8))
        mask.add((8, 8 + k))
    if size >= 2:
        for (x, y) in [(7, 7), (9, 7), (7, 9), (9, 9)]:
            mask.add((x, y))
    if size >= 3:
        for k in (-1, 1):
            mask.add((8 + k, 8 + k * 0))
    return mask, set([(8, 8)])


BELL = [
    "................",
    "................",
    ".......##.......",
    "......#..#......",
    "......####......",
    ".....######.....",
    ".....######.....",
    ".....######.....",
    "....########....",
    "....########....",
    "...##########...",
    "...##########...",
    "......####......",
    ".......##.......",
    "................",
    "................",
]


def bell(swing):
    """The caravan bell on its rope, swung by shearing it about the loop: the lip moves `swing`
    pixels, the rows above it proportionally less. A sheared pixel bell reads as a swing where a
    rotated one breaks up into stairs at this size."""
    upright = from_rows(BELL)
    mask = set()
    for (x, y) in upright:
        mask.add((x + int(round(swing * max(0, y - 2) / 9.0)), y))
    clapper = set((x + int(round(swing * max(0, y - 2) / 9.0)), y) for (x, y) in upright if y >= 12)
    return mask, clapper


def ring(step):
    """The ring of a strike: one short arc, moving out over three steps, drawn on the RIGHT of the bell
    and mirrored on the left by the code. Its centre of curvature is at (1.5, 8), which StageHintCue
    puts on the bell's own centre, so the arc stands clear of the lip. (Nested arcs in one cell ran
    together through their outlines.)"""
    radii = [[6.6], [8.6], [10.8]][step]

    def inside(x, y):
        px, py = x - 1.5, y - 8.0
        d = math.sqrt(px * px + py * py)
        if px <= 0 or not any(r - 0.75 <= d <= r + 0.55 for r in radii):
            return False
        return abs(math.atan2(py, px)) <= math.radians(38.0)
    return sampled(inside), set()


def paper(frame):
    """A paper ward turning end over end: a strip three pixels wide and eleven long, its long axis turned
    a quarter of a half-turn a step (0, 45, 90, 135 degrees). Charred (frames 4-7) it is shorter by a pixel
    at each end and those ends burn. The writing runs down its middle, a mark every other pixel."""
    turn = frame % 4
    burnt = frame >= 4
    angle = turn * math.pi / 4.0 + 0.35
    ca, sa = math.cos(angle), math.sin(angle)
    cx, cy = 8.0, 8.0
    half = 4.6 if burnt else 5.6
    # Foreshortened as it turns edge-on, so it reads as tumbling and not as a needle spinning.
    wide = 1.55 if turn % 2 == 0 else 1.05

    def inside(x, y):
        u = (x - cx) * ca + (y - cy) * sa
        v = -(x - cx) * sa + (y - cy) * ca
        return abs(u) <= half and abs(v) <= wide
    mask = sampled(inside)
    writing = set()
    ends = set()
    for t in range(-10, 11):
        u = t * 0.5
        px = int(math.floor(cx + u * ca))
        py = int(math.floor(cy + u * sa))
        if (px, py) in mask and abs(u) < half - 1.4 and t % 3 == 0:
            writing.add((px, py))
    if burnt:
        for (x, y) in mask:
            u = (x + 0.5 - cx) * ca + (y + 0.5 - cy) * sa
            if abs(u) > half - 1.3:
                ends.add((x, y))
    return mask, (writing, ends)


def paint_paper(mask, inner, tones):
    """The strip in the tell's ramp, its writing in the shade stop - ink, not a glow - and, burning, its
    two ends in fire's edge with one hot pixel each: the char eating in from the edges."""
    writing, ends = inner
    c = paint(mask, set(), tones)
    for p in writing:
        if p in mask:
            c.set(p[0], p[1], tones[2])
    fire = bl.ELEMENTS["fire"]
    hot = set()
    for p in sorted(ends):
        c.set(p[0], p[1], fire[3])
    # One hot pixel per burning end: the one furthest out along the strip on each side.
    if ends:
        xs = sorted(ends)
        for p in (xs[0], xs[-1]):
            c.set(p[0], p[1], fire[2])
    return c


def spark(frame):
    """A spark off the anvil, rising: a white-hot head of two or three pixels and a tail of fire trailing
    down behind it, kinked a pixel one way or the other as it flickers (frames 0-3). Cooling (4-7) the head
    shrinks to one pixel and the tail to a stub - an ember about to go out. The head is the core."""
    cooling = frame >= 4
    k = frame % 4
    kink = [0, 1, 0, -1][k]
    tail = [5, 6, 4, 6][k] if not cooling else [2, 3, 2, 1][k]
    mask = set()
    head = set()
    hx, hy = 8, 5
    if not cooling:
        for (x, y) in ((hx, hy), (hx + 1, hy), (hx, hy + 1), (hx + 1, hy + 1)):
            mask.add((x, y))
            head.add((x, y))
        if k % 2 == 0:
            mask.add((hx, hy - 1))
    else:
        mask.add((hx, hy + 1))
        head.add((hx, hy + 1))
        if k < 2:
            mask.add((hx + 1, hy + 1))
    for t in range(tail):
        y = hy + 2 + t
        x = hx + (kink if t >= tail // 2 else 0)
        mask.add((x, y))
        if t < 2 and not cooling:
            mask.add((x + 1, y))
    return mask, head


def writ(frame):
    """A written leaf on a cold draught: the grove's leaf turning over (leaf()), but where the grove's has a
    vein this one has WRITING - two lines of ink marks along it, a mark every other pixel - because the
    Sibyl wrote her prophecies on leaves and let the wind have them. Turned a quarter-turn ahead of the
    grove's at each frame, so the two rows never show the same leaf."""
    mask, _rib = leaf((frame + 2) % 8)
    angle = ((frame + 2) % 8) * math.pi / 4.0
    ca, sa = math.cos(angle), math.sin(angle)
    writing = set()
    for line in (-1.1, 1.1):
        for t in range(-8, 9):
            if t % 2:
                continue
            u = t * 0.5
            px = int(math.floor(8.0 + u * ca - line * sa))
            py = int(math.floor(8.0 + u * sa + line * ca))
            if (px, py) in mask and all((px + dx, py + dy) in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                writing.add((px, py))
    return mask, writing


def paint_writ(mask, writing, tones):
    """The leaf in the tell's ramp, its writing in the ramp's dark end: ink, not a glow."""
    c = paint(mask, set(), tones)
    ink = bl.ELEMENTS["water"][3]
    for p in writing:
        c.set(p[0], p[1], ink)
    return c


def bird(frame):
    """A bird beating up out of the canopy, seen from below: a small body, a head forward, and two wings that
    are a thick stroke from the shoulder to a bent wrist and a thin one on to the tip - up, half, down, half
    (frames 0-3), the beat that reads as a bird and not a leaf. Frames 4-7 are the same beat at three fifths
    the size: a bird further off."""
    k = frame % 4
    s = 1.0 if frame < 4 else 0.6
    lift = [-4.2, -1.2, 2.6, -0.4][k]          # where the wingtip is, above or below the shoulder
    wrist = [-2.6, -0.8, 1.4, -0.2][k]
    cx, cy = 8.0, 8.6

    def seg(px, py, ax, ay, bx, by):
        dx, dy = bx - ax, by - ay
        L = dx * dx + dy * dy
        t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / L)) if L > 0 else 0.0
        qx, qy = ax + dx * t, ay + dy * t
        return math.hypot(px - qx, py - qy)

    def inside(x, y):
        u, v = (x - cx) / s, (y - cy) / s
        if (u / 1.6) ** 2 + (v / 1.1) ** 2 <= 1.0:          # the body
            return True
        if (u - 0.0) ** 2 + (v + 1.6) ** 2 <= 0.8:          # the head, forward (up the screen)
            return True
        if abs(u) <= 0.5 and 0.8 <= v <= 2.6:               # the tail
            return True
        for side in (-1.0, 1.0):
            if seg(u, v, side * 1.0, -0.2, side * 3.6, wrist) <= 0.85:
                return True
            if seg(u, v, side * 3.6, wrist, side * 6.6, lift) <= 0.55:
                return True
        return False
    return sampled(inside), set()


def star(frame):
    """A star twinkling, the seer's: four points with concave sides (an astroid, |u|^2/3 + |v|^2/3 <= 1) on
    one pixel's centre, so every arm ends in a single pixel. The cycle: small, larger, its longest spikes,
    then a flash - shorter again, with a glint thrown out on each diagonal, eight points for a moment
    (frames 0-3): a twinkle, which the song's frost glint (a cross that only grows) never does. Frames 4-7
    are the same four at three fifths, one further off. The core is the middle pixel and, once it has grown,
    the four beside it."""
    k = frame % 4
    s = 1.0 if frame < 4 else 0.6
    r = [3.6, 5.0, 6.3, 4.2][k] * s
    cx = cy = 8.5

    def inside(x, y):
        u, v = abs(x - cx), abs(y - cy)
        return (u / r) ** (2.0 / 3.0) + (v / r) ** (2.0 / 3.0) <= 1.0
    if k == 3:
        # The flash: a thin cross, and a glint standing off each diagonal with a gap between - eight points
        # that only read as eight because the diagonals are not joined to the middle.
        arm = 4 if s == 1.0 else 2
        mask = set()
        for t in range(-arm, arm + 1):
            mask.add((8 + t, 8))
            mask.add((8, 8 + t))
        if s == 1.0:
            for (ox, oy) in ((2, 2), (-2, 2), (2, -2), (-2, -2)):
                mask.add((8 + ox, 8 + oy))
        return mask, set([(8, 8)])
    mask = sampled(inside)
    mask.add((8, 8))
    core = set([(8, 8)])
    if r >= 4.5:
        core.update([(7, 8), (9, 8), (8, 7), (8, 9)])
    return mask, core


ROWS = [
    ("leaves", [leaf(f) for f in range(8)]),
    ("echo", [echo(d) for d in range(8)]),
    ("wisp", [wisp(f) for f in range(8)]),
    ("song", [note("quaver"), note("beamed"), note("crotchet"), note("minim"),
              glint(0), glint(1), glint(2), glint(3)]),
    ("bell", [bell(-2), bell(-1), bell(0), bell(1), bell(2), ring(0), ring(1), ring(2)]),
    ("paper", [paper(f) for f in range(8)]),
    ("sparks", [spark(f) for f in range(8)]),
    ("writ", [writ(f) for f in range(8)]),
    ("birds", [bird(f) for f in range(8)]),
    ("stars", [star(f) for f in range(8)]),
]


def paint(mask, inner, tones):
    """Key-facing edge lit, far edge shaded, body between; the inner marks (a rib, a core, a
    note's head, a clapper) in the core or the shade; and the eight-way occlusion outline."""
    lit, body, shade, core = tones
    c = raster.Canvas(CELL, CELL)
    for (x, y) in mask:
        if (x, y - 1) not in mask or (x - 1, y) not in mask:
            c.set(x, y, lit)
        elif (x, y + 1) not in mask or (x + 1, y) not in mask:
            c.set(x, y, shade)
        else:
            c.set(x, y, body)
    for p in inner:
        if p in mask:
            c.set(p[0], p[1], core)
    for y in range(CELL):
        for x in range(CELL):
            if (x, y) in mask:
                continue
            if any((x + dx, y + dy) in mask for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                c.set(x, y, OCC)
    return c


def paint_rib(mask, rib, tones):
    """A leaf: as paint(), but the midrib in the shade tone - a vein, not a glow."""
    lit, body, shade, core = tones
    c = paint(mask, set(), tones)
    for p in rib:
        if p in mask and (p[0], p[1] - 1) in mask and (p[0] - 1, p[1]) in mask:
            c.set(p[0], p[1], shade)
    return c


def paint_bell(mask, clapper, tones):
    """The clapper hangs under the lip in the ramp's dark end, a separate thing from the bell."""
    c = paint(mask, set(), tones)
    for p in clapper:
        c.set(p[0], p[1], tones[2])
    return c


def main():
    from PIL import Image

    d = os.path.dirname(OUT)
    if not os.path.isdir(d):
        os.makedirs(d)

    sheet = Image.new("RGBA", (CELL * COLS, CELL * len(ROWS)), (0, 0, 0, 0))
    ok = True
    for r, (name, cells) in enumerate(ROWS):
        tones = ramp_of(name)
        assert len(cells) == COLS, name
        for col, (mask, inner) in enumerate(cells):
            for (x, y) in mask:
                if x < 1 or y < 1 or x > CELL - 2 or y > CELL - 2:
                    ok = False
                    print("GLYPH %s %d touches the cell border at (%d, %d)" % (name, col, x, y))
            if name == "leaves":
                c = paint_rib(mask, inner, tones)
            elif name == "bell" and col < 5:
                c = paint_bell(mask, inner, tones)
            elif name == "paper":
                c = paint_paper(mask, inner, tones)
            elif name == "writ":
                c = paint_writ(mask, inner, tones)
            else:
                c = paint(mask, inner, tones)
            bad = c.audit()
            if bad:
                ok = False
                print("OFF-CONTRACT COLOURS in %s %d:" % (name, col))
                for h, cnt in bad[:5]:
                    print("   %s  x%d" % (h, cnt))
            sheet.alpha_composite(c.img, (col * CELL, r * CELL))
        print("  %-7s %s px" % (name, " ".join("%3d" % len(m) for m, _ in cells)))
    sheet.save(OUT)

    # The preview on the grounds that matter: the reference stone, the Fallow Fields' pale field and
    # the Sketchbook's paper, where the outline is what keeps a pale glyph from dissolving.
    zoom = 6
    floors = [bl.tone("stone", "base"), bl.tone("steel", "lit"), "#E8E2CF"]
    pw, ph = CELL * COLS * zoom, CELL * len(ROWS) * zoom
    preview = Image.new("RGBA", (pw * len(floors) + 12 * (len(floors) + 1), ph + 24), (5, 7, 12, 255))
    big = sheet.resize((pw, ph), Image.NEAREST)
    for f, floor in enumerate(floors):
        x = 12 + f * (pw + 12)
        preview.paste(raster.rgb(floor), (x, 12, x + pw, 12 + ph))
        preview.alpha_composite(big, (x, 12))
    preview.convert("RGB").save(PREVIEW)

    if not ok:
        return 1
    print("")
    print("%d cues x %d cells, palette clean -> %s" % (len(ROWS), COLS, os.path.relpath(OUT, ROOT)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
