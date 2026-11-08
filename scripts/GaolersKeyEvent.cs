using Godot;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// The Cursed Dungeon's side event: the Gaoler's key (.ai/side-events.md; .ai/unlock-puzzles.md 3.8, built 2026-11-08).
//
// "He keeps a wizard in one of these cells." Tituba is behind the one locked door on the map. Reaching it wakes the
// Warden - the dark wizard's jailer, the chapter's miniboss - at its post four to six cells away, and the Warden keeps
// the key. It stands its ground there: it leaves its post for him only when he comes near it, and never follows him far
// (GuardRadius, LeashRadius), so the key always falls on the far side of the gaol from the cell.
//
// SLIP PAST THE LANTERNS (3.8, decided by Eric). The way home is swept by the gaol's lantern-bearers walking their rounds of
// the corridors, each throwing a cone of cold light ahead (LanternBearer, LanternCone). Caught in a cone while carrying
// the key, the key drops where he stands and THAT bearer turns on him; go back for it. The three-hit rule is gone. The
// insight is watching the rounds, waiting in a side passage, and slipping through.
//
// The rounds are the seed's (GaolLayout): the post, and one to three loops of cells laid across or beside the way
// between, kept only if a walker with the key can slip through at several phase sets of the bearers. They are laid from
// the event's first frame, after the stage has placed its secrets (making chunk data before then would miss their
// stamps) - the walls read a slice a frame, the rest on a worker - and the bearers walk from then on, dormant too, so
// their light is seen in the corridors round the cell before anyone comes for it. Their light catches nothing until the
// key is carried.
//
// The bearers are never targets (LanternBearer): not enemies, no group, no collision. The one that turns is the one thing
// that comes for him; it sees only what its light falls on, and it gives up and goes back to its round once he has been
// out of its light 2 s, or after 10 s.
public partial class GaolersKeyEvent : SideEvent
{
	/// <summary>Seconds at the door for the key to turn. GaolSlip.DwellSteps is held to at least this (the validator).</summary>
	public const float DefaultUnlockSeconds = 1.2f;

	[Export] public float UnlockSeconds { get; set; } = DefaultUnlockSeconds;

	private const float CellRadius = 46f;

	/// <summary>Within this of its post, he has come for the key: the Warden leaves its post for him.</summary>
	public const float GuardRadius = 640f;

	/// <summary>The Warden never follows him further than this from its post; past it, it walks back.</summary>
	public const float LeashRadius = 520f;

	// Walking back, it stops this near its post (and then stands its ground until he comes again).
	private const float HomeSlack = 110f;

	// A heading so short Enemy reads it as a deliberate hold: Velocity zero, no separation creep (Enemy, AdjustSteering's
	// rule: a steering under 0.01 long is "hold exactly still"). Non-zero, so it overrides the chase.
	private static readonly Vector2 HoldHeading = new(0.001f, 0f);

	private enum Step { HuntWarden, KeyOnGround, Carrying }

	private Step step = Step.HuntWarden;
	private Enemy warden;
	private Vector2 wardenLastSeen;
	private Vector2 cellPosition;
	private Vector2 postPosition;
	private float unlockProgress;
	private bool wardenWalkingHome;
	private float wardenRouteClock;
	private Vector2 wardenHeadingHome;
	private bool layoutTried;
	private bool posedForPhoto;
	private readonly List<LanternBearer> bearers = new();
	private Vector2I searchMin, searchMax;
	// Where a dropped key lands (Node2DGame.ReachablePoint), made once rather than every frame.
	private System.Func<Vector2, Vector2> dropAt;
	// THE WIN BEAT (in-flight 58): the cell is opened at the cell. The key turns in the lock and
	// nothing moves (the pause before anything does), then the barred gate swings open on its left
	// hinge, the gloom inside lightens and the one sitting there is gone - held open a moment, and
	// only then does the reward column rise, at the exact spot the beat chose. The lanterns gutter out
	// as the key turns: the gaol has lost what it kept.
	private float winBeat = -1f;
	private Vector2 winSpot;
	private const float WinTurnSeconds = 0.35f;
	private const float WinSwingSeconds = 0.9f;
	private const float WinHoldSeconds = 0.45f;

	/// <summary>Where the post, the rounds and the walk graph are: null until the event's first frame.</summary>
	public GaolLayout Layout { get; private set; }

	/// <summary>The lantern-bearers, one per round.</summary>
	public IReadOnlyList<LanternBearer> Bearers => bearers;

	/// <summary>The key's rule and where it lies (KeyCarry).</summary>
	public KeyCarry Key { get; } = new();

	/// <summary>Times the light caught him with the key.</summary>
	public int Catches { get; private set; }

	public bool IsHunting => IsFound && step == Step.HuntWarden && !IsResolved;
	public Enemy Warden => warden;
	public Vector2 CellPosition => cellPosition;
	public Vector2 PostPosition => postPosition;
	public float UnlockProgress => unlockProgress;
	public bool IsUnlocking => winBeat >= 0f;

	/// <summary>True while every bearer walks its round (none turned on him, none walking back).</summary>
	public bool AllOnRounds
	{
		get
		{
			foreach (LanternBearer b in bearers)
				if (b.State != LanternBearer.Duty.Round)
					return false;
			return true;
		}
	}

	/// <summary>The bearer whose light is on <paramref name="feet"/>, or -1: the one authority on "caught" (LanternCone.Covers).</summary>
	public int LightOn(Vector2 feet)
	{
		for (int i = 0; i < bearers.Count; i++)
		{
			LanternBearer b = bearers[i];
			if (b.ConeLive && b.State != LanternBearer.Duty.Gone && b.Cone.Covers(feet))
				return i;
		}
		return -1;
	}

	protected override string Announcement => "A Warden keeps the cell's key - lanterns walk the way between";
	protected override string ChestLine => "The cell stands empty - a chest was left in it";

	protected override string Objective
	{
		get
		{
			foreach (LanternBearer b in bearers)
				if (b.State == LanternBearer.Duty.Turned)
					return "A lantern-bearer has seen you - get out of its light";
			return step switch
			{
				Step.HuntWarden => "Kill the Warden at its post - it keeps the key",
				Step.KeyOnGround => Catches == 0 ? "Take up the key" : "Take the key back once the light has passed",
				_ => "Carry the key to the cell - keep out of the lanterns' light",
			};
		}
	}

	protected override Vector2? PointerTarget => step switch
	{
		Step.HuntWarden => wardenLastSeen,
		Step.KeyOnGround => Key.KeyAt,
		_ => cellPosition,
	};

	protected override string SiteGlyph => "cell";
	protected override float SiteFootprint => 110f;
	protected override float ArrivalRadius => 460f;
	protected override Color TellColor => new Color(0.42f, 0.72f, 0.91f);   // arcane cyan: his mark

	// The locked cell is the site: placed at stage start, out in the maze, and waiting (SideEvent). Arriving at it is
	// what wakes the Warden at its post.
	protected override void OnPlaced()
	{
		cellPosition = MainSite;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Layout == null && !layoutTried && Game != null)
			StartGaol();
		BakeGaol(BakeTilesPerFrame);
		TakeGaol(wait: false);
		float dt = (float)delta;
		if (IsResolved || winBeat >= 0f)
			foreach (LanternBearer b in bearers)
				b.StandDown();
		Player player = Player;
		float scale = Game?.EnemyDamageScale ?? 1f;
		foreach (LanternBearer b in bearers)
			b.Advance(dt, player, scale);
	}

	// THE SEED'S GAOL (GaolLayout) and its bearers. Laid once: from the first frame, or on arrival for a tool that begins it
	// at once. The walls round the cell are read on the main thread - the only part that asks the streamer, which makes
	// chunk data on demand and is not thread-safe - a slice a frame (BakeGaol), and the rest (finding the loops, sweeping
	// them, proving the gap) reads only that baked array, so it runs on a worker. All in one frame it was 313 ms on a
	// desktop debug build, and the bake alone, done at once, 180: on a phone, a visible stall in the chapter's first second.
	// TakeGaol picks the layout up when it is done.
	private System.Threading.Tasks.Task<GaolLayout> gaolTask;
	private GaolGround gaolGround;
	private Vector2I gaolHome;
	private Vector2 gaolAway;
	private bool gaolBaking;
	private long gaolStarted;
	private double gaolBakeMs;
	private int gaolBakeFrames;

	// Wall tiles read a frame: the box is about 23,000, so some seven frames, each touching a chunk or two the streamer has
	// not made the data of yet (1-4 ms each, .ai/terrain.md section 7).
	private const int BakeTilesPerFrame = 3500;

	private void StartGaol()
	{
		layoutTried = true;
		cellPosition = MainSite;
		Vector2 fromStart = cellPosition - (Game?.StageOrigin ?? Vector2.Zero);
		postPosition = Game?.ReachablePoint(cellPosition + (fromStart.LengthSquared() > 1f ? fromStart.Normalized() : Vector2.Right) * 800f)
			?? cellPosition;
		TerrainStreamer terrain = Game?.Terrain;
		if (terrain == null)
			return;
		var ground = new GaolGround
		{
			Origin = Game.StageOrigin,
			Pitch = LevelGenerator.MazeCellTiles * Game.ProceduralTilePixels,
			Tile = Game.ProceduralTilePixels,
			IsWall = terrain.IsWallAtWorld,
		};
		Vector2I home = ground.CellOf(cellPosition);
		searchMin = home - new Vector2I(GaolLayout.SearchRadius, GaolLayout.SearchRadius);
		searchMax = home + new Vector2I(GaolLayout.SearchRadius, GaolLayout.SearchRadius);
		gaolStarted = System.Diagnostics.Stopwatch.GetTimestamp();
		gaolGround = ground;
		gaolHome = home;
		// Beyond the cell from where he comes: the stage's start, which every run walks out from.
		gaolAway = cellPosition - Game.StageOrigin;
		// Nothing to read (already baked) and the first step reports it whole: the worker starts on the next frame either way.
		ground.BeginBake(searchMin, searchMax);
		gaolBaking = true;
	}

	// A slice of the walls a frame; once they are all read, the worker lays the gaol out from them.
	private void BakeGaol(int tiles)
	{
		if (!gaolBaking)
			return;
		long started = System.Diagnostics.Stopwatch.GetTimestamp();
		bool done = gaolGround.BakeStep(tiles);
		gaolBakeMs += (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
		gaolBakeFrames++;
		if (!done)
			return;
		gaolBaking = false;
		GaolGround ground = gaolGround;
		Vector2I home = gaolHome;
		Vector2 away = gaolAway;
		ulong seed = Game.TerrainSeed;
		ground.BakedOnly = true;
		gaolTask = System.Threading.Tasks.Task.Run(() => GaolLayout.Generate(ground, home, away, seed));
	}

	// The worker's gaol, once it is done (or now, waiting for it, when he has arrived before it was: tools that begin the
	// event on the first frames). A failure is reported, never hidden: the cell then has no rounds.
	private void TakeGaol(bool wait)
	{
		if (gaolTask == null || (!wait && !gaolTask.IsCompleted))
			return;
		System.Threading.Tasks.Task<GaolLayout> task = gaolTask;
		gaolTask = null;
		gaolGround.BakedOnly = false;
		try
		{
			Layout = task.Result;
		}
		catch (System.AggregateException failure)
		{
			GD.PushError($"GaolersKeyEvent: laying the gaol out failed - {failure.InnerException}");
			return;
		}
		double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - gaolStarted) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
		if (Layout == null)
		{
			// No cell four to six steps from it: never on the maze (RegressionChecks.ValidateGaolersKey). The old post, no rounds.
			GD.PushWarning($"GaolersKeyEvent: no post found round the cell at {cellPosition}; the Warden wakes beyond it and nothing walks the way.");
			return;
		}
		postPosition = Layout.PostPosition;
		for (int i = 0; i < Layout.Rounds.Count; i++)
		{
			var bearer = LanternBearer.Create(Layout.Rounds[i], gaolGround, i);
			bearer.BoxMin = searchMin;
			bearer.BoxMax = searchMax;
			bearers.Add(bearer);
			AddChild(bearer);
		}
		if (LogLayout)
			GD.Print($"GAOL layout: cell {gaolHome}, post {Layout.Post} ({Layout.Route?.Length - 1} steps), {Layout.Rounds.Count} rounds "
				+ $"[{string.Join(", ", Layout.Rounds.ConvertAll(r => $"{r.Cells.Length} cells {(r.Direction > 0 ? "+" : "-")}"))}], "
				+ $"{Layout.LoopsFound} loops found, {Layout.Attempts} rolls, a straight walk caught {Layout.CaughtShare * 100f:0}% of the time, "
				+ $"slowest slip {Layout.SlowestSlipSeconds:0.0} s{(Layout.Fallback ? ", FALLBACK" : "")}; walls read in {gaolBakeMs:0} ms on the "
				+ $"main thread over {gaolBakeFrames} frame(s), laid out {ms:0} ms after starting (loops {Layout.Cost.Loops:0}, sweeps {Layout.Cost.Sweeps:0}, search {Layout.Cost.Search:0}, "
				+ $"bite {Layout.Cost.Bite:0}){(wait ? " - WAITED for on arrival" : "")}");
	}

	/// <summary>Prints the layout and its cost when the gaol is laid (the probe and --explore-log).</summary>
	public static bool LogLayout { get; set; } = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--explore-log") >= 0;

	protected override void Begin()
	{
		if (Layout == null && !layoutTried)
			StartGaol();
		BakeGaol(int.MaxValue);
		TakeGaol(wait: true);
		cellPosition = MainSite;
		// The chapter's own jailer (StageBestiary): the Warden, whose dungeon this is, at its post.
		warden = Game.SpawnEventEnemy(BestiaryRole.MiniBoss, postPosition, 9f, true);
		wardenLastSeen = warden != null ? warden.GlobalPosition : postPosition;
		if (warden != null)
			warden.ScriptedHeading = HoldHeading;
	}

	protected override void Tick(float delta)
	{
		// The beat: the lock has turned - the door swings and holds before the reward comes.
		if (winBeat >= 0f)
		{
			winBeat += delta;
			if (winBeat >= WinTurnSeconds + WinSwingSeconds + WinHoldSeconds)
				Succeed(winSpot, exact: true);
			return;
		}

		if (step == Step.HuntWarden)
		{
			if (!IsGone(warden))
			{
				wardenLastSeen = warden.GlobalPosition;
				KeepPost(delta);
				return;
			}
			Key.Lay(Game.ReachablePoint(wardenLastSeen));
			step = Step.KeyOnGround;
		}

		// The light read is last frame's (the bearers advance after this Tick): the light he saw is the light that catches.
		int by = posedForPhoto ? -1 : LightOn(Player.FootPosition);
		KeyCarry.Change change = Key.Step(Player.GlobalPosition, by >= 0, dropAt ??= Game.ReachablePoint);
		if (change == KeyCarry.Change.Dropped)
		{
			Catches++;
			unlockProgress = 0f;
			bearers[by].Turn(Player.FootPosition);
			SfxPlayer.AtPosition(SfxCatalog.LanternCatch, Player.GlobalPosition, 0f);
			if (LogLayout)
				GD.Print($"GAOL caught by bearer {by + 1} at {Player.FootPosition}: the key falls at {Key.KeyAt} (catch {Catches})");
		}
		step = Key.Carrying ? Step.Carrying : Step.KeyOnGround;

		if (step == Step.Carrying)
		{
			if (PlayerWithin(cellPosition, CellRadius))
			{
				unlockProgress += delta / UnlockSeconds;
				if (unlockProgress >= 1f)
				{
					winSpot = ChooseRewardSpot(cellPosition);
					winBeat = 0f;
					// The lock's own cue as the key turns; the reward's sound still comes with the column.
					SfxPlayer.AtPosition(SfxCatalog.SecretSeen, cellPosition, 0f);
				}
			}
			else
			{
				unlockProgress = Mathf.Max(0f, unlockProgress - delta);
			}
		}
	}

	// The Warden keeps its post: holds there until he comes within GuardRadius of it, then fights him as any body would -
	// but only so far. Past LeashRadius from its post it walks back by the corridors, and holds again.
	private void KeepPost(float delta)
	{
		Vector2 at = warden.GlobalPosition;
		float fromPost = at.DistanceTo(postPosition);
		bool heIsNear = Player.GlobalPosition.DistanceTo(postPosition) <= GuardRadius;
		if (wardenWalkingHome && fromPost <= HomeSlack)
			wardenWalkingHome = false;
		else if (!wardenWalkingHome && fromPost > LeashRadius)
			wardenWalkingHome = true;

		if (!wardenWalkingHome && heIsNear)
		{
			warden.ScriptedHeading = Vector2.Zero;
			return;
		}
		if (fromPost <= HomeSlack)
		{
			warden.ScriptedHeading = HoldHeading;
			return;
		}
		wardenRouteClock -= delta;
		if (wardenRouteClock <= 0f || wardenHeadingHome == Vector2.Zero)
		{
			wardenRouteClock = 0.25f;
			wardenHeadingHome = HeadingHome(at);
		}
		warden.ScriptedHeading = wardenHeadingHome;
	}

	// Toward the next cell on the corridors' way to its post (the lattice, GaolGround), or straight at it in its own cell.
	private Vector2 HeadingHome(Vector2 at)
	{
		GaolGround ground = Layout?.Ground;
		Vector2 target = postPosition;
		if (ground != null)
		{
			Vector2I here = ground.CellOf(at);
			Vector2I post = ground.CellOf(postPosition);
			if (here != post)
			{
				Vector2I[] path = ground.PathBetween(here, post, searchMin, searchMax);
				if (path != null && path.Length > 1)
					target = ground.CentreOf(path[1]);
			}
		}
		Vector2 to = target - at;
		return to.LengthSquared() > 1f ? to.Normalized() : HoldHeading;
	}

	private static readonly Color Iron = new Color(0.34f, 0.36f, 0.42f);
	private static readonly Color IronLit = new Color(0.62f, 0.64f, 0.70f);
	private static readonly Color Brass = new Color(0.95f, 0.78f, 0.36f);
	private static readonly Color Gloom = new Color(0.03f, 0.03f, 0.05f, 0.92f);

	public override void _Draw()
	{
		if (IsResolved)
			return;

		// The beat's two movements: the key's turn, then the gate's swing (eased, so it lands softly).
		float turn = winBeat < 0f ? 0f : Mathf.Clamp(winBeat / WinTurnSeconds, 0f, 1f);
		float open = winBeat < 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp((winBeat - WinTurnSeconds) / WinSwingSeconds, 0f, 1f));

		// The cell: a dark recess behind bars, with the shape of someone sitting inside. As the door
		// opens the gloom lightens and the sitter is gone - carried out, not kept.
		Vector2 c = ToLocal(cellPosition);
		DrawRect(new Rect2(c - new Vector2(30f, 34f), new Vector2(60f, 60f)), Gloom.Lerp(new Color(0.15f, 0.18f, 0.24f, 0.45f), open));
		if (open < 1f)
		{
			Color sitter = new Color(0.16f, 0.15f, 0.20f, 1f - open);
			DrawCircle(c + new Vector2(0f, -6f), 6f, sitter);
			DrawRect(new Rect2(c + new Vector2(-8f, 0f), new Vector2(16f, 20f)), sitter);
		}
		if (open <= 0f)
		{
			for (int i = 0; i < 6; i++)
			{
				float x = -27f + i * 10.8f;
				DrawLine(c + new Vector2(x, -34f), c + new Vector2(x, 26f), Iron, 3f);
				DrawLine(c + new Vector2(x - 1f, -34f), c + new Vector2(x - 1f, 26f), IronLit, 1f);
			}
		}
		else
		{
			// The gate of six bars and its two rails swings on its bottom-left hinge, out and clear of
			// the doorway, and lies open there through the hold.
			Vector2 hinge = c + new Vector2(-32f, 26f);
			float angle = -1.45f * open;
			float cos = Mathf.Cos(angle);
			float sin = Mathf.Sin(angle);
			Vector2 Swing(Vector2 offset) => hinge + new Vector2(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos);
			for (int i = 0; i < 6; i++)
			{
				float x = -27f + i * 10.8f + 32f;
				DrawLine(Swing(new Vector2(x, -60f)), Swing(new Vector2(x, 0f)), Iron, 3f);
			}
			DrawLine(Swing(new Vector2(5f, -60f)), Swing(new Vector2(59f, -60f)), IronLit, 2f);
			DrawLine(Swing(new Vector2(5f, 0f)), Swing(new Vector2(59f, 0f)), IronLit, 2f);
		}
		DrawRect(new Rect2(c - new Vector2(32f, 36f), new Vector2(64f, 64f)), Iron, false, 3f);
		if (step == Step.Carrying && winBeat < 0f)
			DrawProgressArc(cellPosition, CellRadius, unlockProgress, Brass);

		if (winBeat >= 0f && open <= 0f)
		{
			// The key in the lock, turning, before anything moves.
			DrawSetTransform(c + new Vector2(24f, -4f), -1.3f * turn, Vector2.One);
			DrawKey(Vector2.Zero);
			DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
		}
		else if (step == Step.KeyOnGround)
			DrawKey(ToLocal(Key.KeyAt));
		else if (step == Step.Carrying && winBeat < 0f && Player != null)
			// Clear of the hat: the event draws under the actors now (SideEvent), so a key at the
			// crown of his head would be behind it.
			DrawKey(ToLocal(Player.GlobalPosition) + new Vector2(0f, -64f));
	}

	// The far tell while it waits (base). Once the door swings, the cell's own light spills out of
	// the opening: what he kept in the dark is lit on its way out.
	protected override void DrawLight(CanvasItem light)
	{
		base.DrawLight(light);
		if (step == Step.KeyOnGround && !IsResolved && NearPlayer(Key.KeyAt))
			// A faint pool of the event's own cold round the fallen key, so it is found again in the dark and the crowd. Never
			// warm: a warm glint on the floor is a secret's tell (.ai/stage-secrets.md section 2).
			ExploreArt.DrawGlow(light, ToLocal(Key.KeyAt), 28f, new Color(TellColor, 0.18f + 0.06f * Flicker(7)));
		if (winBeat < 0f || IsResolved || !NearPlayer(cellPosition))
			return;
		float open = Mathf.Clamp((winBeat - WinTurnSeconds) / WinSwingSeconds, 0f, 1f);
		if (open <= 0f)
			return;
		Vector2 c = ToLocal(cellPosition);
		ExploreArt.DrawGlow(light, c + new Vector2(0f, -4f), 50f + 70f * open, new Color(TellColor, 0.22f * open));
		ExploreArt.DrawShaft(light, c + new Vector2(0f, 26f), 14f, 150f * open, new Color(TellColor, 0.30f * open));
	}

	protected override void OnStoodDown()
	{
		foreach (LanternBearer b in bearers)
			b.StandDown();
	}

	/// <summary>
	/// _UiShot --win-event: the key already in hand at the cell door, so the real unlock runs - the
	/// fill, the key's turn, the gate's swing - instead of Succeed called straight (the Warden it
	/// would have taken the key from simply never wakes to miss it). The lanterns hold their light off
	/// him for the photograph: a cone sweeping the door would take the key before the lock had turned.
	/// </summary>
	public void PoseCarryForPhoto()
	{
		if (IsResolved || winBeat >= 0f)
			return;
		Key.Hold();
		step = Step.Carrying;
		posedForPhoto = true;
	}

	private void DrawKey(Vector2 at)
	{
		DrawCircle(at + new Vector2(-6f, 0f), 5f, Brass);
		DrawCircle(at + new Vector2(-6f, 0f), 2.2f, Gloom);
		DrawLine(at + new Vector2(-1f, 0f), at + new Vector2(10f, 0f), Brass, 3f);
		DrawLine(at + new Vector2(7f, 0f), at + new Vector2(7f, 4f), Brass, 2f);
		DrawLine(at + new Vector2(10f, 0f), at + new Vector2(10f, 5f), Brass, 2f);
	}
}
