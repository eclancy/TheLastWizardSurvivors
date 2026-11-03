# -*- coding: utf-8 -*-
"""Every named sound in the game, one function each, plus the registry build.py walks.

Read .ai/audio-direction.md before changing anything here - it holds the mix table, the
naming rules and the reasons behind the loudness targets. The short version:

  * A sound's loudness is set by how OFTEN it plays, not by how important it feels. XP orbs
    are the quietest thing in the game because a run collects a few thousand of them.
  * Anything that can fire more than about four times a second is built short, dark and
    dry. Anything that fires once a run can afford a tail.
  * Variants (_a/_b/_c) exist only where a sound repeats fast enough to machine-gun. They
    differ by seed and by a few Hz, never by design intent.

REGISTRY at the bottom is the single source of truth for what gets written. build.py does
not scan this module for functions - add the entry or the sound does not ship.
"""
from __future__ import division

import math

import palette as P
import synth as S


# ---------------------------------------------------------------------------
# small shared voices
# ---------------------------------------------------------------------------

def _hz(semitones, base=440.0):
    """Equal temperament from A4. The stingers sit in A natural minor, to match the score."""
    return base * (2.0 ** (semitones / 12.0))


def _bell(freq, dur, amps=None, decays=None, seed=1):
    ratios = [1.0, 2.0, 3.01, 4.17]
    return S.partials(freq, ratios, dur,
                      amps=amps or [1.0, 0.42, 0.22, 0.12],
                      decays=decays or [3.0, 4.5, 6.5, 9.0])


def _whoosh(dur, c0, c1, q=2.2, seed=3, curve=1.0):
    # A two-pole bandpass on white noise still leaks a long hiss above its centre; the lowpass
    # is what makes a whoosh air rather than steam.
    air = S.bandpass(S.osc("noise", 0, dur, seed=seed), S.sweep(dur, c0, c1, curve), q)
    air = S.lowpass(air, 5500, 0.707)
    return S.env_mul(air, S.ad(dur, dur * 0.28, 2.0))


def _tick(dur, freq, bright, seed=5):
    """bright is the centre of the click band. It was a bare highpass at 3-5 kHz, which put
    three quarters of every UI tick above 4 kHz - and ui_hover plays on every mouse move."""
    n = S.n_samples(dur)
    click = S.lowpass(S.bandpass(S.osc("noise", 0, dur, seed=seed), bright, 1.1), 5000, 0.707)
    tone = S.osc("sine", S.sweep(dur, freq * 1.6, freq, 0.6), dur)
    return S.mix(S.env_mul(click, S.decay(n, 9.0)), S.gain(S.env_mul(tone, S.decay(n, 6.0)), 0.6))


def _arp(notes, step, note_dur, seed=1, shape="bell"):
    """An ascending or descending run of bells. The only musical construct in the set."""
    items = []
    for i, semi in enumerate(notes):
        f = _hz(semi)
        v = _bell(f, note_dur, seed=seed + i) if shape == "bell" else \
            S.env_mul(S.osc(shape, f, note_dur), S.ad(note_dur, 0.006, 3.0))
        items.append((v, i * step, 1.0 / (1.0 + 0.12 * i)))
    return S.layer(items)


# ---------------------------------------------------------------------------
# elements - generated from the palette, one cast and one impact each
# ---------------------------------------------------------------------------

def _make_cast(el):
    return lambda: P.cast(el)


def _make_impact(el):
    return lambda: P.impact(el)


# ---------------------------------------------------------------------------
# spell shapes that no element table can express
# ---------------------------------------------------------------------------

def spell_explosion():
    """ArcaneExplosion, MeteorImpact - the only sounds allowed to occupy the sub band."""
    dur = 0.9
    n = S.n_samples(dur)
    boom = S.osc("sine", S.sweep(dur, 130, 34, 0.3), dur)
    debris = S.lowpass(S.osc("noise", 0, dur, seed=17), S.sweep(dur, 2600, 300, 0.5), 1.0)
    out = S.layer([
        (P.transient(1.0, 0.010), 0.0, 1.0),
        (S.env_mul(boom, S.expdecay(n, 4.5)), 0.0, 1.0),
        (S.env_mul(debris, S.decay(n, 2.0)), 0.006, 0.55),
    ])
    return S.dc_block(S.reverb(S.drive(out, 2.4), size=0.75, damp=0.5, mix=0.22))


def spell_beam_start():
    """ScorchingRay ignition. Runs straight into spell_beam_loop, so it ends mid-body."""
    dur = 0.28
    n = S.n_samples(dur)
    rise = S.bandpass(S.osc("noise", 0, dur, seed=31), S.sweep(dur, 500, 2400, 1.6), 2.0)
    tone = S.osc("saw", S.sweep(dur, 90, 220, 1.2), dur)
    out = S.mix(S.env_mul(rise, S.ramp(n, 0.1, 1.0, 1.4)),
                S.gain(S.env_mul(tone, S.ramp(n, 0.0, 0.8, 2.0)), 0.6))
    # Lowpassed after the drive, like the loop, but higher: it plays once per beam, and at the
    # loop's 4.5 kHz its rising band measured as a twin of spell_bow_draw.
    return S.lowpass(S.drive(out, 2.0), 6500, 0.707)


def spell_beam_loop():
    """Seamlessly loopable sustain. LOOP SOUND, and three things make the seam vanish:

      * every periodic component completes a whole number of cycles in 0.5 s - 220 and 222 Hz
        (110 and 111 cycles) and the two tremolos at 24 and 8 Hz (12 and 4);
      * it is synthesised at 1.0 s and only the SECOND half is kept, so the biquads have
        settled and the loop does not start with a filter warm-up that the wrap lacks;
      * build.py does not fade it. A fade on a loop IS the click.
    """
    dur = 1.0
    keep = 0.5
    fizz = S.bandpass(S.osc("noise", 0, dur, seed=37), 2100, 1.8)
    tone = S.mix(S.gain(S.osc("saw", 220.0, dur), 0.5), S.gain(S.osc("saw", 222.0, dur), 0.5))
    tone = S.lowpass(tone, 1800, 1.1)
    out = S.mix(S.gain(fizz, 0.55), S.gain(tone, 0.6))
    out = S.tremolo(out, 24.0, 0.28)
    out = S.tremolo(out, 8.0, 0.16)
    out = S.drive(out, 1.8)
    # The drive stage's harmonics are what made a held beam fizz. Filtering the whole second
    # and keeping the settled half keeps the seam exactly as clean as it was.
    out = S.lowpass(out, 4500, 0.707)
    return S.dc_block(out[S.n_samples(dur - keep):])


def spell_beam_end():
    dur = 0.34
    n = S.n_samples(dur)
    fall = S.bandpass(S.osc("noise", 0, dur, seed=41), S.sweep(dur, 2200, 420, 0.8), 2.0)
    tone = S.osc("saw", S.sweep(dur, 210, 70, 0.7), dur)
    out = S.mix(S.env_mul(fall, S.decay(n, 2.4)), S.gain(S.env_mul(tone, S.decay(n, 3.0)), 0.5))
    out = S.lowpass(out, 5000, 0.707)
    return S.dc_block(S.reverb(out, mix=0.18))


def spell_orbit():
    """SpiritualWeapon / OrbitingBlade tick. Fires on a timer for the whole run, so it is
    the second quietest thing in the set after the XP orb, and has no tail at all."""
    dur = 0.17
    n = S.n_samples(dur)
    swipe = S.bandpass(S.osc("noise", 0, dur, seed=43), S.sweep(dur, 2200, 700, 1.0), 3.2)
    swipe = S.lowpass(swipe, 4000, 0.707)
    ring = S.env_mul(S.osc("sine", 820, dur), S.expdecay(n, 12.0))
    # The ring carries more of it than it did: with the swipe pulled down out of the top it was
    # a twin of spell_bow_release, and a tone is the part the bow does not have.
    return S.mix(S.env_mul(swipe, S.decay(n, 6.0)), S.gain(ring, 0.45))


def spell_starfall():
    """One Starfall star landing (Starfall.Land). It plays for every star, hit or miss, at up to
    ten a second behind a 90 ms throttle - so it is built the way pickup_xp is: short, narrow, no
    tail, at the bottom of the mix. A twinkle is a run of glints, not one strike: D7, G6, D6, 28 ms
    apart and falling - the star coming down - over Light's own register (the cast's bell is G5).
    Two earlier versions measured as twins: a falling swish under one glass tone matched spell_orbit
    (0.056, a swipe over a ring), and two glints matched player_shield_absorb (0.065, one struck
    chink) - and a star landing must never sound like the shield taking a hit. Three attacks in 60 ms
    is an envelope nothing else in the set has. No noise; a pure high tone is the trap at this rate
    (section 6), so each glint carries a quiet fifth and everything is lowpassed under 4 kHz. Hits are
    the Light impact, through DealDamageToEnemy, as for any spell."""
    dur = 0.16
    step = 0.028
    items = []
    for k, (semis, level) in enumerate(((17, 0.55), (10, 0.9), (5, 0.7))):
        glint_dur = dur - k * step
        glint = S.partials(_hz(semis, 880.0), [1.0, 1.5], glint_dur, amps=[1.0, 0.25], decays=[12.0, 16.0])
        glint = S.env_mul(glint, S.ad(glint_dur, 0.002, 1.0))
        items.append((glint, k * step, level))
    return S.lowpass(S.pad(S.layer(items), dur), 3800, 0.707)


def spell_familiar_bite():
    """One Familiar bite (Familiar.Bite, and each strike of the Griffin's swoop). A pack of hounds
    bites several times a second, so it is built for that rate: short, dry, lowpassed under 3.6 kHz,
    no tail, at the bottom of the mix with the orbit tick. What makes it a bite and not a hit is the
    envelope - two pitched clacks 24 ms apart, the jaws meeting and the teeth setting, then a short
    huff of breath that RISES after them: the dog in it. Nothing else in the set has a double attack
    that close. The first draft was a falling breath over a falling growl, which is exactly how
    spell_beam_end is built (falling noise over a falling saw) and measured its twin at 0.098; the
    construct changed, not the gain. No bark: a bark is a voice, and a voice at this rate is the thing
    section 5 exists to stop. The damage itself is the Arcane impact, through DealDamageToEnemy."""
    dur = 0.13
    n1 = S.n_samples(0.026)
    clack1 = S.bandpass(S.osc("noise", 0, 0.026, seed=61), 1150, 9.0)
    clack1 = S.lowpass(S.env_mul(clack1, S.expdecay(n1, 9.0)), 3400, 0.707)
    n2 = S.n_samples(0.03)
    clack2 = S.bandpass(S.osc("noise", 0, 0.03, seed=62), 1720, 10.0)
    clack2 = S.lowpass(S.env_mul(clack2, S.expdecay(n2, 8.0)), 3600, 0.707)
    huff = S.bandpass(S.osc("noise", 0, 0.08, seed=63), S.sweep(0.08, 520, 900, 1.0), 1.6)
    huff = S.lowpass(huff, 2000, 0.707)
    huff = S.env_mul(huff, S.ad(0.08, 0.02, 2.0))
    out = S.layer([(clack1, 0.0, 1.0), (clack2, 0.024, 0.9), (huff, 0.04, 0.45)])
    return S.dc_block(S.pad(out, dur))


def spell_stormsweep():
    """One Stormsweep pass (Stormsweep.Tick, every pass of every arc). It replaces the Lightning cast cue
    for its spell rather than sitting on top of it, and at level 8 there are three sweeps every second,
    so it lives in the once-a-second row.

    An arc of storm-light gathers and then discharges, and that order is the construct: 80 ms of air
    rising as the arc leaps out along its band, then the discharge - a few milliseconds of band-limited
    noise falling away in 15 ms - and a short electric buzz behind it, a 90 Hz-gated square on 760 Hz for
    50 ms, as it sweeps. The first draft borrowed Lightning's palette crackle for the tail and measured
    0.025 from cast_lightning and 0.039 from impact_lightning: a cast that sounded like a lightning HIT,
    on the one spell whose hits are lightning hits. The swell into a late attack is what nothing in the
    Lightning rows has. The discharge is centred at 2.4 kHz and the whole lowpassed at 4.5 kHz: at 2.9 and
    5 its hf was 17%, over the 15% a sound that plays more than once a second may carry (section 6, "The
    ceiling")."""
    dur = 0.22
    swing = 0.08
    n = S.n_samples(dur)
    air = S.bandpass(S.osc("noise", 0, swing, seed=211), S.sweep(swing, 500, 1500, 1.4), 2.2)
    air = S.env_mul(air, S.ramp(S.n_samples(swing), 0.0, 1.0, 2.0))
    crack_dur = dur - swing
    m = S.n_samples(crack_dur)
    crack = S.bandpass(S.osc("noise", 0, crack_dur, seed=213), 2400, 1.0)
    crack = S.env_mul(crack, S.expdecay(m, 75.0))
    buzz = S.osc("square", 760.0, 0.05)
    buzz = S.gate(S.lowpass(buzz, 2600, 0.8), 90.0, 0.5, seed=217)
    buzz = S.env_mul(buzz, S.decay(len(buzz), 1.8))
    out = S.layer([
        (air, 0.0, 0.3),
        (crack, swing, 1.0),
        (buzz, swing + 0.012, 0.32),
    ])
    return S.lowpass(S.pad(out, dur), 4500, 0.707)


def spell_sleet_rune():
    """One Sleet Rune leaving the hand (SleetRunes.Release, every rune, behind a 45 ms throttle). At
    level 8 a stream is a rune every 0.05 s, so this is built the way spell_orbit and pickup_xp are:
    short, narrow, no tail, low in the mix. A rune carried off on the wind is a rising breath of air - a
    bandpass sweeping UP, 900 to 2400 Hz, the opposite way to the orbit's falling swipe - that ends on a
    small glassy chime, two of Ice's inharmonic partials struck 45 ms in: the rime of the rune. The swell
    into a late attack is the envelope nothing else in the spell row has; the orbit, the bow and a star
    landing all strike first."""
    dur = 0.12
    n = S.n_samples(dur)
    air = S.bandpass(S.osc("noise", 0, dur, seed=223), S.sweep(dur, 900, 2400, 0.8), 2.6)
    air = S.lowpass(air, 4200, 0.707)
    air = S.env_mul(air, S.ramp(n, 0.0, 1.0, 0.7))
    air = S.env_mul(air, S.ad(dur, 0.05, 2.5))
    tick_dur = dur - 0.045
    tick = S.partials(1720.0, [1.0, 2.76], tick_dur, amps=[1.0, 0.35], decays=[18.0, 24.0])
    tick = S.env_mul(tick, S.ad(tick_dur, 0.001, 1.0))
    out = S.layer([(air, 0.0, 0.8), (tick, 0.045, 0.55)])
    return S.lowpass(S.pad(out, dur), 4200, 0.707)


def spell_summon():
    """BlackTentacles, ThornVine, GuardianVines - something arrives out of the ground."""
    dur = 0.7
    n = S.n_samples(dur)
    earth = P.body("earth", 1.2, seed=47)
    grow = S.osc("saw", S.sweep(dur, 70, 260, 2.0), dur)
    grow = S.lowpass(grow, S.sweep(dur, 400, 1700, 1.4), 2.4)
    out = S.layer([
        (S.env_mul(earth, S.decay(len(earth), 2.0)), 0.0, 0.7),
        (S.env_mul(grow, S.ad(dur, 0.10, 1.6)), 0.05, 0.8),
    ])
    return S.dc_block(S.reverb(S.drive(out, 2.0), size=0.6, mix=0.2))


# Where the crash lands inside spell_maw_open, in seconds from the start of the file.
# BlackTentacles.MawCrashLeadSeconds must match it: the file is started that long before the breach
# (MawBreach of MawOpenSeconds), so the crash lands on the frame the plates are thrown.
MAW_CRASH_AT = 0.20


def spell_maw_open():
    """Elder God Maw breaking up through the earth (BlackTentacles, the maw's summon; Eric, 0.10.0:
    "should make a deep, scary crashing sound when it opens"). It replaces spell_summon for the maw's
    cast and is timed to its three beats, which the shader and BlackTentacles share:

      0.00-0.20 s  the heave: the ground swelling under the slow, a rumble growing out of nothing
                   with two strain cracks in it - stone giving before it goes
      0.20 s       the breach: the CRACK (a band of split stone over a thump falling 110 to 46 Hz)
                   and the rubble thrown up after it, irregularly gated and decimated, as boss_death's
                   collapse is, but a third of a second
      0.26-0.76 s  something huge rising: stone grinding as the maw pushes up, its cutoff climbing,
                   over a sub swelling 40 to 64 Hz - grinding, never a voice (a growl is boss_roar's)
      0.56-1.06 s  the slabs landing on the rim (BlackTentacles' debris flies 0.36-0.5 s): four heavy
                   plates one after another and a bounce, so the envelope steps down in blows
      to 1.5 s     the dust settling, short and low, in a big room

    Deep, so it goes under 80 Hz, which the budget keeps for the explosion, the boss and earth - and
    this is earth breaking. It earns it the way spell_explosion does: it is not a swarm sound (one
    maw every three to four seconds at most, BaseCooldown 4.0), and above 160 Hz it is lowpassed
    hard, so the crack is the only thing in it that reaches the casts' band."""
    rng = __import__("random").Random(811)
    dur = 1.5
    crash = MAW_CRASH_AT

    # The heave: lowpassed noise and a low sine swelling into the breach.
    hd = crash + 0.04
    hn = S.n_samples(hd)
    heave = S.lowpass(S.osc("noise", 0, hd, seed=813), 160.0, 0.9)
    heave = S.mix(heave, S.gain(S.osc("sine", S.sweep(hd, 38.0, 56.0, 1.0), hd), 0.7))
    heave = S.env_mul(heave, S.ramp(hn, 0.0, 1.0, 2.2))
    strain = []
    for k, (t, f) in enumerate(((0.07, 900.0), (0.15, 1200.0))):
        tn = S.n_samples(0.03)
        tick = S.bandpass(S.osc("noise", 0, 0.03, seed=817 + k), f, 2.2)
        strain.append((S.env_mul(S.lowpass(tick, 2400.0), S.decay(tn, 5.0)), t, 0.35 + 0.15 * k))

    # The breach: split stone over the thump. Both short - the crash is one blow, not a swell.
    sd = 0.12
    sn = S.n_samples(sd)
    split = S.bandpass(S.osc("noise", 0, sd, seed=821), S.sweep(sd, 2000.0, 450.0, 0.6), 0.8)
    split = S.env_mul(S.lowpass(split, 3200.0), S.decay(sn, 3.0))
    thump_dur = 0.45
    tn = S.n_samples(thump_dur)
    thump = S.osc("sine", S.sweep(thump_dur, 110.0, 46.0, 0.35), thump_dur)
    thump = S.env_mul(thump, S.expdecay(tn, 5.5))
    rubble_dur = 0.3
    rn = S.n_samples(rubble_dur)
    rubble = S.gate(S.decimate(S.lowpass(S.osc("noise", 0, rubble_dur, seed=823), 1200.0), 4),
                    28.0, 0.6, seed=827)
    rubble = S.env_mul(rubble, S.decay(rn, 2.2))

    # The rise: stone grinding as the maw pushes up through it - low noise chopped irregularly, its
    # cutoff climbing out of the hole, over a sub swelling 40 to 64 Hz. Not a voice: a growl is
    # boss_roar's construct, and the first draft's FM groan measured 0.077 from it.
    gd = 0.28
    gn = S.n_samples(gd)
    grind = S.lowpass(S.osc("noise", 0, gd, seed=829), S.sweep(gd, 120.0, 420.0, 0.8), 1.2)
    grind = S.gate(grind, 22.0, 0.7, seed=833)
    swell = S.osc("sine", S.sweep(gd, 40.0, 64.0, 1.0), gd)
    rise = S.env_mul(S.mix(grind, S.gain(swell, 0.6)), S.ad(gd, 0.06, 1.4, hold=0.08))

    # The slabs: four heavy plates landing one after another on the rim, then a bounce - each its
    # own blow, so the envelope steps down in impacts where every other low sound in the set decays.
    slabs = []
    for k, (t, g) in enumerate(((0.36, 0.95), (0.43, 0.85), (0.5, 0.8), (0.6, 0.55), (0.75, 0.35))):
        ld = 0.13
        ln = S.n_samples(ld)
        body = S.env_mul(S.osc("sine", S.sweep(ld, 130.0, 62.0, 0.5), ld), S.expdecay(ln, 8.0))
        grit = S.env_mul(S.lowpass(S.osc("noise", 0, ld, seed=831 + k), 800.0), S.expdecay(ln, 16.0))
        slabs.append((S.mix(body, S.gain(grit, 0.7)), crash + t + rng.uniform(0.0, 0.02), g))

    # The tail: the dust settling, short and low.
    td = 0.7
    tail = S.lowpass(S.osc("noise", 0, td, seed=837), S.sweep(td, 200.0, 80.0, 0.8), 0.9)
    tail = S.env_mul(tail, S.ad(td, 0.05, 3.2))

    out = S.layer(strain + slabs + [
        (heave, 0.0, 0.35),
        (split, crash, 1.0),
        (thump, crash, 1.0),
        (rubble, crash + 0.012, 0.6),
        (rise, crash + 0.04, 0.45),
        (tail, crash + 0.95, 0.25),
    ])
    # Driven for weight as the explosion is, and lowpassed after the drive (a drive left last is a
    # hiss, audio-direction 6); the highpass at 34 Hz keeps the sub a felt floor, not a DC shove.
    out = S.lowpass(S.drive(S.pad(out, dur), 2.4), 3600.0, 0.707)
    out = S.highpass(out, 34.0, 0.707)
    return S.dc_block(S.reverb(out, size=0.88, damp=0.5, mix=0.3))


def spell_bow_draw():
    """HuntersDraw wind-up. Deliberately 0.45 s so it can telegraph the release.

    The creak sits at 620-1300 Hz, down from 900-1900: once spell_beam_start lost its top end
    the two rising bands measured as twins, and moving the creak down was the softer fix."""
    dur = 0.45
    n = S.n_samples(dur)
    creak = S.bandpass(S.osc("mnoise", S.sweep(dur, 90, 240, 1.0), dur, seed=53),
                       S.sweep(dur, 620, 1300, 1.0), 4.0)
    return S.env_mul(creak, S.ramp(n, 0.05, 1.0, 1.8))


def spell_bow_release():
    dur = 0.3
    n = S.n_samples(dur)
    snap = S.lowpass(S.highpass(S.osc("noise", 0, dur, seed=59), 1400), 4500, 0.707)
    string = S.osc("tri", S.sweep(dur, 320, 150, 0.4), dur)
    out = S.mix(S.env_mul(snap, S.decay(n, 12.0)),
                S.gain(S.env_mul(string, S.expdecay(n, 10.0)), 0.7))
    return S.mix(out, S.gain(_whoosh(dur, 800, 2400, 2.4, seed=61), 0.35))


def spell_sagittarius():
    """Sagittarius's loose (Sagittarius.Loose, .ai/combo-spells.md section 3): every lit star lets fly at
    once, and it is heard ONCE - a bowstring and a glass chord, Light's voice - never once an arrow.

    A loose comes every second or two at most, so it is built for the once-a-second row: short enough,
    no long tail. Two parents in one envelope, the way the spell is two spells in one: the string first, a
    dry snap over a low string falling (Hunter's Draw's release, shorter and without its whoosh, so the
    two are never twins), then 30 ms after it a chord ROLLED upward - G5, B5, D6, 22 ms apart - in glass
    partials (a sine and its inharmonic 2.76), Light's register. cast_light is one bell stack struck at once
    on G5 with a fifth; a rolled major triad after a string is an envelope nothing else in the set has.
    Everything under 4 kHz: a chord of pure high tones is the trap section 6 warns about."""
    dur = 0.6
    snap_dur = 0.05
    snap = S.lowpass(S.highpass(S.osc("noise", 0, snap_dur, seed=211), 1200), 4200, 0.707)
    snap = S.env_mul(snap, S.expdecay(S.n_samples(snap_dur), 30.0))
    string_dur = 0.2
    string = S.osc("tri", S.sweep(string_dur, 250, 120, 0.5), string_dur)
    string = S.env_mul(string, S.expdecay(S.n_samples(string_dur), 13.0))
    items = [(snap, 0.0, 0.75), (string, 0.0, 0.55)]
    for k, semis in enumerate((10, 14, 17)):
        start = 0.03 + k * 0.022
        glass_dur = dur - start
        glass = S.partials(_hz(semis), [1.0, 2.76], glass_dur, amps=[1.0, 0.28], decays=[5.5, 10.0])
        glass = S.env_mul(glass, S.ad(glass_dur, 0.003, 1.0))
        items.append((glass, start, (0.5, 0.42, 0.38)[k]))
    return S.lowpass(S.pad(S.layer(items), dur), 4000, 0.707)


# ---------------------------------------------------------------------------
# the player
# ---------------------------------------------------------------------------

def player_hurt():
    """Fires from Player.TakeDamage step 8, after mitigation - so it means HP actually left."""
    dur = 0.34
    n = S.n_samples(dur)
    thud = S.osc("sine", S.sweep(dur, 210, 62, 0.45), dur)
    tear = S.lowpass(S.osc("noise", 0, dur, seed=67), S.sweep(dur, 2200, 500, 0.7), 1.2)
    out = S.mix(S.env_mul(thud, S.expdecay(n, 7.0)), S.gain(S.env_mul(tear, S.decay(n, 3.0)), 0.6))
    return S.dc_block(S.drive(out, 3.0))


def player_dodge():
    """The dodge roll fully avoids the hit, so this must not sound like a hit at all."""
    dur = 0.26
    return S.gain(_whoosh(dur, 600, 2500, 3.0, seed=71, curve=0.6), 0.9)


def player_heal():
    dur = 0.6
    out = S.layer([
        (_bell(_hz(4), dur, seed=73), 0.0, 0.8),
        (_bell(_hz(11), dur * 0.8, seed=79), 0.09, 0.6),
    ])
    return S.dc_block(S.reverb(S.lowpass(out, 4200, 0.8), mix=0.26))


def player_shield_absorb():
    """Shield pool soak, step 6 of the pipeline. Bright and metallic so it reads as NOT-HP."""
    dur = 0.3
    n = S.n_samples(dur)
    chink = S.partials(1050, [1.0, 1.71, 2.46, 3.39], dur,
                       amps=[1.0, 0.6, 0.36, 0.2], decays=[6.0, 8.0, 10.0, 13.0])
    return S.mix(S.gain(P.transient(0.5, 0.005, seed=83), 1.0), S.gain(chink, 0.8))


def player_shield_break():
    dur = 0.65
    n = S.n_samples(dur)
    shatter = S.partials(S.sweep(dur, 1200, 900, 1.0), [1.0, 2.76, 5.40, 8.93, 12.1], dur,
                         amps=[1.0, 0.7, 0.5, 0.32, 0.2], decays=[3.0, 4.0, 5.5, 7.0, 9.0])
    shatter = S.lowpass(shatter, 6000, 0.707)
    debris = S.lowpass(S.highpass(S.osc("noise", 0, dur, seed=89), 2200), 5500, 0.707)
    debris = S.gate(debris, 60.0, 0.45, seed=97)
    out = S.mix(S.gain(shatter, 0.85), S.gain(S.env_mul(debris, S.decay(n, 2.4)), 0.4))
    return S.dc_block(S.reverb(out, size=0.7, mix=0.3))


def player_death():
    dur = 1.6
    n = S.n_samples(dur)
    fall = S.osc("saw", S.sweep(dur, 220, 38, 1.6), dur)
    fall = S.lowpass(fall, S.sweep(dur, 2000, 260, 1.2), 1.6)
    air = S.lowpass(S.osc("noise", 0, dur, seed=101), S.sweep(dur, 1400, 200, 1.0), 1.0)
    out = S.mix(S.env_mul(fall, S.decay(n, 1.6)), S.gain(S.env_mul(air, S.decay(n, 2.0)), 0.4))
    return S.dc_block(S.reverb(S.drive(out, 2.2), size=0.85, damp=0.35, mix=0.34))


def player_low_health():
    """A two-beat pulse, meant to be played on a repeating timer, not once. Kept narrow-band
    around 520 Hz so it cuts through the music without competing with any spell."""
    dur = 0.55
    beat = 0.2
    n = S.n_samples(beat)
    one = S.env_mul(S.osc("sine", 520, beat), S.ad(beat, 0.01, 3.0))
    two = S.env_mul(S.osc("sine", 392, beat), S.ad(beat, 0.01, 3.0))
    return S.pad(S.layer([(one, 0.0, 1.0), (two, 0.24, 0.85)]), dur)


# ---------------------------------------------------------------------------
# enemies
# ---------------------------------------------------------------------------

def _enemy_hurt(seed, freq):
    """Three of these ship. In a swarm this plays dozens of times a second, so it is short,
    band-limited and quiet - and Player-side code should still rate-limit it."""
    dur = 0.13
    n = S.n_samples(dur)
    hit = S.bandpass(S.osc("noise", 0, dur, seed=seed), S.sweep(dur, freq * 3, freq, 0.6), 2.0)
    body = S.osc("tri", S.sweep(dur, freq, freq * 0.55, 0.5), dur)
    out = S.mix(S.env_mul(hit, S.decay(n, 7.0)), S.gain(S.env_mul(body, S.expdecay(n, 11.0)), 0.55))
    # Lowpassed after the drive: the swarm-rate sound in the game must not carry a fizz.
    return S.lowpass(S.drive(out, 2.0), 3500, 0.707)


def enemy_hurt_a():
    return _enemy_hurt(103, 340)


def enemy_hurt_b():
    return _enemy_hurt(107, 300)


def enemy_hurt_c():
    return _enemy_hurt(109, 378)


def enemy_death_small():
    """Bone clatter. Enemy.StartDeath drops rewards and leaves the group in the same frame,
    so this is allowed to outlast the corpse - it is not gating anything."""
    dur = 0.45
    n = S.n_samples(dur)
    clatter = S.gate(S.bandpass(S.osc("mnoise", 3000, dur, seed=113), 1900, 3.0),
                     42.0, 0.55, seed=127)
    thud = S.osc("sine", S.sweep(dur, 180, 70, 0.5), dur)
    out = S.mix(S.env_mul(clatter, S.decay(n, 2.2)), S.gain(S.env_mul(thud, S.expdecay(n, 9.0)), 0.6))
    return S.dc_block(S.reverb(out, mix=0.14))


def enemy_death_heavy():
    dur = 0.75
    n = S.n_samples(dur)
    collapse = S.osc("sine", S.sweep(dur, 130, 40, 0.4), dur)
    rubble = S.decimate(S.lowpass(S.osc("noise", 0, dur, seed=131), S.sweep(dur, 1500, 320, 0.8)), 3)
    rubble = S.gate(rubble, 30.0, 0.6, seed=137)
    out = S.mix(S.env_mul(collapse, S.expdecay(n, 5.0)), S.gain(S.env_mul(rubble, S.decay(n, 1.8)), 0.55))
    return S.dc_block(S.reverb(S.drive(out, 2.6), size=0.65, mix=0.2))


def enemy_shoot():
    """RangedEnemy / EnemyProjectile. Dry and pitched high, so it never reads as the player."""
    dur = 0.2
    n = S.n_samples(dur)
    pop = S.osc("square", S.sweep(dur, 760, 320, 0.4), dur)
    # A raw square carries odd harmonics to Nyquist; that was half this sound above 4 kHz.
    pop = S.lowpass(pop, 3000, 0.707)
    air = S.bandpass(S.osc("noise", 0, dur, seed=139), S.sweep(dur, 2000, 900, 0.8), 2.6)
    return S.mix(S.env_mul(pop, S.decay(n, 8.0)), S.gain(S.env_mul(air, S.decay(n, 5.0)), 0.45))


def enemy_melee():
    """A contact hit landing on the player. Pairs with player_hurt; this is the attacker."""
    dur = 0.22
    n = S.n_samples(dur)
    swing = _whoosh(0.14, 1800, 600, 2.4, seed=149)
    hit = S.osc("sine", S.sweep(dur, 260, 90, 0.4), dur)
    out = S.layer([(swing, 0.0, 0.5), (S.env_mul(hit, S.expdecay(n, 10.0)), 0.05, 1.0)])
    return S.drive(out, 2.4)


def elite_spawn():
    """The gold-rim elite. A struck metal tone played backwards, so it arrives rather than
    lands - the only enemy cue with an upward gesture.

    It sits an octave and a half above boss_roar on purpose. Both are "something worse is
    here" and the first version of this shared boss_roar's register, which made every elite
    read as a boss for the half second before the sprite resolved."""
    dur = 0.9
    n = S.n_samples(dur)
    gong = S.reverse(P.body("metal", 1.0, seed=151))
    rise = S.bandpass(S.osc("noise", 0, dur, seed=153), S.sweep(dur, 800, 2600, 1.6), 2.8)
    out = S.layer([
        (S.gain(gong, 0.8), 0.0, 1.0),
        (S.env_mul(rise, S.ramp(n, 0.0, 1.0, 2.2)), 0.0, 0.45),
    ])
    return S.dc_block(S.reverb(out, size=0.8, mix=0.3))


def boss_roar():
    dur = 1.8
    n = S.n_samples(dur)
    growl = S.fm("saw", S.sweep(dur, 62, 46, 1.0), 27.0, 0.5, dur, seed=157)
    growl = S.lowpass(growl, S.sweep(dur, 1400, 380, 1.0), 1.4)
    rasp = S.bandpass(S.osc("noise", 0, dur, seed=163), S.sweep(dur, 900, 400, 1.0), 2.0)
    out = S.mix(S.env_mul(growl, S.ad(dur, 0.18, 1.4)), S.gain(S.env_mul(rasp, S.ad(dur, 0.3, 1.6)), 0.4))
    return S.dc_block(S.reverb(S.drive(out, 3.4), size=0.9, damp=0.3, mix=0.34))


def boss_death():
    dur = 2.4
    n = S.n_samples(dur)
    collapse = S.osc("saw", S.sweep(dur, 110, 26, 1.8), dur)
    collapse = S.lowpass(collapse, S.sweep(dur, 1600, 160, 1.4), 1.8)
    rubble = S.gate(S.decimate(S.lowpass(S.osc("noise", 0, dur, seed=167), 1200), 4),
                    22.0, 0.55, seed=173)
    out = S.layer([
        (P.transient(1.0, 0.012), 0.0, 1.0),
        (S.env_mul(collapse, S.decay(n, 1.5)), 0.0, 1.0),
        (S.env_mul(rubble, S.decay(n, 1.2)), 0.03, 0.5),
    ])
    return S.dc_block(S.reverb(S.drive(out, 3.0), size=0.95, damp=0.3, mix=0.36))


# ---------------------------------------------------------------------------
# pickups and progression
# ---------------------------------------------------------------------------

def pickup_xp():
    """The most-played sound in the game by an order of magnitude. Everything about it is a
    concession to that: 90 ms, one narrow band, no tail, and the quietest target in the mix.
    Vary it at PLAY time with AudioStreamPlayer2D.PitchScale, not with more files."""
    dur = 0.09
    n = S.n_samples(dur)
    # About a fourth lower than it was (760-1140 Hz, from 1050-1580), because the streak ladder in
    # SfxPlayer.PickupXp pitches it up to 1.4x on top: at the old register a long streak ended
    # above 2 kHz as a pure sine, which is the definition of a sound that grates. The 6 ms
    # attack rounds the onset off without losing the tick.
    blip = S.osc("sine", S.sweep(dur, 760, 1140, 0.5), dur)
    return S.env_mul(blip, S.ad(dur, 0.006, 3.0))


def pickup_health():
    """Deliberately NOT a bell. Every other reward in the game is one, and the audit had this
    measuring as a small chest_open; a soft filtered glow keeps HP in its own timbre."""
    dur = 0.42
    n = S.n_samples(dur)
    a = S.env_mul(S.osc("sine", _hz(0), dur), S.ad(dur, 0.05, 2.0))
    b = S.env_mul(S.osc("tri", _hz(7), dur), S.ad(dur, 0.07, 2.2))
    warm = S.mix(S.gain(a, 0.7), S.gain(b, 0.45))
    return S.dc_block(S.lowpass(warm, S.sweep(dur, 2600, 1100, 1.0), 0.9))


def pickup_magnet():
    """FortunesFavor / vacuum effects. Six accelerating blips rather than one smooth sweep:
    the event is many orbs arriving, and a glide measured as a slower pickup_health."""
    items = []
    t = 0.0
    for i in xrange(6):
        blip = S.env_mul(S.osc("sine", S.sweep(0.07, 520 + i * 170, 760 + i * 210, 0.5), 0.07),
                         S.ad(0.07, 0.003, 3.0))
        items.append((blip, t, 0.55 + 0.09 * i))
        t += 0.075 - 0.008 * i          # accelerating, so it reads as being pulled in
    return S.dc_block(S.layer(items))


def pickup_haste():
    """The speed potion (BuffItem): attack and move speed for eight seconds.

    A gust that passes you twice - two short bandpassed swishes, the second quicker and higher,
    which is how wind sounds when you are the thing moving - and then a shimmer that climbs: five
    triangle grains a step apart under a fast tremolo. "Faster" has to be its own gesture: the
    health glow is warm and still, the magnet is blips arriving, and one smooth whoosh on its own
    is player_dodge.

    It drops off about one kill in a hundred, so it may sit above the swarm, but every layer is
    lowpassed - an open whoosh is a hiss to Nyquist (.ai/audio-direction.md, "The ceiling")."""
    first = _whoosh(0.16, 650, 1800, 2.6, seed=401, curve=0.7)
    second = _whoosh(0.12, 950, 2500, 2.6, seed=409, curve=0.6)
    grains = []
    for i, semi in enumerate([7, 12, 16, 19, 24]):
        dur = 0.12
        f = _hz(semi)
        g = S.osc("tri", S.sweep(dur, f, f * 1.03, 1.0), dur)
        grains.append((S.env_mul(g, S.ad(dur, 0.004, 2.6)), 0.032 * i, 0.75 - 0.08 * i))
    shimmer = S.tremolo(S.lowpass(S.layer(grains), 4200, 0.707), 23.0, 0.45)
    out = S.layer([
        (first, 0.0, 0.85),
        (second, 0.085, 0.8),
        (shimmer, 0.13, 0.7),
    ])
    return S.dc_block(S.reverb(out, size=0.55, mix=0.2))


def event_reward():
    """A side event paid out a chest (SideEvent.Succeed), and the chest rises out of the ground as
    a ghost before it turns solid. Phantasmal, not triumphant.

    A breath that swells - noise through two slow formant bands, the sound of a voice that is not
    quite there - and resolves into one glass tone with a slow vibrato and a fifth under it. Not a
    bell run (level_up, chest_open, stage_clear are all bell runs) and not a stacked chord
    (meta_unlock, which still plays when an event grants an unlock instead, since that is the save
    changing). One a run, so it sits in the once-a-minute band, not with the stingers."""
    dur = 1.45
    n = S.n_samples(dur)
    noise = S.osc("noise", 0, dur, seed=419)
    low = S.bandpass(noise, S.sweep(dur, 480, 820, 1.0), 3.2)
    high = S.bandpass(S.osc("noise", 0, dur, seed=421), S.sweep(dur, 1250, 1900, 1.0), 4.0)
    breath = S.lowpass(S.mix(low, S.gain(high, 0.55)), 3000, 0.707)
    na = S.n_samples(0.62)
    env = S.ramp(na, 0.0, 1.0, 2.2) + S.decay(n - na, 1.6)
    breath = S.env_mul(breath, env)

    tone_dur = 0.95
    tn = S.n_samples(tone_dur)
    base = _hz(19)
    vib = [base * (1.0 + 0.007 * math.sin(2.0 * math.pi * 5.2 * i / S.SR)) for i in xrange(tn)]
    glass = S.env_mul(S.osc("sine", vib, tone_dur), S.ad(tone_dur, 0.05, 2.0))
    under = S.env_mul(S.osc("sine", _hz(12), tone_dur), S.ad(tone_dur, 0.09, 2.4))
    tone = S.lowpass(S.mix(glass, S.gain(under, 0.5)), 3800, 0.707)
    out = S.layer([
        (breath, 0.0, 0.8),
        (tone, 0.5, 0.75),
    ])
    return S.dc_block(S.reverb(out, size=0.85, mix=0.38))


def bearing_found():
    """A wayfinder taken (WayfinderPickup.Take): the Arcane Compass, the Gaoler's map or the
    bearing cairn. From then on every place the chapter hides is marked at the edge of the screen
    (.ai/side-events.md, "Finding the event").

    A needle swinging and settling: five dry ticks, each later and a little lower than the last -
    a pivot losing speed - and then one open fifth, low and soft, that holds while the arrows come
    up. Not a bell run (level_up, chest_open and stage_clear are bell runs), not the breath of
    event_reward, and the ticks DECELERATE where pickup_magnet's blips accelerate, which is the
    whole difference between "something found its bearing" and "something is being pulled in".
    Once a run, so it may have a tail; every layer is lowpassed (the ceiling, audio-direction 6)."""
    ticks = []
    t = 0.0
    gap = 0.045
    for i in range(5):
        dur = 0.05
        n = S.n_samples(dur)
        click = S.lowpass(S.bandpass(S.osc("noise", 0, dur, seed=431 + i), 1400 - 100 * i, 2.2), 2400, 0.707)
        body = S.osc("tri", S.sweep(dur, 980 - 60 * i, 760 - 50 * i, 0.6), dur)
        # The triangle's odd harmonics run to Nyquist; the lowpass after the mix is what keeps the
        # ticks a wooden click and not a hiss (audio-direction 6, "The ceiling").
        tick = S.lowpass(S.mix(S.env_mul(click, S.decay(n, 10.0)), S.gain(S.env_mul(body, S.decay(n, 7.0)), 0.5)), 2800, 0.707)
        ticks.append((tick, t, 0.8 - 0.08 * i))
        t += gap
        gap *= 1.45          # slowing down: the needle coming to rest
    settle = t + 0.04

    hold = 1.0
    hn = S.n_samples(hold)
    root = S.env_mul(S.osc("sine", _hz(-5), hold), S.ad(hold, 0.08, 2.2))
    fifth = S.env_mul(S.osc("sine", _hz(2), hold), S.ad(hold, 0.12, 2.5))
    shimmer = S.env_mul(S.osc("tri", _hz(14), hold), S.ad(hold, 0.2, 3.2))
    chord = S.lowpass(S.mix(root, S.gain(fifth, 0.7), S.gain(shimmer, 0.18)), 2600, 0.707)
    out = S.layer([
        (S.layer(ticks), 0.0, 0.9),
        (chord, settle, 0.8),
    ])
    return S.dc_block(S.reverb(out, size=0.7, mix=0.28))


def level_up():
    """A4 - C5 - E5 - A5, the A-minor triad the score sits in. The one unmissable stinger."""
    out = _arp([0, 3, 7, 12], 0.085, 0.7, seed=191)
    return S.dc_block(S.reverb(out, size=0.7, mix=0.3))


def chest_open():
    dur = 1.1
    n = S.n_samples(dur)
    creak = S.bandpass(S.osc("mnoise", S.sweep(dur * 0.4, 60, 150, 1.0), dur * 0.4, seed=193),
                       S.sweep(dur * 0.4, 700, 1500, 1.0), 5.0)
    reveal = _arp([0, 7, 12, 16], 0.07, 0.75, seed=197)
    out = S.layer([
        (S.env_mul(creak, S.ad(dur * 0.4, 0.05, 1.4)), 0.0, 0.55),
        (reveal, 0.30, 0.9),
    ])
    return S.dc_block(S.reverb(out, size=0.75, mix=0.3))


def card_appear():
    """One per card dealt in LevelUpMenu / ChestItemSelectionMenu. Three fire in sequence,
    so it has to survive being heard three times in 200 ms.

    Pure high-band flick with no pitched component at all - it used to carry a tone and
    measured as a small ui_open. The menu opening and a card landing inside it must not be
    the same gesture, because they play four hundred milliseconds apart."""
    dur = 0.18
    # Kept high on purpose - it is the one UI sound allowed above 4 kHz, because below that it
    # measures as ui_close. It is lowpassed at 7 kHz rather than left open, and plays three
    # times per level-up, not three times a second.
    paper = S.bandpass(S.osc("noise", 0, dur, seed=199), S.sweep(dur, 1800, 4600, 1.4), 3.4)
    paper = S.lowpass(S.highpass(paper, 1400, 0.7), 7000, 0.707)
    return S.env_mul(paper, S.ad(dur, 0.012, 4.0))


def card_select():
    dur = 0.55
    out = S.layer([
        (_bell(_hz(3), dur, seed=211), 0.0, 0.8),
        (_bell(_hz(10), dur * 0.9, seed=223), 0.05, 0.7),
    ])
    return S.dc_block(S.reverb(out, mix=0.24))


def reroll():
    """A riffle: five irregular ticks, each a little higher. Reads as "these are different"."""
    items = []
    for i in xrange(5):
        t = _tick(0.09, 560 + i * 110, 2400, seed=227 + i)
        items.append((t, 0.035 * i + 0.004 * (i % 2), 0.9 - 0.08 * i))
    return S.dc_block(S.layer(items))


def spell_evolve():
    """Spell evolution - rarer than a level-up, so it is allowed to be longer and brighter.

    Swell first, bells last. Led by the arpeggio it was a longer level_up, and those two fire
    within seconds of each other at the moment a spell evolves; leading with a rising arcane
    shimmer makes the ear hear a transformation with a result, not a second level."""
    dur = 1.4
    n = S.n_samples(dur)
    shimmer = S.tremolo(P.body("arcane", 2.7, seed=233), 11.0, 0.45)
    shimmer = S.env_mul(shimmer, S.ramp(len(shimmer), 0.15, 1.0, 1.6))
    bells = _arp([12, 19, 24], 0.09, 0.9, seed=229)
    out = S.layer([(shimmer, 0.0, 0.9), (bells, 0.62, 1.0)])
    return S.dc_block(S.reverb(out, size=0.8, mix=0.34))


# ---------------------------------------------------------------------------
# interface
# ---------------------------------------------------------------------------

def ui_hover():
    """Plays on every mouse move across a button. Must be nearly subliminal."""
    return _tick(0.05, 1000, 3000, seed=239)


def ui_click():
    dur = 0.11
    n = S.n_samples(dur)
    t = _tick(dur, 800, 2600, seed=241)
    body = S.env_mul(S.osc("tri", S.sweep(dur, 620, 480, 0.6), dur), S.decay(n, 7.0))
    return S.mix(t, S.gain(body, 0.5))


def ui_back():
    """The same gesture as ui_click, a fourth lower. Direction is the whole message."""
    dur = 0.12
    n = S.n_samples(dur)
    t = _tick(dur, 560, 1900, seed=251)
    body = S.env_mul(S.osc("tri", S.sweep(dur, 420, 330, 0.6), dur), S.decay(n, 7.0))
    return S.mix(t, S.gain(body, 0.5))


def ui_denied():
    """Locked stage, unaffordable upgrade. A dull buzz, never a musical interval."""
    dur = 0.24
    n = S.n_samples(dur)
    buzz = S.osc("square", 118, dur)
    buzz = S.lowpass(buzz, 900, 1.2)
    return S.env_mul(buzz, S.ad(dur, 0.006, 2.0, hold=0.09))


def ui_open():
    dur = 0.35
    return S.mix(S.gain(_whoosh(dur, 500, 2200, 2.0, seed=257), 0.7),
                 S.gain(_bell(_hz(7), dur, seed=263), 0.35))


def ui_close():
    dur = 0.3
    return S.mix(S.gain(_whoosh(dur, 2200, 500, 2.0, seed=269), 0.7),
                 S.gain(_bell(_hz(0), dur * 0.8, seed=271), 0.3))


def ui_pause():
    dur = 0.3
    n = S.n_samples(dur)
    drop = S.osc("tri", S.sweep(dur, 520, 260, 1.0), dur)
    return S.dc_block(S.env_mul(drop, S.ad(dur, 0.01, 2.4)))


# ---------------------------------------------------------------------------
# run and meta stingers
# ---------------------------------------------------------------------------

def stage_clear():
    out = _arp([0, 4, 7, 12, 16, 19], 0.11, 1.3, seed=277)
    return S.dc_block(S.reverb(out, size=0.85, mix=0.34))


def game_over():
    """Descending, unresolved, and long enough to sit under the GameOverScreen appearing."""
    dur = 2.2
    out = _arp([-12, -15, -17, -22], 0.30, 1.6, seed=281)
    drone = S.env_mul(S.osc("saw", S.sweep(dur, 55, 51, 1.0), dur), S.ad(dur, 0.4, 1.2))
    drone = S.lowpass(drone, 700, 1.4)
    out = S.layer([(out, 0.0, 1.0), (drone, 0.0, 0.45)])
    return S.dc_block(S.reverb(out, size=0.9, damp=0.35, mix=0.36))


# ---------------------------------------------------------------------------
# the defeat and the payout (2026-11-02, Eric: "Defeat should have a dramatic animation where
# the game freezes, then crash zooms onto the wizard and they fall, then it says defeat ... It
# then tallys up the reward with a satisfying payout sound.")
# ---------------------------------------------------------------------------

def player_fall():
    """The wizard reaching the ground at the end of his fall (DefeatSequence, the landing beat),
    about a second and a half after player_death has carried the killing blow. The defeat's one
    body sound: cloth and a staff on the ground, not stone breaking.

    TWO impacts, not one: the knees, then the body 130 ms later and heavier - the double hit is
    the fall's whole shape, and the first draft (one thump under a noise sweep) measured 0.098 from
    player_hurt, which is one thump under a noise sweep. Each thump stops above 85 Hz (the sub band
    belongs to the boss and the explosions, audio-direction 6). Then a scatter of grit, one dry
    wooden knock as the staff lands after him, and the dust settling. Lowpassed under 3 kHz
    throughout. Once a run."""
    kd = 0.16
    knees = S.env_mul(S.osc("sine", S.sweep(kd, 190, 120, 0.5), kd), S.expdecay(S.n_samples(kd), 14.0))
    bd = 0.34
    body = S.env_mul(S.osc("sine", S.sweep(bd, 150, 86, 0.5), bd), S.expdecay(S.n_samples(bd), 9.0))
    gd = 0.3
    grit = S.gate(S.lowpass(S.bandpass(S.osc("mnoise", 2200, gd, seed=547), 1500, 2.5), 2800, 0.707),
                  34.0, 0.45, seed=557)
    grit = S.env_mul(grit, S.decay(S.n_samples(gd), 2.4))
    sd = 0.09
    staff = S.lowpass(S.env_mul(S.osc("tri", S.sweep(sd, 520, 410, 0.6), sd), S.expdecay(S.n_samples(sd), 7.0)),
                      1800, 0.707)
    dd = 0.5
    dust = S.lowpass(S.osc("noise", 0, dd, seed=541), 1100, 0.707)
    dust = S.env_mul(dust, S.adsr(dd, 0.06, 0.1, 0.5, 0.3))
    out = S.layer([
        (knees, 0.0, 0.7),
        (body, 0.13, 1.0),
        (grit, 0.15, 0.3),
        (staff, 0.24, 0.42),
        (dust, 0.16, 0.12),
    ])
    return S.dc_block(S.reverb(S.lowpass(out, 3000, 0.707), size=0.55, damp=0.5, mix=0.16))


def _coin(freq, dur, seed):
    """One small struck coin: two inharmonic partials over a 3 ms grain. Shared by the tally's tick
    and the payout's spill, so the spill is heard as the same coins the count was counting."""
    n = S.n_samples(dur)
    ring = S.partials(freq, [1.0, 2.41, 3.17], dur, amps=[1.0, 0.42, 0.18], decays=[5.0, 7.0, 9.0])
    grain = S.lowpass(S.bandpass(S.osc("noise", 0, dur, seed=seed), 2100, 1.6), 3200, 0.707)
    return S.mix(S.env_mul(ring, S.ad(dur, 0.0015, 1.0)), S.gain(S.env_mul(grain, S.expdecay(n, 30.0)), 0.5))


def tally_tick():
    """One step of the Arcane Energy count on the results screen (GameOverScreen's tally): up to
    eighteen a second for a second and a half, and pitched up the count at PLAY time
    (SfxPlayer.TallyTick, 1.0 to 1.26 - four semitones), so it is built like the orb: short, one
    narrow band, no tail. A coin, not pickup_xp's sine sweep: the count is money, the orb is
    experience. A coin landing on the heap: struck, and struck again 22 ms later as it settles,
    and left RINGING - no noise grain, and a slow linear decay where a click falls away at once.
    Three drafts were a click: one strike over the grain (0.051 from ui_click, which is exactly a
    noise tick over a short tone), a short double strike on a 1.1 kHz coin (0.069 from ui_click),
    and the same rung at 1.5 kHz (0.022 from ui_hover, whose tone sweeps through that octave).
    What a click does not have is the ring, so this one rings for 0.22 s on A5 (the score's key)
    with one inharmonic partial at 2.76 - 0.40 from every interface sound. Under 3.8 kHz before
    the ladder lifts it (the ceiling, audio-direction 6)."""
    dur = 0.22
    ring = S.partials(_hz(12), [1.0, 2.76], dur, amps=[1.0, 0.25], decays=[2.0, 4.0])
    hit = S.env_mul(ring, S.ad(dur, 0.0015, 1.0))
    out = S.layer([(hit, 0.0, 1.0), (hit, 0.022, 0.55)])
    return S.lowpass(out, 3800, 0.707)


def tally_payout():
    """The count landing on its figure (GameOverScreen's tally): the run's Arcane Energy banked.

    Three layers in about 1.4 s: a purse set down (a short thump, 160 to 95 Hz, above the sub
    band), a spill of eleven of tally_tick's coins over 0.3 s - each a little lower and quieter, a
    heap settling - and over them one struck MAJOR chord, C E G and the C above, all at once.
    Every other reward in the set is a bell RUN (level_up, chest_open, stage_clear) or a swell
    (meta_unlock); a chord struck together is the difference between "something arrived" and "it
    is yours". C major is A minor's relative, so it sits in the score's key and still resolves
    game_over's descent. Lowpassed under 4 kHz."""
    rng = __import__("random").Random(571)
    spill = []
    for i in range(11):
        at = 0.012 + 0.30 * ((i / 10.0) ** 1.6) + rng.uniform(-0.008, 0.008)
        freq = 1260.0 - 32.0 * i + rng.uniform(-40.0, 40.0)
        spill.append((_coin(freq, 0.14, 577 + i), max(0.0, at), 0.78 - 0.045 * i))
    hold = 1.25
    chord = S.mix(_bell(_hz(3), hold), S.gain(_bell(_hz(7), hold), 0.8),
                  S.gain(_bell(_hz(10), hold), 0.7), S.gain(_bell(_hz(15), hold), 0.42))
    td = 0.26
    thump = S.env_mul(S.osc("sine", S.sweep(td, 160, 95, 0.5), td), S.expdecay(S.n_samples(td), 9.0))
    out = S.layer([
        (thump, 0.0, 0.75),
        (S.layer(spill), 0.0, 0.62),
        (chord, 0.035, 0.85),
    ])
    return S.dc_block(S.reverb(S.lowpass(out, 4000, 0.707), size=0.75, damp=0.4, mix=0.28))


def meta_unlock():
    """A character or stage unlocked on the save file - the one sound that means the SAVE
    changed, not the run.

    Built from sustained stacked fifths rather than the bell arpeggio every other reward
    uses. stage_clear and this fire minutes apart at the end of a winning run, and as two
    ascending bell runs they measured 0.03 apart; a swelling chord against a struck one is
    the difference between "you finished" and "you earned something"."""
    dur = 1.7
    n = S.n_samples(dur)
    voices = []
    for k, semi in enumerate([-12, -5, 0, 7, 12]):
        v = S.osc("tri", _hz(semi), dur)
        voices.append(S.gain(S.env_mul(v, S.ad(dur, 0.22 + 0.06 * k, 1.1)), 0.9 / (1 + k * 0.4)))
    chord = S.lowpass(S.mix(*voices), S.sweep(dur, 1200, 4200, 0.8), 0.9)
    cap = _bell(_hz(24), 0.9, seed=283)
    out = S.layer([(chord, 0.0, 1.0), (cap, 0.55, 0.5)])
    return S.dc_block(S.reverb(out, size=0.85, mix=0.36))


def wave_warning():
    """A wave or boss inbound. Low, slow, and deliberately musical so it is not a hit."""
    dur = 1.3
    n = S.n_samples(dur)
    horn = S.mix(S.gain(S.osc("saw", _hz(-24), dur), 0.6), S.gain(S.osc("saw", _hz(-17), dur), 0.4))
    horn = S.lowpass(horn, S.sweep(dur, 600, 1400, 1.0), 1.6)
    return S.dc_block(S.reverb(S.env_mul(horn, S.ad(dur, 0.25, 1.4)), size=0.8, mix=0.3))


# ---------------------------------------------------------------------------
# chapter hints - something out there is still his to lose
# ---------------------------------------------------------------------------
#
# Partway through a run, while a chapter still holds a wizard or a spell this save has not carried
# out, a cue comes from the direction of it (StageHintDirector, .ai/side-events.md "Hints"). One per
# chapter. They are FAR AWAY by construction: every one is lowpassed, highpassed above the sub band
# and put in a room, so it reads as heard across the map rather than played in the player's ear,
# and the game plays it a few hundred pixels off toward the source so it pans that way.
#
# No recorded voice: the cry and the song are formant synthesis, a buzz through the resonances a
# vowel has, phrased like speech. The caption carries the words. Four a run at most, so they sit at
# the quiet end of the once-a-minute band; they should be noticed, never startle.

def _vowel(f0, dur, formants, vibrato=0.012, rate=5.6, attack=0.03, release=0.08, seed=1):
    """One voiced syllable: a vibrato saw and pulse through formant bandpasses.

    formants is [(centre, q, gain), ...]; a centre may be a curve, which is what turns one vowel
    into the next inside a syllable ("there?" is two). f0 may be a curve too - a question rises."""
    n = S.n_samples(dur)
    f = S._ctl(f0, n)
    w = 2.0 * math.pi * rate / S.SR
    pitch = [f[i] * (1.0 + vibrato * math.sin(w * i + seed)) for i in xrange(n)]
    src = S.mix(S.gain(S.osc("saw", pitch, dur), 0.65),
                S.gain(S.osc("pulse", pitch, dur, duty=0.3, phase=0.3), 0.35))
    parts = [S.gain(S.bandpass(src, c, q), g) for c, q, g in formants]
    v = S.mix(*parts)
    return S.env_mul(v, S.adsr(dur, a=attack, d=0.05, s=0.85, r=release))


def _breath(dur, centres, q=1.2, seed=1):
    """An unvoiced syllable: noise through the same resonances, which is all a whisper is."""
    noise = S.osc("noise", 0, dur, seed=seed)
    return S.mix(*[S.gain(S.bandpass(noise, c, q), g) for c, g in centres])


def _far(buf, low=200.0, high=2400.0, size=0.8, mix=0.4, damp=0.5):
    """Heard across the map: the top and the bottom gone, and a room round it."""
    return S.reverb(S.lowpass(S.highpass(buf, low), high), size=size, damp=damp, mix=mix)


def hint_cell_cry():
    """The Cursed Dungeon: a woman's voice from the cells - "Help! Is someone there?"

    The caption says the words; this is their tune. One strong called syllable rising on its
    vowel, a breath, then three quick ones falling and a last that lifts into a question - the
    melody of the sentence with no consonants. A woman's register, called rather than spoken (B-flat 4 rising to C-sharp 5), an open "eh" (F1
    600, F2 1900), and a long stone room with a slap off the corridor walls. Formant synthesis
    is the one honest way this pipeline has to suggest a person (event_reward's breath is the
    nearest thing in the set, and it is unvoiced)."""
    eh = [(600.0, 4.0, 1.0), (1900.0, 6.0, 0.8), (2800.0, 8.0, 0.3)]
    help_ = _vowel(S.sweep(0.34, 466.0, 554.0, 0.7), 0.34, eh, attack=0.025, release=0.06, seed=3)
    some = _vowel(S.sweep(0.13, 415.0, 392.0), 0.13, [(560.0, 3.0, 1.0), (1500.0, 5.0, 0.5)], seed=5)
    one = _vowel(S.sweep(0.13, 392.0, 370.0), 0.13, [(500.0, 3.0, 1.0), (1100.0, 5.0, 0.5)], seed=7)
    there = _vowel(S.sweep(0.52, 370.0, 587.0, 1.8), 0.52,
                   [(S.sweep(0.52, 620.0, 560.0), 3.0, 1.0), (S.sweep(0.52, 1850.0, 1450.0), 5.0, 0.55),
                    (2700.0, 7.0, 0.18)], vibrato=0.016, release=0.16, seed=9)
    phrase = S.layer([
        (help_, 0.0, 1.0),
        (some, 0.50, 0.7),
        (one, 0.64, 0.7),
        (there, 0.78, 0.85),
    ])
    phrase = S.pad(phrase, 2.3)
    slap = S.delay(phrase, 0.19, feedback=0.35, mix=0.28, taps=4)
    return S.dc_block(_far(slap, low=260.0, high=2600.0, size=0.9, mix=0.4))


def hint_grove_whisper():
    """The Enchanted Forest: the grove, whispering, though there is no wind.

    Three breathed syllables ("haa", "huu", "hee") shaped by the vowel resonances, each its own
    breath with a silence after it - the rhythm of speech, which is what separates a whisper from
    the wind voice (one long sweep of the same kind of noise; analyse.py measured the first draft,
    a continuous breath over a rustle, at 0.044 from cast_wind). Under them the grove itself: a
    low hum on an open fifth, swelling and going, in the band the wind voice is highpassed out of.
    A few dry leaf-ticks round it. A short open room, because it is a wood."""
    haa = S.env_mul(_breath(0.30, [(760.0, 1.0), (1200.0, 0.6)], q=2.6, seed=501), S.adsr(0.30, a=0.07, d=0.05, s=0.8, r=0.12))
    huu = S.env_mul(_breath(0.26, [(380.0, 1.0), (900.0, 0.55)], q=2.8, seed=503), S.adsr(0.26, a=0.06, d=0.05, s=0.75, r=0.12))
    hee = S.env_mul(_breath(0.34, [(460.0, 0.8), (1900.0, 0.7)], q=2.8, seed=505), S.adsr(0.34, a=0.07, d=0.06, s=0.8, r=0.18))
    whisper = S.layer([(haa, 0.12, 1.0), (huu, 0.58, 0.8), (hee, 1.0, 0.9)])

    hdur = 1.9
    hn = S.n_samples(hdur)
    hum = S.mix(S.osc("sine", _hz(-14), hdur), S.gain(S.osc("sine", _hz(-7), hdur, phase=0.3), 0.7),
                S.gain(S.osc("tri", _hz(-2), hdur, phase=0.6), 0.12))
    hum = S.tremolo(S.lowpass(hum, 900.0), 3.1, 0.35)
    hum = S.env_mul(hum, S.ramp(hn // 3, 0.0, 1.0, 1.2) + S.decay(hn - hn // 3, 1.5))

    ticks = []
    for i, t in enumerate([0.05, 0.47, 0.9, 1.38, 1.52]):
        tn = S.n_samples(0.025)
        tk = S.env_mul(S.bandpass(S.osc("noise", 0, 0.025, seed=511 + i), 1300.0 + 150.0 * i, 3.0), S.decay(tn, 6.0))
        ticks.append((tk, t, 0.35))
    out = S.pad(S.layer([(hum, 0.0, 0.5), (whisper, 0.0, 1.0)] + ticks), 2.2)
    return S.dc_block(_far(out, low=150.0, high=3000.0, size=0.55, mix=0.3))


def hint_lantern_flare():
    """The Blighted Swamp: far off, one of his lanterns flares and gutters.

    A flame catching - a short band of noise opening upward - then a cold flame that BURNS: held
    level for a second, flickering at 13 Hz, then guttering out in uneven gaps. Over it the
    lantern's own light as a sound, a thin glassy tone on E5 with the poison voice's gurgle slowed
    to a slow wobble. Held, then guttered, rather than struck and decaying: the first draft was a
    whoosh and a thump and measured 0.062 from impact_darkness. Fog is a wide, damp room. Nothing
    below 120 Hz."""
    dur = 0.32
    n = S.n_samples(dur)
    catch = S.bandpass(S.osc("noise", 0, dur, seed=601), S.sweep(dur, 300.0, 1300.0, 0.6), 1.6)
    catch = S.env_mul(catch, S.ramp(S.n_samples(0.06), 0.0, 1.0) + S.decay(n - S.n_samples(0.06), 1.4))

    fdur = 1.5
    fn = S.n_samples(fdur)
    flame = S.bandpass(S.osc("noise", 0, fdur, seed=603), 520.0, 0.8)
    flame = S.tremolo(flame, 13.0, 0.5)
    hold = S.n_samples(0.95)
    gutter = S.gate([1.0] * (fn - hold), 7.0, duty=0.6, seed=605)
    flame = S.env_mul(flame, S.ramp(S.n_samples(0.1), 0.3, 1.0) + [1.0] * (hold - S.n_samples(0.1))
                      + [g * d for g, d in zip(gutter, S.decay(fn - hold, 1.2))])
    flame = S.lowpass(flame, 1000.0)

    tdur = 1.55
    tn = S.n_samples(tdur)
    tone = S.osc("sine", S.fm("sine", _hz(7), 3.2, 0.008, tdur, seed=607), tdur)
    glass = S.osc("sine", _hz(14) * 1.003, tdur, phase=0.2)
    tone = S.mix(tone, S.gain(glass, 0.35))
    tone = S.env_mul(tone, S.ramp(S.n_samples(0.15), 0.0, 1.0) + [1.0] * S.n_samples(0.8)
                     + S.decay(tn - S.n_samples(0.95), 1.6))
    out = S.pad(S.layer([
        (catch, 0.0, 0.8),
        (flame, 0.18, 0.55),
        (tone, 0.16, 0.5),
    ]), 2.3)
    return S.dc_block(_far(out, low=120.0, high=2600.0, size=0.75, mix=0.38, damp=0.6))


# The sung line of hint_ice_song: (onset s, length s, semitones from A4, vowel). StageHintCue puts a
# note on screen at each onset (IceSongNotes in scripts/StageHintCue.cs) - change one, change both.
_ICE_SONG = [
    (0.20, 0.34, -12, "oh"),    # A3
    (0.50, 0.31, -14, "ah"),    # G3
    (0.78, 0.31, -16, "eh"),    # F3
    (1.06, 0.31, -14, "oh"),    # G3
    (1.34, 0.36, -17, "ah"),    # E3
    (1.66, 0.78, -19, "oh"),    # D3, held
]

# A man's vowels: (F1, F2) and their gains. "eh" keeps its F2 low enough to survive the ice.
_SUNG_VOWELS = {
    "oh": [(450.0, 3.5, 1.0), (800.0, 5.0, 0.55), (2500.0, 8.0, 0.08)],
    "ah": [(700.0, 3.5, 1.0), (1150.0, 5.0, 0.6), (2600.0, 8.0, 0.08)],
    "eh": [(530.0, 3.5, 1.0), (1500.0, 5.0, 0.45), (2500.0, 8.0, 0.08)],
}


def hint_ice_song():
    """The Frozen Waste: the ice groans, and under it someone is singing.

    Reported from 0.10.0: the hint should be heard as SINGING. The first version was two notes on
    "oh" at 0.42 under a groan at full level, lowpassed at 1.1 kHz - it read as the ice making a
    noise. Now the song is the sound and the ice is its frame: a short groan and one crack to say
    where he is, then a man's voice singing a line - six notes falling through a D minor pentachord
    (A G F G E D), the shape of a runo line of the Kalevala, legato, each note gliding up or down
    from the last, the vowels changing under it (oh, ah, eh) the way sung words do, a singer's
    vibrato that blooms late on the held last note. Still muffled (lowpassed at 1.6 kHz, through
    the far room), because he is under the ice. The singer of the Kalevala; the caption does not
    name him, and neither does this. The notes are _ICE_SONG, which the screen follows."""
    gdur = 0.7
    gn = S.n_samples(gdur)
    rng = __import__("random").Random(701)
    walk, f = [], 118.0
    for _ in xrange(32):
        f = max(96.0, min(150.0, f + rng.uniform(-9.0, 9.0)))
        walk.append(f)
    buzz = S.osc("saw", S.resample(walk, gn), gdur)
    groan = S.mix(S.bandpass(buzz, S.sweep(gdur, 520.0, 320.0), 6.0),
                  S.gain(S.bandpass(buzz, S.sweep(gdur, 940.0, 720.0), 8.0), 0.5))
    groan = S.env_mul(groan, S.ramp(S.n_samples(0.2), 0.0, 1.0, 0.8) + S.decay(gn - S.n_samples(0.2), 1.6))
    crack = S.lowpass(S.bandpass(S.osc("noise", 0, 0.06, seed=703), 1700.0, 2.0), 2600.0)
    crack = S.env_mul(crack, S.decay(S.n_samples(0.06), 6.0))

    notes = []
    prev = _hz(_ICE_SONG[0][2] - 2)
    for i, (t0, dur, semis, vowel) in enumerate(_ICE_SONG):
        target = _hz(semis)
        n = S.n_samples(dur)
        glide = S.n_samples(0.07)
        f0 = S.ramp(glide, prev, target, 0.7) + [target] * (n - glide)
        held = i == len(_ICE_SONG) - 1
        # Vibrato blooms on the held note, as a singer's does; the moving notes are nearly straight.
        notes.append((_vowel(f0, dur, _SUNG_VOWELS[vowel], vibrato=0.016 if held else 0.006,
                             rate=5.2, attack=0.06, release=0.35 if held else 0.07, seed=11 + i), t0, 1.0))
        prev = target
    song = S.lowpass(S.layer(notes), 1600.0)

    out = S.pad(S.layer([
        (groan, 0.0, 0.55),
        (crack, 0.08, 0.45),
        (song, 0.0, 1.0),
    ]), 2.58)
    return S.dc_block(_far(out, low=105.0, high=2200.0, size=0.8, mix=0.4, damp=0.6))


def _camel_bell(freq, dur, seed):
    """One strike of a hollow tin bell and its wooden clapper. Lower, duller and shorter than
    every other bell in the set: those are struck glass and bronze, this is a pot on a rope."""
    ring = S.partials(freq, [1.0, 1.52, 2.17, 2.94, 3.61], dur,
                      amps=[1.0, 0.62, 0.42, 0.26, 0.16], decays=[4.2, 5.8, 7.5, 9.5, 12.0])
    kn = S.n_samples(0.035)
    knock = S.env_mul(S.bandpass(S.osc("noise", 0, 0.035, seed=seed), 900.0, 2.0), S.decay(kn, 5.0))
    clap = S.env_mul(S.osc("tri", 310.0, 0.05), S.decay(S.n_samples(0.05), 6.0))
    return S.layer([(ring, 0.0, 1.0), (knock, 0.0, 0.6), (clap, 0.0, 0.35)])


def hint_caravan_bell():
    """The Scorched Sands: a caravan bell, somewhere out on the sand.

    A bell swinging on a walking beast: five strikes at the uneven gaps a stride gives, each a
    shade off in pitch as it swings, all nearly as loud as the first - a walking rhythm, not one
    strike and its tail (as one strike it measured 0.084 from ui_pause, a falling triangle). Hollow tin with a wooden clapper,
    not bronze - chest_open, level_up and the light voice are all struck bells, and this must not
    sound like a reward. Open ground has little room: one echo off a dune and a short tail."""
    strikes = [(0.0, 1.0, 330.0), (0.36, 0.92, 334.0), (0.80, 0.86, 327.0), (1.13, 0.8, 332.0), (1.58, 0.66, 329.0)]
    items = [(_camel_bell(f, 0.9, 801 + i), t, g) for i, (t, g, f) in enumerate(strikes)]
    out = S.pad(S.layer(items), 2.5)
    out = S.delay(out, 0.27, feedback=0.3, mix=0.22, taps=3)
    return S.dc_block(_far(out, low=200.0, high=2400.0, size=0.4, mix=0.2, damp=0.45))


# The paper wind's gusts: StageHintCue.PaperGusts is the same list - change one, change both.
_PAPER_GUSTS = [(0.10, 1.0), (0.42, 0.72), (0.70, 0.9), (1.12, 0.78), (1.40, 0.62)]


def hint_paper_wind():
    """The Mystic Ruins: strips of written paper on the wind, burning at the edges as they fall.

    Abe no Seimei's wards, blown out of the seal he is held in. A wind that comes in GUSTS, and on each
    gust a strip of paper snapping in it - band noise in paper's register (1.4-2 kHz) chopped at 24-32 Hz,
    the flutter of a loose strip, which no other sound in the set has - over one low breath of moving
    air that swells and drops. Under the last three gusts the paper is burning: a few dry irregular
    crackles (the gate's seeded chop), very quiet. Not the grove's whisper (breathed syllables with
    silences, a hum under them) and not cast_wind (one long sweep): this is gusty and papery. Open
    ruins: a short bright room. Nothing below 180 Hz."""
    items = []
    for i, (t, g) in enumerate(_PAPER_GUSTS):
        dur = 0.30 + 0.04 * (i % 2)
        flut = S.bandpass(S.osc("noise", 0, dur, seed=901 + i), 1450.0 + 120.0 * i, 1.8)
        flut = S.tremolo(flut, 24.0 + 2.0 * i, 0.92, shape="square")
        flut = S.env_mul(flut, S.adsr(dur, a=0.03, d=0.06, s=0.45, r=0.08))
        items.append((flut, t, g))
        # The gust that carries it: a short swell of lower air, a beat ahead of the paper.
        gdur = 0.34
        gust = S.bandpass(S.osc("noise", 0, gdur, seed=921 + i), S.sweep(gdur, 380.0, 700.0, 0.8), 1.1)
        gust = S.env_mul(gust, S.adsr(gdur, a=0.09, d=0.08, s=0.45, r=0.1))
        items.append((gust, max(0.0, t - 0.06), 0.55 * g))
    for i, t in enumerate((0.78, 1.22, 1.5)):
        cdur = 0.22
        crackle = S.gate(S.highpass(S.osc("noise", 0, cdur, seed=951 + i), 1400.0), 70.0, duty=0.2, seed=961 + i)
        crackle = S.env_mul(crackle, S.decay(S.n_samples(cdur), 2.5))
        items.append((crackle, t, 0.42))
    # The wind through the broken courtyard: one thin whistle under it all, rising and falling with the
    # gusts - the ruins' own note, and what keeps this apart from every swoosh in the set.
    wdur = 1.8
    whistle = S.osc("sine", S.sweep(wdur, 640.0, 760.0, 0.7), wdur)
    whistle = S.tremolo(whistle, 3.2, 0.6)
    whistle = S.env_mul(whistle, S.adsr(wdur, a=0.35, d=0.2, s=0.7, r=0.5))
    items.append((whistle, 0.05, 0.22))
    out = S.pad(S.layer(items), 2.2)
    return S.dc_block(_far(S.highpass(out, 180.0), low=180.0, high=2200.0, size=0.45, mix=0.25, damp=0.5))


# The tired hammer's strikes, each (onset, gain): StageHintCue.AnvilStrikes is the same list of onsets.
_ANVIL_STRIKES = [(0.08, 1.0), (0.64, 0.86), (1.24, 0.8), (1.90, 0.64)]


def _anvil_strike(seed):
    """One blow on an anvil: the steel ringing at a free bar's inharmonic modes (1, 2.76, 5.40), bright and
    quick to die, over the dull thud of the hammer landing - and the little rebound tap a smith lets the
    hammer make a beat after, which nothing else in the set has."""
    dur = 0.6
    ring = S.partials(1240.0, [1.0, 2.76, 5.40, 8.93], dur,
                      amps=[1.0, 0.5, 0.26, 0.1], decays=[7.0, 9.0, 12.0, 16.0])
    tn = S.n_samples(0.06)
    thud = S.env_mul(S.bandpass(S.osc("noise", 0, 0.06, seed=seed), 260.0, 1.4), S.decay(tn, 4.0))
    body = S.env_mul(S.osc("sine", S.sweep(0.08, 180.0, 120.0, 0.6), 0.08), S.decay(S.n_samples(0.08), 3.0))
    tap = S.partials(1240.0, [1.0, 2.76], 0.25, amps=[1.0, 0.3], decays=[14.0, 18.0])
    return S.layer([(ring, 0.0, 0.55), (thud, 0.0, 0.8), (body, 0.0, 0.6), (tap, 0.11, 0.16)])


def hint_anvil():
    """The Emberdeep: a hammer on an anvil, somewhere below.

    Wayland the Smith, chained to his own anvil, working because he is not allowed to stop. Four blows,
    each a ring of struck steel over the thud of the hammer, each with the small rebound tap a smith lets
    the hammer make - and TIRED: the gaps between them lengthen (0.56, 0.60, 0.66 s) and each lands softer
    than the last. The ring sits high (1.2 kHz and its bar modes), well clear of the caravan's tin bell
    (330 Hz) and of chest_open's glassy bells, and it dies fast; what makes it an anvil is the thud under
    the ring and the tap after it. Far below and in a big stone room: the top cut at 3 kHz, a long dark
    tail. Nothing below 100 Hz."""
    items = [(_anvil_strike(1101 + i), t, g) for i, (t, g) in enumerate(_ANVIL_STRIKES)]
    out = S.pad(S.layer(items), 2.3)
    out = S.highpass(out, 100.0)
    return S.dc_block(_far(out, low=100.0, high=3000.0, size=0.8, mix=0.32, damp=0.55))


# The Sunken Cave's nine mouths (.ai/chapter-wizards.md 4.3). The Cumaean Sibyl's voice comes out of every
# mouth in the rock, each a beat later than the last; the first is hers, the rest the cave giving it back.
# The beat is NineMouthsEvent.VoiceBeat and StageHintCue.SibylVoices - change one, change all three.
_SIBYL_BEAT = 0.24
_SIBYL_VOICES = [(0.0, 1.0), (0.24, 0.74), (0.48, 0.6), (0.72, 0.5), (0.96, 0.42), (1.20, 0.35), (1.44, 0.3)]


def _sibyl_phrase(seed=0):
    """Her line, with no words: an old woman's voice intoning three syllables - a vowel rising from F#4 to A4,
    a step down to G4, and a long E4 that trembles, the way a line is spoken when it is a prophecy and not
    a sentence. Old: a slow wide vibrato and a husk of breath through the same resonances under every
    syllable (long life without youth). Lower and slower than the cell's cry, which is called, not
    intoned."""
    ah = [(780.0, 4.0, 1.0), (1220.0, 6.0, 0.55), (2600.0, 8.0, 0.14)]
    oh = [(520.0, 4.0, 1.0), (900.0, 6.0, 0.5), (2500.0, 8.0, 0.1)]
    eh = [(S.sweep(0.52, 620.0, 470.0), 4.0, 1.0), (S.sweep(0.52, 1900.0, 1100.0), 6.0, 0.45), (2600.0, 8.0, 0.1)]
    one = _vowel(S.sweep(0.30, 370.0, 440.0, 0.8), 0.30, ah, vibrato=0.02, rate=4.4, attack=0.05, release=0.05, seed=31 + seed)
    two = _vowel(S.sweep(0.18, 415.0, 392.0), 0.18, oh, vibrato=0.02, rate=4.4, attack=0.03, release=0.05, seed=33 + seed)
    three = _vowel(S.sweep(0.52, 340.0, 326.0), 0.52, eh, vibrato=0.034, rate=5.2, attack=0.04, release=0.22, seed=35 + seed)
    voiced = S.layer([(one, 0.0, 1.0), (two, 0.31, 0.85), (three, 0.50, 0.9)])
    husk = S.layer([
        (S.env_mul(_breath(0.30, [(780.0, 1.0), (1220.0, 0.5)], q=3.0, seed=41 + seed), S.adsr(0.30, a=0.05, d=0.05, s=0.8, r=0.06)), 0.0, 0.22),
        (S.env_mul(_breath(0.18, [(520.0, 1.0), (900.0, 0.5)], q=3.0, seed=43 + seed), S.adsr(0.18, a=0.03, d=0.05, s=0.8, r=0.06)), 0.31, 0.2),
        (S.env_mul(_breath(0.52, [(560.0, 1.0), (1500.0, 0.45)], q=3.0, seed=45 + seed), S.adsr(0.52, a=0.04, d=0.05, s=0.8, r=0.22)), 0.50, 0.24),
    ])
    return S.pad(S.layer([(voiced, 0.0, 1.0), (husk, 0.0, 1.0)]), 1.05)


def sibyl_voice():
    """The Sunken Cave, at the nine mouths: her own voice, out of the one mouth that is hers - played at that
    mouth, first, on every call (NineMouthsEvent). Near and nearly dry: the throat of a cave mouth, a short
    stone room, nothing above 3.4 kHz. It is the clearest thing in the call, so the first voice is also the
    one that sounds like a person; the eight after it are the cave (sibyl_echo)."""
    out = S.pad(_sibyl_phrase(0), 1.25)
    out = S.reverb(S.lowpass(S.highpass(out, 170.0), 3400.0), size=0.35, damp=0.55, mix=0.16)
    return S.dc_block(out)


def sibyl_echo():
    """The cave giving her voice back, out of a mouth that is not hers - played at each of the other eight, a
    beat apart (NineMouthsEvent). The same line from deeper in the rock: its top gone (lowpassed at 1.3
    kHz), hollowed by a short pipe resonance (a 7 ms comb - a voice down a narrow passage), its start
    smeared, and a long dark tail. So the call is one person and eight echoes of her, and the person is
    first: the order and the timbre agree."""
    phrase = _sibyl_phrase(1)
    pipe = S.delay(phrase, 0.007, feedback=0.55, mix=0.5, taps=6)
    smear = S.env_mul(S.pad(pipe, 1.6), S.ramp(S.n_samples(0.09), 0.0, 1.0) + [1.0] * (S.n_samples(1.6) - S.n_samples(0.09)))
    out = S.reverb(S.lowpass(S.highpass(smear, 240.0), 1300.0), size=0.85, damp=0.7, mix=0.45)
    return S.dc_block(out)


def hint_sibyl_voice():
    """The Sunken Cave: a woman's voice from far down, in more places than one.

    The call at the nine mouths as it reaches the player across the map: her line once, then the cave
    giving it back six times, each a beat (_SIBYL_BEAT) after the last and darker and quieter than the one
    before - many voices a beat apart, the first clearest. Every onset is _SIBYL_VOICES, which
    StageHintCue.SibylVoices puts a written leaf on screen for. Then the far room every hint has. Not the
    cell's cry (one voice, called, rising into a question): this is intoned, falling, and multiplied."""
    items = []
    for i, (t, g) in enumerate(_SIBYL_VOICES):
        v = _sibyl_phrase(i)
        if i > 0:
            v = S.lowpass(S.delay(v, 0.007, feedback=0.4, mix=0.35, taps=4), 2200.0 - 160.0 * i)
        items.append((v, t, g))
    out = S.pad(S.layer(items), 2.55)
    return S.dc_block(_far(out, low=190.0, high=2600.0, size=0.9, mix=0.42, damp=0.6))


# The Enchanted Forest's walking hut (.ai/chapter-wizards.md 4.1). Two heavy falls of a great foot a long stride
# apart, then the timber of the hut creaking as its weight settles: (onset, gain). StageHintCue.HutFalls is the
# same list of onsets and the creak's start - change one, change both.
_HUT_FALLS = [(0.10, 1.0), (0.74, 0.9)]
_HUT_CREAK = (1.02, 0.62)


def _hut_fall(seed):
    """One great foot coming down on the moss: the weight landing (a low thump whose pitch drops as the ground
    takes it), the ground giving under it (a short dark crunch of earth and old leaves), and the hut above it
    jarring (a dull knock of wood a beat after). The crunch is what carries on a phone speaker, which has no
    thump at all; the knock is what says the thing walking is built."""
    dur = 0.6
    n = S.n_samples(dur)
    body = S.env_mul(S.osc("sine", S.sweep(dur, 96.0, 54.0, 0.5), dur), S.decay(n, 5.5))
    over = S.env_mul(S.osc("sine", S.sweep(0.2, 190.0, 120.0, 0.6), 0.2), S.decay(S.n_samples(0.2), 4.0))
    cn = S.n_samples(0.24)
    crunch = S.lowpass(S.bandpass(S.osc("noise", 0, 0.24, seed=seed), 560.0, 0.8), 1900.0, 0.707)
    crunch = S.env_mul(crunch, S.decay(cn, 5.0))
    knock = S.partials(230.0, [1.0, 2.31, 3.86], 0.2, amps=[1.0, 0.42, 0.16], decays=[16.0, 22.0, 30.0])
    return S.layer([(body, 0.0, 1.0), (over, 0.0, 0.5), (crunch, 0.004, 0.5), (knock, 0.05, 0.32)])


def _creak(dur, seed):
    """Timber under strain: stick-slip - a train of tiny clicks whose rate climbs and falls back as the joint
    gives - rung through a log wall's two resonances, the lower one sliding as the strain moves along it."""
    n = S.n_samples(dur)
    clicks = [0.0] * n
    t = 0.0
    k = 0
    while t < dur:
        i = int(t * S.SR)
        if i < n:
            clicks[i] = 1.0 if k % 2 == 0 else -0.8
        rate = 38.0 + 52.0 * math.sin(math.pi * min(1.0, t / dur)) + 6.0 * math.sin(t * 23.0 + seed)
        t += 1.0 / rate
        k += 1
    low = S.bandpass(clicks, 360.0, 7.0)
    high = S.bandpass(clicks, 930.0, 9.0)
    wood = S.mix(S.gain(low, 1.0), S.gain(high, 0.45))
    return S.env_mul(wood, S.adsr(dur, a=0.08, d=0.1, s=0.8, r=0.22))


def hint_hut_step():
    """The Enchanted Forest: something heavy walking the deep wood, on two legs.

    Baba Yaga's hut, walking its circle. A slow two-beat tread - two falls of a great foot 0.64 s apart, the
    second a little softer - and then the hut's timbers creaking as its weight settles onto the leg it stands on.
    Not a boss stomp (those are near, sharp and hit the sub): this is far off, through trees, the top gone at 2.2
    kHz and the low end held above 50 Hz, with a forest's short, damp room rather than a cave's. The creak sits in
    wood's own band (360 Hz and 930 Hz), clear of the grove's whispering leaves, which are all air."""
    items = [(_hut_fall(2201 + i), t, g) for i, (t, g) in enumerate(_HUT_FALLS)]
    items.append((_creak(0.62, 7), _HUT_CREAK[0], _HUT_CREAK[1]))
    out = S.pad(S.layer(items), 2.3)
    out = S.highpass(out, 50.0)
    return S.dc_block(_far(out, low=50.0, high=2200.0, size=0.55, mix=0.3, damp=0.65))


# The Last Bridge (.ai/chapter-wizards.md 4.9): Morgan le Fay's barge, moored under the last arch, its hull
# knocking on the pier as the swell lifts it and lets it fall. Each (onset s, gain): StageHintCue.HullKnocks
# is the same list of onsets - change one, change both.
_HULL_KNOCKS = [(0.06, 1.0), (0.98, 0.8), (1.62, 0.62)]


def _hull_knock(seed):
    """One knock of a wooden hull on stone: the planks' dull tok, the hollow of the boat ringing under it (a
    low box mode falling a little as it rings, and the next plate mode up), and the hull touching the pier
    again 90 ms later, softer, as it settles - the double contact of a boat, low and hollow, where the anvil's
    rebound is a small tap of steel."""
    dur = 0.42
    n = S.n_samples(dur)
    boom = S.env_mul(S.osc("sine", S.sweep(dur, 150.0, 128.0, 0.5), dur), S.expdecay(n, 9.0))
    pd = 0.2
    # The next mode up sits at 1.75x the box, not 2.3x: at 352 Hz it filled the bands impact_water's
    # droplet lives in and measured 0.142 from it; at 262 Hz the nearest sound in the set is 0.22 away.
    plate = S.env_mul(S.osc("sine", S.sweep(pd, 262.0, 252.0), pd), S.expdecay(S.n_samples(pd), 12.0))
    td = 0.05
    tok = S.env_mul(S.bandpass(S.osc("noise", 0, td, seed=seed), 700.0, 3.0), S.expdecay(S.n_samples(td), 8.0))
    contact = S.layer([(boom, 0.0, 1.0), (plate, 0.0, 0.5), (tok, 0.0, 0.85)])
    # The hollow inside the hull: a short comb, the box's own flutter.
    contact = S.delay(contact, 0.0045, feedback=0.5, mix=0.35, taps=5)
    return S.layer([(contact, 0.0, 1.0), (contact, 0.09, 0.42)])


def _water_slap(seed, dur=0.2):
    """The swell slapping the pier between knocks: a short wet band of noise falling as it breaks. Noise only -
    a pitched droplet is the water element's cast, and this must not sound like a spell."""
    n = S.n_samples(dur)
    slap = S.bandpass(S.osc("noise", 0, dur, seed=seed), S.sweep(dur, 760.0, 260.0, 0.7), 1.4)
    return S.lowpass(S.env_mul(slap, S.ad(dur, 0.012, 2.5)), 1400.0)


def hint_hull_knock():
    """The Last Bridge: a boat knocking against the pier, far below, where no boat could be.

    Morgan le Fay's barge, moored under the bridge's last arch over a dark sea. Three knocks of a hollow
    wooden hull on stone, at the uneven gaps the swell gives (0.92, 0.64 s), each with the softer second touch
    of the hull settling - a low box resonance at 150 Hz under a dull wooden tok at 700 Hz, short and boomy,
    nothing that rings. Not the anvil (struck steel at 1.2 kHz, a hammer's rebound) and not stone on stone.
    Between the knocks the water slaps the pier, and as the swell lifts the boat for the second knock the
    mooring rope creaks once (a stick-slip buzz through two narrow wooden resonances). Far below and out in
    the open over the sea: one echo off the arch and a short open room, not a cave's long tail; the top cut at
    2.2 kHz. Nothing below 95 Hz. The knocks are _HULL_KNOCKS, which the screen follows."""
    items = [(_hull_knock(1201 + i), t, g) for i, (t, g) in enumerate(_HULL_KNOCKS)]
    for i, (t, g) in enumerate([(0.40, 0.42), (0.62, 0.3), (1.30, 0.36), (1.92, 0.3)]):
        items.append((_water_slap(1221 + i), t, g))
    cd = 0.38
    rope = S.osc("saw", S.sweep(cd, 42.0, 66.0, 0.8), cd)
    rope = S.mix(S.bandpass(rope, 520.0, 5.0), S.gain(S.bandpass(rope, 1180.0, 6.0), 0.6))
    rope = S.lowpass(S.env_mul(rope, S.adsr(cd, a=0.12, d=0.06, s=0.7, r=0.12)), 1600.0)
    items.append((rope, 0.54, 0.22))
    out = S.pad(S.layer(items), 2.4)
    out = S.highpass(out, 95.0)
    out = S.delay(out, 0.13, feedback=0.25, mix=0.2, taps=2)
    return S.dc_block(_far(out, low=95.0, high=2200.0, size=0.6, mix=0.24, damp=0.55))


# The Blighted Swamp's gibbet cage (.ai/chapter-wizards.md 4.4). Two taps on a glass flask, a man coughing twice, and a
# last faint tap: (onset, gain). StageHintCue.FlaskBeats is the same five onsets in order (taps and coughs merged) -
# change one, change both.
_FLASK_TAPS = [(0.10, 0.62), (0.34, 0.5), (1.66, 0.34)]
_FLASK_COUGHS = [(0.86, 1.0), (1.10, 0.72)]


def _flask_tap(seed):
    """A fingernail on a round-bellied flask with a dose in it: the click of the nail (a few ms of bright noise), and
    the glass answering - a stiff shell's inharmonic modes, a ninth apart from any bell in the set (the light element's
    G5, level_up, chest_open), damped by the liquid inside so it rings a third of a second and not three."""
    dur = 0.42
    ring = S.partials(1318.0, [1.0, 2.41, 4.07, 5.93], dur, amps=[1.0, 0.46, 0.22, 0.1], decays=[12.0, 17.0, 23.0, 31.0])
    kn = S.n_samples(0.012)
    click = S.env_mul(S.highpass(S.osc("noise", 0, 0.012, seed=seed), 2600.0), S.decay(kn, 6.0))
    # The liquid's slosh under it: a low wobble of the same shell, a breath after.
    slosh = S.env_mul(S.osc("sine", S.sweep(0.16, 540.0, 470.0), 0.16), S.decay(S.n_samples(0.16), 5.0))
    return S.layer([(ring, 0.0, 0.8), (click, 0.0, 0.5), (slosh, 0.02, 0.18)])


def _cough(seed, f0=118.0):
    """A man's cough, wet and tired: the burst of the glottis opening (noise through the "kh" of the throat and the
    "ah" of an open mouth), a short voiced bark under it in a man's register, and the breath after it."""
    dur = 0.24
    n = S.n_samples(dur)
    burst = S.osc("noise", 0, dur, seed=seed)
    kh = S.mix(S.gain(S.bandpass(burst, 1250.0, 1.6), 1.0), S.gain(S.bandpass(burst, 560.0, 2.2), 0.8),
               S.gain(S.bandpass(burst, 2600.0, 3.0), 0.3))
    kh = S.env_mul(kh, S.ad(dur, attack=0.004, curve=4.0))
    bark = _vowel(S.sweep(0.12, f0 * 1.15, f0 * 0.9), 0.12, [(640.0, 3.0, 1.0), (1150.0, 4.0, 0.6), (2400.0, 6.0, 0.2)],
                  vibrato=0.03, rate=17.0, attack=0.006, release=0.05, seed=seed)
    breath = _breath(0.22, [(900.0, 0.5), (1700.0, 0.3)], q=1.0, seed=seed + 1)
    breath = S.env_mul(breath, S.adsr(0.22, a=0.03, d=0.06, s=0.5, r=0.12))
    return S.layer([(kh, 0.0, 1.0), (bark, 0.008, 0.85), (breath, 0.13, 0.32)])


def hint_flask_chime():
    """The Blighted Swamp: glass ringing across the water, and a man coughing, far out on the bog.

    Paracelsus in the gibbet's cage over the rot, tapping his flask to hear how much is left in it - two taps, a
    breath - then coughing, twice, the second weaker; and one more tap, faint. Glass and a voice: nothing else in the
    set is both. Over water, so the room is wide and soft and the top goes at 3.2 kHz (the taps are glass and want a
    little more air than the hut's timbers had)."""
    items = [(_flask_tap(3301 + i), t, g) for i, (t, g) in enumerate(_FLASK_TAPS)]
    items += [(_cough(3401 + i, 118.0 - 6.0 * i), t, g) for i, (t, g) in enumerate(_FLASK_COUGHS)]
    out = S.pad(S.layer(items), 2.4)
    return S.dc_block(_far(out, low=140.0, high=3200.0, size=0.8, mix=0.36, damp=0.55))


# The Court's seer (.ai/chapter-wizards.md 4.10): Nostradamus, reading aloud in one of the rooms of the house - four
# lines of verse, one quatrain, read as two couplets with a breath between them. The onset of each line, in seconds:
# StageHintCue.QuatrainLines is the same list (the drifting marks come in on each line) - change one, change both.
_QUATRAIN_LINES = [0.06, 0.78, 1.74, 2.46]
# Each line a little softer than the one before: a reader's voice settling as he reads on, not a call.
_QUATRAIN_GAINS = [1.0, 0.92, 0.88, 0.8]

# A man's vowels, (F1, F2, F3) - Peterson and Barney's, the third pulled in a little so nothing reaches for 3 kHz.
_SPOKEN_VOWELS = {
    "ah": (730.0, 1090.0, 2440.0),
    "aw": (570.0, 840.0, 2410.0),
    "uh": (520.0, 1190.0, 2390.0),
    "eh": (530.0, 1840.0, 2480.0),
    "ih": (390.0, 1990.0, 2550.0),
    "ee": (300.0, 2200.0, 2750.0),
    "oo": (320.0, 870.0, 2240.0),
    "er": (490.0, 1350.0, 1690.0),
}

# The verse, line by line: each syllable is (consonant before it, vowel it starts on, vowel it glides to, seconds,
# pitch it starts on, pitch it ends on, gain). Read, not sung: the pitch moves inside every syllable as speech does,
# each line falls from its stress to its end (declination), the first and third lift a little at the line's end -
# the voice going on - and the fourth starts lower, falls furthest and holds its last syllable, because it is the
# end of the verse. Weak, strong, weak, strong: a short line of verse at a reader's pace, not a sentence.
_QUATRAIN_VERSE = [
    [("", "aw", "oo", 0.09, 128.0, 132.0, 0.8), ("w", "eh", "eh", 0.13, 141.0, 134.0, 1.0),
     ("s", "uh", "uh", 0.07, 128.0, 125.0, 0.65), ("l", "ah", "ee", 0.15, 134.0, 138.0, 0.9)],
    [("th", "uh", "uh", 0.07, 127.0, 124.0, 0.6), ("w", "aw", "aw", 0.13, 139.0, 131.0, 1.0),
     ("m", "ih", "ih", 0.07, 128.0, 125.0, 0.7), ("s", "eh", "eh", 0.15, 125.0, 116.0, 0.85)],
    [("w", "eh", "eh", 0.07, 127.0, 126.0, 0.65), ("s", "uh", "er", 0.13, 140.0, 133.0, 1.0),
     ("l", "ah", "ah", 0.07, 128.0, 125.0, 0.65), ("d", "aw", "oo", 0.15, 129.0, 135.0, 0.85)],
    [("h", "aw", "oo", 0.08, 122.0, 119.0, 0.65), ("g", "oo", "oo", 0.13, 128.0, 120.0, 0.95),
     ("z", "ah", "ah", 0.07, 117.0, 112.0, 0.65), ("d", "ah", "er", 0.28, 110.0, 96.0, 0.85)],
]

# What comes before a vowel, as a short sound and the gap it leaves: a breath, a hiss, a buzz or a closed lip.
_CONSONANTS = {
    "": None,
    "h": ("breath", 0.04),
    "th": ("hiss", 0.045, 1700.0, 0.08),
    "s": ("hiss", 0.055, 2600.0, 0.12),
    "z": ("hiss", 0.04, 2400.0, 0.07),
    "t": ("stop", 0.03, 1900.0),
    "d": ("stop", 0.025, 1500.0),
    "g": ("stop", 0.03, 1200.0),
    "w": ("glide", 0.04, "oo"),
    "l": ("glide", 0.035, "er"),
    "m": ("hum", 0.045),
}


def _spoken_syllable(spec, seed):
    """One syllable of the reading: its consonant, then a voiced vowel that glides from one vowel's resonances to
    the next, with an old man's husk of breath under it. Returns the buffer and the seconds of consonant ahead of the
    vowel (the syllable's onset is its consonant, so a line begins where its first sound does)."""
    cons, v0, v1, dur, p0, p1, g = spec
    a, b = _SPOKEN_VOWELS[v0], _SPOKEN_VOWELS[v1]
    # The second and third resonances kept up, as a near speaking voice keeps them: the ice song, sung through the
    # ice, has its top taken off, and a voice in the same room does not.
    formants = [(S.sweep(dur, a[0], b[0]), 4.0, 1.0), (S.sweep(dur, a[1], b[1]), 6.0, 0.8),
                (S.sweep(dur, a[2], b[2]), 9.0, 0.32)]
    # Old: a fast small tremor in the pitch (6.4 Hz) rather than a singer's slow vibrato, and breath through the
    # same resonances under the voice.
    vowel = _vowel(S.sweep(dur, p0, p1), dur, formants, vibrato=0.011, rate=6.4, attack=0.018, release=0.04, seed=seed)
    husk = S.env_mul(_breath(dur, [(a[0], 0.8), (a[1], 0.4)], q=3.0, seed=seed + 50), S.adsr(dur, a=0.02, d=0.03, s=0.8, r=0.04))
    voiced = S.layer([(vowel, 0.0, 1.0), (husk, 0.0, 0.16)])
    kind = _CONSONANTS.get(cons)
    if kind is None:
        return S.gain(voiced, g), 0.0
    if kind[0] == "breath":
        lead = kind[1]
        c = S.env_mul(_breath(lead, [(a[0], 0.7), (a[1], 0.5)], q=2.0, seed=seed + 7), S.adsr(lead, a=0.01, d=0.01, s=0.8, r=0.01))
        return S.gain(S.layer([(c, 0.0, 0.35), (voiced, lead, 1.0)]), g), lead
    if kind[0] == "hiss":
        lead, centre, level = kind[1], kind[2], kind[3]
        noise = S.bandpass(S.osc("noise", 0, lead, seed=seed + 9), centre, 2.0)
        c = S.env_mul(noise, S.adsr(lead, a=0.012, d=0.01, s=0.8, r=0.015))
        return S.gain(S.layer([(c, 0.0, level * 4.0), (voiced, lead * 0.8, 1.0)]), g), lead * 0.8
    if kind[0] == "stop":
        lead, centre = kind[1], kind[2]
        burst = S.env_mul(S.bandpass(S.osc("noise", 0, 0.012, seed=seed + 11), centre, 2.5), S.decay(S.n_samples(0.012), 3.0))
        return S.gain(S.layer([(burst, lead - 0.012, 0.45), (voiced, lead, 1.0)]), g), lead
    if kind[0] == "glide":
        lead, from_vowel = kind[1], kind[2]
        f = _SPOKEN_VOWELS[from_vowel]
        glide = _vowel(S.sweep(lead, p0 * 0.97, p0), lead,
                       [(S.sweep(lead, f[0], a[0]), 4.0, 0.8), (S.sweep(lead, f[1], a[1]), 6.0, 0.35)],
                       vibrato=0.008, rate=6.4, attack=0.012, release=0.005, seed=seed + 13)
        return S.gain(S.layer([(glide, 0.0, 0.55), (voiced, lead, 1.0)]), g), lead
    # A hum: the lips closed on the voice, only its lowest resonance getting out.
    lead = kind[1]
    hum = _vowel(S.sweep(lead, p0, p0), lead, [(260.0, 4.0, 1.0), (1000.0, 8.0, 0.08)],
                 vibrato=0.008, rate=6.4, attack=0.012, release=0.005, seed=seed + 15)
    return S.gain(S.layer([(hum, 0.0, 0.5), (voiced, lead, 1.0)]), g), lead


def _quatrain_line(syllables, seed):
    """A line of the verse: its syllables one after another, each joined to the next with a breath of a gap, the
    first starting at the line's onset."""
    items = []
    t = 0.0
    for k, spec in enumerate(syllables):
        buf, _lead = _spoken_syllable(spec, seed + 17 * k)
        items.append((buf, t, 1.0))
        t += len(buf) / S.SR + 0.012
    return S.layer(items)


def hint_quatrain():
    """The Court: an old man's voice, reading aloud in one of the rooms of the house.

    Nostradamus at his verses (.ai/chapter-wizards.md 4.10). Four lines of a quatrain read in a measured voice: each
    line four syllables, weak and strong, the pitch moving inside every syllable as speech does and falling through
    the line, the first and third lifting at their ends where the verse goes on, the fourth lower, falling furthest,
    its last syllable held. A man's register, low (96-140 Hz) and dry, with an old voice's quick tremor and a husk of
    breath; consonants as breaths, hisses, stops and glides, so it is read and not sung. Near: a person in a room,
    not a voice from the rock - a stone room's air at a fifth of the level and short, top cut at 3.2 kHz. Not the
    Sibyl (a woman, intoned, multiplied by the cave), not the cell's cry (called, rising into a question), not the
    ice song (sung, legato, muffled under ice). It plays as the chapter hint and, in the seek, every few seconds
    from the room his voice is in, so it is level and soft-edged and pleasant on repeat. The line onsets are
    _QUATRAIN_LINES, which StageHintCue puts its marks up on."""
    items = []
    for i, onset in enumerate(_QUATRAIN_LINES):
        items.append((_quatrain_line(_QUATRAIN_VERSE[i], 1301 + 101 * i), onset, _QUATRAIN_GAINS[i]))
    out = S.pad(S.layer(items), 3.3)
    out = S.lowpass(S.highpass(out, 85.0), 3200.0)
    return S.dc_block(S.reverb(out, size=0.5, damp=0.6, mix=0.12))


# The Scorched Sands' dry tomb (.ai/chapter-wizards.md 4.6). Three slow knocks of stone on stone from under the sand,
# each later and softer than the last: (onset, gain). StageHintCue.TombKnocks is the same list of onsets - change one,
# change both.
_TOMB_KNOCKS = [(0.12, 1.0), (0.98, 0.82), (1.96, 0.66)]


def _stone_knock(seed):
    """One knock of stone on stone, heard through rock: a dull, dense tok - a block's inharmonic modes low down (a
    stone bar's, 1 : 2.4 : 4.1, under 1 kHz, gone in a tenth of a second, which is what makes it stone and not wood or
    iron) - over the weight of it (a short low thump), and the grit of the two faces grinding as it lands (a rasp of
    band noise at 700 Hz, chopped). No ring: the anvil rings; this is dead."""
    tok = S.partials(212.0, [1.0, 2.42, 4.07], 0.16, amps=[1.0, 0.5, 0.22], decays=[22.0, 30.0, 40.0])
    dur = 0.3
    thump = S.env_mul(S.osc("sine", S.sweep(dur, 92.0, 62.0, 0.6), dur), S.decay(S.n_samples(dur), 6.0))
    gn = S.n_samples(0.14)
    grit = S.bandpass(S.osc("noise", 0, 0.14, seed=seed), 700.0, 1.6)
    grit = S.env_mul(S.gate(grit, 46.0, duty=0.55, seed=seed + 1), S.decay(gn, 4.0))
    return S.layer([(thump, 0.0, 0.9), (tok, 0.0, 0.75), (grit, 0.01, 0.4)])


def _sand_hiss(dur, seed):
    """Sand running, the whole length of the sound: band noise high in the mids, lowpassed so it is a hiss and not
    steam, low and steady - and swelling a little after each knock, as sand the knock shook loose runs down."""
    n = S.n_samples(dur)
    hiss = S.lowpass(S.bandpass(S.osc("noise", 0, dur, seed=seed), 1400.0, 0.8), 2400.0, 0.707)
    env = [0.16] * n
    for (t, g) in _TOMB_KNOCKS:
        i0 = int((t + 0.05) * S.SR)
        for i in range(i0, min(n, i0 + int(0.9 * S.SR))):
            u = (i - i0) / float(S.SR)
            swell = g * 0.42 * min(1.0, u / 0.12) * math.exp(-u / 0.32)
            env[i] += swell
    fade = S.adsr(dur, a=0.15, d=0.05, s=1.0, r=0.35)
    return S.env_mul(S.env_mul(hiss, env), fade)


def hint_tomb_knock():
    """The Scorched Sands: far off in the dunes, a slow knocking, stone on stone.

    Imhotep, sealed in a dry tomb, knocking on the slab from inside. Three knocks, the gaps lengthening (0.86, 0.98 s)
    and each softer - "slower now; it has not stopped" - under a steady hiss of sand that swells after each knock, as if
    it shook some loose. Not the anvil (a tired hammer, but RINGING steel at 1.2 kHz and up); not the hut's tread (a low
    thump in a damp forest): this is dead stone, short and dry, and the room round it is the open desert - a short,
    bright-free reflection rather than a cave's tail. Top cut at 2.6 kHz, nothing under 60 Hz."""
    dur = 2.7
    items = [(_stone_knock(3301 + i), t, g) for i, (t, g) in enumerate(_TOMB_KNOCKS)]
    items.append((_sand_hiss(dur, 3311), 0.0, 0.3))
    out = S.pad(S.layer(items), dur)
    out = S.highpass(out, 60.0)
    return S.dc_block(_far(out, low=60.0, high=2600.0, size=0.4, mix=0.22, damp=0.7))


# The Fallow Fields' granary (.ai/chapter-wizards.md 4.2). Medea's voice through the reeve's plank walls: "you there",
# a breath, then the longer line falling - (onset, length, f0 from, f0 to, vowel). StageHintCue.GranaryVoices is the
# same list of onsets, a wisp of her smoke on each - change one, change both.
_GRANARY_SYLLABLES = [
    (0.00, 0.18, 233.0, 222.0, "oo"),
    (0.22, 0.38, 247.0, 208.0, "eh"),
    (0.72, 0.14, 233.0, 228.0, "ah"),
    (0.88, 0.14, 222.0, 218.0, "eh"),
    (1.04, 0.16, 210.0, 204.0, "ah"),
    (1.22, 0.46, 220.0, 185.0, "oh"),
]
_GRANARY_VOWELS = {
    "oo": [(330.0, 4.0, 1.0), (820.0, 6.0, 0.45), (2400.0, 8.0, 0.08)],
    "eh": [(560.0, 4.0, 1.0), (1700.0, 6.0, 0.5), (2500.0, 8.0, 0.12)],
    "ah": [(720.0, 4.0, 1.0), (1180.0, 6.0, 0.55), (2500.0, 8.0, 0.1)],
    "oh": [(480.0, 4.0, 1.0), (880.0, 6.0, 0.5), (2450.0, 8.0, 0.08)],
}


def hint_granary_voice():
    """The Fallow Fields: a woman's voice from the granary - "You there. The granary, past the furrows."

    Medea, held in the Tithe-Reeve's granary. She speaks, as Tituba does in her cell, but she does not plead: the
    cell's cry is called high and rises into a question; this is said low and level, an alto (B-flat 3 down to F-sharp
    3), every phrase FALLING, with almost no vibrato - an order, not a plea. Two words, a breath, then the longer line
    trailing down. Heard through the reeve's plank walls: the top gone at 1.7 kHz, a hollow box resonance round 450 Hz
    (a voice inside a timber room, not down a stone corridor), and a short dry room after it. Under the last phrase, a
    few soft crackles of herbs smouldering low - her smoke, which is the cue. Onsets: _GRANARY_SYLLABLES."""
    items = []
    for i, (t, dur, f0, f1, vowel) in enumerate(_GRANARY_SYLLABLES):
        v = _vowel(S.sweep(dur, f0, f1, 0.8), dur, _GRANARY_VOWELS[vowel], vibrato=0.006, rate=4.8,
                   attack=0.03, release=0.07 if i < len(_GRANARY_SYLLABLES) - 1 else 0.2, seed=61 + i)
        items.append((v, t, 1.0 if i != 1 else 0.95))
    voiced = S.pad(S.layer(items), 2.2)
    # A breath of the same resonances under the last phrase, as a low voice trails off.
    tail = S.env_mul(_breath(0.5, [(480.0, 1.0), (880.0, 0.4)], q=3.0, seed=67), S.adsr(0.5, a=0.1, d=0.05, s=0.6, r=0.3))
    crackles = []
    for k, t in enumerate((0.34, 0.61, 0.97, 1.31, 1.52, 1.78)):
        n = S.n_samples(0.03)
        burst = S.env_mul(S.bandpass(S.osc("noise", 0, 0.03, seed=71 + k), 1500.0 + 140.0 * k, 2.0), S.decay(n, 60.0))
        crackles.append((burst, t, 0.22 - 0.02 * k))
    out = S.layer([(voiced, 0.0, 1.0), (tail, 1.35, 0.14)] + crackles)
    out = S.pad(out, 2.2)
    out = S.lowpass(out, 1700.0)
    box = S.bandpass(out, 450.0, 2.2)
    out = S.mix(out, S.gain(box, 0.55))
    return S.dc_block(_far(out, low=150.0, high=1900.0, size=0.45, mix=0.24, damp=0.75))


# The Court's second prisoner (.ai/chapter-wizards.md 4.8): Ma Liang, painting in the Gallery. Three strokes of a wet
# brush along a wall and a drop running off the last: (onset, length, gain). StageHintCue.BrushStrokes is the same
# three onsets, and _BRUSH_DRIP its fourth beat - change one, change both.
_BRUSH_STROKES = [(0.06, 0.46, 1.0), (0.80, 0.36, 0.86), (1.40, 0.32, 0.76)]
_BRUSH_DRIP = 1.96


def _brush_stroke(dur, seed):
    """One stroke of a loaded brush drawn along plaster: the hair meeting the wall (a soft wet press, no click - a
    brush has no hard part), then the drag, which is what a brush sounds like - a hush of bristles over a rough
    surface, the band sliding up a little as the load runs out and the hairs go drier, its grain the bristles
    catching and letting go (an irregular chop at 50-80 Hz, a stick-slip, shallow), and under it the wet of the paint
    itself, a slow low smear. Lifted off at the end, not stopped: the drag thins and rises. Nothing in the set is a
    drag: the paper wind flutters, a whoosh is air, the mire's sounds are below 600 Hz."""
    press = S.env_mul(S.lowpass(S.osc("noise", 0, 0.07, seed=seed), 700.0), S.ad(0.07, 0.012, 3.0))
    body = S.env_mul(S.osc("sine", S.sweep(0.09, 230.0, 170.0, 0.7), 0.09), S.ad(0.09, 0.01, 3.5))
    drag = S.bandpass(S.osc("noise", 0, dur, seed=seed + 1), S.sweep(dur, 1150.0, 1750.0, 1.3), 1.4)
    grain = S.gate(S.osc("noise", 0, dur, seed=seed + 2), 64.0 + 9.0 * (seed % 3), duty=0.55, seed=seed + 3)
    drag = S.mix(S.gain(drag, 0.7), S.gain(S.bandpass(grain, S.sweep(dur, 1500.0, 2300.0), 2.0), 0.45))
    drag = S.env_mul(drag, S.adsr(dur, a=0.05, d=0.1, s=0.75, r=dur * 0.45))
    wet = S.lowpass(S.osc("noise", 0, dur, seed=seed + 4), 420.0)
    wet = S.env_mul(S.tremolo(wet, 7.0 + seed % 2, 0.5), S.adsr(dur, a=0.04, d=0.1, s=0.6, r=dur * 0.4))
    return S.layer([(press, 0.0, 0.7), (body, 0.0, 0.5), (drag, 0.03, 1.0), (wet, 0.02, 0.55)])


def _paint_drip(seed):
    """A drop of paint running off the end of a stroke and landing: a bead of liquid letting go (a short rising chirp,
    what a drop sounds like as it parts) and its small soft landing a beat after."""
    dur = 0.05
    plip = S.env_mul(S.osc("sine", S.sweep(dur, 780.0, 1560.0, 0.6), dur), S.ad(dur, 0.002, 2.5))
    land = S.env_mul(S.bandpass(S.osc("noise", 0, 0.04, seed=seed), 900.0, 1.6), S.ad(0.04, 0.002, 5.0))
    return S.layer([(plip, 0.0, 0.8), (land, 0.16, 0.35)])


def hint_brush():
    """The Court: a brush drawing itself along the wall, in the Gallery where he paints.

    Ma Liang, kept to paint the dark wizard's portraits (.ai/chapter-wizards.md 4.8): three strokes of a wet brush on
    plaster, the first long and the next two shorter, "running the same way", and a drop of paint running off the
    last. A drag and a drip: nothing else in the set is either. Indoors, in a long room of the house - a short bright
    tail, the top kept at 3.4 kHz for the bristles. The stroke onsets are _BRUSH_STROKES, the drop _BRUSH_DRIP, which
    StageHintCue lays its line of paint on. Also the sound of Paint What Is Needed drawing his flask (PaintedFlask)."""
    items = [(_brush_stroke(d, 4101 + 13 * i), t, g) for i, (t, d, g) in enumerate(_BRUSH_STROKES)]
    items.append((_paint_drip(4201), _BRUSH_DRIP, 0.62))
    items.append((_paint_drip(4202), _BRUSH_DRIP + 0.21, 0.4))
    out = S.pad(S.layer(items), 2.4)
    out = S.lowpass(S.highpass(out, 110.0), 3400.0)
    return S.dc_block(S.reverb(out, size=0.55, damp=0.55, mix=0.2))


# ---------------------------------------------------------------------------
# secrets - the light he missed (.ai/stage-secrets.md 7.7, .ai/achievements-and-secrets.md 4.9)
# ---------------------------------------------------------------------------
#
# A secret's near tell is a warm glint that BREATHES (stage-secrets section 2): a 2 x 2 point of
# warm light fading in and out on a slow period, the only animated pixel on a still object. These
# are that glint in sound, and the one rule they keep is the art's: breathed, never struck. Every
# reward in the game is a struck bell (level_up, chest_open, stage_clear, card_select, the heal),
# so a secret is a cloud of soft sine grains with no attack in it - each grain a Hann swell, the
# whole cloud swelling and going like the glint. Warm, not bright: the grains sit in A minor's
# pentatonic from A4 to E6, never on G5 (the light element's bell), and everything is lowpassed
# well under 4 kHz. Nothing here starts with palette.transient, because nothing here is a spell.

# The glint's notes: A minor pentatonic without its G, so no grain is ever the light element's G5.
_GLINT_NOTES = [0, 3, 5, 7, 12, 15, 17, 19, 24, 27, 31]      # semitones from A4: A4 ... E6


def _grain(freq, dur, seed):
    """One grain of the glint: a sine with a whisper of its octave, under a Hann window - it
    swells and goes, it is never struck. The phase is seeded so two grains never start alike."""
    n = S.n_samples(dur)
    rng = __import__("random").Random(seed)
    tone = S.mix(S.osc("sine", freq, dur, phase=rng.random()),
                 S.gain(S.osc("sine", freq * 2.0, dur, phase=rng.random()), 0.1))
    win = [0.5 - 0.5 * math.cos(2.0 * math.pi * i / max(1, n - 1)) for i in xrange(n)]
    return S.env_mul(tone, win)


def _glint_cloud(dur, count, shape, seed, notes=None, grain=(0.07, 0.16), first=None):
    """A cloud of grains whose density and level follow `shape`, a curve over the cloud's length:
    drawn by rejection from a seeded RNG, so the cloud is the same on every build. `first` puts
    one grain of that note at the very start, for a cloud that must answer at once."""
    rng = __import__("random").Random(seed)
    notes = notes or _GLINT_NOTES
    env = S.resample(shape, 512)
    items = []
    if first is not None:
        items.append((_grain(_hz(first), grain[0], seed * 31 - 1), 0.0, max(0.5, env[0])))
    tries = 0
    while len(items) < count and tries < count * 200:
        tries += 1
        t = rng.random() * dur
        weight = env[min(511, int(t / dur * 511))]
        if rng.random() > weight:
            continue
        g = rng.uniform(grain[0], grain[1])
        # Not rng.randrange or rng.choice: those draw differently under Python 3 and 2.7, and the
        # build must write the same bytes under either. random() and uniform() do not.
        f = _hz(notes[min(len(notes) - 1, int(rng.random() * len(notes)))])
        items.append((_grain(f, g, seed * 31 + len(items)), t, (0.55 + 0.45 * rng.random()) * weight))
    return S.pad(S.layer(items), dur)


def secret_seen():
    """A secret glimpsed (SecretDirector, the first time its near tell is on screen - the seen mark,
    achievements-and-secrets 4.3 and 4.9). The soft shimmer stage-secrets 7.7 asked for, and
    nothing else: no toast, no text, because those would be a pointer.

    One breath of the glint: a cloud of sine grains that thickens over 0.7 s and thins over the
    next second, under the faintest air of the same shape. It is not a tremolo (that is arcane's
    17 Hz), not detuned triangles (arcane again), not a run of glints (Starfall falls in 60 ms),
    and not a swell into a held tone (event_reward). Rare - once per secret per save as the seen
    mark is built - but soft on purpose: it is a nudge, and the tell itself is already on screen,
    so it is meant to be played AT the site (SfxPlayer.AtPosition), which takes another 5-6 dB
    off at the 450 px the seen mark is taken from and pans it toward the thing he has noticed."""
    dur = 1.75
    n = S.n_samples(dur)
    breath = S.ramp(n * 2 // 5, 0.05, 1.0, 1.4) + S.decay(n - n * 2 // 5, 1.6, 1.0, 0.0)
    cloud = _glint_cloud(dur, 26, breath, 601)
    air = S.bandpass(S.osc("noise", 0, dur, seed=607), 1250.0, 1.6)
    air = S.env_mul(S.lowpass(air, 2400.0), breath)
    out = S.mix(cloud, S.gain(air, 0.07))
    out = S.highpass(S.lowpass(out, 3400.0, 0.707), 260.0, 0.707)
    return S.dc_block(S.reverb(S.pad(out, 2.05), size=0.7, damp=0.5, mix=0.32))


def secret_found():
    """A secret found (SecretDirector.Found): the toast. The glint he saw, now his.

    Two gestures: the cloud GATHERS - 0.4 s of grains thickening toward the bloom, the first one on
    the frame he finds it, the light coming in to him - and then BLOOMS, a warm chord (F major
    seventh with its third on top, F3 F4 C5 E5 A5) rolled up in 18 ms steps, each voice a sine with
    a soft triangle under a lowpass that opens as it sounds, and three grains settling over it.
    Warm, and on purpose no brighter: spread up to E6 the bloom measured 0.112 from player_heal and
    0.121 from card_select - two struck bells - and a Fenced Bloom heals as it is found, so this
    and player_heal play together. The low F is kept soft: at full weight the centroid sat at
    570 Hz, down in the band the music owns. Not a bell (every other reward is one), not a
    swelling stack (meta_unlock, which keeps meaning "the save changed"), not a needle settling
    (bearing_found).
    A secret is found a few times a run - every flower pen blooms - so it sits beside event_reward
    in the once-a-minute band, which it replaces for secrets so that event_reward goes back to
    meaning a side event's chest."""
    dur = 1.6
    gather_dur = 0.4
    gn = S.n_samples(gather_dur)
    rise = S.ramp(gn, 0.45, 1.0, 1.4)
    gather = _glint_cloud(gather_dur, 12, rise, 613, notes=[3, 7, 12, 15, 19, 24], grain=(0.05, 0.1),
                          first=12)

    bloom_dur = dur - gather_dur + 0.1
    voices = []
    # F3 F4 C5 E5 A5 - F major seventh, the low F soft under it.
    for k, (semi, level) in enumerate(((-16, 0.45), (-4, 0.9), (3, 0.85), (7, 0.75), (12, 0.65))):
        f = _hz(semi)
        v = S.mix(S.osc("sine", f, bloom_dur, phase=0.13 * k),
                  S.gain(S.osc("tri", f, bloom_dur, phase=0.29 * k), 0.22))
        v = S.lowpass(v, S.sweep(bloom_dur, 700.0, 2600.0, 0.6), 0.8)
        v = S.env_mul(v, S.ad(bloom_dur, 0.03, 2.4))
        voices.append((v, 0.018 * k, level))
    bloom = S.layer(voices)
    settle = _glint_cloud(0.7, 3, [0.0, 1.0, 0.7, 0.2], 617, notes=[24, 19, 15], grain=(0.09, 0.14))

    out = S.layer([
        (gather, 0.0, 0.8),
        (bloom, gather_dur - 0.03, 0.85),
        (settle, gather_dur + 0.12, 0.45),
    ])
    out = S.lowpass(S.pad(out, dur), 3600.0, 0.707)
    return S.dc_block(S.reverb(out, size=0.72, damp=0.5, mix=0.3))


def secret_shatter():
    """The Geode Heart breaking (achievements-and-secrets 8.4.1): a crystal three men tall in a
    Sunken Cave cavern, 2,000 damage landed at its foot, and it shatters outward in a ring that
    freezes everything within 300 px. The game's loudest secret sound, once a run at most, so it
    sits in the once-a-run row with spell_evolve and meta_unlock.

    Four parts, in the order the eye gets them: two strain ticks a breath apart (the hairline
    cracks giving), the CRACK - a short band of noise over a low split, a sine falling 190 to 92 Hz
    and never under 80, because the sub band is the explosion's and the boss's - then the ring of
    shards going out, forty small crystal tinkles that start dense and thin out, falling in pitch as
    they fly farther, and a cold outward rush (a band falling 2.2 kHz to 500 Hz: frost spreads, the
    ice element's hiss rises). All of it in the cavern, a long room. The shards are two inharmonic
    partials at 1 : 2.43, not the ice element's 1 : 2.76 : 5.40 stack, and the whole is a cascade
    over a second and a half, where player_shield_break - the other glass breaking in the set - is
    one stack struck once in 0.65 s; a vault breaking must never sound like his shield going.
    The warm light rising out of the shards is NOT here: SecretDirector.Found plays its own sound a
    moment later (meta_unlock the first time, Frost Shard granted), and two stings in one second
    should not both carry the reward."""
    rng = __import__("random").Random(701)
    dur = 2.3

    # The strain: two hairline ticks, the second closer to the crack than the first.
    strain = []
    for k, (t, f) in enumerate(((0.0, 1500.0), (0.085, 1900.0))):
        tn = S.n_samples(0.02)
        tick = S.bandpass(S.osc("noise", 0, 0.02, seed=703 + k), f, 3.0)
        strain.append((S.env_mul(S.lowpass(tick, 3200.0), S.decay(tn, 6.0)), t, 2.0 + 0.6 * k))
    crack_at = 0.15

    cn = S.n_samples(0.05)
    snap = S.bandpass(S.osc("noise", 0, 0.05, seed=709), 1700.0, 0.9)
    snap = S.env_mul(S.lowpass(snap, 4200.0), S.decay(cn, 5.0))
    split_dur = 0.55
    sn = S.n_samples(split_dur)
    split = S.osc("sine", S.sweep(split_dur, 190.0, 92.0, 0.4), split_dur)
    split = S.mix(split, S.gain(S.osc("tri", S.sweep(split_dur, 380.0, 184.0, 0.4), split_dur), 0.25))
    split = S.env_mul(split, S.expdecay(sn, 5.5))

    # The ring of shards: dense at the crack, thinning, each a little lower than the last.
    shards = []
    for k in xrange(40):
        t = crack_at + 0.01 + min(1.5, rng.expovariate(1.0 / 0.32))
        fall = max(0.0, 1.0 - (t - crack_at) / 1.6)
        f = rng.uniform(1100.0, 1500.0) * (1.0 + 0.9 * fall)
        sd = rng.uniform(0.05, 0.16)
        tinkle = S.partials(f, [1.0, 2.43], sd, amps=[1.0, 0.3], decays=[7.0, 11.0])
        tinkle = S.env_mul(tinkle, S.ad(sd, 0.001, 1.0))
        shards.append((tinkle, t, (0.35 + 0.4 * rng.random()) * (0.35 + 0.65 * fall)))
    ring = S.lowpass(S.layer(shards), 4000.0, 0.707)

    rush_dur = 0.7
    rn = S.n_samples(rush_dur)
    rush = S.bandpass(S.osc("noise", 0, rush_dur, seed=719), S.sweep(rush_dur, 2200.0, 500.0, 0.7), 1.8)
    rush = S.env_mul(S.lowpass(rush, 3000.0), S.ad(rush_dur, 0.06, 2.0))

    out = S.layer(strain + [
        (snap, crack_at, 1.0),
        (split, crack_at, 0.9),
        (ring, 0.0, 0.75),
        (rush, crack_at + 0.03, 0.4),
    ])
    # Driven for weight, as spell_explosion and boss_death are, and lowpassed after the drive,
    # because a drive left last is a hiss (audio-direction 6). Its RMS over the whole 2.3 s reads
    # 5 dB under meta_unlock's at the same peak; over the crash itself (0.15-0.7 s) it is level with
    # it. The tail is what a crash is, and the drive cannot buy much there: at 3.2 the crest only
    # moved from 19.2 to 18.1 dB, so it stays at 2.2.
    out = S.lowpass(S.drive(S.pad(out, dur), 2.2), 4500.0, 0.707)
    out = S.highpass(out, 85.0, 0.707)
    return S.dc_block(S.reverb(out, size=0.9, damp=0.42, mix=0.36))


# ---------------------------------------------------------------------------
# registry
# ---------------------------------------------------------------------------
# (filename stem, category, peak target in dBFS, builder, loops)
#
# The peak targets are the mix. They are not arbitrary - see the loudness table in
# .ai/audio-direction.md, which explains why the XP orb is 20 dB under the boss.

# Casts sat at -9 and impacts at -13, both above their own band in the loudness table. The
# persistent pulses (Solar Flare, Toxic Spore, Cinderbreath) now cue every pulse, which put more
# casts in a minute than the table was drawn for; both come down to where the table puts them.
CAST_DB = -11.0
IMPACT_DB = -15.0

REGISTRY = []

for _el in P.ORDER:
    REGISTRY.append(("cast_%s" % _el, "cast", CAST_DB, _make_cast(_el), False))
for _el in P.ORDER:
    REGISTRY.append(("impact_%s" % _el, "impact", IMPACT_DB, _make_impact(_el), False))

REGISTRY += [
    ("spell_explosion",     "spell",  -5.0,  spell_explosion,     False),
    ("spell_beam_start",    "spell",  -11.0, spell_beam_start,    False),
    ("spell_beam_loop",     "spell",  -14.0, spell_beam_loop,     True),
    ("spell_beam_end",      "spell",  -12.0, spell_beam_end,      False),
    ("spell_orbit",         "spell",  -22.0, spell_orbit,         False),
    # Every Starfall landing, hit or miss: the most frequent spell sound, so under the orbit tick.
    ("spell_starfall",      "spell",  -24.0, spell_starfall,      False),
    # Every Familiar bite: a pack bites several times a second, so it sits with the orbit tick.
    ("spell_familiar_bite", "spell",  -21.0, spell_familiar_bite, False),
    # The facing family. A Stormsweep pass replaces its cast cue and comes up to three times a second
    # at level 8: the quiet end of the once-a-second row. A Sleet Rune cast is up to twenty a
    # second behind its throttle: down with the orbit tick.
    ("spell_stormsweep",    "spell",  -14.0, spell_stormsweep,    False),
    ("spell_sleet_rune",    "spell",  -22.0, spell_sleet_rune,    False),
    ("spell_summon",        "spell",  -9.0,  spell_summon,        False),
    # Elder God Maw's open: the maw's summon cue in place of spell_summon, so the same slot and the
    # same level - one maw every three to four seconds, between the once-a-second and once-a-minute
    # rows, at the summon's -9.
    ("spell_maw_open",      "spell",  -9.0,  spell_maw_open,      False),
    ("spell_bow_draw",      "spell",  -15.0, spell_bow_draw,      False),
    ("spell_bow_release",   "spell",  -10.0, spell_bow_release,   False),
    # Sagittarius's loose: one a loose, every second or two at most - the once-a-second row, a step under
    # the bow's own release, which it follows in the same run only if both parents were never fused.
    ("spell_sagittarius",   "spell",  -11.0, spell_sagittarius,   False),

    ("player_hurt",         "player", -6.0,  player_hurt,         False),
    ("player_dodge",        "player", -13.0, player_dodge,        False),
    ("player_heal",         "player", -11.0, player_heal,         False),
    ("player_shield_absorb", "player", -12.0, player_shield_absorb, False),
    ("player_shield_break", "player", -8.0,  player_shield_break,  False),
    ("player_death",        "player", -4.0,  player_death,        False),
    ("player_low_health",   "player", -14.0, player_low_health,   False),

    ("enemy_hurt_a",        "enemy",  -19.0, enemy_hurt_a,        False),
    ("enemy_hurt_b",        "enemy",  -19.0, enemy_hurt_b,        False),
    ("enemy_hurt_c",        "enemy",  -19.0, enemy_hurt_c,        False),
    ("enemy_death_small",   "enemy",  -14.0, enemy_death_small,   False),
    ("enemy_death_heavy",   "enemy",  -11.0, enemy_death_heavy,   False),
    ("enemy_shoot",         "enemy",  -14.0, enemy_shoot,         False),
    ("enemy_melee",         "enemy",  -12.0, enemy_melee,         False),
    ("elite_spawn",         "enemy",  -8.0,  elite_spawn,         False),
    ("boss_roar",           "enemy",  -4.0,  boss_roar,           False),
    ("boss_death",          "enemy",  -3.0,  boss_death,          False),

    ("pickup_xp",           "pickup", -23.0, pickup_xp,           False),
    ("pickup_health",       "pickup", -13.0, pickup_health,       False),
    ("pickup_magnet",       "pickup", -13.0, pickup_magnet,       False),
    ("pickup_haste",        "pickup", -12.0, pickup_haste,        False),
    ("event_reward",        "pickup", -8.0,  event_reward,        False),
    ("bearing_found",       "pickup", -8.0,  bearing_found,       False),
    ("level_up",            "pickup", -5.0,  level_up,            False),
    ("chest_open",          "pickup", -6.0,  chest_open,          False),
    ("card_appear",         "ui",     -15.0, card_appear,         False),
    ("card_select",         "ui",     -9.0,  card_select,         False),
    ("reroll",              "ui",     -12.0, reroll,              False),
    ("spell_evolve",        "pickup", -4.0,  spell_evolve,        False),

    ("ui_hover",            "ui",     -26.0, ui_hover,            False),
    ("ui_click",            "ui",     -16.0, ui_click,            False),
    ("ui_back",             "ui",     -16.0, ui_back,             False),
    ("ui_denied",           "ui",     -15.0, ui_denied,           False),
    ("ui_open",             "ui",     -14.0, ui_open,             False),
    ("ui_close",            "ui",     -15.0, ui_close,            False),
    ("ui_pause",            "ui",     -12.0, ui_pause,            False),

    ("stage_clear",         "meta",   -4.0,  stage_clear,         False),
    ("game_over",           "meta",   -5.0,  game_over,           False),
    ("meta_unlock",         "meta",   -4.0,  meta_unlock,         False),
    ("wave_warning",        "meta",   -7.0,  wave_warning,        False),

    # The defeat and its payout (DefeatSequence, GameOverScreen's tally). The fall lands under
    # player_death's tail and over nothing else - once a run, a body sound, a step under
    # player_hurt. The tick plays up to eighteen times a second for a second and a half: the
    # many-times-a-second row, a few dB over the orb because nothing else is playing then. The
    # payout is once a run and the screen's climax.
    ("player_fall",         "player", -7.0,  player_fall,         False),
    ("tally_tick",          "ui",     -20.0, tally_tick,          False),
    ("tally_payout",        "meta",   -6.0,  tally_payout,        False),

    # Four a run at most, and far away: the quiet end of the once-a-minute band. The game plays
    # them positionally a few hundred pixels toward their source, which takes another 3-4 dB off.
    ("hint_cell_cry",       "hint",   -9.0,  hint_cell_cry,       False),
    ("hint_grove_whisper",  "hint",   -9.0,  hint_grove_whisper,  False),
    ("hint_lantern_flare",  "hint",   -9.0,  hint_lantern_flare,  False),
    ("hint_ice_song",       "hint",   -9.0,  hint_ice_song,       False),
    ("hint_caravan_bell",   "hint",   -10.0, hint_caravan_bell,   False),
    ("hint_paper_wind",     "hint",   -10.0, hint_paper_wind,     False),
    ("hint_anvil",          "hint",   -10.0, hint_anvil,          False),
    ("hint_sibyl_voice",    "hint",   -10.0, hint_sibyl_voice,    False),
    ("hint_hut_step",       "hint",   -10.0, hint_hut_step,       False),
    ("hint_hull_knock",     "hint",   -10.0, hint_hull_knock,     False),
    ("hint_flask_chime",    "hint",   -10.0, hint_flask_chime,    False),
    # The Court's seer reading (SeersVoiceEvent): the chapter hint, and in the seek every few seconds from the room
    # his voice is in while the player looks for it - so it sits with the hints, not above them.
    ("hint_quatrain",       "hint",   -10.0, hint_quatrain,       False),
    ("hint_tomb_knock",     "hint",   -10.0, hint_tomb_knock,     False),
    ("hint_granary_voice",  "hint",   -10.0, hint_granary_voice,  False),
    # The Court's painter (DottedDoorEvent): the chapter's second hint, and his flask being painted (PaintedFlask).
    ("hint_brush",          "hint",   -10.0, hint_brush,          False),
    # The nine mouths' call (NineMouthsEvent): nine voices in two seconds, a call every few seconds, only
    # while the player is at the cliff - the once-a-second row. Hers is the louder of the two because it is
    # the one to hear; the eight echoes come a few dB under it and lose more again to the distance.
    ("sibyl_voice",         "hint",   -12.0, sibyl_voice,         False),
    ("sibyl_echo",          "hint",   -15.0, sibyl_echo,          False),

    # Secrets. The seen shimmer is once per secret per save and meant to be played AT the site, so
    # it loses 5-6 dB to distance before it is heard; the found sting is a few a run, beside
    # event_reward; the Geode Heart shatters once a run at most - the loudest secret sound, in the
    # once-a-run row (achievements-and-secrets 8.4.1 says a sound that rare may sit above a hit).
    ("secret_seen",         "secret", -9.0,  secret_seen,         False),
    ("secret_found",        "secret", -8.0,  secret_found,        False),
    ("secret_shatter",      "secret", -4.0,  secret_shatter,      False),
]
