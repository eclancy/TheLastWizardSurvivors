using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// Nostradamus (.ai/chapter-wizards.md 4.10): what holds the seek and the Quatrains to their sentences, which no build
// sees and a single run would only show once. Every line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	private static void ValidateSeersVoice(List<string> warnings)
	{
		CourtLayout layout = CourtLayout.Load(Vector2.Zero);
		if (layout == null)
		{
			warnings.Add("Seer's voice: the Court's layout will not load, so his readings have no rooms.");
			return;
		}

		// None of his rooms is a room that is already something else's to reach (the Gallery is Ma Liang's, 4.8).
		string[] never = { "gallery", "treasury", "armoury", "solar", "minstrels_gallery", "keepsake_doors", "start", "throne_doors" };
		foreach (string id in SeersVoiceEvent.ReadingRoomIds)
		{
			if (never.Contains(id))
				warnings.Add($"Seer's voice: '{id}' may not hold a reading (.ai/chapter-wizards.md 4.10, 'Its rooms').");
			else if (layout.RoomById(id) is not CourtLayout.Room room)
				warnings.Add($"Seer's voice: reading room '{id}' is not in court_layout.json.");
			else if (SeersVoiceEvent.PlacesIn(layout, room).Count == 0)
				warnings.Add($"Seer's voice: '{id}' has no floor inset {SeersVoiceEvent.SpotInsetCells} cells from its walls and off its furniture.");
		}

		// The seed's three readings, on paper, for 400 seeds: three different rooms of the list, the first far enough
		// from the stair and each leg far enough from the last - every one a walk across the house.
		int bad = 0;
		float shortestLeg = float.MaxValue, nearestFirst = float.MaxValue;
		var firsts = new HashSet<string>();
		for (ulong seed = 1; seed <= 400; seed++)
		{
			var rng = new LevelGenerator.Rng(LevelGenerator.Hash(seed, 12, 1, 0x5E7E));
			(List<string> rooms, List<Vector2> spots) = SeersVoiceEvent.ChooseReadings(layout, rng.NextFloat);
			// Each place reached only from inside its room (or its doorway): no floor of another room or a hall comes
			// within ReachRadius, so the voice's room is never "found" through a wall.
			bool ok = rooms.Count == SeersVoiceEvent.Readings && rooms.Distinct().Count() == rooms.Count
				&& spots[0].DistanceTo(layout.StageOrigin) >= SeersVoiceEvent.FirstFromStart
				&& rooms.Select((r, i) => SeersVoiceEvent.ReachedOnlyFromInside(layout, layout.RoomById(r), spots[i])).All(x => x);
			for (int i = 1; ok && i < spots.Count; i++)
			{
				float leg = spots[i].DistanceTo(spots[i - 1]);
				shortestLeg = MathF.Min(shortestLeg, leg);
				ok = leg >= SeersVoiceEvent.LegDistance;
			}
			if (ok)
			{
				nearestFirst = MathF.Min(nearestFirst, spots[0].DistanceTo(layout.StageOrigin));
				firsts.Add(rooms[0]);
			}
			else if (bad++ == 0)
				warnings.Add($"Seer's voice: seed {seed} gives readings [{string.Join(", ", rooms)}], which break the distances or repeat a room.");
		}
		if (bad > 1)
			warnings.Add($"Seer's voice: {bad} of 400 seeds break the reading distances.");
		if (firsts.Count < 3)
			warnings.Add($"Seer's voice: only {firsts.Count} rooms are ever first ([{string.Join(", ", firsts)}]) - the seed should move him.");

		// The hint and its cue: hint_quatrain with the stars, on the sound's four lines (sfx.py _QUATRAIN_LINES).
		float[] beats = StageHintCue.QuatrainLineTimes;
		float[] lines = { 0.06f, 0.78f, 1.74f, 2.46f };
		if (beats.Length != lines.Length || beats.Where((b, i) => MathF.Abs(b - lines[i]) > 0.001f).Any())
			warnings.Add($"Seer's voice: the hint's stars are on [{string.Join(", ", beats)}], not hint_quatrain's lines ({string.Join(", ", lines)}).");
		if (StageHintCatalog.ForSite("seers_voice") is not StageHint hint || hint.Cue != HintCue.Stars || hint.Sound != SfxCatalog.HintQuatrain)
			warnings.Add("Seer's voice: its hint must be hint_quatrain with the stars cue.");
		foreach (string line in SeersVoiceEvent.LegLines.SelectMany(l => l))
		{
			if (line.Length > StageHintCatalog.MaxLineLength)
				warnings.Add($"Seer's voice: reading line '{line}' is longer than a caption's {StageHintCatalog.MaxLineLength}.");
			if (line.Contains("Nostradamus", StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Seer's voice: reading line '{line}' names him.");
		}

		// The Gallery: his portrait is the last frame of the row, and every wizard he took has a frame.
		int frames = layout.Grid.Furniture.Count(p => p.Kind == "portrait" && p.Room == "gallery");
		IReadOnlyList<string> taken = GalleryRoom.TakenWizards;
		if (taken.Contains(GalleryRoom.LastFrameWizardId) && GalleryRoom.WizardInFrame(frames - 1, frames, taken) != GalleryRoom.LastFrameWizardId)
			warnings.Add("Seer's voice: the Gallery's last frame is not his.");
		if (taken.Count > frames)
			warnings.Add($"Seer's voice: the Gallery hangs {frames} frames for {taken.Count} wizards he took; court_layout.py must hang more.");

		// The art, at the sizes the event reads it at (tools/art/seers_voice.py).
		CheckSeerSheet(warnings, "res://assets/bonelight/world/seer/seer-lectern.png", new Vector2(36 * 4, 44), "the lectern's four 36x44 frames");
		CheckSeerSheet(warnings, "res://assets/bonelight/world/seer/seer-desk.png", new Vector2(48 * 4, 56), "the desk's four 48x56 frames");
	}

	private static void CheckSeerSheet(List<string> warnings, string path, Vector2 size, string what)
	{
		Texture2D texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		if (texture == null)
			warnings.Add($"Seer's voice: {path} ({what}) is missing or not imported.");
		else if (texture.GetSize() != size)
			warnings.Add($"Seer's voice: {path} is {texture.GetSize()}, expected {size} ({what}).");
	}
}
