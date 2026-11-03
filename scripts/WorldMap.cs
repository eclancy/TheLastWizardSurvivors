using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

/// <summary>
/// The world map: where the chosen wizard walks between chapters and a run is started
/// (.ai/world-map.md). It replaces the stage list in the flow - CharacterSelection, this, the run.
/// </summary>
/// <remarks>
/// Three parts, each its own control: the drawn world (<see cref="WorldMapView"/>), the compass strip
/// across the top (<see cref="WorldMapCompass"/>), and the card for the selected stop, built here.
/// The stops are the campaign in StageCatalog order, then Boss Rush once it is open - left and right
/// step through them, and every one is visitable, locked or not, so its card can say why
/// (.ai/world-map.md, section 2). A locked stop can never be started: <see cref="TryStart"/> asks the
/// save again at the moment of starting.
///
/// TWO SHEETS. The Sketchbook is not on the road (Eric, 2026-10-30: "sketchbook level should not be
/// attached to the map ... it shows up as its own separate map that doesn't mix in with the flow of the
/// actual world map"). Once the campaign is finished a "Sketchbook" button joins the top bar; it turns
/// the screen to the page - <see cref="WorldMapPage"/> in the map's place, the same card, no compass and
/// nothing to step to - and Back turns it back to the world where he was standing. Each turn is a reload
/// of this scene with <see cref="OnPage"/> set, so a sheet is always built whole, the same way.
/// </remarks>
public partial class WorldMap : Control
{
	/// <summary>
	/// The stop on the WORLD a run was last started from, for the session: returning puts him back there.
	/// Also where he was standing when he turned to the page, so turning back finds him there.
	/// </summary>
	public static string LastStopId { get; set; }

	/// <summary>Where the harness wants him to open (_UiShot --map-at). Read once, then cleared.</summary>
	public static string StartOverride { get; set; }

	/// <summary>
	/// The sheet the map opens on, for the session: the Sketchbook's page rather than the world. Set by
	/// turning to the page and cleared by turning back, so a run started from the page returns to it.
	/// Honoured only while the page is open to the save (<see cref="GlobalStatsManager.IsStageAvailable"/>).
	/// </summary>
	public static bool OnPage { get; set; }

	// The world's stops shown at its last opening this session, for the reveal of a new one. Null on a
	// cold start: then nothing is animated, the map simply opens as far as it reaches.
	// Public for the harness (_UiShot --map-shown-before), which stands in for an earlier visit.
	public static HashSet<string> LastShownStops { get; set; }

	private const float CardWidth = 404f;
	// Room for the longest card - a two-line lock, the finale's "Still held" line and a row of five
	// slots - at the phone's type sizes; what is left over goes to the map.
	private const float NarrowCardHeight = 456f;
	private const float RepeatDelay = 0.40f;
	private const float RepeatInterval = 0.18f;

	private sealed class Stop
	{
		public WorldMapStop Layout { get; init; }
		public StageDefinition Stage { get; init; }
		public bool IsBossRush { get; init; }
		public WorldMapView.StopLook Look { get; set; }
		public bool Cleared { get; set; }
		// Shown, but not on the save's road: a chapter kept open by an older order (the grandfathered
		// Cursed Dungeon, .ai/endgame-castle.md 1.5 and 8). Its place is lit; the road to it stays dark.
		public bool OffRoad { get; init; }
		public string Id => Layout.Id;
	}

	private readonly List<Stop> stops = new();
	private WorldMapLayout layout;
	private WorldMapView view;
	private WorldMapPage pageView;
	private WorldMapCompass compass;
	private Button backButton;
	private Button startButton;
	// The world's way to the page: in the top bar once the page is open to the save, never before.
	private Button pageButton;
	// The journal: the quest log and the chapter notes (JournalScreen, .ai/quests-and-notes.md).
	private Button journalButton;
	// This screen's sheet, and whether the page is open to the save at all.
	private bool onPage;
	private bool pageOpen;
	private Button leftArrow;
	private Button rightArrow;
	// TEMPORARY, for Eric's testing - remove before the game ships for good (Eric, 2026-11-01: "Add a
	// button in the story map to replay the intro, for testing. We will remove it later."). It is this
	// field, its block in BuildMapView, ReplayIntro, its place in the focus ring in BuildCard, and
	// Prologue.After.Map. It replays the prologue and comes back to this stop; it never writes
	// SaveData.HasSeenPrologue, either way.
	private Button replayIntroButton;
	private VBoxContainer cardBox;
	private SaveData save;
	private CharacterData character;
	private bool narrow;
	private int selected;

	// The held-stick repeat (MenuNavigator's rule: the stick and the D-pad, never the keyboard, which
	// has its own repeat; nothing until a direction held as the screen opened is let go).
	private bool heldAtOpen;
	private int heldDirection;
	private float repeatCountdown;

	public override void _Ready()
	{
		layout = WorldMapLayout.Load(out string problem);
		if (layout == null)
		{
			// The art is generated and committed, so this is a broken checkout, not a player's state:
			// say so loudly, and keep the game usable through the old list.
			GD.PushError($"WorldMap: {problem}. Falling back to the stage list.");
			CallDeferred(nameof(FallBack));
			return;
		}

		save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		character = CharacterRoster.GetByIndex(Global.SelectedCharacterIdx);
		narrow = ResponsiveLayout.IsNarrow(this);
		// A save that finished the campaign before the page was held back for it has it written open here,
		// quietly (the run that finishes it now announces it), so it stays open when the castle's chapters
		// make the campaign longer (GlobalStatsManager.RecordFinishedCampaign). Saved with the next save.
		GlobalStatsManager.RecordFinishedCampaign(save);
		pageOpen = PageStages().Any(s => GlobalStatsManager.IsStageAvailable(save, s));
		onPage = OnPage && pageOpen;
		OnPage = onPage;
		BuildStops();

		var ground = new ColorRect { Name = "Ground", Color = new Color("#05070C"), MouseFilter = MouseFilterEnum.Ignore };
		ground.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(ground);

		if (onPage)
			BuildPageView();
		else
			BuildMapView();
		BuildTopBar();
		BuildCard();

		selected = ResolveStartIndex();
		if (onPage)
		{
			pageView.Setup(layout, character);
		}
		else
		{
			// The stops shown last time the world was open this session: a stop new since then is
			// revealed with the dark drawing back to it (WorldMapView), once.
			float revealFrom = -1f;
			if (LastShownStops != null && stops.Any(s => !LastShownStops.Contains(s.Id)))
				revealFrom = stops.Where(s => !s.OffRoad && LastShownStops.Contains(s.Id)).Select(s => s.Layout.ArcLength).DefaultIfEmpty(0f).Max();
			LastShownStops = new HashSet<string>(stops.Select(s => s.Id));
			view.Setup(layout, stops.Select(s => new WorldMapView.ViewStop { Layout = s.Layout, Look = s.Look, OffRoad = s.OffRoad }).ToList(),
				character, selected, revealFrom);
			compass.SetMarks(stops.Select(MarkFor));
			// A fresh save has one place shown and nowhere to step to.
			leftArrow.Visible = rightArrow.Visible = stops.Count > 1;
			compass.SetState(selected, selected);
		}
		RebuildCard();

		heldAtOpen = AnyDirectionHeld();

		// Clear of the phone's rounded corners and camera hole; nothing on desktop.
		SafeArea.Apply(this);
		MenuNavigator.Attach(this, startButton);
	}

	// The Journal button: the quest log and the notes over the map (Eric, 2026-10-31: "User should be able
	// to track specific quests"). In the top bar, by Back, on the world and on the page alike; the selection
	// comes back to it when the journal closes.
	private Button BuildJournalButton()
	{
		journalButton = new Button
		{
			Name = "JournalButton",
			Text = "Journal",
			TooltipText = "Quests to track, and the notes you have written",
			CustomMinimumSize = new Vector2(narrow ? 0f : 140f, 0f),
			SizeFlagsVertical = SizeFlags.Fill,
		};
		ResponsiveLayout.SetFont(journalButton, narrow ? ResponsiveLayout.TextRole.Label : ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(journalButton, BonelightSkin.Emphasis.Normal);
		journalButton.Pressed += () =>
		{
			if (JournalScreen.IsOpen)
				return;
			JournalScreen.Open(this, () =>
			{
				if (IsInstanceValid(journalButton))
					journalButton.GrabFocus();
			});
		};
		return journalButton;
	}

	private void FallBack() => SceneTransition.ChangeScene(this, "res://scenes/StageSelection.tscn");

	// --- the stops -------------------------------------------------------------------------------

	/// <summary>The chapters on the page rather than the world: held until the campaign is finished.</summary>
	private static IEnumerable<StageDefinition> PageStages() =>
		StageCatalog.All.Where(s => s.AfterCampaign && s.IsPlayable);

	private void BuildStops()
	{
		if (onPage)
		{
			// One place on the page, and he is standing at it: nothing to step to, nothing on a road.
			foreach (StageDefinition stage in PageStages().Where(s => GlobalStatsManager.IsStageAvailable(save, s)))
			{
				stops.Add(new Stop
				{
					Layout = new WorldMapStop { Id = stage.Id, OnMap = false },
					Stage = stage,
					Look = WorldMapView.StopLook.Open,
					Cleared = IsCleared(stage),
				});
			}
			return;
		}

		foreach (var (stage, rush) in RoadOrder(save != null && BossRushCatalog.IsOpen(save)))
		{
			// The map unrolls as the campaign is won (Eric, 2026-10-31: "you should only unlock the map as
			// you defeat the previous level"): a chapter is not a stop, not on the compass and not lit on
			// the map until its previous boss has fallen (GlobalStatsManager.IsChapterRevealed).
			if (!rush && StageCatalog.IsInCampaign(stage) && !GlobalStatsManager.IsChapterRevealed(save, stage))
				continue;
			WorldMapStop placed = layout.Get(rush ? WorldMapLayout.BossRushId : stage.Id);
			if (placed == null)
			{
				// RegressionChecks.ValidateWorldMap names this too; here it only costs the stop. A chapter
				// not built yet may have no place until the map draws it (as the castle's had none until
				// piece C8): it is left off quietly, as a to-do and not a fault (.ai/endgame-castle.md 1.9).
				if (rush || stage.IsPlayable)
					GD.PushError($"WorldMap: '{(rush ? WorldMapLayout.BossRushId : stage.Id)}' has no place on the map (tools/art/world_map.py PLACES).");
				continue;
			}
			stops.Add(new Stop
			{
				Layout = placed,
				Stage = stage,
				IsBossRush = rush,
				Look = rush || GlobalStatsManager.IsStageAvailable(save, stage) ? WorldMapView.StopLook.Open
					: !stage.IsPlayable ? WorldMapView.StopLook.Unbuilt
					: WorldMapView.StopLook.Locked,
				Cleared = !rush && IsCleared(stage),
				OffRoad = !rush && GlobalStatsManager.IsGrandfathered(save, stage) && !GlobalStatsManager.IsStageEarned(save, stage),
			});
		}
		// The route visits them in this order (RoadOrder, which ValidateWorldMap holds the picture to); the
		// sort is a guard, so a stop drawn out of place is still stepped to along the road, never across it.
		stops.Sort((a, b) => a.Layout.ArcLength.CompareTo(b.Layout.ArcLength));
	}

	/// <summary>
	/// THE STOP-ORDER RULE (.ai/endgame-castle.md 8, .ai/world-map.md 6): the order the road visits the
	/// world's stops, west to east. The campaign up to and including its gate chapter (the Emberdeep); the
	/// Boss Rush, whose seat is on the gate chapter's summit (<paramref name="withRush"/>: only once it is a
	/// stop); the rest of the campaign, the castle across the water; and last any chapter outside the
	/// campaign that is open from the start (none today - one held until the campaign is finished is on
	/// the page instead). Read by <see cref="BuildStops"/> and by RegressionChecks.ValidateWorldMap, which
	/// holds tools/art/world_map.py's PLACES to it - otherwise the road would run back from the tower to
	/// the summit. A campaign with no gate chapter puts the rush after its last chapter, as before.
	/// </summary>
	public static List<(StageDefinition Stage, bool Rush)> RoadOrder(bool withRush)
	{
		var order = new List<(StageDefinition Stage, bool Rush)>();
		StageDefinition gate = StageCatalog.GateChapter;
		bool rushPlaced = !withRush || BossRushCatalog.Arena == null;
		foreach (StageDefinition stage in StageCatalog.Campaign)
		{
			order.Add((stage, false));
			if (!rushPlaced && gate != null && stage.Id.Equals(gate.Id, StringComparison.OrdinalIgnoreCase))
			{
				order.Add((BossRushCatalog.Arena, true));
				rushPlaced = true;
			}
		}
		if (!rushPlaced)
			order.Add((BossRushCatalog.Arena, true));
		foreach (StageDefinition stage in StageCatalog.All.Where(s => !StageCatalog.IsInCampaign(s) && !s.AfterCampaign))
			order.Add((stage, false));
		return order;
	}

	private bool IsCleared(StageDefinition stage)
	{
		BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
		return boss != null && save?.Lifetime != null && save.Lifetime.HasDefeatedBoss(boss.Id);
	}

	private WorldMapCompass.Mark MarkFor(Stop stop, int index)
	{
		int position = stop.IsBossRush ? -1 : StageCatalog.CampaignPositionOf(stop.Stage.Id);
		bool outside = !stop.IsBossRush && position < 0;
		return new WorldMapCompass.Mark
		{
			Kind = stop.IsBossRush ? WorldMapCompass.MarkKind.Rush
				: outside ? WorldMapCompass.MarkKind.Page
				: stop.Look != WorldMapView.StopLook.Open ? WorldMapCompass.MarkKind.Locked
				: stop.Cleared ? WorldMapCompass.MarkKind.Cleared
				: WorldMapCompass.MarkKind.Open,
			Caption = position >= 0 ? (position + 1).ToString() : string.Empty,
			BreakBefore = outside && index > 0 && !IsOutside(stops[index - 1]),
		};
	}

	private static bool IsOutside(Stop stop) => !stop.IsBossRush && !StageCatalog.IsInCampaign(stop.Stage);

	private int ResolveStartIndex()
	{
		string want = StartOverride ?? LastStopId;
		StartOverride = null;
		if (onPage)
			return 0;
		if (string.IsNullOrEmpty(want))
			want = save?.RunTelemetryHistory?.FirstOrDefault()?.StageId;
		int found = stops.FindIndex(s => s.Id.Equals(want ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		if (found >= 0)
			return found;
		// A first visit: the furthest open chapter of the campaign, where the road has got to - on the road,
		// so an old save's grandfathered Dungeon (an island past the dark, OffRoad) is not where he wakes.
		for (int i = stops.Count - 1; i >= 0; i--)
		{
			if (!stops[i].IsBossRush && !stops[i].OffRoad && !IsOutside(stops[i]) && stops[i].Look == WorldMapView.StopLook.Open)
				return i;
		}
		return 0;
	}

	// --- building the screen ---------------------------------------------------------------------

	private void BuildMapView()
	{
		view = new WorldMapView { Name = "MapView" };
		view.SetAnchorsPreset(LayoutPreset.FullRect);
		if (narrow)
		{
			view.OffsetTop = TopBarHeight() + 16f;
			view.OffsetBottom = -(NarrowCardHeight + 24f);
		}
		else
		{
			view.OffsetLeft = 16f;
			view.OffsetTop = TopBarHeight() + 20f;
			view.OffsetRight = -(CardWidth + 32f);
			view.OffsetBottom = -16f;
		}
		view.StopPressed += SelectStop;
		AddChild(view);

		// The step plates, at the map's sides: on a phone the only way to step, on a PC a mouse's.
		leftArrow = MakeArrow("left", -1);
		rightArrow = MakeArrow("right", 1);
		view.AddChild(leftArrow);
		view.AddChild(rightArrow);

		// TEMPORARY (see the field): a small plate in the map's bottom-left corner, inside the map's own
		// rectangle - clear of the top bar, the card and the step plates on both canvases, and inside
		// SafeArea with the map. Quiet, so it does not compete with Start.
		replayIntroButton = new Button
		{
			Name = "ReplayIntroButton",
			Text = "Replay intro",
			TooltipText = "Testing only: play the prologue again, then come back here",
			FocusMode = FocusModeEnum.All,
			AnchorLeft = 0f,
			AnchorRight = 0f,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 8f,
			OffsetRight = narrow ? 156f : 168f,
			OffsetTop = -56f,
			OffsetBottom = -8f,
		};
		ResponsiveLayout.SetFont(replayIntroButton, ResponsiveLayout.TextRole.Label);
		BonelightSkin.StyleButton(replayIntroButton, BonelightSkin.Emphasis.Quiet);
		replayIntroButton.Pressed += ReplayIntro;
		view.AddChild(replayIntroButton);
	}

	// The page in the map's place: the same rectangle, so the card sits where it does on the world.
	private void BuildPageView()
	{
		pageView = new WorldMapPage { Name = "PageView" };
		pageView.SetAnchorsPreset(LayoutPreset.FullRect);
		if (narrow)
		{
			pageView.OffsetTop = TopBarHeight() + 16f;
			pageView.OffsetBottom = -(NarrowCardHeight + 24f);
		}
		else
		{
			pageView.OffsetLeft = 16f;
			pageView.OffsetTop = TopBarHeight() + 20f;
			pageView.OffsetRight = -(CardWidth + 32f);
			pageView.OffsetBottom = -16f;
		}
		AddChild(pageView);
	}

	private Button MakeArrow(string side, int direction)
	{
		var arrow = new Button
		{
			Name = side == "left" ? "StepLeft" : "StepRight",
			FocusMode = FocusModeEnum.None,
			CustomMinimumSize = new Vector2(56f, 96f),
			Icon = BonelightSkin.LoadTextureSafe(WorldMapLayout.Directory + "arrow-" + side + ".png"),
			IconAlignment = HorizontalAlignment.Center,
			ExpandIcon = false,
			TooltipText = direction < 0 ? "Back along the road" : "On along the road",
		};
		BonelightSkin.StyleButton(arrow, BonelightSkin.Emphasis.Quiet);
		arrow.AnchorTop = 0.5f;
		arrow.AnchorBottom = 0.5f;
		arrow.OffsetTop = -48f;
		arrow.OffsetBottom = 48f;
		if (direction < 0)
		{
			arrow.AnchorLeft = arrow.AnchorRight = 0f;
			arrow.OffsetLeft = 8f;
			arrow.OffsetRight = 64f;
		}
		else
		{
			arrow.AnchorLeft = arrow.AnchorRight = 1f;
			arrow.OffsetLeft = -64f;
			arrow.OffsetRight = -8f;
		}
		arrow.Pressed += () => Step(direction);
		return arrow;
	}

	private float TopBarHeight() => narrow ? 80f : 76f;

	private void BuildTopBar()
	{
		var bar = new HBoxContainer { Name = "TopBar" };
		bar.AddThemeConstantOverride("separation", 12);
		bar.AnchorLeft = 0f;
		bar.AnchorRight = 1f;
		bar.AnchorTop = 0f;
		bar.AnchorBottom = 0f;
		bar.OffsetLeft = 16f;
		bar.OffsetRight = -16f;
		bar.OffsetTop = 12f;
		bar.OffsetBottom = 12f + TopBarHeight();
		AddChild(bar);

		backButton = new Button
		{
			Name = "BackButton",
			// On the page, Back turns back to the world, and says so: it does not leave the map.
			Text = onPage ? (narrow ? "World" : "Back to the world") : "Back",
			CustomMinimumSize = new Vector2(narrow ? 112f : 140f, 0f),
			SizeFlagsVertical = SizeFlags.Fill,
		};
		ResponsiveLayout.SetFont(backButton, ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(backButton, BonelightSkin.Emphasis.Normal);
		backButton.Pressed += OnBack;
		bar.AddChild(backButton);

		if (onPage)
		{
			// No compass: there is nowhere on the page to step to. The strip's place says where this is.
			var caption = new Label
			{
				Name = "PageCaption",
				Text = "A loose page, apart from the world",
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.Fill,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			ResponsiveLayout.SetBodyText(caption, ResponsiveLayout.TextRole.Label);
			caption.AddThemeColorOverride("font_color", InkPlace);
			bar.AddChild(caption);
			bar.AddChild(BuildJournalButton());
			return;
		}

		compass = new WorldMapCompass
		{
			Name = "Compass",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.Fill,
		};
		compass.MarkPressed += SelectStop;
		bar.AddChild(compass);
		bar.AddChild(BuildJournalButton());

		// The way to the page, at the far end of the road's strip and apart from it: only once the
		// campaign is finished (or the save has been there), so it never sits in the road's flow and
		// never names a place the player cannot reach. The page's own compass mark is its picture.
		if (!pageOpen)
			return;
		pageButton = new Button
		{
			Name = "SketchbookButton",
			Text = "Sketchbook",
			Icon = BonelightSkin.LoadTextureSafe(WorldMapLayout.Directory + "mark-page.png"),
			ExpandIcon = false,
			TooltipText = "Turn to the Sketchbook: a page apart from the world",
			CustomMinimumSize = new Vector2(narrow ? 0f : 168f, 0f),
			SizeFlagsVertical = SizeFlags.Fill,
		};
		ResponsiveLayout.SetFont(pageButton, narrow ? ResponsiveLayout.TextRole.Label : ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(pageButton, BonelightSkin.Emphasis.Normal);
		pageButton.Pressed += TurnToPage;
		bar.AddChild(pageButton);
	}

	private void BuildCard()
	{
		var card = new PanelContainer { Name = "Card" };
		BonelightSkin.ApplyPanel(card, BonelightSkin.Register.Stone);
		if (narrow)
		{
			card.AnchorLeft = 0f;
			card.AnchorRight = 1f;
			card.AnchorTop = 1f;
			card.AnchorBottom = 1f;
			card.OffsetLeft = 12f;
			card.OffsetRight = -12f;
			card.OffsetTop = -(NarrowCardHeight + 12f);
			card.OffsetBottom = -12f;
		}
		else
		{
			card.AnchorLeft = 1f;
			card.AnchorRight = 1f;
			card.AnchorTop = 0f;
			card.AnchorBottom = 1f;
			card.OffsetLeft = -(CardWidth + 16f);
			card.OffsetRight = -16f;
			card.OffsetTop = TopBarHeight() + 20f;
			card.OffsetBottom = -16f;
		}
		AddChild(card);

		var margin = new MarginContainer();
		foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
			margin.AddThemeConstantOverride(side, 18);
		card.AddChild(margin);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 10);
		margin.AddChild(column);

		cardBox = new VBoxContainer { Name = "CardBody", SizeFlagsVertical = SizeFlags.ExpandFill };
		cardBox.AddThemeConstantOverride("separation", 8);
		column.AddChild(cardBox);

		startButton = new Button
		{
			Name = "StartButton",
			CustomMinimumSize = new Vector2(0f, 60f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		ResponsiveLayout.SetFont(startButton, ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(startButton, BonelightSkin.Emphasis.Primary);
		startButton.Pressed += TryStart;
		column.AddChild(startButton);

		// Left and right move the map, never the selection: every focus stop's sideways neighbour is
		// itself, so neither Godot's step nor MenuNavigator's held-stick repeat can walk it off. Up and
		// down go round the stops: Start, then the Sketchbook (once there is one), then Back.
		var ring = new List<Button> { startButton };
		if (pageButton != null)
			ring.Add(pageButton);
		if (journalButton != null)
			ring.Add(journalButton);
		// TEMPORARY (see the field).
		if (replayIntroButton != null)
			ring.Add(replayIntroButton);
		ring.Add(backButton);
		for (int i = 0; i < ring.Count; i++)
		{
			Button button = ring[i];
			button.FocusNeighborLeft = new NodePath(".");
			button.FocusNeighborRight = new NodePath(".");
			button.FocusNeighborTop = button.GetPathTo(ring[(i + 1) % ring.Count]);
			button.FocusNeighborBottom = button.GetPathTo(ring[(i + ring.Count - 1) % ring.Count]);
		}
	}

	// --- the card --------------------------------------------------------------------------------

	private static readonly Color Ink = CodexRows.Stone.Ink;
	private static readonly Color InkSoft = CodexRows.Stone.InkSoft;
	private static readonly Color InkDone = CodexRows.Stone.Done;
	private static readonly Color InkFlag = CodexRows.Stone.Flag;
	// The gold of the inlay, for the "Chapter 3 of 10" line over the name.
	private static readonly Color InkPlace = new(0.85f, 0.69f, 0.29f);

	private void RebuildCard()
	{
		foreach (Node child in cardBox.GetChildren())
		{
			cardBox.RemoveChild(child);
			child.QueueFree();
		}
		if (selected < 0 || selected >= stops.Count)
			return;
		Stop stop = stops[selected];
		StageDefinition stage = stop.Stage;
		bool open = stop.Look == WorldMapView.StopLook.Open;

		int position = stop.IsBossRush ? -1 : StageCatalog.CampaignPositionOf(stage.Id);
		// "of" the chapters the map shows: every chapter is a stop once drawn, and a chapter not drawn
		// yet (the castle's, until piece C8) is not one to count - "Chapter 1 of 13" over ten places
		// would name three the player cannot find.
		int chapters = StageCatalog.Campaign.Count(s => layout.Get(s.Id) != null);
		string where = stop.IsBossRush ? "The summit, over his forge"
			: position >= 0 ? $"Chapter {position + 1} of {chapters}"
			: StageCatalog.OffRoadPlace;
		cardBox.AddChild(MakeText(where, ResponsiveLayout.TextRole.Label, InkPlace, "CardWhere"));
		var name = MakeText(stop.IsBossRush ? "Boss Rush" : stage.DisplayName, ResponsiveLayout.TextRole.Title, Ink, "CardName");
		cardBox.AddChild(name);

		// What he has done to a place is said once it opens, not before: "He keeps a wizard in one of
		// these cells" is the reveal of a secret wizard (UnlockCatalog, Tituba), and it is the beat the
		// chapter plays when it opens. Until then the card says what the place is.
		string line = stop.IsBossRush ? BossRushLine()
			: !open || string.IsNullOrWhiteSpace(stage.CorruptionText) ? stage.FlavorText : stage.CorruptionText;
		cardBox.AddChild(MakeText(line, ResponsiveLayout.TextRole.Body, InkSoft, "CardLine"));

		if (stop.IsBossRush)
		{
			cardBox.AddChild(GlyphLine("kills", BossRushRecord(), InkSoft, "CardState"));
		}
		else if (!open)
		{
			// A gate chapter's lock says who it still waits on, by epithet (StageCatalog.LockedHintFor with the
			// save; .ai/endgame-castle.md 1.6): the one gate the player cannot open by beating one thing. It
			// counts wizards since schema 19, not spells - only those freed before this chapter.
			cardBox.AddChild(GlyphLine("lock", StageCatalog.LockedHintFor(stage, save), InkFlag, "CardState"));
		}
		else
		{
			BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
			if (boss != null)
			{
				cardBox.AddChild(stop.Cleared
					? GlyphLine("done", $"{boss.DisplayName} has fallen", InkDone, "CardState")
					: GlyphLine("elite", $"{boss.DisplayName} holds it", InkSoft, "CardState"));
			}
			float best = save?.Lifetime?.BestTimeOnStage(stage.Id) ?? 0f;
			if (best > 0f)
				cardBox.AddChild(GlyphLine("duration", $"Longest stand: {FormatMinutes(best)}", InkSoft, "CardBest"));
		}

		if (!stop.IsBossRush)
			AddRewards(stage);

		startButton.Text = stop.IsBossRush ? "Begin the Boss Rush"
			: open ? $"Enter {WithArticle(stage.DisplayName)}"
			: "Locked";
		startButton.TooltipText = open || stop.IsBossRush ? string.Empty : StageCatalog.LockedHintFor(stage, save);
	}

	private void AddRewards(StageDefinition stage)
	{
		IReadOnlyList<StageReward> rewards = StageRewards.ForStage(stage.Id);
		if (rewards.Count == 0)
			return;
		cardBox.AddChild(MakeText("What he keeps here", ResponsiveLayout.TextRole.Label, InkPlace, "CardHoldsHeading"));
		var row = new HFlowContainer { Name = "CardHolds" };
		row.AddThemeConstantOverride("h_separation", 8);
		row.AddThemeConstantOverride("v_separation", 8);
		int found = 0;
		foreach (StageReward reward in rewards)
		{
			bool has = StageRewards.IsFound(save, reward);
			if (has)
				found++;
			row.AddChild(BuildSlot(reward, has));
		}
		cardBox.AddChild(row);
		cardBox.AddChild(MakeText($"{found} of {rewards.Count} found", ResponsiveLayout.TextRole.Label, InkSoft, "CardHoldsCount"));
	}

	private const float SlotBox = 64f;

	private Control BuildSlot(StageReward reward, bool found)
	{
		List<Texture2D> frames = SlotFrames(reward, found, out int scale);
		var slot = new PanelContainer
		{
			Name = $"Slot_{reward.Kind}_{reward.Id}",
			CustomMinimumSize = new Vector2(SlotBox + 8f, SlotBox + 8f),
			MouseFilter = MouseFilterEnum.Pass,
			TooltipText = StageRewards.Describe(save, reward, NameOf),
		};
		var style = new StyleBoxFlat
		{
			BgColor = found ? new Color(0.118f, 0.149f, 0.204f) : new Color(0.043f, 0.059f, 0.094f),
			BorderColor = found ? new Color(0.541f, 0.416f, 0.118f) : new Color(0.165f, 0.204f, 0.267f),
		};
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(4);
		slot.AddThemeStyleboxOverride("panel", style);
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		center.AddChild(new PixelArtRect(frames, scale, SlotBox)
		{
			FramesPerSecond = CharacterVisuals.IdleFramesPerSecond,
		});
		slot.AddChild(center);
		return slot;
	}

	/// <summary>
	/// The slot's picture: its own icon or sprite when found, its silhouette when not - the character
	/// select's locked-wizard shadow, for every kind, so a hidden thing reads as one family - and the
	/// sealed portrait for a wizard whose chapter has not opened.
	/// </summary>
	private List<Texture2D> SlotFrames(StageReward reward, bool found, out int scale)
	{
		scale = 1;
		List<Texture2D> art;
		switch (reward.Kind)
		{
			case StageRewardKind.Wizard:
				// A wizard not yet free shows his silhouette (Eric, 2026-11-02: "always show silhouettes of
				// characters not unlocked yet"). Only the dark wizard keeps the seal: his shape is never shown
				// before he is free.
				if (UnlockCatalog.GetCharacter(reward.Id)?.SealedUntilFreed == true && StageRewards.IsSealed(save, reward))
					return new List<Texture2D> { GD.Load<Texture2D>("res://assets/bonelight/characters/sealed-portrait.png") };
				CharacterData who = CharacterRoster.GetAll().FirstOrDefault(c => c.Id.Equals(reward.Id, StringComparison.OrdinalIgnoreCase));
				art = CharacterVisuals.GetIdleFrames(who?.Portrait);
				if (StageRewards.IsSealed(save, reward))
					return CharacterVisuals.BuildSilhouetteFrames(art);
				break;
			case StageRewardKind.Boon:
				art = new List<Texture2D> { BonelightSkin.LoadTextureSafe(BoonCatalog.GetById(reward.Id)?.IconPath) };
				break;
			case StageRewardKind.ArcaneEnergy:
				// A stat glyph, 12 art pixels saved x2: drawn x2 again it sits in the slot like an icon.
				art = new List<Texture2D> { StatIcons.Load("energy") };
				scale = 2;
				break;
			case StageRewardKind.Secret:
				art = new List<Texture2D> { BonelightSkin.LoadTextureSafe(WorldMapLayout.Directory + "secret.png") };
				break;
			case StageRewardKind.Keepsake:
				art = new List<Texture2D> { BonelightSkin.LoadTextureSafe(WorldMapLayout.Directory + "keepsake.png") };
				break;
			default:
				Texture2D icon = SpellIcon(reward.Id);
				art = new List<Texture2D> { icon };
				break;
		}
		art = art.Where(t => t != null).ToList();
		return found ? art : CharacterVisuals.BuildSilhouetteFrames(art);
	}

	private static Texture2D SpellIcon(string spellId)
	{
		SpellData spell = ContentValidator.LoadAllSpells().FirstOrDefault(s => s.Id.Equals(spellId, StringComparison.OrdinalIgnoreCase));
		return spell?.Icon ?? BonelightSkin.LoadTextureSafe("res://assets/bonelight/ui/spells/unknown.png");
	}

	private static string NameOf(StageReward reward) => reward.Kind switch
	{
		StageRewardKind.Wizard => CharacterRoster.GetAll().FirstOrDefault(c => c.Id.Equals(reward.Id, StringComparison.OrdinalIgnoreCase))?.Name ?? reward.Id,
		StageRewardKind.Boon => BoonCatalog.GetById(reward.Id)?.Name ?? reward.Id,
		_ => ContentValidator.LoadAllSpells().FirstOrDefault(s => s.Id.Equals(reward.Id, StringComparison.OrdinalIgnoreCase))?.Name ?? reward.Id,
	};

	private string BossRushLine()
	{
		StageDefinition arena = BossRushCatalog.Arena;
		string where = arena == null ? "the Emberdeep"
			: arena.DisplayName.StartsWith("The ") ? "the " + arena.DisplayName.Substring(4) : arena.DisplayName;
		return $"Every warden of the road, one after another, in {where}. The last is his.";
	}

	private string BossRushRecord()
	{
		int total = BossRushCatalog.Roster.Count;
		if (save == null)
			return $"{total} bosses";
		return save.BossRushBestClearSeconds > 0f ? $"Cleared in {FormatMinutes(save.BossRushBestClearSeconds)}"
			: save.BossRushBestFelled > 0 ? $"Best: {save.BossRushBestFelled} of {total} felled"
			: $"{total} bosses, one after another";
	}

	private static string FormatMinutes(float seconds)
	{
		int whole = Mathf.Max(0, Mathf.RoundToInt(seconds));
		return $"{whole / 60}:{whole % 60:00}";
	}

	// "Enter the Fallow Fields", "Enter Enchanted Forest": a name's own article, lower-cased mid-sentence.
	private static string WithArticle(string name) =>
		name != null && name.StartsWith("The ", StringComparison.Ordinal) ? "the " + name.Substring(4) : name;

	private Label MakeText(string text, ResponsiveLayout.TextRole role, Color ink, string nodeName)
	{
		var label = new Label { Name = nodeName, Text = text, MouseFilter = MouseFilterEnum.Ignore };
		if (role == ResponsiveLayout.TextRole.Title || role == ResponsiveLayout.TextRole.Display)
			ResponsiveLayout.SetFont(label, role);
		else
			ResponsiveLayout.SetBodyText(label, role);
		label.AddThemeColorOverride("font_color", ink);
		return label;
	}

	private Control GlyphLine(string glyph, string text, Color ink, string nodeName)
	{
		var row = new HBoxContainer { Name = nodeName, MouseFilter = MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 8);
		Texture2D icon = StatIcons.Load(glyph);
		if (icon != null)
		{
			row.AddChild(new TextureRect
			{
				Texture = icon,
				CustomMinimumSize = new Vector2(24f, 24f),
				StretchMode = TextureRect.StretchModeEnum.KeepCentered,
				TextureFilter = TextureFilterEnum.Nearest,
				SizeFlagsVertical = SizeFlags.ShrinkBegin,
				MouseFilter = MouseFilterEnum.Ignore,
			});
		}
		Label label = MakeText(text, ResponsiveLayout.TextRole.Label, ink, nodeName + "Text");
		label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(label);
		return row;
	}

	// --- moving ----------------------------------------------------------------------------------

	private void Step(int direction)
	{
		if (stops.Count == 0)
			return;
		SelectStop(Mathf.Clamp(selected + direction, 0, stops.Count - 1));
	}

	private void SelectStop(int index)
	{
		if (index < 0 || index >= stops.Count || index == selected)
			return;
		selected = index;
		view?.Select(index);
		RebuildCard();
		SfxPlayer.Global(SfxCatalog.UiHover);
	}

	public override void _Input(InputEvent @event)
	{
		// The journal over the map has the keys while it is open: its tabs, not the road.
		if (view == null || !IsVisibleInTree() || JournalScreen.IsOpen)
			return;
		// Heard before Godot's own focus step, and consumed, so left and right move the map and never
		// the selection. Echoes included: a held arrow key walks on at the keyboard's own repeat.
		if (@event.IsActionPressed("ui_left", true))
		{
			Step(-1);
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_right", true))
		{
			Step(1);
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if ((view != null || pageView != null) && !JournalScreen.IsOpen && @event.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			OnBack();
		}
	}

	public override void _Notification(int what)
	{
		// Android's back button.
		if (what == NotificationWMGoBackRequest && (view != null || pageView != null) && !JournalScreen.IsOpen)
			OnBack();
	}

	public override void _Process(double delta)
	{
		if (view == null)
			return;
		compass.SetState(selected, view.WizardStopProgress);
		if (!JournalScreen.IsOpen)
			UpdateRepeat((float)delta);
	}

	private void UpdateRepeat(float delta)
	{
		int held = HeldPadDirection();
		if (heldAtOpen)
		{
			if (held == 0 && !AnyDirectionHeld())
				heldAtOpen = false;
			return;
		}
		if (held == 0)
		{
			heldDirection = 0;
			return;
		}
		if (held != heldDirection)
		{
			// The first step was the action event's; repeats start after the delay.
			heldDirection = held;
			repeatCountdown = RepeatDelay;
			return;
		}
		repeatCountdown -= delta;
		if (repeatCountdown > 0f)
			return;
		repeatCountdown = RepeatInterval;
		Step(held);
	}

	/// <summary>The stick or the D-pad held left (-1) or right (1). The hardware, not the actions:
	/// the keyboard has its own repeat and the two would stack.</summary>
	private static int HeldPadDirection()
	{
		foreach (int device in Input.GetConnectedJoypads())
		{
			float x = Input.GetJoyAxis(device, JoyAxis.LeftX);
			if (x <= -0.5f || Input.IsJoyButtonPressed(device, JoyButton.DpadLeft))
				return -1;
			if (x >= 0.5f || Input.IsJoyButtonPressed(device, JoyButton.DpadRight))
				return 1;
		}
		return 0;
	}

	private static bool AnyDirectionHeld()
	{
		foreach (string action in new[] { "ui_left", "ui_right", "move_left", "move_right" })
		{
			if (InputMap.HasAction(action) && Input.IsActionPressed(action))
				return true;
		}
		return HeldPadDirection() != 0;
	}

	// --- leaving ---------------------------------------------------------------------------------

	/// <summary>Starts the selected stop - or refuses, for a place he still holds.</summary>
	private void TryStart()
	{
		if (selected < 0 || selected >= stops.Count)
			return;
		Stop stop = stops[selected];
		if (stop.IsBossRush)
		{
			StageDefinition arena = BossRushCatalog.Arena;
			if (arena == null || !BossRushCatalog.IsOpen(save))
			{
				Refuse();
				return;
			}
			LastStopId = stop.Id;
			Global.SelectedStageIdx = arena.Index;
			Global.BossRush = true;
			SceneTransition.ChangeScene(this, "res://scenes/node_2d_game.tscn", "Boss Rush");
			return;
		}
		// Asked again here, whatever the card said: a locked place can never be started.
		if (!GlobalStatsManager.IsStageAvailable(save, stop.Stage))
		{
			Refuse();
			return;
		}
		// From the page, the world keeps where he stood on the road; OnPage brings him back to the page.
		if (!onPage)
			LastStopId = stop.Id;
		Global.SelectedStageIdx = stop.Stage.Index;
		Global.BossRush = false;
		SceneTransition.ChangeScene(this, "res://scenes/node_2d_game.tscn", stop.Stage.DisplayName);
	}

	private void Refuse()
	{
		SfxPlayer.Global(SfxCatalog.UiDenied);
		view?.Refuse();
		pageView?.Refuse();
	}

	// Back leaves the map from the world; from the page it turns back to the world, where he stood.
	private void OnBack()
	{
		if (onPage)
		{
			OnPage = false;
			SfxPlayer.Global(SfxCatalog.UiBack);
			SceneTransition.ChangeScene(this, "res://scenes/WorldMap.tscn");
			return;
		}
		SceneTransition.ChangeScene(this, "res://scenes/CharacterSelection.tscn");
	}

	/// <summary>
	/// TEMPORARY (see the field): replays the prologue, which comes back to the map with him where he
	/// stands, remembered as TurnToPage remembers him. Never marks the prologue seen or unseen.
	/// </summary>
	private void ReplayIntro()
	{
		if (SelectedStopId.Length > 0)
			LastStopId = SelectedStopId;
		SfxPlayer.Global(SfxCatalog.UiClick);
		Prologue.Play(this, Prologue.After.Map);
	}

	/// <summary>Turns the map to the Sketchbook's page. He is remembered where he stands on the road.</summary>
	private void TurnToPage()
	{
		if (!pageOpen)
		{
			Refuse();
			return;
		}
		if (SelectedStopId.Length > 0)
			LastStopId = SelectedStopId;
		OnPage = true;
		SfxPlayer.Global(SfxCatalog.UiClick);
		SceneTransition.ChangeScene(this, "res://scenes/WorldMap.tscn", "The Sketchbook");
	}

	// --- for the harness -------------------------------------------------------------------------

	public int SelectedIndex => selected;
	public string SelectedStopId => selected >= 0 && selected < stops.Count ? stops[selected].Id : string.Empty;
	public WorldMapView View => view;
	public WorldMapCompass Compass => compass;
	public int IndexOf(string id) => stops.FindIndex(s => s.Id.Equals(id ?? string.Empty, StringComparison.OrdinalIgnoreCase));
	public IReadOnlyList<string> StopIds => stops.Select(s => s.Id).ToList();

	/// <summary>Selects a stop by id (the harness's --map-at, after the screen is up).</summary>
	public void SelectById(string id)
	{
		int index = stops.FindIndex(s => s.Id.Equals(id ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		if (index >= 0)
			SelectStop(index);
	}

	/// <summary>Presses Start through its real handler.</summary>
	public void PressStart() => TryStart();
}
