using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE FIRST WAVE OF .ai/new-items.md (Eric's answers, 2026-11-02): the Poppet and the Gimmel Ring with
// their sets, the Waywiser and Bone Rattle boons, and the run's rerolls and bans spent on the chest
// screen. Kept beside Player.cs rather than in it so the rules read as one piece; each is called from
// the one place in Player.cs where its moment happens (the hit, the touch, the firing loop, the
// reaction, the level-up draw), by a single line.
public partial class Player
{
	// --- The Poppet (section 3): Vulnerable, Weaken and Wither, joined ----------------------------
	//
	// The numbers on the cards: ChestItemCatalog builds each description from them, so the card and the
	// rule cannot disagree (the Reliquary Vial's pattern).
	public const float PrickingPinVulnerable = 0.20f;
	public const float PrickingPinSeconds = 3f;
	public const float BindingThreadWeaken = 0.20f;
	public const float BindingThreadSeconds = 3f;
	public const float SingedLockWither = 0.10f;
	public const float SingedLockRadius = 90f;
	public const float PoppetPartialDurationBonus = 0.20f;
	public const float PoppetFullDurationBonus = 0.50f;
	public const float PoppetVulnerableBonus = 0.15f;
	// Short, and refreshed by every hit while the two debuffs hold: the mark is the two debuffs' echo,
	// not a status of its own. Lengthened like any other Vulnerable by the set's own duration bonus.
	public const float PoppetVulnerableSeconds = 0.5f;

	private bool prickingPinHeld, bindingThreadHeld, singedLockHeld, poppetComplete;
	private readonly List<Enemy> singedLockScratch = new();

	// --- The Gimmel Ring (section 4): the pair ---------------------------------------------------
	//
	// SIMPLIFIED (Eric, 2026-11-02: "Fede hoop / gimmel ring descriptions are confusing. Simplify those
	// items"). Every card is one plain sentence about "your two highest-level spells", with no exception
	// to explain: the wizard's own spell counts now, since nothing the pair does any more moves a level.
	// The Fede Hoop is a flat +15% (it was +30% to an enemy the other spell hit in the last second, about
	// +15% on the sweep's overlap); the Garnet Bezel is +20% recharge (it moved the other spell's timer a
	// fifth of the caster's cooldown per cast - +20% recharge while both fired, by construction). The
	// Betrothal Hoop is the one that would have moved a level: doubling the upgrades of the two highest
	// would lean on the wizard's own spell and pull the Apotheosis earlier, so it leans the other way -
	// toward the lowest-level spell, which is never the wizard's own in a run that is climbing it.
	public const float BetrothalHoopUpgradeWeight = 2f;
	public const float FedeHoopBonus = 0.15f;
	public const float GarnetBezelRecharge = 0.20f;
	public const float GimmelPartialDamageBonus = 0.08f;
	public const float GimmelFullDamageBonus = 0.25f;

	private bool betrothalHoopHeld, garnetBezelHeld;
	// The pair's damage, the Fede Hoop's and the set's partial and full summed: a set's full effect stacks
	// on its partial and is never refunded (.ai/passives-and-items.md section 5).
	private float gimmelPairDamageBonus;

	// The pair, cached: rebuilt only when the loadout changes (RaiseLoadoutChanged, which every level-up,
	// fusion and cut goes through), so a hit reads two references and never sorts anything.
	private SpellData pairFirst, pairSecond;
	private bool pairDirty = true;

	// --- The boons (section 7) --------------------------------------------------------------------
	// Waywiser: how much faster the furthest-from-ready spell winds while walking; 0 = not held.
	private float boonWalkingRecharge;
	// Bone Rattle: seconds a reaction frightens for; 0 = not held.
	private float boonFrightenSeconds;

	/// <summary>The spell the Waywiser wound on the last frame of play, or null. For the pause screen.</summary>
	public string WaywiserWindingSpellName { get; private set; }

	/// <summary>True while the Waywiser boon is held.</summary>
	public bool HasWaywiser => boonWalkingRecharge > 0f;

	// --- The chest's charges (Eric: "you can use rerolls on chests as well as level ups. You can also
	// ban items") ------------------------------------------------------------------------------------
	private readonly HashSet<string> bannedRelicIds = new(StringComparer.OrdinalIgnoreCase);

	// Called from _Ready. A run is one Player, so a new one puts the arena's shared debuff scale back;
	// the tree-exit puts it back for whatever loads after the run.
	private void ResetItemRules()
	{
		Enemy.DebuffDurationScale = 1f;
		TreeExiting += () => Enemy.DebuffDurationScale = 1f;
	}

	// --- relics: taken, and the sets assembled ----------------------------------------------------

	// From ApplyChestItemEffect. Every one is read at its moment, so taking it only switches it on.
	private void TakeItemRuleRelic(string itemId)
	{
		switch (itemId)
		{
			case ChestItemCatalog.PrickingPin: prickingPinHeld = true; break;
			case ChestItemCatalog.BindingThread: bindingThreadHeld = true; break;
			case ChestItemCatalog.SingedLock: singedLockHeld = true; break;
			case ChestItemCatalog.BetrothalHoop: betrothalHoopHeld = true; break;
			case ChestItemCatalog.FedeHoop: gimmelPairDamageBonus += FedeHoopBonus; break;
			case ChestItemCatalog.GarnetBezel: garnetBezelHeld = true; break;
		}
	}

	// From ApplyPartialSetEffect.
	private void ApplyItemRulePartial(string setId)
	{
		if (setId == ChestItemCatalog.PoppetSetId)
			Enemy.DebuffDurationScale += PoppetPartialDurationBonus;
		else if (setId == ChestItemCatalog.GimmelRingSetId)
			gimmelPairDamageBonus += GimmelPartialDamageBonus;
	}

	// From RefreshChestSetEffects, the moment a set completes.
	private void ApplyItemRuleSet(string setId)
	{
		if (setId == ChestItemCatalog.PoppetSetId)
		{
			Enemy.DebuffDurationScale += PoppetFullDurationBonus;
			poppetComplete = true;
		}
		else if (setId == ChestItemCatalog.GimmelRingSetId)
		{
			gimmelPairDamageBonus += GimmelFullDamageBonus;
		}
	}

	// --- the Poppet, at the hit -------------------------------------------------------------------

	/// <summary>
	/// The whole Poppet's mark, before the hit reads Vulnerable: a target carrying two different debuffs is
	/// Vulnerable +15% as well, through the status, so it wears the bracket mark and shares the +60% cap.
	/// </summary>
	/// <remarks>
	/// The debuffs counted are slowed, poisoned, weakened and withered - not Vulnerable itself, though the
	/// doc's list names it. Vulnerable is what the rule PAYS, and counted as an input it fed itself: one
	/// other debuff and the Poppet's own mark made two, so a withered enemy (Wither is permanent) stayed
	/// Vulnerable for as long as it was being hit, and a stronger mark (Shadow Bolt's, the Pricking Pin's)
	/// was kept alive by the weaker one's refresh. Against a Vulnerable enemy the rule could only ever do
	/// that - its 15% is below every other source - so leaving Vulnerable out costs nothing else.
	/// </remarks>
	private void ApplyPoppetMark(Enemy target)
	{
		if (!poppetComplete || target.IsDying)
			return;
		int carried = (target.IsSlowed ? 1 : 0) + (target.IsPoisoned ? 1 : 0)
			+ (target.IsWeakened ? 1 : 0) + (target.IsWithered ? 1 : 0);
		if (carried >= 2)
			target.ApplyVulnerable(PoppetVulnerableBonus, PoppetVulnerableSeconds);
	}

	// The Pricking Pin, after a critical hit has landed: the crit itself is not raised, the hits after it
	// are. Stacking-resistant, so it never weakens a stronger mark.
	private void PrickOnCrit(Enemy target)
	{
		if (prickingPinHeld && !target.IsDying)
			target.ApplyVulnerable(PrickingPinVulnerable, PrickingPinSeconds);
	}

	/// <summary>
	/// The Binding Thread, at every touch the contact loop resolves: the toucher is Weakened BEFORE its
	/// touch is scaled, so the touch that binds it is the first one cut. A 1-damage touch at 20% Weaken
	/// lands for nothing one time in five (Enemy.ScaleOutgoingDamage), and that miss skips DamageTaken as a
	/// dodge does. A boss stops at its MaxWeakenFraction (25%).
	/// </summary>
	private void BindToucher(Node toucher)
	{
		if (bindingThreadHeld && toucher is Enemy bound && !bound.IsDying)
			bound.ApplyWeaken(BindingThreadWeaken, BindingThreadSeconds);
	}

	// The Singed Lock, on a kill the player's hit made: the ash spreads to what stood near. Wither does
	// the rest - the strongest wins, it never kills, and it stops at 25% (8% on a boss). The grid's broad
	// phase, then the exact distance, into a list kept for it: nothing allocated per kill.
	private void SpreadSingedLock(Vector2 at)
	{
		if (!singedLockHeld)
			return;
		EnemyRegistry.CollectNear(at, SingedLockRadius, singedLockScratch);
		const float radiusSquared = SingedLockRadius * SingedLockRadius;
		foreach (Enemy near in singedLockScratch)
		{
			if (IsInstanceValid(near) && !near.IsDying && at.DistanceSquaredTo(near.GlobalPosition) <= radiusSquared)
				near.ApplyWither(SingedLockWither);
		}
		singedLockScratch.Clear();
	}

	// --- the Gimmel Ring --------------------------------------------------------------------------

	/// <summary>
	/// The pair: the run's two highest-level spells, the wizard's own among them. Ties go to the earlier
	/// slot; a Conjunction is always one of them. Either may be null with fewer than two spells held.
	/// </summary>
	public (SpellData First, SpellData Second) GetPairSpells()
	{
		if (pairDirty)
			RebuildPair();
		return (pairFirst, pairSecond);
	}

	// A Conjunction is level 1 of 1 on its own counter, so it is ranked as level 8 - the level its parents
	// were at - and one above, so it wins the tie with an Ascended spell in an earlier slot. That is what
	// keeps a fusion from taking the Gimmel Ring's bonus away the moment it was built toward, and the card
	// need not say so: a fusion of two level 8 spells is plainly one of the highest.
	private const int ConjunctionPairRank = 9;

	private void RebuildPair()
	{
		pairDirty = false;
		pairFirst = pairSecond = null;
		int firstRank = int.MinValue, secondRank = int.MinValue;
		foreach (SpellData spell in equippedSpells)
		{
			if (spell == null || string.IsNullOrEmpty(spell.Id))
				continue;
			int rank = spell.IsCombination ? ConjunctionPairRank : spell.CurrentLevel;
			// Strictly greater, walking in slot order: a tie stays with the earlier slot.
			if (rank > firstRank)
			{
				pairSecond = pairFirst;
				secondRank = firstRank;
				pairFirst = spell;
				firstRank = rank;
			}
			else if (rank > secondRank)
			{
				pairSecond = spell;
				secondRank = rank;
			}
		}
	}

	private static bool SameSpell(SpellData a, SpellData b) =>
		a != null && b != null && (ReferenceEquals(a, b) || a.Id.Equals(b.Id, StringComparison.OrdinalIgnoreCase));

	private bool IsPairSpell(SpellData spell)
	{
		var (first, second) = GetPairSpells();
		return SameSpell(spell, first) || SameSpell(spell, second);
	}

	/// <summary>The Fede Hoop and the set: the pair's own damage. Two reference compares a hit.</summary>
	private int ApplyPairDamage(SpellData source, int damage)
	{
		if (source == null || gimmelPairDamageBonus <= 0f || !IsPairSpell(source))
			return damage;
		return Mathf.RoundToInt(damage * (1f + gimmelPairDamageBonus));
	}

	/// <summary>The Garnet Bezel: the pair recharges faster. Divides the interval the firing loop measures.</summary>
	private float PairRechargeScale(SpellData spell) =>
		garnetBezelHeld && IsPairSpell(spell) ? 1f + GarnetBezelRecharge : 1f;

	// The Betrothal Hoop, inside the upgrade group only: it changes WHICH upgrade is dealt, never whether
	// one is (UpgradeOfferChance is untouched, and NormalizeOfferWeightsByGroup keeps the group's share).
	// Every held spell at the lowest level leans, so a hand of level-1 spells is unchanged.
	private void WeightLowestUpgrades(List<LevelUpOption> upgrades, List<float> weights)
	{
		if (!betrothalHoopHeld || upgrades.Count == 0)
			return;
		int lowest = int.MaxValue;
		foreach (SpellData spell in equippedSpells)
		{
			if (spell != null && !spell.IsCombination)
				lowest = Math.Min(lowest, spell.CurrentLevel);
		}
		for (int i = 0; i < upgrades.Count; i++)
		{
			SpellData held = equippedSpells.FirstOrDefault(s => s != null && s.Id.Equals(upgrades[i].SpellId, StringComparison.OrdinalIgnoreCase));
			if (held != null && held.CurrentLevel == lowest)
				weights[i] *= BetrothalHoopUpgradeWeight;
		}
	}

	// --- Waywiser ---------------------------------------------------------------------------------

	/// <summary>
	/// The Waywiser, after the firing loop: while the player walks (IsStandingStill is intent, the
	/// Brazier's definition), whichever spell is furthest from ready ticks <see cref="boonWalkingRecharge"/>
	/// faster. The loop hands over the spell it measured furthest; a max over at most seven floats.
	/// </summary>
	private void TickWaywiser(SpellData furthest, double delta)
	{
		if (boonWalkingRecharge <= 0f || furthest == null || IsStandingStill || HeldForBossEntrance)
		{
			WaywiserWindingSpellName = null;
			return;
		}
		if (spellFireTimers.TryGetValue(furthest.Id, out float timer))
			spellFireTimers[furthest.Id] = timer + boonWalkingRecharge * (float)delta;
		WaywiserWindingSpellName = furthest.Name;
	}

	// --- the chest's charges ----------------------------------------------------------------------

	/// <summary>
	/// Strikes a relic out of the run's chest pool for the rest of the run, spending one of the run's bans -
	/// the same pool the level-up screen spends (TryBanSpell). False when there is none left, the id is no
	/// relic, or it is already struck.
	/// </summary>
	public bool TryBanRelic(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId) || BansRemaining <= 0)
			return false;
		if (!ChestItemCatalog.AllItemIds.Contains(itemId, StringComparer.OrdinalIgnoreCase))
			return false;
		if (!bannedRelicIds.Add(itemId))
			return false;
		BansRemaining--;
		return true;
	}

	public IReadOnlyCollection<string> GetBannedRelicIds() => bannedRelicIds;

	/// <summary>
	/// The relics a chest must not deal now, owned or not: every relic struck this run, the Scribe's
	/// Penknife while it has nothing to cut (CanUsePenknife), and any relic whose card names a system this
	/// run has not been taught (ChestItemCatalog.TeachingNamedBy, Eric's Q9). Built per chest, never per frame.
	/// </summary>
	public List<string> GetChestUnavailableItems()
	{
		var unavailable = new List<string>(bannedRelicIds);
		if (!CanUsePenknife)
			unavailable.Add(ChestItemCatalog.ScribesPenknife);
		foreach (string id in ChestItemCatalog.AllItemIds)
		{
			if (ChestItemCatalog.TeachingNamedBy(id) is Teaching named && !HasLearnedForRun(named))
				unavailable.Add(id);
		}
		return unavailable;
	}

	// What this run was taught, by the flags read at its start (the Rite by ownership, live, as its offer is).
	private bool HasLearnedForRun(Teaching system) => system switch
	{
		Teaching.Ascension => AscensionLearned,
		Teaching.Apotheosis => ApotheosisLearned,
		Teaching.Conjunctions => ConjunctionsLearned,
		_ => Teachings.HasLearned(GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data, Teaching.SovereignRite),
	};

	/// <summary>Adds to the run's reroll and ban pools. Tools (_UiShot --chest-charges) and, later, the Bone Dice.</summary>
	public void AddLevelUpCharges(int rerolls, int bans)
	{
		RerollsRemaining = Math.Max(0, RerollsRemaining + rerolls);
		BansRemaining = Math.Max(0, BansRemaining + bans);
	}
}
