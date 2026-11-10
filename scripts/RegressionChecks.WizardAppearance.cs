using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE APPEARANCE OF A WIZARD FREED IN THE RUN (scripts/WizardAppearance.cs, in-flight 83). What a build cannot see and a
// run would show only as a freeing with no one in it, or two: every wizard a deed frees has a sprite to step out in or a
// side event that shows him itself, the list of those events names only real events that free someone, and the beat
// keeps the cinematic rules' holds before and after.
public static partial class RegressionChecks
{
	private static void ValidateWizardAppearance(List<string> warnings)
	{
		var freeingSites = AchievementDefinitions.All
			.Where(d => !string.IsNullOrWhiteSpace(d.SiteId) && !string.IsNullOrWhiteSpace(d.CharacterUnlockId))
			.Select(d => d.SiteId)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		foreach (string site in WizardAppearance.SitesWithOwnBeat)
		{
			if (!freeingSites.Contains(site))
				warnings.Add($"Wizard appearance: '{site}' is listed as showing the wizard it frees, but no side event there frees anyone; the list has gone stale.");
		}

		// Every wizard a deed frees, unless his event shows him: the beat needs his own sprite to step out in.
		foreach (AchievementDefinition deed in AchievementDefinitions.All.Where(d => !string.IsNullOrWhiteSpace(d.CharacterUnlockId)))
		{
			if (WizardAppearance.HasOwnBeat(deed.Id))
				continue;
			CharacterData wizard = CharacterRoster.GetAll().FirstOrDefault(c => c != null
				&& c.Id.Equals(deed.CharacterUnlockId, StringComparison.OrdinalIgnoreCase));
			if (wizard == null)
			{
				// A god whose piece has not merged is no wizard yet; the far shore's own checks carry him.
				if (!UnlockCatalog.IsGod(deed.CharacterUnlockId))
					warnings.Add($"Wizard appearance: '{deed.Id}' frees '{deed.CharacterUnlockId}', who is not in CharacterRoster, so nobody steps out.");
				continue;
			}
			bool frames = wizard.Frames != null && wizard.Frames.HasAnimation("idle");
			if (!frames && !CharacterVisuals.TryBuildIdleFrames(wizard.Portrait, out _))
				warnings.Add($"Wizard appearance: {wizard.Name} has no idle frames and no portrait to build them from, so his appearance draws nothing.");
			if (string.IsNullOrWhiteSpace(wizard.Name))
				warnings.Add($"Wizard appearance: '{wizard.Id}' has no Name for the line over him.");
		}

		// The cinematic rules (.ai/cutscenes.md): a hold before he steps out, and one after he has gone; a moment, not a scene.
		float before = WizardAppearance.LightSeconds, after = WizardAppearance.FadeSeconds, total = WizardAppearance.TotalSeconds;
		if (before < 0.4f || after < 0.4f)
			warnings.Add($"Wizard appearance: the light stands {before:0.##} s before he steps out and {after:0.##} s after he has gone; each hold is at least 0.4 s (cinematic rules 3 and 4).");
		if (total < 2.5f || total > 5f)
			warnings.Add($"Wizard appearance: the beat lasts {total:0.##} s; it is a couple of seconds, 2.5 to 5.");
	}
}
