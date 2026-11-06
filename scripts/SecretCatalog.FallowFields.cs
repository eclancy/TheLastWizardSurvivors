namespace TheOneHeMissed.scripts;

public static partial class SecretCatalog
{
	/// <summary>
	/// Chapter 2, the Fallow Fields (stage_9): .ai/achievements-and-secrets.md 8.2. Its side event is the
	/// well (defend).
	/// </summary>
	/// <remarks>
	/// Built: FF1 the Pinfold, the vault (guarded; 2026-10-30). To build: FF2 the Last Sheaf, FF3 the Plough
	/// in the Furrow, FF4 the Tithe Barn (the keepsake - carry the sheaf) and FF5 the Scarecrow's Post (its
	/// Haymaker pair is why the Pinfold's guards are a Rick and a Haymaker, not two Haymakers).
	/// </remarks>
	private static class FallowFields
	{
		private const string Stage = "stage_9";

		internal static SecretDefinition[] Build() => new[]
		{
			// Guarded: both guards down frees the gate; then walk INSIDE to claim the central essence.
			Vault(PinfoldId, Stage, "The Pinfold", SecretVerb.Guarded, "familiar", "Familiar",
				seen: "Drag marks and splayed paw prints lead to a stone pen. A magical binding holds its gate; a creature's light stirs inside.",
				found: "The Hayward's pound, and a creature's light he kept in it. Its guards fell, the binding broke, and you approached the light inside.") with
			{
				IsBuilt = true,
				CreateSite = () => new global::Pinfold(),
				RepeatReward = "Its guards fell again, and a chest was in the pound",
			},
		};
	}

	public const string PinfoldId = "pinfold";
}
