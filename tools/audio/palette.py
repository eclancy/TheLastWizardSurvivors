# -*- coding: utf-8 -*-
"""The element sound palette - the audio counterpart of bonelight.MATERIALS.

.ai/art-direction.md section 3 states the visual rule for the twelve elements:

    The core of every element's effect is white-hot. Identity lives in the mid and edge.

This module is the same rule in time instead of space:

    The first 8 ms of every element sound is the same white transient.
    Identity lives in the body and the tail.

So a cast always reads as "a spell went off" on the attack, and the hue answers "which one"
a beat later - which is what lets twenty spells share one attention budget in a screen with
sixty enemies on it. The transient is deliberately shared and deliberately bright; it is the
audio equivalent of the reserved rim colour, and like the rim it is never element-tinted.

ELEMENTS below is a budget, not a suggestion, in exactly the way the colour table is. A new
element means a new row added here, reviewed, and then used - never numbers typed inline at
a call site. The rows are ordered to match the colour table in the art contract.
"""
from __future__ import division

import synth as S

# Ordering matches the element table in .ai/art-direction.md section 3.
ORDER = [
    "fire", "ice", "arcane", "darkness", "light", "grass",
    "earth", "wind", "lightning", "poison", "metal", "water",
]

# The ceiling every element sound is lowpassed under before its tail (2026-10-15, "many noises
# are high pitched and annoying"). The ear is most sensitive from 2 to 5 kHz and almost nothing
# that tells two elements apart lives above 7: what lived there was the shared transient's
# unfiltered hiss and the drive stage's harmonics, played hundreds of times a minute. Impacts get
# the lower one because they fire about ten times as often. A 12 dB/octave slope, so lightning and
# ice still sit on top of the set - they are just no longer a whistle.
CAST_CEILING = 7000.0
IMPACT_CEILING = 5200.0

# f0/f1   - the body's pitch or filter-centre sweep, in Hz (f0 -> f1 over the body)
# c0/c1   - the cutoff sweep of the element's shaping filter
# q       - filter resonance; high q is a narrow, whistling, "tuned" element
# dur     - body length in seconds, before the tail
# tail    - reverb mix, 0 = dry and close, high = distant and large
# grit    - saturation amount; 0 disables the drive stage
ELEMENTS = {
    "fire":      dict(f0=118, f1=46,   c0=1400, c1=200,  q=1.0, dur=0.42, tail=0.14, grit=4.5),
    "ice":       dict(f0=1050, f1=1220, c0=3000, c1=4200, q=2.2, dur=0.34, tail=0.30, grit=0.0),
    "arcane":    dict(f0=430, f1=980,  c0=2000, c1=4000, q=1.4, dur=0.52, tail=0.36, grit=0.0),
    "darkness":  dict(f0=190, f1=62,   c0=760,  c1=140,  q=1.6, dur=0.60, tail=0.34, grit=1.8),
    "light":     dict(f0=784, f1=784,  c0=4200, c1=2400, q=0.8, dur=0.75, tail=0.30, grit=0.0),
    "grass":     dict(f0=300, f1=214,  c0=2000, c1=700,  q=1.8, dur=0.28, tail=0.10, grit=1.6),
    "earth":     dict(f0=150, f1=44,   c0=900,  c1=260,  q=1.0, dur=0.40, tail=0.12, grit=5.0),
    "wind":      dict(f0=1000, f1=2500, c0=900,  c1=2800, q=3.4, dur=0.62, tail=0.22, grit=0.0),
    "lightning": dict(f0=3800, f1=900,  c0=2400, c1=2200, q=1.5, dur=0.24, tail=0.16, grit=2.4),
    "poison":    dict(f0=232, f1=176,  c0=880,  c1=520,  q=3.0, dur=0.50, tail=0.18, grit=1.4),
    "metal":     dict(f0=520, f1=514,  c0=3800, c1=2200, q=1.0, dur=0.85, tail=0.28, grit=0.0),
    "water":     dict(f0=380, f1=1250, c0=1000, c1=300,  q=0.9, dur=0.36, tail=0.20, grit=0.0),
}

# Inharmonic ratio sets. Integer ratios ring like a tuned instrument; these do not, which is
# what makes metal sound struck and ice sound cracked rather than played.
_BELL = [1.0, 2.00, 3.01, 4.17, 5.43]
_METAL = [1.0, 1.71, 2.46, 3.39, 4.51, 5.88]
_ICE = [1.0, 2.76, 5.40, 8.93]


def transient(level=1.0, length=0.008, seed=7):
    """The shared white attack. Identical for every element by contract - do not tint it.

    Still white, still shared, but no longer unbounded. It used to be noise highpassed at
    2600 Hz with nothing above it, so every cast and every impact in the game opened with a
    burst of 10-20 kHz hiss - the single largest source of top end in the set, because it is
    on all twenty-four element files. It now sits in a 1.6-5.5 kHz band, which is still the
    brightest thing in each sound and still reads as "a spell went off".
    """
    n = S.n_samples(length)
    body = S.osc("noise", 0, length, seed=seed)
    body = S.lowpass(S.highpass(body, 1600, 0.7), 5500, 0.7)
    click = S.osc("sine", S.sweep(length, 2400, 700, 0.5), length)
    out = S.mix(S.env_mul(body, S.decay(n, 4.0)), S.gain(S.env_mul(click, S.decay(n, 8.0)), 0.55))
    return S.gain(out, level)


# ---------------------------------------------------------------------------
# per-element bodies
# ---------------------------------------------------------------------------

def _fire(p, dur, seed):
    n = S.n_samples(dur)
    roar = S.bandpass(S.osc("noise", 0, dur, seed=seed), S.sweep(dur, p["c0"], p["c1"], 0.6), p["q"])
    roar = S.gate(roar, 95.0, 0.86, seed=seed + 1)          # the crackle in a flame
    thump = S.osc("sine", S.sweep(dur, p["f0"], p["f1"], 0.5), dur)
    out = S.mix(S.env_mul(roar, S.decay(n, 2.2)), S.gain(S.env_mul(thump, S.expdecay(n, 7.0)), 0.95))
    return S.drive(out, p["grit"])


def _ice(p, dur, seed):
    n = S.n_samples(dur)
    shards = S.partials(S.sweep(dur, p["f0"], p["f1"], 1.0), _ICE, dur,
                        amps=[1.0, 0.6, 0.34, 0.2], decays=[7.0, 11.0, 15.0, 20.0])
    frost = S.bandpass(S.osc("noise", 0, dur, seed=seed), S.sweep(dur, p["c0"], p["c1"], 0.7), p["q"])
    return S.mix(S.gain(shards, 0.8), S.gain(S.env_mul(frost, S.decay(n, 5.0)), 0.45))


def _arcane(p, dur, seed):
    n = S.n_samples(dur)
    f = S.sweep(dur, p["f0"], p["f1"], 0.7)
    a = S.osc("tri", f, dur)
    b = S.osc("tri", [v * 1.007 for v in f], dur, phase=0.31)   # detune, not chorus
    voice = S.mix(S.gain(a, 0.55), S.gain(b, 0.55))
    voice = S.tremolo(voice, 17.0, 0.4)
    voice = S.lowpass(voice, S.sweep(dur, p["c0"], p["c1"], 1.0), p["q"])
    return S.env_mul(voice, S.ad(dur, 0.012, 2.0))


def _darkness(p, dur, seed):
    """A swell that arrives rather than a hit that decays - but only at cast length.

    The reversal is a CAST gesture. An impact has already arrived, and reversing one only to
    multiply it by impact()'s decay produced a short shapeless blob that the confusion audit
    could not tell from wind. Below 0.4 s this plays forwards as a low pitch-drop instead.
    """
    n = S.n_samples(dur)
    swelling = dur >= 0.4
    lo, hi = (p["f1"], p["f0"]) if swelling else (p["f0"], p["f1"])
    sub = S.osc("saw", S.sweep(dur, lo, hi, 1.0), dur)
    air = S.osc("noise", 0, dur, seed=seed)
    # Very little air. Darkness is the absence of the top end, not a different noise colour -
    # at 0.35 the audit could not separate it from wind or arcane on the impact variant.
    voice = S.mix(S.gain(sub, 0.8), S.gain(air, 0.16))
    cut = S.sweep(dur, p["c1"], p["c0"], 1.4) if swelling else S.sweep(dur, p["c0"], p["c1"], 0.6)
    voice = S.lowpass(voice, cut, p["q"])
    voice = S.env_mul(voice, S.decay(n, 2.0))
    if swelling:
        voice = S.reverse(voice)
    return S.drive(voice, p["grit"])


def _light(p, dur, seed):
    n = S.n_samples(dur)
    bell = S.partials(p["f0"], _BELL, dur, amps=[1.0, 0.5, 0.3, 0.18, 0.1],
                      decays=[3.0, 4.5, 6.0, 8.0, 10.0])
    fifth = S.osc("sine", p["f0"] * 1.5, dur)
    out = S.mix(S.gain(bell, 0.8), S.gain(S.env_mul(fifth, S.expdecay(n, 4.0)), 0.3))
    return S.lowpass(out, S.sweep(dur, p["c0"], p["c1"], 1.0), p["q"])


def _grass(p, dur, seed):
    n = S.n_samples(dur)
    pluck = S.osc("saw", S.sweep(dur, p["f0"], p["f1"], 0.4), dur)
    pluck = S.lowpass(pluck, S.sweep(dur, p["c0"], p["c1"], 1.2), p["q"])
    knock = S.env_mul(S.osc("mnoise", 1200, dur, seed=seed), S.decay(n, 14.0))
    out = S.mix(S.env_mul(pluck, S.decay(n, 3.2)), S.gain(knock, 0.4))
    return S.drive(out, p["grit"])


def _earth(p, dur, seed):
    n = S.n_samples(dur)
    thud = S.osc("sine", S.sweep(dur, p["f0"], p["f1"], 0.35), dur)
    grit = S.lowpass(S.osc("noise", 0, dur, seed=seed), S.sweep(dur, p["c0"], p["c1"], 0.8), p["q"])
    grit = S.decimate(grit, 3)                               # rubble, not hiss
    out = S.mix(S.env_mul(thud, S.expdecay(n, 5.0)), S.gain(S.env_mul(grit, S.decay(n, 3.0)), 0.5))
    return S.drive(out, p["grit"])


def _wind(p, dur, seed):
    n = S.n_samples(dur)
    # Two overlapping bandpasses moving in opposite directions: a gust has no pitch centre.
    air = S.osc("noise", 0, dur, seed=seed)
    a = S.bandpass(air, S.sweep(dur, p["c0"], p["c1"], 0.8), p["q"])
    b = S.bandpass(air, S.sweep(dur, p["f1"], p["f0"], 1.4), p["q"] * 1.6)
    out = S.mix(S.gain(a, 0.8), S.gain(b, 0.45))
    # Air has no bass. Without this highpass wind measures as a near-twin of fire in
    # tools/audio/analyse.py, because both are just wide-band noise under an envelope.
    out = S.highpass(out, 520, 0.7)
    return S.env_mul(out, S.ad(dur, 0.09, 1.6))


def _lightning(p, dur, seed):
    n = S.n_samples(dur)
    zap = S.bandpass(S.osc("noise", 0, dur, seed=seed), S.sweep(dur, p["f0"], p["f1"], 2.0), p["q"])
    # The crackle was highpassed noise with no upper bound and measured 90% above 4 kHz; it
    # is now a band. Lightning stays the brightest element, it just stops hissing.
    crackle = S.lowpass(S.highpass(S.osc("noise", 0, dur, seed=seed + 5), p["c0"]), 5500, 0.7)
    crackle = S.gate(crackle, 220.0, 0.5, seed=seed + 9)
    out = S.mix(S.env_mul(zap, S.decay(n, 3.0)), S.gain(S.env_mul(crackle, S.decay(n, 1.6)), 0.55))
    return S.drive(out, p["grit"])


def _poison(p, dur, seed):
    n = S.n_samples(dur)
    gurgle = S.fm("sine", S.sweep(dur, p["f0"], p["f1"], 1.0), 9.0, 0.42, dur, seed=seed)
    gurgle = S.bandpass(gurgle, S.sweep(dur, p["c0"], p["c1"], 1.0), p["q"])
    bubbles = S.gate(S.osc("sine", S.sweep(dur, 700, 320, 1.0), dur), 26.0, 0.4, seed=seed + 3)
    out = S.mix(S.env_mul(gurgle, S.ad(dur, 0.02, 2.2)), S.gain(S.env_mul(bubbles, S.decay(n, 2.0)), 0.3))
    return S.drive(out, p["grit"])


def _metal(p, dur, seed):
    n = S.n_samples(dur)
    struck = S.partials(S.sweep(dur, p["f0"], p["f1"], 1.0), _METAL, dur,
                        amps=[1.0, 0.72, 0.5, 0.36, 0.24, 0.15],
                        decays=[2.2, 3.0, 4.0, 5.0, 6.5, 8.0])
    struck = S.ringmod(struck, 137.0, 0.35)
    return S.lowpass(struck, S.sweep(dur, p["c0"], p["c1"], 1.0), p["q"])


def _water(p, dur, seed):
    n = S.n_samples(dur)
    droplet = S.osc("sine", S.sweep(dur, p["f0"], p["f1"], 2.4), dur)
    wash = S.lowpass(S.osc("noise", 0, dur, seed=seed), S.sweep(dur, p["c0"], p["c1"], 0.9), p["q"])
    # The droplet carries the identity and the wash only sits under it - at equal weight
    # this element measures as arcane, which is the one collision the confusion audit found.
    return S.mix(S.env_mul(droplet, S.expdecay(n, 9.0)), S.gain(S.env_mul(wash, S.decay(n, 2.6)), 0.34))


BODIES = {
    "fire": _fire, "ice": _ice, "arcane": _arcane, "darkness": _darkness,
    "light": _light, "grass": _grass, "earth": _earth, "wind": _wind,
    "lightning": _lightning, "poison": _poison, "metal": _metal, "water": _water,
}


def body(element, dur_scale=1.0, seed=11):
    """The element's voice on its own, without the shared transient or the tail."""
    p = ELEMENTS[element]
    return BODIES[element](p, p["dur"] * dur_scale, seed)


def _finish(buf, tail_mix, ceiling):
    buf = S.lowpass(buf, ceiling, 0.707)
    buf = S.reverb(buf, size=0.55, damp=0.45, mix=tail_mix)
    return S.dc_block(buf)


def cast(element, seed=11):
    """A spell leaving the caster: full transient, full body, full tail."""
    p = ELEMENTS[element]
    voice = body(element, 1.0, seed)
    out = S.layer([(transient(0.7), 0.0, 1.0), (voice, 0.004, 0.9)])
    return _finish(S.pad(out, p["dur"] + 0.18), p["tail"], CAST_CEILING)


def impact(element, seed=23):
    """A spell landing: harder transient, body squeezed to 55%, a low thump under it.

    Impacts fire far more often than casts - one cast can produce a dozen - so they are
    shorter, darker and quieter by design. The mix table in .ai/audio-direction.md holds
    them about 4 dB under their cast.
    """
    p = ELEMENTS[element]
    dur = p["dur"] * 0.55
    voice = body(element, 0.55, seed)
    n = S.n_samples(dur)
    thump = S.env_mul(S.osc("sine", S.sweep(dur, 210, 70, 0.4), dur), S.expdecay(n, 9.0))
    out = S.layer([
        (transient(0.7, 0.006, seed=seed), 0.0, 1.0),
        (S.env_mul(voice, S.decay(len(voice), 2.4)), 0.003, 0.85),
        (thump, 0.0, 0.5),
    ])
    return _finish(S.pad(out, dur + 0.10), p["tail"] * 0.6, IMPACT_CEILING)


# ---------------------------------------------------------------------------
# notes: the Canticle's chord (2026-11-09)
# ---------------------------------------------------------------------------
# The Cumaean Sibyl's spell is sung on the music's beat (.ai/character-fit.md section 5, row 19): every
# cast that charges it adds a note to a chord, voiced by THAT spell's element, and the burst sounds the
# gathered notes together in the chapter's key. So each element gets one more file, note_<element>: its
# construct played as an instrument - a pitch the ear can sing, with the element's identity in the timbre
# and the envelope, never in the pitch.
#
# ONE note per element, all at NOTE_REF_HZ. The game pitches it to the chord's degree at play time
# (AudioStreamPlayer pitch, section 7 of the audio contract: variation at play time, not on disk), and
# folds every degree into A3..A5 (ArcaneExplosion.NotePitch), so the pitch scale runs 0.5 to 2 and twelve
# files serve every key. Built for that range: lowpassed at NOTE_CEILING, so the top of the fold still sits
# under CAST_CEILING; highpassed under the lowest voice, so the bottom of it never reaches the sub band; and
# the shared transient at a third of a cast's, because a chord is up to six of them on one frame.
NOTE_REF_HZ = 440.0
NOTE_DUR = 0.55
NOTE_CEILING = 3400.0
NOTE_FLOOR = 150.0


def _note_fire(f, dur, seed):
    """A reed of fire: a saw on the pitch through the element's falling filter, its crackle gated in."""
    n = S.n_samples(dur)
    p = ELEMENTS["fire"]
    tone = S.lowpass(S.osc("saw", f, dur), S.sweep(dur, p["c0"], p["c1"] * 2.5, 0.6), 1.2)
    roar = S.bandpass(S.osc("noise", 0, dur, seed=seed), S.sweep(dur, p["c0"] * 0.7, p["c1"] * 1.5, 0.6), p["q"])
    roar = S.gate(roar, 95.0, 0.86, seed=seed + 1)
    # Caught at once and burning on, where grass's pluck is struck and gone and wind's breath swells in.
    out = S.mix(S.env_mul(tone, S.ad(dur, 0.006, 1.0)), S.gain(S.env_mul(roar, S.ad(dur, 0.006, 1.2)), 1.0))
    # The whole flame crackles, not only its roar: the gate over the tone too, and the drive's grit.
    return S.drive(S.gate(out, 60.0, 0.9, seed=seed + 2), 3.5)


def _note_ice(f, dur, seed):
    """Struck glass: the element's inharmonic partials on the pitch, a breath of frost over them."""
    n = S.n_samples(dur)
    glass = S.partials(f, _ICE, dur, amps=[1.0, 0.7, 0.45, 0.25], decays=[2.0, 2.6, 3.4, 4.5])
    glass = S.tremolo(glass, 6.0, 0.25)
    frost = S.bandpass(S.osc("noise", 0, dur, seed=seed), 3000.0, 2.2)
    return S.mix(glass, S.gain(S.env_mul(frost, S.ad(dur, 0.12, 1.4)), 0.3))


def _note_arcane(f, dur, seed):
    """The cast's two detuned triangles, held on the pitch, with its 17 Hz shimmer."""
    a = S.osc("tri", f, dur)
    b = S.osc("tri", f * 1.007, dur, phase=0.31)
    voice = S.tremolo(S.mix(S.gain(a, 0.55), S.gain(b, 0.55)), 17.0, 0.35)
    voice = S.lowpass(voice, S.sweep(dur, 1800.0, 3200.0, 1.0), 1.2)
    return S.env_mul(voice, S.ad(dur, 0.02, 1.8))


def _note_darkness(f, dur, seed):
    """Airless, with the octave under it. FORWARDS: a note has to land on its beat, and the reversal
    is a cast's gesture - so it swells in over 50 ms instead of arriving from nowhere."""
    sub = S.mix(S.gain(S.osc("saw", f, dur), 0.6), S.gain(S.osc("saw", f * 0.5, dur, phase=0.2), 0.45))
    sub = S.lowpass(sub, S.sweep(dur, 1100.0, 380.0, 0.8), 1.6)
    return S.env_mul(sub, S.ad(dur, 0.05, 1.5))


def _note_light(f, dur, seed):
    """The bell, struck on the pitch, with its fifth."""
    n = S.n_samples(dur)
    # Pure and long, and touched rather than struck: the fundamental, its octave and the fifth, coming in
    # over 25 ms - the struck clang is metal's.
    bell = S.partials(f, [1.0, 2.0, 3.0], dur, amps=[1.0, 0.3, 0.08], decays=[1.6, 2.6, 4.0])
    fifth = S.env_mul(S.osc("sine", f * 1.5, dur), S.expdecay(n, 2.4))
    out = S.mix(S.gain(bell, 0.8), S.gain(fifth, 0.26))
    na = S.n_samples(0.025)
    return S.env_mul(out, S.ramp(na, 0.0, 1.0) + [1.0] * (n - na))


def _note_grass(f, dur, seed):
    """A plucked string of wood: the saw pluck on the pitch and its knock."""
    n = S.n_samples(dur)
    pluck = S.lowpass(S.osc("saw", f, dur), S.sweep(dur, 1500.0, 450.0, 0.5), 1.8)
    knock = S.env_mul(S.osc("mnoise", 1200, dur, seed=seed), S.decay(n, 14.0))
    return S.mix(S.env_mul(pluck, S.decay(n, 7.0)), S.gain(knock, 0.55))


def _note_earth(f, dur, seed):
    """Struck stone: a bar (partials 1 and 3.93, a marimba's) dying fast, a thud an octave under, rubble."""
    n = S.n_samples(dur)
    bar = S.partials(f, [1.0, 3.93], dur, amps=[1.0, 0.3], decays=[14.0, 22.0])
    thud = S.env_mul(S.osc("sine", S.sweep(dur, f * 0.5, f * 0.35, 0.4), dur), S.expdecay(n, 10.0))
    grit = S.decimate(S.lowpass(S.osc("noise", 0, dur, seed=seed), 900.0, 1.0), 3)
    return S.drive(S.mix(bar, S.gain(thud, 0.8), S.gain(S.env_mul(grit, S.decay(n, 5.0)), 0.7)), 2.5)


def _note_wind(f, dur, seed):
    """A breath over a bottle: the pitch is a narrow band of air, a whisper of its sine under it. Air
    has no bass, so it keeps the cast's highpass."""
    air = S.osc("noise", 0, dur, seed=seed)
    tone = S.bandpass(air, f, 14.0)
    # The breath's own hiss, high over the pitch where the edge of a bottle whistles - a band, not open.
    edge = S.lowpass(S.bandpass(air, 2300.0, 1.6), 3200.0, 0.7)
    sine = S.osc("sine", f, dur)
    out = S.mix(tone, S.gain(edge, 0.5), S.gain(sine, 0.08))
    out = S.highpass(out, 300.0, 0.7)
    # A breath has no attack: it swells to its height half way and goes as it came - a gust. The only
    # note that does.
    n = S.n_samples(dur)
    na = S.n_samples(dur * 0.45)
    return S.env_mul(out, S.ramp(na, 0.0, 1.0, 0.8) + S.decay(n - na, 1.2))


def _note_lightning(f, dur, seed):
    """A buzz that strikes down onto the pitch from the octave over it in 20 ms, and the crackle - as a
    band, never an open highpass."""
    n = S.n_samples(dur)
    nz = S.n_samples(0.02)
    pitch = S.ramp(nz, f * 2.0, f, 0.5) + [f] * (n - nz)
    buzz = S.lowpass(S.osc("square", pitch, dur), 2400.0, 1.0)
    crackle = S.lowpass(S.highpass(S.osc("noise", 0, dur, seed=seed), 2400.0), 3400.0, 0.7)
    crackle = S.gate(crackle, 220.0, 0.5, seed=seed + 9)
    one = S.mix(S.env_mul(buzz, S.decay(n, 6.0)), S.gain(S.env_mul(crackle, S.decay(n, 4.0)), 0.45))
    # It strikes three times, fast and fading: a stutter nothing else in the chord has.
    return S.pad(S.layer([(one, 0.0, 1.0), (one, 0.07, 0.6), (one, 0.15, 0.36)]), dur)


def _note_poison(f, dur, seed):
    """The 9 Hz gurgle as a vibrato a quarter-tone wide - in tune, but wet - nasal through its band,
    with the bubbles."""
    n = S.n_samples(dur)
    voice = S.fm("saw", f, 9.0, 0.025, dur, seed=seed)
    voice = S.bandpass(voice, f * 3.0, 2.4)
    bubbles = S.gate(S.osc("sine", S.sweep(dur, f * 1.6, f * 0.8, 1.0), dur), 26.0, 0.4, seed=seed + 3)
    return S.mix(S.env_mul(voice, S.ad(dur, 0.03, 1.2)), S.gain(S.env_mul(bubbles, S.decay(n, 2.0)), 0.35))


def _note_metal(f, dur, seed):
    """Struck metal on the pitch: the six inharmonic partials, the ring modulation turned down so the
    fundamental still sings."""
    # A clang: the upper partials as loud as the fundamental at the strike and ringing on after it.
    struck = S.partials(f, _METAL, dur, amps=[1.0, 0.9, 0.8, 0.7, 0.55, 0.4],
                        decays=[5.0, 3.0, 2.4, 2.0, 1.8, 1.6])
    return S.ringmod(struck, 137.0, 0.12)


def _note_water(f, dur, seed):
    """A droplet that rises onto the pitch in 30 ms and rings there, over a little of the dark wash."""
    n = S.n_samples(dur)
    nd = S.n_samples(0.03)
    pitch = S.ramp(nd, f * 0.72, f, 0.6) + [f] * (n - nd)
    droplet = S.env_mul(S.osc("sine", pitch, dur), S.expdecay(n, 9.0))
    # A second drop off the first, a fifth of a second on and softer: water falls in drops, and the
    # pair is what keeps it apart from struck stone and glass, which ring once.
    second = S.env_mul(S.osc("sine", pitch, dur), S.expdecay(n, 9.0))
    wash = S.lowpass(S.osc("noise", 0, dur, seed=seed), S.sweep(dur, 900.0, 300.0, 0.9), 0.9)
    out = S.layer([(droplet, 0.0, 1.0), (second, 0.19, 0.55)])
    return S.mix(S.pad(out, dur), S.gain(S.env_mul(wash, S.decay(n, 3.0)), 0.22))


NOTES = {
    "fire": _note_fire, "ice": _note_ice, "arcane": _note_arcane, "darkness": _note_darkness,
    "light": _note_light, "grass": _note_grass, "earth": _note_earth, "wind": _note_wind,
    "lightning": _note_lightning, "poison": _note_poison, "metal": _note_metal, "water": _note_water,
}


def note(element, seed=31):
    """One note of the Canticle's chord in the element's voice, at NOTE_REF_HZ."""
    voice = NOTES[element](NOTE_REF_HZ, NOTE_DUR, seed)
    out = S.layer([(transient(0.3, 0.006, seed=seed), 0.0, 1.0), (voice, 0.003, 1.0)])
    out = S.highpass(S.lowpass(S.pad(out, NOTE_DUR + 0.12), NOTE_CEILING, 0.707), NOTE_FLOOR, 0.707)
    out = S.reverb(out, size=0.45, damp=0.5, mix=ELEMENTS[element]["tail"] * 0.5)
    return S.dc_block(out)
