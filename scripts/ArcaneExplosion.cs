using Godot;
using System;
using System.Linq;

namespace TheOneHeMissed.scripts
{
	public partial class ArcaneExplosion : Area2D
	{
		[Export] public SpellData SpellData { get; set; }
		[Export] public int CurrentLevel { get; set; } = 1;
		// Charge-driven, not timed. Player counts casts of the OTHER five spells and detonates
		// this when the count comes due - see Player.NotifyArcaneCharge. Left as an export so the
		// old behaviour is one checkbox away if the charge version does not survive playtesting.
		[Export] public bool ChargeDriven { get; set; } = true;

		[Export] public float BaseKnockbackRange { get; set; } = 100f;
		[Export] public float BaseKnockbackSpeed { get; set; } = 2.0f;
		public float DamageMultiplier { get; set; } = 1.0f;
		public float AreaMultiplier { get; set; } = 1.0f;
		public float CooldownMultiplier { get; set; } = 1.0f;
		public float DurationMultiplier { get; set; } = 1.0f;

		private Weapon weapon;
		public Weapon Weapon
		{
			get => weapon;
			set
			{
				weapon = value;
				ApplyLegacyWeapon(value);
				UpdateScale();
			}
		}

		private int damage = 5;
		private float range = 100f;
		private float cooldown = 1.5f;
		private float knockbackRange = 100f;
		private float knockbackSpeed = 2.0f;

		public Node2D PlayerRef;
		[Export]
		public float TotalLifetime = 5.0f; // total time before freeing the node; 0 = infinite
		private float lifeElapsed = 0f;
		private float fireTimer = 0f;
		private CollisionShape2D collisionShape;

		// THE BLAST IS PIXEL ART (2026-10-29, "does not look up to our typical visual fidelity"). It was
		// spell_fx.py's 32 px ring saved at x2 and shown at range / 32 - its art pixels 6 to 14 world px
		// wide - over a ring of 64 white GPU particles one world px wide, with every evolution's tint
		// multiplied over both. Now it is one quad through scenes/shaders/arcane_blast.gdshader, drawn
		// from `range`, the distance TriggerExplosion tests: a front of the force that lands on that
		// radius and breaks up there, every art pixel on the world's 2 px grid. The evolutions are painted
		// there rather than tinted, from what each one does (StartBlast).
		private const string BlastShaderPath = "res://scenes/shaders/arcane_blast.gdshader";
		private const float BlastSeconds = 0.4f;
		private static Shader blastShader;
		private static readonly StringName ParamRadius = "radius";
		private static readonly StringName ParamProgress = "progress";
		private static readonly StringName ParamSeed = "seed";
		private static readonly StringName ParamDual = "dual";
		private static readonly StringName ParamStatic = "static_nova";
		private static readonly StringName ParamSupernova = "supernova";
		private static readonly StringName ParamEventHorizon = "event_horizon";
		private static readonly StringName ParamGilded = "gilded";
		private ShaderMaterial blastMaterial;
		private float blastAge = -1f;
		private float blastRadius;
		private int blastCount;

		public override void _Ready()
		{
			// The blast is in the air, over the bodies it catches. A child of the player at z 0, it was
			// sorted at his feet, under every body south of him (DrawLayers). Its rift is on the ground layer.
			ZAsRelative = false;
			ZIndex = DrawLayers.Air;
			RefreshComputedStats();
			collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			blastShader ??= GD.Load<Shader>(BlastShaderPath);
			blastMaterial = new ShaderMaterial { Shader = blastShader };
			Material = blastMaterial;
			Listen(resonance);

			// Apply initial scaling if weapon is already set
			UpdateScale();

			// Trigger immediately on spawn, then start cooldown timer
			TriggerExplosion();
			fireTimer = 0f;
		}

		private void UpdateScale()
		{
			// The collision circle follows the range as it always did; the blast is drawn from the range
			// itself (_Draw), so there is no sprite to scale.
			float baseRange = 32.0f;
			float scaleFactor = range / baseRange;

			if (collisionShape != null && collisionShape.Shape is CircleShape2D circle)
			{
				// The original circle radius is ~23, scale it proportionally
				circle.Radius = 23.0f * scaleFactor;
			}
		}

		public override void _Process(double delta)
		{
			// follow player if assigned
			if (PlayerRef != null)
			{
				GlobalPosition = PlayerRef.GlobalPosition;
			}

			// update overall lifetime and free when exceeded
			if (TotalLifetime > 0f)
			{
				lifeElapsed += (float)delta;
				if (lifeElapsed >= TotalLifetime)
				{
					QueueFree();
					return;
				}
			}
			// Timed firing only when the charge path is switched off. A spell that both ticks on
			// a clock AND detonates on charge would fire twice as often as either rule describes.
			if (!ChargeDriven)
			{
				fireTimer += (float)delta;
				if (fireTimer >= cooldown)
				{
					TriggerExplosion();
					fireTimer = 0f;
				}
			}

			clock += (float)delta;
			TickEchoes((float)delta);
			TickStasis((float)delta);

			if (blastAge < 0f)
				return;
			blastAge += (float)delta;
			if (blastAge >= BlastSeconds)
				blastAge = -1f;
			else
				blastMaterial?.SetShaderParameter(ParamProgress, blastAge / BlastSeconds);
			QueueRedraw();
		}

		// One quad over the blast, sized in world px - what TriggerExplosion measured in - whatever this
		// node's scale; the shader decides every pixel and discards the rest.
		public override void _Draw()
		{
			if (blastAge < 0f)
				return;
			float half = (blastRadius + 4f) / MathF.Max(0.01f, GlobalScale.X);
			DrawRect(new Rect2(-half, -half, half * 2f, half * 2f), Colors.White);
		}

		// The blast's look, from what this detonation does: the storm's crackle for Static Nova, a collapse
		// for anything that pulls inward, the element of the spell that set it off for Sympathetic Blast,
		// gold for a Legendary. Set once a blast, with the radius it hit. (The shader's second front was
		// Dual Shockwave's look; the card is Sympathetic Blast since 2h, so it is no longer asked for.)
		private void StartBlast(bool isVortex, Element? mimic)
		{
			if (blastMaterial == null)
				return;
			blastRadius = range;
			blastAge = 0f;
			blastCount++;
			Paint(blastMaterial, blastRadius, blastCount, isVortex, mimic);
			QueueRedraw();
		}

		private void Paint(ShaderMaterial material, float radius, int count, bool isVortex, Element? mimic)
		{
			material.SetShaderParameter(ParamRadius, radius);
			material.SetShaderParameter(ParamProgress, 0f);
			material.SetShaderParameter(ParamSeed, (count * 0.6180339f) % 1f * 97f);
			material.SetShaderParameter(ParamDual, false);
			material.SetShaderParameter(ParamStatic, SpellData?.HasEvolution(StaticNovaId) ?? false);
			material.SetShaderParameter(ParamSupernova, false);
			material.SetShaderParameter(ParamEventHorizon, isVortex);
			material.SetShaderParameter(ParamGilded, SpellData?.IsLegendary ?? false);
			material.SetShaderParameter(ParamMimic, mimic.HasValue);
			if (mimic.HasValue)
			{
				ElementColors.Ramp ramp = ElementColors.GetRamp(mimic.Value);
				material.SetShaderParameter(ParamMimicCore, ramp.Core);
				material.SetShaderParameter(ParamMimicHot, ramp.Hot);
				material.SetShaderParameter(ParamMimicMid, ramp.Mid);
				material.SetShaderParameter(ParamMimicEdge, ramp.Edge);
			}
		}

		private static readonly StringName ParamMimic = "mimic";
		private static readonly StringName ParamMimicCore = "m_core";
		private static readonly StringName ParamMimicHot = "m_hot";
		private static readonly StringName ParamMimicMid = "m_mid";
		private static readonly StringName ParamMimicEdge = "m_edge";

		public const string SympatheticBlastId = "arcane_explosion_dual_shockwave";
		private const string StaticNovaId = "arcane_explosion_static_nova";
		public const string ResonanceId = "arcane_explosion_supernova";
		public const string EventHorizonId = "arcane_explosion_event_horizon";

		// THE HUNDRED MOUTHS (the Cumaean Sibyl's Apotheosis, the verb ECHO; .ai/chapter-wizards.md 4.3). The cave
		// at Cumae had a hundred mouths and spoke with a hundred voices: each burst sounds her other spells again,
		// from where she stands, at Player.EchoShare of their strength (Player.Echo.cs). The burst only asks; the
		// player queues the echoes and casts a few a frame, and an echo is never a cast - it charges nothing and
		// is never echoed again - so the form cannot feed itself.
		public const string HundredMouthsId = "arcane_explosion_hundred_mouths";
		private bool hundredMouths;

		/// <summary>Detonate now. Called by Player when the charge comes due, with the cast that filled it.</summary>
		public void TriggerNow(SpellData charger = null)
		{
			lastCharger = charger;
			TriggerExplosion();
			fireTimer = 0f;
		}

		private SpellData lastCharger;

		private void TriggerExplosion()
		{
			// Apply to all enemies within range.
			//
			// The group, not GetTree().CurrentScene.GetChildren(). The old form only saw enemies
			// that were DIRECT children of the scene root, so anything parented under a container
			// - a spawner, a formation node, a test harness - was silently immune. CLAUDE.md is
			// explicit that AoE iterates the group for exactly this reason.
			//
			// Event Horizon (Effect Freeze) froze what it caught and, until 2h, threw it INWARD - a pull,
			// which is Hollow Star's verb, and which turned Dual Shockwave's +70% throw into a 70% harder
			// pull under it. Now the blast stops time: what it catches is frozen where it stands, nothing
			// is thrown either way, and a sphere of stopped time stays where it went off (OpenStasis).
			float freezeSeconds = SpellData?.GetFreezeSecondsAtLevel(CurrentLevel) ?? 0f;
			bool stopsTime = freezeSeconds > 0f;
			bool isVortex = SpellData != null && SpellData.HasEffectFlag(SpellEffect.VortexPull);
			Element? mimic = SympathyElement();
			int hit = mimic.HasValue && ElementPaths.RiderFor(mimic.Value) == ElementRider.None
				? Math.Max(1, Mathf.RoundToInt(damage * SympathyBareShare))
				: damage;
			BlastAt(GlobalPosition, hit, stopsTime ? 0f : 1f, isVortex, freezeSeconds, mimic);

			OpenLingeringRift();
			if (stopsTime)
				OpenStasis(GlobalPosition, range, freezeSeconds);
			StartBlast(isVortex, mimic);
			Resonate(damage, stopsTime, isVortex, freezeSeconds, mimic);
			if (hundredMouths && PlayerRef is Player sibyl)
				sibyl.EchoOtherSpells(SpellData);
		}

		// One detonation's hits at `at`: the damage, the throw (scaled by `throwShare`; inward for a pull),
		// the freeze and Sympathetic Blast's rider.
		private void BlastAt(Vector2 at, int amount, float throwShare, bool isVortex, float freezeSeconds, Element? mimic)
		{
			ElementRider rider = mimic.HasValue ? ElementPaths.RiderFor(mimic.Value) : ElementRider.None;
			Player player = PlayerRef as Player;
			foreach (Enemy e in EnemyRegistry.Snapshot)
			{
				if (!IsInstanceValid(e))
					continue;
				float dist = at.DistanceTo(e.GlobalPosition);
				if (dist > range)
					continue;
				if (throwShare > 0f)
				{
					float knockback = knockbackRange * (1f - (dist / (2f * range)));
					if (knockback < knockbackRange * 0.5f)
						knockback = knockbackRange * 0.5f;
					Vector2 dir = isVortex ? (at - e.GlobalPosition).Normalized() : (e.GlobalPosition - at).Normalized();
					e.ApplyKnockback(dir * knockback * knockbackSpeed * throwShare);
				}
				player?.DealDamageToEnemy(e, amount, source: SpellData);
				if (freezeSeconds > 0f)
					e.ApplySlow(0f, freezeSeconds);
				if (rider != ElementRider.None)
					ApplySympathy(e, rider);
			}
		}

		// SYMPATHETIC BLAST (arcane_explosion_dual_shockwave's id, .ai/spell-roster.md 2h, 2026-10-31). The
		// card was Dual Shockwave: +35% damage and a 70% longer throw. Arcane Explosion is the one spell that
		// is set off by the others, so this card lets the one that set it off show: the blast goes off in
		// that spell's element - repainted in its ramp - and lays that element's status on what it catches
		// (ElementPaths.RiderFor: ice and water slow, lightning staggers, poison poisons, darkness exposes,
		// light weakens, metal withers). The five elements with no status (fire, earth, grass, wind, arcane)
		// make it hit SympathyBareShare harder instead. The element is the spell's strongest that is not
		// Arcane - Spiritual Weapon's Light, Magic Missile's Lightning once Twin Volley or Overcharge has branched it - and
		// Arcane only for a pure Arcane one (Magic Missile unbranched, since it went pure on 2026-11-02).
		public const float SympathyBareShare = 1.3f;

		private Element? SympathyElement()
		{
			if (SpellData == null || !SpellData.HasEvolution(SympatheticBlastId) || lastCharger == null)
				return null;
			Element best = Element.Arcane;
			int bestWeight = 0;
			foreach (var pair in lastCharger.GetElementWeights())
			{
				if (pair.Key == Element.Arcane || pair.Value <= bestWeight)
					continue;
				best = pair.Key;
				bestWeight = pair.Value;
			}
			return best;
		}

		private void ApplySympathy(Enemy e, ElementRider rider)
		{
			switch (rider)
			{
				case ElementRider.Poison:
					ElementPaths.Apply(e, rider, MathF.Max(2f, damage * 0.15f), 3f);
					break;
				case ElementRider.Slow:
					ElementPaths.Apply(e, rider, 0.6f, 1.5f);
					break;
				case ElementRider.Vulnerable:
					ElementPaths.Apply(e, rider, 0.15f, 3f);
					break;
				case ElementRider.Weaken:
					ElementPaths.Apply(e, rider, 0.3f, 3f);
					break;
				case ElementRider.Wither:
					ElementPaths.Apply(e, rider, 0.04f, 0f);
					break;
				case ElementRider.Shock:
					ElementPaths.Apply(e, rider, 0f, 0.3f);
					break;
			}
		}

		// RESONANCE (arcane_explosion_supernova's id, 2h). The card was Supernova: +150% area and +120%
		// damage, the same blast bigger. Now every blast goes off again wherever each of the wizard's other
		// spells last struck (in the last ResonanceMemory seconds, and not on top of the blast round him):
		// the same blast, at ResonanceShare of its damage, drawn there by a copy of its own front. A loadout
		// of six spells lights up the screen where its spells are working; one partner gives one echo.
		public const float ResonanceShare = 1f;
		private const float ResonanceMemory = 2.5f;
		private const int MaxEchoes = 5;

		private struct Strike
		{
			public SpellData Source;
			public Vector2 At;
			public float Time;
		}

		private readonly Strike[] strikes = new Strike[8];
		private float clock;
		private bool resonance;
		private bool listening;
		private readonly Node2D[] echoNodes = new Node2D[MaxEchoes];
		private readonly ShaderMaterial[] echoMaterials = new ShaderMaterial[MaxEchoes];
		private readonly float[] echoAges = new float[MaxEchoes];
		private readonly float[] echoRadii = new float[MaxEchoes];
		private int nextEcho;

		private void Listen(bool on)
		{
			if (on == listening)
				return;
			listening = on;
			if (on)
				Player.EnemyDamagedAt += OnEnemyDamagedAt;
			else
				Player.EnemyDamagedAt -= OnEnemyDamagedAt;
		}

		public override void _ExitTree()
		{
			Listen(false);
		}

		// Every hit the wizard lands: where each of his other spells last struck. A fixed list, one slot a spell.
		private void OnEnemyDamagedAt(Vector2 at, int amount, SpellData source)
		{
			if (source == null || source == SpellData)
				return;
			int free = -1;
			for (int i = 0; i < strikes.Length; i++)
			{
				if (strikes[i].Source == source)
				{
					strikes[i].At = at;
					strikes[i].Time = clock;
					return;
				}
				if (free < 0 && (strikes[i].Source == null || clock - strikes[i].Time > ResonanceMemory))
					free = i;
			}
			if (free >= 0)
				strikes[free] = new Strike { Source = source, At = at, Time = clock };
		}

		private void Resonate(int amount, bool stopsTime, bool isVortex, float freezeSeconds, Element? mimic)
		{
			if (!resonance)
				return;
			int echo = Math.Max(1, Mathf.RoundToInt(amount * ResonanceShare));
			float clear = range * 0.75f;
			int made = 0;
			for (int i = 0; i < strikes.Length && made < MaxEchoes; i++)
			{
				Strike s = strikes[i];
				if (s.Source == null || clock - s.Time > ResonanceMemory || s.At.DistanceTo(GlobalPosition) < clear)
					continue;
				BlastAt(s.At, echo, stopsTime ? 0f : 1f, isVortex, freezeSeconds, mimic);
				if (stopsTime)
					OpenStasis(s.At, range, freezeSeconds);
				ShowEcho(s.At, isVortex, mimic);
				made++;
			}
		}

		// An echo's front: the blast's own shader on a node of its own at the echo, in the air.
		private void ShowEcho(Vector2 at, bool isVortex, Element? mimic)
		{
			int i = nextEcho;
			nextEcho = (nextEcho + 1) % MaxEchoes;
			if (echoNodes[i] == null || !IsInstanceValid(echoNodes[i]))
			{
				echoMaterials[i] = new ShaderMaterial { Shader = blastShader };
				var node = new Node2D { Name = $"Echo{i}", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Air, Material = echoMaterials[i] };
				int index = i;
				node.Draw += () => DrawEcho(index);
				AddChild(node);
				echoNodes[i] = node;
			}
			echoNodes[i].GlobalPosition = at;
			echoAges[i] = 0f;
			echoRadii[i] = range;
			blastCount++;
			Paint(echoMaterials[i], range, blastCount, isVortex, mimic);
			echoNodes[i].QueueRedraw();
		}

		private void DrawEcho(int i)
		{
			if (echoAges[i] < 0f || echoAges[i] >= BlastSeconds)
				return;
			float half = echoRadii[i] + 4f;
			echoNodes[i].DrawRect(new Rect2(-half, -half, half * 2f, half * 2f), Colors.White);
		}

		private void TickEchoes(float delta)
		{
			for (int i = 0; i < MaxEchoes; i++)
			{
				if (echoNodes[i] == null || echoAges[i] < 0f)
					continue;
				echoAges[i] += delta;
				if (echoAges[i] >= BlastSeconds)
					echoAges[i] = -1f;
				else
					echoMaterials[i].SetShaderParameter(ParamProgress, echoAges[i] / BlastSeconds);
				echoNodes[i].QueueRedraw();
			}
		}

		// EVENT HORIZON STOPS TIME (2h). Where the blast goes off, a sphere of stopped time hangs for as long
		// as the freeze: what the blast caught stays frozen in it, and whatever walks in freezes at once, for
		// what is left of it (a boss only slows, Enemy.MinSlowMultiplier). Drawn on the ground under the
		// bodies, as the thing it is - motes of the force hung still in the air over a faint dark floor and a
		// rim of them where time stops - never a ring at the reach with nothing in it (2c). A few at a time
		// in fixed slots, one canvas for all of them.
		private const int MaxStasis = 6;
		private const float StasisTick = 0.1f;

		private struct Stasis
		{
			public Vector2 At;
			public float Radius;
			public float Age;
			public float Life;
			public float Seed;
			public bool Live;
		}

		private readonly Stasis[] stasis = new Stasis[MaxStasis];
		private int nextStasis;
		private float stasisTick;
		private Node2D stasisCanvas;
		private bool stasisDrawn;

		private void OpenStasis(Vector2 at, float radius, float seconds)
		{
			if (stasisCanvas == null || !IsInstanceValid(stasisCanvas))
			{
				stasisCanvas = new Node2D { Name = "Stasis", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Ground };
				stasisCanvas.Draw += DrawStasis;
				AddChild(stasisCanvas);
				stasisCanvas.GlobalPosition = Vector2.Zero;
			}
			stasis[nextStasis] = new Stasis { At = at, Radius = radius, Life = seconds * DurationMultiplier, Seed = (blastCount * 0.381966f) % 1f * 53f, Live = true };
			nextStasis = (nextStasis + 1) % MaxStasis;
		}

		private void TickStasis(float delta)
		{
			if (stasisCanvas == null || !IsInstanceValid(stasisCanvas))
				return;
			bool any = false;
			stasisTick -= delta;
			bool hold = stasisTick <= 0f;
			if (hold)
				stasisTick = StasisTick;
			for (int i = 0; i < MaxStasis; i++)
			{
				if (!stasis[i].Live)
					continue;
				any = true;
				stasis[i].Age += delta;
				float left = stasis[i].Life - stasis[i].Age;
				if (left <= 0f)
				{
					stasis[i].Live = false;
					continue;
				}
				if (!hold)
					continue;
				float r2 = stasis[i].Radius * stasis[i].Radius;
				foreach (Enemy e in EnemyRegistry.Snapshot)
				{
					if (IsInstanceValid(e) && e.GlobalPosition.DistanceSquaredTo(stasis[i].At) <= r2)
						e.ApplySlow(0f, left);
				}
			}
			if (any || stasisDrawn)
				stasisCanvas.QueueRedraw();
			stasisDrawn = any;
		}

		private static readonly Color StasisFloor = new Color(0.078f, 0.063f, 0.122f);   // violet deep
		private static readonly Color StasisMote = new Color(0.910f, 0.753f, 1.000f);    // arcane hot
		private static readonly Color StasisDim = new Color(0.478f, 0.247f, 0.831f);     // arcane edge
		private static readonly Color StasisCore = new Color(1.000f, 0.941f, 1.000f);    // arcane core

		private void DrawStasis()
		{
			for (int i = 0; i < MaxStasis; i++)
			{
				Stasis s = stasis[i];
				if (!s.Live)
					continue;
				float fade = Mathf.Clamp(s.Age / 0.15f, 0f, 1f) * Mathf.Clamp((s.Life - s.Age) / 0.35f, 0f, 1f);
				stasisCanvas.DrawSetTransform(s.At, 0f, new Vector2(1f, CraterGround.Squash));
				stasisCanvas.DrawCircle(Vector2.Zero, s.Radius, new Color(StasisFloor, 0.28f * fade));
				stasisCanvas.DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
				// The rim: motes where time stops, every few degrees, still.
				int rimMotes = Math.Clamp((int)(s.Radius / 7f), 16, 72);
				for (int k = 0; k < rimMotes; k++)
				{
					float a = Mathf.Tau * k / rimMotes + s.Seed;
					Vector2 p = s.At + new Vector2(MathF.Cos(a) * s.Radius, MathF.Sin(a) * s.Radius * CraterGround.Squash);
					Color c = k % 3 == 0 ? StasisMote : StasisDim;
					stasisCanvas.DrawRect(WhipLimb.Cell(p, WhipLimb.Grid), new Color(c, fade));
				}
				// Motes hung in the sphere, where the blast left them: the same place every frame.
				int motes = Math.Clamp((int)(s.Radius * s.Radius / 900f), 10, 60);
				for (int k = 0; k < motes; k++)
				{
					float h1 = ((k * 0.6180339f + s.Seed) % 1f);
					float h2 = ((k * 0.7548777f + s.Seed * 0.31f) % 1f);
					float rr = MathF.Sqrt(h2) * s.Radius * 0.95f;
					float a = h1 * Mathf.Tau;
					Vector2 p = s.At + new Vector2(MathF.Cos(a) * rr, MathF.Sin(a) * rr * CraterGround.Squash);
					Color c = k % 5 == 0 ? StasisCore : StasisMote;
					stasisCanvas.DrawRect(WhipLimb.Cell(p, WhipLimb.Grid), new Color(c, 0.85f * fade));
				}
			}
		}

		// Lingering Rift (Effect ZoneDuration = seconds): the ground the blast went off on keeps hurting
		// for a while after. It promised "a pulsating hazard zone" and ArcaneExplosion left none until
		// 2026-10-26. The rift is a LingeringZone, Mirefoot's pool, in arcane colour: it stays where the
		// blast was while the player walks on, which is what makes it a zone.
		private const float RiftTickShare = 0.25f;
		private const float RiftRadiusShare = 0.8f;

		private void OpenLingeringRift()
		{
			float seconds = SpellData?.GetEffectValueAtLevel(SpellEffect.ZoneDuration, CurrentLevel) ?? 0f;
			if (seconds <= 0f || PlayerRef is not Player player)
				return;
			Node parent = player.GetParent();
			if (parent == null)
				return;

			var rift = new LingeringZone
			{
				Name = "LingeringRift",
				Radius = range * RiftRadiusShare,
				Duration = seconds * DurationMultiplier,
				TickSeconds = 0.5f,
				AppliesSlow = false,
				DamagePerTick = Math.Max(1, Mathf.RoundToInt(damage * RiftTickShare)),
				SpellData = SpellData,
				PlayerRef = player,
			};
			// A tear in the ground with the force wound out of it to the zone's radius, not a translucent
			// disc (scenes/shaders/arcane_rift.gdshader).
			BlastPoolSurface.Attach(rift, BlastPoolSurface.Kind.ArcaneRift);
			parent.AddChild(Player.TagSpell(rift));
			rift.GlobalPosition = GlobalPosition;
		}

		public void SetSpellLevel(int level)
		{
			CurrentLevel = Math.Max(1, level);
			RefreshComputedStats();
			UpdateScale();
		}

		private void RefreshComputedStats()
		{
			if (weapon != null)
			{
				ApplyLegacyWeapon(weapon);
				return;
			}

			damage = Math.Max(1, Mathf.RoundToInt((SpellData?.GetDamageAtLevel(CurrentLevel) ?? 5) * DamageMultiplier));
			// With the level table's Area line (Effect AreaSize, a fraction), read since 2026-10-26.
			range = (SpellData?.GetRangeAtLevel(CurrentLevel) ?? 100f) * AreaMultiplier * (SpellData?.GetAreaMultiplierAtLevel(CurrentLevel) ?? 1f)
				* (1f + (SpellData?.GetEffectValueAtLevel(SpellEffect.AreaSize, CurrentLevel) ?? 0f));
			cooldown = MathF.Max(0.05f, (SpellData?.GetCooldownAtLevel(CurrentLevel) ?? 1.5f) * CooldownMultiplier);
			// Effect Knockback from the level table and KnockbackBonus from an evolution (Dual
			// Shockwave) are the same units; the second was read by nothing until 2026-10-26.
			knockbackRange = MathF.Max(0f, BaseKnockbackRange + (SpellData?.GetEffectValueAtLevel(SpellEffect.Knockback, CurrentLevel) ?? 0f)
				+ (SpellData?.GetKnockbackBonusAtLevel(CurrentLevel) ?? 0f));
			knockbackSpeed = MathF.Max(0f, BaseKnockbackSpeed);
			resonance = SpellData != null && SpellData.HasEvolution(ResonanceId);
			hundredMouths = SpellData != null && SpellData.IsApotheosis
				&& string.Equals(SpellData.SelectedApotheosisId, HundredMouthsId, StringComparison.OrdinalIgnoreCase);
			if (IsInsideTree())
				Listen(resonance);
		}

		private void ApplyLegacyWeapon(Weapon value)
		{
			if (value == null) return;

			damage = value.Damage;
			range = value.Range;
			cooldown = MathF.Max(0.05f, value.Cooldown);
			knockbackRange = value.KnockbackRange;
			knockbackSpeed = value.KnockbackSpeed;
		}
	}
}
