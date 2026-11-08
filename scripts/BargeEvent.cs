using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// The Last Bridge's side event: the barge (.ai/chapter-wizards.md 4.9; its ask since 2026-11-07, .ai/unlock-puzzles.md 3.7).
//
// "His seat is across the water." Morgan le Fay - the enchantress of Avalon, the queen in the black barge who
// carried the dying king over the water - is the one person who could take anyone across without his bridge.
// So he keeps her under it: her barge moored beneath the span, its one lantern lit, and her standing in it.
//
// What it asks: LIGHT THE LAMPS AHEAD. His bridge's lamps have been dead since he took the strait: a row of them
// stands along the parapet on her side, from her mooring on. Her barge drifts only as far as they are lit. Stand by
// a dark lamp for LightSeconds and it lights; a lit lamp burns BurnSeconds, gutters for its last GutterSeconds, and
// goes out. She SETS OUT from a lamp only while it burns, and drifts on to the next - so each dark lamp ahead of her
// is a stop where she waits in the dark, and a lamp that goes out before she reaches it stops her there until it is
// lit again. Set out from LampsPerDrift lit lamps and the next is her landing - its lamp already lit, in her own cold
// light - where she comes ashore to him.
//
// THE RHYTHM IS THE PUZZLE. A stretch between lamps takes her ten seconds (LampSpacing at DriftSpeed) and a lamp
// burns twenty, so a lamp lit more than two stretches ahead of her has gone out by the time she gets to it: lighting
// the whole row in one run ahead fails (she stops at the third), and lighting the next two ahead of her - and coming
// back to her when the gate's dogs come down the deck - carries her in. Nobody reads that; they watch a lamp they lit
// go out before she reached it. RegressionChecks.ValidateBarge plays both on paper over every seed's row.
//
// NOTHING LOCKS, AND A WRONG MOVE COSTS TIME, NEVER PROGRESS. The span falls in behind the player at
// BridgeRules.FrontSpeed (about 31 px a pace second) - the clock the chapter already has. She is on the water, so the
// fall cannot take her, but it can take the deck beside her and the lamp she waits at: so she never lets it within
// RestLead of her. Waiting at a dark lamp with the fall that near, she is BORNE ON ahead of it, past the dark lamp -
// which does not count, so her landing moves on one lamp - and what she had already set out from stays counted.
// While she drifts she outruns it (DriftSpeed is clear of FrontSpeed, which the validator holds). Before anyone
// comes the same rule moves her off a mooring the fall has reached, and her arrow and hints go with her
// (PointOfInterest.DriftTo). The row of lamps runs on to the castle wall for that; only the stretch from her mooring
// to her landing is ever drawn, and a lamp the landing moves onto fades up out of the mist.
//
// WHERE IT IS (the bridge's band rules, not the explore bands): moored under the second or the third span, on one
// side of it, from the seed - far enough ahead of the start that the walk to it is a real part of the crossing, and
// with its whole drift inside the span. It rides hard under the shore on its side (TrackAcross); its lamps stand on
// the deck at the foot of that parapet, LampSpacing apart, never in a gatehouse's narrows, never at a jog, and never
// where one of the broken spans' holes is beside the parapet (they step along to clear deck: LayLamps).
//
// NOTHING PLACED HERE IS A TARGET. Nothing joins "enemies". The barge is a sprite on the water, sorted with the bodies
// at its waterline (so a south-east barge is in front of its parapet and a north-west one behind it); the lamps are
// sprites sorted at their feet, which nothing collides with; all the light is on the additive layer every event has.
// The gate's dogs come down the deck from ahead of her while she is under way or waiting (the chapter's Fast role,
// ordinary bodies): the pressure, as every event has.
//
// THE RULE IS ONE CLASS. BargeDrift (below) is the whole of it - the lamps' clocks, where she may drift, the fall
// bearing her on, the landing - and both this event and the validator's paper player step it, so the picture and the
// proof cannot disagree.
//
// Art: tools/art/barge.py (world/barge/): the barge with her in it, the same barge empty, and its pictogram;
// tools/art/barge_lamps.py (world/barge/lamp.png): the lamp, dead, kindling, burning, guttering, and hers.
public partial class BargeEvent : SideEvent
{
	// --- the numbers ----------------------------------------------------------------------------------

	/// <summary>Along the bridge between two lamps, px (3.7: about five along the drift).</summary>
	public const float LampSpacing = 400f;

	/// <summary>The lit lamps she sets out from on one drift; the next lamp after the last is her landing.</summary>
	public const int LampsPerDrift = 5;

	/// <summary>One drift's length on open deck, px: the mooring's span must hold it whole.</summary>
	public const float DriftLength = LampSpacing * LampsPerDrift;

	/// <summary>Her drift, px a second: slower than his walk (220), clear of the span's fall (about 31).</summary>
	public const float DriftSpeed = 40f;

	/// <summary>Standing by a dark lamp this long lights it (3.7: 1 s).</summary>
	public const float LightSeconds = 1f;

	/// <summary>"Stand by a lamp": his feet this near its foot (3.7: 60 px).</summary>
	public const float LightRadius = 60f;

	/// <summary>Lighting he steps off drains at this share of the rate it fills - back, not to nothing at once.</summary>
	public const float LightDrainShare = 0.5f;

	/// <summary>Seconds a lit lamp burns before it goes out (3.7: 20).</summary>
	public const float BurnSeconds = 20f;

	/// <summary>The last seconds of a burn, when the flame sinks and the glass dims: the clock drawn on the lamp.</summary>
	public const float GutterSeconds = 5f;

	/// <summary>At her landing she comes ashore when he is level with her: within this along the bridge.</summary>
	public const float Reach = 260f;

	/// <summary>She never lets the fall nearer than this: waiting with it nearer, she is borne on ahead of it.</summary>
	public const float RestLead = 560f;

	/// <summary>
	/// It begins when he is this near it along the bridge, and within <see cref="ArrivalRadius"/> of it: near enough
	/// that the lamp beside her is on the screen when it begins, on a phone's narrow canvas too.
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

	// Where a lamp may stand (LayLamps): its foot this far in from the shore, on the deck the staircase never bites;
	// this far from a piece's ends, where the shore jogs and a corner turret stands; this far beyond a hole's own
	// radius (the pit's blob is jittered up to a tile and a half past it), for its foot and for the deck a stride in
	// from it where he stands to light it; and the last of the row this far short of the castle wall.
	public const float LampInset = 40f;
	public const float JogMargin = 120f;
	public const float GapMargin = 100f;
	public const float LastLampFromEnd = 250f;
	// The step a lamp is walked along the bridge until it may stand.
	private const float LampSlideStep = 20f;

	// The gate's dogs while she is under way: a pair every HoundSeconds, from this far ahead of her.
	[Export] public float HoundSeconds { get; set; } = 7f;
	[Export] public int HoundsPerPack { get; set; } = 2;
	private const float HoundsAhead = 1050f;

	// --- where it is ------------------------------------------------------------------------------------

	private Vector2 origin;
	private int side = 1;            // +1 the south-east shore, -1 the north-west
	private float mooringS;
	private float[] lampS = Array.Empty<float>();
	private BargeDrift drift;

	/// <summary>Which side of the bridge it is moored on: +1 south-east (bottom right), -1 north-west (top left).</summary>
	public int Side => side;

	/// <summary>Its mooring, px along the bridge: its first lamp's.</summary>
	public float MooringS => mooringS;

	/// <summary>Where it is now, px along the bridge.</summary>
	public float BargeS => drift?.BargeS ?? mooringS;

	/// <summary>Where it is now, in the world.</summary>
	public Vector2 BargeWorld => WorldOnTrack(BargeS);

	/// <summary>The rule, for the probe: every lamp's clock, her stop, what she has set out from.</summary>
	public BargeDrift Drift => drift;

	/// <summary>Lamps in the row (from her mooring to the castle wall; only her stretch of it is drawn).</summary>
	public int LampCount => lampS.Length;

	/// <summary>Lamp <paramref name="i"/>, px along the bridge.</summary>
	public float LampAlong(int i) => lampS[i];

	/// <summary>Where lamp <paramref name="i"/> stands: its foot, on the deck at the parapet on her side.</summary>
	public Vector2 LampFoot(int i) => origin + BridgeBand.LocalAt(lampS[i], LampAcross(lampS[i], side));

	/// <summary>A point on the deck where his feet light lamp <paramref name="i"/>: a short step in from its foot.</summary>
	public Vector2 LampStand(int i) => origin + BridgeBand.LocalAt(lampS[i], LampAcross(lampS[i], side) - side * 30f);

	/// <summary>Across the bridge, px, where a lamp at <paramref name="s"/> stands on <paramref name="side"/>.</summary>
	public static float LampAcross(float s, int side) => side * Mathf.Max(0f, BridgeBand.HalfWidthAt(s) - LampInset);

	/// <summary>
	/// Moored under the second or the third span, on one side, from the seed: three rolls, drawn in a fixed order
	/// from the event's own stream, so the same seed moors it in the same place (the probe checks it). Then its lamps:
	/// the first beside the mooring - the mooring is moved onto it if the rolled place was no place for a lamp - and
	/// the row on from there. A mooring whose row would land her past the span's end (a hole beside the parapet pushed
	/// some lamps along) is drawn back along the span until her whole drift fits.
	/// </summary>
	public override void PlaceSites(ExplorePlacer placer)
	{
		float spanRoll = placer.NextFloat();
		float sideRoll = placer.NextFloat();
		float alongRoll = placer.NextFloat();
		origin = placer.Origin;
		BridgeBand.Piece span = BridgeBand.Find(BridgeBand.PieceKind.Span, MooringSpans[Mathf.Min(MooringSpans.Length - 1, (int)(spanRoll * MooringSpans.Length))]);
		side = sideRoll < 0.5f ? -1 : 1;
		float lo = span.S0 + MooringFromSpanStart;
		float hi = span.S1 - DriftLength - LandingBeforeSpanEnd;
		float rolled = Mathf.Lerp(lo, hi, alongRoll);
		float limit = span.S1 - LandingBeforeSpanEnd;
		for (float m = rolled; ; m -= 50f)
		{
			lampS = LayLamps(Mathf.Max(lo, m), side);
			if (m <= lo || lampS.Length <= LampsPerDrift || lampS[LampsPerDrift] <= limit)
				break;
		}
		mooringS = lampS.Length > 0 ? lampS[0] : rolled;
		drift = new BargeDrift(lampS);
		AddSite(placer.PlacedByChapter(WorldOnTrack(mooringS), SiteFootprint));
	}

	/// <summary>
	/// The row of lamps on <paramref name="side"/>, px along the bridge: the first at or past <paramref name="fromS"/>,
	/// then one every <see cref="LampSpacing"/>, each walked on along the bridge past anywhere a lamp may not stand
	/// (<see cref="LampMayStand"/>), to <see cref="LastLampFromEnd"/> short of the castle wall. Pure: the validator
	/// lays every seed's row with it.
	/// </summary>
	public static float[] LayLamps(float fromS, int side)
	{
		var row = new List<float>();
		float last = BridgeBand.End - LastLampFromEnd;
		float s = fromS;
		while (s <= last)
		{
			while (s <= last && !LampMayStand(s, side))
				s += LampSlideStep;
			if (s > last)
				break;
			row.Add(s);
			s += LampSpacing;
		}
		return row.ToArray();
	}

	/// <summary>
	/// Whether a lamp may stand at <paramref name="s"/> on <paramref name="side"/>: on a span, a yard or the gate yard
	/// (never the bridgehead, never a gatehouse's narrows), clear of the piece's ends where the shore jogs, and with its
	/// foot and the deck where he stands to light it clear of every hole in the deck.
	/// </summary>
	public static bool LampMayStand(float s, int side)
	{
		int index = BridgeBand.PieceIndexAt(s);
		if (index < 0)
			return false;
		BridgeBand.Piece piece = BridgeBand.Pieces[index];
		if (piece.Kind == BridgeBand.PieceKind.Bridgehead || piece.Kind == BridgeBand.PieceKind.Gatehouse)
			return false;
		if (s - piece.S0 < JogMargin || piece.S1 - s < JogMargin)
			return false;
		float foot = LampAcross(s, side);
		float stand = foot - side * LightRadius;
		foreach (BridgeBand.Gap gap in BridgeBand.Gaps)
		{
			float clear = gap.RadiusTiles * BridgeBand.TilePx + GapMargin;
			float ds = s - gap.S;
			if (ds * ds + (foot - gap.T) * (foot - gap.T) < clear * clear
				|| ds * ds + (stand - gap.T) * (stand - gap.T) < clear * clear)
				return false;
		}
		return true;
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
		float s = BargeS + along;
		return origin + BridgeBand.LocalAt(s, side * Mathf.Max(0f, BridgeBand.HalfWidthAt(s) - fromShore));
	}

	// --- what it is doing ---------------------------------------------------------------------------------

	private float lantern = 1f;      // 0..1, eased: full while she is under way, low while she waits in the dark
	private float houndTimer;
	private float freedFor;
	private bool castOffSaid;

	/// <summary>Whether he is level with her now (along the bridge, anywhere across the deck).</summary>
	public bool PlayerLevel => Player != null && Mathf.Abs(AlongOf(Player.GlobalPosition) - BargeS) <= Reach;

	/// <summary>How far into her drift she is, 0 to 1: the lamps set out from, and the stretch under way.</summary>
	public float Progress => drift == null ? 0f : drift.Progress;

	private BridgeRules Rules => Game?.Rules as BridgeRules;

	private float AlongOf(Vector2 world) => BridgeBand.AlongOf(world - origin);

	// Where the fall pushes her to: RestLead ahead of it, once it is moving; nowhere before.
	private float PushS()
	{
		BridgeRules rules = Rules;
		return rules != null && rules.FrontMoving ? rules.FrontS + RestLead : float.NegativeInfinity;
	}

	protected override string Announcement => "A barge under the span, one lantern lit. The lamps along the parapet are dead.";
	protected override string ChestLine => "The barge comes ashore empty - a chest was left in it";
	protected override string Objective
	{
		get
		{
			if (drift == null)
				return string.Empty;
			if (drift.Landed)
				return PlayerLevel ? "The barge comes ashore" : "The barge has come in - meet it at its landing";
			if (drift.Borne)
				return "The fall drives the barge on - light the lamp ahead of it";
			if (drift.WaitingInDark)
				return drift.SetOutFrom == 0 ? "Light the lamp beside the barge" : "The barge waits in the dark - light its lamp";
			int more = LampsPerDrift - drift.SetOutFrom;
			return more > 0 ? $"Light the lamps ahead of the barge  ({more} more)" : "The barge drifts in to its landing";
		}
	}

	// The lamp she waits at, while she waits in the dark (or is borne toward it) and he is not already at it; her
	// landing, once she is in and he is not level with her; her, while she is under way and he is far from her. Never
	// the next dark lamp while she is under way: which lamps to light, and when, is the puzzle.
	protected override Vector2? PointerTarget
	{
		get
		{
			if (IsResolved || Player == null || drift == null)
				return null;
			if (drift.Landed)
				return PlayerLevel ? null : DeckBeside(BargeS);
			if (drift.WaitingInDark || drift.Borne)
			{
				int stop = drift.Stop;
				return Player.FootPosition.DistanceTo(LampFoot(stop)) <= LightRadius * 2f ? null : LampFoot(stop) + new Vector2(0f, -70f);
			}
			return Mathf.Abs(AlongOf(Player.GlobalPosition) - BargeS) > 1100f ? DeckBeside(BargeS) : null;
		}
	}
	protected override string SiteGlyph => "barge";
	public override bool PlacedByChapterRules => true;
	protected override float SiteFootprint => 120f;
	protected override float ArrivalRadius => 760f;
	// The strait's mist: wind's ramp at its body stop, the colour her lantern and the hint's mist wear
	// (tools/art/barge.py, hint_cues.py). Cold, because he keeps her.
	protected override Color TellColor => new Color(0.706f, 0.933f, 0.878f);   // wind mid #B4EEE0

	protected override bool HasArrived() =>
		Player != null && Mathf.Abs(AlongOf(Player.GlobalPosition) - BargeS) <= ArrivalAlong
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

	// tools/art/barge_lamps.py: a 22x62 cell, ten frames - dead, kindling, burning (four), guttering (two), hers (two).
	// Its foot on row 58 of column 11; the flame's heart at (11, 14). barge_lamps.py prints both.
	public const int LampW = 22;
	public const int LampFrames = 10;
	private const int LampMiddle = 11;
	private const int LampFootRow = 58;
	private static readonly Vector2 LampHeartCell = new(11f, 14f);
	/// <summary>The lamp's flame, world px from its foot (the art is drawn at x2).</summary>
	public static readonly Vector2 LampHeart = (LampHeartCell - new Vector2(LampMiddle, LampFootRow)) * 2f;
	private const int FrameDead = 0;
	private const int FrameKindling = 1;
	private const int FrameBurning = 2;
	private const int FrameGutter = 6;
	private const int FrameHers = 8;
	// A lamp the landing moves onto fades up over this long.
	private const float LampFadeIn = 1.2f;

	private Node2D holder;
	private Sprite2D full;
	private Sprite2D empty;
	private Texture2D fullTex;
	private Texture2D lampTex;
	private Node2D lampHolder;
	// The row's sprites, made as the landing reaches them (index = lamp); when each was first shown, for the fade.
	private readonly List<Sprite2D> lampSprites = new();
	private readonly List<float> lampShownAt = new();
	private bool lampsHandedOver;

	protected override void OnPlaced()
	{
		// What the rule does, said and logged - here, not in PlaceSites, so the validator's paper rows stay silent.
		if (drift != null)
		{
			drift.OnLit = OnLampLit;
			drift.OnWentOut = OnLampWentOut;
			drift.OnSetOut = OnSetOut;
			drift.OnBornePast = OnBornePast;
			drift.OnLanded = OnLanded;
		}
		fullTex = Load("barge-full.png");
		Texture2D emptyTex = Load("barge-empty.png");
		lampTex = Load("lamp.png");
		if (fullTex == null || emptyTex == null || lampTex == null)
		{
			GD.PushError("BargeEvent: the barge's art is missing or not imported (python tools/art/barge.py and tools/art/barge_lamps.py, then import).");
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
		// The lamps: on the bodies' layer and sorted at their feet, like the parapets they stand at.
		lampHolder = new Node2D { Name = "BargeLamps", YSortEnabled = true, ZAsRelative = false, ZIndex = DrawLayers.Bodies };
		Game?.CallDeferred(Node.MethodName.AddChild, lampHolder);
		ShowLamps();
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

	// The row's sprites from her mooring to her landing: made as the landing reaches them (a lamp the landing moves
	// onto fades up), hidden once the fall has passed them, each on the frame its clock says.
	private void ShowLamps()
	{
		if (lampHolder == null || !IsInstanceValid(lampHolder) || lampTex == null || drift == null || lampsHandedOver)
			return;
		int landing = drift.Landing;
		while (lampSprites.Count <= landing && lampSprites.Count < lampS.Length)
		{
			int i = lampSprites.Count;
			Vector2 foot = LampFoot(i);
			var sprite = new Sprite2D
			{
				Name = $"Lamp{i}",
				Texture = lampTex,
				Hframes = LampFrames,
				Centered = false,
				Scale = new Vector2(2f, 2f),
				TextureFilter = TextureFilterEnum.Nearest,
				// Sorted at its foot as the trees are: the node SortLift above the ground line, the art hung below it.
				Position = new Vector2(Mathf.Round(foot.X), Mathf.Round(foot.Y - WorldSprites.SortLift)),
				Offset = new Vector2(-LampMiddle, WorldSprites.SortLift / 2f - LampFootRow),
			};
			lampHolder.AddChild(sprite);
			lampSprites.Add(sprite);
			// The row she starts with is simply there; a lamp the landing moves onto later comes up out of the mist.
			lampShownAt.Add(Clock > 0.1f ? Clock : -LampFadeIn);
		}
		float fallen = Rules != null && Rules.FrontMoving ? Rules.FrontS : float.NegativeInfinity;
		for (int i = 0; i < lampSprites.Count; i++)
		{
			Sprite2D sprite = lampSprites[i];
			if (!IsInstanceValid(sprite))
				continue;
			sprite.Visible = lampS[i] + 34f >= fallen;
			sprite.Frame = LampFrame(i, landing);
			sprite.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp((Clock - lampShownAt[i]) / LampFadeIn, 0f, 1f));
		}
	}

	private int LampFrame(int i, int landing)
	{
		if (i == landing && !Succeeded)
			return FrameHers + ((int)(Clock / 0.7f) % 2);
		float burn = drift.BurnLeft[i];
		if (burn > 0f)
		{
			if (burn <= GutterSeconds)
				return FrameGutter + ((int)(Clock / 0.16f + i) % 2);
			return FrameBurning + ((int)(Clock / 0.12f + i * 3) % 4);
		}
		return drift.Kindle[i] > 0f ? FrameKindling : FrameDead;
	}

	public override void _ExitTree()
	{
		if (holder != null && IsInstanceValid(holder))
			holder.QueueFree();
		holder = null;
		HandLampsToTheBridge();
		base._ExitTree();
	}

	// The lamps outlast the event: the base frees this node a few seconds after it resolves, and they go to the bridge,
	// which takes them with the deck when the fall reaches them (BridgeRules.KeepWithDeck). Their light was this
	// event's, so they are left as the light leaves them: embers where a lamp was burning, dead where it was dark, and
	// her landing's light gone with her.
	private void HandLampsToTheBridge()
	{
		if (lampsHandedOver)
			return;
		lampsHandedOver = true;
		BridgeRules rules = Rules;
		for (int i = 0; i < lampSprites.Count; i++)
		{
			Sprite2D sprite = lampSprites[i];
			if (!IsInstanceValid(sprite))
				continue;
			sprite.Frame = drift != null && drift.BurnLeft[i] > 0f ? FrameKindling : FrameDead;
			sprite.Modulate = Colors.White;
			if (rules != null && IsInstanceValid(rules) && IsInstanceValid(lampHolder))
				rules.KeepWithDeck(sprite, lampS[i]);
		}
		if (lampHolder != null && IsInstanceValid(lampHolder) && (rules == null || !IsInstanceValid(rules)))
			lampHolder.QueueFree();
		// Otherwise the holder stays in the world with its lamps in it: the bridge hides each as the fall takes it.
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
		ShowLamps();
	}

	// --- the rules -----------------------------------------------------------------------------------------

	// Before anyone comes: she waits at her mooring - or, once the fall has reached it, is borne on ahead of it, past
	// lamps nobody has lit - and the arrow and the hints follow her there.
	protected override void DormantTick(float delta)
	{
		drift?.Step(delta, -1, PushS());
		if (Sites.Count > 0)
			Sites[0].DriftTo(BargeWorld);
		lantern = Mathf.MoveToward(lantern, 1f, delta);
	}

	protected override void Begin()
	{
		houndTimer = HoundSeconds * 0.5f;
		SfxPlayer.AtPosition(SfxCatalog.HintHullKnock, BargeWorld, 0f);
		GD.Print($"BargeEvent: found at lamp {drift?.Stop ?? 0} of the row, {BargeS:0} px along (mooring {mooringS:0}); "
			+ $"her landing at lamp {drift?.Landing ?? 0}, {(drift != null ? lampS[drift.Landing] : 0f):0} px; the fall at {Rules?.FrontS ?? 0f:0}.");
	}

	protected override void Tick(float delta)
	{
		if (Player == null || drift == null)
			return;
		drift.Step(delta, LampUnderFoot(), PushS());
		lantern = Mathf.MoveToward(lantern, drift.UnderWay || drift.Landed ? 1f : 0.45f, delta * 1.5f);
		if (!drift.Landed)
			TickHounds(delta);
		else if (PlayerLevel)
		{
			GD.Print($"BargeEvent: came ashore at {BargeS:0} px along, lamp {drift.Stop}; lit {drift.TimesLit}, went out {drift.TimesWentOut}, "
				+ $"waited in the dark {drift.DarkStops}, borne past {drift.BornePast}; the fall at {Rules?.FrontS ?? 0f:0}.");
			SfxPlayer.AtPosition(SfxCatalog.HintHullKnock, BargeWorld, 0f);
			Succeed(DeckBeside(BargeS));
		}
	}

	// The lamp his feet are within reach of, if any: the nearest of the few round where he stands.
	private int LampUnderFoot()
	{
		Vector2 feet = Player.FootPosition;
		int near = BargeDrift.IndexNear(lampS, AlongOf(feet));
		int best = -1;
		float bestD = LightRadius * LightRadius;
		for (int i = Math.Max(0, near - 1); i <= Math.Min(lampS.Length - 1, near + 1); i++)
		{
			float d = LampFoot(i).DistanceSquaredTo(feet);
			if (d <= bestD)
			{
				bestD = d;
				best = i;
			}
		}
		return best;
	}

	private void OnLampLit(int i)
	{
		SfxPlayer.Cast(Element.Fire, LampFoot(i) + LampHeart);
		GD.Print($"BargeEvent: lamp {i} lit, {lampS[i] - BargeS:0} px ahead of her (her stop lamp {drift.Stop}, set out from {drift.SetOutFrom}).");
	}

	private void OnLampWentOut(int i)
	{
		SfxPlayer.Cast(Element.Wind, LampFoot(i) + LampHeart);
		GD.Print($"BargeEvent: lamp {i} went out, {lampS[i] - BargeS:0} px {(lampS[i] >= BargeS ? "ahead of" : "behind")} her.");
	}

	private void OnSetOut(int i)
	{
		if (!castOffSaid)
		{
			castOffSaid = true;
			SfxPlayer.AtPosition(SfxCatalog.HintHullKnock, BargeWorld, 0f);
		}
		GD.Print($"BargeEvent: set out from lamp {i} ({drift.SetOutFrom} of {LampsPerDrift}); her landing lamp {drift.Landing}.");
	}

	private void OnBornePast(int i) =>
		GD.Print($"BargeEvent: the fall bore her past dark lamp {i}; her landing moves on to lamp {drift.Landing} (set out from {drift.SetOutFrom} still).");

	private void OnLanded(int i) =>
		GD.Print($"BargeEvent: in at her landing, lamp {i}, {lampS[i]:0} px along; she waits for him to come level.");

	// The gate's dogs come down the deck from ahead of her: a pair every HoundSeconds while she is out.
	private void TickHounds(float delta)
	{
		houndTimer -= delta;
		if (houndTimer > 0f || Game == null)
			return;
		houndTimer = HoundSeconds;
		for (int k = 0; k < HoundsPerPack; k++)
		{
			float s = BargeS + HoundsAhead + (float)GD.RandRange(0.0, 220.0);
			float half = Mathf.Max(0f, BridgeBand.HalfWidthAt(s) - 90f);
			Vector2 at = origin + BridgeBand.LocalAt(s, (float)GD.RandRange(-half, half));
			Game.SpawnEventEnemy(BestiaryRole.Fast, at, 1f, false);
		}
	}

	protected override void OnStoodDown() => ShowLamps();

	/// <summary>
	/// Poses it part-way, for _UiShot (--lamps-lit=N): begun, set out from her first lamp and some way to the next,
	/// and the next <paramref name="lit"/> lamps ahead of her lit, the nearest longest ago (its clock lowest), so the
	/// lamps, their clocks, the lit water and the first dark lamp past them are photographed. A run never calls it.
	/// </summary>
	public void PoseForPhoto(int lit)
	{
		if (drift == null)
			return;
		if (IsDormant)
			Arrive();
		drift.PoseForPhoto(lit);
		ShowLamps();
	}

	// --- drawing ---------------------------------------------------------------------------------------------

	private static readonly Color WindCore = new(1f, 1f, 1f);
	private static readonly Color WindHot = new(0.918f, 0.988f, 0.965f);   // #EAFCF6
	private static readonly Color WindMid = new(0.706f, 0.933f, 0.878f);   // #B4EEE0
	private static readonly Color WindEdge = new(0.310f, 0.682f, 0.620f);  // #4FAE9E
	// The lamps' fire: fire's ramp (bonelight.ELEMENTS["fire"]) - core, hot, mid, edge, as the tomb's beacons burn.
	private static readonly Color FireCore = new(1f, 0.957f, 0.816f);
	private static readonly Color FireHot = new(1f, 0.784f, 0.282f);
	private static readonly Color FireMid = new(0.910f, 0.478f, 0.102f);
	private static readonly Color FireEdge = new(0.722f, 0.220f, 0.102f);
	private static readonly Color Occ = new("#05070C");

	// On the deck (this node draws on the floor, under the bodies): round each lamp of her stretch, where he stands to
	// light it - a faint ring that fills while he lights a dark one, and round a burning one its clock, running down
	// (hot, then the edge's red once it gutters). Once she is in: the edges of her reach across the deck, until he is
	// level with her.
	public override void _Draw()
	{
		if (IsResolved || !IsFound || Player == null || drift == null)
			return;
		if (drift.Landed)
		{
			if (!PlayerLevel && NearPlayer(BargeWorld))
			{
				DrawReachEdge(BargeS - Reach, 0.5f);
				DrawReachEdge(BargeS + Reach, 0.5f);
			}
			return;
		}
		int landing = drift.Landing;
		for (int i = Math.Max(0, drift.Stop - 1); i < landing; i++)
		{
			Vector2 foot = LampFoot(i);
			if (!NearPlayer(foot))
				continue;
			float burn = drift.BurnLeft[i];
			if (burn > 0f)
				DrawProgressArc(foot, LightRadius, burn / BurnSeconds, burn > GutterSeconds ? FireHot : FireEdge, 3f, 0.24f);
			else if (i >= drift.Stop)
				DrawProgressArc(foot, LightRadius, drift.Kindle[i], FireMid, 3f, drift.Kindle[i] > 0f ? 0.42f : 0.24f);
		}
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

	// Along the water, the lit stretch is drawn this often (a soft light every LaneStep px from her to where she must
	// stop, wide enough to overlap into one band - at 80 px apart they read as a dotted line): at most LaneMax of them,
	// so the cost is bounded whatever the row.
	private const float LaneStep = 50f;
	private const int LaneMax = 48;

	// The light. Dormant: the far tell, and over it the barge's mirage hanging inverted above the water, shimmering -
	// the fata morgana the clue speaks of, which is how it is seen from further off than its lantern. Found: her
	// lantern and a ring round it that fills toward her landing; every burning lamp's flame and the pool it throws on
	// the deck and on the water under it; the landing's lamp in her own cold light; and THE LIT WATER - from her to the
	// first dark lamp ahead, the stretch she can drift, drawn on the strait. A dark lamp ends it.
	protected override void DrawLight(CanvasItem light)
	{
		if (IsResolved || drift == null)
			return;
		Vector2 barge = ToLocal(BargeWorld);
		Vector2 lamp = barge + LanternOffset;
		bool near = NearPlayer(BargeWorld);
		float flicker = Flicker(5);
		if (IsDormant)
		{
			if (!near)
				return;
			DrawFarTell(light, BargeWorld, TellColor, flicker);
			DrawMirage(light, barge, 0.45f);
		}
		if (near)
		{
			float l = lantern * flicker;
			ExploreArt.DrawGlow(light, lamp, 150f, new Color(WindMid, 0.45f * l));
			ExploreArt.DrawGlow(light, lamp, 40f, new Color(WindHot, 0.8f * l));
			ExploreArt.DrawGlow(light, lamp, 10f, new Color(WindCore, 0.85f * l));
			// Its light on the water round the hull.
			ExploreArt.DrawGlow(light, barge + new Vector2(0f, 10f), 90f, new Color(WindEdge, 0.12f * l));
			if (!IsDormant)
			{
				float p01 = Progress;
				light.DrawArc(lamp, 20f, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau, 40, new Color(WindEdge, 0.35f * l), 2f);
				if (p01 > 0f)
					light.DrawArc(lamp, 20f, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * p01, 40, new Color(WindHot, 0.9f * l), 3f);
			}
		}
		if (IsDormant)
			return;

		int landing = drift.Landing;
		// The lit water: from her to the first dark lamp at or past her stop (or her landing, when every lamp to it burns).
		if (!drift.Landed && near)
		{
			float end = lampS[drift.ReachableStop()];
			float s = BargeS + LaneStep * 0.5f;
			for (int k = 0; s < end && k < LaneMax; k++, s += LaneStep)
			{
				Vector2 p = ToLocal(WorldOnTrack(s));
				ExploreArt.DrawGlow(light, p, 100f, new Color(FireMid, 0.075f));
				ExploreArt.DrawGlow(light, p, 44f, new Color(FireHot, 0.05f));
			}
		}
		for (int i = Math.Max(0, drift.Stop - 2); i <= landing && i < lampSprites.Count; i++)
		{
			Vector2 foot = LampFoot(i);
			if (!NearPlayer(foot) || (lampSprites[i] != null && IsInstanceValid(lampSprites[i]) && !lampSprites[i].Visible))
				continue;
			Vector2 f = ToLocal(foot);
			Vector2 heart = f + LampHeart;
			float fade = Mathf.Clamp((Clock - lampShownAt[i]) / LampFadeIn, 0f, 1f);
			if (i == landing && !drift.Landed)
			{
				float b = (0.85f + 0.15f * Mathf.Sin(Clock * 2.2f)) * fade;
				ExploreArt.DrawGlow(light, heart, 90f, new Color(WindMid, 0.32f * b));
				ExploreArt.DrawGlow(light, heart, 22f, new Color(WindHot, 0.7f * b));
				ExploreArt.DrawGlow(light, ToLocal(WorldOnTrack(lampS[i])), 110f, new Color(WindEdge, 0.14f * b));
				continue;
			}
			float burn = drift.BurnLeft[i];
			if (burn <= 0f)
			{
				// Lighting it: the ember catching under the glass.
				if (drift.Kindle[i] > 0f)
					ExploreArt.DrawGlow(light, heart + new Vector2(0f, 6f), 14f, new Color(FireMid, 0.6f * drift.Kindle[i]));
				continue;
			}
			// Burning, and sinking as it gutters.
			float g = burn > GutterSeconds ? 1f : 0.35f + 0.65f * burn / GutterSeconds;
			float fl = Flicker(20 + i) * g;
			ExploreArt.DrawGlow(light, heart, 120f, new Color(FireMid, 0.3f * fl));
			ExploreArt.DrawGlow(light, heart, 26f, new Color(FireHot, 0.75f * fl));
			ExploreArt.DrawGlow(light, heart, 8f, new Color(FireCore, 0.8f * fl));
			ExploreArt.DrawGlow(light, f, 90f, new Color(FireMid, 0.16f * fl));
			ExploreArt.DrawGlow(light, ToLocal(WorldOnTrack(lampS[i])), 120f, new Color(FireEdge, 0.2f * fl));
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

/// <summary>
/// THE BARGE'S RULE, whole (.ai/unlock-puzzles.md 3.7): the row's lamps and their clocks, where she is, the lamp she
/// is at or drifting toward (<see cref="Stop"/>), and how many lit lamps she has set out from (<see cref="SetOutFrom"/>).
/// Pure - no node, no engine call - so BargeEvent steps it with the player's feet and RegressionChecks steps it with a
/// paper player, and the two cannot disagree.
/// </summary>
/// <remarks>
/// Each step, in order:
///   1. every lamp's clock: a burning one burns down and, at nothing, goes out; the one his feet are at (if it is of
///      her stretch - from her stop to before her landing - and dark) fills toward lit; any other's lighting drains;
///   2. her: in at her landing, she only keeps ahead of the fall; under way (set out from a lit lamp), she drifts to
///      her stop and arrives - at her landing, she is in; otherwise at her stop she waits, sets out the moment it burns
///      (it counts), and while it is dark and the fall is within RestLead she is borne on past it (it does not count,
///      and her landing moves on one lamp); between lamps (borne there), she drifts on to a lamp that burns, and
///      otherwise only goes where the fall pushes her.
/// Her landing is always <c>Stop + (LampsPerDrift - SetOutFrom)</c>, so setting out never moves it and being borne
/// past a dark lamp moves it on exactly one.
/// </remarks>
public sealed class BargeDrift
{
	public readonly float[] LampS;
	public readonly float[] Kindle;
	public readonly float[] BurnLeft;

	/// <summary>Where she is, px along the bridge.</summary>
	public float BargeS;
	/// <summary>The lamp she is at, or drifting or being borne toward.</summary>
	public int Stop { get; private set; }
	/// <summary>Lit lamps she has set out from.</summary>
	public int SetOutFrom { get; private set; }
	/// <summary>Set out from a lit lamp and drifting to her stop.</summary>
	public bool UnderWay { get; private set; }
	/// <summary>In at her landing: she comes ashore when he is level with her.</summary>
	public bool Landed { get; private set; }

	// Tallies, for the probe and the log.
	public int TimesLit { get; private set; }
	public int TimesWentOut { get; private set; }
	/// <summary>Arrivals at a lamp that was dark: the stops she waited at in the dark.</summary>
	public int DarkStops { get; private set; }
	/// <summary>Dark lamps the fall bore her past (each moved her landing on one).</summary>
	public int BornePast { get; private set; }

	// What happened, for the event's sound and its log. Set once; the validator leaves them null.
	public Action<int> OnLit, OnWentOut, OnSetOut, OnBornePast, OnLanded;

	public BargeDrift(float[] lampS)
	{
		LampS = lampS;
		Kindle = new float[lampS.Length];
		BurnLeft = new float[lampS.Length];
		BargeS = lampS.Length > 0 ? lampS[0] : 0f;
	}

	/// <summary>Her landing: the lamp she comes in at, once she has set out from LampsPerDrift lit lamps.</summary>
	public int Landing => Landed ? Stop : Math.Min(LampS.Length - 1, Stop + BargeEvent.LampsPerDrift - SetOutFrom);

	/// <summary>Waiting at her stop and it is dark.</summary>
	public bool WaitingInDark => !Landed && !UnderWay && AtStop && BurnLeft[Stop] <= 0f;

	/// <summary>Between lamps, where the fall bore her, and her stop ahead dark.</summary>
	public bool Borne => !Landed && !UnderWay && !AtStop && BurnLeft[Stop] <= 0f;

	private bool AtStop => BargeS >= LampS[Stop] - 0.01f;

	/// <summary>How far into her drift she is, 0 to 1.</summary>
	public float Progress
	{
		get
		{
			if (Landed)
				return 1f;
			float part = 0f;
			if (UnderWay && Stop > 0)
				part = Mathf.Clamp((BargeS - LampS[Stop - 1]) / Mathf.Max(1f, LampS[Stop] - LampS[Stop - 1]), 0f, 1f);
			return Mathf.Clamp((SetOutFrom - (UnderWay ? 1f : 0f) + part) / BargeEvent.LampsPerDrift, 0f, 1f);
		}
	}

	/// <summary>Whether lamp <paramref name="i"/> is of her stretch now: from her stop to before her landing.</summary>
	public bool IsOfStretch(int i) => !Landed && i >= Stop && i < Landing;

	/// <summary>The lamp the lit water reaches to: the first dark lamp at or past her stop, or her landing.</summary>
	public int ReachableStop()
	{
		int landing = Landing;
		int i = Stop;
		while (i < landing && BurnLeft[i] > 0f)
			i++;
		return i;
	}

	/// <summary>The lamp of <paramref name="lampS"/> nearest <paramref name="s"/> along the bridge (a binary search).</summary>
	public static int IndexNear(float[] lampS, float s)
	{
		if (lampS.Length == 0)
			return 0;
		int lo = 0, hi = lampS.Length - 1;
		while (lo < hi)
		{
			int mid = (lo + hi) / 2;
			if (lampS[mid] < s)
				lo = mid + 1;
			else
				hi = mid;
		}
		if (lo > 0 && s - lampS[lo - 1] < lampS[lo] - s)
			lo--;
		return lo;
	}

	/// <summary>
	/// One step of <paramref name="dt"/> seconds. <paramref name="standingAt"/> is the lamp his feet are at, or -1;
	/// <paramref name="pushS"/> is RestLead ahead of the fall, or negative infinity before it moves.
	/// </summary>
	public void Step(float dt, int standingAt, float pushS)
	{
		if (LampS.Length == 0)
			return;
		// 1. The lamps' clocks: every burning lamp of the row up to her landing, and the one he stands at.
		int landing = Landing;
		for (int i = 0; i <= landing; i++)
		{
			if (BurnLeft[i] > 0f)
			{
				BurnLeft[i] -= dt;
				if (BurnLeft[i] <= 0f)
				{
					BurnLeft[i] = 0f;
					Kindle[i] = 0f;
					TimesWentOut++;
					OnWentOut?.Invoke(i);
				}
				continue;
			}
			if (i == standingAt && IsOfStretch(i))
			{
				Kindle[i] += dt / BargeEvent.LightSeconds;
				if (Kindle[i] >= 1f)
				{
					Kindle[i] = 0f;
					BurnLeft[i] = BargeEvent.BurnSeconds;
					TimesLit++;
					OnLit?.Invoke(i);
				}
			}
			else if (Kindle[i] > 0f)
				Kindle[i] = Mathf.Max(0f, Kindle[i] - dt * BargeEvent.LightDrainShare / BargeEvent.LightSeconds);
		}

		// 2. Her.
		if (Landed)
		{
			// In at her landing, she only keeps ahead of the fall: the deck beside her must stay to be stood on.
			BargeS = Mathf.Max(BargeS, pushS);
			return;
		}
		if (UnderWay)
		{
			BargeS = Mathf.Min(Mathf.Max(BargeS + BargeEvent.DriftSpeed * dt, pushS), LampS[Stop]);
			if (BargeS >= LampS[Stop] - 0.01f)
				Arrive();
			return;
		}
		if (AtStop)
		{
			if (BurnLeft[Stop] > 0f)
			{
				SetOut();
				return;
			}
			if (pushS > LampS[Stop] && Stop < LampS.Length - 1)
			{
				// Borne on past a dark lamp: it does not count, and her landing moves on one.
				int past = Stop;
				Stop++;
				BornePast++;
				BargeS = Mathf.Min(pushS, LampS[Stop]);
				OnBornePast?.Invoke(past);
				if (BargeS >= LampS[Stop] - 0.01f)
					Arrive();
			}
			return;
		}
		// Between lamps, where the fall bore her: on to a lamp that burns, otherwise only as far as the fall pushes.
		float drift = BurnLeft[Stop] > 0f ? BargeEvent.DriftSpeed * dt : 0f;
		BargeS = Mathf.Min(Mathf.Max(BargeS + drift, pushS), LampS[Stop]);
		if (BargeS >= LampS[Stop] - 0.01f)
			Arrive();
	}

	private void SetOut()
	{
		int from = Stop;
		SetOutFrom++;
		Stop = Math.Min(LampS.Length - 1, Stop + 1);
		UnderWay = Stop > from;
		OnSetOut?.Invoke(from);
		if (!UnderWay)
			Arrive();
	}

	// At her stop: in, if it is her landing (or the row's last lamp, against the castle wall); a dark stop is counted.
	private void Arrive()
	{
		BargeS = LampS[Stop];
		UnderWay = false;
		if (SetOutFrom >= BargeEvent.LampsPerDrift || Stop == LampS.Length - 1)
		{
			Landed = true;
			OnLanded?.Invoke(Stop);
			return;
		}
		if (BurnLeft[Stop] <= 0f)
			DarkStops++;
	}

	/// <summary>
	/// Poses it part-way for a photograph: set out from her stop and 60% of the way to the next lamp, the lamp she left
	/// guttering, and the next <paramref name="lit"/> lamps ahead lit, the nearer lit longer ago.
	/// </summary>
	public void PoseForPhoto(int lit)
	{
		if (Landed || LampS.Length < 3)
			return;
		BurnLeft[Stop] = BargeEvent.GutterSeconds - 1f;
		SetOut();
		int from = Stop - 1;
		BargeS = Mathf.Lerp(LampS[from], LampS[Stop], 0.6f);
		for (int k = 0; k < lit && Stop + k < Landing; k++)
		{
			BurnLeft[Stop + k] = BargeEvent.BurnSeconds - 4f - 5f * (lit - 1 - k);
			TimesLit++;
		}
	}
}
