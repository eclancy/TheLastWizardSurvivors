using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// THE SILVER HIND - the White Downs' side event (stage_14, .ai/second-half.md 4.3, piece F3). Its ask: STALK.
//
// Artemis's hind grazes out on the down, moon-pale, waiting for a moon that has not risen since he came. His hunt
// is out after her - the beaters, the coursing hounds, the huntsman's horn - and has not caught her. The player is
// asked to get to her side without her seeing him move. She grazes, head down; her head comes up (the TELL); she
// looks (the WATCH), and her gaze is drawn on the turf as a pale fan - stopped by the barrows, which she cannot see
// through. Anything that MOVES in it, she sees: she bolts, bounding away to another of her grazing places on the
// down, and the hunt's hounds are slipped at the one who startled her. A man who stands still in her gaze is not
// seen, and nor is one who moves behind a barrow. Reach her while she does not see you, and she is won.
//
// HELD TO .ai/unlock-puzzles.md's BAR (section 1):
//   1. The state is drawn in the world: her head down or up, and her gaze as a fan on the ground, cut short at
//      the barrows - what is drawn is exactly what sees (the rays below are both the fan and the test).
//   2. One action, learned by doing it: move, or be still. No button.
//   3. A wrong move costs time, never progress: she bolts to another of her places (never lost, never far - a
//      ring of seeded places round the first), the hounds come, and the stalk starts again from where you stand.
//   4. Generated: her places are laid from the run's seed (ExplorePlacer.Near), her rhythm rolled each time.
//   5. Solved under the horde: standing still while the swarm closes is the cost of every look she gives.
// And the event rules: nothing she is joins "enemies" (she is a plain Node2D and a sprite - no body, no group, no
// collision, so no spell aims at her and nothing she does is contact damage); her rhythm is real seconds, never
// the pace clock; nothing locks. The hounds are the chapter's own Fast (StageBestiary.WhiteDowns), not elites:
// they are pressure, and a hound recycled to the ring loses nothing.
//
// IT TEACHES THE CHAPTER'S COVER. The Unstrung Hunter's lane stops at a barrow too (UnstrungHunterBoss): a player
// who has stalked her behind the mounds already knows what they are for. Different verbs - she sees what moves, he
// shoots what is in his line - on the same ground.
//
// WHAT IT PAYS (.ai/second-half.md 4.3): no unlock. The first time, its deed - The Hind at Gaze, the keepsake - and
// its Arcane Energy (AchievementDefinitions.SideEventCurrency), and a chest; every time after, a chest.
public partial class SilverHindEvent : SideEvent
{
	// --- Her gaze ---------------------------------------------------------------------------------
	[Export] public float GazeRange { get; set; } = 600f;
	[Export] public float GazeHalfAngleDegrees { get; set; } = 36f;
	/// <summary>How far off the wizard's bearing a look may fall either way: she looks toward him, roughly.</summary>
	[Export] public float GazeJitterDegrees { get; set; } = 24f;

	// --- Her rhythm (real seconds) ------------------------------------------------------------------
	[Export] public float GrazeMinSeconds { get; set; } = 2.2f;
	[Export] public float GrazeMaxSeconds { get; set; } = 3.6f;
	/// <summary>Her head coming up before she looks: the warning, and the time to stop.</summary>
	[Export] public float TellSeconds { get; set; } = 0.6f;
	[Export] public float WatchMinSeconds { get; set; } = 1.4f;
	[Export] public float WatchMaxSeconds { get; set; } = 2.3f;
	/// <summary>After a bolt she stands and watches where she landed before she grazes again.</summary>
	[Export] public float SettleSeconds { get; set; } = 1.4f;

	// --- What she sees ----------------------------------------------------------------------------
	/// <summary>Moving, to her: the feet carried this far over the last <see cref="MoveWindowSeconds"/>.</summary>
	[Export] public float MoveThreshold { get; set; } = 10f;
	[Export] public float MoveWindowSeconds { get; set; } = 0.15f;
	/// <summary>How close is "at her side".</summary>
	[Export] public float ReachRadius { get; set; } = 72f;

	// --- Her places, and the bolt -------------------------------------------------------------------
	[Export] public int PlaceCount { get; set; } = 6;
	[Export] public float PlaceMinStep { get; set; } = 420f;
	[Export] public float PlaceMaxStep { get; set; } = 640f;
	[Export] public float PlaceReach { get; set; } = 1150f;
	[Export] public float BoltSpeed { get; set; } = 460f;
	[Export] public int HoundsOnBolt { get; set; } = 2;
	[Export] public float PressureIntervalSeconds { get; set; } = 8f;

	// The fan: rays across the gaze, each marched out to the first barrow. The fan drawn and the test that sees
	// are the same rays, so nothing is seen outside what is drawn.
	private const int Rays = 21;
	private const float RayStep = 12f;

	private const string SheetPath = "res://assets/bonelight/world/downs/silver-hind.png";
	private const int Cell = 48;
	private const int FeetRow = 46;
	// Frames (tools/art/silver_hind.py, silver-hind.json).
	private const int FrameGrazeA = 0, FrameGrazeB = 1, FrameAlert = 2, FrameAlertTurn = 3, FrameBound = 4, FrameStand = 7;
	// Her eyes, roughly, above her feet: where the gaze is drawn from (world px, facing right).
	private static readonly Vector2 EyeLift = new(24f, -60f);

	private enum Mood { Grazing, Tell, Watching, Bolting, Settling, Won }

	private readonly List<Vector2> places = new();
	private readonly float[] reach = new float[Rays];
	private readonly RandomNumberGenerator rng = new();
	private readonly Queue<(float Time, Vector2 Feet)> trail = new();

	private Mood mood = Mood.Grazing;
	private float moodLeft;
	private Vector2 hind;
	private Vector2 boltFrom, boltTo;
	private float boltT, boltLength;
	private int placeIndex;
	private float gazeAngle;
	private bool facingRight = true;
	private float sawYouLeft;
	private float pressureLeft;
	private float wonFor = -1f;
	private Vector2 winSpot;
	private bool firstWin;

	private Node2D root;
	private Sprite2D sprite;

	/// <summary>Bolts so far. For _ExploreProbe.</summary>
	public int Bolts { get; private set; }
	/// <summary>Where she is now, and whether she is looking. For _ExploreProbe and _UiShot.</summary>
	public Vector2 HindPosition => hind;
	public bool IsWatching => mood is Mood.Watching or Mood.Settling;
	public bool IsTelling => mood == Mood.Tell;
	public bool IsBolting => mood == Mood.Bolting;
	public IReadOnlyList<Vector2> Places => places;

	protected override string Announcement => "A silver hind grazes out on the down. The hunt has not found her.";
	protected override string Objective => mood == Mood.Won ? "The Silver Hind"
		: sawYouLeft > 0f ? "She saw you move"
		: IsWatching ? "She is looking"
		: "Stalk the Silver Hind";
	protected override Vector2? PointerTarget => mood == Mood.Won ? null : hind;
	protected override string SiteGlyph => "hind";
	protected override float SiteFootprint => 200f;
	// She is seen from further than her gaze reaches, so the stalk begins with her on screen and not looking.
	protected override float ArrivalRadius => 860f;
	// Moon-pale: the light she is waiting for.
	protected override Color TellColor => new(0.78f, 0.84f, 0.98f);
	protected override string ChestLine => firstWin
		? $"The Hind at Gaze - {AchievementDefinitions.SilverHindCurrency} Arcane Energy, and a chest where she stood"
		: "She is gone up the down - a chest where she stood";

	public override void PlaceSites(ExplorePlacer placer)
	{
		AddSite(placer.EventSite(SiteFootprint));
		places.Add(MainSite);
		// Her other places: a seeded chain of steps round the first, each on open turf, none on another's footprint.
		for (int i = 1; i < Math.Max(2, PlaceCount); i++)
			places.Add(placer.Near(places[i - 1], PlaceMinStep, PlaceMaxStep, 90f, places, 360f, MainSite, PlaceReach));
	}

	protected override void OnPlaced()
	{
		rng.Randomize();
		hind = MainSite;
		TextureFilter = TextureFilterEnum.Nearest;
		Texture2D sheet = ResourceLoader.Exists(SheetPath) ? GD.Load<Texture2D>(SheetPath) : null;
		if (sheet == null)
		{
			GD.PushError("SilverHindEvent: the hind's sheet is missing or not imported (python tools/art/silver_hind.py, then import).");
			return;
		}
		// She stands among the bodies, sorted at her feet: a root in the run's world, its own sibling, y-sorted.
		root = new Node2D { Name = "SilverHind", YSortEnabled = true };
		sprite = new Sprite2D
		{
			Name = "Hind",
			Texture = sheet,
			Hframes = Math.Max(1, sheet.GetWidth() / Cell),
			Centered = false,
			Scale = new Vector2(2f, 2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Offset = new Vector2(-Cell / 2f, WorldSprites.SortLift / 2f - FeetRow),
		};
		root.AddChild(sprite);
		Game?.CallDeferred(Node.MethodName.AddChild, root);
		PlaceSprite(FrameGrazeA);
		moodLeft = rng.RandfRange(GrazeMinSeconds, GrazeMaxSeconds);
	}

	public override void _ExitTree()
	{
		if (root != null && IsInstanceValid(root))
			root.QueueFree();
		base._ExitTree();
	}

	// Dormant, she grazes where she is, head down and up - nobody is there to see.
	protected override void DormantTick(float delta)
	{
		int frame = Mathf.PosMod((int)(Clock / 0.9f), 2) == 0 ? FrameGrazeA : FrameGrazeB;
		PlaceSprite(frame);
	}

	protected override void Begin()
	{
		mood = Mood.Grazing;
		moodLeft = rng.RandfRange(GrazeMinSeconds, GrazeMaxSeconds);
		pressureLeft = PressureIntervalSeconds * 0.5f;
		trail.Clear();
	}

	protected override void Tick(float delta)
	{
		Player player = Player;
		if (player == null)
			return;
		Vector2 feet = player.FootPosition;
		RecordTrail(feet);
		sawYouLeft = Mathf.Max(0f, sawYouLeft - delta);

		if (mood == Mood.Won)
		{
			TickWin(delta);
			return;
		}

		// The horde does not stop for her: a ring of the down's own now and then round where she grazes.
		pressureLeft -= delta;
		if (pressureLeft <= 0f)
		{
			pressureLeft = PressureIntervalSeconds;
			SpawnRing(hind, 4, 380f);
		}

		if (mood == Mood.Bolting)
		{
			TickBolt(delta);
			return;
		}

		// What she sees, first: a man moving in her gaze. Then whether he is at her side.
		if (IsWatching)
		{
			CastGaze();
			if (Sees(feet) && IsMoving())
			{
				Startle(feet);
				return;
			}
		}
		if (feet.DistanceTo(hind) <= ReachRadius)
		{
			Win();
			return;
		}

		moodLeft -= delta;
		switch (mood)
		{
			case Mood.Grazing:
				PlaceSprite(Mathf.PosMod((int)(Clock / 0.9f), 2) == 0 ? FrameGrazeA : FrameGrazeB);
				if (moodLeft <= 0f)
				{
					mood = Mood.Tell;
					moodLeft = TellSeconds;
					// She looks toward the one stalking her, roughly - but not always straight at him.
					facingRight = feet.X >= hind.X;
					gazeAngle = (feet - hind).Angle() + Mathf.DegToRad(rng.RandfRange(-GazeJitterDegrees, GazeJitterDegrees));
				}
				break;
			case Mood.Tell:
				PlaceSprite(FrameAlertTurn);
				if (moodLeft <= 0f)
				{
					mood = Mood.Watching;
					moodLeft = rng.RandfRange(WatchMinSeconds, WatchMaxSeconds);
					CastGaze();
				}
				break;
			case Mood.Watching:
			case Mood.Settling:
				PlaceSprite(FrameAlert);
				if (moodLeft <= 0f)
				{
					mood = Mood.Grazing;
					moodLeft = rng.RandfRange(GrazeMinSeconds, GrazeMaxSeconds);
				}
				break;
		}
	}

	// --- Seeing ------------------------------------------------------------------------------------

	/// <summary>Where her gaze starts: her eyes, over her feet.</summary>
	private Vector2 Eye => hind + new Vector2(facingRight ? EyeLift.X : -EyeLift.X, 0f);

	// Every ray of the fan marched out to the first barrow (a wall: MazeNavigation.IsWallAt, the shot test) or to
	// her range. Cast once a frame while she looks; the fan drawn and Sees both read it.
	private void CastGaze()
	{
		MazeNavigation nav = MazeNavigation.Active;
		float half = Mathf.DegToRad(GazeHalfAngleDegrees);
		Vector2 from = Eye;
		for (int i = 0; i < Rays; i++)
		{
			float a = gazeAngle - half + 2f * half * i / (Rays - 1);
			Vector2 dir = Vector2.FromAngle(a);
			float r = GazeRange;
			if (nav != null)
			{
				for (float d = RayStep; d <= GazeRange; d += RayStep)
				{
					if (nav.IsWallAt(from + dir * d))
					{
						r = Mathf.Max(RayStep, d - RayStep * 0.5f);
						break;
					}
				}
			}
			reach[i] = r;
		}
	}

	/// <summary>
	/// Whether <paramref name="point"/> is inside her gaze as it is drawn: inside the fan's angle, and nearer than the
	/// shorter of the two rays either side of it (LanternCone's rule: where a barrow's corner falls between two rays,
	/// the sliver is the player's).
	/// </summary>
	public bool Sees(Vector2 point)
	{
		if (!IsWatching)
			return false;
		Vector2 offset = point - Eye;
		float d = offset.Length();
		if (d > GazeRange)
			return false;
		float half = Mathf.DegToRad(GazeHalfAngleDegrees);
		float off = Mathf.AngleDifference(gazeAngle, offset.Angle());
		if (Mathf.Abs(off) > half)
			return false;
		float u = (off + half) / (2f * half) * (Rays - 1);
		int lo = Mathf.Clamp((int)Mathf.Floor(u), 0, Rays - 1);
		int hi = Mathf.Clamp(lo + 1, 0, Rays - 1);
		return d <= Mathf.Min(reach[lo], reach[hi]);
	}

	private void RecordTrail(Vector2 feet)
	{
		trail.Enqueue((Clock, feet));
		while (trail.Count > 2 && Clock - trail.Peek().Time > MoveWindowSeconds)
			trail.Dequeue();
	}

	/// <summary>Whether the wizard's feet have moved more than <see cref="MoveThreshold"/> over the last window.</summary>
	public bool IsMoving()
	{
		if (trail.Count < 2 || Player == null)
			return false;
		return trail.Peek().Feet.DistanceTo(Player.FootPosition) > MoveThreshold;
	}

	// --- The bolt ------------------------------------------------------------------------------------

	// She saw him move: she bounds away to the place of hers furthest from him, and the hunt is slipped at him.
	private void Startle(Vector2 feet)
	{
		Bolts++;
		sawYouLeft = 2.5f;
		SfxPlayer.AtPosition(SfxCatalog.HindBolt, hind, 0.04f);
		int best = placeIndex;
		float bestDistance = -1f;
		for (int i = 0; i < places.Count; i++)
		{
			if (i == placeIndex)
				continue;
			float d = places[i].DistanceTo(feet);
			if (d > bestDistance)
			{
				bestDistance = d;
				best = i;
			}
		}
		placeIndex = best;
		boltFrom = hind;
		boltTo = places[best];
		boltLength = Mathf.Max(1f, boltFrom.DistanceTo(boltTo));
		boltT = 0f;
		facingRight = boltTo.X >= boltFrom.X;
		mood = Mood.Bolting;

		// The hunt's hounds, slipped at the one who startled her, from the side away from her.
		if (Game != null)
		{
			Vector2 away = (feet - hind).Normalized();
			for (int i = 0; i < HoundsOnBolt; i++)
			{
				float spread = (i - (HoundsOnBolt - 1) * 0.5f) * 0.6f;
				Vector2 at = Game.ReachablePoint(feet + away.Rotated(spread) * 420f);
				Game.SpawnEventEnemy(BestiaryRole.Fast, at, 1f, false);
			}
		}
	}

	private void TickBolt(float delta)
	{
		boltT += BoltSpeed * delta;
		float u = Mathf.Clamp(boltT / boltLength, 0f, 1f);
		hind = boltFrom.Lerp(boltTo, u);
		PlaceSprite(FrameBound + Mathf.PosMod((int)(Clock / 0.09f), 3), lift: 18f * Mathf.Abs(Mathf.Sin(boltT / 70f * Mathf.Pi)));
		if (u < 1f)
			return;
		hind = boltTo;
		mood = Mood.Settling;
		moodLeft = SettleSeconds;
		// She lands looking back the way she came, at him.
		if (Player != null)
		{
			facingRight = Player.FootPosition.X >= hind.X;
			gazeAngle = (Player.FootPosition - hind).Angle() + Mathf.DegToRad(rng.RandfRange(-GazeJitterDegrees, GazeJitterDegrees) * 0.5f);
		}
		CastGaze();
	}

	// --- The win -------------------------------------------------------------------------------------

	// At her side, unseen: she lifts her head and does not run. She stands a moment, looking at him, then goes up the
	// down toward where the moon should rise - and only then does the reward come.
	private void Win()
	{
		mood = Mood.Won;
		wonFor = 0f;
		winSpot = ChooseRewardSpot(hind);
		firstWin = !PaysChest;
		facingRight = Player == null || Player.FootPosition.X >= hind.X;
		SfxPlayer.AtPosition(SfxCatalog.SecretSeen, hind, 0f);
	}

	private const float WinStandSeconds = 1.0f;
	private const float WinGoSeconds = 1.1f;

	private void TickWin(float delta)
	{
		wonFor += delta;
		if (wonFor < WinStandSeconds)
		{
			PlaceSprite(FrameStand);
			return;
		}
		float go = (wonFor - WinStandSeconds) / WinGoSeconds;
		if (go < 1f)
		{
			// Up the down, away to the north-east, fading as she goes.
			facingRight = true;
			PlaceSprite(FrameBound + Mathf.PosMod((int)(Clock / 0.09f), 3), offset: new Vector2(260f, -200f) * go, alpha: 1f - go);
			return;
		}
		if (sprite != null)
			sprite.Visible = false;
		Succeed(winSpot, exact: true);
	}

	private void PlaceSprite(int frame, float lift = 0f, Vector2 offset = default, float alpha = 1f)
	{
		if (root == null || sprite == null)
			return;
		root.Position = hind + offset - new Vector2(0f, WorldSprites.SortLift);
		sprite.Frame = Mathf.Clamp(frame, 0, sprite.Hframes - 1);
		sprite.FlipH = !facingRight;
		sprite.Position = new Vector2(0f, -lift);
		sprite.Modulate = new Color(1f, 1f, 1f, alpha);
	}

	// --- Drawing -------------------------------------------------------------------------------------

	// Her gaze on the turf: a pale fan from her eyes, stopped at the barrows - faint as her head comes up, full while
	// she looks, whiter the moment she sees him. On the floor, under every body.
	public override void _Draw()
	{
		if (IsResolved || IsDormant)
			return;
		float strength = mood switch
		{
			Mood.Tell => Mathf.Clamp(1f - moodLeft / Mathf.Max(0.01f, TellSeconds), 0f, 1f) * 0.45f,
			Mood.Watching or Mood.Settling => 1f,
			_ => 0f,
		};
		if (strength <= 0f)
			return;
		if (mood == Mood.Tell)
			CastGaze();
		float half = Mathf.DegToRad(GazeHalfAngleDegrees);
		Vector2 origin = ToLocal(Eye);
		var moon = new Color(0.80f, 0.86f, 1.0f);
		for (int i = 0; i < Rays - 1; i++)
		{
			float a0 = gazeAngle - half + 2f * half * i / (Rays - 1);
			float a1 = gazeAngle - half + 2f * half * (i + 1) / (Rays - 1);
			Vector2 p0 = origin + Vector2.FromAngle(a0) * reach[i];
			Vector2 p1 = origin + Vector2.FromAngle(a1) * reach[i + 1];
			DrawPolygon(new[] { origin, p0, p1 }, new[]
			{
				new Color(moon, 0.20f * strength), new Color(moon, 0.05f * strength), new Color(moon, 0.05f * strength),
			});
			// The rim where her gaze lands on the turf or meets a barrow.
			DrawLine(p0, p1, new Color(moon, 0.55f * strength), 2f);
		}
		DrawLine(origin, origin + Vector2.FromAngle(gazeAngle - half) * reach[0], new Color(moon, 0.35f * strength), 2f);
		DrawLine(origin, origin + Vector2.FromAngle(gazeAngle + half) * reach[Rays - 1], new Color(moon, 0.35f * strength), 2f);
	}

	// The far tell over her place while nobody has come; then a faint cold sheen on her, the palest thing on the down.
	protected override void DrawLight(CanvasItem light)
	{
		base.DrawLight(light);
		if (mood == Mood.Won && wonFor >= WinStandSeconds)
			return;
		if (NearPlayer(hind))
			ExploreArt.DrawGlow(light, ToLocal(hind + new Vector2(0f, -36f)), 70f, new Color(0.78f, 0.84f, 0.98f, 0.10f));
	}

	protected override void OnStoodDown()
	{
		if (sprite != null)
			sprite.Visible = false;
	}
}
