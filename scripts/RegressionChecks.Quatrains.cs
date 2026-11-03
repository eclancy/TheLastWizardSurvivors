using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheOneHeMissed.scripts;

// The Quatrains and Foresight (.ai/chapter-wizards.md 4.10): the wind-up cancel and the longer wind-up, held to their
// sentences on paper - what no build sees and a run would show only by luck. Every line is a warning, as RunAll's are.
public static partial class RegressionChecks
{
	// THE QUATRAINS: the cancel may only take an attack away, and the star must land before any wind-up it is for can.
	private static void ValidateQuatrains(List<string> warnings)
	{
		// A clock on paper: wound up, cancelled 0.3 s in. It must never resolve that wind-up; the next must wait out the
		// whole interval and then show a whole wind-up of its own.
		const float Dt = 1f / 60f;
		var clock = new AttackTelegraph(2f, 0.8f);
		float t = 0f;
		while (clock.Tick(Dt, true) != AttackTelegraph.Beat.Started && t < 10f)
			t += Dt;
		for (float s = 0f; s < 0.3f; s += Dt)
			clock.Tick(Dt, true);
		if (!clock.Cancel())
			warnings.Add("The Quatrains: AttackTelegraph.Cancel found no wind-up to end mid wind-up.");
		if (clock.Cancel())
			warnings.Add("The Quatrains: AttackTelegraph.Cancel ended a wind-up twice.");
		float since = 0f, startedAt = -1f, resolvedAt = -1f;
		while (since < 10f && resolvedAt < 0f)
		{
			since += Dt;
			switch (clock.Tick(Dt, true))
			{
				case AttackTelegraph.Beat.Started:
					startedAt = since;
					break;
				case AttackTelegraph.Beat.Resolved:
					resolvedAt = since;
					break;
			}
		}
		if (startedAt < 2f - Dt * 1.5f)
			warnings.Add($"The Quatrains: a cancelled attack began its next wind-up {startedAt:0.00} s after, inside its 2 s cooldown.");
		if (resolvedAt < 0f || resolvedAt - startedAt < 0.8f - Dt * 1.5f)
			warnings.Add($"The Quatrains: after a cancel the next wind-up showed {resolvedAt - startedAt:0.00} s, not its whole 0.8.");

		// Foresight lengthens a foreseeable wind-up and nothing else.
		float was = AttackTelegraph.ForesightScale;
		AttackTelegraph.ForesightScale = 1f + Player.CharacterForesightWindUpBonus;
		float Shown(bool foreseeable)
		{
			var c = new AttackTelegraph(0.05f, 0.8f) { Foreseeable = foreseeable };
			float shown = 0f, k = 0f;
			bool started = false;
			while (k < 5f)
			{
				k += Dt;
				AttackTelegraph.Beat beat = c.Tick(Dt, true);
				if (beat == AttackTelegraph.Beat.Started)
					started = true;
				else if (beat == AttackTelegraph.Beat.Resolved && started)
					return shown;
				if (started)
					shown += Dt;
			}
			return shown;
		}
		float foreseen = Shown(true), plain = Shown(false);
		AttackTelegraph.ForesightScale = was;
		if (MathF.Abs(foreseen - 0.8f * (1f + Player.CharacterForesightWindUpBonus)) > Dt * 2f || MathF.Abs(plain - 0.8f) > Dt * 2f)
			warnings.Add($"Foresight: a 0.8 s wind-up showed {foreseen:0.00} s foreseeable and {plain:0.00} s not (expected {0.8f * (1f + Player.CharacterForesightWindUpBonus):0.00} and 0.80).");

		// The star lands before the shortest wind-up of any ordinary body can: the five exports' defaults and every
		// scene's override, against the fall, the step the edge is seen late, and a margin.
		var exports = new[] { "CastWindUpSeconds", "LungeWindUpSeconds", "SlamTelegraphSeconds", "SummonWindUpSeconds", "FuseSeconds" };
		float shortest = new[]
		{
			DefaultOf<RangedEnemy>(e => e.CastWindUpSeconds), DefaultOf<LungerEnemy>(e => e.LungeWindUpSeconds),
			DefaultOf<SlammerEnemy>(e => e.SlamTelegraphSeconds), DefaultOf<SummonerEnemy>(e => e.SummonWindUpSeconds),
			DefaultOf<ExploderEnemy>(e => e.FuseSeconds),
		}.Min();
		string shortestWhere = "a default";
		DirAccess dir = DirAccess.Open("res://scenes");
		if (dir != null)
		{
			var pattern = new Regex(@"^(" + string.Join("|", exports) + @") = ([0-9.]+)", RegexOptions.Multiline);
			foreach (string file in dir.GetFiles())
			{
				if (!file.EndsWith(".tscn", StringComparison.Ordinal))
					continue;
				string text = Godot.FileAccess.GetFileAsString("res://scenes/" + file);
				foreach (Match m in pattern.Matches(text))
				{
					if (float.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v)
						&& v < shortest)
					{
						shortest = v;
						shortestWhere = $"{file}'s {m.Groups[1].Value}";
					}
				}
			}
		}
		if (shortest < Starfall.ForeseenFallSeconds + 2f * Dt + 0.1f)
			warnings.Add($"The Quatrains: the shortest wind-up ({shortestWhere}, {shortest:0.00} s) is not long enough for a foreseen star "
				+ $"({Starfall.ForeseenFallSeconds:0.00} s) to land before it can finish.");
	}

	// An export's default, read off a body made for the purpose and freed at once (never in the tree).
	private static float DefaultOf<T>(Func<T, float> read) where T : Node, new()
	{
		var body = new T();
		float v = read(body);
		body.Free();
		return v;
	}
}
