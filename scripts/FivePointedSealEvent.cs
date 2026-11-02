using Godot;
using WizardSurvivors.scripts;

// The Mystic Ruins' side event: the Five-Pointed Seal (.ai/chapter-wizards.md 4.5).
//
// "The old wards were broken from the inside." A school of wards was Abe no Seimei's kind of house, and
// the dark wizard turned a ward on its maker: the onmyoji kneels in the middle of his own five-pointed
// seal, a star cut into a courtyard floor and full of the dark wizard's cold light. Four guardian stones
// stand at its quarters, each cut with one arrow. Stepping on a stone's plate turns it a quarter,
// clockwise. When all four face the seal, it breaks, and he is free.
//
// What it asks: SOLVE. Every other event is a place, a target, a carry or a fight; this one is a state
// to put right, and the horde is what makes walking the four plates cost something. It is the one
// rescue that can be done without killing anything, and the one that can be ruined by nothing - a
// stone turned past the seal comes round again, so a wrong step costs three more and never locks
// (the event rules, side-events.md). The stones start a seeded one to three quarters off the seal, so
// the whole thing is four to twelve steps; about twelve, the doc's guess, is the worst a seed deals.
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

	[Export] public float GuardIntervalSeconds { get; set; } = 6f;

	// North, east, south, west: the order of the stones round the seal, of the arrow frames in the sheet,
	// and of a quarter turn clockwise.
	private static readonly Vector2[] Quarters = { Vector2.Up, Vector2.Right, Vector2.Down, Vector2.Left };

	private Vector2 centre;
	private readonly int[] facing = new int[StoneCount];
	private readonly bool[] armed = { true, true, true, true };
	private readonly float[] turnedAt = { -10f, -10f, -10f, -10f };
	private bool laidOut;
	private float guardTimer;
	private int steps;

	protected override string Announcement => "Someone kneels inside a seal cut in the floor.";
	protected override string ChestLine => "The seal is broken - a chest was left inside it";
	protected override string Objective => $"Turn the four stones to face the seal  ({Aligned} of {StoneCount})";
	// The plate of the nearest stone still turned away; the chevron hangs above where he stands on it.
	protected override Vector2? PointerTarget => NextPlate();
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

	/// <summary>Plates stepped on so far this run. For the probe.</summary>
	public int Steps => steps;

	/// <summary>Where the player stands to step on stone <paramref name="i"/>'s plate: his node, not his feet.</summary>
	public Vector2 PlateStand(int i) => Plate(i) - new Vector2(0f, FeetBelow);

	public bool IsFacing(int i) => facing[i] == Inward(i);

	// The arrow that faces the seal points back at the middle: the north stone's points south.
	private static int Inward(int i) => (i + 2) % StoneCount;

	private Vector2 Stone(int i) => centre + Quarters[i] * StoneDistance;

	private Vector2 Plate(int i) => centre + Quarters[i] * PlateDistance;

	/// <summary>
	/// The seal on open ground over its whole disc, as the thaw's lake is (the courtyard is about as wide),
	/// then each stone's starting arrow from the seed: one to three quarters short of the seal, never on it.
	/// </summary>
	public override void PlaceSites(ExplorePlacer placer)
	{
		AddSite(placer.EventSite(SiteFootprint, dense: true));
		centre = MainSite;
		for (int i = 0; i < StoneCount; i++)
		{
			int short_ = 1 + Mathf.Min(2, (int)(placer.NextFloat() * 3f));
			facing[i] = ((Inward(i) - short_) % StoneCount + StoneCount) % StoneCount;
		}
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

	protected override void OnPlaced()
	{
		if (!laidOut)
		{
			// Placed without the director (nothing in play does that): the plain layout, one turn each.
			centre = MainSite;
			for (int i = 0; i < StoneCount; i++)
				facing[i] = (Inward(i) + StoneCount - 1) % StoneCount;
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
				stoneSprites[i].Frame = facing[i] + (IsFacing(i) || Succeeded ? StoneCount : 0);
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
		if (seimei != null && IsInstanceValid(seimei))
		{
			// He breathes, slowly; once he is free he rises into the column of light and is gone.
			seimeiClock += dt;
			seimei.Frame = (int)(seimeiClock * 2.5f) % 4;
			if (Succeeded)
			{
				float a = Mathf.Clamp(1f - SecondsSinceResolved / 1.2f, 0f, 1f);
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

	private bool OnPlate(int i, float radius) =>
		Player != null && (Player.GlobalPosition + new Vector2(0f, FeetBelow)).DistanceTo(Plate(i)) <= radius;

	protected override void Tick(float delta)
	{
		for (int i = 0; i < StoneCount; i++)
		{
			if (armed[i])
			{
				if (OnPlate(i, PlateRadius))
				{
					armed[i] = false;
					Turn(i);
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

	private void Turn(int i)
	{
		facing[i] = (facing[i] + 1) % StoneCount;
		turnedAt[i] = Clock;
		steps++;
		// Stone grinding on stone; and the cut filling with his light when it comes to face the seal.
		SfxPlayer.Impact(Element.Earth, Stone(i));
		if (IsFacing(i))
			SfxPlayer.Cast(Element.Arcane, Stone(i));
	}

	private Vector2? NextPlate()
	{
		if (IsResolved || Player == null)
			return null;
		float best = float.MaxValue;
		Vector2? target = null;
		for (int i = 0; i < StoneCount; i++)
		{
			if (IsFacing(i))
				continue;
			float d = Player.GlobalPosition.DistanceSquaredTo(PlateStand(i));
			if (d < best)
			{
				best = d;
				target = PlateStand(i);
			}
		}
		return target;
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
	// over the star, a pool round him, a glow over every arrow that faces it, and a flare as a stone turns.
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
			if (since < 0.5f)
				ExploreArt.DrawGlow(light, s, 70f, new Color(ColdEdge, 0.4f * (1f - since / 0.5f)));
			if (IsFacing(i))
				ExploreArt.DrawGlow(light, s, 40f, new Color(Cold, 0.24f * Flicker(i)));
		}
	}
}
