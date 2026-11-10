using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// WINDFALL (Grass + Metal), built 2026-11-09. Eric approved it from .ai/character-fit.md's queue, reworked: "windfall can be
// shaken by both, i like it". Nobody's opener; everyone earns it with the Fieldfare feat (Grass and Metal kills in one run).
//
// IT PLANTS A TREE. Every cast of the ONE fire loop plants a small iron-fruited tree where the wizard stands (his feet,
// Player.FootPosition) - unless one of his trees already stands close by, when the cast waits, ripe, for him to step clear
// (Player.FireWindfall answers NoTarget, Hunter's Draw's pattern). At the tree cap a new planting UPROOTS THE OLDEST, and an
// uprooted tree drops what it holds where it stood: nothing that ripened is thrown away, and the oldest tree, the fullest,
// pays out once more as it goes.
//
// IT RIPENS. A tree carries a crop (Windfall.CropAt: three fruit at level 1, more as it levels) and ripens ONE FRUIT AT A
// TIME on a ripening clock, so the longer a tree is left the more it holds. The ripening is drawn on the tree: an empty slot
// blooms, the fruit sets small and green, swells, turns iron from its shaded side and hangs ripe as a steel apple
// (tools/art/windfall.py). That is the opposite of Gravewell's trip - a trap that goes off when they come - and the reason
// the spell reads as an orchard: a tree pays more the longer it is left.
//
// BOTH SHAKE IT:
//   - AN ENEMY BUMPING IT brings ONE ripe fruit down on itself (BumpEvery apart, so a crowd cannot strip it in a frame): the
//     tree defends itself a little while he is away.
//   - HE WALKS BACK UNDER IT and the WHOLE CROP comes down at once in a ring round the tree - the payoff for coming back. It
//     needs him to have LEFT the canopy first (Windfall.StepShake: a tree is armed only once he has been LeaveReach away), so a
//     fresh tree never empties on the spot where he planted it.
// The fruit are heavy iron apples: each thuds where it lands, striking everything in ThudRadius for a fruit's damage and
// knocking it back from the tree (Enemy.ApplyKnockback, BaseKnockback in KnockbackBonus units, SpellData.KnockbackSpeedPerPoint).
//
// THE TREE ITSELF has no collision and blocks nothing; it is never in the "enemies" group, so nothing targets it and nothing
// hurts it (the breakables rule: a world object a spell made is never a target). It stands on the bodies' layer, y-sorted at
// its foot as every tree is (DrawLayers: "a body, a familiar, a tree, a chest") - one Node2D a tree in the grove, a y-sorted
// holder in the arena, ZAsRelative off - so what walks behind it is hidden by it and what walks in front is not. The light
// pool's bodies pass darkens it at the edge of the dark chapters as it darkens every prop; the iron fruit, steel's high
// stops, stay the brightest thing on it.
//
// THE MILESTONES (SpellEvolutionCatalog.SetupWindfallEvolutions), every one a change on screen:
//   Orchard      (level 4) - one more tree.
//   Bramble Fall (level 4, +1 Grass) - a fallen fruit splits into thorns that fly out past its thud.
//   Cider Rot    (level 4, trades Grass for Poison) - a fallen fruit rots where it lands into a patch that poisons.
//   Harvest Moon (level 8) - when he is crowded, every tree drops its whole crop at once.
//   Iron Bough   (level 8, +1 Metal) - the iron branches lash whatever stands under a tree.
//
// COST. Nothing per frame walks the whole swarm: a tree asks the registry's grid who is near its trunk (EnemyRegistry.
// CollectNear) on its bump clock and its lash clock, a thud on the frame it lands, a thorn as it flies, a rot patch on its
// tick, Harvest Moon four times a second. Fruit, fallen fruit, rot, thorns and lashes are fixed pools, the trees a fixed array
// of nodes made once; a struck body is held by reference, never by an id asked of the engine (.ai/performance.md).
public partial class Windfall : Node2D, IPaintsElementPath
{
	public const string SpellId = "windfall";

	// Milestone ids. SpellEvolutionCatalog builds the cards from these; this node reads them.
	public const string OrchardId = "windfall_orchard";
	public const string BrambleFallId = "windfall_bramble_fall";
	public const string CiderRotId = "windfall_cider_rot";
	public const string HarvestMoonId = "windfall_harvest_moon";
	public const string IronBoughId = "windfall_iron_bough";

	// --- the tree ---------------------------------------------------------------------------------
	/// <summary>Fruit a tree carries at level 1, before the level table's Crop lines (CropAt).</summary>
	public const int BaseCrop = 3;
	/// <summary>The most fruit a crown can hang: the slots tools/art/windfall.py drew room for.</summary>
	public const int MaxCrop = 10;
	/// <summary>Seconds one fruit takes to ripen, at the wizard's own cast speed.</summary>
	public const float RipenSeconds = 1.6f;
	/// <summary>The least time between two bumps of one tree: a crowd cannot strip it in a frame.</summary>
	public const float BumpEvery = 0.75f;
	/// <summary>A body this close to the trunk's foot (plus a body's width) has bumped the tree.</summary>
	public const float BumpReach = 22f;
	/// <summary>His feet this close to the trunk's foot: he is under the tree.</summary>
	public const float ShakeReach = 36f;
	/// <summary>His feet this far from the trunk's foot: he has left the canopy, and the tree is armed.</summary>
	public const float LeaveReach = 76f;
	/// <summary>No tree is planted this close to one of his own: the cast waits for him to step clear.</summary>
	public const float PlantSpacing = 60f;
	/// <summary>How far round the trunk a shaken crop lands, before the wizard's Area.</summary>
	public const float RingRadius = 60f;
	/// <summary>How far a fruit's thud reaches, before the wizard's Area.</summary>
	public const float ThudRadius = 26f;
	/// <summary>A thud's shove, in KnockbackBonus units (SpellData.KnockbackSpeedPerPoint): about a body's width back.</summary>
	public const float BaseKnockback = 110f;
	/// <summary>The most trees one wizard's orchard holds, Amount and Orchard and all.</summary>
	public const int MaxTrees = 12;

	// --- Bramble Fall -------------------------------------------------------------------------------
	public const int ThornsPerFruit = 4;
	/// <summary>A thorn's strike, of a fruit's. The card says it; RegressionChecks.Windfall holds the two.</summary>
	public const float ThornShare = 0.3f;
	/// <summary>How far past the thud a thorn flies.</summary>
	public const float ThornReach = 72f;
	private const float ThornSpeed = 420f;
	private const float ThornHit = 6f;

	// --- Cider Rot ----------------------------------------------------------------------------------
	/// <summary>Seconds a rot patch lies poisoning what stands in it.</summary>
	public const float RotSeconds = 3f;
	private const float RotTickSeconds = 0.5f;
	/// <summary>The poison a rot patch lays a second, of a fruit's damage.</summary>
	public const float RotPoisonShare = 0.2f;
	private const float RotPoisonSeconds = 2f;

	// --- Harvest Moon -------------------------------------------------------------------------------
	/// <summary>Bodies within CrowdReach of the wizard that bring the moon up: Saint's Crown's crowd.</summary>
	public const int CrowdEnter = 8;
	public const float CrowdReach = 140f;
	/// <summary>The least time between two harvest moons.</summary>
	public const float MoonRestSeconds = 4f;
	private const float CrowdCountSeconds = 0.25f;

	// --- Iron Bough ---------------------------------------------------------------------------------
	/// <summary>A lash's strike, of a fruit's.</summary>
	public const float LashShare = 0.45f;
	public const float LashEvery = 0.8f;
	public const float LashReach = 64f;
	public const int LashMax = 3;
	private const float LashSeconds = 0.2f;

	// --- timing and the drawing --------------------------------------------------------------------
	private const float SproutSeconds = 0.12f;
	private const float SaplingSeconds = 0.26f;
	private const float ShakeSeconds = 0.36f;
	private const float ShakeFlicker = 0.06f;
	private const float UprootSeconds = 0.4f;
	private const float DropSeconds = 0.26f;
	private const float RingFallSeconds = 0.42f;
	private const float RingStagger = 0.05f;
	private const float RingToss = 18f;
	private const float LieSeconds = 0.7f;
	private const float RefreshSeconds = 0.5f;
	private const float Body = RiptideWave.BodyAllowance;
	private const float Grid = SpellInk.Grid;

	private const int MaxFruit = 96;
	private const int MaxFallen = 96;
	private const int MaxThorns = 160;
	private const int MaxLashes = 24;

	// THE SHEET (tools/art/windfall.py): authored 1:1, saved at x2. These are its numbers at x2.
	public const string TexturePath = "res://assets/bonelight/effects/windfall.png";
	private const float TreeCellW = 96f, TreeCellH = 112f;
	private static readonly Vector2 FootInCell = new(48f, 100f);
	private const float FruitCell = 20f;
	private const float FruitRowY = 112f;
	private static readonly Vector2 FruitMid = new(10f, 10f);
	private const int FrameGrown = 0, FrameShakeLeft = 1, FrameShakeRight = 2, FrameSapling = 3, FrameSprout = 4;
	private const int StageBlossom = 0, StageIron = 4, StageRotten = 5, StageFallen = 6, StageRot = 7;

	/// <summary>
	/// Where a crown's fruit hang, as offsets from the trunk's foot in world units: tools/art/windfall.py's FRUIT_SLOTS,
	/// (slot - FOOT) x 2. The first slots are the low, lit front of the crown, so a small crop hangs where it is seen.
	/// </summary>
	public static readonly Vector2[] FruitSlots =
	{
		new(-16f, -50f), new(12f, -48f), new(-2f, -60f), new(-28f, -64f), new(24f, -62f),
		new(-14f, -76f), new(10f, -78f), new(26f, -44f), new(-30f, -46f), new(-2f, -42f),
	};

	/// <summary>The middle of the crown, from the trunk's foot: where Iron Bough's branches lash from.</summary>
	private static readonly Vector2 CrownMiddle = new(0f, -60f);

	// Contract colours the drawing uses by name (.ai/art-direction.md section 3): occlusion, verdant's shade for a thorn's
	// shaft, steel's shade and lit for an iron branch.
	private static readonly Color Occ = new(0x05070CFF);
	private static readonly Color VerdantShade = new(0x255416FF);
	private static readonly Color SteelShade = new(0x3A4A62FF);
	private static readonly Color SteelLit = new(0xB8C8DCFF);

	private struct Tree
	{
		public Vector2 Foot;
		public float Age;
		public float RipenClock;
		public float BumpClock;
		public float LashClock;
		public float ShakeAge;
		public float FadeAge;
		public int Ripe;
		public int Order;
		public long Drawn;
		public bool Armed;
		public bool Inside;
		public bool Live;
		public bool Uprooting;
	}

	private enum Fall : byte { Bump, Ring, Uproot }

	private struct Fruit
	{
		public Vector2 From;
		public Vector2 To;
		public Vector2 Tree;
		public Enemy Target;
		public float Delay;
		public float Age;
		public float Seconds;
		public int Damage;
		public uint Seed;
		public Fall Kind;
		public bool Rot;
		public bool Live;
	}

	private struct Fallen { public Vector2 At; public float Age; public float Life; public int Poison; public uint Seed; public bool Rot; public bool Live; }

	private struct Thorn { public Vector2 Pos; public Vector2 From; public Vector2 Dir; public float Travelled; public int Damage; public bool Live; }

	private struct Lash { public Vector2 Root; public Vector2 Tip; public float Age; public uint Seed; public bool Live; }

	public Player PlayerRef { get; set; }
	public SpellData SpellData { get; private set; }

	// For the harness (_BalanceHarness's WINDFALL line). Process-wide, as Corposant's are.
	public static int StatPlanted { get; set; }
	public static int StatUprooted { get; set; }
	public static int StatRipened { get; set; }
	public static int StatBumps { get; set; }
	public static int StatShakes { get; set; }
	public static int StatMoons { get; set; }
	public static int StatLanded { get; set; }
	public static int StatThudHits { get; set; }
	public static int StatThornHits { get; set; }
	public static int StatRotTicks { get; set; }
	public static int StatLashes { get; set; }
	/// <summary>The most fruit one shake of his brought down, for the harness and the checks.</summary>
	public static int StatLargestShake { get; set; }

	private int damage = 16;
	private int cap = 1;
	private int crop = BaseCrop;
	private float ripenSeconds = RipenSeconds;
	private float area = 1f;
	private float shove = BaseKnockback * SpellData.KnockbackSpeedPerPoint;
	private Color tint = Colors.White;
	private ElementColors.Ramp ramp = ElementColors.GetRamp(Element.Grass);
	// What a thud throws: iron grit, Metal's ramp - its white-hot core and pale steel read on the darkest floor and the
	// palest - or, under Cider Rot, Poison's pulp.
	private ElementColors.Ramp thudRamp = ElementColors.GetRamp(Element.Metal);
	private bool brambleForm, rotForm, moonForm, boughForm;

	private readonly Tree[] trees = new Tree[MaxTrees];
	private readonly Node2D[] treeNodes = new Node2D[MaxTrees];
	private readonly Action[] treeDrawers = new Action[MaxTrees];
	private readonly Fruit[] fruit = new Fruit[MaxFruit];
	private readonly Fallen[] fallen = new Fallen[MaxFallen];
	private readonly Thorn[] thorns = new Thorn[MaxThorns];
	private readonly Lash[] lashes = new Lash[MaxLashes];
	private readonly WhipLimb[] lashLimbs = new WhipLimb[MaxLashes];
	private readonly List<Enemy> near = new(32);
	private readonly RandomNumberGenerator rng = new();

	private Node2D grove;
	private Node2D ground;
	private static Texture2D sheet;

	private float clock;
	private float refreshedAt = float.NegativeInfinity;
	private int refreshedLevel = -1;
	private string refreshedLevel4, refreshedLevel8;
	private float rotClock;
	private float crowdClock;
	private float moonRest;
	private int plantClock;
	private int nextFruit, nextFallen, nextThorn, nextLash;
	private uint seedClock;
	private bool drewLastFrame, groundDrewLastFrame;

	public override void _Ready()
	{
		// The air, over every body (DrawLayers): fruit falling, thorns flying, branches lashing.
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		TopLevel = true;
		GlobalPosition = Vector2.Zero;
		Material = SpellInk.DitherByCell;
		TextureFilter = TextureFilterEnum.Nearest;
		rng.Seed = GD.Randi();
		sheet ??= GD.Load<Texture2D>(TexturePath);
		if (sheet == null)
			GD.PushError($"Windfall: {TexturePath} failed to load (python tools/art/windfall.py, then import).");

		// The floor, under every body: fallen fruit, rot, and a falling fruit's shadow where it will land.
		ground = new Node2D { Name = "WindfallGround", TopLevel = true, ZAsRelative = false, ZIndex = DrawLayers.Ground,
			Material = SpellInk.DitherByCell, TextureFilter = TextureFilterEnum.Nearest };
		ground.Draw += DrawGround;
		AddChild(ground);
		ground.GlobalPosition = Vector2.Zero;

		// The trees: one node each, in a y-sorted holder that is the arena's own child, so every tree sorts among the bodies
		// at its foot (BargeEvent's lamps). Made once; a planting shows one and an uprooting hides it.
		grove = new Node2D { Name = "WindfallGrove", YSortEnabled = true, ZAsRelative = false, ZIndex = DrawLayers.Bodies };
		Player.TagSpell(grove);
		for (int i = 0; i < MaxTrees; i++)
		{
			int index = i;
			var node = new Node2D { Name = $"Tree{i}", Visible = false, TextureFilter = TextureFilterEnum.Nearest, Material = SpellInk.DitherByTexel };
			treeDrawers[i] = () => DrawTree(index);
			node.Draw += treeDrawers[i];
			treeNodes[i] = node;
			grove.AddChild(node);
		}
		if (GetParent() is Node arena)
			arena.CallDeferred(Node.MethodName.AddChild, grove);
		for (int i = 0; i < MaxLashes; i++)
			lashLimbs[i] = new WhipLimb();
	}

	public override void _ExitTree()
	{
		if (ground != null && IsInstanceValid(ground))
			ground.Draw -= DrawGround;
		for (int i = 0; i < MaxTrees; i++)
			if (treeNodes[i] != null && IsInstanceValid(treeNodes[i]))
				treeNodes[i].Draw -= treeDrawers[i];
		// The orchard goes with the spell (Player.ReleaseWindfall: the Penknife, a fusion), and with the run.
		if (grove != null && IsInstanceValid(grove))
			grove.QueueFree();
	}

	/// <summary>True when the numbers should be read again: a new level, a milestone taken, or RefreshSeconds gone by.</summary>
	public bool RefreshDue(SpellData spell) =>
		spell != SpellData || spell.CurrentLevel != refreshedLevel || clock - refreshedAt >= RefreshSeconds
		|| !ReferenceEquals(spell.SelectedLevel4EvolutionId, refreshedLevel4)
		|| !ReferenceEquals(spell.SelectedLevel8EvolutionId, refreshedLevel8);

	/// <summary>
	/// The orchard's numbers: a fruit's damage, the tree cap, the crop, the ripening clock (at the wizard's cast speed:
	/// <paramref name="hasteScale"/> is the fire loop's interval over the spell's own cooldown) and the milestones. Trees
	/// already standing take the new crop and clock; fruit already falling keep their own damage.
	/// </summary>
	public void Refresh(SpellData spell, float damageMultiplier, Color visualTint, float playerArea, int treeCap, float hasteScale)
	{
		if (spell == null)
			return;
		SpellData = spell;
		int level = Math.Max(1, spell.CurrentLevel);
		damage = Math.Max(1, Mathf.RoundToInt(spell.GetDamageAtLevel(level) * damageMultiplier));
		cap = Math.Clamp(treeCap, 1, MaxTrees);
		crop = CropAt(spell, level);
		ripenSeconds = RipenSeconds * Mathf.Clamp(hasteScale, 0.25f, 2f);
		area = MathF.Max(0.25f, playerArea);
		shove = (BaseKnockback + spell.GetKnockbackBonusAtLevel(level)) * SpellData.KnockbackSpeedPerPoint;
		tint = visualTint;
		brambleForm = spell.HasEvolution(BrambleFallId);
		rotForm = spell.HasEvolution(CiderRotId);
		moonForm = spell.HasEvolution(HarvestMoonId);
		boughForm = spell.HasEvolution(IronBoughId);
		// Cider Rot repaints what falls in Poison's ramp (IPaintsElementPath): the thorns' points, the rot, a thud's splash.
		Element? branch = spell.GetBranchElement();
		ramp = ElementColors.GetRamp(branch ?? Element.Grass);
		thudRamp = ElementColors.GetRamp(branch ?? Element.Metal);
		refreshedAt = clock;
		refreshedLevel = spell.CurrentLevel;
		refreshedLevel4 = spell.SelectedLevel4EvolutionId;
		refreshedLevel8 = spell.SelectedLevel8EvolutionId;
	}

	/// <summary>The fruit one tree of this spell carries at <paramref name="level"/>: BaseCrop and its level table's Crop lines.</summary>
	public static int CropAt(SpellData spell, int level) =>
		Math.Clamp(BaseCrop + Mathf.RoundToInt(spell?.GetEffectValueAtLevel(SpellEffect.Crop, level) ?? 0f), 1, MaxCrop);

	/// <summary>The crop a tree carries now, the cap on trees and the ripening clock (for the harness and the checks).</summary>
	public int Crop => crop;
	public int Cap => cap;
	public float RipenEvery => ripenSeconds;

	/// <summary>Trees standing now, uprooting ones not counted.</summary>
	public int Standing
	{
		get
		{
			int n = 0;
			for (int i = 0; i < MaxTrees; i++)
				if (trees[i].Live && !trees[i].Uprooting)
					n++;
			return n;
		}
	}

	/// <summary>The fruit ripe on every standing tree, for the harness.</summary>
	public int RipeNow
	{
		get
		{
			int n = 0;
			for (int i = 0; i < MaxTrees; i++)
				if (trees[i].Live && !trees[i].Uprooting)
					n += trees[i].Ripe;
			return n;
		}
	}

	// --- planting ---------------------------------------------------------------------------------

	/// <summary>
	/// Plants a tree at <paramref name="foot"/>, his feet. False - the cast waits, ripe - while one of his trees already
	/// stands within PlantSpacing. At the cap, the oldest tree is uprooted first and drops what it holds where it stood.
	/// </summary>
	public bool Plant(Vector2 foot)
	{
		if (SpellData == null || sheet == null)
			return false;
		for (int i = 0; i < MaxTrees; i++)
		{
			if (trees[i].Live && !trees[i].Uprooting && trees[i].Foot.DistanceSquaredTo(foot) < PlantSpacing * PlantSpacing)
				return false;
		}
		while (Standing >= cap)
		{
			int oldest = OldestStanding();
			if (oldest < 0)
				break;
			Uproot(oldest);
		}
		int slot = -1;
		for (int i = 0; i < MaxTrees; i++)
		{
			if (!trees[i].Live)
			{
				slot = i;
				break;
			}
		}
		if (slot < 0)
		{
			// Every slot is a tree still dissolving: the one furthest gone gives way at once.
			float most = -1f;
			for (int i = 0; i < MaxTrees; i++)
			{
				if (trees[i].FadeAge > most)
				{
					most = trees[i].FadeAge;
					slot = i;
				}
			}
		}
		trees[slot] = new Tree
		{
			Foot = SpellInk.Snap(foot),
			Order = ++plantClock,
			// He is standing on it: under it, and not yet armed. He must leave the canopy before it can be shaken.
			Inside = true,
			Live = true,
			Drawn = -1,
			ShakeAge = ShakeSeconds,
		};
		Node2D node = treeNodes[slot];
		node.Position = trees[slot].Foot;
		node.Visible = true;
		node.QueueRedraw();
		StatPlanted++;
		return true;
	}

	private int OldestStanding()
	{
		int oldest = -1;
		for (int i = 0; i < MaxTrees; i++)
		{
			if (!trees[i].Live || trees[i].Uprooting)
				continue;
			if (oldest < 0 || trees[i].Order < trees[oldest].Order)
				oldest = i;
		}
		return oldest;
	}

	// The oldest tree at the cap: what it holds comes down where it stood, scattered under its crown, and it dissolves.
	private void Uproot(int i)
	{
		ref Tree tree = ref trees[i];
		Drop(i, ref tree, Fall.Uproot, tree.Ripe);
		tree.Ripe = 0;
		tree.Uprooting = true;
		tree.FadeAge = 0f;
		tree.ShakeAge = 0f;
		StatUprooted++;
	}

	// --- the frame --------------------------------------------------------------------------------

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (dt <= 0f)
			return;
		clock += dt;
		bool alive = PlayerRef != null && IsInstanceValid(PlayerRef) && !PlayerRef.IsDead;
		// A boss arriving holds everything (the spell group is frozen too, Node2DGame.FreezePlayerSpells); this is the
		// frame before that lands, and every frame of a held field after it.
		bool held = alive && PlayerRef.HeldForBossEntrance;
		Vector2 feet = alive ? PlayerRef.FootPosition : Vector2.Zero;

		TickTrees(dt, alive && !held, feet);
		if (moonForm && alive && !held)
			TickMoon(dt);
		bool anyFruit = TickFruit(dt);
		bool anyThorn = TickThorns(dt);
		bool anyLash = TickLashes(dt);
		bool anyFallen = TickFallen(dt);

		bool airAny = anyFruit || anyThorn || anyLash;
		if (airAny || drewLastFrame)
			QueueRedraw();
		drewLastFrame = airAny;
		bool groundAny = anyFallen || anyFruit;
		if (groundAny || groundDrewLastFrame)
			ground.QueueRedraw();
		groundDrewLastFrame = groundAny;
	}

	private void TickTrees(float dt, bool active, Vector2 feet)
	{
		float sqrtArea = MathF.Sqrt(area);
		for (int i = 0; i < MaxTrees; i++)
		{
			ref Tree tree = ref trees[i];
			if (!tree.Live)
				continue;
			tree.Age += dt;
			tree.ShakeAge += dt;
			if (tree.Uprooting)
			{
				tree.FadeAge += dt;
				if (tree.FadeAge >= UprootSeconds)
				{
					tree.Live = false;
					treeNodes[i].Visible = false;
					continue;
				}
				treeNodes[i].QueueRedraw();
				continue;
			}

			// Ripening: one fruit at a time, up to the crop. A crop that shrank (it never does) keeps what is ripe.
			if (tree.Ripe < crop)
			{
				tree.RipenClock += dt;
				if (tree.RipenClock >= ripenSeconds)
				{
					tree.RipenClock -= ripenSeconds;
					tree.Ripe++;
					StatRipened++;
				}
			}
			else
				tree.RipenClock = 0f;

			if (active && tree.Age >= SaplingSeconds)
			{
				// He walks back in under it: the whole crop, in a ring.
				float distance = feet.DistanceTo(tree.Foot);
				if (StepShake(ref tree.Armed, ref tree.Inside, distance, tree.Ripe > 0))
				{
					StatShakes++;
					StatLargestShake = Math.Max(StatLargestShake, tree.Ripe);
					Harvest(i, ref tree);
				}

				// A body bumps it: one fruit, on that body.
				tree.BumpClock += dt;
				if (tree.Ripe > 0 && tree.BumpClock >= BumpEvery && Bumper(tree.Foot) is Enemy bumper)
				{
					tree.BumpClock = 0f;
					StatBumps++;
					DropOn(i, ref tree, bumper);
				}

				// Iron Bough: the branches lash what stands under it.
				if (boughForm)
				{
					tree.LashClock += dt;
					if (tree.LashClock >= LashEvery && LashUnder(ref tree, sqrtArea))
						tree.LashClock = 0f;
				}
			}

			long drawn = DrawKey(in tree);
			if (drawn != tree.Drawn)
			{
				tree.Drawn = drawn;
				treeNodes[i].QueueRedraw();
			}
		}
	}

	/// <summary>
	/// One frame of a tree's shake rule, for the distance from his feet to its trunk. It is ARMED once he has been
	/// LeaveReach away; it shakes on the frame he walks back in under it (within ShakeReach) armed and with fruit ripe, and is
	/// disarmed by it. Walking in with nothing ripe leaves it armed. A tree is planted with him inside and unarmed, so it can
	/// never shake where he planted it. True on the frame it shakes.
	/// </summary>
	public static bool StepShake(ref bool armed, ref bool inside, float distance, bool hasFruit)
	{
		if (distance >= LeaveReach)
			armed = true;
		// A little give on the way out, so a wizard standing on the edge does not flicker in and out.
		bool nowInside = inside ? distance <= ShakeReach + 6f : distance <= ShakeReach;
		bool entered = nowInside && !inside;
		inside = nowInside;
		if (!entered || !armed || !hasFruit)
			return false;
		armed = false;
		return true;
	}

	// The first live body bumping the trunk: within BumpReach of its foot, a body's width allowed.
	private Enemy Bumper(Vector2 foot)
	{
		float reach = BumpReach + Body;
		EnemyRegistry.CollectNear(foot, reach, near);
		Enemy found = null;
		foreach (Enemy enemy in near)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			if (enemy.GlobalPosition.DistanceSquaredTo(foot) <= reach * reach)
			{
				found = enemy;
				break;
			}
		}
		near.Clear();
		return found;
	}

	// Harvest Moon: four times a second, the crowd round him is counted; at CrowdEnter every tree with fruit drops it all.
	private void TickMoon(float dt)
	{
		moonRest = MathF.Max(0f, moonRest - dt);
		crowdClock += dt;
		if (crowdClock < CrowdCountSeconds || moonRest > 0f)
			return;
		crowdClock = 0f;
		Vector2 me = PlayerRef.GlobalPosition;
		EnemyRegistry.CollectNear(me, CrowdReach, near);
		int crowd = 0;
		foreach (Enemy enemy in near)
			if (enemy != null && IsInstanceValid(enemy) && !enemy.IsDying && enemy.GlobalPosition.DistanceSquaredTo(me) <= CrowdReach * CrowdReach)
				crowd++;
		near.Clear();
		if (crowd < CrowdEnter)
			return;
		bool any = false;
		for (int i = 0; i < MaxTrees; i++)
		{
			ref Tree tree = ref trees[i];
			if (!tree.Live || tree.Uprooting || tree.Ripe <= 0)
				continue;
			Harvest(i, ref tree);
			any = true;
		}
		if (!any)
			return;
		moonRest = MoonRestSeconds;
		StatMoons++;
	}

	// Iron Bough: up to LashMax bodies under the crown are lashed, each struck and thrown out from the trunk. True if any was.
	private bool LashUnder(ref Tree tree, float sqrtArea)
	{
		float reach = LashReach * sqrtArea + Body;
		EnemyRegistry.CollectNear(tree.Foot, reach, near);
		int struck = 0;
		int lashDamage = Math.Max(1, Mathf.RoundToInt(damage * LashShare));
		foreach (Enemy enemy in near)
		{
			if (struck >= LashMax)
				break;
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			Vector2 at = enemy.GlobalPosition;
			if (at.DistanceSquaredTo(tree.Foot) > reach * reach)
				continue;
			struck++;
			StatLashes++;
			AddLash(tree.Foot + CrownMiddle, at);
			PlayerRef.DealDamageToEnemy(enemy, lashDamage, 0f, false, SpellData, null, tree.Foot);
			if (IsInstanceValid(enemy) && !enemy.IsDying)
				enemy.ApplyKnockback(Away(tree.Foot, at) * shove * 0.5f);
		}
		near.Clear();
		if (struck > 0)
			tree.ShakeAge = 0f;
		return struck > 0;
	}

	// --- the fruit --------------------------------------------------------------------------------

	// His shake, or the moon: everything ripe comes down at once in a ring round the trunk, a beat apart round it.
	private void Harvest(int i, ref Tree tree)
	{
		Drop(i, ref tree, Fall.Ring, tree.Ripe);
		tree.Ripe = 0;
		tree.ShakeAge = 0f;
	}

	private void Drop(int i, ref Tree tree, Fall kind, int count)
	{
		if (count <= 0)
			return;
		float start = rng.Randf() * Mathf.Tau;
		float ring = RingRadius * MathF.Sqrt(area);
		for (int k = 0; k < count; k++)
		{
			Vector2 from = tree.Foot + FruitSlots[Math.Min(k, MaxCrop - 1)];
			Vector2 to;
			float delay;
			if (kind == Fall.Ring)
			{
				to = RingPoint(tree.Foot, ring, start, k, count);
				delay = k * RingStagger;
			}
			else
			{
				// Uprooted: scattered under the crown where it stood.
				float a = rng.Randf() * Mathf.Tau;
				float d = MathF.Sqrt(rng.Randf()) * ring * 0.75f;
				to = tree.Foot + new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.7f) * d;
				delay = rng.Randf() * 0.15f;
			}
			Spawn(from, to, tree.Foot, null, kind, kind == Fall.Ring ? RingFallSeconds : RingFallSeconds * 0.8f, delay);
		}
	}

	/// <summary>Where the <paramref name="k"/>th of <paramref name="count"/> fruit of a shaken crop lands: evenly round the
	/// trunk at <paramref name="radius"/>, from <paramref name="start"/>.</summary>
	public static Vector2 RingPoint(Vector2 foot, float radius, float start, int k, int count)
	{
		float a = start + Mathf.Tau * k / Math.Max(1, count);
		return foot + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
	}

	// A bump: the front-most ripe fruit drops straight down on the body that bumped it.
	private void DropOn(int i, ref Tree tree, Enemy target)
	{
		int k = Math.Max(0, tree.Ripe - 1);
		tree.Ripe--;
		tree.ShakeAge = ShakeSeconds * 0.5f;
		Spawn(tree.Foot + FruitSlots[Math.Min(k, MaxCrop - 1)], target.GlobalPosition, tree.Foot, target, Fall.Bump, DropSeconds, 0f);
	}

	private void Spawn(Vector2 from, Vector2 to, Vector2 treeFoot, Enemy target, Fall kind, float seconds, float delay)
	{
		int slot = nextFruit;
		for (int n = 0; n < MaxFruit; n++)
		{
			int s = (nextFruit + n) % MaxFruit;
			if (!fruit[s].Live)
			{
				slot = s;
				break;
			}
		}
		nextFruit = (slot + 1) % MaxFruit;
		fruit[slot] = new Fruit
		{
			From = from,
			To = to,
			Tree = treeFoot,
			Target = target,
			Delay = delay,
			Seconds = seconds,
			Damage = damage,
			Seed = ++seedClock,
			Kind = kind,
			Rot = rotForm,
			Live = true,
		};
	}

	private bool TickFruit(float dt)
	{
		bool any = false;
		for (int f = 0; f < MaxFruit; f++)
		{
			if (!fruit[f].Live)
				continue;
			ref Fruit one = ref fruit[f];
			any = true;
			if (one.Delay > 0f)
			{
				one.Delay -= dt;
				continue;
			}
			one.Age += dt;
			// A bump's fruit lands on the body it fell for, wherever that body has got to.
			if (one.Target != null)
			{
				if (IsInstanceValid(one.Target) && !one.Target.IsDying)
					one.To = one.Target.GlobalPosition;
				else
					one.Target = null;
			}
			if (one.Age < one.Seconds)
				continue;
			one.Live = false;
			Thud(one.To, one.Tree, one.Damage, one.Rot);
		}
		return any;
	}

	/// <summary>Where a fruit is <paramref name="u"/> (0 to 1) through its fall: a bump drops straight, a shaken one is tossed out.</summary>
	private static Vector2 FallAt(in Fruit one, float u)
	{
		if (one.Kind == Fall.Bump)
			return one.From.Lerp(one.To, u * u);
		float across = 1f - (1f - u) * (1f - u);
		Vector2 p = one.From.Lerp(one.To, across);
		p.Y = Mathf.Lerp(one.From.Y, one.To.Y, u * u) - MathF.Sin(u * Mathf.Pi) * RingToss;
		return p;
	}

	// The thud: everything within reach struck for a fruit and thrown back from the tree, iron grit thrown to the reach
	// (ImpactDebrisLayer: the burst's own substance stops on the radius it hit), and the apple left lying - or, under Cider
	// Rot, burst into a patch of rot.
	private void Thud(Vector2 at, Vector2 treeFoot, int fruitDamage, bool rot)
	{
		StatLanded++;
		float reach = ThudRadius * MathF.Sqrt(area);
		if (PlayerRef != null && IsInstanceValid(PlayerRef))
		{
			EnemyRegistry.CollectNear(at, reach + Body, near);
			foreach (Enemy enemy in near)
			{
				if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
					continue;
				Vector2 e = enemy.GlobalPosition;
				if (e.DistanceSquaredTo(at) > (reach + Body) * (reach + Body))
					continue;
				StatThudHits++;
				PlayerRef.DealDamageToEnemy(enemy, fruitDamage, 0f, true, SpellData, null, at);
				if (IsInstanceValid(enemy) && !enemy.IsDying)
					enemy.ApplyKnockback(Away(treeFoot, e, at) * shove);
			}
			near.Clear();
		}
		if (GetParent() is Node2D arena)
			ImpactDebrisLayer.Spawn(arena, at, reach, thudRamp, tint);
		AddFallen(at, rot, Math.Max(1, Mathf.RoundToInt(fruitDamage * RotPoisonShare)));
		if (brambleForm)
			Splinter(at, reach, Math.Max(1, Mathf.RoundToInt(fruitDamage * ThornShare)));
	}

	// The way a struck body is thrown: out from the tree, or out from the thud for a body standing on the trunk.
	private static Vector2 Away(Vector2 treeFoot, Vector2 body, Vector2? thud = null)
	{
		Vector2 away = body - treeFoot;
		if (away.LengthSquared() < 16f && thud is Vector2 t)
			away = body - t;
		return away.LengthSquared() > 0.01f ? away.Normalized() : Vector2.Down;
	}

	private void AddFallen(Vector2 at, bool rot, int poison)
	{
		int slot = nextFallen;
		for (int n = 0; n < MaxFallen; n++)
		{
			int s = (nextFallen + n) % MaxFallen;
			if (!fallen[s].Live)
			{
				slot = s;
				break;
			}
		}
		nextFallen = (slot + 1) % MaxFallen;
		fallen[slot] = new Fallen { At = SpellInk.Snap(at), Life = rot ? RotSeconds : LieSeconds, Poison = poison, Seed = ++seedClock, Rot = rot, Live = true };
	}

	private bool TickFallen(float dt)
	{
		bool any = false;
		rotClock += dt;
		bool rotTick = false;
		if (rotClock >= RotTickSeconds)
		{
			rotClock -= RotTickSeconds;
			rotTick = true;
		}
		float reach = ThudRadius * MathF.Sqrt(area) + Body;
		for (int k = 0; k < MaxFallen; k++)
		{
			if (!fallen[k].Live)
				continue;
			ref Fallen lying = ref fallen[k];
			lying.Age += dt;
			if (lying.Age >= lying.Life)
			{
				lying.Live = false;
				continue;
			}
			any = true;
			if (!lying.Rot || !rotTick)
				continue;
			// Cider Rot: what stands in the rot is poisoned. Stacking-resistant (Enemy.ApplyPoison): patches that overlap
			// refresh one another, never add.
			EnemyRegistry.CollectNear(lying.At, reach, near);
			foreach (Enemy enemy in near)
			{
				if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying || enemy.GlobalPosition.DistanceSquaredTo(lying.At) > reach * reach)
					continue;
				StatRotTicks++;
				enemy.ApplyPoison(lying.Poison, RotPoisonSeconds);
			}
			near.Clear();
		}
		return any;
	}

	// --- Bramble Fall -----------------------------------------------------------------------------

	// The fruit splits: thorns fly out from the rim of its thud, each striking the first body it meets past it.
	private void Splinter(Vector2 at, float reach, int thornDamage)
	{
		float start = rng.Randf() * Mathf.Tau;
		for (int k = 0; k < ThornsPerFruit; k++)
		{
			float a = start + Mathf.Tau * k / ThornsPerFruit;
			Vector2 dir = new(MathF.Cos(a), MathF.Sin(a));
			int slot = nextThorn;
			for (int n = 0; n < MaxThorns; n++)
			{
				int s = (nextThorn + n) % MaxThorns;
				if (!thorns[s].Live)
				{
					slot = s;
					break;
				}
			}
			nextThorn = (slot + 1) % MaxThorns;
			Vector2 from = at + dir * reach * 0.7f;
			thorns[slot] = new Thorn { Pos = from, From = from, Dir = dir, Damage = thornDamage, Live = true };
		}
	}

	private bool TickThorns(float dt)
	{
		bool any = false;
		float step = ThornSpeed * dt;
		for (int t = 0; t < MaxThorns; t++)
		{
			if (!thorns[t].Live)
				continue;
			ref Thorn thorn = ref thorns[t];
			any = true;
			thorn.From = thorn.Pos;
			thorn.Pos += thorn.Dir * step;
			thorn.Travelled += step;
			if (PlayerRef != null && IsInstanceValid(PlayerRef))
			{
				Vector2 mid = (thorn.From + thorn.Pos) * 0.5f;
				float reach = ThornHit + Body;
				EnemyRegistry.CollectNear(mid, reach + step * 0.5f, near);
				foreach (Enemy enemy in near)
				{
					if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
						continue;
					if (Corposant.DistanceSquaredToSegment(enemy.GlobalPosition, thorn.From, thorn.Pos) > reach * reach)
						continue;
					StatThornHits++;
					PlayerRef.DealDamageToEnemy(enemy, thorn.Damage, 0f, false, SpellData, null, thorn.From);
					thorn.Live = false;
					break;
				}
				near.Clear();
			}
			if (thorn.Travelled >= ThornReach)
				thorn.Live = false;
		}
		return any;
	}

	// --- Iron Bough's lashes ----------------------------------------------------------------------

	private void AddLash(Vector2 root, Vector2 tip)
	{
		int slot = nextLash;
		for (int n = 0; n < MaxLashes; n++)
		{
			int s = (nextLash + n) % MaxLashes;
			if (!lashes[s].Live)
			{
				slot = s;
				break;
			}
		}
		nextLash = (slot + 1) % MaxLashes;
		lashes[slot] = new Lash { Root = root, Tip = tip, Seed = ++seedClock, Live = true };
	}

	private bool TickLashes(float dt)
	{
		bool any = false;
		for (int l = 0; l < MaxLashes; l++)
		{
			if (!lashes[l].Live)
				continue;
			lashes[l].Age += dt;
			if (lashes[l].Age >= LashSeconds)
				lashes[l].Live = false;
			else
				any = true;
		}
		return any;
	}

	// --- the drawing ------------------------------------------------------------------------------

	private Color Ink(Color c, float coverage = 1f) => SpellInk.Ink(c, tint, coverage * tint.A);

	// What a tree shows, packed: its frame, its ripe count, the stage of the fruit ripening, the crop and its fade. A tree is
	// redrawn only when this changes - a ripening is a few redraws a second, not sixty.
	private long DrawKey(in Tree tree)
	{
		int frame = TreeFrame(in tree);
		int stage = tree.Ripe < crop ? RipeningStage(tree.RipenClock / MathF.Max(0.01f, ripenSeconds)) : 0;
		return frame + 10L * tree.Ripe + 1000L * stage + 10000L * crop + (rotForm ? 1000000L : 0L);
	}

	private static int TreeFrame(in Tree tree)
	{
		if (tree.Age < SproutSeconds)
			return FrameSprout;
		if (tree.Age < SaplingSeconds)
			return FrameSapling;
		if (tree.ShakeAge < ShakeSeconds)
			return (int)(tree.ShakeAge / ShakeFlicker) % 2 == 0 ? FrameShakeLeft : FrameShakeRight;
		return FrameGrown;
	}

	/// <summary>The drawing of a fruit <paramref name="progress"/> (0 to 1) of the way to ripe: set, green, turning.</summary>
	public static int RipeningStage(float progress) => progress < 0.3f ? 1 : progress < 0.65f ? 2 : 3;

	private void DrawTree(int i)
	{
		Tree tree = trees[i];
		if (!tree.Live || sheet == null)
			return;
		Node2D node = treeNodes[i];
		float coverage = tree.Uprooting ? MathF.Max(0f, 1f - tree.FadeAge / UprootSeconds) : 1f;
		Color mod = new(tint.R, tint.G, tint.B, coverage);
		int frame = TreeFrame(in tree);
		node.DrawTextureRectRegion(sheet, new Rect2(-FootInCell, new Vector2(TreeCellW, TreeCellH)),
			new Rect2(frame * TreeCellW, 0f, TreeCellW, TreeCellH), mod);
		if (frame > FrameShakeRight)
			return;
		// The crop, slot by slot: ripe, the one ripening, blossom where the rest will come. The crown moves with a shake.
		float dx = frame == FrameShakeLeft ? -Grid : frame == FrameShakeRight ? Grid : 0f;
		int ripening = tree.Ripe < crop ? RipeningStage(tree.RipenClock / MathF.Max(0.01f, ripenSeconds)) : 0;
		int shown = Math.Min(crop, MaxCrop);
		for (int k = 0; k < shown; k++)
		{
			int stage = k < tree.Ripe ? (rotForm ? StageRotten : StageIron) : k == tree.Ripe ? ripening : StageBlossom;
			Vector2 at = FruitSlots[k] + new Vector2(dx, 0f) - FruitMid;
			node.DrawTextureRectRegion(sheet, new Rect2(at, new Vector2(FruitCell, FruitCell)),
				new Rect2(stage * FruitCell, FruitRowY, FruitCell, FruitCell), mod);
		}
	}

	private Rect2 FruitRegion(int stage) => new(stage * FruitCell, FruitRowY, FruitCell, FruitCell);

	public override void _Draw()
	{
		if (sheet == null)
			return;
		// The falling fruit, over the bodies they fall on.
		for (int f = 0; f < MaxFruit; f++)
		{
			if (!fruit[f].Live || fruit[f].Delay > 0f)
				continue;
			Fruit one = fruit[f];
			Vector2 at = SpellInk.Snap(FallAt(in one, Mathf.Clamp(one.Age / one.Seconds, 0f, 1f)));
			DrawTextureRectRegion(sheet, new Rect2(at - FruitMid, new Vector2(FruitCell, FruitCell)),
				FruitRegion(one.Rot ? StageRotten : StageIron), Ink(Colors.White));
		}
		DrawThorns();
		DrawLashes();
	}

	// A thorn: a dark green shaft and a pale point, three cells along its flight, laid on the 2 px grid.
	private void DrawThorns()
	{
		Color shaft = Ink(VerdantShade);
		Color body = Ink(ramp.Edge);
		Color point = Ink(ramp.Hot);
		for (int t = 0; t < MaxThorns; t++)
		{
			if (!thorns[t].Live)
				continue;
			Thorn thorn = thorns[t];
			Vector2 p = thorn.Pos;
			DrawRect(WhipLimb.Cell(p - thorn.Dir * Grid * 2f, Grid), shaft);
			DrawRect(WhipLimb.Cell(p - thorn.Dir * Grid, Grid), body);
			DrawRect(WhipLimb.Cell(p, Grid), point);
		}
	}

	// Iron Bough: an iron branch whipping from the crown to the body, there and gone in a fifth of a second.
	private void DrawLashes()
	{
		var metal = ElementColors.GetRamp(Element.Metal);
		for (int l = 0; l < MaxLashes; l++)
		{
			if (!lashes[l].Live)
				continue;
			Lash lash = lashes[l];
			float u = lash.Age / LashSeconds;
			float coverage = 1f - u * u;
			Vector2 to = lash.Tip - lash.Root;
			float length = to.Length();
			if (length < Grid * 2f)
				continue;
			float curl = (Hash(lash.Seed, 1) - 0.5f) * 1.2f;
			WhipLimb limb = lashLimbs[l];
			limb.Layout(lash.Root, to.Angle() - curl * 0.5f, length * Mathf.Lerp(0.85f, 1f, MathF.Min(1f, u * 4f)), curl, 3f, u * 12f);
			limb.Draw(this, 6f, 2f, Ink(Occ, coverage), Ink(SteelShade, coverage), Ink(metal.Mid, coverage), Ink(SteelLit, coverage), 2f, 1.4f);
		}
	}

	// The floor: a falling fruit's shadow where it will land, the apples lying, the rot.
	private void DrawGround()
	{
		if (sheet == null)
			return;
		Color occ = Ink(Occ);
		for (int f = 0; f < MaxFruit; f++)
		{
			if (!fruit[f].Live || fruit[f].Delay > 0f)
				continue;
			// The shadow grows as it falls: the falling thing is its own warning, never a mark on the area.
			float u = Mathf.Clamp(fruit[f].Age / fruit[f].Seconds, 0f, 1f);
			Vector2 at = SpellInk.Snap(fruit[f].To);
			float w = WhipLimb.SnapSize(2f + 6f * u);
			ground.DrawRect(new Rect2(at.X - w * 0.5f, at.Y + 4f, w, Grid), occ);
		}
		for (int k = 0; k < MaxFallen; k++)
		{
			if (!fallen[k].Live)
				continue;
			Fallen lying = fallen[k];
			float left = lying.Life - lying.Age;
			float coverage = MathF.Min(1f, left / 0.3f);
			if (lying.Rot)
				DrawRot(lying, coverage);
			else
				ground.DrawTextureRectRegion(sheet, new Rect2(lying.At - FruitMid, new Vector2(FruitCell, FruitCell)),
					FruitRegion(StageFallen), Ink(Colors.White, coverage));
		}
	}

	// Cider Rot's patch: the burst apple in the middle, its pulp splashed out to the reach the poison is laid in, and the rot
	// seething - a few cells of the ramp's hot stop rising and re-chosen as it works. The pulp IS the patch: no disc, no rim.
	private void DrawRot(in Fallen lying, float coverage)
	{
		float reach = ThudRadius * MathF.Sqrt(area);
		uint seethe = (uint)(clock / 0.12f);
		for (int k = 0; k < 14; k++)
		{
			float a = Hash(lying.Seed, k) * Mathf.Tau;
			float d = reach * (0.35f + 0.65f * MathF.Sqrt(Hash(lying.Seed, k + 40)));
			Vector2 p = lying.At + new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.7f) * d;
			float size = k % 3 == 0 ? Grid * 2f : Grid;
			ground.DrawRect(WhipLimb.Cell(p, size), Ink(k % 2 == 0 ? ramp.Edge : ramp.Mid, coverage));
		}
		for (int k = 0; k < 4; k++)
		{
			float a = Hash(lying.Seed + seethe, k) * Mathf.Tau;
			float d = reach * 0.6f * Hash(lying.Seed + seethe, k + 9);
			Vector2 p = lying.At + new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.7f) * d;
			ground.DrawRect(WhipLimb.Cell(p, Grid), Ink(ramp.Hot, coverage));
		}
		ground.DrawTextureRectRegion(sheet, new Rect2(lying.At - FruitMid, new Vector2(FruitCell, FruitCell)),
			FruitRegion(StageRot), Ink(Colors.White, coverage));
	}

	// A small integer hash to 0..1, so the seething needs no stored state (Corposant's, Stormsweep's).
	private static float Hash(uint seed, int i)
	{
		uint h = (uint)i * 374761393u + seed * 668265263u;
		h = (h ^ (h >> 13)) * 1274126177u;
		h ^= h >> 16;
		return (h & 0xFFFFFF) / 16777216f;
	}
}
