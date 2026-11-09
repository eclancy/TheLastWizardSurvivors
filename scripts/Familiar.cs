using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// The Familiar: a creature the wizard conjures out of his own light, that keeps by him and fights what
// comes close (.ai/spell-roster.md, "Familiar"). It is taken in one of THREE FORMS, chosen the first time
// the spell is taken (SpellData.Variants; .ai/decisions/2026-10-28-spell-variants.md):
//
//   the DOG  - a hound at his heel: the most direct damage. It goes for what nears him, leaps and bites.
//   the CROW - a crow on the air over him: the fastest. It ranges far, stoops on its quarry, pecks and is
//              gone, and is back on the next one before a hound would have turned round.
//   the CAT  - a calico near him that never deals damage (2026-10-31): its pounce WEAKENS what it lands on,
//              and each visit back to him gives him its grants - every spell faster, more hits slipped
//              (Familiar.LiveGrants, read by Player).
//
// Eric, 2026-10-28: "when you FIRST take familiar, you should be able to select your familiar. each has
// it's own advantages - crow moves faster, cat gives passive bonuses, dog does the most direct damage.
// they each have their own mutations/ascensions".
//
// WHY IT EXISTS. The developer: "add new spell, one that summons a pet that fights with you (clearly
// a wizard dog, but can evolve/grow/become a new creature on mutation/ascension)". It is the roster's
// autonomous summon - phase 5 of .ai/spell-variety.md, the one weapon type that makes the screen feel
// like it has an ally in it. Kindled Ward was built as that archetype WITHOUT an ally, on the reading of
// .ai/world-and-tone.md that the player has no escort; the familiar keeps the half of that reading that
// matters - it is something the wizard MADE, lit with his light, not someone who came to help.
//
// THE SIGNATURE (aim / cadence / demand / payload), by form:
//   dog:  the enemy nearest the PLAYER inside its range (it guards); a bite per hound per cooldown;
//         it fights where he stands; a pounce and a bite - one quarry and whatever is in its jaws.
//   crow: an enemy that shoots first, else the one nearest the crow, inside a wider range; a peck per
//         crow per cooldown, at a shorter cooldown; none; a stoop and a peck on one enemy.
//   cat:  what comes near him; a pounce per cooldown; it wanders near him and comes back; a debuff, no damage.
//
// THE BODY. Each creature is plain state in this node plus one AnimatedSprite2D, a child of this node,
// which is y-sorted with the arena so it stands in front of or behind the bodies round it - or, for a
// flier, drawn in the air over all of them (DrawLayers.Air). It is not a
// physics body and not in any group: no enemy can see it, hit it or be blocked by it, and it never
// blocks the player. A walker (the dog, the cat) meets the ground the way the enemies' routing does -
// MazeNavigation's solid grid (water, maze walls, trunks, fences) - by refusing to step onto a blocked
// cell, sliding along it, and routing home round it on the navigation's flow field. A flier (the crow,
// the griffin) goes straight over all of it. If anything is ever stuck, left behind or standing on
// something solid anyway, it BLINKS: a puff of its own light, and it is back at heel. A familiar can do
// that; it is a spell. The balance harness counts every blink (StatsLine).
//
// THE MILESTONES (SpellEvolutionCatalog.SetupFamiliarEvolutions, one block per form; ids start with the
// form's id; tools/art/familiar.py draws every look):
//   dog:  Great Hound (L4, +Arcane), Rimehound (L4, the Ice path), Littermates (L4) /
//         Cerberus (L8, +Arcane - three heads), Hound of Annwn (L8 - elites first, and bites them double)
//   crow: Raven (L4, +Arcane), Stormcrow (L4, the Lightning path), Murder of Crows (L4) /
//         The Morrigan (L8, +Arcane - a peck on the nearly dead kills), Griffin (L8 - the swoop through a line)
//   cat:  Grimalkin (L4, +Arcane - its grants grow), Cait Sith (L4, the Darkness path), Purr (L4 - a heal a
//         visit), Nine Lives (L4 - a life for yours) / Cath Palug (L8, +Arcane - the great cat), Black Cat (L8 -
//         it casts his spells)
// After an element path the level 8 looks are drawn in that element.
//   crow, milestone 9: The Painted Door (Ma Liang's Apotheosis, verb displace) - where it strikes it paints a door, and
//         what stands on it steps out of a second door far across the field (Familiar.PaintedDoor.cs).
//
// NO MARKERS (art-direction.md section 3): the bite's reach is the jaws snapping shut on the bite radius
// and ImpactDebris thrown out to exactly it; a swipe is three claw strokes inside it; the swoop's swath
// is the feathers it sheds, inside it.
public partial class Familiar : Node2D, IPaintsElementPath
{
	// --- the forms (SpellData_Familiar.tres, SpellVariant ids) ---------------------------------------
	public const string DogId = "familiar_dog";
	public const string CrowId = "familiar_crow";
	public const string CatId = "familiar_cat";

	public enum Kind { Dog, Crow, Cat }

	/// <summary>Which form a held copy of the spell is. A template (no form chosen) is the dog, its default.</summary>
	public static Kind FormOf(SpellData spell) => spell?.SelectedVariantId switch
	{
		CrowId => Kind.Crow,
		CatId => Kind.Cat,
		_ => Kind.Dog,
	};

	// Milestone ids. SpellEvolutionCatalog builds the options from these; this node reads them. Each
	// starts with its form's id (ValidateSpellVariants). The element paths are built by ElementPathOption,
	// which names its option "<spell id>_<suffix>" - hence "dog_rimehound" and the like there.
	public const string GreatHoundId = "familiar_dog_great_hound";
	public const string RimehoundId = "familiar_dog_rimehound";
	public const string LittermatesId = "familiar_dog_littermates";
	public const string CerberusId = "familiar_dog_cerberus";
	public const string AnnwnId = "familiar_dog_hound_of_annwn";

	public const string RavenId = "familiar_crow_raven";
	public const string StormcrowId = "familiar_crow_stormcrow";
	public const string MurderId = "familiar_crow_murder_of_crows";
	public const string MorriganId = "familiar_crow_the_morrigan";
	public const string GriffinId = "familiar_crow_griffin";

	public const string GrimalkinId = "familiar_cat_grimalkin";
	public const string CaitSithId = "familiar_cat_cait_sith";
	public const string PurrId = "familiar_cat_purr";
	public const string CathPalugId = "familiar_cat_cath_palug";
	// A level 4 card since 2026-10-31 (it was the level 8 the Black Cat took). Same id: the life is given by
	// id at whatever milestone it is taken (Player.FireFamiliar), and a save that saw it as an Ascension
	// (SaveData.SeenAscensionIds) only holds a seen key no level 8 option names any more - read by nothing.
	public const string NineLivesId = "familiar_cat_nine_lives";
	public const string BlackCatId = "familiar_cat_black_cat";

	/// <summary>
	/// The two Ascensions the hound pack offered before it had forms, and what each is now. A seen
	/// Ascension is saved as "spell:branch" (SaveData.SeenAscensionIds), so a save that met Cerberus or
	/// the Griffin before forms existed would find them sealed again; SaveData.IsAscensionSeen reads an
	/// old key as its new one through this table. Cerberus stayed the dog's; the Griffin, a flier, went
	/// to the crow. Level 4 milestones were never recorded anywhere a save keeps.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> RenamedMilestones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["familiar_cerberus"] = CerberusId,
		["familiar_griffin"] = GriffinId,
	};

	/// <summary>The most creatures a familiar holds, whatever its count says.</summary>
	public const int MaxHounds = 6;

	// --- the dog's milestones --------------------------------------------------------------------------
	/// <summary>Heads that bite on every Cerberus bite: the quarry and the nearest others.</summary>
	public const int CerberusHeads = 3;
	/// <summary>How far from the quarry the other two heads reach, before Area.</summary>
	public const float CerberusReach = 64f;
	/// <summary>The Hound of Annwn's bite on an elite or a boss, as a multiple of its bite.</summary>
	public const float AnnwnEliteMultiplier = 2.0f;
	// How much nearer an elite or a boss looks to the Hound of Annwn: it goes for one first when one is near.
	private const float AnnwnElitePull = 140f;

	// --- the crow's milestones -------------------------------------------------------------------------
	/// <summary>The Morrigan's peck kills an ordinary enemy at or under this share of its health outright.</summary>
	public const float MorriganExecuteFraction = 0.3f;
	/// <summary>...and lands this many times over on an elite or a boss there, which no peck kills outright.</summary>
	public const float MorriganEliteMultiplier = 2.0f;
	/// <summary>Half the width of the swath a griffin's swoop strikes, before Area; tested against enemy
	/// centres with RiptideWave.BodyAllowance on top, so what it strikes is what its feathers are seen to cross.</summary>
	public const float SwoopHalfWidth = 22f;
	/// <summary>How far past its quarry a swoop carries on.</summary>
	public const float SwoopOvershoot = 130f;
	public const float SwoopSpeed = 760f;

	// --- the cat's grants (Familiar.GrantsOf; its level lines add to these) ------------------------------
	/// <summary>Every spell's cast speed while the cat is out, at level 1 (an attack speed share).</summary>
	public const float CatCastSpeed = 0.08f;
	/// <summary>The wizard's evasion while the cat is out, at level 1.</summary>
	public const float CatEvasion = 0.04f;
	/// <summary>Grimalkin: every grant this many times over.</summary>
	public const float GrimalkinGrants = 1.4f;
	/// <summary>Cath Palug: every grant this many times over again.</summary>
	public const float CathPalugGrants = 1.5f;

	// THE CAT IS THE SUPPORT FAMILIAR (Eric, 2026-10-31: "rebalance cat to not attack enemies, but instead
	// wander around nearby, debuffing enemies and wandering back to the player to provide buffs"). It deals
	// no damage. Its cooldown hands it a pounce: it picks an enemy near the wizard, rears back, pounces and
	// lands a DEBUFF - Weaken, so what it marked hits the wizard softer, and a moment's slow (Enemy.Apply*,
	// stacking-resistant) - then trots back to his feet, and the visit is what gives him its grants for the
	// next CatBuffSeconds. With nothing to pounce on it wanders near him and comes back before the grants lapse.
	/// <summary>Weaken from a pounce: what it lands on deals this much less damage.</summary>
	public const float CatWeakenFraction = 0.3f;
	/// <summary>...for this long (Grimalkin: GrimalkinGrants times longer).</summary>
	public const float CatWeakenSeconds = 4f;
	/// <summary>The pounce also checks the quarry's stride: this speed multiplier, for CatSlowSeconds.</summary>
	public const float CatSlowMultiplier = 0.7f;
	public const float CatSlowSeconds = 1f;
	/// <summary>The Cait Sith's rider (the Darkness path): Vulnerable, so every spell hits what it marked harder.</summary>
	public const float CaitSithVulnerable = 0.25f;
	public const float CaitSithVulnerableSeconds = 3f;
	/// <summary>How long one visit to the wizard keeps the cat's grants (cast speed, evasion) on him.</summary>
	public const float CatBuffSeconds = 8f;
	// Within this of his feet, a visit lands; with this little of the grants left, the cat comes home anyway.
	private const float CatTouch = 44f;
	private const float CatBuffLow = 2f;
	// Where it wanders when there is nothing to pounce on: this near him, picking a new spot this often.
	private const float CatWanderMin = 40f;
	private const float CatWanderMax = 95f;
	private const float CatWanderSeconds = 2.2f;
	// The rear-back before a pounce: longer than a hound's crouch, the cat's wiggle.
	private const float CatRearSeconds = 0.3f;

	/// <summary>Purr: every PurrSeconds the cat runs to the wizard and heals PurrHeal - 9 HP a minute, the
	/// healing budget's rate for a relic or a boon (.ai/difficulty-curve.md section 5).</summary>
	public const float PurrSeconds = 20f;
	/// <summary>Tools only (_UiShot --purr-soon): the first Purr heal comes due 1.5 s after the card, to photograph.</summary>
	public static bool DebugPurrSoon { get; set; }
	public const int PurrHeal = 3;

	/// <summary>Black Cat (the cat's Ascension): every this many seconds it casts one of the wizard's attack
	/// spells, chosen at random, from where it stands (Player.CastFromFamiliar).</summary>
	public const float BlackCatSeconds = 3f;

	// --- the hunt (tuned in scenes/Familiar.tscn; the dog's) ---------------------------------------------
	[Export] public float RunSpeed { get; set; } = 330f;
	[Export] public float SprintSpeed { get; set; } = 450f;
	[Export] public float FlySpeed { get; set; } = 400f;
	// The jaws: what one bite closes on, round the quarry's centre, before Area.
	[Export] public float BaseBiteRadius { get; set; } = 18f;
	// How far it leaps, as a share of the hunting range - so "Range" on a level card is both.
	[Export] public float LeapReachFraction { get; set; } = 0.42f;
	[Export] public float CrouchSeconds { get; set; } = 0.08f;
	[Export] public float LeapSeconds { get; set; } = 0.22f;
	[Export] public float BiteSeconds { get; set; } = 0.16f;
	[Export] public float HowlSeconds { get; set; } = 0.85f;
	// Past this from the wizard's feet it gives up walking and blinks home.
	[Export] public float BlinkDistance { get; set; } = 470f;
	// This long unable to step, and it blinks home.
	[Export] public float StuckSeconds { get; set; } = 0.6f;

	// THE CROW. Its speed is the form: it covers ground half again as fast as a running hound's sprint,
	// stoops on a quarry from a long way off in a fraction of a hound's leap, and rises straight back off
	// it - so a peck is a short cycle and the next one is never long coming.
	public const float CrowFlySpeed = 640f;
	/// <summary>How far off its quarry a crow stoops from.</summary>
	public const float CrowStoopReach = 120f;
	private const float CrowStoopSpeed = 900f;
	private const float CrowPeckSeconds = 0.08f;
	// The beak: what one peck closes on, round the quarry's centre, before Area. Smaller than the jaws.
	private const float CrowBeakRadius = 12f;
	// Where a crow waits for its next peck: this far off its quarry, circling.
	private const float CrowHoldDistance = 96f;
	// How much nearer a quarry that shoots looks to a crow: it goes for the archers first.
	private const float CrowRangedPull = 220f;
	// The crow ranges far from the wizard, so it is left behind only well past where a hound would be.
	private const float CrowBlinkDistance = 640f;
	// The griffin, the crow's Ascension, keeps most of the crow's pace.
	private const float GriffinFlyFactor = 0.85f;
	// THE CROW'S PATROL AND DIVE (Eric, 2026-10-31: the crow "flies around, mostly patrolling a loose ring near
	// the wizard, and divebombs enemies"). With no peck in hand it rides a loose ring round him - two crows the
	// opposite ways round. With one, it flies at its quarry, CLIMBS (pulls up and back, wings raised), STOOPS,
	// pecks, and RISES off it back toward the ring.
	private const float CrowPatrolRadius = 110f;
	private const float CrowPatrolWobble = 18f;
	private const float CrowPatrolTurn = 1.3f;
	private const float CrowClimbSeconds = 0.1f;
	private const float CrowClimbBack = 28f;
	private const float CrowRiseSeconds = 0.16f;
	private const float CrowRiseSpeed = 420f;

	// THE CAT. It stays underfoot - its range is short and its heel is close - and its pounce is short and
	// quick, with the wiggle before it a cat makes.
	private const float CatRunSpeed = 300f;
	public const float CatClawRadius = 16f;
	private const float CatLeapFraction = 0.5f;
	private const float CatLeapSeconds = 0.16f;
	private const float CatSwipeSeconds = 0.12f;
	private const float CatHeelBehind = 34f;
	private const float CatHeelSide = 16f;
	// THE DOG AT HEEL (Eric, 2026-10-31: the dog "heels at the wizard's side when not attacking and tries to
	// stay close; its attack runs are short and it comes straight back to heel"). Its heel is BESIDE him on
	// screen - left or right of him, a hound's half-width clear of his robe - and it only leaves it with a bite
	// in hand; after the bite it comes straight back. (Beside him along his walk put it above or below him, and
	// the one below stood over his legs.)
	private const float DogHeelSide = 52f;
	private const float DogHeelDrop = 4f;

	// CATCHING BOLTS (2026-10-28, Eric: "a few spells should help guard you from projectiles, like
	// familiar being able to catch projectiles"). A familiar at heel or on the hunt watches the bolts in
	// flight (EnemyProjectile.Live); one coming at the wizard, inside its watch of him and where it will
	// be within its reach, it goes for and takes out of the air. Then it rests before it will go for
	// another, so it is a guard and not a wall: a volley of five gets past a lone hound, and a pack of
	// three takes most of it. Every form catches, each in its own way: the hound leaps and snaps, the
	// crow (and the griffin) takes it on the wing from farther out, and the cat bats at what comes right
	// up to the wizard, often - it rests least, and watches least far.
	private readonly struct CatchRule
	{
		public CatchRule(float watch, float reach, float rest)
		{
			Watch = watch;
			Reach = reach;
			Rest = rest;
		}

		public float Watch { get; }
		public float Reach { get; }
		public float Rest { get; }
	}

	private static readonly CatchRule DogCatch = new(260f, 170f, 1.6f);
	private static readonly CatchRule CrowCatch = new(320f, 230f, 1.5f);
	private static readonly CatchRule CatCatch = new(170f, 110f, 1.0f);
	private const float CatchJaws = 34f;
	private const float CatchLeapSeconds = 0.18f;
	// Only a bolt really coming at him: the cosine between its heading and the line to him.
	private const float CatchThreatCosine = 0.6f;

	private const float BlinkSeconds = 0.14f;
	private const float RetargetSeconds = 0.2f;
	// Where heel is: behind the wizard, away from where he walks, and to one side - far enough that a
	// hound at rest stands clear of his robe (at 40 px it stood in it).
	private const float HeelBehind = 58f;
	private const float HeelSide = 22f;
	// Sound throttle: a pack of six biting together is one snap, not six.
	private const int SoundGapMs = 110;

	private const int MaxMotes = 128;
	private const int MaxSnaps = 24;

	// THE COURSING (.ai/side-events.md has the wizards; this is the hound's own deed, AchievementDefinitions
	// "coursing"): a Black Hare of the Broken Circle killed by a hound's bite. Read by EnemyType, the
	// one name the hare's scene and StageBestiary agree on (ValidateBestiary holds them to each other).
	public const string CoursedEnemyType = "HeathBlackHare";

	private static readonly StringName AnimIdle = "idle";
	private static readonly StringName AnimRun = "run";
	private static readonly StringName AnimPounce = "pounce";
	private static readonly StringName AnimHowl = "howl";

	// Every drawing the familiar has, one SpriteFrames each (tools/art/familiar.py). In FormNames order.
	// The last seven are the dog's PACK under an Ascension and Littermates (ResolvePackLook): the hounds
	// after the first wear them, so only the main hound is the legendary creature.
	private enum Look
	{
		Hound, Rimehound, GreatHound, Cerberus, CerberusRime, Annwn, AnnwnRime,
		Crow, Stormcrow, Raven, Morrigan, MorriganStorm, Griffin, GriffinStorm,
		Cat, CaitSith, Grimalkin, CathPalug, CathPalugSith,
		Hellhound, HellhoundRime, AnnwnHound, AnnwnHoundRime, Pup, HellPup, AnnwnPup,
		BlackCat, BlackCatSith,
	}

	private enum State : byte { Heel, Chase, Crouch, Leap, Bite, Howl, BlinkOut, BlinkIn, Swoop, Catch, Rise }
	private enum BlinkWhy : byte { Arrive, Stuck, Leash, Inside, Dismiss }
	// What a strike leaves on the air for a moment: a hound's jaws, a cat's claws, a crow's beak.
	private enum SnapKind : byte { Jaws, Claws, Peck }

	private sealed class Hound
	{
		public AnimatedSprite2D Sprite;
		// What this creature is drawn as: the main hound's look for the first, the pack's for the rest
		// (Familiar.LookAt). Its cell and where its jaws are come from it.
		public Look Look;
		// The animation this creature's sprite is showing, kept here: reading AnimatedSprite2D.Animation
		// back builds a new managed StringName every call, and Show runs for every creature every frame.
		public StringName Anim;
		public Vector2 Pos;
		public State State;
		public float T;
		public Enemy Target;
		public bool HasBite;
		public float Retarget;
		public float Stuck;
		public float Inside;
		public float Facing = 1f;
		public Vector2 From;
		public Vector2 To;
		public float Duration;
		public BlinkWhy Why;
		public bool HowlNext;
		public bool Leaving;
		public float EmitClock;
		public readonly List<Enemy> SwoopHits = new(16);
		// The bolt it has gone for (State.Catch), and how long until it may go for another.
		public EnemyProjectile Bolt;
		public float CatchRest;
		// The cat: bound for the wizard's feet (after a pounce, for Purr, or with its grants running low),
		// and where it is wandering to meanwhile. The crow: where it is on its patrol ring.
		public bool Returning;
		public Vector2 WanderAt;
		public float WanderClock;
		public float Orbit;
	}

	private struct Mote
	{
		public Vector2 At;
		public Vector2 Vel;
		public float Age;
		public float Life;
		public Color Colour;
		public float Size;
		public bool Live;
	}

	private struct Snap
	{
		public Vector2 At;
		public float Radius;
		public float Age;
		public bool Live;
		public SnapKind Kind;
		public float Facing;
	}

	public Player PlayerRef { get; set; }
	public SpellData SpellData { get; private set; }

	private readonly List<Hound> hounds = new(MaxHounds);
	private readonly List<Enemy> nearBuffer = new(64);
	private readonly Mote[] motes = new Mote[MaxMotes];
	private readonly Snap[] snaps = new Snap[MaxSnaps];
	private int nextMote;
	private int nextSnap;
	private readonly RandomNumberGenerator rng = new();
	private Node2D fx;

	// The familiar as of the last cooldown.
	private Kind kind = Kind.Dog;
	private Look look = Look.Hound;
	// What every creature after the first is drawn as (ResolvePackLook): the main look, but for the dog's
	// pups and its pack under an Ascension.
	private Look packLook = Look.Hound;
	private bool lookSet;
	private int lastLevel = -1;
	private int damage = 12;
	private float biteRadius = 18f;
	private float huntRange = 200f;
	private float leapReach = 84f;
	private float areaMul = 1f;
	private bool threeHeads;
	private bool flies;
	private bool griffin;
	private bool annwn;
	private bool morrigan;
	// The cat's milestones as of the last cooldown, and its clocks (TickCat).
	private bool grimalkin;
	private bool caitSith;
	private bool purr;
	private bool blackCat;
	private float buffLeft;
	private float purrClock;
	private bool purrDue;
	private float blackCatClock;
	// Purr's heart over the wizard: its age (negative when none is showing) and where it rises from.
	private float heartAge = -1f;
	private Vector2 heartAt;
	private ElementColors.Ramp ramp = ElementColors.GetRamp(Element.Arcane);
	private Color tint = Colors.White;
	private Vector2 heelDir = new(-1f, 0.35f);

	// --- what the harness reads (scripts/_BalanceHarness.cs). Static: one run per process there. ---
	public static int StatBites { get; private set; }
	public static int StatLeaps { get; private set; }
	public static int StatSwoops { get; private set; }
	public static int StatCatches { get; private set; }
	public static int StatExecutions { get; private set; }
	public static int StatHaresCoursed { get; private set; }
	// The cat's work, which the strike counts above do not see: pounces landed (each a debuff), visits to the
	// wizard (each refreshing its grants), Purr's heals and the Black Cat's casts.
	public static int StatPounces { get; private set; }
	public static int StatVisits { get; private set; }
	public static int StatPurrHeals { get; private set; }
	public static int StatBlackCatCasts { get; private set; }
	public static int StatBlinksStuck { get; private set; }
	public static int StatBlinksLeash { get; private set; }
	public static int StatBlinksInside { get; private set; }
	public static float StatWorstStuck { get; private set; }
	public static long StatHoundFrames { get; private set; }
	public static long StatFramesOnSolid { get; private set; }
	// The same question asked of the terrain itself (TerrainStreamer, the data the collision is painted
	// from) rather than of the navigation the hound steers by - so a missing navigation window cannot
	// make "never on water or a wall" true by default. Debug builds only; a release build skips it.
	public static long StatNavFrames { get; private set; }
	public static long StatFramesOnSolidTerrain { get; private set; }
	private static readonly bool AuditTerrain = OS.IsDebugBuild();
	private static string statForm = "dog";

	/// <summary>One line for the balance report: is it striking, and did any creature get stuck.</summary>
	public static string StatsLine() =>
		$"familiar     : the {statForm}; {StatBites} strikes from {StatLeaps} leaps, {StatSwoops} swoops, {StatCatches} bolts caught, "
		+ $"{StatExecutions} executed, {StatHaresCoursed} Black Hares coursed; cat: {StatPounces} pounces, {StatVisits} visits, "
		+ $"{StatPurrHeals} Purr heals, {StatBlackCatCasts} Black Cat casts; blinks home - stuck {StatBlinksStuck}, "
		+ $"left behind {StatBlinksLeash}, on solid ground {StatBlinksInside}; worst stall {StatWorstStuck:0.00} s; "
		+ $"{StatFramesOnSolid} of {StatHoundFrames} walker-frames standing on water or a wall by the navigation "
		+ $"(present on {StatNavFrames}), {StatFramesOnSolidTerrain} by the terrain itself";

	/// <summary>
	/// What the familiar grants the wizard while it is out. Only the cat grants anything: every spell's
	/// cast speed (an attack speed share), his evasion, and with Purr his health a second. Read by
	/// Player.FireFamiliar every cooldown and held there as the live value - never added to a stored stat
	/// - so a grant can never be counted twice, and is gone the moment the spell is.
	/// </summary>
	public readonly record struct Grants(float CastSpeed, float Evasion)
	{
		public bool Any => CastSpeed > 0f || Evasion > 0f;
	}

	/// <summary>The grants the wizard has NOW: the cat's, while a visit's CatBuffSeconds last, else none.
	/// Player.FireFamiliar holds this every cooldown.</summary>
	public Grants LiveGrants(SpellData spell) => buffLeft > 0f ? GrantsOf(spell) : default;

	public static Grants GrantsOf(SpellData spell)
	{
		if (spell == null || FormOf(spell) != Kind.Cat)
			return default;
		int level = Math.Max(1, spell.CurrentLevel);
		float scale = 1f;
		if (spell.HasEvolution(GrimalkinId))
			scale *= GrimalkinGrants;
		if (spell.HasEvolution(CathPalugId))
			scale *= CathPalugGrants;
		float cast = (CatCastSpeed + spell.GetEffectValueAtLevel(SpellEffect.CastSpeed, level)) * scale;
		float evasion = (CatEvasion + spell.GetEffectValueAtLevel(SpellEffect.Evasion, level)) * scale;
		return new Grants(MathF.Max(0f, cast), MathF.Max(0f, evasion));
	}

	/// <summary>The grants as stat lines, for the pause screen's spell row (Node2DGame.BuildSpellRow).</summary>
	public static IEnumerable<string> GrantLines(SpellData spell)
	{
		Grants g = GrantsOf(spell);
		if (g.CastSpeed > 0f)
			yield return $"Cast speed +{Mathf.RoundToInt(g.CastSpeed * 100f)}%";
		if (g.Evasion > 0f)
			yield return $"Evasion +{Mathf.RoundToInt(g.Evasion * 100f)}%";
		if (spell != null && FormOf(spell) == Kind.Cat && spell.HasEvolution(PurrId))
			yield return $"Heal {PurrHeal} HP every {PurrSeconds:0} s";
	}

	public override void _Ready()
	{
		rng.Randomize();
		ResetDoorStats();
		// The creatures are children of this node, and y-sort nests: they sort with the bodies in the arena.
		YSortEnabled = true;
		ZIndex = 0;
		GlobalPosition = Vector2.Zero;
		// The effects: over the bodies, as ImpactDebrisLayer is; under the HUD.
		fx = new Node2D { Name = "FamiliarFx", ZAsRelative = false, ZIndex = DrawLayers.Air, YSortEnabled = false };
		// The pixel-art pass (2026-10-29): the motes, jaws, claws and pecks faded with alpha, translucent
		// over the bodies they bit; through SpellInk's dither the same alpha is the share of their pixels
		// drawn, each opaque. (This node stands on the world origin, so their cells are the world's.)
		fx.Material = SpellInk.DitherByCell;
		AddChild(fx);
		fx.Draw += DrawFx;
	}

	/// <summary>
	/// One cooldown: the familiar's numbers, its count, and a strike for every creature that has none.
	/// Called by the fire loop every cooldown (Player.FireFamiliar), which is what keeps it current
	/// through level-ups and milestones - and what makes the cooldown the strike rate.
	/// </summary>
	public void Summon(SpellData spell, int count, float playerArea, float damageMultiplier, Color visualTint)
	{
		if (spell == null)
			return;
		SpellData = spell;
		kind = FormOf(spell);
		statForm = kind.ToString().ToLowerInvariant();
		int level = Math.Max(1, spell.CurrentLevel);
		damage = Math.Max(1, Mathf.RoundToInt(spell.GetDamageAtLevel(level) * damageMultiplier));
		// Every area term once: the player's Area and every milestone's AreaMultiplier. It sizes the jaws
		// (and a Cerberus's reach, and a swoop's swath) - never the hunting range, which only levels move.
		areaMul = MathF.Max(0.25f, playerArea * spell.GetAreaMultiplierAtLevel(level));
		float strike = kind switch
		{
			Kind.Crow => CrowBeakRadius,
			Kind.Cat => CatClawRadius,
			_ => BaseBiteRadius,
		};
		biteRadius = MathF.Max(8f, strike * areaMul);
		huntRange = MathF.Max(80f, spell.GetReachAtLevel(level));
		leapReach = kind switch
		{
			Kind.Crow => CrowStoopReach,
			Kind.Cat => huntRange * CatLeapFraction,
			_ => huntRange * LeapReachFraction,
		};
		threeHeads = kind == Kind.Dog && spell.HasEvolution(CerberusId);
		grimalkin = kind == Kind.Cat && spell.HasEvolution(GrimalkinId);
		caitSith = kind == Kind.Cat && spell.HasEvolution(CaitSithId);
		bool hadPurr = purr;
		purr = kind == Kind.Cat && spell.HasEvolution(PurrId);
		// Tools only (_UiShot --purr-soon): the first heal a second and a half after Purr is taken.
		if (purr && !hadPurr && DebugPurrSoon)
			purrClock = PurrSeconds - 1.5f;
		blackCat = kind == Kind.Cat && spell.HasEvolution(BlackCatId);
		// The cat arrives at his heel, so it arrives giving its grants.
		if (kind == Kind.Cat && !lookSet)
			buffLeft = CatBuffSeconds;

		annwn = kind == Kind.Dog && spell.HasEvolution(AnnwnId);
		griffin = kind == Kind.Crow && spell.HasEvolution(GriffinId);
		morrigan = kind == Kind.Crow && spell.HasEvolution(MorriganId);
		// Ma Liang's Apotheosis on the crow (Familiar.PaintedDoor.cs).
		SetPaintedDoor(spell);
		flies = kind == Kind.Crow;
		ramp = ElementColors.GetRamp(spell.GetBranchElement() ?? Element.Arcane);
		tint = visualTint;

		Look wanted = ResolveLook(spell);
		Look wantedPack = ResolvePackLook(spell, wanted);
		bool changed = !lookSet || wanted != look || wantedPack != packLook;
		look = wanted;
		packLook = wantedPack;

		count = Math.Clamp(count, 1, MaxHounds);
		int live = 0;
		foreach (Hound h in hounds)
			if (!h.Leaving)
				live++;
		while (live < count)
		{
			AddHound(arriving: true);
			live++;
		}
		for (int i = hounds.Count - 1; i >= 0 && live > count; i--)
		{
			if (hounds[i].Leaving)
				continue;
			StartBlink(hounds[i], BlinkWhy.Dismiss);
			live--;
		}

		// Every creature in the look its place in the pack asks for: the first the main look, the rest the
		// pack's. Littermates adds two pups; an Ascension makes only the first
		// the legendary creature.
		for (int i = 0; i < hounds.Count; i++)
		{
			Look l = LookAt(i);
			if (hounds[i].Look != l || hounds[i].Sprite?.SpriteFrames == null)
				ApplyFrames(hounds[i], l);
		}
		// A new level or a new form is greeted with a howl; the first summoning arrives with one.
		if (lookSet && (changed || level != lastLevel))
			foreach (Hound h in hounds)
				h.HowlNext = true;
		// Summoned, or become something new: the one loud moment the familiar has.
		if (changed)
			SfxPlayer.Instance?.PlayAt(SfxCatalog.SpellSummon, PlayerRef?.GlobalPosition ?? GlobalPosition, 0.04f);
		lookSet = true;
		lastLevel = level;

		foreach (Hound h in hounds)
		{
			h.HasBite = true;
			if (h.Sprite != null)
				h.Sprite.SelfModulate = tint;
		}
	}

	private static Look ResolveLook(SpellData spell)
	{
		switch (FormOf(spell))
		{
			case Kind.Crow:
			{
				bool storm = spell.HasEvolution(StormcrowId);
				if (spell.HasEvolution(MorriganId))
					return storm ? Look.MorriganStorm : Look.Morrigan;
				if (spell.HasEvolution(GriffinId))
					return storm ? Look.GriffinStorm : Look.Griffin;
				if (spell.HasEvolution(RavenId))
					return Look.Raven;
				return storm ? Look.Stormcrow : Look.Crow;
			}
			case Kind.Cat:
			{
				bool sith = spell.HasEvolution(CaitSithId);
				if (spell.HasEvolution(CathPalugId))
					return sith ? Look.CathPalugSith : Look.CathPalug;
				if (spell.HasEvolution(BlackCatId))
					return sith ? Look.BlackCatSith : Look.BlackCat;
				if (spell.HasEvolution(GrimalkinId))
					return Look.Grimalkin;
				return sith ? Look.CaitSith : Look.Cat;
			}
			default:
			{
				bool rime = spell.HasEvolution(RimehoundId);
				if (spell.HasEvolution(CerberusId))
					return rime ? Look.CerberusRime : Look.Cerberus;
				if (spell.HasEvolution(AnnwnId))
					return rime ? Look.AnnwnRime : Look.Annwn;
				if (spell.HasEvolution(GreatHoundId))
					return Look.GreatHound;
				return rime ? Look.Rimehound : Look.Hound;
			}
		}
	}

	// THE PACK (Eric, 2026-10-30, of 0.9.2: "when ascending hound familiar, it should only make the main
	// hound into the full size legendary creature. littermates should produce two puppies instead of 2 of
	// the same dog. The two puppies should also be affected by the ascension, but the sprite should just look
	// like a similar-styled hellhound with one head, instead of the 3 headed cerberus. same goes for the
	// other ascension."). The first creature is the main one and wears ResolveLook; every other wears this.
	// With Littermates they are PUPS - the fawn hound's, a one-headed hellpup under Cerberus, a white
	// red-eared pup under the Hound of Annwn. Without it, the level 3 hound under an Ascension is a hound
	// of its own size in the Ascension's coat with one head. Only the drawing differs: every creature bites
	// with the Ascension's power, which Summon reads once for the whole pack (threeHeads, annwn). The crow
	// and the cat are unchanged - their pack wears their main look.
	private static Look ResolvePackLook(SpellData spell, Look main)
	{
		if (FormOf(spell) != Kind.Dog)
			return main;
		bool rime = spell.HasEvolution(RimehoundId);
		bool cerberus = spell.HasEvolution(CerberusId);
		bool hunt = spell.HasEvolution(AnnwnId);
		// Littermates and the Rimehound are both level 4 cards, so a pup is never a frost one.
		if (spell.HasEvolution(LittermatesId))
			return cerberus ? Look.HellPup : hunt ? Look.AnnwnPup : Look.Pup;
		if (cerberus)
			return rime ? Look.HellhoundRime : Look.Hellhound;
		if (hunt)
			return rime ? Look.AnnwnHoundRime : Look.AnnwnHound;
		return main;
	}

	// The look of the creature at `index` in the pack: the main look for the first, the pack's for the rest.
	// Creatures leave from the end of the list (Summon), so the first is the first summoned for the whole run.
	private Look LookAt(int index) => index == 0 ? look : packLook;

	/// <summary>The SpriteFrames each look loads, by the name tools/art/familiar.py writes.</summary>
	public static string FramesPath(string lookName) => $"res://scenes/resources/Familiar{lookName}Frames.tres";

	/// <summary>Every look's frames file, for RegressionChecks.ValidateFamiliar. In <see cref="Look"/> order.</summary>
	public static readonly string[] FormNames =
	{
		"Hound", "Rimehound", "Greathound", "Cerberus", "CerberusRime", "Annwn", "AnnwnRime",
		"Crow", "Stormcrow", "Raven", "Morrigan", "MorriganStorm", "Griffin", "GriffinStorm",
		"Cat", "CaitSith", "Grimalkin", "CathPalug", "CathPalugSith",
		"Hellhound", "HellhoundRime", "AnnwnHound", "AnnwnHoundRime", "Pup", "HellPup", "AnnwnPup",
		"BlackCat", "BlackCatSith",
	};

	// The cell each look is drawn on (tools/art/familiar.py FORMS): the hound, the crow, the cat and the
	// one-headed pack on the elite row, the pups on the basic row, Cerberus alone on the 96 (it is the
	// biggest thing at his side: "the main cerberus itself should be bigger"), every other larger look on 64.
	private static int CellOf(Look l) => l switch
	{
		Look.Hound or Look.Rimehound or Look.Crow or Look.Stormcrow or Look.Cat or Look.CaitSith => 48,
		Look.Hellhound or Look.HellhoundRime or Look.AnnwnHound or Look.AnnwnHoundRime => 48,
		_ when IsPup(l) => 32,
		Look.Cerberus or Look.CerberusRime => 96,
		_ => 64,
	};

	private static SpriteFrames LoadFrames(Look l) => GD.Load<SpriteFrames>(FramesPath(FormNames[(int)l]));

	// How far in front of its feet a creature's jaws (or claws) are, in world pixels: where to stand so the
	// strike lands on the quarry. Read off the drawings at x2. The crow strikes from above, on the quarry.
	private float JawReachOf(Look l) => l switch
	{
		Look.GreatHound or Look.Annwn or Look.AnnwnRime => 46f,
		// Cerberus on the 96 cell at 2.5 (it was 50 on the 64 at 1.72): its jaws 2.5 / 1.72 as far out.
		Look.Cerberus or Look.CerberusRime => 72f,
		_ when IsPup(l) => 20f,
		Look.Cat or Look.CaitSith => 22f,
		Look.Grimalkin => 28f,
		Look.BlackCat or Look.BlackCatSith => 30f,
		Look.CathPalug or Look.CathPalugSith => 40f,
		_ when flies => 0f,
		_ => 34f,
	};
	private const float GroundBelowCentre = 28f;

	private float WalkSpeed => kind == Kind.Cat ? CatRunSpeed : RunSpeed;
	private float AirSpeed => griffin ? CrowFlySpeed * GriffinFlyFactor : flies ? CrowFlySpeed : FlySpeed;
	private float Crouch => kind switch
	{
		Kind.Cat => CatRearSeconds,
		Kind.Crow => CrowClimbSeconds,
		_ => CrouchSeconds,
	};
	private float LeapTime => kind == Kind.Cat ? CatLeapSeconds : LeapSeconds;
	private float StrikeSeconds => kind switch
	{
		Kind.Crow => CrowPeckSeconds,
		Kind.Cat => CatSwipeSeconds,
		_ => BiteSeconds,
	};
	private float Leash => flies ? CrowBlinkDistance : BlinkDistance;
	private CatchRule Catching => kind switch
	{
		Kind.Crow => CrowCatch,
		Kind.Cat => CatCatch,
		_ => DogCatch,
	};

	private void AddHound(bool arriving)
	{
		var h = new Hound
		{
			Sprite = new AnimatedSprite2D
			{
				Name = "Hound",
				Scale = new Vector2(2f, 2f),
				TextureFilter = TextureFilterEnum.Nearest,
				// A blink fades the creature out and in (its modulate's alpha, _Process): through the
				// dither it loses and regains whole art pixels instead of going translucent.
				Material = SpellInk.DitherByTexel,
			},
		};
		Vector2 at = PlayerRef != null ? FreeHeelSpot(hounds.Count, MazeNavigation.Active) : Vector2.Zero;
		h.Pos = at;
		h.Sprite.Position = at;
		// Crows patrol from opposite sides of the ring.
		h.Orbit = hounds.Count * MathF.PI + 2.4f;
		AddChild(h.Sprite);
		hounds.Add(h);
		ApplyFrames(h, LookAt(hounds.Count - 1));
		if (arriving)
		{
			h.State = State.BlinkIn;
			h.T = 0f;
			h.HowlNext = true;
			Puff(at, inward: true);
		}
	}

	private void ApplyFrames(Hound h, Look l)
	{
		h.Look = l;
		SpriteFrames frames = LoadFrames(l);
		if (h.Sprite == null || frames == null)
			return;
		int cell = CellOf(l);
		h.Sprite.SpriteFrames = frames;
		// The feet row (cell - 2) sits on the node's origin, which is the creature's ground point (a
		// flier's is its shadow's, drawn on the bottom rows of its cell).
		h.Sprite.Offset = new Vector2(0f, -(cell * 0.5f - 1f));
		// A walker stands on the ground and is y-sorted among the bodies round it; a flier (the crow, the
		// griffin) is in the air over them (DrawLayers.Air), or every body south of its shadow would cut
		// it. Its shadow is drawn in its own cell, so over a body it falls on the body, as a shadow does.
		h.Sprite.ZAsRelative = !flies;
		h.Sprite.ZIndex = flies ? DrawLayers.Air : 0;
		h.Sprite.Play(AnimIdle);
		h.Anim = AnimIdle;
	}

	public override void _Process(double delta)
	{
		var perfStart = FrameProfiler.Begin();
		ProcessFamiliar(delta);
		FrameProfiler.End(FrameProfiler.Section.Familiar, perfStart);
	}

	private void ProcessFamiliar(double delta)
	{
		float dt = (float)delta;
		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;

		MazeNavigation nav = MazeNavigation.Active;
		Vector2 v = PlayerRef.Velocity;
		if (v.LengthSquared() > 900f)
			heelDir = heelDir.Lerp(-v.Normalized(), MathF.Min(1f, dt * 4f)).Normalized();

		if (kind == Kind.Cat)
			TickCat(dt);

		for (int i = hounds.Count - 1; i >= 0; i--)
		{
			Hound h = hounds[i];
			Tick(h, i, dt, nav);
			if (h.Leaving && h.State == State.BlinkOut && h.T >= BlinkSeconds)
			{
				h.Sprite?.QueueFree();
				hounds.RemoveAt(i);
			}
		}
		Separate(dt, nav);

		TerrainStreamer terrain = AuditTerrain ? Node2DGame.Current?.Terrain as TerrainStreamer : null;
		foreach (Hound h in hounds)
		{
			StatHoundFrames++;
			if (nav != null)
				StatNavFrames++;
			if (!flies && nav != null && nav.IsBlockedAt(h.Pos))
				StatFramesOnSolid++;
			if (!flies && terrain != null && h.State is not (State.BlinkOut or State.BlinkIn)
				&& (terrain.IsBlockedAtWorld(h.Pos) || terrain.IsWallAtWorld(h.Pos)))
				StatFramesOnSolidTerrain++;
			if (h.Sprite == null)
				continue;
			h.Sprite.Position = h.Pos;
			h.Sprite.FlipH = h.Facing < 0f;
			float alpha = h.State switch
			{
				State.BlinkOut => 1f - Mathf.Clamp(h.T / BlinkSeconds, 0f, 1f),
				State.BlinkIn => Mathf.Clamp(h.T / BlinkSeconds, 0f, 1f),
				_ => 1f,
			};
			h.Sprite.Modulate = new Color(1f, 1f, 1f, alpha);
		}

		TickDoors(dt);
		TickFx(dt);
		fx?.QueueRedraw();
	}

	// --- one creature ---------------------------------------------------------------------------------

	private void Tick(Hound h, int index, float dt, MazeNavigation nav)
	{
		h.T += dt;
		h.CatchRest = MathF.Max(0f, h.CatchRest - dt);
		Vector2 player = PlayerRef.GlobalPosition;
		Vector2 feet = PlayerRef.FootPosition;

		switch (h.State)
		{
			case State.Catch:
				TickCatch(h, nav);
				return;
			case State.BlinkOut:
				Show(h, AnimIdle, -1, 1f);
				if (h.T >= BlinkSeconds && !h.Leaving)
				{
					h.Pos = FreeHeelSpot(index, nav);
					Puff(h.Pos, inward: true);
					h.State = State.BlinkIn;
					h.T = 0f;
					h.Stuck = 0f;
					h.Inside = 0f;
					h.Target = null;
				}
				return;
			case State.BlinkIn:
				Show(h, AnimIdle, -1, 1f);
				if (h.T >= BlinkSeconds)
					Settle(h);
				return;
			case State.Howl:
				Show(h, AnimHowl, Mathf.Clamp((int)(h.T / HowlSeconds * 6f), 0, 5), 1f);
				EmitHowl(h, dt);
				if (h.T >= HowlSeconds || h.Pos.DistanceTo(feet) > 190f)
					Settle(h);
				return;
			case State.Crouch:
				Show(h, AnimPounce, 0, 1f);
				if (flies && Alive(h.Target))
				{
					// The crow's CLIMB: wings raised, it pulls up and back off its quarry before the stoop.
					Vector2 off = h.Pos - (h.Target.GlobalPosition + new Vector2(0f, GroundBelowCentre));
					if (off.LengthSquared() > 1f)
						h.Pos += off.Normalized() * (CrowClimbBack / CrowClimbSeconds) * dt;
				}
				if (h.T >= Crouch)
				{
					if (!Alive(h.Target))
					{
						Settle(h);
						return;
					}
					if (flies)
					{
						float rest = h.T - Crouch;
						StartStoop(h, h.Target);
						h.T = rest;
						return;
					}
					h.From = h.Pos;
					h.To = Landing(h.From, OpenSpot(h, h.Target, nav), nav);
					h.Duration = LeapTime;
					h.State = State.Leap;
					// Carry what is left of the frame into the leap: on a long frame (a loaded machine,
					// a phone) every phase taking a whole frame of its own stretched a bite to a second.
					h.T -= Crouch;
					StatLeaps++;
				}
				return;
			case State.Leap:
			{
				float t = Mathf.Clamp(h.T / h.Duration, 0f, 1f);
				if (Alive(h.Target))
					h.To = Landing(h.From, OpenSpot(h, h.Target, nav), nav);
				float eased = flies ? t * t : 1f - (1f - t) * (1f - t);
				h.Pos = h.From.Lerp(h.To, eased);
				Show(h, AnimPounce, 1 + Math.Min(2, (int)(t * 3f)), 1f);
				EmitTrail(h, dt);
				if (t >= 1f)
				{
					Bite(h);
					h.State = State.Bite;
					h.T -= h.Duration;
				}
				return;
			}
			case State.Bite:
				Show(h, AnimPounce, h.T < StrikeSeconds * 0.5f ? 4 : 5, 1f);
				if (h.T >= StrikeSeconds)
				{
					if (flies && !griffin)
					{
						h.State = State.Rise;
						h.T -= StrikeSeconds;
						return;
					}
					Settle(h);
				}
				return;
			case State.Rise:
			{
				// The crow RISES off its quarry, back up toward its ring round him.
				Vector2 out0 = h.Pos - feet;
				Vector2 ring = feet + (out0.LengthSquared() > 1f ? out0.Normalized() : new Vector2(-h.Facing, 0f)) * CrowPatrolRadius;
				Fly(h, ring, CrowRiseSpeed, dt);
				Show(h, AnimPounce, 5, 1f);
				if (h.T >= CrowRiseSeconds)
					Settle(h);
				return;
			}
			case State.Swoop:
				TickSwoop(h, dt);
				return;
		}

		// Heel or chase. First the leash: too far, stuck, or standing on something solid - blink home.
		float fromFeet = h.Pos.DistanceTo(feet);
		if (fromFeet > Leash)
		{
			StartBlink(h, BlinkWhy.Leash);
			return;
		}
		if (!flies)
		{
			if (h.Stuck >= StuckSeconds)
			{
				StartBlink(h, BlinkWhy.Stuck);
				return;
			}
			if (nav != null && nav.IsBlockedAt(h.Pos))
			{
				h.Inside += dt;
				if (h.Inside > 0.25f)
				{
					StartBlink(h, BlinkWhy.Inside);
					return;
				}
			}
			else
				h.Inside = 0f;
		}

		if (h.HowlNext)
		{
			h.HowlNext = false;
			h.State = State.Howl;
			h.T = 0f;
			return;
		}

		// A bolt coming at him comes before the quarry.
		if (h.CatchRest <= 0f && TryGoForBolt(h, player, nav))
			return;

		// The cat's visits come before anything: after a pounce, for Purr, or with its grants running low.
		if (kind == Kind.Cat && TickCatHome(h, feet, dt, nav))
			return;

		// Every form leaves his side only with a strike in hand (2026-10-31): the hound's run is short and it
		// comes straight back to heel, the crow patrols until it has a peck to dive with, the cat wanders until
		// it has a pounce. Without one there is no quarry.
		h.Retarget -= dt;
		if (!h.HasBite)
			h.Target = null;
		else if (h.Retarget <= 0f || !Alive(h.Target) || h.Target.GlobalPosition.DistanceTo(player) > huntRange * 1.3f)
		{
			h.Retarget = RetargetSeconds;
			h.Target = Acquire(h, player, nav);
		}

		if (h.Target == null)
		{
			h.State = State.Heel;
			if (flies)
			{
				Patrol(h, index, feet, dt);
				return;
			}
			if (kind == Kind.Cat)
			{
				Wander(h, index, feet, dt, nav);
				return;
			}
			Vector2 heel = HeelSpot(index);
			float speed = fromFeet > 160f ? SprintSpeed : WalkSpeed;
			if (Walk(h, heel, speed, dt, nav, homeward: true))
			{
				// At heel: face the way the wizard faces.
				if (PlayerRef.Velocity.LengthSquared() > 900f)
					h.Facing = PlayerRef.Velocity.X >= 0f ? 1f : -1f;
				Show(h, AnimIdle, -1, 1f);
			}
			else
				Show(h, AnimRun, -1, Mathf.Clamp(speed / WalkSpeed, 0.8f, 1.5f));
			return;
		}

		h.State = State.Chase;
		Enemy quarry = h.Target;
		if (flies)
		{
			Vector2 ground = quarry.GlobalPosition + new Vector2(0f, GroundBelowCentre);
			float dist = h.Pos.DistanceTo(ground);
			if (h.HasBite && griffin && dist <= leapReach * 2.4f)
			{
				StartSwoop(h, quarry);
				return;
			}
			if (h.HasBite && !griffin && dist <= leapReach)
			{
				// In reach: the CLIMB first (State.Crouch), then the stoop.
				h.Facing = ground.X >= h.Pos.X ? 1f : -1f;
				h.State = State.Crouch;
				h.T = 0f;
				return;
			}
			float holdOff = griffin ? leapReach * 1.6f : CrowHoldDistance;
			Vector2 away = (h.Pos - ground).LengthSquared() > 1f ? (h.Pos - ground).Normalized() : new Vector2(-h.Facing, 0f);
			Vector2 hold = ground + away * holdOff;
			if (Fly(h, h.HasBite ? ground : hold, AirSpeed, dt))
				Show(h, AnimIdle, -1, 1f);
			else
				Show(h, AnimRun, -1, 1f);
			return;
		}

		Vector2 spot = OpenSpot(h, quarry, nav);
		float toSpot = h.Pos.DistanceTo(spot);
		if (h.HasBite && toSpot <= leapReach && LeapClear(h.Pos, spot, nav))
		{
			h.Facing = quarry.GlobalPosition.X >= h.Pos.X ? 1f : -1f;
			h.State = State.Crouch;
			h.T = 0f;
			return;
		}
		if (!h.HasBite && toSpot < leapReach * 0.7f)
		{
			// Waiting on the next bite: hold off a little, facing it, ready.
			h.Facing = quarry.GlobalPosition.X >= h.Pos.X ? 1f : -1f;
			Show(h, AnimIdle, -1, 1.6f);
			return;
		}
		if (Walk(h, spot, WalkSpeed * 1.1f, dt, nav, homeward: false))
			Show(h, AnimIdle, -1, 1f);
		else
			Show(h, AnimRun, -1, 1.1f);
	}

	private void Settle(Hound h)
	{
		h.State = h.Target != null ? State.Chase : State.Heel;
		h.T = 0f;
	}

	private static bool Alive(Enemy e) =>
		e != null && IsInstanceValid(e) && !e.IsDying && e.InEnemyRegistry;

	// The quarry, inside the hunting range round the WIZARD, one no other creature has claimed if there is
	// one, and - for a walker - one it can walk to in a straight line (no lake or wall between them). What
	// it prefers is the form's:
	//   the dog (and the cat) the enemy nearest the wizard - they guard, not hunt;
	//   the Hound of Annwn an elite or a boss near the wizard before anything else, then as the dog;
	//   the crow an enemy that shoots, then whatever is nearest the crow itself - it ranges, and goes from
	//   one quarry to the next without coming home between.
	private Enemy Acquire(Hound h, Vector2 player, MazeNavigation nav)
	{
		EnemyRegistry.CollectNear(player, huntRange, nearBuffer);
		Enemy blockedA = null, blockedB = null;
		for (int attempt = 0; attempt < 3; attempt++)
		{
			Enemy best = null;
			float bestScore = float.MaxValue;
			float rangeSq = huntRange * huntRange;
			foreach (Enemy e in nearBuffer)
			{
				if (!Alive(e) || e == blockedA || e == blockedB)
					continue;
				float dPlayer = e.GlobalPosition.DistanceSquaredTo(player);
				if (dPlayer > rangeSq)
					continue;
				float score;
				if (flies)
					score = e.GlobalPosition.DistanceTo(h.Pos) - (e is RangedEnemy ? CrowRangedPull : 0f);
				else
					score = MathF.Sqrt(dPlayer) + 0.25f * e.GlobalPosition.DistanceTo(h.Pos) - (annwn && e.IsMiniBoss ? AnnwnElitePull : 0f);
				if (Claimed(h, e))
					score += 160f;
				if (score < bestScore)
				{
					bestScore = score;
					best = e;
				}
			}
			if (best == null || flies || nav == null || nav.HasClearWalk(h.Pos, OpenSpot(h, best, nav)))
			{
				nearBuffer.Clear();
				return best;
			}
			if (blockedA == null) blockedA = best;
			else blockedB = best;
		}
		nearBuffer.Clear();
		return null;
	}

	private bool Claimed(Hound me, Enemy e)
	{
		foreach (Hound h in hounds)
			if (h != me && h.Target == e)
				return true;
		return false;
	}

	// Where to stand so the jaws are on the quarry: beside it on its own ground line, on this side of it.
	// The side holds while the creature is nearly level with its quarry, or one passing under it would
	// swap sides every frame. A crow strikes from above: its spot is the quarry's own ground point.
	private Vector2 AttackSpot(Hound h, Enemy e)
	{
		if (flies)
			return e.GlobalPosition + new Vector2(0f, GroundBelowCentre);
		float dx = e.GlobalPosition.X - h.Pos.X;
		float side = dx > 6f ? -1f : dx < -6f ? 1f : -h.Facing;
		return e.GlobalPosition + new Vector2(side * JawReachOf(h.Look), GroundBelowCentre);
	}

	private static bool LeapClear(Vector2 from, Vector2 to, MazeNavigation nav) =>
		nav == null || (!nav.IsBlockedAt(to) && nav.HasClearWalk(from, to));

	// The spot to strike from, on open ground: AttackSpot, or the same spot on the quarry's other side when
	// that one is over water or a wall and the other is not - a quarry walking a shore is bitten from the
	// land, not from the tarn beside it.
	private Vector2 OpenSpot(Hound h, Enemy e, MazeNavigation nav)
	{
		Vector2 spot = AttackSpot(h, e);
		if (flies || nav == null || !nav.IsBlockedAt(spot))
			return spot;
		Vector2 other = new(2f * e.GlobalPosition.X - spot.X, spot.Y);
		return nav.IsBlockedAt(other) ? spot : other;
	}

	// Where a walker's leap from `from` for `want` comes down: `want` when it is open ground with a clear
	// line to it, otherwise the furthest point short of it on that line that is - the shore, not the tarn -
	// or `from` when none is. A flier comes down anywhere.
	//
	// The crouch asks LeapClear of its spot, but the spot moves with the quarry - once more at take-off and
	// every frame of the leap - and a bolt is tracked the same way; nothing asked again, so on the Broken
	// Circle a hound came down in a tarn several times a run and stood there until the blink fetched it.
	private Vector2 Landing(Vector2 from, Vector2 want, MazeNavigation nav)
	{
		if (flies || nav == null || LeapClear(from, want, nav))
			return want;
		for (int k = LandingSteps - 1; k >= 1; k--)
		{
			Vector2 p = from.Lerp(want, k / (float)LandingSteps);
			if (LeapClear(from, p, nav))
				return p;
		}
		return from;
	}

	private const int LandingSteps = 8;

	// Whether the quarry is in the jaws where the leap came down. A leap lands on the quarry's own spot, so
	// this holds but for one Landing cut short at a shore: then the jaws close on what is in front of them,
	// never on a quarry still a stride away.
	private bool InJaws(Hound h, Enemy quarry) => flies || h.Pos.DistanceTo(AttackSpot(h, quarry)) <= LandingSlack;

	private const float LandingSlack = 14f;

	private Vector2 HeelSpot(int index)
	{
		Vector2 feet = PlayerRef.FootPosition;
		Vector2 side = new Vector2(-heelDir.Y, heelDir.X);
		if (kind == Kind.Dog)
		{
			// THE DOG HEELS AT HIS SIDE (2026-10-31): the first hound on the side behind him as he walks, the
			// second on the other, each further out by its own bulk (Cerberus on its 96 cell stands half a cell
			// wider). The pups trail together outside the second hound, a pace further out and lower.
			float bulk = MathF.Max(0f, (CellOf(LookAt(index)) - 48) * 0.5f);
			float behindSide = heelDir.X <= 0f ? -1f : 1f;
			if (index >= 2 && IsPup(LookAt(index)))
				return feet + new Vector2(-behindSide * (DogHeelSide + 26f * (index - 1)), DogHeelDrop + 12f * (index - 1));
			float sign = index % 2 == 0 ? behindSide : -behindSide;
			return feet + new Vector2(sign * (DogHeelSide + bulk + 18f * (index / 2)), DogHeelDrop + 10f * (index / 2));
		}
		float behind = kind == Kind.Cat ? CatHeelBehind : HeelBehind;
		float beside = kind == Kind.Cat ? CatHeelSide : HeelSide;
		side *= (index % 2 == 0) ? 1f : -1f;
		return feet + heelDir * (behind + 10f * (index / 2)) + side * (beside + 14f * (index / 2));
	}

	private static bool IsPup(Look l) => l is Look.Pup or Look.HellPup or Look.AnnwnPup;

	// A heel spot on open ground: the creature's own, or the first of eight round the wizard that is.
	private Vector2 FreeHeelSpot(int index, MazeNavigation nav)
	{
		Vector2 want = HeelSpot(index);
		if (nav == null || flies || !nav.IsBlockedAt(want))
			return want;
		Vector2 feet = PlayerRef.FootPosition;
		for (int k = 0; k < 8; k++)
		{
			float a = k * Mathf.Tau / 8f;
			Vector2 p = feet + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 46f;
			if (!nav.IsBlockedAt(p))
				return p;
		}
		return feet;
	}

	// One step toward a goal on the ground. Never onto a blocked cell: it slides along one instead, and
	// on the way home it takes the navigation's route round water and walls. True once there.
	private bool Walk(Hound h, Vector2 goal, float speed, float dt, MazeNavigation nav, bool homeward)
	{
		Vector2 to = goal - h.Pos;
		float dist = to.Length();
		if (dist < 3f)
		{
			h.Stuck = MathF.Max(0f, h.Stuck - dt * 2f);
			return true;
		}
		Vector2 dir = to / dist;
		if (homeward && nav != null && !nav.HasClearWalk(h.Pos, goal))
		{
			Vector2 flow = nav.FlowDirectionAt(h.Pos);
			if (flow != Vector2.Zero)
				dir = flow;
		}
		float step = MathF.Min(speed * dt, dist);
		Vector2 next = h.Pos + dir * step;
		if (nav != null && nav.IsBlockedAt(next))
		{
			Vector2 alongX = h.Pos + new Vector2(MathF.Sign(dir.X) * step, 0f);
			Vector2 alongY = h.Pos + new Vector2(0f, MathF.Sign(dir.Y) * step);
			if (MathF.Abs(dir.X) > 0.05f && !nav.IsBlockedAt(alongX))
				next = alongX;
			else if (MathF.Abs(dir.Y) > 0.05f && !nav.IsBlockedAt(alongY))
				next = alongY;
			else
			{
				h.Stuck += dt;
				StatWorstStuck = MathF.Max(StatWorstStuck, h.Stuck);
				return false;
			}
		}
		h.Stuck = MathF.Max(0f, h.Stuck - dt);
		if (MathF.Abs(next.X - h.Pos.X) > 0.05f)
			h.Facing = next.X > h.Pos.X ? 1f : -1f;
		h.Pos = next;
		return false;
	}

	// A flier: straight there, over anything.
	private bool Fly(Hound h, Vector2 goal, float speed, float dt)
	{
		Vector2 to = goal - h.Pos;
		float dist = to.Length();
		if (dist < 4f)
			return true;
		Vector2 step = to / dist * MathF.Min(speed * dt, dist);
		if (MathF.Abs(step.X) > 0.05f)
			h.Facing = step.X > 0f ? 1f : -1f;
		h.Pos += step;
		return false;
	}

	// Creatures do not stand in each other.
	private void Separate(float dt, MazeNavigation nav)
	{
		for (int i = 0; i < hounds.Count; i++)
		{
			Hound a = hounds[i];
			if (a.State is State.Leap or State.Swoop or State.BlinkOut)
				continue;
			for (int j = i + 1; j < hounds.Count; j++)
			{
				Hound b = hounds[j];
				if (b.State is State.Leap or State.Swoop or State.BlinkOut)
					continue;
				Vector2 d = a.Pos - b.Pos;
				float l = d.Length();
				const float Gap = 22f;
				if (l >= Gap || l < 0.01f)
					continue;
				Vector2 push = d / l * (Gap - l) * MathF.Min(1f, dt * 8f) * 0.5f;
				Nudge(a, push, nav);
				Nudge(b, -push, nav);
			}
		}
	}

	private void Nudge(Hound h, Vector2 by, MazeNavigation nav)
	{
		Vector2 next = h.Pos + by;
		if (flies || nav == null || !nav.IsBlockedAt(next))
			h.Pos = next;
	}

	private void StartBlink(Hound h, BlinkWhy why)
	{
		if (h.State == State.BlinkOut)
			return;
		ReleaseBolt(h);
		switch (why)
		{
			case BlinkWhy.Stuck: StatBlinksStuck++; break;
			case BlinkWhy.Leash: StatBlinksLeash++; break;
			case BlinkWhy.Inside: StatBlinksInside++; break;
		}
		h.Why = why;
		h.Leaving = why == BlinkWhy.Dismiss;
		h.State = State.BlinkOut;
		h.T = 0f;
		Puff(h.Pos, inward: false);
	}

	// --- the strike -----------------------------------------------------------------------------------

	// The crow's stoop: from where it is, down onto its quarry's ground point, folding its wings - the
	// leap's state and frames, with no crouch before it and a short flight that speeds up as it falls.
	private void StartStoop(Hound h, Enemy quarry)
	{
		Vector2 ground = quarry.GlobalPosition + new Vector2(0f, GroundBelowCentre);
		h.From = h.Pos;
		h.To = ground;
		h.Duration = Mathf.Clamp(h.From.DistanceTo(ground) / CrowStoopSpeed, 0.06f, 0.2f);
		h.Facing = ground.X >= h.Pos.X ? 1f : -1f;
		h.State = State.Leap;
		h.T = 0f;
		StatLeaps++;
	}

	private void Bite(Hound h)
	{
		if (kind == Kind.Cat)
		{
			CatPounce(h);
			return;
		}
		h.HasBite = false;
		if (PlayerRef == null || SpellData == null)
			return;
		Enemy quarry = Alive(h.Target) && InJaws(h, h.Target) ? h.Target : null;
		Vector2 centre = quarry?.GlobalPosition ?? h.Pos + new Vector2(h.Facing * JawReachOf(h.Look), -GroundBelowCentre);
		StatBites++;
		BiteAt(centre, quarry, h.Facing);
		if (threeHeads)
		{
			// The other two heads take the two enemies nearest the quarry.
			float reach = CerberusReach * areaMul;
			Enemy second = null, third = null;
			float d2 = float.MaxValue, d3 = float.MaxValue;
			EnemyRegistry.CollectNear(centre, reach, nearBuffer);
			foreach (Enemy e in nearBuffer)
			{
				if (!Alive(e) || e == quarry)
					continue;
				float d = e.GlobalPosition.DistanceSquaredTo(centre);
				if (d > reach * reach)
					continue;
				if (d < d2)
				{
					third = second; d3 = d2;
					second = e; d2 = d;
				}
				else if (d < d3)
				{
					third = e; d3 = d;
				}
			}
			nearBuffer.Clear();
			if (second != null)
				BiteAt(second.GlobalPosition, second, h.Facing);
			if (third != null)
				BiteAt(third.GlobalPosition, third, h.Facing);
		}
		SfxPlayer.Instance?.PlayAt(SfxCatalog.SpellFamiliarBite, centre, 0.08f, SfxCatalog.SpellFamiliarBite, SoundGapMs);
		// The Painted Door: where the crow's peck lands, a door (Familiar.PaintedDoor.cs).
		if (kind == Kind.Crow)
			TryPaintDoor(centre);
		// The strike done, it lets go of its quarry: a hound comes straight back to heel and a crow rises back
		// to its ring, and each takes a new quarry with its next strike.
		h.Target = null;
	}

	// --- the cat (2026-10-31): it never deals damage --------------------------------------------------

	// The pounce lands a DEBUFF, not a blow: the quarry and whatever else is under the cat's paws (its claws'
	// radius - Cath Palug's is wide) are Weakened and checked in their stride, and after the Cait Sith made
	// Vulnerable. Then it trots back to the wizard (Returning) to give him its grants.
	private void CatPounce(Hound h)
	{
		h.HasBite = false;
		if (PlayerRef == null || SpellData == null)
			return;
		Enemy quarry = Alive(h.Target) && InJaws(h, h.Target) ? h.Target : null;
		Vector2 centre = quarry?.GlobalPosition ?? h.Pos + new Vector2(h.Facing * JawReachOf(h.Look), -GroundBelowCentre);
		StatPounces++;
		if (quarry != null)
			Mark(quarry);
		float r2 = biteRadius * biteRadius;
		EnemyRegistry.CollectNear(centre, biteRadius, nearBuffer);
		foreach (Enemy e in nearBuffer)
			if (e != quarry && Alive(e) && e.GlobalPosition.DistanceSquaredTo(centre) <= r2)
				Mark(e);
		nearBuffer.Clear();
		// The mark: the cat's light thrown out from where it landed, to the edge of what it marked.
		for (int i = 0; i < 10; i++)
		{
			float a = i * Mathf.Tau / 10f;
			var dir = new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.7f);
			AddMote(centre + dir * 4f, dir * biteRadius / 0.3f, 0.3f, i % 2 == 0 ? ramp.Core : ramp.Mid, 2f);
		}
		h.Target = null;
		h.Returning = true;
	}

	private void Mark(Enemy e)
	{
		e.ApplyWeaken(CatWeakenFraction, CatWeakenSeconds * (grimalkin ? GrimalkinGrants : 1f));
		e.ApplySlow(CatSlowMultiplier, CatSlowSeconds);
		if (caitSith)
			e.ApplyVulnerable(CaitSithVulnerable, CaitSithVulnerableSeconds);
	}

	// Home to his feet when a visit is due; true while it is going (nothing else this frame).
	private bool TickCatHome(Hound h, Vector2 feet, float dt, MazeNavigation nav)
	{
		if (buffLeft < CatBuffLow || purrDue)
			h.Returning = true;
		if (!h.Returning)
			return false;
		h.Target = null;
		h.State = State.Heel;
		if (h.Pos.DistanceTo(feet) <= CatTouch)
		{
			Visit(h);
			Show(h, AnimIdle, -1, 1f);
			return true;
		}
		if (Walk(h, feet + heelDir * 16f, CatRunSpeed, dt, nav, homeward: true))
			Visit(h);
		Show(h, AnimRun, -1, 1.1f);
		return true;
	}

	// A visit: the grants on him for CatBuffSeconds, a few motes of its light round him - and, when Purr's
	// heal is due, the heal and a heart over his head.
	private void Visit(Hound h)
	{
		h.Returning = false;
		buffLeft = CatBuffSeconds;
		StatVisits++;
		Vector2 head = PlayerRef.GlobalPosition + new Vector2(0f, -26f);
		for (int i = 0; i < 6; i++)
		{
			float a = -MathF.PI * (i + 0.5f) / 6f;
			var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
			AddMote(head + dir * 10f, dir * 34f, 0.4f, i % 2 == 0 ? ramp.Core : ramp.Hot, 2f);
		}
		if (purrDue)
		{
			purrDue = false;
			purrClock = 0f;
			PlayerRef.Heal(PurrHeal);
			StatPurrHeals++;
			heartAge = 0f;
		}
	}

	// Nothing to pounce on: it wanders near him, a new spot every couple of seconds.
	private void Wander(Hound h, int index, Vector2 feet, float dt, MazeNavigation nav)
	{
		h.WanderClock -= dt;
		if (h.WanderClock <= 0f || h.WanderAt.DistanceTo(feet) > CatWanderMax * 1.4f)
		{
			h.WanderClock = CatWanderSeconds * rng.RandfRange(0.7f, 1.3f);
			float a = rng.Randf() * Mathf.Tau;
			h.WanderAt = feet + new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.7f) * rng.RandfRange(CatWanderMin, CatWanderMax);
			if (nav != null && nav.IsBlockedAt(h.WanderAt))
				h.WanderAt = FreeHeelSpot(index, nav);
		}
		float speed = h.Pos.DistanceTo(feet) > CatWanderMax * 1.6f ? CatRunSpeed : CatRunSpeed * 0.45f;
		if (Walk(h, h.WanderAt, speed, dt, nav, homeward: false))
			Show(h, AnimIdle, -1, 1f);
		else
			Show(h, AnimRun, -1, speed < CatRunSpeed ? 0.8f : 1.1f);
	}

	// The crow with no peck in hand: a loose ring round him, two crows the opposite ways round.
	private void Patrol(Hound h, int index, Vector2 feet, float dt)
	{
		h.Orbit += dt * CrowPatrolTurn * (index % 2 == 0 ? 1f : -1f);
		float r = CrowPatrolRadius + CrowPatrolWobble * MathF.Sin(h.Orbit * 1.7f + index * 2f);
		Vector2 at = feet + new Vector2(MathF.Cos(h.Orbit), MathF.Sin(h.Orbit) * 0.7f) * r;
		Fly(h, at, AirSpeed * 0.6f, dt);
		Show(h, AnimRun, -1, 1f);
	}

	// The cat's clocks: its grants running down, Purr's heal coming due, the Black Cat's next cast.
	private void TickCat(float dt)
	{
		buffLeft = MathF.Max(0f, buffLeft - dt);
		if (heartAge >= 0f)
		{
			heartAge += dt;
			if (heartAge >= HeartSeconds)
				heartAge = -1f;
		}
		if (purr && !purrDue)
		{
			purrClock += dt;
			if (purrClock >= PurrSeconds)
				purrDue = true;
		}
		if (!blackCat || hounds.Count == 0)
			return;
		blackCatClock += dt;
		if (blackCatClock < BlackCatSeconds)
			return;
		Hound cat = hounds[0];
		if (cat.State is State.BlinkOut or State.BlinkIn || PlayerRef.IsDead)
			return;
		SpellData cast = PlayerRef.PickBlackCatSpell(rng);
		// The wizard's spells aim and spawn from his body's centre, the cat stands on its feet: the same offset.
		Vector2 origin = cat.Pos + (PlayerRef.GlobalPosition - PlayerRef.FootPosition);
		if (cast != null && PlayerRef.CastFromFamiliar(cast, origin))
		{
			blackCatClock = 0f;
			StatBlackCatCasts++;
			for (int i = 0; i < 8; i++)
			{
				float a = -MathF.PI * (i + 0.5f) / 8f;
				var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
				AddMote(cat.Pos + new Vector2(0f, -20f) + dir * 6f, dir * 40f, 0.35f, i % 2 == 0 ? ramp.Core : ramp.Hot, 2f);
			}
		}
		else
			// Nothing it could cast at (no spell in reach): try again shortly rather than a whole interval on.
			blackCatClock = BlackCatSeconds - 0.4f;
	}

	// PURR'S HEART: a small pixel heart that pops over the wizard's head when the heal lands, rises and fades.
	private const float HeartSeconds = 0.9f;
	private static readonly string[] HeartRows = { ".##.##.", "#######", "#######", ".#####.", "..###..", "...#..." };

	private void DrawHeart()
	{
		if (heartAge < 0f || PlayerRef == null)
			return;
		float t = heartAge / HeartSeconds;
		// It pops: one cell larger for the first tenth of a second, then its own size, rising as it fades.
		float cell = t < 0.12f ? 3f : 2f;
		float alpha = t < 0.6f ? 1f : (1f - t) / 0.4f;
		Vector2 origin = PlayerRef.GlobalPosition + new Vector2(0f, -54f - 16f * t);
		Color body = new(0.847f, 0.141f, 0.180f, alpha);   // ember base #D8242E: the wizard's own red
		Color hi = new(1f, 0.753f, 0.659f, alpha);          // ember hi #FFC0A8
		Color dark = new(0.541f, 0.071f, 0.125f, alpha);    // ember shade #8A1220
		for (int r = 0; r < HeartRows.Length; r++)
			for (int c = 0; c < HeartRows[r].Length; c++)
			{
				if (HeartRows[r][c] != '#')
					continue;
				Color col = (r == 1 && c == 1) ? hi : (r >= 3 && c >= 4 ? dark : body);
				fx.DrawRect(new Rect2(origin + new Vector2((c - 3.5f) * cell, (r - 3f) * cell), new Vector2(cell, cell)), col);
			}
	}

	// One strike landing at `centre`: the quarry, and everything else whose centre is inside the jaws.
	private void BiteAt(Vector2 centre, Enemy quarry, float facing)
	{
		if (quarry != null)
			Strike(quarry, damage, quarry: true);
		float r2 = biteRadius * biteRadius;
		EnemyRegistry.CollectNear(centre, biteRadius, nearBuffer);
		foreach (Enemy e in nearBuffer)
		{
			if (e == quarry || !Alive(e) || e.GlobalPosition.DistanceSquaredTo(centre) > r2)
				continue;
			Strike(e, damage, quarry: false);
		}
		nearBuffer.Clear();
		AddSnap(centre, biteRadius, kind switch { Kind.Crow => SnapKind.Peck, Kind.Cat => SnapKind.Claws, _ => SnapKind.Jaws }, facing);
		if (GetParent() is Node2D arena)
			ImpactDebrisLayer.Spawn(arena, centre, biteRadius, ramp, tint);
	}

	// One enemy struck. What only some forms do happens here, so a Cerberus head, a jaw's bystander and a
	// swoop's every victim are all treated alike: the Hound of Annwn's double on an elite, the Morrigan's
	// kill on the nearly dead - and the coursing, a Black Hare a hound brought down.
	private void Strike(Enemy e, int amount, bool quarry)
	{
		bool wasAlive = Alive(e);
		if (annwn && e.IsMiniBoss)
			amount = Mathf.RoundToInt(amount * AnnwnEliteMultiplier);
		if (morrigan && quarry && e.HealthFraction <= MorriganExecuteFraction)
		{
			if (e.IsMiniBoss)
				amount = Mathf.RoundToInt(amount * MorriganEliteMultiplier);
			else
			{
				// The battle-crow chooses who falls: the peck is the rest of its health. Through the ordinary
				// pipeline, so it is still this spell's damage, its kill, its riders.
				amount = Math.Max(amount, e.Health + 1);
				StatExecutions++;
			}
		}
		PlayerRef.DealDamageToEnemy(e, amount, source: SpellData);
		if (wasAlive && kind == Kind.Dog && e.IsDying && e.EnemyType == CoursedEnemyType)
		{
			StatHaresCoursed++;
			RunEvents.RecordHareCoursed();
		}
	}

	// --- catching bolts --------------------------------------------------------------------------------

	private bool TryGoForBolt(Hound h, Vector2 player, MazeNavigation nav)
	{
		CatchRule rule = Catching;
		EnemyProjectile best = null;
		float bestToPlayer = float.MaxValue;
		Vector2 bestAt = Vector2.Zero;
		IReadOnlyList<EnemyProjectile> bolts = EnemyProjectile.Live;
		for (int i = 0; i < bolts.Count; i++)
		{
			EnemyProjectile bolt = bolts[i];
			if (bolt == null || !IsInstanceValid(bolt) || !bolt.InFlight || bolt.ClaimedBy != null)
				continue;
			Vector2 toPlayer = player - bolt.GlobalPosition;
			float distance = toPlayer.Length();
			if (distance > rule.Watch || distance < 1f)
				continue;
			if (bolt.Heading.Dot(toPlayer / distance) < CatchThreatCosine)
				continue;
			Vector2 meet = bolt.GlobalPosition + bolt.Heading * bolt.Speed * CatchLeapSeconds;
			if (h.Pos.DistanceTo(meet) > rule.Reach || distance >= bestToPlayer)
				continue;
			// A bolt flies over a tarn; a walker does not leap into one after it.
			if (!flies && !LeapClear(h.Pos, meet, nav))
				continue;
			best = bolt;
			bestToPlayer = distance;
			bestAt = meet;
		}
		if (best == null)
			return false;

		best.ClaimedBy = h;
		h.Bolt = best;
		h.From = h.Pos;
		h.To = bestAt;
		h.Duration = CatchLeapSeconds;
		h.Facing = bestAt.X >= h.Pos.X ? 1f : -1f;
		h.State = State.Catch;
		h.T = 0f;
		return true;
	}

	private void TickCatch(Hound h, MazeNavigation nav)
	{
		EnemyProjectile bolt = h.Bolt;
		bool flying = bolt != null && IsInstanceValid(bolt) && bolt.InFlight;
		float t = Mathf.Clamp(h.T / h.Duration, 0f, 1f);
		if (flying)
			h.To = Landing(h.From, bolt.GlobalPosition + bolt.Heading * bolt.Speed * MathF.Max(0f, h.Duration - h.T), nav);
		float eased = 1f - (1f - t) * (1f - t);
		h.Pos = h.From.Lerp(h.To, eased);
		Show(h, AnimPounce, 1 + Math.Min(2, (int)(t * 3f)), 1f);
		EmitTrail(h, 1f / 60f);

		if (flying && h.Pos.DistanceTo(bolt.GlobalPosition) <= CatchJaws)
		{
			Vector2 at = bolt.GlobalPosition;
			bolt.Destroy(SpellData?.GetBranchElement() ?? Element.Arcane);
			h.Bolt = null;
			h.CatchRest = Catching.Rest;
			StatCatches++;
			AddSnap(at, CatchJaws, kind switch { Kind.Crow => SnapKind.Peck, Kind.Cat => SnapKind.Claws, _ => SnapKind.Jaws }, h.Facing);
			SfxPlayer.Instance?.PlayAt(SfxCatalog.SpellFamiliarBite, at, 0.08f, SfxCatalog.SpellFamiliarBite, SoundGapMs);
			h.State = State.Bite;
			h.T = 0f;
			return;
		}
		if (!flying || t >= 1f)
		{
			// Missed, or it was gone first: back to heel, and a short rest so it does not lunge again
			// at the same bolt from behind.
			ReleaseBolt(h);
			h.CatchRest = Catching.Rest * 0.4f;
			Settle(h);
		}
	}

	private static void ReleaseBolt(Hound h)
	{
		if (h.Bolt != null && IsInstanceValid(h.Bolt) && h.Bolt.ClaimedBy == h)
			h.Bolt.ClaimedBy = null;
		h.Bolt = null;
	}

	// --- the griffin's swoop ---------------------------------------------------------------------------

	private void StartSwoop(Hound h, Enemy quarry)
	{
		Vector2 ground = quarry.GlobalPosition + new Vector2(0f, GroundBelowCentre);
		Vector2 dir = ground - h.Pos;
		if (dir.LengthSquared() < 1f)
			dir = new Vector2(h.Facing, 0f);
		dir = dir.Normalized();
		h.From = h.Pos;
		h.To = ground + dir * SwoopOvershoot;
		h.Duration = Mathf.Clamp(h.From.DistanceTo(h.To) / SwoopSpeed, 0.18f, 0.6f);
		h.Facing = dir.X >= 0f ? 1f : -1f;
		h.SwoopHits.Clear();
		h.HasBite = false;
		h.State = State.Swoop;
		h.T = 0f;
		StatSwoops++;
	}

	private void TickSwoop(Hound h, float dt)
	{
		float t = Mathf.Clamp(h.T / h.Duration, 0f, 1f);
		Vector2 prev = h.Pos;
		h.Pos = h.From.Lerp(h.To, t);
		Show(h, AnimPounce, Math.Min(5, 1 + (int)(t * 4.5f)), 1f);
		StrikeAlong(h, prev, h.Pos);
		EmitFeathers(h, prev, h.Pos, dt);
		if (t >= 1f)
		{
			h.State = State.Heel;
			h.T = 0f;
			h.Target = null;
			h.Retarget = 0f;
		}
	}

	// Everything whose centre is within the swath of the stretch flown this frame, once per swoop. The
	// swoop flies on the ground line; an enemy's ground point is its centre plus GroundBelowCentre.
	private void StrikeAlong(Hound h, Vector2 a, Vector2 b)
	{
		if (PlayerRef == null || SpellData == null)
			return;
		float half = SwoopHalfWidth * areaMul + RiptideWave.BodyAllowance;
		Vector2 mid = (a + b) * 0.5f - new Vector2(0f, GroundBelowCentre);
		float reach = a.DistanceTo(b) * 0.5f + half;
		EnemyRegistry.CollectNear(mid, reach, nearBuffer);
		Vector2 ab = b - a;
		float ll = MathF.Max(1e-3f, ab.LengthSquared());
		foreach (Enemy e in nearBuffer)
		{
			if (!Alive(e) || h.SwoopHits.Contains(e))
				continue;
			Vector2 p = e.GlobalPosition + new Vector2(0f, GroundBelowCentre);
			float u = Mathf.Clamp((p - a).Dot(ab) / ll, 0f, 1f);
			if ((a + ab * u).DistanceSquaredTo(p) > half * half)
				continue;
			h.SwoopHits.Add(e);
			// The griffin's swoop paints its door at the first body it strikes (Familiar.PaintedDoor.cs).
			if (h.SwoopHits.Count == 1)
				TryPaintDoor(e.GlobalPosition);
			Strike(e, damage, quarry: false);
			StatBites++;
			if (GetParent() is Node2D arena)
				ImpactDebrisLayer.Spawn(arena, e.GlobalPosition, MathF.Max(10f, half * 0.6f), ramp, tint);
			SfxPlayer.Instance?.PlayAt(SfxCatalog.SpellFamiliarBite, e.GlobalPosition, 0.08f, SfxCatalog.SpellFamiliarBite, SoundGapMs);
		}
		nearBuffer.Clear();
	}

	// --- animation -------------------------------------------------------------------------------------

	private static void Show(Hound h, StringName anim, int frame, float speed)
	{
		AnimatedSprite2D s = h.Sprite;
		if (s == null)
			return;
		if (frame < 0)
		{
			if (!ReferenceEquals(h.Anim, anim) || !s.IsPlaying())
			{
				s.Play(anim);
				h.Anim = anim;
			}
			s.SpeedScale = speed;
			return;
		}
		if (!ReferenceEquals(h.Anim, anim))
		{
			s.Animation = anim;
			h.Anim = anim;
		}
		if (s.IsPlaying())
			s.Pause();
		s.Frame = frame;
	}

	// --- effects: fixed pools, drawn on the 2 px grid ----------------------------------------------------

	private void AddMote(Vector2 at, Vector2 vel, float life, Color colour, float size)
	{
		motes[nextMote] = new Mote { At = at, Vel = vel, Life = life, Colour = colour, Size = size, Live = true };
		nextMote = (nextMote + 1) % MaxMotes;
	}

	private void AddSnap(Vector2 at, float radius, SnapKind snapKind, float facing)
	{
		snaps[nextSnap] = new Snap { At = at, Radius = radius, Live = true, Kind = snapKind, Facing = facing < 0f ? -1f : 1f };
		nextSnap = (nextSnap + 1) % MaxSnaps;
	}

	// A blink: a puff of the creature's own light, flying out from where it vanished or in to where it comes.
	private void Puff(Vector2 at, bool inward)
	{
		Vector2 body = at - new Vector2(0f, flies ? 30f : 16f);
		for (int i = 0; i < 12; i++)
		{
			float a = i * Mathf.Tau / 12f + rng.Randf() * 0.4f;
			var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
			float d = rng.RandfRange(18f, 30f);
			Color c = i % 3 == 0 ? ramp.Core : i % 3 == 1 ? ramp.Hot : ramp.Mid;
			if (inward)
				AddMote(body + dir * d, -dir * d / 0.22f, 0.22f, c, 4f);
			else
				AddMote(body + dir * 4f, dir * d / 0.26f, 0.26f, c, 4f);
		}
	}

	private void EmitTrail(Hound h, float dt)
	{
		h.EmitClock += dt;
		while (h.EmitClock >= 0.03f)
		{
			h.EmitClock -= 0.03f;
			Vector2 back = h.Pos + new Vector2(-h.Facing * rng.RandfRange(6f, 14f), -rng.RandfRange(14f, 26f));
			AddMote(back, new Vector2(-h.Facing * 30f, -20f), 0.24f, rng.Randf() < 0.5f ? ramp.Hot : ramp.Mid, 2f);
		}
	}

	private void EmitHowl(Hound h, float dt)
	{
		h.EmitClock += dt;
		while (h.EmitClock >= 0.07f)
		{
			h.EmitClock -= 0.07f;
			float head = CellOf(h.Look) * 0.8f;
			Vector2 at = h.Pos + new Vector2(h.Facing * rng.RandfRange(4f, 14f), -head - rng.RandfRange(0f, 8f));
			AddMote(at, new Vector2(h.Facing * rng.RandfRange(6f, 18f), -rng.RandfRange(40f, 70f)), 0.5f,
				rng.Randf() < 0.5f ? ramp.Core : ramp.Hot, rng.Randf() < 0.3f ? 4f : 2f);
		}
	}

	// The swoop sheds feathers of the wizard's indigo and its own light, inside the swath it struck.
	private void EmitFeathers(Hound h, Vector2 a, Vector2 b, float dt)
	{
		float half = SwoopHalfWidth * areaMul;
		Vector2 ab = b - a;
		if (ab.LengthSquared() < 1f)
			return;
		Vector2 n = new Vector2(-ab.Y, ab.X).Normalized();
		int count = Math.Max(1, (int)(ab.Length() / 10f));
		for (int i = 0; i < count; i++)
		{
			Vector2 p = a.Lerp(b, rng.Randf()) - new Vector2(0f, GroundBelowCentre) + n * rng.RandfRange(-half, half);
			Color c = (i % 3) switch { 0 => ramp.Hot, 1 => new Color(0.42f, 0.53f, 0.88f), _ => ramp.Mid };
			AddMote(p, n * rng.RandfRange(-16f, 16f) + new Vector2(0f, 12f), 0.35f, c, i % 4 == 0 ? 4f : 2f);
		}
	}

	private void TickFx(float dt)
	{
		for (int i = 0; i < MaxMotes; i++)
		{
			if (!motes[i].Live)
				continue;
			motes[i].Age += dt;
			motes[i].At += motes[i].Vel * dt;
			if (motes[i].Age >= motes[i].Life)
				motes[i].Live = false;
		}
		for (int i = 0; i < MaxSnaps; i++)
		{
			if (!snaps[i].Live)
				continue;
			snaps[i].Age += dt;
			if (snaps[i].Age >= SnapSeconds)
				snaps[i].Live = false;
		}
	}

	private const float SnapSeconds = 0.18f;
	private const int SnapTeeth = 5;

	private void DrawFx()
	{
		DrawHeart();
		for (int i = 0; i < MaxMotes; i++)
		{
			ref Mote m = ref motes[i];
			if (!m.Live)
				continue;
			float f = m.Age / m.Life;
			Color c = new Color(m.Colour.R * tint.R, m.Colour.G * tint.G, m.Colour.B * tint.B, f < 0.6f ? 1f : (1f - f) / 0.4f);
			fx.DrawRect(WhipLimb.Cell(m.At, m.Size), c);
		}
		for (int i = 0; i < MaxSnaps; i++)
		{
			ref Snap s = ref snaps[i];
			if (!s.Live)
				continue;
			switch (s.Kind)
			{
				case SnapKind.Claws: DrawClaws(ref s); break;
				case SnapKind.Peck: DrawPeck(ref s); break;
				default: DrawJaws(ref s); break;
			}
		}
	}

	private Color Lit(Color c, float alpha) => new(c.R * tint.R, c.G * tint.G, c.B * tint.B, alpha);

	// The jaws: two rows of teeth closing on the bite, the outermost on its radius, then a flash.
	private void DrawJaws(ref Snap s)
	{
		float t = s.Age / SnapSeconds;
		float close = MathF.Min(1f, t / 0.55f);
		float gap = s.Radius * (1f - close * 0.85f);
		float alpha = t < 0.7f ? 1f : (1f - t) / 0.3f;
		// Each tooth sits on an arc inside the radius and closes toward the bite's centre line, so
		// nothing is ever drawn past what the bite tested.
		float rr = MathF.Max(4f, s.Radius - 2f);
		for (int k = 0; k < SnapTeeth; k++)
		{
			float a = Mathf.Lerp(-2.55f, -0.59f, k / (float)(SnapTeeth - 1));
			var up = new Vector2(MathF.Cos(a) * rr, MathF.Sin(a) * rr * (1f - close * 0.85f));
			var down = new Vector2(up.X, -up.Y);
			Color c = Lit(k % 2 == 0 ? ramp.Core : ramp.Hot, alpha);
			fx.DrawRect(WhipLimb.Cell(s.At + up, 4f), c);
			fx.DrawRect(WhipLimb.Cell(s.At + down, 4f), c);
			fx.DrawRect(WhipLimb.Cell(s.At + up * 0.7f, 2f), c);
			fx.DrawRect(WhipLimb.Cell(s.At + down * 0.7f, 2f), c);
		}
		if (close >= 1f && gap >= 0f)
			fx.DrawRect(WhipLimb.Cell(s.At, 6f), Lit(ramp.Core, alpha));
	}

	// The claws: three strokes raked across the swipe, down and toward the way the cat struck, drawn in as
	// the paw comes through - every cell inside the radius it tested.
	private void DrawClaws(ref Snap s)
	{
		float t = s.Age / SnapSeconds;
		float drawn = MathF.Min(1f, t / 0.4f);
		float alpha = t < 0.6f ? 1f : (1f - t) / 0.4f;
		float r = MathF.Max(6f, s.Radius - 2f);
		for (int k = -1; k <= 1; k++)
		{
			// Each stroke runs from upper back to lower front, parallel, a third of the radius apart.
			var off = new Vector2(k * r * 0.34f, k * r * 0.12f);
			var from = s.At + off + new Vector2(-s.Facing * r * 0.55f, -r * 0.62f);
			var to = s.At + off + new Vector2(s.Facing * r * 0.45f, r * 0.62f);
			Color c = Lit(k == 0 ? ramp.Core : ramp.Hot, alpha);
			int cells = Math.Max(3, (int)(r * 2f / 4f));
			for (int j = 0; j <= cells; j++)
			{
				float u = j / (float)cells;
				if (u > drawn)
					break;
				fx.DrawRect(WhipLimb.Cell(from.Lerp(to, u), u > 0.2f && u < 0.8f ? 4f : 2f), c);
			}
		}
	}

	// The peck: a point of light where the beak went in, and four sparks thrown out to the beak's reach.
	private void DrawPeck(ref Snap s)
	{
		float t = s.Age / SnapSeconds;
		float alpha = t < 0.5f ? 1f : (1f - t) / 0.5f;
		float out_ = MathF.Max(4f, s.Radius - 2f) * MathF.Min(1f, t / 0.6f);
		fx.DrawRect(WhipLimb.Cell(s.At, t < 0.35f ? 6f : 4f), Lit(ramp.Core, alpha));
		for (int k = 0; k < 4; k++)
		{
			float a = k * Mathf.Pi * 0.5f + Mathf.Pi * 0.25f;
			var at = s.At + new Vector2(MathF.Cos(a), MathF.Sin(a)) * out_;
			fx.DrawRect(WhipLimb.Cell(at, 2f), Lit(k % 2 == 0 ? ramp.Hot : ramp.Mid, alpha));
		}
	}
}
