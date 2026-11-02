namespace WizardSurvivors.scripts;

public static partial class StageBestiary
{
	/// <summary>
	/// Chapter 13, the Throne (stage_13): his household guard. Boss: him, after the procession.
	/// </summary>
	/// <remarks>
	/// NO CAST OF ITS OWN, ON PURPOSE (.ai/endgame-castle.md 6.3): "the minions are his household guard - the
	/// Court's Heavy, Wall and Ranged, and the bridge's knights - on the end-of-run spawn curve". They come
	/// home with him, so every creature here is another chapter's, and the roster says why
	/// (<see cref="ChapterBestiary.BorrowedCastBecause"/>) rather than listing them as a to-do: the bestiary
	/// line would otherwise ask for art this chapter must never have. Piece C4 chose a roster over
	/// suppressing the trickle (11.5): the guard then arrives by the ordinary spawner, on the pinned
	/// end-of-run curve (ThroneRules), with nothing in Node2DGame to change.
	///
	/// THE ROLES. The Household Guard is the hall's body - the Court's heavy, and here also the Bulk slot, so
	/// anything that asks for "an ordinary body" gets a guard (only tools do in this chapter). The Empty
	/// Armour is the wall, the Falconer the back line, the Gate Knight the charger, each on its own roll as
	/// everywhere. The rest are absent: a throne room has no hounds, candelabra, censers, ushers or
	/// huntsmen in it. The miniboss slot must name a body (the table's validator holds every chapter to
	/// it), so it names the Court's Chamberlain - and ThroneRules switches the miniboss clock off, so he
	/// never comes.
	///
	/// THE TABLE is the Court's two bands for these two bodies, at the Court's own health for them (a
	/// player meets the same Household Guard and it takes the same hits), in the Court's proportion:
	/// 0.38 x 2.2 + 0.62 x 1.4 = 1.70. Heavier than any chapter's table (the Court's 1.37, the Emberdeep's
	/// 1.28): the room is his guard and nothing lighter. The Throne's run is measured on its own line
	/// (6.6), never in .ai/run-pacing.md's tables.
	/// </remarks>
	private static class Throne
	{
		private const string Stage = "stage_13";

		internal static ChapterBestiary Build()
		{
			BestiarySlot guard = Own(BestiaryRole.Heavy, Court.Guard);
			BestiarySlot armour = Own(BestiaryRole.Wall, Court.Armour);

			return new ChapterBestiary
			{
				StageId = Stage,
				BorrowedCastBecause = "his household guard comes home with him (.ai/endgame-castle.md 6.3)",
				Slots = new[]
				{
					Own(BestiaryRole.Bulk, Court.Guard),
					Absent(BestiaryRole.Fast, "No hounds in the throne room: the household guard only (6.3)."),
					guard,
					armour,
					Own(BestiaryRole.Ranged, Court.Falconer),
					Absent(BestiaryRole.Turret, "Nothing stands lit in his hall but his own sconces."),
					Own(BestiaryRole.Charger, Bridge.Knight),
					Absent(BestiaryRole.Exploder, "The household guard only (6.3)."),
					Absent(BestiaryRole.Slammer, "The household guard only (6.3)."),
					Absent(BestiaryRole.Summoner, "The household guard only (6.3)."),
					// Named because the slot must be; ThroneRules turns the miniboss clock off.
					Own(BestiaryRole.MiniBoss, Court.Chamberlain),
				},
				Table = new[]
				{
					Band(guard, 0.38f, 2.2f),
					Band(armour, 0.62f, 1.4f),
				},
			};
		}
	}
}
