using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>One wizard's Apotheosis: whose it is, on which spell, and the form it takes.</summary>
public sealed class ApotheosisDefinition
{
	/// <summary>The gate is a (character, spell) pair: a Frostweaver who maxes Cinderbreath gets nothing.</summary>
	public string CharacterId { get; init; } = string.Empty;
	public string SpellId { get; init; } = string.Empty;

	/// <summary>
	/// For a spell taken in a form (SpellData.Variants): the form this Apotheosis is the final form OF, so a
	/// wizard who opens on a spell with forms can have one per form, each with its own verb. Empty for a
	/// spell with no forms.
	/// </summary>
	public string VariantId { get; init; } = string.Empty;

	/// <summary>
	/// The wizard's element: the one his spell carries and the form deepens (+1, and nothing else). No
	/// longer a gate - the tier was dropped on 2026-11-02 - but it is still what the validator holds the
	/// form to, so the form can never name a new element.
	/// </summary>
	public Element Element { get; init; }

	/// <summary>The one verb this form owns and no other may (issue #59, the rule that keeps the set apart).</summary>
	public string Verb { get; init; } = string.Empty;

	public SpellEvolutionOption Form { get; init; }
}

/// <summary>
/// The single authority on Apotheoses (issue #59, #60, #61; .ai/decisions/2026-10-10-apotheosis.md).
/// </summary>
/// <remarks>
/// An Apotheosis is the final form of the spell a wizard starts with, offered in the Ascension strip
/// above the level-up cards once it holds on that spell (<see cref="SpellMeetsGate"/>): the wizard's own
/// starting spell, at its max level, ascended - ANY level 8 Ascension - and never combined with another
/// spell. And the player is at least <see cref="MinimumPlayerLevel"/>, which is what sets when. Taking it
/// spends the pick, and it is terminal.
///
/// Eric, 2026-11-02: "Apotheosis should require the spell to be at Max level, not combined with another
/// spell, and ascended." It replaced a covenant of a NAMED level 4 and level 8 path and the wizard's element
/// at tier 6: a run that took the other mutation, or ascended down the other branch, lost its form for good
/// at that click, and the tier asked for a whole book of one element on top. Now every route through the
/// spell's tree ends in its form, so no path is required and none can close it.
///
/// "Not combined": a Conjunction (ComboCatalog) takes both parents out of the loadout, and a wizard's own
/// spell is never an ingredient for him (ComboCatalog.Evaluate seals it, ComboSeal.OwnSpell) - so in today's
/// rules his spell is never fused. The gate still asks, so a change to that rule cannot hand a fused spell its
/// form, and a Conjunction's own spell is never eligible. RegressionChecks.ValidateApotheosisCatalog holds both.
///
/// Each form's behaviour lives in the spell's own script, switched on by
/// <see cref="SpellData.IsApotheosis"/>; this catalog is only the gate and the stat card. The
/// form's element weight is +1 of the wizard's own element and nothing else, so accepting it never
/// moves a tier down and never names a new element - and since any path may now come before it, no
/// level 4 or level 8 option on a wizard's spell may trade that element away (the validator again).
/// </remarks>
public static class ApotheosisCatalog
{
	public const int MilestoneLevel = 9;

	/// <summary>
	/// The player level below which no wizard is offered a form, whatever else holds - the one gate on the
	/// player rather than the spell, and the one that sets WHEN. A player level moves far less from run to
	/// run than the upgrade roll: the same build once took its form at 12.9 and at 17.2 minutes on two runs.
	/// History: 20, then 22 when the enemy cap doubled to 140, then 23 (at 22 the Pyromancer still took his
	/// under the floor). The 15-minute run's XP step of 45 was solved to put level 23 at about 8.4 minutes,
	/// over the 7.5 floor (.ai/run-pacing.md section 9).
	/// 21 since 2026-11-02, when the spell's gate became "max level, ascended, not combined". That gate alone
	/// is met the level-up after the Ascension (itself gated at <see cref="Player.AscensionMinimumPlayerLevel"/>),
	/// which the bot reached at 6.5-8.0 minutes - under the floor - so a player-level gate stays, now tuned
	/// to it: on that build the bot reached level 20 as early as 7.8 (on the floor), and at 21 it took its
	/// form at 8.6-10.8, where 23 had put it at 9.5-11.5. .ai/run-pacing.md section 11 has the runs.
	/// </summary>
	public const int MinimumPlayerLevel = 21;

	private static readonly ApotheosisDefinition[] Definitions =
	{
		new()
		{
			CharacterId = "pyromancer", SpellId = "cinderbreath",
			Element = Element.Fire, Verb = "burn",
			Form = Make("cinderbreath_cataclysm", "Cataclysm", Element.Fire,
				"Whatever the flame kills leaves a burning crater, and anything that dies in a crater opens another. The crowd is the fuel.",
				"Burn: the denser the wave, the further it spreads. Almost nothing against a lone boss.",
				new Color(1.0f, 0.45f, 0.12f), NoVisualScale),
		},
		new()
		{
			CharacterId = "frostweaver", SpellId = "cone_of_cold",
			Element = Element.Ice, Verb = "stop",
			Form = Make("cone_of_cold_long_winter", "The Long Winter", Element.Ice,
				"The cone opens into a full ring that freezes all it touches. The frozen shatter when they die, freezing their neighbours.",
				"Stop: measured in what does not reach you. Bosses take a hard slow instead of a root.",
				new Color(0.70f, 0.92f, 1.0f), NoVisualScale),
		},
		new()
		{
			CharacterId = "stormcaller", SpellId = "chain_lightning",
			Element = Element.Lightning, Verb = "link",
			Form = Make("chain_lightning_lattice", "The Lattice", Element.Lightning,
				"The chain stops ending. Everything it strikes stays linked for a few seconds, and the web keeps discharging along its own edges.",
				"Link: damage follows how connected the crowd is. Herd them together.",
				new Color(1.0f, 0.95f, 0.45f)),
		},
		new()
		{
			CharacterId = "geomancer", SpellId = "obsidian_spike",
			Element = Element.Earth, Verb = "hold",
			Form = Make("obsidian_spike_sundering", "Sundering", Element.Earth,
				"Each cast shoves everything near you back and raises a broken ring of obsidian around you. Inside it you take less from every hit.",
				"Hold: the only form that edits the arena. Keep re-siting the ring as the wave moves.",
				new Color(0.55f, 0.45f, 0.70f), NoVisualScale),
		},
		new()
		{
			CharacterId = "tituba", SpellId = "riptide",
			Element = Element.Water, Verb = "flood",
			Form = Make("riptide_spring_tide", "Spring Tide", Element.Water,
				"Each wave floods the ground behind it, and the current drags whatever stands in it and will not let it walk.",
				"Flood: aim the line, then let the water carry them where you want them.",
				new Color(0.40f, 0.72f, 1.0f)),
		},
		new()
		{
			CharacterId = "vainamoinen", SpellId = "bramble_seed",
			Element = Element.Grass, Verb = "swarm",
			Form = Make("bramble_seed_wildwood", "Wildwood", Element.Grass,
				"A patch that finishes growing throws three seeds around itself, so every planting becomes a thicket.",
				"Swarm: the ground you stand your crowd on is ground that fights for you.",
				new Color(0.55f, 0.90f, 0.40f), NoVisualScale),
		},
		// John Dee, the collector, freed by opening chests. "Pull" was on the ADR's unclaimed list and
		// Hollow Star is the one spell in the roster that is ABOUT pulling, so the verb was his to take.
		// The Spirit Mirror is Dee's obsidian scrying glass (the British Museum's): a well that drags
		// the swarm from half again as far and, when it closes, draws every orb and flask in its reach
		// to him.
		new()
		{
			CharacterId = "john_dee", SpellId = "hollow_star",
			Element = Element.Darkness, Verb = "pull",
			Form = Make("hollow_star_spirit_mirror", "The Spirit Mirror", Element.Darkness,
				"The well opens half again as wide and drags harder. Its collapse pulls every orb, potion and flask in reach to you.",
				"Pull: the knot comes to your spells, and what it drops comes to your hands.",
				new Color(0.62f, 0.42f, 0.92f), NoVisualScale),
		},
		// Laelaps, the hound fated to catch whatever it hunted, freed by the coursing deed. "Reflect" was on
		// the ADR's unclaimed list and a whirlwind is the thing in the roster that can throw a missile back;
		// the hound that always caught its quarry catches what is thrown at him and returns it
		// (OrbitingBlade.ReflectBolts). No visual scale: the ring draws itself out of the radius it hits.
		new()
		{
			CharacterId = "laelaps", SpellId = "cyclone_slash",
			Element = Element.Wind, Verb = "reflect",
			Form = Make(OrbitingBlade.EndlessChaseId, "The Endless Chase", Element.Wind,
				"The whirlwind catches enemy bolts and throws them back at the nearest enemy, three times as hard as a blade.",
				"Reflect: the archers feed it. Run among the shooters and let them aim at you.",
				new Color(0.62f, 0.95f, 0.88f), NoVisualScale),
		},
		// Abe no Seimei, the onmyoji sealed in his own five-pointed seal in the Mystic Ruins (.ai/chapter-wizards.md
		// 4.5). "Banish" was on the ADR's unclaimed list, and an exorcist's seal is what it is for: a ward that
		// burns a body low seals it where it stands, harmless, and when the ward goes out everything it sealed is
		// gone (KindledWard.FivePointedSealId). Not an execute - the sealed stop fighting at once and the kills land
		// together; elites take a heavy blow instead and bosses are never sealed. No visual scale: the ward is
		// drawn out of the reach it burns.
		new()
		{
			CharacterId = "seimei", SpellId = "kindled_ward",
			Element = Element.Light, Verb = "banish",
			Form = Make(KindledWard.FivePointedSealId, "The Five-Pointed Seal", Element.Light,
				"A ward that burns an enemy below a quarter of its health seals it there, harmless. When the ward goes out, all it sealed is gone.",
				"Banish: sealed now, gone later. Elites take a heavy blow instead; bosses are never sealed.",
				new Color(1.0f, 0.94f, 0.66f), NoVisualScale),
		},
		// Wayland the Smith, chained to his own anvil in the Emberdeep (.ai/chapter-wizards.md 4.7). "Temper" was
		// nobody's: a smith hardens iron by working it, and the stakes he forged harden under every blow the crowd
		// lands on them, then ring and cut all along the line (IronPalisade.TemperedLineId). Nothing moves, holds
		// or stops anything (hold is the Geomancer's), so a boss takes it as any cut. Metal, Iron Palisade's own;
		// its one branch, Lightning Rod, adds Lightning and trades nothing away. No visual scale: the wall is
		// drawn out of its own length (Player.RaiseIronPalisade drops any scale).
		new()
		{
			CharacterId = "wayland", SpellId = "iron_palisade",
			Element = Element.Metal, Verb = "temper",
			Form = Make(IronPalisade.TemperedLineId, "The Tempered Line", Element.Metal,
				"Every blow the stakes take hardens the line, +5% a blow to +100%. Struck enough, it rings and cuts all along it.",
				"Temper: the more they strike, the harder. Raise it where the crowd leans hardest.",
				new Color(0.92f, 0.95f, 0.98f), NoVisualScale),
		},
		// The Cumaean Sibyl, freed at the nine mouths in the Sunken Cave (.ai/chapter-wizards.md 4.3). "Echo" was
		// nobody's: her cave had a hundred mouths and spoke with a hundred voices, and Arcane Explosion is the
		// one spell set off by the others - so each burst sounds them again, from where she stands, at 40%
		// (ArcaneExplosion.HundredMouthsId, Player.Echo.cs). Nothing is pulled, linked or doubled behind her:
		// it is her own loadout, heard twice. Arcane, the spell's only element; no card on it trades Arcane
		// away (Static Nova adds Lightning beside it). No visual scale: the blast is drawn from its range.
		new()
		{
			CharacterId = "sibyl", SpellId = "arcane_explosion",
			Element = Element.Arcane, Verb = "echo",
			Form = Make(ArcaneExplosion.HundredMouthsId, "The Hundred Mouths", Element.Arcane,
				"Each burst sounds your other spells again from where you stand, at 40% strength.",
				"Echo: everything you cast, twice. An echo is never echoed, and never charges the burst.",
				new Color(0.91f, 0.75f, 1.0f), NoVisualScale),
		},
		// Baba Yaga, freed by running her walking hut down in the Enchanted Forest (.ai/chapter-wizards.md 4.1). "Feed"
		// was nobody's: her hut stands behind a fence of bones, and the vine she calls up out of the ground eats what it
		// cuts down and grows on it - longer and wider for the rest of the run, to a cap (ThornVine.HungryHedgeId,
		// Player.HungryHedge.cs). Nothing is summoned (swarm), pulled (pull) or spread (burn): one vine, growing. Grass,
		// the one of Thorned Vine's two the form deepens; no card on Thorned Vine trades Grass away (Barbed Lash deepens
		// it, Thicket adds Poison beside it). No visual scale: the vine is drawn out of its own length and arc.
		new()
		{
			CharacterId = "baba_yaga", SpellId = "thorn_vine",
			Element = Element.Grass, Verb = "feed",
			Form = Make(ThornVine.HungryHedgeId, "The Hungry Hedge", Element.Grass,
				"Every enemy the vine kills feeds it: it sweeps further and wider for the rest of the run, to +60% reach and +40% arc.",
				"Feed: feed it early. It remembers. What it eats never comes off it.",
				new Color(0.66f, 0.92f, 0.42f), NoVisualScale),
		},
		// The dark wizard, taken on the hardest road (.ai/dark-wizard-playable.md). "Take" was nobody's, and it is
		// his verb in the fight and in the story: whatever his lanes kill gives up its light too, and it comes
		// home to be caught in its turn (TakingLanes; TakenLight.MaxInAir in the air at most). Nothing moves the
		// swarm (pull) and nothing is summoned (swarm). Darkness, the one of his two the form deepens; no card on
		// The Taking trades it away. No visual scale: the hand and the lanes draw their own size.
		new()
		{
			CharacterId = "dark_wizard", SpellId = TheTaking.SpellId,
			Element = Element.Darkness, Verb = "take",
			Form = Make(TheTaking.AllHeTookId, "All He Took", Element.Darkness,
				"Whatever your lanes kill gives up its light too. Catch it, and the lanes cross again.",
				"Take: every kill on a lane is another light coming home. Stand where they fall.",
				new Color(0.78f, 0.62f, 1.0f), NoVisualScale),
		},
		// Merlin, the old enchanter (.ai/merlin.md), who lives his life backwards in the old stories.
		// "Rewind" was held for him (.ai/decisions/2026-10-10-apotheosis.md, .ai/chapter-wizards.md): each time the orb
		// finishes its star it flies it again backwards and the ward's hour turns back, so his circle stands for as long
		// as he holds it (OrbOfAges.OnceAndFutureId). Protection as a place that does not fall, not a heal. Light, the
		// one of his two the form deepens; no card on Orb of Ages trades it away. No visual scale: the ward and the orb
		// draw their own size.
		new()
		{
			CharacterId = "merlin", SpellId = OrbOfAges.SpellId,
			Element = Element.Light, Verb = "rewind",
			Form = Make(OrbOfAges.OnceAndFutureId, "The Once and Future", Element.Light,
				"When the orb finishes its star it flies it again backwards. Your ward stands as long as you stay inside it.",
				"Rewind: the ward's hour turns back. Hold your ground and it never falls.",
				new Color(1.0f, 0.96f, 0.78f), NoVisualScale),
		},
		// The Archmage (.ai/protagonist.md): the title figure, the player, the one the dark wizard missed - and Magic Missile
		// is the dart that never misses. "Count" was nobody's: he was the one left out of a count, and his final form
		// leaves no one out of his - every fourth volley is a dart at every enemy in reach, one each (MagicMissile.
		// NotOneMissedId). A targeting verb: it changes who is struck, never what a strike does, so nothing is pulled,
		// linked, echoed or doubled, and a boss is struck once like anyone. Arcane, the spell's only element since
		// 2026-11-02; no card on it trades Arcane away (Twin Volley and Overcharge add Lightning beside it). Scale reaches
		// the dart (EvolutionStatReaders), so the form keeps the usual lift.
		new()
		{
			CharacterId = "archmage", SpellId = "magic_missile",
			Element = Element.Arcane, Verb = "count",
			Form = Make(MagicMissile.NotOneMissedId, "Not One Missed", Element.Arcane,
				$"Every {MagicMissile.CountEvery}th volley is a count: a missile at every enemy in reach, up to {MagicMissile.CountDarts}, one each.",
				"Count: the bigger the crowd, the bigger the count. Nobody in reach is left out.",
				new Color(0.86f, 0.80f, 1.0f)),
		},
		// Morgan le Fay, freed from her barge under the Last Bridge (.ai/chapter-wizards.md 4.9). "Double" was nobody's:
		// the mirage over the strait bears her name, and hers is of the player - his own figure walking beside him the
		// other way, every cast of the runes let go again from its hand, behind him (SleetRunes.FataMorganaId,
		// FataMorganaMirage, Player.Mirage.cs) - and what is near it chases it. Not an echo (the Sibyl's: her other
		// spells again, weaker, from where she stands) and not a pull (nothing is dragged): the same runes at the same
		// strength from a second origin, and a lure no boss answers. Ice, the one of its two the form deepens; no card
		// on Sleet Runes trades Ice away (Great Rune adds it, Turning Gale adds Wind). No visual scale: the rune sheet
		// is drawn at one size.
		new()
		{
			CharacterId = "morgan", SpellId = "sleet_runes",
			Element = Element.Ice, Verb = "double",
			Form = Make(SleetRunes.FataMorganaId, "Fata Morgana", Element.Ice,
				"A mirage of you walks beside you facing the other way and casts behind you. Enemies near it chase it.",
				"Double: half of them chase a ghost. Every cast leaves both hands; no boss is fooled.",
				new Color(0.78f, 0.96f, 1.0f), NoVisualScale),
		},
	};

	// A form's visual lift is a 15% larger spell, but only where the spell's node keeps a visual scale.
	// Cinderbreath's emitter draws nothing (its tongues are the arena's), and Cone of Cold, Bramble Seed,
	// Hollow Star and Obsidian Spike (since 2026-10-28) draw themselves out of the radius they hit and
	// drop any scale, so on those five the 1.15 reached nothing (EvolutionStatReaders: no Scale) and is
	// left off.
	private const float FormVisualScale = 1.15f;
	private const float NoVisualScale = 1.0f;

	private static SpellEvolutionOption Make(string id, string name, Element element, string description, string synergy, Color tint,
		float scale = FormVisualScale) => new()
	{
		Id = id,
		DisplayName = name,
		Description = description,
		MilestoneLevel = MilestoneLevel,
		SynergyTag = "Apotheosis",
		SynergyDescription = synergy,
		// A modest stat lift: the form's behaviour is the reward, not a bigger number.
		DamageMultiplier = 1.25f,
		BonusElementWeights = new() { { element.ToString(), 1 } },
		ModulateColor = tint,
		ScaleMultiplier = scale,
		VisualTag = "Apotheosis",
	};

	public static IReadOnlyList<ApotheosisDefinition> All => Definitions;

	public static ApotheosisDefinition ForCharacter(string characterId) =>
		Definitions.FirstOrDefault(d => d.CharacterId.Equals(characterId ?? string.Empty, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// This wizard's Apotheosis for this held spell: the one naming the spell and - for a spell taken in a
	/// form - that form (ApotheosisDefinition.VariantId). Null when the wizard has none for it.
	/// </summary>
	public static ApotheosisDefinition ForSpell(string characterId, SpellData spell) =>
		spell == null ? null : Definitions.FirstOrDefault(d =>
			d.CharacterId.Equals(characterId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
			&& d.SpellId.Equals(spell.Id, StringComparison.OrdinalIgnoreCase)
			&& (string.IsNullOrEmpty(d.VariantId) || d.VariantId.Equals(spell.SelectedVariantId ?? string.Empty, StringComparison.OrdinalIgnoreCase)));

	public static ApotheosisDefinition ForForm(string formId) =>
		Definitions.FirstOrDefault(d => d.Form.Id.Equals(formId ?? string.Empty, StringComparison.OrdinalIgnoreCase));

	/// <summary>The spell has taken a level 8 Ascension - any of them (Eric, 2026-11-02: no path is named).</summary>
	public static bool IsAscended(SpellData spell) => spell != null && !string.IsNullOrEmpty(spell.SelectedLevel8EvolutionId);

	/// <summary>
	/// A Conjunction's own spell (ComboCatalog), or anything carrying fused parents (SpellData.ComboParents): two
	/// spells made one. Never anyone's to transform - it is terminal, and it is no wizard's starting spell.
	/// </summary>
	public static bool IsCombined(SpellData spell) =>
		spell != null && (spell.IsCombination || (spell.ComboParents?.Length ?? 0) > 0);

	/// <summary>
	/// Whether <paramref name="spell"/> meets the spell's half of the gate right now: this wizard's own spell (in
	/// the definition's form), not yet transformed, at its max level, ascended down any branch, and not combined
	/// with another spell - neither a Conjunction itself nor a spell that went into one. Only the run knows the
	/// second (<paramref name="fusedAway"/>, Player.WasFusedAway), since a fused parent leaves the loadout. The
	/// player's half, <see cref="MinimumPlayerLevel"/>, is the caller's (Player.TryBuildApotheosisOffer).
	/// </summary>
	/// <remarks>
	/// ONE EXCEPTION, the wizard who OPENS on a Conjunction (<see cref="OnStartingConjunction"/>: the dark wizard's
	/// The Taking, ComboDefinition.StartsFusedFor). His spell is combined from the first moment and has no
	/// Ascension of its own to take - a Conjunction never ascends - so for that one spell "ascended" is its max
	/// level (Eric, 2026-11-02: it starts at level 1 and levels up), and "not combined" means not fused again,
	/// which a Conjunction never is. Anyone else who holds it made it by fusing, and it is no starting spell of
	/// theirs, so it is never offered a form (the CharacterId check in <see cref="ForSpell"/>).
	/// </remarks>
	public static bool SpellMeetsGate(ApotheosisDefinition definition, SpellData spell, bool fusedAway)
	{
		if (definition == null || spell == null || spell.IsApotheosis || fusedAway
			|| !spell.Id.Equals(definition.SpellId, StringComparison.OrdinalIgnoreCase))
			return false;
		if (OnStartingConjunction(definition))
			return spell.CurrentLevel >= spell.MaxLevel;
		return !IsCombined(spell)
			&& (string.IsNullOrEmpty(definition.VariantId)
				|| definition.VariantId.Equals(spell.SelectedVariantId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			&& spell.CurrentLevel >= spell.MaxLevel
			&& IsAscended(spell);
	}

	/// <summary>
	/// True for a form whose spell is a Conjunction its wizard opens on, already made (the dark wizard's The
	/// Taking): its gate is max level alone (<see cref="SpellMeetsGate"/>).
	/// </summary>
	public static bool OnStartingConjunction(ApotheosisDefinition definition) =>
		definition != null && ComboCatalog.StartsFused(ComboCatalog.ForId(definition.SpellId), definition.CharacterId);
}
