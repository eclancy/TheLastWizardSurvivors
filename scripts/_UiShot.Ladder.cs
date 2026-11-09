using Godot;
using TheOneHeMissed.scripts;

// THE CONJUNCTION LADDER's shots (.ai/conjunction-progression.md; ConjunctionLadder). In memory only - writes are off:
//   --ladder-learned : the ladder's three lessons taught (the Ascension, the Apotheosis, Conjunctions), so every rung
//                      the save has earned is on show - Kindred Draw on the shelf, Suits on the character cards, the pin;
//   --pin=ID         : that recipe pinned (rung I): the pause screen's "Pinned Conjunction" and the quest corner's line.
// Applied after every other flag has built its save (--demo-shop, --all-unlocked), so neither is undone by it.
public partial class _UiShot
{
	private bool ladderLearned;
	private string ladderPin;

	private void ApplyLadderShotArgs()
	{
		if (!ladderLearned && string.IsNullOrEmpty(ladderPin))
			return;
		SaveManager.WritesDisabled = true;
		SaveData save = CurrentSave();
		if (save == null)
		{
			GD.PrintErr("_UiShot: --ladder-learned / --pin need a save, and there is none.");
			return;
		}
		if (ladderLearned)
		{
			save.AscensionLearned = true;
			foreach (Teachings.Lesson lesson in Teachings.Ladder)
				Teachings.Teach(save, lesson);
		}
		if (!string.IsNullOrEmpty(ladderPin))
		{
			// Through the rung's own rule, so a shot can never show a pin the game would refuse.
			if (!ConjunctionLadder.MayPin(ComboCatalog.ForId(ladderPin), save, out string why))
				GD.PrintErr($"_UiShot: --pin={ladderPin} refused: {why}");
			else
				save.PinnedRecipeId = ComboCatalog.ForId(ladderPin).Id;
		}
		GD.Print($"_UiShot: ladder - Conjunctions learned {Teachings.HasLearned(save, Teaching.Conjunctions)}, pinned '{save.PinnedRecipeId}'");
	}
}
