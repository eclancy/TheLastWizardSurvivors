// Cone of Cold's gust (scripts/ConeBlast.cs): the blast of freezing air itself, as pixel art on the
// world's 2 px grid. One quad over the cone (and Zero Point's relay); every fragment snaps to the world
// cell and is measured against the cone the hit tested.
//
// WHY IT EXISTS (2026-11-01). Eric, playing 0.10.0: Cone of Cold "is very hard to see". All it drew was
// the shared ImpactDebris spray - a few dozen single art pixels of ice thrown out along the cone - and at
// the game's zoom an art pixel of ice is a speck: in a crowd the cast read as nothing at all, and on the
// pale floors (the Frozen Waste's snow) Ice's white core and pale hot are the
// floor's own colour, so most of the spray vanished there (.ai/art-direction.md, the Bonelight caution:
// "a pale blue effect on a deep blue floor is the one place this tone can fail"). Now the cast has MASS:
//
//   THE FRONT. A wall of frost crystals leaves his hand across exactly the cone's angle and coasts out to
//   exactly its reach in TRAVEL seconds, jagged (each crystal's point a hash of its place round the arc),
//   and breaks up there by dither - a one-shot front that lands on the radius, which section 3 allows.
//   From its leading edge back: a cell of the Frostweaver's azure deep-blue (the outline that carries it on
//   paper and on snow), a cell of Ice's white-hot core (what carries it on a dark floor), Ice's hot and mid,
//   and Ice's edge trailing off in dither. The core is held at full white and the edge is pushed MORE
//   saturated, as the caution asks.
//   THE GUST. Streaks of wind racing outward behind the front, one cell wide, dashed, in Ice's mid and
//   edge with an azure cell on their tail - sparse on purpose, so the bodies inside the cone stay readable.
//   THE BREATH. A puff of white-hot cold at his hand for the first instant.
//   THE RIME. As the front breaks up it leaves motes of ice sparkling inside the reach, gone by dither.
//
// THE RADIUS IS THE HIT. `radius` and `half_angle` are what ConeBlast.Strike tested, in WORLD px, about
// `cast_point` (the exact cast point, a fraction of a cell off the node) along `heading`. Nothing is drawn
// past the radius or outside the angle; a half angle of PI is the Long Winter's full ring. Zero Point's
// relay is the same drawing from `relay_tip`, its own reach and its own clock.
//
// Every colour is an opaque Bonelight tone; dither is the only gradient; the node's modulate (a
// Legendary's gold) multiplies in.
shader_type canvas_item;
render_mode unshaded;

uniform float radius = 150.0;
uniform float half_angle = 0.5236;
uniform float heading = 0.0;
uniform vec2 cast_point = vec2(0.0);
uniform float age = 0.0;
uniform float seed = 0.0;
// Zero Point's relay: seconds since it burst (below 0: not yet, or none), from where, how far.
uniform float relay_age = -1.0;
uniform vec2 relay_tip = vec2(0.0);
uniform float relay_radius = 0.0;
uniform float relay_half = 0.5236;

const vec3 ICE_CORE = vec3(0.957, 0.996, 1.0);        // ice core #F4FEFF
const vec3 ICE_HOT = vec3(0.784, 0.957, 1.0);         // ice hot #C8F4FF
const vec3 ICE_MID = vec3(0.498, 0.863, 0.973);       // ice mid #7FDCF8
const vec3 ICE_EDGE = vec3(0.180, 0.498, 0.769);      // ice edge #2E7FC4
const vec3 AZURE_BASE = vec3(0.165, 0.447, 0.847);    // azure base #2A72D8
const vec3 AZURE_SHADE = vec3(0.102, 0.243, 0.541);   // azure shade #1A3E8A

const float GRID = 2.0;
// Seconds for the front to reach the radius, and the whole gust's life (ConeBlast's, ImpactDebris's).
const float TRAVEL = 0.15;
const float LIFE = 0.42;

varying vec2 world;
varying flat vec2 origin;

void vertex() {
	world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
	origin = MODEL_MATRIX[3].xy;
}

float bayer4(vec2 c) {
	int x = int(mod(c.x, 4.0));
	int y = int(mod(c.y, 4.0));
	float m[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
	return (m[x + y * 4] + 0.5) / 16.0;
}

float hash11(float n) {
	return fract(sin(n * 12.9898 + 4.1414) * 43758.5453);
}

// A fract hash, not a sine one: sin() of a large world cell number loses its low bits in a float and
// bands into stripes along the rows.
float hash21(vec2 v) {
	vec3 p3 = fract(vec3(v.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float wrap_angle(float x) {
	return x - TAU * floor((x + PI) / TAU);
}

// One gust at p (world px from its cast point), t seconds old. Later parts are drawn over earlier
// ones: the rime, the streaks, the front, the breath.
bool gust(vec2 p, vec2 cell, float r, float h, float t, float salt, float b, out vec3 col) {
	col = ICE_MID;
	if (t < 0.0 || t >= LIFE || r < GRID * 2.0)
		return false;
	float d = length(p);
	if (d > r - GRID * 0.5)
		return false;
	float a = wrap_angle(atan(p.y, p.x) - heading);
	if (h < PI - 0.001 && abs(a) > h)
		return false;
	bool hit = false;
	float u = clamp(t / TRAVEL, 0.0, 1.0);
	float eased = 1.0 - (1.0 - u) * (1.0 - u) * (1.0 - u);
	float front = r * eased;
	// 0 while the front travels, 1 when the gust is gone.
	float fade = clamp((t - TRAVEL) / (LIFE - TRAVEL), 0.0, 1.0);
	// Place round the arc in px at the full reach, so a crystal keeps its place as the front moves.
	float arc = (a + PI) * r;

	// The rime: motes of ice left sparkling inside the reach as the front breaks up, cooling down the
	// ramp, every one inside the radius by its own cell.
	if (fade > 0.0 && d > r * 0.35) {
		float m = hash21(cell + vec2(salt * 3.1, seed));
		if (m < 0.045 * (1.0 - fade) + 0.01 && b > fade * 0.9) {
			col = fade < 0.3 ? ICE_CORE : (fade < 0.6 ? ICE_HOT : ICE_MID);
			hit = true;
		}
	}

	// The gust's streaks: one cell wide, racing outward behind the front, dashed; each its own length.
	float step_a = 0.15;
	float k = floor((a + PI) / step_a);
	float centre_a = (k + 0.5) * step_a - PI + (hash11(k + seed + salt) - 0.5) * step_a * 0.6;
	float across = abs(wrap_angle(a - centre_a)) * d;
	float reach = front - GRID * 4.0;
	float inner = r * (0.12 + 0.25 * hash11(k * 1.7 + seed));
	if (across < GRID * 0.5 && d > inner && d < reach && (h >= PI - 0.001 || abs(centre_a) < h)) {
		float dash = fract((d - t * r * 2.4) / 22.0 + hash11(k + 3.0 + salt));
		if (dash < 0.62 && b > fade * 1.2 - 0.05) {
			col = dash < 0.12 ? AZURE_BASE : (dash < 0.4 ? ICE_MID : ICE_EDGE);
			hit = true;
		}
	}

	// The front: a wall of jagged crystals coasting out to the reach. Its depth is thick while it flies
	// and thins as it lands; it breaks up by dither once it has arrived. Never past the radius.
	float crystal = floor(arc / 6.0);
	float jag = (hash11(crystal + seed * 7.0 + salt) - 0.35) * GRID * 4.0;
	float lead = min(front + jag * eased, r - GRID * 0.5);
	float depth = GRID * mix(7.0, 3.0, fade) * (0.6 + 0.8 * hash11(crystal * 3.3 + salt));
	float q = (lead - d) / GRID;
	if (q >= 0.0 && q * GRID < depth && b > fade * 1.15 - 0.1) {
		if (q < 1.0)
			col = AZURE_SHADE;
		else if (q < 2.0)
			col = ICE_CORE;
		else if (q < 3.0)
			col = fade < 0.5 ? ICE_HOT : ICE_MID;
		else if (q < 4.0)
			col = ICE_MID;
		else
			col = b < 0.5 ? ICE_EDGE : AZURE_BASE;
		hit = true;
	}

	// The breath: white-hot cold puffing from his hand for the first instant.
	float puff = r * 0.16 * (1.0 - u);
	if (d < puff && b < 0.75 * (1.0 - u)) {
		col = d < puff * 0.5 ? ICE_CORE : ICE_HOT;
		hit = true;
	}
	return hit;
}

void fragment() {
	vec2 cell = floor(world / GRID);
	vec2 centre = cell * GRID + GRID * 0.5;
	float b = bayer4(cell);
	vec3 col = ICE_MID;
	vec3 col2 = ICE_MID;
	bool hit = gust(centre - (origin + cast_point), cell, radius, half_angle, age, 0.0, b, col);
	// Zero Point's relay, thrown on from the tip of the first cone along the same heading.
	if (relay_radius > 0.0 && gust(centre - (origin + relay_tip), cell, relay_radius, relay_half, relay_age, 5.0, b, col2)) {
		col = col2;
		hit = true;
	}
	if (!hit)
		discard;
	COLOR = vec4(col, 1.0) * COLOR;
}
