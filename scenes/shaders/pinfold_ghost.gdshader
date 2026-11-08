shader_type canvas_item;
render_mode unshaded;

uniform bool ghosted = true;
uniform float release_amount : hint_range(0.0, 1.0) = 0.0;

float bayer4(vec2 p) {
	int x = int(mod(p.x, 4.0));
	int y = int(mod(p.y, 4.0));
	float m[16] = float[16](0.0,8.0,2.0,10.0,12.0,4.0,14.0,6.0,3.0,11.0,1.0,9.0,15.0,7.0,13.0,5.0);
	return (m[x + y * 4] + 0.5) / 16.0;
}

void fragment() {
	vec4 c = COLOR;
	float coverage = ghosted ? 0.78 + sin(TIME * 2.8) * 0.08 : 1.0 - release_amount;
	if (c.a < 0.5 || bayer4(floor(UV / TEXTURE_PIXEL_SIZE)) >= coverage) discard;
	if (ghosted) {
		float light = dot(c.rgb, vec3(0.22, 0.70, 0.08));
		// Existing bone/indigo stops: drained colour, no smooth translucent wash.
		c.rgb = light > 0.62 ? vec3(0.863,0.910,1.0)
			: light > 0.34 ? vec3(0.659,0.722,0.863)
			: light > 0.15 ? vec3(0.384,0.431,0.518) : vec3(0.220,0.251,0.310);
	}
	COLOR = vec4(c.rgb, 1.0);
}
