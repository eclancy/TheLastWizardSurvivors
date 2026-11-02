using Godot;

namespace WizardSurvivors.scripts;

public partial class SaveManager : Node
{
	public const string SavePath = "user://savegame.json";

	public SaveData Data { get; private set; } = new();

	/// <summary>
	/// When true, SaveGame does nothing - including the write-back after a migration on load. Set
	/// before the load for every tool-scene launch (<see cref="IsWriteProtectedLaunch"/>), and by
	/// tools that swap in an in-memory save they have altered, so a test run can never overwrite
	/// the player's real progress.
	/// </summary>
	public static bool WritesDisabled { get; set; }

	/// <summary>Replaces the in-memory save. Tools only; pair with <see cref="WritesDisabled"/>.</summary>
	public void ReplaceInMemory(SaveData data)
	{
		Data = data ?? new SaveData();
		DisplaySettings.ApplyFromSave(Data);
	}

	/// <summary>The user argument that keeps a whole launch from writing the save, migration included.</summary>
	public const string NoSaveWritesArg = "--no-save-writes";

	public override void _Ready()
	{
		// DECIDED BEFORE THE LOAD, not after it. LoadGame migrates an older save and writes the
		// result straight back, and this autoload is ready before any scene is: a tool scene that
		// sets WritesDisabled in its own _Ready is already too late for that write. On 2026-10-25 the
		// developer's real save was rewritten to a branch's schema 13, with a run of 6-second forced
		// defeats paid into it, by tool runs on branches still being built. A save written by a
		// harness, a probe or a screenshot is a save the player did not earn, and one migrated by a
		// branch that has not shipped is a schema the released build will never upgrade again.
		if (IsWriteProtectedLaunch())
			WritesDisabled = true;
		Data = LoadGame();
		// The display switches are read on hot paths (a damage number per hit), so they are held as
		// statics and taken from the save here, before any scene runs.
		DisplaySettings.ApplyFromSave(Data);
	}

	/// <summary>
	/// True when this launch must never write user://savegame.json: any run of a tool scene - every
	/// scene whose file name starts with an underscore (_UiShot, _BalanceHarness, _BossProbe,
	/// _ExploreProbe, _MazeProbe, ...) - or any run passed <c>-- --no-save-writes</c>.
	/// </summary>
	/// <remarks>
	/// Read from the command line because nothing else exists yet: the scene a headless or editor
	/// run opens is the one ".tscn" argument Godot hands through (OS.GetCmdlineArgs), and the main
	/// scene is not in the tree until after every autoload is ready. The game itself, launched with
	/// no scene argument, is the only thing that writes. A tool that needs a save to start from
	/// builds one in memory (ReplaceInMemory), which is what --all-unlocked and --fresh-save do.
	/// </remarks>
	public static bool IsWriteProtectedLaunch() =>
		System.Array.IndexOf(OS.GetCmdlineUserArgs(), NoSaveWritesArg) >= 0 || IsToolSceneLaunch();

	/// <summary>True when the scene this launch opens is a tool scene (its file name starts with "_").</summary>
	public static bool IsToolSceneLaunch()
	{
		foreach (string arg in OS.GetCmdlineArgs())
		{
			if (!arg.EndsWith(".tscn", System.StringComparison.OrdinalIgnoreCase)
				&& !arg.EndsWith(".scn", System.StringComparison.OrdinalIgnoreCase))
				continue;
			string normalized = arg.Replace('\\', '/');
			string fileName = normalized.Substring(normalized.LastIndexOf('/') + 1);
			if (fileName.StartsWith("_", System.StringComparison.Ordinal))
				return true;
		}
		return false;
	}

	/// <summary>
	/// Wipes progress back to a brand-new save and writes it immediately.
	/// </summary>
	/// <remarks>
	/// Needed because migration deliberately grandfathers pre-campaign saves into owning
	/// everything - which is right for a returning player and useless for testing, since it leaves
	/// no way to see the unlock flow from the start short of hand-editing the JSON. Destructive and
	/// irreversible: every caller must confirm first.
	/// </remarks>
	public void ResetProgress()
	{
		Data = new SaveData();
		SaveGame();
		GD.Print("SaveManager: progress reset to a new save.");
	}

	public void SaveGame()
	{
		if (WritesDisabled)
			return;
		WriteSave(SavePath, Data);
	}

	public SaveData LoadGame()
	{
		Data = LoadFrom(SavePath, writes: !WritesDisabled);
		return Data;
	}

	/// <summary>
	/// The whole load of the save at <paramref name="path"/>: read it, start the story again if it was
	/// earned in an earlier telling, migrate it, and write the result back when <paramref name="writes"/>.
	/// </summary>
	/// <remarks>
	/// Static and path-driven so RegressionChecks.ValidateStoryReset can run this exact code against a
	/// scratch file rather than a copy of it - the player's save is only ever the path LoadGame passes.
	/// <paramref name="writes"/> is explicit for the same reason: the check writes its scratch file while
	/// the run's <see cref="WritesDisabled"/> keeps the real one untouched.
	/// </remarks>
	public static SaveData LoadFrom(string path, bool writes, bool log = true)
	{
		if (!FileAccess.FileExists(path))
			return new SaveData();

		string json;
		using (FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read))
		{
			if (file == null)
			{
				GD.PushError($"Failed to open save file for reading: {path}");
				return new SaveData();
			}
			json = file.GetAsText();
		}

		Variant parsed = Json.ParseString(json);
		if (parsed.VariantType == Variant.Type.Nil)
		{
			GD.PushWarning("Save file exists but contains invalid JSON. Loading defaults.");
			return new SaveData();
		}

		SaveData data = SaveData.FromVariant(parsed);
		bool changed = false;

		// Before the migration, which would only be work done on progress about to be set aside. A save
		// from a NEWER epoch (a copy run back on an older build) is left alone, like a newer schema.
		if (data.IsFromEarlierStory)
		{
			SaveData restarted = StartNewStory(path, json, data, writes, log);
			if (restarted != null)
			{
				data = restarted;
				changed = true;
			}
		}

		// Before anything reads unlock state. A pre-campaign save has an empty UnlockedSpellIds
		// that means "everything was free", not "nothing was earned"; Migrate is what keeps that
		// from reading as an empty spellbook.
		if (data.Migrate(log))
			changed = true;

		if (changed && writes)
			WriteSave(path, data);
		return data;
	}

	/// <summary>
	/// Where a story reset keeps the save it replaced: beside it, named for the epoch being entered -
	/// user://savegame.before-epoch-2.json. Never overwritten: a second reset into the same epoch (only
	/// possible after running an older build in between) takes the next free -2, -3.
	/// </summary>
	public static string BackupPathFor(string savePath, int epoch)
	{
		string stem = savePath.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)
			? savePath.Substring(0, savePath.Length - ".json".Length)
			: savePath;
		string path = $"{stem}.before-epoch-{epoch}.json";
		for (int n = 2; FileAccess.FileExists(path); n++)
			path = $"{stem}.before-epoch-{epoch}-{n}.json";
		return path;
	}

	/// <summary>
	/// The story has been rebuilt since this save was earned (<see cref="SaveData.CurrentProgressEpoch"/>):
	/// keep the old file, word for word, and hand back a new save carrying only its settings. Null when
	/// the old save could not be kept - then nothing is reset, and the next launch tries again.
	/// </summary>
	/// <remarks>
	/// The backup is written BEFORE anything is reset and the reset is refused without it, because a story
	/// reset may take a player's progress out of play but must never take it out of existence. With writes
	/// off (every tool scene, --no-save-writes) the reset happens in memory only, so a harness sees what a
	/// player would, and neither file is touched.
	/// </remarks>
	private static SaveData StartNewStory(string path, string json, SaveData old, bool writes, bool log)
	{
		int from = old.ProgressEpoch;
		int to = SaveData.CurrentProgressEpoch;
		if (!writes)
		{
			if (log)
				GD.Print($"SaveManager: the save is from story epoch {from}, this build is {to}: progress restarted in memory only (writes are off).");
			return old.StartNewStory();
		}

		string backup = BackupPathFor(path, to);
		if (!WriteText(backup, json))
		{
			GD.PushError($"SaveManager: the save is from story epoch {from} and this build is {to}, but the old save " +
				$"could not be kept at {backup} - so it was NOT reset. The next launch tries again.");
			return null;
		}
		if (log)
			GD.Print($"SaveManager: story epoch {from} -> {to}: progress restarted, settings kept, old save kept at {backup}.");
		return old.StartNewStory();
	}

	private static void WriteSave(string path, SaveData data)
	{
		WriteText(path, Json.Stringify(data.ToGodotDictionary()));
	}

	private static bool WriteText(string path, string text)
	{
		using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PushError($"Failed to open save file for writing: {path}");
			return false;
		}

		file.StoreString(text);
		file.Flush();
		return file.GetError() == Error.Ok;
	}
}
