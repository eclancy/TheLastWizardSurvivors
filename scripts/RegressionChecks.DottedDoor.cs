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
		// Every line any figure can draw, either way round, on open floor with the tolerance round it.
		int offFloor = 0;
		IReadOnlyList<(int[] Lines, int Index)> family = DoorFigure.Family;
		foreach (bool mirror in new[] { false, true })
			foreach ((int[] _, int index) in family)
			{
				DoorFigure fig = DoorFigure.At(index, mirror);
				for (int l = 0; l < fig.LineCount; l++)
					for (float s = 0f; s <= fig.Length(l); s += 8f)
					{
						Vector2 p = centre + fig.Point(l, fig.EndA(l), s / fig.Length(l));
						float tol = DottedDoorEvent.TraceTolerance;
						if (!layout.IsFloor(p) || layout.RoomAt(p)?.Id != DottedDoorEvent.RoomId
							|| !layout.IsFloor(p + new Vector2(tol, 0f)) || !layout.IsFloor(p - new Vector2(tol, 0f))
							|| !layout.IsFloor(p + new Vector2(0f, tol)) || !layout.IsFloor(p - new Vector2(0f, tol)))
							offFloor++;
					}
			}
		if (offFloor > 0)
			warnings.Add($"Dotted door: {offFloor} stretch(es) of the figures' lines are not on the Gallery's open floor with the tolerance round them; they could not be walked.");
		// Nothing else of the house's is drawn over it: no reading room (the seer never reads in the Gallery) and no furniture.
		if (SeersVoiceEvent.ReadingRoomIds.Contains(DottedDoorEvent.RoomId))
			warnings.Add("Dotted door: the seer may read from the Gallery; two prisoners never share a room (.ai/chapter-wizards.md 4.8).");
		foreach (AuthoredLayout.Piece piece in layout.Grid.Furniture.Where(f => f.Room == DottedDoorEvent.RoomId && !f.Wall))
		{
			Vector2 at = layout.CellToWorld(piece.At);
			if (Mathf.Abs(at.X - centre.X) < DottedDoorEvent.HalfWidth + 40f && Mathf.Abs(at.Y - centre.Y) < DottedDoorEvent.HalfHeight + 40f)
				warnings.Add($"Dotted door: the Gallery's '{piece.Kind}' stands on the drawing.");
		}

		ValidateDoorStroke(warnings);

		// --- the art, at the sizes the event and the form read it at (tools/art/painted_door.py) --------------------
		CheckDoorSheet(warnings, "res://assets/bonelight/world/door/door-sketch.png", new Vector2(104, 172), "the sketch");
		CheckDoorSheet(warnings, "res://assets/bonelight/world/door/door-inked.png", new Vector2(104 * 4, 172), "the inked door's four frames");
		CheckDoorSheet(warnings, "res://assets/bonelight/effects/painted-door.png", new Vector2(20 * 5, 30), "the Painted Door's five frames");
		// The figure's outline is the painted door's at x2 (painted_door.py HALF_W 40, HALF_H 75, ARCH_R 40), so the
		// drawing inked in becomes the door that opens.
		if (DottedDoorEvent.HalfWidth != 80f || DottedDoorEvent.HalfHeight != 150f || DottedDoorEvent.ArchRadius != 80f)
			warnings.Add("Dotted door: the figure's outline is no longer painted_door.py's at x2 (HALF_W 40, HALF_H 75, ARCH_R 40); the drawing would not become the door.");

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

	// THE ONE-STROKE RULE ON PAPER (.ai/unlock-puzzles.md 3.2), over the whole family both ways round, through the same
	// DoorStroke the game feeds his feet: the true stroke from an odd dot inks everything and never smudges; no stroke
	// covers it from an even dot; the wrong start's dead end, walked back along the ink, smudges; stepping off a line
	// only pauses it; and crossing a line is not walking it.
	private static void ValidateDoorStroke(List<string> warnings)
	{
		IReadOnlyList<(int[] Lines, int Index)> family = DoorFigure.Family;
		if (family.Count < 50)
			warnings.Add($"Dotted door: the figure family holds {family.Count}; a run's door should be one of many (it was 317 when built).");
		var sizes = new int[DoorFigure.MaxLines + 1];
		int unsolved = 0, evenSolved = 0, deadEndKept = 0, notStuck = 0;
		string firstBad = null;
		foreach (bool mirror in new[] { false, true })
			foreach ((int[] _, int index) in family)
			{
				DoorFigure fig = DoorFigure.At(index, mirror);
				if (!mirror) sizes[fig.LineCount]++;
				(DoorFigure.Dot odd, _) = fig.OddDots;
				// The true stroke, walked at his pace.
				var stroke = new DoorStroke(fig);
				List<(int Line, DoorFigure.Dot From)> path = fig.StrokeFrom(odd);
				if (path == null || !WalkStroke(stroke, fig, path, out bool smudged) || smudged)
				{
					unsolved++;
					firstBad ??= $"figure {index}{(mirror ? " mirrored" : "")}";
				}
				// From every even dot: no stroke covers it.
				foreach (DoorFigure.Dot d in fig.Dots)
					if (fig.Degree(d) % 2 == 0 && fig.StrokeFrom(d) != null)
						evenSolved++;
				// The wrong start, greedily: from an even dot, line after line until none is left at the pen; then back
				// along the last line inked, which must smudge it all.
				DoorFigure.Dot even = DoorFigure.Dot.LeftFoot;
				foreach (DoorFigure.Dot d in fig.Dots)
					if (fig.Degree(d) % 2 == 0) { even = d; break; }
				var wrong = new DoorStroke(fig);
				var greedy = new List<(int, DoorFigure.Dot)>();
				DoorFigure.Dot at = even;
				var used = new bool[fig.LineCount];
				while (true)
				{
					int next = -1;
					for (int l = 0; l < fig.LineCount; l++)
						if (!used[l] && fig.Touches(l, at)) { next = l; break; }
					if (next < 0) break;
					used[next] = true;
					greedy.Add((next, at));
					at = fig.Other(next, at);
				}
				WalkStroke(wrong, fig, greedy, out _);
				if (wrong.IsDone)
					continue;   // a lucky greedy order cannot happen from an even dot, but never call it a failure
				if (!wrong.Stuck)
					notStuck++;
				(int lastLine, DoorFigure.Dot lastFrom) = greedy[^1];
				bool backSmudged = false;
				float len = fig.Length(lastLine);
				for (float s = len; s >= 0f && !backSmudged; s -= 7f)
					backSmudged = wrong.Step(fig.Point(lastLine, lastFrom, s / len)) == StrokeEvent.Smudged;
				if (!backSmudged || wrong.InkedCount != 0 || wrong.Begun)
					deadEndKept++;
			}
		if (unsolved > 0)
		{
			warnings.Add($"Dotted door: {unsolved} figure(s) could not be inked by walking the true stroke from an odd dot (first: {firstBad}).");
			DumpFailedStroke(firstBad);
		}
		if (evenSolved > 0)
			warnings.Add($"Dotted door: {evenSolved} even start(s) found a stroke covering the figure; only an odd dot may begin it.");
		if (notStuck > 0)
			warnings.Add($"Dotted door: {notStuck} wrong start(s) ran out of lines without the stroke saying it was stuck.");
		if (deadEndKept > 0)
			warnings.Add($"Dotted door: {deadEndKept} wrong start(s), walked back along the ink, did not smudge the drawing back to charcoal.");
		for (int n = DoorFigure.MinLines; n <= DoorFigure.MaxLines; n++)
			if (sizes[n] == 0)
				warnings.Add($"Dotted door: no figure in the family has {n} lines; the design asks for seven to ten.");

		// Stepping off a line pauses it; crossing an inked line does not smudge it.
		DoorFigure f0 = DoorFigure.At(0, false);
		(DoorFigure.Dot o0, _) = f0.OddDots;
		List<(int Line, DoorFigure.Dot From)> p0 = f0.StrokeFrom(o0);
		var s0 = new DoorStroke(f0);
		(int line0, DoorFigure.Dot from0) = p0[0];
		float l0 = f0.Length(line0);
		for (float s = 0f; s <= l0 * 0.5f; s += 7f)
			s0.Step(f0.Point(line0, from0, s / l0));
		float heldT = s0.ActiveT;
		Vector2 off = f0.Point(line0, from0, 0.5f) + new Vector2(140f, 140f);
		bool stayedOff = true;
		for (int k = 0; k < 120; k++)
		{
			s0.Step(off + new Vector2(Mathf.Sin(k * 0.3f) * 40f, 0f));
			stayedOff &= !s0.OnLine;
		}
		if (!stayedOff || Mathf.Abs(s0.ActiveT - heldT) > 0.001f || s0.Active != line0)
			warnings.Add($"Dotted door: stepping off a line moved the ink ({heldT:0.00} -> {s0.ActiveT:0.00}); off the line it must only pause.");
		// Crossing: a figure with a straight inked line, walked across its middle from one side to the other.
		var cross = new DoorStroke(f0);
		cross.DebugInk(f0.LineCount - 1);
		int straight = -1;
		for (int l = 0; l < f0.LineCount; l++)
			if (cross.IsInked(l) && f0.KindOf(l) == DoorFigure.Kind.Line && f0.Length(l) > 100f) { straight = l; break; }
		if (straight >= 0)
		{
			Vector2 a = f0.Point(straight, f0.EndA(straight), 0f), b = f0.Point(straight, f0.EndA(straight), 1f);
			Vector2 mid = (a + b) * 0.5f, across = (b - a).Normalized().Orthogonal();
			bool crossSmudged = false;
			for (float s = -60f; s <= 60f; s += 7f)
				crossSmudged |= cross.Step(mid + across * s) == StrokeEvent.Smudged;
			if (crossSmudged)
				warnings.Add("Dotted door: crossing an inked line smudged the drawing; only walking along one may.");
		}
	}

	// The first failing figure, replayed with every event and the state at each line's end, so a failure names its cause.
	private static void DumpFailedStroke(string which)
	{
		if (which == null)
			return;
		string[] parts = which.Split(' ');
		DoorFigure fig = DoorFigure.At(int.Parse(parts[1]), which.Contains("mirrored"));
		(DoorFigure.Dot odd, _) = fig.OddDots;
		List<(int Line, DoorFigure.Dot From)> path = fig.StrokeFrom(odd);
		var lines = new List<string>();
		for (int l = 0; l < fig.LineCount; l++)
			lines.Add($"{l}:{fig.EndA(l)}-{fig.EndB(l)}({fig.KindOf(l)},{fig.Length(l):0})");
		GD.Print($"DOORSTROKE {which}: lines {string.Join(" ", lines)}; odd {fig.OddDots}; path "
			+ (path == null ? "none" : string.Join(" ", path.Select(p => $"{p.Line}<{p.From}"))));
		if (path == null)
			return;
		var stroke = new DoorStroke(fig);
		foreach ((int line, DoorFigure.Dot from) in path)
		{
			float len = fig.Length(line);
			for (float s = 0f; s <= len + 7f; s += 7f)
			{
				Vector2 at = fig.Point(line, from, Mathf.Min(1f, s / len));
				StrokeEvent e = stroke.Step(at);
				if (e != StrokeEvent.None)
					GD.Print($"DOORSTROKE   walking {line}<{from} at {s:0}/{len:0}: {e}; active {stroke.Active} from {stroke.ActiveFrom} "
						+ $"t {stroke.ActiveT:0.00}, pen {stroke.Pen}, inked {stroke.InkedCount}");
			}
			GD.Print($"DOORSTROKE   end of {line}<{from}: inked {stroke.InkedCount}/{fig.LineCount}, line {line} inked {stroke.IsInked(line)}, "
				+ $"active {stroke.Active} from {stroke.ActiveFrom} t {stroke.ActiveT:0.00}, pen {stroke.Pen}, smudges {stroke.Smudges}");
		}
	}

	// A stroke walked at his pace: to each line's first dot, then along it, 7 px a step (220 px/s at 30 Hz).
	private static bool WalkStroke(DoorStroke stroke, DoorFigure fig, List<(int Line, DoorFigure.Dot From)> path, out bool smudged)
	{
		smudged = false;
		foreach ((int line, DoorFigure.Dot from) in path)
		{
			float len = fig.Length(line);
			for (float s = 0f; s <= len + 7f; s += 7f)
			{
				StrokeEvent e = stroke.Step(fig.Point(line, from, Mathf.Min(1f, s / len)));
				if (e == StrokeEvent.Smudged)
					smudged = true;
			}
		}
		return stroke.IsDone;
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
