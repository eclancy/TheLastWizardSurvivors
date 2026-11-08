using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// _ExploreProbe's Cursed Dungeon leg: the Gaoler's key slipped past the lanterns (.ai/unlock-puzzles.md 3.8). See the header
// of _ExploreProbe.cs for what it does; here is how.
//
// WHAT IT KNOWS, AND WHY THAT IS FAIR. A player who has watched the gaol for a lap knows each bearer's round (it walks the
// same loop of corridors over and over, its light ahead of it) and sees where each bearer is now. That is all the probe
// reads: each round's cells and its bearer's place on it (LanternBearer.Arc), never anything the screen does not show. It
// plans with a lantern a little larger than the bearers carry (GaolSlip.PlanningCone), as a cautious player keeps clear of
// the edge of the light, walks at 200 px a second against his 220, and plans again whenever what it planned on stops being
// true - a bearer turned or walking back, the key not where it was.
public partial class _ExploreProbe
{
	// --into-cone: once the key is in hand, walk into a lit cone on purpose (see the header).
	private bool intoCone;
	// --trace-gaol: every frame round a planned pickup, where he stands against the key.
	private bool traceGaol = Array.IndexOf(OS.GetCmdlineUserArgs(), "--trace-gaol") >= 0;

	private float slipClearClock;
	private GaolSlip slipPlan;
	private GaolWalk slipWalk;
	private float slipClock;
	private float slipRetryAt;
	private int slipCatchesAtPlan;
	private bool slipCarryingAtPlan;
	private int slipPlans;
	private int slipPlansFailed;
	private int slipHeldWaits;
	private int slipFreeWaits;
	private float slipPlannedSeconds;
	private float slipWalked;
	private Vector2 slipLastFeet = Vector2.Inf;
	private int slipRetreatFrames;
	private bool bearerEverTargeted;
	private float wardenDiedFromPost = -1f;
	private float wardenFarthestFromPost;
	private float slipStartedAt = -1f;
	private float slipHomeAt = -1f;
	private int gaolCatchesSeen;

	// --into-cone, and what it saw: 0 before, 1 walking into the light, 2 caught (watching the turn), 3 done.
	private int conePhase;
	private int coneBearer = -1;
	private Vector2 coneBodyAtCatch;
	private Vector2 coneKeyAt;
	private float coneFacingDot = -2f;
	private float coneDistanceAtCatch;
	private float coneClosest = float.MaxValue;
	private float coneWatch;
	private bool coneTurned;
	private int coneReturnsBefore;
	private bool coneReturnedToRound;
	private int coneBlows;
	private float coneGaveUpAt = -1f;
	private bool coneLostHim;
	private float coneFollowed;

	private Vector2 SlipStep(GaolersKeyEvent gaol, float delta, out bool clear)
	{
		clear = false;
		slipClearClock += delta;
		if (slipClearClock >= 5f)
		{
			slipClearClock = 0f;
			clear = true;
		}
		Vector2 feetOffset = player.FootPosition - player.GlobalPosition;
		Vector2 feet = player.FootPosition;
		if (slipLastFeet != Vector2.Inf)
			slipWalked += feet.DistanceTo(slipLastFeet);
		slipLastFeet = feet;
		foreach (LanternBearer b in gaol.Bearers)
			if (b.IsInGroup("enemies") || b.IsInGroup("enemy") || EnemyRegistry.Snapshot.Any(x => (Node)x == b))
				bearerEverTargeted = true;
		WatchTheCatch(gaol, delta);

		// The lock turning and the gate swinging: stand.
		if (gaol.IsUnlocking)
		{
			if (slipHomeAt < 0f)
				slipHomeAt = game.RunSeconds;
			return player.GlobalPosition;
		}

		// The hunt: beside the Warden at its post, close enough to bleed it (the probe's 140 px) and never on the spot its key
		// will fall. Its distance from the post is watched: it must keep it.
		if (gaol.IsHunting)
		{
			Enemy w = gaol.Warden;
			if (w == null || !IsInstanceValid(w) || w.IsDying)
				return player.GlobalPosition;
			float fromPost = w.GlobalPosition.DistanceTo(gaol.PostPosition);
			wardenFarthestFromPost = Mathf.Max(wardenFarthestFromPost, fromPost);
			wardenDiedFromPost = fromPost;
			Vector2 side = player.GlobalPosition - w.GlobalPosition;
			Vector2 dir = side.LengthSquared() > 1f ? side.Normalized() : Vector2.Up;
			Vector2 stand = w.GlobalPosition + dir * 110f;
			return game.IsGroundBlockedAt(stand + feetOffset) ? w.GlobalPosition + dir.Rotated(Mathf.Pi / 2f) * 110f : stand;
		}

		if (gaol.Layout == null)
			return player.GlobalPosition;
		if (slipStartedAt < 0f)
		{
			slipStartedAt = game.RunSeconds;
			GD.Print($"EXPLOREPROBE gaol: the Warden fell {wardenDiedFromPost:0} px from its post (it went {wardenFarthestFromPost:0} px from it at most); "
				+ $"the key lies at {gaol.Key.KeyAt}");
		}

		// --into-cone: the key in hand, into the nearest lit cone, until the light falls on him.
		if (intoCone && conePhase == 1 && gaol.Key.Carrying)
			return IntoTheLight(gaol, feet, feetOffset, delta);

		// A bearer off its round (turned on him, or walking back): out of its light, and wait for it to take its round up.
		// So too a key he has just lost and must step off before it can be taken up.
		if (!gaol.AllOnRounds || (!gaol.Key.Carrying && !gaol.Key.Armed))
		{
			slipPlan = null;
			slipRetreatFrames++;
			return Retreat(gaol, feet, feetOffset, delta);
		}

		if (gaol.Key.Carrying != slipWasCarrying)
		{
			slipWasCarrying = gaol.Key.Carrying;
			if (slipWasCarrying)
				GD.Print($"EXPLOREPROBE gaol: took the key up at {slipClock:0.0} s into plan {slipPlans} (planned {(slipPlan?.PickupTick ?? -1) * GaolSlip.Tick:0.0} s), "
					+ $"standing {player.GlobalPosition.DistanceTo(gaol.Key.KeyAt):0} px from where it lay");
		}
		// What the plan stood on is gone: plan again.
		if (slipPlan != null && gaol.Catches != slipCatchesAtPlan)
			DropPlan("the light caught him");
		else if (slipPlan != null && !slipCarryingAtPlan && gaol.Key.Carrying && slipClock < slipPlan.PickupTick * GaolSlip.Tick - 0.5f)
			DropPlan("the key was taken up earlier than planned");
		else if (slipPlan != null && slipClock > slipPlan.Ticks * GaolSlip.Tick + 1.5f)
			DropPlan($"the plan ran out ({(gaol.Key.Carrying ? "carrying" : "NOT carrying")}, {player.GlobalPosition.DistanceTo(gaol.CellPosition):0} px from the cell, "
				+ $"{player.GlobalPosition.DistanceTo(gaol.Key.KeyAt):0} px from the key)");
		if (slipPlan == null)
		{
			if (completeClock < slipRetryAt)
				return player.GlobalPosition;
			if (!PlanSlip(gaol, feet))
			{
				slipRetryAt = completeClock + 1f;
				return player.GlobalPosition;
			}
		}

		// IN STEP WITH THE LIGHT HE IS TESTED AGAINST. The event reads the cones the bearers aimed on the last frame and moves
		// them after (GaolersKeyEvent.Tick), so the bearers stand at the plan's slipClock now: he is put there, and the clock
		// moves on by this frame's delta afterwards. The first build added the delta first, standing him one frame ahead of
		// the light that catches - 0.17 s at 24 fps and timescale 4, far more on the frame after a plan's own hitch - and seed
		// 7 was caught once that way, under load.
		float before = slipPrevClock;
		slipPrevClock = slipClock;
		int[] path = slipPlan.Path;
		float u = slipClock / GaolSlip.Tick;
		int s = Math.Min((int)u, path.Length - 1);
		Vector2 goal = s >= path.Length - 1
			? slipWalk.Points[path[^1]]
			: slipWalk.Points[path[s]].Lerp(slipWalk.Points[path[s + 1]], u - s);
		// A long frame (timescale 4 under load is a fifth of a second, 40 px of walk) can step over the key without ever
		// standing within its 30 px: the frame that passes the planned pickup stands on the planned pickup point instead.
		// The clock is not held back, so the plan stays in step with the bearers.
		float pickupAt = slipPlan.PickupTick * GaolSlip.Tick;
		if (!slipCarryingAtPlan && !gaol.Key.Carrying && slipPlan.PickupTick > 0 && before < pickupAt && slipClock >= pickupAt)
			goal = slipWalk.Points[path[Math.Min(slipPlan.PickupTick, path.Length - 1)]];
		if (traceGaol && !gaol.Key.Carrying && slipClock > pickupAt - 0.4f && slipClock < pickupAt + 0.8f)
			GD.Print($"EXPLOREPROBE gaol trace: t {slipClock:0.00} (pickup {pickupAt:0.00}), body to the key {(goal - feetOffset).DistanceTo(gaol.Key.KeyAt):0.0} px, "
				+ $"he stands {player.GlobalPosition.DistanceTo(gaol.Key.KeyAt):0.0} px from it now, armed {gaol.Key.Armed}, frame {delta * 1000f:0} ms");
		// --into-cone: as soon as the key is in hand, the plan is dropped for the light.
		if (intoCone && conePhase == 0 && gaol.Key.Carrying)
		{
			conePhase = 1;
			slipPlan = null;
			GD.Print("EXPLOREPROBE gaol: the key is in hand - walking into a lantern's light on purpose (--into-cone)");
		}
		slipClock += delta;
		return goal - feetOffset;
	}

	private bool slipWasCarrying;
	private float slipPrevClock = -1f;
	private int slipSideWaits;
	private int slipPlansOffTheWay;

	private void DropPlan(string why)
	{
		GD.Print($"EXPLOREPROBE gaol: plan {slipPlans} dropped at {slipClock:0.0} s - {why}");
		slipPlan = null;
	}

	// A plan from where the probe stands: to the key (wherever the floor lets its feet reach it), and home to the cell's door,
	// through the dark. Built on a fresh walk graph each time (Attach adds the two off-graph points).
	private bool PlanSlip(GaolersKeyEvent gaol, Vector2 feet)
	{
		GaolLayout layout = gaol.Layout;
		GaolGround ground = layout.Ground;
		Vector2 feetOffset = player.FootPosition - player.GlobalPosition;
		var walk = GaolWalk.Build(ground, layout.RegionMin, layout.RegionMax);
		int start = walk.Attach(feet);
		if (start < 0)
		{
			// Somewhere the region's corridors cannot be walked to in a straight line: to the nearest of them first.
			int near = walk.Nearest(feet);
			GD.Print($"EXPLOREPROBE gaol: standing off the walk ({feet}); stepping to {walk.Points[near]} first");
			player.GlobalPosition = walk.Points[near] - feetOffset;
			return false;
		}
		int key = start;
		if (!gaol.Key.Carrying)
		{
			Vector2 keyFeet = gaol.Key.KeyAt + feetOffset;
			key = walk.Attach(keyFeet);
			for (int r = 6; key < 0 && r <= KeyCarry.PickupRadius; r += 6)
				for (int k = 0; k < 12 && key < 0; k++)
					key = walk.Attach(keyFeet + Vector2.FromAngle(k * Mathf.Tau / 12f) * r);
			if (key < 0)
			{
				GD.Print($"EXPLOREPROBE gaol: the key at {gaol.Key.KeyAt} cannot be walked to on the region's corridors");
				return false;
			}
		}
		int home = walk.PointOfCell(layout.Home);
		var sweeps = new List<GaolSweep>();
		var starts = new int[gaol.Bearers.Count];
		for (int i = 0; i < gaol.Bearers.Count; i++)
		{
			LanternBearer b = gaol.Bearers[i];
			var sweep = new GaolSweep(b.Round, walk, ground, GaolSlip.PlanningCone());
			sweeps.Add(sweep);
			starts[i] = sweep.IndexOfArc(b.Arc);
		}
		// THE WAY HE KNOWS: the shortest way home, and the side passages that open off it - each a dead end to stand in while
		// a light goes by - and nothing else. A player who watches the rounds walks the way he came and steps aside; he does
		// not go exploring the maze for a way round them. Only if that ground holds no way at all is the rest of the region
		// searched (and the log says so).
		// His way: from where he stands to where the key lies, and from there home - the corridors' shortest each.
		Vector2I meCell = ground.CellOf(feet), keyCell = ground.CellOf(walk.Points[key]);
		Vector2I[] toKey = ground.PathBetween(meCell, keyCell, layout.RegionMin, layout.RegionMax);
		Vector2I[] keyHome = ground.PathBetween(keyCell, layout.Home, layout.RegionMin, layout.RegionMax);
		var way = new List<Vector2I>();
		if (toKey != null)
			way.AddRange(toKey);
		way.AddRange(keyHome ?? layout.Route);
		bool[] known = layout.WayHeKnows(walk, way, out bool[] onRoute);
		int horizon = GaolSlip.StepsFor(GaolLayout.SlipHorizonSeconds);
		GaolSlip plan = GaolSlip.Search(walk, sweeps, starts, start, key, home, horizon,
			keepPath: true, pickupRadius: KeyCarry.PickupRadius, roam: true, allowed: known);
		slipPlans++;
		if (!plan.Found)
		{
			plan = GaolSlip.Search(walk, sweeps, starts, start, key, home, horizon,
				keepPath: true, pickupRadius: KeyCarry.PickupRadius, roam: true);
			if (plan.Found)
			{
				slipPlansOffTheWay++;
				GD.Print("EXPLOREPROBE gaol: no way along the way home and its side passages; going round by the rest of the maze");
			}
		}
		if (!plan.Found)
		{
			slipPlansFailed++;
			GD.Print($"EXPLOREPROBE gaol: no way through the dark from {feet} within {GaolLayout.SlipHorizonSeconds:0} s; waiting and looking again");
			return false;
		}
		int sideWaits = 0;
		foreach (int p in plan.WaitPoints)
			if (p < onRoute.Length && !onRoute[p])
				sideWaits++;
		slipSideWaits += sideWaits;
		slipPlan = plan;
		slipWalk = walk;
		slipClock = 0f;
		slipPrevClock = -1f;
		slipCatchesAtPlan = gaol.Catches;
		slipCarryingAtPlan = gaol.Key.Carrying;
		slipPlannedSeconds += plan.Seconds;
		slipHeldWaits += plan.HeldWaits;
		int free = 0;
		for (int t = 1; t <= plan.PickupTick && t < plan.Path.Length; t++)
			if (plan.Path[t] == plan.Path[t - 1])
				free++;
		slipFreeWaits += free;
		GD.Print($"EXPLOREPROBE gaol: plan {slipPlans}: {plan.Seconds:0.0} s - the key taken up at {plan.PickupTick * GaolSlip.Tick:0.0} s "
			+ $"after {free * GaolSlip.Tick:0.0} s waiting for the dark, then {plan.HeldWaits * GaolSlip.Tick:0.0} s standing in the dark with it "
			+ $"({sideWaits * GaolSlip.Tick:0.0} s of it in a side passage); "
			+ $"bearers at [{string.Join(", ", starts)}]{(gaol.Key.Carrying ? " (already carrying)" : "")}; from {feet} "
			+ $"(key point {walk.Points[key]}, {walk.Points[plan.Path[Math.Min(plan.PickupTick, plan.Path.Length - 1)]]} at the pickup); "
			+ $"{Engine.GetFramesPerSecond():0} fps at timescale {Engine.TimeScale:0.#}");
		return true;
	}

	// Out of the light of every bearer that is off its round, and out of its sight - out of its lantern's reach, or with a
	// wall between, from where it stands and from where it last saw him (where it will go to look): along the corridors
	// (the region's walk graph), to the nearest such point. What a player does who has learned it does not see round
	// corners: steps into the dark and waits.
	private Vector2 Retreat(GaolersKeyEvent gaol, Vector2 feet, Vector2 feetOffset, float delta)
	{
		GaolLayout layout = gaol.Layout;
		GaolWalk walk = slipWalk ?? GaolWalk.Build(layout.Ground, layout.RegionMin, layout.RegionMax);
		slipWalk = walk;
		int from = walk.Nearest(feet);
		var hunters = gaol.Bearers.Where(b => b.State != LanternBearer.Duty.Round).ToList();
		Vector2 keyFeet = gaol.Key.KeyAt + feetOffset;
		bool InSight(Vector2 a, Vector2 p)
		{
			if (a.DistanceTo(p) > LanternCone.DefaultReach + 60f)
				return false;
			int n = Math.Max(1, Mathf.CeilToInt(a.DistanceTo(p) / 12f));
			for (int k = 1; k < n; k++)
				if (layout.Ground.Wall(a.Lerp(p, k / (float)n)))
					return false;
			return true;
		}
		bool Safe(Vector2 p) =>
			hunters.All(b => !(b.ConeLive && b.Cone.Covers(p)) && !InSight(b.Feet, p)
				&& (b.State != LanternBearer.Duty.Turned || !InSight(b.LastSeen, p)))
			&& (gaol.Key.Carrying || gaol.Key.Armed || p.DistanceTo(keyFeet) > KeyCarry.RearmRadius + 30f);
		if (Safe(feet))
			return player.GlobalPosition;
		// The way there keeps out of reach of every hunter - never past it to a dark spot behind it. It may cross the light:
		// he is in it already, and without the key the light costs nothing. (Barring the light too kept the first build's
		// search from ever leaving where he stood, and it fled straight down the lit corridor.)
		bool Passable(Vector2 p) => hunters.All(b => b.Feet.DistanceTo(p) > LanternBearer.BlowReach * 1.25f);
		var came = new Dictionary<int, int> { [from] = from };
		var queue = new Queue<int>();
		queue.Enqueue(from);
		int goal = -1;
		while (queue.Count > 0 && goal < 0)
		{
			int p = queue.Dequeue();
			if (Safe(walk.Points[p]))
			{
				goal = p;
				break;
			}
			foreach (int q in walk.NeighboursOf(p))
				if (!came.ContainsKey(q) && Passable(walk.Points[q]))
				{
					came[q] = p;
					queue.Enqueue(q);
				}
		}
		if (goal < 0)
		{
			// Nowhere hidden without passing it: straight away from the nearest hunter along the corridors.
			LanternBearer near = hunters.OrderBy(b => b.Feet.DistanceTo(feet)).FirstOrDefault();
			if (near == null)
				return player.GlobalPosition;
			int best = from;
			foreach (int q in walk.NeighboursOf(from))
				if (walk.Points[q].DistanceTo(near.Feet) > walk.Points[best].DistanceTo(near.Feet))
					best = q;
			goal = best;
			came[best] = from;
		}
		if (traceGaol && (slipRetreatFrames % 15) == 1)
		{
			LanternBearer h = hunters.FirstOrDefault();
			GD.Print($"EXPLOREPROBE gaol trace: retreat at {feet}, goal {walk.Points[goal]} ({walk.Points[goal].DistanceTo(feet):0} px off); "
				+ (h == null ? "no hunter" : $"hunter {h.State} at {h.Feet}, {h.Feet.DistanceTo(feet):0} px, sees him {h.SeesHim}, lost {h.LostClock:0.0} s, "
					+ $"turned {h.TurnedClock:0.0} s, last saw him at {h.LastSeen}"));
		}
		// The first step of the way there.
		int next = goal;
		while (came[next] != from && came[next] != next)
			next = came[next];
		Vector2 to = (feet.DistanceTo(walk.Points[from]) > GaolWalk.Step ? walk.Points[from] : walk.Points[next]) - feet;
		float step = player.WalkingSpeed * delta;
		return feet + (to.Length() <= step ? to : to.Normalized() * step) - feetOffset;
	}

	// --into-cone: to a point well inside the nearest lit cone, at his own walk, until its light is on him.
	private Vector2 IntoTheLight(GaolersKeyEvent gaol, Vector2 feet, Vector2 feetOffset, float delta)
	{
		LanternBearer target = null;
		float best = float.MaxValue;
		foreach (LanternBearer b in gaol.Bearers)
		{
			if (!b.ConeLive || b.State != LanternBearer.Duty.Round)
				continue;
			float d = b.Feet.DistanceTo(feet);
			if (d < best)
			{
				best = d;
				target = b;
			}
		}
		if (target == null)
			return player.GlobalPosition;
		Vector2 inside = target.Feet + target.Facing * Mathf.Min(140f, Mathf.Max(60f, target.Cone.AxisReach * 0.5f));
		Vector2 to = inside - feet;
		float step = player.WalkingSpeed * delta;
		return feet + (to.Length() <= step ? to : to.Normalized() * step) - feetOffset;
	}

	// What the light did, every frame: the catch (where the key fell, which bearer turned and which way it faced), the
	// turned bearer coming for him, and whether it went back to its round.
	private void WatchTheCatch(GaolersKeyEvent gaol, float delta)
	{
		if (gaol.Catches > gaolCatchesSeen)
		{
			gaolCatchesSeen = gaol.Catches;
			coneBodyAtCatch = player.GlobalPosition;
			coneKeyAt = gaol.Key.KeyAt;
			coneBearer = -1;
			for (int i = 0; i < gaol.Bearers.Count; i++)
				if (gaol.Bearers[i].State == LanternBearer.Duty.Turned)
					coneBearer = i;
			GD.Print($"EXPLOREPROBE gaol: CAUGHT (catch {gaol.Catches}) by bearer {coneBearer + 1}: he stood at {coneBodyAtCatch}, the key fell at {coneKeyAt}");
			if (conePhase == 1)
			{
				conePhase = 2;
				coneWatch = 0f;
				coneClosest = float.MaxValue;
				coneReturnsBefore = coneBearer >= 0 ? gaol.Bearers[coneBearer].Returns : 0;
				coneDistanceAtCatch = coneBearer >= 0 ? gaol.Bearers[coneBearer].Feet.DistanceTo(player.FootPosition) : -1f;
			}
		}
		if (conePhase != 2 || coneBearer < 0)
			return;
		LanternBearer b = gaol.Bearers[coneBearer];
		coneWatch += delta;
		if (b.State == LanternBearer.Duty.Turned)
		{
			coneTurned = true;
			Vector2 toHim = player.FootPosition - b.Feet;
			if (coneFacingDot < -1.5f && toHim.LengthSquared() > 1f)
				coneFacingDot = b.Facing.Dot(toHim.Normalized());
			coneClosest = Mathf.Min(coneClosest, toHim.Length());
			coneBlows = b.Blows;
			coneFollowed = b.FollowedFurthest;
		}
		if (b.State == LanternBearer.Duty.Returning && coneGaveUpAt < 0f)
		{
			coneGaveUpAt = coneWatch;
			coneLostHim = b.LastLostHim;
		}
		if (b.Returns > coneReturnsBefore && b.State == LanternBearer.Duty.Round)
		{
			coneReturnedToRound = true;
			conePhase = 3;
			GD.Print($"EXPLOREPROBE gaol: bearer {coneBearer + 1} gave up {coneGaveUpAt:0.0} s after the catch "
				+ $"({(coneLostHim ? "he was out of its light" : "its time ran out")}) and took its round up again {coneWatch:0.0} s after it "
				+ $"(it came to {coneClosest:0} px of him from {coneDistanceAtCatch:0}, {coneBlows} blow(s))");
		}
	}

	private void GaolResults(GaolersKeyEvent gaol)
	{
		GaolLayout layout = gaol.Layout;
		Check("the gaol was laid: the Warden's post four to six cells from the cell, rounds across the way",
			layout != null && layout.Rounds.Count >= 1 && layout.Route != null
			&& layout.Route.Length - 1 >= GaolLayout.PostMinSteps && layout.Route.Length - 1 <= GaolLayout.PostMaxSteps,
			layout == null ? "no layout" : $"post {layout.Route?.Length - 1} steps out, {layout.Rounds.Count} round(s) of "
				+ $"[{string.Join(", ", layout.Rounds.Select(r => r.Cells.Length))}] cells, a straight walk caught {layout.CaughtShare * 100f:0}% of the time"
				+ (layout.Fallback ? " (the best of its rolls)" : ""));
		Check("the Warden kept its post: it fell near it, so the key lay on the far side of the rounds",
			wardenDiedFromPost >= 0f && wardenDiedFromPost <= GaolersKeyEvent.LeashRadius + 120f,
			$"{wardenDiedFromPost:0} px from its post when it fell, {wardenFarthestFromPost:0} at most");
		Check("the lantern-bearers were never targets: in no group the spells read, and never hurt", !bearerEverTargeted
			&& gaol.Bearers.All(b => !b.IsInGroup("enemies")), $"{gaol.Bearers.Count} bearer(s)");
		float slip = slipStartedAt >= 0f && slipHomeAt >= 0f ? slipHomeAt - slipStartedAt : -1f;
		if (!intoCone)
		{
			Check("watching the rounds, he slipped the key home without the light ever falling on him", gaol.Catches == 0 && gaol.IsWon,
				$"{gaol.Catches} catch(es); {slipPlans} plan(s) ({slipPlansFailed} found no way at first, {slipPlansOffTheWay} had to leave the way home), "
				+ $"{slipFreeWaits * GaolSlip.Tick:0.0} s waiting beside the key for the dark and {slipHeldWaits * GaolSlip.Tick:0.0} s standing in the "
				+ $"dark with it before the door ({slipSideWaits * GaolSlip.Tick:0.0} s in a side passage); {slipWalked:0} px walked "
				+ $"in {slip:0.0} s from the Warden's fall to the lock");
		}
		else
		{
			Check("into the light with the key: it fell where he stood", conePhase >= 2 && coneKeyAt.DistanceTo(coneBodyAtCatch) <= 40f,
				$"he stood at {coneBodyAtCatch}, the key fell at {coneKeyAt}");
			Check("THAT bearer turned toward him", coneTurned && coneFacingDot >= 0.9f,
				$"bearer {coneBearer + 1}, its lantern {Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(coneFacingDot, -1f, 1f))):0} degrees off him");
			Check("it came for him: it left its round and followed him", coneTurned && (coneFollowed >= 80f || coneBlows > 0),
				$"it went {coneFollowed:0} px from where it turned; {coneDistanceAtCatch:0} px from him at the catch, {coneClosest:0} at the closest, "
				+ $"{coneBlows} blow(s) landed");
			// Either way ends it - he out of its light 2 s, or its ten seconds up - and the line says which. The probe's retreat
			// is a heuristic: in a long straight corridor with no side passage near the catch it can take the whole ten.
			Check("it gave up, and took its round up again", coneReturnedToRound && coneGaveUpAt >= 0f
				&& coneGaveUpAt <= LanternBearer.TurnedMaxSeconds + 0.5f,
				$"gave up {coneGaveUpAt:0.0} s after the catch ({(coneLostHim ? "he was out of its light" : "its ten seconds ran out")}), back on "
				+ $"its round {coneWatch:0.0} s after; {slipRetreatFrames} frame(s) spent getting out of its light");
			Check("the key was taken back and slipped home after", gaol.IsWon && gaol.Catches == 1,
				$"{gaol.Catches} catch(es), {slipPlans} plan(s), {slip:0.0} s from the Warden's fall to the lock");
		}
	}
}
