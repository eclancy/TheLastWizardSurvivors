using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE SOVEREIGN RITE (.ai/sovereign-spell.md; SovereignRite holds the numbers, SovereignRiteCaster the payload).
//
// Since 2026-11-08 an ordinary two-spell Conjunction (Eric: "the soverign rite should be a combination spell
// itself"): Arcane Explosion down Resonance and Gravewell down Mass Grave, offered in the Ascension strip like
// every recipe (TryBuildComboOffer) and made by TryFuse - once the run also meets its late gate (RiteGateMet: the
// player's level, and the fuel the other active spells would pour in). No lesson of its own ("it just unlocks with
// combination spells"): ConjunctionsLearned is its gate as it is every recipe's.
//
// Fusing it crowns it (Crown). From then on, for the rest of the run:
//   - every other active spell is SILENCED: the fire loop skips it, its standing nodes are taken out of the
//     arena, Frost Shard's shatter and the Black Cat leave it alone, and nothing fuses again (IsSilenced);
//   - the Rite carries a milestone of its own, SpellData.SovereignBoon, shared with its two frozen parents, which
//     every stat reader walks (SpellData.CollectActive), so the fuel reaches it through the one path the
//     milestones use;
//   - the fuel is every silenced spell's level, live: they keep levelling, a card for one reads "+1 fuel to
//     Sovereign Rite" (GetLevelUpOptions), it levels straight to 8 with no mutation page or Ascension strip of its
//     own (CardLevelCap), and RefreshSovereignFuel rewrites the boon in place.
// Passives - today the one shield, Aegis Ward (SovereignRite.IsPassiveForRite) - are untouched.
public partial class Player
{
	private SpellData sovereignSpell;
	private int sovereignFuel;
	// The Rite's first cast flashes (.ai/sovereign-spell.md section 5, "the moment it is made").
	private bool sovereignFlashPending;
	private SovereignCrown sovereignCrown;
	private SovereignRiteCaster sovereignRiteCaster;

	/// <summary>The Sovereign Rite, once made this run; otherwise null.</summary>
	public SpellData SovereignSpell => sovereignSpell;

	/// <summary>The Rite's fuel now: every silenced spell's level, summed. 0 before the Rite.</summary>
	public int SovereignFuel => sovereignFuel;

	/// <summary>The player level the Rite was made at, or -1. Read by the balance harness.</summary>
	public int SovereignTakenAtLevel { get; private set; } = -1;

	/// <summary>
	/// True for a spell the Rite has silenced: held, levelling, counted as fuel, never fired. The one rule every
	/// caster asks (the fire loop, the Black Cat, Frost Shard's shatter).
	/// </summary>
	public bool IsSilenced(SpellData spell) =>
		sovereignSpell != null && spell != null && !ReferenceEquals(spell, sovereignSpell) && !SovereignRite.IsPassiveForRite(spell);

	/// <summary>
	/// The fuel the Rite would be made with now: every active spell's levels but its two parents', which become it.
	/// </summary>
	private int RiteFuelAtFusion(ComboStanding standing) =>
		SovereignRite.FuelFrom(equippedSpells, standing?.FirstHeld, standing?.SecondHeld);

	/// <summary>The Rite's own gate on top of its recipe: the player's level and the fuel (SovereignRite.MeetsGate).</summary>
	private bool RiteGateMet(ComboStanding standing) =>
		SovereignRite.MeetsGate(CurrentLevel, RiteFuelAtFusion(standing));

	/// <summary>
	/// Whether the strip offers this recipe now - its standing, the lesson, the Rite's gate and "nothing fuses after
	/// the Rite" - without building the offer. Read by the balance harness, which asks every frame.
	/// </summary>
	public bool IsComboOffered(ComboDefinition recipe)
	{
		if (recipe == null || SovereignSpell != null || !ConjunctionsLearned)
			return false;
		ComboStanding standing = EvaluateCombo(recipe);
		return standing.Ready && (!recipe.IsRite || RiteGateMet(standing));
	}

	/// <summary>
	/// The Rite made: it is crowned, its fuel poured in, and every other active spell silenced. Called by TryFuse
	/// once the fusion has put the Rite in its first parent's slot.
	/// </summary>
	private void Crown(SpellData rite)
	{
		sovereignSpell = rite;
		sovereignElement = SfxCatalog.DominantElement(rite);
		rite.SovereignBoon = SovereignRite.BuildBoon(rite, 0);
		// The caster reads the frozen parents' numbers (the blast's hit and reach from Arcane Explosion, the grave's
		// from Gravewell), so they carry the same boon - one object, rewritten in place with the fuel.
		foreach (SpellData parent in rite.ComboParents ?? Array.Empty<SpellData>())
		{
			if (parent != null)
				parent.SovereignBoon = rite.SovereignBoon;
		}

		var silenced = new List<string>();
		foreach (SpellData spell in equippedSpells)
		{
			if (!IsSilenced(spell))
				continue;
			Silence(spell);
			silenced.Add(spell.Name);
		}
		// Its first cast goes off at once, crowned, and flashes.
		spellFireTimers[rite.Id] = 1e6f;
		sovereignFlashPending = true;
		SovereignTakenAtLevel = CurrentLevel;
		RefreshSovereignFuel();
		AttachSovereignCrown();
		GD.Print($"SOVEREIGN RITE: made with {sovereignFuel} fuel at player level {CurrentLevel}; silenced {string.Join(", ", silenced)}");
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
		sovereignFuel = SovereignRite.FuelFrom(equippedSpells, sovereignSpell);
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

	// --- the cast (SovereignRiteCaster) ----------------------------------------------------------------------

	// The cooldown is the grave's: each cast lets the crowned blasts fall on the thickest packs in reach, a great
	// grave opening under each; the echoes in the graves run in the node. Amount (the fuel's +1 per 20, and the
	// player's own) is more blasts a cast.
	private CastOutcome FireSovereignRite(SpellData spell)
	{
		if (SovereignRiteScene == null || GetParent() is not Node2D arena)
			return CastOutcome.NoTarget;
		SpellData blast = spell.ComboParent(SovereignRiteCaster.BlastSpellId);
		SpellData grave = spell.ComboParent(SovereignRiteCaster.GraveSpellId);
		if (blast == null || grave == null)
		{
			GD.PushError("Player: the Sovereign Rite carries no frozen parents (SpellData.ComboParents); it was not made by a fusion.");
			return CastOutcome.NoTarget;
		}
		if (sovereignRiteCaster == null || !IsInstanceValid(sovereignRiteCaster))
		{
			sovereignRiteCaster = SovereignRiteScene.Instantiate<SovereignRiteCaster>();
			sovereignRiteCaster.PlayerRef = this;
			arena.AddChild(TagSpell(sovereignRiteCaster));
		}
		int blasts = Math.Max(1, spell.GetProjectileCountAtLevel(spell.CurrentLevel) + AmountFor(spell));
		return sovereignRiteCaster.Cast(spell, blast, grave, GravewellScene, blasts, spell.GetRangeAtLevel(spell.CurrentLevel),
			GetEffectiveAreaMultiplier(), GetSpellDamageMultiplier(spell), durationMultiplier)
			? CastOutcome.Fired : CastOutcome.NoTarget;
	}

	// --- how it looks (section 5) -------------------------------------------------------------------------

	/// <summary>True for the Rite, or one of its frozen parents (the caster hands their SpellData to nothing, but a
	/// strike credited to one is still the Rite's).</summary>
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
		// A cast that makes no node of its own flashes on its first strike instead (CrownIfSovereign takes it otherwise).
		bool flash = sovereignFlashPending;
		sovereignFlashPending = false;
		sovereignHits?.Mark(where, flash);
	}

	/// <summary>Whether a node a cast just made belongs to the Rite, and so is crowned.</summary>
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

	// The small turning crown-ring of the Rite's element over the wizard's head.
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
