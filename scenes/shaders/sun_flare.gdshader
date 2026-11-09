// The Eye of Ra's flare (scripts/SunFlare.cs, ScorchingRayBeam.FlareEyeOfRa): the sun breaking out where the hottest
// ray of a cast lands, as pixel art on the game's 2 px grid. One quad per flare, every flare on screen one batch.
//
// Eric, 2026-11-09: "lets upgrade imhotep's flare to look more interesting". It was a stroked ring, a translucent wash
// and twelve antialiased spokes - faint on a dark floor, and a translucent disc and a ring held on the radius are the
// area markers .ai/art-direction.md section 3 forbids. Now, in order:
//   - the sun's disc: a white-hot disc at the strike for the first instant, shrinking away;
//   - the light: a front of white-hot light thrown out from it that lands on EXACTLY the radius it blinds a fifth of a
//     second in, gold behind its leading edge, shimmering with heat along its length (inward only, never past the
//     radius), then breaking up by dither - a one-shot shockwave of the effect's own light ending on its radius,
//     never a ring held there;
//   - god-rays: long thin rays from the disc out toward the front, turning a little as they fade, more of them from a
//     hotter ray;
//   - the eye of Ra, the wedjat, flashing gold at the heart for a beat over a dark-gold backing so it reads on a pale
//     floor too.
// THE RADIUS IS THE BLIND: the radius in the vertex colour is the one FlareEyeOfRa tested, and nothing is drawn past it.
// Every value is computed once per 2 px cell of the world, every colour is an opaque Bonelight tone (the Light ramp and
// gold), and Bayer dithering is the only gradient, as in fireburst.gdshader.
//
// PER FLARE, IN THE VERTEX COLOUR (SunFlare._Draw; the node's modulate stays white):
//   r  age / SunFlare.Seconds, 0..1
//   g  the radius it blinds, / 1024
//   b  a seed, 0..1
//   a  the ray's heat, 0..1
// The quad is the flare's bounding square and its UV runs 0..1 across it.
shader_type canvas_item;
render_mode unshaded;

const vec3 LIGHT_CORE = vec3(1.0, 1.0, 1.0);         // light core #FFFFFF
const vec3 LIGHT_HOT = vec3(1.0, 0.973, 0.847);      // light hot #FFF8D8
const vec3 LIGHT_MID = vec3(1.0, 0.941, 0.659);      // light mid #FFF0A8
const vec3 LIGHT_EDGE = vec3(1.0, 0.776, 0.235);     // light edge #FFC63C
const vec3 GOLD_HI = vec3(1.0, 0.941, 0.753);        // gold hi #FFF0C0
const vec3 GOLD_LIT = vec3(0.847, 0.690, 0.290);     // gold lit #D8B04A
const vec3 GOLD_SHADE = vec3(0.329, 0.247, 0.063);   // gold shade #543F10

const float GRID = 2.0;
// The share of the flare's life the light takes to reach the radius (0.17 s of 0.5): a flash, faster than a blast.
const float ARRIVE = 0.34;

// The wedjat, 14 x 10 art pixels, row by row from the brow down; bit x is column x. Gold, then the pupil's white-hot
// and hot cells. The same eye as its icon's (tools/art/evolution_icons.py, eye_of_ra).
const int EYE_GOLD[10] = int[10](4094, 0, 1016, 3078, 12289, 4092, 8736, 9248, 6160, 16);
const int EYE_CORE[10] = int[10](0, 0, 0, 192, 192, 0, 0, 0, 0, 0);
const int EYE_HOT[10] = int[10](0, 0, 0, 32, 256, 0, 0, 0, 0, 0);

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

float noise1(float x, float s) {
	float i = floor(x);
	float f = fract(x);
	f = f * f * (3.0 - 2.0 * f);
	return mix(hash12(vec2(i, 0.0), s), hash12(vec2(i + 1.0, 0.0), s), f);
}

// 0 nothing, 1 gold, 2 white-hot, 3 hot.
int eye_at(ivec2 q) {
	if (q.x < 0 || q.y < 0 || q.x >= 14 || q.y >= 10)
		return 0;
	int bit = 1 << q.x;
	if ((EYE_CORE[q.y] & bit) != 0)
		return 2;
	if ((EYE_HOT[q.y] & bit) != 0)
		return 3;
	if ((EYE_GOLD[q.y] & bit) != 0)
		return 1;
	return 0;
}

void fragment() {
	float t = params.r;
	float radius = params.g * 1024.0;
	float s = params.b * 997.0;
	float heat = params.a;

	vec2 cell = floor(local / GRID);
	vec2 p = cell * GRID + GRID * 0.5;
	float d = length(p);
	if (d > radius - GRID * 0.5 || radius < GRID * 6.0)
		discard;
	float b = bayer4(cell);
	float u = d / radius;
	vec2 dir = d > 0.01 ? p / d : vec2(1.0, 0.0);

	vec3 col = vec3(0.0);
	bool hit = false;

	// God-rays: thin rays from the disc toward the front, 8 from a cold ray to 12 at full heat, turning as they fade.
	float k = clamp(t / ARRIVE, 0.0, 1.0);
	float front = radius * (1.0 - (1.0 - k) * (1.0 - k) * (1.0 - k));
	float n = floor(8.0 + 4.0 * heat + 0.5);
	float ang = atan(dir.y, dir.x) - (params.b * TAU + t * 0.6);
	float seg = TAU / n;
	float idx = floor(ang / seg + 0.5);
	float off = (ang - idx * seg) * d;
	float reach = front * (0.55 + 0.45 * hash12(vec2(mod(idx, n), 3.0), s));
	float halfw = d < radius * 0.3 ? GRID : GRID * 0.5;
	float rayFade = clamp((t - 0.2) / 0.7, 0.0, 1.0);
	if (abs(off) <= halfw && d < reach && d > 6.0) {
		float along = d / max(reach, 1.0);
		// Out from the heart and dimming toward the tip; the outer half dotted by dither, so the rays are light and
		// not bars over the crowd; the whole ray dissolves from the heart outward as the flare ages.
		bool lit = b > rayFade * 1.2 - along * 0.35 && (along < 0.5 || b < 0.7 - 0.4 * along);
		if (lit) {
			col = along < 0.35 ? LIGHT_HOT : (along < 0.7 ? LIGHT_MID : LIGHT_EDGE);
			hit = true;
		}
	}

	// The light: a front thrown out to the radius, white-hot on its leading edge and gold behind it, its edge shimmering
	// with heat - only inward of where it has got to.
	float wob = noise1(atan(dir.y, dir.x) * 7.0 + t * 26.0, s + 4.0);
	float edge = front * (1.0 - 0.03 * wob) - GRID * 0.5;
	float band = clamp(radius * 0.05, GRID * 2.0, GRID * 4.0);
	float gone = clamp((t - ARRIVE) / (1.0 - ARRIVE), 0.0, 1.0);
	if (d <= edge && d > edge - band) {
		float w = (edge - d) / band;
		if (b >= gone * 1.25 - 0.1) {
			col = w < 0.34 ? (gone < 0.3 ? LIGHT_CORE : LIGHT_HOT) : (w < 0.67 ? LIGHT_MID : LIGHT_EDGE);
			hit = true;
		}
	} else if (d <= edge - band && d > edge - band * 4.0) {
		// Heat shimmer just behind the front: a few cells of light flickering, thinning as it goes.
		float h = hash12(cell, s + floor(t * 24.0));
		if (h > 0.965 + 0.03 * gone) {
			col = h > 0.985 ? LIGHT_MID : LIGHT_EDGE;
			hit = true;
		}
	}

	// The sun's disc at the strike: white-hot for the first instant, shrinking away. Capped in world px, so a wide flare
	// is not a white screen.
	float flash = clamp(1.0 - t / 0.22, 0.0, 1.0);
	float hr = min(radius * 0.22, 24.0 + 12.0 * heat) * sqrt(flash);
	if (d < hr) {
		col = LIGHT_CORE;
		hit = true;
	} else if (d < hr * 1.35 && b < 0.65) {
		col = LIGHT_HOT;
		hit = true;
	} else if (d < hr * 1.7 && b < 0.3) {
		col = LIGHT_EDGE;
		hit = true;
	}

	// The eye of Ra at the heart, flashing gold for a beat and then dissolving; two 2 px cells to an art pixel (28 x 20
	// cells, about a body's width). One size whatever the heat: three cells from a hot ray was louder than a boss's
	// tell, and the light's reach already says how hot the ray was.
	if (t > 0.06 && t < 0.78) {
		float px = 2.0;
		ivec2 q = ivec2(floor((cell + vec2(7.0 * px, 5.0 * px)) / px));
		int e = eye_at(q);
		float eyeGone = clamp((t - 0.5) / 0.28, 0.0, 1.0);
		if (b >= eyeGone) {
			if (e == 2) {
				col = LIGHT_CORE;
				hit = true;
			} else if (e == 3) {
				col = LIGHT_HOT;
				hit = true;
			} else if (e == 1) {
				// The flash: bright gold, a beat of the hottest gold, then the lamp's own gold.
				col = t < 0.2 ? LIGHT_EDGE : (mod(floor(t * 20.0), 2.0) < 1.0 && t < 0.4 ? GOLD_HI : GOLD_LIT);
				hit = true;
			} else if (eye_at(q + ivec2(1, 0)) == 1 || eye_at(q - ivec2(1, 0)) == 1
				|| eye_at(q + ivec2(0, 1)) == 1 || eye_at(q - ivec2(0, 1)) == 1) {
				// A backing of dark gold round the strokes, so the eye reads on the white disc and on a pale floor.
				col = GOLD_SHADE;
				hit = true;
			}
		}
	}

	if (!hit)
		discard;
	COLOR = vec4(col, 1.0);
}
