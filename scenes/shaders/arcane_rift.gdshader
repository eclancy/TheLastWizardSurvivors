// Sustain's ringing ground (ArcaneExplosion.OpenLingeringRift, drawn by scripts/BlastPoolSurface.cs): where the
// Canticle's chord sounded, the note rings on in the floor for a while and hurts what stands in it. On the
// ground, under the bodies. (Lingering Rift's id and zone; the card is Sustain since 2026-11-09, when the spell
// became a chord sung on the music's beat - .ai/character-fit.md section 5, row 19.)
//
// IT IS A STANDING WAVE. The rift it replaced was a hole torn in the floor with arms of light wound out of it; a
// held note is the floor itself ringing. So: concentric bands of the note's light - the antinodes of a wave
// standing in the disc - each swinging between bright and dark, the neighbours in opposite phase, which is what
// makes it read as RINGING rather than as rings going out. The swing is the note's own, fast; on every bite (the
// zone's half-second tick) the whole floor flares, so the beat it hurts on is still the beat it pulses on. A
// struck point glints at the middle, and in its last moments it dissolves from the edge in, by dither.
//
// THE RADIUS IS THE HIT: LingeringZone.Radius, the reach its ticks test. The wave has an antinode exactly there -
// cos(PI * n * (1 - u)) is 1 at u = 1 - so the outermost band's outer edge IS the reach, and nothing is drawn past
// it. That band is a solid ring of the floor ringing, not a dotted outline (crater.gdshader's lesson was about a
// ring of flakes standing alone at an edge; this one is one of the bands of the thing itself). Every value is
// computed once per 2 px cell and every colour is an opaque Arcane, Darkness or violet tone; Bayer dithering is
// the only gradient.
//
// PER RIFT, IN THE VERTEX COLOUR (BlastPoolSurface): r how much of its life has gone (0..1), g the radius
// / 1024, b a seed, a its age in seconds / 4. The quad is its bounding square, UV 0..1.
shader_type canvas_item;
render_mode unshaded;

const vec3 ARC_CORE = vec3(1.000, 0.941, 1.000);     // arcane core #FFF0FF
const vec3 ARC_HOT = vec3(0.910, 0.753, 1.000);      // arcane hot #E8C0FF
const vec3 ARC_MID = vec3(0.753, 0.549, 1.000);      // arcane mid #C08CFF
const vec3 ARC_EDGE = vec3(0.478, 0.247, 0.831);     // arcane edge #7A3FD4
const vec3 DARK_EDGE = vec3(0.208, 0.125, 0.361);    // darkness edge #35205C
const vec3 VIOLET_DEEP = vec3(0.078, 0.063, 0.122);  // robe violet deep #14101F

const float GRID = 2.0;
const float SEEP_SECONDS = 0.2;
// LingeringZone's tick for the rift (ArcaneExplosion.OpenLingeringRift).
const float TICK_SECONDS = 0.5;
// How fast the floor swings: a note's ring, far faster than the bite.
const float SWING_HZ = 3.0;
// The bands' spacing in world px, two to four of them from the middle to the reach, and how far a band reaches
// either side of its antinode: a band three or four cells wide.
const float BAND_SPACING = 40.0;
const float BAND_HALF = 3.5;

varying vec2 local;
varying flat vec4 params;

void vertex() {
	params = COLOR;
	float r = COLOR.g * 1024.0;
	local = (UV - vec2(0.5)) * 2.0 * vec2(r);
}

float bayer4(vec2 c) {
	int x = int(mod(c.x, 4.0));
	int y = int(mod(c.y, 4.0));
	float m[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
	return (m[x + y * 4] + 0.5) / 16.0;
}

float hash12(vec2 p, float s) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031 + s * 0.0137);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

void fragment() {
	float life = params.r;
	float radius = params.g * 1024.0;
	float s = params.b * 97.0;
	float age = params.a * 4.0;

	vec2 cell = floor(local / GRID);
	vec2 p = cell * GRID + GRID * 0.5;
	float d = length(p);
	float seep = mix(0.35, 1.0, clamp(age / SEEP_SECONDS, 0.0, 1.0));
	float reach = (radius - GRID) * seep;
	if (d > reach || radius < GRID * 4.0)
		discard;
	float u = d / reach;
	float b = bayer4(cell);

	float gone = clamp((life - 0.85) / 0.15, 0.0, 1.0);
	if (b < gone * 1.3 - (1.0 - u) * 0.3)
		discard;

	// The bite: a flare on every tick, fading over the next third of a second. Past four seconds the age
	// stops being carried, and the flare keeps its own beat.
	float since = age < 3.99 ? mod(age, TICK_SECONDS) : mod(TIME, TICK_SECONDS);
	float flare = 1.0 - clamp(since / 0.3, 0.0, 1.0);

	vec3 col;
	if (d < GRID * 2.0) {
		// The struck point at the middle: a glint of the note's core.
		col = hash12(cell, s + floor(TIME * 8.0)) > 0.4 ? ARC_CORE : ARC_HOT;
	} else {
		// The wave: n antinodes from the rim in, the outermost ON the rim (k = 0), the neighbours in opposite
		// phase. Its shape is fixed in the floor; only its sign swings, so the bands blink against each other in
		// place. Few and thin, and mostly in the dark end of the ramp: at Sustain and Fermata together it reaches
		// past 250 px, and the first draw - a band every 16 px, a third of each half-wave wide, lit to the hot -
		// covered the field like a target, louder than any boss's telegraph. Now a band is BAND_HALF either side
		// of its antinode whatever the reach, and the floor shows between the bands, and through them as they dim.
		float n = clamp(floor((radius - GRID) / BAND_SPACING), 2.0, 4.0);
		float k = (1.0 - u) * n;
		float antinode = floor(k + 0.5);
		if (abs(k - antinode) * reach / n > BAND_HALF + (b - 0.5) * GRID)
			discard;
		float swing = cos(TIME * TAU * SWING_HZ + s);
		float v = (mod(antinode, 2.0) < 0.5 ? 1.0 : -1.0) * swing;
		// Dimmer is sparser: at the bottom of its swing a band is a dotted line on the floor, at the top nearly whole.
		if (b > 0.35 + 0.55 * abs(v) + flare * 0.3)
			discard;
		float level = abs(v) * 0.75 + flare * 0.4 + (b - 0.5) * 0.15;
		if (v >= 0.0)
			col = level > 0.75 ? ARC_MID : (level > 0.4 ? ARC_EDGE : DARK_EDGE);
		else
			col = level > 0.75 ? ARC_EDGE : DARK_EDGE;
	}
	COLOR = vec4(col, 1.0);
}
