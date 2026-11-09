// Iron Palisade's Rolling Wall: the earth it churns up as it marches (scripts/IronPalisade.cs). One quad
// over the strip of ground the wall has crossed, on the ground layer under every body; every fragment
// snaps to the world's 2 px cell and is measured in the wall's own frame.
//
// WHY (2026-11-01). Eric, playing 0.10.0: the rolling walls "should dig up the dirt as it moves". A wall
// of iron stakes was gliding across the floor and leaving it as it found it. Now each stake ploughs a
// groove behind it, the ground between the grooves is turned over in lumps, and (in IronPalisade's own
// drawing) clods are kicked up off the stakes' feet. Pixel art on the Bonelight contract:
//
//   THE GROOVES. One per stake, along the way the wall walks, at the stake's place across the wall: a cell
//   of the soil's deep at the bottom, the wall of the groove facing the key (the side away from the upper
//   left) lit, the side toward it in shade - a trench lit as a trench is.
//   THE TURNED EARTH between them: lumps of the soil's base and shade picked cell by cell, a lit clod and
//   a dark pit here and there, its edges ragged where it meets the untouched floor.
//   THE SOIL'S TONES ARE THE FLOOR'S where the floor is earth or stone (IronPalisade.SoilFor): turf and
//   soil on grass, rubble on flagstones, snow and frozen ground on the Frozen Waste, sand in the Sands,
//   mud in the Swamp. Every one comes from the contract's material rows.
//   IT DISSOLVES, never fades: a cell of the strip goes out by Bayer dither as it ages - the oldest ground,
//   furthest behind the wall, first, so the trail is a wake that closes behind it - and the whole strip
//   goes out the same way once the wall has fallen (`fallen`).
//
// The strip is only the ground the wall's stakes crossed: from just behind the stakes to where the march
// began (`marched`), and the wall's length across. It hurts nothing; it is a picture of where the wall went.
shader_type canvas_item;
render_mode unshaded;

uniform vec2 facing = vec2(1.0, 0.0);
uniform float marched = 0.0;
uniform float half_length = 80.0;
uniform float spacing = 20.0;
uniform float march_speed = 75.0;
uniform float fallen = 0.0;
uniform float seed = 0.0;
uniform vec3 soil_lit = vec3(0.353, 0.541, 0.580);
uniform vec3 soil_base = vec3(0.165, 0.306, 0.345);
uniform vec3 soil_shade = vec3(0.329, 0.247, 0.063);
uniform vec3 soil_deep = vec3(0.180, 0.133, 0.031);

const float GRID = 2.0;
// How long the ground the wall crossed stays churned before it has dissolved, seconds.
const float TRAIL_SECONDS = 1.6;
// The key light's direction, from the upper left (.ai/art-direction.md section 2).
const vec2 KEY = vec2(-0.7071, -0.7071);

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

// A fract hash, not a sine one: sin() of a large world cell number loses its low bits in a float and
// bands into stripes along the rows.
float hash21(vec2 v) {
	vec3 p3 = fract(vec3(v.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

void fragment() {
	vec2 cell = floor(world / GRID);
	vec2 centre = cell * GRID + GRID * 0.5;
	vec2 p = centre - origin;
	vec2 across_dir = vec2(-facing.y, facing.x);
	float behind = -dot(p, facing);
	float across = dot(p, across_dir);
	float h = hash21(cell + vec2(seed, seed * 0.37));
	float h2 = hash21(cell * 1.7 + vec2(3.1, seed));

	// Only the ground the stakes crossed: just behind them back to where the march began, the wall's
	// length across, ragged at its sides.
	float ragged = (h2 - 0.5) * GRID * 3.0;
	if (behind < GRID * 2.0 || behind > marched || abs(across) > half_length + GRID * 2.0 + ragged)
		discard;

	// The wake closes: the longer since the wall passed a cell, the likelier it has dissolved; and all
	// of it once the wall is down.
	float since = behind / max(1.0, march_speed);
	float left = (1.0 - since / TRAIL_SECONDS) * (1.0 - fallen);
	float b = bayer4(cell);
	if (b >= left)
		discard;

	// The groove of the nearest stake, along the march.
	float k = floor((across + half_length) / spacing + 0.5);
	float groove = across - (k * spacing - half_length);
	vec3 col;
	if (abs(groove) < GRID * 0.75) {
		col = soil_deep;
	} else if (abs(groove) < GRID * 1.75) {
		// The groove's wall facing the key is lit; the one toward it lies in its own shadow.
		float side = sign(groove) * dot(across_dir, KEY);
		col = side < 0.0 ? soil_lit : soil_shade;
	} else {
		// The turned earth between the grooves: lumps, cell by cell, with the odd lit clod and dark pit,
		// thinner toward the ragged sides so it frays into the floor.
		float edge = clamp((half_length + GRID * 2.0 - abs(across)) / (GRID * 4.0), 0.0, 1.0);
		if (h > 0.25 + 0.6 * edge)
			discard;
		col = h < 0.05 ? soil_lit : (h < 0.11 ? soil_deep : (h2 < 0.55 ? soil_base : soil_shade));
	}
	COLOR = vec4(col, 1.0);
}
