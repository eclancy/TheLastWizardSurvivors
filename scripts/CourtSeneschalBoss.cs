using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// The Court (stage_12): THE SENESCHAL (.ai/endgame-castle.md 5.6). "He leaves when it suits him.
// Follow."
//
// The steward of the house, who ran it for its lord and runs it now for him, with his white staff of
// office. Drawn by tools/art/boss_seneschal.py.
//
// THE VERB NO OTHER BOSS ASKS: PURSUE. At 75%, 50% and 25% he steps out through a servants' door -
// untargetable (SetTargetable) and taking no damage - travels through the walls (one long BurrowMove)
// to a room one or two rooms away (CourtLayout.RoomsNear), and steps out of another door there. The
// room he left fills with his household, coming in at its doors. He waits in his new room, a shade of
// himself, until the player reaches it (CourtLayout.RoomAt); then the fight resumes where he stands.
// The off-screen boss badge (Node2DGame's WorldPointer) points the way the whole time, because it
// follows the body. In the open his flight would be a teleport with a pointer; in the Court the player
// has to choose a way round, which is what the chapter has spent fifteen minutes teaching.
//
// WHEN HE IS THERE, two attacks, both from BossEnemy's primitives:
//   - THE STAFF: the slam, a Ring at his feet (GroundSlamAttack, BossEnemy's clock). Get out of it.
//   - THE RANK: he calls a line of his household guards across the room at the player - a wall that
//     walks. Its lane is marked first (TelegraphOutline, a Line the width of the rank), then the rank
//     sets off down it; the band the guards stand in is a GroundSlamAttack Line whose Contains is the
//     hit, so the wall can only touch what lies inside the lane the player was shown. Step out of the
//     lane, sideways. The guards are drawn, not spawned: they are an attack, not bodies.
//   - In the Great Hall (5.9), the chandeliers come down on the player while he fights there
//     (TelegraphedGroundHit rings).
//
// NO HIT CROSSES TWO THRESHOLDS. A hit that would carry him past the next threshold stops at it
// (AdjustIncomingDamage), and from that moment he takes nothing until he is found - so a big build
// cannot skip a flight, and a poison tick cannot bleed him while he is away.
//
// Fits BossEnemy's division of labour: the slam's clock, the phases and the victory signal are the
// base class's; what is his is the rank, the flight and the household. Read with
// .ai/enemy-behaviour.md, "The ten chapter bosses" and "Boss entrances and longer fights".
public partial class CourtSeneschalBoss : BossEnemy
{
	// --- THE RANK ------------------------------------------------------------------------------------

	/// <summary>Seconds between ranks, from the start of one to the next.</summary>
	[Export] public float RankIntervalSeconds { get; set; } = 7.5f;
	/// <summary>Seconds into the fight (and after he is found) before the first rank.</summary>
	[Export] public float RankFirstSeconds { get; set; } = 3.5f;
	/// <summary>Seconds the lane is marked before the rank sets off down it.</summary>
	[Export] public float RankTellSeconds { get; set; } = 1.1f;
	/// <summary>How fast the wall walks, world pixels a second. Quicker than the wizard (220), so it cannot be outrun down the lane.</summary>
	[Export] public float RankSpeed { get; set; } = 380f;
	/// <summary>Guards abreast; the lane is as wide as they stand.</summary>
	[Export] public int RankGuards { get; set; } = 5;
	/// <summary>World pixels between two guards' middles.</summary>
	[Export] public float RankSpacing { get; set; } = 46f;
	/// <summary>How deep the band the guards stand in is, along the lane: what the wall touches at once.</summary>
	[Export] public float RankDepth { get; set; } = 40f;
	/// <summary>How far past the player the wall walks; it never walks less than RankMinLength nor more than RankMaxLength.</summary>
	[Export] public float RankOvershoot { get; set; } = 360f;
	[Export] public float RankMinLength { get; set; } = 520f;
	[Export] public float RankMaxLength { get; set; } = 1100f;
	[Export] public int RankDamage { get; set; } = 5;
	/// <summary>He calls a rank only at a player he can see and this close.</summary>
	[Export] public float RankEngageDistance { get; set; } = 820f;

	// --- THE FLIGHT ----------------------------------------------------------------------------------

	/// <summary>Through the walls, world pixels a second.</summary>
	[Export] public float FlightSpeed { get; set; } = 560f;
	/// <summary>The travel is never shorter or longer than these, whatever the distance: a long BurrowMove.</summary>
	[Export] public float FlightMinSeconds { get; set; } = 2.4f;
	[Export] public float FlightMaxSeconds { get; set; } = 4.5f;
	/// <summary>Seconds to step back through the servants' door, and out of the one in the new room.</summary>
	[Export] public float StepOutSeconds { get; set; } = 0.9f;
	[Export] public float StepInSeconds { get; set; } = 0.8f;
	/// <summary>Rooms within this of him are where he may go; the nearest two not yet held are the choice.</summary>
	[Export] public float RefugeSearchRadius { get; set; } = 3200f;
	/// <summary>Outside his room, a player this close with a clear line to him has found him too.</summary>
	[Export] public float FoundDistance { get; set; } = 300f;
	/// <summary>On ground with no building (a Boss Rush, a tool): how far he goes, and how close is found.</summary>
	[Export] public float OpenGroundFlightDistance { get; set; } = 1100f;
	[Export] public float OpenGroundFoundDistance { get; set; } = 380f;
	/// <summary>
	/// Between flights, a player he has not been near for this long (out of sight and further than
	/// HopNearDistance) is one he comes to, by a servants' door, rather than one he walks into a wall
	/// after: a house's steward is never lost in it.
	/// </summary>
	[Export] public float HopAfterSeconds { get; set; } = 12f;
	[Export] public float HopNearDistance { get; set; } = 760f;
	/// <summary>How far from the player he comes out of a door he takes to reach them.</summary>
	[Export] public float HopArriveDistance { get; set; } = 380f;

	// --- THE HOUSEHOLD -------------------------------------------------------------------------------

	/// <summary>Bodies that come into the room he left on his first flight; each flight brings HouseholdStep more.</summary>
	[Export] public int HouseholdFirst { get; set; } = 8;
	[Export] public int HouseholdStep { get; set; } = 3;
	/// <summary>On the run's frozen spawn health, as a side event's bodies are (Node2DGame.SpawnEventEnemy).</summary>
	[Export] public float HouseholdHealthMultiplier { get; set; } = 1f;
	[Export] public float HouseholdStaggerSeconds { get; set; } = 0.2f;
	/// <summary>No one comes in at a door this close to the player.</summary>
	[Export] public float HouseholdMinPlayerDistance { get; set; } = 280f;

	// --- THE CHANDELIERS (5.9) -----------------------------------------------------------------------

	[Export] public float ChandelierIntervalSeconds { get; set; } = 3.4f;
	[Export] public float ChandelierTellSeconds { get; set; } = 1.2f;
	[Export] public float ChandelierRadius { get; set; } = 78f;
	[Export] public int ChandelierDamage { get; set; } = 4;

	/// <summary>Where his household guards' sheet is: the rank is drawn from it.</summary>
	public const string GuardFramesPath = "res://scenes/resources/CourtGuardFrames.tres";

	// The flights. Even quarters: each part of the fight is long enough to be learned before he leaves.
	private static readonly float[] FlightThresholds = { 0.75f, 0.5f, 0.25f };

	// Who comes into the room he left, in turn - the table's bodies and the charger, never a turret or
	// a summoner: his household arrives to stand in the way, not to make a second fight. Health on each
	// as the Court's table carries it (StageBestiary.Court: heavy 2.2, bulk 1, fast 0.75).
	private static readonly (BestiaryRole Role, float Health)[] Household =
	{
		(BestiaryRole.Bulk, 1f), (BestiaryRole.Fast, 0.75f), (BestiaryRole.Bulk, 1f), (BestiaryRole.Charger, 1.4f),
		(BestiaryRole.Heavy, 2.2f), (BestiaryRole.Bulk, 1f), (BestiaryRole.Fast, 0.75f), (BestiaryRole.Bulk, 1f),
		(BestiaryRole.Charger, 1.4f), (BestiaryRole.Heavy, 2.2f), (BestiaryRole.Fast, 0.75f), (BestiaryRole.Bulk, 1f),
		(BestiaryRole.Bulk, 1f), (BestiaryRole.Fast, 0.75f), (BestiaryRole.Heavy, 2.2f), (BestiaryRole.Charger, 1.4f),
	};

	// His staff's cold light: the ring. The rank's lane in steel, the household's own colour. The
	// chandeliers carry his fire, as every made thing in his house does.
	private static readonly Color StaffColor = ElementColors.GetRamp(Element.Arcane).Mid;
	private static readonly Color RankColor = new(0.78f, 0.86f, 0.98f);
	private static readonly Color ChandelierColor = new(1.0f, 0.62f, 0.30f);
	// While he waits to be found: a shade of himself, so a body the spells will not touch reads as one.
	private static readonly Color WaitingLook = new(0.66f, 0.72f, 0.92f, 0.55f);

	private enum Stance { Fighting, SteppingOut, Away, SteppingIn, Waiting }

	private Stance stance = Stance.Fighting;
	private float stanceClock;
	private BurrowMove passage;
	private bool untouchable;
	private int flights;
	private int pendingFlights;
	private float untilRank;
	private float rankIntervalNow;
	private int rankGuardsNow;
	private float untilChandelier;
	private Vector2 stepFrom;
	private CourtLayout.Room room;
	private CourtLayout.Room refuge;
	private Vector2 refugePoint;
	private string refugeName = "";
	private readonly HashSet<string> roomsHeld = new();
	private readonly List<Rank> ranks = new();
	private readonly List<Door> doors = new();
	private readonly List<Arrival> arrivals = new();
	private readonly RandomNumberGenerator rng = new();
	private SpriteFrames guardFrames;
	private int householdTurn;
	// A door taken to the player between flights (Hop), not a flight: no household, no waiting.
	private bool hopping;
	private float awayFromPlayer;

	// --- What the tools read (_BossProbe, _BalanceHarness, _UiShot) ----------------------------------

	/// <summary>Flights taken so far (0 to 3).</summary>
	public int FlightsTaken => flights;

	/// <summary>True from the moment he steps out until he is found: the player's job is to go to him.</summary>
	public bool AwaitingPursuit => stance != Stance.Fighting && !hopping;

	/// <summary>Doors taken to a player he could not reach (Hop), as opposed to flights.</summary>
	public int HopsTaken { get; private set; }

	/// <summary>Where to go to find him: where he will come out while he travels, where he stands after.</summary>
	public Vector2 PursuitTarget => stance == Stance.Away ? passage.Destination : (stance == Stance.SteppingOut ? refugePoint : GlobalPosition);

	/// <summary>The room he went to, or "" on open ground.</summary>
	public string RefugeName => refugeName;

	/// <summary>The room he stands in now, or "" in a hall or on open ground. For photographs.</summary>
	public string RoomName => room?.Name ?? "";

	/// <summary>Bodies of his household brought in so far.</summary>
	public int HouseholdCalled { get; private set; }

	/// <summary>Ranks called so far, and how many reached the player.</summary>
	public int RanksCalled { get; private set; }
	public int RankHits { get; private set; }

	public override void _Ready()
	{
		base._Ready();
		rng.Randomize();
		passage = new BurrowMove(FlightMinSeconds);
		rankIntervalNow = RankIntervalSeconds;
		rankGuardsNow = Math.Max(1, RankGuards);
		untilRank = RankFirstSeconds;
		untilChandelier = ChandelierIntervalSeconds;
		guardFrames = ResourceLoader.Exists(GuardFramesPath) ? GD.Load<SpriteFrames>(GuardFramesPath) : null;
		if (guardFrames == null)
			GD.PushError($"CourtSeneschalBoss: {GuardFramesPath} is missing, so his rank walks unseen (the lane still marks it).");
		room = CurrentLayout()?.RoomAt(GlobalPosition);
		if (room != null)
			roomsHeld.Add(room.Id);
	}

	protected override GroundSlamAttack BuildSlam() =>
		new(SlamIntervalSeconds, SlamTelegraphSeconds, SlamRadius, SlamDamage, StaffColor);

	protected override float[] BuildPhaseThresholds() => (float[])FlightThresholds.Clone();

	// Every threshold is a flight; what changes in the fight is applied when he is found (Resume).
	protected override void OnPhaseEntered(int phase)
	{
		if (stance != Stance.Fighting)
		{
			pendingFlights++;
			return;
		}
		BeginFlight();
	}

	// --- Damage: no threshold skipped, nothing taken while away -----------------------------------

	protected override int AdjustIncomingDamage(int amount)
	{
		if (amount <= 0)
			return amount;
		// A tool's kill (_UiShot's --boss-death, Node2DGame.DebugAdvanceBossRush) is many times his whole
		// health; no hit in play is. It must still fell him, wherever he is.
		if (amount >= MaxHealth)
			return amount;
		if (untouchable || stance != Stance.Fighting || IsEntering)
			return 0;
		int phase = BossPhase;
		if (phase < FlightThresholds.Length)
		{
			int floor = Mathf.FloorToInt(FlightThresholds[phase] * MaxHealth);
			if (Health - amount <= floor)
			{
				// It stops at the threshold, and that is the last of him this room gets.
				untouchable = true;
				return Math.Max(0, Health - floor);
			}
		}
		return amount;
	}

	// --- The fight -----------------------------------------------------------------------------------

	private bool MarkingRank => ranks.Count > 0 && ranks[^1].IsMarking;

	protected override bool WantsToAttack(Node2D target, float distanceToPlayer) =>
		stance == Stance.Fighting && !MarkingRank && distanceToPlayer <= SlamRadius * SlamEngageRadiusMultiplier;

	// A wind-up begun just before a threshold still runs out; it must not land from wherever he is now.
	protected override void OnAttackResolved(Node2D target)
	{
		if (stance == Stance.Fighting)
			base.OnAttackResolved(target);
	}

	protected override Vector2 AdjustSteering(Vector2 chaseDirection, float distanceToPlayer)
	{
		if (stance != Stance.Fighting || MarkingRank)
			return Vector2.Zero;
		return base.AdjustSteering(chaseDirection, distanceToPlayer);
	}

	protected override void DrawGroundTells(CanvasItem floor)
	{
		if (stance == Stance.Fighting)
			base.DrawGroundTells(floor);
	}

	// Over his head: the slam's arrow, or - while a rank's lane is marked - the summons.
	protected override bool ReadAttackTell(out TelegraphGlyph glyph, out float progress)
	{
		if (stance != Stance.Fighting)
		{
			glyph = TelegraphGlyph.Slam;
			progress = 0f;
			return false;
		}
		if (MarkingRank)
		{
			glyph = TelegraphGlyph.Summon;
			progress = ranks[^1].MarkProgress;
			return true;
		}
		return base.ReadAttackTell(out glyph, out progress);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		base._PhysicsProcess(delta);
		if (IsDying)
			return;

		switch (stance)
		{
			case Stance.Fighting:
				TickFight(dt);
				break;
			case Stance.SteppingOut:
				TickSteppingOut(dt);
				break;
			case Stance.Away:
				TickAway(dt);
				break;
			case Stance.SteppingIn:
				TickSteppingIn(dt);
				break;
			case Stance.Waiting:
				TickWaiting(dt);
				break;
		}

		TickRanks(dt);
		TickDoors(dt);
		TickArrivals(dt);
	}

	private void TickFight(float dt)
	{
		// Null in a hall: the room he leaves is the one he is in when he goes, if any.
		room = CurrentLayout()?.RoomAt(GlobalPosition);
		Node2D target = TargetPlayer;
		if (target == null || !IsInstanceValid(target))
			return;

		untilRank -= dt;
		if (untilRank <= 0f && !Slam.IsWindingUp && !MarkingRank)
		{
			float distance = GlobalPosition.DistanceTo(target.GlobalPosition);
			if (distance <= RankEngageDistance && CanSee(target.GlobalPosition))
			{
				CallRank(target.GlobalPosition);
				untilRank = rankIntervalNow;
			}
			else
				untilRank = 0.4f;
		}

		TickChandeliers(dt, target);
		TickHop(dt, target);
	}

	// Between flights he comes to a player he has lost - one who has stood out of his sight and his
	// reach for HopAfterSeconds, kiting far down a hall or somewhere his walk round the walls does not
	// find (the flow field's horizon is 64 tiles, and a boss is never recycled to the spawn ring as
	// an ordinary body is). Not while an attack is winding up: a tell is always finished.
	private void TickHop(float dt, Node2D target)
	{
		float distance = GlobalPosition.DistanceTo(target.GlobalPosition);
		bool near = distance <= HopNearDistance * 0.6f || (distance <= HopNearDistance && CanSee(target.GlobalPosition));
		awayFromPlayer = near ? 0f : awayFromPlayer + dt;
		if (awayFromPlayer < HopAfterSeconds || Slam.IsWindingUp || MarkingRank)
			return;
		awayFromPlayer = 0f;
		BeginHop(target);
	}

	private void BeginHop(Node2D target)
	{
		hopping = true;
		HopsTaken++;
		stance = Stance.SteppingOut;
		stanceClock = 0f;
		stepFrom = GlobalPosition;
		SetTargetable(false);
		if (BodySprite != null)
			BodySprite.Visible = true;
		Velocity = Vector2.Zero;
		// Out of a door on his side of the player, in sight of him, at a step's distance.
		Vector2 toHim = GlobalPosition - target.GlobalPosition;
		Vector2 side = toHim.LengthSquared() > 1f ? toHim.Normalized() : Vector2.Up;
		refugePoint = target.GlobalPosition + side * HopArriveDistance;
		for (int k = 0; k < 8; k++)
		{
			Vector2 candidate = Node2DGame.Current?.ReachablePoint(target.GlobalPosition + side.Rotated(k * Mathf.Tau / 8f) * HopArriveDistance)
				?? target.GlobalPosition;
			MazeNavigation nav = MazeNavigation.Routing;
			if (nav == null || nav.HasLineOfSight(target.GlobalPosition, candidate))
			{
				refugePoint = candidate;
				break;
			}
		}
		refuge = CurrentLayout()?.RoomAt(refugePoint);
		passage.TravelSeconds = Mathf.Clamp(GlobalPosition.DistanceTo(refugePoint) / Mathf.Max(60f, FlightSpeed), 1.2f, 3f);
		OpenDoor(GlobalPosition, true);
		GD.Print($"Seneschal: comes to the player by a servants' door ({GlobalPosition.DistanceTo(refugePoint):0} px), lost for {HopAfterSeconds:0} s");
	}

	private bool CanSee(Vector2 point)
	{
		MazeNavigation nav = MazeNavigation.Routing;
		return nav == null || nav.HasLineOfSight(GlobalPosition, point);
	}

	// --- The rank: a wall that walks ------------------------------------------------------------------

	private void CallRank(Vector2 at)
	{
		Node arena = GetParent();
		if (arena == null)
			return;
		Vector2 from = GlobalPosition;
		Vector2 toPlayer = at - from;
		Vector2 along = toPlayer.LengthSquared() > 1f ? toPlayer.Normalized() : Vector2.Right;
		float length = Mathf.Clamp(toPlayer.Length() + RankOvershoot, RankMinLength, RankMaxLength);
		// Across the room, never through its wall: the lane stops where the floor does.
		length = ClipToFloor(from, along, length);
		if (length < RankDepth * 2f)
			return;

		float halfWidth = (rankGuardsNow - 1) * 0.5f * RankSpacing + RankSpacing * 0.5f;
		var rank = new Rank(this, arena, from, along, length, halfWidth, guardFrames, rankGuardsNow);
		ranks.Add(rank);
		RanksCalled++;
		PlayAttackAnimation();
	}

	private float ClipToFloor(Vector2 from, Vector2 along, float length)
	{
		TerrainStreamer ground = Node2DGame.Current?.Terrain;
		if (ground == null)
			return length;
		for (float d = 24f; d <= length; d += 24f)
		{
			Vector2 p = from + along * d;
			if (ground.IsWallAtWorld(p) || ground.IsBlockedAtWorld(p))
				return Mathf.Max(0f, d - 24f);
		}
		return length;
	}

	private void TickRanks(float dt)
	{
		for (int i = ranks.Count - 1; i >= 0; i--)
		{
			if (!ranks[i].Tick(dt))
			{
				ranks[i].Free();
				ranks.RemoveAt(i);
			}
		}
	}

	// Called by a rank whose band has the player in it, once.
	private void RankReached(Node2D target)
	{
		int dealt = ScaleOutgoingDamage(RankDamage);
		if (dealt <= 0 || target == null || !IsInstanceValid(target) || !target.HasMethod("TakeDamage"))
			return;
		target.Call("TakeDamage", dealt);
		RankHits++;
	}

	/// <summary>
	/// One rank of the household called across the room: its lane marked, then the guards walking it.
	/// A plain object the boss ticks, with two nodes in the arena (the lane on the floor and the guards
	/// among the bodies), as TelegraphedGroundHit is a node: a rank outlives the frame its caster moves.
	/// </summary>
	private sealed class Rank
	{
		public readonly CourtSeneschalBoss Caster;
		private readonly Vector2 origin;
		private readonly Vector2 along;
		private readonly float length;
		private readonly float halfWidth;
		private readonly float tell;
		private readonly float march;
		private readonly Node2D lane;
		private readonly AnimatedSprite2D[] guards;
		private readonly GroundSlamAttack band;
		private float age;
		private bool reached;

		public Rank(CourtSeneschalBoss caster, Node arena, Vector2 origin, Vector2 along, float length, float halfWidth,
			SpriteFrames frames, int count)
		{
			Caster = caster;
			this.origin = origin;
			this.along = along;
			this.length = length;
			this.halfWidth = halfWidth;
			tell = Mathf.Max(0.2f, caster.RankTellSeconds);
			march = length / Mathf.Max(60f, caster.RankSpeed);
			// The band the guards stand in: the only ground the wall can touch, inside the lane drawn.
			band = new GroundSlamAttack(1f, 1f, caster.RankDepth, caster.RankDamage, RankColor)
			{
				Shape = TelegraphShape.Line,
				HalfWidth = halfWidth,
				Facing = along,
			};

			lane = new Node2D { Name = "SeneschalRankLane", ZAsRelative = false, ZIndex = DrawLayers.GroundTell };
			arena.AddChild(lane);
			lane.GlobalPosition = origin;
			lane.Draw += PaintLane;

			guards = new AnimatedSprite2D[frames != null ? count : 0];
			Vector2 across = new(-along.Y, along.X);
			for (int i = 0; i < guards.Length; i++)
			{
				var guard = new AnimatedSprite2D
				{
					Name = "SeneschalRankGuard",
					SpriteFrames = frames,
					Scale = new Vector2(2f, 2f),
					FlipH = along.X < 0f,
					Visible = false,
				};
				arena.AddChild(guard);
				guard.Play("moving");
				// A step out of time with each other, so the wall reads as men and not as a stamp.
				guard.Frame = i % 8;
				guards[i] = guard;
			}
			PlaceGuards(0f, across);
		}

		public bool IsMarking => age < tell;
		public float MarkProgress => Mathf.Clamp(age / tell, 0f, 1f);

		// Where the band's front is, along the lane.
		private float Front => Mathf.Clamp((age - tell) * Caster.RankSpeed, 0f, length);

		/// <summary>Advances the rank; false once it has walked its lane and faded.</summary>
		public bool Tick(float dt)
		{
			age += dt;
			if (GodotObject.IsInstanceValid(lane))
			{
				// Drawn round the node's own origin, so the node walks with the wall's back edge.
				lane.GlobalPosition = origin + along * Mathf.Max(0f, Front - Caster.RankDepth);
				lane.QueueRedraw();
			}
			float marched = age - tell;
			if (marched < 0f)
				return true;

			float front = Front;
			Vector2 across = new(-along.Y, along.X);
			PlaceGuards(front, across);
			float fade = 1f - Mathf.Clamp((marched - march) / 0.3f, 0f, 1f);
			float appear = Mathf.Clamp(marched / 0.18f, 0f, 1f);
			foreach (AnimatedSprite2D guard in guards)
			{
				if (!GodotObject.IsInstanceValid(guard))
					continue;
				guard.Visible = true;
				// Darker than the household that fights: they are called, an attack, not bodies to hit.
				guard.Modulate = new Color(0.62f, 0.68f, 0.84f, 0.92f * appear * fade);
			}

			if (!reached && marched <= march && Caster.TargetPlayer is Node2D target && GodotObject.IsInstanceValid(target))
			{
				Vector2 bandOrigin = origin + along * Mathf.Max(0f, front - Caster.RankDepth);
				if (band.Contains(bandOrigin, target.GlobalPosition))
				{
					reached = true;
					Caster.RankReached(target);
				}
			}
			return marched < march + 0.3f;
		}

		private void PlaceGuards(float front, Vector2 across)
		{
			int n = guards.Length;
			for (int i = 0; i < n; i++)
			{
				if (!GodotObject.IsInstanceValid(guards[i]))
					continue;
				float side = (i - (n - 1) * 0.5f) * Caster.RankSpacing;
				// The middle guard a half step ahead: a rank walking, not a ruler.
				float step = (i % 2 == 0 ? 0f : -6f);
				guards[i].GlobalPosition = origin + along * (front - Caster.RankDepth * 0.5f + step) + across * side;
			}
		}

		// The lane still to be walked: from the band's back edge to its end. Marked in full first, the
		// outline blinking as it sets off, then steady while the wall walks it.
		private void PaintLane()
		{
			if (!GodotObject.IsInstanceValid(lane))
				return;
			float marched = age - tell;
			if (marched > march)
				return;
			float back = Mathf.Max(0f, Front - Caster.RankDepth);
			float remaining = length - back;
			if (remaining <= 2f)
				return;
			float progress = marched < 0f ? MarkProgress : 0.5f;
			TelegraphOutline.Warning(lane, TelegraphShape.Line, remaining, 0f, halfWidth, along, RankColor, progress);
		}

		public void Free()
		{
			if (GodotObject.IsInstanceValid(lane))
				lane.QueueFree();
			foreach (AnimatedSprite2D guard in guards)
				if (GodotObject.IsInstanceValid(guard))
					guard.QueueFree();
		}
	}

	// --- The chandeliers (5.9) ------------------------------------------------------------------------

	private void TickChandeliers(float dt, Node2D target)
	{
		CourtLayout layout = CurrentLayout();
		if (layout == null || room == null || room.Kind != "great_hall")
			return;
		Vector2 feet = target is Player wizard ? wizard.FootPosition : target.GlobalPosition;
		if (layout.RoomAt(feet) != room)
			return;
		untilChandelier -= dt;
		if (untilChandelier > 0f)
			return;
		untilChandelier = ChandelierIntervalSeconds;
		// Near him, not on him: a ring dropped exactly where he stands would be a hit he could only
		// avoid by never standing still, which is the rank's question, not this one's.
		float angle = rng.Randf() * Mathf.Tau;
		Vector2 at = target.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * rng.RandfRange(30f, 110f);
		if (layout.RoomAt(at) != room)
			at = target.GlobalPosition;
		TelegraphedGroundHit.Place(GetParent(), at, ChandelierTellSeconds, ChandelierRadius,
			ScaleOutgoingDamage(ChandelierDamage), ChandelierColor, owner: this);
	}

	// --- The flight ----------------------------------------------------------------------------------

	private static CourtLayout CurrentLayout() => (Node2DGame.Current?.Rules as CourtRules)?.Layout;

	private void BeginFlight()
	{
		flights++;
		untouchable = true;
		stance = Stance.SteppingOut;
		stanceClock = 0f;
		stepFrom = GlobalPosition;
		SetTargetable(false);
		// Untargetable hides the body; he is still here, stepping back through his door.
		if (BodySprite != null)
			BodySprite.Visible = true;
		Velocity = Vector2.Zero;

		CourtLayout.Room left = room ?? CurrentLayout()?.RoomAt(GlobalPosition);
		PickRefuge(left);
		OpenDoor(GlobalPosition, true);
		FillTheRoomHeLeft(left, HouseholdFirst + HouseholdStep * (flights - 1));
		// The house answers him: the wave's warning, not the roar, which is kept for when he is found.
		SfxPlayer.Global(SfxCatalog.WaveWarning);
		GD.Print($"Seneschal: flight {flights} from {left?.Name ?? "the open"} to {(refugeName.Length > 0 ? refugeName : "the open")} "
			+ $"({GlobalPosition.DistanceTo(refugePoint):0} px) at {BossHealthFraction:P0}");
	}

	// One or two rooms away: of the rooms near him that he has not yet held, not the one he is in nor
	// the one the player is in, the nearest two, one of them at random. Never the hidden Solar, the
	// Keepsake Doors' corridor (its alcoves are shut) or the balcony reached by the back stair.
	private void PickRefuge(CourtLayout.Room left)
	{
		refuge = null;
		refugeName = "";
		CourtLayout layout = CurrentLayout();
		Vector2 from = GlobalPosition;
		Player wizard = TargetPlayer as Player;
		if (layout != null)
		{
			CourtLayout.Room playerRoom = wizard != null ? layout.RoomAt(wizard.FootPosition) : null;
			var choice = new List<CourtLayout.Room>();
			for (int pass = 0; pass < 2 && choice.Count == 0; pass++)
			{
				foreach (CourtLayout.Room r in layout.RoomsNear(from, RefugeSearchRadius))
				{
					if (r == left || r == playerRoom || r.Kind is "keepsake" or "minstrels")
						continue;
					if (pass == 0 && roomsHeld.Contains(r.Id))
						continue;
					choice.Add(r);
					if (choice.Count == 2)
						break;
				}
			}
			if (choice.Count > 0)
			{
				refuge = choice[rng.RandiRange(0, choice.Count - 1)];
				refugeName = refuge.Name;
				roomsHeld.Add(refuge.Id);
				refugePoint = PointIn(refuge, from);
				passage.TravelSeconds = Mathf.Clamp(from.DistanceTo(refugePoint) / Mathf.Max(60f, FlightSpeed), FlightMinSeconds, FlightMaxSeconds);
				return;
			}
		}

		// No building (or no room left near): away from the player, a long walk off.
		Vector2 away = wizard != null ? (from - wizard.GlobalPosition) : Vector2.Right;
		float angle = (away.LengthSquared() > 1f ? away.Angle() : 0f) + rng.RandfRange(-1f, 1f);
		Vector2 wanted = from + Vector2.FromAngle(angle) * OpenGroundFlightDistance;
		refugePoint = Node2DGame.Current?.ReachablePoint(wanted) ?? wanted;
		passage.TravelSeconds = Mathf.Clamp(from.DistanceTo(refugePoint) / Mathf.Max(60f, FlightSpeed), FlightMinSeconds, FlightMaxSeconds);
	}

	// Inside the room on the side he comes from, well in from its walls: across a wall from where he
	// left, which is a short line and a long walk.
	private static Vector2 PointIn(CourtLayout.Room r, Vector2 from)
	{
		Rect2 floor = r.World;
		float insetX = Mathf.Min(220f, floor.Size.X * 0.3f);
		float insetY = Mathf.Min(220f, floor.Size.Y * 0.3f);
		Rect2 inner = new(floor.Position + new Vector2(insetX, insetY), floor.Size - new Vector2(insetX, insetY) * 2f);
		var nearest = new Vector2(Mathf.Clamp(from.X, inner.Position.X, inner.End.X), Mathf.Clamp(from.Y, inner.Position.Y, inner.End.Y));
		Vector2 point = nearest.Lerp(r.Centre, 0.35f);
		return Node2DGame.Current?.ReachablePoint(point) ?? point;
	}

	private void TickSteppingOut(float dt)
	{
		stanceClock += dt;
		// He holds while the door opens behind him, then steps back into the dark of it.
		float back = Mathf.Clamp((stanceClock - 0.35f) / Mathf.Max(0.05f, StepOutSeconds - 0.4f), 0f, 1f);
		GlobalPosition = stepFrom + new Vector2(0f, -14f * back);
		float lit = 1f - back * 0.8f;
		Modulate = new Color(lit, lit, lit * 1.05f, 1f - back);
		if (stanceClock < StepOutSeconds)
			return;
		stance = Stance.Away;
		stanceClock = 0f;
		if (BodySprite != null)
			BodySprite.Visible = false;
		Modulate = new Color(1f, 1f, 1f, 0f);
		passage.Begin(GlobalPosition, refugePoint);
	}

	private void TickAway(float dt)
	{
		switch (passage.Tick(dt))
		{
			case BurrowMove.Beat.Travelling:
				GlobalPosition = passage.CurrentPosition;
				break;
			case BurrowMove.Beat.Surfaced:
				GlobalPosition = passage.Destination;
				stance = Stance.SteppingIn;
				stanceClock = 0f;
				room = refuge ?? CurrentLayout()?.RoomAt(GlobalPosition);
				OpenDoor(GlobalPosition + new Vector2(0f, -14f), false);
				if (BodySprite != null)
					BodySprite.Visible = true;
				break;
		}
	}

	private void TickSteppingIn(float dt)
	{
		stanceClock += dt;
		float step = Mathf.Clamp((stanceClock - 0.3f) / Mathf.Max(0.05f, StepInSeconds - 0.35f), 0f, 1f);
		GlobalPosition = passage.Destination + new Vector2(0f, -14f * (1f - step));
		Color dark = new(0.2f, 0.2f, 0.26f, 0f);
		Modulate = dark.Lerp(hopping ? Colors.White : WaitingLook, step);
		if (stanceClock < StepInSeconds)
			return;
		GlobalPosition = passage.Destination;
		if (hopping)
		{
			Rejoin();
			return;
		}
		stance = Stance.Waiting;
		stanceClock = 0f;
		Modulate = WaitingLook;
	}

	private void TickWaiting(float dt)
	{
		stanceClock += dt;
		if (stanceClock < 0.3f || TargetPlayer is not Player wizard || !IsInstanceValid(wizard) || wizard.IsDead)
			return;
		if (IsFound(wizard))
			Resume();
	}

	// "Reaching his room resumes the fight": standing in it, or - outside it, or on open ground - close
	// to him with nothing between.
	private bool IsFound(Player wizard)
	{
		CourtLayout layout = CurrentLayout();
		if (layout != null && refuge != null)
		{
			if (layout.RoomAt(wizard.FootPosition) == refuge)
				return true;
			return wizard.GlobalPosition.DistanceTo(GlobalPosition) <= FoundDistance && CanSee(wizard.GlobalPosition);
		}
		return wizard.GlobalPosition.DistanceTo(GlobalPosition) <= OpenGroundFoundDistance;
	}

	// Back in the fight after a hop: nothing the flights change is touched.
	private void Rejoin()
	{
		hopping = false;
		stance = Stance.Fighting;
		stanceClock = 0f;
		Modulate = Colors.White;
		awayFromPlayer = 0f;
		SetTargetable(true);
		untilRank = Mathf.Min(untilRank, 1.5f);
		if (pendingFlights > 0)
		{
			pendingFlights--;
			BeginFlight();
		}
	}

	private void Resume()
	{
		stance = Stance.Fighting;
		stanceClock = 0f;
		Modulate = Colors.White;
		untouchable = false;
		awayFromPlayer = 0f;
		SetTargetable(true);
		GD.Print($"Seneschal: found in {(refugeName.Length > 0 ? refugeName : "the open")} after flight {flights}");

		// What the flight changed. The wall comes oftener and wider, and at the last he loses the
		// composure of his office (BossEnemy.ApplyEnrage: quicker on his feet and with the staff).
		if (flights >= 1)
			rankIntervalNow = RankIntervalSeconds * (flights == 1 ? 0.85f : 0.72f);
		if (flights >= 2)
			rankGuardsNow = Math.Max(1, RankGuards) + 2;
		if (flights >= 3)
			ApplyEnrage();
		// He greets the player with the wall, almost at once.
		untilRank = Mathf.Min(untilRank, 1.2f);
		SfxPlayer.AtPosition(SfxCatalog.BossRoar, GlobalPosition, 0.0f);

		if (pendingFlights > 0)
		{
			pendingFlights--;
			BeginFlight();
		}
	}

	// --- The servants' doors ---------------------------------------------------------------------------

	// The door is drawn on the floor layer, under every body, so he stands in front of it; its foot is
	// at his feet (the node is the middle of a 192 px body, 92 px above them).
	private const float FootBelowCentre = 92f;

	private void OpenDoor(Vector2 bodyCentre, bool exit)
	{
		Node arena = GetParent();
		if (arena == null)
			return;
		var node = new Node2D { Name = exit ? "SeneschalDoorOut" : "SeneschalDoorIn", ZAsRelative = false, ZIndex = DrawLayers.GroundTell - 1 };
		arena.AddChild(node);
		node.GlobalPosition = bodyCentre + new Vector2(0f, FootBelowCentre);
		var door = new Door { Node = node, Exit = exit };
		node.Draw += () => PaintDoorAt(door);
		doors.Add(door);
	}

	private sealed class Door
	{
		public Node2D Node;
		public bool Exit;
		public float Age;
	}

	private const float DoorSeconds = 1.5f;

	private void TickDoors(float dt)
	{
		for (int i = doors.Count - 1; i >= 0; i--)
		{
			Door door = doors[i];
			door.Age += dt;
			if (!IsInstanceValid(door.Node))
			{
				doors.RemoveAt(i);
				continue;
			}
			door.Node.QueueRedraw();
			if (door.Age >= DoorSeconds)
			{
				door.Node.QueueFree();
				doors.RemoveAt(i);
			}
		}
	}

	private static void PaintDoorAt(Door door)
	{
		float t = door.Age;
		// In: fade up shut, swing open, hold while he goes through, swing shut, fade.
		float alpha = BossDeath.Span(t, 0f, 0.15f) * (1f - BossDeath.Span(t, DoorSeconds - 0.4f, DoorSeconds));
		float open = BossDeath.EaseOut(BossDeath.Span(t, 0.12f, 0.38f)) * (1f - BossDeath.EaseIn(BossDeath.Span(t, 0.95f, 1.15f)));
		PaintDoor(door.Node, Vector2.Zero, open, alpha);
	}

	// Palette: the Court's oak (gold's lower stops: .ai/endgame-castle.md 5.1, oak boards), iron, occlusion.
	private static readonly Color OakDeep = new(0.180f, 0.133f, 0.031f);
	private static readonly Color OakShade = new(0.329f, 0.247f, 0.063f);
	private static readonly Color OakLit = new(0.541f, 0.416f, 0.118f);
	private static readonly Color DoorDark = new(0.020f, 0.027f, 0.047f);
	private static readonly Color Sill = new(0.165f, 0.204f, 0.267f);
	private static readonly Color IronLit = new(0.416f, 0.494f, 0.596f);

	/// <summary>
	/// A servants' door - a pair of panelled oak leaves in a frame - standing on <paramref name="foot"/>,
	/// <paramref name="open"/> 0 shut to 1 wide, on the 2 px art grid. Shared by his arrival (BossDeath's
	/// ground layer) and his flights (a node of the arena's floor).
	/// </summary>
	internal static void PaintDoor(CanvasItem canvas, Vector2 foot, float open, float alpha)
	{
		if (alpha <= 0.01f)
			return;
		const float px = BossDeath.Px;
		const int halfW = 36, high = 102, jamb = 4, lintel = 5;   // in art pixels
		Vector2 o = BossDeath.Snap(foot);
		Rect2 Art(int x0, int y0, int w, int h) => new(o + new Vector2(x0 * px, -(y0 + h) * px), new Vector2(w * px, h * px));
		Color A(Color c, float k = 1f) => new(c, c.A * alpha * k);

		// The sill, and the dark of the doorway behind the leaves.
		canvas.DrawRect(Art(-halfW - 1, -1, halfW * 2 + 2, 2), A(Sill));
		canvas.DrawRect(Art(-halfW + jamb, 1, (halfW - jamb) * 2, high - lintel - 1), A(DoorDark));
		// The frame: two jambs and a lintel, lit along their key edges.
		canvas.DrawRect(Art(-halfW, 0, jamb, high), A(OakShade));
		canvas.DrawRect(Art(halfW - jamb, 0, jamb, high), A(OakDeep));
		canvas.DrawRect(Art(-halfW, high - lintel, halfW * 2, lintel), A(OakShade));
		canvas.DrawRect(Art(-halfW, high - 1, halfW * 2, 1), A(OakLit));
		canvas.DrawRect(Art(-halfW, 0, 1, high), A(OakLit));
		canvas.DrawRect(Art(-halfW + jamb, high - lintel, (halfW - jamb) * 2, 1), A(DoorDark, 0.8f));

		// The two leaves, each hinged at its jamb and turned in toward the dark as it opens.
		int inner = halfW - jamb;
		int leafW = Mathf.Max(1, Mathf.RoundToInt(inner * Mathf.Cos(open * Mathf.Pi * 0.44f)));
		int leafH = high - lintel - 1;
		for (int side = -1; side <= 1; side += 2)
		{
			int x0 = side < 0 ? -inner : inner - leafW;
			canvas.DrawRect(Art(x0, 1, leafW, leafH), A(side < 0 ? OakShade : OakDeep));
			if (leafW >= 8)
			{
				// Two panels to a leaf: inset dark, lit along their top and key edges.
				int pw = leafW - 6;
				for (int panel = 0; panel < 2; panel++)
				{
					int py = panel == 0 ? leafH - 44 : 6;
					int ph = panel == 0 ? 38 : 46;
					canvas.DrawRect(Art(x0 + 3, py, pw, ph), A(OakDeep));
					canvas.DrawRect(Art(x0 + 3, py + ph - 1, pw, 1), A(OakLit, 0.8f));
					canvas.DrawRect(Art(x0 + 3, py, 1, ph), A(OakShade));
				}
				// The ring pull near the meeting edge.
				int rx = side < 0 ? x0 + leafW - 4 : x0 + 2;
				canvas.DrawRect(Art(rx, 44, 2, 2), A(IronLit));
			}
			// The leaf's edge toward the opening, the line that says it has turned.
			int ex = side < 0 ? x0 + leafW - 1 : x0;
			canvas.DrawRect(Art(ex, 1, 1, leafH), A(side < 0 ? OakLit : OakShade, 0.9f));
		}
	}

	// --- His household comes into the room he left -------------------------------------------------

	private struct Arrival
	{
		public float Due;
		public Vector2 At;
		public BestiaryRole Role;
		public float Health;
	}

	private float arrivalClock;

	// At the room's doors - they come in from the halls, between the player and his way out - and, at a
	// door too near the player or in a room with none, round where he left.
	private void FillTheRoomHeLeft(CourtLayout.Room left, int count)
	{
		Node2DGame game = Node2DGame.Current;
		Player wizard = TargetPlayer as Player;
		if (game == null || count <= 0)
			return;
		Vector2 playerAt = wizard?.GlobalPosition ?? GlobalPosition;
		var entries = new List<Vector2>();
		CourtLayout layout = CurrentLayout();
		if (layout != null && left != null)
		{
			foreach (AuthoredLayout.Gap gap in layout.Grid.Doors)
			{
				if (gap.Room != left.Id)
					continue;
				Vector2 mouth = layout.CellsToWorld(gap.Rect).GetCenter();
				Vector2 inward = (left.Centre - mouth).Normalized();
				Vector2 at = mouth + inward * 90f;
				if (at.DistanceTo(playerAt) >= HouseholdMinPlayerDistance)
					entries.Add(at);
			}
		}
		if (entries.Count == 0)
		{
			for (int k = 0; k < 3; k++)
			{
				float angle = rng.Randf() * Mathf.Tau;
				Vector2 at = stepFrom + Vector2.FromAngle(angle) * rng.RandfRange(260f, 420f);
				if (at.DistanceTo(playerAt) < HouseholdMinPlayerDistance)
					at = playerAt + (at - playerAt).Normalized() * HouseholdMinPlayerDistance * 1.3f;
				entries.Add(game.ReachablePoint(at));
			}
		}

		for (int i = 0; i < count; i++)
		{
			(BestiaryRole role, float health) = Household[householdTurn++ % Household.Length];
			Vector2 door = entries[i % entries.Count];
			arrivals.Add(new Arrival
			{
				Due = arrivalClock + 0.3f + i * HouseholdStaggerSeconds,
				At = door + new Vector2(rng.RandfRange(-40f, 40f), rng.RandfRange(-40f, 40f)),
				Role = role,
				Health = health * HouseholdHealthMultiplier,
			});
		}
	}

	private void TickArrivals(float dt)
	{
		arrivalClock += dt;
		if (arrivals.Count == 0)
			return;
		Node2DGame game = Node2DGame.Current;
		for (int i = arrivals.Count - 1; i >= 0; i--)
		{
			if (arrivals[i].Due > arrivalClock)
				continue;
			Arrival a = arrivals[i];
			arrivals.RemoveAt(i);
			// Never past the field's cap: the household fills a room, it does not flood the run.
			if (game == null || EnemyRegistry.Count >= HouseholdFieldCap)
				continue;
			Enemy body = game.SpawnEventEnemy(a.Role, game.ReachablePoint(a.At), a.Health, false);
			if (body != null)
				HouseholdCalled++;
		}
	}

	private const int HouseholdFieldCap = 130;

	// --- Teardown -------------------------------------------------------------------------------------

	protected override void StartDeath()
	{
		passage?.Abort();
		arrivals.Clear();
		foreach (Rank rank in ranks)
			rank.Free();
		ranks.Clear();
		foreach (Door door in doors)
			if (IsInstanceValid(door.Node))
				door.Node.QueueFree();
		doors.Clear();
		// A fall seen in full: whatever the flight had done to his look is undone first.
		Modulate = Colors.White;
		base.StartDeath();
	}

	public override void _ExitTree()
	{
		foreach (Rank rank in ranks)
			rank.Free();
		ranks.Clear();
		foreach (Door door in doors)
			if (IsInstanceValid(door.Node))
				door.Node.QueueFree();
		doors.Clear();
		base._ExitTree();
	}

	// --- RegressionChecks.ValidateBossPattern ------------------------------------------------------

	/// <summary>What would silently switch an idea of this fight off. Empty when all is well.</summary>
	internal IEnumerable<string> TuningProblems()
	{
		if (RankTellSeconds <= 0f)
			yield return "The Seneschal's rank sets off with no lane marked, so the wall that walks lands unseen.";
		if (RankGuards < 1 || RankSpacing <= 0f || RankDepth <= 0f || RankDamage <= 0)
			yield return $"The Seneschal's rank ({RankGuards} guards {RankSpacing} px apart, {RankDepth} px deep, {RankDamage} damage) can touch nothing.";
		if (RankSpeed <= 220f)
			yield return $"The Seneschal's rank walks at {RankSpeed} px/s, no faster than the wizard, so the lane is outrun rather than stepped out of.";
		if (RankMinLength > RankMaxLength)
			yield return $"The Seneschal's rank is at least {RankMinLength} px and at most {RankMaxLength} px long.";
		if (FlightMinSeconds <= 0f || FlightMaxSeconds < FlightMinSeconds)
			yield return $"The Seneschal travels for {FlightMinSeconds}-{FlightMaxSeconds} s, so he never leaves or never arrives.";
		if (StepOutSeconds <= 0f || StepInSeconds <= 0f)
			yield return "The Seneschal leaves or arrives with no door, between one frame and the next.";
		if (HouseholdFirst <= 0)
			yield return "The Seneschal leaves an empty room behind him, so his flight costs the player nothing.";
		if (HopAfterSeconds <= 0f || HopArriveDistance <= SlamRadius)
			yield return $"The Seneschal comes to a lost player after {HopAfterSeconds} s and {HopArriveDistance} px from him, inside his own {SlamRadius} px ring: a door straight into a hit.";
		if (ChandelierTellSeconds <= 0f || ChandelierRadius <= 0f)
			yield return "The Great Hall's chandeliers fall unmarked or on nothing.";
		if (!ResourceLoader.Exists(GuardFramesPath))
			yield return $"The Seneschal's rank is drawn from '{GuardFramesPath}', which is missing.";
		if (FlightThresholds.Length < 2)
			yield return "The Seneschal flees fewer than twice, so there is no pursuit to speak of.";
	}

	// --- The arrival (BossDeath, entrance) ---------------------------------------------------------
	//
	// THROUGH A SERVANTS' DOOR. A pair of panelled oak doors stands where there was none; they swing
	// open on the dark; he steps out of it, unlit at first, the light finding him as he comes forward;
	// the doors shut behind him with a thud of dust; he lifts his staff of office and brings it down on
	// the floor - the house called to order - and the mark opens under his cap. The doors are gone. It
	// is how he will leave, three times, and how he will be found.
	protected internal override float EntranceSeconds => 3.2f;
	protected internal override float EntranceClimaxWeight => 0.9f;
	protected internal override float EntranceRoarSeconds => StaffStrikes + 0.04f;

	private BossDeath.Piece arrivingBody;
	private BossDeath.Piece arrivingStaff;
	private const float DoorsAppear = 0.35f;
	private const float DoorsOpen = 0.6f;
	private const float StepsOut = 0.95f;
	private const float StepsClear = 1.75f;
	private const float DoorsShut = 1.95f;
	private const float StaffLifts = 2.25f;
	private const float StaffStrikes = 2.5f;

	// Where his staff stands and his hand holds it, in the sheet's art pixels (boss_seneschal.py).
	private static readonly Rect2I StaffAbove = new(68, 2, 6, 46);
	private static readonly Rect2I StaffBelow = new(68, 54, 6, 41);
	private static readonly Vector2 StaffBreak = new(71f, 51f);
	private static readonly Vector2 StaffFoot = new(71f, 94f);
	private static readonly Rect2I Cap = new(35, 2, 19, 14);
	private static readonly Vector2 EyeHot = new(43f, 20f), EyeCool = new(46f, 20f);
	private static readonly Vector2 Chain = new(43f, 37f);
	private static readonly Color HallDust = new(0.42f, 0.44f, 0.50f, 0.55f);

	protected internal override void OnEntranceBegin(BossDeath stage)
	{
		arrivingStaff = stage.Cut(new Vector2I(71, 94), new Rect2I(68, 2, 6, 93));
		arrivingBody = stage.Cut(new Vector2I(43, 94), new Rect2I(0, 0, 68, 96), new Rect2I(68, 95, 28, 1));
		foreach (BossDeath.Piece p in new[] { arrivingBody, arrivingStaff })
			p.Color = new Color(1f, 1f, 1f, 0f);
	}

	protected internal override void TickEntrance(BossDeath stage, float t, float dt)
	{
		if (arrivingBody == null)
			return;
		float s = stage.ArtScale.Y;
		// Out of the dark of the doorway: forward a step, the light finding him as he comes.
		float step = BossDeath.EaseOut(BossDeath.Span(t, StepsOut, StepsClear));
		float shown = BossDeath.Span(t, StepsOut, StepsOut + 0.35f);
		var lit = new Color(0.16f, 0.17f, 0.24f).Lerp(Colors.White, step);
		foreach (BossDeath.Piece p in new[] { arrivingBody, arrivingStaff })
		{
			p.Color = new Color(lit, shown);
			p.Offset = new Vector2(0f, -8f * s * (1f - step));
		}
		// Then the staff: lifted a hand's breadth, and brought down on the floor.
		float lift = BossDeath.EaseOut(BossDeath.Span(t, StaffLifts, StaffStrikes - 0.08f))
			* (1f - BossDeath.EaseIn(BossDeath.Span(t, StaffStrikes - 0.08f, StaffStrikes)));
		arrivingStaff.Offset += new Vector2(0f, -5f * s * lift);

		Vector2 foot = stage.Foot;
		if (stage.Crossed(DoorsShut))
		{
			stage.Impact(0.45f);
			stage.DustRing(foot + new Vector2(0f, -2f), 64f, 8, HallDust, 0.7f, 14f);
		}
		if (stage.Crossed(StaffStrikes))
		{
			Vector2 tip = stage.ArtToLocal(StaffFoot);
			stage.DustRing(tip, 30f, 6, HallDust, 0.6f, 12f);
			stage.PuffAt(tip, 4f, 22f, 0.5f, new Color(StaffColor, 0.6f), 4f, 0f, true);
		}
	}

	protected internal override void DrawEntranceGround(BossDeath stage, CanvasItem canvas, float t)
	{
		float alpha = BossDeath.Span(t, 0f, DoorsAppear) * (1f - BossDeath.Span(t, DoorsShut + 0.35f, DoorsShut + 0.9f));
		float open = BossDeath.EaseOut(BossDeath.Span(t, DoorsAppear, DoorsOpen))
			* (1f - BossDeath.EaseIn(BossDeath.Span(t, StepsClear + 0.05f, DoorsShut)));
		PaintDoor(canvas, stage.Foot, open, alpha);
	}

	protected internal override void DrawEntranceLight(BossDeath stage, CanvasItem canvas, float t)
	{
		if (arrivingBody == null)
			return;
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		// The mark opens with the strike, flares once, and settles.
		float eyes = BossDeath.Span(t, StaffStrikes, StaffStrikes + 0.1f) * (1f - BossDeath.Span(t, StaffStrikes + 0.25f, EntranceSeconds + 0.3f));
		if (eyes > 0f)
		{
			BossDeath.Disc(canvas, stage.PieceToLocal(arrivingBody, EyeHot), 6f, 6f, new Color(arcane.Hot, 0.8f * eyes));
			BossDeath.Disc(canvas, stage.PieceToLocal(arrivingBody, EyeCool), 6f, 6f, new Color(arcane.Hot, 0.8f * eyes));
		}
		// The strike: a short flash of his light at the staff's foot.
		float flash = BossDeath.Span(t, StaffStrikes, StaffStrikes + 0.05f) * (1f - BossDeath.Span(t, StaffStrikes + 0.05f, StaffStrikes + 0.35f));
		if (flash > 0f)
			BossDeath.Disc(canvas, stage.ArtToLocal(StaffFoot), 14f, 6f, new Color(arcane.Mid, 0.7f * flash));
	}

	// --- The fall (BossDeath) ---------------------------------------------------------------------
	//
	// THE STAFF IS BROKEN. A steward broke his white staff over his lord's grave when his office ended;
	// this one's ends here. The staff snaps where his hand holds it, in a burst of white splinters; the
	// top half spins away and the foot tips over across his front; his cap goes off behind him; he sinks
	// to his knees and the gown pools on the floor; the chain of office slides off him and its links
	// skitter across the boards; and the mark under the cap goes out. Linen, red, gilt and bone - every
	// piece of debris is one of what he wore.
	protected internal override float DeathSeconds => 2.8f;

	private BossDeath.Piece fallBody;
	private BossDeath.Piece fallCap;
	private BossDeath.Piece fallStaffTop;
	private BossDeath.Piece fallStaffFoot;
	private const float Snaps = 0.14f;
	private const float TopLands = 0.82f;
	private const float FootLands = 0.72f;
	private const float CapLands = 1.05f;
	private const float Kneels = 1.3f;

	protected internal override void OnDeathBegin(BossDeath death)
	{
		// The body first, so what falls from it - the cap, the staff's foot across his front - lies over it.
		fallBody = death.Cut(new Vector2I(43, 94), new Rect2I(0, 16, 68, 80), new Rect2I(0, 0, 35, 16),
			new Rect2I(54, 0, 14, 16), new Rect2I(68, 48, 6, 6), new Rect2I(74, 94, 22, 2), new Rect2I(68, 95, 6, 1));
		fallCap = death.Cut(new Vector2I(44, 15), Cap);
		fallStaffFoot = death.Cut(new Vector2I(71, 94), StaffBelow);
		fallStaffTop = death.Cut(new Vector2I(71, 47), StaffAbove);
	}

	protected internal override void TickDeath(BossDeath death, float t, float dt)
	{
		if (fallBody == null)
			return;
		float s = death.ArtScale.Y;

		// The top half: up off the break with the snap, turning away from him, down flat on the floor.
		float top = BossDeath.Span(t, Snaps, TopLands);
		float hop = 4f * top * (1f - top);
		float drop = BossDeath.EaseIn(top);
		fallStaffTop.Rotation = Mathf.DegToRad(96f) * BossDeath.EaseIn(top) - 0.06f * Mathf.Sin(Mathf.Pi * BossDeath.Span(t, TopLands, TopLands + 0.2f));
		fallStaffTop.Offset = new Vector2(30f * s * top, (47f * drop - 14f * hop) * s);

		// The foot tips the other way, across his front, on its iron shoe.
		float foot = BossDeath.EaseIn(BossDeath.Span(t, Snaps + 0.06f, FootLands));
		fallStaffFoot.Rotation = -Mathf.DegToRad(84f) * foot + 0.05f * Mathf.Sin(Mathf.Pi * BossDeath.Span(t, FootLands, FootLands + 0.18f));

		// The cap off behind him, once over, onto its crown.
		float cap = BossDeath.Span(t, 0.3f, CapLands);
		float capArc = 4f * cap * (1f - cap);
		fallCap.Offset = new Vector2(-34f * s * cap, (78f * cap * cap - 18f * capArc) * s);
		fallCap.Rotation = -Mathf.DegToRad(200f) * BossDeath.EaseOut(cap);

		// He goes down onto his knees and the gown pools round him.
		float kneel = BossDeath.EaseInOut(BossDeath.Span(t, 0.35f, Kneels));
		fallBody.Sink(death, 28f * kneel);
		fallBody.Scale = new Vector2(1f + 0.12f * kneel, 1f);
		fallBody.Rotation = Mathf.DegToRad(5f) * kneel;

		// Then the house's dusk takes him and what he dropped.
		float dim = BossDeath.Span(t, 1.4f, 2.1f);
		var dusk = new Color(1f - 0.5f * dim, 1f - 0.5f * dim, 1f - 0.38f * dim, 1f);
		fallBody.Color = new Color(dusk, 1f - BossDeath.Span(t, 1.9f, 2.5f));
		var gone = new Color(dusk, 1f - BossDeath.Span(t, 2.15f, DeathSeconds));
		fallCap.Color = gone;
		fallStaffTop.Color = gone;
		fallStaffFoot.Color = gone;

		if (death.Crossed(Snaps))
		{
			death.Impact(0.4f);
			// The snap: white splinters off the break.
			death.Emit(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.Bone, SpeedMin = 60f, SpeedMax = 170f, UpMin = 90f, UpMax = 220f,
				Spin = 7f, Bounce = 0.3f, Friction = 7f, Life = DeathSeconds - Snaps, Fade = 0.4f,
			}, 7, StaffBreak, new Vector2(2f, 3f));
			death.Emit(new BossDeath.Throw
			{
				Size = BossDeath.Px * 2f, Color = new Color(0.93f, 0.95f, 0.98f), ColorB = new Color(0.60f, 0.64f, 0.72f),
				SpeedMin = 40f, SpeedMax = 150f, UpMin = 80f, UpMax = 200f, Bounce = 0.2f, Life = 1.3f, Fade = 0.4f,
			}, 10, StaffBreak, new Vector2(2f, 2f));
		}
		if (death.Crossed(0.9f))
		{
			// The chain of office off his shoulders - from where they are now he is half down, not where
			// they stood - gilt links, sliding and skittering.
			Vector2 chest = death.PieceToLocal(fallBody, Chain);
			death.EmitAt(new BossDeath.Throw
			{
				Cell = BossDeath.Cell.LinkFlat, Variants = 2, Color = new Color(1.0f, 0.86f, 0.42f), ColorB = new Color(0.84f, 0.66f, 0.26f),
				SpeedMin = 30f, SpeedMax = 120f, UpMin = 30f, UpMax = 90f, Spin = 4f, Bounce = 0.25f, Friction = 5f,
				Life = DeathSeconds - 0.9f, Fade = 0.4f, Glint = new Color(1f, 0.94f, 0.7f, 0.9f),
			}, 9, new Vector2(chest.X, death.Foot.Y - 4f), Mathf.Max(0f, death.Foot.Y - chest.Y), 14f, 0f, 0.02f);
		}
		if (death.Crossed(FootLands))
		{
			Vector2 tip = death.PieceToLocal(fallStaffFoot, new Vector2(71f, 56f));
			death.DustRing(new Vector2(tip.X, death.Foot.Y - 4f), 22f, 4, HallDust, 0.5f, 10f);
		}
		if (death.Crossed(TopLands))
		{
			Vector2 tip = death.PieceToLocal(fallStaffTop, new Vector2(71f, 6f));
			death.DustRing(new Vector2(tip.X, death.Foot.Y - 4f), 26f, 5, HallDust, 0.55f, 11f);
		}
		if (death.Crossed(CapLands))
			death.PuffAt(death.PieceToLocal(fallCap, new Vector2(44f, 8f)), 6f, 18f, 0.45f, HallDust, 6f);
		if (death.Crossed(Kneels - 0.1f))
			death.DustRing(death.Foot + new Vector2(0f, -4f), 70f, 9, HallDust, 0.8f, 16f);
	}

	protected internal override void DrawDeathLight(BossDeath death, CanvasItem canvas, float t)
	{
		if (fallBody == null)
			return;
		ElementColors.Ramp arcane = ElementColors.GetRamp(Element.Arcane);
		// The break: his light runs across the staff where it will part, and goes with the snap.
		float seam = BossDeath.Span(t, 0f, Snaps) * (1f - BossDeath.Span(t, Snaps, Snaps + 0.2f));
		if (seam > 0f)
		{
			for (int k = -1; k <= 3; k++)
			{
				Vector2 at = death.ArtToLocal(StaffBreak + new Vector2(k, (k % 2) - 3f));
				canvas.DrawRect(WhipLimb.Cell(at, BossDeath.Px * 2f), new Color(arcane.Mid, 0.75f * seam));
				canvas.DrawRect(WhipLimb.Cell(at, BossDeath.Px), new Color(arcane.Core, seam));
			}
		}
		// And the mark under the cap flares once and goes out.
		float eyes = BossDeath.Span(t, 0f, 0.06f) * (1f - BossDeath.Span(t, 0.1f, 0.5f));
		if (eyes > 0f)
		{
			BossDeath.Disc(canvas, death.PieceToLocal(fallBody, EyeHot), 6f, 6f, new Color(arcane.Hot, 0.8f * eyes));
			BossDeath.Disc(canvas, death.PieceToLocal(fallBody, EyeCool), 6f, 6f, new Color(arcane.Hot, 0.8f * eyes));
		}
	}
}
