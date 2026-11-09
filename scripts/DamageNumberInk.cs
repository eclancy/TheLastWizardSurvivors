using Godot;
using System;

namespace TheOneHeMissed.scripts;

// THE INK A DAMAGE NUMBER IS WRITTEN IN (.ai/art-direction.md, "Damage numbers wear the element";
// .ai/genre-lessons.md item 4). Every number an enemy throws used to be red whatever hit it, so a
// screen of six spells said nothing about which of them was doing the work. Now a number is tinted by
// the element of what dealt it: the spell's (SpellData.GetNumberElement), or the element a hit carries
// without one (a poison tick, the Lightning tier's chain, the boon chain's steel shards).
//
// ONE OUTLINE, AND A RAMP STOP PER ELEMENT. No colour at all clears 4.5:1 on both the palest ground
// (taken as white, the worst case) and the darkest floor stone (#0B0F18): it would have to be darker than
// 0.18 luminance for the one and lighter than 0.20 for the other. Ice's mid on white is 1.6:1. And the
// ground a number is actually read against is usually an enemy - it rises from the body it struck, and a
// skeleton's bone is near white. So every number is outlined in occlusion (#05070C, the palette's shared
// contact shadow), opaque, and each element writes
// in the stop of its own emissive ramp that (a) clears 4.5:1 against that outline - the body-text floor
// of .ai/level-up-screen.md section 4a, held here although a number is large text, because it is read
// moving and fading - and (b) keeps the twelve as far apart from each other as the ramps allow. The
// outline makes the ground irrelevant to the letterforms, which is what lets one table serve every
// chapter. tools/art/ui_contrast.py measures it ("damage number" rows) and RegressionChecks
// .ValidateDamageNumberInks holds it.
//
// Why the stops are not all "mid" (the ramp's identity tone, and ElementColors.GetColor's): four pairs
// of ramps share a hue family - Grass and Poison, Light and Lightning, Arcane and Darkness, Ice and
// Water. At the mids, Grass and Poison were 5.2 apart (CIEDE2000; about 2 is "just noticeable" side by
// side, and a number is seen for 0.7 s in a crowd), and Darkness's mid fails the floor against the
// outline (4.2:1), so it takes its hot - 3.8 from Arcane's mid. Each pair is split by moving one of them
// to another stop of its own ramp: Poison to its pale hot, Light to its gold edge, Arcane to its pale
// hot, and Wind to its teal edge, out of the pale cluster Metal and Ice sit in. Closest pairs after:
// Lightning and Poison 10.5, Grass and Poison 10.6, Ice and Water 12.7. Every stop is still a colour of
// that element's own ramp.
//
// Grass took its deep edge at first (#4A8C3A, which kept every pair 12.7 apart), but on the Enchanted
// Forest's dark teal floor - Grass's own chapter - it was 2.2:1 against the ground itself: legible inside
// its outline, and still the one number in the photographs that had to be looked for. Moving Poison
// instead keeps both bright.
//
// A CRIT is written in its element's own ink, told apart only by the "!" and the 1.4x size it always
// had (Eric, 2026-11-08). It used to burn one stop hotter toward the white-hot core, and every
// element's crit came out near-white and alike - which said "crit" and lost which spell it was. Before
// that it was gold for every spell.
//
// Damage with no element (Bastion of Spikes, the Iron Fang, a spell that names
// no element) keeps the red every number was, crit or not. A turned enemy's blow on its own kind - not his hit at all - is written in
// bone, the horde's own colourless material (TurnedBlow); its old warm gold was Light's gold now.
//
// Per hit this is two array reads: the inks are built once, here, from ElementColors.GetRamp.
public static class DamageNumberInk
{
	/// <summary>The stroke round every damage number: the palette's occlusion, opaque.</summary>
	public static readonly Color Outline = new(5f / 255f, 7f / 255f, 12f / 255f, 1f);

	/// <summary>
	/// The outline's thickness in the label's own pixels, before FloatingText's 2x label scale - the
	/// size the HUD's level numerals use (LoadoutHud.NumeralOutlineSize).
	/// </summary>
	public const int OutlineSize = 4;

	/// <summary>The floor every ink is held to against <see cref="Outline"/>: WCAG 2 body text.</summary>
	public const float ContrastFloor = 4.5f;

	/// <summary>A hit with no element: the red every number used to be. Its crit is the same red.</summary>
	public static readonly Color Untyped = new(1f, 0.2f, 0.2f, 1f);

	/// <summary>
	/// A turned enemy's blow on one of its own (Enemy.TakeTurnedBlow): bone's base (#9AA4B8), the horde's own
	/// colourless material (.ai/world-and-tone.md: the dead have no faction colour). Not his hit, so no element's
	/// ink. It was the stone's warm gold (#FFDB75), 7.3 from Light's gold edge and 6.4 from Fire's crit; bone's
	/// base is 12.7 from the nearest ink (Metal's), as far as the elements are from each other.
	/// </summary>
	public static readonly Color TurnedBlow = new(0.604f, 0.643f, 0.722f, 1f);

	/// <summary>A stop on an element's emissive ramp, white-hot core to saturated edge.</summary>
	public enum RampStop
	{
		Core = 0,
		Hot = 1,
		Mid = 2,
		Edge = 3,
	}

	/// <summary>The stop an ordinary hit of <paramref name="element"/> is written in. See the header.</summary>
	public static RampStop StopFor(Element element) => element switch
	{
		Element.Fire => RampStop.Mid,
		Element.Ice => RampStop.Mid,
		Element.Arcane => RampStop.Hot,
		Element.Darkness => RampStop.Hot,
		Element.Light => RampStop.Edge,
		Element.Grass => RampStop.Mid,
		Element.Earth => RampStop.Mid,
		Element.Wind => RampStop.Edge,
		Element.Lightning => RampStop.Mid,
		Element.Poison => RampStop.Hot,
		Element.Metal => RampStop.Mid,
		Element.Water => RampStop.Mid,
		_ => RampStop.Mid,
	};

	public static Color StopColor(ElementColors.Ramp ramp, RampStop stop) => stop switch
	{
		RampStop.Core => ramp.Core,
		RampStop.Hot => ramp.Hot,
		RampStop.Mid => ramp.Mid,
		_ => ramp.Edge,
	};

	private static readonly Color[] Inks = BuildInks();

	private static Color[] BuildInks()
	{
		Array elements = Enum.GetValues(typeof(Element));
		var inks = new Color[elements.Length];
		foreach (Element element in elements)
			inks[(int)element] = StopColor(ElementColors.GetRamp(element), StopFor(element));
		return inks;
	}

	/// <summary>
	/// The ink for a hit of <paramref name="element"/> (null: no element). A crit is written in the SAME ink: it is
	/// told apart by its "!" and its 1.4x size alone (Eric, 2026-11-08), so it still says which spell it was. The
	/// argument stays so every call site keeps reading as "this hit, crit or not". Allocation-free: read once per hit
	/// in a full swarm.
	/// </summary>
	public static Color For(Element? element, bool crit)
	{
		if (element is Element e && (uint)e < (uint)Inks.Length)
			return Inks[(int)e];
		return Untyped;
	}
}
