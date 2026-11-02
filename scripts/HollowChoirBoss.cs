using Godot;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

// Chapter 3, the Sunken Cave: THE HOLLOW CHOIR.
//
// Three bodies, one fight. Drop any one of them and it does not die - it goes quiet and slumps where it
// stood, and one of the other two walks over and sings it back up. The only way to end the Choir is to
// have all three silent at the same moment.
//
// WHY THIS FIGHT EXISTS. Every other boss in the game is answered by pointing the build at it,
// because every other boss is one health bar and the auto-fire finds it. This one cannot be, and
// the reason is structural rather than numerical: the spells in this game choose their own target,
// so a fight that requires the player to choose one is a fight the loadout cannot play for them.
// Focus everything down in turn and the first one you dropped is back before the third falls.
// The answer is spread damage, a burst big enough to close all three inside the revive window - or
// stopping the one that has gone to sing (2026-11-01, below).
//
// LINKED HEALTH is the primitive, and it is deliberately not a framework. There is no manager node
// and no shared pool: each voice holds a list of its siblings, and the check "is everyone else
// already down" is three pointer comparisons on the frame one of them falls. A boss with a
// coordinator would be a second place for the fight to be wrong.
//
// THE PLAYTEST (Eric, 0.10.0, 2026-11-01), four things, all built here:
//   - "pathing can get stuck": a voice walked straight at its standing place and pressed on the cave's
//     rock 37% of the time (_BossProbe "held"). It walks there along the cave now (WalkToward), and a
//     place inside the rock is moved to open ground (OpenGroundNear).
//   - "Instead of a circle and arrow, show some sort of attack that they launch": the round's notes are
//     sung - the voice's mouths light from the hem up, its cowl flares - and fly at him (ChoirNote).
//   - "If one of the hollow choir gets too far from the player, they should dip into the ground and
//     reappear": a voice far from where it means to be, or stuck, sinks into a pool of black water and
//     comes up at its place (the dip, BurrowMove and SetTargetable).
//   - "The time between them respawning ... should be larger and they should visibly walk over to their
//     falling comrade and revive each other ... CC like freezing them should pause the revival": a
//     fallen voice lies where it fell, the nearest living voice walks to it and sings it back for
//     ReviveSeconds, and the song runs only while that voice is in reach and not held by crowd control
//     (Enemy.IsHeldByCrowdControl) - which, on a boss, is never for more than 0.75 s at a stretch.
public partial class HollowChoirBoss : BossEnemy
{
	/// <summary>
	/// False on the voice the run spawned and tracks, true on the two it calls up. Only the
	/// original emits the victory the run listens for.
	/// </summary>
	[Export] public bool IsEcho { get; set; }

	/// <summary>How many voices in total, including this one. Three is the fight.</summary>
	[Export] public int VoiceCount { get; set; } = 3;

	/// <summary>How far apart they start. Far enough that no single blast covers all three.</summary>
	[Export] public float ChoirSpreadRadius { get; set; } = 210f;

	/// <summary>
	/// Seconds a living voice must sing at a fallen one's side to raise it. Its walk there comes first, and
	/// the song pauses while the singer is held, so a voice is down for this and more.
	/// </summary>
	[Export] public float ReviveSeconds { get; set; } = 7.0f;

	/// <summary>How close to a fallen voice the singer must stand for the song to reach it.</summary>
	[Export] public float ReviveReach { get; set; } = 96f;

	/// <summary>How much health a revived voice comes back with. Well under half, or it never ends.</summary>
	[Export] public float ReviveHealthFraction { get; set; } = 0.35f;

	[Export] public Color ChoirColor { get; set; } = new Color(0.45f, 0.86f, 0.92f);

	// THE VOICES KEEP THEIR PLACES (2026-10-31). The Choir died in six seconds to a farmed build because
	// all three chased the wizard and arrived on top of each other, where one blast caught the whole
	// set and the linked health never came into it. Now each voice holds its own bearing round him at
	// StandRadius - the three stand a triangle apart - and only steps in to sing. Spreading damage is
	// the fight's question again, rather than something a crowd answers by standing still.
	[Export] public float StandRadius { get; set; } = 230f;

	/// <summary>Degrees a second the voices' places turn round the wizard from phase 1 on.</summary>
	[Export] public float RoundDegreesPerSecond { get; set; } = 16f;

	// PHASES, on the whole Choir's health (BossHealthFraction): at RoundHealthFraction the voices begin
	// to turn round him and to sing a ROUND - each in turn sings a note at where he stands, a beat apart,
	// so the three come as a sequence to walk through; at LastVerseHealthFraction the round quickens and a
	// fallen voice is sung back sooner and stronger, so the window to silence all three narrows just as
	// the fight is nearly won.
	[Export] public float RoundHealthFraction { get; set; } = 0.7f;
	[Export] public float LastVerseHealthFraction { get; set; } = 0.33f;
	[Export] public float NoteIntervalSeconds { get; set; } = 4.8f;
	/// <summary>The gesture before a note leaves: the mouths lighting from the hem up (the `sing` strip).</summary>
	[Export] public float NoteTellSeconds { get; set; } = 0.55f;
	[Export] public float NoteRadius { get; set; } = 26f;
	[Export] public float NoteSpeed { get; set; } = 230f;
	[Export] public int NoteDamage { get; set; } = 4;

	// THE DIP. Further than this from where it means to be - its place, or a fallen voice's side - or held
	// where it stands two windows running, a voice goes under and comes up there.
	[Export] public float DipDistance { get; set; } = 560f;
	[Export] public float DipTravelSeconds { get; set; } = 1.2f;
	private const float DipSinkSeconds = 0.5f;
	private const float DipCooldownSeconds = 4f;

	private readonly List<HollowChoirBoss> voices = new();
	// This voice's place in the Choir: 0 the one the run spawned, then the echoes in the order called.
	private int voiceOrder;
	private bool singingRound;
	private float untilNote;

	// Down: it fell and has not been sung back. Rising: the song is done and it is getting up (still down,
	// still out of reach, for RiseSeconds). Its song so far, and who is singing it.
	private bool downed;
	private float downedFor;
	private float riseClock = -1f;
	private float reviveProgress;
	private HollowChoirBoss reviver;
	// On a living voice: the fallen one it has gone to sing back.
	private HollowChoirBoss reviving;
	private const float FallSeconds = 0.6f;
	private const float RiseSeconds = 0.6f;

	private bool IsDowned => downed;

	/// <summary>True while this voice is down - fallen, or getting up - and cannot be struck.</summary>
	public bool IsFallen => downed;

	/// <summary>How far through being sung back this voice is, 0 to 1. For _BossProbe and _UiShot.</summary>
	public float ReviveProgress => downed ? Mathf.Clamp(reviveProgress / Mathf.Max(0.01f, ReviveSeconds), 0f, 1f) : 0f;

	/// <summary>True while this voice stands at a fallen one's side singing it back.</summary>
	public bool IsSingingBack => reviving != null && IsInstanceValid(reviving) && InReachOf(reviving);

	/// <summary>True from the moment it sinks until it is back up. For _BossProbe and _UiShot.</summary>
	public bool IsDipping => dipStage != DipStage.None;

	/// <summary>How many times this voice has dipped. For _BossProbe.</summary>
	public int DipsTaken { get; private set; }

	public override void _Ready()
	{
		base._Ready();
		voices.Add(this);

		if (IsEcho)
			return;

		CallDeferred(nameof(SummonTheRest));
	}

	// Where an echo stands: on the ring round the first voice, at its own bearing - or, on walled ground,
	// the nearest bearing and radius on that ring that something could walk out of. The Sunken Cave is
	// caverns now (.ai/endgame-castle.md 7.3), and an echo set down on the bare ring landed inside a rock
	// mass in _BossProbe and stood there the whole fight, out of reach of the player's feet. The first
	// voice stands on ground Node2DGame.FindBossSpawnPosition vetted, so its own spot is the last resort.
	private Vector2 EchoPosition(float angle)
	{
		MazeNavigation nav = MazeNavigation.Active;
		foreach (float radius in new[] { ChoirSpreadRadius, ChoirSpreadRadius * 0.7f, ChoirSpreadRadius * 0.4f })
		{
			for (int step = 0; step < 12; step++)
			{
				// 0, then 15 degrees either way, then 30, out to 90.
				float turn = (step + 1) / 2 * (Mathf.Tau / 24f) * (step % 2 == 0 ? 1f : -1f);
				Vector2 at = GlobalPosition + Vector2.FromAngle(angle + turn) * radius;
				if (nav == null || nav.IsReachable(at))
					return at;
			}
		}
		return GlobalPosition;
	}

	// Deferred because _Ready runs while this node is being added, and adding siblings to the same
	// parent mid-add is the kind of thing Godot tolerates until one day it does not.
	private void SummonTheRest()
	{
		Node arena = GetParent();
		if (arena == null || !IsInstanceValid(arena) || string.IsNullOrEmpty(SceneFilePath))
			return;

		var scene = ResourceLoader.Load<PackedScene>(SceneFilePath);
		if (scene == null)
		{
			GD.PushError($"HollowChoirBoss: could not reload its own scene '{SceneFilePath}', so it fights alone.");
			return;
		}

		int wanted = Mathf.Max(1, VoiceCount) - 1;
		for (int i = 0; i < wanted; i++)
		{
			if (scene.Instantiate() is not HollowChoirBoss echo)
			{
				GD.PushError("HollowChoirBoss: its own scene did not instantiate as a HollowChoirBoss.");
				return;
			}

			echo.IsEcho = true;
			echo.voiceOrder = i + 1;
			echo.Health = Health;
			echo.BossId = BossId;
			echo.BossDisplayName = BossDisplayName;
			voices.Add(echo);
			arena.AddChild(echo);

			float angle = Mathf.Tau * (i + 1) / Mathf.Max(1, VoiceCount);
			echo.GlobalPosition = EchoPosition(angle);
		}

		// Everyone learns everyone. Done here rather than in each echo's _Ready because only this
		// node ever knows the whole set.
		//
		// THE SNAPSHOT IS LOAD-BEARING. Iterating `voices` directly while assigning into
		// `voice.voices` hits the case where voice IS this, so the Clear below is a modification
		// of the collection being enumerated - it threw InvalidOperationException on the frame
		// the fight started, abandoned the loop half done, and left the three voices with no
		// knowledge of each other. The fight then ran as three unrelated bosses that each died on
		// their own, which looks exactly like the linked health simply not being implemented.
		HollowChoirBoss[] everyone = voices.ToArray();
		foreach (HollowChoirBoss voice in everyone)
		{
			voice.voices.Clear();
			voice.voices.AddRange(everyone);
		}
	}

	protected override GroundSlamAttack BuildSlam() =>
		new(SlamIntervalSeconds, SlamTelegraphSeconds, SlamRadius, SlamDamage, ChoirColor);

	// No enrage - three bodies that all sped up at once would be a cliff nobody could read - but two
	// verses, read off the whole Choir's bar so the three change on one frame (BossEnemy.TickPhases).
	protected override float[] BuildPhaseThresholds() => new[] { RoundHealthFraction, LastVerseHealthFraction };

	protected override void OnPhaseEntered(int phase)
	{
		if (phase == 1)
		{
			// The round: each voice's first note a third of the interval after the one before it.
			singingRound = true;
			untilNote = 1.2f + voiceOrder * NoteIntervalSeconds / Mathf.Max(1, VoiceCount);
			SetBaseModulate(new Color(0.62f, 0.96f, 1.0f));
			return;
		}
		NoteIntervalSeconds *= 0.72f;
		RoundDegreesPerSecond *= 1.6f;
		ReviveHealthFraction = Mathf.Min(0.6f, ReviveHealthFraction + 0.12f);
		// Sung back sooner: the last verse's window to silence all three is shorter as well as stronger.
		ReviveSeconds *= 0.75f;
		SetBaseModulate(new Color(0.80f, 1.0f, 1.0f));
	}

	// --- Where it stands, and how it gets there ------------------------------------------------------

	// Where this voice stands: its third of the circle round the wizard, turned by the round.
	private float firstBearing = float.NaN;
	private float roundTurn;
	private bool steppingIn;
	private float stepInLeft;
	private float untilStepIn = -1f;
	private const float StepInIntervalSeconds = 6.6f;
	private const float StepInHoldSeconds = 3.2f;

	// The place, held as an offset from the wizard and re-chosen on a stagger (it costs a few dozen cell
	// reads to move off rock); and whether the straight walk to where it is going crosses rock, likewise.
	private Vector2 placeOffset;
	private bool hasPlaceOffset;
	private float routeClock;
	private bool routeBlocked;
	private const float RouteRefreshSeconds = 0.2f;

	private Vector2? StandingPlace(Node2D target)
	{
		if (target == null || !IsInstanceValid(target))
			return null;
		if (!hasPlaceOffset)
			ChoosePlace(target);
		return target.GlobalPosition + placeOffset;
	}

	private void ChoosePlace(Node2D target)
	{
		if (float.IsNaN(firstBearing))
		{
			HollowChoirBoss first = voices.Count > 0 && IsInstanceValid(voices[0]) ? voices[0] : this;
			firstBearing = (first.GlobalPosition - target.GlobalPosition).Angle();
		}
		float bearing = firstBearing + voiceOrder * Mathf.Tau / Mathf.Max(1, VoiceCount) + roundTurn;
		float radius = steppingIn ? SlamRadius * 0.55f : StandRadius;
		placeOffset = OpenGroundNear(target.GlobalPosition, bearing, radius) - target.GlobalPosition;
		hasPlaceOffset = true;
	}

	// A place inside the cave's rock is no place to stand. In order: the place itself; the same distance
	// up to 45 degrees either side, so the voice keeps its own third of the circle and the three stay
	// apart; nearer along its own bearing, as far out as open ground runs, if that is still out of its
	// own reach of him; and only then the nearest open bearing anywhere, at that distance or nearer (the
	// search EchoPosition and the Archivist's surfacing make). The first version searched the whole
	// circle at once, and in a corner of the cave it sent two voices to the same patch of open floor -
	// one blast for both, the thing the places exist to prevent.
	private Vector2 OpenGroundNear(Vector2 around, float bearing, float radius)
	{
		Vector2 straight = around + Vector2.FromAngle(bearing) * radius;
		MazeNavigation nav = MazeNavigation.Active;
		if (nav == null || nav.IsReachable(straight))
			return straight;
		for (int step = 1; step <= 6; step++)
		{
			float turn = (step + 1) / 2 * (Mathf.Tau / 24f) * (step % 2 == 0 ? 1f : -1f);
			Vector2 at = around + Vector2.FromAngle(bearing + turn) * radius;
			if (nav.IsReachable(at))
				return at;
		}
		Vector2 along = Vector2.FromAngle(bearing);
		float open = 0f;
		for (float r = 24f; r < radius; r += 24f)
		{
			if (!nav.IsReachable(around + along * r))
				break;
			open = r;
		}
		if (open >= Mathf.Min(radius, SlamRadius))
			return around + along * open;
		for (int ring = 0; ring < 3; ring++)
		{
			float r = radius * (1f - 0.25f * ring);
			for (int step = 7; step < 24; step++)
			{
				float turn = (step + 1) / 2 * (Mathf.Tau / 24f) * (step % 2 == 0 ? 1f : -1f);
				Vector2 at = around + Vector2.FromAngle(bearing + turn) * r;
				if (nav.IsReachable(at))
					return at;
			}
		}
		return straight;
	}

	// Where it is going right now: a fallen voice's side while it goes to sing one back, else its place.
	private Vector2? Goal()
	{
		if (reviving != null && IsInstanceValid(reviving))
			return SideOf(reviving);
		return StandingPlace(TargetPlayer);
	}

	// Where to stand to sing a fallen voice back: within reach of it, on the side it is coming from.
	private Vector2 SideOf(HollowChoirBoss fallen)
	{
		Vector2 from = GlobalPosition - fallen.GlobalPosition;
		float bearing = from.LengthSquared() > 1f ? from.Angle() : voiceOrder * 2.1f;
		// Far enough off the heap that the song is seen crossing to it, not sung into its own hem.
		return OpenGroundNear(fallen.GlobalPosition, bearing, ReviveReach * 0.85f);
	}

	private bool InReachOf(HollowChoirBoss fallen) =>
		GlobalPosition.DistanceSquaredTo(fallen.GlobalPosition) <= ReviveReach * ReviveReach;

	// THE WALK, ALONG THE CAVE (2026-11-01). It used to walk straight at its place and ignore the chase
	// direction, so neither the flow field nor the skirt ever reached it - it pressed on rock 37% of the
	// time. Now, while the straight line to where it is going crosses rock (MazeNavigation.HasClearWalk),
	// it follows the field round it as the chase does (the field leads to the wizard, and its place and
	// its fallen are round him); while Enemy's skirt has it turning along something the grid cannot see,
	// it takes the chase's heading, which carries that turn; otherwise it walks straight there, easing in
	// over the last forty pixels rather than overshooting and hunting round the spot.
	private Vector2 WalkToward(Vector2 goal, Vector2 chaseDirection)
	{
		Vector2 to = goal - GlobalPosition;
		float d = to.Length();
		if (d < 10f)
			return Vector2.Zero;
		float pace = Mathf.Min(1.5f, 0.25f + d / 50f);
		if (IsSkirting && chaseDirection != Vector2.Zero)
			return chaseDirection * pace;
		if (routeBlocked)
		{
			Vector2 flow = MazeNavigation.Routing?.FlowDirectionAt(GlobalPosition) ?? Vector2.Zero;
			Vector2 round = flow != Vector2.Zero ? flow : chaseDirection;
			if (round != Vector2.Zero)
				return round * pace;
		}
		return to / d * pace;
	}

	private void TickRoute(float delta)
	{
		routeClock -= delta;
		if (routeClock > 0f)
			return;
		// Staggered by voice, so the three do not all ask on one frame.
		routeClock = RouteRefreshSeconds + voiceOrder * 0.013f;
		Node2D target = TargetPlayer;
		if (target != null && IsInstanceValid(target))
			ChoosePlace(target);
		MazeNavigation nav = MazeNavigation.Routing;
		routeBlocked = nav != null && Goal() is Vector2 goal && !nav.HasClearWalk(GlobalPosition, goal);
	}

	// The voices take turns to step in and sing at him, a third of the interval apart, and go back to
	// their places when the note has landed.
	private void TickPlaces(float delta)
	{
		if (IsDowned || IsDying)
			return;
		if (untilStepIn < 0f)
			untilStepIn = 2.0f + voiceOrder * StepInIntervalSeconds / Mathf.Max(1, VoiceCount);
		if (singingRound)
			roundTurn += Mathf.DegToRad(RoundDegreesPerSecond) * delta;
		if (steppingIn)
		{
			// Its time to step in runs at its own pace: a voice slowed, held or dragged by crowd control
			// gets the longer to arrive, rather than turning back before it can sing - under all of it at
			// once (_BossProbe --cc=all) it otherwise sang once in 45 s.
			stepInLeft -= delta * Mathf.Max(0.25f, PaceShare);
			if (stepInLeft <= 0f)
				steppingIn = false;
		}
		else if (reviving == null && !IsDipping)
		{
			untilStepIn -= delta;
			if (untilStepIn <= 0f)
			{
				steppingIn = true;
				stepInLeft = StepInHoldSeconds;
				untilStepIn = StepInIntervalSeconds;
			}
		}
	}

	// --- The round: a note, sung and sent ------------------------------------------------------------

	// Seconds left of the gesture before a note leaves, or negative when not singing one.
	private float noteGestureLeft = -1f;

	private void TickRound(float delta)
	{
		if (!singingRound || IsDowned || IsDying || IsDipping || reviving != null)
		{
			noteGestureLeft = -1f;
			return;
		}
		if (noteGestureLeft >= 0f)
		{
			noteGestureLeft -= delta;
			if (noteGestureLeft < 0f)
				LaunchNote();
			return;
		}
		untilNote -= delta;
		if (untilNote > 0f)
			return;
		untilNote = NoteIntervalSeconds;
		if (TargetPlayer == null || !IsInstanceValid(TargetPlayer))
			return;
		// The gesture: planted (AdjustSteering), its mouths lighting from the hem up (the sing strip).
		noteGestureLeft = Mathf.Max(0.1f, NoteTellSeconds);
	}

	// The note leaves the mouths for where he stands now: read, and stepped out of, and the next voice's
	// comes a beat later. From the column of mouths, not the cowl: the cowl stands 44 px above the body's
	// place on the floor, which against a cave wall is inside the rock, and a note born in a wall cell is
	// stopped there on its first frame (EnemyProjectile) - the first photographs had no note at all.
	private void LaunchNote()
	{
		Node2D target = TargetPlayer;
		Node arena = GetParent();
		if (target == null || !IsInstanceValid(target) || arena == null || !IsInstanceValid(arena))
			return;
		Vector2 from = GlobalPosition + MouthsOffset;
		Vector2 at = target.GlobalPosition;
		float reach = from.DistanceTo(at) + 160f;
		var note = ChoirNote.Make(NoteRadius, NoteSpeed, ScaleOutgoingDamage(NoteDamage),
			Mathf.Clamp(reach / Mathf.Max(1f, NoteSpeed), 0.6f, 3.5f), ChoirColor);
		arena.AddChild(note);
		note.Launch(from, at);
		AttacksBegun++;
		SfxPlayer.AtPosition(SfxCatalog.EnemyShoot, from);
	}

	/// <summary>The middle of the column of mouths, from the body's origin: where a note and the song leave.</summary>
	private Vector2 MouthsOffset => ArtToLocal(new Vector2(48f, 58f));

	// A point on the 96-cell sheet (sprite_bosses.py) in this body's own space: the sprite is centred on
	// the origin and scaled up (x1.7 in the scene), so the feet are well below the origin.
	private Vector2 ArtToLocal(Vector2 art)
	{
		AnimatedSprite2D body = BodySprite;
		Vector2 scale = body != null ? body.Scale : Vector2.One;
		return BodySpriteRestPosition + (art - new Vector2(48f, 48f)) * scale;
	}

	// The heap's middle (the fall strip's last frame, CHOIR_FALL), and the ground under a standing voice.
	private Vector2 HeapOffset => ArtToLocal(new Vector2(48f, 86f));
	private Vector2 FeetOffset => ArtToLocal(new Vector2(48f, 89f));

	// A voice struck down in the middle of stepping in to sing has nothing left to sing with: its wind-up
	// runs out unseen (the ring and the icon go with it, below) and lands nothing. The telegraph's own
	// rule - a wind-up once begun always resolves - is about the player stepping away, not the singer
	// falling.
	protected override void OnAttackResolved(Node2D target)
	{
		if (!IsDowned)
			base.OnAttackResolved(target);
		steppingIn = false;
	}

	protected override bool ReadAttackTell(out TelegraphGlyph glyph, out float progress) =>
		base.ReadAttackTell(out glyph, out progress) && !IsDowned;

	/// <summary>
	/// The health bar shows the whole Choir, not whichever third the run happens to hold a
	/// reference to. A bar that jumped back to full when one voice revived would be lying about
	/// progress the player really had made.
	/// </summary>
	public override float BossHealthFraction
	{
		get
		{
			int current = 0;
			int max = 0;
			foreach (HollowChoirBoss voice in voices)
			{
				if (voice == null || !IsInstanceValid(voice))
					continue;
				current += voice.IsDowned ? 0 : Mathf.Max(0, voice.Health);
				max += voice.MaxHealth;
			}

			return max > 0 ? Mathf.Clamp(current / (float)max, 0f, 1f) : 0f;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (IsDying)
			return;
		float dt = (float)delta;
		TickRoute(dt);
		TickDip(dt);
		TickPlaces(dt);
		TickRound(dt);
		TickHeld(dt);
		if (IsDowned)
			TickDowned(dt);
		TickPose(dt);
	}

	// To its place or a fallen voice's side, not at him; planted through its own wind-up (the base's rule),
	// through a note's gesture and while it sings one back; still while down or under.
	protected override Vector2 AdjustSteering(Vector2 chaseDirection, float distanceToPlayer)
	{
		if (IsDowned || IsDipping || noteGestureLeft >= 0f || (Slam != null && Slam.IsWindingUp))
			return Vector2.Zero;
		if (reviving != null && IsInstanceValid(reviving) && InReachOf(reviving))
			return Vector2.Zero;
		if (Goal() is not Vector2 goal)
			return base.AdjustSteering(chaseDirection, distanceToPlayer);
		return WalkToward(goal, chaseDirection);
	}

	// Only a voice that has stepped in sings at him; the others hold their places, and one that has gone
	// to sing a fallen voice back sings only that.
	protected override bool WantsToAttack(Node2D target, float distanceToPlayer) =>
		!IsDowned && !IsDipping && reviving == null && noteGestureLeft < 0f && steppingIn
		&& distanceToPlayer <= SlamRadius * 1.25f;

	// --- Going quiet, and being sung back -------------------------------------------------------------

	/// <summary>
	/// Falling, not dying. Overriding <c>StartDeath</c> rather than intercepting damage is what
	/// keeps the rest of the game out of this: every spell, poison tick and trap still calls
	/// TakeDamage exactly as it does for anything else, and only the last step changes.
	/// </summary>
	protected override void StartDeath()
	{
		if (IsDying || IsDowned)
			return;

		if (EveryOtherVoiceIsDown())
		{
			// The whole Choir is silent at once. This is the only way the fight ends, and it ends
			// for all three in the same frame so the player never sees two thirds of a corpse.
			// Same reason as the snapshot in SummonTheRest: Silence walks into base.StartDeath,
			// and nothing on that path may be allowed to touch the list being enumerated.
			// They all die on this frame; they only go QUIET in turn - this one first, as the last
			// one struck, then the others in the order they were called up (fallOrder, the fall).
			int order = 1;
			foreach (HollowChoirBoss voice in voices.ToArray())
			{
				if (voice != null && IsInstanceValid(voice) && voice != this)
					voice.Silence(order++);
			}

			fallOrder = 0;
			EndDip();
			ShowStanding();
			base.StartDeath();
			return;
		}

		GoDown();
	}

	private bool EveryOtherVoiceIsDown()
	{
		foreach (HollowChoirBoss voice in voices)
		{
			if (voice == null || !IsInstanceValid(voice) || voice == this)
				continue;
			if (!voice.IsDowned && !voice.IsDying)
				return false;
		}

		return true;
	}

	// It slumps where it stood (the fall strip, then the heap breathing) and lies there out of reach -
	// SetTargetable(false) takes it out of every spell's sight - until a living voice has walked over and
	// sung it back. Nothing is on a clock any more: no voice comes to it, and it stays down.
	private void GoDown()
	{
		Health = 0;
		downed = true;
		downedFor = 0f;
		riseClock = -1f;
		reviveProgress = 0f;
		noteGestureLeft = -1f;
		steppingIn = false;
		EndDip();
		// A voice that falls while going to sing another back leaves it for the third.
		if (reviving != null && IsInstanceValid(reviving) && reviving.reviver == this)
			reviving.reviver = null;
		reviving = null;
		SetTargetable(false);
		// Untargetable hides the sprite (Enemy.SetTargetable); a fallen voice is still seen, as a heap.
		if (BodySprite != null)
			BodySprite.Visible = true;
		SfxPlayer.AtPosition(SfxCatalog.BossDeath, GlobalPosition, -6.0f);
	}

	private void TickDowned(float delta)
	{
		downedFor += delta;
		if (riseClock >= 0f)
		{
			riseClock += delta;
			if (riseClock >= RiseSeconds)
				Rise();
			return;
		}

		if (reviver == null || !IsInstanceValid(reviver) || reviver.IsDowned || reviver.IsDying || reviver.reviving != this)
			reviver = ChooseReviver();

		// The song runs only while the singer stands at its side and nothing holds it: a freeze, a root, a
		// stagger or stopped time on the singer pauses it (and on a boss none of those lasts).
		if (reviver != null && !reviver.IsDipping && reviver.InReachOf(this) && !reviver.IsHeldByCrowdControl)
			reviveProgress += delta;
		if (reviveProgress >= ReviveSeconds)
			BeginRise();
	}

	// The nearest voice standing that is not already singing another back - so with two down, the one
	// left raises them in turn, and the first raised goes to the second.
	private HollowChoirBoss ChooseReviver()
	{
		HollowChoirBoss best = null;
		float bestDistance = float.MaxValue;
		foreach (HollowChoirBoss voice in voices)
		{
			if (voice == null || !IsInstanceValid(voice) || voice == this || voice.IsDowned || voice.IsDying)
				continue;
			if (voice.reviving != null && voice.reviving != this && IsInstanceValid(voice.reviving) && voice.reviving.IsDowned)
				continue;
			float d = voice.GlobalPosition.DistanceSquaredTo(GlobalPosition);
			if (d < bestDistance)
			{
				best = voice;
				bestDistance = d;
			}
		}
		if (best != null)
		{
			best.reviving = this;
			best.steppingIn = false;
			best.noteGestureLeft = -1f;
		}
		return best;
	}

	private void BeginRise()
	{
		riseClock = 0f;
		SfxPlayer.Cast(Element.Water, GlobalPosition);
	}

	private void Rise()
	{
		downed = false;
		riseClock = -1f;
		reviveProgress = 0f;
		if (reviver != null && IsInstanceValid(reviver) && reviver.reviving == this)
			reviver.reviving = null;
		reviver = null;
		Health = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * ReviveHealthFraction));
		SetTargetable(true);
		ShowStanding();
		// It comes back already on cooldown rather than already swinging: a revive that opened with
		// a slam would punish the player for standing where the body had been harmless.
		steppingIn = false;
		untilStepIn = StepInIntervalSeconds;
	}

	// Called on the other two when the last one falls. Not a public kill switch - it exists so the
	// three deaths happen on one frame.
	private void Silence(int order)
	{
		fallOrder = order;
		downed = false;
		riseClock = -1f;
		reviving = null;
		reviver = null;
		EndDip();
		// Back in the group first: base.StartDeath expects to be the thing that takes it out, and
		// it also restores the sprite so there is a corpse to animate - standing, as the fallen rise to
		// sing the last one back and cannot (the fall, below).
		SetTargetable(true);
		ShowStanding();
		Health = 0;
		base.StartDeath();
	}

	// The fallen and the singing look after their own frames; this hands the body back to the walk.
	private void ShowStanding()
	{
		AnimatedSprite2D body = BodySprite;
		if (body == null)
			return;
		body.Visible = true;
		if (body.Animation != MovingAnimation || !body.IsPlaying())
			body.Play(MovingAnimation);
	}

	// Held by crowd control while it sings one back: counted, for _BossProbe (the song is paused for it).
	private void TickHeld(float delta)
	{
		if (IsSingingBack && IsHeldByCrowdControl)
			SongHeldSeconds += delta;
	}

	/// <summary>Seconds this voice's song over a fallen one was paused by crowd control. For _BossProbe.</summary>
	public float SongHeldSeconds { get; private set; }

	// --- The dip -----------------------------------------------------------------------------------

	private enum DipStage { None, Sinking, Under, Surfacing }
	private DipStage dipStage;
	private float dipClock;
	private float dipCooldown;
	private BurrowMove dip;
	// Two windows running in which it asked to walk and hardly moved, touching something: stuck.
	private float stuckClock;
	private float stuckWanted;
	private Vector2 stuckFrom;
	private int stuckWindows;
	private const float StuckWindowSeconds = 1.5f;

	private void TickDip(float delta)
	{
		if (dipCooldown > 0f)
			dipCooldown -= delta;
		switch (dipStage)
		{
			case DipStage.None:
				if (!IsDowned && !IsEntering && WantsToDip(delta) is Vector2 to)
					BeginDip(to);
				return;
			case DipStage.Sinking:
				dipClock += delta;
				if (dipClock >= DipSinkSeconds)
				{
					dipStage = DipStage.Under;
					dip.Begin(GlobalPosition, dipTo);
					if (BodySprite != null)
						BodySprite.Visible = false;
				}
				return;
			case DipStage.Under:
				BurrowMove.Beat beat = dip.Tick(delta);
				if (beat == BurrowMove.Beat.Travelling || beat == BurrowMove.Beat.Submerged)
					GlobalPosition = dip.CurrentPosition;
				else if (beat == BurrowMove.Beat.Surfaced || beat == BurrowMove.Beat.Idle)
				{
					GlobalPosition = dipTo;
					dipStage = DipStage.Surfacing;
					dipClock = 0f;
					if (BodySprite != null)
						BodySprite.Visible = true;
				}
				return;
			case DipStage.Surfacing:
				dipClock += delta;
				if (dipClock >= DipSinkSeconds)
					EndDip();
				return;
		}
	}

	private Vector2 dipTo;

	// Where it should go under to, if anywhere: its goal, when that is far, or when it is stuck.
	private Vector2? WantsToDip(float delta)
	{
		if (dipCooldown > 0f || noteGestureLeft >= 0f || (Slam != null && Slam.IsWindingUp) || IsSingingBack)
		{
			stuckWindows = 0;
			return null;
		}
		if (Goal() is not Vector2 goal)
			return null;

		bool stuck = false;
		stuckClock += delta;
		stuckWanted += IntendedSpeed * delta;
		if (stuckClock >= StuckWindowSeconds)
		{
			float moved = GlobalPosition.DistanceTo(stuckFrom);
			bool held = stuckWanted > 30f && moved < stuckWanted * 0.2f && GetSlideCollisionCount() > 0;
			stuckWindows = held ? stuckWindows + 1 : 0;
			stuck = stuckWindows >= 2;
			stuckClock = 0f;
			stuckWanted = 0f;
			stuckFrom = GlobalPosition;
		}

		if (!stuck && GlobalPosition.DistanceSquaredTo(goal) <= DipDistance * DipDistance)
			return null;
		MazeNavigation nav = MazeNavigation.Active;
		if (nav != null && !nav.IsReachable(goal))
			return null;
		return LeadDip(goal);
	}

	// Where it comes up when it is going to its place: led by the wizard's own walk over the time it is
	// under, so a voice left behind by a kiting wizard surfaces at his side and not where he was - the
	// first photographs had all three surfacing in a line behind him, dip after dip. A fallen voice's side
	// does not move, so that is not led.
	private Vector2 LeadDip(Vector2 goal)
	{
		if (reviving != null || TargetPlayer is not CharacterBody2D walker)
			return goal;
		Vector2 lead = walker.Velocity * (DipSinkSeconds + DipTravelSeconds) * DipLeadShare;
		if (lead.LengthSquared() > MaxDipLead * MaxDipLead)
			lead = lead.Normalized() * MaxDipLead;
		Vector2 led = goal + lead;
		MazeNavigation nav = MazeNavigation.Active;
		return nav == null || nav.IsReachable(led) ? led : goal;
	}

	// Most of the way, not all: led the whole way, a voice came up ahead of a wizard who then turned, and
	// was behind him again - one run took fifteen dips.
	private const float DipLeadShare = 0.6f;
	private const float MaxDipLead = 300f;

	// It sinks where it stands - out of reach from the first frame, as a burrower is (SetTargetable),
	// its sprite left showing for the sink - travels under the floor in a straight line, as every burrow
	// does, and comes up where it was going.
	private void BeginDip(Vector2 to)
	{
		dipStage = DipStage.Sinking;
		dipClock = 0f;
		dipTo = to;
		stuckWindows = 0;
		steppingIn = false;
		dip ??= new BurrowMove(DipTravelSeconds);
		dip.TravelSeconds = Mathf.Max(0.3f, DipTravelSeconds);
		DipsTaken++;
		SetTargetable(false);
		if (BodySprite != null)
			BodySprite.Visible = true;
	}

	private void EndDip()
	{
		if (dipStage == DipStage.None)
			return;
		dip?.Abort();
		dipStage = DipStage.None;
		dipCooldown = DipCooldownSeconds;
		stuckClock = 0f;
		stuckWanted = 0f;
		stuckFrom = GlobalPosition;
		hasPlaceOffset = false;
		if (!IsDowned && !IsDying)
		{
			SetTargetable(true);
			ShowStanding();
		}
	}

	// --- For _UiShot --choir: each of the three new things, on demand, so it can be photographed -----

	/// <summary>Sings a note now (after <paramref name="delay"/> seconds), whatever the phase.</summary>
	internal void DebugSingNote(float delay)
	{
		singingRound = true;
		untilNote = Mathf.Max(0f, delay);
	}

	/// <summary>Dips now, to the far side of the wizard from where it stands. False if it cannot now.</summary>
	internal bool DebugDip()
	{
		Node2D target = TargetPlayer;
		if (target == null || !IsInstanceValid(target) || IsDowned || IsDipping || (Slam != null && Slam.IsWindingUp))
			return false;
		float bearing = (target.GlobalPosition - GlobalPosition).Angle();
		BeginDip(OpenGroundNear(target.GlobalPosition, bearing, StandRadius));
		return true;
	}

	// --- How it looks -------------------------------------------------------------------------------

	private static readonly StringName MovingAnimation = "moving";
	private static readonly StringName SingAnimation = "sing";
	private static readonly StringName ChantAnimation = "chant";
	private static readonly StringName FallAnimation = "fall";
	private static readonly StringName DownedAnimation = "downed";
	private static readonly StringName DipAnimation = "dip";

	// The strips the Choir plays of its own (sprite_bosses.py, choir_extra_anims), on its own clocks, so a
	// flinch never cuts a song short or wakes a fallen voice (HoldsOwnPose); otherwise Enemy's walk.
	protected override bool HoldsOwnPose => IsDowned || IsDipping || noteGestureLeft >= 0f || IsSingingBack;

	private bool showingOwnPose;

	private void TickPose(float delta)
	{
		// The gold ring every boss wears (Enemy.ConfigureEliteMarker) hangs at the body's middle: over a
		// heap on the floor it floats in the air, and under the floor it would go on walking about alone.
		bool ringShown = !IsDowned && dipStage != DipStage.Under;
		if (GetNodeOrNull<CanvasItem>("EliteMarker") is CanvasItem ring && ring.Visible != ringShown)
			ring.Visible = ringShown;

		AnimatedSprite2D body = BodySprite;
		if (body?.SpriteFrames == null)
			return;

		if (IsDowned)
		{
			if (riseClock >= 0f)
				ShowFrame(body, FallAnimation, 1f - riseClock / RiseSeconds);
			else if (downedFor < FallSeconds)
				ShowFrame(body, FallAnimation, downedFor / FallSeconds);
			else
				Loop(body, DownedAnimation);
			return;
		}
		switch (dipStage)
		{
			case DipStage.Sinking:
				ShowFrame(body, DipAnimation, dipClock / DipSinkSeconds);
				return;
			case DipStage.Under:
				return;
			case DipStage.Surfacing:
				ShowFrame(body, DipAnimation, 1f - dipClock / DipSinkSeconds);
				return;
		}
		if (noteGestureLeft >= 0f)
		{
			ShowFrame(body, SingAnimation, 1f - noteGestureLeft / Mathf.Max(0.1f, NoteTellSeconds));
			return;
		}
		if (IsSingingBack)
		{
			Loop(body, ChantAnimation);
			return;
		}
		if (showingOwnPose)
		{
			showingOwnPose = false;
			ShowStanding();
		}
	}

	// One frame of a strip, picked by how far through it is (0 to 1), held still.
	private void ShowFrame(AnimatedSprite2D body, StringName animation, float through)
	{
		if (!body.SpriteFrames.HasAnimation(animation))
			return;
		showingOwnPose = true;
		TakeOverSprite();
		int count = body.SpriteFrames.GetFrameCount(animation);
		int frame = Mathf.Clamp((int)(Mathf.Clamp(through, 0f, 1f) * count), 0, count - 1);
		if (body.Animation != animation)
			body.Animation = animation;
		if (body.IsPlaying())
			body.Pause();
		if (body.Frame != frame)
			body.Frame = frame;
	}

	private void Loop(AnimatedSprite2D body, StringName animation)
	{
		if (!body.SpriteFrames.HasAnimation(animation))
			return;
		showingOwnPose = true;
		TakeOverSprite();
		if (body.Animation != animation || !body.IsPlaying())
			body.Play(animation);
	}

	public override void _Draw()
	{
		base._Draw();
		if (IsDying)
			return;
		if (IsDowned && riseClock < 0f)
			DrawReviveClock();
		if (IsSingingBack && !IsHeldByCrowdControl)
			DrawSongTo(reviving);
		if (noteGestureLeft >= 0f)
			DrawGathering();
	}

	// The song so far, round the heap, flattened onto the floor: a dim ring of whole cells and the part
	// already sung over it bright. The player has to be able to see how long is left on a body they
	// cannot hit - and, now, that it only fills while a voice stands over it singing.
	private void DrawReviveClock()
	{
		float progress = ReviveProgress;
		var dim = new Color(ChoirColor.R * 0.35f, ChoirColor.G * 0.35f, ChoirColor.B * 0.4f);
		Vector2 foot = HeapOffset;
		BossDeath.Arc(this, foot, DownedRingRadius, DownedRingRadius * 0.45f, 0f, Mathf.Tau, dim);
		if (progress > 0f)
			BossDeath.Arc(this, foot, DownedRingRadius, DownedRingRadius * 0.45f, -Mathf.Pi / 2f,
				-Mathf.Pi / 2f + Mathf.Tau * progress, ChoirColor, BossDeath.Px * 2f);
		// Its mark wakes as the song fills it.
		if (progress > 0f)
			BossDeath.Disc(this, ArtToLocal(new Vector2(47f, 73f)), 3f + 5f * progress, 2f + 3f * progress,
				new Color(ElementColors.GetRamp(Element.Water).Hot, 0.5f + 0.5f * progress));
	}

	private const float DownedRingRadius = 44f;

	// THE SONG, flowing from the singer's cowl to the fallen one: arcs of it leaving the mouth and
	// travelling down to the heap, the death's song arcs sent on their way, on the 2-unit grid. Not drawn
	// while the singer is held, so the pause is seen.
	private void DrawSongTo(HollowChoirBoss fallen)
	{
		ElementColors.Ramp water = ElementColors.GetRamp(Element.Water);
		Vector2 from = MouthsOffset;
		Vector2 to = fallen.GlobalPosition - GlobalPosition + fallen.HeapOffset;
		Vector2 along = to - from;
		float length = along.Length();
		if (length < 4f)
			return;
		float facing = along.Angle();
		float t = (float)Time.GetTicksMsec() / 1000f;
		for (int k = 0; k < 4; k++)
		{
			float u = Mathf.PosMod(t * 0.9f + k * 0.25f, 1f);
			float fade = Mathf.Min(1f, 5f * u) * Mathf.Min(1f, 4f * (1f - u));
			Vector2 at = from + along * u;
			float r = 10f + 6f * Mathf.Sin(u * Mathf.Pi);
			BossDeath.Arc(this, at, r, r, facing - 0.8f, facing + 0.8f, new Color(water.Hot, 0.9f * fade));
			BossDeath.Arc(this, at - along / length * 4f, r * 0.7f, r * 0.7f, facing - 0.6f, facing + 0.6f,
				new Color(ChoirColor, 0.7f * fade));
		}
	}

	// The note gathering at the mouths through the gesture, as the sing strip lights them from the hem up,
	// in the second half of it: the note it is about to send, swelling to the size it flies at.
	private void DrawGathering()
	{
		float u = Mathf.Clamp(2f * (1f - noteGestureLeft / Mathf.Max(0.1f, NoteTellSeconds)) - 1f, 0f, 1f);
		if (u <= 0f)
			return;
		ElementColors.Ramp water = ElementColors.GetRamp(Element.Water);
		Vector2 at = MouthsOffset;
		float r = NoteRadius * u;
		BossDeath.Disc(this, at, r * 0.7f, r * 0.55f, new Color(water.Mid, 0.45f + 0.35f * u));
		BossDeath.Disc(this, at, r * 0.35f, r * 0.3f, new Color(water.Core, 0.7f + 0.3f * u));
	}

	// Under the floor, a dark swell travelling with it and a ring of ripples where it will come up; when it
	// sinks or surfaces, the strip's own pool does the work. Whole cells, opaque.
	protected override void DrawGroundTells(CanvasItem floor)
	{
		if (!IsDowned)
			base.DrawGroundTells(floor);
		if (IsDying || dipStage != DipStage.Under || dip == null)
			return;
		var deep = new Color(0.03f, 0.05f, 0.08f);
		var rim = ElementColors.GetRamp(Element.Water).Edge;
		float swell = 1f + 0.15f * Mathf.Sin(dip.Progress * Mathf.Tau * 3f);
		Vector2 feet = FeetOffset;
		BossDeath.Disc(floor, feet, 20f * swell, 9f * swell, deep);
		BossDeath.Arc(floor, feet, 24f * swell, 11f * swell, 0f, Mathf.Tau, rim);
		// Where it comes up, through the last half of the travel: ripples spreading on the water there.
		if (dip.Progress > 0.5f)
		{
			Vector2 there = dipTo - GlobalPosition + feet;
			float u = (dip.Progress - 0.5f) * 2f;
			BossDeath.Disc(floor, there, 10f + 18f * u, 4f + 8f * u, deep);
			BossDeath.Arc(floor, there, 16f + 22f * u, 7f + 10f * u, 0f, Mathf.Tau, rim);
		}
	}

	// --- The arrival (BossDeath, entrance) ---------------------------------------------------------
	//
	// OUT OF THE DARK, ONE VOICE AT A TIME. Where each voice will stand the cave goes darker than dark;
	// motes of its colour drift in out of the air and wind down into it; the robe forms inside the
	// dark as the dark thins; and the hood flares as it finds its note - the first voice, then the
	// second, then the third, a beat apart. Only the first roars: three at once would be one sound
	// three times over (boss_roar is a once-a-run cue, .ai/audio-direction.md).
	protected internal override float EntranceSeconds => 3.0f;
	// The climax (BossDeath calls it on the roar): a song, not a landing: the screen trembles rather than jumps.
	protected internal override float EntranceClimaxWeight => 0.45f;
	protected internal override Element EntranceFlareElement => Element.Water;
	protected internal override float EntranceDelaySeconds => voiceOrder * 0.35f;
	protected internal override float EntranceRoarSeconds => IsEcho ? -1f : 2.3f;

	private BossDeath.Piece robe;
	private const float Forms = 0.9f;
	private const float Formed = 2.3f;

	protected internal override void OnEntranceBegin(BossDeath stage)
	{
		robe = stage.CutWhole();
		robe.Color = new Color(0.05f, 0.08f, 0.10f, 0f);
	}

	protected internal override void TickEntrance(BossDeath stage, float t, float dt)
	{
		if (robe == null)
			return;
		float form = BossDeath.EaseInOut(BossDeath.Span(t, Forms, Formed));
		robe.Color = new Color(0.05f, 0.08f, 0.10f, 0f).Lerp(Colors.White, form);
		robe.Offset = new Vector2(0f, 10f * (1f - form) * stage.ArtScale.Y);
		float flare = BossDeath.Span(t, Formed - 0.1f, Formed) * (1f - BossDeath.Span(t, Formed, EntranceSeconds));
		robe.Glow = new Color(ChoirColor, 0.7f * flare);
		// An echo forms without a roar (only the first voice has one): a lighter beat of its own.
		if (IsEcho && stage.Crossed(Formed))
			stage.Impact(0.2f);
		if (stage.Crossed(Formed))
			stage.PuffAt(stage.ArtToLocal(new Vector2(48f, 30f)), 8f, 46f, 0.6f, new Color(ChoirColor, 0.6f), 6f, 0f, true);
	}

	// The dark it forms in: a pool of night over and round the body, thinning as the robe forms.
	protected internal override void DrawEntranceAir(BossDeath stage, CanvasItem canvas, float t)
	{
		float dark = BossDeath.Span(t, 0f, 0.5f) * (1f - BossDeath.Span(t, Forms + 0.4f, Formed + 0.2f));
		if (dark <= 0f)
			return;
		Vector2 centre = stage.ArtToLocal(new Vector2(48f, 54f));
		var night = new Color(0.01f, 0.02f, 0.04f, 0.55f * dark);
		BossDeath.Disc(canvas, centre, 70f, 84f, night);
		BossDeath.Disc(canvas, centre + new Vector2(0f, 10f), 46f, 58f, night);
	}

	// The motes winding in, and after the flare the song: short arcs lifting off the hood.
	protected internal override void DrawEntranceLight(BossDeath stage, CanvasItem canvas, float t)
	{
		Vector2 centre = stage.ArtToLocal(new Vector2(48f, 46f));
		float gather = BossDeath.Span(t, 0f, Formed);
		if (gather < 1f)
		{
			for (int k = 0; k < 12; k++)
			{
				float born = 0.9f * BossDeath.Hash(31u, k);
				float u = BossDeath.Span(t, born, born + 1.3f);
				if (u <= 0f || u >= 1f)
					continue;
				float a = Mathf.Tau * BossDeath.Hash(37u, k) + 3.2f * u;
				float r = (70f + 70f * BossDeath.Hash(41u, k)) * (1f - BossDeath.EaseIn(u));
				Vector2 at = BossDeath.Snap(centre + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.8f));
				float alpha = Mathf.Min(1f, 4f * (1f - u)) * Mathf.Min(1f, 5f * u);
				canvas.DrawRect(new Rect2(at - new Vector2(BossDeath.Px, BossDeath.Px), new Vector2(BossDeath.Px * 3f, BossDeath.Px * 3f)), new Color(ChoirColor, 0.35f * alpha));
				canvas.DrawRect(new Rect2(at, new Vector2(BossDeath.Px, BossDeath.Px)), new Color(Colors.White, 0.9f * alpha));
			}
		}
		float song = BossDeath.Span(t, Formed, Formed + 0.9f);
		if (song > 0f && song < 1f)
		{
			Vector2 hood = stage.ArtToLocal(new Vector2(48f, 18f));
			for (int k = 0; k < 3; k++)
			{
				float u = Mathf.Clamp(song * 1.4f - k * 0.2f, 0f, 1f);
				if (u <= 0f || u >= 1f)
					continue;
				float rx = 14f + 26f * u;
				BossDeath.Arc(canvas, hood - new Vector2(0f, 30f * u), rx, rx * 0.45f, Mathf.Pi * 1.15f, Mathf.Pi * 1.85f,
					new Color(ChoirColor, 0.8f * (1f - u)));
			}
		}
	}

	// --- The fall (BossDeath) ---------------------------------------------------------------------
	//
	// THE VOICES GO OUT ONE BY ONE. The fight's rule is that the three must fall on the same frame,
	// and they do - the rewards, the group, the victory are all that frame's (StartDeath above). What
	// is staggered is only the silence: the voice struck last sings its last note first, and then the
	// two that had already sunk rise to sing it back, as they have all fight, and cannot. Each holds
	// its note - the hood flares, the song goes out of it in three widening arcs - and then the robe,
	// with nothing in it any more, folds down onto the cave floor and is gone, leaving a ring of wet
	// stone. Cloth and water: sprite_bosses.py's bell of grave linen, and the Sunken Cave.
	private int fallOrder;
	private BossDeath.Piece deathRobe;

	/// <summary>Seconds between one voice going quiet and the next.</summary>
	private const float VoiceTurnSeconds = 0.45f;
	private const float NoteSeconds = 0.36f;

	protected internal override float DeathDelaySeconds => fallOrder * VoiceTurnSeconds;
	protected internal override float DeathSeconds => 1.5f;

	protected internal override void OnDeathBegin(BossDeath death)
	{
		deathRobe = death.CutWhole();
	}

	protected internal override void TickDeath(BossDeath death, float t, float dt)
	{
		if (deathRobe == null)
			return;

		// The note: a breath of the choir's light through the cloth.
		float note = BossDeath.Span(t, 0f, 0.08f) * (1f - BossDeath.Span(t, 0.25f, 0.5f));
		deathRobe.Glow = new Color(ChoirColor, 0.4f * note);

		// Then the robe empties and folds onto the floor, spreading a little as it settles.
		float fold = BossDeath.EaseIn(BossDeath.Span(t, 0.32f, 0.95f));
		deathRobe.Scale = new Vector2(1f + 0.32f * fold, 1f - 0.86f * fold);
		float dim = BossDeath.Span(t, 0.3f, 0.9f);
		deathRobe.Color = new Color(1f - 0.4f * dim, 1f - 0.35f * dim, 1f - 0.25f * dim, 1f - BossDeath.Span(t, 0.95f, 1.4f));

		if (death.Crossed(0.32f))
		{
			ElementColors.Ramp water = ElementColors.GetRamp(Element.Water);
			// What it was made of goes up out of it as it falls...
			death.Emit(new BossDeath.Throw
			{
				Size = 4f, Color = ChoirColor, ColorB = water.Hot, Light = true, NoLand = true,
				SpeedMin = 6f, SpeedMax = 30f, UpMin = 20f, UpMax = 70f, Gravity = -50f, Drag = 1f,
				Life = 1.0f, Fade = 0.6f, Flutter = 3f,
			}, 14, new Vector2(48f, 54f), new Vector2(16f, 30f), 0f, 0.02f);
			// ...and what it was soaked in runs off it.
			death.Emit(new BossDeath.Throw
			{
				Size = 2f, Color = water.Mid, ColorB = water.Edge, SpeedMin = 10f, SpeedMax = 60f,
				UpMin = 10f, UpMax = 60f, Gravity = 520f, Bounce = 0f, Friction = 12f, Life = 0.9f, Fade = 0.4f,
				DepthJitter = 14f,
			}, 12, new Vector2(48f, 70f), new Vector2(22f, 18f));
		}
	}

	// The wet stone it leaves, spreading from the heap: a dark patch with a wetter edge, uneven, and
	// never a ring - a ring round a boss is a telegraph in this game.
	protected internal override void DrawDeathGround(BossDeath death, CanvasItem canvas, float t)
	{
		if (deathRobe == null || t < 0.7f)
			return;
		ElementColors.Ramp water = ElementColors.GetRamp(Element.Water);
		float spread = BossDeath.EaseOut(BossDeath.Span(t, 0.7f, 1.45f));
		float fade = 1f - BossDeath.Span(t, 1.05f, DeathSeconds);
		float r = 22f + 46f * spread;
		var edge = new Color(water.Edge, 0.22f * fade);
		var wet = new Color(water.Edge.R * 0.25f, water.Edge.G * 0.3f, water.Edge.B * 0.45f, 0.45f * fade);
		Vector2 foot = death.Foot;
		BossDeath.Disc(canvas, foot + new Vector2(-r * 0.3f, 0f), r * 0.75f + 4f, r * 0.32f + 3f, edge);
		BossDeath.Disc(canvas, foot + new Vector2(r * 0.28f, 2f), r * 0.6f + 4f, r * 0.28f + 3f, edge);
		BossDeath.Disc(canvas, foot + new Vector2(-r * 0.3f, 0f), r * 0.75f, r * 0.32f, wet);
		BossDeath.Disc(canvas, foot + new Vector2(r * 0.28f, 2f), r * 0.6f, r * 0.28f, wet);
	}

	// The last note: the eye flares, and the song leaves the hood in three arcs either side.
	protected internal override void DrawDeathLight(BossDeath death, CanvasItem canvas, float t)
	{
		if (deathRobe == null || t > 0.9f)
			return;

		Vector2 head = death.PieceToLocal(deathRobe, new Vector2(47.5f, 20f));
		float eye = BossDeath.Span(t, 0f, 0.06f) * (1f - BossDeath.Span(t, 0.2f, 0.5f));
		BossDeath.Disc(canvas, head, 7f, 7f, new Color(ChoirColor, 0.9f * eye));

		for (int k = 0; k < 3; k++)
		{
			float born = k * 0.1f;
			float u = BossDeath.Span(t, born, born + NoteSeconds);
			if (u <= 0f || u >= 1f)
				continue;
			float r = 16f + 70f * BossDeath.EaseOut(u);
			var c = new Color(ChoirColor, 0.85f * (1f - u));
			BossDeath.Arc(canvas, head, r, r * 0.8f, -0.55f, 0.55f, c);
			BossDeath.Arc(canvas, head, r, r * 0.8f, Mathf.Pi - 0.55f, Mathf.Pi + 0.55f, c);
		}
	}
}
