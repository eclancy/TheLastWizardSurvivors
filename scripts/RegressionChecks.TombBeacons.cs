using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// The dry tomb (.ai/chapter-wizards.md 4.6) and, since 2026-11-08, its mirrors (.ai/unlock-puzzles.md 3.1, "route the
// sun"): what holds the turning rule, the light and every layout the seed can roll to their sentences, which no build sees
// and a single run shows once. Every line is a warning, as the rest of RunAll's are. The name keeps the beacons', as the
// site id does.
public static partial class RegressionChecks
{
	// Layouts rolled on paper at every launch: on open ground, round a lake, and across a ridge of rock.
	private const int TombOpenSeeds = 40;
	private const int TombLakeSeeds = 24;
	private const int TombRidgeSeeds = 16;

	private static void ValidateTombBeacons(List<string> warnings)
	{
		// It is the Sands' SECOND event, after the caravan (P2's API): the chapter's original event keeps stream 1.
		IReadOnlyList<SideEventEntry> sands = SideEventDirector.TableEventsFor("stage_6");
		if (sands.Count < 2 || sands[0].SiteId != "caravan_master" || sands[1].SiteId != "tomb_beacons")
			warnings.Add($"Tomb mirrors: the Scorched Sands' line must be the caravan, then the tomb ({string.Join(", ", sands.Select(e => e.SiteId))}).");

		// The hint's cue is the sand, on the sound's knocks (sfx.py _TOMB_KNOCKS: 0.12, 0.98, 1.96).
		float[] knocks = StageHintCue.TombKnockTimes;
		float[] want = { 0.12f, 0.98f, 1.96f };
		if (knocks.Length != want.Length || knocks.Where((k, i) => MathF.Abs(k - want[i]) > 0.001f).Any())
			warnings.Add($"Tomb mirrors: the hint's sand lifts on [{string.Join(", ", knocks)}], not hint_tomb_knock's knocks (0.12, 0.98, 1.96).");
		if (StageHintCatalog.ForSite("tomb_beacons") is not StageHint hint || hint.Cue != HintCue.Sand || hint.Sound != SfxCatalog.HintTombKnock)
			warnings.Add("Tomb mirrors: its hint must be hint_tomb_knock with the sand cue.");

		// The art, at the sizes the event reads it at (tools/art/tomb_beacons.py).
		CheckTombSheet(warnings, "res://assets/bonelight/world/tomb/tomb.png", new Vector2(88 * 4, 72), "the tomb's four 88x72 frames");
		CheckTombSheet(warnings, "res://assets/bonelight/world/tomb/mirror.png", new Vector2(32 * SunMirrors.Facings, 48),
			"the mirror's eight 32x48 facings");
		CheckTombSheet(warnings, "res://assets/bonelight/world/tomb/socket.png", new Vector2(30 * 2, 14), "the sun-disc's two 30x14 frames");
		// The sounds: allowlisted (SfxCatalog's own check finds the files).
		foreach (string name in new[] { SfxCatalog.MirrorTurn, SfxCatalog.SunArrive })
			if (!SfxCatalog.AllNames.Contains(name))
				warnings.Add($"Tomb mirrors: '{name}' is not in SfxCatalog's allowlist, so it is never preloaded.");

		// The numbers the rule leans on.
		if (SunMirrors.MinLeg < 300f || SunMirrors.MaxLeg <= SunMirrors.MinLeg)
			warnings.Add($"Tomb mirrors: a leg must be at least 300 px (3.1); it is {SunMirrors.MinLeg}-{SunMirrors.MaxLeg}.");
		if (SunMirrors.MinSpacing - SunMirrors.ApproachDistance <= SunMirrors.Reach)
			warnings.Add($"Tomb mirrors: walking in to one mirror ({SunMirrors.ApproachDistance} px out) comes within {SunMirrors.Reach} px of the next "
				+ $"({SunMirrors.MinSpacing} px apart), so turning one turns another.");
		if (SunMirrors.ApproachDistance <= SunMirrors.Reach || SunMirrors.StandDistance >= SunMirrors.Reach
			|| SunMirrors.StandDistance <= SunMirrors.MinTurnDistance)
			warnings.Add("Tomb mirrors: a mirror is turned from inside its reach and walked in to from outside it.");
		if (SunMirrors.BeamReach < SunMirrors.MaxLeg + SunMirrors.MirrorCatch)
			warnings.Add($"Tomb mirrors: the light runs out ({SunMirrors.BeamReach} px) short of the longest leg ({SunMirrors.MaxLeg} px).");
		if (SunMirrors.MirrorCatch * 2f >= SunMirrors.LegClearance)
			warnings.Add("Tomb mirrors: a mirror catches light so wide a leg kept clear of it could still graze it.");
		if (TombBeaconsEvent.MirrorArrival <= SunMirrors.Reach)
			warnings.Add("Tomb mirrors: he must arrive before his feet can turn a mirror, or one turned before the event begins would not count.");
		if (TombBeaconsEvent.BurnDamage <= 0 || TombBeaconsEvent.BurnSeconds <= 0f || TombBeaconsEvent.BurnReach <= 0f)
			warnings.Add("Tomb mirrors: the light must burn the bodies it passes through (3.1, decided).");
		if (SunMirrors.TombBody.End.Y > -1f)
			warnings.Add("Tomb mirrors: the tomb's body must stand behind its foot, so light from the front reaches the disc first.");

		ValidateMirrorFacing(warnings);
		ValidateMirrorLight(warnings);
		ValidateMirrorLayouts(warnings);
	}

	// THE ONE ACTION on paper: behind a mirror it faces away; out of reach or on top of it, it holds; it holds past the line
	// to the next facing by the hysteresis, and no further.
	private static void ValidateMirrorFacing(List<string> warnings)
	{
		Vector2 m = new(1000f, -400f);
		int wrong = 0;
		for (int f = 0; f < SunMirrors.Facings; f++)
			for (int was = -1; was < SunMirrors.Facings; was++)
				if (SunMirrors.FacingAway(m, SunMirrors.StandPoint(m, f), was) != f)
					wrong++;
		if (wrong > 0)
			warnings.Add($"Tomb mirrors: standing behind a mirror turned it some other way {wrong} time(s) in 72.");
		if (SunMirrors.FacingAway(m, m - new Vector2(SunMirrors.Reach + 4f, 0f), 3) != 3)
			warnings.Add("Tomb mirrors: a mirror turned for feet outside its reach.");
		if (SunMirrors.FacingAway(m, m - new Vector2(SunMirrors.MinTurnDistance * 0.5f, 0f), 3) != 3)
			warnings.Add("Tomb mirrors: a mirror turned for feet on its own stand.");
		float held = Mathf.DegToRad(22.5f + SunMirrors.TurnHysteresisDegrees - 1.5f);
		float past = Mathf.DegToRad(22.5f + SunMirrors.TurnHysteresisDegrees + 1.5f);
		Vector2 FeetAt(float angle) => m - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 100f;
		for (int f = 0; f < SunMirrors.Facings; f++)
		{
			float centre = f * Mathf.Pi / 4f;
			if (SunMirrors.FacingAway(m, FeetAt(centre + held), f) != f || SunMirrors.FacingAway(m, FeetAt(centre - held), f) != f)
				warnings.Add($"Tomb mirrors: facing {f} gave way inside its hysteresis.");
			if (SunMirrors.FacingAway(m, FeetAt(centre + past), f) != SunMirrors.Wrap(f + 1)
				|| SunMirrors.FacingAway(m, FeetAt(centre - past), f) != SunMirrors.Wrap(f - 1))
				warnings.Add($"Tomb mirrors: facing {f} held past its hysteresis, where the next should take it.");
		}
	}

	// THE LIGHT on paper: the disc takes light only from the front; the tomb's body stops it from the back and the sides;
	// a mirror in the way catches it; terrain stops it where it starts.
	private static void ValidateMirrorLight(List<string> warnings)
	{
		Vector2 tomb = Vector2.Zero;
		SunMirrors.Hit Shoot(Vector2 from, int facing, Func<Vector2, bool> blocked = null, Vector2? other = null)
		{
			var mirrors = other is Vector2 o ? new[] { from, o } : new[] { from };
			return SunMirrors.Cast(mirrors, tomb, 0, facing, blocked);
		}
		if (Shoot(new Vector2(0f, 400f), 6).Kind != SunMirrors.End.Socket)
			warnings.Add("Tomb mirrors: light straight up at the tomb's front does not land on its disc.");
		if (Shoot(new Vector2(-300f, 300f), 7).Kind != SunMirrors.End.Socket || Shoot(new Vector2(300f, 300f), 5).Kind != SunMirrors.End.Socket)
			warnings.Add("Tomb mirrors: light coming in at the tomb's front on a diagonal does not land on its disc.");
		if (Shoot(new Vector2(0f, -500f), 2).Kind != SunMirrors.End.Blocked)
			warnings.Add("Tomb mirrors: light from behind the tomb reached past its body.");
		if (Shoot(new Vector2(-400f, -40f), 0).Kind != SunMirrors.End.Blocked)
			warnings.Add("Tomb mirrors: light along the tomb's side did not stop at its body.");
		if (Shoot(new Vector2(-400f, 0f), 0).Kind == SunMirrors.End.Socket)
			warnings.Add("Tomb mirrors: light along the tomb's foot from the side landed on the disc, which faces south.");
		SunMirrors.Hit caught = Shoot(new Vector2(0f, 900f), 6, other: new Vector2(10f, 500f));
		if (caught.Kind != SunMirrors.End.Mirror || caught.Mirror != 1)
			warnings.Add("Tomb mirrors: a mirror standing in the light's way did not catch it.");
		SunMirrors.Hit walled = Shoot(new Vector2(0f, 900f), 6, p => p.Y < 700f && p.Y > 660f);
		if (walled.Kind != SunMirrors.End.Blocked || walled.Length < 190f || walled.Length > 210f)
			warnings.Add($"Tomb mirrors: a ridge 200 px ahead stopped the light at {walled.Length:0} px ({walled.Kind}).");
		if (Shoot(new Vector2(3000f, 0f), 0).Kind != SunMirrors.End.Open || Mathf.Abs(Shoot(new Vector2(3000f, 0f), 0).Length - SunMirrors.BeamReach) > 0.5f)
			warnings.Add("Tomb mirrors: light with nothing in its way did not run its whole reach.");
	}

	// THE SEED on paper (3.1, and the proof unlock-puzzles.md section 6 asks for): every layout rolled round a tomb - on
	// open ground, round a lake, across a ridge - is found, keeps its shape, and is solvable with exactly its path's
	// mirrors and no fewer; its start and every single wrong facing of a path mirror leave the disc dark; and a player who
	// walks in behind each path mirror in turn, as the probe does, brings the light to the disc.
	private static void ValidateMirrorLayouts(List<string> warnings)
	{
		int rolled = 0, missing = 0, unproved = 0, misshapen = 0, wrongStill = 0, walkFailed = 0, nondeterministic = 0;
		int threeLegs = 0, fourLegs = 0, relaxed = 0, maxAttempts = 0;
		var bySize = new SortedDictionary<int, int>();
		string firstFault = null;
		var chain = new int[SunMirrors.MaxMirrors];
		ulong started = Time.GetTicksMsec();

		void Roll(string what, ulong seed, Vector2 tomb, Func<Vector2, bool> blocked)
		{
			rolled++;
			var ground = new SunMirrors.Ground
			{
				Blocked = blocked,
				// The explore test's shape: the centre and two rings clear of what blocks.
				Clear = blocked == null ? null : (p, r) =>
				{
					if (blocked(p))
						return false;
					for (int k = 0; k < 8; k++)
					{
						Vector2 dir = Vector2.FromAngle(k * Mathf.Tau / 8f);
						if (blocked(p + dir * r) || blocked(p + dir * r * 0.5f))
							return false;
					}
					return true;
				},
			};
			SunMirrors layout = SunMirrors.Generate(tomb, seed, ground);
			if (layout == null)
			{
				missing++;
				firstFault ??= $"{what} seed {seed}: no layout";
				return;
			}
			maxAttempts = Math.Max(maxAttempts, layout.Attempts);
			if (layout.Relaxed) relaxed++;
			if (layout.Legs == 3) threeLegs++; else fourLegs++;
			bySize[layout.MirrorCount] = bySize.TryGetValue(layout.MirrorCount, out int c) ? c + 1 : 1;
			SunMirrors again = SunMirrors.Generate(tomb, seed, ground);
			if (again == null || again.MirrorCount != layout.MirrorCount
				|| Enumerable.Range(0, layout.MirrorCount).Any(i => again.Mirrors[i] != layout.Mirrors[i] || again.Start[i] != layout.Start[i]))
				nondeterministic++;

			// Its shape: three or four legs of 300 px or more, the source and four or five more, spaced, uncrossed, on open
			// ground, the last coming in from the front.
			string shape = TombLayoutShape(layout, blocked);
			if (shape != null)
			{
				misshapen++;
				firstFault ??= $"{what} seed {seed}: {shape}";
			}
			if (!layout.Proves(out string why))
			{
				unproved++;
				firstFault ??= $"{what} seed {seed}: {why}";
			}
			// The wrong facing, said on its own: the solution with any one path mirror turned any other way leaves it dark.
			int[] facings = layout.SolutionFacings();
			foreach (int m in layout.Path)
			{
				int right = facings[m];
				for (int f = 0; f < SunMirrors.Facings; f++)
				{
					if (f == right) continue;
					facings[m] = f;
					if (layout.Trace(facings, chain, out _))
						wrongStill++;
				}
				facings[m] = right;
			}
			// The player's walk: in behind each path mirror in turn from just outside its reach, out again the same way.
			string walk = WalkTheMirrors(layout);
			if (walk != null)
			{
				walkFailed++;
				firstFault ??= $"{what} seed {seed}: {walk}";
			}
		}

		for (int s = 1; s <= TombOpenSeeds; s++)
			Roll("open ground", (ulong)(s * 7919), new Vector2(s * 3011 % 4000, -(s * 1777 % 3000)), null);
		for (int s = 1; s <= TombLakeSeeds; s++)
		{
			// A lake 180-300 px across, 420-700 px from the tomb on a bearing the seed picks.
			Vector2 tomb = new(-2000f + s * 97f, 1500f - s * 61f);
			float bearing = s * 2.399f;
			Vector2 lake = tomb + Vector2.FromAngle(bearing) * (420f + (s * 53 % 280));
			float radius = 180f + (s * 37 % 120);
			Roll("round a lake", (ulong)(s * 104729 + 3), tomb, p => p.DistanceSquaredTo(lake) < radius * radius);
		}
		for (int s = 1; s <= TombRidgeSeeds; s++)
		{
			// A ridge of rock 60 px thick and 1,400 px long, 500 px off the tomb across a bearing the seed picks.
			Vector2 tomb = new(4000f + s * 131f, 800f + s * 17f);
			Vector2 normal = Vector2.FromAngle(s * 1.618f);
			Vector2 along = normal.Orthogonal();
			Vector2 mid = tomb + normal * 500f;
			Roll("across a ridge", (ulong)(s * 15485863 + 11), tomb, p =>
			{
				Vector2 rel = p - mid;
				return Mathf.Abs(rel.Dot(normal)) < 30f && Mathf.Abs(rel.Dot(along)) < 700f;
			});
		}

		if (missing > 0)
			warnings.Add($"Tomb mirrors: {missing} of {rolled} tombs found no layout of mirrors (first: {firstFault}).");
		if (misshapen > 0)
			warnings.Add($"Tomb mirrors: {misshapen} of {rolled} layouts broke the seed's shape (first: {firstFault}).");
		if (unproved > 0)
			warnings.Add($"Tomb mirrors: {unproved} of {rolled} layouts were kept without their proof (first: {firstFault}).");
		if (wrongStill > 0)
			warnings.Add($"Tomb mirrors: {wrongStill} single wrong facing(s) of a path mirror still brought the light to the disc.");
		if (walkFailed > 0)
			warnings.Add($"Tomb mirrors: {walkFailed} of {rolled} layouts were not solved by walking in behind each path mirror (first: {firstFault}).");
		if (nondeterministic > 0)
			warnings.Add($"Tomb mirrors: {nondeterministic} of {rolled} layouts came out differently from the same seed.");
		if (relaxed > 0)
			warnings.Add($"Tomb mirrors: {relaxed} of {rolled} layouts were laid relaxed; Generate only relaxes when asked.");
		if (rolled - missing > 8 && (threeLegs == 0 || fourLegs == 0))
			warnings.Add($"Tomb mirrors: the seed rolled only {(threeLegs == 0 ? "four" : "three")}-leg paths ({threeLegs} three, {fourLegs} four); 3.1 asks for both.");
		if (rolled - missing > 8 && !(bySize.ContainsKey(5) && bySize.ContainsKey(6)))
			warnings.Add($"Tomb mirrors: the layouts held {string.Join(", ", bySize.Select(p => $"{p.Value} of {p.Key}"))} mirrors; the build chose five and six.");
		if (maxAttempts > SunMirrors.MaxAttempts * 3 / 4)
			warnings.Add($"Tomb mirrors: a tomb's layout was kept only on roll {maxAttempts} of {SunMirrors.MaxAttempts}; the seed's rules are close to refusing a tomb outright.");
		ulong took = Time.GetTicksMsec() - started;
		if (took > 6000)
			warnings.Add($"Tomb mirrors: rolling {rolled} layouts twice took {took} ms; a run rolls one at its start, and should not wait for it.");
		GD.Print($"RegressionChecks: the dry tomb's mirrors on paper over {rolled} tombs ({TombOpenSeeds} open, {TombLakeSeeds} by a lake, "
			+ $"{TombRidgeSeeds} by a ridge) - {rolled - missing} laid ({threeLegs} of three legs, {fourLegs} of four; "
			+ $"{string.Join(", ", bySize.Select(p => $"{p.Value} of {p.Key} mirrors"))}), the slowest on roll {maxAttempts}; "
			+ $"{unproved} unproved, {wrongStill} wrong facings reaching the disc, {walkFailed} not solved by walking in behind each; {took} ms.");
	}

	// Its shape, or why not.
	private static string TombLayoutShape(SunMirrors layout, Func<Vector2, bool> blocked)
	{
		if (layout.Legs < SunMirrors.MinLegs || layout.Legs > SunMirrors.MaxLegs)
			return $"{layout.Legs} legs";
		int more = layout.MirrorCount - 1;
		if (more < 4 || more > 5)
			return $"the source and {more} more (the build chose four or five more)";
		if (layout.DecoyCount < SunMirrors.MinDecoys || layout.DecoyCount > SunMirrors.MaxDecoys)
			return $"{layout.DecoyCount} decoys";
		var ends = new Vector2[layout.Legs + 1];
		for (int k = 0; k < layout.Legs; k++)
			ends[k] = layout.Mirrors[layout.Path[k]];
		ends[layout.Legs] = layout.Tomb;
		for (int k = 0; k < layout.Legs; k++)
		{
			float len = ends[k].DistanceTo(ends[k + 1]);
			if (len < SunMirrors.MinLeg - 1f || len > SunMirrors.MaxLeg + 1f)
				return $"leg {k} is {len:0} px";
			if (blocked != null)
				for (float s = 0f; s <= len; s += 8f)
					if (blocked(ends[k] + (ends[k + 1] - ends[k]) * (s / len)))
						return $"leg {k} crosses ground that stops the light";
			for (int j = k + 2; j < layout.Legs; j++)
				if (SunMirrors.SegmentGap(ends[k], ends[k + 1], ends[j], ends[j + 1]) < SunMirrors.LegClearance - 0.5f)
					return $"legs {k} and {j} cross or crowd each other";
		}
		int last = layout.Path[layout.Legs - 1];
		if (layout.Solution[last] != 6 && layout.Solution[last] != 5 && layout.Solution[last] != 7)
			return $"the last leg comes in facing {layout.Solution[last]}, not from the front";
		for (int i = 0; i < layout.MirrorCount; i++)
		{
			if (layout.Mirrors[i].DistanceTo(layout.Tomb) < SunMirrors.TombKeepOff - 0.5f)
				return $"mirror {i} stands {layout.Mirrors[i].DistanceTo(layout.Tomb):0} px from the tomb";
			for (int j = i + 1; j < layout.MirrorCount; j++)
				if (layout.Mirrors[i].DistanceTo(layout.Mirrors[j]) < SunMirrors.MinSpacing - 0.5f)
					return $"mirrors {i} and {j} stand {layout.Mirrors[i].DistanceTo(layout.Mirrors[j]):0} px apart";
		}
		return null;
	}

	// A player who knows the answer, under the real rule: for each path mirror in turn, from just outside its reach on the
	// line behind it, in to where he stands to turn it and out the same way, 6 px a step, every mirror answering his feet
	// each step. From the start facings that must end with the light on the disc, and turn nothing it did not mean to.
	private static string WalkTheMirrors(SunMirrors layout)
	{
		var facings = layout.Start.ToArray();
		var chain = new int[layout.MirrorCount];
		foreach (int m in layout.Path)
		{
			int want = layout.Solution[m];
			Vector2 approach = SunMirrors.StandPoint(layout.Mirrors[m], want, SunMirrors.ApproachDistance);
			Vector2 stand = SunMirrors.StandPoint(layout.Mirrors[m], want);
			for (int i = 0; i < layout.MirrorCount; i++)
				if (i != m && (layout.Mirrors[i].DistanceTo(approach) <= SunMirrors.Reach || layout.Mirrors[i].DistanceTo(stand) <= SunMirrors.Reach))
					return $"turning mirror {m} he stands within reach of mirror {i}";
			foreach ((Vector2 from, Vector2 to) in new[] { (approach, stand), (stand, approach) })
			{
				float len = from.DistanceTo(to);
				for (float s = 0f; s <= len; s += 6f)
				{
					Vector2 feet = from + (to - from) * (s / len);
					for (int i = 0; i < layout.MirrorCount; i++)
						facings[i] = SunMirrors.FacingAway(layout.Mirrors[i], feet, facings[i]);
				}
			}
			if (facings[m] != want)
				return $"walked in behind mirror {m}, it faces {facings[m]}, not {want}";
		}
		return layout.Trace(facings, chain, out int lit) ? null : $"every path mirror turned, the light goes through {lit} and not to the disc";
	}

	private static void CheckTombSheet(List<string> warnings, string path, Vector2 size, string what)
	{
		Texture2D texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		if (texture == null)
			warnings.Add($"Tomb mirrors: {path} ({what}) is missing or not imported.");
		else if (texture.GetSize() != size)
			warnings.Add($"Tomb mirrors: {path} is {texture.GetSize()}, expected {size} ({what}).");
	}
}
