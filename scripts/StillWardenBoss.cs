using Godot;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// Chapter 6, the Frozen Waste: THE STILL WARDEN.
//
// It does not move and it does not reach. It is a thing frozen into the ground that has been
// standing there long enough to have made the ground its own, and the fight is against the arena
// rather than against the body in the middle of it.
//
// THREE LAYERS, AND ONLY THE THIRD IS THE BOSS.
//
//   ICE FALLS on marked ground, in volleys, wherever the player is standing. That is the layer
//   that punishes standing still, and it is the reason the fight has any pressure at all given
//   that the boss itself cannot follow anyone anywhere.
//
//   GUARDS are posted around it and only strike the wedge in front of them. They are ordinary
//   SlammerEnemies with the telegraph shape flipped to a cone, which is the whole implementation -
//   the "guards that only attack in the direction they face" idea needed no new enemy, because
//   the difference between it and a slammer is the shape of one warning.
//
//   THE WARDEN ITSELF only hits the ring it is standing in, and hard. It is a wall around a prize.
//
// WHY A BOSS THAT CANNOT CHASE. Every other boss in the campaign closes distance, so every other
// boss is answered by kiting, and by chapter six kiting is the only thing the player has been
// asked to do. This one inverts it: the safe ground is the ground away from the boss, and the
// boss is the only place worth being. The player has to come to it and stay, which is the exact
// thing every previous fight trained them out of.
public partial class StillWardenBoss : BossEnemy
{
	[Export] public float IceVolleyIntervalSeconds { get; set; } = 3.4f;
	[Export] public int ShardsPerVolley { get; set; } = 3;
	[Export] public float IceTellSeconds { get; set; } = 1.0f;
	[Export] public float IceShardRadius { get; set; } = 66f;
	[Export] public int IceShardDamage { get; set; } = 4;

	/// <summary>How far a shard may land from the player. Zero would be undodgeable.</summary>
	[Export] public float IceScatterRadius { get; set; } = 110f;

	[Export] public string GuardScenePath { get; set; } = "res://scenes/RimeGuard.tscn";
	[Export] public int GuardCount { get; set; } = 3;
	[Export] public float GuardPostRadius { get; set; } = 150f;
	[Export] public float GuardRepostSeconds { get; set; } = 14f;
	[Export] public float GuardHealthFraction { get; set; } = 0.05f;

	[Export] public Color RimeColor { get; set; } = new Color(0.55f, 0.82f, 0.95f);

	private PackedScene guardScene;
	private readonly List<Enemy> guards = new();
	private float untilVolley;
	private float untilRepost;

	public override void _Ready()
	{
		base._Ready();
		untilVolley = IceVolleyIntervalSeconds;
		untilRepost = 0f;

		guardScene = ResourceLoader.Load<PackedScene>(GuardScenePath);
		if (guardScene == null)
			GD.PushError($"StillWardenBoss: guard scene not found at '{GuardScenePath}', so it stands unguarded.");
	}

	protected override GroundSlamAttack BuildSlam() =>
		new(SlamIntervalSeconds, SlamTelegraphSeconds, SlamRadius, SlamDamage, RimeColor);

	// TWO PHASES (2026-10-31; it had the one stock enrage). At ColdHealthFraction the cold closes in:
	// heavier volleys, and the guards come back sooner and one more of them. At EnrageHealthFraction,
	// WHITEOUT: the stock enrage, and every few seconds a ring of ice is marked round the Warden at
	// WhiteoutRadius - outside its claim - so the ground between the wide and the near is no longer
	// safe and the place to be is at its feet, which is the thing this fight asks.
	[Export] public float ColdHealthFraction { get; set; } = 0.66f;
	[Export] public float WhiteoutIntervalSeconds { get; set; } = 6.5f;
	[Export] public float WhiteoutRadius { get; set; } = 250f;
	[Export] public int WhiteoutShards { get; set; } = 12;
	[Export] public float WhiteoutTellSeconds { get; set; } = 1.3f;

	private bool whiteout;
	private float untilWhiteout;

	protected override float[] BuildPhaseThresholds() => new[] { ColdHealthFraction, EnrageHealthFraction };

	protected override void OnPhaseEntered(int phase)
	{
		if (phase == 1)
		{
			IceVolleyIntervalSeconds *= 0.75f;
			ShardsPerVolley += 1;
			GuardCount += 1;
			GuardRepostSeconds *= 0.7f;
			untilRepost = Mathf.Min(untilRepost, 1.0f);
			SetBaseModulate(new Color(0.84f, 0.95f, 1.0f));
			return;
		}

		ApplyEnrage();
		// The enrage has to land somewhere other than movement speed on a boss that never moves.
		IceVolleyIntervalSeconds *= 0.8f;
		ShardsPerVolley += 1;
		whiteout = true;
		untilWhiteout = 1.5f;
	}

	private void TickWhiteout(float delta)
	{
		if (!whiteout || IsDying)
			return;
		untilWhiteout -= delta;
		if (untilWhiteout > 0f)
			return;
		untilWhiteout = WhiteoutIntervalSeconds;
		Node arena = GetParent();
		if (arena == null || !IsInstanceValid(arena))
			return;
		int count = Mathf.Max(3, WhiteoutShards);
		float turn = GD.Randf() * Mathf.Tau;
		for (int i = 0; i < count; i++)
		{
			Vector2 at = GlobalPosition + Vector2.FromAngle(turn + Mathf.Tau * i / count) * WhiteoutRadius;
			TelegraphedGroundHit.Place(arena, at, WhiteoutTellSeconds, IceShardRadius, ScaleOutgoingDamage(IceShardDamage), RimeColor, owner: this);
		}
	}

	// Frozen in. It has no chase and no drift, and returning Zero also keeps the separation nudge
	// from sliding it out of the ring it is defending.
	protected override Vector2 AdjustSteering(Vector2 chaseDirection, float distanceToPlayer) => Vector2.Zero;

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		TickIce((float)delta);
		TickWhiteout((float)delta);
		TickGuards((float)delta);
	}

	private void TickIce(float delta)
	{
		if (IsDying)
			return;

		untilVolley -= delta;
		if (untilVolley > 0f)
			return;

		untilVolley = IceVolleyIntervalSeconds;

		Node2D target = TargetPlayer;
		Node arena = GetParent();
		if (target == null || !IsInstanceValid(target) || arena == null || !IsInstanceValid(arena))
			return;

		for (int i = 0; i < Mathf.Max(1, ShardsPerVolley); i++)
		{
			// Scattered around where the player IS, not where they will be. Leading the shot would
			// punish the correct response, which is to keep moving.
			float angle = GD.Randf() * Mathf.Tau;
			float distance = Mathf.Sqrt(GD.Randf()) * IceScatterRadius;
			Vector2 where = target.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
			TelegraphedGroundHit.Place(arena, where, IceTellSeconds, IceShardRadius, ScaleOutgoingDamage(IceShardDamage), RimeColor, owner: this);
		}
	}

	private void TickGuards(float delta)
	{
		for (int i = guards.Count - 1; i >= 0; i--)
		{
			Enemy guard = guards[i];
			if (guard == null || !IsInstanceValid(guard) || !guard.IsInGroup("enemies"))
				guards.RemoveAt(i);
		}

		untilRepost -= delta;
		if (untilRepost > 0f || IsDying)
			return;

		untilRepost = GuardRepostSeconds;
		PostGuards();
	}

	// Refills the posts rather than adding to them, so clearing the guards buys a real window and
	// the arena never silently accumulates a wall of them.
	private void PostGuards()
	{
		Node arena = GetParent();
		if (guardScene == null || arena == null || !IsInstanceValid(arena))
			return;

		int wanted = Mathf.Max(0, GuardCount - guards.Count);
		for (int i = 0; i < wanted; i++)
		{
			if (guardScene.Instantiate() is not Enemy guard)
			{
				GD.PushError($"StillWardenBoss: '{GuardScenePath}' has no Enemy script attached.");
				return;
			}

			guard.Health = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * GuardHealthFraction));
			guards.Add(guard);
			arena.AddChild(guard);

			float angle = Mathf.Tau * (guards.Count - 1) / Mathf.Max(1, GuardCount);
			guard.GlobalPosition = GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * GuardPostRadius;
		}
	}

	protected override void DrawGroundTells(CanvasItem floor)
	{
		// The floor it has claimed. Drawn under the slam ring (so first) and much fainter, so the
		// player can see the shape of the ground they are fighting for before anything is winding up.
		if (!IsDying)
		{
			var claim = new Color(RimeColor.R, RimeColor.G, RimeColor.B, 0.13f);
			floor.DrawArc(Vector2.Zero, GuardPostRadius, 0f, Mathf.Tau, 40, claim, 2f, true);
		}
		base.DrawGroundTells(floor);
	}

	// --- The arrival (BossDeath, entrance) ---------------------------------------------------------
	//
	// THAWED OUT OF A BLOCK OF ICE - its fall run the other way. Frost spreads over the ground; a pillar
	// of ice grows up out of it with something dark inside; cracks run through the pillar, and it
	// bursts, shards everywhere, leaving the Warden standing in the rime it brought, cold-white until
	// the frost on it settles.
	protected internal override float EntranceSeconds => 3.3f;
	// The climax (BossDeath calls it on the roar): the pillar bursts a beat before the roar, and this is that burst.
	protected internal override float EntranceClimaxWeight => 1.2f;
	protected internal override Element EntranceFlareElement => Element.Ice;

	private BossDeath.Piece frozen;
	private const float PillarUp = 1.4f;
	private const float Bursts = 2.3f;
	private Vector2[][] pillarCracks;

	protected internal override void OnEntranceBegin(BossDeath stage)
	{
		frozen = stage.CutWhole();
		frozen.Color = new Color(0.4f, 0.5f, 0.6f, 0f);
		pillarCracks = new Vector2[5][];
		for (int k = 0; k < pillarCracks.Length; k++)
			pillarCracks[k] = stage.CrackPath(211u + (uint)k, new Vector2(48f, 50f), Mathf.Tau * k / pillarCracks.Length, 10, 6f);
	}

	protected internal override void TickEntrance(BossDeath stage, float t, float dt)
	{
		if (frozen == null)
			return;
		// Inside the ice: a dark shape, cold. Out of it: white with frost, warming to its own colour.
		float seen = BossDeath.Span(t, 0.4f, PillarUp);
		bool free = t >= Bursts;
		frozen.Color = free
			? new Color(1.5f, 1.6f, 1.7f).Lerp(Colors.White, BossDeath.Span(t, Bursts, EntranceSeconds))
			: new Color(0.4f, 0.5f, 0.6f, 0.85f * seen);
		frozen.Glow = new Color(RimeColor, free ? 0.6f * (1f - BossDeath.Span(t, Bursts, Bursts + 0.6f)) : 0f);
		frozen.Offset = new Vector2(t < Bursts ? BossDeath.Shudder(t, BossDeath.Span(t, Bursts - 0.5f, Bursts), 22f) * stage.ArtScale.X : 0f, 0f);

		if (stage.Crossed(Bursts))
		{
			// The pillar goes: long shards and chunks thrown wide, glinting where they land.
			stage.Emit(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.ShardLong, Variants = 3, SpeedMin = 90f, SpeedMax = 230f, UpMin = 80f, UpMax = 220f,
				Spin = 7f, Bounce = 0.35f, Life = 1.7f, Fade = 0.45f, Glint = new Color(0.9f, 0.97f, 1f, 0.9f), DepthJitter = 20f,
			}, 22, new Vector2(48f, 48f), new Vector2(26f, 36f));
			stage.Emit(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.IceChunk, SpeedMin = 50f, SpeedMax = 140f, UpMin = 60f, UpMax = 160f,
				Spin = 5f, Bounce = 0.3f, Life = 1.6f, Fade = 0.4f, Glint = new Color(0.9f, 0.97f, 1f, 0.9f),
			}, 8, new Vector2(48f, 70f), new Vector2(20f, 16f));
			stage.DustRing(stage.Foot, 60f, 9, new Color(0.85f, 0.94f, 1f, 0.55f), 0.8f, 16f);
		}
	}

	private static readonly Color Rime = new(0.78f, 0.90f, 0.98f, 0.8f);

	protected internal override void DrawEntranceGround(BossDeath stage, CanvasItem canvas, float t)
	{
		float spread = BossDeath.EaseOut(BossDeath.Span(t, 0f, 1.0f));
		float fade = 1f - BossDeath.Span(t, EntranceSeconds, EntranceSeconds + 1.1f);
		if (spread <= 0f || fade <= 0f)
			return;
		Vector2 foot = stage.Foot;
		BossDeath.Disc(canvas, foot, 80f * spread, 32f * spread, new Color(Rime, 0.45f * fade));
		BossDeath.Disc(canvas, foot + new Vector2(26f, 6f), 50f * spread, 18f * spread, new Color(Rime, 0.35f * fade));
		BossDeath.Disc(canvas, foot + new Vector2(-34f, -4f), 44f * spread, 16f * spread, new Color(Rime, 0.35f * fade));
	}

	// The pillar: a column of pale ice over the body, grown from the ground, cracking, gone at the burst.
	protected internal override void DrawEntranceAir(BossDeath stage, CanvasItem canvas, float t)
	{
		if (t >= Bursts)
			return;
		float grown = BossDeath.EaseOut(BossDeath.Span(t, 0.2f, PillarUp));
		if (grown <= 0f)
			return;
		Vector2 foot = stage.Foot;
		float height = 190f * grown;
		float half = 58f;
		// Thin enough that the dark shape held in it shows through.
		var ice = new Color(0.72f, 0.88f, 0.98f, 0.38f);
		canvas.DrawRect(new Rect2(BossDeath.Snap(new Vector2(foot.X - half, foot.Y - height)), new Vector2(half * 2f, height)), ice);
		canvas.DrawRect(new Rect2(BossDeath.Snap(new Vector2(foot.X - half + 8f, foot.Y - height + 6f)), new Vector2(BossDeath.Px * 3f, height - 12f)),
			new Color(1f, 1f, 1f, 0.5f));
		BossDeath.Disc(canvas, new Vector2(foot.X, foot.Y - height), half, 14f, new Color(0.86f, 0.95f, 1f, 0.75f));
		float crack = BossDeath.Span(t, Bursts - 0.7f, Bursts);
		if (crack > 0f && pillarCracks != null)
		{
			foreach (Vector2[] path in pillarCracks)
				stage.DrawCrack(canvas, null, path, crack, new Color(1f, 1f, 1f, 0.95f), new Color(RimeColor, 0.6f));
		}
	}

	// --- The fall (BossDeath) ---------------------------------------------------------------------
	//
	// THE COLD LETS GO. It is a thing frozen into the ground that made the ground its own, and it ends
	// the way ice ends under a blow: it cracks. White seams race out from the eye across the plate and
	// the crystals, the whole figure goes white along them, and then it shatters - ice everywhere,
	// plate with it - and the rime it had laid round itself, the ground it claimed, draws back in
	// towards where it stood and is gone. The shards lie winking on the frost a moment and melt.
	private BossDeath.Piece deathIce;
	private Vector2[][] deathCracks;

	private const float ShatterAt = 0.58f;
	private const int RimeSpecks = 70;

	protected internal override float DeathSeconds => 2.6f;

	protected internal override void OnDeathBegin(BossDeath death)
	{
		deathIce = death.CutWhole();
		// Seven seams out of the eye, spread round it - more of them downwards, where the body is.
		var from = new Vector2(47.5f, 18f);
		float[] headings = { -2.6f, -0.5f, 0.5f, 1.25f, 1.9f, 2.6f, Mathf.Pi * 0.5f };
		deathCracks = new Vector2[headings.Length][];
		for (int i = 0; i < headings.Length; i++)
			deathCracks[i] = death.CrackPath((uint)(i * 17 + 3), from, headings[i], i < 2 ? 10 : 24, 3f);
	}

	protected internal override void TickDeath(BossDeath death, float t, float dt)
	{
		if (deathIce == null)
			return;

		float s = death.ArtScale.X;
		ElementColors.Ramp ice = ElementColors.GetRamp(Element.Ice);
		float strain = BossDeath.Span(t, 0f, ShatterAt);
		deathIce.Offset = new Vector2(BossDeath.Shudder(t, strain, 24f) * s, 0f);
		deathIce.Glow = new Color(ice.Hot, 0.65f * BossDeath.EaseIn(BossDeath.Span(t, 0.3f, ShatterAt)));
		deathIce.Visible = t < ShatterAt;

		if (!death.Crossed(ShatterAt))
			return;
		death.Impact(0.9f);

		var centre = new Vector2(47.5f, 52f);
		death.Emit(new BossDeath.Throw
		{
			Cell = BossDeath.Cell.ShardLong, Variants = 3, SpeedMin = 80f, SpeedMax = 270f, UpMin = 110f, UpMax = 290f,
			Gravity = 660f, Bounce = 0.25f, Friction = 6f, Spin = 7f, Life = 1.85f, Fade = 0.7f, DepthJitter = 18f,
			Glint = new Color(ice.Core, 0.9f),
		}, 26, centre, new Vector2(30f, 36f));
		death.Emit(new BossDeath.Throw
		{
			Cell = BossDeath.Cell.IceChunk, SpeedMin = 40f, SpeedMax = 150f, UpMin = 80f, UpMax = 180f,
			Bounce = 0.2f, Friction = 7f, Spin = 4f, Life = 1.85f, Fade = 0.7f,
		}, 6, centre, new Vector2(24f, 30f));
		death.Emit(new BossDeath.Throw
		{
			Cell = BossDeath.Cell.PlateShard, Tinted = true, SpeedMin = 50f, SpeedMax = 180f, UpMin = 70f, UpMax = 170f,
			Bounce = 0.3f, Friction = 6f, Spin = 6f, Life = DeathSeconds - ShatterAt, Fade = 0.45f,
		}, 10, new Vector2(47.5f, 30f), new Vector2(30f, 12f));
		death.Emit(new BossDeath.Throw
		{
			Size = 2f, Color = ice.Core, ColorB = ice.Mid, Light = true, NoLand = true, SpeedMin = 10f, SpeedMax = 60f,
			UpMin = 10f, UpMax = 80f, Gravity = -40f, Drag = 1.5f, Life = 1.1f, Fade = 0.7f, Flutter = 4f,
		}, 22, centre, new Vector2(30f, 40f));
		for (int i = 0; i < 6; i++)
		{
			Vector2 at = death.ArtToLocal(centre + new Vector2(death.Rand(-26f, 26f), death.Rand(-30f, 34f)));
			death.PuffAt(at, 6f, 24f, 0.4f, new Color(ice.Hot, 0.3f), 8f, death.Rand(0f, 0.08f), true);
		}
	}

	// The seams, racing out of the eye and blazing white just before it goes.
	protected internal override void DrawDeathLight(BossDeath death, CanvasItem canvas, float t)
	{
		if (deathIce == null || t >= ShatterAt)
			return;
		ElementColors.Ramp ice = ElementColors.GetRamp(Element.Ice);
		for (int i = 0; i < deathCracks.Length; i++)
		{
			float grown = BossDeath.EaseOut(BossDeath.Span(t, i * 0.035f, 0.42f + i * 0.02f));
			death.DrawCrack(canvas, deathIce, deathCracks[i], grown, new Color(ice.Core, 0.95f), new Color(ice.Mid, 0.45f));
		}
	}

	// The rime it laid round itself, out to the guards' posts: brightening as the cold gathers, then
	// drawing back in to where it stood after the shatter, and gone.
	protected internal override void DrawDeathGround(BossDeath death, CanvasItem canvas, float t)
	{
		if (deathIce == null)
			return;
		ElementColors.Ramp ice = ElementColors.GetRamp(Element.Ice);
		float reach = GuardPostRadius * (1f - BossDeath.EaseIn(BossDeath.Span(t, ShatterAt + 0.1f, 2.3f)));
		if (reach <= 4f)
			return;
		float bright = 0.35f + 0.4f * BossDeath.Span(t, 0f, ShatterAt) * (1f - BossDeath.Span(t, ShatterAt, ShatterAt + 0.5f));
		Vector2 centre = death.Foot;
		for (int k = 0; k < RimeSpecks; k++)
		{
			float a = Mathf.Tau * BossDeath.Hash(71u, k);
			float d = GuardPostRadius * Mathf.Sqrt(BossDeath.Hash(73u, k));
			if (d > reach)
				continue;
			Vector2 at = centre + new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d * 0.5f);
			float size = BossDeath.Hash(79u, k) < 0.3f ? BossDeath.Px * 2f : BossDeath.Px;
			Color c = BossDeath.Hash(83u, k) < 0.25f ? ice.Hot : ice.Mid;
			canvas.DrawRect(WhipLimb.Cell(at, size), new Color(c, bright * (d > reach - 12f ? 0.5f : 1f)));
		}
		// Specks, never a ring: a ring round a boss is a telegraph in this game, and this is not one.
	}
}
