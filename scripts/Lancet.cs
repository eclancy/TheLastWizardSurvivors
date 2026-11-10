using Godot;
using System;

namespace TheOneHeMissed.scripts;

// LANCET (Metal + Poison), built 2026-11-09. Eric approved it from .ai/character-fit.md's queue: "a steel needle that always
// flies at the most wounded enemy in reach (by share of health lost; the nearest when none is hurt); a fast timer; hits
// harder the more health its target has lost, up to double, and an ordinary enemy below a small sliver dies outright; one
// needle at level 1, more as it levels, each to the next most wounded." Nobody's opener: a poison build's finisher.
//
// IT AIMS BY HEALTH. The only spell that chooses its target by how hurt it is (SpellTargetingMode.Wounded): the share of
// its health a body has lost, the most first, and between bodies equally hurt - every unhurt one among them - the nearest.
// So with nothing hurt in reach it is a nearest-enemy spell, and the moment anything else in the loadout wounds the crowd it
// goes where the wounds are. Hunter's Mark marks the STRONGEST in reach; this is the other end of the same line.
//
// THE CHOICE IS ONE WALK. A cast walks EnemyRegistry.Snapshot once (Rank): every body in reach is weighed and the best
// MaxRank kept in fixed arrays by insertion, so a body worse than the last kept costs one compare and nothing is allocated
// (.ai/performance.md). The cast's needles take the top of that list, one body each - each extra needle goes to the next
// most wounded, a distinct target - and when the crowd in reach is thinner than the volley the rest go round the list again,
// but only at a body the needles already on their way to it could leave standing (a lone boss still takes every needle; a
// skeleton the first needle kills is not sent a second, which the first harness run did and saw a fifth of its needles
// fall on nothing). The same list is what a needle turns to when
// its quarry dies before it lands, and what Triage sends a killing needle on to: the next on it that no needle in flight is
// already going for. Nothing else ever walks the swarm.
//
// THE PAYLOAD. A needle homes on its body and strikes it once, for its damage times one plus the share of health the body
// had lost (WoundBonus): a fresh body takes the needle's damage, one at half health half again, one nearly dead almost
// double. An ORDINARY body below the sliver (SliverShare, a tenth of its health) dies outright: the needle strikes
// for at least what it has left. Never an elite, a miniboss or a boss (IsMiniBoss, and BossEnemy whatever it says): those
// take only the bonus, as the execute rule says (Player.DealDamageToEnemy's Reaper's Scythe spares the bosses for the same
// reason). Every strike goes through Player.DealDamageToEnemy with the spell as its source, so a kill is counted, numbered
// in the spell's ink and charged to it like every other spell's, and a crit, a rider (Bodkin's Wither) and the tiers ride it.
//
// SPELLS START SMALL (Eric, 2026-11-02): one needle a cast at level 1, two at 3, three at 6 (SpellData_Lancet.tres).
//
// THE DRAWING. One node in the arena at the origin, drawing in world space on the 2 px grid, on DrawLayers.Air, which the
// light pool never darkens. A needle is a thin line of steel - a white-hot point, steel's hot and mid down the shaft over a
// flank of its edge on the side away from the key light, the eye in the edge - with a bead of poison just behind the point
// and a short dithered streak of steel behind it, so it reads as fast and thin. A strike is small and clean: a steel glint,
// the needle standing a moment in the body, two drops of poison flicked on. An outright kill is the moment: a long white
// cut drawn through the body along the needle's line, a starburst and a ring of steel opening round it. Bloodletting's
// release is a fan of poison spraying out of the wound. Every fade is a dither (SpellInk.DitherByCell). Metal and Poison
// are not warm (LightPool.IsWarm): a needle lights nothing in the dark.
//
// THE MILESTONES (SpellEvolutionCatalog.SetupLancetEvolutions):
//   Bloodletting   (level 4, +1 Poison) - a needle that lances a poisoned body lets out all its remaining poison at once:
//                  Enemy.RemainingPoisonDamage, dealt as one hit numbered in Poison's ink, then Enemy.EndPoison.
//   Twin Lancets   (level 4) - one more needle a cast.
//   Bodkin         (level 4, +1 Metal) - the needle is a bodkin point: its first strike on a body Withers a share of its
//                  max health (Metal's rider, through the house path) - and a withered body counts as wounded.
//   Triage         (level 8) - a needle that kills goes on to the next most wounded.
//   Surgeon's Hand (level 8) - the outright kill takes an ordinary body at a larger sliver (SurgeonsSliverShare).
public partial class Lancet : Node2D
{
	public const string SpellId = "lancet";

	// Milestone ids. SpellEvolutionCatalog builds the cards from these; this node reads them.
	public const string BloodlettingId = "lancet_bloodletting";
	public const string TwinLancetsId = "lancet_twin_lancets";
	public const string BodkinId = "lancet_bodkin";
	public const string TriageId = "lancet_triage";
	public const string SurgeonsHandId = "lancet_surgeons_hand";

	// --- the payload ------------------------------------------------------------------------------
	/// <summary>How much harder a needle strikes a body that has lost all its health: 1 is double. Linear in the share lost.</summary>
	public const float WoundBonus = 1f;
	/// <summary>Below this share of its health an ordinary enemy dies to a needle outright.</summary>
	public const float SliverShare = 0.10f;
	/// <summary>Surgeon's Hand's sliver.</summary>
	public const float SurgeonsSliverShare = 0.20f;
	/// <summary>Bodkin's Wither: the share of a body's max health its first strike takes for good (ElementPaths' Metal rider).</summary>
	public const float BodkinWither = 0.05f;
	/// <summary>The most needles one cast throws, Amount and all.</summary>
	public const int MaxPerCast = 8;
	/// <summary>How many bodies a cast's walk keeps, best first: its needles' targets, then the ones a needle may turn to.</summary>
	public const int MaxRank = 16;
	/// <summary>Two wound shares closer than this are the same wound, and the nearer body goes first.</summary>
	public const float WoundTie = 0.001f;

	// --- the needle -------------------------------------------------------------------------------
	private const int MaxNeedles = 48;
	private const int MaxFx = 96;
	private const float LegSeconds = 1.4f;
	private const float StrikeAllowance = 6f;
	private const float LaunchSpread = 7f;
	private const float TrailLength = 26f;
	private const float HitSeconds = 0.16f;
	private const float OutrightSeconds = 0.34f;
	private const float BloodletSeconds = 0.38f;
	private const float FizzleSeconds = 0.14f;
	private const float Grid = SpellInk.Grid;

	// --- tuned in scenes/Lancet.tscn --------------------------------------------------------------
	[Export] public float Speed { get; set; } = 860f;
	[Export] public float NeedleLength { get; set; } = 20f;

	private struct Needle
	{
		public Enemy Target;
		public Vector2 Pos;
		public Vector2 Aim;
		public Vector2 Heading;
		public Vector2 LegFrom;
		public float LegAge;
		public float Age;
		public int Damage;
		public int Hops;
		public uint Seed;
		public bool Retargeted;
		public bool Live;
	}

	private enum FxKind : byte { Hit, Outright, Bloodlet, Fizzle }

	private struct Fx { public Vector2 At; public Vector2 Dir; public float Age; public uint Seed; public FxKind Kind; public bool Live; }

	public Player PlayerRef { get; set; }
	public SpellData SpellData { get; private set; }

	// For the harness (_BalanceHarness's LANCET line). Process-wide, as Corposant's are.
	public static int StatCasts { get; set; }
	public static int StatThrown { get; set; }
	public static int StatStrikes { get; set; }
	public static int StatWoundedStrikes { get; set; }
	public static int StatKills { get; set; }
	public static int StatOutright { get; set; }
	public static int StatReleases { get; set; }
	public static long StatReleased { get; set; }
	public static int StatTriage { get; set; }
	public static int StatRetargets { get; set; }
	public static int StatFizzles { get; set; }
	/// <summary>The wound shares of every strike summed, so the harness can say how hurt the average needle's body was.</summary>
	public static double StatWoundSum { get; set; }

	private int damage = 5;
	private float reach = 300f;
	private float sliver = SliverShare;
	private Color tint = Colors.White;
	private bool bloodletting, triage;
	private readonly ElementColors.Ramp steel = ElementColors.GetRamp(Element.Metal);
	private readonly ElementColors.Ramp poison = ElementColors.GetRamp(Element.Poison);

	private readonly Needle[] needles = new Needle[MaxNeedles];
	private readonly Fx[] fx = new Fx[MaxFx];
	// The last cast's walk: the best MaxRank bodies in reach, most wounded first.
	private readonly Enemy[] rank = new Enemy[MaxRank];
	private readonly float[] rankWound = new float[MaxRank];
	private readonly float[] rankDistance = new float[MaxRank];
	// What this cast's needles are already carrying to each body on the list, so a volley larger than the crowd is never
	// spent twice on a body the first needle kills.
	private readonly int[] rankSent = new int[MaxRank];
	private int rankCount;
	private int nextNeedle, nextFx;
	private uint seedClock;
	private float clock;
	private bool drewLastFrame;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		Material = SpellInk.DitherByCell;
	}

	/// <summary>The cast's numbers, read again on every cast as Corposant's are; needles in flight keep their own damage.</summary>
	public void Refresh(SpellData spell, float damageMultiplier, Color visualTint, float castReach)
	{
		if (spell == null)
			return;
		SpellData = spell;
		int level = Math.Max(1, spell.CurrentLevel);
		damage = Math.Max(1, Mathf.RoundToInt(spell.GetDamageAtLevel(level) * damageMultiplier));
		reach = MathF.Max(16f, castReach);
		sliver = SliverAt(spell);
		tint = visualTint;
		bloodletting = spell.HasEvolution(BloodlettingId);
		triage = spell.HasEvolution(TriageId);
	}

	/// <summary>The share of its health under which an ordinary enemy dies to this spell's needle outright.</summary>
	public static float SliverAt(SpellData spell) =>
		spell != null && spell.HasEvolution(SurgeonsHandId) ? SurgeonsSliverShare : SliverShare;

	/// <summary>How much a needle of <paramref name="baseDamage"/> strikes a body that has lost <paramref name="wound"/> of its health for.</summary>
	public static int WoundedDamage(int baseDamage, float wound) =>
		Math.Max(1, Mathf.RoundToInt(baseDamage * (1f + WoundBonus * Mathf.Clamp(wound, 0f, 1f))));

	/// <summary>
	/// Whether a needle kills this body outright: an ordinary enemy - never an elite, a miniboss or a boss - below
	/// <paramref name="sliverShare"/> of its health and not already dead.
	/// </summary>
	public static bool KillsOutright(bool isMiniBoss, bool isBoss, float healthFraction, float sliverShare) =>
		!isMiniBoss && !isBoss && healthFraction > 0f && healthFraction < sliverShare;

	/// <summary>
	/// Whether body A goes before body B: more wounded first, and between wounds closer than <see cref="WoundTie"/> - every
	/// unhurt body among them - the nearer. The one ordering the walk, the needles and the checks all use.
	/// </summary>
	public static bool Before(float woundA, float distanceA, float woundB, float distanceB) =>
		woundA > woundB + WoundTie || (MathF.Abs(woundA - woundB) <= WoundTie && distanceA < distanceB);

	/// <summary>
	/// Throws up to <paramref name="count"/> needles from the wizard at the most wounded in reach, one body each. When the
	/// crowd in reach is thinner than the volley the rest go round the list again - but only at a body the needles already
	/// sent at it could leave standing, most wounded first, so a lone boss takes every needle and a dying skeleton one. Returns
	/// how many left; none when nothing is in reach, so the fire loop keeps the cast ripe for when something is.
	/// </summary>
	public int Cast(int count)
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef) || SpellData == null)
			return 0;
		Vector2 me = PlayerRef.GlobalPosition;
		Rank(me, reach);
		if (rankCount == 0)
			return 0;
		count = Math.Clamp(count, 1, MaxPerCast);
		int thrown = 0;
		for (int k = 0; k < count; k++)
		{
			int r = k;
			if (k >= rankCount)
			{
				// Round again: the first on the list that what is already on its way may not kill. A crit or a rider may
				// still finish it - this is the needles' own reckoning, not a promise.
				r = -1;
				for (int i = 0; i < rankCount && r < 0; i++)
				{
					if (rank[i].Health > rankSent[i])
						r = i;
				}
				if (r < 0)
					break;
			}
			Enemy target = rank[r];
			rankSent[r] += KillsOutright(target.IsMiniBoss, target is BossEnemy, 1f - rankWound[r], sliver)
				? target.Health
				: WoundedDamage(damage, rankWound[r]);
			thrown++;
			Vector2 to = target.GlobalPosition;
			// A little fan off the hand, so two needles at two bodies the same way never leave as one line.
			Vector2 heading = (to - me).LengthSquared() > 1f ? (to - me).Normalized() : Vector2.Right;
			Vector2 side = new(-heading.Y, heading.X);
			Vector2 from = me + side * ((k - (count - 1) * 0.5f) * LaunchSpread);
			Spawn(from, target, heading);
		}
		StatCasts++;
		StatThrown += thrown;
		return thrown;
	}

	// THE ONE WALK: every body in reach weighed by the share of health it has lost, the best MaxRank kept, most wounded first
	// (Before). Dying, untargetable and held bodies are passed over: a needle at any of them would strike nothing.
	private void Rank(Vector2 me, float within)
	{
		rankCount = 0;
		float withinSquared = within * within;
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying || !enemy.IsTargetable || enemy.IsHeld)
				continue;
			float d = enemy.GlobalPosition.DistanceSquaredTo(me);
			if (d > withinSquared)
				continue;
			float wound = 1f - enemy.HealthFraction;
			if (rankCount == MaxRank && !Before(wound, d, rankWound[MaxRank - 1], rankDistance[MaxRank - 1]))
				continue;
			int at = Math.Min(rankCount, MaxRank - 1);
			while (at > 0 && Before(wound, d, rankWound[at - 1], rankDistance[at - 1]))
			{
				rank[at] = rank[at - 1];
				rankWound[at] = rankWound[at - 1];
				rankDistance[at] = rankDistance[at - 1];
				at--;
			}
			rank[at] = enemy;
			rankWound[at] = wound;
			rankDistance[at] = d;
			if (rankCount < MaxRank)
				rankCount++;
		}
		Array.Clear(rankSent, 0, MaxRank);
	}

	/// <summary>
	/// The ordering a cast's walk keeps, applied to plain numbers for RegressionChecks: the indices of
	/// <paramref name="wounds"/> and <paramref name="distances"/> a cast of <paramref name="count"/> needles would throw at,
	/// in throwing order, written to <paramref name="into"/>. Returns how many it wrote (never more than there are bodies).
	/// </summary>
	public static int Order(float[] wounds, float[] distances, int count, int[] into)
	{
		int n = Math.Min(wounds.Length, distances.Length);
		int taken = 0;
		Span<bool> used = stackalloc bool[Math.Max(1, n)];
		while (taken < count && taken < n && taken < into.Length)
		{
			int best = -1;
			for (int i = 0; i < n; i++)
			{
				if (used[i])
					continue;
				if (best < 0 || Before(wounds[i], distances[i], wounds[best], distances[best]))
					best = i;
			}
			used[best] = true;
			into[taken++] = best;
		}
		return taken;
	}

	private void Spawn(Vector2 from, Enemy target, Vector2 heading)
	{
		int slot = -1;
		for (int i = 0; i < MaxNeedles; i++)
		{
			int s = (nextNeedle + i) % MaxNeedles;
			if (!needles[s].Live)
			{
				slot = s;
				break;
			}
		}
		// Every needle is out (a storm of casts): the oldest gives way.
		if (slot < 0)
		{
			float oldest = -1f;
			for (int s = 0; s < MaxNeedles; s++)
			{
				if (needles[s].Age > oldest)
				{
					oldest = needles[s].Age;
					slot = s;
				}
			}
		}
		nextNeedle = (slot + 1) % MaxNeedles;
		needles[slot] = new Needle
		{
			Target = target,
			Pos = from,
			Aim = target.GlobalPosition,
			Heading = heading,
			LegFrom = from,
			Damage = damage,
			Seed = ++seedClock,
			Live = true,
		};
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (dt <= 0f)
			return;
		clock += dt;
		bool any = false;
		for (int i = 0; i < MaxNeedles; i++)
		{
			if (!needles[i].Live)
				continue;
			Fly(ref needles[i], dt);
			if (needles[i].Live)
				any = true;
		}
		for (int i = 0; i < MaxFx; i++)
		{
			if (!fx[i].Live)
				continue;
			fx[i].Age += dt;
			if (fx[i].Age >= FxSeconds(fx[i].Kind))
				fx[i].Live = false;
			else
				any = true;
		}
		if (any || drewLastFrame)
			QueueRedraw();
		drewLastFrame = any;
	}

	private static float FxSeconds(FxKind kind) => kind switch
	{
		FxKind.Outright => OutrightSeconds,
		FxKind.Bloodlet => BloodletSeconds,
		FxKind.Fizzle => FizzleSeconds,
		_ => HitSeconds,
	};

	// --- flight -----------------------------------------------------------------------------------

	private void Fly(ref Needle needle, float dt)
	{
		needle.Age += dt;
		needle.LegAge += dt;
		if (needle.Target != null && !Strikable(needle.Target))
		{
			// Its body died or went out of reach of anything (another spell's kill, a burrow): it turns once to the next most
			// wounded on the cast's list that no needle is already going for, or flies on to where it was and falls.
			needle.Target = null;
			if (!needle.Retargeted)
			{
				needle.Retargeted = true;
				Enemy next = NextOnList(needle.Pos);
				if (next != null)
				{
					needle.Target = next;
					StatRetargets++;
				}
			}
		}
		if (needle.Target != null)
			needle.Aim = needle.Target.GlobalPosition;
		Vector2 to = needle.Aim - needle.Pos;
		float distance = to.Length();
		float step = Speed * dt;
		if (distance > 0.01f)
			needle.Heading = to / distance;
		if (distance <= step + StrikeAllowance || needle.LegAge >= LegSeconds)
		{
			needle.Pos = needle.Aim;
			if (needle.Target != null && needle.LegAge < LegSeconds)
				Strike(ref needle);
			else
			{
				StatFizzles++;
				AddFx(needle.Pos, needle.Heading, FxKind.Fizzle);
				needle.Live = false;
				needle.Target = null;
			}
			return;
		}
		needle.Pos += needle.Heading * step;
	}

	private static bool Strikable(Enemy enemy) =>
		enemy != null && IsInstanceValid(enemy) && !enemy.IsDying && enemy.IsTargetable && !enemy.IsHeld;

	// The next body on the last cast's list that is still there, still in the wizard's reach, and that no needle in flight is
	// already going for: the most wounded the cast saw that nothing has claimed. The list is at most a cast old.
	private Enemy NextOnList(Vector2 from)
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return null;
		Vector2 me = PlayerRef.GlobalPosition;
		float withinSquared = reach * reach;
		for (int r = 0; r < rankCount; r++)
		{
			Enemy candidate = rank[r];
			if (!Strikable(candidate) || candidate.GlobalPosition.DistanceSquaredTo(me) > withinSquared)
				continue;
			bool claimed = false;
			for (int i = 0; i < MaxNeedles && !claimed; i++)
				claimed = needles[i].Live && ReferenceEquals(needles[i].Target, candidate);
			if (!claimed)
				return candidate;
		}
		return null;
	}

	// --- the strike -------------------------------------------------------------------------------

	private void Strike(ref Needle needle)
	{
		Enemy enemy = needle.Target;
		Vector2 at = needle.Pos;
		float fraction = enemy.HealthFraction;
		float wound = 1f - fraction;
		bool outright = KillsOutright(enemy.IsMiniBoss, enemy is BossEnemy, fraction, sliver);
		int hit = WoundedDamage(needle.Damage, wound);
		// The outright kill strikes for at least what the body has left, through the same house path, so the kill is counted,
		// numbered and charged to the spell as any kill is (a crit or Vulnerable only make it more than enough).
		if (outright)
			hit = Math.Max(hit, enemy.Health);
		// Bloodletting reads the poison BEFORE the strike, so what it lets out is what was in the body when the needle went
		// in - never a poison the strike itself lays (the Poison tier's) - and lets it out only after, so a body the needle
		// kills dies poisoned (the Shrike feat counts it).
		int poisonHeld = bloodletting ? enemy.RemainingPoisonDamage : 0;

		StatStrikes++;
		StatWoundSum += wound;
		if (wound > WoundTie)
			StatWoundedStrikes++;
		PlayerRef.DealDamageToEnemy(enemy, hit, 0f, true, SpellData, null, at);
		bool killed = !IsInstanceValid(enemy) || enemy.IsDying || enemy.Health <= 0;

		if (!killed && poisonHeld > 0)
		{
			// All of it at once, numbered in Poison's ink (it is the poison doing it), and no elemental chain off it - the
			// needle's strike set off its own. Then the body is clean: what is let out is gone.
			StatReleases++;
			StatReleased += poisonHeld;
			PlayerRef.DealDamageToEnemy(enemy, poisonHeld, 0f, false, SpellData, Element.Poison, at);
			AddFx(at, needle.Heading, FxKind.Bloodlet);
			killed = !IsInstanceValid(enemy) || enemy.IsDying || enemy.Health <= 0;
			if (!killed)
				enemy.EndPoison();
		}

		if (killed)
		{
			StatKills++;
			if (outright)
				StatOutright++;
		}
		AddFx(at, needle.Heading, outright && killed ? FxKind.Outright : FxKind.Hit);

		// Triage: a needle that kills goes on, from the body it killed, to the next most wounded no needle is going for.
		if (killed && triage)
		{
			Enemy next = NextOnList(at);
			if (next != null)
			{
				StatTriage++;
				needle.Target = next;
				needle.Aim = next.GlobalPosition;
				needle.LegFrom = at;
				needle.LegAge = 0f;
				needle.Hops++;
				needle.Retargeted = false;
				return;
			}
		}
		needle.Live = false;
		needle.Target = null;
	}

	private void AddFx(Vector2 at, Vector2 dir, FxKind kind)
	{
		int slot = -1;
		for (int i = 0; i < MaxFx; i++)
		{
			int s = (nextFx + i) % MaxFx;
			if (!fx[s].Live)
			{
				slot = s;
				break;
			}
		}
		if (slot < 0)
			slot = nextFx;
		nextFx = (slot + 1) % MaxFx;
		fx[slot] = new Fx { At = at, Dir = dir.LengthSquared() > 0.01f ? dir.Normalized() : Vector2.Right, Kind = kind, Seed = ++seedClock, Live = true };
	}

	// --- the drawing ------------------------------------------------------------------------------

	public override void _Draw()
	{
		for (int i = 0; i < MaxFx; i++)
		{
			if (fx[i].Live)
				DrawFx(fx[i]);
		}
		for (int i = 0; i < MaxNeedles; i++)
		{
			if (needles[i].Live)
				DrawNeedle(needles[i]);
		}
	}

	private Color Ink(Color c, float coverage = 1f) => SpellInk.Ink(c, tint, coverage * tint.A);

	// The side of a line away from the key light (upper left): where a shaft's flank of shade goes.
	private static Vector2 ShadeSide(Vector2 heading)
	{
		Vector2 side = new(-heading.Y, heading.X);
		return side.X + side.Y >= 0f ? side : -side;
	}

	// A needle: the white-hot point, a bead of poison behind it, the shaft in steel's hot then mid over a flank of its edge,
	// the eye in the edge - and behind it a streak of steel thinning out, as long as the leg it has flown allows.
	private void DrawNeedle(in Needle needle)
	{
		float coverage = MathF.Min(1f, needle.Age / 0.04f);
		Vector2 h = needle.Heading;
		Vector2 shade = ShadeSide(h);
		Vector2 tip = needle.Pos;
		float length = NeedleLength;

		float flown = needle.Pos.DistanceTo(needle.LegFrom);
		float trail = MathF.Min(TrailLength, MathF.Max(0f, flown - length));
		for (float k = 0f; k < trail; k += Grid)
		{
			float u = k / TrailLength;
			Vector2 p = tip - h * (length + Grid + k);
			DrawRect(WhipLimb.Cell(p, Grid), Ink(u < 0.4f ? steel.Mid : steel.Edge, coverage * (1f - u) * 0.85f));
		}

		for (float k = 0f; k <= length; k += Grid)
		{
			Vector2 p = tip - h * k;
			Color tone = k < Grid ? steel.Core : k < length * 0.45f ? steel.Hot : k < length - Grid ? steel.Mid : steel.Edge;
			if (k >= Grid * 2f && k < length - Grid)
				DrawRect(WhipLimb.Cell(p + shade * Grid, Grid), Ink(steel.Edge, coverage));
			DrawRect(WhipLimb.Cell(p, Grid), Ink(tone, coverage));
		}

		// The bead: a drop of poison hanging on the shade side just behind the point, lit on its upper left.
		Vector2 bead = tip - h * (Grid * 2.5f) + shade * (Grid * 0.5f);
		DrawRect(WhipLimb.Cell(bead, Grid * 2f), Ink(poison.Mid, coverage));
		DrawRect(WhipLimb.Cell(bead - new Vector2(Grid * 0.5f, Grid * 0.5f), Grid), Ink(poison.Hot, coverage));
	}

	private void DrawFx(in Fx f)
	{
		float u = Mathf.Clamp(f.Age / FxSeconds(f.Kind), 0f, 1f);
		switch (f.Kind)
		{
			case FxKind.Hit:
				DrawHit(f, u);
				break;
			case FxKind.Outright:
				DrawOutright(f, u);
				break;
			case FxKind.Bloodlet:
				DrawBloodlet(f, u);
				break;
			default:
				DrawRect(WhipLimb.Cell(f.At, Grid), Ink(steel.Mid, 1f - u));
				DrawRect(WhipLimb.Cell(f.At - f.Dir * Grid * 2f, Grid), Ink(steel.Edge, 1f - u));
				break;
		}
	}

	// A small clean strike: the needle standing a moment in the body along its line, a steel glint at the point, two drops of
	// poison flicked on past it.
	private void DrawHit(in Fx f, float u)
	{
		float coverage = 1f - u * u;
		Vector2 d = f.Dir;
		Vector2 shade = ShadeSide(d);
		if (u < 0.6f)
		{
			for (float k = Grid * 2f; k <= NeedleLength * 0.6f; k += Grid)
			{
				Vector2 p = f.At - d * k;
				DrawRect(WhipLimb.Cell(p + shade * Grid, Grid), Ink(steel.Edge, coverage));
				DrawRect(WhipLimb.Cell(p, Grid), Ink(k < NeedleLength * 0.3f ? steel.Hot : steel.Mid, coverage));
			}
		}
		float arm = u < 0.35f ? 6f : 4f;
		DrawRect(WhipLimb.Cell(f.At, u < 0.3f ? Grid * 2f : Grid), Ink(steel.Core, coverage));
		Vector2 a = (d + shade).Normalized(), b = (d - shade).Normalized();
		for (float r = Grid * 1.5f; r <= arm; r += Grid)
		{
			Color t = Ink(r < arm * 0.6f ? steel.Hot : steel.Mid, coverage);
			DrawRect(WhipLimb.Cell(f.At + a * r, Grid), t);
			DrawRect(WhipLimb.Cell(f.At - a * r, Grid), t);
			DrawRect(WhipLimb.Cell(f.At + b * r, Grid), t);
			DrawRect(WhipLimb.Cell(f.At - b * r, Grid), t);
		}
		for (int k = 0; k < 2; k++)
		{
			float across = (Hash(f.Seed, k) - 0.5f) * 10f;
			Vector2 p = f.At + d * (4f + 14f * u) + shade * across + new Vector2(0f, 10f * u * u);
			DrawRect(WhipLimb.Cell(p, Grid), Ink(u < 0.5f ? poison.Hot : poison.Mid, coverage));
		}
	}

	// The outright kill, the moment: a long white cut drawn through the body along the needle's line in the first beat, then
	// a starburst of steel and a ring of it opening round where it went in, and drops of poison flung on.
	private void DrawOutright(in Fx f, float u)
	{
		float coverage = 1f - u * u * u;
		Vector2 d = f.Dir;
		Vector2 shade = ShadeSide(d);
		float drawn = MathF.Min(1f, u / 0.2f);
		const float cut = 26f;
		for (float k = -cut * drawn; k <= cut * drawn; k += Grid)
		{
			float t = MathF.Abs(k) / cut;
			Color tone = t < 0.45f ? steel.Core : t < 0.8f ? steel.Hot : steel.Mid;
			DrawRect(WhipLimb.Cell(f.At + d * k + shade * Grid, Grid), Ink(steel.Edge, coverage));
			DrawRect(WhipLimb.Cell(f.At + d * k, Grid), Ink(tone, coverage));
		}
		if (u < 0.12f)
			DrawRect(WhipLimb.Cell(f.At, Grid * 4f), Ink(steel.Core, 1f));
		float arm = u < 0.4f ? 14f : 9f;
		for (int a = 0; a < 8; a++)
		{
			// Eight arms, the ones across the cut the longest.
			float angle = a * Mathf.Tau / 8f + MathF.Atan2(d.Y, d.X) + Mathf.Tau / 16f;
			Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
			float length = arm * (a % 2 == 0 ? 1f : 0.6f);
			for (float r = Grid * 2f; r <= length; r += Grid)
				DrawRect(WhipLimb.Cell(f.At + dir * r, Grid), Ink(r < length * 0.5f ? steel.Core : steel.Hot, coverage));
		}
		float ring = Mathf.Lerp(8f, 22f, MathF.Sqrt(u));
		for (int a = 0; a < 12; a++)
		{
			if (u > 0.55f && a % 2 == 1)
				continue;
			float angle = a * Mathf.Tau / 12f;
			DrawRect(WhipLimb.Cell(f.At + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * ring, Grid), Ink(u < 0.5f ? steel.Mid : steel.Edge, coverage));
		}
		for (int k = 0; k < 4; k++)
		{
			float across = (Hash(f.Seed, k) - 0.5f) * 16f;
			Vector2 p = f.At + d * (6f + 22f * u * (0.7f + 0.3f * Hash(f.Seed, k + 7))) + shade * across + new Vector2(0f, 14f * u * u);
			DrawRect(WhipLimb.Cell(p, u < 0.5f ? Grid * 2f : Grid), Ink(u < 0.5f ? poison.Hot : poison.Mid, coverage));
		}
	}

	// Bloodletting: the poison let out of the wound all at once, a fan of drops spraying back out of where the needle went in
	// and falling, Poison's hot at the start and its edge as they go.
	private void DrawBloodlet(in Fx f, float u)
	{
		float coverage = 1f - u * u;
		float back = MathF.Atan2(-f.Dir.Y, -f.Dir.X);
		if (u < 0.2f)
			DrawRect(WhipLimb.Cell(f.At, Grid * 3f), Ink(poison.Core, coverage));
		for (int k = 0; k < 9; k++)
		{
			float angle = back + (k / 8f - 0.5f) * 2.4f + (Hash(f.Seed, k) - 0.5f) * 0.25f;
			float speed = 46f + 50f * Hash(f.Seed, k + 11);
			Vector2 p = f.At + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed * u + new Vector2(0f, 46f * u * u);
			Color tone = u < 0.3f ? poison.Hot : u < 0.65f ? poison.Mid : poison.Edge;
			DrawRect(WhipLimb.Cell(p, u < 0.45f && k % 3 == 0 ? Grid * 2f : Grid), Ink(tone, coverage));
		}
	}

	// A small integer hash to 0..1, so a strike's scatter needs no stored state (Corposant's).
	private static float Hash(uint seed, int i)
	{
		uint h = (uint)i * 374761393u + seed * 668265263u;
		h = (h ^ (h >> 13)) * 1274126177u;
		h ^= h >> 16;
		return (h & 0xFFFFFF) / 16777216f;
	}
}
