shader_type canvas_item;

// The wizard's body sprite (Player's AnimatedSprite2D). Two jobs, one material each, both on this shader:
//
// THE WHITE. Renders the sprite as a flat white silhouette while a post-menu invincibility grace period is
// running. Applied as a material rather than via modulate because modulate multiplies (so it can darken or
// tint but never whiten), and because the player's modulate already carries the per-character tint and the
// Blur dodge flash - overwriting it would clobber both.
//
// THE WADE (in-flight 74, Player.Wading.cs). Wading, his feet sink in: the whole figure drops sink_drop
// texels and nothing at or below the waterline is drawn - DISCARDED, never faded, so the boots, the shins
// and the contact line under them are simply under the water - and the last row above it is laid in
// waterline_ink, the water's glint, where he meets it. In the sprite's own texels (the node scales them x2),
// whole numbers only, so he sinks a whole art pixel at a time on the grid he is drawn on. The ordinary
// material (whiten 0) carries it while he is not white, and the white one carries the same numbers, so a
// grace taken in the water is a white figure still standing in it.

uniform float whiten : hint_range(0.0, 1.0) = 1.0;
// How much of him is there: 1 for every whitened grace but one. Overlooked (the Archmage's passive) draws
// him a pale ghost for its ten seconds (Player.OverlookedOpacity; Eric, 2026-11-09: "semi-transparent for
// those 10 seconds, to make it clear that he is untouchable"). Player.SetInvincibilityVisual puts it back
// to 1 when the white ends, so a later ordinary grace is opaque.
uniform float opacity : hint_range(0.0, 1.0) = 1.0;
// How far the figure has dropped, in texels; 0 out of the water.
uniform float sink_drop = 0.0;
// The local y (texels, the sprite's own space) of the water's surface: a texel drawn at or below it is not
// drawn at all. Far below the cell out of the water, so nothing is cut.
uniform float waterline = 100000.0;
uniform vec4 waterline_ink : source_color = vec4(1.0);

// The texel row a fragment came from, before the drop: VERTEX is the sprite's local space, one unit a texel.
varying float cell_y;

void vertex()
{
	cell_y = VERTEX.y;
	VERTEX.y += sink_drop;
}

void fragment()
{
	// COLOR arrives as texture(TEXTURE, UV) * MODULATE, so mixing only rgb preserves the
	// sprite's alpha and keeps transparent pixels transparent.
	COLOR.rgb = mix(COLOR.rgb, vec3(1.0), whiten);
	COLOR.a *= opacity;
	// Where this texel is drawn once dropped: under the water, nothing; on the row just above it, the line.
	float drawn = floor(cell_y) + sink_drop;
	if (drawn >= waterline) {
		discard;
	}
	if (drawn >= waterline - 1.0 && COLOR.a > 0.5) {
		COLOR.rgb = waterline_ink.rgb;
	}
}
