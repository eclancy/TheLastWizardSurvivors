using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// The Scorched Sands' second side event: the dry tomb and its three beacons (.ai/chapter-wizards.md 4.6).
//
// "He hid the water." The builder who could find water in the desert and raise it - Imhotep, architect of the first
// pyramid, physician, High Priest of Ra - is sealed in a dry tomb beside the road the water left, his mark on its
// slab. Three signal beacons stand on the dunes round it, BeaconSpacing apart, cold.
//
// What it asks: KINDLE. Stand by a beacon for KindleSeconds and it catches; a lit beacon burns BurnSeconds before the
// dry wind puts it out. With all three alight at once, the slab slides into the wall and he climbs out. The order is
// the player's: three fires, each its own clock, and the question is the route - which one first, and how to get
// through the dead between them before the first goes out. The moot (MootFiresEvent) is the nearest thing in the
// game and its opposite: there the circle lights its next stone and he runs to it in its order; here nothing tells
// him where to go next, and what matters is that the three burn together.
//
// THE LIGHT DRAWS THE DEAD. When a beacon catches, WaveSize of the chapter's own come in at it out of the dark
// (SpawnRing round the beacon) - so the beacon he has just lit is the worst place to stand, and the way to the next
// runs through whatever the last one called.
//
// NOTHING LOCKS, NOTHING IS LOST. A beacon that goes out is cold again and can be lit again, as often as it takes;
// kindling one he steps off drains back rather than resetting. Nothing here fails the event.
//
// NOTHING HERE IS A TARGET. The tomb and the beacons are sprites sorted among the bodies at their feet, never in
// "enemies". A beacon's pillar has a trunk's footprint on the ground's physics layer, as the moot's stones do, so
// the boots and the swarm meet it; the tomb blocks nothing (it is a facade on the sand, like the nine mouths' rock),
// so nothing can be wedged behind it. Both outlast the event: won or stood down they are handed to the run.
//
// Art: tools/art/tomb_beacons.py (world/tomb/: the tomb's four frames, the beacon's eight, poi-tomb). His mark on the
// slab is the one cold light, and the far tell stands up from it; the beacons' fire is warm, the wizards' light.
public partial class TombBeaconsEvent : SideEvent
{
	/// <summary>The beacons round the tomb (4.6: three).</summary>
	public const int BeaconCount = 3;

	/// <summary>World px between two beacons (4.6: about 700). They stand on a triangle round the tomb.</summary>
	public const float BeaconSpacing = 700f;

	/// <summary>How far each beacon stands from the tomb: the triangle's circumradius, BeaconSpacing / sqrt(3).</summary>
	public const float RingRadius = 404.1452f;

	/// <summary>Seconds of standing by a cold beacon to light it (4.6: 1.5).</summary>
	public const float KindleSeconds = 1.5f;

	/// <summary>"Stand by one": this close to its foot.</summary>
	public const float KindleRadius = 100f;

	/// <summary>Kindling he steps off drains at this share of the rate it fills - back, not to nothing at once.</summary>
	public const float KindleDrainShare = 0.5f;

	/// <summary>Seconds a lit beacon burns before the dry wind puts it out (4.6: 25).</summary>
	public const float BurnSeconds = 25f;

	/// <summary>The last seconds of a burn, when the flames sink and gutter: the clock made visible on the fire itself.</summary>
	public const float GutterSeconds = 5f;

	/// <summary>The dead the light draws when a beacon catches, and how far out they come in from.</summary>
	public const int WaveSize = 6;
	public const float WaveRadius = 420f;

	/// <summary>A beacon's pillar on the ground's physics layer: a trunk's, so a body slides off it.</summary>
	public const float BeaconFootRadius = 14f;

	protected override string Announcement => "Three cold beacons stand round a sealed tomb in the sand.";
	protected override string ChestLine => "The tomb stands open - a chest was left on its stair";

	protected override string Objective
	{
		get
		{
			if (IsResolved)
				return string.Empty;
			if (openFor >= 0f)
				return "The tomb opens";
			int lit = LitCount;
			if (lit == 0)
				return $"Light the three beacons together  (0 of {BeaconCount} alight)";
			float soonest = float.MaxValue;
			foreach (Beacon b in beacons)
				if (b.Lit)
					soonest = Mathf.Min(soonest, b.BurnLeft);
			return $"Light the three beacons together  ({lit} of {BeaconCount} alight, {Mathf.CeilToInt(soonest)}s)";
		}
	}

	// Away from the tomb, the tomb; at it, the nearest beacon still cold - the arrow never says which order, only where
	// the nearest cold fire is. Once all three burn, the tomb again. Null once it is over: that is what says it is over.
	protected override Vector2? PointerTarget
	{
		get
		{
			if (IsResolved)
				return null;
			if (Player == null || openFor >= 0f || !PlayerWithin(tombAt, RingRadius + 320f))
				return tombAt + MarkOffset;
			Beacon best = null;
			float bestD = float.MaxValue;
			foreach (Beacon b in beacons)
			{
				if (b.Lit)
					continue;
				float d = b.Foot.DistanceSquaredTo(Player.GlobalPosition);
				if (d < bestD)
				{
					bestD = d;
					best = b;
				}
			}
			return best != null ? best.Foot + new Vector2(0f, -60f) : tombAt + MarkOffset;
		}
	}

	protected override string SiteGlyph => "tomb";
	// The whole triangle is kept clear of decor and stands on open ground.
	protected override float SiteFootprint => RingRadius + 90f;
	// He has come when the tomb and the near beacon are on screen.
	protected override float ArrivalRadius => 420f;
	// His mark on the slab: the arcane ramp's mid #C08CFF, cold, his light.
	protected override Color TellColor => new(0.753f, 0.549f, 1.0f);

	// The beacons' fire: fire's ramp (bonelight.ELEMENTS["fire"]) - core, hot, mid, edge.
	private static readonly Color FireCore = new(1f, 0.957f, 0.816f);
	private static readonly Color FireHot = new(1f, 0.784f, 0.282f);
	private static readonly Color FireMid = new(0.910f, 0.478f, 0.102f);
	private static readonly Color FireEdge = new(0.722f, 0.220f, 0.102f);

	// --- state ---------------------------------------------------------------------------------------

	private sealed class Beacon
	{
		public Vector2 Foot;
		public Node2D Node;
		public Sprite2D Sprite;
		public float Kindle;        // 0 to 1 while cold
		public float BurnLeft;      // seconds, above 0 while lit
		public float WentOutAt = -100f;
		public float Phase;
		public bool Lit => BurnLeft > 0f;
	}

	private readonly List<Beacon> beacons = new();
	private Vector2 tombAt;
	private Node2D tombNode;
	private Sprite2D tombSprite;
	private Sprite2D him;
	private float openFor = -1f;
	private float freedFor;
	private bool succeeded;
	private bool handedOver;

	/// <summary>The tomb's foot (the site). For the probe and _UiShot.</summary>
	public Vector2 TombPosition => tombAt;

	/// <summary>Where beacon <paramref name="i"/> stands (its foot). For the probe and _UiShot.</summary>
	public Vector2 BeaconAt(int i) => beacons[i].Foot;

	public bool IsLit(int i) => beacons[i].Lit;
	public float KindleOf(int i) => beacons[i].Kindle;
	public float BurnLeftOf(int i) => beacons[i].BurnLeft;

	/// <summary>How many burn now.</summary>
	public int LitCount
	{
		get
		{
			int n = 0;
			foreach (Beacon b in beacons)
				if (b.Lit)
					n++;
			return n;
		}
	}

	/// <summary>Tallies for the probe: beacons caught, beacons the wind put out, the dead the light drew.</summary>
	public int TimesLit { get; private set; }
	public int TimesWentOut { get; private set; }
	public int DeadDrawn { get; private set; }

	/// <summary>True from the moment all three burn together: the slab is sliding, or he is out.</summary>
	public bool IsOpening => openFor >= 0f;

	/// <summary>Where to stand to photograph the opening: down and to one side of the door, so he climbs out toward
	/// him and the whole front is on screen. For _UiShot.</summary>
	public Vector2 StandPoint => tombAt + new Vector2(130f, 150f);

	// --- placement -----------------------------------------------------------------------------------

	/// <summary>
	/// The tomb on open ground over the whole triangle (the band swept for one, as the seal's courtyard is), then the
	/// three beacons on it, the triangle turned by the site so a seed lays out the same three - each walked in or out
	/// along its spoke to open ground if it fell on rock or water, as the moot's stones are.
	/// </summary>
	public override void PlaceSites(ExplorePlacer placer)
	{
		tombAt = AddSite(placer.EventSiteInBand(SiteFootprint)).Position;
		beacons.Clear();
		int seed = Mathf.Abs((int)(tombAt.X * 7f + tombAt.Y * 13f));
		float phase = (seed % 360) * Mathf.Pi / 180f;
		for (int i = 0; i < BeaconCount; i++)
		{
			float a = phase + i * Mathf.Tau / BeaconCount;
			Vector2 at = tombAt + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * RingRadius;
			beacons.Add(new Beacon { Foot = OpenAlongSpoke(placer, at), Phase = i * 0.37f });
		}
	}

	private Vector2 OpenAlongSpoke(ExplorePlacer placer, Vector2 at)
	{
		Vector2 spoke = (at - tombAt).Normalized();
		foreach (float d in new[] { 0f, -50f, 50f, -100f, 100f })
		{
			Vector2 p = at + spoke * d;
			if (placer.IsClear(p, 50f))
				return p;
		}
		return at;
	}

	// --- what stands there ---------------------------------------------------------------------------

	private const string TombPath = "res://assets/bonelight/world/tomb/tomb.png";
	private const string BeaconPath = "res://assets/bonelight/world/tomb/beacon.png";
	private const string HimPath = "res://assets/bonelight/characters/imhotep-1.png";

	// tools/art/tomb_beacons.py: the tomb 88x72 a frame, its middle on column 44, its foot on row 64; the beacon 28x52,
	// its middle on column 14, its foot on row 48, the basket's rim on row 22. Drawn at x2.
	private const int TombW = 88;
	private const int TombMiddle = 44;
	private const int TombFoot = 64;
	private const int BeaconW = 28;
	private const int BeaconMiddle = 14;
	private const int BeaconFoot = 48;
	private const int BasketRow = 22;
	// The beacon's frames (tomb_beacons.py beacon()).
	private const int FrameCold = 0;
	private const int FrameKindling = 1;
	private const int FrameBurning = 2;
	private const int FrameGutter = 6;

	// His mark on the slab, from the tomb's foot, in world px (the art at x2): column 44, row 49.
	private static readonly Vector2 MarkOffset = new(0f, -30f);
	// Where the fire sits on a beacon, from its foot: the basket's rim, and a little above it for the flame's heart.
	private static readonly Vector2 FireOffset = new(0f, -(BeaconFoot - BasketRow) * 2f - 10f);

	// The opening, seconds after all three caught: the slab slides a third, two thirds, away; he climbs out; free.
	private const float SlideThirdAt = 0.5f;
	private const float SlideTwoThirdsAt = 1.0f;
	private const float OpenAt = 1.5f;
	private const float ClimbAt = 1.7f;
	private const float FreedAt = 2.7f;

	protected override void OnPlaced()
	{
		TextureFilter = TextureFilterEnum.Nearest;
		Texture2D tombTex = ResourceLoader.Exists(TombPath) ? GD.Load<Texture2D>(TombPath) : null;
		Texture2D beaconTex = ResourceLoader.Exists(BeaconPath) ? GD.Load<Texture2D>(BeaconPath) : null;
		Texture2D himTex = ResourceLoader.Exists(HimPath) ? GD.Load<Texture2D>(HimPath) : null;
		if (tombTex == null || beaconTex == null || himTex == null)
			GD.PushError("TombBeaconsEvent: the tomb's art is missing or not imported (python tools/art/tomb_beacons.py, then import).");

		tombNode = new Node2D
		{
			Name = "Tomb",
			Position = new Vector2(Mathf.Round(tombAt.X), Mathf.Round(tombAt.Y - WorldSprites.SortLift)),
			// Under this node's -5, so the tomb sorts with the actors at 0.
			ZIndex = 5,
			YSortEnabled = true,
		};
		if (tombTex != null)
		{
			tombSprite = new Sprite2D
			{
				Name = "Front",
				Texture = tombTex,
				Hframes = Math.Max(1, tombTex.GetWidth() / TombW),
				Centered = false,
				Scale = new Vector2(2f, 2f),
				TextureFilter = TextureFilterEnum.Nearest,
				Offset = new Vector2(-TombMiddle, WorldSprites.SortLift / 2f - TombFoot),
			};
			tombNode.AddChild(tombSprite);
		}
		if (himTex != null)
		{
			// Him, for the moment he climbs out: the same sheet as his portrait (48 cell, feet on row 46), sorted a pixel
			// in front of the tomb's foot.
			him = new Sprite2D
			{
				Name = "Him",
				Texture = himTex,
				Centered = false,
				Scale = new Vector2(2f, 2f),
				TextureFilter = TextureFilterEnum.Nearest,
				Position = new Vector2(0f, 2f),
				Offset = new Vector2(-24f, WorldSprites.SortLift / 2f - 46f),
				Visible = false,
			};
			tombNode.AddChild(him);
		}
		AddChild(tombNode);

		foreach (Beacon b in beacons)
		{
			b.Node = new Node2D
			{
				Name = "Beacon",
				Position = new Vector2(Mathf.Round(b.Foot.X), Mathf.Round(b.Foot.Y - WorldSprites.SortLift)),
				ZIndex = 5,
			};
			if (beaconTex != null)
			{
				b.Sprite = new Sprite2D
				{
					Texture = beaconTex,
					Hframes = Math.Max(1, beaconTex.GetWidth() / BeaconW),
					Centered = false,
					Scale = new Vector2(2f, 2f),
					TextureFilter = TextureFilterEnum.Nearest,
					Offset = new Vector2(-BeaconMiddle, WorldSprites.SortLift / 2f - BeaconFoot),
				};
				b.Node.AddChild(b.Sprite);
			}
			var body = new StaticBody2D
			{
				Position = new Vector2(0f, WorldSprites.SortLift),
				CollisionLayer = 1u << (LevelTilePainter.TerrainCollisionLayer - 1),
				CollisionMask = 0,
			};
			body.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = BeaconFootRadius } });
			b.Node.AddChild(body);
			AddChild(b.Node);
		}
		ShowFrames();
	}

	// --- the rules -----------------------------------------------------------------------------------

	protected override void DormantTick(float delta)
	{
		// He has come close to a beacon before the tomb (the site is the tomb; arriving there is caught by the base).
		if (Player == null)
			return;
		foreach (Beacon b in beacons)
		{
			if (PlayerWithin(b.Foot, KindleRadius + 180f))
			{
				Arrive();
				return;
			}
		}
	}

	protected override void Begin()
	{
		ShowFrames();
	}

	protected override void Tick(float delta)
	{
		if (openFor >= 0f)
		{
			Opening(delta);
			return;
		}
		foreach (Beacon b in beacons)
		{
			if (b.Lit)
			{
				b.BurnLeft -= delta;
				if (b.BurnLeft <= 0f)
					GoOut(b);
				continue;
			}
			if (PlayerWithin(b.Foot, KindleRadius))
			{
				b.Kindle += delta / KindleSeconds;
				if (b.Kindle >= 1f)
					Light(b);
			}
			else if (b.Kindle > 0f)
				b.Kindle = Mathf.Max(0f, b.Kindle - delta * KindleDrainShare / KindleSeconds);
		}
		if (LitCount == BeaconCount)
		{
			// All three together: the slab goes. The fires hold while it does - they are what opened it.
			openFor = 0f;
			SfxPlayer.Impact(Element.Earth, tombAt);
		}
		ShowFrames();
	}

	private void Light(Beacon b)
	{
		b.Kindle = 0f;
		b.BurnLeft = BurnSeconds;
		TimesLit++;
		SfxPlayer.Cast(Element.Fire, b.Foot + FireOffset);
		// The light draws the dead in at it out of the dark. Not on the last of the three: then the tomb opens.
		if (LitCount < BeaconCount)
		{
			SpawnRing(b.Foot, WaveSize, WaveRadius);
			DeadDrawn += WaveSize;
		}
	}

	private void GoOut(Beacon b)
	{
		b.BurnLeft = 0f;
		b.Kindle = 0f;
		b.WentOutAt = Clock;
		TimesWentOut++;
		SfxPlayer.Cast(Element.Wind, b.Foot + FireOffset);
	}

	private void Opening(float delta)
	{
		openFor += delta;
		if (!succeeded && openFor >= (PaysChest ? OpenAt + 0.2f : FreedAt))
		{
			succeeded = true;
			Succeed(tombAt + new Vector2(0f, 40f));
		}
		ShowFrames();
	}

	/// <summary>
	/// Lights the first <paramref name="count"/> beacons where they stand, as if he had stood by each, with the later
	/// ones lit later (their clocks staggered), and lets the real path run from there - all three, and the tomb opens.
	/// For _UiShot (--beacons-lit, --win-event); a run never calls it.
	/// </summary>
	public void KindleForPhoto(int count)
	{
		if (IsDormant)
			Arrive();
		for (int i = 0; i < Math.Min(count, beacons.Count); i++)
		{
			if (beacons[i].Lit)
				continue;
			beacons[i].BurnLeft = BurnSeconds - (count - 1 - i) * 7.5f;
			TimesLit++;
		}
		ShowFrames();
	}

	protected override void OnStoodDown()
	{
		foreach (Beacon b in beacons)
			b.BurnLeft = 0f;
		ShowFrames();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Succeeded && him != null && IsInstanceValid(him))
		{
			freedFor += (float)delta;
			ShowFrames();
		}
		else if (!IsResolved && IsFound)
			ShowFrames();
		if (IsResolved && SecondsSinceResolved >= 4.5f && !handedOver)
			StandOnTheSand();
	}

	// The tomb and the beacons outlast the event: the base frees this node a few seconds after it resolves, and they go
	// to the run so they keep standing where they are - the tomb open, the fires burnt down to embers.
	private void StandOnTheSand()
	{
		handedOver = true;
		if (Game == null || !IsInstanceValid(Game))
			return;
		foreach (Beacon b in beacons)
		{
			if (b.Node == null || !IsInstanceValid(b.Node))
				continue;
			if (b.Sprite != null && IsInstanceValid(b.Sprite))
				b.Sprite.Frame = Succeeded ? FrameKindling : FrameCold;
			b.Node.Reparent(Game, true);
			b.Node.ZIndex = 0;
		}
		if (tombNode != null && IsInstanceValid(tombNode))
		{
			if (him != null && IsInstanceValid(him))
				him.QueueFree();
			tombNode.Reparent(Game, true);
			tombNode.ZIndex = 0;
		}
	}

	private void ShowFrames()
	{
		foreach (Beacon b in beacons)
		{
			if (b.Sprite == null || !IsInstanceValid(b.Sprite))
				continue;
			int frame;
			if (b.Lit)
			{
				int flicker = (int)((Clock + b.Phase) * 8f) % 4;
				frame = b.BurnLeft > GutterSeconds || openFor >= 0f
					? FrameBurning + flicker
					: FrameGutter + flicker % 2;
			}
			else
				frame = b.Kindle > 0f ? FrameKindling : FrameCold;
			b.Sprite.Frame = Mathf.Clamp(frame, 0, b.Sprite.Hframes - 1);
		}
		if (tombSprite != null && IsInstanceValid(tombSprite))
		{
			int t = openFor < SlideThirdAt ? 0 : openFor < SlideTwoThirdsAt ? 1 : openFor < OpenAt ? 2 : 3;
			tombSprite.Frame = Mathf.Clamp(t, 0, tombSprite.Hframes - 1);
		}
		UpdateHim();
	}

	// He climbs the stair out of the dark of the door and a little way toward the player, and once he is free goes into
	// the column of light.
	private void UpdateHim()
	{
		if (him == null || !IsInstanceValid(him))
			return;
		if (PaysChest || openFor < ClimbAt)
		{
			him.Visible = false;
			return;
		}
		float up = Mathf.Clamp((openFor - ClimbAt) / (FreedAt - ClimbAt), 0f, 1f);
		float eased = up * up * (3f - 2f * up);
		// Up out of the stair (from below the threshold, so his head comes first) and then down off it toward him.
		him.Visible = true;
		him.Position = new Vector2(0f, 2f + 40f * eased);
		him.Offset = new Vector2(-24f, WorldSprites.SortLift / 2f - 46f + (1f - Mathf.Clamp(up * 2f, 0f, 1f)) * 14f);
		him.RegionEnabled = up < 0.5f;
		if (him.RegionEnabled)
			him.RegionRect = new Rect2(0f, 0f, 48f, 48f - (1f - up * 2f) * 14f);
		float a = Mathf.Clamp(up * 3f, 0f, 1f);
		if (Succeeded)
			a *= Mathf.Clamp(1f - freedFor / 1.2f, 0f, 1f);
		him.Modulate = new Color(1f, 1f, 1f, a);
		if (Succeeded && freedFor >= 1.2f)
		{
			him.QueueFree();
			him = null;
		}
	}

	// --- drawing -------------------------------------------------------------------------------------

	// On the ground round each beacon, while it runs: the reach to stand in, a dashed track; filling as it is kindled;
	// draining as it burns down - the clock on the ground, the guttering on the fire.
	public override void _Draw()
	{
		if (IsResolved || !IsFound || openFor >= 0f || Player == null)
			return;
		foreach (Beacon b in beacons)
		{
			if (!NearPlayer(b.Foot))
				continue;
			bool standing = PlayerWithin(b.Foot, KindleRadius);
			if (b.Lit)
			{
				float left = Mathf.Clamp(b.BurnLeft / BurnSeconds, 0f, 1f);
				DrawProgressArc(b.Foot, KindleRadius, left, b.BurnLeft > GutterSeconds ? FireHot : FireEdge, 3f, 0.24f);
			}
			else
				DrawProgressArc(b.Foot, KindleRadius, b.Kindle, FireMid, 3f, standing ? 0.42f : 0.24f);
		}
	}

	protected override void DrawLight(CanvasItem light)
	{
		Vector2 mark = tombAt + MarkOffset;
		if (IsResolved && SecondsSinceResolved > 2f)
			return;
		if (IsDormant)
		{
			// While nobody has come: his cold far tell over the slab, and nothing at the beacons - they are cold.
			DrawFarTell(light, mark, TellColor, Flicker(3));
			return;
		}
		if (openFor < OpenAt && !IsResolved && NearPlayer(mark))
		{
			float strength = openFor >= 0f ? Mathf.Clamp(1f - openFor / OpenAt, 0f, 1f) * 0.4f : 0.4f;
			DrawFarTell(light, mark, TellColor, strength * Flicker(3));
			Vector2 m = ToLocal(mark);
			ExploreArt.DrawGlow(light, m, 14f, new Color(TellColor.Lerp(Colors.White, 0.4f), 0.5f * Flicker(1)));
		}
		if (openFor >= 0f && NearPlayer(tombAt))
		{
			// The door open: the beacons' light falls into it, and the day comes in on the stair.
			float a = Mathf.Clamp((openFor - SlideThirdAt) / (OpenAt - SlideThirdAt), 0f, 1f);
			Vector2 door = ToLocal(tombAt + new Vector2(0f, -20f));
			ExploreArt.DrawGlow(light, door, 90f, new Color(FireHot, 0.18f * a));
		}
		foreach (Beacon b in beacons)
		{
			if (!NearPlayer(b.Foot))
				continue;
			Vector2 fire = ToLocal(b.Foot + FireOffset);
			if (b.Lit)
			{
				// Warm, and sinking as the wind gets in: a full fire's light until the gutter, then less and less.
				float burn = b.BurnLeft > GutterSeconds || openFor >= 0f ? 1f : 0.35f + 0.65f * b.BurnLeft / GutterSeconds;
				float f = burn * (0.85f + 0.15f * Mathf.Sin((Clock + b.Phase) * 9.1f) * Mathf.Sin((Clock + b.Phase) * 4.3f));
				ExploreArt.DrawGlow(light, fire, 150f * burn, new Color(FireMid, 0.16f * f));
				ExploreArt.DrawGlow(light, fire, 46f, new Color(FireHot, 0.5f * f));
				ExploreArt.DrawGlow(light, ToLocal(b.Foot), 110f * burn, new Color(FireEdge, 0.12f * f));
				// Seen from beyond the edge of the screen: a column over each fire.
				ExploreArt.DrawShaft(light, fire, 14f, 420f * burn, new Color(FireMid, 0.28f * f));
				ExploreArt.DrawShaft(light, fire, 4f, 380f * burn, new Color(FireCore, 0.36f * f));
			}
			else
			{
				// Cold: only a faint cold glow on the basket, so the three can be found by their light before they burn.
				float k = Mathf.Clamp(b.Kindle, 0f, 1f);
				ExploreArt.DrawGlow(light, fire, 60f, new Color(TellColor, 0.07f));
				if (k > 0f)
					ExploreArt.DrawGlow(light, fire, 20f + 26f * k, new Color(FireEdge, 0.45f * k));
				// Just gone out: a wisp of the light still going up off it.
				float since = Clock - b.WentOutAt;
				if (since < 1.6f)
					ExploreArt.DrawShaft(light, fire, 6f, 120f * (1f - since / 1.6f), new Color(FireEdge, 0.25f * (1f - since / 1.6f)));
			}
		}
	}
}
