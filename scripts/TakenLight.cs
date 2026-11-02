using Godot;
using System;
using WizardSurvivors.scripts;

// THE LIGHT, GOING HOME (.ai/dark-wizard-playable.md section 3). The Taking's second verb: what the hand
// tears out of a body rises as a warm mote - the light ramp, not his - and drifts back to the wizard who
// took it, slower than he walks. The boss's SealOrb turned round: there the player chased his own spell
// home before it reached the dark wizard; here the dark wizard meets what he took before it gutters out.
//
//   - It rises out of the body first, untouchable, so a hand closing at his feet cannot be caught the frame
//     it lands.
//   - CAUGHT (his body within CatchRadius), his light crosses the field from where he caught it
//     (TakingLanes), one lane along the way the mote came home.
//   - LEFT, it gutters out at the end of its life and throws nothing. It comes home slowly and gathers
//     speed (DriftSpeed, then Acceleration up to MaxSpeed), so a wizard walking on still meets it within a
//     second or two - the first measured build drifted at a flat 95 px/s and the kiting bot caught none -
//     and only one who runs from it for its whole life loses it. The demand is where he stands when it
//     arrives: the lanes cross there, one of them back along the way it came, through the crowd it left.
//
// IT BURNS ON ITS WAY (Pyre, the Conjunction's second parent): every BurnInterval it sears what stands within
// BurnRadius of it for BurnDamage, so a light coming home through the crowd it was taken from is already
// working before it is caught.
//
// Code-only, like SealOrb and DarkHand: the catch is measured once a frame, and nothing is authored.
public partial class TakenLight : Node2D
{
	/// <summary>What a light carries home, set by Player.FireTheTaking for every light its hands tear out.</summary>
	public struct Setup
	{
		public SpellData Spell;
		public int LaneDamage;
		public float LaneLength;
		public float LaneWidth;
		public bool Star;
		public bool AllHeTook;
		public float DriftSpeed;
		public float Life;
		/// <summary>What it sears on its way home, every <see cref="BurnInterval"/>, and how near.</summary>
		public int BurnDamage;
		public float BurnRadius;
	}

	public const float BurnInterval = 0.3f;

	/// <summary>How close the wizard's body must come to catch it.</summary>
	public const float CatchRadius = 40f;
	/// <summary>The rise out of the body: untouchable until it is over.</summary>
	public const float RiseSeconds = 0.35f;
	private const float RiseHeight = 20f;
	private const float EndSeconds = 0.3f;
	/// <summary>How fast a light gathers speed on its way home, and the most it reaches (he walks at 220).</summary>
	public const float Acceleration = 240f;
	public const float MaxSpeed = 380f;

	/// <summary>
	/// The most lights in the air at once. A ceiling on what All He Took can chain, which a packed lane would
	/// otherwise turn into a light for every body on it; past it a kill on a lane gives nothing.
	/// </summary>
	public const int MaxInAir = 10;

	private static int inAir;

	/// <summary>How many lights are in the air now (the cap's count, and a probe's).</summary>
	public static int InAir => inAir;

	private Setup setup;
	private Player wizard;
	private float speed;
	private float burnClock;
	private float age;
	private float lifeLeft;
	private Vector2 rose;
	private Vector2 homeward = Vector2.Up;
	private float ending = -1f;
	private bool caught;
	private bool counted;

	/// <summary>
	/// Looses a light at <paramref name="at"/> into <paramref name="arena"/>, to go home to <paramref name="caster"/>.
	/// Null when the air is full or there is no one to go home to.
	/// </summary>
	public static TakenLight Loose(Node arena, Player caster, Vector2 at, Setup carries)
	{
		if (arena == null || !IsInstanceValid(arena) || caster == null || !IsInstanceValid(caster) || caster.IsDead
			|| inAir >= MaxInAir)
			return null;
		var light = new TakenLight
		{
			Name = "TakenLight",
			setup = carries,
			wizard = caster,
			lifeLeft = Mathf.Max(0.5f, carries.Life),
			ZAsRelative = false,
			ZIndex = DrawLayers.Air,
		};
		arena.AddChild(Player.TagSpell(light));
		light.GlobalPosition = at;
		light.rose = at + Vector2.Up * RiseHeight;
		return light;
	}

	public override void _EnterTree()
	{
		if (!counted)
		{
			counted = true;
			inAir++;
		}
	}

	public override void _ExitTree()
	{
		if (counted)
		{
			counted = false;
			inAir = Math.Max(0, inAir - 1);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		age += dt;
		QueueRedraw();

		if (ending >= 0f)
		{
			ending += dt;
			if (ending >= EndSeconds)
				QueueFree();
			return;
		}

		if (wizard == null || !IsInstanceValid(wizard) || wizard.IsDead)
		{
			End(false);
			return;
		}

		if (age < RiseSeconds)
		{
			float u = age / RiseSeconds;
			GlobalPosition = GlobalPosition.Lerp(rose, Mathf.Clamp(u * 0.5f, 0f, 1f));
			return;
		}

		Vector2 to = wizard.GlobalPosition - GlobalPosition;
		if (to.Length() <= CatchRadius)
		{
			Vector2 at = GlobalPosition;
			TakingLanes.Cross(GetParent(), wizard, at, homeward, setup);
			End(true);
			return;
		}
		homeward = to.Normalized();
		speed = Mathf.Min(MaxSpeed, Mathf.Max(speed, setup.DriftSpeed) + Acceleration * dt);
		GlobalPosition += homeward * Mathf.Min(to.Length(), speed * dt);

		burnClock += dt;
		if (burnClock >= BurnInterval && setup.BurnDamage > 0 && setup.BurnRadius > 0f)
		{
			burnClock = 0f;
			Burn();
		}

		lifeLeft -= dt;
		if (lifeLeft <= 0f)
			End(false);
	}

	// One pass over the field, every BurnInterval, nothing held.
	private void Burn()
	{
		float reach = setup.BurnRadius * setup.BurnRadius;
		Vector2 at = GlobalPosition;
		foreach (Enemy body in EnemyRegistry.Snapshot)
		{
			if (body == null || !IsInstanceValid(body) || body.IsDying || body.Health <= 0
				|| body.GlobalPosition.DistanceSquaredTo(at) > reach)
				continue;
			wizard.DealDamageToEnemy(body, setup.BurnDamage, 0f, true, setup.Spell);
		}
	}

	private void End(bool wasCaught)
	{
		if (ending >= 0f)
			return;
		ending = 0f;
		caught = wasCaught;
	}

	// A warm mote of the light ramp on the 2 px grid: a white-hot heart in an amber flame, two sparks
	// turning round it, guttering through its last second. Caught, it flares wide and goes; left, it shrinks
	// to a point.
	public override void _Draw()
	{
		ElementColors.Ramp light = ElementColors.GetRamp(Element.Light);
		float u = ending >= 0f ? Mathf.Clamp(ending / EndSeconds, 0f, 1f) : 0f;
		float scale = ending < 0f ? Mathf.Clamp(age / 0.15f, 0.3f, 1f) : caught ? 1f + 1.2f * u : 1f - u;
		float alpha = ending < 0f ? 1f : 1f - u;
		// The last second: it gutters, a blink that quickens as it goes out.
		if (ending < 0f && lifeLeft < 1f && MathF.Sin(age * (18f + 20f * (1f - lifeLeft))) < -0.3f)
			alpha *= 0.45f;
		if (alpha <= 0f || scale <= 0f)
			return;
		float pulse = 0.5f + 0.5f * MathF.Sin(age * 6f);
		// The pyre round it, out to the reach it sears.
		float pyre = Mathf.Max(11f, setup.BurnRadius) * scale;
		BossDeath.Disc(this, Vector2.Zero, pyre, pyre, new Color(light.Edge, 0.22f * alpha));
		BossDeath.Disc(this, Vector2.Zero, 11f * scale, 11f * scale, new Color(light.Edge, 0.35f * alpha));
		BossDeath.Disc(this, Vector2.Zero, 7f * scale, 7f * scale, new Color(light.Mid, alpha));
		BossDeath.Disc(this, Vector2.Zero, 4.5f * scale, 4.5f * scale, new Color(light.Hot.Lerp(light.Mid, pulse * 0.4f), alpha));
		DrawRect(WhipLimb.Cell(Vector2.Zero, BossDeath.Px * 2f * Mathf.Max(1f, scale)), new Color(light.Core, alpha));
		for (int k = 0; k < 2; k++)
		{
			float a = age * 4.2f + k * Mathf.Pi;
			Vector2 at = BossDeath.Snap(new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.7f) * (12f * scale));
			DrawRect(WhipLimb.Cell(at, BossDeath.Px), new Color(light.Hot, alpha));
		}
	}
}
