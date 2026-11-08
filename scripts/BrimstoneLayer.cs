using Godot;
using System;

namespace TheOneHeMissed.scripts;

/// <summary>
/// Fireball's Brimstone (the Ascension on <c>fireball_hellfire_nova</c>'s id, .ai/spell-roster.md 2h):
/// where a fireball bursts, a lump of brimstone lies burning and spits small fireballs at the nearest
/// enemies until it burns out. One node per arena, every stone and every spit a slot in a fixed array.
/// </summary>
/// <remarks>
/// The card was Hellfire Nova: +150% area and +100% damage, the same blast bigger. A fireball is the
/// sniper round of the fire spells (section 3), so what it leaves behind is a small one of itself that
/// keeps firing: the stone keeps the blast's spot dangerous for a few seconds, picking off what walks
/// back in, which no other card in the roster does (a pool burns what stands in it; this reaches out).
///
/// The stone lies on the ground, under the bodies (its glow and its flare on the fire's z, one over the
/// craters', as a crater's fire is); a spit is in the air, over them, and bursts through FireburstLayer
/// in Hellfire's red rows, its debris through ImpactDebrisLayer, each to exactly the radius it tested.
/// A stone is never laid on blocked ground (water, a wall): the fireball can burst over a tarn's edge,
/// and a burning rock floating on the water would be a lie. Nothing is allocated per stone or per spit,
/// and the node stops processing when nothing is live.
/// </remarks>
public partial class BrimstoneLayer : Node2D
{
	/// <summary>How long a stone burns.</summary>
	public const float StoneSeconds = 3f;
	/// <summary>A spit every this many seconds while a stone burns.</summary>
	public const float SpitSeconds = 0.4f;
	/// <summary>How far a stone reaches for a target, as a share of the blast it came from.</summary>
	public const float ReachShare = 1.6f;
	/// <summary>A spit's damage, as a share of the fireball's.</summary>
	public const float SpitDamageShare = 0.35f;
	/// <summary>A spit's burst, as a share of the blast's radius.</summary>
	public const float SpitRadiusShare = 0.3f;
	private const float SpitSpeed = 430f;
	private const int MaxStones = 8;
	private const int MaxSpits = 32;
	// A burning stone is a light in a dark chapter (LightPool, .ai/light-pool.md section 3): a few times its glow's
	// width, throbbing and flaring with the glow drawn under it and cooling with it; a spit is a small fireball's
	// light in flight, and its burst lights itself (FireburstLayer).
	private const float StoneLightReach = 90f;
	private const float SpitLightReach = 40f;

	private struct Stone
	{
		public Vector2 At;
		public float Age;
		public float Reach;
		public float SpitRadius;
		public int SpitDamage;
		public float NextSpit;
		public float Flare;
		public uint Seed;
		public bool Live;
	}

	private struct Spit
	{
		public Vector2 From;
		public Vector2 To;
		public float Age;
		public float Flight;
		public float Radius;
		public int Damage;
		public bool Live;
	}

	private readonly Stone[] stones = new Stone[MaxStones];
	private readonly Spit[] spits = new Spit[MaxSpits];
	private int nextStone;
	private int nextSpit;
	private uint seedClock;
	private Player player;
	private SpellData source;
	private Node2D air;

	private static BrimstoneLayer current;
	private static readonly ElementColors.Ramp FireRamp = ElementColors.GetRamp(Element.Fire);
	// Hellfire's red rows (fireball.gdshader): the stone's crust and the spit's tail.
	private static readonly Color RedLit = new Color(0.416f, 0.125f, 0.157f);
	private static readonly Color RedShade = new Color(0.165f, 0.055f, 0.078f);
	private static readonly Color RedDeep = new Color(0.086f, 0.027f, 0.039f);

	/// <summary>
	/// Lays a stone at <paramref name="globalAt"/>, burning for <see cref="StoneSeconds"/>; it reaches
	/// <see cref="ReachShare"/> of <paramref name="blastRadius"/> and spits for
	/// <see cref="SpitDamageShare"/> of <paramref name="damage"/>.
	/// </summary>
	public static void Lay(Node2D arena, Player player, SpellData source, Vector2 globalAt, float blastRadius, int damage)
	{
		if (arena == null || !IsInstanceValid(arena) || player == null || !IsInstanceValid(player))
			return;
		MazeNavigation nav = MazeNavigation.Active;
		if (nav != null && nav.IsBlockedAt(globalAt))
			return;
		if (current == null || !IsInstanceValid(current) || !current.IsInsideTree() || current.GetParent() != arena)
		{
			current = new BrimstoneLayer { Name = "Brimstone" };
			arena.AddChild(Player.TagSpell(current));
		}
		current.player = player;
		current.source = source;
		current.Add(globalAt, blastRadius, damage);
	}

	public override void _Ready()
	{
		ZIndex = CraterGround.FireZ;
		ZAsRelative = false;
		air = new Node2D { Name = "Spits", ZIndex = DrawLayers.Air, ZAsRelative = false };
		AddChild(air);
		air.Draw += DrawSpits;
		SetProcess(false);
	}

	public override void _ExitTree()
	{
		if (air != null && IsInstanceValid(air))
			air.Draw -= DrawSpits;
		if (current == this)
			current = null;
	}

	private void Add(Vector2 at, float blastRadius, int damage)
	{
		stones[nextStone] = new Stone
		{
			At = at,
			Reach = blastRadius * ReachShare,
			SpitRadius = MathF.Max(16f, blastRadius * SpitRadiusShare),
			SpitDamage = Math.Max(1, Mathf.RoundToInt(damage * SpitDamageShare)),
			// The first spit a beat after the blast, so it is seen to come from the stone.
			NextSpit = 0.25f,
			Seed = ++seedClock * 2654435761u,
			Live = true,
		};
		nextStone = (nextStone + 1) % MaxStones;
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		bool any = false;
		for (int i = 0; i < MaxStones; i++)
		{
			if (!stones[i].Live)
				continue;
			any = true;
			Stone s = stones[i];
			s.Age += dt;
			s.Flare = MathF.Max(0f, s.Flare - dt * 5f);
			if (s.Age >= StoneSeconds)
				s.Live = false;
			else
			{
				s.NextSpit -= dt;
				if (s.NextSpit <= 0f)
				{
					s.NextSpit += SpitSeconds;
					if (SpitFrom(s))
						s.Flare = 1f;
				}
				// The glow _Draw lays under it, as a light.
				if (LightPool.Lit)
				{
					float left = Mathf.Clamp((StoneSeconds - s.Age) / 0.5f, 0f, 1f);
					float throb = 0.5f + 0.5f * MathF.Sin(s.Age * 9f + (s.Seed % 7));
					LightPool.Light(s.At, StoneLightReach, 0.8f * (0.35f + 0.25f * throb + 0.4f * s.Flare) * left);
				}
			}
			stones[i] = s;
		}
		for (int i = 0; i < MaxSpits; i++)
		{
			if (!spits[i].Live)
				continue;
			any = true;
			Spit p = spits[i];
			p.Age += dt;
			if (p.Age >= p.Flight)
			{
				p.Live = false;
				Land(p);
			}
			else
				LightPool.Light(p.From.Lerp(p.To, p.Age / p.Flight), SpitLightReach, 0.5f);
			spits[i] = p;
		}
		QueueRedraw();
		air.QueueRedraw();
		if (!any)
			SetProcess(false);
	}

	// The nearest enemy in reach, led by its walk for as long as the spit flies. False with none in reach.
	private bool SpitFrom(in Stone s)
	{
		Enemy best = null;
		float bestD2 = s.Reach * s.Reach;
		foreach (Enemy e in EnemyRegistry.Snapshot)
		{
			if (!IsInstanceValid(e) || e.IsDying)
				continue;
			float d2 = e.GlobalPosition.DistanceSquaredTo(s.At);
			if (d2 < bestD2)
			{
				bestD2 = d2;
				best = e;
			}
		}
		if (best == null)
			return false;
		Vector2 from = s.At + new Vector2(0f, -8f);
		float flight = MathF.Max(0.08f, MathF.Sqrt(bestD2) / SpitSpeed);
		spits[nextSpit] = new Spit
		{
			From = from,
			To = best.GlobalPosition + best.Velocity * flight,
			Flight = flight,
			Radius = s.SpitRadius,
			Damage = s.SpitDamage,
			Live = true,
		};
		nextSpit = (nextSpit + 1) % MaxSpits;
		return true;
	}

	private void Land(in Spit p)
	{
		if (player == null || !IsInstanceValid(player))
			return;
		float r2 = p.Radius * p.Radius;
		foreach (Enemy e in EnemyRegistry.Snapshot)
		{
			if (IsInstanceValid(e) && e.GlobalPosition.DistanceSquaredTo(p.To) <= r2)
				player.DealDamageToEnemy(e, p.Damage, source: source);
		}
		if (GetParent() is Node2D arena)
		{
			FireburstLayer.Spawn(arena, p.To, p.Radius, FireburstLayer.Style.Hellfire);
			ImpactDebrisLayer.Spawn(arena, p.To, p.Radius, FireRamp, Colors.White);
		}
	}

	// The stones, on the ground: a dark lump crusted in Hellfire's red with molten seams that throb, a
	// glow under it, flaring white as it spits, cooling for its last half second. On the 2 px grid.
	public override void _Draw()
	{
		for (int i = 0; i < MaxStones; i++)
		{
			Stone s = stones[i];
			if (!s.Live)
				continue;
			Vector2 at = ToLocal(s.At);
			float left = Mathf.Clamp((StoneSeconds - s.Age) / 0.5f, 0f, 1f);
			float throb = 0.5f + 0.5f * MathF.Sin(s.Age * 9f + (s.Seed % 7));
			float glow = (0.35f + 0.25f * throb + 0.4f * s.Flare) * left;
			DrawSetTransform(at, 0f, new Vector2(1f, CraterGround.Squash));
			DrawCircle(Vector2.Zero, 26f, new Color(FireRamp.Mid, 0.25f * glow));
			DrawCircle(Vector2.Zero, 16f, new Color(FireRamp.Hot, 0.35f * glow));
			DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
			// The lump: rows of cells, its crown lit, darkest at the foot.
			for (int y = -5; y <= 1; y++)
			{
				int half = y == -5 ? 2 : (y == -4 ? 4 : (y == 1 ? 4 : 5));
				for (int x = -half; x < half; x++)
				{
					uint h = (uint)((x + 8) * 73856093 ^ (y + 8) * 19349663) ^ s.Seed;
					Color c = y >= 0 ? RedDeep : (y <= -4 ? RedLit : RedShade);
					// The seams: molten, brighter as it flares, going dark as it cools.
					if ((h % 3) == 0 && y > -5)
						c = left < 0.5f ? FireRamp.Edge : (s.Flare > 0.4f ? FireRamp.Core : (throb > 0.5f ? FireRamp.Hot : FireRamp.Mid));
					DrawRect(new Rect2(Snap(at.X) + x * WhipLimb.Grid, Snap(at.Y) + y * WhipLimb.Grid, WhipLimb.Grid, WhipLimb.Grid), c);
				}
			}
		}
	}

	// The spits, in the air: a white-hot head and three cells of tail cooling into Hellfire's red,
	// stepping back along the line it flies - the meteor's streak, small.
	private void DrawSpits()
	{
		for (int i = 0; i < MaxSpits; i++)
		{
			Spit p = spits[i];
			if (!p.Live)
				continue;
			float k = Mathf.Clamp(p.Age / MathF.Max(0.01f, p.Flight), 0f, 1f);
			Vector2 head = air.ToLocal(p.From.Lerp(p.To, k)) + new Vector2(0f, -MathF.Sin(MathF.PI * k) * 14f);
			Vector2 back = (p.From - p.To).Normalized() * 4f;
			for (int t = 3; t >= 0; t--)
			{
				Color c = t switch { 0 => FireRamp.Core, 1 => FireRamp.Hot, 2 => FireRamp.Edge, _ => RedLit };
				float size = t < 2 ? 6f : 4f;
				air.DrawRect(WhipLimb.Cell(head + back * t, size), c);
			}
		}
	}

	private static float Snap(float v) => MathF.Round(v / WhipLimb.Grid) * WhipLimb.Grid;
}
