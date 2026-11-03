using System;
using Godot;
using WizardSurvivors.scripts;

// What a chapter hint shows (StageHintDirector; .ai/side-events.md, "Hints that something is out
// there"): a few marks in the site's own language coming in from the edge of the screen on the side
// the sound came from, on the sound's beat, drifting a little way in and fading.
//
// Eric, playing 0.10.0: "The clues for each wizard should not just be text and an arrow. For example,
// for the singing wizard, once in a while, the player should see musical notes on screen in the
// direction of the wizard, and should hear the singing as well." So:
//
//   the grove     leaves tumbling in, though there is no wind - on the whisper's three breaths
//   the cell      arcs of a voice coming down a corridor - on the cry's syllables
//   the lanterns  cold flames rising, flickering and guttering - on the flare
//   the thaw      the song's notes, one per sung note, each a step higher or lower as the tune is,
//                 with a frost glint left behind it
//   the caravan   the bell swinging as it walks, ringing at each strike
//   the seal      strips of written paper tumbling in on the wind, charring at the edges as they fall -
//                 on each gust of the paper wind
//   the mouths    written leaves blown in fast and flat on a cold draught, one on each voice of the call,
//                 the first the nearest the line to her
//
// IN THE WORLD'S LAYERS, HELD TO THE VIEW. The marks are drawn in the world, on the layer over the
// bodies and under the HUD, so they read as something out there rather than as an interface - but
// they are laid out and moved in screen space and follow the camera. Left on the ground, they slid
// under the line whenever the player walked toward the sound (the line is fixed to the screen, and the
// band they come in on is the edge beside it). Each wears its event's far-tell colour
// (tools/art/hint_cues.py draws them in the matching emissive ramp, with an occlusion outline that
// keeps them readable on the Fallow Fields and the Sketchbook's paper) over a soft glow of it.
//
// They come in along a band at the edge of the same safe rectangle the line keeps to
// (StageHintCaption.SafeRect: under the HUD, out of a phone's thumb zone), spread along it either side
// of the line to the source, and the line itself is set just inside that band (StageHintCaption.CueBand),
// so the marks arrive first, at the edge, and the words fade in beside them, never on top of them.
// Every mark is a slot in a fixed array, so playing one allocates nothing.
public partial class StageHintCue : Node2D
{
	public const string SheetPath = "res://assets/bonelight/effects/hint-cues.png";
	public const int Cell = 16;
	public const int Columns = 8;
	public const int Rows = 7;   // HintCue's members, in order

	// x3, the attack icons' size: at x2 a 16 px mark was a speck on the phone.
	private const float DrawScale = 3f;
	private const float Half = Cell * DrawScale * 0.5f;
	// A mark's half-size on the canvas, for keeping it off the HUD's corners.
	private static readonly Vector2 MarkHalf = new(Half, Half);
	private const int MaxMarks = 24;

	// The song's notes: onset (s), the note's glyph (sheet column), and its pitch in semitones from A4.
	// tools/audio/sfx.py _ICE_SONG is the same line - change one, change both.
	private static readonly (float At, int Glyph, int Pitch)[] IceSongNotes =
	{
		(0.20f, 0, -12), (0.50f, 2, -14), (0.78f, 1, -16), (1.06f, 0, -14), (1.34f, 2, -17), (1.66f, 3, -19),
	};

	// The cry's syllables (sfx.py hint_cell_cry: "Help!" then "is some-one there?").
	// Each syllable comes in as an arc and its echo a beat behind, so it reads as a voice off stone.
	private static readonly float[] CryBeats = { 0.0f, 0.13f, 0.50f, 0.62f, 0.78f, 0.90f };
	// The whisper's breaths and the leaves between them (hint_grove_whisper).
	private static readonly float[] WhisperBeats = { 0.12f, 0.34f, 0.58f, 0.80f, 1.0f, 1.26f };
	// The flame catching, then burning and guttering (hint_lantern_flare).
	private static readonly float[] FlareBeats = { 0.0f, 0.14f, 0.32f, 0.6f, 0.95f };
	// The bell's strikes (hint_caravan_bell), at a stride's uneven gaps.
	private static readonly float[] BellStrikes = { 0.0f, 0.36f, 0.80f, 1.13f, 1.58f };
	// The paper wind's gusts (hint_paper_wind): a strip comes in on each, the flutter between them.
	private static readonly float[] PaperGusts = { 0.10f, 0.42f, 0.70f, 1.12f, 1.40f };
	// The nine mouths' call heard from afar (hint_sibyl_voice): her voice, then the cave giving it back a beat
	// apart (sfx._SIBYL_VOICES, NineMouthsEvent.VoiceBeat) - a written leaf on each.
	private static readonly float[] SibylVoices = { 0.0f, 0.24f, 0.48f, 0.72f, 0.96f, 1.20f, 1.44f };

	/// <summary>The nine mouths' hint beats, for RegressionChecks.ValidateNineMouths (a copy: it cannot be changed through this).</summary>
	public static float[] SibylVoiceBeats => (float[])SibylVoices.Clone();

	private struct Mark
	{
		public bool Live;
		public int Row;
		public int Glyph;          // the sheet column, or the first frame of an animated mark
		public Vector2 Position;   // on the canvas (screen space), so it holds to the view
		public Vector2 Velocity;   // canvas units a second
		public float Age;
		public float Life;
		public float Phase;
		public int Follow;         // a ring follows its bell: the bell's slot, or -1
		public bool Mirror;
		public float FadeIn;
	}

	private readonly Mark[] marks = new Mark[MaxMarks];
	private Texture2D sheet;
	private Node2D glowLayer;

	private Node2D anchor;
	private Vector2 source;
	private HintCue cue;
	private Color tell = Colors.White;
	private float clock = -1f;
	private int nextBeat;
	private int bellSlot = -1;
	// Where the marks come in, moved along the edge off the tracked quests and the chapter note
	// (HudKeepOut), worked out once a play (LayLane), and which way that edge runs.
	private Vector2 laneShift;
	private bool alongY;
	private readonly Random random = new();

	/// <summary>True while a cue is coming in or any of its marks is still up.</summary>
	public bool Showing
	{
		get
		{
			if (clock >= 0f)
				return true;
			foreach (Mark m in marks)
				if (m.Live)
					return true;
			return false;
		}
	}

	public override void _Ready()
	{
		ZIndex = DrawLayers.ForemostAir;
		ZAsRelative = false;
		TextureFilter = TextureFilterEnum.Nearest;
		sheet = ResourceLoader.Exists(SheetPath) ? GD.Load<Texture2D>(SheetPath) : null;
		if (sheet == null)
			GD.PushError("StageHintCue: hint-cues.png is missing or not imported (python tools/art/hint_cues.py, then import); hints will be heard and read but not seen.");

		// The glow under each mark: additive, behind the marks, smooth (light is not a sprite).
		glowLayer = new Node2D
		{
			Name = "Glow",
			ShowBehindParent = true,
			Material = ExploreArt.Additive,
			TextureFilter = TextureFilterEnum.Linear,
		};
		glowLayer.Draw += OnGlowDraw;
		AddChild(glowLayer);
	}

	public override void _ExitTree()
	{
		if (glowLayer != null && IsInstanceValid(glowLayer))
			glowLayer.Draw -= OnGlowDraw;
	}

	/// <summary>
	/// Starts a cue coming in from the side of the screen toward <paramref name="worldSource"/>, as
	/// seen from <paramref name="from"/>.
	/// </summary>
	public void Play(HintCue kind, Node2D from, Vector2 worldSource, Color tellColor)
	{
		anchor = from;
		source = worldSource;
		cue = kind;
		tell = tellColor;
		clock = 0f;
		nextBeat = 0;
		bellSlot = -1;
		LayLane();
		Beat(0f);
	}

	// How far either way along the edge a cue's marks start, and how far in they drift.
	private static float SpreadOf(HintCue kind) => kind switch
	{
		HintCue.Song => 132f,
		HintCue.Leaves => 126f,
		HintCue.Wisp => 96f,
		HintCue.Bell => 72f,
		HintCue.Paper => 110f,
		HintCue.Writ => 120f,
		_ => 8f,
	};

	// The tracked quests sit in the lower left and the chapter note, while it is up, in the lower right
	// (the top on a phone): both on the edge strip the marks come in on. So the whole spread of a cue -
	// a row of six notes, a drift of leaves - is slid along the edge off them in one piece, the way the
	// edge pointers slide (HudKeepOut), rather than each mark piling up at the zone's end.
	private void LayLane()
	{
		laneShift = Vector2.Zero;
		var (edge, side, d, safe) = Band();
		alongY = edge.X <= safe.Position.X + 1f || edge.X >= safe.End.X - 1f;
		float spread = SpreadOf(cue);
		Rect2 reach = new Rect2(ClampTo(edge - side * spread, safe), Vector2.Zero)
			.Expand(ClampTo(edge + side * spread, safe))
			.Expand(ClampTo(edge - d * 60f, safe))
			.Grow(Half);
		laneShift = HudKeepOut.SlideOff(reach, alongY, safe.GetCenter(), 6f).Position - reach.Position;
	}

	// The band the marks come in on: where the line to the source meets the safe rectangle (less most of a
	// mark, so a whole one fits), the direction along it, the direction to the source, and the rectangle.
	private (Vector2 Edge, Vector2 Side, Vector2 Toward, Rect2 Safe) Band()
	{
		Viewport viewport = GetViewport();
		Transform2D canvas = viewport.GetCanvasTransform();
		Vector2 player = anchor != null && IsInstanceValid(anchor) ? canvas * anchor.GlobalPosition : viewport.GetVisibleRect().GetCenter();
		Vector2 toward = canvas * source - player;
		Vector2 d = toward.LengthSquared() > 1f ? toward.Normalized() : Vector2.Up;
		Rect2 safe = StageHintCaption.SafeRect(viewport, ResponsiveLayout.IsNarrow(this)).Grow(-Half * 0.85f);
		return (StageHintCaption.ExitPoint(player, d, safe), new Vector2(-d.Y, d.X), d, safe);
	}

	/// <summary>The boss has come, or the place is found: everything up fades out now, nothing new comes.</summary>
	public void Dismiss()
	{
		clock = -1f;
		for (int i = 0; i < marks.Length; i++)
		{
			if (marks[i].Live)
				marks[i].Life = Mathf.Min(marks[i].Life, marks[i].Age + 0.4f);
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		Vector2 middle = GetViewport().GetVisibleRect().GetCenter();
		if (clock >= 0f)
		{
			clock += dt;
			Beat(clock);
		}
		bool any = false;
		for (int i = 0; i < marks.Length; i++)
		{
			ref Mark m = ref marks[i];
			if (!m.Live)
				continue;
			m.Age += dt;
			if (m.Age >= m.Life)
			{
				m.Live = false;
				continue;
			}
			any = true;
			if (m.Follow >= 0)
			{
				m.Position = marks[m.Follow].Position;
			}
			else
			{
				m.Position += m.Velocity * dt;
				// A mark drifting along the edge stops at the tracked quests (or the note) rather than
				// crossing them.
				var at = new Rect2(m.Position - MarkHalf, MarkHalf * 2f);
				Rect2 off = HudKeepOut.SlideOff(at, alongY, middle, 2f);
				if (off.Position != at.Position)
					m.Position = off.GetCenter();
			}
		}
		// Redrawn while anything is up, and once more after the last mark has gone, to clear it.
		bool active = any || clock >= 0f;
		if (active || wasActive)
		{
			QueueRedraw();
			glowLayer?.QueueRedraw();
		}
		wasActive = active;
	}

	private bool wasActive;

	// Everything due by time t: each cue's own beats. "along" is where on the edge a mark comes in,
	// measured along it from the line to the source; "depth" how far in from it; "inward" how fast it
	// drifts in, "drift" how fast along. Slow on purpose: the line is set just inside the band the marks
	// arrive in (StageHintCaption.CueBand), and a mark has faded before it gets there.
	private void Beat(float t)
	{
		switch (cue)
		{
			case HintCue.Song:
				while (nextBeat < IceSongNotes.Length && IceSongNotes[nextBeat].At <= t)
				{
					var (_, glyph, pitch) = IceSongNotes[nextBeat];
					// The tune laid out along the edge in the order it is sung, each note a little further
					// in the higher it is.
					float along = (nextBeat - (IceSongNotes.Length - 1) * 0.5f) * 44f;
					int note = Spawn((int)HintCue.Song, glyph, 3.0f, along, (pitch + 19) * 3f,
						inward: 8f, drift: 0f, rise: 10f, fadeIn: 0.18f);
					// A glint left where the note came in, a step behind it.
					if (note >= 0)
						Spawn((int)HintCue.Song, 4, 0.7f, along - 16f, (pitch + 19) * 3f - 12f,
							inward: 0f, drift: 0f, rise: 0f, fadeIn: 0.05f);
					nextBeat++;
				}
				if (nextBeat >= IceSongNotes.Length)
					clock = -1f;
				break;
			case HintCue.Echo:
				while (nextBeat < CryBeats.Length && CryBeats[nextBeat] <= t)
				{
					// One voice down one corridor: every arc on the line to the source, coming in fast and
					// gone within the band.
					Spawn((int)HintCue.Echo, -1, 1.4f, 0f, -12f, inward: 40f, drift: 0f, rise: 0f, fadeIn: 0.06f);
					nextBeat++;
				}
				if (nextBeat >= CryBeats.Length)
					clock = -1f;
				break;
			case HintCue.Leaves:
				while (nextBeat < WhisperBeats.Length && WhisperBeats[nextBeat] <= t)
				{
					Spawn((int)HintCue.Leaves, random.Next(Columns), 3.2f, Jitter(120f), Jitter(10f),
						inward: 8f, drift: Jitter(18f), rise: 0f, fadeIn: 0.3f);
					nextBeat++;
				}
				if (nextBeat >= WhisperBeats.Length)
					clock = -1f;
				break;
			case HintCue.Wisp:
				while (nextBeat < FlareBeats.Length && FlareBeats[nextBeat] <= t)
				{
					Spawn((int)HintCue.Wisp, random.Next(Columns), 2.4f, Jitter(90f), Jitter(8f),
						inward: 5f, drift: 0f, rise: 22f, fadeIn: 0.12f);
					nextBeat++;
				}
				if (nextBeat >= FlareBeats.Length)
					clock = -1f;
				break;
			case HintCue.Paper:
				while (nextBeat < PaperGusts.Length && PaperGusts[nextBeat] <= t)
				{
					// Blown in, tumbling, and falling as it comes: on a side edge the strips drift down the
					// screen as well as in, and char from the edges in the second half of their life (Pose).
					Spawn((int)HintCue.Paper, random.Next(4), 3.0f, Jitter(110f), Jitter(10f),
						inward: 10f, drift: Jitter(16f), rise: -14f, fadeIn: 0.25f);
					nextBeat++;
				}
				if (nextBeat >= PaperGusts.Length)
					clock = -1f;
				break;
			case HintCue.Writ:
				while (nextBeat < SibylVoices.Length && SibylVoices[nextBeat] <= t)
				{
					// Blown in flat and fast on the draught, not tumbling down as the grove's leaves fall: the first
					// voice's leaf on the line to her, each echo's further along the edge either way, and fainter.
					float along = (nextBeat == 0 ? 0f : ((nextBeat % 2 == 0) ? 1f : -1f) * (24f + 16f * nextBeat)) + Jitter(8f);
					Spawn((int)HintCue.Writ, random.Next(Columns), 2.6f - 0.15f * nextBeat, along, Jitter(8f),
						inward: 26f, drift: Jitter(10f), rise: 0f, fadeIn: 0.12f);
					nextBeat++;
				}
				if (nextBeat >= SibylVoices.Length)
					clock = -1f;
				break;
			case HintCue.Bell:
				// The bell walks along the edge, as the caravan walks its road.
				if (bellSlot < 0)
					bellSlot = Spawn((int)HintCue.Bell, 2, 2.6f, -30f, 0f, inward: 4f, drift: 24f, rise: 0f, fadeIn: 0.2f);
				while (nextBeat < BellStrikes.Length && BellStrikes[nextBeat] <= t)
				{
					if (bellSlot >= 0 && marks[bellSlot].Live)
					{
						// The ring of the strike, either side of the bell.
						for (int side = 0; side < 2; side++)
						{
							int ring = Spawn((int)HintCue.Bell, 5, 0.36f, 0f, 0f, 0f, 0f, 0f, 0.02f);
							if (ring >= 0)
							{
								marks[ring].Follow = bellSlot;
								marks[ring].Position = marks[bellSlot].Position;
								marks[ring].Mirror = side == 1;
							}
						}
					}
					nextBeat++;
				}
				if (nextBeat >= BellStrikes.Length)
					clock = -1f;
				break;
		}
	}

	private float Jitter(float span) => (float)(random.NextDouble() * 2.0 - 1.0) * span;

	// One mark, coming in at the edge of the screen toward the source: "along" canvas units along the
	// edge from the line to the source, "depth" in from it. Returns its slot, or -1.
	private int Spawn(int row, int glyph, float life, float along, float depth, float inward, float drift, float rise, float fadeIn)
	{
		if (anchor == null || !IsInstanceValid(anchor))
			return -1;
		int slot = -1;
		for (int i = 0; i < marks.Length; i++)
		{
			if (!marks[i].Live)
			{
				slot = i;
				break;
			}
		}
		if (slot < 0)
			return -1;

		var (edge, side, d, safe) = Band();
		Vector2 start = ClampTo(edge + side * along - d * depth + laneShift, safe);
		// And this one mark off them too, in case the band has moved under the player since the lane was laid.
		start = HudKeepOut.SlideOff(new Rect2(start - MarkHalf, MarkHalf * 2f), alongY, safe.GetCenter(), 4f).GetCenter();

		// Off the edge, slowly; on a side edge flames and notes also rise (on the top edge up is back out
		// under the HUD, on the bottom one it is into the line).
		float up = Mathf.Abs(d.X) > 0.6f ? rise : 0f;
		Vector2 velocity = -d * inward + side * drift + Vector2.Up * up;
		int column = row == (int)HintCue.Echo ? DirectionColumn(-d) : glyph;
		marks[slot] = new Mark
		{
			Live = true,
			Row = row,
			Glyph = column,
			Position = start,
			Velocity = velocity,
			Age = 0f,
			Life = life,
			Phase = (float)random.NextDouble() * Mathf.Tau,
			Follow = -1,
			Mirror = false,
			FadeIn = fadeIn,
		};
		return slot;
	}

	private static Vector2 ClampTo(Vector2 p, Rect2 r) =>
		new(Mathf.Clamp(p.X, r.Position.X, Mathf.Max(r.Position.X, r.End.X)),
			Mathf.Clamp(p.Y, r.Position.Y, Mathf.Max(r.Position.Y, r.End.Y)));

	// The echo arcs are drawn for eight directions (0 = east, clockwise on the screen): the arc bulges
	// the way it travels.
	private static int DirectionColumn(Vector2 travel)
	{
		float angle = Mathf.Atan2(travel.Y, travel.X);
		int k = Mathf.RoundToInt(angle / (Mathf.Pi / 4f));
		return ((k % 8) + 8) % 8;
	}

	private float Alpha(in Mark m)
	{
		float a = Mathf.Min(Mathf.Clamp(m.Age / Mathf.Max(0.01f, m.FadeIn), 0f, 1f), Mathf.Clamp((m.Life - m.Age) / 0.7f, 0f, 1f));
		if (m.Row == (int)HintCue.Wisp)
		{
			// His light flickers, and gutters before it goes (SideEvent.Flicker's rhythm, shorter).
			a *= 0.78f + 0.22f * Mathf.Sin(m.Age * 23f + m.Phase) * Mathf.Sin(m.Age * 9.1f + m.Phase * 2f);
			if (m.Life - m.Age < 0.8f && Mathf.Sin(m.Age * 31f + m.Phase) > 0.4f)
				a *= 0.35f;
		}
		return Mathf.Clamp(a, 0f, 1f);
	}

	// Where a mark is drawn this frame (local), with its sway, and which sheet column.
	private (Vector2 At, int Column) Pose(in Mark m, Transform2D fromCanvas)
	{
		Vector2 at = m.Position;
		int column = m.Glyph;
		switch ((HintCue)m.Row)
		{
			case HintCue.Leaves:
				// Tumbling, and see-sawing across the way it drifts.
				column = (m.Glyph + (int)(m.Age * 7f)) % Columns;
				at += new Vector2(Mathf.Sin(m.Age * 2.6f + m.Phase) * 20f, Mathf.Cos(m.Age * 5.2f + m.Phase) * 5f);
				break;
			case HintCue.Wisp:
				column = (m.Glyph + (int)(m.Age * 11f)) % Columns;
				at.X += Mathf.Sin(m.Age * 3.1f + m.Phase) * 6f;
				break;
			case HintCue.Song:
				if (m.Glyph >= 4)
				{
					// A glint: grows, peaks, goes.
					float f = m.Age / m.Life;
					column = 4 + Mathf.Clamp(Mathf.RoundToInt(3f * Mathf.Sin(f * Mathf.Pi)), 0, 3);
				}
				else
				{
					at.Y += Mathf.Sin(m.Age * 3.4f + m.Phase) * 4f;
				}
				break;
			case HintCue.Paper:
				// Fluttering end over end (four turns, columns 0-3); past half its life it burns at the edges
				// (the same four turns charred, 4-7), and it see-saws as it falls.
				column = (m.Glyph + (int)(m.Age * 6f)) % 4 + (m.Age > m.Life * 0.5f ? 4 : 0);
				at.X += Mathf.Sin(m.Age * 2.2f + m.Phase) * 14f;
				break;
			case HintCue.Writ:
				// Turning over as it is blown, and lifted and dropped by the draught.
				column = (m.Glyph + (int)(m.Age * 9f)) % Columns;
				at.Y += Mathf.Sin(m.Age * 4.1f + m.Phase) * 6f;
				break;
			case HintCue.Bell:
				if (m.Glyph >= 5)
				{
					column = 5 + Mathf.Min(2, (int)(m.Age / 0.11f));
					at += new Vector2((m.Mirror ? -1f : 1f) * 6.5f * DrawScale, 0f);
				}
				else
				{
					// Swinging on a walking beast: at the end of its swing on each strike.
					float swing = Mathf.Sin(m.Age * Mathf.Pi / 0.4f);
					column = 2 + Mathf.Clamp(Mathf.RoundToInt(swing * 2f), -2, 2);
				}
				break;
		}
		return (ToLocal(fromCanvas * at).Round(), column);
	}

	public override void _Draw()
	{
		if (sheet == null)
			return;
		Transform2D fromCanvas = GetViewport().GetCanvasTransform().AffineInverse();
		for (int i = 0; i < marks.Length; i++)
		{
			Mark m = marks[i];
			if (!m.Live)
				continue;
			float a = Alpha(m);
			if (a <= 0.01f)
				continue;
			var (at, column) = Pose(m, fromCanvas);
			var src = new Rect2(column * Cell, m.Row * Cell, Cell, Cell);
			// The voice's arcs are thin strokes: at x3 they were lost on the Dungeon's brick, so they are
			// drawn at x4, the size an attack icon has over a boss.
			float h = m.Row == (int)HintCue.Echo ? Cell * 2f : Half;
			if (m.Mirror)
			{
				DrawSetTransform(at, 0f, new Vector2(-1f, 1f));
				DrawTextureRectRegion(sheet, new Rect2(-h, -h, h * 2f, h * 2f), src, new Color(1f, 1f, 1f, a));
				DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
			}
			else
			{
				DrawTextureRectRegion(sheet, new Rect2(at.X - h, at.Y - h, h * 2f, h * 2f), src, new Color(1f, 1f, 1f, a));
			}
		}
	}

	private void OnGlowDraw()
	{
		Transform2D fromCanvas = GetViewport().GetCanvasTransform().AffineInverse();
		for (int i = 0; i < marks.Length; i++)
		{
			Mark m = marks[i];
			if (!m.Live || m.Follow >= 0)
				continue;
			float a = Alpha(m);
			if (a <= 0.01f)
				continue;
			var (at, _) = Pose(m, fromCanvas);
			ExploreArt.DrawGlow(glowLayer, at, 34f, new Color(tell, 0.32f * a));
		}
	}
}
