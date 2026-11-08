using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TheOneHeMissed.scripts;

public static partial class AchievementsPage
{
	// All Godot resource and node work stays on the main thread. The title's bounded work loop
	// advances these small units between draws, instead of constructing the entire page in one frame.
	public static IEnumerable<int> PrepareMenu(VBoxContainer list, AchievementContext context, Filter filter, Action<Result> complete)
	{
		var page = new PageModel(context?.Save ?? new SaveData(), null, deferred: true);
		foreach (int step in page.Populate(null)) yield return step;
		foreach (int step in BuildMenuRows(list, page, filter, CodexRows.ColumnsFor(list))) yield return step;
		var deeds = page.Items.Where(i => i.Secret == null).ToList();
		int found = page.Chapters.Sum(c => c.Found);
		complete(new Result { Earned = deeds.Count(i => i.Done) + found,
			Total = deeds.Count + page.Chapters.Sum(c => c.Total), SecretsFound = found });
	}

	private static IEnumerable<int> BuildMenuRows(VBoxContainer list, PageModel page, Filter filter, int columns)
	{
		foreach (var plan in MenuSections(page, filter))
		{
			VBoxContainer section = CodexRows.AddSection(list, plan.Title, plan.Count, CodexRows.Stone, columns, Array.Empty<Control>());
			GridContainer grid = section.GetNode<GridContainer>(CodexRows.SectionRowsName);
			foreach (Control row in plan.Rows)
			{
				grid.AddChild(row);
				yield return 0;
			}
		}
	}

	private static IEnumerable<(string Title, string Count, IEnumerable<Control> Rows)> MenuSections(PageModel page, Filter filter)
	{
		if (filter == Filter.Chapters)
		{
			foreach (StageDefinition stage in StageCatalog.InDisplayOrder)
			{
				var mine = page.Items.Where(i => !i.IsFeat && SameStage(i.StageId, stage.Id)).ToList();
				ChapterSecrets secrets = page.ChapterOf(stage.Id);
				if (mine.Count == 0 && secrets == null) continue;
				int total = mine.Count(i => i.Secret == null) + (secrets?.Total ?? 0);
				int done = mine.Count(i => i.Done && i.Secret == null) + (secrets?.Found ?? 0);
				yield return (stage.DisplayName, $"{done} of {total}", ChapterRows(mine, secrets));
			}
			yield break;
		}
		if (filter == Filter.Secrets)
		{
			int left = page.Chapters.Sum(c => c.Total - c.Found);
			if (page.Items.Any(i => i.Seen) || page.Chapters.Any(c => c.Sealed.Count > 0))
				yield return ("Still to find", $"{left} to find", SeenSecretRows(page));
			var found = page.Items.Where(i => i.Done && i.Secret != null).ToList();
			if (found.Count > 0)
				yield return ("Found", $"{found.Count}", found.Select(i => ItemRow(i, true)));
			yield break;
		}
		var items = page.Items.Where(i => filter != Filter.Feats || i.IsFeat).ToList();
		var open = items.Where(i => !i.Done && !i.SealedWizard).ToList();
		bool showChapter = filter != Filter.Feats;
		if (open.Count > 0)
			yield return ("In progress", $"{open.Count} to go", OrderOpen(open).Select(i => ItemRow(i, showChapter)));
		if (filter == Filter.All)
		{
			int hidden = page.Chapters.Sum(c => c.Sealed.Count) + page.Items.Count(i => i.SealedWizard);
			if (hidden > 0) yield return ("Not yet found", $"{hidden} to find", HiddenMenuRows(page));
		}
		var doneItems = items.Where(i => i.Done).ToList();
		if (doneItems.Count > 0)
			yield return ("Done", $"{doneItems.Count}", doneItems.Select(i => ItemRow(i, showChapter)));
	}

	private static IEnumerable<Control> ChapterRows(List<Item> mine, ChapterSecrets secrets)
	{
		foreach (Item i in mine.Where(i => !i.Done && !i.SealedWizard && i.Secret == null)) yield return ItemRow(i, false);
		foreach (Item i in mine.Where(i => i.Seen)) yield return ItemRow(i, false);
		if (secrets?.Sealed.Count > 0) yield return SealedChapterRow(secrets);
		foreach (Item i in mine.Where(i => i.SealedWizard)) yield return SealedWizardRow(i);
		foreach (Item i in mine.Where(i => i.Done)) yield return ItemRow(i, false);
	}

	private static IEnumerable<Control> SeenSecretRows(PageModel page)
	{
		foreach (ChapterSecrets chapter in page.Chapters)
		{
			foreach (Item i in page.Items.Where(i => i.Seen && SameStage(i.StageId, chapter.Stage?.Id))) yield return ItemRow(i, true);
			if (chapter.Sealed.Count > 0) yield return SealedChapterRow(chapter);
		}
	}

	private static IEnumerable<Control> HiddenMenuRows(PageModel page)
	{
		foreach (ChapterSecrets chapter in page.Chapters)
			if (chapter.Sealed.Count > 0) yield return SealedChapterRow(chapter);
		foreach (Item i in page.Items.Where(i => i.SealedWizard)) yield return SealedWizardRow(i);
	}
}
