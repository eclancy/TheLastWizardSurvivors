// THE PLAYER IS THE LAMP (LightPool.cs, .ai/light-pool.md). One full-screen quad, drawn twice by two
// nodes at two depths, both through this one material:
//
//   - the ROOM pass, over the floor, the walls and the decals and under everything standing on them
//     (DrawLayers.RoomDark): the dark proper, deep, with the warm lift at the heart of the pool;
//   - the BODIES pass, over every body and under every spell (sorted last among DrawLayers.Bodies): a
//     gentle dark with a high floor, so the swarm falls off into the dark without sinking into it.
//
// Which pass a fragment belongs to rides in its vertex colour (the node's DrawRect colour): r is how
// dark the dark gets there, g how much warmth is lifted into the pool. Everything else is shared, so
// the light points are uploaded once a frame for both.
//
// premultiplied blend: what is drawn is warm + dst * m, where m is the multiplier (1 in the light, the
// pass's floor in the dark). One quad darkens and lifts at once, with no read of the screen - a plain
// blend, which on the phone's tiled GPU stays in tile memory.
shader_type canvas_item;
render_mode blend_premul_alpha, unshaded;

// The wizard's light, in world units: full out to inner, the pass's floor from outer on.
uniform vec2 centre = vec2(0.0);
uniform float inner = 230.0;
uniform float outer = 740.0;
// 0 is no effect at all, 1 the chapter's full dark (StageEnvironmentProfile.Darkness x the setting).
uniform float strength = 1.0;
// The warm lift at the heart of the pool, premultiplied, and how far it carries.
uniform vec3 warm : source_color = vec3(0.16, 0.10, 0.03);
uniform float warm_reach = 300.0;

// The small lights: xy where (world), z the reach (world units), w how bright (0..1), NEGATIVE for a cold
// light that lifts the dark and lays no warmth (a boss arriving: LightPool.ColdLight). Sixteen plain
// uniforms rather than one array: an array is uploaded as a packed array, and a Variant of one carries
// a finalizable disposer - an allocation every frame (MireSurface learned it). Only the first
// light_count are read.
uniform int light_count = 0;
uniform vec4 light_0;
uniform vec4 light_1;
uniform vec4 light_2;
uniform vec4 light_3;
uniform vec4 light_4;
uniform vec4 light_5;
uniform vec4 light_6;
uniform vec4 light_7;
uniform vec4 light_8;
uniform vec4 light_9;
uniform vec4 light_10;
uniform vec4 light_11;
uniform vec4 light_12;
uniform vec4 light_13;
uniform vec4 light_14;
uniform vec4 light_15;

varying vec2 world;
varying vec2 pass;

void vertex() {
	world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
	pass = COLOR.rg;
}

// A small light's reach at a point: full in its heart, nothing past its radius.
float reach(vec2 at, vec4 l) {
	float d = length(at - l.xy) / max(l.z, 1.0);
	return (1.0 - smoothstep(0.15, 1.0, d)) * abs(l.w);
}

void fragment() {
	float r = length(world - centre);
	float lit = 1.0 - smoothstep(inner, outer, r);
	float heat = 1.0 - smoothstep(0.0, warm_reach, r);
	heat *= heat;

	vec4 lights[16] = vec4[16](light_0, light_1, light_2, light_3, light_4, light_5, light_6, light_7,
		light_8, light_9, light_10, light_11, light_12, light_13, light_14, light_15);
	for (int i = 0; i < 16; i++) {
		if (i >= light_count) {
			break;
		}
		float k = reach(world, lights[i]);
		lit = max(lit, k);
		if (lights[i].w > 0.0) {
			heat = max(heat, k * k);
		}
	}

	float m = mix(1.0, mix(pass.r, 1.0, lit), strength);
	COLOR = vec4(warm * (pass.g * strength * heat), 1.0 - m);
}
