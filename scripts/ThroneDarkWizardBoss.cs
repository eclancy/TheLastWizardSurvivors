using Godot;
using System;
using System.Collections.Generic;
using WizardSurvivors.scripts;

// THE DARK WIZARD (.ai/endgame-castle.md 6.4). The Throne's last fight and the story's end; never named on
// screen - the banner is his line, "He takes what you have. Take it back."
//
// THE VERB NO OTHER BOSS ASKS: TAKE IT BACK. He took every wizard's magic; now he reaches for the player's.
//
//   PHASE 0 (to 66%): HE TAKES. Every TakingIntervalSeconds he raises his hand - a long tell, the alert over
//     his head and his light gathering in his palm - and throws a hand of his dark light (DarkHand): slow,
//     slower to turn, and it takes nothing if it is dodged. If it lands, one of the wizard's spells is
//     SEALED (Player.SealSpell: it stops firing and its slot on the loadout strip wears the seal) and goes
//     out of him as an orb with its icon in it, carried back toward his raised hand at about 60 px a second
//     (SealOrb). Touch the orb and the spell is back at once; let it reach him and it stays sealed for
//     SealSeconds. He is targetable throughout. Up close he answers with a ring of his light round himself
//     (the Slam), so the chase for an orb that drifts home to him is a chase into his reach.
//   PHASE 1 (to 33%): HIS LIGHT. The hall goes dark but for him and the player (DarkVeil), and he fires lines
//     of his dark light across it (TelegraphedGroundHit lanes, through and beside the player). The takings
//     go on. Darkness dealt to him counts DarknessBonus more (IncomingSpellMultiplier): "a player can end up
//     throwing his own element back at him" (.ai/world-and-tone.md), made true.
//   PHASE 2 (to 0): ALL HE TOOK. Between takings he casts, one every EchoIntervalSeconds, the telegraph of a
//     warden the player has beaten in the procession (ThroneRules.FelledWardenIds) - the Hayward's cone, the
//     Gaoler's lane, Mother Rot's pools, the Archivist's sweep, the Still Warden's falling ice, the Long
//     Coil's surfacing - as ground hits placed round the player, each in its own warden's colour, so the
//     player knows it before it lands. The road, thrown back one last time. The hall stays dark.
//
// THE FIVE HOOKS and nothing else (BossEnemy): the Slam is his ring; two thresholds; OnPhaseEntered turns
// the dark on and then the echoes; he plants through his ring's wind-up; he rings only a player at his feet.
// The taking, the lines and the echoes run on their own clocks beside the Slam, as Elderbark's roots do.
//
// HIS ARRIVAL (BossDeath, entrance): his shadow pools on the floor in front of the throne, he rises out of
// it as the dark it is, the eclipse round his head opens, and the light in his raised hand catches last.
// HIS FALL: his dark light goes out - the eclipse flares once and closes, the hall's dark lifts - and the
// warm light he took leaves his body as a storm of motes flying off the screen in every direction, the
// magic going home, while what is left of him sinks back into his shadow. Every seal lets go as he falls.
public partial class ThroneDarkWizardBoss : BossEnemy
{
	// --- the taking (every phase) --------------------------------------------------------------------
	[Export] public float TakingIntervalSeconds { get; set; } = 12f;
	/// <summary>The long tell before the hand is thrown: he raises it and his light gathers in it.</summary>
	[Export] public float TakingTellSeconds { get; set; } = 1.4f;
	[Export] public float HandSpeed { get; set; } = 170f;
	[Export] public float HandTurnDegreesPerSecond { get; set; } = 80f;
	[Export] public float HandLifeSeconds { get; set; } = 6f;
	/// <summary>How close the hand must come to the wizard's body to close on him.</summary>
	[Export] public float HandRadius { get; set; } = 24f;
	[Export] public float OrbDriftSpeed { get; set; } = 60f;
	[Export] public float OrbPopDistance { get; set; } = 170f;
	/// <summary>How long a spell stays sealed once its orb has reached him.</summary>
	[Export] public float SealSeconds { get; set; } = 20f;
	/// <summary>Phase 2 throws a little more often: the takings go on, and the last phase is the hardest.</summary>
	[Export] public float LastPhaseTakingIntervalSeconds { get; set; } = 10f;

	// --- phase 1: his light --------------------------------------------------------------------------
	[Export] public float DarkHealthFraction { get; set; } = 0.66f;
	[Export] public float LineIntervalSeconds { get; set; } = 4.2f;
	[Export] public float LineTellSeconds { get; set; } = 1.1f;
	/// <summary>A volley's second and third lanes land this long after its first.</summary>
	[Export] public float LineStaggerSeconds { get; set; } = 0.35f;
	[Export] public float LineHalfWidth { get; set; } = 26f;
	[Export] public float LineLength { get; set; } = 1500f;
	[Export] public int LineDamage { get; set; } = 5;
	/// <summary>The gap between a volley's parallel lanes, edge to edge: room to stand between them.</summary>
	[Export] public float LineGap { get; set; } = 84f;
	[Export] public float DarknessBonus { get; set; } = 0.25f;

	// --- phase 2: all he took ------------------------------------------------------------------------
	[Export] public float EchoIntervalSeconds { get; set; } = 6f;
	[Export] public float EchoTellSeconds { get; set; } = 1.2f;
	[Export] public int EchoDamage { get; set; } = 6;

	// --- how he stands off -----------------------------------------------------------------------------
	/// <summary>Below this he backs away; above <see cref="StandoffDistance"/> he closes (RangedEnemy's bands).</summary>
	/// <remarks>
	/// Held inside what a phone shows either side of the wizard (about 425 px, less his own half-width): at
	/// 400 he stood at the phone's edge, half off it, for most of the fight.
	/// </remarks>
	[Export] public float RetreatDistance { get; set; } = 200f;
	[Export] public float StandoffDistance { get; set; } = 330f;

	/// <summary>Every warden he can echo, in the procession's order: the fallback when no procession was walked.</summary>
	public static readonly string[] EchoableWardens =
	{
		"fields_hayward", "castle_warden", "swamp_mother", "ruins_sentinel", "frost_warden", "sands_coil",
	};

	// The road's colours: each echo is drawn in its own warden's, so it is known before it lands (the
	// Hayward's shove, the Gaoler's violet, Mother Rot's rot, the Archivist's page-light, the rime, the sand).
	private static readonly Dictionary<string, Color> WardenColours = new(StringComparer.OrdinalIgnoreCase)
	{
		["fields_hayward"] = new Color(0.94f, 0.68f, 0.26f),
		["castle_warden"] = new Color(0.74f, 0.30f, 0.86f),
		["swamp_mother"] = new Color(0.52f, 0.78f, 0.34f),
		["ruins_sentinel"] = new Color(0.62f, 0.52f, 0.95f),
		["frost_warden"] = new Color(0.55f, 0.82f, 0.95f),
		["sands_coil"] = new Color(0.86f, 0.66f, 0.34f),
	};

	private AttackTelegraph taking;
	private bool linesOn;
	private bool echoesOn;
	private float untilLines;
	private float untilEcho;
	private int lineVolley;
	private int echoIndex;
	private DarkVeil veil;
	private readonly RandomNumberGenerator rng = new();
	private readonly List<SpellData> sealable = new(8);
	private readonly List<SealOrb> orbs = new();

	// --- what the tools read (_BossProbe, _UiShot) ------------------------------------------------------
	public int HandsThrown { get; private set; }
	public int HandsLanded { get; private set; }
	public int SealsTakenBack { get; private set; }
	public int SealsReachedHim { get; private set; }
	public int LinesCast { get; private set; }
	public int EchoesCast { get; private set; }
	/// <summary>Each echo's warden id, as cast.</summary>
	public readonly List<string> EchoLog = new();
	public bool DarkIsOn => veil != null && IsInstanceValid(veil) && veil.Shown;
	public bool TakingWindingUp => taking != null && taking.IsWindingUp;
	public int OrbsInFlight
	{
		get
		{
			int n = 0;
			foreach (SealOrb orb in orbs)
				if (IsInstanceValid(orb) && !orb.Spent)
					n++;
			return n;
		}
	}

	/// <summary>
	/// His raised hand, in the world: where a hand is thrown from and where an orb goes home to. The art's
	/// orb (boss_dark_wizard.py ORB, at 19, 7 on the 96 cell) at the sprite's x2, mirrored with it.
	/// </summary>
	public Vector2 HandPoint
	{
		get
		{
			bool mirrored = BodySprite != null && BodySprite.FlipH;
			return GlobalPosition + new Vector2(mirrored ? HandArtOffset.X * -1f : HandArtOffset.X, HandArtOffset.Y);
		}
	}

	private static readonly Vector2 HandArtOffset = new((19f - 48f) * 2f, (7f - 48f) * 2f);

	public override void _Ready()
	{
		// The ring is the Slam's; its last threshold is the third phase, which is all the validator's
		// enrage check reads (it holds EnrageHealthFraction inside 0-1).
		EnrageHealthFraction = 0.33f;
		base._Ready();
		rng.Randomize();
		taking = new AttackTelegraph(TakingIntervalSeconds, TakingTellSeconds);
		// The first taking comes a few seconds after he lands, not a whole interval: the fight's verb is
		// shown at once.
		taking.Prime();
		takingDelay = FirstTakingSeconds;
	}

	private float takingDelay;
	private const float FirstTakingSeconds = 3f;

	public override void _ExitTree()
	{
		LetGoOfEverything();
		base._ExitTree();
	}

	// --- the five hooks -----------------------------------------------------------------------------------

	// His ring: his own light round himself, for a player who comes to his feet (to take an orb back, or to
	// fight him up close). A ring, because it says "get out", which is all it asks.
	protected override GroundSlamAttack BuildSlam() =>
		new(SlamIntervalSeconds, SlamTelegraphSeconds, SlamRadius, SlamDamage, ElementColors.GetRamp(Element.Darkness).Mid)
		{
			GlyphOverride = TelegraphGlyph.Burst,
		};

	protected override float[] BuildPhaseThresholds() => new[] { DarkHealthFraction, EnrageHealthFraction };

	protected override void OnPhaseEntered(int phase)
	{
		if (phase == 1)
		{
			// HIS LIGHT: the hall goes dark, his lines begin.
			linesOn = true;
			untilLines = 1.6f;
			if (veil == null || !IsInstanceValid(veil))
				veil = DarkVeil.Cast(GetParent(), this);
			// And the music's pulse drops out with the light (the Throne's set is written for it, C7).
			if (Node2DGame.Current != null)
				Node2DGame.Current.RunMusicPulseHeld = true;
			GD.Print($"ThroneDarkWizardBoss: his light - the hall goes dark at {BossHealthFraction:P0}");
			return;
		}
		// ALL HE TOOK: the wardens' tells take the lines' place, and he throws a little more often.
		linesOn = false;
		echoesOn = true;
		untilEcho = 1.2f;
		taking.IntervalSeconds = LastPhaseTakingIntervalSeconds;
		SetBaseModulate(new Color(0.9f, 0.82f, 1.0f));
		GD.Print($"ThroneDarkWizardBoss: all he took - echoing {string.Join(", ", EchoSources())}");
	}

	protected override bool WantsToAttack(Node2D target, float distanceToPlayer) =>
		!TakingWindingUp && distanceToPlayer <= SlamRadius * SlamEngageRadiusMultiplier;

	// Planted through his ring's wind-up and through the taking's tell (the hand is drawn from where he
	// stands); otherwise RangedEnemy's bands - back off, hold, close.
	protected override Vector2 AdjustSteering(Vector2 chaseDirection, float distanceToPlayer)
	{
		if ((Slam != null && Slam.IsWindingUp) || TakingWindingUp)
			return Vector2.Zero;
		if (distanceToPlayer < RetreatDistance)
			return -chaseDirection;
		return distanceToPlayer > StandoffDistance ? chaseDirection : Vector2.Zero;
	}

	/// <summary>Darkness, once his own light is out over the hall, lands a quarter harder (6.4).</summary>
	public override float IncomingSpellMultiplier(SpellData source)
	{
		if (CurrentPhase < 1 || source == null)
			return 1f;
		return source.GetElementWeights().ContainsKey(Element.Darkness) ? 1f + DarknessBonus : 1f;
	}

	// --- the clocks beside the Slam --------------------------------------------------------------------

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (IsDying || IsEntering)
			return;
		float dt = (float)delta;
		TickTaking(dt);
		TickLines(dt);
		TickEchoes(dt);
		if (TakingWindingUp)
			QueueRedraw();
	}

	private bool takingWasWinding;

	private void TickTaking(float dt)
	{
		if (takingDelay > 0f)
		{
			takingDelay -= dt;
			return;
		}
		Node2D target = TargetPlayer;
		bool valid = target != null && IsInstanceValid(target);
		// Never while his ring is winding up: one tell at a time over his head.
		switch (taking.Tick(dt, valid && !(Slam?.IsWindingUp ?? false)))
		{
			case AttackTelegraph.Beat.Started:
				AttacksBegun++;
				PlayAttackAnimation();
				SfxPlayer.AtPosition(SfxCatalog.SpellSummon, GlobalPosition, 0.03f);
				break;
			case AttackTelegraph.Beat.Resolved:
				ThrowHand(target);
				break;
		}
		if (takingWasWinding && !TakingWindingUp)
			QueueRedraw();
		takingWasWinding = TakingWindingUp;
	}

	private void ThrowHand(Node2D target)
	{
		Node arena = GetParent();
		if (arena == null || target == null || !IsInstanceValid(target))
			return;
		Vector2 from = HandPoint;
		DarkHand.Throw(arena, this, from, target.GlobalPosition - from, HandSpeed, HandTurnDegreesPerSecond, HandLifeSeconds, HandRadius);
		HandsThrown++;
		SfxPlayer.Cast(Element.Darkness, from);
	}

	/// <summary>
	/// A hand reached the wizard (DarkHand): one of his firing spells, at random, is sealed until its orb is
	/// touched, and the orb goes out toward this hand. Nothing to take - every spell sealed or silent - and
	/// the hand closes on nothing.
	/// </summary>
	internal void OnHandLanded(Player wizard, Vector2 at)
	{
		if (IsDying || wizard == null || wizard.IsDead)
			return;
		HandsLanded++;
		sealable.Clear();
		foreach (SpellData spell in wizard.GetEquippedSpells())
			if (wizard.CanSeal(spell))
				sealable.Add(spell);
		if (sealable.Count == 0)
		{
			SfxPlayer.AtPosition(SfxCatalog.PlayerDodge, at, 0.05f);
			return;
		}
		SpellData taken = sealable[rng.RandiRange(0, sealable.Count - 1)];
		if (wizard.SealSpell(taken, float.PositiveInfinity) == null)
			return;
		SfxPlayer.AtPosition(SfxCatalog.PlayerShieldBreak, at, 0.04f);
		SealOrb orb = SealOrb.Send(GetParent(), this, wizard, taken, OrbPopDistance, OrbDriftSpeed, SealSeconds);
		if (orb != null)
		{
			orbs.RemoveAll(o => !IsInstanceValid(o));
			orbs.Add(orb);
		}
		GD.Print($"ThroneDarkWizardBoss: took {taken.Id} (phase {CurrentPhase}, {wizard.SealedCount} sealed)");
	}

	/// <summary>Tools only (_UiShot --dark-wizard=..,hand): a hand thrown now, with no tell.</summary>
	internal void DebugThrowHand() => ThrowHand(TargetPlayer);

	/// <summary>Tools only (_UiShot --dark-wizard=..,take): the next taking's tell begins on the next frame.</summary>
	internal void DebugBeginTaking()
	{
		takingDelay = 0f;
		taking.Prime();
	}

	internal void OnSealTakenBack(SpellData spell) => SealsTakenBack++;

	internal void OnSealReachedHim(SpellData spell) => SealsReachedHim++;

	// HIS LIGHT: a volley every LineIntervalSeconds, alternating two shapes the player reads at a glance -
	// a cross through where he stands (step out on a diagonal), and three parallel lanes with him in the
	// middle one and room between (step into a gap). Every lane crosses the hall.
	private void TickLines(float dt)
	{
		if (!linesOn)
			return;
		untilLines -= dt;
		if (untilLines > 0f)
			return;
		untilLines = LineIntervalSeconds;
		Node2D target = TargetPlayer;
		Node arena = GetParent();
		if (target == null || !IsInstanceValid(target) || arena == null)
			return;

		Color dark = ElementColors.GetRamp(Element.Darkness).Mid;
		int damage = ScaleOutgoingDamage(LineDamage);
		Vector2 at = target.GlobalPosition;
		float angle = rng.RandfRange(0f, Mathf.Pi);
		Vector2 along = Vector2.Right.Rotated(angle);
		if (lineVolley % 2 == 0)
		{
			PlaceLane(arena, at, along, 0f, LineTellSeconds, damage, dark);
			PlaceLane(arena, at, along.Rotated(Mathf.Pi * 0.5f), 0f, LineTellSeconds + LineStaggerSeconds, damage, dark);
		}
		else
		{
			float spacing = LineHalfWidth * 2f + LineGap;
			PlaceLane(arena, at, along, 0f, LineTellSeconds, damage, dark);
			PlaceLane(arena, at, along, spacing, LineTellSeconds + LineStaggerSeconds, damage, dark);
			PlaceLane(arena, at, along, -spacing, LineTellSeconds + LineStaggerSeconds, damage, dark);
		}
		lineVolley++;
		LinesCast++;
		SfxPlayer.AtPosition(SfxCatalog.WaveWarning, GlobalPosition, 0.03f);
	}

	// A lane across the hall: centred on <paramref name="through"/> moved <paramref name="offset"/> sideways,
	// running LineLength along <paramref name="along"/>.
	private void PlaceLane(Node arena, Vector2 through, Vector2 along, float offset, float tell, int damage, Color colour)
	{
		Vector2 side = new(-along.Y, along.X);
		Vector2 start = through + side * offset - along * LineLength * 0.5f;
		TelegraphedGroundHit.Place(arena, start, tell, LineLength, damage, colour, TelegraphShape.Line, along,
			TelegraphGlyph.Slam, halfWidth: LineHalfWidth);
	}

	// ALL HE TOOK: one warden's tell every EchoIntervalSeconds, round the player, in the order they fell.
	private void TickEchoes(float dt)
	{
		if (!echoesOn)
			return;
		untilEcho -= dt;
		if (untilEcho > 0f)
			return;
		untilEcho = EchoIntervalSeconds;
		// Not on top of a taking's tell: the hand is the fight's verb and is never crowded out.
		if (TakingWindingUp)
		{
			untilEcho = 0.6f;
			return;
		}
		Node2D target = TargetPlayer;
		Node arena = GetParent();
		if (target == null || !IsInstanceValid(target) || arena == null)
			return;
		IReadOnlyList<string> sources = EchoSources();
		string warden = sources[echoIndex % sources.Count];
		echoIndex++;
		CastEcho(arena, warden, target.GlobalPosition);
		EchoesCast++;
		EchoLog.Add(warden);
		PlayAttackAnimation();
		SfxPlayer.AtPosition(SfxCatalog.WaveWarning, GlobalPosition, 0.03f);
	}

	/// <summary>The wardens the procession felled this run, or every one he can echo if none was walked.</summary>
	public IReadOnlyList<string> EchoSources()
	{
		var felled = new List<string>();
		if (Node2DGame.Current?.Rules is ThroneRules throne)
		{
			foreach (string id in throne.FelledWardenIds)
				if (WardenColours.ContainsKey(id))
					felled.Add(id);
		}
		return felled.Count > 0 ? felled : EchoableWardens;
	}

	private void CastEcho(Node arena, string warden, Vector2 at)
	{
		Color colour = WardenColours.TryGetValue(warden, out Color c) ? c : ElementColors.GetRamp(Element.Darkness).Mid;
		int damage = ScaleOutgoingDamage(EchoDamage);
		float tell = EchoTellSeconds;
		Vector2 side = Vector2.Right.Rotated(rng.RandfRange(0f, Mathf.Tau));
		switch (warden)
		{
			case "fields_hayward":
				// His shove: a wide wedge from beside the player, across him.
				TelegraphedGroundHit.Place(arena, at - side * 150f, tell, 340f, damage, colour, TelegraphShape.Cone, side,
					halfAngleDegrees: 50f);
				break;
			case "castle_warden":
				// The Gaoler's lane: a charge's width, from far off, through where the player stands.
				TelegraphedGroundHit.Place(arena, at - side * 520f, tell, 1040f, damage, colour, TelegraphShape.Line, side,
					halfWidth: 34f);
				break;
			case "swamp_mother":
				// Her pools: a ring of them round the player and one under him, opening in turn.
				TelegraphedGroundHit.Place(arena, at, tell, 64f, damage, colour);
				for (int i = 0; i < 6; i++)
				{
					Vector2 p = at + side.Rotated(Mathf.Tau * i / 6f) * 150f;
					TelegraphedGroundHit.Place(arena, p, tell + 0.12f * (i + 1), 60f, damage, colour);
				}
				break;
			case "ruins_sentinel":
				// The Archivist's sweep: its beam, in three steps across the player.
				for (int i = -1; i <= 1; i++)
				{
					Vector2 from = at - side * 260f;
					TelegraphedGroundHit.Place(arena, from, tell + 0.3f * (i + 1), 520f, damage, colour, TelegraphShape.Cone,
						side.Rotated(Mathf.DegToRad(28f * i)), halfAngleDegrees: 14f);
				}
				break;
			case "frost_warden":
				// The Still Warden's ceiling: ice falling round the player, one shard on him.
				TelegraphedGroundHit.Place(arena, at, tell, 46f, damage, colour);
				for (int i = 0; i < 9; i++)
				{
					Vector2 p = at + Vector2.Right.Rotated(rng.RandfRange(0f, Mathf.Tau)) * rng.RandfRange(70f, 230f);
					TelegraphedGroundHit.Place(arena, p, tell + 0.08f * (i + 1), 46f, damage, colour);
				}
				break;
			case "sands_coil":
				// The Long Coil: it surfaces under the player and throws its sand out along a line.
				TelegraphedGroundHit.Place(arena, at, tell, 96f, damage, colour);
				for (int i = 1; i <= 4; i++)
					TelegraphedGroundHit.Place(arena, at + side * (96f + 70f * i), tell + 0.14f * i, 54f, damage, colour);
				break;
		}
	}

	// --- what RegressionChecks.ValidateBossPattern holds him to --------------------------------------------

	/// <summary>
	/// Each number that would quietly break the fight's promise, as a warning: the taking's tell long enough
	/// to see, the hand and the orb slower than the wizard walks (so a hand can always be left behind and an
	/// orb always caught), a seal that ends, his lines and the wardens' tells marked before they land, room to
	/// stand between his lanes, the dark before the echoes, and the dark's shader present.
	/// </summary>
	public IEnumerable<string> TuningProblems()
	{
		const float wizardWalk = 220f; // Player.Speed's default: the slowest a wizard walks before boons
		if (TakingTellSeconds < 0.8f)
			yield return $"The dark wizard's taking is marked for {TakingTellSeconds}s; under 0.8 s the long tell 6.4 asks for is not there.";
		if (TakingIntervalSeconds <= TakingTellSeconds || LastPhaseTakingIntervalSeconds <= TakingTellSeconds)
			yield return $"The dark wizard throws every {TakingIntervalSeconds}s / {LastPhaseTakingIntervalSeconds}s with a {TakingTellSeconds}s tell; the tell would never end.";
		if (HandSpeed <= 0f || HandSpeed >= wizardWalk)
			yield return $"The dark wizard's hand flies at {HandSpeed} px/s; at or above the wizard's {wizardWalk} it cannot be left behind.";
		if (HandTurnDegreesPerSecond <= 0f || HandLifeSeconds <= 0f || HandRadius <= 0f)
			yield return "The dark wizard's hand never turns, never lives or reaches nothing, so the taking never lands.";
		if (OrbDriftSpeed <= 0f || OrbDriftSpeed >= wizardWalk * 0.6f)
			yield return $"The seal drifts home at {OrbDriftSpeed} px/s; outside 0-{wizardWalk * 0.6f:0} it is either never home or never caught.";
		if (SealSeconds <= 0f)
			yield return "A seal that reaches him never lifts; the spell would be lost for the run.";
		if (LineTellSeconds <= 0f || EchoTellSeconds <= 0f)
			yield return $"The dark wizard's lines ({LineTellSeconds}s) or echoes ({EchoTellSeconds}s) land with no mark.";
		if (LineGap < 40f)
			yield return $"The dark wizard's parallel lanes leave {LineGap} px between them; under 40 the wizard cannot stand in the gap.";
		if (DarkHealthFraction <= EnrageHealthFraction)
			yield return $"The dark comes at {DarkHealthFraction} and the echoes at {EnrageHealthFraction}; the dark must come first.";
		if (!ResourceLoader.Exists("res://scenes/shaders/dark_veil.gdshader"))
			yield return "The dark's shader (scenes/shaders/dark_veil.gdshader) is missing, so the hall never goes dark.";
	}

	// --- the tells he wears -------------------------------------------------------------------------------

	// Over his head, the taking's wind-up (the alert: a special, not a strike) wins over his ring's.
	protected override bool ReadAttackTell(out TelegraphGlyph glyph, out float progress)
	{
		if (TakingWindingUp)
		{
			glyph = TelegraphGlyph.Alert;
			progress = taking.WindUpProgress;
			return true;
		}
		return base.ReadAttackTell(out glyph, out progress);
	}

	// The taking's tell on his body: his light gathering into his raised hand, a ring of it closing on the
	// palm and the palm going dark as it fills. On the 2 px grid, in the boss's own space (Foremost).
	public override void _Draw()
	{
		if (!TakingWindingUp || IsDying || IsEntering)
			return;
		ElementColors.Ramp dark = ElementColors.GetRamp(Element.Darkness);
		float u = taking.WindUpProgress;
		Vector2 palm = ToLocal(HandPoint);
		float r = Mathf.Lerp(46f, 10f, u);
		BossDeath.Ring(this, palm, r, r, BossDeath.Px * 2f, new Color(dark.Mid, 0.35f + 0.6f * u));
		BossDeath.Ring(this, palm, r * 0.6f + 6f, r * 0.6f + 6f, BossDeath.Px, new Color(dark.Hot, 0.5f * u));
		BossDeath.Disc(this, palm, 6f + 8f * u, 6f + 8f * u, new Color(0.06f, 0.02f, 0.10f, 0.4f + 0.55f * u));
		for (int k = 0; k < 6; k++)
		{
			float a = Mathf.Tau * k / 6f + u * 3f;
			Vector2 mote = palm + new Vector2(MathF.Cos(a), MathF.Sin(a)) * (r + 4f);
			DrawRect(WhipLimb.Cell(BossDeath.Snap(mote), BossDeath.Px * 2f), new Color(dark.Hot, 0.4f + 0.6f * u));
		}
	}

	// --- his fall lets go of everything ----------------------------------------------------------------------

	private void LetGoOfEverything()
	{
		if (veil != null && IsInstanceValid(veil))
			veil.Lift();
		if (GetTree()?.GetFirstNodeInGroup("player") is Player wizard && IsInstanceValid(wizard))
			wizard.ReleaseAllSeals();
		if (Node2DGame.Current != null && IsInstanceValid(Node2DGame.Current))
			Node2DGame.Current.RunMusicPulseHeld = false;
		linesOn = false;
		echoesOn = false;
	}

	// --- the arrival: out of his own shadow ------------------------------------------------------------------

	protected internal override float EntranceSeconds => 3.6f;
	protected internal override float EntranceRoarSeconds => LightCatches;
	protected internal override float EntranceClimaxWeight => 1.2f;
	protected internal override Element EntranceFlareElement => Element.Darkness;

	private BossDeath.Piece risen;
	private const float PoolOpens = 0.9f;
	private const float RiseStart = 0.7f;
	private const float RiseEnd = 2.4f;
	private const float EclipseOpens = 2.3f;
	private const float LightCatches = 2.9f;

	protected internal override void OnEntranceBegin(BossDeath stage)
	{
		risen = stage.CutWhole();
		risen.Visible = false;
	}

	protected internal override void TickEntrance(BossDeath stage, float t, float dt)
	{
		if (risen == null)
			return;
		// He rises out of the pool as the dark he is: occlusion first, his colours coming into him as he
		// stands, the last of the shadow running off his hem.
		float up = BossDeath.EaseOut(BossDeath.Span(t, RiseStart, RiseEnd));
		risen.Visible = t >= RiseStart;
		RiseFromGround(stage, risen, up);
		float colour = BossDeath.Span(t, RiseEnd - 0.6f, EclipseOpens + 0.3f);
		risen.Color = new Color(0.16f, 0.10f, 0.24f, 1f).Lerp(Colors.White, colour);
		risen.Offset = new Vector2(BossDeath.Shudder(t, 1f - BossDeath.Span(t, RiseStart, RiseEnd), 11f) * stage.ArtScale.X * 0.5f, 0f);
		ElementColors.Ramp dark = ElementColors.GetRamp(Element.Darkness);
		if (stage.Crossed(RiseStart))
			stage.Impact(0.3f);
		if (stage.Crossed(EclipseOpens))
		{
			stage.Impact(0.5f);
			stage.PuffAt(stage.ArtToLocal(new Vector2(ECLIPSE_X, ECLIPSE_Y)), 8f, 52f, 0.6f, new Color(dark.Mid, 0.7f), 0f, 0f, true);
		}
		if (stage.Crossed(LightCatches))
		{
			Vector2 orb = stage.ArtToLocal(new Vector2(19f, 7f));
			stage.PuffAt(orb, 4f, 30f, 0.45f, new Color(ElementColors.GetRamp(Element.Light).Hot, 0.9f), 0f, 0f, true);
		}
	}

	private const float ECLIPSE_X = 47f;
	private const float ECLIPSE_Y = 20f;

	// His shadow pools out on the floor in front of the throne before anything stands in it: a dark that
	// spreads with his ramp at its rim, tendrils running out of it, and goes back under his hem as he stands.
	protected internal override void DrawEntranceGround(BossDeath stage, CanvasItem canvas, float t)
	{
		ElementColors.Ramp dark = ElementColors.GetRamp(Element.Darkness);
		Vector2 foot = stage.Foot;
		float open = BossDeath.EaseOut(BossDeath.Span(t, 0f, PoolOpens));
		float settle = 1f - 0.45f * BossDeath.Span(t, RiseEnd, EntranceSeconds);
		float fade = 1f - BossDeath.Span(t, EntranceSeconds, EntranceSeconds + 1.1f);
		if (open <= 0f || fade <= 0f)
			return;
		float rx = 96f * open * settle, ry = 34f * open * settle;
		BossDeath.Disc(canvas, foot, rx + 4f, ry + 2f, new Color(dark.Edge, 0.8f * fade));
		BossDeath.Disc(canvas, foot, rx, ry, new Color(0.03f, 0.01f, 0.05f, 0.92f * fade));
		for (int k = 0; k < 7; k++)
		{
			float a = Mathf.Tau * k / 7f + (BossDeath.Hash(41u, k) - 0.5f) * 0.6f;
			float length = (rx + 30f + 50f * BossDeath.Hash(43u, k)) * open;
			var dir = new Vector2(MathF.Cos(a), 0.36f * MathF.Sin(a));
			Vector2 tip = foot + dir * length;
			BossDeath.Line(canvas, foot + dir * rx * 0.6f, tip, new Color(0.03f, 0.01f, 0.05f, 0.85f * fade), BossDeath.Px * 2f);
			canvas.DrawRect(WhipLimb.Cell(BossDeath.Snap(tip), BossDeath.Px * 2f), new Color(dark.Mid, 0.8f * fade));
		}
	}

	// The eclipse round his head opening: a ring of his dark light, growing from a point to its size, drawn
	// over him (normal blend, not additive: it is DARK light, and light added over the hall would read gold).
	protected internal override void DrawEntranceAir(BossDeath stage, CanvasItem canvas, float t)
	{
		float open = BossDeath.EaseOut(BossDeath.Span(t, EclipseOpens, EclipseOpens + 0.5f));
		float gone = BossDeath.Span(t, EclipseOpens + 0.5f, EclipseOpens + 0.9f);
		if (open <= 0f || gone >= 1f)
			return;
		ElementColors.Ramp dark = ElementColors.GetRamp(Element.Darkness);
		Vector2 centre = stage.ArtToLocal(new Vector2(ECLIPSE_X, ECLIPSE_Y));
		float r = 34f * open + 20f * gone;
		BossDeath.Ring(canvas, centre, r, r, BossDeath.Px * 3f, new Color(dark.Mid, 0.9f * (1f - gone)));
		BossDeath.Ring(canvas, centre, r - 4f, r - 4f, BossDeath.Px, new Color(dark.Hot, 0.9f * (1f - gone)));
	}

	// The light he holds catching in his hand at the climax: the one warm thing in his arrival.
	protected internal override void DrawEntranceLight(BossDeath stage, CanvasItem canvas, float t)
	{
		float on = BossDeath.Span(t, LightCatches - 0.1f, LightCatches + 0.1f) * (1f - BossDeath.Span(t, LightCatches + 0.3f, EntranceSeconds + 0.4f));
		if (on <= 0f)
			return;
		ElementColors.Ramp light = ElementColors.GetRamp(Element.Light);
		Vector2 orb = stage.ArtToLocal(new Vector2(19f, 7f));
		BossDeath.Disc(canvas, orb, 16f, 16f, new Color(light.Edge, 0.35f * on));
		BossDeath.Disc(canvas, orb, 8f, 8f, new Color(light.Hot, 0.7f * on));
	}

	protected internal override void OnEntranceEnd(BossDeath stage) => QueueRedraw();

	// --- the fall: the magic going home -----------------------------------------------------------------

	protected internal override float DeathSeconds => 3.3f;

	private BossDeath.Piece fallen;
	private const float StormStart = 0.25f;
	private const float SinkStart = 1.2f;
	private const float SinkEnd = 3.0f;

	protected internal override void OnDeathBegin(BossDeath death)
	{
		LetGoOfEverything();
		fallen = death.CutWhole();
		death.Impact(1.0f);
	}

	protected internal override void TickDeath(BossDeath death, float t, float dt)
	{
		if (fallen == null)
			return;
		// He shudders as it leaves him, goes to the dark he is, and sinks back into his shadow.
		float shake = 1f - BossDeath.Span(t, 0f, SinkStart);
		fallen.Offset = new Vector2(BossDeath.Shudder(t, shake, 19f) * death.ArtScale.X, 0f);
		float drained = BossDeath.Span(t, StormStart, SinkStart + 0.3f);
		fallen.Color = Colors.White.Lerp(new Color(0.16f, 0.10f, 0.24f, 1f), drained);
		RiseFromGround(death, fallen, 1f - BossDeath.EaseIn(BossDeath.Span(t, SinkStart, SinkEnd)));

		// THE STORM: the warm light he took leaves him in every direction, fast enough to be off the screen
		// before the fall ends - in waves, the first the biggest. Mostly the light's own ramp, with a scatter
		// of every element's: the wizards' magic, each its own colour, going home.
		if (death.Crossed(StormStart))
			Storm(death, 90, 1f);
		if (death.Crossed(StormStart + 0.35f))
			Storm(death, 60, 0.85f);
		if (death.Crossed(StormStart + 0.8f))
			Storm(death, 40, 0.7f);
		if (death.Crossed(SinkStart))
			death.Impact(0.5f);
	}

	private static readonly Element[] HomeGoing =
	{
		Element.Fire, Element.Ice, Element.Lightning, Element.Earth, Element.Water, Element.Grass, Element.Arcane,
		Element.Metal, Element.Poison, Element.Wind, Element.Light,
	};

	private void Storm(BossDeath death, int count, float speed)
	{
		ElementColors.Ramp light = ElementColors.GetRamp(Element.Light);
		int warm = count * 2 / 3;
		death.Emit(new BossDeath.Throw
		{
			Cell = -1, Size = 4f, Color = light.Core, ColorB = light.Edge, Light = true, NoLand = true,
			Angle = 0f, Spread = Mathf.Tau, Depth = 1f, SpeedMin = 420f * speed, SpeedMax = 980f * speed,
			UpMin = -60f, UpMax = 160f, Gravity = 0f, Drag = 0.15f, Life = 2.6f, Fade = 0.6f,
		}, warm, new Vector2(47f, 44f), new Vector2(14f, 26f), 0f, 0.002f);
		for (int i = warm; i < count; i++)
		{
			ElementColors.Ramp ramp = ElementColors.GetRamp(HomeGoing[i % HomeGoing.Length]);
			death.Emit(new BossDeath.Throw
			{
				Cell = -1, Size = 4f, Color = ramp.Hot, ColorB = ramp.Mid, Light = true, NoLand = true,
				Angle = 0f, Spread = Mathf.Tau, Depth = 1f, SpeedMin = 380f * speed, SpeedMax = 900f * speed,
				UpMin = -60f, UpMax = 160f, Gravity = 0f, Drag = 0.15f, Life = 2.6f, Fade = 0.6f,
			}, 1, new Vector2(47f, 44f), new Vector2(14f, 26f), 0f, 0f);
		}
	}

	// His own light going out: the eclipse flares once and closes to nothing, the eyes last. Drawn over him
	// in the normal blend, as the arrival drew it - dark light.
	protected internal override void DrawDeathAir(BossDeath death, CanvasItem canvas, float t)
	{
		ElementColors.Ramp dark = ElementColors.GetRamp(Element.Darkness);
		float flare = BossDeath.Span(t, 0f, 0.15f);
		float close = BossDeath.EaseIn(BossDeath.Span(t, 0.15f, 1.0f));
		if (close >= 1f)
			return;
		Vector2 centre = death.ArtToLocal(new Vector2(ECLIPSE_X, ECLIPSE_Y));
		float r = (34f + 14f * flare) * (1f - close);
		if (r >= BossDeath.Px * 2f)
			BossDeath.Ring(canvas, centre, r, r, BossDeath.Px * 3f, new Color(dark.Mid, 0.95f * (1f - close)));
		float eyes = 1f - BossDeath.Span(t, 0.6f, 1.1f);
		if (eyes > 0f)
		{
			canvas.DrawRect(new Rect2(death.ArtToLocal(new Vector2(45f, 22f)), new Vector2(BossDeath.Px * 2f, BossDeath.Px)), new Color(dark.Core, eyes));
			canvas.DrawRect(new Rect2(death.ArtToLocal(new Vector2(49f, 22f)), new Vector2(BossDeath.Px * 2f, BossDeath.Px)), new Color(dark.Core, eyes));
		}
	}

	// The light leaving: a bright core at his chest as the storm goes, and the orb in his hand going out
	// first - it was the first thing he took.
	protected internal override void DrawDeathLight(BossDeath death, CanvasItem canvas, float t)
	{
		ElementColors.Ramp light = ElementColors.GetRamp(Element.Light);
		float core = BossDeath.Span(t, 0.05f, StormStart) * (1f - BossDeath.Span(t, StormStart + 0.8f, StormStart + 1.6f));
		if (core > 0f)
		{
			Vector2 chest = death.ArtToLocal(new Vector2(47f, 44f));
			BossDeath.Disc(canvas, chest, 40f * core, 40f * core, new Color(light.Edge, 0.35f * core));
			BossDeath.Disc(canvas, chest, 18f * core, 18f * core, new Color(light.Hot, 0.8f * core));
		}
	}
}
