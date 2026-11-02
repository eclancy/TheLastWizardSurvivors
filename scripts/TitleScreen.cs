using Godot;
using WizardSurvivors.scripts;
using System;
using System.Text.Json;

public partial class TitleScreen : Control
{
	// How long the artwork sits alone before the prompt appears, and how long the fade takes.
	[Export] public float PromptDelay { get; set; } = 1.1f;
	[Export] public float PromptFadeIn { get; set; } = 1.0f;
	[Export] public float PromptBreath { get; set; } = 1.6f;

	// --- animation ------------------------------------------------------------------------
	// Every state of every moving thing is a pre-rendered frame with a closed palette (see
	// tools/art/anim.py). Nothing here tints, fades or cross-dissolves a layer: a blended
	// pixel is not one of the 66 colours the Bonelight contract allows, and the palette audit
	// the generator runs would stop meaning anything the moment this screen breathed. The one
	// exception is the prompt, which is bone-white type alone on its own layer with nothing
	// behind it to dirty.
	[Export] public float FlameFrameSeconds { get; set; } = 0.13f;
	[Export] public float WardPulseSeconds { get; set; } = 0.95f;

	// The eyes in the dark. Every pair blinks on its own clock: it waits BlinkMinGap-BlinkMaxGap
	// seconds, then its lid comes down through two frames, stays shut BlinkHoldSeconds and goes
	// back up a little slower than it came down. Sometimes it blinks twice. At most
	// MaxBlinkingPairs are mid-blink at once and no two start within BlinkStagger of each other,
	// so the wood never blinks together - sixteen pairs on one clock is a lighthouse, not a wood.
	// Every ~10 s a pair: across the phone's sixteen that is a blink about every 0.7 s somewhere.
	[Export] public float BlinkMinGap { get; set; } = 5.0f;
	[Export] public float BlinkMaxGap { get; set; } = 16.0f;
	[Export] public float BlinkCloseSeconds { get; set; } = 0.04f;   // each of the two frames down
	[Export] public float BlinkHoldSeconds { get; set; } = 0.09f;
	[Export] public float BlinkOpenSeconds { get; set; } = 0.06f;    // each of the two frames up
	[Export] public float DoubleBlinkChance { get; set; } = 0.2f;
	[Export] public int MaxBlinkingPairs { get; set; } = 2;
	[Export] public float BlinkStagger { get; set; } = 0.15f;

	private const int FlamePhases = 4;
	private const int WardFrames = 3;

	private Texture2D[] flamesFar, flamesNear, figure, ward;
	private TextureRect flamesFarRect, flamesNearRect, figureRect, wardRect;

	private float flameClock, wardClock;
	private int flameStep, wardStep;                // walks 0,1,2,1 so the pulse turns round rather than snapping
	private readonly RandomNumberGenerator rng = new RandomNumberGenerator();

	// One pair of eyes: a small TextureRect over the picture showing one cell of eyes.png.
	private sealed class EyePair
	{
		public TextureRect Cell;
		public AtlasTexture[] Frames;   // open, half, slit - the pair's row of the sheet
		public int Step = -1;           // -1: open, waiting to blink; else an index into BlinkSteps
		public float Clock;             // seconds left of the wait or of the current step
		public bool Again;              // the second blink of a double
	}

	// A blink as columns of eyes.png: half, slit, shut (no cell), slit, half - then open again.
	private static readonly int[] BlinkSteps = { 1, 2, -1, 2, 1 };
	private EyePair[] eyePairs;
	private int pairsBlinking;
	private float sinceBlinkStarted = 10f;

	// The art is drawn at a whole x2 into TitleImage on both canvases (720x1680 for the 360x840
	// portrait set, 1920x720 for the 960x360 wide set), so an art pixel is two canvas units, and
	// a cell placed at twice its art position sits on the same pixel grid as the layers under it.
	private const float ArtScale = 2f;

	private bool transitioned = false;
	private Control menuInstance;

	// True while the update check is laid over the screen. Input is ignored until it is done, so
	// a key pressed while "Checking for updates..." is up cannot open the menu underneath it.
	private bool updating;
	private bool pendingOpenMenu;

	// The Android copy's "a new version is out" strip (UpdateBanner). Null everywhere else, which
	// is every Windows copy, the editor and every harness.
	private UpdateBanner updateBanner;

	public override void _Ready()
	{
		// Returning from a run, a quit, or backing out of character select: the player has already
		// seen the title beat and should not have to press a key to get past it again.
		bool openMenuNow = Global.OpenMenuImmediately;
		Global.OpenMenuImmediately = false;

		SetUpAnimation();
		AddVersionLabel();
		PlacePrompt();
		GetViewport().SizeChanged += PlacePrompt;

		// The phone cannot update itself, so it only says when there is something newer, and never
		// holds the title up to do it. See UpdateBanner and .ai/releasing.md, "Android".
		updateBanner = UpdateBanner.AttachTo(this);

		// A downloaded copy checks for a newer version before anything else on this screen
		// starts: the prompt, the music and the menu all wait for it. UpdateScreen never runs in
		// the editor or from `godot --path .`, so outside an exported Windows build this is the
		// same screen it always was. See .ai/releasing.md.
		if (UpdateScreen.ShouldCheck())
		{
			updating = true;
			pendingOpenMenu = openMenuNow;
			var update = new UpdateScreen();
			update.Finished += OnUpdateFinished;
			AddChild(update);
			return;
		}

		StartTitle(openMenuNow);
	}

	public override void _ExitTree()
	{
		var viewport = GetViewport();
		if (viewport != null)
			viewport.SizeChanged -= PlacePrompt;
	}

	// PRESS ANY BUTTON sits near the bottom of what the screen actually shows, not at a fixed row
	// of the artwork. The art is 1680 tall at a fixed x2 and a phone shows a different slice of it
	// depending on its height - a Pixel 9 shows file rows 16-823, a 16:9 screen only 100-739 - so
	// a row low enough for one is off the bottom of the other.
	//
	// Rows are the 360x840 art's own (tools/art/splash.py). The strip is PromptRows tall; its
	// bottom goes PromptLowestRow at the lowest, which on a Pixel 9 leaves 44 rows (88 canvas
	// units) under it, and otherwise PromptClearance above the bottom of the screen. That much,
	// not just the rounded corners: AddVersionLabel puts the version 12-40 units off the bottom,
	// raised a further 36 by SafeArea on a phone, and at less clearance it sat beside the P. It never climbs above PromptHighestRow, the row it used to sit on just under the
	// ward, so a short or landscape window cannot pull it up over the figure. Whole art rows only,
	// so the strip stays on the same pixel grid as the art it is drawn over.
	private const int ArtRows = 840;
	private const int PromptRows = 24;
	private const int PromptLowestRow = 780;
	private const int PromptHighestRow = 710;
	private const float PromptClearance = 88f;

	// Landscape: the canvas is always exactly the art's 720 rows tall, so the prompt has one
	// place - under the ward, on the dark pocket splash_wide.py keeps for it (PROMPT_BOTTOM there).
	private const int WideArtRows = 360;
	private const int WidePromptBottomRow = 346;

	private void PlacePrompt()
	{
		var prompt = GetNodeOrNull<Control>("TitleImage/Prompt");
		if (prompt == null)
			return;

		if (!DisplayProfile.IsPortrait)
		{
			prompt.OffsetBottom = (WidePromptBottomRow - WideArtRows) * 2f;
			prompt.OffsetTop = prompt.OffsetBottom - PromptRows * 2f;
			return;
		}

		float canvasHeight = GetViewportRect().Size.Y;
		float clearance = Mathf.Max(PromptClearance, SafeArea.Insets(GetViewport()).Bottom + 52f);
		// Canvas y = height / 2 + (row - 420) * 2, so the row at canvas y = height - clearance is
		// 420 + height / 4 - clearance / 2.
		int lastVisibleRow = Mathf.FloorToInt(ArtRows / 2f + canvasHeight / 4f - clearance / 2f);
		int bottomRow = Mathf.Clamp(lastVisibleRow, PromptHighestRow, PromptLowestRow);

		// Offsets are from TitleImage's bottom edge (anchor 1.0), in canvas units at x2.
		prompt.OffsetBottom = (bottomRow - ArtRows) * 2f;
		prompt.OffsetTop = prompt.OffsetBottom - PromptRows * 2f;
	}

	/// <summary>Reached only when no update was installed - an update quits and restarts the game.</summary>
	private void OnUpdateFinished()
	{
		updating = false;
		StartTitle(pendingOpenMenu);
	}

	/// <summary>
	/// The version, small and quiet in the bottom-left corner, so a player can say which copy
	/// they are on. It is a child of the screen rather than of TitleImage: it sits on the ground
	/// (or in the letterbox, on a tall phone) instead of being scaled into the x2 artwork.
	/// </summary>
	private void AddVersionLabel()
	{
		var version = new Label
		{
			Name = "Version",
			Text = $"v{UpdateScreen.CurrentVersion}",
			AnchorLeft = 0f,
			AnchorRight = 0f,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 14f,
			OffsetRight = 214f,
			OffsetTop = -40f,
			OffsetBottom = -12f,
			VerticalAlignment = VerticalAlignment.Bottom,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(version);
		// Bottom-left is exactly where a phone rounds its corner off.
		SafeArea.ApplyTo(version);
		ResponsiveLayout.SetFont(version, ResponsiveLayout.TextRole.Micro);
		// BonelightSkin's quiet menu tone: there to be found, not to be read on the way past.
		version.AddThemeColorOverride("font_color", new Color(0.70f, 0.72f, 0.78f, 0.75f));
	}

	private void StartTitle(bool openMenuNow)
	{
		// No skin pass here. The scene used to paint two stock fantasy-GUI textures over the
		// whole screen at 0.96 and 0.28 opacity before the title art drew; both are from the
		// pre-Bonelight asset set and they fought the composition. The title art is now a
		// single 360x640 image rendered at a whole x2 (see .ai/art-direction.md section 1).
		FadeInPrompt();

		// Route menu music through the MusicPlayer autoload so it plays continuously from the title
		// screen through the menus and restarts when the player quits a run back to the menu.
		// Which music is MusicCatalog's decision; MusicPlayer.PlayMenuMusic asks it.
		GetNodeOrNull<MusicPlayer>("/root/MusicPlayer")?.PlayMenuMusic();

		GD.Print("TitleScreen: _Ready() invoked");

		// Last, deliberately: everything the menu draws on top of has to exist before it is added.
		if (openMenuNow)
		{
			transitioned = true;
			ShowMenu();
		}
	}

	/// <summary>
	/// Find the animated layers and their frame sets. Every one of them is optional: if the
	/// frames are missing the scene still renders, it just holds on whatever the .tscn has, so
	/// a half-finished regeneration of the art never takes the title screen down.
	/// </summary>
	private void SetUpAnimation()
	{
		rng.Randomize();

		flamesFarRect = GetNodeOrNull<TextureRect>("TitleImage/FlamesFar");
		flamesNearRect = GetNodeOrNull<TextureRect>("TitleImage/FlamesNear");
		figureRect = GetNodeOrNull<TextureRect>("TitleImage/Figure");
		wardRect = GetNodeOrNull<TextureRect>("TitleImage/Ward");

		flamesFar = LoadFrames("flames-far-", FlamePhases);
		flamesNear = LoadFrames("flames-near-", FlamePhases);
		figure = LoadFrames("figure-", FlamePhases);
		ward = LoadFrames("ward-", WardFrames);

		if (!DisplayProfile.IsPortrait)
			UseLandscapeArt();

		SetUpEyes();
	}

	/// <summary>
	/// The eyes in the dark: one small cell per pair, laid over the picture at its place in
	/// eyes.json and stepped through the frames of eyes.png (tools/art/title_eyes.py draws both,
	/// and places every pair where nothing in front of the eyes is drawn, so a cell is never
	/// covered). They used to be two whole-screen layers with six frames that each shut one pair,
	/// so only five pairs ever blinked; now every pair has its own clock and costs one TextureRect.
	/// Like every other layer, missing art leaves the screen without eyes rather than broken.
	/// </summary>
	private void SetUpEyes()
	{
		var container = GetNodeOrNull<Control>("TitleImage/Eyes");
		var sheet = LoadLayer("eyes");
		string specPath = ArtFolder + "eyes.json";
		if (container == null || sheet == null || !FileAccess.FileExists(specPath))
			return;

		var pairs = new System.Collections.Generic.List<EyePair>();
		try
		{
			using JsonDocument doc = JsonDocument.Parse(FileAccess.GetFileAsString(specPath));
			JsonElement root = doc.RootElement;
			JsonElement cell = root.GetProperty("cell");
			JsonElement pitch = root.GetProperty("pitch");
			Vector2 cellSize = new Vector2(cell[0].GetInt32(), cell[1].GetInt32());
			int frameCount = root.GetProperty("frames").GetArrayLength();
			var rows = new System.Collections.Generic.List<string>();
			foreach (JsonElement r in root.GetProperty("rows").EnumerateArray())
				rows.Add(r.GetString());

			// One set of AtlasTextures per row, shared by every pair in that row.
			var frames = new AtlasTexture[rows.Count][];
			for (int row = 0; row < rows.Count; row++)
			{
				frames[row] = new AtlasTexture[frameCount];
				for (int f = 0; f < frameCount; f++)
				{
					frames[row][f] = new AtlasTexture
					{
						Atlas = sheet,
						Region = new Rect2(new Vector2(f * pitch[0].GetInt32(), row * pitch[1].GetInt32()), cellSize),
					};
				}
			}

			int i = 0;
			foreach (JsonElement p in root.GetProperty("pairs").EnumerateArray())
			{
				int row = rows.IndexOf(p.GetProperty("depth").GetString());
				if (row < 0)
					throw new InvalidOperationException($"pair {i} has a depth eyes.json does not list in rows");
				var rect = new TextureRect
				{
					Name = $"Pair{i++}",
					Texture = frames[row][0],
					ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
					StretchMode = TextureRect.StretchModeEnum.Scale,
					MouseFilter = MouseFilterEnum.Ignore,
					Position = new Vector2(p.GetProperty("x").GetInt32(), p.GetProperty("y").GetInt32()) * ArtScale,
					Size = cellSize * ArtScale,
				};
				container.AddChild(rect);
				// Staggered from the start, so the first blinks come at different moments.
				pairs.Add(new EyePair { Cell = rect, Frames = frames[row], Clock = rng.RandfRange(0.5f, BlinkMaxGap) });
			}
		}
		catch (Exception e) when (e is JsonException || e is System.Collections.Generic.KeyNotFoundException
			|| e is InvalidOperationException || e is IndexOutOfRangeException)
		{
			GD.PushError($"TitleScreen: {specPath} is malformed ({e.Message}) - run python tools/art/anim.py");
			foreach (EyePair made in pairs)
				made.Cell.QueueFree();
			return;
		}
		eyePairs = pairs.ToArray();
	}

	// Two complete sets of the title art, drawn for the two shapes of screen (tools/art/splash.py
	// and splash_wide.py). The phone keeps the tall one; a PC gets the wide one. Same nine layers
	// and the same frame counts in both, so everything below is the same code either way.
	private static string ArtFolder => DisplayProfile.IsPortrait
		? "res://assets/bonelight/ui/title/"
		: "res://assets/bonelight/ui/title-wide/";

	// The wide art: 960x360 drawn at x2. Its 640x360 core fills a 1280x720 canvas exactly, and the
	// canvas is always 720 tall on a PC (keep_height), so the rect is centred and never scaled -
	// a wider monitor only shows more of the 160 columns each side of the core.
	private const float WideArtHalfWidth = 960f;
	private const float WideArtHalfHeight = 360f;

	/// <summary>
	/// The .tscn is authored with the portrait set. On a landscape screen, resize the art rect to
	/// the wide set and point every static layer at it; the animated layers already loaded their
	/// frames from ArtFolder, and are given frame 0 here so nothing shows a portrait frame first.
	/// </summary>
	private void UseLandscapeArt()
	{
		var image = GetNodeOrNull<TextureRect>("TitleImage");
		if (image == null)
			return;
		image.OffsetLeft = -WideArtHalfWidth;
		image.OffsetRight = WideArtHalfWidth;
		image.OffsetTop = -WideArtHalfHeight;
		image.OffsetBottom = WideArtHalfHeight;
		image.Texture = LoadLayer("back-a");
		SetStatic("TitleImage/BackB", "back-b");
		SetStatic("TitleImage/Fore", "fore");
		Show(flamesFarRect, flamesFar, 0);
		Show(flamesNearRect, flamesNear, 0);
		Show(figureRect, figure, 0);
		Show(wardRect, ward, 0);
	}

	private void SetStatic(string path, string layer)
	{
		var rect = GetNodeOrNull<TextureRect>(path);
		var tex = LoadLayer(layer);
		if (rect != null && tex != null)
			rect.Texture = tex;
	}

	private static Texture2D LoadLayer(string name)
	{
		var path = ArtFolder + name + ".png";
		return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
	}

	private static Texture2D[] LoadFrames(string prefix, int count)
	{
		var frames = new Texture2D[count];
		for (int i = 0; i < count; i++)
		{
			var path = $"{ArtFolder}{prefix}{i}.png";
			frames[i] = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		}
		return frames[0] == null ? null : frames;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// The strip belongs to the title and the root menu; over a full-screen panel it would cover
		// that panel's heading.
		if (updateBanner != null)
			updateBanner.SetHiddenByMenu(menuInstance is MainMenu menu && IsInstanceValid(menu) && !menu.IsAtRoot);

		// The flames and the figure share one clock on purpose. The rim and bounce on the
		// wizard are the light those flames are throwing, so driven off a second timer they
		// read as two unrelated animations happening near each other rather than as one light
		// source and the thing it is lighting. The far and near flame layers share it too -
		// they are the same six flames with the figure standing between them.
		flameClock += dt;
		if (flameClock >= FlameFrameSeconds)
		{
			flameClock -= FlameFrameSeconds;
			flameStep = (flameStep + 1) % FlamePhases;
			Show(flamesFarRect, flamesFar, flameStep);
			Show(flamesNearRect, flamesNear, flameStep);
			Show(figureRect, figure, flameStep);
		}

		// The ward breathes: 0,1,2,1 rather than 0,1,2,0, so it turns round at the bottom
		// instead of snapping back to full brightness.
		wardClock += dt;
		if (wardClock >= WardPulseSeconds)
		{
			wardClock -= WardPulseSeconds;
			wardStep = (wardStep + 1) % (WardFrames * 2 - 2);
			Show(wardRect, ward, wardStep < WardFrames ? wardStep : WardFrames * 2 - 2 - wardStep);
		}

		TickEyes(dt);
	}

	/// <summary>
	/// Every pair on its own clock. A pair that comes due while MaxBlinkingPairs are already
	/// mid-blink, or within BlinkStagger of another starting, waits a moment and tries again -
	/// which is what keeps the wood from ever blinking together. Frame swaps and a Visible flag
	/// only: nothing is tinted or faded.
	/// </summary>
	private void TickEyes(float dt)
	{
		if (eyePairs == null)
			return;
		sinceBlinkStarted += dt;
		foreach (EyePair pair in eyePairs)
		{
			pair.Clock -= dt;
			if (pair.Clock > 0f)
				continue;

			if (pair.Step < 0)
			{
				if (pairsBlinking >= MaxBlinkingPairs || (!pair.Again && sinceBlinkStarted < BlinkStagger))
				{
					pair.Clock = rng.RandfRange(0.2f, 0.7f);
					continue;
				}
				pair.Step = 0;
				pairsBlinking++;
				sinceBlinkStarted = 0f;
			}
			else if (++pair.Step >= BlinkSteps.Length)
			{
				pair.Step = -1;
				pairsBlinking--;
				pair.Cell.Texture = pair.Frames[0];
				// Now and then straight into a second blink; otherwise a long wait.
				pair.Again = !pair.Again && rng.Randf() < DoubleBlinkChance;
				pair.Clock = pair.Again ? rng.RandfRange(0.12f, 0.2f) : rng.RandfRange(BlinkMinGap, BlinkMaxGap);
				continue;
			}

			int column = BlinkSteps[pair.Step];
			pair.Cell.Visible = column >= 0;
			if (column >= 0)
				pair.Cell.Texture = pair.Frames[Mathf.Min(column, pair.Frames.Length - 1)];
			int middle = BlinkSteps.Length / 2;
			pair.Clock = pair.Step < middle ? BlinkCloseSeconds
				: pair.Step == middle ? BlinkHoldSeconds : BlinkOpenSeconds;
		}
	}

	private static void Show(TextureRect rect, Texture2D[] frames, int index)
	{
		if (rect == null || frames == null)
			return;
		var next = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
		if (next != null && rect.Texture != next)
			rect.Texture = next;
	}

	/// <summary>
	/// PRESS ANY KEY is a separate texture rather than part of the title art, so it can arrive
	/// after the artwork has had a moment on its own and then breathe. It is a TextureRect and
	/// not a Label because the prompt is set in the same hand-drawn pixel face as the wordmark
	/// and there is no Godot font resource for that face.
	/// </summary>
	private void FadeInPrompt()
	{
		var prompt = GetNodeOrNull<CanvasItem>("TitleImage/Prompt");
		if (prompt == null)
			return;

		prompt.Modulate = new Color(1f, 1f, 1f, 0f);

		var appear = CreateTween();
		appear.TweenInterval(PromptDelay);
		appear.TweenProperty(prompt, "modulate:a", 1.0f, PromptFadeIn)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		// The pulse is a second tween started on completion rather than looped stages of the
		// first: SetLoops on the first would replay the delay and the fade every cycle.
		appear.Finished += () =>
		{
			if (!IsInstanceValid(prompt))
				return;
			var breath = CreateTween().SetLoops();
			breath.TweenProperty(prompt, "modulate:a", 0.42f, PromptBreath)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
			breath.TweenProperty(prompt, "modulate:a", 1.0f, PromptBreath)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		};
	}

	public override void _Input(InputEvent @event)
	{
		if (transitioned || updating) return;
		// A tap on the update strip's Download or Later is for the strip, not "press any button".
		if (@event is InputEventMouseButton press && updateBanner != null && IsInstanceValid(updateBanner) && updateBanner.Covers(press.Position))
			return;
		if ((@event is InputEventKey ek && ek.Pressed) || (@event is InputEventMouseButton mb && mb.Pressed) || (@event is InputEventJoypadButton jb && jb.Pressed))
		{
			transitioned = true;
			ShowMenu();
		}
	}

	/// <summary>Brings the main menu up on top of the artwork.</summary>
	/// <remarks>
	/// The menu is a CHILD of this screen rather than the next scene, and that is the whole change.
	/// Swapping scenes tore the artwork down and rebuilt it, which restarted every animated layer -
	/// so even though both screens showed the same picture, the handoff read as a flicker and the
	/// flames visibly jumped. Now the picture never goes away: the menu arrives on top of it, and
	/// the flames, eyes, ward pulse and blinking keep running behind it.
	///
	/// MainMenu hides its own background when it finds itself parented here (see
	/// MainMenu.LayoutAroundTitleArt), so there is exactly one copy of the art on screen.
	/// </remarks>
	private void ShowMenu()
	{
		if (menuInstance != null && IsInstanceValid(menuInstance))
			return;

		var scene = ResourceLoader.Load<PackedScene>("res://scenes/MainMenu.tscn");
		if (scene == null)
		{
			GD.PushError("TitleScreen: could not load res://scenes/MainMenu.tscn");
			return;
		}

		menuInstance = scene.Instantiate<Control>();
		AddChild(menuInstance);

		// The prompt and the menu say the same thing, so only one may be on screen. The breathing
		// tween is left running; it only touches modulate on a node nobody can see.
		var promptNode = GetNodeOrNull<CanvasItem>("TitleImage/Prompt");
		if (promptNode != null)
			promptNode.Visible = false;
	}
}
