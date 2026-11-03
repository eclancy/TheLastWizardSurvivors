using Godot;
using System.Collections.Generic;
using WizardSurvivors.scripts;

// The Emberdeep's side event: the great anvil (.ai/chapter-wizards.md 4.7).
//
// "Every shackle came from here." The Emberdeep is the forge at the end of the road, and the chains that hold
// every other wizard were struck in it - by Wayland the Smith, who is chained to his own anvil with four of
// them, staked out into the floor at the corners, and still working, because he is not allowed to stop.
//
// What it asks: BAIT. No spell breaks his own iron; only his forge's hammers can. When the player comes, the
// bellows wake four Trip-Hammers (the chapter's Slammer, StageBestiary) and they come for him. A slam that
// lands on a stake breaks its chain - measured with GroundSlamAttack.Contains, the one authority on the
// shape, so the ring the player was shown is the ring that broke it. Stand by a stake as one winds up, step
// out of the tell, and let it come down on the iron. Four chains, four slams. The only ask in the game that
// uses the enemy's own attack, and the player is never asked to take a hit: the hammers here only begin
// their wind-up once he is inside the ring (SlamEngageRadiusMultiplier under 1), so the tell always lands
// round him and the stake both, and the wind-up is his to walk out of.
//
// NOTHING LOCKS. A hammer killed before the chains are all broken is replaced: the bellows breathe another
// awake every ReplaceSeconds while the player is at the forge, four on the floor at most - so a strong
// loadout that melts them costs time, never the wizard. Walk away and no more come (an event left alone
// should not quietly fill the map). A broken chain stays broken.
//
// THE STAKES ARE NOT BODIES. Nothing placed here joins "enemies"; the stakes, the anvil and he are sprites,
// and the chains are drawn on the floor. The hammers are the only bodies, and they are the chapter's own.
//
// Art: tools/art/great_anvil.py (world/anvil/). The forge floor is a ground decal; the anvil, he and the
// stakes sort with the bodies at their feet; the chains lie on the floor under them. The forge's light is
// code, on the additive layer every event has - and it is fire, because the forge is built and built things
// burn (world-and-tone.md, the one carve-out).
public partial class GreatAnvilEvent : SideEvent
{
	public const int StakeCount = 4;
	public const int HammerCount = 4;

	/// <summary>A stake's foot from the anvil, world px, on the diagonals (the floor is drawn at x2).</summary>
	public const float StakeDistance = 140f;

	/// <summary>How much of a stake a slam must reach: the iron's half-width at the floor, world px.</summary>
	public const float StakeRadius = 12f;

	/// <summary>
	/// The event hammers' ring, world px: a little wider than a Trip-Hammer's 96, so a stake beside the player
	/// is under it with room to spare.
	/// </summary>
	public const float HammerSlamRadius = 104f;

	/// <summary>
	/// They begin the wind-up only once the player is this share of the ring in (ordinary slammers start at
	/// 1.5, while the player is still coming): the tell then always lands round him, and round the stake he is
	/// standing by - and he has the whole wind-up to step out of it.
	/// </summary>
	public const float HammerEngage = 0.85f;

	/// <summary>Where the bellows wake them, from the anvil: just beyond the forge floor.</summary>
	public const float WakeDistance = 460f;

	[Export] public float HammerHealthMultiplier { get; set; } = 12f;
	[Export] public float ReplaceSeconds { get; set; } = 3.5f;

	// The diagonals, north-east first and clockwise: the corners the chains are staked to.
	private static readonly Vector2[] Corners =
	{
		new Vector2(1f, -1f).Normalized(), new Vector2(1f, 1f).Normalized(),
		new Vector2(-1f, 1f).Normalized(), new Vector2(-1f, -1f).Normalized(),
	};

	private Vector2 centre;
	private readonly bool[] broken = new bool[StakeCount];
	private readonly float[] brokenAt = { -10f, -10f, -10f, -10f };
	private readonly List<SlammerEnemy> hammers = new();
	private float replaceTimer;
	private int woken;

	protected override string Announcement => "Someone is chained to the anvil. The bellows wake the hammers.";
	protected override string ChestLine => "The chains are broken - a chest was left at the anvil";
	protected override string Objective => $"Bring the hammers down on the chains  ({ChainsBroken} of {StakeCount})";
	// To the forge while he is away from it; nothing once he is on its floor - the stakes are in plain sight,
	// and which one to bait is his to choose.
	protected override Vector2? PointerTarget =>
		IsResolved || Player == null || PlayerWithin(centre, StakeDistance + 200f) ? null : centre;
	protected override string SiteGlyph => "anvil";
	// The floor and the stakes on it, and a little more: placement wants all of it open (no lava under the
	// anvil), and the furnishing keeps off it.
	protected override float SiteFootprint => StakeDistance + 90f;
	protected override float ArrivalRadius => 440f;
	// Fire's hot stop: the forge's own light, which the hint's sparks wear too (tools/art/hint_cues.py).
	protected override Color TellColor => new Color(1.0f, 0.784f, 0.282f);   // fire hot #FFC848

	/// <summary>Chains broken so far (0 to 4). Public for the probe.</summary>
	public int ChainsBroken
	{
		get
		{
			int n = 0;
			foreach (bool b in broken)
				if (b) n++;
			return n;
		}
	}

	/// <summary>Slams that landed with the player inside them. For the probe: he is never asked to take one.</summary>
	public int SlamsOnPlayer { get; private set; }

	/// <summary>Hammers the bellows have woken, replacements included. For the probe.</summary>
	public int HammersWoken => woken;

	public bool IsBroken(int i) => broken[i];

	/// <summary>Stake <paramref name="i"/>'s foot, in the world. For the probe.</summary>
	public Vector2 StakeAt(int i) => Stake(i);

	/// <summary>The hammers on the floor now. For the probe.</summary>
	public IReadOnlyList<SlammerEnemy> Hammers => hammers;

	private Vector2 Stake(int i) => centre + Corners[i] * StakeDistance;

	/// <summary>
	/// The forge on open ground over its whole disc, as the seal's courtyard is: the event band swept for one
	/// (ExplorePlacer.EventSiteInBand), so the anvil never stands in lava or across a wall.
	/// </summary>
	public override void PlaceSites(ExplorePlacer placer)
	{
		AddSite(placer.EventSiteInBand(SiteFootprint, dense: true));
		centre = MainSite;
	}

	// --- what stands there --------------------------------------------------------------------------

	private const string ArtDir = "res://assets/bonelight/world/anvil/";
	// tools/art/great_anvil.py: the anvil's cell is 56x36, its block on row 33, middle column 28, its two chain
	// rings at (9, 28) and (46, 28); his cell is the wizard's, drawn from the waist up with the bottom of him
	// on row 40; a stake's is 16x28, foot on row 25, middle 8, its ring at (11, 13); a link's is 8.
	private const int AnvilFootRow = 33;
	private const int AnvilMiddle = 28;
	private const int SmithFootRow = 40;
	private const int SmithMiddle = 24;
	private const int StakeFootRow = 25;
	private const int StakeMiddle = 8;
	private const int LinkCell = 8;
	// The anvil a little south of the middle; he behind it and to its left, so the hammer comes down on its
	// face (great_anvil.py's preview lays them out the same way).
	private static readonly Vector2 AnvilFoot = new(0f, 30f);
	private static readonly Vector2 SmithFoot = new(-16f, -10f);
	private static readonly Vector2 LeftRing = new((9 - AnvilMiddle) * 2f, 30f - (AnvilFootRow - 28) * 2f);
	private static readonly Vector2 RightRing = new((46 - AnvilMiddle) * 2f, 30f - (AnvilFootRow - 28) * 2f);
	// Where a chain meets its stake: the ring through the head, as the floor sees it.
	private static readonly Vector2 StakeRing = new((11 - StakeMiddle) * 2f, -6f);

	// His hammer's beat: raised, coming down, struck, resting - seconds into each 1.9 s blow.
	private const float BlowSeconds = 1.9f;
	private static readonly float[] BlowFrames = { 0f, 0.7f, 0.85f, 1.3f };
	private const float StrikeAt = 0.85f;

	private Node2D forge;
	private Sprite2D floorSprite;
	private Sprite2D smith;
	private readonly Sprite2D[] stakeSprites = new Sprite2D[StakeCount];
	private Texture2D linkTex;
	private float smithClock;
	private float freedFor;

	protected override void OnPlaced()
	{
		centre = MainSite;
		Texture2D floorTex = Load("anvil-floor.png");
		Texture2D anvilTex = Load("anvil-anvil.png");
		Texture2D smithTex = Load("anvil-smith.png");
		Texture2D stakeTex = Load("anvil-stake.png");
		linkTex = Load("anvil-link.png");
		if (floorTex == null || anvilTex == null || smithTex == null || stakeTex == null || linkTex == null)
		{
			GD.PushError("GreatAnvilEvent: the forge's art is missing or not imported (python tools/art/great_anvil.py, then import).");
			return;
		}

		forge = new Node2D { Name = "GreatAnvil", YSortEnabled = true };
		floorSprite = new Sprite2D
		{
			Name = "ForgeFloor",
			Texture = floorTex,
			Centered = true,
			Scale = new Vector2(2f, 2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Position = centre.Round(),
			ZIndex = DrawLayers.Ground - 2,
			ZAsRelative = false,
		};
		forge.AddChild(floorSprite);
		for (int i = 0; i < StakeCount; i++)
		{
			stakeSprites[i] = Grounded($"Stake{i}", stakeTex, 2, Stake(i), StakeMiddle, StakeFootRow);
			forge.AddChild(stakeSprites[i]);
		}
		forge.AddChild(Grounded("Anvil", anvilTex, 1, centre + AnvilFoot, AnvilMiddle, AnvilFootRow));
		smith = Grounded("Smith", smithTex, 4, centre + SmithFoot, SmithMiddle, SmithFootRow);
		forge.AddChild(smith);
		// The forge sits in the run's world, at its origin, so its children stand at world positions. It is the
		// event's sibling: it stays, the anvil empty and the chains burst, after the win.
		Game?.CallDeferred(Node.MethodName.AddChild, forge);
	}

	// Standing in the world and sorted with the actors at its foot (WorldSprites.SortLift); drawn at x2, so the
	// offset is in sheet pixels. FivePointedSealEvent's helper, for the same reason.
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

	private static Texture2D Load(string file)
	{
		string path = ArtDir + file;
		return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		float dt = (float)delta;
		for (int i = 0; i < StakeCount; i++)
		{
			if (stakeSprites[i] != null && IsInstanceValid(stakeSprites[i]))
				// Freed, every ring is burst (a win always gets there; a photograph's shortcut does not).
				stakeSprites[i].Frame = broken[i] || Succeeded ? 1 : 0;
		}
		if (smith == null || !IsInstanceValid(smith))
			return;
		// He works on, slowly, whether anyone has come or not - until he is free, when he lowers the hammer,
		// and is gone into the column of light.
		smithClock += dt;
		float beat = smithClock % BlowSeconds;
		int frame = 0;
		for (int f = 0; f < BlowFrames.Length; f++)
			if (beat >= BlowFrames[f]) frame = f;
		if (Succeeded)
		{
			freedFor += dt;
			frame = 3;
			float a = Mathf.Clamp(1f - freedFor / 1.2f, 0f, 1f);
			smith.Modulate = new Color(1f, 1f, 1f, a);
			if (a <= 0f)
			{
				smith.QueueFree();
				smith = null;
				return;
			}
		}
		smith.Frame = frame;
	}

	// --- the rules -----------------------------------------------------------------------------------

	protected override void Begin()
	{
		// The bellows breathe and the four wake at the forge's edge, one on each side.
		for (int k = 0; k < HammerCount; k++)
			Wake(centre + Vector2.FromAngle(Mathf.Pi * 0.5f * k) * WakeDistance);
		replaceTimer = ReplaceSeconds;
	}

	private void Wake(Vector2 at)
	{
		if (Game == null)
			return;
		Enemy body = Game.SpawnEventEnemy(BestiaryRole.Slammer, Game.ReachablePoint(at), HammerHealthMultiplier, true);
		if (body is not SlammerEnemy hammer)
		{
			// Only reachable if the chapter's Slammer stopped being one: then nothing here can break a chain.
			GD.PushError($"GreatAnvilEvent: the Emberdeep's Slammer is {body?.GetType().Name ?? "missing"}, not a SlammerEnemy; its slams cannot break the chains.");
			return;
		}
		hammer.SlamRadius = HammerSlamRadius;
		hammer.SlamEngageRadiusMultiplier = HammerEngage;
		hammer.RebuildSlam();
		hammer.SlamLanded += OnSlamLanded;
		hammers.Add(hammer);
		woken++;
	}

	protected override void Tick(float delta)
	{
		// The fallen are let go; while chains remain and he is at the forge, the bellows wake their next.
		for (int i = hammers.Count - 1; i >= 0; i--)
		{
			if (IsGone(hammers[i]))
			{
				if (IsInstanceValid(hammers[i]))
					hammers[i].SlamLanded -= OnSlamLanded;
				hammers.RemoveAt(i);
			}
		}
		if (hammers.Count < HammerCount && PlayerWithin(centre, WakeDistance + 200f))
		{
			replaceTimer -= delta;
			if (replaceTimer <= 0f)
			{
				replaceTimer = ReplaceSeconds;
				// On the far side of the forge from him, so it walks in across the floor where he can see it come.
				Vector2 away = Player != null ? (centre - Player.GlobalPosition) : Vector2.Up;
				Wake(centre + (away.LengthSquared() > 1f ? away.Normalized() : Vector2.Up) * WakeDistance);
			}
		}
		else
		{
			replaceTimer = Mathf.Max(replaceTimer, 0.5f);
		}

		if (ChainsBroken == StakeCount)
		{
			SfxPlayer.Impact(Element.Metal, centre);
			Release();
			Succeed(centre);
		}
	}

	// A hammer came down. If its ring reached a stake still holding - the nearest of the iron to it, so a slam
	// that only clips a stake's edge counts as it would look like it should - that chain breaks.
	private void OnSlamLanded(SlammerEnemy hammer)
	{
		if (IsResolved || !IsInstanceValid(hammer))
			return;
		if (Player != null && hammer.SlamCovers(Player.GlobalPosition))
			SlamsOnPlayer++;
		for (int i = 0; i < StakeCount; i++)
		{
			if (broken[i])
				continue;
			Vector2 stake = Stake(i);
			Vector2 toward = hammer.GlobalPosition - stake;
			Vector2 nearest = toward.LengthSquared() > StakeRadius * StakeRadius ? stake + toward.Normalized() * StakeRadius : stake;
			if (!hammer.SlamCovers(nearest))
				continue;
			broken[i] = true;
			brokenAt[i] = Clock;
			// Iron on iron, and the chain going.
			SfxPlayer.Impact(Element.Metal, stake);
			SfxPlayer.Impact(Element.Earth, stake);
		}
	}

	// Done or stood down: the hammers are left to the run, as ordinary elites, and stop being listened to.
	private void Release()
	{
		foreach (SlammerEnemy hammer in hammers)
		{
			if (IsInstanceValid(hammer))
				hammer.SlamLanded -= OnSlamLanded;
		}
		hammers.Clear();
	}

	protected override void OnStoodDown() => Release();

	public override void _ExitTree()
	{
		Release();
		base._ExitTree();
	}

	// --- drawing -------------------------------------------------------------------------------------

	private static readonly Color FireHot = new(1.0f, 0.784f, 0.282f);    // fire hot #FFC848
	private static readonly Color FireMid = new(0.910f, 0.478f, 0.102f);  // fire mid #E87A1A
	private static readonly Color FireEdge = new(0.722f, 0.220f, 0.102f); // fire edge #B8381A
	private static readonly Color MetalCore = new(1f, 1f, 1f);           // metal core #FFFFFF
	private static readonly Color MetalMid = new(0.769f, 0.831f, 0.910f); // metal mid #C4D4E8

	// The chains, lying on the floor from the anvil's rings out to the stakes, link by link - and round each
	// stake the track every event's progress uses: dashed while its chain holds, whole once it is broken, so the
	// four stakes read as the four things to do. A broken chain is two lengths with the floor between them.
	public override void _Draw()
	{
		if (linkTex == null || !NearPlayer(centre))
			return;
		for (int i = 0; i < StakeCount; i++)
		{
			Vector2 from = centre + (Corners[i].X < 0f ? LeftRing : RightRing);
			Vector2 to = Stake(i) + StakeRing;
			bool burst = broken[i] || Succeeded;
			DrawChain(from, to, burst ? 0.42f : 1f, 0f);
			if (burst)
				DrawChain(from, to, 1f, 0.78f);
		}
		if (IsResolved || !IsFound)
			return;
		for (int i = 0; i < StakeCount; i++)
			DrawProgressArc(Stake(i) - new Vector2(0f, 6f), 30f, broken[i] ? 1f : 0f, FireHot, 2f, 0.4f);
	}

	// Links from t0 to t1 of the way along, face on and edge on in turn, every 9 px, on the art grid.
	private void DrawChain(Vector2 from, Vector2 to, float t1, float t0)
	{
		float length = from.DistanceTo(to);
		int links = Mathf.Max(2, Mathf.RoundToInt(length / 9f));
		var size = new Vector2(LinkCell * 2f, LinkCell * 2f);
		for (int k = Mathf.RoundToInt(t0 * links); k <= Mathf.RoundToInt(t1 * links); k++)
		{
			Vector2 at = ToLocal(from.Lerp(to, k / (float)links));
			at = new Vector2(Mathf.Round(at.X / 2f) * 2f, Mathf.Round(at.Y / 2f) * 2f);
			DrawTextureRectRegion(linkTex, new Rect2(at - size / 2f, size), new Rect2((k % 2) * LinkCell, 0, LinkCell, LinkCell));
		}
	}

	// The far tell while nobody has come. Then the forge's light: the hearth glowing under the anvil, a burst
	// of sparks off every blow he strikes, a coal-glow on each stake still holding - and a stake whose chain
	// has just gone flares white, iron on iron.
	protected override void DrawLight(CanvasItem light)
	{
		if (IsDormant)
			base.DrawLight(light);
		if (IsResolved || !NearPlayer(centre))
			return;
		Vector2 c = ToLocal(centre);
		ExploreArt.DrawGlow(light, c + AnvilFoot, 150f, new Color(FireMid, 0.10f * Flicker(7)));
		ExploreArt.DrawGlow(light, c + AnvilFoot - new Vector2(0f, 30f), 60f, new Color(FireHot, 0.14f * Flicker(3)));
		float beat = smithClock % BlowSeconds;
		float since = beat - StrikeAt;
		if (!Succeeded && since >= 0f && since < 0.35f)
		{
			// Where the hammer comes down on the face (great_anvil.py: the head at his column 37, row 33).
			Vector2 face = c + SmithFoot + new Vector2((37 - SmithMiddle) * 2f, -(SmithFootRow - 33) * 2f);
			float f = 1f - since / 0.35f;
			ExploreArt.DrawGlow(light, face, 40f, new Color(FireHot, 0.5f * f));
			for (int k = 0; k < 5; k++)
			{
				float a = -Mathf.Pi * (0.15f + 0.7f * k / 4f);
				Vector2 spark = face + Vector2.FromAngle(a) * (10f + 46f * (1f - f)) + new Vector2(0f, 30f * (1f - f) * (1f - f));
				ExploreArt.DrawGlow(light, spark, 7f, new Color(FireHot, 0.8f * f));
			}
		}
		for (int i = 0; i < StakeCount; i++)
		{
			Vector2 s = ToLocal(Stake(i)) - new Vector2(0f, 20f);
			float gone = Clock - brokenAt[i];
			if (gone < 0.9f)
				ExploreArt.DrawGlow(light, s, 90f, new Color(MetalCore, 0.55f * (1f - gone / 0.9f)));
			if (!broken[i])
				ExploreArt.DrawGlow(light, s, 34f, new Color(FireEdge, 0.18f * Flicker(i)));
			else if (gone < 2.5f)
				ExploreArt.DrawGlow(light, s, 30f, new Color(MetalMid, 0.2f * (1f - gone / 2.5f)));
		}
	}
}
