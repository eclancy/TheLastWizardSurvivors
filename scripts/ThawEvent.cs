using Godot;
using System;
using TheOneHeMissed.scripts;

// The Frozen Waste's side event: the thaw (.ai/side-events.md).
//
// "A wizard lies under the ice." A figure is sealed in a block of ice at the centre of a frozen lake. The block is
// NOT a target and never joins "enemies" - the standing rule for anything breakable - so no spell is ever aimed at it.
//
// What it asks: SING IT BACK (.ai/unlock-puzzles.md 3.6, Eric 2026-11-03; built 2026-11-07). Over the lake, fanned
// out from the block to the shore, are stretched five great strings of ice - a kantele, the instrument he made in the
// legend - each with its own note and its own colour (Kantele). The ice sings a phrase: its strings light and sound in
// turn. He steps on them in the same order - a string counts where his feet touch it (StringFeet), never where an
// enemy stands. Three phrases, of three, four and five notes, each the last with a note added. Each one sung back
// takes the block a thaw stage further; the last frees him. A wrong string replays the phrase, and nothing is lost but
// the time. The seed is the phrases (PlaceSites); the strings lie where they always lie.
//
// HOW IT READS WITH THE SOUND OFF. Each string's pin and stake are in its colour at rest; it lights in that colour and
// shakes while it sings, and again under his feet when he gets it right, the light running out along it from where
// he stood; a wrong one goes dull and rattles. A row of pips under the block counts the notes of the phrase, filling
// in the colours he has sung back. The order is never written anywhere: it is heard and seen once per singing.
//
// THE SWARM. The strings are 145-195 px long and the way between two that are not side by side is in through the open
// ice round the block, so a phrase is sung on the run through the crowd; the cold still sends its guard while he is
// at the lake. Nothing is timed by the pace clock: the song's beat and every wait are real seconds.
//
// What went (3.6): the heat, fire counting triple, and the window of clear ice as the place where fighting counts.
// The window is still drawn; the light under the ice now grows with the phrases sung back.
//
// WHERE HE IS (Eric, playing 0.10.0: "Wizard trapped in ice should be in the center of a large, magically animated
// lake. Current art is corny looking"). He stands frozen in a block of ice - his own sprite seen through it - at the
// centre of a frozen lake 640 px across (ThawLake), with cold light running out under the ice from him, frost creeping
// in from the shore and a glow round him. The block cracks, chips and pools meltwater as each phrase is sung back (four
// stages in its sheet). Art: tools/art/thaw_lake.py (the lake, the block) and tools/art/kantele.py (the strings); the
// notes are tools/audio/sfx.py's ice_string_1-5 and ice_string_wrong. The lake is walkable ground, not terrain.
public partial class ThawEvent : SideEvent
{
	[Export] public float GuardIntervalSeconds { get; set; } = 5f;

	// --- the song's clock, real seconds ------------------------------------------------------------------

	/// <summary>Within this of the block the ice sings to him; past LeaveRadius it falls silent and forgets the answer.</summary>
	public const float ListenRadius = 380f;
	public const float LeaveRadius = 640f;

	/// <summary>The breath before a singing: every string shimmers once, so the first note is not missed.</summary>
	public const float BreathSeconds = 0.9f;

	/// <summary>From one sung note to the next.</summary>
	public const float NoteSeconds = 0.6f;

	/// <summary>The last note rings this long before the ice listens.</summary>
	public const float TailSeconds = 0.55f;

	/// <summary>Listening with no note sung back for this long: the ice sings it again.</summary>
	public const float IdleSeconds = 8f;

	/// <summary>Listening with part of it sung back and no right note for this long: the ice sings it again.</summary>
	public const float StallSeconds = 14f;

	/// <summary>After a wrong string, before the phrase is sung again.</summary>
	public const float WrongSeconds = 1.3f;

	/// <summary>After a phrase sung back - the strings ringing together, the block giving - before the next is sung.</summary>
	public const float GivesSeconds = 2.0f;

	/// <summary>A sung or answered string stays lit this long, fading.</summary>
	private const float LitSeconds = 0.75f;
	private const float WrongLitSeconds = 0.6f;

	/// <summary>How long a whole singing of <paramref name="length"/> notes takes, breath excluded.</summary>
	public static float SongSeconds(int length) => (length - 1) * NoteSeconds + TailSeconds;

	private enum Voice
	{
		/// <summary>He is not at the lake: the ice is quiet.</summary>
		Waiting,
		Breath,
		Singing,
		/// <summary>The phrase is sung: the ice listens for it back.</summary>
		Listening,
		Wrong,
		/// <summary>A phrase sung back: the strings ring together and the block gives.</summary>
		Gives,
	}

	private enum LitBy : byte { None, Song, Right, Wrong }

	private Vector2 block;
	private readonly int[] song = new int[Kantele.SongLength];
	private SingBack answer;
	private readonly StringFeet feet = new();
	private PlacedSoundSchedule singer;
	private Voice voice = Voice.Waiting;
	private float voiceClock;
	private float lastRightAt;
	private float guardTimer;
	// What is lit: per string, the Clock it was lit at, by what, and where along it (0 the pin) the light starts.
	private readonly float[] litAt = { -100f, -100f, -100f, -100f, -100f };
	private readonly LitBy[] litBy = new LitBy[Kantele.StringCount];
	private readonly float[] litFrom = new float[Kantele.StringCount];
	// The strings he sang back of the phrase so far, for the pips' colours.
	private readonly int[] sungBack = new int[Kantele.SongLength];
	// How far the light under the ice has got, eased toward the phrases sung back.
	private float thawShown;
	private int phrasesShown;

	// THE WIN BEAT (in-flight 58): the ice visibly falls away at the block. A deep crack and a held breath with nothing
	// moving, then the block breaks - chips of ice fall off its face and melt where they land, the block itself fades
	// as they go, meltwater runs out round its foot - and only then does the reward column rise, at the exact spot the
	// beat chose.
	private float winBeat = -1f;
	private Vector2 winSpot;
	private const float WinCrackSeconds = 0.4f;
	private const float WinFallSeconds = 1.1f;
	private const int ChipCount = 10;

	// --- what the probe and _UiShot read: only what a player is shown -----------------------------------------

	/// <summary>Phrases sung back so far (0-3).</summary>
	public int PhrasesSung => answer?.Phrase ?? 0;

	/// <summary>Notes of the phrase being sung back so far.</summary>
	public int Answered => answer?.Answered ?? 0;

	/// <summary>How many notes the phrase being sung back has.</summary>
	public int PhraseLength => answer?.Length ?? Kantele.PhraseLengths[0];

	/// <summary>True while the ice is singing (breath included): what a player waits through and watches.</summary>
	public bool IsSinging => voice == Voice.Breath || voice == Voice.Singing;

	/// <summary>True while the ice listens for the phrase back.</summary>
	public bool IsListening => voice == Voice.Listening && winBeat < 0f && answer != null && !answer.Done;

	/// <summary>Every sung note so far, counted: the probe sees a new one each time this moves.</summary>
	public int SungNotes { get; private set; }

	/// <summary>The string the ice last sang, lit as it sounds; -1 before any.</summary>
	public int LastSungString { get; private set; } = -1;

	/// <summary>
	/// The strings this singing has lit so far, in the order they lit - what a player watching saw. Kept, not just the
	/// last, because on a slow frame two notes can sound in one (the probe at 2 fps would otherwise miss one).
	/// </summary>
	public int SungThisSinging { get; private set; }
	public int SungStringAt(int k) => sungThisSinging[Math.Clamp(k, 0, Kantele.SongLength - 1)];
	private readonly int[] sungThisSinging = new int[Kantele.SongLength];

	/// <summary>Singings begun (the first included), wrong strings, and phrases sung back, for the probe's story.</summary>
	public int Singings { get; private set; }
	public int Wrongs { get; private set; }

	/// <summary>Where the block stands: the lake's middle, from which the strings are laid (Kantele).</summary>
	public Vector2 Block => block;

	// -------------------------------------------------------------------------------------------------------

	protected override string Announcement => "Someone is frozen in the ice. Over it, five strings of ice sing a tune.";
	protected override string ChestLine => "The ice gives up a chest";
	protected override string Objective
	{
		get
		{
			if (winBeat >= 0f)
				return "The ice gives";
			string of = $"({Math.Min(PhrasesSung + 1, Kantele.PhraseCount)}/{Kantele.PhraseCount})";
			return voice switch
			{
				Voice.Waiting => "The ice is singing: go out onto the lake",
				Voice.Breath or Voice.Singing => $"Listen: the ice is singing  {of}",
				Voice.Listening => $"Sing it back: step on the strings in that order  {of}",
				Voice.Wrong => "Not that string - listen again",
				_ => $"The ice gives  ({PhrasesSung}/{Kantele.PhraseCount} sung)",
			};
		}
	}
	// The top of the block, not its foot: the on-screen chevron hangs above its target, and on the foot it sat on his face.
	// Gone once the song is sung back: the ice is giving, and there is nothing left to go to.
	protected override Vector2? PointerTarget => winBeat >= 0f ? null : block + new Vector2(0f, -BlockStandsWorld);

	protected override string SiteGlyph => "ice";
	// The whole lake and its bank, and a little more: placement wants all of it on open ground (no pond under the ice),
	// and decor stays off it.
	protected override float SiteFootprint => ThawLake.WorldAxes.X + ThawLake.WorldBank + 20f;

	// Tested over the whole disc, not on two rings (Node2DGame.IsExploreGroundClearDense): at 360 px eight points a ring
	// are 280 px apart, and a pond under walkable ice would be an invisible wall. Then the seed's song: one roll, drawn
	// after the site so where the lake lies never moves for it (the seal's stones are rolled the same way).
	public override void PlaceSites(ExplorePlacer placer)
	{
		AddSite(placer.EventSite(SiteFootprint, dense: true));
		SetSong(Kantele.SongFromRoll(placer.NextFloat()));
	}

	/// <summary>The song the ice sings, three, four and five notes of it (the seed's; RegressionChecks lays its own).</summary>
	public void SetSong(int[] notes)
	{
		Array.Copy(notes, song, Kantele.SongLength);
		answer = new SingBack(song);
	}

	// Inside the reach where the cold starts sending guards (360), so arriving and the fight begin together.
	protected override float ArrivalRadius => 420f;
	protected override Color TellColor => new Color(0.50f, 0.86f, 0.97f);   // the ice ramp's mid

	private ThawLake lake;
	private Sprite2D iceBlock;
	private Node2D strings;
	private float glintClock;

	// The block's sheet: four stages of thawing, each four frames of a glint crossing its face.
	private const string BlockPath = ThawLake.Directory + "thaw-block.png";
	private const int BlockCell = 64;
	private const int BlockStages = 4;
	private const int GlintFrames = 4;
	// The row of a cell the block stands on (its contact line), and its middle column.
	private const int BlockFootRow = 58;
	private const int BlockAnchorX = 32;
	// How tall the block stands in the world, from its foot to its top (rows 4..58 of the cell, at x2).
	private const float BlockStandsWorld = 108f;
	// A glint crosses in three frames, then the face rests.
	private const float GlintFrameSeconds = 0.11f;
	private const float GlintRestSeconds = 2.6f;

	protected override void OnPlaced()
	{
		block = MainSite;
		answer ??= new SingBack(Kantele.SongFromRoll(0.5f));

		// The lake is the event's sibling, so it stays when the event frees itself (ThawLake); its strings lie on it.
		lake = ThawLake.Create(block);
		if (lake != null)
			Game?.CallDeferred(Node.MethodName.AddChild, lake);

		// The strings as they sound: on the floor, over the craters and pools a spell leaves and over the strings at
		// rest (ThawLake's, one below), under every body and every slam's tell (DrawLayers.GroundTell).
		strings = new Node2D
		{
			Name = "SingingStrings",
			ZIndex = DrawLayers.GroundTell - 1,
			ZAsRelative = false,
			TextureFilter = TextureFilterEnum.Nearest,
		};
		strings.Draw += DrawStrings;
		AddChild(strings);

		Texture2D sheet = ResourceLoader.Exists(BlockPath) ? GD.Load<Texture2D>(BlockPath) : null;
		if (sheet == null)
		{
			GD.PushError("ThawEvent: thaw-block.png is missing or not imported (python tools/art/thaw_lake.py, then import).");
			return;
		}
		// Standing in the world and sorted with the actors at its foot, as the bog lantern is (ZIndex 5 under this
		// node's -5 is the actors' 0). Drawn at x2, so the offset is in sheet pixels.
		Vector2 ground = ToLocal(block);
		iceBlock = new Sprite2D
		{
			Name = "IceBlock",
			Texture = sheet,
			Hframes = BlockStages * GlintFrames,
			Centered = false,
			Scale = new Vector2(2f, 2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Position = new Vector2(Mathf.Round(ground.X), Mathf.Round(ground.Y - WorldSprites.SortLift)),
			Offset = new Vector2(-BlockAnchorX, WorldSprites.SortLift / 2f - BlockFootRow),
			ZIndex = 5,
		};
		AddChild(iceBlock);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		float target = Mathf.Clamp(PhrasesSung / (float)Kantele.PhraseCount, 0f, 1f);
		thawShown = Mathf.MoveToward(thawShown, target, (float)delta / 0.8f);
		if (lake != null && IsInstanceValid(lake))
		{
			lake.Heat = thawShown;
			// He is out, or the event stood down: the light under the ice dies away.
			if (IsResolved)
				lake.Silent = true;
		}
		// Redrawn after the win too, so the last lit frame is cleared rather than left standing until the event frees itself.
		if (strings != null && !IsDormant && NearPlayer(block))
			strings.QueueRedraw();
		if (iceBlock == null)
			return;
		if (IsResolved)
		{
			iceBlock.Visible = false;
			return;
		}
		// The beat: the block goes with its chips - fading as they fall, gone when they have landed.
		if (winBeat >= 0f)
		{
			float fall = Mathf.Clamp((winBeat - WinCrackSeconds) / WinFallSeconds, 0f, 1f);
			iceBlock.Modulate = new Color(1f, 1f, 1f, 1f - fall);
			iceBlock.Visible = fall < 1f;
			return;
		}
		// Each phrase sung back sends a glint across the block's face at once, as the stage changes.
		if (PhrasesSung != phrasesShown)
		{
			phrasesShown = PhrasesSung;
			glintClock = 0f;
		}
		glintClock += (float)delta;
		float cycle = GlintFrameSeconds * (GlintFrames - 1) + GlintRestSeconds;
		int glint = Mathf.Min(GlintFrames - 1, Mathf.FloorToInt((glintClock % cycle) / GlintFrameSeconds));
		int stage = Mathf.Clamp(PhrasesSung, 0, BlockStages - 1);
		iceBlock.Frame = stage * GlintFrames + glint;
	}

	// Placed at stage start and waiting (SideEvent); the song begins when he arrives.
	protected override void Begin()
	{
		block = MainSite;
		singer = new PlacedSoundSchedule(Kantele.SongLength * 2, OnSung);
		if (Player != null)
			feet.Place(Player.FootPosition - block);
		voice = Voice.Waiting;
		voiceClock = 0f;
		GD.Print($"ThawEvent: the ice sings {Kantele.PhraseCount} phrases on its strings ({string.Join(" ", song)}; "
			+ $"{Kantele.PhraseLengths[0]}, {Kantele.PhraseLengths[1]} and {Kantele.PhraseLengths[2]} notes)");
	}

	protected override void Tick(float delta)
	{
		// The beat: the ice has given - no more guards, no more song - and it falls away before the reward comes. The
		// strum that opens it is still on the schedule, so the schedule is still ticked.
		if (winBeat >= 0f)
		{
			singer?.Tick(delta);
			winBeat += delta;
			if (winBeat >= WinCrackSeconds + WinFallSeconds)
				Succeed(winSpot, exact: true);
			return;
		}
		// _UiShot --win-event: every phrase sung back, so the real beat runs.
		if (answer.Done)
		{
			Win();
			return;
		}

		singer?.Tick(delta);
		voiceClock += delta;

		// His feet on the strings since the last frame, in the order he reached them.
		if (Player != null && !Player.IsDead)
		{
			int touched = feet.Step(Player.FootPosition - block);
			for (int k = 0; k < touched && winBeat < 0f; k++)
				OnTouched(feet.Hit(k), feet.Along(k));
			if (winBeat >= 0f)
				return;
		}

		bool near = PlayerWithin(block, ListenRadius);
		bool gone = !PlayerWithin(block, LeaveRadius);
		switch (voice)
		{
			case Voice.Waiting:
				if (near)
					Breathe();
				break;
			case Voice.Breath:
				if (gone)
					Hush();
				else if (voiceClock >= BreathSeconds)
					Sing();
				break;
			case Voice.Singing:
				if (gone)
					Hush();
				else if (voiceClock >= SongSeconds(answer.Length))
				{
					voice = Voice.Listening;
					voiceClock = 0f;
					lastRightAt = Clock;
				}
				break;
			case Voice.Listening:
				if (gone)
					Hush();
				else if (answer.Answered == 0 ? voiceClock >= IdleSeconds : Clock - lastRightAt >= StallSeconds)
					Breathe();
				break;
			case Voice.Wrong:
				if (voiceClock >= WrongSeconds)
				{
					if (near)
						Breathe();
					else
						Hush();
				}
				break;
			case Voice.Gives:
				if (voiceClock >= GivesSeconds)
				{
					if (near)
						Breathe();
					else
						Hush();
				}
				break;
		}

		// The guard only comes while the player is at the lake - an event left alone should not quietly fill the map.
		if (PlayerWithin(block, 360f))
		{
			guardTimer -= delta;
			if (guardTimer <= 0f)
			{
				guardTimer = GuardIntervalSeconds;
				SpawnRing(block, 4, 280f);
			}
		}
	}

	// A string under his feet. Only while the ice listens does it count; otherwise it is ice underfoot.
	private void OnTouched(int s, float along)
	{
		if (voice != Voice.Listening)
			return;
		Vector2 at = block + Kantele.Along(s, along);
		int index = answer.Answered;
		SingOutcome outcome = answer.Pluck(s);
		if (outcome == SingOutcome.Wrong)
		{
			Wrongs++;
			Light(s, LitBy.Wrong, along);
			SfxPlayer.AtPosition(SfxCatalog.IceStringWrong, at, 0.03f);
			voice = Voice.Wrong;
			voiceClock = 0f;
			GD.Print($"ThawEvent: string {s} out of turn at note {index + 1} of phrase {answer.Phrase + 1}; the ice sings it again");
			return;
		}
		Light(s, LitBy.Right, along);
		SfxPlayer.AtPosition(Kantele.Notes[s], at, 0f);
		sungBack[Math.Clamp(index, 0, Kantele.SongLength - 1)] = s;
		lastRightAt = Clock;
		if (outcome == SingOutcome.Right)
			return;
		// A phrase sung back: the strings it used ring together, low to high, and the block gives a stage.
		GD.Print($"ThawEvent: phrase {answer.Phrase} of {Kantele.PhraseCount} sung back");
		Strum(Kantele.PhraseLengths[answer.Phrase - 1]);
		if (outcome == SingOutcome.SongSung)
		{
			Win();
			return;
		}
		voice = Voice.Gives;
		voiceClock = 0f;
	}

	// Every string the last phrase used (every string, as the ice gives), a beat after his last note, rising, a stroke
	// across them.
	private void Strum(int length, bool all = false)
	{
		singer?.Clear();
		int order = 0;
		for (int s = 0; s < Kantele.StringCount; s++)
		{
			bool used = all;
			for (int k = 0; k < length && !used; k++)
				used = song[k] == s;
			if (used)
				singer?.Schedule(Kantele.Notes[s], block + Kantele.Middle(s), 0.35f + 0.07f * order++, StrumTag + s, 0f);
		}
	}

	// A tag on a scheduled note that is a strum, not a sung note: lit as answered, not counted as sung.
	private const int StrumTag = 100;

	private void Breathe()
	{
		answer.Restart();
		singer?.Clear();
		voice = Voice.Breath;
		voiceClock = 0f;
	}

	private void Sing()
	{
		voice = Voice.Singing;
		voiceClock = 0f;
		Singings++;
		SungThisSinging = 0;
		singer?.Clear();
		for (int k = 0; k < answer.Length; k++)
			singer?.Schedule(Kantele.Notes[song[k]], block + Kantele.Middle(song[k]), k * NoteSeconds, song[k], 0f);
	}

	// He has gone from the lake: the ice falls quiet, and the phrase he was singing back is forgotten - the phrases he
	// sang back are not.
	private void Hush()
	{
		answer.Restart();
		singer?.Clear();
		voice = Voice.Waiting;
		voiceClock = 0f;
	}

	// A scheduled note has sounded: a sung one lights its string from its pin, a strum's lights it whole.
	private void OnSung(int tag)
	{
		if (tag >= StrumTag)
		{
			Light(tag - StrumTag, LitBy.Right, 0.5f);
			return;
		}
		Light(tag, LitBy.Song, 0f);
		LastSungString = tag;
		SungNotes++;
		if (SungThisSinging < Kantele.SongLength)
			sungThisSinging[SungThisSinging++] = tag;
	}

	private void Light(int s, LitBy by, float from)
	{
		litAt[s] = Clock;
		litBy[s] = by;
		litFrom[s] = from;
	}

	private void Win()
	{
		if (winBeat >= 0f)
			return;
		// All five strings, rising, as the ice gives (the beat above keeps the schedule ticking).
		Strum(Kantele.SongLength, all: true);
		winSpot = ChooseRewardSpot(block);
		winBeat = 0f;
		// The ice's own cue as it gives; the reward's sound still comes with the column.
		SfxPlayer.AtPosition(SfxCatalog.SecretSeen, block, 0f);
		GD.Print($"ThawEvent: the song is sung back ({Singings} singings, {Wrongs} wrong strings); the ice gives");
	}

	/// <summary>
	/// _UiShot --win-event: every phrase sung back, so the real path runs - the crack, the chips, the ice falling away -
	/// instead of Succeed called straight past the beat.
	/// </summary>
	public void SingAllForPhoto() => answer?.Pose(Kantele.PhraseCount, 0);

	/// <summary>
	/// _UiShot --sing-pose: the ice listening for phrase <paramref name="phrase"/> (0-2) with <paramref name="answered"/>
	/// notes of it sung back, the last of them lit under his feet now. Returns where his feet are: on that string.
	/// </summary>
	public Vector2 PoseForPhoto(int phrase, int answered)
	{
		answer.Pose(phrase, answered);
		voice = Voice.Listening;
		voiceClock = 0f;
		lastRightAt = Clock;
		for (int k = 0; k < answer.Answered; k++)
			sungBack[k] = song[k];
		if (answer.Answered == 0)
			return block + new Vector2(0f, 40f);
		int last = song[answer.Answered - 1];
		Light(last, LitBy.Right, 0.55f);
		Vector2 foot = block + Kantele.Along(last, 0.55f);
		feet.Place(foot - block);
		return foot;
	}

	public override void _ExitTree()
	{
		if (strings != null && IsInstanceValid(strings))
			strings.Draw -= DrawStrings;
		base._ExitTree();
	}

	protected override void OnStoodDown()
	{
		singer?.Clear();
		voice = Voice.Waiting;
	}

	// --- drawing ---------------------------------------------------------------------------------------------

	private static readonly Color Warm = new Color(1.0f, 0.62f, 0.30f, 0.95f);
	private static readonly Color IceMid = new Color(0.498f, 0.863f, 0.973f);   // ice mid #7FDCF8
	private static readonly Color FireEdge = new Color(0.910f, 0.478f, 0.102f); // fire mid #E87A1A
	private static readonly Color Dull = new Color("6A7E98");                 // steel base: a string gone dead
	private static readonly Color Frost = new Color("DCE8F4");                // steel hi: a string at rest, or a pip to fill

	// A string drawn as it shakes: this many points from pin to stake, written into one buffer and handed to the canvas
	// (which copies it), so nothing is allocated a frame.
	private const int StringPoints = 17;
	private readonly Vector2[] line = new Vector2[StringPoints];

	/// <summary>How lit string <paramref name="s"/> is now, 1 the moment it sounded to 0, and by what.</summary>
	private float LitStrength(int s, out LitBy by)
	{
		by = litBy[s];
		if (by == LitBy.None)
			return 0f;
		float age = Clock - litAt[s];
		float span = by == LitBy.Wrong ? WrongLitSeconds : LitSeconds;
		if (age < 0f || age >= span)
			return 0f;
		float u = 1f - age / span;
		return u * u * (3f - 2f * u);
	}

	// The strings as they sound, on the floor over the strings at rest (SingingStrings, GroundTell - 1).
	private void DrawStrings()
	{
		if (IsDormant || IsResolved || strings == null)
			return;
		Vector2 c = ToLocal(block);
		float breath = voice == Voice.Breath ? Mathf.Clamp(voiceClock / BreathSeconds, 0f, 1f) : -1f;
		for (int s = 0; s < Kantele.StringCount; s++)
		{
			float lit = LitStrength(s, out LitBy by);
			// The breath before a singing: a pale shimmer running out along every string at once, pin to stake.
			if (breath >= 0f && lit <= 0f)
			{
				float w = Mathf.Sin(breath * Mathf.Pi);
				Vector2 a = c + Kantele.Along(s, Mathf.Max(0f, breath - 0.25f));
				Vector2 b = c + Kantele.Along(s, Mathf.Min(1f, breath + 0.05f));
				strings.DrawLine(a, b, new Color(Frost, 0.55f * w), Kantele.Width(s));
			}
			if (lit <= 0f)
				continue;
			float age = Clock - litAt[s];
			float width = Kantele.Width(s);
			if (by == LitBy.Wrong)
			{
				// Gone dull, and rattling: a short jitter across it at 30 Hz, its colour drained to cold steel.
				float jitter = 3f * lit * Mathf.Sin(age * 190f);
				Shake(c, s, jitter, 0f, 1f);
				strings.DrawPolyline(line, new Color(Dull, 0.9f * lit), width + 2f);
				continue;
			}
			// Shaking as it sounds: a standing wave, fullest in the middle, dying with the light.
			float amplitude = 3.5f * lit * Mathf.Sin(age * (34f + 5f * s));
			Shake(c, s, amplitude, 0f, 1f);
			strings.DrawPolyline(line, new Color(Kantele.Edge(s), 0.95f * lit), width + 4f);
			strings.DrawPolyline(line, new Color(Kantele.Mid(s), lit), width + 1f);
			// The white core runs out along it: from the pin when the ice sings it, from his feet when he does.
			float run = Mathf.Clamp(age / 0.16f, 0f, 1f);
			float from = by == LitBy.Song ? 0f : Mathf.Max(0f, litFrom[s] - run);
			float to = by == LitBy.Song ? run : Mathf.Min(1f, litFrom[s] + run);
			Shake(c, s, amplitude, from, to);
			strings.DrawPolyline(line, new Color(Kantele.Core(s), lit), Mathf.Max(2f, width - 2f));
			// The pin flares with it.
			strings.DrawCircle(c + Kantele.Inner(s), 4f + 3f * lit, new Color(Kantele.Mid(s), lit));
			strings.DrawCircle(c + Kantele.Inner(s), 2f + 1.5f * lit, new Color(Kantele.Core(s), lit));
		}
		DrawPips(c);
	}

	// Writes the string between <paramref name="from"/> and <paramref name="to"/> of its length into the buffer,
	// displaced across itself by a standing wave of <paramref name="amplitude"/> (fullest at its middle).
	private void Shake(Vector2 c, int s, float amplitude, float from, float to)
	{
		Vector2 a = c + Kantele.Inner(s), b = c + Kantele.Outer(s);
		Vector2 across = (b - a).Orthogonal().Normalized();
		for (int k = 0; k < StringPoints; k++)
		{
			float u = Mathf.Lerp(from, to, k / (float)(StringPoints - 1));
			line[k] = a.Lerp(b, u) + across * (amplitude * Mathf.Sin(u * Mathf.Pi));
		}
	}

	// The phrase's notes as a row of pips under the block: hollow while to come, white as the ice sings each, filled in
	// the colour of each string he has sung back. They count; they never say which.
	private void DrawPips(Vector2 c)
	{
		if (voice == Voice.Waiting || winBeat >= 0f)
			return;
		bool gave = voice == Voice.Gives;
		int length = gave ? Kantele.PhraseLengths[Math.Clamp(answer.Phrase - 1, 0, Kantele.PhraseCount - 1)] : answer.Length;
		int filled = gave ? length : answer.Answered;
		const float gap = 15f;
		Vector2 first = c + new Vector2(-(length - 1) * gap * 0.5f, 34f);
		int heard = voice == Voice.Singing ? Mathf.Min(SungThisSinging, length) : 0;
		for (int k = 0; k < length; k++)
		{
			Vector2 p = first + new Vector2(k * gap, 0f);
			strings.DrawCircle(p, 5.5f, new Color(0.02f, 0.03f, 0.05f, 0.7f));
			if (voice != Voice.Wrong && k < filled)
			{
				int s = sungBack[k];
				strings.DrawCircle(p, 4.5f, Kantele.Mid(s));
				strings.DrawCircle(p + new Vector2(-1f, -1f), 1.6f, Kantele.Core(s));
			}
			else if (k < heard)
				strings.DrawCircle(p, 3.5f, new Color(Frost, 0.95f));
			else
				strings.DrawArc(p, 3.8f, 0f, Mathf.Tau, 12, new Color(voice == Voice.Wrong ? Dull : Frost, 0.8f), 1.5f);
		}
	}

	public override void _Draw()
	{
		if (IsResolved || winBeat < 0f)
			return;
		// The beat on the ice: meltwater running out round the foot while the chips come down. Each chip is a pure
		// function of its index and the clock - nothing allocated, nothing kept.
		Vector2 c = ToLocal(block);
		float fall = Mathf.Clamp((winBeat - WinCrackSeconds) / WinFallSeconds, 0f, 1f);
		if (fall <= 0f)
			return;
		DrawCircle(c, 10f + 36f * fall, new Color(0.30f, 0.52f, 0.62f, 0.26f * (1f - 0.4f * fall)));
		DrawArc(c, 16f + 52f * fall, 0f, Mathf.Tau, 32, new Color(IceMid, 0.5f * (1f - fall)), 3f);
		for (int i = 0; i < ChipCount; i++)
		{
			float t = Mathf.Max(0f, fall * WinFallSeconds - (i % 5) * 0.06f);
			if (t <= 0f)
				continue;
			float sx = ((i * 37) % 11 - 5) * 4f;
			float sy = -(16f + ((i * 53) % 10) * 8f);
			float vx = (i % 2 == 0 ? 1f : -1f) * (14f + (i * 29) % 22);
			Vector2 p = c + new Vector2(sx + vx * t, sy - 30f * t + 150f * t * t);
			// Size carries the fade, never alpha alone (the pixel-pass rule).
			float r = 3f - 1.6f * fall;
			float ground = 4f + (i % 3) * 3f;
			if (p.Y >= c.Y + ground)
			{
				// Landed: a melt pip on the ice, thinning away.
				DrawCircle(new Vector2(p.X, c.Y + ground), r + 1.5f, new Color(0.35f, 0.58f, 0.68f, 0.5f * (1f - fall)));
				continue;
			}
			DrawCircle(p, r + 1f, new Color(IceMid, 0.85f));
			DrawCircle(p, r * 0.5f, new Color(0.92f, 0.99f, 1f, 0.9f));
		}
	}

	// The far tell, and a glow round him that breathes - found things breathe, his things flicker, and this one is
	// both: the light is the dark wizard's ice, the breath is the singer inside it. As the phrases are sung back a warm
	// glow grows in the block; a string that sounds throws its own colour off the ice.
	protected override void DrawLight(CanvasItem light)
	{
		// The far tell only while nobody has come: it is how the lake is found, and its column and pool, added over the
		// block once he is standing there, washed the wizard in it out.
		if (IsDormant)
			base.DrawLight(light);
		if (IsResolved || !NearPlayer(block))
			return;
		Vector2 c = ToLocal(block);
		float breath = 0.82f + 0.18f * Mathf.Sin(Clock * 1.7f);
		// A wide, faint breath over the lake; the bright light round him is under the ice (ThawLake).
		ExploreArt.DrawGlow(light, c, 260f, new Color(IceMid, 0.06f * breath));
		if (!IsDormant)
		{
			for (int s = 0; s < Kantele.StringCount; s++)
			{
				float lit = LitStrength(s, out LitBy by);
				if (lit <= 0f || by == LitBy.Wrong)
					continue;
				for (int k = 0; k < 4; k++)
					ExploreArt.DrawGlow(light, c + Kantele.Along(s, 0.12f + 0.25f * k), 30f * lit + 10f, new Color(Kantele.Mid(s), 0.30f * lit));
				ExploreArt.DrawGlow(light, c + Kantele.Inner(s), 22f * lit + 6f, new Color(Kantele.Hot(s), 0.45f * lit));
			}
		}
		float t = thawShown;
		float warm = t;
		if (winBeat >= 0f)
		{
			// The crack: one cold flash out of the block; then the warmth dies with the ice.
			if (winBeat < 0.55f)
				ExploreArt.DrawGlow(light, c + new Vector2(0f, -48f), 60f + 200f * winBeat, new Color(IceMid, 0.5f * (1f - winBeat / 0.55f)));
			warm = t * (1f - Mathf.Clamp((winBeat - WinCrackSeconds) / WinFallSeconds, 0f, 1f));
		}
		if (warm > 0f)
			ExploreArt.DrawGlow(light, c + new Vector2(0f, -48f), 70f + 30f * t, new Color(FireEdge, 0.28f * warm));
	}
}
