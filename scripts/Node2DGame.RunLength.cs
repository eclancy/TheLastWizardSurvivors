using Godot;
using TheOneHeMissed.scripts;

// A CHAPTER'S OWN RUN LENGTH (.ai/run-pacing.md section 12). Eric, 2026-11-08, after the far sky's element waiver:
// "okay lets let the waiver land ... still make the far shore longer". So the far shore's chapters run twenty
// minutes, the boss at 20:00, and every road chapter stays at fifteen (StageCatalog.RunSeconds, read through
// StageCatalog.VictorySecondsFor).
//
// Two things follow the length, and both are settled here, once, as the run starts:
//
//   1. THE CLOCK. TimerVictorySeconds becomes the chapter's, so the boss arrives, the HUD timer stops and the results
//      screen counts to it ("Boss at 20:00"), and RunPace = length / ReferenceRunSeconds stretches the pace clock: every
//      paced number (spawn growth, health, elites, the Warden, the hints, the XP multiplier) lands at the same SHARE of
//      a twenty-minute run as of the fifteen-minute one it was tuned on. What is real seconds stays real (a burst, a
//      grace, the formation rhythm, the swarm's rise and fall).
//
//   2. THE XP CURVE. Levels are paid by kills, and the kill rate is the spawner's, whose rates are all read at a pace
//      minute and all paid in real seconds (the trickle's gap, the burst window, the formation rhythm). So a run that
//      is 4/3 as long spawns - and a build that keeps up kills - 4/3 as much at every share of it, and banks 4/3 the
//      XP: left alone, a far-shore run would reach level 17 and 21 a third of the way sooner by share, and meet its
//      boss about four levels richer. Every level costs RunPace times as much instead (13 + 60 a level on the far
//      shore, against 10 + 45), so the same level comes at the same share and the boss meets the build of the same
//      strength. This is run-pacing.md's strategy B (keep the gates, move the step) solved for a stretched run rather
//      than a shortened one. Read from the chapter, not from TimerVictorySeconds, so a tool's short clock (_BossProbe)
//      or the Throne's 0 never touches it; and on the road, where the scale is exactly 1, the curve is never written.
//
// What does NOT follow, and is left so on purpose: the carriers that come a fixed number of times a run (elites,
// Wardens, chests) pay the same count across the longer run, so in levels each is worth 3/4 of what it is
// on the road - a few percent of a run's XP; the Arcane Energy a minute pays the four extra minutes; and the
// achievements that name real minutes (Survivor's ten, Untouchable's three) mean the same minutes everywhere.
public partial class Node2DGame
{
	/// <summary>TimerVictorySeconds before the run has read its chapter's: "the chapter's own" (ResolveRunLength).</summary>
	public const float ChapterRunLength = -1f;

	/// <summary>The chapter's own run length in real seconds (StageCatalog.VictorySecondsFor), whatever a tool set the clock to.</summary>
	public float ChapterRunSeconds { get; private set; } = StageCatalog.RoadRunSeconds;

	/// <summary>
	/// What every level costs on this chapter against the road's: its run length over the reference run's. 1 on the road
	/// (the curve untouched), 4/3 on the far shore.
	/// </summary>
	public float XpCurveScale => ChapterRunSeconds / ReferenceRunSeconds;

	// First in _Ready once the player is known, before the chapter's rules (the Throne sets the clock to 0 in its
	// RunStarted) and before anything reads the pace.
	private void ResolveRunLength()
	{
		StageDefinition? stage = StageCatalog.GetByIndex(Global.SelectedStageIdx);
		ChapterRunSeconds = StageCatalog.VictorySecondsFor(stage?.Id);
		bool toolClock = TimerVictorySeconds != ChapterRunLength;
		if (!toolClock)
			TimerVictorySeconds = ChapterRunSeconds;

		if (player != null && !Mathf.IsEqualApprox(XpCurveScale, 1f))
		{
			(player.XpFirstLevelCost, player.XpCostPerLevel) = XpCurveForRun(player.XpFirstLevelCost, player.XpCostPerLevel, ChapterRunSeconds);
			player.XPToNextLevel = player.CalculateXPForLevel(player.CurrentLevel);
		}

		if (!Mathf.IsEqualApprox(ChapterRunSeconds, ReferenceRunSeconds) || toolClock)
			GD.Print($"Node2DGame: {stage?.DisplayName ?? "this chapter"} runs {ChapterRunSeconds / 60f:0.#} minutes"
				+ (toolClock ? $" (the clock set to {TimerVictorySeconds:0.#} s by a tool)" : "")
				+ $"; XP curve {player?.XpFirstLevelCost ?? 0} + {player?.XpCostPerLevel ?? 0} a level.");
	}

	/// <summary>
	/// The road's XP curve (<paramref name="first"/> + <paramref name="step"/> a level) for a run of
	/// <paramref name="runSeconds"/>: both terms times the run's length over the reference run's, rounded. The same
	/// numbers back on the road. Static so RegressionChecks.ValidateRunLengths can hold it without a game node.
	/// </summary>
	public static (int First, int Step) XpCurveForRun(int first, int step, float runSeconds)
	{
		float scale = runSeconds / ReferenceRunSeconds;
		if (Mathf.IsEqualApprox(scale, 1f))
			return (first, step);
		return (Mathf.RoundToInt(first * scale), Mathf.RoundToInt(step * scale));
	}
}
