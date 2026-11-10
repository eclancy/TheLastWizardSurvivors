using Godot;
using System;

namespace TheOneHeMissed.scripts;

// THE PAINTED DOOR (milestone 9 on INK RING, Ma Liang's Apotheosis, verb DISPLACE: .ai/chapter-wizards.md 4.8). "Closing
// a ring paints a door inside it, and whatever the ring holds steps out of a second door far across the field." Tagline:
// "Move the knot, not the kill."
//
// It was the crow Familiar's until 2026-11-10, when Ink Ring became his opener (Eric: "would it make sense for ink ring to
// be his starting spell instead?" - "Yes, add it"; .ai/character-fit.md row 21). The boy of the tale painted a door and
// walked through it; here the brush that closes the ring paints one. Where a ring closes a door is drawn on the ground at
// the deepest point inside it, in two strokes, then a whole door, shut a beat and open. Once the ring has struck - twice
// under Second Stroke, so the door never steals the second strike's crowd - it takes every ORDINARY body standing inside
// the ring, up to DoorCapacity, for as long as it stands open, and each steps out of a second door painted far across the
// field: ExitDistance from the wizard, on the far side of him from the ring and turned off that line by up to ExitTurn,
// on ground the swarm can stand on (Node2DGame.ReachablePoint). It is not a kill and deals nothing of its own: the ring's
// strike is the damage; the door takes the crowd he walked round and puts it where it has to walk back from, spread out.
// Under Ink Prison the wall holds them in for it. Elites, minibosses and bosses are never displaced (as every form that
// moves the crowd rules, chapter-wizards.md 6.6) - they are counted, and the door leaves them. A body taken through is not
// taken again for DisplacedCooldown, so a knot is moved once and not ping-ponged by the next ring.
//
// One door a ring, so the recharge between rings is the door's cadence: a door is an event, not a carpet.
//
// NO MARKERS (art-direction.md section 3): the door IS the reach - it stands inside the ring it empties. Its art is
// tools/art/painted_door.py (effects/painted-door.png: drawn in two strokes, shut, ajar, open), the door the crow painted,
// standing on the ground under the bodies (DrawLayers.GroundTell), so what steps onto it is seen to be taken. Allocation:
// fixed arrays, a door per ring slot and a ring of recently moved bodies; one walk of EnemyRegistry.Snapshot a scan.
public partial class InkRing
{
	/// <summary>Ma Liang's Apotheosis on Ink Ring (ApotheosisCatalog).</summary>
	public const string PaintedDoorId = "ink_ring_painted_door";

	/// <summary>The id the form had on the crow Familiar, which a save may hold as seen (SaveData.MovedAscensions).</summary>
	public const string CrowPaintedDoorId = "familiar_crow_painted_door";

	/// <summary>How long a door stands open, taking what stands in its ring.</summary>
	public const float DoorOpenSeconds = 1.2f;

	/// <summary>The most bodies one door takes through: "everything inside it", held to a number a ring at level 8 holds.</summary>
	public const int DoorCapacity = 40;

	/// <summary>How far from the wizard the second door is painted: past the edge of a PC's view across, off a phone's.</summary>
	public const float ExitDistance = 820f;

	/// <summary>How far round (radians) from straight across the wizard the second door may turn.</summary>
	public const float ExitTurn = 1.1f;

	/// <summary>The nearest a second door may stand to the wizard, after the ground has moved it.</summary>
	public const float ExitMinDistance = 600f;

	/// <summary>Seconds a body taken through a door is not taken again.</summary>
	public const float DisplacedCooldown = 4f;

	// How the bodies stepping out are spread round the second door: wider than the crow's, since a ring sends a crowd.
	private const float ExitScatter = 64f;
	// How often an open door looks for what is in its ring.
	private const float DoorScanSeconds = 0.1f;
	// The door's frames on its sheet (effects/painted-door.png): drawn half, drawn whole, shut, ajar, open.
	private const string DoorSheetPath = "res://assets/bonelight/effects/painted-door.png";
	private const int DoorFrameW = 20;
	private const int DoorFrameH = 30;
	private const int DoorFootRow = 27;
	private const int DoorMiddle = 10;
	private const float DoorDrawSeconds = 0.12f;

	/// <summary>Seconds after a ring closes that its door opens and starts taking: once the ring has struck.</summary>
	public static float DoorTakesAfter(bool secondStroke) =>
		secondStroke ? SecondStrokeDelay + 0.1f : DoorDrawSeconds * 2.6f;

	/// <summary>This process's doors and what they moved, for the harness's PAINTED DOOR line.</summary>
	public static int StatDoors { get; set; }
	public static int StatDisplaced { get; set; }
	public static int StatStrongIgnored { get; set; }

	private struct Door
	{
		public bool Live;
		public Vector2 At;
		public Vector2 Exit;
		public float Age;
		public float TakesAt;
		public float Scan;
		public int Taken;
		public bool Counted;      // its elites counted once
		public float ExitOpened;  // seconds since a body last stepped out of the second door, for its frames
	}

	private readonly Door[] doors = new Door[MaxRings];
	private bool doorForm;
	private Node2D doorLayer;
	private Texture2D doorSheet;
	private readonly RandomNumberGenerator doorRng = new();
	private bool doorsDrawnLastFrame;

	// Recently moved bodies, a ring: their instance ids and when they may be taken again.
	private const int MovedRing = 160;
	private readonly ulong[] movedIds = new ulong[MovedRing];
	private readonly float[] movedUntil = new float[MovedRing];
	private int movedNext;

	/// <summary>Whether a door takes <paramref name="e"/>: an ordinary body only - never an elite, a miniboss or a boss.</summary>
	public static bool DoorTakes(Enemy e) => e != null && !e.IsMiniBoss && e is not BossEnemy;

	// Refresh's half: the form switched on with the spell's milestones, and its layer made the first time.
	private void SetPaintedDoor(SpellData spell)
	{
		doorForm = spell.HasEvolution(PaintedDoorId);
		if (!doorForm || doorLayer != null)
			return;
		doorRng.Randomize();
		doorSheet = ResourceLoader.Exists(DoorSheetPath) ? GD.Load<Texture2D>(DoorSheetPath) : null;
		if (doorSheet == null)
			GD.PushError("InkRing: painted-door.png is missing or not imported (python tools/art/painted_door.py, then import); The Painted Door draws nothing.");
		// On the ground under the bodies: what stands on a door is drawn over it as it is taken.
		doorLayer = new Node2D { Name = "PaintedDoors", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.GroundTell, TextureFilter = TextureFilterEnum.Nearest };
		doorLayer.Draw += DrawDoors;
		AddChild(doorLayer);
		doorLayer.GlobalPosition = Vector2.Zero;
	}

	// A ring has closed (Close, after its flood is laid): its door, at the deepest point inside it - the flood's blot
	// furthest from every edge, which a concave ring's middle may not be.
	private void PaintDoor(int slot)
	{
		ref Ring ring = ref rings[slot];
		Vector2 foot = ring.Centre;
		int fOff = slot * MaxFlood;
		float deepest = -1f;
		for (int k = 0; k < ring.FloodCount; k++)
		{
			if (floodDelay[fOff + k] > deepest)
			{
				deepest = floodDelay[fOff + k];
				foot = floodPts[fOff + k];
			}
		}
		doors[slot] = new Door
		{
			Live = true, At = foot, Exit = ExitFor(foot), Age = 0f, TakesAt = DoorTakesAfter(secondForm), Scan = 0f, ExitOpened = 9f,
		};
		StatDoors++;
	}

	// The second door: far across the field from the ring, on ground a body can stand on.
	private Vector2 ExitFor(Vector2 entrance)
	{
		Vector2 player = PlayerRef.GlobalPosition;
		Vector2 across = player - entrance;
		across = across.LengthSquared() > 1f ? across.Normalized() : Vector2.Right;
		Node2DGame game = Node2DGame.Current;
		Vector2 best = player + across * ExitDistance;
		for (int attempt = 0; attempt < 6; attempt++)
		{
			Vector2 dir = across.Rotated(doorRng.RandfRange(-ExitTurn, ExitTurn));
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
			if (movedIds[i] == id && movedUntil[i] > clock)
				return true;
		}
		return false;
	}

	/// <summary>True while a door stands in ring slot <paramref name="slot"/>: the ring keeps its outline until it shuts.</summary>
	private bool DoorHolds(int slot) => doors[slot].Live;

	private void TickDoors(float dt)
	{
		bool any = false;
		for (int s = 0; s < doors.Length; s++)
		{
			ref Door d = ref doors[s];
			if (!d.Live)
				continue;
			any = true;
			d.Age += dt;
			d.ExitOpened += dt;
			if (d.Age >= d.TakesAt + DoorOpenSeconds)
			{
				d.Live = false;
				continue;
			}
			if (d.Age < d.TakesAt || d.Taken >= DoorCapacity || PlayerRef == null || !IsInstanceValid(PlayerRef))
				continue;
			d.Scan -= dt;
			if (d.Scan > 0f)
				continue;
			d.Scan = DoorScanSeconds;
			TakeThrough(s, ref d);
		}
		if (doorLayer != null && (any || doorsDrawnLastFrame))
			doorLayer.QueueRedraw();
		doorsDrawnLastFrame = any;
	}

	// One walk of the swarm: every ordinary body whose ground point lies in the ring goes through, up to the door's
	// capacity, and steps out of the second door spread round it, at a standstill.
	private void TakeThrough(int slot, ref Door d)
	{
		ref Ring ring = ref rings[slot];
		int off = slot * MaxRingPoints;
		var drop = new Vector2(0f, GroundDrop);
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (d.Taken >= DoorCapacity)
				break;
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			Vector2 e = enemy.GlobalPosition + drop;
			if (e.X < ring.Lo.X || e.Y < ring.Lo.Y || e.X > ring.Hi.X || e.Y > ring.Hi.Y
				|| !InkPath.Contains(ringPts, off, ring.Count, e))
				continue;
			if (!DoorTakes(enemy))
			{
				if (!d.Counted)
					StatStrongIgnored++;
				continue;
			}
			ulong id = enemy.GetInstanceId();
			if (RecentlyMoved(id))
				continue;
			float a = doorRng.RandfRange(0f, Mathf.Tau);
			enemy.GlobalPosition = d.Exit + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * doorRng.RandfRange(0f, ExitScatter);
			enemy.Velocity = Vector2.Zero;
			movedIds[movedNext] = id;
			movedUntil[movedNext] = clock + DisplacedCooldown;
			movedNext = (movedNext + 1) % MovedRing;
			d.Taken++;
			d.ExitOpened = 0f;
			StatDisplaced++;
		}
		d.Counted = true;
	}

	private void DrawDoors()
	{
		if (doorSheet == null)
			return;
		for (int i = 0; i < doors.Length; i++)
		{
			Door d = doors[i];
			if (!d.Live)
				continue;
			// The entrance: drawn in two strokes, shut until the ring has struck, ajar, then open for the rest of its time,
			// and fading at the end.
			int frame = d.Age < DoorDrawSeconds ? 0 : d.Age < DoorDrawSeconds * 2f ? 1 : d.Age < d.TakesAt ? 2
				: d.Age < d.TakesAt + DoorDrawSeconds * 0.6f ? 3 : 4;
			float end = d.TakesAt + DoorOpenSeconds;
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
