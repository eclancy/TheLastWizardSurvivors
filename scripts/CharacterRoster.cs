using Godot;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// Central character roster (issue #29): single source of truth for both CharacterSelection.cs
// (issue #1's dynamic refactor) and Player.cs, replacing CharacterSelection's previous hardcoded
// stub list that didn't correspond to any real CharacterData resource.
public static class CharacterRoster
{
	// Test Wizard stays first for development flows (it is the only entry that can pick its own
	// starting loadout); the four after it are the first four starters, one per starting element. The
	// fifth, The Archmage, is appended at index 14 (below), and the character select shows him first by
	// its own order (SelectionOrder), not by this one.
	//
	// CharacterData.tres - the Arcane "Apprentice Wizard" - is deliberately no longer listed. It
	// started with Magic Missile when that carried Arcane and Lightning weight, so it was the one
	// character whose opening spell pulled toward two elements at once. Magic Missile is pure Arcane
	// since 2026-11-02 and The Archmage opens on it; the file is still on disk, unlisted.
	private static readonly string[] ResourcePaths = new[]
	{
		"res://CharacterData_TestWizard.tres",
		"res://CharacterData_Pyromancer.tres",
		"res://CharacterData_Frostweaver.tres",
		"res://CharacterData_Stormcaller.tres",
		"res://CharacterData_Geomancer.tres",
		// Rescued in side events (.ai/side-events.md). Listed here like everyone else; whether the
		// player can pick them is UnlockCatalog's question, not the roster's.
		"res://CharacterData_Tituba.tres",
		"res://CharacterData_Vainamoinen.tres",
		// Freed by collecting (the "mortlake_library" achievement): the collector, on Hollow Star.
		// Appended, never inserted - Global.SelectedCharacterIdx and the balance harness's
		// --character=N both index this list.
		"res://CharacterData_JohnDee.tres",
		// Freed with the Pinfold's three companions: Laelaps, the hound, on Cyclone Slash. Appended
		// for the reason John Dee was.
		"res://CharacterData_Laelaps.tres",
		// The chapter wizards (.ai/chapter-wizards.md), appended so every saved index keeps its wizard.
		"res://CharacterData_Seimei.tres",
		// Taken on the hardest road (.ai/dark-wizard-playable.md): the dark wizard himself. Appended, as everyone
		// after the four is, so every saved index keeps its wizard (index 10).
		"res://CharacterData_DarkWizard.tres",
		// Merlin, the old enchanter (.ai/merlin.md), in a look of his own (the title screen's blue-robed figure is the
		// player), playable once every chapter has fallen on Default or harder. Appended (index 11).
		"res://CharacterData_Merlin.tres",
		// The Emberdeep's chapter wizard (.ai/chapter-wizards.md 4.7): Wayland the Smith, chained to his own anvil.
		// Appended, so every saved index keeps its wizard (index 12: Merlin took 11 the same day).
		"res://CharacterData_Wayland.tres",
		// The chapter wizards again: the Cumaean Sibyl, freed at the nine mouths in the Sunken Cave. Appended (index 13: Merlin and Wayland took 11 and 12 the same day).
		"res://CharacterData_Sibyl.tres",
		// The Archmage (.ai/protagonist.md): the title screen's blue-robed figure, the player, the one the dark wizard
		// missed - the fifth starter (Eric, 2026-11-02), on Magic Missile. APPENDED, never inserted, although he is
		// first on the character select: a saved index and --character=N must keep their wizard (index 14). The
		// screen's order is SelectionOrder's.
		"res://CharacterData_Archmage.tres",
		// The chapter wizards again: Baba Yaga, freed by running her walking hut down in the Enchanted Forest. Appended,
		// so every saved index keeps its wizard (index 15: the Archmage took 14).
		"res://CharacterData_BabaYaga.tres",
		// The Last Bridge's chapter wizard (.ai/chapter-wizards.md 4.9): Morgan le Fay, freed from her barge under the
		// span. Appended, so every saved index keeps its wizard (index 16).
		"res://CharacterData_Morgan.tres",
		// The chapter wizards again: Paracelsus, brought out of the gibbet's cage to the ferry landing in the Blighted
		// Swamp. Appended, so every saved index keeps its wizard (index 17).
		"res://CharacterData_Paracelsus.tres",
		// The chapter wizards again: Nostradamus, freed by seeking his voice from room to room of the Court until he
		// is found at his desk (.ai/chapter-wizards.md 4.10). Appended, so every saved index keeps its wizard (index 18).
		"res://CharacterData_Nostradamus.tres",
		// The chapter wizards again: Imhotep, freed by kindling the three beacons round his tomb on the Scorched Sands.
		// Appended, so every saved index keeps its wizard (index 19).
		"res://CharacterData_Imhotep.tres",
		// The chapter wizards again: Medea, freed by paying the Tithe-Reeve's scale in the Broken Circle. Appended, so every
		// saved index keeps its wizard (index 20).
		"res://CharacterData_Medea.tres",
		// The Throne's kept wizard (.ai/endgame-castle.md decision 11): Nimue, the Lady of the Lake, the one the dark
		// wizard kept by him, freed when he falls. Appended, so every saved index keeps its wizard (index 21).
		"res://CharacterData_Nimue.tres",
		// The chapter wizards again: Ma Liang, the Court's second, freed by tracing the door he sketched on the Gallery's end
		// wall, dot to dot (.ai/chapter-wizards.md 4.8). Appended, so every saved index keeps its wizard (index 22).
		"res://CharacterData_MaLiang.tres",
	};

	/// <summary>
	/// Who the character select shows first, by id, ahead of everyone else (who follow in roster order). Eric,
	/// 2026-11-02: The Archmage "should be the first one on the list on the character select screen". Kept apart
	/// from the roster's own order on purpose: the roster index is what a save, Global.SelectedCharacterIdx and the
	/// tools' --character=N name, so a wizard who moves on the screen must never move in the list.
	/// </summary>
	private static readonly string[] ShownFirst = { "archmage" };

	/// <summary>
	/// Every roster index in the order the character select lays its cards out: <see cref="ShownFirst"/>, then the
	/// rest in roster order. A display order only - nothing stores a position in it.
	/// </summary>
	public static List<int> SelectionOrder()
	{
		var all = GetAll();
		var order = new List<int>(all.Count);
		foreach (string id in ShownFirst)
		{
			int index = all.FindIndex(c => c != null && c.Id.Equals(id, System.StringComparison.OrdinalIgnoreCase));
			if (index >= 0)
				order.Add(index);
		}
		for (int i = 0; i < all.Count; i++)
		{
			if (!order.Contains(i))
				order.Add(i);
		}
		return order;
	}

	/// <summary>The roster index of the character with this id, or -1.</summary>
	public static int IndexOf(string characterId) =>
		GetAll().FindIndex(c => c != null && c.Id.Equals(characterId ?? string.Empty, System.StringComparison.OrdinalIgnoreCase));

	// Loaded once and held. Every call used to load all seven again, and each load hands back a
	// fresh managed wrapper for the same cached Godot resource; the old wrappers were left to the
	// finalizer. On Android's Mono runtime a finalizer disposing one of them while ResourceLoader
	// was handing out the same resource is a FATAL "gchandle.is_released()" - the game died on the
	// title screen, inside RegressionChecks, which calls this many times in a row. Desktop had met
	// the same churn as a finalizer crash in the balance bot. Holding the list keeps the wrappers
	// alive for the life of the process, which is what a roster of seven is anyway.
	private static List<CharacterData> loaded;

	public static List<CharacterData> GetAll()
	{
		// A copy, so a caller that sorts or filters its list cannot reorder the roster for everyone.
		if (loaded != null)
			return new List<CharacterData>(loaded);

		var result = new List<CharacterData>();
		foreach (var path in ResourcePaths)
		{
			// Plain load (default CacheMode.Reuse) - matches every other resource load in this
			// codebase (SpellData etc. in Player.cs). CacheMode.Replace was tried here previously
			// as a speculative fix for a suspected stale-cache bug, but it turned out to be the
			// actual cause of a real bug: on the FIRST load of a resource in a fresh process,
			// Replace mode returns a blank/default-constructed instance instead of the
			// deserialized .tres data (confirmed via runtime diagnostic logging - every exported
			// property came back as the C# class's bare default, e.g. Id="", Name="",
			// StartingSpellResource=null). Reuse mode works correctly and is what the rest of the
			// project already relies on.
			var data = ResourceLoader.Load<CharacterData>(path);
			if (data != null)
				result.Add(data);
		}
		loaded = result;
		return new List<CharacterData>(loaded);
	}

	public static CharacterData GetByIndex(int index)
	{
		var all = GetAll();
		if (all.Count == 0)
			return null;
		if (index < 0 || index >= all.Count)
			return all[0];
		return all[index];
	}
}
