using Godot;
using System.Collections.Generic;
using WizardSurvivors.scripts;

// A BOSS'S ARRIVAL, the run's half (2026-10-31: "whenever a boss spawns, all of the players spells
// should stop firing, and you should see an animation of the boss spawning"). The boss plays its own
// entrance (BossEnemy, on the BossDeath stage) and is untargetable and still while it does; this holds
// everything else for as long as any body of it is arriving (BossEnemy.EnteringCount):
//
//   - the player's spells are silent, their timers HELD rather than reset (Player.HeldForBossEntrance,
//     read inside the one spellFireTimers loop), and nothing can hurt him;
//   - every ordinary body on the field is HELD where it stands (Enemy.SetHeld): no step, no wind-up,
//     no shot, and no damage taken. Silencing the spells and leaving the swarm walking would hand it
//     a free hit the moment the fight began; holding it means the fight starts exactly where the field
//     stood when the boss appeared;
//   - the camera leans toward the boss, so the arrival is seen rather than heard off screen, and comes
//     back to the wizard as the fight starts.
//
// Real seconds throughout: the arrival is how a moment feels, not a position on the run.
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

	private bool bossEntranceHeld;
	private float bossEntranceHeldSeconds;
	private readonly List<Enemy> heldForBossEntrance = new();
	private Camera2D? entranceCamera;
	private bool entranceCameraMoved;

	/// <summary>True while the field is held for a boss's arrival. For _BossProbe and _UiShot.</summary>
	public bool BossEntranceHeld => bossEntranceHeld;

	// From SpawnBoss, on the frame the boss is added: the spells stop that frame, not the next.
	private void BeginBossEntranceHold()
	{
		if (bossEntranceHeld || BossEnemy.EnteringCount <= 0)
			return;
		bossEntranceHeld = true;
		bossEntranceHeldSeconds = 0f;
		player?.SetHeldForBossEntrance(true);
		HoldOrdinaryBodies();
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

	// The camera is the player's child (player.tscn), so leaning it is moving it off his centre and
	// easing it back. Position, not Offset: Offset is the screen shake's (SpiritualWeapon). The same ease
	// carries a chapter's own lean (hook 8, ChapterRules.CameraLean - the bridge's, toward the Constable on
	// the wall), which an arriving boss's lean overrides while the field is held.
	private void TickBossEntranceCamera(float delta)
	{
		if (player == null || !IsInstanceValid(player))
			return;
		// Hook 8: a chapter's lean of the camera.
		Vector2 chapterLean = chapterRules?.CameraLean(player) ?? Vector2.Zero;
		if (!bossEntranceHeld && !entranceCameraMoved && chapterLean == Vector2.Zero)
			return;
		entranceCamera ??= player.GetNodeOrNull<Camera2D>(CameraPath);
		if (entranceCamera == null)
			return;

		Vector2 want = chapterLean;
		if (bossEntranceHeld && bossInstance != null && IsInstanceValid(bossInstance))
		{
			want = (bossInstance.GlobalPosition - player.GlobalPosition) * BossEntranceCameraShare;
			if (want.Length() > BossEntranceCameraLean)
				want = want.Normalized() * BossEntranceCameraLean;
		}
		Vector2 now = entranceCamera.Position;
		now = now.Lerp(want, 1f - Mathf.Exp(-BossEntranceCameraEase * delta));
		if (!bossEntranceHeld && chapterLean == Vector2.Zero && now.Length() < 0.5f)
		{
			now = Vector2.Zero;
			entranceCameraMoved = false;
		}
		else
			entranceCameraMoved = true;
		entranceCamera.Position = now;
	}
}
