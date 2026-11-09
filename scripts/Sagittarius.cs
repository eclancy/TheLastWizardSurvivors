using Godot;
using System;

namespace TheOneHeMissed.scripts;

// SAGITTARIUS - Hunter's Draw + Starfall, the first combination (.ai/combo-spells.md section 3; ComboCatalog).
//
// Made from Hunter's Draw at level 8 down Hunter's Mark and Starfall at level 8 down Constellation, and from
// nothing else: both parents ride on the spell frozen as they were at the fusion (SpellData.ComboParents),
// and everything here is built from their numbers. The verb is CONVERGE.
//
//   aim      from the ground he has walked, inward, on the mark. A full draw marks the strongest enemy in
//            reach, as Hunter's Mark does; every lit star looses at it, so the arrows come in from every
//            side and pierce whatever stands between. With nothing marked, the enemy nearest the wizard.
//   cadence  the rain falls as always; while he moves every star that lands stays lit for as long as the
//            bow is drawn (up to MaxLitStars, the oldest going out first); when he stops, the loose. The
//            bow's arms still give out at a held full draw, as Hunter's Draw's do.
//   demand   walk the rain round the crowd, then stop. Moving pays twice: it lights the stars (how many
//            arrows) and draws the bow (how hard each one hits - Hunter's Draw's own draw).
//   payload  a converging volley, one piercing arrow from each lit star; the mark's Vulnerable stays; the
//            lines between the stars still cut while he walks.
//
// WHAT IT IS MADE OF. The rain is a Starfall node (Starfall.tscn), handed the frozen Starfall and credited to
// the combination (Starfall.DamageSource): its stars, its Constellation lines and whatever its level 4 took
// (Shooting Stars, Skipping Stars) fall as they always did, and Starfall.HoldKindled keeps a star lit while the
// bow is drawn. The arrows are Hunter's Draw's (HuntersArrow.tscn, ElementalBolt) on the frozen Hunter's Draw,
// so its pierce, its speed, Hunter's Mark's size and whatever its level 4 rides on an arrow (Heavy Draw's
// stagger on a full draw, Barbed's bleed) come with them; each is ArrowShare of its hit, scaled by the draw.
// Running Shot does not carry: it fires from the wizard, and nothing here does. The mark is HuntersMark.
//
// THE LOOK (no new sheet): the lit stars as white-gold points, joined by Constellation's lines; the mark's four
// arrowheads turning round the quarry; as the draw fills, a thin arc of light pulling taut between the two lit
// stars furthest apart (this node's own stroke); on the loose every star flares (Starfall.Flare), a gold
// arrow streaks from each to the mark, and the lines flash once and go out. One sound per loose, never one an
// arrow: a bowstring and a glass chord, Light's voice (SfxCatalog.SpellSagittarius).
//
// TWO WAYS TO HOLD IT (.ai/second-half.md 3.4, F2). Fused, as above: the frozen parents ride on the spell. OPENED ON,
// by Artemis (ComboDefinition.StartsFusedFor): she never held the parents, so her copy carries none
// (SpellData.ReadsOwnTable) and levels 1 to 8 off its own table. Then the rain is a Starfall on the Conjunction's own
// numbers (Player.FireSagittarius hands the spell itself in as the rain), the lines are the recipe's whatever the
// level (Starfall.Linked), and every arrow is TableArrowShare of one star's hit, loosed from a bow that levels with her
// (Player.SagittariusTableBow: Hunter's Draw down Hunter's Mark at her level, for its pierce, speed, reach, size and
// nock - a fused copy's frozen bow at her level 8).
// Everything after the cloudburst - the draw, the mark, the loose - is the one code path for both.
//
// THE HUNTER'S MOON (HuntersMoonId, Artemis's Apotheosis, verb hunt; .ai/second-half.md 3.3): "When your quarry
// falls, the mark passes to the strongest in reach and every lit star looses again." Every loose under it marks what
// it was loosed at, and does not put its stars out: each stays lit for a lit star's linger with one shot left
// (Starfall.LooseLit, FlareLoosed). When the quarry falls - to anything - the mark passes to the strongest in reach
// and every star with a shot left looses at it, with the draw the stop was loosed with, and then goes out. Each star
// at most twice a stop (HuntShotsPerStar), so a stop is at most twice MaxLitStars arrows; the mark goes on passing
// kill to kill while there is anyone in reach, so the next stop looses at it.
public partial class Sagittarius : Node2D
{
	public const string SpellId = "sagittarius";
	public const string BowSpellId = "hunters_draw";
	public const string RainSpellId = "starfall";

	/// <summary>Artemis's Apotheosis on it (ApotheosisCatalog, verb hunt).</summary>
	public const string HuntersMoonId = "sagittarius_hunters_moon";

	/// <summary>The most times one lit star looses in a stop under the Hunter's Moon (3.3: "each star at most twice a stop").</summary>
	public const int HuntShotsPerStar = 2;

	// --- the combination's own multipliers (tuned in scenes/Sagittarius.tscn) -------------------------------

	/// <summary>Each arrow's share of Hunter's Draw's level 8 arrow, before the draw scales it. Section 3: 0.8.</summary>
	[Export] public float ArrowShare { get; set; } = 0.8f;

	/// <summary>
	/// The table path's arrow (Artemis's copy): each arrow's share of ONE STAR'S hit at her level, before the draw scales
	/// it. 0.7 makes her level 8 a fused copy's: 0.7 of 22 is 15.4, where the fused bow's 19 at 0.8 is 15.2.
	/// </summary>
	[Export] public float TableArrowShare { get; set; } = 0.7f;

	/// <summary>Each star's share of Starfall's level 8 star (its lines are Constellation's share of that).</summary>
	[Export] public float StarShare { get; set; } = 1.0f;

	/// <summary>The most stars the draw holds lit at once: the most arrows a loose can carry. Section 3: 16.</summary>
	[Export] public int MaxLitStars { get; set; } = 16;

	[Export] public PackedScene RainScene { get; set; }
	[Export] public PackedScene ArrowScene { get; set; }

	public Player PlayerRef { get; set; }

	/// <summary>The combination, which every hit is credited to.</summary>
	public SpellData Spell { get; private set; }

	private SpellData bow;
	private SpellData rain;
	private Starfall sky;
	private HuntersMark mark;
	private Enemy quarry;
	private float quarryLeft;

	// The draw, as Player.TickHuntersDraw keeps Hunter's Draw's, read off the same tunables on the player.
	private float charge;
	private float stillSeconds;
	private float holdSeconds;
	private float nockLeft;
	private float nockSeconds = 0.4f;
	private Vector2 lastPosition;
	private bool tracking;

	// What the last cloudburst handed over for the arrows: one arrow's hit before the draw scales it (the frozen bow's
	// times ArrowShare, or a star's times TableArrowShare on the table path), and the player's terms on it.
	private float arrowBaseDamage = 1f;
	private float arrowDamageMultiplier = 1f;
	private float areaMultiplier = 1f;
	private float durationMultiplier = 1f;

	// The Hunter's Moon: whether the spell holds it, and the draw the stop was loosed with (the hunt's second volley
	// flies with it - the bow has been let down since).
	private bool hunting;
	private float huntDraw;

	// The archers, collected at the loose; the two stars the taut arc spans, every frame. No allocation.
	private const int ArcherBuffer = 32;
	private readonly Vector2[] archers = new Vector2[ArcherBuffer];
	private readonly Vector2[] drawn = new Vector2[ArcherBuffer];

	// The arc: one SpellStrokes on a child drawn with the web, in Light's ramp.
	private const int ArcSegments = 10;
	private SpellStrokes arcStrokes;
	private Node2D arcLayer;
	private static readonly Color Occ = new Color(0.020f, 0.027f, 0.047f, 1f);

	public override void _Ready()
	{
		// The node sits at the arena's origin and draws in world space, as Starfall's does. The arc is the
		// web's: just under the falling stars, over the bodies it spans.
		ZAsRelative = false;
		ZIndex = DrawLayers.Air - 1;
		arcStrokes = new SpellStrokes(ArcSegments);
		ElementColors.Ramp light = ElementColors.GetRamp(Element.Light);
		arcStrokes.SetTones(light, light.Edge, Occ);
		arcLayer = new Node2D { Name = "SagittariusArc", Material = arcStrokes.Material };
		AddChild(arcLayer);
		arcLayer.Draw += DrawArc;

		mark = new HuntersMark { Name = "SagittariusMark" };
		AddChild(mark);
	}

	/// <summary>
	/// The fire loop's cooldown, as Starfall's: <paramref name="stars"/> to let fall over the next
	/// <paramref name="interval"/>, and the numbers the arrows will fly with. Keeps the node current.
	/// <paramref name="frozenBow"/> and <paramref name="frozenRain"/> are the two parents frozen at the fusion - or, on
	/// the table path (the combination carries no parents: Artemis's copy), the bow at her level and the combination
	/// itself, whose table is the rain's.
	/// </summary>
	public void Cloudburst(SpellData combination, SpellData frozenBow, SpellData frozenRain, int stars, float interval,
		float nock, float playerArea, float damageMultiplier, float duration, Color visualTint)
	{
		if (combination == null || frozenBow == null || frozenRain == null)
			return;
		Spell = combination;
		bow = frozenBow;
		rain = frozenRain;
		nockSeconds = nock;
		areaMultiplier = playerArea;
		durationMultiplier = duration;
		arrowDamageMultiplier = damageMultiplier;
		arrowBaseDamage = combination.ReadsOwnTable
			? combination.GetDamageAtLevel(combination.CurrentLevel) * TableArrowShare
			: bow.GetDamageAtLevel(bow.CurrentLevel) * ArrowShare;
		hunting = combination.HasEvolution(HuntersMoonId);

		if (sky == null || !IsInstanceValid(sky))
		{
			sky = RainScene?.Instantiate<Starfall>();
			if (sky == null)
			{
				GD.PushError("Sagittarius: no RainScene (scenes/Starfall.tscn) to carry the rain.");
				return;
			}
			sky.PlayerRef = PlayerRef;
			sky.Name = "SagittariusRain";
			AddChild(sky);
		}
		sky.DamageSource = combination;
		sky.HeldCap = Math.Max(1, MaxLitStars);
		// The archer's figure is the recipe's: lit stars and their lines on the table path too.
		sky.Linked = true;
		sky.Cloudburst(frozenRain, stars, interval, playerArea, damageMultiplier * StarShare, duration, visualTint);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef) || bow == null || sky == null)
			return;
		TickQuarry((float)delta);
		// A boss is arriving: every spell is silent (Player's fire loop), and so is the bow - the draw is held
		// where it stands, never loosed and never lost.
		if (PlayerRef.HeldForBossEntrance)
		{
			lastPosition = PlayerRef.GlobalPosition;
			return;
		}
		TickDraw((float)delta);
	}

	public override void _Process(double delta)
	{
		arcLayer?.QueueRedraw();
		ShedLight();
	}

	// THE STRING IS A LIGHT in a dark chapter (LightPool, .ai/light-pool.md section 3): a band of light between the
	// two lit stars it spans, brightening as the draw fills. Pushed here, every frame it is drawn, not from the draw's
	// clock in _PhysicsProcess (a frame with no physics step would drop it). The stars light themselves (Starfall),
	// and so do the arrows of the loose (HuntersArrowLook).
	private const float StringLightReach = 48f;

	private void ShedLight()
	{
		if (!LightPool.Lit || sky == null || charge <= 0f || nockLeft > 0f)
			return;
		if (StringEnds(out Vector2 from, out Vector2 to))
			LightPool.LightAlong(from, to, StringLightReach, 0.25f + 0.45f * charge);
	}

	// The two lit stars furthest apart, which the string spans. False with fewer than two lit. Allocates nothing.
	private bool StringEnds(out Vector2 from, out Vector2 to)
	{
		from = to = Vector2.Zero;
		int n = sky.CollectLit(drawn);
		if (n < 2)
			return false;
		int a = 0, b = 1;
		float widest = -1f;
		for (int i = 0; i < n; i++)
		{
			for (int j = i + 1; j < n; j++)
			{
				float d = drawn[i].DistanceSquaredTo(drawn[j]);
				if (d > widest)
				{
					widest = d;
					a = i;
					b = j;
				}
			}
		}
		from = drawn[a];
		to = drawn[b];
		return true;
	}

	// --- the draw ---------------------------------------------------------------------------------------------
	//
	// Hunter's Draw's rule, unchanged: distance walked draws the bow (a full draw is
	// Player.HuntersDrawFullDrawDistance), standing still for the release grace looses it if it is past the
	// minimum, a full draw held too long looses on its own, and a draw too shallow to loose relaxes. This node
	// is after the player in the arena, so his move this frame has already happened: a wall or a slow counts
	// as standing still, which is the honest reading.
	private void TickDraw(float dt)
	{
		Vector2 at = PlayerRef.GlobalPosition;
		if (nockLeft > 0f)
		{
			// Nocking: Hunter's Draw's cooldown. Nothing is drawn and nothing held until it is done.
			nockLeft -= dt;
			lastPosition = at;
			sky.HoldKindled = false;
			return;
		}
		if (!tracking)
		{
			tracking = true;
			lastPosition = at;
			charge = 0f;
			stillSeconds = 0f;
			holdSeconds = 0f;
		}

		float moved = at.DistanceTo(lastPosition);
		lastPosition = at;
		// A pixel of drift per frame is the physics solver settling, not the wizard walking.
		bool moving = moved > 0.5f;
		if (moving)
		{
			stillSeconds = 0f;
			charge = MathF.Min(1f, charge + moved / MathF.Max(1f, PlayerRef.HuntersDrawFullDrawDistance));
		}
		else
		{
			stillSeconds += dt;
		}
		if (charge >= 1f)
			holdSeconds += dt;

		// Every star that lands while the bow is drawn stays lit until the loose.
		sky.HoldKindled = charge > 0f;

		if (charge >= 1f && holdSeconds >= PlayerRef.HuntersDrawHoldSeconds && Loose(1f))
			return;

		if (!moving && stillSeconds >= PlayerRef.HuntersDrawReleaseGrace)
		{
			if (charge >= PlayerRef.HuntersDrawMinLooseCharge)
			{
				Loose(charge);
				return;
			}
			charge = MathF.Max(0f, charge - PlayerRef.HuntersDrawRelaxPerSecond * dt);
			if (charge <= 0f)
			{
				// The bow is down: what it held is only lit now, for Constellation's linger.
				sky.HoldKindled = false;
				sky.ReleaseHeld();
			}
		}
	}

	// --- the loose ---------------------------------------------------------------------------------------------

	// Every lit star looses one arrow at the mark - or, with nothing marked, at the enemy nearest the wizard.
	// True if the volley left. With nothing in reach, or no star lit to loose from, the draw is kept and its
	// fatigue clock stopped, as Hunter's Draw keeps a draw with nothing to shoot.
	private bool Loose(float drawn)
	{
		float castRange = Reach();
		drawn = Mathf.Clamp(drawn, 0f, 1f);
		bool fullDraw = drawn >= 0.999f;

		// Hunter's Mark: a full draw marks the strongest enemy in reach. A mark still on its quarry from an
		// earlier draw is still the aim.
		Enemy marked = LiveQuarry();
		if (fullDraw && PlayerRef.FindStrongestEnemyWithin(castRange) is Enemy strongest)
			marked = strongest;
		Node2D target = marked ?? NearestToWizard(castRange);
		// Under the Hunter's Moon each star may loose twice a stop (Starfall.LooseLit); otherwise every lit star once.
		int count = target == null ? 0 : hunting ? sky.LooseLit(archers, 0, HuntShotsPerStar) : sky.CollectLit(archers);
		if (target == null || count == 0)
		{
			holdSeconds = 0f;
			return false;
		}
		// A full draw marks (or marks again, for the full five seconds) whatever it chose. Under the Hunter's Moon every
		// volley marks what it was loosed at: that is the quarry whose fall passes the mark on.
		if (hunting && marked == null)
			marked = target as Enemy;
		if (marked != null && (fullDraw || hunting))
			Mark(marked);

		Volley(target, marked, count, drawn);
		huntDraw = drawn;

		charge = 0f;
		holdSeconds = 0f;
		stillSeconds = 0f;
		tracking = false;
		nockLeft = nockSeconds;
		sky.HoldKindled = false;
		return true;
	}

	// The mark's reach: the bow's, with the wizard's own Far-Shooting on it (SpellData.ReachScale, Artemis's).
	private float Reach() => bow.GetReachAtLevel(bow.CurrentLevel);

	// One volley: an arrow from each of the first `count` archers at the target, homing when it is marked, each the
	// arrow's hit scaled by the draw; then the stars flare - out, or (under the Hunter's Moon) out only once they have
	// loosed twice - and one sound, at the wizard.
	private void Volley(Node2D target, Enemy marked, int count, float drawn)
	{
		bool fullDraw = drawn >= 0.999f;
		int arrowDamage = Math.Max(1, Mathf.RoundToInt(arrowBaseDamage * arrowDamageMultiplier
			* Mathf.Lerp(PlayerRef.HuntersDrawMinDamageMultiplier, PlayerRef.HuntersDrawMaxDamageMultiplier, drawn)));
		float speedScale = Mathf.Lerp(PlayerRef.HuntersDrawMinSpeedMultiplier, 1f, drawn);
		int pierceBonus = fullDraw ? PlayerRef.HuntersDrawFullDrawPierceBonus : 0;
		bool heavy = fullDraw && bow.HasEvolution(BoltEvolutions.HeavyDrawId);
		Node arena = GetParent();
		Vector2 aimAt = target.GlobalPosition;
		for (int i = 0; i < count && ArrowScene != null && arena != null; i++)
		{
			Vector2 from = archers[i];
			if (ArrowScene.Instantiate() is not ElementalBolt arrow)
				continue;
			arrow.Position = from;
			arrow.SpellData = bow;
			arrow.DamageSource = Spell;
			arrow.OverrideDamage = arrowDamage;
			arrow.AreaMultiplier = areaMultiplier;
			arrow.DurationMultiplier = durationMultiplier;
			arrow.BaseSpeed *= speedScale;
			arrow.BasePierce += pierceBonus;
			arrow.KnockbackScale = heavy ? 1f : 0f;
			arrow.StaggerSeconds = heavy ? BoltEvolutions.HeavyStaggerSeconds : 0f;
			// The marked volley homes, as Hunter's Mark's does; an unmarked one goes exactly where it was loosed.
			arrow.Homing = marked != null;
			arrow.SetSpellLevel(bow.CurrentLevel);
			arrow.PlayerRef = PlayerRef;
			arena.AddChild(Player.TagSpell(arrow));
			// Hunter's Mark's size. The arrow paints itself (HuntersArrowLook), gilded - a gold head with a
			// white point - because its hits are the combination's.
			PlayerRef.ApplySpellVisual(arrow, bow);
			if (heavy)
				arrow.Scale *= BoltEvolutions.HeavyArrowScale;
			// Aimed at where the quarry stands; a star on top of it still flies through it.
			Vector2 heading = aimAt - from;
			if (heading.LengthSquared() < 1f)
				heading = Vector2.Right;
			arrow.Shoot(from, from + heading.Normalized() * MathF.Max(heading.Length(), 400f), marked);
		}

		if (hunting)
		{
			sky.FlareLoosed(HuntShotsPerStar, Starfall.LingerSeconds * MathF.Max(0.1f, durationMultiplier));
			HuntVolleys++;
		}
		else
		{
			sky.Flare();
		}
		// One sound a volley, at the wizard - the bow is his - never one an arrow.
		SfxPlayer.Instance?.PlayAt(SfxCatalog.SpellSagittarius, PlayerRef.GlobalPosition, 0.05f, SfxCatalog.SpellSagittarius, 120);
	}

	/// <summary>Volleys loosed under the Hunter's Moon this run, the stops' and the hunt's (the balance harness reads it).</summary>
	public static int HuntVolleys { get; set; }

	/// <summary>Times a marked quarry fell under the Hunter's Moon and the mark passed on (the harness reads it).</summary>
	public static int HuntPasses { get; set; }

	/// <summary>Of those, the times a star had a shot left and the stars loosed again at the next quarry.</summary>
	public static int HuntRevolleys { get; set; }

	/// <summary>A run's tallies start at nothing (Player._Ready, as the Eye of Ra's do).</summary>
	public static void ResetHuntTallies()
	{
		HuntVolleys = 0;
		HuntPasses = 0;
		HuntRevolleys = 0;
	}

	private void Mark(Enemy enemy)
	{
		quarry = enemy;
		quarryLeft = BoltEvolutions.MarkSeconds;
		enemy.ApplyVulnerable(BoltEvolutions.MarkVulnerable, BoltEvolutions.MarkSeconds);
		mark?.Mark(enemy, BoltEvolutions.MarkSeconds);
	}

	private void TickQuarry(float dt)
	{
		if (quarry == null)
			return;
		quarryLeft -= dt;
		if (!IsInstanceValid(quarry))
		{
			quarry = null;
			return;
		}
		if (quarry.IsDying)
		{
			// THE HUNTER'S MOON: the quarry fell while it was still marked - to her arrows or to anything else.
			quarry = null;
			if (hunting && quarryLeft > 0f && !PlayerRef.HeldForBossEntrance)
				Hunt();
			return;
		}
		if (quarryLeft <= 0f)
			quarry = null;
	}

	// The mark passes to the strongest in reach, and every star with a shot left looses at it with the draw its stop
	// was loosed with - then those stars go out. With no star left the mark still passes, and the next stop looses at it.
	private void Hunt()
	{
		if (sky == null || PlayerRef.FindStrongestEnemyWithin(Reach()) is not Enemy next)
			return;
		Mark(next);
		HuntPasses++;
		int count = sky.LooseLit(archers, 1, HuntShotsPerStar);
		if (count <= 0)
			return;
		HuntRevolleys++;
		Volley(next, next, count, huntDraw);
	}

	private Enemy LiveQuarry() => quarry != null && IsInstanceValid(quarry) && !quarry.IsDying && quarryLeft > 0f ? quarry : null;

	private Node2D NearestToWizard(float range)
	{
		Vector2 at = PlayerRef.GlobalPosition;
		Node2D nearest = null;
		float best = range * range;
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			float d = at.DistanceSquaredTo(enemy.GlobalPosition);
			if (d <= best)
			{
				best = d;
				nearest = enemy;
			}
		}
		return nearest;
	}

	// --- the taut arc -------------------------------------------------------------------------------------------
	//
	// As the draw fills, a thin arc of light pulls taut between the two lit stars furthest apart: slack - bowed
	// out by up to a quarter of its span - at the start of a draw, a straight bright line at a full one, which
	// flashes while it is held. It is the bowstring, and the only meter the draw has.
	private void DrawArc()
	{
		if (arcLayer == null || sky == null || charge <= 0f || nockLeft > 0f || PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;
		if (!StringEnds(out Vector2 from, out Vector2 to))
			return;
		Vector2 span = to - from;
		float length = span.Length();
		if (length < 8f)
			return;
		// Bowed away from the wizard, the way a drawn string's slack hangs from the bow's tips.
		Vector2 normal = new Vector2(-span.Y, span.X) / length;
		Vector2 middle = (from + to) * 0.5f;
		if (normal.Dot(middle - PlayerRef.GlobalPosition) < 0f)
			normal = -normal;
		float slack = (1f - charge) * 0.25f * length;
		Vector2 control = middle + normal * slack * 2f;
		bool full = charge >= 0.999f;

		arcStrokes.Begin();
		Vector2 previous = from;
		for (int s = 1; s <= ArcSegments; s++)
		{
			float t = s / (float)ArcSegments;
			float u = 1f - t;
			Vector2 point = u * u * from + 2f * u * t * control + t * t * to;
			arcStrokes.Add(new SpellStrokes.Stroke
			{
				A = previous, B = point, HalfA = 1f, HalfB = 1f, Spine = full ? 0 : 1,
				Glow = 1f + 2f * charge, Density = 0.25f + 0.5f * charge, GlowTone = 3, Flash = full,
			});
			previous = point;
		}
		arcStrokes.Draw(arcLayer, Colors.White);
	}

	// --- the fusion ---------------------------------------------------------------------------------------------

	/// <summary>
	/// The numbers the fire loop and the cards read from the spell itself, set once at the fusion from the
	/// frozen parents (Player.TryFuse): the cooldown is the rain's cloudburst, the count its stars, the range
	/// its width, the damage one star's. Everything else is read from the parents as it fires.
	/// </summary>
	public static void Freeze(SpellData combination)
	{
		SpellData rainParent = combination.ComboParent(RainSpellId);
		if (rainParent == null)
			return;
		int level = rainParent.CurrentLevel;
		combination.LevelUpgrades = new Godot.Collections.Array<SpellLevelUpgrade>();
		combination.BaseCooldown = rainParent.GetCooldownAtLevel(level);
		combination.BaseDamage = rainParent.GetDamageAtLevel(level);
		combination.BaseProjectileCount = rainParent.GetProjectileCountAtLevel(level);
		combination.BaseRange = rainParent.GetRangeAtLevel(level);
	}
}
