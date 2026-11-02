using Godot;
using System.Collections.Generic;
using WizardSurvivors.scripts;

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
public partial class Node2DGame
{
	/// <summary>The longest any hold may last, whatever the count says: a body freed oddly must not hold the run.</summary>
	private const float MaxBossEntranceHoldSeconds = 6f;

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

	/// <summary>Real seconds the camera holds still once a hold begins: the bars' closing and a beat.</summary>
	private const float BossEntrancePanDelay = LetterboxInSeconds + 0.2f;

	/// <summary>The pan's spring (1/s): critically damped; about 1.1 s to settle on the boss.</summary>
	private const float BossEntrancePanStiffness = 3.6f;

	// --- The cinematic's tuning ----------------------------------------------------------------------

	/// <summary>
	/// Each bar's height, as a share of the canvas height (plus the phone's safe inset on its edge): about a
	/// 2.35:1 picture on a 16:9 screen, and a little less of the tall phone canvas, where the same share
	/// would take a third of the screen.
	/// </summary>
	private const float LetterboxShare = 0.11f;
	private const float LetterboxSharePortrait = 0.09f;
	private const float LetterboxInSeconds = 0.45f;
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
	private const float ZoomInEase = 1.4f;
	private const float ZoomOutEase = 4.5f;

	/// <summary>World pixels a weight-1 beat shakes the screen; a climax shakes a little harder.</summary>
	private const float ImpactShakePx = 6f;
	private const float ClimaxShakePx = 8f;
	/// <summary>Canvas pixels the bars jump in at a weight-1 climax, and the most they ever jump.</summary>
	private const float ClimaxBarJolt = 8f;
	private const float MaxBarJolt = 12f;
	/// <summary>The push-in's kick at a weight-1 climax, on the zoom factor.</summary>
	private const float ClimaxZoomPunch = 0.05f;
	private const float FlareSeconds = 0.34f;

	private bool bossEntranceHeld;
	private float bossEntranceHeldSeconds;
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
	private static readonly int[] Bayer4 = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

	/// <summary>True while the field is held for a boss's arrival. For _BossProbe and _UiShot.</summary>
	public bool BossEntranceHeld => bossEntranceHeld;

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
	/// An arrival's climax (BossDeath, on the roar): the shake at the boss's weight, and while the bars are
	/// in, the jolt, the kick of the push-in and the flare in <paramref name="element"/>'s light.
	/// </summary>
	public void OnBossClimax(BossEnemy boss, float weight, Element element)
	{
		if (weight > 0f)
			ShakeScreen(ClimaxShakePx * weight, 0.35f + 0.35f * weight);
		if (!bossEntranceHeld && letterboxShown <= 0f)
			return;
		float w = Mathf.Max(0.3f, weight);
		letterboxJolt = Mathf.Clamp(ClimaxBarJolt * w, letterboxJolt, MaxBarJolt);
		cinematicZoomPunch = Mathf.Max(cinematicZoomPunch, ClimaxZoomPunch * w);
		flareRamp = ElementColors.GetRamp(element);
		flareAge = 0f;
	}

	// From SpawnBoss, on the frame the boss is added: the spells stop that frame, not the next.
	private void BeginBossEntranceHold()
	{
		if (bossEntranceHeld || BossEnemy.EnteringCount <= 0)
			return;
		bossEntranceHeld = true;
		bossEntranceHeldSeconds = 0f;
		player?.SetHeldForBossEntrance(true);
		HoldOrdinaryBodies();
		EnsureLetterbox();
	}

	// Every frame from ProcessTick. Lets go when the last arriving body has landed.
	private void TickBossEntrance(float delta)
	{
		if (bossEntranceHeld)
		{
			bossEntranceHeldSeconds += delta;
			// Anything that arrives while the field is held (the Choir's two echoes come a frame after
			// the first voice) is held with it.
			HoldOrdinaryBodies();
			bool overdue = bossEntranceHeldSeconds > MaxBossEntranceHoldSeconds;
			if (overdue)
				GD.PushWarning($"Node2DGame: a boss arrival held the field for {bossEntranceHeldSeconds:0.0}s; letting it go.");
			if (BossEnemy.EnteringCount <= 0 || overdue || runFinished)
				EndBossEntranceHold();
		}
		TickBossEntranceCamera(delta);
		TickLetterbox(delta);
		if (entranceCamera != null && IsInstanceValid(entranceCamera))
			screenShake.Tick(entranceCamera, delta);
	}

	private void EndBossEntranceHold()
	{
		bossEntranceHeld = false;
		if (player != null && IsInstanceValid(player))
			player.SetHeldForBossEntrance(false);
		foreach (Enemy body in heldForBossEntrance)
		{
			if (body != null && IsInstanceValid(body))
				body.SetHeld(false);
		}
		heldForBossEntrance.Clear();
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
			cinematicPanVelocity = Vector2.Zero;
		}

		Vector2 want = chapterLean;
		float wantZoom = 1f;
		if (bossEntranceHeld && TryArrivalFocus(out Vector2 focus))
		{
			want = (focus - player.GlobalPosition) * BossEntranceCameraShare;
			if (want.Length() > BossEntranceCameraLean)
				want = want.Normalized() * BossEntranceCameraLean;
			wantZoom = FitZoomFactor(want);
		}
		// The pan waits out the bars (BossEntrancePanDelay), then springs; the zoom pushes in from the start.
		Vector2 now = entranceCamera.Position;
		if (!bossEntranceHeld || bossEntranceHeldSeconds >= BossEntrancePanDelay)
		{
			// Stepped in sixtieths, and never more than a tenth of a second a frame: one long frame (a hitch
			// as a boss's phase builds) taken as a single step flung the camera 300 px in a frame.
			float k = BossEntrancePanStiffness;
			for (float left = Mathf.Min(delta, 0.1f); left > 0f; left -= 1f / 60f)
			{
				float h = Mathf.Min(left, 1f / 60f);
				Vector2 accel = (want - now) * (k * k) - cinematicPanVelocity * (2f * k);
				cinematicPanVelocity += accel * h;
				now += cinematicPanVelocity * h;
			}
		}
		else
		{
			cinematicPanVelocity = Vector2.Zero;
		}
		cinematicZoom = Mathf.Lerp(cinematicZoom, wantZoom, 1f - Mathf.Exp(-(bossEntranceHeld ? ZoomInEase : ZoomOutEase) * delta));
		cinematicZoomPunch *= Mathf.Exp(-9f * delta);

		if (!bossEntranceHeld && now.DistanceTo(chapterLean) < 0.5f && cinematicPanVelocity.Length() < 2f
			&& Mathf.Abs(cinematicZoom - 1f) < 0.002f && cinematicZoomPunch < 0.001f)
		{
			// At rest: exactly where the run had it (or on the chapter's lean), and the cinematic lets go of
			// the camera.
			entranceCamera.Position = chapterLean;
			entranceCamera.Zoom = cinematicBaseZoom;
			cinematicZoom = 1f;
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
		// The defeat films its fall between the same bars (Node2DGame.Defeat.cs).
		letterboxShown = bossEntranceHeld || defeatFilming
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
		bool showing = letterboxShown > 0f || flareAge >= 0f;
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

	private static Texture2D[] BayerTiles()
	{
		if (bayerTiles != null)
			return bayerTiles;
		bayerTiles = new Texture2D[17];
		for (int level = 0; level <= 16; level++)
		{
			Image image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8);
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					bool lit = Bayer4[(y / 2) % 4 * 4 + (x / 2) % 4] < level;
					image.SetPixel(x, y, lit ? Colors.White : new Color(1f, 1f, 1f, 0f));
				}
			}
			bayerTiles[level] = ImageTexture.CreateFromImage(image);
		}
		return bayerTiles;
	}
}
