using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE FAR SHORE'S PLUMBING (.ai/second-half.md 7 and 9, piece F0). The second half is six chapters across the dark
// sea in an order of their own (StageCatalog.FarShoreOrder), opened by a gate of their own (StageGate.FarShore).
// What a build cannot see, and what F0's "done when" asks: the far shore exists; nobody can enter it while it is
// unbuilt; and the Dark Wizard's and The Road Restored's targets have not moved - which is the whole reason it is
// a second order and not six lines appended to the campaign's. Every line is a warning, as the rest of RunAll's
// are, except the to-do list of an unbuilt far chapter, which is printed: six chapters are a long build, and six
// standing warnings would bury the real ones.
public static partial class RegressionChecks
{
	private static void ValidateFarShore(List<string> warnings)
	{
		IReadOnlyList<StageDefinition> shore = StageCatalog.FarShore;
		if (shore.Count == 0)
		{
			warnings.Add("Far shore: StageCatalog.FarShoreOrder names no chapter, so the second half has nowhere to be.");
			return;
		}

		// THE ORDER. Every id a stage, none twice, none on the campaign's road - one far chapter in CampaignOrder
		// would add a chapter to both whole-road deeds (section 7).
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string id in StageCatalog.FarShoreOrderIds)
		{
			if (StageCatalog.GetById(id) == null)
				warnings.Add($"Far shore: the order names '{id}', which is not a stage in StageCatalog.");
			if (!seen.Add(id))
				warnings.Add($"Far shore: '{id}' appears twice in the order, so it would have to be beaten to open itself.");
			if (StageCatalog.CampaignOrderIds.Contains(id, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"Far shore: '{id}' is in CampaignOrder too; the far shore is never in the campaign, or the Dark Wizard's and The Road Restored's targets move.");
		}

		// THE GATES. The first chapter is the one FarShore gate (the whole road behind it, 6.1); every later one
		// opens on the boss before it in the far shore's own order; nothing else anywhere is gated FarShore.
		for (int i = 0; i < shore.Count; i++)
		{
			StageDefinition stage = shore[i];
			StageGate want = i == 0 ? StageGate.FarShore : StageGate.PreviousBoss;
			if (stage.Gate != want)
				warnings.Add($"Far shore: {stage.DisplayName} is gated {stage.Gate}; as far chapter {i + 1} it must be {want}.");
		}
		foreach (StageDefinition stage in StageCatalog.All.Where(s => s.Gate == StageGate.FarShore && !StageCatalog.IsOnFarShore(s)))
			warnings.Add($"Far shore: {stage.DisplayName} is gated FarShore but is not on the far shore.");

		// EACH ORDER KEEPS TO ITSELF. No boss of the road opens a far chapter (the Throne's fall opens nothing: the
		// far shore's gate wants the spells and the wizards too), and a far chapter is only ever gated on a far one.
		foreach (StageDefinition stage in StageCatalog.Campaign)
		{
			StageDefinition opens = StageCatalog.ChapterOpenedBy(stage.Id);
			if (opens != null && StageCatalog.IsOnFarShore(opens))
				warnings.Add($"Far shore: beating the boss of {stage.DisplayName} writes {opens.DisplayName} open; the road's bosses open nothing across the water.");
		}
		for (int i = 0; i < shore.Count; i++)
		{
			StageDefinition stage = shore[i];
			StageDefinition previous = StageCatalog.PreviousChapter(stage);
			if (previous != null && (!StageCatalog.IsOnFarShore(previous) || StageCatalog.FarShorePositionOf(previous.Id) >= i))
				warnings.Add($"Far shore: {stage.DisplayName} is gated on {previous.DisplayName}, which is not earlier on the far shore.");
			// While a far chapter is unbuilt its previous is whatever playable one stands before it, if any (none
			// today); once it is playable it must be opened by a real boss in its order, as the campaign's are.
			if (!stage.IsPlayable || stage.Gate != StageGate.PreviousBoss)
				continue;
			if (previous == null)
				warnings.Add($"Far shore: {stage.DisplayName} is playable and has no playable far chapter before it, so no boss can ever open it.");
			else if (BossCatalog.ForStageIndex(previous.Index) == null)
				warnings.Add($"Far shore: {stage.DisplayName} is opened by the boss of {previous.DisplayName}, which has none in BossCatalog.");
			else if (StageCatalog.ChapterOpenedBy(previous.Id) != stage)
				warnings.Add($"Far shore: {stage.DisplayName}'s gate names {previous.DisplayName}, but beating it opens {StageCatalog.ChapterOpenedBy(previous.Id)?.DisplayName ?? "nothing"}.");
		}

		// THE WHOLE-ROAD DEEDS HAVE NOT MOVED: both count the campaign as CampaignOrder writes it (thirteen chapters
		// today), never a far one, and the chapters that end the road are the road's. A
		// far chapter won counts toward neither: ValidateTakerTaken holds every chapter outside the campaign to that.
		int road = StageCatalog.CampaignOrderIds.Count;
		if (StageCatalog.Campaign.Count != road || AchievementDefinitions.CampaignChapterCount != road)
			warnings.Add($"Far shore: the campaign counts {AchievementDefinitions.CampaignChapterCount} chapters, not the {road} CampaignOrder names.");
		foreach (string deedId in new[] { AchievementDefinitions.TakerTakenId, AchievementDefinitions.OneHeMissedId })
		{
			AchievementDefinition deed = AchievementDefinitions.GetById(deedId);
			if (deed == null)
				warnings.Add($"Far shore: the whole-road deed '{deedId}' is missing.");
			else if (deed.Target != road)
				warnings.Add($"Far shore: {deed.DisplayName} asks for {deed.Target} chapters, not the campaign's {road}; its target has moved.");
		}
		foreach (var (role, chapter) in new[]
		{
			("FinalChapter", StageCatalog.FinalChapter), ("LastPlayableChapter", StageCatalog.LastPlayableChapter),
			("GateChapter", StageCatalog.GateChapter), ("FinalGateChapter", StageCatalog.FinalGateChapter),
		})
		{
			if (chapter != null && StageCatalog.IsOnFarShore(chapter))
				warnings.Add($"Far shore: StageCatalog.{role} is {chapter.DisplayName}, a far chapter; the road ends at the Throne.");
		}
		var roadOnly = new SaveData { BalancePresetId = GlobalStatsManager.BalancePresetHardcore };
		foreach (StageDefinition stage in StageCatalog.Campaign)
		{
			BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
			if (boss == null)
				continue;
			AchievementManager.ApplyRunAchievements(roadOnly, new global::RunResult
			{
				Outcome = global::RunOutcome.Victory,
				BalancePresetId = GlobalStatsManager.BalancePresetHardcore,
				BossId = boss.Id,
				StageId = stage.Id,
			});
		}
		if (!GlobalStatsManager.ClearedEveryChapterAt(roadOnly, GlobalStatsManager.PresetRankHardcore))
			warnings.Add("Far shore: every chapter of the campaign won on Hardcore, and nothing across the water, does not finish the whole road; it must not ask for the far shore.");

		// NOBODY CAN ENTER IT, AND NOTHING UNBUILT IS WRITTEN DOWN. A save with everything - every spell, every
		// character, every boss beaten - meets the far shore's gate, and still cannot enter an unbuilt chapter, and
		// has none written into its opened chapters, by the far shore's write-down or by the gate chapters'.
		SaveData veteran = SaveThroughGates();
		foreach (BossDefinition beaten in BossCatalog.All)
			veteran.Lifetime.BossesDefeated.Add(beaten.Id);
		foreach (string spellId in UnlockCatalog.AllSpellIds)
			GlobalStatsManager.UnlockSpell(veteran, spellId);
		foreach (string characterId in UnlockCatalog.AllCharacterIds)
			GlobalStatsManager.UnlockCharacter(veteran, characterId);
		if (!GlobalStatsManager.IsFarShoreGateMet(veteran))
			warnings.Add("Far shore: a save with every spell, every character and every boss beaten does not meet the far shore's gate.");
		foreach (StageDefinition stage in shore.Where(s => !s.IsPlayable && GlobalStatsManager.IsStageAvailable(veteran, s)))
			warnings.Add($"Far shore: {stage.DisplayName} is not playable, but a save with everything can enter it.");
		foreach (StageDefinition written in GlobalStatsManager.RecordMetFarShore(veteran).Concat(GlobalStatsManager.RecordMetGates(veteran))
			.Where(s => StageCatalog.IsOnFarShore(s) && !s.IsPlayable))
			warnings.Add($"Far shore: {written.DisplayName} is not playable, but a save with everything had it written into its opened chapters.");
		if (GlobalStatsManager.FarShoreGateMissing(veteran).Count > 0)
			warnings.Add($"Far shore: a save with everything is still told it lacks something: \"{StageCatalog.FarShoreLockedHintFor(veteran)}\".");

		// NOTHING ON SHOW BEFORE TAKEN BACK (6.2): a new save is shown no far chapter and has earned none; once the
		// Throne is won - by the deed or by his fall in the lifetime record - the first stop shows, under its seal,
		// and no other.
		var fresh = new SaveData();
		foreach (StageDefinition stage in shore)
		{
			if (GlobalStatsManager.IsStageEarned(fresh, stage))
				warnings.Add($"Far shore: a new save has earned {stage.DisplayName}.");
			if (GlobalStatsManager.IsChapterRevealed(fresh, stage))
				warnings.Add($"Far shore: a new save is shown {stage.DisplayName} on the map; nothing of the far shore is on show before Taken Back.");
		}
		var taken = new SaveData();
		taken.Lifetime.BossesDefeated.Add(AchievementDefinitions.ThroneBossId);
		var deedOnly = new SaveData();
		deedOnly.UnlockedAchievementIds.Add(AchievementDefinitions.TakenBackId);
		if (!GlobalStatsManager.HasTakenBackTheThrone(taken) || !GlobalStatsManager.HasTakenBackTheThrone(deedOnly))
			warnings.Add("Far shore: Taken Back is not read from both the deed and the dark wizard's fall in the lifetime record.");
		for (int i = 0; i < shore.Count; i++)
		{
			bool shown = GlobalStatsManager.IsChapterRevealed(taken, shore[i]);
			if (shown != (i == 0))
				warnings.Add($"Far shore: once the Throne is won {shore[i].DisplayName} is {(shown ? "shown" : "hidden")}; only the first far stop should be.");
			if (GlobalStatsManager.IsStageEarned(taken, shore[i]))
				warnings.Add($"Far shore: the Throne won alone earns {shore[i].DisplayName}, without the spells and the wizards.");
		}

		// THE LOCK LINE names nobody still held - by epithet, or "a wizard" while sealed (QuestLog's rule).
		foreach (var (label, save) in new[] { ("a new save", fresh), ("a save that has taken the Throne", taken) })
		{
			string line = StageCatalog.FarShoreLockedHintFor(save);
			foreach (string id in GlobalStatsManager.FarShoreWizardIds.Where(id => !GlobalStatsManager.IsCharacterUnlocked(save, id)))
			{
				if (NamesWizard(line, QuestLog.NameOf(id)))
					warnings.Add($"Far shore: on {label} the lock line names '{id}', who is still held: \"{line}\".");
			}
		}

		// ITS NOTES AND ITS DIFFICULTY, which F0 wrote for every far chapter so neither is lost to a flip.
		foreach (StageDefinition stage in shore)
		{
			if (!ChapterNotes.Has(stage.Id))
				warnings.Add($"Far shore: {stage.DisplayName} has no chapter note (ChapterNotes).");
			else
			{
				IReadOnlyList<string> intro = ChapterNotes.IntroFor(stage.Id);
				if (intro.Count > ChapterNotes.MaxIntroLines)
					warnings.Add($"Far shore: {stage.DisplayName}'s note is {intro.Count} lines; at most {ChapterNotes.MaxIntroLines}.");
				foreach (string line in intro.Where(l => l.Length > ChapterNotes.MaxLineLength))
					warnings.Add($"Far shore: '{line}' is {line.Length} characters; at most {ChapterNotes.MaxLineLength}.");
			}
			if (ReferenceEquals(ChapterDifficultyCatalog.For(stage.Id), ChapterDifficultyCatalog.Neutral))
				warnings.Add($"Far shore: {stage.DisplayName} has no row in ChapterDifficultyCatalog.");
		}

		ReportFarShoreToDo(warnings);
	}

	// THE FAR SHORE'S GATE, ON PAPER AND IN MEMORY (.ai/second-half.md 6.1 and 7), from ValidateGateReachability.
	// It may wait on secrets, as no road gate may: Eric accepted the Grimoire, nine vaults deep, with the guard of
	// question 6 (the vaults pointed at once the Throne is won). What it may never wait on is anything paid on the
	// far shore itself. And it must want all three halves, and neither the Dark Wizard nor John Dee.
	private static void ValidateFarShoreGateReachability(List<string> warnings)
	{
		StageDefinition gate = StageCatalog.FarShore.FirstOrDefault(s => s.Gate == StageGate.FarShore);
		if (gate == null)
		{
			warnings.Add("Gate reachability: no far shore chapter is gated FarShore, so nothing ever opens the far shore.");
			return;
		}

		// On paper.
		List<string> counted = GlobalStatsManager.FarShoreWizardIds.ToList();
		foreach (string id in counted)
		{
			AchievementDefinition deed = UnlockCatalog.SourceDeedOf(UnlockCatalog.GetCharacter(id));
			if (deed == null || StageCatalog.CampaignPositionOf(deed.StageId) < 0)
				warnings.Add($"Gate reachability: the far shore counts '{id}', who is not freed in a chapter of the campaign.");
		}
		foreach (string id in new[] { "merlin", "laelaps" })
		{
			if (!counted.Contains(id, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"Gate reachability: the far shore does not count '{id}'; 6.1 names him.");
		}
		foreach (string id in new[] { "dark_wizard", "john_dee" })
		{
			if (counted.Contains(id, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"Gate reachability: the far shore counts '{id}'; 6.1 leaves him out (a difficulty or a grind, not a place).");
		}
		if (StageCatalog.FinalGateChapter is StageDefinition throne)
		{
			foreach (string id in GlobalStatsManager.GateWizardIds(throne).Where(id =>
				!string.IsNullOrWhiteSpace(GlobalStatsManager.RescueChapterOf(UnlockCatalog.GetCharacter(id)))))
			{
				if (!counted.Contains(id, StringComparer.OrdinalIgnoreCase))
					warnings.Add($"Gate reachability: {throne.DisplayName} waits on '{id}', held in a place, but the far shore does not.");
			}
		}
		foreach (string spellId in UnlockCatalog.GrimoireSpellIds)
		{
			AchievementDefinition deed = UnlockCatalog.SourceDeedOf(UnlockCatalog.GetSpell(spellId));
			if (deed != null && StageCatalog.IsOnFarShore(StageCatalog.GetById(deed.StageId)))
				warnings.Add($"Gate reachability: the Grimoire counts '{spellId}', paid on the far shore ('{deed.Id}'), so the far shore would wait on itself.");
		}

		// In memory: a new save walks the whole campaign - every boss beaten as a victory records it, every side
		// event won, every built secret found, every Feat done - and must then have earned the far shore. The
		// Grimoire's own deed is never done by hand here: it must come from the spells, or it proves nothing.
		var save = new SaveData();
		IReadOnlyList<StageDefinition> campaign = StageCatalog.Campaign;
		for (int pass = 0; pass <= campaign.Count + 1; pass++)
		{
			bool changed = false;
			foreach (StageDefinition stage in campaign)
			{
				if (!GlobalStatsManager.IsStageAvailable(save, stage))
					continue;
				BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
				if (boss != null && !save.Lifetime.HasDefeatedBoss(boss.Id))
				{
					WinOnPaper(save, boss, stage);
					changed = true;
				}
				foreach (SideEventEntry site in global::SideEventDirector.EventsFor(stage.Id))
				{
					if (save.Lifetime.HasWonEvent(site.SiteId))
						continue;
					DiscoveryRewards.GrantSite(save, site.SiteId);
					changed = true;
				}
				foreach (SecretDefinition secret in SecretCatalog.ForStage(stage.Id))
				{
					if (save.HasFoundSecret(secret.Id))
						continue;
					AchievementManager.Complete(save, AchievementDefinitions.SecretDeedId(secret.Id));
					changed = true;
				}
			}
			// A wizard's opener (in-flight 84) is done as him once he is free, where its chapter is open: beside its side
			// event or secret through the door the run uses, or straight for a Feat or a boss.
			foreach (AchievementDefinition opener in AchievementDefinitions.All.Where(d => d.IsWizardOpener))
			{
				if (GlobalStatsManager.IsAchievementUnlocked(save, opener.Id) || !GlobalStatsManager.IsCharacterUnlocked(save, opener.RequiredCharacterId))
					continue;
				StageDefinition at = StageCatalog.GetById(opener.StageId);
				if (at != null && (StageCatalog.CampaignPositionOf(at.Id) < 0 || !GlobalStatsManager.IsStageAvailable(save, at)))
					continue;
				if (!string.IsNullOrWhiteSpace(opener.AsWizardOf))
					AchievementManager.Complete(save, opener.AsWizardOf, opener.RequiredCharacterId);
				else
					AchievementManager.Complete(save, opener.Id);
				changed = true;
			}
			foreach (AchievementDefinition feat in AchievementDefinitions.All.Where(d =>
				(d.Kind == AchievementKind.Feat || d.Kind == AchievementKind.Collection) && !d.IsWizardOpener
				&& !d.Id.Equals(AchievementDefinitions.GrimoireRestoredId, StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(d.StageId) || StageCatalog.CampaignPositionOf(d.StageId) >= 0)))
			{
				if (GlobalStatsManager.IsAchievementUnlocked(save, feat.Id))
					continue;
				AchievementManager.Complete(save, feat.Id);
				changed = true;
			}
			if (!changed)
				break;
		}
		if (!GlobalStatsManager.IsStageEarned(save, gate))
		{
			var spells = UnlockCatalog.GrimoireSpellIds.Where(id => !GlobalStatsManager.IsSpellUnlockedForLevelUp(save, id)).ToList();
			warnings.Add($"Gate reachability: a new save that does everything on the campaign still cannot reach {gate.DisplayName} - "
				+ string.Join("; ", GlobalStatsManager.FarShoreGateMissing(save))
				+ (spells.Count > 0 ? $" (spells never recovered: {string.Join(", ", spells)})" : "") + ".");
		}

		// Every half is needed, and nothing more: Taken Back, every Grimoire spell and every wizard it counts opens
		// it (with the Dark Wizard and John Dee still held); without any one of them it stays shut.
		SaveData Sample(bool throneWon, string skipSpell, string skipWizard)
		{
			var sample = new SaveData();
			if (throneWon)
				sample.Lifetime.BossesDefeated.Add(AchievementDefinitions.ThroneBossId);
			foreach (string id in UnlockCatalog.GrimoireSpellIds.Where(id => !id.Equals(skipSpell ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
				GlobalStatsManager.UnlockSpell(sample, id);
			foreach (string id in counted.Where(id => !id.Equals(skipWizard ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
				GlobalStatsManager.UnlockCharacter(sample, id);
			return sample;
		}
		if (!GlobalStatsManager.IsStageEarned(Sample(true, null, null), gate))
			warnings.Add($"Gate reachability: {gate.DisplayName} stays shut to a save with the Throne won, every spell and every wizard it counts - the Dark Wizard and John Dee must not be needed.");
		if (GlobalStatsManager.IsStageEarned(Sample(false, null, null), gate))
			warnings.Add($"Gate reachability: {gate.DisplayName} opens without the Throne taken back.");
		string oneSpell = UnlockCatalog.GrimoireSpellIds.FirstOrDefault(id => !UnlockCatalog.IsStarter(id, UnlockKind.Spell));
		if (oneSpell != null && GlobalStatsManager.IsStageEarned(Sample(true, oneSpell, null), gate))
			warnings.Add($"Gate reachability: {gate.DisplayName} opens with '{oneSpell}' still unrecovered.");
		foreach (string missing in counted)
		{
			if (GlobalStatsManager.IsStageEarned(Sample(true, null, missing), gate))
				warnings.Add($"Gate reachability: {gate.DisplayName} opens with '{missing}' still held.");
		}
	}

	// The far shore's to-do list, one line a chapter, in the manner of the campaign's (ValidateCampaignToDo): each
	// item a file that does not exist yet or a line not yet filled, gone when its piece lands. PRINTED while the
	// chapter is unbuilt (six long builds would otherwise stand as six warnings over every run of the validator);
	// a FAULT once it is playable, so no far chapter flips with a stand-in left.
	private static void ReportFarShoreToDo(List<string> warnings)
	{
		WorldMapLayout map = WorldMapLayout.Load(out _);
		foreach (StageDefinition stage in StageCatalog.FarShore)
		{
			var todo = new List<string>();
			BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
			if (boss == null)
				todo.Add("no warden in BossCatalog");
			else if (boss.IsOnStandIn)
				todo.Add($"its warden fights as '{ShortScenePath(boss.StandInScenePath)}' until '{ShortScenePath(boss.ScenePath)}' exists");
			if (UnlockCatalog.GodSeatedAt(stage.Id) == null)
				todo.Add("no god seated here (UnlockCatalog.FromGod)");
			if (string.IsNullOrWhiteSpace(StageRewards.WardenDeed(stage)?.CharacterUnlockId))
				todo.Add("no deed frees its god when its warden falls");
			if (StageBestiary.ForStageId(stage.Id) == null)
				todo.Add("no cast (StageBestiary.<Chapter>.cs)");
			StageDefinition sharer = StageCatalog.All.FirstOrDefault(o => !ReferenceEquals(o, stage) && o.EnvironmentKind == stage.EnvironmentKind);
			if (sharer != null)
				todo.Add($"its ground is borrowed from {sharer.DisplayName} ({stage.EnvironmentKind}) until it has a StageEnvironmentKind of its own");
			MusicSet own = MusicCatalog.OwnSetForStage(stage.Index);
			if (own == null)
				todo.Add("no row in MusicCatalog");
			else if (own.StandIn != null && !own.IsRendered)
				todo.Add($"music plays '{own.Resolved.Id}' until {own.Id}_bed/_pulse/_lead.wav are rendered and imported");
			if (SideEventDirector.EventsFor(stage.Id).Count == 0)
				todo.Add("its side event is still the null line in SideEventDirector");
			if (ChapterRulesCatalog.IsAwaitingRules(stage.Id))
				todo.Add("its rules are still the null line in ChapterRulesCatalog");
			if (!SecretCatalog.Defined.Any(s => s.StageId.Equals(stage.Id, StringComparison.OrdinalIgnoreCase)))
				todo.Add("no secrets (SecretCatalog.<Chapter>.cs)");
			if (map != null && map.Get(stage.Id) == null)
				todo.Add("no place on the world map (F5: PLACES in tools/art/world_map.py)");

			if (todo.Count == 0)
				continue;
			if (stage.IsPlayable)
				warnings.Add($"Far shore: {stage.DisplayName} is playable but still has stand-ins - " + string.Join("; ", todo) + ".");
			else
				GD.Print($"RegressionChecks: far shore to-do (.ai/second-half.md 9): {stage.DisplayName} ({stage.Id}) is not playable yet - "
					+ string.Join("; ", todo) + "; then IsPlayable = true, last.");
		}
	}
}
