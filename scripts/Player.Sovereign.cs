using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE SOVEREIGN RITE (.ai/sovereign-spell.md, approved by Eric 2026-11-02; SovereignRite holds the numbers).
//
// A Conjunction whose second ingredient is whichever active spell the player points it at. It waits on the
// conjunction plate in the Ascension strip (GetAscensionOffers -> TryBuildSovereignOffer) once the save owns it
// (the Warden of the Deep's deed) and the run meets its gate; taking the plate opens the conjunction page with
// one card per active spell, each showing the fuel and what it would buy, and choosing one spends the pick.
//
// From then on, for the rest of the run:
//   - every other active spell is SILENCED: the fire loop skips it, its standing nodes are taken out of the
//     arena, Frost Shard's shatter and the Black Cat leave it alone, and it is never fused (IsSilenced);
//   - the anointed spell carries a milestone of its own, SpellData.SovereignBoon, which every stat reader
//     walks (SpellData.CollectActive), so the fuel reaches it through the one path the milestones use;
//   - the fuel is every silenced spell's level, live: they keep levelling, a card for one reads "+1 fuel to
//     Sovereign X" (GetLevelUpOptions), it levels straight to 8 with no mutation page or Ascension strip of
//     its own (CardLevelCap), and RefreshSovereignFuel rewrites the boon in place.
// Passives - today the one shield, Aegis Ward (SovereignRite.IsPassiveForRite) - are untouched.
public partial class Player
{
	private SpellData sovereignSpell;
	private int sovereignFuel;
	// The anointed spell's next cast flashes (.ai/sovereign-spell.md section 5, "the moment it is taken").
	private bool sovereignFlashPending;
	private SovereignCrown sovereignCrown;

	/// <summary>The spell the Sovereign Rite anointed this run, or null.</summary>
	public SpellData SovereignSpell => sovereignSpell;

	/// <summary>The anointed spell's fuel now: every silenced spell's level, summed. 0 before the Rite.</summary>
	public int SovereignFuel => sovereignFuel;

	/// <summary>The player level the Rite was taken at, or -1. Read by the balance harness.</summary>
	public int SovereignTakenAtLevel { get; private set; } = -1;

	/// <summary>
	/// True for a spell the Rite has silenced: held, levelling, counted as fuel, never fired. The one rule every
	/// caster asks (the fire loop, the Black Cat, Frost Shard's shatter).
	/// </summary>
	public bool IsSilenced(SpellData spell) =>
		sovereignSpell != null && spell != null && !ReferenceEquals(spell, sovereignSpell) && !SovereignRite.IsPassiveForRite(spell);

	/// <summary>
	/// The strip's Rite, or null: the save owns it, it is not taken, and the run meets the gate. One card on its
	/// page per active spell, in slot order, each naming the fuel it would be poured and what that buys now; a
	/// spell the Rite will not anoint, or one with less than the fuel the gate asks behind it, is shown locked.
	/// </summary>
	private LevelUpOption TryBuildSovereignOffer()
	{
		if (sovereignSpell != null)
			return null;
		SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		if (!GlobalStatsManager.IsSpellUnlockedForLevelUp(save, SovereignRite.SpellId))
			return null;
		if (!SovereignRite.MeetsGate(CurrentLevel, equippedSpells))
			return null;
		ComboDefinition recipe = ComboCatalog.Rite;
		SpellData template = ComboCatalog.Template(recipe);
		if (template == null)
		{
			GD.PushError($"Player: the Sovereign Rite has no SpellData in ContentValidator.SpellResourcePaths ({recipe?.ResourcePath}); it cannot be offered.");
			return null;
		}

		var choices = new List<SpellEvolutionOption>();
		var locks = new Dictionary<string, string>();
		foreach (SpellData spell in SovereignRite.ActiveSpells(equippedSpells))
		{
			int fuel = SovereignRite.FuelFor(spell, equippedSpells);
			SovereignGains gains = SovereignRite.GainsFor(spell, fuel);
			choices.Add(new SpellEvolutionOption
			{
				// The anointed spell's id: the selection is "sovereign_rite:<spell id>".
				Id = spell.Id,
				DisplayName = SovereignRite.SovereignName(spell.Name),
				Description = SovereignRite.GainLine(gains),
				MilestoneLevel = ComboCatalog.MilestoneLevel,
				SynergyTag = $"+{fuel} fuel",
				// The spell as it is now (its newest milestone's picture), so the row reads as the loadout.
				Icon = EvolutionIcons.Current(spell) ?? spell.Icon,
				VisualTag = "Conjunction",
			});
			string refusal = SovereignRite.RefusalFor(spell)
				?? (fuel < SovereignRite.MinimumFuel ? $"Needs {SovereignRite.MinimumFuel} fuel" : null);
			if (refusal != null)
				locks[spell.Id] = refusal;
		}

		return new LevelUpOption
		{
			SpellId = SovereignRite.SpellId,
			ComboId = SovereignRite.SpellId,
			DisplayName = template.Name,
			Description = template.Description,
			UpgradeSummary = "✶ CONJUNCTION - one spell takes every other spell's power",
			NextLevel = 1,
			MaxLevel = 1,
			IsNewUnlock = false,
			IsPassive = false,
			Icon = template.Icon,
			IsEvolutionMilestone = true,
			MilestoneLevel = ComboCatalog.MilestoneLevel,
			EvolutionChoices = choices,
			EvolutionLockReason = locks,
		};
	}

	/// <summary>
	/// The Rite taken: <paramref name="targetId"/> is anointed and every other active spell silenced. Refused
	/// unless the offer stands right now and the target is one the page would let the player choose - the
	/// selection is never trusted on its own.
	/// </summary>
	private bool TryAnoint(string targetId)
	{
		if (sovereignSpell != null)
		{
			GD.PrintErr($"Cannot take the Sovereign Rite again: {sovereignSpell.Name} is already anointed.");
			return false;
		}
		if (TryBuildSovereignOffer() is not LevelUpOption offer)
		{
			GD.PrintErr("Cannot take the Sovereign Rite: the save does not own it or the run does not meet its gate.");
			return false;
		}
		SpellData target = equippedSpells.FirstOrDefault(s => s != null && s.Id.Equals(targetId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		if (target == null || offer.EvolutionLockReason.ContainsKey(target.Id) || offer.EvolutionChoices.All(c => c.Id != target.Id))
		{
			GD.PrintErr($"Cannot anoint '{targetId}': {(target == null ? "not held" : offer.EvolutionLockReason.TryGetValue(target.Id, out string why) ? why : "not on the page")}.");
			return false;
		}

		sovereignSpell = target;
		sovereignElement = SfxCatalog.DominantElement(target);
		target.Name = SovereignRite.SovereignName(target.Name);
		target.SovereignBoon = SovereignRite.BuildBoon(target, 0);
		// The crown's second gift (Eric, 2026-11-07, in-flight 50): the crowned spell gains its
		// unchosen Ascension too - both level-8 paths at once. If it has not ascended yet, the twin
		// arrives with the pick instead (SpellData.ApplyEvolution calls this again).
		target.RefreshSovereignTwin();
		// A Conjunction's script reads its frozen parents' numbers (Sagittarius: the arrows from the bow's, the
		// stars from the rain's), so they carry the same boon - one object, rewritten in place with the fuel.
		foreach (SpellData parent in target.ComboParents ?? Array.Empty<SpellData>())
		{
			if (parent != null)
				parent.SovereignBoon = target.SovereignBoon;
		}

		var silenced = new List<string>();
		foreach (SpellData spell in equippedSpells)
		{
			if (!IsSilenced(spell))
				continue;
			Silence(spell);
			silenced.Add(spell.Name);
		}
		// Its own standing nodes go too, and are made again on its next cast - crowned, and that cast flashes.
		DropStandingNodes(target);
		spellFireTimers[target.Id] = 1e6f;
		sovereignFlashPending = true;
		SovereignTakenAtLevel = CurrentLevel;
		RefreshSovereignFuel();
		AttachSovereignCrown();

		RunEvents.RecordSpellEvolved(target.Id);
		SfxPlayer.Global(SfxCatalog.SpellEvolve);
		GD.Print($"SOVEREIGN RITE: {target.Name} anointed with {sovereignFuel} fuel at player level {CurrentLevel}; silenced {string.Join(", ", silenced)}");
		RaiseLoadoutChanged();
		return true;
	}

	// One spell falls silent: flagged (CardLevelCap and the cards read it), its timer held at zero, and every
	// node it keeps standing in the arena or on the wizard taken away. What it already put on the ground - a
	// ward, a wall, a pool - burns out on its own clock.
	private void Silence(SpellData spell)
	{
		spell.SilencedBySovereign = true;
		spellFireTimers[spell.Id] = 0f;
		DropStandingNodes(spell);
	}

	// Every node a spell keeps alive between casts, found as the fusion and the Penknife find them
	// (RemovePersistentSpellInstance), plus the four that live on the wizard by type.
	private void DropStandingNodes(SpellData spell)
	{
		RemovePersistentSpellInstance(spell);
		foreach (Node child in GetChildren())
		{
			SpellData owner = child switch
			{
				OrbitingBlade orbit => orbit.SpellData,
				Flamethrower flame => flame.SpellData,
				ElementalPulse pulse => pulse.SpellData,
				TrailWeaver weaver => weaver.SpellData,
				_ => null,
			};
			if (owner == null || !owner.Id.Equals(spell.Id, StringComparison.OrdinalIgnoreCase))
				continue;
			if (ReferenceEquals(child, cinderbreathEmitter))
				cinderbreathEmitter = null;
			child.QueueFree();
		}
		if (spell.Id.Equals("hunters_draw", StringComparison.OrdinalIgnoreCase))
			ResetHuntersDraw();
	}

	/// <summary>
	/// The fuel read again from the loadout and written into the boon. Called whenever a level or a spell is
	/// added after the Rite; a no-op before it.
	/// </summary>
	private void RefreshSovereignFuel()
	{
		if (sovereignSpell == null)
			return;
		sovereignFuel = SovereignRite.FuelFor(sovereignSpell, equippedSpells);
		SovereignRite.Refresh(sovereignSpell.SovereignBoon, sovereignSpell, sovereignFuel);
	}

	// A spell taken after the Rite is silenced the moment it arrives: it is fuel from its first level.
	private void SilenceIfLate(SpellData spell)
	{
		if (!IsSilenced(spell))
			return;
		Silence(spell);
		RefreshSovereignFuel();
	}

	/// <summary>
	/// The card a silenced spell is offered on: its next level is one more level of fuel, and that is all it
	/// says. No mutation page - a silenced spell has no use for one - so the level 4 card is a plain level.
	/// </summary>
	private void DressAsFuel(LevelUpOption option)
	{
		if (sovereignSpell == null || option == null)
			return;
		string line = $"✶ +1 fuel to {sovereignSpell.Name}";
		option.IsEvolutionMilestone = false;
		option.MilestoneLevel = 0;
		option.EvolutionChoices = new List<SpellEvolutionOption>();
		if (option.IsNewUnlock)
			option.StatSummary = line;
		else
			option.UpgradeSummary = line;
	}

	// --- how it looks (section 5) -------------------------------------------------------------------------

	/// <summary>
	/// True for the anointed spell, or one of the frozen parents a Conjunction reads (Sagittarius's arrows are
	/// handed the bow's SpellData for their look).
	/// </summary>
	private bool IsSovereignSource(SpellData spell)
	{
		if (sovereignSpell == null || spell == null)
			return false;
		if (ReferenceEquals(spell, sovereignSpell))
			return true;
		foreach (SpellData parent in sovereignSpell.ComboParents ?? Array.Empty<SpellData>())
		{
			if (ReferenceEquals(parent, spell))
				return true;
		}
		return false;
	}

	private SovereignHits sovereignHits;
	private Element sovereignElement;

	// One pooled layer in the arena marks every strike; made on the first.
	private void MarkSovereignHit(Vector2 where)
	{
		if (sovereignHits == null || !IsInstanceValid(sovereignHits))
			sovereignHits = SovereignHits.For(GetParent(), sovereignElement);
		// A spell whose casts are not nodes flashes on its first strike instead (CrownIfSovereign takes it otherwise).
		bool flash = sovereignFlashPending;
		sovereignFlashPending = false;
		sovereignHits?.Mark(where, flash);
	}

	/// <summary>Whether a node a cast just made belongs to the anointed spell, and so is crowned.</summary>
	private void CrownIfSovereign(CanvasItem visual, SpellData spell)
	{
		if (!IsSovereignSource(spell) || visual is not Node2D node)
			return;
		bool flash = sovereignFlashPending;
		sovereignFlashPending = false;
		SovereignCorona.Attach(node, sovereignElement, flash);
	}

	// The corona's ring textures are kept for the run; a static Resource that outlived the engine would be
	// reported as still in use at exit, so they go when the wizard does.
	public override void _ExitTree()
	{
		SovereignCorona.ClearCache();
	}

	// The small turning crown-ring of the anointed spell's element over the wizard's head.
	private void AttachSovereignCrown()
	{
		if (sovereignCrown != null && IsInstanceValid(sovereignCrown))
			sovereignCrown.QueueFree();
		// Just above the hood: the body sprite's top edge (a 48 cell at its scale, centred) and a little air.
		float top = bodySprite != null ? bodySprite.Position.Y - 24f * bodySprite.Scale.Y : -52f;
		sovereignCrown = new SovereignCrown
		{
			Name = "SovereignCrown",
			Element = sovereignElement,
			Position = new Vector2(0f, top - 6f),
		};
		AddChild(sovereignCrown);
	}
}
