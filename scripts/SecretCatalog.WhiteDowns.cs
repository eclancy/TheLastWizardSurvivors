namespace TheOneHeMissed.scripts;

public static partial class SecretCatalog
{
	public const string HoundInTheHillId = "hound_in_the_hill";
	public const string HartInTheHillId = "hart_in_the_hill";

	/// <summary>
	/// Far Shore 1, the White Downs (stage_14, piece F3): .ai/second-half.md 4.2 and 4.3 - "a hound and a hart cut in
	/// the hillsides", and two secrets. Its side event is the Silver Hind (stalk), so neither secret stalks.
	/// </summary>
	/// <remarks>
	/// What a far chapter's secret may pay (.ai/stage-secrets.md section 3, and 4.3: never a spell, wizard or boon; a
	/// keepsake inside its budget). Every spell is earned by the time the far shore opens and the four found levels of
	/// issue #20 are each spoken for on the road, so the keepsake pays Arcane Energy, the other kind a keepsake may.
	/// Built: the Hound in the Hill (the anchor - linger) and the Hart in the Hill (the keepsake - gather). Both are
	/// chalk figures cut in the down, and their far tell is the same: fresh chalk spilled where the turf was cut,
	/// thickening toward them - the old chalk of the ground stays well under it (tools/art/detail_downs.py).
	/// </remarks>
	private static class WhiteDowns
	{
		private const string Stage = "stage_14";

		internal static SecretDefinition[] Build() => new[]
		{
			// Linger in the hound's eye (HoundInTheHill.cs): his pace for a minute, every run it is found.
			new SecretDefinition
			{
				Id = HoundInTheHillId,
				StageId = Stage,
				DisplayName = "The Hound in the Hill",
				FoundLine = "A hound cut in the chalk, the one that never failed to catch what it ran after. Stand in its eye and it lends you its pace.",
				SeenLine = "A hound cut into the chalk of the down, running. Its eye is a hollow, and something in it is warm.",
				Verb = SecretVerb.Linger,
				RunReward = "Its pace: 25% faster for a minute",
				Placement = SecretPlacement.Anchor,
				Footprint = global::HoundInTheHill.Footprint,
				OpenRadius = global::HoundInTheHill.OpenRadius,
				CreateSite = () => new global::HoundInTheHill(),
			},
			// Gather the knots of turf off the hart's lines (HartInTheHill.cs): the keepsake.
			new SecretDefinition
			{
				Id = HartInTheHillId,
				StageId = Stage,
				DisplayName = "The Hart in the Hill",
				FoundLine = "A hart cut in the chalk far out on the down, grown over since the moon went out. Every knot of turf lifted, it shines again.",
				SeenLine = "A great hart cut into a far hillside, grown over: the turf has crept back across its lines in knots.",
				Verb = SecretVerb.Gather,
				IsKeepsake = true,
				RunReward = "Healed by half",
				MetaReward = SecretMetaReward.ArcaneEnergy,
				MetaAmount = MaxKeepsakeArcane,
				Placement = SecretPlacement.Keepsake,
				Footprint = global::HartInTheHill.Footprint,
				OpenRadius = global::HartInTheHill.OpenRadius,
				CreateSite = () => new global::HartInTheHill(),
			},
		};
	}
}
