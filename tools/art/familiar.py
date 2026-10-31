# -*- coding: utf-8 -*-
"""The Familiar: the wizard's hound, and what it becomes. Run: python tools/art/familiar.py

WHAT IT IS (.ai/spell-roster.md, "Familiar"). A hound the wizard conjures out of his own light, that
runs at his heel and bites what comes close. It is the one figure on screen besides the wizard that
is on his side, so every choice below is about reading as HIS, at phone size, in a crowd that already
holds four kinds of dog: the Dire Wolf, the Bog Hound, the Sled Hound and the Chainhound, every one of
them a dark grey-green body with two points of the cold arcane mark for eyes (foes_forest.py says why
a wolf with warm eyes would read as wildlife). So the familiar is the inverse, point for point:

  - WARM FUR, not deep linen. The `flesh` row - "anything alive" (art-direction.md section 3) - lit
    from the bright end, because the player's cast is the one thing allowed to be bright.
  - THE WIZARD'S HAT. A small pointed hat in his own indigo wool with a gold band and a gold star,
    swept back like his. It is the silhouette-breaking attachment section 4 asks every class for:
    filled solid black, a dog in a pointed hat is still the wizard's dog, and no enemy wears one.
  - WARM EYES. Amber, the player's light - never the violet every taken thing stares with.
  - A GOLD STAR on the collar. The one hot pixel on the body.
  - THE HEAD UP. "A level neck is a dog" (foes_forest.wolf); the wolves stalk head-low, this one
    carries its head high and its tail up.

It is not a mascot (world-and-tone.md, tone rule 1): a lean sighthound, long in the leg and the
muzzle, deep in the chest - built to run things down - with the hat worn the way the wizard wears his.

THE FORMS. Every one is posed from one skeleton per body plan and drawn by the same renderer:

  hound          48 cell   the base familiar
  greathound     64 cell   level 4, Great Hound: heavier, a mantle over the shoulders, bigger jaws
  rimehound      48 cell   level 4, Rimehound (the Ice path): frost-blue fur and a ruff of ice
  cerberus       96 cell   level 8, Cerberus: three heads on one great body, the hat on the top one -
                           and the hellhound's hide: near-black, lit blood red, six ember eyes, embers
                           in the coat and on the breath (HELL). The one form on the boss cell, with
                           hackles and two-pixel features drawn for it (2026-10-30, "bigger")
  cerberus-rime  96 cell   the same in frost, after the Ice path: a stop darker than the Rimehound,
                           eyes lit ice (RIME_DEEP)
  hellhound,     48 cell   the rest of the pack under an Ascension, without Littermates: the hound's
  annwn-hound              size, one head, the Ascension's coat (and -rime after the Ice path)
  pup, hell-pup, 32 cell   Littermates' two pups: the fawn hound's, and under each Ascension a one-headed
  annwn-pup                pup in its coat (pup(), a body plan of its own; THE PACK below)
  griffin        64 cell   level 8, Griffin: eagle head, the wizard's indigo wings, a lion's quarters;
                           it flies, so its shadow sits detached below it (section 4's Flyer row)
  griffin-rime   64 cell   the same, after the Ice path

Each has four strips: idle (4), run (6), pounce (6 - the leap and the bite, or the griffin's swoop)
and howl (6 - the flourish on summoning and on a level). The game sets pounce and howl frame by frame
from its own clock (scripts/Familiar.cs), so a leap's frames always match where the leap is.

THE LEGS are a dog's, not a person's. A foreleg is shoulder, elbow (pointing BACK, at the brisket),
a straight forearm and a wrist that folds the paw back only while the leg swings through the air; in
the gallop it reaches out straight in front of the chest (front_leg). A hind leg has its hock - the
joint that points back - between a long thigh and the paw (hind_leg). The first build bent the
foreleg's one joint forward at mid-height, which read as a leg on backwards.

THE RENDERER. A frame is built from parts - ellipsoid blobs, tapered capsules, shaded polygons -
each lit by the contract's one light (key from the upper left), quantised to its material's five
tones with a small ordered dither so a turning form is a few stops rather than bands. Three passes
then do what section 2 and 4b ask: a part in front casts a one-pixel deep shadow onto the part
behind it (every overlapping form separated - the biggest single source of internal edges), the
key-facing silhouette edge gets the bone-white rim, and the occ contact line sits on row cell-1.
Fur gets a lay of darker flecks keyed to the part's own coordinates, so it moves with the body
instead of boiling. Every pixel is checked against the palette (raster.Canvas.audit).
"""
from __future__ import print_function, division

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import bonelight as B
import matte
import raster
import spriteframes

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT_DIR = os.path.join(ROOT, "assets", "bonelight", "characters")
OUT_REL = "assets/bonelight/characters"
FRAMES_DIR = os.path.join(ROOT, "scenes", "resources")
PREVIEW_DIR = os.path.join(HERE, "_familiar")

M = B.MATERIALS
E = B.ELEMENTS
RIM = B.RIM
OCC = B.OCC
BAYER = raster.BAYER8

# The key light: upper left, tipped toward the viewer so a form's middle is lit, not its edge.
_L = (-1.0, -1.0, 1.25)
_LN = math.sqrt(sum(c * c for c in _L))
LIGHT = (_L[0] / _LN, _L[1] / _LN, _L[2] / _LN)

# --- materials (five tones each, hi..deep) -----------------------------------------------------
FAWN = M["flesh"]
WOOL = M["wool"]
GOLD = M["gold"]
FROST = M["azure"]
EMBER = M["ember"]
BONE = M["skin"]
STONE = M["stone"]
AMBER = M["amber"]

ARC = E["arcane"]
ICE = E["ice"]
LIGHT_E = E["light"]

# CERBERUS'S HIDE (Eric, 2026-10-28: "cerberus should give the dogs a new redder / blacker skin"). At
# level 8 the hound stops being a warm-coated sighthound and becomes the hellhound: the bulk of the
# body in the red row's near-black shade, its shadows in the red row's deep and then violet's (the
# shade turning toward indigo, as section 2 asks, rather than going grey), and only the key-facing
# planes lit - blood red (the ember row's shade) and crimson (its base) at the edge - so it reads
# black at a glance and red where the light falls. Every stop is the contract's; the two from `ember`
# are the player's pigment, and this is the player's dog. It is still his: the hat, the mantle, the
# gold collar and its star are unchanged on it.
HELL = [EMBER[2], EMBER[3], M["red"][3], M["red"][4], M["violet"][4]]
# The share of its dark coat pixels that are embers (Fig.render): a scatter, not a pattern.
EMBER_FLECKS = 0.025


def hsh(x, y, salt=0):
    """A stable hash of a pixel in a part's own coordinates: fur that moves with the body."""
    h = (x * 374761393 + y * 668265263 + salt * 1442695041) & 0xffffffff
    h = ((h ^ (h >> 13)) * 1274126177) & 0xffffffff
    return ((h ^ (h >> 16)) & 0xffff) / 65535.0


def rot(px, py, a):
    c, s = math.cos(a), math.sin(a)
    return (px * c - py * s, px * s + py * c)


def ik(root, target, a, b, bend):
    """Two-bone IK: the joint between root and target for bones a and b, bent to `bend` (+1/-1)."""
    dx, dy = target[0] - root[0], target[1] - root[1]
    d = math.sqrt(dx * dx + dy * dy)
    d = max(1e-3, min(d, a + b - 1e-3))
    # angle at the root between the root-target line and the first bone
    cosang = (a * a + d * d - b * b) / (2 * a * d)
    ang = math.acos(max(-1.0, min(1.0, cosang)))
    base = math.atan2(dy, dx)
    j = base + bend * ang
    return (root[0] + math.cos(j) * a, root[1] + math.sin(j) * a)


class Fig(object):
    """One frame under construction: parts painted back to front, then lit, separated and rimmed."""

    def __init__(self, cell):
        self.cell = cell
        self.cells = {}
        self.part = 0
        self.shadow = set()
        # A material that should sit deeper than the light alone puts it, by its row (id of the list).
        self.deeper = {}
        # The coat that smoulders (Cerberus's): a few of its dark pixels are embers. None: no embers.
        self.embers = None
        # A coat with a pattern in it (the cat's): (ramp, "stripes" | "spots"). The marks are keyed to each
        # part's own coordinates, as the lay of the fur is, so they ride with the body. None: plain.
        self.pattern = None
        # The ordered dither's strength (tone_of). The sprites keep the renderer's; a card icon (ICON_DITHER)
        # takes far less, because at 16 to 32 pixels a dithered plane is speckle, not a turning form.
        self.dither = 0.18
        # The lay of the coat (render): broken darker strokes through the lit stops. Off on a card icon, where
        # at its size they were the speckle that made a fawn head read as a brown smudge.
        self.lay = True

    # --- painting ------------------------------------------------------------------------------
    def _put(self, x, y, ramp, tone, lx, ly, kind, far=False):
        if 0 <= x < self.cell and 0 <= y < self.cell:
            self.cells[(x, y)] = (ramp, tone, self.part, lx, ly, "far" if far and kind != "glow" else kind)

    @staticmethod
    def tone_of(inten, bias, x, y, dither=0.18):
        v = inten - bias * 0.32 + (BAYER[y % 8][x % 8] / 64.0 - 0.5) * dither
        if v > 0.90:
            return 0
        if v > 0.68:
            return 1
        if v > 0.34:
            return 2
        if v > -0.06:
            return 3
        return 4

    def blob(self, cx, cy, rx, ry, ramp, bias=0.0, kind="body", angle=0.0):
        """An ellipsoid seen side on: lit by its own normal."""
        self.part += 1
        r = max(rx, ry) + 1
        for y in range(int(math.floor(cy - r)), int(math.ceil(cy + r)) + 1):
            for x in range(int(math.floor(cx - r)), int(math.ceil(cx + r)) + 1):
                dx, dy = rot(x + 0.5 - cx, y + 0.5 - cy, -angle)
                nx, ny = dx / rx, dy / ry
                d2 = nx * nx + ny * ny
                if d2 > 1.0:
                    continue
                nz = math.sqrt(max(0.0, 1.0 - d2))
                wx, wy = rot(nx, ny, angle)
                inten = wx * LIGHT[0] + wy * LIGHT[1] + nz * LIGHT[2]
                self._put(x, y, ramp, self.tone_of(inten, bias + self.deeper.get(id(ramp), 0.0), x, y, self.dither), int(round(dx)), int(round(dy)), kind, bias >= 0.6)

    def limb(self, pts, radii, ramp, bias=0.0, kind="body"):
        """A tapered capsule through `pts`: legs, necks, tails. Lit across its own width."""
        self.part += 1
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        rmax = max(radii) + 1
        ax0, ay0 = pts[0]
        for y in range(int(math.floor(min(ys) - rmax)), int(math.ceil(max(ys) + rmax)) + 1):
            for x in range(int(math.floor(min(xs) - rmax)), int(math.ceil(max(xs) + rmax)) + 1):
                px, py = x + 0.5, y + 0.5
                best = None
                for i in range(len(pts) - 1):
                    (x0, y0), (x1, y1) = pts[i], pts[i + 1]
                    vx, vy = x1 - x0, y1 - y0
                    ll = vx * vx + vy * vy
                    t = 0.0 if ll < 1e-6 else max(0.0, min(1.0, ((px - x0) * vx + (py - y0) * vy) / ll))
                    cxp, cyp = x0 + vx * t, y0 + vy * t
                    rr = radii[i] + (radii[i + 1] - radii[i]) * t
                    ddx, ddy = px - cxp, py - cyp
                    dist = math.sqrt(ddx * ddx + ddy * ddy)
                    if dist <= rr + 0.15 and (best is None or dist / max(rr, 0.3) < best[0]):
                        best = (dist / max(rr, 0.3), ddx, ddy, dist, i, t)
                if best is None:
                    continue
                q, ddx, ddy, dist, i, t = best
                q = min(1.0, q)
                if dist > 1e-4:
                    nx, ny = ddx / dist * q, ddy / dist * q
                else:
                    nx, ny = 0.0, 0.0
                nz = math.sqrt(max(0.0, 1.0 - q * q))
                inten = nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]
                self._put(x, y, ramp, self.tone_of(inten, bias + self.deeper.get(id(ramp), 0.0), x, y, self.dither), int(round(px - ax0)), int(round(py - ay0)), kind, bias >= 0.6)

    def poly(self, pts, ramp, bias=0.0, kind="cloth", lean=(0.0, -0.35)):
        """A flat-ish form (a hat cone, a wing, a mantle): lit across each row like a cylinder."""
        self.part += 1
        ys = [p[1] for p in pts]
        n = len(pts)
        ax0, ay0 = pts[0]
        for y in range(int(math.floor(min(ys))), int(math.ceil(max(ys))) + 1):
            yc = y + 0.5
            xints = []
            for i in range(n):
                (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]
                if y0 == y1:
                    continue
                if min(y0, y1) <= yc < max(y0, y1):
                    xints.append(x0 + (yc - y0) * (x1 - x0) / (y1 - y0))
            xints.sort()
            for k in range(0, len(xints) - 1, 2):
                xa, xb = xints[k], xints[k + 1]
                w = max(1e-3, xb - xa)
                for x in range(int(math.floor(xa)), int(math.ceil(xb))):
                    xc = x + 0.5
                    if xc < xa - 0.15 or xc > xb + 0.15:
                        continue
                    u = (xc - xa) / w * 2.0 - 1.0
                    nx = u * 0.85 + lean[0]
                    ny = lean[1]
                    nz = math.sqrt(max(0.05, 1.0 - min(0.95, nx * nx + ny * ny)))
                    inten = nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]
                    self._put(x, y, ramp, self.tone_of(inten, bias + self.deeper.get(id(ramp), 0.0), x, y, self.dither), int(round(xc - ax0)), int(round(yc - ay0)), kind, bias >= 0.6)

    def dot(self, x, y, colour, kind="detail"):
        """One unlit pixel: an eye, a nose, a star. Never separated, never rimmed."""
        self.part += 1
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.cell and 0 <= y < self.cell:
            self.cells[(x, y)] = (None, colour, self.part, 0, 0, kind)

    def block(self, x, y, colour, n=1, kind="detail"):
        """An n x n block of one unlit colour: a dot, at a form's own detail size (Skin.detail)."""
        for dy in range(n):
            for dx in range(n):
                self.dot(int(round(x)) + dx, int(round(y)) + dy, colour, kind)

    def contact(self, x0, x1, row=None):
        row = self.cell - 1 if row is None else row
        for x in range(int(round(x0)), int(round(x1)) + 1):
            if 0 <= x < self.cell:
                self.shadow.add((x, row))

    # --- the three passes ----------------------------------------------------------------------
    def render(self, finish=False):
        """The frame as a Canvas. `finish` gives it the actor finish (matte.py) - the sprite strips
        ask for it (frames_for); the evolution icons, which paint these same forms into a picture,
        call render() bare and are left as they were drawn."""
        c = raster.Canvas(self.cell, self.cell)
        cells = self.cells
        out = {}
        stops = {}
        for (x, y), (ramp, tone, part, lx, ly, kind) in cells.items():
            if ramp is None:
                out[(x, y)] = tone
                continue
            t = tone
            if kind in ("fur", "far") and t in (1, 2) and self.lay:
                # The lay of the coat: short broken strokes running back and down the body.
                if (lx * 2 + ly * 3) % 7 == 0 and hsh(lx, ly, 3) < 0.7:
                    t += 1
            if kind in ("fur", "far") and self.pattern is not None and ramp is self.pattern[0] and t <= 3:
                # The cat's coat: a tabby's broken bands across each part, or a great cat's rosettes - a
                # stop or two down its own ramp, so the light still turns the form through them.
                if self.pattern[1] == "stripes":
                    if (lx * 3 + ly) % 9 in (0, 1) and hsh(lx // 2, ly, 5) < 0.8:
                        t = min(4, t + 2)
                elif hsh(lx // 2, ly // 2, 7) < 0.16 and (lx + ly) % 2 == 0:
                    t = 4
            elif kind == "cloth" and t in (1, 2, 3):
                # A fold every few columns, a dark stop beside a light one (section 4b).
                if (lx + ly // 3) % 5 == 0:
                    t = min(4, t + 1)
            # A form in front casts a pixel of shadow onto the form behind it, down and right of
            # its edge - the key is upper left. This is every overlapping form separated.
            if kind not in ("detail", "glow"):
                for dx, dy in ((-1, 0), (0, -1)):
                    n = cells.get((x + dx, y + dy))
                    if n is not None and n[2] > part and n[5] not in ("detail", "glow"):
                        t = 4 if t >= 2 else 3
                        break
            t = min(4, max(0, t))
            out[(x, y)] = ramp[t]
            if kind != "glow":
                stops[(x, y)] = (id(ramp), ramp, t)
        if finish:
            # A highlight is a plane, not a point (matte.settle), on the coat's OWN ramp: Cerberus's
            # hide and the frost forms are built from stops of several rows, so the colour alone
            # cannot say which ramp a pixel is on - this is the one place that still knows.
            for p, i in matte.settle(stops).items():
                out[p] = stops[p][1][i]
        for (x, y), (_key, ramp, t) in stops.items():
            _r, _t, _part, lx, ly, kind = cells[(x, y)]
            if ramp is self.embers and kind == "fur" and t in (2, 3):
                # Embers in a dark coat: single hot pixels on the near side, keyed like the lay so
                # they ride with the body - and, as the parts turn, flicker from frame to frame.
                e = hsh(lx, ly, 11)
                if e < EMBER_FLECKS:
                    out[(x, y)] = EMBER[1] if e < EMBER_FLECKS * 0.3 else EMBER[2]
        # The rim: bone-white on the key-facing silhouette edge, the one place light touches.
        for (x, y), (ramp, tone, part, lx, ly, kind) in cells.items():
            # Only the near side, and only where the form faces the key: a rim down the back edge of
            # every leg was a white comb, and the far legs are in the body's own shadow.
            if ramp is None or kind in ("glow", "norim", "far") or tone > 2:
                continue
            if (x - 1, y) not in cells or ((x, y - 1) not in cells and (x - 1, y - 1) not in cells):
                if (x, y + 1) in cells or (x + 1, y) in cells:
                    out[(x, y)] = RIM
        for p in self.shadow:
            if p not in out:
                out[p] = OCC
        for (x, y), col in out.items():
            c.set(x, y, col)
        if finish:
            # ...and the rim as the surface returns it: soft on the dark coats, full on the pale.
            matte.rims(c.img)
        return c


# =============================================================================================
# THE HOUND. Designed on the 32 cell, facing right, paws on row 30. `s` scales it onto a larger
# cell (the Great Hound) about the ground line.
# =============================================================================================

# The fur sits a stop deeper than the light alone would put it: at the bright end of `flesh` the
# first build read pinkish-white in the game, a ghost of a dog. A stop down it is a warm fawn.
FUR_DEPTH = 0.2


class Skin(object):
    """What a hound is made of, so one skeleton draws the fawn hound and the frost one."""

    def __init__(self, fur, eye=(AMBER[0], AMBER[1]), frost=False, mantle=False, heavy=0.0, heads=None,
                 head_scale=1.0, leap=1.0, depth=FUR_DEPTH, glow_eye=False, embers=False, ear=None,
                 feline=False, pattern=None, star=None, breath=None, paws=None, collar_star=True, calm=False,
                 detail=1, mane=False):
        self.fur = fur
        self.eye = eye
        self.frost = frost
        self.mantle = mantle
        self.heavy = heavy
        # (neck dx, neck dy, head turn, bias, wears the hat), far to near. None: one head.
        self.heads = heads
        self.head_scale = head_scale
        # How high it leaves the ground on a pounce: a heavy beast leaps lower, and keeps its hat in the cell.
        self.leap = leap
        # How far below the light the coat sits (Fig.deeper): the hellhound's is mostly its two darkest stops.
        self.depth = depth
        # A lit eye - two pixels of the eye's own colour, the socket too - rather than a warm point in
        # a dark socket. Only the level 8 forms: the base hound's eye stays the plain amber one.
        self.glow_eye = glow_eye
        # Embers in the coat and on the breath: the hellhound's (Fig.embers).
        self.embers = embers
        # The ears' own ramp, where it is not the coat's: the Hound of Annwn's are red. None: the coat.
        self.ear = ear
        # A cat (cat() and _cat_head): a lower, longer body on shorter legs, a round head with pointed
        # ears, a long tail carried up, and a coat pattern (Fig.pattern) - "stripes" or "spots".
        self.feline = feline
        self.pattern = pattern
        # A white star on the breast (the Cait Sith's): a colour, or None.
        self.star = star
        # What the breath is when the mouth is open (the howl, the bite): a 3-colour run, or None.
        self.breath = breath
        # Feet of another ramp (Laelaps's, still stone from the hill he was held in). None: the coat.
        self.paws = paws
        # The gold star on the collar is the wizard's mark: a familiar wears it, a hound of its own
        # (Laelaps, tools/art/laelaps.py) wears a plain disc.
        self.collar_star = collar_star
        # The calm eye (calm_eye) on the sprite as well as the portrait: no dark socket pixel. For a
        # figure that is also its own portrait (a playable character, shown at x5 on the select).
        self.calm = calm
        # How many pixels a "one pixel" feature is - an eye, a nostril, a tooth, a claw. 1 for every form
        # drawn on the 48 and 64 cells; 2 for Cerberus on its own 96 cell (Eric, 2026-10-30: "the main
        # cerberus itself should be bigger"), where a one-pixel eye on a head twice the size read as a
        # pinprick - the form is DRAWN for the bigger cell, never the smaller one scaled up.
        self.detail = detail
        # Hackles: ragged spikes of the coat along the spine and the backs of the necks, ember-tipped on a
        # smouldering coat. Cerberus at 96 only - at 64 they were a pixel each and read as noise.
        self.mane = mane


# The head a little large for the body: at 48 pixels the face is what says "dog" under the hat.
HOUND = Skin(FAWN, leap=0.85, head_scale=1.15)
RIME = Skin(FROST, frost=True, leap=0.85, head_scale=1.15)
GREAT = Skin(FAWN, mantle=True, heavy=1.0, leap=0.6, head_scale=1.1)
# Three heads on one neck root: the far one high and back, the near one low and forward. The hat is
# on the middle head, which is the one the eye goes to.
_THREE = [(-0.6, -7.6, -0.3, 0.35, True), (2.6, -1.2, 0.05, 0.15, False), (4.0, 5.0, 0.42, -0.05, False)]
# Cerberus is the hellhound: the HELL hide, six eyes lit ember red, embers in the coat and on its breath.
# Deep enough that a plane square to the viewer never dithers up into the reds: at the hound's depth
# the blood red checkered right across the body and the whole beast read crimson, not black.
#
# BIGGER (Eric, 2026-10-30, of 0.9.2: "the main cerberus itself should be bigger"). It was the 64 cell at
# 1.72, the Great Hound's size, and a three-headed beast no bigger than the hound it grew from read as the
# same dog with extras. It is now the 96 cell - the contract's boss cell, the one size up - drawn at 2.5:
# the renderer lays every part at that size, so the coat's lay, the folds of the mantle and the light are
# drawn at 96, not a 64 frame scaled up. What a bigger head needs that a small one did not is the detail
# block (two-pixel eyes, nostrils, teeth and claws) and the hackles, ember-tipped, along the spine and the
# backs of the three necks. Only the MAIN hound is this: the rest of the pack stays the hound's size
# (HELL_PACK, the pups), so the legendary creature is one, and the biggest thing at his side.
CERB = Skin(HELL, eye=(EMBER[0], EMBER[1]), mantle=True, heavy=1.4, heads=_THREE, head_scale=0.82, leap=0.45,
            depth=0.6, glow_eye=True, embers=True, detail=2, mane=True)
# After the Ice path it stays frost - azure fur, the ruff, frost breath - but a whole stop down the
# azure row from the Rimehound (its hi is the Rimehound's lit, its deep wool's navy), to stand beside
# the hellhound as the same beast; and its eyes are lit ice.
RIME_DEEP = [FROST[1], FROST[2], FROST[3], FROST[4], WOOL[4]]
CERB_RIME = Skin(RIME_DEEP, eye=(ICE[0], ICE[1]), frost=True, mantle=True, heavy=1.4, heads=_THREE,
                 head_scale=0.82, leap=0.45, glow_eye=True, detail=2, mane=True)


def _pose(**kw):
    p = dict(swing=(0.0, 0.0, 0.0, 0.0), lift=(0.0, 0.0, 0.0, 0.0), bob=0.0, stretch=0.0,
             head=(0.0, 0.0, 0.0), jaw=0.0, tail=0.0, hat=0.0, ear=0.0, sit=0.0, breathe=0.0,
             air=0.0, crouch=0.0, breath=0)
    p.update(kw)
    return p


# The foreleg's pastern (front_leg): its length, the angle it stands at under a weight-bearing leg (down
# and a little forward onto the paw) and the angle it folds back to in the swing, in screen radians.
PASTERN = 1.6
PASTERN_DOWN = math.radians(68.0)
PASTERN_FOLD = math.radians(150.0)
# How far a forepaw travels per unit of a pose's swing: a long reach forward, a shorter sweep back.
FRONT_REACH = 7.0
FRONT_SWEEP = 3.6
# The least a reaching foreleg points ahead of straight down, in radians: a gallop's forelegs reach.
REACH_ANGLE = math.radians(40.0)


def hound(cell, skin, p, s=1.0):
    """One hound frame. p: a _pose. Legs are (front near, front far, hind near, hind far)."""
    g = Fig(cell)
    g.deeper[id(skin.fur)] = skin.depth
    if skin.embers:
        g.embers = skin.fur
    if skin.pattern:
        g.pattern = (skin.fur, skin.pattern)
    # A cat stands lower on shorter legs, its body longer and leaner than a hound's.
    low = 3.2 if skin.feline else 0.0
    lean = 0.86 if skin.feline else 1.0
    # ...and its haunch sits closer behind its shoulders.
    tuck = 1.4 if skin.feline else 0.0
    G = 30.0
    gy = cell - 2.0
    ox = (cell - 32.0 * s) / 2.0

    def T(x, y):
        return (x * s + ox, (y - G) * s + gy)

    def R(r):
        return r * s

    fur = skin.fur
    heavy = skin.heavy
    air = p["air"] * skin.leap
    rise = air + p["bob"] - p["crouch"]
    sit = p["sit"]
    st = p["stretch"]
    # The two masses. A sitting hound drops its haunch onto its hocks and lifts its chest.
    hx, hy = 9.6 + tuck - st * 0.5 + sit * 1.0, 19.2 - rise + sit * (4.2 - low * 0.6) + low
    cx, cy = 20.2 + st * 0.5, 18.8 - rise - sit * 1.6 + low
    sw = p["swing"]
    li = p["lift"]

    def front_leg(k, bias):
        # A DOG'S FORELEG (Eric, 2026-10-28: "the dogs front legs look like they're going backwards").
        # It was one two-bone reach from mid-chest to the paw with the joint thrown FORWARD, so every
        # standing leg had a knee jutting out at mid-height and its shin raking back to the paw - a
        # hind leg put on backwards, and worse on the heavy forms, whose longer bone bent it further.
        # A foreleg is three joints, and none of them points forward while it bears weight:
        #   - the ROOT is the point of the shoulder, at the front of the chest; the upper arm runs
        #     down and BACK from it, mostly over the chest, to
        #   - the ELBOW, which points back and sits at the bottom of the chest (the brisket), so a
        #     standing forearm drops straight from it - the column under the chest that says "dog";
        #   - the CARPUS (the wrist), low, where a short pastern slopes forward onto the paw. It is
        #     the one joint that folds: in the air, as the leg swings through, the paw tucks back
        #     under it, and it opens again as the leg reaches forward for the ground.
        root = (cx + 2.6, cy + 1.6)
        drop = 2.5 + heavy * 0.7           # root to elbow, standing: deeper chest, lower elbow
        back = 3.0                         # how far behind the root the elbow and the standing forearm are
        ua = math.sqrt(drop * drop + back * back)
        # The forearm is what is left of the standing root's height (9.6) once the elbow and pastern have theirs.
        fa = 9.6 - low - PASTERN * math.sin(PASTERN_DOWN) - drop
        # Where the paw goes. Standing, under the forearm; in the gallop it reaches a long way forward
        # (to below the muzzle) and sweeps a shorter way back under the chest.
        stand = -back + PASTERN * math.cos(PASTERN_DOWN)
        reach = sw[k] * (FRONT_REACH if sw[k] > 0 else FRONT_SWEEP)
        paw = (root[0] + stand + reach, G - li[k] - max(0.0, air - 2.5) * 0.55)
        # The fold: most as the lifted paw passes under the body, opening as it reaches forward.
        fold = min(1.0, li[k] / 2.6) * (1.0 - min(1.0, max(0.0, sw[k])) * 0.85)
        if sit:
            # Sitting, the forelegs are two straight columns under the raised chest.
            paw = (root[0] - 1.0, G)
            fold = 0.0
        pa = PASTERN_DOWN + fold * (PASTERN_FOLD - PASTERN_DOWN)
        carpus = (paw[0] - math.cos(pa) * PASTERN, paw[1] - math.sin(pa) * PASTERN)
        dx, dy = carpus[0] - root[0], carpus[1] - root[1]
        d = math.sqrt(dx * dx + dy * dy)
        # THE REACH: swinging forward through the air, the leg opens out straight and swings up to at
        # least REACH_ANGLE in front of the shoulder - shoulder, elbow and wrist in one line, the paw
        # held off the ground ahead of the chest - rather than arriving bent at the elbow.
        straight = min(1.0, max(0.0, sw[k])) * min(1.0, li[k] / 1.2)
        want = d + max(0.0, (ua + fa) * 0.97 - d) * straight
        # Out of reach (the pounce's air), the leg straightens toward the paw rather than stretching.
        want = min(want, (ua + fa) * 1.06)
        if abs(want - d) > 1e-3 or straight > 0.0:
            ang = math.atan2(dx, dy)
            ang += max(0.0, REACH_ANGLE - ang) * straight
            carpus = (root[0] + math.sin(ang) * want, root[1] + math.cos(ang) * want)
            paw = (carpus[0] + math.cos(pa) * PASTERN, carpus[1] + math.sin(pa) * PASTERN)
            if paw[1] > G:
                # Never through the ground: a reach that would put the paw under it stops on it.
                carpus = (carpus[0], carpus[1] - (paw[1] - G))
                paw = (paw[0], G)
        # +1: the elbow is on the BACK side of the root-to-wrist line, always.
        elbow = ik(root, carpus, ua, fa, 1)
        pw = T(*paw)
        g.limb([T(*root), T(*elbow), T(*carpus), pw],
               [R(2.0 + heavy * 0.5), R(1.45 + heavy * 0.3), R(1.05 + heavy * 0.2), R(0.95 + heavy * 0.15)],
               fur, bias, "fur")
        # The foot: toes forward on the ground, curled under as the pastern folds back.
        fx, fy = rot(0.7, 0.1, fold * 1.5)
        g.blob(pw[0] + R(fx), pw[1] + R(fy), R(1.5 + heavy * 0.2), R(0.85), skin.paws or fur, bias + 0.4, "body", angle=fold * 1.5)
        if skin.detail > 1 and bias < 0.6:
            _claws(g, pw, R, skin.detail)
        return pw

    def hind_leg(k, bias):
        hip = (hx - 0.4, hy + 1.6)
        paw = (hip[0] - 0.4 + sw[k] * 4.2, G - li[k] - max(0.0, air - 2.5) * 0.55)
        if sit:
            paw = (hip[0] + 3.4, G)
        hock = (paw[0] - 1.7, paw[1] - 2.7)
        if sit:
            hock = (hip[0] - 1.2, G - 0.9)
        knee = ik(hip, hock, 4.4 + heavy * 0.4, 4.0, 1)
        pw = T(*paw)
        g.limb([T(*hip), T(*knee)], [R(2.6 + heavy * 0.6), R(1.5 + heavy * 0.3)], fur, bias, "fur")
        g.limb([T(*knee), T(*hock), pw], [R(1.4 + heavy * 0.3), R(1.0 + heavy * 0.2), R(1.0)], fur, bias + 0.15, "fur")
        g.blob(pw[0] + R(0.7), pw[1] + R(0.1), R(1.5 + heavy * 0.2), R(0.85), skin.paws or fur, bias + 0.4, "body")
        if skin.detail > 1 and bias < 0.6:
            _claws(g, pw, R, skin.detail)
        return pw

    paws = []
    # FAR SIDE first, a stop darker: the legs on the other side of the body, and the tail.
    tail_a = p["tail"]
    base = (hx - 3.6 * lean, hy - 1.6)
    tp = [base]
    if skin.feline:
        for i, (dx, dy) in enumerate(((-2.4, -0.6), (-1.6, -2.2), (-0.6, -2.6), (0.6, -2.4), (1.4, -1.2))):
            a = tail_a * (0.35 + i * 0.3)
            rx, ry = rot(dx, dy, a)
            tp.append((tp[-1][0] + rx, tp[-1][1] + ry))
        g.limb([T(*q) for q in tp], [R(1.3 + heavy * 0.3), R(1.15), R(1.05), R(0.95), R(0.85), R(0.7)], fur, 0.25, "fur")
    else:
        for i, (dx, dy) in enumerate(((-2.2, -2.0), (-1.4, -2.8), (-0.2, -2.6))):
            a = tail_a * (0.5 + i * 0.35)
            rx, ry = rot(dx, dy, a)
            tp.append((tp[-1][0] + rx, tp[-1][1] + ry))
        g.limb([T(*q) for q in tp], [R(1.4 + heavy * 0.3), R(1.1), R(0.8), R(0.5)], fur, 0.25, "fur")
    paws.append(hind_leg(3, 1.25))
    paws.append(front_leg(1, 1.25))

    # THE BODY: haunch, barrel and chest, the waist tucked up under a level back.
    breathe = p["breathe"]
    g.blob(*T(hx, hy), rx=R((4.6 + heavy * 0.8) * lean), ry=R((4.2 + heavy * 0.6) * lean), ramp=fur, bias=0.0, kind="fur")
    mid = ((hx + cx) / 2.0, (hy + cy) / 2.0 - 1.0 + low * 0.3)
    g.limb([T(hx + 1.0, hy - 0.6), T(*mid), T(cx - 1.0, cy - 0.4)],
           [R((3.6 + heavy * 0.7) * lean), R((2.7 + heavy * 0.8) * (lean + (0.08 if skin.feline else 0.0))), R((3.9 + heavy * 0.7) * lean)], fur, 0.0, "fur")
    g.blob(*T(cx, cy), rx=R((5.0 + heavy * 0.9) * lean), ry=R((4.8 + breathe * 0.3 + heavy * 0.8) * lean), ramp=fur, bias=-0.1, kind="fur")
    if skin.mane:
        # Hackles along the spine, from the haunch to the withers, raised and swept back.
        back = []
        for k, t in enumerate((0.05, 0.3, 0.55, 0.8)):
            bx = hx + (cx - hx) * t
            by = (hy - 4.2 - heavy * 0.6) * (1.0 - t) + (cy - 4.8 - heavy * 0.8) * t + 0.6
            back.append((bx, by, -2.25 - 0.1 * (k % 2), 1.5 + 0.5 * ((k + 1) % 2)))
        _hackles(g, T, R, back, fur, 0.05, skin.embers)
    if skin.star:
        # The Cait Sith's white star on its breast, low on the near side of the chest.
        sx, sy = T(cx + 3.0, cy + 1.2)
        for (ddx, ddy) in ((0, 0), (1, 0), (0, 1), (0, -1), (-1, 0)):
            g.dot(sx + ddx, sy + ddy, skin.star)

    # NEAR SIDE legs.
    paws.append(hind_leg(2, 0.0))

    # THE NECK goes UP off the chest - the wolves' goes down - and the head is carried high.
    # Cerberus carries three: the far one first, a stop darker, the hat on the middle one.
    hdx, hdy, ha = p["head"]
    heads = skin.heads or [(0.0, 0.0, 0.0, 0.0, True)]
    hc = None
    for i, (ndx, ndy, nda, nbias, hat) in enumerate(heads):
        if i == len(heads) - 1:
            if skin.mantle:
                _mantle(g, T, R, hx, hy, cx, cy, p)
            if skin.frost:
                _frost_ruff(g, T, R, hx, hy, cx, cy, (cx + 5.4 + hdx, cy - 8.6 + hdy))
        spread = p.get("spread", 0.0)
        neck0 = (cx + 2.2 + ndx * 0.3, cy - 2.6 + ndy * 0.25)
        hci = (cx + 5.4 + hdx + ndx * (1.0 + spread * 0.25), cy - 8.6 + hdy + ndy * (1.0 + spread * 0.2))
        if skin.feline:
            # A cat's head sits close on a short thick neck, forward of the shoulders and lower.
            hci = (cx + 4.4 + hdx * 0.8, cy - 5.4 + hdy * 0.8)
        if skin.mane:
            # Hackles down the back of this neck, behind it, so the neck stands in front of their roots.
            nx, ny = hci[0] - neck0[0], hci[1] - neck0[1]
            ln = math.sqrt(nx * nx + ny * ny) or 1.0
            px, py = ny / ln, -nx / ln
            if px > 0:
                px, py = -px, -py
            ridge = [(neck0[0] + nx * t + px * (2.2 + heavy * 0.3), neck0[1] + ny * t + py * (2.2 + heavy * 0.3),
                      math.atan2(py, px) - 0.5, 1.6 + 0.4 * (k % 2))
                     for k, t in enumerate((0.15, 0.45, 0.75))]
            _hackles(g, T, R, ridge, fur, nbias + 0.05, skin.embers)
        g.limb([T(*neck0), T((neck0[0] + hci[0]) / 2.0 + 0.4, (neck0[1] + hci[1]) / 2.0), T(hci[0] - 0.6, hci[1] + 0.6)],
               [R(3.0 + heavy * 0.5), R(2.4 + heavy * 0.35), R(2.1 + heavy * 0.25)], fur, nbias - 0.05, "fur")
        # THE COLLAR at the base of each neck, with the gold star hanging from it.
        col0 = (neck0[0] + 0.2 + (hci[0] - neck0[0]) * 0.18, neck0[1] - 0.6 + (hci[1] - neck0[1]) * 0.18)
        col1 = (col0[0] + 1.8, col0[1] + 0.8)
        g.limb([T(*col0), T(*col1)], [R(0.9), R(0.8)], GOLD, nbias - 0.2, "body")
        star = T(col1[0] + 0.1, col1[1] + 1.5)
        if skin.collar_star:
            g.dot(star[0], star[1], GOLD[0])
            g.dot(star[0], star[1] + 1, LIGHT_E[0] if not skin.frost else ICE[0])
        else:
            g.dot(star[0], star[1], GOLD[1])
            g.dot(star[0], star[1] + 1, GOLD[2])
        if skin.feline:
            _cat_head(g, T, R, hci, ha * 0.8, p, skin, hat=hat, bias=nbias)
        else:
            _head(g, T, R, hci, ha + nda * (1.0 + spread * 0.4), p, skin, hat=hat, bias=nbias)
        if hat:
            hc = hci
    paws.append(front_leg(0, 0.0))

    # THE CONTACT LINE. It stays on the ground when the hound leaves it, and shrinks.
    xs = [q[0] for q in paws]
    lo, hi = min(xs) - R(1.0), max(xs) + R(2.0)
    if air > 0.5:
        mid_x = (lo + hi) / 2.0
        half = (hi - lo) / 2.0 * max(0.35, 1.0 - air * 0.09)
        lo, hi = mid_x - half, mid_x + half
    g.contact(lo, hi)
    return g


def _head(g, T, R, hc, ha, p, skin, hat=True, bias=0.0):
    """Skull, long muzzle, jaw, ear, eye - and the hat. Rotated about the skull by `ha`."""
    fur = skin.fur
    jaw = p["jaw"]
    b = bias
    hs = skin.head_scale
    R0 = R

    def R(r):
        return R0(r * hs)

    def H(dx, dy):
        rx, ry = rot(dx * hs, dy * hs, ha)
        return T(hc[0] + rx, hc[1] + ry)

    # The ear hangs back under the brim, on the far side of the skull from the light.
    ear_a = p["ear"]
    e0 = H(-1.0, -1.6)
    ex, ey = rot(-2.0, 2.6, ear_a)
    e1 = H(-1.0 + ex, -1.6 + ey)
    # Annwn's red ears are its read, so a coloured ear hangs a little fuller than the coat's.
    big = 1.25 if skin.ear else 1.0
    g.limb([e0, e1], [R(1.25 * big), R(0.85 * big)], skin.ear or fur, 0.8 + b, "fur")
    # The lower jaw first, so the muzzle sits over it; it swings down to open.
    j0 = (0.6, 1.4)
    jx, jy = rot(3.6, 0.6, jaw * 0.75)
    g.limb([H(*j0), H(j0[0] + jx, j0[1] + jy)], [R(1.1), R(0.7)], fur, 0.5 + b, "fur")
    if jaw > 0.25:
        # The inside of the open mouth: teeth along both jaws, the tongue between.
        tx, ty = rot(2.6, 0.5, jaw * 0.4)
        g.dot(*H(j0[0] + tx, j0[1] + ty - 0.2), colour=EMBER[1])
        g.dot(*H(3.2, 1.1), colour=BONE[0])
        g.dot(*H(j0[0] + jx - 0.4, j0[1] + jy - 0.6), colour=BONE[1])
    g.blob(*H(0.0, 0.0), rx=R(3.0), ry=R(2.6), ramp=fur, bias=b - 0.1, kind="fur", angle=ha)
    g.limb([H(1.6, -0.2), H(3.6, 0.3), H(5.0, 0.9)], [R(1.85), R(1.5), R(1.05)], fur, b, "fur")
    if skin.detail > 1:
        breath = _big_face(g, H, skin, jaw, fur)
        if p.get("breath") and breath:
            for (dx, dy), c in zip(((6.6, 0.4), (7.6, -0.4), (8.4, 0.6), (9.0, -0.6)), breath + breath[:1]):
                g.dot(*H(dx, dy), colour=c, kind="glow")
        if hat:
            _hat(g, H, R, p)
        return
    g.dot(*H(5.4, 0.7), colour=OCC)
    # The eye: a warm point in a dark socket, under a brow. Amber is the player's light; the
    # enemies' eyes are the cold mark, and this is the one place the difference is spelled out.
    g.dot(*H(0.9, -1.4), colour=fur[4])
    g.dot(*H(1.8, -1.3), colour=fur[3])
    if skin.glow_eye:
        # The level 8 forms' eye is lit: the socket filled with its colour, a hot point in front.
        g.dot(*H(0.9, -0.5), colour=skin.eye[1], kind="glow")
        g.dot(*H(1.7, -0.5), colour=skin.eye[0], kind="glow")
    else:
        if skin.calm:
            g.dot(*H(0.9, -0.5), colour=fur[3])
            g.dot(*H(1.7, -0.5), colour=calm_eye(skin.eye)[0])
        else:
            g.dot(*H(0.9, -0.5), colour=OCC)
            g.dot(*H(1.7, -0.5), colour=skin.eye[1])
    g.dot(*H(1.4, 0.4), colour=fur[4])
    if p.get("breath") and skin.frost:
        for i, (dx, dy) in enumerate(((6.6, 0.4), (7.6, -0.4), (8.4, 0.6))):
            g.dot(*H(dx, dy), colour=ICE[i % 2], kind="glow")
    elif p.get("breath") and skin.embers:
        # The hellhound breathes sparks where the Rimehound breathes frost.
        for (dx, dy), c in zip(((6.6, 0.4), (7.6, -0.4), (8.4, 0.6)), (EMBER[1], EMBER[0], EMBER[2])):
            g.dot(*H(dx, dy), colour=c, kind="glow")
    elif p.get("breath") and skin.breath:
        for (dx, dy), c in zip(((6.6, 0.4), (7.6, -0.4), (8.4, 0.6)), skin.breath):
            g.dot(*H(dx, dy), colour=c, kind="glow")
    if hat:
        _hat(g, H, R, p)


def _hat(g, H, R, p):
    """The wizard's own hat, small: indigo wool, a gold band, a gold star, the tip swept back."""
    tilt = p["hat"]

    # Seated on the crown, clear of the brow, so the eye shows under the brim.
    def Hh(dx, dy):
        rx, ry = rot(dx, dy, tilt)
        return H(rx - 0.7, ry - 2.7)

    flap = math.sin(p.get("flap", 0.0)) * 0.6
    cone = [Hh(-2.6, 0.2), Hh(2.4, 0.2), Hh(1.0, -3.4), Hh(-0.6 - flap, -6.4), Hh(-2.6 - flap * 1.5, -7.6),
            Hh(-3.4 - flap * 1.6, -6.6), Hh(-1.9, -4.2)]
    g.poly(cone, WOOL, 0.35, "cloth", lean=(0.0, -0.35))
    # The brim, wider than the cone and flat.
    b0 = Hh(-3.6, 0.6)
    b1 = Hh(3.3, 0.6)
    g.limb([b0, b1], [R(0.8), R(0.8)], WOOL, 0.25, "body")
    # The band and the star.
    g.limb([Hh(-2.2, -0.5), Hh(2.0, -0.5)], [R(0.55), R(0.55)], GOLD, -0.4, "norim")
    sx, sy = Hh(-0.4, -2.4)
    g.dot(sx, sy, GOLD[0])
    g.dot(sx + 1, sy, LIGHT_E[1])
    g.dot(sx, sy - 1, GOLD[1])


def _big_face(g, H, skin, jaw, fur):
    """The face of a form drawn on the 96 cell (Skin.detail 2): the nose, the brow, the lit eye and the
    teeth at two pixels a feature, where _head's one-pixel dots on a head this size read as pinpricks."""
    n = skin.detail
    nx, ny = H(5.0, 0.3)
    g.block(nx - 1, ny, OCC, n)
    g.dot(nx + 1, ny + 1, OCC)
    # The brow: a dark ridge over the eye, two pixels deep at the front.
    for (dx, dy) in ((0.2, -1.7), (0.9, -1.8), (1.6, -1.7), (2.3, -1.5)):
        g.dot(*H(dx, dy), colour=fur[4])
    bx, by = H(2.3, -1.5)
    g.dot(bx, by + 1, fur[3])
    ex, ey = H(0.8, -0.9)
    if skin.glow_eye:
        g.block(ex, ey, skin.eye[1], n, "glow")
        g.dot(ex + n, ey + 1, skin.eye[0], kind="glow")
    else:
        warm, dark = calm_eye(skin.eye)
        g.block(ex, ey, dark, n)
        g.dot(ex + n, ey + 1, warm)
    # The line of the lip back from the nose, and the cheek under the eye.
    for dx in (2.2, 3.0, 3.8):
        g.dot(*H(dx, 1.0), colour=fur[4])
    g.dot(*H(1.4, 0.5), colour=fur[4])
    if jaw > 0.25:
        # Fangs: the upper two-pixel canine at the front of the jaw, smaller teeth behind it, and the lower
        # canine standing up off the dropped jaw.
        jx, jy = rot(3.6, 0.6, jaw * 0.75)
        fx, fy = H(4.2, 1.2)
        g.dot(fx, fy, BONE[0])
        g.dot(fx, fy + 1, BONE[1])
        for dx in (2.6, 3.4):
            g.dot(*H(dx, 1.3), colour=BONE[1])
        lx, ly = H(0.6 + jx * 0.85, 1.4 + jy * 0.85 - 0.6)
        g.dot(lx, ly, BONE[0])
        g.dot(lx, ly - 1, BONE[1])
    # What it breathes when its mouth is open (_head draws it): frost, sparks, or its skin's own.
    breath = None
    if skin.frost:
        breath = (ICE[0], ICE[1], ICE[0])
    elif skin.embers:
        breath = (EMBER[1], EMBER[0], EMBER[2])
    elif skin.breath:
        breath = skin.breath
    return breath


def _claws(g, pw, R, n):
    """Claws at the front of a paw, on a form drawn on the 96 cell (Skin.detail): bone, two of them."""
    x, y = pw[0] + R(1.7), pw[1] + R(0.5)
    g.dot(x, y, BONE[1])
    g.dot(x - 2, y, BONE[2])


def _hackles(g, T, R, pts, fur, bias, embers):
    """Ragged spikes of the coat standing off a ridge (Skin.mane): (x, y, angle, length) in the hound's
    design units. On a smouldering coat every other tip is an ember, as the coat's own flecks are."""
    for k, (x, y, a, ln) in enumerate(pts):
        tip = (x + math.cos(a) * ln, y + math.sin(a) * ln)
        ox, oy = -math.sin(a) * 0.75, math.cos(a) * 0.75
        g.poly([T(x - ox, y - oy), T(x + ox, y + oy), T(*tip)], fur, bias, "fur", lean=(-0.2, -0.5))
        if embers and k % 2 == 0:
            tx, ty = T(*tip)
            g.dot(tx, ty, EMBER[2], kind="glow")


def _cat_head(g, T, R, hc, ha, p, skin, hat=True, bias=0.0):
    """A cat's head: a round skull, a short muzzle, two pointed ears, a slit-pupilled eye - and the hat,
    sat back on the crown between the ears, the near ear standing in front of its brim."""
    fur = skin.fur
    jaw = p["jaw"]
    b = bias
    hs = skin.head_scale
    R0 = R

    def R(r):
        return R0(r * hs)

    def H(dx, dy):
        rx, ry = rot(dx * hs, dy * hs, ha)
        return T(hc[0] + rx, hc[1] + ry)

    ear = skin.ear or fur
    ear_a = p["ear"] * 0.4
    # The far ear first, a stop darker.
    g.poly([H(-2.2, -1.2), H(-0.6, -2.0), H(*rot(-2.0, -4.8, ear_a))], ear, 0.75 + b, "fur", lean=(-0.2, -0.4))
    # The lower jaw, under the muzzle; it drops to open (a hiss, a yowl).
    jx, jy = rot(1.9, 0.5, jaw * 0.7)
    g.limb([H(0.8, 1.5), H(0.8 + jx, 1.5 + jy)], [R(0.9), R(0.6)], fur, 0.45 + b, "fur")
    if jaw > 0.25:
        g.dot(*H(1.8, 1.7), colour=EMBER[1])
        g.dot(*H(2.2, 1.3), colour=BONE[0])
    g.blob(*H(0.0, 0.0), rx=R(2.9), ry=R(2.6), ramp=fur, bias=b - 0.12, kind="fur", angle=ha)
    # The muzzle: a short round pad, the nose at its tip.
    g.blob(*H(2.2, 0.8), rx=R(1.35), ry=R(1.05), ramp=fur, bias=b - 0.05, kind="fur", angle=ha)
    g.dot(*H(3.3, 0.4), colour=EMBER[0])
    g.dot(*H(3.2, 1.2), colour=fur[4])
    # The eye: warm - the player's light, as the hound's is - and calm: no near-white point and no dark
    # slit (2026-10-28, the portraits' "too prominent" eye; at a sprite's two pixels a slit read the same).
    if skin.glow_eye:
        g.dot(*H(1.0, -0.6), colour=skin.eye[1], kind="glow")
        g.dot(*H(1.8, -0.6), colour=calm_eye(skin.eye)[0], kind="glow")
    else:
        g.dot(*H(1.0, -0.6), colour=skin.eye[1])
        g.dot(*H(1.8, -0.6), colour=calm_eye(skin.eye)[0])
    g.dot(*H(1.0, -1.5), colour=fur[4])
    g.dot(*H(1.9, -1.5), colour=fur[3])
    if p.get("breath") and skin.breath:
        for (dx, dy), c in zip(((4.4, 0.6), (5.2, -0.2), (5.8, 0.8)), skin.breath):
            g.dot(*H(dx, dy), colour=c, kind="glow")
    if hat:
        tilt = p["hat"]

        # Sat back on the skull between the ears, smaller than the hound's, so both ears show.
        def Hh(dx, dy):
            rx, ry = rot(dx, dy, tilt)
            return H(rx * 0.68 - 1.4, ry * 0.68 - 2.0)
        flap = math.sin(p.get("flap", 0.0)) * 0.6
        cone = [Hh(-2.6, 0.2), Hh(2.4, 0.2), Hh(1.0, -3.4), Hh(-0.6 - flap, -6.4), Hh(-2.6 - flap * 1.5, -7.6),
                Hh(-3.4 - flap * 1.6, -6.6), Hh(-1.9, -4.2)]
        g.poly(cone, WOOL, 0.35, "cloth", lean=(0.0, -0.35))
        g.limb([Hh(-3.6, 0.6), Hh(3.3, 0.6)], [R(0.75), R(0.75)], WOOL, 0.25, "body")
        g.limb([Hh(-2.2, -0.5), Hh(2.0, -0.5)], [R(0.5), R(0.5)], GOLD, -0.4, "norim")
        sx, sy = Hh(-0.4, -2.4)
        g.dot(sx, sy, GOLD[0])
        g.dot(sx + 1, sy, LIGHT_E[1])
    # The near ear, standing in front of the brim.
    g.poly([H(0.2, -1.8), H(2.0, -1.9), H(*rot(1.2, -5.2, ear_a))], ear, 0.1 + b, "fur", lean=(-0.3, -0.4))
    g.dot(*H(1.1, -2.8), colour=EMBER[0] if not skin.glow_eye else ear[1])
    g.dot(*H(1.1, -3.6), colour=EMBER[1] if not skin.glow_eye else ear[2])


def _mantle(g, T, R, hx, hy, cx, cy, p):
    """The Great Hound's mantle: indigo wool over the shoulders, gold at the hem, flaring back."""
    flare = p.get("flare", 0.0)
    pts = [T(cx + 2.6, cy - 4.4), T(cx + 3.4, cy - 1.0), T(cx + 0.4, cy + 2.0), T(hx + 3.0, hy + 0.8 + flare * 0.5),
           T(hx - 1.6 - flare, hy - 1.2 - flare * 0.6), T(hx + 1.2, hy - 4.0), T(cx - 1.4, cy - 5.0)]
    g.poly(pts, WOOL, 0.15, "cloth", lean=(-0.1, -0.5))
    g.limb([T(cx + 0.4, cy + 2.0), T(hx + 3.0, hy + 0.8 + flare * 0.5), T(hx - 1.6 - flare, hy - 1.2 - flare * 0.6)],
           [R(0.55), R(0.55), R(0.5)], GOLD, -0.2, "norim")


def _frost_ruff(g, T, R, hx, hy, cx, cy, hc):
    """The Rimehound's ruff: shards of ice standing up along the neck and back."""
    spikes = [(cx + 2.0, cy - 3.6, -0.5, 3.2), (cx - 0.2, cy - 4.4, -0.8, 2.8), (cx - 2.8, cy - 4.2, -1.0, 2.2),
              (hx + 2.4, hy - 4.0, -1.1, 1.8), (hc[0] - 2.6, hc[1] + 0.4, -0.3, 2.4)]
    for (bx, by, lean, ln) in spikes:
        tip = (bx + lean, by - ln)
        g.poly([T(bx - 0.9, by), T(bx + 0.9, by), T(*tip)], [ICE[0], ICE[1], ICE[2], ICE[3], FROST[3]], -0.4, "body",
               lean=(-0.2, -0.6))


# --- the four animations ------------------------------------------------------------------------

def gallop(k, n=6):
    """The rotary gallop: fronts and hinds half a cycle apart, each pair a little offset."""
    ph = 2 * math.pi * k / n
    sw, li = [], []
    for off in (0.0, 0.75, math.pi, math.pi + 0.75):
        a = ph + off
        sw.append(math.sin(a) * 1.15)
        li.append(max(0.0, math.cos(a)) * 2.6)
    return _pose(swing=tuple(sw), lift=tuple(li), stretch=math.sin(ph) * 1.6,
                 bob=max(0.0, -math.sin(ph)) * 1.2, head=(1.2, 1.2, 0.12 + math.sin(ph) * 0.05),
                 tail=-0.9 + math.sin(ph) * 0.15, hat=-0.35, ear=-0.6, flap=ph, flare=1.0 + math.sin(ph) * 0.5)


HOUND_ANIMS = [
    ("idle", [_pose(tail=0.0, breathe=0.0, hat=-0.08),
              _pose(tail=0.28, breathe=0.6, hat=-0.08, flap=0.6),
              _pose(tail=0.5, breathe=1.0, hat=-0.06, ear=0.2, flap=1.2),
              _pose(tail=0.28, breathe=0.5, hat=-0.08, flap=0.6)], True, 6.0),
    ("run", [gallop(k) for k in range(6)], True, 12.0),
    ("pounce", [
        _pose(crouch=2.2, swing=(0.5, 0.4, 0.8, 0.7), head=(0.8, 2.4, 0.15), tail=-0.6, hat=-0.3, ear=-0.4),
        _pose(air=2.5, stretch=2.0, swing=(1.3, 1.1, -1.2, -1.0), lift=(2.5, 2.0, 1.0, 0.6), head=(1.6, 1.0, 0.1),
              jaw=0.4, tail=-1.1, hat=-0.5, ear=-0.8, flap=1.0, flare=1.6, spread=0.6),
        _pose(air=6.0, stretch=2.6, swing=(1.5, 1.3, -1.3, -1.2), lift=(1.0, 0.8, 0.4, 0.2), head=(2.2, 1.0, 0.18),
              jaw=1.0, tail=-1.3, hat=-0.6, ear=-1.0, flap=2.0, flare=2.0, spread=1.0),
        _pose(air=3.0, stretch=2.0, swing=(1.4, 1.2, -1.0, -0.9), lift=(0.4, 0.2, 0.6, 0.4), head=(2.0, 2.2, 0.3),
              jaw=1.0, tail=-1.0, hat=-0.45, ear=-0.8, flap=3.0, flare=1.6, spread=1.3),
        _pose(crouch=1.2, stretch=-0.6, swing=(0.9, 0.7, 0.2, 0.0), head=(1.8, 3.4, 0.42), jaw=0.0, tail=-0.4,
              hat=0.15, ear=0.3, flap=4.0, flare=0.6, breath=1, spread=1.5),
        _pose(crouch=0.4, swing=(0.6, 0.4, 0.0, -0.1), head=(1.2, 2.2, 0.05), jaw=0.15, tail=-0.2, hat=-0.2,
              ear=-0.3, flap=5.0, breath=1, spread=0.6),
    ], False, 14.0),
    ("howl", [
        _pose(tail=0.2, hat=-0.08),
        _pose(sit=0.6, tail=0.4, head=(-0.2, -0.6, -0.1), hat=-0.12),
        _pose(sit=1.0, tail=0.5, head=(-0.6, -1.4, -0.5), jaw=0.2, hat=-0.3, ear=-0.3),
        _pose(sit=1.0, tail=0.6, head=(-1.0, -2.0, -0.95), jaw=0.6, hat=-0.55, ear=-0.6, flap=1.0, breath=1),
        _pose(sit=1.0, tail=0.7, head=(-1.0, -2.2, -1.0), jaw=0.8, hat=-0.62, ear=-0.7, flap=2.0, breath=1),
        _pose(sit=0.6, tail=0.4, head=(-0.4, -1.0, -0.4), jaw=0.2, hat=-0.25, ear=-0.2),
    ], False, 8.0),
]


# =============================================================================================
# THE GRIFFIN. Level 8: the familiar takes wing. An eagle's head and forequarters in gold, a lion's
# haunch in the hound's own fawn, and the wizard's indigo for wings, edged in gold - the hat stays
# on. Designed on the 64 cell, facing right. It never touches the ground, so its contact shadow
# sits detached on the bottom rows and shrinks as it climbs (section 4's Flyer row).
# =============================================================================================

GRIFF = dict(fur=FAWN, feather=GOLD, wing=WOOL, frost=False)
GRIFF_RIME = dict(fur=FROST, feather=[ICE[0], ICE[1], FROST[1], FROST[2], FROST[3]], wing=FROST, frost=True)


def _gpose(**kw):
    p = dict(wing=-2.0, wing_far=None, alt=0.0, tilt=0.0, beak=0.0, head=(0.0, 0.0, 0.0), talon=0.0,
             tail=0.0, hat=-0.2, flap=0.0, legs=0.0)
    p.update(kw)
    return p


def _wing(g, root, ang, length, chord, ramp, bias, edge_bias):
    """A wing seen side on: a gold leading edge, primaries splayed at the tip, coverts at the root."""
    dx, dy = math.cos(ang), math.sin(ang)
    px, py = -dy, dx
    if px > 0:
        px, py = -px, -py
    # The arm: root to wrist, bowed toward the leading side. The hand carries the primaries.
    wrist = (root[0] + dx * length * 0.46 - px * chord * 0.16, root[1] + dy * length * 0.46 - py * chord * 0.16)
    # The inner wing - coverts and secondaries - with a scalloped trailing edge, three feathers deep.
    inner = [root, wrist]
    for i in range(4):
        t = 0.46 - i * 0.14
        w = chord * (0.95 if i % 2 == 0 else 0.78)
        inner.append((root[0] + dx * length * t + px * w, root[1] + dy * length * t + py * w))
    inner.append((root[0] + px * chord * 0.55, root[1] + py * chord * 0.55))
    g.poly(inner, ramp, bias, "cloth", lean=(-0.15, -0.45))
    # Five primaries, fanned from the hand: the outermost longest and nearest the leading edge.
    for k in range(5):
        f = k / 4.0
        base = (wrist[0] + dx * length * 0.05 * (1 - f), wrist[1] + dy * length * 0.05 * (1 - f))
        a = ang + (0.12 + f * 0.62) * (1 if (px * math.cos(ang + 1.0) + py * math.sin(ang + 1.0)) > (px * math.cos(ang - 1.0) + py * math.sin(ang - 1.0)) else -1)
        ln = length * (0.62 - f * 0.17)
        tip = (base[0] + math.cos(a) * ln, base[1] + math.sin(a) * ln)
        mid = (base[0] + math.cos(a) * ln * 0.55, base[1] + math.sin(a) * ln * 0.55)
        g.limb([base, mid, tip], [2.1, 1.6, 0.5], ramp, bias + 0.15 * (k % 2), "cloth")
    g.limb([root, wrist, (wrist[0] + dx * length * 0.2, wrist[1] + dy * length * 0.2)], [0.9, 0.8, 0.5], GOLD, edge_bias, "norim")


def griffin(look, p):
    cell = 64
    g = Fig(cell)
    fur, feather, wingm = look["fur"], look["feather"], look["wing"]
    g.deeper[id(fur)] = FUR_DEPTH
    alt = p["alt"]
    tilt = p["tilt"]
    pivot = (31.0, 30.0)

    def T(x, y):
        rx, ry = rot(x - pivot[0], y - pivot[1], tilt)
        return (pivot[0] + rx, pivot[1] + ry - alt)

    wf = p["wing_far"] if p["wing_far"] is not None else p["wing"] + 0.18
    # Far wing, far legs, tail: behind the body and a stop darker.
    _wing(g, T(34.0, 26.0), wf + tilt, 26.0, 10.0, wingm, 1.0, 0.8)
    ta = p["tail"]
    tp = [T(15.0, 31.0)]
    for i, (dx, dy) in enumerate(((-4.0, 1.5), (-4.0, 1.0), (-3.0, -1.0), (-1.5, -2.5))):
        rx, ry = rot(dx, dy, ta * (0.4 + i * 0.25))
        tp.append((tp[-1][0] + rx, tp[-1][1] + ry))
    g.limb(tp, [1.6, 1.3, 1.1, 0.9, 0.8], fur, 0.3, "fur")
    g.blob(tp[-1][0], tp[-1][1] - 0.5, 2.2, 1.8, feather, 0.3, "fur")
    legs = p["legs"]
    talon = p["talon"]

    def hind(bias, off):
        hip = T(18.0 + off, 35.0)
        knee = T(22.0 + off - legs * 2.0, 39.5 + legs)
        paw = T(16.5 + off - legs * 3.0, 42.0 + legs * 0.5)
        g.limb([hip, knee, paw], [3.0, 1.8, 1.4], fur, bias, "fur")
        g.blob(paw[0] - 0.5, paw[1] + 0.3, 1.9, 1.1, fur, bias + 0.3, "body")

    def fore(bias, off):
        sh = T(36.0 + off, 34.0)
        kn = T(38.5 + off + talon * 3.0, 38.5 - talon * 0.5)
        ft = T(40.5 + off + talon * 6.0, 41.0 - talon * 1.0)
        g.limb([sh, kn, ft], [2.4, 1.4, 1.1], feather, bias, "fur")
        for (ex, ey) in ((1.6, 1.4), (2.4, 0.2), (0.4, 1.9)):
            c = STONE[4] if bias > 0.5 else STONE[3]
            g.dot(ft[0] + ex * (0.6 + talon * 0.5), ft[1] + ey * (0.8 - talon * 0.3), c)
            g.dot(ft[0] + ex * (0.6 + talon * 0.5) + 0.6, ft[1] + ey * (0.8 - talon * 0.3) + 0.8, OCC)

    hind(1.2, 2.0)
    fore(1.2, 2.0)
    # The body: a lion's haunch, the barrel, an eagle's breast.
    g.blob(*T(19.0, 31.5), rx=7.0, ry=6.0, ramp=fur, bias=0.0, kind="fur")
    g.limb([T(22.0, 30.0), T(28.0, 30.5), T(32.0, 30.5)], [5.6, 5.0, 5.8], fur, 0.0, "fur")
    g.blob(*T(35.0, 30.0), rx=7.0, ry=6.6, ramp=feather, bias=-0.05, kind="fur")
    hind(0.0, 0.0)
    fore(0.0, 0.0)
    # Neck and head, eagle: a hooked amber beak, a dark brow over an amber eye.
    hdx, hdy, ha = p["head"]
    hc = (44.0 + hdx, 20.5 + hdy)
    g.limb([T(37.0, 26.0), T(41.0, 23.0), T(hc[0] - 1.0, hc[1] + 1.0)], [5.0, 4.2, 3.6], feather, -0.1, "fur")

    def H(dx, dy):
        rx, ry = rot(dx, dy, ha)
        return T(hc[0] + rx, hc[1] + ry)

    g.blob(*H(0.0, 0.0), rx=4.6, ry=4.0, ramp=feather, bias=-0.15, kind="fur", angle=ha + tilt)
    beak = p["beak"]
    by = beak * 2.2
    g.poly([H(2.6, 1.8 + by * 0.2), H(6.6, 2.2 + by), H(5.6, 3.6 + by), H(3.0, 3.0 + by * 0.6)], AMBER, 0.6, "body")
    g.poly([H(2.6, -1.0), H(6.6, -0.4), H(8.6, 1.0), H(8.4, 3.0), H(7.2, 2.0), H(3.0, 1.8)], AMBER, -0.1, "body")
    g.dot(*H(8.2, 3.2), colour=AMBER[3])
    if beak > 0.3:
        g.dot(*H(5.0, 2.6 + by * 0.7), colour=EMBER[1])
    g.dot(*H(1.2, -2.2), colour=OCC)
    g.dot(*H(2.2, -2.0), colour=OCC)
    g.dot(*H(3.2, -1.6), colour=feather[4])
    g.dot(*H(1.6, -1.0), colour=AMBER[1])
    g.dot(*H(2.4, -1.0), colour=AMBER[0])
    g.dot(*H(1.6, -0.2), colour=OCC)
    # The collar and its star, on the gold of the neck.
    g.limb([T(37.6, 24.6), T(40.6, 26.4)], [1.1, 1.0], WOOL, -0.1, "body")
    g.dot(*T(40.0, 28.4), colour=GOLD[0])
    g.dot(*T(40.0, 29.4), colour=LIGHT_E[0] if not look["frost"] else ICE[0])
    # The near wing, over everything but the head.
    _wing(g, T(32.0, 26.5), p["wing"] + tilt, 29.0, 11.0, wingm, 0.0, -0.2)

    def Hh(dx, dy):
        return H(dx * 1.25 - 0.8, dy * 1.25 - 3.4)

    _hat(g, Hh, lambda r: r * 1.3, dict(hat=p["hat"], flap=p["flap"]))
    if look["frost"] and p["beak"] > 0.3:
        for i, (dx, dy) in enumerate(((9.6, 2.0), (11.0, 1.4), (12.2, 2.6))):
            g.dot(*H(dx, dy), colour=ICE[i % 2], kind="glow")
    # The detached shadow: on the ground, never touching the bird, narrower the higher it is.
    lift = 62.0 - (44.0 - alt)
    half = max(5.0, 14.0 - lift * 0.35)
    g.contact(31.0 - half, 31.0 + half, row=62)
    g.contact(31.0 - half + 2, 31.0 + half - 2, row=63)
    return g


def _wcycle(k, n, lo=-2.05, hi=2.55):
    """A wingbeat: up (pointing up and back), back, down and back, and round again."""
    t = k / float(n)
    return lo + (hi - lo) * (0.5 - 0.5 * math.cos(2 * math.pi * t))


GRIFFIN_ANIMS = [
    ("idle", [_gpose(wing=_wcycle(k, 4), alt=[0.0, -1.0, -2.0, -1.0][k], tail=math.sin(k * 1.57) * 0.3, flap=k)
              for k in range(4)], True, 7.0),
    ("run", [_gpose(wing=_wcycle(k, 6, -1.95, 2.7), alt=[0.0, -1.0, -2.0, -2.0, -1.0, 0.0][k], tilt=0.12,
                    tail=-0.3, hat=-0.45, flap=k, legs=1.0) for k in range(6)], True, 12.0),
    ("pounce", [
        _gpose(wing=-1.95, alt=1.0, tilt=-0.05, hat=-0.3, legs=0.5),
        _gpose(wing=2.95, alt=-4.0, tilt=0.38, hat=-0.6, legs=1.0, tail=-0.5, head=(0.5, 1.0, 0.2)),
        _gpose(wing=3.05, alt=-9.0, tilt=0.45, hat=-0.7, talon=0.6, legs=1.0, tail=-0.6, head=(0.8, 1.5, 0.25), beak=0.6),
        _gpose(wing=-1.7, alt=-10.0, tilt=0.1, hat=-0.4, talon=1.0, beak=1.0, tail=-0.3, head=(1.0, 0.5, 0.1)),
        _gpose(wing=2.4, alt=-6.0, tilt=-0.2, hat=-0.2, talon=0.4, beak=0.4, legs=0.6, head=(0.0, -0.5, -0.15)),
        _gpose(wing=-2.0, alt=-2.0, tilt=-0.08, hat=-0.25, legs=0.6),
    ], False, 14.0),
    ("howl", [
        _gpose(wing=-2.0),
        _gpose(wing=-1.8, alt=-1.0, head=(-0.5, -1.0, -0.2), beak=0.2),
        _gpose(wing=-1.55, alt=-2.0, head=(-1.0, -2.0, -0.45), beak=0.7, hat=-0.45, flap=1),
        _gpose(wing=-1.45, wing_far=-1.1, alt=-2.5, head=(-1.2, -2.4, -0.55), beak=1.0, hat=-0.55, flap=2, tail=0.4),
        _gpose(wing=-1.5, wing_far=-1.15, alt=-2.0, head=(-1.2, -2.2, -0.5), beak=0.8, hat=-0.5, flap=3, tail=0.3),
        _gpose(wing=-1.9, alt=-1.0, head=(-0.4, -0.8, -0.15), beak=0.2),
    ], False, 8.0),
]


# =============================================================================================
# THE CROW. The familiar taken as a crow (Eric: "crow moves faster"): the fastest of the three, a flier
# that ranges far, stoops on its quarry, pecks and rises off it. Drawn the way the griffin is - on the
# wing, high in its cell, with its shadow detached on the bottom rows (section 4's Flyer row), facing
# right - and kept the wizard's by the same three things as the hound: HIS HAT on its head, a gold collar
# and star, and a warm eye. What keeps it from ever reading as the Fallow Fields' Harvest Rook: that is a
# rook the size of a dog in stone grey with a pale bone beak, and this is indigo - the wizard's wool -
# going near-black in its shade, lit along its wings in gold, with an amber beak and the hat.
#
# Designed on the 48 cell (s scales it about its own centre for the 64-cell looks), facing right.
# =============================================================================================

VIOLET = M["violet"]
LT = E["lightning"]
STORM_EDGE = [LT[0], LT[1], LT[2], LT[3], AMBER[3]]
# The plumage: the wizard's indigo in the light, going to the violet near-black in its shade - a crow's
# blue sheen on black, in his colours.
CROW_FEATHER = [WOOL[2], WOOL[3], VIOLET[2], VIOLET[3], VIOLET[4]]
# The Stormcrow's: a storm-cloud blue a stop paler, lit along every edge in lightning.
STORM_FEATHER = [WOOL[0], WOOL[1], WOOL[2], WOOL[3], VIOLET[3]]
# The Morrigan's: the violet row whole, darker and redder than the crow - and ember eyes.
MORRIGAN_FEATHER = [VIOLET[0], VIOLET[1], VIOLET[2], VIOLET[3], VIOLET[4]]


class Bird(object):
    """What a crow is made of: its plumage, the edge its wings are lit along, its eye, and its size."""

    def __init__(self, feather, edge=GOLD, eye=(AMBER[0], AMBER[1]), glow_eye=False, s=1.0, ruff=False,
                 crest=False, storm=False, beak=AMBER, depth=0.3):
        self.feather = feather
        self.edge = edge
        self.eye = eye
        self.glow_eye = glow_eye
        self.s = s
        # A raven's shaggy throat; the Morrigan's crest of ragged feathers.
        self.ruff = ruff
        self.crest = crest
        # Lightning in its feathers: sparks thrown off the wings and the beak.
        self.storm = storm
        self.beak = beak
        self.depth = depth


# How much lower than its design a crow flies in its cell (crow()): enough that the biggest keeps its hat in.
FLY_DROP = 7.0
CROW = Bird(CROW_FEATHER, s=1.25, depth=0.1)
STORMCROW = Bird(STORM_FEATHER, edge=STORM_EDGE, eye=(LT[0], LT[1]), glow_eye=True, storm=True, s=1.25, depth=0.05)
RAVEN = Bird(CROW_FEATHER, s=1.45, ruff=True, depth=0.12)
MORRIGAN = Bird(MORRIGAN_FEATHER, eye=(EMBER[0], EMBER[1]), glow_eye=True, s=1.6, ruff=True, crest=True, depth=0.05)
MORRIGAN_STORM = Bird(STORM_FEATHER, edge=STORM_EDGE, eye=(LT[0], LT[1]), glow_eye=True, s=1.6, ruff=True, crest=True,
                      storm=True, depth=0.05)


def _cpose(**kw):
    p = dict(wing=-1.9, wing_far=None, alt=0.0, tilt=0.0, beak=0.0, head=(0.0, 0.0, 0.0), talon=0.0,
             tail=0.0, hat=-0.2, flap=0.0, fold=0.0, sparks=0)
    p.update(kw)
    return p


def crow(cell, bird, p):
    """One crow frame on `cell`. p: a _cpose. Its ground point is the cell's bottom-centre (where the game
    stands it), so its shadow is drawn there and the bird well above."""
    g = Fig(cell)
    g.deeper[id(bird.feather)] = bird.depth
    s = bird.s
    alt = p["alt"]
    tilt = p["tilt"]
    # The bird's middle sits at (24, 19) of the 48 design cell, scaled about the cell's ground point, so a
    # bigger bird rises as well as grows and its shadow stays on the floor.
    gx, gy = cell / 2.0, cell - 2.0
    pivot = (24.0, 19.0)

    # It flies FLY_DROP design units lower than it was designed, so a bigger bird keeps its hat in the cell.
    def T(x, y):
        rx, ry = rot(x - pivot[0], y - pivot[1], tilt)
        return (gx + (pivot[0] - 24.0 + rx) * s, gy + (pivot[1] - 46.0 + ry - alt + FLY_DROP) * s)

    def R(r):
        return r * s

    fe = bird.feather
    wf = p["wing_far"] if p["wing_far"] is not None else p["wing"] + 0.2
    fold = p["fold"]
    # The far wing, a stop darker, behind everything. Folded for the stoop, it lies back along the body.
    _wing(g, T(23.0, 16.5), wf + tilt, R(18.0 - fold * 7.0), R(7.2 - fold * 2.4), fe, 1.0, 0.8)
    if bird.edge is not GOLD:
        pass
    # The tail: a wedge of long feathers fanned behind, lifting with `tail`.
    ta = p["tail"]
    root = (15.5, 20.0)
    for k, (dx, dy) in enumerate(((-7.0, 2.6), (-7.6, 1.0), (-7.2, -0.6))):
        rx, ry = rot(dx, dy, ta * (0.6 + 0.2 * k))
        tip = (root[0] + rx, root[1] + ry)
        g.limb([T(*root), T(root[0] + rx * 0.55, root[1] + ry * 0.55), T(*tip)], [R(1.6), R(1.4), R(0.7)], fe, 0.35 + 0.1 * k, "cloth")
    # The talons: tucked up under the body in flight, reaching down to strike.
    talon = p["talon"]
    for (ox, bias) in ((1.2, 1.0), (0.0, 0.2)):
        hip = (21.0 + ox, 22.0)
        foot = (22.5 + ox + talon * 3.0, 23.6 + talon * 4.0)
        g.limb([T(*hip), T(*foot)], [R(1.0), R(0.7)], STONE, bias, "body")
        fx, fy = T(*foot)
        g.dot(fx + 1, fy + 1, AMBER[2] if bias < 0.5 else AMBER[3])
        g.dot(fx - 1, fy + 1, OCC)
    # The body: a long tapered back, the breast deep and forward.
    g.limb([T(15.5, 19.6), T(20.0, 19.4), T(25.5, 18.6)], [R(3.0), R(4.2), R(4.4)], fe, 0.0, "fur")
    g.blob(*T(26.5, 19.0), rx=R(4.4), ry=R(4.0), ramp=fe, bias=-0.1, kind="fur")
    if bird.ruff:
        # A raven's shaggy throat: hackles standing off the breast under the beak.
        for (bx, by, ln) in ((29.0, 17.0, 2.6), (28.0, 19.5, 2.2), (26.6, 21.4, 1.8)):
            g.poly([T(bx - 1.0, by - 0.8), T(bx + 0.6, by - 0.6), T(bx + ln * 0.4, by + ln)], fe, 0.1, "fur", lean=(-0.2, -0.4))
    # The collar, and its star.
    g.limb([T(27.4, 15.4), T(29.6, 17.6)], [R(0.85), R(0.8)], GOLD, -0.2, "body")
    sx, sy = T(29.4, 19.0)
    g.dot(sx, sy, GOLD[0])
    g.dot(sx, sy + 1, LIGHT_E[0] if not bird.storm else LT[0])
    # The head, carried forward of the breast, turned by `head`.
    hdx, hdy, ha = p["head"]
    hc = (30.6 + hdx, 14.2 + hdy)

    def H(dx, dy):
        rx, ry = rot(dx, dy, ha)
        return T(hc[0] + rx, hc[1] + ry)

    if bird.crest:
        # The Morrigan's crest: ragged feathers raised off the crown and the nape.
        for (bx, by, a, ln) in ((-1.6, -2.4, -2.3, 3.6), (-0.4, -3.0, -1.9, 3.2), (-2.6, -1.0, -2.7, 3.0)):
            tip = (bx + math.cos(a) * ln, by + math.sin(a) * ln)
            g.limb([H(bx, by), H(*tip)], [R(0.9), R(0.35)], fe, 0.3, "fur")
    g.blob(*H(0.0, 0.0), rx=R(3.5), ry=R(3.1), ramp=fe, bias=-0.15, kind="fur", angle=ha + tilt)
    # The beak: heavy, straight, a crow's, opening on `beak`.
    bk = p["beak"]
    by = bk * 1.6
    g.poly([H(2.0, 1.0), H(6.2, 1.6 + by), H(5.4, 2.6 + by), H(2.2, 2.4)], bird.beak, 0.55, "body")
    g.poly([H(2.0, -1.2), H(5.2, -0.4), H(7.4, 0.8), H(6.6, 1.6), H(2.4, 1.2)], bird.beak, -0.05, "body")
    if bk > 0.3:
        g.dot(*H(4.2, 1.6 + by * 0.6), colour=EMBER[1])
    # The eye: a warm point (or a lit one) under a dark brow.
    g.dot(*H(0.8, -1.6), colour=fe[4])
    g.dot(*H(1.8, -1.4), colour=fe[4])
    if bird.glow_eye:
        g.dot(*H(1.0, -0.6), colour=bird.eye[1], kind="glow")
        g.dot(*H(1.8, -0.6), colour=bird.eye[0], kind="glow")
    else:
        # Warm, and no near-white point in it: the calm eye (calm_eye), a stop brighter for a sprite.
        g.dot(*H(1.0, -0.6), colour=OCC)
        g.dot(*H(1.8, -0.6), colour=bird.eye[1])
        g.dot(*H(1.8, 0.3), colour=calm_eye(bird.eye)[0])
    # The near wing over everything but the head.
    _wing(g, T(22.6, 16.6), p["wing"] + tilt, R(20.0 - fold * 8.0), R(8.0 - fold * 2.8), fe, 0.0, -0.2)
    if bird.edge is not GOLD:
        # The Stormcrow's leading edge is lightning, not gold: laid over the gold the wing drew.
        dx, dy = math.cos(p["wing"] + tilt), math.sin(p["wing"] + tilt)
        L = R(20.0 - fold * 8.0)
        r0 = T(22.6, 16.6)
        g.limb([r0, (r0[0] + dx * L * 0.46, r0[1] + dy * L * 0.46), (r0[0] + dx * L * 0.66, r0[1] + dy * L * 0.66)],
               [R(0.8), R(0.75), R(0.5)], bird.edge, -0.3, "norim")

    # _hat seats the hat on the crown of a hound's skull; a crow's is smaller and rounder, so it sits a
    # little lower and further back.
    def Hh(dx, dy):
        return H(dx * 0.95 - 0.5, dy * 0.95 + 0.4)

    _hat(g, Hh, lambda r: r * 0.95 * s, dict(hat=p["hat"], flap=p["flap"]))
    if bird.storm and p["sparks"]:
        # Lightning thrown off it: a few hot points off the wingtip and the beak.
        for i, (dx, dy) in enumerate(((7.8, 0.2), (9.0, -1.0), (8.4, 1.8))[:p["sparks"]]):
            g.dot(*H(dx, dy), colour=LT[i % 2], kind="glow")
    # The detached shadow: on the floor, never touching the bird, narrower the higher it flies.
    height = (26.0 - FLY_DROP) * s + alt * s
    half = max(3.0, (10.0 - height * 0.12) * s)
    g.contact(cell / 2.0 - half, cell / 2.0 + half, row=cell - 2)
    g.contact(cell / 2.0 - half + 2, cell / 2.0 + half - 2, row=cell - 1)
    return g


def _beat(k, n, lo=-2.0, hi=2.45):
    """A wingbeat: up and back, down and back, and round."""
    t = k / float(n)
    return lo + (hi - lo) * (0.5 - 0.5 * math.cos(2 * math.pi * t))


CROW_ANIMS = [
    # Hovering at the wizard's shoulder: slow beats, a little bob.
    ("idle", [_cpose(wing=_beat(k, 4), alt=[0.0, 1.0, 1.6, 1.0][k], tail=math.sin(k * 1.57) * 0.15, flap=k)
              for k in range(4)], True, 8.0),
    # On the wing: quicker beats, pitched forward, the hat swept back.
    ("run", [_cpose(wing=_beat(k, 6, -2.1, 2.6), alt=[0.0, 1.0, 1.6, 1.4, 0.6, 0.0][k], tilt=0.14, tail=-0.15,
                    hat=-0.45, flap=k, head=(0.4, 0.4, 0.06)) for k in range(6)], True, 14.0),
    # The stoop, set frame by frame by Familiar.cs: 0 wings raised to fold, 1-3 the dive (folded, pitched
    # down, falling), 4 the peck (beak driven in at the quarry's middle, wings thrown open to brake), 5 off.
    ("pounce", [
        _cpose(wing=-2.4, wing_far=-2.2, alt=1.0, tilt=-0.1, hat=-0.3),
        _cpose(wing=-2.75, wing_far=-2.6, fold=0.8, alt=-3.0, tilt=0.45, hat=-0.6, tail=-0.3, head=(0.6, 0.8, 0.25)),
        _cpose(wing=-2.85, wing_far=-2.7, fold=1.0, alt=-7.0, tilt=0.65, hat=-0.7, tail=-0.4, head=(0.8, 1.2, 0.35), beak=0.4,
               sparks=2),
        _cpose(wing=-2.9, wing_far=-2.75, fold=1.0, alt=-10.0, tilt=0.75, hat=-0.75, tail=-0.4, head=(0.9, 1.6, 0.45),
               beak=0.8, talon=0.6, sparks=3),
        _cpose(wing=2.1, wing_far=2.3, alt=-11.0, tilt=0.55, hat=-0.5, talon=1.0, beak=1.0, head=(0.8, 1.8, 0.5),
               tail=0.4, sparks=3),
        _cpose(wing=-1.8, alt=-6.0, tilt=0.0, hat=-0.25, talon=0.4, beak=0.2, tail=0.2, sparks=1),
    ], False, 14.0),
    # The caw on summoning and on a level: head thrown up, beak wide, wings half open and beating.
    ("howl", [
        _cpose(wing=-1.9),
        _cpose(wing=-1.5, alt=0.6, head=(-0.4, -0.8, -0.3), beak=0.3),
        _cpose(wing=-1.0, wing_far=-0.8, alt=1.2, head=(-0.8, -1.6, -0.6), beak=0.9, hat=-0.45, flap=1, sparks=2),
        _cpose(wing=-2.3, wing_far=-2.1, alt=1.6, head=(-0.9, -1.8, -0.7), beak=1.0, hat=-0.55, flap=2, tail=0.3, sparks=3),
        _cpose(wing=-1.2, wing_far=-1.0, alt=1.2, head=(-0.8, -1.6, -0.6), beak=0.7, hat=-0.5, flap=3, sparks=1),
        _cpose(wing=-1.9, alt=0.4, head=(-0.3, -0.6, -0.2), beak=0.2),
    ], False, 8.0),
]

# The griffin after the crow's Lightning path: its eagle half lit in storm, its wings the storm's blue.
GRIFF_STORM = dict(fur=FAWN, feather=[LT[0], LT[1], AMBER[1], AMBER[2], AMBER[3]], wing=STORM_FEATHER, frost=False)


# =============================================================================================
# THE CAT. The familiar taken as a cat (Eric: "cat gives passive bonuses"): it stays underfoot and makes
# the wizard better while it is out, and its own swipe is the weakest of the three. A cat on the hound's
# skeleton (hound() with Skin.feline): low and long on short legs, a round head with pointed ears, a long
# tail carried up and curling over - the silhouette that says cat before anything else does - and the
# wizard's hat sat back between its ears, the gold collar and the warm eye that make it his. Its coat is
# the hound's warm fawn with a tabby's bands across it (Fig.pattern), so it reads as the same family.
# =============================================================================================

CAT_BREATH = (EMBER[0], BONE[0], EMBER[1])
CAT = Skin(FAWN, feline=True, pattern="stripes", leap=1.0, head_scale=1.45, breath=None)
# The Cait Sith: the fairy cat, black with a white star on its breast, its eyes lit with Darkness.
DK = E["darkness"]
SITH_FUR = [VIOLET[1], VIOLET[2], VIOLET[3], VIOLET[4], WOOL[4]]
CAIT_SITH = Skin(SITH_FUR, feline=True, eye=(DK[0], DK[1]), glow_eye=True, star=BONE[0], leap=1.0, head_scale=1.45,
                 depth=0.25, breath=(DK[1], DK[2], DK[0]))
# Grimalkin: the old witch's cat, grown - grizzled, heavier, in the wizard's mantle as the Great Hound is.
GRIM_FUR = [FAWN[1], FAWN[2], M["skin"][2], FAWN[3], FAWN[4]]
GRIMALKIN = Skin(GRIM_FUR, feline=True, pattern="stripes", mantle=True, heavy=0.6, leap=0.8, head_scale=1.3)
# Cath Palug: the monstrous cat of the Welsh tale - a great spotted cat in gold, its eyes lit ember, in the
# mantle; after the Cait Sith a black panther with the fairy cat's lit eyes.
PALUG_FUR = [GOLD[1], GOLD[2], FAWN[2], GOLD[3], GOLD[4]]
CATH_PALUG = Skin(PALUG_FUR, feline=True, pattern="spots", mantle=True, heavy=1.1, leap=0.6, head_scale=1.2,
                  eye=(EMBER[0], EMBER[1]), glow_eye=True, depth=0.25, breath=(EMBER[1], EMBER[0], EMBER[2]))
CATH_PALUG_SITH = Skin(SITH_FUR, feline=True, mantle=True, heavy=1.1, leap=0.6, head_scale=1.2, eye=(DK[0], DK[1]),
                       glow_eye=True, star=BONE[0], depth=0.3, breath=(DK[1], DK[2], DK[0]))


def _cat_anims():
    """The hound's strips posed for a cat: it sits at heel (idle), bounds (run), wiggles before it
    pounces and swipes (pounce), and sits up to yowl (howl)."""
    idle = [_pose(sit=1.0, tail=0.2 + 0.25 * math.sin(k * 1.57), breathe=[0.0, 0.6, 1.0, 0.5][k], hat=-0.08, flap=k * 0.6,
                  head=(-0.2, 0.2, -0.05))
            for k in range(4)]
    run = []
    for k in range(6):
        q = gallop(k)
        q["tail"] = -0.25 + math.sin(2 * math.pi * k / 6.0) * 0.12
        q["head"] = (0.6, 1.4, 0.06)
        run.append(q)
    pounce = [
        _pose(crouch=2.4, swing=(0.6, 0.5, 0.9, 0.8), head=(0.8, 2.6, 0.1), tail=0.5, hat=-0.3, ear=0.4),
        _pose(air=2.0, stretch=2.2, swing=(1.4, 1.2, -1.2, -1.0), lift=(2.4, 2.0, 1.0, 0.6), head=(1.4, 1.2, 0.05),
              jaw=0.4, tail=-0.4, hat=-0.5, ear=-0.6, flap=1.0),
        _pose(air=5.0, stretch=2.6, swing=(1.6, 1.4, -1.3, -1.2), lift=(1.6, 1.2, 0.4, 0.2), head=(1.8, 1.0, 0.1),
              jaw=0.8, tail=-0.6, hat=-0.6, ear=-0.8, flap=2.0),
        _pose(air=2.4, stretch=2.0, swing=(1.6, 1.3, -1.0, -0.9), lift=(1.8, 0.4, 0.6, 0.4), head=(1.8, 2.0, 0.2),
              jaw=0.9, tail=-0.4, hat=-0.45, ear=-0.6, flap=3.0),
        _pose(crouch=1.0, stretch=-0.4, swing=(1.4, 0.6, 0.2, 0.0), lift=(2.6, 0.0, 0.0, 0.0), head=(1.6, 3.0, 0.25),
              jaw=0.5, tail=0.0, hat=0.1, ear=0.2, flap=4.0, breath=1),
        _pose(crouch=0.4, swing=(0.6, 0.4, 0.0, -0.1), head=(1.0, 2.0, 0.05), jaw=0.1, tail=0.2, hat=-0.2, flap=5.0),
    ]
    howl = [
        _pose(sit=1.0, tail=0.2, hat=-0.08),
        _pose(sit=1.0, tail=0.4, head=(-0.2, -0.4, -0.15), hat=-0.12),
        _pose(sit=1.0, tail=0.6, head=(-0.4, -0.8, -0.4), jaw=0.5, hat=-0.3, ear=-0.2, breath=1),
        _pose(sit=1.0, tail=0.7, head=(-0.6, -1.0, -0.6), jaw=1.0, hat=-0.5, ear=-0.4, flap=1.0, breath=1),
        _pose(sit=1.0, tail=0.6, head=(-0.6, -1.0, -0.55), jaw=0.8, hat=-0.5, ear=-0.4, flap=2.0, breath=1),
        _pose(sit=1.0, tail=0.3, head=(-0.2, -0.4, -0.15), jaw=0.2, hat=-0.2),
    ]
    return [("idle", idle, True, 5.0), ("run", run, True, 13.0), ("pounce", pounce, False, 14.0), ("howl", howl, False, 8.0)]


CAT_ANIMS = _cat_anims()


# =============================================================================================
# THE HOUND OF ANNWN. The dog's untagged Ascension: the white hound of the Welsh otherworld, red-eared,
# leaner than the Great Hound and as tall - the pack that hunts the strongest thing on the field. Its
# coat is the bone row's pale end (lit, never the reserved rim colour), its ears the ember row, its eyes
# lit; after the Ice path, ears and eyes of ice and frost on its breath.
# =============================================================================================

ANNWN_FUR = [M["skin"][0], M["skin"][1], M["skin"][2], M["skin"][3], WOOL[3]]
ANNWN_EAR = [EMBER[1], EMBER[2], EMBER[3], M["red"][3], M["red"][4]]
ANNWN = Skin(ANNWN_FUR, ear=ANNWN_EAR, eye=(EMBER[0], EMBER[1]), glow_eye=True, heavy=0.5, leap=0.6, head_scale=1.05,
             depth=0.1, breath=(EMBER[1], EMBER[0], EMBER[2]))
ANNWN_RIME = Skin(ANNWN_FUR, ear=[ICE[1], ICE[2], ICE[3], FROST[3], FROST[4]], eye=(ICE[0], ICE[1]), glow_eye=True,
                  heavy=0.5, leap=0.6, head_scale=1.05, depth=0.1, frost=True)


# =============================================================================================
# THE PACK UNDER AN ASCENSION, AND THE PUPS. Eric, 2026-10-30, of 0.9.2: "when ascending hound familiar,
# it should only make the main hound into the full size legendary creature. littermates should produce
# two puppies instead of 2 of the same dog. The two puppies should also be affected by the ascension, but
# the sprite should just look like a similar-styled hellhound with one head, instead of the 3 headed
# cerberus. same goes for the other ascension."
#
# Every hound used to wear the one look the spell resolved to, so a Littermates pack that took Cerberus
# was three Cerberuses. Now the FIRST hound is the main one (scripts/Familiar.cs, Hound.Look) and wears
# the full form; the others wear the PACK look:
#
#   with Littermates  - two PUPS: younger, smaller hounds on the 32 cell, a big head on a round body and
#                       short legs, with no hat (the hat is the main hound's). Before level 8 they are the
#                       fawn hound's pups; under Cerberus a one-headed HELLPUP in its hide, lit eyes and
#                       embers; under the Hound of Annwn a white pup with red ears.
#   without it        - the second hound (the level 3 one) is the hound at its own 48-cell size in the
#                       Ascension's look with ONE head: a hellhound, or a white red-eared hound, in frost
#                       after the Ice path. It keeps the hat - it is still the wizard's hound, just not the
#                       legendary one.
#
# Every one of them bites with the Ascension's power - Cerberus's three bites, Annwn's double on an elite -
# because those are the pack's (Familiar.Summon reads them once for every hound); only the drawing differs.
# =============================================================================================

# The second hound, adult and one-headed, under an Ascension: the hound's proportions (48 cell, 1.3), the
# legendary's coat.
HELL_PACK = Skin(HELL, eye=(EMBER[0], EMBER[1]), leap=0.85, head_scale=1.15, depth=0.6, glow_eye=True, embers=True)
HELL_PACK_RIME = Skin(RIME_DEEP, eye=(ICE[0], ICE[1]), frost=True, leap=0.85, head_scale=1.15, glow_eye=True)
ANNWN_PACK = Skin(ANNWN_FUR, ear=ANNWN_EAR, eye=(EMBER[0], EMBER[1]), glow_eye=True, leap=0.85, head_scale=1.15,
                  depth=0.1, breath=(EMBER[1], EMBER[0], EMBER[2]))
ANNWN_PACK_RIME = Skin(ANNWN_FUR, ear=[ICE[1], ICE[2], ICE[3], FROST[3], FROST[4]], eye=(ICE[0], ICE[1]), glow_eye=True,
                       leap=0.85, head_scale=1.15, depth=0.1, frost=True)


class Pup(object):
    """What a pup is made of. Drawn by pup(), a body plan of its own: a puppy is not the hound scaled down
    (that is a small sighthound), it is a big head and big paws on a round body with short legs."""

    def __init__(self, fur, ear=None, eye=(AMBER[0], AMBER[1]), glow_eye=False, embers=False, depth=FUR_DEPTH,
                 breath=None, ear_tip=None, collar="star", shaggy=False):
        self.fur = fur
        # The ears' ramp, where it is not the coat's; and a colour for the last pixel of each ear (Annwn's red).
        self.ear = ear
        self.ear_tip = ear_tip
        self.eye = eye
        self.glow_eye = glow_eye
        self.embers = embers
        self.depth = depth
        self.breath = breath
        # "star": the gold collar and the wizard's star, as every familiar wears. "tag": a dark collar with a
        # round silver tag on it - Pancho's own (PANCHO).
        self.collar = collar
        # Pancho's body plan over the pup's (PANCHO): a long coat falling to the ground, a plumed tail curled
        # over the back, a shaggy face with a fringe over the eyes and a beard over the muzzle, dark round
        # eyes, long fringed ears and the underbite with its teeth showing.
        self.shaggy = shaggy


PUP = Pup(FAWN)
HELL_PUP = Pup(HELL, eye=(EMBER[0], EMBER[1]), glow_eye=True, embers=True, depth=0.55,
               breath=(EMBER[1], EMBER[0], EMBER[2]))

# PANCHO (Eric, 2026-10-30: "when you use littermates, and then select the white dog option, the two puppies
# pixel art should be styled like my dog pancho"). The white dog option is the Hound of Annwn - "a white hound
# of the otherworld, red-eared" - so Littermates and then Annwn makes the two pups Pancho: a small long-haired
# white-and-cream dog of the Shih Tzu / Lhasa kind, from Eric's photo. What says Pancho at 32 pixels, in the
# order the eye takes it: the PLUME of a tail curled up over his back (the silhouette - no hound or pup has
# one), the LONG COAT falling nearly to the ground with the paws peeping out under its hem, the big round
# shaggy head with a fringe over the eyes and a beard over the muzzle, the long floppy EARS in buff, the dark
# ROUND EYES and the black nose, the UNDERBITE - the lower jaw out past the nose with two teeth showing - and
# the dark collar with its round silver tag. His coat is cream, lit white where the cold key falls on it, its
# shadows turning toward indigo as the contract asks (bone's cool lower stops), never a grey cream.
#
# THE ASCENSION'S TOUCH: the Hound of Annwn is white and red-eared, so Pancho keeps his buff ears with their
# fringes dipped red at the ends, and breathes the hound's embers when he bites and howls. His eyes stay his
# own - dark and round - because they are the most Pancho thing about his face; the main hound carries the
# lit eyes.
PANCHO_FUR = [BONE[0], FAWN[0], FAWN[0], BONE[2], BONE[3]]
PANCHO_EAR = [FAWN[0], FAWN[0], FAWN[1], FAWN[2], FAWN[3]]
PANCHO = Pup(PANCHO_FUR, ear=PANCHO_EAR, ear_tip=EMBER[2], eye=(OCC, M["violet"][4]), depth=0.05,
             breath=(EMBER[1], EMBER[0], EMBER[2]), collar="tag", shaggy=True)
ANNWN_PUP = PANCHO


def pup(cell, skin, p):
    """One pup frame on the 32 cell, facing right, paws on row 30. p: a _pose - the pups play the hound's
    own poses (HOUND_ANIMS), so a pup's leap, bite and howl land on the frames Familiar.cs sets."""
    g = Fig(cell)
    g.deeper[id(skin.fur)] = skin.depth
    if skin.embers:
        g.embers = skin.fur
    G = 30.0
    ox = (cell - 32.0) / 2.0
    gy = cell - 2.0

    def T(x, y):
        return (x + ox, y - G + gy)

    fur = skin.fur
    # A pup leaps lower than the hound and bobs less: a short body has less to throw.
    air = p["air"] * 0.7
    rise = air + p["bob"] * 0.8 - p["crouch"] * 0.8
    sit = p["sit"]
    st = p["stretch"] * 0.6
    hx, hy = 11.0 - st * 0.5 + sit * 1.2, 23.0 - rise + sit * 2.6
    cx, cy = 17.2 + st * 0.5, 22.4 - rise - sit * 1.6
    sw, li = p["swing"], p["lift"]

    def front_leg(k, bias):
        sh = (cx + 1.2, cy + 1.4)
        reach = sw[k] * (3.0 if sw[k] > 0 else 2.0)
        paw = (sh[0] + 0.3 + reach, G - li[k] * 0.8 - max(0.0, air - 1.5) * 0.5)
        if sit:
            paw = (sh[0] - 0.2, G)
        d = math.hypot(paw[0] - sh[0], paw[1] - sh[1])
        elbow = ik(sh, paw, d * 0.5 + 0.4, d * 0.5 + 0.4, 1)
        pw = T(*paw)
        g.limb([T(*sh), T(*elbow), pw], [1.9, 1.45, 1.2], fur, bias, "fur")
        g.blob(pw[0] + 0.6, pw[1] + 0.1, 1.7, 1.0, fur, bias + 0.4, "body")
        return pw

    def hind_leg(k, bias):
        hip = (hx - 0.3, hy + 1.4)
        paw = (hip[0] - 0.2 + sw[k] * 2.6, G - li[k] * 0.8 - max(0.0, air - 1.5) * 0.5)
        hock = (paw[0] - 1.0, paw[1] - 1.9)
        if sit:
            paw = (hip[0] + 2.6, G)
            hock = (hip[0] - 0.9, G - 0.6)
        knee = ik(hip, hock, 2.9, 2.5, 1)
        pw = T(*paw)
        g.limb([T(*hip), T(*knee)], [2.3, 1.5], fur, bias, "fur")
        g.limb([T(*knee), T(*hock), pw], [1.4, 1.15, 1.05], fur, bias + 0.15, "fur")
        g.blob(pw[0] + 0.6, pw[1] + 0.1, 1.7, 1.0, fur, bias + 0.4, "body")
        return pw

    paws = []
    # FAR SIDE first: a short tail carried up, the far legs a stop darker. (Pancho's plume is drawn over
    # his back, after the body.)
    ta = p["tail"]
    if not skin.shaggy:
        tp = [(hx - 3.4, hy - 1.6)]
        for i, (dx, dy) in enumerate(((-1.5, -1.5), (-0.7, -1.8), (0.2, -1.5))):
            rx, ry = rot(dx, dy, ta * (0.5 + i * 0.3))
            tp.append((tp[-1][0] + rx, tp[-1][1] + ry))
        g.limb([T(*q) for q in tp], [1.4, 1.15, 0.9, 0.6], fur, 0.2, "fur")
    paws.append(hind_leg(3, 1.25))
    paws.append(front_leg(1, 1.25))

    # THE BODY: round - a haunch, a short barrel, a deep chest.
    breathe = p["breathe"]
    g.blob(*T(hx, hy), rx=4.2, ry=3.9, ramp=fur, bias=0.0, kind="fur")
    g.limb([T(hx + 1.0, hy - 0.3), T((hx + cx) / 2.0, (hy + cy) / 2.0 - 0.4), T(cx - 1.0, cy - 0.2)],
           [3.7, 3.4, 3.8], fur, 0.0, "fur")
    g.blob(*T(cx, cy), rx=4.2, ry=4.1 + breathe * 0.3, ramp=fur, bias=-0.1, kind="fur")
    paws.append(hind_leg(2, 0.0))
    if skin.shaggy:
        # THE PLUME: up off the rump and curled forward over the back, widest where it turns - long hair,
        # so a fat tapering capsule rather than a tail. It wags about its root.
        root = (hx - 3.0, hy - 2.4)
        path = [root]
        for i, (dx, dy) in enumerate(((-1.6, -2.6), (0.6, -2.8), (2.6, -1.0), (2.4, 1.2))):
            rx, ry = rot(dx, dy, ta * 0.18 * (1 + i))
            path.append((path[-1][0] + rx, path[-1][1] + ry))
        g.limb([T(*q) for q in path], [1.3, 2.1, 2.7, 2.5, 1.8], fur, -0.15, "fur")

    # THE HEAD, big for the body and carried up, on a short neck.
    hdx, hdy, ha = p["head"]
    hc = (cx + 4.2 + hdx * 0.7, cy - 6.4 + hdy * 0.7)
    neck0 = (cx + 1.2, cy - 2.2)
    g.limb([T(*neck0), T(hc[0] - 1.2, hc[1] + 2.0)], [2.9, 2.6], fur, -0.05, "fur")
    _pup_collar(g, T, neck0, hc, skin)
    _pup_head(g, T, hc, ha, p, skin)
    paws.append(front_leg(0, 0.0))
    if skin.shaggy:
        # THE LONG COAT, falling from the barrel nearly to the ground over the tops of the legs, its hem
        # ragged, swinging back as he runs: the paws peep out under it.
        sway = st * 0.6 + air * 0.25
        top = [(cx + 3.4, cy + 0.6), ((hx + cx) / 2.0, (hy + cy) / 2.0 + 1.0), (hx - 3.8, hy + 0.2)]
        hem = []
        x0, x1 = hx - 4.4 - sway, cx + 3.0 - sway * 0.5
        n = 8
        for k in range(n + 1):
            x = x0 + (x1 - x0) * k / float(n)
            hem.append((x, min(G - 1.4, (hy + cy) / 2.0 + 5.4) - (0.9 if k % 2 else 0.0) - air * 0.3))
        g.poly([T(*q) for q in top + hem], fur, 0.1, "fur", lean=(-0.1, -0.6))

    xs = [q[0] for q in paws]
    lo, hi = min(xs) - 1.0, max(xs) + 2.0
    if air > 0.5:
        mid_x = (lo + hi) / 2.0
        half = (hi - lo) / 2.0 * max(0.4, 1.0 - air * 0.1)
        lo, hi = mid_x - half, mid_x + half
    g.contact(lo, hi)
    return g


def _pup_collar(g, T, neck0, hc, skin):
    col0 = (neck0[0] + 0.1 + (hc[0] - neck0[0]) * 0.2, neck0[1] - 0.5 + (hc[1] - neck0[1]) * 0.2)
    col1 = (col0[0] + 1.6, col0[1] + 0.9)
    if skin.collar == "tag":
        # Pancho's: a dark collar, and the round silver tag hanging off it - two pixels of steel, lit on top.
        g.limb([T(*col0), T(*col1)], [0.8, 0.75], WOOL, 0.9, "norim")
        sx, sy = T(col1[0] + 0.1, col1[1] + 1.1)
        g.dot(sx, sy, M["steel"][0])
        g.dot(sx, sy + 1, M["steel"][2])
        return
    g.limb([T(*col0), T(*col1)], [0.8, 0.75], GOLD, -0.2, "body")
    sx, sy = T(col1[0] + 0.1, col1[1] + 1.3)
    g.dot(sx, sy, GOLD[0])


def _pup_head(g, T, hc, ha, p, skin):
    """A pup's head: a round skull big for its body, a short muzzle, a floppy ear, the eye low and forward."""
    fur = skin.fur
    jaw = p["jaw"]

    def H(dx, dy):
        rx, ry = rot(dx, dy, ha)
        return T(hc[0] + rx, hc[1] + ry)

    if skin.shaggy:
        # Pancho's head is big and round with hair, and lit high: a white face in shade went dirty grey.
        g.blob(*H(0.0, -0.2), rx=5.5, ry=5.0, ramp=fur, bias=-0.3, kind="fur", angle=ha)
        _pancho_face(g, H, p, skin)
        return
    # The lower jaw, dropping to open.
    jx, jy = rot(2.8, 0.4, jaw * 0.8)
    g.limb([H(1.0, 2.0), H(1.0 + jx, 2.0 + jy)], [1.0, 0.75], fur, 0.5, "fur")
    if jaw > 0.25:
        g.dot(*H(2.6, 2.1), colour=EMBER[1])
        g.dot(*H(3.6, 1.7), colour=BONE[0])
    g.blob(*H(0.0, 0.0), rx=4.8, ry=4.4, ramp=fur, bias=-0.1, kind="fur", angle=ha)
    # The muzzle: short and round.
    g.blob(*H(3.5, 1.3), rx=2.0, ry=1.6, ramp=fur, bias=-0.05, kind="fur", angle=ha)
    g.dot(*H(4.9, 0.9), colour=OCC)
    g.dot(*H(3.0, 2.5), colour=fur[4])
    # The ear, hanging over the back of the skull on the near side - a pup's ear flops, where the hound's
    # stands back under its hat - a stop darker than the skull, so the head reads as skull and ear.
    ea = p["ear"] * 0.4
    ex, ey = rot(-0.6, 4.6, ea)
    ear = skin.ear or fur
    g.limb([H(-1.2, -3.2), H(-1.6 + ex * 0.5, -3.0 + ey * 0.5), H(-1.4 + ex, -3.0 + ey)], [1.6, 1.9, 1.4], ear, 0.35, "fur")
    if skin.ear_tip:
        tx, ty = H(-1.4 + ex, -3.0 + ey + 0.9)
        g.dot(tx, ty, skin.ear_tip)
        g.dot(tx + 1, ty, skin.ear_tip)
    # The eye: low and forward on a pup's face, under a soft brow.
    g.dot(*H(1.6, -1.7), colour=fur[3])
    g.dot(*H(2.4, -1.6), colour=fur[3])
    if skin.glow_eye:
        g.dot(*H(1.6, -0.6), colour=skin.eye[1], kind="glow")
        g.dot(*H(2.4, -0.6), colour=skin.eye[0], kind="glow")
    else:
        g.dot(*H(1.6, -0.6), colour=OCC)
        g.dot(*H(2.4, -0.6), colour=calm_eye(skin.eye)[0])
    if p.get("breath") and skin.breath:
        for (dx, dy), c in zip(((6.8, 0.8), (7.8, 0.0), (8.4, 1.0)), skin.breath):
            g.dot(*H(dx, dy), colour=c, kind="glow")


def _pancho_face(g, H, p, skin):
    """Pancho's face over the skull pup() has laid (PANCHO): the long buff ear, the short flat muzzle, the
    underbite with its teeth, the beard, the fringe over a dark round eye, the black nose."""
    fur = skin.fur
    jaw = p["jaw"]
    # The ear: long, hanging from the crown to below the jaw line, buff, its fringe dipped red (Annwn's).
    ea = p["ear"] * 0.4
    ex, ey = rot(-0.6, 6.4, ea)
    g.limb([H(-1.2, -3.6), H(-1.9 + ex * 0.5, -3.4 + ey * 0.5), H(-1.7 + ex, -3.4 + ey)], [1.7, 2.3, 2.0],
           skin.ear, 0.15, "fur")
    tx, ty = H(-1.7 + ex, -3.4 + ey + 1.4)
    for k, dx in enumerate((-1, 0, 1)):
        g.dot(tx + dx, ty - (k % 2), skin.ear_tip or skin.ear[2])
    # The muzzle: short and flat-faced, under the hair.
    g.blob(*H(3.5, 1.3), rx=2.2, ry=1.9, ramp=fur, bias=-0.3, kind="fur")
    # THE UNDERBITE: the lower jaw out a pixel past the nose, dropping when he bites or howls.
    jx, jy = rot(3.6, -0.2, jaw * 0.8)
    g.limb([H(1.8, 2.8), H(1.8 + jx, 2.8 + jy)], [1.1, 0.95], fur, 0.35, "fur")
    if jaw > 0.25:
        g.dot(*H(3.2, 2.2), colour=EMBER[1])
    # The beard over the muzzle and chin, hanging below the jaw.
    g.limb([H(1.8, 2.2), H(2.1, 4.6)], [1.4, 0.9], fur, -0.05, "fur")
    # The fringe falling over the brow.
    g.limb([H(-0.8, -3.9), H(1.0, -3.3), H(2.6, -2.4)], [1.5, 1.3, 0.9], fur, -0.3, "fur")
    # The eye: dark and round, two by two, under the fringe, with the faintest glint.
    ex0, ey0 = H(0.9, -1.4)
    ex0, ey0 = int(round(ex0)), int(round(ey0))
    g.block(ex0, ey0, OCC, 2)
    g.dot(ex0 + 1, ey0 + 1, skin.eye[1])
    g.dot(ex0, ey0, BONE[2])
    # The black nose at the front of the flat muzzle, and under it the lower teeth on the jutting jaw.
    nx, ny = H(5.1, 0.1)
    nx, ny = int(round(nx)), int(round(ny))
    g.block(nx - 1, ny, OCC, 2)
    tx, ty = H(1.8 + jx, 2.8 + jy)
    tx, ty = int(round(tx)), int(round(ty))
    g.dot(tx - 1, ty - 1, BONE[0])
    g.dot(tx - 2, ty - 1, BONE[1])
    if p.get("breath") and skin.breath:
        for (dx, dy), c in zip(((6.6, 1.0), (7.6, 0.2), (8.2, 1.2)), skin.breath):
            g.dot(*H(dx, dy), colour=c, kind="glow")


FORMS = [
    # name, cell, skin, scale
    # The hound, the crow and the cat are on the 48 cell, the elite row - the hound at 1.3: on the 32 cell
    # it stood a head under the skeletons round it and its hat was a speck, and the hat is the read. Every
    # larger look is on the 64 cell (in the contract's table, as a large prop), drawn bigger inside it.
    # The dog's
    ("hound", 48, HOUND, 1.3),
    ("rimehound", 48, RIME, 1.3),
    ("greathound", 64, GREAT, 1.78),
    # The main hound's Cerberus is the one form on the 96 cell (CERB, "bigger").
    ("cerberus", 96, CERB, 2.5),
    ("cerberus-rime", 96, CERB_RIME, 2.5),
    ("annwn", 64, ANNWN, 1.72),
    ("annwn-rime", 64, ANNWN_RIME, 1.72),
    # The rest of the pack under an Ascension (HELL_PACK, the pups): one head, the hound's size or a pup's.
    ("hellhound", 48, HELL_PACK, 1.3),
    ("hellhound-rime", 48, HELL_PACK_RIME, 1.3),
    ("annwn-hound", 48, ANNWN_PACK, 1.3),
    ("annwn-hound-rime", 48, ANNWN_PACK_RIME, 1.3),
    ("pup", 32, PUP, 1.0),
    ("hell-pup", 32, HELL_PUP, 1.0),
    ("annwn-pup", 32, ANNWN_PUP, 1.0),
    # the crow's
    ("crow", 48, CROW, 1.0),
    ("stormcrow", 48, STORMCROW, 1.0),
    ("raven", 64, RAVEN, 1.0),
    ("morrigan", 64, MORRIGAN, 1.0),
    ("morrigan-storm", 64, MORRIGAN_STORM, 1.0),
    ("griffin", 64, GRIFF, 1.0),
    ("griffin-storm", 64, GRIFF_STORM, 1.0),
    # the cat's
    ("cat", 48, CAT, 1.3),
    ("cait-sith", 48, CAIT_SITH, 1.3),
    ("grimalkin", 64, GRIMALKIN, 1.7),
    ("cath-palug", 64, CATH_PALUG, 1.85),
    ("cath-palug-sith", 64, CATH_PALUG_SITH, 1.85),
]


class _Frame(object):
    """Where a portrait is drawn: scaled by `s` and moved to `at`, into a Fig of its own or a shared one,
    so the spell's icon can hold all three heads (trio) drawn by the same lights and passes."""

    def __init__(self, s=1.0, at=(0.0, 0.0)):
        self.s = s
        self.ox, self.oy = at

    def P(self, x, y):
        return (self.ox + x * self.s, self.oy + y * self.s)

    def r(self, v):
        return v * self.s


# THE CARD ICONS (Eric, 2026-10-30: "familiar icons are very muted and hard to read. remake them, keeping the
# theme"). The portraits below were drawn with the sprites' own depth and dither, which is right for a figure
# lit among the bodies on a dark floor and wrong for a card: a fawn coat sitting in its base and shade stops,
# speckled by the dither, on a night of the same value, came out as a brown smudge under an indigo smudge at
# the size a card is read. A portrait now sits ICON_LIFT higher in its ramps - the lit and base stops, the
# shade kept for the turn of the form - with almost no dither, so each plane is one clean tone; the hat is
# lit, so its cone reads against the night; and spell_icons._familiar_card hangs a moon behind the head, so
# every silhouette - the hat's point, the crow's beak, the cat's ears - has a pale ground to read against.
ICON_LIFT = 0.3
ICON_DITHER = 0.05


def calm_eye(eye):
    """The two colours a portrait's eye is drawn in, from a skin's eye (its hot point and its colour).

    Eric, 2026-10-28, of the Familiar's card icon enlarged: "it looks a little creepy, the eye is too
    prominent." The eye was the most contrasty thing on the head - a 2x2 block of amber's two brightest
    stops, the near-white one on top, with a hard occlusion pixel beside it for a pupil: a staring eye,
    and the head's focal point. A portrait's eye is now two pixels from the MIDDLE of its own ramp - the
    base stop and the shade under it - with no white glint and no black socket: a dark, warm, calm eye
    that still reads at 32 and at 64, and lets the hat and the muzzle carry the head. The ramp is found
    from the eye's own colour, so the hellhound's ember eye and the frost forms' ice eye calm the same way."""
    for ramp in (AMBER, EMBER, ICE, E["darkness"], E["lightning"], E["light"], GOLD):
        if eye[0] in ramp or eye[1] in ramp:
            return (ramp[2], ramp[3])
    return (eye[1], eye[1])


def portrait(skin=HOUND, cell=32, hat=True, s=1.0, at=(0.0, 0.0), into=None):
    """The hound's head at card size, for the spell icon (spell_icons.familiar_icon). Drawn with the
    sprites' own materials, light and passes, but to a portrait's proportions: scaled up from the
    sprite, the long muzzle read as a slab under a hat. A rounder skull, the ear hanging clear of it,
    a black nose, a two-pixel eye and the mouth a little open are what say "dog" at 32 pixels.

    `hat=False` is for Cerberus's icon (tools/art/evolution_icons.py): the hat is on one head of three,
    as it is on the sprite. `s` and `at` draw it smaller and elsewhere, `into` into a Fig already begun
    (trio). Rendered with the actor finish, as the sprites are, so the family matches."""
    g = into or Fig(cell)
    g.dither = ICON_DITHER
    g.lay = False
    F = _Frame(s, at)
    P, r = F.P, F.r
    fur = skin.fur
    g.deeper[id(fur)] = FUR_DEPTH - ICON_LIFT
    # Neck and chest, out of the bottom of the frame.
    g.limb([P(13.0, 22.0), P(10.0, 27.0), P(8.0, 33.0)], [r(4.6), r(5.6), r(6.6)], fur, 0.1, "fur")
    # The ear hangs back, clear of the skull, a stop darker.
    g.limb([P(10.0, 13.5), P(6.6, 18.0), P(5.6, 22.5)], [r(2.6), r(2.4), r(1.9)], skin.ear or fur, 0.7, "fur")
    # The lower jaw, closed up under the muzzle: a relaxed mouth, not a gape of teeth (2026-10-28, with
    # the eye below - the open red mouth under a staring eye was half of what read as creepy).
    g.limb([P(15.0, 20.4), P(20.0, 21.6), P(23.6, 21.6)], [r(2.3), r(1.7), r(1.15)], fur, 0.45, "fur")
    g.blob(*P(13.0, 16.5), rx=r(6.4), ry=r(5.6), ramp=fur, bias=-0.1, kind="fur")
    g.limb([P(16.0, 16.0), P(21.0, 17.6), P(26.0, 19.2)], [r(4.0), r(3.2), r(2.3)], fur, 0.0, "fur")
    # The line of the mouth, back from the nose along the jaw, turning up at its corner: a dog at ease.
    for (x, y) in ((24, 21), (23, 21), (22, 21), (21, 21), (20, 21), (19, 20)):
        g.dot(*P(x, y), colour=fur[4])
    # Nose, eye, brow.
    for (x, y) in ((26, 18), (27, 18), (26, 19), (27, 19)):
        g.dot(*P(x, y), colour=OCC)
    g.dot(*P(27, 17), colour=fur[2])
    for (x, y) in ((14, 13), (15, 13), (16, 13)):
        g.dot(*P(x, y), colour=fur[3])
    kind = "glow" if skin.glow_eye else "detail"
    warm, dark = calm_eye(skin.eye)
    g.dot(*P(15, 14), colour=warm, kind=kind)
    g.dot(*P(16, 14), colour=dark, kind=kind)
    # The collar and its star.
    g.limb([P(6.0, 26.0), P(15.0, 27.5)], [r(1.2), r(1.2)], GOLD, -0.3, "body")
    g.dot(*P(13, 29), colour=GOLD[0])
    g.dot(*P(14, 29), colour=LIGHT_E[0])
    g.dot(*P(13, 30), colour=GOLD[1])
    if hat:
        # The hat, tipped back, its tip swept down behind.
        g.poly([P(5.0, 11.0), P(20.0, 11.0), P(15.0, 5.0), P(10.0, 1.5), P(5.0, 1.0), P(2.0, 3.0), P(6.0, 4.5)], WOOL, -0.1,
               "cloth", lean=(0.0, -0.35))
        g.limb([P(3.0, 11.5), P(21.0, 11.5)], [r(1.4), r(1.4)], WOOL, -0.05, "body")
        g.limb([P(6.0, 9.6), P(18.0, 9.6)], [r(0.9), r(0.9)], GOLD, -0.4, "norim")
        g.dot(*P(11, 6), colour=GOLD[0])
        g.dot(*P(12, 6), colour=LIGHT_E[0])
        g.dot(*P(11, 5), colour=GOLD[1])
        g.dot(*P(10, 6), colour=GOLD[1])
    return g if into is not None else g.render(finish=True)


# The crow's beak in a portrait: amber and gold from the middle of their ramps. The sprite keeps the
# brighter amber beak - at 2 pixels it is what tells it from the Fields' pale-beaked rook.
BEAK_CALM = [AMBER[2], AMBER[3], GOLD[2], GOLD[3], GOLD[4]]


def crow_portrait(bird=CROW, cell=32, hat=True, s=1.0, at=(0.0, 0.0), into=None):
    """The crow's head and shoulders at card size: a big round head over a deep breast, a heavy straight
    beak in amber, a warm eye under a dark brow, the gold collar and star - and the hat, set back on the
    crown. The shoulder of a wing comes up behind, edged in gold (or lightning, for the Stormcrow)."""
    g = into or Fig(cell)
    g.dither = ICON_DITHER
    g.lay = False
    F = _Frame(s, at)
    P, r = F.P, F.r
    fe = bird.feather
    g.deeper[id(fe)] = bird.depth - ICON_LIFT
    # The wing's shoulder, behind the breast, its primaries out of the frame at the lower left.
    g.poly([P(1.0, 33.0), P(2.0, 22.0), P(8.0, 17.0), P(14.0, 19.0), P(12.0, 27.0), P(8.0, 33.0)], fe, 0.35, "cloth",
           lean=(-0.15, -0.45))
    g.limb([P(2.0, 23.0), P(8.0, 17.4), P(13.4, 19.0)], [r(0.9), r(0.9), r(0.7)], bird.edge, -0.2, "norim")
    # Breast and throat.
    g.blob(*P(15.0, 26.0), rx=r(7.6), ry=r(7.0), ramp=fe, bias=0.0, kind="fur")
    if bird.ruff:
        for (bx, by, ln) in ((20.0, 20.5, 4.4), (18.4, 24.0, 4.0), (21.6, 23.0, 3.6)):
            g.poly([P(bx - 1.6, by - 1.0), P(bx + 1.0, by - 0.8), P(bx + ln * 0.35, by + ln)], fe, 0.05, "fur", lean=(-0.2, -0.4))
    if bird.crest:
        for (bx, by, a, ln) in ((10.0, 9.0, -2.4, 6.0), (12.4, 7.6, -2.0, 5.6), (8.4, 11.4, -2.8, 5.0)):
            g.limb([P(bx, by), P(bx + math.cos(a) * ln, by + math.sin(a) * ln)], [r(1.4), r(0.5)], fe, 0.3, "fur")
    # The head.
    g.blob(*P(15.0, 14.5), rx=r(7.4), ry=r(6.8), ramp=fe, bias=-0.15, kind="fur")
    # The beak: the lower mandible, then the upper over it, long and straight with a hooked tip - in the
    # dark middle of the gold, so the eye, not the beak, is where a face is read from, and neither shouts.
    g.poly([P(20.0, 16.5), P(29.0, 18.0), P(27.5, 20.0), P(20.5, 19.5)], BEAK_CALM, 0.55, "body")
    g.poly([P(19.6, 12.4), P(26.0, 14.0), P(31.0, 16.6), P(29.4, 18.2), P(20.0, 17.0)], BEAK_CALM, -0.05, "body")
    g.dot(*P(29, 18), colour=BEAK_CALM[3])
    # The eye under its brow, back on the head: the calm eye (calm_eye), two warm pixels and no glint.
    for (x, y) in ((12, 11), (13, 11), (14, 11)):
        g.dot(*P(x, y), colour=fe[3])
    kind = "glow" if bird.glow_eye else "detail"
    warm, dark = calm_eye(bird.eye)
    g.dot(*P(13, 12), colour=warm, kind=kind)
    g.dot(*P(14, 12), colour=dark, kind=kind)
    # The collar and its star.
    g.limb([P(9.0, 22.0), P(19.0, 23.5)], [r(1.2), r(1.2)], GOLD, -0.3, "body")
    g.dot(*P(16, 25), colour=GOLD[0])
    g.dot(*P(17, 25), colour=LIGHT_E[0] if not bird.storm else LT[0])
    g.dot(*P(16, 26), colour=GOLD[1])
    if bird.storm:
        for (x, y, t) in ((29, 10, 0), (30, 12, 1), (27, 8, 1), (3, 18, 1)):
            g.dot(*P(x, y), colour=LT[t], kind="glow")
    if hat:
        g.poly([P(6.0, 9.0), P(19.0, 9.0), P(15.0, 3.6), P(10.0, 0.6), P(5.0, 0.4), P(2.4, 2.4), P(6.4, 3.8)], WOOL, -0.1,
               "cloth", lean=(0.0, -0.35))
        g.limb([P(4.4, 9.4), P(20.4, 9.4)], [r(1.3), r(1.3)], WOOL, -0.05, "body")
        g.limb([P(7.0, 7.8), P(17.6, 7.8)], [r(0.85), r(0.85)], GOLD, -0.4, "norim")
        g.dot(*P(11, 5), colour=GOLD[0])
        g.dot(*P(12, 5), colour=LIGHT_E[0])
        g.dot(*P(11, 4), colour=GOLD[1])
    return g if into is not None else g.render(finish=True)


def cat_portrait(skin=CAT, cell=32, hat=True, s=1.0, at=(0.0, 0.0), into=None, mantle=False):
    """The cat's face at card size, face on: two big pointed ears, a round head a little wider than tall,
    two calm eyes (calm_eye - no slit, no glint), a pink nose over a small mouth, whiskers, the tabby's
    bands in its coat, the collar and star - and the hat sat on its crown between the ears. Face on
    because a cat is read from two ears and two eyes; in profile at 32 pixels its short face was a lump."""
    g = into or Fig(cell)
    g.dither = ICON_DITHER
    g.lay = False
    F = _Frame(s, at)
    P, r = F.P, F.r
    fur = skin.fur
    g.deeper[id(fur)] = (FUR_DEPTH if skin.depth == FUR_DEPTH else skin.depth) - ICON_LIFT
    if skin.pattern:
        g.pattern = (fur, skin.pattern)
    ear = skin.ear or fur
    # Shoulders and chest out of the bottom of the frame.
    g.blob(*P(15.5, 31.0), rx=r(10.0), ry=r(5.6), ramp=fur, bias=0.15, kind="fur")
    if mantle:
        w = WOOL
        g.poly([P(2.0, 33.0), P(4.0, 27.0), P(10.0, 25.0), P(21.0, 25.0), P(27.0, 27.0), P(29.0, 33.0)], w, 0.1, "cloth",
               lean=(-0.1, -0.5))
        g.limb([P(4.0, 27.0), P(10.0, 25.0), P(21.0, 25.0), P(27.0, 27.0)], [r(0.6)] * 4, GOLD, -0.2, "norim")
    # The ears, out from the top of the head on either side, the far one a stop darker.
    g.poly([P(5.0, 14.0), P(11.5, 10.0), P(5.5, 3.0)], ear, 0.45, "fur", lean=(-0.2, -0.4))
    g.poly([P(19.5, 10.0), P(26.0, 14.0), P(25.5, 3.0)], ear, 0.15, "fur", lean=(-0.3, -0.4))
    # The head, and the pale pads of the muzzle.
    g.blob(*P(15.5, 17.5), rx=r(9.4), ry=r(7.8), ramp=fur, bias=-0.1, kind="fur")
    g.blob(*P(15.5, 21.4), rx=r(4.6), ry=r(2.7), ramp=fur, bias=-0.3, kind="fur")
    # The nose and the small mouth under it.
    g.dot(*P(15, 19), colour=EMBER[0])
    g.dot(*P(16, 19), colour=EMBER[0])
    g.dot(*P(15, 20), colour=EMBER[1])
    for (x, y) in ((15, 21), (14, 22), (16, 21), (17, 22)):
        g.dot(*P(x, y), colour=fur[4])
    # The eyes: the calm eye, twice, under a soft lid each.
    kind = "glow" if skin.glow_eye else "detail"
    warm, dark = calm_eye(skin.eye)
    for ex in (10, 19):
        for x in (ex - 1, ex, ex + 1, ex + 2):
            g.dot(*P(x, 15), colour=fur[3])
        g.dot(*P(ex, 16), colour=warm, kind=kind)
        g.dot(*P(ex + 1, 16), colour=dark, kind=kind)
    # Whiskers: short pale strokes off the pads, either side.
    for (x, y, t) in ((8, 21, 1), (7, 21, 2), (8, 23, 2), (23, 21, 1), (24, 21, 2), (23, 23, 2)):
        g.dot(*P(x, y), colour=BONE[t])
    if skin.star:
        for (x, y) in ((15, 29), (16, 29), (15, 28), (15, 30), (14, 29)):
            g.dot(*P(x, y), colour=skin.star)
    # The collar and its star.
    g.limb([P(8.0, 26.6), P(23.0, 26.6)], [r(1.2), r(1.2)], GOLD, -0.3, "body")
    g.dot(*P(15, 28), colour=GOLD[0])
    g.dot(*P(16, 28), colour=LIGHT_E[0])
    g.dot(*P(15, 29), colour=GOLD[1])
    # The inside of each ear, pink (or the lit eye's colour, on a glow-eyed cat).
    for (x, y) in ((7, 8), (7, 9), (24, 8), (24, 9)):
        g.dot(*P(x, y), colour=EMBER[0] if not skin.glow_eye else ear[1])
    if hat:
        # On the crown between the ears, tipped back and swept to the left, its brim across the brow.
        g.poly([P(8.4, 10.6), P(22.6, 10.6), P(18.6, 5.0), P(13.0, 1.4), P(8.0, 0.6), P(5.4, 2.4), P(9.6, 4.6)], WOOL, -0.1,
               "cloth", lean=(0.0, -0.35))
        g.limb([P(7.4, 11.0), P(23.6, 11.0)], [r(1.2), r(1.2)], WOOL, -0.05, "body")
        g.limb([P(9.4, 9.2), P(21.6, 9.2)], [r(0.8), r(0.8)], GOLD, -0.4, "norim")
        g.dot(*P(14, 5), colour=GOLD[0])
        g.dot(*P(15, 5), colour=LIGHT_E[0])
        g.dot(*P(14, 4), colour=GOLD[1])
    return g if into is not None else g.render(finish=True)


def trio(cell=32):
    """The Familiar's own card icon (spell_icons.familiar_icon): the three it can be taken as, together -
    the hound large in front in the wizard's hat, the crow over its shoulder and the cat below it, smaller
    and bare-headed so three hats do not crowd 32 pixels - each drawn by its own portrait into one Fig, so
    the passes separate one from another."""
    g = Fig(cell)
    g.dither = ICON_DITHER
    g.lay = False
    crow_portrait(CROW, hat=False, s=0.5, at=(16.5, 1.5), into=g)
    cat_portrait(CAT, hat=False, s=0.5, at=(17.0, 15.5), into=g)
    portrait(HOUND, s=0.78, at=(-1.0, 6.5), into=g)
    return g.render(finish=True)


def frames_for(cell, skin, scale):
    out = []
    if isinstance(skin, dict):
        for anim, poses, loop, speed in GRIFFIN_ANIMS:
            out.append((anim, [griffin(skin, p).render(finish=True) for p in poses], loop, speed))
        return out
    if isinstance(skin, Bird):
        for anim, poses, loop, speed in CROW_ANIMS:
            out.append((anim, [crow(cell, skin, p).render(finish=True) for p in poses], loop, speed))
        return out
    if isinstance(skin, Pup):
        # The hound's own strips and timing: a pup runs, leaps, bites and howls on the frames Familiar.cs sets.
        for anim, poses, loop, speed in HOUND_ANIMS:
            out.append((anim, [pup(cell, skin, p).render(finish=True) for p in poses], loop, speed))
        return out
    anims = CAT_ANIMS if skin.feline else HOUND_ANIMS
    for anim, poses, loop, speed in anims:
        out.append((anim, [hound(cell, skin, p, scale).render(finish=True) for p in poses], loop, speed))
    return out


def density(canvas):
    w, h = canvas.w, canvas.h
    filled = 0
    edges = 0
    for y in range(h):
        for x in range(w):
            p = canvas.get(x, y)
            if not p[3]:
                continue
            filled += 1
            for dx, dy in ((1, 0), (0, 1)):
                q = canvas.get(x + dx, y + dy)
                if q[3] and q[:3] != p[:3]:
                    edges += 1
    return filled, edges / float(max(1, filled))


def write_strip(frames, path, cell):
    from PIL import Image
    sheet = Image.new("RGBA", (cell * len(frames), cell), (0, 0, 0, 0))
    for i, c in enumerate(frames):
        sheet.alpha_composite(c.img, (i * cell, 0))
    sheet.save(path)


def preview(sets, path, zoom=5):
    from PIL import Image
    bg = (30, 38, 52, 255)
    rows = []
    for name, cell, anims in sets:
        for anim, frames, _l, _s in anims:
            rows.append((name + " " + anim, cell, frames))
    W = max(len(f) * (c * zoom + 6) for _, c, f in rows) + 6
    H = sum(c * zoom + 6 for _, c, _f in rows) + 6
    sheet = Image.new("RGBA", (W, H), bg)
    y = 6
    for name, cell, frames in rows:
        for i, c in enumerate(frames):
            im = c.img.resize((cell * zoom, cell * zoom), Image.NEAREST)
            sheet.alpha_composite(im, (6 + i * (cell * zoom + 6), y))
        y += cell * zoom + 6
    sheet.save(path)


def main(write=True, only=None):
    if not os.path.isdir(PREVIEW_DIR):
        os.makedirs(PREVIEW_DIR)
    sets = []
    bad_total = 0
    for name, cell, skin, scale in FORMS:
        if only and name not in only:
            continue
        anims = frames_for(cell, skin, scale)
        sets.append((name, cell, anims))
        for anim, frames, loop, speed in anims:
            for c in frames:
                bad = c.audit()
                if bad:
                    bad_total += 1
                    print("OFF-CONTRACT COLOURS in %s %s: %s" % (name, anim, bad[:4]))
        filled, ratio = density(anims[0][1][0])
        print("  familiar-%-14s %dx%d  idle %4d px  %.2f edges/px%s"
              % (name, cell, cell, filled, ratio, "" if ratio >= 0.80 else "   SPARSE"))
    preview(sets, os.path.join(PREVIEW_DIR, "familiar-forms.png"))
    preview(sets, os.path.join(PREVIEW_DIR, "familiar-forms-x2.png"), zoom=2)
    if bad_total:
        return 1
    if not write:
        return 0
    for name, cell, anims in sets:
        spec = []
        for anim, frames, loop, speed in anims:
            png = "familiar-%s-%s.png" % (name, anim)
            write_strip(frames, os.path.join(OUT_DIR, png), cell)
            spec.append((anim, png, len(frames), loop, speed))
        sheet = frames_name(name)
        spriteframes.write(os.path.join(FRAMES_DIR, sheet + ".tres"), spec, OUT_REL, cell=cell)
        print("  %-24s %3d frames -> scenes/resources/%s.tres" % ("familiar-" + name, sum(a[2] for a in spec), sheet))
    return 0


def frames_name(form):
    """familiar-cerberus-rime -> FamiliarCerberusRimeFrames: the name scripts/Familiar.cs loads by."""
    return "Familiar" + "".join(part.capitalize() for part in form.split("-")) + "Frames"


if __name__ == "__main__":
    _only = [a.split("=", 1)[1] for a in sys.argv if a.startswith("--only=")]
    sys.exit(main(write="--preview" not in sys.argv, only=_only[0].split(",") if _only else None))
