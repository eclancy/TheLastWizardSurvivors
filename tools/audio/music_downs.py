# -*- coding: utf-8 -*-
"""The White Downs' set (.ai/second-half.md 4.2, F3): a hunt at night on chalk, under no moon. Scored
here and rendered by tools/audio/music.py, which finds this module by its name and appends PIECE to its
chapters - run music.py, never this:

    python tools/audio/music.py downs --scratch --balance    # iterate, into tools/audio/_music_preview/
    python tools/audio/music.py downs                        # render into assets/music/, once

THE HUNT IS THE FOREST'S TOO, SO THIS ONE IS MADE OTHERWISE. The Enchanted Forest already gallops (6/8 at
112, its pulse is commented "the hunt") and already has horns, and every mode of the seven notes is some
chapter's tonic. What keeps the Downs apart, at once:

  KEY   E, WITH NO F AND NO F SHARP - the hexatonic minor E G A B C D. F sharp is not among the seven, and
        the F that is would make it E Phrygian, the Swamp's, whose whole voice is the E-F half step. So the
        Downs have no second degree at all: open and cold, an old folk shape (English tunes are often
        hexatonic). SCALE below enforces it - check_scale refuses an F before anything renders. The D chord
        has no third for the same reason (its F sharp is missing): an open fifth.
  WRAP  Am to Em, iv - i, the plagal close. No other chapter wraps on it.
  METRE 12/8 at dotted quarter 140: four beats a bar, so a bar is long - the long night - over eighths
        running at 420 a minute, a quarter faster than the Forest. 140 strides a minute is about a
        galloping horse's own stride rate, so the pulse's four footfalls (three quick, then the leading
        leg on the beat) are a real gallop, where the Forest's is a figure (eighth, two sixteenths,
        eighth). No other piece is in 12/8 or at 140.
  LEAD  A HUNTING HORN (hunt_horn below), not the orchestral horn every other horn line uses. A straight
        tube with no valves plays only its own harmonic series, and this one is pitched on E: its fifth
        harmonic, the major third G sharp, is the one note of that series the seven do not have, so the
        horn plays E, B and its seventh harmonic's D and never a third - a hunting call with the moon
        missing from it. It speaks the way a hunting horn does: repeated notes, tongued ("doubling the
        horn"), leaps of a fourth and an octave. In B the hook passes to the night's voices (high women's
        voices in unison on "oo", the huntress's company calling for a moon that does not rise), who have
        the notes the horn has not - G, A and C - while the horn sounds once more, far off, in each gap.

  A  Em Em C D | Em Em G D      the hunting horn's call, twice
  B  C C G G | Am Am Em Am      the voices answer; the horn far off at the end of each phrase
  Am wraps to Em: iv - i.

THE STEMS (audio-direction section 11):
  bed    a B-E drone fifth, held strings, and THE BEATERS' LINE as the ostinato: the low strings walking
         the 12/8 long-short (quarter, eighth) through the chord, the line of men the hunt sends through
         the gorse. In B the line turns back on itself and two horns are held low under the strings, far
         off - a change of colour, the strings stepping back so the level does not rise.
  pulse  (battle music from the first second) the taiko on beats one and three with a stroke into each,
         the HOOFBEATS on a hand drum - four footfalls to every beat, three quick and the fourth on it - a
         snare on two and four, the bass on the gallop's own rhythm (root on the beat, root and octave on
         the footfalls into the next), and pizzicato running the chord in eighths; a roll into each section.
  lead   the hunting horn (A); the night's voices with the horn far off (B).
  boss   the Unstrung Hunter's layer: the taiko doubled through the four beats, brass stabbing the roots on
         one and three, strings running the chord in sixteenths, and the hunting horn "doubling" on E -
         the hunt's call, now his.

SIXTEEN BARS is 27.4 s, inside section 11's 30 s Size rule, in two sections of 13.7 s.

THE TWO NEW VOICES are checked against the element table (section 11, "No chapter imitates an effect"):
the hunting horn is harmonic saws and a pulse through a loudness-tracking lowpass under 2.4 kHz, its only
pitch movement a lip's 30-cent scoop over 25 ms (the water droplet is a sweep of more than an octave); the
voices are three harmonic sources a few cents apart through formants that follow the note, with a late 5 Hz
vibrato and no tremolo (arcane is two triangles 0.7% apart at a 17 Hz tremolo), lowpassed at 2 kHz. Nothing
under E2: the drone is B2 and E3, the lowest bass note G2.

MEASURED (2026-11-08): seams step at most 0.27 and click at most 0.15 of each loop's own body, every stem
within 1.8 dB of its median second at the wrap; 0% under 80 Hz and above 5 kHz, 0% in 2-5 kHz, hf at most 1%
(the pulse 83% low band, the boss layer 64%); the lead's two sections within 0.2 dB of each other (the horn
and the voices -2.8 and -3.5 dB of the stem); the hoofbeats 8.2 dB under the pulse, the snare 16.3; and under
the effects (mixsim.py --music --policy tiered) a median cast +12.2 dB over bed and pulse, +7.4 over the
fight's bed, pulse and boss layer, +8.1 over a late-game set, player_hurt +17.2 to +18.6 - inside the other
chapters' ranges on every row.

NOT YET HEARD. Composed, measured (--balance, --report, analyse.py --music, mixsim.py --music) and rendered by
an agent that cannot listen, like every chapter set before it. The first listen will move things.
"""
from __future__ import division

import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import music as M
import synth as S

# 12/8 in sixteenths: six to the dotted quarter, twenty-four to the bar.
GRID = M.Grid(6, 24)
B = 8

# The hexatonic minor on E: the seven notes less their F. A subset of M.SEVEN, so every stinger still fits.
SCALE = M.pitch_classes(["E", "G", "A", "B", "C", "D"])

CHORDS = {
    # root (the bass), pad (the held strings), arp (the pizzicato and the motor), low (the beaters' line)
    "Em": M._ch("E3", ["B3", "E4", "G4"], ["B3", "E4", "G4", "B4"], ["E3", "B3", "E4", "G4"]),
    "Am": M._ch("A2", ["C4", "E4", "A4"], ["A3", "C4", "E4", "A4"], ["A2", "E3", "A3", "C4"]),
    "C": M._ch("C3", ["C4", "E4", "G4"], ["G3", "C4", "E4", "G4"], ["C3", "G3", "C4", "E4"]),
    "G": M._ch("G2", ["B3", "D4", "G4"], ["G3", "B3", "D4", "G4"], ["G2", "D3", "G3", "B3"]),
    # The open fifth: D's third is F sharp, which the seven do not have.
    "D": M._ch("D3", ["A3", "D4", "A4"], ["A3", "D4", "A4", "D5"], ["D3", "A3", "D4", "A4"]),
}
PROG = "Em Em C D Em Em G D C C G G Am Am Em Am".split()

# The call. Only E, B and D - the harmonics of a horn on E that the seven have - in a hunting horn's
# rhythms: three tongued notes into a held one, an octave leap, the call falling back to the low B.
HORN = """E4:2 E4:2 B4:2 E5:6 D5:2 B4:2 E4:2 B4:6 | E5:4 D5:2 B4:6 E4:2 E4:2 E4:2 B3:6 |
B4:6 E5:6 D5:4 B4:2 E4:6 | D5:6 B4:2 D5:2 E5:2 D5:6 .:6 |
E4:2 E4:2 B4:2 E5:6 D5:2 B4:2 E4:2 B4:6 | E5:4 D5:2 B4:4 D5:2 E5:12 |
B4:2 B4:2 B4:2 D5:6 B4:2 E4:2 B4:2 D5:6 | E5:6 D5:12 .:6 |"""

# The voices' answer: long notes, and the notes the horn cannot play. Each phrase ends a half bar early,
# and the horn sounds in the gap, far off (FAR_HORN).
VOICES = """E5:12 G5:6 E5:6 | C5:18 D5:6 | D5:12 B4:6 D5:6 | G4:12 .:12 |
A4:12 C5:6 E5:6 | G5:12 E5:6 D5:6 | E5:12 B4:6 G4:6 | A4:12 .:12 |"""
FAR_HORN = """.:24 | .:24 | .:24 | .:12 E4:2 E4:2 B4:2 E5:6 |
.:24 | .:24 | .:24 | .:12 E4:2 E4:2 B4:2 B4:6 |"""


def hunt_horn(freq, dur):
    """A hunting horn: one straight brass tube, no valves. Brighter, quicker and rougher than `horn`, which
    is the orchestra's: the lips speak each note with a scoop up from 30 cents under in 25 ms and a breath
    of tongue (a short band of noise at 700 Hz), so repeated notes articulate - a hunting call is mostly
    repeated notes. A saw and a narrow pulse through a lowpass that opens with the note's loudness, as
    brass does, driven a little for the bell's rasp and lowpassed again AFTER the drive (a drive left last
    is a hiss, audio-direction 6), under 2.4 kHz."""
    n = S.n_samples(dur)
    env = S.adsr(dur, a=0.018, d=0.12, s=0.74, r=min(0.14, dur * 0.3))
    f = M._pitch(freq, dur, 5.0, 0.0, 0.2, bend_cents=-30.0, bend_time=0.025)
    src = S.mix(S.gain(S.osc("saw", f, dur), 0.6),
                S.gain(S.osc("pulse", [x * 1.002 for x in f], dur, duty=0.38, phase=0.3), 0.4))
    cutoff = [min(2300.0, freq * (1.8 + 4.2 * e)) for e in env]
    v = M._lp4(src, cutoff, 0.85)
    v = S.drive(S.env_mul(v, env[:n]), 1.6)
    tongue = S.bandpass(S.osc("noise", 0, dur, seed=M._seed(freq, dur, 61)), 700.0, 2.0)
    tongue = S.env_mul(tongue, M._ring(n, 0.012))
    v = S.mix(v, S.gain(tongue, 0.22))
    v = M._lp4(v, 2400.0)
    return M._end_fade(M._soft_attack(v, 0.002), min(0.02, dur * 0.2))


def far_horn(freq, dur):
    """The same horn from the next down: its top gone at 1.1 kHz and its tongue softened, so the call
    reads as distance, not as a quieter player."""
    v = M._lp4(hunt_horn(freq, dur), 1100.0)
    return M._soft_attack(v, 0.02)


def night_voices(freq, dur):
    """The huntress's company: three high women's voices in unison on "oo", a few cents apart, so the line
    beats slowly as a small choir's does rather than ringing as one throat. Each is a saw and a triangle
    with a vibrato that arrives late and slow; the first formant follows the note (a high voice on "oo"
    sings through it) and a second sits near 850 Hz, softer. A slow swell in and a long release, lowpassed
    at 2 kHz. No tremolo: arcane is two triangles 0.7% apart at 17 Hz, and these beat at 1-3 Hz in
    amplitude only because three people never sing exactly one pitch."""
    voices = []
    for k, (cents, rate, phase) in enumerate([(0.0, 5.0, 0.0), (6.0, 5.4, 0.37), (-5.0, 4.7, 0.71)]):
        base = freq * (2.0 ** (cents / 1200.0))
        f = M._pitch(base, dur, rate, 0.006, 0.3 + 0.05 * k)
        voices.append(S.mix(S.gain(S.osc("saw", f, dur, phase=phase), 0.45),
                            S.gain(S.osc("tri", f, dur, phase=phase + 0.2), 0.55)))
    src = S.mix(*voices)
    f1 = S.bandpass(src, max(420.0, freq * 1.02), 3.2)
    f2 = S.bandpass(src, max(850.0, freq * 1.5), 4.5)
    body = M._lp4(src, min(1000.0, freq * 1.8))
    v = M._lp4(S.mix(f1, S.gain(f2, 0.35), S.gain(body, 0.5)), 2000.0)
    return S.env_mul(v, S.adsr(dur, a=0.14, d=0.2, s=0.88, r=min(0.45, dur * 0.35)))


def bed():
    g, ch = GRID, CHORDS
    in_b = lambda bar: bar >= B
    ev = M._drone_runs(g, len(PROG), ["B2", "E3"], 0.2)
    # The strings step back in B so the low horns arrive without the bed getting louder.
    ev += M.held(g, PROG, lambda c: ch[c]["pad"], M.pad_strings,
                 lambda bar: 0.32 if in_b(bar) else 0.40, 12)
    # The hunt's horns held low and far off under the voices, in B only: root and fifth, no third.
    ev += M.held(g, PROG, lambda c: [ch[c]["low"][0], ch[c]["low"][1]], M.horn, 0.26, 12,
                 spread=0.04, only=in_b)
    # THE BEATERS' LINE: long-short through the chord in every beat, walking; turned back in B.
    ev += M.ost(g, PROG, lambda c: ch[c]["low"],
                lambda bar: "3...2.1...2.0...2.1...3." if in_b(bar) else "0...1.2...1.0...1.3...2.",
                M.spiccato, 3, 0.68)
    return ev


FILLS = (B - 1, 16 - 1)


def pulse():
    """The hunt at a gallop: the taiko on one and three with a stroke into each, the hoofbeats - four
    footfalls to a beat, three quick and the fourth on it - a snare on two and four, the bass on the
    gallop's rhythm, and pizzicato running the chord."""
    g, ch = GRID, CHORDS
    ev = M.ost(g, PROG, lambda c: ch[c]["arp"],
               lambda bar: "3.2.1.0.1.2.3.2.1.0.1.2." if bar >= B else "0.1.2.3.2.1.0.1.2.3.2.1.",
               M.pizz, 2, 0.4)
    # The bass on the gallop: the root on the beat, the root again and its octave on the footfalls
    # that lead into the next.
    ev += M.ost(g, PROG, M.octaves_of(ch), "0..0.10..0.10..0.10..0.1", M.bass_pluck, 1.8, 0.62)
    for bar in range(len(PROG)):
        ev += M.hits(g, bar, "X.........x.X.........o.", M.taiko, "A2", 0.78, 8)
        if bar in FILLS:
            # A roll into the next section, and across the seam into the first.
            ev += M.hits(g, bar, "X..-oxX..-ox..o.o.x.xxXX", M.hand_drum, "D3", 1.15, 2)
            ev += M.hits(g, bar, "..............o.o.x.xxXX", M.snare, None, 0.6, 1)
        else:
            # The hoofbeats.
            ev += M.hits(g, bar, "X..-oxX..-oxX..-oxX..-ox", M.hand_drum, "D3", 1.15, 2)
            ev += M.hits(g, bar, "......X...........X.....", M.snare, None, 0.9, 3)
    return ev


def boss():
    """The Unstrung Hunter: the taiko doubled through the four beats, brass on the roots on one and three,
    the strings running the chord in sixteenths, and the hunting horn doubling on E - his call now."""
    g, ch = GRID, CHORDS

    def horn_call(bar):
        # Every other bar, the horn's "doubling" - the same note tongued fast - into beat four.
        if bar % 2 == 0 or bar in FILLS:
            return []
        return M.hits(g, bar, "............X.x.x.X.....", hunt_horn, "E4", 0.5, 2)

    return M.boss_layer(
        g, PROG, ch,
        drum=("X.xX.xX.xX.xX.xX.xX.xx.x", M.taiko, "D3", 0.62, 4),
        stabs=("X.....x.....X.....x.....", M.brass, 0.58, 3),
        motor=("012321012321012321012321", M.spiccato, lambda c: c["arp"], 0.46, 1.2),
        fills=FILLS, roll=("X.x.x.X.x.x.X.xxx.xxxXXX", M.snare, None, 0.75, 1),
        extra=horn_call)


def lead():
    g = GRID
    ev = M.line(g, 0, HORN, hunt_horn, 0.53, legato=0.3)
    ev += M.line(g, B, VOICES, night_voices, 0.82, legato=0.6)
    ev += M.line(g, B, FAR_HORN, far_horn, 0.3, legato=0.3)
    return ev


PIECE = M.Piece(
    "downs", 140.0, 4, 16,
    [("bed", bed, 0.3), ("pulse", pulse, 0.14), ("lead", lead, 0.26), ("boss", boss, 0.14)],
    tail_beats=4, scale=SCALE, room=(0.78, 0.5), key=M.Key("E", "hexatonic minor"),
    describe="The White Downs - E hexatonic minor (no F), 12/8 at dotted quarter 140, A/B of 8 bars")
