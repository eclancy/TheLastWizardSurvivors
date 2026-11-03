using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// The wind-up clock every telegraphed enemy attack runs on: wait out a cooldown, plant for a
/// visible wind-up, then resolve exactly once.
/// </summary>
/// <remarks>
/// This shape was written twice before it was named - <c>BossEnemy</c>'s ground slam had its own
/// pair of float timers, and the ranged caster needed the same pair with different numbers. It is
/// deliberately not a Node: an attack pattern is bookkeeping its owner ticks, not something with a
/// place in the scene tree, and a swarm-heavy scene should not pay for a child node per attacker.
///
/// The wind-up is the whole contract. An enemy attack that lands without one is indistinguishable
/// from damage arriving at random, so <see cref="Beat.Started"/> is the caller's cue to plant the
/// enemy and draw its tell, and <see cref="Beat.Resolved"/> only ever fires after the player has
/// had <see cref="WindUpSeconds"/> to react.
/// </remarks>
public sealed class AttackTelegraph
{
	public enum Beat
	{
		/// <summary>On cooldown, or holding fire because the caller said not yet.</summary>
		Idle,
		/// <summary>The wind-up began this frame: plant the enemy and play its tell.</summary>
		Started,
		/// <summary>Mid wind-up. The attack has not landed and can still be read.</summary>
		WindingUp,
		/// <summary>The attack lands this frame, once.</summary>
		Resolved
	}

	/// <summary>Seconds between the start of one attack and the next becoming available.</summary>
	public float IntervalSeconds { get; set; }

	/// <summary>Seconds the enemy spends visibly winding up before the attack lands.</summary>
	public float WindUpSeconds { get; set; }

	/// <summary>
	/// How much longer every foreseeable wind-up shows this run (Foresight, Nostradamus's passive:
	/// .ai/chapter-wizards.md 4.10). 1 unless he is the one playing; every Player._Ready puts it back, so it is
	/// a run's and never the process's. Read on the frame a wind-up STARTS, so a passive taken mid-swing cannot
	/// shorten one already shown.
	/// </summary>
	public static float ForesightScale { get; set; } = 1f;

	/// <summary>
	/// Whether <see cref="ForesightScale"/> reaches this clock. The ordinary bodies' attacks set it (the caster's
	/// bolt, the lunger's leap, the slammer's ring, the exploder's fuse, the summoner's call). A boss's clocks do
	/// not: their tells are their fights' timing - a burrow that must outlast the tell it places, a charge whose
	/// tell is a share of its run - and a hazard placed on the ground is the ground's.
	/// </summary>
	public bool Foreseeable { get; set; }

	private float cooldown;
	private float windUpRemaining;
	// The wind-up being shown now, as it started: WindUpSeconds, lengthened by Foresight where it reaches.
	private float windUpTotal;

	public AttackTelegraph(float intervalSeconds, float windUpSeconds)
	{
		IntervalSeconds = Mathf.Max(0.05f, intervalSeconds);
		WindUpSeconds = Mathf.Max(0f, windUpSeconds);
		// Start on a full cooldown so nothing attacks on the frame it spawns, which would land
		// before the player has even seen it arrive.
		cooldown = IntervalSeconds;
	}

	public bool IsWindingUp => windUpRemaining > 0f;

	/// <summary>0 the frame the wind-up starts, 1 the frame before it resolves. For drawing tells.</summary>
	public float WindUpProgress => windUpTotal > 0f
		? 1f - Mathf.Clamp(windUpRemaining / windUpTotal, 0f, 1f)
		: 1f;

	/// <summary>Seconds until the wind-up being shown lands; 0 while none is.</summary>
	public float WindUpRemaining => Mathf.Max(0f, windUpRemaining);

	/// <summary>
	/// Ends a wind-up that has not landed WITHOUT landing it, and starts the whole cooldown over: The Quatrains'
	/// star (.ai/chapter-wizards.md 4.10), which falls on whatever begins one, "before it can finish". True when
	/// there was a wind-up to end.
	/// </summary>
	/// <remarks>
	/// The one way a wind-up may stop short, and it can only take an attack away, never bring one sooner: the
	/// cancelled attack never resolves, and the next must wait out a full <see cref="IntervalSeconds"/> and then
	/// a full wind-up of its own, shown from its first frame. <see cref="Prime"/> after it skips only the wait,
	/// as it always has. RegressionChecks.ValidateQuatrains runs a clock through it on paper.
	/// </remarks>
	public bool Cancel()
	{
		if (windUpRemaining <= 0f)
			return false;
		windUpRemaining = 0f;
		cooldown = IntervalSeconds;
		return true;
	}

	/// <summary>
	/// Advances the clock one frame. <paramref name="wantsToAttack"/> is the caller's gate - range,
	/// line of sight, whatever the attack needs - and is only consulted at the moment a wind-up
	/// would start. Once started, a wind-up always resolves: an attack that could be cancelled by
	/// stepping away mid-tell would teach the player that the tell means nothing. (<see cref="Cancel"/> is
	/// the one exception, and it is the player's spell that calls it, never the attacker's range.)
	/// </summary>
	/// <summary>
	/// Clears the remaining cooldown so the next <see cref="Tick"/> begins a wind-up at once.
	/// </summary>
	/// <remarks>
	/// For an attack whose timing is decided by something other than this clock - a hazard placed on
	/// the ground to land once, or an attack a phase change should open with. Without it the caller
	/// has to pass a near-zero interval, which works by accident and reads as a typo.
	///
	/// It cannot skip a wind-up, only the wait before one, so it can never be used to land an
	/// untelegraphed hit.
	/// </remarks>
	public void Prime()
	{
		if (windUpRemaining <= 0f)
			cooldown = 0f;
	}

	public Beat Tick(float delta, bool wantsToAttack)
	{
		if (windUpRemaining > 0f)
		{
			windUpRemaining -= delta;
			if (windUpRemaining > 0f)
				return Beat.WindingUp;

			windUpRemaining = 0f;
			return Beat.Resolved;
		}

		cooldown -= delta;
		if (cooldown > 0f || !wantsToAttack)
			return Beat.Idle;

		// The cooldown is only consumed when the attack actually begins, so an enemy that spent the
		// whole cooldown out of range fires the instant it closes rather than waiting a second time.
		cooldown = IntervalSeconds;
		if (WindUpSeconds <= 0f)
			return Beat.Resolved;

		windUpTotal = WindUpSeconds * (Foreseeable ? Mathf.Max(1f, ForesightScale) : 1f);
		windUpRemaining = windUpTotal;
		return Beat.Started;
	}
}
