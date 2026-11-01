using Godot;
using System;

namespace WizardSurvivors.scripts;

// Meteor Swarm, one meteor of a volley: it streaks in from the upper left onto a point, and on
// landing hits everything within explosionRadius, opens a crater in the ground and throws burning rock
// out to exactly that radius.
//
// THE LOOK IS THE WEB DEMO'S (2026-10-25: "meteor swarm should look like the meteors on the web based
// demo version"). SurviveDemo.tsx draws a meteor as a tiny pixel-art head with a short stepped trail -
// four squares in the fire ramp, white-hot at the head, stepping back up a diagonal from the upper
// left - and the landing as the meteor-impact crater. That is what this draws, at the game's scale:
// the demo's numbers in demo pixels times 2.5 world px for the path and 3 for the squares (the demo
// shows its sprites at half the game's x2; the rest is so the head still reads on a phone), every
// square stamped on the 2 px art grid with no smoothing. The meteor used to be the crater sprite
// itself, tinted, rotated and slid straight down - an orange smear that read as nothing in particular.
//
// What the demo draws and this does not: its flickering red ellipse on the landing point for the
// whole wait. That is an area marker, and the player's spells carry none (.ai/art-direction.md,
// section 3; .ai/spell-roster.md, 2c). The streak is the warning; the reach is shown by the crater and
// the debris at the moment of the hit, both sized from the radius the hit tests and neither drawn past it.
//
// THE CRATER IS ON THE GROUND (2026-10-28: "the crater from meteor should not cover other elements
// that would stand on top of the ground, like the player ... fires from meteor and the crater should
// look better and more detailed"). It used to be the demo's crater sprite flashed for a third of a
// second on this node, at z 6 - over every actor, so a meteor landing beside the wizard painted an
// orange ellipse across him - and Magma Craters' fire was Ember Caps' lick of flame stamped in rings and
// multiplied flat red by the evolution's tint. Now the landing opens a crater on CraterLayer, under the
// actors: a scorched bowl with a lit, lumpy rim, cracks glowing out from a molten floor that crusts over
// as it cools, soot and rocks thrown out in rays (scenes/shaders/crater.gdshader), and a short blaze in
// the bowl with smoke and embers off it (CraterFire); Magma Craters keeps it molten and fills the burning
// radius with fire for as long as it burns. What is in the AIR stays on this node, over the actors: the
// streak, and the debris thrown up out of the impact.
//
// One node per meteor, drawn in code: a few rects in flight and the debris on landing, nothing
// allocated per frame; the crater outlives it on the shared layer. The volley's stagger is the delay
// CastAt is given - the node waits invisible, then falls.
public partial class MeteorImpact : Node2D
{
	[Export] public SpellData SpellData { get; set; }
	[Export] public int CurrentLevel { get; set; } = 1;
	[Export] public float BaseExplosionRadius { get; set; } = 60f;
	// Seconds in the sky, entry to landing. The demo's 0.45.
	[Export] public float FallSeconds { get; set; } = 0.45f;
	// World px per demo px: PixelScale for the path, TrailScale for the squares. See the header. The
	// squares are the chunkier of the two so the head is still a few device pixels on a phone, where
	// the camera shows the same ground on a much smaller screen.
	[Export] public float PixelScale { get; set; } = 2.5f;
	[Export] public float TrailScale { get; set; } = 3f;

	public float DamageMultiplier { get; set; } = 1.0f;
	public float AreaMultiplier { get; set; } = 1.0f;
	public Node2D PlayerRef;

	// Where the meteor enters, from where it lands, in demo px: 60 left and 140 up (SurviveDemo.tsx).
	private static readonly Vector2 FallFrom = new Vector2(-60f, -140f);
	// The trail, head first: the demo's square sizes in demo px, and how far apart they sit up the
	// path (its step of 3 across and 7 up, which is the path's own slope).
	private static readonly float[] TrailSizes = { 4f, 4f, 3f, 3f };
	private static readonly float TrailSpacing = new Vector2(3f, 7f).Length();

	// Magma Craters (meteor_magma_crater, SpellEffect.Burn = seconds): the crater keeps burning, and
	// hurts what stands in it every tick for a share of the impact. The card said so and nothing read it.
	// The fire filling it is CraterLayer's, laid out to the same explosionRadius these ticks test.
	private const float MagmaTickSeconds = 0.5f;
	private const float MagmaTickShare = 0.25f;

	private static readonly ElementColors.Ramp FireRamp = ElementColors.GetRamp(Element.Fire);


	// PHASE 2 (.ai/spell-roster.md 2h, 2026-10-31). Three cards change what one meteor does, read by id when
	// it is cast: Breakup breaks it apart in the sky into fragments that land round its mark; Skipstone
	// glances off the ground and bounds on to land again; Extinction Event's finale (Finale, set by
	// Player.FireMeteorSwarm) is one colossal meteor at the end of the volley. A meteor's landing spots
	// are a few fixed slots (offsets from its node), so one node draws and hits all of them.
	public const string BreakupId = "meteor_cluster";
	public const string SkipstoneId = "meteor_starcrash";
	public const string ExtinctionId = "meteor_extinction_event";
	public const string BarrageId = "meteor_orbital_bombardment";

	/// <summary>Breakup: fragments a meteor breaks into, how far from its mark they land (blast radii), each one's share of the blast and of the damage.</summary>
	public const int BreakupFragments = 3;
	public const float BreakupSpread = 0.8f;
	public const float BreakupRadiusShare = 0.65f;
	public const float BreakupDamageShare = 0.8f;
	// The share of the fall at which it breaks apart.
	private const float BreakupAt = 0.55f;

	/// <summary>Skipstone: how far the stone bounds on (blast radii), how long it is in the air, and the second landing's share of the damage and the blast.</summary>
	public const float SkipDistance = 1.5f;
	public const float SkipSeconds = 0.3f;
	public const float SkipDamageShare = 0.9f;
	public const float SkipRadiusShare = 0.9f;

	/// <summary>Extinction Event's finale: its blast and damage against a volley meteor's, and its fall.</summary>
	public const float FinaleArea = 2.5f;
	public const float FinaleDamage = 3f;
	private const float FinaleFallSeconds = 0.75f;

	/// <summary>Set before the meteor enters the tree: the colossal last meteor of an Extinction Event volley.</summary>
	public bool Finale { get; set; }

	/// <summary>The blast radius this meteor hits at (before a fragment's or a skip's share).</summary>
	public float Radius => explosionRadius;

	/// <summary>A volley meteor's blast before any Area: the scene's base radius (60) with the level table's Area line.</summary>
	public static float BaseRadiusFor(SpellData spell) =>
		60f * (1f + (spell?.GetEffectValueAtLevel(SpellEffect.AreaSize, spell.CurrentLevel) ?? 0f));

	private const int MaxPoints = BreakupFragments;
	private readonly Vector2[] points = new Vector2[MaxPoints];
	private int pointCount = 1;
	private float pointRadius;
	private int pointDamage;
	private bool skips;
	private bool skipped;
	private Vector2 skipTo;
	private float skipRadius;

	private int damage = 8;
	private float explosionRadius = 60f;
	private float meteorScale = 1f;
	private float burnSeconds;
	private int burnDamage;
	private float knockbackSpeed;
	private float burnTick;
	// Counts up from -delay: below zero the meteor is still waiting its turn in the volley.
	private float age;
	private bool impacted;
	private float sinceImpact;
	private float lifeAfterImpact;

	public override void _Ready()
	{
		// The streak and the debris are in the air: over the bodies it lands among, as the debris layer
		// is; under the HUD. The crater and its fire are on the ground, on CraterLayer.
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		RefreshComputedStats();
	}

	// The spell's tint - an evolution's colour, a Legendary's gold (Player.ApplyLegendaryVisual) - taken off
	// this node's modulate when the meteor is cast, and handed on softened. As a modulate it multiplied
	// the streak and the debris at full strength: Magma Craters' (1, 0.35, 0.1) turned the white-hot head
	// red and the whole streak one flat colour (2026-10-29). Now they take the share the crater's fire takes
	// (CraterLayer.FireTintShare) - enough to say which evolution, never enough to put out the core.
	private const float TintShare = 0.3f;
	private Color spellTint = Colors.White;
	private Color drawTint = Colors.White;

	private void TakeTint()
	{
		if (Modulate == Colors.White)
			return;
		spellTint = Modulate;
		drawTint = Colors.White.Lerp(spellTint, TintShare);
		Modulate = Colors.White;
	}

	public void SetSpellLevel(int level)
	{
		CurrentLevel = Math.Max(1, level);
		RefreshComputedStats();
	}

	/// <summary>Lands on <paramref name="position"/> FallSeconds after <paramref name="delay"/> has passed.</summary>
	public void CastAt(Vector2 position, float delay = 0f)
	{
		TakeTint();
		GlobalPosition = position;
		age = -MathF.Max(0f, delay);
		impacted = false;
		skipped = false;
		sinceImpact = 0f;
		RefreshComputedStats();
		LayOutPoints(position);
		QueueRedraw();
	}

	// Where this meteor comes down: its mark, or Breakup's fragments round it (none on blocked ground -
	// a fragment over water goes to the mark instead); and where Skipstone's bound will land, if it can.
	private void LayOutPoints(Vector2 at)
	{
		pointCount = 1;
		points[0] = Vector2.Zero;
		pointRadius = explosionRadius;
		pointDamage = damage;
		MazeNavigation nav = MazeNavigation.Active;
		if (!Finale && SpellData != null && SpellData.HasEvolution(BreakupId))
		{
			pointCount = BreakupFragments;
			pointRadius = explosionRadius * BreakupRadiusShare;
			pointDamage = Math.Max(1, Mathf.RoundToInt(damage * BreakupDamageShare));
			float spin = (float)(GetInstanceId() % 360) * 0.0174533f;
			for (int k = 0; k < BreakupFragments; k++)
			{
				float a = spin + Mathf.Tau * k / BreakupFragments;
				Vector2 off = new Vector2(MathF.Cos(a), MathF.Sin(a) * CraterGround.Squash) * explosionRadius * BreakupSpread;
				points[k] = nav != null && nav.IsBlockedAt(at + off) ? Vector2.Zero : off;
			}
		}
		skips = !Finale && SpellData != null && SpellData.HasEvolution(SkipstoneId);
		if (skips)
		{
			// On along the fall, the way a stone thrown in at a slant goes on.
			skipTo = -FallFrom.Normalized() * explosionRadius * SkipDistance;
			skipRadius = explosionRadius * SkipRadiusShare;
			if (nav != null && nav.IsBlockedAt(at + skipTo))
				skips = false;
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (!impacted)
		{
			age += dt;
			if (age < 0f)
				return;
			if (age >= FallSeconds)
				Impact();
			QueueRedraw();
			return;
		}

		sinceImpact += dt;
		if (burnSeconds > 0f && sinceImpact < burnSeconds)
		{
			burnTick -= dt;
			if (burnTick <= 0f)
			{
				burnTick += MagmaTickSeconds;
				for (int k = 0; k < pointCount; k++)
					HitWithin(points[k], pointRadius, burnDamage);
			}
		}
		if (skips && !skipped && sinceImpact >= SkipSeconds)
			Skip();

		if (sinceImpact >= lifeAfterImpact)
		{
			QueueFree();
			return;
		}
		// Past the debris and the bound there is nothing of this node's left to draw; a Magma Crater's node
		// stays only to tick its burn.
		if (sinceImpact - dt < ImpactDebris.LifeSeconds + (skips ? SkipSeconds : 0f))
			QueueRedraw();
	}

	public override void _Draw()
	{
		if (!impacted)
		{
			if (age >= 0f)
				DrawFall(Mathf.Clamp(age / MathF.Max(0.05f, FallSeconds), 0f, 1f));
			return;
		}

		// Burning rock thrown out of each landing to exactly the radius it hit.
		uint seed = (uint)(GetInstanceId() % 9973);
		for (int k = 0; k < pointCount; k++)
			ImpactDebris.Draw(this, points[k], pointRadius, sinceImpact, seed + (uint)k * 31u,
				FireRamp, ImpactDebris.CountFor(pointRadius), drawTint);
		if (!skips)
			return;
		if (!skipped)
		{
			// The stone bounding on: a lob from the crater to where it lands, the head and a short tail.
			float k = Mathf.Clamp(sinceImpact / SkipSeconds, 0f, 1f);
			Vector2 head = skipTo * k + new Vector2(0f, -MathF.Sin(MathF.PI * k) * explosionRadius * 0.45f);
			Vector2 back = (-skipTo).Normalized() * TrailSpacing * TrailScale * 0.8f;
			DrawStreakAt(head, back, 0.8f);
		}
		else
			ImpactDebris.Draw(this, skipTo, skipRadius, sinceImpact - SkipSeconds, seed + 97u,
				FireRamp, ImpactDebris.CountFor(skipRadius), drawTint);
	}

	// The fall: the demo's streak down to the mark, or - for Breakup - down to where it breaks apart, and
	// then a smaller streak to each fragment's spot, diverging from that point.
	private void DrawFall(float k)
	{
		Vector2 up = FallFrom.Normalized() * TrailSpacing * TrailScale * meteorScale;
		if (pointCount <= 1 || k < BreakupAt)
		{
			DrawStreakAt(FallFrom * PixelScale * (1f - k), up, meteorScale);
			return;
		}
		Vector2 split = FallFrom * PixelScale * (1f - BreakupAt);
		float u = (k - BreakupAt) / (1f - BreakupAt);
		for (int p = 0; p < pointCount; p++)
		{
			Vector2 dir = points[p] - split;
			Vector2 back = -dir.Normalized() * TrailSpacing * TrailScale * 0.7f;
			DrawStreakAt(split + dir * u, back, 0.7f);
		}
	}

	// The demo's meteor at `head`: three squares behind it stepping back along `back`, cooling down the
	// ramp. A bigger rock, not a longer fall: the path is the same on screen whatever the blast.
	private void DrawStreakAt(Vector2 head, Vector2 back, float scale)
	{
		float unit = TrailScale * scale;
		Vector2 step = back.Normalized() * TrailSpacing * unit;
		for (int i = TrailSizes.Length - 1; i >= 0; i--)
		{
			// The head stays white-hot whatever the evolution: the tint is identity, and identity lives in
			// the cooler squares behind it (.ai/art-direction.md section 3).
			Color c = i switch { 0 => FireRamp.Core, 1 => FireRamp.Hot * drawTint, 2 => FireRamp.Mid * drawTint, _ => FireRamp.Edge * drawTint };
			// Whole art pixels, rounded up: the demo's 4 and 3 stay two different sizes at any scale.
			float size = MathF.Max(WhipLimb.Grid, MathF.Ceiling(TrailSizes[i] * unit / WhipLimb.Grid) * WhipLimb.Grid);
			DrawRect(WhipLimb.Cell(head + step * i, size), c);
		}
	}

	private void Impact()
	{
		if (impacted)
			return;
		impacted = true;
		sinceImpact = 0f;
		burnTick = MagmaTickSeconds;
		lifeAfterImpact = MathF.Max(ImpactDebris.LifeSeconds + (skips ? SkipSeconds : 0f), burnSeconds) + 0.02f;

		// The group, like every AoE (CLAUDE.md), read through EnemyRegistry - the group without the
		// native array (.ai/performance.md). Under the balance harness the scene's direct children are
		// not the enemies, which is why it once hit nothing.
		for (int k = 0; k < pointCount; k++)
			HitWithin(points[k], pointRadius, pointDamage, knockbackSpeed);

		// The crater, on the ground under the actors, sized from the same radius: its ejecta stops short
		// of it, and a Magma Crater's fire fills it for as long as the burn ticks. This node's modulate is
		// the evolution's or a Legendary's tint (Player.ApplyLegendaryVisual), taken off it at the cast
		// (TakeTint); the layer softens it for the fire and keeps it off the ground.
		if (GetParent() is Node2D arena)
		{
			for (int k = 0; k < pointCount; k++)
				CraterLayer.Open(arena, GlobalPosition + points[k], pointRadius, burnSeconds, spellTint);
		}
	}

	// Skipstone's second landing: a smaller crater a bound further on, no burn of its own.
	private void Skip()
	{
		skipped = true;
		HitWithin(skipTo, skipRadius, Math.Max(1, Mathf.RoundToInt(damage * SkipDamageShare)), knockbackSpeed);
		if (GetParent() is Node2D arena)
			CraterLayer.Open(arena, GlobalPosition + skipTo, skipRadius, 0f, spellTint);
	}

	// knockbackSpeed is the impact's shockwave (the level table's and any card's KnockbackBonus); the
	// crater's burn ticks pass 0 and only hurt. `offset` is from this node.
	private void HitWithin(Vector2 offset, float radius, int amount, float knockbackSpeed = 0f)
	{
		if (PlayerRef is not Player player || !IsInstanceValid(player))
			return;
		Vector2 at = GlobalPosition + offset;
		float r2 = radius * radius;
		foreach (var enemy in EnemyRegistry.Snapshot)
		{
			if (!IsInstanceValid(enemy) || at.DistanceSquaredTo(enemy.GlobalPosition) > r2)
				continue;
			player.DealDamageToEnemy(enemy, amount, source: SpellData);
			if (knockbackSpeed > 0f && enemy is Enemy shoved)
			{
				Vector2 away = enemy.GlobalPosition - at;
				shoved.ApplyKnockback((away.LengthSquared() > 0.01f ? away.Normalized() : Vector2.Right) * knockbackSpeed);
			}
		}
	}

	private void RefreshComputedStats()
	{
		damage = Math.Max(1, Mathf.RoundToInt((SpellData?.GetDamageAtLevel(CurrentLevel) ?? 8) * DamageMultiplier * (Finale ? FinaleDamage : 1f)));
		// Level 3's "Area +0.16" is a fraction of the blast, as Glacial Spike reads it. Nothing read it here.
		float areaBonus = SpellData?.GetEffectValueAtLevel(SpellEffect.AreaSize, CurrentLevel) ?? 0f;
		explosionRadius = MathF.Max(8f, BaseExplosionRadius * (1f + areaBonus) * AreaMultiplier * (Finale ? FinaleArea : 1f));
		// A wider blast is a bigger rock - the finale is a visibly heavier meteor - but by the square root,
		// so the streak stays a streak.
		meteorScale = Mathf.Clamp(MathF.Sqrt(explosionRadius / MathF.Max(1f, BaseExplosionRadius)), 1f, Finale ? 3f : 2.2f);
		if (Finale)
			FallSeconds = FinaleFallSeconds;
		burnSeconds = SpellData?.GetEffectValueAtLevel(SpellEffect.Burn, CurrentLevel) ?? 0f;
		burnDamage = Math.Max(1, Mathf.RoundToInt(damage * MagmaTickShare));
		knockbackSpeed = SpellData?.GetKnockbackSpeedAtLevel(CurrentLevel) ?? 0f;
	}
}
