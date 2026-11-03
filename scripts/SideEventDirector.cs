using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

/// <summary>One side event a chapter holds: the Discovery site it pays out, and how to make it.</summary>
public readonly record struct SideEventEntry(string SiteId, Func<SideEvent> Create);

/// <summary>
/// A chapter's line in <see cref="SideEventDirector"/>'s table: the wayfinder it hides near the start (one a
/// chapter, whatever it marks), and its side events in a fixed order - the first the chapter's original one.
/// </summary>
public sealed class ChapterEvents
{
	public ChapterEvents(WayfinderKind wayfinder, params SideEventEntry[] events)
	{
		Wayfinder = wayfinder;
		Events = events ?? Array.Empty<SideEventEntry>();
	}

	public WayfinderKind Wayfinder { get; }
	public IReadOnlyList<SideEventEntry> Events { get; }
}

// Places the chapter's side events and its wayfinder at stage start (.ai/side-events.md).
//
// The table below is the whole mapping. Each entry is a chapter, the events that live there - each with
// the Discovery site it pays out - and the wayfinder that reveals them; UnlockCatalog says what a site
// gives. A chapter that is not listed has no event - and so no wayfinder, since there would be nothing
// for it to point at - which is true of three of the fourteen today: the Sketchbook, which holds nothing,
// and the Court and the Throne, whose null lines are already here waiting.
//
// TWO EVENTS IN ONE CHAPTER (P2, .ai/chapter-wizards.md 6.3). Five chapter wizards are held in chapters
// that already had an event, so a chapter's line is a LIST: the first is the chapter's original event,
// laid out exactly as a one-event chapter always was (stream 1); each one after it on its own stream, in
// the same 4-6 chunk band, at least MinSiteSeparationDegrees round from every earlier event's main site
// (ExplorePlacer.Avoiding). The one wayfinder marks every site of every event. The hints alternate them,
// one a slot (StageHintDirector). Everything downstream is per site: a site's deed, its reward on the map
// card, its clue in the chapter note, LifetimeStats.EventsWon.
//
// NO TIMER (2026-10-22). This used to start the event at 90 pace seconds, near wherever the player
// was. Now, the moment the run's terrain exists, it lays the event out in the world from the run's
// terrain seed - four to six chunks from the start, on open ground (ExplorePlacer) - and leaves it
// there, dormant, for the player to find. The event begins itself when he arrives (SideEvent). The
// first-minute grace the timer gave ("an objective arriving before they have a build is a demand they
// cannot yet meet") now comes from distance: the event is half a minute's walk away at the least, and
// going there is the player's choice.
//
// THE WAYFINDER. Each event chapter also hides one object much nearer the start, 1.5 to 2.5 chunks
// out on a bearing at least 75 degrees from every event's: take it and every point of interest on the
// map gets an arrow at the edge of the screen for the rest of the run (PointsOfInterest, PoiPointer).
// It is the Arcane Compass by default; the Cursed Dungeon has the Gaoler's own map nailed to a post,
// and the Frozen Waste a cairn with a bearing plate (WayfinderPickup).
public partial class SideEventDirector : Node
{
	public Node2DGame Game { get; set; }

	/// <summary>This run's events, placed and waiting, in the chapter's order; empty on a chapter without one.</summary>
	public IReadOnlyList<SideEvent> Events => events;

	/// <summary>The chapter's first event (its original one); null on a chapter without one.</summary>
	public SideEvent Event => events.Count > 0 ? events[0] : null;

	/// <summary>This run's wayfinder; null on a chapter without an event, and freed once taken.</summary>
	public WayfinderPickup Wayfinder { get; private set; }

	/// <summary>This run's chapter hints (StageHintDirector), alternating its events; null when none has one.</summary>
	public StageHintDirector Hints { get; private set; }

	private readonly List<SideEvent> events = new();

	// The bands, in terrain chunks (1536 px) from the start. At the wizard's 220 px/s a chunk is
	// seven seconds of straight walking; the swarm and the ground make it more (.ai/side-events.md
	// gives the measured walks). Near enough that the trip is a real part of a 15-minute run, far
	// enough that nobody falls over the event without going looking.
	public const float EventMinChunks = 4f;
	public const float EventMaxChunks = 6f;
	public const float WayfinderMinChunks = 1.5f;
	public const float WayfinderMaxChunks = 2.5f;
	// So the wayfinder never sits on the way to an event: its arrow should send the player
	// somewhere he was not already going.
	public const float WayfinderMinSeparationDegrees = 75f;
	// A chapter's second event is at least this far round from its first (measured at the start, between
	// main sites): two events on one bearing would be one trip, and one hint's direction would be the other's.
	public const float MinSiteSeparationDegrees = 90f;

	/// <summary>
	/// The placer stream of a chapter's event <paramref name="index"/>: the first keeps stream 1, so every
	/// chapter's original event is placed exactly where it always was; the later ones take 3 to 6 (2 is the
	/// wayfinder's, 7 to 9 the secrets').
	/// </summary>
	public static int EventStream(int index) => index == 0 ? 1 : 2 + index;

	/// <summary>The most events one chapter may hold: the free streams.</summary>
	public const int MaxEventsPerChapter = 5;

	// THE TABLE. A second event goes on its chapter's line, after the first:
	//   "stage_0" => new(WayfinderKind.Compass, new("the_grove", () => new GroveEvent()), new("my_site", () => new MyEvent())),
	private static ChapterEvents Chapter(string stageId) => stageId switch
	{
		// The Forest's chapter wizard (.ai/chapter-wizards.md 4.1), the first second event: Baba Yaga, kept in her own
		// hut and the hut kept walking.
		"stage_0" => new(WayfinderKind.Compass, new SideEventEntry("the_grove", () => new GroveEvent()),
			new SideEventEntry("walking_hut", () => new WalkingHutEvent())),
		"stage_1" => new(WayfinderKind.GaolersMap, new SideEventEntry("gaolers_key", () => new GaolersKeyEvent())),
		// The Sunken Cave's chapter wizard (.ai/chapter-wizards.md 4.3): the Cumaean Sibyl, behind nine mouths.
		"stage_2" => new(WayfinderKind.Compass, new SideEventEntry("nine_mouths", () => new NineMouthsEvent())),
		// The Swamp's chapter wizard (.ai/chapter-wizards.md 4.4), its second event: Paracelsus, caged over the rot and led
		// out to the ferry.
		"stage_3" => new(WayfinderKind.Compass, new SideEventEntry("bog_lanterns", () => new BogLanternsEvent()),
			new SideEventEntry("gibbet_cage", () => new GibbetCageEvent())),
		// The first chapter wizard (.ai/chapter-wizards.md 4.5): Abe no Seimei, sealed in his own star.
		"stage_4" => new(WayfinderKind.Compass, new SideEventEntry("five_pointed_seal", () => new FivePointedSealEvent())),
		"stage_5" => new(WayfinderKind.BearingCairn, new SideEventEntry("the_thaw", () => new ThawEvent())),
		"stage_6" => new(WayfinderKind.Compass, new SideEventEntry("caravan_master", () => new CaravanMasterEvent())),
		// The Emberdeep's chapter wizard (.ai/chapter-wizards.md 4.7): Wayland the Smith, chained to his own anvil.
		"stage_7" => new(WayfinderKind.Compass, new SideEventEntry("great_anvil", () => new GreatAnvilEvent())),
		// The two early chapters (.ai/campaign.md). The comment between them keeps two agents' edits off
		// adjacent lines, which is what git calls a conflict.
		"stage_9" => new(WayfinderKind.Compass, new SideEventEntry("the_well", () => new WellEvent())),
		// (the Broken Circle's line is below; leave this one as it is)
		"stage_10" => new(WayfinderKind.Compass, new SideEventEntry("the_moot", () => new MootFiresEvent())),
		// THE CASTLE (.ai/endgame-castle.md). The chapters' own goals - the bridge's crossing, the Court's rooms,
		// the Throne's fight - are their ChapterRules' (ChapterRulesCatalog), not here. The lines are written so
		// that an event designed later is one line's edit, a line apart from its neighbours. The Cursed Dungeon
		// keeps the Gaoler's key.
		// The Last Bridge's chapter wizard (.ai/chapter-wizards.md 4.9): Morgan le Fay, in her barge under the span.
		// Placed by the bridge's band (BargeEvent.PlaceSites), its wayfinder by the bridge's rules (hook 10).
		"stage_11" => new(WayfinderKind.Compass, new SideEventEntry("the_barge", () => new BargeEvent())),
		// (the Court's line is below)
		// The Court's first event (.ai/chapter-wizards.md 4.10): Nostradamus, heard reading from room to room. Its
		// rooms are the Court's own, chosen by the seed (SeersVoiceEvent.PlaceSites); Ma Liang's dotted door in the
		// Gallery will be the second event on this line (4.8), and the Gallery is kept out of the seer's rooms for it.
		"stage_12" => new(WayfinderKind.Compass, new SideEventEntry("seers_voice", () => new SeersVoiceEvent())),
		// (the Throne's line is below)
		"stage_13" => null,
		_ => null,
	};

	/// <summary>
	/// The chapter's line: its wayfinder and its events, with the stand-in second event appended on a tool
	/// run that asked for one (<see cref="SecondEventStandIn"/>; never in a real run). Null without events.
	/// </summary>
	private static ChapterEvents Line(string stageId)
	{
		ChapterEvents line = Chapter(stageId);
		if (line == null || !SecondEventStandIn.AddsTo(stageId))
			return line;
		return new ChapterEvents(line.Wayfinder, line.Events.Append(SecondEventStandIn.Entry).ToArray());
	}

	/// <summary>
	/// Every side event a chapter holds, in a fixed order - the first its original event. Empty for a
	/// chapter without one. The one authority on what a chapter holds out in the world.
	/// </summary>
	public static IReadOnlyList<SideEventEntry> EventsFor(string stageId) =>
		Line(stageId)?.Events ?? Array.Empty<SideEventEntry>();

	/// <summary>
	/// The table's own line for the chapter, never with a tool run's stand-in: for the validator, which holds
	/// it to never naming the stand-in's site (SecondEventStandIn) - that would be the stand-in leaking into play.
	/// </summary>
	public static IReadOnlyList<SideEventEntry> TableEventsFor(string stageId) =>
		Chapter(stageId)?.Events ?? Array.Empty<SideEventEntry>();

	/// <summary>The chapter's wayfinder, or null for a chapter without an event (nothing for it to mark).</summary>
	public static WayfinderKind? WayfinderFor(string stageId) =>
		Line(stageId) is ChapterEvents line && line.Events.Count > 0 ? line.Wayfinder : null;

	/// <summary>Whether <paramref name="siteId"/> is one of <paramref name="stageId"/>'s events.</summary>
	public static bool HoldsSite(string stageId, string siteId) =>
		!string.IsNullOrWhiteSpace(siteId)
		&& EventsFor(stageId).Any(e => e.SiteId.Equals(siteId, StringComparison.OrdinalIgnoreCase));

	/// <summary>Every chapter's every event, chapter by chapter in StageCatalog's order.</summary>
	public static IEnumerable<(StageDefinition Stage, SideEventEntry Entry)> AllEvents =>
		StageCatalog.All.SelectMany(stage => EventsFor(stage.Id).Select(entry => (stage, entry)));

	/// <summary>The chapters whose events stand on <paramref name="siteId"/> - exactly one, for a sound table.</summary>
	public static List<StageDefinition> ChaptersOfSite(string siteId) =>
		StageCatalog.All.Where(stage => HoldsSite(stage.Id, siteId)).ToList();

	/// <summary>The chapter whose event stands on <paramref name="siteId"/>, or null.</summary>
	public static StageDefinition ChapterOfSite(string siteId) =>
		StageCatalog.All.FirstOrDefault(stage => HoldsSite(stage.Id, siteId));

	/// <summary>
	/// Lays a chapter's events out, as the run does: event 0 on <see cref="EventStream"/>(0) exactly as a
	/// one-event chapter always has been; each later one on its own stream, kept at least
	/// <see cref="MinSiteSeparationDegrees"/> round from every earlier event's main site and off every
	/// footprint placed so far. <paramref name="placerFor"/> makes a placer for a stream. The events are
	/// made, given their site ids and placed, not added anywhere. Shared with the validator and the probe,
	/// so what they measure is what the run does.
	/// </summary>
	public static List<(SideEvent Event, ExplorePlacer Placer)> LayOut(IReadOnlyList<SideEventEntry> entries, Func<int, ExplorePlacer> placerFor)
	{
		var laid = new List<(SideEvent, ExplorePlacer)>();
		var bearings = new List<float>();
		var footprints = new List<(Vector2 Centre, float Radius)>();
		float separation = Mathf.DegToRad(MinSiteSeparationDegrees);
		for (int i = 0; i < entries.Count; i++)
		{
			ExplorePlacer placer = placerFor(EventStream(i));
			if (i > 0)
				placer.Avoiding(bearings.ToArray(), separation, footprints.ToArray());
			SideEvent sideEvent = entries[i].Create();
			sideEvent.SiteId = entries[i].SiteId;
			sideEvent.Index = i;
			sideEvent.PlaceSites(placer);
			laid.Add((sideEvent, placer));
			if (sideEvent.Sites.Count > 0)
				bearings.Add(ExplorePlacer.BearingFrom(placer.Origin, sideEvent.Sites[0].Position));
			footprints.AddRange(placer.Reserved);
		}
		return laid;
	}

	/// <summary>
	/// The bearing the wayfinder goes out on: a roll mapped onto the bearings at least
	/// <see cref="WayfinderMinSeparationDegrees"/> from every event's (ExplorePlacer.BearingAvoiding). With one
	/// event, exactly the bearing it has always had.
	/// </summary>
	public static float WayfinderBearing(float roll, IReadOnlyList<float> eventBearings) =>
		ExplorePlacer.BearingAvoiding(roll, eventBearings, Mathf.DegToRad(WayfinderMinSeparationDegrees));

	private PoiPointer poiPointer;
	private bool logPlacement;

	public override void _Ready()
	{
		PointsOfInterest.Reset();
		SideEvent.ResetObjectiveLine();
		// Nobody is being led at the start of a run, whatever the last one left (HuntedEscort's statics are the process's).
		HuntedEscort.Withdraw();
		HuntedEscort.ResetTally();
		logPlacement = OS.GetCmdlineUserArgs().Contains("--explore-log");
		if (Game == null)
			return;

		StageDefinition stage = StageCatalog.GetByIndex(Global.SelectedStageIdx);
		IReadOnlyList<SideEventEntry> entries = stage != null ? EventsFor(stage.Id) : Array.Empty<SideEventEntry>();
		if (entries.Count == 0)
		{
			if (logPlacement)
				GD.Print($"EXPLORE stage {Global.SelectedStageIdx}: no side event, no wayfinder");
			return;
		}

		var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		List<(SideEvent Event, ExplorePlacer Placer)> laid = LayOut(entries,
			stream => Game.CreateExplorePlacer(stream, EventMinChunks, EventMaxChunks));
		foreach (var (sideEvent, _) in laid)
		{
			sideEvent.Name = sideEvent.Index == 0 ? "SideEvent" : $"SideEvent{sideEvent.Index + 1}";
			sideEvent.Game = Game;
			sideEvent.PaysChest = saveManager != null && DiscoveryRewards.IsSiteExhausted(saveManager.Data, sideEvent.SiteId);
		}

		// The wayfinder, on its own stream so a change to how an event lays itself out never moves it. Kept off
		// every event's bearing - with one event, the rolled bearing it has always kept off.
		ExplorePlacer near = Game.CreateExplorePlacer(stream: 2, EventMinChunks, EventMaxChunks);
		float[] eventBearings = laid.Select(l => l.Placer.EventBearing).ToArray();
		float bearing = WayfinderBearing(near.NextFloat(), eventBearings);
		WayfinderKind kind = WayfinderFor(stage.Id) ?? WayfinderKind.Compass;
		// ChapterRules hook 10: a chapter whose ground the band cannot read (the Last Bridge, a deck over a drop)
		// says where its wayfinder goes. Every other chapter searches the band: where the first search finds
		// ground this is exactly AtBearing's spot, so a chapter that had a wayfinder keeps it where it was; where it
		// does not, the band is swept from the bearing outward rather than the wayfinder being left in the rock. The
		// Court needs it: its building fills only the quarter of the band north-east of the stair.
		Vector2 wayfinderAt = Game.Rules?.WayfinderSite(near, WayfinderPickup.Footprint)
			?? near.AtBearingInBandAvoiding(bearing, WayfinderMinChunks, WayfinderMaxChunks, WayfinderPickup.Footprint,
				eventBearings, Mathf.DegToRad(WayfinderMinSeparationDegrees));
		var site = PointsOfInterest.Add(new PointOfInterest(wayfinderAt, WayfinderPickup.GlyphFor(kind), isWayfinder: true));
		var wayfinder = new WayfinderPickup { Name = "Wayfinder", Game = Game, Kind = kind, Site = site };

		// Before the first chunk is furnished (LoadInitialTerrain runs after this), so no tree, rock
		// or spike plate is ever put down on a lantern's pool or across the caravan's road.
		foreach (var (_, placer) in laid)
			foreach (var (centre, radius) in placer.Reserved)
				Game.ReserveExploreGround(centre, radius);
		foreach (var (centre, radius) in near.Reserved)
			Game.ReserveExploreGround(centre, radius);

		foreach (var (sideEvent, _) in laid)
		{
			Game.AddChild(sideEvent);
			events.Add(sideEvent);
		}
		Game.AddChild(wayfinder);
		Wayfinder = wayfinder;

		// The chapter's hints, for each event that has one (StageHintCatalog, by site): a sound from its
		// direction partway through the run, until it is found or its discovery is owned - alternating the
		// chapter's events, one a slot, never two at once.
		var sources = new List<(SideEvent, StageHint)>();
		foreach (SideEvent sideEvent in events)
		{
			if (StageHintCatalog.ForSite(sideEvent.SiteId) is StageHint hint)
				sources.Add((sideEvent, hint));
		}
		if (sources.Count > 0)
		{
			Hints = new StageHintDirector { Name = "StageHintDirector", Game = Game, StageId = stage.Id, Sources = sources };
			AddChild(Hints);
		}

		var overlay = Game.GetNodeOrNull<CanvasLayer>("UIOverlay");
		if (overlay != null)
		{
			poiPointer = new PoiPointer { Name = "PoiPointer" };
			overlay.AddChild(poiPointer);
		}

		if (logPlacement)
		{
			Vector2 origin = Game.StageOrigin;
			GD.Print($"EXPLORE stage {Global.SelectedStageIdx} seed {Game.TerrainSeed}: wayfinder {kind} at ({wayfinderAt.X:0},{wayfinderAt.Y:0}) "
				+ $"{wayfinderAt.DistanceTo(origin):0} px, bearing {Mathf.RadToDeg(bearing) % 360f:0} deg");
			foreach (var (sideEvent, placer) in laid)
			{
				GD.Print($"EXPLORE   event {sideEvent.Index} {sideEvent.GetType().Name} at '{sideEvent.SiteId}', "
					+ $"bearing {Mathf.RadToDeg(placer.EventBearing):0} deg");
				for (int i = 0; i < sideEvent.Sites.Count; i++)
				{
					Vector2 p = sideEvent.Sites[i].Position;
					GD.Print($"EXPLORE     site {i} {sideEvent.Sites[i].Glyph} at ({p.X:0},{p.Y:0}), {p.DistanceTo(origin):0} px from the start");
				}
			}
		}
	}

	public override void _ExitTree()
	{
		if (poiPointer != null && IsInstanceValid(poiPointer))
			poiPointer.QueueFree();
		PointsOfInterest.Reset();
		SideEvent.ResetObjectiveLine();
	}

	public override void _Process(double delta)
	{
		if (Game == null || PointsOfInterest.Live.Count == 0)
			return;
		PointsOfInterest.Suspended = Game.BossFightActive;
		if (Game.RunFinished || Game.BossFightActive)
			return;

		// What is on screen now is seen: the camera's view of the world, grown by a little so a
		// lantern whose flame is just over the edge counts. Read from the canvas transform, so it is
		// right on both canvas shapes and at any zoom.
		Viewport viewport = GetViewport();
		if (viewport == null)
			return;
		Transform2D toWorld = viewport.GetCanvasTransform().AffineInverse();
		Rect2 screen = viewport.GetVisibleRect();
		Vector2 a = toWorld * screen.Position;
		Vector2 b = toWorld * screen.End;
		var view = new Rect2(a, b - a).Abs().Grow(SeenMargin);
		PointsOfInterest.UpdateSeen(view);
		if (logPlacement)
			LogDiscoveries();
	}

	// World pixels past the screen's edge at which a point counts as seen.
	private const float SeenMargin = 80f;

	private int loggedSeen;

	private void LogDiscoveries()
	{
		int seen = 0;
		foreach (PointOfInterest p in PointsOfInterest.Live)
			if (p.Seen) seen++;
		if (seen == loggedSeen)
			return;
		loggedSeen = seen;
		GD.Print($"EXPLORE t={Game.RunSeconds:0.0}s seen {seen} of {PointsOfInterest.Live.Count} points, revealed={PointsOfInterest.Revealed}");
	}

	/// <summary>
	/// Starts this chapter's event <paramref name="index"/> (0, its original, by default) now, where it
	/// stands, as if the player had arrived. For probes and _UiShot --win-event; a run never calls it.
	/// </summary>
	public SideEvent Start(int index = 0)
	{
		if (index < 0 || index >= events.Count || !IsInstanceValid(events[index]))
			return null;
		events[index].Arrive();
		return events[index];
	}
}
