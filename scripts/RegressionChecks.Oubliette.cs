using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// D7, THE OUBLIETTE (.ai/endgame-castle.md 4.4 and decision 11): the Dungeon's secret whose find turns Nimue's card,
// and the earlier-reveal rule it brought (UnlockDefinition.RevealSecretId). Everything here fails silently in play: a
// reveal secret that is misnamed, unbuilt, in a later chapter than the fallback or paying the wizard herself is a card
// that turns at the Throne as it always did, and nobody would know the grate had stopped mattering. Every line is a
// warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	private static void ValidateOubliette(List<string> warnings)
	{
		foreach (UnlockDefinition definition in UnlockCatalog.All.Where(d => !string.IsNullOrWhiteSpace(d.RevealSecretId)))
			ValidateRevealSecret(definition, warnings);

		SecretDefinition oubliette = SecretCatalog.Get(SecretCatalog.OublietteId);
		if (oubliette == null)
		{
			warnings.Add("Oubliette: SecretCatalog has no 'oubliette' (D7, the Cursed Dungeon's).");
			return;
		}
		if (!oubliette.IsBuilt || !oubliette.StageId.Equals("stage_1", StringComparison.OrdinalIgnoreCase))
			warnings.Add($"Oubliette: it must be built and in the Cursed Dungeon (stage_1); it is {(oubliette.IsBuilt ? "built" : "unbuilt")} in '{oubliette.StageId}'.");
		// Every run of the Dungeon has one, five to nine chunks out (SecretPlacement.Mid), and only in a dead end.
		if (oubliette.Placement != SecretPlacement.Mid || oubliette.RoomAt == null || oubliette.CreateSite == null)
			warnings.Add($"Oubliette: placed as {oubliette.Placement}{(oubliette.RoomAt == null ? " with no dead-end test (RoomAt)" : "")}; it is a mid-band site in a dead end in every run (SecretPlacement.Mid, Oubliette.DeadEndAt).");
		if (oubliette.Verb != SecretVerb.Untouched)
			warnings.Add($"Oubliette: its verb is {oubliette.Verb}; the design's is untouched (five seconds on the grate without a blow).");
		// It reveals; it pays nothing beyond the page. The wizard is the Throne's to free (stage-secrets section 1), and
		// the 150 Arcane Energy D7 held was for a road with no Nimue on it.
		if (oubliette.MetaReward != SecretMetaReward.PageOnly || oubliette.IsKeepsake || oubliette.IsVault)
			warnings.Add($"Oubliette: it pays {oubliette.MetaReward}{(oubliette.IsKeepsake ? " as a keepsake" : "")}{(oubliette.IsVault ? " as a vault" : "")}; its find is a reveal and the page, nothing more.");

		// The one it turns: exactly one wizard, Nimue, and the Throne still turns her for a save that never finds it.
		List<UnlockDefinition> kept = UnlockCatalog.All
			.Where(d => d.RevealSecretId.Equals(SecretCatalog.OublietteId, StringComparison.OrdinalIgnoreCase)).ToList();
		if (kept.Count != 1 || !kept[0].Id.Equals("nimue", StringComparison.OrdinalIgnoreCase))
			warnings.Add($"Oubliette: its find must turn exactly one card, Nimue's; it turns {(kept.Count == 0 ? "none" : string.Join(", ", kept.Select(d => d.Id)))}.");
		else if (!kept[0].RevealStageId.Equals("stage_13", StringComparison.OrdinalIgnoreCase))
			warnings.Add($"Oubliette: Nimue's reveal chapter is '{kept[0].RevealStageId}'; the Throne stays the fallback for a save that never stands on the grate (decision 11).");
		if (Oubliette.KeptBelow(SecretCatalog.OublietteId)?.Id is not "nimue")
			warnings.Add("Oubliette: Oubliette.KeptBelow does not find Nimue, so the voice would never answer.");

		// The ask: no hold over five seconds (stage-secrets section 8, rule 3), and a grate the feet can find.
		float hold = Oubliette.HoldSeconds;
		if (hold <= 0f || hold > 5f)
			warnings.Add($"Oubliette: it asks {hold} s on the grate; a secret holds for five at most.");

		// The voice: allowlisted, generated, and the find waits it out - the found sting is the Critical meta_unlock,
		// which would duck the voice if it landed on top of it.
		if (!SfxCatalog.AllNames.Contains(SfxCatalog.SecretVoiceBelow))
			warnings.Add($"Oubliette: '{SfxCatalog.SecretVoiceBelow}' is not in SfxCatalog's allowlist, so the voice never plays.");
		string voicePath = SfxCatalog.PathFor(SfxCatalog.SecretVoiceBelow);
		AudioStream voice = ResourceLoader.Exists(voicePath) ? GD.Load<AudioStream>(voicePath) : null;
		if (voice == null)
			warnings.Add($"Oubliette: {voicePath} is missing or not imported (python tools/audio/build.py secret_voice_below, then import).");
		else if (voice.GetLength() > Oubliette.VoiceSeconds + 0.05 || voice.GetLength() < Oubliette.VoiceSeconds - 0.4)
			warnings.Add($"Oubliette: the voice is {voice.GetLength():0.00} s but the find waits {Oubliette.VoiceSeconds} s for it (Oubliette.VoiceSeconds); the sting must follow it, not cut it.");

		// The art (tools/art/secret_dungeon.py): the grate 80 px with its curb, so GrateReach (44) reaches its edge,
		// and the four pieces of the far tell.
		const string grate = "res://assets/bonelight/world/dungeon/oubliette-grate.png";
		Texture2D grateTexture = ResourceLoader.Exists(grate) ? GD.Load<Texture2D>(grate) : null;
		if (grateTexture == null)
			warnings.Add($"Oubliette: {grate} is missing or not imported (python tools/art/secret_dungeon.py, then import).");
		else if (grateTexture.GetSize() != new Vector2(80f, 80f))
			warnings.Add($"Oubliette: the grate is {grateTexture.GetSize()}, expected 80x80 - its curb is what GrateReach measures to.");
		foreach (string piece in new[] { "puddle-1", "puddle-2", "rime-1", "rime-2" })
		{
			string path = $"res://assets/bonelight/world/dungeon/{piece}.png";
			if (!ResourceLoader.Exists(path))
				warnings.Add($"Oubliette: its far tell's {path} is missing or not imported.");
		}
	}

	// A wizard turned by a secret's find as well as by his reveal chapter (UnlockDefinition.RevealSecretId).
	private static void ValidateRevealSecret(UnlockDefinition definition, List<string> warnings)
	{
		string label = $"Unlock '{definition.Id}'";
		if (definition.Kind != UnlockKind.Character || !definition.IsSecret)
		{
			warnings.Add($"{label} names a reveal secret '{definition.RevealSecretId}' but is not a secret wizard, so nothing reads it.");
			return;
		}
		if (string.IsNullOrWhiteSpace(definition.RevealStageId))
			warnings.Add($"{label} is revealed by the secret '{definition.RevealSecretId}' alone; keep a reveal chapter beside it, the fallback for a save that never finds it.");
		SecretDefinition secret = SecretCatalog.Get(definition.RevealSecretId);
		if (secret == null)
		{
			warnings.Add($"{label} is revealed by the secret '{definition.RevealSecretId}', which SecretCatalog does not define, so only its chapter turns the card.");
			return;
		}
		if (!secret.IsBuilt)
			warnings.Add($"{label} is revealed by '{secret.DisplayName}', which is not built yet, so only its chapter turns the card.");
		// A reveal, never the freeing: the secret's deed must not be the one that pays him (stage-secrets section 1).
		if (definition.SourceId.Equals(AchievementDefinitions.SecretDeedId(secret.Id), StringComparison.OrdinalIgnoreCase))
			warnings.Add($"{label} is FREED by the secret '{secret.Id}'; a secret may reveal a wizard, never pay one.");
		// The EARLIER reveal: the secret's chapter must come before the reveal chapter on the road, or it never is.
		int secretAt = StageCatalog.CampaignPositionOf(secret.StageId);
		int stageAt = StageCatalog.CampaignPositionOf(definition.RevealStageId);
		if (secretAt >= 0 && stageAt >= 0 && secretAt >= stageAt)
			warnings.Add($"{label} is revealed by '{secret.DisplayName}' in '{secret.StageId}', which is not before its reveal chapter '{definition.RevealStageId}' - it would never be the earlier reveal.");
		// The derivation, on paper: a brand-new save with only that secret found reads him revealed and still locked;
		// one without it, not revealed.
		var found = new SaveData();
		found.RecordSecretFound(secret.Id);
		if (!GlobalStatsManager.IsCharacterRevealed(found, definition.Id))
			warnings.Add($"{label}: a save that has found '{secret.Id}' does not read him revealed; IsCharacterRevealed has stopped honouring RevealSecretId.");
		if (GlobalStatsManager.IsCharacterUnlocked(found, definition.Id))
			warnings.Add($"{label}: finding '{secret.Id}' unlocks him; it must only reveal.");
		if (GlobalStatsManager.IsCharacterRevealed(new SaveData(), definition.Id))
			warnings.Add($"{label} reads revealed on a brand-new save; the reveal secret has opened him to everyone.");
	}
}
