using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// INK RING (scripts/InkRing.cs, 2026-11-09). What a build cannot see and a run would only show as a ring that never
// closes, closes on a shuffle, strikes the wrong side of its line or starts the size of the screen: the loop geometry
// walked by hand (InkPath), the small start, the cards' words against the node's numbers, the prison's layer, and the
// Cormorant feat that pays for it.
public static partial class RegressionChecks
{
	private static void ValidateInkRing(List<string> warnings)
	{
		if (!ContentValidator.SpellResourcePaths.Contains("res://SpellData_InkRing.tres"))
			warnings.Add("Ink Ring: SpellData_InkRing.tres is not in ContentValidator.SpellResourcePaths, so nothing validates it.");

		SpellData spell = ContentValidator.LoadAllSpells().FirstOrDefault(s => s?.Id == InkRing.SpellId);
		if (spell == null)
			warnings.Add("Ink Ring: no loaded spell has the id 'ink_ring'.");
		else
		{
			if (spell.TargetingMode != SpellTargetingMode.Drawn)
				warnings.Add($"Ink Ring: its TargetingMode is {spell.TargetingMode}; the walk draws what it strikes (Drawn).");
			Dictionary<Element, int> weights = spell.GetElementWeights();
			if (weights.Count != 2 || !weights.ContainsKey(Element.Water) || !weights.ContainsKey(Element.Darkness))
				warnings.Add($"Ink Ring: it names {string.Join("/", weights.Keys)}; it is Water and Darkness, the two halves of the ink.");
			// Spells start small (Eric, 2026-11-02): the ink fades fast at level 1, so only a small ring closes, and it
			// lasts longer as the spell levels.
			float first = InkRing.InkSecondsAt(spell, 1), last = InkRing.InkSecondsAt(spell, spell.MaxLevel);
			if (first > 2.5f || last <= first)
				warnings.Add($"Ink Ring: the ink lasts {first:0.##} s at level 1 and {last:0.##} s at its last; it starts short and grows.");
		}

		// THE LOOP, walked by hand. A wizard walking a loop at the line's spacing: a prolate cycloid, each turn of which is
		// a loop that crosses its own path, drawn along at his walk. It must close exactly once a turn, holding about the
		// loop's area; a straight walk must never close; a shuffle (a loop far under MinRingArea) must not either; and a
		// loop whose start has dried before he gets back round must not.
		// x = 12t - 70 sin t, y = -70 cos t over three half-turns: one loop about 140 units tall, 366 walked round it and
		// 10,300 square units inside it (measured off the curve itself), crossing its own path once.
		int closed = WalkInk((theta) => new Vector2(12f * theta - 70f * MathF.Sin(theta), -70f * MathF.Cos(theta)),
			MathF.PI * 3f, 60f, out float area);
		if (closed != 1)
			warnings.Add($"Ink Ring: walking one loop closed {closed} rings; a loop that crosses its own line closes one.");
		else if (area < 8500f || area > 11500f)
			warnings.Add($"Ink Ring: a loop of about 10,300 square units closed a ring of {area:0}; its area is lost somewhere.");
		if (WalkInk((theta) => new Vector2(theta * 40f, 0f), 30f, 60f, out _) != 0)
			warnings.Add("Ink Ring: a straight walk closed a ring.");
		if (WalkInk((theta) => new Vector2(4f * theta - 18f * MathF.Sin(theta), -18f * MathF.Cos(theta)), MathF.PI * 3f, 60f, out _) != 0)
			warnings.Add("Ink Ring: a shuffle no wider than a body closed a ring; MinRingArea must turn it down.");
		if (WalkInk((theta) => new Vector2(12f * theta - 70f * MathF.Sin(theta), -70f * MathF.Cos(theta)), MathF.PI * 3f, 0.5f, out _) != 0)
			warnings.Add("Ink Ring: a loop whose start had dried away still closed; old ink is gone.");

		// Inside and outside: a square, and the point-in-polygon test the strike uses.
		var square = new[] { new Vector2(0f, 0f), new Vector2(100f, 0f), new Vector2(100f, 100f), new Vector2(0f, 100f) };
		if (!InkPath.Contains(square, 0, 4, new Vector2(50f, 50f)) || InkPath.Contains(square, 0, 4, new Vector2(150f, 50f))
			|| InkPath.Contains(square, 0, 4, new Vector2(50f, -5f)))
			warnings.Add("Ink Ring: the inside test does not tell inside a ring from outside it.");
		if (MathF.Abs(MathF.Abs(InkPath.SignedArea(square, 0, 4)) - 10000f) > 0.5f)
			warnings.Add("Ink Ring: the shoelace area of a 100-unit square is not 10000.");

		// The cards' words against the numbers the node reads. Read through locals: a constant compared with a constant is
		// CS0162 at the warning it guards.
		float longShare = InkRing.LongStrokeShare, wetShare = InkRing.WetShare, poolSlow = InkRing.PoolSlowShare;
		if (longShare <= 1f)
			warnings.Add($"Ink Ring: Long Stroke makes the ink last x{longShare}; it lasts longer.");
		if (wetShare <= 0f || wetShare >= 1f)
			warnings.Add($"Ink Ring: Wet Ink stings for {wetShare:0%} of a strike; a sting is less than the ring.");
		if (spell != null)
		{
			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			SpellEvolutionOption lengthCard = spell.Level4Options?.FirstOrDefault(o => o?.Id == InkRing.LongStrokeId);
			SpellEvolutionOption wetCard = spell.Level4Options?.FirstOrDefault(o => o?.Id == InkRing.WetInkId);
			SpellEvolutionOption poolCard = spell.Level4Options?.FirstOrDefault(o => o?.Id == InkRing.InkwellId);
			if (lengthCard == null || !lengthCard.Description.Contains($"{(longShare - 1f) * 100f:0}%"))
				warnings.Add($"Ink Ring: Long Stroke's card does not say the ink lasts {(longShare - 1f) * 100f:0}% longer.");
			if (wetCard == null || !wetCard.Description.Contains($"{wetShare * 100f:0}%"))
				warnings.Add($"Ink Ring: Wet Ink's card does not say it stings for {wetShare * 100f:0}% of a strike.");
			if (poolCard == null || !poolCard.Description.Contains($"{poolSlow * 100f:0}%"))
				warnings.Add($"Ink Ring: Inkwell's card does not say its pool slows by {poolSlow * 100f:0}%.");
			// Folio's Inkblot is a stain that slows: no Ink Ring card may wear its name.
			if (spell.Level4Options.Concat(spell.Level8Options).Any(o => o?.DisplayName == "Inkblot"))
				warnings.Add("Ink Ring: a card is named Inkblot, which is Folio's.");
			foreach (string id in new[] { InkRing.InkPrisonId, InkRing.SecondStrokeId })
				if (spell.Level8Options?.Any(o => o?.Id == id) != true)
					warnings.Add($"Ink Ring: no level 8 card '{id}', which the node reads by id.");
		}

		// THE PRISON stands on the layer only the swarm meets (Ma Liang's inked door lines'): every enemy masks it, and the
		// wizard's body and feet never do, so he walks through the wall he drew.
		if (InkRing.PrisonLayerBit != 1u << (LevelTilePainter.EnemyOnlyCollisionLayer - 1))
			warnings.Add("Ink Ring: the prison wall is not on LevelTilePainter.EnemyOnlyCollisionLayer.");

		// THE CORMORANT pays it: a run one Water-or-Darkness kill short does not, one that makes the count does.
		AchievementDefinition cormorant = AchievementDefinitions.GetById("cormorant");
		if (cormorant == null || cormorant.SpellUnlockId != InkRing.SpellId)
			warnings.Add("Ink Ring: no Cormorant feat pays Ink Ring, so nobody can ever hold it.");
		else
		{
			int target = AchievementDefinitions.CormorantInkKills;
			if (cormorant.IsComplete(AchievementContext.Projected(new SaveData(), new RunResult { InkKills = target - 1 })))
				warnings.Add($"Ink Ring: Cormorant completes on {target - 1} Water and Darkness kills in a run; it asks for {target}.");
			if (!cormorant.IsComplete(AchievementContext.Projected(new SaveData(), new RunResult { InkKills = target })))
				warnings.Add($"Ink Ring: a run with {target} Water and Darkness kills does not complete Cormorant (LifetimeStats.BestInkKillsInRun).");
			var save = new SaveData();
			save.Lifetime.Absorb(new RunResult { InkKills = 42 });
			if (LifetimeStats.FromDictionary(save.Lifetime.ToGodotDictionary()).BestInkKillsInRun != 42)
				warnings.Add("Ink Ring: LifetimeStats.BestInkKillsInRun is lost through the save's round trip.");
		}
	}

	// Walks a path the way InkRing.Lay does - a point every SampleSpacing along it, dated by a walk of 220 units a second
	// (the wizard's), the ink lasting `life` seconds - and tries to close after every point, as an armed node does. Returns
	// how many rings closed, and the area of the last.
	private static int WalkInk(Func<float, Vector2> at, float to, float life, out float area)
	{
		var path = new InkPath(640);
		var loop = new Vector2[256];
		area = 0f;
		int closed = 0;
		float travelled = 0f;
		Vector2 last = at(0f);
		path.Add(last, 0f, 1u);
		for (float theta = 0f; theta <= to; theta += 0.004f)
		{
			Vector2 p = at(theta);
			while (p.DistanceTo(last) >= InkRing.SampleSpacing)
			{
				last += (p - last).Normalized() * InkRing.SampleSpacing;
				travelled += InkRing.SampleSpacing;
				float now = travelled / 220f;
				path.Expire(now, life);
				path.Add(last, now, (uint)closed + 7u);
				int count = path.TryClose(loop, InkRing.MinRingArea, out float a, out _);
				if (count >= 3)
				{
					closed++;
					area = a;
				}
			}
		}
		return closed;
	}
}
