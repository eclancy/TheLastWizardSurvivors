using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// Drives one chapter boss, headless, from the real game scene, and prints what it actually did.
//
// Run:
//   "$GODOT_BIN" --headless --path . scenes/_BossProbe.tscn -- --stage=1
//   "$GODOT_BIN" --headless --path . scenes/_BossProbe.tscn -- --stage=all
//   ... --stage=2 --no-skirt   (the before of the slide, the skirt and the prop clusters - the A/B for
//                              the "held" line, .ai/enemy-behaviour.md section 8 item 13)
//   ... --stage=2 --cc=all     (the bosses' crowd-control rule, Enemy "Bosses and crowd control": the
//                              wizard stands still and every body is frozen, staggered and pulled
//                              away from him on every frame - does it still reach him and attack?
//                              --no-cc-cap is the before, in the same build)
//
// WHY THIS EXISTS. A boss is thirty minutes into a run, so every one of these fights had been
// written, compiled, and validated without anybody watching it move. dotnet build sees none of it
// - not the scene, not the exported tuning, not the telegraph - and RegressionChecks only sees
// numbers, so a boss whose burrow never surfaces or whose phase change throws on the frame it
// fires passes both and fails in front of a player.
//
// It shortens the survival clock to a few seconds and then BLEEDS the boss on a fixed timer
// rather than waiting for the loadout to kill it. That is the whole trick: a scripted drain walks
// every boss through every phase threshold in a known time, so the probe reports on the fight
// rather than on whatever build the level-up RNG happened to hand the bot.
//
// WHAT IT CANNOT TELL YOU. Nothing here is a judgement about whether a fight is fun, fair or
// readable - it renders nothing and reads nothing. It answers four questions only: did the boss
// arrive, did it attack, did the burrowers actually submerge and come back, and did it die
// cleanly. Everything else still needs eyes.
public partial class _BossProbe : Node
{
	[Export] public int StageIndex { get; set; } = 1;
	[Export] public float TimeScale { get; set; } = 8f;
	[Export] public float BossTimerSeconds { get; set; } = 6f;
	[Export] public float DrainSeconds { get; set; } = 26f;
	[Export] public float GiveUpSeconds { get; set; } = 140f;
	[Export] public bool FocusOneBody { get; set; }

	// --approach: for a boss that comes down only to a wizard who has reached a place - the Constable
	// (.ai/endgame-castle.md 3.6) holds the gate from the wall until the wizard stands at its foot. The
	// probe's clock brings the boss in seconds, long before a bot could walk a chapter to it, so on the
	// first frame the wizard is set down ApproachStartPx short of the chapter's boss point (ChapterRules
	// hook 4) - the probe does not walk the bridge, it starts where the walk ends - and from then on he
	// walks to that point and holds it, leaning away from what is close. He is kept whole, because
	// walking into the fire is the point, and the drain bites only a body that is in reach (IsTargetable),
	// so a boss out of reach is not walked through its phases by the probe's clock.
	[Export] public bool Approach { get; set; }
	private const float ApproachStartPx = 900f;
	private Vector2? approachTo;

	// --cc=KIND: the bosses' crowd-control rule (Eric, 0.10.0: "Bosses should also be immune to CC that
	// would make it impossible for them to ever get to the player"). Once the arrival is over the wizard
	// stands where he is, kept whole, and every boss body is given KIND on every frame, harder than any
	// spell gives it: freeze (ApplySlow 0, Event Horizon's stopped time is the same call), slow (a 0.05
	// slow), stagger (ApplyShock 1 s), pull (ApplyKnockback 900 px/s straight AWAY from him - every pull,
	// drag and wall is this call, and away is the worst a pull can do), all (every one at once), or none,
	// the baseline. The drain is slowed so the fight lasts the window. It reports, per run, how close
	// the nearest body came, when it first came within CcReachPx, the attacks begun, and the share of
	// body-time held still.
	private string CrowdControl = "";
	private bool drainGiven;
	private bool CrowdControlTest => CrowdControl.Length > 0;
	private const float CcWindowSeconds = 45f;
	private const float CcReachPx = 150f;
	private float ccStartedAt = -1f, ccStartDistance = -1f, ccClosest = float.MaxValue, ccReachedAt = -1f;
	private int ccAttacksAtStart, ccHeldFrames, ccBodyFrames;
	private Vector2 ccStand;

	private Node game;
	private Node2D player;
	private BossEnemy boss;
	private float elapsed;
	private float sinceDrain;
	private bool finished;

	// What the run is asked to prove, collected as it happens.
	private bool sawBoss;
	private bool sawUntargetable;
	private bool sawRetargetable;
	private int peakPhase;
	private int drainTicks;
	private float entranceSeenAt = -1f, entranceEndedAt = -1f;
	private int hpAtEntrance, phaseCountSeen;
	private bool sawSpellsHeld, hurtDuringEntrance, releaseChecked;
	// The cinematic (2026-11-01): the wizard stands while the bot pushes, the bars come in, the camera pushes
	// in; and once it lets go he walks again. Measured from the frame the arrival is first seen.
	private Vector2 entranceStartPosition, releasedAt;
	private float driftDuringEntrance, peakCinematicZoom = 1f, releaseSeenAt = -1f;
	private int pushedFramesDuringEntrance;
	private bool sawBarsIn, walkChecked;
	// The camera through the arrival (2026-11-02, Eric: "not pan the camera immediately"): when the pan
	// began after the arrival was first seen, and the fastest it moved and when, in world px per second.
	private Vector2 camLast;
	private bool camSeen;
	private float panBeganAt = -1f, peakCamSpeed, peakCamSpeedAt;
	// The player's spell nodes through the arrival (Player.SpellGroup): the most seen at once, and the most
	// of those still processing on any frame of it - which should be none.
	private int spellNodesSeen, spellNodesLive, bossEntranceFramesSeen;
	private float bossArrivedAt;
	private readonly List<string> notes = new();

	private static readonly string[] MoveActions = { "move_left", "move_right", "move_up", "move_down" };

	public override void _Ready()
	{
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			if (arg.StartsWith("--stage=")) StageIndex = int.Parse(arg.Substring(8));
			else if (arg.StartsWith("--timescale=")) TimeScale = float.Parse(arg.Substring(12));
			else if (arg.StartsWith("--drain=")) { DrainSeconds = float.Parse(arg.Substring(8)); drainGiven = true; }
			else if (arg.StartsWith("--drain-interval=")) DrainInterval = float.Parse(arg.Substring(17));
			else if (arg == "--focus") FocusOneBody = true;
			// --boss-rush: the run is a Boss Rush, so the first arrival is the rush's shorter one
			// (Node2DGame.BossRush*); read its "arrival held the field" note against a chapter's.
			else if (arg == "--boss-rush") Global.BossRush = true;
			else if (arg == "--no-skirt") Enemy.TerrainSkirting = false;
			else if (arg == "--approach") Approach = true;
			else if (arg.StartsWith("--cc=")) CrowdControl = arg.Substring(5);
			else if (arg == "--no-cc-cap") Enemy.CrowdControlCapping = false;
			// For a fight drained slowly (--drain) to stand in for a build's damage, longer than 140 s.
			else if (arg.StartsWith("--give-up=")) GiveUpSeconds = float.Parse(arg.Substring(10), System.Globalization.CultureInfo.InvariantCulture);
		}

		// Immune to the pause, or the first level-up stops the probe dead.
		ProcessMode = ProcessModeEnum.Always;

		BossDefinition definition = BossCatalog.ForStageIndex(StageIndex);
		GD.Print($"BOSSPROBE stage={StageIndex} boss={definition?.Id ?? "NONE"} timescale={TimeScale}");
		if (definition == null)
		{
			GD.Print("BOSSPROBE no boss for that stage - nothing to drive.");
			GetTree().Quit();
			return;
		}

		if (CrowdControlTest)
		{
			// The fight has to outlast the window, so the drain is slowed to a trickle that still walks
			// the thresholds eventually.
			// (unless --drain says otherwise: the Choir's revive under a freeze wants a voice down).
			if (!drainGiven)
				DrainSeconds = Math.Max(DrainSeconds, 600f);
			GiveUpSeconds = Math.Min(GiveUpSeconds, CcWindowSeconds + 40f);
		}

		Global.SelectedCharacterIdx = 1;
		// --character=N --start-spell=ID (the wizard opens on that spell in place of his own, Global.ToolStartingSpellId):
		// another loadout through the arrival - the cutscene freeze (Player.SpellGroup) wants a spell that stays out, the
		// Familiar or the Spiritual Weapon.
		bool startSpellGiven = false;
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			if (arg.StartsWith("--character=") && int.TryParse(arg.Substring("--character=".Length), out int who))
				Global.SelectedCharacterIdx = who;
			else if (arg.StartsWith("--start-spell="))
			{
				Global.ToolStartingSpellId = arg.Substring("--start-spell=".Length);
				startSpellGiven = true;
			}
		}
		// Never left from an earlier tool in the same process.
		if (!startSpellGiven)
			Global.ToolStartingSpellId = string.Empty;
		Global.SelectedStageIdx = StageIndex;
		Engine.TimeScale = TimeScale;
		GameStats.ResetRunTelemetry();

		game = ResourceLoader.Load<PackedScene>("res://scenes/node_2d_game.tscn").Instantiate();
		// Set before the add so _Ready sees it: the boss phase is gated on this and fifteen real
		// minutes of survival is not a test anyone runs twice. A clock set before the add is a tool's own,
		// and Node2DGame.ResolveRunLength leaves it alone: the chapter's own length (twenty minutes on the far
		// shore, StageCatalog.VictorySecondsFor) still sets the XP curve, and the pace clock still reaches its
		// end state when the boss arrives, as on any chapter.
		game.Set("TimerVictorySeconds", BossTimerSeconds);
		GD.Print($"BOSSPROBE the chapter's clock is {StageCatalog.VictorySecondsFor(StageCatalog.GetByIndex(StageIndex)?.Id) / 60f:0.#} minutes; shortened to {BossTimerSeconds:0.#} s");
		AddChild(game);
	}

	public override void _ExitTree()
	{
		Engine.TimeScale = 1.0f;
		// A static Resource that outlived the engine is reported as still in use at exit.
		darknessProbe = null;
		foreach (string a in MoveActions)
			Input.ActionRelease(a);
	}

	public override void _Process(double delta)
	{
		if (finished)
			return;

		elapsed += (float)delta;

		if (player == null || !IsInstanceValid(player))
		{
			player = FindNode<Player>(game);
			if (player == null)
				return;
			if (Approach)
				PlaceForApproach();
		}

		AutoPickLevelUp();
		AutoPickChest();
		if (CrowdControlTest && entranceEndedAt >= 0f)
			HoldStillForCrowdControl();
		else
			DriveMovement();
		if (procession == null && Node2DGame.Current is Node2DGame run)
		{
			// The Throne (.ai/endgame-castle.md 6.3): a procession of pairs and then him, sent by its rules,
			// not one boss on the clock. Its fights are drained like any other; the probe waits for the way
			// home instead of stopping at the first fall.
			procession = run.Rules is ThroneRules;
			if (procession == true)
			{
				GiveUpSeconds = Math.Max(GiveUpSeconds, ProcessionGiveUpSeconds);
				GD.Print("BOSSPROBE the Throne: walking the whole procession");
			}
		}
		if (procession == true)
		{
			// The procession is fought in the end-of-run swarm (the pace is pinned at 15:00), which kills
			// the kiting bot long before the last pair: the probe is about the bosses, so the wizard is kept
			// standing, as the balance harness's --immortal keeps him.
			if (player is Player wizard)
			{
				wizard.MaxHP = Math.Max(wizard.MaxHP, 100000);
				wizard.CurrentHP = wizard.MaxHP;
			}
			TrackProcession();
		}
		TrackBoss((float)delta);
		if (CrowdControlTest)
			TickCrowdControlTest();
		if (Approach && player is Player kept && !kept.IsDead)
			kept.CurrentHP = kept.MaxHP;

		if (elapsed >= GiveUpSeconds)
			Finish("ran out of clock");
	}

	// --- the Throne's procession -------------------------------------------------------------------
	// Null until the run exists; true for a chapter whose rules send its bosses (ThroneRules).
	private bool? procession;
	private const float ProcessionGiveUpSeconds = 900f;
	private int processionWave;
	private readonly HashSet<ulong> processionSeen = new();
	private readonly Dictionary<string, (int Peak, int Of)> processionPhases = new();
	private readonly Dictionary<string, int> processionInGround = new();

	// Each wave as it arrives, each boss's deepest phase, and the way home when he falls.
	private void TrackProcession()
	{
		var standing = new List<BossEnemy>();
		CollectBosses(game, standing);
		var arrivals = standing.Where(b => processionSeen.Add(b.GetInstanceId())).ToList();
		if (arrivals.Count > 0)
		{
			processionWave++;
			notes.Add($"wave {processionWave} at {elapsed:0.0}s: " + string.Join(", ", arrivals.Select(b => $"{b.BossId} {b.Health} HP")));
		}
		foreach (BossEnemy b in standing)
		{
			processionPhases.TryGetValue(b.BossId, out var seen);
			processionPhases[b.BossId] = (Math.Max(seen.Peak, b.BossPhase), b.PhaseCount);
		}
	}

	// A chest screen the run opened (the Throne's Muster opens two): take the first relic of an honest
	// offer, as the balance bot deals its own (no menu reports what it shows).
	private readonly ChestOfferMemory chestMemory = new();
	private readonly RandomNumberGenerator chestRng = new();

	private void AutoPickChest()
	{
		ChestItemSelectionMenu menu = FindNode<ChestItemSelectionMenu>(game);
		if (menu == null || menu.IsQueuedForDeletion() || player is not Player wizard)
			return;
		List<string> offer = ChestItemCatalog.GetChestItemOptions(wizard.GetOwnedChestItems(), chestRng, 3,
			new[] { ChestItemCatalog.ScribesPenknife }, chestMemory);
		if (offer.Count > 0)
			menu.EmitSignal(ChestItemSelectionMenu.SignalName.ItemSelected, offer[0]);
	}

	// EVERY boss body on the field, not just the first one found. The Hollow Choir is three of
	// them and its whole mechanic is that they have to fall together, so a probe that drained one
	// would report a fight that never ends and tell you nothing about the one that exists.
	private readonly List<BossEnemy> bodies = new();
	// How much bigger than it arrived any body ever got. The hit punch once read its rest size
	// mid-punch, so a boss hit faster than the punch eased back grew without limit; this is the
	// number that showed it, and it should stay at the punch itself (1.08 at most).
	private readonly Dictionary<BossEnemy, float> arrivalScale = new();
	private float peakScaleGrowth = 1f;
	// Frames a targetable body spent, summed over the bodies, and how many of them on a wall or water cell.
	private int bodyFrames, bodyFramesInGround;
	// HELD: a body that asked to walk (Enemy.IntendedSpeed, at least 0.4 of its speed), covered under
	// 0.15 of that walk in a two-second window and is touching something - the boss's share of
	// _MazeProbe's stuck count, split by what it touches. "The Hollow Choir pathing can get stuck"
	// (Eric, 0.10.0) is this number. A body that covered little and touches nothing is easing onto a
	// point it has nearly reached (a Choir voice on its standing place), not held, and is counted apart.
	private const float HeldWindow = 2f;
	private float heldClock;
	private readonly Dictionary<BossEnemy, Vector2> heldLast = new();
	private int heldEligible, heldWindows, heldOnGround, heldOnProp, heldOnBody, heldTouchingNothing;

	private void TrackBoss(float delta)
	{
		bodies.Clear();
		CollectBosses(game, bodies);

		if (bodies.Count == 0)
		{
			// They were here and now they are not: that is the win, and the only clean way out - once
			// the fall has played and freed the body (BossDeath). A fall that never ends would leave a
			// corpse per boss in the tree, ten of them in a Boss Rush, so it is waited for and timed.
			// In a procession a wave falling is not the end: the next comes, and the end is the way home.
			if (sawBoss && procession == true && !(Node2DGame.Current?.VictoryPortalOpen ?? false))
			{
				fellAt = -1f;
				return;
			}
			if (sawBoss)
			{
				if (fellAt < 0f)
					fellAt = elapsed;
				int corpses = CountBossNodes(game);
				// The Throne's end: the dark lifted, then the way home, then the ending over the results
				// (ThroneEnding), checked once.
				if (corpses == 0 && darkWizard != null && !endingChecked)
				{
					TrackDarkWizardFall();
					if (!darkVeilGoneAfterFall && elapsed - darkFellAt <= 3f)
						return;
					if (!endingEntered)
					{
						endingEntered = true;
						endingEnteredAt = elapsed;
						Node2DGame.Current?.EnterVictoryPortal();
						return;
					}
					if (elapsed - endingEnteredAt < 1.5f)
						return;
					endingChecked = true;
					GameOverScreen results = FindNode<GameOverScreen>(game);
					ThroneEnding ending = results?.Ending;
					notes.Add(ending != null
						? $"the ending is over the results: {ending.StandingIds.Count} wizards in the row ({string.Join(", ", ending.StandingIds)})"
						: $"NO ENDING over the results (results screen {(results != null ? "up" : "missing")})");
				}
				if (corpses == 0)
					Finish($"boss died; its fall played for {elapsed - fellAt:0.0}s and freed the body");
				else if (elapsed - fellAt > CorpseGiveUpSeconds)
				{
					GD.PushError($"BOSSPROBE stage {StageIndex}: {corpses} fallen boss bod{(corpses == 1 ? "y was" : "ies were")} never freed.");
					Finish("boss died, but its body was never freed");
				}
			}
			return;
		}

		if (!sawBoss)
		{
			sawBoss = true;
			bossArrivedAt = elapsed;
			boss = bodies[0];
			phaseCountSeen = boss.PhaseCount;
			notes.Add($"{bodies.Count} bodies arrived at {elapsed:0.0}s with {bodies[0].Health} HP each");
		}
		TrackEntranceBeats(delta);

		// The arrival (2026-10-31): every body untargetable and still, the field held, the wizard's spells
		// silent and nothing hurting him, until the last body has landed. Timed, and nothing drained
		// until it is over - an arrival is not part of the fight it opens.
		// Landed and waiting for its fight (BossEnemy.IsAwaitingFight) is still the arrival: the field is held until it ends.
		bool anyEntering = bodies.Any(b => b.IsEntering || b.IsAwaitingFight);
		if (anyEntering)
		{
			Player wizard = player as Player;
			if (entranceSeenAt < 0f)
			{
				entranceSeenAt = elapsed;
				hpAtEntrance = wizard?.CurrentHP ?? 0;
				entranceStartPosition = player.GlobalPosition;
			}
			if (wizard != null && wizard.HeldForBossEntrance)
				sawSpellsHeld = true;
			// The bot keeps kiting through the arrival (DriveMovement), so a wizard who stands is one held.
			driftDuringEntrance = Math.Max(driftDuringEntrance, player.GlobalPosition.DistanceTo(entranceStartPosition));
			if (MoveActions.Any(a => Input.IsActionPressed(a)))
				pushedFramesDuringEntrance++;
			if (Node2DGame.Current is Node2DGame filming)
			{
				sawBarsIn |= filming.LetterboxShown >= 0.999f;
				peakCinematicZoom = Math.Max(peakCinematicZoom, filming.CinematicZoomFactor);
			}
			int seen = 0, live = 0;
			foreach (Node spellNode in GetTree().GetNodesInGroup(Player.SpellGroup))
			{
				seen++;
				if (spellNode.CanProcess())
					live++;
			}
			spellNodesSeen = Math.Max(spellNodesSeen, seen);
			if (bossEntranceFramesSeen++ > 2)
				spellNodesLive = Math.Max(spellNodesLive, live);
			if (player.GetNodeOrNull<Camera2D>("Camera2D") is Camera2D cam && delta > 0.0001f)
			{
				Vector2 at = cam.Position;
				if (camSeen)
				{
					float speed = at.DistanceTo(camLast) / delta;
					if (speed > peakCamSpeed)
					{
						peakCamSpeed = speed;
						peakCamSpeedAt = elapsed - entranceSeenAt;
					}
					if (panBeganAt < 0f && speed > 4f)
						panBeganAt = elapsed - entranceSeenAt;
				}
				camLast = at;
				camSeen = true;
			}
			if (wizard != null && wizard.CurrentHP < hpAtEntrance)
				hurtDuringEntrance = true;
			return;
		}
		if (entranceSeenAt >= 0f && entranceEndedAt < 0f)
			entranceEndedAt = elapsed;
		else if (entranceEndedAt >= 0f && !releaseChecked)
		{
			// A frame after the last body landed: the run lets go in its own _Process, after this one's.
			releaseChecked = true;
			Player wizard = player as Player;
			notes.Add($"arrival held the field {entranceEndedAt - entranceSeenAt:0.0}s; spells held: {sawSpellsHeld}; "
				+ $"hurt during it: {hurtDuringEntrance}; still held a frame after: spells {wizard?.HeldForBossEntrance}, field {(Node2DGame.Current?.BossEntranceHeld ?? false)}");
			notes.Add($"cinematic: the wizard moved {driftDuringEntrance:0.0} px during it with the bot pushing on {pushedFramesDuringEntrance} frames; "
				+ $"bars all the way in: {sawBarsIn}; push-in peak x{peakCinematicZoom:0.00}");
			notes.Add($"camera: the pan began {(panBeganAt < 0f ? "never" : $"{panBeganAt:0.00}s")} into the arrival; "
				+ $"fastest {peakCamSpeed:0} px/s at {peakCamSpeedAt:0.00}s");
			int frozenAfter = 0;
			foreach (Node spellNode in GetTree().GetNodesInGroup(Player.SpellGroup))
				if (!spellNode.CanProcess())
					frozenAfter++;
			notes.Add($"spells: {spellNodesSeen} spell nodes out during it, at most {spellNodesLive} still processing; "
				+ $"{frozenAfter} still frozen a frame after");
			releasedAt = player.GlobalPosition;
			releaseSeenAt = elapsed;
		}
		else if (releaseChecked && !walkChecked && elapsed - releaseSeenAt >= 0.5f)
		{
			// Half a second (of the probe's scaled clock) after the hold let go: free to walk again, the bars
			// gone and the camera back at the run's own zoom.
			walkChecked = true;
			Node2DGame filming = Node2DGame.Current;
			notes.Add($"after the arrival: walked {player.GlobalPosition.DistanceTo(releasedAt):0} px in 0.5 s; "
				+ $"bars {(filming?.LetterboxShown ?? 0f):0.00}, push-in x{(filming?.CinematicZoomFactor ?? 1f):0.00}");
		}

		heldClock += delta;
		if (heldClock >= HeldWindow)
		{
			foreach (BossEnemy body in bodies)
			{
				Vector2 at = body.GlobalPosition;
				float walk = body.Speed * body.SwarmSpeedScale;
				if (heldLast.TryGetValue(body, out Vector2 was) && body.IsTargetable && walk > 1f
					&& body.IntendedSpeed >= walk * 0.4f)
				{
					heldEligible++;
					if (at.DistanceTo(was) < walk * heldClock * 0.15f)
					{
						GodotObject touching = body.GetSlideCollisionCount() > 0 ? body.GetSlideCollision(0).GetCollider() : null;
						if (touching == null)
							heldTouchingNothing++;
						else
						{
							heldWindows++;
							if (touching is TileMapLayer or WaterSurface)
								heldOnGround++;
							else if (touching is StaticBody2D)
								heldOnProp++;
							else
								heldOnBody++;
						}
					}
				}
				heldLast[body] = at;
			}
			heldClock = 0f;
		}

		TrackChoir(delta);
		TrackHayward();

		foreach (BossEnemy body in bodies)
		{
			if (!body.IsTargetable)
				sawUntargetable = true;
			else if (sawUntargetable)
				sawRetargetable = true;

			peakPhase = Math.Max(peakPhase, body.BossPhase);
			// On walled ground a body can be put where it cannot stand: an echo placed on a ring round
			// its voice, a burrower surfacing at a random point near the player. Counted only while it is
			// out (a burrower under the floor is driven straight through anything, as it should be).
			if (body.IsTargetable && Node2DGame.Current?.Terrain is TerrainStreamer ground)
			{
				bodyFrames++;
				if (ground.IsWallAtWorld(body.GlobalPosition) || ground.IsBlockedAtWorld(body.GlobalPosition))
				{
					bodyFramesInGround++;
					// In a procession, whose: a closed hall is where a lane, a dive or an orbit meets a wall.
					if (procession == true)
						processionInGround[body.BossId] = processionInGround.GetValueOrDefault(body.BossId) + 1;
				}
			}
			if (!arrivalScale.TryGetValue(body, out float arrived))
				arrivalScale[body] = arrived = Mathf.Abs(body.Scale.X);
			if (arrived > 0f)
				peakScaleGrowth = Math.Max(peakScaleGrowth, Mathf.Abs(body.Scale.X) / arrived);
		}

		// The scripted drain. A fixed fraction of max health per tick, so every boss reaches every
		// threshold at a known time no matter what the bot is doing.
		sinceDrain += delta;
		if (sinceDrain < DrainInterval)
			return;

		sinceDrain = 0f;
		drainTicks++;
		// --focus drains only the first body. It exists for the Hollow Choir: draining all three at
		// one rate kills them on the same frame, so the revive - the entire mechanic - never fires
		// and the probe reports a fight it did not actually test.
		foreach (BossEnemy body in FocusOneBody ? bodies.Take(1) : bodies)
		{
			if (!IsInstanceValid(body) || (Approach && !body.IsTargetable))
				continue;
			int bite = Mathf.Max(1, Mathf.RoundToInt(body.MaxHealth * DrainInterval / Mathf.Max(0.5f, DrainSeconds)));
			body.TakeDamage(bite);
		}
	}

	// --cc: the wizard stands where the arrival left him (no keys), and is kept whole - the question is
	// whether the boss comes, not whether he survives it.
	private void HoldStillForCrowdControl()
	{
		foreach (string a in MoveActions)
			Input.ActionRelease(a);
		if (player is Player wizard && !wizard.IsDead)
			wizard.CurrentHP = wizard.MaxHP;
	}

	private void TickCrowdControlTest()
	{
		if (entranceEndedAt < 0f || bodies.Count == 0 || player == null)
			return;
		if (ccStartedAt < 0f)
		{
			ccStartedAt = elapsed;
			ccStand = player.GlobalPosition;
			ccStartDistance = bodies.Min(b => b.GlobalPosition.DistanceTo(ccStand));
			ccAttacksAtStart = bodies.Sum(b => b.AttacksBegun);
		}
		float since = elapsed - ccStartedAt;
		if (since > CcWindowSeconds)
		{
			int attacks = bodies.Sum(b => b.AttacksBegun) - ccAttacksAtStart;
			notes.Add($"--cc={CrowdControl}: started {ccStartDistance:0} px away; closest {ccClosest:0} px; "
				+ $"within {CcReachPx:0} px {(ccReachedAt >= 0f ? $"after {ccReachedAt:0.0}s" : "NEVER")}; "
				+ $"{attacks} attacks begun in {CcWindowSeconds:0}s; held still {(ccBodyFrames > 0 ? 100f * ccHeldFrames / ccBodyFrames : 0f):0}% of body-frames");
			Finish($"crowd-control test ({CrowdControl}) complete");
			return;
		}

		bool all = CrowdControl == "all";
		foreach (BossEnemy body in bodies)
		{
			if (!IsInstanceValid(body) || !body.IsTargetable)
				continue;
			if (all || CrowdControl == "freeze")
				body.ApplySlow(0f, 2f);
			if (all || CrowdControl == "slow")
				body.ApplySlow(0.05f, 2f);
			if (all || CrowdControl == "stagger")
				body.ApplyShock(1f);
			if (all || CrowdControl == "pull")
			{
				Vector2 away = body.GlobalPosition - ccStand;
				body.ApplyKnockback((away.LengthSquared() > 1f ? away.Normalized() : Vector2.Right) * 900f);
			}
			ccBodyFrames++;
			if (body.IsHeldByCrowdControl)
				ccHeldFrames++;
			float d = body.GlobalPosition.DistanceTo(ccStand);
			ccClosest = Math.Min(ccClosest, d);
			if (d <= CcReachPx && ccReachedAt < 0f)
				ccReachedAt = since;
		}
	}

	// THE HOLLOW CHOIR (2026-11-01): a fallen voice lies until another walks over and sings it back, a
	// voice far from its place or stuck dips under and comes up there. Counted: each fall and each rise
	// with the time between, the dips, and the song's seconds paused by crowd control on the singer.
	private readonly Dictionary<HollowChoirBoss, float> choirFellAt = new();
	private readonly List<float> choirDownTimes = new();
	private int choirFalls;
	private readonly Dictionary<ulong, (int Dips, float Held)> choirVoices = new();

	private void TrackChoir(float delta)
	{
		foreach (BossEnemy body in bodies)
		{
			if (body is not HollowChoirBoss voice)
				continue;
			choirVoices[voice.GetInstanceId()] = (voice.DipsTaken, voice.SongHeldSeconds);
			bool fallen = voice.IsFallen;
			bool was = choirFellAt.ContainsKey(voice);
			if (fallen && !was)
			{
				choirFellAt[voice] = elapsed;
				choirFalls++;
				notes.Add($"a voice fell at {elapsed:0.0}s");
			}
			else if (!fallen && was)
			{
				choirDownTimes.Add(elapsed - choirFellAt[voice]);
				choirFellAt.Remove(voice);
				notes.Add($"a voice was sung back up at {elapsed:0.0}s");
			}
		}
	}

	// THE HAYWARD'S COMBO (2026-11-07, .ai/in-flight.md 57): the beats as the fight runs them,
	// snapshotted while the body is valid - an attack count alone cannot say whether the swing
	// chained into its charge, or whether the charge really left him spent.
	private (int Swings, int Charges, int Rings, int Spent, int Turned) haywardBeats;
	private bool sawHayward;

	// The Unstrung Hunter's draws (F3): how many he loosed, and how many a barrow caught - snapshotted while he stands.
	private (int Looses, int IntoBarrows, int Phase) hunterDraws;
	private bool sawHunter;

	private string HunterSummary() => sawHunter
		? $"{hunterDraws.Looses} draws loosed, {hunterDraws.IntoBarrows} of them cut short by a barrow; reached phase {hunterDraws.Phase}"
		: null;

	private void TrackHayward()
	{
		foreach (BossEnemy body in bodies)
		{
			if (body is UnstrungHunterBoss hunter)
			{
				sawHunter = true;
				hunterDraws = (hunter.Looses, hunter.LoosesIntoBarrows, hunter.BossPhase);
			}
			if (body is not HaywardBoss hayward)
				continue;
			sawHayward = true;
			haywardBeats = (hayward.SwingsBegun, hayward.ChargesBegun, hayward.RingSwingsBegun,
				hayward.SpentWindowsEntered, hayward.HitsTurned);
		}
	}

	private string HaywardSummary()
	{
		if (!sawHayward)
			return null;
		return $"{haywardBeats.Swings} swings, {haywardBeats.Charges} charges chained, "
			+ $"{haywardBeats.Spent} spent windows, {haywardBeats.Rings} ring swings; "
			+ $"{haywardBeats.Turned} hits turned by the hurdle";
	}

	private string ChoirSummary()
	{
		if (choirVoices.Count == 0)
			return null;
		int dips = choirVoices.Values.Sum(v => v.Dips);
		float held = choirVoices.Values.Sum(v => v.Held);
		string times = choirDownTimes.Count > 0 ? string.Join(", ", choirDownTimes.Select(t => t.ToString("0.0"))) + " s" : "none";
		return $"{choirFalls} falls, {choirDownTimes.Count} sung back up (down for {times}); {dips} dips;"
			+ $" song paused {held:0.0} s by crowd control on its singer";
	}

	// When the last body stopped being a fight, and how long its fall may take before the probe calls
	// it stuck: the longest fall is about three seconds, the Choir's last voice about two and a half.
	private float fellAt = -1f;
	private const float CorpseGiveUpSeconds = 8f;

	// Every boss node still in the tree, the dying included.
	private static int CountBossNodes(Node root)
	{
		if (root == null)
			return 0;
		int count = root is BossEnemy && IsInstanceValid(root) ? 1 : 0;
		foreach (Node child in root.GetChildren())
			count += CountBossNodes(child);
		return count;
	}

	private static void CollectBosses(Node root, List<BossEnemy> into)
	{
		if (root == null)
			return;
		if (root is BossEnemy hit && IsInstanceValid(hit) && !hit.IsDying)
			into.Add(hit);
		foreach (Node child in root.GetChildren())
			CollectBosses(child, into);
	}

	// --drain-interval shortens it to hit the boss as often as a full build does. Every bite plays
	// the hit punch, so a bite faster than the punch eases back (0.1 s) is what tests its rest size.
	private float DrainInterval = 0.25f;

	// Kite, exactly as the balance harness does. The bot is not the subject here - it exists so
	// the boss has something to chase and something to aim at.
	private void DriveMovement()
	{
		if (PursueSeneschal())
			return;
		if (DuelDarkWizard())
			return;
		Vector2 away = Vector2.Zero;
		int counted = 0;

		foreach (Node n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Node2D e || !IsInstanceValid(n))
				continue;

			Vector2 offset = player.GlobalPosition - e.GlobalPosition;
			float d = offset.Length();
			if (d > 260f || d <= 0.01f)
				continue;

			away += offset / d * (1f - d / 260f);
			counted++;
		}

		Vector2 toCentre = -player.GlobalPosition;
		Vector2 desired = counted > 0
			? away.Normalized() + toCentre.Normalized() * 0.45f
			: toCentre.Normalized();
		if (approachTo is Vector2 hold)
		{
			// --approach: the place, not the middle of the arena, and the place first - a wizard walking
			// into the fire leans away from what is close but does not give the ground up to it.
			Vector2 toPlace = hold - player.GlobalPosition;
			desired = (toPlace.Length() > 60f ? toPlace.Normalized() : Vector2.Zero)
				+ (counted > 0 ? away.Normalized() * 0.35f : Vector2.Zero);
		}

		if (desired.LengthSquared() > 0.0001f)
			desired = desired.Normalized();

		Press("move_right", "move_left", desired.X);
		Press("move_down", "move_up", desired.Y);
	}

	// THE DARK WIZARD (.ai/endgame-castle.md 6.4): the fight's verb is "take it back", which a kiting bot
	// never meets - it outruns every hand. So while he stands the bot is made to answer it both ways, and the
	// probe proves the seal underneath each answer:
	//   - a hand in flight: the bot stands and lets it land (every taking, so the seal is exercised);
	//   - an orb in flight: on odd seals it walks to the orb and takes the spell back; on even seals it walks
	//     away and lets the orb reach him, so the twenty-second seal runs out on its own;
	//   - and for every sealed spell, its casts through the firing loop are counted: none while sealed, and
	//     at least one after the seal lifts, which is the whole primitive (Player.Seal.cs).
	// Kept whole throughout, as the Seneschal's bot is: the question is the mechanic, not the bot's health.
	private int darkSealsSeen;
	private readonly Dictionary<SpellData, (int CastsAtSeal, float SealedAt, bool TakeBack)> darkSealed = new();
	private readonly Dictionary<SpellData, (int CastsAtRelease, float ReleasedAt)> darkReleased = new();
	private int darkCastWhileSealed, darkCastAgain, darkNeverCastAgain, darkPeakSealed;
	private bool darkVeilSeenInPhase1, darkVeilSeenInPhase0, darkVeilGoneAfterFall, darkSealsGoneAfterFall;
	private float darkMultiplierPhase0 = -1f, darkMultiplierPhase1 = -1f;
	private ThroneDarkWizardBoss darkWizard;
	private int darkHands, darkLanded, darkTakenBack, darkReachedHim, darkLines, darkEchoes;
	private readonly HashSet<string> darkEchoed = new();
	private float darkFellAt = -1f;
	private bool endingEntered, endingChecked;
	private float endingEnteredAt;

	private bool DuelDarkWizard()
	{
		// Found once and kept, valid or not: the summary and the ending check read it after he is gone.
		if (darkWizard == null)
			darkWizard = bodies.OfType<ThroneDarkWizardBoss>().FirstOrDefault();
		if (darkWizard == null || !IsInstanceValid(darkWizard) || darkWizard.IsDying || darkWizard.IsEntering || player is not Player wizard)
			return false;
		if (!wizard.IsDead)
			wizard.CurrentHP = wizard.MaxHP;
		TrackSeals(wizard);

		Vector2? toward = null;
		bool stand = false;
		SealOrb orb = FindNode<SealOrb>(game);
		if (orb != null && !orb.Spent && darkSealed.TryGetValue(orb.Spell, out var sealedNow))
		{
			Vector2 to = orb.GlobalPosition - wizard.GlobalPosition;
			toward = sealedNow.TakeBack ? to : -to;
		}
		else if (FindNode<DarkHand>(game) is DarkHand hand && !hand.Spent)
			stand = true;
		else if (darkWizard.TakingWindingUp)
			stand = true;
		if (!stand && toward == null)
			return false;
		Vector2 d = toward is Vector2 v && v.LengthSquared() > 1f ? v.Normalized() : Vector2.Zero;
		Press("move_right", "move_left", stand ? 0f : d.X);
		Press("move_down", "move_up", stand ? 0f : d.Y);
		return true;
	}

	private void TrackSeals(Player wizard)
	{
		darkPeakSealed = Math.Max(darkPeakSealed, wizard.SealedCount);
		darkHands = darkWizard.HandsThrown;
		darkLanded = darkWizard.HandsLanded;
		darkTakenBack = darkWizard.SealsTakenBack;
		darkReachedHim = darkWizard.SealsReachedHim;
		darkLines = darkWizard.LinesCast;
		darkEchoes = darkWizard.EchoesCast;
		foreach (string id in darkWizard.EchoLog)
			darkEchoed.Add(id);
		if (darkWizard.BossPhase == 0 && darkMultiplierPhase0 < 0f)
			darkMultiplierPhase0 = darkWizard.IncomingSpellMultiplier(DarknessProbeSpell());
		if (darkWizard.BossPhase >= 1 && darkMultiplierPhase1 < 0f)
			darkMultiplierPhase1 = darkWizard.IncomingSpellMultiplier(DarknessProbeSpell());
		darkVeilSeenInPhase0 |= darkWizard.BossPhase == 0 && darkWizard.DarkIsOn;
		darkVeilSeenInPhase1 |= darkWizard.BossPhase >= 1 && darkWizard.DarkIsOn;
		foreach (SpellData spell in wizard.GetEquippedSpells())
		{
			if (spell == null)
				continue;
			bool sealedNow = wizard.IsSealed(spell);
			if (sealedNow && !darkSealed.ContainsKey(spell))
			{
				darkSealsSeen++;
				darkSealed[spell] = (wizard.CastsOf(spell.Id), elapsed, darkSealsSeen % 2 == 1);
				darkReleased.Remove(spell);
			}
			else if (sealedNow && wizard.CastsOf(spell.Id) > darkSealed[spell].CastsAtSeal)
			{
				darkCastWhileSealed++;
				darkSealed[spell] = (wizard.CastsOf(spell.Id), darkSealed[spell].SealedAt, darkSealed[spell].TakeBack);
			}
			else if (!sealedNow && darkSealed.TryGetValue(spell, out var was))
			{
				notes.Add($"seal on {spell.Id} lifted after {elapsed - was.SealedAt:0.0}s ({(was.TakeBack ? "the bot went for the orb" : "the bot let it go home")})");
				darkSealed.Remove(spell);
				darkReleased[spell] = (wizard.CastsOf(spell.Id), elapsed);
			}
		}
		foreach (SpellData spell in darkReleased.Keys.ToList())
		{
			var (casts, at) = darkReleased[spell];
			if (wizard.CastsOf(spell.Id) > casts)
			{
				darkCastAgain++;
				darkReleased.Remove(spell);
			}
			else if (elapsed - at > 30f)
			{
				darkNeverCastAgain++;
				darkReleased.Remove(spell);
			}
		}
	}

	private static SpellData darknessProbe;

	// Hollow Star since 2026-11-07: the probe only needs a Darkness-tagged spell to read the boss's
	// multiplier with, and Shadow Bolt is a Conjunction now (ComboCatalog) rather than a pool spell.
	private static SpellData DarknessProbeSpell() =>
		darknessProbe ??= ResourceLoader.Load<SpellData>("res://SpellData_HollowStar.tres");

	// After he falls: every seal let go, the dark lifted; then the way home, and the ending over the results.
	private void TrackDarkWizardFall()
	{
		if (darkWizard == null || player is not Player wizard)
			return;
		if (darkFellAt < 0f)
		{
			darkFellAt = elapsed;
			darkSealsGoneAfterFall = wizard.SealedCount == 0;
		}
		if (!darkVeilGoneAfterFall)
			darkVeilGoneAfterFall = FindNode<DarkVeil>(game) == null;
	}

	private string DarkWizardSummary()
	{
		if (darkWizard == null)
			return null;
		return $"hands {darkHands} thrown, {darkLanded} landed; seals {darkSealsSeen} "
			+ $"(peak {darkPeakSealed} at once), {darkTakenBack} taken back, {darkReachedHim} reached him; "
			+ $"casts while sealed {darkCastWhileSealed}, cast again after release {darkCastAgain}, never again {darkNeverCastAgain}; "
			+ $"dark in phase 0 / 1+: {darkVeilSeenInPhase0} / {darkVeilSeenInPhase1}; darkness x{darkMultiplierPhase0:0.00} then x{darkMultiplierPhase1:0.00}; "
			+ $"lines {darkLines}, echoes {darkEchoes} ({(darkEchoed.Count > 0 ? string.Join(", ", darkEchoed) : "none")}); "
			+ $"after the fall: seals all let go {darkSealsGoneAfterFall}, dark lifted {darkVeilGoneAfterFall}";
	}

	// THE SENESCHAL (.ai/endgame-castle.md 5.6): he leaves at each threshold and the fight resumes only
	// when the player reaches the room he went to, so a bot that kites would never find him and the
	// probe would report a fight it never finished. While he is away the bot walks to him through the
	// Court's halls - a breadth-first search over the building's own grid (CourtLayout), cell to cell,
	// from where he will be - and the probe notes each flight and how long the finding took.
	private int seneschalFlightsSeen;
	private float seneschalLeftAt = -1f;
	private int[] courtSteps;
	private Vector2I courtGoal = new(-1, -1);

	private bool PursueSeneschal()
	{
		if (boss is not CourtSeneschalBoss seneschal || !IsInstanceValid(seneschal) || seneschal.IsDying)
			return false;
		// Kept whole for this fight: the question is whether he can be followed and finished, and a
		// probe wizard at its level dies to the household it walks through on the way, which says
		// nothing about either.
		if (player is Player kept && !kept.IsDead)
			kept.CurrentHP = kept.MaxHP;
		if (seneschal.FlightsTaken > seneschalFlightsSeen)
		{
			seneschalFlightsSeen = seneschal.FlightsTaken;
			seneschalLeftAt = elapsed;
			notes.Add($"flight {seneschalFlightsSeen} at {elapsed:0.0}s, {seneschal.BossHealthFraction:P0} health, to "
				+ $"{(seneschal.RefugeName.Length > 0 ? seneschal.RefugeName : "the open")}, {player.GlobalPosition.DistanceTo(seneschal.PursuitTarget):0} px from the bot; "
				+ $"household so far {seneschal.HouseholdCalled}");
		}
		if (!seneschal.AwaitingPursuit)
		{
			if (seneschalLeftAt >= 0f)
			{
				notes.Add($"found him in {(seneschal.RefugeName.Length > 0 ? seneschal.RefugeName : "the open")} {elapsed - seneschalLeftAt:0.0}s after he left; "
					+ $"household {seneschal.HouseholdCalled}, ranks {seneschal.RanksCalled} ({seneschal.RankHits} reached the bot)");
				seneschalLeftAt = -1f;
			}
			return false;
		}

		Vector2 feet = player is Player wizard ? wizard.FootPosition : player.GlobalPosition;
		Vector2 to = seneschal.PursuitTarget - feet;
		CourtLayout layout = (Node2DGame.Current?.Rules as CourtRules)?.Layout;
		if (layout != null)
		{
			Vector2I goal = layout.WorldToCell(seneschal.PursuitTarget + new Vector2(0f, 80f));
			if (goal != courtGoal || courtSteps == null)
				courtSteps = CourtStepsTo(layout, courtGoal = goal);
			Vector2I at = layout.WorldToCell(feet);
			// Three cells down the gradient, so the feet round a door's jamb rather than catch on it.
			Vector2I next = at;
			for (int k = 0; k < 3; k++)
				next = DownHill(layout, next);
			if (next != at)
				to = layout.CellToWorld(new Vector2(next.X + 0.5f, next.Y + 0.5f)) - feet;
		}
		if (to.LengthSquared() > 1f)
			to = to.Normalized();
		// The grid knows the walls, not what stands in a hall (a blocker, a prop): not having moved for
		// half a second, step sideways for a moment, alternating sides, as the balance bot does.
		if (elapsed < sidestepUntil)
			to = (new Vector2(-to.Y, to.X) * sidestepSide + to * 0.3f).Normalized();
		else if (elapsed - stuckCheckAt >= 0.5f)
		{
			if (feet.DistanceTo(stuckCheckFrom) < 8f)
			{
				sidestepSide = -sidestepSide;
				sidestepUntil = elapsed + 0.7f;
			}
			stuckCheckAt = elapsed;
			stuckCheckFrom = feet;
		}
		Press("move_right", "move_left", to.X);
		Press("move_down", "move_up", to.Y);
		return true;
	}

	private float sidestepUntil, stuckCheckAt;
	private float sidestepSide = 1f;
	private Vector2 stuckCheckFrom;

	private static readonly Vector2I[] Neighbours = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

	private int[] CourtStepsTo(CourtLayout layout, Vector2I goal)
	{
		int w = layout.Grid.Width, h = layout.Grid.Height;
		var steps = new int[w * h];
		Array.Fill(steps, int.MaxValue);
		if (!layout.Grid.Contains(goal))
			return steps;
		var queue = new Queue<Vector2I>();
		steps[goal.X + goal.Y * w] = 0;
		queue.Enqueue(goal);
		while (queue.Count > 0)
		{
			Vector2I c = queue.Dequeue();
			int here = steps[c.X + c.Y * w];
			foreach (Vector2I d in Neighbours)
			{
				Vector2I n = c + d;
				// A cell's width clear of every wall, so the route keeps the feet off the jambs: a
				// shortest path hugs each corner it turns, and the feet collider snags on the first.
				if (!layout.Grid.Contains(n) || !Clear(layout, n) || steps[n.X + n.Y * w] != int.MaxValue)
					continue;
				steps[n.X + n.Y * w] = here + 1;
				queue.Enqueue(n);
			}
		}
		// Then the floor along the walls, each cell one more than a neighbour already reached, so a bot
		// standing against a wall (where the run starts) still has a way down into the clear.
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
				if (steps[x + y * w] != int.MaxValue)
					queue.Enqueue(new Vector2I(x, y));
		while (queue.Count > 0)
		{
			Vector2I c = queue.Dequeue();
			int here = steps[c.X + c.Y * w];
			foreach (Vector2I d in Neighbours)
			{
				Vector2I n = c + d;
				if (!layout.Grid.Contains(n) || !layout.Grid.IsWalkable(n) || steps[n.X + n.Y * w] != int.MaxValue)
					continue;
				steps[n.X + n.Y * w] = here + 1;
				queue.Enqueue(n);
			}
		}
		return steps;
	}

	private static bool Clear(CourtLayout layout, Vector2I cell)
	{
		for (int dy = -1; dy <= 1; dy++)
			for (int dx = -1; dx <= 1; dx++)
				if (!layout.Grid.IsWalkable(cell + new Vector2I(dx, dy)))
					return false;
		return true;
	}

	private Vector2I DownHill(CourtLayout layout, Vector2I at)
	{
		int w = layout.Grid.Width;
		if (courtSteps == null || !layout.Grid.Contains(at))
			return at;
		Vector2I best = at;
		int bestSteps = courtSteps[at.X + at.Y * w];
		foreach (Vector2I d in Neighbours)
		{
			Vector2I n = at + d;
			if (layout.Grid.Contains(n) && courtSteps[n.X + n.Y * w] < bestSteps)
			{
				best = n;
				bestSteps = courtSteps[n.X + n.Y * w];
			}
		}
		return best;
	}

	// --approach, on the first frame the wizard exists: set him down short of the boss point, on the side
	// he started from, and give him the point to walk to.
	private void PlaceForApproach()
	{
		BossDefinition definition = BossCatalog.ForStageIndex(StageIndex);
		if (Node2DGame.Current?.Rules?.BossSpawnPoint(definition) is not Vector2 point)
		{
			notes.Add("--approach: the chapter names no boss point (hook 4), so the wizard kites as usual");
			return;
		}
		Vector2 back = player.GlobalPosition - point;
		back = back.LengthSquared() > 1f ? back.Normalized() : Vector2.Down;
		player.GlobalPosition = point + back * ApproachStartPx;
		approachTo = point;
		notes.Add($"--approach: wizard set down {ApproachStartPx:0} px short of the boss point {point.Round()} and walked to it");
	}

	private static void Press(string positive, string negative, float axis)
	{
		if (axis > 0.02f)
		{
			Input.ActionRelease(negative);
			Input.ActionPress(positive, Math.Min(1f, axis));
		}
		else if (axis < -0.02f)
		{
			Input.ActionRelease(positive);
			Input.ActionPress(negative, Math.Min(1f, -axis));
		}
		else
		{
			Input.ActionRelease(positive);
			Input.ActionRelease(negative);
		}
	}

	private void AutoPickLevelUp()
	{
		LevelUpMenu menu = FindNode<LevelUpMenu>(game);
		if (menu == null || !menu.Visible || menu.IsQueuedForDeletion())
			return;

		IReadOnlyList<LevelUpOption> options = menu.GetOfferedOptions();
		if (options == null || options.Count == 0)
			return;

		LevelUpOption pick = options.FirstOrDefault(o => o.IsNewUnlock)
			?? options.OrderBy(o => o.NextLevel).First();
		menu.EmitSignal(LevelUpMenu.SignalName.WeaponSelected, pick.AutoChoice());
	}

	// --- the cinematic's beats (.ai/cutscenes.md, "The cinematic rules") ------------------------------------
	// The first arrival, timed from the frame the field is first held to the frame the camera has let go, by
	// what can be seen rather than by the run's own bookkeeping, so the same lines read the same before and after a
	// change to it: when the pan began and came to rest on the bodies (rule 3: a hold before it, and a hold on what
	// it found), when the last body landed, when the camera started back and was home on the wizard, and when the
	// field was let go (rule 4: a beat before play) - and the fastest the camera moved and zoomed each way (rule 5).
	private const float BeatMovingPxPerSec = 4f;
	private const float BeatRestPxPerSec = 12f;
	private bool beatsDone;
	private float beatHeldAt = -1f, beatPanAt = -1f, beatPanRestAt = -1f, beatLandedAt = -1f, beatReturnAt = -1f,
		beatHomeAt = -1f, beatReleasedAt = -1f;
	private float beatOutPeak, beatBackPeak, beatOutZoomRate, beatBackZoomRate, beatLastZoom = 1f;
	private Vector2 beatLastCam;
	private bool beatCamSeen;

	private void TrackEntranceBeats(float delta)
	{
		if (beatsDone || bodies.Count == 0 || delta <= 0.0001f || Node2DGame.Current is not Node2DGame run)
			return;
		float now = elapsed;
		if (beatHeldAt < 0f)
		{
			if (!run.BossEntranceHeld)
				return;
			beatHeldAt = now;
		}
		Camera2D cam = player?.GetNodeOrNull<Camera2D>("Camera2D");
		if (cam == null)
			return;
		Vector2 at = cam.Position;
		float zoom = run.CinematicZoomFactor;
		float speed = beatCamSeen ? at.DistanceTo(beatLastCam) / delta : 0f;
		float zoomRate = beatCamSeen ? Mathf.Abs(zoom - beatLastZoom) / delta : 0f;
		beatLastCam = at;
		beatLastZoom = zoom;
		beatCamSeen = true;

		if (beatLandedAt < 0f && bodies.All(b => b.EntranceProgress >= 1f))
			beatLandedAt = now;
		if (beatReleasedAt < 0f && !run.BossEntranceHeld)
			beatReleasedAt = now;

		bool goingBack = beatPanAt >= 0f && beatLandedAt >= 0f;
		if (beatPanAt < 0f && speed > BeatMovingPxPerSec)
			beatPanAt = now;
		else if (beatPanAt >= 0f && beatReturnAt < 0f)
		{
			// Outbound: the last time it came to rest before it started back.
			if (speed < BeatRestPxPerSec)
			{
				if (beatPanRestAt < 0f)
					beatPanRestAt = now;
			}
			else if (!(goingBack && at.Length() < beatLastCamLength))
				beatPanRestAt = -1f;
			// Back: once the bodies have landed, the first frame it moves toward the wizard.
			if (goingBack && speed > BeatMovingPxPerSec && at.Length() < beatLastCamLength)
				beatReturnAt = now;
		}
		if (beatReturnAt < 0f)
		{
			beatOutPeak = Mathf.Max(beatOutPeak, speed);
			beatOutZoomRate = Mathf.Max(beatOutZoomRate, zoomRate);
		}
		else
		{
			beatBackPeak = Mathf.Max(beatBackPeak, speed);
			beatBackZoomRate = Mathf.Max(beatBackZoomRate, zoomRate);
			// Home: back on the wizard (or the chapter's lean, which no probed chapter has) and at its own zoom,
			// as near as the eye can tell.
			if (beatHomeAt < 0f && at.Length() < 8f && speed < 25f && Mathf.Abs(zoom - 1f) < 0.01f)
				beatHomeAt = now;
		}
		beatLastCamLength = at.Length();

		bool settled = beatReleasedAt >= 0f && at.Length() < 0.5f && Mathf.Abs(zoom - 1f) < 0.002f;
		if (settled || now - beatHeldAt > 14f)
		{
			beatsDone = true;
			string T(float t) => t < 0f ? "never" : $"{t - beatHeldAt:0.00}s";
			notes.Add($"beats: held 0.00s, pan {T(beatPanAt)}, at rest on the bodies {T(beatPanRestAt)}, landed {T(beatLandedAt)}, "
				+ $"back {T(beatReturnAt)}, home {T(beatHomeAt)}, field let go {T(beatReleasedAt)}");
			float onBodies = beatPanRestAt >= 0f && beatReturnAt >= 0f ? beatReturnAt - beatPanRestAt : -1f;
			float beforePlay = beatHomeAt >= 0f && beatReleasedAt >= 0f ? beatReleasedAt - beatHomeAt : float.NaN;
			notes.Add($"holds: before the pan {(beatPanAt >= 0f ? beatPanAt - beatHeldAt : -1f):0.00}s, on the bodies after it {onBodies:0.00}s, "
				+ $"before play {(float.IsNaN(beforePlay) ? "n/a" : $"{beforePlay:0.00}s")} (negative: let go before the camera was home)");
			notes.Add($"speeds: out {beatOutPeak:0} px/s and x{beatOutZoomRate:0.00}/s zoom; back {beatBackPeak:0} px/s and x{beatBackZoomRate:0.00}/s zoom");
		}
	}

	private float beatLastCamLength;

	private void Finish(string why)
	{
		finished = true;
		foreach (string a in MoveActions)
			Input.ActionRelease(a);

		BossDefinition definition = BossCatalog.ForStageIndex(StageIndex);
		GD.Print("");
		GD.Print($"BOSSPROBE RESULT  stage {StageIndex}  {definition?.DisplayName ?? "?"}");
		GD.Print($"  outcome        : {why} at {elapsed:0.0}s");
		GD.Print($"  boss arrived   : {(sawBoss ? $"yes, at {bossArrivedAt:0.0}s" : "NO - the fight never started")}");
		GD.Print($"  bodies left    : {bodies.Count}");
		GD.Print($"  phases entered : {peakPhase} of {phaseCountSeen}");
		GD.Print($"  peak size      : x{peakScaleGrowth:0.00} of its arrival size");
		GD.Print($"  drain ticks    : {drainTicks}");
		GD.Print($"  went untargetable / came back : {sawUntargetable} / {sawRetargetable}");
		GD.Print($"  in wall/water  : {bodyFramesInGround} of {bodyFrames} body-frames out of the ground");
		GD.Print($"  held           : {heldWindows} of {heldEligible} body-windows ({HeldWindow:0} s) asked to walk, covered under 0.15 of it and touched something"
			+ $" (ground {heldOnGround}, a prop {heldOnProp}, a body {heldOnBody}); {heldTouchingNothing} more touched nothing");
		if (ChoirSummary() is string choir)
			GD.Print($"  the choir      : {choir}");
		if (HaywardSummary() is string combo)
			GD.Print($"  the combo      : {combo}");
		if (HunterSummary() is string draws)
			GD.Print($"  the draws      : {draws}");
		if (DarkWizardSummary() is string dark)
			GD.Print($"  the dark wizard: {dark}");
		foreach (string note in notes)
			GD.Print($"  note           : {note}");
		foreach (var (id, phases) in processionPhases)
			GD.Print($"  procession     : {id} entered {phases.Peak} of {phases.Of} phases; "
				+ $"{processionInGround.GetValueOrDefault(id)} frames in a wall");
		if (procession == true)
			GD.Print($"  way home open  : {Node2DGame.Current?.VictoryPortalOpen ?? false}");

		// The one hard failure. Everything else is reported and judged by a person; a boss that
		// never arrives is a stage that cannot be completed, and that is worth an exit code.
		if (!sawBoss)
			GD.PushError($"BOSSPROBE stage {StageIndex}: the boss never spawned.");

		player = null;
		boss = null;
		game = null;
		GetTree().Quit();
	}

	private static T FindNode<T>(Node root) where T : Node
	{
		if (root == null)
			return null;
		if (root is T hit)
			return hit;
		foreach (Node child in root.GetChildren())
		{
			T found = FindNode<T>(child);
			if (found != null)
				return found;
		}
		return null;
	}
}
