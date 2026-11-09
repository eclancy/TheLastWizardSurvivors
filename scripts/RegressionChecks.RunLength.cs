using Godot;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// A CHAPTER'S OWN RUN LENGTH (.ai/run-pacing.md section 12; Node2DGame.RunLength.cs). Eric, 2026-11-08: "still make the
// far shore longer". Every road chapter runs fifteen minutes and every far-shore chapter twenty, read through
// StageCatalog.VictorySecondsFor - and what a build cannot see is a chapter added to either order without its length,
// or the road quietly moved off the run every pacing number was tuned on. So:
//
//   - every chapter of the campaign (the road and the castle) and every chapter outside both orders (the Sketchbook)
//     runs StageCatalog.RoadRunSeconds, 900; every chapter of FarShoreOrder runs FarShoreRunSeconds, 1200;
//   - the road's run IS the reference run (RoadRunSeconds == Node2DGame.ReferenceRunSeconds), so RunPace is exactly 1
//     there and every paced number plays as it was tuned;
//   - VictorySecondsFor falls back to the road for an id it does not know;
//   - the XP curve follows the length (Node2DGame.XpCurveForRun): untouched on the road, whatever its numbers, and on a
//     longer run every level costs the run's length over the reference run's, to within a level's rounding - which is
//     what puts the same level at the same share of the run.
public static partial class RegressionChecks
{
	private static void ValidateRunLengths(List<string> warnings)
	{
		if (!Mathf.IsEqualApprox(StageCatalog.RoadRunSeconds, Node2DGame.ReferenceRunSeconds))
			warnings.Add($"Run length: a road run is {StageCatalog.RoadRunSeconds:0} s but the pace clock's reference run is {Node2DGame.ReferenceRunSeconds:0} s; "
				+ "the road must BE the reference run, or every paced number on it plays off its tuning.");

		int road = 0, far = 0;
		foreach (StageDefinition stage in StageCatalog.All)
		{
			bool onFarShore = StageCatalog.IsOnFarShore(stage);
			float want = onFarShore ? StageCatalog.FarShoreRunSeconds : StageCatalog.RoadRunSeconds;
			float have = StageCatalog.VictorySecondsFor(stage.Id);
			if (!Mathf.IsEqualApprox(have, want))
				warnings.Add($"Run length: '{stage.Id}' ({stage.DisplayName}) runs {have:0} s; "
					+ (onFarShore ? $"every far-shore chapter runs {want:0} (StageCatalog.FarShoreRunSeconds, Eric 2026-11-08)." : $"every chapter off the far shore runs {want:0} (StageCatalog.RoadRunSeconds)."));
			if (onFarShore) far++; else road++;
		}

		if (!Mathf.IsEqualApprox(StageCatalog.VictorySecondsFor("no_such_stage"), StageCatalog.RoadRunSeconds)
			|| !Mathf.IsEqualApprox(StageCatalog.VictorySecondsFor(null), StageCatalog.RoadRunSeconds))
			warnings.Add("Run length: StageCatalog.VictorySecondsFor does not fall back to a road run for an unknown stage.");

		// The XP curve. On the road it is never touched; on a longer run each level costs length/reference as much.
		var curves = new List<string>();
		foreach (float length in StageCatalog.All.Select(s => StageCatalog.VictorySecondsFor(s.Id)).Distinct().OrderBy(l => l))
		{
			(int first, int step) = Node2DGame.XpCurveForRun(10, 45, length);
			curves.Add($"{length / 60f:0.#} min: {first} + {step}");
			if (Mathf.IsEqualApprox(length, Node2DGame.ReferenceRunSeconds))
			{
				foreach ((int f, int s) in new[] { (10, 45), (7, 31), (1, 1) })
					if (Node2DGame.XpCurveForRun(f, s, length) != (f, s))
						warnings.Add($"Run length: the reference run's XP curve {f} + {s} came back as {Node2DGame.XpCurveForRun(f, s, length)}; the road's curve must never move.");
				continue;
			}
			float scale = length / Node2DGame.ReferenceRunSeconds;
			for (int level = 1; level <= 40; level++)
			{
				float roadCost = 10 + 45 * (level - 1);
				float runCost = first + step * (level - 1);
				// Rounding the two terms costs at most half a point each, so a level is off by at most (level - 1) / 2 + 1/2.
				if (Mathf.Abs(runCost - roadCost * scale) > 0.5f * level + 0.01f)
				{
					warnings.Add($"Run length: on a {length / 60f:0.#}-minute run level {level} costs {runCost:0}, not about {roadCost * scale:0} ({scale:0.##} x the road's {roadCost:0}); "
						+ "a longer run banks proportionally more XP at every share of it, so its levels must cost proportionally more.");
					break;
				}
			}
		}

		GD.Print($"RegressionChecks: run lengths - {road} chapters at {StageCatalog.RoadRunSeconds / 60f:0.#} minutes, {far} far-shore chapters at "
			+ $"{StageCatalog.FarShoreRunSeconds / 60f:0.#}; XP curves {string.Join(", ", curves)}");
	}
}
