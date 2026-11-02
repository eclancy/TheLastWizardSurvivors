// The dark of the dark wizard's second phase (DarkVeil.cs, .ai/endgame-castle.md 6.4): "the hall goes dark
// but for him and the player". One quad over the floor under every body; two clear pools of light, the
// wizard's and his, falling off in four ordered-dithered stops on the 2 px art grid, so the edge of the
// dark is pixels and not a blur.
shader_type canvas_item;

uniform vec2 light_a = vec2(0.0);
uniform vec2 light_b = vec2(0.0);
uniform float radius_a = 170.0;
uniform float radius_b = 150.0;
uniform float strength = 0.0;
uniform vec4 veil_color : source_color = vec4(0.02, 0.01, 0.04, 1.0);

varying vec2 world;

void vertex() {
	world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

float pool(vec2 at, vec2 centre, float radius) {
	// Squashed onto the floor the way everything on the ground is drawn.
	vec2 d = (at - centre) / vec2(radius, radius * 0.72);
	return 1.0 - smoothstep(0.45, 1.0, length(d));
}

void fragment() {
	vec2 cell = floor(world / 2.0);
	vec2 c = mod(cell, 2.0);
	float bayer = c.x < 0.5 ? (c.y < 0.5 ? 0.125 : 0.875) : (c.y < 0.5 ? 0.625 : 0.375);
	float lit = max(pool(world, light_a, radius_a), pool(world, light_b, radius_b));
	float dark = clamp(1.0 - lit, 0.0, 1.0);
	float stepped = floor(dark * 4.0 + bayer) / 4.0;
	COLOR = vec4(veil_color.rgb, strength * 0.9 * clamp(stepped, 0.0, 1.0));
}
