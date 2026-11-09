using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// FOLIO, John Dee's opener since 2026-11-09 (.ai/character-fit.md section 6; scripts/Folio.cs). What a build cannot see
// and a run would only show as silence: his form left on the spell he no longer opens on (never offered again), a save
// that saw the Spirit Mirror on Hollow Star finding it sealed in the Ascension browser, a book that holds nothing or
// everything, a card whose words drifted from its number, and the feat that pays the spell to everyone else.
public static partial class RegressionChecks
{
	private static void ValidateFolio(List<string> warnings)
	{
		if (!ContentValidator.SpellResourcePaths.Contains("res://SpellData_Folio.tres"))
			warnings.Add("Folio: SpellData_Folio.tres is not in ContentValidator.SpellResourcePaths, so nothing validates it.");

		// His opener, and his form on it - and none left behind on Hollow Star, which is nobody's opener now.
		if (CharacterRoster.GetAll().FirstOrDefault(c => c?.Id == "john_dee") is CharacterData dee
			&& dee.StartingSpellResource?.Id != Folio.SpellId)
			warnings.Add($"Folio: John Dee opens on '{dee.StartingSpellResource?.Id}', not Folio, so his Spirit Mirror can never be reached.");
		ApotheosisDefinition form = ApotheosisCatalog.ForCharacter("john_dee");
		if (form == null || form.SpellId != Folio.SpellId || form.Form?.Id != Folio.SpiritMirrorId || form.Verb != "pull"
			|| form.Element != Element.Darkness)
			warnings.Add("Folio: ApotheosisCatalog has no 'pull' form for John Dee on Folio deepening Darkness (the Spirit Mirror).");
		if (ApotheosisCatalog.All.Any(d => d.SpellId.Equals("hollow_star", StringComparison.OrdinalIgnoreCase)))
			warnings.Add("Folio: an Apotheosis still names Hollow Star; the Spirit Mirror moved to Folio with John Dee and Hollow Star is nobody's opener.");
		// The card's numbers are the code's: "every tenth page".
		if (form?.Form != null && (Folio.MirrorEvery != 10 || !form.Form.Description.Contains("tenth page")))
			warnings.Add($"Folio: the Spirit Mirror's card says every tenth page and the code opens it every {Folio.MirrorEvery}.");

		// A save that saw the form on Hollow Star (SeenAscensionIds keeps "hollow_star:hollow_star_spirit_mirror") still has
		// it seen on Folio, so the Ascension browser does not seal it again (SaveData.MovedAscensions).
		var seen = new SaveData();
		seen.SeenAscensionIds.Add(SaveData.AscensionKey("hollow_star", Folio.HollowStarSpiritMirrorId));
		if (!seen.IsAscensionSeen(Folio.SpellId, Folio.SpiritMirrorId))
			warnings.Add("Folio: a save that saw the Spirit Mirror on Hollow Star finds it sealed again on Folio (SaveData.MovedAscensions).");
		if (new SaveData().IsAscensionSeen(Folio.SpellId, Folio.SpiritMirrorId))
			warnings.Add("Folio: a brand-new save reads the Spirit Mirror as already seen.");

		// The book: it holds pages while nothing is in reach, a few and no more, and a timed turn into a full book is
		// refused (the fire loop then keeps its timer ripe) rather than lost.
		// Read through locals: a constant compared with a constant is CS0162 at the warning it guards.
		int holds = Folio.BookHolds, mirrorEvery = Folio.MirrorEvery;
		float gap = Folio.PageGap, tornShare = Folio.TornShare, inkSlow = Folio.InkSlow;
		if (holds <= 0 || gap <= 0f || mirrorEvery <= 0)
			warnings.Add($"Folio: the book holds {holds}, lets pages go {gap} s apart and opens the mirror every {mirrorEvery}; all must be above zero.");
		var book = new Folio();
		try
		{
			for (int i = 0; i < Folio.BookHolds * 3; i++)
				book.TurnByOrb();
			if (book.Waiting != Folio.BookHolds)
				warnings.Add($"Folio: {Folio.BookHolds * 3} orbs with nobody in reach left {book.Waiting} pages in the book; it holds {Folio.BookHolds}.");
			if (book.TurnByTime(1))
				warnings.Add("Folio: a timed turn into a full book was taken; it must be refused so the fire loop keeps it ripe.");
		}
		finally
		{
			book.Free();
		}

		// The cards' words against the numbers they name.
		if (MathF.Abs(tornShare - 0.5f) > 0.0001f)
			warnings.Add($"Folio: Torn Leaf's card says each half cuts for half the page, and the code gives it {tornShare:0%}.");
		if (inkSlow <= 0f || inkSlow >= 1f)
			warnings.Add($"Folio: Inkblot slows by {inkSlow:0%}; a slow is between none and a root.");

		// The Magpie feat pays it: a run that opens one chest short does not, one that opens the target does.
		AchievementDefinition magpie = AchievementDefinitions.GetById("magpie");
		if (magpie == null || magpie.SpellUnlockId != Folio.SpellId)
			warnings.Add("Folio: no Magpie feat pays Folio, so nobody but John Dee can ever hold it.");
		else
		{
			if (magpie.IsComplete(AchievementContext.Projected(new SaveData(), new RunResult { ChestsOpened = AchievementDefinitions.MagpieChestTarget - 1 })))
				warnings.Add($"Folio: Magpie completes on {AchievementDefinitions.MagpieChestTarget - 1} chests in a run; it asks for {AchievementDefinitions.MagpieChestTarget}.");
			if (!magpie.IsComplete(AchievementContext.Projected(new SaveData(), new RunResult { ChestsOpened = AchievementDefinitions.MagpieChestTarget })))
				warnings.Add($"Folio: a run that opened {AchievementDefinitions.MagpieChestTarget} chests does not complete Magpie (LifetimeStats.BestChestsInRun).");
		}
	}
}
