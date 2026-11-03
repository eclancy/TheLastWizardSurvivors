using System;

namespace TheOneHeMissed.scripts;

public static partial class SecretCatalog
{
	/// <summary>
	/// The Sketchbook (stage_8) holds no secrets (Eric, 2026-11-02: "the sketchbook should not have any
	/// unlocks or any achievements"). Every secret is a deed, so this stays empty;
	/// RegressionChecks.ValidateSketchbookHoldsNothing reports one placed here.
	/// </summary>
	/// <remarks>
	/// .ai/stage-secrets.md 6.9 keeps the shelved plan (the Dog's Find, Colour It In, the Kids' Page).
	/// </remarks>
	private static class Sketchbook
	{
		internal static SecretDefinition[] Build() => Array.Empty<SecretDefinition>();
	}
}
