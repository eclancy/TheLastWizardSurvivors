using Godot;
using System;
using TheOneHeMissed.scripts;

// The Blighted Swamp's second side event: the gibbet's cage, where Paracelsus is kept (.ai/chapter-wizards.md 4.4).
//
// "Mother Rot heals herself; he is the one who could cure the rot. So the cure hangs in a cage over her mire,
// breathing it." The physician who wrote that the dose makes the poison hangs in an iron cage from a gibbet over a pool
// of her rot, coughing.
//
// What it asks: LEAD - choose his path (.ai/unlock-puzzles.md 3.4). Hold the winch WinchSeconds and the cage comes down;
// he steps out and follows the player - at FollowShare of his walk, along the player's own steps (LedEscort) - to the
// ferry landing, LandingMin to LandingMax px off, which the objective arrow shows. Between the cage and the landing the
// bog is cut by her rot (RotPoolLayout): three walls of pools, each with one gap, that hurt HIM and not the player. The
// player can wade them; he cannot, and he walks exactly where the player walked, so the route the player walks is the
// route he takes and the puzzle is finding the way through. The player's steps glow on the ground for the stretch he
// has not walked yet - his path, seen ahead - and turn the rot's sickly colour where they run through a pool; a pool
// stirs and bubbles while he is near it. Every RiseSeconds the Mired come up in the next gap ahead of them both, so the
// safe line keeps changing. Nothing goes for him and nothing bites him: 3.4 took out the 250 px hunt and the bites,
// because the puzzle is the route, not guarding him.
//
// A WRONG MOVE COSTS TIME. Walk into a pool and back out the way you came and he never goes in: the steps you took
// back close a loop he leaves out (LedEscort). Lead him through one and he coughs, his bar shows and the rot takes
// RotDamage off it at once and every RotTickSeconds he stays in. At the landing the Ferryman's empty punt takes him.
//
// NOTHING IS LOST BUT THE WALK. If the rot takes all of him, the bog takes him back: he sinks where he stands, and he is
// in the cage again, hoisted, with the winch to hold again and the walk to start again from the cage. The bar is full
// again with him, and the pools are where they were. Nothing locks.
//
// NOTHING HERE IS A BODY. He is a sprite sorted among the bodies by his feet and walked by LedEscort; he is never in
// "enemies", has no collision layer and is never a target. The gibbet, the cage, the pools, the landing and the punt
// are drawings: none of them blocks anything, as the seal's stones and the nine mouths' cliff do not.
//
// Art: tools/art/gibbet.py (world/gibbet/). The one light on any of it is his mark on the cage's lock, in the arcane
// ramp's violet, and the far tell stands up from there; Paracelsus's own sprite (roster.paracelsus) is drawn into the
// cage and the punt, and walks between them as his own sheets. The pools are rot-pools.png; the steps draw themselves.
public partial class GibbetCageEvent : SideEvent
{
	/// <summary>Seconds of holding the winch before the cage comes down (4.4: 2).</summary>
	public const float WinchSeconds = 2f;

	/// <summary>Within this of the winch's foot he is holding it.</summary>
	public const float WinchRadius = 60f;

	/// <summary>How the winch's hold is lost when he lets go: all of it in two seconds.</summary>
	public const float WinchDrainPerSecond = 0.5f;

	/// <summary>How far from the cage the ferry landing is (4.4: about 1,600 px).</summary>
	public const float LandingMin = 1450f;
	public const float LandingMax = 1750f;

	/// <summary>His walk as a share of the player's (4.4: 75%).</summary>
	public const float FollowShare = 0.75f;

	/// <summary>
	/// His bar. A fixed pool, as it was when the swarm bit him (4.4's "What moved" 1): the rot's bite does not grow over a
	/// run, so neither does what he can take of it.
	/// </summary>
	public const int HealthMax = 60;

	/// <summary>
	/// What the rot takes from him: RotDamage the moment his feet are in a pool's bite, and again every RotTickSeconds
	/// they stay (3.4 left "a pool's bite" open). A brush with an edge is one or two of these; walked straight across
	/// the middle of a mid-sized pool he is in it for about a second, four or five - a third of him. Twelve in all, so
	/// two or three full crossings take him, and every step the player takes back from a pool before he reaches it is
	/// free.
	/// </summary>
	public const int RotDamage = 5;
	public const float RotTickSeconds = 0.25f;

	/// <summary>A pool stirs - the skin goes sickly and bubbles - while his feet are within this of its edge.</summary>
	public const float StirReach = 150f;

	/// <summary>He coughs on stepping into the rot, and again this often while he stays in it.</summary>
	public const float CoughSeconds = 1.1f;

	/// <summary>The bog closes the way: while he walks, this many of the chapter's Mired rise in the next gap this often.</summary>
	public const float RiseSeconds = 5f;
	public const int RiseCount = 3;
	// Past the last wall, there is no gap ahead: they rise across his way, this far ahead of him, as they always did.
	public const float RiseMin = 220f;
	public const float RiseMax = 300f;

	/// <summary>
	/// A riser's health, as a share of the run's ordinary spawn health: the bog's own, sturdier than the trickle. At 1 the
	/// first probe runs' loadout burned every one down before it came near; at 3 they stand in the gap long enough to
	/// make the player choose: through them, or round the wall.
	/// </summary>
	public const float RiseHealth = 3f;

	/// <summary>Within this of the landing's foot he is at the ferry, and the punt takes him.</summary>
	public const float BoardRadius = 90f;

	protected override string Announcement => "A cage hangs from a gibbet over the rot. Someone in it is coughing.";
	protected override string ChestLine => "The punt has taken him - a chest was left on the landing";
	protected override string Objective => IsResolved ? string.Empty : phase switch
	{
		Phase.Hanging when takenBackFor >= 0f && takenBackFor < TakenBackLineSeconds => "The bog took him back to the cage",
		Phase.Hanging => PlayerWithin(WinchPoint, WinchRadius)
			? $"Hold the winch  ({Mathf.RoundToInt(winch * 100f)}%)"
			: "Lower the cage at the winch",
		Phase.Lowering or Phase.Opening => "The cage comes down",
		// The first time the rot takes him the line says why; the steps on the ground have been saying how.
		Phase.Walking => inPool >= 0
			? $"The rot is choking him - lead him round it  ({Mathf.RoundToInt(WalkProgress * 100f)}%)"
			: health < HealthMax * 0.4f
				? $"He cannot take much more of the rot  ({Mathf.RoundToInt(WalkProgress * 100f)}%)"
				: $"Lead him to the ferry landing  ({Mathf.RoundToInt(WalkProgress * 100f)}%)",
		Phase.Sinking => "The bog is taking him back",
		_ => "The punt takes him",
	};
	// The winch while the cage hangs (and while it comes down), the landing while he walks and boards - the arrow going
	// null is what says an event is over, so it stays on the landing until the win.
	protected override Vector2? PointerTarget => IsResolved ? null : phase switch
	{
		Phase.Hanging or Phase.Lowering or Phase.Opening or Phase.Sinking => WinchPoint,
		_ => landing + new Vector2(0f, -40f),
	};

	protected override string SiteGlyph => "gibbet";
	// The gibbet, its pool and the winch, and a margin.
	protected override float SiteFootprint => 180f;
	protected override float ArrivalRadius => 380f;
	// His mark on the cage's lock: the arcane ramp's mid #C08CFF, the cold point every taken thing carries.
	protected override Color TellColor => new(0.753f, 0.549f, 1.0f);

	// --- state ---------------------------------------------------------------------------------------------

	private enum Phase { Hanging, Lowering, Opening, Walking, Sinking, Boarding }

	private Phase phase = Phase.Hanging;
	private float phaseFor;
	private Vector2 landing;
	private RotPoolLayout layout;
	private float walkFrom = 1f;
	// The winch's hold, 0 to 1. Named "progress" because _UiShot --win-event fills a float field of that name and lets
	// the real path run - here that is the winch, so the cage comes down and he steps out.
	private float progress;
	private float winch { get => progress; set => progress = value; }
	private int health = HealthMax;
	// The pool his feet are in (RotPoolLayout.PoolUnder), -1 for none; the clock to its next bite; the cough's.
	private int inPool = -1;
	private float rotClock;
	private float coughClock;
	private float riseClock;
	private float hurtFlash;
	private float takenBackFor = -1f;
	private const float TakenBackLineSeconds = 3.5f;
	private bool succeeded;

	private readonly LedEscort him;

	public GibbetCageEvent()
	{
		// He leaves a loop the player closed out only if the step across it keeps out of the rot and off the water.
		him = new LedEscort { SpeedShare = FollowShare, FollowGap = 64f, MayCut = MayStepAcross };
	}

	// Tallies, for the probe.
	/// <summary>How often the cage was lowered and he stepped out.</summary>
	public int Releases { get; private set; }
	/// <summary>How often the bog took him back.</summary>
	public int Falls { get; private set; }
	/// <summary>Times he stepped into a pool, the rot's bites on him and what they took, over the whole event.</summary>
	public int PoolEntries { get; private set; }
	public int RotTicks { get; private set; }
	public int RotTaken { get; private set; }
	/// <summary>The rot's bites on him on the walk now under way (or the last one), from the cage door.</summary>
	public int RotTicksThisWalk { get; private set; }
	/// <summary>The Mired the bog raised in his way.</summary>
	public int Risen { get; private set; }
	/// <summary>How many of those rose in a gap in the rot rather than across his way.</summary>
	public int RisenInGaps { get; private set; }
	/// <summary>The furthest he got toward the landing on any walk, 0 to 1.</summary>
	public float BestProgress { get; private set; }
	/// <summary>How far he walked, over every walk.</summary>
	public float Walked { get; private set; }
	/// <summary>Loops the player walked and closed that he left out, on this walk.</summary>
	public int LoopsLeftOut => him.LoopsLeftOut;

	/// <summary>Where the winch is held from (its foot).</summary>
	public Vector2 WinchPoint => MainSite + new Vector2(-92f, 0f);
	/// <summary>The ferry landing: the foot of its jetty, on the bank.</summary>
	public Vector2 Landing => landing;
	/// <summary>Where he steps out of the cage onto the bank, and the walk is measured from.</summary>
	public Vector2 Door => MainSite + new Vector2(16f, 44f);
	/// <summary>
	/// Where the way to the ferry starts, for the rot: the winch, where the player stands to lower the cage and walks on
	/// from. The walls are laid across the line from here, because that is the straight walk a player makes - laid
	/// across the line from the door instead, a probe walking straight from the winch slipped through two walls' gaps
	/// (the winch is 110 px to the side of the door) and the rot had two bites of him.
	/// </summary>
	public Vector2 WayStart => WinchPoint;
	/// <summary>The rot on his way, and the way through it proved on paper when it was laid (RotPoolLayout).</summary>
	public RotPoolLayout Rot => layout;
	/// <summary>Where he stands (his feet).</summary>
	public Vector2 HimAt => him.Position;
	/// <summary>True while he is out of the cage and following.</summary>
	public bool IsWalking => phase == Phase.Walking;
	public bool IsHanging => phase == Phase.Hanging;
	public bool IsBoarding => phase == Phase.Boarding;
	public bool IsSinking => phase == Phase.Sinking;
	public int Health => health;
	/// <summary>True while his feet are in a pool's bite.</summary>
	public bool InRot => inPool >= 0;
	/// <summary>0 at the cage, 1 at the landing: how much of the way he has come on this walk.</summary>
	public float WalkProgress => walkFrom <= 1f ? 0f : Mathf.Clamp(1f - him.Position.DistanceTo(landing) / walkFrom, 0f, 1f);

	// --- placement ------------------------------------------------------------------------------------------

	/// <summary>
	/// The gibbet on open ground through the band (the seal's and the hut's sweep), then the landing a walk from it:
	/// LandingMin to LandingMax off at a seeded bearing, the first of twenty-four that is open ground and off every
	/// earlier event's footprint (the lanterns'), else the first open one. Not a site: nothing is found there, and
	/// nothing points at it until he walks. Reserved, so no tree stands on the jetty. Then the rot between them, from
	/// this placer's own rolls (RotPoolLayout): each pool and each wall's gap reserved, so no tree grows out of the
	/// sludge or stands in the one way through a wall.
	/// </summary>
	public override void PlaceSites(ExplorePlacer placer)
	{
		Vector2 cage = AddSite(placer.EventSiteInBand(SiteFootprint, dense: true)).Position;
		float bearing = placer.NextFloat() * Mathf.Tau;
		float step = Mathf.Lerp(LandingMin, LandingMax, placer.NextFloat());
		const float footprint = 150f;
		Vector2? open = null, kept = null;
		for (int k = 0; k < 24 && kept == null; k++)
		{
			Vector2 candidate = placer.Snap(cage + Vector2.FromAngle(bearing + k * Mathf.Tau / 24f) * step);
			// Open over the whole of it, not only at its foot: a landing whose bank is half water is one the player
			// cannot bring him to (seed 7's first landing: he stopped at 93% of the way, the probe on the shore).
			if (!placer.IsClearDense(candidate, footprint))
				continue;
			open ??= candidate;
			if (placer.KeepsSiteRule(candidate, footprint))
				kept = candidate;
		}
		landing = kept ?? open ?? placer.Near(cage, LandingMin, LandingMax, footprint, new[] { cage }, LandingMin, cage, LandingMax + 200f);
		if (kept != null || open != null)
			placer.Reserve(landing, footprint);

		ulong seed = ((ulong)(placer.NextFloat() * 4294967295f) << 32) | (ulong)(placer.NextFloat() * 4294967295f);
		layout = RotPoolLayout.Generate(WayStart, landing, seed, placer.IsWalkable);
		for (int i = 0; i < layout.Pools.Count; i++)
			placer.Reserve(layout.Pools[i].Centre, layout.Pools[i].Radius);
		for (int i = 0; i < layout.Openings.Count; i++)
			placer.Reserve(layout.Openings[i].Centre, layout.Openings[i].Width * 0.5f);
		if (LogPhases)
			GD.Print($"GIBBET rot: {layout.Pools.Count} pools in {RotPoolLayout.Walls} walls, roll {layout.Attempts}"
				+ (layout.Trimmed ? " (trimmed)" : "") + (layout.Route != null ? $", a way through in {layout.Route.Length - 1} legs" : ", NO way through")
				+ (layout.RouteOnGround ? " on the ground" : " on open ground (the ground shut the box)"));
	}

	// LedEscort.MayCut: a loop is left out only if the step across it keeps out of the rot and off the water.
	private bool MayStepAcross(Vector2 from, Vector2 to) =>
		(layout == null || !layout.CrossesBite(from, to)) && (Game == null || !Game.IsGroundBlockedAt(from.Lerp(to, 0.5f)));

	// --- what stands there -------------------------------------------------------------------------------------

	private const string Dir = "res://assets/bonelight/world/gibbet/";
	private const string HimPath = "res://assets/bonelight/characters/paracelsus-idle.png";
	private const string HimWalkPath = "res://assets/bonelight/characters/paracelsus-moving.png";

	// tools/art/gibbet.py's cells and the points on them, in art px (drawn at x2).
	private const int GibbetW = 96, GibbetH = 120, PostX = 14, GibbetFoot = 118;
	private static readonly Vector2 Pulley = new(78f, 20f);
	private const int CageW = 32, CageH = 54, CageMiddle = 16, CageFoot = 52;
	private static readonly Vector2 CageLock = new(23f, 33f);
	private const int CageWithHim = 0, CageCough = 1, CageEmpty = 2, CageOpen = 3;
	private const int PoolW = 84, PoolH = 40, PoolFrames = 3;
	private const int LandingW = 120, LandingH = 84;
	private static readonly Vector2 LandingFootArt = new(58f, 78f);
	private const int PuntW = 54, PuntH = 56, PuntMiddle = 27, PuntFoot = 46;
	private const int LinkCell = 8;

	// The world layout round the site (the cage's ground point), x2 of the art.
	private Vector2 PostFoot => MainSite + new Vector2(-(Pulley.X - PostX) * 2f, -10f);
	private Vector2 PulleyAt => PostFoot + (Pulley - new Vector2(PostX, GibbetFoot)) * 2f;
	private Vector2 PoolAt => MainSite + new Vector2(0f, -6f);
	// Along the jetty to where the punt lies beside it, then into it: the punt in the black water right of the jetty.
	private Vector2 JettyEnd => landing + new Vector2(0f, -96f);
	private Vector2 PuntAt => landing + new Vector2(64f, -96f);

	// How high the cage hangs over the pool, raised, in world px.
	private const float HangHeight = 64f;
	private const float LowerSeconds = 1.3f;
	private const float OpenSeconds = 0.5f;
	private const float StepOutSeconds = 0.7f;
	private const float SinkSeconds = 1.4f;
	private const float BoardWalkSeconds = 1.3f;
	private const float DriftSeconds = 2.4f;
	private const float SucceedAfterDrift = 0.7f;

	private Node2D root;
	private Sprite2D gibbet;
	private Sprite2D cage;
	private Node2D chain;
	private Sprite2D himSprite;
	private Sprite2D punt;
	private Node2D bar;
	private Texture2D poolTex, landingTex, linkTex, idleTex, walkTex, rotTex;
	private float hang = HangHeight;
	private float walkPhase;
	private float crankTurn;
	private Vector2 boardFrom;

	// The Mired rising: where each broke the surface, for the ring of mud drawn there.
	private const int RiseMarks = 6;
	private readonly Vector2[] riseAt = new Vector2[RiseMarks];
	private readonly float[] riseAge = new float[RiseMarks];
	private int riseNext;

	// His path, seen ahead: the crumbs he has still to walk (LedEscort.CopyAhead), at most StepsDrawn of them - 2,400 px,
	// more than a screen - refreshed once a frame while he walks, and which pools any of them lie in.
	private const int StepsDrawn = 120;
	private readonly Vector2[] ahead = new Vector2[StepsDrawn];
	private readonly long[] aheadIds = new long[StepsDrawn];
	private readonly bool[] aheadInRot = new bool[StepsDrawn];
	private int aheadCount;
	private int poolsOnPath;

	private static Texture2D Load(string name)
	{
		string path = Dir + name;
		return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
	}

	protected override void OnPlaced()
	{
		TextureFilter = TextureFilterEnum.Nearest;
		for (int i = 0; i < RiseMarks; i++)
			riseAge[i] = 99f;
		Texture2D gibbetTex = Load("gibbet.png"), cageTex = Load("gibbet-cage.png"), puntTex = Load("landing-punt.png");
		poolTex = Load("gibbet-pool.png");
		landingTex = Load("landing.png");
		rotTex = Load("rot-pools.png");
		linkTex = ResourceLoader.Exists("res://assets/bonelight/world/anvil/anvil-link.png")
			? GD.Load<Texture2D>("res://assets/bonelight/world/anvil/anvil-link.png") : null;
		idleTex = ResourceLoader.Exists(HimPath) ? GD.Load<Texture2D>(HimPath) : null;
		walkTex = ResourceLoader.Exists(HimWalkPath) ? GD.Load<Texture2D>(HimWalkPath) : null;
		if (gibbetTex == null || cageTex == null || puntTex == null || poolTex == null || landingTex == null || linkTex == null
			|| idleTex == null || walkTex == null || rotTex == null)
		{
			GD.PushError("GibbetCageEvent: the gibbet's art is missing or not imported (python tools/art/gibbet.py, then import).");
			return;
		}
		root = new Node2D { Name = "Gibbet", YSortEnabled = true };
		gibbet = Grounded(gibbetTex, GibbetW, PostX, GibbetFoot, "Post");
		root.AddChild(gibbet);
		gibbet.Position = new Vector2(PostFoot.X, PostFoot.Y - WorldSprites.SortLift);
		cage = Grounded(cageTex, CageW, CageMiddle, CageFoot, "Cage");
		root.AddChild(cage);
		// The chain from the pulley down to the cage's ring, drawn behind the cage in its own (x2) space.
		chain = new Node2D { Name = "Chain", ShowBehindParent = true };
		chain.Draw += DrawChain;
		cage.AddChild(chain);
		himSprite = Grounded(walkTex, 48, 24, 46, "Paracelsus");
		himSprite.Visible = false;
		root.AddChild(himSprite);
		punt = Grounded(puntTex, PuntW, PuntMiddle, PuntFoot, "Punt");
		root.AddChild(punt);
		punt.Position = new Vector2(PuntAt.X, PuntAt.Y - WorldSprites.SortLift);
		// His bar, over the bodies and their icons: only while he walks and the rot has had some of him.
		bar = new Node2D { Name = "HisBar", ZAsRelative = false, ZIndex = DrawLayers.AttackIcon, Visible = false };
		bar.Draw += DrawBar;
		AddChild(bar);
		PlaceCage();
		// In the run's world at its origin, so its children stand at world positions; it is the event's sibling.
		Game?.CallDeferred(Node.MethodName.AddChild, root);
	}

	// A sprite stood on its feet, sorted at them (WorldSprites.SortLift), drawn at x2 from a sheet of cells.
	private static Sprite2D Grounded(Texture2D tex, int cellW, int middle, int footRow, string name) => new()
	{
		Name = name,
		Texture = tex,
		Hframes = Math.Max(1, tex.GetWidth() / cellW),
		Centered = false,
		Scale = new Vector2(2f, 2f),
		TextureFilter = TextureFilterEnum.Nearest,
		Offset = new Vector2(-middle, WorldSprites.SortLift / 2f - footRow),
	};

	public override void _ExitTree()
	{
		if (chain != null && IsInstanceValid(chain))
			chain.Draw -= DrawChain;
		if (bar != null && IsInstanceValid(bar))
			bar.Draw -= DrawBar;
		base._ExitTree();
	}

	// --- the rules ----------------------------------------------------------------------------------------------

	protected override void DormantTick(float delta) => PlaceCage();

	protected override void Begin()
	{
		// He is here: the gibbet is no place to point at any more; the winch is (its own arrow), then the landing.
		foreach (PointOfInterest site in Sites)
			site.Done = true;
	}

	protected override void Tick(float delta)
	{
		phaseFor += delta;
		if (takenBackFor >= 0f)
			takenBackFor += delta;
		hurtFlash = Mathf.Max(0f, hurtFlash - delta);
		coughClock = Mathf.Max(0f, coughClock - delta);
		for (int i = 0; i < RiseMarks; i++)
			riseAge[i] += delta;
		switch (phase)
		{
			case Phase.Hanging:
				TickWinch(delta);
				break;
			case Phase.Lowering:
				hang = HangHeight * Mathf.Clamp(1f - phaseFor / LowerSeconds, 0f, 1f);
				crankTurn += delta * 9f;
				if (phaseFor >= LowerSeconds)
				{
					hang = 0f;
					SfxPlayer.Impact(Element.Metal, MainSite);
					Enter(Phase.Opening);
				}
				break;
			case Phase.Opening:
				if (phaseFor >= OpenSeconds + StepOutSeconds)
					Release();
				break;
			case Phase.Walking:
				TickWalk(delta);
				break;
			case Phase.Sinking:
				if (phaseFor >= SinkSeconds)
					TakenBack();
				break;
			case Phase.Boarding:
				TickBoarding();
				break;
		}
		PlaceCage();
		PlaceHim();
	}

	/// <summary>Prints the rot's layout and each change of phase, each rising and each fall. Set by _ExploreProbe.</summary>
	public static bool LogPhases { get; set; }

	private void Enter(Phase next)
	{
		if (LogPhases)
			GD.Print($"GIBBET {phase} -> {next} at t={Clock:0.0}s: him ({him.Position.X:0},{him.Position.Y:0}), landing ({landing.X:0},{landing.Y:0}), "
				+ $"player ({Player?.GlobalPosition.X:0},{Player?.GlobalPosition.Y:0}), health {health}, "
				+ $"{WalkProgress * 100f:0}% of the way, {RotTicksThisWalk} rot bites this walk ({RotTicks} in all, {PoolEntries} pools stepped in), "
				+ $"{him.LoopsLeftOut} loops left out, {Risen} risen ({RisenInGaps} in gaps)");
		phase = next;
		phaseFor = 0f;
	}

	// Standing at the winch fills the hold; away from it the hold drains. Full, the cage comes down.
	private void TickWinch(float delta)
	{
		if (PlayerWithin(WinchPoint, WinchRadius))
		{
			winch = Mathf.Min(1f, winch + delta / WinchSeconds);
			crankTurn += delta * 4f;
		}
		else
			winch = Mathf.Max(0f, winch - delta * WinchDrainPerSecond);
		if (winch >= 1f)
		{
			winch = 0f;
			takenBackFor = -1f;
			Enter(Phase.Lowering);
		}
	}

	// The door is open and he has stepped out onto the bank: he follows from here.
	private void Release()
	{
		him.Reset(Door);
		walkFrom = Mathf.Max(1f, Door.DistanceTo(landing));
		health = HealthMax;
		inPool = -1;
		rotClock = 0f;
		RotTicksThisWalk = 0;
		aheadCount = 0;
		poolsOnPath = 0;
		riseClock = RiseSeconds * 0.6f;
		Releases++;
		Enter(Phase.Walking);
	}

	private void TickWalk(float delta)
	{
		float moved = him.Step(delta, Player.GlobalPosition, Player.WalkingSpeed);
		if (moved > 0f)
			walkPhase += moved / 112f;
		Walked += moved;
		BestProgress = Mathf.Max(BestProgress, WalkProgress);
		ReadPathAhead();

		// The bog closes the way.
		riseClock += delta;
		if (riseClock >= RiseSeconds)
		{
			riseClock = 0f;
			Rise();
		}

		// The rot: a bite the moment his feet are in a pool, and every RotTickSeconds they stay.
		int under = layout?.PoolUnder(him.Position) ?? -1;
		if (under >= 0)
		{
			if (inPool < 0)
			{
				PoolEntries++;
				rotClock = RotTickSeconds;
			}
			inPool = under;
			rotClock += delta;
			while (rotClock >= RotTickSeconds)
			{
				rotClock -= RotTickSeconds;
				if (TakeRot())
					return;
			}
		}
		else
		{
			inPool = -1;
			rotClock = 0f;
		}

		// At the ferry: he is at the landing's foot, or the player is and he is close behind him (the bank there is the
		// player's to find - he only ever walks where the player walked).
		bool atLanding = him.Position.DistanceTo(landing) <= BoardRadius
			|| (PlayerWithin(landing, BoardRadius) && him.Position.DistanceTo(Player.GlobalPosition) <= him.FollowGap + 40f);
		if (atLanding)
		{
			boardFrom = him.Position;
			BestProgress = 1f;
			inPool = -1;
			Enter(Phase.Boarding);
		}
	}

	// One bite of the rot. True if it was the last of him.
	private bool TakeRot()
	{
		health -= RotDamage;
		RotTicks++;
		RotTicksThisWalk++;
		RotTaken += RotDamage;
		hurtFlash = 0.14f;
		if (coughClock <= 0f)
		{
			coughClock = CoughSeconds;
			SfxPlayer.AtPosition(SfxCatalog.EscortCough, him.Position);
		}
		if (health > 0)
			return false;
		health = 0;
		Fall();
		return true;
	}

	// His path ahead, for the drawing: the crumbs he has still to walk, which of them lie in the rot, and which pools.
	private void ReadPathAhead()
	{
		aheadCount = him.CopyAhead(ahead, aheadIds);
		poolsOnPath = 0;
		for (int k = 0; k < aheadCount; k++)
		{
			int pool = layout?.PoolUnder(ahead[k]) ?? -1;
			aheadInRot[k] = pool >= 0;
			if (pool >= 0 && pool < 32)
				poolsOnPath |= 1 << pool;
		}
	}

	/// <summary>
	/// For _UiShot: out of the cage now, through the real release, and stood <paramref name="along"/> of the way along
	/// the route proved through the rot (RotPoolLayout.Route), with the steps a player keeping ahead of him has walked
	/// and he has not laid on along it for DebugLead px (1 puts him within BoardRadius of the landing, so the punt takes
	/// him on the next step). <paramref name="straight"/> lays those steps straight at the landing instead, through
	/// whatever rot is in the way. Returns where the player should stand: where those steps end. A run never calls it.
	/// </summary>
	public Vector2 DebugWalkFrom(float along, bool straight = false)
	{
		// Always a fresh walk: _UiShot holds the player on the arrow's target until now, and once he is out that is the
		// landing, so the steps he had were one straight line through every wall and the rot had most of him.
		Release();
		Vector2[] route = layout?.Route;
		if (along >= 1f || route == null || route.Length < 2)
		{
			Vector2 at = along >= 1f ? landing + (Door - landing).Normalized() * (BoardRadius * 0.5f) : WayStart.Lerp(landing, Mathf.Clamp(along, 0f, 1f));
			him.Reset(at);
			PlaceHim();
			return at + (landing - at).Normalized() * 90f;
		}
		float total = 0f;
		var corners = new System.Text.StringBuilder();
		for (int i = 1; i < route.Length; i++)
		{
			total += route[i - 1].DistanceTo(route[i]);
			if (i < route.Length - 1)
				corners.Append($" {total:0}");
		}
		GD.Print($"GibbetCageEvent: the way through the rot is {total:0} px in {route.Length - 1} legs, its corners at{corners} px");
		float from = total * Mathf.Clamp(along, 0f, 1f);
		float to = Mathf.Min(total, from + DebugLead);
		// Straight, he stands on the straight line from the winch instead, where a player walking at the arrow has led him.
		Vector2 start = straight ? WayStart.Lerp(landing, Mathf.Clamp(along, 0f, 1f)) : OnRoute(route, from);
		him.Reset(start);
		Vector2 heading = (landing - start).Normalized();
		Vector2 Step(float s) => straight ? start + heading * (s - from) : OnRoute(route, s);
		for (float s = from; s < to; s += LedEscort.CrumbSpacing * 0.5f)
			him.Record(Step(s));
		Vector2 stand = Step(to);
		him.Record(stand);
		ReadPathAhead();
		PlaceHim();
		return stand;
	}

	private const float DebugLead = 420f;

	private static Vector2 OnRoute(Vector2[] route, float distance)
	{
		for (int i = 1; i < route.Length; i++)
		{
			float leg = route[i - 1].DistanceTo(route[i]);
			if (distance <= leg)
				return route[i - 1].Lerp(route[i], leg > 0f ? distance / leg : 0f);
			distance -= leg;
		}
		return route[^1];
	}

	// How far along the way to the landing a point is, from where the way starts (the winch).
	private float AlongWay(Vector2 point) => (point - WayStart).Dot((landing - WayStart).Normalized());

	// The chapter's Mired breaking the surface where the way is: in the first gap in the rot further along than he or the
	// player has come, across it - so the gap the player meant to take is shut for a while, and the line keeps changing.
	// Past the last wall there is no gap ahead, and they rise across his way as they always did: spread over 140 degrees
	// of the way he is going, RiseMin to RiseMax ahead of him. Nothing in them is aimed at him; they chase the player.
	private void Rise()
	{
		if (Game == null)
			return;
		float passed = Mathf.Max(AlongWay(him.Position), AlongWay(Player.GlobalPosition)) + 60f;
		int gap = -1;
		for (int i = 0; layout != null && i < layout.Openings.Count; i++)
		{
			float at = AlongWay(layout.Openings[i].Centre);
			if (at > passed && (gap < 0 || at < AlongWay(layout.Openings[gap].Centre)))
				gap = i;
		}
		float ahead = (landing - him.Position).Angle();
		for (int i = 0; i < RiseCount; i++)
		{
			Vector2 at;
			if (gap >= 0)
			{
				RotPoolLayout.Opening opening = layout.Openings[gap];
				float share = (i + 0.5f) / RiseCount - 0.5f;
				at = opening.Centre + opening.Across * (opening.Width * 0.7f * share)
					+ opening.Across.Orthogonal() * (float)GD.RandRange(-20.0, 20.0);
			}
			else
			{
				float angle = ahead + Mathf.DegToRad(-70f + 140f * (i + 0.5f) / RiseCount) + (float)GD.RandRange(-0.25, 0.25);
				at = him.Position + Vector2.FromAngle(angle) * (float)GD.RandRange(RiseMin, RiseMax);
			}
			Enemy risen = Game.SpawnEventEnemy(BestiaryRole.Bulk, at, RiseHealth, false);
			if (risen == null)
				continue;
			// The placement moves anything it cannot prove reachable to the spawn ring, and it asks the navigation
			// window - which, measured, did not cover him a few seconds after the walk began (every riser of the
			// first probe runs went to the ring, 400-1100 px off). The bog raises them where it raises them: on any
			// ground that is not water or a wall, they come up there.
			if (risen.GlobalPosition.DistanceSquaredTo(at) > 60f * 60f && !Game.IsGroundBlockedAt(at))
				risen.GlobalPosition = at;
			Risen++;
			if (gap >= 0)
				RisenInGaps++;
			riseAt[riseNext] = risen.GlobalPosition;
			riseAge[riseNext] = 0f;
			riseNext = (riseNext + 1) % RiseMarks;
		}
	}

	// The rot has had all of him: the bog takes him.
	private void Fall()
	{
		Falls++;
		inPool = -1;
		Enter(Phase.Sinking);
		SfxPlayer.Impact(Element.Poison, him.Position);
	}

	// Gone under, and in the cage again, hoisted, the door shut: the winch to hold again, the walk to start again.
	private void TakenBack()
	{
		hang = HangHeight;
		health = HealthMax;
		winch = 0f;
		takenBackFor = 0f;
		aheadCount = 0;
		poolsOnPath = 0;
		Enter(Phase.Hanging);
	}

	// Along the jetty to its end, then into the punt; the punt drifts off into the fog with him, and he is free.
	private void TickBoarding()
	{
		float walk = Mathf.Clamp(phaseFor / BoardWalkSeconds, 0f, 1f);
		if (walk < 1f)
		{
			Vector2 mid = JettyEnd;
			Vector2 before = him.Position;
			him.Position = walk < 0.6f ? boardFrom.Lerp(mid, walk / 0.6f) : mid.Lerp(PuntAt, (walk - 0.6f) / 0.4f);
			walkPhase += before.DistanceTo(him.Position) / 112f;
			return;
		}
		float drift = phaseFor - BoardWalkSeconds;
		if (!succeeded && drift >= SucceedAfterDrift)
		{
			succeeded = true;
			Succeed(landing);
		}
	}

	protected override void OnStoodDown()
	{
		if (himSprite != null && IsInstanceValid(himSprite))
			himSprite.Visible = false;
		if (bar != null && IsInstanceValid(bar))
			bar.Visible = false;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		// After the win the punt keeps drifting off with him and is gone into the fog.
		if (Succeeded && phase == Phase.Boarding)
		{
			phaseFor += (float)delta;
			PlaceHim();
		}
	}

	// --- the sprites -------------------------------------------------------------------------------------------

	private void PlaceCage()
	{
		if (cage == null || !IsInstanceValid(cage))
			return;
		// It swings a little on its chain while it hangs; still once it is down.
		float sway = hang > 0f ? Mathf.Sin(Clock * 1.1f) * 3f * (hang / HangHeight) : 0f;
		Vector2 foot = SpellInk.Snap(MainSite + new Vector2(sway, 0f));
		cage.Position = new Vector2(foot.X, foot.Y - WorldSprites.SortLift);
		cage.Offset = new Vector2(-CageMiddle, WorldSprites.SortLift / 2f - CageFoot - Mathf.Round(hang / 2f));
		int frame;
		if (phase == Phase.Hanging || phase == Phase.Lowering)
			frame = (int)(Clock / 2.6f) % 3 == 2 && phase == Phase.Hanging ? CageCough : CageWithHim;
		// Down, the door shut on him for OpenSeconds; then it swings open as his own sprite steps out of it (PlaceHim).
		else if (phase == Phase.Opening)
			frame = phaseFor < OpenSeconds ? CageWithHim : CageOpen;
		else
			frame = CageOpen;
		cage.Frame = Mathf.Clamp(frame, 0, cage.Hframes - 1);
		chain?.QueueRedraw();
		if (gibbet != null && IsInstanceValid(gibbet))
			gibbet.Frame = ((int)crankTurn) % Math.Max(1, gibbet.Hframes);
	}

	private void PlaceHim()
	{
		if (himSprite == null || !IsInstanceValid(himSprite))
			return;
		// His bar only once the rot has had some of him, or while it has him: until then there is nothing to read in it.
		bar.Visible = phase == Phase.Walking && (health < HealthMax || inPool >= 0);
		bar.QueueRedraw();
		Vector2 at;
		bool walking = false;
		float alpha = 1f;
		int sunk = 0;
		switch (phase)
		{
			case Phase.Opening when phaseFor >= OpenSeconds:
			{
				// Stepping down out of the open door onto the bank.
				float t = Mathf.Clamp((phaseFor - OpenSeconds) / StepOutSeconds, 0f, 1f);
				at = MainSite.Lerp(Door, t * t * (3f - 2f * t));
				walking = true;
				break;
			}
			case Phase.Walking:
				at = him.Position;
				walking = him.Speed > 1f;
				break;
			case Phase.Sinking:
				at = him.Position;
				sunk = Mathf.Clamp(Mathf.RoundToInt(44f * phaseFor / SinkSeconds), 0, 44);
				break;
			case Phase.Boarding:
			{
				at = him.Position;
				walking = phaseFor < BoardWalkSeconds;
				if (phaseFor >= BoardWalkSeconds)
				{
					// In the punt now: the punt's own frame carries him (gibbet.py draws him standing in it).
					himSprite.Visible = false;
					PlacePunt(Mathf.Clamp((phaseFor - BoardWalkSeconds) / DriftSeconds, 0f, 1f), true);
					return;
				}
				break;
			}
			default:
				himSprite.Visible = false;
				PlacePunt(0f, false);
				return;
		}
		PlacePunt(0f, false);
		himSprite.Visible = true;
		Vector2 foot = SpellInk.Snap(at);
		himSprite.Position = new Vector2(foot.X, foot.Y - WorldSprites.SortLift);
		himSprite.Texture = walking ? walkTex : idleTex;
		himSprite.Hframes = Math.Max(1, himSprite.Texture.GetWidth() / 48);
		himSprite.Frame = walking ? ((int)(walkPhase * 8f)) % himSprite.Hframes : ((int)(Clock * 6f)) % himSprite.Hframes;
		// He faces the way he walks (his sheet faces the viewer's right, the flask on that side).
		if (phase == Phase.Walking && Mathf.Abs(him.Heading.X) > 0.25f)
			himSprite.FlipH = him.Heading.X < 0f;
		// Sinking: the bottom of him goes first, row by row, into the bog.
		himSprite.RegionEnabled = sunk > 0;
		if (sunk > 0)
			himSprite.RegionRect = new Rect2(0, 0, 48 * himSprite.Hframes, 48 - sunk);
		himSprite.Offset = new Vector2(-24f, WorldSprites.SortLift / 2f - 46f + sunk);
		// In the rot he goes the sickly colour of it; a bite flashes him.
		float flash = hurtFlash > 0f ? 1f : 0f;
		Color tint = inPool >= 0 && phase == Phase.Walking ? new Color(0.86f, 1f, 0.7f) : Colors.White;
		himSprite.Modulate = new Color(tint.R, tint.G * (1f - 0.45f * flash), tint.B * (1f - 0.45f * flash), alpha);
	}

	// The punt at its mooring, or drifting off with him into the fog (t 0 to 1), fading as it goes.
	private void PlacePunt(float t, bool aboard)
	{
		if (punt == null || !IsInstanceValid(punt))
			return;
		Vector2 at = SpellInk.Snap(PuntAt + new Vector2(70f * t * t, -26f * t));
		punt.Position = new Vector2(at.X, at.Y - WorldSprites.SortLift);
		punt.Frame = aboard ? Mathf.Min(1, punt.Hframes - 1) : 0;
		punt.Modulate = new Color(1f, 1f, 1f, aboard ? Mathf.Clamp(1f - (t - 0.35f) / 0.65f, 0f, 1f) : 1f);
		punt.Visible = !aboard || t < 1f;
	}

	// The chain from the pulley to the cage's ring, link by link, in the cage's own space (x2).
	private void DrawChain()
	{
		if (linkTex == null || cage == null || !IsInstanceValid(cage))
			return;
		Vector2 ring = cage.Offset + new Vector2(CageMiddle, 1f);
		Vector2 top = (PulleyAt - cage.Position) / 2f;
		Vector2 along = top - ring;
		float length = along.Length();
		if (length < 1f)
			return;
		Vector2 dir = along / length;
		// A link every 9 world px (4.5 here), each the anvil's 8 px link at the world's x2 - GreatAnvilEvent's chain, hung.
		int links = Mathf.Max(1, Mathf.CeilToInt(length / 4.5f));
		var size = new Vector2(LinkCell, LinkCell);
		for (int k = 0; k < links; k++)
		{
			Vector2 at = (ring + dir * (k * length / links)).Round();
			chain.DrawTextureRectRegion(linkTex, new Rect2(at - size / 2f, size), new Rect2((k % 2) * LinkCell, 0, LinkCell, LinkCell));
		}
	}

	// His bar, over his head once the rot has had some of him: the bar's track in occlusion, what he has left in a warm
	// pale gold (he is a person, not one of his), the bite just taken flashing white.
	private void DrawBar()
	{
		if (phase != Phase.Walking)
			return;
		Vector2 top = SpellInk.Snap(him.Position) + new Vector2(-24f, -112f);
		const float w = 48f, h = 6f;
		bar.DrawRect(new Rect2(top - new Vector2(2f, 2f), new Vector2(w + 4f, h + 4f)), new Color(0.020f, 0.027f, 0.047f, 0.92f));
		bar.DrawRect(new Rect2(top, new Vector2(w, h)), new Color(0.165f, 0.055f, 0.078f));
		float share = health / (float)HealthMax;
		Color fill = hurtFlash > 0f ? new Color(1f, 0.94f, 0.75f) : share < 0.4f ? new Color(0.847f, 0.643f, 0.518f) : new Color(0.847f, 0.69f, 0.29f);
		bar.DrawRect(new Rect2(top, new Vector2(Mathf.Round(w * share / 2f) * 2f, h)), fill);
		bar.DrawRect(new Rect2(top, new Vector2(w * share, 2f)), new Color(1f, 0.94f, 0.75f, 0.55f));
	}

	// --- drawing on the ground -----------------------------------------------------------------------------------

	// The steps ahead of him: the light ramp's hot and mid stops on the peat (warm, his - the bar's family), and where
	// they run through the rot, the poison ramp's mid on its edge, so the trail itself says "he will be in it here".
	private static readonly Color StepWarm = new(1f, 0.973f, 0.847f);          // light #FFF8D8
	private static readonly Color StepWarmEdge = new(0.545f, 0.38f, 0.07f);    // under it: gold's shade-to-base, so it holds on pale ground
	private static readonly Color StepRot = new(0.706f, 0.878f, 0.29f);        // poison #B4E04A
	private static readonly Color StepRotEdge = new(0.18f, 0.133f, 0.031f);    // gold's deep, the sludge under it
	private static readonly Color GlowWarm = new(1f, 0.776f, 0.235f);          // light's edge #FFC63C
	private static readonly Color GlowRot = new(0.42f, 0.58f, 0.125f);         // poison's edge #6B9420

	// How long the rot takes to fade off the bog after the punt has taken him (the event is freed a few seconds on).
	private const float RotFadeSeconds = 2f;

	// The rot pools on his way, the rot pool under the gibbet, the landing's black water and jetty, the steps ahead of
	// him, and the mud where the Mired broke the surface. World units: the node is at the run's origin.
	public override void _Draw()
	{
		if (Player == null)
			return;
		DrawRot();
		if (poolTex != null && NearPlayer(PoolAt))
		{
			int f = (int)(Clock / 0.45f) % PoolFrames;
			Vector2 at = SpellInk.Snap(ToLocal(PoolAt)) - new Vector2(PoolW, PoolH);
			DrawTextureRectRegion(poolTex, new Rect2(at, new Vector2(PoolW * 2f, PoolH * 2f)), new Rect2(f * PoolW, 0, PoolW, PoolH));
		}
		if (landingTex != null && NearPlayer(landing))
		{
			Vector2 at = SpellInk.Snap(ToLocal(landing)) - LandingFootArt * 2f;
			DrawTextureRect(landingTex, new Rect2(at, new Vector2(LandingW * 2f, LandingH * 2f)), false);
		}
		if (phase == Phase.Walking && !IsResolved)
			DrawSteps();
		// The Mired coming up: a ring of the bog's own dark spreading out and going, where each broke the surface.
		for (int i = 0; i < RiseMarks; i++)
		{
			float age = riseAge[i];
			if (age > 0.8f)
				continue;
			float r = 10f + 30f * age / 0.8f;
			Color mud = new Color(0.106f, 0.153f, 0.090f, 0.8f * (1f - age / 0.8f));
			DrawArc(ToLocal(riseAt[i]), r, 0f, Mathf.Tau, 20, mud, 4f);
			DrawArc(ToLocal(riseAt[i]) + new Vector2(0f, -2f), r - 3f, Mathf.Pi, Mathf.Tau, 12,
				new Color(0.706f, 0.878f, 0.290f, 0.35f * (1f - age / 0.8f)), 2f);
		}
		// The rot round his feet while it has him: two rings of its sickly edge rolling out.
		if (phase == Phase.Walking && inPool >= 0)
		{
			Vector2 feet = SpellInk.Snap(ToLocal(him.Position));
			for (int k = 0; k < 2; k++)
			{
				float t = (Clock * 1.6f + k * 0.5f) % 1f;
				DrawArc(feet, 12f + 22f * t, 0f, Mathf.Tau, 18, new Color(GlowRot, 0.8f * (1f - t)), 2f);
			}
		}
		// The winch's hold, filling along a dashed ring round its foot while the cage hangs.
		if (IsFound && !IsResolved && phase == Phase.Hanging)
		{
			bool at = PlayerWithin(WinchPoint, WinchRadius);
			DrawProgressArc(WinchPoint, WinchRadius, winch, TellColor, 3f, at ? 0.42f : 0.24f);
		}
	}

	// The rot on his way: each pool still, or stirred and bubbling while he is near it or his path ahead runs into it;
	// fading off the bog once the punt has him.
	private void DrawRot()
	{
		if (rotTex == null || layout == null)
			return;
		float alpha = IsResolved ? Mathf.Clamp(1f - SecondsSinceResolved / RotFadeSeconds, 0f, 1f) : 1f;
		if (alpha <= 0f)
			return;
		const int cell = RotPoolLayout.ArtCell;
		var size = new Vector2(cell * 2f, cell * 2f);
		var modulate = new Color(1f, 1f, 1f, alpha);
		for (int i = 0; i < layout.Pools.Count; i++)
		{
			RotPoolLayout.Pool pool = layout.Pools[i];
			if (!NearPlayer(pool.Centre))
				continue;
			int frame = IsStirred(i) ? 1 + ((int)(Clock / 0.22f) + i) % (RotPoolLayout.ArtFrames - 1) : 0;
			Vector2 centre = SpellInk.Snap(ToLocal(pool.Centre));
			DrawSetTransform(centre, 0f, new Vector2(pool.Flip ? -1f : 1f, 1f));
			DrawTextureRectRegion(rotTex, new Rect2(-size / 2f, size), new Rect2(frame * cell, pool.ArtRow * cell, cell, cell), modulate);
		}
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
	}

	private bool IsStirred(int pool)
	{
		if (phase != Phase.Walking || layout == null)
			return false;
		RotPoolLayout.Pool p = layout.Pools[pool];
		float reach = p.Radius + StirReach;
		return him.Position.DistanceSquaredTo(p.Centre) < reach * reach || (pool < 32 && (poolsOnPath & (1 << pool)) != 0);
	}

	// His path, seen ahead: each crumb he has still to walk as a small mark beside the line, left and right in turn by
	// the crumb's own number (so a mark holds still as he walks), a shimmer running along them toward the player.
	private void DrawSteps()
	{
		Vector2 player = Player.GlobalPosition;
		for (int k = 0; k < aheadCount; k++)
		{
			Vector2 p = ahead[k];
			if (p.DistanceSquaredTo(player) > 1500f * 1500f)
				continue;
			Vector2 dir = ahead[Math.Min(k + 1, aheadCount - 1)] - ahead[Math.Max(k - 1, 0)];
			dir = dir.LengthSquared() > 0.01f ? dir.Normalized() : him.Heading;
			float side = (aheadIds[k] & 1) == 0 ? 1f : -1f;
			Vector2 at = SpellInk.Snap(ToLocal(p + dir.Orthogonal() * (5f * side)));
			float shimmer = StepShimmer(aheadIds[k]);
			bool rot = aheadInRot[k];
			Color edge = rot ? StepRotEdge : StepWarmEdge;
			Color core = rot ? StepRot : StepWarm;
			// A 6 px mark (three art pixels) on a darker cross a pixel wider: at 4 px the first photographs lost it on the
			// phone's canvas.
			DrawRect(new Rect2(at - new Vector2(5f, 3f), new Vector2(10f, 6f)), new Color(edge, 0.6f * shimmer));
			DrawRect(new Rect2(at - new Vector2(3f, 5f), new Vector2(6f, 10f)), new Color(edge, 0.6f * shimmer));
			DrawRect(new Rect2(at - new Vector2(3f, 3f), new Vector2(6f, 6f)), new Color(core, 0.95f * shimmer));
		}
	}

	// 0.55 to 1: a brighter beat running along the steps from him toward the player, about two a second.
	private float StepShimmer(long id) => 0.55f + 0.45f * Mathf.Max(0f, Mathf.Sin(Clock * 5f - id * 0.45f));

	// The far tell stands up from his mark on the cage's lock, wherever the cage hangs: full while nobody has come, dim
	// once he has; out once he is out. And always, near, the lock itself - a cold point. While he walks, a faint column
	// over the landing too, so the far end of the walk is seen at the edge of the screen before the arrow is needed; the
	// steps ahead of him glowing; and a sickly glow over every pool that is stirred.
	protected override void DrawLight(CanvasItem light)
	{
		if (cage == null || !IsInstanceValid(cage))
			return;
		Vector2 lockAt = cage.Position + (cage.Offset + CageLock) * 2f;
		bool caged = phase == Phase.Hanging || phase == Phase.Lowering;
		if (NearPlayer(lockAt) && caged)
		{
			float strength = IsDormant ? 1f : 0.4f;
			DrawFarTell(light, lockAt, TellColor, strength * Flicker(3));
			Vector2 p = ToLocal(lockAt);
			ExploreArt.DrawGlow(light, p, 22f, new Color(TellColor, 0.32f * Flicker(1)));
			ExploreArt.DrawGlow(light, p, 8f, new Color(TellColor.Lerp(Colors.White, 0.4f), 0.5f * Flicker(4)));
		}
		if (phase == Phase.Walking && NearPlayer(landing))
			DrawFarTell(light, JettyEnd, new Color(0.706f, 0.835f, 0.9f), 0.35f);
		if (phase == Phase.Walking && layout != null)
		{
			for (int i = 0; i < layout.Pools.Count; i++)
			{
				if (IsStirred(i) && NearPlayer(layout.Pools[i].Centre))
					ExploreArt.DrawGlow(light, ToLocal(layout.Pools[i].Centre), layout.Pools[i].Radius * 1.1f,
						new Color(GlowRot, 0.16f * Flicker(5 + i)));
			}
			Vector2 player = Player.GlobalPosition;
			for (int k = 0; k < aheadCount; k += 2)
			{
				if (ahead[k].DistanceSquaredTo(player) > 1500f * 1500f)
					continue;
				ExploreArt.DrawGlow(light, ToLocal(ahead[k]), 18f,
					new Color(aheadInRot[k] ? GlowRot : GlowWarm, (aheadInRot[k] ? 0.45f : 0.32f) * StepShimmer(aheadIds[k])));
			}
		}
		// The dose in his flask, his own light, while he walks.
		if ((phase == Phase.Walking || phase == Phase.Boarding && phaseFor < BoardWalkSeconds) && himSprite != null && himSprite.Visible)
		{
			Vector2 flask = SpellInk.Snap(him.Position) + new Vector2(himSprite.FlipH ? -28f : 28f, -74f);
			ExploreArt.DrawGlow(light, ToLocal(flask), 18f, new Color(0.706f, 0.878f, 0.290f, 0.35f));
		}
	}
}
