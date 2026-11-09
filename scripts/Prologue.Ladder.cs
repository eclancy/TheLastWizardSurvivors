using Godot;

// RUNG III'S LESSON (.ai/conjunction-progression.md, "the pair shares its light"; Teachings.PairSharesLesson). Taught by
// the Archivist's deed, after the Conjunctions lesson, so it opens on that lesson's own two panels - the two lights on
// their stones, then the two made one - with lines of its own: the keeper of the books kept how two pages are bound,
// and a Conjunction's two spells share their light. No new art. Its own file, and a method, for Prologue.FarSky.cs's
// reasons (a partial class's static order cannot bite, and no other piece edits these lines).
public partial class Prologue
{
	private static Beat[] PairSharesBeats() => new Beat[]
	{
		// Two lights, each on its own stone, leaning toward each other.
		new("conjunctions-two", 5.0f, "The Archivist kept how two pages are bound.", new Vector2(2f, 0f), new Vector2(10f, -2f), new Vector2(-18f, 0f)),
		// The two made one: each lends the other its light toward the path the recipe names.
		new("conjunctions-one", 6.0f, "Held together, two spells share their light.", new Vector2(0f, 3f), new Vector2(0f, -10f), new Vector2(0f, 12f)),
	};
}
