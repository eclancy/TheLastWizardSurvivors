using Godot;
using System;

namespace TheOneHeMissed.scripts;

/// <summary>
/// Manual save export and import: the save as a file a player can carry between devices by hand
/// (.ai/in-flight.md item 14's first stage - no accounts, no sync service, no hosting bill).
/// Export copies the save file somewhere the player chooses; import reads one back, shows what it
/// holds against what is here, and only replaces the save once the player has said so, keeping the
/// old file beside the new one.
/// </summary>
/// <remarks>
/// Everything here is path-driven and file-based, like SaveManager.LoadFrom, so
/// RegressionChecks.ValidateSaveTransfer runs this exact code against scratch files - the player's
/// save is only ever the path SaveManager passes.
///
/// Three rules from item 14's discussion:
/// - An import must not revive progress from before a story reset, and an older build's file must
///   not be guessed at: <see cref="Inspect"/> refuses an earlier epoch and anything from a newer
///   schema or epoch, before the player is ever asked to confirm.
/// - Settings stay the device's (SaveData.SettingsKeys): a phone's fullscreen or volume must not
///   follow the file onto a PC, so <see cref="ImportInto"/> writes the incoming progress with THIS
///   device's settings - taken from the save being replaced, or a new save's defaults when there is
///   none - mirroring what a story reset keeps.
/// - The replaced save is kept first (SaveManager.ImportBackupPathFor), and the import is refused
///   without it, the same bargain as a story reset: an import may set progress aside, never take it
///   out of existence.
///
/// Export reads the save FILE, never SaveManager.Data: a tool launch (--all-unlocked and friends)
/// swaps a fabricated save in memory, and progress the player did not earn must not be mintable as
/// a file. The Options screen also refuses both buttons outright while SaveManager.WritesDisabled.
/// </remarks>
public static class SaveTransfer
{
	public enum Verdict
	{
		Ok,
		/// <summary>Not JSON, not a dictionary, or missing the keys every save this game wrote has.</summary>
		NotASave,
		/// <summary>A newer schema or a newer story epoch: written by a newer build than this one.</summary>
		FromNewerBuild,
		/// <summary>From before the story was rebuilt (SaveData.CurrentProgressEpoch): importing it
		/// would revive progress a story reset deliberately set aside.</summary>
		FromEarlierStory,
	}

	/// <summary>What <see cref="Inspect"/> made of a file.</summary>
	public sealed class Inspection
	{
		public Verdict Verdict;
		/// <summary>The incoming save migrated in memory - exactly what the import would load - for
		/// the confirm screen's summary. Only on <see cref="Verdict.Ok"/>.</summary>
		public SaveData Preview;
		/// <summary>The file's text, word for word: what the import writes. Only on Ok.</summary>
		public string RawJson;
	}

	/// <summary>
	/// Reads a would-be save and says whether it can be imported. Refusals happen here, before any
	/// confirm screen, so the player is never asked to approve an import that cannot go ahead.
	/// </summary>
	public static Inspection Inspect(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return new Inspection { Verdict = Verdict.NotASave };
		Variant parsed = Json.ParseString(json);
		if (parsed.VariantType != Variant.Type.Dictionary)
			return new Inspection { Verdict = Verdict.NotASave };
		// Every save this game has ever written stores SchemaVersion. Without this check, any JSON
		// object would "load" as a fresh-looking save through FromVariant's property defaults - and
		// a missing key would default SchemaVersion to current, skipping every migration.
		if (!parsed.AsGodotDictionary().ContainsKey("SchemaVersion"))
			return new Inspection { Verdict = Verdict.NotASave };

		SaveData incoming = SaveData.FromVariant(parsed);
		// A newer build's file is left alone rather than guessed at, the same way LoadFrom leaves a
		// newer schema: FromVariant has already dropped the keys this build does not know, and
		// writing that back would quietly confiscate them.
		if (incoming.SchemaVersion > SaveData.CurrentSchemaVersion
			|| incoming.ProgressEpoch > SaveData.CurrentProgressEpoch)
			return new Inspection { Verdict = Verdict.FromNewerBuild };
		if (incoming.IsFromEarlierStory)
			return new Inspection { Verdict = Verdict.FromEarlierStory };

		// The preview the confirm screen shows must be what the player would actually get, so it is
		// migrated the way the load after the import will migrate the file.
		incoming.Migrate(log: false);
		return new Inspection { Verdict = Verdict.Ok, Preview = incoming, RawJson = json };
	}

	/// <summary>The refusal in the player's words, for the Options screen's status line.</summary>
	public static string MessageFor(Verdict verdict) => verdict switch
	{
		Verdict.NotASave => "That file is not a save from this game.",
		Verdict.FromNewerBuild => "That save is from a newer version of the game. Update this copy first.",
		Verdict.FromEarlierStory => "That save is from an earlier telling of the story and cannot be brought across.",
		_ => null,
	};

	/// <summary>
	/// Copies the save at <paramref name="savePath"/> to <paramref name="targetPath"/>, word for
	/// word. Null on success, otherwise what went wrong in the player's words.
	/// </summary>
	public static string ExportTo(string savePath, string targetPath)
	{
		if (!FileAccess.FileExists(savePath))
			return "There is no save to export yet.";
		string json = FileAccess.GetFileAsString(savePath);
		if (!WriteText(targetPath, json))
			return "The file could not be written there.";
		GD.Print($"SaveTransfer: exported {savePath} to {targetPath}.");
		return null;
	}

	/// <summary>
	/// The import itself: keeps the current save beside the new one, then writes the incoming
	/// progress carrying this device's settings. Null on success, otherwise what went wrong in the
	/// player's words - then the save on disk is unchanged. The caller reloads through
	/// SaveManager.LoadFrom afterwards, so an older file migrates exactly as a local save would.
	/// </summary>
	public static string ImportInto(string savePath, string incomingJson)
	{
		// Belt and braces: the UI inspected the file before confirming, but this is the last door.
		Inspection look = Inspect(incomingJson);
		if (look.Verdict != Verdict.Ok)
			return MessageFor(look.Verdict);

		var incoming = (Godot.Collections.Dictionary)Json.ParseString(incomingJson);

		// Settings stay the device's: every SettingsKeys value comes from the save being replaced,
		// or a new save's defaults when there is none - never from the file. Through the dictionary,
		// like SaveData.StartNewStory, so SettingsKeys IS the rule.
		bool hasCurrent = FileAccess.FileExists(savePath);
		string currentJson = hasCurrent ? FileAccess.GetFileAsString(savePath) : null;
		Variant localParsed = currentJson != null ? Json.ParseString(currentJson) : default;
		Godot.Collections.Dictionary local = localParsed.VariantType == Variant.Type.Dictionary
			? localParsed.AsGodotDictionary()
			: new SaveData().ToGodotDictionary();
		foreach (string key in SaveData.SettingsKeys)
		{
			if (local.ContainsKey(key))
				incoming[key] = local[key];
			else
				incoming.Remove(key);
		}

		// The backup is written BEFORE the save is touched, and the import is refused without it.
		if (hasCurrent)
		{
			string backup = SaveManager.ImportBackupPathFor(savePath);
			if (!WriteText(backup, currentJson))
			{
				GD.PushError($"SaveTransfer: the old save could not be kept at {backup}, so nothing was imported.");
				return "Your old save could not be kept beside the new one, so nothing was changed.";
			}
			GD.Print($"SaveTransfer: old save kept at {backup}.");
		}

		if (!WriteText(savePath, Json.Stringify(incoming)))
			return "The save could not be written. Your old save is untouched.";
		GD.Print($"SaveTransfer: imported a save into {savePath}.");
		return null;
	}

	/// <summary>What the confirm screen compares, one side at a time, in the player's words.</summary>
	public sealed class Summary
	{
		public string Playtime;
		public int Victories;
		public int Wizards;
		public int Chapters;
		public int Achievements;
		public int Currency;
	}

	public static Summary Summarize(SaveData save) => new()
	{
		Playtime = FormatPlaytime(save?.Lifetime?.TotalPlaytimeSeconds ?? 0f),
		Victories = save?.Lifetime?.TotalVictories ?? 0,
		Wizards = save?.UnlockedCharacterIds.Count ?? 0,
		Chapters = save?.UnlockedStageIds.Count ?? 0,
		Achievements = save?.UnlockedAchievementIds.Count ?? 0,
		Currency = save?.TotalCurrency ?? 0,
	};

	/// <summary>"47h 23m", or "23m" under an hour - a comparison figure, not a stopwatch.</summary>
	public static string FormatPlaytime(float seconds)
	{
		int minutes = (int)(Math.Max(0f, seconds) / 60f);
		return minutes >= 60 ? $"{minutes / 60}h {minutes % 60:00}m" : $"{minutes}m";
	}

	private static bool WriteText(string path, string text)
	{
		using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PushError($"SaveTransfer: could not open {path} for writing ({FileAccess.GetOpenError()}).");
			return false;
		}
		file.StoreString(text);
		file.Flush();
		return file.GetError() == Error.Ok;
	}
}
