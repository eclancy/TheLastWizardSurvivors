using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>What a boon does. Interpreted by <c>Player.ApplyBoon</c>.</summary>
/// <remarks>
/// An enum and a magnitude rather than a delegate, so the catalog is pure data with no dependency
/// on <c>Player</c> - which is what lets a validator read the whole roster without a running game.
/// </remarks>
public enum BoonEffect
{
	Armor,
	MaxHealth,
	Regen,
	MoveSpeed,
	SpellDamage,
	SpellArea,
	CooldownReduction,
	PickupRadius,
	Evasion,

	// Feeds GetLegendaryChance and GetBonusDropChance. It needs a boon because the passive spells
	// that used to grant it are gone, and without one the stat would sit at its shop value forever
	// and two systems would quietly stop scaling.
	Luck,

	// --- Reactive effects ---------------------------------------------------------------
	//
	// The two below are the only boons that DO something rather than being a number, and they
	// exist because removing passive spells removed every reactive effect in the game at once -
	// Thornmail retaliated, Frozen Bulwark froze the attacker, Stormguard arced. Losing them made
	// a defensive build indistinguishable from a bigger health bar, which is the thing a defensive
	// build is supposed to feel different from.
	//
	// Both answer being STRUCK rather than being hurt: they fire on a hit that landed, even one
	// fully eaten by armour or a shield. Answering only HP loss would mean the better your defence
	// the less your defence reacts, which is backwards. Player throttles both on one shared
	// cooldown so standing in a crowd cannot turn a reaction into a permanent aura.

	/// <summary>Damages everything near the player when the player is hit. Magnitude is base damage.</summary>
	Retaliate,

	/// <summary>Slows everything near the player when the player is hit. Magnitude is the speed multiplier.</summary>
	Chill,

	// --- Stance effects -----------------------------------------------------------------
	//
	// A boon that asks something of the player rather than sitting on him: it pays only once he
	// has stood still for BoonCatalog.StanceSeconds (Player.StillSeconds, the same stillness the
	// Brazier set reads). Added with the Brazier's stand-still rework so the mechanic can be met in
	// a run that never finds the three relics.

	/// <summary>Health per second while the player has stood still for <see cref="BoonCatalog.StanceSeconds"/>. Magnitude is HP per second.</summary>
	StanceRegen,

	// --- The first wave of .ai/new-items.md (section 7) ------------------------------------

	/// <summary>
	/// While the player walks, whichever spell is furthest from ready recharges faster - Taproot's mirror:
	/// a stand pays one, a kite pays the other. Magnitude is the extra rate (0.5 = 50% faster).
	/// </summary>
	WalkingRecharge,

	/// <summary>
	/// The third reaction: frightens every ordinary enemy near the player when he is struck, on the
	/// reactions' shared pulse and radius. Magnitude is the fright's seconds (Player.FearSeconds).
	/// </summary>
	Frighten,
}

public sealed class BoonDefinition
{
	public required string Id { get; init; }
	public required string Name { get; init; }

	/// <summary>A sentence of flavour: what the object is. Never the number - see <see cref="BoonCatalog.ExplainEffect"/>.</summary>
	/// <remarks>
	/// It used to end with the effect as well ("... +8% Armor.", "... Luck rises."), which put the one
	/// thing a buyer needs last, and gave three boons no number at all. Eric, 2026-10-28: "boon
	/// descriptions in the shop are hard to understand." The effect is said once now, from the
	/// effect and its magnitude, so the words can never drift from what the boon does.
	/// </remarks>
	public required string Description { get; init; }

	/// <summary>Element tags, same rules as a spell: never the same element twice.</summary>
	/// <remarks>
	/// ONE tag per boon since the boon rework (Eric, 2026-11-06, in-flight item 41). Boons carried
	/// two each when the cap was six; with two slots to start and four at most, two tags per boon
	/// would let two boons move four elements at once, and the trim is what pays for the extra
	/// capacity. The tag kept is each boon's FIRST - the element its icon is already lit in
	/// (tools/art/boon_icons.py bakes that light into the PNG), so no art moved. The tag does not
	/// deepen as the boon levels: a boon is one carrier at any level, as a spell is.
	/// <c>RegressionChecks.ValidateBoonRework</c> holds the one-tag rule.
	/// </remarks>
	public required (string Element, int Weight)[] ElementWeights { get; init; }

	/// <summary>Where the boon's 64px icon lives, drawn by tools/art/boon_icons.py.</summary>
	/// <remarks>
	/// Boons had no icon at all until now: the level-up card fell through to DefaultSpellIcon and
	/// tinted it by dominant element, so all fourteen were one picture in fourteen shades - worse
	/// than nothing, because it read as a spell the player had already been offered.
	///
	/// They are drawn on the MATERIAL rows rather than the element ramps, unlike spell icons. A
	/// spell icon is an effect going off; a boon is an object you are carrying, and that difference
	/// is doing real work in telling the two kinds of card apart.
	/// </remarks>
	public string IconPath => $"res://assets/bonelight/ui/boons/{Id}.png";

	public required BoonEffect Effect { get; init; }

	/// <summary>The effect at level 1. See <see cref="BoonCatalog.MagnitudeAt"/> for a held level.</summary>
	public required float Magnitude { get; init; }

	/// <summary>
	/// What each level past the first adds to <see cref="Magnitude"/>. Negative where smaller is
	/// stronger (Chill's speed multiplier). Level 2 of almost every boon equals what the boon did
	/// before boons levelled, so a player who takes one pick on an old favourite feels no change.
	/// </summary>
	public required float MagnitudePerLevel { get; init; }
}

/// <summary>
/// Boons: permanent pickups that sit beside the spell loadout, each levelling up to
/// <see cref="MaxLevel"/> when an owned one is picked again.
/// </summary>
/// <remarks>
/// See `.ai/passives-and-items.md`. Three rules shape every entry here.
///
/// **A boon levels, but must still be worth taking at level 1** (the rework, Eric 2026-11-06,
/// in-flight item 41 - boons never levelled before it). Level 1 is most of the effect and the
/// levels are a modest climb, never a back-loaded promise: level 2 of almost every boon is what
/// the boon did before levels existed, so the floor the roster was balanced on still holds.
///
/// **A boon does not occupy a spell slot.** The six spell slots are what make a spell choice a
/// decision; a boon competing for one would just be a passive spell again, which is the thing
/// these replace. Boons have their own slots - two to start, four with both shop slots
/// (<c>Player.BaseBoonSlots</c>, <c>Player.MaxBoonSlots</c>) - and that cap is what keeps element
/// tags honest, because uncapped tagged boons would put every capstone within reach at once.
///
/// **The roster is deliberately lopsided toward Metal, Water, Light, Grass and Darkness.** Those
/// five elements get most of their carriers from passive spells today, and passive spells are being
/// removed; without that weighting their capstones become unreachable. `RegressionChecks`
/// .ValidateElementReachability is the check that holds this honest.
/// </remarks>
public static class BoonCatalog
{
	private static readonly BoonDefinition[] Definitions =
	{
		// --- Metal ------------------------------------------------------------------
		new()
		{
			Id = "iron_rivets", Name = "Iron Rivets",
			Description = "Plates riveted over what the robe does not cover.",
			ElementWeights = new[] { ("Metal", 1) },
			Effect = BoonEffect.Armor, Magnitude = 0.06f, MagnitudePerLevel = 0.02f,
		},
		new()
		{
			Id = "quicksilver_bead", Name = "Quicksilver Bead",
			Description = "It never stops moving, and neither do your hands.",
			ElementWeights = new[] { ("Metal", 1) },
			Effect = BoonEffect.CooldownReduction, Magnitude = 0.06f, MagnitudePerLevel = 0.02f,
		},
		new()
		{
			Id = "sunsteel_filament", Name = "Sunsteel Filament",
			Description = "A thread of drawn light, warm to the touch.",
			ElementWeights = new[] { ("Light", 1) },
			Effect = BoonEffect.Armor, Magnitude = 0.06f, MagnitudePerLevel = 0.02f,
		},

		// --- Water ------------------------------------------------------------------
		new()
		{
			Id = "tidewater_flask", Name = "Tidewater Flask",
			Description = "Salt water that closes what it is poured on.",
			ElementWeights = new[] { ("Water", 1) },
			// 0.4 to 0.8 a second until the healing budget (.ai/difficulty-curve.md, section 5):
			// level 3 (0.2) is a third over the old flat flask, paid for by two more picks.
			Effect = BoonEffect.Regen, Magnitude = 0.10f, MagnitudePerLevel = 0.05f,
		},
		// Was a third +8% Armor boon, alongside Iron Rivets and Sunsteel Filament. That redundancy
		// was an artifact of unifying damage reduction: the three used to sit on three different
		// mechanisms and read as three different things, and collapsing those into one Armor stat
		// quietly turned them into three copies of one card. Five of thirteen boons on two effects,
		// against six slots, meant a likely draw of the same card twice.
		//
		// Water-tagged (its first tag, the light its icon is baked in). It carried Metal too until
		// the one-tag trim; Metal keeps Iron Rivets and Quicksilver Bead.
		new()
		{
			Id = "saltbound_chain", Name = "Saltbound Chain",
			Description = "Crusted links that lash out at whatever strikes you.",
			ElementWeights = new[] { ("Water", 1) },
			Effect = BoonEffect.Retaliate, Magnitude = 8f, MagnitudePerLevel = 4f,
		},
		new()
		{
			Id = "deepwater_pearl", Name = "Deepwater Pearl",
			Description = "Things drift toward it.",
			ElementWeights = new[] { ("Water", 1) },
			Effect = BoonEffect.PickupRadius, Magnitude = 30f, MagnitudePerLevel = 10f,
		},

		// --- Light ------------------------------------------------------------------
		new()
		{
			Id = "lantern_oil", Name = "Lantern Oil",
			Description = "Burns brighter than it should.",
			ElementWeights = new[] { ("Light", 1) },
			Effect = BoonEffect.SpellDamage, Magnitude = 0.08f, MagnitudePerLevel = 0.02f,
		},
		new()
		{
			Id = "gilded_mote", Name = "Gilded Mote",
			Description = "A fleck of the warded circle, still lit.",
			ElementWeights = new[] { ("Light", 1) },
			Effect = BoonEffect.MaxHealth, Magnitude = 15f, MagnitudePerLevel = 5f,
		},

		// --- Grass ------------------------------------------------------------------
		new()
		{
			Id = "mossgrown_charm", Name = "Mossgrown Charm",
			Description = "Older than the occupation, and still growing.",
			ElementWeights = new[] { ("Grass", 1) },
			Effect = BoonEffect.MaxHealth, Magnitude = 20f, MagnitudePerLevel = 5f,
		},
		new()
		{
			Id = "thornseed", Name = "Thornseed",
			Description = "It puts out roots wherever a spell lands.",
			ElementWeights = new[] { ("Grass", 1) },
			Effect = BoonEffect.SpellArea, Magnitude = 0.09f, MagnitudePerLevel = 0.03f,
		},

		// --- Darkness ---------------------------------------------------------------
		new()
		{
			Id = "shadegrease", Name = "Shadegrease",
			Description = "Nothing quite gets hold of you.",
			ElementWeights = new[] { ("Darkness", 1) },
			Effect = BoonEffect.MoveSpeed, Magnitude = 0.06f, MagnitudePerLevel = 0.02f,
		},
		new()
		{
			Id = "wishing_coin", Name = "Wishing Coin",
			Description = "Thrown into still water by someone who is not coming back.",
			ElementWeights = new[] { ("Water", 1) },
			Effect = BoonEffect.Luck, Magnitude = 2f, MagnitudePerLevel = 1f,
		},

		// --- Ice ---------------------------------------------------------------------
		//
		// Ice-tagged (its first tag, its icon's light). Its Grass second tag went with the one-tag
		// trim; Grass keeps Mossgrown Charm and Thornseed.
		new()
		{
			Id = "rimebriar", Name = "Rimebriar",
			Description = "Frost-blackened thorns close around anything that reaches you.",
			ElementWeights = new[] { ("Ice", 1) },
			Effect = BoonEffect.Chill, Magnitude = 0.55f, MagnitudePerLevel = -0.10f,
		},

		new()
		{
			Id = "umbral_veil", Name = "Umbral Veil",
			Description = "A hand's width of somewhere else.",
			ElementWeights = new[] { ("Darkness", 1) },
			Effect = BoonEffect.Evasion, Magnitude = 0.04f, MagnitudePerLevel = 0.01f,
		},

		// --- Earth: the stance -------------------------------------------------------
		//
		// The stand-still boon, the defensive half of the Brazier's offer: the set pays damage for
		// standing in the swarm, this pays for surviving it. Healing, not Armor, on purpose. What
		// a planted player takes is the swarm's one-damage touches, and Armor is floored at 1 - it
		// never deletes a hit (Player.TakeDamage) - so "take 15% less damage while still" would
		// change nothing against exactly the hits standing still invites, and the card would lie
		// to the one player who took it. Regeneration is the stat that answers chip.
		//
		// 1 a second at level 1 and 2 at level 3: it climbs from Grass tier 2's regen (1) to tier
		// 4's (2) and never past the element, with the old flat 1.5 as its middle level - the same
		// premium the conditional relics carry over their unconditional cousins, earned by levels
		// now rather than granted at the door.
		//
		// Named as an object, like every boon (a root, not a "stance"), and tagged Earth: the aura
		// and the ring at the feet are earthen. Its Grass second tag went with the one-tag trim.
		new()
		{
			Id = "taproot", Name = "Taproot",
			Description = "It only feeds once it is in the ground.",
			ElementWeights = new[] { ("Earth", 1) },
			Effect = BoonEffect.StanceRegen, Magnitude = TaprootRegenPerSecond, MagnitudePerLevel = TaprootRegenPerLevel,
		},

		// --- Wind: the walk -----------------------------------------------------------
		//
		// Walking is the one condition nothing paid (.ai/new-items.md section 7): Taproot pays a stand, this
		// pays a kite, and holding both means whichever you are doing pays. It winds ONE spell - the one
		// furthest from ready - so it suits a build with one big slow spell (Meteor Swarm, Starfall). A
		// starter because Lightning had the thinnest floor in the game. Wind-tagged: its light is the wheel's.
		new()
		{
			Id = "waywiser", Name = "Waywiser",
			Description = "A wheel that counts the road, and pays out for every turn.",
			ElementWeights = new[] { ("Wind", 1) },
			Effect = BoonEffect.WalkingRecharge, Magnitude = 0.4f, MagnitudePerLevel = 0.1f,
		},

		// --- Darkness: the third reaction ----------------------------------------------
		//
		// Saltbound Chain hurts, Rimebriar slows and this scatters: a reactive build gets three answers on
		// one pulse. It gives #69's Fear a carrier that is not a Darkness spell. The Gaoler's deed pays it,
		// since a jailer carries a rattle.
		//
		// The one boon whose level 1 is the old flat value rather than its level 2: a fright under
		// Player.FearSeconds (1.5s) reads as a flicker, so the levels climb from there instead.
		new()
		{
			Id = "bone_rattle", Name = "Bone Rattle",
			Description = "Shake it, and whatever is close remembers it is afraid.",
			ElementWeights = new[] { ("Darkness", 1) },
			Effect = BoonEffect.Frighten, Magnitude = Player.FearSeconds, MagnitudePerLevel = 0.5f,
		},
	};

	/// <summary>How long the player must stand still before a stance boon takes hold.</summary>
	/// <remarks>
	/// Long enough that a stance is a decision - a pause between two dodges does not earn it - and
	/// short enough that it is there for the whole of a stand. Any movement starts it again: a
	/// stance is all or nothing, where the Brazier's heat cools on a slope.
	/// </remarks>
	public const float StanceSeconds = 1.5f;

	private const float TaprootRegenPerSecond = 1.0f;
	private const float TaprootRegenPerLevel = 0.5f;

	/// <summary>
	/// Every boon's level cap. One number for the whole roster on purpose: the card pips, the pause
	/// screen and the level-up pool all read it, and a per-boon cap would be fourteen numbers for a
	/// distinction no boon has earned. Three keeps a two-slot run to at most six boon picks - the
	/// same count the old six flat slots allowed - and level 2 is the old flat value, so the climb
	/// is one step down and one step up from what shipped.
	/// </summary>
	public const int MaxLevel = 3;

	public static IReadOnlyList<BoonDefinition> All => Definitions;

	public static BoonDefinition GetById(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;

		return Definitions.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
	}

	public static bool IsBoon(string id) => GetById(id) != null;

	/// <summary>The boon's magnitude at a held level, clamped to the roster's cap.</summary>
	public static float MagnitudeAt(BoonDefinition definition, int level)
	{
		if (definition == null)
			return 0f;
		int held = Math.Clamp(level, 1, MaxLevel);
		return definition.Magnitude + (held - 1) * definition.MagnitudePerLevel;
	}

	/// <summary>
	/// What the boon does, said plainly as a sentence, with its number: the shop's line, and anywhere
	/// a player has room to read rather than glance. <paramref name="level"/> picks the held level's
	/// numbers; the shop and a level-up's new-boon card read level 1.
	/// </summary>
	/// <remarks>
	/// The shop led with the flavour and ended on the effect, and three boons never said how much:
	/// "Pickups are drawn from further away", "Hitting you costs damage", "Luck rises". Each line here
	/// names what changes for the player in the game's own words - a hit, your spells, the orbs - and
	/// the reactions say how near and how often, which is the part nobody could guess.
	/// </remarks>
	public static string ExplainEffect(BoonDefinition definition, int level = 1)
	{
		if (definition == null)
			return string.Empty;

		float m = MagnitudeAt(definition, level);
		string once = Player.BoonReactionCooldownSeconds <= 1f ? "once a second" : $"once every {Player.BoonReactionCooldownSeconds:0.#} s";
		return definition.Effect switch
		{
			BoonEffect.Armor => $"Every hit you take does {m * 100f:0}% less damage.",
			BoonEffect.MaxHealth => $"+{m:0} maximum health.",
			BoonEffect.Regen => $"You heal {m:0.0} health every second.",
			BoonEffect.MoveSpeed => $"You move {m * 100f:0}% faster.",
			BoonEffect.SpellDamage => $"All your spells deal {m * 100f:0}% more damage.",
			BoonEffect.SpellArea => $"All your spells cover {m * 100f:0}% more area.",
			BoonEffect.CooldownReduction => $"All your spells recharge {m * 100f:0}% faster.",
			// A percentage, never a flat distance (Eric, 2026-11-04): the share of the reach every
			// pickup starts with (Player.BasePickupAttractDistance).
			BoonEffect.PickupRadius => $"XP and drops fly to you from further away: {Player.PickupRadiusPercent(m)} pickup range.",
			BoonEffect.Evasion => $"A {m * 100f:0}% chance to dodge a hit completely.",
			BoonEffect.Luck => $"+{m:0} Luck: a little more critical-hit chance, more bonus drops, and a better chance of a legendary spell.",
			BoonEffect.Retaliate => $"When you are hit, every enemy close around you takes {m:0} damage (at most {once}).",
			BoonEffect.Chill => $"When you are hit, every enemy close around you is slowed to {m * 100f:0}% speed for {Player.BoonChillDurationSeconds:0.#} s (at most {once}).",
			BoonEffect.StanceRegen => $"Stand still and you heal {m:0.0} health every second until you move.",
			BoonEffect.WalkingRecharge => $"While you walk, whichever of your spells is furthest from ready recharges {m * 100f:0}% faster.",
			BoonEffect.Frighten => $"When you are struck, every enemy close around you is frightened (at most {once}).",
			_ => string.Empty,
		};
	}

	/// <summary>The boon's effect as a stat line, with no flavour, at a held level.</summary>
	/// <remarks>
	/// A boon's Description carries a sentence of flavour and the number together, which was fine
	/// while the level-up card printed prose. The card shows stats only now, and a boon has no
	/// SpellData to read them off, so the line is rebuilt from the effect and the magnitude here -
	/// the one place that already knows what a magnitude means for each effect.
	/// </remarks>
	public static string DescribeEffect(BoonDefinition definition, int level = 1)
	{
		if (definition == null)
			return string.Empty;

		float m = MagnitudeAt(definition, level);
		return definition.Effect switch
		{
			BoonEffect.Armor => $"+{m * 100f:0}% Armor",
			BoonEffect.MaxHealth => $"+{m:0} Max HP",
			BoonEffect.Regen => $"+{m:0.0} HP per second",
			BoonEffect.MoveSpeed => $"+{m * 100f:0}% Move Speed",
			BoonEffect.SpellDamage => $"+{m * 100f:0}% Spell Damage",
			BoonEffect.SpellArea => $"+{m * 100f:0}% Spell Area",
			BoonEffect.CooldownReduction => $"-{m * 100f:0}% Cooldowns",
			BoonEffect.PickupRadius => $"{Player.PickupRadiusPercent(m)} Pickup Radius",
			BoonEffect.Evasion => $"+{m * 100f:0}% Evasion",
			BoonEffect.Luck => $"+{m:0} Luck",
			BoonEffect.Retaliate => $"{m:0} damage to nearby enemies when struck",
			BoonEffect.Chill => $"Slows nearby enemies to {m * 100f:0}% when struck",
			// "HP per" picks the regen glyph (StatIcons.KeyFor); the condition leads, because it is
			// the part that makes this a different card from Tidewater Flask.
			BoonEffect.StanceRegen => $"Stand still: +{m:0.0} HP per second",
			// "cooldown" picks the cooldown glyph (StatIcons.KeyFor); the condition leads, as Taproot's does.
			// "Slowest spell" read as a riddle (Eric, 2026-11-06): name the real rule, furthest from ready.
			BoonEffect.WalkingRecharge => $"Walking: the spell furthest from ready winds {m * 100f:0}% faster",
			// "when struck" picks the reactions' glyph, as Saltbound Chain's and Rimebriar's lines do.
			BoonEffect.Frighten => "Frightens nearby enemies when struck",
			_ => string.Empty,
		};
	}

	/// <summary>
	/// What the next level adds, for the level-up card of an owned boon - the same register as a
	/// spell upgrade's delta lines (Player.BuildSpellUpgradeSummary): the change for a stat that
	/// adds, the new value for an effect whose whole number is the point.
	/// </summary>
	public static string DescribeLevelGain(BoonDefinition definition, int nextLevel)
	{
		if (definition == null)
			return string.Empty;

		float step = definition.MagnitudePerLevel;
		float next = MagnitudeAt(definition, nextLevel);
		return definition.Effect switch
		{
			BoonEffect.Armor => $"Armor +{step * 100f:0}%",
			BoonEffect.MaxHealth => $"Max HP +{step:0}",
			BoonEffect.Regen => $"+{step:0.0} HP per second more",
			BoonEffect.MoveSpeed => $"Move Speed +{step * 100f:0}%",
			BoonEffect.SpellDamage => $"Spell Damage +{step * 100f:0}%",
			BoonEffect.SpellArea => $"Spell Area +{step * 100f:0}%",
			BoonEffect.CooldownReduction => $"Cooldowns -{step * 100f:0}% more",
			BoonEffect.PickupRadius => $"Pickup Radius +{step:0}",
			BoonEffect.Evasion => $"Evasion +{step * 100f:0}%",
			BoonEffect.Luck => $"Luck +{step:0}",
			BoonEffect.Retaliate => $"Damage when struck +{step:0}",
			// The multiplier's whole value is the effect, so the card says where it lands.
			BoonEffect.Chill => $"Slows nearby enemies to {next * 100f:0}% when struck",
			BoonEffect.StanceRegen => $"Stand still: +{next:0.0} HP per second",
			BoonEffect.WalkingRecharge => $"Walking: winds the spell furthest from ready {next * 100f:0}% faster",
			BoonEffect.Frighten => $"Fright lasts {next:0.0} s",
			_ => string.Empty,
		};
	}
}
