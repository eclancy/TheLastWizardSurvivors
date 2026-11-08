using Godot;
using System;
using TheOneHeMissed.scripts;

/// <summary>
/// The Tithe-Reeve's scale while it is taking its due (TitheScaleEvent): where its pan hangs, how far round it an
/// orb that falls is his, and what it has weighed so far. Plain values, no reference to any node - the orbs ask it
/// as they land (XPOrb.Claims) and pay into it when they reach the pan, and a freed event can never be touched
/// through it. Closed whenever no scale is taking.
/// </summary>
public static class GranaryTithe
{
	/// <summary>True while a scale is taking: orbs that fall inside its reach drift into its pans.</summary>
	public static bool Open { get; private set; }

	/// <summary>Where each pan hangs, in world space: what a tithed orb flies to. Left is -1, right +1 (SideOf).</summary>
	public static Vector2 PanLeft { get; private set; }
	public static Vector2 PanRight { get; private set; }
	public static Vector2 PanFor(int side) => side < 0 ? PanLeft : PanRight;

	/// <summary>The middle of its reach: the scale's foot. The line down from its post splits the reach in two.</summary>
	public static Vector2 Centre { get; private set; }

	public static float Reach { get; private set; }

	/// <summary>
	/// MEDEA'S BALANCE (unlock-puzzles.md 3.5): an orb that lands left of the post goes to the left pan, right of it
	/// to the right, so where he fights decides which pan fills. Asked once, as it lands, with its claim.
	/// </summary>
	public static int SideOf(Vector2 at) => at.X < Centre.X ? -1 : 1;

	/// <summary>What each pan holds since it opened, in points of his bar at the moment each orb came in.</summary>
	public static float TakenLeft { get; private set; }
	public static float TakenRight { get; private set; }
	public static float Taken => TakenLeft + TakenRight;

	/// <summary>
	/// Past this the reeve takes nothing more (3.5's open question, decided at the first guess: 1.6 levels), so a bad
	/// start cannot cost a run - the scale settles itself and the reeve keeps the difference. Infinite until set.
	/// </summary>
	public static float Cap { get; private set; } = float.PositiveInfinity;
	public static bool Capped => Open && Taken >= Cap;

	/// <summary>Orbs weighed, and the XP they carried, since it opened. For the probe.</summary>
	public static int OrbsTaken { get; private set; }
	public static int ValueTaken { get; private set; }

	/// <summary>Orbs on their way to the pan right now. Kept by XPOrb.</summary>
	public static int OrbsInFlight { get; set; }

	// THE FLIGHTS, SEEN (Eric, 2026-11-06; in-flight 61): each orb on its way re-reports where it is every
	// frame, into fixed slots the event draws its stream of motes from. Re-published rather than held, so an
	// orb that dies mid-flight simply stops reporting and there is no slot to leak. Nothing is allocated.
	public const int FlightSlots = 16;
	private static readonly Vector2[] flightAt = new Vector2[FlightSlots];
	private static readonly sbyte[] flightSide = new sbyte[FlightSlots];
	private static ulong flightFrame;
	private static int flightCount;

	/// <summary>An orb on its way says where it is this frame, and which pan it is bound for (XPOrb.TickTithe).</summary>
	public static void ReportFlight(Vector2 at, int side)
	{
		if (!Open)
			return;
		ulong frame = Engine.GetProcessFrames();
		if (frame != flightFrame)
		{
			flightFrame = frame;
			flightCount = 0;
		}
		if (flightCount < FlightSlots)
		{
			flightSide[flightCount] = (sbyte)(side < 0 ? -1 : 1);
			flightAt[flightCount++] = at;
		}
	}

	/// <summary>How many orbs reported this frame: what the stream is drawn from (TitheScaleEvent).</summary>
	public static int Flights => Open && Engine.GetProcessFrames() == flightFrame ? flightCount : 0;

	/// <summary>Where flight <paramref name="i"/> is, in world space, and the pan it is bound for.</summary>
	public static Vector2 FlightPosition(int i) => flightAt[Math.Clamp(i, 0, FlightSlots - 1)];
	public static int FlightSide(int i) => flightSide[Math.Clamp(i, 0, FlightSlots - 1)];

	public static void OpenAt(Vector2 centre, Vector2 panLeft, Vector2 panRight, float reach, float cap)
	{
		Open = true;
		Centre = centre;
		PanLeft = panLeft;
		PanRight = panRight;
		Reach = reach;
		Cap = cap;
		TakenLeft = 0f;
		TakenRight = 0f;
		OrbsTaken = 0;
		ValueTaken = 0;
		OrbsInFlight = 0;
	}

	/// <summary>The pans hang somewhere else now (the beam has tipped).</summary>
	public static void MovePans(Vector2 left, Vector2 right)
	{
		PanLeft = left;
		PanRight = right;
	}

	public static void Close()
	{
		Open = false;
		OrbsInFlight = 0;
		flightCount = 0;
		flightFrame = 0;
	}

	/// <summary>Whether an orb landing at <paramref name="at"/> is the reeve's: inside the reach, and the cap not met.</summary>
	public static bool Claims(Vector2 at) => Open && !Capped && at.DistanceSquaredTo(Centre) <= Reach * Reach;

	/// <summary>An orb has reached pan <paramref name="side"/>: <paramref name="worth"/> points of his bar, carrying <paramref name="value"/>.</summary>
	public static void Pay(float worth, int value, int side)
	{
		if (!Open)
			return;
		if (side < 0)
			TakenLeft += Math.Max(0f, worth);
		else
			TakenRight += Math.Max(0f, worth);
		LastSide = side < 0 ? -1 : 1;
		OrbsTaken++;
		ValueTaken += Math.Max(0, value);
	}

	/// <summary>The pan the last weighed orb went into: where its flash plays (TitheScaleEvent).</summary>
	public static int LastSide { get; private set; } = 1;
}

// The Fallow Fields' second side event: the Tithe-Reeve's scale (.ai/chapter-wizards.md 4.2).
//
// "He takes a tithe of everything that grows here." He counts Medea among what grows: she is held in the reeve's
// granary against a tithe the village could never pay. The reeve's great scale stands in the granary yard, its door
// barred, and the bar is held by the scale's beam.
//
// What it asks: BALANCE (unlock-puzzles.md 3.5, Eric 2026-11-03; built 2026-11-07). Still the only ask in the game
// that costs the player something. From the moment he arrives, every XP orb that falls within TitheReach of the scale
// drifts up into a pan instead of to him (GranaryTithe, XPOrb) - the LEFT pan if it fell left of the post, the RIGHT
// if right, so where he fights decides which pan fills. The beam tips toward the heavier. The price is one level's XP
// at that moment of the run (Player.XPToNextLevel at arrival, each orb weighed at Player.XpWorthOf when it reaches its
// pan). Once the two pans hold the price between them, KEEP THE BEAM LEVEL FOR HoldSeconds - within LevelBand of the
// price of each other - and the bar falls from the granary door, the doors open and she steps out. Overshoot one pan
// and the only answer is to fight on the other side, which costs more: the reeve takes the difference too.
//
// NO FAILURE, ONLY A PRICE. Walking off keeps what is paid: the pans never empty, the arrow stays on the scale, and
// whatever falls near it later goes on the count - nothing drains, nothing locks, nothing comes at him for it. The
// hold only counts while the beam is level. And the price is CAPPED at CapInLevels: past it the reeve takes nothing
// more, keeps the difference, and the beam settles level by itself - a bad start costs XP, never the run. There is no
// ring of enemies either: what pays the tithe is the chapter's own crowd, killed where the reeve can count it.
//
// Every SideEvent rule holds: placed by seed, dormant until he arrives, nothing timed by the pace clock, nothing it
// places in "enemies" (the scale and the granary are sprites with no collision, sorted at their feet as the hut is),
// and it stands down when the boss arrives - the pan stops taking, and anything still in the air falls to him.
//
// Art: tools/art/tithe_scale.py (world/granary/: the granary's three frames, the scale's six, poi-scale). The one
// light here is hers, the cold herb-smoke that leaks from the granary's eaves; the scale is iron and oak and his.
public partial class TitheScaleEvent : SideEvent
{
	/// <summary>How far round the scale an orb that falls is the reeve's (4.2: 320).</summary>
	public const float TitheReach = 320f;

	/// <summary>The price, as a share of his bar at arrival: one level (4.2).</summary>
	public const float PriceInLevels = 1f;

	/// <summary>"Level": the pans within this share of the price of each other (3.5's first guess, 10%).</summary>
	public const float LevelBand = 0.10f;

	/// <summary>Seconds the beam must stay level, once the price is met, before the bar falls (3.5: decided, 3 s).</summary>
	public const float HoldSeconds = 3f;

	/// <summary>The most the reeve can ever take, in levels (3.5's first guess, 1.6): past it the beam settles itself.</summary>
	public const float CapInLevels = 1.6f;

	/// <summary>How long the beam takes to settle level by itself once the cap is met.</summary>
	public const float SettleSeconds = 1.2f;

	// A met price reads as met a hair under: a float that has carried the whole price is not always exactly it.
	private const float Full = 0.999f;

	protected override string Announcement => "A great scale in the granary yard. What falls either side of it is weighed.";
	protected override string ChestLine => "The scale is level - a chest was left in the granary";
	protected override string Objective => IsResolved || paidFor >= 0f ? string.Empty
		: !PriceMet ? $"Fill the reeve's two pans  ({Mathf.FloorToInt(Mathf.Min(PaidShare, 1f) * 100f)}%)"
		: !IsLevel ? "Level the beam: fight on the lighter side"
		: $"Hold the beam level  ({Mathf.CeilToInt(Mathf.Max(0f, HoldSeconds - levelHeld))})";
	// The post's head, until it is paid: the arrow says where the scale stands.
	protected override Vector2? PointerTarget => IsResolved || paidFor >= 0f ? null : ScaleWorld(Pivot);

	protected override string SiteGlyph => "scale";
	// The yard: the granary, the scale and room round them; no tree or stook is ever put down on it.
	protected override float SiteFootprint => 250f;
	protected override float ArrivalRadius => 460f;
	// Her smoke: the wind ramp's body, cold and pale - herbs burning low somewhere shut away. Not the well's water.
	protected override Color TellColor => new(0.706f, 0.933f, 0.878f);

	// --- where it all stands, from the yard's middle (the site) --------------------------------------

	/// <summary>The granary's foot, from the site.</summary>
	public static readonly Vector2 GranaryOffset = new(-40f, -40f);

	/// <summary>The scale's foot, from the site: in front of the granary and to its right, off the door.</summary>
	public static readonly Vector2 ScaleOffset = new(150f, 70f);

	// tools/art/tithe_scale.py: the granary 96x96 a frame, its foot's middle on column 48, the ground row 95; the scale
	// 64x80, column 32, row 79. Both drawn at x2.
	private const int GranaryW = 96;
	private const int GranaryH = 96;
	private const int GranaryMiddle = 48;
	private const int GranaryFootRow = 95;
	private const int ScaleW = 64;
	private const int ScaleH = 80;
	private const int ScaleMiddle = 32;
	private const int ScaleFootRow = 79;

	// The beam (tithe_scale.py PIVOT, ARM, HANG, TILTS - change one, change both): the pivot on the post's head, each
	// arm's length to its chains, the chains' drop to the bowl, and the beam's tilt in each frame, degrees, positive
	// with the LEFT pan down. Frames 0-2 tip left by degrees, 3 is level, 4-6 tip right.
	private static readonly Vector2 Pivot = new(32f, 13f);
	private const float Arm = 24f;
	private const float Hang = 27f;
	private static readonly float[] Tilts = { 18f, 12f, 6f, 0f, -6f, -12f, -18f };
	public const int LevelFrame = 3;
	// The level mark on the post head (tithe_scale.py MARK_Y): what lights while the beam is level and the price met.
	private static readonly Vector2 LevelMark = new(32f, 2f);

	/// <summary>The sheets' frame counts, for RegressionChecks.ValidateTitheScale.</summary>
	public const int ScaleFrames = 7;
	public const int GranaryFrames = 3;

	// The granary's frames: barred, the bar fallen and the doors ajar, open.
	private const int GranaryBarred = 0;
	private const int GranaryAjar = 1;
	private const int GranaryOpen = 2;
	// The doors' sill, authored, and the foot of the steps under it: she comes out at the one and down to the other.
	private static readonly Vector2 Sill = new(48f, 69f);
	private static readonly Vector2 StepFoot = new(48f, 93f);
	// The vent at the ridge her smoke leaks out of, authored (tithe_scale.py VENT).
	private static readonly Vector2 Louvre = new(48f, 18f);

	/// <summary>Pan <paramref name="side"/>'s bowl (-1 left, +1 right) in the authored scale cell, at frame <paramref name="frame"/>.</summary>
	public static Vector2 PanAuthored(int frame, int side)
	{
		float a = Mathf.DegToRad(Tilts[Math.Clamp(frame, 0, Tilts.Length - 1)]);
		float s = side < 0 ? -1f : 1f;
		return Pivot + new Vector2(s * Arm * Mathf.Cos(a), -s * Arm * Mathf.Sin(a)) + new Vector2(0f, Hang);
	}

	/// <summary>
	/// The frame for a difference between the pans, as a share of the price (positive: left heavier): level inside
	/// LevelBand, then a step of the beam at a quarter, a half, and past. Shared with RegressionChecks, so the beam
	/// the player reads and the rule that frees her are the same numbers.
	/// </summary>
	public static int FrameForDifference(float share)
	{
		float m = Mathf.Abs(share);
		if (m <= LevelBand)
			return LevelFrame;
		int step = m <= 0.25f ? 1 : m <= 0.5f ? 2 : 3;
		return share > 0f ? LevelFrame - step : LevelFrame + step;
	}

	// --- the run's state ------------------------------------------------------------------------------

	// The hold's share of HoldSeconds, 0 to 1. Named "progress" because _UiShot --win-event fills a float field of
	// that name and lets the real path run (the bar falls, the doors open and she steps out): a 1 here is a hold met.
	private float progress;
	private float price = 1f;
	private int levelAtArrival;
	// Seconds the beam has been level with the price met; back to 0 whenever it is not.
	private float levelHeld;
	// Seconds since the cap was met and the beam began settling itself, or below 0 before.
	private float settling = -1f;
	private float settleFrom;
	// Seconds since the pan was paid, or below 0 while it is still taking.
	private float paidFor = -1f;
	// The weigh beat (in-flight 61): seconds since a pan last took an orb, or below 0 before the first.
	private float paidFlash = -1f;
	private int paidFlashSide = 1;
	private int orbsFlashed;
	private bool succeeded;
	private float freedFor;
	private Vector2 yard;

	/// <summary>The price, in points of his bar, and what the pans hold. For the probe.</summary>
	public float Price => price;
	public float Paid => GranaryTithe.Open ? GranaryTithe.Taken : paidTotal;
	public float PaidLeft => GranaryTithe.Open ? GranaryTithe.TakenLeft : paidLeft;
	public float PaidRight => GranaryTithe.Open ? GranaryTithe.TakenRight : paidRight;
	public float PaidShare => Paid / price;
	public float Progress => progress;
	public bool PriceMet => Paid >= price * Full;
	public bool IsSettled => settling >= SettleSeconds;
	/// <summary>The pans within LevelBand of the price of each other (or settled by the cap).</summary>
	public bool IsLevel => IsSettled || Mathf.Abs(PaidLeft - PaidRight) <= LevelBand * price;
	/// <summary>Which pan is lighter now: -1 left, +1 right (left when even). For the probe's policy.</summary>
	public int LighterSide => PaidLeft <= PaidRight ? -1 : 1;
	public float LevelHeld => levelHeld;
	public int LevelAtArrival => levelAtArrival;
	public bool IsPaid => paidFor >= 0f;
	public int OrbsTaken => GranaryTithe.OrbsTaken;
	public int ValueTaken => GranaryTithe.ValueTaken;
	// What the pans held when the scale closed (GranaryTithe's own count resets with the next open).
	private float paidTotal, paidLeft, paidRight;

	/// <summary>Where the scale, its pans and the granary stand in the world. For the probe and _UiShot.</summary>
	public Vector2 ScaleFoot => yard + ScaleOffset;
	public Vector2 GranaryFoot => yard + GranaryOffset;
	public Vector2 PanPosition(int side) => PanWorld(ScaleFrame, side);

	/// <summary>Where to stand to photograph it: in front of the yard, between the scale and the doors.</summary>
	public Vector2 StandPoint => yard + new Vector2(40f, 120f);

	private Vector2 ScaleWorld(Vector2 authored) =>
		ScaleFoot + (authored - new Vector2(ScaleMiddle, ScaleFootRow)) * 2f;

	private Vector2 PanWorld(int frame, int side) => ScaleWorld(PanAuthored(frame, side));

	// The beam's frame: level once paid, or settled; otherwise the difference between the pans, eased toward level
	// while the cap's settle plays (the reeve taking the difference, seen).
	private int ScaleFrame
	{
		get
		{
			if (paidFor >= 0f || IsSettled)
				return LevelFrame;
			float share = (PaidLeft - PaidRight) / price;
			if (settling >= 0f)
				share = settleFrom * (1f - settling / SettleSeconds);
			return FrameForDifference(share);
		}
	}

	public override void PlaceSites(ExplorePlacer placer)
	{
		// A yard of open ground over its whole disc, swept for through the band as the hut's circle and the seal's
		// courtyard are: the granary and the scale must both stand on the stubble, never in a pond or a hedge.
		yard = AddSite(placer.EventSiteInBand(SiteFootprint, dense: true)).Position;
	}

	// --- what stands there ----------------------------------------------------------------------------

	private const string GranaryPath = "res://assets/bonelight/world/granary/granary.png";
	private const string ScalePath = "res://assets/bonelight/world/granary/scale.png";
	private const string HerPath = "res://assets/bonelight/characters/medea-1.png";

	private Node2D yardRoot;
	private Sprite2D granary;
	private Sprite2D scale;
	private Sprite2D her;

	protected override void OnPlaced()
	{
		TextureFilter = TextureFilterEnum.Nearest;
		yard = MainSite;
		Texture2D granaryTex = ResourceLoader.Exists(GranaryPath) ? GD.Load<Texture2D>(GranaryPath) : null;
		Texture2D scaleTex = ResourceLoader.Exists(ScalePath) ? GD.Load<Texture2D>(ScalePath) : null;
		Texture2D herTex = ResourceLoader.Exists(HerPath) ? GD.Load<Texture2D>(HerPath) : null;
		if (granaryTex == null || scaleTex == null || herTex == null)
		{
			GD.PushError("TitheScaleEvent: the granary's art is missing or not imported (python tools/art/tithe_scale.py, then import).");
			return;
		}
		yardRoot = new Node2D { Name = "GranaryYard", YSortEnabled = true };
		granary = Grounded("Granary", granaryTex, GranaryW, GranaryMiddle, GranaryFootRow, GranaryFoot);
		scale = Grounded("TitheScale", scaleTex, ScaleW, ScaleMiddle, ScaleFootRow, ScaleFoot);
		yardRoot.AddChild(granary);
		yardRoot.AddChild(scale);
		// Her, for the moment she steps out: the same sheet as her portrait (48 cell, feet on row 46).
		her = new Sprite2D
		{
			Name = "Her",
			Texture = herTex,
			Centered = false,
			Scale = new Vector2(2f, 2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Offset = new Vector2(-24f, WorldSprites.SortLift / 2f - 46f),
			Visible = false,
		};
		yardRoot.AddChild(her);
		PlaceFrames();
		// In the run's world at its origin, so its children stand at world positions; the event's sibling, so the
		// granary stays open and the beam down after the win.
		Game?.CallDeferred(Node.MethodName.AddChild, yardRoot);
	}

	// A sprite stood on its ground line and sorted at its foot (the hut's way): the node SortLift above the ground,
	// the texture's bottom row on it.
	private static Sprite2D Grounded(string name, Texture2D texture, int frameWidth, int middle, int footRow, Vector2 foot) => new()
	{
		Name = name,
		Texture = texture,
		Hframes = Math.Max(1, texture.GetWidth() / frameWidth),
		Centered = false,
		Scale = new Vector2(2f, 2f),
		TextureFilter = TextureFilterEnum.Nearest,
		Position = new Vector2(Mathf.Round(foot.X), Mathf.Round(foot.Y) - WorldSprites.SortLift),
		Offset = new Vector2(-middle, WorldSprites.SortLift / 2f - footRow),
	};

	// --- the rules -------------------------------------------------------------------------------------

	protected override void Begin()
	{
		yard = MainSite;
		// The price is set the moment he comes: one level's XP at that moment of the run.
		levelAtArrival = Player?.CurrentLevel ?? 1;
		price = Mathf.Max(1f, (Player?.XPToNextLevel ?? 10) * PriceInLevels);
		progress = 0f;
		levelHeld = 0f;
		settling = -1f;
		paidFlash = -1f;
		orbsFlashed = 0;
		GranaryTithe.OpenAt(ScaleFoot, PanWorld(LevelFrame, -1), PanWorld(LevelFrame, 1), TitheReach, price * CapInLevels);
		GD.Print($"TitheScaleEvent: the reeve's price is {price:0} (level {levelAtArrival}) between two pans, held level "
			+ $"within {LevelBand * 100f:0}% for {HoldSeconds:0} s; the scale takes what falls within {TitheReach:0} px, "
			+ $"never more than {price * CapInLevels:0}");
	}

	protected override void Tick(float delta)
	{
		// Each orb a pan takes flashes there (in-flight 61): the weigh, seen landing. Before the paid
		// branch, so the last orb's flash still plays into the bar's fall.
		if (GranaryTithe.OrbsTaken > orbsFlashed)
		{
			orbsFlashed = GranaryTithe.OrbsTaken;
			paidFlash = 0f;
			paidFlashSide = GranaryTithe.LastSide;
		}
		else if (paidFlash >= 0f)
			paidFlash += delta;
		if (paidFor >= 0f)
		{
			Paying(delta);
			return;
		}
		// _UiShot --win-event (or a hold met last frame): straight to the win.
		if (progress >= Full)
		{
			Release();
			return;
		}
		if (GranaryTithe.Open)
		{
			// The cap met: the reeve takes nothing more and the beam settles itself, the difference his (3.5).
			if (GranaryTithe.Capped && settling < 0f)
			{
				settling = 0f;
				settleFrom = (GranaryTithe.TakenLeft - GranaryTithe.TakenRight) / price;
				GD.Print($"TitheScaleEvent: the cap is met at {GranaryTithe.Taken:0}; the reeve keeps the difference and the beam settles");
			}
			else if (settling >= 0f && settling < SettleSeconds)
				settling = Mathf.Min(SettleSeconds, settling + delta);
			GranaryTithe.MovePans(PanWorld(ScaleFrame, -1), PanWorld(ScaleFrame, 1));
		}
		// The hold: only while the price is met AND the beam is level; anything else starts it over.
		levelHeld = PriceMet && IsLevel ? levelHeld + delta : 0f;
		progress = Mathf.Clamp(levelHeld / HoldSeconds, 0f, 1f);
		if (progress >= Full)
			Release();
		PlaceFrames();
	}

	// Paid and level: the scale stops taking (anything still in the air falls back to him), and the bar falls.
	private void Release()
	{
		progress = 1f;
		paidFor = 0f;
		CloseScale();
		SfxPlayer.Impact(Element.Metal, ScaleFoot);
		GD.Print($"TitheScaleEvent: held level - {PaidLeft:0} left, {PaidRight:0} right, {Paid:0} of {price:0} in "
			+ $"{GranaryTithe.OrbsTaken} orbs ({GranaryTithe.ValueTaken} XP){(IsSettled ? ", settled by the cap" : "")}");
		PlaceFrames();
	}

	// Closing keeps what the pans held, for the probe and the after-win frames: the next open resets the counts.
	private void CloseScale()
	{
		paidLeft = GranaryTithe.TakenLeft;
		paidRight = GranaryTithe.TakenRight;
		paidTotal = paidLeft + paidRight;
		GranaryTithe.Close();
	}

	// The beat after the pan is paid, seconds: the bar falls, the doors open, she steps out and down, she is free.
	private const float BarFallsAt = 0.45f;
	private const float DoorsOpenAt = 1.05f;
	private const float StepOutAt = 1.35f;
	private const float FreedAt = 2.2f;

	private void Paying(float delta)
	{
		float before = paidFor;
		paidFor += delta;
		if (before < BarFallsAt && paidFor >= BarFallsAt)
			SfxPlayer.Impact(Element.Earth, GranaryFoot);
		if (!succeeded && paidFor >= (PaysChest ? DoorsOpenAt + 0.2f : FreedAt))
		{
			succeeded = true;
			Succeed(GranaryWorld(StepFoot));
		}
		PlaceFrames();
	}

	protected override void OnStoodDown()
	{
		CloseScale();
		PlaceFrames();
	}

	public override void _ExitTree()
	{
		// Freed with the run, or stood down: nothing is weighed by a scale that is not there.
		if (GranaryTithe.Open)
			GranaryTithe.Close();
		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		// After the win the granary stays open and the beam down; she goes into the column of light.
		if (Succeeded && her != null && IsInstanceValid(her))
		{
			freedFor += (float)delta;
			PlaceFrames();
		}
	}

	private Vector2 GranaryWorld(Vector2 authored) =>
		GranaryFoot + (authored - new Vector2(GranaryMiddle, GranaryFootRow)) * 2f;

	private void PlaceFrames()
	{
		if (scale == null || !IsInstanceValid(scale))
			return;
		scale.Frame = Math.Clamp(ScaleFrame, 0, scale.Hframes - 1);
		int door = paidFor < BarFallsAt ? GranaryBarred : paidFor < DoorsOpenAt ? GranaryAjar : GranaryOpen;
		if (granary != null && IsInstanceValid(granary))
			granary.Frame = Math.Clamp(door, 0, granary.Hframes - 1);
		UpdateHer();
	}

	// She comes out at the sill and down the steps toward him, and once she is free goes into the light.
	private void UpdateHer()
	{
		if (her == null || !IsInstanceValid(her))
			return;
		if (PaysChest || paidFor < StepOutAt)
		{
			her.Visible = false;
			return;
		}
		float t = Mathf.Clamp((paidFor - StepOutAt) / (FreedAt - StepOutAt), 0f, 1f);
		float eased = t * t * (3f - 2f * t);
		Vector2 sill = GranaryWorld(Sill);
		Vector2 foot = GranaryWorld(StepFoot) + new Vector2(0f, 22f);
		Vector2 at = sill.Lerp(foot, eased);
		// Sorted just in front of the granary (by a node a pixel below its foot), drawn where she stands.
		float sortY = GranaryFoot.Y + 2f;
		her.Visible = true;
		her.Position = new Vector2(Mathf.Round(at.X), sortY - WorldSprites.SortLift);
		her.Offset = new Vector2(-24f, WorldSprites.SortLift / 2f - 46f + Mathf.Round(at.Y - sortY) / 2f);
		float a = Mathf.Clamp(t * 3f, 0f, 1f);
		if (Succeeded)
			a *= Mathf.Clamp(1f - freedFor / 1.2f, 0f, 1f);
		her.Modulate = new Color(1f, 1f, 1f, a);
		if (Succeeded && freedFor >= 1.2f)
		{
			her.QueueFree();
			her = null;
		}
	}

	// --- drawing ---------------------------------------------------------------------------------------

	// On the ground while the scale is taking: the edge of its reach, SPLIT by a line down through the post (3.5,
	// "how it reads"), each half filling from the bottom as its own pan does, toward half the price - so which side a
	// kill pays, and which side is light, is never a guess. Brighter while he is inside. The orbs' own flight is
	// dressed in DrawLight: the bare orb alone read as one more drifting orb (Eric, 2026-11-06; in-flight 61).
	public override void _Draw()
	{
		if (Player == null || IsDormant || IsResolved || paidFor >= 0f || !NearPlayer(ScaleFoot))
			return;
		bool inside = PlayerWithin(ScaleFoot, TitheReach);
		Vector2 c = ToLocal(ScaleFoot);
		Color track = new(TellColor, inside ? 0.42f : 0.26f);
		// The track, dashed, and the split: a dashed line down through the post, top to bottom of the reach.
		const int dashes = 40;
		float dash = Mathf.Tau / dashes;
		for (int k = 0; k < dashes; k++)
			DrawArc(c, TitheReach, k * dash, k * dash + dash * 0.55f, 4, track, 2f);
		for (float y = -TitheReach; y < TitheReach; y += 26f)
			DrawLine(c + new Vector2(0f, y), c + new Vector2(0f, Mathf.Min(y + 14f, TitheReach)), track, 2f);
		// Each half fills from the bottom up toward the top (pi/2 is straight down in screen space), a half-ring
		// being half the price: a level, met fairly, is both halves full.
		float half = price * 0.5f;
		float fillL = Mathf.Clamp(PaidLeft / half, 0f, 1f);
		float fillR = Mathf.Clamp(PaidRight / half, 0f, 1f);
		Color fill = new(TellColor, 0.9f);
		if (fillL > 0f)
			DrawArc(c, TitheReach, Mathf.Pi / 2f, Mathf.Pi / 2f + Mathf.Pi * fillL, 32, fill, 3f);
		if (fillR > 0f)
			DrawArc(c, TitheReach, Mathf.Pi / 2f - Mathf.Pi * fillR, Mathf.Pi / 2f, 32, fill, 3f);
	}

	// Her smoke, always: a thin cold herb-smoke leaking from the granary's louvre and drifting low over the yard,
	// her far tell from the first second. Running, the pan's glow as well, rising with what it holds - what he has
	// given up, held in the reeve's iron.
	protected override void DrawLight(CanvasItem light)
	{
		Vector2 louvre = GranaryWorld(Louvre);
		if (!NearPlayer(louvre) || (IsResolved && freedFor > 1.5f))
			return;
		if (IsDormant)
			DrawFarTell(light, louvre, TellColor, Flicker(3));
		else if (!IsResolved && paidFor < 0f)
			DrawFarTell(light, louvre, TellColor, 0.35f * Flicker(3));
		float smoke = paidFor >= 0f ? Mathf.Clamp(1f - paidFor / 2f, 0f, 1f) : 1f;
		if (smoke > 0f)
			DrawSmoke(light, ToLocal(louvre), smoke);
		if (!IsDormant && !IsResolved)
		{
			float fade = paidFor >= 0f ? Mathf.Clamp(1f - paidFor / 1.5f, 0f, 1f) : 1f;
			for (int side = -1; side <= 1; side += 2)
			{
				// Each pan glows with what it holds, toward half the price.
				Vector2 pan = ToLocal(PanWorld(ScaleFrame, side));
				float held = fade * Mathf.Clamp((side < 0 ? PaidLeft : PaidRight) / (price * 0.5f), 0f, 1f);
				ExploreArt.DrawGlow(light, pan, 24f + 30f * held, new Color(TellColor, (0.08f + 0.26f * held) * Flicker(6 + side)));
				ExploreArt.DrawGlow(light, pan, 8f + 7f * held, new Color(TellColor.Lerp(Colors.White, 0.5f), 0.2f + 0.3f * held));
				if (paidFor < 0f)
					DrawDrawnIn(light, pan, side);
			}
			// THE LEVEL MARK (3.5): lit while the price is met and the beam is inside the band, brightening as the
			// hold runs - the one place to look while holding it. Dim before the price is met.
			Vector2 mark = ToLocal(ScaleWorld(LevelMark));
			bool holding = paidFor < 0f && PriceMet && IsLevel;
			float markGlow = paidFor >= 0f ? fade : holding ? 0.45f + 0.55f * progress : 0.12f;
			ExploreArt.DrawGlow(light, mark, 10f + 16f * markGlow, new Color(TellColor, 0.6f * markGlow * Flicker(4)));
			if (holding)
				light.DrawArc(mark, 14f, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * progress, 24,
					new Color(TellColor.Lerp(Colors.White, 0.5f), 0.85f), 2.5f);
			// The reeve taking the difference, seen: a smoke of light lifted off the heavier pan while it settles.
			if (settling >= 0f && settling < SettleSeconds && paidFor < 0f)
			{
				float u = settling / SettleSeconds;
				Vector2 heavy = ToLocal(PanWorld(ScaleFrame, settleFrom > 0f ? -1 : 1));
				for (int k = 0; k < 4; k++)
					ExploreArt.DrawGlow(light, heavy + new Vector2((k - 1.5f) * 9f, -30f * u - k * 7f),
						14f - 8f * u, new Color(TellColor, 0.32f * (1f - u)));
			}
			// THE PAYMENT, SEEN (Eric, 2026-11-06; in-flight 61): each orb on its way tethered to ITS pan under a
			// stream of motes, and a flash as each is weighed - the XP is visibly sucked into the machine, so the
			// ask is legible without the objective line's words.
			if (paidFor < 0f)
			{
				int flights = GranaryTithe.Flights;
				for (int i = 0; i < flights; i++)
				{
					Vector2 orb = ToLocal(GranaryTithe.FlightPosition(i));
					Vector2 pan = ToLocal(PanWorld(ScaleFrame, GranaryTithe.FlightSide(i)));
					light.DrawLine(orb, pan, new Color(TellColor, 0.12f), 2f);
					// Three motes flowing up the tether, shrinking as the pan takes them in
					// (the pixel-pass rule: size, never alpha fade alone).
					for (int k = 0; k < 3; k++)
					{
						float u = (Clock * 1.5f + k / 3f + i * 0.37f) % 1f;
						ExploreArt.DrawGlow(light, orb.Lerp(pan, u), 11f - 6f * u, new Color(TellColor, 0.30f - 0.10f * u));
					}
				}
			}
			if (paidFlash >= 0f && paidFlash < PayFlashSeconds)
			{
				// The weigh: a burst at the pan that took it and a ring closing onto it - taken, counted.
				Vector2 pan = ToLocal(PanWorld(ScaleFrame, paidFlashSide));
				float u = paidFlash / PayFlashSeconds;
				ExploreArt.DrawGlow(light, pan, 24f + 26f * u, new Color(TellColor, 0.45f * (1f - u)));
				light.DrawArc(pan, 8f + 26f * (1f - u), 0f, Mathf.Tau, 24,
					new Color(TellColor.Lerp(Colors.White, 0.6f), 0.8f * (1f - u * 0.5f)), 2.5f);
			}
		}
	}

	/// <summary>How long each weighed orb's flash at the pan lasts.</summary>
	private const float PayFlashSeconds = 0.4f;

	// The machine's pull, on show from the first second it takes (in-flight 61): faint motes drawn up
	// out of the yard's air into the pan, so what the scale does is read off the scale itself before
	// the first orb falls. From the clock alone, the smoke's way: nothing kept, nothing allocated.
	private void DrawDrawnIn(CanvasItem light, Vector2 pan, int side)
	{
		const int motes = 4;
		const float period = 2.8f;
		for (int i = 0; i < motes; i++)
		{
			float t = (Clock / period + i / (float)motes + (side < 0 ? 0f : 0.5f / motes)) % 1f;
			// Each mote comes in on its own slowly wandering bearing, from ITS side of the post only (each pan
			// pulls from its own half of the yard), flattened toward the ground and starting low off the
			// stubble, so the pull reads as up-and-in rather than a halo.
			float bearing = (side < 0 ? Mathf.Pi : 0f) + (i - 1.5f) * 0.45f + Mathf.Sin(Clock * 0.13f + i * 2.1f) * 0.3f;
			float r = (1f - t) * 120f;
			Vector2 p = pan + new Vector2(Mathf.Cos(bearing) * r, Mathf.Sin(bearing) * r * 0.45f + (1f - t) * 46f);
			float fade = Mathf.Sin(t * Mathf.Pi);
			ExploreArt.DrawGlow(light, p, 3f + 7f * (1f - t), new Color(TellColor, 0.14f * fade));
		}
	}

	// Six puffs, each its own phase, rising a little out of the louvre and then lying down and drifting off over the
	// stubble on the wind, swelling and thinning as they go. From the clock alone: nothing is kept, nothing allocated.
	private void DrawSmoke(CanvasItem light, Vector2 at, float strength)
	{
		const int puffs = 6;
		const float period = 5.5f;
		for (int i = 0; i < puffs; i++)
		{
			float t = (Clock / period + i / (float)puffs) % 1f;
			float rise = Mathf.Min(t, 0.25f) / 0.25f;
			Vector2 p = at + new Vector2(t * 230f + Mathf.Sin(Clock * 0.9f + i * 1.7f) * 10f,
				-26f * rise + t * 60f + Mathf.Sin(Clock * 1.3f + i) * 4f);
			float fade = Mathf.Sin(t * Mathf.Pi);
			ExploreArt.DrawGlow(light, p, 16f + 40f * t, new Color(TellColor, 0.16f * fade * strength));
		}
	}
}
