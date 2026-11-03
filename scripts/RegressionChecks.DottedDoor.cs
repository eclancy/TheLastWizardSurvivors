using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// Ma Liang (.ai/chapter-wizards.md 4.8): what holds the trace, the Painted Door and the opening page to their sentences,
// which no build sees and a single run shows once. Every line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	private static void ValidateDottedDoor(List<string> warnings)
	{
		// --- the door in the house: on the Gallery's floor, every stretch of its line walkable within the tolerance ---
		CourtLayout layout = CourtLayout.Load(Vector2.Zero);
		if (layout == null)
		{
			warnings.Add("Dotted door: the Court's layout will not load, so the door has nowhere to be.");
			return;
		}
		if (DottedDoorEvent.DoorCentreIn(layout) is not Vector2 centre)
		{
			warnings.Add("Dotted door: the Court has no Gallery to sketch the door in.");
			return;
		}
		if (layout.RoomAt(centre)?.Id != DottedDoorEvent.RoomId)
			warnings.Add($"Dotted door: its middle ({centre.X:0},{centre.Y:0}) is not on the Gallery's floor.");
		int offFloor = 0;
		for (float s = 0f; s <= DottedDoorEvent.PathLength; s += 8f)
		{
			Vector2 p = centre + DottedDoorEvent.PathPoint(s);
			if (!layout.IsFloor(p) || layout.RoomAt(p)?.Id != DottedDoorEvent.RoomId
				|| !layout.IsFloor(p + new Vector2(DottedDoorEvent.TraceTolerance, 0f)) || !layout.IsFloor(p - new Vector2(DottedDoorEvent.TraceTolerance, 0f))
				|| !layout.IsFloor(p + new Vector2(0f, DottedDoorEvent.TraceTolerance)) || !layout.IsFloor(p - new Vector2(0f, DottedDoorEvent.TraceTolerance)))
				offFloor++;
		}
		if (offFloor > 0)
			warnings.Add($"Dotted door: {offFloor} stretch(es) of its line are not on the Gallery's open floor with the tolerance round them; it could not be walked.");
		// Nothing else of the house's is drawn over it: no reading room (the seer never reads in the Gallery) and no furniture.
		if (SeersVoiceEvent.ReadingRoomIds.Contains(DottedDoorEvent.RoomId))
			warnings.Add("Dotted door: the seer may read from the Gallery; two prisoners never share a room (.ai/chapter-wizards.md 4.8).");
		foreach (AuthoredLayout.Piece piece in layout.Grid.Furniture.Where(f => f.Room == DottedDoorEvent.RoomId && !f.Wall))
		{
			Vector2 at = layout.CellToWorld(piece.At);
			if (Mathf.Abs(at.X - centre.X) < DottedDoorEvent.HalfWidth + 40f && Mathf.Abs(at.Y - centre.Y) < DottedDoorEvent.HalfHeight + 40f)
				warnings.Add($"Dotted door: the Gallery's '{piece.Kind}' stands on the drawing.");
		}

		// --- the trace rule, on paper ---------------------------------------------------------------------------
		const float walk = 220f, dt = 1f / 30f;
		float L = DottedDoorEvent.PathLength;
		// Walked along the line at his pace, from dot 1: inked, every dot reached.
		(float pen, bool begun, bool on) = (0f, false, false);
		for (float s = 0f; s <= L + walk * dt; s += walk * dt)
			(pen, begun, on) = DottedDoorEvent.Step(pen, begun, DottedDoorEvent.PathPoint(s));
		if (!begun || pen < L - 0.5f || DottedDoorEvent.DotsReached(pen, begun) != DottedDoorEvent.DotCount)
			warnings.Add($"Dotted door: walked dot to dot at his pace the line inks only {pen:0} of {L:0} px ({DottedDoorEvent.DotsReached(pen, begun)} dots).");
		// Walked off it half way, away and back: the ink waits where it was, and picks up again from there.
		(pen, begun) = (0f, false);
		float half = L * 0.5f;
		for (float s = 0f; s <= half; s += walk * dt)
			(pen, begun, on) = DottedDoorEvent.Step(pen, begun, DottedDoorEvent.PathPoint(s));
		float held = pen;
		Vector2 off = DottedDoorEvent.PathPoint(half) + new Vector2(0f, 140f) + new Vector2(140f, 0f);
		bool stayedOff = true;
		for (int k = 0; k < 120; k++)
		{
			(pen, begun, on) = DottedDoorEvent.Step(pen, begun, off + new Vector2(Mathf.Sin(k * 0.3f) * 40f, 0f));
			stayedOff &= !on;
		}
		if (!stayedOff || Mathf.Abs(pen - held) > 0.01f)
			warnings.Add($"Dotted door: stepping off the line moved the ink ({held:0} -> {pen:0} px); off the line it must only pause.");
		for (float s = half; s <= L + walk * dt; s += walk * dt)
			(pen, begun, on) = DottedDoorEvent.Step(pen, begun, DottedDoorEvent.PathPoint(s));
		if (pen < L - 0.5f)
			warnings.Add($"Dotted door: picked up again where it paused, the line inks only {pen:0} of {L:0} px; it must never reset.");
		// Cutting across the floor from the first dot to the last does not ink it: the line is walked, not skipped.
		(pen, begun) = (0f, false);
		Vector2 a0 = DottedDoorEvent.PathPoint(0f), a1 = DottedDoorEvent.PathPoint(L);
		for (int k = 0; k <= 60; k++)
			(pen, begun, on) = DottedDoorEvent.Step(pen, begun, a0.Lerp(a1, k / 60f));
		if (pen > DottedDoorEvent.PenLookAhead + 1f)
			warnings.Add($"Dotted door: walked straight across from dot 1 to dot {DottedDoorEvent.DotCount}, the ink reached {pen:0} px; the line must be followed.");
		// Begun anywhere but the first dot, nothing begins.
		(pen, begun) = (0f, false);
		for (float s = L; s >= L * 0.6f; s -= walk * dt)
			(pen, begun, on) = DottedDoorEvent.Step(pen, begun, DottedDoorEvent.PathPoint(s));
		if (begun)
			warnings.Add("Dotted door: the trace began at the last dot; it begins at dot 1.");
		// The chord the look-ahead allows is inside the tolerance: no step it takes leaves the line by more than that.
		float chordSag = DottedDoorEvent.ArchRadius * (1f - Mathf.Cos(DottedDoorEvent.PenLookAhead / DottedDoorEvent.ArchRadius * 0.5f));
		if (chordSag > DottedDoorEvent.TraceTolerance * 1.5f)
			warnings.Add($"Dotted door: the look-ahead's chord across the arch sags {chordSag:0} px, far past the {DottedDoorEvent.TraceTolerance:0} px tolerance; a step could skip the arch.");

		// --- the art, at the sizes the event and the form read it at (tools/art/painted_door.py) --------------------
		CheckDoorSheet(warnings, "res://assets/bonelight/world/door/door-sketch.png", new Vector2(104, 172), "the sketch");
		CheckDoorSheet(warnings, "res://assets/bonelight/world/door/door-inked.png", new Vector2(104 * 4, 172), "the inked door's four frames");
		CheckDoorSheet(warnings, "res://assets/bonelight/effects/painted-door.png", new Vector2(20 * 5, 30), "the Painted Door's five frames");
		// The sheet's path is the event's at x2 (painted_door.py HALF_W 40, HALF_H 75, ARCH_R 40, 15 dots).
		if (DottedDoorEvent.HalfWidth != 80f || DottedDoorEvent.HalfHeight != 150f || DottedDoorEvent.ArchRadius != 80f || DottedDoorEvent.DotCount != 15)
			warnings.Add("Dotted door: the event's door is no longer painted_door.py's at x2 (HALF_W 40, HALF_H 75, ARCH_R 40, 15 dots); the ink would miss the charcoal.");

		// --- the hint: hint_brush with the paint cue, on the sound's strokes (sfx.py _BRUSH_STROKES and _BRUSH_DRIP) --------
		float[] beats = StageHintCue.BrushBeatTimes;
		float[] strokes = { 0.06f, 0.80f, 1.40f, 1.96f };
		if (beats.Length != strokes.Length || beats.Where((b, i) => MathF.Abs(b - strokes[i]) > 0.001f).Any())
			warnings.Add($"Dotted door: the hint's paint is laid on [{string.Join(", ", beats)}], not hint_brush's strokes ({string.Join(", ", strokes)}).");
		if (StageHintCatalog.ForSite("dotted_door") is not StageHint hint || hint.Cue != HintCue.Paint || hint.Sound != SfxCatalog.HintBrush)
			warnings.Add("Dotted door: its hint must be hint_brush with the paint cue.");

		// --- the Gallery: his portrait is the first frame of the row, the seer's still the last --------------------
		int frames = layout.Grid.Furniture.Count(p => p.Kind == "portrait" && p.Room == "gallery");
		IReadOnlyList<string> taken = GalleryRoom.TakenWizards;
		if (taken.Contains(GalleryRoom.FirstFrameWizardId) && GalleryRoom.WizardInFrame(0, frames, taken) != GalleryRoom.FirstFrameWizardId)
			warnings.Add("Dotted door: the Gallery's first frame is not the painter's own portrait.");
		var framed = Enumerable.Range(0, frames).Select(i => GalleryRoom.WizardInFrame(i, frames, taken)).Where(id => id != null).ToList();
		if (framed.Distinct().Count() != framed.Count)
			warnings.Add($"Dotted door: a wizard hangs in two of the Gallery's frames ({string.Join(", ", framed)}).");
		foreach (string id in taken.Take(frames))
			if (!framed.Contains(id))
				warnings.Add($"Dotted door: '{id}' has no frame in the Gallery although the wall holds {frames}.");

		// --- the opening page and the form ------------------------------------------------------------------------------
		CharacterData him = CharacterRoster.GetAll().FirstOrDefault(c => c.Id == "ma_liang");
		if (him == null)
			warnings.Add("Dotted door: Ma Liang is not in CharacterRoster.");
		else
		{
			if (!Player.ChoosesOpeningForm(him))
				warnings.Add("Ma Liang: he does not choose his Familiar's form at the start of a run; the dog and the cat could never be his.");
			ApotheosisDefinition form = ApotheosisCatalog.ForCharacter("ma_liang");
			if (form == null || form.VariantId != Familiar.CrowId || form.Form?.Id != Familiar.PaintedDoorId)
				warnings.Add("Ma Liang: his Apotheosis must be The Painted Door, on the crow.");
		}
		var ordinary = new Enemy();
		var elite = new Enemy { IsMiniBoss = true };
		var boss = new BossEnemy();
		if (!Familiar.DoorTakes(ordinary) || Familiar.DoorTakes(elite) || Familiar.DoorTakes(boss))
			warnings.Add("Painted Door: a door must take an ordinary body and never an elite, a miniboss or a boss.");
		ordinary.Free();
		elite.Free();
		boss.Free();
		if (Familiar.ExitMinDistance >= Familiar.ExitDistance || Familiar.ExitMinDistance < 500f)
			warnings.Add($"Painted Door: the second door must stand far across the field (ExitMinDistance {Familiar.ExitMinDistance:0}, ExitDistance {Familiar.ExitDistance:0}).");
		if (Familiar.DoorEverySeconds <= 0f || Familiar.MaxDoors < 1 || Familiar.DoorCapacity < 1 || Familiar.DisplacedCooldown < Familiar.DoorOpenSeconds)
			warnings.Add("Painted Door: its cadence must keep a door an event (DoorEverySeconds > 0, MaxDoors and DoorCapacity >= 1, a body not moved again while the door stays open).");
		if (Player.PaintFlaskHealthFraction <= 0f || Player.PaintFlaskHealthFraction >= 0.5f)
			warnings.Add($"Paint What Is Needed: it paints below {Player.PaintFlaskHealthFraction:P0} of his health; the design is a quarter.");
	}

	private static void CheckDoorSheet(List<string> warnings, string path, Vector2 size, string what)
	{
		Texture2D texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		if (texture == null)
			warnings.Add($"Dotted door: {path} ({what}) is missing or not imported.");
		else if (texture.GetSize() != size)
			warnings.Add($"Dotted door: {path} is {texture.GetSize()}, expected {size} ({what}).");
	}
}
