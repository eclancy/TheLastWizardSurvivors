using Godot;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

public static partial class RegressionChecks
{
	// Manual save export/import (SaveTransfer, .ai/in-flight.md item 14), run for real against
	// scratch files of this process's own - never the player's save:
	//   - Inspect refuses what must never be imported: not a save, a newer build's file (schema or
	//     epoch), a file from before the story was rebuilt - and accepts a real one, migrated for
	//     its preview;
	//   - export is the save file word for word;
	//   - import keeps the old save beside the new one word for word, never overwriting an earlier
	//     backup, carries the progress across, keeps THIS device's settings (SaveData.SettingsKeys),
	//     and an older-schema file migrates exactly as a local save would;
	//   - importing onto a device with no save gives the file a new save's settings, not its own;
	//   - a refused import changes nothing on disk.
	private static void ValidateSaveTransfer(List<string> warnings)
	{
		// In memory: the verdicts.
		if (SaveTransfer.Inspect(null).Verdict != SaveTransfer.Verdict.NotASave
			|| SaveTransfer.Inspect("not json at all").Verdict != SaveTransfer.Verdict.NotASave
			|| SaveTransfer.Inspect("[1, 2, 3]").Verdict != SaveTransfer.Verdict.NotASave
			|| SaveTransfer.Inspect("{\"TotalCurrency\": 5}").Verdict != SaveTransfer.Verdict.NotASave)
			warnings.Add("Save transfer: something that is not a save passed inspection.");

		string JsonOf(SaveData save) => Json.Stringify(save.ToGodotDictionary());

		var newerSchema = new SaveData { SchemaVersion = SaveData.CurrentSchemaVersion + 1 };
		if (SaveTransfer.Inspect(JsonOf(newerSchema)).Verdict != SaveTransfer.Verdict.FromNewerBuild)
			warnings.Add("Save transfer: a newer build's schema was not refused.");
		var newerEpoch = new SaveData { ProgressEpoch = SaveData.CurrentProgressEpoch + 1 };
		if (SaveTransfer.Inspect(JsonOf(newerEpoch)).Verdict != SaveTransfer.Verdict.FromNewerBuild)
			warnings.Add("Save transfer: a newer story epoch was not refused.");
		var earlierStory = new SaveData { ProgressEpoch = SaveData.CurrentProgressEpoch - 1 };
		if (SaveTransfer.Inspect(JsonOf(earlierStory)).Verdict != SaveTransfer.Verdict.FromEarlierStory)
			warnings.Add("Save transfer: a save from before the story was rebuilt was not refused - an import could revive reset progress.");

		// A pre-campaign file (schema 7, empty unlocks meant "everything was free") must preview as
		// the migration would grant it, not as an empty spellbook.
		var preCampaign = new SaveData { SchemaVersion = SaveData.CampaignSchemaVersion - 1 };
		preCampaign.UnlockedSpellIds.Clear();
		SaveTransfer.Inspection migratedLook = SaveTransfer.Inspect(JsonOf(preCampaign));
		if (migratedLook.Verdict != SaveTransfer.Verdict.Ok)
			warnings.Add("Save transfer: an older-schema save was refused instead of migrated.");
		else if (migratedLook.Preview.SchemaVersion != SaveData.CurrentSchemaVersion
			|| migratedLook.Preview.UnlockedSpellIds.Count == 0)
			warnings.Add("Save transfer: the preview of an older-schema save was not migrated, so the confirm screen would show the wrong numbers.");

		foreach (SaveTransfer.Verdict refusal in new[] { SaveTransfer.Verdict.NotASave, SaveTransfer.Verdict.FromNewerBuild, SaveTransfer.Verdict.FromEarlierStory })
		{
			if (string.IsNullOrWhiteSpace(SaveTransfer.MessageFor(refusal)))
				warnings.Add($"Save transfer: the refusal {refusal} has no words for the player.");
		}

		if (!OS.IsDebugBuild())
			return;

		// On disk, through the real code. Never SaveManager.ActiveSavePath: a scratch folder of this
		// process's own.
		string dir = $"user://save-transfer-check-{OS.GetProcessId()}";
		string path = dir + "/savegame.json";
		DirAccess.MakeDirRecursiveAbsolute(dir);
		try
		{
			SaveData Device()
			{
				// Settings off their defaults, so "kept" cannot pass by coincidence; progress that
				// the file must replace.
				var s = new SaveData
				{
					TotalCurrency = 111, MasterVolume = 0.3f, Fullscreen = false, WindowWidth = 1280, WindowHeight = 800,
					PlaytestModeEnabled = true, BalancePresetId = GlobalStatsManager.BalancePresetHardcore,
				};
				s.UnlockedSpellIds.Add("gravewell");
				s.Lifetime.TotalRuns = 2;
				return s;
			}
			SaveData FileSave()
			{
				// The other device: different settings (which must NOT come across) and more progress
				// (which must).
				var s = new SaveData
				{
					TotalCurrency = 999, MasterVolume = 0.9f, Fullscreen = true,
					PlaytestModeEnabled = false, BalancePresetId = GlobalStatsManager.BalancePresetCasual,
				};
				s.UnlockedSpellIds.Add("starfall");
				s.UnlockedCharacterIds.Add("hayward");
				s.UnlockedStageIds.Add("stage_1");
				s.Lifetime.TotalRuns = 40;
				s.Lifetime.TotalVictories = 7;
				s.Lifetime.TotalPlaytimeSeconds = 7200f;
				return s;
			}

			string deviceText = JsonOf(Device());
			void StartDevice()
			{
				using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
				file?.StoreString(deviceText);
			}

			// Export: the file word for word.
			StartDevice();
			string exported = dir + "/exported.json";
			if (SaveTransfer.ExportTo(path, exported) != null || FileAccess.GetFileAsString(exported) != deviceText)
				warnings.Add("Save transfer (disk): the export is not the save file word for word.");
			if (SaveTransfer.ExportTo(dir + "/no-such-save.json", dir + "/never.json") == null)
				warnings.Add("Save transfer (disk): exporting a save that does not exist did not refuse.");

			// Import: backup, progress across, settings local.
			string incomingText = JsonOf(FileSave());
			string backup = SaveManager.ImportBackupPathFor(path);
			if (SaveTransfer.ImportInto(path, incomingText) != null)
				warnings.Add("Save transfer (disk): a good import was refused.");
			if (!FileAccess.FileExists(backup) || FileAccess.GetFileAsString(backup) != deviceText)
				warnings.Add($"Save transfer (disk): the replaced save was not kept word for word at {backup}.");
			SaveData loaded = SaveManager.LoadFrom(path, writes: true, log: false);
			if (loaded.TotalCurrency != 999 || !loaded.UnlockedSpellIds.Contains("starfall")
				|| loaded.Lifetime.TotalRuns != 40 || loaded.UnlockedCharacterIds.Count != 1)
				warnings.Add("Save transfer (disk): the file's progress did not come across.");
			if (loaded.MasterVolume != 0.3f || loaded.Fullscreen || !loaded.PlaytestModeEnabled
				|| loaded.BalancePresetId != GlobalStatsManager.BalancePresetHardcore
				|| loaded.WindowWidth != 1280 || loaded.WindowHeight != 800)
				warnings.Add("Save transfer (disk): the import changed this device's settings - SaveData.SettingsKeys must stay local.");

			// A second import must take the next backup name, never overwrite the first.
			string secondBackup = SaveManager.ImportBackupPathFor(path);
			if (secondBackup == backup)
				warnings.Add("Save transfer (disk): a second import would overwrite the first backup.");
			if (SaveTransfer.ImportInto(path, incomingText) != null || !FileAccess.FileExists(secondBackup)
				|| FileAccess.GetFileAsString(backup) != deviceText)
				warnings.Add("Save transfer (disk): the second import did not keep both backups intact.");

			// An older-schema file migrates on import exactly as a local save would, and the write-back
			// leaves it current, so the next launch has nothing to do.
			var oldFile = new SaveData { SchemaVersion = SaveData.CampaignSchemaVersion - 1, TotalCurrency = 42 };
			oldFile.UnlockedSpellIds.Clear();
			if (SaveTransfer.ImportInto(path, JsonOf(oldFile)) != null)
				warnings.Add("Save transfer (disk): an older-schema import was refused.");
			SaveData migrated = SaveManager.LoadFrom(path, writes: true, log: false);
			if (migrated.SchemaVersion != SaveData.CurrentSchemaVersion || migrated.UnlockedSpellIds.Count == 0)
				warnings.Add("Save transfer (disk): an imported older-schema save did not migrate as a local one does.");
			SaveData reread = SaveManager.LoadFrom(path, writes: false, log: false);
			if (reread.SchemaVersion != SaveData.CurrentSchemaVersion)
				warnings.Add("Save transfer (disk): the imported save was not rewritten at the current schema.");

			// A refused import changes nothing: the save stays, no new backup appears.
			string beforeRefusal = FileAccess.GetFileAsString(path);
			string wouldBeBackup = SaveManager.ImportBackupPathFor(path);
			if (SaveTransfer.ImportInto(path, JsonOf(earlierStory)) == null)
				warnings.Add("Save transfer (disk): an earlier-story import went ahead.");
			if (FileAccess.GetFileAsString(path) != beforeRefusal || FileAccess.FileExists(wouldBeBackup))
				warnings.Add("Save transfer (disk): a refused import still touched the disk.");

			// A device with no save yet: the file's progress arrives, the file's settings do not -
			// a new save's defaults stand in, because settings are the device's.
			string freshPath = dir + "/fresh/savegame.json";
			DirAccess.MakeDirRecursiveAbsolute(dir + "/fresh");
			if (SaveTransfer.ImportInto(freshPath, incomingText) != null)
				warnings.Add("Save transfer (disk): importing onto a device with no save was refused.");
			SaveData fresh = SaveManager.LoadFrom(freshPath, writes: false, log: false);
			var defaults = new SaveData();
			if (fresh.TotalCurrency != 999 || fresh.MasterVolume != defaults.MasterVolume
				|| fresh.Fullscreen != defaults.Fullscreen || fresh.BalancePresetId != defaults.BalancePresetId)
				warnings.Add("Save transfer (disk): a first import did not pair the file's progress with this device's default settings.");
		}
		finally
		{
			foreach (string sub in new[] { dir + "/fresh", dir })
			{
				using DirAccess folder = DirAccess.Open(sub);
				if (folder != null)
				{
					foreach (string file in folder.GetFiles())
						folder.Remove(file);
				}
				DirAccess.RemoveAbsolute(sub);
			}
		}
	}
}
