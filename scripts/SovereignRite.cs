using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>What the Rite's fuel pours into it: the four numbers, as the boon carries them.</summary>
public readonly record struct SovereignGains(int Fuel, float DamageMultiplier, float CooldownMultiplier,
	float AreaMultiplier, float ScaleMultiplier, int AmountBonus);

/// <summary>
/// The single authority on the Sovereign Rite's numbers and rules (.ai/sovereign-spell.md): its late gate, what
/// counts as fuel, what the fuel buys, and who is never silenced.
/// </summary>
/// <remarks>
/// <para><b>Since 2026-11-08 it is an ordinary two-spell Conjunction</b> (Eric: "the soverign rite should be a
/// combination spell itself - pick two spells that make sense to combine to create this"): Arcane Explosion down
/// Resonance and Gravewell down Mass Grave, made one in the Ascension strip like every recipe (ComboCatalog), its
/// payload a crowned blast that opens a great grave (SovereignRiteCaster). And it "doesn't need an animation to
/// learn. it just unlocks with combination spells": no lesson of its own - a save that has learned Conjunctions
/// is offered it as one more recipe.</para>
///
/// <para>What still makes it the Rite: it is LATE (the strip waits for <see cref="MinimumPlayerLevel"/> and for
/// <see cref="MinimumFuel"/> levels across the other active spells, so it is never a trap), and making it
/// SILENCES every other active spell for the rest of the run (Player.IsSilenced). Their levels - and every level
/// they gain afterwards, on plain "+1 fuel" cards - are its fuel.</para>
///
/// <para>The gain is a milestone the Rite carries (SpellData.SovereignBoon), shared with its two frozen parents,
/// so it reaches every stat reader through the one list they all walk (SpellData.CollectActive) - no reader names
/// the Rite. It carries damage, cooldown, area and amount and nothing else: no effect, no rider, no slow, no
/// knockback. So it cannot open a crowd-control or an execute path on a boss (RegressionChecks.ValidateSovereignRite).</para>
/// </remarks>
public static class SovereignRite
{
	/// <summary>The Rite's own id: its SpellData (SpellData_SovereignRite.tres), its recipe and its unlock entry.</summary>
	public const string SpellId = "sovereign_rite";

	/// <summary>The boon's id, as SpellData.HasEvolution would read it (it never does: the boon is no branch).</summary>
	public const string BoonId = "sovereign_rite_boon";

	// --- the gate (section 3) ------------------------------------------------------------------------------

	/// <summary>The player level the strip waits for: Eric's "can't unlock until late in the game". 25 in the
	/// first draft; 28 since section 6's measurement put a planned build's first offer at minute 10-12.</summary>
	public const int MinimumPlayerLevel = 28;

	/// <summary>
	/// The fuel the fusion must find: the levels of every active spell held besides the two parents, summed. Two
	/// other spells at level 8 is the least that meets it, so the Rite is never taken onto an empty book.
	/// </summary>
	public const int MinimumFuel = 16;

	// --- the power (section 4), per level of fuel ---------------------------------------------------------------

	// Retuned 2026-11-08 for the Rite as its own spell (.ai/sovereign-spell.md section 9): the measured ratio is
	// the Rite alone against the whole loadout it silenced, target about 1.3.
	public const float DamagePerFuel = 0.005f;
	public const float CooldownPerFuel = 0.008f;
	/// <summary>The cooldown never falls below this share of what it was (-30%).</summary>
	public const float CooldownFloor = 0.70f;
	public const float AreaPerFuel = 0.01f;
	/// <summary>The area never grows past this (+40%).</summary>
	public const float AreaCap = 1.40f;
	/// <summary>
	/// One more crowned blast (and its grave) a cast for every this many levels of fuel. 20 in the first build,
	/// where one more was a tongue or a bolt; for the Rite one more is a whole second blast and grave, which at 20
	/// doubled it within a minute of the fusion (fuel climbs four levels in that minute on a late book). At 40 only
	/// a full book - five other spells at level 8 - lets a second fall (section 9.5).
	/// </summary>
	public const int FuelPerAmount = 40;

	/// <summary>
	/// The spells the Rite counts as passive: never silenced, never fuel. SpellData.IsPassive is set on nothing
	/// since boons replaced the passive spells, so the doc's "wards, auras, the defensive spells" is today the
	/// one shield: Aegis Ward costs a slot but is not an attack (UnlockCatalog's note).
	/// </summary>
	public static readonly string[] DefensiveSpellIds = { "aegis_ward" };

	/// <summary>True for a spell the Rite leaves alone: a passive, or a defensive spell.</summary>
	public static bool IsPassiveForRite(SpellData spell) =>
		spell != null && (spell.IsPassive || DefensiveSpellIds.Contains(spell.Id, StringComparer.OrdinalIgnoreCase));

	/// <summary>The held spells the Rite counts: every one but the passives.</summary>
	public static IEnumerable<SpellData> ActiveSpells(IEnumerable<SpellData> loadout) =>
		(loadout ?? Enumerable.Empty<SpellData>()).Where(s => s != null && !string.IsNullOrEmpty(s.Id) && !IsPassiveForRite(s));

	/// <summary>
	/// The fuel one held spell is worth: its level - or, for a Conjunction made by fusing, the levels of the two
	/// frozen parents it carries (sixteen), since a fusion is two spells' levels in one slot and counting it as one
	/// level would make every earlier Conjunction a trap. A Conjunction a wizard opened on (SpellData.ReadsOwnTable)
	/// has its own level table, and its level is its fuel.
	/// </summary>
	public static int FuelOf(SpellData spell)
	{
		if (spell == null)
			return 0;
		if (spell.ComboParents is { Length: > 0 } parents)
			return parents.Sum(p => Math.Max(0, p?.CurrentLevel ?? 0));
		return Math.Max(0, spell.CurrentLevel);
	}

	/// <summary>
	/// The fuel the loadout pours into the Rite: every active spell's worth (<see cref="FuelOf"/>) but those
	/// named in <paramref name="besides"/> - the Rite itself once made, or its two parents before the fusion.
	/// </summary>
	public static int FuelFrom(IEnumerable<SpellData> loadout, params SpellData[] besides) =>
		ActiveSpells(loadout).Where(s => !besides.Any(b => ReferenceEquals(b, s))).Sum(FuelOf);

	/// <summary>Whether the strip may offer the Rite now: the player's level, and the fuel the fusion would find.</summary>
	public static bool MeetsGate(int playerLevel, int fuel) =>
		playerLevel >= MinimumPlayerLevel && fuel >= MinimumFuel;

	/// <summary>
	/// What <paramref name="fuel"/> buys the Rite: damage, cooldown, area and amount, each only where its script
	/// reads it (EvolutionStatReaders' row), so a number is never promised that nothing reads.
	/// </summary>
	public static SovereignGains GainsFor(SpellData spell, int fuel)
	{
		fuel = Math.Max(0, fuel);
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

	/// <summary>"x1.2 damage, -30% cooldown, +40% area, +2 amount": what a fuel buys, the numbers that apply.</summary>
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
}
