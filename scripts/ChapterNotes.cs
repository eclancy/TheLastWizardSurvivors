using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

/// <summary>
/// The note the player's wizard writes to himself the first time he walks into a chapter: the place, how
/// it smells or feels, and who is held there (.ai/quests-and-notes.md holds the text and why).
/// </summary>
/// <remarks>
/// Eric, 2026-10-31: "Starting every new map can have a little context as well, such as the players wizard
/// making a small note to themselves about the place they are in, what it smells like or feels like, which
/// wizard is in this location that they're trying to rescue" - and "Overall keep text minimal". So two
/// lines a chapter, and a third only where someone is held. The held line is not written here: it is the
/// prisoner's (QuestLog.Prisoners), placed in whichever chapter his rescue deed belongs to, so a wizard
/// whose chapter moves (Tituba, into the castle) takes his line with him. It names him by epithet until
/// he is free, and by name after - which only the journal, reread later, ever shows.
///
/// Shown once per chapter (SaveData.SeenChapterNoteIds) by ChapterNoteCard, and kept in the journal
/// (JournalPage). A playable chapter with no note is reported by RegressionChecks.ValidateQuestsAndNotes.
///
/// THE CLUES (Eric, 2026-11-01: "after that, it should provide clues for unlockables in the level, like how
/// it says 'someone is singing under the ice'. the clues should be separated from the other text, in
/// italics. after unlocking, that text clue will not appear."). Under the intro, set apart, one line for
/// each thing the chapter still holds for this save: the wizard held there (his held line), what its side
/// event pays when that is not a wizard (SiteClues), and a vault's spell (VaultClue). Each goes the moment
/// it is won. A clue says something is there and never what: no wizard's name, no spell's, no place, no
/// verb - the hints' rule (StageHintCatalog), checked by ValidateQuestsAndNotes. While a clue remains, a
/// returning player sees the card again, with the clues alone (ChapterNoteCard).
/// </remarks>
public static class ChapterNotes
{
	/// <summary>The most lines a note's own text - the place - may carry.</summary>
	public const int MaxIntroLines = 2;

	/// <summary>
	/// The most lines a chapter's note may carry, held lines included: the place's two, and one for each
	/// wizard held there - at least one, the "third only where someone is held" the note was written to. A
	/// chapter with two events can hold two wizards (the Court, .ai/chapter-wizards.md 6.3), and each is a line.
	/// </summary>
	public static int MaxLinesFor(string stageId) =>
		MaxIntroLines + Math.Max(1, QuestLog.Prisoners.Count(p =>
			GlobalStatsManager.RescueChapterOf(UnlockCatalog.GetCharacter(p.CharacterId))
				.Equals(stageId ?? string.Empty, StringComparison.OrdinalIgnoreCase)));

	/// <summary>The longest a line may run - one line of the note card on a phone.</summary>
	public const int MaxLineLength = 64;

	private static readonly Dictionary<string, string[]> Lines = new(StringComparer.OrdinalIgnoreCase)
	{
		// 1 Enchanted Forest - "Every path now leads inward."
		["stage_0"] = new[]
		{
			"Every path bends back toward the middle of the wood.",
			"Wet bark and leaf mould. The trees are listening.",
		},
		// 2 The Fallow Fields - "He takes a tithe of everything that grows here."
		["stage_9"] = new[]
		{
			"Stubble and thistle, and no one left to bring it in.",
			"Chaff in my throat. Only the well still smells clean.",
		},
		// 3 The Broken Circle - "The circle where the wizards met. He came here first."
		["stage_10"] = new[]
		{
			"The stones where we used to meet. He came here first.",
			"Heather, granite, and a wind with nothing in its way.",
		},
		// 4 Sunken Cave - "His shadow followed the water down into the dark."
		["stage_2"] = new[]
		{
			"Water drips everywhere, and my light is the only light.",
			"Cold, wet stone. It smells like old coins.",
		},
		// 11 Cursed Dungeon (fifth until piece C2 moved it into the castle) - "He keeps a wizard in one of
		// these cells." His own cells now: the second line says whose house it is, and that the stair at the
		// start is the way down from the bridge's gate (.ai/endgame-castle.md 4.1).
		["stage_1"] = new[]
		{
			"Rust, damp straw and old fear. Every door is locked.",
			"Under his own house. The stair behind me goes up to the gate.",
		},
		// 6 Blighted Swamp - "The lanterns still burn. Nothing living lights them."
		["stage_3"] = new[]
		{
			"Reeds, fog, and lanterns nobody living lit.",
			"Rot and standing water. My boots never dry.",
		},
		// 7 Mystic Ruins - "The old wards were broken from the inside."
		["stage_4"] = new[]
		{
			"Our old seat of learning. The wards broke from inside.",
			"Dust and cold ash. The stones still hum.",
		},
		// 8 Frozen Waste - "A wizard lies under the ice, and the cold stands guard."
		["stage_5"] = new[]
		{
			"Too cold to smell anything. The wind cuts through the robe.",
		},
		// 9 Scorched Sands - "He hid the water. The dead still walk the trade road."
		["stage_6"] = new[]
		{
			"Heat off cracked ground, and no water anywhere.",
			"Hot dust and old salt. The dead still walk the road.",
		},
		// 10 The Emberdeep - "His forge, at the end of the road." ("His seat", until the castle was drawn across the water.)
		["stage_7"] = new[]
		{
			"Sulphur, and stone too hot to touch.",
			"The end of the road. His seat is close.",
		},
		// 10 The Last Bridge - "His seat is across the water." Written with the move (C2), ready for the flip:
		// a playable chapter with no note is a validator fault, and it was the one the bridge's flip still had.
		["stage_11"] = new[]
		{
			"Wind off the dark sea, and his rock at the far end.",
			"Salt and old mortar. The span falls in behind me.",
		},
		// 12 The Court - "A court with no one left in it." Written for its flip (2026-11-02).
		["stage_12"] = new[]
		{
			"His house. Tables laid for a feast nobody comes to.",
			"Cold wax and old rushes. Every door opens on another room.",
		},
		// 13 The Throne - "He is here." Written for its flip (2026-11-02).
		["stage_13"] = new[]
		{
			"One hall of black stone. The doors shut behind me.",
			"Nothing to smell, nothing to hear. He is here.",
		},
		// The Sketchbook - outside the campaign; nothing here is his doing (StageCatalog).
		["stage_8"] = new[]
		{
			"Squared paper underfoot. It smells of pencil.",
			"He has never been here.",
		},
	};

	// What a side event holds when it is not a wizard (a wizard's line is his held line, QuestLog.Prisoners),
	// keyed by its Discovery site (SideEventDirector.EventsFor). In the note's voice: where the place's
	// intro says what it is like, a clue says that something is still out there.
	private static readonly Dictionary<string, string> SiteClues = new(StringComparer.OrdinalIgnoreCase)
	{
		["the_grove"] = "Somewhere in the wood, something is still growing.",
		["bog_lanterns"] = "His lanterns are keeping something out in the fog.",
		["caravan_master"] = "A bell on the old road. Something still walks it.",
		["the_well"] = "Something glints at the bottom of the well.",
		["the_moot"] = "Embers still glow somewhere among the stones.",
	};

	// A spell vault (SecretCatalog, IsVault) the save has not opened. One line for the chapter however many
	// it holds, and only the chapter: the Spell Index already says "Hidden somewhere in" it, and that is
	// all a secret is told before it is seen (.ai/achievements-and-secrets.md 4.3).
	public const string VaultClue = "Something he took is hidden here. I can feel it.";

	/// <summary>The site clues, for the validator: the real ones (a tool run's stand-in is answered by <see cref="SiteClueFor"/> alone).</summary>
	public static IReadOnlyDictionary<string, string> SiteClueLines => SiteClues;

	/// <summary>The clue for the side event at <paramref name="siteId"/>, or null; the stand-in's on a tool run that has it.</summary>
	public static string SiteClueFor(string siteId)
	{
		if (string.IsNullOrWhiteSpace(siteId))
			return null;
		if (SiteClues.TryGetValue(siteId, out string clue))
			return clue;
		return SecondEventStandIn.Active && SecondEventStandIn.IsStandInSite(siteId) ? SecondEventStandIn.Clue : null;
	}

	/// <summary>Whether a chapter has a note written. The castle's chapters get theirs when they are built (the Last Bridge has its own already).</summary>
	public static bool Has(string stageId) => !string.IsNullOrWhiteSpace(stageId) && Lines.ContainsKey(stageId);

	/// <summary>The stage ids a note is written for, for the validator.</summary>
	public static IEnumerable<string> WrittenFor => Lines.Keys;

	/// <summary>A chapter's note on this save: its own lines, then whoever is held there, by epithet or by name once free.</summary>
	public static List<string> LinesFor(string stageId, SaveData save)
	{
		var lines = new List<string>();
		if (!Has(stageId))
			return lines;
		lines.AddRange(Lines[stageId]);
		lines.AddRange(QuestLog.HeldLinesFor(stageId, save));
		return lines;
	}

	/// <summary>The chapter's own lines - the place - without whoever is held there.</summary>
	public static IReadOnlyList<string> IntroFor(string stageId) =>
		Has(stageId) ? Lines[stageId] : Array.Empty<string>();

	/// <summary>
	/// What the chapter still holds for this save, one clue a thing, in the order the card sets them: the
	/// wizard held there, what its side event pays, a vault. Empty once everything here is won.
	/// </summary>
	public static List<string> CluesFor(string stageId, SaveData save)
	{
		var clues = new List<string>();
		if (!Has(stageId))
			return clues;
		clues.AddRange(QuestLog.HeldOnlyLinesFor(stageId, save));

		// Every side event the chapter holds, in its order (a chapter may hold two, .ai/chapter-wizards.md 6.3):
		// a site's clue while it still pays something other than a wizard this save lacks - a wizard's clue is
		// his held line, above.
		foreach (SideEventEntry site in SideEventDirector.EventsFor(stageId))
		{
			if (SiteClueFor(site.SiteId) is string siteClue
				&& !DiscoveryRewards.IsSiteExhausted(save, site.SiteId)
				&& UnlockCatalog.ForDiscoverySite(site.SiteId).Any(u => u.Kind != UnlockKind.Character
					&& !IsOwned(save, u)))
				clues.Add(siteClue);
		}

		SecretDefinition teacher = SecretCatalog.ForStage(stageId).FirstOrDefault(secret => Teachings.LessonAt(save, secret.Id) != null);
		if (teacher != null)
		{
			// Glimpsed on an earlier run, the clue stops being coy (Teachings.LandmarkHint).
			bool glimpsed = save?.HasGlimpsedSecret(teacher.Id) == true;
			clues.Add(teacher.Id == SecretCatalog.PinfoldId && !glimpsed
				? "Something struggled through the stubble."
				: Teachings.LandmarkHint(teacher.Id, glimpsed));
		}
		else if (SecretCatalog.ForStage(stageId).Any(secret => secret.IsVault && !string.IsNullOrEmpty(secret.SpellUnlockId)
			&& (save == null || (!save.HasFoundSecret(secret.Id) && !GlobalStatsManager.IsSpellUnlockedForLevelUp(save, secret.SpellUnlockId)))))
			clues.Add(VaultClue);
		return clues;
	}

	// A Discovery unlock this save already has, asked the way DiscoveryRewards.IsSiteExhausted asks it.
	private static bool IsOwned(SaveData save, UnlockDefinition unlock)
	{
		if (save == null)
			return false;
		return unlock.Kind switch
		{
			UnlockKind.Character => GlobalStatsManager.IsCharacterUnlocked(save, unlock.Id),
			UnlockKind.Boon => GlobalStatsManager.IsBoonUnlocked(save, unlock.Id),
			_ => GlobalStatsManager.IsSpellUnlockedForLevelUp(save, unlock.Id),
		};
	}

	public static bool IsSeen(SaveData save, string stageId) =>
		save != null && !string.IsNullOrWhiteSpace(stageId)
		&& save.SeenChapterNoteIds.Any(id => id.Equals(stageId, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Marks a chapter's note read. In memory: it is saved with the run, as everything a run learns is, so a
	/// tool scene (which never writes) shows it again next time, and so does a crash - neither costs anything.
	/// </summary>
	public static void MarkSeen(SaveData save, string stageId)
	{
		if (save == null || !Has(stageId) || IsSeen(save, stageId))
			return;
		save.SeenChapterNoteIds.Add(stageId);
	}

	/// <summary>
	/// The chapters in the journal: every one whose note has been shown, and every one the save has played
	/// (a save from before the notes has been to places it has no note for yet), in the road's order and then
	/// the chapters outside it.
	/// </summary>
	public static List<StageDefinition> JournalChapters(SaveData save)
	{
		return StageCatalog.InDisplayOrder
			.Where(s => s.IsPlayable && Has(s.Id)
				&& (IsSeen(save, s.Id) || (save?.Lifetime != null && save.Lifetime.BestTimeOnStage(s.Id) > 0f)))
			.ToList();
	}
}
