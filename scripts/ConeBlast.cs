using Godot;
using System;
using System.Linq;

namespace TheOneHeMissed.scripts;

// Cone of Cold (Ice x2 double-weight, issue #28): a single-cast cone-shaped burst in the aim/
// nearest-enemy direction, dealing damage and slowing everything hit. New "cone" archetype - a
// manual angle+range sweep rather than an Area2D collision shape, since all existing active spells
// are homing-projectile, self-pulse, or orbit shaped.
public partial class ConeBlast : Node2D
{
	[Export] public SpellData SpellData { get; set; }
	[Export] public int CurrentLevel { get; set; } = 1;
	[Export] public float BaseRange { get; set; } = 150f;
	[Export] public float ConeAngleDegrees { get; set; } = 60f;
	[Export] public bool GuaranteedSlow { get; set; } = true;
	[Export] public float SlowMultiplier { get; set; } = 0.5f;
	[Export] public float SlowDuration { get; set; } = 2.0f;
	public float DamageMultiplier { get; set; } = 1.0f;
	public float AreaMultiplier { get; set; } = 1.0f;
	public Node2D PlayerRef;

	private int damage = 5;
	private float range = 150f;
	private float scaledSlowMultiplier = 0.5f;
	private float scaledSlowDuration = 2.0f;

	// As long as the spray takes to fly out, land and cool (ImpactDebris).
	private const float WaveDuration = ImpactDebris.LifeSeconds;
	private static readonly ElementColors.Ramp IceRamp = ElementColors.GetRamp(Element.Ice);

	// The Long Winter (issue #61, ApotheosisCatalog): the cone opens into a full ring.
	private const float LongWinterFreezeSeconds = 1.6f;
	// Arctic Blast (cone_wide_angle) opens it to a half circle, as its card always said; until
	// 2026-10-26 it stayed 60 degrees and only its range grew.
	public const string ArcticBlastId = "cone_wide_angle";
	public const float ArcticBlastDegrees = 180f;
	private float HalfAngleRad => SpellData?.IsApotheosis == true ? Mathf.Pi : ConeHalfAngleRad;
	// The cone's own angle, never the Long Winter's ring: what Zero Point's relay is thrown with.
	private float ConeHalfAngleRad =>
		Mathf.DegToRad((SpellData?.HasEvolution(ArcticBlastId) == true ? ArcticBlastDegrees : ConeAngleDegrees) * 0.5f);
	private float waveTime = -1f;

	// ZERO POINT SHOCKWAVE (cone_zero_point_wave, the Frostweaver's level 8; .ai/spell-roster.md section
	// 2h). It was +150% damage and +40% reach on the same spray - a number on the same shape. Now the cold
	// that reaches the end of the cone gathers there, at a point on its centre line, into a star of ice
	// that brightens for RelaySeconds and bursts on into a SECOND cone thrown from that point the same way,
	// as wide and RelayLength of the first one's reach: what stood just behind the front of the pack is
	// caught too, and the eye sees the blast go on. The relay never overlaps the first cone (any point of
	// a cone at most 90 degrees wide thrown from the tip is further from the caster than the tip), so
	// nothing is struck twice. It keeps the cone's angle under the Long Winter's ring, so the form's ring
	// carries an aimed relay out past it. The +40% reach stays: the Long Winter's ring is measured on it.
	public const string ZeroPointId = "cone_zero_point_wave";
	public const float RelaySeconds = 0.16f;
	public const float RelayLength = 0.5f;
	private bool relayPending;
	private Vector2 relayTip;
	private Vector2 relayDirection;
	private float relayRange;
	private float relayHalfAngle;

	// THE GUST AND THE FROST (2026-11-01). Eric, playing 0.10.0: Cone of Cold "is very hard to see". The
	// spray of ice below was all it drew - a few dozen single art pixels, specks at the game's zoom, and on
	// the pale floors most of them the floor's own colour. Now the cast is drawn by
	// scenes/shaders/cone_of_cold.gdshader on a child quad (`gust`): a wall of frost crystals that leaves
	// his hand across exactly the cone's angle and lands on exactly its reach, outlined in the Frostweaver's
	// azure so it holds on paper and snow, white-hot at its leading edge so it holds on a dark floor, with
	// streaks of wind racing out behind it. And the HIT is drawn: every body the cone strikes flashes with a
	// star of frost the moment the front reaches it (frostAt), so what was caught reads in a crowd. The
	// spray still flies over both. Nothing here changes what is hit or when the damage lands.
	public const string GustShaderPath = "res://scenes/shaders/cone_of_cold.gdshader";
	// The gust's front reaches the radius in GustTravelSeconds on a cubic ease out (the shader's TRAVEL).
	public const float GustTravelSeconds = 0.15f;
	private static Shader gustShader;
	private static readonly StringName ParamRadius = "radius";
	private static readonly StringName ParamHalfAngle = "half_angle";
	private static readonly StringName ParamHeading = "heading";
	private static readonly StringName ParamCastPoint = "cast_point";
	private static readonly StringName ParamAge = "age";
	private static readonly StringName ParamSeed = "seed";
	private static readonly StringName ParamRelayAge = "relay_age";
	private static readonly StringName ParamRelayTip = "relay_tip";
	private static readonly StringName ParamRelayRadius = "relay_radius";
	private static readonly StringName ParamRelayHalf = "relay_half";
	private Node2D gust;
	private ShaderMaterial gustMaterial;

	// The frost on what was struck: the body, and the age at which the front reached it. A fixed array,
	// filled by Strike; past MaxFrost the rest are still struck, only not starred.
	private const int MaxFrost = 24;
	private const float FrostSeconds = 0.32f;
	private static readonly Color AzureShade = new Color(0.102f, 0.243f, 0.541f);   // azure shade #1A3E8A
	private readonly Node2D[] frostOn = new Node2D[MaxFrost];
	private readonly float[] frostAt = new float[MaxFrost];
	private int frostCount;

	public override void _Ready()
	{
		// The spray of ice is thrown through the air, over the bodies it hits (DrawLayers).
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		// The spray's last third fades in alpha (ImpactDebris, shared); drawn through this material that
		// alpha is coverage, so a shard dissolves cell by cell on the world's grid instead of going
		// see-through over the floor (the pixel-art pass, 2026-10-29).
		Material = SpellInk.DitherByCell;
		// The gust: one quad through its own shader, behind this node's spray and frost stars.
		gustShader ??= GD.Load<Shader>(GustShaderPath);
		gustMaterial = new ShaderMaterial { Shader = gustShader };
		gust = new Node2D { Name = "Gust", ShowBehindParent = true, Material = gustMaterial };
		AddChild(gust);
		gust.Draw += DrawGust;
		RefreshComputedStats();
	}

	public override void _ExitTree()
	{
		if (gust != null && IsInstanceValid(gust))
			gust.Draw -= DrawGust;
	}

	public void SetSpellLevel(int level)
	{
		CurrentLevel = Math.Max(1, level);
		RefreshComputedStats();
	}

	public void Fire(Vector2 origin, Vector2 direction)
	{
		// THE NODE IS NEVER TURNED (the pixel-art pass, 2026-10-29). It used to take the cast's heading as
		// its rotation, so every square of ice was a square in a turned frame: on any heading but the four
		// axes the spray drew as tilted diamonds off the world's 2-unit grid, the art pixels at odds with
		// everything round them. Now the node stands on that grid, unturned, and the spray is thrown at the
		// heading's angle from the exact cast point, a fraction of a cell off the node - so the squares
		// land on the grid and the reach is still measured from where the hit is.
		GlobalPosition = new Vector2(MathF.Round(origin.X / WhipLimb.Grid) * WhipLimb.Grid,
			MathF.Round(origin.Y / WhipLimb.Grid) * WhipLimb.Grid);
		Rotation = 0f;
		castPoint = origin - GlobalPosition;

		Vector2 dirNorm = direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector2.Right;
		heading = dirNorm.Angle();
		Strike(origin, dirNorm, range, HalfAngleRad, 0f);

		// Zero Point: the relay is resolved RelaySeconds later, from the tip as it was at the cast (the
		// node stays where it was cast; it does not follow the caster).
		relayPending = SpellData?.HasEvolution(ZeroPointId) == true;
		if (relayPending)
		{
			relayDirection = dirNorm;
			relayTip = origin + dirNorm * range;
			relayRange = range * RelayLength;
			relayHalfAngle = ConeHalfAngleRad;
		}

		waveTime = 0f;
		if (gustMaterial != null)
		{
			gustMaterial.SetShaderParameter(ParamRadius, range);
			gustMaterial.SetShaderParameter(ParamHalfAngle, HalfAngleRad);
			gustMaterial.SetShaderParameter(ParamHeading, heading);
			gustMaterial.SetShaderParameter(ParamCastPoint, castPoint);
			gustMaterial.SetShaderParameter(ParamSeed, (float)(GetInstanceId() % 97));
			gustMaterial.SetShaderParameter(ParamAge, 0f);
			gustMaterial.SetShaderParameter(ParamRelayAge, -1f);
			// The relay's tip, from this node; its reach is 0 (nothing drawn) unless Zero Point throws one.
			gustMaterial.SetShaderParameter(ParamRelayTip, castPoint + dirNorm * range);
			gustMaterial.SetShaderParameter(ParamRelayRadius, relayPending ? relayRange : 0f);
			gustMaterial.SetShaderParameter(ParamRelayHalf, relayHalfAngle);
		}
		QueueRedraw();
		gust?.QueueRedraw();

		var timer = GetTree().CreateTimer(WaveDuration + (relayPending ? RelaySeconds : 0f));
		timer.Timeout += () => { if (IsInstanceValid(this)) QueueFree(); };
	}

	// One spray's hit: everything within `reach` of `from` and inside `halfAngleRad` of `heading`.
	// `at` is this spray's age on the node's clock (0 the cast, RelaySeconds the relay), for the frost.
	private void Strike(Vector2 from, Vector2 heading, float reach, float halfAngleRad, float at)
	{
		var player = PlayerRef as Player;
		// Deep Freeze (the level 4 the Long Winter requires) freezes for its own seconds; the Long
		// Winter never freezes for less than its 1.6 s. Shorter than the 1.8 s cooldown on purpose:
		// a 2 s freeze on a 1.8 s cone held everything in front of the Frostweaver still forever,
		// which is the Long Winter's job and not a level 4's.
		float freezeSeconds = SpellData?.GetFreezeSecondsAtLevel(CurrentLevel) ?? 0f;
		if (SpellData?.IsApotheosis == true)
			freezeSeconds = MathF.Max(freezeSeconds, LongWinterFreezeSeconds);
		float knockbackSpeed = SpellData?.GetKnockbackSpeedAtLevel(CurrentLevel) ?? 0f;
		float reachSquared = reach * reach;

		// The registry, like every other AoE (CLAUDE.md). This walked CurrentScene's direct children,
		// which in a real run happens to be where enemies live and in the balance harness is not -
		// there the game is one node down, so Cone of Cold has dealt nothing in every bot run.
		foreach (Node node in EnemyRegistry.Snapshot)
		{
			if (node is not Node2D e || !IsInstanceValid(e))
				continue;
			var toEnemy = e.GlobalPosition - from;
			float distSquared = toEnemy.LengthSquared();
			if (distSquared > reachSquared || distSquared <= 0.0001f)
				continue;
			float dist = MathF.Sqrt(distSquared);

			float angle = MathF.Abs(heading.AngleTo(toEnemy / dist));
			if (angle <= halfAngleRad)
			{
				player?.DealDamageToEnemy(e, damage, source: SpellData);
				// The Long Winter (issue #61) and Deep Freeze: a root, through the existing freeze
				// path. A boss's MinSlowMultiplier turns it into a hard slow on its own, which is the spec.
				if (freezeSeconds > 0f && e.HasMethod("ApplySlow"))
					e.Call("ApplySlow", 0.0f, freezeSeconds);
				else if (GuaranteedSlow && e.HasMethod("ApplySlow"))
					e.Call("ApplySlow", scaledSlowMultiplier, scaledSlowDuration);
				// Glacier Tide: the wall of ice shoves what it hits straight away from the caster.
				if (knockbackSpeed > 0f && e is Enemy shoved)
					shoved.ApplyKnockback(toEnemy / dist * knockbackSpeed);
				// Its frost flashes as the drawn front reaches it: the front's own ease, inverted.
				if (frostCount < MaxFrost)
				{
					float reached = 1f - MathF.Cbrt(1f - MathF.Min(1f, dist / reach));
					frostOn[frostCount] = e;
					frostAt[frostCount] = at + reached * GustTravelSeconds;
					frostCount++;
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		if (waveTime < 0f)
			return;
		waveTime += (float)delta;
		if (relayPending && waveTime >= RelaySeconds)
		{
			relayPending = false;
			relayFired = true;
			Strike(relayTip, relayDirection, relayRange, relayHalfAngle, RelaySeconds);
		}
		if (gustMaterial != null)
		{
			gustMaterial.SetShaderParameter(ParamAge, waveTime);
			gustMaterial.SetShaderParameter(ParamRelayAge, relayFired ? waveTime - RelaySeconds : -1f);
		}
		QueueRedraw();
		gust?.QueueRedraw();
	}

	private bool relayFired;
	// The cast's heading (radians) and the exact cast point from this node (under a cell; see Fire).
	private float heading;
	private Vector2 castPoint;

	// The cone is a spray of ice thrown out from the player across exactly the angle that is hit,
	// the furthest of it landing on exactly the range - so the spray's own reach is the cone's. It was
	// a flat translucent wedge with a bright arc on its leading edge: an area marker (2026-10-23).
	// The Long Winter's full turn is the same spray all the way round.
	public override void _Draw()
	{
		if (waveTime < 0f)
			return;

		float halfAngle = HalfAngleRad;
		float sweep = 2f * halfAngle;
		// Denser than a burst: a cone is a spray, and a sparse one reads as a few stray shards.
		int count = Mathf.Clamp((int)(range * sweep / 8f), 12, 56);
		ImpactDebris.Draw(this, castPoint, range, waveTime, (uint)(GetInstanceId() % 9973), IceRamp, count,
			Colors.White, heading - halfAngle, sweep);

		if (relayPending || relayFired)
			DrawRelay();
		DrawFrost();
	}

	// The gust's quad: a square over the cone's reach round the cast point, and over the relay's round
	// the tip. The shader decides every pixel and discards the rest.
	private void DrawGust()
	{
		if (waveTime < 0f || waveTime >= WaveDuration + RelaySeconds)
			return;
		var box = new Rect2(castPoint - new Vector2(range, range), new Vector2(range, range) * 2f);
		if (relayPending || relayFired)
		{
			var tip = castPoint + new Vector2(MathF.Cos(heading), MathF.Sin(heading)) * range;
			float rr = range * RelayLength;
			box = box.Merge(new Rect2(tip - new Vector2(rr, rr), new Vector2(rr, rr) * 2f));
		}
		gust.DrawRect(box.Grow(WhipLimb.Grid * 2f), Colors.White);
	}

	// The frost on each body struck, the moment the front reaches it: a star of whole art pixels that
	// flares out and draws back in, white-hot at its heart, the azure outline at its tips so it holds on
	// a pale floor, dissolving by dither (this node's material) over its last third. Upright on the
	// world's grid, over the body, following it.
	private void DrawFrost()
	{
		float g = WhipLimb.Grid;
		for (int i = 0; i < frostCount; i++)
		{
			float s = (waveTime - frostAt[i]) / FrostSeconds;
			if (s < 0f || s >= 1f)
				continue;
			Node2D body = frostOn[i];
			if (body == null || !IsInstanceValid(body))
				continue;
			Vector2 at = body.GlobalPosition - GlobalPosition;
			float cover = s < 0.65f ? 1f : (1f - s) / 0.35f;
			int arm = s < 0.2f ? 4 : (s < 0.45f ? 5 : (s < 0.7f ? 4 : 3));
			for (int d = 1; d <= arm; d++)
			{
				Color c = new Color(d == arm ? AzureShade : (d <= 2 ? IceRamp.Hot : IceRamp.Mid), cover);
				DrawRect(WhipLimb.Cell(at + new Vector2(d * g, 0f), g), c);
				DrawRect(WhipLimb.Cell(at - new Vector2(d * g, 0f), g), c);
				DrawRect(WhipLimb.Cell(at + new Vector2(0f, d * g), g), c);
				DrawRect(WhipLimb.Cell(at - new Vector2(0f, d * g), g), c);
			}
			int diag = arm / 2;
			for (int d = 1; d <= diag; d++)
			{
				Color c = new Color(d == diag ? IceRamp.Edge : IceRamp.Mid, cover);
				DrawRect(WhipLimb.Cell(at + new Vector2(d * g, d * g), g), c);
				DrawRect(WhipLimb.Cell(at + new Vector2(-d * g, d * g), g), c);
				DrawRect(WhipLimb.Cell(at + new Vector2(d * g, -d * g), g), c);
				DrawRect(WhipLimb.Cell(at + new Vector2(-d * g, -d * g), g), c);
			}
			DrawRect(WhipLimb.Cell(at, g * 2f), new Color(IceRamp.Core, cover));
		}
	}

	// Zero Point: the star of ice gathering at the cone's tip while the spray is still in flight, flaring
	// as it bursts, then the second spray thrown on from it - drawn out of the reach it hits, as the first.
	// The node is unturned (Fire), so the tip is out along the heading from the cast point, and the star
	// stands upright on the world's grid whatever way the cone was thrown - a sparkle, not a turned cross.
	private void DrawRelay()
	{
		var tip = castPoint + new Vector2(MathF.Cos(heading), MathF.Sin(heading)) * range;
		float since = waveTime - RelaySeconds;
		if (since < 0.08f)
		{
			// Gathering (since < 0), then a flash for a few frames as it goes.
			float gather = Mathf.Clamp((waveTime - ImpactDebris.TravelSeconds * 0.5f) / (RelaySeconds - ImpactDebris.TravelSeconds * 0.5f), 0f, 1f);
			float flash = since >= 0f ? 1f - since / 0.08f : 0f;
			float arm = WhipLimb.Grid * (1f + 4f * gather + 4f * flash);
			if (gather > 0f || flash > 0f)
			{
				Color core = IceRamp.Core, hot = IceRamp.Hot, mid = IceRamp.Mid;
				float g = WhipLimb.Grid;
				// A four-pointed star of whole art pixels, a ring of the ramp's mid round a white-hot heart.
				for (float d = g; d <= arm; d += g)
				{
					Color c = d <= arm * 0.5f ? hot : mid;
					DrawRect(WhipLimb.Cell(tip + new Vector2(d, 0f), g), c);
					DrawRect(WhipLimb.Cell(tip - new Vector2(d, 0f), g), c);
					DrawRect(WhipLimb.Cell(tip + new Vector2(0f, d), g), c);
					DrawRect(WhipLimb.Cell(tip - new Vector2(0f, d), g), c);
				}
				float diag = arm * 0.45f;
				for (float d = g; d <= diag; d += g)
				{
					DrawRect(WhipLimb.Cell(tip + new Vector2(d, d), g), mid);
					DrawRect(WhipLimb.Cell(tip + new Vector2(-d, d), g), mid);
					DrawRect(WhipLimb.Cell(tip + new Vector2(d, -d), g), mid);
					DrawRect(WhipLimb.Cell(tip + new Vector2(-d, -d), g), mid);
				}
				DrawRect(WhipLimb.Cell(tip, g * (2f + 2f * flash)), core);
			}
		}
		if (since >= 0f)
		{
			float reach = range * RelayLength;
			float relayHalf = ConeHalfAngleRad;
			// Denser than the first spray for its size: it is the smaller of the two, and has to read on its own.
			int relayCount = Mathf.Clamp((int)(reach * relayHalf * 2f / 5f), 12, 48);
			ImpactDebris.Draw(this, tip, reach, since, (uint)(GetInstanceId() % 9973) + 7919u, IceRamp, relayCount,
				Colors.White, heading - relayHalf, relayHalf * 2f);
		}
	}

	private void RefreshComputedStats()
	{
		damage = Math.Max(1, Mathf.RoundToInt((SpellData?.GetDamageAtLevel(CurrentLevel) ?? 5) * DamageMultiplier));
		range = MathF.Max(20f, (SpellData?.GetRangeAtLevel(CurrentLevel) ?? BaseRange) * AreaMultiplier);
		float slowPower = SpellData?.GetEffectValueAtLevel(SpellEffect.SlowPower, CurrentLevel) ?? 0f;
		float slowDurationBonus = SpellData?.GetEffectValueAtLevel(SpellEffect.SlowDuration, CurrentLevel) ?? 0f;
		scaledSlowMultiplier = Mathf.Clamp(SlowMultiplier - slowPower, 0f, 0.98f);
		scaledSlowDuration = MathF.Max(0.1f, SlowDuration + slowDurationBonus);
	}
}
