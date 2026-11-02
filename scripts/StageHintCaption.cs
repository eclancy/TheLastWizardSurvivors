using Godot;
using WizardSurvivors.scripts;

// The words of a chapter hint (StageHintDirector), fading in on the side of the screen the sound came
// from, after the cue (StageHintCue) has started to drift in.
//
// NOT A BUBBLE (Eric, playing 0.10.0: "The clue text should show up on the screen, not in a text
// bubble, but fading in as another hint"). It was a near-black band with a rim in the far-tell colour
// and a speech bubble's tail. Now it is the line itself, set like the onboarding tip, over a soft
// dark plate that is drawn the contract's way: a capsule of occlusion, solid behind the words and
// dissolving outward through the Bayer matrix, so it reads as a shadow the words cast rather than a box. No rim,
// no tail - the cue in the world says where it came from, and does it better.
//
// WHY A PLATE AT ALL. Outline-only text was tried on the onboarding tip and Eric found it unreadable
// on the Fallow Fields' pale ground; the Sketchbook's paper is paler still. The plate is what holds
// the contrast floor (.ai/level-up-screen.md 4a, 4.5:1 for anything read as text) on every ground:
// the ink is measured against the plate, and the plate is 86% occlusion over whatever is under it.
// The outline stays too, for the glyph edges.
//
// Placed every frame from the player's position on the canvas and the direction to the source:
// pushed out along that line to the edge of a safe inner rectangle - under the HUD band (the touch
// stick's reserved band on a phone), clear of a phone's bottom third where the thumb sits, inset by
// the safe area - and never over the wizard himself. It takes no input at all (MouseFilter.Ignore,
// and so does its label): a finger on it lands on the touch stick beneath as if it were not there.
public partial class StageHintCaption : Control
{
	// The cue leads: the marks are already coming in when the words begin to appear.
	public const float DelaySeconds = 0.45f;
	private const float FadeInSeconds = 0.8f;
	private const float HoldSeconds = 4.4f;
	private const float FadeOutSeconds = 1.0f;

	private const float PadX = 16f;
	private const float PadY = 10f;
	// How far in from the safe rectangle's edge the line sits, toward the player: past the band the
	// cue's marks come in on (StageHintCue), and a little more.
	// The marks start half a mark in from the edge and drift a little; the plate dissolves past the words.
	public const float CueBand = 74f;
	// Clear air left between the line's plate and the tracked quests or the chapter note.
	private const float KeepOutMargin = 10f;
	private const float EdgeGap = 10f;
	// Clear of the HUD across the top on a PC: the timer, the boss bar's slot, the objective line and
	// the onboarding tip all end by 128; the loadout column on the left by about 150.
	private const float WideTop = 150f;
	private const float WideBottom = 40f;
	private const float Side = 18f;
	// On a phone: under the touch stick's band (TouchControls.DefaultHudBandHeight) plus a little,
	// and out of the bottom 30% of the screen, where the thumb holding the stick is.
	private const float NarrowBandGap = 18f;
	private const float NarrowThumbShare = 0.30f;
	// One line on a PC wherever it fits - a subtitle, not a paragraph in a box.
	private const float WideMaxTextWidth = 640f;
	private const float NarrowMaxTextWidth = 470f;
	// The wizard at the centre of the screen, with his bow out: the line is never put over him.
	private static readonly Vector2 PlayerKeepOut = new Vector2(78f, 96f);

	// The plate: occlusion, solid a little beyond the words, then dissolving over PlateFall.
	private const float PlateCoreX = 4f;
	private const float PlateCoreY = 4f;
	private const float PlateFall = 26f;
	private const float PlateAlpha = 0.86f;
	// One plate texel is this many canvas units, so the dither is on the same chunky grid as the art.
	private const int PlateTexel = 2;
	private static readonly Color Occlusion = new Color(0.020f, 0.027f, 0.047f);   // #05070C

	private static readonly Color SpeechInk = new Color(0.97f, 0.93f, 0.84f);
	private static readonly Color SoundInk = new Color(0.84f, 0.89f, 0.94f);

	private Label label;
	private ImageTexture plate;
	private Vector2 plateOffset;

	private Node2D anchor;
	private Vector2 source;
	private Vector2 textSize;
	private float age = -1f;
	private Rect2 box;

	/// <summary>True while a line is up (waiting to fade in, held or fading out).</summary>
	public bool Showing => anchor != null;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		TextureFilter = TextureFilterEnum.Nearest;
		Visible = false;

		label = new Label
		{
			Name = "HintLine",
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		ResponsiveLayout.SetFont(label, ResponsiveLayout.TextRole.Body);
		label.AddThemeColorOverride("font_outline_color", Occlusion);
		label.AddThemeConstantOverride("outline_size", 4);
		AddChild(label);
	}

	/// <summary>
	/// Puts a line up on the side of the screen toward <paramref name="worldSource"/>, fading in after
	/// <see cref="DelaySeconds"/>. The far-tell colour is the cue's to wear now, not the line's.
	/// </summary>
	public void ShowLine(string line, HintVoice voice, Node2D from, Vector2 worldSource)
	{
		if (label == null)
			return;
		anchor = from;
		source = worldSource;

		// Speech is set in quotation marks, as somebody's words; a sound plainly, as a caption of it.
		label.Text = voice == HintVoice.Speech ? $"“{line}”" : line;
		label.AddThemeColorOverride("font_color", voice == HintVoice.Speech ? SpeechInk : SoundInk);

		// Measured once per line, not per frame: as wide as the line wants up to the column, then
		// as tall as it wraps to at that width.
		Font font = label.GetThemeFont("font");
		int size = label.GetThemeFontSize("font_size");
		float maxWidth = ResponsiveLayout.IsNarrow(this) ? NarrowMaxTextWidth : WideMaxTextWidth;
		float natural = font.GetStringSize(label.Text, HorizontalAlignment.Left, -1, size).X;
		float width = Mathf.Min(natural + 2f, maxWidth);
		float height = font.GetMultilineStringSize(label.Text, HorizontalAlignment.Center, width, size).Y;
		textSize = new Vector2(Mathf.Ceil(width), Mathf.Ceil(height));
		(plate, plateOffset) = BuildPlate(textSize);

		age = -DelaySeconds;
		Modulate = new Color(1f, 1f, 1f, 0f);
		Visible = true;
		Layout();
		QueueRedraw();
	}

	/// <summary>Starts the fade-out now (the boss has arrived, or the place has been found).</summary>
	public void Dismiss()
	{
		if (anchor == null)
			return;
		if (age < 0f)
		{
			// Not faded in yet: it never appears.
			anchor = null;
			Visible = false;
			return;
		}
		if (age < FadeInSeconds + HoldSeconds)
			age = FadeInSeconds + HoldSeconds;
	}

	public override void _Process(double delta)
	{
		if (anchor == null)
			return;
		age += (float)delta;
		float total = FadeInSeconds + HoldSeconds + FadeOutSeconds;
		if (age >= total || !IsInstanceValid(anchor))
		{
			anchor = null;
			Visible = false;
			return;
		}
		float fadeIn = Mathf.Clamp(age / FadeInSeconds, 0f, 1f);
		float fadeOut = Mathf.Clamp((total - age) / FadeOutSeconds, 0f, 1f);
		// Eased, so the words come up out of nothing rather than switching on at a low alpha.
		float a = Mathf.Min(fadeIn * fadeIn * (3f - 2f * fadeIn), fadeOut);
		Modulate = new Color(1f, 1f, 1f, a);
		Layout();
		QueueRedraw();
	}

	/// <summary>
	/// The part of the screen a hint may use, in canvas units: under the HUD (the touch band on a
	/// phone), out of a phone's bottom third, inset by the safe area. The cue's marks keep to it too.
	/// </summary>
	public static Rect2 SafeRect(Viewport viewport, bool narrow)
	{
		Rect2 screen = viewport.GetVisibleRect();
		var (left, top, right, bottom) = SafeArea.Insets(viewport);
		float innerTop = narrow ? TouchControls.DefaultHudBandHeight + top + NarrowBandGap : WideTop + top;
		float innerBottom = narrow
			? screen.Size.Y * (1f - NarrowThumbShare) - bottom
			: screen.Size.Y - WideBottom - bottom;
		return new Rect2(screen.Position.X + Side + left, screen.Position.Y + innerTop,
			screen.Size.X - 2f * Side - left - right, innerBottom - innerTop);
	}

	private void Layout()
	{
		if (anchor == null || !IsInstanceValid(anchor))
			return;
		Viewport viewport = GetViewport();
		Transform2D canvas = viewport.GetCanvasTransform();
		Vector2 player = canvas * anchor.GlobalPosition;
		Vector2 toward = canvas * source - player;
		Vector2 d = toward.LengthSquared() > 1f ? toward.Normalized() : Vector2.Up;
		Rect2 inner = SafeRect(viewport, ResponsiveLayout.IsNarrow(this));

		Vector2 size = textSize + new Vector2(2f * PadX, 2f * PadY);
		Vector2 half = size * 0.5f;
		// Held inside by the plate's solid part, not the words: a round end cut off by the edge of
		// the screen read as a band running off it.
		Vector2 hold = half + PlateSpill(textSize);
		// Out along the line to the source until it meets the inner rectangle's edge, then held inside.
		Vector2 edge = ExitPoint(player, d, inner);
		// The band the cue comes in on stays clear: on whichever side the line to the source leaves by,
		// the line is held CueBand further in, even when it is clamped (a wide line toward a corner).
		Rect2 room = inner;
		if (edge.X <= inner.Position.X + 1f)
			room = room.GrowIndividual(-CueBand, 0f, 0f, 0f);
		if (edge.X >= inner.End.X - 1f)
			room = room.GrowIndividual(0f, 0f, -CueBand, 0f);
		if (edge.Y <= inner.Position.Y + 1f)
			room = room.GrowIndividual(0f, -CueBand, 0f, 0f);
		if (edge.Y >= inner.End.Y - 1f)
			room = room.GrowIndividual(0f, 0f, 0f, -CueBand);
		Vector2 centre = Clamp(edge - d * (CueBand + EdgeGap + Mathf.Min(hold.X, hold.Y)), room, hold);

		// Never over the wizard: on a phone a line to the side is wider than the half-screen beside
		// him. Step it up off him (or down, for a source below), and hold it inside again.
		var keepOut = new Rect2(player - PlayerKeepOut, PlayerKeepOut * 2f);
		if (new Rect2(centre - half, size).Intersects(keepOut))
		{
			centre.Y = d.Y > 0.35f
				? keepOut.End.Y + half.Y + EdgeGap
				: keepOut.Position.Y - half.Y - EdgeGap;
			centre = Clamp(centre, room, hold);
		}

		// Off the tracked quests in their corner and the chapter note while it is up (HudKeepOut): slid
		// along the edge it lies by, toward the middle, as the edge pointers slide, and held inside again.
		// If the edge has no room past them, it steps in across the other way.
		bool alongY = edge.X <= inner.Position.X + 1f || edge.X >= inner.End.X - 1f;
		var plate = new Rect2(centre - hold, hold * 2f);
		Rect2 slid = HudKeepOut.SlideOff(plate, alongY, inner.GetCenter(), KeepOutMargin);
		if (slid.Position != plate.Position)
		{
			centre = Clamp(slid.GetCenter(), room, hold);
			plate = new Rect2(centre - hold, hold * 2f);
			if (HudKeepOut.Touches(plate.Grow(KeepOutMargin)))
				centre = Clamp(HudKeepOut.SlideOff(plate, !alongY, inner.GetCenter(), KeepOutMargin).GetCenter(), room, hold);
		}

		// On the plate's grid, so the dither does not crawl as the player walks.
		Vector2 corner = (centre - half) / PlateTexel;
		box = new Rect2(new Vector2(Mathf.Round(corner.X), Mathf.Round(corner.Y)) * PlateTexel, size);
		label.Position = box.Position + new Vector2(PadX, PadY);
		label.Size = textSize;
	}

	// Where a ray from inside the rectangle leaves it.
	public static Vector2 ExitPoint(Vector2 from, Vector2 d, Rect2 rect)
	{
		Vector2 p = from;
		p.X = Mathf.Clamp(p.X, rect.Position.X, rect.End.X);
		p.Y = Mathf.Clamp(p.Y, rect.Position.Y, rect.End.Y);
		float tx = d.X > 0.0001f ? (rect.End.X - p.X) / d.X : d.X < -0.0001f ? (rect.Position.X - p.X) / d.X : float.MaxValue;
		float ty = d.Y > 0.0001f ? (rect.End.Y - p.Y) / d.Y : d.Y < -0.0001f ? (rect.Position.Y - p.Y) / d.Y : float.MaxValue;
		return p + d * Mathf.Max(0f, Mathf.Min(tx, ty));
	}

	private static Vector2 Clamp(Vector2 centre, Rect2 inner, Vector2 half)
	{
		float minX = inner.Position.X + half.X, maxX = inner.End.X - half.X;
		float minY = inner.Position.Y + half.Y, maxY = inner.End.Y - half.Y;
		return new Vector2(
			minX <= maxX ? Mathf.Clamp(centre.X, minX, maxX) : inner.GetCenter().X,
			minY <= maxY ? Mathf.Clamp(centre.Y, minY, maxY) : inner.GetCenter().Y);
	}

	// How far the plate's solid capsule, and the first part of its dissolve, reach past the padded box.
	private static Vector2 PlateSpill(Vector2 text) =>
		new(Mathf.Max(0f, text.Y * 0.5f + PlateCoreY + PlateCoreX + PlateFall * 0.4f - PadX),
			Mathf.Max(0f, PlateCoreY + PlateFall * 0.25f - PadY));

	private static readonly int[] Bayer8 =
	{
		0, 32, 8, 40, 2, 34, 10, 42,
		48, 16, 56, 24, 50, 18, 58, 26,
		12, 44, 4, 36, 14, 46, 6, 38,
		60, 28, 52, 20, 62, 30, 54, 22,
		3, 35, 11, 43, 1, 33, 9, 41,
		51, 19, 59, 27, 49, 17, 57, 25,
		15, 47, 7, 39, 13, 45, 5, 37,
		63, 31, 55, 23, 61, 29, 53, 21,
	};

	// The plate for a line of this size: a capsule of occlusion behind the words - solid over them,
	// its ends round - dissolving through the Bayer matrix over PlateFall beyond. A capsule, not a
	// rectangle: with square corners it read as the band again. Built once per line, one texel per
	// PlateTexel canvas units. Returns the texture and where its corner sits relative to the box.
	private static (ImageTexture Texture, Vector2 Offset) BuildPlate(Vector2 text)
	{
		// The solid part covers every pixel of the words: a segment as long as the text, swept by a
		// radius of half its height (and a little), so the straight run spans the text and the round
		// ends stand out beyond it.
		float radius = text.Y * 0.5f + PlateCoreY;
		float length = text.X * 0.5f + PlateCoreX;
		Vector2 half = new Vector2(length + radius + PlateFall, radius + PlateFall);
		int w = Mathf.CeilToInt(2f * half.X / PlateTexel), h = Mathf.CeilToInt(2f * half.Y / PlateTexel);
		Image image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		Color ink = new Color(Occlusion, PlateAlpha);
		Vector2 middle = new Vector2(w, h) * PlateTexel * 0.5f;
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				Vector2 p = new Vector2((x + 0.5f) * PlateTexel, (y + 0.5f) * PlateTexel) - middle;
				float along = Mathf.Max(0f, Mathf.Abs(p.X) - length);
				float outside = Mathf.Sqrt(along * along + p.Y * p.Y) - radius;
				float cover = outside <= 0f ? 1f : Mathf.Pow(Mathf.Max(0f, 1f - outside / PlateFall), 1.6f);
				float threshold = (Bayer8[(y % 8) * 8 + x % 8] + 0.5f) / 64f;
				image.SetPixel(x, y, cover > threshold ? ink : new Color(0f, 0f, 0f, 0f));
			}
		}
		// The box's middle is the plate's middle.
		Vector2 boxHalf = text * 0.5f + new Vector2(PadX, PadY);
		return (ImageTexture.CreateFromImage(image), boxHalf - middle);
	}

	public override void _Draw()
	{
		if (anchor == null || plate == null)
			return;
		DrawTextureRect(plate, new Rect2(box.Position + plateOffset, plate.GetSize() * PlateTexel), false);
	}
}
