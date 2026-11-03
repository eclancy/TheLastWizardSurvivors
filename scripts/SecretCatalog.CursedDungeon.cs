namespace TheOneHeMissed.scripts;

public static partial class SecretCatalog
{
	/// <summary>
	/// The Cursed Dungeon (stage_1), in the castle now: .ai/achievements-and-secrets.md 8.5. Its side event
	/// is the Gaoler's key (carry). The maze: every site snaps to a cell, and no secret ever moves a wall.
	/// </summary>
	/// <remarks>
	/// Built: D5 the Cold Forge, the vault (cull; 2026-10-30, in the Dungeon as it is now - the move into
	/// the castle changes nothing about it), and D7 the Oubliette (untouched; 2026-11-03, the earlier reveal
	/// of Nimue). To build: D1 Chalk Marks, D2 the Alarm Bell, D3 the Hollow Flagstone (strike, on
	/// Breakable), D4 the Gaoler's Ledger (the keepsake - guarded) and D6 the Empty Cell.
	/// </remarks>
	private static class CursedDungeon
	{
		private const string Stage = "stage_1";

		internal static SecretDefinition[] Build() => new[]
		{
			// Cull: forty kills inside the hearth ring in front of the anvil light the coals until the forge
			// roars (ColdForge.cs). Moved to the nearest hall room; the hearth is solid ground.
			Vault(ColdForgeId, Stage, "The Cold Forge", SecretVerb.Cull, "molten_shard", "Molten Shard",
				seen: "A dead smithy in the gaol: a cold hearth, an anvil, and a warm glint on the anvil's horn.",
				found: "The forge that made the shackles. Every kill on its hearth set an ember in the coals until it roared.") with
			{
				IsBuilt = true,
				// Placed by a corridor cell, which is all the maze's ground is: ColdForge.LayOut then moves it to
				// the nearest hall and reserves the vault's footprint there. Asked for the vault's 420 px here it
				// could never fit a corridor, and the placer fell back and warned on every seed.
				Footprint = CorridorFootprint,
				// ...and only at a cell with a hall beside it, so that move is one step and never fails.
				RoomAt = (game, at) => global::ColdForge.HallBeside(game, at) != null,
				CreateSite = () => new global::ColdForge(),
				RepeatReward = "It roared again, and gave up a chest",
			},
			// Untouched: five seconds on the grate without a blow, and a voice comes up the shaft (Oubliette.cs). Its
			// find is what turns Nimue's card (UnlockCatalog's RevealSecretId; .ai/endgame-castle.md decision 11) -
			// a reveal, never the wizard herself, who is the Throne's to free (stage-secrets section 1). Pays the
			// page and nothing else: the 150 Arcane Energy D7 held was for a road with no Nimue on it.
			new SecretDefinition
			{
				Id = OublietteId,
				StageId = Stage,
				DisplayName = "The Oubliette",
				SeenLine = "A grate in the floor of a dead end, and a cold draught rising through it.",
				FoundLine = "A shaft under the gaol's floor, too deep for any light: the cell for someone meant to be forgotten.",
				Verb = SecretVerb.Untouched,
				RunReward = "Someone else is still held here",
				RewardText = "Tells of someone else held",
				Placement = SecretPlacement.Mid,
				// The cell's middle: a corridor's width, as the forge is placed (Oubliette.LayOut keeps the doorway).
				Footprint = CorridorFootprint,
				// ...and only a dead end: a cell with walls on three sides.
				RoomAt = (game, at) => global::Oubliette.DeadEndAt(game, at) != null,
				CreateSite = () => new global::Oubliette(),
			},
		};
	}

	public const string ColdForgeId = "cold_forge";
	public const string OublietteId = "oubliette";
}
