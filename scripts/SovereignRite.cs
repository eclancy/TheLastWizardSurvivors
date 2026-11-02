using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace WizardSurvivors.scripts;

/// <summary>What the Rite would pour into one spell: the four numbers, as the boon carries them.</summary>
public readonly record struct SovereignGains(int Fuel, float DamageMultiplier, float CooldownMultiplier,
	float AreaMultiplier, float ScaleMultiplier, int AmountBonus);

/// <summary>
/// The single authority on the Sovereign Rite's numbers and rules (.ai/sovereign-spell.md, approved by Eric
/// 2026-11-02): who may be anointed, what counts as fuel, what the fuel buys, and when the strip offers it.
/// </summary>
/// <remarks>
/// The Rite is a Conjunction (ComboCatalog.SovereignRiteId) whose second ingredient is whichever active spell
/// the player points it at. Taken once, late: every OTHER active spell falls silent for the rest of the run
/// (Player.IsSilenced), and the one it anoints gains from every level those silenced spells hold - their
/// levels are its fuel, and they keep levelling (an upgrade card for one reads "+1 fuel to Sovereign X").
///
/// The gain is a milestone the spell carries (SpellData.SovereignBoon), so it reaches every stat reader
/// through the one list they all walk (SpellData.CollectActive) - no reader names the Rite. It carries
/// damage, cooldown, area (or, for a spell that has no area, its drawn scale) and amount, and nothing else:
/// no effect, no rider, no slow, no knockback. So it cannot open a crowd-control or an execute path on a
/// boss; bosses take its damage in full and every clamp they have still applies
/// (RegressionChecks.ValidateSovereignRite holds both).
/// </remarks>
public static class SovereignRite
{
	/// <summary>The Rite's own id: its SpellData (SpellData_SovereignRite.tres), its recipe and its unlock entry.</summary>
	public const string SpellId = "sovereign_rite";

	/// <summary>The boon's id, as SpellData.HasEvolution would read it (it never does: the Rite is no branch).</summary>
	public const string BoonId = "sovereign_rite_boon";

	// --- the offer (section 3) ------------------------------------------------------------------------

	/// <summary>The player level the strip waits for. 25 in the doc's first draft; moved by section 6's
	/// measurement so a planned build is first offered it at minute 11-12 (section 8, "Tuned").</summary>
	public const int MinimumPlayerLevel = 28;

	/// <summary>Active spells held, the anointed one among them.</summary>
	public const int MinimumActiveSpells = 3;

	/// <summary>The fuel the chosen spell must be offered: the other active spells' levels, summed.</summary>
	public const int MinimumFuel = 16;

	// --- the power (section 4), per level of fuel -------------------------------------------------------

	// Section 4 started at 7%, which made the build's main spell, anointed at the gate's 16 fuel, x2.3-3.0 the
	// whole loadout against a 1.3 target: the cooldown, area and amount steps carry most of that on their own.
	// Tuned by section 6's measurement to 0.5%, which measured x1.34-1.44 (section 8, "Tuned").
	public const float DamagePerFuel = 0.005f;
	public const float CooldownPerFuel = 0.008f;
	/// <summary>The cooldown never falls below this share of what it was (-30%).</summary>
	public const float CooldownFloor = 0.70f;
	public const float AreaPerFuel = 0.01f;
	/// <summary>The area never grows past this (+40%).</summary>
	public const float AreaCap = 1.40f;
	/// <summary>One more projectile, strike or tentacle for every this many levels of fuel. 10 in section 4's
	/// draft, which kept a spell that reads an amount (Cinderbreath's tongues) over the target however far the
	/// damage was cut; at 20 the gate's 16 fuel gives none and a full book's 40 gives two.</summary>
	public const int FuelPerAmount = 20;

	/// <summary>"Sovereign Cinderbreath": what the anointed spell is called for the rest of the run.</summary>
	public const string NamePrefix = "Sovereign ";

	/// <summary>
	/// The spells the Rite counts as passive: never silenced, never fuel, never anointed. SpellData.IsPassive
	/// is set on nothing since boons replaced the passive spells, so the doc's "wards, auras, the defensive
	/// spells" is today the one shield: Aegis Ward costs a slot but is not an attack (UnlockCatalog's note).
	/// </summary>
	public static readonly string[] DefensiveSpellIds = { "aegis_ward" };

	/// <summary>
	/// Spells the Rite will not anoint, and why. Each is fed by the other spells' casts or kills, which the
	/// Rite silences, so anointed it would be the whole run's damage gone for a spell that no longer goes off.
	/// </summary>
	private static readonly Dictionary<string, string> RefusedTargets = new(StringComparer.OrdinalIgnoreCase)
	{
		["arcane_explosion"] = "Charged by the casts it would silence",
		["frost_shard"] = "Shatters only what other spells kill",
	};

	/// <summary>True for a spell the Rite leaves alone: a passive, or a defensive spell.</summary>
	public static bool IsPassiveForRite(SpellData spell) =>
		spell != null && (spell.IsPassive || DefensiveSpellIds.Contains(spell.Id, StringComparer.OrdinalIgnoreCase));

	/// <summary>The held spells the Rite counts: every one but the passives.</summary>
	public static IEnumerable<SpellData> ActiveSpells(IEnumerable<SpellData> loadout) =>
		(loadout ?? Enumerable.Empty<SpellData>()).Where(s => s != null && !string.IsNullOrEmpty(s.Id) && !IsPassiveForRite(s));

	/// <summary>The fuel <paramref name="target"/> would have from this loadout: every OTHER active spell's level.</summary>
	public static int FuelFor(SpellData target, IEnumerable<SpellData> loadout) =>
		ActiveSpells(loadout).Where(s => !ReferenceEquals(s, target)).Sum(s => Math.Max(0, s.CurrentLevel));

	/// <summary>Why the Rite will not anoint this spell, or null when it will.</summary>
	public static string RefusalFor(SpellData spell)
	{
		if (spell == null)
			return "No spell";
		if (IsPassiveForRite(spell))
			return "Not an attack";
		return RefusedTargets.TryGetValue(spell.Id, out string why) ? why : null;
	}

	/// <summary>The ids the Rite refuses to anoint, for the validator.</summary>
	public static IEnumerable<string> RefusedTargetIds => RefusedTargets.Keys;

	/// <summary>
	/// What <paramref name="fuel"/> buys <paramref name="spell"/>: damage on every spell; cooldown, area and
	/// amount only on a spell whose script reads them (EvolutionStatReaders), so a card never promises a
	/// number its spell ignores. A spell with no area of its own grows its drawn scale instead, which is its
	/// reach on a bolt; never both, which would compound.
	/// </summary>
	public static SovereignGains GainsFor(SpellData spell, int fuel)
	{
		fuel = Math.Max(0, fuel);
		// A spell with no row in the table (none today: ValidateEvolutionReaders wants one) is promised damage only.
		EvolutionStat stats = EvolutionStatReaders.TryGet(spell, out SpellStatReaders readers) ? readers.Stats : EvolutionStat.Damage;
		float damage = (stats & EvolutionStat.Damage) != 0 ? 1f + DamagePerFuel * fuel : 1f;
		float cooldown = (stats & EvolutionStat.Cooldown) != 0 ? MathF.Max(CooldownFloor, 1f - CooldownPerFuel * fuel) : 1f;
		float grow = MathF.Min(AreaCap, 1f + AreaPerFuel * fuel);
		float area = (stats & EvolutionStat.Area) != 0 ? grow : 1f;
		float scale = (stats & EvolutionStat.Area) == 0 && (stats & EvolutionStat.Scale) != 0 ? grow : 1f;
		int amount = (stats & EvolutionStat.Projectiles) != 0 ? fuel / FuelPerAmount : 0;
		return new SovereignGains(fuel, damage, cooldown, area, scale, amount);
	}

	/// <summary>A fresh boon for <paramref name="spell"/> at <paramref name="fuel"/> (run-only: never in a resource).</summary>
	public static SpellEvolutionOption BuildBoon(SpellData spell, int fuel)
	{
		var boon = new SpellEvolutionOption
		{
			Id = BoonId,
			DisplayName = "Sovereign Rite",
			MilestoneLevel = ComboCatalog.MilestoneLevel,
		};
		Refresh(boon, spell, fuel);
		return boon;
	}

	/// <summary>Writes <paramref name="fuel"/>'s numbers into the boon in place: every reader sees them on its next read.</summary>
	public static void Refresh(SpellEvolutionOption boon, SpellData spell, int fuel)
	{
		if (boon == null)
			return;
		SovereignGains gains = GainsFor(spell, fuel);
		boon.DamageMultiplier = gains.DamageMultiplier;
		boon.CooldownMultiplier = gains.CooldownMultiplier;
		boon.AreaMultiplier = gains.AreaMultiplier;
		boon.ScaleMultiplier = gains.ScaleMultiplier;
		boon.ProjectileCountBonus = gains.AmountBonus;
	}

	/// <summary>"x1.2 damage, -30% cooldown, +40% area, +2 amount": a chooser card's line, the numbers that apply.</summary>
	public static string GainLine(SovereignGains gains)
	{
		var parts = new List<string>();
		if (gains.DamageMultiplier > 1.005f)
			parts.Add($"x{gains.DamageMultiplier:0.0} damage");
		if (gains.CooldownMultiplier < 0.995f)
			parts.Add($"-{(1f - gains.CooldownMultiplier) * 100f:0}% cooldown");
		if (gains.AreaMultiplier > 1.005f)
			parts.Add($"+{(gains.AreaMultiplier - 1f) * 100f:0}% area");
		if (gains.ScaleMultiplier > 1.005f)
			parts.Add($"+{(gains.ScaleMultiplier - 1f) * 100f:0}% size");
		if (gains.AmountBonus > 0)
			parts.Add($"+{gains.AmountBonus} amount");
		return string.Join(", ", parts);
	}

	/// <summary>The anointed spell's name: "Sovereign Cinderbreath" (the legendary star, if any, stays at the end).</summary>
	public static string SovereignName(string name) =>
		string.IsNullOrEmpty(name) || name.StartsWith(NamePrefix, StringComparison.Ordinal) ? name ?? string.Empty : NamePrefix + name;

	/// <summary>
	/// Whether the strip may offer the Rite to this loadout now (section 3): the player level, three active
	/// spells, and at least one of them that the Rite will anoint with <see cref="MinimumFuel"/> behind it.
	/// The save's ownership and "not taken yet" are the caller's (Player.TryBuildSovereignOffer).
	/// </summary>
	public static bool MeetsGate(int playerLevel, IReadOnlyCollection<SpellData> loadout) =>
		playerLevel >= MinimumPlayerLevel
		&& ActiveSpells(loadout).Count() >= MinimumActiveSpells
		&& ActiveSpells(loadout).Any(s => RefusalFor(s) == null && FuelFor(s, loadout) >= MinimumFuel);
}
