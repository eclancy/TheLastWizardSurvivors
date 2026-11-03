using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE DOTTED DOOR: the Court's second side event (.ai/chapter-wizards.md 4.8). "He keeps him to paint: the Gallery's
// portraits are his work, done under orders, and his brush paints nothing real while it is held. He drew himself a door
// in the Gallery's end wall and was never let finish it."
//
// WHAT IT ASKS: TRACE. At the Gallery's west end a door is sketched in charcoal, an arch about 300 px tall, fifteen dots
// along its outline, numbered. From the first dot, the player walks the line dot to dot: wherever his feet stay within
// TraceTolerance of it, his own light follows them as ink (the pen), dot after dot lighting as it passes. Stepping off
// the line only pauses it - the ink stays where it got to and nothing ever resets - and he picks the line up again where
// the ink ends. The last dot inks the whole door in: it is painted, red lacquer and gold studs, a real door; it opens on
// his light and Ma Liang steps out of it, and goes into the column of light.
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
// Nothing here is a body: the drawing lies on the floor (DrawLayers.Ground), he is a sprite y-sorted at his feet, never
// in "enemies", with no collision.
public partial class DottedDoorEvent : SideEvent
{
	/// <summary>The room the door is drawn in.</summary>
	public const string RoomId = "gallery";

	// --- the door's path, in world px: tools/art/painted_door.py's HALF_W, HALF_H and ARCH_R at x2 ---------------------
	public const float HalfWidth = 80f;
	public const float HalfHeight = 150f;
	public const float ArchRadius = 80f;

	/// <summary>The dots along the outline, numbered from the left jamb's foot to the right's.</summary>
	public const int DotCount = 15;

	/// <summary>How near the line his feet must stay for the ink to follow them.</summary>
	public const float TraceTolerance = 30f;

	/// <summary>How far along the line ahead of the ink a step may take it in one frame (a chord, not a jump).</summary>
	public const float PenLookAhead = 120f;

	/// <summary>How far behind the ink his feet may be and still count as on the line.</summary>
	public const float PenLookBack = 30f;

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

	public static float JambLength => 2f * HalfHeight - ArchRadius;
	public static float ArcLength => Mathf.Pi * ArchRadius;
	public static float PathLength => 2f * JambLength + ArcLength;

	/// <summary>The point <paramref name="s"/> world px along the outline from the left jamb's foot, relative to the door's middle.</summary>
	public static Vector2 PathPoint(float s)
	{
		s = Mathf.Clamp(s, 0f, PathLength);
		if (s <= JambLength)
			return new Vector2(-HalfWidth, HalfHeight - s);
		if (s <= JambLength + ArcLength)
		{
			float a = Mathf.Pi + (s - JambLength) / ArchRadius;
			float spring = -HalfHeight + ArchRadius;
			return new Vector2(ArchRadius * Mathf.Cos(a), spring + ArchRadius * Mathf.Sin(a));
		}
		return new Vector2(HalfWidth, -HalfHeight + ArchRadius + (s - JambLength - ArcLength));
	}

	/// <summary>How far along the outline dot <paramref name="k"/> (0-based) stands.</summary>
	public static float DotArc(int k) => k * PathLength / (DotCount - 1);

	/// <summary>The point of the outline between arcs <paramref name="from"/> and <paramref name="to"/> nearest <paramref name="rel"/>.</summary>
	public static float NearestArc(Vector2 rel, float from, float to, out float distance)
	{
		float best = from;
		distance = float.MaxValue;
		from = Mathf.Max(0f, from);
		to = Mathf.Min(PathLength, to);
		// Every 2 px, and the window's far end itself, so the last dot can always be reached.
		for (float s = from; ; s = Mathf.Min(to, s + 2f))
		{
			float d = PathPoint(s).DistanceTo(rel);
			if (d < distance)
			{
				distance = d;
				best = s;
			}
			if (s >= to)
				break;
		}
		return best;
	}

	/// <summary>
	/// THE TRACE RULE, one step: where the ink is after his feet stand at <paramref name="feetRel"/> (relative to the
	/// door's middle). Before it has begun, only the first dot begins it. After, the nearest point of the line a little
	/// behind the ink to a chord's length ahead of it, within the tolerance, carries the ink to it - never back, so a
	/// step off the line pauses it and nothing resets. Pure: the validator traces it on paper.
	/// </summary>
	public static (float Pen, bool Begun, bool OnLine) Step(float pen, bool begun, Vector2 feetRel)
	{
		if (!begun)
		{
			bool atFirst = feetRel.DistanceTo(PathPoint(0f)) <= TraceTolerance;
			return (0f, atFirst, atFirst);
		}
		float s = NearestArc(feetRel, pen - PenLookBack, pen + PenLookAhead, out float d);
		if (d > TraceTolerance)
			return (pen, true, false);
		return (Mathf.Max(pen, s), true, true);
	}

	/// <summary>How many dots the ink has reached at <paramref name="pen"/>.</summary>
	public static int DotsReached(float pen, bool begun)
	{
		if (!begun)
			return 0;
		int n = 0;
		for (int k = 0; k < DotCount; k++)
			if (DotArc(k) <= pen + 0.5f)
				n++;
		return n;
	}

	protected override string SiteGlyph => "door";

	// The Court's building places it, not the band.
	public override bool PlacedByChapterRules => true;

	// The whole door and a margin: the drawing is 160 x 300.
	protected override float SiteFootprint => 200f;

	// Arrival is the Gallery and this near the door (HasArrived): the Gallery's west wall has a hall behind it.
	protected override float ArrivalRadius => 440f;

	// His paint: the arcane ramp's hot stop, a stop paler than the seer's violet in the same house.
	protected override Color TellColor => new(0.910f, 0.753f, 1.0f);

	protected override string Announcement => "A door sketched in charcoal, its dots numbered";

	protected override string Objective
	{
		get
		{
			if (inked)
				return "The door is inked in";
			if (!begun)
				return $"Trace the door from dot 1 (0 of {DotCount})";
			if (!onLine)
				return $"Back to the line - dot {Mathf.Min(DotsInked + 1, DotCount)}";
			return $"Trace the door dot to dot ({DotsInked} of {DotCount})";
		}
	}

	// The arrow (off screen only) is on the first dot until it is begun, then on the ink while he is off the line.
	protected override Vector2? PointerTarget => inked ? null : !begun ? DotWorld(0) : onLine ? null : PenWorld;

	protected override string ChestLine => "He left something in the doorway - a chest";

	// --- where it is -------------------------------------------------------------------------------------------

	private CourtLayout court;
	private Vector2 centre;

	/// <summary>The door's middle in world px.</summary>
	public Vector2 DoorCentre => centre;

	/// <summary>Dot <paramref name="k"/> (0-based) in world px.</summary>
	public Vector2 DotWorld(int k) => centre + PathPoint(DotArc(Mathf.Clamp(k, 0, DotCount - 1)));

	/// <summary>Where the ink has got to, in world px.</summary>
	public Vector2 PenWorld => centre + PathPoint(pen);

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

	// --- the trace ----------------------------------------------------------------------------------------------

	private float pen;
	private bool begun;
	private bool onLine;
	private bool inked;
	private float inkedFor;
	private float householdClock;
	private float offLineFor;

	/// <summary>How far the ink has got along the outline (world px).</summary>
	public float Pen => pen;

	/// <summary>True once he has stood on the first dot.</summary>
	public bool TraceBegun => begun;

	/// <summary>True while his feet are on the line (within the tolerance, near the ink).</summary>
	public bool OnLine => onLine;

	/// <summary>True once the last dot is reached and the door inked in.</summary>
	public bool IsInked => inked;

	/// <summary>Dots the ink has reached.</summary>
	public int DotsInked => DotsReached(pen, begun);

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

		bool wasOn = onLine;
		bool wasBegun = begun;
		(pen, begun, onLine) = Step(pen, begun, player.FootPosition - centre);
		if (begun && !wasBegun)
		{
			SfxPlayer.AtPosition(SfxCatalog.BearingFound, DotWorld(0), 0.02f);
			GD.Print($"DottedDoorEvent: the trace begun at dot 1 ({DotWorld(0).X:0},{DotWorld(0).Y:0}).");
		}
		if (begun && wasOn && !onLine)
			StepOffs++;
		if (begun && !onLine)
		{
			offLineFor += delta;
			SecondsOffLine += delta;
		}
		else
			offLineFor = 0f;

		if (begun && pen >= PathLength - 0.5f)
		{
			InkIn();
			return;
		}

		// The household, while the trace is begun and he is in the Gallery.
		if (begun && (court == null || court.RoomAt(player.FootPosition)?.Id == RoomId))
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

	private void InkIn()
	{
		inked = true;
		inkedFor = 0f;
		pen = PathLength;
		onLine = true;
		// The impact beat (.ai/cutscenes.md, the cinematic rules): the last dot closes the drawing - a short shake through
		// Reduce Shake (ScreenShake.Kick), the brush's sound, and the ink running round the whole door in a flash.
		Game?.ShakeScreen(5f, 0.3f);
		SfxPlayer.AtPosition(SfxCatalog.HintBrush, centre, 0f);
		GD.Print($"DottedDoorEvent: the last dot - the door inked in after {StepOffs} step(s) off the line, {SecondsOffLine:0.0} s off it.");
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
		if (sketchSprite != null && IsInstanceValid(sketchSprite))
			sketchSprite.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(1f - inkedFor / InkSeconds, 0f, 1f));
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

	/// <summary>For _UiShot: the trace carried to <paramref name="fraction"/> of the outline as if walked, begun if not.</summary>
	public void DebugTraceTo(float fraction)
	{
		if (IsDormant)
			Arrive();
		begun = true;
		onLine = true;
		pen = Mathf.Clamp(fraction, 0f, 1f) * PathLength;
		if (pen >= PathLength - 0.5f)
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
	private Sprite2D sketchSprite;
	private Sprite2D inkedSprite;
	private Sprite2D him;
	private float freedFor;

	protected override void OnPlaced()
	{
		Texture2D sketchTex = Load(ArtDir + "door-sketch.png");
		Texture2D inkedTex = Load(ArtDir + "door-inked.png");
		if (sketchTex == null || inkedTex == null)
		{
			GD.PushError("DottedDoorEvent: the door's art is missing or not imported (python tools/art/painted_door.py, then import).");
			return;
		}
		house = new Node2D { Name = "DottedDoor", YSortEnabled = true };
		sketchSprite = Floor("Sketch", sketchTex, 1);
		inkedSprite = Floor("Inked", inkedTex, 4);
		inkedSprite.Visible = false;
		house.AddChild(sketchSprite);
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

	protected override void OnStoodDown() { }

	// --- the ink ------------------------------------------------------------------------------------------------

	private static readonly Color InkHot = new(0.910f, 0.753f, 1.0f);   // arcane hot (#E8C0FF)
	private static readonly Color InkMid = new(0.753f, 0.549f, 1.0f);   // arcane mid (#C08CFF)
	private static readonly Color InkEdge = new(0.478f, 0.247f, 0.831f); // arcane edge (#7A3FD4)
	private static readonly Color InkCore = new(1.0f, 0.941f, 1.0f);    // arcane core (#FFF0FF)

	public override void _Draw()
	{
		if (!begun || inked || !NearPlayer(centre))
			return;
		// His light along the line behind his feet, stamped on the world's 2 px grid as the sketch's pixels are: the mid
		// stop with the edge under it, the hot one at the wet head of the stroke. Over the charcoal, under the bodies.
		Vector2 origin = ToLocal(centre);
		for (float s = 0f; s <= pen; s += 2f)
		{
			Vector2 p = PathPoint(s);
			var cell = new Vector2(Mathf.Floor(p.X / 2f) * 2f, Mathf.Floor(p.Y / 2f) * 2f) + origin;
			bool head = s > pen - 10f;
			DrawRect(new Rect2(cell + new Vector2(0f, 2f), new Vector2(2f, 2f)), InkEdge);
			DrawRect(new Rect2(cell, new Vector2(2f, 2f)), head ? InkHot : InkMid);
		}
		// The dots reached, inked: a 6 px point of his light over each charcoal one.
		for (int k = 0; k < DotCount; k++)
		{
			if (DotArc(k) > pen + 0.5f)
				break;
			Vector2 d = PathPoint(DotArc(k));
			var cell = new Vector2(Mathf.Floor(d.X / 2f) * 2f, Mathf.Floor(d.Y / 2f) * 2f) + origin;
			DrawRect(new Rect2(cell - new Vector2(2f, 2f), new Vector2(6f, 6f)), InkMid);
			DrawRect(new Rect2(cell, new Vector2(2f, 2f)), InkCore);
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
			// The wet head of the stroke, where he must pick it up: brighter while he is off the line.
			Vector2 headAt = ToLocal(begun ? PenWorld : DotWorld(0));
			float f = Flicker(3) * (onLine ? 0.7f : 1f);
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
