using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// The cutscenes' checks (.ai/cutscenes.md): the prologue's panels on disk, every freeing's reel, line and debt.
// Every line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	// THE PROLOGUE'S PANELS. Every layer of every beat imported, and an animated box's atlas beside its manifest:
	// a panel missing from disk draws nothing and says so only once it is reached, 30 seconds into a first run.
	private static void ValidatePrologueReel(List<string> warnings)
	{
		Prologue.Reel reel = Prologue.Reels[Prologue.PrologueReelId];
		foreach (Prologue.Beat beat in reel.Beats)
		{
			foreach (string layer in new[] { "back", "mid", "fore" })
			{
				string path = Prologue.LayerPathFor(reel, beat, layer);
				if (!ResourceLoader.Exists(path))
					warnings.Add($"Prologue: panel {path} is missing or not imported (python tools/art/prologue.py {beat.Name}, then import).");
			}
			string manifest = Prologue.LayerPathFor(reel, beat, "anim").Replace(".png", ".json");
			if (FileAccess.FileExists(manifest) && !ResourceLoader.Exists(Prologue.LayerPathFor(reel, beat, "anim")))
				warnings.Add($"Prologue: '{beat.Name}' has an animation manifest and no imported atlas beside it.");
			if (!string.IsNullOrEmpty(beat.Caption) && beat.Caption.Length > 46)
				warnings.Add($"Prologue: '{beat.Name}' says more than one line on a PC (46 characters).");
		}
		// Three lines were the prologue's rule, and the freeing adds one (Eric: "Overall keep text minimal"); the moot's
		// adds a fifth, where the story begins (Eric, 2026-11-03: "The stones where we used to meet.").
		int lines = reel.Beats.Count(b => !string.IsNullOrEmpty(b.Caption));
		if (lines > 5)
			warnings.Add($"Prologue: {lines} panels carry a line; it was written for five at most.");
	}

	// THE FREEINGS (Freeings). Every wizard with a reel is a real, freeable character with a freed line that fits one
	// line on a PC once his name is in it; every panel is on disk; every wizard a place holds has one; and a deed
	// that frees one owes his reel, which a save keeps and paying it clears.
	private static void ValidateFreeings(List<string> warnings)
	{
		foreach (string wizardId in Freeings.WizardIds)
		{
			UnlockDefinition who = UnlockCatalog.GetCharacter(wizardId);
			if (who == null)
			{
				warnings.Add($"Freeings: '{wizardId}' has a reel and no UnlockCatalog entry.");
				continue;
			}
			if (!Prologue.Reels.TryGetValue(Freeings.ReelIdFor(wizardId), out Prologue.Reel reel))
			{
				warnings.Add($"Freeings: '{wizardId}' has no reel in Prologue.Reels.");
				continue;
			}
			foreach (Prologue.Beat beat in reel.Beats)
			{
				foreach (string layer in new[] { "back", "mid", "fore" })
				{
					string path = Prologue.LayerPathFor(reel, beat, layer);
					if (!ResourceLoader.Exists(path))
						warnings.Add($"Freeings: {path} is missing or not imported (python tools/art/freed.py {wizardId}, then import).");
				}
				string line = Prologue.CaptionFor(reel, beat);
				if (string.IsNullOrWhiteSpace(line) || line.Contains("{0}") || line.Length > 46)
					warnings.Add($"Freeings: '{wizardId}' needs one short line, his freed line with his name in it, 46 characters at most (got '{line}').");
			}
		}

		// Every wizard a place holds - a prisoner with a held line - is owed a freeing when he is freed.
		foreach (QuestLog.Prisoner prisoner in QuestLog.Prisoners.Where(p => !string.IsNullOrEmpty(p.HeldLine)))
		{
			if (!Freeings.HasReel(prisoner.CharacterId))
				warnings.Add($"Freeings: {prisoner.Epithet} ('{prisoner.CharacterId}') is held in a place and has no freeing reel (tools/art/freed.py WIZARDS, Freeings.WizardIds).");
		}

		// The debt, on sample saves: owed by the deed that frees him, kept by a save and a load, paid by the reel, and
		// never owed by a fresh save or by a wizard freed before the reels existed.
		var fresh = new SaveData();
		if (Freeings.Owed(fresh).Count > 0)
			warnings.Add("Freeings: a new save owes a freeing.");
		var old = new SaveData();
		GlobalStatsManager.UnlockCharacter(old, "tituba");
		if (Freeings.Owed(old).Count > 0)
			warnings.Add("Freeings: a wizard freed before the reels existed (no debt in the save) is owed his reel.");

		AchievementDefinition deed = AchievementDefinitions.All.FirstOrDefault(d =>
			Freeings.HasReel(d.CharacterUnlockId) && d.CompletesInRun);
		if (deed == null)
		{
			warnings.Add("Freeings: no deed completed in a run frees a wizard with a reel; nothing would ever owe one.");
			return;
		}
		var save = new SaveData();
		AchievementManager.Complete(save, deed.Id);
		if (!Freeings.Owed(save).Contains(deed.CharacterUnlockId, StringComparer.OrdinalIgnoreCase))
			warnings.Add($"Freeings: completing '{deed.Id}' frees {deed.CharacterUnlockId} and does not owe his reel.");
		SaveData reloaded = SaveData.FromVariant(save.ToGodotDictionary());
		if (!Freeings.Owed(reloaded).Contains(deed.CharacterUnlockId, StringComparer.OrdinalIgnoreCase))
			warnings.Add("Freeings: a freeing owed is lost between a save and a load.");
		AchievementManager.Complete(reloaded, deed.Id);
		if (reloaded.FreeingsOwed.Count(id => id.Equals(deed.CharacterUnlockId, StringComparison.OrdinalIgnoreCase)) != 1)
			warnings.Add("Freeings: completing the same deed twice owes the reel twice.");
		if (!Freeings.MarkShown(reloaded, deed.CharacterUnlockId) || Freeings.Owed(reloaded).Count > 0)
			warnings.Add("Freeings: playing the reel does not pay the debt.");
	}
}
