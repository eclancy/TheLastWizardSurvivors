using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE PINNED CONJUNCTION OVER A RUN (rung I of .ai/conjunction-progression.md; ConjunctionLadder names the pin,
// Player.BuildRecipeChecklist reads where it stands).
//
// Two places and no more: the pause screen's Run tab gets the recipe's checklist as its own section, after the far
// sky's (one marked line in ShowEscapeRunOverview), and the quest corner gets ONE line - the two linked rings and the
// recipe's next step (QuestTracker.SetRecipeLine) - on the same one-second reading the quests move on. The corner is
// QuestTracker's and stays so: the line is part of it, under the quests, outlined as they are. The recipe is named only
// once made (ConjunctionLadder.ShownName): until then its result is sealed, as everywhere else.
public partial class Node2DGame
{
	// The recipe this run follows: the one Nostradamus foretold for it (rung IV), else the pinned one, read live from
	// the save (a pin made in the Codex over the pause screen shows on the next reading) - and only on a run that has
	// learned Conjunctions (the run's own flag).
	private ComboDefinition PinnedRecipeNow()
	{
		if (IsBossRush || player == null || !IsInstanceValid(player))
			return null;
		if (player.ForetoldRecipe != null)
			return player.ForetoldRecipe;
		return player.ConjunctionsLearned ? ConjunctionLadder.Pinned(CurrentSaveData()) : null;
	}

	// Rung IV, Nostradamus foretells: called once from _Ready, after StartFarSky - the far sky's night wins, so a run
	// with tonight's recipe is foretold nothing (Player.BeginForetold). ConjunctionLadder.Foretold says what, and where.
	private void StartForetold()
	{
		if (player == null || !IsInstanceValid(player))
			return;
		player.BeginForetold(ConjunctionLadder.Foretold(CurrentSaveData(), CharacterRoster.GetByIndex(Global.SelectedCharacterIdx),
			StageCatalog.GetByIndex(Global.SelectedStageIdx), IsBossRush));
	}

	/// <summary>The quest corner's one line for the pinned recipe, or nothing when none is pinned.</summary>
	private void RefreshPinnedRecipeLine()
	{
		if (questTracker == null || !IsInstanceValid(questTracker))
			return;
		ComboDefinition recipe = PinnedRecipeNow();
		if (recipe == null)
		{
			questTracker.SetRecipeLine(null, false);
			return;
		}
		RecipeChecklist list = player.BuildRecipeChecklist(recipe, CurrentSaveData());
		// Made, the name is revealed (its Feat completes as the strip offers it); before, the step names the spell.
		questTracker.SetRecipeLine(list.Made ? $"{ConjunctionLadder.ShownName(recipe, CurrentSaveData())} made" : list.NextStep, list.Made);
	}

	/// <summary>
	/// The pause screen's section: the recipe and how it is made, then each line of its checklist behind its glyph -
	/// each parent in the book and its level, each named path's element count, your level against the Ascension's, a
	/// level 4 path that would seal it, or why it is sealed. Null when nothing is pinned.
	/// </summary>
	private Control BuildPinnedRecipeSection()
	{
		ComboDefinition recipe = PinnedRecipeNow();
		if (recipe == null)
			return null;
		RecipeChecklist list = player.BuildRecipeChecklist(recipe, CurrentSaveData());
		var box = new VBoxContainer { Name = "EscapePinnedRecipe" };
		box.AddThemeConstantOverride("separation", 4);
		box.AddChild(BuildDetailLabel(ConjunctionLadder.Describe(recipe, CurrentSaveData()), PauseInk.Ink, ResponsiveLayout.TextRole.Body));
		bool foretold = player.ForetoldRecipe == recipe;
		if (foretold && !list.Made && !list.Sealed)
			box.AddChild(BuildDetailLabel("Nostradamus foretold it: its two spells come to your hand first.", PauseInk.InkSoft));
		foreach (RecipeCheck check in list.Lines)
			box.AddChild(BuildChecklistLine(check));
		if (!list.Made && !list.Sealed)
			box.AddChild(BuildDetailLabel($"Next: {list.NextStep}", PauseInk.InkSoft));
		return BuildEscapeSection(foretold ? "Foretold Conjunction" : "Pinned Conjunction", BuildInfoFrame(box, PauseInk.Face, PauseInk.Edge, focusStop: true));
	}

	// One line behind its own stat glyph (.ai/level-up-screen.md 4a), done in the done ink, closed in the flag's.
	private Control BuildChecklistLine(RecipeCheck check)
	{
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 8);
		// The 12-pixel glyph at x2 on both canvases, the Label line's height.
		const float size = 24f;
		row.AddChild(new TextureRect
		{
			Texture = StatIcons.Load(check.Glyph),
			CustomMinimumSize = new Vector2(size, size),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});
		Color ink = check.Closed ? PauseInk.Flag : check.Met ? PauseInk.Done : PauseInk.Ink;
		Label words = BuildDetailLabel(ResponsiveLayout.NoWidow(check.Text), ink);
		words.VerticalAlignment = VerticalAlignment.Center;
		row.AddChild(words);
		return row;
	}
}
