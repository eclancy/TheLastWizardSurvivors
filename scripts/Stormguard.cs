using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// Stormguard - the first defensive spell brought back on Aegis Ward's rules (in-flight item 9;
// .ai/passives-and-items.md, "One thing came back"). The old Stormguard Aura was a number that arced
// when the player was hurt; this is an active thing in a slot: the cooldown GATHERS a storm over the
// wizard, and the blows he takes bring it down - at StrikesToBreak strikes the storm BREAKS, a
// thunderclap that damages and throws back everything close and soaks it in slowing rain.
//
// Its failure modes are the design (a defensive spell must be able to fail, as Aegis Ward breaks):
// while the storm regathers the player is uncovered, and a gathered storm over a player nobody
// touches never breaks at all - it asks the player to stand their ground, where Aegis Ward asks
// nothing. Strikes are counted PRE-mitigation, beside the reactive boons and for their reason
// (Player.TakeDamage: a reaction answers being struck, not being hurt).
//
// Like Aegis Ward this is no scene: one code-made node in the arena on DrawLayers.Air, fixed pools,
// nothing allocated per frame. Player._PhysicsProcess's cast branch arms it (Arm), Player.TakeDamage
// feeds it (NotifyStruck), and the milestones run here:
//
//   Charged Air    (level 4, +1 Lightning) - the storm breaks after one blow fewer.
//   Stormfront     (level 4) - the clap reaches wider and throws harder (Area, Knockback).
//   Downpour       (level 4) - the rain's slow deepens (SlowMagnitudeBonus) and outlasts the clap.
//   Eye of the Storm (level 8, +1 Water) - the break leaves the storm's eye for EyeSeconds: enemy
//                  bolts die at its edge (the 2d-bis guard family) and the rain keeps falling inside.
//   Rolling Thunder (level 8) - a second thunderclap follows the first at EchoShare of its weight.
public partial class Stormguard : Node2D
{
	public const string SpellId = "stormguard";
	public const string ChargedAirId = "stormguard_charged_air";
	public const string StormfrontId = "stormguard_stormfront";
	public const string DownpourId = "stormguard_downpour";
	public const string EyeId = "stormguard_eye";
	public const string RollingThunderId = "stormguard_rolling_thunder";

	// How many blows bring a gathered storm down (Charged Air takes one off).
	public const int StrikesToBreak = 3;
	// The clap's slow: the rain. Downpour adds SlowMagnitudeBonus on top and DownpourExtraSeconds.
	public const float BaseSlow = 0.35f;
	public const float BaseSlowSeconds = 2.5f;
	public const float DownpourExtraSeconds = 1.5f;
	// The clap's shove, in KnockbackBonus units (SpellData.KnockbackSpeedPerPoint turns it into px/s).
	public const float BaseKnockback = 130f;
	// The eye (level 8): how long it holds after the break, as a share of the clap's reach.
	public const float EyeSeconds = 4f;
	public const float EyeReachShare = 0.9f;
	// Rolling Thunder: the echo's delay and its share of the clap.
	public const float EchoDelaySeconds = 0.7f;
	public const float EchoShare = 0.75f;

	// The balance report's counters (_BalanceHarness), across the run.
	public static int StatBreaks { get; private set; }
	public static int StatStruck { get; private set; }
	public static int StatClapHits { get; private set; }
	public static int StatEyeBoltsDrowned { get; private set; }

	public static bool HasStats => StatBreaks + StatStruck > 0;

	public static string StatsLine() =>
		$"stormguard   : {StatBreaks} breaks from {StatStruck} blows, {StatClapHits} clap hits; {StatEyeBoltsDrowned} bolts drowned in the eye";

	public Player PlayerRef { get; set; }
	public SpellData SpellData { get; set; }
	public float DamageMultiplier { get; set; } = 1f;
	public float AreaMultiplier { get; set; } = 1f;

	/// <summary>Whether a storm hangs gathered overhead, waiting on the blows.</summary>
	public bool Gathered { get; private set; }

	private int strikes;
	private float eyeSecondsRemaining;
	private float eyeReach;
	private float eyeSlowClock;
	private float echoInSeconds = -1f;
	private int echoDamage;
	private float echoReach;

	// One expanding clap ring at a time is enough to read; the echo reuses it.
	private float clapAge = -1f;
	private float clapReach;
	// The rain: drawn over the clap's ground for RainSeconds after a break.
	private const float RainSeconds = 1.1f;
	private float rainAge = -1f;
	private float rainReach;
	private const float ClapSeconds = 0.3f;

	private readonly List<Enemy> nearBuffer = new();
	private readonly ElementColors.Ramp lightning = ElementColors.GetRamp(Element.Lightning);
	private readonly ElementColors.Ramp water = ElementColors.GetRamp(Element.Water);
	private float sparkClock;
	private uint sparkSeed = 1;
	private bool drewLastFrame;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		// Every fade a dissolve on the world's 2 px cell, as AegisWard draws (SpellInk).
		Material = SpellInk.DitherByCell;
	}

	/// <summary>
	/// The cast: gathers a storm if none hangs. True when one actually gathered (CastOutcome.Fired);
	/// false keeps the stats current, as Aegis Ward topping up a full pool is Maintained.
	/// </summary>
	public bool Arm()
	{
		if (Gathered)
			return false;
		Gathered = true;
		strikes = 0;
		QueueRedraw();
		return true;
	}

	private int BreakAt => SpellData != null && SpellData.HasEvolution(ChargedAirId)
		? StrikesToBreak - 1 : StrikesToBreak;

	/// <summary>A blow landed on the wizard (pre-mitigation, Player.TakeDamage). Enough of them break the storm.</summary>
	public void NotifyStruck()
	{
		if (!Gathered || SpellData == null || PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;
		StatStruck++;
		strikes++;
		if (strikes >= BreakAt)
			Break();
	}

	private void Break()
	{
		Gathered = false;
		strikes = 0;
		StatBreaks++;
		int level = SpellData.CurrentLevel;
		int damage = Math.Max(1, Mathf.RoundToInt(SpellData.GetDamageAtLevel(level) * DamageMultiplier));
		float reach = MathF.Max(40f, SpellData.GetRangeAtLevel(level)
			* SpellData.GetAreaMultiplierAtLevel(level) * MathF.Sqrt(MathF.Max(0.25f, AreaMultiplier)));
		Clap(damage, reach);
		SfxPlayer.Impact(Element.Lightning, PlayerRef.GlobalPosition);
		if (SpellData.HasEvolution(RollingThunderId))
		{
			echoInSeconds = EchoDelaySeconds;
			echoDamage = Math.Max(1, Mathf.RoundToInt(damage * EchoShare));
			echoReach = reach;
		}
		if (SpellData.HasEvolution(EyeId))
		{
			eyeSecondsRemaining = EyeSeconds;
			eyeReach = reach * EyeReachShare;
			eyeSlowClock = 0f;
		}
	}

	// The thunderclap itself: damage, the shove outward, and the rain's slow, on everything close.
	private void Clap(int damage, float reach)
	{
		Vector2 centre = PlayerRef.GlobalPosition;
		int level = SpellData.CurrentLevel;
		float slow = Mathf.Clamp(BaseSlow + SpellData.GetSlowMagnitudeAtLevel(level), 0f, 0.9f);
		float slowSeconds = BaseSlowSeconds + (SpellData.HasEvolution(DownpourId) ? DownpourExtraSeconds : 0f);
		float shove = (BaseKnockback + SpellData.GetKnockbackBonusAtLevel(level)) * SpellData.KnockbackSpeedPerPoint;
		EnemyRegistry.CollectNear(centre, reach + RiptideWave.BodyAllowance, nearBuffer);
		foreach (Enemy enemy in nearBuffer)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			StatClapHits++;
			PlayerRef.DealDamageToEnemy(enemy, damage, source: SpellData);
			Vector2 away = enemy.GlobalPosition - centre;
			enemy.ApplyKnockback((away.LengthSquared() > 1f ? away.Normalized() : Vector2.Down) * shove);
			enemy.ApplySlow(1f - slow, slowSeconds);
		}
		nearBuffer.Clear();
		clapAge = 0f;
		clapReach = reach;
		rainAge = 0f;
		rainReach = reach;
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (PlayerRef == null || !IsInstanceValid(PlayerRef) || SpellData == null)
			return;
		// The spell left his book (cut, or the Sovereign Rite's silence): the storm stands down.
		if (SpellData.SilencedBySovereign || !PlayerRef.HasEquippedSpell(SpellId))
		{
			Gathered = false;
			eyeSecondsRemaining = 0f;
			echoInSeconds = -1f;
			if (drewLastFrame)
				QueueRedraw();
			drewLastFrame = false;
			return;
		}
		if (echoInSeconds >= 0f)
		{
			echoInSeconds -= dt;
			if (echoInSeconds < 0f)
				Clap(echoDamage, echoReach);
		}
		if (eyeSecondsRemaining > 0f)
			TickEye(dt);
		bool any = Gathered || eyeSecondsRemaining > 0f;
		if (clapAge >= 0f)
		{
			any = true;
			clapAge += dt;
			if (clapAge >= ClapSeconds)
				clapAge = -1f;
		}
		if (rainAge >= 0f)
		{
			any = true;
			rainAge += dt;
			if (rainAge >= RainSeconds)
				rainAge = -1f;
		}
		if (any)
			sparkClock += dt;
		if (any || drewLastFrame)
			QueueRedraw();
		drewLastFrame = any;
	}

	// The eye (level 8): bolts die at its edge and the rain keeps slowing what stands in it.
	private void TickEye(float dt)
	{
		eyeSecondsRemaining -= dt;
		Vector2 centre = PlayerRef.GlobalPosition;
		IReadOnlyList<EnemyProjectile> bolts = EnemyProjectile.Live;
		float reachSquared = eyeReach * eyeReach;
		for (int b = bolts.Count - 1; b >= 0; b--)
		{
			EnemyProjectile bolt = bolts[b];
			if (bolt == null || !IsInstanceValid(bolt) || !bolt.InFlight)
				continue;
			if ((bolt.GlobalPosition - centre).LengthSquared() > reachSquared)
				continue;
			bolt.Destroy(Element.Water);
			StatEyeBoltsDrowned++;
		}
		// The rain inside, reapplied in sips rather than per frame.
		eyeSlowClock -= dt;
		if (eyeSlowClock > 0f)
			return;
		eyeSlowClock = 0.5f;
		EnemyRegistry.CollectNear(centre, eyeReach, nearBuffer);
		foreach (Enemy enemy in nearBuffer)
		{
			if (enemy != null && IsInstanceValid(enemy) && !enemy.IsDying)
				enemy.ApplySlow(1f - BaseSlow, 0.7f);
		}
		nearBuffer.Clear();
	}

	// --- drawing ---------------------------------------------------------------------------------
	// All of it whole art pixels on the world's 2 px cell (PixelInk), fades as dissolves through
	// SpellInk.DitherByCell, and every radius drawn is one a hit reads - the clap ring is clapReach,
	// the eye's rim is eyeReach - so the tell never lies (the GroundSlamAttack rule).

	private const float GatherRadius = 26f;

	public override void _Draw()
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;
		Transform2D toLocal = GlobalTransform.AffineInverse();
		Vector2 centre = toLocal * PlayerRef.GlobalPosition;
		if (Gathered)
			DrawGatheredStorm(centre);
		if (clapAge >= 0f)
			DrawClap(centre);
		if (rainAge >= 0f)
			DrawRain(centre);
		if (eyeSecondsRemaining > 0f)
			DrawEye(centre);
	}

	private static uint Hash(uint x)
	{
		x ^= x << 13; x ^= x >> 17; x ^= x << 5;
		return x;
	}

	// The gathered storm: sparks crackling round the wizard, more of them the closer it is to
	// breaking, flickering cell to cell so it reads as charge and not as a ring he owns.
	private void DrawGatheredStorm(Vector2 centre)
	{
		const float g = WhipLimb.Grid;
		int beat = (int)(sparkClock * 14f);
		int count = 3 + strikes * 3;
		for (int i = 0; i < count; i++)
		{
			uint h = Hash((uint)(beat * 31 + i * 7 + 1) * (sparkSeed + 11u));
			float angle = (h % 1024u) / 1024f * Mathf.Tau;
			float r = GatherRadius + (Hash(h) % 3u) * g - g;
			var at = centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;
			Color c = (h & 3u) == 0u ? lightning.Core : (h & 1u) == 0u ? lightning.Hot : lightning.Mid;
			PixelInk.Cell(this, at, c, 1f);
			// A short tail inward every few sparks: the arc, not just a dot.
			if ((h & 7u) == 0u)
				PixelInk.Cell(this, at - at.DirectionTo(centre) * -g, lightning.Mid, 0.9f);
		}
	}

	// The break: a ring that opens out to the clap's true reach and dissolves as it arrives.
	private void DrawClap(Vector2 centre)
	{
		const float g = WhipLimb.Grid;
		float u = Mathf.Clamp(clapAge / ClapSeconds, 0f, 1f);
		float eased = 1f - (1f - u) * (1f - u);
		float r = Mathf.Lerp(GatherRadius, clapReach, eased);
		float cover = 1f - u * u;
		int steps = Math.Max(16, (int)(Mathf.Tau * r / g));
		for (int k = 0; k < steps; k++)
		{
			float a = k * Mathf.Tau / steps;
			var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
			Color c = (k & 3) == 0 ? lightning.Core : (k & 1) == 0 ? lightning.Hot : lightning.Mid;
			PixelInk.Cell(this, centre + dir * r, c, cover);
			if ((k & 1) == 0)
				PixelInk.Cell(this, centre + dir * (r - g), lightning.Edge, cover * 0.8f);
		}
	}

	// The rain after the break: short falling streaks of Water scattered over the clap's ground,
	// the slow made visible, thinning out as it ends.
	private void DrawRain(Vector2 centre)
	{
		const float g = WhipLimb.Grid;
		float u = Mathf.Clamp(rainAge / RainSeconds, 0f, 1f);
		float cover = 1f - u;
		int beat = (int)(sparkClock * 10f);
		int drops = (int)(rainReach * 0.34f);
		for (int i = 0; i < drops; i++)
		{
			uint h = Hash((uint)(beat * 17 + i * 13 + 5) * 2654435761u);
			float angle = (h % 2048u) / 2048f * Mathf.Tau;
			float r = MathF.Sqrt((Hash(h) % 1024u) / 1024f) * rainReach;
			var at = centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;
			Color c = (h & 3u) == 0u ? water.Hot : water.Mid;
			PixelInk.Cell(this, at, c, cover * 0.9f);
			PixelInk.Cell(this, at + new Vector2(g * 0.5f, -g), water.Edge, cover * 0.6f);
		}
	}

	// The eye: a thin, calm rim of Water at the radius bolts actually die on.
	private void DrawEye(Vector2 centre)
	{
		const float g = WhipLimb.Grid;
		float cover = Mathf.Clamp(eyeSecondsRemaining / 0.8f, 0f, 1f) * 0.8f;
		int steps = Math.Max(16, (int)(Mathf.Tau * eyeReach / (3f * g)));
		for (int k = 0; k < steps; k++)
		{
			float a = k * Mathf.Tau / steps + sparkClock * 0.4f;
			var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
			PixelInk.Cell(this, centre + dir * eyeReach, (k & 3) == 0 ? water.Hot : water.Edge, cover);
		}
	}
}
