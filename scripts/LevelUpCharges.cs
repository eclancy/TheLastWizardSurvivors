// Global namespace, matching LevelUpOption and the rest of LevelUpMenu.Models.cs.
// The namespaces in this project are inconsistent by history; match the file you sit beside.
/// <summary>
/// The level-up charges a run is carrying, handed to <c>LevelUpMenu</c> so it can draw the bar.
/// </summary>
/// <remarks>
/// Inspired by the potion system in Halls of Torment: a small pool of one-shot tools that let the
/// player argue with the level-up roll instead of only accepting it. Mechanics reference only.
///
/// **Both are a pool for the whole run** (Player.RerollsRemaining, Player.BansRemaining). Rerolls used to
/// refill at every level-up, which at the shop's cap was about 160 a run and no limit at all; since
/// 2026-10-31 (Eric: "rerolls/bans/etc should be per run, not per level up") a run starts with two per
/// level of Fate Fracture and never gets more, so a reroll is a decision as a ban already was
/// (.ai/difficulty-curve.md, section 7).
///
/// Both are bought in the shop, so a player who never buys any sees exactly the screen the game
/// shipped with. There were four: Save (defer a level-up for two picks at the next) and Augury (name
/// a spell the next offer must hold) were removed on 2026-10-28 ("remove save and augury mechanics"),
/// and the shop levels bought of them refunded (SaveData schema 18).
/// </remarks>
public sealed class LevelUpCharges
{
	/// <summary>What is left of the run's rerolls (Player.RerollsRemaining).</summary>
	public int Rerolls { get; set; } = 0;

	/// <summary>Removes a spell from the offer pool for the rest of the run.</summary>
	public int Bans { get; set; } = 0;

	public bool HasAny => Rerolls > 0 || Bans > 0;
}
