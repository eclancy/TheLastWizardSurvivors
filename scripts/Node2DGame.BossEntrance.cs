using Godot;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// A BOSS'S ARRIVAL, the run's half (2026-10-31: "whenever a boss spawns, all of the players spells
// should stop firing, and you should see an animation of the boss spawning"). The boss plays its own
// entrance (BossEnemy, on the BossDeath stage) and is untargetable and still while it does; this holds
// everything else for as long as any body of it is arriving (BossEnemy.EnteringCount):
//
//   - the player's spells are silent, their timers HELD rather than reset (Player.HeldForBossEntrance,
//     read inside the one spellFireTimers loop), nothing can hurt him, and he cannot walk (MovePlayer
//     drops every device's push while it is set);
//   - every ordinary body on the field is HELD where it stands (Enemy.SetHeld): no step, no wind-up,
//     no shot, and no damage taken. Silencing the spells and leaving the swarm walking would hand it
//     a free hit the moment the fight began; holding it means the fight starts exactly where the field
//     stood when the boss appeared.
//
// AND IT IS SHOT AS A CINEMATIC (2026-11-01, from playing 0.10.0: "Player should not be able to move when
// bosses are spawning. Show black bars like movie trailers, use zoom, screenshake, and dramatic cinematic
// flare to highlight what is happening"). Four things, all presentation, none of them gameplay:
//
//   - LETTERBOX BARS slide in from the top and bottom edges over the HUD's edges and out again when the
//     hold lets go. A canvas layer of their own (4): over the HUD (1), under the touch pad and its pause
//     button (5) and every menu, and they take no input. The boss bar fills underneath them and is
//     there, full, as they leave.
//   - THE CAMERA leans toward the arriving bodies (Position: the centre of all of them, so a pair from
//     the Throne's procession is framed as one) and PUSHES IN (Zoom) over the arrival, then eases back as
//     the fight starts. Never further in than keeps every arriving body - with room round it for its
//     first tell - and the wizard inside the picture between the bars (FitZoomFactor).
//   - THE SCREEN SHAKES (ScreenShake, through the camera's Offset, which is the shake's alone) on the
//     beats each boss marks in its own timeline: its climax (the roar) and its landings and bursts
//     (BossDeath.Impact), each weighted by the boss. A fall's beats shake it too.
//   - THE CLIMAX FLARES: on the roar, the bars jolt in a few pixels, the push-in kicks, and the bars'
//     inner edges catch the boss's light - a line stepping down its element's ramp, core to edge, with a
//     spill into the picture that dissolves through the Bayer matrix. Opaque stops and dither only (no
//     translucent wash, .ai/art-direction.md), and gone a third of a second after it lands.
//
// One hold covers every body arriving at once - the Choir's three voices, a procession's pair - so one
// cinematic does too. Real seconds throughout: the arrival is how a moment feels, not a position on the run.
//
// THE CINEMATIC RULES (Eric, 2026-11-03: "Always give a pause before panning, or beginning play after a cutscene,
// and don't move too fast, so the player can keep up visually"; .ai/cutscenes.md). The hold runs in four beats:
//
//   ARRIVING  the bars close and the picture pushes in while the camera holds on the wizard (BossEntrancePanDelay),
//             then it pans to the arrival - eased in and out, never faster than BossEntrancePanMaxSpeed - and the
//             boss's climax lands with speed lines thrown out of it in its own light (SpeedLines).
//   LANDED    the last body is down, in its own sprite, waiting for its fight (BossEnemy.IsAwaitingFight). The
//             camera stays on it: BossEntranceLandedHold since the landing, and BossEntranceRestHold since the pan
//             came to rest, whichever is later - a short arrival lands while its pan is still settling.
//   RETURNING the camera goes back to the wizard and the zoom to the run's own, at the same capped ease.
//   READY     the camera is home and the bars go out; BossEntrancePlayHold later the field is let go - the
//             wizard walks, his spells fire, the swarm and the boss move - all on one frame.
public partial class Node2DGame
{
	/// <summary>The longest the ARRIVING beat may last, whatever the count says: a body freed oddly must not hold the run.</summary>
	private const float MaxBossEntranceHoldSeconds = 6f;

	/// <summary>The longest any later beat may last, if the camera never settles (it always should).</summary>
	private const float MaxEntranceBeatSeconds = 3.5f;

	/// <summary>How far the camera leans toward an arriving boss, at most, in world pixels.</summary>
	private const float BossEntranceCameraLean = 230f;

	/// <summary>Of the way to the boss, the share the camera leans.</summary>
	private const float BossEntranceCameraShare = 0.6f;

	/// <summary>How quickly the camera eases toward its lean and back (1/s, exponential).</summary>
	private const float BossEntranceCameraEase = 3.2f;

	// BARS FIRST, THEN THE PAN (Eric, 2026-11-02: "cutscenes should stop spells firing, show the black
	// bars, but not pan the camera immediately. after the zoom/black bars, etc, then it can pan the camera
	// to the boss spawning ... i want to prevent the player from being overwhelmed by sudden camera
	// panning"). The pan used to start on the hold's first frame, on the exponential ease above, which
	// moves fastest in its first frame: the picture lurched toward the boss while the bars were still
	// closing. Now the camera holds still on the wizard while the bars close and the picture pushes in,
	// and only then pans, on a critically damped spring - it starts from rest, gathers speed and settles
	// without overshoot - and comes back the same way when the hold lets go.

	/// <summary>Real seconds the camera holds still once a hold begins: the bars' closing and a beat (rule 3).</summary>
	internal const float BossEntrancePanDelay = LetterboxInSeconds + 0.2f;

	/// <summary>The pan's spring (1/s): critically damped, under the caps below; about 1.5 s to settle on the boss.</summary>
	internal const float BossEntrancePanStiffness = 3.6f;

	// NEVER FASTER THAN THE EYE (rule 5). The spring alone starts at its hardest - 230 px of lean asked ~3000 px/s2
	// of it on the first frame, and it was near its top speed a quarter of a second later (_BossProbe before this:
	// "fastest 292 px/s at 0.90s", 0.25 s after the pan began). Capping the acceleration makes the start an ease of
	// about 0.4 s, and capping the speed keeps the furthest lean (230 px) to about a sixth of a PC screen a second.
	// The settle is the spring's own (its deceleration peaks near 320 px/s2, under the cap): no overshoot.

	/// <summary>The fastest the arrival's camera pans, world px/s.</summary>
	internal const float BossEntrancePanMaxSpeed = 220f;

	/// <summary>The hardest it speeds up or slows down, world px/s2: the ease in and out.</summary>
	internal const float BossEntrancePanMaxAccel = 600f;

	// THE HOLDS (rules 3 and 4), picked on the probe's numbers. A hold under about 0.4 s reads as a hitch rather than
	// a held shot; one over a second reads as the game waiting on the player.

	/// <summary>Seconds the camera stays on the arrival after the last body lands, before it goes back.</summary>
	internal const float BossEntranceLandedHold = 0.5f;

	/// <summary>Seconds the pan must have been at rest on the arrival before it goes back: never a pan straight into a pan.</summary>
	internal const float BossEntranceRestHold = 0.45f;

	/// <summary>Seconds from the camera home on the wizard (the bars going out) to the field let go.</summary>
	internal const float BossEntrancePlayHold = 0.6f;

	// BOSS RUSH ARRIVES SHORTER (Eric, 2026-11-03, of a ~6 s arrival ten times in a row: "Yes, shorter there"). The
	// rush keeps every rule - the bars, a hold before the pan, a pan no faster than the eye, a hold on the boss and
	// one before play - but each at its least: the camera leans half as far (a rush boss spawns 300-400 px off, so
	// half the lean still frames it) and each hold sits at the floor where it still reads as a held shot
	// (RegressionChecks.Cinematics holds both sets). The boss's own themed entrance is untouched.

	/// <summary>In Boss Rush, of the way to the boss, the share the camera leans.</summary>
	internal const float BossRushCameraShare = 0.3f;

	/// <summary>In Boss Rush, how far the camera leans toward an arriving boss, at most, in world pixels.</summary>
	internal const float BossRushCameraLean = 120f;

	/// <summary>In Boss Rush, seconds the camera stays on the arrival after the last body lands.</summary>
	internal const float BossRushLandedHold = 0.4f;

	/// <summary>In Boss Rush, seconds the pan must have been at rest on the arrival before it goes back.</summary>
	internal const float BossRushRestHold = 0.3f;

	/// <summary>In Boss Rush, seconds from the camera home to the field let go.</summary>
	internal const float BossRushPlayHold = 0.4f;

	private float EntranceCameraShare => IsBossRush ? BossRushCameraShare : BossEntranceCameraShare;
	private float EntranceCameraLean => IsBossRush ? BossRushCameraLean : BossEntranceCameraLean;
	private float EntranceLandedHold => IsBossRush ? BossRushLandedHold : BossEntranceLandedHold;
	private float EntranceRestHold => IsBossRush ? BossRushRestHold : BossEntranceRestHold;
	private float EntrancePlayHold => IsBossRush ? BossRushPlayHold : BossEntrancePlayHold;

	/// <summary>At rest on its mark: within this many world px, moving slower than CameraRestSpeed px/s.</summary>
	internal const float CameraRestPx = 6f;
	internal const float CameraRestSpeed = 12f;
	/// <summary>Home on the wizard as the eye sees it; the last of the settle goes on under the beat before play.</summary>
	private const float CameraHomePx = 8f;
	private const float CameraHomeSpeed = 25f;
	private const float CameraHomeZoom = 0.01f;

	// --- The cinematic's tuning ----------------------------------------------------------------------

	/// <summary>
	/// Each bar's height, as a share of the canvas height (plus the phone's safe inset on its edge): about a
	/// 2.35:1 picture on a 16:9 screen, and a little less of the tall phone canvas, where the same share
	/// would take a third of the screen.
	/// </summary>
	private const float LetterboxShare = 0.11f;
	private const float LetterboxSharePortrait = 0.09f;
	internal const float LetterboxInSeconds = 0.45f;
	private const float LetterboxOutSeconds = 0.4f;
	private const int LetterboxLayer = 4;
	/// <summary>The bars: the palette's occlusion, the one black everything shares.</summary>
	private static readonly Color LetterboxColor = new(0.0196f, 0.0275f, 0.0471f);

	/// <summary>The furthest the camera pushes in, as a factor on the run's zoom (0.85 becomes 1.09).</summary>
	private const float CinematicMaxZoom = 1.28f;
	/// <summary>
	/// The furthest it pulls OUT, for bodies spread wider than the picture between the bars holds: the Choir's
	/// three voices, a procession's pair. A wide shot rather than a body arriving out of frame.
	/// </summary>
	private const float CinematicMinZoom = 0.88f;
	/// <summary>Room kept round an arriving body's origin, world px: its body, and the reach of its first tell.</summary>
	private static readonly Vector2 ArrivalFrameMargin = new(150f, 160f);
	/// <summary>Room kept round the wizard, world px: he is where the fight starts.</summary>
	private static readonly Vector2 WizardFrameMargin = new(60f, 80f);
	// The push-in rides a spring too (rule 5). It eased exponentially, which moves fastest on its first frame
	// (x0.39 a second into a push of x0.28), and back out at 4.5/s - a snap as the fight began.
	private const float CinematicZoomStiffness = 3.6f;
	private const float CinematicZoomMaxRate = 0.35f;
	private const float CinematicZoomMaxAccel = 1.2f;

	/// <summary>World pixels a weight-1 beat shakes the screen; a climax shakes a little harder.</summary>
	private const float ImpactShakePx = 6f;
	private const float ClimaxShakePx = 8f;
	/// <summary>Canvas pixels the bars jump in at a weight-1 climax, and the most they ever jump.</summary>
	private const float ClimaxBarJolt = 8f;
	private const float MaxBarJolt = 12f;
	/// <summary>The push-in's kick at a weight-1 climax, on the zoom factor.</summary>
	private const float ClimaxZoomPunch = 0.05f;
	private const float FlareSeconds = 0.34f;

	// THE CLIMAX'S SPEED LINES (rule 1): thrown out of the boss in its own light, from a clear ring round its body to
	// past the edge of the picture between the bars, on the world's 2 px grid; more of them the harder it lands.
	private const float ClimaxLinesSeconds = 0.55f;
	private const float ClimaxLinesReach = 330f;
	private const int ClimaxLinesFew = 18;
	private const int ClimaxLinesMore = 14;

	private bool bossEntranceHeld;
	private float bossEntranceHeldSeconds;

	private enum EntranceBeat { Arriving, Landed, Returning, Ready }
	private EntranceBeat entranceBeat;
	private float entranceBeatSeconds;
	// How long the camera has been at rest on the arrival (LANDED waits on it) and whether it is home on the wizard
	// (RETURNING waits on that); both written by the camera's tick.
	private float cameraRestSeconds;
	private bool cameraHome;
	private bool cinematicPanBegun;
	private float cinematicZoomVelocity;
	private readonly List<Enemy> heldForBossEntrance = new();
	private Camera2D? entranceCamera;

	// The camera's own state while the cinematic has it: the zoom it found, the factor and kick written on
	// top, and whether it is still away from rest (lean, zoom or both).
	private bool cinematicCameraActive;
	private Vector2 cinematicBaseZoom = Vector2.One;
	private float cinematicZoom = 1f;
	private float cinematicZoomPunch;
	private float appliedZoomFactor = 1f;
	private Vector2 cinematicPanVelocity;

	private readonly ScreenShake screenShake = new();

	private CanvasLayer? letterboxLayer;
	private Control? letterbox;
	private float letterboxShown;
	private float letterboxJolt;
	private float flareAge = -1f;
	private ElementColors.Ramp flareRamp;

	// Sixteen levels of the 4x4 Bayer matrix on the 2 px art grid, as 8x8 white tiles, drawn tiled and
	// modulated to an opaque ramp stop: the flare's spill, and the only gradient the art contract allows.
	private static Texture2D[]? bayerTiles;

	/// <summary>True while the field is held for a boss's arrival. For _BossProbe and _UiShot.</summary>
	public bool BossEntranceHeld => bossEntranceHeld;

	/// <summary>True while a body that lands must wait for this run to let it fight (BossEnemy.FinishEntrance).</summary>
	public bool HoldsLandedBossFights => bossEntranceHeld && !runFinished;

	/// <summary>The hold's beat by name (Arriving, Landed, Returning, Ready); empty when nothing is held. For the tools.</summary>
	public string BossEntranceBeatName => bossEntranceHeld ? entranceBeat.ToString() : string.Empty;

	/// <summary>0 to 1: how far in the letterbox bars are. For _BossProbe and _UiShot.</summary>
	public float LetterboxShown => letterboxShown;

	/// <summary>The push-in written on the camera's zoom, 1 at rest. For _BossProbe and _UiShot.</summary>
	public float CinematicZoomFactor => appliedZoomFactor;

	/// <summary>
	/// The run camera's zoom without the arrival's push-in: what a spawn ring or an "is it on screen" test
	/// must read, or the ring would close in while the camera is pushed in and the swarm would be standing in
	/// plain sight when it eases back out.
	/// </summary>
	public static float GameplayZoomOf(Camera2D? camera)
	{
		if (camera == null || camera.Zoom.X <= 0.01f)
			return 1f;
		float zoom = camera.Zoom.X;
		if (Current is Node2DGame run && run.entranceCamera == camera && run.appliedZoomFactor > 0.01f)
			zoom /= run.appliedZoomFactor;
		return Mathf.Max(0.01f, zoom);
	}

	/// <summary>Shakes the run's screen (ScreenShake): the stronger of this and whatever is still shaking.</summary>
	public void ShakeScreen(float amplitude, float seconds) => screenShake.Kick(amplitude, seconds);

	/// <summary>A boss's landing, burst or thud, from its own timeline (BossDeath.Impact), weight 1 a roar's.</summary>
	public void OnBossImpact(BossEnemy boss, float weight)
	{
		if (weight <= 0f)
			return;
		ShakeScreen(ImpactShakePx * weight, 0.25f + 0.3f * weight);
		if (letterboxShown > 0f)
			letterboxJolt = Mathf.Clamp(ClimaxBarJolt * 0.4f * weight, letterboxJolt, MaxBarJolt);
	}

	/// <summary>
	/// An arrival's climax (BossDeath, on the roar): the shake at the boss's weight, speed lines thrown out of the
	/// body (round <paramref name="bodyCentre"/>, clear of <paramref name="bodyRadius"/>) in
	/// <paramref name="element"/>'s light, and while the bars are in, the jolt, the kick of the push-in and the flare.
	/// </summary>
	public void OnBossClimax(BossEnemy boss, float weight, Element element, Vector2? bodyCentre = null, float bodyRadius = 0f)
	{
		if (weight > 0f)
			ShakeScreen(ClimaxShakePx * weight, 0.35f + 0.35f * weight);
		float w = Mathf.Max(0.3f, weight);
		if (boss != null && IsInstanceValid(boss))
		{
			ElementColors.Ramp ramp = ElementColors.GetRamp(element);
			Vector2 centre = bodyCentre ?? boss.GlobalPosition;
			float inner = Mathf.Max(56f, bodyRadius);
			int streaks = ClimaxLinesFew + Mathf.RoundToInt(ClimaxLinesMore * Mathf.Min(1.3f, w) / 1.3f);
			BurstSpeedLines(centre, inner, inner + ClimaxLinesReach * (0.75f + 0.25f * Mathf.Min(1f, w)), streaks,
				ClimaxLinesSeconds, ramp.Hot, ramp.Mid, SeedOf(boss.BossId), SpeedLines.Motion.Outward);
		}
		if (!bossEntranceHeld && letterboxShown <= 0f)
			return;
		letterboxJolt = Mathf.Clamp(ClimaxBarJolt * w, letterboxJolt, MaxBarJolt);
		cinematicZoomPunch = Mathf.Max(cinematicZoomPunch, ClimaxZoomPunch * w);
		flareRamp = ElementColors.GetRamp(element);
		flareAge = 0f;
	}

	// --- Impact beats: speed lines and the flash (.ai/cutscenes.md, rule 1) ---------------------------------
	//
	// One burst of speed lines at a time, in world space on its own node over every body (DrawLayers.ForemostAir,
	// absolute), and one flash at a time on the letterbox's canvas layer, over the HUD. Both tick from the arrival's
	// tick in a run and from FilmDefeat through the defeat's paused tree, and draw nothing once they are done.

	private Node2D? impactLayer;
	private readonly SpeedLines impactLines = new();
	private readonly DitherFlash screenFlash = new();
	private bool impactDrawn;

	/// <summary>
	/// Speed lines round <paramref name="centre"/> (world px), between <paramref name="inner"/> and
	/// <paramref name="outer"/>, on the world's 2 px grid: a boss's climax, the dark wizard's fall, the defeat's
	/// crash zoom. Replaces any burst still drawing.
	/// </summary>
	public void BurstSpeedLines(Vector2 centre, float inner, float outer, int streaks, float seconds, Color near, Color far,
		uint seed, SpeedLines.Motion motion)
	{
		EnsureImpactLayer();
		// The layer sits at the run's origin, so its local space is the world's and the grid is the world's grid.
		impactLines.Burst(impactLayer!.ToLocal(centre), inner, outer, streaks, seconds, near, far, 2f, seed, motion);
		impactLayer.QueueRedraw();
	}

	/// <summary>
	/// A dithered flash blooming out of <paramref name="worldSource"/> over the screen and the HUD (DitherFlash):
	/// opaque <paramref name="hot"/> then <paramref name="edge"/> pixels through the Bayer matrix, up to
	/// <paramref name="density"/> of them at the source, thinning to none <paramref name="reach"/> canvas units out
	/// and over <paramref name="seconds"/>. Only for an impact that calls for it (the dark wizard's fall).
	/// </summary>
	public void FlashScreen(Color hot, Color edge, float density, float seconds, Vector2 worldSource, float reach)
	{
		EnsureLetterbox();
		// The letterbox's layer draws in the viewport's canvas units; the camera's transform takes the world there.
		Vector2 source = GetViewport().GetCanvasTransform() * worldSource;
		screenFlash.Flash(hot, edge, density, seconds, source, reach);
		letterbox?.QueueRedraw();
	}

	private void EnsureImpactLayer()
	{
		if (impactLayer != null && IsInstanceValid(impactLayer))
			return;
		impactLayer = new Node2D
		{
			Name = "ImpactLines",
			ZAsRelative = false,
			ZIndex = DrawLayers.ForemostAir,
			// Through the defeat's paused tree too: its crash zoom draws here.
			ProcessMode = ProcessModeEnum.Always,
		};
		impactLayer.Draw += DrawImpactLines;
		AddChild(impactLayer);
	}

	// From the arrival's tick and from FilmDefeat: the lines' clock and the flash's, and a redraw while either shows.
	private void TickImpactBeats(float delta)
	{
		if (impactLayer != null && IsInstanceValid(impactLayer) && (impactLines.Active || impactDrawn))
		{
			impactLines.Tick(delta);
			impactLayer.QueueRedraw();
		}
		if (screenFlash.Active)
		{
			screenFlash.Tick(delta);
			letterbox?.QueueRedraw();
		}
	}

	private void DrawImpactLines()
	{
		impactDrawn = impactLines.Active;
		if (impactLayer != null)
			impactLines.Draw(impactLayer);
	}

	private static uint SeedOf(string id)
	{
		uint seed = 2166136261u;
		foreach (char c in id ?? string.Empty)
			seed = (seed ^ c) * 16777619u;
		return seed;
	}

	// From SpawnBoss, on the frame the boss is added: the spells stop that frame, not the next.
	private void BeginBossEntranceHold()
	{
		if (bossEntranceHeld || BossEnemy.EnteringCount <= 0)
			return;
		bossEntranceHeld = true;
		bossEntranceHeldSeconds = 0f;
		SetEntranceBeat(EntranceBeat.Arriving);
		player?.SetHeldForBossEntrance(true);
		HoldOrdinaryBodies();
		FreezePlayerSpells(true);
		EnsureLetterbox();
	}

	// Every frame from ProcessTick. Walks the hold's four beats (the header) and lets go after the last.
	private void TickBossEntrance(float delta)
	{
		if (bossEntranceHeld)
		{
			bossEntranceHeldSeconds += delta;
			entranceBeatSeconds += delta;
			// Anything that arrives while the field is held (the Choir's two echoes come a frame after
			// the first voice) is held with it.
			HoldOrdinaryBodies();
			if (runFinished)
				EndBossEntranceHold();
			else
				TickEntranceBeat();
		}
		TickBossEntranceCamera(delta);
		TickLetterbox(delta);
		TickImpactBeats(delta);
		if (entranceCamera != null && IsInstanceValid(entranceCamera))
			screenShake.Tick(entranceCamera, delta);
	}

	private void SetEntranceBeat(EntranceBeat beat)
	{
		entranceBeat = beat;
		entranceBeatSeconds = 0f;
	}

	private void TickEntranceBeat()
	{
		// A body arriving while the camera goes back (a wave sent on the heels of the last) is the arrival again.
		if (BossEnemy.EnteringCount > 0 && entranceBeat != EntranceBeat.Arriving)
			SetEntranceBeat(EntranceBeat.Arriving);
		bool noCamera = entranceCamera == null || !IsInstanceValid(entranceCamera);
		switch (entranceBeat)
		{
			case EntranceBeat.Arriving:
				if (entranceBeatSeconds > MaxBossEntranceHoldSeconds)
				{
					GD.PushWarning($"Node2DGame: a boss arrival held the field for {entranceBeatSeconds:0.0}s; letting it go.");
					EndBossEntranceHold();
				}
				else if (BossEnemy.EnteringCount <= 0)
					SetEntranceBeat(EntranceBeat.Landed);
				break;
			case EntranceBeat.Landed:
				if (entranceBeatSeconds >= EntranceLandedHold && (cameraRestSeconds >= EntranceRestHold || noCamera))
					SetEntranceBeat(EntranceBeat.Returning);
				else if (OverdueBeat())
					SetEntranceBeat(EntranceBeat.Returning);
				break;
			case EntranceBeat.Returning:
				if (cameraHome || noCamera)
					SetEntranceBeat(EntranceBeat.Ready);
				else if (OverdueBeat())
					SetEntranceBeat(EntranceBeat.Ready);
				break;
			case EntranceBeat.Ready:
				if (entranceBeatSeconds >= EntrancePlayHold)
					EndBossEntranceHold();
				break;
		}
	}

	private bool OverdueBeat()
	{
		if (entranceBeatSeconds <= MaxEntranceBeatSeconds)
			return false;
		GD.PushWarning($"Node2DGame: the arrival's camera never settled in its {entranceBeat} beat ({entranceBeatSeconds:0.0}s); moving on.");
		return true;
	}

	private void EndBossEntranceHold()
	{
		bossEntranceHeld = false;
		// Every landed body's fight begins on the frame the field is let go, never before it.
		BossEnemy.LetLandedBodiesFight();
		if (player != null && IsInstanceValid(player))
			player.SetHeldForBossEntrance(false);
		foreach (Enemy body in heldForBossEntrance)
		{
			if (body != null && IsInstanceValid(body))
				body.SetHeld(false);
		}
		heldForBossEntrance.Clear();
		FreezePlayerSpells(false);
	}

	// What the player's spells already put in the world stands still for the arrival (Player.SpellGroup):
	// holding the fire loop stopped new casts, but the Familiar kept biting, the Spiritual Weapon kept
	// swinging and bolts already out kept flying (Eric, 2026-11-02: "I'm still seeing spells firing on
	// cutscenes"). Disabled, not freed: each takes up again exactly where it stood, at the mode it had.
	private readonly List<(Node Node, ProcessModeEnum Mode)> frozenSpells = new();

	private void FreezePlayerSpells(bool freeze)
	{
		if (freeze)
		{
			foreach (Node node in GetTree().GetNodesInGroup(Player.SpellGroup))
			{
				if (!IsInstanceValid(node) || node.ProcessMode == ProcessModeEnum.Disabled)
					continue;
				frozenSpells.Add((node, node.ProcessMode));
				node.ProcessMode = ProcessModeEnum.Disabled;
			}
			return;
		}
		foreach ((Node node, ProcessModeEnum mode) in frozenSpells)
		{
			if (IsInstanceValid(node))
				node.ProcessMode = mode;
		}
		frozenSpells.Clear();
	}

	private void HoldOrdinaryBodies()
	{
		foreach (Node node in EnemyRegistry.Snapshot)
		{
			// An arriving boss is out of the registry already; one that has landed (a Boss Rush never
			// sends two at once, but be plain about it) fights.
			if (node is not Enemy body || body is BossEnemy || body.IsHeld || !IsInstanceValid(body))
				continue;
			body.SetHeld(true);
			heldForBossEntrance.Add(body);
		}
	}

	// --- The camera ----------------------------------------------------------------------------------

	// The camera is the player's child (player.tscn), so leaning it is moving it off his centre and
	// easing it back. Position for the lean and Zoom for the push-in; never Offset, which is the shake's.
	// A chapter may hold a lean of its own (hook 9, ChapterRules.CameraLean - the bridge's, up to the
	// Constable on the wall). Out of an arrival that is the camera's whole lean, eased the same way; through
	// one the cinematic owns the camera, and eases out to the chapter's lean rather than to the centre.
	private void TickBossEntranceCamera(float delta)
	{
		if (player == null || !IsInstanceValid(player))
			return;
		// Hook 9: a chapter's lean of the camera.
		Vector2 chapterLean = chapterRules?.CameraLean(player) ?? Vector2.Zero;
		if (!bossEntranceHeld && !cinematicCameraActive && chapterLean == Vector2.Zero && !chapterLeanMoved)
			return;
		entranceCamera ??= player.GetNodeOrNull<Camera2D>(CameraPath);
		if (entranceCamera == null)
			return;
		if (!bossEntranceHeld && !cinematicCameraActive)
		{
			TickChapterCameraLean(chapterLean, delta);
			return;
		}
		if (!cinematicCameraActive)
		{
			// The zoom the run was at when the arrival began: what the cinematic pushes in from and
			// hands back exactly.
			cinematicCameraActive = true;
			cinematicBaseZoom = entranceCamera.Zoom;
			cinematicZoom = 1f;
			cinematicZoomVelocity = 0f;
			cinematicPanVelocity = Vector2.Zero;
			cinematicPanBegun = false;
			cameraRestSeconds = 0f;
		}

		// On the arrival through ARRIVING and LANDED; back to the wizard (or the chapter's lean) after.
		bool onArrival = bossEntranceHeld && entranceBeat is EntranceBeat.Arriving or EntranceBeat.Landed;
		Vector2 want = chapterLean;
		float wantZoom = 1f;
		if (onArrival && TryArrivalFocus(out Vector2 focus))
		{
			want = (focus - player.GlobalPosition) * EntranceCameraShare;
			if (want.Length() > EntranceCameraLean)
				want = want.Normalized() * EntranceCameraLean;
			wantZoom = FitZoomFactor(want);
		}
		// The pan waits out the bars (BossEntrancePanDelay), then eases on its capped spring; the zoom pushes in from
		// the start, on its own.
		Vector2 now = entranceCamera.Position;
		if (!onArrival || bossEntranceHeldSeconds >= BossEntrancePanDelay)
		{
			StepPan(ref now, ref cinematicPanVelocity, want, delta);
			cinematicPanBegun |= onArrival;
		}
		else
		{
			cinematicPanVelocity = Vector2.Zero;
		}
		StepSpring(ref cinematicZoom, ref cinematicZoomVelocity, wantZoom, CinematicZoomStiffness, CinematicZoomMaxRate,
			CinematicZoomMaxAccel, delta);
		cinematicZoomPunch *= Mathf.Exp(-9f * delta);

		float speed = cinematicPanVelocity.Length();
		bool atRest = now.DistanceTo(want) < CameraRestPx && speed < CameraRestSpeed;
		cameraRestSeconds = onArrival && cinematicPanBegun && atRest ? cameraRestSeconds + delta : 0f;
		cameraHome = !onArrival && now.DistanceTo(chapterLean) < CameraHomePx && speed < CameraHomeSpeed
			&& Mathf.Abs(cinematicZoom - 1f) < CameraHomeZoom;

		if (!bossEntranceHeld && now.DistanceTo(chapterLean) < 0.5f && speed < 2f
			&& Mathf.Abs(cinematicZoom - 1f) < 0.002f && cinematicZoomPunch < 0.001f)
		{
			// At rest: exactly where the run had it (or on the chapter's lean), and the cinematic lets go of
			// the camera.
			entranceCamera.Position = chapterLean;
			entranceCamera.Zoom = cinematicBaseZoom;
			cinematicZoom = 1f;
			cinematicZoomVelocity = 0f;
			cinematicZoomPunch = 0f;
			appliedZoomFactor = 1f;
			cinematicPanVelocity = Vector2.Zero;
			cinematicCameraActive = false;
			chapterLeanMoved = chapterLean != Vector2.Zero;
			return;
		}
		entranceCamera.Position = now;
		appliedZoomFactor = cinematicZoom + cinematicZoomPunch;
		entranceCamera.Zoom = cinematicBaseZoom * appliedZoomFactor;
	}

	/// <summary>
	/// One frame of the arrival's pan: a critically damped spring toward <paramref name="want"/>, its acceleration
	/// and speed capped (BossEntrancePanMaxAccel, BossEntrancePanMaxSpeed) so it eases in, never outruns the eye, and
	/// settles without overshoot. Stepped in sixtieths, and never more than a tenth of a second a frame: one long
	/// frame (a hitch as a boss's phase builds) taken as a single step flung the camera 300 px. Static, so
	/// RegressionChecks walks the same pan the camera does.
	/// </summary>
	internal static void StepPan(ref Vector2 now, ref Vector2 velocity, Vector2 want, float delta)
	{
		float k = BossEntrancePanStiffness;
		for (float left = Mathf.Min(delta, 0.1f); left > 0.00001f; left -= 1f / 60f)
		{
			float h = Mathf.Min(left, 1f / 60f);
			Vector2 accel = ((want - now) * (k * k) - velocity * (2f * k)).LimitLength(BossEntrancePanMaxAccel);
			velocity = (velocity + accel * h).LimitLength(BossEntrancePanMaxSpeed);
			now += velocity * h;
		}
	}

	// The same capped spring on one number: the push-in's zoom factor.
	private static void StepSpring(ref float now, ref float velocity, float want, float k, float maxRate, float maxAccel, float delta)
	{
		for (float left = Mathf.Min(delta, 0.1f); left > 0.00001f; left -= 1f / 60f)
		{
			float h = Mathf.Min(left, 1f / 60f);
			float accel = Mathf.Clamp((want - now) * (k * k) - velocity * (2f * k), -maxAccel, maxAccel);
			velocity = Mathf.Clamp(velocity + accel * h, -maxRate, maxRate);
			now += velocity * h;
		}
	}

	// Hook 9 out of an arrival: the camera's Position eased to the chapter's lean and back to the centre,
	// at the arrival's own ease. The zoom is never touched here.
	private bool chapterLeanMoved;

	private void TickChapterCameraLean(Vector2 lean, float delta)
	{
		Vector2 now = entranceCamera!.Position.Lerp(lean, 1f - Mathf.Exp(-BossEntranceCameraEase * delta));
		if (lean == Vector2.Zero && now.Length() < 0.5f)
		{
			entranceCamera.Position = Vector2.Zero;
			chapterLeanMoved = false;
			return;
		}
		entranceCamera.Position = now;
		chapterLeanMoved = true;
	}

	// The middle of every body still arriving; the chapter's boss if the count names none (a tool's).
	private bool TryArrivalFocus(out Vector2 focus)
	{
		focus = Vector2.Zero;
		int n = 0;
		foreach (BossEnemy body in BossEnemy.Arriving)
		{
			if (body == null || !IsInstanceValid(body))
				continue;
			focus += body.GlobalPosition;
			n++;
		}
		if (n == 0 && bossInstance != null && IsInstanceValid(bossInstance))
		{
			focus = bossInstance.GlobalPosition;
			n = 1;
		}
		if (n == 0)
			return false;
		focus /= n;
		return true;
	}

	// How far in the camera may push with its centre leaned by <lean>: every arriving body, with
	// ArrivalFrameMargin round it, and the wizard, with WizardFrameMargin, inside the picture BETWEEN the
	// bars. Never in past CinematicMaxZoom; out past the run's own zoom only as far as it takes to keep
	// every body in the picture, and never past CinematicMinZoom.
	private float FitZoomFactor(Vector2 lean)
	{
		Vector2 canvas = GetViewport().GetVisibleRect().Size;
		var (top, bottom) = LetterboxBarHeights(canvas);
		var half = new Vector2(canvas.X * 0.5f, (canvas.Y - top - bottom) * 0.5f);
		float baseZoom = Mathf.Max(0.01f, cinematicBaseZoom.X);
		Vector2 centre = player!.GlobalPosition + lean;
		float fit = FitFor(player.GlobalPosition - centre, WizardFrameMargin, half, baseZoom);
		foreach (BossEnemy body in BossEnemy.Arriving)
		{
			if (body != null && IsInstanceValid(body))
				fit = Mathf.Min(fit, FitFor(body.GlobalPosition - centre, ArrivalFrameMargin, half, baseZoom));
		}
		return Mathf.Clamp(fit, CinematicMinZoom, CinematicMaxZoom);
	}

	// The zoom factor at which <offset> plus <margin> just fits in <half> canvas units each way.
	private static float FitFor(Vector2 offset, Vector2 margin, Vector2 half, float baseZoom)
	{
		float zx = half.X / Mathf.Max(1f, Mathf.Abs(offset.X) + margin.X);
		float zy = half.Y / Mathf.Max(1f, Mathf.Abs(offset.Y) + margin.Y);
		return Mathf.Min(zx, zy) / baseZoom;
	}

	// --- The letterbox -------------------------------------------------------------------------------

	private void EnsureLetterbox()
	{
		if (letterbox != null && IsInstanceValid(letterbox))
			return;
		letterboxLayer = new CanvasLayer { Name = "BossLetterbox", Layer = LetterboxLayer };
		letterbox = new Control
		{
			Name = "Bars",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			FocusMode = Control.FocusModeEnum.None,
			TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};
		// Edge to edge, the phone's camera hole included: a full-bleed backdrop, so SafeArea is not applied.
		letterbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		letterbox.Draw += DrawLetterbox;
		letterboxLayer.AddChild(letterbox);
		AddChild(letterboxLayer);
	}

	private void TickLetterbox(float delta)
	{
		if (letterbox == null || !IsInstanceValid(letterbox))
			return;
		float was = letterboxShown;
		// The defeat films its fall between the same bars (Node2DGame.Defeat.cs). An arrival's bars go out as its
		// READY beat begins - the camera home - so the field is seen whole for a moment before it is let go.
		bool wanted = (bossEntranceHeld && entranceBeat != EntranceBeat.Ready) || defeatFilming;
		letterboxShown = wanted
			? Mathf.Min(1f, letterboxShown + delta / LetterboxInSeconds)
			: Mathf.Max(0f, letterboxShown - delta / LetterboxOutSeconds);
		letterboxJolt *= Mathf.Exp(-14f * delta);
		if (letterboxJolt < 0.5f)
			letterboxJolt = 0f;
		if (flareAge >= 0f)
		{
			flareAge += delta;
			if (flareAge > FlareSeconds)
				flareAge = -1f;
		}
		bool showing = letterboxShown > 0f || flareAge >= 0f || screenFlash.Active;
		letterbox.Visible = showing;
		if (showing || was > 0f)
			letterbox.QueueRedraw();
	}

	/// <summary>
	/// The canvas y a banner must start below to be clear of the top bar while an arrival is filmed: the bar
	/// at full height, its deepest jolt, and the climax's spill into the picture. 0 when nothing is filmed.
	/// ShowRunBanner reads it, so the boss's name is never under the bar it is announced beneath.
	/// </summary>
	private float LetterboxBannerFloor()
	{
		if (!bossEntranceHeld && letterboxShown <= 0f)
			return 0f;
		var (top, _) = LetterboxBarHeights(GetViewport().GetVisibleRect().Size);
		return top + MaxBarJolt + FlareDepth + 6f;
	}

	// Each bar at full height, in canvas units: a share of the canvas, plus the safe inset on its own edge
	// on a phone (the camera hole and the rounded corners), so the bar seen below the hole matches the other.
	private (float Top, float Bottom) LetterboxBarHeights(Vector2 canvas)
	{
		var insets = SafeArea.Insets(GetViewport());
		float share = Mathf.Round(canvas.Y * (canvas.Y > canvas.X ? LetterboxSharePortrait : LetterboxShare) * 0.5f) * 2f;
		return (share + Mathf.Round(insets.Top), share + Mathf.Round(insets.Bottom));
	}

	private void DrawLetterbox()
	{
		if (letterbox == null)
			return;
		Vector2 canvas = letterbox.Size;
		if (canvas.X <= 0f || canvas.Y <= 0f)
			return;
		// Under the bars, so a flash over a filmed beat never lights them.
		screenFlash.Draw(letterbox, new Rect2(Vector2.Zero, canvas));
		var (fullTop, fullBottom) = LetterboxBarHeights(canvas);
		// In fast and settling, out by falling away: a smoothstep on whole pixels.
		float u = letterboxShown * letterboxShown * (3f - 2f * letterboxShown);
		float jolt = Mathf.Round(letterboxJolt);
		float top = Mathf.Round(fullTop * u) + (u > 0f ? jolt : 0f);
		float bottom = Mathf.Round(fullBottom * u) + (u > 0f ? jolt : 0f);
		if (top > 0f)
			letterbox.DrawRect(new Rect2(0f, 0f, canvas.X, top), LetterboxColor);
		if (bottom > 0f)
			letterbox.DrawRect(new Rect2(0f, canvas.Y - bottom, canvas.X, bottom), LetterboxColor);
		if (flareAge >= 0f && u > 0f)
			DrawFlare(canvas, top, bottom);
	}

	// The climax's light caught on the bars' inner edges: a line stepping down the element's ramp (core,
	// hot, mid, edge; 4 px while it is hot, 2 px after) and a spill of four 6 px strips into the picture,
	// each a step thinner through the Bayer matrix, the whole of it thinning out as the flare ages. Every
	// pixel drawn is an opaque stop of the ramp.
	private void DrawFlare(Vector2 canvas, float top, float bottom)
	{
		float k = flareAge / FlareSeconds;
		Color line = k < 0.15f ? flareRamp.Core : k < 0.35f ? flareRamp.Hot : k < 0.65f ? flareRamp.Mid : flareRamp.Edge;
		float thick = k < 0.35f ? 4f : 2f;
		letterbox!.DrawRect(new Rect2(0f, top, canvas.X, thick), line);
		letterbox.DrawRect(new Rect2(0f, canvas.Y - bottom - thick, canvas.X, thick), line);

		Texture2D[] tiles = BayerTiles();
		for (int i = 0; i < FlareSpill.Length; i++)
		{
			float density = Mathf.Clamp(0.95f - 0.22f * i - 1.0f * k, 0f, 1f);
			int level = Mathf.Clamp(Mathf.RoundToInt(density * 16f), 0, 16);
			if (level == 0)
				continue;
			Color stop = FlareSpill[i] switch { 0 => flareRamp.Hot, 1 => flareRamp.Mid, _ => flareRamp.Edge };
			float into = thick + FlareStrip * i;
			letterbox.DrawTextureRect(tiles[level], new Rect2(0f, top + into, canvas.X, FlareStrip), true, stop);
			letterbox.DrawTextureRect(tiles[level], new Rect2(0f, canvas.Y - bottom - into - FlareStrip, canvas.X, FlareStrip), true, stop);
		}
	}

	// The spill's strips, outward from the edge, by ramp stop (0 hot, 1 mid, 2 edge), and their height.
	private static readonly int[] FlareSpill = { 0, 1, 1, 2 };
	private const float FlareStrip = 6f;
	/// <summary>How far the flare reaches into the picture past a bar's edge, line and spill.</summary>
	private static float FlareDepth => 4f + FlareSpill.Length * FlareStrip;

	private static Texture2D[] BayerTiles() => bayerTiles ??= DitherFlash.BuildBayerTiles();
}
