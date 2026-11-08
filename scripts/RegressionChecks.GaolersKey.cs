using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// The Gaoler's key, slipped past the lanterns (.ai/unlock-puzzles.md 3.8; GaolersKeyEvent, GaolRounds, LanternCone,
// LanternBearer): what holds the puzzle to its sentence, which no build sees and a single run would show once. Every line
// is a warning, as the rest of RunAll's are.
//
//   1. The numbers keep their promises: a bearer walks slower than he does, a turned one too, and the proof's walker is
//      slower still; the tick grid lines up with the lattice; the dwell is as long as the lock takes.
//   2. What is drawn is what catches: on real maze ground, no point is caught outside the drawn fan, no point drawn lit
//      and further than one ray's gap from the fan's edge is spared, and no caught point has a wall between it and the
//      lantern.
//   3. Walking straight through a cone drops the key, where he stood, and it cannot be taken up again until he has
//      stepped off it - and walking past the cone, or behind a wall it lights up to, drops nothing.
//   4. Every generated set of rounds leaves a gap: gaols laid on real maze ground from many seeds, exactly as the event lays
//      them (walls baked, nothing else read), each searched again at more phase sets than the generator tried (a turned
//      bearer goes back to its round at any phase) - and along the way home and its side passages only
//      (GaolLayout.WayHeKnows), never round through the maze: the cautious walker (GaolSlip.PlanningCone) must slip from
//      beside the key to the cell's turned lock every time. And the rounds bite: a straight walk home, at once, is caught
//      on most layouts.
public static partial class RegressionChecks
{
	// Real maze ground for the gaol: one padded grid of the Cursed Dungeon's lattice, well out from the start's clearing.
	private const int GaolTile = 48;
	private const int GaolChunkTiles = 96;
	private const int GaolMargin = 48;

	private static GaolGround GaolGroundFor(ulong seed, int chunkX, int chunkY, out Vector2 middle)
	{
		var generator = new LevelGenerator();
		string[,] grid = generator.GenerateChunk(StageEnvironmentKind.Castle, seed, chunkX, chunkY, GaolChunkTiles, GaolMargin, 6f, 4f);
		int size = grid.GetLength(0);
		int x0 = chunkX * GaolChunkTiles - GaolMargin, y0 = chunkY * GaolChunkTiles - GaolMargin;
		bool IsWall(Vector2 w)
		{
			int tx = Mathf.FloorToInt(w.X / GaolTile) - x0, ty = Mathf.FloorToInt(w.Y / GaolTile) - y0;
			return tx < 0 || ty < 0 || tx >= size || ty >= size || grid[tx, ty] == LevelGenerator.WallTerrain;
		}
		middle = new Vector2((x0 + size / 2) * GaolTile, (y0 + size / 2) * GaolTile);
		return new GaolGround
		{
			Origin = Vector2.Zero,
			Pitch = LevelGenerator.MazeCellTiles * GaolTile,
			Tile = GaolTile,
			IsWall = IsWall,
		};
	}

	private static void ValidateGaolersKey(List<string> warnings)
	{
		// 1. The numbers. Read into locals: they are constants, and a check on constants alone is code the compiler calls
		// unreachable - it is not, the day one of them is changed.
		float hisWalk = 220f;
		float pace = GaolRound.Speed, back = GaolRound.Speed * LanternBearer.ReturnSpeedShare, turned = LanternBearer.TurnedSpeedShare;
		float lose = LanternBearer.LoseSeconds, most = LanternBearer.TurnedMaxSeconds;
		if (pace >= hisWalk * 0.6f)
			warnings.Add($"Gaoler's key: a bearer walks its round at {pace} px/s; it must be well under his {hisWalk} so it can be followed and outwalked.");
		if (back >= hisWalk * 0.75f)
			warnings.Add($"Gaoler's key: a bearer walks back to its round at {back:0} px/s; it should stay well under his walk.");
		if (turned >= 1f || lose <= 0f || lose >= most)
			warnings.Add($"Gaoler's key: a turned bearer must be slower than him ({turned}) and give up ({lose} s out of its light, under {most} s at most).");
		float walker = GaolWalk.Step / GaolSlip.Tick;
		if (walker >= hisWalk)
			warnings.Add($"Gaoler's key: the proof's walker goes {walker:0} px/s, not under his {hisWalk}: a way it finds could be too quick for him.");
		float pitch = LevelGenerator.MazeCellTiles * GaolTile;
		if (Mathf.Abs(pitch / GaolSweep.ArcStep - Mathf.Round(pitch / GaolSweep.ArcStep)) > 0.001f
			|| Mathf.Abs(pitch / GaolWalk.Step - Mathf.Round(pitch / GaolWalk.Step)) > 0.001f)
			warnings.Add($"Gaoler's key: a cell ({pitch} px) is not a whole number of bearer steps ({GaolSweep.ArcStep}) and walk steps ({GaolWalk.Step}); the sweeps drift.");
		float dwell = GaolSlip.DwellSteps * GaolSlip.Tick;
		if (dwell < GaolersKeyEvent.DefaultUnlockSeconds - 0.001f)
			warnings.Add($"Gaoler's key: the proof stands {dwell:0.00} s at the door; the lock takes longer to turn.");

		ValidateLanternCone(warnings);
		ValidateKeyCarry(warnings);
		GD.Print(ValidateGaolLayouts(warnings));
	}

	// 2. What is drawn is what catches.
	private static void ValidateLanternCone(List<string> warnings)
	{
		GaolGround ground = GaolGroundFor(4242UL, 3, 2, out Vector2 middle);
		var rng = new LevelGenerator.Rng(LevelGenerator.Hash(4242UL, 3, 2, 0x6A02));
		var cone = new LanternCone();
		Vector2I home = ground.CellOf(middle);
		int aims = 0, samples = 0, outsideFan = 0, spared = 0, throughWall = 0, caught = 0;
		string firstOutside = null, firstSpared = null, firstWall = null;
		float rayGap = 2f * Mathf.DegToRad(LanternCone.DefaultHalfAngleDegrees) / (LanternCone.Rays - 1) * LanternCone.DefaultReach;
		float tolerance = Mathf.Max(rayGap, LanternCone.MarchStep) + 2f;
		for (int k = 0; k < 160; k++)
		{
			// A lantern on a corridor's middle (a cell centre or a passage), pointing along it or anywhere.
			var cell = new Vector2I(home.X + rng.Next(9) - 4, home.Y + rng.Next(9) - 4);
			if (!ground.IsCell(cell))
				continue;
			Vector2 origin = ground.CentreOf(cell) + (rng.NextFloat() < 0.5f
				? (Vector2)GaolGround.Steps[rng.Next(4)] * (ground.Pitch * 0.5f * rng.NextFloat())
				: Vector2.Zero);
			if (ground.Wall(origin))
				continue;
			Vector2 facing = Vector2.FromAngle(rng.NextFloat() * Mathf.Tau);
			cone.Aim(origin, facing, ground.WallTest);
			aims++;
			for (int s = 0; s < 120; s++)
			{
				Vector2 p = origin + Vector2.FromAngle(facing.Angle() + (rng.NextFloat() * 2f - 1f) * Mathf.DegToRad(cone.HalfAngleDegrees + 6f))
					* (rng.NextFloat() * (cone.Reach + 20f));
				samples++;
				bool covers = cone.Covers(p);
				float edge = FanEdgeDistance(cone, p, out bool inFan);
				if (covers)
				{
					caught++;
					if (!inFan && edge > 0.5f)
					{
						outsideFan++;
						firstOutside ??= $"{p} from a lantern at {origin} facing {facing}, {edge:0.0} px outside the fan";
					}
					if (WallBetween(ground, origin, p))
					{
						throughWall++;
						firstWall ??= $"{p} from a lantern at {origin}";
					}
				}
				else if (inFan && edge > tolerance)
				{
					spared++;
					firstSpared ??= $"{p} from a lantern at {origin} facing {facing}, {edge:0.0} px inside the fan";
				}
			}
		}
		if (aims < 80 || caught < 1000)
			warnings.Add($"Gaoler's key: the cone check aimed {aims} lanterns and caught {caught} of {samples} points - too few to test what it claims.");
		if (outsideFan > 0)
			warnings.Add($"Gaoler's key: {outsideFan} point(s) caught OUTSIDE the drawn light (first: {firstOutside}). LanternCone.Covers no longer reads the fan it draws.");
		if (spared > 0)
			warnings.Add($"Gaoler's key: {spared} point(s) drawn lit, more than a ray's gap ({tolerance:0} px) inside the fan, were not caught (first: {firstSpared}).");
		if (throughWall > 0)
			warnings.Add($"Gaoler's key: {throughWall} point(s) caught with a wall between them and the lantern (first: {firstWall}). The walls must stop the light.");
	}

	// How far a point is from the edge of the fan LanternCone draws (the triangles from its point to each pair of ray ends),
	// and whether it is inside it. Point-in-triangle and the distance to the fan's outline, by brute force.
	private static float FanEdgeDistance(LanternCone cone, Vector2 p, out bool inside)
	{
		inside = false;
		for (int k = 0; k < LanternCone.Rays - 1 && !inside; k++)
			inside = InTriangle(p, cone.Origin, cone.EndOf(k), cone.EndOf(k + 1));
		float d = float.MaxValue;
		Vector2 prev = cone.Origin;
		for (int k = 0; k <= LanternCone.Rays; k++)
		{
			Vector2 next = k < LanternCone.Rays ? cone.EndOf(k) : cone.Origin;
			Vector2 seg = next - prev;
			float t = seg.LengthSquared() > 1e-6f ? Mathf.Clamp((p - prev).Dot(seg) / seg.LengthSquared(), 0f, 1f) : 0f;
			d = Mathf.Min(d, p.DistanceTo(prev + seg * t));
			prev = next;
		}
		return d;
	}

	// Inside or on a triangle; a degenerate one holds nothing.
	private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
	{
		float area = (b - a).Cross(c - a);
		if (Mathf.Abs(area) < 1e-4f)
			return false;
		float s1 = (b - a).Cross(p - a), s2 = (c - b).Cross(p - b), s3 = (a - c).Cross(p - c);
		return area > 0f ? s1 >= 0f && s2 >= 0f && s3 >= 0f : s1 <= 0f && s2 <= 0f && s3 <= 0f;
	}

	// A wall crossed, at the light's own resolution: a run of wall along the segment at least a march step long. A wall
	// tile's corner clipped for less than that can fall between a ray's samples, and between two rays, by design.
	private static bool WallBetween(GaolGround ground, Vector2 a, Vector2 b)
	{
		const float sample = 3f;
		int n = Math.Max(1, Mathf.CeilToInt(a.DistanceTo(b) / sample));
		int run = 0;
		for (int k = 1; k <= n; k++)
		{
			run = ground.Wall(a.Lerp(b, k / (float)n)) ? run + 1 : 0;
			if (run * sample > LanternCone.MarchStep + sample)
				return true;
		}
		return false;
	}

	// 3. The key: walked straight through a cone it falls where he stood; walked past it, or behind a wall, it does not.
	private static void ValidateKeyCarry(List<string> warnings)
	{
		// Open floor: no walls at all, a lantern at the origin looking east.
		var open = new LanternCone();
		open.Aim(Vector2.Zero, Vector2.Right, _ => false);
		{
			var key = new KeyCarry();
			key.Lay(new Vector2(150f, -400f));
			// Up to the key and onto it, then south through the cone's middle (x 150, well inside 300 px and 30 degrees).
			Vector2 at = new(150f, -470f);
			Vector2? droppedAt = null;
			bool pickedUp = false;
			for (int i = 0; i < 80 && droppedAt == null; i++)
			{
				at += new Vector2(0f, 12f);
				KeyCarry.Change change = key.Step(at, open.Covers(at));
				pickedUp |= change == KeyCarry.Change.PickedUp;
				if (change == KeyCarry.Change.Dropped)
					droppedAt = at;
			}
			if (!pickedUp)
				warnings.Add("Gaoler's key: walking onto the key did not pick it up.");
			if (droppedAt is not Vector2 fell)
				warnings.Add("Gaoler's key: walked straight through a lantern's cone with the key, he was never caught - the key never fell.");
			else
			{
				if (!open.Covers(fell) || open.Covers(fell - new Vector2(0f, 12f)))
					warnings.Add($"Gaoler's key: the key fell at {fell}, not on the first step into the light.");
				if (key.KeyAt != fell || key.Carrying || key.Armed)
					warnings.Add($"Gaoler's key: caught, the key must lie where he stood ({fell}, lies at {key.KeyAt}), out of his hands and not to be taken up at once.");
				// Standing on it in the light: nothing happens until he steps off it.
				for (int i = 0; i < 30; i++)
					key.Step(fell, open.Covers(fell));
				if (key.Carrying || key.Pickups != 1)
					warnings.Add("Gaoler's key: standing on the fallen key in the light, it was taken up again - it must wait for him to step off it.");
				// Off it, then back on in the light: taken up and lost on the same frame.
				key.Step(fell + new Vector2(0f, KeyCarry.RearmRadius + 10f), false);
				KeyCarry.Change again = key.Step(fell, open.Covers(fell));
				if (again != KeyCarry.Change.Dropped || key.Drops != 2 || key.Pickups != 2)
					warnings.Add($"Gaoler's key: taken up again inside the light it must fall again on the same frame (got {again}, {key.Drops} drops, {key.Pickups} pickups).");
			}
		}
		{
			// Past the cone: north to south at x = -40, behind the lantern. Never in its light.
			var key = new KeyCarry();
			key.Lay(new Vector2(-40f, -300f));
			for (int i = 0; i < 60; i++)
				key.Step(new Vector2(-40f, -300f + i * 12f), open.Covers(new Vector2(-40f, -300f + i * 12f)));
			if (!key.Carrying || key.Drops != 0)
				warnings.Add("Gaoler's key: walked behind a lantern, never in its cone, the key fell anyway.");
		}
		{
			// A wall across the cone 120 px out (x 120-168): the light stops at it, and a walk behind it drops nothing.
			var walled = new LanternCone();
			walled.Aim(Vector2.Zero, Vector2.Right, p => p.X >= 120f && p.X < 168f);
			if (walled.Covers(new Vector2(220f, 0f)) || !walled.Covers(new Vector2(100f, 0f)))
				warnings.Add("Gaoler's key: a wall across the cone did not stop the light at its face.");
			var key = new KeyCarry();
			key.Lay(new Vector2(220f, -200f));
			for (int i = 0; i < 40; i++)
			{
				Vector2 at = new(220f, -200f + i * 12f);
				key.Step(at, walled.Covers(at));
			}
			if (!key.Carrying || key.Drops != 0)
				warnings.Add("Gaoler's key: a walk behind a wall the light stops at dropped the key.");
		}
	}

	// 4. Every set of rounds leaves a gap, and bites. Returns the line the validator prints of what it measured.
	private static string ValidateGaolLayouts(List<string> warnings)
	{
		const int layouts = 12;
		const int extraPhaseSets = 8;
		int made = 0, fallbacks = 0, bitten = 0, noGap = 0, broken = 0, roundsTotal = 0, offTheWay = 0, knownSlips = 0, knownWaited = 0;
		float worstSlip = 0f, shareSum = 0f;
		double worstMs = 0, totalMs = 0;
		string firstNoGap = null, firstBroken = null, firstOffTheWay = null;
		for (int k = 0; k < layouts; k++)
		{
			ulong seed = 20261108UL + (ulong)k * 7919UL;
			int cx = 2 + k % 3, cy = 1 + (k / 3) % 3;
			GaolGround ground = GaolGroundFor(seed, cx, cy, out Vector2 middle);
			Vector2I home = ground.CellOf(middle);
			for (int r = 0; r < 4 && !ground.IsCell(home); r++)
				home += new Vector2I(1, 0);
			long started = System.Diagnostics.Stopwatch.GetTimestamp();
			// As the event lays it: the walls round the cell baked first, then nothing but the baked box read (the worker's
			// rule, GaolersKeyEvent.StartGaol) - and any question past the box counted, which must never happen.
			ground.Bake(home - new Vector2I(GaolLayout.SearchRadius, GaolLayout.SearchRadius), home + new Vector2I(GaolLayout.SearchRadius, GaolLayout.SearchRadius));
			ground.BakedOnly = true;
			GaolLayout layout = GaolLayout.Generate(ground, home, Vector2.FromAngle(k * 2.39996f), seed);
			ground.BakedOnly = false;
			if (ground.OutsideQueries > 0)
			{
				broken++;
				firstBroken ??= $"seed {seed}: the generator asked {ground.OutsideQueries} question(s) outside the baked walls; the worker would lay a different gaol";
			}
			double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
			worstMs = Math.Max(worstMs, ms);
			totalMs += ms;
			if (layout == null)
			{
				broken++;
				firstBroken ??= $"seed {seed}: no post four to six cells from the cell at {home}";
				continue;
			}
			made++;
			roundsTotal += layout.Rounds.Count;
			if (layout.Fallback)
				fallbacks++;
			if (layout.CaughtShare >= GaolLayout.MinCaughtShare)
				bitten++;
			shareSum += layout.CaughtShare;
			worstSlip = Mathf.Max(worstSlip, layout.SlowestSlipSeconds);

			// Its shape: a post four to six cells from the cell; every round a closed loop of joined cells, off the cell and
			// the post, sharing no cell with another.
			int steps = layout.Route == null ? -1 : layout.Route.Length - 1;
			if (steps < GaolLayout.PostMinSteps || steps > GaolLayout.PostMaxSteps)
			{
				broken++;
				firstBroken ??= $"seed {seed}: the post is {steps} steps from the cell";
			}
			if (layout.Rounds.Count == 0)
			{
				broken++;
				firstBroken ??= $"seed {seed}: no rounds at all ({layout.LoopsFound} loops found)";
			}
			var used = new HashSet<Vector2I>();
			foreach (GaolRound round in layout.Rounds)
			{
				for (int i = 0; i < round.Cells.Length; i++)
				{
					Vector2I c = round.Cells[i];
					if (!ground.Open(c, round.Cells[(i + 1) % round.Cells.Length]) || c == layout.Home || c == layout.Post || !used.Add(c))
					{
						broken++;
						firstBroken ??= $"seed {seed}: a round of {round.Cells.Length} is not a closed loop clear of the cell, the post and the other rounds (at {c})";
						break;
					}
				}
			}

			// The gap, again, at phases the generator never tried: each a turned bearer could have come back in. And on the
			// way he knows (GaolLayout.WayHeKnows): the shortest way home and the side passages off it, nothing round - so
			// the gap is there for a player who watches and steps aside, not only for a search of the whole maze. A slip
			// along it is a slip; only if there is none is the whole region asked, to say which it is.
			var rng = new LevelGenerator.Rng(LevelGenerator.Hash(seed, 9, 9, 0x6A03));
			int key = layout.Walk.PointOfCell(layout.Post);
			int start = key >= 0 && layout.Walk.NeighboursOf(key).Count > 0 ? layout.Walk.NeighboursOf(key)[0] : key;
			int homePoint = layout.Walk.PointOfCell(layout.Home);
			bool[] known = layout.Rounds.Count > 0 ? layout.WayHeKnows(layout.Walk, out bool[] onRoute) : null;
			for (int set = 0; set < extraPhaseSets && layout.Rounds.Count > 0; set++)
			{
				int[] starts = new int[layout.Sweeps.Count];
				for (int i = 0; i < starts.Length; i++)
					starts[i] = rng.Next(layout.Sweeps[i].Positions);
				int horizon = GaolSlip.StepsFor(GaolLayout.SlipHorizonSeconds);
				GaolSlip slip = GaolSlip.Search(layout.Walk, layout.Sweeps, starts, start, key, homePoint, horizon, keepPath: true,
					roam: false, allowed: known);
				if (slip.Found)
				{
					worstSlip = Mathf.Max(worstSlip, slip.Seconds);
					knownSlips++;
					if (slip.HeldWaits > 0)
						knownWaited++;
					continue;
				}
				GaolSlip anywhere = GaolSlip.Search(layout.Walk, layout.Sweeps, starts, start, key, homePoint, horizon, keepPath: false, roam: false);
				if (anywhere.Found)
				{
					offTheWay++;
					firstOffTheWay ??= $"seed {seed}, bearers at [{string.Join(", ", starts)}]";
				}
				else
				{
					noGap++;
					firstNoGap ??= $"seed {seed}, bearers at [{string.Join(", ", starts)}]";
				}
			}
		}
		if (broken > 0)
			warnings.Add($"Gaoler's key: {broken} problem(s) with the gaols laid on maze ground (first: {firstBroken}).");
		if (noGap > 0)
			warnings.Add($"Gaoler's key: {noGap} phase set(s) left NO gap a walker at {GaolWalk.Step / GaolSlip.Tick:0} px/s could slip through "
				+ $"within {GaolLayout.SlipHorizonSeconds:0} s (first: {firstNoGap}). Every set of rounds must leave one, whatever the bearers' phases.");
		if (offTheWay > 0)
			warnings.Add($"Gaoler's key: {offTheWay} phase set(s) left a gap only by going round through the maze, none along the way home "
				+ $"and its side passages (first: {firstOffTheWay}). A player who watches the rounds and steps aside must be able to slip through.");
		if (made > 0 && bitten < made * 3 / 4)
			warnings.Add($"Gaoler's key: only {bitten} of {made} gaols catch a straight walk home at least {GaolLayout.MinCaughtShare * 100f:0}% of the time "
				+ $"(mean {shareSum / made * 100f:0}%); the rounds are not sweeping the way.");
		if (made > 0 && roundsTotal < made * 3 / 2)
			warnings.Add($"Gaoler's key: {roundsTotal} rounds over {made} gaols - fewer than one and a half each; the way is barely watched.");
		if (worstSlip > GaolLayout.SlipHorizonSeconds * 0.6f)
			warnings.Add($"Gaoler's key: the slowest slip home took {worstSlip:0.0} s; the gaps are too rare to read.");
		return $"ContentValidator: Gaoler's key: {made} gaols laid, {roundsTotal} rounds, {bitten} bite a straight walk "
			+ $"(mean {(made > 0 ? shareSum / made * 100f : 0f):0}%), {fallbacks} kept as the best of their rolls; slipped along the way home at "
			+ $"{knownSlips} of {knownSlips + offTheWay + noGap} further phase sets ({knownWaited} of them standing in the dark with the key "
			+ $"to let a light pass), slowest slip {worstSlip:0.0} s; generated in {totalMs / Math.Max(1, layouts):0} ms each, {worstMs:0} at worst.";
	}
}
