using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// Aegis Ward's milestones, the ones that show (.ai/spell-roster.md section 2h). The spell itself is no
// node: its cooldown refills Player's shield pool (Player, the aegis_ward branch) and TakeDamage spends it.
// What its cards add is drawn and run here - one node in the arena, fixed pools, nothing allocated per
// bolt, shard or strike - and on the Air layer, over the bodies: a ward is round the wizard, not on the
// floor.
//
//   Rampart     (level 4, was Quick Bastion) - while the shield holds it stands out from him as a dome,
//               and an enemy bolt that reaches the dome breaks on it, costing the shield only RampartCost
//               of the bolt. The dome is drawn only where a bolt breaks on it: a flash of its curve -
//               the shield is the aura Player already draws; this is the bolt guard (2d-bis) the spell
//               is, made visible.
//   Reforged    (level 4, +1 Metal, was Fortified Aegis) - every time the ward comes back it is forged
//               out of the air: ReforgeShards shards of iron fly in to him from ReforgeReach round him,
//               cutting what they pass for ReforgeShare of the shield's size.
//   Reflective Plating (level 4) - Player.TryReflectAbsorbed's strike now shows: a shaft of light from
//               him to the enemy that takes it back.
//
// Thorn Crystal Shell's burst and Sun Aegis's flash on a break stay in Player (TryCrystalShellBurst).
public partial class AegisWard : Node2D
{
	public const string RampartId = "aegis_ward_quick_bastion";
	public const string ReforgedId = "aegis_ward_fortified";
	public const string ReflectiveId = "aegis_ward_reflective";

	// --- Rampart -------------------------------------------------------------------------------------
	// The dome's radius from his centre: two bodies out, past his own body and the HurtBox, so a bolt is
	// seen to break on something that is not him.
	public const float RampartRadius = 52f;
	// What a bolt broken on the dome costs the shield, as a share of the bolt's damage.
	public const float RampartCost = 0.5f;
	private const float DomeFlashSeconds = 0.28f;
	// The arc of the dome lit by one bolt, either side of where it broke (radians).
	private const float DomeFlashHalfArc = 0.55f;

	// --- Reforged ------------------------------------------------------------------------------------
	public const int ReforgeShards = 8;
	public const float ReforgeReach = 140f;
	public const float ReforgeShare = 0.6f;
	private const float ReforgeSeconds = 0.32f;
	// A shard's half-width as a hit, before the body it meets.
	private const float ShardHalfWidth = 4f;
	private const int MaxShards = 24;
	private const int MaxHitsPerShard = 16;
	// The shard's sheet (tools/art/aegis_ward.py): ShardDirections square cells in one strip, cell i
	// pointing at i x 11.25 degrees. An API: change both.
	public const string ShardSheetPath = "res://assets/bonelight/effects/aegis-shard.png";
	public const int ShardDirections = 32;

	// --- Reflective Plating ---------------------------------------------------------------------------
	private const float ShaftSeconds = 0.14f;
	// How the shaft is drawn (visual only - the strike is dealt the frame it is shown): shot out from him
	// over ShaftShootSeconds, held whole to ShaftHoldSeconds, dissolving to ShaftSeconds; leaving him
	// ShaftLeaves out from his centre, at the edge of his body.
	private const float ShaftShootSeconds = 0.035f;
	private const float ShaftHoldSeconds = 0.05f;
	private const float ShaftLeaves = 14f;
	private const int MaxShafts = 8;
	private const int MaxFlashes = 12;

	// The balance report's count (_BalanceHarness), across the run.
	public static int StatBoltsWarded { get; private set; }
	public static int StatShardHits { get; private set; }
	public static int StatReforges { get; private set; }
	public static int StatReflects { get; private set; }

	public static bool HasStats => StatBoltsWarded + StatShardHits + StatReflects > 0;

	public static string StatsLine() =>
		$"aegis ward   : {StatBoltsWarded} bolts broken on the rampart; {StatReforges} reforges cutting {StatShardHits} times; {StatReflects} strikes reflected";

	private struct Flash
	{
		public float Angle;
		public float Age;
		public bool Live;
	}

	private struct Shard
	{
		public Vector2 Dir;      // from him out to where it started, unit
		public float Age;
		public float Last;       // how far out it was last frame
		public int Damage;
		public int Hits;
		public uint Seed;
		public bool Live;
	}

	private struct Shaft
	{
		public Vector2 From;
		public Vector2 To;
		public float Age;
		public bool Live;
	}

	public Player PlayerRef { get; set; }
	public SpellData SpellData { get; set; }

	private readonly Flash[] flashes = new Flash[MaxFlashes];
	private readonly Shard[] shards = new Shard[MaxShards];
	private readonly ulong[] shardHits = new ulong[MaxShards * MaxHitsPerShard];
	private readonly Shaft[] shafts = new Shaft[MaxShafts];
	private readonly List<Enemy> nearBuffer = new();
	private readonly ElementColors.Ramp light = ElementColors.GetRamp(Element.Light);
	private readonly ElementColors.Ramp metal = ElementColors.GetRamp(Element.Metal);
	private float reach = ReforgeReach;
	private int nextFlash;
	private int nextShard;
	private int nextShaft;
	private uint seedClock;
	private bool drewLastFrame;
	private Texture2D shardSheet;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		// Every fade a dissolve on the world's 2 px cell: an alpha drawn here is coverage (see "drawing").
		Material = SpellInk.DitherByCell;
		shardSheet = GD.Load<Texture2D>(ShardSheetPath);
		if (shardSheet == null)
			GD.PushError($"AegisWard: {ShardSheetPath} did not load (not imported?); Reforged's shards would cut unseen.");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;
		GuardBolts();
		bool any = false;
		for (int i = 0; i < MaxShards; i++)
		{
			if (!shards[i].Live)
				continue;
			any = true;
			TickShard(i, dt);
		}
		for (int i = 0; i < MaxFlashes; i++)
		{
			if (!flashes[i].Live)
				continue;
			any = true;
			flashes[i].Age += dt;
			if (flashes[i].Age >= DomeFlashSeconds)
				flashes[i].Live = false;
		}
		for (int i = 0; i < MaxShafts; i++)
		{
			if (!shafts[i].Live)
				continue;
			any = true;
			shafts[i].Age += dt;
			if (shafts[i].Age >= ShaftSeconds)
				shafts[i].Live = false;
		}
		// Once more after the last one goes, or its final frame would stay on screen.
		if (any || drewLastFrame)
			QueueRedraw();
		drewLastFrame = any;
	}

	// --- Rampart ---------------------------------------------------------------------------------

	// Every enemy bolt in flight that has reached the dome while the shield holds breaks on it: no hit on
	// him, a burst of light where it broke, and the shield pays RampartCost of it (Player.SpendShield,
	// which breaks the shield - and sets off what a break sets off - if that empties it). The bolt list is
	// EnemyProjectile.Live, a handful at most.
	private void GuardBolts()
	{
		if (SpellData == null || !SpellData.HasEvolution(RampartId) || PlayerRef.ShieldPoints <= 0)
			return;
		IReadOnlyList<EnemyProjectile> bolts = EnemyProjectile.Live;
		if (bolts.Count == 0)
			return;
		Vector2 centre = PlayerRef.GlobalPosition;
		float reachSquared = RampartRadius * RampartRadius;
		for (int b = bolts.Count - 1; b >= 0 && PlayerRef.ShieldPoints > 0; b--)
		{
			EnemyProjectile bolt = bolts[b];
			if (bolt == null || !IsInstanceValid(bolt) || !bolt.InFlight)
				continue;
			Vector2 offset = bolt.GlobalPosition - centre;
			if (offset.LengthSquared() > reachSquared)
				continue;
			int cost = Math.Max(bolt.Damage > 0 ? 1 : 0, Mathf.CeilToInt(bolt.Damage * RampartCost));
			bolt.Destroy(Element.Light);
			StatBoltsWarded++;
			AddFlash(offset.Angle());
			if (cost > 0)
				PlayerRef.SpendShield(cost);
		}
	}

	private void AddFlash(float angle)
	{
		int slot = nextFlash;
		for (int i = 0; i < MaxFlashes; i++)
		{
			int s = (nextFlash + i) % MaxFlashes;
			if (!flashes[s].Live)
			{
				slot = s;
				break;
			}
		}
		nextFlash = (slot + 1) % MaxFlashes;
		flashes[slot] = new Flash { Angle = angle, Live = true };
	}

	// --- Reforged ---------------------------------------------------------------------------------

	/// <summary>
	/// The ward has come back (Player's aegis cast restored the shield): under Reforged, it is forged out
	/// of the air - ReforgeShards shards fly in to him from ReforgeReach x Area, each cutting what its
	/// path crosses for ReforgeShare of <paramref name="shieldSize"/>.
	/// </summary>
	public void Reforge(int shieldSize, float area)
	{
		if (SpellData == null || !SpellData.HasEvolution(ReforgedId))
			return;
		StatReforges++;
		reach = ReforgeReach * MathF.Sqrt(MathF.Max(0.25f, area));
		int damage = Math.Max(1, Mathf.RoundToInt(shieldSize * ReforgeShare));
		float turn = (seedClock * 0.6180339f % 1f) * Mathf.Tau;
		for (int k = 0; k < ReforgeShards; k++)
		{
			float angle = turn + k * Mathf.Tau / ReforgeShards;
			int slot = nextShard;
			for (int i = 0; i < MaxShards; i++)
			{
				int s = (nextShard + i) % MaxShards;
				if (!shards[s].Live)
				{
					slot = s;
					break;
				}
			}
			nextShard = (slot + 1) % MaxShards;
			shards[slot] = new Shard
			{
				Dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle)),
				Last = reach,
				Damage = damage,
				Seed = ++seedClock,
				Live = true,
			};
		}
		SfxPlayer.Instance?.PlayAt(SfxCatalog.PlayerShieldAbsorb, PlayerRef.GlobalPosition, 0.06f, SfxCatalog.PlayerShieldAbsorb, 120);
	}

	// How far out from him a shard is, `age` into its flight: fast at first, slowing as it closes - it is
	// drawn in, not thrown.
	private float ShardOut(float age)
	{
		float u = Mathf.Clamp(age / ReforgeSeconds, 0f, 1f);
		float eased = 1f - (1f - u) * (1f - u);
		return Mathf.Lerp(reach, 10f, eased);
	}

	// A shard strikes everything whose centre its path crossed this frame - the stretch between where it
	// was and where it is, along its line in to him - once.
	private void TickShard(int i, float dt)
	{
		ref Shard s = ref shards[i];
		s.Age += dt;
		float now = ShardOut(s.Age);
		Vector2 centre = PlayerRef.GlobalPosition;
		Vector2 a = centre + s.Dir * s.Last;
		Vector2 b = centre + s.Dir * now;
		s.Last = now;
		Strike(ref s, i, a, b);
		if (s.Age >= ReforgeSeconds)
			s.Live = false;
	}

	private void Strike(ref Shard s, int i, Vector2 a, Vector2 b)
	{
		Vector2 path = b - a;
		float length = path.Length();
		float hit = ShardHalfWidth + RiptideWave.BodyAllowance;
		EnemyRegistry.CollectNear(a + path * 0.5f, length * 0.5f + hit, nearBuffer);
		float lengthSquared = MathF.Max(0.0001f, length * length);
		int row = i * MaxHitsPerShard;
		foreach (Enemy enemy in nearBuffer)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			Vector2 p = enemy.GlobalPosition;
			float t = Mathf.Clamp((p - a).Dot(path) / lengthSquared, 0f, 1f);
			if (p.DistanceSquaredTo(a + path * t) > hit * hit)
				continue;
			ulong id = enemy.GetInstanceId();
			bool seen = false;
			for (int h = 0; h < s.Hits; h++)
			{
				if (shardHits[row + h] == id)
				{
					seen = true;
					break;
				}
			}
			if (seen)
				continue;
			if (s.Hits < MaxHitsPerShard)
				shardHits[row + s.Hits++] = id;
			StatShardHits++;
			PlayerRef.DealDamageToEnemy(enemy, s.Damage, source: SpellData);
		}
		nearBuffer.Clear();
	}

	// --- Reflective Plating ---------------------------------------------------------------------------

	/// <summary>The reflected strike, drawn: a shaft of light from him to the enemy that took it back.</summary>
	public void ShowReflect(Vector2 from, Vector2 to)
	{
		StatReflects++;
		int slot = nextShaft;
		for (int i = 0; i < MaxShafts; i++)
		{
			int s = (nextShaft + i) % MaxShafts;
			if (!shafts[s].Live)
			{
				slot = s;
				break;
			}
		}
		nextShaft = (slot + 1) % MaxShafts;
		shafts[slot] = new Shaft { From = from, To = to, Live = true };
		QueueRedraw();
	}

	// --- drawing ---------------------------------------------------------------------------------
	//
	// THE PIXEL-ART PASS (2026-11-01, .ai/art-replacement-manifest.md section 9). All three were DrawLine
	// and DrawColoredPolygon: smooth rotated quads, a translucent 6 px wash under every line, every part
	// fading by alpha. Now every part is whole art pixels on the world's 2 px grid in opaque ramp stops -
	// the dome's flash and the shaft stamped cell by cell (PixelInk), the shard a region of a pre-rotated
	// sheet (tools/art/aegis_ward.py) - and every fade is a dissolve: the node draws through
	// SpellInk.DitherByCell, so an alpha is coverage. Nothing allocated; the numbers each part is drawn
	// from are the ones its hit reads (RampartRadius, the shard's line, the struck enemy).

	public override void _Draw()
	{
		Transform2D toLocal = GlobalTransform.AffineInverse();
		Vector2 centre = PlayerRef != null && IsInstanceValid(PlayerRef) ? toLocal * PlayerRef.GlobalPosition : Vector2.Zero;
		DrawFlashes(centre);
		DrawShards(centre);
		DrawShafts(toLocal);
	}

	private static Vector2 Snap(Vector2 p) =>
		new Vector2(MathF.Round(p.X / WhipLimb.Grid) * WhipLimb.Grid, MathF.Round(p.Y / WhipLimb.Grid) * WhipLimb.Grid);

	// Where a bolt broke, the dome lights: a stretch of its curve either side of the break, one art pixel
	// on RampartRadius - the radius a bolt is stopped at - white-hot at the break and cooling down Light's
	// ramp along it, thickened at the break, with a contour of Light's gold edge just outside it (what
	// holds it on a pale floor) and a band of Metal plates just inside, broken at their seams, lit on the
	// side the key reaches. It opens out from the break and dissolves from its ends in, the break last:
	// the dome is there, it is struck, and it is gone in a quarter of a second.
	private void DrawFlashes(Vector2 centre)
	{
		const float g = WhipLimb.Grid;
		for (int i = 0; i < MaxFlashes; i++)
		{
			if (!flashes[i].Live)
				continue;
			Flash f = flashes[i];
			float u = f.Age / DomeFlashSeconds;
			float spread = DomeFlashHalfArc * (0.6f + 0.4f * u);
			// A step of under one art pixel along the curve, so it is walked without a gap.
			int steps = Math.Max(4, (int)MathF.Ceiling(2f * spread * RampartRadius / g));
			for (int k = 0; k <= steps; k++)
			{
				float t = -1f + 2f * k / steps;
				float a = f.Angle + t * spread;
				var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
				float heat = 1f - MathF.Abs(t);
				// Whole until its dissolve begins - at 15% of the flash at the ends, 60% at the break -
				// then thinning to nothing at the end of it.
				float from = 0.15f + 0.45f * heat;
				float cover = u <= from ? 1f : 1f - (u - from) / (1f - from);
				if (cover <= 0f)
					continue;
				Color c = heat > 0.75f ? light.Core : heat > 0.45f ? light.Hot : heat > 0.2f ? light.Mid : light.Edge;
				if (heat > 0.2f)
					PixelInk.Cell(this, centre + dir * (RampartRadius + g), light.Edge, cover);
				PixelInk.Cell(this, centre + dir * RampartRadius, c, cover);
				if (heat > 0.7f)
					PixelInk.Cell(this, centre + dir * (RampartRadius - g), light.Hot, cover);
				// The plates: a band three cells in, broken at a seam every 0.4 of the arc.
				float seam = MathF.Abs(t * 2.5f - MathF.Round(t * 2.5f));
				if (MathF.Abs(t) < 0.85f && seam > 0.12f)
				{
					// Lit where the dome's surface faces the key (upper left).
					bool lit = dir.X + dir.Y < 0f;
					PixelInk.Cell(this, centre + dir * (RampartRadius - 3f * g), lit ? metal.Hot : metal.Mid, cover * 0.95f);
				}
			}
		}
	}

	// A shard of iron flying in: a region of the 32-heading sheet, the one nearest its line in to him -
	// a splinter of Metal lit on the key's side with a white-hot point and a streak of the ward's Light
	// behind it. It dissolves in its last 0.06 s, inside his body, as it is set into the ward.
	private void DrawShards(Vector2 centre)
	{
		if (shardSheet == null)
			return;
		float cell = shardSheet.GetHeight();
		for (int i = 0; i < MaxShards; i++)
		{
			if (!shards[i].Live)
				continue;
			Shard s = shards[i];
			Vector2 at = centre + s.Dir * ShardOut(s.Age);
			float coverage = Mathf.Clamp((ReforgeSeconds - s.Age) / 0.06f, 0f, 1f);
			float heading = (-s.Dir).Angle();
			int cellIndex = ((int)MathF.Round(heading / Mathf.Tau * ShardDirections) % ShardDirections + ShardDirections) % ShardDirections;
			Vector2 corner = Snap(at - new Vector2(cell * 0.5f, cell * 0.5f));
			DrawTextureRectRegion(shardSheet, new Rect2(corner, new Vector2(cell, cell)),
				new Rect2(cellIndex * cell, 0f, cell, cell), new Color(1f, 1f, 1f, coverage));
		}
	}

	// Reflective Plating: a shaft of light from the edge of his body to the enemy that takes the strike
	// back - a white-hot spine one art pixel wide in a body of Light's hot, in a dithered glow of its gold
	// edge - shot out in its first few frames, then dissolving, the spine last; and where it lands, a
	// sparkle of whole cells. It ends on the struck enemy, the one body it hurts: not an area.
	private void DrawShafts(Transform2D toLocal)
	{
		const float g = WhipLimb.Grid;
		for (int i = 0; i < MaxShafts; i++)
		{
			if (!shafts[i].Live)
				continue;
			Shaft s = shafts[i];
			Vector2 a = toLocal * s.From;
			Vector2 b = toLocal * s.To;
			Vector2 path = b - a;
			float length = path.Length();
			if (length < g)
				continue;
			Vector2 dir = path / length;
			// From the edge of his body, not across his sprite, when there is room.
			if (length > 2f * ShaftLeaves)
				a += dir * ShaftLeaves;
			float reach = Mathf.Clamp(s.Age / ShaftShootSeconds, 0f, 1f);
			Vector2 head = a + (b - a) * reach;
			float cover = s.Age < ShaftHoldSeconds ? 1f : 1f - (s.Age - ShaftHoldSeconds) / (ShaftSeconds - ShaftHoldSeconds);
			PixelInk.Line(this, a, head, light.Edge, 5, cover * 0.45f);
			PixelInk.Line(this, a, head, light.Hot, 3, cover);
			PixelInk.Line(this, a, head, light.Core, 1, cover * 1.6f);
			if (reach < 1f)
				continue;
			// The sparkle where it lands: a white heart, hot arms two cells long, mid at the tips.
			PixelInk.Cell(this, b, light.Core, MathF.Min(1f, cover * 1.6f));
			for (int d = 1; d <= 2; d++)
			{
				Color c = d == 2 ? light.Mid : light.Hot;
				float cd = Mathf.Clamp(cover * (1.5f - 0.25f * d), 0f, 1f);
				PixelInk.Cell(this, b + new Vector2(d * g, 0f), c, cd);
				PixelInk.Cell(this, b - new Vector2(d * g, 0f), c, cd);
				PixelInk.Cell(this, b + new Vector2(0f, d * g), c, cd);
				PixelInk.Cell(this, b - new Vector2(0f, d * g), c, cd);
			}
		}
	}
}
