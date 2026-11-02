using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

// THE COMBINATIONS (.ai/combo-spells.md, approved by Eric 2026-11-01; ComboCatalog is the one authority).
//
// Two spells, each at level 8 down the one Ascension a recipe names, offered as one in the Ascension
// strip above the level-up cards, where the Apotheosis already waits (GetAscensionOffers). Taking it
// spends the pick: both parents leave the loadout, the combination takes the first parent's slot and
// the second slot is freed. It is terminal - no levels, no milestones, never fused again - and neither
// parent is ever offered again that run (WasFusedAway, read by GetLevelUpOptions).
//
// The combination carries both parents' element weights exactly (ComboCatalog.SumWeights), so taking it
// never moves an element tier in either direction and can never drop a wizard out of the tier his
// Apotheosis needs. Its numbers are its parents', read from the two frozen parents it carries
// (SpellData.ComboParents) by its own script, times its own multipliers.
public partial class Player
{
	// Spells that left the loadout in a fusion this run. Never offered again as a new spell.
	private readonly HashSet<string> fusedAwaySpellIds = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>True for a spell that went into a combination this run: it is never offered again.</summary>
	public bool WasFusedAway(string spellId) => !string.IsNullOrEmpty(spellId) && fusedAwaySpellIds.Contains(spellId);

	// The wizard's own starting spell, never an ingredient for him: its last form is his Apotheosis. The
	// Test Wizard has none and picks his own opener, so for him nothing is excluded.
	private string OwnSpellForCombos =>
		selectedCharacter == null || selectedCharacter.Id.Equals("test_wizard", StringComparison.OrdinalIgnoreCase)
			? null
			: selectedCharacter.StartingSpellResource?.Id;

	/// <summary>Where a recipe stands against this run's loadout (ComboCatalog.Evaluate, the one rule).</summary>
	public ComboStanding EvaluateCombo(ComboDefinition recipe) => ComboCatalog.Evaluate(recipe, equippedSpells, OwnSpellForCombos);

	/// <summary>
	/// The strip's offer for a recipe this run meets, or null. One form on its page - the combination - and
	/// the elements it would carry, which are the two parents' summed: no element moves.
	/// </summary>
	private LevelUpOption TryBuildComboOffer(ComboDefinition recipe)
	{
		// After the Sovereign Rite the loadout is one spell and its fuel: nothing fuses (.ai/sovereign-spell.md
		// section 4, "a spell already anointed cannot fuse"; a silenced pair would trade its fuel for one level).
		if (SovereignSpell != null)
			return null;
		// Not until a landmark has taught them (Teachings, .ai/slow-start.md section 3). Nothing is offered,
		// so the recipe's Feat is not met either (Node2DGame.RecordCombinationsMet reads this strip).
		if (!ConjunctionsLearned)
			return null;
		ComboStanding standing = EvaluateCombo(recipe);
		if (!standing.Ready)
			return null;
		SpellData template = ComboCatalog.Template(recipe);
		if (template == null)
		{
			GD.PushError($"Player: combination '{recipe.Id}' has no SpellData in ContentValidator.SpellResourcePaths ({recipe.ResourcePath}); it cannot be offered.");
			return null;
		}

		var tags = new Dictionary<string, int>();
		foreach (var pair in ComboCatalog.SumWeights(standing.FirstHeld, standing.SecondHeld))
			tags[pair.Key] = pair.Value;
		return new LevelUpOption
		{
			SpellId = recipe.Id,
			ComboId = recipe.Id,
			DisplayName = template.Name,
			Description = template.Description,
			UpgradeSummary = $"✶ CONJUNCTION - {standing.FirstHeld.Name} and {standing.SecondHeld.Name} become {template.Name}",
			NextLevel = 1,
			MaxLevel = 1,
			IsNewUnlock = false,
			IsPassive = false,
			Icon = template.Icon,
			IsEvolutionMilestone = true,
			MilestoneLevel = ComboCatalog.MilestoneLevel,
			EvolutionChoices = new List<SpellEvolutionOption> { ComboCatalog.FormOf(recipe, template) },
			SpellElementTags = tags,
			ResultingElementCounts = GetElementInstanceCounts().ToDictionary(p => p.Key.ToString(), p => p.Value),
		};
	}

	/// <summary>Every combination the strip offers now.</summary>
	private IEnumerable<LevelUpOption> BuildComboOffers()
	{
		foreach (ComboDefinition recipe in ComboCatalog.All)
		{
			if (TryBuildComboOffer(recipe) is LevelUpOption offer)
				yield return offer;
		}
	}

	/// <summary>
	/// The fusion: both parents leave, the combination takes the first one's slot, the second slot is
	/// freed. Refused unless the recipe is met right now - the selection is never trusted on its own.
	/// </summary>
	private bool TryFuse(ComboDefinition recipe)
	{
		if (SovereignSpell != null)
		{
			GD.PrintErr($"Cannot make '{recipe.Id}': the Sovereign Rite holds the loadout ({SovereignSpell.Name}).");
			return false;
		}
		ComboStanding standing = EvaluateCombo(recipe);
		if (!standing.Ready)
		{
			GD.PrintErr($"Cannot make '{recipe.Id}': the recipe is not met ({(standing.Taken ? "already made" : standing.Seal != ComboSeal.None ? standing.Seal.ToString() : "a parent is not at level 8 down its named Ascension")}).");
			return false;
		}
		SpellData template = ComboCatalog.Template(recipe);
		if (template?.Duplicate(true) is not SpellData combo)
		{
			GD.PushError($"Player: combination '{recipe.Id}' has no SpellData to make.");
			return false;
		}

		SpellData first = standing.FirstHeld;
		SpellData second = standing.SecondHeld;
		// Frozen as they stand. They leave the loadout here and never level again; the copies keep anything
		// that later touches the run's own objects from reaching through into the combination.
		var frozenFirst = (SpellData)first.Duplicate(true);
		var frozenSecond = (SpellData)second.Duplicate(true);
		combo.ComboParents = new[] { frozenFirst, frozenSecond };
		combo.ElementWeights = ComboCatalog.SumWeights(first, second);
		combo.CurrentLevel = 1;
		combo.MaxLevel = 1;
		// The combination's own script reads its parents' numbers; the few the fire loop and the cards read
		// from the spell itself are set from them here, once.
		if (recipe.Id.Equals(Sagittarius.SpellId, StringComparison.OrdinalIgnoreCase))
			Sagittarius.Freeze(combo);

		int slot = equippedSpells.IndexOf(first);
		DropForFusion(first);
		DropForFusion(second);
		equippedSpells[slot] = combo;
		equippedSpells.Remove(second);
		fusedAwaySpellIds.Add(first.Id);
		fusedAwaySpellIds.Add(second.Id);

		spellFireTimers[combo.Id] = 0f;
		RunEvents.RecordSpellEvolved(combo.Id);
		SfxPlayer.Global(SfxCatalog.SpellEvolve);
		GD.Print($"CONJUNCTION: {first.Name} and {second.Name} became {combo.Name} (slot {slot + 1}; {equippedSpells.Count} of {SpellSlots} held)");
		RefreshElementalMaxHp();
		RaiseLoadoutChanged();
		return true;
	}

	// A parent leaving for a fusion: its timer, its node in the arena and any state of its own go with it,
	// as for a spell the Penknife cuts - but the loadout is rewritten by the caller, in one place.
	private void DropForFusion(SpellData parent)
	{
		spellFireTimers.Remove(parent.Id);
		RemovePersistentSpellInstance(parent);
		if (parent.Id.Equals("hunters_draw", StringComparison.OrdinalIgnoreCase))
			ResetHuntersDraw();
	}

	/// <summary>
	/// Every recipe the save knows - it owns both parents - or this run holds, as the ascension browser shows
	/// it. Reports and never decides: <see cref="ComboPreview.Offered"/> is the strip's own answer.
	/// </summary>
	public List<ComboPreview> BuildComboPreviews(SaveData save)
	{
		var previews = new List<ComboPreview>();
		// One locked thing on show at a time (Teachings.IsShown, Eric's Q8): no recipe is shown, sealed or not,
		// and no level 8 path carries the linked rings, until the Apotheosis has been learned. With the run's
		// flags rather than the live save's, so a lesson found in this run changes nothing until it ends.
		if (!ApotheosisLearned)
			return previews;
		foreach (ComboDefinition recipe in ComboCatalog.Fusions)
		{
			ComboStanding standing = EvaluateCombo(recipe);
			bool heldNow = standing.Taken || standing.FirstHeld != null || standing.SecondHeld != null;
			if (!ComboCatalog.IsKnown(recipe, save) && !heldNow)
				continue;
			SpellData template = ComboCatalog.Template(recipe);
			SpellData firstTemplate = ContentValidator.LoadAllSpells().FirstOrDefault(s => s.Id.Equals(recipe.First.SpellId, StringComparison.OrdinalIgnoreCase));
			var preview = new ComboPreview
			{
				Id = recipe.Id,
				ParentsName = $"{recipe.First.SpellName} + {recipe.Second.SpellName}",
				ParentIcon = firstTemplate?.Icon,
				Name = recipe.Name,
				Description = template?.Description ?? string.Empty,
				Icon = template?.Icon,
				// Met now - offered - reveals it too, as the Apotheosis is revealed by its first offer.
				Revealed = standing.Taken || standing.Ready || ComboCatalog.IsMet(recipe, save),
				Offered = standing.Ready,
				Taken = standing.Taken,
				Sealed = standing.Sealed,
			};
			preview.Names.Add((recipe.First.SpellId, recipe.First.AscensionId));
			preview.Names.Add((recipe.Second.SpellId, recipe.Second.AscensionId));
			// Shown but not yet learned: sealed with where it is learned, first, as the Ascension's is.
			if (!ConjunctionsLearned)
				preview.Requirements.Add(new AscensionRequirement { Kind = AscensionRequirementKind.Learned, Have = 0, Need = 1,
					Line = TeachingSealedLine(Teaching.Conjunctions) });
			preview.Requirements.Add(Ingredient(recipe.First, standing.Taken || standing.FirstMet, standing.FirstClosed));
			preview.Requirements.Add(Ingredient(recipe.Second, standing.Taken || standing.SecondMet, standing.SecondClosed));
			if (!standing.Taken && (standing.FirstOwn || standing.SecondOwn))
			{
				preview.Requirements.Add(new AscensionRequirement
				{
					Kind = AscensionRequirementKind.OwnSpell, Have = 0, Need = 1, Closed = true,
					SpellName = standing.FirstOwn ? recipe.First.SpellName : recipe.Second.SpellName,
				});
			}
			if (!standing.Taken && standing.Elements > ComboCatalog.MaxElements)
			{
				preview.Requirements.Add(new AscensionRequirement
				{
					Kind = AscensionRequirementKind.ElementCeiling, Have = standing.Elements, Need = ComboCatalog.MaxElements, Closed = true,
					SpellName = standing.ExtraElementBy,
				});
			}
			previews.Add(preview);
		}
		return previews;
	}

	private static AscensionRequirement Ingredient(ComboIngredient ingredient, bool met, bool closed) => new()
	{
		Kind = AscensionRequirementKind.Ingredient,
		Have = met ? 1 : 0,
		Need = 1,
		PathName = ingredient.AscensionName,
		SpellName = ingredient.SpellName,
		Closed = closed,
	};

	// --- what a combination needs of the player while it fires -----------------------------------------

	/// <summary>The damage multiplier a cast of this spell is handed now (attunement and the Split Lens folded in).</summary>
	public float SpellDamageMultiplierFor(SpellData spell) => GetSpellDamageMultiplier(spell);

	/// <summary>The enemy in reach with the most health left - a miniboss or elite first. Hunter's Mark's choice.</summary>
	public Enemy FindStrongestEnemyWithin(float range) => FindStrongestEnemy(range);

	/// <summary>A spell node's tint and scale, as every cast gives them (ApplyLegendaryVisual).</summary>
	public void ApplySpellVisual(CanvasItem visual, SpellData spell) => ApplyLegendaryVisual(visual, spell);

	// --- Sagittarius (scripts/Sagittarius.cs) ---------------------------------------------------------------

	private Sagittarius sagittarius;

	// The cooldown is the rain's cloudburst, as Starfall's is: it hands the node the stars for the next
	// interval, keeps it current, and casts nothing itself. The draw and the loose run in the node, every
	// physics frame, off the wizard's own movement. Maintained, never Fired: the loose has its own sound.
	private CastOutcome FireSagittarius(SpellData spell, float interval)
	{
		if (SagittariusScene == null || GetParent() is not Node2D arena)
			return CastOutcome.NoTarget;
		SpellData bow = spell.ComboParent(Sagittarius.BowSpellId);
		SpellData rain = spell.ComboParent(Sagittarius.RainSpellId);
		if (bow == null || rain == null)
		{
			GD.PushError("Player: Sagittarius carries no frozen parents (SpellData.ComboParents); it was not made by a fusion.");
			return CastOutcome.NoTarget;
		}
		if (sagittarius == null || !IsInstanceValid(sagittarius))
		{
			sagittarius = SagittariusScene.Instantiate<Sagittarius>();
			sagittarius.PlayerRef = this;
			arena.AddChild(TagSpell(sagittarius));
		}
		int stars = Math.Max(1, rain.GetProjectileCountAtLevel(rain.CurrentLevel) + AmountFor(spell));
		// The nock is Hunter's Draw's own cooldown, under every cooldown term the loop just put on the rain's.
		float scale = interval / MathF.Max(0.05f, spell.GetCooldownAtLevel(spell.CurrentLevel));
		float nock = MathF.Max(0.05f, bow.GetCooldownAtLevel(bow.CurrentLevel) * scale);
		sagittarius.Cloudburst(spell, bow, rain, stars, interval, nock, GetEffectiveAreaMultiplier(),
			GetSpellDamageMultiplier(spell), durationMultiplier, GetSpellVisualTint(rain, paintsOwnPath: true));
		return CastOutcome.Maintained;
	}
}
