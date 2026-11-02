using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WizardSurvivors.scripts;

// THE THRONE'S RULES (.ai/endgame-castle.md 6, ChapterRules' hooks). "He is here."
//
// Not a map and not a survival clock: one closed hall, a build made in one breath, and a procession.
// This:
//
//   - pins the pace clock at 15:00 (hook 6), so the world is at its end state from the first second -
//     the spawn curve, enemy health, the XP an ordinary kill pays (x2.5) - which is what the Muster's
//     build should meet; and turns off what a pinned clock would otherwise break or the room does not
//     hold: the survival clock's boss (TimerVictorySeconds 0: the procession brings him), the recurring
//     miniboss, and the elites (their clock would fire every elite of the run at once against a pinned
//     pace, and each carries a chest, where the Throne's chests are counted - two in the Muster and one
//     in each breather);
//   - runs THE MUSTER (6.2): eighteen levels granted at once (Node2DGame.GrantLevelUps; 6.2 said seventeen,
//     moved by measurement - ThroneProcession.MusterLevels), each through the ordinary screen, whose
//     heading reads "The Muster - 5 of 18" and whose arming pause is 0.8 s on the first and 0.3 s on the
//     rest (hook 8); then two relic chests through the chest screen;
//   - runs THE PROCESSION (6.3, ThroneProcession's data): the household guard alone for 45 s, then the
//     road's wardens two at a time (Node2DGame.SendBoss, outside the boss phase), the guard standing down
//     while they stand, a 20 s breather and a chest after each of the first two pairs, then him, whose
//     fall wins the run;
//   - lays out the hall (throne_room.json): the throne on its dais, solid and closed to placement (hook 3),
//     and the great doors behind it, which open when he falls and are the way home (hook 5);
//   - and spawns the guard on the hall's floor out of sight where it can (hook 2).
//
// The cast is StageBestiary.Throne.cs - the Court's Household Guard, Empty Armour and Falconer and the
// bridge's Gate Knight - through the ordinary trickle. Nothing in Node2DGame branches on this chapter.
public partial class ThroneRules : ChapterRules
{
	public const int TilePixels = 48;

	// Renewed every frame of a fight, so it only has to outlast one frame - but a frame is scaled time, and
	// a crowded one under the harness's timescale ran past a quarter of a second and let the guard through.
	private const float GuardHoldSeconds = 1f;

	private static readonly float? PinnedPace = Node2DGame.ReferenceRunSeconds;

	// How far from the wizard the throne may be for the dark wizard to rise before it: in sight on either
	// canvas (SendWave, the finale).
	private const float FinaleInSightPx = 400f;

	/// <summary>Hook 6: the end of the reference run, for the whole of this one.</summary>
	public override float? PinnedPaceSeconds => PinnedPace;

	/// <summary>Where the procession is.</summary>
	public enum ProcessionPhase
	{
		Muster,
		WarmUp,
		Fighting,
		Breather,
		Beat,
		Finale,
	}

	public ProcessionPhase Phase { get; private set; } = ProcessionPhase.Muster;

	/// <summary>The procession's wave being fought or next to come (ThroneProcession.Waves index).</summary>
	public int WaveIndex { get; private set; }

	/// <summary>Muster screens opened so far.</summary>
	public int MusterStep { get; private set; }

	/// <summary>Real seconds the procession has run since the Muster closed.</summary>
	public float ProcessionSeconds { get; private set; }

	/// <summary>
	/// The wardens felled in this run's procession, in the order they fell (BossCatalog ids): what the dark
	/// wizard throws back in his last phase, "the telegraph of each warden the player has beaten"
	/// (.ai/endgame-castle.md 6.4, ThroneDarkWizardBoss).
	/// </summary>
	public IReadOnlyList<string> FelledWardenIds => felledWardenIds;

	private readonly List<string> felledWardenIds = new();

	// The hall, in world pixels.
	private AuthoredLayout grid;
	private Vector2 origin;
	private Rect2 hallWorld;
	private Rect2 throneFootprint;
	private Vector2 throneBase;
	private Vector2 doorsFoot;
	private Node2D holder;
	private Sprite2D doors;
	private bool doorsOpen;

	private float stageClock;
	private int standing;
	private Vector2 lastFellAt;
	private float waveStartedAt;
	private bool musterBegun;
	private int musterLevels = ThroneProcession.MusterLevels;
	private int firstWave;
	// --throne-wave=N was given: the warm-up is cut to two seconds.
	private bool skipWarmUp;
	private readonly RandomNumberGenerator rng = new();

	// --- Hook 1: run start ---------------------------------------------------------------------------

	public override void RunStarted()
	{
		rng.Randomize();
		// No survival clock: the procession brings him. A pinned pace would fire every elite of a run at
		// once (Node2DGame.ShouldSpawnElite reads the pace), and the room has no miniboss.
		Game.TimerVictorySeconds = 0f;
		Game.EnableWardenMiniBoss = false;
		Game.EliteStartTimeSeconds = float.MaxValue;
		Game.SentBossFell += OnWardenFell;

		LoadHall();
		PlaceHall();

		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			// Tools: start at doc wave N (1-4) a moment after the Muster, for photographs and probes.
			if (arg.StartsWith("--throne-wave=", StringComparison.Ordinal)
				&& int.TryParse(arg.Substring("--throne-wave=".Length), out int wave))
			{
				firstWave = Mathf.Clamp(wave - 1, 0, ThroneProcession.Waves.Count - 1);
				skipWarmUp = true;
			}
			// Tools: a Muster of another size, for measuring where the Apotheosis lands (6.6) without a build.
			else if (arg.StartsWith("--throne-muster=", StringComparison.Ordinal)
				&& int.TryParse(arg.Substring("--throne-muster=".Length), out int levels))
				musterLevels = Mathf.Clamp(levels, 1, 40);
		}
		WaveIndex = firstWave;
		GD.Print($"THRONE the hall is {(grid != null ? "built" : "MISSING (open ground)")}; the Muster grants "
			+ $"{musterLevels} levels and {ThroneProcession.MusterRelicChests} relic chests; "
			+ $"the procession has {ThroneProcession.Waves.Count} waves{(skipWarmUp ? $", starting at wave {firstWave + 1}" : string.Empty)}");
		// A frame later, so the first screen opens over a drawn hall rather than inside the run's _Ready.
		Callable.From(BeginMuster).CallDeferred();
	}

	public override void _ExitTree()
	{
		if (Game != null)
			Game.SentBossFell -= OnWardenFell;
		if (holder != null && IsInstanceValid(holder))
			holder.QueueFree();
		holder = null;
		doors = null;
	}

	// --- The Muster (6.2) ----------------------------------------------------------------------------

	private void BeginMuster()
	{
		if (musterBegun || Game == null)
			return;
		musterBegun = true;
		Game.GrantLevelUps(musterLevels);
	}

	/// <summary>Hook 8: each Muster screen says it is the Muster, and arms faster after the first.</summary>
	public override void LevelUpOpening(LevelUpMenu menu, bool granted)
	{
		if (!granted || MusterStep >= musterLevels)
			return;
		MusterStep++;
		menu.HeadingOverride = $"The Muster - {MusterStep} of {musterLevels}";
		menu.ArmingSeconds = MusterStep == 1 ? LevelUpMenu.InputGuardSeconds : LevelUpMenu.MusterArmingSeconds;
		// The relic chests wait behind the last screen: queued once it is up, they open as it closes.
		if (MusterStep == musterLevels)
			Callable.From(QueueMusterChests).CallDeferred();
	}

	private void QueueMusterChests()
	{
		for (int i = 0; i < ThroneProcession.MusterRelicChests; i++)
			Game.OpenChestSelectionMenu();
	}

	// --- The procession (6.3) -------------------------------------------------------------------------

	public override void _Process(double delta)
	{
		// The run processes while paused (it reads the pause itself), and so do its children: the procession
		// must stop for every screen, as the run's own clock does.
		if (Game == null || Game.RunFinished || GetTree().Paused)
			return;
		float d = (float)delta;

		// So the Muster (paused from its first screen to its last chest) is over the first time this runs
		// with nothing left to grant.
		if (Phase == ProcessionPhase.Muster)
		{
			if (!musterBegun || MusterStep < musterLevels || Game.GrantedLevelUpsWaiting > 0)
				return;
			Phase = ProcessionPhase.WarmUp;
			stageClock = 0f;
			GD.Print($"THRONE the Muster is done: level {Game.RunPlayer?.CurrentLevel ?? 0}, "
				+ $"{Game.RunPlayer?.GetOwnedChestItems()?.Count ?? 0} relics; the household guard comes");
			return;
		}

		ProcessionSeconds += d;
		stageClock += d;
		OpenDoorsWhenWon();

		switch (Phase)
		{
			case ProcessionPhase.WarmUp:
				if (stageClock >= (skipWarmUp ? 2f : ThroneProcession.WarmUpSeconds))
					SendWave();
				break;
			case ProcessionPhase.Breather:
				if (stageClock >= ThroneProcession.BreatherSeconds)
					SendWave();
				break;
			case ProcessionPhase.Beat:
				if (stageClock >= ThroneProcession.FinaleBeatSeconds)
					SendWave();
				break;
			case ProcessionPhase.Fighting:
			case ProcessionPhase.Finale:
				// The guard stands down while a fight is on, and the field drains to the bosses, as it does
				// for every boss in the game (ThroneProcession.GuardStandsDownForFights says why).
				if (ThroneProcession.GuardStandsDownForFights)
					Game.HoldTrickle(GuardHoldSeconds);
				LogFight(d);
				break;
		}
	}

	// Every 20 s of a fight, how much of each standing boss is left and where it is from the player: what
	// ThroneProcession's health is tuned against (_BalanceHarness, _BossProbe).
	private float fightLogClock;

	private void LogFight(float delta)
	{
		fightLogClock += delta;
		if (fightLogClock < 20f)
			return;
		fightLogClock = 0f;
		Vector2 at = Game.RunPlayer?.FootPosition ?? Game.StageOrigin;
		GD.Print($"THRONE {ProcessionSeconds - waveStartedAt:0}s into wave {ThroneProcession.Waves[Math.Min(WaveIndex, ThroneProcession.Waves.Count - 1)].Number}: "
			+ string.Join(", ", Game.SentBossesStanding.Select(b =>
				$"{b.BossId} {b.BossHealthFraction * 100f:0}% at {b.GlobalPosition.DistanceTo(at):0} px{(b.IsTargetable ? string.Empty : " (under)")}")));
	}

	private void SendWave()
	{
		if (WaveIndex >= ThroneProcession.Waves.Count)
			return;
		ThroneProcession.Wave wave = ThroneProcession.Waves[WaveIndex];
		Vector2 from = Game.RunPlayer?.FootPosition ?? Game.StageOrigin;
		waveStartedAt = ProcessionSeconds;

		if (wave.Finale)
		{
			BossDefinition him = BossCatalog.ForStageIndex(ThroneProcession.FinaleStageIndex);
			if (him == null)
			{
				GD.PushError("ThroneRules: BossCatalog has no boss for the Throne; the run cannot end in victory.");
				return;
			}
			// Before his throne when the wizard is in sight of it; otherwise where the pairs arrive, a few
			// hundred pixels toward the hall's middle - between the wizard and the throne. His arrival is the
			// story's last entrance and is played on screen (a phone sees about 425 px either side, a PC about
			// 425 px up and down): from the south doors the throne is 1,700 px off, and he rose out of sight.
			Vector2 at = throneBase + new Vector2(0f, 2.5f * TilePixels);
			if (grid == null || at.DistanceTo(from) < 320f || at.DistanceTo(from) > FinaleInSightPx || !IsClear(at, 60f))
				at = ArrivalPoints(from, 1)[0];
			Phase = ProcessionPhase.Finale;
			standing = 1;
			if (Game.SendBoss(him, him.Health, at, finale: true) == null)
				GD.PushError("ThroneRules: he could not be sent; the run cannot end in victory.");
			// Never named on screen (6.4): his banner is his line alone.
			Game.ShowChapterBanner(him.Tagline);
			GD.Print($"THRONE wave {wave.Number}: {him.Id} at real {ProcessionSeconds:0}s (procession), level {Game.RunPlayer?.CurrentLevel ?? 0}");
			return;
		}

		List<Vector2> points = ArrivalPoints(from, wave.Wardens.Count);
		var names = new List<string>();
		standing = 0;
		for (int i = 0; i < wave.Wardens.Count; i++)
		{
			ThroneProcession.Warden warden = wave.Wardens[i];
			BossDefinition definition = BossCatalog.GetById(warden.BossId);
			if (definition == null)
			{
				GD.PushError($"ThroneRules: the procession names '{warden.BossId}', which BossCatalog does not have.");
				continue;
			}
			if (Game.SendBoss(definition, warden.Health, points[i], finale: false) != null)
			{
				standing++;
				names.Add(definition.DisplayName.Split(',')[0].Trim());
			}
		}
		Phase = ProcessionPhase.Fighting;
		Game.ShowChapterBanner(string.Join("  ·  ", names));
		GD.Print($"THRONE wave {wave.Number}: {string.Join(" and ", wave.Wardens.Select(w => $"{w.BossId} ({w.Health})"))} "
			+ $"at {ProcessionSeconds:0}s of the procession, level {Game.RunPlayer?.CurrentLevel ?? 0}");
		if (standing == 0)
			WaveDone();
	}

	private void OnWardenFell(BossDefinition definition, Vector2 fellAt)
	{
		if (Phase != ProcessionPhase.Fighting)
			return;
		lastFellAt = fellAt;
		standing--;
		if (definition != null && !felledWardenIds.Contains(definition.Id))
			felledWardenIds.Add(definition.Id);
		GD.Print($"THRONE {definition.Id} falls {ProcessionSeconds - waveStartedAt:0}s into wave {ThroneProcession.Waves[WaveIndex].Number}, "
			+ $"level {Game.RunPlayer?.CurrentLevel ?? 0}");
		if (standing <= 0)
			WaveDone();
	}

	private void WaveDone()
	{
		ThroneProcession.Wave wave = ThroneProcession.Waves[WaveIndex];
		GD.Print($"THRONE wave {wave.Number} down in {ProcessionSeconds - waveStartedAt:0}s "
			+ $"({ProcessionSeconds:0}s of the procession), level {Game.RunPlayer?.CurrentLevel ?? 0}");
		WaveIndex++;
		stageClock = 0f;
		if (wave.BreatherAfter)
		{
			Phase = ProcessionPhase.Breather;
			Game.PlaceChestWhereBossFell(lastFellAt);
			Game.HoldTrickle(ThroneProcession.BreatherTrickleHoldSeconds);
		}
		else
			Phase = ProcessionPhase.Beat;
	}

	// --- Where the wardens arrive ---------------------------------------------------------------------

	// Two points a few hundred pixels from the player, either side of the line from him toward the
	// middle of the hall, on open floor clear of the pillars and the throne and apart from each other -
	// close enough that both arrivals are on screen on either canvas (a phone sees ~850 px across, a PC
	// ~850 px down), far enough that neither lands on him.
	private List<Vector2> ArrivalPoints(Vector2 from, int count)
	{
		var chosen = new List<Vector2>();
		Vector2 centre = grid != null ? hallWorld.GetCenter() : from + Vector2.Up * 400f;
		Vector2 toward = centre - from;
		Vector2 dir = toward.Length() > 120f ? toward.Normalized() : Vector2.Up;
		float[] spreads = count == 1 ? new[] { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 180f } : new[] { 50f, 35f, 65f, 20f, 80f, 100f, 130f };
		float[] radii = { 360f, 320f, 400f, 280f, 440f, 240f };
		for (int k = 0; k < count; k++)
		{
			float side = count == 1 ? 1f : (k % 2 == 0 ? -1f : 1f);
			Vector2? found = null;
			foreach (float spread in spreads)
			{
				foreach (float r in radii)
				{
					Vector2 p = from + dir.Rotated(Mathf.DegToRad(spread * side)) * r;
					if (!IsClear(p, 64f) || chosen.Any(c => c.DistanceTo(p) < 300f))
						continue;
					found = p;
					break;
				}
				if (found != null)
					break;
			}
			chosen.Add(found ?? Game.ReachablePoint(centre + new Vector2(side * 300f, 0f)));
		}
		return chosen;
	}

	// Open floor in a disc: the centre and eight points round it, none a wall cell or the throne.
	private bool IsClear(Vector2 p, float radius)
	{
		if (grid == null)
			return true;
		if (!IsFloorAt(p))
			return false;
		for (int i = 0; i < 8; i++)
		{
			if (!IsFloorAt(p + Vector2.Right.Rotated(i * Mathf.Tau / 8f) * radius))
				return false;
		}
		return true;
	}

	private bool IsFloorAt(Vector2 world)
	{
		if (grid == null)
			return true;
		if (throneFootprint.HasPoint(world))
			return false;
		Vector2I cell = CellOf(world);
		return grid.Contains(cell) && grid.IsWalkable(cell);
	}

	// --- The hall -------------------------------------------------------------------------------------

	private Vector2I CellOf(Vector2 world)
	{
		Vector2 c = (world - origin) / TilePixels;
		return new Vector2I(Mathf.FloorToInt(c.X), Mathf.FloorToInt(c.Y));
	}

	private Vector2 WorldOf(Vector2 cell) => origin + cell * TilePixels;

	private Rect2 WorldOf(Rect2I cells) => new(WorldOf((Vector2)cells.Position), (Vector2)cells.Size * TilePixels);

	private void LoadHall()
	{
		grid = AuthoredLayout.For(StageEnvironmentKind.Throne);
		if (grid == null)
		{
			GD.PushError("ThroneRules: throne_room.json is missing or unreadable - run python tools/art/throne_room.py. The hall is open ground.");
			return;
		}
		// The layout's origin cell is world tile (0, 0), whose north-west corner is the stage origin.
		origin = Game.StageOrigin - (Vector2)grid.Origin * TilePixels;
		AuthoredLayout.Room hall = grid.RoomById("hall");
		hallWorld = hall != null ? WorldOf(hall.Rect) : WorldOf(new Rect2I(0, 0, grid.Width, grid.Height));

		using JsonDocument doc = JsonDocument.Parse(grid.Source);
		JsonElement root = doc.RootElement;
		throneFootprint = root.TryGetProperty("throneFootprint", out JsonElement fp) ? WorldOf(AuthoredLayout.RectOf(fp)) : default;
		foreach (AuthoredLayout.Piece piece in grid.Furniture)
		{
			if (piece.Kind == "throne")
				throneBase = WorldOf(piece.At);
			else if (piece.Kind == "throne_doors")
				doorsFoot = WorldOf(piece.At);
		}
	}

	// The throne and the great doors, standing among the bodies by where their feet are.
	private void PlaceHall()
	{
		if (grid == null)
			return;
		holder = new Node2D { Name = "ThroneHall", YSortEnabled = true };
		Game.AddChild(holder);

		Texture2D throneTexture = LoadTexture("res://assets/bonelight/world/throne/throne.png");
		if (throneTexture != null)
		{
			holder.AddChild(StandingSprite("Throne", throneTexture, throneBase));
			// Solid on the terrain layer, as a wall is: the player's feet and every enemy stop at it.
			var body = new StaticBody2D { Name = "ThroneBody", Position = throneBase, CollisionLayer = 1u << 3, CollisionMask = 0 };
			body.AddChild(new CollisionShape2D
			{
				Shape = new RectangleShape2D { Size = new Vector2(88f, 44f) },
				Position = new Vector2(0f, -22f),
			});
			holder.AddChild(body);
		}
		// The Court's doors (props_court.py), the same doors seen from his side of them.
		Texture2D shut = LoadTexture("res://assets/bonelight/world/court/throne-doors.png");
		if (shut != null)
		{
			doors = StandingSprite("NorthDoors", shut, doorsFoot);
			holder.AddChild(doors);
		}
	}

	private static Sprite2D StandingSprite(string name, Texture2D texture, Vector2 foot)
	{
		Vector2 size = texture.GetSize();
		return new Sprite2D
		{
			Name = name,
			Texture = texture,
			Centered = false,
			Position = foot,
			Offset = new Vector2(-Mathf.Round(size.X / 2f), -size.Y),
			ZIndex = DrawLayers.Bodies,
			ZAsRelative = false,
		};
	}

	private static Texture2D LoadTexture(string path)
	{
		Texture2D t = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		if (t == null)
			GD.PushError($"ThroneRules: '{path}' is missing or not imported (python tools/art/throne_room.py, then --import).");
		return t;
	}

	private void OpenDoorsWhenWon()
	{
		if (doorsOpen || doors == null || !IsInstanceValid(doors) || !Game.VictoryPortalOpen)
			return;
		doorsOpen = true;
		Texture2D open = LoadTexture("res://assets/bonelight/world/court/throne-doors-open.png");
		if (open != null)
			doors.Texture = open;
	}

	// --- Hook 2: the guard comes from out of sight, on the hall's floor ------------------------------

	/// <summary>
	/// A point on the hall's floor outside what the camera shows, where there is any (a PC sees the hall's
	/// width but not its length, a phone the reverse); failing that, the farthest of a few from the player.
	/// Node2DGame vets it as it vets any.
	/// </summary>
	public override bool TryPickSpawnPoint(Vector2 playerPosition, out Vector2 point)
	{
		point = default;
		if (grid == null)
			return false;
		Viewport viewport = GetViewport();
		Camera2D camera = viewport?.GetCamera2D();
		// The run's zoom, not a boss arrival's push-in (Node2DGame.GameplayZoomOf): a procession's pair arrives
		// while the household guard keeps coming.
		float zoom = Node2DGame.GameplayZoomOf(camera);
		Vector2 half = (viewport?.GetVisibleRect().Size ?? new Vector2(1280f, 720f)) / zoom * 0.5f;
		Vector2 seen = camera?.GetScreenCenterPosition() ?? playerPosition;
		var view = new Rect2(seen - half - Vector2.One * 24f, half * 2f + Vector2.One * 48f);
		Rect2 floor = hallWorld.Grow(-TilePixels * 0.5f);

		Vector2 farthest = default;
		float farthestDistance = -1f;
		for (int i = 0; i < 14; i++)
		{
			var p = new Vector2(rng.RandfRange(floor.Position.X, floor.End.X), rng.RandfRange(floor.Position.Y, floor.End.Y));
			if (!IsFloorAt(p))
				continue;
			if (!view.HasPoint(p))
			{
				point = p;
				return true;
			}
			float distance = p.DistanceTo(playerPosition);
			if (distance > farthestDistance)
			{
				farthest = p;
				farthestDistance = distance;
			}
		}
		if (farthestDistance < 420f)
			return false;
		point = farthest;
		return true;
	}

	// --- Hook 3: the throne is solid ------------------------------------------------------------------

	public override bool IsPlacementBlocked(Vector2 point) => throneFootprint.HasPoint(point);

	// --- Hook 5: the way home is through the doors behind the throne ----------------------------------

	/// <summary>At the foot of the great doors behind the throne (6.4's end): they open when he falls.</summary>
	public override Vector2? VictoryPortalPoint(Vector2 bossFellAt) =>
		grid == null ? null : doorsFoot + new Vector2(0f, TilePixels * 0.9f);
}
