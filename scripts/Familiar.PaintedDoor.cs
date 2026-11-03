using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// THE PAINTED DOOR (milestone 9 on the CROW, Ma Liang's Apotheosis, verb DISPLACE: .ai/chapter-wizards.md 4.8). "Where the
// crow strikes it paints a door, and whatever stands on it steps out of a second door far across the field." Tagline:
// "Move the knot, not the kill."
//
// The boy of the tale painted a door and walked through it. Here the crow paints: where its peck lands (the stoop's
// strike; the griffin's swoop, at the first body it strikes) a door is drawn on the ground in its light - two strokes,
// then a whole door - and it stands open DoorOpenSeconds. Whatever ORDINARY body stands on its threshold (DoorRadius,
// times the spell's area) while it is open is taken through it, up to DoorCapacity a door, and steps out of a second
// door painted far across the field: ExitDistance from the wizard, on the far side of him from the knot it came out of
// and turned off that line by up to ExitTurn, on ground the swarm can stand on (Node2DGame.ReachablePoint). It is not
// a kill and deals nothing of its own: the crow's peck is the damage; the door takes the crowd that was on top of him
// and puts it where it has to walk back from, spread out. Elites, minibosses and bosses are never displaced (as every
// form that moves the crowd rules, chapter-wizards.md 6.6) - they are counted, and the door ignores them. A body taken
// through is not taken again for DisplacedCooldown, so one knot is moved once and not ping-ponged.
//
// The crow paints at most one door every DoorEverySeconds and keeps at most MaxDoors open, whatever its count and cadence
// (two crows at level 8 peck five times a second): a door is an event, not a carpet.
//
// NO MARKERS (art-direction.md section 3): the door IS the reach. Its art is tools/art/painted_door.py
// (effects/painted-door.png: drawn in two strokes, shut, ajar, open), in the arcane ramp, standing on the ground under the
// bodies (DrawLayers.GroundTell), so what steps onto it is seen to be taken. Allocation: a fixed array of doors and of
// recently moved bodies; candidates through EnemyRegistry.CollectNear's shared buffer (.ai/performance.md).
public partial class Familiar
{
	/// <summary>Ma Liang's Apotheosis on the crow (ApotheosisCatalog, VariantId familiar_crow).</summary>
	public const string PaintedDoorId = "familiar_crow_painted_door";

	/// <summary>The radius round a door's foot that it takes bodies from, before the spell's area.</summary>
	public const float DoorRadius = 54f;

	/// <summary>How long a door stands open, taking what steps onto it.</summary>
	public const float DoorOpenSeconds = 1.2f;

	/// <summary>The most bodies one door takes through.</summary>
	public const int DoorCapacity = 8;

	/// <summary>The crow paints at most one door this often.</summary>
	public const float DoorEverySeconds = 0.9f;

	/// <summary>The most doors open at once.</summary>
	public const int MaxDoors = 3;

	/// <summary>How far from the wizard the second door is painted: past the edge of a PC's view across, off a phone's.</summary>
	public const float ExitDistance = 820f;

	/// <summary>How far round (radians) from straight across the wizard the second door may turn.</summary>
	public const float ExitTurn = 1.1f;

	/// <summary>The nearest a second door may stand to the wizard, after the ground has moved it.</summary>
	public const float ExitMinDistance = 600f;

	/// <summary>Seconds a body taken through a door is not taken again.</summary>
	public const float DisplacedCooldown = 4f;

	// How the bodies stepping out are spread round the second door.
	private const float ExitScatter = 46f;
	// How often an open door looks for what is on it.
	private const float DoorScanSeconds = 0.1f;
	// The door's frames on its sheet (effects/painted-door.png): drawn half, drawn whole, shut, ajar, open.
	private const string DoorSheetPath = "res://assets/bonelight/effects/painted-door.png";
	private const int DoorFrameW = 20;
	private const int DoorFrameH = 30;
	private const int DoorFootRow = 27;
	private const int DoorMiddle = 10;
	private const float DoorDrawSeconds = 0.12f;

	/// <summary>This run's doors and what they moved, for the harness (reset by every Familiar).</summary>
	public static int DoorsPainted { get; private set; }
	public static int BodiesDisplaced { get; private set; }
	public static int StrongIgnored { get; private set; }

	private struct Door
	{
		public bool Live;
		public Vector2 At;
		public Vector2 Exit;
		public float Age;
		public float Scan;
		public int Taken;
		public float ExitOpened;   // seconds since a body last stepped out of the second door, for its frames
	}

	private readonly Door[] doors = new Door[MaxDoors];
	private bool paintedDoor;
	private float doorCooldown;
	private Node2D doorLayer;
	private Texture2D doorSheet;

	// Recently moved bodies, a ring: their instance ids and when they may be taken again.
	private const int MovedRing = 96;
	private readonly ulong[] movedIds = new ulong[MovedRing];
	private readonly float[] movedUntil = new float[MovedRing];
	private int movedNext;
	private float doorClock;

	private static void ResetDoorStats()
	{
		DoorsPainted = 0;
		BodiesDisplaced = 0;
		StrongIgnored = 0;
	}

	// Summon's half: the form switched on or off with the spell's milestones.
	private void SetPaintedDoor(SpellData spell)
	{
		paintedDoor = kind == Kind.Crow && spell.HasEvolution(PaintedDoorId);
		if (paintedDoor && doorLayer == null)
		{
			doorSheet = ResourceLoader.Exists(DoorSheetPath) ? GD.Load<Texture2D>(DoorSheetPath) : null;
			if (doorSheet == null)
				GD.PushError("Familiar: painted-door.png is missing or not imported (python tools/art/painted_door.py, then import); The Painted Door draws nothing.");
			// On the ground under the bodies: what steps onto a door is drawn over it as it is taken.
			doorLayer = new Node2D { Name = "PaintedDoors", ZAsRelative = false, ZIndex = DrawLayers.GroundTell, TextureFilter = TextureFilterEnum.Nearest };
			doorLayer.Draw += DrawDoors;
			AddChild(doorLayer);
		}
	}

	/// <summary>Where a crow's strike landed: a door is painted there, if one may be.</summary>
	private void TryPaintDoor(Vector2 strikeCentre)
	{
		if (!paintedDoor || doorCooldown > 0f || PlayerRef == null)
			return;
		int slot = -1;
		for (int i = 0; i < doors.Length; i++)
		{
			if (!doors[i].Live)
			{
				slot = i;
				break;
			}
		}
		if (slot < 0)
			return;
		Vector2 foot = strikeCentre + new Vector2(0f, GroundBelowCentre * 0.5f);
		doors[slot] = new Door { Live = true, At = foot, Exit = ExitFor(foot), Age = 0f, Scan = DoorDrawSeconds * 2f, ExitOpened = 9f };
		doorCooldown = DoorEverySeconds;
		DoorsPainted++;
		Puff(foot, inward: true);
	}

	// The second door: far across the field from the knot, on ground a body can stand on.
	private Vector2 ExitFor(Vector2 entrance)
	{
		Vector2 player = PlayerRef.GlobalPosition;
		Vector2 across = player - entrance;
		across = across.LengthSquared() > 1f ? across.Normalized() : Vector2.Right;
		Node2DGame game = Node2DGame.Current;
		Vector2 best = player + across * ExitDistance;
		for (int attempt = 0; attempt < 6; attempt++)
		{
			Vector2 dir = across.Rotated(rng.RandfRange(-ExitTurn, ExitTurn));
			Vector2 at = player + dir * ExitDistance;
			if (game != null)
				at = game.ReachablePoint(at);
			if (at.DistanceTo(player) >= ExitMinDistance)
				return at;
			best = at;
		}
		return best;
	}

	private bool RecentlyMoved(ulong id)
	{
		for (int i = 0; i < MovedRing; i++)
		{
			if (movedIds[i] == id && movedUntil[i] > doorClock)
				return true;
		}
		return false;
	}

	/// <summary>Whether a door takes <paramref name="e"/>: an ordinary body only - never an elite, a miniboss or a boss.</summary>
	public static bool DoorTakes(Enemy e) => e != null && !e.IsMiniBoss && e is not BossEnemy;

	private void TickDoors(float dt)
	{
		if (!paintedDoor)
			return;
		doorClock += dt;
		if (doorCooldown > 0f)
			doorCooldown -= dt;
		bool any = false;
		for (int i = 0; i < doors.Length; i++)
		{
			ref Door d = ref doors[i];
			if (!d.Live)
				continue;
			any = true;
			d.Age += dt;
			d.ExitOpened += dt;
			if (d.Age >= DoorOpenSeconds + DoorDrawSeconds * 2f)
			{
				d.Live = false;
				continue;
			}
			d.Scan -= dt;
			if (d.Scan > 0f || d.Taken >= DoorCapacity)
				continue;
			d.Scan = DoorScanSeconds;
			float radius = DoorRadius * areaMul;
			EnemyRegistry.CollectNear(d.At, radius, nearBuffer);
			foreach (Enemy e in nearBuffer)
			{
				if (d.Taken >= DoorCapacity)
					break;
				if (!Alive(e) || e.GlobalPosition.DistanceSquaredTo(d.At) > radius * radius)
					continue;
				if (!DoorTakes(e))
				{
					StrongIgnored++;
					continue;
				}
				ulong id = e.GetInstanceId();
				if (RecentlyMoved(id))
					continue;
				// Through the door: out of the second, spread round it, at a standstill.
				float a = rng.RandfRange(0f, Mathf.Tau);
				e.GlobalPosition = d.Exit + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rng.RandfRange(0f, ExitScatter);
				e.Velocity = Vector2.Zero;
				movedIds[movedNext] = id;
				movedUntil[movedNext] = doorClock + DisplacedCooldown;
				movedNext = (movedNext + 1) % MovedRing;
				d.Taken++;
				d.ExitOpened = 0f;
				BodiesDisplaced++;
			}
			nearBuffer.Clear();
		}
		if (any || doorLayer != null)
			doorLayer?.QueueRedraw();
	}

	private void DrawDoors()
	{
		var perfStart = FrameProfiler.Begin();
		DrawOpenDoors();
		FrameProfiler.End(FrameProfiler.Section.Familiar, perfStart);
	}

	private void DrawOpenDoors()
	{
		if (doorSheet == null)
			return;
		for (int i = 0; i < doors.Length; i++)
		{
			Door d = doors[i];
			if (!d.Live)
				continue;
			// The entrance: drawn in two strokes, shut a beat, then open for the rest of its time, and fading at the end.
			int frame = d.Age < DoorDrawSeconds ? 0 : d.Age < DoorDrawSeconds * 2f ? 1 : d.Age < DoorDrawSeconds * 2.6f ? 2
				: d.Age < DoorDrawSeconds * 3.2f ? 3 : 4;
			float end = DoorOpenSeconds + DoorDrawSeconds * 2f;
			float alpha = Mathf.Clamp((end - d.Age) / 0.25f, 0f, 1f);
			DrawDoor(d.At, frame, alpha);
			// The second door: there only while something has just stepped out of it, open, fading.
			if (d.Taken > 0)
			{
				float exitAlpha = Mathf.Min(alpha, Mathf.Clamp(1f - (d.ExitOpened - 0.5f) / 0.4f, 0f, 1f));
				if (exitAlpha > 0f)
					DrawDoor(d.Exit, d.ExitOpened < 0.08f ? 3 : 4, exitAlpha);
			}
		}
	}

	private void DrawDoor(Vector2 foot, int frame, float alpha)
	{
		var src = new Rect2(frame * DoorFrameW, 0, DoorFrameW, DoorFrameH);
		Vector2 topLeft = new Vector2(Mathf.Round(foot.X) - DoorMiddle * 2f, Mathf.Round(foot.Y) - DoorFootRow * 2f);
		doorLayer.DrawTextureRectRegion(doorSheet, new Rect2(topLeft, new Vector2(DoorFrameW * 2f, DoorFrameH * 2f)), src, new Color(1f, 1f, 1f, alpha));
	}
}
