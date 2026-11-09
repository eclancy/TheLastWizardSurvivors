using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// ENDLESS (.ai/endless-mode.md; EndlessCatalog, Node2DGame.Endless.cs). What a build cannot see about the mode: which
// chapters may be held through the night and on which saves, that the strength climbs and walls as the design says,
// that a night never pays a campaign deed the first night could not, and that the purse is capped.
public static partial class RegressionChecks
{
	private static void ValidateEndless(List<string> warnings)
	{
		ValidateEndlessChapters(warnings);
		ValidateEndlessStrength(warnings);
		ValidateEndlessUnlock(warnings);
	}

	// Every endless chapter is playable, on the road or the Sketchbook, has a warden with a scene of its own, and is
	// never the Bridge, the Court, the Throne or a chapter of the far shore (section 1.3, question 5).
	private static void ValidateEndlessChapters(List<string> warnings)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string id in EndlessCatalog.RoadChapterIds.Append(EndlessCatalog.SketchbookId))
		{
			if (!seen.Add(id))
				warnings.Add($"Endless: '{id}' is listed twice.");
			StageDefinition stage = StageCatalog.GetById(id);
			if (stage == null)
			{
				warnings.Add($"Endless: '{id}' names no chapter in StageCatalog.");
				continue;
			}
			if (!stage.IsPlayable)
				warnings.Add($"Endless: {stage.DisplayName} is listed but not playable.");
			if (!EndlessCatalog.IsRecordsOnly(stage) && !StageCatalog.IsInCampaign(stage))
				warnings.Add($"Endless: {stage.DisplayName} is listed as a road chapter but is not on the road.");
			if (StageCatalog.IsOnFarShore(stage))
				warnings.Add($"Endless: {stage.DisplayName} is on the far shore, which is never endless (question 5).");
			if (EndlessCatalog.ExcludedRoadChapterIds.Contains(id, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"Endless: {stage.DisplayName} is both endless and excluded.");
			BossDefinition warden = BossCatalog.ForStageIndex(stage.Index);
			if (warden == null)
				warnings.Add($"Endless: {stage.DisplayName} has no warden to send again.");
			else if (warden.IsOnStandIn || !ResourceLoader.Exists(warden.ScenePath))
				warnings.Add($"Endless: {stage.DisplayName}'s warden {warden.DisplayName} has no scene of its own at '{warden.ScenePath}'.");
		}
		foreach (string id in EndlessCatalog.ExcludedRoadChapterIds)
		{
			if (EndlessCatalog.IsEndlessChapter(StageCatalog.GetById(id)))
				warnings.Add($"Endless: {StageCatalog.DisplayNameFor(id)} is held through the night, but section 1.3 rules it out.");
		}
		foreach (StageDefinition far in StageCatalog.FarShore)
		{
			if (EndlessCatalog.IsEndlessChapter(far))
				warnings.Add($"Endless: {far.DisplayName}, on the far shore, is endless.");
		}
		// The road's every chapter is either endless or excluded: a chapter added to the road is a decision, not a default.
		foreach (StageDefinition road in StageCatalog.Campaign.Where(s => s.IsPlayable))
		{
			bool listed = EndlessCatalog.RoadChapterIds.Contains(road.Id, StringComparer.OrdinalIgnoreCase)
				|| EndlessCatalog.ExcludedRoadChapterIds.Contains(road.Id, StringComparer.OrdinalIgnoreCase);
			if (!listed)
				warnings.Add($"Endless: {road.DisplayName} is on the road but neither endless nor excluded (EndlessCatalog).");
		}
		// Every Vigil Kept counts the road's endless chapters: ten, as the doc says it.
		if (EndlessCatalog.RoadChapterIds.Count != 10)
			warnings.Add($"Endless: {EndlessCatalog.RoadChapterIds.Count} road chapters are endless; section 1.3 and Every Vigil Kept name ten.");
	}

	// Both curves start at 1, never fall, damage never under 1; the wall comes after the linear part and climbs faster
	// than it; the cap and the shares are positive; and the clamp holds the pace at the chapter's victory time.
	private static void ValidateEndlessStrength(List<string> warnings)
	{
		if (!Mathf.IsEqualApprox(EndlessCatalog.HealthStrength(0f), 1f) || !Mathf.IsEqualApprox(EndlessCatalog.DamageStrength(0f), 1f))
			warnings.Add($"Endless: the strength at the first fall is health x{EndlessCatalog.HealthStrength(0f):0.00}, damage x{EndlessCatalog.DamageStrength(0f):0.00}; both must be 1.");
		float lastHealth = 1f, lastDamage = 1f;
		for (float m = 0f; m <= 120f; m += 0.5f)
		{
			float health = EndlessCatalog.HealthStrength(m);
			float damage = EndlessCatalog.DamageStrength(m);
			if (health + 1e-4f < lastHealth || damage + 1e-4f < lastDamage)
			{
				warnings.Add($"Endless: the strength falls at {m:0.0} minutes past the first fall.");
				break;
			}
			if (damage < 1f)
			{
				warnings.Add($"Endless: enemy damage is under x1 at {m:0.0} minutes.");
				break;
			}
			lastHealth = health;
			lastDamage = damage;
		}
		if (EndlessCatalog.WallMinutes <= 0f || EndlessCatalog.HealthCompoundPerMinute <= 1f || EndlessCatalog.DamageCompoundPerMinute <= 1f)
			warnings.Add("Endless: the wall must come after a linear climb and compound past it.");
		// Past the wall the step a minute is larger than the linear one: it is a wall, not a gentler slope.
		float linearStep = EndlessCatalog.HealthPerMinute;
		float wallStep = EndlessCatalog.HealthStrength(EndlessCatalog.WallMinutes + 1f) - EndlessCatalog.HealthStrength(EndlessCatalog.WallMinutes);
		if (wallStep <= linearStep)
			warnings.Add($"Endless: a minute past the wall adds x{wallStep:0.000} health, no more than the linear x{linearStep:0.000}.");
		// "Every run is over by about an hour and a half": 30 minutes past the wall, 18 times the first night's health.
		float at90 = EndlessCatalog.HealthStrength(75f);
		if (at90 < 10f)
			warnings.Add($"Endless: at 90:00 on the clock enemies carry x{at90:0.0} health; the wall should have closed in (about x18).");
		if (EndlessCatalog.ArcaneCap <= 0)
			warnings.Add($"Endless: the Arcane cap is {EndlessCatalog.ArcaneCap}; it must be positive.");
		if (EndlessCatalog.ReturningWardenShare <= 0f || EndlessCatalog.WardenBonusShare <= 0f || EndlessCatalog.WardenBonusShare > 1f)
			warnings.Add("Endless: the returning warden's share and his bonus share must be positive (the bonus share at most 1).");
		if (EndlessCatalog.MaxLooseOrbs <= 0 || EndlessCatalog.VigilHoldSeconds <= 0f)
			warnings.Add("Endless: the loose-orb fold and the vigil's hold must be positive.");
		// The second warden is 1.1 times the first's, the third 1.7, the fourth 2.3 (section 1.4).
		float second = EndlessCatalog.ReturningWardenShare * EndlessCatalog.HealthStrength(15f);
		if (Math.Abs(second - 1.1f) > 0.05f)
			warnings.Add($"Endless: the second night's warden is x{second:0.00} the first's; section 1.4 says x1.1.");
	}

	// Never before Taken Back, never on an excluded chapter, never before the chapter's own warden has fallen there.
	private static void ValidateEndlessUnlock(List<string> warnings)
	{
		SaveData beaten = EndlessSampleSave(takenBack: false);
		SaveData open = EndlessSampleSave(takenBack: true);
		if (EndlessCatalog.IsOpen(new SaveData()))
			warnings.Add("Endless: a new save has endless open.");
		if (EndlessCatalog.IsOpen(beaten))
			warnings.Add("Endless: a save with every warden beaten but the Throne not taken back has endless open.");
		if (!EndlessCatalog.IsOpen(open))
			warnings.Add("Endless: a save that took back the Throne does not have endless open.");
		foreach (StageDefinition stage in StageCatalog.All)
		{
			bool endless = EndlessCatalog.IsEndlessChapter(stage);
			if (EndlessCatalog.IsOpenOn(beaten, stage))
				warnings.Add($"Endless: {stage.DisplayName} can be held through the night before Taken Back.");
			if (EndlessCatalog.IsOpenOn(open, stage) != endless)
				warnings.Add($"Endless: {stage.DisplayName} {(endless ? "cannot" : "can")} be held through the night on a save that took back the Throne.");
		}
		// The chapter's own warden is Boss Rush's rule: unbeaten there, no vigil.
		StageDefinition forest = StageCatalog.GetById(EndlessCatalog.RoadChapterIds[0]);
		BossDefinition elderbark = forest != null ? BossCatalog.ForStageIndex(forest.Index) : null;
		if (elderbark != null)
		{
			SaveData unbeaten = EndlessSampleSave(takenBack: true);
			unbeaten.Lifetime.BossesDefeated.RemoveAll(id => id.Equals(elderbark.Id, StringComparison.OrdinalIgnoreCase));
			if (EndlessCatalog.IsOpenOn(unbeaten, forest))
				warnings.Add($"Endless: {forest.DisplayName} can be held through the night before {elderbark.DisplayName} has fallen there.");
		}
	}

	// Every warden of every chapter in the lifetime record; Taken Back done or not.
	private static SaveData EndlessSampleSave(bool takenBack)
	{
		var save = new SaveData();
		foreach (StageDefinition stage in StageCatalog.All)
		{
			BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
			if (boss != null && !save.Lifetime.HasDefeatedBoss(boss.Id))
				save.Lifetime.BossesDefeated.Add(boss.Id);
		}
		if (!takenBack)
			save.Lifetime.BossesDefeated.RemoveAll(id => id.Equals(AchievementDefinitions.ThroneBossId, StringComparison.OrdinalIgnoreCase));
		else if (!save.UnlockedAchievementIds.Contains(AchievementDefinitions.TakenBackId))
			save.UnlockedAchievementIds.Add(AchievementDefinitions.TakenBackId);
		return save;
	}
}
