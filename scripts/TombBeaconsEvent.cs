using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// The Scorched Sands' second side event: the dry tomb (.ai/chapter-wizards.md 4.6), and since 2026-11-08 its bronze
// mirrors (.ai/unlock-puzzles.md 3.1, "route the sun"). The class keeps the beacons' name, as the site keeps its id
// (tomb_beacons), its deed, its hint and its reward: a save that freed him keeps him, and a new save is asked this.
//
// "He hid the water." The builder who could find water in the desert and raise it - Imhotep, architect of the first
// pyramid, physician, High Priest of Ra - is sealed in a dry tomb beside the road the water left, his mark on its
// slab, a winged sun-disc on the cornice over its door.
//
// WHAT IT ASKS: AIM. Sunlight falls through a gap in the dunes onto one bronze mirror, the source; four or five more
// stand on the sand round the tomb (SunMirrors, rolled from the seed). A mirror faces one of eight ways, and while he
// stands within reach of it, it swings to face AWAY from him: walk round it and its face follows, and its light goes
// the other way. A mirror the light reaches shines where it faces. Bring the light mirror to mirror onto the sun-disc
// and the slab slides into the wall - the beacons' win beat, unchanged. It is the Halls of Torment laser puzzle in his
// own legend; his form is already the Eye of Ra.
//
// THE LIGHT BURNS. It passes through bodies and burns each one on a lit leg for BurnDamage every BurnSeconds (3.1,
// Eric: "the beam should not be stopped by the bodies, just burn them lightly as it passes through them"); only terrain,
// the tomb and the mirrors stop it. So the light is a weapon while he works, which pays for the minute it takes.
//
// NOTHING LOCKS, NOTHING IS LOST. A wrong facing sends the light somewhere useless; turn it back. Every layout is
// proved on paper (SunMirrors.Proves): solvable with exactly its path's mirrors, and no single mirror turned wrong
// reaches the socket.
//
// NOTHING HERE IS A TARGET. The tomb, the mirrors and the socket are sprites sorted among the bodies at their feet,
// never in "enemies". A mirror's stand has a trunk's footprint on the ground's physics layer, as the beacons' pillars
// did, so the boots and the swarm meet it; it does not stop the light (a mirror catches it). The tomb blocks nothing.
// All of it outlasts the event: won or stood down it is handed to the run.
//
// Art: tools/art/tomb_beacons.py (world/tomb/: the tomb's four frames, the mirror's eight facings, the socket's two,
// poi-tomb). The light is drawn here: a warm gold line in the air layer (the wizards' light is warm, his is cold,
// .ai/world-and-tone.md), the shaft of sunlight on the source on the light layer. His mark on the slab stays the one
// cold light, where the far tell stands up from. Sound: mirror_turn as a facing changes, sun_arrive as the light lands
// on a mirror and on the disc (tools/audio/sfx.py).
public partial class TombBeaconsEvent : SideEvent
{
	/// <summary>What the light does to a body on a lit leg, and how often (3.1: first guess 3 every 0.5 s).</summary>
	public const int BurnDamage = 3;
	public const float BurnSeconds = 0.5f;

	/// <summary>A body whose feet are this near a lit leg's line on the ground is in the light.</summary>
	public const float BurnReach = 24f;

	/// <summary>He has come when he is this near any mirror - a player who follows the light to the source arrives there.</summary>
	public const float MirrorArrival = 260f;

	// How fast a turned disc swings round to its new facing on screen (the light moves at once).
	private const float SwingStepSeconds = 0.05f;
	// The fewest seconds between two turning sounds, and between two arrivals of the light.
	private const float TurnSoundGap = 0.14f;
	private const float ArriveSoundGap = 0.2f;
	// The last stretch of a beam that runs out with nothing in its way, which fades.
	private const float FadeTail = 140f;
	// The rolls the band's sweep allows a candidate tomb before it moves on (the chosen one's layout is kept).
	private const int SiteAttempts = 16;

	protected override string Announcement => "Bronze mirrors stand on the sand round a sealed tomb.";
	protected override string ChestLine => "The tomb stands open - a chest was left on its stair";

	protected override string Objective
	{
		get
		{
			if (IsResolved)
				return string.Empty;
			if (openFor >= 0f)
				return "The sun is on the disc - the tomb opens";
			if (dark)
				return string.Empty;
			if (nearMirror >= 0)
				return "The mirror turns away from you - walk round it to aim it";
			return "Bring the sunlight, mirror to mirror, to the sun-disc over the tomb door";
		}
	}

	// Away from the whole layout, the tomb. Among it, where the light ends now: the mirror to turn next, or the dead end it
	// went into - never which way to turn it, and never a mirror the light has not reached.
	protected override Vector2? PointerTarget
	{
		get
		{
			if (IsResolved)
				return null;
			if (Player == null || layout == null || openFor >= 0f || dark || chainLength == 0 || !PlayerAmongMirrors())
				return tombAt + MarkOffset;
			return Face(layout.Mirrors[chain[chainLength - 1]]);
		}
	}

	protected override string SiteGlyph => "tomb";
	// The tomb and its door: the mirrors keep their own ground (SunMirrors.MirrorFootprint), reserved below.
	protected override float SiteFootprint => 200f;
	// He has come when the tomb is on screen (or, HasArrived, when he is at any mirror).
	protected override float ArrivalRadius => 420f;
	// His mark on the slab: the arcane ramp's mid #C08CFF, cold, his light.
	protected override Color TellColor => new(0.753f, 0.549f, 1.0f);

	// The sun: the light element's ramp (bonelight.ELEMENTS["light"]) - core, hot, mid, edge. Warm gold.
	private static readonly Color SunCore = new(1f, 1f, 1f);
	private static readonly Color SunHot = new(1f, 0.973f, 0.847f);
	private static readonly Color SunMid = new(1f, 0.941f, 0.659f);
	private static readonly Color SunEdge = new(1f, 0.776f, 0.235f);

	// --- state ---------------------------------------------------------------------------------------

	private sealed class MirrorStand
	{
		public Node2D Node;
		public Sprite2D Sprite;
		public int Shown;
		public float Swing;
	}

	private SunMirrors layout;
	private readonly List<MirrorStand> stands = new();
	private int[] facing;
	private bool[] lit;
	private bool[] wasLit;
	private float[] litFor;
	private int[] chain;
	private int chainLength;
	private bool socketLit;
	private int nearMirror = -1;
	private bool dark;
	private float burnClock;
	private float lastTurnSound = -10f;
	private float lastArriveSound = -10f;
	private readonly List<Enemy> burnBuffer = new(64);
	private readonly HashSet<ulong> burned = new();

	private Vector2 tombAt;
	private Node2D tombNode;
	private Sprite2D tombSprite;
	private Sprite2D socketSprite;
	private Sprite2D him;
	private Node2D beamNode;
	private float openFor = -1f;
	private float freedFor;
	private bool succeeded;
	private bool handedOver;

	/// <summary>The tomb's foot (the site). For the probe and _UiShot.</summary>
	public Vector2 TombPosition => tombAt;

	/// <summary>The run's mirrors: where they stand, the path, the solution, every beam on paper. For the probe.</summary>
	public SunMirrors Layout => layout;

	public int MirrorCount => layout?.MirrorCount ?? 0;
	public Vector2 MirrorAt(int i) => layout.Mirrors[i];
	public int FacingOf(int i) => facing[i];
	public bool IsLit(int i) => lit[i];

	/// <summary>How many mirrors the light goes through now, the source included.</summary>
	public int LitCount => chainLength;

	/// <summary>The mirror the light ends at now.</summary>
	public int LightEndsAt => chainLength > 0 ? chain[chainLength - 1] : 0;

	/// <summary>True while the light is on the sun-disc.</summary>
	public bool SocketLit => socketLit;

	/// <summary>Tallies for the probe: facings changed, mirrors (and decoys) the light newly reached, burns dealt.</summary>
	public int TimesTurned { get; private set; }
	public int MirrorsCaught { get; private set; }
	public int DecoysCaught { get; private set; }
	public int BurnHits { get; private set; }
	public int BurnTicks { get; private set; }

	/// <summary>True from the moment the light reaches the disc: the slab is sliding, or he is out.</summary>
	public bool IsOpening => openFor >= 0f;

	/// <summary>Where to stand to photograph the opening: down and to one side of the door, so he climbs out toward
	/// him and the whole front is on screen. For _UiShot.</summary>
	public Vector2 StandPoint => tombAt + new Vector2(130f, 150f);

	// --- placement -----------------------------------------------------------------------------------

	/// <summary>
	/// The tomb on open ground in the band, swept for where a layout of mirrors fits round it (SunMirrors.Generate, from a
	/// seed drawn before anything is tested); then the decor kept off every mirror and every leg of the light's path.
	/// </summary>
	public override void PlaceSites(ExplorePlacer placer)
	{
		ulong seed = ((ulong)(placer.NextFloat() * 4294967295f) << 32) | (ulong)(placer.NextFloat() * 4294967295f);
		var ground = new SunMirrors.Ground
		{
			// The same test the light is held to in the run: what stops a body stops the light (Node2DGame.IsGroundBlockedAt).
			Blocked = p => !placer.IsWalkable(p),
			Clear = placer.IsClear,
			Allowed = placer.IsOffOtherSites,
		};
		SunMirrors found = null;
		Vector2 site = placer.EventSiteInBand(SiteFootprint,
			also: p => (found = SunMirrors.Generate(p, seed, ground, maxAttempts: SiteAttempts)) != null);
		tombAt = AddSite(site).Position;
		layout = found != null && found.Tomb == tombAt ? found : SunMirrors.Generate(tombAt, seed, ground);
		if (layout == null)
		{
			GD.PushWarning($"TombBeaconsEvent: no layout of mirrors on open ground round the tomb at {tombAt}; laying one without the footprints.");
			layout = SunMirrors.Generate(tombAt, seed, ground, relaxed: true);
		}
		if (layout == null)
		{
			GD.PushError($"TombBeaconsEvent: no layout of mirrors round the tomb at {tombAt} keeps the light off the terrain; laying one on open ground.");
			layout = SunMirrors.Generate(tombAt, seed, SunMirrors.Ground.Open, relaxed: true);
		}
		// Decor keeps off every mirror and every leg of the light's path (Node2DGame.ReserveExploreGround).
		foreach (Vector2 m in layout.Mirrors)
			placer.Reserve(m, SunMirrors.MirrorFootprint * 0.9f);
		for (int k = 0; k < layout.Legs; k++)
		{
			Vector2 a = layout.Mirrors[layout.Path[k]];
			Vector2 b = k + 1 < layout.Legs ? layout.Mirrors[layout.Path[k + 1]] : tombAt;
			float len = a.DistanceTo(b);
			for (float s = 80f; s < len - 40f; s += 80f)
				placer.Reserve(a + (b - a) * (s / len), 46f);
		}

		int n = layout.MirrorCount;
		facing = new int[n];
		for (int i = 0; i < n; i++)
			facing[i] = layout.Start[i];
		lit = new bool[n];
		wasLit = new bool[n];
		litFor = new float[n];
		chain = new int[n];
		Relight(sound: false);
	}

	// Among the mirrors: near the tomb or any mirror.
	private bool PlayerAmongMirrors()
	{
		if (PlayerWithin(tombAt, 700f))
			return true;
		foreach (Vector2 m in layout.Mirrors)
			if (PlayerWithin(m, 700f))
				return true;
		return false;
	}

	protected override bool HasArrived()
	{
		if (base.HasArrived())
			return true;
		if (layout == null || IsResolved)
			return false;
		foreach (Vector2 m in layout.Mirrors)
			if (PlayerWithin(m, MirrorArrival))
				return true;
		return false;
	}

	// --- what stands there ---------------------------------------------------------------------------

	private const string TombPath = "res://assets/bonelight/world/tomb/tomb.png";
	private const string MirrorPath = "res://assets/bonelight/world/tomb/mirror.png";
	private const string SocketPath = "res://assets/bonelight/world/tomb/socket.png";
	private const string HimPath = "res://assets/bonelight/characters/imhotep-1.png";

	// tools/art/tomb_beacons.py: the tomb 88x72 a frame, its middle on column 44, its foot on row 64; the mirror 32x48,
	// its middle on column 16, its foot on row 44, its disc's middle on row 12; the socket 30x14 with its middle at (15, 7),
	// set on the tomb's (44, 32). Drawn at x2.
	private const int TombW = 88;
	private const int TombMiddle = 44;
	private const int TombFoot = 64;
	private const int MirrorW = 32;
	private const int MirrorMiddle = 16;
	private const int MirrorFoot = 44;
	private const int SocketW = 30;
	private const int SocketMiddleX = 15;
	private const int SocketMiddleY = 7;
	// The socket sorts a hair in front of the tomb's front (it is on it), and is lifted to the tomb's row 32 by its offset.
	private const float SocketSortAhead = 2f;
	private const float MirrorStandRadius = 14f;

	// His mark on the slab, from the tomb's foot, in world px (the art at x2): column 44, row 49.
	private static readonly Vector2 MarkOffset = new(0f, -30f);

	// The opening, seconds after the light reached the disc: the slab slides a third, two thirds, away; he climbs out; free.
	private const float SlideThirdAt = 0.5f;
	private const float SlideTwoThirdsAt = 1.0f;
	private const float OpenAt = 1.5f;
	private const float ClimbAt = 1.7f;
	private const float FreedAt = 2.7f;

	// A mirror's disc, and the sun-disc: BeamHeight over their feet, on the world's whole pixels the sprites are on.
	private static Vector2 Face(Vector2 foot) => new(Mathf.Round(foot.X), Mathf.Round(foot.Y) - SunMirrors.BeamHeight);

	/// <summary>The sun-disc's middle in the world. For the probe and _UiShot.</summary>
	public Vector2 SocketFace => Face(tombAt);

	protected override void OnPlaced()
	{
		TextureFilter = TextureFilterEnum.Nearest;
		Texture2D tombTex = Load(TombPath);
		Texture2D mirrorTex = Load(MirrorPath);
		Texture2D socketTex = Load(SocketPath);
		Texture2D himTex = Load(HimPath);
		if (tombTex == null || mirrorTex == null || socketTex == null || himTex == null)
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
		if (socketTex != null)
		{
			// The winged sun-disc on the cornice: its middle on the tomb's row 32, BeamHeight over the foot.
			socketSprite = new Sprite2D
			{
				Name = "Socket",
				Texture = socketTex,
				Hframes = Math.Max(1, socketTex.GetWidth() / SocketW),
				Centered = false,
				Scale = new Vector2(2f, 2f),
				TextureFilter = TextureFilterEnum.Nearest,
				Position = new Vector2(0f, SocketSortAhead),
				Offset = new Vector2(-SocketMiddleX,
					(WorldSprites.SortLift - SocketSortAhead - SunMirrors.BeamHeight) / 2f - SocketMiddleY),
			};
			tombNode.AddChild(socketSprite);
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

		for (int i = 0; i < layout.MirrorCount; i++)
		{
			Vector2 foot = layout.Mirrors[i];
			var stand = new MirrorStand
			{
				Node = new Node2D
				{
					Name = i == 0 ? "SourceMirror" : $"Mirror{i}",
					Position = new Vector2(Mathf.Round(foot.X), Mathf.Round(foot.Y - WorldSprites.SortLift)),
					ZIndex = 5,
				},
				Shown = facing[i],
			};
			if (mirrorTex != null)
			{
				stand.Sprite = new Sprite2D
				{
					Texture = mirrorTex,
					Hframes = Math.Max(1, mirrorTex.GetWidth() / MirrorW),
					Centered = false,
					Scale = new Vector2(2f, 2f),
					TextureFilter = TextureFilterEnum.Nearest,
					Offset = new Vector2(-MirrorMiddle, WorldSprites.SortLift / 2f - MirrorFoot),
				};
				stand.Node.AddChild(stand.Sprite);
			}
			var body = new StaticBody2D
			{
				Position = new Vector2(0f, WorldSprites.SortLift),
				CollisionLayer = 1u << (LevelTilePainter.TerrainCollisionLayer - 1),
				CollisionMask = 0,
			};
			body.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = MirrorStandRadius } });
			stand.Node.AddChild(body);
			AddChild(stand.Node);
			stands.Add(stand);
		}

		// The light, in the air over every body (.ai/godot-engine.md, "Draw layers"): never under them, never sorted among them.
		beamNode = new Node2D
		{
			Name = "Sunlight",
			ZIndex = DrawLayers.Air,
			ZAsRelative = false,
			TextureFilter = TextureFilterEnum.Nearest,
		};
		beamNode.Draw += DrawBeams;
		AddChild(beamNode);
		ShowFrames();
	}

	public override void _ExitTree()
	{
		if (beamNode != null && IsInstanceValid(beamNode))
			beamNode.Draw -= DrawBeams;
		base._ExitTree();
	}

	private static Texture2D Load(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

	// --- the rules -----------------------------------------------------------------------------------

	protected override void Begin()
	{
		GD.Print($"TombBeaconsEvent: {layout.MirrorCount} mirrors ({layout.Legs} on the path, {layout.DecoyCount} decoy(s)), "
			+ $"the fewest that reach the disc {layout.FewestMirrors()}, rolled on attempt {layout.Attempts}"
			+ (layout.Relaxed ? " (relaxed)" : "") + $"; the light starts through {chainLength} mirror(s). From the tomb: "
			+ string.Join(", ", System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0, layout.MirrorCount),
				i => $"{i}{(layout.IsDecoy(i) ? " (decoy)" : "")} at {(layout.Mirrors[i] - tombAt).X:0},{(layout.Mirrors[i] - tombAt).Y:0}")));
		ShowFrames();
	}

	protected override void Tick(float delta)
	{
		if (openFor >= 0f)
		{
			Opening(delta);
			return;
		}
		Player player = Player;
		if (player == null || dark)
			return;

		// THE ONE ACTION: within reach, a mirror faces away from his feet (SunMirrors.FacingAway).
		Vector2 feet = player.FootPosition;
		bool changed = false;
		nearMirror = -1;
		for (int i = 0; i < layout.MirrorCount; i++)
		{
			Vector2 at = layout.Mirrors[i];
			if (feet.DistanceSquaredTo(at) <= SunMirrors.Reach * SunMirrors.Reach)
				nearMirror = i;
			int f = SunMirrors.FacingAway(at, feet, facing[i]);
			if (f == facing[i])
				continue;
			facing[i] = f;
			changed = true;
			TimesTurned++;
			if (Clock - lastTurnSound >= TurnSoundGap)
			{
				lastTurnSound = Clock;
				SfxPlayer.AtPosition(SfxCatalog.MirrorTurn, at, 0.05f);
			}
		}
		if (changed)
			Relight(sound: true);

		burnClock += delta;
		if (burnClock >= BurnSeconds)
		{
			burnClock -= BurnSeconds;
			Burn();
		}

		if (socketLit)
		{
			// The light on the disc: the slab goes. The mirrors hold where they are while it does - they are what opened it.
			openFor = 0f;
			SfxPlayer.Impact(Element.Earth, tombAt);
			GD.Print($"TombBeaconsEvent: the sun is on the disc through {chainLength} mirror(s), {TimesTurned} turn(s) after arriving.");
		}
		ShowFrames();
	}

	// The light traced again from the source (SunMirrors.Trace, on the table worked out at placement: no beam is cast in
	// the run), and every mirror it newly reaches sounds and flares.
	private void Relight(bool sound)
	{
		int n = layout.MirrorCount;
		Array.Copy(lit, wasLit, n);
		bool wasSocket = socketLit;
		socketLit = layout.Trace(facing, chain, out chainLength);
		Array.Clear(lit, 0, n);
		for (int k = 0; k < chainLength; k++)
			lit[chain[k]] = true;
		for (int i = 0; i < n; i++)
		{
			if (!lit[i] || wasLit[i])
				continue;
			litFor[i] = 0f;
			if (!sound)
				continue;
			MirrorsCaught++;
			if (layout.IsDecoy(i))
				DecoysCaught++;
			if (Clock - lastArriveSound >= ArriveSoundGap)
			{
				lastArriveSound = Clock;
				SfxPlayer.AtPosition(SfxCatalog.SunArrive, layout.Mirrors[i], 0.03f);
			}
		}
		if (socketLit && !wasSocket && sound)
			SfxPlayer.AtPosition(SfxCatalog.SunArrive, tombAt, 0f);
	}

	// The light burns every body on a lit leg, once a tick however many legs it stands on: the registry's broad phase into
	// this event's own list (CollectNear - a burn can kill, and a kill can start another walk of the registry, which
	// ForEachNear's shared buffer would not survive), then the live position against the leg's line on the ground.
	private void Burn()
	{
		if (chainLength == 0)
			return;
		BurnTicks++;
		burned.Clear();
		for (int k = 0; k < chainLength; k++)
		{
			int m = chain[k];
			Vector2 a = layout.Mirrors[m];
			Vector2 b = layout.BeamEnd(m, facing[m]);
			EnemyRegistry.CollectNear((a + b) * 0.5f, a.DistanceTo(b) * 0.5f + BurnReach, burnBuffer);
			foreach (Enemy enemy in burnBuffer)
			{
				if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
					continue;
				if (SunMirrors.DistanceToSegment(enemy.GlobalPosition, a, b) > BurnReach)
					continue;
				if (!burned.Add(enemy.GetInstanceId()))
					continue;
				// Every argument passed: Godot matches a duck-typed Call by count, but this is a typed call either way.
				enemy.TakeDamage(BurnDamage, false);
				BurnHits++;
			}
			burnBuffer.Clear();
		}
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
	/// Turns the first <paramref name="legs"/> mirrors of the path to their facings in the solution, as if he had stood
	/// behind each, and lets the real path run from there - all of them, and the light is on the disc and the tomb opens.
	/// For _UiShot (--mirrors-aimed, --win-event); a run never calls it.
	/// </summary>
	public void AimForPhoto(int legs)
	{
		if (IsDormant)
			Arrive();
		for (int k = 0; k < Math.Min(legs, layout.Legs); k++)
		{
			int m = layout.Path[k];
			facing[m] = layout.Solution[m];
			stands[m].Shown = facing[m];
		}
		Relight(sound: false);
		ShowFrames();
	}

	protected override void OnStoodDown()
	{
		dark = true;
		ShowFrames();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		float dt = (float)delta;
		if (layout != null)
		{
			for (int i = 0; i < stands.Count; i++)
			{
				litFor[i] += dt;
				MirrorStand s = stands[i];
				if (s.Shown == facing[i])
				{
					s.Swing = 0f;
					continue;
				}
				// The disc swings round the short way, an eighth at a time.
				s.Swing += dt;
				while (s.Swing >= SwingStepSeconds && s.Shown != facing[i])
				{
					s.Swing -= SwingStepSeconds;
					int ahead = SunMirrors.Wrap(facing[i] - s.Shown);
					s.Shown = SunMirrors.Wrap(s.Shown + (ahead <= 4 ? 1 : -1));
				}
			}
		}
		beamNode?.QueueRedraw();
		if (Succeeded && him != null && IsInstanceValid(him))
		{
			freedFor += dt;
			ShowFrames();
		}
		else
			ShowFrames();
		if (IsResolved && SecondsSinceResolved >= 4.5f && !handedOver)
			StandOnTheSand();
	}

	// The tomb and the mirrors outlast the event: the base frees this node a few seconds after it resolves, and they go to
	// the run so they keep standing where they are - the tomb open and its disc lit, each mirror as he left it.
	private void StandOnTheSand()
	{
		handedOver = true;
		if (Game == null || !IsInstanceValid(Game))
			return;
		foreach (MirrorStand s in stands)
		{
			if (s.Node == null || !IsInstanceValid(s.Node))
				continue;
			s.Node.Reparent(Game, true);
			s.Node.ZIndex = 0;
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
		foreach (MirrorStand s in stands)
		{
			if (s.Sprite != null && IsInstanceValid(s.Sprite))
				s.Sprite.Frame = Mathf.Clamp(s.Shown, 0, s.Sprite.Hframes - 1);
		}
		if (socketSprite != null && IsInstanceValid(socketSprite))
			socketSprite.Frame = Mathf.Clamp(socketLit || openFor >= 0f ? 1 : 0, 0, socketSprite.Hframes - 1);
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

	// How strongly the light shows: full while it runs, fading over two seconds once the event is over, nothing once the
	// boss has stood it down.
	private float LightStrength => dark ? 0f : IsResolved ? Mathf.Clamp(1f - SecondsSinceResolved / 2f, 0f, 1f) : 1f;

	// The legs of light, in the air: a haze, the beam, its white heart, and motes running along it from the source toward
	// where it ends - so which way the light is going reads at a glance. A beam that runs out fades over its last stretch.
	private void DrawBeams()
	{
		float strength = LightStrength;
		if (layout == null || strength <= 0f || Player == null)
			return;
		for (int k = 0; k < chainLength; k++)
		{
			int m = chain[k];
			int f = facing[m];
			Vector2 a = layout.Mirrors[m];
			Vector2 b = layout.BeamEnd(m, f);
			if (!NearPlayer(a) && !NearPlayer(b))
				continue;
			SunMirrors.End end = layout.HitFrom(m, f).Kind;
			Vector2 from = beamNode.ToLocal(Face(a));
			Vector2 to = beamNode.ToLocal(end == SunMirrors.End.Socket ? SocketFace : Face(b));
			float len = from.DistanceTo(to);
			if (len < 1f)
				continue;
			Vector2 u = (to - from) / len;
			float solid = end == SunMirrors.End.Open ? Mathf.Max(0f, len - FadeTail) : len;
			DrawLegStretch(from, from + u * solid, strength);
			if (solid < len)
			{
				const int pieces = 5;
				float piece = (len - solid) / pieces;
				for (int p = 0; p < pieces; p++)
					DrawLegStretch(from + u * (solid + p * piece), from + u * (solid + (p + 1) * piece), strength * (1f - (p + 0.5f) / pieces));
			}
			float phase = Mathf.PosMod(Clock * 260f + k * 37f, 96f);
			for (float s = phase; s < solid - 10f; s += 96f)
				beamNode.DrawLine(from + u * s, from + u * (s + 10f), new Color(SunCore, 0.95f * strength), 4f);
		}
	}

	private void DrawLegStretch(Vector2 a, Vector2 b, float strength)
	{
		beamNode.DrawLine(a, b, new Color(SunEdge, 0.22f * strength), 10f);
		beamNode.DrawLine(a, b, new Color(SunMid, 0.95f * strength), 4f);
		beamNode.DrawLine(a, b, new Color(SunHot, 0.9f * strength), 2f);
	}

	// On the ground round a mirror he is near: its reach, a faint dashed ring, brighter while he is inside it - where his
	// feet turn it. Under every body.
	public override void _Draw()
	{
		if (layout == null || IsResolved || !IsFound || dark || openFor >= 0f || Player == null)
			return;
		for (int i = 0; i < layout.MirrorCount; i++)
		{
			Vector2 m = layout.Mirrors[i];
			if (!PlayerWithin(m, SunMirrors.Reach + 160f))
				continue;
			DrawProgressArc(m, SunMirrors.Reach, 0f, SunEdge, 2f, nearMirror == i ? 0.6f : 0.3f);
		}
	}

	// The sunlight's shaft onto the source, falling from the upper left where the key light is; a pool of it on the sand;
	// a glow along every lit leg and on every lit disc, a flare where the light has just arrived; the disc over the door
	// when the light is on it. And his cold mark on the slab, the far tell, as before.
	private static readonly Vector2 SunFrom = new Vector2(-0.42f, -1f).Normalized();
	private readonly Vector2[] shaft = new Vector2[4];
	private readonly Color[] shaftColours = new Color[4];

	protected override void DrawLight(CanvasItem light)
	{
		Vector2 mark = tombAt + MarkOffset;
		if (IsResolved && SecondsSinceResolved > 2f)
			return;
		if (IsDormant)
			DrawFarTell(light, mark, TellColor, Flicker(3));
		else if (openFor < OpenAt && !IsResolved && NearPlayer(mark))
		{
			float strength = openFor >= 0f ? Mathf.Clamp(1f - openFor / OpenAt, 0f, 1f) * 0.4f : 0.4f;
			DrawFarTell(light, mark, TellColor, strength * Flicker(3));
			ExploreArt.DrawGlow(light, ToLocal(mark), 14f, new Color(TellColor.Lerp(Colors.White, 0.4f), 0.5f * Flicker(1)));
		}
		if (layout == null)
			return;
		float sun = LightStrength;
		if (sun <= 0f)
			return;

		Vector2 source = layout.Mirrors[0];
		if (NearPlayer(source))
		{
			Vector2 face = ToLocal(Face(source));
			Vector2 side = new(-SunFrom.Y, SunFrom.X);
			shaft[0] = face + side * 12f;
			shaft[1] = face - side * 12f;
			shaft[2] = face + SunFrom * 1100f - side * 80f;
			shaft[3] = face + SunFrom * 1100f + side * 80f;
			shaftColours[0] = shaftColours[1] = new Color(SunEdge, 0.30f * sun);
			shaftColours[2] = shaftColours[3] = new Color(SunEdge, 0f);
			light.DrawPolygon(shaft, shaftColours);
			ExploreArt.DrawGlow(light, ToLocal(source), 90f, new Color(SunEdge, 0.16f * sun));
		}
		for (int k = 0; k < chainLength; k++)
		{
			int m = chain[k];
			Vector2 a = layout.Mirrors[m];
			Vector2 b = layout.BeamEnd(m, facing[m]);
			if (!NearPlayer(a) && !NearPlayer(b))
				continue;
			SunMirrors.End end = layout.HitFrom(m, facing[m]).Kind;
			Vector2 from = ToLocal(Face(a));
			Vector2 to = ToLocal(end == SunMirrors.End.Socket ? SocketFace : Face(b));
			if (end == SunMirrors.End.Open)
				to = from + (to - from) * Mathf.Max(0f, 1f - FadeTail / Mathf.Max(1f, from.DistanceTo(to)));
			light.DrawLine(from, to, new Color(SunEdge, 0.10f * sun), 28f);
			float flare = Mathf.Clamp(1f - litFor[m] / 0.6f, 0f, 1f);
			ExploreArt.DrawGlow(light, from, 34f + 50f * flare, new Color(SunHot, (0.35f + 0.4f * flare) * sun));
			if (end == SunMirrors.End.Blocked)
				ExploreArt.DrawGlow(light, to, 26f, new Color(SunEdge, 0.3f * sun));
		}
		if ((socketLit || openFor >= 0f) && NearPlayer(tombAt))
		{
			Vector2 disc = ToLocal(SocketFace);
			ExploreArt.DrawGlow(light, disc, 80f, new Color(SunEdge, 0.28f * sun));
			ExploreArt.DrawGlow(light, disc, 26f, new Color(SunCore, 0.6f * sun));
		}
		if (openFor >= 0f && NearPlayer(tombAt))
		{
			// The door open: the sun's light falls into it, and the day comes in on the stair.
			float a = Mathf.Clamp((openFor - SlideThirdAt) / (OpenAt - SlideThirdAt), 0f, 1f);
			Vector2 door = ToLocal(tombAt + new Vector2(0f, -20f));
			ExploreArt.DrawGlow(light, door, 90f, new Color(SunEdge, 0.18f * a * sun));
		}
	}
}
