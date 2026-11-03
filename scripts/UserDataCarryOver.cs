using System;
using System.IO;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// Carries a player's user:// folder across the game's rename, once: on a desktop, the first launch
/// under the new name copies everything from the folder of the old name into its own.
/// </summary>
/// <remarks>
/// WHY THERE IS ANYTHING TO CARRY. On Windows, Linux and macOS Godot names the user:// folder after
/// application/config/name: %APPDATA%/Godot/app_userdata/&lt;name&gt;/, ~/.local/share/godot/app_userdata/&lt;name&gt;/.
/// The game was renamed on 2026-11-02 (Eric: "replace it with the new name of the game"), so from then on
/// it looks in a new, empty folder, and every save, setting, backup and shot from before is in the old one.
///
/// WHY A COPY, NOT application/config/use_custom_user_dir. A custom user dir is placed straight under
/// %APPDATA% (not under Godot/app_userdata), so pointing it back at the old folder takes a path, and a
/// different one per platform ("Godot" on Windows and macOS, "godot" on Linux) - and it would keep the old
/// name in project.godot for good. Copying gives the folder the game's name and leaves the old one exactly
/// as it was: a backup, and still the live folder of any copy not yet updated. See .ai/releasing.md,
/// "The rename".
///
/// THE RULES. Nothing is ever moved, deleted or overwritten: a file the new folder already has (this very
/// launch's log, say) is left alone. Nothing is copied into a folder that already holds a save it did not
/// get from here, and once the copy is done a marker says so, so a player who later deletes their save is
/// not handed the old one back. The save is copied last, which makes a copy cut short (a full disk, a
/// crash) finish on the next launch instead of stopping half-done. The updater's scratch folder is
/// skipped: a copy updating itself across the rename is started by a script that is still deleting it.
///
/// Android is untouched by all of it: there user:// is the app's private folder, named for the package
/// (com.ecec.wizardsurvivors, which never changes), not for the game. So is a run that may not write the
/// save (<see cref="SaveManager.WritesDisabled"/>): a tool run must not leave a stale copy behind for the
/// real first launch to trust.
/// </remarks>
public static class UserDataCarryOver
{
	/// <summary>
	/// The game's name until 0.12.0, and so the name of every desktop player's user:// folder from before
	/// the rename. Kept on purpose, and nowhere else in the game: it is where their saves are.
	/// </summary>
	public const string OldFolderName = "Wizard Survivors";

	/// <summary>Left in the new folder once the old one has been copied. Its presence means "done".</summary>
	public const string MarkerFileName = "carried-over.txt";

	private const string SaveFileName = "savegame.json";

	// Godot keeps every project's folder side by side in one of this name; anything else (Android's app
	// folder, a custom user dir) has no sibling of the old name to copy from.
	private const string GodotUserDataParent = "app_userdata";

	// Never copied. "update" is UpdateScreen's scratch folder (user://update): an old copy that has just
	// updated itself across the rename is started again by its apply script, which then deletes the
	// unpacked files from there while this copy runs.
	private static readonly string[] SkippedFolders = { "update" };

	public enum Outcome
	{
		/// <summary>There is no folder of the old name: a new player, or not a desktop.</summary>
		NoOldFolder,
		/// <summary>Copied already, on an earlier launch.</summary>
		AlreadyCarried,
		/// <summary>The new folder has a save of its own, so nothing is copied over it.</summary>
		NewFolderHasASave,
		/// <summary>Copied now.</summary>
		Carried,
	}

	/// <summary>
	/// Copies the old folder into this launch's user:// folder if it has not been already. Called by
	/// SaveManager before it loads the save, and only when this launch may write it.
	/// </summary>
	public static void Run()
	{
		string current = OS.GetUserDataDir();
		string parent = Path.GetDirectoryName(current);
		if (parent == null || Path.GetFileName(parent) != GodotUserDataParent)
			return;
		string old = Path.Combine(parent, OldFolderName);
		if (Path.GetFullPath(old) == Path.GetFullPath(current))
			return;

		try
		{
			(Outcome outcome, int files) = CarryOver(old, current);
			if (outcome == Outcome.Carried)
				GD.Print($"UserDataCarryOver: copied {files} files from {old} into {current}; the old folder is left as it was.");
			else if (outcome == Outcome.NewFolderHasASave && !File.Exists(Path.Combine(current, MarkerFileName)))
				GD.Print($"UserDataCarryOver: {current} already has a save of its own, so {old} was not copied into it.");
		}
		catch (Exception e)
		{
			// Loud, not fatal. Failing the launch would cost the player the game as well as the copy, and the
			// old folder is untouched, so nothing is lost: the save goes last, so the next launch finds no save
			// here and copies again.
			GD.PushError($"UserDataCarryOver: could not copy {old} into {current} ({e.GetType().Name}: {e.Message}). "
				+ "The old folder is untouched; the next launch tries again.");
		}
	}

	/// <summary>
	/// The whole copy, from <paramref name="oldDir"/> into <paramref name="newDir"/>, under the rules above.
	/// Path-driven so RegressionChecks can run this exact code against scratch folders. Throws on an I/O
	/// failure, having copied the save only if everything before it was copied.
	/// </summary>
	public static (Outcome Outcome, int Files) CarryOver(string oldDir, string newDir)
	{
		if (!Directory.Exists(oldDir))
			return (Outcome.NoOldFolder, 0);
		if (File.Exists(Path.Combine(newDir, MarkerFileName)))
			return (Outcome.AlreadyCarried, 0);
		if (File.Exists(Path.Combine(newDir, SaveFileName)))
			return (Outcome.NewFolderHasASave, 0);

		int files = CopyTree(oldDir, newDir, top: true);
		string oldSave = Path.Combine(oldDir, SaveFileName);
		if (File.Exists(oldSave))
		{
			File.Copy(oldSave, Path.Combine(newDir, SaveFileName), overwrite: false);
			files++;
		}
		File.WriteAllText(Path.Combine(newDir, MarkerFileName),
			$"Copied from {oldDir} on {DateTime.Now:yyyy-MM-dd HH:mm}, the first time the game ran under its new name.\n"
			+ "That folder was left exactly as it was. While this file is here, nothing is copied again.\n");
		return (Outcome.Carried, files);
	}

	private static int CopyTree(string from, string to, bool top)
	{
		Directory.CreateDirectory(to);
		int copied = 0;
		foreach (string file in Directory.GetFiles(from))
		{
			string name = Path.GetFileName(file);
			// The save goes last (CarryOver), so a copy cut short is redone rather than trusted.
			if (top && name == SaveFileName)
				continue;
			string target = Path.Combine(to, name);
			if (File.Exists(target))
				continue;
			File.Copy(file, target, overwrite: false);
			copied++;
		}
		foreach (string dir in Directory.GetDirectories(from))
		{
			string name = Path.GetFileName(dir);
			if (top && Array.IndexOf(SkippedFolders, name) >= 0)
				continue;
			copied += CopyTree(dir, Path.Combine(to, name), top: false);
		}
		return copied;
	}
}
