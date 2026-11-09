using Godot;
using System;
using System.Linq;
using TheOneHeMissed.scripts;

// MA LIANG, the Court's second prisoner (.ai/chapter-wizards.md 4.8): the two things of his that live on the player.
//
// PAINT WHAT IS NEEDED, his passive ("paint_flask"). The boy of the tale painted for the poor what each of them needed
// - a plough, a bucket, a lamp - and the brush made it real. Once a run, the first time a blow leaves him below a
// quarter of his health and still standing, a flask is painted at his feet: ink strokes of his light drawn on the
// ground beside him (PaintedFlask), and where the last stroke closes it is a real flask - the ordinary HealthPickup,
// 8 HP, which comes to him as any flask does. One flask's worth, once: on the healing ledger
// (.ai/difficulty-curve.md 5.1) it is the size of the rarest drop in the game, and no more. It answers being hurt
// (after the shield, on the HP that actually left, step 8 of TakeDamage), never a blow that kills: a single hit
// from above a quarter to nothing paints nothing, as no flask would have saved him.
//
// HE OPENS ON THE CROW. He opens on the Familiar, a spell with forms (SpellData.Variants), in the crow's form every run:
// the form his Apotheosis, The Painted Door, belongs to (CharacterData.StartingFormId, from ApotheosisCatalog), as Nimue
// opens on the Sword - and his crow is painted in ink (Familiar.PaintsInInk). Until 2026-11-09 he chose dog, crow or cat
// on the run's first screen, and as the dog or the cat never met his final form (Eric: "lets just not let him choose a
// different familiar than the crow then - he automatically starts with a crow familiar", .ai/character-fit.md section
// 5, row 21).
//
// THE OPENING PAGE below (OwesOpeningForm and the rest, and Node2DGame.OpeningForm.cs) was built for that choice and is
// kept: it still works for any wizard whose CharacterData.ChoosesStartingForm is set, and none is now. The spell is
// OWED until such a wizard takes it: nothing is equipped before the page answers, and the tree is paused while it is up.
public partial class Player
{
	/// <summary>Paint What Is Needed paints its flask once a run, below this share of his health.</summary>
	public const float PaintFlaskHealthFraction = 0.25f;

	/// <summary>How far from his feet the flask is painted: beside him, not under him, so the painting is seen.</summary>
	public const float PaintFlaskDistance = 44f;

	private bool characterPaintsFlask;
	private bool paintedFlaskSpent;

	/// <summary>Run seconds at which Paint What Is Needed painted its flask, or -1 while it has not. For the harness.</summary>
	public float FlaskPaintedAtSeconds { get; private set; } = -1f;

	/// <summary>
	/// The spell this run's wizard opens on whose form is still to be chosen (a wizard with ChoosesStartingForm; none
	/// since Ma Liang opens on the crow), or empty once it is held. Node2DGame opens the page while this is set.
	/// </summary>
	public string OpeningFormSpellId { get; private set; } = string.Empty;

	/// <summary>True while the wizard's opening spell waits for its form (the run's first screen).</summary>
	public bool OwesOpeningForm => !string.IsNullOrEmpty(OpeningFormSpellId)
		&& !equippedSpells.Any(s => s != null && s.Id.Equals(OpeningFormSpellId, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Whether <paramref name="character"/> chooses the form of the spell he opens on at the start of each run: a spell
	/// with forms, and a wizard marked to choose it (CharacterData.ChoosesStartingForm) - every other wizard on a spell
	/// with forms opens in his Apotheosis's form (Nimue's Sword, CharacterData.StartingFormId). Never the Test Wizard,
	/// whose picker names one on the character screen. Read by the character select too, so the card shows the spell
	/// and not one form of it.
	/// </summary>
	public static bool ChoosesOpeningForm(CharacterData character)
	{
		SpellData spell = character?.StartingSpellResource;
		return spell != null && spell.HasVariants && character.ChoosesStartingForm
			&& !character.Id.Equals("test_wizard", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The card the opening page is made from: the spell as a level-up deals it, with a card per form.</summary>
	public LevelUpOption BuildOpeningFormOffer() => OwesOpeningForm ? BuildNewSpellOffer(OpeningFormSpellId) : null;

	/// <summary>
	/// The opening page answered: the spell taken in the form it names ("familiar:familiar_crow"), at level 1 and
	/// Legendary if the wizard starts so. A selection with no form, or one for another spell, is refused - the page
	/// stays owed - so the run never starts with a form nobody chose.
	/// </summary>
	public bool TakeOpeningForm(string selection)
	{
		(string spellId, string variantId) = SpellVariants.Split(selection);
		if (!OwesOpeningForm || string.IsNullOrEmpty(variantId)
			|| !spellId.Equals(OpeningFormSpellId, StringComparison.OrdinalIgnoreCase))
		{
			GD.PrintErr($"Player: the opening page sent '{selection}', not a form of '{OpeningFormSpellId}'.");
			return false;
		}
		if (!TryAddOrLevelSpell(selection, openingFormLegendary))
			return false;
		GD.Print($"Opening form taken: {selection}");
		return true;
	}

	private bool openingFormLegendary;

	// ApplySelectedCharacter's half: the spell is owed, not equipped.
	private void OweOpeningForm(string spellId, bool legendary)
	{
		OpeningFormSpellId = spellId ?? string.Empty;
		openingFormLegendary = legendary;
	}

	// TakeDamage step 8's half, after HP has left: below a quarter and still standing, once a run, a flask is painted.
	private void MaybePaintFlask()
	{
		if (!characterPaintsFlask || paintedFlaskSpent || CurrentHP <= 0 || CurrentHP >= MaxHP * PaintFlaskHealthFraction)
			return;
		paintedFlaskSpent = true;
		FlaskPaintedAtSeconds = Node2DGame.Current?.RunSeconds ?? 0f;
		Node2DGame game = Node2DGame.Current;
		Vector2 feet = FootPosition;
		// To the side he is facing away from the last hit is unknowable here; beside him, on whichever side has ground.
		Vector2 at = feet + new Vector2(PaintFlaskDistance, 0f);
		if (game != null)
		{
			Vector2 left = game.ReachablePoint(feet - new Vector2(PaintFlaskDistance, 0f));
			Vector2 right = game.ReachablePoint(at);
			at = right.DistanceTo(feet + new Vector2(PaintFlaskDistance, 0f)) <= left.DistanceTo(feet - new Vector2(PaintFlaskDistance, 0f)) ? right : left;
		}
		Node parent = game ?? GetParent();
		if (parent == null)
			return;
		parent.CallDeferred(Node.MethodName.AddChild, new PaintedFlask { Name = "PaintedFlask", Position = at });
		GD.Print($"Paint What Is Needed: a flask painted at {FlaskPaintedAtSeconds:0.0} s, {CurrentHP}/{MaxHP} HP.");
	}
}
