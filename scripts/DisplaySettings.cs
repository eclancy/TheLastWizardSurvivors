using Godot;

namespace TheOneHeMissed.scripts;

// What the screen shows, as opposed to what the game does: the damage numbers and the screen shake.
//
// 2026-10-28: "add way to remove damage numbers from enemies". The numbers stay on by default - they
// are how a new player learns what a spell is worth - and the switch sits beside the audio controls
// on the main menu's Options and in the pause screen. Stored in the save like the volumes
// (SaveData.ShowDamageNumbers), and held here as a plain static the hit path reads, because
// Enemy.TakeDamage runs several times a physics step in a swarm and must not look a node up to ask.
//
// 2026-11-02: a "reduce screen shake" switch beside it (Eric, from the open-questions sweep). Reduced, not
// off: at a quarter strength a boss's landing still lands, it just no longer throws the picture about.
// Every shake reads ShakeScale - ScreenShake.Kick, and SpiritualWeapon's hammer, which shakes for itself.
public static class DisplaySettings
{
	/// <summary>How much of a shake is kept with the switch on.</summary>
	public const float ReducedShakeScale = 0.25f;

	/// <summary>Whether a hit on an enemy throws its number (Enemy.TakeDamage).</summary>
	public static bool DamageNumbers { get; private set; } = true;

	/// <summary>Whether screen shake is reduced (Options, and the pause screen's Display section).</summary>
	public static bool ReduceShake { get; private set; }

	/// <summary>What every screen shake's strength is multiplied by.</summary>
	public static float ShakeScale => ReduceShake ? ReducedShakeScale : 1f;

	/// <summary>Takes the saved switches. Called by SaveManager whenever its data is loaded or replaced.</summary>
	public static void ApplyFromSave(SaveData data)
	{
		if (data == null)
			return;
		DamageNumbers = data.ShowDamageNumbers;
		ReduceShake = data.ReduceScreenShake;
	}

	/// <summary>Flips the damage numbers and persists it. Safe to call when there is no SaveManager.</summary>
	public static void StoreDamageNumbers(Node context, bool show)
	{
		DamageNumbers = show;
		var saveManager = context?.GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager == null)
			return;
		saveManager.Data.ShowDamageNumbers = show;
		saveManager.SaveGame();
	}

	/// <summary>Flips the reduced screen shake and persists it. Safe to call when there is no SaveManager.</summary>
	public static void StoreReduceShake(Node context, bool reduce)
	{
		ReduceShake = reduce;
		var saveManager = context?.GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager == null)
			return;
		saveManager.Data.ReduceScreenShake = reduce;
		saveManager.SaveGame();
	}
}
