using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE DOTTED DOOR: the Court's second side event (.ai/chapter-wizards.md 4.8). "He keeps him to paint: the Gallery's
// portraits are his work, done under orders, and his brush paints nothing real while it is held. He drew himself a door
// in the Gallery's end wall and was never let finish it."
//
// WHAT IT ASKS: ONE STROKE (.ai/unlock-puzzles.md 3.2, Eric 2026-11-03; built 2026-11-07). At the Gallery's west end a
// door is sketched in charcoal, an arch about 300 px tall: five to seven dots joined by seven to ten faint lines - its
// jambs and arch, and the run's own lintel, sill, brace or ring (DoorFigure, rolled from the seed). INK EVERY LINE
// EXACTLY ONCE: walking a line dot to dot within the tolerance inks it in his light; the stroke goes on only from the
// dot the ink has reached; walking ALONG a line already inked smudges the whole drawing back to charcoal and the
// stroke starts again anywhere (DoorStroke). Stepping off a line to dodge only pauses it. Every figure has exactly two
// dots where an odd number of lines meet, and the stroke must begin at one of them: the insight is where to begin.
// What his brush finishes becomes real - each inked line is a wall the household cannot cross (layer
// EnemyOnlyCollisionLayer; he walks his own lines freely), so the drawing shelters him as it grows; the walls go
// with the ink when it smudges or when the door opens. The last line inks the whole door in: it is painted, red
// lacquer and gold studs, a real door; it opens on his light and Ma Liang steps out of it, into the column of light.
//
// WHERE: not the band. The Court is one building (CourtLayout), and the door is in it - on the Gallery's floor at its
// west end, three cells in from the end wall and on the room's middle row, under the first frame of the row of portraits
// (GalleryRoom.FirstFrameWizardId: his own, the painter's self-portrait). In a top-down house an end wall is only its top,
// so the door he drew stands up the screen on the boards in front of it, the way every door in this projection stands
// up a wall: the drawing is laid where a door would be walked through. PlacedByChapterRules, as the seer's rooms are;
// the seer never reads from the Gallery (SeersVoiceEvent.ReadingRoomIds), so two prisoners never share a room.
//
// THE PRESSURE is the house's: once the trace has begun, every HouseholdSeconds while it is unfinished and he is in the
// Gallery, the household comes in at the Gallery's three doors - HouseholdPerDoor ordinary bodies at each, the chapter's
// own (the Seneschal's household comes the same way, .ai/endgame-castle.md 5.6). Not elites: nothing here must be
// killed; they are only what pushes him off the line. Nothing is timed by the pace clock and nothing fails.
//
// Nothing here is a target: the drawing lies on the floor (DrawLayers.Ground), he is a sprite y-sorted at his feet,
// never in "enemies". Its only collision is the ink-walls, on a layer only enemies mask.
public partial class DottedDoorEvent : SideEvent
{
	/// <summary>The room the door is drawn in.</summary>
	public const string RoomId = "gallery";

	// --- the door's outline, in world px: tools/art/painted_door.py's HALF_W, HALF_H and ARCH_R at x2 (DoorFigure's) --
	public const float HalfWidth = DoorFigure.HalfWidth;
	public const float HalfHeight = DoorFigure.HalfHeight;
	public const float ArchRadius = DoorFigure.ArchRadius;

	/// <summary>How near a line his feet must stay for the ink to follow them.</summary>
	public const float TraceTolerance = DoorStroke.Tolerance;

	/// <summary>How long the smudge runs and fades, so a mistake is seen (3.2: about a second).</summary>
	public const float SmudgeSeconds = 1.1f;

	/// <summary>Where the door's middle stands: this many cells in from the Gallery's west wall, on its middle row.</summary>
	public const float CellsFromWestWall = 3f;

	/// <summary>Seconds between the household coming in at the Gallery's doors, while the trace is begun and unfinished.</summary>
	public const float HouseholdSeconds = 7f;

	/// <summary>How many of the household come in at each door each time.</summary>
	public const int HouseholdPerDoor = 2;

	/// <summary>How far inside the Gallery from a door's gap they come in.</summary>
	private const float HouseholdInside = 70f;

	// The ending, in seconds from the last dot: the ink runs round the whole door, it is painted, it opens, he steps out.
	public const float InkSeconds = 0.4f;
	public const float OpenAt = 0.55f;
	public const float OpenStepSeconds = 0.18f;
	public const float StepOutAt = 1.15f;
	public const float StepOutSeconds = 0.9f;
	/// <summary>When he has stepped out and the win is paid (Succeed): the column of light, the freed line.</summary>
	public const float FreedAt = StepOutAt + StepOutSeconds;
	private const float StepOutDistance = 40f;

	protected override string SiteGlyph => "door";

	// The Court's building places it, not the band.
	public override bool PlacedByChapterRules => true;

	// The whole door and a margin: the drawing is 160 x 300.
	protected override float SiteFootprint => 200f;

	// Arrival is the Gallery and this near the door (HasArrived): the Gallery's west wall has a hall behind it.
	protected override float ArrivalRadius => 440f;

	// His paint: the arcane ramp's hot stop, a stop paler than the seer's violet in the same house.
	protected override Color TellColor => new(0.910f, 0.753f, 1.0f);

	protected override string Announcement => "A door sketched in charcoal. Ink it in one stroke";

	protected override string Objective
	{
		get
		{
			if (inked)
				return "The door is inked in";
			if (stroke == null)
				return string.Empty;
			if (smudgedFor >= 0f && smudgedFor < 2.5f && !stroke.Begun)
				return "Smudged - begin again from any dot";
			if (!stroke.Begun)
				return $"Ink every line of the door once, in one stroke (0 of {figure.LineCount})";
			if (stroke.Stuck)
				return "No line left from here - walk the ink to smudge it";
			if (!stroke.OnLine)
				return $"Back to the line ({stroke.InkedCount} of {figure.LineCount})";
			return $"Ink every line once ({stroke.InkedCount} of {figure.LineCount})";
		}
	}

	// The arrow (off screen only) is on the door until it is begun - never on a dot, which would give the start away -
	// then on where the ink ends while he is off the line.
	protected override Vector2? PointerTarget => inked || stroke == null ? null
		: !stroke.Begun ? centre : stroke.OnLine ? null : PenWorld;

	protected override string ChestLine => "He left something in the doorway - a chest";

	// --- where it is -------------------------------------------------------------------------------------------

	private CourtLayout court;
	private Vector2 centre;

	/// <summary>The door's middle in world px.</summary>
	public Vector2 DoorCentre => centre;

	/// <summary>The run's figure, and the stroke being drawn on it. For the probes.</summary>
	public DoorFigure Figure => figure;
	public DoorStroke Stroke => stroke;

	/// <summary>Dot <paramref name="d"/> in world px.</summary>
	public Vector2 DotWorld(DoorFigure.Dot d) => centre + figure.DotPosition(d);

	/// <summary>Where the ink has got to, in world px: the head of the line in hand, or the dot the pen is at.</summary>
	public Vector2 PenWorld => stroke == null || !stroke.Begun ? centre
		: stroke.Active >= 0 ? centre + figure.Point(stroke.Active, stroke.ActiveFrom, stroke.ActiveT)
		: DotWorld(stroke.Pen);

	/// <summary>The Gallery's floor, for the probe: null off the Court.</summary>
	public CourtLayout Court => court;

	/// <summary>
	/// The door's middle in the building: on the Gallery's middle row, <see cref="CellsFromWestWall"/> in from its west
	/// wall, on even world pixels so the drawing's sheet pixels land on the world's 2 px grid. Null without the room.
	/// </summary>
	public static Vector2? DoorCentreIn(CourtLayout layout)
	{
		CourtLayout.Room room = layout?.RoomById(RoomId);
		if (room == null)
			return null;
		Vector2 c = layout.CellToWorld(new Vector2(room.Cells.Position.X + CellsFromWestWall, room.Cells.Position.Y + room.Cells.Size.Y * 0.5f));
		return new Vector2(Mathf.Round(c.X / 2f) * 2f, Mathf.Round(c.Y / 2f) * 2f);
	}

	public override void PlaceSites(ExplorePlacer placer)
	{
		// The run's figure, from the placer's seed: one of the family, mirrored or not (DoorFigure.Roll).
		ulong seed = ((ulong)(placer.NextFloat() * 4294967295f) << 32) | (ulong)(placer.NextFloat() * 4294967295f);
		figure = DoorFigure.Roll(seed);
		stroke = new DoorStroke(figure);
		court = CourtLayout.Load(placer.Origin);
		if (DoorCentreIn(court) is Vector2 c)
		{
			centre = c;
			AddSite(placer.FixedSite(centre, SiteFootprint));
			return;
		}
		// Not the Court (a tool run on another stage): one site on open ground, so the event still exists and says why.
		GD.PushError("DottedDoorEvent: the Court's layout has no Gallery; placing the door on open ground.");
		centre = placer.EventSite(SiteFootprint);
		AddSite(centre);
	}

	// --- the stroke ---------------------------------------------------------------------------------------------

	private DoorFigure figure;
	private DoorStroke stroke;
	private bool everBegun;
	private bool inked;
	private float inkedFor;
	private float householdClock;
	// Seconds since the drawing last smudged, below 0 before; and which lines were inked when it did, to draw the run.
	private float smudgedFor = -1f;
	private bool[] smudgedLines;
	// The ink made real: one body per inked line, on the layer only enemies mask.
	private StaticBody2D[] inkWalls;

	/// <summary>True once a stroke has ever begun (a smudge does not take it back: the household knows).</summary>
	public bool TraceBegun => everBegun;

	/// <summary>True while his feet are on the line in hand.</summary>
	public bool OnLine => stroke != null && stroke.OnLine;

	/// <summary>True once the last line is inked and the door inked in.</summary>
	public bool IsInked => inked;

	/// <summary>Lines inked in the stroke now, and the figure's whole count.</summary>
	public int LinesInked => stroke?.InkedCount ?? 0;
	public int LineCount => figure?.LineCount ?? 0;

	/// <summary>Times the drawing smudged, and ink-walls standing now. For the probe.</summary>
	public int Smudges => stroke?.Smudges ?? 0;
	public int WallsStanding
	{
		get
		{
			int n = 0;
			if (inkWalls != null)
				foreach (StaticBody2D w in inkWalls)
					if (w != null && IsInstanceValid(w)) n++;
			return n;
		}
	}

	/// <summary>Times he left the line after it had begun, and the seconds spent off it. For the probe.</summary>
	public int StepOffs { get; private set; }
	public float SecondsOffLine { get; private set; }

	/// <summary>How many of the household came in at the Gallery's doors. For the probe.</summary>
	public int HouseholdCalled { get; private set; }

	protected override bool HasArrived()
	{
		Player player = Player;
		if (player == null || Sites.Count == 0 || Sites[0].Done)
			return false;
		Vector2 feet = player.FootPosition;
		if (feet.DistanceTo(centre) > ArrivalRadius)
			return false;
		// In the Gallery, not in the hall behind its end wall.
		return court == null || court.RoomAt(feet)?.Id == RoomId;
	}

	protected override void Begin()
	{
		householdClock = 0f;
	}

	protected override void Tick(float delta)
	{
		Player player = Player;
		if (player == null)
			return;
		if (inked)
		{
			TickEnding(delta);
			return;
		}

		if (smudgedFor >= 0f)
			smudgedFor += delta;
		bool wasOn = stroke.OnLine;
		int inHand = stroke.Active;
		switch (stroke.Step(player.FootPosition - centre))
		{
			case StrokeEvent.Begun:
				everBegun = true;
				SfxPlayer.AtPosition(SfxCatalog.BearingFound, DotWorld(stroke.Pen), 0.02f);
				GD.Print($"DottedDoorEvent: a stroke begun at {stroke.Pen} (figure {figure.Index}{(figure.Mirrored ? ", mirrored" : "")}, "
					+ $"{figure.LineCount} lines, odd dots {figure.OddDots}).");
				break;
			case StrokeEvent.Inked:
				RaiseWall(LastInked());
				SfxPlayer.AtPosition(SfxCatalog.HintBrush, DotWorld(stroke.Pen), 0.02f);
				break;
			case StrokeEvent.Smudged:
				SmudgeOut();
				break;
			case StrokeEvent.Done:
				InkIn();
				return;
		}
		if (stroke.Begun && wasOn && !stroke.OnLine && inHand >= 0)
			StepOffs++;
		if (stroke.Begun && !stroke.OnLine)
			SecondsOffLine += delta;

		// The household, once a stroke has ever begun and while he is in the Gallery.
		if (everBegun && (court == null || court.RoomAt(player.FootPosition)?.Id == RoomId))
		{
			householdClock -= delta;
			if (householdClock <= 0f)
			{
				householdClock = HouseholdSeconds;
				CallHousehold();
			}
		}
	}

	private void CallHousehold()
	{
		if (Game == null || court == null)
			return;
		CourtLayout.Room room = court.RoomById(RoomId);
		foreach (AuthoredLayout.Gap door in court.Grid.Doors.Where(d => d.Room == RoomId))
		{
			Rect2 gap = court.CellsToWorld(door.Rect);
			Vector2 into = (room.Centre - gap.GetCenter());
			into = Mathf.Abs(into.X) > Mathf.Abs(into.Y) ? new Vector2(Mathf.Sign(into.X), 0f) : new Vector2(0f, Mathf.Sign(into.Y));
			for (int i = 0; i < HouseholdPerDoor; i++)
			{
				Vector2 at = gap.GetCenter() + into * HouseholdInside + new Vector2(into.Y, into.X) * (i - (HouseholdPerDoor - 1) * 0.5f) * 44f;
				if (Game.SpawnEventEnemy((string)null, at, 1f, false) != null)
					HouseholdCalled++;
			}
		}
	}

	// The line the last Inked event closed: the one inked whose wall is not up yet.
	private int LastInked()
	{
		for (int l = 0; l < figure.LineCount; l++)
			if (stroke.IsInked(l) && (inkWalls?[l] == null || !IsInstanceValid(inkWalls[l])))
				return l;
		return -1;
	}

	// His brush makes what it paints real (3.2): an inked line becomes a wall the household cannot cross. Thin segments
	// along it, on the layer only enemies mask - the player walks his own lines. Arcs as short chords.
	private void RaiseWall(int line)
	{
		if (line < 0 || Game == null)
			return;
		inkWalls ??= new StaticBody2D[figure.LineCount];
		var body = new StaticBody2D
		{
			Name = $"InkWall{line}",
			CollisionLayer = 1u << (LevelTilePainter.EnemyOnlyCollisionLayer - 1),
			CollisionMask = 0,
			Position = centre,
		};
		int pieces = figure.KindOf(line) == DoorFigure.Kind.Arc ? 6 : 1;
		DoorFigure.Dot from = figure.EndA(line);
		for (int k = 0; k < pieces; k++)
			body.AddChild(new CollisionShape2D
			{
				Shape = new SegmentShape2D
				{
					A = figure.Point(line, from, k / (float)pieces),
					B = figure.Point(line, from, (k + 1) / (float)pieces),
				},
			});
		Game.AddChild(body);
		inkWalls[line] = body;
	}

	private void DropWalls()
	{
		if (inkWalls == null)
			return;
		for (int l = 0; l < inkWalls.Length; l++)
		{
			if (inkWalls[l] != null && IsInstanceValid(inkWalls[l]))
				inkWalls[l].QueueFree();
			inkWalls[l] = null;
		}
	}

	// Walked along the ink: the drawing runs and fades back to charcoal (drawn over SmudgeSeconds from what was inked),
	// the walls go with it, and the stroke starts again from any dot.
	private void SmudgeOut()
	{
		smudgedLines ??= new bool[figure.LineCount];
		for (int l = 0; l < figure.LineCount; l++)
			smudgedLines[l] = inkWalls != null && inkWalls[l] != null && IsInstanceValid(inkWalls[l]);
		smudgedFor = 0f;
		DropWalls();
		SfxPlayer.AtPosition(SfxCatalog.EnemyMelee, centre, 0.04f);
		GD.Print($"DottedDoorEvent: smudged (smudge {stroke.Smudges}) - walked along an inked line.");
	}

	private void InkIn()
	{
		inked = true;
		inkedFor = 0f;
		// The door opens: the walls go with the ink, into the painted door.
		DropWalls();
		// The impact beat (.ai/cutscenes.md, the cinematic rules): the last line closes the drawing - a short shake through
		// Reduce Shake (ScreenShake.Kick), the brush's sound, and the ink running round the whole door in a flash.
		Game?.ShakeScreen(5f, 0.3f);
		SfxPlayer.AtPosition(SfxCatalog.HintBrush, centre, 0f);
		GD.Print($"DottedDoorEvent: the last line - the door inked in after {StepOffs} step(s) off the line, {SecondsOffLine:0.0} s off it, "
			+ $"{stroke.Smudges} smudge(s).");
	}

	private void TickEnding(float delta)
	{
		inkedFor += delta;
		if (inkedSprite != null && IsInstanceValid(inkedSprite))
		{
			inkedSprite.Visible = true;
			inkedSprite.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(inkedFor / InkSeconds, 0f, 1f));
			int frame = inkedFor < OpenAt ? 0 : Mathf.Clamp(1 + (int)((inkedFor - OpenAt) / OpenStepSeconds), 1, 3);
			if (frame != inkedSprite.Frame && frame == 1)
				SfxPlayer.AtPosition(SfxCatalog.EventReward, centre, 0.02f);
			inkedSprite.Frame = frame;
		}
		if (inkedFor < InkSeconds)
			QueueRedraw();   // the drawn figure fades under the painted door (_Draw)
		if (inkedFor >= StepOutAt && him == null)
			ShowHim();
		if (him != null && IsInstanceValid(him) && !Succeeded)
		{
			float t = Mathf.Clamp((inkedFor - StepOutAt) / StepOutSeconds, 0f, 1f);
			him.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(t * 2.5f, 0f, 1f));
			him.Position = HimAt(t);
			him.Frame = (int)(inkedFor * 10f) % Mathf.Max(1, him.Hframes);
		}
		if (inkedFor >= FreedAt && !Succeeded)
			Succeed(him != null && IsInstanceValid(him) ? him.GlobalPosition : centre + new Vector2(0f, HalfHeight));
	}

	/// <summary>For _UiShot: <paramref name="fraction"/> of a true stroke's lines inked as if walked, walls up; whole
	/// inks the door in.</summary>
	public void DebugTraceTo(float fraction)
	{
		if (IsDormant)
			Arrive();
		int lines = Mathf.RoundToInt(Mathf.Clamp(fraction, 0f, 1f) * figure.LineCount);
		DropWalls();
		stroke.DebugInk(lines);
		everBegun = true;
		for (int l = 0; l < figure.LineCount; l++)
			if (stroke.IsInked(l))
				RaiseWall(l);
		if (stroke.IsDone)
			InkIn();
	}

	// --- what stands there -----------------------------------------------------------------------------------------

	private const string ArtDir = "res://assets/bonelight/world/door/";
	private const string HimPath = "res://assets/bonelight/characters/maliang-moving.png";
	// tools/art/painted_door.py: both sheets 104x172 with the door's middle at (52, 92); the inked one four frames.
	private const int SheetMiddleX = 52;
	private const int SheetMiddleY = 92;
	private const int HimMiddle = 24;
	private const int HimFootRow = 46;
	private const int HimCell = 48;

	private Node2D house;
	private Sprite2D inkedSprite;
	private Sprite2D him;
	private float freedFor;

	protected override void OnPlaced()
	{
		// The charcoal figure is drawn by code (_Draw, on the event's own ground layer under the bodies), since every
		// run's is its own; the painted door it becomes is still art.
		Texture2D inkedTex = Load(ArtDir + "door-inked.png");
		if (inkedTex == null)
		{
			GD.PushError("DottedDoorEvent: the door's art is missing or not imported (python tools/art/painted_door.py, then import).");
			return;
		}
		house = new Node2D { Name = "DottedDoor", YSortEnabled = true };
		inkedSprite = Floor("Inked", inkedTex, 4);
		inkedSprite.Visible = false;
		house.AddChild(inkedSprite);
		// In the run's world at its origin, so its children stand at world positions: the event's sibling, which stays
		// (the door open on his light) after the win.
		Game?.CallDeferred(Node.MethodName.AddChild, house);
	}

	// The drawing lies on the floor, under every body, drawn at x2 on the world's 2 px grid.
	private Sprite2D Floor(string name, Texture2D texture, int frames) => new()
	{
		Name = name,
		Texture = texture,
		Hframes = frames,
		Centered = false,
		Scale = new Vector2(2f, 2f),
		TextureFilter = TextureFilterEnum.Nearest,
		Position = centre - new Vector2(SheetMiddleX, SheetMiddleY) * 2f,
		ZIndex = DrawLayers.Ground,
		ZAsRelative = false,
	};

	private void ShowHim()
	{
		Texture2D tex = Load(HimPath);
		if (tex == null || house == null)
			return;
		him = new Sprite2D
		{
			Name = "MaLiang",
			Texture = tex,
			Hframes = Mathf.Max(1, tex.GetWidth() / HimCell),
			Centered = false,
			Scale = new Vector2(2f, 2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Offset = new Vector2(-HimMiddle, WorldSprites.SortLift / 2f - HimFootRow),
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		him.Position = HimAt(0f);
		house.AddChild(him);
	}

	// In the doorway, his feet on its threshold, walking out of it toward the room.
	private Vector2 HimAt(float t)
	{
		Vector2 foot = centre + new Vector2(0f, HalfHeight - 6f + StepOutDistance * t);
		return new Vector2(Mathf.Round(foot.X), Mathf.Round(foot.Y - WorldSprites.SortLift));
	}

	private static Texture2D Load(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (him == null || !IsInstanceValid(him) || !Succeeded)
			return;
		// Free: he is gone into the column of light.
		freedFor += (float)delta;
		float a = Mathf.Clamp(1f - freedFor / 1.2f, 0f, 1f);
		him.Modulate = new Color(1f, 1f, 1f, a);
		if (a <= 0f)
		{
			him.QueueFree();
			him = null;
		}
	}

	// The boss has come: the drawing stops, and its walls go with it, so nothing of it outlives the event.
	protected override void OnStoodDown() => DropWalls();

	// --- the ink ------------------------------------------------------------------------------------------------

	private static readonly Color InkHot = new(0.910f, 0.753f, 1.0f);   // arcane hot (#E8C0FF)
	private static readonly Color InkMid = new(0.753f, 0.549f, 1.0f);   // arcane mid (#C08CFF)
	private static readonly Color InkEdge = new(0.478f, 0.247f, 0.831f); // arcane edge (#7A3FD4)
	private static readonly Color InkCore = new(1.0f, 0.941f, 1.0f);    // arcane core (#FFF0FF)

	// Charcoal on the oak: the figure as he finds it.
	private static readonly Color Charcoal = new(0.18f, 0.165f, 0.155f, 0.9f);
	private static readonly Color CharcoalEdge = new(0.35f, 0.325f, 0.30f, 0.55f);

	// Everything on the world's 2 px grid, as the sketch's pixels were: charcoal lines and dots for the whole figure;
	// his light along each inked line (the mid stop with the edge under it) and along the line in hand up to the ink,
	// the hot stop at its wet head; a dot lit once every line into it is inked; and after a smudge the ink running and
	// fading off what was drawn. On the event's ground layer: over the floor, under every body.
	public override void _Draw()
	{
		if (figure == null || !NearPlayer(centre))
			return;
		Vector2 origin = ToLocal(centre);
		if (inked)
		{
			// The drawing fades into the painted door.
			float fade = Mathf.Clamp(1f - inkedFor / InkSeconds, 0f, 1f);
			if (fade > 0f)
				for (int l = 0; l < figure.LineCount; l++)
					StampLine(origin, l, figure.EndA(l), 1f, new Color(InkMid, fade), new Color(InkEdge, fade), -1f);
			return;
		}
		for (int l = 0; l < figure.LineCount; l++)
			StampLine(origin, l, figure.EndA(l), 1f, Charcoal, CharcoalEdge, -1f);
		if (smudgedFor >= 0f && smudgedFor < SmudgeSeconds && smudgedLines != null)
		{
			// The run: the ink slides a little down the boards and thins out, so a mistake is seen happen.
			float u = smudgedFor / SmudgeSeconds;
			Vector2 slide = origin + new Vector2(0f, 2f * Mathf.Floor(u * 4f));
			for (int l = 0; l < figure.LineCount; l++)
				if (smudgedLines[l])
					StampLine(slide, l, figure.EndA(l), 1f, new Color(InkMid, 0.85f * (1f - u)), new Color(InkEdge, 0.6f * (1f - u)), -1f);
		}
		for (int l = 0; l < figure.LineCount; l++)
			if (stroke.IsInked(l))
				StampLine(origin, l, figure.EndA(l), 1f, InkMid, InkEdge, -1f);
		if (stroke.Active >= 0)
			StampLine(origin, stroke.Active, stroke.ActiveFrom, stroke.ActiveT, InkMid, InkEdge, 10f);
		foreach (DoorFigure.Dot d in figure.Dots)
		{
			Vector2 p = figure.DotPosition(d);
			var cell = new Vector2(Mathf.Floor(p.X / 2f) * 2f, Mathf.Floor(p.Y / 2f) * 2f) + origin;
			bool lit = stroke.DotDone(d);
			DrawRect(new Rect2(cell - new Vector2(2f, 2f), new Vector2(6f, 6f)), lit ? InkMid : Charcoal);
			DrawRect(new Rect2(cell, new Vector2(2f, 2f)), lit ? InkCore : CharcoalEdge);
		}
	}

	// One line, from `from`, up to `upTo` (0 to 1) of its length, 2 px a stamp; the last `head` px in the hot stop.
	private void StampLine(Vector2 origin, int line, DoorFigure.Dot from, float upTo, Color body, Color under, float head)
	{
		float len = figure.Length(line);
		float end = Mathf.Clamp(upTo, 0f, 1f) * len;
		for (float s = 0f; s <= end; s += 2f)
		{
			Vector2 p = figure.Point(line, from, s / len);
			var cell = new Vector2(Mathf.Floor(p.X / 2f) * 2f, Mathf.Floor(p.Y / 2f) * 2f) + origin;
			DrawRect(new Rect2(cell + new Vector2(0f, 2f), new Vector2(2f, 2f)), under);
			DrawRect(new Rect2(cell, new Vector2(2f, 2f)), head > 0f && s > end - head ? InkHot : body);
		}
	}

	protected override void DrawLight(CanvasItem light)
	{
		if (IsDormant)
		{
			base.DrawLight(light);
			return;
		}
		if (!NearPlayer(centre))
			return;
		if (!inked)
		{
			// Before a stroke, a soft light over the whole drawing - never on a dot, which would give the start away.
			// After, the wet head of the stroke, where he picks it up: brighter while he is off the line.
			if (stroke == null || !stroke.Begun)
			{
				ExploreArt.DrawGlow(light, ToLocal(centre), 150f, new Color(TellColor, 0.10f * Flicker(3)));
				return;
			}
			Vector2 headAt = ToLocal(PenWorld);
			float f = Flicker(3) * (stroke.OnLine ? 0.7f : 1f);
			ExploreArt.DrawGlow(light, headAt, 70f, new Color(TellColor, 0.22f * f));
			ExploreArt.DrawGlow(light, headAt, 24f, new Color(InkCore, 0.35f * f));
			return;
		}
		// The doorway open on his light, once it is painted.
		float open = Mathf.Clamp((inkedFor - OpenAt) / (OpenStepSeconds * 3f), 0f, 1f);
		Vector2 doorway = ToLocal(centre + new Vector2(0f, HalfHeight * 0.4f));
		ExploreArt.DrawGlow(light, doorway, 220f, new Color(TellColor, 0.18f * open));
		ExploreArt.DrawGlow(light, doorway, 90f, new Color(InkCore, 0.28f * open));
	}
}
