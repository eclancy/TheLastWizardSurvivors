using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// Drives one chapter's exploration headless, from the real game scene, and checks what the side
// events promise now that they are placed rather than timed (.ai/side-events.md, "Finding the event").
//
// Run, one stage per process (a run's statics are reset when its director is made):
//   "$GODOT_BIN" --headless --path . scenes/_ExploreProbe.tscn -- --stage=3 [--seed=4242] [--no-complete]
//
// What it checks, as PASS / FAIL lines:
//   1. placed at start: the event's sites and the wayfinder exist before anyone moves, inside their
//      bands, on open ground, the wayfinder at least 75 degrees off the event's bearing, and dormant;
//   2. deterministic: laying the same chapter out again from the same seed gives the same places;
//   3. hidden: nothing is marked until it is seen or revealed;
//   4. the wayfinder reveals: walking onto it marks every site of the event;
//   5. it stays put across unload and reload: the player is sent far enough away that the site's
//      chunks are forgotten, then back, and the decor round the site is the same both visits and
//      never inside its footprint;
//   6. it starts on arrival, not before: dormant at 900 px, begun once the player is on it;
//   7. (unless --no-complete) it can be finished: the probe stands on whatever the event wants and
//      bleeds its elites, the way _BossProbe bleeds a boss, and reports whether it was won.
//      --slow-bleed bleeds them at 4% of their health a second instead of a sixth a frame, so a
//      fight with a phase in it lives to reach it: on the Swamp the lantern-tender rekindles a
//      lantern at half health, the objective moves to that lantern, the probe stands in it until it
//      is out, and only then comes back for the tender (LanternTenderEnemy logs both beats).
//      The great anvil (--stage=7) is baited, not bled: the probe stands on a stake still holding its
//      chain, and the moment a hammer near it starts its wind-up it steps out of the ring on the far side
//      of the stake, so the slam comes down on the iron and not on him; then the next stake.
//      --kill-hammers=N kills the first N hammers that come near him before they can strike, to show the
//      bellows wake their replacements and the forge cannot be locked by a strong loadout.
//      The Sunken Cave's nine mouths are LISTENED to: the probe waits in front of the cliff for a call
//      and stands in the mouth whose voice came first (NineMouthsEvent.LastCallFirst - what a player
//      hears and sees), never the answer itself. --wrong-mouth first stands in a mouth that was NOT first,
//      and checks that it answers with a flight and that hers moves.
//      The Forest's walking hut is HUNTED, at the player's own walking speed, never teleported: the probe walks
//      at it and holds just inside its startle radius, so it bolts again and again and the bolts really gain on
//      him. --lose-hut, once the bar is two fifths full, walks straight away past its lose radius and waits
//      there, and checks the bar held and the hut calmed and stood; then walks back to it and finishes.
//      The Last Bridge's barge (--stage=11) is KEPT PACE WITH: placed by the bridge's band, not the explore
//      bands, so its placement is checked against the bridge's rules (moored under a span, on the water beside
//      the deck, the wayfinder on the deck ahead of the start); then the probe comes level with it and stays
//      level - a little behind it, on its side of the deck - for the whole drift, and checks it stayed ahead of
//      the fall. --fall-behind=S stops following for S seconds a third of the way in: under the barge's grace
//      (6 s) it must recover and win in one drift, over it the drift must fail, drift back, and be won from the
//      start again. --arrive-late waits for the fall to pass the barge's mooring before coming, and checks it
//      slipped its mooring and waits ahead of the fall, its arrow with it. Pair it with --bridge-front=-6000
//      (the front started 6,000 px ahead of the start) to keep the wait short.
//      The Swamp's gibbet cage is LED ROUND THE ROT (.ai/unlock-puzzles.md 3.4): the probe holds the winch, and each time
//      Paracelsus steps out it plans its own way from where it stands to the ferry landing through the gaps in the rot
//      (RotPoolLayout.FindRoute over the pools it can see and the ground - what a player who has seen the pools does) and
//      walks it at the player's own walking speed, waiting whenever he falls more than a few strides behind, so he really
//      walks the whole way on the player's steps. The swarm is cleared only every few seconds, so the Mired the bog raises
//      in the gaps and the trickle are round them both; the probe counts the frames a body was within a touch of him, and
//      checks that nothing took anything from him but the rot. --straight, on his first walk only, walks straight at the
//      landing round water as the old escort did, through whatever rot is on the line, and checks the rot bit him (the
//      wrong approach); then leads him round on the next. --let-him-fall, once he is out the first time, leads him into
//      the largest pool near him and walks a small ring in it until the rot has all of him, and checks he falls, sinks, is
//      in the cage again and is lowered and led out again from the cage - and then leads him round to the landing.
//      The Court's seer (--stage=12) is SOUGHT, at the player's own walking speed, never teleported: once a room is
//      found empty the probe stands and waits for the voice, and when it reads it walks the shortest way through the
//      halls (the building's own grid, CourtLayout) to where the reading came from - what a player who knows the
//      house does - and logs, per leg, how long the voice took, how far the walls made the walk against the straight
//      line the sound promised, and how long it took. The Court's events are placed by its rooms, not the bands, so
//      the band check is the rooms' there, and the probe's far points are kept on the building's floor.
//      The Sands' dry tomb is KINDLED, walked at the player's own walking speed from beacon to beacon, never teleported:
//      the probe goes to the nearest cold one, stands in its reach until it catches, and on to the next, so the 25 s
//      clock is run against a real walk. --let-beacon-die, once the first has caught, stands off at the tomb (outside
//      every reach) until the wind puts it out, checks it went cold, and then lights all three.
//      The Court's dotted door (its second event) is TRACED at the player's own walking speed, never teleported along
//      it: from where it arrives the probe walks to dot 1 and then along the line a stride ahead of the ink, feet first
//      (Player.FootPosition, what the trace reads), and logs how long it took and how much of the household came in at
//      the Gallery's doors. --step-off, once two fifths of it is inked, walks 130 px off the line (into the door's middle) and waits there 3 s,
//      and checks the ink held where it was and nothing reset; then walks back to where the ink ends and finishes.
// TWO EVENTS IN A CHAPTER (P2, .ai/chapter-wizards.md 6.3). On a chapter whose list holds more than one - or
// with --second-event-test[=STAGE], which adds a stand-in second event on this tool run only
// (SecondEventStandIn) - every placement check runs for every event, each pair of main sites must be at least
// SideEventDirector.MinSiteSeparationDegrees apart, and the order is: the hints (forced, four of them) must
// alternate the events; the first event is won WITHOUT the wayfinder; two more hints must all be the second's
// (a won event stops hinting); then the wayfinder is taken and must mark what is left; then the second event is
// visited, found and won. --wayfinder-first takes the wayfinder before anything instead, as a one-event chapter
// does, and checks it marks every site of both events and that no hint plays after it; and once the first has
// begun it starts the second too, where it stands, so two run at once - the line under the timer must pass to
// the one begun last, back to the first for its win line, and to the second again after.
// --fresh-save runs on a brand-new save and --all-unlocked on one that owns every spell, boon and wizard,
// both built in memory (the balance harness's), so a won event's first-win path (the wizard freed) and its
// chest path are each driven on purpose rather than by whatever the machine's save holds.
// It teleports rather than walks: the walking is _BalanceHarness --explore's job. Saving is off.
// --fresh-save plays it on a brand-new save in memory (the site pays its unlock: a wizard is freed); --all-unlocked
// on one that owns everything (the site is exhausted, so the win pays a chest). Without either, the machine's save.
// WHAT IT CANNOT TELL YOU: whether anything is visible, readable or fun. It renders nothing.
public partial class _ExploreProbe : Node
{
	[Export] public int StageIndex { get; set; } = 3;
	[Export] public long Seed { get; set; } = 4242;
	[Export] public float TimeScale { get; set; } = 4f;
	[Export] public bool Complete { get; set; } = true;
	// --log-props: name every furnished thing the visit comparison counts.
	private bool logProps;
	// --fresh-save / --all-unlocked: the save the run plays on, in memory.
	private bool freshSave;
	private bool allUnlocked;
	// --kill-hammers=N: the great anvil's first N hammers killed before they strike.
	private int killHammers;
	private int hammersKilled;
	// --slow-bleed: elites bled at SlowBleedPerSecond of their health, carried between frames.
	private bool slowBleed;
	// --wrong-mouth: at the nine mouths, stand in a wrong one first and check it answers.
	private bool wrongMouth;
	// --wayfinder-first: with two events, take the wayfinder first (see the header).
	private bool wayfinderFirst;
	// --lose-hut: at the walking hut, walk away from it part-way through and come back (see the header).
	private bool loseHut;
	// --let-beacon-die: at the dry tomb, let the first beacon burn out before lighting the three (see the header).
	private bool letBeaconDie;
	private int beaconPhase;
	private float beaconWentOutAt = -1f;
	private bool beaconWentCold;
	private int lostPhase;
	// --let-him-fall: at the gibbet's cage, let the rot have him once (see the header). --straight: walk his first walk
	// straight at the landing, through the rot.
	private bool letHimFall;
	private bool straightThrough;
	private bool backInCageAfterFall;
	private float escortClearClock;
	private int escortWaits;
	// The way the probe planned through the rot for the walk under way, and the leg it is on; the pool it led him into.
	private Vector2[] escortRoute;
	private int escortLeg;
	private int fallPool = -1;
	private float fallRing;
	// What the straight walk cost him, and the frames a body was within a touch of him while he walked (the old bite's
	// 46 px), against what he lost to anything but the rot.
	private int straightBites = -1;
	private bool straightReachedLanding;
	private int closeFrames;
	private int escortReleasesSeen;
	private float lostClock;
	private float progressOutside = -1f;
	private float progressAfterWait = -1f;
	// The Last Bridge's barge: --fall-behind=S and --arrive-late (see the header).
	private float fallBehind;
	private bool arriveLate;
	private float bargeHeld = -1f;
	private float bargeWait;
	private float bargeLeadMin = float.MaxValue;
	private bool bargeOutSeen;
	private float bargeOutMax;
	private Vector2 fellBackTo;
	// --tithe-level=N: the player is put at level N as he arrives at the Tithe-Reeve's scale, so the price is a real
	// level's (the probe arrives in the first minute, at level 1, where a level is 10 XP). --tithe-walk-off: once the
	// pan is two fifths paid he walks 1,500 px off, waits, and comes back - what is paid must stay paid.
	private int titheLevel;
	private bool titheWalkOff;
	private int tithePhase;
	private float titheClock;
	private float titheBefore = -1f;
	private float titheAfter = -1f;
	private int titheXpAtArrival;
	private float titheArrivedAt = -1f;
	private float titheLogNext;
	private ulong titheLogReal;
	// Which of the chapter's events is being visited, found and won; and which of them were won.
	private int cur;
	private readonly List<bool> wonEvents = new();
	// The hints phase: forced hints wanted, and its game-time clock.
	private int hintsWanted;
	private float hintClock;
	// --wayfinder-first with two events: the second was begun while the first ran.
	private bool secondBegunEarly;
	private int mouthWrongFirst = -1;
	private int mouthTrueBefore = -1;
	private int mouthTrueAfter = -1;
	private float bleedCarry;
	private Enemy bleedTarget;
	private int bleedFloor;
	private const float SlowBleedPerSecond = 0.04f;

	private Node2DGame game;
	private Player player;
	private SideEventDirector director;
	private int step;
	private int wait;
	private float stepClock;
	private int passes, fails;
	private readonly List<string> notes = new();
	private List<Vector2> visitA;
	private Vector2 lookout;
	// Where each event stood at the start: it must never move.
	private List<Vector2> siteAtStart = new();
	private LevelUpMenu levelUpMenu;
	private int settleFrames;
	private float completeClock;
	private int sealFrames;
	private int sealAlignedAtStart = -1;
	private bool finished;

	private static readonly string[] MoveActions = { "move_left", "move_right", "move_up", "move_down" };

	public override void _Ready()
	{
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			if (arg.StartsWith("--stage=")) StageIndex = int.Parse(arg.Substring(8));
			else if (arg.StartsWith("--seed=")) Seed = long.Parse(arg.Substring(7));
			else if (arg.StartsWith("--timescale=")) TimeScale = float.Parse(arg.Substring(12), System.Globalization.CultureInfo.InvariantCulture);
			else if (arg == "--no-complete") Complete = false;
			else if (arg == "--log-props") logProps = true;
			else if (arg == "--slow-bleed") slowBleed = true;
			else if (arg == "--fresh-save") freshSave = true;
			else if (arg == "--all-unlocked") allUnlocked = true;
			else if (arg.StartsWith("--kill-hammers=")) killHammers = int.Parse(arg.Substring(15));
			else if (arg == "--wrong-mouth") wrongMouth = true;
			else if (arg == "--wayfinder-first") wayfinderFirst = true;
			else if (arg == "--lose-hut") loseHut = true;
			else if (arg.StartsWith("--fall-behind=")) fallBehind = float.Parse(arg.Substring(14), System.Globalization.CultureInfo.InvariantCulture);
			else if (arg == "--arrive-late") arriveLate = true;
			else if (arg == "--let-him-fall") letHimFall = true;
			else if (arg == "--straight") straightThrough = true;
			else if (arg == "--let-beacon-die") letBeaconDie = true;
			else if (arg.StartsWith("--tithe-level=")) titheLevel = int.Parse(arg.Substring(14));
			else if (arg == "--tithe-walk-off") titheWalkOff = true;
			else if (arg == "--step-off") stepOffDoor = true;
		}
		// The gibbet's cage says each change of phase, each rising and each fall (a run says nothing).
		GibbetCageEvent.LogPhases = true;
		ProcessMode = ProcessModeEnum.Always;
		SaveManager.WritesDisabled = true;
		if ((freshSave || allUnlocked) && GetNodeOrNull<SaveManager>("/root/SaveManager") is SaveManager saves)
		{
			var data = new SaveData();
			if (allUnlocked)
			{
				foreach (string id in UnlockCatalog.AllSpellIds) data.UnlockedSpellIds.Add(id);
				foreach (string id in UnlockCatalog.AllBoonIds) data.UnlockedBoonIds.Add(id);
				foreach (string id in UnlockCatalog.AllCharacterIds) data.UnlockedCharacterIds.Add(id);
			}
			saves.ReplaceInMemory(data);
			GD.Print($"EXPLOREPROBE save: {(allUnlocked ? "everything owned" : "brand new")}, in memory");
		}
		Global.SelectedCharacterIdx = 1;
		Global.SelectedStageIdx = StageIndex;
		Engine.TimeScale = TimeScale;
		GameStats.ResetRunTelemetry();
		GD.Print($"EXPLOREPROBE stage={StageIndex} seed={Seed} timescale={TimeScale}"
			+ (allUnlocked ? " all-unlocked" : freshSave ? " fresh-save" : "")
			+ (SecondEventStandIn.Active ? $" second-event-test={SecondEventStandIn.StageId}" : ""));

		game = ResourceLoader.Load<PackedScene>("res://scenes/node_2d_game.tscn").Instantiate<Node2DGame>();
		game.ProceduralSeed = Seed;
		AddChild(game);
	}

	public override void _ExitTree()
	{
		Engine.TimeScale = 1.0f;
		foreach (string a in MoveActions)
			Input.ActionRelease(a);
	}

	private void Check(string what, bool ok, string detail = "")
	{
		if (ok) passes++; else fails++;
		GD.Print($"EXPLOREPROBE {(ok ? "PASS" : "FAIL")} {what}{(detail.Length > 0 ? " - " + detail : "")}");
	}

	public override void _Process(double delta)
	{
		if (finished)
			return;
		stepClock += (float)delta;
		if (player == null || !IsInstanceValid(player))
		{
			player = game.RunPlayer;
			if (player == null)
				return;
		}
		player.MaxHP = Math.Max(player.MaxHP, 100000);
		player.CurrentHP = player.MaxHP;
		AutoPickLevelUp();
		if (wait > 0)
		{
			wait--;
			return;
		}
		// After a teleport, nothing is compared until the streamer has painted and furnished every
		// chunk round the new spot: a snapshot taken half-way would differ from the next for no reason.
		// Not while keeping pace with the barge (step 34): it walks, and a frame skipped there is a frame left behind.
		if (step >= 4 && step != 34 && game.Terrain != null && !game.Terrain.IsSettled && settleFrames < 2000)
		{
			settleFrames++;
			return;
		}
		settleFrames = 0;
		if (step >= 3 && player.IsDead)
		{
			Finish("player died");
			return;
		}

		switch (step)
		{
			case 0:
				// Let the stage build and the first chunks settle.
				wait = 20;
				step = 1;
				break;
			case 1:
				CheckPlacement();
				break;
			case 2:
				CheckDeterminism();
				break;
			case 3:
				// Away, far enough that the event's chunks and the wayfinder's are forgotten.
				SendAway();
				break;
			case 4:
				CheckHiddenThenTakeWayfinder();
				break;
			case 5:
				CheckRevealed();
				break;
			case 6:
				FirstVisit();
				break;
			case 7:
				RecordVisitAndLeave();
				break;
			case 8:
				ComeBack();
				break;
			case 9:
				CompareVisits();
				break;
			case 10:
				Arrive();
				break;
			case 11:
				CheckBegun();
				break;
			case 12:
				DriveToCompletion((float)delta);
				break;
			// Two events (see the header).
			case 20:
				HintsAlternate((float)delta);
				break;
			case 21:
				HintsAfterWin((float)delta);
				break;
			case 22:
				TakeWayfinderBetweenEvents();
				break;
			case 23:
				CheckRevealedBetweenEvents();
				break;
			// The Last Bridge's barge (see the header).
			case 30:
				BargeTakeWayfinder();
				break;
			case 31:
				BargeCheckRevealedThenNear();
				break;
			case 32:
				BargeWaitForTheFall((float)delta);
				break;
			case 33:
				BargeArrive();
				break;
			case 34:
				BargeKeepPace((float)delta);
				break;
		}
	}

	// The event being visited now: the chapter's first, then (with two) its second.
	private SideEvent Event => director != null && cur < director.Events.Count && IsInstanceValid(director.Events[cur]) ? director.Events[cur] : null;

	private bool Multi => director != null && director.Events.Count > 1;

	// "event 1 " on a chapter with two, nothing on a chapter with one - so a one-event chapter's lines read as they always did.
	private string Of(int index) => Multi ? $"event {index} " : "";

	private void Teleport(Vector2 to, int settleFrames = 60)
	{
		player.GlobalPosition = to;
		player.Velocity = Vector2.Zero;
		wait = settleFrames;
	}

	private void CheckPlacement()
	{
		director = game.GetNodeOrNull<SideEventDirector>("SideEventDirector");
		StageDefinition stage = StageCatalog.GetByIndex(StageIndex);
		IReadOnlyList<SideEventEntry> entries = stage != null ? SideEventDirector.EventsFor(stage.Id) : Array.Empty<SideEventEntry>();
		if (entries.Count == 0)
		{
			Check("a chapter without an event places nothing", director != null && director.Event == null
				&& director.Wayfinder == null && PointsOfInterest.Live.Count == 0);
			Finish("no side event on this stage");
			return;
		}
		bool placed = director.Events.Count == entries.Count && director.Events.All(e => e != null && e.Sites.Count > 0);
		Check("the director placed the event at stage start" + (entries.Count > 1 ? $"s ({entries.Count})" : ""), placed,
			string.Join("; ", director.Events.Select(e => $"{e.GetType().Name} at '{e.SiteId}' with {e.Sites.Count} site(s)")) + $", t={game.RunSeconds:0.0}s");
		Check("the wayfinder was placed", director.Wayfinder != null, director.Wayfinder?.Kind.ToString() ?? "none");
		if (!placed || director.Wayfinder == null)
		{
			Finish("nothing to probe");
			return;
		}
		if (director.Events.Count == 1 && director.Events[0] is BargeEvent moored)
		{
			CheckBargePlacement(moored);
			return;
		}

		Vector2 origin = game.StageOrigin;
		float chunk = TerrainStreamer.ChunkTiles * game.ProceduralTilePixels;
		TerrainStreamer terrain = game.Terrain;
		// The bands are where the roll lands; the open-ground search may move a site up to 1600 px.
		const float slack = 1700f;
		Vector2 w = director.Wayfinder.Site.Position;
		float wd = w.DistanceTo(origin);
		foreach (SideEvent e in director.Events)
		{
			if (e is DottedDoorEvent drawn)
			{
				// Placed by the Court's own building, not the bands: on the Gallery's floor at its west end, every dot on it.
				Vector2 at = drawn.Sites[0].Position;
				Check("the door is drawn on the Gallery's floor", drawn.Court != null && drawn.Court.RoomAt(at)?.Id == DottedDoorEvent.RoomId
					&& at.DistanceTo(drawn.DoorCentre) < 0.01f, $"({at.X:0},{at.Y:0})");
				bool dotsOnFloor = Enumerable.Range(0, DottedDoorEvent.DotCount).All(k => drawn.Court != null
					&& drawn.Court.IsFloor(drawn.DotWorld(k)) && drawn.Court.RoomAt(drawn.DotWorld(k))?.Id == DottedDoorEvent.RoomId);
				Check("every one of its dots is on the Gallery's floor", dotsOnFloor, $"{DottedDoorEvent.DotCount} dots, {DottedDoorEvent.PathLength:0} px of line");
				Check("the door stands on the building's floor", game.IsExploreGroundClear(at, 40f));
				continue;
			}
			if (e is SeersVoiceEvent seer)
			{
				// Placed by the Court's own rooms, not the bands: three different rooms of its list, the first far enough
				// from the stair, its one point of interest the first reading place.
				Vector2 first = seer.Sites[0].Position;
				Check("the seer's readings are three rooms of the Court", seer.ReadingRooms.Count == SeersVoiceEvent.Readings
					&& seer.ReadingRooms.Distinct().Count() == SeersVoiceEvent.Readings
					&& seer.ReadingRooms.All(r => SeersVoiceEvent.ReadingRoomIds.Contains(r)), string.Join(" -> ", seer.ReadingRooms));
				Check("only the first reading is a point of interest", seer.Sites.Count == 1 && first.DistanceTo(seer.ReadingSpots[0]) < 0.01f);
				Check("the first reading is a walk from the stair", first.DistanceTo(origin) >= SeersVoiceEvent.FirstFromStart, $"{first.DistanceTo(origin):0} px");
				Check("the first reading stands on the building's floor", game.IsExploreGroundClear(first, 40f));
				for (int i = 1; i < seer.ReadingSpots.Count; i++)
					notes.Add($"reading {i + 1} in {seer.ReadingRooms[i]}, {seer.ReadingSpots[i].DistanceTo(seer.ReadingSpots[i - 1]):0} px straight from the last");
				continue;
			}
			for (int i = 0; i < e.Sites.Count; i++)
			{
				Vector2 p = e.Sites[i].Position;
				float d = p.DistanceTo(origin);
				float min = e.PlacementMinChunks * chunk - slack;
				// The lanterns are a chain from the first, so later ones may lie a mire's reach further.
				float max = e.PlacementMaxChunks * chunk + slack + (i > 0 ? 2200f : 0f);
				if (i == 0 && e is WalkingHutEvent)
					Check("the hut is surrounded by deep woodland", game.CreateExplorePlacer(0, 6f, 8f).HasWoodedSurrounds(p, WalkingHutEvent.WoodlandRingRadius));
				Check($"{Of(e.Index)}site {i} ({e.Sites[i].Glyph}) is in the event band", d >= min && d <= max, $"{d:0} px = {d / chunk:0.0} chunks");
				bool open = terrain == null || (!terrain.IsBlockedAtWorld(p) && !terrain.IsWallAtWorld(p)
					&& !terrain.IsFenceAtWorld(p) && !terrain.IsHazardAtWorld(p));
				Check($"{Of(e.Index)}site {i} stands on open ground", open && game.IsExploreGroundClear(p, 40f));
			}
		}
		Check("the wayfinder is in its band", wd >= SideEventDirector.WayfinderMinChunks * chunk - slack
			&& wd <= SideEventDirector.WayfinderMaxChunks * chunk + slack, $"{wd:0} px = {wd / chunk:0.0} chunks");
		foreach (SideEvent e in director.Events)
		{
			float angle = Mathf.RadToDeg(Mathf.Abs(Mathf.AngleDifference((w - origin).Angle(), (e.Sites[0].Position - origin).Angle())));
			// The search can turn a site by a few degrees; the roll keeps 75. In the Court the band holds floor only in the
			// building's quarter north-east of the stair, so the wayfinder is as far round as that allows (25 at the least).
			float wanted = e.PlacedByChapterRules ? 25f : 60f;
			Check(Multi ? $"the wayfinder is well off event {e.Index}'s bearing" : "the wayfinder is well off the event's bearing", angle >= wanted, $"{angle:0} degrees apart");
		}
		// Two events: their main sites keep SideEventDirector.MinSiteSeparationDegrees between them, measured here
		// where they really are, not where they were rolled.
		for (int i = 0; i < director.Events.Count; i++)
		{
			for (int j = i + 1; j < director.Events.Count; j++)
			{
				// Two events the building places (the Court's seer and its door) are where the house has them: the 90
				// degrees round the start is the band's rule, so only the distance is noted.
				if (director.Events[i].PlacedByChapterRules && director.Events[j].PlacedByChapterRules)
				{
					notes.Add($"events {i} and {j}: placed by the building, {director.Events[i].Sites[0].Position.DistanceTo(director.Events[j].Sites[0].Position):0} px apart");
					continue;
				}
				float apart = Mathf.RadToDeg(ExplorePlacer.Separation(
					ExplorePlacer.BearingFrom(origin, director.Events[i].Sites[0].Position),
					ExplorePlacer.BearingFrom(origin, director.Events[j].Sites[0].Position)));
				Check($"events {i} and {j} are at least {SideEventDirector.MinSiteSeparationDegrees:0} degrees apart",
					apart >= SideEventDirector.MinSiteSeparationDegrees - 0.01f, $"{apart:0.0} degrees");
				notes.Add($"events {i} and {j}: {apart:0.0} degrees apart, "
					+ $"{director.Events[i].Sites[0].Position.DistanceTo(director.Events[j].Sites[0].Position):0} px");
			}
		}
		Check(Multi ? "every event is dormant" : "the event is dormant", director.Events.All(e => e.IsDormant));
		Check("nothing is marked before it is seen or revealed", !PointsOfInterest.AnyMarked);
		siteAtStart = director.Events.Select(e => e.Sites[0].Position).ToList();
		foreach (SideEvent e in director.Events)
			notes.Add($"{Of(e.Index)}{(Multi ? $"at {e.SiteId}, " : "")}event sites: {string.Join("; ", e.Sites.Select(s => $"({s.Position.X:0},{s.Position.Y:0})"))}");
		notes.Add($"wayfinder: ({w.X:0},{w.Y:0}) {wd:0} px");
		for (int i = 0; i < director.Events.Count; i++)
			wonEvents.Add(false);
		step = 2;
	}

	private void CheckDeterminism()
	{
		// Laid out again from the same seed, by the run's own code (SideEventDirector.LayOut): every event of the
		// chapter in its place, the second still kept round from the first.
		StageDefinition stage = StageCatalog.GetByIndex(StageIndex);
		int before = PointsOfInterest.Live.Count;
		var again = SideEventDirector.LayOut(SideEventDirector.EventsFor(stage.Id),
			stream => game.CreateExplorePlacer(stream, SideEventDirector.EventMinChunks, SideEventDirector.EventMaxChunks));
		bool same = again.Count == director.Events.Count;
		for (int k = 0; same && k < again.Count; k++)
		{
			SideEvent copy = again[k].Event, real = director.Events[k];
			same = copy.SiteId == real.SiteId && copy.Sites.Count == real.Sites.Count;
			for (int i = 0; same && i < copy.Sites.Count; i++)
				same = copy.Sites[i].Position.DistanceTo(real.Sites[i].Position) < 0.01f;
		}
		// Leave the run's list as it was: the copies' sites were never the run's.
		PointsOfInterest.Live.RemoveRange(before, PointsOfInterest.Live.Count - before);
		foreach (var (copy, _) in again)
			copy.Free();
		Check(Multi ? "the same seed lays every event out in the same places" : "the same seed lays the event out in the same places", same);
		step = Event is BargeEvent ? 30 : 3;
	}

	// --- the Last Bridge's barge ---------------------------------------------------------------------------

	private BridgeRules Bridge => game.Rules as BridgeRules;

	// Its world point level with the barge, d px along from it, on its side of the deck this far in from the shore.
	private Vector2 DeckLevelWith(BargeEvent barge, float along, float fromShore = 150f)
	{
		float s = barge.BargeS + along;
		float t = barge.Side * Mathf.Max(0f, BridgeBand.HalfWidthAt(s) - fromShore);
		return Bridge.WorldAt(s, t);
	}

	private void CheckBargePlacement(BargeEvent barge)
	{
		BridgeRules rules = Bridge;
		Check("the bridge's rules are running", rules != null);
		if (rules == null)
		{
			Finish("no bridge");
			return;
		}
		Vector2 site = barge.Sites[0].Position;
		int piece = BridgeBand.PieceIndexAt(barge.MooringS);
		BridgeBand.Piece span = piece >= 0 ? BridgeBand.Pieces[piece] : default;
		Check("it is moored under one of its spans", piece >= 0 && span.Kind == BridgeBand.PieceKind.Span
			&& System.Array.IndexOf(BargeEvent.MooringSpans, span.Index) >= 0,
			$"{barge.MooringS:0} px along, span {span.Index + 1}, the {(barge.Side > 0 ? "south-east" : "north-west")} side");
		Check("its whole drift is inside that span", barge.MooringS + BargeEvent.DriftLength <= span.S1 - BargeEvent.LandingBeforeSpanEnd + 0.5f,
			$"landing at {barge.MooringS + BargeEvent.DriftLength:0}, the span ends at {span.S1:0}");
		TerrainStreamer terrain = game.Terrain;
		Check("it rides on the water, not the deck", terrain == null || terrain.IsBlockedAtWorld(site),
			$"{Mathf.Abs(rules.AcrossOf(site)):0} px across, the shore at {BridgeBand.HalfWidthAt(barge.MooringS):0}");
		Vector2 deck = DeckLevelWith(barge, 0f);
		Check("the deck level with it is open ground", terrain == null || (!terrain.IsBlockedAtWorld(deck) && !terrain.IsHazardAtWorld(deck)));
		Vector2 w = director.Wayfinder.Site.Position;
		float ws = rules.AlongOf(w);
		float chunk = TerrainStreamer.ChunkTiles * game.ProceduralTilePixels;
		Check("the wayfinder is on the deck, ahead of the start, in its band along the bridge",
			BridgeBand.IsOnDeck(w - game.StageOrigin, 40f) && ws >= SideEventDirector.WayfinderMinChunks * chunk - 1f
			&& ws <= SideEventDirector.WayfinderMaxChunks * chunk + 1f, $"{ws:0} px along = {ws / chunk:0.0} chunks");
		Check("the event is dormant", barge.IsDormant);
		Check("nothing is marked before it is seen or revealed", !PointsOfInterest.AnyMarked);
		siteAtStart = new List<Vector2> { site };
		notes.Add($"barge moored at ({site.X:0},{site.Y:0}), {barge.MooringS:0} px along span {span.Index + 1}, side {barge.Side}; wayfinder {ws:0} px along");
		wonEvents.Add(false);
		step = 2;
	}

	private void BargeTakeWayfinder()
	{
		Vector2 w = director.Wayfinder.Site.Position;
		Teleport(w - (player.FootPosition - player.GlobalPosition), 40);
		step = 31;
	}

	private void BargeCheckRevealedThenNear()
	{
		var barge = (BargeEvent)Event;
		bool taken = director.Wayfinder == null || !IsInstanceValid(director.Wayfinder) || director.Wayfinder.Taken;
		Check("walking onto the wayfinder takes it", taken);
		Check("taking it marks the barge for an arrow", PointsOfInterest.Revealed && barge.Sites[0].Marked);
		// 900 px short of it along the bridge, level across: near enough to see its light, not arrived.
		Teleport(DeckLevelWith(barge, -900f, BridgeBand.HalfWidthAt(barge.BargeS)), 60);
		bargeWait = 0f;
		step = 32;
	}

	// Dormant 900 px short of it; with --arrive-late, kept there while the fall passes its mooring.
	private void BargeWaitForTheFall(float delta)
	{
		var barge = (BargeEvent)Event;
		if (bargeWait == 0f)
			Check("dormant 900 px short of it along the bridge", barge.IsDormant);
		bargeWait += delta;
		if (arriveLate && Bridge.FrontS < barge.MooringS + 200f)
		{
			// He has gone on past it, well beyond arriving, and the fall comes on behind them both.
			player.GlobalPosition = Bridge.WorldAt(barge.BargeS + 1400f, 0f);
			if (bargeWait > 600f)
			{
				Check("the fall reached its mooring", false, $"the fall at {Bridge.FrontS:0} after {bargeWait:0} s");
				Finish("gave up waiting");
			}
			return;
		}
		if (arriveLate)
		{
			wait = 90;
			Check("the fall has taken its mooring and it waits ahead of it", barge.IsDormant && barge.BargeS >= Bridge.FrontS + BargeEvent.RestLead - 2f,
				$"mooring {barge.MooringS:0}, the fall {Bridge.FrontS:0}, the barge {barge.BargeS:0}");
			Check("its arrow went with it", barge.Sites[0].Position.DistanceTo(barge.BargeWorld) < 1f && barge.BargeS > barge.MooringS);
		}
		step = 33;
	}

	private void BargeArrive()
	{
		var barge = (BargeEvent)Event;
		if (!barge.IsFound)
		{
			Teleport(DeckLevelWith(barge, -200f), 10);
			return;
		}
		Check("it begins when the player comes level with it", barge.IsFound, $"t={game.RunSeconds:0.0}s, pace {game.PaceSeconds:0}");
		string line = barge.ObjectiveShown;
		Check("the objective line is up", !string.IsNullOrEmpty(line), string.IsNullOrEmpty(line) ? "no line" : line);
		completeClock = 0f;
		step = 34;
	}

	// Level with it, a little behind, on its side of the deck: every frame, as a player walking beside it would be.
	// --fall-behind=S stops for S seconds a third of the way in.
	private void BargeKeepPace(float delta)
	{
		var barge = (BargeEvent)Event;
		completeClock += delta;
		if (barge == null || barge.IsWon)
		{
			Check("it came ashore: the event was won", barge == null || barge.IsWon, $"{completeClock:0.0} s of game time after arriving");
			Check("he stayed ahead of the fall all the way", bargeLeadMin > 0f, $"closest {bargeLeadMin:0} px ahead of it");
			if (fallBehind > 0f)
			{
				bool over = fallBehind > BargeEvent.OutOfReachSeconds;
				Check(over ? "falling behind longer than its grace failed the drift, and it drifted back" : "falling behind for less than its grace was forgiven",
					over ? barge.DriftsFailed >= 1 : barge.DriftsFailed == 0 && bargeOutSeen,
					$"{barge.DriftsFailed} drift(s) failed; out of reach {bargeOutMax:0.0} s at most; held back {fallBehind:0.0} s");
			}
			else
				Check("kept pace in one drift", barge.DriftsFailed == 0, $"{barge.DriftsFailed} failed");
			SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
			if (barge != null && !barge.PaysChest && save != null)
				Check("'morgan' is freed, in memory", GlobalStatsManager.IsCharacterUnlocked(save, "morgan"), $"writes {(SaveManager.WritesDisabled ? "off" : "ON")}");
			else if (barge != null)
				Check("it paid a chest (she is already free on this save)", barge.PaysChest);
			Check("no arrows are left once it is over", !PointsOfInterest.AnyMarked);
			Finish("event over");
			return;
		}
		if (completeClock > 300f)
		{
			Check("the event can be finished", false, $"not finished after 300 s ({barge.Progress * 100f:0}% of a drift, {barge.DriftsFailed} failed)");
			Finish("gave up");
			return;
		}
		bargeLeadMin = Mathf.Min(bargeLeadMin, Bridge.AlongOf(player.GlobalPosition) - Bridge.FrontS);
		if (barge.SecondsOutOfReach > 0f)
		{
			bargeOutSeen = true;
			bargeOutMax = Mathf.Max(bargeOutMax, barge.SecondsOutOfReach);
		}
		// Held back: from a third of the way in, for --fall-behind seconds, he falls back out of its reach - a pack
		// in his way, a step back from it - and stands there; then he catches it up.
		if (fallBehind > 0f && bargeHeld < 0f && barge.IsDrifting && barge.Progress >= 0.33f)
		{
			bargeHeld = 0f;
			fellBackTo = DeckLevelWith(barge, -(BargeEvent.Reach + 140f));
			notes.Add($"fell back out of reach at {barge.Progress * 100f:0}% of the drift, for {fallBehind:0.0} s");
		}
		if (bargeHeld >= 0f && bargeHeld < fallBehind)
		{
			bargeHeld += delta;
			player.GlobalPosition = fellBackTo;
		}
		else if (barge.IsReturning)
		{
			// It drifts back to its mooring: he walks back with it, and waits there.
			player.GlobalPosition = DeckLevelWith(barge, -60f);
		}
		else
		{
			player.GlobalPosition = DeckLevelWith(barge, -90f);
		}
		player.Velocity = Vector2.Zero;
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (!IsInstanceValid(enemy) || enemy.IsDying || enemy.IsMiniBoss)
				continue;
			if (enemy.GlobalPosition.DistanceTo(player.GlobalPosition) < 500f)
				enemy.QueueFree();
		}
	}

	private void SendAway()
	{
		Vector2 origin = game.StageOrigin;
		Vector2 toEvent = (Event.Sites[0].Position - origin).Normalized();
		Vector2 away = -toEvent;
		if (Multi)
		{
			// Away from every event: the bearing, of 36, farthest round from all of them, so no second site is
			// on screen out there to be seen and marked (which would retire its hints before they were heard).
			float best = -1f;
			for (int k = 0; k < 36; k++)
			{
				float bearing = k * Mathf.Tau / 36f;
				float gap = director.Events.Min(e => ExplorePlacer.Separation(bearing, ExplorePlacer.BearingFrom(origin, e.Sites[0].Position)));
				if (gap > best)
				{
					best = gap;
					away = new Vector2(Mathf.Cos(bearing), Mathf.Sin(bearing));
				}
			}
		}
		lookout = origin + away * 6f * TerrainStreamer.ChunkTiles * game.ProceduralTilePixels;
		// The Court is a building with rock all round it: the far point is the room's floor furthest from the event.
		if (CourtFloor != null)
			lookout = CourtFloor.Rooms.Where(r => !r.Hidden && CourtFloor.IsFloor(r.Centre))
				.OrderByDescending(r => r.Centre.DistanceTo(Event.Sites[0].Position)).First().Centre;
		Teleport(lookout, 120);
		step = 4;
	}

	private void CheckHiddenThenTakeWayfinder()
	{
		Check("still nothing marked after walking away", !PointsOfInterest.AnyMarked);
		Check("still dormant", director.Events.All(e => e.IsDormant));
		if (Multi && !wayfinderFirst)
		{
			// The hints first, while nothing is found or marked: they must alternate the two events. A run plays
			// four at most (StageHintCatalog's schedule): two here - the first event's, then the second's - and two
			// after the first is won, the first of which the rotation would have given back to it.
			hintsWanted = 2;
			hintClock = 0f;
			step = 20;
			return;
		}
		// Onto the wayfinder: its chunks are loaded on arrival, and it is taken with the feet.
		Vector2 w = director.Wayfinder.Site.Position;
		Teleport(w - (player.FootPosition - player.GlobalPosition), 40);
		step = 5;
	}

	private void CheckRevealed()
	{
		bool taken = director.Wayfinder == null || !IsInstanceValid(director.Wayfinder) || director.Wayfinder.Taken;
		Check("walking onto the wayfinder takes it", taken);
		Check("taking it reveals", PointsOfInterest.Revealed);
		foreach (SideEvent e in director.Events)
		{
			int marked = e.Sites.Count(s => s.Marked);
			Check(Multi ? $"every site of event {e.Index} ({e.SiteId}) is marked for an arrow" : "every site of the event is marked for an arrow",
				marked == e.Sites.Count, $"{marked} of {e.Sites.Count}");
		}
		if (Multi)
			Check("no hint plays once the wayfinder has marked every event", director.Hints == null || !director.Hints.ForceHint());
		step = 6;
	}

	// --- two events -----------------------------------------------------------------------------------

	// Which of the chapter's events can be hinted at on this save: dormant, unmarked, and paying something it lacks.
	private List<string> HintableSites()
	{
		SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		return director.Events
			.Where(e => IsInstanceValid(e) && e.IsDormant && !e.PaysChest && !e.Sites.Any(s => s.Marked)
				&& StageHintCatalog.ForSite(e.SiteId) != null && (save == null || !DiscoveryRewards.IsSiteExhausted(save, e.SiteId)))
			.Select(e => e.SiteId).ToList();
	}

	// Forces hints, one as soon as the screen's turn allows (StageHintDirector.ForceHint refuses while the last
	// one's line is up - never two at once), until enough have played since a count, or the clock runs out.
	// True when done.
	private bool ForceHints(float delta, int wanted, int already)
	{
		hintClock += delta;
		StageHintDirector hints = director.Hints;
		if (hints != null && hints.PlayedSites.Count - already < wanted && hintClock < 150f)
		{
			// Asked again on the very frame one starts: refused, or two would be up at once.
			if (hints.ForceHint() && hints.ForceHint())
				hintsAtOnce++;
			return false;
		}
		return true;
	}

	private int hintsAtOnce;

	private int hintsBeforeWin;

	private void HintsAlternate(float delta)
	{
		List<string> hintable = HintableSites();
		StageHintDirector hints = director.Hints;
		if (hintable.Count == 0)
		{
			Check("no hint plays when this save owns what every event pays", hints == null || !hints.ForceHint(),
				allUnlocked ? "everything owned" : "nothing to hint at");
			hintsBeforeWin = hints?.PlayedSites.Count ?? 0;
			step = 6;
			return;
		}
		if (!ForceHints(delta, hintsWanted, 0))
			return;
		List<string> played = hints.PlayedSites.ToList();
		notes.Add($"hints before either was found: {string.Join(", ", played)} ({hintClock:0.0}s of game time)");
		Check($"{hintsWanted} hints played, one at a time", played.Count >= hintsWanted && hintsAtOnce == 0,
			$"{played.Count} in {hintClock:0.0}s; a second asked for on the frame one started was refused {(hintsAtOnce == 0 ? "every time" : $"all but {hintsAtOnce} times")}");
		bool alternates = true;
		for (int i = 1; i < played.Count; i++)
			alternates &= hintable.Count < 2 || played[i] != played[i - 1];
		Check(hintable.Count >= 2 ? "the hints alternate the chapter's events" : "the only event worth hinting at has every hint",
			alternates && hintable.All(played.Contains) && played.All(hintable.Contains), string.Join(" -> ", played));
		hintsBeforeWin = played.Count;
		step = 6;
	}

	private void HintsAfterWin(float delta)
	{
		StageHintDirector hints = director.Hints;
		List<string> hintable = HintableSites();
		if (hintable.Count == 0)
		{
			Check("no hint plays after the win when nothing is left to hint at", hints == null || !hints.ForceHint());
			step = 22;
			return;
		}
		if (!ForceHints(delta, 2, hintsBeforeWin))
			return;
		List<string> after = hints.PlayedSites.Skip(hintsBeforeWin).ToList();
		string wonSite = director.Events[0].SiteId;
		notes.Add($"hints after event 0 was won: {string.Join(", ", after)}");
		Check("a won event stops hinting; the other has every slot after", after.Count >= 2 && !after.Contains(wonSite) && hintsAtOnce == 0
			&& after.All(hintable.Contains), string.Join(" -> ", after));
		// That was the run's fourth: the schedule has no fifth, whoever is still out there.
		if (hints.PlayedSites.Count >= 4)
			Check("the run's hints stop at four", !hints.ForceHint(), $"{hints.PlayedSites.Count} played");
		step = 22;
	}

	private void TakeWayfinderBetweenEvents()
	{
		Vector2 w = director.Wayfinder.Site.Position;
		Teleport(w - (player.FootPosition - player.GlobalPosition), 40);
		step = 23;
	}

	private void CheckRevealedBetweenEvents()
	{
		bool taken = director.Wayfinder == null || !IsInstanceValid(director.Wayfinder) || director.Wayfinder.Taken;
		Check("walking onto the wayfinder takes it", taken);
		Check("taking it reveals", PointsOfInterest.Revealed);
		int left = 0, marked = 0;
		foreach (PointOfInterest site in PointsOfInterest.Live.Where(p => !p.IsWayfinder && !p.Done))
		{
			left++;
			if (site.Marked)
				marked++;
		}
		Check("it marks every site still to be found", left > 0 && marked == left, $"{marked} of {left}");
		cur++;
		step = 6;
	}

	// 900 px from the first site, on the side away from the rest of the event (the lanterns are a
	// chain, and 900 px south of one can be beside the next): near enough to load its ground, past
	// every arrival radius.
	private Vector2 Approach
	{
		get
		{
			Vector2 first = Event.Sites[0].Position;
			Vector2 centroid = Vector2.Zero;
			foreach (PointOfInterest site in Event.Sites)
				centroid += site.Position;
			centroid /= Event.Sites.Count;
			Vector2 away = first - centroid;
			if (CourtFloor != null)
			{
				// In the Court: a point of floor about 900 px off, on the first of sixteen bearings that has one.
				for (float reach = 900f; reach >= 500f; reach -= 100f)
					for (int k = 0; k < 16; k++)
					{
						Vector2 p = first + Vector2.FromAngle(k * Mathf.Tau / 16f) * reach;
						if (CourtFloor.IsFloor(p) && CourtFloor.IsFloor(p + new Vector2(0f, 40f)))
							return p;
					}
			}
			return first + (away.LengthSquared() > 1f ? away.Normalized() : Vector2.Down) * 900f;
		}
	}

	private void FirstVisit()
	{
		Teleport(Approach, 200);
		step = 7;
	}

	private void RecordVisitAndLeave()
	{
		if (secondBegunEarly && cur > 0)
			Check($"{Of(cur)}still running at 900 px, begun while the first ran", Event.IsFound && !Event.IsWon);
		else
			Check("dormant at 900 px from the site", Event.IsDormant);
		visitA = PropsNear(Event.Sites[0].Position, 700f);
		int inside = 0;
		foreach (PointOfInterest site in Event.Sites)
			inside += PropsNear(site.Position, 70f).Count;
		Check("no decor, prop or hazard inside a site's footprint", inside == 0, $"{visitA.Count} things within 700 px of site 0, {inside} on a footprint");
		Teleport(lookout, 200);
		step = 8;
	}

	private void ComeBack()
	{
		Teleport(Approach, 200);
		step = 9;
	}

	private void CompareVisits()
	{
		List<Vector2> visitB = PropsNear(Event.Sites[0].Position, 700f);
		bool same = visitA.Count == visitB.Count && visitA.Zip(visitB).All(p => p.First.DistanceTo(p.Second) < 0.5f);
		Check("the ground round the site is furnished the same after it was forgotten", same, $"{visitA.Count} then {visitB.Count}");
		if (!same)
		{
			notes.Add("first visit: " + string.Join(", ", visitA.Select(v => $"({v.X:0},{v.Y:0})")));
			notes.Add("second visit: " + string.Join(", ", visitB.Select(v => $"({v.X:0},{v.Y:0})")));
		}
		Check("the site did not move", Event.Sites[0].Position.DistanceTo(siteAtStart[cur]) < 0.01f);
		step = 10;
	}

	private void Arrive()
	{
		// The Tithe-Reeve's price is a level at the moment he comes: put him at the level asked for first.
		if (Event is TitheScaleEvent && titheLevel > 0)
		{
			player.CurrentLevel = titheLevel;
			player.CurrentXP = 0;
			player.XPToNextLevel = player.CalculateXPForLevel(titheLevel);
		}
		if (Event is TitheScaleEvent)
		{
			titheXpAtArrival = player.RunXpGathered;
			titheArrivedAt = game.RunSeconds;
		}
		Teleport(Event.Sites[0].Position + new Vector2(0f, 30f), 20);
		step = 11;
	}

	private void CheckBegun()
	{
		Check($"{Of(cur)}it begins when the player arrives", Event.IsFound, $"t={game.RunSeconds:0.0}s");
		// Its own line, and the one under the timer (two events' lines share the spot; the one arrived at speaks).
		string line = Event.ObjectiveShown;
		Check($"{Of(cur)}the objective line is up", !string.IsNullOrEmpty(line), string.IsNullOrEmpty(line) ? "no line" : line);
		if (Multi && wayfinderFirst && cur == 0 && !secondBegunEarly)
		{
			// Two at once: the second begun where it stands while the first runs. The line under the timer is
			// the one begun last; the first's goes quiet (its line and arrow hidden), and comes back after.
			secondBegunEarly = true;
			director.Start(1);
			Check("two running: the one begun last speaks under the timer", director.Events[1].HoldsObjectiveLine
				&& !director.Events[0].HoldsObjectiveLine && director.Events[1].IsFound);
		}
		if (!Complete)
		{
			Finish("arrived (--no-complete)");
			return;
		}
		completeClock = 0f;
		step = 12;
	}

	// Stand on whatever the event wants - the objective arrow's target, else the nearest site not
	// yet done - clear the ordinary swarm round it, and bleed any elite there.
	private void DriveToCompletion(float delta)
	{
		completeClock += delta;
		SideEvent e = Event;
		// The seer's one site is done the moment he arrives (an arrow on the later rooms would be the answer): it is over
		// only once the third reading is reached.
		bool seeking = e is SeersVoiceEvent sv && sv.Leg < SeersVoiceEvent.Readings && !sv.IsWon;
		if (e == null || !seeking && e.Sites.All(s => s.Done) && PointsOfInterest.ObjectiveTarget == null)
		{
			bool won = e == null || e.IsWon;
			Check($"{Of(cur)}the event can be finished", won, $"{completeClock:0.0}s of game time after arriving");
			if (secondBegunEarly && cur == 0)
				Check("its win line takes the line under the timer from the one still running", e == null || e.HoldsObjectiveLine,
					$"the other {(director.Events[1].IsFound && !director.Events[1].IsWon ? "still running, quiet" : "not running")}");
			if (cur < wonEvents.Count)
				wonEvents[cur] = won;
			// The chapter's last event: nothing at all is marked any more. An earlier one: nothing of it is.
			bool last = director == null || cur >= director.Events.Count - 1;
			Check($"{Of(cur)}no arrows are left once it is over", last ? !PointsOfInterest.AnyMarked : e == null || !e.Sites.Any(s => s.Marked));
			if (e is GreatAnvilEvent forge)
			{
				Check("every chain is broken", forge.ChainsBroken == GreatAnvilEvent.StakeCount,
					$"{forge.HammersWoken} hammers woken, {forge.HammersWoken - GreatAnvilEvent.HammerCount} of them replacements");
				Check("the hammers broke them without one slam landing on him", forge.SlamsOnPlayer == 0,
					$"{forge.SlamsOnPlayer} slam(s) caught him");
				if (killHammers > 0)
					Check("every hammer killed before it struck was replaced", hammersKilled == killHammers
						&& forge.HammersWoken >= GreatAnvilEvent.HammerCount + killHammers,
						$"{hammersKilled} killed, {forge.HammersWoken} woken");
			}
			if (e is NineMouthsEvent heard)
			{
				Check(e.PaysChest ? "her mouth paid a chest (she is already free on this save)" : "the win was her first",
					true, $"PaysChest={e.PaysChest}");
				Check("she was found by listening", heard.CallsHeard > 0,
					$"{heard.CallsHeard} calls heard, freed at mouth {heard.TrueMouth}, {heard.WrongAnswers} wrong mouths");
				if (wrongMouth)
				{
					Check("a wrong mouth answered with a flight", heard.WrongAnswers >= 1 && heard.GloomwingsSent >= NineMouthsEvent.FlightSize,
						$"mouth {mouthWrongFirst}: {heard.WrongAnswers} answers, {heard.GloomwingsSent} Gloomwings");
					Check("and her mouth moved", mouthTrueBefore >= 0 && mouthTrueAfter >= 0 && mouthTrueBefore != mouthTrueAfter,
						$"{mouthTrueBefore} -> {mouthTrueAfter}");
				}
			}
			if (e is WalkingHutEvent ran)
			{
				Check("the hut fled: it bolted, and the bolts gained on him", ran.BoltsStarted >= 2,
					$"{ran.BoltsStarted} bolts, {ran.BoltsCut} cut short by the ground, {ran.FreshPrintsLaid} prints laid");
				Check("its feet left prints, and its old trails were laid", ran.FreshPrintsLaid > 0 && ran.OldPrintCount > 0,
					$"{ran.FreshPrintsLaid} fresh, {ran.OldPrintCount} old");
				Check("it tired and knelt", ran.IsTired, $"bar {ran.Progress * 100f:0}%");
				if (loseHut)
				{
					Check("lost: the bar held while he was away", lostPhase == 2 && progressOutside >= 0f
						&& Mathf.Abs(progressAfterWait - progressOutside) < 0.001f,
						$"{progressOutside * 100f:0.0}% when he passed its reach, {progressAfterWait * 100f:0.0}% after waiting");
					Check("lost: it calmed and stood, then was found again and run down", ran.TimesCalmed >= 1 && ran.IsWon,
						$"calmed {ran.TimesCalmed} time(s)");
				}
			}
			if (e is GibbetCageEvent led)
			{
				Check("the winch lowered the cage and he stepped out", led.Releases >= 1, $"{led.Releases} time(s)");
				Check("he followed the player's steps to the landing", led.BestProgress >= 0.99f && led.Walked > GibbetCageEvent.LandingMin * 0.8f,
					$"{led.Walked:0} px walked, best {led.BestProgress * 100f:0}% of the way; the probe waited for him {escortWaits} frame(s) and turned off water {escortDetours} time(s)");
				RotPoolLayout rot = led.Rot;
				Check("the rot was laid between the cage and the landing, with a way through it on paper",
					rot != null && rot.Pools.Count >= 3 && rot.Pools.Count <= RotPoolLayout.MaxCount && rot.Route != null,
					rot == null ? "no layout" : $"{rot.Pools.Count} pools, roll {rot.Attempts}{(rot.Trimmed ? " (trimmed)" : "")}, "
						+ $"{(rot.Route == null ? "no way through" : $"{rot.Route.Length - 1} legs")}{(rot.RouteOnGround ? "" : " on open ground")}");
				Check("the straight line from the winch to the landing runs through the rot", rot != null && rot.CrossesBite(led.WayStart, led.Landing));
				// --straight: a walk ends at the landing or in a fall, so if he never fell the straight walk was the one that
				// reached the landing, and every bite he took was on it.
				straightReachedLanding = straightThrough && led.Falls == 0;
				if (straightReachedLanding)
					straightBites = led.RotTicks;
				if (!straightReachedLanding)
					Check("led round the rot: the walk that reached the landing took no bite from it", led.RotTicksThisWalk == 0,
						$"{led.RotTicksThisWalk} bites on that walk, {led.RotTicks} in all ({led.PoolEntries} pools stepped in), {led.LoopsLeftOut} loops left out");
				Check("nothing but the rot touched him", led.Health == GibbetCageEvent.HealthMax - led.RotTicksThisWalk * GibbetCageEvent.RotDamage,
					$"{led.Health} of {GibbetCageEvent.HealthMax} left; a body within a touch of him on {closeFrames} frame(s); {led.Risen} Mired risen, {led.RisenInGaps} of them in the gaps");
				if (straightThrough)
					Check("straight: walked straight at the landing, the rot bit him", straightBites > 0,
						$"{straightBites} bites ({straightBites * GibbetCageEvent.RotDamage} of his {GibbetCageEvent.HealthMax}), "
						+ (straightReachedLanding ? "and he reached the landing" : "and he fell"));
				Check("the punt took him", led.IsWon);
				if (letHimFall)
				{
					Check("let fall: the rot had all of him, and the bog took him back to the cage", led.Falls >= 1 && backInCageAfterFall,
						$"{led.Falls} fall(s), back in the cage: {backInCageAfterFall}");
					Check("let fall: the cage was lowered again and the walk began again from it", led.Releases >= 2,
						$"{led.Releases} releases");
				}
			}
			if (e is SeersVoiceEvent sawHim)
			{
				SeekStep(sawHim, 0f);   // closes the last leg's line
				Check("he was found by seeking: the voice read on every leg", sawHim.ReadingsHeard >= SeersVoiceEvent.Readings - 1
					&& seekLegsHeard == SeersVoiceEvent.Readings - 1, $"{sawHim.ReadingsHeard} readings heard, {seekLegsHeard} legs walked to the voice");
				Check("all three readings, to the desk", sawHim.Leg == SeersVoiceEvent.Readings && sawHim.IsWon, string.Join(" -> ", sawHim.ReadingRooms));
				foreach (string leg in seekLog)
					notes.Add(leg);
			}
			if (e is TombBeaconsEvent tomb)
			{
				float spread = float.MaxValue;
				for (int i = 0; i < TombBeaconsEvent.BeaconCount; i++)
					for (int j = i + 1; j < TombBeaconsEvent.BeaconCount; j++)
						spread = Mathf.Min(spread, tomb.BeaconAt(i).DistanceTo(tomb.BeaconAt(j)));
				Check("the beacons stand about 700 px apart round the tomb", spread >= TombBeaconsEvent.BeaconSpacing - 120f,
					$"nearest pair {spread:0} px");
				Check("all three burned together and the tomb opened", tomb.IsOpening && tomb.IsWon,
					$"{tomb.TimesLit} kindled, {tomb.TimesWentOut} put out by the wind, {tomb.DeadDrawn} of the dead drawn to the light");
				if (letBeaconDie)
				{
					Check("a beacon left burning went out on the wind, and went cold", beaconWentCold && tomb.TimesWentOut >= 1,
						$"out at {beaconWentOutAt:0.0}s after arriving");
					Check("and it was lit again with the other two", tomb.TimesLit >= TombBeaconsEvent.BeaconCount + 1,
						$"{tomb.TimesLit} kindled in all");
				}
			}
			if (e is TitheScaleEvent tithe)
			{
				Check("the price was one level's XP at that moment of the run", tithe.Price >= 1f,
					$"{tithe.Price:0} for level {tithe.LevelAtArrival}");
				Check("the pan was paid from orbs that fell near it", tithe.IsPaid && tithe.OrbsTaken > 0 && tithe.Paid >= tithe.Price - 0.5f,
					$"{tithe.Paid:0} of {tithe.Price:0} in {tithe.OrbsTaken} orbs ({tithe.ValueTaken} XP), "
					+ $"{(titheArrivedAt >= 0f ? game.RunSeconds - titheArrivedAt : 0f):0.0} s after arriving; "
					+ $"he banked {player.RunXpGathered - titheXpAtArrival} XP from elsewhere meanwhile");
				Check("the scale stopped taking once it was paid", !GranaryTithe.Open, $"open {GranaryTithe.Open}");
				if (titheWalkOff)
					Check("walked off: what was paid stayed paid", tithePhase == 2 && titheBefore >= 0f && titheAfter >= titheBefore - 0.0001f,
						$"{titheBefore * 100f:0.0}% when he left, {titheAfter * 100f:0.0}% after waiting away");
			}
			if (e is DottedDoorEvent traced)
			{
				Check("the door was traced dot to dot and inked in", traced.IsInked && traced.DotsInked == DottedDoorEvent.DotCount && traced.IsWon,
					$"{traced.DotsInked} of {DottedDoorEvent.DotCount} dots, {traceWalked:0} px walked for {DottedDoorEvent.PathLength:0} px of line, "
					+ $"begun {traceBegunAt:0.0} s and inked {traceInkedAt:0.0} s after arriving; {traced.HouseholdCalled} of the household came in at its doors");
				if (stepOffDoor)
				{
					Check("stepped off: the ink waited where it was", traceHeld >= 0f && Mathf.Abs(traceAfterWait - traceHeld) < 0.01f && traced.StepOffs >= 1,
						$"held at {traceHeld:0} px, {traceAfterWait:0} px after {StepOffWait:0} s off the line; {traced.StepOffs} step(s) off, {traced.SecondsOffLine:0.0} s off it");
					Check("stepped off: picked up where it paused and finished, nothing reset", traced.IsInked && traceLowest >= traceHeld - 0.01f,
						$"the ink never fell below {traceLowest:0} px after the pause");
				}
			}
			if (e is FivePointedSealEvent seal)
			{
				Check("every stone faces the seal", seal.Aligned == FivePointedSealEvent.StoneCount, $"{seal.Steps} presses");
				// The probe presses only plates an answer needs, so its count is the fewest the start took.
				Check("the seal took at least its minimum of presses", seal.Steps >= FivePointedSealEvent.MinPresses,
					$"{seal.Steps}, minimum {FivePointedSealEvent.MinPresses}; {sealAlignedAtStart} of 4 facing at the start");
			}
			// A wizard the site pays is free now - in memory: a tool scene never writes the save.
			SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
			if (won && e != null && !e.PaysChest && save != null)
			{
				foreach (UnlockDefinition wizard in UnlockCatalog.ForDiscoverySite(e.SiteId).Where(u => u.Kind == UnlockKind.Character))
					Check($"'{wizard.Id}' is freed, in memory", GlobalStatsManager.IsCharacterUnlocked(save, wizard.Id),
						$"writes {(SaveManager.WritesDisabled ? "off" : "ON")}");
				// Two events: whatever else the site pays is the save's now too - per site.
				if (Multi)
				{
					foreach (UnlockDefinition found in UnlockCatalog.ForDiscoverySite(e.SiteId).Where(u => u.Kind != UnlockKind.Character))
						Check($"{Of(cur)}{found.Id} is the save's, in memory", found.Kind == UnlockKind.Boon
							? GlobalStatsManager.IsBoonUnlocked(save, found.Id) : GlobalStatsManager.IsSpellUnlockedForLevelUp(save, found.Id));
				}
			}
			// Recorded by its site on a first win; a chest win records nothing (SideEvent.Succeed pays the chest without
			// DiscoveryRewards, as it always has), so that path is not asked.
			if (Multi && e != null && save != null && !e.PaysChest)
				Check($"{Of(cur)}its own deed is recorded, by its site", save.Lifetime.HasWonEvent(e.SiteId)
					&& GlobalStatsManager.IsAchievementUnlocked(save, AchievementDefinitions.EventDeedId(e.SiteId)), e.SiteId);
			if (Multi && director != null && cur < director.Events.Count - 1)
			{
				// On to the next event: with the wayfinder still to take, the hints are heard again first.
				completeClock = 0f;
				hintClock = 0f;
				if (wayfinderFirst)
				{
					cur++;
					step = 6;
				}
				else
					step = 21;
				return;
			}
			if (Multi)
				Check("every one of the chapter's events was won", wonEvents.All(w => w), $"{wonEvents.Count(w => w)} of {wonEvents.Count}");
			Finish(Multi ? "every event over" : "event over");
			return;
		}
		if (completeClock > (e is SeersVoiceEvent ? 480f : 240f))
		{
			if (e is TitheScaleEvent stuck)
				notes.Add($"the tithe at 240 s: pan {stuck.Paid:0} of {stuck.Price:0} ({stuck.Progress * 100f:0}%), {stuck.OrbsTaken} orbs weighed, "
					+ $"{GranaryTithe.OrbsInFlight} in flight, open {GranaryTithe.Open}; {EnemyRegistry.Count} enemies, "
					+ $"{EnemyRegistry.Snapshot.Count(x => IsInstanceValid(x) && x.GlobalPosition.DistanceTo(stuck.ScaleFoot) < TitheScaleEvent.TitheReach)} within its reach; "
					+ $"he stands {player.GlobalPosition.DistanceTo(stuck.ScaleFoot):0} px from it");
			Check($"{Of(cur)}the event can be finished", false, "not finished after 240 s");
			Finish("gave up");
			return;
		}

		// The arrow under the timer is the event that speaks there; with two running it may be the other's, so
		// drive this one by its own.
		Vector2? target = e.HoldsObjectiveLine ? PointsOfInterest.ObjectiveTarget : e.OwnObjectiveTarget;
		// The coupled seal points at nothing once he is in the courtyard (an arrow on a plate would solve it for
		// him): the probe works the answer out itself and presses a plate it needs.
		if (e is FivePointedSealEvent puzzle && puzzle.IsFound)
		{
			if (sealAlignedAtStart < 0)
				sealAlignedAtStart = puzzle.Aligned;
			int next = puzzle.NextPress();
			if (next >= 0)
				target = puzzle.PlateAt(next);
		}
		// The great anvil: bait. Stand on the first stake still holding; once a hammer near it winds up, step out
		// of its ring on the far side of the stake (GreatAnvilEvent.HammerSlamRadius), and come back once it lands.
		if (e is GreatAnvilEvent forge2 && forge2.IsFound)
		{
			// A strong loadout melting them on the way in: the first --kill-hammers=N that come near die at once.
			if (hammersKilled < killHammers)
			{
				foreach (SlammerEnemy hammer in forge2.Hammers)
				{
					if (!IsInstanceValid(hammer) || hammer.IsDying || hammer.GlobalPosition.DistanceTo(player.GlobalPosition) > 260f)
						continue;
					hammer.TakeDamage(hammer.Health + 1, false);
					hammersKilled++;
					break;
				}
			}
			for (int i = 0; i < GreatAnvilEvent.StakeCount; i++)
			{
				if (forge2.IsBroken(i))
					continue;
				Vector2 stake = forge2.StakeAt(i);
				target = stake;
				foreach (SlammerEnemy hammer in forge2.Hammers)
				{
					if (!IsInstanceValid(hammer) || !hammer.IsWindingUp
						|| hammer.GlobalPosition.DistanceTo(stake) > GreatAnvilEvent.HammerSlamRadius + 40f)
						continue;
					Vector2 away = stake - hammer.GlobalPosition;
					target = stake + (away.LengthSquared() > 1f ? away.Normalized() : Vector2.Down) * (GreatAnvilEvent.HammerSlamRadius + 70f);
					break;
				}
				break;
			}
		}
		// The nine mouths: wait in front of the cliff for a call, then stand in the mouth that spoke first. With
		// --wrong-mouth, first stand in one that did not, until it answers.
		if (e is NineMouthsEvent mouths && mouths.IsFound)
		{
			int heard = mouths.LastCallFirst;
			if (heard < 0)
				target = mouths.ListeningPoint;
			else if (wrongMouth && mouths.WrongAnswers == 0)
			{
				if (mouthWrongFirst < 0)
				{
					mouthWrongFirst = (heard + 3) % NineMouthsEvent.MouthCount;
					mouthTrueBefore = mouths.TrueMouth;
				}
				target = mouths.StandPoint(mouthWrongFirst);
			}
			else
			{
				if (wrongMouth && mouthTrueAfter < 0)
					mouthTrueAfter = mouths.TrueMouth;
				target = mouths.StandPoint(heard);
			}
		}
		// The walking hut: hunted at his own walk (HuntStep), and stood still while it kneels.
		if (e is WalkingHutEvent hunt && hunt.IsFound)
			target = hunt.IsTired ? player.GlobalPosition : HuntStep(hunt, delta);
		// The gibbet's cage: the winch held, then led to the ferry at his own walk (EscortStep).
		bool clearSwarm = true;
		if (e is GibbetCageEvent escort && escort.IsFound)
			target = EscortStep(escort, delta, out clearSwarm);
		// The seer: listened for, then walked to through the halls (SeekStep).
		if (e is SeersVoiceEvent sought && sought.IsFound)
			target = SeekStep(sought, delta);
		// The dry tomb: kindled at his own walk (KindleStep), and stood still while it opens.
		if (e is TombBeaconsEvent beacons && beacons.IsFound)
			target = beacons.IsOpening ? player.GlobalPosition : KindleStep(beacons, delta);
		// The Tithe-Reeve's scale: stand in the yard beside it and let the field come; what dies there pays. With
		// --tithe-walk-off, part-way through: 1,500 px off, a wait, and back.
		bool tithing = e is TitheScaleEvent titheRun && titheRun.IsFound && !titheRun.IsPaid;
		if (e is TitheScaleEvent scaleEvent && scaleEvent.IsFound)
		{
			target = scaleEvent.ScaleFoot + new Vector2(-30f, 60f);
			if (completeClock >= titheLogNext || Time.GetTicksMsec() >= titheLogReal)
			{
				titheLogNext = completeClock + 20f;
				titheLogReal = Time.GetTicksMsec() + 15000;
				GD.Print($"EXPLOREPROBE tithe at {completeClock:0}s: pan {scaleEvent.Paid:0}/{scaleEvent.Price:0}, {scaleEvent.OrbsTaken} orbs, "
					+ $"{GranaryTithe.OrbsInFlight} in flight, {EnemyRegistry.Count} enemies, {GetTree().GetNodesInGroup(XPOrb.GroupName).Count} orbs in the world, "
					+ $"frame {delta * 1000f:0} ms of game time, {Engine.GetFramesPerSecond():0} fps");
			}
			if (titheWalkOff && tithePhase == 0 && scaleEvent.Progress >= 0.4f)
			{
				tithePhase = 1;
				titheBefore = scaleEvent.Progress;
				titheClock = 0f;
			}
			if (tithePhase == 1)
			{
				target = scaleEvent.ScaleFoot + new Vector2(1500f, 0f);
				tithing = false;
				titheClock += delta;
				if (titheClock >= 5f)
				{
					titheAfter = scaleEvent.Progress;
					GD.Print($"EXPLOREPROBE walked off the scale for {titheClock:0.0}s: pan {titheBefore * 100f:0.0}% -> {titheAfter * 100f:0.0}%; walking back");
					tithePhase = 2;
				}
			}
		}
		// The dotted door: traced, dot to dot, at his own walk (TraceStep).
		if (e is DottedDoorEvent tracing && tracing.IsFound)
			target = TraceStep(tracing, delta);
		if (target == null)
		{
			float best = float.MaxValue;
			foreach (PointOfInterest site in e.Sites)
			{
				if (site.Done) continue;
				float d = site.Position.DistanceSquaredTo(player.GlobalPosition);
				if (d < best) { best = d; target = site.Position; }
			}
		}
		if (target is not Vector2 at)
			return;
		// The seal turns a stone when he STEPS onto its plate, not while he stands there: step on and off,
		// a few frames each, so every visit is one step (FivePointedSealEvent.RearmRadius).
		if (e is FivePointedSealEvent && (sealFrames++ / 3) % 2 == 1)
			at += new Vector2(0f, FivePointedSealEvent.RearmRadius + 40f);
		player.GlobalPosition = at;
		player.Velocity = Vector2.Zero;
		foreach (Node n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy enemy || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			float d = enemy.GlobalPosition.DistanceTo(at);
			if (enemy.IsMiniBoss && d < 140f && slowBleed)
			{
				// The bleed is the only damage that counts: whatever the player's own spells took since
				// the last frame is given back first, or a strong opening loadout kills the elite before
				// its phase. The bleed itself goes through TakeDamage, so a ward still cuts it.
				if (enemy != bleedTarget)
				{
					bleedTarget = enemy;
					bleedFloor = enemy.Health;
				}
				enemy.Health = Math.Max(enemy.Health, bleedFloor);
				bleedCarry += enemy.MaxHealth * SlowBleedPerSecond * delta;
				int whole = Mathf.FloorToInt(bleedCarry);
				if (whole > 0)
				{
					bleedCarry -= whole;
					enemy.TakeDamage(whole, false);
				}
				bleedFloor = enemy.Health;
			}
			else if (enemy.IsMiniBoss && d < 140f && e is not GreatAnvilEvent)
				enemy.TakeDamage(Math.Max(1, enemy.Health / 6 + 50), false);
			// At the scale they are killed, not cleared: a kill near it drops the orb that pays the tithe.
			else if (tithing && !enemy.IsMiniBoss && e is TitheScaleEvent paying
				&& enemy.GlobalPosition.DistanceTo(paying.ScaleFoot) < TitheScaleEvent.TitheReach - 30f)
				enemy.TakeDamage(enemy.Health + 1, false);
			else if (!enemy.IsMiniBoss && d < 500f && e is not ThawEvent && clearSwarm && !tithing)
				enemy.QueueFree();
		}
	}

	// One step of the escort. While the cage hangs: stand at the winch. While he walks: LEAD him round the rot - a way
	// planned from where the probe stands, once per walk, through the gaps (RotPoolLayout.FindRoute), walked at the
	// player's own walking speed, waiting while he is more than a few strides behind; the swarm cleared only every few
	// seconds. --straight, his first walk: straight at the landing (StraightStep). --let-him-fall, his first walk: into
	// the largest pool near him, and a small ring walked in it until the rot has him.
	private Vector2 EscortStep(GibbetCageEvent escort, float delta, out bool clear)
	{
		clear = false;
		Vector2 me = player.GlobalPosition;
		if (escort.IsSinking)
		{
			clear = true;
			return me;
		}
		if (escort.IsHanging && escort.Falls > 0)
			backInCageAfterFall = true;
		if (escort.IsHanging)
			return escort.WinchPoint + new Vector2(0f, 8f);
		if (!escort.IsWalking)
			return me;
		// A new walk: plan again from here.
		if (escort.Releases != escortReleasesSeen)
		{
			escortReleasesSeen = escort.Releases;
			escortRoute = null;
		}
		foreach (Enemy body in EnemyRegistry.Snapshot)
		{
			if (body.GlobalPosition.DistanceSquaredTo(escort.HimAt) < 46f * 46f)
			{
				closeFrames++;
				break;
			}
		}
		escortClearClock += delta;
		if (escortClearClock >= 5f)
		{
			escortClearClock = 0f;
			clear = true;
		}
		if (me.DistanceTo(escort.HimAt) > 150f)
		{
			escortWaits++;
			return me;
		}
		float step = player.WalkingSpeed * delta;
		if (letHimFall && escort.Falls == 0)
			return IntoTheRot(escort, me, step);
		if (straightThrough && escort.Releases == 1)
		{
			straightBites = escort.RotTicks;
			return StraightStep(escort, me, step);
		}
		if (escortRoute == null)
		{
			escortRoute = RotPoolLayout.FindRoute(escort.Rot?.Pools ?? Array.Empty<RotPoolLayout.Pool>(), me,
				escort.Landing + new Vector2(0f, -40f), p => !game.IsGroundBlockedAt(p), RotPoolLayout.RouteClearance);
			escortLeg = 1;
			float length = 0f;
			for (int i = 1; escortRoute != null && i < escortRoute.Length; i++)
				length += escortRoute[i - 1].DistanceTo(escortRoute[i]);
			GD.Print(escortRoute == null
				? "EXPLOREPROBE escort: no way round the rot from here - walking straight"
				: $"EXPLOREPROBE escort: planned a way round the rot from ({me.X:0},{me.Y:0}), {escortRoute.Length - 1} legs, {length:0} px "
					+ $"(straight: {me.DistanceTo(escort.Landing):0} px)");
		}
		if (escortRoute == null)
			return StraightStep(escort, me, step);
		while (escortLeg < escortRoute.Length && me.DistanceTo(escortRoute[escortLeg]) <= 4f)
			escortLeg++;
		Vector2 goal = escortLeg < escortRoute.Length ? escortRoute[escortLeg] : escort.Landing + new Vector2(0f, -40f);
		return me.MoveToward(goal, step);
	}

	// --let-him-fall: to the largest pool near him (the nearest of the largest), and once at its middle a small ring
	// walked round it, so the steps he follows stay in the rot until it has all of him.
	private Vector2 IntoTheRot(GibbetCageEvent escort, Vector2 me, float step)
	{
		RotPoolLayout rot = escort.Rot;
		if (rot == null || rot.Pools.Count == 0)
			return me;
		if (fallPool < 0)
		{
			fallPool = 0;
			for (int i = 1; i < rot.Pools.Count; i++)
			{
				RotPoolLayout.Pool p = rot.Pools[i], best = rot.Pools[fallPool];
				if (p.Radius > best.Radius
					|| (p.Radius == best.Radius && p.Centre.DistanceTo(escort.HimAt) < best.Centre.DistanceTo(escort.HimAt)))
					fallPool = i;
			}
			GD.Print($"EXPLOREPROBE let him fall: leading him into the rot at ({rot.Pools[fallPool].Centre.X:0},{rot.Pools[fallPool].Centre.Y:0}), "
				+ $"{rot.Pools[fallPool].Radius * 2f:0} px across");
		}
		Vector2 centre = rot.Pools[fallPool].Centre;
		if (me.DistanceTo(centre) > 44f && fallRing == 0f)
			return me.MoveToward(centre, step);
		fallRing += step / 40f;
		return me.MoveToward(centre + Vector2.FromAngle(fallRing) * 40f, step);
	}

	// Straight at the landing at his own walk, round water: the old escort's walk, through whatever rot is on the line.
	private Vector2 StraightStep(GibbetCageEvent escort, Vector2 me, float step)
	{
		Vector2 toLanding = escort.Landing + new Vector2(0f, -40f) - me;
		float d = toLanding.Length();
		if (d <= 4f)
			return me;
		// Walked as a player walks: round water and walls, not through them - his feet test the ground a stride ahead,
		// and turn by degrees either way from the straight line until it is open. A straight line over a lake would
		// put the player (and so the man following his steps) where no player can stand, and the game's own placement
		// then puts nothing near him.
		Vector2 dir = toLanding / d;
		Vector2 feet = player.FootPosition - player.GlobalPosition;
		for (int k = 0; k <= 12; k++)
		{
			float turn = (k % 2 == 1 ? 1f : -1f) * Mathf.DegToRad(15f * ((k + 1) / 2));
			Vector2 tryDir = dir.Rotated(k == 0 ? escortTurn : escortTurn + turn);
			Vector2 ahead = me + feet + tryDir * 40f;
			if (!game.IsGroundBlockedAt(ahead) && !game.IsGroundBlockedAt(me + feet + tryDir * step))
			{
				if (k > 0)
					escortDetours++;
				escortTurn = Mathf.Wrap(escortTurn + (k == 0 ? 0f : turn), -Mathf.Pi, Mathf.Pi) * 0.92f;
				return me + tryDir * Mathf.Min(step, d - 4f);
			}
		}
		escortTurn *= 0.5f;
		return me + dir * Mathf.Min(step, d - 4f);
	}

	private float escortTurn;
	private int escortDetours;

	// One step of the hunt, at the player's own walking speed: to just inside its startle radius on the line to it,
	// so it bolts whenever it can. With --lose-hut, part-way through: straight away past its lose radius, a wait
	// there, and back.
	private Vector2 HuntStep(WalkingHutEvent hunt, float delta)
	{
		Vector2 me = player.GlobalPosition;
		Vector2 toHut = hunt.HutPosition - me;
		float d = toHut.Length();
		float step = player.WalkingSpeed * delta;
		if (loseHut && lostPhase == 0 && hunt.Progress >= 0.4f)
			lostPhase = 1;
		if (lostPhase == 1)
		{
			if (d <= WalkingHutEvent.KeepRadius)
				return d > 1f ? me - toHut / d * step : me + Vector2.Down * step;
			if (progressOutside < 0f)
				progressOutside = hunt.Progress;
			if (d < WalkingHutEvent.LoseRadius + 400f)
				return me - toHut / d * step;
			lostClock += delta;
			if (lostClock >= WalkingHutEvent.CalmSeconds + 2f)
			{
				progressAfterWait = hunt.Progress;
				GD.Print($"EXPLOREPROBE lost the hut at {d:0} px for {lostClock:0.0}s: bar {progressOutside * 100f:0.0}% -> {progressAfterWait * 100f:0.0}%, "
					+ $"fleeing {hunt.IsFleeing}, calmed {hunt.TimesCalmed}; walking back");
				lostPhase = 2;
			}
			return me;
		}
		const float Want = WalkingHutEvent.StartleRadius - 40f;
		return d > Want ? me + toHut / d * Mathf.Min(step, d - Want) : me;
	}

	// --- the dotted door: traced ----------------------------------------------------------------------

	private bool stepOffDoor;
	private int traceOffPhase;
	private Vector2 traceOffPoint;
	private float traceOffClock;
	private float traceHeld = -1f;
	private float traceAfterWait = -1f;
	private float traceLowest = float.MaxValue;
	private float traceWalked;
	private float traceClock;
	private float traceBegunAt = -1f;
	private float traceInkedAt = -1f;
	private const float StepOffWait = 3f;

	// One step of the trace at his own walking speed, by the feet: to dot 1, then along the line a stride ahead of the ink.
	// With --step-off, once two fifths is inked: off the line, a wait, and back to where the ink ends.
	private Vector2 TraceStep(DottedDoorEvent door, float delta)
	{
		traceClock += delta;
		if (door.TraceBegun && traceBegunAt < 0f)
			traceBegunAt = traceClock;
		if (door.IsInked)
		{
			if (traceInkedAt < 0f)
				traceInkedAt = traceClock;
			return player.GlobalPosition;            // stand while it is painted and he steps out
		}
		if (traceOffPhase >= 3)
			traceLowest = Mathf.Min(traceLowest, door.Pen);
		Vector2 feetOffset = player.FootPosition - player.GlobalPosition;
		Vector2 feet = player.FootPosition;
		Vector2 goal;
		if (!door.TraceBegun)
			goal = door.DotWorld(0);
		else
		{
			if (stepOffDoor && traceOffPhase == 0 && door.Pen >= DottedDoorEvent.PathLength * 0.4f)
			{
				traceOffPhase = 1;
				traceHeld = door.Pen;
				// Into the door's middle, over open boards: the door stands 64 px off the end wall, and a step off outward
				// from its jamb or its arch would be into the wall, which the body cannot stand in.
				Vector2 inward = door.DoorCentre + new Vector2(0f, 40f) - door.PenWorld;
				inward = inward.LengthSquared() > 1f ? inward.Normalized() : Vector2.Right;
				traceOffPoint = door.PenWorld + inward * 130f;
				GD.Print($"EXPLOREPROBE trace: stepping off the line at {door.Pen:0} px ({door.DotsInked} dots), to ({traceOffPoint.X:0},{traceOffPoint.Y:0})");
			}
			if (traceOffPhase == 1 || traceOffPhase == 2)
			{
				goal = traceOffPoint;
				if (traceOffPhase == 1 && feet.DistanceTo(traceOffPoint) < 2f)
				{
					traceOffPhase = 2;
					traceOffClock = 0f;
				}
				if (traceOffPhase == 2 && (traceOffClock += delta) >= StepOffWait)
				{
					traceAfterWait = door.Pen;
					traceOffPhase = 3;
					GD.Print($"EXPLOREPROBE trace: {StepOffWait:0} s off the line, the ink at {door.Pen:0} px (held at {traceHeld:0}); back to it");
				}
			}
			else
				goal = door.DoorCentre + DottedDoorEvent.PathPoint(Mathf.Min(DottedDoorEvent.PathLength, door.Pen + 24f));
		}
		float step = player.WalkingSpeed * delta;
		Vector2 next = feet.MoveToward(goal, step);
		traceWalked += feet.DistanceTo(next);
		return next - feetOffset;
	}

	// --- the seer: sought through the house ------------------------------------------------------------

	private CourtLayout courtFloor;
	private bool courtFloorAsked;

	// The Court's building, when this run is in it; null anywhere else.
	private CourtLayout CourtFloor
	{
		get
		{
			if (!courtFloorAsked)
			{
				courtFloorAsked = true;
				courtFloor = (game.Rules as CourtRules)?.Layout;
			}
			return courtFloor;
		}
	}

	private int seekLeg = -1;
	private int seekReadingsAtLeg;
	private float seekLegClock;
	private float seekHeardAt;
	private List<Vector2> seekPath;
	private int seekIndex;
	private float seekWalked;
	private float seekStraight;
	private int seekLegsHeard;
	private readonly List<string> seekLog = new();

	// One step of the seek at his own walking speed. A new leg (a room found empty): stand and listen. The voice reads:
	// the shortest way through the halls to where it came from, walked cell by cell - the straight line the sound
	// promised is what the walls bend. The feet walk the path (Player.FootPosition), as every Court reader of a cell does.
	private Vector2 SeekStep(SeersVoiceEvent seer, float delta)
	{
		if (seer.Leg != seekLeg)
		{
			if (seekLeg >= 1 && seekPath != null)
				seekLog.Add($"seek leg {seekLeg}: heard {seekHeardAt:0.0} s after the last room, walked {seekWalked:0} px through the halls "
					+ $"against {seekStraight:0} px straight (x{seekWalked / Mathf.Max(1f, seekStraight):0.00}), there {seekLegClock:0.0} s after it was found empty");
			seekLeg = seer.Leg;
			seekReadingsAtLeg = seer.ReadingsHeard;
			seekLegClock = 0f;
			seekPath = null;
		}
		seekLegClock += delta;
		if (seer.CurrentSpot is not Vector2 || CourtFloor == null)
			return player.GlobalPosition;
		if (seer.ReadingsHeard == seekReadingsAtLeg)
			return player.GlobalPosition;            // listening: nothing has read since the room was found empty
		Vector2 feetOffset = player.FootPosition - player.GlobalPosition;
		if (seekPath == null)
		{
			Vector2 from = seer.LastReadingFrom ?? seer.CurrentSpot.Value;
			seekHeardAt = seekLegClock;
			seekPath = CourtPath(player.FootPosition, from);
			seekIndex = 0;
			seekWalked = 0f;
			seekStraight = player.FootPosition.DistanceTo(from);
			seekLegsHeard++;
			GD.Print($"EXPLOREPROBE seek: the voice reads from {seer.ReadingRooms[Mathf.Min(seer.Leg, seer.ReadingRooms.Count - 1)]}, "
				+ $"{seekStraight:0} px straight; the halls' way is {seekPath.Count} cells");
		}
		float step = player.WalkingSpeed * delta;
		Vector2 feet = player.FootPosition;
		while (step > 0f && seekIndex < seekPath.Count)
		{
			float d = feet.DistanceTo(seekPath[seekIndex]);
			if (d <= step)
			{
				feet = seekPath[seekIndex++];
				step -= d;
				seekWalked += d;
			}
			else
			{
				feet = feet.MoveToward(seekPath[seekIndex], step);
				seekWalked += step;
				step = 0f;
			}
		}
		return feet - feetOffset;
	}

	// The shortest way between two points of the building's floor, by its cells (4-way), as world points: each cell's
	// centre, the goal itself last. Empty if there is none.
	private List<Vector2> CourtPath(Vector2 from, Vector2 to)
	{
		AuthoredLayout grid = CourtFloor.Grid;
		Vector2I a = CourtFloor.WorldToCell(from), b = CourtFloor.WorldToCell(to);
		var came = new Dictionary<Vector2I, Vector2I> { [a] = a };
		var queue = new Queue<Vector2I>();
		queue.Enqueue(a);
		Vector2I[] steps = { Vector2I.Right, Vector2I.Left, Vector2I.Down, Vector2I.Up };
		while (queue.Count > 0 && !came.ContainsKey(b))
		{
			Vector2I c = queue.Dequeue();
			foreach (Vector2I st in steps)
			{
				Vector2I n = c + st;
				if (came.ContainsKey(n) || !grid.IsWalkable(n))
					continue;
				came[n] = c;
				queue.Enqueue(n);
			}
		}
		var path = new List<Vector2>();
		if (!came.ContainsKey(b))
			return path;
		for (Vector2I c = b; c != a; c = came[c])
			path.Add(CourtFloor.CellToWorld(new Vector2(c.X + 0.5f, c.Y + 0.5f)));
		path.Reverse();
		path.Add(to);
		return path;
	}

	// One step of the kindling, at the player's own walking speed: to the nearest cold beacon, into its reach, and held
	// there until it catches. With --let-beacon-die, once the first has caught: to the tomb (404 px from every beacon,
	// outside every reach) until the wind has put that one out, and then on.
	private Vector2 KindleStep(TombBeaconsEvent tomb, float delta)
	{
		Vector2 me = player.GlobalPosition;
		float step = player.WalkingSpeed * delta;
		if (letBeaconDie && beaconPhase == 0 && tomb.TimesLit >= 1)
			beaconPhase = 1;
		if (beaconPhase == 1)
		{
			if (tomb.TimesWentOut >= 1)
			{
				beaconWentOutAt = completeClock;
				beaconWentCold = tomb.LitCount == 0;
				GD.Print($"EXPLOREPROBE a beacon went out on the wind {completeClock:0.0}s after arriving; lit now: {tomb.LitCount}; lighting all three");
				beaconPhase = 2;
			}
			else
				return me.MoveToward(tomb.TombPosition, step);
		}
		int best = -1;
		float bestD = float.MaxValue;
		for (int i = 0; i < TombBeaconsEvent.BeaconCount; i++)
		{
			if (tomb.IsLit(i))
				continue;
			float d = tomb.BeaconAt(i).DistanceTo(me);
			if (d < bestD)
			{
				bestD = d;
				best = i;
			}
		}
		if (best < 0)
			return me;
		// Into the reach on the side he comes from, short of the pillar, and stand there.
		Vector2 foot = tomb.BeaconAt(best);
		Vector2 from = (me - foot).LengthSquared() > 1f ? (me - foot).Normalized() : Vector2.Down;
		Vector2 stand = foot + from * (TombBeaconsEvent.KindleRadius * 0.55f);
		return me.MoveToward(stand, step);
	}

	// Everything the streamer furnished within radius of a point: children of the chunks' prop
	// holders (Node2DGame names them Props_x_y), sorted so two visits compare in order.
	private List<Vector2> PropsNear(Vector2 centre, float radius)
	{
		var found = new List<Vector2>();
		foreach (Node child in game.GetChildren())
		{
			if (!child.Name.ToString().StartsWith("Props_"))
				continue;
			foreach (Node prop in child.GetChildren())
			{
				if (prop is Node2D p2 && p2 is not Landmark && p2.GlobalPosition.DistanceTo(centre) <= radius)
				{
					found.Add(p2.GlobalPosition);
					if (logProps)
						GD.Print($"EXPLOREPROBE prop {prop.Name} ({prop.GetType().Name}) at {p2.GlobalPosition} under {child.Name}");
				}
			}
		}
		found.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
		return found;
	}

	private void AutoPickLevelUp()
	{
		// Found again once the last one is freed: each level-up is a new screen, and holding the first one's freed
		// reference left the second open for ever, the tree paused (a long wait, --arrive-late, met it).
		if (levelUpMenu == null || !IsInstanceValid(levelUpMenu))
			levelUpMenu = FindNode<LevelUpMenu>(game);
		LevelUpMenu menu = levelUpMenu;
		if (menu == null || !IsInstanceValid(menu) || !menu.Visible)
			return;
		IReadOnlyList<LevelUpOption> options = menu.GetOfferedOptions();
		if (options == null || options.Count == 0)
			return;
		LevelUpOption pick = options.FirstOrDefault(o => o.IsNewUnlock) ?? options.OrderBy(o => o.NextLevel).First();
		menu.EmitSignal(LevelUpMenu.SignalName.WeaponSelected, pick.AutoChoice());
	}

	private static T FindNode<T>(Node root) where T : Node
	{
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

	private void Finish(string why)
	{
		finished = true;
		GD.Print("");
		GD.Print($"EXPLOREPROBE RESULT stage {StageIndex} ({StageCatalog.GetByIndex(StageIndex)?.DisplayName}) seed {Seed}: "
			+ $"{passes} passed, {fails} failed - {why}");
		foreach (string note in notes)
			GD.Print($"  note: {note}");
		if (fails > 0)
			GD.PushError($"EXPLOREPROBE stage {StageIndex}: {fails} check(s) failed.");
		GetTree().Quit(fails > 0 ? 1 : 0);
	}
}
