using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// The Enchanted Forest's second side event: the walking hut (.ai/chapter-wizards.md 4.1).
//
// "Every path in a wood once led to her door. He bent the wood so that every path leads inward, and set the hut
// walking its circle for ever." Baba Yaga is kept in her own hut, and the hut is kept walking: round and round a
// clearing in the deep wood, on hen's legs, its back to whoever comes - as in the tales, where the traveller has to
// ask it to turn its back to the forest and its front to him.
//
// What it asks: HUNT. Every other thing in the game that is found waits for you; this walks away. Within
// StartleRadius it turns its back and strides off, quicker than his walk for two seconds, then slower than him
// (FleeingMover, the primitive section 7 asked for: SecretMover inverted). A bar fills while he is within
// KeepRadius of it and HOLDS while he is not - nothing drains. At HuntSeconds of keeping up it tires: it stops,
// turns its door to him, kneels, and she steps out. Water or trunks across its way cut a bolt short, so a hut
// run into a lake is a hut caught; the 100 px between the two radii is where a player who reads it walks.
//
// NOTHING LOCKS, NOTHING IS LOST. Losing it costs nothing: past LoseRadius it soon calms and stands where it is,
// the bar held, the gold arrow still on it, and its prints lead back - every fall lays a fresh three-toed print,
// pointing the way it went (WalkingHutEvent draws them; tools/art/walking_hut.py). From two chunks out, old
// trails of them spiral in through the moss to the worn ring of its circle: the prints are its far tell before
// the light is.
//
// NOTHING HERE IS A BODY. The hut is a sprite sorted among the bodies by its feet and moved by FleeingMover; it is
// never in "enemies", has no collision layer, and is never a target. The trunks it is stopped by are asked of the
// physics (layer 4, the blocking ground's) at the one point ahead of its feet, and the water and the walls of the
// streamer's own data, so it is stopped by what stops the player's boots.
//
// Art: tools/art/walking_hut.py (world/hut/: the hut's fourteen frames on a 96x128 cell, its prints, poi-hut). The
// one light on it is his mark in the eyes of the horse skull on its gable, and the far tell stands up from there.
public partial class WalkingHutEvent : SideEvent
{
	/// <summary>Within this it turns its back and strides away (4.1: 420).</summary>
	public const float StartleRadius = 420f;

	/// <summary>Within this the bar fills (4.1: 520); outside it the bar holds.</summary>
	public const float KeepRadius = 520f;

	/// <summary>Seconds of keeping up before it tires (4.1: 30).</summary>
	public const float HuntSeconds = 30f;

	/// <summary>
	/// A bolt: how long, and how much quicker than his walk (4.1: quicker for 2 s), easing to TiredBoltShare as the bar
	/// fills. 1.5, not the first guess of 1.3: at 1.3 a 2 s bolt gains 132 px, so from just inside the startle radius
	/// it never left the 520 px reach, and a player who simply walked at it ran it down in 31 s - the bolt had no teeth.
	/// At 1.5 rushing it costs about 8 s out of reach on open ground (38 s in all), and through a crowd at 80% of his
	/// walk about 71 s; shadowing it in the band between the radii, so it never bolts, is still 30 s. The band is the skill.
	/// </summary>
	public const float BoltSeconds = 2f;
	public const float BoltShare = 1.5f;
	public const float TiredBoltShare = 1.15f;

	/// <summary>After a bolt, while he is still that close: slower than him (4.1: "then slower").</summary>
	public const float StrideShare = 0.75f;

	/// <summary>Further than this from him for CalmSeconds and it stands where it is, the bar held.</summary>
	public const float LoseRadius = 1100f;
	public const float CalmSeconds = 3f;

	/// <summary>Its circle while nobody has come: round the placed site, this far out.</summary>
	public const float CircleRadius = 200f;
	public const float DeepWoodMinChunks = 6f;
	public const float DeepWoodMaxChunks = 8f;
	public const float WoodlandRingRadius = 440f;
	public override float PlacementMinChunks => DeepWoodMinChunks;
	public override float PlacementMaxChunks => DeepWoodMaxChunks;

	/// <summary>World px between two falls of its feet: one print each.</summary>
	public const float FallSpacing = 56f;

	/// <summary>How far from its home the old trails start: two chunks (4.1: prints "from two chunks out").</summary>
	public const float TrailReach = 3072f;
	public const int OldTrails = 3;

	protected override string Announcement => "A hut walks the deep wood on hen's legs, its back to you.";
	protected override string ChestLine => "The hut kneels - a chest was left at its door";
	protected override string Objective => IsResolved ? string.Empty
		: tiredFor >= 0f ? "The hut kneels"
		: Player != null && Player.GlobalPosition.DistanceTo(mover.Position) <= KeepRadius
			? $"Keep up with the walking hut  ({Mathf.RoundToInt(progress * 100f)}%)"
			: $"Get close to the walking hut  ({Mathf.RoundToInt(progress * 100f)}%)";
	// The hut itself, wherever it has got to - aimed at its walls, not its feet, so the chevron is not on its toes.
	// On it while it kneels too: the arrow going null is what says an event is over (SideEvent, the probes), and she
	// has not stepped out yet.
	protected override Vector2? PointerTarget => IsResolved ? null : mover.Position + new Vector2(0f, -110f);

	protected override string SiteGlyph => "hut";
	// The whole circle it walks, and a margin: no tree is ever put down on its round.
	protected override float SiteFootprint => CircleRadius + 80f;
	protected override float ArrivalRadius => 480f;
	// His mark: the arcane ramp's mid #C08CFF, the eye-light in the horse skull on its gable.
	protected override Color TellColor => new(0.753f, 0.549f, 1.0f);

	// --- state ---------------------------------------------------------------------------------------

	private readonly FleeingMover mover = new()
	{
		StartleRadius = StartleRadius,
		BoltSeconds = BoltSeconds,
		BoltShare = BoltShare,
		TiredBoltShare = TiredBoltShare,
		StrideShare = StrideShare,
		LoseRadius = LoseRadius,
		CalmSeconds = CalmSeconds,
		CircleRadius = CircleRadius,
	};

	// The bar, 0 to 1. Named "progress" because _UiShot --win-event fills a float field of that name and lets the real
	// path run (the hut tires, turns, kneels and she steps out).
	private float progress;
	// Seconds since it tired, or below 0 while it is still walking.
	private float tiredFor = -1f;
	private bool succeeded;
	private Vector2 home;

	/// <summary>Where the hut stands now (its feet). For the probe and _UiShot.</summary>
	public Vector2 HutPosition => mover.Position;

	/// <summary>The bar: 0 to 1. For the probe.</summary>
	public float Progress => progress;

	/// <summary>True once it has tired and is kneeling. For the probe.</summary>
	public bool IsTired => tiredFor >= 0f;

	/// <summary>The mover's tallies and state. For the probe.</summary>
	public int BoltsStarted => mover.BoltsStarted;
	public int BoltsCut => mover.BoltsCut;
	public int TimesCalmed => mover.TimesCalmed;
	public bool IsFleeing => mover.Fleeing;
	public bool IsBolting => mover.Bolting;

	/// <summary>Prints laid by its feet this run. For the probe.</summary>
	public int FreshPrintsLaid { get; private set; }

	/// <summary>The old prints laid out from the seed, and how many of them fall on open ground. For the probe.</summary>
	public int OldPrintCount => oldPrints.Count;

	/// <summary>Where to stand to photograph it: off its front corner, so the whole hut is on screen and she steps out
	/// beside him, not onto him. For _UiShot.</summary>
	public Vector2 StandPoint => mover.Position + new Vector2(150f, 130f);

	// --- placement ------------------------------------------------------------------------------------

	private struct Print
	{
		public Vector2 At;
		public int Direction;   // the sheet's column: the way its middle toe points
		public float LaidAt;    // Clock when laid; old prints are below 0
		public sbyte Ground;    // 0 not asked yet, 1 open, -1 blocked (old prints only)
	}

	private readonly List<Print> oldPrints = new();
	private const int FreshCapacity = 96;
	private readonly Print[] fresh = new Print[FreshCapacity];
	private int freshNext;
	private int freshCount;
	// A fresh print turns old after this long: shallower, the turned earth gone.
	private const float FreshSeconds = 50f;

	/// <summary>
	/// The circle on open ground over its whole disc (the hut walks all of it), swept for through the band as the seal's
	/// courtyard is. Then its old prints, laid from the same placer so they are the run's: the worn ring of its circle,
	/// and OldTrails trails spiralling in to it from TrailReach out - each toward the ring, so they lead to it.
	/// </summary>
	public override void PlaceSites(ExplorePlacer placer)
	{
		home = AddSite(placer.EventSiteInBand(SiteFootprint, dense: true,
			also: p => placer.HasWoodedSurrounds(p, WoodlandRingRadius),
			minChunks: DeepWoodMinChunks, maxChunks: DeepWoodMaxChunks)).Position;
		oldPrints.Clear();
		// The ring it has worn: it walks it with the angle rising (clockwise on screen), so the toes point that way.
		int round = Mathf.CeilToInt(Mathf.Tau * CircleRadius / FallSpacing);
		for (int i = 0; i < round; i++)
		{
			float a = i * Mathf.Tau / round;
			Vector2 tangent = new(-Mathf.Sin(a), Mathf.Cos(a));
			Vector2 side = Vector2.FromAngle(a) * (i % 2 == 0 ? 14f : -14f);
			AddOld(home + Vector2.FromAngle(a) * CircleRadius + side, tangent);
		}
		// The trails: each from two chunks out, curling round as it comes in, onto the ring.
		for (int t = 0; t < OldTrails; t++)
		{
			float start = (t + placer.NextFloat() * 0.6f) * Mathf.Tau / OldTrails;
			float turn = (placer.NextFloat() < 0.5f ? -1f : 1f) * (0.8f + 0.7f * placer.NextFloat());
			Vector2 last = home + Vector2.FromAngle(start) * TrailReach;
			float walked = 0f;
			int falls = 0;
			for (int k = 1; k <= 600; k++)
			{
				float s = k / 600f;
				float r = Mathf.Lerp(TrailReach, CircleRadius, s);
				Vector2 at = home + Vector2.FromAngle(start + turn * s) * r;
				walked += at.DistanceTo(last);
				Vector2 along = at - last;
				last = at;
				if (walked < FallSpacing)
					continue;
				walked = 0f;
				Vector2 heading = along.LengthSquared() > 0.01f ? along.Normalized() : Vector2.Up;
				AddOld(at + new Vector2(-heading.Y, heading.X) * (falls++ % 2 == 0 ? 14f : -14f), heading);
			}
		}
	}

	private void AddOld(Vector2 at, Vector2 travel) =>
		oldPrints.Add(new Print { At = at, Direction = PrintDirection(travel), LaidAt = -1f });

	/// <summary>
	/// The sheet column for a print laid walking along <paramref name="travel"/>: the middle toe points the way
	/// it walked. (They were laid toes-back at first - the print read as an arrow onward, the heel its point -
	/// but a chicken track whose toes point away from its walk reads as backwards tracks however it runs;
	/// Eric, 2026-11-05: "the toes point opposite the hut's direction of travel". The trail still leads to it,
	/// the way real tracks do: by being followed in the direction the toes point.)
	/// </summary>
	public static int PrintDirection(Vector2 travel)
	{
		float a = travel.Angle();
		int k = Mathf.RoundToInt(a / (Mathf.Pi / 4f));
		return ((k % 8) + 8) % 8;
	}

	// --- what stands there ----------------------------------------------------------------------------

	private const string HutPath = "res://assets/bonelight/world/hut/hut.png";
	private const string PrintsPath = "res://assets/bonelight/world/hut/hut-prints.png";
	private const string HerPath = "res://assets/bonelight/characters/yaga-1.png";

	// tools/art/walking_hut.py: 96x128 a frame, the feet's middle on column 48, the feet on row 126.
	private const int HutW = 96;
	private const int HutH = 128;
	private const int HutMiddle = 48;
	private const int HutFootRow = 126;
	private const int PrintCell = 16;
	// The frames (walking_hut.py frames()).
	private const int FrameWalk = 0;
	private const int WalkFrames = 6;
	private const int FrameStand = 6;
	private const int FrameSide = 8;
	private const int FrameFront = 9;
	private const int FrameKneelHalf = 10;
	private const int FrameKnelt = 11;
	private const int FrameAjar = 12;
	private const int FrameOpen = 13;

	// Where the skull's eyes are on each kind of frame, from its feet, in world px (the art at x2).
	private static readonly Vector2 EyesStanding = new(-4f, -242f);
	private static readonly Vector2 EyesSide = new(-74f, -230f);
	private static readonly Vector2 EyesKneelHalf = new(-4f, -202f);
	private static readonly Vector2 EyesKnelt = new(-4f, -162f);
	// The open door's threshold when it kneels, from its feet: where she comes out.
	private static readonly Vector2 Threshold = new(0f, -22f);

	// The tired beat, seconds after it tires: stops, turns, faces him, kneels, the door goes, she steps out.
	private const float TurnAt = 0.3f;
	private const float FaceAt = 0.6f;
	private const float KneelAt = 0.9f;
	private const float KneltAt = 1.3f;
	private const float AjarAt = 1.75f;
	private const float OpenAt = 2.15f;
	private const float StepOutAt = 2.35f;
	private const float FreedAt = 3.1f;

	private Node2D hutRoot;
	private Sprite2D hut;
	private Sprite2D her;
	private Texture2D printsTex;
	private float walkedSinceFall;
	private int footSide = 1;
	private float walkPhase;
	private float freedFor;
	private Vector2 skullAt;
	private PhysicsShapeQueryParameters2D trunkQuery;

	protected override void OnPlaced()
	{
		TextureFilter = TextureFilterEnum.Nearest;
		mover.Home = home;
		mover.Position = home + Vector2.Right * CircleRadius;
		mover.IsBlocked = Blocked;
		Texture2D hutTex = ResourceLoader.Exists(HutPath) ? GD.Load<Texture2D>(HutPath) : null;
		printsTex = ResourceLoader.Exists(PrintsPath) ? GD.Load<Texture2D>(PrintsPath) : null;
		Texture2D herTex = ResourceLoader.Exists(HerPath) ? GD.Load<Texture2D>(HerPath) : null;
		if (hutTex == null || printsTex == null || herTex == null)
		{
			GD.PushError("WalkingHutEvent: the hut's art is missing or not imported (python tools/art/walking_hut.py, then import).");
			return;
		}
		hutRoot = new Node2D { Name = "WalkingHut", YSortEnabled = true };
		hut = new Sprite2D
		{
			Name = "Hut",
			Texture = hutTex,
			Hframes = hutTex.GetWidth() / HutW,
			Centered = false,
			Scale = new Vector2(2f, 2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Offset = new Vector2(-HutMiddle, WorldSprites.SortLift / 2f - HutFootRow),
		};
		hutRoot.AddChild(hut);
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
		hutRoot.AddChild(her);
		PlaceHut();
		// In the run's world at its origin, so its children stand at world positions; it is the event's sibling and
		// stays, knelt with its door open, after the win.
		Game?.CallDeferred(Node.MethodName.AddChild, hutRoot);
	}

	// The ground under a foot it is about to put down: the streamer's data (water, walls, fences, the stage's edge),
	// the navigation grid's prop clusters, and the trunks, which are bodies on the blocking layer.
	private bool Blocked(Vector2 at)
	{
		if (Game == null)
			return false;
		if (Game.IsGroundBlockedAt(at) || MazeNavigation.Active?.IsBlockedAt(at) == true)
			return true;
		PhysicsDirectSpaceState2D space = GetWorld2D()?.DirectSpaceState;
		if (space == null)
			return false;
		trunkQuery ??= new PhysicsShapeQueryParameters2D
		{
			Shape = new CircleShape2D { Radius = 18f },
			CollisionMask = 1u << (LevelTilePainter.TerrainCollisionLayer - 1),
			CollideWithAreas = false,
			CollideWithBodies = true,
		};
		trunkQuery.Transform = new Transform2D(0f, at);
		return space.IntersectShape(trunkQuery, 1).Count > 0;
	}

	// --- the rules -------------------------------------------------------------------------------------

	protected override void DormantTick(float delta)
	{
		// Its circle, for ever - only while someone is near enough to see it go round; the rest of the run it is
		// a hut standing in a wood, and nobody can tell.
		if (Player != null && Player.GlobalPosition.DistanceSquaredTo(home) < 3200f * 3200f)
			Walk(delta);
		// He has come close to the hut itself (the site is its circle's middle; arriving there is caught by the base).
		if (Player != null && PlayerWithin(mover.Position, ArrivalRadius))
			Arrive();
	}

	protected override void Begin()
	{
		// He is here: the circle's middle is no place to point at any more, the hut is (its own arrow).
		foreach (PointOfInterest site in Sites)
			site.Done = true;
	}

	protected override void Tick(float delta)
	{
		if (tiredFor >= 0f)
		{
			Tired(delta);
			return;
		}
		mover.Tire = progress;
		Walk(delta);
		if (Player != null && PlayerWithin(mover.Position, KeepRadius))
			progress = Mathf.Min(1f, progress + delta / HuntSeconds);
		if (progress >= 1f)
		{
			// Tired: it stops where it is and will not run again.
			tiredFor = 0f;
			mover.Stopped = true;
			SfxPlayer.Impact(Element.Earth, mover.Position);
		}
	}

	// One step of the mover, and what follows from it: the walk's frame, a fall of a foot, a print.
	private void Walk(float delta)
	{
		Vector2 him = Player?.GlobalPosition ?? (mover.Position + Vector2.Down * 5000f);
		float speed = Player?.WalkingSpeed ?? 220f;
		float moved = mover.Step(delta, him, speed);
		if (moved > 0f)
		{
			walkPhase += moved / (FallSpacing * 2f);
			walkedSinceFall += moved;
			if (walkedSinceFall >= FallSpacing)
			{
				walkedSinceFall -= FallSpacing;
				Fall();
			}
		}
		PlaceHut();
	}

	// A foot comes down: a fresh print where it fell, pointing the way it is going, and the ground takes the weight.
	private void Fall()
	{
		footSide = -footSide;
		Vector2 heading = mover.Heading;
		Vector2 at = mover.Position + new Vector2(-heading.Y, heading.X) * (16f * footSide);
		fresh[freshNext] = new Print { At = at, Direction = PrintDirection(heading), LaidAt = Clock, Ground = 1 };
		freshNext = (freshNext + 1) % FreshCapacity;
		freshCount = Math.Min(FreshCapacity, freshCount + 1);
		FreshPrintsLaid++;
		if (Player != null && Player.GlobalPosition.DistanceSquaredTo(at) < 900f * 900f)
			SfxPlayer.Impact(Element.Earth, at);
	}

	private void Tired(float delta)
	{
		tiredFor += delta;
		if (!succeeded && tiredFor >= (PaysChest ? OpenAt + 0.2f : FreedAt))
		{
			succeeded = true;
			Succeed(mover.Position + Threshold);
		}
		PlaceHut();
	}

	protected override void OnStoodDown()
	{
		mover.Stopped = true;
		PlaceHut();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		// After the win the hut stays knelt with its door open; she goes into the column of light.
		if (Succeeded && her != null && IsInstanceValid(her))
		{
			freedFor += (float)delta;
			PlaceHut();
		}
	}

	// The sprite to where the mover is, on the art grid, and the frame for what it is doing.
	private void PlaceHut()
	{
		Vector2 foot = SpellInk.Snap(mover.Position);
		if (hut == null || !IsInstanceValid(hut))
		{
			skullAt = foot + EyesStanding;
			return;
		}
		hut.Position = new Vector2(foot.X, foot.Y - WorldSprites.SortLift);
		int frame;
		Vector2 eyes = EyesStanding;
		if (tiredFor >= 0f)
		{
			float t = tiredFor;
			hut.FlipH = false;
			if (t < TurnAt) frame = FrameStand;
			else if (t < FaceAt) { frame = FrameSide; eyes = EyesSide; }
			else if (t < KneelAt) frame = FrameFront;
			else if (t < KneltAt) { frame = FrameKneelHalf; eyes = EyesKneelHalf; }
			else if (t < AjarAt) { frame = FrameKnelt; eyes = EyesKnelt; }
			else if (t < OpenAt) { frame = FrameAjar; eyes = EyesKnelt; }
			else { frame = FrameOpen; eyes = EyesKnelt; }
			UpdateHer(foot, t);
		}
		else if (mover.Speed > 1f)
		{
			frame = FrameWalk + ((int)(walkPhase * WalkFrames) % WalkFrames);
			// Its back is to him whichever way it goes; it leans the way it walks.
			if (Mathf.Abs(mover.Heading.X) > 0.2f)
				hut.FlipH = mover.Heading.X < 0f;
		}
		else
			frame = FrameStand + ((int)(Clock / 0.7f) % 2);
		hut.Frame = Mathf.Clamp(frame, 0, hut.Hframes - 1);
		skullAt = foot + new Vector2(hut.FlipH ? -eyes.X : eyes.X, eyes.Y);
	}

	// She steps out of the open door and a little way toward him, and once she is free goes into the light.
	private void UpdateHer(Vector2 foot, float t)
	{
		if (her == null || !IsInstanceValid(her))
			return;
		if (PaysChest || t < StepOutAt)
		{
			her.Visible = false;
			return;
		}
		float out_ = Mathf.Clamp((t - StepOutAt) / (FreedAt - StepOutAt), 0f, 1f);
		float eased = out_ * out_ * (3f - 2f * out_);
		// Sorted just in front of the hut (by a node a pixel below its feet), drawn up at the threshold, and coming down
		// off it toward him.
		Vector2 at = foot + Threshold + new Vector2(0f, 46f * eased);
		her.Visible = true;
		her.Position = new Vector2(at.X, foot.Y + 2f - WorldSprites.SortLift);
		her.Offset = new Vector2(-24f, WorldSprites.SortLift / 2f - 46f + (at.Y - foot.Y - 2f) / 2f);
		float a = Mathf.Clamp(out_ * 3f, 0f, 1f);
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

	// What is drawn on the ground: its prints (old, then fresh over them), and while the hunt runs, the ring it must be
	// kept inside, filling as the bar does. World units: the node is at the run's origin.
	public override void _Draw()
	{
		if (Player == null)
			return;
		Vector2 him = Player.GlobalPosition;
		const float Reach = 1300f;
		if (printsTex != null)
		{
			for (int i = 0; i < oldPrints.Count; i++)
			{
				Print p = oldPrints[i];
				if (p.At.DistanceSquaredTo(him) > Reach * Reach)
					continue;
				if (p.Ground == 0)
				{
					p.Ground = (sbyte)(Game != null && Game.IsGroundBlockedAt(p.At) ? -1 : 1);
					oldPrints[i] = p;
				}
				if (p.Ground > 0)
					DrawPrint(p.At, p.Direction, old: true);
			}
			for (int i = 0; i < freshCount; i++)
			{
				Print p = fresh[i];
				if (p.At.DistanceSquaredTo(him) > Reach * Reach)
					continue;
				DrawPrint(p.At, p.Direction, old: Clock - p.LaidAt > FreshSeconds);
			}
		}
		if (IsFound && !IsResolved && tiredFor < 0f)
		{
			bool inside = PlayerWithin(mover.Position, KeepRadius);
			DrawProgressArc(mover.Position, KeepRadius, progress, TellColor, 3f, inside ? 0.42f : 0.24f);
		}
	}

	private void DrawPrint(Vector2 world, int direction, bool old)
	{
		Vector2 at = SpellInk.Snap(ToLocal(world)) - new Vector2(PrintCell, PrintCell);
		DrawTextureRectRegion(printsTex, new Rect2(at, new Vector2(PrintCell * 2f, PrintCell * 2f)),
			new Rect2(direction * PrintCell, old ? PrintCell : 0, PrintCell, PrintCell));
	}

	// The far tell stands up from the skull's eyes, wherever the hut has walked to: full while nobody has come, dim once
	// the hunt is on. And always, near, the eyes themselves - his mark, a cold point each.
	protected override void DrawLight(CanvasItem light)
	{
		if (IsResolved && freedFor > 1.5f)
			return;
		if (!NearPlayer(skullAt))
			return;
		float strength = IsDormant ? 1f : (tiredFor >= 0f ? Mathf.Clamp(1f - tiredFor / 2f, 0f, 0.4f) : 0.4f);
		if (strength > 0f && !IsResolved)
			DrawFarTell(light, skullAt, TellColor, strength * Flicker(5));
		Vector2 eyes = ToLocal(skullAt);
		ExploreArt.DrawGlow(light, eyes, 26f, new Color(TellColor, 0.32f * Flicker(2)));
		ExploreArt.DrawGlow(light, eyes, 9f, new Color(TellColor.Lerp(Colors.White, 0.4f), 0.5f * Flicker(4)));
	}
}
