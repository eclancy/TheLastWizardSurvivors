using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

/// <summary>
/// The run's opening (.ai/quests-and-notes.md): the tracked quests written in big over the upper middle of
/// the screen by motes of light gathering into the letters, held a moment, then shrunk and flown into the
/// tracker in the lower left (QuestTracker), which takes each one up as it lands. About two seconds.
/// </summary>
/// <remarks>
/// Eric, 2026-10-31: "When you start a level, the tracked quests should show up as big text on the screen,
/// with a spell like animation, and then shrink into the corner."
///
/// CODE-DRAWN, on layer 7 with the set and achievement moment (SetAssembledMoment): above the HUD and the
/// touch stick (5), below the level-up menu (10). Pausable, so a level-up or the pause screen freezes it
/// under the menu rather than letting it play out unseen. Its one Control ignores the mouse, so a tap goes
/// through to the stick and the run starts moving at once; nothing here reads input.
///
/// NOTHING ALLOCATED WHILE IT PLAYS. The motes are a fixed pool made on arrival, each letter's offset is
/// measured once, and the strings are the quests' own.
///
/// Where it stands: the band above the player's head. The camera keeps him at the centre, so the words sit
/// between the HUD and his hat - the achievement plate's band, which is quiet at a run's first second.
/// </remarks>
public partial class QuestIntro : CanvasLayer
{
	/// <summary>Seconds from the first mote to the last word landing in the corner.</summary>
	public const float Duration = 2.1f;

	private const float GatherEnd = 0.75f;
	private const float FlyStart = 1.3f;
	private const float FlyEnd = 1.95f;
	private const int MotesPerLine = 64;

	public QuestTracker? Tracker { get; set; }

	private static readonly Color Light = CodexRows.Stone.Heading;          // gold hi, the words
	private static readonly Color Ember = CodexRows.Stone.Reward;           // gold lit, the motes' tails
	private static readonly Color StepInk = CodexRows.Stone.InkSoft;
	private static readonly Color Outline = new(0.02f, 0.025f, 0.04f, 0.9f);

	private sealed class Line
	{
		public string Title = string.Empty;
		public string Step = string.Empty;
		public float[] CharX = Array.Empty<float>();
		public string[] Chars = Array.Empty<string>();
		public int StepSize;
		public float Width;
		public float Scale = 1f;
		public Vector2 Origin;          // the title's left baseline at full size, on screen
		public Vector2 StepOrigin;
		public float StepWidth;
		public bool Landed;
	}

	private struct Mote
	{
		public Vector2 From;
		public Vector2 To;
		public float Delay;
		public float Travel;
		public int Line;
	}

	private readonly List<Line> lines = new();
	private Mote[] motes = Array.Empty<Mote>();
	private Control painter = null!;
	private Font? heading;
	private Font? sans;
	private int titleSize;
	private int stepSize;
	private float clock;

	public override void _Ready()
	{
		Layer = 7;
		ProcessMode = ProcessModeEnum.Pausable;
		painter = new Control { Name = "Painter", MouseFilter = Control.MouseFilterEnum.Ignore };
		painter.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		painter.Draw += Paint;
		AddChild(painter);
		// Laid out on the next frame, when the tracker's labels have their sizes.
		CallDeferred(nameof(Begin));
	}

	private void Begin()
	{
		if (Tracker == null || !IsInstanceValid(Tracker) || Tracker.Count == 0)
		{
			Finish();
			return;
		}
		bool narrow = ResponsiveLayout.IsNarrow(painter);
		heading = ResourceLoader.Exists("res://scenes/resources/HeadingFont.tres")
			? ResourceLoader.Load<Font>("res://scenes/resources/HeadingFont.tres") : ThemeDB.FallbackFont;
		sans = ThemeDB.FallbackFont;
		titleSize = narrow ? 50 : 44;
		stepSize = narrow ? 26 : 22;

		Rect2 screen = painter.GetViewportRect();
		float maxWidth = screen.Size.X - (narrow ? 56f : 160f);
		int count = Tracker.Count;
		// Three quests at full size would reach the player; each is written a little smaller as there are more.
		float shrink = count == 1 ? 1f : count == 2 ? 0.86f : 0.74f;
		float block = 0f;
		for (int i = 0; i < count; i++)
		{
			Quest quest = Tracker.Quests[i];
			var line = new Line { Title = quest.Title, Step = QuestTracker.StepLine(quest) };
			float natural = heading.GetStringSize(line.Title, HorizontalAlignment.Left, -1, titleSize).X;
			line.Scale = Mathf.Min(shrink, maxWidth / Mathf.Max(1f, natural));
			line.Width = natural;
			line.CharX = new float[line.Title.Length + 1];
			line.Chars = new string[line.Title.Length];
			for (int c = 0; c < line.Title.Length; c++)
				line.Chars[c] = line.Title.Substring(c, 1);
			for (int c = 1; c <= line.Title.Length; c++)
				line.CharX[c] = heading.GetStringSize(line.Title.Substring(0, c), HorizontalAlignment.Left, -1, titleSize).X;
			// A long step is set smaller rather than run off the screen; never under the Label role's floor.
			float stepNatural = sans.GetStringSize(line.Step, HorizontalAlignment.Left, -1, stepSize).X;
			line.StepSize = stepNatural > maxWidth ? Mathf.Max(narrow ? 20 : 18, Mathf.FloorToInt(stepSize * maxWidth / stepNatural)) : stepSize;
			line.StepWidth = Mathf.Min(maxWidth, sans.GetStringSize(line.Step, HorizontalAlignment.Left, -1, line.StepSize).X);
			block += LineHeight(line);
			lines.Add(line);
		}

		// The block ends above the player's head: the centre of the screen, less his height and a gap.
		float bottom = screen.Size.Y * 0.5f - (narrow ? 120f : 84f);
		float top = Mathf.Max(screen.Size.Y * (narrow ? 0.2f : 0.17f), bottom - block);
		float y = top;
		foreach (Line line in lines)
		{
			float ascent = heading.GetAscent(titleSize) * line.Scale;
			line.Origin = new Vector2(screen.Size.X * 0.5f - line.Width * line.Scale * 0.5f, y + ascent);
			line.StepOrigin = new Vector2(screen.Size.X * 0.5f - line.StepWidth * 0.5f,
				y + (heading.GetHeight(titleSize) * line.Scale) + sans.GetAscent(line.StepSize) + 4f);
			y += LineHeight(line);
		}

		// The pool: each line's motes start scattered round it and end on its letters.
		var rng = new RandomNumberGenerator { Seed = 7919u };
		motes = new Mote[lines.Count * MotesPerLine];
		for (int i = 0; i < motes.Length; i++)
		{
			int index = i / MotesPerLine;
			Line line = lines[index];
			float w = line.Width * line.Scale;
			float capHeight = heading.GetAscent(titleSize) * line.Scale * 0.7f;
			Vector2 centre = line.Origin + new Vector2(w * 0.5f, -capHeight * 0.5f);
			float angle = rng.RandfRange(0f, Mathf.Tau);
			float reach = rng.RandfRange(narrow ? 140f : 160f, narrow ? 340f : 420f);
			motes[i] = new Mote
			{
				Line = index,
				From = centre + new Vector2(Mathf.Cos(angle) * reach, Mathf.Sin(angle) * reach * 0.6f),
				To = line.Origin + new Vector2(rng.RandfRange(0f, w), -rng.RandfRange(0f, capHeight)),
				Delay = rng.RandfRange(0f, 0.22f) + (float)index * 0.06f,
				Travel = rng.RandfRange(0.32f, 0.5f),
			};
		}
		Tracker.HoldForIntro();
	}

	private float LineHeight(Line line) =>
		heading!.GetHeight(titleSize) * line.Scale + sans!.GetHeight(stepSize) + 18f;

	public override void _Process(double delta)
	{
		if (lines.Count == 0)
			return;
		clock += (float)delta;
		for (int i = 0; i < lines.Count; i++)
		{
			// Each lands a beat after the one above it; the tracker takes it up as it arrives.
			if (!lines[i].Landed && clock >= LandTime(i))
			{
				lines[i].Landed = true;
				Tracker?.RevealEntry(i);
			}
		}
		if (clock >= Duration)
		{
			Finish();
			return;
		}
		painter.QueueRedraw();
	}

	private float LandTime(int index) => FlyEnd - 0.12f * (lines.Count - 1 - index);

	private void Finish()
	{
		if (Tracker != null && IsInstanceValid(Tracker))
			Tracker.ReleaseHold();
		QueueFree();
	}

	private void Paint()
	{
		if (heading == null || sans == null)
			return;
		bool narrow = ResponsiveLayout.IsNarrow(painter);
		float dot = narrow ? 5f : 4f;

		// The motes: light drawn in from all round, each a small square on the pixel grain, a fainter
		// one trailing behind it. They go out as they reach the letters - the letters are what they became.
		foreach (Mote mote in motes)
		{
			float t = (clock - mote.Delay) / mote.Travel;
			if (t <= 0f || t >= 1.15f)
				continue;
			float eased = 1f - Mathf.Pow(1f - Mathf.Clamp(t, 0f, 1f), 3f);
			Vector2 at = mote.From.Lerp(mote.To, eased);
			Vector2 behind = mote.From.Lerp(mote.To, Mathf.Max(0f, eased - 0.12f));
			float alpha = t < 1f ? Mathf.Clamp(t * 3f, 0f, 1f) : 1f - (t - 1f) / 0.15f;
			painter.DrawRect(new Rect2(behind.Round() - new Vector2(dot, dot) * 0.5f, new Vector2(dot, dot)), new Color(Ember, alpha * 0.45f));
			painter.DrawRect(new Rect2(at.Round() - new Vector2(dot, dot) * 0.5f, new Vector2(dot, dot)), new Color(Light, alpha));
		}

		for (int i = 0; i < lines.Count; i++)
			PaintLine(lines[i], i);
	}

	private void PaintLine(Line line, int index)
	{
		// Written in: a sweep left to right as the motes arrive, its leading edge brighter.
		float sweep = Mathf.Clamp((clock - 0.18f - index * 0.06f) / (GatherEnd - 0.18f), 0f, 1f);
		float landAt = LandTime(index);
		float flyFrom = FlyStart + 0.06f * index;
		float fly = Mathf.Clamp((clock - flyFrom) / (landAt - flyFrom), 0f, 1f);
		if (fly >= 1f)
			return;
		float flyEased = fly < 0.5f ? 4f * fly * fly * fly : 1f - Mathf.Pow(-2f * fly + 2f, 3f) * 0.5f;

		Vector2 origin = line.Origin;
		float scale = line.Scale;
		if (fly > 0f && Tracker != null && Tracker.TryGetTitleRect(index, out Rect2 target, out int targetSize))
		{
			float endScale = targetSize / (float)titleSize;
			Vector2 endOrigin = target.Position + new Vector2(0f, heading!.GetAscent(targetSize));
			// Out to the left first and then down the side - a curve, so the words never cross the player.
			var bend = new Vector2(endOrigin.X, origin.Y);
			float u = 1f - flyEased;
			origin = origin * (u * u) + bend * (2f * u * flyEased) + endOrigin * (flyEased * flyEased);
			scale = Mathf.Lerp(line.Scale, endScale, flyEased);
		}
		float lineAlpha = fly > 0.85f ? 1f - (fly - 0.85f) / 0.15f : 1f;

		painter.DrawSetTransform(origin, 0f, new Vector2(scale, scale));
		// Past the last letter by the glint's width, so the whole line has settled to ink by the hold.
		float revealX = sweep * (line.Width + 90f);
		for (int c = 0; c < line.Title.Length; c++)
		{
			float x = line.CharX[c];
			float a = Mathf.Clamp((revealX - x) / 40f, 0f, 1f) * lineAlpha;
			if (a <= 0f)
				break;
			string ch = line.Chars[c];
			var at = new Vector2(x, 0f);
			heading!.DrawStringOutline(painter.GetCanvasItem(), at, ch, HorizontalAlignment.Left, -1, titleSize, 8, new Color(Outline, Outline.A * a));
			// The edge still being written burns hotter than what is already ink.
			float edge = Mathf.Clamp(1f - (revealX - x) / 90f, 0f, 1f);
			Color ink = Light.Lerp(Colors.White, edge * 0.7f);
			heading.DrawString(painter.GetCanvasItem(), at, ch, HorizontalAlignment.Left, -1, titleSize, new Color(ink, a));
		}
		painter.DrawSetTransform(Vector2.Zero, 0f, Vector2.One);

		// A rule drawn out from the middle under the words while they hold, gone before they fly.
		if (fly <= 0f && sweep > 0.3f)
		{
			float half = line.Width * line.Scale * 0.5f * Mathf.Clamp((sweep - 0.3f) / 0.7f, 0f, 1f);
			float mid = line.Origin.X + line.Width * line.Scale * 0.5f;
			float y = line.Origin.Y + 6f;
			float ruleAlpha = Mathf.Clamp((FlyStart - clock) / 0.2f, 0f, 1f) * 0.8f;
			painter.DrawRect(new Rect2(new Vector2(mid - half, y).Round(), new Vector2(half * 2f, 2f)), new Color(Ember, ruleAlpha));
		}

		// The step comes up under the words once they are written, and goes as they fly.
		float stepAlpha = Mathf.Clamp((clock - 0.55f) / 0.25f, 0f, 1f) * (1f - Mathf.Clamp(fly * 3f, 0f, 1f));
		if (stepAlpha > 0f && !string.IsNullOrEmpty(line.Step))
		{
			sans!.DrawStringOutline(painter.GetCanvasItem(), line.StepOrigin, line.Step, HorizontalAlignment.Left, line.StepWidth + 2f, line.StepSize, 6, new Color(Outline, Outline.A * stepAlpha));
			sans.DrawString(painter.GetCanvasItem(), line.StepOrigin, line.Step, HorizontalAlignment.Left, line.StepWidth + 2f, line.StepSize, new Color(StepInk, stepAlpha));
		}
	}
}
