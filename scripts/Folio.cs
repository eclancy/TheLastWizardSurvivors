using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// Folio (Arcane + Darkness): John Dee's opener since 2026-11-09 (.ai/character-fit.md section 6). Eric: "I don't
// think hollow star feels like a good spell for him - what spell could feel like his?" Dee is the collector - the
// largest library in England, at Mortlake, looted while he was abroad; freed here by opening a hundred chests - so
// his spell is paid for by what he gathers.
//
// THE CADENCE IS WHAT HE GATHERS. The book turns a page on its own every cooldown (2 s at level 1, through the ONE
// fire loop - Player.FireFolio hands the turn over, no timer of its own), and every XP orb he picks up turns one more
// page at once (XPOrb.OnBodyEntered -> Player.NotifyOrbGathered -> TurnByOrb). No other spell fires on pickups
// (.ai/spell-variety.md section 2, "What decides when it fires"): the orbs he goes and collects are his rate of fire.
// The page every cooldown is the floor that gets the first kills (Eric: "how would john dee produce the first orb if
// he only fires when he picks up an orb?"); gathering only quickens it.
//
// THE BOOK HOLDS WHAT IT TURNS. A turned page waits in the book until something is in reach, BookHolds at most, rather
// than being lost (a full book and no one near leaves the fire loop's timer ripe, so not even the timed turn is
// spent). Pages leave the book one at a time, PageGap apart: a pickup streak or the Gleaning Bell's stream is a stream
// of pages, never a single frame's wall of them, and the gap is what keeps the most a build can throw bounded.
//
// THE PAYLOAD. A page is thrown out of the book fanned off its mark and homes in hard - a short sharp cut - on the
// enemy in reach that the fewest pages already fly at, nearest first, so a turn of several pages is spread over the
// front of the crowd rather than stacked on one body. It strikes what its path crosses this frame (the segment, as
// Sleet Runes tests a rune: nothing steps over a body at a low frame rate) and is spent in the first body it meets,
// unless a card gives it pierce. Level 1 throws one page a turn (Eric's rule: spells start small); the levels add
// pages a turn, edge and a quicker turn (SpellData_Folio.tres).
//
// ONE NODE, EVERY PAGE. The node stands in the arena at the origin and draws in world space; pages, sparks, ink
// stains, the bound volumes, the ink flying home and the mirror are slots in fixed arrays. Every page is cells on the
// world's 2 px grid in the Bonelight palette - a pale leaf (the bone ramp), two lines of ink, a violet-black edge on
// its shaded sides, fluttering as it flies by narrowing to its edge and back - and every fade is a dither
// (SpellInk.DitherByCell). Nothing is allocated per frame. The book itself floats at the wizard's shoulder, open,
// while it is turning or holding pages: a page lifts across it on every turn, and a ribbon for every page it holds.
//
// THE MILESTONES (SpellEvolutionCatalog.SetupFolioEvolutions), every one a change on screen:
//   Torn Leaf   (level 4) - a page tears in two on its first strike; each half flies on at another enemy nearby.
//   Inkblot     (level 4, +1 Darkness) - where a page strikes, its ink spills: a black stain on the ground that slows
//               whatever walks through it.
//   Gilt Edge   (level 4) - every page is edged in gold and cuts on through two more enemies, faster.
//   Bound Volume (level 8, +1 Arcane) - the timed turn flies bound as one volume to the thickest pack and bursts open
//               there, every page cutting a different enemy round it.
//   Commonplace Book (level 8) - whatever a page kills is written into the book: its ink flies home and turns a page.
//   The Spirit Mirror (the Apotheosis, John Dee's; verb "pull") - every tenth page, his black obsidian mirror opens
//               over the thickest pack and draws every orb and flask in its reach to him; every orb it brings turns a
//               page through the same pickup hook. It moves no enemy, so no boss is ever touched.
public partial class Folio : Node2D
{
	public const string SpellId = "folio";

	// Milestone ids. SpellEvolutionCatalog builds the cards from these; this node reads them.
	public const string TornLeafId = "folio_torn_leaf";
	public const string InkblotId = "folio_inkblot";
	public const string GiltEdgeId = "folio_gilt_edge";
	public const string BoundVolumeId = "folio_bound_volume";
	public const string CommonplaceBookId = "folio_commonplace_book";
	/// <summary>John Dee's Apotheosis (ApotheosisCatalog), the form that moved here from Hollow Star.</summary>
	public const string SpiritMirrorId = "folio_spirit_mirror";
	/// <summary>The id the Spirit Mirror had on Hollow Star, which a save may hold as seen (SaveData.MovedAscensions).</summary>
	public const string HollowStarSpiritMirrorId = "hollow_star_spirit_mirror";

	// --- the book -----------------------------------------------------------------------------------
	/// <summary>Turned pages the book holds while nothing is in reach. A turn past it waits in the fire loop.</summary>
	public const int BookHolds = 5;
	/// <summary>Seconds between two pages leaving the book: at most 12.5 a second, however fast they are turned.</summary>
	public const float PageGap = 0.08f;

	// --- Torn Leaf ------------------------------------------------------------------------------------
	public const float TornShare = 0.5f;
	public const float TornReach = 150f;

	// --- Inkblot --------------------------------------------------------------------------------------
	public const float InkSlow = 0.35f;
	public const float InkSeconds = 2.5f;
	public const float InkRadius = 22f;
	private const float InkTickSeconds = 0.2f;
	private const float InkHoldSeconds = 0.35f;

	// --- Bound Volume ---------------------------------------------------------------------------------
	public const float VolumeSpeed = 380f;
	public const float VolumeBurstReach = 170f;

	// --- Commonplace Book ------------------------------------------------------------------------------
	public const float LetterSeconds = 0.4f;

	// --- The Spirit Mirror -----------------------------------------------------------------------------
	public const int MirrorEvery = 10;
	public const float MirrorReach = 240f;
	public const float MirrorFindReaches = 1.25f;
	private const float MirrorOpenSeconds = 0.22f;
	private const float MirrorHoldSeconds = 0.7f;
	private const float MirrorCloseSeconds = 0.22f;
	private const float MirrorRadius = 16f;

	// --- the page (tuned in scenes/Folio.tscn) ------------------------------------------------------
	[Export] public float BaseSpeed { get; set; } = 520f;
	[Export] public float HitRadius { get; set; } = 6f;
	[Export] public float TurnRate { get; set; } = 9f;
	[Export] public float LaunchDegrees { get; set; } = 40f;
	[Export] public float BookReach { get; set; } = 34f;
	[Export] public float BookLift { get; set; } = 26f;

	private const int MaxPages = 64;
	private const int MaxHitsPerPage = 8;
	private const int MaxSparks = 48;
	private const int MaxStains = 16;
	private const int MaxLetters = 24;
	private const int MaxVolumes = 4;
	private const int MaxCandidates = 8;
	private const int MaxPack = 16;
	private const float PackRadius = 90f;
	private const int MaxCrossed = 8;
	private const int SoundGapMs = 70;
	private const float Body = RiptideWave.BodyAllowance;
	private const float SparkSeconds = 0.24f;
	private const float BookShownSeconds = 0.6f;
	private const float FlipSeconds = 0.18f;
	private const float Grid = SpellInk.Grid;

	private static readonly Color LeafHi = Hex(0xEEF2FA);
	private static readonly Color LeafLit = Hex(0xD4DCEA);
	private static readonly Color LeafShade = Hex(0x9AA4B8);
	private static readonly Color InkDeep = Hex(0x14101F);
	private static readonly Color Occ = Hex(0x05070C);
	private static readonly Color GiltLit = Hex(0xD8B04A);
	private static readonly Color GiltBase = Hex(0x8A6A1E);
	private static readonly Color CoverBase = Hex(0x342C4C);
	private static readonly Color CoverShade = Hex(0x221C33);
	private static readonly Color SteelHi = Hex(0xDCE8F4);
	private static readonly Color SteelLit = Hex(0xB8C8DC);
	private static readonly ElementColors.Ramp ArcaneRamp = ElementColors.GetRamp(Element.Arcane);
	private static readonly ElementColors.Ramp DarkRamp = ElementColors.GetRamp(Element.Darkness);
	private static readonly int[] Flutter = { 6, 6, 5, 3, 1, 3, 5, 6 };
	private static readonly int[] Tumble = { 6, 5, 3, 5 };

	private struct Page
	{
		public Vector2 Pos;
		public Vector2 Vel;
		public float Speed;
		public float Travelled;
		public float Range;
		public float Age;
		public Enemy Quarry;
		public int Damage;
		public int PierceLeft;
		public int Hits;
		public int Phase;
		public bool Torn;
		public bool Gilt;
		public bool Live;
	}

	private struct Spark { public Vector2 At; public Vector2 Heading; public float Age; public bool Gilt; public bool Live; }
	private struct Stain { public Vector2 At; public float Age; public uint Seed; public bool Live; }
	private struct Letter { public Vector2 From; public float Age; public bool Live; }

	private struct Volume
	{
		public Vector2 From;
		public Vector2 To;
		public Enemy Quarry;
		public float Age;
		public float Duration;
		public int Pages;
		public int Damage;
		public bool Live;
	}

	public Player PlayerRef { get; set; }
	public SpellData SpellData { get; private set; }
	public int Waiting => waiting;
	public static int StatPages { get; set; }
	public static int StatMirrors { get; set; }
	public static int StatMirrorPulled { get; set; }

	private int damage = 7;
	private float speed = 520f;
	private float reach = 300f;
	private int pierce;
	private float area = 1f;
	private Color tint = Colors.White;
	private bool tearing, inking, gilt, bound, commonplace, mirrorForm;

	private int waiting;
	private float releaseClock;
	private int launches;
	private int pagesTowardMirror;
	private float bookShown;
	private float flip;
	private float inkClock;

	private Vector2 mirrorAt;
	private float mirrorAge = -1f;
	private bool mirrorPulled;
	private float mirrorSpin;

	private readonly Page[] pages = new Page[MaxPages];
	private readonly ulong[] hitIds = new ulong[MaxPages * MaxHitsPerPage];
	private readonly Spark[] sparks = new Spark[MaxSparks];
	private readonly Stain[] stains = new Stain[MaxStains];
	private readonly Letter[] letters = new Letter[MaxLetters];
	private readonly Volume[] volumes = new Volume[MaxVolumes];
	private readonly Enemy[] candidate = new Enemy[MaxCandidates];
	private readonly float[] candidateAt = new float[MaxCandidates];
	private readonly Enemy[] pack = new Enemy[MaxPack];
	private readonly float[] packAt = new float[MaxPack];
	private readonly Enemy[] crossed = new Enemy[MaxCrossed];
	private readonly float[] crossedAt = new float[MaxCrossed];
	private readonly List<Enemy> nearBuffer = new(32);
	private int nextPage, nextSpark, nextStain;
	private uint seedClock;
	private bool drewLastFrame, inkDrewLastFrame;
	private Node2D inkLayer;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = DrawLayers.Air;
		Material = SpellInk.DitherByCell;
		inkLayer = new Node2D { Name = "Ink", ZAsRelative = false, ZIndex = DrawLayers.GroundTell - 1, Material = SpellInk.DitherByCell };
		AddChild(inkLayer);
		inkLayer.Draw += DrawInk;
	}

	public override void _ExitTree()
	{
		if (inkLayer != null && IsInstanceValid(inkLayer))
			inkLayer.Draw -= DrawInk;
	}

	public void Refresh(SpellData spell, float damageMultiplier, Color visualTint, float playerArea)
	{
		if (spell == null)
			return;
		SpellData = spell;
		int level = Math.Max(1, spell.CurrentLevel);
		damage = Math.Max(1, Mathf.RoundToInt(spell.GetDamageAtLevel(level) * damageMultiplier));
		speed = MathF.Max(120f, BaseSpeed * spell.GetSpeedMultiplier());
		reach = MathF.Max(80f, spell.GetReachAtLevel(level));
		pierce = Math.Max(0, spell.GetPierceAtLevel(level));
		area = MathF.Max(0.25f, playerArea);
		// A third of the evolutions' tint (a Legendary's gold, the Spirit Mirror's violet): at full strength a page is
		// multiplied violet and stops reading as paper (the first Spirit Mirror capture, 2026-11-09). The trail and the
		// sparks carry the spell's colour already.
		tint = Colors.White.Lerp(visualTint, 0.35f);
		tearing = spell.HasEvolution(TornLeafId);
		inking = spell.HasEvolution(InkblotId);
		gilt = spell.HasEvolution(GiltEdgeId);
		bound = spell.HasEvolution(BoundVolumeId);
		commonplace = spell.HasEvolution(CommonplaceBookId);
		mirrorForm = spell.HasEvolution(SpiritMirrorId);
	}

	public bool TurnByTime(int count)
	{
		count = Math.Max(1, count);
		if (bound && ThrowVolume(count))
			return true;
		if (waiting >= BookHolds)
			return false;
		Add(count);
		return true;
	}

	public void TurnByOrb() => Add(1);

	private void Add(int count)
	{
		int room = BookHolds - waiting;
		if (room <= 0)
			return;
		waiting += Math.Min(room, count);
		OpenBook();
	}

	private void OpenBook()
	{
		bookShown = BookShownSeconds;
		flip = FlipSeconds;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		bool alive = PlayerRef != null && IsInstanceValid(PlayerRef);
		if (bookShown > 0f)
			bookShown = MathF.Max(0f, bookShown - dt);
		if (flip > 0f)
			flip = MathF.Max(0f, flip - dt);

		if (alive && SpellData != null && waiting > 0)
		{
			releaseClock = MathF.Min(releaseClock + dt, PageGap);
			if (releaseClock >= PageGap && PlayerRef.FolioMayCast(SpellData))
			{
				Enemy quarry = PickQuarry(PlayerRef.GlobalPosition, reach, null);
				if (quarry != null)
				{
					ThrowPage(BookAt(), quarry, damage, false, Vector2.Zero, -1);
					waiting--;
					releaseClock = 0f;
					OpenBook();
				}
			}
		}

		bool any = bookShown > 0f || waiting > 0;
		for (int i = 0; i < MaxPages; i++)
		{
			if (!pages[i].Live)
				continue;
			any = true;
			Fly(i, dt, alive);
		}
		for (int v = 0; v < MaxVolumes; v++)
		{
			if (!volumes[v].Live)
				continue;
			any = true;
			TickVolume(ref volumes[v], dt);
		}
		for (int l = 0; l < MaxLetters; l++)
		{
			if (!letters[l].Live)
				continue;
			any = true;
			letters[l].Age += dt;
			if (letters[l].Age >= LetterSeconds)
			{
				letters[l].Live = false;
				if (alive && SpellData != null && PlayerRef.FolioMayCast(SpellData))
					Add(1);
			}
		}
		for (int s = 0; s < MaxSparks; s++)
		{
			if (!sparks[s].Live)
				continue;
			any = true;
			sparks[s].Age += dt;
			if (sparks[s].Age >= SparkSeconds)
				sparks[s].Live = false;
		}
		if (mirrorAge >= 0f)
		{
			any = true;
			TickMirror(dt, alive);
		}

		bool inked = TickInk(dt);
		if (any || drewLastFrame)
			QueueRedraw();
		drewLastFrame = any;
		if (inked || inkDrewLastFrame)
			inkLayer?.QueueRedraw();
		inkDrewLastFrame = inked;
	}

	private Vector2 BookAt()
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return GlobalPosition;
		return PlayerRef.GlobalPosition + new Vector2(-PlayerRef.FacingSide * BookReach, -BookLift);
	}

	private static int CollectNearest(Vector2 origin, float within, Enemy exclude, Enemy[] into, float[] at, int cap)
	{
		int found = 0;
		float withinSquared = within * within;
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
		{
			if (enemy == null || enemy == exclude || !IsInstanceValid(enemy) || enemy.IsDying || !enemy.IsTargetable)
				continue;
			float d2 = origin.DistanceSquaredTo(enemy.GlobalPosition);
			if (d2 > withinSquared || (found == cap && d2 >= at[cap - 1]))
				continue;
			int slot = Math.Min(found, cap - 1);
			while (slot > 0 && at[slot - 1] > d2)
			{
				into[slot] = into[slot - 1];
				at[slot] = at[slot - 1];
				slot--;
			}
			into[slot] = enemy;
			at[slot] = d2;
			if (found < cap)
				found++;
		}
		return found;
	}

	private Enemy PickQuarry(Vector2 origin, float within, Enemy exclude)
	{
		int found = CollectNearest(origin, within, exclude, candidate, candidateAt, MaxCandidates);
		Enemy best = null;
		int fewest = int.MaxValue;
		for (int c = 0; c < found; c++)
		{
			int aimed = PagesAt(candidate[c]);
			if (aimed < fewest)
			{
				fewest = aimed;
				best = candidate[c];
			}
			if (fewest == 0)
				break;
		}
		ClearCandidates(found);
		return best;
	}

	private int PagesAt(Enemy enemy)
	{
		int n = 0;
		for (int i = 0; i < MaxPages; i++)
			if (pages[i].Live && pages[i].Quarry == enemy)
				n++;
		return n;
	}

	private void ClearCandidates(int found)
	{
		for (int c = 0; c < found; c++)
			candidate[c] = null;
	}

	private Enemy ThickestPack(Vector2 origin, float within)
	{
		int found = CollectNearest(origin, within, null, pack, packAt, MaxPack);
		Enemy best = null;
		int most = -1;
		float r2 = PackRadius * PackRadius;
		for (int i = 0; i < found; i++)
		{
			Vector2 here = pack[i].GlobalPosition;
			int count = 0;
			for (int j = 0; j < found; j++)
				if (here.DistanceSquaredTo(pack[j].GlobalPosition) <= r2)
					count++;
			if (count > most)
			{
				most = count;
				best = pack[i];
			}
		}
		for (int i = 0; i < found; i++)
			pack[i] = null;
		return best;
	}

	private int FreeSlot(int avoid)
	{
		for (int i = 0; i < MaxPages; i++)
		{
			int s = (nextPage + i) % MaxPages;
			if (!pages[s].Live)
			{
				nextPage = (s + 1) % MaxPages;
				return s;
			}
		}
		int oldest = nextPage == avoid ? (nextPage + 1) % MaxPages : nextPage;
		nextPage = (oldest + 1) % MaxPages;
		return oldest;
	}

	private void ThrowPage(Vector2 from, Enemy quarry, int pageDamage, bool torn, Vector2 heading, int avoid)
	{
		Vector2 dir;
		if (heading != Vector2.Zero)
			dir = heading.Normalized();
		else
		{
			Vector2 to = quarry != null ? quarry.GlobalPosition - from : Vector2.Up;
			dir = to.LengthSquared() > 0.0001f ? to.Normalized() : Vector2.Up;
			float jitter = 0.6f + 0.4f * ((launches * 0.618034f) % 1f);
			dir = dir.Rotated(Mathf.DegToRad(((launches & 1) == 0 ? 1f : -1f) * LaunchDegrees * jitter));
		}
		launches++;
		int slot = FreeSlot(avoid);
		float pageSpeed = torn ? speed * 0.9f : speed;
		pages[slot] = new Page
		{
			Pos = from,
			Vel = dir * pageSpeed,
			Speed = pageSpeed,
			Range = quarry == null ? 70f : torn ? TornReach * 1.5f : reach * 1.6f,
			Quarry = quarry,
			Damage = Math.Max(1, pageDamage),
			PierceLeft = torn ? 0 : pierce,
			Phase = launches,
			Torn = torn,
			Gilt = gilt,
			Live = true,
		};
		if (!torn)
		{
			StatPages++;
			pagesTowardMirror++;
			SfxPlayer.Instance?.PlayAt(SfxCatalog.SpellOrbit, from, 0.12f, "folio_page", SoundGapMs);
			CheckMirror();
		}
	}

	private void Fly(int i, float dt, bool alive)
	{
		ref Page p = ref pages[i];
		p.Age += dt;
		Vector2 from = p.Pos;
		if (p.Quarry != null && (!IsInstanceValid(p.Quarry) || p.Quarry.IsDying || !p.Quarry.IsTargetable))
			p.Quarry = null;
		if (p.Quarry != null)
		{
			Vector2 to = p.Quarry.GlobalPosition - p.Pos;
			if (to.LengthSquared() > 0.0001f)
				p.Vel = p.Vel.MoveToward(to.Normalized() * p.Speed, p.Speed * TurnRate * dt);
		}
		p.Pos += p.Vel * dt;
		p.Travelled += p.Speed * dt;
		if (alive)
			Strike(ref p, i, from, p.Pos);
		if (p.Live && p.Travelled >= p.Range)
		{
			p.Live = false;
			AddSpark(p.Pos, p.Vel, p.Gilt);
		}
	}

	private void Strike(ref Page p, int i, Vector2 from, Vector2 to)
	{
		Vector2 path = to - from;
		float length = path.Length();
		float hit = (p.Torn ? HitRadius * 0.7f : HitRadius) + Body;
		EnemyRegistry.CollectNear(from + path * 0.5f, length * 0.5f + hit, nearBuffer);
		if (nearBuffer.Count == 0)
			return;

		int row = i * MaxHitsPerPage;
		int found = 0;
		float hitSquared = hit * hit;
		float lengthSquared = MathF.Max(0.0001f, length * length);
		foreach (Enemy enemy in nearBuffer)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			Vector2 e = enemy.GlobalPosition;
			float t = Mathf.Clamp((e - from).Dot(path) / lengthSquared, 0f, 1f);
			if (e.DistanceSquaredTo(from + path * t) > hitSquared)
				continue;
			ulong id = enemy.GetInstanceId();
			bool seen = false;
			for (int h = 0; h < p.Hits && !seen; h++)
				seen = hitIds[row + h] == id;
			if (seen)
				continue;
			int at = Math.Min(found, MaxCrossed - 1);
			if (found == MaxCrossed && t >= crossedAt[at])
				continue;
			while (at > 0 && crossedAt[at - 1] > t)
			{
				crossed[at] = crossed[at - 1];
				crossedAt[at] = crossedAt[at - 1];
				at--;
			}
			crossed[at] = enemy;
			crossedAt[at] = t;
			if (found < MaxCrossed)
				found++;
		}
		nearBuffer.Clear();

		for (int c = 0; c < found; c++)
		{
			Enemy enemy = crossed[c];
			crossed[c] = null;
			if (!p.Live || enemy == null || !IsInstanceValid(enemy) || enemy.IsDying)
				continue;
			bool first = p.Hits == 0;
			if (p.Hits < MaxHitsPerPage)
				hitIds[row + p.Hits++] = enemy.GetInstanceId();
			Vector2 where = enemy.GlobalPosition;
			Vector2 heading = p.Vel.LengthSquared() > 0.0001f ? p.Vel.Normalized() : Vector2.Right;
			AddSpark(where, heading, p.Gilt);
			PlayerRef.DealDamageToEnemy(enemy, p.Damage, 0f, true, SpellData, null, null);
			bool killed = !IsInstanceValid(enemy) || enemy.IsDying;
			if (inking)
				AddStain(where);
			if (commonplace && killed)
				AddLetter(where);
			if (tearing && first && !p.Torn)
				Tear(where, killed ? null : enemy, heading, p.Damage, i);
			if (p.Quarry == enemy)
				p.Quarry = null;
			if (p.PierceLeft-- <= 0)
				p.Live = false;
		}
		for (int c = found; c < MaxCrossed; c++)
			crossed[c] = null;
	}

	private void Tear(Vector2 where, Enemy struck, Vector2 heading, int pageDamage, int avoid)
	{
		int found = CollectNearest(where, TornReach, struck, candidate, candidateAt, 2);
		int half = Math.Max(1, Mathf.RoundToInt(pageDamage * TornShare));
		for (int k = 0; k < 2; k++)
		{
			Enemy target = k < found ? candidate[k] : null;
			Vector2 off = heading.Rotated(Mathf.DegToRad(k == 0 ? 60f : -60f));
			ThrowPage(where, target, half, true, off, avoid);
		}
		ClearCandidates(found);
	}

	private bool ThrowVolume(int count)
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef) || !PlayerRef.FolioMayCast(SpellData))
			return false;
		Enemy target = ThickestPack(PlayerRef.GlobalPosition, reach);
		if (target == null)
			return false;
		int slot = -1;
		for (int v = 0; v < MaxVolumes; v++)
		{
			if (!volumes[v].Live)
			{
				slot = v;
				break;
			}
		}
		if (slot < 0)
			return false;
		Vector2 from = BookAt();
		float distance = from.DistanceTo(target.GlobalPosition);
		volumes[slot] = new Volume
		{
			From = from,
			To = target.GlobalPosition,
			Quarry = target,
			Duration = Mathf.Clamp(distance / VolumeSpeed, 0.22f, 0.6f),
			Pages = count,
			Damage = damage,
			Live = true,
		};
		OpenBook();
		SfxPlayer.Instance?.PlayAt(SfxCatalog.SpellOrbit, from, 0.05f, "folio_volume", SoundGapMs);
		return true;
	}

	private void TickVolume(ref Volume v, float dt)
	{
		v.Age += dt;
		if (v.Quarry != null && IsInstanceValid(v.Quarry) && !v.Quarry.IsDying)
			v.To = v.Quarry.GlobalPosition;
		else
			v.Quarry = null;
		if (v.Age < v.Duration)
			return;
		v.Live = false;
		Burst(v.To, v.Pages, v.Damage);
	}

	private void Burst(Vector2 at, int count, int pageDamage)
	{
		if (GetParent() is Node2D arena)
			ImpactDebrisLayer.Spawn(arena, at, 30f, ArcaneRamp, tint);
		SfxPlayer.Instance?.PlayAt(SfxCatalog.SpellOrbit, at, 0.05f, "folio_burst", SoundGapMs);
		int found = CollectNearest(at, VolumeBurstReach, null, candidate, candidateAt, MaxCandidates);
		for (int k = 0; k < count; k++)
		{
			Enemy target = found > 0 ? candidate[k % found] : null;
			Vector2 heading = Vector2.Right.Rotated(Mathf.Tau * k / Math.Max(1, count) - Mathf.Pi * 0.5f);
			ThrowPage(at, target, pageDamage, false, heading, -1);
		}
		ClearCandidates(found);
	}

	private void AddStain(Vector2 at)
	{
		float radius = InkRadius * area;
		float merge = radius * 0.6f;
		for (int s = 0; s < MaxStains; s++)
		{
			if (stains[s].Live && stains[s].At.DistanceSquaredTo(at) < merge * merge)
			{
				stains[s].Age = MathF.Min(stains[s].Age, 0.1f);
				return;
			}
		}
		int slot = -1;
		for (int s = 0; s < MaxStains; s++)
		{
			int q = (nextStain + s) % MaxStains;
			if (!stains[q].Live)
			{
				slot = q;
				break;
			}
		}
		if (slot < 0)
			slot = nextStain;
		nextStain = (slot + 1) % MaxStains;
		stains[slot] = new Stain { At = at, Seed = ++seedClock, Live = true };
	}

	private bool TickInk(float dt)
	{
		bool any = false;
		inkClock += dt;
		bool tick = inkClock >= InkTickSeconds;
		if (tick)
			inkClock = 0f;
		float radius = InkRadius * area;
		for (int s = 0; s < MaxStains; s++)
		{
			if (!stains[s].Live)
				continue;
			stains[s].Age += dt;
			if (stains[s].Age >= InkSeconds)
			{
				stains[s].Live = false;
				continue;
			}
			any = true;
			if (!tick)
				continue;
			float reachSquared = (radius + Body * 0.5f) * (radius + Body * 0.5f);
			EnemyRegistry.CollectNear(stains[s].At, radius + Body, nearBuffer);
			foreach (Enemy enemy in nearBuffer)
			{
				if (enemy != null && IsInstanceValid(enemy) && !enemy.IsDying
					&& enemy.GlobalPosition.DistanceSquaredTo(stains[s].At) <= reachSquared)
					enemy.ApplySlow(1f - InkSlow, InkHoldSeconds);
			}
			nearBuffer.Clear();
		}
		return any;
	}

	private void AddLetter(Vector2 from)
	{
		for (int l = 0; l < MaxLetters; l++)
		{
			if (letters[l].Live)
				continue;
			letters[l] = new Letter { From = from, Live = true };
			return;
		}
	}

	private Vector2 LetterAt(in Letter letter)
	{
		float t = Mathf.Clamp(letter.Age / LetterSeconds, 0f, 1f);
		float eased = t * t * (3f - 2f * t);
		Vector2 home = BookAt();
		Vector2 at = letter.From.Lerp(home, eased);
		return at + new Vector2(0f, -MathF.Sin(t * Mathf.Pi) * MathF.Min(30f, letter.From.DistanceTo(home) * 0.2f));
	}

	private void CheckMirror()
	{
		if (!mirrorForm || pagesTowardMirror < MirrorEvery)
			return;
		if (mirrorAge >= 0f || PlayerRef == null || !IsInstanceValid(PlayerRef))
		{
			pagesTowardMirror = MirrorEvery;
			return;
		}
		Enemy thickest = ThickestPack(PlayerRef.GlobalPosition, reach * MirrorFindReaches);
		if (thickest == null)
		{
			pagesTowardMirror = MirrorEvery;
			return;
		}
		pagesTowardMirror -= MirrorEvery;
		mirrorAt = thickest.GlobalPosition;
		mirrorAge = 0f;
		mirrorPulled = false;
		StatMirrors++;
	}

	private void TickMirror(float dt, bool alive)
	{
		mirrorAge += dt;
		mirrorSpin += dt;
		if (!mirrorPulled && mirrorAge >= MirrorOpenSeconds)
		{
			mirrorPulled = true;
			if (alive)
			{
				StatMirrorPulled += PullLoot(mirrorAt, MirrorReach * area);
				SfxPlayer.Instance?.PlayAt(SfxCatalog.PickupMagnet, mirrorAt, 0.05f, "folio_mirror", 300);
			}
		}
		if (mirrorAge >= MirrorOpenSeconds + MirrorHoldSeconds + MirrorCloseSeconds)
		{
			mirrorAge = -1f;
			CheckMirror();
		}
	}

	// Lifted from HollowStar.PullLootToPlayer (the Spirit Mirror on Hollow Star): once a mirror, not a frame.
	private int PullLoot(Vector2 at, float radius)
	{
		float reachSquared = radius * radius;
		int pulled = 0;
		foreach (Node node in GetTree().GetNodesInGroup(XPOrb.GroupName))
		{
			if (node is XPOrb orb && IsInstanceValid(orb) && at.DistanceSquaredTo(orb.GlobalPosition) <= reachSquared)
			{
				orb.PullToPlayer();
				pulled++;
			}
		}
		Node scene = GetTree().CurrentScene;
		if (scene != null)
		{
			foreach (Node node in scene.GetChildren())
			{
				if (node is PickupBase pickup && IsInstanceValid(pickup) && pickup.MagnetAttracted
					&& at.DistanceSquaredTo(pickup.GlobalPosition) <= reachSquared)
				{
					pickup.PullToPlayer();
					pulled++;
				}
			}
		}
		return pulled;
	}

	private void AddSpark(Vector2 at, Vector2 heading, bool gilded)
	{
		int slot = -1;
		for (int s = 0; s < MaxSparks; s++)
		{
			int q = (nextSpark + s) % MaxSparks;
			if (!sparks[q].Live)
			{
				slot = q;
				break;
			}
		}
		if (slot < 0)
			slot = nextSpark;
		nextSpark = (slot + 1) % MaxSparks;
		sparks[slot] = new Spark
		{
			At = at,
			Heading = heading.LengthSquared() > 0.0001f ? heading.Normalized() : Vector2.Right,
			Gilt = gilded,
			Live = true,
		};
	}

	public override void _Draw()
	{
		DrawMirror();
		DrawLetters();
		DrawVolumes();
		for (int i = 0; i < MaxPages; i++)
		{
			if (pages[i].Live)
				DrawPage(pages[i]);
		}
		DrawSparks();
		DrawBook();
	}

	private Color Ink(Color c, float coverage = 1f) => SpellInk.Ink(c, tint, coverage * tint.A);

	private void Px(Vector2 origin, int x, int y, Color c) =>
		DrawRect(new Rect2(origin.X + x * Grid, origin.Y + y * Grid, Grid, Grid), c);

	private void Run(Vector2 origin, int x, int y, int w, int h, Color c)
	{
		if (w > 0 && h > 0)
			DrawRect(new Rect2(origin.X + x * Grid, origin.Y + y * Grid, w * Grid, h * Grid), c);
	}

	// A page: a pale leaf `w` art pixels wide (its flutter) and `h` tall, lit from the upper left, a dog-eared top-right
	// corner, two lines of broken ink - writing, not strokes: two solid bars on a pale block read as a white "E", too
	// near a damage number (2026-11-09, the first PC capture) - a violet-black edge on its shaded sides (gold under Gilt
	// Edge), and behind it a short trail of Arcane and Darkness. A torn half is smaller, its corner torn away.
	private void DrawPage(in Page p)
	{
		Vector2 heading = p.Vel.LengthSquared() > 0.0001f ? p.Vel.Normalized() : Vector2.Right;
		DrawRect(WhipLimb.Cell(p.Pos - heading * 8f, Grid), Ink(ArcaneRamp.Mid, 0.85f));
		DrawRect(WhipLimb.Cell(p.Pos - heading * 14f, Grid), Ink(DarkRamp.Mid, 0.45f));

		int step = (int)(p.Age * 14f) + p.Phase;
		int w = Flutter[((step % Flutter.Length) + Flutter.Length) % Flutter.Length];
		int h = 7;
		if (p.Torn)
		{
			w = Math.Min(w, 4);
			h = 5;
		}
		Vector2 origin = SpellInk.Snap(p.Pos - new Vector2(w * Grid * 0.5f, h * Grid * 0.5f));
		Color edge = p.Gilt ? Ink(GiltLit) : Ink(DarkRamp.Edge);
		if (w <= 1)
		{
			// Edge-on: a sliver of light with its edge under it.
			Run(origin, 0, 0, 1, h - 1, Ink(LeafHi));
			Px(origin, 0, h - 1, edge);
			return;
		}
		// The leaf, its top row a cell short: the top-right corner is folded over (or torn away).
		Run(origin, 0, 1, w, h - 1, Ink(LeafLit));
		Run(origin, 0, 0, w - 1, 1, Ink(LeafHi));
		Run(origin, 0, 1, 1, h - 2, Ink(LeafHi));
		// The shaded sides: right and bottom, the edge the key never reaches.
		Run(origin, w - 1, 1, 1, h - 1, edge);
		Run(origin, 0, h - 1, w, 1, edge);
		if (!p.Torn && w >= 3)
			Px(origin, w - 2, 0, Ink(LeafShade));
		if (p.Gilt)
			Px(origin, w - 1, h - 1, Ink(GiltBase));
		// Writing: two lines of ink broken into words, the second shorter.
		if (w >= 3)
		{
			for (int x = 1; x <= w - 2; x++)
			{
				if (x % 3 != 0)
					Px(origin, x, 2, Ink(InkDeep));
				if (h >= 7 && x <= w - 3 && x % 3 != 2)
					Px(origin, x, 4, Ink(InkDeep));
			}
		}
	}

	private void DrawSparks()
	{
		for (int s = 0; s < MaxSparks; s++)
		{
			if (!sparks[s].Live)
				continue;
			Spark k = sparks[s];
			float u = Mathf.Clamp(k.Age / SparkSeconds, 0f, 1f);
			float outward = 5f + 14f * (1f - (1f - u) * (1f - u));
			float coverage = 1f - u;
			for (int f = 0; f < 3; f++)
			{
				Vector2 dir = k.Heading.Rotated(Mathf.DegToRad(f == 0 ? 0f : f == 1 ? 55f : -55f));
				Color c = f == 0 ? InkDeep : k.Gilt && f == 1 ? GiltLit : LeafHi;
				DrawRect(WhipLimb.Cell(k.At + dir * outward, Grid), Ink(c, coverage));
			}
			DrawRect(WhipLimb.Cell(k.At + k.Heading * (outward * 0.5f), Grid), Ink(ArcaneRamp.Hot, coverage * 0.8f));
		}
	}

	private void DrawLetters()
	{
		for (int l = 0; l < MaxLetters; l++)
		{
			if (!letters[l].Live)
				continue;
			Vector2 at = LetterAt(letters[l]);
			DrawRect(WhipLimb.Cell(at, Grid * 2f), Ink(InkDeep));
			DrawRect(WhipLimb.Cell(at, Grid), Ink(ArcaneRamp.Hot));
			Vector2 back = letters[l].From - at;
			if (back.LengthSquared() > 1f)
				DrawRect(WhipLimb.Cell(at + back.Normalized() * 6f, Grid), Ink(DarkRamp.Mid, 0.6f));
		}
	}

	private void DrawVolumes()
	{
		for (int v = 0; v < MaxVolumes; v++)
		{
			if (!volumes[v].Live)
				continue;
			Volume b = volumes[v];
			float t = Mathf.Clamp(b.Age / MathF.Max(0.01f, b.Duration), 0f, 1f);
			float hop = MathF.Sin(t * Mathf.Pi) * MathF.Min(44f, b.From.DistanceTo(b.To) * 0.3f);
			Vector2 at = b.From.Lerp(b.To, t) + new Vector2(0f, -hop);
			int w = Tumble[(int)(b.Age * 12f) & 3];
			const int h = 7;
			Vector2 origin = SpellInk.Snap(at - new Vector2(w * Grid * 0.5f, h * Grid * 0.5f));
			Run(origin, 0, 0, w, h, Ink(CoverBase));
			Run(origin, 0, 0, 1, h, Ink(DarkRamp.Mid));
			Run(origin, 0, h - 1, w, 1, Ink(CoverShade));
			if (w >= 4)
			{
				Run(origin, w - 2, 1, 1, h - 2, Ink(LeafLit));
				Px(origin, w - 2, h / 2, Ink(GiltLit));
				Px(origin, w - 1, h / 2, Ink(GiltBase));
			}
			Px(origin, 1, 1, Ink(DarkRamp.Hot));
		}
	}

	private void DrawBook()
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef) || (bookShown <= 0f && waiting <= 0))
			return;
		float coverage = waiting > 0 ? 1f : MathF.Min(1f, bookShown / 0.2f);
		Vector2 origin = SpellInk.Snap(BookAt() - new Vector2(8f * Grid * 0.5f, 5f * Grid * 0.5f));
		Run(origin, 0, 4, 8, 1, Ink(CoverBase, coverage));
		Px(origin, 3, 4, Ink(CoverShade, coverage));
		Px(origin, 4, 4, Ink(CoverShade, coverage));
		Run(origin, 0, 0, 4, 4, Ink(LeafLit, coverage));
		Run(origin, 0, 0, 1, 4, Ink(LeafHi, coverage));
		Run(origin, 4, 0, 4, 4, Ink(LeafShade, coverage));
		Run(origin, 5, 0, 3, 1, Ink(LeafLit, coverage));
		Run(origin, 3, 1, 2, 3, Ink(DarkRamp.Edge, coverage));
		Run(origin, 1, 1, 2, 1, Ink(InkDeep, coverage));
		Run(origin, 1, 2, 1, 1, Ink(InkDeep, coverage));
		Run(origin, 5, 1, 2, 1, Ink(InkDeep, coverage));
		Run(origin, 5, 2, 2, 1, Ink(InkDeep, coverage));
		if (flip > 0f)
		{
			float u = 1f - flip / FlipSeconds;
			int x = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(6f, 1f, u)), 0, 7);
			int lift = Mathf.RoundToInt(MathF.Sin(u * Mathf.Pi) * 2f);
			Run(origin, x, -lift, 1, 4, Ink(LeafHi, coverage));
			Px(origin, x, 3 - lift, Ink(DarkRamp.Edge, coverage));
		}
		for (int k = 0; k < waiting; k++)
			Px(origin, 1 + k + (k >= 2 ? 1 : 0), -1, Ink((k & 1) == 0 ? ArcaneRamp.Mid : DarkRamp.Mid, coverage));
	}

	private void DrawMirror()
	{
		if (mirrorAge < 0f)
			return;
		float open;
		if (mirrorAge < MirrorOpenSeconds)
			open = mirrorAge / MirrorOpenSeconds;
		else if (mirrorAge < MirrorOpenSeconds + MirrorHoldSeconds)
			open = 1f;
		else
			open = 1f - (mirrorAge - MirrorOpenSeconds - MirrorHoldSeconds) / MirrorCloseSeconds;
		open = Mathf.Clamp(open, 0f, 1f);
		float r = MirrorRadius * (0.25f + 0.75f * open);
		Vector2 centre = SpellInk.Snap(mirrorAt + new Vector2(0f, -24f));

		if (open >= 1f)
		{
			float reachNow = MirrorReach * area;
			float held = mirrorAge - MirrorOpenSeconds;
			for (int i = 0; i < 12; i++)
			{
				float p = (i * 0.6180339f + held * 1.1f) % 1f;
				float rr = Mathf.Lerp(reachNow, r + 2f, p * p);
				float a = i * (Mathf.Tau / 12f) + mirrorSpin * 0.6f + p * 2.2f;
				Vector2 at = mirrorAt + new Vector2(MathF.Cos(a), MathF.Sin(a)) * rr;
				DrawRect(WhipLimb.Cell(at, Grid), Ink(p < 0.6f ? DarkRamp.Mid : DarkRamp.Hot));
			}
		}

		int cells = Math.Max(1, Mathf.RoundToInt(r / Grid));
		for (int y = -cells; y <= cells; y++)
		{
			int hw = Mathf.RoundToInt(MathF.Sqrt(MathF.Max(0f, cells * cells - y * y)));
			if (hw <= 0)
				continue;
			float rowY = centre.Y + y * Grid;
			Color rimColour = y > 0 ? DarkRamp.Hot : DarkRamp.Mid;
			if (Math.Abs(y) >= cells - 1)
			{
				DrawRect(new Rect2(centre.X - hw * Grid, rowY, hw * 2 * Grid, Grid), Ink(rimColour));
				continue;
			}
			DrawRect(new Rect2(centre.X - hw * Grid, rowY, Grid, Grid), Ink(DarkRamp.Mid));
			DrawRect(new Rect2(centre.X + (hw - 1) * Grid, rowY, Grid, Grid), Ink(rimColour));
			DrawRect(new Rect2(centre.X - (hw - 1) * Grid, rowY, (hw * 2 - 2) * Grid, Grid), Ink(Occ));
			if (y < 0)
				DrawRect(new Rect2(centre.X - (hw - 1) * Grid, rowY, Grid, Grid), Ink(InkDeep));
		}
		if (cells >= 3)
		{
			for (int k = -cells / 2; k <= cells / 3; k++)
			{
				Vector2 at = centre + new Vector2(k * Grid, (-k - cells / 3) * Grid);
				DrawRect(new Rect2(at, new Vector2(Grid, Grid)), Ink(k == 0 ? SteelHi : SteelLit));
			}
		}
		DrawRect(new Rect2(centre.X - Grid, centre.Y + (cells + 1) * Grid, Grid * 2, Grid * 3), Ink(CoverBase));
		DrawRect(new Rect2(centre.X - Grid, centre.Y + (cells + 1) * Grid, Grid, Grid * 3), Ink(DarkRamp.Mid));
	}

	private void DrawInk()
	{
		if (inkLayer == null)
			return;
		float radius = InkRadius * area;
		for (int s = 0; s < MaxStains; s++)
		{
			if (!stains[s].Live)
				continue;
			Stain k = stains[s];
			float coverage = MathF.Min(1f, k.Age / 0.08f) * MathF.Min(1f, (InkSeconds - k.Age) / 0.6f);
			Vector2 centre = SpellInk.Snap(k.At);
			int cells = Math.Max(2, Mathf.RoundToInt(radius / Grid));
			for (int y = -cells; y <= cells; y++)
			{
				uint h = (k.Seed * 2654435761u) ^ (uint)((y + 64) * 40503);
				float wobble = 0.72f + 0.28f * ((h >> 8) % 1000u) / 1000f;
				int hw = Mathf.RoundToInt(MathF.Sqrt(MathF.Max(0f, cells * cells - y * y)) * wobble);
				if (hw <= 0)
					continue;
				int shift = (int)((h >> 20) % 3u) - 1;
				float x0 = centre.X + (shift - hw) * Grid;
				float rowY = SpellInk.Snap(centre.Y + y * Grid * 0.75f);
				inkLayer.DrawRect(new Rect2(x0, rowY, hw * 2 * Grid, Grid), SpellInk.Ink(DarkRamp.Edge, tint, coverage));
				if (hw >= 2)
					inkLayer.DrawRect(new Rect2(x0 + Grid, rowY, (hw * 2 - 2) * Grid, Grid), SpellInk.Ink(InkDeep, tint, coverage));
			}
			Vector2 glint = SpellInk.Snap(centre + new Vector2(-cells * 0.4f * Grid, -cells * 0.3f * Grid));
			inkLayer.DrawRect(new Rect2(glint, new Vector2(Grid, Grid)), SpellInk.Ink(DarkRamp.Mid, tint, coverage));
		}
	}

	private static Color Hex(uint rgb) =>
		new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
}
