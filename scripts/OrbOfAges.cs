using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// ORB OF AGES (.ai/merlin.md): Merlin's own spell, and no one else's (UnlockDefinition.ExclusiveTo). Eric,
// 2026-11-02: he should have "a starter spell that no one else has", and he is "one that uses his magic to
// protect others". A wizard standing in a warded circle, the light on his staff's head sent round it.
//
// A cast DRAWS THE WARD on the ground where he stands - anchored there, not on him - and the orb leaves the staff
// and ricochets inside it, vertex to vertex of a seven-pointed star ({7/3}: each stroke skips three points), so the
// strokes cross the whole ring and leave its middle, where he stands, quiet. Everything a stroke passes through is
// struck once a stroke and WEAKENED (it hits him softer for a while): the dark that comes into his circle comes in
// weak. The ward stands WardSeconds, and closes at once if he steps out of it; then the orb flies home, and only
// then does the cooldown begin (Player.RestartSpellCooldown). So the demand is to hold the ground he chose.
//
// Against the other wards: Aegis Ward is a pool of shield on him that breaks and recharges; Kindled Ward is a mote
// that drifts off and burns on its own; this is a PLACE he must stay in, cut by a ricochet. Nothing here shoves
// (Iron Palisade's wall, Sundering's ring) and nothing heals (.ai/difficulty-curve.md).
//
// The ring is drawn, and it is not an area marker (.ai/spell-roster.md 2c): it is the ward's edge, the line he must
// not cross, a state like Taproot's ring at his feet. The orb and its strokes are what hit; under Burning Rim the
// rim itself does too.
//
// Its cards, switched on by id (SpellEvolutionCatalog.SetupOrbOfAgesEvolutions):
//   Burning Rim (4)   - whatever crosses into the ring is struck by it and Weakened (RimShare of a hit).
//   Twin Orbs (4)     - one more orb runs the star (a second comes with level 6, the other way round).
//   Missile Ward (4)  - every enemy bolt inside the ring is burnt out while the ward stands.
//   Great Circle (8)  - a ring half as wide again (its Area).
//   Walking Ward (8)  - the ward is drawn round him and walks with him; only its time closes it.
// And his Apotheosis:
//   The Once and Future (rewind) - each time an orb finishes its star it flies it again backwards and the ward's
//   hour turns back: it stands for as long as he stays inside it.
public partial class OrbOfAges : Node2D
{
	public const string SpellId = "orb_of_ages";
	public const string BurningRimId = "orb_of_ages_amplified_impact";
	public const string TwinOrbsId = "orb_of_ages_hyper_acceleration";
	public const string MissileWardId = "orb_of_ages_reactive_catalyst";
	public const string GreatCircleId = "orb_of_ages_titan_cataclysm";
	public const string WalkingWardId = "orb_of_ages_infinite_horizon";
	public const string OnceAndFutureId = "orb_of_ages_once_and_future";

	/// <summary>The star: seven points, each stroke skipping three ({7/3}), so a whole star is seven strokes.</summary>
	public const int StarPoints = 7;
	public const int StarSkip = 3;

	/// <summary>What a stroke does besides its hit: the share of his damage the struck lose, and for how long.</summary>
	public const float WeakenShare = 0.3f;
	public const float WeakenSeconds = 2.5f;
	/// <summary>Burning Rim: what crossing into the ring costs, as a share of a stroke's hit.</summary>
	public const float RimShare = 0.6f;
	/// <summary>How far past the rim he may step before the ward is broken - a stumble is not leaving.</summary>
	public const float LeaveMargin = 8f;
	/// <summary>How close to a stroke an enemy's centre must be to be struck, before Area.</summary>
	public const float StrokeReach = 18f;
	/// <summary>How fast the orb flies home once the ward closes.</summary>
	public const float HomeSpeed = 900f;
	private const float FadeSeconds = 0.3f;

	// Set by Player.FireOrbOfAges before _Ready.
	public SpellData SpellData { get; set; }
	public Player PlayerRef { get; set; }
	public int Damage { get; set; } = 8;
	public float Radius { get; set; } = 140f;
	public float OrbSpeed { get; set; } = 760f;
	public float WardSeconds { get; set; } = 6f;
	public float WeakenDurationScale { get; set; } = 1f;
	public int Orbs { get; set; } = 1;
	/// <summary>The angle of the star's first point: toward the nearest enemy when it was cast.</summary>
	public float FirstPointAngle { get; set; }
	public bool BurningRim { get; set; }
	public bool MissileWard { get; set; }
	public bool Walking { get; set; }
	public bool OnceAndFuture { get; set; }

	/// <summary>True from the moment it closes: the orbs are on their way home, and nothing is struck.</summary>
	public bool Closing => phase != Phase.Standing;
	/// <summary>Where the ward stands now (on him under Walking Ward).</summary>
	public Vector2 Centre => Walking && IsLive(PlayerRef) ? PlayerRef.GlobalPosition : centre;

	private enum Phase { Standing, Homing, Fading }

	private sealed class Orb
	{
		public Vector2 At;
		public Vector2 Last;
		public int Vertex;      // the point it is flying to
		public int Step;        // +StarSkip or -StarSkip: which way round the star it runs
		public int Strokes;     // strokes finished this star (0..StarPoints)
		public bool Leaving;    // still on its way out of the staff to its first point
		public bool Home;
		public readonly HashSet<Enemy> Struck = new();
	}

	private Phase phase = Phase.Standing;
	private Vector2 centre;
	private float age;
	private float fade;
	private readonly List<Orb> orbs = new(2);
	// Burning Rim's crossing test: who was inside last frame, swapped with who is inside now (no allocation).
	private HashSet<Enemy> insideBefore = new();
	private HashSet<Enemy> insideNow = new();

	// The look (the drawing section below): the ward's canvas and the two that draw behind it, and the orbs'.
	private Node2D ward;
	private Node2D star;
	private Node2D stroke;
	private Node2D air;
	// What the ward and the star last drew, so each redraws only when that changes - not every frame.
	private int drawnRing = -1;
	private int drawnTone = -1;
	private long drawnStar = long.MinValue;
	private bool strokeDrawn;
	private bool orbsDrawn;

	private static readonly ElementColors.Ramp Light = ElementColors.GetRamp(Element.Light);
	private static readonly ElementColors.Ramp Arcane = ElementColors.GetRamp(Element.Arcane);

	public override void _Ready()
	{
		// The ward lies on the ground under the bodies; the orb flies in the air over them (DrawLayers). Each is a
		// canvas of its own, on the world's 2 px grid (the drawing section below).
		ZAsRelative = false;
		ZIndex = DrawLayers.Ground;
		ward = new Node2D { Name = "Ward", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Ground };
		star = new Node2D { Name = "Star", ShowBehindParent = true };
		stroke = new Node2D { Name = "Stroke", ShowBehindParent = true };
		ward.AddChild(star);
		ward.AddChild(stroke);
		AddChild(ward);
		air = new Node2D { Name = "Orbs", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Air };
		AddChild(air);
		air.GlobalPosition = Vector2.Zero;
		ward.Draw += DrawWard;
		star.Draw += DrawStar;
		stroke.Draw += DrawStroke;
		air.Draw += DrawOrbs;

		centre = GlobalPosition;
		Vector2 from = IsLive(PlayerRef) ? PlayerRef.GlobalPosition + PlayerRef.StaffTipLocal() : GlobalPosition;
		int count = Math.Max(1, Orbs);
		for (int i = 0; i < count; i++)
		{
			// The first runs the star one way; Twin Orbs' second the other, from the opposite side of the ring.
			int start = (i * StarPoints / count) % StarPoints;
			orbs.Add(new Orb
			{
				At = from,
				Last = from,
				Vertex = start,
				Step = i % 2 == 0 ? StarSkip : -StarSkip,
				Leaving = true,
			});
		}
		UpdateLook();
	}

	public override void _ExitTree()
	{
		if (ward != null)
		{
			ward.Draw -= DrawWard;
			star.Draw -= DrawStar;
			stroke.Draw -= DrawStroke;
		}
		if (air != null)
			air.Draw -= DrawOrbs;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		age += dt;
		if (Walking)
			GlobalPosition = Centre;

		switch (phase)
		{
			case Phase.Standing:
				Stand(dt);
				break;
			case Phase.Homing:
				FlyHome(dt);
				break;
			default:
				fade += dt;
				if (fade >= FadeSeconds)
				{
					QueueFree();
					return;
				}
				break;
		}
		UpdateLook();
	}

	private void Stand(float dt)
	{
		if (!IsLive(PlayerRef))
		{
			Close();
			return;
		}
		// Broken the moment he steps out of it (never under Walking Ward, which is round him); worn out when its
		// time runs - except under The Once and Future, whose hour turns back while he holds it (below).
		bool inside = PlayerRef.GlobalPosition.DistanceTo(Centre) <= Radius + LeaveMargin;
		if (!inside || (age >= WardSeconds && !OnceAndFuture))
		{
			Close();
			return;
		}

		foreach (Orb orb in orbs)
			Fly(orb, dt);
		if (BurningRim)
			BurnTheRim();
		if (MissileWard)
			BurnBolts();
	}

	private void Fly(Orb orb, float dt)
	{
		orb.Last = orb.At;
		float budget = OrbSpeed * dt;
		// Corners within one frame's flight are all turned this frame, so a fast orb never overshoots a point.
		for (int guard = 0; guard < 4 && budget > 0f; guard++)
		{
			Vector2 target = Point(orb.Vertex);
			Vector2 to = target - orb.At;
			float length = to.Length();
			if (length > budget)
			{
				orb.At += to / length * budget;
				break;
			}
			orb.At = target;
			budget -= length;
			Strike(orb, orb.Last, orb.At);
			orb.Last = orb.At;
			Turn(orb);
		}
		Strike(orb, orb.Last, orb.At);
	}

	// A point reached: the stroke is done, so whatever it struck may be struck by the next.
	private void Turn(Orb orb)
	{
		orb.Struck.Clear();
		if (orb.Leaving)
		{
			orb.Leaving = false;
		}
		else if (++orb.Strokes >= StarPoints)
		{
			orb.Strokes = 0;
			// THE ONCE AND FUTURE: the star is flown again backwards, and the ward's hour turns back with it.
			if (OnceAndFuture)
			{
				orb.Step = -orb.Step;
				age = 0f;
			}
		}
		orb.Vertex = Mod(orb.Vertex + orb.Step, StarPoints);
	}

	private void Strike(Orb orb, Vector2 a, Vector2 b)
	{
		if (orb.Leaving || !IsLive(PlayerRef))
			return;
		// A wider ring is a bigger orb: the stroke widens with it, so a grown ward is not a thinner net.
		float reach = StrokeReach * MathF.Max(1f, Radius / Player.OrbWardRadius);
		float reachSquared = reach * reach;
		Vector2 ab = b - a;
		float abLengthSquared = MathF.Max(0.0001f, ab.LengthSquared());
		foreach (Enemy body in EnemyRegistry.Snapshot)
		{
			if (!IsLive(body) || orb.Struck.Contains(body))
				continue;
			Vector2 p = body.GlobalPosition;
			float t = Mathf.Clamp((p - a).Dot(ab) / abLengthSquared, 0f, 1f);
			if (p.DistanceSquaredTo(a + ab * t) > reachSquared)
				continue;
			orb.Struck.Add(body);
			Hit(body, Damage);
		}
	}

	private void Hit(Enemy body, int amount)
	{
		PlayerRef.DealDamageToEnemy(body, Math.Max(1, amount), 0f, true, SpellData);
		if (IsLive(body))
			body.ApplyWeaken(WeakenShare, WeakenSeconds * WeakenDurationScale);
	}

	// BURNING RIM: whatever was outside the ring last frame and is inside it now crossed it, and pays for it.
	private void BurnTheRim()
	{
		insideNow.Clear();
		Vector2 c = Centre;
		float r2 = Radius * Radius;
		int rimDamage = Math.Max(1, Mathf.RoundToInt(Damage * RimShare));
		foreach (Enemy body in EnemyRegistry.Snapshot)
		{
			if (!IsLive(body) || body.GlobalPosition.DistanceSquaredTo(c) > r2)
				continue;
			insideNow.Add(body);
			// The first frame counts everyone already inside as inside: the ward is drawn round them, they did
			// not cross it.
			if (age > 0.05f && !insideBefore.Contains(body))
				Hit(body, rimDamage);
		}
		(insideBefore, insideNow) = (insideNow, insideBefore);
	}

	// MISSILE WARD: every enemy bolt inside the ring is burnt out, in the ward's light.
	private void BurnBolts()
	{
		Vector2 c = Centre;
		float r2 = Radius * Radius;
		IReadOnlyList<EnemyProjectile> bolts = EnemyProjectile.Live;
		for (int i = bolts.Count - 1; i >= 0; i--)
		{
			EnemyProjectile bolt = bolts[i];
			if (bolt != null && bolt.InFlight && bolt.GlobalPosition.DistanceSquaredTo(c) <= r2)
				bolt.Destroy(Element.Light);
		}
	}

	private void Close()
	{
		if (phase != Phase.Standing)
			return;
		phase = Phase.Homing;
		age = 0f;
		foreach (Orb orb in orbs)
			orb.Struck.Clear();
	}

	// The ward is closed: the orbs fly home to the staff, striking nothing, and the cooldown starts when the last
	// is in his hand.
	private void FlyHome(float dt)
	{
		bool allHome = true;
		Vector2 home = IsLive(PlayerRef) ? PlayerRef.GlobalPosition + PlayerRef.StaffTipLocal() : Centre;
		foreach (Orb orb in orbs)
		{
			if (orb.Home)
				continue;
			Vector2 to = home - orb.At;
			float step = HomeSpeed * dt;
			if (to.Length() <= step || age > 1.5f)
			{
				orb.At = home;
				orb.Home = true;
				continue;
			}
			orb.At += to.Normalized() * step;
			allHome = false;
		}
		if (!allHome)
			return;
		phase = Phase.Fading;
		fade = 0f;
		if (IsLive(PlayerRef))
			PlayerRef.RestartSpellCooldown(SpellId);
	}

	private Vector2 Point(int vertex)
	{
		float angle = FirstPointAngle + vertex * Mathf.Tau / StarPoints;
		return Centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Radius;
	}

	private static int Mod(int value, int m) => ((value % m) + m) % m;

	private static bool IsLive(Enemy body) =>
		body != null && IsInstanceValid(body) && !body.IsDying && body.Health > 0 && body.IsInGroup("enemies");

	private static bool IsLive(Player player) => player != null && IsInstanceValid(player) && !player.IsDead;

	// --- drawing -----------------------------------------------------------------------------------------------
	//
	// THE LOOK IS PIXEL ART (2026-11-03; the pixel-art pass's rules, .ai/art-replacement-manifest.md section 9, which
	// Cyclone Slash had on 2026-10-29 for the same complaint). It was DrawArc rings and DrawLine strokes at whole
	// world-unit widths - half an art pixel, on no grid - and DrawCircle orbs: thin smooth lines in a frame where every
	// sprite round them stands on the world's 2 px art grid. Now every part is stamped in whole cells of that grid by
	// PixelInk, opaque, from the two ramps, and a fade is a dissolve through the Bayer matrix, never alpha. Cyclone
	// Slash's whirlwind is a shader because it is an area; what the pass found to be lines - a chain's arc, a thread,
	// a crescent in flight - it stamped, and a ring and a star are lines.
	//
	//   the ward    the midpoint circle one art pixel thick at Radius, in Light's edge (the gold), and a second a
	//               cell's gap inside it in Arcane's edge: the line he must not cross, which reads on the dark floors
	//               by its gold and on the Sketchbook's paper by its violet. Seven points of light where the star
	//               touches it, a white-hot cell with arms of the hot and mid stops, set in a square of the gold.
	//   the star    the strokes an orb has flown this star, a cell wide in Light's mid; the stroke it is flying, a
	//               seam of the hot stop on a band of the edge three cells wide, from the point it turned at to it.
	//   the orb     a disc of the light ramp in a rim of Arcane's edge, white-hot up and to the left, as wide as a
	//               ward of this size makes it: a wider ring is a bigger orb (StrokeReach grows by the same share).
	//
	// Closing, the light goes out of the ring and the arcane is left (the edge and points step to Arcane's ramp, the
	// star is gone); fading, that dissolves cell by cell. The hit is measured from Centre and Radius exactly; the
	// drawing stands on the cell Centre falls in, so it is never more than an art pixel off what it strikes.
	//
	// Cheap on purpose (.ai/performance.md): the ward's canvas stands on that cell and draws round its own origin, so
	// it is MOVED, not redrawn, when the ward walks; the ring and its points redraw only when the ring's size or tone
	// changes, the flown strokes only when an orb turns a point; only the stroke in flight and the orbs are drawn
	// every frame. Nothing allocates.

	/// <summary>The gap, in art pixels, between the gold ring and the arcane one inside it, plus the line itself.</summary>
	private const int InnerRingCells = 2;
	private const float Grid = PixelInk.Grid;

	private float Coverage => phase == Phase.Fading ? Mathf.Clamp(1f - fade / FadeSeconds, 0f, 1f) : 1f;

	private int RingCells => Math.Max(InnerRingCells + 2, (int)MathF.Round(Radius / Grid));

	// Standing, closing, or how far through its dissolve (sixteen steps, the Bayer matrix's).
	private int Tone => phase switch
	{
		Phase.Standing => 100,
		Phase.Homing => 99,
		_ => (int)MathF.Ceiling(Coverage * 16f),
	};

	private void UpdateLook()
	{
		if (ward == null)
			return;
		Vector2 c = Centre;
		var corner = new Vector2(MathF.Floor(c.X / Grid) * Grid, MathF.Floor(c.Y / Grid) * Grid);
		if (ward.GlobalPosition != corner)
			ward.GlobalPosition = corner;

		int ring = RingCells;
		int tone = Tone;
		if (ring != drawnRing || tone != drawnTone)
		{
			drawnRing = ring;
			drawnTone = tone;
			ward.QueueRedraw();
		}
		// The star changes when an orb turns a point (or sets out from the staff), and with the ring.
		long key = ring * 131L + tone;
		foreach (Orb orb in orbs)
			key = key * 257L + orb.Vertex + 8 * (orb.Strokes + 8 * ((orb.Leaving ? 1 : 0) + 2 * (orb.Step > 0 ? 1 : 0)));
		if (key != drawnStar)
		{
			drawnStar = key;
			star.QueueRedraw();
		}
		if (phase == Phase.Standing || strokeDrawn)
			stroke.QueueRedraw();
		if (phase != Phase.Fading || orbsDrawn)
			air.QueueRedraw();
	}

	// A point of the star, as the cell's centre in the ward canvas's frame: the true point's direction, out the
	// ring's whole number of cells, so it lands on the ring that is drawn.
	private Vector2 PointCell(int vertex)
	{
		float angle = FirstPointAngle + vertex * Mathf.Tau / StarPoints;
		int ring = RingCells;
		return new Vector2(MathF.Round(MathF.Cos(angle) * ring) * Grid + Grid * 0.5f, MathF.Round(MathF.Sin(angle) * ring) * Grid + Grid * 0.5f);
	}

	// The ring and its seven points of light.
	private void DrawWard()
	{
		float cover = Coverage;
		if (cover <= 0f)
			return;
		bool lit = phase == Phase.Standing;
		int ring = RingCells;
		PixelInk.Circle(ward, 0, 0, ring, lit ? Light.Edge : Arcane.Edge, cover);
		PixelInk.Circle(ward, 0, 0, ring - InnerRingCells, Arcane.Edge, cover);
		for (int v = 0; v < StarPoints; v++)
		{
			Vector2 p = PointCell(v);
			// Lit, its four corners in the gold too, so the point still shows on the Sketchbook's paper, where the
			// white-hot cross alone is lost.
			if (lit)
			{
				PixelInk.Cell(ward, p + new Vector2(Grid, Grid), Light.Edge, cover);
				PixelInk.Cell(ward, p + new Vector2(-Grid, Grid), Light.Edge, cover);
				PixelInk.Cell(ward, p + new Vector2(Grid, -Grid), Light.Edge, cover);
				PixelInk.Cell(ward, p + new Vector2(-Grid, -Grid), Light.Edge, cover);
			}
			for (int arm = 1; arm <= (lit ? 2 : 1); arm++)
			{
				Color tone = arm == 1 ? (lit ? Light.Hot : Arcane.Mid) : Light.Mid;
				PixelInk.Cell(ward, p + new Vector2(arm * Grid, 0f), tone, cover);
				PixelInk.Cell(ward, p - new Vector2(arm * Grid, 0f), tone, cover);
				PixelInk.Cell(ward, p + new Vector2(0f, arm * Grid), tone, cover);
				PixelInk.Cell(ward, p - new Vector2(0f, arm * Grid), tone, cover);
			}
			PixelInk.Cell(ward, p, lit ? Light.Core : Arcane.Hot, cover);
		}
	}

	// The strokes each orb has flown this star, from the point it started on round to the one it last turned at.
	private void DrawStar()
	{
		if (phase != Phase.Standing)
			return;
		foreach (Orb orb in orbs)
		{
			if (orb.Leaving)
				continue;
			int v = Mod(orb.Vertex - orb.Step * (orb.Strokes + 1), StarPoints);
			for (int s = 0; s < orb.Strokes; s++)
			{
				int next = Mod(v + orb.Step, StarPoints);
				PixelInk.Line(star, PointCell(v), PointCell(next), Light.Mid);
				v = next;
			}
		}
	}

	// The stroke each orb is flying, from the point it last turned at to where it is now.
	private void DrawStroke()
	{
		strokeDrawn = false;
		if (phase != Phase.Standing)
			return;
		Vector2 origin = ward.GlobalPosition;
		foreach (Orb orb in orbs)
		{
			if (orb.Leaving)
				continue;
			Vector2 from = PointCell(Mod(orb.Vertex - orb.Step, StarPoints));
			Vector2 to = orb.At - origin;
			PixelInk.Line(stroke, from, to, Light.Edge, 3);
			PixelInk.Line(stroke, from, to, Light.Hot);
			strokeDrawn = true;
		}
	}

	// The orbs, in the world's frame (this canvas stands at the origin).
	private void DrawOrbs()
	{
		orbsDrawn = false;
		if (phase == Phase.Fading)
			return;
		// As StrokeReach grows with the ward, so does the orb.
		float size = MathF.Max(1f, Radius / Player.OrbWardRadius);
		// The core a whole art pixel up and to the left of the middle, toward the key.
		var key = new Vector2(-Grid, -Grid) * MathF.Round(size);
		foreach (Orb orb in orbs)
		{
			if (orb.Home)
				continue;
			Vector2 p = orb.At;
			PixelInk.Disc(air, p, 8f * size, Arcane.Edge);
			PixelInk.Disc(air, p, 6f * size, Light.Edge);
			PixelInk.Disc(air, p, 4f * size, Light.Hot);
			PixelInk.Disc(air, p + key, 2f * size, Light.Core);
			orbsDrawn = true;
		}
	}
}
