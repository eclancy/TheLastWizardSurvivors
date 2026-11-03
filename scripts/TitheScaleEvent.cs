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
	/// <summary>True while a scale is taking: orbs that fall inside its reach drift into its pan.</summary>
	public static bool Open { get; private set; }

	/// <summary>Where the pan hangs, in world space: what a tithed orb flies to.</summary>
	public static Vector2 Pan { get; private set; }

	/// <summary>The middle of its reach: the scale's foot.</summary>
	public static Vector2 Centre { get; private set; }

	public static float Reach { get; private set; }

	/// <summary>What the pan holds since it opened, in points of his bar at the moment each orb came in.</summary>
	public static float Taken { get; private set; }

	/// <summary>Orbs weighed, and the XP they carried, since it opened. For the probe.</summary>
	public static int OrbsTaken { get; private set; }
	public static int ValueTaken { get; private set; }

	/// <summary>Orbs on their way to the pan right now. Kept by XPOrb.</summary>
	public static int OrbsInFlight { get; set; }

	public static void OpenAt(Vector2 centre, Vector2 pan, float reach)
	{
		Open = true;
		Centre = centre;
		Pan = pan;
		Reach = reach;
		Taken = 0f;
		OrbsTaken = 0;
		ValueTaken = 0;
		OrbsInFlight = 0;
	}

	/// <summary>The pan hangs somewhere else now (the beam has moved as it filled).</summary>
	public static void MovePan(Vector2 pan) => Pan = pan;

	public static void Close()
	{
		Open = false;
		OrbsInFlight = 0;
	}

	/// <summary>Whether an orb landing at <paramref name="at"/> is the reeve's.</summary>
	public static bool Claims(Vector2 at) => Open && at.DistanceSquaredTo(Centre) <= Reach * Reach;

	/// <summary>An orb has reached the pan: <paramref name="worth"/> points of his bar, carrying <paramref name="value"/>.</summary>
	public static void Pay(float worth, int value)
	{
		if (!Open)
			return;
		Taken += Math.Max(0f, worth);
		OrbsTaken++;
		ValueTaken += Math.Max(0, value);
	}
}

// The Fallow Fields' second side event: the Tithe-Reeve's scale (.ai/chapter-wizards.md 4.2).
//
// "He takes a tithe of everything that grows here." He counts Medea among what grows: she is held in the reeve's
// granary against a tithe the village could never pay. The reeve's great scale stands in the granary yard, its door
// barred, and the bar is held by the scale's beam.
//
// What it asks: PAY. The only ask in the game that costs the player something. From the moment he arrives, every XP
// orb that falls within TitheReach of the scale drifts up into its pan instead of to him (GranaryTithe, XPOrb). The
// price is one level's XP at that moment of the run: the whole of his bar at the level he arrived at
// (Player.XPToNextLevel), and each orb is weighed at what it would have been worth to him when it reached the pan
// (Player.XpWorthOf, the run's pace in it) - so the price is a level, whenever he comes. When the pan holds it, the
// beam drops, the bar falls from the granary door, the doors open and she steps out.
//
// NO FAILURE, ONLY A PRICE. Walking off keeps what is paid: the pan never empties, the arrow stays on the scale, and
// whatever falls near it later goes on the count - nothing drains, nothing locks, nothing comes at him for it. There
// is no ring of enemies either: what pays the tithe is the field's own crowd, killed where the reeve can count it.
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

	// A paid pan reads as paid a hair under: a float that has carried the whole price is not always exactly 1.
	private const float Full = 0.999f;

	protected override string Announcement => "A great scale in the granary yard. What falls near it is weighed.";
	protected override string ChestLine => "The tithe is paid - a chest was left in the granary";
	protected override string Objective => IsResolved || paidFor >= 0f ? string.Empty
		: $"Pay the reeve's tithe  ({Mathf.FloorToInt(Mathf.Min(progress, 1f) * 100f)}%)";
	// The pan, until it is paid: the arrow says where the tithe goes.
	protected override Vector2? PointerTarget => IsResolved || paidFor >= 0f ? null : PanWorld(ScaleFrame);

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
	// with the TITHE (left) pan down. Frames 0-4 fill toward level; frame 5 is the beam dropped.
	private static readonly Vector2 Pivot = new(32f, 13f);
	private const float Arm = 24f;
	private const float Hang = 27f;
	private static readonly float[] Tilts = { -15f, -11.5f, -8f, -4.5f, -1.5f, 21f };
	private const int DroppedFrame = 5;

	/// <summary>The sheets' frame counts, for RegressionChecks.ValidateTitheScale.</summary>
	public const int ScaleFrames = 6;
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

	/// <summary>The pan's bowl in the authored scale cell, at frame <paramref name="frame"/>.</summary>
	public static Vector2 PanAuthored(int frame)
	{
		float a = Mathf.DegToRad(Tilts[Math.Clamp(frame, 0, Tilts.Length - 1)]);
		return Pivot + new Vector2(-Arm * Mathf.Cos(a), Arm * Mathf.Sin(a)) + new Vector2(0f, Hang);
	}

	// --- the run's state ------------------------------------------------------------------------------

	// The pan's share of the price, 0 to 1. Named "progress" because _UiShot --win-event fills a float field of that
	// name and lets the real path run (the beam drops, the bar falls, the doors open and she steps out).
	private float progress;
	private float price = 1f;
	private int levelAtArrival;
	// Seconds since the pan was paid, or below 0 while it is still taking.
	private float paidFor = -1f;
	private bool succeeded;
	private float freedFor;
	private Vector2 yard;

	/// <summary>The price, in points of his bar, and what the pan holds. For the probe.</summary>
	public float Price => price;
	public float Paid => progress * price;
	public float Progress => progress;
	public int LevelAtArrival => levelAtArrival;
	public bool IsPaid => paidFor >= 0f;
	public int OrbsTaken => GranaryTithe.OrbsTaken;
	public int ValueTaken => GranaryTithe.ValueTaken;

	/// <summary>Where the scale, its pan and the granary stand in the world. For the probe and _UiShot.</summary>
	public Vector2 ScaleFoot => yard + ScaleOffset;
	public Vector2 GranaryFoot => yard + GranaryOffset;
	public Vector2 PanPosition => PanWorld(ScaleFrame);

	/// <summary>Where to stand to photograph it: in front of the yard, between the scale and the doors.</summary>
	public Vector2 StandPoint => yard + new Vector2(40f, 120f);

	private Vector2 PanWorld(int frame) =>
		ScaleFoot + (PanAuthored(frame) - new Vector2(ScaleMiddle, ScaleFootRow)) * 2f;

	private int ScaleFrame => paidFor >= 0f ? DroppedFrame : Math.Clamp(Mathf.FloorToInt(progress * DroppedFrame), 0, DroppedFrame - 1);

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
		GranaryTithe.OpenAt(ScaleFoot, PanWorld(0), TitheReach);
		GD.Print($"TitheScaleEvent: the reeve's price is {price:0} (level {levelAtArrival}); the pan takes what falls within {TitheReach:0} px");
	}

	protected override void Tick(float delta)
	{
		if (paidFor >= 0f)
		{
			Paying(delta);
			return;
		}
		if (GranaryTithe.Open)
		{
			progress = Mathf.Max(progress, GranaryTithe.Taken / price);
			GranaryTithe.MovePan(PanWorld(ScaleFrame));
		}
		if (progress >= Full)
		{
			// Paid: the scale stops taking (anything still in the air falls back to him), and the beam drops.
			progress = 1f;
			paidFor = 0f;
			GranaryTithe.Close();
			SfxPlayer.Impact(Element.Metal, ScaleFoot);
			GD.Print($"TitheScaleEvent: paid {Paid:0} of {price:0} in {GranaryTithe.OrbsTaken} orbs ({GranaryTithe.ValueTaken} XP)");
		}
		PlaceFrames();
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
		GranaryTithe.Close();
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

	// On the ground while the scale is taking: the edge of its reach, filling as the pan does, brighter while he is
	// inside it - so where a kill pays the reeve is never a guess. Between the pan and each orb on its way, nothing:
	// the orb is seen going.
	public override void _Draw()
	{
		if (Player == null || IsDormant || IsResolved || paidFor >= 0f || !NearPlayer(ScaleFoot))
			return;
		bool inside = PlayerWithin(ScaleFoot, TitheReach);
		DrawProgressArc(ScaleFoot, TitheReach, progress, TellColor, 3f, inside ? 0.42f : 0.26f);
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
			Vector2 pan = ToLocal(PanWorld(ScaleFrame));
			float held = paidFor >= 0f ? Mathf.Clamp(1f - paidFor / 1.5f, 0f, 1f) : progress;
			ExploreArt.DrawGlow(light, pan, 30f + 40f * held, new Color(TellColor, (0.10f + 0.3f * held) * Flicker(6)));
			ExploreArt.DrawGlow(light, pan, 10f + 8f * held, new Color(TellColor.Lerp(Colors.White, 0.5f), 0.25f + 0.35f * held));
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
