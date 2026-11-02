using Godot;
using System;
using System.Collections.Generic;
using WizardSurvivors.scripts;

// THE TAKING (.ai/dark-wizard-playable.md section 3): the dark wizard's own spell, the boss's three verbs
// turned round to face the horde. This node is the first of them - THE HAND. Player.FireTheTaking throws
// one at the strongest enemy in reach (most health; the nearest of equals); it turns to follow it, closes
// on it, holds it still (a root - never a boss, which no crowd control may stop reaching the player), drains
// it twice and then tears its light out. The light rises as a TakenLight that drifts home to the wizard,
// and catching it is what throws his lanes (TakingLanes). The hand itself deals the drain and the tear; the
// lanes are the spell's reach.
//
// A scene with this script and nothing else, like KindledWard: it measures its own latch and draws its own
// hand, so what is seen holding a body is what holds it.
//
// EVERY CARD ITS OWN. The ids are the archetype's (SpellEvolutionCatalog.Branch); what each does is here:
//   Crushing Grip - the tear throws down what stands round the taken (CrushShare of a hit, CrushRadius).
//   Long Reach    - its numbers only: a faster hand that turns harder, a shorter cooldown.
//   Eightfold Cross - a caught light throws four lanes, not two (TakingLanes).
//   Great Taking  - the hand closes on the strongest and the GreatTakingExtra nearest it: a light from each.
//   Many Hands    - its numbers only: more hands a cast, each at its own quarry (Player.FireTheTaking).
//   All He Took   - the Apotheosis (take): a kill on a lane looses a light of its own (TakingLanes).
public partial class TheTaking : Node2D
{
	[Export] public float HandSpeed { get; set; } = 340f;
	[Export] public float TurnDegreesPerSecond { get; set; } = 540f;
	[Export] public float Lifetime { get; set; } = 2.4f;
	[Export] public float GripSeconds { get; set; } = 0.9f;
	[Export] public float LatchRadius { get; set; } = 22f;

	public const string SpellId = "the_taking";
	public const string CrushingGripId = "the_taking_amplified_impact";
	public const string LongReachId = "the_taking_hyper_acceleration";
	public const string EightfoldCrossId = "the_taking_reactive_catalyst";
	public const string GreatTakingId = "the_taking_titan_cataclysm";
	public const string ManyHandsId = "the_taking_infinite_horizon";
	public const string AllHeTookId = "the_taking_all_he_took";

	/// <summary>Each of the two drains, as a share of a hit; the tear is a whole one.</summary>
	public const float DrainShare = 0.3f;
	public const int DrainTicks = 2;
	/// <summary>Crushing Grip: what the tear throws down round the taken, and how far.</summary>
	public const float CrushShare = 0.6f;
	public const float CrushRadius = 70f;
	/// <summary>Great Taking: how many more it closes on, and how near the strongest they must stand.</summary>
	public const int GreatTakingExtra = 3;
	public const float GreatTakingReach = 130f;
	/// <summary>How far from where it lost its quarry the hand looks for another before it goes out.</summary>
	public const float RetargetReach = 220f;

	public int Damage { get; set; } = 8;
	public SpellData SpellData { get; set; }
	public Player PlayerRef { get; set; }
	public Enemy Quarry { get; set; }
	/// <summary>What each light it tears out carries home: the lanes' numbers (TakenLight.Setup).</summary>
	public TakenLight.Setup Light { get; set; }

	private enum Phase { Flying, Gripping, Gone }

	private Phase phase = Phase.Flying;
	private Vector2 heading = Vector2.Right;
	private float age;
	private float gripClock;
	private int drainsDone;
	private float fade = -1f;
	private const float FadeSeconds = 0.3f;
	private bool crushing;
	private bool great;
	private float crushFlash = -1f;
	private Vector2 crushAt;
	private const float CrushFlashSeconds = 0.3f;

	// What it holds: the quarry first, and Great Taking's three. Where each was last seen, so a body that
	// dies in the grip still gives its light up where it fell.
	private readonly List<Enemy> held = new(1 + GreatTakingExtra);
	private readonly List<Vector2> heldAt = new(1 + GreatTakingExtra);

	// Its trail: a fixed ring of points, written in place (DarkHand's).
	private const int TrailLength = 6;
	private readonly Vector2[] trail = new Vector2[TrailLength];
	private int trailHead;
	private float trailClock;

	public override void _Ready()
	{
		// In the air, over every body (DrawLayers): a thing thrown.
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		crushing = SpellData?.HasEvolution(CrushingGripId) == true;
		great = SpellData?.HasEvolution(GreatTakingId) == true;
		if (Quarry != null && IsInstanceValid(Quarry))
		{
			Vector2 to = Quarry.GlobalPosition - GlobalPosition;
			if (to.LengthSquared() > 0.01f)
				heading = to.Normalized();
		}
		for (int i = 0; i < TrailLength; i++)
			trail[i] = GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		age += dt;
		QueueRedraw();
		if (crushFlash >= 0f)
			crushFlash += dt;

		if (fade >= 0f)
		{
			fade += dt;
			if (fade >= FadeSeconds && (crushFlash < 0f || crushFlash >= CrushFlashSeconds))
				QueueFree();
			return;
		}

		if (phase == Phase.Flying)
			Fly(dt);
		else if (phase == Phase.Gripping)
			Grip(dt);
	}

	private void Fly(float dt)
	{
		if (!IsLive(Quarry))
		{
			Quarry = Strongest(GlobalPosition, RetargetReach, null);
			if (Quarry == null)
			{
				GoOut();
				return;
			}
		}

		Vector2 to = Quarry.GlobalPosition - GlobalPosition;
		if (to.Length() <= LatchRadius)
		{
			Latch();
			return;
		}
		float turn = Mathf.DegToRad(TurnDegreesPerSecond) * dt;
		heading = heading.Rotated(Mathf.Clamp(heading.AngleTo(to), -turn, turn));
		GlobalPosition += heading * HandSpeed * dt;

		trailClock += dt;
		if (trailClock >= 0.035f)
		{
			trailClock = 0f;
			trailHead = (trailHead + 1) % TrailLength;
			trail[trailHead] = GlobalPosition;
		}

		Lifetime -= dt;
		if (Lifetime <= 0f)
			GoOut();
	}

	private void Latch()
	{
		phase = Phase.Gripping;
		gripClock = 0f;
		held.Clear();
		heldAt.Clear();
		Hold(Quarry);
		if (great)
		{
			// The three nearest the strongest, within reach of it: one great hand closing on a knot. A pass over
			// the field for each, once a latch - never a frame.
			Vector2 knot = Quarry.GlobalPosition;
			for (int k = 0; k < GreatTakingExtra; k++)
			{
				Enemy nearest = null;
				float best = GreatTakingReach * GreatTakingReach;
				foreach (Enemy body in EnemyRegistry.Snapshot)
				{
					if (!IsLive(body) || held.Contains(body))
						continue;
					float d = body.GlobalPosition.DistanceSquaredTo(knot);
					if (d <= best)
					{
						best = d;
						nearest = body;
					}
				}
				if (nearest == null)
					break;
				Hold(nearest);
			}
		}
		SfxPlayer.AtPosition(SfxCatalog.SpellMawOpen, GlobalPosition, 0.06f);
	}

	private void Hold(Enemy body)
	{
		held.Add(body);
		heldAt.Add(body.GlobalPosition);
		// Held still while it is drained: a root, the length of the grip. Never a boss.
		if (body is not BossEnemy)
			body.ApplySlow(0f, GripSeconds + 0.1f);
	}

	private void Grip(float dt)
	{
		gripClock += dt;
		bool anyAlive = false;
		for (int i = 0; i < held.Count; i++)
		{
			if (IsLive(held[i]))
			{
				heldAt[i] = held[i].GlobalPosition;
				anyAlive = true;
			}
		}
		// The hand rides on what it holds.
		if (held.Count > 0)
			GlobalPosition = heldAt[0];

		// Every body it held is gone (to the drain or to anything else): the light comes out now.
		if (!anyAlive)
		{
			Tear();
			return;
		}

		float tick = GripSeconds / (DrainTicks + 1);
		while (drainsDone < DrainTicks && gripClock >= tick * (drainsDone + 1))
		{
			drainsDone++;
			int drain = Math.Max(1, Mathf.RoundToInt(Damage * DrainShare));
			for (int i = 0; i < held.Count; i++)
				Strike(held[i], drain);
		}

		if (gripClock >= GripSeconds)
			Tear();
	}

	private void Tear()
	{
		for (int i = 0; i < held.Count; i++)
			Strike(held[i], Damage);

		Node arena = GetParent();
		for (int i = 0; i < held.Count; i++)
			TakenLight.Loose(arena, PlayerRef, heldAt[i], Light);

		if (crushing && held.Count > 0)
		{
			crushAt = heldAt[0];
			crushFlash = 0f;
			int crush = Math.Max(1, Mathf.RoundToInt(Damage * CrushShare));
			foreach (Enemy body in EnemyRegistry.Snapshot)
			{
				if (!IsLive(body) || held.Contains(body) || body.GlobalPosition.DistanceTo(crushAt) > CrushRadius)
					continue;
				Strike(body, crush);
			}
		}
		phase = Phase.Gone;
		GoOut();
	}

	private void Strike(Enemy body, int amount)
	{
		if (!IsLive(body) || PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;
		PlayerRef.DealDamageToEnemy(body, amount, 0f, true, SpellData);
	}

	private void GoOut()
	{
		if (fade < 0f)
			fade = 0f;
	}

	private static bool IsLive(Enemy body) =>
		body != null && IsInstanceValid(body) && !body.IsDying && body.Health > 0 && body.IsInGroup("enemies");

	/// <summary>
	/// The strongest enemy within <paramref name="reach"/> of <paramref name="from"/>: the most health, and of
	/// equals the nearest. <paramref name="skip"/> is a set of bodies another hand of the same cast has taken
	/// (Many Hands: each at its own quarry). Null when there is none.
	/// </summary>
	public static Enemy Strongest(Vector2 from, float reach, ICollection<Enemy> skip)
	{
		Enemy best = null;
		int bestHealth = -1;
		float bestDistance = float.MaxValue;
		float reachSquared = reach * reach;
		foreach (Enemy body in EnemyRegistry.Snapshot)
		{
			if (!IsLive(body) || (skip != null && skip.Contains(body)))
				continue;
			float d = from.DistanceSquaredTo(body.GlobalPosition);
			if (d > reachSquared)
				continue;
			if (body.Health > bestHealth || (body.Health == bestHealth && d < bestDistance))
			{
				best = body;
				bestHealth = body.Health;
				bestDistance = d;
			}
		}
		return best;
	}

	// A hand of his dark light on the 2 px grid, the boss's (DarkHand) at two thirds: a dark palm with his
	// ramp at its rim, four fingers reaching ahead and a thumb, the core in the fingertips. Gripping, the
	// fingers close round what it holds and pulse with the drain; Great Taking runs a thread of his light
	// from the palm to each of the others it holds.
	public override void _Draw()
	{
		ElementColors.Ramp dark = ElementColors.GetRamp(Element.Darkness);
		float alpha = fade >= 0f ? 1f - Mathf.Clamp(fade / FadeSeconds, 0f, 1f) : Mathf.Clamp(age / 0.12f, 0f, 1f);
		Color shadow = new Color(0.06f, 0.02f, 0.10f, alpha);

		if (crushFlash >= 0f && crushFlash < CrushFlashSeconds)
		{
			float u = crushFlash / CrushFlashSeconds;
			BossDeath.Ring(this, ToLocal(crushAt), CrushRadius * (0.5f + 0.5f * u), CrushRadius * (0.5f + 0.5f * u),
				BossDeath.Px * 2f, new Color(dark.Hot, 1f - u));
			BossDeath.Ring(this, ToLocal(crushAt), CrushRadius * 0.4f * (1f + u), CrushRadius * 0.4f * (1f + u),
				BossDeath.Px, new Color(dark.Mid, 1f - u));
		}
		if (alpha <= 0f)
			return;

		if (phase == Phase.Flying)
		{
			for (int k = 1; k < TrailLength; k++)
			{
				int index = (trailHead - k + TrailLength) % TrailLength;
				Vector2 at = ToLocal(trail[index]);
				float a = alpha * (1f - k / (float)TrailLength) * 0.7f;
				DrawRect(WhipLimb.Cell(BossDeath.Snap(at), BossDeath.Px * (k < 3 ? 3f : 2f)), new Color(dark.Edge, a));
			}
		}

		bool gripping = phase != Phase.Flying;
		float pulse = 0.5f + 0.5f * MathF.Sin(age * (gripping ? 16f : 7f));
		// Gripping, the fingers come round toward the palm: shorter, splayed wider, as a hand closing on a body.
		float reach = gripping ? 13f : 18f;
		float splay = gripping ? 0.42f : 0.26f;
		float flex = 0.10f * MathF.Sin(age * 9f);

		if (great && gripping)
		{
			for (int i = 1; i < heldAt.Count; i++)
			{
				Vector2 to = ToLocal(heldAt[i]);
				BossDeath.Line(this, Vector2.Zero, to, new Color(dark.Mid, 0.55f * alpha), BossDeath.Px);
				DrawRect(WhipLimb.Cell(BossDeath.Snap(to), BossDeath.Px * 3f), new Color(dark.Hot.Lerp(dark.Core, pulse), alpha));
			}
		}

		BossDeath.Disc(this, Vector2.Zero, 15f, 15f, new Color(dark.Mid, 0.26f * alpha));
		BossDeath.Disc(this, Vector2.Zero, 11f, 11f, new Color(dark.Hot.Lerp(dark.Mid, pulse), alpha));
		BossDeath.Disc(this, Vector2.Zero, 8f, 8f, shadow);
		for (int f = 0; f < 4; f++)
		{
			float spread = (f - 1.5f) * splay + flex * (f % 2 == 0 ? 1f : -1f);
			Vector2 dir = heading.Rotated(spread);
			Vector2 knuckle = dir * 7f;
			Vector2 tip = dir * (reach + (f == 1 || f == 2 ? 4f : 0f));
			if (gripping)
				tip = tip.Rotated((f < 2 ? 1f : -1f) * 0.5f);
			BossDeath.Line(this, knuckle, tip, new Color(dark.Hot, alpha), BossDeath.Px * 2f);
			DrawRect(WhipLimb.Cell(BossDeath.Snap(tip), BossDeath.Px * 2f), new Color(gripping ? dark.Core : dark.Hot, alpha));
		}
		Vector2 thumbDir = heading.Rotated(-1.25f - flex);
		BossDeath.Line(this, thumbDir * 6f, thumbDir * 14f, new Color(dark.Hot, alpha), BossDeath.Px * 2f);
		DrawRect(WhipLimb.Cell(BossDeath.Snap(heading * 8f), BossDeath.Px * 2f), new Color(dark.Core, alpha));
	}
}
