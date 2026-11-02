// The thaw's frozen lake, alive (scripts/ThawLake.cs; .ai/side-events.md, "The thaw").
//
// TEXTURE is the lake as it lies (tools/art/thaw_lake.py). Two more copies of it - the light under
// the ice one step up and two steps up - and a data map are what this picks from. Every colour comes
// out of those generated textures and the two frost uniforms: nothing is blended, so nothing off the
// Bonelight contract can appear, and the Bayer matrix is the only gradient (the water's rule,
// .ai/terrain.md section 3).
//
// What moves:
//   - RINGS of cold light running out from the block under the ice, the way his song carries: bright
//     near him and gone by the edge of the clear window, which is about where fighting warms the
//     block, so the reach of the heat is where the light is. They brighten as the ice warms.
//   - WANDERING LIGHT: soft patches drifting slowly under the whole lake (the data map's B channel,
//     which tiles every 32 px, read at a sliding offset).
//   - FROST ferns creeping in from the shore and from the cracks and drawing back again over about
//     twenty seconds (each fern pixel's G is when in the cycle it grows in).
// The data map's R says how much light each pixel lets through: clear ice all of it, frost half,
// snow none - so the snow bank never lights and the clear window round him lights most.
shader_type canvas_item;

uniform sampler2D lit_tex : filter_nearest;
uniform sampler2D bright_tex : filter_nearest;
uniform sampler2D data_tex : filter_nearest, repeat_enable;
uniform vec4 frost_new = vec4(0.416, 0.494, 0.596, 1.0);   // steel.base #6A7E98
uniform vec4 frost_old = vec4(0.604, 0.643, 0.722, 1.0);   // skin.base #9AA4B8
// 0..1: how strongly he is singing (1 while he is in the ice, falling to 0 once he is free).
uniform float song = 1.0;
// 0..1: how far the thaw has got. The light under the ice grows with it.
uniform float heat = 0.0;
// The lake's own clock, so a paused tree stops the light with everything else.
uniform float clock = 0.0;
// The lake's semi-axes in texels (thaw_lake.py A, B): the rings run out as ellipses of that shape.
uniform vec2 axes = vec2(160.0, 128.0);
// Rings: texels a second outward, and texels between crests.
uniform float ring_speed = 9.0;
uniform float ring_spacing = 34.0;
// The frost cycle, in seconds.
uniform float frost_period = 22.0;

float bayer4(vec2 p) {
	int x = int(mod(p.x, 4.0));
	int y = int(mod(p.y, 4.0));
	int i = x + y * 4;
	float m[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
	return (m[i] + 0.5) / 16.0;
}

void fragment() {
	vec2 size = vec2(textureSize(TEXTURE, 0));
	ivec2 texel = ivec2(clamp(floor(UV * size), vec2(0.0), size - 1.0));
	vec4 base = texelFetch(TEXTURE, texel, 0);
	if (base.a < 0.5) {
		discard;
	}
	vec4 data = texelFetch(data_tex, texel, 0);
	vec2 p = vec2(texel) + 0.5 - size * 0.5;
	// Distance from him, in texels along the lake's short axis.
	float r = length(vec2(p.x * axes.y / axes.x, p.y));

	// Rings: narrow crests running outward, strongest near him.
	float phase = (r - clock * ring_speed * (1.0 + 0.6 * heat)) * 6.2831853 / ring_spacing;
	float crest = pow(max(0.0, cos(phase)), 6.0);
	float reach = 52.0 + 28.0 * heat;
	float rings = crest * exp(-r / reach) * 1.9 * song;
	// A steady glow round the block, under the ice.
	float core = smoothstep(46.0, 4.0, r) * (0.75 + 0.5 * heat) * song;
	// Wandering light: the tiling noise read at a slow drift, only its upper part.
	ivec2 wt = ivec2(mod(vec2(texel) + clock * vec2(2.4, 1.1), size));
	float wander = smoothstep(0.62, 0.9, texelFetch(data_tex, wt, 0).b) * (0.55 + 0.25 * song);

	float light = data.r * (rings + core + wander);
	float level = floor(clamp(light, 0.0, 1.99) + bayer4(vec2(texel)));
	vec4 col = base;
	if (level >= 2.0) {
		col = texelFetch(bright_tex, texel, 0);
	} else if (level >= 1.0) {
		col = texelFetch(lit_tex, texel, 0);
	}

	// Frost, over the light: a fern pixel is up while the cycle has passed the moment it grows in.
	// The cycle never falls all the way back, so the roots of the ferns along the shore stay.
	float grow = data.g * 255.0;
	if (grow < 254.5) {
		float g = grow / 250.0;
		float cycle = 0.6 + 0.36 * sin(clock * 6.2831853 / frost_period);
		if (g <= cycle) {
			col = g < cycle - 0.35 ? frost_old : frost_new;
		}
	}
	COLOR = vec4(col.rgb, 1.0);
}
