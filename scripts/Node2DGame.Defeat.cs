using Godot;
using TheOneHeMissed.scripts;

// THE DEFEAT, the run's half (2026-11-02, Eric: "Defeat should have a dramatic animation where the game
// freezes, then crash zooms onto the wizard and they fall, then it says defeat, and when the player
// clicks, the defeat screen covers up."). DefeatSequence is the timeline and the card; this is what it
// needs from the run - the camera, the letterbox bars, the shake and the ground - all of which a boss's
// arrival already owns (Node2DGame.BossEntrance.cs). The defeat drives that machinery; it is not a
// second camera system.
//
// THE FREEZE IS THE PAUSED TREE. FinishRunAndReward pays, saves and pauses exactly as it always did, so
// every enemy, bolt, spell, particle and timer stops where it stood on the frame of the killing blow.
// What still moves is what processes through a pause: the sequence, the run camera (set so here, for the
// push-in and the shake), the wizard's body sprite (Player.TakeDamage already lets it run, for his death
// row) and the sounds. ProcessTick, which ticks the arrival's camera and bars in a run, is stopped by
// runFinished, so the sequence ticks them itself through FilmDefeat.
//
// Only the death that ends the run comes here: the extra life's heal never reaches FinishRunAndReward,
// and a death after the boss has fallen ends the run as the victory it is (OnPlayerDied).
public partial class Node2DGame
{
	private DefeatSequence? defeatSequence;
	// True while the defeat wants the bars in; TickLetterbox reads it beside bossEntranceHeld.
	private bool defeatFilming;
	// The run camera's own zoom (0.85), without any arrival's push-in: what the close-up is measured from.
	private float defeatBaseZoom = 1f;

	/// <summary>The defeat being played, or null. For _UiShot and _TouchProbe.</summary>
	public DefeatSequence? Defeat => defeatSequence;

	/// <summary>The run camera's zoom without any push-in, which the defeat's close-up is a factor on.</summary>
	internal float DefeatBaseZoom => defeatBaseZoom;

	// From FinishRunAndReward, on the frame of the killing blow, after the save and the pause. False when
	// there is nothing to film (no wizard, no camera): the results then come up at once, as they used to.
	private bool BeginDefeatSequence(RunReport report)
	{
		if (player == null || !IsInstanceValid(player) || !player.IsDead)
			return false;
		entranceCamera ??= player.GetNodeOrNull<Camera2D>(CameraPath);
		if (entranceCamera == null)
			return false;

		entranceCamera.ProcessMode = ProcessModeEnum.Always;
		defeatBaseZoom = GameplayZoomOf(entranceCamera);
		EnsureLetterbox();
		defeatSequence = new DefeatSequence
		{
			Name = "DefeatSequence",
			Run = this,
			Wizard = player,
			// Wherever an arrival had pushed in or leaned the camera is where the crash zoom starts.
			StartZoomFactor = entranceCamera.Zoom.X / defeatBaseZoom,
			StartLean = entranceCamera.Position,
			OnContinue = () => ShowGameOver(report, afterDefeat: true),
		};
		AddChild(defeatSequence);
		return true;
	}

	/// <summary>
	/// One frame of the defeat's camera, from DefeatSequence: the zoom as a factor on the run's own, the
	/// lean in world px off the wizard (Position, as an arrival leans it; Offset stays the shake's), whether
	/// the bars are wanted, and then the shake, the bars and the tiled ground brought up to match.
	/// </summary>
	internal void FilmDefeat(float zoomFactor, Vector2 lean, bool bars, float delta)
	{
		defeatFilming = bars;
		if (entranceCamera != null && IsInstanceValid(entranceCamera))
		{
			appliedZoomFactor = zoomFactor;
			entranceCamera.Zoom = Vector2.One * defeatBaseZoom * zoomFactor;
			entranceCamera.Position = lean;
			screenShake.Tick(entranceCamera, delta);
		}
		TickLetterbox(delta);
		// The crash zoom's speed lines (DefeatSequence), on the run's impact layer, which draws through the pause.
		TickImpactBeats(delta);
		SyncGroundToCamera();
	}

	/// <summary>
	/// The run's own furniture off the screen for the close-up: every canvas layer the run put over the
	/// field - the HUD (its bars, timer, quest corner and the damage flash that came with the blow), the
	/// touch pad, a chapter note or quest card still up from the opening, a toast - but not the ground
	/// (layer -1), the letterbox or the defeat's own card. The results screen covers it all after.
	/// </summary>
	internal void HideRunHudForDefeat()
	{
		foreach (Node child in GetChildren())
		{
			if (child is CanvasLayer layer && layer.Layer >= 0 && layer != letterboxLayer && layer != defeatSequence)
				layer.Visible = false;
		}
	}
}
