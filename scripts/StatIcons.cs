using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

/// <summary>
/// The glyph in front of a stat line: a sword for damage, an hourglass for cooldown, and so on.
/// The art is tools/art/stat_icons.py; this decides which glyph a line of text gets and lays the
/// lines out.
/// </summary>
/// <remarks>
/// WHY GLYPHS. A card's stats used to be one row of words in one ink - "DMG 20   CD 5.5s   RNG
/// 480" - so telling damage from cooldown meant reading the abbreviation, on a paused screen, in
/// the middle of a fight. A glyph is recognised rather than read. Colour stays a second cue (an
/// upgrade's lines are rubric, a boon's are bone) but meaning no longer depends on it, which is
/// also what makes the cards usable by a player who cannot tell the element hues apart.
///
/// WHY CLASSIFY TEXT rather than carry a stat id. Every summary is already built as text by
/// Player (BuildSpellStatSummary, BuildSpellUpgradeSummary), BoonCatalog.DescribeEffect and
/// ChestItemCatalog.GetDescription, and the balance harness and its reports read those strings.
/// Matching a keyword at the point of display costs one lookup per line and leaves every producer
/// alone. A line nothing matches gets the gold lozenge (<c>generic</c>) - a mark, never a gap - and
/// a line that opens with one of the milestone stars (✦ ★ ✶) gets nothing, because the star IS
/// its glyph.
///
/// ORDER MATTERS in <see cref="KeyFor"/>: "Slow duration" is slow, not duration; "Projectile
/// speed" is speed, not projectiles; "10 damage to nearby enemies when struck" is thorns, not
/// damage; "-1 damage taken" is the shield. The specific test always comes first.
/// </remarks>
public static class StatIcons
{
	private const string IconRoot = "res://assets/bonelight/ui/stats/stat-";

	/// <summary>The separator Player puts between the parts of a summary.</summary>
	public const string Separator = "   ";

	/// <summary>Drawn size: the 12px glyph at x2, which is a capital letter at the Body role.</summary>
	public const float IconSize = 24f;

	/// <summary>Which glyph a stat line takes, or null for a line that should have none.</summary>
	public static string KeyFor(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
			return null;

		string t = line.Trim();
		// The milestone lines carry their own star.
		if (t.StartsWith("✦") || t.StartsWith("★") || t.StartsWith("✶") || t.StartsWith("★"))
			return null;

		t = t.ToLowerInvariant();

		// The currency, by name: Marginalia's twin pays it (in-flight 98), and "reward" below would call it luck.
		if (t.Contains("arcane energy"))
			return "energy";
		if (t.Contains("crit"))
			return "crit";
		// "Re-weaves 9% sooner": Gloam Veil's recharge is a walk, not a timer, but it is still the
		// line that says how soon the spell is back, so it takes the recharge glyph.
		if (t.Contains("cooldown") || t.StartsWith("cd ") || t.Contains("attack speed") || t.Contains("cast speed")
			|| t.Contains("re-weave"))
			return "cooldown";
		if (t.Contains("poison") || t.Contains("dot ") || t.Contains("venom"))
			return "poison";
		if (t.Contains("burn") || t.Contains("ignite"))
			return "burn";
		if (t.Contains("freeze") || t.Contains("frozen") || t.Contains("chill"))
			return "freeze";
		if (t.Contains("root"))
			return "root";
		// Wayland's Wings, "+10% move speed. Floor traps can't touch you, and water doesn't slow you": his feet off the
		// ground, so the boot with the air streaming off it - not the slow glyph its last words would otherwise claim.
		if (t.Contains("floor trap"))
			return "speed";
		if (t.Contains("slow"))
			return "slow";
		if (t.Contains("knockback") || t.Contains("push"))
			return "knockback";
		if (t.Contains("pierce"))
			return "pierce";
		if (t.Contains("chain") || t.Contains("branch arc") || t.Contains("jump"))
			return "chain";
		if (t.Contains("pickup") || t.Contains("magnet"))
			return "magnet";
		// "Faster projectiles" is the speed line with no number on it (Player.FormatSpellEffectUpgrade):
		// it must take the speed glyph before the projectile-count test below can claim it.
		if (t.Contains("projectile speed") || t.Contains("faster projectile"))
			return "speed";
		// Black Tentacles counts its projectiles as tentacles, Toxic Spore Burst as mushrooms, Meteor
		// Swarm as meteors and Starfall as stars (Player.ProjectileNoun). "stars " only at the start:
		// "start" contains it. Stormsweep counts sweeps, Sleet Runes runes, Folio pages, Corposant balls, Windfall trees
		// and the fruit a tree carries, and Lancet needles, also only at the start: a card's sentence can use any of those
		// words ("it sweeps the ground", "the fruit falls", "the needles fly") without being a count.
		if (t.Contains("projectile") || t.Contains("tentacle") || t.Contains("mushroom") || t.Contains("meteor")
			|| t.StartsWith("stars ") || t.StartsWith("sweeps ") || t.StartsWith("runes ") || t.StartsWith("pages ")
			|| t.StartsWith("balls ") || t.StartsWith("trees ") || t.StartsWith("fruit ") || t.StartsWith("needles ")
			|| (t.Length > 1 && t[0] == 'x' && char.IsDigit(t[1])))
			return "projectiles";
		// A spell taken in a form counts in the form's own noun ("Axes +1", SpellVariant.CountNoun).
		int space = t.IndexOf(' ');
		if (space > 0 && space + 1 < t.Length && (t[space + 1] == '+' || char.IsDigit(t[space + 1]))
			&& SpellVariants.IsCountNoun(t[..space]))
			return "projectiles";
		if (t.Contains("duration") || t.Contains("zone"))
			return "duration";
		if (t.Contains("area") || t.Contains("radius") || t.Contains("splash"))
			return "area";
		if (t.StartsWith("rng ") || t.Contains("range"))
			return "range";
		if (t.Contains("speed"))
			return "speed";
		// The Reliquary Vial's overheal is a shield that healing fills, not a heal: "as a shield"
		// has to win before the word "healing" sends it to the regen glyph.
		if (t.Contains("as a shield"))
			return "shield";
		if (t.Contains("hp per") || t.Contains("regen") || t.Contains("heal") || t.Contains("mend"))
			return "regen";
		if (t.Contains("max hp") || t.Contains("max health") || t.EndsWith(" hp"))
			return "heart";
		// "misses": The Archmage's Overlooked, "the blow that would end you misses" - a hit avoided, as a dodge is.
		// "mirage": Morgan le Fay's Glamour, "1 in 10 blows strike a mirage of you instead" - a dodge.
		if (t.Contains("evasion") || t.Contains("dodge") || t.Contains("ignores one hit") || t.Contains(" misses")
			|| t.Contains("mirage"))
			return "evasion";
		if (t.Contains("luck") || t.Contains("chest") || t.Contains("relic") || t.Contains("reward"))
			return "luck";
		if (t.Contains("xp") || t.Contains("experience"))
			return "xp";
		if (t.Contains("when struck") || t.Contains("touching") || t.Contains("thorn") || t.Contains("retaliat"))
			return "thorns";
		if (t.Contains("shield") || t.Contains("armor") || t.Contains("armour") || t.Contains("defense")
			|| t.Contains("damage taken") || t.Contains("less damage"))
			return "shield";
		if (t.Contains("damage") || t.StartsWith("dmg "))
			return "damage";
		return "generic";
	}

	/// <summary>The glyph texture for a key, or null if the art is missing (never an error).</summary>
	public static Texture2D Load(string key)
	{
		return key == null ? null : BonelightSkin.LoadTextureSafe(IconRoot + key + ".png");
	}

	/// <summary>
	/// Spells the card abbreviations out. With a glyph in front, "DMG" was the one part of the line
	/// still asking to be decoded; the word costs a few characters and the cards have the room.
	/// </summary>
	public static string Expand(string part)
	{
		string t = part.Trim();
		if (t.StartsWith("DMG "))
			return "Damage " + t.Substring(4);
		if (t.StartsWith("CD "))
			return "Cooldown " + t.Substring(3);
		if (t.StartsWith("RNG "))
			return "Range " + t.Substring(4);
		if (t.Length > 1 && t[0] == 'x' && char.IsDigit(t[1]))
			return "Projectiles " + t.Substring(1);
		return t;
	}

	/// <summary>Splits a summary into its parts, in order, without empties.</summary>
	public static List<string> Split(string summary)
	{
		var parts = new List<string>();
		if (string.IsNullOrWhiteSpace(summary))
			return parts;
		foreach (string raw in summary.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
		{
			string p = raw.Trim();
			if (p.Length > 0)
				parts.Add(p);
		}
		return parts;
	}

	/// <summary>
	/// One line: glyph, then text. The text wraps; the glyph stays at the top of its line.
	/// </summary>
	public static Control BuildLine(string text, Color ink, ResponsiveLayout.TextRole role, bool expand = true,
		bool wrap = true)
	{
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 8);

		string key = KeyFor(text);
		Texture2D icon = Load(key);
		if (icon != null)
		{
			row.AddChild(new TextureRect
			{
				Name = "StatIcon",
				Texture = icon,
				CustomMinimumSize = new Vector2(IconSize, IconSize),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				TooltipText = key,
			});
		}

		var label = new Label
		{
			// The last two words bound together (ResponsiveLayout.NoWidow): a wrapped stat line never
			// leaves a single word on a line of its own (Eric, 2026-11-04).
			Text = ResponsiveLayout.NoWidow(expand ? Expand(text) : text),
			AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		ResponsiveLayout.SetFont(label, role);
		label.AddThemeColorOverride("font_color", ink);
		// A status named on the line is a keyword that opens what it does, and a "+1 Fire" a chip
		// (KeywordText) - the same Label back when the line names neither.
		row.AddChild(KeywordText.Wrap(label));
		return row;
	}

	/// <summary>
	/// A whole summary as a list, one glyph-led line per part. <paramref name="centred"/> centres
	/// the BLOCK and keeps the lines left-aligned inside it, so the glyphs form a column - a centred
	/// list with ragged glyphs reads as decoration rather than as a list.
	/// </summary>
	/// <remarks>
	/// A shrink-centred block cannot hold a wrapping label - the container asks the label for its
	/// minimum width, a wrapping label's minimum is one letter, and the result was "Damage" set one
	/// letter per line down the card. So a block is only centred when every line is short enough to
	/// stand unwrapped (<see cref="CentredLineLimit"/>); a long line (a boon's sentence) makes the
	/// list full-width, left-aligned and wrapping instead.
	/// </remarks>
	public static Control BuildList(string summary, Color ink, ResponsiveLayout.TextRole role, bool centred)
	{
		List<string> parts = Split(summary);
		bool shrink = centred && parts.TrueForAll(p => Expand(p).Length <= CentredLineLimit);
		var list = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = shrink ? Control.SizeFlags.ShrinkCenter : Control.SizeFlags.ExpandFill,
		};
		list.AddThemeConstantOverride("separation", 4);
		foreach (string part in parts)
			list.AddChild(BuildLine(part, ink, role, wrap: !shrink));
		return list;
	}

	/// <summary>The longest line a centred list will hold unwrapped: about 220px at the Body role.</summary>
	private const int CentredLineLimit = 22;

	/// <summary>WCAG 2 relative luminance of a colour.</summary>
	public static float RelativeLuminance(Color c)
	{
		static float Channel(float v) => v <= 0.03928f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
		return 0.2126f * Channel(c.R) + 0.7152f * Channel(c.G) + 0.0722f * Channel(c.B);
	}

	/// <summary>WCAG 2 contrast ratio between two opaque colours, 1 to 21.</summary>
	public static float ContrastRatio(Color a, Color b)
	{
		float la = RelativeLuminance(a);
		float lb = RelativeLuminance(b);
		return (MathF.Max(la, lb) + 0.05f) / (MathF.Min(la, lb) + 0.05f);
	}

	/// <summary>
	/// Whichever of near-black and white reads better on <paramref name="background"/>, by WCAG
	/// contrast. The old rule thresholded perceived brightness at 0.62 and put white on the poison
	/// and grass chips at about 2.3:1.
	/// </summary>
	public static Color ReadableOn(Color background)
	{
		var dark = new Color(0.06f, 0.06f, 0.07f);
		return ContrastRatio(dark, background) >= ContrastRatio(Colors.White, background) ? dark : Colors.White;
	}
}
