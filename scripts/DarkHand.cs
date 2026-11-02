using Godot;
using System;
using WizardSurvivors.scripts;

// THE HAND OF HIS DARK LIGHT (.ai/endgame-castle.md 6.4, "he takes"). Thrown by ThroneDarkWizardBoss every
// TakingIntervalSeconds after a long tell: a slow hand of his own light that turns toward the wizard and,
// if it reaches him, closes on one of his spells (Player.SealSpell). Dodge it and nothing happens - it
// fizzles out at the end of its life, or on anything that is not the wizard.
//
// SLOWER THAN HE IS, AND IT TURNS SLOWER STILL. The wizard walks at 220; the hand flies at about 170 and
// turns about 80 degrees a second, so a player who sees it coming can always leave it behind, and one who
// waits for it and steps aside at the last moment beats its turn. What makes it land is everything else
// on the floor - his lines in the dark, the wardens' tells in his last phase - which is the point: the
// taking is the threat, the rest of the fight is what pins him in its way.
//
// A bare Node2D created in code, like TelegraphedGroundHit, and for its reasons: nothing to author, and the
// hit is measured against the wizard once a frame rather than left to a collision mask. It never deals
// damage - it takes - so the dodge roll, a shield and the bolt-catching hound do not apply to it.
public partial class DarkHand : Node2D
{
	private ThroneDarkWizardBoss caster;
	private Vector2 heading = Vector2.Right;
	private float speed;
	private float turnRadiansPerSecond;
	private float lifeLeft;
	private float radius;
	private float age;
	private float fading = -1f;
	private const float FadeSeconds = 0.3f;

	// Where it has been, for its trail: a fixed ring of points, written in place.
	private const int TrailLength = 8;
	private readonly Vector2[] trail = new Vector2[TrailLength];
	private int trailHead;
	private float trailClock;

	/// <summary>True once it has landed or gone out: a tool counting hands in flight skips it.</summary>
	public bool Spent => fading >= 0f;

	/// <summary>Throws a hand from <paramref name="from"/> along <paramref name="towards"/> into <paramref name="arena"/>.</summary>
	public static DarkHand Throw(Node arena, ThroneDarkWizardBoss caster, Vector2 from, Vector2 towards,
		float speed, float turnDegreesPerSecond, float lifeSeconds, float radius)
	{
		if (arena == null || !IsInstanceValid(arena))
			return null;
		var hand = new DarkHand
		{
			Name = "DarkHand",
			caster = caster,
			heading = towards.LengthSquared() > 0.0001f ? towards.Normalized() : Vector2.Down,
			speed = speed,
			turnRadiansPerSecond = Mathf.DegToRad(turnDegreesPerSecond),
			lifeLeft = lifeSeconds,
			radius = radius,
			// In the air, over every body (DrawLayers): a thing thrown, which the dark of his second phase
			// lies under (DarkVeil), so it is always seen coming.
			ZAsRelative = false,
			ZIndex = DrawLayers.Air,
		};
		arena.AddChild(hand);
		hand.GlobalPosition = from;
		for (int i = 0; i < TrailLength; i++)
			hand.trail[i] = from;
		return hand;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		age += dt;
		QueueRedraw();

		if (fading >= 0f)
		{
			fading += dt;
			if (fading >= FadeSeconds)
				QueueFree();
			return;
		}

		// His fall takes his light with him: a hand in flight goes out with it.
		if (caster == null || !IsInstanceValid(caster) || caster.IsDying)
		{
			GoOut();
			return;
		}

		Player wizard = FindWizard();
		if (wizard != null && !wizard.IsDead)
		{
			Vector2 to = wizard.GlobalPosition - GlobalPosition;
			if (to.LengthSquared() > 1f)
			{
				float turn = Mathf.Clamp(heading.AngleTo(to), -turnRadiansPerSecond * dt, turnRadiansPerSecond * dt);
				heading = heading.Rotated(turn);
			}
		}
		GlobalPosition += heading * speed * dt;

		trailClock += dt;
		if (trailClock >= 0.04f)
		{
			trailClock = 0f;
			trailHead = (trailHead + 1) % TrailLength;
			trail[trailHead] = GlobalPosition;
		}

		if (wizard != null && !wizard.IsDead && !wizard.HeldForBossEntrance
			&& wizard.GlobalPosition.DistanceTo(GlobalPosition) <= radius)
		{
			caster.OnHandLanded(wizard, GlobalPosition);
			GoOut();
			return;
		}

		lifeLeft -= dt;
		if (lifeLeft <= 0f)
			GoOut();
	}

	private void GoOut()
	{
		if (fading < 0f)
			fading = 0f;
	}

	// Through the group each frame rather than a reference held from the throw: a reference to a node this
	// outlives on a game over is the shape that has twice crashed a finalizer here (TelegraphedGroundHit).
	private Player FindWizard() => GetTree().GetFirstNodeInGroup("player") as Player;

	// A hand of his light on the 2 px art grid: a dark palm with his ramp at its rim, four long fingers
	// reaching ahead and a thumb, the core burning in the fingertips, and wisps of it behind. Drawn in his
	// colours only - the one thing in the world that sheds dark light (.ai/world-and-tone.md).
	public override void _Draw()
	{
		ElementColors.Ramp dark = ElementColors.GetRamp(Element.Darkness);
		float alpha = fading >= 0f ? 1f - Mathf.Clamp(fading / FadeSeconds, 0f, 1f) : Mathf.Clamp(age / 0.15f, 0f, 1f);
		if (alpha <= 0f)
			return;
		// The trail, in this node's space: oldest faintest.
		for (int k = 1; k < TrailLength; k++)
		{
			int index = (trailHead - k + TrailLength) % TrailLength;
			Vector2 at = ToLocal(trail[index]);
			float a = alpha * (1f - k / (float)TrailLength) * 0.7f;
			float size = BossDeath.Px * (k < 3 ? 5f : 3f);
			DrawRect(WhipLimb.Cell(BossDeath.Snap(at), size), new Color(dark.Edge, a));
			if (k < 4)
				DrawRect(WhipLimb.Cell(BossDeath.Snap(at), BossDeath.Px), new Color(dark.Mid, a));
		}

		// Big enough to read over a crowd on a phone: a palm of 34 px, fingers reaching 30 past it.
		float flex = 0.12f * MathF.Sin(age * 9f);
		float pulse = 0.5f + 0.5f * MathF.Sin(age * 7f);
		Vector2 palm = Vector2.Zero;
		BossDeath.Disc(this, palm, 22f, 22f, new Color(dark.Mid, 0.28f * alpha));
		BossDeath.Disc(this, palm, 17f, 17f, new Color(dark.Hot.Lerp(dark.Mid, pulse), alpha));
		BossDeath.Disc(this, palm, 13f, 13f, new Color(0.06f, 0.02f, 0.10f, alpha));
		for (int f = 0; f < 4; f++)
		{
			float spread = (f - 1.5f) * 0.30f + flex * (f % 2 == 0 ? 1f : -1f);
			Vector2 dir = heading.Rotated(spread);
			Vector2 knuckle = palm + dir * 11f;
			Vector2 tip = palm + dir * (26f + (f == 1 || f == 2 ? 6f : 0f));
			BossDeath.Line(this, knuckle, tip, new Color(dark.Hot, alpha), BossDeath.Px * 3f);
			BossDeath.Line(this, knuckle, tip, new Color(0.06f, 0.02f, 0.10f, alpha), BossDeath.Px);
			DrawRect(WhipLimb.Cell(BossDeath.Snap(tip), BossDeath.Px * 3f), new Color(dark.Hot, alpha));
			DrawRect(WhipLimb.Cell(BossDeath.Snap(tip), BossDeath.Px), new Color(dark.Core, alpha));
		}
		Vector2 thumbDir = heading.Rotated(-1.25f - flex);
		BossDeath.Line(this, palm + thumbDir * 10f, palm + thumbDir * 21f, new Color(dark.Hot, alpha), BossDeath.Px * 3f);
		DrawRect(WhipLimb.Cell(BossDeath.Snap(palm + thumbDir * 21f), BossDeath.Px * 2f), new Color(dark.Core, alpha));
		// The leading rim catches his light.
		Vector2 lead = palm + heading * 12f;
		DrawRect(WhipLimb.Cell(BossDeath.Snap(lead), BossDeath.Px * 2f), new Color(dark.Core, alpha));
	}
}
