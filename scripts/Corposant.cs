using Godot;
using System;

namespace TheOneHeMissed.scripts;

// CORPOSANT (Lightning), built 2026-11-09: sailors' word for St Elmo's fire, the pale storm-fire that burns at a
// masthead in foul weather. Eric approved it from .ai/character-fit.md's queue: "a ball of storm-fire that bounces
// around the screen for a few seconds, off the edges of the visible camera view, zapping what it passes through".
//
// IT AIMS AT NOTHING. Every cast looses its balls from the wizard in random directions (spread evenly round a random
// start, so two never leave together), and nothing after that looks for an enemy: a ball flies straight, pierces
// everything it passes and turns only at the edge of what the camera shows. It is the only spell that picks no target,
// no spot and no direction (SpellTargetingMode.Unaimed; .ai/spell-variety.md section 2, the Runetracer row). Its worth
// is the open ground it crosses, so it rewards fighting in the open: a maze wall (MazeNavigation.IsWallAt, the test
// every bolt uses) GROUNDS a ball where it touches - the storm-fire goes to earth - and a wizard pressed against a wall
// loses half of every cast into it.
//
// THE EDGES ARE THE CAMERA'S. The visible rect taken back through the canvas transform, read once a frame (ViewNow), so
// they follow the wizard and are right on the PC's 1280x720 landscape canvas and the phone's 720x1280 portrait one at
// the camera's 0.85 (DisplayProfile, player.tscn): about 1,500 world units the long way and 850 the short. An axis is
// turned only while the ball is past that edge AND still heading out (Bounce), so a camera that runs past a ball never
// throws it about: it simply comes back in. Inset by the ball's radius, so its shell, not its heart, meets the edge.
// Without a camera framing the wizard (a headless harness) the view is the canvas over the zoom, centred on him.
//
// THE PAYLOAD. A ball strikes every body its swept path passes within reach of - the frame's segment, as Folio's pages
// test theirs, so nothing steps over a body at a low frame rate - and one ball strikes one body at most once every
// StrikeEvery seconds: its hit memory is a ring of the last HitMemory bodies it struck. ONE walk of
// EnemyRegistry.Snapshot a frame serves every ball, the wake and Saint's Crown's crowd count: each body's position is
// read once and tested against every live ball's swept box, so a frame costs one position read a body whatever the
// count of balls (.ai/performance.md). Nothing is allocated per frame: balls, hit memory, wake, sparks and bolts are
// fixed arrays, and a struck body is remembered by reference, never by an id asked of the engine.
//
// SPELLS START SMALL (Eric, 2026-11-02): one ball at level 1, two at 3, three at 6; 3 s of life, 3.5 from level 5 and 4
// at 8; a 3.0 s cooldown down to 2.6 s (SpellData_Corposant.tres). About one ball out at level 1, four or five at 8.
//
// THE DRAWING. One node in the arena at the origin, drawing in world space on the 2 px grid, on DrawLayers.Air. A ball is
// a white-hot heart in a pale shell of the element's ramp with a rim of its edge, three tongues of storm-fire streaming
// back against its travel (it burns like a flame, not a lamp) and crackling spurs re-chosen every flicker. A strike draws
// a jagged bolt from the ball to the body for a tenth of a second - what it hits is seen hit - and a bounce, a grounding
// and a burnt-out ball each crack where they happen. Every fade is a dither (SpellInk.DitherByCell). Lightning is not a
// warm element (LightPool.IsWarm), so a ball lights nothing in the dark chapters.
//
// THE MILESTONES (SpellEvolutionCatalog.SetupCorposantEvolutions), every one a change on screen:
//   Twin Fires    (level 4) - on its first bounce a ball splits in two, each twin flying its own way for TwinShare.
//   Static Wake   (level 4, +1 Lightning) - a ball leaves a wake of static that crackles a moment and zaps what stands
//                 in it for WakeShare of a strike.
//   Spindrift     (level 4, +1 Water: the element path) - the fire burns sea-blue (Water's ramp; IPaintsElementPath) and
//                 its hits slow (the rider, which Player.DealDamageToEnemy delivers for every spell).
//   Storm Lantern (level 8, +1 Lightning) - the edges throw a ball home rather than back: between bounces it hangs over
//                 the wizard like a masthead light, then flies out a new way.
//   Saint's Crown (level 8) - while the wizard is crowded the balls are drawn into a crown circling close round him, and
//                 flung off along it when the crowd thins.
public partial class Corposant : Node2D, IPaintsElementPath
{
	public const string SpellId = "corposant";

	// Milestone ids. SpellEvolutionCatalog builds the cards from these; this node reads them.
	public const string TwinFiresId = "corposant_twin_fires";
	public const string StaticWakeId = "corposant_static_wake";
	/// <summary>The element path into Water: SpellEvolutionCatalog.ElementPathOption names it the spell's id and "spindrift".</summary>
	public const string SpindriftId = "corposant_spindrift";
	public const string StormLanternId = "corposant_storm_lantern";
	public const string SaintsCrownId = "corposant_saints_crown";

	// --- the ball ---------------------------------------------------------------------------------
	/// <summary>Seconds a ball burns at level 1, before the levels' ZoneDuration and the wizard's own Duration.</summary>
	public const float BaseLifeSeconds = 3f;
	/// <summary>The least time between two strikes of one ball on one body.</summary>
	public const float StrikeEvery = 0.5f;
	/// <summary>The most balls one cast looses, Amount and all.</summary>
	public const int MaxPerCast = 12;
	private const int MaxBalls = 48;
	private const int HitMemory = 24;
	private const float FadeInSeconds = 0.08f;
	private const float FadeOutSeconds = 0.25f;
	private const float LaunchJitter = 0.35f;

	// --- Twin Fires --------------------------------------------------------------------------------
	/// <summary>What each twin strikes for, of the ball it split from. The card says it; RegressionChecks.Corposant holds the two.</summary>
	public const float TwinShare = 0.7f;
	/// <summary>Each twin leaves the bounce this far either side of where the ball was turned.</summary>
	public const float TwinSplitDegrees = 26f;

	// --- Static Wake -------------------------------------------------------------------------------
	/// <summary>A wake's zap, of a strike.</summary>
	public const float WakeShare = 0.3f;
	public const float WakeSeconds = 0.8f;
	public const float WakeTickSeconds = 0.25f;
	public const float WakeSpacing = 16f;
	public const float WakeReach = 12f;
	private const int MaxWake = 256;

	// --- Storm Lantern -----------------------------------------------------------------------------
	/// <summary>How high over the wizard's middle a ball hangs: a masthead light over his hat.</summary>
	public const float LanternLift = 52f;
	public const float LanternHangSeconds = 0.4f;

	// --- Saint's Crown -----------------------------------------------------------------------------
	/// <summary>Bodies within CrowdReach of the wizard that crown the balls, and the count under which they are let go.</summary>
	public const int CrowdEnter = 8;
	public const int CrowdLeave = 5;
	public const float CrowdReach = 140f;
	public const float CrownRadius = 78f;
	/// <summary>A crown lasts at least this long, so a crowd at the threshold does not make it flicker.</summary>
	public const float CrownHoldSeconds = 1f;
	private const float CrownTurnShare = 0.8f;
	private const float CrownCatchUp = 1.6f;

	private const int MaxSparks = 64;
	private const int BoltsPerBall = 3;
	private const float BoltSeconds = 0.1f;
	private const float SparkSeconds = 0.22f;
	private const float FlickerSeconds = 0.06f;
	private const float Body = RiptideWave.BodyAllowance;
	private const float Grid = SpellInk.Grid;
	/// <summary>The camera's zoom in player.tscn, for a view with no camera to ask.</summary>
	private const float FallbackZoom = 0.85f;

	// --- the ball (tuned in scenes/Corposant.tscn) -------------------------------------------------
	[Export] public float BaseSpeed { get; set; } = 430f;
	[Export] public float HitRadius { get; set; } = 12f;

	private enum Mode : byte { Free, Home, Hang, Crown }

	private struct Ball
	{
		public Vector2 Pos;
		public Vector2 From;
		public Vector2 Vel;
		public float Age;
		public float Life;
		public float Radius;
		public float HangOffset;
		public float ModeClock;
		public float WakeTravel;
		public float BoltAge;
		public int Damage;
		public int Bolts;
		public int Bounces;
		public uint Seed;
		public uint BoltSeed;
		public Mode Mode;
		public bool Twin;
		public bool Fresh;
		public bool Live;
	}

	private struct Wake { public Vector2 At; public float Age; public int Damage; public uint Seed; public bool Live; }

	private enum SparkKind : byte { Bounce, Ground, Out, Split }

	private struct Spark { public Vector2 At; public float Age; public float Size; public uint Seed; public SparkKind Kind; public bool Live; }

	public Player PlayerRef { get; set; }
	public SpellData SpellData { get; private set; }

	// For the harness (_BalanceHarness's CORPOSANT line). Process-wide, as Folio's are.
	public static int StatLaunched { get; set; }
	public static int StatBounces { get; set; }
	public static int StatGrounded { get; set; }
	public static int StatStrikes { get; set; }
	public static int StatTwins { get; set; }
	public static int StatWakeStrikes { get; set; }
	public static int StatHangs { get; set; }
	public static int StatCrowns { get; set; }

	private int damage = 6;
	private float life = BaseLifeSeconds;
	private float area = 1f;
	private float radius = 12f;
	private Color tint = Colors.White;
	private ElementColors.Ramp ramp = ElementColors.GetRamp(Element.Lightning);
	private bool twinForm, wakeForm, lanternForm, crownForm;

	private readonly Ball[] balls = new Ball[MaxBalls];
	private readonly Enemy[] struck = new Enemy[MaxBalls * HitMemory];
	private readonly float[] struckAt = new float[MaxBalls * HitMemory];
	private readonly int[] struckNext = new int[MaxBalls];
	private readonly Vector2[] boltEnds = new Vector2[MaxBalls * BoltsPerBall];
	private readonly Wake[] wakes = new Wake[MaxWake];
	private readonly Spark[] sparks = new Spark[MaxSparks];
	// One frame's live balls and their swept boxes, for the walk of the swarm.
	private readonly int[] sweepBall = new int[MaxBalls];
	private readonly Vector2[] sweepLo = new Vector2[MaxBalls];
	private readonly Vector2[] sweepHi = new Vector2[MaxBalls];
	private readonly int[] wakeLive = new int[MaxWake];
	private readonly RandomNumberGenerator rng = new();

	private float clock;
	private float wakeClock;
	private float crownPhase;
	private float crownHeld;
	private bool crowned;
	private int lastCrowd;
	private int nextBall, nextWake, nextSpark;
	private uint seedClock;
	private bool drewLastFrame;
	private Vector2 lastMe;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		Material = SpellInk.DitherByCell;
		// One draw from the global stream, so a seeded harness run looses the same directions every time.
		rng.Seed = GD.Randi();
		for (int i = 0; i < struckAt.Length; i++)
			struckAt[i] = float.NegativeInfinity;
	}

	/// <summary>The cast's numbers, read again on every cast as Sleet Runes' and Folio's are; balls in flight keep their own.</summary>
	public void Refresh(SpellData spell, float damageMultiplier, Color visualTint, float playerArea, float playerDuration)
	{
		if (spell == null)
			return;
		SpellData = spell;
		int level = Math.Max(1, spell.CurrentLevel);
		damage = Math.Max(1, Mathf.RoundToInt(spell.GetDamageAtLevel(level) * damageMultiplier));
		life = LifeAt(spell, level) * MathF.Max(0.25f, playerDuration);
		area = MathF.Max(0.25f, playerArea);
		radius = HitRadius * MathF.Sqrt(area);
		tint = visualTint;
		twinForm = spell.HasEvolution(TwinFiresId);
		wakeForm = spell.HasEvolution(StaticWakeId);
		lanternForm = spell.HasEvolution(StormLanternId);
		crownForm = spell.HasEvolution(SaintsCrownId);
		// Spindrift repaints the whole fire in Water's ramp - the blue St Elmo's fire really is.
		ramp = ElementColors.GetRamp(spell.GetBranchElement() ?? Element.Lightning);
	}

	/// <summary>How long a ball of this spell burns at <paramref name="level"/>, before the wizard's own Duration.</summary>
	public static float LifeAt(SpellData spell, int level) =>
		BaseLifeSeconds + (spell?.GetEffectValueAtLevel(SpellEffect.ZoneDuration, level) ?? 0f);

	/// <summary>
	/// Looses <paramref name="count"/> balls from the wizard (from the lantern over him under Storm Lantern), spread evenly
	/// round a random start. Returns how many left; the cast is Fired whenever any did, whatever is near.
	/// </summary>
	public int Launch(int count)
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef) || SpellData == null)
			return 0;
		count = Math.Clamp(count, 1, MaxPerCast);
		Vector2 me = PlayerRef.GlobalPosition;
		float start = rng.Randf() * Mathf.Tau;
		for (int k = 0; k < count; k++)
		{
			float angle = start + Mathf.Tau * k / count + (rng.Randf() - 0.5f) * LaunchJitter;
			Vector2 heading = new(MathF.Cos(angle), MathF.Sin(angle));
			float offset = (k - (count - 1) * 0.5f) * 8f;
			Vector2 from = lanternForm ? me + new Vector2(offset, -LanternLift) : me;
			int slot = Spawn(from, heading * BaseSpeed, damage, life, 0f, false, crowned ? Mode.Crown : Mode.Free);
			balls[slot].HangOffset = offset;
		}
		StatLaunched += count;
		return count;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (dt <= 0f)
			return;
		clock += dt;
		bool alive = PlayerRef != null && IsInstanceValid(PlayerRef) && !PlayerRef.IsDead;
		Vector2 me = alive ? PlayerRef.GlobalPosition : lastMe;
		lastMe = me;

		int live = 0;
		for (int b = 0; b < MaxBalls; b++)
			if (balls[b].Live)
				live++;

		if (live > 0)
		{
			UpdateCrown(dt);
			Rect2 view = ViewNow(me).Grow(-radius);
			MazeNavigation nav = MazeNavigation.Active;
			int crownCount = 0;
			if (crowned)
				for (int b = 0; b < MaxBalls; b++)
					if (balls[b].Live && balls[b].Mode == Mode.Crown)
						crownCount++;
			int crownIndex = 0;
			for (int b = 0; b < MaxBalls; b++)
			{
				if (!balls[b].Live)
					continue;
				ref Ball ball = ref balls[b];
				ball.From = ball.Pos;
				if (ball.Fresh)
				{
					// A twin born this frame: it starts moving next frame, from where it was split.
					ball.Fresh = false;
					continue;
				}
				ball.Age += dt;
				ball.BoltAge += dt;
				if (ball.Age >= ball.Life)
				{
					ball.Live = false;
					AddSpark(ball.Pos, 10f, SparkKind.Out);
					continue;
				}
				Move(b, ref ball, dt, me, view, crownCount, ref crownIndex);
				if (!ball.Live)
					continue;
				if (ball.Mode != Mode.Hang && nav != null && nav.IsWallAt(ball.Pos))
				{
					// Stone grounds it: the storm-fire goes to earth where it touched.
					ball.Live = false;
					StatGrounded++;
					AddSpark(ball.Pos, 16f, SparkKind.Ground);
					continue;
				}
				if (wakeForm)
					LayWake(ref ball);
			}
			if (crowned && crownCount > 0)
				crownPhase += BaseSpeed * CrownTurnShare / MathF.Max(24f, CrownRadius * MathF.Sqrt(area)) * dt;
		}

		bool wakeTick = false;
		wakeClock += dt;
		if (wakeClock >= WakeTickSeconds)
		{
			wakeClock -= WakeTickSeconds;
			wakeTick = true;
		}
		int wakeCount = AgeWake(dt);
		if (alive && (live > 0 || (wakeTick && wakeCount > 0)))
			Sweep(me, wakeTick && wakeCount > 0);

		bool any = live > 0 || wakeCount > 0;
		for (int s = 0; s < MaxSparks; s++)
		{
			if (!sparks[s].Live)
				continue;
			sparks[s].Age += dt;
			if (sparks[s].Age >= SparkSeconds)
				sparks[s].Live = false;
			else
				any = true;
		}
		if (any || drewLastFrame)
			QueueRedraw();
		drewLastFrame = any;
	}

	// --- flight -----------------------------------------------------------------------------------

	private void Move(int b, ref Ball ball, float dt, Vector2 me, Rect2 view, int crownCount, ref int crownIndex)
	{
		switch (ball.Mode)
		{
			case Mode.Crown:
			{
				float ring = CrownRadius * MathF.Sqrt(area);
				float slot = crownPhase + Mathf.Tau * crownIndex / Math.Max(1, crownCount);
				crownIndex++;
				Vector2 target = me + new Vector2(MathF.Cos(slot), MathF.Sin(slot)) * ring;
				Vector2 to = target - ball.Pos;
				float step = BaseSpeed * CrownCatchUp * dt;
				ball.Pos = to.LengthSquared() <= step * step ? target : ball.Pos + to.Normalized() * step;
				Vector2 moved = ball.Pos - ball.From;
				if (moved.LengthSquared() > 0.01f)
					ball.Vel = moved.Normalized() * BaseSpeed;
				return;
			}
			case Mode.Home:
			{
				Vector2 spot = me + new Vector2(ball.HangOffset, -LanternLift);
				Vector2 to = spot - ball.Pos;
				float step = BaseSpeed * dt;
				if (to.LengthSquared() <= step * step)
				{
					ball.Pos = spot;
					ball.Mode = Mode.Hang;
					ball.ModeClock = 0f;
					StatHangs++;
				}
				else
				{
					ball.Vel = to.Normalized() * BaseSpeed;
					ball.Pos += ball.Vel * dt;
				}
				return;
			}
			case Mode.Hang:
			{
				ball.ModeClock += dt;
				// It hangs over him and goes where he goes, bobbing as a lamp on a mast does.
				ball.Pos = me + new Vector2(ball.HangOffset, -LanternLift + MathF.Sin(clock * 5f + ball.Seed) * 2f);
				if (ball.ModeClock >= LanternHangSeconds)
				{
					float angle = rng.Randf() * Mathf.Tau;
					ball.Vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * BaseSpeed;
					ball.Mode = Mode.Free;
				}
				return;
			}
		}

		ball.Pos += ball.Vel * dt;
		if (!Bounce(ball.Pos, ref ball.Vel, view, out Vector2 at))
			return;
		StatBounces++;
		AddSpark(at, 8f, SparkKind.Bounce);
		bool first = ball.Bounces == 0;
		ball.Bounces++;
		// Storm Lantern: the edge throws it home rather than back. Before the split, so both twins go home.
		if (lanternForm)
			ball.Mode = Mode.Home;
		if (twinForm && first && !ball.Twin)
			Split(b, ref ball);
	}

	/// <summary>
	/// Turns a ball off the edges of <paramref name="view"/>: an axis it has crossed while still heading out flips, and
	/// <paramref name="at"/> is where on the edge it turned. A ball outside but already heading in (the camera ran past
	/// it) is left alone to come back in. True when it turned.
	/// </summary>
	public static bool Bounce(Vector2 pos, ref Vector2 vel, Rect2 view, out Vector2 at)
	{
		at = pos;
		bool turned = false;
		Vector2 lo = view.Position, hi = view.End;
		if (pos.X < lo.X && vel.X < 0f)
		{
			vel.X = -vel.X;
			at.X = lo.X;
			turned = true;
		}
		else if (pos.X > hi.X && vel.X > 0f)
		{
			vel.X = -vel.X;
			at.X = hi.X;
			turned = true;
		}
		if (pos.Y < lo.Y && vel.Y < 0f)
		{
			vel.Y = -vel.Y;
			at.Y = lo.Y;
			turned = true;
		}
		else if (pos.Y > hi.Y && vel.Y > 0f)
		{
			vel.Y = -vel.Y;
			at.Y = hi.Y;
			turned = true;
		}
		return turned;
	}

	/// <summary>The world rectangle a canvas transform shows of a visible canvas rect: the camera's view.</summary>
	public static Rect2 WorldView(Transform2D canvasTransform, Rect2 visible)
	{
		Transform2D toWorld = canvasTransform.AffineInverse();
		Vector2 a = toWorld * visible.Position;
		Vector2 b = toWorld * visible.End;
		return new Rect2(a, b - a).Abs();
	}

	// What the camera shows now. A view that does not hold the wizard (no camera framing him: a headless harness, a frame
	// before the camera has moved) is the canvas over the camera's zoom, centred on him - the same rectangle on screen.
	private Rect2 ViewNow(Vector2 me)
	{
		Viewport viewport = GetViewport();
		if (viewport == null)
			return new Rect2(me - new Vector2(1280f, 720f) / FallbackZoom * 0.5f, new Vector2(1280f, 720f) / FallbackZoom);
		Rect2 visible = viewport.GetVisibleRect();
		Rect2 view = WorldView(viewport.GetCanvasTransform(), visible);
		if (view.Size.X >= 64f && view.Size.Y >= 64f && view.Grow(-16f).HasPoint(me))
			return view;
		Camera2D camera = viewport.GetCamera2D();
		Vector2 zoom = camera != null && camera.Zoom.X > 0.05f && camera.Zoom.Y > 0.05f ? camera.Zoom : new Vector2(FallbackZoom, FallbackZoom);
		Vector2 canvas = visible.Size.X >= 64f ? visible.Size : new Vector2(1280f, 720f);
		Vector2 size = new(canvas.X / zoom.X, canvas.Y / zoom.Y);
		return new Rect2(me - size * 0.5f, size);
	}

	// Twin Fires: the ball turns off the edge as one and leaves it as two, each a little either side of where it was
	// turned, each striking for TwinShare. The twin remembers what its parent struck, so the split never strikes twice.
	private void Split(int b, ref Ball ball)
	{
		Vector2 turned = ball.Vel;
		int twinDamage = Math.Max(1, Mathf.RoundToInt(ball.Damage * TwinShare));
		ball.Vel = turned.Rotated(Mathf.DegToRad(TwinSplitDegrees));
		ball.Damage = twinDamage;
		ball.Twin = true;
		ball.Radius *= 0.85f;
		int slot = Spawn(ball.Pos, turned.Rotated(-Mathf.DegToRad(TwinSplitDegrees)), twinDamage, ball.Life, ball.Age, true, ball.Mode);
		if (slot == b)
			return;
		balls[slot].Fresh = true;
		balls[slot].Bounces = ball.Bounces;
		balls[slot].Radius = ball.Radius;
		balls[slot].HangOffset = ball.HangOffset + 14f;
		Array.Copy(struck, b * HitMemory, struck, slot * HitMemory, HitMemory);
		Array.Copy(struckAt, b * HitMemory, struckAt, slot * HitMemory, HitMemory);
		struckNext[slot] = struckNext[b];
		StatTwins++;
		AddSpark(ball.Pos, 12f, SparkKind.Split);
	}

	private int Spawn(Vector2 at, Vector2 vel, int ballDamage, float ballLife, float age, bool twin, Mode mode)
	{
		int slot = -1;
		for (int i = 0; i < MaxBalls; i++)
		{
			int s = (nextBall + i) % MaxBalls;
			if (!balls[s].Live)
			{
				slot = s;
				break;
			}
		}
		if (slot < 0)
		{
			// Every ball is out: the one nearest its end gives way, and one more or less in a storm is invisible.
			float least = float.MaxValue;
			for (int s = 0; s < MaxBalls; s++)
			{
				float left = balls[s].Life - balls[s].Age;
				if (left < least)
				{
					least = left;
					slot = s;
				}
			}
		}
		nextBall = (slot + 1) % MaxBalls;
		balls[slot] = new Ball
		{
			Pos = at,
			From = at,
			Vel = vel,
			Age = age,
			Life = ballLife,
			Radius = radius,
			Damage = ballDamage,
			Seed = ++seedClock,
			Mode = mode,
			Twin = twin,
			Live = true,
		};
		int row = slot * HitMemory;
		for (int k = 0; k < HitMemory; k++)
		{
			struck[row + k] = null;
			struckAt[row + k] = float.NegativeInfinity;
		}
		struckNext[slot] = 0;
		return slot;
	}

	// --- Saint's Crown ----------------------------------------------------------------------------

	private void UpdateCrown(float dt)
	{
		if (!crownForm)
		{
			if (crowned)
				ReleaseCrown();
			return;
		}
		crownHeld += dt;
		if (!crowned && lastCrowd >= CrowdEnter)
		{
			crowned = true;
			crownHeld = 0f;
			StatCrowns++;
			for (int b = 0; b < MaxBalls; b++)
				if (balls[b].Live)
					balls[b].Mode = Mode.Crown;
		}
		else if (crowned && crownHeld >= CrownHoldSeconds && lastCrowd < CrowdLeave)
			ReleaseCrown();
	}

	// The crowd has thinned: every crowned ball is flung off along the ring the way it was turning.
	private void ReleaseCrown()
	{
		crowned = false;
		Vector2 me = lastMe;
		for (int b = 0; b < MaxBalls; b++)
		{
			if (!balls[b].Live || balls[b].Mode != Mode.Crown)
				continue;
			Vector2 out_ = balls[b].Pos - me;
			Vector2 tangent = out_.LengthSquared() > 0.01f ? new Vector2(-out_.Y, out_.X).Normalized() : Vector2.Right;
			balls[b].Vel = (tangent + out_.Normalized() * 0.35f).Normalized() * BaseSpeed;
			balls[b].Mode = Mode.Free;
		}
	}

	// --- Static Wake ------------------------------------------------------------------------------

	// Cells of static laid every WakeSpacing along the path the ball flew this frame - at most four a frame, so a long
	// frame in a fast harness lays a dotted wake rather than a flood.
	private void LayWake(ref Ball ball)
	{
		Vector2 path = ball.Pos - ball.From;
		float length = path.Length();
		if (length <= 0.001f)
			return;
		ball.WakeTravel += length;
		int laid = 0;
		int wakeDamage = Math.Max(1, Mathf.RoundToInt(ball.Damage * WakeShare));
		while (ball.WakeTravel >= WakeSpacing && laid < 4)
		{
			ball.WakeTravel -= WakeSpacing;
			Vector2 at = ball.Pos - path / length * MathF.Min(ball.WakeTravel, length);
			AddWake(at, wakeDamage);
			laid++;
		}
		if (ball.WakeTravel > WakeSpacing)
			ball.WakeTravel = WakeSpacing;
	}

	private void AddWake(Vector2 at, int wakeDamage)
	{
		int slot = -1;
		for (int i = 0; i < MaxWake; i++)
		{
			int s = (nextWake + i) % MaxWake;
			if (!wakes[s].Live)
			{
				slot = s;
				break;
			}
		}
		if (slot < 0)
			slot = nextWake;
		nextWake = (slot + 1) % MaxWake;
		wakes[slot] = new Wake { At = at, Damage = wakeDamage, Seed = ++seedClock, Live = true };
	}

	private int AgeWake(float dt)
	{
		int n = 0;
		for (int w = 0; w < MaxWake; w++)
		{
			if (!wakes[w].Live)
				continue;
			wakes[w].Age += dt;
			if (wakes[w].Age >= WakeSeconds)
			{
				wakes[w].Live = false;
				continue;
			}
			wakeLive[n++] = w;
		}
		return n;
	}

	// --- the strike -------------------------------------------------------------------------------

	// The one walk of the swarm a frame: every body's position read once, tested against every live ball's swept path,
	// against the wake on its tick, and counted for Saint's Crown's crowd.
	private void Sweep(Vector2 me, bool wakeTick)
	{
		int active = 0;
		for (int b = 0; b < MaxBalls; b++)
		{
			if (!balls[b].Live)
				continue;
			float reach = balls[b].Radius + Body;
			Vector2 pad = new(reach, reach);
			sweepBall[active] = b;
			sweepLo[active] = new Vector2(MathF.Min(balls[b].From.X, balls[b].Pos.X), MathF.Min(balls[b].From.Y, balls[b].Pos.Y)) - pad;
			sweepHi[active] = new Vector2(MathF.Max(balls[b].From.X, balls[b].Pos.X), MathF.Max(balls[b].From.Y, balls[b].Pos.Y)) + pad;
			active++;
		}

		int wakeCount = 0;
		Vector2 wakeLo = default, wakeHi = default;
		float wakeReach = WakeReach * MathF.Sqrt(area) + Body * 0.5f;
		if (wakeTick)
		{
			wakeCount = 0;
			for (int w = 0; w < MaxWake; w++)
			{
				if (!wakes[w].Live)
					continue;
				Vector2 at = wakes[w].At;
				if (wakeCount == 0)
				{
					wakeLo = at;
					wakeHi = at;
				}
				else
				{
					wakeLo = new Vector2(MathF.Min(wakeLo.X, at.X), MathF.Min(wakeLo.Y, at.Y));
					wakeHi = new Vector2(MathF.Max(wakeHi.X, at.X), MathF.Max(wakeHi.Y, at.Y));
				}
				wakeLive[wakeCount++] = w;
			}
			wakeLo -= new Vector2(wakeReach, wakeReach);
			wakeHi += new Vector2(wakeReach, wakeReach);
		}

		bool countCrowd = crownForm && active > 0;
		float crowdSquared = CrowdReach * CrowdReach;
		float wakeSquared = wakeReach * wakeReach;
		int crowd = 0;
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			Vector2 e = enemy.GlobalPosition;
			if (countCrowd && e.DistanceSquaredTo(me) <= crowdSquared)
				crowd++;
			for (int i = 0; i < active; i++)
			{
				if (e.X < sweepLo[i].X || e.Y < sweepLo[i].Y || e.X > sweepHi[i].X || e.Y > sweepHi[i].Y)
					continue;
				int b = sweepBall[i];
				ref Ball ball = ref balls[b];
				if (!ball.Live)
					continue;
				float reach = ball.Radius + Body;
				if (DistanceSquaredToSegment(e, ball.From, ball.Pos) > reach * reach || StruckLately(b, enemy))
					continue;
				NoteStrike(b, enemy);
				AddBolt(b, ref ball, e);
				StatStrikes++;
				PlayerRef.DealDamageToEnemy(enemy, ball.Damage, 0f, true, SpellData, null, ball.Pos);
				if (!IsInstanceValid(enemy) || enemy.IsDying)
					break;
			}
			if (wakeCount == 0 || !IsInstanceValid(enemy) || enemy.IsDying
				|| e.X < wakeLo.X || e.Y < wakeLo.Y || e.X > wakeHi.X || e.Y > wakeHi.Y)
				continue;
			for (int k = 0; k < wakeCount; k++)
			{
				ref Wake cell = ref wakes[wakeLive[k]];
				if (e.DistanceSquaredTo(cell.At) > wakeSquared)
					continue;
				// One zap a tick, however many cells it stands in. The wake's ticks are many and small, so they set off
				// no elemental chain of their own; the ball's strike does.
				StatWakeStrikes++;
				PlayerRef.DealDamageToEnemy(enemy, cell.Damage, 0f, false, SpellData, null, cell.At);
				break;
			}
		}
		if (countCrowd)
			lastCrowd = crowd;
		else if (!crownForm)
			lastCrowd = 0;
	}

	/// <summary>The squared distance from <paramref name="p"/> to the segment from <paramref name="a"/> to <paramref name="b"/>.</summary>
	public static float DistanceSquaredToSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float lengthSquared = ab.LengthSquared();
		if (lengthSquared < 0.0001f)
			return p.DistanceSquaredTo(a);
		float t = Mathf.Clamp((p - a).Dot(ab) / lengthSquared, 0f, 1f);
		return p.DistanceSquaredTo(a + ab * t);
	}

	private bool StruckLately(int b, Enemy enemy)
	{
		int row = b * HitMemory;
		float since = clock - StrikeEvery;
		for (int k = 0; k < HitMemory; k++)
		{
			if (ReferenceEquals(struck[row + k], enemy) && struckAt[row + k] > since)
				return true;
		}
		return false;
	}

	private void NoteStrike(int b, Enemy enemy)
	{
		int at = b * HitMemory + struckNext[b];
		struck[at] = enemy;
		struckAt[at] = clock;
		struckNext[b] = (struckNext[b] + 1) % HitMemory;
	}

	private void AddBolt(int b, ref Ball ball, Vector2 to)
	{
		if (ball.BoltAge >= BoltSeconds)
			ball.Bolts = 0;
		if (ball.Bolts < BoltsPerBall)
			boltEnds[b * BoltsPerBall + ball.Bolts++] = to;
		ball.BoltAge = 0f;
		ball.BoltSeed = ++seedClock;
	}

	private void AddSpark(Vector2 at, float size, SparkKind kind)
	{
		int slot = -1;
		for (int i = 0; i < MaxSparks; i++)
		{
			int s = (nextSpark + i) % MaxSparks;
			if (!sparks[s].Live)
			{
				slot = s;
				break;
			}
		}
		if (slot < 0)
			slot = nextSpark;
		nextSpark = (slot + 1) % MaxSparks;
		sparks[slot] = new Spark { At = at, Size = size, Kind = kind, Seed = ++seedClock, Live = true };
	}

	// --- the drawing ------------------------------------------------------------------------------

	public override void _Draw()
	{
		uint flicker = (uint)(clock / FlickerSeconds);
		DrawWake(flicker);
		for (int b = 0; b < MaxBalls; b++)
		{
			if (balls[b].Live)
				DrawBall(b, balls[b], flicker);
		}
		DrawSparks();
	}

	private Color Ink(Color c, float coverage = 1f) => SpellInk.Ink(c, tint, coverage * tint.A);

	// A ball of storm-fire: a glow, three tongues streaming back against its travel, a rim of the ramp's edge, a pale
	// shell, a white-hot heart a step ahead of its middle, and crackling spurs. The bolts of its last strike first, so the
	// ball sits on them.
	private void DrawBall(int b, in Ball ball, uint flicker)
	{
		float coverage = MathF.Min(1f, ball.Age / FadeInSeconds) * MathF.Min(1f, (ball.Life - ball.Age) / FadeOutSeconds);
		if (coverage <= 0f)
			return;
		Vector2 at = SpellInk.Snap(ball.Pos);
		Vector2 heading = ball.Vel.LengthSquared() > 1f ? ball.Vel.Normalized() : Vector2.Up;
		Vector2 side = new(-heading.Y, heading.X);
		float r = ball.Radius;
		uint f = flicker + ball.Seed * 7u;
		bool pulse = f % 3 == 0;

		if (ball.Bolts > 0 && ball.BoltAge < BoltSeconds)
		{
			float fade = 1f - ball.BoltAge / BoltSeconds;
			for (int k = 0; k < ball.Bolts; k++)
				DrawBolt(at, boltEnds[b * BoltsPerBall + k], ball.BoltSeed + (uint)k * 31u, Ink(ramp.Core, fade), Ink(ramp.Mid, fade));
		}

		Disc(at, r + 4f + (pulse ? 2f : 0f), Ink(ramp.Mid, 0.3f * coverage));
		for (int k = -1; k <= 1; k++)
		{
			float wobble = (Hash(f, k + 5) - 0.5f) * 0.5f;
			Vector2 dir = (-heading + side * (k * 0.5f + wobble)).Normalized();
			float length = r * (k == 0 ? 1.6f : 1.05f) * (0.75f + 0.5f * Hash(f, k + 9));
			int steps = Math.Max(2, (int)(length / (Grid * 2f)));
			for (int s = 0; s < steps; s++)
			{
				float u = (s + 1f) / steps;
				Vector2 p = at + dir * (r * 0.5f + length * u);
				float size = WhipLimb.SnapSize((1f - u * 0.8f) * r * 0.75f);
				Color tone = u < 0.35f ? ramp.Hot : u < 0.7f ? ramp.Mid : ramp.Edge;
				DrawRect(WhipLimb.Cell(p, size), Ink(tone, coverage));
			}
		}
		Disc(at, r, Ink(ramp.Edge, coverage));
		Disc(at, r - Grid, Ink(ramp.Mid, coverage));
		Disc(at, r - Grid * 2f, Ink(ramp.Hot, coverage));
		Disc(SpellInk.Snap(at + heading * Grid), MathF.Max(Grid * 1.5f, r * (pulse ? 0.55f : 0.45f)), Ink(ramp.Core, coverage));

		// The crackle: three jagged spurs off the shell, a fresh set every flicker, reaching half the ball again.
		Color spur = Ink(ramp.Hot, coverage);
		Color spurTip = Ink(ramp.Mid, coverage);
		for (int k = 0; k < 3; k++)
		{
			float angle = (Hash(f, k) * 0.8f + k) * Mathf.Tau / 3f;
			Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
			Vector2 across = new(-dir.Y, dir.X);
			float length = r * (1.35f + 0.5f * Hash(f, k + 11));
			Vector2 mid = at + dir * (r + (length - r) * 0.5f) + across * ((Hash(f, k + 23) - 0.5f) * 8f);
			DrawLine(SpellInk.Snap(at + dir * (r - Grid)), SpellInk.Snap(mid), spur, 2f);
			DrawLine(SpellInk.Snap(mid), SpellInk.Snap(at + dir * length), spurTip, 2f);
		}
	}

	// A disc of cells on the 2 px grid, row by row.
	private void Disc(Vector2 centre, float r, Color color)
	{
		int n = Math.Max(1, Mathf.RoundToInt(r / Grid));
		for (int y = -n; y < n; y++)
		{
			float yc = y + 0.5f;
			int half = Mathf.RoundToInt(MathF.Sqrt(MathF.Max(0f, n * n - yc * yc)));
			if (half <= 0)
				continue;
			DrawRect(new Rect2(centre.X - half * Grid, centre.Y + y * Grid, half * 2 * Grid, Grid), color);
		}
	}

	// A jagged bolt from a to b in three runs, its kinks off the line a few units either way (Stormsweep's).
	private void DrawBolt(Vector2 a, Vector2 b, uint seed, Color core, Color glow)
	{
		Vector2 d = b - a;
		if (d.LengthSquared() < 4f)
			return;
		Vector2 side = new Vector2(-d.Y, d.X).Normalized();
		Vector2 p1 = a + d * 0.33f + side * ((Hash(seed, 1) - 0.5f) * 10f);
		Vector2 p2 = a + d * 0.66f + side * ((Hash(seed, 2) - 0.5f) * 10f);
		DrawLine(SpellInk.Snap(a), SpellInk.Snap(p1), glow, 4f);
		DrawLine(SpellInk.Snap(p1), SpellInk.Snap(p2), glow, 4f);
		DrawLine(SpellInk.Snap(p2), SpellInk.Snap(b), glow, 4f);
		DrawLine(SpellInk.Snap(a), SpellInk.Snap(p1), core, 2f);
		DrawLine(SpellInk.Snap(p1), SpellInk.Snap(p2), core, 2f);
		DrawLine(SpellInk.Snap(p2), SpellInk.Snap(b), core, 2f);
	}

	// The wake: every cell a kinked stroke of static and a hot point, re-chosen every flicker, thinning out as it dies.
	private void DrawWake(uint flicker)
	{
		for (int w = 0; w < MaxWake; w++)
		{
			if (!wakes[w].Live)
				continue;
			Wake cell = wakes[w];
			float u = cell.Age / WakeSeconds;
			float coverage = 1f - u * u;
			uint f = flicker + cell.Seed * 13u;
			float angle = Hash(f, 1) * Mathf.Tau;
			Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
			Vector2 across = new(-dir.Y, dir.X);
			float half = 5f + 3f * Hash(f, 2);
			Vector2 a = cell.At - dir * half;
			Vector2 b = cell.At + dir * half;
			Vector2 kink = cell.At + across * ((Hash(f, 3) - 0.5f) * 8f);
			Color hot = Ink(u < 0.4f ? ramp.Hot : ramp.Mid, coverage);
			Color edge = Ink(ramp.Edge, coverage);
			DrawLine(SpellInk.Snap(a), SpellInk.Snap(kink), hot, 2f);
			DrawLine(SpellInk.Snap(kink), SpellInk.Snap(b), edge, 2f);
			if ((f & 1u) == 0u)
				DrawRect(WhipLimb.Cell(kink, Grid), Ink(ramp.Core, coverage));
		}
	}

	// A crack where a ball turned off an edge, went to ground, burnt out or split: a white heart and four arms, square or
	// diagonal by its seed, shrinking over its life, and a broken ring opening out to its size.
	private static readonly Vector2[] Arms =
	{
		new(1f, 0f), new(0f, 1f), new(-1f, 0f), new(0f, -1f),
		new(0.7071f, 0.7071f), new(-0.7071f, 0.7071f), new(-0.7071f, -0.7071f), new(0.7071f, -0.7071f),
	};

	private void DrawSparks()
	{
		for (int s = 0; s < MaxSparks; s++)
		{
			if (!sparks[s].Live)
				continue;
			Spark spark = sparks[s];
			float u = Mathf.Clamp(spark.Age / SparkSeconds, 0f, 1f);
			float coverage = 1f - u;
			Vector2 at = SpellInk.Snap(spark.At);
			DrawRect(WhipLimb.Cell(at, u < 0.35f ? 6f : 4f), Ink(ramp.Core, coverage));
			float arm = spark.Size * (u < 0.4f ? 1f : 0.6f);
			int first = (spark.Seed & 1u) == 1u ? 4 : 0;
			Color hot = Ink(u < 0.5f ? ramp.Core : ramp.Hot, coverage);
			for (int a = 0; a < 4; a++)
			{
				Vector2 dir = Arms[first + a];
				for (float r = 4f; r <= arm; r += Grid * 2f)
					DrawRect(WhipLimb.Cell(at + dir * r, Grid), hot);
			}
			if (spark.Kind == SparkKind.Out)
				continue;
			// The ring: eight beats of light opening out, every other one dropping out halfway.
			float ring = Mathf.Lerp(4f, spark.Size + 4f, u);
			Color shock = Ink(spark.Kind == SparkKind.Ground ? ramp.Edge : ramp.Mid, coverage);
			for (int a = 0; a < 8; a++)
			{
				if (u > 0.5f && a % 2 == 1)
					continue;
				float angle = a * Mathf.Tau / 8f + 0.39f;
				DrawRect(WhipLimb.Cell(at + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * ring, Grid), shock);
			}
		}
	}

	// A small integer hash to 0..1, so the flicker needs no stored state (Stormsweep's, ImpactDebris's).
	private static float Hash(uint seed, int i)
	{
		uint h = (uint)i * 374761393u + seed * 668265263u;
		h = (h ^ (h >> 13)) * 1274126177u;
		h ^= h >> 16;
		return (h & 0xFFFFFF) / 16777216f;
	}
}
