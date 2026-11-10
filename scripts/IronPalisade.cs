using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// Iron Palisade: a line of stakes driven up between the player and the nearest pack (.ai/side-events.md,
// issue #64). Wayland's opener, kept at the Cold Forge in the Cursed Dungeon for him (in-flight 66).
//
// **The wall.** The one delivery shape #64 still listed as missing: something that occupies space
// against the swarm. It is not a physics body - a body on a layer the enemies collide with would
// need a new layer and every enemy mask changed to match (CLAUDE.md, collision layers), and it would
// stop the player too. Instead anything inside the line is shoved back out the far side every tick
// and cut once per CutIntervalSeconds, which holds a front exactly as a fence would while letting
// the player walk through their own wall.
//
// **Pure Metal.** Metal had no pure spell, so it could never reach its capstone.
//
// The shove rides Enemy.ApplyKnockback, like Hollow Star's pull, so boss knockback resistance
// applies without a special case.
//
// **Lightning Rod** (the level 4 element path into Lightning, ElementPaths): the stakes' lit edge is
// repainted in Lightning's ramp and sparks crawl off their tips; every cut staggers (the rider,
// applied by Player.DealDamageToEnemy).
//
// **Its own four cards** (2026-10-29, .ai/spell-roster.md section 2e), each a change to the wall you can
// see. The ids are the template's.
//   - Spiked Rail (level 4): a spiked rail runs along the stakes; what touches the wall is thrown back
//     RailThrow times as hard and bleeds - drops falling from it - for BleedSeconds.
//   - Caltrops (level 4): when the wall comes down its stakes break into caltrops strewn on its far side,
//     which cut and slow whatever walks over them for CaltropSeconds. The proposal's Quick Raise (the
//     wall raised in a ring round you where you stop) is Sundering's verb, the Geomancer's Apotheosis
//     ("hold": a ring round you that shoves the near ones back), so the card leaves something behind instead.
//   - Iron Ring (level 8): the stakes close into a ring round the nearest pack: what is inside is
//     shoved back in, what is outside kept out, and whatever leans on the stakes is cut.
//   - Rolling Wall (level 8): the wall marches forward at a walk once it is up, shoving what it meets
//     ahead of it and cutting it as it goes. It halts at water and walls.
//
// **The Tempered Line** (Wayland the Smith's Apotheosis, the verb TEMPER; .ai/chapter-wizards.md 4.7): every
// blow the stakes take hardens the wall. Each cut it lands on something leaning on it adds TemperPerBlow to
// its cuts, up to TemperMax; the stakes heat from steel to white as it climbs (the metal ramp). Full, the
// wall RINGS: one cut along its whole length and RingReach either side of it, at the full-tempered cut
// times RingShare, and the temper goes back to nothing and starts again. Per wall, not per spell: a wall
// is what the crowd strikes, and a wall you can see heating is a wall you can read. Nothing here moves or
// stops anything, so a boss takes it like any cut.
public partial class IronPalisade : Node2D, IPaintsElementPath
{
	public const string SpikedRailId = "iron_palisade_amplified_impact";
	public const string CaltropsId = "iron_palisade_hyper_acceleration";
	public const string IronRingId = "iron_palisade_titan_cataclysm";
	public const string RollingWallId = "iron_palisade_infinite_horizon";

	// Spiked Rail: how much harder the wall throws, and the bleed - a share of a cut, every BleedTick
	// seconds for BleedSeconds after the last cut.
	public const float RailThrow = 1.8f;
	public const float BleedShare = 0.25f;
	public const float BleedSeconds = 2f;
	private const float BleedTick = 0.5f;
	private const int MaxBleeding = 32;

	// Caltrops: how long they lie, how deep a strip of ground past the wall they cover, what each tick
	// over them cuts for (a share of a cut) and how hard they slow.
	public const float CaltropSeconds = 3.5f;
	public const float CaltropDepth = 56f;
	public const float CaltropShare = 0.75f;
	public const float CaltropSlow = 0.5f;
	private const int MaxCaltrops = 48;

	// Iron Ring: its radius as a share of the wall's half-length - the line's stakes and more, bent round.
	public const float RingRadiusShare = 1.4f;

	// Rolling Wall: how fast it marches.
	public const float MarchSpeed = 75f;

	// The Tempered Line: +5% a blow to +100%, then the ring - the full-tempered cut, along the whole wall and
	// this far either side of it (a band about as wide as a Trip-Hammer's ring), and a fresh temper.
	public const string TemperedLineId = "iron_palisade_tempered_line";
	public const float TemperPerBlow = 0.05f;
	public const float TemperMax = 1.0f;
	public const float RingReach = 72f;
	public const float RingShare = 1.0f;
	private const float RingFlashSeconds = 0.32f;

	[Export] public float HalfLength { get; set; } = 80f;
	[Export] public float HalfThickness { get; set; } = 14f;
	[Export] public float LifeSeconds { get; set; } = 4f;
	[Export] public float ShoveSpeed { get; set; } = 240f;
	[Export] public float CutIntervalSeconds { get; set; } = 0.5f;
	[Export] public float RiseSeconds { get; set; } = 0.15f;

	public int Damage { get; set; } = 8;
	public SpellData SpellData { get; set; }
	public Player PlayerRef { get; set; }

	/// <summary>Unit vector pointing away from the player, across the wall. Set before adding.</summary>
	public Vector2 Facing { get; set; } = Vector2.Right;

	/// <summary>Iron Ring: the stakes stand on a circle of this radius round the node, not along a line.</summary>
	public float RingRadius { get; set; }
	private bool IsRing => RingRadius > 0f;

	private float age;
	private float tickTimer;
	// Instance id -> the age at which it may be cut again. Never pruned: a wall lives four seconds
	// and meets a few dozen enemies, and pruning would cost an allocation per frame.
	private readonly Dictionary<ulong, float> nextCutAt = new();

	// The cards, read once from the spell in _Ready.
	private bool spikedRail;
	private bool caltrops;
	private bool rolling;
	private bool tempered;

	// The Tempered Line: how hard this wall is now (0 to TemperMax), and when it last rang.
	private float temper;
	private float rangAt = -10f;

	/// <summary>The Tempered Line: this wall's temper, 0 to <see cref="TemperMax"/>. For the probes.</summary>
	public float Temper => temper;

	/// <summary>The Tempered Line: how many times any wall has rung since the count was reset. For the probes.</summary>
	public static int RingsRung { get; private set; }

	/// <summary>Forgets the ring count, for a probe that measures one run.</summary>
	public static void ResetRingCount() => RingsRung = 0;

	// What a cut deals now: the card's damage, hardened by every blow the wall has taken.
	private int CutDamage => tempered ? Mathf.RoundToInt(Damage * (1f + temper)) : Damage;

	// Spiked Rail's bleeding: who, how long left, and when each next bleeds. Swap-removed, never resized.
	private readonly Enemy[] bleeder = new Enemy[MaxBleeding];
	private readonly float[] bleedLeft = new float[MaxBleeding];
	private readonly float[] bleedNext = new float[MaxBleeding];
	private int bleedCount;

	// Caltrops, strewn when the wall falls: where each lies (local), and the age they were strewn at.
	private readonly Vector2[] caltropAt = new Vector2[MaxCaltrops];
	private int caltropCount;
	private float strewnAt = -1f;
	private Node2D ground;

	// Iron Ring: which stakes stand on blocked ground (water, a wall) and are not raised. Made in _Ready.
	private bool[] ringStakeBlocked;
	private int ringStakes;

	private const int StakeCount = 9;
	// The base wall's spacing: nine stakes along 160 px.
	private const float StakeSpacing = 20f;
	// THE LOOK (2026-10-29, the pixel-art pass: "does not look up to our typical visual fidelity"). Every
	// stake was a DrawLine in a grey no palette holds, rotated with the wall, over a translucent black
	// disc; the rail and its spikes thin smooth lines; the caltrops four lines each; the blood a red at 95%.
	// Now everything is drawn in WORLD coordinates (this node turns with the wall, so _Draw undoes its
	// transform first) on the 2 px art grid, in the steel row (.ai/art-direction.md section 3), lit from
	// the upper left as everything is: a stake is three columns of art pixels - lit, base, shade - with a
	// point catching the key, an iron band, and occlusion at its foot on the side away from the light.
	// The rail and its spikes are SpellStrokes on a child (`rail`), a bar shaded by the same key; the
	// caltrops are little iron caltrops of pixels; the bleeding is drops of the ember row. Every fade -
	// the wall going, the caltrops going, a bleed running out - is SpellInk's dither, never a translucency.
	// The stakes stand where they stood; they are a little taller (26 and 20 px against 22 and 17 - a
	// height the hit never read: the wall holds the band either side of its line) so that, drawn at a
	// real art pixel's width, a palisade reads as one rather than as a row of pegs, and the rail runs
	// behind them at two-thirds of that height.
	private static readonly Color SteelHi = new Color(0.863f, 0.910f, 0.957f);     // steel hi #DCE8F4
	private static readonly Color SteelLit = new Color(0.722f, 0.784f, 0.863f);    // steel lit #B8C8DC
	private static readonly Color SteelBase = new Color(0.416f, 0.494f, 0.596f);   // steel base #6A7E98
	private static readonly Color SteelShade = new Color(0.227f, 0.290f, 0.384f);  // steel shade #3A4A62
	private static readonly Color SteelDeep = new Color(0.125f, 0.157f, 0.220f);   // steel deep #202838
	private static readonly Color Occ = new Color(0.020f, 0.027f, 0.047f);         // #05070C
	private static readonly Color BloodFresh = new Color(0.847f, 0.141f, 0.180f);  // ember base #D8242E
	private static readonly Color Blood = new Color(0.541f, 0.071f, 0.125f);       // ember shade #8A1220
	// The lit side of a stake; an element path repaints it in its own ramp and lights sparks.
	private Color ironLit = SteelLit;
	private Color spark = new Color(0f, 0f, 0f, 0f);
	private Color sparkHot = new Color(0f, 0f, 0f, 0f);
	private Node2D rail;
	private SpellStrokes railStrokes;

	// ROLLING WALL DIGS UP THE DIRT (2026-11-01). Eric, playing 0.10.0: the rolling walls "should dig up the
	// dirt as it moves". Two parts, both pictures of where the wall went and neither a hit:
	//   - THE FURROW: scenes/shaders/palisade_furrow.gdshader on a child quad on the ground layer, under
	//     every body - a groove ploughed behind each stake and the earth between turned over, from just
	//     behind the stakes back to where the march began (`marched`), dissolving by dither as it ages so it
	//     is a wake that closes behind the wall, and all of it once the wall is down (FurrowFallSeconds).
	//   - THE CLODS: lumps kicked up off the stakes' feet as the wall walks, thrown back over the furrow in a
	//     short arc with an occlusion shadow under them, landing and crumbling away by dither. Slots in a
	//     fixed array; nothing is allocated a frame.
	// Both in the floor's own tones where the floor is earth or stone (SoilFor), from the contract's rows.
	private const float FurrowFallSeconds = 0.5f;
	public const string FurrowShaderPath = "res://scenes/shaders/palisade_furrow.gdshader";
	private static Shader furrowShader;
	private static readonly StringName ParamMarched = "marched";
	private static readonly StringName ParamFallen = "fallen";
	private Node2D furrow;
	private ShaderMaterial furrowMaterial;
	private float marched;
	private Color soilLit, soilBase, soilShade, soilDeep;

	private const int MaxClods = 28;
	private const float ClodEvery = 0.04f;
	private const float ClodFlight = 0.32f;
	private const float ClodLie = 0.35f;
	private const float ClodHeight = 12f;
	private readonly Vector2[] clodFrom = new Vector2[MaxClods];
	private readonly Vector2[] clodVelocity = new Vector2[MaxClods];
	private readonly float[] clodBorn = new float[MaxClods];
	private int nextClod;
	private uint clodsKicked;
	private float clodTimer;

	/// <summary>
	/// The floor's own tones for earth turned over on it - lit, base, shade and deep - by the kind of ground
	/// the chapter stands on: turf and soil on grass, rubble on flagstones, snow on the Frozen Waste, sand in
	/// the Sands. Every one a row of the contract (tools/art/bonelight.py).
	/// </summary>
	public static void SoilFor(StageEnvironmentKind kind, out Color lit, out Color baseTone, out Color shade, out Color deep)
	{
		switch (kind)
		{
			case StageEnvironmentKind.Castle:
			case StageEnvironmentKind.Ruins:
			case StageEnvironmentKind.Cave:
			case StageEnvironmentKind.Volcanic:
			case StageEnvironmentKind.Bridge:
			case StageEnvironmentKind.Court:
			case StageEnvironmentKind.Throne:
				// Rubble prised out of the flagstones, grit in the grooves: the stone row, the gold row's deep.
				lit = Hex(0x3A4658); baseTone = Hex(0x2A3444); shade = Hex(0x141A26); deep = Hex(0x2E2208);
				break;
			case StageEnvironmentKind.Ice:
				// Snow thrown up off frozen ground: the steel row.
				lit = Hex(0xB8C8DC); baseTone = Hex(0x6A7E98); shade = Hex(0x3A4A62); deep = Hex(0x202838);
				break;
			case StageEnvironmentKind.Desert:
				// Sand: the gold row.
				lit = Hex(0xD8B04A); baseTone = Hex(0x8A6A1E); shade = Hex(0x543F10); deep = Hex(0x2E2208);
				break;
			case StageEnvironmentKind.Swamp:
				// Mud: the linen row the swamp's floor is.
				lit = Hex(0x7A8A7C); baseTone = Hex(0x46544C); shade = Hex(0x2C3834); deep = Hex(0x1A2220);
				break;
			default:
				// Turf torn up off soil (the Forest, the Fields, the Heath): the lichen row on top, the gold
				// row's soil under it (Gravewell's bank, Thorned Vine's torn soil).
				lit = Hex(0x5A8A94); baseTone = Hex(0x543F10); shade = Hex(0x2A4E58); deep = Hex(0x2E2208);
				break;
		}
	}

	private static Color Hex(uint rgb) =>
		new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

	private bool Standing => age < LifeSeconds;

	public override void _Ready()
	{
		// The stakes stand up over the bodies they hold (DrawLayers.Air: what strikes, a marching wall);
		// the caltrops a fallen wall leaves lie on the floor under them, on a node of their own.
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		Rotation = IsRing ? 0f : Facing.Angle();
		if (SpellData?.GetBranchElement() is Element branch)
		{
			ElementColors.Ramp ramp = ElementColors.GetRamp(branch);
			ironLit = ramp.Mid;
			spark = ramp.Core;
			sparkHot = ramp.Hot;
		}
		if (SpellData != null)
		{
			spikedRail = SpellData.HasEvolution(SpikedRailId);
			caltrops = SpellData.HasEvolution(CaltropsId);
			rolling = !IsRing && SpellData.HasEvolution(RollingWallId);
			tempered = SpellData.HasEvolution(TemperedLineId);
		}
		if (spikedRail)
			ShoveSpeed *= RailThrow;
		if (IsRing)
			ringStakes = Math.Max(12, Mathf.RoundToInt(Mathf.Tau * RingRadius / StakeSpacing));
		if (caltrops)
		{
			ground = new Node2D { Name = "Caltrops", ZAsRelative = false, ZIndex = DrawLayers.Ground, Material = SpellInk.DitherByCell };
			AddChild(ground);
			ground.Draw += DrawCaltrops;
		}
		// The stakes, the sparks and the blood: cells on the world's grid, faded by dither.
		Material = SpellInk.DitherByCell;
		if (rolling)
		{
			StageEnvironmentKind kind = StageCatalog.GetByIndex(Global.SelectedStageIdx)?.EnvironmentKind ?? StageEnvironmentKind.Forest;
			SoilFor(kind, out soilLit, out soilBase, out soilShade, out soilDeep);
			furrowShader ??= GD.Load<Shader>(FurrowShaderPath);
			furrowMaterial = new ShaderMaterial { Shader = furrowShader };
			furrowMaterial.SetShaderParameter("facing", Facing.Normalized());
			furrowMaterial.SetShaderParameter("half_length", HalfLength);
			furrowMaterial.SetShaderParameter("spacing", HalfLength * 2f / Math.Max(1, LineStakes - 1));
			furrowMaterial.SetShaderParameter("march_speed", MarchSpeed);
			furrowMaterial.SetShaderParameter("seed", (float)(GetInstanceId() % 89));
			furrowMaterial.SetShaderParameter("soil_lit", soilLit);
			furrowMaterial.SetShaderParameter("soil_base", soilBase);
			furrowMaterial.SetShaderParameter("soil_shade", soilShade);
			furrowMaterial.SetShaderParameter("soil_deep", soilDeep);
			furrowMaterial.SetShaderParameter(ParamMarched, 0f);
			furrowMaterial.SetShaderParameter(ParamFallen, 0f);
			// On the ground, under every body; it turns with the wall, so its quad lies along the march.
			furrow = new Node2D { Name = "Furrow", ZAsRelative = false, ZIndex = DrawLayers.Ground, Material = furrowMaterial };
			AddChild(furrow);
			furrow.Draw += DrawFurrow;
		}
		if (spikedRail)
		{
			railStrokes = new SpellStrokes((IsRing ? ringStakes : MaxLineStakes) * 2 + 2);
			railStrokes.SetTones(SteelHi, SteelLit, SteelBase, SteelShade, SteelDeep, Occ);
			// Behind the stakes it is run along, at their layer: the stakes are what hits, so they stay
			// whole in front of it and the rail shows between them.
			rail = new Node2D { Name = "Rail", ShowBehindParent = true, Material = railStrokes.Material };
			AddChild(rail);
			rail.Draw += DrawRail;
		}
	}

	// The most stakes a line wall stands, for the rail's buffer: a long wall at the old spacing.
	private const int MaxLineStakes = 64;

	private Vector2 RingDir(int stake)
	{
		float a = stake * Mathf.Tau / Math.Max(1, ringStakes);
		return new Vector2(MathF.Cos(a), MathF.Sin(a));
	}

	// Which of the ring's stakes would stand on water or a wall, once the ring is where it was raised
	// (the caster places it after adding it, so not in _Ready).
	private void SurveyRing()
	{
		ringStakeBlocked = new bool[ringStakes];
		MazeNavigation nav = MazeNavigation.Active;
		for (int i = 0; i < ringStakes && nav != null; i++)
			ringStakeBlocked[i] = nav.IsBlockedAt(GlobalPosition + RingDir(i) * RingRadius);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		age += dt;
		QueueRedraw();
		ground?.QueueRedraw();
		rail?.QueueRedraw();
		if (furrow != null)
		{
			furrowMaterial.SetShaderParameter(ParamMarched, marched);
			furrowMaterial.SetShaderParameter(ParamFallen, Mathf.Clamp((age - LifeSeconds) / FurrowFallSeconds, 0f, 1f));
			furrow.QueueRedraw();
		}
		if (IsRing && ringStakeBlocked == null)
			SurveyRing();

		if (!Standing)
		{
			if (caltrops && strewnAt < 0f)
				Strew();
			bool caltropsLie = strewnAt >= 0f && age - strewnAt < CaltropSeconds && caltropCount > 0;
			// The furrow and the last clods go out after the wall does.
			bool earthLies = rolling && marched > 0f && age - LifeSeconds < MathF.Max(FurrowFallSeconds, ClodFlight + ClodLie);
			if (!caltropsLie && bleedCount == 0 && !earthLies)
			{
				QueueFree();
				return;
			}
		}
		else if (rolling && age >= RiseSeconds)
		{
			March(dt);
		}

		tickTimer -= dt;
		if (tickTimer > 0f || age < RiseSeconds)
			return;
		tickTimer = 0.1f;

		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;
		if (Standing)
			HoldTheLine();
		if (strewnAt >= 0f)
			TreadOnCaltrops();
		if (bleedCount > 0)
			Bleed();
	}

	// Rolling Wall: forward at a walk, unless the ground ahead of its middle is water or a wall.
	private void March(float dt)
	{
		Vector2 next = GlobalPosition + Facing * MarchSpeed * dt;
		if (MazeNavigation.Active?.IsBlockedAt(next + Facing * (HalfThickness + 6f)) == true)
			return;
		GlobalPosition = next;
		marched += MarchSpeed * dt;
		KickClods(dt);
	}

	// A clod kicked off a stake's foot every ClodEvery while the wall walks: thrown back over the furrow and
	// a little to one side, the stake and the throw picked by a hash of how many have been kicked.
	private void KickClods(float dt)
	{
		clodTimer -= dt;
		int stakes = LineStakes;
		while (clodTimer <= 0f)
		{
			clodTimer += ClodEvery;
			uint h = ++clodsKicked * 2654435761u;
			int stake = (int)(h % (uint)stakes);
			float side = ((h >> 8) & 0xFF) / 255f - 0.5f;
			float back = 0.6f + 0.4f * (((h >> 16) & 0xFF) / 255f);
			Vector2 across = new Vector2(-Facing.Y, Facing.X);
			clodFrom[nextClod] = ToGlobal(LineFoot(stake, stakes)) - Facing * 4f;
			clodVelocity[nextClod] = -Facing * (70f * back) + across * (side * 60f);
			clodBorn[nextClod] = age;
			nextClod = (nextClod + 1) % MaxClods;
		}
	}

	// The clods: two art pixels square or one, lit on the key's corner and shaded under it, an occlusion
	// shadow on the ground under each while it flies; landed, they crumble away by dither.
	private void DrawClods()
	{
		if (!rolling)
			return;
		float g = SpellInk.Grid;
		for (int i = 0; i < MaxClods; i++)
		{
			float s = age - clodBorn[i];
			if (clodBorn[i] <= 0f || s < 0f || s >= ClodFlight + ClodLie)
				continue;
			float t = MathF.Min(s, ClodFlight);
			float u = t / ClodFlight;
			Vector2 onGround = clodFrom[i] + clodVelocity[i] * t * (1f - 0.35f * u);
			float height = 4f * ClodHeight * u * (1f - u);
			float cover = s < ClodFlight ? 1f : 1f - (s - ClodFlight) / ClodLie;
			Vector2 at = SpellInk.Snap(onGround - new Vector2(0f, height));
			bool big = i % 3 != 0;
			if (height > g)
				DrawRect(new Rect2(SpellInk.Snap(onGround), new Vector2(big ? g * 2f : g, g)), new Color(Occ, cover));
			if (big)
			{
				DrawRect(new Rect2(at.X, at.Y, g, g), new Color(soilLit, cover));
				DrawRect(new Rect2(at.X + g, at.Y, g, g), new Color(soilBase, cover));
				DrawRect(new Rect2(at.X, at.Y + g, g, g), new Color(soilBase, cover));
				DrawRect(new Rect2(at.X + g, at.Y + g, g, g), new Color(soilShade, cover));
			}
			else
			{
				DrawRect(new Rect2(at.X, at.Y, g, g), new Color(i % 2 == 0 ? soilLit : soilBase, cover));
			}
		}
	}

	// The furrow's quad: in the wall's own frame (this child turns with it), from just behind the stakes
	// back to where the march began, the wall's length and a little more across. The shader decides the rest.
	private void DrawFurrow()
	{
		if (marched <= 0f)
			return;
		furrow.DrawRect(new Rect2(-marched - 4f, -HalfLength - 10f, marched + 6f, HalfLength * 2f + 20f), Colors.White);
	}

	private void HoldTheLine()
	{
		foreach (Node node in EnemyRegistry.Snapshot)
		{
			if (node is not Enemy enemy || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;

			Vector2 shove;
			if (IsRing)
			{
				// On the ring: inside it is shoved back in, outside it is kept out. A stake not raised
				// (blocked ground) holds nothing.
				Vector2 offset = enemy.GlobalPosition - GlobalPosition;
				float d = offset.Length();
				if (MathF.Abs(d - RingRadius) > HalfThickness + 10f || d < 1f)
					continue;
				int stake = Mathf.PosMod(Mathf.RoundToInt(MathF.Atan2(offset.Y, offset.X) / Mathf.Tau * ringStakes), ringStakes);
				if (ringStakeBlocked[stake])
					continue;
				shove = (d < RingRadius ? -offset : offset) / d * ShoveSpeed;
			}
			else
			{
				// In the wall's own frame: x across the wall (along Facing), y along it. A rolling wall also
				// takes what is just ahead of its face, so the pack is carried before it rather than
				// stepped through.
				Vector2 local = ToLocal(enemy.GlobalPosition);
				float ahead = rolling ? 18f : 0f;
				if (local.X < -(HalfThickness + 10f) || local.X > HalfThickness + 10f + ahead || Mathf.Abs(local.Y) > HalfLength + 8f)
					continue;
				shove = Facing * (rolling ? MathF.Max(ShoveSpeed, MarchSpeed * 1.6f) : ShoveSpeed);
			}

			enemy.ApplyKnockback(shove);
			ulong id = enemy.GetInstanceId();
			if (nextCutAt.TryGetValue(id, out float readyAt) && age < readyAt)
				continue;
			nextCutAt[id] = age + CutIntervalSeconds;
			PlayerRef.DealDamageToEnemy(enemy, CutDamage, source: SpellData);
			if (spikedRail && IsInstanceValid(enemy) && !enemy.IsDying)
				StartBleeding(enemy);
			// The Tempered Line: the blow hardens the wall.
			if (tempered)
				temper = MathF.Min(TemperMax, temper + TemperPerBlow);
		}
		if (tempered && temper >= TemperMax)
			Ring();
	}

	// The Tempered Line, struck enough: the wall rings, and everything along it is cut at the temper it
	// reached - along the line and RingReach either side, or that far either side of an Iron Ring's circle.
	// A stake not raised (blocked ground) still rings with the rest: the line is one piece of iron.
	private void Ring()
	{
		int cut = Mathf.RoundToInt(Damage * (1f + temper) * RingShare);
		foreach (Node node in EnemyRegistry.Snapshot)
		{
			if (node is not Enemy enemy || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			bool along;
			if (IsRing)
			{
				float d = GlobalPosition.DistanceTo(enemy.GlobalPosition);
				along = MathF.Abs(d - RingRadius) <= RingReach;
			}
			else
			{
				Vector2 local = ToLocal(enemy.GlobalPosition);
				along = MathF.Abs(local.X) <= RingReach && Mathf.Abs(local.Y) <= HalfLength + 16f;
			}
			if (along)
				PlayerRef.DealDamageToEnemy(enemy, cut, source: SpellData);
		}
		temper = 0f;
		rangAt = age;
		RingsRung++;
		SfxPlayer.Impact(Element.Metal, GlobalPosition);
	}

	// --- Spiked Rail -----------------------------------------------------------------------------

	private void StartBleeding(Enemy enemy)
	{
		for (int i = 0; i < bleedCount; i++)
		{
			if (bleeder[i] == enemy)
			{
				bleedLeft[i] = BleedSeconds;
				return;
			}
		}
		if (bleedCount >= MaxBleeding)
			return;
		bleeder[bleedCount] = enemy;
		bleedLeft[bleedCount] = BleedSeconds;
		bleedNext[bleedCount] = BleedTick;
		bleedCount++;
	}

	private void Bleed()
	{
		int bite = Math.Max(1, Mathf.RoundToInt(Damage * BleedShare));
		for (int i = bleedCount - 1; i >= 0; i--)
		{
			Enemy enemy = bleeder[i];
			bool gone = enemy == null || !IsInstanceValid(enemy) || enemy.IsDying;
			if (!gone)
			{
				bleedLeft[i] -= 0.1f;
				bleedNext[i] -= 0.1f;
				if (bleedNext[i] <= 0f)
				{
					bleedNext[i] += BleedTick;
					PlayerRef.DealDamageToEnemy(enemy, bite, source: SpellData);
				}
			}
			if (gone || bleedLeft[i] <= 0f || !IsInstanceValid(enemy) || enemy.IsDying)
			{
				bleedCount--;
				bleeder[i] = bleeder[bleedCount];
				bleedLeft[i] = bleedLeft[bleedCount];
				bleedNext[i] = bleedNext[bleedCount];
				bleeder[bleedCount] = null;
			}
		}
	}

	// --- Caltrops --------------------------------------------------------------------------------

	// The strip they lie in: past the line on its far side, or inside the ring. Scattered evenly through
	// it, so where they are drawn is where they cut; none drawn on water or a wall.
	private void Strew()
	{
		strewnAt = age;
		caltropCount = 0;
		var rng = new RandomNumberGenerator { Seed = (ulong)GetInstanceId() };
		MazeNavigation nav = MazeNavigation.Active;
		float band = CaltropDepth - HalfThickness;
		float length = IsRing ? Mathf.Tau * (RingRadius - HalfThickness - band * 0.5f) : HalfLength * 2f;
		int count = Math.Clamp(Mathf.RoundToInt(length * band / 260f), 6, MaxCaltrops);
		for (int i = 0; i < count; i++)
		{
			Vector2 local;
			if (IsRing)
			{
				float a = (i + rng.Randf() * 0.8f) * Mathf.Tau / count;
				float d = RingRadius - HalfThickness - rng.Randf() * band;
				local = new Vector2(MathF.Cos(a), MathF.Sin(a)) * d;
			}
			else
			{
				float y = Mathf.Lerp(-HalfLength, HalfLength, (i + rng.Randf() * 0.8f) / count);
				local = new Vector2(HalfThickness + rng.Randf() * band, y);
			}
			if (nav != null && nav.IsBlockedAt(ToGlobal(local)))
				continue;
			caltropAt[caltropCount++] = local;
		}
	}

	private bool OnCaltrops(Enemy enemy)
	{
		if (IsRing)
		{
			float d = GlobalPosition.DistanceTo(enemy.GlobalPosition);
			return d <= RingRadius - HalfThickness + 6f && d >= RingRadius - CaltropDepth - 6f;
		}
		Vector2 local = ToLocal(enemy.GlobalPosition);
		return local.X >= HalfThickness - 6f && local.X <= CaltropDepth + 6f && Mathf.Abs(local.Y) <= HalfLength + 6f;
	}

	// Every half second while they lie: a cut and a slow for what stands on them.
	private float caltropTimer;
	private void TreadOnCaltrops()
	{
		if (age - strewnAt >= CaltropSeconds || caltropCount == 0)
			return;
		caltropTimer -= 0.1f;
		if (caltropTimer > 0f)
			return;
		caltropTimer = 0.5f;
		int cut = Math.Max(1, Mathf.RoundToInt(Damage * CaltropShare));
		foreach (Node node in EnemyRegistry.Snapshot)
		{
			if (node is not Enemy enemy || !IsInstanceValid(enemy) || enemy.IsDying || !OnCaltrops(enemy))
				continue;
			PlayerRef.DealDamageToEnemy(enemy, cut, source: SpellData);
			if (IsInstanceValid(enemy) && !enemy.IsDying)
				enemy.ApplySlow(CaltropSlow, 0.7f);
		}
	}

	// A caltrop: four iron points, one always up, the three on the ground lit and shaded by the key and
	// occlusion under the side away from it; mirrored every other one, so a scatter does not repeat.
	// Drawn in world coordinates on the 2 px grid, faded by dither as they go.
	private void DrawCaltrops()
	{
		if (strewnAt < 0f || ground == null)
			return;
		float since = age - strewnAt;
		if (since >= CaltropSeconds)
			return;
		float fade = Mathf.Clamp((CaltropSeconds - since) / 0.4f, 0f, 1f);
		// Scattered out from the stakes' feet over the first fifth of a second, as the wall breaks.
		float thrown = Mathf.Clamp(since / 0.2f, 0f, 1f);
		ground.DrawSetTransformMatrix(ground.GetGlobalTransform().AffineInverse());
		for (int i = 0; i < caltropCount; i++)
		{
			Vector2 rest = caltropAt[i];
			Vector2 from = IsRing ? rest.Normalized() * RingRadius : new Vector2(0f, rest.Y);
			Vector2 at = SpellInk.Snap(ToGlobal(from.Lerp(rest, thrown)));
			float m = i % 2 == 0 ? 1f : -1f;
			Px(ground, at, 0, -2, ironLit, fade);
			Px(ground, at, 0, -1, SteelBase, fade);
			Px(ground, at, -m, 0, i % 2 == 0 ? ironLit : SteelShade, fade);
			Px(ground, at, 0, 0, SteelHi, fade);
			Px(ground, at, m, 0, i % 2 == 0 ? SteelShade : SteelBase, fade);
			Px(ground, at, m, 1, Occ, fade);
			Px(ground, at, 0, 1, Occ, fade);
		}
	}

	// One art pixel, (ax, ay) art pixels from a point on the world's grid.
	private static void Px(CanvasItem canvas, Vector2 at, float ax, float ay, Color c, float coverage) =>
		canvas.DrawRect(new Rect2(at.X + ax * SpellInk.Grid, at.Y + ay * SpellInk.Grid, SpellInk.Grid, SpellInk.Grid),
			new Color(c, coverage));

	// --- the stakes ------------------------------------------------------------------------------

	public override void _Draw()
	{
		// The wall turns to face the pack; its stakes stand up on the screen. Draw in the world.
		DrawSetTransformMatrix(GetGlobalTransform().AffineInverse());
		DrawClods();
		if (!Standing)
		{
			DrawBleeding();
			return;
		}
		float rise = Mathf.Clamp(age / RiseSeconds, 0f, 1f);
		float fade = Mathf.Clamp((LifeSeconds - age) / 0.3f, 0f, 1f);
		if (IsRing)
		{
			if (ringStakeBlocked == null)
				return;
			for (int i = 0; i < ringStakes; i++)
			{
				if (ringStakeBlocked[i])
					continue;
				DrawStake(ToGlobal(RingDir(i) * RingRadius), i, rise, fade);
			}
		}
		else
		{
			// A longer wall is more stakes at the same spacing, not the same nine spread thin: the stakes
			// are the thing that hits (.ai/spell-roster.md, section 2c), so a gap drawn in the fence would
			// say enemies walk through where they do not.
			int stakes = LineStakes;
			for (int i = 0; i < stakes; i++)
				DrawStake(ToGlobal(LineFoot(i, stakes)), i, rise, fade);
		}
		if (tempered)
			DrawRing(fade);
		DrawBleeding();
	}

	private static readonly ElementColors.Ramp MetalRamp = ElementColors.GetRamp(Element.Metal);

	// The Tempered Line ringing: the cut runs out from the stakes to the edge of its reach and goes - two
	// lines of the metal's white at RingReach either side, and the line itself white, for RingFlashSeconds.
	// On the art grid, faded by the dither like everything else the wall draws.
	private void DrawRing(float fade)
	{
		float since = age - rangAt;
		if (since >= RingFlashSeconds)
			return;
		float reach = Mathf.Clamp(since / (RingFlashSeconds * 0.4f), 0f, 1f) * RingReach;
		float cover = fade * (1f - Mathf.Clamp((since - RingFlashSeconds * 0.4f) / (RingFlashSeconds * 0.6f), 0f, 1f));
		float g = SpellInk.Grid;
		if (IsRing)
		{
			int segments = Math.Max(24, ringStakes * 3);
			for (int band = -1; band <= 1; band++)
			{
				float r = RingRadius + band * reach;
				Color c = band == 0 ? MetalRamp.Core : MetalRamp.Hot;
				for (int k = 0; k < segments; k++)
				{
					float a = k * Mathf.Tau / segments;
					Vector2 at = SpellInk.Snap(GlobalPosition + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r);
					DrawRect(new Rect2(at.X, at.Y, g, g), new Color(c, cover));
				}
			}
			return;
		}
		int steps = Math.Max(8, Mathf.RoundToInt((HalfLength * 2f + 16f) / g));
		for (int band = -1; band <= 1; band++)
		{
			Color c = band == 0 ? MetalRamp.Core : MetalRamp.Hot;
			for (int k = 0; k <= steps; k += band == 0 ? 1 : 2)
			{
				Vector2 local = new Vector2(band * reach, Mathf.Lerp(-HalfLength - 8f, HalfLength + 8f, k / (float)steps));
				Vector2 at = SpellInk.Snap(ToGlobal(local));
				// The line itself rings at the stakes' middle height, where the iron is.
				DrawRect(new Rect2(at.X, at.Y - (band == 0 ? 12f : 0f), g, g), new Color(c, cover));
			}
		}
	}

	private const float StakeTall = 26f;
	private const float StakeShort = 20f;
	private const float RailHeight = 15f;

	private int LineStakes => Math.Max(StakeCount, Mathf.RoundToInt(HalfLength * 2f / StakeSpacing) + 1);

	// A line stake's foot, in the wall's frame. Stakes stagger slightly so the line reads as driven by
	// hand, not drawn with a ruler.
	private Vector2 LineFoot(int i, int stakes) =>
		new Vector2(i % 2 == 0 ? -3f : 3f, Mathf.Lerp(-HalfLength, HalfLength, i / (float)(stakes - 1)));

	// One stake, upright on the screen whatever the wall's turn: three columns of art pixels - the key
	// side lit, the middle base, the far side shade - its point the steel's hi, an iron band a third of
	// the way up, and occlusion at the foot on the side away from the key. 26 px tall every third stake
	// and 20 the rest (StakeTall, StakeShort); it rises out of the ground over RiseSeconds.
	private void DrawStake(Vector2 footWorld, int i, float rise, float fade)
	{
		float height = (i % 3 == 0 ? StakeTall : StakeShort) * rise;
		// The Tempered Line: the iron heats toward white as the wall hardens, and is white the moment it rings.
		Color lit = ironLit, shade = SteelShade, body = SteelBase, point = SteelHi;
		if (tempered)
		{
			float heat = MathF.Max(temper / TemperMax, 1f - Mathf.Clamp((age - rangAt) / RingFlashSeconds, 0f, 1f));
			lit = ironLit.Lerp(MetalRamp.Core, heat);
			body = SteelBase.Lerp(MetalRamp.Mid, heat);
			shade = SteelShade.Lerp(MetalRamp.Edge, heat);
			point = SteelHi.Lerp(MetalRamp.Core, heat);
		}
		// Rolling Wall: the stakes heave in turn as the wall walks, a stamp running down the line.
		if (rolling && rise >= 1f)
			height -= 3f * MathF.Max(0f, MathF.Sin(age * 9f + i * 1.3f));
		int rows = Math.Max(1, Mathf.RoundToInt(height / SpellInk.Grid));
		Vector2 foot = SpellInk.Snap(footWorld);
		float g = SpellInk.Grid;
		float top = foot.Y - rows * g;
		// The contact: occlusion under the foot and off to the lower right.
		DrawRect(new Rect2(foot.X - g, foot.Y, g * 4f, g), new Color(Occ, fade));
		// The three columns, cut to a slanted point: the lit side stops one art pixel below the tip, the
		// shaded side two, so the top reads as a sharpened stake and not as a peg.
		if (rows > 1)
			DrawRect(new Rect2(foot.X - g, top + g, g, rows * g - g), new Color(lit, fade));
		if (rows > 2)
			DrawRect(new Rect2(foot.X + g, top + 2f * g, g, rows * g - 2f * g), new Color(shade, fade));
		DrawRect(new Rect2(foot.X, top, g, rows * g), new Color(body, fade));
		// The point catching the key, and the band a third of the way up.
		DrawRect(new Rect2(foot.X, top, g, g), new Color(point, fade));
		if (rows >= 5)
		{
			float band = foot.Y - Mathf.RoundToInt(rows * 0.34f) * g - g;
			DrawRect(new Rect2(foot.X - g, band, g, g), new Color(SteelBase, fade));
			DrawRect(new Rect2(foot.X, band, g, g), new Color(SteelShade, fade));
			DrawRect(new Rect2(foot.X + g, band, g, g), new Color(SteelDeep, fade));
		}

		// Lightning Rod: a spark crawling off the tip, on a few stakes at a time, re-struck ten
		// times a second: art pixels zig-zagging up from the point, white-hot at the ends.
		if (spark.A > 0f && rise >= 1f && ((int)(age * 10f) + i) % 3 == 0)
		{
			float jag = ((int)(age * 10f) + i * 7) % 2 == 0 ? 1f : -1f;
			Vector2 tip = new Vector2(foot.X, top);
			Px(this, tip, 0, -1, sparkHot, fade);
			Px(this, tip, jag, -2, spark, fade);
			Px(this, tip, 0, -3, sparkHot, fade);
			Px(this, tip, -jag, -4, spark, fade);
		}
	}

	// Spiked Rail: an iron bar run along the stakes at two-thirds of their height (RailHeight), with a spike
	// thrust out toward the enemy side between each pair - SpellStrokes, shaded by the key: the bar's top
	// lit, its underside in shade. Along the line in its own frame; round the ring segment by segment,
	// skipping the gaps where a stake was not raised. Drawn in world coordinates, on the rail's child.
	private void DrawRail()
	{
		if (railStrokes == null || !Standing)
			return;
		float rise = Mathf.Clamp(age / RiseSeconds, 0f, 1f);
		float fade = Mathf.Clamp((LifeSeconds - age) / 0.3f, 0f, 1f);
		Vector2 up = new Vector2(0f, -RailHeight * rise);
		float gone = 1f - fade;
		railStrokes.Begin();
		if (IsRing)
		{
			if (ringStakeBlocked == null)
				return;
			for (int i = 0; i < ringStakes; i++)
			{
				int next = (i + 1) % ringStakes;
				if (ringStakeBlocked[i] || ringStakeBlocked[next])
					continue;
				Vector2 a = ToGlobal(RingDir(i) * RingRadius) + up;
				Vector2 b = ToGlobal(RingDir(next) * RingRadius) + up;
				Bar(a, b, gone);
				// Spikes point both ways on a ring: in at the penned, out at the rest.
				Vector2 mid = (a + b) * 0.5f;
				Vector2 outward = RingDir(i).Lerp(RingDir(next), 0.5f).Normalized();
				Spike(mid, mid + outward * (i % 2 == 0 ? 8f : -8f), gone);
			}
		}
		else
		{
			Vector2 a = ToGlobal(new Vector2(0f, -HalfLength)) + up;
			Vector2 b = ToGlobal(new Vector2(0f, HalfLength)) + up;
			Bar(a, b, gone);
			int spikes = Math.Max(4, Mathf.RoundToInt(HalfLength * 2f / StakeSpacing));
			for (int i = 0; i < spikes; i++)
			{
				Vector2 at = ToGlobal(new Vector2(0f, Mathf.Lerp(-HalfLength, HalfLength, (i + 0.5f) / spikes))) + up;
				Spike(at, at + Facing * 9f, gone);
			}
		}
		railStrokes.Draw(rail, Colors.White);
	}

	private void Bar(Vector2 a, Vector2 b, float gone) => railStrokes.Add(new SpellStrokes.Stroke
	{
		A = a, B = b, HalfA = 2.5f, HalfB = 2.5f, SpineHalf = 0.5f, Spine = 2, Body = 2, Lit = true, Dissolve = gone,
	});

	private void Spike(Vector2 a, Vector2 b, float gone) => railStrokes.Add(new SpellStrokes.Stroke
	{
		A = a, B = b, HalfA = 1.5f, HalfB = 0.5f, Spine = 1, Body = 2, Lit = true, Dissolve = gone,
	});

	// Spiked Rail: blood dripping from what is bleeding - two drops at a time falling down the body, the
	// ember row's base as it leaves and its shade as it falls, going out by dither as the bleed runs out.
	private void DrawBleeding()
	{
		for (int i = 0; i < bleedCount; i++)
		{
			Enemy enemy = bleeder[i];
			if (enemy == null || !IsInstanceValid(enemy))
				continue;
			Vector2 body = enemy.GlobalPosition;
			float alpha = MathF.Min(1f, bleedLeft[i] / 0.4f);
			for (int k = 0; k < 2; k++)
			{
				float fall = (age * 1.8f + k * 0.5f + i * 0.37f) % 1f;
				Vector2 drop = new Vector2(k == 0 ? -4f : 3f, -10f + 16f * fall);
				DrawRect(WhipLimb.Cell(body + drop, WhipLimb.Grid * (fall < 0.7f ? 2f : 1f)),
					new Color(fall < 0.35f ? BloodFresh : Blood, alpha * (1f - fall * 0.5f)));
			}
		}
	}
}
