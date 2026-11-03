using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE SEER'S VOICE: the Court's first side event (.ai/chapter-wizards.md 4.10). "A court needs a seer. He keeps
// him in the empty house to read what is coming, and what is coming is the player."
//
// WHAT IT ASKS: SEEK. Nostradamus is never seen until the end; he is heard. His voice reads from one of the Court's
// rooms, chosen by the seed. Reach the reading place there and he is gone - an open book on a lectern, a candle
// still lit. Then the voice reads again, from another room; reach that one and he is gone again. The third time he
// is at his desk, and he is free. Nothing fails and nothing times out: the voice goes on reading until it is found.
//
// HEARD THROUGH WALLS. The building is fixed (CourtLayout), so the rooms are its own and the sound promises a
// straight line the house will not give: every reading is played from the direction of the room it comes from
// (300 px toward it from the player, as the chapter hints are, so a far room is as audible as a near one), with
// the stars of his cue drifting in from that edge of the screen and the line he reads set beside them
// (StageHintCue, StageHintCaption) - and the player must find a way round to it, by the halls and the signs at
// their junctions (.ai/endgame-castle.md 5.2). No arrow points at a reading: the first room has the far tell any
// dormant event has, and once he has arrived the only guides are the voice and its stars. With the sound off the
// stars and the line still say which way.
//
// ITS ROOMS. Seven of the Court's rooms may hold a reading: the Great Hall, the Kitchens, the Chapel, the Library,
// the Kennels, the Map Room and the Well Court. Not the Gallery (Ma Liang's door will be in it, 4.8 - two
// prisoners never share a room), not the Armoury (beside the stair the run starts on), not the Treasury (its
// Chamberlain comes out the moment anyone steps in), and not the Solar, the Minstrels' Gallery or the Keepsake
// Doors, which are each a way of their own to reach. The first is at least FirstFromStart from the stair, and each
// next one at least LegDistance from the last, so every leg is a walk across the house (ChooseReadings, held by
// RegressionChecks.ValidateSeersVoice on 400 seeds). Each reading place stands well in from the room's walls and off
// its furniture, so it is reached only from inside the room (ReachRadius against the inset).
//
// Nothing here is a body: the lectern, the desk and he are sprites y-sorted at their feet, never in "enemies", with
// no collision. No event enemy is spawned - the Court's household is the pressure, and walking its halls under it
// is the ask.
public partial class SeersVoiceEvent : SideEvent
{
	/// <summary>The rooms a reading may come from, in the order the seed's shuffle starts from.</summary>
	public static readonly IReadOnlyList<string> ReadingRoomIds = new[]
	{
		"great_hall", "kitchens", "chapel", "library", "kennels", "map_room", "well_court",
	};

	/// <summary>How many readings: two empty rooms, then his desk.</summary>
	public const int Readings = 3;

	/// <summary>The first reading place is at least this far (world px, straight) from the stair the run starts on.</summary>
	public const float FirstFromStart = 3000f;

	/// <summary>Each next reading place is at least this far (world px, straight) from the last.</summary>
	public const float LegDistance = 2400f;

	/// <summary>How close to a reading place he must come: inside the room, never through its wall (see SpotInset).</summary>
	public const float ReachRadius = 220f;

	/// <summary>A reading place stands at least this many cells in from every wall of its room...</summary>
	public const int SpotInsetCells = 4;

	/// <summary>...and at least this far (world px) from every piece of the room's furniture.</summary>
	public const float FurnitureClearance = 130f;

	/// <summary>Seconds between one reading and the next while he is sought.</summary>
	public const float ReadingSeconds = 8f;

	/// <summary>
	/// Seconds after a room is found empty before the voice reads from the next: past the 3.5 s the line under the
	/// timer says what was found (the announcement holds the screen that long, and the reading's line would wait).
	/// </summary>
	public const float FirstReadingDelay = 4f;

	/// <summary>How far toward the room the voice is played from the player (StageHintDirector's offset).</summary>
	public const float VoiceOffset = 300f;

	/// <summary>
	/// What he reads while he is sought, one list per leg: written for the game in the shape of his quatrains,
	/// never his own words, and about whoever is following (the hints' three lines are the chapter's, StageHintCatalog).
	/// </summary>
	public static readonly string[][] LegLines =
	{
		new[]
		{
			"He comes too late, and finds the candle lit.",
			"A book left open; the reader has gone on.",
		},
		new[]
		{
			"Twice to an empty chair. The third, the seer.",
			"The third room waits, and one who waits in it.",
		},
	};

	protected override string SiteGlyph => "seer";

	// Its rooms are the Court's building's, not the 4-6 chunk band's.
	public override bool PlacedByChapterRules => true;

	// The lectern's footprint, for the placer and the decor: the reading place is on a room's floor, well inside it.
	protected override float SiteFootprint => 120f;

	// Reached, not passed near: the place is inset SpotInsetCells from the walls, so this cannot be met through one.
	protected override float ArrivalRadius => ReachRadius;

	// His far tell is his own: the cold violet of everything he holds (the arcane ramp's mid, as the stars of the cue).
	protected override Color TellColor => new(0.753f, 0.549f, 1.0f);

	protected override string Announcement => "Gone - an open book, and a candle still lit";

	protected override string Objective
	{
		get
		{
			if (leg >= 2 && goneAgainFor > 0f)
				return "Gone again - the candle still lit";
			return $"Follow the voice reading ({Mathf.Min(leg + 1, Readings)} of {Readings})";
		}
	}

	// No arrow: he is heard, not seen (the stars and the line give the side). The first room's far tell, and the
	// wayfinder's arrow to it, are the dormant event's as anywhere.
	protected override Vector2? PointerTarget => null;

	protected override string ChestLine => "He left something on the desk - a chest";

	// --- where the readings are ---------------------------------------------------------------------

	private readonly List<Vector2> spots = new();
	private readonly List<string> rooms = new();

	/// <summary>The three reading places in order (world px), the last his desk. For the probe and the validator.</summary>
	public IReadOnlyList<Vector2> ReadingSpots => spots;

	/// <summary>The rooms they stand in, in order.</summary>
	public IReadOnlyList<string> ReadingRooms => rooms;

	/// <summary>
	/// Which reading he is sought for now: 0 dormant (the first room's), 1 and 2 the voice from the second and third
	/// rooms, 3 found. The place he must reach is <see cref="CurrentSpot"/>.
	/// </summary>
	public int Leg => leg;

	/// <summary>Where he must reach now (the next reading place), or null once he is found.</summary>
	public Vector2? CurrentSpot => leg < spots.Count ? spots[leg] : null;

	/// <summary>Readings heard during the seek (not counting the chapter's hints). For the probe.</summary>
	public int ReadingsHeard { get; private set; }

	/// <summary>Where the last reading was heard from (the place it came from), for the probe.</summary>
	public Vector2? LastReadingFrom { get; private set; }

	/// <summary>
	/// Where to stand him to see the desk, for _UiShot: in front of it, down-screen and just outside its reach, so the
	/// photograph is of him at his desk and not of the moment he is found.
	/// </summary>
	public Vector2 StandPoint
	{
		get
		{
			if (spots.Count == 0)
				return MainSite;
			Vector2 desk = spots[^1];
			// Beside it rather than below, so the desk is clear of the HUD's band at the top of a PC screen; on the room's
			// floor, so the photograph is not taken from inside a wall.
			foreach (Vector2 offset in new[] { new Vector2(ReachRadius + 40f, 30f), new Vector2(-ReachRadius - 40f, 30f), new Vector2(0f, ReachRadius + 50f) })
			{
				Vector2 p = desk + offset;
				if (court == null || (court.IsFloor(p) && court.RoomAt(p) == court.RoomAt(desk)))
					return p;
			}
			return desk + new Vector2(0f, ReachRadius + 50f);
		}
	}

	private CourtLayout court;

	/// <summary>
	/// The seed's three readings in a fixed building: the rooms (in the order the voice reads from them) and each
	/// one's reading place. Every roll is drawn before anything is tested (.ai/stage-secrets.md section 4): one per
	/// room for its place, then a shuffle of the room order; the first ordered three that keep the distances are
	/// taken. Pure - the validator runs it on paper for 400 seeds.
	/// </summary>
	public static (List<string> Rooms, List<Vector2> Spots) ChooseReadings(CourtLayout layout, Func<float> roll)
	{
		var rooms = new List<string>();
		var spots = new List<Vector2>();
		if (layout == null)
			return (rooms, spots);
		int n = ReadingRoomIds.Count;
		float[] placeRolls = new float[n];
		for (int i = 0; i < n; i++)
			placeRolls[i] = roll();
		float[] orderRolls = new float[n];
		for (int i = 0; i < n; i++)
			orderRolls[i] = roll();

		// Each room's reading place, from its roll, among the floor cells inset from its walls and off its furniture.
		var place = new Dictionary<string, Vector2>();
		for (int i = 0; i < n; i++)
		{
			CourtLayout.Room room = layout.RoomById(ReadingRoomIds[i]);
			if (room == null)
				continue;
			List<Vector2> candidates = PlacesIn(layout, room);
			if (candidates.Count == 0)
				continue;
			place[room.Id] = candidates[Mathf.Clamp((int)(placeRolls[i] * candidates.Count), 0, candidates.Count - 1)];
		}

		// The order: a Fisher-Yates shuffle of the rooms from the rolls drawn above.
		var order = ReadingRoomIds.Where(place.ContainsKey).ToList();
		for (int i = order.Count - 1; i > 0; i--)
		{
			int j = Mathf.Clamp((int)(orderRolls[i] * (i + 1)), 0, i);
			(order[i], order[j]) = (order[j], order[i]);
		}

		Vector2 start = layout.StageOrigin;
		foreach (string a in order)
		{
			if (place[a].DistanceTo(start) < FirstFromStart)
				continue;
			foreach (string b in order)
			{
				if (b == a || place[b].DistanceTo(place[a]) < LegDistance)
					continue;
				foreach (string c in order)
				{
					if (c == a || c == b || place[c].DistanceTo(place[b]) < LegDistance)
						continue;
					rooms.AddRange(new[] { a, b, c });
					spots.AddRange(new[] { place[a], place[b], place[c] });
					return (rooms, spots);
				}
			}
		}
		// Measured never to happen on the Court as built (ValidateSeersVoice); if the building changes so that it
		// does, the first three in the seed's order rather than no event.
		GD.PushWarning("SeersVoiceEvent: no three rooms keep the distances; taking the seed's first three.");
		foreach (string id in order.Take(Readings))
		{
			rooms.Add(id);
			spots.Add(place[id]);
		}
		return (rooms, spots);
	}

	/// <summary>The places a reading may stand in a room, in a fixed order: cell centres inset from its walls, on floor, off its furniture.</summary>
	public static List<Vector2> PlacesIn(CourtLayout layout, CourtLayout.Room room)
	{
		var found = new List<Vector2>();
		var furniture = layout.Grid.Furniture.Where(p => p.Room == room.Id).Select(p => layout.CellToWorld(p.At)).ToList();
		Rect2I cells = room.Cells;
		for (int y = cells.Position.Y + SpotInsetCells; y < cells.End.Y - SpotInsetCells; y++)
		{
			for (int x = cells.Position.X + SpotInsetCells; x < cells.End.X - SpotInsetCells; x++)
			{
				Vector2 p = layout.CellToWorld(new Vector2(x + 0.5f, y + 0.5f));
				if (!layout.IsFloor(p) || !ReachedOnlyFromInside(layout, room, p))
					continue;
				bool clear = true;
				foreach (Vector2 f in furniture)
				{
					if (f.DistanceTo(p) < FurnitureClearance)
					{
						clear = false;
						break;
					}
				}
				if (clear)
					found.Add(p);
			}
		}
		return found;
	}

	/// <summary>
	/// True when nothing walkable within <see cref="ReachRadius"/> of <paramref name="spot"/> lies outside the room but a
	/// doorway of it (a door gap touching the room) - so the place is reached only by coming into the room. Only a place
	/// near the room's edge can fail; the rest answer at once.
	/// </summary>
	public static bool ReachedOnlyFromInside(CourtLayout layout, CourtLayout.Room room, Vector2 spot)
	{
		Rect2 floor = room.World;
		float edge = Mathf.Min(Mathf.Min(spot.X - floor.Position.X, floor.End.X - spot.X), Mathf.Min(spot.Y - floor.Position.Y, floor.End.Y - spot.Y));
		if (edge >= ReachRadius + CourtLayout.TilePixels)
			return true;
		Rect2I near = room.Cells.Grow(1);
		int r = Mathf.CeilToInt(ReachRadius / CourtLayout.TilePixels) + 1;
		Vector2I c = layout.WorldToCell(spot);
		for (int y = c.Y - r; y <= c.Y + r; y++)
		{
			for (int x = c.X - r; x <= c.X + r; x++)
			{
				var cell = new Vector2I(x, y);
				if (room.Cells.HasPoint(cell) || !layout.Grid.IsWalkable(cell))
					continue;
				if (CourtLayout.DistanceTo(layout.CellsToWorld(new Rect2I(cell, Vector2I.One)), spot) >= ReachRadius)
					continue;
				bool doorway = false;
				foreach (AuthoredLayout.Gap door in layout.Grid.Doors)
				{
					if (door.Rect.HasPoint(cell) && door.Rect.Intersects(near))
					{
						doorway = true;
						break;
					}
				}
				if (!doorway)
					return false;
			}
		}
		return true;
	}

	public override void PlaceSites(ExplorePlacer placer)
	{
		CourtLayout layout = CourtLayout.Load(placer.Origin);
		court = layout;
		(List<string> chosen, List<Vector2> places) = ChooseReadings(layout, placer.NextFloat);
		if (places.Count < Readings)
		{
			// Not the Court (a tool run on another stage), or a layout that will not read: one site on open ground, so the
			// event still exists and says why.
			GD.PushError("SeersVoiceEvent: the Court's layout gave no readings; placing one site on open ground.");
			Vector2 site = placer.EventSite(SiteFootprint);
			chosen = new List<string> { "", "", "" };
			places = new List<Vector2> { site, site, site };
		}
		rooms.AddRange(chosen);
		spots.AddRange(places);
		// Only the first is a point of interest: an arrow on the second or third (the wayfinder's) would be the answer.
		AddSite(placer.FixedSite(spots[0], SiteFootprint));
	}

	// --- what stands there --------------------------------------------------------------------------

	private const string ArtDir = "res://assets/bonelight/world/seer/";
	private const string HimPath = "res://assets/bonelight/characters/nostradamus-idle.png";
	// tools/art/seers_voice.py: the lectern's cell is 36x44, its foot on row 41, middle column 18; the desk's 48x56,
	// its front's foot on row 52, middle 24. Both 4 frames of the candle flickering. He stands behind the desk on its
	// middle column, his feet 8 sheet px north of its foot, so its front hides him to the waist; his own cell is the
	// wizards' 48, feet on row 46.
	private const int LecternMiddle = 18;
	private const int LecternFootRow = 41;
	private const int DeskMiddle = 24;
	private const int DeskFootRow = 52;
	private const int HimMiddle = 24;
	private const int HimFootRow = 46;
	private const int HimCell = 48;
	private static readonly Vector2 HimBehindDesk = new(0f, -16f);
	// Where the candle's flame is on each sheet (frame 0, sheet px from the anchor), for its glow on the light layer.
	private static readonly Vector2 LecternFlame = new((3 - LecternMiddle) * 2f, (5 - LecternFootRow) * 2f);
	private static readonly Vector2 DeskFlame = new((39 - DeskMiddle) * 2f, (9 - DeskFootRow) * 2f);

	private Node2D house;
	private readonly Sprite2D[] places = new Sprite2D[Readings];
	private Sprite2D him;
	private Texture2D lecternTex;
	private Texture2D deskTex;
	private Texture2D himTex;
	private float freedFor;

	private int leg;
	private float readingTimer = -1f;
	private int readingsThisLeg;
	private float goneAgainFor;
	private StageHintCaption caption;
	private StageHintCue cue;
	private bool holdingHintTurn;

	protected override void OnPlaced()
	{
		lecternTex = Load(ArtDir + "seer-lectern.png");
		deskTex = Load(ArtDir + "seer-desk.png");
		himTex = Load(HimPath);
		if (lecternTex == null || deskTex == null || himTex == null)
		{
			GD.PushError("SeersVoiceEvent: the seer's art is missing or not imported (python tools/art/seers_voice.py, build.py, then import).");
			return;
		}
		house = new Node2D { Name = "SeersRooms", YSortEnabled = true };
		// The first room's reading place stands from the start; the others are laid when the voice moves to them, in
		// rooms far off screen - nobody sees a lectern appear.
		ShowPlace(0);
		// In the run's world, at its origin, so its children stand at world positions: the event's sibling, which stays
		// (the desk, its candle burning on) after the win.
		Game?.CallDeferred(Node.MethodName.AddChild, house);

		var overlay = Game?.GetNodeOrNull<CanvasLayer>("UIOverlay");
		if (overlay != null)
		{
			caption = new StageHintCaption { Name = "SeersVoiceCaption" };
			overlay.AddChild(caption);
		}
		if (Game != null)
		{
			cue = new StageHintCue { Name = "SeersVoiceCue" };
			Game.CallDeferred(Node.MethodName.AddChild, cue);
		}
	}

	private void ShowPlace(int i)
	{
		if (house == null || i < 0 || i >= spots.Count || places[i] != null)
			return;
		bool desk = i == Readings - 1;
		places[i] = Grounded(desk ? "Desk" : $"Lectern{i}", desk ? deskTex : lecternTex, 4, spots[i],
			desk ? DeskMiddle : LecternMiddle, desk ? DeskFootRow : LecternFootRow);
		house.AddChild(places[i]);
		if (OS.GetCmdlineUserArgs().Contains("--explore-log"))
			GD.Print($"EXPLORE seer: reading place {i + 1} laid in {rooms[i]} at ({spots[i].X:0},{spots[i].Y:0}), house in tree {house.IsInsideTree()}");
		if (desk)
		{
			him = Grounded("Nostradamus", himTex, Mathf.Max(1, himTex.GetWidth() / HimCell), spots[i] + HimBehindDesk, HimMiddle, HimFootRow);
			house.AddChild(him);
		}
	}

	// Standing in the world and sorted with the actors at its foot (WorldSprites.SortLift); drawn at x2, so the offset
	// is in sheet pixels. GreatAnvilEvent's helper, for the same reason.
	private static Sprite2D Grounded(string name, Texture2D texture, int frames, Vector2 foot, int anchorX, int footRow) => new()
	{
		Name = name,
		Texture = texture,
		Hframes = frames,
		Centered = false,
		Scale = new Vector2(2f, 2f),
		TextureFilter = TextureFilterEnum.Nearest,
		Position = new Vector2(Mathf.Round(foot.X), Mathf.Round(foot.Y - WorldSprites.SortLift)),
		Offset = new Vector2(-anchorX, WorldSprites.SortLift / 2f - footRow),
	};

	private static Texture2D Load(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

	public override void _ExitTree()
	{
		ReleaseHintTurn();
		if (caption != null && IsInstanceValid(caption))
			caption.QueueFree();
		if (cue != null && IsInstanceValid(cue))
			cue.QueueFree();
		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		float dt = (float)delta;
		// Found, or stood down: nothing reads any more, and a line still fading in from the last reading goes.
		if (IsResolved && caption != null && caption.Showing)
		{
			caption.Dismiss();
			cue?.Dismiss();
			ReleaseHintTurn();
		}
		// The candles burn whether anyone has come or not: steady, lean, gutter, lean back, at a candle's uneven pace.
		for (int i = 0; i < places.Length; i++)
		{
			if (places[i] != null && IsInstanceValid(places[i]))
				places[i].Frame = CandleFrame(i);
		}
		if (him == null || !IsInstanceValid(him))
			return;
		// He reads at his desk, the sphere raised, a breath of his idle at a time - until he is free, when he is gone into
		// the column of light.
		him.Frame = (int)(Clock * 2.2f) % Mathf.Max(1, him.Hframes);
		if (Succeeded)
		{
			freedFor += dt;
			float a = Mathf.Clamp(1f - freedFor / 1.2f, 0f, 1f);
			him.Modulate = new Color(1f, 1f, 1f, a);
			if (a <= 0f)
			{
				him.QueueFree();
				him = null;
			}
		}
	}

	private int CandleFrame(int i)
	{
		float t = Clock * 5.5f + i * 1.7f;
		float wobble = Mathf.Sin(t) + 0.6f * Mathf.Sin(t * 2.3f + i);
		return wobble > 1.1f ? 2 : wobble > 0.3f ? 1 : wobble < -0.9f ? 3 : 0;
	}

	// --- the rules -------------------------------------------------------------------------------

	protected override void Begin()
	{
		// He reached the first reading place: gone. The far tell is put out (the first room's arrow with it), and the
		// voice reads next from the second room.
		foreach (PointOfInterest site in Sites)
			site.Done = true;
		Gone();
	}

	private void Gone()
	{
		leg++;
		SfxPlayer.AtPosition(SfxCatalog.BearingFound, spots[Math.Min(leg - 1, spots.Count - 1)], 0.02f);
		if (leg >= Readings)
			return;
		ShowPlace(leg);
		readingTimer = FirstReadingDelay;
		readingsThisLeg = 0;
		goneAgainFor = leg >= 2 ? 3.5f : 0f;
		caption?.Dismiss();
		cue?.Dismiss();
	}

	protected override void Tick(float delta)
	{
		if (goneAgainFor > 0f)
			goneAgainFor -= delta;
		if (!caption?.Showing ?? true)
			ReleaseHintTurn();
		if (leg >= Readings || Player == null)
			return;

		Vector2 target = spots[leg];
		if (PlayerWithin(target, ReachRadius))
		{
			if (leg == Readings - 1)
			{
				leg = Readings;
				caption?.Dismiss();
				cue?.Dismiss();
				ReleaseHintTurn();
				Succeed(target);
				return;
			}
			Gone();
			return;
		}

		readingTimer -= delta;
		if (readingTimer <= 0f)
		{
			readingTimer = ReadingSeconds;
			Read(target);
		}
	}

	/// <summary>
	/// One reading: his voice from the room's direction, the stars of his cue in from that edge, and the line beside
	/// them. Public for _UiShot, which photographs one; a run reads on its own clock.
	/// </summary>
	public void Read(Vector2 from)
	{
		Player player = Player;
		if (player == null)
			return;
		string[] lines = LegLines[Mathf.Clamp(leg - 1, 0, LegLines.Length - 1)];
		string line = lines[readingsThisLeg % lines.Length];
		readingsThisLeg++;
		ReadingsHeard++;
		LastReadingFrom = from;
		Vector2 toward = from - player.GlobalPosition;
		float distance = toward.Length();
		Vector2 heard = player.GlobalPosition + (distance > 1f ? toward / distance : Vector2.Up) * Mathf.Min(VoiceOffset, distance);
		SfxPlayer.AtPosition(SfxCatalog.HintQuatrain, heard, 0f);
		cue?.Play(HintCue.Stars, player, from, TellColor);
		// The line waits its turn on the screen (a level-up, an achievement plate); the voice and the stars never do.
		if (caption == null)
			return;
		if (holdingHintTurn || Announcements.Request(this, Announcements.Kind.Hint))
		{
			holdingHintTurn = true;
			caption.ShowLine(line, HintVoice.Speech, player, from);
		}
		else
			// Not this time: out of the line again, rather than holding a place for a line already said.
			Announcements.Release(this);
	}

	/// <summary>
	/// For _UiShot only: begins the event if it is dormant and moves it on to <paramref name="targetLeg"/> as if each room
	/// before it had been found empty, laying each next place. A run never calls it.
	/// </summary>
	public void DebugAdvanceTo(int targetLeg)
	{
		if (IsDormant)
			Arrive();
		while (leg < Mathf.Min(targetLeg, Readings - 1))
			Gone();
	}

	private void ReleaseHintTurn()
	{
		if (!holdingHintTurn)
			return;
		holdingHintTurn = false;
		Announcements.Release(this);
	}

	protected override void OnStoodDown()
	{
		caption?.Dismiss();
		cue?.Dismiss();
		ReleaseHintTurn();
	}

	// --- light ----------------------------------------------------------------------------------

	protected override void DrawLight(CanvasItem light)
	{
		base.DrawLight(light);
		// The candles: a small warm pool round each flame that stands, the one warm light in his cold house.
		for (int i = 0; i < places.Length; i++)
		{
			if (places[i] == null || !IsInstanceValid(places[i]))
				continue;
			Vector2 flame = spots[i] + (i == Readings - 1 ? DeskFlame : LecternFlame);
			if (!NearPlayer(flame))
				continue;
			float f = Flicker(i + 3);
			ExploreArt.DrawGlow(light, ToLocal(flame), 120f, new Color(1f, 0.82f, 0.5f, 0.16f * f));
			ExploreArt.DrawGlow(light, ToLocal(flame), 40f, new Color(1f, 0.9f, 0.65f, 0.28f * f));
		}
	}
}
