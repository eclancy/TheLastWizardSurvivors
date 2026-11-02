# -*- coding: utf-8 -*-
"""The Throne's hall (.ai/endgame-castle.md 6.1). Run: python tools/art/throne_room.py

    assets/bonelight/layouts/throne_room.json   the hall, cell by cell, and what stands in it
    assets/bonelight/world/throne/throne.png    the throne (x2), with throne.json beside it
    tools/art/_throne_room_map.png              a map of it, for looking at (not committed)

ONE HALL, CLOSED BEHIND HIM. Not a map: a bounded room of 40 x 40 tiles (1,920 px square) inside walls
two tiles thick, read by the same reader as the Court (LevelGenerator.Authored.cs). Square because the
two canvases are mirror images: a phone sees most of its height and half its width, a PC the reverse,
so neither is given a long way to look down that the other is not.

    the nave     the player comes in by the doors in the SOUTH wall, which close behind him, and
                 stands at the foot of a red runner that runs the length of the hall to the dais;
    the pillars  two rows of four, solid (wall cells, so the flow field, the collision on layer 4 and
                 the placement checks all know them with no code of their own), either side of the
                 runner - something to move round, never a maze;
    the dais     laid marble at the NORTH end, the throne on it, and behind the throne the great doors
                 that open when he falls: the way home (ThroneRules, hook 5).

The floor is the castle's flags (the palette's `stonetile`), the runner the Court's red runner and the
dais the Court's marble - his house is one house, and every one of those already has drawn edges
(terrain_court.py). The walls are throne_walls.py's `throne_wall`, which is where his light burns.

THE GRID'S RULES are the Court's (court_layout.py), and this file checks them and refuses to write a
hall that fails: walls two thick everywhere (the pillars are 2 x 2), a laid floor or a carpet never
touching a wall, every floor cell reachable from the start, the throne's front and the north doors'
foot reachable, nothing walkable on the grid's edge.

THE THRONE is the one sprite drawn here: empty, high-backed, unlit stone with a red cushion and a
tarnished crown inlaid in its back. It does not glow - the only light in the room is the sconces' -
and it is solid (ThroneRules gives it a body on the terrain layer and closes its footprint to
placement). The great doors behind it are the Court's (props_court.py's throne-doors, at the walls'
own 1:1): the same doors seen from the other side of the house.
"""
from __future__ import division, print_function

import io
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bonelight as bl
import raster

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "assets", "bonelight", "layouts")
OUT = os.path.join(OUT_DIR, "throne_room.json")
SPRITE_DIR = os.path.join(ROOT, "assets", "bonelight", "world", "throne")
PREVIEW = os.path.join(ROOT, "tools", "art", "_throne_room_map.png")

OCC = bl.OCC

WALL = "#"
FLOOR = "."
LEGEND = {
    WALL: "wall",
    FLOOR: "",
    "m": "court_marble",
    "R": "runner_red",
}

# ------------------------------------------------------------------------------------------------
# THE PLAN. Rectangles are (x, y, w, h) in cells, x east and y south, (0, 0) the north-west corner.

THICK = 2                          # the walls
W = H = 40 + 2 * THICK             # the hall's 40 x 40 floor inside them
HALL = (THICK, THICK, 40, 40)      # x 2..41, y 2..41; its middle line is x = 22

# Two rows of four, 2 x 2 each, nine cells either side of the middle line: a nave sixteen cells wide
# (768 px) between them and an aisle ten wide outside each. Seven cells between pillars down a row.
PILLAR_X = (12, 30)
PILLAR_Y = (11, 18, 25, 32)
PILLARS = [(x, y, 2, 2) for x in PILLAR_X for y in PILLAR_Y]

# The dais: laid marble across the north end of the nave, a row of flags between it and the wall.
DAIS = (15, 4, 14, 5)              # x 15..28, y 4..8
# The runner: two cells wide down the middle line, from the dais's foot to two rows short of the south
# wall (a carpet never touches a wall).
RUNNER = (21, 9, 2, 31)            # x 21..22, y 9..39

# The throne: its base point (where its feet stand, in cells, fractional) and the footprint that is
# solid and closed to placement, both on the dais's middle line.
THRONE_AT = (22.0, 8.0)
THRONE_FOOTPRINT = (21, 6, 2, 2)   # x 21..22, y 6..7

# The great doors behind the throne (the way home), on the north wall's face: eight cells wide, the
# Court's throne-doors sprite standing on the wall's foot line. And the doors he came in by, in the
# south wall, eight wide: shut behind him, drawn as wall.
NORTH_DOORS = (18, 0, 8, THICK)
SOUTH_DOORS = (18, H - THICK, 8, THICK)

# Where the run starts (the origin cell; the player stands on its north-west corner, which is the
# middle line): at the foot of the runner, four rows in from the south doors.
START = (22, 38)


def rect_cells(r):
    x, y, w, h = r
    for yy in range(y, y + h):
        for xx in range(x, x + w):
            yield xx, yy


class Grid(object):
    def __init__(self):
        self.c = [[WALL] * W for _ in range(H)]

    def get(self, x, y):
        if 0 <= x < W and 0 <= y < H:
            return self.c[y][x]
        return WALL

    def set(self, x, y, ch):
        if 0 <= x < W and 0 <= y < H:
            self.c[y][x] = ch

    def carve(self, r, ch=FLOOR):
        for x, y in rect_cells(r):
            self.set(x, y, ch)

    def walkable(self, x, y):
        return self.get(x, y) != WALL


def build_grid():
    g = Grid()
    g.carve(HALL)
    for p in PILLARS:
        g.carve(p, WALL)
    g.carve(DAIS, "m")
    g.carve(RUNNER, "R")
    return g


def flood(g, start):
    seen = {start}
    todo = [start]
    while todo:
        x, y = todo.pop()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if n in seen or not g.walkable(*n):
                continue
            seen.add(n)
            todo.append(n)
    return seen


def check(g):
    failures = []
    reach = flood(g, START)
    if not g.walkable(*START):
        failures.append("the start cell %r is not floor" % (START,))

    thin = 0
    for y in range(H):
        for x in range(W):
            if g.get(x, y) != WALL:
                continue
            if (g.walkable(x - 1, y) and g.walkable(x + 1, y)) or (g.walkable(x, y - 1) and g.walkable(x, y + 1)):
                thin += 1
    if thin:
        failures.append("%d wall cells are one tile thick" % thin)

    touching = 0
    for y in range(H):
        for x in range(W):
            if g.get(x, y) in (WALL, FLOOR):
                continue
            if any(g.get(x + dx, y + dy) == WALL for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                touching += 1
    if touching:
        failures.append("%d cells of marble or runner touch a wall" % touching)

    pockets = sum(1 for y in range(H) for x in range(W) if g.walkable(x, y) and (x, y) not in reach)
    if pockets:
        failures.append("%d walkable cells are cut off from the start" % pockets)

    for x in range(W):
        if g.walkable(x, 0) or g.walkable(x, H - 1):
            failures.append("floor on the hall's north or south edge")
            break
    for y in range(H):
        if g.walkable(0, y) or g.walkable(W - 1, y):
            failures.append("floor on the hall's east or west edge")
            break

    floor = sum(1 for y in range(H) for x in range(W) if g.walkable(x, y)) + 4 * len(PILLARS)
    if floor != 40 * 40:
        failures.append("the hall is %d cells, not 40 x 40" % floor)
    if len(PILLARS) != 8:
        failures.append("%d pillars, not two rows of four" % len(PILLARS))

    # The throne's front, the doors' foot (the way home opens there) and a way round the throne's back.
    tx, ty, tw, th = THRONE_FOOTPRINT
    for need, cell in (("the throne's front", (tx, ty + th)),
                       ("the north doors' foot", (NORTH_DOORS[0] + NORTH_DOORS[2] // 2, NORTH_DOORS[1] + NORTH_DOORS[3])),
                       ("the throne's back", (tx, ty - 1))):
        if cell not in reach:
            failures.append("%s %r cannot be reached from the start" % (need, cell))
    for x, y in rect_cells(THRONE_FOOTPRINT):
        if g.get(x, y) != "m":
            failures.append("the throne's footprint (%d, %d) is not on the dais" % (x, y))
            break
    return failures


# ------------------------------------------------------------------------------------------------
# The throne (1:1, saved x2).

def ramp(mat):
    return [bl.tone(mat, r) for r in ("hi", "lit", "base", "shade", "deep")]


ST = ramp("stone")
STL = ramp("steel")
GD = ramp("gold")
RD = ramp("red")


def box(c, x0, y0, x1, y1, fill, lit=None, dark=None):
    c.rect(x0, y0, x1, y1, fill)
    if lit:
        c.hline(x0, x1, y0, lit)
        c.vline(x0, y0, y1, lit)
    if dark:
        c.hline(x0, x1, y1, dark)
        c.vline(x1, y0, y1, dark)


def throne():
    """Empty, high-backed, unlit. 48 x 69 at 1:1 (96 x 138 on screen): a back of dark stone rising to a
    pointed top with two finials, a crown of tarnished gold inlaid in it, arms capped in steel, a red
    cushion on the seat, all on a plinth step. Key light upper-left; contact shadow on the last rows."""
    w, h = 48, 69
    c = raster.Canvas(w, h, None)
    cx = w // 2
    # The back: a slab from y 8 to the seat, its top a pointed arch.
    bx0, bx1 = 11, 36
    for y in range(2, 46):
        for x in range(bx0, bx1 + 1):
            dx = abs(x - (bx0 + bx1) / 2.0) / ((bx1 - bx0) / 2.0)
            top = 2 + int((dx ** 0.7) * 9)
            if y < top:
                continue
            tone = ST[2]
            if x <= bx0 + 1:
                tone = ST[1]
            elif x >= bx1 - 1:
                tone = ST[4]
            elif y == top:
                tone = ST[1] if x < cx else ST[2]
            c.set(x, y, tone)
    # The carved panel inset down the back, and the groove at its middle.
    for y in range(14, 40):
        for x in range(15, 33):
            c.set(x, y, ST[3])
        c.set(15, y, ST[4])
        c.set(32, y, ST[2])
    c.hline(15, 32, 14, ST[4])
    c.hline(15, 32, 39, ST[2])
    # The crown, inlaid in tarnished gold (pigment, never light): three points on a band.
    crown = ["X.....X.....X",
             "X....XXX....X",
             "XX..XXXXX..XX",
             "XXXXXXXXXXXXX",
             "XXXXXXXXXXXXX",
             ".XXXXXXXXXXX."]
    for j, row in enumerate(crown):
        for i, ch in enumerate(row):
            if ch != "X":
                continue
            # Lit on its upper-left, the band at its foot a stop down, the right-hand edge in shade.
            tone = GD[2] if (j < 3 and i < 6) else GD[3]
            if j >= 4:
                tone = GD[4] if i > 0 else GD[3]
            if i == len(row) - 1:
                tone = GD[4]
            c.set(cx - 6 + i, 22 + j, tone)
    for i in (2, 6, 10):
        c.set(cx - 6 + i, 26, ST[4])
    # Finials either side of the back's top.
    for fx in (bx0 - 2, bx1 - 1):
        box(c, fx, 9, fx + 3, 16, ST[2], ST[1], ST[4])
        c.rect(fx + 1, 6, fx + 2, 8, ST[2])
        c.set(fx + 1, 5, ST[1])
    # The arms: blocks either side of the seat, capped in steel.
    for ax0 in (5, 37):
        box(c, ax0, 34, ax0 + 6, 52, ST[3], ST[2], ST[4])
        c.hline(ax0, ax0 + 6, 33, STL[2])
        c.hline(ax0, ax0 + 6, 34, STL[3])
        c.set(ax0, 33, STL[1])
    # The seat: its top face (lit), then its front face.
    box(c, 9, 42, 38, 46, ST[2], ST[1], ST[3])
    box(c, 9, 47, 38, 55, ST[3], ST[2], ST[4])
    # The cushion on the seat, red cloth lit on its left and top edge.
    for y in range(40, 46):
        for x in range(12, 36):
            tone = RD[3]
            if y == 40 or x == 12:
                tone = RD[2]
            elif y == 45 or x == 35:
                tone = RD[4]
            c.set(x, y, tone)
    c.hline(13, 34, 46, OCC)
    # The plinth step under it all.
    box(c, 2, 56, 45, 66, ST[3], ST[2], ST[4])
    c.hline(2, 45, 56, ST[1])
    c.hline(3, 44, 61, ST[4])
    c.hline(3, 44, 62, ST[3])
    # Shadows the arms and seat throw down the step, and the contact line.
    for x in range(10, 40):
        c.set(x, 57, ST[4])
    for y in range(67, 69):
        c.hline(3, 44, y, OCC)
    return c


SPRITES = [
    ("throne", throne, 2, "ground"),
]


def write_sprites():
    if not os.path.isdir(SPRITE_DIR):
        os.makedirs(SPRITE_DIR)
    manifest = {}
    ok = True
    for name, fn, scale, anchor in SPRITES:
        c = fn()
        bad = c.audit()
        if bad:
            ok = False
            print("OFF-CONTRACT COLOURS in %s: %s" % (name, bad[:4]))
            continue
        img = c.scaled(scale) if scale != 1 else c.img
        img.save(os.path.join(SPRITE_DIR, name + ".png"))
        manifest[name] = {"width": img.size[0], "height": img.size[1], "scale": scale, "anchor": anchor}
    manifest["_comment"] = ("Generated by tools/art/throne_room.py - what stands in the Throne's hall. width/height on "
                            "screen; anchor: ground = bottom middle stands on the point.")
    text = json.dumps(manifest, indent=1, sort_keys=True)
    text = text if isinstance(text, type(u"")) else text.decode("utf-8")
    io.open(os.path.join(SPRITE_DIR, "throne.json"), "w", encoding="utf-8", newline="\n").write(text + u"\n")
    return ok


# ------------------------------------------------------------------------------------------------

def write_json(g):
    rows = ["".join(g.c[y]) for y in range(H)]
    doc = {
        "_comment": "Generated by tools/art/throne_room.py - the Throne, one closed hall (.ai/endgame-castle.md 6.1). "
                    "Read by LevelGenerator.Authored.cs (the grid) and ThroneRules.cs (the rest). Cells are (x east, y south) "
                    "from the north-west corner; `origin` is the cell that is world tile (0, 0), where the run starts.",
        "format": 1,
        "name": "throne",
        "width": W,
        "height": H,
        "origin": [START[0], START[1]],
        "outside": WALL,
        "legend": LEGEND,
        "rows": rows,
        "rooms": [{"id": "hall", "name": "The Throne", "kind": "hall", "rect": list(HALL), "wing": "",
                   "hidden": False, "counted": False, "glyph": ""}],
        "furniture": [
            {"kind": "throne", "room": "hall", "at": list(THRONE_AT)},
            {"kind": "throne_doors", "room": "hall", "wall": True,
             "at": [NORTH_DOORS[0] + NORTH_DOORS[2] / 2.0, float(NORTH_DOORS[1] + NORTH_DOORS[3])]},
        ],
        "pillars": [list(p) for p in PILLARS],
        "dais": list(DAIS),
        "runner": list(RUNNER),
        "throneFootprint": list(THRONE_FOOTPRINT),
        "northDoors": list(NORTH_DOORS),
        "southDoors": list(SOUTH_DOORS),
    }
    text = json.dumps(doc, indent=1, sort_keys=True)
    text = text if isinstance(text, type(u"")) else text.decode("utf-8")
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(text + u"\n")


PREVIEW_COLOURS = {
    WALL: (12, 15, 24),
    FLOOR: (70, 78, 96),
    "m": (122, 126, 140),
    "R": (150, 46, 56),
}


def write_preview(g):
    from PIL import Image, ImageDraw
    s = 12
    img = Image.new("RGB", (W * s, H * s), PREVIEW_COLOURS[WALL])
    d = ImageDraw.Draw(img)
    for y in range(H):
        for x in range(W):
            col = PREVIEW_COLOURS.get(g.get(x, y), (255, 0, 255))
            if col != PREVIEW_COLOURS[WALL]:
                d.rectangle([x * s, y * s, x * s + s - 1, y * s + s - 1], fill=col)
    tx, ty, tw, th = THRONE_FOOTPRINT
    d.rectangle([tx * s, ty * s, (tx + tw) * s - 1, (ty + th) * s - 1], fill=(40, 40, 50), outline=(230, 200, 120))
    for r, col in ((NORTH_DOORS, (200, 160, 80)), (SOUTH_DOORS, (110, 90, 60))):
        x, y, w, h = r
        d.rectangle([x * s, y * s, (x + w) * s - 1, (y + h) * s - 1], outline=col)
    sx, sy = START
    d.ellipse([sx * s - 6, sy * s - 6, sx * s + 6, sy * s + 6], outline=(255, 255, 255))
    img.save(PREVIEW)


def main():
    g = build_grid()
    failures = check(g)
    walk = sum(1 for y in range(H) for x in range(W) if g.walkable(x, y))
    print("  throne: %dx%d cells, %d walkable, %d pillars, dais %dx%d, runner %d long"
          % (W, H, walk, len(PILLARS), DAIS[2], DAIS[3], RUNNER[3]))
    write_preview(g)
    print("  map: tools/art/_throne_room_map.png")
    if failures:
        print("THE THRONE FAILS ITS OWN CHECK; throne_room.json NOT written:")
        for f in failures:
            print("   - " + f)
        return 1
    print("  checked: walls two thick; no carpet or marble against a wall; every floor cell reachable; "
          "the throne's front and back and the north doors' foot reachable; 40 x 40")
    write_json(g)
    print("wrote assets/bonelight/layouts/throne_room.json")
    if not write_sprites():
        return 1
    print("wrote assets/bonelight/world/throne/ (throne.png, throne.json)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
