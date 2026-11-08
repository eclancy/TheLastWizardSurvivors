using Godot;
using TheOneHeMissed.scripts;

// One of the gaol's lantern-bearers (.ai/unlock-puzzles.md 3.8; GaolersKeyEvent, GaolRounds): a lesser jailer walking its
// round of the Cursed Dungeon's corridors, a lantern held out in front, throwing a cone of cold light ahead (LanternCone).
//
// UNTARGETABLE, ALWAYS (Eric, 2026-11-03: "the lantern-bearers are untargetable"). It is not an Enemy at all: no group, no
// health, no collision shape, nothing on any physics layer - so no spell can aim at it, no area finds it, and no touch of
// it is contact damage. An auto-shooter kills whatever its spells reach, and a killable bearer would be a puzzle the
// loadout solves. It is a body in the world the way the walking hut is: drawn, sorted with the bodies at its feet, and
// answerable only to the event that walks it.
//
// TURNED (the build's decision, 3.8: "the bearer who turns is the one thing that comes for him"). The bearer whose light
// catches him with the key stops its round and turns on him: its lantern swung onto him, it comes after him down the
// corridors (the swarm's own flow field) at seven tenths of his walk, so walking away always loses it. If it reaches him it
// strikes him with the lantern, once a second. It follows him only while its light is on him: the moment he is out of it -
// round a corner, into a side passage, out of its cone - it walks to where it last saw him and stands there swinging its
// lantern, looking; catch him in it again and it comes on. Out of its light LoseSeconds and it gives up (or after
// TurnedMaxSeconds whatever he does) and walks back to the nearest cell of its round by the corridors, its light thrown
// ahead of it as on its round (it is on duty again: its cone still catches a key carried into it), and takes its round up
// from there. So the way to lose it is the puzzle's own move - step out of the light into the dark, and wait. Its lantern
// does not see through walls (the first build turned it on him wherever he was, and a corner never lost it: only
// outwalking its 300 px reach did, at 44 px a second, and the probe ran its ten seconds out doing it). Nothing is ever
// lost but the key's place and the time.
//
// Its light is drawn on DrawLayers.Air (ZAsRelative false), additively: over every body, under nothing in the world but
// the icons and numbers. In the dark chapter it is the brightest thing on screen, as 3.8 asks, and it lifts the room's dark
// along its axis with two of LightPool's cold lights so the floor under it is seen, not only washed.
public partial class LanternBearer : Node2D
{
	public enum Duty { Round, Turned, Returning, Gone }

	/// <summary>
	/// A turned bearer's walk, as a share of his: under one, so walking away always loses it. 0.8 was built first, and in a
	/// long straight corridor with no side passage (seed 31) outwalking its lantern's reach at 44 px a second took most of
	/// the ten seconds; at 0.7 he gains 66 a second.
	/// </summary>
	public const float TurnedSpeedShare = 0.7f;

	/// <summary>Seconds out of its light before a turned bearer gives up and goes back to its round.</summary>
	public const float LoseSeconds = 2f;

	/// <summary>The longest a bearer stays turned, seconds, whatever he does.</summary>
	public const float TurnedMaxSeconds = 10f;

	/// <summary>How close a turned bearer must come, feet to feet, to strike him with the lantern.</summary>
	public const float BlowReach = 60f;

	/// <summary>The lantern's blow, before the chapter's damage scale: about a tenth of his health in the Dungeon.</summary>
	public const int BlowDamage = 4;

	/// <summary>Seconds between blows while it is on him.</summary>
	public const float BlowCooldown = 1.0f;

	/// <summary>A turned bearer's first blow waits this long: the turn is a beat he can see before anything lands.</summary>
	public const float FirstBlowDelay = 0.7f;

	/// <summary>
	/// Walking back to its round it hurries, at this share over its round's pace: it may have followed him a long way, and
	/// the gaol's rhythm - what the player reads - is its rounds, not the walk back.
	/// </summary>
	public const float ReturnSpeedShare = 1.5f;

	/// <summary>
	/// The body: the Warden's own sheet (the chapter's jailer, StageBestiary.CursedDungeon), drawn smaller than the Warden's 2.7
	/// and colder - a lesser turnkey of the same gaol, never mistaken for the one that carries the key, which is a size above
	/// it and the only one of them in the fight.
	/// </summary>
	public const string BodyFramesPath = "res://scenes/resources/WardenEnemyFrames.tres";

	public const float BodyScale = 2f;

	/// <summary>The lantern it holds (tools/art/gaol_lantern.py): a cold flame in an iron cage, four flicker frames.</summary>
	public const string LanternPath = "res://assets/bonelight/world/dungeon/gaol-lantern.png";

	public const int LanternFrames = 4;

	// The Warden's feet are row 46 of its 48 px cell, 22 px below the middle: at BodyScale, 44 px.
	private const float FeetBelowCentre = 22f * BodyScale;
	// The lantern at the hand: held out in front, at the hip.
	private const float LanternHeight = 40f;
	private const float LanternForward = 18f;

	// Beyond this from the player a bearer's light cannot reach the screen on any canvas: it is not aimed or drawn.
	private const float LiveDistance = 1900f;

	/// <summary>The cold of his light (the far tell's arcane cyan, his mark), and its white-hot heart when it flares.</summary>
	public static readonly Color Cold = new(0.42f, 0.72f, 0.91f);
	private static readonly Color ColdCore = new(0.80f, 0.93f, 1.00f);
	private static readonly Color BodyTint = new(0.66f, 0.74f, 0.86f);

	public GaolRound Round { get; private set; }
	public GaolGround Ground { get; private set; }
	public int Index { get; private set; }
	public Duty State { get; private set; } = Duty.Round;

	/// <summary>Where it is on its round, as arc length along the round's cells (only meaningful on its round).</summary>
	public float Arc { get; private set; }

	/// <summary>Where it stands on the floor: the cone's point.</summary>
	public Vector2 Feet { get; private set; }

	/// <summary>The way its lantern points.</summary>
	public Vector2 Facing { get; private set; } = Vector2.Right;

	/// <summary>Its light, aimed every frame it is near enough to matter (<see cref="ConeLive"/>).</summary>
	public LanternCone Cone { get; } = new();

	/// <summary>True while the cone was aimed this frame: near the player and not gone.</summary>
	public bool ConeLive { get; private set; }

	public float TurnedClock { get; private set; }
	public float LostClock { get; private set; }
	public int Turns { get; private set; }
	public int Blows { get; private set; }
	public int Returns { get; private set; }

	/// <summary>Why it last gave up on him: true when he got out of its light, false when its time ran out. For the probe.</summary>
	public bool LastLostHim { get; private set; }

	/// <summary>The bounds the walk back to its round is found in (the event's search box).</summary>
	public Vector2I BoxMin { get; set; }
	public Vector2I BoxMax { get; set; }

	/// <summary>Where a turned bearer last had him in its light, and whether its light was on him on its last frame.</summary>
	public Vector2 LastSeen { get; private set; }
	public bool SeesHim { get; private set; }

	/// <summary>Where it stood when it turned, and how far from there it has gone after him: for the probe.</summary>
	public Vector2 TurnedFrom { get; private set; }
	public float FollowedFurthest { get; private set; }

	private float blowClock;
	private float fade = 1f;
	private Vector2 searchFacing = Vector2.Right;
	private float flare;
	private float clock;
	private Vector2[] way;
	private int wayLeg;
	private int rejoinIndex;
	private AnimatedSprite2D body;
	private Sprite2D lantern;
	private Node2D light;
	// The fan drawn each frame, in the light node's space: reused, so drawing allocates nothing.
	private readonly Vector2[] tri = new Vector2[3];
	private readonly Color[] triColors = new Color[3];
	private readonly Vector2[] rim = new Vector2[LanternCone.Rays];

	public static LanternBearer Create(GaolRound round, GaolGround ground, int index)
	{
		var bearer = new LanternBearer
		{
			Name = $"LanternBearer{index + 1}",
			Round = round,
			Ground = ground,
			Index = index,
			Arc = round.StartArc,
		};
		bearer.Feet = round.PointAt(bearer.Arc);
		bearer.Facing = round.FacingAt(bearer.Arc);
		bearer.Position = bearer.Feet - new Vector2(0f, WorldSprites.SortLift);
		return bearer;
	}

	public override void _Ready()
	{
		// Standing in the world, sorted with the bodies at its feet: ZIndex 5 under the event's -5 is the actors' 0 (as
		// the bog lantern and the ice block are), and the node sits SortLift above the feet (WorldSprites).
		ZIndex = 5;
		SpriteFrames frames = ResourceLoader.Exists(BodyFramesPath) ? GD.Load<SpriteFrames>(BodyFramesPath) : null;
		if (frames == null)
			GD.PushError($"LanternBearer: {BodyFramesPath} is missing; the bearer walks without a body.");
		body = new AnimatedSprite2D
		{
			Name = "Body",
			SpriteFrames = frames,
			Scale = new Vector2(BodyScale, BodyScale),
			Position = new Vector2(0f, WorldSprites.SortLift - FeetBelowCentre),
			SelfModulate = BodyTint,
			TextureFilter = TextureFilterEnum.Nearest,
			// The Warden strides at 118 px a second; a bearer on its round at 80.
			SpeedScale = GaolRound.Speed / 118f,
		};
		AddChild(body);
		if (frames != null && frames.HasAnimation("moving"))
			body.Play("moving");

		light = new Node2D
		{
			Name = "LanternLight",
			ZIndex = DrawLayers.Air,
			ZAsRelative = false,
			Material = ExploreArt.Additive,
			TextureFilter = TextureFilterEnum.Linear,
		};
		light.Draw += DrawLight;
		AddChild(light);

		Texture2D sheet = ResourceLoader.Exists(LanternPath) ? GD.Load<Texture2D>(LanternPath) : null;
		if (sheet == null)
			GD.PushError("LanternBearer: gaol-lantern.png is missing or not imported (python tools/art/gaol_lantern.py, then import).");
		lantern = new Sprite2D
		{
			Name = "Lantern",
			Texture = sheet,
			Hframes = LanternFrames,
			ZIndex = DrawLayers.Air,
			ZAsRelative = false,
			TextureFilter = TextureFilterEnum.Nearest,
		};
		AddChild(lantern);
		Place();
	}

	public override void _ExitTree()
	{
		if (light != null && IsInstanceValid(light))
			light.Draw -= DrawLight;
	}

	/// <summary>
	/// Caught him with the key at <paramref name="feet"/>: stops its round and turns its light onto him, and comes for him.
	/// </summary>
	public void Turn(Vector2 feet)
	{
		if (State == Duty.Gone)
			return;
		if (State != Duty.Turned)
		{
			Turns++;
			TurnedClock = 0f;
			blowClock = FirstBlowDelay;
			TurnedFrom = Feet;
			FollowedFurthest = 0f;
		}
		State = Duty.Turned;
		LostClock = 0f;
		LastSeen = feet;
		SeesHim = true;
		flare = 1f;
	}

	/// <summary>The event is over or stood down: its light gutters out and it fades from the corridor.</summary>
	public void StandDown()
	{
		State = Duty.Gone;
	}

	/// <summary>True once it has gone out entirely.</summary>
	public bool Faded => State == Duty.Gone && fade <= 0f;

	/// <summary>
	/// One frame: walks its round, comes for him, or walks back; then aims its light if he is near enough to see it. Called by
	/// the event every frame, dormant or running, after the event's own Tick read last frame's light - the light he saw.
	/// </summary>
	public void Advance(float dt, Player player, float damageScale)
	{
		clock += dt;
		flare = Mathf.MoveToward(flare, 0f, dt / 0.7f);
		switch (State)
		{
			case Duty.Round:
				Arc = Mathf.PosMod(Arc + Round.Direction * GaolRound.Speed * dt, Round.Length);
				Feet = Round.PointAt(Arc);
				Facing = Round.FacingAt(Arc);
				break;
			case Duty.Turned:
				ComeFor(dt, player, damageScale);
				break;
			case Duty.Returning:
				WalkBack(dt);
				break;
			case Duty.Gone:
				fade = Mathf.MoveToward(fade, 0f, dt / 1.2f);
				break;
		}
		Place();

		ConeLive = fade > 0f && player != null && IsInstanceValid(player) && Feet.DistanceSquaredTo(player.GlobalPosition) < LiveDistance * LiveDistance;
		if (ConeLive)
		{
			Cone.Aim(Feet, Facing, Ground.WallTest);
			// The floor under the light: the room's dark lifted at the lantern and halfway down the cone, cold.
			float strength = fade * (State == Duty.Turned ? 1f : 0.8f);
			LightPool.ColdLight(LanternAt(), 80f, 0.7f * strength);
			float down = Cone.AxisReach * 0.5f;
			if (down > 40f)
				LightPool.ColdLight(Feet + Facing * down, down * 0.9f, 0.55f * strength);
		}
		if (State == Duty.Turned && player != null && IsInstanceValid(player))
		{
			// Seen is its light on him: the cone, the walls stopping it. Not seen, it does not know where he is.
			SeesHim = ConeLive && Cone.Covers(player.FootPosition);
			if (SeesHim)
				LastSeen = player.FootPosition;
			LostClock = SeesHim ? 0f : LostClock + dt;
			FollowedFurthest = Mathf.Max(FollowedFurthest, Feet.DistanceTo(TurnedFrom));
			if (LostClock >= LoseSeconds || TurnedClock >= TurnedMaxSeconds)
			{
				LastLostHim = LostClock >= LoseSeconds;
				BeginWalkBack();
			}
		}
		light?.QueueRedraw();
	}

	private void ComeFor(float dt, Player player, float damageScale)
	{
		TurnedClock += dt;
		blowClock -= dt;
		if (player == null || !IsInstanceValid(player))
			return;
		Vector2 him = player.FootPosition;
		float pace = player.WalkingSpeed * TurnedSpeedShare * dt;
		if (SeesHim)
		{
			// In its light: lantern on him, down the corridors the swarm takes to him (the flow field is built toward him),
			// straight at him in his own cell.
			Vector2 toHim = him - Feet;
			if (toHim.LengthSquared() > 1f)
				Facing = toHim.Normalized();
			if (toHim.Length() > BlowReach * 0.75f)
			{
				Vector2 dir = MazeNavigation.Routing?.FlowDirectionAt(Feet) ?? Vector2.Zero;
				if (dir == Vector2.Zero)
					dir = Facing;
				Feet = OnTheFloor(Feet, dir * pace);
			}
		}
		else
		{
			// Out of its light: to where it last saw him (in its light then, so in a straight line), its lantern ahead; there,
			// it stands and swings the lantern either way, looking.
			Vector2 toLast = LastSeen - Feet;
			float d = toLast.Length();
			if (d > 4f)
			{
				searchFacing = toLast / d;
				Facing = searchFacing;
				Feet = OnTheFloor(Feet, toLast / d * Mathf.Min(d, pace));
			}
			else
				Facing = searchFacing.Rotated(0.75f * Mathf.Sin(clock * 2.4f));
		}
		if (SeesHim && Feet.DistanceTo(him) <= BlowReach && blowClock <= 0f && !player.IsDead)
		{
			// Not an Enemy, so no Weaken can be on it (nothing can hit it to put one there): the chapter's scale is the only
			// multiplier its blow takes. Player.TakeDamage runs the dodge and every mitigation as for any hit.
			player.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(BlowDamage * damageScale)));
			blowClock = BlowCooldown;
			Blows++;
			flare = 1f;
		}
	}

	// The step, slid along a wall rather than into it: the bearer has no body to collide, so the floor is asked directly.
	private Vector2 OnTheFloor(Vector2 from, Vector2 step)
	{
		Vector2 to = from + step;
		if (!Ground.Wall(to))
			return to;
		Vector2 alongX = from + new Vector2(step.X, 0f);
		if (!Ground.Wall(alongX))
			return alongX;
		Vector2 alongY = from + new Vector2(0f, step.Y);
		return Ground.Wall(alongY) ? from : alongY;
	}

	private void BeginWalkBack()
	{
		Vector2I here = Ground.CellOf(Feet);
		var reach = Ground.Distances(here, BoxMin, BoxMax);
		int best = -1;
		int bestSteps = int.MaxValue;
		for (int i = 0; i < Round.Cells.Length; i++)
		{
			if (reach.TryGetValue(Round.Cells[i], out int steps) && steps < bestSteps)
			{
				bestSteps = steps;
				best = i;
			}
		}
		Vector2I[] cells = best >= 0 ? Ground.PathBetween(here, Round.Cells[best], BoxMin, BoxMax) : null;
		if (cells == null)
		{
			// Somewhere the box does not reach (it followed him far out): it goes back to where its round began.
			best = 0;
			cells = new[] { Round.Cells[0] };
			Feet = Ground.CentreOf(Round.Cells[0]);
		}
		way = new Vector2[cells.Length];
		for (int i = 0; i < cells.Length; i++)
			way[i] = Ground.CentreOf(cells[i]);
		wayLeg = 0;
		rejoinIndex = best;
		State = Duty.Returning;
	}

	private void WalkBack(float dt)
	{
		float step = GaolRound.Speed * ReturnSpeedShare * dt;
		while (step > 0f && wayLeg < way.Length)
		{
			Vector2 target = way[wayLeg];
			Vector2 to = target - Feet;
			float d = to.Length();
			if (d > 0.5f)
				Facing = to / d;
			if (d <= step)
			{
				Feet = target;
				step -= d;
				wayLeg++;
			}
			else
			{
				Feet += to / d * step;
				step = 0f;
			}
		}
		if (wayLeg >= way.Length)
		{
			State = Duty.Round;
			Arc = Round.ArcOfCell(rejoinIndex);
			Feet = Round.PointAt(Arc);
			Facing = Round.FacingAt(Arc);
			Returns++;
		}
	}

	private Vector2 LanternAt() => Feet + new Vector2(Facing.X * LanternForward, Facing.Y * LanternForward * 0.5f - LanternHeight);

	private void Place()
	{
		Position = Feet - new Vector2(0f, WorldSprites.SortLift);
		Modulate = new Color(1f, 1f, 1f, fade);
		if (body != null)
		{
			body.FlipH = Facing.X < -0.05f;
			// Turned on him it stops to strike: the stride slows as it closes in, as anything walking up to hit does.
			body.SpeedScale = (State == Duty.Turned ? 1.3f : 1f) * GaolRound.Speed / 118f;
		}
		if (lantern != null)
		{
			lantern.Position = LanternAt() - Position;
			// The flame's own flicker, and its brightest frame while it flares.
			lantern.Frame = flare > 0.3f ? 0 : 1 + (int)(clock * 7f + Index * 1.7f) % (LanternFrames - 1);
		}
	}

	// THE LIGHT, drawn from the same reach table the catch reads (LanternCone): a fan of triangles from the lantern's foot
	// to each ray's end, bright at the point and thinning toward the far end; a brighter rim where the light lands on the
	// floor or meets a wall; the cone's two edges faint; and a glow round the lantern itself. Turned, it flares white-hot and
	// stays brighter, so the one that has seen him is told apart from the rest at a glance.
	private void DrawLight()
	{
		if (!ConeLive || fade <= 0f)
			return;
		Vector2 o = Feet - GlobalPosition;
		bool turned = State == Duty.Turned;
		float pulse = turned ? 0.85f + 0.15f * Mathf.Sin(clock * 9f) : 0.92f + 0.08f * Mathf.Sin(clock * 3.1f + Index * 2f);
		float strength = fade * pulse;
		Color hue = Cold.Lerp(ColdCore, Mathf.Clamp((turned ? 0.35f : 0f) + flare * 0.6f, 0f, 1f));
		float coreAlpha = (turned ? 0.42f : 0.32f) * strength;
		float farAlpha = (turned ? 0.12f : 0.08f) * strength;
		float reach = Cone.Reach;
		for (int k = 0; k < LanternCone.Rays - 1; k++)
		{
			tri[0] = o;
			tri[1] = Cone.EndOf(k) - GlobalPosition;
			tri[2] = Cone.EndOf(k + 1) - GlobalPosition;
			if ((tri[1] - o).LengthSquared() < 4f && (tri[2] - o).LengthSquared() < 4f)
				continue;
			triColors[0] = new Color(hue, coreAlpha);
			triColors[1] = new Color(hue, Mathf.Lerp(coreAlpha, farAlpha, Cone.ReachOf(k) / reach));
			triColors[2] = new Color(hue, Mathf.Lerp(coreAlpha, farAlpha, Cone.ReachOf(k + 1) / reach));
			light.DrawPolygon(tri, triColors);
		}
		for (int k = 0; k < LanternCone.Rays; k++)
			rim[k] = Cone.EndOf(k) - GlobalPosition;
		light.DrawPolyline(rim, new Color(ColdCore, (turned ? 0.55f : 0.38f) * strength), 2f);
		light.DrawLine(o, rim[0], new Color(hue, 0.22f * strength), 1.5f);
		light.DrawLine(o, rim[LanternCone.Rays - 1], new Color(hue, 0.22f * strength), 1.5f);
		Vector2 lamp = LanternAt() - GlobalPosition;
		ExploreArt.DrawGlow(light, lamp, 30f + 16f * flare, new Color(hue, (0.38f + 0.4f * flare) * strength));
		ExploreArt.DrawGlow(light, o, 46f, new Color(hue, 0.16f * strength));
	}
}
