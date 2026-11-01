using System;
using System.Collections.Generic;
using System.Linq;

namespace WizardSurvivors.scripts;

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
/// </remarks>
public static class ChapterNotes
{
	/// <summary>The most lines a note may carry, held line included.</summary>
	public const int MaxLines = 3;

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
			"Water dripping everywhere, and my light the only light.",
			"Cold, wet stone. It smells like old coins.",
		},
		// 5 Cursed Dungeon - "He keeps a wizard in one of these cells."
		["stage_1"] = new[]
		{
			"Rust, damp straw and old fear. Every door is locked.",
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
		// 10 The Emberdeep - "His seat, and the end of the road."
		["stage_7"] = new[]
		{
			"Sulphur, and stone too hot to touch.",
			"The end of the road. His seat is close.",
		},
		// The Sketchbook - outside the campaign; nothing here is his doing (StageCatalog).
		["stage_8"] = new[]
		{
			"Squared paper underfoot. It smells of pencil.",
			"He has never been here.",
		},
	};

	/// <summary>Whether a chapter has a note written. The castle's chapters get theirs when they are built.</summary>
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
