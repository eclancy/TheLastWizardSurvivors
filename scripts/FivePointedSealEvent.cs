using Godot;
using WizardSurvivors.scripts;

// The Mystic Ruins' side event: the Five-Pointed Seal (.ai/chapter-wizards.md 4.5).
//
// "The old wards were broken from the inside." A school of wards was Abe no Seimei's kind of house, and
// the dark wizard turned a ward on its maker: the onmyoji kneels in the middle of his own five-pointed
// seal, a star cut into a courtyard floor and full of the dark wizard's cold light. Four guardian stones
// stand at its quarters, each cut with one arrow. Stepping on a stone's plate turns it a quarter,
// clockwise - AND THE TWO STONES BESIDE IT, the ones the ring of the seal runs to from it. When all four
// face the seal, it breaks, and he is free.
//
// What it asks: SOLVE. Every other event is a place, a target, a carry or a fight; this one is a puzzle
// (Eric, 2026-11-01: "Yes I like including actual puzzles in the game"), and the horde is what makes
// walking the plates cost something. It is the one rescue that can be done without killing anything, and
// one that nothing can ruin: pressing a plate four times is no press at all, so every state can be undone
// and none locks (the event rules, side-events.md).
//
// THE PUZZLE IS ALWAYS SOLVABLE, AND NEVER SHORT. A press adds a quarter to its own stone and its two
// neighbours: over Z4 that is the circulant matrix (1 1 0 1), whose determinant is -3, a unit mod 4, so
// every one of the 256 states has exactly one answer in press counts 0-3 a plate, and that answer is the
// fewest presses (presses commute, and four of one cancel). The start is MADE from the answer: the seed
// rolls how many times each plate must be pressed (at least MinPresses in all, so never fewer than six),
// and the stones are turned back from the solved seal by legal presses until that is what it takes.
// RegressionChecks.ValidateSealPuzzle walks all 256 states from the solved seal to prove the first half,
// and the generator over a thousand roll sets to prove the second.
//
// THE TELL. One press shows the rule: the three stones turn on the same beat and flare, and the seal's
// light runs out along its ring from the pressed stone to the two it moves, and not to the one opposite.
//
// NOT THE BROKEN WARD. The Ruins' spell vault (BrokenWard) is an ORDER: five tall notched stones touched
// one after another. This is a STATE: four low square blocks, each turned to face the middle, in any
// order. The art is kept apart on purpose (tools/art/five_pointed_seal.py).
//
// A stone turns when the player STEPS onto its plate - his feet arriving, not standing - and turns again
// only once he has stepped off (RearmRadius). Standing on it does nothing more. The stones are not
// bodies: the courtyard is walkable all over, the plates are on the floor, and nothing here joins
// "enemies".
//
// Art: tools/art/five_pointed_seal.py (world/seal/). The courtyard is a ground decal, the stones and he
// sort with the bodies at their feet, the plates lie on the floor. The light is code, on the additive
// layer every event has.
public partial class FivePointedSealEvent : SideEvent
{
	public const int StoneCount = 4;

	/// <summary>A stone's foot from the seal's middle, world px (the courtyard is drawn at x2).</summary>
	public const float StoneDistance = 180f;

	/// <summary>A plate's middle from the seal's middle: outside its stone, the side the player comes from.</summary>
	public const float PlateDistance = 244f;

	/// <summary>His feet this near a plate's middle are on it.</summary>
	public const float PlateRadius = 30f;

	/// <summary>And this far off it, he has stepped off: the next step on turns the stone again.</summary>
	public const float RearmRadius = 52f;

	/// <summary>How far below the player's node his feet are (player.tscn's Feet, WorldSprites.SortLift).</summary>
	private const float FeetBelow = 36f;

	/// <summary>The fewest presses any seed's start can take: a puzzle, never a formality.</summary>
	public const int MinPresses = 6;

	// How long the light runs along the ring after a press, and how long it takes to reach the neighbours.
	private const float PressTellSeconds = 1.4f;
	private const float PressRunSeconds = 0.35f;

	[Export] public float GuardIntervalSeconds { get; set; } = 6f;

	// North, east, south, west: the order of the stones round the seal, of the arrow frames in the sheet,
	// and of a quarter turn clockwise.
	private static readonly Vector2[] Quarters = { Vector2.Up, Vector2.Right, Vector2.Down, Vector2.Left };

	private Vector2 centre;
	private readonly int[] facing = new int[StoneCount];
	private readonly bool[] armed = { true, true, true, true };
	private readonly float[] turnedAt = { -10f, -10f, -10f, -10f };
	private readonly float[] pressedAt = { -10f, -10f, -10f, -10f };
	private bool laidOut;
	private float guardTimer;
	private int steps;

	protected override string Announcement => "Someone kneels inside a seal cut in the floor.";
	protected override string ChestLine => "The seal is broken - a chest was left inside it";
	protected override string Objective => $"Turn the four stones to face the seal  ({Aligned} of {StoneCount})";
	// To the courtyard while he is away from it; nothing once he is in it. Pointing at a plate would be
	// solving the puzzle for him, and the plates are in plain sight.
	protected override Vector2? PointerTarget =>
		IsResolved || Player == null || PlayerWithin(centre, PlateDistance + 120f) ? null : centre;
	protected override string SiteGlyph => "seal";
	// The courtyard and the plates outside it, and a little more: placement wants all of it open (no wall
	// of the ruins through the star), and the furnishing keeps off it.
	protected override float SiteFootprint => PlateDistance + 46f;
	protected override float ArrivalRadius => 440f;
	protected override Color TellColor => new Color(0.753f, 0.549f, 1.0f);   // the arcane ramp's mid #C08CFF

	/// <summary>Stones facing the seal now (0 to 4). Public for the probe.</summary>
	public int Aligned
	{
		get
		{
			int n = 0;
			for (int i = 0; i < StoneCount; i++)
				if (IsFacing(i)) n++;
			return n;
		}
	}

	/// <summary>Plates pressed so far this run. For the probe.</summary>
	public int Steps => steps;

	/// <summary>The fewest presses that would break the seal from here. For the probe.</summary>
	public int PressesLeft => Sum(Solve(Offsets()));

	/// <summary>
	/// A plate an answer still needs pressed, nearest the player first; -1 once the seal faces in. For the
	/// probe, which has to solve it rather than walk to an arrow.
	/// </summary>
	public int NextPress()
	{
		int[] answer = Solve(Offsets());
		int best = -1;
		float bestD = float.MaxValue;
		for (int j = 0; j < StoneCount; j++)
		{
			if (answer == null || answer[j] == 0)
				continue;
			float d = Player != null ? Player.GlobalPosition.DistanceSquaredTo(Plate(j)) : j;
			if (d < bestD)
			{
				bestD = d;
				best = j;
			}
		}
		return best;
	}

	/// <summary>Stone <paramref name="i"/>'s plate, in the world. For the probe.</summary>
	public Vector2 PlateAt(int i) => Plate(i);

	public bool IsFacing(int i) => facing[i] == Inward(i);

	// The arrow that faces the seal points back at the middle: the north stone's points south.
	private static int Inward(int i) => (i + 2) % StoneCount;

	private Vector2 Stone(int i) => centre + Quarters[i] * StoneDistance;

	private Vector2 Plate(int i) => centre + Quarters[i] * PlateDistance;

	// --- the puzzle, as pure functions (RegressionChecks.ValidateSealPuzzle proves them) -----------------
	// A state is each stone's OFFSET: quarters clockwise past facing the seal, 0 to 3. Solved is all zero.

	/// <summary>The stones a press of <paramref name="plate"/> turns: its own, and the two beside it on the ring.</summary>
	public static int[] TurnedBy(int plate) =>
		new[] { plate, (plate + 1) % StoneCount, (plate + StoneCount - 1) % StoneCount };

	/// <summary>The state after one press of <paramref name="plate"/>.</summary>
	public static int[] Press(int[] offsets, int plate)
	{
		var next = (int[])offsets.Clone();
		foreach (int i in TurnedBy(plate))
			next[i] = (next[i] + 1) % StoneCount;
		return next;
	}

	/// <summary>
	/// How many times each plate must be pressed to face every stone in, 0-3 each; the fewest presses there
	/// are (presses commute, and four of one are none). Null if none - which ValidateSealPuzzle proves never.
	/// </summary>
	public static int[] Solve(int[] offsets)
	{
		for (int code = 0; code < 256; code++)
		{
			var n = new int[StoneCount];
			for (int j = 0; j < StoneCount; j++)
				n[j] = (code >> (2 * j)) & 3;
			bool solves = true;
			for (int i = 0; i < StoneCount && solves; i++)
				solves = (offsets[i] + n[i] + n[(i + 1) % StoneCount] + n[(i + StoneCount - 1) % StoneCount]) % StoneCount == 0;
			if (solves)
				return n;
		}
		return null;
	}

	public static int Sum(int[] n)
	{
		if (n == null)
			return -1;
		int total = 0;
		foreach (int k in n)
			total += k;
		return total;
	}

	/// <summary>
	/// The seed's start, made from its answer: four rolls say how many times each plate must be pressed (0-3),
	/// raised one at a time on the plate that needs fewest until there are at least <see cref="MinPresses"/>;
	/// then, from the solved seal, each plate is pressed the rest of the way round to four - legal presses
	/// only - which leaves the stones exactly that answer away.
	/// </summary>
	public static int[] StartFromRolls(float[] rolls)
	{
		var answer = new int[StoneCount];
		for (int j = 0; j < StoneCount; j++)
			answer[j] = Mathf.Clamp((int)(rolls[j] * 4f), 0, 3);
		while (Sum(answer) < MinPresses)
		{
			int low = 0;
			for (int j = 1; j < StoneCount; j++)
				if (answer[j] < answer[low]) low = j;
			answer[low]++;
		}
		var offsets = new int[StoneCount];
		for (int j = 0; j < StoneCount; j++)
			for (int k = 0; k < (StoneCount - answer[j]) % StoneCount; k++)
				offsets = Press(offsets, j);
		return offsets;
	}

	private int[] Offsets()
	{
		var o = new int[StoneCount];
		for (int i = 0; i < StoneCount; i++)
			o[i] = (facing[i] - Inward(i) + StoneCount) % StoneCount;
		return o;
	}

	/// <summary>
	/// The seal on open ground over its whole disc, as the thaw's lake is (the courtyard is about as wide),
	/// then the stones' start from the seed (<see cref="StartFromRolls"/>): six to twelve presses away.
	/// On the Ruins' lattice a disc that wide fits only in a court, one block in eight, so the placer sweeps
	/// the whole event band for one (ExplorePlacer.EventSiteInBand) rather than giving up near the roll and
	/// laying the courtyard across the walls.
	/// </summary>
	public override void PlaceSites(ExplorePlacer placer)
	{
		AddSite(placer.EventSiteInBand(SiteFootprint, dense: true));
		centre = MainSite;
		var rolls = new float[StoneCount];
		for (int j = 0; j < StoneCount; j++)
			rolls[j] = placer.NextFloat();
		SetOffsets(StartFromRolls(rolls));
		laidOut = true;
	}

	// --- what stands there --------------------------------------------------------------------------

	private const string ArtDir = "res://assets/bonelight/world/seal/";
	// tools/art/five_pointed_seal.py: the stone's cell is 28 with its contact shadow on row 26 and its
	// middle on column 14; the kneeling wizard's is the wizard cell, contact on row 47, middle 24.
	private const int StoneCell = 28;
	private const int StoneFootRow = 26;
	private const int SeimeiCell = 48;
	private const int SeimeiFootRow = 47;
	// He kneels a little south of the middle, so his head is in the star's heart and not over its top point.
	private static readonly Vector2 SeimeiOffset = new(0f, 26f);

	// The courtyard is the event's SIBLING, as the thaw's lake is: it stays, broken, when the event frees
	// itself. Y-sorted, so the stones and he sort with the bodies at their feet; the floor and the plates
	// keep to the ground by their z.
	private Node2D courtyard;
	private Sprite2D floorSprite;
	private readonly Sprite2D[] stoneSprites = new Sprite2D[StoneCount];
	private readonly Sprite2D[] plateSprites = new Sprite2D[StoneCount];
	private Sprite2D seimei;
	private float seimeiClock;
	// Seconds since he was freed: his own clock, not the win line's, which waits its turn behind a note.
	private float freedFor;

	protected override void OnPlaced()
	{
		if (!laidOut)
		{
			// Placed without the director (nothing in play does that): a fixed start, still a puzzle.
			centre = MainSite;
			SetOffsets(StartFromRolls(new[] { 0.1f, 0.6f, 0.35f, 0.85f }));
			laidOut = true;
		}

		Texture2D floorTex = Load("seal-floor.png");
		Texture2D stoneTex = Load("seal-stone.png");
		Texture2D plateTex = Load("seal-plate.png");
		Texture2D seimeiTex = Load("seal-seimei.png");
		if (floorTex == null || stoneTex == null || plateTex == null || seimeiTex == null)
		{
			GD.PushError("FivePointedSealEvent: the seal's art is missing or not imported (python tools/art/five_pointed_seal.py, then import).");
			return;
		}

		courtyard = new Node2D { Name = "FivePointedSeal", YSortEnabled = true };

		// The courtyard: on the ground, over the painted terrain and under everything that stands.
		floorSprite = new Sprite2D
		{
			Name = "SealFloor",
			Texture = floorTex,
			Hframes = 2,
			Centered = true,
			Scale = new Vector2(2f, 2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Position = centre.Round(),
			ZIndex = DrawLayers.Ground - 2,
			ZAsRelative = false,
		};
		courtyard.AddChild(floorSprite);

		for (int i = 0; i < StoneCount; i++)
		{
			plateSprites[i] = new Sprite2D
			{
				Name = $"SealPlate{i}",
				Texture = plateTex,
				Hframes = 2,
				Centered = true,
				Scale = new Vector2(2f, 2f),
				TextureFilter = TextureFilterEnum.Nearest,
				Position = Plate(i).Round(),
				ZIndex = DrawLayers.Ground - 1,
				ZAsRelative = false,
			};
			courtyard.AddChild(plateSprites[i]);
			stoneSprites[i] = Grounded($"SealStone{i}", stoneTex, 8, Stone(i), StoneCell / 2, StoneFootRow);
			courtyard.AddChild(stoneSprites[i]);
		}
		seimei = Grounded("SealSeimei", seimeiTex, 4, centre + SeimeiOffset, SeimeiCell / 2, SeimeiFootRow);
		courtyard.AddChild(seimei);
		RefreshSprites();
		// The courtyard sits in the run's world, at its origin, so its children stand at world positions.
		Game?.CallDeferred(Node.MethodName.AddChild, courtyard);
	}

	// Standing in the world and sorted with the actors at its foot (WorldSprites.SortLift); drawn at x2,
	// so the offset is in sheet pixels.
	private static Sprite2D Grounded(string name, Texture2D texture, int frames, Vector2 foot, int anchorX, int footRow)
	{
		Vector2 ground = foot;
		return new Sprite2D
		{
			Name = name,
			Texture = texture,
			Hframes = frames,
			Centered = false,
			Scale = new Vector2(2f, 2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Position = new Vector2(Mathf.Round(ground.X), Mathf.Round(ground.Y - WorldSprites.SortLift)),
			Offset = new Vector2(-anchorX, WorldSprites.SortLift / 2f - footRow),
		};
	}

	private void SetOffsets(int[] offsets)
	{
		for (int i = 0; i < StoneCount; i++)
			facing[i] = (Inward(i) + offsets[i]) % StoneCount;
	}

	private static Texture2D Load(string file)
	{
		string path = ArtDir + file;
		return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
	}

	private void RefreshSprites()
	{
		for (int i = 0; i < StoneCount; i++)
		{
			if (stoneSprites[i] != null)
				// Broken, every arrow faces in and is lit (a win always gets there; a photograph's shortcut does not).
				stoneSprites[i].Frame = Succeeded ? Inward(i) + StoneCount : facing[i] + (IsFacing(i) ? StoneCount : 0);
			if (plateSprites[i] != null)
				plateSprites[i].Frame = !armed[i] ? 1 : 0;
		}
		if (floorSprite != null)
			floorSprite.Frame = Succeeded ? 1 : 0;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		float dt = (float)delta;
		// Every frame, not only from Tick: a win that does not come through Tick (_UiShot --win-event) still
		// breaks the floor and lights the stones.
		RefreshSprites();
		if (seimei != null && IsInstanceValid(seimei))
		{
			// He breathes, slowly; once he is free he rises into the column of light and is gone.
			seimeiClock += dt;
			seimei.Frame = (int)(seimeiClock * 2.5f) % 4;
			if (Succeeded)
			{
				freedFor += dt;
				float a = Mathf.Clamp(1f - freedFor / 1.2f, 0f, 1f);
				seimei.Modulate = new Color(1f, 1f, 1f, a);
				if (a <= 0f)
				{
					seimei.QueueFree();
					seimei = null;
				}
			}
		}
	}

	// --- the rules -----------------------------------------------------------------------------------

	protected override void Begin()
	{
		guardTimer = GuardIntervalSeconds * 0.5f;
		// Whatever he is standing on when he arrives does not count as a step.
		for (int i = 0; i < StoneCount; i++)
			armed[i] = !OnPlate(i, PlateRadius);
		RefreshSprites();
	}

	// On it with his feet or his middle: walking up from the north his boots arrive first, from the south
	// his body does, and either is a step. Measured to both, so the plate is as wide as it is drawn.
	private bool OnPlate(int i, float radius)
	{
		if (Player == null)
			return false;
		Vector2 at = Player.GlobalPosition;
		Vector2 plate = Plate(i);
		return at.DistanceTo(plate) <= radius || (at + new Vector2(0f, FeetBelow)).DistanceTo(plate) <= radius;
	}

	protected override void Tick(float delta)
	{
		for (int i = 0; i < StoneCount; i++)
		{
			if (armed[i])
			{
				if (OnPlate(i, PlateRadius))
				{
					armed[i] = false;
					PressPlate(i);
				}
			}
			else if (!OnPlate(i, RearmRadius))
			{
				armed[i] = true;
			}
		}
		RefreshSprites();

		// The seal's guard comes while he is at work in the courtyard, as the thaw's does - an event left
		// alone should not quietly fill the map.
		if (PlayerWithin(centre, PlateDistance + 80f))
		{
			guardTimer -= delta;
			if (guardTimer <= 0f)
			{
				guardTimer = GuardIntervalSeconds;
				SpawnRing(centre, 4, PlateDistance + 90f);
			}
		}

		if (Aligned == StoneCount)
		{
			SfxPlayer.Impact(Element.Arcane, centre);
			Succeed(centre);
			RefreshSprites();
		}
	}

	// One press: the plate's stone and the two beside it turn a quarter together, on one beat.
	private void PressPlate(int plate)
	{
		pressedAt[plate] = Clock;
		steps++;
		bool anyFacing = false;
		foreach (int i in TurnedBy(plate))
		{
			facing[i] = (facing[i] + 1) % StoneCount;
			turnedAt[i] = Clock;
			anyFacing |= IsFacing(i);
		}
		// Stone grinding on stone; and a cut filling with his light when one comes to face the seal.
		SfxPlayer.Impact(Element.Earth, Stone(plate));
		if (anyFacing)
			SfxPlayer.Cast(Element.Arcane, Stone(plate));
	}

	// --- drawing -------------------------------------------------------------------------------------

	private static readonly Color Cold = new(0.753f, 0.549f, 1.0f);       // arcane mid #C08CFF
	private static readonly Color ColdEdge = new(0.478f, 0.247f, 0.831f);  // arcane edge #7A3FD4

	public override void _Draw()
	{
		if (IsResolved || !IsFound)
			return;
		// Round each plate, the track every event's progress uses: dashed while its stone is turned away,
		// whole once it faces the seal - so the four plates read as the four things to do.
		for (int i = 0; i < StoneCount; i++)
			DrawProgressArc(Plate(i), PlateRadius + 6f, IsFacing(i) ? 1f : 0f, Cold, 2f, 0.4f);
	}

	// The far tell while nobody has come (how the courtyard is found). Then the seal's light: a slow breath
	// over the star, a pool round him, a glow over every arrow that faces it, a flare as a stone turns - and
	// after a press, the light running out along the ring from the pressed stone to the two it moved.
	protected override void DrawLight(CanvasItem light)
	{
		if (IsDormant)
			base.DrawLight(light);
		if (IsResolved || !NearPlayer(centre))
			return;
		Vector2 c = ToLocal(centre);
		float breath = 0.8f + 0.2f * Mathf.Sin(Clock * 1.3f);
		float held = 0.35f + 0.65f * Aligned / (float)StoneCount;
		ExploreArt.DrawGlow(light, c, 190f, new Color(Cold, 0.07f * breath * held));
		ExploreArt.DrawGlow(light, c + SeimeiOffset - new Vector2(0f, 40f), 64f, new Color(Cold, 0.16f * breath));
		for (int i = 0; i < StoneCount; i++)
		{
			Vector2 s = ToLocal(Stone(i)) - new Vector2(0f, 20f);
			float since = Clock - turnedAt[i];
			if (since < 0.8f)
				ExploreArt.DrawGlow(light, s, 70f, new Color(ColdEdge, 0.45f * (1f - since / 0.8f)));
			if (IsFacing(i))
				ExploreArt.DrawGlow(light, s, 40f, new Color(Cold, 0.24f * Flicker(i)));
		}
		for (int plate = 0; plate < StoneCount; plate++)
		{
			float since = Clock - pressedAt[plate];
			if (since >= PressTellSeconds)
				continue;
			// Out from the pressed stone both ways round the ring, a quarter of it each way, in the first
			// PressRunSeconds; then the whole lit run fades. The stone opposite is never reached.
			float reach = Mathf.Clamp(since / PressRunSeconds, 0f, 1f) * Mathf.Pi / 2f;
			float fade = 1f - Mathf.Clamp((since - PressRunSeconds) / (PressTellSeconds - PressRunSeconds), 0f, 1f);
			float from = Mathf.Atan2(Quarters[plate].Y, Quarters[plate].X);
			const float r = StoneDistance - 6f;
			for (int k = 0; k < 2; k++)
			{
				float to = from + (k == 0 ? -reach : reach);
				light.DrawArc(c, r, Mathf.Min(from, to), Mathf.Max(from, to), 24, new Color(ColdEdge, 0.55f * fade), 10f);
				light.DrawArc(c, r, Mathf.Min(from, to), Mathf.Max(from, to), 24, new Color(Cold, 0.85f * fade), 4f);
				// The head of the run: a bright point travelling to the neighbour.
				Vector2 head = c + Vector2.FromAngle(to) * r;
				ExploreArt.DrawGlow(light, head, 26f, new Color(Cold, 0.6f * fade));
			}
		}
	}
}
