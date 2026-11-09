using Godot;

// THE FAR SKY'S LESSON (.ai/second-half.md 6.3, piece F4; Teachings.FarSkyLesson; tools/art/lessons.py, p_far_sky_*).
// Taught by the huntress, after her rekindling reel, so it opens on the White Downs under the moon she has just put
// back: two stars meeting over the downs; then their light coming down to the two stones the deal now favours.
// Its own file so the far shore's pieces, each adding a reel to Prologue, never edit the same lines; a method rather
// than a static field, so the order C# initialises the statics of a partial class's files in can never leave
// Prologue.BuildReels reading it before it exists.
public partial class Prologue
{
	private static Beat[] FarSkyBeats() => new Beat[]
	{
		// The downs at night, the moon up again, and two stars leaning together over them.
		new("far-sky-meet", 5.0f, "Each night the far sky names a Conjunction.", new Vector2(-3f, 0f), new Vector2(-10f, -4f), new Vector2(-20f, 0f)),
		// Their light coming down to two spells' stones: its two spells come to your hand first.
		new("far-sky-favour", 6.0f, "Its two spells come to your hand first.", new Vector2(0f, 3f), new Vector2(0f, -12f), new Vector2(0f, 12f)),
	};
}
