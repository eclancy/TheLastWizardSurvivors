using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// The gibbet's cage (.ai/chapter-wizards.md 4.4; its ask since .ai/unlock-puzzles.md 3.4, "choose his path"): what holds
// the walk and the rot on his way to their sentences, which no build sees and a single run would only show once. Every
// line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	private static void ValidateGibbetCage(List<string> warnings)
	{
		// The numbers keep their shape: he walks slower than the player, the landing is the walk the design names, the rot
		// takes him in more than one full crossing and less than ten.
		if (!(GibbetCageEvent.FollowShare > 0f && GibbetCageEvent.FollowShare < 1f))
			warnings.Add($"Gibbet cage: he must walk slower than the player ({GibbetCageEvent.FollowShare}).");
		if (!(GibbetCageEvent.LandingMin >= 1200f && GibbetCageEvent.LandingMax <= 2000f && GibbetCageEvent.LandingMin < GibbetCageEvent.LandingMax))
			warnings.Add($"Gibbet cage: the landing should be about 1,600 px off ({GibbetCageEvent.LandingMin}-{GibbetCageEvent.LandingMax}).");
		if (GibbetCageEvent.HealthMax <= 0 || GibbetCageEvent.RotDamage <= 0 || GibbetCageEvent.RotTickSeconds <= 0f || GibbetCageEvent.WinchSeconds <= 0f)
			warnings.Add("Gibbet cage: his bar, the rot's bite and the winch all need to be more than nothing.");
		int bitesToFall = (GibbetCageEvent.HealthMax + GibbetCageEvent.RotDamage - 1) / GibbetCageEvent.RotDamage;
		if (bitesToFall < 6 || bitesToFall > 40)
			warnings.Add($"Gibbet cage: the rot takes him in {bitesToFall} bites - a brush with a pool must not, and two or three crossings should.");

		ValidateLedWalk(warnings);
		ValidateRotLayouts(warnings);

		// The hint's cue is the vapour, on the sound's beats (sfx.py _FLASK_TAPS and _FLASK_COUGHS: 0.10, 0.34, 0.86, 1.10, 1.66).
		float[] beats = StageHintCue.FlaskBeatTimes;
		float[] sound = { 0.10f, 0.34f, 0.86f, 1.10f, 1.66f };
		if (beats.Length != sound.Length || beats.Zip(sound, (a, b) => MathF.Abs(a - b)).Any(d => d > 0.001f))
			warnings.Add($"Gibbet cage: the hint's vapour is on [{string.Join(", ", beats)}], not hint_flask_chime's taps and coughs.");
		if (StageHintCatalog.ForSite("gibbet_cage") is not StageHint hint || hint.Cue != HintCue.Vapour || hint.Sound != SfxCatalog.HintFlaskChime)
			warnings.Add("Gibbet cage: its hint must be hint_flask_chime with the vapour cue.");

		// The art, at the sizes the event reads it at (tools/art/gibbet.py).
		CheckGibbetSheet(warnings, "gibbet.png", new Vector2(96 * 4, 120), "the gibbet's four 96x120 frames");
		CheckGibbetSheet(warnings, "gibbet-cage.png", new Vector2(32 * 4, 54), "the cage's four 32x54 frames");
		CheckGibbetSheet(warnings, "gibbet-pool.png", new Vector2(84 * 3, 40), "the pool's three 84x40 frames");
		CheckGibbetSheet(warnings, "landing.png", new Vector2(120, 84), "the landing, 120x84");
		CheckGibbetSheet(warnings, "landing-punt.png", new Vector2(54 * 2, 56), "the punt's two 54x56 frames");
		const int cell = RotPoolLayout.ArtCell;
		CheckGibbetSheet(warnings, "rot-pools.png",
			new Vector2(cell * RotPoolLayout.ArtFrames, cell * RotPoolLayout.Radii.Length * RotPoolLayout.ArtShapes),
			$"the rot pools: {RotPoolLayout.Radii.Length} sizes x {RotPoolLayout.ArtShapes} shapes, {RotPoolLayout.ArtFrames} {cell}x{cell} frames each");
	}

	// He walks the player's steps, on paper (LedEscort).
	private static void ValidateLedWalk(List<string> warnings)
	{
		// Round a lake: the player goes east, round a lake that fills the square x 300-700, y -200-200 (down its south
		// shore at y 260), and on east; the straight line from start to end crosses the lake. He must keep to the walked
		// path the whole way (never inside the lake), never outpace his share, and end close behind once the player stops.
		{
			Vector2[] path = { new(0f, 0f), new(200f, 0f), new(260f, 260f), new(740f, 260f), new(800f, 0f), new(1200f, 0f) };
			LedWalk walk = WalkLed(path, null, new Vector2(-80f, 0f), 30f);
			if (walk.Visited(p => p.X > 300f && p.X < 700f && p.Y > -200f && p.Y < 200f))
				warnings.Add("Gibbet cage: led round a lake, he walked into it - he must keep to the player's steps.");
			if (walk.WorstOffPath > LedEscort.CrumbSpacing + 1f)
				warnings.Add($"Gibbet cage: led round a lake, he strayed {walk.WorstOffPath:0} px off the path the player walked.");
			if (walk.Fastest > GibbetCageEvent.FollowShare * LedWalk.Speed * 1.01f)
				warnings.Add($"Gibbet cage: he walked {walk.Fastest:0} px/s, faster than {GibbetCageEvent.FollowShare:P0} of the player's {LedWalk.Speed}.");
			if (walk.EndGap > 64f + LedEscort.CrumbSpacing + 1f || walk.EndGap < 40f)
				warnings.Add($"Gibbet cage: once the player stopped he ended {walk.EndGap:0} px from him, not close behind.");
		}
		// Up a dead end and back, while he is far enough behind not to have reached it: the player walks east, 260 px up
		// a blind alley to the north and back down it (a stride to the side, as a player retraces his steps), then on
		// east. The way back closes a loop, and he leaves the alley out - going back costs the player time, not him.
		{
			Vector2[] path = { new(0f, 0f), new(400f, 0f), new(400f, -260f), new(424f, -260f), new(424f, 20f), new(900f, 20f) };
			LedWalk walk = WalkLed(path, null, new Vector2(-240f, 0f), 30f);
			if (walk.Visited(p => p.Y < -60f))
				warnings.Add($"Gibbet cage: led up a dead end and back, he walked up it too (as far as y {walk.Highest:0}) - a loop the player closes is one he leaves out.");
			if (walk.LoopsLeftOut < 1)
				warnings.Add("Gibbet cage: led up a dead end and back, he left no loop out.");
			if (walk.WorstOffPath > LedEscort.LoopRadius * 0.5f + 1f)
				warnings.Add($"Gibbet cage: leaving the dead end out, he strayed {walk.WorstOffPath:0} px off the walked path (at most half a loop's step).");
		}
		// The same, but the step across is refused (the gibbet refuses one through the rot or over water): he walks the
		// whole loop, as the player did.
		{
			Vector2[] path = { new(0f, 0f), new(400f, 0f), new(400f, -260f), new(424f, -260f), new(424f, 20f), new(900f, 20f) };
			LedWalk walk = WalkLed(path, (_, _) => false, new Vector2(-240f, 0f), 30f);
			if (!walk.Visited(p => p.Y < -200f) || walk.LoopsLeftOut != 0)
				warnings.Add("Gibbet cage: with the step across refused, he must walk the loop the player walked.");
		}
	}

	private sealed class LedWalk
	{
		public const float Speed = 220f;
		public readonly List<Vector2> Points = new();
		public float WorstOffPath, Fastest, EndGap, Highest = float.MaxValue;
		public int LoopsLeftOut;

		public bool Visited(Func<Vector2, bool> where) => Points.Any(where);
	}

	// The leader walks the path at LedWalk.Speed and stops at its end; he follows from `start` for `seconds`.
	private static LedWalk WalkLed(Vector2[] path, Func<Vector2, Vector2, bool> mayCut, Vector2 start, float seconds)
	{
		var him = new LedEscort { SpeedShare = GibbetCageEvent.FollowShare, FollowGap = 64f, MayCut = mayCut };
		him.Reset(start);
		var walk = new LedWalk();
		const float Dt = 1f / 60f;
		Vector2 leader = path[0];
		int leg = 1;
		for (float t = 0f; t < seconds; t += Dt)
		{
			if (leg < path.Length)
			{
				leader = leader.MoveToward(path[leg], LedWalk.Speed * Dt);
				if (leader.DistanceTo(path[leg]) < 0.01f)
					leg++;
			}
			Vector2 before = him.Position;
			him.Step(Dt, leader, LedWalk.Speed);
			walk.Fastest = MathF.Max(walk.Fastest, before.DistanceTo(him.Position) / Dt);
			Vector2 p = him.Position;
			walk.Points.Add(p);
			walk.Highest = MathF.Min(walk.Highest, p.Y);
			float off = float.MaxValue;
			for (int i = 0; i + 1 < path.Length; i++)
				off = MathF.Min(off, RotPoolLayout.DistanceToSegment(p, path[i], path[i + 1]));
			if (p.X > path[0].X)
				walk.WorstOffPath = MathF.Max(walk.WorstOffPath, off);
		}
		walk.EndGap = him.Position.DistanceTo(leader);
		walk.LoopsLeftOut = him.LoopsLeftOut;
		return walk;
	}

	// The rot on his way (RotPoolLayout), laid out on paper over seeds, walk lengths and bearings - on open ground and
	// with a lake on the way - and held to every rule of 3.4: six to twelve pools (RotPoolLayout.MaxCount), 120-260 px
	// across, none at the door or the landing, no squeeze between two of them, the straight line cut (the wrong
	// approach), and a way through that keeps its distance from every pool. 24 layouts, the walk's shortest to its
	// longest, three bearings, every other one with a lake: this runs at every launch, and the 24 take 40-140 ms of a
	// debug build (the whole of RunAll took 31 s on the same machine). The wide sweep (3,000 seeds, the
	// same rules) was run once by hand when the rot was built - the Built note in .ai/unlock-puzzles.md 3.4 says what it
	// found - and the generator never returns a layout without a way through in any case (it takes pools away until there
	// is one); what this guards is that it does not have to.
	private static void ValidateRotLayouts(List<string> warnings)
	{
		if (RotPoolLayout.Radii.Min() * 2f < 120f - 0.5f || RotPoolLayout.Radii.Max() * 2f > 260f + 0.5f)
			warnings.Add($"Gibbet cage: the rot's pools must be 120-260 px across (3.4), not {RotPoolLayout.Radii.Min() * 2f}-{RotPoolLayout.Radii.Max() * 2f}.");
		if (RotPoolLayout.MergeMin < 2f * RotPoolLayout.Radii.Max() * (1f - RotPoolLayout.BiteShare))
			warnings.Add($"Gibbet cage: merged pools overlap by {RotPoolLayout.MergeMin} px, too little for their bites to meet - a neck of rot you can see would let him through.");
		int trimmed = 0, layouts = 0;
		var problems = new List<string>();
		for (int s = 0; s < 8; s++)
		{
			for (int b = 0; b < 3; b++)
			{
				int lake = (s + b) % 2;
				{
					layouts++;
					ulong seed = 0x51B6E7UL + (ulong)s * 7919UL + (ulong)b * 104729UL;
					float length = Mathf.Lerp(GibbetCageEvent.LandingMin - 50f, GibbetCageEvent.LandingMax + 50f, s / 7f);
					Vector2 door = new(37f * s, -21f * s);
					Vector2 landing = door + Vector2.FromAngle(0.4f + b * 2.1f + s * 0.13f) * length;
					// The lake: 170 px round, a third of the way along and a little off the line - the kind the Swamp has.
					Vector2 lakeAt = door.Lerp(landing, 0.3f + 0.04f * s) + (landing - door).Normalized().Orthogonal() * (80f - 22f * s);
					Func<Vector2, bool> ground = lake == 1 ? p => p.DistanceTo(lakeAt) > 170f : null;
					RotPoolLayout rot = RotPoolLayout.Generate(door, landing, seed, ground);
					if (rot.Trimmed)
						trimmed++;
					string where = $"seed {s}, bearing {b}, {length:0} px{(lake == 1 ? ", a lake" : "")}";
					foreach (string problem in RotLayoutProblems(rot, door, landing, ground))
						problems.Add($"{where}: {problem}");
				}
			}
		}
		foreach (string problem in problems.Take(6))
			warnings.Add($"Gibbet cage: the rot - {problem}");
		if (problems.Count > 6)
			warnings.Add($"Gibbet cage: the rot - and {problems.Count - 6} more.");
		// Taking pools away is the fallback, not the way it is made: if most rolls need it the shape is wrong.
		if (trimmed * 8 > layouts)
			warnings.Add($"Gibbet cage: {trimmed} of {layouts} rot layouts kept no rule until pools were taken away.");
	}

	private static IEnumerable<string> RotLayoutProblems(RotPoolLayout rot, Vector2 door, Vector2 landing, Func<Vector2, bool> ground)
	{
		// On open ground every pool rolled is laid; on the Swamp's, a pool that would lie on water is not.
		if ((ground == null && rot.Pools.Count < RotPoolLayout.MinCount) || rot.Pools.Count > RotPoolLayout.MaxCount)
			yield return $"{rot.Pools.Count} pools, not {RotPoolLayout.MinCount}-{RotPoolLayout.MaxCount}";
		foreach (RotPoolLayout.Pool pool in rot.Pools)
		{
			if (pool.Centre.DistanceTo(door) - pool.Radius < RotPoolLayout.StartClear - 0.5f)
				yield return $"a pool {pool.Centre.DistanceTo(door) - pool.Radius:0} px from the way's start (the winch)";
			if (pool.Centre.DistanceTo(landing) - pool.Radius < RotPoolLayout.EndClear - 0.5f)
				yield return $"a pool {pool.Centre.DistanceTo(landing) - pool.Radius:0} px from the landing";
			if (ground != null && !ground(pool.Centre))
				yield return "a pool laid on water";
		}
		if (RotPoolLayout.FirstSqueeze(rot.Pools, ground) >= 0)
			yield return "two pools neither merged nor a way apart - a slot that looks passable and puts him in the rot";
		if (!RotPoolLayout.CutsStraightLine(rot.Pools, door, landing))
			yield return "the straight line to the landing misses the rot - there is no puzzle";
		Vector2[] route = rot.Route;
		if (route == null || route.Length < 2)
		{
			yield return "no way through";
			yield break;
		}
		if (route[0].DistanceTo(door) > 1f || route[^1].DistanceTo(landing) > 1f)
			yield return "the way through does not run from the door to the landing";
		// Every leg keeps off every pool's bite by more than a stride, and off the water.
		float keep = RotPoolLayout.RouteClearance - RotPoolLayout.RouteCell * 0.5f - 1f;
		for (int i = 1; i < route.Length; i++)
		{
			foreach (RotPoolLayout.Pool pool in rot.Pools)
			{
				float off = RotPoolLayout.DistanceToSegment(pool.Centre, route[i - 1], route[i]) - pool.Radius;
				if (off < keep)
				{
					yield return $"the way through passes {off:0} px from a pool's edge";
					yield break;
				}
			}
			if (ground != null && !ground(route[i - 1].Lerp(route[i], 0.5f)))
				yield return "the way through crosses water";
		}
		// The wrong approach costs him: walked straight, the rot bites. Walked along the way through, it never does.
		if (rot.CrossesBite(route[0], route[^1]) == false)
			yield return "walking straight to the landing never enters a pool's bite";
		for (int i = 1; i < route.Length; i++)
		{
			if (rot.CrossesBite(route[i - 1], route[i]))
			{
				yield return "the way through enters a pool's bite";
				yield break;
			}
		}
	}

	private static void CheckGibbetSheet(List<string> warnings, string name, Vector2 size, string what)
	{
		string path = "res://assets/bonelight/world/gibbet/" + name;
		Texture2D texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		if (texture == null)
			warnings.Add($"Gibbet cage: {path} ({what}) is missing or not imported.");
		else if (texture.GetSize() != size)
			warnings.Add($"Gibbet cage: {path} is {texture.GetSize()}, expected {size} ({what}).");
	}
}
