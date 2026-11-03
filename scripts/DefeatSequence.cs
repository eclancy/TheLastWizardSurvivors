using Godot;
using System;
using TheOneHeMissed.scripts;

/// <summary>
/// The defeat (2026-11-02, Eric: "Defeat should have a dramatic animation where the game freezes, then
/// crash zooms onto the wizard and they fall, then it says defeat, and when the player clicks, the defeat
/// screen covers up."). The timeline and the card; the camera, bars and shake it drives are the run's
/// (Node2DGame.Defeat.cs).
/// </summary>
/// <remarks>
/// Real seconds from the killing blow, on a paused tree (the freeze):
///
///   0.00  FREEZE. The field stops dead on the frame of the blow; the wizard is held on the frame he was
///         struck in (his death row's first, which is still standing), the damage flash hangs over the
///         field, and the screen gives one small kick. player_death has already played (Player.TakeDamage).
///   0.55  CRASH ZOOM. The HUD and the touch pad go, the letterbox bars slide in, and the camera drives in
///         on him - the run's 0.85 to 2.0 in 0.6 s, eased in and out, overshooting by 4% and settling, with
///         a jolt as it lands - while speed lines in the dark wizard's cold violet rush in on him from the
///         edges and are gone as it lands. 2.0 because the body sprite is drawn at x2: at a camera zoom of 2
///         every art pixel of the close-up is exactly four canvas pixels. He is framed a tenth of the screen
///         above centre, so the word can sit below him.
///   1.45  THE FALL, after a held beat on the close-up. His own death row (every generated wizard has one,
///         tools/art/anim_sets.py: six frames folding to the ground), slowed to take 0.9 s; when its last
///         frame lands, player_fall, a puff of dust at his feet and a second jolt. The Test Wizard, Eric's own
///         drawing, has no death row and is never derived (CLAUDE.md), so he falls in code on whole pixels: a
///         two-pixel buckle, then one quarter turn onto the ground - no fractional rotation of pixel art.
///   2.95  DEFEAT, a beat after he lands. A band opens across the screen below him (the results band's stone
///         deep and gold rule, full bleed like the letterbox), the field dims behind it, and "Defeat" slams
///         onto it in the heading face - large, cloth red - with game_over and a third jolt.
///   3.35  ARMED. Only now does a press do anything, and only a press that starts now: a held button or a
///         thumb still on the stick sends no new press, so nothing is skipped by accident. Until then
///         every press and release is swallowed, so nothing under the card hears one either.
///   3.85  The prompt fades in under the word.
///   press The results screen covers the field from below (GameOverScreen.CoversFromBelow) and this goes.
///
/// THE CINEMATIC RULES (Eric, 2026-11-03; .ai/cutscenes.md). The first cut snapped in a third of a second on a
/// quartic ease-out - fastest on its first frame - 0.4 s after the blow, fell 0.08 s after the zoom landed and
/// said "Defeat" 0.4 s after the fall: a crash the eye could not follow and beats that ran into each other. Now
/// the freeze is held long enough to read (FreezeHold), the zoom eases in and out at under half the old peak
/// rate, the close-up holds before he falls (CloseUpHold) and a beat passes between his landing and the word
/// (WordBeat). RegressionChecks.ValidateCinematicRules holds all three above their floors.
///
/// Every shake goes through the run's ScreenShake, so Reduce Screen Shake (DisplaySettings.ShakeScale)
/// scales all four. The card is full bleed - a band edge to edge, like the letterbox - so SafeArea is not
/// applied, and it is not a menu: there is nothing to select, so MenuNavigator is not attached; any
/// press continues, which is also how a pad continues.
/// </remarks>
public partial class DefeatSequence : CanvasLayer
{
	// --- the timeline (real seconds from the killing blow) -----------------------------------------
	/// <summary>The freeze, held: the frozen field reads as the moment of the blow before anything moves (rule 3).</summary>
	public const float FreezeHold = 0.55f;
	/// <summary>The zoom in on him: long enough for the eye to ride it (rule 5), short enough to still be a crash.</summary>
	public const float ZoomSeconds = 0.60f;
	/// <summary>The close-up, held still before he falls.</summary>
	public const float CloseUpHold = 0.30f;
	public const float FallSeconds = 0.90f;
	/// <summary>The beat between his landing and the word.</summary>
	public const float WordBeat = 0.60f;

	public const float ZoomAt = FreezeHold;
	public const float FallAt = ZoomAt + ZoomSeconds + CloseUpHold;
	public const float TitleAt = FallAt + FallSeconds + WordBeat;
	public const float BandOpenSeconds = 0.14f;
	public const float SlamSeconds = 0.18f;
	public const float ArmAt = TitleAt + 0.40f;
	public const float PromptAt = TitleAt + 0.90f;

	// --- the camera ---------------------------------------------------------------------------------
	/// <summary>The run camera's zoom on the close-up: the x2 body sprite at exactly 4 canvas px an art pixel.</summary>
	public const float CloseZoom = 2.0f;
	/// <summary>How far past the close-up the crash zoom overshoots before it settles, as a share.</summary>
	private const float ZoomOvershoot = 0.04f;

	// The crash zoom's speed lines: rushing in on him from past the edge of the wide shot to a ring clear of his
	// body (the x2 sprite is 96 px tall), in the dark wizard's violet - the blow is his. World px, the 2 px grid.
	private const float ZoomLinesInner = 60f;
	private const float ZoomLinesOuter = 560f;
	private const int ZoomLinesCount = 26;
	/// <summary>The slow push-in that keeps the held shot alive after the snap, as a share, over DriftSeconds.</summary>
	private const float DriftZoom = 0.05f;
	private const float DriftSeconds = 2.6f;
	/// <summary>Where the wizard's feet-centre stands on the close-up, as a share of the canvas height from the top.</summary>
	private const float WizardScreenY = 0.40f;

	// Kicks, world pixels and seconds (ScreenShake scales them by DisplaySettings.ShakeScale).
	private const float BlowShakePx = 4f;
	private const float ZoomShakePx = 6f;
	private const float LandShakePx = 5f;
	private const float SlamShakePx = 7f;

	// --- the card -----------------------------------------------------------------------------------
	/// <summary>Where the band's middle sits, as a share of the canvas height, on a wide and a tall canvas.</summary>
	private const float BandYWide = 0.70f;
	private const float BandYTall = 0.63f;
	private const float BandHeightWide = 176f;
	private const float BandHeightTall = 200f;
	private const int TitleFontSize = 96;
	private const int TitleSpacing = 10;
	private const float SlamFromScale = 1.35f;
	private const float DimAlpha = 0.42f;
	private const float DimSeconds = 0.5f;
	// The results screen's band and inks (GameOverScreen), so the word is the same object there and here:
	// stone deep with a gold rule; cloth red at 4.8:1 on it, the meta ink at 7.6:1 (tools/art/ui_contrast.py).
	private static readonly Color BandColour = new(0.043f, 0.059f, 0.094f);
	private static readonly Color BandRule = new(0.541f, 0.416f, 0.118f);
	private static readonly Color DefeatInk = new(0.85f, 0.32f, 0.34f);
	private static readonly Color DimColour = new(0.02f, 0.03f, 0.05f);

	// --- what it is given ---------------------------------------------------------------------------
	public Node2DGame Run { get; set; }
	public Player Wizard { get; set; }
	public float StartZoomFactor { get; set; } = 1f;
	public Vector2 StartLean { get; set; }
	/// <summary>The press: brings the results screen down. Called once.</summary>
	public Action OnContinue { get; set; }

	/// <summary>
	/// For _UiShot: stop the clock at this many seconds, so one moment of the sequence (the zoom mid-way,
	/// the fall) can be photographed exactly rather than wherever a frame happens to land. Never set in play.
	/// </summary>
	public static float? DebugHoldAt { get; set; }

	/// <summary>Seconds since the killing blow.</summary>
	public float Elapsed { get; private set; }
	/// <summary>True once a press will continue: DEFEAT is fully shown and nothing has been pressed yet.</summary>
	public bool Armed => Elapsed >= ArmAt && !continued;
	public bool Continued => continued;

	private AnimatedSprite2D body;
	private bool fallRow;
	private int fallFrames;
	private bool fallStarted;
	private bool landed;
	private bool zoomStarted;
	private bool zoomLanded;
	private bool slamStarted;
	private bool slamLanded;
	private bool continued;
	// The Test Wizard's fall in code: where the sprite stood, and the two steps of it.
	private Vector2 bodyRest;
	private bool buckled;

	private Control card;
	private ColorRect dim;
	private Label title;
	private Label prompt;
	private float bandOpen;
	private bool narrow;

	public override void _Ready()
	{
		Layer = 9;
		ProcessMode = ProcessModeEnum.Always;
		narrow = ResponsiveLayout.IsNarrow(this);
		BuildCard();

		body = Wizard != null && IsInstanceValid(Wizard) ? Wizard.BodySprite : null;
		if (body != null)
		{
			// Held on the frame the blow landed. Player.TakeDamage has just started his death row, so this
			// is its first frame - still standing - and Play() later resumes it from there.
			body.ProcessMode = ProcessModeEnum.Always;
			fallRow = body.SpriteFrames != null && body.Animation == "death" && body.SpriteFrames.HasAnimation("death");
			fallFrames = fallRow ? body.SpriteFrames.GetFrameCount("death") : 0;
			body.Pause();
			bodyRest = body.Position;
		}
		Run?.ShakeScreen(BlowShakePx, 0.22f);
	}

	public override void _Process(double delta)
	{
		float d = (float)delta;
		if (DebugHoldAt is float hold && Elapsed + d > hold)
		{
			d = Mathf.Max(0f, hold - Elapsed);
			if (d <= 0f)
			{
				if (body != null && IsInstanceValid(body) && body.IsPlaying())
					body.Pause();
				return;
			}
		}
		Elapsed += d;
		float t = Elapsed;

		TickCamera(t, d);
		TickFall(t);
		TickCard(t);
	}

	// --- the camera ---------------------------------------------------------------------------------

	private void TickCamera(float t, float delta)
	{
		if (Run == null || !IsInstanceValid(Run))
			return;
		float baseZoom = Mathf.Max(0.01f, Run.DefeatBaseZoom);
		float close = CloseZoom / baseZoom;
		float zoomEnd = ZoomAt + ZoomSeconds;
		float factor = StartZoomFactor;
		float share = 0f;
		if (t >= ZoomAt)
		{
			if (!zoomStarted)
			{
				zoomStarted = true;
				Run.HideRunHudForDefeat();
				if (Wizard != null && IsInstanceValid(Wizard))
				{
					ElementColors.Ramp dark = ElementColors.GetRamp(Element.Darkness);
					Run.BurstSpeedLines(Wizard.GlobalPosition, ZoomLinesInner, ZoomLinesOuter, ZoomLinesCount, ZoomSeconds,
						dark.Hot, dark.Mid, 0xDEFEA7u, SpeedLines.Motion.Inward);
				}
			}
			float u = Mathf.Clamp((t - ZoomAt) / ZoomSeconds, 0f, 1f);
			// Eased in and out (rule 5: no snap of the camera), the second half steeper than the first, so it still
			// lands as a crash: a smoothstep on u^1.3 gathers for a beat and drives home.
			share = Smooth(Mathf.Pow(u, 1.3f));
			if (t < zoomEnd)
			{
				factor = Mathf.Lerp(StartZoomFactor, close * (1f + ZoomOvershoot), share);
			}
			else
			{
				if (!zoomLanded)
				{
					zoomLanded = true;
					Run.ShakeScreen(ZoomShakePx, 0.3f);
				}
				float after = t - zoomEnd;
				float drift = Smooth(Mathf.Clamp(after / DriftSeconds, 0f, 1f));
				factor = close * (1f + DriftZoom * drift + ZoomOvershoot * Mathf.Exp(-9f * after));
			}
		}

		// The lean that puts his feet-centre at WizardScreenY at this zoom, so he stays there as it changes.
		float canvasHeight = GetViewport().GetVisibleRect().Size.Y;
		var framed = new Vector2(0f, (0.5f - WizardScreenY) * canvasHeight / (baseZoom * factor));
		Vector2 lean = StartLean.Lerp(framed, share);
		Run.FilmDefeat(factor, lean, zoomStarted, delta);
	}

	private static float Smooth(float u) => u * u * (3f - 2f * u);

	// --- the fall -----------------------------------------------------------------------------------

	private void TickFall(float t)
	{
		if (body == null || !IsInstanceValid(body))
			return;
		if (!fallStarted && t >= FallAt)
		{
			fallStarted = true;
			if (fallRow)
			{
				// His own death row, slowed so the whole of it takes FallSeconds whatever its frame rate.
				float fps = Mathf.Max(1f, (float)body.SpriteFrames.GetAnimationSpeed("death"));
				body.SpeedScale = fallFrames / (fps * FallSeconds);
				body.Play("death");
			}
		}
		if (!fallStarted || landed)
			return;
		if (fallRow)
		{
			if (body.Frame >= fallFrames - 1)
				Land();
			return;
		}
		// No death row (the Test Wizard): a buckle of two art pixels, then a quarter turn to the ground,
		// landing when the drawn row would have.
		float landAt = FallAt + FallSeconds * 5f / 6f;
		if (!buckled && t >= FallAt + 0.35f)
		{
			buckled = true;
			body.Position = bodyRest + new Vector2(0f, 2f * body.Scale.Y);
		}
		if (t >= landAt)
		{
			LayDown();
			Land();
		}
	}

	private void Land()
	{
		landed = true;
		SfxPlayer.Global(SfxCatalog.PlayerFall);
		Run?.ShakeScreen(LandShakePx, 0.28f);
		if (Wizard != null && IsInstanceValid(Wizard) && Run != null && IsInstanceValid(Run))
		{
			var dust = new DefeatDust { Name = "DefeatDust" };
			Run.AddChild(dust);
			dust.GlobalPosition = Wizard.GlobalPosition + new Vector2(0f, FeetY());
		}
	}

	// The feet line under the player's origin, world px: the bottom of the body sprite's cell.
	private float FeetY()
	{
		if (body == null || body.SpriteFrames == null)
			return 40f;
		Texture2D frame = body.SpriteFrames.GetFrameTexture(body.Animation, body.Frame);
		float half = frame != null ? frame.GetHeight() * 0.5f : 24f;
		return bodyRest.Y + half * body.Scale.Y - 2f * body.Scale.Y;
	}

	// The Test Wizard laid on the ground: a quarter turn toward the way he faces, about the cell's centre,
	// then dropped so the bottom of the turned figure sits on the line his feet stood on. Whole art pixels
	// throughout - a quarter turn of a nearest-filtered sprite keeps every pixel square.
	private void LayDown()
	{
		bool toLeft = body.FlipH;
		body.Rotation = toLeft ? -Mathf.Pi * 0.5f : Mathf.Pi * 0.5f;
		body.Position = bodyRest;
		Texture2D frame = body.SpriteFrames?.GetFrameTexture(body.Animation, body.Frame);
		Image image = frame?.GetImage();
		if (image == null || image.IsEmpty())
			return;
		if (image.IsCompressed())
			image.Decompress();
		Rect2I used = image.GetUsedRect();
		Vector2 half = new Vector2(image.GetWidth(), image.GetHeight()) * 0.5f;
		float s = body.Scale.Y;
		float left = (used.Position.X - half.X) * s;
		float right = (used.End.X - half.X) * s;
		if (body.FlipH)
			(left, right) = (-right, -left);
		float feet = (used.End.Y - half.Y) * s;
		// A clockwise quarter turn sends a column x to the row y = x; anticlockwise to y = -x.
		float bottom = toLeft ? -left : right;
		float drop = Mathf.Round((feet - bottom) / s) * s;
		body.Position = bodyRest + new Vector2(0f, drop);
		body.Modulate = body.Modulate * new Color(0.78f, 0.78f, 0.82f);
	}

	// --- the card -----------------------------------------------------------------------------------

	private void BuildCard()
	{
		dim = new ColorRect
		{
			Name = "DefeatDim",
			Color = new Color(DimColour, 0f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		card = new Control { Name = "DefeatBand", MouseFilter = Control.MouseFilterEnum.Ignore };
		card.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		card.Draw += DrawBand;
		AddChild(card);

		float bandY = narrow ? BandYTall : BandYWide;
		float bandHeight = narrow ? BandHeightTall : BandHeightWide;

		title = new Label
		{
			Name = "DefeatTitle",
			Text = "Defeat",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		ResponsiveLayout.SetFont(title, ResponsiveLayout.TextRole.Display);
		title.AddThemeFontSizeOverride("font_size", TitleFontSize);
		// Spaced, as a word cut across a lintel is, and at Cinzel's heaviest (900 on the 'wght' axis, the
		// tag packed as HeadingFont.tres packs it): at 96 px the heading's 700 drew a hairline of a word.
		if (title.GetThemeFont("font") is Font heading)
		{
			var weight = new Godot.Collections.Dictionary { { 2003265652, 900 } };
			title.AddThemeFontOverride("font", new FontVariation { BaseFont = heading, SpacingGlyph = TitleSpacing, VariationOpentype = weight });
		}
		title.AddThemeColorOverride("font_color", DefeatInk);
		Place(title, bandY, -bandHeight * 0.09f, 132f);
		AddChild(title);

		prompt = new Label
		{
			Name = "DefeatPrompt",
			Text = TouchControls.ShouldEnable() ? "Tap to continue" : "Press any button",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		ResponsiveLayout.SetFont(prompt, ResponsiveLayout.TextRole.Label);
		prompt.AddThemeColorOverride("font_color", CodexRows.Stone.InkMeta);
		Place(prompt, bandY, bandHeight * 0.5f - 30f, 36f);
		AddChild(prompt);
	}

	// A full-width label whose middle is <offset> canvas px from the band's middle.
	private static void Place(Control control, float bandY, float offset, float height)
	{
		control.AnchorLeft = 0f;
		control.AnchorRight = 1f;
		control.AnchorTop = control.AnchorBottom = bandY;
		control.OffsetLeft = 0f;
		control.OffsetRight = 0f;
		control.OffsetTop = offset - height * 0.5f;
		control.OffsetBottom = offset + height * 0.5f;
	}

	private void TickCard(float t)
	{
		if (t < TitleAt)
			return;
		float was = bandOpen;
		bandOpen = Mathf.Clamp((t - TitleAt) / BandOpenSeconds, 0f, 1f);
		if (bandOpen != was)
			card.QueueRedraw();
		dim.Color = new Color(DimColour, DimAlpha * Smooth(Mathf.Clamp((t - TitleAt) / DimSeconds, 0f, 1f)));

		float slamAt = TitleAt + BandOpenSeconds;
		if (t >= slamAt)
		{
			if (!slamStarted)
			{
				slamStarted = true;
				SfxPlayer.Global(SfxCatalog.GameOver);
			}
			float u = Mathf.Clamp((t - slamAt) / SlamSeconds, 0f, 1f);
			float eased = 1f - (1f - u) * (1f - u);
			title.PivotOffset = title.Size * 0.5f;
			title.Scale = Vector2.One * Mathf.Lerp(SlamFromScale, 1f, eased);
			title.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(u * 2.5f, 0f, 1f));
			if (u >= 1f && !slamLanded)
			{
				slamLanded = true;
				Run?.ShakeScreen(SlamShakePx, 0.35f);
			}
		}

		if (t >= PromptAt && !continued)
		{
			// In over a third of a second, then breathing, so it reads as waiting rather than as text.
			float since = t - PromptAt;
			float fadeIn = Mathf.Clamp(since / 0.35f, 0f, 1f);
			float breath = 0.75f + 0.25f * Mathf.Cos(since * 3.2f);
			prompt.Modulate = new Color(1f, 1f, 1f, fadeIn * breath);
		}
	}

	// The band: stone deep, opening from its middle on whole pixels, with the results band's gold rule
	// along both edges.
	private void DrawBand()
	{
		if (bandOpen <= 0f || card == null)
			return;
		Vector2 canvas = card.Size;
		float middle = Mathf.Round(canvas.Y * (narrow ? BandYTall : BandYWide));
		float full = narrow ? BandHeightTall : BandHeightWide;
		float eased = 1f - (1f - bandOpen) * (1f - bandOpen);
		float half = Mathf.Round(full * 0.5f * eased);
		if (half < 1f)
			return;
		card.DrawRect(new Rect2(0f, middle - half, canvas.X, half * 2f), BandColour);
		card.DrawRect(new Rect2(0f, middle - half, canvas.X, 2f), BandRule);
		card.DrawRect(new Rect2(0f, middle + half - 2f, canvas.X, 2f), BandRule);
	}

	// --- the press ----------------------------------------------------------------------------------

	public override void _Input(InputEvent @event)
	{
		if (continued || !IsButton(@event, out bool pressed))
			return;
		// Swallowed whether or not it continues: before the card is armed nothing under it may hear a
		// press, and the press that continues must not also reach the screen that comes up.
		GetViewport().SetInputAsHandled();
		if (pressed && Elapsed >= ArmAt)
			Continue();
	}

	// A press or release of anything a player presses: a key, a mouse button, a finger, a pad button. Not
	// a stick, a wheel or the mouse moving; not a key's repeat; and not F11 or Alt+Enter, which belong to
	// the window (DisplayProfile) on every screen.
	internal static bool IsButton(InputEvent @event, out bool pressed)
	{
		pressed = false;
		switch (@event)
		{
			case InputEventKey key:
				if (key.Echo || key.Keycode == Key.F11 || (key.AltPressed && key.Keycode is Key.Enter or Key.KpEnter))
					return false;
				pressed = key.Pressed;
				return true;
			case InputEventMouseButton mouse:
				if (mouse.ButtonIndex is not (MouseButton.Left or MouseButton.Right or MouseButton.Middle))
					return false;
				pressed = mouse.Pressed;
				return true;
			case InputEventScreenTouch touch:
				pressed = touch.Pressed;
				return true;
			case InputEventJoypadButton pad:
				pressed = pad.Pressed;
				return true;
			default:
				return false;
		}
	}

	/// <summary>The press. Public for a harness that photographs the screen after it without input.</summary>
	public void Continue()
	{
		if (continued)
			return;
		continued = true;
		prompt.Modulate = new Color(1f, 1f, 1f, 0f);
		SfxPlayer.Global(SfxCatalog.UiOpen);
		OnContinue?.Invoke();
		// The results screen (layer 10) slides up over this card; it goes once it is covered.
		GetTree().CreateTimer(GameOverScreen.CoverSeconds + 0.1, processAlways: true).Timeout += () =>
		{
			if (IsInstanceValid(this))
				QueueFree();
		};
	}

	/// <summary>
	/// The dust the wizard raises where he lands (DefeatSequence). A dozen puffs thrown out along the
	/// ground both ways and lifting a little, drawn as squares on the 2-pixel art grid of the x2 sprite and
	/// stepping down three opaque tones of dust as they age; gone in about 0.7 s. Processes through the
	/// paused tree, like the sequence.
	/// </summary>
	private partial class DefeatDust : Node2D
	{
		private const int Count = 12;
		private const float Grid = 2f;
		private static readonly Color DustLight = new(0.72f, 0.68f, 0.60f);
		private static readonly Color DustMid = new(0.62f, 0.58f, 0.52f);
		private static readonly Color DustDark = new(0.42f, 0.40f, 0.38f);

		private readonly Vector2[] from = new Vector2[Count];
		private readonly Vector2[] velocity = new Vector2[Count];
		private readonly float[] life = new float[Count];
		private readonly float[] size = new float[Count];
		private float age;

		public override void _Ready()
		{
			ProcessMode = ProcessModeEnum.Always;
			ZAsRelative = false;
			ZIndex = DrawLayers.ForemostAir;
			var rng = new RandomNumberGenerator { Seed = 0x5D1E5EED };
			for (int i = 0; i < Count; i++)
			{
				float side = i % 2 == 0 ? -1f : 1f;
				from[i] = new Vector2(side * rng.RandfRange(4f, 22f), -rng.RandfRange(0f, 4f));
				velocity[i] = new Vector2(side * rng.RandfRange(36f, 96f), -rng.RandfRange(6f, 24f));
				life[i] = rng.RandfRange(0.45f, 0.75f);
				size[i] = rng.RandfRange(4f, 8f);
			}
		}

		public override void _Process(double delta)
		{
			age += (float)delta;
			if (age > 0.8f)
			{
				QueueFree();
				return;
			}
			QueueRedraw();
		}

		public override void _Draw()
		{
			for (int i = 0; i < Count; i++)
			{
				float u = age / life[i];
				if (u >= 1f)
					continue;
				// Slowing as it spreads: the air takes it.
				float travel = 1f - (1f - u) * (1f - u);
				Vector2 at = from[i] + velocity[i] * life[i] * 0.6f * travel;
				float s = Mathf.Max(Grid, Mathf.Round(size[i] * (1f + 0.6f * u) / Grid) * Grid);
				at = new Vector2(Mathf.Round(at.X / Grid) * Grid, Mathf.Round(at.Y / Grid) * Grid);
				Color tone = u < 0.35f ? DustLight : u < 0.7f ? DustMid : DustDark;
				var colour = new Color(tone, 0.75f * (1f - u * u));
				// Two squares a puff, the second smaller and up toward the trailing side, so a puff is a
				// lump and not a tile.
				DrawRect(new Rect2(at - new Vector2(s * 0.5f, s), new Vector2(s, s)), colour);
				float k = Mathf.Max(Grid, Mathf.Round(s * 0.6f / Grid) * Grid);
				float trailing = velocity[i].X < 0f ? 1f : -1f;
				float x = trailing > 0f ? at.X + s * 0.5f - k * 0.5f : at.X - s * 0.5f - k * 0.5f;
				DrawRect(new Rect2(x, at.Y - s - k * 0.5f, k, k), colour);
			}
		}
	}
}
