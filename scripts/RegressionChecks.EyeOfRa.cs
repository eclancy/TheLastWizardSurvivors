using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// The Eye of Ra, Imhotep's Apotheosis (.ai/chapter-wizards.md 4.6): blindness is decided entirely inside
// Enemy.ApplyBlind and ScorchingRayBeam.FlareEyeOfRa, and every way it can go wrong is silent in a run - a blind that
// never lands, one that stacks into a permanent lock, one that lands on a boss and stops the fight. Checked on bare
// enemies that are never added to the tree, as ValidateFear checks fear. Since 2026-11-09 the form is Scorching Ray's,
// as his opener is (.ai/character-fit.md section 1); it was Fireball's.
public static partial class RegressionChecks
{
	private static void ValidateEyeOfRa(List<string> warnings)
	{
		const string scenePath = "res://scenes/enemy.tscn";
		if (GD.Load<PackedScene>(scenePath)?.Instantiate() is not Enemy ordinary)
		{
			warnings.Add($"Eye of Ra: {scenePath} did not instantiate as an Enemy.");
			return;
		}
		ordinary.ApplyBlind(ScorchingRayBeam.BlindSeconds);
		if (!ordinary.IsBlinded || MathF.Abs(ordinary.BlindSecondsLeft - ScorchingRayBeam.BlindSeconds) > 0.001f)
			warnings.Add($"Eye of Ra: an ordinary enemy did not take a {ScorchingRayBeam.BlindSeconds} s blind ({ordinary.BlindSecondsLeft:0.00} s); the form blinds nothing.");
		// Stacking-resistant: a second flare inside the first takes the longer of the two, never the sum.
		ordinary.ApplyBlind(ScorchingRayBeam.BlindSeconds * 0.5f);
		ordinary.ApplyBlind(ScorchingRayBeam.BlindSeconds);
		if (ordinary.BlindSecondsLeft > ScorchingRayBeam.BlindSeconds + 0.001f)
			warnings.Add($"Eye of Ra: flares stacked a blind to {ordinary.BlindSecondsLeft:0.00} s; it must take the longer, not the sum.");
		ordinary.Free();

		if (GD.Load<PackedScene>(scenePath)?.Instantiate() is Enemy elite)
		{
			elite.IsMiniBoss = true;
			elite.ApplyBlind(ScorchingRayBeam.BlindSeconds);
			if (elite.IsBlinded)
				warnings.Add("Eye of Ra: an elite or boss (IsMiniBoss) took ApplyBlind; elites are seared instead and bosses never lose him.");
			elite.Free();
		}

		// The card's numbers are the code's: "loses you for two seconds", on Imhotep's own opener.
		ApotheosisDefinition form = ApotheosisCatalog.ForCharacter("imhotep");
		if (form == null || form.Form.Id != ScorchingRayBeam.EyeOfRaId || form.Verb != "blind" || form.SpellId != "scorching_ray")
			warnings.Add("Eye of Ra: ApotheosisCatalog has no 'blind' form on Scorching Ray for Imhotep.");
		else if (!form.Form.Description.Contains("two seconds") || MathF.Abs(ScorchingRayBeam.BlindSeconds - 2f) > 0.001f)
			warnings.Add($"Eye of Ra: the card says two seconds and the code blinds for {ScorchingRayBeam.BlindSeconds} s.");
		if (CharacterRoster.GetAll().FirstOrDefault(c => c?.Id == "imhotep") is CharacterData imhotep
			&& imhotep.StartingSpellResource?.Id != "scorching_ray")
			warnings.Add($"Eye of Ra: Imhotep opens on '{imhotep.StartingSpellResource?.Id}', not Scorching Ray, so his form can never be reached.");

		// "Hotter rays flare wider": a cold ray's flare is smaller than a hot one's, area grows both, and the cap holds.
		float cold = ScorchingRayBeam.FlareRadiusFor(0f, 1f), hot = ScorchingRayBeam.FlareRadiusFor(1f, 1f);
		if (cold <= 0f || hot <= cold)
			warnings.Add($"Eye of Ra: a full-heat flare ({hot:0} px) must reach past a cold one ({cold:0} px); the card says hotter rays flare wider.");
		if (ScorchingRayBeam.FlareRadiusFor(1f, 1.5f) <= hot)
			warnings.Add("Eye of Ra: area does not grow the flare.");
		if (ScorchingRayBeam.FlareRadiusFor(1f, 100f) > ScorchingRayBeam.MaxFlareRadius + 0.001f)
			warnings.Add($"Eye of Ra: the flare passed its cap of {ScorchingRayBeam.MaxFlareRadius} px; a huge area would blind the whole screen.");
		if (Enemy.BlindPace <= 0f || Enemy.BlindPace >= 1f)
			warnings.Add($"Eye of Ra: a blinded body must wander slower than it chases (pace {Enemy.BlindPace}).");

		// The form moved from Fireball with him: a save that saw it offered there (SeenAscensionIds keeps
		// "fireball:fireball_eye_of_ra") still has it seen on the ray, so the Ascension browser does not seal it again.
		var save = new SaveData();
		save.SeenAscensionIds.Add(SaveData.AscensionKey("fireball", "fireball_eye_of_ra"));
		if (!save.IsAscensionSeen("scorching_ray", ScorchingRayBeam.EyeOfRaId))
			warnings.Add("Eye of Ra: a save that saw the form on Fireball finds it sealed again on Scorching Ray (SaveData.MovedAscensions).");
		foreach (var moved in SaveData.MovedAscensions)
		{
			string[] now = moved.Key.Split(':');
			if (now.Length != 2 || !ApotheosisCatalog.All.Any(d => d.SpellId == now[0] && d.Form?.Id == now[1]))
				warnings.Add($"Eye of Ra: SaveData.MovedAscensions reads an old key as '{moved.Key}', which no Apotheosis offers.");
		}
	}
}
