using System;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// FLEE (.ai/chapter-wizards.md 4.1 and section 7, "a mover that flees"): SecretMover inverted. SecretMover goes on
/// ahead of him and waits; this goes AWAY from him and never waits. When he comes within <see cref="StartleRadius"/> it
/// turns its back on him and bolts - quicker than his walk for <see cref="BoltSeconds"/> - and then strides on slower
/// than him while he stays that close; further off it treads its own slow way. Water, a wall or a trunk across the
/// way it wants cuts a bolt short (<see cref="BoltsCut"/>): it slides off along whatever way is open at the stride, or
/// stands at bay if none is. Left alone past <see cref="LoseRadius"/> for <see cref="CalmSeconds"/> it calms and stands.
/// Given a <see cref="Goal"/>, fright sends it TOWARD that instead, "away from him" only a lean (the walking hut, which
/// runs for its yard since in-flight 79).
/// </summary>
/// <remarks>
/// <para>A plain class, not a node: the owner (WalkingHutEvent) calls <see cref="Step"/> once a frame with where the
/// chaser is and how fast he walks, and draws whatever it likes from <see cref="Position"/>, <see cref="Heading"/>
/// and <see cref="Speed"/>. So the same rules run in the game and in RegressionChecks, which chases it on paper.</para>
/// <para>Every speed that matters is a SHARE of the chaser's own walking speed, read each step: "quicker than his
/// walk" stays true for a wizard with a speed boon. Only the tread (its own pace, unhurried) and the circle
/// it walks before anyone has come are in pixels.</para>
/// <para>What is blocked is the owner's to say (<see cref="IsBlocked"/>): the terrain, the stage's edge, trunks.
/// Nothing here is a body; nothing here joins "enemies".</para>
/// </remarks>
public sealed class FleeingMover
{
	/// <summary>He comes this close and it turns its back and goes (4.1: 420).</summary>
	public float StartleRadius { get; set; } = 420f;

	/// <summary>How long a bolt lasts at <see cref="BoltShare"/> of his speed (4.1: 2 s).</summary>
	public float BoltSeconds { get; set; } = 2f;

	/// <summary>A bolt's speed, as a share of his walking speed: quicker than him.</summary>
	public float BoltShare { get; set; } = 1.3f;

	/// <summary>After a bolt, while he is still within the startle radius: slower than him, so he gains.</summary>
	public float StrideShare { get; set; } = 0.75f;

	/// <summary>The least time from the end of one bolt to the start of the next.</summary>
	public float BoltRest { get; set; } = 1.5f;

	/// <summary>Its own pace when he is near but not that near: still walking away, unhurried. Pixels a second.</summary>
	public float TreadSpeed { get; set; } = 60f;

	/// <summary>Further than this from him for <see cref="CalmSeconds"/> and it stops fleeing and stands.</summary>
	public float LoseRadius { get; set; } = 1100f;
	public float CalmSeconds { get; set; } = 3f;

	/// <summary>
	/// Before anyone has startled it, it walks a circle this wide round <see cref="Home"/> at <see cref="CircleSpeed"/>
	/// (0 radius: it stands). Once it has fled, it never goes back to its circle.
	/// </summary>
	public Vector2 Home { get; set; }
	public float CircleRadius { get; set; } = 200f;
	public float CircleSpeed { get; set; } = 45f;

	/// <summary>0 to 1: how tired it is. A tired bolt is a slower one (to <see cref="TiredBoltShare"/>).</summary>
	public float Tire { get; set; }
	public float TiredBoltShare { get; set; } = 1.1f;

	/// <summary>How far ahead of its feet a way is tested for open ground.</summary>
	public float ProbeAhead { get; set; } = 44f;

	/// <summary>The owner's ground test: true where it cannot put a foot.</summary>
	public Func<Vector2, bool> IsBlocked { get; set; } = _ => false;

	/// <summary>
	/// Where it runs TO while it flees, from where it stands; null (the default) and it runs straight away from him.
	/// The walking hut's is its yard (in-flight 79, Eric 2026-11-09: "Baba Yaga's hut should head toward the yard if
	/// you're near it at all, it's too hard to herd it"): the gap's mouth first, then the middle.
	/// </summary>
	public Func<Vector2, Vector2> Goal { get; set; }

	/// <summary>With a <see cref="Goal"/>: how much "away from him" still leans its way, against the goal's 1.</summary>
	public float AwayWeight { get; set; } = 0.35f;

	public Vector2 Position { get; set; }

	/// <summary>The way it is going (or last went); unit length.</summary>
	public Vector2 Heading { get; private set; } = Vector2.Up;

	/// <summary>Pixels a second this step, 0 when it stands.</summary>
	public float Speed { get; private set; }

	/// <summary>True between being startled and calming again.</summary>
	public bool Fleeing { get; private set; }

	/// <summary>True while a bolt lasts.</summary>
	public bool Bolting => boltLeft > 0f;

	/// <summary>True once it has fled at least once: it has left its circle for good.</summary>
	public bool HasFled { get; private set; }

	/// <summary>True when every way away from him was blocked this step: it stands at bay.</summary>
	public bool AtBay { get; private set; }

	/// <summary>Stopped for good (it is tired, or the event is over): <see cref="Step"/> moves nothing.</summary>
	public bool Stopped { get; set; }

	/// <summary>Bolts started, and bolts cut short by blocked ground. For the probes.</summary>
	public int BoltsStarted { get; private set; }
	public int BoltsCut { get; private set; }

	/// <summary>How many times it calmed after losing him. For the probes.</summary>
	public int TimesCalmed { get; private set; }

	/// <summary>Raised when a bolt begins.</summary>
	public event Action Startled;

	private float boltLeft;
	private float restLeft;
	private float farFor;
	private float circleAngle = float.NaN;
	private int turnSide = 1;

	// The ways tried, in order, as turns off the way it wants (radians): straight on, then wider each side.
	private static readonly float[] Turns = { 0f, 0.6f, -0.6f, 1.2f, -1.2f, 1.8f, -1.8f };

	/// <summary>One step of <paramref name="delta"/> seconds, with the chaser at <paramref name="chaser"/> walking at
	/// <paramref name="chaserSpeed"/>. Returns the distance it moved.</summary>
	public float Step(float delta, Vector2 chaser, float chaserSpeed)
	{
		Speed = 0f;
		AtBay = false;
		if (Stopped || delta <= 0f)
			return 0f;

		Vector2 fromHim = Position - chaser;
		float d = fromHim.Length();
		Vector2 away = d > 1f ? fromHim / d : Heading;
		boltLeft = MathF.Max(0f, boltLeft - delta);
		restLeft = MathF.Max(0f, restLeft - delta);

		if (d <= StartleRadius)
		{
			farFor = 0f;
			if (!Fleeing)
			{
				Fleeing = true;
				HasFled = true;
			}
			if (boltLeft <= 0f && restLeft <= 0f)
			{
				boltLeft = BoltSeconds;
				restLeft = BoltSeconds + BoltRest;
				BoltsStarted++;
				Startled?.Invoke();
			}
		}
		else if (Fleeing && d > LoseRadius)
		{
			farFor += delta;
			if (farFor >= CalmSeconds)
			{
				Fleeing = false;
				boltLeft = 0f;
				TimesCalmed++;
			}
		}
		else
			farFor = 0f;

		Vector2 want;
		float speed;
		// Where fright sends it: straight away from him, or - with a goal - toward the goal (FleeWay).
		Vector2 flee = Goal == null ? away : FleeWay(away, d);
		if (boltLeft > 0f)
		{
			want = flee;
			speed = Mathf.Lerp(BoltShare, TiredBoltShare, Mathf.Clamp(Tire, 0f, 1f)) * chaserSpeed;
		}
		else if (Fleeing && d <= StartleRadius)
		{
			want = flee;
			speed = StrideShare * chaserSpeed;
		}
		else if (Fleeing)
		{
			// Near enough to have its attention, not near enough to frighten it: it keeps on its way, turning off
			// from him (or toward its goal) by degrees rather than snapping round.
			want = (Heading * 2f + flee).Normalized();
			speed = TreadSpeed;
		}
		else if (!HasFled && CircleRadius > 1f)
		{
			// Its circle, for ever, until someone comes.
			Vector2 fromHome = Position - Home;
			if (float.IsNaN(circleAngle))
				circleAngle = fromHome.LengthSquared() > 1f ? fromHome.Angle() : 0f;
			circleAngle += CircleSpeed * delta / CircleRadius;
			Vector2 on = Home + Vector2.FromAngle(circleAngle) * CircleRadius;
			Vector2 to = on - Position;
			want = to.LengthSquared() > 1f ? to.Normalized() : Heading;
			speed = MathF.Min(CircleSpeed * 1.5f, to.Length() / delta);
		}
		else
			return 0f;

		// Never past its goal in one step: a stride that would carry it over the point stops on it.
		if (Goal != null && Fleeing)
		{
			float toGoal = Position.DistanceTo(Goal(Position));
			if (toGoal < speed * delta)
				speed = toGoal / delta;
		}

		// The ground. The way it wants first, then wider either side, the side it last turned to first so it does not
		// dither; the first way that is open is the one it goes.
		for (int k = 0; k < Turns.Length; k++)
		{
			float turn = Turns[k] * turnSide;
			Vector2 way = want.Rotated(turn);
			if (IsBlocked(Position + way * (ProbeAhead + speed * delta)))
				continue;
			if (k > 0)
			{
				// Something is across the way it wanted: a bolt ends there, and it goes on at the stride.
				if (boltLeft > 0f)
				{
					boltLeft = 0f;
					BoltsCut++;
				}
				speed = MathF.Min(speed, MathF.Max(TreadSpeed, StrideShare * chaserSpeed));
				turnSide = turn >= 0f ? 1 : -1;
			}
			Heading = way;
			Speed = speed;
			Position += way * speed * delta;
			return speed * delta;
		}
		// Nowhere open: it stands at bay, its bolt spent.
		if (boltLeft > 0f)
		{
			boltLeft = 0f;
			BoltsCut++;
		}
		AtBay = true;
		turnSide = -turnSide;
		return 0f;
	}

	// With a goal: toward it, "away from him" only a lean (AwayWeight), and never through him - when he stands across
	// its way to the goal it goes round him, on the side its way already leans to (the side it last turned to when he
	// is dead ahead), harder the more squarely he blocks it.
	private Vector2 FleeWay(Vector2 away, float d)
	{
		Vector2 toGoal = Goal(Position) - Position;
		if (toGoal.LengthSquared() < 1f)
			return Heading;
		toGoal = toGoal.Normalized();
		Vector2 want = toGoal + away * AwayWeight;
		float blocking = toGoal.Dot(-away);
		if (blocking > 0.3f && d < StartleRadius * 1.5f)
		{
			Vector2 side = new(-away.Y, away.X);
			float lean = side.Dot(toGoal);
			if (lean < 0f || (lean == 0f && turnSide < 0))
				side = -side;
			want += side * (1.6f * blocking);
		}
		return want.LengthSquared() > 1e-4f ? want.Normalized() : toGoal;
	}
}
