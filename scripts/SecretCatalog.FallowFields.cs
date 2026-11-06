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
			// Guarded: both guards down frees the gate; then approach each trapped companion inside.
			Vault(PinfoldId, Stage, "The Pinfold", SecretVerb.Guarded, "familiar", "Familiar",
				seen: "Drag marks and splayed paw prints lead to a stone pen. A hound, a cat and a bird pace behind its magical seal, their color drained.",
				found: "The Hayward's pound held three companions as pale ghosts. Its guards fell, the binding broke, and you freed each of them. Laelaps runs free, and Familiar is yours.") with
			{
				IsBuilt = true,
				CreateSite = () => new global::Pinfold(),
				Footprint = 1360f,
				CharacterUnlockId = "laelaps",
				RepeatReward = "The three companions are free again; a chest waits in the pound",
			},
		};
	}

	public const string PinfoldId = "pinfold";
}
