using Godot;

namespace TheOneHeMissed.scripts;

// THE RUN'S SCREEN SHAKE (2026-11-01, from playing 0.10.0: "make use of screenshake during boss animations
// when helpful"). One per run, owned and ticked by Node2DGame (ShakeScreen), so any number of callers can
// kick it and the strongest kick wins rather than the last.
//
// The run camera's Offset is the shake's and nothing else's. A boss's arrival leans the camera through its
// Position and pushes in through its Zoom (Node2DGame.BossEntrance.cs), so a shake can never leave the
// camera somewhere it was not: when it ends, Offset is zero again. SpiritualWeapon's hammer still writes
// Offset for itself, in the same way (a short kick, then zero) - it predates this and can move onto it.
//
// Mostly up and down, as the hammer's always was (the ground jumps, it does not slide), in whole world
// pixels, and re-aimed every other frame rather than every frame: a jitter at the frame rate reads as noise,
// at half of it as a jolt. Each re-aim flips the vertical sign, so it bounces rather than drifts. The
// strength falls off with the square of what is left, so a kick lands at once and dies away.
//
// Options' "Reduce Screen Shake" (DisplaySettings.ShakeScale) is read here, in Kick, so every caller
// honours it without knowing it exists.
public sealed class ScreenShake
{
	private const float AimSeconds = 1f / 30f;

	private float amplitude;
	private float seconds;
	private float left;
	private float aimLeft;
	private float upDown = 1f;
	private Vector2 aim;
	private bool wrote;
	private readonly RandomNumberGenerator rng = new();

	/// <summary>True while a kick is still shaking.</summary>
	public bool Active => left > 0f;

	/// <summary>How hard it is shaking now, in world pixels.</summary>
	public float Strength => left > 0f && seconds > 0f ? amplitude * (left / seconds) * (left / seconds) : 0f;

	/// <summary>
	/// A kick of <paramref name="kickAmplitude"/> world pixels that dies away over <paramref name="kickSeconds"/>.
	/// The stronger of it and what is still shaking, never the sum: two impacts on one frame are one jolt.
	/// </summary>
	public void Kick(float kickAmplitude, float kickSeconds)
	{
		kickAmplitude *= DisplaySettings.ShakeScale;
		if (kickAmplitude <= 0f || kickSeconds <= 0f || kickAmplitude < Strength)
			return;
		amplitude = kickAmplitude;
		seconds = kickSeconds;
		left = kickSeconds;
		aimLeft = 0f;
	}

	/// <summary>Once a frame: writes the camera's Offset while shaking, and zero once on the frame it ends.</summary>
	public void Tick(Camera2D camera, float delta)
	{
		if (camera == null || !GodotObject.IsInstanceValid(camera))
			return;
		if (!Advance(delta))
		{
			Release(camera);
			return;
		}
		camera.Offset = (aim * Strength).Round();
		wrote = true;
	}

	/// <summary>
	/// Once a frame, for a shake that has no camera (the prologue's panels, Prologue.cs): the offset to draw the
	/// picture at this frame, in the caller's own units, unrounded - the caller snaps it to its own grid. Zero
	/// once the kick has died. Kick, and so Reduce Screen Shake, is the same for both.
	/// </summary>
	public Vector2 Step(float delta) => Advance(delta) ? aim * Strength : Vector2.Zero;

	// The clock and the re-aim, shared by Tick and Step. False once nothing is shaking.
	private bool Advance(float delta)
	{
		if (left <= 0f)
			return false;
		left -= delta;
		if (left <= 0f)
		{
			left = 0f;
			return false;
		}
		aimLeft -= delta;
		if (aimLeft <= 0f)
		{
			aimLeft = AimSeconds;
			upDown = -upDown;
			aim = new Vector2(rng.RandfRange(-0.45f, 0.45f), upDown * rng.RandfRange(0.55f, 1f));
		}
		return true;
	}

	/// <summary>Stops at once and hands the camera back centred.</summary>
	public void Stop(Camera2D camera)
	{
		left = 0f;
		if (camera != null && GodotObject.IsInstanceValid(camera))
			Release(camera);
	}

	private void Release(Camera2D camera)
	{
		if (!wrote)
			return;
		camera.Offset = Vector2.Zero;
		wrote = false;
	}
}
