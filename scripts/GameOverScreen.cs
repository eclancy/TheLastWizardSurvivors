using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TheOneHeMissed.scripts;

/// <summary>
/// The results screen at the end of every run, Defeat or Victory. See .ai/level-up-screen.md,
/// section 6.
/// </summary>
/// <remarks>
/// It was nine centred lines of one weight - "Enemies killed: 0", "Final loadout: Magic Missile Lv
/// 1, ...", "Arcane Energy earned: +28" - with one picture on it, the button. Then it showed
/// everything at once: the headline, every reward card, and the build and the stats behind two tabs.
///
/// FOLDED (2026-11-02, Eric: "The end game info pane should not show much info at first, but the
/// player can click into info sections to see stats and details"). It opens on what a player asks
/// first, and nothing else:
///
///   1. What happened: Defeat or Victory, the chapter and wizard, and three headline numbers.
///   2. What I take away: the Arcane Energy, COUNTED UP a coin at a time and landing on its figure
///      with the payout (the tally), the purse after it, and one picture per thing unlocked under
///      a New seal.
///   3. Three sections, a row each with a line of what is inside - Rewards (how the energy was
///      counted, a card per unlock or the closest unlock), Your build (spells, element tags, boons,
///      relics, sets) and Stats (the run's tiles, damage by spell, Playtest telemetry). A press opens
///      one in place of the summary; Back, B or Escape folds it again.
///   4. Where to go: Retry, or the main menu.
///
/// The header and summary/details share one scrolling pane inside the screen. Retry and Main menu
/// stay outside it, always reachable. Every number is still in one of the sections.
///
/// AFTER A DEFEAT the screen is brought down by the player's press at the end of the defeat
/// (DefeatSequence) and COVERS the field from below (CoversFromBelow); the defeat has played the
/// sting. A victory comes up as it always did, with stage_clear rather than game_over. Either way the
/// count runs once the screen is in place, and every press until it lands is swallowed - one lands it
/// at once - so neither the press that brought the screen down nor a tap's emulated mouse twin can
/// reach a button underneath.
///
/// It READS, it never grants. Everything on it was paid and saved by Node2DGame.FinishRunAndReward
/// before the screen was built (RunReport); the count is dressing on a number already banked.
/// </remarks>
public partial class GameOverScreen : CanvasLayer
{
	// --- inks -----------------------------------------------------------------------------------
	// Everything on the screen writes in CodexRows.Stone, the register of the pause screen's lists,
	// so a row here is the same object as a row there. The few colours of its own are below, all
	// mirrored in tools/art/ui_contrast.py ("results screen") - change one, change both.
	private static CodexRows.Palette Stone => CodexRows.Stone;
	// The outcome words. Gold for a win, cloth red for a loss: the first thing read, so it carries
	// the result. On the heading band (stone deep) the red is 4.8:1 and the gold 14.2:1, at 34px.
	private static readonly Color VictoryInk = new(1.0f, 0.86f, 0.42f);
	private static readonly Color DefeatInk = new(0.85f, 0.32f, 0.34f);
	// LevelUpMenu.StyleHeading's band: the darkest stone, solid, a gold rule at its foot.
	private static readonly Color BandColour = new(0.043f, 0.059f, 0.094f);
	private static readonly Color BandRule = new(0.541f, 0.416f, 0.118f);
	// The milestone marks take the loadout strip's rims (LoadoutHud), so a spell wears the same
	// colour on the HUD and here: gold mutated, cyan ascended, fire at its Apotheosis. Solid, with
	// near-black words on them (9.3, 8.8 and 6.6:1). Internal: the pause screen's Run tab wears them too.
	internal static readonly Color MutationMark = new(0.847f, 0.690f, 0.290f);
	internal static readonly Color AscensionMark = new(0.416f, 0.722f, 0.910f);
	internal static readonly Color ApotheosisMark = new(0.910f, 0.478f, 0.102f);
	private static readonly Color HeldRim = new(0.416f, 0.494f, 0.596f);
	private static readonly Color MarkInk = new(0.06f, 0.06f, 0.07f);
	// A new unlock's card is edged in gold lit, two pixels, where an ordinary row has stone hi.
	private static readonly Color NewEdge = new(0.847f, 0.690f, 0.290f);
	// The icon well: stone deep, so a dark icon still has an edge against the row.
	private static readonly Color Well = new(0.043f, 0.059f, 0.094f);
	// Over the run: dark enough that the swarm stops competing, light enough to show the run ended
	// where it stands.
	private static readonly Color DimColour = new(0.02f, 0.03f, 0.05f, 0.80f);

	private const string RewardIconRoot = "res://assets/bonelight/ui/rewards/";
	private const string WaxSealPath = "res://assets/bonelight/ui/ui-wax-seal.png";
	private const string UnknownSpellIcon = "res://assets/bonelight/ui/spells/unknown.png";

	// A PC panel is capped so a wide monitor does not stretch a row of text across it.
	private const float MaxPanelWidth = 1080f;
	private const int SectionGap = 22;
	private const int RowGap = 10;

	/// <summary>How long the screen takes to cover the field after a defeat; DefeatSequence goes after it.</summary>
	public const float CoverSeconds = 0.42f;
	// The count: about fourteen coins a second, never shorter or longer than these, a little longer the
	// bigger the payout (a tenfold payout is about a quarter of a second more).
	private const float TallyMinSeconds = 0.7f;
	private const float TallyMaxSeconds = 1.6f;
	private const float TallyTicksPerSecond = 14f;
	private const float TallyTickGapSeconds = 0.045f;
	// A victory's screen comes up without a cover; the count waits this long so it is seen to start.
	private const float VictoryTallyDelay = 0.25f;
	// After a swallowed press the next quarter second is swallowed too: a tap arrives as a touch AND an
	// emulated mouse press, in either order, and the second must not press what is under the finger.
	private const ulong QuietAfterPressMsec = 250;
	// Pictures of the unlocks on the folded card; the rest are counted, and all of them are in Rewards.
	private const int StripMaxIconsWide = 6;
	private const int StripMaxIconsNarrow = 5;

	/// <summary>
	/// Set before the screen enters the tree: it was brought down by the press at the end of a defeat
	/// (DefeatSequence), so it covers the field from below and leaves the sting to the defeat.
	/// </summary>
	public bool CoversFromBelow { get; set; }

	// --- the shell, built once ----------------------------------------------------------------
	private PanelContainer panel;
	private VBoxContainer column;
	private VBoxContainer headerHost;
	private Control bodyHost;
	private Button continueButton;
	private Button retryButton;

	// --- per report -----------------------------------------------------------------------------
	private RunReport report;
	private Label energyValue;
	private Control purseLine;
	private Control stripSeal;
	private readonly List<Control> stripIcons = new();
	private Control summaryView;
	private Control sectionView;
	private Label sectionTitle;
	private Button sectionBack;
	private ScrollContainer sectionScroll;
	private readonly List<Section> sections = new();
	private int openSection = -1;
	private bool narrow;

	private sealed class Section
	{
		public string Title;
		public Control Page;
		public Button Opener;
	}

	// --- the arrival and the count --------------------------------------------------------------
	// Ending: a victory over the dark wizard lays the ending over the screen first (ThroneEnding), and the
	// count waits for it. Reels: after the ending, the reels that run owes (the freeing of the one he kept, any
	// lesson) play over the hidden screen (AfterEnding), and the count waits for them too.
	private enum Phase { Cover, Waiting, Tally, Live, Ending, Reels }
	private Phase phase = Phase.Live;
	private float phaseAge;
	private float tallySeconds;
	private int tallySteps;
	private int tallyNextStep;
	private float lastTickAge;
	private ulong quietUntilMsec;
	private Tween coverTween;
	private bool stingPlayed;

	/// <summary>True until the count has landed: presses are being swallowed. For _UiShot.</summary>
	public bool Counting => phase != Phase.Live;

	public override void _Ready()
	{
		narrow = ResponsiveLayout.ViewportSize(this).X < 1120f;
		BuildShell();
		GetViewport().SizeChanged += ResizeResults;

		// Clear of the phone's rounded corners and camera hole; nothing on desktop (SafeArea).
		SafeArea.Apply(this);
		// The selection opens on Main menu, as it opened on Continue before: A carries straight on
		// from a game over without the player putting the pad down to find the mouse.
		MenuNavigator.Attach(this, continueButton);
		// B, Escape and Android's back fold an open section; with none open they do nothing here.
		MenuBackListener.Attach(this, CloseSection);

		if (CoversFromBelow)
		{
			// Below the screen from the first frame it draws, then up over the field - the whole layer,
			// dim and all, so the field is covered from the bottom up. The count starts once it is in.
			phase = Phase.Cover;
			Offset = new Vector2(0f, ResponsiveLayout.ViewportSize(this).Y);
			coverTween = CreateTween();
			coverTween.TweenProperty(this, "offset", Vector2.Zero, CoverSeconds)
				.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			coverTween.TweenCallback(Callable.From(StartTally));
		}
		else
		{
			phase = Phase.Waiting;
		}
	}

	public override void _ExitTree() => GetViewport().SizeChanged -= ResizeResults;

	private void ResizeResults()
	{
		if (report == null) return;
		bool nowNarrow = ResponsiveLayout.ViewportSize(this).X < 1120f;
		bool stackedFooter = ResponsiveLayout.ViewportSize(this).X < 540f;
		if (nowNarrow == narrow && (footer is VBoxContainer) == stackedFooter)
		{
			if (!narrow) panel.CustomMinimumSize = new Vector2(Mathf.Min(MaxPanelWidth, ResponsiveLayout.ViewportSize(this).X - 40f), 0f);
			return;
		}
		int wasOpen = openSection;
		RunUnlockEntry shown = selectedUnlock;
		string sourceName = unlockReturnFocus != null && IsInstanceValid(unlockReturnFocus) ? unlockReturnFocus.Name.ToString() : "";
		foreach (string name in new[] { "Frame", "Dim" })
			if (GetNodeOrNull<Node>(name) is Node node) { RemoveChild(node); node.QueueFree(); }
		narrow = nowNarrow;
		BuildShell();
		unlockDetail = null;
		Present(report);
		if (wasOpen >= 0) OpenSection(wasOpen);
		else if (wasOpen == -2 && shown != null)
			OpenUnlock(shown, FindChild(sourceName, true, false) as Control ?? sections[0].Opener);
		MenuNavigator.Attach(this, continueButton);
		SafeArea.Apply(this);
	}

	/// <summary>Builds the screen from a finished run. Safe to call again: it rebuilds.</summary>
	public void Present(RunReport runReport)
	{
		report = runReport;
		if (report?.Result == null || headerHost == null)
			return;

		Clear(headerHost);
		Clear(bodyHost);
		stripIcons.Clear();
		sections.Clear();
		unlockDetail = null;
		unlockReturnFocus = null;
		selectedUnlock = null;
		openSection = -1;
		energyValue = null;
		purseLine = null;
		stripSeal = null;

		BuildHeader();
		BuildBody();
		ApplyFold();
		ShowEndingIfTheStoryEnds();

		if (!stingPlayed)
		{
			stingPlayed = true;
			// After a defeat the sequence played game_over as DEFEAT landed. A win is stage_clear - it
			// was game_over too, a descent with no resolution, under the word Victory.
			if (!CoversFromBelow)
				SfxPlayer.Global(report.Result.Outcome == RunOutcome.Victory ? SfxCatalog.StageClear
					: report.Result.Outcome == RunOutcome.Quit ? SfxCatalog.UiClose : SfxCatalog.GameOver);
		}
		// Built at its final figures, then wound back to nothing if the count is still to run: a frame
		// that never animates (a harness re-presenting after the count) still says the right thing.
		if (phase != Phase.Live)
			WindBackForTally();

		// A rebuild after the first frame (a harness re-presenting) has new cards to ring and new
		// lists to prepare for a finger; the selection stays where it was, on the footer.
		GetNodeOrNull<MenuNavigator>(MenuNavigator.NodeName)?.Rescan();
	}

	/// <summary>What the screen was built from, for a harness that wants to add to it.</summary>
	public RunReport Report => report;

	private bool endingShown;

	/// <summary>The ending over the screen while it plays (a victory over the dark wizard), else null. For _UiShot.</summary>
	public ThroneEnding Ending { get; private set; }

	/// <summary>
	/// What plays once the ending is done, before the count: Node2DGame's owed reels (Node2DGame.Lessons.cs,
	/// PlayOwedReelsThen), handed the call that brings the screen back. The run that ends the story owes the freeing of
	/// the one he kept (Nimue, freed by his fall - .ai/endgame-castle.md decision 11), and it belongs after the row of
	/// the freed and the credits, not before the ending as every other run's reels are. Null: the count follows the
	/// ending at once. While they play the screen is hidden (no button under the reel can take a press) and swallows
	/// nothing, so the reel's own skip hears every press.
	/// </summary>
	public Action<Action> AfterEnding { get; set; }

	/// <summary>True while the reels after the ending play over the hidden screen. For _UiShot.</summary>
	public bool HeldForReels => phase == Phase.Reels;

	// .ai/endgame-castle.md 6.4: the results of the run that beat him carry the ending - the freed wizards in a
	// row, then the credits - before the ledger. Once a screen, over everything on it; the count waits.
	private void ShowEndingIfTheStoryEnds()
	{
		if (endingShown || !ThroneEnding.EndsTheStory(report?.Result))
			return;
		endingShown = true;
		phase = Phase.Ending;
		Ending = new ThroneEnding();
		Ending.Finished += () =>
		{
			Ending = null;
			if (AfterEnding == null)
			{
				phase = Phase.Waiting;
				phaseAge = 0f;
				return;
			}
			phase = Phase.Reels;
			Visible = false;
			AfterEnding(ResumeAfterReels);
		};
		AddChild(Ending);
	}

	// The reels after the ending are done: the screen back, its selection on the footer for a pad, and the count.
	private void ResumeAfterReels()
	{
		if (!IsInstanceValid(this) || phase != Phase.Reels)
			return;
		Visible = true;
		phase = Phase.Waiting;
		phaseAge = 0f;
		GetNodeOrNull<MenuNavigator>(MenuNavigator.NodeName)?.Refresh();
	}

	private static void Clear(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

	// ============================================================================================
	// The shell: bounded stone panel, scrolling header/body, and a pinned footer.
	// ============================================================================================

	private void BuildShell()
	{
		var dim = new ColorRect { Name = "Dim", Color = DimColour, MouseFilter = Control.MouseFilterEnum.Stop };
		dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		// Edge-anchored, so SafeArea pulls it in from the phone's corners and camera hole.
		var frame = new MarginContainer { Name = "Frame", MouseFilter = Control.MouseFilterEnum.Ignore };
		frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		int margin = narrow ? 10 : 12;
		frame.AddThemeConstantOverride("margin_left", margin);
		frame.AddThemeConstantOverride("margin_right", margin);
		frame.AddThemeConstantOverride("margin_top", margin);
		frame.AddThemeConstantOverride("margin_bottom", margin);
		AddChild(frame);

		var stack = new VBoxContainer { Name = "Stack", MouseFilter = Control.MouseFilterEnum.Ignore };
		stack.AddThemeConstantOverride("separation", 0);
		frame.AddChild(stack);

		panel = new PanelContainer { Name = "Panel", SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		panel.AddThemeStyleboxOverride("panel", BonelightSkin.PanelStyle(BonelightSkin.Register.Stone));
		if (!narrow)
		{
			// Width is the panel's own on a PC; a phone gives it the whole width, which is scarce there.
			Vector2 view = ResponsiveLayout.ViewportSize(this);
			panel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			panel.CustomMinimumSize = new Vector2(Mathf.Min(MaxPanelWidth, view.X - 40f), 0f);
		}
		stack.AddChild(panel);

		column = new VBoxContainer { Name = "Column" };
		column.AddThemeConstantOverride("separation", narrow ? 12 : 10);
		panel.AddChild(column);

		// Only the content scrolls; the footer always fits inside the viewport, even when a run
		// earns enough unlocks to make the folded summary taller than the screen.
		var content = new VBoxContainer { Name = "ResultsContent", MouseFilter = Control.MouseFilterEnum.Ignore };
		content.AddThemeConstantOverride("separation", narrow ? 12 : 10);
		sectionScroll = Scroll("ResultsScroll", content);
		sectionScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		column.AddChild(sectionScroll);
		headerHost = new VBoxContainer { Name = "Header" };
		headerHost.AddThemeConstantOverride("separation", 10);
		content.AddChild(headerHost);

		bodyHost = new MarginContainer
		{
			Name = "Body",
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		content.AddChild(bodyHost);

		column.AddChild(BuildFooter());
	}

	// Show the summary or a section; both use the same bounded scroll above the footer.
	private void ApplyFold()
	{
		bool open = openSection >= 0;
		if (summaryView != null)
			summaryView.Visible = !open;
		if (sectionView != null)
			sectionView.Visible = open;
	}

	private BoxContainer footer;
	private Control BuildFooter()
	{
		footer = ResponsiveLayout.ViewportSize(this).X < 540f ? new VBoxContainer() : new HBoxContainer();
		footer.Name = "Footer";
		footer.Alignment = BoxContainer.AlignmentMode.Center;
		footer.AddThemeConstantOverride("separation", 16);

		// The same restart the pause screen's Restart runs: unpause, reload the run scene with the
		// wizard and chapter Global still holds.
		retryButton = MakeFooterButton("RetryButton", "Retry", BonelightSkin.Emphasis.Normal);
		retryButton.Pressed += OnRetryPressed;
		footer.AddChild(retryButton);

		// Named ContinueButton still: it is the button the old screen had, with the old destination,
		// and the touch probe taps it by that name (_TouchProbe.TapTarget).
		continueButton = MakeFooterButton("ContinueButton", "Main menu", BonelightSkin.Emphasis.Primary);
		continueButton.Pressed += OnContinuePressed;
		footer.AddChild(continueButton);
		// These destinations are a pair: left/right must stay within the footer regardless of
		// the surrounding results layout or which section is open.
		retryButton.FocusNeighborRight = retryButton.GetPathTo(continueButton);
		continueButton.FocusNeighborLeft = continueButton.GetPathTo(retryButton);
		return footer;
	}

	private Button MakeFooterButton(string name, string text, BonelightSkin.Emphasis emphasis)
	{
		var button = new Button
		{
			Name = name,
			Text = text,
			CustomMinimumSize = new Vector2(narrow ? 220f : 200f, narrow ? 60f : 50f),
			FocusMode = Control.FocusModeEnum.All,
		};
		ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(button, emphasis);
		ResponsiveLayout.EnsureTouchTarget(button);
		return button;
	}

	// ============================================================================================
	// 1. The headline: Defeat or Victory, where and who, and three numbers.
	// ============================================================================================

	private void BuildHeader()
	{
		RunResult result = report.Result;
		bool won = result.Outcome == RunOutcome.Victory;

		var band = new PanelContainer { Name = "OutcomeBand", MouseFilter = Control.MouseFilterEnum.Ignore };
		var bandStyle = new StyleBoxFlat { BgColor = BandColour, BorderColor = BandRule };
		bandStyle.BorderWidthBottom = 2;
		bandStyle.SetCornerRadiusAll(2);
		bandStyle.ContentMarginLeft = 16;
		bandStyle.ContentMarginRight = 16;
		bandStyle.ContentMarginTop = 4;
		bandStyle.ContentMarginBottom = 6;
		band.AddThemeStyleboxOverride("panel", bandStyle);

		var bandRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		bandRow.AddThemeConstantOverride("separation", 18);
		band.AddChild(bandRow);

		var title = new Label
		{
			Name = "OutcomeTitle",
			// Endless: a death in a night is not a defeat - the save already has the first night's victory (EndlessHeading).
			Text = EndlessHeading(result) ?? (won ? "Victory" : result.Outcome == RunOutcome.Quit ? "Run ended" : "Defeat"),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = narrow ? HorizontalAlignment.Center : HorizontalAlignment.Left,
			SizeFlagsHorizontal = narrow ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkBegin,
			CustomMinimumSize = new Vector2(0f, ResponsiveLayout.MinTouchTarget),
		};
		// Endless's heading is a sentence, not a word: at the Display size it pushed a PC's band past 1280.
		ResponsiveLayout.SetFont(title, EndlessHeading(result) != null && !narrow ? ResponsiveLayout.TextRole.Title : ResponsiveLayout.TextRole.Display);
		title.AddThemeColorOverride("font_color", won ? VictoryInk : result.Outcome == RunOutcome.Quit || EndlessHeading(result) != null ? Stone.Heading : DefeatInk);
		bandRow.AddChild(title);

		Label where = CodexRows.MakeText(BuildWhereLine(), ResponsiveLayout.TextRole.Label, Stone.InkSoft);
		where.Name = "OutcomeWhere";
		where.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		BoxContainer tiles = ResponsiveLayout.ViewportSize(this).X < 540f ? new VBoxContainer() : new HBoxContainer();
		tiles.Name = "Headline";
		tiles.MouseFilter = Control.MouseFilterEnum.Ignore;
		tiles.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		tiles.AddThemeConstantOverride("separation", narrow ? 8 : 10);
		tiles.AddChild(BuildTimeTile(result, won));
		tiles.AddChild(BuildHeadlineTile("xp", result.FinalPlayerLevel.ToString(CultureInfo.InvariantCulture), "Level reached"));
		tiles.AddChild(BuildHeadlineTile("kills", Count(result.EnemiesKilled), "Enemies slain"));

		if (narrow)
		{
			where.HorizontalAlignment = HorizontalAlignment.Center;
			headerHost.AddChild(band);
			headerHost.AddChild(where);
			headerHost.AddChild(tiles);
			return;
		}

		// On a PC the outcome and its three numbers are one band: height is the scarce axis at
		// 1280x720, and every row the header does not take is a reward card the player sees without
		// scrolling. The title and where it happened stack on the left; the tiles fill the rest.
		var left = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			CustomMinimumSize = new Vector2(250f, 0f),
		};
		left.AddThemeConstantOverride("separation", 0);
		bandRow.RemoveChild(title);
		title.CustomMinimumSize = Vector2.Zero;
		// Endless's sentence of a heading wraps in the column's own width rather than widening it (GameOverScreen.Endless.cs).
		WrapEndlessHeading(title);
		left.AddChild(title);
		where.HorizontalAlignment = HorizontalAlignment.Left;
		// Its own width, not the column's: wrapped at 250 a chapter name broke over two lines.
		where.AutowrapMode = TextServer.AutowrapMode.Off;
		left.AddChild(where);
		bandRow.AddChild(left);
		bandRow.AddChild(tiles);
		bandStyle.ContentMarginTop = 8;
		bandStyle.ContentMarginBottom = 10;
		headerHost.AddChild(band);
	}

	// "Chapter 1 · Enchanted Forest" over "Aedan" on a PC, where it stacks under the title;
	// one line on a phone.
	private string BuildWhereLine()
	{
		// A Boss Rush is not a chapter, though it is fought in one (BossRushCatalog.Arena).
		if (report.BossRushTotal > 0)
		{
			string rush = string.IsNullOrWhiteSpace(report.StageName) ? "Boss Rush" : $"Boss Rush  ·  {report.StageName}";
			if (string.IsNullOrWhiteSpace(report.CharacterName))
				return rush;
			return narrow ? $"{rush}  ·  {report.CharacterName}" : $"{rush}\n{report.CharacterName}";
		}

		// Its place on the road, as the map and the pause screen say it (StageCatalog.ChapterPlace), not the roster
		// index - which called the Fallow Fields, second on the road, Chapter 10.
		StageDefinition stage = StageCatalog.GetByIndex(report.StageIndex);
		string place = stage != null ? StageCatalog.ChapterPlace(stage) : $"Chapter {report.StageIndex + 1}";
		string chapter = string.IsNullOrWhiteSpace(report.StageName)
			? place
			: $"{place}  ·  {report.StageName}";
		if (string.IsNullOrWhiteSpace(report.CharacterName))
			return chapter;
		return narrow ? $"{chapter}  ·  {report.CharacterName}" : $"{chapter}\n{report.CharacterName}";
	}

	private Control BuildTimeTile(RunResult result, bool won)
	{
		if (report.BossRushTotal > 0)
			return BuildBossRushTile(won);
		// Endless: the night and its clock, built as the rush's is (GameOverScreen.Endless.cs).
		if (report.EndlessNight > 0)
			return BuildEndlessTile();

		// A chapter with no survival clock (the Throne: its procession brings him) has no arrival to count to.
		bool clocked = report.BossArrivalSeconds > 0f;
		float arrival = Mathf.Max(1f, report.BossArrivalSeconds);
		// The boss by the name before its epithet - "Elderbark", not "Elderbark, the Treant" - so the
		// line under the bar stays one line on a PC. The breakdown and the chapter card say it in full.
		string boss = (report.BossName ?? string.Empty).Split(',')[0].Trim();
		string caption;
		if (won)
			caption = string.IsNullOrWhiteSpace(boss) ? "Boss defeated" : $"Defeated {boss}";
		else if (report.BossReached && result.Outcome != RunOutcome.Quit)
			caption = string.IsNullOrWhiteSpace(boss) ? "Fell to the boss" : $"Fell to {boss}";
		else
			caption = clocked ? $"Boss at {FormatTime(arrival)}" : null;

		float fraction = won || report.BossReached ? 1f : clocked ? Mathf.Clamp(result.TimeSurvived / arrival, 0f, 1f) : 0f;
		Control tile = BuildHeadlineTile("duration", FormatTime(result.TimeSurvived), "Time survived", fraction, caption, won);
		// Wider than its neighbours: it carries the boss's line as well as its own.
		tile.SizeFlagsStretchRatio = narrow ? 1.25f : 1.6f;
		return tile;
	}

	// A Boss Rush's time tile: the whole rush, fights in (its clock stops for them, so TimeSurvived
	// alone would leave every fight out), and the bar is bosses felled rather than the road to one.
	private Control BuildBossRushTile(bool won)
	{
		int total = Math.Max(1, report.BossRushTotal);
		string caption = won ? $"All {total} bosses felled" : $"{report.BossRushFelled} of {total} bosses felled";
		float fraction = Mathf.Clamp((float)report.BossRushFelled / total, 0f, 1f);
		Control tile = BuildHeadlineTile("duration", FormatTime(report.BossRushSeconds), "Boss Rush", fraction, caption, won);
		tile.SizeFlagsStretchRatio = narrow ? 1.25f : 1.6f;
		return tile;
	}

	private Control BuildHeadlineTile(string glyph, string value, string label, float? progress = null,
		string caption = null, bool progressWon = false)
	{
		var tile = new PanelContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		tile.AddThemeStyleboxOverride("panel", RowStyle(Stone.Face, Stone.Edge, 1, narrow ? 10 : 14, narrow ? 8 : 6));

		Label number = CodexRows.MakeText(value, ResponsiveLayout.TextRole.Title, Stone.Ink);
		number.AutowrapMode = TextServer.AutowrapMode.Off;
		number.Name = "HeadlineValue";
		Label name = CodexRows.MakeText(label, ResponsiveLayout.TextRole.Label, Stone.InkMeta);
		name.AutowrapMode = TextServer.AutowrapMode.Off;
		// Wraps rather than trims: "Defeated the Hollow Choir" is the line a winner reads.
		Label sub = progress == null ? null : CodexRows.MakeText(caption ?? string.Empty, ResponsiveLayout.TextRole.Label,
			progressWon ? Stone.Reward : Stone.InkSoft);

		if (narrow)
		{
			// Three tiles share about 600 units: the glyph stays at its file size beside the number,
			// and everything else stacks under it.
			var box = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			box.AddThemeConstantOverride("separation", 2);
			tile.AddChild(box);
			var top = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			top.AddThemeConstantOverride("separation", 8);
			top.AddChild(Glyph(glyph, 24f));
			top.AddChild(number);
			box.AddChild(top);
			box.AddChild(name);
			if (progress is float f)
			{
				box.AddChild(Bar(f, progressWon ? NewEdge : Stone.BarFill, 6f));
				box.AddChild(sub);
			}
			return tile;
		}

		// On a PC the glyph is 48, the 12-pixel drawing at x4 - a headline is the one place a number
		// should be recognised from across the room - and the time's caption shares the label's line.
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 12);
		tile.AddChild(line);
		line.AddChild(Glyph(glyph, 48f));
		var text = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		text.AddThemeConstantOverride("separation", 0);
		line.AddChild(text);
		text.AddChild(number);
		var foot = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		foot.AddThemeConstantOverride("separation", 10);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		foot.AddChild(name);
		text.AddChild(foot);
		if (progress is float fraction)
		{
			text.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = Control.MouseFilterEnum.Ignore });
			text.AddChild(Bar(fraction, progressWon ? NewEdge : Stone.BarFill, 6f));
			// Under the bar, with the tile's whole width, and free to wrap. Beside the label and
			// unwrapped, "Defeated Elderbark, the Treant" set the tile's minimum width and pushed the
			// panel past the right of a 1280 screen; beside it and wrapped, it took three lines.
			text.AddChild(sub);
		}
		return tile;
	}

	// ============================================================================================
	// The body: the summary - the reward and a row per section - or one section open in its place.
	// ============================================================================================

	private void BuildBody()
	{
		summaryView = BuildSummary();
		bodyHost.AddChild(summaryView);
		// After the summary, which built the pages it holds.
		sectionView = BuildSectionView();
		bodyHost.AddChild(sectionView);
	}

	private static ScrollContainer Scroll(string name, Control content)
	{
		ScrollContainer scroll = CodexRows.BuildScroll(name, new VBoxContainer());
		// BuildScroll wraps a list in its gutter; this content is already laid out, so swap it in.
		Node gutter = scroll.GetChild(0);
		Clear(gutter);
		content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		gutter.AddChild(content);
		return scroll;
	}

	// --- the summary -------------------------------------------------------------------------------

	private Control BuildSummary()
	{
		var box = new VBoxContainer { Name = "Summary", MouseFilter = Control.MouseFilterEnum.Ignore };
		box.AddThemeConstantOverride("separation", narrow ? 14 : 12);
		box.AddChild(BuildRewardCard());

		var rows = new VBoxContainer { Name = "Sections", MouseFilter = Control.MouseFilterEnum.Ignore };
		rows.AddThemeConstantOverride("separation", 8);
		// The reward card's own picture at half its 64: the stat glyph for energy is a dot at this size.
		TextureRect energy = Icon(Reward("reward-energy"), 32f);
		energy.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		AddSection(rows, "Rewards", "Rewards", RewardsGist(), energy, BuildRewardsPage());
		AddSection(rows, "Build", "Your build", BuildGist(), BuildSpellGlyph(), BuildBuildPage());
		AddSection(rows, "Stats", "Stats", StatsGist(), Glyph("damage", 24f), BuildStatsPage());
		box.AddChild(rows);
		return box;
	}

	// What the run paid, counted up by the tally: the Arcane Energy, the purse after it, and a picture
	// of each thing unlocked. Edged in gold when something was unlocked, as an unlock's own card is.
	private Control BuildRewardCard()
	{
		List<RunUnlockEntry> entries = report.Unlocks?.Entries ?? new List<RunUnlockEntry>();
		bool unlocked = entries.Count > 0;
		PanelContainer card = Row(Stone.Face, unlocked ? NewEdge : Stone.Edge, unlocked ? 2 : 1, focusStop: false);
		card.Name = "RewardCard";
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 14);
		RowContent(card).AddChild(line);

		line.AddChild(IconWell(Reward("reward-energy"), 64f, Stone.Reward));

		var text = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		text.AddThemeConstantOverride("separation", 2);
		line.AddChild(text);

		var amount = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		amount.AddThemeConstantOverride("h_separation", 10);
		amount.AddThemeConstantOverride("v_separation", 2);
		// The final figure is set here and wound back to nothing by the tally (WindBackForTally), so a
		// screen that never counts still says the right thing. The heading face at the Display size:
		// it is the number the screen is for.
		energyValue = CodexRows.MakeText($"+{Count(report.ArcaneEarned)}", ResponsiveLayout.TextRole.Title, Stone.Reward);
		energyValue.Name = "EnergyValue";
		energyValue.AutowrapMode = TextServer.AutowrapMode.Off;
		energyValue.AddThemeFontSizeOverride("font_size", ResponsiveLayout.FontSize(energyValue, ResponsiveLayout.TextRole.Display));
		amount.AddChild(energyValue);
		Label unit = CodexRows.MakeText("Arcane Energy", ResponsiveLayout.TextRole.Body, Stone.Ink);
		unit.AutowrapMode = TextServer.AutowrapMode.Off;
		unit.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		amount.AddChild(unit);
		text.AddChild(amount);

		Label purse = CodexRows.MakeText($"You now hold {Count(report.ArcanePurse)}", ResponsiveLayout.TextRole.Label, Stone.InkSoft);
		purse.Name = "PurseLine";
		purseLine = purse;
		text.AddChild(purse);

		if (unlocked)
			RowContent(card).AddChild(BuildUnlockStrip(entries));
		return card;
	}

	// Reward pictures are buttons: each opens its name, effect and source. Overflow opens Rewards.
	private Control BuildUnlockStrip(List<RunUnlockEntry> entries)
	{
		var strip = new HFlowContainer { Name = "UnlockStrip", MouseFilter = Control.MouseFilterEnum.Ignore };
		strip.AddThemeConstantOverride("h_separation", 8);
		strip.AddThemeConstantOverride("v_separation", 8);
		Control seal = BuildSeal();
		seal.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		stripSeal = seal;
		strip.AddChild(seal);
		int most = narrow ? StripMaxIconsNarrow : StripMaxIconsWide;
		foreach (RunUnlockEntry entry in entries.Take(most))
		{
			var button = new Button
			{
				Name = $"StripUnlock_{entry.Kind}_{stripIcons.Count}",
				CustomMinimumSize = new Vector2(80f, 80f),
				FocusMode = Control.FocusModeEnum.All,
				TooltipText = entry.Name,
				MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			};
			BonelightSkin.StyleButton(button);
			Control well = IconWell(UnlockIcon(entry), 64f, NewEdge);
			well.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			BonelightSkin.MakeSubtreeClickThrough(well);
			button.AddChild(well);
			button.Pressed += () => OpenUnlock(entry, button);
			stripIcons.Add(button);
			strip.AddChild(button);
		}
		if (entries.Count > most)
		{
			var more = new Button { Name = "MoreUnlocks", Text = $"+{entries.Count - most} more", FocusMode = Control.FocusModeEnum.All };
			ResponsiveLayout.SetFont(more, ResponsiveLayout.TextRole.Label);
			BonelightSkin.StyleButton(more);
			more.Pressed += () => OpenSection(0);
			stripIcons.Add(more);
			strip.AddChild(more);
		}
		return strip;
	}

	private Control unlockReturnFocus;
	private Control unlockDetail;
	private RunUnlockEntry selectedUnlock;

	private void OpenUnlock(RunUnlockEntry entry, Control source)
	{
		unlockReturnFocus = source;
		selectedUnlock = entry;
		openSection = -2;
		foreach (Section section in sections) section.Page.Visible = false;
		if (unlockDetail != null && IsInstanceValid(unlockDetail))
		{
			unlockDetail.GetParent()?.RemoveChild(unlockDetail);
			unlockDetail.QueueFree();
		}
		unlockDetail = BuildUnlockCard(entry);
		unlockDetail.Name = "UnlockDetail";
		sectionPages.AddChild(unlockDetail);
		sectionTitle.Text = entry.Name;
		summaryView.Visible = false;
		sectionView.Visible = true;
		sectionScroll.ScrollVertical = 0;
		sectionBack.GrabFocus();
		GetNodeOrNull<MenuNavigator>(MenuNavigator.NodeName)?.Rescan();
		SfxPlayer.Global(SfxCatalog.UiOpen);
	}

	// One row of the summary: a picture, the section's name, a line of what is in it, and a chevron. The
	// whole card is the button (MakeCardClickOverlay, first so the content draws over it and lets clicks
	// through), so a click, a tap or A anywhere on it opens the section.
	private void AddSection(VBoxContainer rows, string key, string title, string gist, Control picture, Control page)
	{
		int index = sections.Count;
		var row = new PanelContainer
		{
			Name = $"SectionRow_{key}",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		row.AddThemeStyleboxOverride("panel", RowStyle(Stone.Face, Stone.Edge, 1, 0, 0));
		Button opener = BonelightSkin.MakeCardClickOverlay(true);
		opener.Name = $"Section_{key}";
		opener.Pressed += () => OpenSection(index);
		row.AddChild(opener);

		var padding = new MarginContainer { Name = "Padding", MouseFilter = Control.MouseFilterEnum.Ignore };
		padding.AddThemeConstantOverride("margin_left", 16);
		padding.AddThemeConstantOverride("margin_right", 16);
		padding.AddThemeConstantOverride("margin_top", narrow ? 12 : 10);
		padding.AddThemeConstantOverride("margin_bottom", narrow ? 12 : 10);
		row.AddChild(padding);

		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 14);
		padding.AddChild(line);

		var well = new CenterContainer { CustomMinimumSize = new Vector2(32f, 32f), MouseFilter = Control.MouseFilterEnum.Ignore };
		well.AddChild(picture);
		line.AddChild(well);

		var text = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		text.AddThemeConstantOverride("separation", 0);
		Label name = CodexRows.MakeText(title, ResponsiveLayout.TextRole.Title, Stone.Heading);
		name.AutowrapMode = TextServer.AutowrapMode.Off;
		text.AddChild(name);
		text.AddChild(CodexRows.MakeText(gist, ResponsiveLayout.TextRole.Label, Stone.InkSoft));
		line.AddChild(text);

		// The engine sans, which has the guillemet; the heading face may not.
		Label chevron = CodexRows.MakeText("›", ResponsiveLayout.TextRole.Title, Stone.InkMeta);
		chevron.RemoveThemeFontOverride("font");
		chevron.AutowrapMode = TextServer.AutowrapMode.Off;
		chevron.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		line.AddChild(chevron);

		BonelightSkin.MakeSubtreeClickThrough(padding);
		rows.AddChild(row);
		sections.Add(new Section { Title = title, Page = page, Opener = opener });
	}

	private string RewardsGist()
	{
		int count = report.Unlocks?.Entries?.Count ?? 0;
		return count switch
		{
			0 => "How it was counted",
			1 => "1 new unlock, and how it was counted",
			_ => $"{count} new unlocks, and how it was counted",
		};
	}

	private string BuildGist()
	{
		static string Many(int n, string one) => n == 1 ? $"1 {one}" : $"{n} {one}s";
		int spells = report.Result.EquippedSpells?.Count ?? 0;
		int boons = report.BoonIds?.Count ?? 0;
		int relics = report.RelicIds?.Count ?? 0;
		var parts = new List<string> { Many(spells, "spell") };
		if (boons > 0)
			parts.Add(Many(boons, "boon"));
		if (relics > 0)
			parts.Add(Many(relics, "relic"));
		return string.Join("  ·  ", parts);
	}

	private string StatsGist() => $"{Count(report.Result.TotalDamageDealt)} damage dealt";

	// The build's row leads with the wizard's first spell, at half its 64 (exact pixels).
	private Control BuildSpellGlyph()
	{
		RunSpellSnapshot first = report.Result.EquippedSpells?.FirstOrDefault(s => !s.IsPassive)
			?? report.Result.EquippedSpells?.FirstOrDefault();
		if (first == null)
			return Glyph("generic", 24f);
		TextureRect icon = Icon(SpellIcon(first.Id, first.IconPath), 32f);
		icon.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		return icon;
	}

	// --- a section open ----------------------------------------------------------------------------

	// The section in place of the summary: Back and its name over a rule, then its page in a scroll.
	// Every page is built with the summary and kept, so opening one is a visibility change.
	private VBoxContainer sectionPages;
	private Control BuildSectionView()
	{
		var view = new VBoxContainer
		{
			Name = "SectionView",
			Visible = false,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		view.AddThemeConstantOverride("separation", 10);

		var head = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		head.AddThemeConstantOverride("separation", 16);
		sectionBack = new Button
		{
			Name = "SectionBack",
			Text = "Back",
			FocusMode = Control.FocusModeEnum.All,
			CustomMinimumSize = new Vector2(narrow ? 140f : 120f, narrow ? 56f : 44f),
		};
		ResponsiveLayout.SetFont(sectionBack, ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(sectionBack, BonelightSkin.Emphasis.Normal);
		ResponsiveLayout.EnsureTouchTarget(sectionBack);
		sectionBack.Pressed += CloseSection;
		head.AddChild(sectionBack);
		sectionTitle = CodexRows.MakeText(string.Empty, ResponsiveLayout.TextRole.Title, Stone.Heading);
		sectionTitle.Name = "SectionTitle";
		sectionTitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		sectionTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		sectionTitle.VerticalAlignment = VerticalAlignment.Center;
		head.AddChild(sectionTitle);
		view.AddChild(head);
		view.AddChild(new ColorRect { Color = Stone.Rule, CustomMinimumSize = new Vector2(0, 2), MouseFilter = Control.MouseFilterEnum.Ignore });

		var pages = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		sectionPages = pages;
		foreach (Section section in sections)
		{
			section.Page.Visible = false;
			pages.AddChild(section.Page);
		}
		view.AddChild(pages);
		return view;
	}

	private void OpenSection(int index)
	{
		if (index < 0 || index >= sections.Count)
			return;
		openSection = index;
		if (unlockDetail != null && IsInstanceValid(unlockDetail)) unlockDetail.Visible = false;
		for (int i = 0; i < sections.Count; i++)
			sections[i].Page.Visible = i == index;
		sectionTitle.Text = sections[index].Title;
		ApplyFold();
		sectionScroll.ScrollVertical = 0;
		SfxPlayer.Global(SfxCatalog.UiOpen);
		// The selection goes to Back, above the list, so a pad walks down into the section from its top.
		sectionBack.GrabFocus();
		GetNodeOrNull<MenuNavigator>(MenuNavigator.NodeName)?.Rescan();
	}

	private void CloseSection()
	{
		if (openSection == -1)
			return;
		int was = openSection;
		openSection = -1;
		ApplyFold();
		SfxPlayer.Global(SfxCatalog.UiBack);
		// Back on the row it was opened from.
		if (was == -2 && unlockReturnFocus != null && IsInstanceValid(unlockReturnFocus))
			unlockReturnFocus.GrabFocus();
		else if (was >= 0 && was < sections.Count && IsInstanceValid(sections[was].Opener))
			sections[was].Opener.GrabFocus();
		GetNodeOrNull<MenuNavigator>(MenuNavigator.NodeName)?.Rescan();
	}

	// --- Rewards -----------------------------------------------------------------------------------

	// How the energy was counted, then one card per unlock - or, with nothing unlocked, the closest.
	private Control BuildRewardsPage()
	{
		var page = new VBoxContainer { Name = "RewardsPage", MouseFilter = Control.MouseFilterEnum.Ignore };
		page.AddThemeConstantOverride("separation", SectionGap);

		PanelContainer counted = Row(Stone.Face, Stone.Edge, 1, focusStop: true);
		counted.Name = "Breakdown";
		RowContent(counted).AddChild(BuildBreakdown());
		page.AddChild(Group("How it was counted", string.Empty, new Control[] { counted }, columns: 1));

		List<RunUnlockEntry> entries = report.Unlocks?.Entries ?? new List<RunUnlockEntry>();
		if (entries.Count > 0)
		{
			page.AddChild(Group("Unlocked", $"{entries.Count} new", entries.Select(BuildUnlockCard).ToArray(), columns: 1));
		}
		else
		{
			page.AddChild(Group("Unlocked", string.Empty, new Control[] { Quiet("Nothing new unlocked this run.") }, columns: 1));
			if (BuildClosestUnlock() is Control closest)
				page.AddChild(closest);
		}
		return page;
	}

	// How the payout was counted, one line per part, numbers in a column on the right. Every
	// value is one CalculateArcaneReward recorded as it worked (ArcaneRewardBreakdown).
	private Control BuildBreakdown()
	{
		ArcaneRewardBreakdown a = report.Arcane ?? new ArcaneRewardBreakdown();
		var lines = new List<(string Glyph, string Label, string Value, bool Strong)>();
		lines.Add(("generic", "Base", $"+{Count(a.BaseFlat)}", false));
		if (a.Minutes > 0)
			lines.Add(("duration", $"{a.Minutes} {(a.Minutes == 1 ? "minute" : "minutes")} survived  (x{a.PerMinute})",
				$"+{Count(a.Minutes * a.PerMinute)}", false));
		lines.Add(("xp", $"Level {a.Level} reached  (x{a.PerLevel})", $"+{Count(a.Level * a.PerLevel)}", false));
		if (Math.Abs(a.UpgradeMultiplier - 1f) > 0.001f)
			lines.Add(("energy", "Arcane Upgrades", $"x{a.UpgradeMultiplier:0.00}", false));
		foreach (var (name, bonus) in a.DynamicParts)
			lines.Add(("luck", name, $"+{Mathf.RoundToInt(bonus * 100f)}%", false));
		if (Math.Abs(a.PresetScale - 1f) > 0.001f)
			lines.Add(("generic", "Balance preset", $"x{a.PresetScale:0.00}", false));
		if (a.BossBonus > 0)
			lines.Add(("crit", string.IsNullOrWhiteSpace(a.BossName) ? "Boss defeated" : $"Defeated {a.BossName}",
				$"+{Count(a.BossBonus)}", false));
		// F4, the far sky: tonight's Conjunction made (FarSky.ArcaneBonus).
		if (a.FarSkyBonus > 0) lines.Add(("conjunction", $"Tonight's Conjunction made - {a.FarSkyName}", $"+{Count(a.FarSkyBonus)}", false));
		// Endless: the later nights' purse, capped (GameOverScreen.Endless.cs).
		if (a.NightBonus > 0) lines.Add(("duration", EndlessBreakdownLabel(a), $"+{Count(a.NightBonus)}", false));
		lines.Add(("energy", "This run", $"+{Count(report.ArcaneEarned)}", true));
		if (a.UnlockBonus > 0)
			lines.Add(("done", "Unlock rewards", $"+{Count(a.UnlockBonus)}", true));

		var table = new GridContainer { Columns = 3, MouseFilter = Control.MouseFilterEnum.Ignore };
		table.AddThemeConstantOverride("h_separation", 10);
		table.AddThemeConstantOverride("v_separation", 4);
		foreach (var (glyph, label, value, strong) in lines)
		{
			table.AddChild(Glyph(glyph, 24f));
			Label name = CodexRows.MakeText(label, ResponsiveLayout.TextRole.Label, strong ? Stone.Ink : Stone.InkSoft);
			name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			table.AddChild(name);
			Label number = CodexRows.MakeText(value, ResponsiveLayout.TextRole.Label, strong ? Stone.Reward : Stone.Ink);
			number.AutowrapMode = TextServer.AutowrapMode.Off;
			number.HorizontalAlignment = HorizontalAlignment.Right;
			table.AddChild(number);
		}

		return table;
	}

	private Control BuildUnlockCard(RunUnlockEntry entry)
	{
		PanelContainer card = Row(Stone.Face, NewEdge, 2, focusStop: true);
		card.Name = $"Unlock_{entry.Kind}";
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 14);
		RowContent(card).AddChild(line);

		line.AddChild(IconWell(UnlockIcon(entry), 64f, NewEdge));

		var text = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		text.AddThemeConstantOverride("separation", 3);
		line.AddChild(text);

		var kindRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		kindRow.AddThemeConstantOverride("separation", 8);
		Label kind = CodexRows.MakeText(KindLine(entry.Kind), ResponsiveLayout.TextRole.Label, Stone.Reward);
		kind.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		kind.AutowrapMode = TextServer.AutowrapMode.Off;
		kindRow.AddChild(kind);
		kindRow.AddChild(BuildSeal());
		text.AddChild(kindRow);

		text.AddChild(CodexRows.MakeText(entry.Name, ResponsiveLayout.TextRole.Body, Stone.Ink));
		if (!string.IsNullOrWhiteSpace(entry.Source))
			text.AddChild(CodexRows.MakeText(entry.Source, ResponsiveLayout.TextRole.Label, Stone.InkSoft));
		string description = UnlockDescription(entry);
		if (!string.IsNullOrWhiteSpace(description) && description != entry.Source)
			text.AddChild(KeywordText.Wrap(CodexRows.MakeText(description, ResponsiveLayout.TextRole.Body, Stone.Ink)));
		if (entry.Currency > 0)
			text.AddChild(GlyphLine("energy", $"+{Count(entry.Currency)} Arcane Energy", Stone.Reward));

		return card;
	}

	private static string UnlockDescription(RunUnlockEntry entry) => !string.IsNullOrWhiteSpace(entry.Description) ? entry.Description : entry.Kind switch
	{
		RunUnlockKind.Spell => ContentValidator.LoadAllSpells().FirstOrDefault(s => s.Id.Equals(entry.Id, StringComparison.OrdinalIgnoreCase))?.Description,
		RunUnlockKind.Wizard => CharacterRoster.GetAll().FirstOrDefault(c => c.Id.Equals(entry.Id, StringComparison.OrdinalIgnoreCase))?.StartingPassiveDescription,
		RunUnlockKind.Boon => BoonCatalog.GetById(entry.Id)?.Description,
		RunUnlockKind.Chapter => StageCatalog.GetById(entry.Id)?.FlavorText,
		RunUnlockKind.Deed => AchievementDefinitions.GetById(entry.Id)?.Description,
		_ => entry.Source,
	};

	// The New seal: the mutation page's red wax seal and the word, in the flag ink. Every card in
	// Rewards is new by construction - nothing is listed that the save already had - so the seal is
	// what tells an unlock from the Arcane Energy above it, and it is what pops in.
	private Control BuildSeal()
	{
		var seal = new HBoxContainer { Name = "NewSeal", MouseFilter = Control.MouseFilterEnum.Ignore };
		seal.AddThemeConstantOverride("separation", 4);
		Texture2D wax = BonelightSkin.LoadTextureSafe(WaxSealPath);
		if (wax != null)
		{
			seal.AddChild(new TextureRect
			{
				Texture = wax,
				CustomMinimumSize = new Vector2(30f, 30f),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			});
		}
		Label word = CodexRows.MakeText("New", ResponsiveLayout.TextRole.Label, Stone.Flag);
		word.AutowrapMode = TextServer.AutowrapMode.Off;
		seal.AddChild(word);
		return seal;
	}

	private static string KindLine(RunUnlockKind kind) => kind switch
	{
		RunUnlockKind.Wizard => "New wizard",
		RunUnlockKind.Spell => "New spell",
		RunUnlockKind.Boon => "New boon",
		RunUnlockKind.Chapter => "New chapter",
		RunUnlockKind.Ascension => "Ascension revealed",
		RunUnlockKind.Apotheosis => "Apotheosis revealed",
		RunUnlockKind.Page => "New spell page",
		RunUnlockKind.Lesson => "Learned",
		RunUnlockKind.Mode => "New way to play",
		_ => "Unlock completed",
	};

	private Texture2D UnlockIcon(RunUnlockEntry entry)
	{
		switch (entry.Kind)
		{
			case RunUnlockKind.Wizard:
				return CharacterRoster.GetAll()
					.FirstOrDefault(c => c != null && c.Id.Equals(entry.Id, StringComparison.OrdinalIgnoreCase))?.Portrait
					?? Reward("reward-deed");
			case RunUnlockKind.Spell:
				return SpellIcon(entry.Id);
			case RunUnlockKind.Boon:
				return BonelightSkin.LoadTextureSafe(BoonCatalog.GetById(entry.Id)?.IconPath) ?? Reward("reward-deed");
			case RunUnlockKind.Chapter:
				return Reward("reward-chapter");
			case RunUnlockKind.Ascension:
				return BonelightSkin.LoadTextureSafe(entry.IconPath) ?? Reward("reward-ascension");
			case RunUnlockKind.Apotheosis:
				return BonelightSkin.LoadTextureSafe(entry.IconPath) ?? Reward("reward-apotheosis");
			// A lesson wears its family's mark: the Ascension's glass, the final forms' fire (Teachings).
			case RunUnlockKind.Lesson:
				return Reward(entry.Id is "apotheosis" or "sovereign_rite" ? "reward-apotheosis" : "reward-ascension");
			// A way to play: Endless's candle in its ring of light (tools/art/reward_icons.py).
			case RunUnlockKind.Mode:
				return Reward("reward-vigil") ?? Reward("reward-chapter");
			default:
				return Reward("reward-deed");
		}
	}

	// Nothing unlocked: say what is nearest, so the next run has something to aim at. The Unlocks
	// list's own row (CodexRows.BuildDeedRow), so it reads exactly as it does on the main menu.
	private Control BuildClosestUnlock()
	{
		SaveData data = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		if (data == null)
			return null;
		AchievementContext context = AchievementContext.ForSave(data);
		// ListedFor, not All: the nearest deed must be one this save is shown in full, or the results
		// screen names a secret, or a deed of a wizard nobody has met yet (the Coursing before the
		// Broken Circle opens), that the achievements page still keeps sealed.
		var open = AchievementDefinitions.ListedFor(data)
			.Where(d => !GlobalStatsManager.IsAchievementUnlocked(data, d.Id))
			.Select(d => (Deed: d, Progress: d.GetProgress(context)))
			.Where(p => !string.IsNullOrEmpty(p.Progress?.Display) && p.Progress.Fraction < 1f);
		var closest = CodexRows.OrderInProgress(open).FirstOrDefault();
		if (closest.Deed == null)
			return null;

		var box = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		box.AddThemeConstantOverride("separation", 6);
		box.AddChild(CodexRows.MakeText("Closest unlock", ResponsiveLayout.TextRole.Label, Stone.InkMeta));
		box.AddChild(CodexRows.BuildDeedRow(closest.Deed, false, closest.Progress, SpellIcon, Stone));
		return box;
	}

	// --- Your build ---------------------------------------------------------------------------

	private Control BuildBuildPage()
	{
		var page = new VBoxContainer { Name = "BuildPage", MouseFilter = Control.MouseFilterEnum.Ignore };
		page.AddThemeConstantOverride("separation", SectionGap);
		List<RunSpellSnapshot> spells = report.Result.EquippedSpells ?? new List<RunSpellSnapshot>();

		var attacks = spells.Where(s => !s.IsPassive).ToList();
		var passives = spells.Where(s => s.IsPassive).ToList();
		page.AddChild(Group(passives.Count > 0 ? "Attacks" : "Spells", attacks.Count.ToString(CultureInfo.InvariantCulture),
			attacks.Count == 0 ? new[] { Quiet("None") } : attacks.Select(BuildSpellRow).ToArray(), columns: 1));
		if (passives.Count > 0)
			page.AddChild(Group("Passives", passives.Count.ToString(CultureInfo.InvariantCulture),
				passives.Select(BuildSpellRow).ToArray(), columns: 1));

		// The run's element tags in total, highest first: the shape of the build in one row of
		// chips, which is what every tier and every Ascension gate was counting.
		var elements = (report.Result.ElementCounts ?? new Dictionary<string, int>())
			.Where(p => p.Value > 0)
			.OrderByDescending(p => p.Value).ThenBy(p => p.Key)
			.Select(p => (p.Key, p.Value)).ToList();
		if (elements.Count > 0)
			page.AddChild(Group("Element tags", string.Empty, new[] { Chips(elements, totals: true) }, columns: 1));

		IReadOnlyList<string> boons = report.BoonIds ?? new List<string>();
		page.AddChild(Group("Boons", boons.Count.ToString(CultureInfo.InvariantCulture),
			boons.Count == 0 ? new[] { Quiet("None taken.") } : new[] { Flow(boons.Select(BuildBoonPill)) }, columns: 1));

		IReadOnlyList<string> relics = report.RelicIds ?? new List<string>();
		var relicRows = new List<Control>();
		if (relics.Count == 0)
			relicRows.Add(Quiet("None claimed."));
		else
			relicRows.Add(Flow(relics.Select(BuildRelicPill)));
		page.AddChild(Group("Relics", relics.Count.ToString(CultureInfo.InvariantCulture), relicRows.ToArray(), columns: 1));

		// Full Set Enchantments the run started, complete ones first (the pause screen's order).
		var sets = ChestItemCatalog.Sets
			.Select(set => (Set: set, Progress: ChestItemCatalog.GetSetProgress(set, relics.ToList())))
			.Where(s => s.Progress.OwnedCount > 0)
			.OrderByDescending(s => s.Progress.OwnedCount >= s.Progress.TotalRequired)
			.ThenByDescending(s => s.Progress.TotalRequired == 0 ? 0f : s.Progress.OwnedCount / (float)s.Progress.TotalRequired)
			.ToList();
		if (sets.Count > 0)
			page.AddChild(Group("Full Set Enchantments", string.Empty,
				new[] { Flow(sets.Select(s => BuildSetPill(s.Set, s.Progress.OwnedCount, s.Progress.TotalRequired))) }, columns: 1));
		return page;
	}

	private Control BuildSpellRow(RunSpellSnapshot spell)
	{
		PanelContainer row = Row(Stone.Face, Stone.Edge, 1, focusStop: true);
		row.Name = $"Spell_{spell.Id}";
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 14);
		RowContent(row).AddChild(line);

		// The icon in its milestone's rim, the loadout strip's colour language.
		Color rim = !string.IsNullOrEmpty(spell.ApotheosisName) ? ApotheosisMark
			: !string.IsNullOrEmpty(spell.AscensionName) ? AscensionMark
			: !string.IsNullOrEmpty(spell.MutationName) ? MutationMark
			: HeldRim;
		line.AddChild(IconWell(SpellIcon(spell.Id, spell.IconPath), 64f, rim));

		var text = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		text.AddThemeConstantOverride("separation", 4);
		line.AddChild(text);

		var titleRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		titleRow.AddThemeConstantOverride("separation", 10);
		Label name = CodexRows.MakeText(string.IsNullOrWhiteSpace(spell.DisplayName) ? spell.Id : spell.DisplayName,
			ResponsiveLayout.TextRole.Body, Stone.Ink);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		titleRow.AddChild(name);
		bool maxed = spell.MaxLevel > 0 && spell.Level >= spell.MaxLevel;
		Label level = CodexRows.MakeText(maxed ? $"Level {spell.Level} - max" : $"Level {spell.Level} of {spell.MaxLevel}",
			ResponsiveLayout.TextRole.Label, maxed ? Stone.Reward : Stone.InkSoft);
		level.AutowrapMode = TextServer.AutowrapMode.Off;
		level.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		titleRow.AddChild(level);
		text.AddChild(titleRow);

		if (!string.IsNullOrEmpty(spell.MutationName))
			text.AddChild(Milestone("Mutation", spell.MutationName, MutationMark, spell.MutationId));
		if (!string.IsNullOrEmpty(spell.AscensionName))
			text.AddChild(Milestone("Ascension", spell.AscensionName, AscensionMark, spell.AscensionId));
		if (!string.IsNullOrEmpty(spell.ApotheosisName))
			text.AddChild(Milestone("Apotheosis", spell.ApotheosisName, ApotheosisMark, spell.ApotheosisId));
		if (spell.Elements != null && spell.Elements.Count > 0)
			text.AddChild(Chips(spell.Elements));
		return row;
	}

	// "[Ascension] (icon) Titan Cataclysm": the milestone as a solid mark in its rim colour, the
	// branch's own picture at 32 (EvolutionIcons; exactly half its 64, so it stays on whole pixels),
	// and the branch taken in the row's ink after it. A branch with no picture is the words alone.
	// Internal: the pause screen's Run tab lists each held spell's milestones with this same line
	// (Node2DGame.BuildSpellRow), so the two screens cannot word or colour them two ways.
	//
	// pending: a milestone still AHEAD - the pause screen's Apotheosis on the wizard's own spell before
	// it is taken (Node2DGame.BuildPendingApotheosis). The mark is outlined, not solid, since solid is
	// what says "taken" on this line; its word is in the mark's colour (Fire mid on stone base, 5.2:1),
	// the name in the softer ink and the note after it in the meta ink (6.1:1).
	internal static Control Milestone(string word, string branch, Color mark, string branchId, bool pending = false, string note = null)
	{
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 8);
		Texture2D picture = EvolutionIcons.Own(branchId);
		var pill = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
		var style = new StyleBoxFlat { BgColor = pending ? new Color(mark, 0f) : mark };
		if (pending)
		{
			style.BorderColor = mark;
			style.SetBorderWidthAll(2);
		}
		style.SetCornerRadiusAll(4);
		style.ContentMarginLeft = 8;
		style.ContentMarginRight = 8;
		style.ContentMarginTop = 1;
		style.ContentMarginBottom = 1;
		pill.AddThemeStyleboxOverride("panel", style);
		Label label = CodexRows.MakeText(word, ResponsiveLayout.TextRole.Label, pending ? mark : MarkInk);
		label.AutowrapMode = TextServer.AutowrapMode.Off;
		pill.AddChild(label);
		line.AddChild(pill);
		if (picture != null)
		{
			TextureRect icon = Icon(picture, 32f);
			icon.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			line.AddChild(icon);
		}
		Label name = CodexRows.MakeText(branch, ResponsiveLayout.TextRole.Label, pending ? Stone.InkSoft : Stone.Ink);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		if (string.IsNullOrEmpty(note))
		{
			line.AddChild(name);
			return line;
		}
		// The note beside the name, wrapping with it as one run of words on a phone.
		name.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		name.AutowrapMode = TextServer.AutowrapMode.Off;
		var words = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		words.AddThemeConstantOverride("h_separation", 8);
		words.AddChild(name);
		Label after = CodexRows.MakeText($"·  {note}", ResponsiveLayout.TextRole.Label, Stone.InkMeta);
		after.AutowrapMode = TextServer.AutowrapMode.Off;
		words.AddChild(after);
		line.AddChild(words);
		return line;
	}

	private Control BuildBoonPill(string id)
	{
		BoonDefinition boon = BoonCatalog.GetById(id);
		// The relic in its own light, as on the pause screen (Node2DGame.BuildBoonListSection): one
		// aura cell at x1, quieter than on the card, so a list of them still reads as a list.
		Control picture = new BoonAura
		{
			Icon = BonelightSkin.LoadTextureSafe(boon?.IconPath),
			AuraElement = boon != null ? BoonAura.ElementFor(boon) : Element.Arcane,
			Intensity = 0.7f,
			CustomMinimumSize = new Vector2(BoonAura.Cell, BoonAura.Cell),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		return Pill(picture, boon?.Name ?? id, null, Stone.Ink);
	}

	private Control BuildRelicPill(string id)
	{
		return Pill(Icon(BonelightSkin.LoadTextureSafe(ChestItemCatalog.GetIconPath(id)), 32f),
			ChestItemCatalog.GetDisplayName(id), null, Stone.Ink);
	}

	private Control BuildSetPill(ChestSetDefinition set, int have, int need)
	{
		bool complete = need > 0 && have >= need;
		return Pill(Icon(BonelightSkin.LoadTextureSafe(set.IconPath), 32f), set.Name,
			complete ? $"{have}/{need} complete" : $"{have}/{need}", complete ? Stone.Reward : Stone.Ink);
	}

	// --- Stats -----------------------------------------------------------------------------------

	private Control BuildStatsPage()
	{
		RunResult r = report.Result;
		var page = new VBoxContainer { Name = "StatsPage", MouseFilter = Control.MouseFilterEnum.Ignore };
		page.AddThemeConstantOverride("separation", SectionGap);

		var tiles = new List<Control>
		{
			StatTile("kills", Count(r.EnemiesKilled), "Enemies slain"),
			StatTile("elite", Count(r.ElitesKilled), "Elites slain"),
			StatTile("damage", Count(r.TotalDamageDealt), "Damage dealt"),
			StatTile("heart", Count(r.TotalDamageTaken), r.HitsTaken == 1 ? "Damage taken, 1 hit" : $"Damage taken, {Count(r.HitsTaken)} hits"),
			StatTile("xp", Count(report.XpGathered), "XP gathered"),
			StatTile("chest", Count(r.ChestsOpened), r.ChestsOpened == 1 ? "Chest opened" : "Chests opened"),
			r.HitsTaken == 0
				? StatTile("evasion", "Untouched", "Never hit all run")
				: StatTile("evasion", FormatTime(r.UndamagedSeconds), "First hit taken"),
		};
		if (r.RevivesUsed > 0)
			tiles.Add(StatTile("regen", Count(r.RevivesUsed), r.RevivesUsed == 1 ? "Extra life spent" : "Extra lives spent"));
		page.AddChild(Group("The run", string.Empty, tiles.ToArray(), columns: narrow ? 2 : 3));

		page.AddChild(Group("Damage by spell", string.Empty, BuildDamageRows().ToArray(), columns: 1));

		if (report.PlaytestMode)
			page.AddChild(Group("Playtest telemetry", string.Empty,
				new Control[] { CodexRows.MakeText(BuildTelemetrySummaryText(r), ResponsiveLayout.TextRole.Label, Stone.InkMeta) }, columns: 1));
		return page;
	}

	private Control StatTile(string glyph, string value, string label)
	{
		var tile = new PanelContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		tile.AddThemeStyleboxOverride("panel", RowStyle(Stone.Face, Stone.Edge, 1, 12, 8));
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 10);
		tile.AddChild(line);
		line.AddChild(Glyph(glyph, 24f));
		var text = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		text.AddThemeConstantOverride("separation", 0);
		Label number = CodexRows.MakeText(value, ResponsiveLayout.TextRole.Body, Stone.Ink);
		number.AutowrapMode = TextServer.AutowrapMode.Off;
		text.AddChild(number);
		text.AddChild(CodexRows.MakeText(label, ResponsiveLayout.TextRole.Label, Stone.InkMeta));
		line.AddChild(text);
		return tile;
	}

	// Each spell's share of the damage, biggest first, as a bar against the biggest. A tick of
	// poison or burn reaches the enemy with no spell named (GameStats.RecordDamageBySource), so it
	// is one "Other" line at the foot rather than a guess at whose it was.
	private List<Control> BuildDamageRows()
	{
		var rows = new List<Control>();
		var sources = report.DamageBySource ?? new List<(string SourceId, int Damage)>();
		long sum = sources.Sum(s => (long)s.Damage);
		if (sum <= 0)
		{
			rows.Add(Quiet("No damage recorded."));
			return rows;
		}

		var named = sources.Where(s => s.SourceId != "(unattributed)").ToList();
		int other = sources.Where(s => s.SourceId == "(unattributed)").Sum(s => s.Damage);
		int top = Math.Max(1, sources.Max(s => s.Damage));
		foreach (var (id, damage) in named)
			rows.Add(DamageRow(SpellIcon(id), SpellName(id), damage, top, sum, Stone.BarFill));
		if (other > 0)
			rows.Add(DamageRow(StatIcons.Load("generic"), "Other - poison, burns and the rest", other, top, sum, Stone.InkMeta));
		return rows;
	}

	private Control DamageRow(Texture2D icon, string name, int damage, int top, long sum, Color fill)
	{
		PanelContainer row = Row(Stone.Face, Stone.Edge, 1, focusStop: false, padX: 12, padY: 8);
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 12);
		RowContent(row).AddChild(line);
		line.AddChild(Icon(icon, 32f));

		var text = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		text.AddThemeConstantOverride("separation", 4);
		var head = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		head.AddThemeConstantOverride("separation", 10);
		Label label = CodexRows.MakeText(name, ResponsiveLayout.TextRole.Label, Stone.Ink);
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		head.AddChild(label);
		int share = (int)Math.Round(damage * 100.0 / Math.Max(1L, sum));
		Label amount = CodexRows.MakeText($"{Count(damage)}  ({share}%)", ResponsiveLayout.TextRole.Label, Stone.InkSoft);
		amount.AutowrapMode = TextServer.AutowrapMode.Off;
		head.AddChild(amount);
		text.AddChild(head);
		text.AddChild(Bar(damage / (float)top, fill, 6f));
		line.AddChild(text);
		return row;
	}

	// ============================================================================================
	// The count (Eric, 2026-11-02: "It then tallys up the reward with a satisfying payout sound").
	// ============================================================================================

	// Once the screen is in place (the cover landed, or a victory's short wait), the Arcane Energy
	// counts up from nothing with a coin each time it passes another step of the figure - quick at
	// first and slowing as it lands, a cubic ease - pitched up the count (SfxPlayer.TallyTick). On the
	// figure: tally_payout, the number pops, and the purse and the unlocks' pictures arrive. Any press
	// lands it at once. Nothing is laid out by it: text, alpha and a scale about the middle only.
	public override void _Process(double delta)
	{
		phaseAge += (float)delta;
		if (phase == Phase.Waiting && phaseAge >= VictoryTallyDelay)
			StartTally();
		else if (phase == Phase.Tally)
			TickTally();
	}

	// The figures as built, wound back to before the count: nothing earned yet, the purse and the
	// pictures not arrived.
	private void WindBackForTally()
	{
		ShowEnergy(0);
		if (purseLine != null)
			purseLine.Modulate = new Color(1f, 1f, 1f, 0f);
		if (stripSeal != null)
			stripSeal.Modulate = new Color(1f, 1f, 1f, 0f);
		foreach (Control icon in stripIcons)
			icon.Modulate = new Color(1f, 1f, 1f, 0f);
	}

	private void StartTally()
	{
		if (report == null)
		{
			phase = Phase.Live;
			return;
		}
		phase = Phase.Tally;
		phaseAge = 0f;
		lastTickAge = -1f;
		int target = Math.Max(0, report.ArcaneEarned);
		tallySeconds = Mathf.Clamp(0.55f + 0.28f * Mathf.Log(target + 1f) / Mathf.Log(10f), TallyMinSeconds, TallyMaxSeconds);
		tallySteps = Math.Max(1, Mathf.RoundToInt(tallySeconds * TallyTicksPerSecond));
		tallyNextStep = 1;
		if (target <= 0)
			LandTally();
	}

	private void TickTally()
	{
		float u = Mathf.Clamp(phaseAge / tallySeconds, 0f, 1f);
		float eased = 1f - (1f - u) * (1f - u) * (1f - u);
		ShowEnergy(Mathf.RoundToInt(report.ArcaneEarned * eased));
		// One coin when the count passes the next step, never two in a frame or closer than the gap:
		// the early steps go by faster than coins can be told apart.
		if (tallyNextStep <= tallySteps && eased >= tallyNextStep / (float)tallySteps)
		{
			if (phaseAge - lastTickAge >= TallyTickGapSeconds)
			{
				SfxPlayer.TallyTick(tallyNextStep / (float)tallySteps);
				lastTickAge = phaseAge;
			}
			while (tallyNextStep <= tallySteps && eased >= tallyNextStep / (float)tallySteps)
				tallyNextStep++;
		}
		if (u >= 1f)
			LandTally();
	}

	// On the figure: the payout, the number's pop, then the purse and the unlocks arriving in turn.
	private void LandTally()
	{
		if (phase == Phase.Live)
			return;
		coverTween?.Kill();
		Offset = Vector2.Zero;
		phase = Phase.Live;
		phaseAge = 0f;
		if (report == null)
			return;
		ShowEnergy(report.ArcaneEarned);
		if (report.ArcaneEarned > 0)
			SfxPlayer.Global(SfxCatalog.TallyPayout);
		Pop(energyValue, 1.3f);

		Tween arrive = CreateTween();
		arrive.SetParallel(true);
		if (purseLine != null)
			arrive.TweenProperty(purseLine, "modulate:a", 1f, 0.25f).SetDelay(0.1f);
		if (stripSeal != null)
			arrive.TweenCallback(Callable.From(() => PopSeal(stripSeal))).SetDelay(0.12f);
		for (int i = 0; i < stripIcons.Count; i++)
		{
			Control icon = stripIcons[i];
			arrive.TweenProperty(icon, "modulate:a", 1f, 0.18f).SetDelay(0.16f + 0.07f * i);
		}
	}

	private void ShowEnergy(int value)
	{
		if (energyValue != null && IsInstanceValid(energyValue))
			energyValue.Text = $"+{Count(value)}";
	}

	// A control grown about its middle and settled back with a little overshoot: a scale, never a move.
	private void Pop(Control control, float from)
	{
		if (control == null || !IsInstanceValid(control))
			return;
		control.PivotOffset = control.Size * 0.5f;
		control.Scale = new Vector2(from, from);
		Tween pop = control.CreateTween();
		pop.TweenProperty(control, "scale", Vector2.One, 0.32f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}

	private void PopSeal(Control seal)
	{
		if (seal == null || !IsInstanceValid(seal))
			return;
		seal.PivotOffset = seal.Size * 0.5f;
		seal.Scale = new Vector2(1.6f, 1.6f);
		Tween pop = seal.CreateTween();
		pop.SetParallel(true);
		pop.TweenProperty(seal, "modulate:a", 1f, 0.12f);
		pop.TweenProperty(seal, "scale", Vector2.One, 0.28f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}

	// Until the count lands, every press and release is swallowed before the buttons hear it: the
	// press that brought the screen down, a tap's touch-and-emulated-mouse pair, a pad A held from the
	// defeat. A press during the count lands it at once; a press during the cover only waits. Each
	// swallowed press keeps the next QuietAfterPressMsec swallowed too, so its twin goes with it.
	public override void _Input(InputEvent @event)
	{
		if (!DefeatSequence.IsButton(@event, out bool pressed))
			return;
		// The reels after the ending own every press while they play (the hold to skip); the screen is hidden under them.
		if (phase == Phase.Reels)
			return;
		ulong now = Time.GetTicksMsec();
		if (phase == Phase.Live && now >= quietUntilMsec)
			return;
		GetViewport().SetInputAsHandled();
		if (!pressed)
			return;
		quietUntilMsec = now + QuietAfterPressMsec;
		if (phase == Phase.Tally || phase == Phase.Waiting)
			LandTally();
	}

	// ============================================================================================
	// Building blocks
	// ============================================================================================

	private static StyleBoxFlat RowStyle(Color face, Color edge, int edgeWidth, int padX, int padY)
	{
		var style = new StyleBoxFlat { BgColor = face, BorderColor = edge };
		style.SetBorderWidthAll(edgeWidth);
		style.SetCornerRadiusAll(6);
		style.ContentMarginLeft = padX;
		style.ContentMarginRight = padX;
		style.ContentMarginTop = padY;
		style.ContentMarginBottom = padY;
		return style;
	}

	// A row is a PanelContainer whose padding is an inner MarginContainer (CodexRows.BuildRow's
	// arrangement), so a focus overlay - a sibling of the padding - covers the whole card and its
	// ring sits on the edge. The overlay ignores the mouse: to a pointer the row is text, and a press
	// that starts on it must be free to become a drag of the list.
	private static PanelContainer Row(Color face, Color edge, int edgeWidth, bool focusStop, int padX = 16, int padY = 12)
	{
		var row = new PanelContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		row.AddThemeStyleboxOverride("panel", RowStyle(face, edge, edgeWidth, 0, 0));
		if (focusStop)
		{
			Button overlay = BonelightSkin.MakeCardClickOverlay(true);
			overlay.Name = "RowFocus";
			overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
			row.AddChild(overlay);
		}
		var padding = new MarginContainer { Name = "Padding", MouseFilter = Control.MouseFilterEnum.Ignore };
		padding.AddThemeConstantOverride("margin_left", padX);
		padding.AddThemeConstantOverride("margin_right", padX);
		padding.AddThemeConstantOverride("margin_top", padY);
		padding.AddThemeConstantOverride("margin_bottom", padY);
		row.AddChild(padding);
		var content = new VBoxContainer { Name = "Content", MouseFilter = Control.MouseFilterEnum.Ignore };
		content.AddThemeConstantOverride("separation", 10);
		padding.AddChild(content);
		return row;
	}

	private static VBoxContainer RowContent(PanelContainer row) => row.GetNode<VBoxContainer>("Padding/Content");

	// A group inside a tab: a quiet label with its count, then its rows in one or more columns.
	private static Control Group(string title, string count, Control[] rows, int columns)
	{
		var box = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		box.AddThemeConstantOverride("separation", 8);
		var head = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		head.AddThemeConstantOverride("separation", 10);
		Label label = CodexRows.MakeText(title, ResponsiveLayout.TextRole.Body, Stone.Heading);
		label.AutowrapMode = TextServer.AutowrapMode.Off;
		head.AddChild(label);
		if (!string.IsNullOrEmpty(count))
		{
			Label tally = CodexRows.MakeText(count, ResponsiveLayout.TextRole.Label, Stone.InkMeta);
			tally.AutowrapMode = TextServer.AutowrapMode.Off;
			tally.SizeFlagsVertical = Control.SizeFlags.ShrinkEnd;
			head.AddChild(tally);
		}
		box.AddChild(head);

		var grid = new GridContainer
		{
			Columns = Math.Max(1, columns),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		grid.AddThemeConstantOverride("h_separation", 10);
		grid.AddThemeConstantOverride("v_separation", RowGap);
		foreach (Control row in rows)
		{
			row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			grid.AddChild(row);
		}
		box.AddChild(grid);
		return box;
	}

	private static Label Quiet(string text) => CodexRows.MakeText(text, ResponsiveLayout.TextRole.Label, Stone.InkMeta);

	private static Control Flow(IEnumerable<Control> items)
	{
		var flow = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		flow.AddThemeConstantOverride("h_separation", 8);
		flow.AddThemeConstantOverride("v_separation", 8);
		foreach (Control item in items)
			flow.AddChild(item);
		return flow;
	}

	// A boon, relic or set as a compact pill: its picture and its name, and a count for a set.
	private static Control Pill(Control picture, string name, string note, Color noteInk)
	{
		var pill = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		pill.AddThemeStyleboxOverride("panel", RowStyle(Stone.Face, Stone.Edge, 1, 8, 4));
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 8);
		pill.AddChild(line);
		picture.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		line.AddChild(picture);
		Label label = CodexRows.MakeText(name, ResponsiveLayout.TextRole.Label, Stone.Ink);
		label.AutowrapMode = TextServer.AutowrapMode.Off;
		line.AddChild(label);
		if (!string.IsNullOrEmpty(note))
		{
			Label tail = CodexRows.MakeText(note, ResponsiveLayout.TextRole.Label, noteInk);
			tail.AutowrapMode = TextServer.AutowrapMode.Off;
			line.AddChild(tail);
		}
		// Room on the right, so the text does not sit on the edge.
		line.AddChild(new Control { CustomMinimumSize = new Vector2(2, 0), MouseFilter = Control.MouseFilterEnum.Ignore });
		return pill;
	}

	// Element chips, CodexRows.BuildChip: a tap opens the element's card, the same object as on
	// every list. Not focusable - the row's overlay is the pad's stop, one per row.
	// A spell's own tags read "Fire x2" (a double tag, the codex's wording); the run's totals read
	// "Fire 7", a count.
	private static Control Chips(IEnumerable<(string Element, int Weight)> tags, bool totals = false)
	{
		var flow = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		flow.AddThemeConstantOverride("h_separation", 6);
		flow.AddThemeConstantOverride("v_separation", 6);
		foreach (var (name, weight) in tags)
		{
			if (!Enum.TryParse(name, true, out Element element))
				continue;
			string text = totals ? $"{element} {weight}" : weight > 1 ? $"{element} x{weight}" : element.ToString();
			flow.AddChild(CodexRows.BuildChip(element, text, false));
		}
		return flow;
	}

	// A picture on the icon well: stone deep with a rim. Icons are drawn at a whole multiple of
	// their pixels - the 64-pixel spell and reward art as drawn, a 48-pixel portrait as drawn and
	// centred - so nothing smears.
	private static Control IconWell(Texture2D texture, float size, Color rim)
	{
		var well = new PanelContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
		};
		var style = new StyleBoxFlat { BgColor = Well, BorderColor = rim };
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(6);
		style.SetContentMarginAll(4);
		well.AddThemeStyleboxOverride("panel", style);
		well.AddChild(Icon(texture, size));
		return well;
	}

	private static TextureRect Icon(Texture2D texture, float size)
	{
		bool native = texture != null && texture.GetWidth() <= size && texture.GetHeight() <= size
			&& Mathf.Abs(texture.GetWidth() - size) > 0.5f;
		return new TextureRect
		{
			Texture = texture,
			CustomMinimumSize = new Vector2(size, size),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			// A picture smaller than its box is centred at its own size (a 48 portrait in 64); one
			// as big or bigger is fitted, which for the 64 art in a 32 box is an exact half.
			StretchMode = native ? TextureRect.StretchModeEnum.KeepCentered : TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
	}

	// A stat glyph (tools/art/stat_icons.py) at its file size or a whole multiple of it.
	private static Control Glyph(string key, float size)
	{
		TextureRect glyph = Icon(StatIcons.Load(key), size);
		glyph.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		glyph.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		return glyph;
	}

	private static Control GlyphLine(string key, string text, Color ink)
	{
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 8);
		line.AddChild(Glyph(key, 24f));
		Label label = CodexRows.MakeText(text, ResponsiveLayout.TextRole.Label, ink);
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		line.AddChild(label);
		return line;
	}

	private static Control Bar(float fraction, Color fill, float height)
	{
		fraction = Mathf.Clamp(fraction, 0f, 1f);
		var track = new PanelContainer { CustomMinimumSize = new Vector2(0, height), MouseFilter = Control.MouseFilterEnum.Ignore };
		var trackStyle = new StyleBoxFlat { BgColor = Stone.BarTrack };
		trackStyle.SetCornerRadiusAll(3);
		track.AddThemeStyleboxOverride("panel", trackStyle);
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 0);
		var filled = new PanelContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsStretchRatio = Math.Max(0.001f, fraction),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		var fillStyle = new StyleBoxFlat { BgColor = fraction > 0f ? fill : new Color(0, 0, 0, 0) };
		fillStyle.SetCornerRadiusAll(3);
		filled.AddThemeStyleboxOverride("panel", fillStyle);
		row.AddChild(filled);
		row.AddChild(new Control
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsStretchRatio = Math.Max(0.001f, 1f - fraction),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});
		track.AddChild(row);
		return track;
	}

	private static Texture2D Reward(string name) => BonelightSkin.LoadTextureSafe($"{RewardIconRoot}{name}.png");

	// A spell's picture: as the run drew it if it was held, otherwise from its resource.
	private Texture2D SpellIcon(string spellId) => SpellIcon(spellId, null);

	private Texture2D SpellIcon(string spellId, string iconPath)
	{
		if (!string.IsNullOrEmpty(spellId) && report?.SpellIcons != null && report.SpellIcons.TryGetValue(spellId, out Texture2D held) && held != null)
			return held;
		if (BonelightSkin.LoadTextureSafe(iconPath) is Texture2D fromPath)
			return fromPath;
		SpellData data = ContentValidator.LoadAllSpells()
			.FirstOrDefault(s => s != null && s.Id.Equals(spellId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		return data?.Icon ?? BonelightSkin.LoadTextureSafe(UnknownSpellIcon);
	}

	private string SpellName(string spellId)
	{
		RunSpellSnapshot held = report?.Result?.EquippedSpells?
			.FirstOrDefault(s => s.Id.Equals(spellId, StringComparison.OrdinalIgnoreCase));
		if (held != null && !string.IsNullOrWhiteSpace(held.DisplayName))
			return held.DisplayName;
		SpellData data = ContentValidator.LoadAllSpells()
			.FirstOrDefault(s => s != null && s.Id.Equals(spellId, StringComparison.OrdinalIgnoreCase));
		if (data != null)
			return data.Name;
		string[] words = (spellId ?? string.Empty).Split('_', StringSplitOptions.RemoveEmptyEntries);
		return words.Length == 0 ? spellId : string.Join(" ", words.Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1)));
	}

	private static string Count(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

	private static string FormatTime(float seconds)
	{
		int totalSeconds = Math.Max(0, Mathf.FloorToInt(seconds));
		int minutes = totalSeconds / 60;
		int remainder = totalSeconds % 60;
		return $"{minutes:00}:{remainder:00}";
	}

	// The playtester's readout, kept from the old screen and shown only with Playtest Mode on: damage
	// rate, hits, rerolls, pity and variety multipliers - numbers nobody reading "Defeat" is after.
	private static string BuildTelemetrySummaryText(RunResult result)
	{
		float minutes = Math.Max(0.0167f, result.TimeSurvived / 60.0f);
		float dps = result.TotalDamageDealt / minutes;
		float avgHit = result.HitsTaken > 0 ? (float)result.TotalDamageTaken / result.HitsTaken : 0f;

		string topPickedSpell = GetTopSpell(result.SpellPickCounts);
		string topUpgradedSpell = GetTopSpell(result.SpellUpgradeCounts);

		string rewardModel = result.ArcaneRewardMultiplier > 0f
			? $"Reward x{result.ArcaneRewardMultiplier:0.00}" + (string.IsNullOrWhiteSpace(result.ArcaneRewardBreakdown) ? string.Empty : $" ({result.ArcaneRewardBreakdown})")
			: string.Empty;

		return $"Damage dealt {result.TotalDamageDealt} ({dps:0} per min), damage taken {result.TotalDamageTaken} across {result.HitsTaken} hits (avg {avgHit:0.0}), level-ups {result.LevelUpsGained}, rerolls {result.RerollsUsed}, swaps {result.SwapsUsed}, removals {result.RemovalsUsed}, skips {result.SkipsUsed}, bans {result.BansUsed}. Picks: {topPickedSpell}. Upgrades: {topUpgradedSpell}. {rewardModel}";
	}

	private static string GetTopSpell(Dictionary<string, int> counts)
	{
		if (counts == null || counts.Count == 0)
			return "none";

		var top = counts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).First();
		return $"{top.Key} x{top.Value}";
	}

	// ============================================================================================
	// The buttons - destinations unchanged.
	// ============================================================================================

	private void OnContinuePressed()
	{
		GetTree().Paused = false;
		// The menu lives on the title screen now, so going "back to the menu" means loading
		// its shell and telling it to skip the press-any-key beat.
		Global.OpenMenuImmediately = true;
		GetTree().ChangeSceneToFile("res://scenes/TitleScreen.tscn");
	}

	// The pause screen's Restart (Node2DGame.OnEscapeRestartPressed), from the results instead:
	// the same wizard on the same chapter, Global still holding both.
	private void OnRetryPressed()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://scenes/node_2d_game.tscn");
	}
}
