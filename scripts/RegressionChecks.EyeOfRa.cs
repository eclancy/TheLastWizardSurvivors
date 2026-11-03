using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// The Eye of Ra, Imhotep's Apotheosis (.ai/chapter-wizards.md 4.6): blindness is decided entirely inside
// Enemy.ApplyBlind and Fireball.FlareEyeOfRa, and every way it can go wrong is silent in a run - a blind that never
// lands, one that stacks into a permanent lock, one that lands on a boss and stops the fight. Checked on bare enemies
// that are never added to the tree, as ValidateFear checks fear.
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
		ordinary.ApplyBlind(Fireball.BlindSeconds);
		if (!ordinary.IsBlinded || MathF.Abs(ordinary.BlindSecondsLeft - Fireball.BlindSeconds) > 0.001f)
			warnings.Add($"Eye of Ra: an ordinary enemy did not take a {Fireball.BlindSeconds} s blind ({ordinary.BlindSecondsLeft:0.00} s); the form blinds nothing.");
		// Stacking-resistant: a second flare inside the first takes the longer of the two, never the sum.
		ordinary.ApplyBlind(Fireball.BlindSeconds * 0.5f);
		ordinary.ApplyBlind(Fireball.BlindSeconds);
		if (ordinary.BlindSecondsLeft > Fireball.BlindSeconds + 0.001f)
			warnings.Add($"Eye of Ra: flares stacked a blind to {ordinary.BlindSecondsLeft:0.00} s; it must take the longer, not the sum.");
		ordinary.Free();

		if (GD.Load<PackedScene>(scenePath)?.Instantiate() is Enemy elite)
		{
			elite.IsMiniBoss = true;
			elite.ApplyBlind(Fireball.BlindSeconds);
			if (elite.IsBlinded)
				warnings.Add("Eye of Ra: an elite or boss (IsMiniBoss) took ApplyBlind; elites are seared instead and bosses never lose him.");
			elite.Free();
		}

		// The card's numbers are the code's: "loses you for two seconds", a flare past the blast's own reach.
		ApotheosisDefinition form = ApotheosisCatalog.ForCharacter("imhotep");
		if (form == null || form.Form.Id != Fireball.EyeOfRaId || form.Verb != "blind" || form.SpellId != "fireball")
			warnings.Add("Eye of Ra: ApotheosisCatalog has no 'blind' form on Fireball for Imhotep.");
		else if (!form.Form.Description.Contains("two seconds") || MathF.Abs(Fireball.BlindSeconds - 2f) > 0.001f)
			warnings.Add($"Eye of Ra: the card says two seconds and the code blinds for {Fireball.BlindSeconds} s.");
		if (Fireball.FlareRadiusShare <= 1f || Fireball.FlareRadiusFor(95f) <= 95f)
			warnings.Add($"Eye of Ra: the flare must reach past the blast ({Fireball.FlareRadiusShare}x, {Fireball.FlareRadiusFor(95f):0} px for the level 1 blast).");
		if (Enemy.BlindPace <= 0f || Enemy.BlindPace >= 1f)
			warnings.Add($"Eye of Ra: a blinded body must wander slower than it chases (pace {Enemy.BlindPace}).");
	}
}
