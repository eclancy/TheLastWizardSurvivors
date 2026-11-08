using Godot;
using System.Collections.Generic;
using System;

namespace TheOneHeMissed.scripts;

// Shared script for simple "AoE pulse" offensive spells (issue #13 roster expansion): Solar Flare
// (Toxic Spore Burst left it for its mushrooms, SporeMushroom). Behaves like ArcaneExplosion
// (auto-repeating pulse around the caster, one persistent instance per equipped spell) plus an
// optional guaranteed status effect applied to everything caught in the blast.
public partial class ElementalPulse : Area2D
{
	[Export] public SpellData SpellData { get; set; }
	[Export] public int CurrentLevel { get; set; } = 1;
	public float DamageMultiplier { get; set; } = 1.0f;
	public float AreaMultiplier { get; set; } = 1.0f;
	public float CooldownMultiplier { get; set; } = 1.0f;
	public float DurationMultiplier { get; set; } = 1.0f;
	public Node2D PlayerRef;
	[Export] public float TotalLifetime = 0f; // 0 = infinite (persistent child of Player)

	[Export] public bool GuaranteedPoison { get; set; } = false;
	[Export] public int PoisonDamagePerTick { get; set; } = 2;
	[Export] public float PoisonDuration { get; set; } = 3.0f;
	// Weaken applied to everything the pulse touches (Enemy.ApplyWeaken, issue #66). 0 = off.
	// Solar Flare's: a flash at arm's length that leaves whatever is closest swinging blind.
	[Export] public float WeakenFraction { get; set; } = 0f;
	[Export] public float WeakenDuration { get; set; } = 2.5f;
	[Export] public float RingExpandDuration { get; set; } = 0.22f;
	[Export] public float RingRetractDuration { get; set; } = 0.14f;
	[Export] public Color VisualColor { get; set; } = new Color(1.0f, 0.84f, 0.20f, 0.85f);

	// SOLAR FLARE GROWS WHILE YOU STAND STILL (#68, .ai/spell-roster.md section 3): the anti-kite
	// spell. Every pulse reads how long the player has held his ground and goes off wider and hotter
	// for it, up to StillRadiusGrowth more reach and StillDamageGrowth more damage after
	// StillGrowthSeconds; a step resets it. The stillness is Player.StillSeconds - intent, not
	// displacement, the same reading the Brazier and Taproot make - so a shove from the crowd never
	// cools the flare when it does not cool the Brazier, and the three cannot disagree about what
	// "still" is. It shows twice: the burst is drawn to the reach it hits at, and between bursts a
	// glow at his feet brightens as it builds (SolarFlareVisual.SetCharge) - never a ring at the
	// reach, which is the area marker .ai/spell-roster.md section 2c took off every player spell.
	[Export] public float StillGrowthSeconds { get; set; } = 4f;
	[Export] public float StillRadiusGrowth { get; set; } = 0.6f;
	[Export] public float StillDamageGrowth { get; set; } = 1.0f;

	private Node2D feetShape;

	/// <summary>How far the flare has built, 0 (just moved) to 1 (held for StillGrowthSeconds).</summary>
	public float StillCharge => StillGrowthSeconds > 0f && PlayerRef is Player holder && holder.IsStandingStill
		? Mathf.Clamp(holder.StillSeconds / StillGrowthSeconds, 0f, 1f)
		: 0f;

	// Blinding Flash (solar_flare_blinding_flash): the chance a pulse staggers and shoves what it
	// hits, and how long the stagger holds. Its KnockbackBonus is the shove (SpellData.GetKnockbackSpeedAtLevel).
	public const float BlindingFlashChance = 0.3f;
	public const float BlindingFlashStunSeconds = 0.4f;

	private int damage = 5;
	private float range = 100f;
	private float cooldown = 1.5f;
	private float lifeElapsed = 0f;
	private float fireTimer = 0f;
	private float ringPulseTimer = 0f;
	private bool ringPulseActive = false;
	private GpuParticles2D particles;
	private PlaceholderShape visual;
	// A spell may ship its own bespoke visual (a child node named "Visual" implementing IPulseVisual)
	// instead of the shared PlaceholderShape ring. Solar Flare, the one pulse spell left, does.
	private IPulseVisual customVisual;

	public override void _Ready()
	{
		RefreshComputedStats();
		particles = GetNodeOrNull<GpuParticles2D>("Particles");
		visual = GetNodeOrNull<PlaceholderShape>("PlaceholderShape");
		customVisual = GetNodeOrNull<Node2D>("Visual") as IPulseVisual;
		TriggerPulse();
		fireTimer = 0f;
	}

	public override void _ExitTree()
	{
		if (sunDogGlow != null && IsInstanceValid(sunDogGlow))
			sunDogGlow.Draw -= DrawSunDogs;
		if (prominenceCanvas != null && IsInstanceValid(prominenceCanvas))
			prominenceCanvas.Draw -= DrawProminences;
		if (stoneCanvas != null && IsInstanceValid(stoneCanvas))
			stoneCanvas.Draw -= DrawStones;
	}

	public override void _Process(double delta)
	{
		if (PlayerRef != null)
			GlobalPosition = PlayerRef.GlobalPosition;

		UpdateRingPulse((float)delta);
		BurnBolts();
		TickSunDogs((float)delta);
		TickProminences((float)delta);
		TickStones((float)delta);
		if (customVisual is SolarFlareVisual building)
		{
			// The Feet capsule, which Player.FitFeetToSprite moves for other cell sizes; the pulse sits
			// on the player's origin, so its position is already in this node's space.
			if (feetShape == null || !IsInstanceValid(feetShape))
				feetShape = PlayerRef?.GetNodeOrNull<Node2D>("Feet/CollisionShape2D");
			building.SetCharge(StillCharge, feetShape?.Position ?? new Vector2(0f, 36f));
		}

		if (TotalLifetime > 0f)
		{
			lifeElapsed += (float)delta;
			if (lifeElapsed >= TotalLifetime)
			{
				QueueFree();
				return;
			}
		}

		fireTimer += (float)delta;
		if (fireTimer >= cooldown)
		{
			TriggerPulse();
			fireTimer = 0f;
		}
	}

	private void TriggerPulse()
	{
		// The cue plays on every pulse, hit or not. This spell is a persistent emitter, so the fire
		// loop's cast cue never reaches it after creation (CastOutcome.Maintained) and it used to go
		// off in silence on an empty field. A pulse is a cast the player can see; it should be one
		// they can hear. Creation plays it here too - the fire loop stays quiet for that frame - so
		// the first pulse is heard once, not twice.
		SfxPlayer.Cast(SfxCatalog.DominantElement(SpellData), GlobalPosition);

		var player = PlayerRef as Player;
		float knockbackSpeed = SpellData?.GetKnockbackSpeedAtLevel(CurrentLevel) ?? 0f;
		// This pulse's reach and damage, grown by however long he has stood still for it.
		float charge = StillCharge;
		float reach = range * (1f + StillRadiusGrowth * charge);
		int hit = Math.Max(1, Mathf.RoundToInt(damage * (1f + StillDamageGrowth * charge)));
		// Iterate the "enemies" group, like every other AoE in the project. This used to walk
		// CurrentScene's *direct children* only, so any enemy parented to a spawner or container
		// was skipped entirely and silently took no damage from the pulse.
		foreach (var node in EnemyRegistry.Snapshot)
		{
			if (node is not Node2D e || !IsInstanceValid(e))
				continue;
			float distance = GlobalPosition.DistanceTo(e.GlobalPosition);
			if (distance > reach)
				continue;

			// Solar Flare rises out of the ground under what it strikes, as its front passes under it
			// (SolarFlareVisual, FROM UNDERNEATH) - before the hit, so a body it kills still stands in it.
			(customVisual as SolarFlareVisual)?.RiseUnder(e, distance, reach);
			player?.DealDamageToEnemy(e, hit, source: SpellData);
			if (GuaranteedPoison && e.HasMethod("ApplyPoison"))
				e.Call("ApplyPoison", PoisonDamagePerTick, PoisonDuration);
			if (WeakenFraction > 0f && e is Enemy dazzled)
				dazzled.ApplyWeaken(WeakenFraction, WeakenDuration * DurationMultiplier);
			// Blinding Flash: some of what the flash catches reels - a stagger and a shove straight
			// out from the caster. Not every one: a pulse that pushed the whole ring away every two
			// seconds would hold the crowd at its edge, which is a ward and not a flash.
			if (knockbackSpeed > 0f && e is Enemy blinded && GD.Randf() < BlindingFlashChance)
			{
				Vector2 away = e.GlobalPosition - GlobalPosition;
				blinded.ApplyShock(BlindingFlashStunSeconds);
				blinded.ApplyKnockback((away.LengthSquared() > 0.01f ? away.Normalized() : Vector2.Right) * knockbackSpeed);
			}
		}

		FireCoronaBeams(player, reach, hit);
		FlareSunDogs(player, reach, hit);
		HurlProminences(reach, hit, charge);
		ThrowTeethStone(reach, hit, charge);

		if (particles != null)
			particles.Restart();

		if (customVisual != null)
		{
			// A bespoke visual owns its own animation and timing, so the generic ring tween below
			// (and the ringPulseActive state it drives) is skipped entirely for those spells.
			customVisual.Burst(reach);
			// And under him: the flare goes off beneath his boots.
			(customVisual as SolarFlareVisual)?.RiseUnder(PlayerRef, 0f, reach);
			return;
		}

		if (visual != null)
		{
			visual.Ring = true;
			visual.EnableIdleMotion = false;
			visual.Radius = 2f;
			visual.RingWidth = 6f;
			visual.ShapeColor = VisualColor;
			visual.QueueRedraw();
		}

		burstReach = reach;
		ringPulseTimer = 0f;
		ringPulseActive = true;
	}

	// The reach of the pulse the fallback ring is drawing, grown or not.
	private float burstReach = 100f;

	// Corona Discharge (solar_flare_corona_discharge): every pulse also throws CoronaBeams sunbeams out
	// past the flare, evenly spaced and turned a little each pulse, each striking everything along it
	// for CoronaBeamShare of the pulse. The card promised eight sunbeams from the start and the pulse
	// fired none; then it was damage alone (2026-10-26, morning), which is not what an Ascension should
	// feel like. Built that afternoon. SolarFlareVisual draws the beams at exactly this length and width.
	public const string CoronaDischargeId = "solar_flare_corona_discharge";

	// DAYBREAK AVATAR IS A WARD (2026-10-28). Eric: "one of the spells that can protect you from
	// projectiles should be an ascension for an aoe spell around you that destroys projectiles, like
	// garlic does in vampire survivors." The flare is the spell around the wizard, and Daybreak was the
	// Ascension that only made it bigger (+100% area, +100% damage): now every enemy bolt that comes
	// inside the flare's reach - the reach it has this moment, grown by standing still - burns away in
	// a burst of light, between pulses as well as on them. Ranged casters stop being a threat inside
	// it; they still are outside, and nothing else they do is touched.
	public const string DaybreakId = "solar_flare_daybreak";
	public static int StatBoltsBurned { get; private set; }

	private void BurnBolts()
	{
		IReadOnlyList<EnemyProjectile> bolts = EnemyProjectile.Live;
		if (bolts.Count == 0 || SpellData == null || !SpellData.HasEvolution(DaybreakId))
			return;
		float reach = range * (1f + StillRadiusGrowth * StillCharge);
		float r2 = reach * reach;
		for (int i = bolts.Count - 1; i >= 0; i--)
		{
			EnemyProjectile bolt = bolts[i];
			if (bolt == null || !IsInstanceValid(bolt) || !bolt.InFlight)
				continue;
			if (bolt.GlobalPosition.DistanceSquaredTo(GlobalPosition) > r2)
				continue;
			bolt.Destroy(Element.Light);
			StatBoltsBurned++;
		}
	}
	public const int CoronaBeams = 8;
	private const float CoronaBeamReach = 2.6f;
	private const float CoronaBeamHalfWidth = 14f;
	private const float CoronaBeamShare = 0.75f;
	private float coronaTurn;

	// The beams grow with the flare they come out of: the reach and damage of THIS pulse.
	private void FireCoronaBeams(Player player, float reach, int pulseDamage)
	{
		if (SpellData == null || !SpellData.HasEvolution(CoronaDischargeId))
			return;
		coronaTurn += 0.41f;
		float length = reach * CoronaBeamReach;
		float halfWidth = CoronaBeamHalfWidth * MathF.Max(1f, AreaMultiplier);
		int beamDamage = Math.Max(1, Mathf.RoundToInt(pulseDamage * CoronaBeamShare));
		Vector2 origin = GlobalPosition;
		foreach (Node node in EnemyRegistry.Snapshot)
		{
			if (node is not Node2D e || !IsInstanceValid(e))
				continue;
			Vector2 offset = e.GlobalPosition - origin;
			float distance = offset.Length();
			if (distance > length || distance < 1f)
				continue;
			for (int i = 0; i < CoronaBeams; i++)
			{
				Vector2 dir = Vector2.FromAngle(coronaTurn + Mathf.Tau * i / CoronaBeams);
				if (offset.Dot(dir) <= 0f || MathF.Abs(offset.Cross(dir)) > halfWidth)
					continue;
				player?.DealDamageToEnemy(e, beamDamage, source: SpellData);
				break;
			}
		}
		if (customVisual is SolarFlareVisual flare)
			flare.SetBeams(CoronaBeams, length, halfWidth, coronaTurn);
	}

	// SUN DOGS (solar_flare_radiance, .ai/spell-roster.md 2h, 2026-10-29). The card was Radiant Glow:
	// +40% area and 25% faster pulses, a bigger flare more often and nothing a player could point at. A
	// sun dog is the real thing - a mock sun either side of the sun, on the halo round it. Two hang either
	// side of the wizard on the line through his middle and go off with every pulse, each a flare of its
	// own SunDogReach of this pulse's reach, for SunDogDamageShare of its damage (and the flare's Weaken).
	// They hang SunDogOffset of the flare's reach out from him, so they drift outward as it builds while
	// he stands still: the charge shows in where they hang as well as in the pool at his feet. Between
	// pulses each is a small sun in the air; on a pulse, the flare's own burst (a SolarFlareVisual of its
	// own) drawn out of the radius it hits. The pull, the beams and the ward stay the flare's alone.
	public const string SunDogsId = "solar_flare_radiance";
	public const float SunDogOffset = 1.6f;
	public const float SunDogReach = 0.6f;
	public const float SunDogDamageShare = 0.8f;
	private Node2D sunDogGlow;
	private readonly SolarFlareVisual[] sunDogBursts = new SolarFlareVisual[2];
	private float sunDogClock;

	// ON THE SCREEN, AND GLIDING HOME (2026-10-30). Eric: "solar dogs + daybreak avatar means that solar dogs
	// are off the screen on a phone", and "they should be animated to return to you when you start moving if
	// they are currently drifted away. they shouldn't pop closer." Daybreak doubles the flare (90 to 180 px at
	// level 8) and a still wizard grows it 60% more, so the dogs hung up to 461 px out on the line through his
	// middle - and a phone shows 423 px either side of him. They now hang on the screen's LONG axis (either side
	// of him on a PC, above and below on a phone), never further out than SunDogScreenShare of the half view
	// along it, and the distance they hang at is eased (SunDogReturnRate): when he steps and the charge drops,
	// they glide back in over about half a second instead of jumping. The hit is struck where they are drawn,
	// at the eased distance.
	public const float SunDogScreenShare = 0.72f;
	private const float SunDogReturnRate = 5f;
	// How far a mock sun's light carries in a dark chapter: a few times its halo (DrawSunDogs).
	private const float SunDogLightReach = 64f;
	private float sunDogDistance = -1f;
	private Vector2 sunDogAxis = Vector2.Right;

	private bool HasSunDogs => SpellData != null && SpellData.HasEvolution(SunDogsId);

	// The camera's view in world px decides the axis and the cap. Without a camera (a harness), the line
	// through his middle and no cap, as before.
	private float SunDogTargetDistance()
	{
		float target = range * (1f + StillRadiusGrowth * StillCharge) * SunDogOffset;
		Camera2D camera = GetViewport()?.GetCamera2D();
		Vector2 view = GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero;
		if (camera == null || view.X <= 0f || camera.Zoom.X <= 0f || camera.Zoom.Y <= 0f)
		{
			sunDogAxis = Vector2.Right;
			return target;
		}
		Vector2 world = new Vector2(view.X / camera.Zoom.X, view.Y / camera.Zoom.Y);
		bool tall = world.Y > world.X;
		sunDogAxis = tall ? Vector2.Down : Vector2.Right;
		return MathF.Min(target, (tall ? world.Y : world.X) * 0.5f * SunDogScreenShare);
	}

	private void TickSunDogs(float delta)
	{
		if (!HasSunDogs)
		{
			if (sunDogGlow != null && IsInstanceValid(sunDogGlow) && sunDogGlow.Visible)
				sunDogGlow.Visible = false;
			sunDogDistance = -1f;
			return;
		}
		EnsureSunDogs();
		sunDogClock += delta;
		float target = SunDogTargetDistance();
		sunDogDistance = sunDogDistance < 0f
			? target
			: Mathf.Lerp(sunDogDistance, target, 1f - MathF.Exp(-SunDogReturnRate * delta));
		sunDogGlow.Visible = true;
		sunDogGlow.QueueRedraw();
		// Each mock sun is a small sun's light in a dark chapter (LightPool, .ai/light-pool.md section 3), where it hangs.
		if (LightPool.Lit)
		{
			LightPool.Light(GlobalPosition - sunDogAxis * sunDogDistance, SunDogLightReach, 0.6f);
			LightPool.Light(GlobalPosition + sunDogAxis * sunDogDistance, SunDogLightReach, 0.6f);
		}
	}

	private void EnsureSunDogs()
	{
		if (sunDogGlow != null && IsInstanceValid(sunDogGlow))
			return;
		// In the air, as the flare is (DrawLayers): a mock sun hangs in the sky, over the bodies.
		sunDogGlow = new Node2D { Name = "SunDogs", ZAsRelative = false, ZIndex = DrawLayers.Air };
		AddChild(sunDogGlow);
		sunDogGlow.Draw += DrawSunDogs;
		for (int i = 0; i < sunDogBursts.Length; i++)
		{
			sunDogBursts[i] = new SolarFlareVisual { Name = $"SunDogBurst{i}", RayCount = 10, EmberCount = 8 };
			AddChild(sunDogBursts[i]);
		}
	}

	private void FlareSunDogs(Player player, float reach, int pulseDamage)
	{
		if (!HasSunDogs)
			return;
		EnsureSunDogs();
		// Where they hang now (eased, on the screen's long axis), so each strikes where it is drawn.
		float distance = sunDogDistance >= 0f ? sunDogDistance : SunDogTargetDistance();
		float dogReach = reach * SunDogReach;
		float r2 = dogReach * dogReach;
		int dogDamage = Math.Max(1, Mathf.RoundToInt(pulseDamage * SunDogDamageShare));
		for (int side = 0; side < 2; side++)
		{
			Vector2 offset = sunDogAxis * (side == 0 ? -distance : distance);
			Vector2 centre = GlobalPosition + offset;
			SolarFlareVisual dogBurst = sunDogBursts[side];
			foreach (Enemy e in EnemyRegistry.Snapshot)
			{
				float d2 = IsInstanceValid(e) ? e.GlobalPosition.DistanceSquaredTo(centre) : float.MaxValue;
				if (d2 > r2)
					continue;
				// Rising under what this mock sun strikes, as the flare's own burst does.
				if (dogBurst != null && IsInstanceValid(dogBurst))
					dogBurst.RiseUnder(e, MathF.Sqrt(d2), dogReach);
				player?.DealDamageToEnemy(e, dogDamage, source: SpellData);
				if (WeakenFraction > 0f)
					e.ApplyWeaken(WeakenFraction, WeakenDuration * DurationMultiplier);
			}
			SolarFlareVisual burst = sunDogBursts[side];
			if (burst != null && IsInstanceValid(burst))
			{
				burst.Position = offset;
				burst.Burst(dogReach);
			}
		}
	}

	// Each mock sun between pulses: a halo, a glow and a white-gold kernel with four short rays turning
	// slowly, throbbing on its own beat. Small on purpose - it says where the next flare goes off, and the
	// burst says how far it reaches (2c: no ring at the reach).
	//
	// As pixel art since 2026-10-29: it was three translucent discs and four antialiased lines. Now every
	// part is a square of the world's 2 px grid in an opaque Light tone, stamped cell by cell round the
	// sun's snapped centre - the halo and the glow as Bayer dither, the rays as the cells they cross - so it
	// sits on the same grid as the wizard and the floor however he stands. A few dozen rects a sun.
	private static readonly ElementColors.Ramp LightRamp = ElementColors.GetRamp(Element.Light);

	private void DrawSunDogs()
	{
		const float grid = WhipLimb.Grid;
		float distance = sunDogDistance >= 0f ? sunDogDistance : SunDogTargetDistance();
		Vector2 here = sunDogGlow.GlobalPosition;
		for (int side = 0; side < 2; side++)
		{
			Vector2 at = sunDogAxis * (side == 0 ? -distance : distance);
			float throb = 0.5f + 0.5f * MathF.Sin(sunDogClock * 3.1f + side * 1.7f);
			float r = Mathf.Lerp(5f, 6.5f, throb);
			float turn = sunDogClock * 0.8f + side * 0.6f;
			// The sun's centre on a grid line of the world; every cell is then measured from it.
			Vector2 world = here + at;
			var centre = new Vector2(MathF.Round(world.X / grid) * grid, MathF.Round(world.Y / grid) * grid);
			int span = (int)MathF.Ceiling(r * 2.6f / grid);
			for (int j = -span; j < span; j++)
			{
				for (int i = -span; i < span; i++)
				{
					var p = new Vector2((i + 0.5f) * grid, (j + 0.5f) * grid);
					float d = p.Length();
					float b = PixelInk.Bayer((int)(centre.X / grid) + i, (int)(centre.Y / grid) + j);
					Color c;
					if (d < r)
						c = d < r * 0.55f ? LightRamp.Core : LightRamp.Hot;
					else if (OnSunDogRay(p, d, r, turn))
						c = LightRamp.Hot;
					else if (d < r * 1.6f && b < 0.62f)
						c = LightRamp.Mid;
					else if (d < r * 2.6f && b < 0.1f + 0.08f * throb)
						c = LightRamp.Edge;
					else
						continue;
					sunDogGlow.DrawRect(new Rect2(centre + new Vector2(i * grid, j * grid) - here, grid, grid), c);
				}
			}
		}
	}

	// Whether a cell lies on one of a mock sun's four turning rays: from just outside its kernel to a
	// little more than twice its radius, one art pixel wide.
	private static bool OnSunDogRay(Vector2 p, float d, float r, float turn)
	{
		if (d < r * 1.1f || d > r * 2.3f)
			return false;
		for (int k = 0; k < 4; k++)
		{
			Vector2 dir = Vector2.FromAngle(turn + Mathf.Tau * k / 4f);
			if (p.Dot(dir) > 0f && MathF.Abs(p.Cross(dir)) < WhipLimb.Grid * 0.75f)
				return true;
		}
		return false;
	}

	// SOLAR PROMINENCE (solar_flare_scorching_heat, 2h). The card was Scorching Heat: +45% damage and the
	// Fire tag, a hotter number. A prominence is the sun's own: a loop of burning gas thrown up off its
	// edge that falls back. Every pulse hurls ProminenceLoops of them off the flare's rim - one more at
	// half charge and another at full, so the sun held longest throws most - each at a different enemy
	// standing just past the flare's edge, nearest first, out to ProminenceReach of this pulse's reach.
	// Each arcs over and comes down on it in a burst of fire ProminenceBurstShare of the reach across, for
	// ProminenceShare of the pulse. The stand-still spell reaching the rank that hangs back from it. Drawn
	// in the air on a TopLevel canvas, so a loop in flight stays where it was thrown while the wizard walks
	// on; a fixed pool, and nothing allocated per pulse.
	public const string ProminenceId = "solar_flare_scorching_heat";
	public const int ProminenceLoops = 2;
	public const float ProminenceReach = 2.4f;
	public const float ProminenceShare = 0.8f;
	public const float ProminenceBurstShare = 0.4f;
	// Past this share of the reach (the flare already burns what is inside it), and thrown from this share
	// out from the wizard's middle.
	private const float ProminenceInner = 1.1f;
	private const float ProminenceRoot = 0.35f;
	private const float ProminenceFlight = 0.4f;
	private const float ProminenceFade = 0.14f;
	private const int MaxProminences = 16;

	private struct Prominence
	{
		public Vector2 From;
		public Vector2 To;
		public float Height;
		public float Age;
		public float Burst;
		public int Damage;
		public bool Landed;
		public bool Live;
	}

	private readonly Prominence[] prominences = new Prominence[MaxProminences];
	private readonly Enemy[] prominenceTargets = new Enemy[4];
	private readonly float[] prominenceDistances = new float[4];
	private int nextProminence;
	private bool prominencesDrawn;
	private Node2D prominenceCanvas;
	private static readonly ElementColors.Ramp FireRamp = ElementColors.GetRamp(Element.Fire);

	private void HurlProminences(float reach, int pulseDamage, float charge)
	{
		if (SpellData == null || !SpellData.HasEvolution(ProminenceId))
			return;
		int loops = Math.Min(prominenceTargets.Length, ProminenceLoops + (charge >= 0.5f ? 1 : 0) + (charge >= 0.999f ? 1 : 0));
		// The nearest few past the rim, nearest first, kept in fixed slots.
		int found = 0;
		float outer = reach * ProminenceReach;
		Vector2 origin = GlobalPosition;
		foreach (Enemy e in EnemyRegistry.Snapshot)
		{
			if (!IsInstanceValid(e) || e.IsDying)
				continue;
			float d = origin.DistanceTo(e.GlobalPosition);
			if (d <= reach * ProminenceInner || d > outer)
				continue;
			int slot;
			if (found < loops)
				slot = found++;
			else if (d < prominenceDistances[loops - 1])
				slot = loops - 1;
			else
				continue;
			while (slot > 0 && prominenceDistances[slot - 1] > d)
			{
				prominenceTargets[slot] = prominenceTargets[slot - 1];
				prominenceDistances[slot] = prominenceDistances[slot - 1];
				slot--;
			}
			prominenceTargets[slot] = e;
			prominenceDistances[slot] = d;
		}
		if (found == 0)
			return;

		EnsureProminenceCanvas();
		int damage = Math.Max(1, Mathf.RoundToInt(pulseDamage * ProminenceShare));
		float burst = reach * ProminenceBurstShare;
		for (int i = 0; i < found; i++)
		{
			Enemy target = prominenceTargets[i];
			prominenceTargets[i] = null;
			// Led by where it is walking for as long as the loop is in the air, as a meteor is.
			Vector2 to = target.GlobalPosition + target.Velocity * ProminenceFlight;
			Vector2 dir = (to - origin).Normalized();
			// Thrown up from the body of the sun, not its rim: a loop that left the rim for an enemy a step
			// past it went straight up and straight down, a spark and not an arch.
			Vector2 from = origin + dir * reach * ProminenceRoot;
			prominences[nextProminence] = new Prominence
			{
				From = from,
				To = to,
				Height = 22f + 0.5f * from.DistanceTo(to),
				Burst = burst,
				Damage = damage,
				Live = true,
			};
			nextProminence = (nextProminence + 1) % MaxProminences;
		}
	}

	private void EnsureProminenceCanvas()
	{
		if (prominenceCanvas != null && IsInstanceValid(prominenceCanvas))
			return;
		prominenceCanvas = new Node2D { Name = "Prominences", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Air };
		AddChild(prominenceCanvas);
		prominenceCanvas.GlobalPosition = Vector2.Zero;
		prominenceCanvas.Draw += DrawProminences;
	}

	private void TickProminences(float delta)
	{
		if (prominenceCanvas == null || !IsInstanceValid(prominenceCanvas))
			return;
		bool any = false;
		for (int i = 0; i < MaxProminences; i++)
		{
			Prominence p = prominences[i];
			if (!p.Live)
				continue;
			any = true;
			p.Age += delta;
			if (!p.Landed && p.Age >= ProminenceFlight)
			{
				p.Landed = true;
				LandProminence(p);
			}
			if (p.Age >= ProminenceFlight + ProminenceFade)
				p.Live = false;
			// Burning gas is a light in a dark chapter (LightPool): at its head in flight, out over its burst as it lands.
			else if (LightPool.Lit)
				LightPool.Light(ArcPoint(p, MathF.Min(1f, p.Age / ProminenceFlight)), p.Landed ? p.Burst * 2f : 56f, 0.7f);
			prominences[i] = p;
		}
		// One more redraw after the last has gone, to clear it.
		if (any || prominencesDrawn)
			prominenceCanvas.QueueRedraw();
		prominencesDrawn = any;
	}

	private void LandProminence(in Prominence p)
	{
		if (PlayerRef is not Player player || !IsInstanceValid(player))
			return;
		float r2 = p.Burst * p.Burst;
		foreach (Enemy e in EnemyRegistry.Snapshot)
		{
			if (IsInstanceValid(e) && e.GlobalPosition.DistanceSquaredTo(p.To) <= r2)
				player.DealDamageToEnemy(e, p.Damage, source: SpellData);
		}
		if (player.GetParent() is Node2D arena)
			ImpactDebrisLayer.Spawn(arena, p.To, p.Burst, FireRamp, Colors.White);
	}

	// A loop: a lob from the rim to the mark, rising and falling, its head white-hot and its tail cooling
	// down the fire ramp, stamped on the 2 px grid. After it lands the tail drains into the burst.
	private Vector2 ArcPoint(in Prominence p, float u) =>
		p.From.Lerp(p.To, u) + new Vector2(0f, -MathF.Sin(MathF.PI * u) * p.Height);

	private void DrawProminences()
	{
		const int steps = 14;
		for (int i = 0; i < MaxProminences; i++)
		{
			Prominence p = prominences[i];
			if (!p.Live)
				continue;
			float head = Mathf.Clamp(p.Age / ProminenceFlight, 0f, 1f);
			float drained = p.Landed ? Mathf.Clamp((p.Age - ProminenceFlight) / ProminenceFade, 0f, 1f) : 0f;
			float tail = Mathf.Max(0f, head - 0.55f + 0.55f * drained);
			for (int s = 0; s <= steps; s++)
			{
				float w = s / (float)steps;
				float u = Mathf.Lerp(tail, head, w);
				Color c = w > 0.85f ? FireRamp.Core : w > 0.6f ? FireRamp.Hot : w > 0.3f ? FireRamp.Mid : FireRamp.Edge;
				float size = MathF.Max(WhipLimb.Grid, MathF.Round(Mathf.Lerp(4f, 10f, w) / WhipLimb.Grid) * WhipLimb.Grid);
				prominenceCanvas.DrawRect(WhipLimb.Cell(ArcPoint(p, u), size), c);
			}
		}
	}

	// DRAGON'S TEETH (Medea's Apotheosis, the verb TURN; .ai/chapter-wizards.md 4.2). "At full growth the flare throws
	// a stone of light into the crowd: what it strikes turns on its neighbours for three seconds." In the story Medea
	// told Jason to throw one stone among the men sown from the dragon's teeth, and they fell on each other. Every pulse
	// that goes off at FULL growth - he has stood still the whole StillGrowthSeconds - throws one stone, a lob of the
	// sun's own light, onto the densest knot within TeethThrowReach of this pulse's reach; where it lands it strikes
	// everything within TeethStrikeRadius for TeethStoneShare of the pulse (through DealDamageToEnemy: it is his
	// spell), and turns the ordinary ones on their neighbours, nearest the stone first, up to TeethTurnPerStone of them
	// (Enemy.Turn; never past Enemy.MaxTurnedAtOnce in the arena). Elites are struck, not turned; a boss is struck.
	// The stand-still spell's form asks for the stillness it already rewards, and pays it in the crowd's own blows.
	// A fixed pool of stones on a TopLevel canvas, as the prominences are; nothing allocated per pulse.
	public const string DragonsTeethId = "solar_flare_dragons_teeth";
	public const float TeethThrowReach = 2.6f;
	public const float TeethStrikeRadius = 76f;
	public const int TeethTurnPerStone = 8;
	// A fifth of the pulse: a stone, not a bomb. At the first guess (the whole pulse) the stone killed most of what it
	// struck - the flare at level 8 with its Ascension one-shots the crowd - and so turned 0.43 a stone: the seek bot
	// standing still after the form threw 178 stones in six minutes and turned 77, whose 45 blows killed 23. What it
	// strikes must live to turn.
	public const float TeethStoneShare = 0.2f;
	public const float TeethFlight = 0.5f;
	private const float TeethFade = 0.3f;
	private const int MaxStones = 4;
	// How many knots are scored for the throw: the candidates are the first this many in reach.
	private const int TeethCandidates = 32;

	/// <summary>This run's stones, and what they did, for the harness and the shots.</summary>
	public static int StatStonesThrown { get; private set; }
	public static int StatBodiesTurned { get; private set; }
	public static int StatStonesStruckElites { get; private set; }

	/// <summary>Sets the tally back for a new run (Player._Ready, with Enemy.ResetTurnedTally).</summary>
	public static void ResetTeethTally()
	{
		StatStonesThrown = 0;
		StatBodiesTurned = 0;
		StatStonesStruckElites = 0;
	}

	private struct Stone
	{
		public Vector2 From;
		public Vector2 To;
		public float Height;
		public float Age;
		public int Damage;
		public bool Landed;
		public bool Live;
	}

	private readonly Stone[] stones = new Stone[MaxStones];
	private int nextStone;
	private bool stonesDrawn;
	private Node2D stoneCanvas;
	// Who the landing strikes, nearest the stone first, in fixed slots.
	private readonly Enemy[] stoneHits = new Enemy[24];
	private readonly float[] stoneHitDistances = new float[24];

	private bool HasDragonsTeeth => SpellData != null && SpellData.HasEvolution(DragonsTeethId);

	// The knot a stone is thrown at: of the first TeethCandidates in reach, the one with most bodies within the
	// stone's strike. Counted through the swarm's grid, a struct visitor - once a full-grown pulse, never a frame.
	private struct KnotCount : INeighbourVisitor
	{
		private readonly Vector2 origin;
		private readonly float radiusSquared;
		public int Count;

		public KnotCount(Vector2 origin, float radius)
		{
			this.origin = origin;
			radiusSquared = radius * radius;
			Count = 0;
		}

		public void Visit(Enemy enemy, Vector2 position)
		{
			if (!enemy.IsDying && origin.DistanceSquaredTo(position) <= radiusSquared)
				Count++;
		}
	}

	private void ThrowTeethStone(float reach, int pulseDamage, float charge)
	{
		if (!HasDragonsTeeth || charge < 0.999f)
			return;
		Vector2 origin = GlobalPosition;
		float throwReach = reach * TeethThrowReach;
		float r2 = throwReach * throwReach;
		Vector2 best = Vector2.Zero;
		int bestCount = 0;
		int scored = 0;
		foreach (Enemy e in EnemyRegistry.Snapshot)
		{
			if (scored >= TeethCandidates)
				break;
			if (!IsInstanceValid(e) || e.IsDying)
				continue;
			Vector2 at = e.GlobalPosition;
			if (at.DistanceSquaredTo(origin) > r2)
				continue;
			scored++;
			var knot = new KnotCount(at, TeethStrikeRadius);
			EnemyRegistry.ForEachNear(at, TeethStrikeRadius, ref knot);
			if (knot.Count > bestCount)
			{
				bestCount = knot.Count;
				best = at;
			}
		}
		if (bestCount == 0)
			return;

		EnsureStoneCanvas();
		Vector2 dir = (best - origin).LengthSquared() > 1f ? (best - origin).Normalized() : Vector2.Up;
		// Thrown from her hand, a little up from the middle of her: lobbed high so it is seen to come down.
		Vector2 from = origin + dir * 14f + new Vector2(0f, -30f);
		stones[nextStone] = new Stone
		{
			From = from,
			To = best,
			Height = 40f + 0.45f * from.DistanceTo(best),
			Damage = Math.Max(1, Mathf.RoundToInt(pulseDamage * TeethStoneShare)),
			Live = true,
		};
		nextStone = (nextStone + 1) % MaxStones;
		StatStonesThrown++;
	}

	private void EnsureStoneCanvas()
	{
		if (stoneCanvas != null && IsInstanceValid(stoneCanvas))
			return;
		stoneCanvas = new Node2D { Name = "TeethStones", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Air };
		AddChild(stoneCanvas);
		stoneCanvas.GlobalPosition = Vector2.Zero;
		stoneCanvas.Draw += DrawStones;
	}

	private void TickStones(float delta)
	{
		if (stoneCanvas == null || !IsInstanceValid(stoneCanvas))
			return;
		bool any = false;
		for (int i = 0; i < MaxStones; i++)
		{
			Stone s = stones[i];
			if (!s.Live)
				continue;
			any = true;
			s.Age += delta;
			if (!s.Landed && s.Age >= TeethFlight)
			{
				s.Landed = true;
				LandStone(s);
			}
			if (s.Age >= TeethFlight + TeethFade)
				s.Live = false;
			// A stone of the sun's light is a light in a dark chapter (LightPool): in flight, then out over its broken ring.
			else if (LightPool.Lit)
				LightPool.Light(StonePoint(s, MathF.Min(1f, s.Age / TeethFlight)), s.Landed ? TeethStrikeRadius * 1.4f : 48f,
					s.Landed ? 0.7f * (1f - (s.Age - TeethFlight) / TeethFade) : 0.6f);
			stones[i] = s;
		}
		// The turned are marked while any is (DrawTurned), so the canvas redraws for them too.
		any |= Enemy.TurnedCount > 0;
		if (any || stonesDrawn)
			stoneCanvas.QueueRedraw();
		stonesDrawn = any;
	}

	// WHO IS FIGHTING WHOM. A turned body is lit warm (Enemy.TurnedLook), but a tint on a dark body in a crowd is a
	// faint thing; so over each one, while it lasts, a small mark of the stone's light flickers at its head, and a
	// short run of light leans from it toward the one it is going for - a third of the way, never the whole line, so
	// a knot of them reads as bodies turned on each other and not as a web. At most Enemy.MaxTurnedAtOnce; nothing is
	// allocated, and the walk of the registry is skipped outright while nobody is turned.
	private void DrawTurned()
	{
		if (Enemy.TurnedCount <= 0)
			return;
		float t = (float)Time.GetTicksMsec() * 0.001f;
		foreach (Enemy e in EnemyRegistry.Snapshot)
		{
			if (!IsInstanceValid(e) || !e.IsTurned)
				continue;
			Vector2 at = e.GlobalPosition;
			Vector2 head = at + new Vector2(0f, -38f);
			bool bright = ((int)(t * 8f + e.GetInstanceId() % 5)) % 2 == 0;
			stoneCanvas.DrawRect(WhipLimb.Cell(head, 6f), bright ? LightRamp.Hot : LightRamp.Mid);
			stoneCanvas.DrawRect(WhipLimb.Cell(head, 2f), LightRamp.Core);
			if (e.TurnedTarget is not Enemy foe)
				continue;
			Vector2 to = foe.GlobalPosition - at;
			float d = to.Length();
			if (d < 1f)
				continue;
			Vector2 dir = to / d;
			float reach = MathF.Min(d * 0.35f, 46f);
			for (float k = 10f; k < reach; k += 6f)
				stoneCanvas.DrawRect(WhipLimb.Cell(at + new Vector2(0f, -14f) + dir * k, WhipLimb.Grid * 2f),
					k < reach * 0.5f ? LightRamp.Hot : LightRamp.Edge);
		}
	}

	// The stone comes down: everything under it struck, then the ordinary ones nearest it turned.
	private void LandStone(in Stone s)
	{
		if (PlayerRef is not Player player || !IsInstanceValid(player))
			return;
		int found = 0;
		float r2 = TeethStrikeRadius * TeethStrikeRadius;
		foreach (Enemy e in EnemyRegistry.Snapshot)
		{
			if (!IsInstanceValid(e) || e.IsDying)
				continue;
			float d2 = e.GlobalPosition.DistanceSquaredTo(s.To);
			if (d2 > r2)
				continue;
			int slot;
			if (found < stoneHits.Length)
				slot = found++;
			else if (d2 < stoneHitDistances[found - 1])
				slot = found - 1;
			else
				continue;
			while (slot > 0 && stoneHitDistances[slot - 1] > d2)
			{
				stoneHits[slot] = stoneHits[slot - 1];
				stoneHitDistances[slot] = stoneHitDistances[slot - 1];
				slot--;
			}
			stoneHits[slot] = e;
			stoneHitDistances[slot] = d2;
		}
		int turned = 0;
		for (int i = 0; i < found; i++)
		{
			Enemy e = stoneHits[i];
			stoneHits[i] = null;
			if (!IsInstanceValid(e) || e.IsDying)
				continue;
			player.DealDamageToEnemy(e, s.Damage, source: SpellData);
			if (!IsInstanceValid(e) || e.IsDying)
				continue;
			if (e.IsMiniBoss || e is BossEnemy)
			{
				StatStonesStruckElites++;
				continue;
			}
			if (turned < TeethTurnPerStone && e.Turn(Enemy.TurnSeconds))
			{
				turned++;
				StatBodiesTurned++;
			}
		}
		if (player.GetParent() is Node2D arena)
			ImpactDebrisLayer.Spawn(arena, s.To, TeethStrikeRadius, LightRamp, Colors.White);
		SfxPlayer.Impact(Element.Light, s.To);
	}

	// A stone of light in flight: a small hard stone, white at its heart and the light ramp's hot stop round it, its
	// trail falling behind it down the ramp, lobbed high and coming down on the knot; once down, a broken ring of light
	// runs out over the strike's reach and fades. Stamped on the 2 px grid, as the prominences are.
	private static Vector2 StonePoint(in Stone s, float u) =>
		s.From.Lerp(s.To, u) + new Vector2(0f, -MathF.Sin(MathF.PI * u) * s.Height);

	private void DrawStones()
	{
		DrawTurned();
		const int trail = 9;
		for (int i = 0; i < MaxStones; i++)
		{
			Stone s = stones[i];
			if (!s.Live)
				continue;
			if (!s.Landed)
			{
				float head = Mathf.Clamp(s.Age / TeethFlight, 0f, 1f);
				for (int k = trail; k >= 1; k--)
				{
					float u = Mathf.Max(0f, head - k * 0.035f);
					float w = 1f - k / (float)trail;
					Color c = w > 0.6f ? LightRamp.Hot : w > 0.3f ? LightRamp.Mid : LightRamp.Edge;
					float size = MathF.Max(WhipLimb.Grid, MathF.Round(Mathf.Lerp(2f, 6f, w) / WhipLimb.Grid) * WhipLimb.Grid);
					stoneCanvas.DrawRect(WhipLimb.Cell(StonePoint(s, u), size), c);
				}
				// The stone itself: a white-hot heart in a ring of the ramp's hot stop.
				stoneCanvas.DrawRect(WhipLimb.Cell(StonePoint(s, head), 8f), LightRamp.Hot);
				stoneCanvas.DrawRect(WhipLimb.Cell(StonePoint(s, head), 4f), LightRamp.Core);
				continue;
			}
			// Landed: a broken ring of light out over what it struck, cooling down the ramp as it goes.
			float f = Mathf.Clamp((s.Age - TeethFlight) / TeethFade, 0f, 1f);
			float radius = Mathf.Lerp(TeethStrikeRadius * 0.35f, TeethStrikeRadius, f);
			Color ring = f < 0.4f ? LightRamp.Hot : f < 0.75f ? LightRamp.Mid : LightRamp.Edge;
			const int steps = 28;
			for (int k = 0; k < steps; k++)
			{
				if ((k + (int)(f * 6f)) % 3 == 2)
					continue;
				Vector2 at = s.To + Vector2.FromAngle(Mathf.Tau * k / steps) * radius;
				stoneCanvas.DrawRect(WhipLimb.Cell(at, WhipLimb.Grid * 2f), ring);
			}
		}
	}

	private void UpdateRingPulse(float delta)
	{
		if (!ringPulseActive || visual == null)
			return;

		ringPulseTimer += delta;
		float expand = Mathf.Max(0.01f, RingExpandDuration);
		float retract = Mathf.Max(0.01f, RingRetractDuration);
		float total = expand + retract;

		if (ringPulseTimer < expand)
		{
			float t = ringPulseTimer / expand;
			visual.Radius = Mathf.Lerp(2f, burstReach, t);
			visual.RingWidth = Mathf.Lerp(4f, 10f, t);
			visual.ShapeColor = new Color(VisualColor.R, VisualColor.G, VisualColor.B, Mathf.Lerp(0.55f, 0.9f, t) * VisualColor.A);
			visual.QueueRedraw();
			return;
		}

		if (ringPulseTimer < total)
		{
			float t = (ringPulseTimer - expand) / retract;
			visual.Radius = burstReach;
			visual.RingWidth = Mathf.Lerp(10f, 1f, t);
			visual.ShapeColor = new Color(VisualColor.R, VisualColor.G, VisualColor.B, Mathf.Lerp(0.9f, 0.12f, t) * VisualColor.A);
			visual.QueueRedraw();
			return;
		}

		ringPulseActive = false;
		visual.RingWidth = 0.01f;
		visual.ShapeColor = new Color(VisualColor.R, VisualColor.G, VisualColor.B, 0.0f);
		visual.QueueRedraw();
	}

	public void SetSpellLevel(int level)
	{
		CurrentLevel = Math.Max(1, level);
		RefreshComputedStats();
	}

	private void RefreshComputedStats()
	{
		damage = Math.Max(1, Mathf.RoundToInt((SpellData?.GetDamageAtLevel(CurrentLevel) ?? 5) * DamageMultiplier));
		// The evolutions' Area as well as the player's. Radiant Glow and Daybreak promised a bigger
		// flare and this read only the player's, so both did nothing to its size until 2026-10-26.
		range = (SpellData?.GetRangeAtLevel(CurrentLevel) ?? 100f) * AreaMultiplier * (SpellData?.GetAreaMultiplierAtLevel(CurrentLevel) ?? 1f)
			* (1f + (SpellData?.GetEffectValueAtLevel(SpellEffect.AreaSize, CurrentLevel) ?? 0f)); // the level table's Area line
		cooldown = MathF.Max(0.05f, (SpellData?.GetCooldownAtLevel(CurrentLevel) ?? 1.5f) * CooldownMultiplier);
	}
}
