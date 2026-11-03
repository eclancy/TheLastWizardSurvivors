using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>
/// The freeings: the short cutscene after the run that frees one of the wizards a chapter holds - the Archmage
/// (the one he missed), beside the wizard he has just broken out, that wizard's own light coming back to him (.ai/cutscenes.md;
/// the art is tools/art/freed.py, one template, and Prologue.Reels plays it as "freed_&lt;id&gt;"). Eric,
/// 2026-11-02: "We need to add cutscenes to help the player identify with the blue robed character."
/// </summary>
/// <remarks>
/// <para><b>Owed, not "seen".</b> A wizard is owed his reel the moment a deed frees him
/// (AchievementManager.GrantUnlocks, <see cref="RecordFreed"/>), and the debt is written into the save
/// (<see cref="SaveData.FreeingsOwed"/>), so a run quit after the cell was opened still pays it at the end of the
/// next. It is paid when the reel ends, played through or skipped (<see cref="MarkShown"/>), over the paused run
/// before the results and before any lesson (Node2DGame.Lessons.cs). A "seen" list would have owed every wizard an
/// existing save had already freed - six reels in a row at its next run's end - unless a migration wrote them
/// all seen; a list that only a new freeing writes needs neither, and defaults to nothing owed.</para>
///
/// <para><b>Who.</b> The wizards held in a place and freed there (<see cref="WizardIds"/>): each chapter's
/// prisoner and Laelaps, set in stone on the heath. Not John Dee, who is freed by collecting and never stood in a
/// place to be seen freed; not Merlin or the dark wizard, who were never held. A wizard added here needs a row in
/// tools/art/freed.py's WIZARDS and a freed line in QuestLog.Prisoners, which the reel says
/// (RegressionChecks.ValidateFreeings says what is missing).</para>
/// </remarks>
public static class Freeings
{
	/// <summary>The wizards with a freeing reel, in the order freed.py draws them.</summary>
	public static readonly IReadOnlyList<string> WizardIds = new[]
	{
		"tituba", "vainamoinen", "seimei", "wayland", "sibyl", "laelaps", "baba_yaga", "morgan", "paracelsus", "nostradamus",
		// The one he kept (.ai/endgame-castle.md decision 11): freed by his fall in the Throne, so her reel plays after the
		// ending, not before the results (GameOverScreen.AfterEnding).
		"nimue",
	};

	/// <summary>A freeing's reel id in Prologue.Reels: "freed_tituba".</summary>
	public const string ReelPrefix = "freed_";

	public static string ReelIdFor(string wizardId) => ReelPrefix + (wizardId ?? string.Empty).Trim().ToLowerInvariant();

	/// <summary>The wizard a freeing reel shows, or empty when <paramref name="reelId"/> is not a freeing.</summary>
	public static string WizardOfReel(string reelId) =>
		reelId != null && reelId.StartsWith(ReelPrefix, StringComparison.OrdinalIgnoreCase)
			? reelId.Substring(ReelPrefix.Length)
			: string.Empty;

	public static bool HasReel(string wizardId) =>
		WizardIds.Any(id => id.Equals((wizardId ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// A deed has just freed <paramref name="wizardId"/>: his reel is owed. Nothing for a wizard with no reel, or one
	/// already owed. The caller saves (CompleteNow does at once; a run's end does with everything else).
	/// </summary>
	public static void RecordFreed(SaveData data, string wizardId)
	{
		if (data == null || !HasReel(wizardId))
			return;
		data.FreeingsOwed ??= new List<string>();
		string id = wizardId.Trim().ToLowerInvariant();
		if (!data.FreeingsOwed.Contains(id, StringComparer.OrdinalIgnoreCase))
			data.FreeingsOwed.Add(id);
	}

	/// <summary>The reels owed now, in the order their wizards were freed: each one free and with a reel.</summary>
	public static List<string> Owed(SaveData data)
	{
		if (data?.FreeingsOwed == null)
			return new List<string>();
		return data.FreeingsOwed
			.Where(id => HasReel(id) && GlobalStatsManager.IsCharacterUnlocked(data, id))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	/// <summary>The reel has been shown (or skipped): the debt is paid. True when it was owed. The caller saves.</summary>
	public static bool MarkShown(SaveData data, string wizardId)
	{
		if (data?.FreeingsOwed == null || string.IsNullOrWhiteSpace(wizardId))
			return false;
		return data.FreeingsOwed.RemoveAll(id => id.Equals(wizardId.Trim(), StringComparison.OrdinalIgnoreCase)) > 0;
	}

	/// <summary>
	/// The freeings Options lists to watch again: every wizard with a reel this save has freed, owed or not. A
	/// wizard still held is never listed - his name is his reward (QuestLog).
	/// </summary>
	public static IEnumerable<string> Watchable(SaveData data) =>
		data == null ? Enumerable.Empty<string>() : WizardIds.Where(id => GlobalStatsManager.IsCharacterUnlocked(data, id));
}
