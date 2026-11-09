using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE FAR SKY OVER A RUN (.ai/second-half.md section 5, piece F4; FarSky names it, Player.FarSky.cs deals it).
//
// The run is played under the recipe FarSky.ForRun names as it starts - tonight's, on a far-shore chapter of a save
// that has learned the far sky - and holds it whatever the save does in the meantime (a Feat completing mid-run
// changes what the NEXT night weighs, not this one). The pause screen's Run tab names it, with where it stands;
// making it pays FarSky.ArcaneBonus at the run's end, flat after every multiplier, and the Under the Far Sky Feat
// the first time (AchievementDefinitions; met mid-run through the achievement watch, which reads RunResult.FarSkyMade).
// The sky turns when the run ends: the save's run count is the night (FarSky.Night).
public partial class Node2DGame
{
	private ComboDefinition farSkyRecipe;

	// Called once from _Ready, after the player has read the save.
	private void StartFarSky()
	{
		if (player == null || !IsInstanceValid(player))
			return;
		SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		farSkyRecipe = IsBossRush ? null
			: FarSky.ForRun(save, CharacterRoster.GetByIndex(Global.SelectedCharacterIdx), StageCatalog.GetByIndex(Global.SelectedStageIdx));
		player.BeginFarSky(farSkyRecipe);
	}

	/// <summary>The run's end: tonight's Conjunction made pays its bonus, recorded on the breakdown. 0 otherwise.</summary>
	private int FarSkyArcane(RunResult result, ArcaneRewardBreakdown breakdown)
	{
		if (result == null || !result.FarSkyMade)
			return 0;
		if (breakdown != null)
		{
			breakdown.FarSkyBonus = FarSky.ArcaneBonus;
			breakdown.FarSkyName = farSkyRecipe?.Name ?? string.Empty;
		}
		return FarSky.ArcaneBonus;
	}

	/// <summary>
	/// The pause screen's line (.ai/second-half.md 5: "named before the run, on the world map's card ... and on the
	/// pause screen"): tonight's Conjunction and how it is made, then where it stands. Null on a run with no far sky.
	/// </summary>
	private Control BuildFarSkySection()
	{
		if (farSkyRecipe == null || player == null || !IsInstanceValid(player))
			return null;
		var list = new VBoxContainer { Name = "EscapeFarSky" };
		list.AddThemeConstantOverride("separation", 4);
		list.AddChild(BuildDetailLabel($"Tonight: {FarSky.Describe(farSkyRecipe)}", PauseInk.Ink, ResponsiveLayout.TextRole.Body));
		list.AddChild(BuildDetailLabel(FarSkyStanding(), PauseInk.InkSoft));
		return BuildEscapeSection("The Far Sky", BuildInfoFrame(list, PauseInk.Face, PauseInk.Edge, focusStop: true));
	}

	private string FarSkyStanding()
	{
		if (player.FarSkyMade)
			return $"Made this run. {FarSky.ArcaneBonus} Arcane Energy when it ends.";
		ComboStanding standing = player.EvaluateCombo(farSkyRecipe);
		if (standing.Seal == ComboSeal.OtherAscension)
		{
			SpellData closed = standing.FirstClosed ? standing.FirstHeld : standing.SecondHeld;
			return $"Sealed this run: {closed?.Name ?? "a parent"} took its other path.";
		}
		if (standing.Seal == ComboSeal.TooManyElements)
			return "Sealed this run: the two would name four elements.";
		if (player.SovereignSpell != null)
			return "Sealed this run: the Sovereign Rite holds your spells.";
		return "Its two spells come to your hand first. Make it before the run ends.";
	}
}
