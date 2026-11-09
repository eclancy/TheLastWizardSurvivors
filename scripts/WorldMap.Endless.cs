using Godot;
using System;
using System.Linq;
using TheOneHeMissed.scripts;

// ENDLESS ON THE CARD (.ai/endless-mode.md 2.4, 3.4). An endless chapter's card gains one glyph-led line under the
// warden's state, after "Longest stand": this wizard's longest night here on the preset he is about to play
// ("Longest night: Night 3 · 41:20"), with the best of any wizard beside it when that is better - or, before there
// is a record, what the mode is ("Endless: keep the vigil when Elderbark falls"), marked new until the save keeps its
// first vigil. Only once endless is open on the chapter (EndlessCatalog.IsOpenOn): nothing of it is on show before
// Taken Back, and after it it is shown open, never sealed. No new focus stop and no new button.
public partial class WorldMap
{
	private Control EndlessCardLine(StageDefinition stage)
	{
		if (stage == null || !EndlessCatalog.IsOpenOn(save, stage))
			return null;
		string preset = save?.BalancePresetId;
		EndlessRecord mine = EndlessCatalog.RecordFor(save, stage.Id, character?.Id ?? string.Empty, preset);
		EndlessRecord best = EndlessCatalog.BestOnStage(save, stage.Id, preset);
		string text;
		bool fresh = false;
		if (mine != null)
		{
			text = $"Longest night: {EndlessCatalog.NightLine(EndlessCatalog.NightOf(mine), mine.ClockSeconds)}";
			if (best != null && !ReferenceEquals(best, mine) && best.Beats(mine))
				text += $"\nAny wizard: {EndlessCatalog.NightLine(EndlessCatalog.NightOf(best), best.ClockSeconds)}, {WizardName(best.CharacterId)}";
		}
		else if (best != null)
		{
			text = $"Longest night: {EndlessCatalog.NightLine(EndlessCatalog.NightOf(best), best.ClockSeconds)}, {WizardName(best.CharacterId)}";
		}
		else
		{
			BossDefinition warden = BossCatalog.ForStageIndex(stage.Index);
			string name = warden == null ? "the warden" : WithArticle(warden.DisplayName.Split(',')[0].Trim());
			text = $"Endless: keep the vigil when {name} falls";
			fresh = save != null && !save.HasKeptVigil;
		}

		var row = new HBoxContainer { Name = "CardEndless", MouseFilter = MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 8);
		Control line = GlyphLine("duration", text, fresh ? InkPlace : InkSoft, "CardEndlessLine");
		line.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(line);
		if (fresh)
		{
			// The New mark the results' cards wear, in the flag ink: the one way into the mode is a line on this card.
			Label mark = MakeText("New", ResponsiveLayout.TextRole.Label, InkFlag, "CardEndlessNew");
			mark.SizeFlagsVertical = SizeFlags.ShrinkBegin;
			row.AddChild(mark);
		}
		return row;
	}

	private static string WizardName(string characterId) =>
		CharacterRoster.GetAll().FirstOrDefault(c => c != null && c.Id.Equals(characterId ?? string.Empty, StringComparison.OrdinalIgnoreCase))?.Name
		?? "another wizard";
}
