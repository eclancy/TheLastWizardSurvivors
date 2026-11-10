using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

/// <summary>A SpellEvolutionOption field, or a group of them that one reader always takes together.</summary>
[Flags]
public enum EvolutionStat
{
	None = 0,
	Damage = 1 << 0,        // DamageBonus, DamageMultiplier
	Cooldown = 1 << 1,      // CooldownBonus, CooldownMultiplier
	Projectiles = 1 << 2,   // ProjectileCountBonus
	Range = 1 << 3,         // RangeBonus
	Area = 1 << 4,          // AreaMultiplier
	Pierce = 1 << 5,        // PierceBonus
	ChainArcs = 1 << 6,     // ChainArcBonus
	ChainBranches = 1 << 7, // ChainBranchBonus
	Knockback = 1 << 8,     // KnockbackBonus
	Slow = 1 << 9,          // SlowMagnitudeBonus
	PoisonTick = 1 << 10,   // PoisonTickBonus
	Speed = 1 << 11,        // SpeedMultiplier
	Scale = 1 << 12,        // ScaleMultiplier
	Rider = 1 << 13,        // Rider, RiderMagnitude, RiderSeconds
	// Effect and EffectValue. Which effects a spell reads is a list of its own (SpellStatReaders.Effects);
	// this flag only names the field in a report and in KnownUnbuilt.
	Effect = 1 << 14,
	// Not a card's field: the player-wide Amount - the Split Lens relic's +1 (the shop's Twinstar Sigil too, until
	// in-flight 69 removed it) - added to the spell's own count through Player.AmountFor. It decides both halves of the lens's
	// trade (0.9.3, Eric: "it should only reduce damage of spells that are affected by the projectile
	// count"): AmountFor pays the +1 only to a spell whose row has it, and GetSpellDamageMultiplier charges
	// the -25% only to the same spells (SpellStatReaders.ReadsAmount).
	Amount = 1 << 15,
}

/// <summary>What one spell's script reads of the options its milestones grant, and where.</summary>
public sealed class SpellStatReaders
{
	private readonly SpellEffect[] effects;

	public SpellStatReaders(EvolutionStat stats, string readIn, SpellEffect[] effects)
	{
		Stats = stats;
		ReadIn = readIn;
		this.effects = effects ?? Array.Empty<SpellEffect>();
	}

	public EvolutionStat Stats { get; }
	public string ReadIn { get; }
	public IReadOnlyList<SpellEffect> Effects => effects;

	public bool ReadsEffect(SpellEffect effect) => Array.IndexOf(effects, effect) >= 0;

	private string[] amountDroppedBy = Array.Empty<string>();

	/// <summary>Milestones under which the spell no longer adds Amount to anything (Gravewell's Mass Grave).</summary>
	public IReadOnlyList<string> AmountDroppedBy => amountDroppedBy;

	/// <summary>Names the milestones whose cast path stops reading Amount. Used once, in the table below.</summary>
	public SpellStatReaders ExceptAmountUnder(params string[] evolutionIds)
	{
		amountDroppedBy = evolutionIds ?? Array.Empty<string>();
		return this;
	}

	/// <summary>
	/// Whether this spell, holding the milestones it holds now, adds the player-wide Amount to its count:
	/// its row has EvolutionStat.Amount and no milestone it holds drops it.
	/// </summary>
	public bool ReadsAmount(SpellData spell)
	{
		if ((Stats & EvolutionStat.Amount) == 0)
			return false;
		foreach (string id in amountDroppedBy)
		{
			if (spell != null && spell.HasEvolution(id))
				return false;
		}
		return true;
	}
}

/// <summary>A number a card promises that its spell never built, kept visible until it is.</summary>
public readonly record struct UnbuiltPromise(string OptionId, EvolutionStat Stat, SpellEffect Effect, string Promise);

/// <summary>
/// Which SpellEvolutionOption fields each spell's script actually reads, so that every number on a
/// level 4, level 8 or Apotheosis card is one that reaches the spell (RegressionChecks.ValidateEvolutionReaders).
/// </summary>
/// <remarks>
/// Why it exists: thirteen spells shared one set of default evolutions written for a bolt, and a
/// trap, a wall, a plague, a ward and a flamethrower all offered "+25% projectile speed" and "pierce 2
/// more enemies" that nothing they ran ever read. Nothing could see it - a field nobody reads is a
/// field set, which compiles and loads - so it took a player to ask "isn't it an aoe? does that even
/// make sense?" (2026-10-24). A table cannot read C#, so this one is written by hand from the scripts,
/// and each entry says where its reads are. **A new spell needs an entry here** (the validator says so
/// until it has one), and a spell whose script starts or stops reading a field updates its row.
///
/// What "reads" means, field by field:
///   - Damage, Cooldown: the spell's damage and the interval its cooldown decides. Frost Shard casts
///     nothing on its cooldown and Arcane Explosion is charge-driven, so neither reads Cooldown.
///   - Area: SpellData.GetAreaMultiplierAtLevel reaches what is hit. The player's own Area is not this.
///   - Speed: SpellData.GetSpeedMultiplier reaches the spell, as whatever "faster" is for it.
///   - Scale: an evolution's ScaleMultiplier reaches what the player sees, because the spell's node
///     keeps the scale Player.ApplyLegendaryVisual gives it. A spell that draws its own hit radius drops
///     the scale (Bramble Seed's lesson), and a persistent one is only ever scaled once, when it is made
///     at level 1, before any evolution exists - neither reads Scale.
///   - Rider: the spell's hits go through Player.DealDamageToEnemy with itself as the source, which is
///     where an element path's rider (and a per-spell CritDamage) applies.
///   - Effects: the SpellEffect values the spell asks its SpellData for (GetEffectValueAtLevel,
///     HasEffectFlag), plus CritDamage for every spell that passes its source.
///   - Amount: the spell's cast adds the player-wide Amount to its count, through Player.AmountFor and
///     nowhere else. No card sets it; it is what the Split Lens relic's -25% damage is charged on, so a
///     spell the lens cannot give a projectile is never made to pay for one. AmountFor gives a spell
///     without it nothing and reports it, so a cast path that starts reading Amount shows up the first
///     time it fires; a path that stops reading it takes the flag off its row (or, for one milestone,
///     names that milestone in ExceptAmountUnder). RegressionChecks.ValidateAmountReaders checks the rest.
/// </remarks>
public static class EvolutionStatReaders
{
	private const EvolutionStat Core = EvolutionStat.Damage | EvolutionStat.Cooldown;

	// ElementalBolt's reads (RefreshComputedStats, ResolveImpact), for the five spells that fly on it.
	// Knockback since 2026-10-29: the shove along the heading Riptide's Breaker and Hunter's Heavy Draw
	// give (SpellData.GetKnockbackSpeedAtLevel, scaled by the bolt's KnockbackScale).
	private const EvolutionStat Bolt = Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Pierce
		| EvolutionStat.Speed | EvolutionStat.Scale | EvolutionStat.Knockback | EvolutionStat.Rider;
	private static readonly SpellEffect[] BoltEffects =
	{
		SpellEffect.CritChance, SpellEffect.ProjectileSpeed, SpellEffect.Pierce, SpellEffect.AreaSize,
		SpellEffect.SlowPower, SpellEffect.SlowDuration, SpellEffect.DotDamage, SpellEffect.ZoneDuration,
		SpellEffect.CritDamage,
	};

	// ChainLightning.cs, which Shadow Bolt's scene runs too.
	// Area is the jump (ChainLightning.SpawnBranches), since 2026-10-26. Amount deepens the arc
	// (ChainLightning.ProjectileCountBonus, handed over by Player.FireChainLightningSpell and FireBoltSpell).
	private const EvolutionStat Chain = Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.ChainArcs
		| EvolutionStat.ChainBranches | EvolutionStat.PoisonTick | EvolutionStat.Scale | EvolutionStat.Rider | EvolutionStat.Amount;
	private static readonly SpellEffect[] ChainEffects =
	{
		SpellEffect.CritChance, SpellEffect.SlowPower, SpellEffect.Vulnerability, SpellEffect.Chain, SpellEffect.DotDamage,
		SpellEffect.CritDamage,
	};

	private static SpellStatReaders R(EvolutionStat stats, string readIn, params SpellEffect[] effects) => new(stats, readIn, effects);

	private static readonly Dictionary<string, SpellStatReaders> BySpell = new(StringComparer.OrdinalIgnoreCase)
	{
		["magic_missile"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Pierce
				| EvolutionStat.Scale | EvolutionStat.Rider | EvolutionStat.Amount,
			"MagicMissile.RefreshComputedStats and the missile loop in Player.CastSpellNow (Amount is more missiles; Area also sizes the Arcane Orb and the Overcharge burst; Threaded Needle, Overcharge and Arcane Orb by id, through StaticCharges; The Archmage's Not One Missed by id, the count in that loop)",
			SpellEffect.ProjectileSpeed, SpellEffect.AreaSize, SpellEffect.ExplosionOnHit, SpellEffect.SplitOnHit,
			SpellEffect.Pierce, SpellEffect.CritChance, SpellEffect.CritDamage),
		["arcane_explosion"] = R(EvolutionStat.Damage | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Knockback | EvolutionStat.Rider,
			"ArcaneExplosion.RefreshComputedStats, TriggerExplosion and OpenLingeringRift; charge-driven and persistent, so no cooldown and no visual scale (the Canticle's cards by id: Harmony's element from the next slot, Resonance, and Fermata's stasis)",
			SpellEffect.VortexPull, SpellEffect.Knockback, SpellEffect.Freeze, SpellEffect.ZoneDuration, SpellEffect.AreaSize, SpellEffect.CritDamage),
		// Spiritual Weapon is read by FORM (SpellData.Variants): one script, but what reaches a blow differs by
		// weapon, so each form has its own row and TryGet(SpellData) takes the form's before the spell's. The
		// spell's own row is the template's, which no run plays. Scale is the colossal Ascension: it is the
		// weapon's drawn size and the reach of its blow alike (SpiritualWeapon.Arm), never a node scale.
		["spiritual_weapon"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Scale
				| EvolutionStat.Rider,
			"SpiritualWeapon.Arm (the template; every run plays a form, below)",
			SpellEffect.AreaSize, SpellEffect.CritDamage),
		["spiritual_weapon_axe"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Scale
				| EvolutionStat.Knockback | EvolutionStat.Rider,
			"SpiritualWeapon.Arm and AxeBlow (Area and Scale are the cleave; Knockback adds to the axe's own throw; Headsman and Whirling Axes by id)",
			SpellEffect.AreaSize, SpellEffect.CritDamage),
		["spiritual_weapon_hammer"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Scale
				| EvolutionStat.Slow | EvolutionStat.Rider,
			"SpiritualWeapon.Arm, HammerSlam and TickZones (Area and Scale are the slam; Slow on its slow; ZoneDuration is Hallowed Ground's; Aftershock and Mjolnir's lightning, CallThunder, by id)",
			SpellEffect.AreaSize, SpellEffect.SlowPower, SpellEffect.SlowDuration, SpellEffect.Freeze, SpellEffect.ZoneDuration, SpellEffect.CritDamage),
		["spiritual_weapon_sword"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Scale | EvolutionStat.Rider,
			"SpiritualWeapon.Arm, SwordBlow, ParryBolts and Siphon (Scale is the greatsword's blade and guard; Deflection's interceptions, Twin Blades' dagger, Thousand Cuts and the greatsword's stroke by id)",
			SpellEffect.CritChance, SpellEffect.Lifesteal, SpellEffect.CritDamage),
		["fireball"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Speed
				| EvolutionStat.Scale | EvolutionStat.Rider,
			"Fireball.RefreshComputedStats, LeaveMagmaPool and ScatterClusterBombs (one ball; projectiles past the first are bomblets; Brimstone and Armageddon Sun's plough by id, through BrimstoneLayer and Fireball.Sear)",
			SpellEffect.AreaSize, SpellEffect.Burn, SpellEffect.CritDamage),
		["frost_shard"] = R(EvolutionStat.Damage | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Slow
				| EvolutionStat.Rider,
			"Player.OnEnemyDiedAt and ShatterAt: a shatter on every death, so it casts nothing on its cooldown (projectiles past the first are Ice Flurry's flung shards; Ice Statues, Avalanche and Meltwater by id, through FrostShatter)",
			SpellEffect.Freeze, SpellEffect.AreaSize, SpellEffect.SlowPower, SpellEffect.SlowDuration, SpellEffect.CritDamage),
		["chain_lightning"] = R(Chain, "ChainLightning.cs (Storm Reach's long leaps, Return Stroke, Cascade's kills and Electrocution by id, through Electrocutions)", ChainEffects),
		// Shadow Bolt, the third combination (ComboCatalog) since 2026-11-07: terminal, so no card reaches it.
		// The bolt's leaps, the festering curse and the beat run in ShadowBolt.cs on the two frozen parents it
		// carries (SpellData.ComboParents), every hit credited to the combination.
		["shadow_bolt"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area
				| EvolutionStat.Slow | EvolutionStat.Rider | EvolutionStat.Amount,
			"ShadowBolt.Cast, CaveInAll and Freeze, through Player.FireShadowBolt: the bolt's hit and the cave-ins are shares of the frozen Void Lance's strike, the curse's poison, reach and length the frozen Contagion's, the beat Pandemic's (Amount is more leaps a cast)",
			SpellEffect.AreaSize, SpellEffect.ZoneDuration, SpellEffect.DotDamage, SpellEffect.CritDamage),
		// The Sovereign Rite, the capstone combination (ComboCatalog) since 2026-11-08: terminal, so no card reaches it;
		// its fuel does, as a milestone of its own (SpellData.SovereignBoon) shared with its frozen parents. The blast,
		// the grave and the echo run in SovereignRiteCaster.cs on the two frozen parents it carries, every hit
		// credited to the combination. No Scale: the blast and the grave draw themselves out of the radius they hit.
		["sovereign_rite"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Speed
				| EvolutionStat.Rider | EvolutionStat.Amount,
			"SovereignRiteCaster.Cast, OpenGrave and Freeze, through Player.FireSovereignRite: the blast's hit and reach are the frozen Arcane Explosion's, the grave is a GravewellTrap on the frozen Gravewell's numbers (Mass Grave's width, hold and drain; its Speed how fast it opens), the cooldown and the reach the frozen Gravewell's; Amount (the fuel's +1 per 20, and the player's) is more blasts a cast",
			SpellEffect.AreaSize, SpellEffect.RootDuration, SpellEffect.CritDamage),
		// The four bolts' own cards (BoltEvolutions, 2026-10-29) are behaviours read by id; their numbers
		// ride on the fields below.
		["gale_blade"] = R(Bolt, "ElementalBolt (Returning; Eddy, Hooked Throw, Live Wire, Thunderhead and Squall by id)", BoltEffects),
		["riptide"] = R(Bolt, "ElementalBolt, RiptideWave and Player.FireBoltSpell (Knockback is Breaker's throw out and Undertow's drag in, ElementalBolt.DragTowardCaster; Groundswell, Undertow and Trident by id)", BoltEffects),
		["molten_shard"] = R(Bolt, "ElementalBolt, MoltenSlag and Player.FireBoltSpell (projectiles are Forge Swarm's forged shards; Slag Trail, Splitting Shard, Magma Core and Caldera by id)", BoltEffects),
		// The one bolt that reads Amount (more arrows a volley); the other three never have.
		["hunters_draw"] = R(Bolt | EvolutionStat.Amount, "ElementalBolt, ArrowRain and Player.LooseHuntersVolley (Amount is more arrows a volley; Knockback is Heavy Draw's, on a full draw; Running Shot, Barbed, Rain of Arrows and Hunter's Mark by id)", BoltEffects),
		["aegis_ward"] = R(Core,
			"the aegis_ward branch of Player._PhysicsProcess: damage is the shield, cooldown the recharge; Player.CrystalShellBurst on a break"),
		// The defensive returns (in-flight item 9). Stormguard: the clap's damage and reach, the
		// gathering's cooldown, the shove and the rain's slow. Gloam Veil reads NO cooldown - its
		// cadence is the walk (Range is WeaveDistance, levelled DOWN) - so no auto cooldown line is
		// generated for it (Player.EnsureRelevantLevelUps) and no card may promise one.
		[Stormguard.SpellId] = R(Core | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Knockback | EvolutionStat.Slow,
			"Stormguard.Break and Clap: damage and Range x Area are the thunderclap, cooldown the gathering, knockback its shove, SlowMagnitude the rain; Charged Air, Downpour's longer rain, Eye of the Storm and Rolling Thunder by id"),
		[GloamVeil.SpellId] = R(EvolutionStat.Damage | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Knockback,
			"GloamVeil.Tear and WeaveDistance: damage is the tear's lash, Range the walk that re-weaves (negative bonuses shorten it), Area the gust's reach, knockback its shove; Night's Cloth, Tailwind, Second Skin and Hour of Gloam by id"),
		["void_lance"] = R(Core | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Scale,
			"VoidLance.RefreshComputedStats and CollapseAtTerminus (hit at its drawn scale; Area is the collapse; its hits pass their source since 2026-10-28, but no card of its carries a rider; Abyssal Scar, Void Glaive, Dread and Witching Hour by id, through VoidScars and Player.FireVoidLanceSpell)",
			SpellEffect.ZoneDuration),
		["cyclone_slash"] = R(EvolutionStat.Damage | EvolutionStat.Projectiles | EvolutionStat.Area | EvolutionStat.Speed | EvolutionStat.Rider
				| EvolutionStat.Amount,
			"OrbitingBlade.RefreshComputedStats and PullTowardRing (Amount is more blades; the gust then scales the orbit it computes); persistent, so its cooldown only refreshes it and it is never re-scaled; Twin Gyres' inner ring, Wind Shear's crescents and Typhoon Core by id",
			SpellEffect.VortexPull, SpellEffect.AreaSize, SpellEffect.CritDamage),
		["solar_flare"] = R(Core | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Knockback | EvolutionStat.Rider,
			"ElementalPulse.RefreshComputedStats and TriggerPulse (Area is the flare's reach, Knockback Blinding Flash's shove; Sun Dogs, Solar Prominence, Daybreak's ward, Corona's beams and Dragon's Teeth's stone by id)",
			SpellEffect.AreaSize, SpellEffect.CritDamage),
		["black_tentacles"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Rider
				| EvolutionStat.Amount,
			"BlackTentacles.cs (Amount is more tentacles, its TentacleBonus; the rift is drawn at the radius it slows, so no visual scale; Crushing Grip's coils and Fling by id, Soul Drain's wisps on its Lifesteal)",
			SpellEffect.AreaSize, SpellEffect.ZoneDuration, SpellEffect.SlowPower, SpellEffect.SlowDuration,
			SpellEffect.RootDuration, SpellEffect.Lifesteal, SpellEffect.VortexPull, SpellEffect.CritDamage),
		["thorn_vine"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Pierce
				| EvolutionStat.Speed | EvolutionStat.Rider,
			"ThornVine.cs and Player.FireThornVine (drawn out of its reach, so no visual scale; Barbed Lash, Snapback, Entangle, Thicket and Hydra Vine by id, through ThornBarbs and ThornThicket)",
			SpellEffect.AreaSize, SpellEffect.DotDamage, SpellEffect.ZoneDuration, SpellEffect.Pierce, SpellEffect.CritDamage),
		["cone_of_cold"] = R(Core | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Knockback | EvolutionStat.Rider,
			"ConeBlast.cs and Player.FireConeBlast (which drops the visual scale; Arctic Blast's half-moon and Zero Point's relayed second cone by id)",
			SpellEffect.SlowPower, SpellEffect.SlowDuration, SpellEffect.Freeze, SpellEffect.AreaSize, SpellEffect.CritDamage),
		["obsidian_spike"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Rider
				| EvolutionStat.Amount,
			"GroundSpike.cs and Player.FireGroundSpike (Amount is more spikes; the cracks, chips and splinters are drawn out of the radius it hits, so no visual scale; Splinter Field, Deep Strata's throw, Tectonic Uprising's vents and Basalt Lance by id)",
			SpellEffect.AreaSize, SpellEffect.CritDamage),
		["meteor_swarm"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Knockback | EvolutionStat.Rider
				| EvolutionStat.Amount,
			"MeteorImpact.cs and Player.FireMeteorSwarm (the volley is the projectile count plus Amount; drops the visual scale; Breakup, Skipstone, Extinction Event's finale and Creeping Barrage by id)",
			SpellEffect.Burn, SpellEffect.AreaSize, SpellEffect.CritDamage),
		["starfall"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Rider
				| EvolutionStat.Amount,
			"Starfall.Cloudburst and the fire loop (the stars a cloudburst lets fall are the projectile count plus Amount; Area sizes each star and a shooting star's line, never the rain; Shooting Stars, Skipping Stars, Starburst and Constellation by id)",
			SpellEffect.AreaSize, SpellEffect.CritDamage),
		// The Familiar is read by FORM too (the dog, the crow, the cat): one script, but what reaches a strike
		// differs by creature. The spell's own row is the template's, which no run plays.
		["familiar"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Rider,
			"Familiar.Summon and its strikes (the template; every run plays a form, below)",
			SpellEffect.CritDamage),
		["familiar_dog"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Rider,
			"Familiar.Summon, Bite and Strike (the hounds are the projectile count; Area sizes the jaws and a Cerberus's reach, never the hunt; Cerberus and the Hound of Annwn by id)",
			SpellEffect.CritDamage),
		["familiar_crow"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Rider,
			"Familiar.Summon, StartStoop, Bite and Strike (the crows are the projectile count; Area sizes the beak and a griffin's swath; the Morrigan and the Griffin by id)",
			SpellEffect.CritDamage),
		// The cat deals no damage (2026-10-31): its level lines are its pounce rate (cooldown), how far from him
		// it pounces (range) and what a visit grants him (Familiar.GrantsOf through LiveGrants and Player: Cast
		// speed and Evasion). Area sizes the pounce (Cath Palug's reach). Grimalkin, Purr, Nine Lives, Cath
		// Palug's grants and the Black Cat are read by id; the Cait Sith's rider is Familiar.Mark's Vulnerable.
		["familiar_cat"] = R(EvolutionStat.Cooldown | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Rider,
			"Familiar.Summon, CatPounce and Mark (Area sizes the pounce; the rider is Mark's Vulnerable), and Familiar.LiveGrants through Player (cast speed, evasion)",
			SpellEffect.CastSpeed, SpellEffect.Evasion),
		// The facing family (.ai/spell-roster.md). Neither reads Scale: the arc is drawn out of the band
		// it sweeps, and the rune sheet is drawn at one size.
		["stormsweep"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Speed
				| EvolutionStat.Rider | EvolutionStat.Amount,
			"Stormsweep.Cast and the fire loop (sweeps are the projectile count plus Amount; Range is the arc's length, Area its length and band and a ball's reach, Speed its tempo; Ball Lightning, Forked Lightning, Thunderclap and Cardinal Storm by id)",
			SpellEffect.AreaSize, SpellEffect.CritDamage),
		["sleet_runes"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Pierce | EvolutionStat.Speed
				| EvolutionStat.Rider | EvolutionStat.Amount,
			"SleetRunes.Cast and the fire loop (runes are the projectile count plus Amount; Range is how far one flies, Speed scales its speed; Great Rune, Braided Wind, Parting Wind, Sigil Fan, Turning Gale and Fata Morgana by id)",
			SpellEffect.ProjectileSpeed, SpellEffect.Pierce, SpellEffect.CritDamage),
		["toxic_spore_burst"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Speed
				| EvolutionStat.PoisonTick | EvolutionStat.Rider | EvolutionStat.Amount,
			"SporeMushroom.For and Player.FireSporeMushrooms (Amount is more mushrooms; drops the visual scale)",
			SpellEffect.AreaSize, SpellEffect.DotDamage, SpellEffect.CritDamage),

		// The thirteen that left the default archetype (SpellEvolutionCatalog, "The default archetype,
		// spell by spell"). None reads Scale: each draws itself out of the radius it hits.
		// Amount is more graves a cast - except under Mass Grave, which digs one great grave whatever the count.
		["gravewell"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Speed
				| EvolutionStat.Rider | EvolutionStat.Amount,
			"GravewellTrap.For and Player.FireGravewellTrap (Amount is more graves, but not under Mass Grave; Bone Spikes, Quickset, Grave Chill, Mass Grave and Grave Line by id, through ThrownShards and GravePath)",
			SpellEffect.AreaSize, SpellEffect.RootDuration, SpellEffect.CritDamage).ExceptAmountUnder(GravewellTrap.MassGraveId),
		["hollow_star"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Speed
				| EvolutionStat.Rider,
			"Player.FireHollowStar and OpenHollowStar, HollowStar.Collapse (Projectiles are wells - Twin Stars' pair; Crush, Twin Stars' drift, Black Sun and Nova Seed by id in HollowStar)",
			SpellEffect.AreaSize, SpellEffect.ZoneDuration, SpellEffect.RootDuration, SpellEffect.CritDamage),
		["bramble_seed"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Area | EvolutionStat.Speed | EvolutionStat.Slow
				| EvolutionStat.Rider,
			"Player.FireBrambleSeed and BrambleSeed.CapFor (planted at the feet, so no range; Projectiles are patches that may stand; Burrs, Runners, the Keep's hedge and tending, and Thornstorm by id in BrambleSeed)",
			SpellEffect.AreaSize, SpellEffect.ZoneDuration, SpellEffect.SlowPower, SpellEffect.SlowDuration, SpellEffect.CritDamage),
		["contagion"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Pierce
				| EvolutionStat.Speed | EvolutionStat.Rider,
			"Player.FireContagion and SpreadContagion, Contagion.Infect (pierce is its MaxInfected; Fever Burst, Outbreak, Pandemic and Plague Wind by id in Contagion)",
			SpellEffect.AreaSize, SpellEffect.ZoneDuration, SpellEffect.Pierce, SpellEffect.DotDamage, SpellEffect.CritDamage),
		["iron_palisade"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Speed
				| EvolutionStat.Rider,
			"Player.FireIronPalisade and RaiseIronPalisade (Projectiles are walls; Spiked Rail, Caltrops, Iron Ring and Rolling Wall by id in IronPalisade)",
			SpellEffect.AreaSize, SpellEffect.ZoneDuration, SpellEffect.CritDamage),
		["kindled_ward"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Area | EvolutionStat.Speed | EvolutionStat.Rider
				| EvolutionStat.Amount,
			"Player.FireKindledWard (Amount raises how many wards may stand; it has no reach to cast within; Flare, Wisp Trail and Kindling Flock by id, in KindledWard and WardWisps)",
			SpellEffect.ZoneDuration, SpellEffect.AreaSize, SpellEffect.CritDamage),
		// A Conjunction he opens on and levels (ComboCatalog.StartsFusedFor): its level lines and his Apotheosis
		// are all that set anything; no milestone card does.
		["the_taking"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area
				| EvolutionStat.Rider | EvolutionStat.Amount,
			"Player.FireTheTaking (Projectiles and Amount are hands a cast, Range the reach a quarry is found in, Area the lanes and the burn; the star and the great hand by level, All He Took by id, in TheTaking and TakingLanes)",
			SpellEffect.AreaSize, SpellEffect.CritDamage),
		// Merlin's own (.ai/merlin.md): what Player.FireOrbOfAges reads into the ward. Projectiles and Amount are orbs,
		// Area the ring, Speed the orb; the cards' behaviour by id in OrbOfAges.
		["orb_of_ages"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Area | EvolutionStat.Speed | EvolutionStat.Amount,
			"Player.FireOrbOfAges (Projectiles and Amount are orbs, Area the ring, Speed the orb, ZoneDuration the ward's time; Burning Rim, Missile Ward, Walking Ward and The Once and Future by id, in OrbOfAges)",
			SpellEffect.AreaSize, SpellEffect.ZoneDuration, SpellEffect.CritDamage),
		// John Dee's opener (.ai/character-fit.md section 6): Folio.Refresh reads the turn's numbers, Player.FireFolio the
		// count. Projectiles and Amount are pages a timed turn throws (an orb's turn is always one); Range is the reach a
		// page finds its quarry in; Pierce is Gilt Edge's, the only pierce it has; Speed is the page's flight. No Area and
		// no Scale: no card sets either (the ink and the mirror grow with the wizard's own Area), and the pages are drawn
		// at one size. Torn Leaf, Inkblot, Gilt Edge's gold, Bound Volume, Commonplace Book and the Spirit Mirror by id.
		[Folio.SpellId] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Pierce | EvolutionStat.Speed
				| EvolutionStat.Rider | EvolutionStat.Amount,
			"Folio.Refresh, Folio.Strike and Player.FireFolio (pages a timed turn throws plus Amount; Range is the reach a page finds its quarry in; Torn Leaf, Inkblot, Gilt Edge, Bound Volume, Commonplace Book and the Spirit Mirror by id)",
			SpellEffect.CritDamage),
		// St Elmo's fire (scripts/Corposant.cs): Corposant.Refresh reads the cast's numbers, Player.FireCorposant the count.
		// Projectiles and Amount are the balls a cast looses; ZoneDuration is a ball's life (Corposant.LifeAt); every strike
		// and wake zap passes the spell as its source, so Spindrift's slow rides it. No Range (the view's edges are its
		// range), no Area or Scale on a card (the ball grows with the wizard's own Area), no Speed (no card sets one).
		[Corposant.SpellId] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Rider | EvolutionStat.Amount,
			"Corposant.Refresh and Player.FireCorposant (balls a cast looses plus Amount; ZoneDuration is a ball's life; Twin Fires, Static Wake, Spindrift's repaint, Storm Lantern and Saint's Crown by id)",
			SpellEffect.ZoneDuration, SpellEffect.CritDamage),
		// Ma Liang's brush, walked (scripts/InkRing.cs): InkRing.Refresh reads the ring's damage and the ink's life, the fire
		// loop its cooldown (the recharge between rings). ZoneDuration is how long the ink lasts (InkRing.InkSecondsAt), and
		// so how wide a ring can be walked. Every strike and sting passes the spell as its source, so a per-spell crit reaches
		// it. No Range, Area, Speed, Scale or count: the wizard's walk is all of those.
		[InkRing.SpellId] = R(Core,
			"InkRing.Refresh, InkRing.Strike and the fire loop's recharge (ZoneDuration is the ink's life; Long Stroke, Wet Ink, Inkwell, Ink Prison and Second Stroke by id)",
			SpellEffect.ZoneDuration, SpellEffect.CritDamage),
		// The iron-fruited orchard (scripts/Windfall.cs): Windfall.Refresh reads a fruit's damage, the crop (Crop lines,
		// Windfall.CropAt) and the thud's shove (KnockbackBonus over BaseKnockback, though no card sets one), Player.FireWindfall
		// the tree cap - Projectiles and Amount, Orchard's +1 among them - and the fire loop the planting's cooldown. Every
		// thud, thorn and lash passes the spell as its source, so a rider and a per-spell crit reach it. No Range, Area, Speed
		// or Scale on a card: the ring and the thud grow with the wizard's own Area, which is not a card's.
		[Windfall.SpellId] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Knockback | EvolutionStat.Rider | EvolutionStat.Amount,
			"Windfall.Refresh and Player.FireWindfall (trees standing at once plus Amount; Crop is a tree's fruit; Orchard by its count, Bramble Fall, Cider Rot, Harvest Moon and Iron Bough by id)",
			SpellEffect.Crop, SpellEffect.CritDamage),
		// The surgeon's needle (scripts/Lancet.cs): Lancet.Refresh reads a needle's damage and the reach it chooses in
		// (GetReachAtLevel, Range), Player.FireLancet the needles a cast throws - Projectiles and Amount, Twin Lancets' +1 among
		// them - and the fire loop the throw's cooldown, Bloodletting's and Bodkin's 12% with it. Every strike passes the spell
		// as its source, so Bodkin's Wither and a per-spell crit reach it. No Area, Speed or Scale on a card: a needle is drawn
		// at its own size and flies at its own speed.
		[Lancet.SpellId] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Rider | EvolutionStat.Amount,
			"Lancet.Refresh, Lancet.Cast and Player.FireLancet (needles a cast throws plus Amount; Range is the reach it chooses in; Bloodletting, Triage and Surgeon's Hand by id, Bodkin's Wither through the house path)",
			SpellEffect.CritDamage),
		["glacial_spike"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Speed
				| EvolutionStat.Rider | EvolutionStat.Amount,
			"IceSpike.RefreshComputedStats and Player.FireIceSpikes (Amount is more spikes; Shatter, Spike Run, Rimefoot, Glacier Crown and Frozen Spire by id, through ThrownShards, FrostPatch and GlacierCrown)",
			SpellEffect.AreaSize, SpellEffect.RootDuration, SpellEffect.CritDamage),
		["scorching_ray"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Pierce
				| EvolutionStat.Speed | EvolutionStat.Rider | EvolutionStat.Amount,
			"ScorchingRayBeam.RefreshComputedStats and Player.FireScorchingRay (Amount is more rays; Area is the burst and the Eye of Ra's flare; Flashpoint, Handoff, Burn Through, Prism, Sunlance and the Eye of Ra by id)",
			SpellEffect.CritChance, SpellEffect.Pierce, SpellEffect.CritDamage),
		["cinderbreath"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Speed
				| EvolutionStat.Rider,
			"Flamethrower.RefreshComputedStats and FlamePuff (area is the width of a tongue, never its reach; Backdraft, Ember Wake and Dragon's Breath's reach and wall by id, through CinderGround)",
			SpellEffect.AreaSize, SpellEffect.ZoneDuration, SpellEffect.CritDamage),
		// The combinations (ComboCatalog). Terminal - no level-ups and no milestones of their own - so no card
		// reaches them; the row says what the script reads of the spell itself and of the frozen parents it
		// carries (SpellData.ComboParents), whose own milestones ride on the rain and the arrows.
		["sagittarius"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Range | EvolutionStat.Area | EvolutionStat.Pierce
				| EvolutionStat.Speed | EvolutionStat.Knockback | EvolutionStat.Rider | EvolutionStat.Amount,
			"Sagittarius.Cloudburst, Loose and Freeze, through Player.FireSagittarius: the rain is a Starfall on the frozen Starfall (Amount is more stars, so more archers), the arrows are Hunter's Draw's on the frozen Hunter's Draw, every hit credited to the combination. Artemis's copy (the table path, SpellData.ReadsOwnTable) rains on its own level table - Damage a star's hit and the arrows' (TableArrowShare), Cooldown the cloudburst, Projectiles the stars, Range the rain's width, AreaSize each burst - and the Hunter's Moon's Damage reaches both; its behaviour by id (HuntersMoonId) in Sagittarius.Hunt",
			SpellEffect.AreaSize, SpellEffect.ProjectileSpeed, SpellEffect.Pierce, SpellEffect.CritChance, SpellEffect.CritDamage),
		["mirefoot"] = R(Core | EvolutionStat.Projectiles | EvolutionStat.Area | EvolutionStat.Speed | EvolutionStat.Slow
				| EvolutionStat.Rider,
			"TrailWeaver.LayPool and PoolCap (its cooldown is the pools' spacing; it has no reach; Sucking Mud, Broad Wake, Fen Gas, Great Bog and Will-o'-Wisps by id, through LingeringZone.GripSeconds, FenGas and WispSwarm)",
			SpellEffect.ZoneDuration, SpellEffect.AreaSize, SpellEffect.SlowPower, SpellEffect.SlowDuration, SpellEffect.DotDamage, SpellEffect.CritDamage),
	};

	public static bool TryGet(string spellId, out SpellStatReaders readers) =>
		BySpell.TryGetValue(spellId ?? string.Empty, out readers);

	/// <summary>
	/// The readers of a spell as it is: its form's row if it was taken in a form that has one
	/// (SpellData.SelectedVariantId), else the spell's.
	/// </summary>
	public static bool TryGet(SpellData spell, out SpellStatReaders readers)
	{
		readers = null;
		if (spell == null)
			return false;
		if (!string.IsNullOrEmpty(spell.SelectedVariantId) && BySpell.TryGetValue(spell.SelectedVariantId, out readers))
			return true;
		return TryGet(spell.Id, out readers);
	}

	/// <summary>
	/// Whether this spell as it is (its form, its milestones) adds the player-wide Amount to its count: what
	/// Player.AmountFor pays out by and what the Split Lens charges by. False for a spell with no row.
	/// </summary>
	public static bool ReadsAmount(SpellData spell) => TryGet(spell, out SpellStatReaders readers) && readers.ReadsAmount(spell);

	/// <summary>Whether a form has a row of its own (RegressionChecks.ValidateSpellVariants).</summary>
	public static bool HasRow(string id) => !string.IsNullOrEmpty(id) && BySpell.ContainsKey(id);

	/// <summary>
	/// Every card below promises something its spell does not do, because the mechanic was never built:
	/// a burn, a freeze, a pull, an area or a count the spell has no reader for. They predate the reader
	/// check, every one is on a hand-written block, and each is a decision - build it, or rewrite the card
	/// to drop it - rather than a typo, so they are listed rather than deleted. This is the backlog, not
	/// an allowlist to grow: a new option has to set only what its spell reads, and an entry here that
	/// comes true (the spell starts reading it, or the option stops setting it) is reported as stale so
	/// the list only ever shrinks. Fields no card mentioned were simply removed instead.
	/// </summary>
	public static readonly UnbuiltPromise[] KnownUnbuilt =
	{
		// Empty since 2026-10-26: the last 46 were built or their cards rewritten to say what they pay.
	};

	private static UnbuiltPromise U(string optionId, EvolutionStat stat, string promise) =>
		new(optionId, stat, SpellEffect.None, promise);

	private static UnbuiltPromise U(string optionId, SpellEffect effect, string promise) =>
		new(optionId, EvolutionStat.Effect, effect, promise);

	/// <summary>The fields an option sets to something other than "no change".</summary>
	public static EvolutionStat StatsSetBy(SpellEvolutionOption option)
	{
		EvolutionStat set = EvolutionStat.None;
		if (option == null)
			return set;
		if (option.DamageBonus != 0 || !Same(option.DamageMultiplier, 1f)) set |= EvolutionStat.Damage;
		if (!Same(option.CooldownBonus, 0f) || !Same(option.CooldownMultiplier, 1f)) set |= EvolutionStat.Cooldown;
		if (option.ProjectileCountBonus != 0) set |= EvolutionStat.Projectiles;
		if (!Same(option.RangeBonus, 0f)) set |= EvolutionStat.Range;
		if (!Same(option.AreaMultiplier, 1f)) set |= EvolutionStat.Area;
		if (option.PierceBonus != 0) set |= EvolutionStat.Pierce;
		if (option.ChainArcBonus != 0) set |= EvolutionStat.ChainArcs;
		if (option.ChainBranchBonus != 0) set |= EvolutionStat.ChainBranches;
		if (!Same(option.KnockbackBonus, 0f)) set |= EvolutionStat.Knockback;
		if (!Same(option.SlowMagnitudeBonus, 0f)) set |= EvolutionStat.Slow;
		if (option.PoisonTickBonus != 0) set |= EvolutionStat.PoisonTick;
		if (!Same(option.SpeedMultiplier, 1f)) set |= EvolutionStat.Speed;
		if (!Same(option.ScaleMultiplier, 1f)) set |= EvolutionStat.Scale;
		if (option.Rider != ElementRider.None || !Same(option.RiderMagnitude, 0f) || !Same(option.RiderSeconds, 0f)) set |= EvolutionStat.Rider;
		if (option.Effect != SpellEffect.None || !Same(option.EffectValue, 0f)) set |= EvolutionStat.Effect;
		return set;
	}

	private static bool Same(float a, float b) => MathF.Abs(a - b) < 0.0001f;
}
