using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// Contagion: a plague put on one enemy that jumps to everything near it when it dies
// (.ai/side-events.md). Kept by the lantern-tender in the Blighted Swamp.
//
// **The cadence nothing else has: it spreads on death, by itself.** The cast only ever touches one
// enemy. Everything after that is the swarm infecting itself - a carrier that dies in a pack seeds
// the whole pack, each of those seeds its neighbours, and the plague walks through the crowd for as
// long as the crowd stays packed. Against stragglers it does almost nothing, which is the point:
// it is a spell about density, where Frost Shard's on-kill burst is a spell about killing.
//
// **Pure Poison.** Poison had no pure spell, so it could never reach its capstone.
//
// Bounded twice, because a chain reaction in a swarm is unbounded otherwise: a cast can infect at
// most MaxInfected enemies in total, and a mark lapses after MarkSeconds if its carrier survives.
//
// **Marsh Fever** (the level 4 element path into Water, ElementPaths): the marks and the bursts of
// a jump are repainted in Water's ramp, and every infection drags its carrier (the rider, applied by
// Player.DealDamageToEnemy on the infecting hit).
//
// **Its own four cards** (2026-10-29, .ai/spell-roster.md section 2e), each a new way the plague
// moves, drawn in the air over the bodies it reaches. The ids are the template's.
//   - Fever Burst (level 4): a carrier that dies bursts - everything in the jump is hit, carrier or not
//     - and leaves a fever cloud hanging where it fell for FeverCloudSeconds that infects whatever
//     walks into it. (The proposal's "dosing all near it" was what a death already did; the cloud and
//     the hit are what is new.)
//   - Outbreak (level 4): the first carrier bursts the moment it is infected, so the plague is in the
//     pack at once - and still jumps again when that carrier dies. (The proposal's name, Quickening,
//     is Gale Blade's card; this is its behaviour under its own name.)
//   - Pandemic (level 8): every PandemicSeconds, every carrier on the field bursts at once without
//     dying - one beat for all of them (a shared clock) - and each burst infects and hits what is round it.
//   - Plague Wind (level 8): each cast also looses a cloud of the plague that drifts out from the
//     wizard through the crowd and on past it, infecting everything it passes. It stops at a maze wall.
//
// **The Dose** (Paracelsus's Apotheosis, the verb DISTIL; .ai/chapter-wizards.md 4.4): "Each jump of the plague reaches
// half as many and doubles its dose. The last carrier dies hardest." Every carrier has a generation - the cast's is 0 -
// and the plague is distilled as it goes: the first jump reaches the DoseFirstJump nearest, the next four, then two,
// then one, each generation's carriers taking twice the last one's dose (the infecting hit and the poison both, x2,
// x4, x8, x16). The quota is the generation's, shared by every carrier of the one before it, so the four jumps reach
// 8, 4, 2 and 1 bodies in all - fifteen, inside MaxInfected - and the one at the end carries sixteen times the dose and
// spreads nothing: it is where the plague ends. Within a jump the nearest go first. Every card still works on it, each
// in its generation: a fever cloud infects into the next generation of the carrier that left it, a Pandemic beat
// bursts each carrier as its own generation, Outbreak's burst is the first jump, and the Plague Wind infects as the
// first jump does. Poison, the spell's only element; no card on Contagion trades it away (Marsh Fever adds Water).
public partial class Contagion : Node2D, IPaintsElementPath
{
	public const string FeverBurstId = "contagion_amplified_impact";
	public const string OutbreakId = "contagion_hyper_acceleration";
	public const string PandemicId = "contagion_titan_cataclysm";
	public const string PlagueWindId = "contagion_infinite_horizon";
	public const string TheDoseId = "contagion_the_dose";

	// The Dose: how many the first jump reaches (each later one half the last), and how many jumps there are at most
	// (4.4: four generations). 8 then 4, 2, 1: the last generation is one body.
	public const int DoseFirstJump = 8;
	public const int DoseGenerations = 4;

	/// <summary>The Dose's multiplier on a carrier of <paramref name="generation"/>: twice the last generation's.</summary>
	public static int DoseOf(int generation) => 1 << Math.Clamp(generation, 0, DoseGenerations);

	/// <summary>
	/// The most a BOSS is ever dosed, as a generation: x4. The last carrier is meant to die of its dose, and a boss does not -
	/// it stands in its adds for a whole fight, and under Pandemic a beat re-doses it every two seconds without anything
	/// dying, so uncapped it would hold the last carrier's x16 for the fight. It is still infected and still passes the
	/// plague on; only its dose stops doubling at the second jump.
	/// </summary>
	public const int DoseBossGeneration = 2;

	/// <summary>The Dose's multiplier for <paramref name="enemy"/> as a carrier of <paramref name="generation"/>.</summary>
	public static int DoseFor(Enemy enemy, int generation) =>
		DoseOf(enemy is BossEnemy ? Math.Min(generation, DoseBossGeneration) : generation);

	/// <summary>The Dose's quota for <paramref name="generation"/> (1 to DoseGenerations): half the one before it.</summary>
	public static int DoseQuota(int generation) =>
		generation < 1 || generation > DoseGenerations ? 0 : Math.Max(1, DoseFirstJump >> (generation - 1));

	/// <summary>Last carriers The Dose has finished this process, for the harness: each one sixteen times dosed.</summary>
	public static int LastCarriersDosed { get; private set; }

	/// <summary>Carriers The Dose has infected this process, by generation (0 the cast's), for the harness.</summary>
	public static readonly int[] DoseInfectedByGeneration = new int[DoseGenerations + 1];

	// Fever Burst: the hit a dying carrier's burst deals everything in its jump (a share of the plague's
	// damage), and the cloud it leaves: how long it hangs, and how wide it is as a share of the jump.
	public const float FeverHit = 0.6f;
	public const float FeverCloudSeconds = 1.5f;
	public const float FeverCloudShare = 0.6f;

	// Outbreak: the cast's burst reaches this share of a jump - the carrier's own pack, not the next one.
	public const float OutbreakShare = 0.6f;

	// Pandemic: the beat, and the hit each burst deals. One hit per enemy per beat however many carriers
	// stand round it, so a beat costs at most one hit an enemy on the field.
	public const float PandemicSeconds = 2.0f;
	public const float PandemicHit = 1.0f;

	// Plague Wind: how fast the cloud drifts, for how long, and how wide it is as a share of the jump.
	public const float WindSpeed = 85f;
	public const float WindSeconds = 5f;
	public const float WindShare = 0.7f;

	[Export] public float SpreadRadius { get; set; } = 95f;
	[Export] public float MarkSeconds { get; set; } = 6f;
	[Export] public int MaxInfected { get; set; } = 24;

	public int Damage { get; set; } = 6;
	public SpellData SpellData { get; set; }
	public Player PlayerRef { get; set; }

	// How much faster the poison runs: each tick bites PoisonRate times harder and the poison runs out
	// PoisonRate times sooner, so the dose is the same and a carrier dies sooner - which is what makes
	// the plague spread sooner. The mark, and so the spread on death, is unchanged. 1 unless a card's
	// Speed says otherwise (none does since 2026-10-29).
	public float PoisonRate { get; set; } = 1f;

	// Carriers one cast may infect, each with its own plague.
	public const int MaxCarriersPerCast = 6;

	private struct Mark
	{
		public Enemy Enemy;
		public Vector2 LastSeen;
		public float Remaining;
		public int Generation;   // the cast's carrier is 0; each jump is one more (The Dose reads it)
	}

	private readonly List<Mark> marks = new();
	private readonly List<(Vector2 At, int Generation)> pendingSpreads = new();
	private readonly List<(Vector2 At, float Age, uint Seed, int Count, float Reach)> bursts = new();
	private readonly List<(Vector2 At, float Age, int Generation)> fevers = new();
	// The Dose: whether this plague is distilled, and how many each generation has reached so far.
	private bool dose;
	private readonly int[] doseReached = new int[DoseGenerations + 1];
	private uint burstSeed;
	private readonly HashSet<ulong> everInfected = new();
	private float pollTimer;

	// The cards, read once from the spell in _Ready.
	private bool fever;
	private bool pandemic;

	// Plague Wind's cloud: where it is, which way it drifts, and how long it has left (0: none).
	private Vector2 windAt;
	private Vector2 windDir;
	private float windLeft;

	// Pandemic's beat, shared by every plague on the field so they burst together: one clock, advanced
	// once a frame by whichever plague runs first, and the beat each plague last answered.
	private static double pandemicClock;
	private static ulong pandemicClockFrame;
	private static long pandemicHitBeat = -1;
	private static readonly HashSet<ulong> pandemicHitThisBeat = new();
	private long answeredBeat = -1;

	// The broad phase for a jump. One poll at a time.
	private static readonly List<Enemy> near = new(64);

	// Poison's ramp, for the carriers' marks, the spores a jump throws and the clouds; an element path
	// repaints all of it in its own.
	private ElementColors.Ramp spores = ElementColors.GetRamp(Element.Poison);

	// The spores a jump throws are in the air, over the bodies they land on; the carriers' marks are on
	// the floor at their feet, under them (DrawLayers). One node at z 1 drew both over every body. The
	// fever clouds and the plague wind are in the air too, on a child of their own: they are drawn by
	// spore_cloud.gdshader (GasCloud), which must not see the spores.
	private Node2D air;
	private Node2D clouds;
	private ShaderMaterial cloudMaterial;
	private float clock;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = DrawLayers.GroundTell;
		air = new Node2D { Name = "Spores", ZAsRelative = false, ZIndex = DrawLayers.Air };
		AddChild(air);
		air.Draw += DrawSpores;
		if (SpellData?.GetBranchElement() is Element branch)
			spores = ElementColors.GetRamp(branch);
		cloudMaterial = GasCloud.MakeMaterial(spores);
		clouds = new Node2D { Name = "Clouds", ZAsRelative = false, ZIndex = DrawLayers.Air, Material = cloudMaterial };
		AddChild(clouds);
		clouds.Draw += DrawClouds;
		if (SpellData != null)
		{
			fever = SpellData.HasEvolution(FeverBurstId);
			pandemic = SpellData.HasEvolution(PandemicId);
			dose = SpellData.HasEvolution(TheDoseId);
		}
		if (pandemic)
			answeredBeat = (long)(pandemicClock / PandemicSeconds);
	}

	/// <summary>
	/// Infects the first carrier. Call once, after adding to the tree. Under Outbreak the carrier bursts
	/// at once, as if it had died, and keeps its mark to burst again when it does.
	/// </summary>
	public void InfectFirst(Enemy carrier)
	{
		Infect(carrier, 0);
		if (SpellData?.HasEvolution(OutbreakId) == true && carrier != null && IsInstanceValid(carrier) && !carrier.IsDying)
			Burst(carrier.GlobalPosition, 0f, ImpactDebris.CountFor(SpreadRadius * OutbreakShare), 0, reach: SpreadRadius * OutbreakShare);
	}

	/// <summary>
	/// Plague Wind: a cloud of this plague set drifting from <paramref name="from"/> along
	/// <paramref name="direction"/>. What it passes is infected into this plague, inside its MaxInfected.
	/// </summary>
	public void LooseWind(Vector2 from, Vector2 direction)
	{
		windAt = from;
		windDir = direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector2.Right;
		windLeft = WindSeconds;
	}

	// True if The Dose's quota for this generation has room (always true without the form).
	private bool HasRoom(int generation) =>
		!dose || (generation >= 0 && generation <= DoseGenerations && (generation == 0 || doseReached[generation] < DoseQuota(generation)));

	private void Infect(Enemy enemy, int generation)
	{
		if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying || everInfected.Count >= MaxInfected || !HasRoom(generation))
			return;
		if (!everInfected.Add(enemy.GetInstanceId()))
			return;
		if (dose)
		{
			doseReached[Math.Clamp(generation, 0, DoseGenerations)]++;
			DoseInfectedByGeneration[Math.Clamp(generation, 0, DoseGenerations)]++;
		}

		marks.Add(new Mark { Enemy = enemy, LastSeen = enemy.GlobalPosition, Remaining = MarkSeconds, Generation = generation });
		// The poison carries the damage over time; the hit on infection is what credits the spell
		// in the per-spell damage table and makes the jump visible as a number. Under The Dose both are this
		// generation's dose.
		int damage = dose ? Damage * DoseFor(enemy, generation) : Damage;
		float rate = MathF.Max(0.25f, PoisonRate);
		enemy.ApplyPoison(Math.Max(1, Mathf.RoundToInt(damage * rate)), MarkSeconds / rate);
		if (PlayerRef != null && IsInstanceValid(PlayerRef))
			PlayerRef.DealDamageToEnemy(enemy, damage, source: SpellData);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		clock += dt;
		QueueRedraw();
		air?.QueueRedraw();
		if (fevers.Count > 0 || windLeft > 0f || cloudsDrawn)
		{
			GasCloud.SetTime(cloudMaterial, clock);
			clouds.QueueRedraw();
		}
		cloudsDrawn = fevers.Count > 0 || windLeft > 0f;

		for (int i = bursts.Count - 1; i >= 0; i--)
		{
			var b = bursts[i];
			b.Age += dt;
			if (b.Age > ImpactDebris.LifeSeconds) bursts.RemoveAt(i); else bursts[i] = b;
		}
		for (int i = fevers.Count - 1; i >= 0; i--)
		{
			var f = fevers[i];
			f.Age += dt;
			if (f.Age > FeverCloudSeconds) fevers.RemoveAt(i); else fevers[i] = f;
		}

		if (windLeft > 0f)
		{
			windLeft -= dt;
			Vector2 next = windAt + windDir * WindSpeed * dt;
			// A wall stops a shot, and it stops the wind: the cloud breaks up against it.
			if (MazeNavigation.Active?.IsWallAt(next) == true)
				windLeft = MathF.Min(windLeft, 0.3f);
			else
				windAt = next;
		}

		if (pandemic)
			Pulse(dt);

		pollTimer -= dt;
		if (pollTimer <= 0f)
		{
			pollTimer = 0.1f;
			Poll(0.1f);
		}

		if (marks.Count == 0 && pendingSpreads.Count == 0 && bursts.Count == 0 && fevers.Count == 0 && windLeft <= 0f)
			QueueFree();
	}

	// Pandemic: the shared clock moves on once a frame, and on each new beat every carrier of this
	// plague bursts where it stands.
	private void Pulse(float dt)
	{
		ulong frame = Engine.GetProcessFrames();
		if (pandemicClockFrame != frame)
		{
			pandemicClockFrame = frame;
			pandemicClock += dt;
		}
		long beat = (long)(pandemicClock / PandemicSeconds);
		if (beat == answeredBeat)
			return;
		answeredBeat = beat;
		if (pandemicHitBeat != beat)
		{
			pandemicHitBeat = beat;
			pandemicHitThisBeat.Clear();
		}
		for (int i = marks.Count - 1; i >= 0; i--)
		{
			Enemy carrier = marks[i].Enemy;
			if (carrier != null && IsInstanceValid(carrier) && !carrier.IsDying)
				Burst(carrier.GlobalPosition, PandemicHit, 12, marks[i].Generation, pandemicHitThisBeat);
		}
	}

	// How far into the beat the field is, 0..1 - the marks swell over its last fifth, the tell.
	private static float BeatPhase => (float)(pandemicClock % PandemicSeconds / PandemicSeconds);

	private void Poll(float step)
	{
		pendingSpreads.Clear();
		for (int i = marks.Count - 1; i >= 0; i--)
		{
			Mark mark = marks[i];
			bool dead = mark.Enemy == null || !IsInstanceValid(mark.Enemy) || mark.Enemy.IsDying;
			if (dead)
			{
				pendingSpreads.Add((mark.LastSeen, mark.Generation));
				marks.RemoveAt(i);
				continue;
			}

			mark.LastSeen = mark.Enemy.GlobalPosition;
			mark.Remaining -= step;
			if (mark.Remaining <= 0f)
				marks.RemoveAt(i);
			else
				marks[i] = mark;
		}

		foreach (var (origin, generation) in pendingSpreads)
		{
			// The Dose's last carrier: the plague ends in it. It goes down hardest - a heavy, dark burst out of it - and
			// infects nobody.
			if (dose && generation >= DoseGenerations)
			{
				LastCarriersDosed++;
				bursts.Add((origin, 0f, ++burstSeed + (uint)(GetInstanceId() % 9973), ImpactDebris.CountFor(SpreadRadius) * 2, SpreadRadius * 0.7f));
				continue;
			}
			Burst(origin, fever ? FeverHit : 0f, ImpactDebris.CountFor(SpreadRadius), generation);
			if (fever)
				fevers.Add((origin, 0f, generation));
		}
		pendingSpreads.Clear();

		// What walks into a fever cloud, or that the wind drifts over, catches the plague - into the next generation of
		// the carrier that left the cloud, and the wind as the first jump does.
		foreach (var cloud in fevers)
			InfectWithin(cloud.At, SpreadRadius * FeverCloudShare, cloud.Generation + 1);
		if (windLeft > 0f)
			InfectWithin(windAt, SpreadRadius * WindShare, 1);
	}

	// A jump from `origin`: spores thrown out to exactly its reach (SpreadRadius unless `reach` says
	// otherwise), everything inside infected, and - with `hitShare` above 0 - hit for that share of the
	// plague's damage, carrier or not. A hit set passed in (Pandemic's beat) lets each enemy take one
	// such hit however many bursts reach it.
	private void Burst(Vector2 origin, float hitShare, int particles, int generation, HashSet<ulong> hitOnce = null, float reach = -1f)
	{
		float radius = reach > 0f ? reach : SpreadRadius;
		bursts.Add((origin, 0f, ++burstSeed + (uint)(GetInstanceId() % 9973), particles, radius));
		float radiusSquared = radius * radius;
		// Under The Dose the burst's hit is the bursting carrier's dose.
		int hit = hitShare > 0f ? Math.Max(1, Mathf.RoundToInt(Damage * hitShare * (dose ? DoseOf(generation) : 1))) : 0;
		// A boss in the burst takes it at no more than its own dose cap (DoseBossGeneration).
		int bossHit = hitShare > 0f ? Math.Max(1, Mathf.RoundToInt(Damage * hitShare * (dose ? DoseOf(Math.Min(generation, DoseBossGeneration)) : 1))) : 0;
		EnemyRegistry.CollectNear(origin, radius, near);
		// The Dose: the jump reaches only the next generation's quota, nearest first - infected before the walk below,
		// which then only hits.
		if (dose)
			InfectNearest(origin, radiusSquared, generation + 1);
		foreach (Enemy neighbour in near)
		{
			if (!IsInstanceValid(neighbour) || neighbour.IsDying || origin.DistanceSquaredTo(neighbour.GlobalPosition) > radiusSquared)
				continue;
			if (!dose)
				Infect(neighbour, generation + 1);
			if (hit > 0 && PlayerRef != null && IsInstanceValid(PlayerRef) && IsInstanceValid(neighbour) && !neighbour.IsDying
				&& (hitOnce == null || hitOnce.Add(neighbour.GetInstanceId())))
				PlayerRef.DealDamageToEnemy(neighbour, neighbour is BossEnemy ? bossHit : hit, source: SpellData);
		}
		near.Clear();
	}

	private void InfectWithin(Vector2 at, float radius, int generation)
	{
		if (everInfected.Count >= MaxInfected || !HasRoom(generation))
			return;
		float radiusSquared = radius * radius;
		EnemyRegistry.CollectNear(at, radius, near);
		if (dose)
			InfectNearest(at, radiusSquared, generation);
		else
		{
			foreach (Enemy enemy in near)
			{
				if (IsInstanceValid(enemy) && at.DistanceSquaredTo(enemy.GlobalPosition) <= radiusSquared)
					Infect(enemy, generation);
			}
		}
		near.Clear();
	}

	// The Dose's jump: out of `near` (already collected round `origin`), the nearest bodies not yet carrying this plague,
	// one at a time, until the generation's quota is full. A selection, not a sort, so it allocates nothing: a jump
	// takes eight at most out of a few dozen.
	private void InfectNearest(Vector2 origin, float radiusSquared, int generation)
	{
		while (generation >= 1 && HasRoom(generation) && everInfected.Count < MaxInfected)
		{
			Enemy best = null;
			float bestD = radiusSquared;
			foreach (Enemy candidate in near)
			{
				if (!IsInstanceValid(candidate) || candidate.IsDying || everInfected.Contains(candidate.GetInstanceId()))
					continue;
				float d = origin.DistanceSquaredTo(candidate.GlobalPosition);
				if (d <= bestD)
				{
					best = candidate;
					bestD = d;
				}
			}
			if (best == null)
				return;
			Infect(best, generation);
		}
	}

	private bool cloudsDrawn;

	// Each carrier's mark at its feet: a status on one enemy, not an area, so it stays. A ring of the
	// plague's own ramp stamped on the world's 2 px grid - its far side lit, its near side in the edge
	// tone, in an occlusion line so it reads on any floor - where it was an antialiased arc at 90% alpha.
	// Under Pandemic the marks swell together before each beat, a step at a time - the tell that every
	// carrier is about to go.
	public override void _Draw()
	{
		float swell = 1f;
		if (pandemic)
		{
			float phase = BeatPhase;
			swell = phase > 0.8f ? 1f + 0.6f * (phase - 0.8f) / 0.2f : 1f;
		}
		float g = PixelCells.Grid;
		foreach (Mark mark in marks)
		{
			// Round the carrier's feet, a little wider than its body so the ring shows past it on the
			// floor (the body stands on it); where the carrier has just gone, where it was last seen.
			Vector2 c;
			float half = 10f;
			if (mark.Enemy != null && IsInstanceValid(mark.Enemy) && !mark.Enemy.IsDying)
			{
				StatusFxGeometry body = mark.Enemy.StatusFxGeometry;
				c = ToLocal(mark.Enemy.GlobalPosition) + new Vector2(0f, body.FeetY - 1f);
				half = MathF.Max(half, body.HalfWidth + 3f);
			}
			else
			{
				c = ToLocal(mark.LastSeen) + new Vector2(0f, 10f);
			}
			// The Dose: each generation's mark a grid step wider than the last - the dose seen growing as the plague
			// narrows - and the last carrier's ringed twice in the hot tone below.
			if (dose)
				half += mark.Generation * g;
			float rx = MathF.Round(half * swell / g) * g;
			float ry = MathF.Max(g * 2f, MathF.Round(rx * 0.45f / g) * g);
			int steps = Math.Max(16, (int)(Mathf.Tau * rx / g));
			for (int pass = 0; pass < 2; pass++)
			{
				for (int i = 0; i < steps; i++)
				{
					float a = i * Mathf.Tau / steps;
					Vector2 at = c + new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry);
					if (pass == 0)
						DrawRect(PixelCells.Snap(this, at, g * 2f), Occ);
					else
						DrawRect(PixelCells.Snap(this, at, g), MathF.Sin(a) < -0.3f ? spores.Hot : MathF.Sin(a) > 0.3f ? spores.Edge : spores.Mid);
				}
			}
			// Swelling: a second ring of the hot tone just outside, the beat about to land. Always on The Dose's last.
			if (swell > 1.25f || (dose && mark.Generation >= DoseGenerations))
			{
				for (int i = 0; i < steps; i += 2)
				{
					float a = i * Mathf.Tau / steps;
					DrawRect(PixelCells.Snap(this, c + new Vector2(MathF.Cos(a) * (rx + g * 2f), MathF.Sin(a) * (ry + g)), g), spores.Hot);
				}
			}
		}
	}

	private static readonly Color Occ = new Color(0.020f, 0.027f, 0.047f, 1f);

	// A jump: plague spores thrown out of the corpse to exactly SpreadRadius, the reach that was
	// infected. It was a ring opening to that radius - an area marker - and went (2026-10-23).
	private void DrawSpores()
	{
		foreach (var burst in bursts)
			ImpactDebris.Draw(air, ToLocal(burst.At), burst.Reach, burst.Age, burst.Seed, spores, burst.Count, Colors.White);
	}

	// Fever Burst's clouds and the plague wind, over the bodies: billowing lobes of the plague's own ramp,
	// dithered so the swarm inside still shows, whose outer edge is the ground they infect (GasCloud,
	// spore_cloud.gdshader). They were overlapping translucent discs.
	private void DrawClouds()
	{
		foreach (var cloud in fevers)
		{
			float t = cloud.Age / FeverCloudSeconds;
			// Billows out over its first fifth, hangs, then thins away.
			float grow = Mathf.Clamp(cloud.Age / 0.25f, 0f, 1f);
			float thin = t < 0.7f ? 0f : (t - 0.7f) / 0.3f;
			float seed = cloud.At.X * 0.0131f + cloud.At.Y * 0.0071f;
			GasCloud.Draw(clouds, ToLocal(cloud.At), SpreadRadius * FeverCloudShare * (0.6f + 0.4f * grow), thin, seed, 0.6f);
		}
		if (windLeft > 0f)
		{
			// Gathers in its first moment and breaks up in its last.
			float thin = 1f - Mathf.Clamp(windLeft / 0.4f, 0f, 1f) * Mathf.Clamp((WindSeconds - windLeft) / 0.3f, 0f, 1f);
			Vector2 at = ToLocal(windAt);
			float radius = SpreadRadius * WindShare;
			GasCloud.Draw(clouds, at, radius, thin, 0.37f, 0.65f);
			// A thin trail of the plague streaming off its back.
			for (int i = 1; i <= 3; i++)
				GasCloud.Draw(clouds, at - windDir * radius * 0.7f * i, radius * (0.5f - 0.1f * i), MathF.Min(1f, thin + 0.25f * i), 0.37f + i * 0.21f, 0.5f);
		}
	}
}
