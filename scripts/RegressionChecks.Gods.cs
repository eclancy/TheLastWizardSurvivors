using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE GOD PLUMBING (.ai/second-half.md 3, 6 and 9, piece F1). A god of the far shore is a character who opens on a
// Conjunction, made, at level 1 of its own table, as the dark wizard opens on The Taking; is rekindled by its seat's
// warden; and is worded, hidden and drawn as a god rather than a wizard. F1's "done when" is that a god can be added
// as DATA - a CharacterData row and catalog lines, no new code - so what a build cannot see is checked here three ways:
//   - ValidateOpeners: every opener (ComboDefinition.StartsFusedFor) is a real character who starts with it, levels it
//     1 to 8 off its own table with a line at every level, and alone may be offered the form on it;
//   - ValidateGods: every god in UnlockCatalog (FromGod) has every part a god needs, and nothing counts it that must not;
//   - ValidateGodsAsData: a god built outside the catalogs, from data alone, passes those same checks - and each part
//     taken away or got wrong is refused, so the checks are shown to fire on every run of the validator, not once.
// Every line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	// --- the openers -----------------------------------------------------------------------------------------------

	// Every Conjunction a character opens on, and every character who opens on one.
	private static void ValidateOpeners(List<string> warnings)
	{
		List<SpellData> spells = ContentValidator.LoadAllSpells().ToList();
		List<CharacterData> roster = CharacterRoster.GetAll();
		var openedBy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (ComboDefinition recipe in ComboCatalog.Openers)
		{
			string who = recipe.StartsFusedFor;
			if (openedBy.TryGetValue(who, out string other))
				warnings.Add($"Openers: '{who}' opens on both '{other}' and '{recipe.Id}'; a character opens on one spell.");
			else
				openedBy[who] = recipe.Id;
			if (UnlockCatalog.GetCharacter(who) == null)
				warnings.Add($"Openers: '{recipe.Id}' is opened on by '{who}', who has no UnlockCatalog entry, so nobody can ever play it.");

			SpellData combo = spells.FirstOrDefault(s => s.Id.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase));
			CharacterData opener = roster.FirstOrDefault(c => c != null && c.Id.Equals(who, StringComparison.OrdinalIgnoreCase));
			ApotheosisDefinition form = ApotheosisCatalog.All.FirstOrDefault(d => d.SpellId.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase)
				&& d.CharacterId.Equals(who, StringComparison.OrdinalIgnoreCase));
			ValidateOpener($"Combination '{recipe.Id}'", recipe, combo, opener, form, warnings);

			// Whoever else fuses it holds it at its last level and it is no starting spell of theirs: never offered his form.
			if (combo == null)
				continue;
			foreach (CharacterData character in roster.Where(c => c != null && !c.Id.Equals(who, StringComparison.OrdinalIgnoreCase)))
			{
				if (ApotheosisCatalog.ForSpell(character.Id, combo) != null)
					warnings.Add($"Combination '{recipe.Id}': '{character.Id}', who would only ever fuse it, is offered a form on it; only '{who}' may be.");
			}
		}

		// The other direction: a character whose starting spell is a Conjunction must be its opener. Otherwise his first
		// frame asks to FUSE it (Player.ApplySelectedCharacter -> TryAddOrLevelSpell -> TryFuse), which is refused, and he
		// walks into the run holding nothing.
		foreach (CharacterData character in roster)
		{
			string id = character?.StartingSpellResource?.Id;
			if (ComboCatalog.ForId(id) is ComboDefinition recipe && !recipe.IsRite && !ComboCatalog.StartsFused(recipe, character.Id))
				warnings.Add($"Openers: '{character.Id}' starts with the Conjunction '{id}', which does not name him (ComboDefinition.StartsFusedFor); his run would try to fuse it, be refused, and hold nothing.");
		}
	}

	// One opener, from its parts - shared by the catalog's openers and the scratch god, so both face the same rule: the
	// character starts with it; it levels 1 to the Ascension level, a line at every level, and the cards take it all the
	// way (it has no Ascension to wait for); it names at most three elements; and its form, if it has one, is its
	// opener's, deepens an element it carries, and opens at its max level alone - shut a level short.
	private static void ValidateOpener(string label, ComboDefinition recipe, SpellData combo, CharacterData opener,
		ApotheosisDefinition form, List<string> warnings)
	{
		string who = recipe.StartsFusedFor;
		if (recipe.IsRite)
			warnings.Add($"{label} is the Sovereign Rite, the capstone that silences every other spell; it is made late, and nobody may open on it.");
		if (opener == null || opener.StartingSpellResource == null
			|| !opener.StartingSpellResource.Id.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase))
			warnings.Add($"{label} is opened on by '{who}', who does not start with it.");
		if (combo == null)
		{
			warnings.Add($"{label} is opened on by '{who}' and has no SpellData in ContentValidator.SpellResourcePaths.");
			return;
		}
		// Its own table (.ai/second-half.md 3.4): its opener never held the parents a fused copy freezes.
		if (combo.MaxLevel != Player.AscensionSpellLevel)
			warnings.Add($"{label} levels to {combo.MaxLevel}; a Conjunction a character opens on levels to {Player.AscensionSpellLevel} off its own table, as every spell does.");
		var levels = new HashSet<int>((combo.LevelUpgrades ?? new Godot.Collections.Array<SpellLevelUpgrade>()).Where(u => u != null).Select(u => u.Level));
		for (int level = 2; level <= combo.MaxLevel; level++)
		{
			if (!levels.Contains(level))
				warnings.Add($"{label} has no level-up line for level {level}; a card that changes nothing is a dead pick.");
		}
		if (Player.CardLevelCap(combo) != combo.MaxLevel)
			warnings.Add($"{label} stops at level {Player.CardLevelCap(combo)} on cards; with no Ascension of its own, the cards must take it to {combo.MaxLevel}.");
		if ((combo.ElementWeights?.Count ?? 0) > ComboCatalog.MaxElements)
			warnings.Add($"{label} carries {combo.ElementWeights.Count} elements; a Conjunction may name {ComboCatalog.MaxElements}, never more.");

		if (form == null)
			return;
		if (!form.CharacterId.Equals(who ?? string.Empty, StringComparison.OrdinalIgnoreCase)
			|| !form.SpellId.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase))
			warnings.Add($"{label}: the form on it is '{form.CharacterId}'s on '{form.SpellId}'; only its opener '{who}' may have one, on it.");
		if (combo.ElementWeights == null || !combo.ElementWeights.ContainsKey(form.Element.ToString()))
			warnings.Add($"{label}: '{who}'s form deepens {form.Element}, which the Conjunction does not carry; a form never names a new element.");
		var mine = (SpellData)combo.Duplicate(true);
		mine.CurrentLevel = mine.MaxLevel;
		if (!ApotheosisCatalog.SpellMeetsGate(form, mine, fusedAway: false, recipe))
			warnings.Add($"{label}: '{who}'s form stays shut on it at level {mine.MaxLevel}; an opener's gate is its max level alone.");
		mine.CurrentLevel = mine.MaxLevel - 1;
		if (ApotheosisCatalog.SpellMeetsGate(form, mine, fusedAway: false, recipe))
			warnings.Add($"{label}: '{who}'s form opens below its max level.");
	}

	// --- the gods --------------------------------------------------------------------------------------------------

	// Everything one god is made of, read from the catalogs (GodPartsFor) or built by hand (the scratch god).
	private sealed class GodParts
	{
		public UnlockDefinition Unlock;
		public CharacterData Character;
		public ComboDefinition Recipe;
		public SpellData Template;
		public ApotheosisDefinition Form;
		public QuestLog.Prisoner Prisoner;
		public bool HasReel;
		public string ReelGround = string.Empty;
		public StageDefinition Seat;
		public AchievementDefinition SeatDeed;
	}

	private static GodParts GodPartsFor(UnlockDefinition god)
	{
		ComboDefinition recipe = ComboCatalog.OpenerFor(god.Id);
		return new GodParts
		{
			Unlock = god,
			Character = CharacterRoster.GetAll().FirstOrDefault(c => c != null && c.Id.Equals(god.Id, StringComparison.OrdinalIgnoreCase)),
			Recipe = recipe,
			Template = ComboCatalog.Template(recipe),
			Form = ApotheosisCatalog.ForCharacter(god.Id),
			Prisoner = QuestLog.PrisonerOf(god.Id),
			HasReel = Freeings.HasReel(god.Id),
			ReelGround = Freeings.GroundFor(god.Id),
			Seat = StageCatalog.GetById(god.SeatStageId),
			SeatDeed = AchievementDefinitions.GetById(AchievementDefinitions.SeatDeedId(god.SeatStageId)),
		};
	}

	private static void ValidateGods(List<string> warnings)
	{
		var seats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var fresh = new SaveData();
		foreach (UnlockDefinition god in UnlockCatalog.Gods)
		{
			string label = $"God '{god.Id}'";
			if (seats.TryGetValue(god.SeatStageId, out string twin))
				warnings.Add($"{label} and '{twin}' both hold the seat '{god.SeatStageId}'; a seat's warden rekindles one god.");
			else
				seats[god.SeatStageId] = god.Id;

			// The parts. The opener's own rule is ValidateOpeners', run on every opener already.
			ValidateGod(label, GodPartsFor(god), checkOpener: false, warnings);

			// The plumbing every god rides on, read by id - its words, its reel's ground, its card, its place outside
			// every gate. None of it is written per god: each is asked of UnlockCatalog.IsGod.
			if (!QuestLog.FreeTitle(god.Id).StartsWith("Rekindle ", StringComparison.Ordinal)
				|| !QuestLog.FreedTitle(god.Id).EndsWith(" is rekindled", StringComparison.Ordinal)
				|| !QuestLog.PaidLine(god.Id).StartsWith("God rekindled", StringComparison.Ordinal)
				|| !QuestLog.HiddenRewardLine(god.Id).StartsWith(QuestLog.RekindlesVerb, StringComparison.Ordinal))
				warnings.Add($"{label} is worded as a wizard somewhere (QuestLog's freeing words); a god is rekindled, never freed.");
			if (!Freeings.GroundFor(god.Id).Equals(Freeings.RekindledGround, StringComparison.Ordinal))
				warnings.Add($"{label}'s reel stands on the heath; a god's rekindling stands on the far shore's ground.");
			if (!CharacterSelection.IsHiddenUntilRevealed(fresh, god.Id))
				warnings.Add($"{label} is on a new save's character select; nothing of the far shore is on show before its gate (.ai/second-half.md 6.2).");
			if (!CharacterVisuals.GetCharacterTint(god.Id).IsEqualApprox(Colors.White))
				warnings.Add($"{label} is tinted on the character select and in the run; a god with art of its own renders untinted.");
			if (UnlockCatalog.AllWizardIds.Contains(god.Id, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"{label} is counted as one of the imprisoned wizards; a god is outside every gate (UnlockCatalog.FromGod).");
			foreach (StageDefinition gate in StageCatalog.GateChapters)
			{
				if (GlobalStatsManager.CountsTowardGate(god.Id, gate))
					warnings.Add($"{label} is counted by {gate.DisplayName}'s gate, which no god can be behind.");
			}
			if (GlobalStatsManager.CountsTowardFarShore(god.Id))
				warnings.Add($"{label} is counted by the far shore's own gate, which would then wait on itself.");
		}

		// Every seat deed: on a far chapter, named for it, and paying the god seated there (or nobody, while no god is).
		foreach (AchievementDefinition deed in AchievementDefinitions.All.Where(d =>
			d.Id.StartsWith(AchievementDefinitions.SeatDeedPrefix, StringComparison.OrdinalIgnoreCase)))
		{
			StageDefinition stage = StageCatalog.GetById(deed.StageId);
			if (!StageCatalog.IsOnFarShore(stage) || !deed.Id.Equals(AchievementDefinitions.SeatDeedId(deed.StageId), StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Gods: the deed '{deed.Id}' is a seat's by its id but stands in '{deed.StageId}'; a seat deed is SeatClear's, on its own far chapter.");
			string seated = UnlockCatalog.GodSeatedAt(deed.StageId)?.Id ?? string.Empty;
			if (!deed.CharacterUnlockId.Equals(seated, StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Gods: the seat deed '{deed.Id}' rekindles '{deed.CharacterUnlockId}', but the god seated there is '{seated}'.");
		}

		// SeatClear on a seat no god holds yet pays nobody (the chapter merged before its god), named for its seat.
		StageDefinition empty = StageCatalog.FarShore.FirstOrDefault(s => UnlockCatalog.GodSeatedAt(s.Id) == null);
		if (empty != null)
		{
			AchievementDefinition probe = AchievementDefinitions.SeatClear(empty.Id, "Probe", "probe_warden", "Probe");
			if (!probe.Id.Equals(AchievementDefinitions.SeatDeedId(empty.Id), StringComparison.Ordinal) || !string.IsNullOrEmpty(probe.CharacterUnlockId)
				|| probe.Kind != AchievementKind.Road || !probe.StageId.Equals(empty.Id, StringComparison.Ordinal)
				|| !probe.RewardText.Equals($"{QuestLog.RekindlesVerb} Probe", StringComparison.Ordinal))
				warnings.Add($"Gods: AchievementDefinitions.SeatClear on {empty.DisplayName}, which no god holds, does not build its seat's Road deed paying nobody.");
		}

		// A GOD MUST NOT SHOW A LOCKED SYSTEM (.ai/slow-start.md; second-half.md 6.1). A god opens on a Conjunction, so a
		// save that can play one must have been taught them. Today that holds because every seat is behind the far shore's
		// gate, which asks for the Grimoire, and the Grimoire counts every spell the ladder's three teachers pay - so no
		// save meets it without having found all three. If a teacher ever stops paying a Grimoire spell (in-flight item
		// 37 moving the ladder to characters), the gate must ask Teachings for Conjunctions itself; this says so.
		foreach (Teachings.Lesson lesson in Teachings.Ladder)
		{
			AchievementDefinition teacher = AchievementDefinitions.GetById(AchievementDefinitions.SecretDeedId(lesson.TeacherSecretId));
			if (teacher == null || string.IsNullOrEmpty(teacher.SpellUnlockId)
				|| !UnlockCatalog.GrimoireSpellIds.Contains(teacher.SpellUnlockId, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"Gods: {lesson.Name}'s teacher ('{lesson.TeacherSecretId}') pays no spell the Grimoire counts, so the far shore's gate no longer implies it has been taught - a god would open on a Conjunction for a save never taught one. Make the gate ask Teachings directly.");
		}
	}

	// One god, from its parts: the rules every god is held to, whether it is in the catalogs or built by hand.
	private static void ValidateGod(string label, GodParts g, bool checkOpener, List<string> warnings)
	{
		UnlockDefinition u = g.Unlock;
		if (u == null || !u.IsGod)
		{
			warnings.Add($"{label} is not declared a god (UnlockCatalog.FromGod, its seat).");
			return;
		}

		// Its source and its reveal (6.1-6.2): its seat's warden deed, secret until the seat opens, outside every gate.
		if (u.Source != UnlockSource.Achievement || !u.SourceId.Equals(AchievementDefinitions.SeatDeedId(u.SeatStageId), StringComparison.OrdinalIgnoreCase))
			warnings.Add($"{label} is not paid by its seat's warden deed '{AchievementDefinitions.SeatDeedId(u.SeatStageId)}'; a god is rekindled by its warden (decision 8).");
		if (!u.IsSecret || !u.RevealStageId.Equals(u.SeatStageId, StringComparison.OrdinalIgnoreCase))
			warnings.Add($"{label} is not secret until its seat opens; nothing of the far shore is on show before then (6.2).");
		if (!u.OutsideCampaignGate || u.CountedAtFinalGateOnly)
			warnings.Add($"{label} is inside a gate (OutsideCampaignGate); a god is not one of the imprisoned wizards.");
		if (u.SealedUntilFreed)
			warnings.Add($"{label} is sealed until freed; once its seat opens a god shows its silhouette and its held line (6.2).");
		if (!string.IsNullOrEmpty(u.SiteId))
			warnings.Add($"{label} is paid at a side event; a god is rekindled by its seat's warden.");

		if (g.Seat == null || !StageCatalog.IsOnFarShore(g.Seat))
			warnings.Add($"{label}'s seat '{u.SeatStageId}' is not a chapter of the far shore.");
		if (g.Character == null)
			warnings.Add($"{label} has no CharacterData in CharacterRoster (appended, in the order the gods merge).");

		// What it opens on (3.4): a Conjunction that names it, levelled from its own table, and its Apotheosis on it.
		if (g.Recipe == null || !ComboCatalog.StartsFused(g.Recipe, u.Id))
			warnings.Add($"{label} opens on no Conjunction that names it (ComboDefinition.StartsFusedFor); every god opens on one, at level 1 (3.4).");
		else if (checkOpener)
			ValidateOpener($"{label}'s Conjunction '{g.Recipe.Id}'", g.Recipe, g.Template, g.Character, g.Form, warnings);
		if (g.Form == null)
			warnings.Add($"{label} has no Apotheosis on its Conjunction (ApotheosisCatalog); every god has one, on a verb nobody owns (3.2).");
		else if (string.IsNullOrWhiteSpace(g.Form.Verb) || g.Form.Form == null)
			warnings.Add($"{label}'s Apotheosis names no verb or no form.");

		// Its words (6.3): a locked god is never named, so its row is all it has to be called by.
		if (g.Prisoner == null)
			warnings.Add($"{label} has no QuestLog.Prisoners row (epithet, held line, rekindled line - 6.3).");
		else
		{
			if (string.IsNullOrWhiteSpace(g.Prisoner.Epithet) || string.IsNullOrWhiteSpace(g.Prisoner.HeldLine) || string.IsNullOrWhiteSpace(g.Prisoner.FreedLine))
				warnings.Add($"{label}'s Prisoners row lacks an epithet, a held line or a rekindled line.");
			if (!(u.LockedHint ?? string.Empty).Equals(g.Prisoner.HeldLine ?? string.Empty, StringComparison.Ordinal))
				warnings.Add($"{label}'s card line \"{u.LockedHint}\" (UnlockCatalog.FromGod) is not its held line \"{g.Prisoner.HeldLine}\"; its silhouette carries its held line (6.2).");
			if (!(g.Prisoner.FreedLine ?? string.Empty).Contains("{0}"))
				warnings.Add($"{label}'s rekindled line \"{g.Prisoner.FreedLine}\" does not carry its name ({{0}}); the reel says it at the moment it becomes true.");
			else
			{
				string caption = string.Format(g.Prisoner.FreedLine, g.Character?.Name ?? u.Id);
				if (caption.Length > 46)
					warnings.Add($"{label}'s rekindled line \"{caption}\" is {caption.Length} characters; a reel's line fits one line on a PC (46).");
			}
		}

		// Its reel (6.3): a rekindling, on the far shore's ground.
		if (!g.HasReel)
			warnings.Add($"{label} has no rekindling reel (Freeings.WizardIds, and its row in tools/art/freed.py's GODS).");
		else if (!(g.ReelGround ?? string.Empty).Equals(Freeings.RekindledGround, StringComparison.Ordinal))
			warnings.Add($"{label}'s reel stands on '{g.ReelGround}', not the far shore's ground ('{Freeings.RekindledGround}').");

		// Its deed. Missing is the chapter's to-do while the seat is unbuilt (the god may merge first), a fault once it plays.
		if (g.SeatDeed == null)
		{
			if (g.Seat?.IsPlayable == true)
				warnings.Add($"{label}'s seat {g.Seat.DisplayName} is playable and no deed rekindles it (AchievementDefinitions.SeatClear).");
			return;
		}
		if (!g.SeatDeed.CharacterUnlockId.Equals(u.Id, StringComparison.OrdinalIgnoreCase))
			warnings.Add($"{label}'s seat deed '{g.SeatDeed.Id}' rekindles '{g.SeatDeed.CharacterUnlockId}', not it.");
		if (g.SeatDeed.Kind != AchievementKind.Road || !g.SeatDeed.StageId.Equals(u.SeatStageId, StringComparison.OrdinalIgnoreCase)
			|| string.IsNullOrWhiteSpace(g.SeatDeed.BossId))
			warnings.Add($"{label}'s seat deed '{g.SeatDeed.Id}' is not its seat's warden's Road deed.");
		if (g.Character != null && !g.SeatDeed.RewardText.Equals($"{QuestLog.RekindlesVerb} {g.Character.Name}", StringComparison.Ordinal))
			warnings.Add($"{label}'s seat deed says \"{g.SeatDeed.RewardText}\"; it should say \"{QuestLog.RekindlesVerb} {g.Character.Name}\".");
	}

	// --- a god added as data ---------------------------------------------------------------------------------------

	// A god built by hand, outside every catalog, as a god's piece would write it - and the ways to get it wrong.
	private sealed class ScratchGod
	{
		public string Id = "scratch_god";
		public string Name = "Scratch";
		public string SeatId = string.Empty;
		public string HeldLine = "No light has come home to this seat.";
		public string CardLine;                       // null: the held line, as FromGod is written
		public string RekindledLine = "{0} is lit again.";
		public bool NoPrisoner;
		public bool InsideGate;
		public bool NotSecret;
		public string Opener;                         // null: itself
		public bool OneLevel;
		public int DropLevel;
		public bool OpensOnOther;
		public bool NoForm;
		public Element FormElement = Element.Darkness;
		public bool NoReel;
		public string Ground = Freeings.RekindledGround;
		public string DeedPays;                       // null: itself
		public bool NoDeed;
	}

	private static GodParts Assemble(ScratchGod s, SpellData taking, SpellData other)
	{
		string card = s.CardLine ?? s.HeldLine;
		UnlockDefinition unlock = !s.InsideGate && !s.NotSecret
			? UnlockCatalog.FromGod(s.Id, s.SeatId, card)
			: new UnlockDefinition
			{
				Id = s.Id, Kind = UnlockKind.Character, Source = UnlockSource.Achievement,
				SourceId = AchievementDefinitions.SeatDeedId(s.SeatId), LockedHint = card,
				IsSecret = !s.NotSecret, RevealStageId = s.NotSecret ? string.Empty : s.SeatId,
				OutsideCampaignGate = !s.InsideGate, SeatStageId = s.SeatId,
			};

		// Its Conjunction: a copy of The Taking's - a recipe whose resource already levels 1 to 8 off its own table -
		// opened on by the scratch god instead of the dark wizard, as F2 points Sagittarius at Artemis.
		ComboDefinition shipped = ComboCatalog.ForId(TheTaking.SpellId);
		var template = (SpellData)taking.Duplicate(true);
		if (s.OneLevel)
		{
			template.MaxLevel = 1;
			template.LevelUpgrades = new Godot.Collections.Array<SpellLevelUpgrade>();
		}
		else if (s.DropLevel > 0)
		{
			var kept = new Godot.Collections.Array<SpellLevelUpgrade>();
			foreach (SpellLevelUpgrade upgrade in template.LevelUpgrades)
			{
				if (upgrade != null && upgrade.Level != s.DropLevel)
					kept.Add(upgrade);
			}
			template.LevelUpgrades = kept;
		}
		var recipe = new ComboDefinition
		{
			Id = shipped.Id, Name = shipped.Name, Verb = "scratch",
			First = shipped.First, Second = shipped.Second,
			ResourcePath = shipped.ResourcePath, SynergyTag = shipped.SynergyTag,
			StartsFusedFor = s.Opener ?? s.Id,
		};
		var character = new CharacterData
		{
			Id = s.Id, Name = s.Name,
			StartingSpellResource = s.OpensOnOther ? other : template,
			HealthModifier = 1f, SpeedModifier = 1f,
		};
		ApotheosisDefinition form = s.NoForm ? null : new ApotheosisDefinition
		{
			CharacterId = s.Id, SpellId = recipe.Id, Element = s.FormElement, Verb = "scratch",
			Form = new SpellEvolutionOption
			{
				Id = "scratch_form", DisplayName = "The Scratch Form", MilestoneLevel = ApotheosisCatalog.MilestoneLevel,
				BonusElementWeights = new Godot.Collections.Dictionary<string, int> { { s.FormElement.ToString(), 1 } },
			},
		};
		AchievementDefinition deed = s.NoDeed ? null : new AchievementDefinition
		{
			Id = AchievementDefinitions.SeatDeedId(s.SeatId), DisplayName = "The Scratch Seat",
			Description = "Defeat the boss of the scratch seat.", RewardText = $"{QuestLog.RekindlesVerb} {s.Name}",
			CharacterUnlockId = s.DeedPays ?? s.Id, Kind = AchievementKind.Road,
			Visibility = AchievementVisibility.ChapterOpen, StageId = s.SeatId, BossId = "scratch_warden",
		};
		return new GodParts
		{
			Unlock = unlock,
			Character = character,
			Recipe = recipe,
			Template = template,
			Form = form,
			Prisoner = s.NoPrisoner ? null : new QuestLog.Prisoner(s.Id, "the scratch", s.HeldLine, s.RekindledLine),
			HasReel = !s.NoReel,
			ReelGround = s.Ground,
			Seat = StageCatalog.GetById(s.SeatId),
			SeatDeed = deed,
		};
	}

	// F1's "done when" (.ai/second-half.md 9): a god can be added as data. A complete god, built from nothing but the data
	// a god's piece writes, passes every god and opener check; each part missing or wrong is refused; and a god may land
	// before its chapter's deed while the seat is unbuilt. Never written into a shipped catalog.
	private static void ValidateGodsAsData(List<string> warnings)
	{
		StageDefinition seat = StageCatalog.FarShore.FirstOrDefault(s => !s.IsPlayable);
		SpellData taking = ComboCatalog.Template(ComboCatalog.ForId(TheTaking.SpellId));
		SpellData other = ContentValidator.LoadAllSpells().FirstOrDefault(s => s.Id.Equals("magic_missile", StringComparison.OrdinalIgnoreCase));
		if (seat == null || taking == null || other == null)
		{
			// Every far chapter built: the shipped gods are the proof now, and nothing unbuilt is left to stand one on.
			if (seat == null && StageCatalog.FarShore.Count > 0)
				return;
			warnings.Add("Gods as data: cannot build the scratch god (no far chapter, The Taking or Magic Missile to build it from).");
			return;
		}

		List<string> Check(ScratchGod spec)
		{
			spec.SeatId = string.IsNullOrEmpty(spec.SeatId) ? seat.Id : spec.SeatId;
			var found = new List<string>();
			GodParts parts = Assemble(spec, taking, other);
			ValidateGod("Scratch god", parts, checkOpener: true, found);
			// Let go of what was made here at once: a script instance left to the finalizer outlives the engine's
			// teardown and is reported as a resource still in use at exit (CharacterRoster's note on the same churn).
			parts.Character?.Dispose();
			parts.Template?.Dispose();
			parts.Form?.Form?.Dispose();
			return found;
		}

		List<string> complete = Check(new ScratchGod());
		if (complete.Count > 0)
			warnings.Add("Gods as data: a complete god built from data alone is refused - " + string.Join(" | ", complete));
		if (Check(new ScratchGod { NoDeed = true }).Count > 0)
			warnings.Add("Gods as data: a god whose unbuilt seat has no deed yet is refused; a god's piece must be able to merge before its chapter's.");

		var broken = new (string What, ScratchGod Spec)[]
		{
			("no Prisoners row", new ScratchGod { NoPrisoner = true }),
			("a card line that is not its held line", new ScratchGod { CardLine = "Someone is here." }),
			("a rekindled line without its name", new ScratchGod { RekindledLine = "The seat is lit again." }),
			("a rekindled line too long for one line", new ScratchGod { RekindledLine = "{0} has come home to the seat that was held dark all this while." }),
			("no reel", new ScratchGod { NoReel = true }),
			("its reel on the heath", new ScratchGod { Ground = Freeings.FreedGround }),
			("a Conjunction that names another opener", new ScratchGod { Opener = "dark_wizard" }),
			("a Conjunction with no table (one level)", new ScratchGod { OneLevel = true }),
			("a level line missing from its table", new ScratchGod { DropLevel = 5 }),
			("a starting spell that is not its Conjunction", new ScratchGod { OpensOnOther = true }),
			("no Apotheosis", new ScratchGod { NoForm = true }),
			("a form deepening an element its Conjunction lacks", new ScratchGod { FormElement = Element.Fire }),
			("a seat on the road", new ScratchGod { SeatId = StageCatalog.Campaign.FirstOrDefault()?.Id ?? "stage_0" }),
			("a place inside the campaign's gates", new ScratchGod { InsideGate = true }),
			("a card on show before its seat opens", new ScratchGod { NotSecret = true }),
			("a seat deed that rekindles someone else", new ScratchGod { DeedPays = "dark_wizard" }),
		};
		int refused = 0;
		foreach (var (what, spec) in broken)
		{
			if (Check(spec).Count == 0)
				warnings.Add($"Gods as data: a god with {what} passes; the check that should refuse it does not fire.");
			else
				refused++;
		}
		GD.Print($"RegressionChecks: gods as data (.ai/second-half.md 9, F1) - a complete god built from data alone passes on {seat.DisplayName}"
			+ $" ({complete.Count} complaints), and {refused} of {broken.Length} broken ones are refused.");
	}
}
