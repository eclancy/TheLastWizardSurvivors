using Godot;
using System;
using System.Collections.Generic;
using WizardSurvivors.scripts;

/// <summary>
/// The note the player's wizard writes on first walking into a chapter (ChapterNotes): a scrap of paper,
/// the chapter's name and two or three lines written in as if by hand, for about ten seconds or until put
/// away. It never pauses the run.
/// </summary>
/// <remarks>
/// WHERE. On a PC the lower right - the opposite corner to the tracked quests, out of the band above the
/// player's head where the run's opening writes and the achievement plate stands. On a phone the lower
/// half belongs to the thumb, so it hangs just under the HUD band instead.
///
/// PUT AWAY by a click or tap on it, or A / Enter / Space. Layer 7, above the touch stick (5), so a tap on
/// the paper reaches it - and only the paper takes the mouse; the rest of the screen is the stick's.
/// Pausable: a level-up arriving while it is up freezes it under the menu, and its keys are not heard
/// while the tree is paused, so A on a level-up card never puts the note away as well.
///
/// THE MATERIAL. Paper, except in the Sketchbook, whose ground is paper already: there it is a slate of
/// floor stone with the stone register's inks, so no screen shows the note on its own material.
///
/// THE HAND. The engine sans, slanted (a FontVariation's transform), in ink on the page: the legibility
/// rule keeps prose out of display faces (.ai/level-up-screen.md 4a), and a hand that cannot be read at a
/// glance mid-run is worse than none. The heading is the bold Cinzel every heading is.
/// </remarks>
public partial class ChapterNoteCard : CanvasLayer
{
	private const float WriteSecondsPerChar = 0.028f;
	private const float HoldSeconds = 7.5f;
	private const float FadeSeconds = 0.6f;
	private const float WideWidth = 430f;
	private const float NarrowSide = 40f;
	private const float Edge = 16f;

	// The page: the Vellum register's face and inks (CodexRows.Vellum, measured by ui_contrast.py).
	private static readonly Color PaperFace = CodexRows.Vellum.Face;
	private static readonly Color PaperEdge = CodexRows.Vellum.Edge;
	private static readonly Color PaperInk = CodexRows.Vellum.Ink;
	private static readonly Color PaperHeading = CodexRows.Vellum.Heading;
	// The slate, for the Sketchbook: floor stone and the stone register's inks.
	private static readonly Color SlateFace = CodexRows.Stone.Face;
	private static readonly Color SlateEdge = CodexRows.Stone.Edge;
	private static readonly Color SlateInk = CodexRows.Stone.Ink;
	private static readonly Color SlateHeading = CodexRows.Stone.Heading;
	private static readonly Color Shadow = new(0.02f, 0.025f, 0.04f, 0.5f);

	private StageDefinition stage = null!;
	private List<string> lines = new();
	private bool slate;
	private PanelContainer page = null!;
	private readonly List<Label> written = new();
	private readonly List<Vector2> outline = new();
	private bool dismissing;
	private float clock;
	private float writeSeconds;

	/// <summary>Shows a chapter's note over the run and returns it.</summary>
	public static ChapterNoteCard Show(Node parent, StageDefinition stage, SaveData save)
	{
		var card = new ChapterNoteCard
		{
			Name = "ChapterNoteCard",
			stage = stage,
			lines = ChapterNotes.LinesFor(stage.Id, save),
			slate = stage.EnvironmentKind == StageEnvironmentKind.Sketchbook,
		};
		parent.AddChild(card);
		return card;
	}

	/// <summary>The words on the card, for the name check (_UiShot --name-audit).</summary>
	public IEnumerable<string> Words
	{
		get
		{
			yield return stage?.DisplayName ?? string.Empty;
			foreach (string line in lines)
				yield return line;
		}
	}

	public override void _Ready()
	{
		Layer = 7;
		ProcessMode = ProcessModeEnum.Pausable;
		bool narrow = ResponsiveLayout.IsNarrow(this);

		var root = new Control { Name = "NoteRoot", MouseFilter = Control.MouseFilterEnum.Ignore };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		page = new PanelContainer { Name = "NotePage", MouseFilter = Control.MouseFilterEnum.Stop };
		page.AddThemeStyleboxOverride("panel", new StyleBoxEmpty
		{
			ContentMarginLeft = 26, ContentMarginRight = 26, ContentMarginTop = 18, ContentMarginBottom = 20,
		});
		if (narrow)
		{
			page.AnchorLeft = 0f;
			page.AnchorRight = 1f;
			page.AnchorTop = 0f;
			page.AnchorBottom = 0f;
			page.OffsetLeft = NarrowSide;
			page.OffsetRight = -NarrowSide;
			page.OffsetTop = TouchControls.DefaultHudBandHeight + 20f;
			page.OffsetBottom = page.OffsetTop;
		}
		else
		{
			page.AnchorLeft = 1f;
			page.AnchorRight = 1f;
			page.AnchorTop = 1f;
			page.AnchorBottom = 1f;
			page.OffsetLeft = -Edge - WideWidth;
			page.OffsetRight = -Edge;
			page.OffsetTop = -Edge;
			page.OffsetBottom = -Edge;
			page.GrowVertical = Control.GrowDirection.Begin;
		}
		page.Draw += PaintPage;
		page.Resized += () => { BuildOutline(); page.QueueRedraw(); };
		page.GuiInput += OnPageInput;
		root.AddChild(page);

		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 6);
		page.AddChild(column);

		var heading = new Label { Text = stage.DisplayName, MouseFilter = Control.MouseFilterEnum.Ignore };
		ResponsiveLayout.SetFont(heading, ResponsiveLayout.TextRole.Title);
		heading.AddThemeColorOverride("font_color", slate ? SlateHeading : PaperHeading);
		column.AddChild(heading);

		var hand = new FontVariation
		{
			BaseFont = ThemeDB.FallbackFont,
			// A slant of a fifth: a hand, not an oblique - the letters keep their own widths.
			VariationTransform = new Transform2D(1f, 0.2f, 0f, 1f, 0f, 0f),
		};
		int total = 0;
		foreach (string text in lines)
		{
			var line = new Label
			{
				Text = text,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				VisibleRatio = 0f,
			};
			ResponsiveLayout.SetFont(line, ResponsiveLayout.TextRole.Body);
			line.AddThemeFontOverride("font", hand);
			line.AddThemeColorOverride("font_color", slate ? SlateInk : PaperInk);
			column.AddChild(line);
			written.Add(line);
			total += text.Length;
		}
		writeSeconds = total * WriteSecondsPerChar;

		// Clear of the phone's corners and camera hole (SafeArea), as every screen over a run is.
		SafeArea.ApplyTo(page);

		// Slides up a little as it appears.
		page.Modulate = new Color(1f, 1f, 1f, 0f);
		page.Position += new Vector2(0f, 12f);
		Tween tween = CreateTween().SetParallel();
		tween.TweenProperty(page, "modulate:a", 1f, 0.3f);
		tween.TweenProperty(page, "position:y", page.Position.Y - 12f, 0.35f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
	}

	public override void _ExitTree() => HudKeepOut.Clear(this);

	public override void _Process(double delta)
	{
		clock += (float)delta;
		// Written line by line, each at the hand's pace.
		float elapsed = Mathf.Max(0f, clock - 0.25f);
		foreach (Label line in written)
		{
			float seconds = Mathf.Max(0.2f, line.Text.Length * WriteSecondsPerChar);
			line.VisibleRatio = Mathf.Clamp(elapsed / seconds, 0f, 1f);
			elapsed -= seconds;
		}
		if (!dismissing)
			HudKeepOut.Set(this, page.GetGlobalRect().Grow(6f));
		if (!dismissing && clock > 0.25f + writeSeconds + HoldSeconds)
			Dismiss();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (dismissing || !@event.IsActionPressed("ui_accept"))
			return;
		GetViewport().SetInputAsHandled();
		Dismiss();
	}

	// The mouse button alone: a finger arrives as a touch AND as the emulated click, and listening to both
	// would act twice (CLAUDE.md, "Touch").
	private void OnPageInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
		{
			page.AcceptEvent();
			Dismiss();
		}
	}

	/// <summary>Puts the note away: written out in full if it was not, then faded.</summary>
	public void Dismiss()
	{
		if (dismissing)
			return;
		dismissing = true;
		HudKeepOut.Clear(this);
		page.MouseFilter = Control.MouseFilterEnum.Ignore;
		Tween tween = CreateTween();
		tween.TweenProperty(page, "modulate:a", 0f, FadeSeconds);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	// A torn scrap: the rectangle's edges walked in short steps, each pulled in by a pixel or two, on the
	// whole-pixel grain the rest of the art is drawn on. Seeded by the chapter, so a note is always the
	// same scrap.
	private void BuildOutline()
	{
		outline.Clear();
		Vector2 size = page.Size;
		if (size.X < 4f || size.Y < 4f)
			return;
		var rng = new RandomNumberGenerator { Seed = (ulong)(stage.Index * 131 + 17) };
		const float Step = 8f;
		void Walk(Vector2 from, Vector2 to, Vector2 inward, bool torn)
		{
			float length = from.DistanceTo(to);
			Vector2 along = (to - from).Normalized();
			for (float d = 0f; d < length; d += Step)
			{
				float bite = torn ? rng.RandiRange(0, 3) : rng.RandiRange(0, 1);
				outline.Add((from + along * d + inward * bite).Round());
			}
		}
		// The top edge is the torn one, as a leaf torn from a book; the other three only uneven.
		Walk(new Vector2(0f, 0f), new Vector2(size.X, 0f), Vector2.Down, true);
		Walk(new Vector2(size.X, 0f), new Vector2(size.X, size.Y), Vector2.Left, false);
		Walk(new Vector2(size.X, size.Y), new Vector2(0f, size.Y), Vector2.Up, false);
		Walk(new Vector2(0f, size.Y), new Vector2(0f, 0f), Vector2.Right, false);
	}

	private void PaintPage()
	{
		if (outline.Count < 3)
			BuildOutline();
		if (outline.Count < 3)
			return;
		Vector2[] points = outline.ToArray();
		var shadow = new Vector2[points.Length];
		for (int i = 0; i < points.Length; i++)
			shadow[i] = points[i] + new Vector2(4f, 5f);
		page.DrawColoredPolygon(shadow, Shadow);
		page.DrawColoredPolygon(points, slate ? SlateFace : PaperFace);
		var closed = new Vector2[points.Length + 1];
		points.CopyTo(closed, 0);
		closed[points.Length] = points[0];
		page.DrawPolyline(closed, slate ? SlateEdge : PaperEdge, 2f);
	}
}
