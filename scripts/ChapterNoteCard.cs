using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

/// <summary>
/// The note the player's wizard writes on first walking into a chapter (ChapterNotes): a scrap of paper,
/// the chapter's name and two or three lines written in as if by hand, for about ten seconds or until put
/// away. It never pauses the run.
/// </summary>
/// <remarks>
/// WHERE. On a PC the lower right - the opposite corner to the tracked quests, out of the band above the
/// player's head where the run's opening writes and the achievement plate stands. On a phone the lower
/// half belongs to the thumb, so it hangs just under the HUD band instead - and CONDENSED (in-flight 70,
/// Eric 2026-11-09 and 10: "it is huge on mobile and covers the wizard"; <see cref="Condense"/>): the place's
/// first line, the first clue, and how many more.
///
/// PUT AWAY by a click or tap on it, or A / Enter / Space. Layer 7, above the touch stick (5), so a tap on
/// the paper reaches it - and only the paper takes the mouse; the rest of the screen is the stick's.
/// Pausable: a level-up arriving while it is up freezes it under the menu, and its keys are not heard
/// while the tree is paused, so A on a level-up card never puts the note away as well.
///
/// THE MATERIAL. Paper. (In the Sketchbook, whose ground was paper already, it was a slate of floor stone
/// until the chapter was removed on 2026-11-09.)
///
/// THE HAND. The engine sans in ink on the page: the legibility rule keeps prose out of display faces
/// (.ai/level-up-screen.md 4a), and a hand that cannot be read at a glance mid-run is worse than none. The
/// heading is the bold Cinzel every heading is.
///
/// THE CLUES (Eric, 2026-11-01: "the clues should be separated from the other text, in italics"). Under
/// the intro, past a short rule, one line for each thing the chapter still holds for this save
/// (ChapterNotes.CluesFor), set in italics - the sans slanted by a fifth, as the whole note was before; the
/// intro now stands upright, so the clues are the only slanted words on it. A returning player sees the
/// card again while a clue remains, with the clues alone (the place is described the first time only).
/// </remarks>
public partial class ChapterNoteCard : CanvasLayer
{
	private const float WriteSecondsPerChar = 0.028f;
	private const float HoldSeconds = 7.5f;
	// On a phone it is over the run's upper half: read and gone (in-flight 70). A tap still puts it away sooner.
	private const float NarrowHoldSeconds = 4f;
	// How long it stays, once written, when another message is waiting for the screen.
	private const float ReadSecondsWhenWaitedFor = 3f;
	private const float FadeSeconds = 0.6f;
	private const float WideWidth = 430f;
	private const float NarrowSide = 40f;
	private const float Edge = 16f;

	// The page: the Vellum register's face and inks (CodexRows.Vellum, measured by ui_contrast.py).
	private static readonly Color PaperFace = CodexRows.Vellum.Face;
	private static readonly Color PaperEdge = CodexRows.Vellum.Edge;
	private static readonly Color PaperInk = CodexRows.Vellum.Ink;
	private static readonly Color PaperHeading = CodexRows.Vellum.Heading;
	private static readonly Color Shadow = new(0.02f, 0.025f, 0.04f, 0.5f);

	private StageDefinition stage = null!;
	private List<string> lines = new();
	private List<string> clues = new();
	private Control rule;
	private Label firstClue;
	private PanelContainer page = null!;
	private readonly List<Label> written = new();
	private readonly List<Vector2> outline = new();
	private bool dismissing;
	private float clock;
	private float writeSeconds;
	private float holdSeconds = HoldSeconds;

	/// <summary>
	/// Shows a chapter's note over the run and returns it: its intro on the first visit
	/// (<paramref name="intro"/>), and under it the clues this save still has to find there.
	/// </summary>
	public static ChapterNoteCard Show(Node parent, StageDefinition stage, SaveData save, bool intro = true)
	{
		var card = new ChapterNoteCard
		{
			Name = "ChapterNoteCard",
			stage = stage,
			lines = intro ? new List<string>(ChapterNotes.IntroFor(stage.Id)) : new List<string>(),
			clues = ChapterNotes.CluesFor(stage.Id, save),
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
			foreach (string clue in clues)
				yield return clue;
		}
	}

	public override void _Ready()
	{
		Layer = 7;
		ProcessMode = ProcessModeEnum.Pausable;
		bool narrow = ResponsiveLayout.IsNarrow(this);
		if (narrow)
		{
			Condense();
			holdSeconds = NarrowHoldSeconds;
		}

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
		heading.AddThemeColorOverride("font_color", PaperHeading);
		column.AddChild(heading);

		var italic = new FontVariation
		{
			BaseFont = ThemeDB.FallbackFont,
			// A slant of a fifth: italics, not a lean - the letters keep their own widths.
			VariationTransform = new Transform2D(1f, 0.2f, 0f, 1f, 0f, 0f),
		};
		int total = 0;
		Label AddLine(string text, bool slanted)
		{
			var line = new Label
			{
				// The last two words bound together, so a wrap never leaves one word alone on a line
				// (Eric, 2026-11-04 - he saw it on these notes first). Words keeps the raw lines.
				Text = ResponsiveLayout.NoWidow(text),
				MouseFilter = Control.MouseFilterEnum.Ignore,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				VisibleRatio = 0f,
			};
			ResponsiveLayout.SetFont(line, ResponsiveLayout.TextRole.Body);
			if (slanted)
				line.AddThemeFontOverride("font", italic);
			line.AddThemeColorOverride("font_color", PaperInk);
			column.AddChild(line);
			written.Add(line);
			total += text.Length;
			return line;
		}
		foreach (string text in lines)
			AddLine(text, false);
		if (clues.Count > 0)
		{
			// The clues are set apart: a short rule in the ink, with room above it, then the italic lines.
			if (lines.Count > 0)
			{
				rule = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(0f, 14f) };
				Color ruleInk = new Color(PaperInk, 0.55f);
				rule.Draw += () => rule.DrawRect(new Rect2(0f, 6f, 56f, 2f), ruleInk);
				rule.Modulate = new Color(1f, 1f, 1f, 0f);
				column.AddChild(rule);
			}
			foreach (string clue in clues)
			{
				Label line = AddLine(clue, true);
				firstClue ??= line;
			}
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

	public override void _ExitTree()
	{
		HudKeepOut.Clear(this);
		// The note holds the opening's turn (Node2DGame.ShowChapterNote); the plate waits for it.
		Announcements.Release(this);
	}

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
		// The rule is drawn as the hand reaches the clues.
		if (rule != null && firstClue != null)
			rule.Modulate = new Color(1f, 1f, 1f, firstClue.VisibleRatio > 0f ? 1f : 0f);
		if (!dismissing)
			HudKeepOut.Set(this, page.GetGlobalRect().Grow(6f));
		if (!dismissing && clock > 0.25f + writeSeconds + holdSeconds)
			Dismiss();
		// It holds the screen's turn (Announcements). Once it is written and has been up long enough to
		// read, a message waiting behind it - an achievement earned in the first seconds - may have it.
		if (!dismissing && clock > 0.25f + writeSeconds + ReadSecondsWhenWaitedFor
			&& Announcements.Holds(this) && Announcements.AnyWaiting)
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

	// ON A PHONE, CONDENSED (in-flight 70). The card hangs from under the HUD band (y 196 of the 1280-tall canvas) and
	// grows down toward the wizard, who stands at the canvas centre (y 640): written out in full - the heading, the
	// place's two lines, a rule and up to three clues, each line wrapping to two at the phone's 24-unit body size - it
	// reached him. So on a phone it carries the heading, the place's FIRST line, and the FIRST CLUE as written with the
	// rest as a count under it ("And two more things are hidden here.") - a lone clue as it is - and it holds 4 s, not
	// 7.5. Until 2026-11-10 the clues were a count alone whenever there were two or more, so a phone never showed a hint
	// at all (the fourth batch's follow-up). The PC's card, in the lower right corner, never comes near him and is as it was.
	private void Condense()
	{
		if (lines.Count > 1)
			lines = new List<string> { lines[0] };
		if (clues.Count > 1)
			clues = new List<string> { clues[0], MoreLine(clues.Count - 1) };
	}

	// In the note's own voice, and like every clue it says that something is there and never what (ChapterNotes).
	private static string MoreLine(int more) =>
		more == 1 ? "And one more thing is hidden here." : $"And {CountWord(more)} more things are hidden here.";

	private static string CountWord(int count) => count switch
	{
		2 => "two",
		3 => "three",
		4 => "four",
		5 => "five",
		_ => count.ToString(),
	};

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
	//
	// Every outline must triangulate, or DrawColoredPolygon draws NO paper and says nothing - only the
	// rim's polyline is left, round dark ink on the bare ground. That is what the Fallow Fields' note did
	// (Eric, 2026-11-01: "unreadable because of no background"): its seed bit a corner so that the walk
	// doubled back on itself, while the Forest's seed happened to come out clean. So the corners stay on
	// the corners, a point the rounding repeats is dropped, and an outline that still will not triangulate
	// is torn again from the next seed, and in the end left as the plain rectangle.
	private void BuildOutline()
	{
		outline.Clear();
		Vector2 size = page.Size;
		if (size.X < 4f || size.Y < 4f)
			return;
		for (int attempt = 0; attempt < OutlineAttempts; attempt++)
		{
			TearOutline(size, (ulong)(stage.Index * 131 + 17 + attempt * 7919));
			if (Geometry2D.TriangulatePolygon(outline.ToArray()).Length > 0)
				return;
		}
		outline.Clear();
		outline.Add(Vector2.Zero);
		outline.Add(new Vector2(size.X, 0f));
		outline.Add(size);
		outline.Add(new Vector2(0f, size.Y));
	}

	private const int OutlineAttempts = 4;

	private void TearOutline(Vector2 size, ulong seed)
	{
		outline.Clear();
		var rng = new RandomNumberGenerator { Seed = seed };
		const float Step = 8f;
		void Walk(Vector2 from, Vector2 to, Vector2 inward, bool torn)
		{
			float length = from.DistanceTo(to);
			Vector2 along = (to - from).Normalized();
			for (float d = 0f; d < length; d += Step)
			{
				// The corner itself is never bitten: a bitten corner is where the walk folded over.
				float bite = d == 0f ? 0f : torn ? rng.RandiRange(0, 3) : rng.RandiRange(0, 1);
				Vector2 point = (from + along * d + inward * bite).Round();
				if (outline.Count == 0 || outline[^1] != point)
					outline.Add(point);
			}
		}
		// The top edge is the torn one, as a leaf torn from a book; the other three only uneven.
		Walk(new Vector2(0f, 0f), new Vector2(size.X, 0f), Vector2.Down, true);
		Walk(new Vector2(size.X, 0f), new Vector2(size.X, size.Y), Vector2.Left, false);
		Walk(new Vector2(size.X, size.Y), new Vector2(0f, size.Y), Vector2.Up, false);
		Walk(new Vector2(0f, size.Y), new Vector2(0f, 0f), Vector2.Right, false);
		if (outline.Count > 1 && outline[^1] == outline[0])
			outline.RemoveAt(outline.Count - 1);
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
		page.DrawColoredPolygon(points, PaperFace);
		var closed = new Vector2[points.Length + 1];
		points.CopyTo(closed, 0);
		closed[points.Length] = points[0];
		page.DrawPolyline(closed, PaperEdge, 2f);
	}
}
