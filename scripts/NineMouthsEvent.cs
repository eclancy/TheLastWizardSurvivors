using Godot;
using WizardSurvivors.scripts;

// The Sunken Cave's side event: the nine mouths (.ai/chapter-wizards.md 4.3).
//
// "His shadow followed the water down into the dark." The Cumaean Sibyl sees what is coming, so he keeps her
// where nobody comes to ask: under the water, behind rock. The cave at Cumae had a hundred mouths and spoke
// with a hundred voices; here there are nine, a crescent of the caverns' own rock with an opening at the foot
// of each, and every few seconds her voice comes out of ALL of them, one a beat after another. The first is
// hers. The other eight are the cave giving it back.
//
// What it asks: LISTEN. Stand in the mouth her voice comes out of first for HoldSeconds and she is free. Stand
// in any other for WrongDwellSeconds and that mouth answers instead - a flight of Gloomwings (the chapter's
// fast creature) out of it - and the order SHIFTS: hers moves to another mouth, and the next call says which.
// Nothing fails or locks; a wrong mouth costs a fight and a fresh listen. Every other event is a place, a
// target, a carry, a fight or a puzzle in plain sight; this one is only heard, and seen in step with it.
//
// FAIR ON A PHONE SPEAKER. Section 7's warning: a phone has no stereo worth the name, so the panning that
// places each voice on a PC is gone. The picture carries the whole answer on its own: on each voice its mouth
// breathes out cold light and a puff of written leaves, in the call's order, so the first puff is hers - and
// her leaves (and only hers) carry the warm glint, the secrets' tell for a light he does not have
// (.ai/stage-secrets.md section 2). And the ear has a second tell besides the order: hers is a person's voice,
// near and dry (sibyl_voice); the other eight are the same line from deep in the rock, dark and hollow
// (sibyl_echo). Order, timbre and glint all point at one mouth.
//
// THE NEW PRIMITIVE is per-mouth delayed audio (PlacedSoundSchedule): each call schedules nine positioned
// voices VoiceBeat apart, and the schedule's hook spawns that mouth's leaves and breath on the frame its
// voice plays, so sound and picture can never drift apart.
//
// A probe that cheats would read TrueMouth. _ExploreProbe listens instead: it reads LastCallFirst, the mouth
// that sounded first on the last call - which is what a player hears and sees - and is -1 after a shift
// until she has called again.
//
// Art: tools/art/nine_mouths.py (world/mouths/). The cliff is ONE painting cut into nine strips, each
// y-sorted at its own mouth's foot, so whoever walks behind the rock is hidden by it, and there is no seam.
// The rock is not terrain: like the seal's stones, it blocks nothing and joins no group. The leaves are the
// hint cue's written leaf (hint-cues.png, the Writ row), drawn at x2 in the world; the light is code, on the
// additive layer every event has.
public partial class NineMouthsEvent : SideEvent
{
	public const int MouthCount = 9;

	// The crescent: mouth i at x = (i - 4) * Spacing, its foot at y = -Depth * (1 - u^2), u = (i - 4) / 4,
	// rounded to the painting's 2-unit grid - from CliffOrigin. tools/art/nine_mouths.py: SPACING, DEPTH.
	public const float Spacing = 60f;
	public const float Depth = 120f;

	/// <summary>Where a mouth's threshold is: this far in front of (south of) the foot of its opening.</summary>
	public const float ThresholdAhead = 26f;

	/// <summary>His measuring point this near a threshold's middle is standing in that mouth.</summary>
	public const float ThresholdRadius = 26f;

	/// <summary>How far below the player's node the point that stands in a mouth is: halfway to his feet.</summary>
	public const float MeasureBelow = 18f;

	/// <summary>One voice to the next in a call (sfx._SIBYL_BEAT).</summary>
	public const float VoiceBeat = 0.24f;

	/// <summary>From one call's first voice to the next's.</summary>
	public const float CallInterval = 5.5f;

	public const float FirstCallDelay = 1.2f;
	public const float AfterAnswerDelay = 2.6f;

	/// <summary>Standing in hers this long frees her. Out of it the hold drains at a quarter of that rate.</summary>
	public const float HoldSeconds = 2f;

	/// <summary>Standing in another this long, and it answers.</summary>
	public const float WrongDwellSeconds = 0.6f;

	/// <summary>Gloomwings in the flight a wrong mouth answers with.</summary>
	public const int FlightSize = 5;

	// The painting (tools/art/nine_mouths.py: IW, IH, OX, OY, END), in art px, drawn at x2.
	private const int PaintingWidth = 300;
	private const int PaintingHeight = 116;
	private const int PaintingOriginX = 150;
	private const int PaintingOriginY = 112;
	private const int RockEnd = 140;
	private const string ArtPath = "res://assets/bonelight/world/mouths/mouths-cliff.png";
	private const string SibylIdlePath = "res://assets/bonelight/characters/sibyl-idle.png";

	// The site (its POI and its footprint) sits on the middle of the whole thing - the cliff's top to the
	// thresholds - which is this far north of the cliff's origin.
	private const float SiteAboveOrigin = 78f;

	private Vector2 origin;
	private int trueMouth;
	private readonly int[] order = new int[MouthCount];
	private RandomNumberGenerator shiftRng;
	private bool laidOut;

	private PlacedSoundSchedule voices;
	private float nextCall = -1f;
	private int lastCallFirst = -1;
	private int callFirstPending = -1;
	private int callsHeard;

	private int standingIn = -1;
	private float standingFor;
	private bool answeredThisVisit;
	private float hold;
	private int wrongAnswers;
	private int gloomwingsSent;

	// When each mouth last spoke (game clock), and whether that voice was hers - for its breath of light.
	private readonly float[] spokeAt = { -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f };
	private readonly float[] answeredAt = { -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f };

	protected override string Announcement => "A voice comes out of every mouth in the rock.";
	protected override string ChestLine => "Her mouth falls quiet - a chest was left in it";
	protected override string Objective => "Listen - stand in the mouth the voice comes from first";
	// To the cliff while he is away from it; nothing once he is at it. Pointing at a mouth would be answering.
	protected override Vector2? PointerTarget =>
		IsResolved || Player == null || PlayerWithin(origin + new Vector2(0f, -40f), 380f) ? null : origin + new Vector2(0f, -60f);
	protected override string SiteGlyph => "mouths";
	// The whole crescent and its thresholds, and a little more: placement wants all of it open, and the
	// furnishing keeps off it. The crescent is 560 px wide, so this is the widest event footprint there is.
	protected override float SiteFootprint => 300f;
	protected override float ArrivalRadius => 460f;
	protected override Color TellColor => new Color(0.435f, 0.722f, 0.941f);   // the water ramp's mid #6FB8F0

	// --- for the probe and the shots -----------------------------------------------------------------

	/// <summary>The mouth that is hers now. The probe never reads it to win; the validator and the shots do.</summary>
	public int TrueMouth => trueMouth;

	/// <summary>The mouth whose voice came first on the last call: what a player heard. -1 since a shift.</summary>
	public int LastCallFirst => lastCallFirst;

	public int CallsHeard => callsHeard;
	public int WrongAnswers => wrongAnswers;
	public int GloomwingsSent => gloomwingsSent;

	/// <summary>Where to put the player's node so he stands in mouth <paramref name="i"/>.</summary>
	public Vector2 StandPoint(int i) => Threshold(i) - new Vector2(0f, MeasureBelow);

	/// <summary>A spot in front of the cliff that is in no mouth: where a listener waits for a call.</summary>
	public Vector2 ListeningPoint => origin + new Vector2(0f, 130f);

	/// <summary>Starts the next call now - for _UiShot, so the photograph has leaves in the air.</summary>
	public void CallNow() => nextCall = 0f;

	// --- the shape -------------------------------------------------------------------------------------

	/// <summary>Mouth <paramref name="i"/>'s foot (the sill of its opening), from the cliff's origin.</summary>
	public static Vector2 MouthOffset(int i)
	{
		float u = (i - 4) / 4f;
		return new Vector2((i - 4) * Spacing, -2f * Mathf.Round(Depth * 0.5f * (1f - u * u)));
	}

	private Vector2 MouthFoot(int i) => origin + MouthOffset(i);

	private Vector2 Threshold(int i) => MouthFoot(i) + new Vector2(0f, ThresholdAhead);

	// Where the voice comes out: the middle of the opening, about halfway up it.
	private Vector2 MouthVoice(int i) => MouthFoot(i) + new Vector2(0f, -22f);

	/// <summary>Mouth i's strip of the painting, in art px (nine_mouths.strip_bounds).</summary>
	public static Rect2I StripRect(int i)
	{
		int half = (int)Spacing / 2;
		int x0 = i == 0 ? PaintingOriginX - RockEnd : PaintingOriginX + (i - 4) * half - half / 2;
		int x1 = i == MouthCount - 1 ? PaintingOriginX + RockEnd + 1 : PaintingOriginX + (i - 4) * half + half / 2;
		return new Rect2I(x0, 0, x1 - x0, PaintingHeight);
	}

	/// <summary>
	/// The call's order: hers first, the other eight after it in a seeded shuffle. Public and pure for the
	/// validator, which proves hers is always first and every mouth speaks exactly once.
	/// </summary>
	public static void OrderFor(int first, RandomNumberGenerator rng, int[] into)
	{
		into[0] = first;
		int n = 1;
		for (int i = 0; i < MouthCount; i++)
			if (i != first)
				into[n++] = i;
		for (int k = MouthCount - 1; k > 1; k--)
		{
			int j = 1 + (int)(rng.Randi() % (uint)k);
			(into[k], into[j]) = (into[j], into[k]);
		}
	}

	/// <summary>
	/// Where hers moves after a wrong mouth answers: never where it was, never the mouth that answered, and
	/// at least two mouths from where it was - a shift a player can hear, not a step he could stumble into.
	/// </summary>
	public static int ShiftFrom(int was, int answered, RandomNumberGenerator rng)
	{
		for (int tries = 0; tries < 32; tries++)
		{
			int next = (int)(rng.Randi() % MouthCount);
			if (next != was && next != answered && Mathf.Abs(next - was) >= 2)
				return next;
		}
		return (was + 4) % MouthCount == answered ? (was + 5) % MouthCount : (was + 4) % MouthCount;
	}

	/// <summary>
	/// The crescent on open ground over its whole disc, as the seal's courtyard is; then hers, from the seed.
	/// On the caverns a 300 px disc fits only in a chamber or a hall, so the placer sweeps the band for one.
	/// </summary>
	public override void PlaceSites(ExplorePlacer placer)
	{
		AddSite(placer.EventSiteInBand(SiteFootprint, dense: true));
		LayOut(placer.NextFloat(), placer.NextFloat());
	}

	private void LayOut(float trueRoll, float shiftRoll)
	{
		origin = MainSite + new Vector2(0f, SiteAboveOrigin);
		trueMouth = Mathf.Clamp((int)(trueRoll * MouthCount), 0, MouthCount - 1);
		shiftRng = new RandomNumberGenerator { Seed = (ulong)(shiftRoll * 2147483647.0) + 7919UL };
		laidOut = true;
	}

	// --- what stands there --------------------------------------------------------------------------

	// The cliff and its leaves are the event's SIBLINGS, as the seal's courtyard is: the rock stays after the
	// win. Y-sorted, so each strip sorts with the bodies at its mouth's foot.
	private Node2D cliff;
	private Node2D leafLayer;
	private Texture2D cueSheet;
	private Sprite2D sibyl;
	private float freedFor = -1f;
	// Where she steps to: out of her mouth and aside from whoever freed her, who is standing in front of it.
	private Vector2 stepTo;

	protected override void OnPlaced()
	{
		if (!laidOut)
			LayOut(0.37f, 0.61f);   // placed without the director (nothing in play does): a fixed start
		voices = new PlacedSoundSchedule(MouthCount + 2, OnVoice);

		Texture2D art = ResourceLoader.Exists(ArtPath) ? GD.Load<Texture2D>(ArtPath) : null;
		cueSheet = ResourceLoader.Exists(StageHintCue.SheetPath) ? GD.Load<Texture2D>(StageHintCue.SheetPath) : null;
		if (art == null || cueSheet == null)
		{
			GD.PushError("NineMouthsEvent: the cliff's art or the hint cue sheet is missing or not imported (python tools/art/nine_mouths.py and hint_cues.py, then import).");
			return;
		}

		cliff = new Node2D { Name = "NineMouths", YSortEnabled = true };
		for (int i = 0; i < MouthCount; i++)
		{
			Rect2I r = StripRect(i);
			Vector2 foot = MouthFoot(i);
			// Painting pixel (px, py) lands at origin + 2 * (px - OX, py - OY), whichever strip draws it.
			cliff.AddChild(new Sprite2D
			{
				Name = $"Mouth{i}",
				Texture = art,
				RegionEnabled = true,
				RegionRect = new Rect2(r.Position, r.Size),
				Centered = false,
				Scale = new Vector2(2f, 2f),
				TextureFilter = TextureFilterEnum.Nearest,
				Position = new Vector2(Mathf.Round(foot.X), Mathf.Round(foot.Y - WorldSprites.SortLift)),
				Offset = new Vector2(r.Position.X - PaintingOriginX - MouthOffset(i).X / 2f,
					-PaintingOriginY - MouthOffset(i).Y / 2f + WorldSprites.SortLift / 2f),
			});
		}
		leafLayer = new Node2D
		{
			Name = "MouthLeaves",
			ZAsRelative = false,
			ZIndex = DrawLayers.Air,
			TextureFilter = TextureFilterEnum.Nearest,
		};
		leafLayer.Draw += DrawLeaves;
		cliff.AddChild(leafLayer);
		Game?.CallDeferred(Node.MethodName.AddChild, cliff);
	}

	public override void _ExitTree()
	{
		if (leafLayer != null && IsInstanceValid(leafLayer))
		{
			leafLayer.Draw -= DrawLeaves;
			// A canvas keeps its last drawing until it is asked again: with no handler, the next one is empty.
			leafLayer.QueueRedraw();
		}
		// Freed before she finished stepping out (nothing in play does that): she does not stay half-drawn.
		if (sibyl != null && IsInstanceValid(sibyl))
			sibyl.QueueFree();
		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		float dt = (float)delta;
		TickLeaves(dt);
		if (sibyl != null && IsInstanceValid(sibyl))
		{
			// She steps out of her mouth and aside, and is gone with the column of light.
			freedFor += dt;
			sibyl.Frame = (int)(freedFor * 6f) % 4;
			float step = Mathf.Clamp(freedFor / 0.9f, 0f, 1f);
			Vector2 at = MouthFoot(trueMouth).Lerp(stepTo, step * (2f - step));
			sibyl.Position = new Vector2(Mathf.Round(at.X), Mathf.Round(at.Y - WorldSprites.SortLift));
			float a = Mathf.Min(Mathf.Clamp(freedFor / 0.4f, 0f, 1f), Mathf.Clamp((3.6f - freedFor) / 0.8f, 0f, 1f));
			sibyl.Modulate = new Color(1f, 1f, 1f, a);
			if (freedFor >= 3.6f)
			{
				sibyl.QueueFree();
				sibyl = null;
			}
		}
	}

	// --- the rules -----------------------------------------------------------------------------------

	protected override void Begin()
	{
		nextCall = FirstCallDelay;
		standingIn = MouthAt();
		standingFor = 0f;
		// Whatever mouth he arrives standing in does not answer until he has stepped out of it.
		answeredThisVisit = standingIn >= 0 && standingIn != trueMouth;
	}

	// The mouth whose threshold he is standing in, or -1: the nearest one within reach of his measuring point.
	private int MouthAt()
	{
		if (Player == null)
			return -1;
		Vector2 at = Player.GlobalPosition + new Vector2(0f, MeasureBelow);
		int best = -1;
		float bestD = ThresholdRadius * ThresholdRadius;
		for (int i = 0; i < MouthCount; i++)
		{
			float d = at.DistanceSquaredTo(Threshold(i));
			if (d <= bestD)
			{
				bestD = d;
				best = i;
			}
		}
		return best;
	}

	protected override void Tick(float delta)
	{
		voices.Tick(delta);
		if (nextCall >= 0f)
		{
			nextCall -= delta;
			if (nextCall <= 0f)
				Call();
		}

		int mouth = MouthAt();
		if (mouth != standingIn)
		{
			standingIn = mouth;
			standingFor = 0f;
			answeredThisVisit = false;
		}
		if (mouth >= 0)
			standingFor += delta;

		if (mouth >= 0 && mouth == trueMouth)
		{
			hold += delta / HoldSeconds;
		}
		else
		{
			hold = Mathf.Max(0f, hold - delta / (HoldSeconds * 4f));
			if (mouth >= 0 && !answeredThisVisit && standingFor >= WrongDwellSeconds)
				Answer(mouth);
		}

		if (hold >= 1f)
		{
			hold = 1f;
			voices.Clear();
			SfxPlayer.Impact(Element.Arcane, Threshold(trueMouth));
			Succeed(Threshold(trueMouth));
			StepOut();
		}
	}

	// One call: hers first, then the other eight a beat apart, each from its own mouth.
	private void Call()
	{
		nextCall = CallInterval;
		OrderFor(trueMouth, shiftRng, order);
		callFirstPending = order[0];
		voices.Clear();
		for (int k = 0; k < MouthCount; k++)
		{
			int i = order[k];
			voices.Schedule(k == 0 ? SfxCatalog.SibylVoice : SfxCatalog.SibylEcho, MouthVoice(i), k * VoiceBeat, i);
		}
	}

	// A voice has come out of mouth i (PlacedSoundSchedule's hook): its breath of light, its leaves - hers
	// glinting - and, for the first of a call, what the listener now knows.
	private void OnVoice(int i)
	{
		bool hers = i == trueMouth;
		if (i == callFirstPending)
		{
			lastCallFirst = i;
			callsHeard++;
			callFirstPending = -1;
		}
		spokeAt[i] = Clock;
		for (int n = 0; n < (hers ? 4 : 3); n++)
			SpawnLeaf(i, hers);
	}

	// A wrong mouth answers: a flight of Gloomwings out of it, and hers moves. The next call says where.
	private void Answer(int mouth)
	{
		answeredThisVisit = true;
		wrongAnswers++;
		answeredAt[mouth] = Clock;
		voices.Clear();
		callFirstPending = -1;
		SfxPlayer.AtPosition(SfxCatalog.SibylEcho, MouthVoice(mouth), 0f);
		if (Game != null)
		{
			for (int n = 0; n < FlightSize; n++)
			{
				float a = Mathf.Pi * 0.5f + (n - (FlightSize - 1) * 0.5f) * 0.42f;
				Vector2 at = MouthFoot(mouth) + new Vector2(Mathf.Cos(a) * 30f, 8f + Mathf.Sin(a) * 18f);
				if (Game.SpawnEventEnemy(BestiaryRole.Fast, at, 1f, false) != null)
					gloomwingsSent++;
			}
		}
		trueMouth = ShiftFrom(trueMouth, mouth, shiftRng);
		lastCallFirst = -1;
		hold = 0f;
		nextCall = AfterAnswerDelay;
	}

	private void StepOut()
	{
		var idle = ResourceLoader.Exists(SibylIdlePath) ? GD.Load<Texture2D>(SibylIdlePath) : null;
		if (idle == null || cliff == null || !IsInstanceValid(cliff))
			return;
		Vector2 foot = MouthFoot(trueMouth);
		float side = Player != null && Player.GlobalPosition.X > foot.X ? -1f : 1f;
		stepTo = foot + new Vector2(side * 46f, 14f);
		sibyl = new Sprite2D
		{
			Name = "Sibyl",
			Texture = idle,
			Hframes = 4,
			Centered = false,
			Scale = new Vector2(2f, 2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Position = new Vector2(Mathf.Round(foot.X), Mathf.Round(foot.Y - WorldSprites.SortLift)),
			// The wizard cell: middle column 24, contact on row 47.
			Offset = new Vector2(-24f, WorldSprites.SortLift / 2f - 47f),
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		cliff.AddChild(sibyl);
		freedFor = 0f;
	}

	protected override void OnStoodDown() => voices?.Clear();

	// --- the leaves ----------------------------------------------------------------------------------

	private struct Leaf
	{
		public bool Live;
		public bool Hers;
		public Vector2 At;
		public Vector2 Velocity;
		public float Age;
		public float Life;
		public float Phase;
		public int Glyph;
	}

	private readonly Leaf[] leaves = new Leaf[40];
	private int leafSeed = 1;

	private float Hash()
	{
		leafSeed = leafSeed * 1103515245 + 12345;
		return ((leafSeed >> 8) & 0xFFFF) / 65535f;
	}

	// Blown out of the opening on the cold draught, toward whoever is listening, turning as they go.
	private void SpawnLeaf(int mouth, bool hers)
	{
		for (int s = 0; s < leaves.Length; s++)
		{
			if (leaves[s].Live)
				continue;
			leaves[s] = new Leaf
			{
				Live = true,
				Hers = hers,
				At = MouthVoice(mouth) + new Vector2((Hash() - 0.5f) * 16f, (Hash() - 0.5f) * 14f),
				Velocity = new Vector2((Hash() - 0.5f) * 70f, 40f + Hash() * 45f),
				Age = 0f,
				Life = 1.5f + Hash() * 0.5f,
				Phase = Hash() * Mathf.Tau,
				Glyph = (int)(Hash() * StageHintCue.Columns) % StageHintCue.Columns,
			};
			return;
		}
	}

	private bool leavesDrawn;

	private void TickLeaves(float delta)
	{
		bool any = false;
		for (int s = 0; s < leaves.Length; s++)
		{
			ref Leaf l = ref leaves[s];
			if (!l.Live)
				continue;
			l.Age += delta;
			if (l.Age >= l.Life)
			{
				l.Live = false;
				continue;
			}
			// The draught slackens: they slow, and drift sideways as they settle.
			l.Velocity *= 1f - 0.9f * delta;
			l.At += l.Velocity * delta;
			any = true;
		}
		if ((any || leavesDrawn) && leafLayer != null && IsInstanceValid(leafLayer))
			leafLayer.QueueRedraw();
		leavesDrawn = any;
	}

	private void DrawLeaves()
	{
		if (cueSheet == null)
			return;
		const float half = StageHintCue.Cell;     // a 16 px cell at x2
		int row = (int)HintCue.Writ;
		for (int s = 0; s < leaves.Length; s++)
		{
			Leaf l = leaves[s];
			if (!l.Live)
				continue;
			float a = Mathf.Min(Mathf.Clamp(l.Age / 0.12f, 0f, 1f), Mathf.Clamp((l.Life - l.Age) / 0.5f, 0f, 1f));
			Vector2 at = l.At + new Vector2(Mathf.Sin(l.Age * 3.3f + l.Phase) * 8f, Mathf.Sin(l.Age * 6.1f + l.Phase) * 3f);
			at = new Vector2(Mathf.Round(at.X / 2f) * 2f, Mathf.Round(at.Y / 2f) * 2f);
			int column = (l.Glyph + (int)(l.Age * 8f)) % StageHintCue.Columns;
			var src = new Rect2(column * StageHintCue.Cell, row * StageHintCue.Cell, StageHintCue.Cell, StageHintCue.Cell);
			leafLayer.DrawTextureRectRegion(cueSheet, new Rect2(at.X - half, at.Y - half, half * 2f, half * 2f), src, new Color(1f, 1f, 1f, a));
			// Hers carry the warm glint: the one thing on the cliff that is not his.
			if (l.Hers && a > 0.15f)
				SecretSite.DrawGlint(leafLayer, at + new Vector2(0f, -2f), SecretSite.GlintPeriod * 0.5f + l.Phase * 0.05f, a);
		}
	}

	// --- drawing -------------------------------------------------------------------------------------

	private static readonly Color Cold = new(0.435f, 0.722f, 0.941f);       // water mid #6FB8F0
	private static readonly Color ColdEdge = new(0.165f, 0.373f, 0.769f);   // water edge #2A5FC4
	private static readonly Color Answered = new(0.478f, 0.247f, 0.831f);   // arcane edge #7A3FD4: his

	public override void _Draw()
	{
		if (IsResolved || !IsFound)
			return;
		// Round each threshold, the track every event's progress uses: the nine places to stand. The one he
		// is in fills - hers with the hold, any other only toward the moment it answers - so a ring cannot
		// tell a wrong mouth from hers before it is too late to step out.
		for (int i = 0; i < MouthCount; i++)
		{
			float fill = 0f;
			if (i == standingIn)
				fill = i == trueMouth ? hold : (answeredThisVisit ? 0f : standingFor / HoldSeconds);
			DrawProgressArc(Threshold(i), ThresholdRadius + 4f, fill, Cold, 2f, 0.35f);
		}
	}

	// The far tell while nobody has come; then each mouth's breath of cold light on its voice, a flare of his
	// colour out of a mouth that answered, and a pool on hers while he holds it.
	protected override void DrawLight(CanvasItem light)
	{
		if (IsDormant)
			base.DrawLight(light);
		if (IsResolved || !NearPlayer(origin))
			return;
		for (int i = 0; i < MouthCount; i++)
		{
			Vector2 m = ToLocal(MouthVoice(i));
			float since = Clock - spokeAt[i];
			if (since >= 0f && since < 0.7f)
			{
				float k = 1f - since / 0.7f;
				ExploreArt.DrawGlow(light, m, 46f, new Color(Cold, 0.34f * k));
				ExploreArt.DrawGlow(light, m + new Vector2(0f, 26f), 30f, new Color(ColdEdge, 0.22f * k));
			}
			float answered = Clock - answeredAt[i];
			if (answered >= 0f && answered < 1.0f)
				ExploreArt.DrawGlow(light, m, 70f, new Color(Answered, 0.42f * (1f - answered)));
		}
		if (standingIn == trueMouth && hold > 0f)
			ExploreArt.DrawGlow(light, ToLocal(Threshold(trueMouth)), 40f + 30f * hold, new Color(Cold, 0.12f + 0.18f * hold));
	}
}
