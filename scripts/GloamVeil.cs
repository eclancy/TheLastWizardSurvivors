using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// Gloam Veil - the second defensive spell brought back on Aegis Ward's rules (in-flight item 9;
// .ai/passives-and-items.md, "One thing came back"). The old Blur was a dodge chance sitting on the
// player; this is an active thing in a slot: a veil of dusk and wind that SWALLOWS THE NEXT HIT
// WHOLE, whatever its size, and is RE-WOVEN BY WALKING - its cadence is distance, not time (the
// Mirefoot family), so standing your ground is what makes it fail. The tear is its payload: the
// gust throws back and lashes everything close, and the same wind carries the wizard free
// (Player.ApplyTemporaryBuff).
//
// How it differs from its neighbours, which is the whole design (.ai/spell-roster.md section 2):
// Aegis Ward is a pool that soaks many small hits on a timer; the veil is one hit of ANY size,
// walked back - strong against the boss's blow, spent whole by a one-damage touch. Basalt Carapace
// (the relic) swallows a hit on a time cooldown, invisibly; the veil is visible, levelled, chosen.
// The reactive boons pulse when you are HURT; the veil answers a hit that never landed at all.
//
// Like Aegis Ward this is no scene: one code-made node in the arena on DrawLayers.Air, fixed pools,
// nothing allocated per frame. Player.TakeDamage spends it (TryConsume, placed after Basalt
// Carapace on purpose: the relic recharges on its own clock, the veil must be walked back, so the
// relic spends first and the veil holds). The milestones run here:
//
//   Night's Cloth (level 4, +1 Darkness) - while the veil is torn, bolts that would find the
//                 wizard die in the gloom at his shoulder (the 2d-bis guard family).
//   Tailwind      (level 4, +1 Wind) - the gust throws harder and the walk re-weaves sooner
//                 (the option's KnockbackBonus and RangeBonus, read off the spell).
//   Razor Hem     (level 4) - the tear's lash cuts deeper (DamageMultiplier).
//   Second Skin   (level 8) - the veil is woven double: two hits before it is bare.
//   Hour of Gloam (level 8) - the tear dims the world: all the gust touches is weakened.
public partial class GloamVeil : Node2D
{
	public const string SpellId = "gloam_veil";
	public const string NightsClothId = "gloam_veil_nights_cloth";
	public const string TailwindId = "gloam_veil_tailwind";
	public const string RazorHemId = "gloam_veil_razor_hem";
	public const string SecondSkinId = "gloam_veil_second_skin";
	public const string HourOfGloamId = "gloam_veil_hour_of_gloam";

	// The gust: its reach (x Area), its shove in KnockbackBonus units, and the lift that carries
	// the wizard free (ApplyTemporaryBuff: +35% move speed for GustCarrySeconds).
	public const float GustReach = 110f;
	public const float BaseKnockback = 150f;
	public const float GustCarryBonus = 0.35f;
	public const float GustCarrySeconds = 1.4f;
	// Hour of Gloam's weaken (Enemy.ApplyWeaken: they deal less while it holds).
	public const float GloamWeaken = 0.3f;
	public const float GloamWeakenSeconds = 4f;
	// Night's Cloth: how close a bolt may come to the torn wizard before the gloom takes it.
	public const float GloomBoltReach = 30f;
	// A frame's walk is credited only up to this, so a teleport or a shove weaves nothing.
	private const float MaxWeaveStepPerFrame = 12f;

	// The balance report's counters (_BalanceHarness), across the run.
	public static int StatHitsSwallowed { get; private set; }
	public static int StatWeaves { get; private set; }
	public static int StatGustHits { get; private set; }
	public static int StatGloomBolts { get; private set; }

	public static bool HasStats => StatHitsSwallowed + StatWeaves > 0;

	public static string StatsLine() =>
		$"gloam veil   : {StatHitsSwallowed} hits swallowed, {StatWeaves} re-weaves, {StatGustHits} gust hits; {StatGloomBolts} bolts lost in the gloom";

	public Player PlayerRef { get; set; }
	public SpellData SpellData { get; set; }
	public float DamageMultiplier { get; set; } = 1f;
	public float AreaMultiplier { get; set; } = 1f;

	private int plies;
	private float walked;
	private Vector2 lastPlayerAt;
	private bool hasLastAt;

	// The tear, drawn: ribbon fragments scattering for TearSeconds, and the gust's true ring.
	private const float TearSeconds = 0.4f;
	private float tearAge = -1f;
	private float tearReach;
	private float spin;
	private readonly List<Enemy> nearBuffer = new();
	private readonly ElementColors.Ramp darkness = ElementColors.GetRamp(Element.Darkness);
	private readonly ElementColors.Ramp wind = ElementColors.GetRamp(Element.Wind);
	private bool drewLastFrame;

	private int MaxPlies => SpellData != null && SpellData.HasEvolution(SecondSkinId) ? 2 : 1;

	/// <summary>Whether a hit would be swallowed right now.</summary>
	public bool IsWoven => plies > 0;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		Material = SpellInk.DitherByCell;
		// The first veil comes woven with the cast, so the spell is worth something the moment it
		// is taken; every later ply is walked back.
		plies = 1;
	}

	/// <summary>
	/// How far the wizard must walk to weave one ply back: the spell's Range, levelled down by its
	/// table and its milestones (Tailwind's RangeBonus lands here through GetRangeAtLevel).
	/// </summary>
	public float WeaveDistance =>
		SpellData == null ? 440f : MathF.Max(120f, SpellData.GetRangeAtLevel(SpellData.CurrentLevel));

	/// <summary>How far along the next ply is, 0..1, for the ribbons and the HUD-curious.</summary>
	public float WeaveProgress => plies >= MaxPlies ? 1f : Mathf.Clamp(walked / WeaveDistance, 0f, 1f);

	/// <summary>
	/// The hit that would have landed is swallowed whole and the veil tears (Player.TakeDamage,
	/// after the dodge and Basalt Carapace, before the DamageTaken signal: a veiled hit is a
	/// non-event, exactly like a dodge). False when the veil is bare.
	/// </summary>
	public bool TryConsume()
	{
		if (plies <= 0 || SpellData == null || PlayerRef == null || !IsInstanceValid(PlayerRef))
			return false;
		plies--;
		walked = 0f;
		StatHitsSwallowed++;
		Tear();
		return true;
	}

	// The tear: the gust - a shove and a lash of shadowed wind on everything close - and the same
	// wind carrying the wizard out of the press.
	private void Tear()
	{
		Vector2 centre = PlayerRef.GlobalPosition;
		int level = SpellData.CurrentLevel;
		int damage = Math.Max(1, Mathf.RoundToInt(SpellData.GetDamageAtLevel(level) * DamageMultiplier));
		float reach = MathF.Max(40f, GustReach
			* SpellData.GetAreaMultiplierAtLevel(level) * MathF.Sqrt(MathF.Max(0.25f, AreaMultiplier)));
		float shove = (BaseKnockback + SpellData.GetKnockbackBonusAtLevel(level)) * SpellData.KnockbackSpeedPerPoint;
		bool gloam = SpellData.HasEvolution(HourOfGloamId);
		EnemyRegistry.CollectNear(centre, reach + RiptideWave.BodyAllowance, nearBuffer);
		foreach (Enemy enemy in nearBuffer)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			StatGustHits++;
			PlayerRef.DealDamageToEnemy(enemy, damage, source: SpellData);
			Vector2 away = enemy.GlobalPosition - centre;
			enemy.ApplyKnockback((away.LengthSquared() > 1f ? away.Normalized() : Vector2.Down) * shove);
			if (gloam)
				enemy.ApplyWeaken(GloamWeaken, GloamWeakenSeconds);
		}
		nearBuffer.Clear();
		PlayerRef.ApplyTemporaryBuff(0f, GustCarryBonus, GustCarrySeconds);
		// A veiled hit must not sound like a hit - nothing landed (the dodge's rule).
		SfxPlayer.Global(SfxCatalog.PlayerDodge, 0.05f);
		tearAge = 0f;
		tearReach = reach;
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (PlayerRef == null || !IsInstanceValid(PlayerRef) || SpellData == null)
			return;
		// The spell left his book (cut, or the Sovereign Rite's silence): the veil hangs unspun.
		bool stood = SpellData.SilencedBySovereign || !PlayerRef.HasEquippedSpell(SpellId);
		spin += dt;

		if (!stood)
		{
			// The weave is the walk: credit this frame's travel, capped so a shove or a teleport
			// weaves nothing it did not earn.
			Vector2 at = PlayerRef.GlobalPosition;
			if (hasLastAt && plies < MaxPlies)
			{
				walked += MathF.Min((at - lastPlayerAt).Length(), MaxWeaveStepPerFrame);
				if (walked >= WeaveDistance)
				{
					walked = 0f;
					plies++;
					StatWeaves++;
				}
			}
			lastPlayerAt = at;
			hasLastAt = true;

			// Night's Cloth: while the veil is bare, bolts about to find him die in the gloom.
			if (plies == 0 && SpellData.HasEvolution(NightsClothId))
				DrownBoltsInGloom();
		}

		bool any = !stood && (plies > 0 || PlayerRef.Velocity.LengthSquared() > 1f);
		if (tearAge >= 0f)
		{
			any = true;
			tearAge += dt;
			if (tearAge >= TearSeconds)
				tearAge = -1f;
		}
		if (any || drewLastFrame)
			QueueRedraw();
		drewLastFrame = any;
	}

	private void DrownBoltsInGloom()
	{
		Vector2 centre = PlayerRef.GlobalPosition;
		IReadOnlyList<EnemyProjectile> bolts = EnemyProjectile.Live;
		float reachSquared = GloomBoltReach * GloomBoltReach;
		for (int b = bolts.Count - 1; b >= 0; b--)
		{
			EnemyProjectile bolt = bolts[b];
			if (bolt == null || !IsInstanceValid(bolt) || !bolt.InFlight)
				continue;
			if ((bolt.GlobalPosition - centre).LengthSquared() > reachSquared)
				continue;
			bolt.Destroy(Element.Darkness);
			StatGloomBolts++;
		}
	}

	// --- drawing ---------------------------------------------------------------------------------
	// Whole art pixels on the world's 2 px cell (PixelInk) through SpellInk.DitherByCell, like
	// AegisWard. The ribbons orbit at VeilRadius; a part-woven ply draws only its walked share, so
	// the weave is read off the wizard without a meter. The tear's ring is the gust's true reach.

	private const float VeilRadius = 22f;
	// Each ply is three ribbon arcs; the arc's half-length in radians when whole.
	private const float RibbonHalfArc = 0.55f;

	public override void _Draw()
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef) || SpellData == null)
			return;
		if (SpellData.SilencedBySovereign || !PlayerRef.HasEquippedSpell(SpellId))
			return;
		Transform2D toLocal = GlobalTransform.AffineInverse();
		Vector2 centre = toLocal * PlayerRef.GlobalPosition;
		int max = MaxPlies;
		for (int ply = 0; ply < max; ply++)
		{
			float share = ply < plies ? 1f : ply == plies ? WeaveProgress : 0f;
			if (share <= 0.02f)
				continue;
			DrawPly(centre, ply, share);
		}
		if (tearAge >= 0f)
			DrawTear(centre);
	}

	// One ply: three arcs of dusk round the wizard, a pale fleck of wind at each leading end,
	// counter-spun per ply so two plies read as cloth and not as one thick ring. A part-woven ply
	// is shorter and dimmer - the weave arriving.
	private void DrawPly(Vector2 centre, int ply, float share)
	{
		const float g = WhipLimb.Grid;
		float dir = ply % 2 == 0 ? 1f : -1f;
		float r = VeilRadius + ply * 3f;
		float arc = RibbonHalfArc * share;
		float cover = 0.45f + 0.55f * share;
		for (int k = 0; k < 3; k++)
		{
			float head = dir * spin * 1.6f + k * (Mathf.Tau / 3f) + ply * 0.9f;
			int steps = Math.Max(3, (int)(2f * arc * r / (2f * g)));
			for (int i = 0; i <= steps; i++)
			{
				float t = i / (float)steps;
				float a = head - dir * t * 2f * arc;
				var at = centre + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
				Color c = t < 0.2f ? darkness.Hot : t < 0.6f ? darkness.Mid : darkness.Edge;
				PixelInk.Cell(this, at, c, cover * (1f - t * 0.5f));
			}
			// The wind in it: one pale fleck at the ribbon's leading end.
			var tip = centre + new Vector2(MathF.Cos(head), MathF.Sin(head)) * r;
			PixelInk.Cell(this, tip, wind.Hot, cover);
		}
	}

	// The tear: the ribbons scatter outward and the gust's ring opens to its true reach.
	private void DrawTear(Vector2 centre)
	{
		const float g = WhipLimb.Grid;
		float u = Mathf.Clamp(tearAge / TearSeconds, 0f, 1f);
		float eased = 1f - (1f - u) * (1f - u);
		float cover = 1f - u;
		// Scattering fragments: twelve, flung out from the veil's radius.
		for (int i = 0; i < 12; i++)
		{
			float a = i * (Mathf.Tau / 12f) + 0.3f;
			float r = Mathf.Lerp(VeilRadius, VeilRadius + 26f, eased);
			var at = centre + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
			PixelInk.Cell(this, at, (i & 1) == 0 ? darkness.Mid : wind.Mid, cover);
		}
		// The gust's ring, at the reach the lash was measured on.
		float ring = Mathf.Lerp(VeilRadius, tearReach, eased);
		int steps = Math.Max(12, (int)(Mathf.Tau * ring / (2f * g)));
		for (int k = 0; k < steps; k++)
		{
			float a = k * Mathf.Tau / steps;
			var dirV = new Vector2(MathF.Cos(a), MathF.Sin(a));
			PixelInk.Cell(this, centre + dirV * ring, (k & 3) == 0 ? wind.Hot : wind.Edge, cover * 0.9f);
		}
	}
}
