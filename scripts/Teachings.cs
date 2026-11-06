using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>The systems a save is taught one at a time (.ai/slow-start.md, section 3).</summary>
public enum Teaching
{
	Ascension,
	Apotheosis,
	Conjunctions,
	SovereignRite,
}

/// <summary>
/// The one authority on what a save has been taught: the Ascension, the Apotheosis, Conjunctions and the
/// Sovereign Rite (.ai/slow-start.md, "Systems, and what teaches each"). Eric, 2026-11-02: "new systems
/// should be unlocked in a post-run cutscene, after fulfilling an achievement, like finding a landmark.
/// The cutscene can then explain the new system."
/// </summary>
/// <remarks>
/// <para><b>The ladder.</b> The first three arrive in a fixed order, because each needs the one before it:
/// the Apotheosis is the wizard's own spell ascended, and a Conjunction is two ascended spells made one. Each
/// is taught by a spell vault - the landmark of its chapter, placed in every run (.ai/achievements-and-secrets.md
/// 6.4): the Pinfold (the Fallow Fields), the Fallen Star (the Broken Circle), the Geode Heart (the Sunken
/// Cave). <b>A teacher teaches the next rung, not a fixed one</b>: whichever of the three a save finds first
/// teaches the Ascension, the second the Apotheosis, the third Conjunctions. So a player who walked past the
/// Pinfold and found the Fallen Star is taught the Ascension there, never a system he cannot use yet, and
/// never two systems on one evening. In campaign order - the common path - each landmark teaches its own.</para>
///
/// <para><b>Learned means shown.</b> Finding a teacher makes a lesson <i>owed</i> (<see cref="Owed"/>); it is
/// LEARNED when its cutscene plays, after the run that found it and before the results (Node2DGame,
/// <see cref="Teach"/>). Quitting a run also plays its owed lesson before the results. So a system is never usable before
/// it has been explained, and the run that found the teacher is played as it began (Player reads the flags
/// once, at the start).</para>
///
/// <para><b>The Sovereign Rite</b> stays the Warden of the Deep's deed: it is usable when the save owns the
/// spell, as before (Player.TryBuildSovereignOffer), and its lesson is only the cutscene that explains it,
/// owed the first time the save owns it.</para>
///
/// <para><b>One locked thing on show at a time</b> (Eric's Q8, <see cref="IsShown"/>): the Ascension is shown
/// sealed from the first run, the Apotheosis only once the Ascension is learned, Conjunctions only once the
/// Apotheosis is - in the Ascension strip, the Evolution Codex and the character select.</para>
///
/// <para>A null save has learned everything: tools build players bare.</para>
/// </remarks>
public static class Teachings
{
	/// <summary>One system's lesson: what it is called, what teaches it, the cutscene that explains it.</summary>
	public sealed record Lesson(
		Teaching System,
		// Stored in SaveData.LessonsLearned; also the cutscene's reel (Prologue.Reels).
		string Id,
		string Name,
		// The spell vault that teaches this rung in campaign order; empty for the Rite.
		string TeacherSecretId,
		// The in-run line when it is owed, and the results card's line.
		string Line);

	/// <summary>The ladder, in the order a save learns it.</summary>
	public static readonly IReadOnlyList<Lesson> Ladder = new[]
	{
		new Lesson(Teaching.Ascension, "ascension", "The Ascension", SecretCatalog.PinfoldId,
			"A spell at level 7 may ascend"),
		new Lesson(Teaching.Apotheosis, "apotheosis", "The Apotheosis", SecretCatalog.FallenStarId,
			"Your wizard's own spell has a final form"),
		new Lesson(Teaching.Conjunctions, "conjunctions", "Conjunctions", SecretCatalog.GeodeHeartId,
			"Two ascended spells may become one"),
	};

	/// <summary>The Rite: owned through the Warden of the Deep's deed, explained once.</summary>
	public static readonly Lesson Rite = new(Teaching.SovereignRite, "sovereign_rite", "The Sovereign Rite", string.Empty,
		"One spell may take the strength of the rest");

	/// <summary>Every lesson, the ladder first.</summary>
	public static IEnumerable<Lesson> All => Ladder.Append(Rite);

	public static Lesson Get(Teaching system) => All.First(l => l.System == system);

	public static Lesson GetById(string id) =>
		All.FirstOrDefault(l => l.Id.Equals(id ?? string.Empty, StringComparison.OrdinalIgnoreCase));

	/// <summary>The next lesson an unfound teacher can give, including teachers visited out of order.</summary>
	public static Lesson LessonAt(SaveData data, string secretId)
	{
		Lesson teacher = Ladder.FirstOrDefault(l => l.TeacherSecretId.Equals(secretId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		if (data == null || teacher == null || TeacherFound(data, teacher))
			return null;
		int creditAfterFind = TeachersFound(data) + 1 - Ladder.Count(l => HasLearned(data, l.System));
		return creditAfterFind <= 0 ? null
			: Ladder.Where(l => !HasLearned(data, l.System)).Skip(creditAfterFind - 1).FirstOrDefault();
	}

	/// <summary>A gentle map hint: names no unrecovered spell and gives no puzzle solution.</summary>
	public static string LandmarkHint(string secretId) => secretId switch
	{
		SecretCatalog.PinfoldId => "Something of light is penned in the fields.",
		SecretCatalog.FallenStarId => "A fallen light lies beyond the standing stones.",
		SecretCatalog.GeodeHeartId => "A warm light shines inside the cave's crystals.",
		_ => string.Empty,
	};

	/// <summary>The secrets that teach, as deeds: the vault deeds of the ladder's three landmarks.</summary>
	public static IEnumerable<string> TeacherDeedIds =>
		Ladder.Select(l => AchievementDefinitions.SecretDeedId(l.TeacherSecretId));

	public static bool IsTeacherDeed(string deedId) =>
		TeacherDeedIds.Any(id => id.Equals(deedId ?? string.Empty, StringComparison.OrdinalIgnoreCase));

	/// <summary>Whether the save has been taught <paramref name="system"/>, and may use it. Null is taught.</summary>
	public static bool HasLearned(SaveData data, Teaching system)
	{
		if (data == null)
			return true;
		if (system == Teaching.SovereignRite)
			return GlobalStatsManager.IsSpellUnlockedForLevelUp(data, SovereignRite.SpellId);
		// The Ascension's schema 20 flag still counts: a save that had it kept it (SaveData.AscensionLearned).
		if (system == Teaching.Ascension && data.AscensionLearned)
			return true;
		return HasBeenTaught(data, Get(system));
	}

	/// <summary>Whether the lesson's cutscene has been given (or the migration stood in for it).</summary>
	public static bool HasBeenTaught(SaveData data, Lesson lesson) =>
		data == null || lesson == null
		|| (data.LessonsLearned ?? new List<string>()).Any(id => id.Equals(lesson.Id, StringComparison.OrdinalIgnoreCase))
		|| (lesson.System == Teaching.Ascension && data.AscensionLearned);

	/// <summary>
	/// Whether the system may be SHOWN on this save, sealed or not (Q8, "one locked thing visible at a time"):
	/// the Ascension always; the Apotheosis once the Ascension is learned; Conjunctions once the Apotheosis
	/// is; the Rite once it is owned or Conjunctions are learned.
	/// </summary>
	public static bool IsShown(SaveData data, Teaching system) => system switch
	{
		Teaching.Ascension => true,
		Teaching.Apotheosis => HasLearned(data, Teaching.Ascension),
		Teaching.Conjunctions => HasLearned(data, Teaching.Apotheosis),
		_ => HasLearned(data, Teaching.SovereignRite) || HasLearned(data, Teaching.Conjunctions),
	};

	/// <summary>How many of the ladder's three teachers this save has found.</summary>
	public static int TeachersFound(SaveData data) =>
		data == null ? Ladder.Count : Ladder.Count(l => data.HasFoundSecret(l.TeacherSecretId)
			|| GlobalStatsManager.IsAchievementUnlocked(data, AchievementDefinitions.SecretDeedId(l.TeacherSecretId)));

	/// <summary>
	/// The lessons owed now and not yet given, in the order they are given: one rung of the ladder for every
	/// teacher found beyond the rungs already learned, lowest first, then the Rite once it is owned.
	/// </summary>
	public static List<Lesson> Owed(SaveData data)
	{
		var owed = new List<Lesson>();
		if (data == null)
			return owed;
		int learned = Ladder.Count(l => HasLearned(data, l.System));
		int credit = TeachersFound(data) - learned;
		foreach (Lesson rung in Ladder)
		{
			if (credit <= 0)
				break;
			if (HasLearned(data, rung.System))
				continue;
			owed.Add(rung);
			credit--;
		}
		if (HasLearned(data, Teaching.SovereignRite) && !HasBeenTaught(data, Rite))
			owed.Add(Rite);
		return owed;
	}

	/// <summary>Gives the lesson: the save has learned it. True the first time. The caller saves.</summary>
	public static bool Teach(SaveData data, Lesson lesson)
	{
		if (data == null || lesson == null || HasBeenTaught(data, lesson))
			return false;
		data.LessonsLearned ??= new List<string>();
		data.LessonsLearned.Add(lesson.Id);
		return true;
	}

	/// <summary>
	/// What a sealed system says it is waiting for, as an unmet requirement: the chapter of the first teacher
	/// not yet found, which is where the next rung is learned ("Learned from a secret of the Fallow Fields").
	/// The secret is not named: until it is seen it is sealed on the achievements page as well.
	/// </summary>
	public static string SealedLine(SaveData data, Teaching system)
	{
		if (system == Teaching.SovereignRite)
			return "Learned by defeating the Warden of the Deep";
		// Found this run: it is taught when the run ends, and the line says so rather than send him back out.
		if (Owed(data).Any(l => l.System == system))
			return "Learned at the end of a run";
		Lesson next = Ladder.FirstOrDefault(l => !TeacherFound(data, l)) ?? Ladder[0];
		SecretDefinition secret = SecretCatalog.Get(next.TeacherSecretId);
		return $"Learned from a secret of {AchievementDefinitions.ChapterPhrase(secret?.StageId ?? string.Empty)}";
	}

	private static bool TeacherFound(SaveData data, Lesson lesson) =>
		data != null && (data.HasFoundSecret(lesson.TeacherSecretId)
			|| GlobalStatsManager.IsAchievementUnlocked(data, AchievementDefinitions.SecretDeedId(lesson.TeacherSecretId)));
}
