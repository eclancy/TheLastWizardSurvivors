using System;
using System.Collections.Generic;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// Places a chapter's side event and its wayfinder out in the world at stage start, as a pure
/// function of the run's terrain seed (.ai/side-events.md, "Where it is").
/// </summary>
/// <remarks>
/// THE STAGE-SECRETS RULES, APPLIED TO EVENTS (.ai/stage-secrets.md section 4). Where a site is
/// depends on (terrain seed, stage, what the ground is) and nothing that happens in the run:
/// <list type="bullet">
/// <item>Every roll is drawn BEFORE its position is tested, and the fallback search is a fixed
/// pattern, so a rejected spot never shifts what comes after it.</item>
/// <item>A site has a footprint, not a point: its centre and a ring round it must all be open ground
/// (no water, no wall, no hazard, no fence, no landmark) - <see cref="Ground.IsClear"/>, which
/// Node2DGame answers from the streamer's chunk data. Asking about far ground generates that
/// chunk's DATA (not its paint), the same data the chunk will be painted from when the player gets
/// there, so a site can never be put on a lake that has not been drawn yet.</item>
/// <item>On the maze a candidate snaps to a corridor cell's centre: every corridor block is floor,
/// and the endless maze is connected (.ai/terrain.md section 7), so any cell can be walked to.</item>
/// </list>
/// Because the terrain is itself a pure function of the seed, so is every site: a chunk forgotten
/// and repainted when the player walks back is furnished round the same site, and a probe with
/// --terrain-seed=N gets the same map every time.
/// </remarks>
public sealed class ExplorePlacer
{
	/// <summary>What the placer needs to know about the ground. Supplied by Node2DGame.</summary>
	public sealed class Ground
	{
		/// <summary>True if a footprint of this radius round this point is open ground the player can reach.</summary>
		public Func<Vector2, float, bool> IsClear { get; init; }
		/// <summary>IsClear, and every point of the disc open too - for a site that draws over its whole
		/// footprint (the thaw's lake). Null falls back to IsClear.</summary>
		public Func<Vector2, float, bool> IsClearDense { get; init; }
		/// <summary>True if a body can stand on this one point (no water, wall or fence): for a site that walks a route
		/// over its ground on paper (the gibbet's rot pools, RotPoolLayout). Null: everywhere is.</summary>
		public Func<Vector2, bool> IsWalkable { get; init; }
		/// <summary>True inside mature woodland; optional for placement probes and chapters without canopy.</summary>
		public Func<Vector2, bool> IsWooded { get; init; }
		/// <summary>The run's start: distances and bearings are measured from here.</summary>
		public Vector2 Origin { get; init; }
		/// <summary>World pixels in one terrain chunk (TerrainStreamer.ChunkTiles x tile size): the unit the bands are in.</summary>
		public float ChunkPixels { get; init; } = 1536f;
		/// <summary>Maze stages snap every site to a corridor cell's centre, this far apart; 0 on an open stage.</summary>
		public float MazeCellPixels { get; init; }
	}

	private LevelGenerator.Rng rng;
	private readonly Ground ground;
	private readonly List<(Vector2 Centre, float Radius)> reserved = new();

	public ExplorePlacer(ulong seed, int stageIndex, int stream, Ground ground)
	{
		this.ground = ground;
		rng = new LevelGenerator.Rng(LevelGenerator.Hash(seed, stageIndex, stream, SaltExplore));
	}

	private const int SaltExplore = 0x5E7E;

	/// <summary>Where the event's first site was rolled, in radians from east; NaN until then.</summary>
	public float EventBearing { get; private set; } = float.NaN;

	/// <summary>The event band, in chunks from the start. Set by the director before PlaceSites.</summary>
	public float EventMinChunks { get; init; } = 4f;
	public float EventMaxChunks { get; init; } = 6f;

	/// <summary>Footprints placed so far: the decor passes keep out of them (Node2DGame.ReserveExploreGround).</summary>
	public IReadOnlyList<(Vector2 Centre, float Radius)> Reserved => reserved;

	public Vector2 Origin => ground.Origin;

	/// <summary>World pixels in one terrain chunk: the unit the bands are in.</summary>
	public float ChunkPixels => ground.ChunkPixels;

	/// <summary>A clearing with mature woodland on at least half its surrounding ring.</summary>
	public bool HasWoodedSurrounds(Vector2 at, float radius)
	{
		if (ground.IsWooded == null) return true;
		int wooded = 0;
		for (int i = 0; i < 8; i++)
			if (ground.IsWooded(at + Direction(i * Mathf.Tau / 8f) * radius)) wooded++;
		return wooded >= 4;
	}

	/// <summary>The next roll in this placer's stream, 0 to 1.</summary>
	public float NextFloat() => rng.NextFloat();

	/// <summary>
	/// An event's main site placed by its chapter's own shape rather than by the band (the Last Bridge's barge,
	/// BargeEvent: on a deck over a drop the band's bearing is mostly water). Remembered as the event's site -
	/// its bearing for the wayfinder and the secrets, as <see cref="EventSite"/> remembers its roll - and its
	/// footprint reserved. The rolls that chose it are the caller's, drawn from this placer.
	/// </summary>
	public Vector2 PlacedByChapter(Vector2 site, float footprint)
	{
		if (float.IsNaN(EventBearing))
			EventBearing = BearingFrom(ground.Origin, site);
		return Keep(site, footprint);
	}

	// --- a chapter's second event (P2, .ai/chapter-wizards.md 6.3) ---------------------------------------
	// The bearings an event's main site must keep away from - every earlier event's in the same chapter - and
	// by how much, and the footprints it may not overlap. Empty for a chapter's first event, which is then
	// placed exactly as before: no roll, no test and no search changes.
	private IReadOnlyList<float> avoidBearings = Array.Empty<float>();
	private float avoidSeparation;
	private IReadOnlyList<(Vector2 Centre, float Radius)> keepOff = Array.Empty<(Vector2, float)>();

	/// <summary>
	/// Keeps this placer's event site (<see cref="EventSite"/>, <see cref="EventSiteInBand"/>) at least
	/// <paramref name="separation"/> radians round from each of <paramref name="bearings"/>, seen from the
	/// start, and off every one of <paramref name="footprints"/>: the roll is drawn inside the allowed arcs
	/// (<see cref="BearingAvoiding"/>), and every spot the search or the band sweep tries is held to the same
	/// rule, so the site the search settles on keeps it too - not only the roll. Set before PlaceSites.
	/// </summary>
	public ExplorePlacer Avoiding(IReadOnlyList<float> bearings, float separation,
		IReadOnlyList<(Vector2 Centre, float Radius)> footprints = null)
	{
		avoidBearings = bearings ?? Array.Empty<float>();
		avoidSeparation = separation;
		keepOff = footprints ?? Array.Empty<(Vector2, float)>();
		return this;
	}

	/// <summary>True when this placer's event site has bearings to keep away from (it is a chapter's later event).</summary>
	public bool HasSiteRule => avoidBearings.Count > 0 || keepOff.Count > 0;

	/// <summary>The bearing of <paramref name="point"/> seen from <paramref name="origin"/>, in radians from east.</summary>
	public static float BearingFrom(Vector2 origin, Vector2 point) => Mathf.Atan2(point.Y - origin.Y, point.X - origin.X);

	/// <summary>How far round <paramref name="a"/> is from <paramref name="b"/>: 0 to pi.</summary>
	public static float Separation(float a, float b) => Mathf.Abs(Mathf.AngleDifference(a, b));

	/// <summary>
	/// A roll (0 to 1) mapped onto the bearings at least <paramref name="separation"/> from every one of
	/// <paramref name="avoid"/>: the arcs between them, each trimmed by the separation at both ends, laid end to
	/// end. None to avoid: the roll times a whole turn, as every unconstrained bearing is rolled. One: exactly
	/// <c>b + separation + roll * (tau - 2 * separation)</c>, the wayfinder's formula from before there were two
	/// events, so a one-event chapter's wayfinder is where it always was. When the bearings leave no gap that
	/// wide, the middle of the widest gap.
	/// </summary>
	public static float BearingAvoiding(float roll, IReadOnlyList<float> avoid, float separation)
	{
		if (avoid == null || avoid.Count == 0)
			return roll * Mathf.Tau;
		if (avoid.Count == 1)
			return avoid[0] + separation + roll * (Mathf.Tau - 2f * separation);

		var sorted = new List<float>(avoid.Count);
		foreach (float bearing in avoid)
			sorted.Add(Mathf.PosMod(bearing, Mathf.Tau));
		sorted.Sort();
		float total = 0f;
		float widestStart = sorted[0], widest = -1f;
		for (int i = 0; i < sorted.Count; i++)
		{
			float start = sorted[i];
			float gap = (i + 1 < sorted.Count ? sorted[i + 1] : sorted[0] + Mathf.Tau) - start;
			total += Mathf.Max(0f, gap - 2f * separation);
			if (gap > widest)
			{
				widest = gap;
				widestStart = start;
			}
		}
		if (total <= 0f)
			return widestStart + widest * 0.5f;
		float along = Mathf.Clamp(roll, 0f, 1f) * total;
		for (int i = 0; i < sorted.Count; i++)
		{
			float start = sorted[i];
			float gap = (i + 1 < sorted.Count ? sorted[i + 1] : sorted[0] + Mathf.Tau) - start;
			float open = Mathf.Max(0f, gap - 2f * separation);
			if (along <= open)
				return start + separation + along;
			along -= open;
		}
		return widestStart + widest * 0.5f;
	}

	/// <summary>True if a spot keeps this placer's site rule: far enough round from every bearing it avoids, off every footprint.</summary>
	public bool KeepsSiteRule(Vector2 point, float footprint)
	{
		if (avoidBearings.Count > 0 && point.DistanceSquaredTo(ground.Origin) > 1f)
		{
			float bearing = BearingFrom(ground.Origin, point);
			foreach (float other in avoidBearings)
			{
				// A hair under, so a site the band sweep put exactly on the edge of an arc is not refused for rounding.
				if (Separation(bearing, other) < avoidSeparation - 1e-4f)
					return false;
			}
		}
		foreach (var (centre, radius) in keepOff)
		{
			if (centre.DistanceTo(point) < radius + footprint)
				return false;
		}
		return true;
	}

	// The test with the site rule folded in; the test itself when there is no rule.
	private Func<Vector2, float, bool> WithSiteRule(Func<Vector2, float, bool> test)
	{
		if (!HasSiteRule)
			return test;
		Func<Vector2, float, bool> inner = test ?? ground.IsClear;
		return (p, footprint) => KeepsSiteRule(p, footprint) && inner(p, footprint);
	}

	// The event band's roll: anywhere round for a chapter's first event, inside the allowed arcs for a later one.
	private float RollEventBearing() =>
		avoidBearings.Count == 0 ? NextFloat() * Mathf.Tau : BearingAvoiding(NextFloat(), avoidBearings, avoidSeparation);

	/// <summary>
	/// The event's main site: a seeded bearing and a seeded distance in the event band, then the
	/// nearest open footprint to that spot. Its bearing is remembered, so the wayfinder can be put
	/// somewhere else.
	/// </summary>
	public Vector2 EventSite(float footprint, bool dense = false)
	{
		float bearing = RollEventBearing();
		float distance = Mathf.Lerp(EventMinChunks, EventMaxChunks, NextFloat()) * ground.ChunkPixels;
		float searchFrom = NextFloat() * Mathf.Tau;
		if (float.IsNaN(EventBearing))
			EventBearing = bearing;
		Func<Vector2, float, bool> test = dense ? ground.IsClearDense ?? ground.IsClear : null;
		if (!HasSiteRule)
			return Search(ground.Origin + Direction(bearing) * distance, footprint, searchFrom, test: test);

		// A later event: the search held to the rule, then - if nothing open keeps it near the roll - the whole
		// band swept for one, as EventSiteInBand does. Only then the old fallback, which says so.
		test = WithSiteRule(test);
		Vector2 candidate = ground.Origin + Direction(bearing) * distance;
		if (TrySearch(candidate, footprint, searchFrom, null, 0f, test, out Vector2 found))
			return Keep(found, footprint);
		if (TrySweepBand(bearing, distance, EventMinChunks, EventMaxChunks, footprint, test, out found))
		{
			EventBearing = BearingFrom(ground.Origin, found);
			return Keep(found, footprint);
		}
		GD.PushWarning($"ExplorePlacer: no open ground keeping a chapter's events apart for a {footprint:0} px footprint; placing at {candidate} anyway.");
		return Keep(Snap(candidate), footprint);
	}

	/// <summary>
	/// An event site chosen by its chapter's own rules rather than rolled in the band - the Court's rooms, which are
	/// a fixed building (SeersVoiceEvent) - recorded as the event's site all the same: its bearing becomes
	/// <see cref="EventBearing"/> (so the wayfinder keeps away from it, as from any) and its footprint is reserved.
	/// Draws no roll.
	/// </summary>
	public Vector2 FixedSite(Vector2 site, float footprint)
	{
		if (float.IsNaN(EventBearing))
			EventBearing = BearingFrom(ground.Origin, site);
		return Keep(site, footprint);
	}

	/// <summary>A site on a given bearing, at a seeded distance between two bands (chunks from the start).</summary>
	public Vector2 AtBearing(float bearing, float minChunks, float maxChunks, float footprint)
	{
		float distance = Mathf.Lerp(minChunks, maxChunks, NextFloat()) * ground.ChunkPixels;
		float searchFrom = NextFloat() * Mathf.Tau;
		return Search(ground.Origin + Direction(bearing) * distance, footprint, searchFrom);
	}

	/// <summary>
	/// A site a seeded step from <paramref name="from"/>: <paramref name="minStep"/> to
	/// <paramref name="maxStep"/> away at a seeded bearing, at least <paramref name="spacing"/> from
	/// every point in <paramref name="avoid"/> and within <paramref name="reach"/> of
	/// <paramref name="anchor"/>. Twelve bearings are tried, all rolled up front; the bog lanterns'
	/// chain is laid out with it.
	/// </summary>
	public Vector2 Near(Vector2 from, float minStep, float maxStep, float footprint,
		IReadOnlyList<Vector2> avoid, float spacing, Vector2 anchor, float reach)
	{
		float bearing = NextFloat() * Mathf.Tau;
		float step = Mathf.Lerp(minStep, maxStep, NextFloat());
		float searchFrom = NextFloat() * Mathf.Tau;
		Vector2 fallback = Vector2.Inf;
		for (int k = 0; k < 12; k++)
		{
			Vector2 candidate = Snap(from + Direction(bearing + k * Mathf.Tau / 12f) * step);
			if (candidate.DistanceTo(anchor) > reach || !Spaced(candidate, avoid, spacing))
				continue;
			if (ground.IsClear(candidate, footprint))
				return Keep(candidate, footprint);
			if (fallback == Vector2.Inf)
				fallback = candidate;
		}
		// Nothing open on the ring at that step: search outward from the first spaced candidate (or
		// the plain step), keeping the spacing.
		Vector2 start = fallback != Vector2.Inf ? fallback : from + Direction(bearing) * step;
		return Search(start, footprint, searchFrom, avoid, spacing);
	}

	/// <summary>Keeps decor out of a circle that is not a site's footprint (a road, say).</summary>
	public void Reserve(Vector2 centre, float radius) => reserved.Add((centre, radius));

	/// <summary>True if a footprint round this point is open ground. On a maze, asked of the snapped cell.</summary>
	public bool IsClear(Vector2 point, float footprint) => ground.IsClear(Snap(point), footprint);

	/// <summary>
	/// <see cref="IsClear"/> over the whole footprint (the dense test EventSite takes with <c>dense</c>): for a place that
	/// is drawn over its whole disc and walked to, not only stood on - the gibbet's ferry landing.
	/// </summary>
	public bool IsClearDense(Vector2 point, float footprint) => (ground.IsClearDense ?? ground.IsClear)(Snap(point), footprint);

	/// <summary>True if a body can stand on this point (Ground.IsWalkable; everywhere when the ground does not say).</summary>
	public bool IsWalkable(Vector2 point) => ground.IsWalkable?.Invoke(point) ?? true;

	/// <summary>The nearest corridor cell centre on a maze; the point itself on an open stage.</summary>
	public Vector2 Snap(Vector2 point)
	{
		float cell = ground.MazeCellPixels;
		if (cell <= 0f)
			return point;
		Vector2 rel = point - ground.Origin;
		return ground.Origin + new Vector2(Mathf.Round(rel.X / cell) * cell, Mathf.Round(rel.Y / cell) * cell);
	}

	// How far the fallback search goes. A candidate that lands in a lake or a landmark walks outward
	// in rings until it finds open ground; a stage with no open ground within this of a spot is not a
	// stage this game has.
	private const float SearchStep = 96f;
	private const float SearchReach = 1600f;

	private Vector2 Search(Vector2 candidate, float footprint, float startAngle, IReadOnlyList<Vector2> avoid = null, float spacing = 0f,
		Func<Vector2, float, bool> test = null)
	{
		if (TrySearch(candidate, footprint, startAngle, avoid, spacing, test, out Vector2 found))
			return Keep(found, footprint);
		// Measured never to happen on the shipping stages; if it ever does, the site still exists
		// rather than the event silently not being there, and --explore-log says so.
		GD.PushWarning($"ExplorePlacer: no open ground within {SearchReach} px of {candidate}; placing there anyway.");
		return Keep(Snap(candidate), footprint);
	}

	private bool TrySearch(Vector2 candidate, float footprint, float startAngle, IReadOnlyList<Vector2> avoid,
		float spacing, Func<Vector2, float, bool> test, out Vector2 found)
	{
		test ??= ground.IsClear;
		Vector2 first = Snap(candidate);
		found = first;
		if (Spaced(first, avoid, spacing) && test(first, footprint))
			return true;

		float step = ground.MazeCellPixels > 0f ? ground.MazeCellPixels : SearchStep;
		for (float r = step; r <= SearchReach + 0.1f; r += step)
		{
			int around = Mathf.Max(8, Mathf.Min(24, Mathf.RoundToInt(Mathf.Tau * r / step)));
			for (int k = 0; k < around; k++)
			{
				Vector2 p = Snap(candidate + Direction(startAngle + k * Mathf.Tau / around) * r);
				if (Spaced(p, avoid, spacing) && test(p, footprint))
				{
					found = p;
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// The event's main site for a footprint wider than a corridor - the Five-Pointed Seal's courtyard, which
	/// on the Ruins' lattice fits only in a court (one block in eight, .ai/terrain.md section 11). EventSite's
	/// rolls and its search first; if that finds nothing open within its reach, the whole event band is swept,
	/// ring by ring out from the rolled distance and either way round from the rolled bearing, one maze cell
	/// a step, and the first open footprint taken - still 4 to 6 chunks out, still the seed's. The event's
	/// bearing becomes the site's, so the wayfinder keeps away from where it really is. Only then the old
	/// fallback. Opt-in: every site placed before is placed exactly as it was.
	/// </summary>
	public Vector2 EventSiteInBand(float footprint, bool dense = false,
		IReadOnlyList<(Vector2 Centre, float Radius)> keepOff = null, Func<Vector2, bool> also = null,
		float? minChunks = null, float? maxChunks = null)
	{
		float min = minChunks ?? EventMinChunks, max = maxChunks ?? EventMaxChunks;
		float bearing = RollEventBearing();
		float distance = Mathf.Lerp(min, max, NextFloat()) * ground.ChunkPixels;
		float searchFrom = NextFloat() * Mathf.Tau;
		if (float.IsNaN(EventBearing))
			EventBearing = bearing;
		Func<Vector2, float, bool> test = KeepingOff(dense ? ground.IsClearDense ?? ground.IsClear : ground.IsClear, keepOff);
		if (also != null)
		{
			Func<Vector2, float, bool> inner = test;
			test = (p, f) => inner(p, f) && also(p);
		}
		// A chapter's later event keeps round from the earlier ones however far the sweep goes (no-op for the first).
		test = WithSiteRule(test);
		Vector2 candidate = ground.Origin + Direction(bearing) * distance;
		if (minChunks.HasValue || maxChunks.HasValue)
		{
			Func<Vector2, float, bool> allowed = test;
			test = (p, f) => p.DistanceTo(ground.Origin) >= min * ground.ChunkPixels
				&& p.DistanceTo(ground.Origin) <= max * ground.ChunkPixels && allowed(p, f);
		}
		if (TrySearch(candidate, footprint, searchFrom, null, 0f, test, out Vector2 found))
			return Keep(found, footprint);
		if (TrySweepBand(bearing, distance, min, max, footprint, test, out found))
		{
			EventBearing = Mathf.Atan2(found.Y - ground.Origin.Y, found.X - ground.Origin.X);
			return Keep(found, footprint);
		}
		GD.PushWarning($"ExplorePlacer: no open ground for a {footprint:0} px footprint anywhere in the event band; placing at {candidate} anyway.");
		return Keep(Snap(candidate), footprint);
	}

	/// <summary>
	/// <see cref="AtBearing"/> for a footprint wider than a corridor (a vault's, on the Ruins' lattice): the
	/// same rolls and search, then - if that finds nothing open - the whole band between the two distances
	/// swept as <see cref="EventSiteInBand"/> sweeps it, starting at this bearing and turning away from it
	/// either side, so the site lands as near the bearing it was given as open ground allows. With
	/// <paramref name="dense"/>, open over the whole footprint (the thaw's test). When the first search
	/// finds ground and the test is not dense, the site is exactly the one AtBearing gives.
	/// </summary>
	public Vector2 AtBearingInBand(float bearing, float minChunks, float maxChunks, float footprint, bool dense = false,
		IReadOnlyList<(Vector2 Centre, float Radius)> keepOff = null, Func<Vector2, bool> also = null)
	{
		float distance = Mathf.Lerp(minChunks, maxChunks, NextFloat()) * ground.ChunkPixels;
		float searchFrom = NextFloat() * Mathf.Tau;
		Func<Vector2, float, bool> test = KeepingOff(dense ? ground.IsClearDense ?? ground.IsClear : ground.IsClear, keepOff);
		if (also != null)
		{
			Func<Vector2, float, bool> inner = test;
			test = (p, f) => inner(p, f) && also(p);
		}
		Vector2 candidate = ground.Origin + Direction(bearing) * distance;
		if (TrySearch(candidate, footprint, searchFrom, null, 0f, test, out Vector2 found))
			return Keep(found, footprint);
		if (TrySweepBand(bearing, distance, minChunks, maxChunks, footprint, test, out found))
			return Keep(found, footprint);
		GD.PushWarning($"ExplorePlacer: no open ground for a {footprint:0} px footprint anywhere {minChunks:0.#}-{maxChunks:0.#} chunks out; placing at {candidate} anyway.");
		return Keep(Snap(candidate), footprint);
	}

	/// <summary>
	/// The wayfinder's site: exactly <see cref="AtBearing"/>'s wherever its first search finds ground (every open
	/// chapter, so no wayfinder there moved). Where it does not - the Court, whose building fills only the quarter of
	/// the band north-east of the stair - the band is swept for one that keeps <paramref name="separation"/> round
	/// from every one of <paramref name="avoid"/>, then two thirds of that, a third, and none: as far round from the
	/// events as the ground there allows, never left in the rock.
	/// </summary>
	public Vector2 AtBearingInBandAvoiding(float bearing, float minChunks, float maxChunks, float footprint,
		IReadOnlyList<float> avoid, float separation)
	{
		float distance = Mathf.Lerp(minChunks, maxChunks, NextFloat()) * ground.ChunkPixels;
		float searchFrom = NextFloat() * Mathf.Tau;
		Vector2 candidate = ground.Origin + Direction(bearing) * distance;
		if (TrySearch(candidate, footprint, searchFrom, null, 0f, null, out Vector2 found))
			return Keep(found, footprint);
		foreach (float share in new[] { 1f, 2f / 3f, 1f / 3f, 0f })
		{
			float need = separation * share;
			Func<Vector2, float, bool> test = (p, f) =>
			{
				if (need > 0f && avoid != null)
				{
					float b = BearingFrom(ground.Origin, p);
					foreach (float other in avoid)
						if (!float.IsNaN(other) && Separation(b, other) < need)
							return false;
				}
				return ground.IsClear(p, f);
			};
			if (TrySweepBand(bearing, distance, minChunks, maxChunks, footprint, test, out found))
				return Keep(found, footprint);
		}
		GD.PushWarning($"ExplorePlacer: no open ground for a {footprint:0} px footprint anywhere {minChunks:0.#}-{maxChunks:0.#} chunks out; placing at {candidate} anyway.");
		return Keep(Snap(candidate), footprint);
	}

	/// <summary>
	/// <see cref="AtBearingInBand"/>, but the spot it settles on keeps <paramref name="separation"/> round from every
	/// bearing in <paramref name="avoid"/> - then two thirds of that, a third, and none, each searched near the roll
	/// and then over the whole band. For a site whose room is rare (the Oubliette's dead end: the maze's braid pass
	/// gives nine dead ends in ten a second way out), where the plain sweep walks round its ring until it finds one,
	/// and on seed 4242 walked the Oubliette round to 5 degrees off the Gaoler's key's bearing. The bearing is tested
	/// first, so a spot outside the arc never asks the ground about itself.
	/// </summary>
	public Vector2 AtBearingInBandKeeping(float bearing, float minChunks, float maxChunks, float footprint,
		IReadOnlyList<float> avoid, float separation, bool dense = false,
		IReadOnlyList<(Vector2 Centre, float Radius)> keepOff = null, Func<Vector2, bool> also = null)
	{
		float distance = Mathf.Lerp(minChunks, maxChunks, NextFloat()) * ground.ChunkPixels;
		float searchFrom = NextFloat() * Mathf.Tau;
		Func<Vector2, float, bool> open = KeepingOff(dense ? ground.IsClearDense ?? ground.IsClear : ground.IsClear, keepOff);
		Vector2 candidate = ground.Origin + Direction(bearing) * distance;
		foreach (float share in new[] { 1f, 2f / 3f, 1f / 3f, 0f })
		{
			float need = separation * share;
			Func<Vector2, float, bool> test = (p, f) =>
			{
				if (need > 0f && avoid != null)
				{
					float b = BearingFrom(ground.Origin, p);
					foreach (float other in avoid)
						if (!float.IsNaN(other) && Separation(b, other) < need - 1e-4f)
							return false;
				}
				return (also == null || also(p)) && open(p, f);
			};
			if (TrySearch(candidate, footprint, searchFrom, null, 0f, test, out Vector2 found))
				return Keep(found, footprint);
			if (TrySweepBand(bearing, distance, minChunks, maxChunks, footprint, test, out found))
				return Keep(found, footprint);
		}
		GD.PushWarning($"ExplorePlacer: no open ground for a {footprint:0} px footprint anywhere {minChunks:0.#}-{maxChunks:0.#} chunks out; placing at {candidate} anyway.");
		return Keep(Snap(candidate), footprint);
	}

	// A test that also refuses a footprint overlapping one already reserved - another site's court, which the
	// ground tests cannot see (a reservation keeps decor off, not other sites). Null keeps the test as it is.
	private static Func<Vector2, float, bool> KeepingOff(Func<Vector2, float, bool> test,
		IReadOnlyList<(Vector2 Centre, float Radius)> keepOff)
	{
		if (keepOff == null || keepOff.Count == 0)
			return test;
		return (p, footprint) =>
		{
			foreach (var (centre, radius) in keepOff)
				if (centre.DistanceTo(p) < radius + footprint)
					return false;
			return test(p, footprint);
		};
	}

	// The band between two distances, ring by ring out from the rolled distance and back in, and round each
	// ring either way from the bearing, one maze cell (or SearchStep) a step; the first open footprint.
	private bool TrySweepBand(float bearing, float distance, float minChunks, float maxChunks, float footprint,
		Func<Vector2, float, bool> test, out Vector2 found)
	{
		float step = ground.MazeCellPixels > 0f ? ground.MazeCellPixels : SearchStep;
		float min = minChunks * ground.ChunkPixels;
		float max = maxChunks * ground.ChunkPixels;
		int rings = Mathf.FloorToInt((max - min) / step) + 1;
		for (int n = 0; n < rings * 2 + 2; n++)
		{
			// distance, +1, -1, +2, -2...
			float d = distance + ((n % 2 == 0) ? 1 : -1) * ((n + 1) / 2) * step;
			if (d < min || d > max)
				continue;
			int around = Mathf.Max(8, Mathf.RoundToInt(Mathf.Tau * d / step));
			for (int k = 0; k < around; k++)
			{
				int side = (k % 2 == 0) ? (k / 2) : -((k + 1) / 2);
				Vector2 p = Snap(ground.Origin + Direction(bearing + side * Mathf.Tau / around) * d);
				if (test(p, footprint))
				{
					found = p;
					return true;
				}
			}
		}
		found = Vector2.Zero;
		return false;
	}

	private Vector2 Keep(Vector2 site, float footprint)
	{
		reserved.Add((site, footprint));
		return site;
	}

	private static bool Spaced(Vector2 p, IReadOnlyList<Vector2> avoid, float spacing)
	{
		if (avoid == null || spacing <= 0f)
			return true;
		foreach (Vector2 other in avoid)
			if (other.DistanceTo(p) < spacing)
				return false;
		return true;
	}

	private static Vector2 Direction(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));
}
