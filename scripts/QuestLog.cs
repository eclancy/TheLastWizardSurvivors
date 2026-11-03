using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>One quest as the player reads it: a title, one line of what to do, and how far along it is.</summary>
public sealed class Quest
{
	/// <summary>What the save tracks: an achievement id, or <see cref="QuestLog.NextWizardId"/>.</summary>
	public string Id = string.Empty;
	public string Title = string.Empty;
	/// <summary>One line of what to do - the deed's own words, never a reward.</summary>
	public string Step = string.Empty;
	/// <summary>The chapter it is done in, or empty for anywhere.</summary>
	public string StageId = string.Empty;
	/// <summary>The deed it reads. Every quest is a view of one; nothing here keeps progress of its own.</summary>
	public AchievementDefinition Deed;
	/// <summary>Counted progress ("12 / 30"), or null for a yes/no deed.</summary>
	public AchievementProgress Progress;
	public bool Done;
	/// <summary>"Free the next wizard": resolved afresh every time it is read, so it moves on by itself.</summary>
	public bool IsNextWizard;
	/// <summary>The wizard this quest frees, if any: the log draws his silhouette, or his portrait once free.</summary>
	public string WizardId = string.Empty;
	/// <summary>The next wizard's chapter is not open yet, so the quest points down the road instead.</summary>
	public bool OnTheRoad;
}

/// <summary>
/// The quest log (.ai/quests-and-notes.md). Not a progress system: every quest is a reading of an
/// achievement the game already measures (<see cref="AchievementDefinitions"/>), and the only thing it
/// stores is which ones the player wants on screen (<see cref="SaveData.TrackedQuestIds"/>).
/// </summary>
/// <remarks>
/// Eric, 2026-10-31: "User should be able to track specific quests, and those default quests should always
/// be to free the next wizard. Tracked quests should show up on their screen while playing."
///
/// THE NEXT WIZARD. Every wizard who is not a starter, in the order the road reaches his rescue chapter
/// (<see cref="StageCatalog.CampaignOrderIds"/>), then the ones freed anywhere (John Dee's chests); the first
/// one not yet free is the next. Read live, so freeing him moves the quest on with no write.
///
/// A LOCKED WIZARD'S NAME IS HIS REWARD (.ai/side-events.md, "Secret wizards"). Nothing here names one
/// until he is free: a quest that frees him is titled by his epithet (<see cref="Prisoners"/>), and while
/// his chapter is still closed the quest does not even say who - only where the road goes next.
/// RegressionChecks.ValidateQuestsAndNotes reads every line this class can produce on sample saves and
/// reports any that carries a locked wizard's name.
/// </remarks>
public static class QuestLog
{
	/// <summary>The tracked id of "free the next wizard" - tracked on every save until the player says not.</summary>
	public const string NextWizardId = "next_wizard";

	/// <summary>
	/// How many may be tracked at once. Three is what the HUD's corner holds in two lines each without
	/// growing into the play field (QuestTracker); "keep text minimal".
	/// </summary>
	public const int MaxTracked = 3;

	/// <summary>
	/// Everyone held, by epithet: how a quest and a chapter note say who is there without saying his name.
	/// The held line goes in his chapter's note while he is locked; the freed line, with his name in it,
	/// once he is out. A wizard added to the roster needs a row here (the validator says so).
	/// </summary>
	public sealed record Prisoner(string CharacterId, string Epithet, string HeldLine, string FreedLine);

	public static readonly IReadOnlyList<Prisoner> Prisoners = new[]
	{
		// Taken from Barbados and accused at Salem (.ai/side-events.md). The game does not agree with her
		// accusers, which is why "the accused" and never what she was accused of.
		new Prisoner("tituba", "the accused", "The accused is in one of these cells.", "{0}'s cell stands open."),
		// The singer of the Kalevala: he is heard before he is found.
		new Prisoner("vainamoinen", "the singer under the ice", "Someone is singing under the ice.", "{0} sings above the ice now."),
		// The onmyoji, sealed in his own five-pointed seal: "the old wards were broken from the inside".
		new Prisoner("seimei", "the master of wards", "Someone here was sealed in by his own wards.", "{0} keeps his own wards again."),
		// The smith who struck every chain in the game, chained to his own anvil: "every shackle came from here".
		new Prisoner("wayland", "the smith at the anvil", "Someone below is still at the anvil. He may not stop.", "{0} has put his hammer down."),
		// The prophetess of Cumae, kept where nobody comes to ask: she is heard in more places than one.
		new Prisoner("sibyl", "the voice in the rock", "A woman's voice comes out of the rock, from more than one place.", "{0} speaks with one voice again."),
		// The witch of the deep wood, kept in her own hut and the hut kept walking its circle for ever (4.1). The Forest
		// is open on a new save, so this line is on its note from the first run.
		new Prisoner("baba_yaga", "the one in the walking hut", "Something heavy walks the deep wood, round and round.", "{0}'s hut has knelt at last."),
		// The physician who could cure the rot, hung in a cage over it to breathe it (4.4).
		new Prisoner("paracelsus", "the man in the gibbet's cage", "A man is coughing in a cage somewhere over the rot.", "{0} has crossed the water."),
		// The seer kept in the Court to read what is coming - and what is coming is the player (4.10). Heard reading,
		// room to room, never seen until the third.
		new Prisoner("nostradamus", "the reader in the house", "Somewhere in the house a voice is reading aloud.", "{0} has closed his book."),
		// The builder who could find water in the desert and raise it, sealed in a dry tomb beside the road the water
		// left (4.6). He is heard knocking under the sand.
		new Prisoner("imhotep", "the builder in the dry tomb", "Something knocks, stone on stone, under the sand.", "{0} walks in the sun again."),
		// The sorceress of Colchis, counted among what grows here and held in the reeve's granary against a tithe the
		// village could never pay (4.2).
		new Prisoner("medea", "the one in the granary", "Someone is kept in the reeve's granary, counted with the grain.", "{0} has walked out of the granary."),
		// The hound fated to catch whatever it hunted, set in stone mid-chase on the heath.
		new Prisoner("laelaps", "the hound in the stone", "A hound of stone stands mid-leap, still chasing.", "{0} has run his chase out."),
		// His library was looted while he was abroad, and he is kept the same way: scattered through chests.
		new Prisoner("john_dee", "the scattered scholar", "", "{0} has his library back."),
		// Not a prisoner: the one who took them (.ai/dark-wizard-playable.md), taken in his turn on the hardest
		// road. No held line - he is held nowhere - and once taken the Throne's note says what became of him.
		new Prisoner("dark_wizard", "the one on the throne", "", "{0} has nothing left to take."),
		// Not a prisoner either: Merlin, the old enchanter (.ai/merlin.md), earned by the whole road on Default. No
		// held line - he was never held. Not "the one he missed": that is the player (.ai/world-and-tone.md), and the
		// game is named for him.
		new Prisoner("merlin", "the old enchanter", "", "{0} keeps his circle."),
		// The queen in the barge, who carried the dying king over the water: the one who could cross without his
		// bridge, so he moored her under it (.ai/chapter-wizards.md 4.9). Seen as a light on the water.
		new Prisoner("morgan", "the woman in the barge", "A lantern on the water under the bridge, where no boat could be.", "{0} has come ashore."),
	};

	public static Prisoner PrisonerOf(string characterId) =>
		Prisoners.FirstOrDefault(p => p.CharacterId.Equals(characterId ?? string.Empty, StringComparison.OrdinalIgnoreCase));

	/// <summary>"the accused" - or, with no row for him, "a wizard", so a new wizard is never named by accident.</summary>
	public static string EpithetOf(string characterId) => PrisonerOf(characterId)?.Epithet ?? "a wizard";

	/// <summary>A character's display name, for the lines written once he is free.</summary>
	public static string NameOf(string characterId) =>
		CharacterRoster.GetAll().FirstOrDefault(c => c.Id.Equals(characterId ?? string.Empty, StringComparison.OrdinalIgnoreCase))?.Name
		?? characterId ?? string.Empty;

	/// <summary>
	/// The line a chapter note carries about whoever is held in <paramref name="stageId"/>: his held line
	/// while he is locked, his freed line (named) once he is out. Empty when nobody is held there.
	/// </summary>
	public static IEnumerable<string> HeldLinesFor(string stageId, SaveData save)
	{
		foreach (Prisoner prisoner in Prisoners)
		{
			UnlockDefinition who = UnlockCatalog.GetCharacter(prisoner.CharacterId);
			if (who == null || !GlobalStatsManager.RescueChapterOf(who).Equals(stageId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
				continue;
			if (GlobalStatsManager.IsCharacterUnlocked(save, prisoner.CharacterId))
				yield return string.Format(prisoner.FreedLine, NameOf(prisoner.CharacterId));
			else if (!string.IsNullOrEmpty(prisoner.HeldLine))
				yield return prisoner.HeldLine;
		}
	}

	/// <summary>
	/// The held lines alone - the wizards still held in a chapter, by epithet - and nothing once they are
	/// free. The chapter note's clues (ChapterNotes.CluesFor): a freed wizard is no longer a clue.
	/// </summary>
	public static IEnumerable<string> HeldOnlyLinesFor(string stageId, SaveData save)
	{
		foreach (Prisoner prisoner in Prisoners)
		{
			UnlockDefinition who = UnlockCatalog.GetCharacter(prisoner.CharacterId);
			if (who == null || !GlobalStatsManager.RescueChapterOf(who).Equals(stageId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
				continue;
			if (!GlobalStatsManager.IsCharacterUnlocked(save, prisoner.CharacterId) && !string.IsNullOrEmpty(prisoner.HeldLine))
				yield return prisoner.HeldLine;
		}
	}

	// --- the next wizard ---------------------------------------------------------------------------

	/// <summary>
	/// Every wizard the next-wizard quest walks, in order: the ones freed on the road in the order the road
	/// reaches them, then the ones freed anywhere. Laelaps is left out with the gates (he is not a wizard,
	/// UnlockCatalog.AllWizardIds) and so is a wizard whose chapter is not built yet - nobody can free him.
	/// </summary>
	public static IEnumerable<UnlockDefinition> WizardChain =>
		UnlockCatalog.AllWizardIds
			.Select(UnlockCatalog.GetCharacter)
			.Where(d => d != null && d.Source != UnlockSource.Starter && UnlockCatalog.SourceDeedOf(d) != null && IsBuildable(d))
			.Select((d, i) => (d, i))
			.OrderBy(p => ChainRank(p.d))
			.ThenBy(p => p.i)
			.Select(p => p.d);

	private static bool IsBuildable(UnlockDefinition wizard)
	{
		string chapter = GlobalStatsManager.RescueChapterOf(wizard);
		return string.IsNullOrWhiteSpace(chapter) || StageCatalog.GetById(chapter)?.IsPlayable == true;
	}

	// A wizard freed anywhere comes after the road: he needs no chapter, so he is never what stops the
	// player going on, and pointing at him first would send a new save to open chests rather than down
	// the road the story is about.
	private static int ChainRank(UnlockDefinition wizard)
	{
		string chapter = GlobalStatsManager.RescueChapterOf(wizard);
		if (string.IsNullOrWhiteSpace(chapter))
			return int.MaxValue - 1;
		int position = StageCatalog.CampaignPositionOf(chapter);
		return position >= 0 ? position : int.MaxValue;
	}

	/// <summary>The first wizard in <see cref="WizardChain"/> this save has not freed, or null when all are free.</summary>
	public static UnlockDefinition NextWizard(SaveData save) =>
		WizardChain.FirstOrDefault(d => !GlobalStatsManager.IsCharacterUnlocked(save, d.Id));

	// --- reading one quest -------------------------------------------------------------------------

	/// <summary>
	/// The quest a tracked id stands for on this save, measured in <paramref name="context"/> (the save's
	/// lifetime, or with a run folded in: AchievementContext.Projected). Null for an id that names nothing
	/// the player may be shown - a deed not listed, a sealed secret, the next wizard once all are free.
	/// </summary>
	public static Quest Resolve(string id, SaveData save, AchievementContext context)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		context ??= AchievementContext.ForSave(save);
		if (id.Equals(NextWizardId, StringComparison.OrdinalIgnoreCase))
			return ResolveNextWizard(save, context);
		AchievementDefinition deed = AchievementDefinitions.GetById(id);
		if (deed == null || !IsOffered(deed, save))
			return null;
		return FromDeed(deed, save, context);
	}

	private static Quest ResolveNextWizard(SaveData save, AchievementContext context)
	{
		UnlockDefinition wizard = NextWizard(save);
		AchievementDefinition deed = wizard != null ? UnlockCatalog.SourceDeedOf(wizard) : null;
		if (deed == null)
			return null;
		StageDefinition chapter = StageCatalog.GetById(deed.StageId);
		// His chapter is open (or he needs none): the quest is his deed, by his epithet.
		if (chapter == null || GlobalStatsManager.IsStageAvailable(save, chapter))
		{
			Quest quest = FromDeed(deed, save, context);
			quest.Id = NextWizardId;
			quest.IsNextWizard = true;
			return quest;
		}
		// Not open yet: say only that someone is further on, and where the road goes next - the boss of
		// the first chapter before his that still stands. Neither his name nor his chapter's: an unrevealed
		// chapter is not even on the world map (GlobalStatsManager.IsChapterRevealed).
		StageDefinition frontier = Frontier(save, chapter);
		AchievementDefinition clear = frontier != null ? BossClearOf(frontier) : null;
		return new Quest
		{
			Id = NextWizardId,
			IsNextWizard = true,
			OnTheRoad = true,
			// "Free the wizard", not "the next": Eric, 2026-11-02.
			Title = "Free the wizard",
			Step = clear != null ? clear.Description : "Further down the road.",
			StageId = frontier?.Id ?? string.Empty,
			Deed = clear,
			WizardId = wizard.Id,
		};
	}

	/// <summary>The first playable chapter before <paramref name="target"/> whose boss still stands.</summary>
	private static StageDefinition Frontier(SaveData save, StageDefinition target)
	{
		int end = StageCatalog.CampaignPositionOf(target.Id);
		IReadOnlyList<StageDefinition> road = StageCatalog.Campaign;
		for (int i = 0; i < road.Count && (end < 0 || i < end); i++)
		{
			StageDefinition stage = road[i];
			if (!stage.IsPlayable)
				continue;
			BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
			if (boss != null && !(save?.Lifetime?.HasDefeatedBoss(boss.Id) ?? false))
				return stage;
		}
		return null;
	}

	private static AchievementDefinition BossClearOf(StageDefinition stage)
	{
		BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
		return boss == null ? null : AchievementDefinitions.All.FirstOrDefault(
			d => d.BossId.Equals(boss.Id, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>The non-starter wizard a deed frees, or empty.</summary>
	public static string WizardFreedBy(AchievementDefinition deed)
	{
		if (deed == null || string.IsNullOrWhiteSpace(deed.CharacterUnlockId))
			return string.Empty;
		return UnlockCatalog.IsStarter(deed.CharacterUnlockId, UnlockKind.Character) ? string.Empty : deed.CharacterUnlockId;
	}

	private static Quest FromDeed(AchievementDefinition deed, SaveData save, AchievementContext context)
	{
		string wizard = WizardFreedBy(deed);
		bool done = GlobalStatsManager.IsAchievementUnlocked(save, deed.Id) || deed.IsComplete(context)
			|| (!string.IsNullOrEmpty(wizard) && GlobalStatsManager.IsCharacterUnlocked(save, wizard));
		AchievementProgress progress = deed.GetProgress(context);
		SecretDefinition secret = string.IsNullOrEmpty(deed.SecretId) ? null : SecretCatalog.Get(deed.SecretId);
		string step = secret != null && !done
			? (!string.IsNullOrEmpty(deed.SeenLine) ? deed.SeenLine : secret.SeenLine)
			: deed.Description;
		string title = deed.DisplayName;
		if (!string.IsNullOrEmpty(wizard))
		{
			// Named only once he is out; until then, by what he is.
			title = GlobalStatsManager.IsCharacterUnlocked(save, wizard)
				? $"{NameOf(wizard)} is free"
				: "Free " + EpithetOf(wizard);
		}
		return new Quest
		{
			Id = deed.Id,
			Title = title,
			Step = step,
			StageId = deed.StageId ?? string.Empty,
			Deed = deed,
			Progress = string.IsNullOrEmpty(progress.Display) ? null : progress,
			Done = done,
			WizardId = wizard,
		};
	}

	/// <summary>
	/// Whether a deed is a quest this save may see and track: listed in full on the achievements page
	/// (never sealed, never a hidden secret not yet seen), and - for a chapter's deed - in a chapter the
	/// world map has shown, so the log never names a place the map still keeps dark.
	/// </summary>
	public static bool IsOffered(AchievementDefinition deed, SaveData save)
	{
		if (deed == null || !AchievementDefinitions.IsListed(deed, save))
			return false;
		if (string.IsNullOrWhiteSpace(deed.StageId))
			return true;
		StageDefinition stage = StageCatalog.GetById(deed.StageId);
		if (stage == null)
			return true;
		return StageCatalog.IsInCampaign(stage)
			? GlobalStatsManager.IsChapterRevealed(save, stage)
			: GlobalStatsManager.IsStageAvailable(save, stage);
	}

	// --- tracking -------------------------------------------------------------------------------------

	public static bool IsTracked(SaveData save, string id) =>
		save != null && !string.IsNullOrWhiteSpace(id)
		&& save.TrackedQuestIds.Any(t => t.Equals(id, StringComparison.OrdinalIgnoreCase));

	/// <summary>Tracks or untracks. False when tracking would pass <see cref="MaxTracked"/> - the caller says no.</summary>
	public static bool SetTracked(SaveData save, string id, bool tracked)
	{
		if (save == null || string.IsNullOrWhiteSpace(id))
			return false;
		if (!tracked)
		{
			save.TrackedQuestIds.RemoveAll(t => t.Equals(id, StringComparison.OrdinalIgnoreCase));
			return true;
		}
		if (IsTracked(save, id))
			return true;
		Prune(save);
		if (save.TrackedQuestIds.Count >= MaxTracked)
			return false;
		save.TrackedQuestIds.Add(id);
		return true;
	}

	/// <summary>
	/// Drops a tracked deed that is done (or names nothing any more). The next wizard is never dropped: it
	/// moves on instead, and with every wizard free it simply shows nothing until one is added.
	/// </summary>
	public static void Prune(SaveData save)
	{
		if (save == null)
			return;
		AchievementContext lifetime = AchievementContext.ForSave(save);
		save.TrackedQuestIds.RemoveAll(id =>
		{
			if (id.Equals(NextWizardId, StringComparison.OrdinalIgnoreCase))
				return false;
			AchievementDefinition deed = AchievementDefinitions.GetById(id);
			if (deed == null)
				return true;
			return FromDeed(deed, save, lifetime).Done;
		});
	}

	/// <summary>
	/// What the HUD shows: the tracked quests in the order they were tracked, at most
	/// <see cref="MaxTracked"/>, each deed once (the next wizard and the same deed tracked by hand are one).
	/// </summary>
	public static List<Quest> Tracked(SaveData save, AchievementContext context)
	{
		var quests = new List<Quest>();
		if (save == null)
			return quests;
		foreach (string id in save.TrackedQuestIds)
		{
			Quest quest = Resolve(id, save, context);
			if (quest == null)
				continue;
			if (quests.Any(q => q.Deed != null && quest.Deed != null && q.Deed.Id == quest.Deed.Id))
				continue;
			quests.Add(quest);
			if (quests.Count >= MaxTracked)
				break;
		}
		return quests;
	}

	// --- the log --------------------------------------------------------------------------------------

	/// <summary>Everything the quest log lists, sorted into its sections.</summary>
	public sealed class Model
	{
		public Quest Next;
		public readonly List<Quest> Wizards = new();
		public readonly List<Quest> Freed = new();
		public readonly List<Quest> ThisChapter = new();
		public readonly List<Quest> Chapters = new();
		public readonly List<Quest> Feats = new();
		public int ChaptersDone;
		public int FeatsDone;
	}

	/// <summary>
	/// The log's sections. <paramref name="currentStageId"/>, on the pause screen, pulls the chapter being
	/// played to its own section first.
	/// </summary>
	public static Model BuildModel(SaveData save, AchievementContext context, string currentStageId)
	{
		var model = new Model();
		save ??= new SaveData();
		context ??= AchievementContext.ForSave(save);
		model.Next = ResolveNextWizard(save, context);

		foreach (AchievementDefinition deed in AchievementDefinitions.All)
		{
			if (!IsOffered(deed, save))
				continue;
			Quest quest = FromDeed(deed, save, context);
			if (!string.IsNullOrEmpty(quest.WizardId))
			{
				// The next wizard's own deed is the Next row; the rest stand under it, then the freed.
				if (quest.Done)
					model.Freed.Add(quest);
				else if (model.Next == null || model.Next.Deed?.Id != deed.Id)
					model.Wizards.Add(quest);
				continue;
			}
			bool chapterDeed = deed.Kind == AchievementKind.Road || deed.Kind == AchievementKind.Secret;
			if (quest.Done)
			{
				if (chapterDeed)
					model.ChaptersDone++;
				else
					model.FeatsDone++;
				continue;
			}
			if (!chapterDeed)
				model.Feats.Add(quest);
			else if (!string.IsNullOrEmpty(currentStageId) && deed.StageId.Equals(currentStageId, StringComparison.OrdinalIgnoreCase))
				model.ThisChapter.Add(quest);
			else
				model.Chapters.Add(quest);
		}
		return model;
	}

	/// <summary>Every line the log, the HUD and the run's opening can show on this save - for the name check.</summary>
	public static IEnumerable<string> AllText(SaveData save)
	{
		Model model = BuildModel(save, null, string.Empty);
		IEnumerable<Quest> every = new[] { model.Next }
			.Concat(model.Wizards).Concat(model.Freed).Concat(model.ThisChapter).Concat(model.Chapters).Concat(model.Feats)
			.Concat(Tracked(save, null));
		foreach (Quest quest in every.Where(q => q != null))
		{
			yield return quest.Title;
			yield return quest.Step;
		}
	}
}
