using Godot;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// The Last Bridge's side event: the barge (.ai/chapter-wizards.md 4.9).
//
// "His seat is across the water." Morgan le Fay - the enchantress of Avalon, the queen in the black barge who
// carried the dying king over the water - is the one person who could take anyone across without his bridge.
// So he keeps her under it: her barge moored beneath the span, its one lantern lit, and her standing in it.
//
// What it asks: KEEP PACE. Come level with the barge and it casts off, drifting under the span toward the gate -
// slower than he walks, faster than the span falls. Stay level with it (within Reach of it ALONG the bridge,
// anywhere across the deck) for the whole of its drift, and at its landing she comes ashore. Fall behind or run
// ahead and its lantern dims; out of reach OutOfReachSeconds and it drifts back to its mooring, to be met again.
// The nearest thing in the game is the caravan, which is caught; the barge must be accompanied.
//
// AGAINST THE FALL (endgame-castle.md 3.3). The span falls in behind the player at BridgeRules.FrontSpeed, about
// 31 px a pace second. The design's first guess drifted 1,200 px in 50 s - 24 px a second, SLOWER than the fall,
// so a player who kept pace from an arrival with the front a few hundred pixels behind him was walked into the
// drop by the event itself, and whether it could be won at all depended on when he got there (the pace clock,
// which no event may be timed by). The drift is 2,000 px in 50 s now, 40 px a second (DriftSpeed): every second
// of it puts 9 px between him and the fall, so from any arrival he is ahead of, keeping pace keeps him ahead.
// ValidateBarge holds it above the front's speed.
//
// NOTHING LOCKS. The barge is on the water, not the deck, so the fall cannot take it - but it can take the deck
// beside it. A barge whose mooring the fall has reached slips it and waits RestLead ahead of the fall instead
// (RestS), so whoever comes late still finds it on ground he can stand on, and its arrow and hints follow it
// there (PointOfInterest.DriftTo). A drift that fails drifts back to the same place.
//
// WHERE IT IS (the bridge's band rules, not the explore bands): moored under the second or the third span, on one
// side of it, from the seed - far enough ahead of the start that the walk to it is a real part of the crossing,
// and with the whole of its drift inside the span. It rides hard under the shore on its side (TrackAcross), and
// the deck level with it is where the player must be.
//
// THE BARGE IS NOT A BODY. Nothing placed here joins "enemies". It is a sprite on the water, sorted with the
// bodies at its waterline (so a south-east barge is in front of its parapet and a north-west one behind it), and
// its light is on the additive layer every event has. The gate's dogs come down the deck at its level while it
// drifts (the chapter's Fast role, ordinary bodies): the pressure, as every event has.
//
// Art: tools/art/barge.py (world/barge/): the barge with her in it, the same barge empty, and its pictogram.
public partial class BargeEvent : SideEvent
{
	// --- the numbers ----------------------------------------------------------------------------------

	/// <summary>How far one drift carries the barge, px along the bridge: about a third of a span.</summary>
	public const float DriftLength = 2000f;

	/// <summary>Its drift, px a second: slower than his walk (220), faster than the span falls (about 31).</summary>
	public const float DriftSpeed = 40f;

	/// <summary>Level with it: within this along the bridge, anywhere across the deck.</summary>
	public const float Reach = 260f;

	/// <summary>Out of reach this long, continuously, and it drifts back to its mooring.</summary>
	public const float OutOfReachSeconds = 6f;

	/// <summary>How fast it drifts back after a failed drift, px a second (a visible drift, not a jump).</summary>
	public const float ReturnSpeed = 220f;

	/// <summary>Once the fall has reached its mooring, it waits this far ahead of the fall: a player level with it
	/// at the back of its reach still stands on whole deck.</summary>
	public const float RestLead = 560f;

	/// <summary>
	/// It begins when he is this near it along the bridge, and within <see cref="ArrivalRadius"/> of it: a step
	/// beyond its reach, so its line is up before it casts off - and near enough that on a phone's narrow canvas
	/// it is on the screen when it begins (at 480 a player at the parapet arrived with the barge off the edge).
	/// </summary>
	public const float ArrivalAlong = 400f;

	// Where it may moor in its span: this far past the span's start, and its landing this far short of its end.
	public const float MooringFromSpanStart = 600f;
	public const float LandingBeforeSpanEnd = 400f;

	// The spans it may be moored under (BridgeBand.Find(Span, i)): the second and the third. Under the first it is
	// found by everyone in the first minute; under the fourth, the last before the gate yard, the fall can be at
	// its mooring before most players reach it.
	public static readonly int[] MooringSpans = { 1, 2 };

	// Its hull's middle this far beyond the shore on its side: hard under the parapet, its prow and lantern clear
	// of it. Further on the north-west side, where the parapet stands between it and the viewer.
	public const float HullOffsetSouthEast = 70f;
	public const float HullOffsetNorthWest = 92f;
	// Eased over a jog (a gatehouse's narrows, a yard's widening) across this much of the bridge either side.
	private const float TrackEase = 240f;

	// The gate's dogs while it drifts: a pair every HoundSeconds, from this far ahead of it.
	[Export] public float HoundSeconds { get; set; } = 7f;
	[Export] public int HoundsPerPack { get; set; } = 2;
	private const float HoundsAhead = 1050f;

	// --- where it is ------------------------------------------------------------------------------------

	private Vector2 origin;
	private int side = 1;            // +1 the south-east shore, -1 the north-west
	private float mooringS;

	/// <summary>Which side of the bridge it is moored on: +1 south-east (bottom right), -1 north-west (top left).</summary>
	public int Side => side;

	/// <summary>Its mooring, px along the bridge.</summary>
	public float MooringS => mooringS;

	/// <summary>Where it is now, px along the bridge.</summary>
	public float BargeS => bargeS;

	/// <summary>Where it is now, in the world.</summary>
	public Vector2 BargeWorld => WorldOnTrack(bargeS);

	/// <summary>
	/// Moored under the second or the third span, on one side, from the seed: three rolls, drawn in a fixed order
	/// from the event's own stream, so the same seed moors it in the same place (the probe checks it).
	/// </summary>
	public override void PlaceSites(ExplorePlacer placer)
	{
		float spanRoll = placer.NextFloat();
		float sideRoll = placer.NextFloat();
		float alongRoll = placer.NextFloat();
		origin = placer.Origin;
		BridgeBand.Piece span = BridgeBand.Find(BridgeBand.PieceKind.Span, MooringSpans[Mathf.Min(MooringSpans.Length - 1, (int)(spanRoll * MooringSpans.Length))]);
		side = sideRoll < 0.5f ? -1 : 1;
		mooringS = Mathf.Lerp(span.S0 + MooringFromSpanStart, span.S1 - DriftLength - LandingBeforeSpanEnd, alongRoll);
		bargeS = mooringS;
		AddSite(placer.PlacedByChapter(WorldOnTrack(mooringS), SiteFootprint));
	}

	/// <summary>
	/// Across the bridge, px from its axis, where a barge rides at <paramref name="s"/>: hard under the shore on
	/// <paramref name="side"/>, never nearer the axis than a span's shore (so it passes outside a gatehouse's
	/// towers, under the arch), out along a yard's wider shore, and eased over each jog.
	/// </summary>
	public static float TrackAcross(float s, int side)
	{
		float edge = 0.25f * Edge(s - TrackEase) + 0.5f * Edge(s) + 0.25f * Edge(s + TrackEase);
		return side * (edge + (side < 0 ? HullOffsetNorthWest : HullOffsetSouthEast));
	}

	private static float SpanHalfWidth => BridgeBand.Find(BridgeBand.PieceKind.Span, 0).HalfWidthPx;

	private static float Edge(float s) => Mathf.Max(BridgeBand.HalfWidthAt(s), SpanHalfWidth);

	private Vector2 WorldOnTrack(float s) => origin + BridgeBand.LocalAt(s, TrackAcross(s, side));

	// A point on the deck level with it, a little in from its shore: where the reward and the arrow aim.
	private Vector2 DeckBeside(float s) => origin + BridgeBand.LocalAt(s, side * Mathf.Max(0f, BridgeBand.HalfWidthAt(s) - 90f));

	/// <summary>
	/// A point on the deck <paramref name="along"/> px along the bridge from the barge (negative: behind it, toward
	/// the start), <paramref name="fromShore"/> px in from the shore on its side. For _UiShot and the probe.
	/// </summary>
	public Vector2 StandPoint(float along = 0f, float fromShore = 150f)
	{
		float s = bargeS + along;
		return origin + BridgeBand.LocalAt(s, side * Mathf.Max(0f, BridgeBand.HalfWidthAt(s) - fromShore));
	}

	// --- what it is doing ---------------------------------------------------------------------------------

	private enum Phase { Moored, Drifting, Returning, Landed }

	private Phase phase = Phase.Moored;
	private float bargeS;
	private float castOffS;
	private float outOfReach;
	private float lantern = 1f;      // 0..1, eased: full while he is level with it, low and guttering while not
	private float houndTimer;
	private float freedFor;

	/// <summary>Drifting now (for the probe and _UiShot).</summary>
	public bool IsDrifting => phase == Phase.Drifting;

	/// <summary>Drifting back after a failed drift (for the probe).</summary>
	public bool IsReturning => phase == Phase.Returning;

	/// <summary>How far into its drift it is, 0 to 1 (0 while moored or returning).</summary>
	public float Progress => phase == Phase.Drifting || phase == Phase.Landed
		? Mathf.Clamp((bargeS - castOffS) / DriftLength, 0f, 1f) : 0f;

	/// <summary>Seconds he has been out of reach in this drift (for the probe).</summary>
	public float SecondsOutOfReach => outOfReach;

	/// <summary>Drifts that failed and drifted back (for the probe).</summary>
	public int DriftsFailed { get; private set; }

	/// <summary>Whether he is level with it now.</summary>
	public bool PlayerLevel => Player != null && Mathf.Abs(AlongOf(Player.GlobalPosition) - bargeS) <= Reach;

	private BridgeRules Rules => Game?.Rules as BridgeRules;

	private float AlongOf(Vector2 world) => BridgeBand.AlongOf(world - origin);

	/// <summary>
	/// Where it waits: its mooring - or, once the fall has reached that, RestLead ahead of the fall, and never so
	/// far along that a whole drift would not fit before the castle wall.
	/// </summary>
	private float RestS()
	{
		float rest = mooringS;
		BridgeRules rules = Rules;
		if (rules != null && rules.FrontMoving)
			rest = Mathf.Max(rest, rules.FrontS + RestLead);
		return Mathf.Min(rest, BridgeBand.End - DriftLength - 300f);
	}

	protected override string Announcement => "A barge under the span, one lantern lit. It waits for someone.";
	protected override string ChestLine => "The barge comes ashore empty - a chest was left in it";
	protected override string Objective => phase switch
	{
		Phase.Drifting when !PlayerLevel => "The lantern is going out - keep level with the barge",
		Phase.Drifting => $"Keep level with the barge to its landing  ({Mathf.CeilToInt((castOffS + DriftLength - bargeS) / DriftSpeed)} s)",
		Phase.Returning => "The barge drifts back to its mooring",
		_ => "Come level with the barge",
	};
	// At the barge while it waits or drifts back, and while he is out of reach of it; nothing while he keeps
	// pace - it is beside him, and the arrow would sit on the edge of the screen it is already on.
	protected override Vector2? PointerTarget =>
		IsResolved || Player == null || (phase == Phase.Drifting && PlayerLevel) ? null : DeckBeside(bargeS);
	protected override string SiteGlyph => "barge";
	public override bool PlacedByChapterRules => true;
	protected override float SiteFootprint => 120f;
	protected override float ArrivalRadius => 760f;
	// The strait's mist: wind's ramp at its body stop, the colour her lantern and the hint's mist wear
	// (tools/art/barge.py, hint_cues.py). Cold, because he keeps her.
	protected override Color TellColor => new Color(0.706f, 0.933f, 0.878f);   // wind mid #B4EEE0

	protected override bool HasArrived() =>
		Player != null && Mathf.Abs(AlongOf(Player.GlobalPosition) - bargeS) <= ArrivalAlong
		&& Player.GlobalPosition.DistanceTo(BargeWorld) <= ArrivalRadius;

	// --- what stands there --------------------------------------------------------------------------------

	private const string ArtDir = "res://assets/bonelight/world/barge/";
	// tools/art/barge.py: an 88x88 cell, four frames (the lantern breathing, the ripples sliding under the hull, the
	// barge lifting on the swell). The waterline under the hull's middle is at (44, 63): the node stands there, so it
	// sorts at its waterline. The lantern hangs at (76, 15) from the curl of the prow post. barge.py prints both.
	public const int CellW = 88;
	public const int CellH = 88;
	public const int Frames = 4;
	private static readonly Vector2 Anchor = new(44f, 63f);
	private static readonly Vector2 LanternCell = new(76f, 15f);
	/// <summary>The lantern, world px from the barge's waterline (the art is drawn at x2).</summary>
	public static readonly Vector2 LanternOffset = (LanternCell - Anchor) * 2f;
	private const float FrameSeconds = 0.22f;

	private Node2D holder;
	private Sprite2D full;
	private Sprite2D empty;
	private Texture2D fullTex;

	protected override void OnPlaced()
	{
		fullTex = Load("barge-full.png");
		Texture2D emptyTex = Load("barge-empty.png");
		if (fullTex == null || emptyTex == null)
		{
			GD.PushError("BargeEvent: the barge's art is missing or not imported (python tools/art/barge.py, then import).");
			return;
		}
		holder = new Node2D { Name = "Barge", YSortEnabled = true };
		var at = new Node2D { Name = "AtWaterline" };
		empty = Layer("Empty", emptyTex);
		full = Layer("Full", fullTex);
		at.AddChild(empty);
		at.AddChild(full);
		holder.AddChild(at);
		empty.Visible = false;
		// In the run's world, sorted with the bodies, like the anvil's forge: a sibling of the event.
		Game?.CallDeferred(Node.MethodName.AddChild, holder);
		PlaceBarge();
	}

	private static Sprite2D Layer(string name, Texture2D texture) => new()
	{
		Name = name,
		Texture = texture,
		Hframes = Frames,
		Centered = false,
		Scale = new Vector2(2f, 2f),
		TextureFilter = TextureFilterEnum.Nearest,
		Offset = -Anchor,
	};

	private static Texture2D Load(string file)
	{
		string path = ArtDir + file;
		return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
	}

	// The holder's one child stands at the barge, on whole pixels; the holder stays at the origin.
	private void PlaceBarge()
	{
		if (holder == null || !IsInstanceValid(holder) || holder.GetChildCount() == 0)
			return;
		var at = holder.GetChild<Node2D>(0);
		at.Position = BargeWorld.Round();
		int frame = (int)(Clock / FrameSeconds) % Frames;
		if (full != null) full.Frame = frame;
		if (empty != null) empty.Frame = frame;
	}

	public override void _ExitTree()
	{
		if (holder != null && IsInstanceValid(holder))
			holder.QueueFree();
		holder = null;
		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		float dt = (float)delta;
		if (Succeeded)
		{
			// She steps out into the column of light, and the barge she was kept on thins to mist: it was always
			// partly a mirage.
			freedFor += dt;
			if (full != null && IsInstanceValid(full))
			{
				full.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(1f - freedFor / 1.2f, 0f, 1f));
				if (empty != null)
				{
					empty.Visible = true;
					empty.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(1f - (freedFor - 1.2f) / 2.4f, 0f, 1f));
				}
			}
		}
		PlaceBarge();
	}

	// --- the rules -----------------------------------------------------------------------------------------

	// Before anyone comes: it waits at its mooring, or - once the fall has reached it - ahead of the fall, and the
	// arrow and the hints follow it there.
	protected override void DormantTick(float delta)
	{
		bargeS = Mathf.MoveToward(bargeS, RestS(), ReturnSpeed * delta);
		if (Sites.Count > 0)
			Sites[0].DriftTo(BargeWorld);
		lantern = Mathf.MoveToward(lantern, 1f, delta);
	}

	protected override void Begin()
	{
		phase = Phase.Moored;
		houndTimer = HoundSeconds * 0.5f;
		SfxPlayer.AtPosition(SfxCatalog.HintHullKnock, BargeWorld, 0f);
	}

	protected override void Tick(float delta)
	{
		if (Player == null)
			return;
		bool level = PlayerLevel;
		switch (phase)
		{
			case Phase.Moored:
				bargeS = Mathf.MoveToward(bargeS, RestS(), ReturnSpeed * delta);
				lantern = Mathf.MoveToward(lantern, 0.75f, delta);
				if (level)
				{
					phase = Phase.Drifting;
					castOffS = bargeS;
					outOfReach = 0f;
					SfxPlayer.AtPosition(SfxCatalog.HintHullKnock, BargeWorld, 0f);
					GD.Print($"BargeEvent: cast off at {castOffS:0} px along (mooring {mooringS:0}, the fall at {Rules?.FrontS ?? 0f:0}).");
				}
				break;
			case Phase.Drifting:
				bargeS = Mathf.Min(bargeS + DriftSpeed * delta, castOffS + DriftLength);
				outOfReach = level ? 0f : outOfReach + delta;
				lantern = Mathf.MoveToward(lantern, level ? 1f : 0.22f, delta * (level ? 2f : 0.6f));
				TickHounds(delta);
				if (outOfReach >= OutOfReachSeconds)
				{
					GD.Print($"BargeEvent: out of reach {OutOfReachSeconds:0} s at {bargeS:0} px ({Progress * 100f:0}% of its drift); it drifts back.");
					phase = Phase.Returning;
					DriftsFailed++;
					break;
				}
				// At its landing it waits for him to be level with it, as it would for anyone coming ashore.
				if (bargeS >= castOffS + DriftLength - 0.5f && level)
				{
					phase = Phase.Landed;
					GD.Print($"BargeEvent: came ashore at {bargeS:0} px along after one drift of {DriftLength:0} px; the fall at {Rules?.FrontS ?? 0f:0}.");
					SfxPlayer.AtPosition(SfxCatalog.HintHullKnock, BargeWorld, 0f);
					Succeed(DeckBeside(bargeS));
				}
				break;
			case Phase.Returning:
				float rest = RestS();
				bargeS = Mathf.MoveToward(bargeS, rest, ReturnSpeed * delta);
				lantern = Mathf.MoveToward(lantern, 0.4f, delta);
				if (Mathf.Abs(bargeS - rest) < 1f)
					phase = Phase.Moored;
				break;
		}
	}

	// The gate's dogs come down the deck at its level from ahead: a pair every HoundSeconds while it drifts.
	private void TickHounds(float delta)
	{
		houndTimer -= delta;
		if (houndTimer > 0f || Game == null)
			return;
		houndTimer = HoundSeconds;
		for (int k = 0; k < HoundsPerPack; k++)
		{
			float s = bargeS + HoundsAhead + (float)GD.RandRange(0.0, 220.0);
			float half = Mathf.Max(0f, BridgeBand.HalfWidthAt(s) - 90f);
			Vector2 at = origin + BridgeBand.LocalAt(s, (float)GD.RandRange(-half, half));
			Game.SpawnEventEnemy(BestiaryRole.Fast, at, 1f, false);
		}
	}

	// --- drawing ---------------------------------------------------------------------------------------------

	private static readonly Color WindCore = new(1f, 1f, 1f);
	private static readonly Color WindHot = new(0.918f, 0.988f, 0.965f);   // #EAFCF6
	private static readonly Color WindMid = new(0.706f, 0.933f, 0.878f);   // #B4EEE0
	private static readonly Color WindEdge = new(0.310f, 0.682f, 0.620f);  // #4FAE9E
	private static readonly Color Occ = new("#05070C");

	// On the deck (this node draws on the floor, under the bodies): while it is running, the edges of the reach,
	// two dashed lines across the deck level with it, Reach either side, in its lantern's colour - bright while he
	// is between them, faint while he is not. The deck between them is where he must be.
	public override void _Draw()
	{
		if (IsResolved || !IsFound || Player == null || !NearPlayer(BargeWorld))
			return;
		bool level = PlayerLevel;
		float alpha = phase == Phase.Returning ? 0.2f : level ? 0.8f : 0.36f;
		DrawReachEdge(bargeS - Reach, alpha);
		DrawReachEdge(bargeS + Reach, alpha);
	}

	private void DrawReachEdge(float s, float alpha)
	{
		float half = BridgeBand.HalfWidthAt(s) - 10f;
		if (half <= 0f)
			return;
		// Dashes 12 px long, 8 apart, on the art's 2 px grid, over a stroke of occlusion so they read on the setts.
		const float dash = 12f, gap = 8f;
		for (float t = -half; t < half; t += dash + gap)
		{
			Vector2 a = Snap(ToLocal(origin + BridgeBand.LocalAt(s, t)));
			Vector2 b = Snap(ToLocal(origin + BridgeBand.LocalAt(s, Mathf.Min(half, t + dash))));
			DrawLine(a + new Vector2(0f, 2f), b + new Vector2(0f, 2f), new Color(Occ, 0.7f * alpha), 2f);
			DrawLine(a, b, new Color(WindMid, alpha), 2f);
		}
	}

	private static Vector2 Snap(Vector2 p) => new(Mathf.Round(p.X / 2f) * 2f, Mathf.Round(p.Y / 2f) * 2f);

	// The light. Dormant: the far tell, and over it the barge's mirage hanging inverted above the water,
	// shimmering - the fata morgana the clue speaks of, which is how it is seen from further off than its
	// lantern. Running: its lantern, a pool of it thrown across the deck level with the barge from its side (the
	// light the player keeps in), and round the lantern a ring that fills as it nears its landing.
	protected override void DrawLight(CanvasItem light)
	{
		if (IsResolved || !NearPlayer(BargeWorld))
			return;
		Vector2 barge = ToLocal(BargeWorld);
		Vector2 lamp = barge + LanternOffset;
		float flicker = Flicker(5);
		if (IsDormant)
		{
			DrawFarTell(light, BargeWorld, TellColor, flicker);
			DrawMirage(light, barge, 0.45f);
		}
		float l = lantern * flicker;
		// Guttering for the last two seconds before it drifts back.
		if (phase == Phase.Drifting && outOfReach > OutOfReachSeconds - 2f && Mathf.Sin(Clock * 29f) > 0.2f)
			l *= 0.45f;
		ExploreArt.DrawGlow(light, lamp, 150f, new Color(WindMid, 0.45f * l));
		ExploreArt.DrawGlow(light, lamp, 40f, new Color(WindHot, 0.8f * l));
		ExploreArt.DrawGlow(light, lamp, 10f, new Color(WindCore, 0.85f * l));
		// Its light on the water round the hull.
		ExploreArt.DrawGlow(light, barge + new Vector2(0f, 10f), 90f, new Color(WindEdge, 0.12f * l));
		if (IsDormant)
			return;
		// The pool on the deck level with it: from its shore inward, three soft lights along the across-axis.
		for (int k = 0; k < 3; k++)
		{
			float t = side * (BridgeBand.HalfWidthAt(bargeS) - 60f - k * 220f);
			Vector2 p = ToLocal(origin + BridgeBand.LocalAt(bargeS, t));
			ExploreArt.DrawGlow(light, p, 240f, new Color(WindMid, (0.34f - 0.08f * k) * l));
		}
		if (phase == Phase.Drifting || phase == Phase.Landed)
		{
			float p01 = Progress;
			light.DrawArc(lamp, 20f, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau, 40, new Color(WindEdge, 0.35f * l), 2f);
			if (p01 > 0f)
				light.DrawArc(lamp, 20f, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * p01, 40, new Color(WindHot, 0.9f * l), 3f);
		}
	}

	// The barge's image hung upside down in the air above it, as a superior mirage hangs a ship over the horizon,
	// its rows sliding a pixel or two on the heat-shimmer. Drawn on the light layer, so it is light, not a thing.
	private void DrawMirage(CanvasItem light, Vector2 barge, float strength)
	{
		if (fullTex == null)
			return;
		int frame = (int)(Clock / FrameSeconds) % Frames;
		// Its waterline this far above the real one, upside down, so its lantern hangs down toward the real
		// lantern as a superior mirage's top meets the top of what it mirrors.
		const float lift = 150f;
		// Row by row - one art row is the same either way up - each band of four nudged sideways by the shimmer.
		for (int row = 0; row < CellH; row++)
		{
			int band = row / 4;
			float shimmer = Mathf.Round(Mathf.Sin(Clock * 3.1f + band * 1.4f) * 1.5f) * 2f;
			var src = new Rect2(frame * CellW, row, CellW, 1);
			var dst = new Rect2(barge.X - Anchor.X * 2f + shimmer, barge.Y - lift - (row - Anchor.Y + 1f) * 2f, CellW * 2f, 2f);
			float a = strength * (0.65f + 0.35f * Mathf.Sin(Clock * 1.7f + band));
			light.DrawTextureRectRegion(fullTex, dst, src, new Color(WindMid.R, WindMid.G, WindMid.B, a));
		}
	}
}
