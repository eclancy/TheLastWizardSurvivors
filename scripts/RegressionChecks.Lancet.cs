using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// LANCET (scripts/Lancet.cs, 2026-11-09). What a build cannot see and a run would only show as a needle going to the wrong
// body, a bonus that never doubles, an outright kill that takes a boss, a Bloodletting that lets out more or less than the
// poison held, a card promising a share the node does not deal, or a feat nobody can finish: the start, the ordering on
// paper, the wound bonus, the outright kill's rule, the poison let out of a real body, the cards' words against the node's
// numbers, and the Shrike feat.
public static partial class RegressionChecks
{
	private static void ValidateLancet(List<string> warnings)
	{
		if (!ContentValidator.SpellResourcePaths.Contains("res://SpellData_Lancet.tres"))
			warnings.Add("Lancet: SpellData_Lancet.tres is not in ContentValidator.SpellResourcePaths, so nothing validates it.");

		SpellData spell = ContentValidator.LoadAllSpells().FirstOrDefault(s => s?.Id == Lancet.SpellId);
		if (spell == null)
			warnings.Add("Lancet: no loaded spell has the id 'lancet'.");
		else
		{
			// Spells start small (Eric, 2026-11-02): one needle at level 1, more as it levels.
			if (spell.GetProjectileCountAtLevel(1) != 1)
				warnings.Add($"Lancet: level 1 throws {spell.GetProjectileCountAtLevel(1)} needles; it starts with one.");
			if (spell.GetProjectileCountAtLevel(spell.MaxLevel) <= 1)
				warnings.Add("Lancet: the needles never grow past one as it levels.");
			if (spell.GetProjectileCountAtLevel(spell.MaxLevel) + 2 > Lancet.MaxPerCast)
				warnings.Add($"Lancet: level {spell.MaxLevel} and a card or two of Amount would pass the {Lancet.MaxPerCast} needles a cast can throw.");
			if (spell.TargetingMode != SpellTargetingMode.Wounded)
				warnings.Add($"Lancet: its TargetingMode is {spell.TargetingMode}; it aims at the most wounded (Wounded).");
			Dictionary<Element, int> weights = spell.GetElementWeights();
			if (weights.Count != 2 || !weights.ContainsKey(Element.Metal) || !weights.ContainsKey(Element.Poison))
				warnings.Add($"Lancet: it names {string.Join("/", weights.Keys)}; it is Metal and Poison.");
			// A fast timer, in the middle band (.ai/spell-roster.md section 1: 150-350, the band the roster lacked).
			float first = spell.GetCooldownAtLevel(1), last = spell.GetCooldownAtLevel(spell.MaxLevel);
			if (first > 1.0f || last >= first)
				warnings.Add($"Lancet: its throw is {first:0.##} s at level 1 and {last:0.##} s at its last; it is a fast timer that quickens.");
			float near = spell.GetRangeAtLevel(1), far = spell.GetRangeAtLevel(spell.MaxLevel);
			if (near < 150f || far > 350f || far < near)
				warnings.Add($"Lancet: its reach is {near:0} at level 1 and {far:0} at its last; it chooses in the middle band, 150 to 350.");
			if (spell.Description == null || !spell.Description.Contains($"{Lancet.SliverShare * 100f:0}%"))
				warnings.Add($"Lancet: its description does not say the outright kill's {Lancet.SliverShare * 100f:0}%.");
		}

		// THE ORDER, on paper: the most wounded first, a tie of wounds - every unhurt body among them - to the nearer, and a
		// volley one body each.
		{
			var into = new int[4];
			int n = Lancet.Order(new[] { 0f, 0.5f, 0.2f, 0.5f }, new[] { 100f, 400f, 50f, 90f }, 4, into);
			if (n != 4 || into[0] != 3 || into[1] != 1 || into[2] != 2 || into[3] != 0)
				warnings.Add($"Lancet: a crowd at 0/50/20/50% lost ordered {string.Join(",", into.Take(n))}; the most wounded go first, the nearer of two alike.");
			n = Lancet.Order(new[] { 0f, 0f, 0f }, new[] { 900f, 100f, 400f }, 1, into);
			if (n != 1 || into[0] != 1)
				warnings.Add("Lancet: with nothing hurt it did not go for the nearest.");
			n = Lancet.Order(new[] { 0.3f, 0.6f }, new[] { 10f, 20f }, 3, into);
			if (n != 2 || into[0] == into[1])
				warnings.Add("Lancet: a volley larger than the crowd ordered the same body twice; each needle of the walk is one body.");
		}

		// THE BONUS: a fresh body takes the needle, a body with nothing left twice it, linear between.
		if (Lancet.WoundedDamage(10, 0f) != 10 || Lancet.WoundedDamage(10, 0.5f) != 15 || Lancet.WoundedDamage(10, 1f) != 20
			|| Lancet.WoundedDamage(10, 3f) != 20)
			warnings.Add("Lancet: a needle of 10 does not strike for 10, 15 and 20 at 0%, 50% and 100% of health lost (and no more past it).");

		// THE OUTRIGHT KILL: an ordinary body below the sliver, never at it, never an elite, a miniboss or a boss.
		{
			float under = Lancet.SliverShare * 0.5f;
			if (!Lancet.KillsOutright(false, false, under, Lancet.SliverShare))
				warnings.Add($"Lancet: an ordinary body at {under:P0} health did not die outright.");
			if (Lancet.KillsOutright(false, false, Lancet.SliverShare, Lancet.SliverShare))
				warnings.Add("Lancet: a body exactly at the sliver died outright; it dies below it.");
			if (Lancet.KillsOutright(true, false, under, Lancet.SliverShare) || Lancet.KillsOutright(false, true, under, Lancet.SliverShare))
				warnings.Add("Lancet: an elite, miniboss or boss died outright; they take only the wound bonus.");
			float sliver = Lancet.SliverShare, surgeon = Lancet.SurgeonsSliverShare;
			if (sliver <= 0f || sliver > 0.15f || surgeon <= sliver || surgeon > 0.3f)
				warnings.Add($"Lancet: the sliver is {sliver:P0} and Surgeon's Hand's {surgeon:P0}; a small sliver, and a larger one under the Reaper's Scythe's 30%.");
		}

		// THE POISON LET OUT, on a real body: what it holds is the ticks its clock has left at the tick it was laid at, and
		// ending it leaves nothing - the same clean state the poison's own expiry leaves.
		{
			float scale = Enemy.PlayerPoisonScale, durations = Enemy.DebuffDurationScale;
			Enemy.PlayerPoisonScale = 1f;
			Enemy.DebuffDurationScale = 1f;
			var body = new Enemy();
			try
			{
				if (body.RemainingPoisonDamage != 0)
					warnings.Add("Lancet: an unpoisoned body says it holds poison.");
				body.ApplyPoison(4, 3f);
				if (!body.IsPoisoned || body.RemainingPoisonDamage != 12)
					warnings.Add($"Lancet: a body poisoned for 4 a second over 3 s holds {body.RemainingPoisonDamage}; three ticks of 4 is 12.");
				body.ApplyPoison(2, 5f);
				if (body.RemainingPoisonDamage != 20)
					warnings.Add($"Lancet: the stronger tick and the longer clock (4 over 5 s) hold {body.RemainingPoisonDamage}, not 20.");
				body.EndPoison();
				if (body.IsPoisoned || body.RemainingPoisonDamage != 0)
					warnings.Add("Lancet: a body whose poison was let out still holds some.");
				Enemy.PlayerPoisonScale = 1.5f;
				body.ApplyPoison(4, 3f);
				if (body.RemainingPoisonDamage != 18)
					warnings.Add($"Lancet: at Sola Dosis's dose x1.5 three ticks of 4 hold {body.RemainingPoisonDamage}, not 18.");
			}
			finally
			{
				Enemy.PlayerPoisonScale = scale;
				Enemy.DebuffDurationScale = durations;
				// A run's layer (a validator run from the game) took it up when it was poisoned; it never entered the tree.
				StatusEffectLayer.Current?.Untrack(body);
				body.Free();
			}
		}

		// The cards' words against the numbers the node reads.
		if (spell != null)
		{
			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			SpellEvolutionOption blood = spell.Level4Options?.FirstOrDefault(o => o?.Id == Lancet.BloodlettingId);
			SpellEvolutionOption twin = spell.Level4Options?.FirstOrDefault(o => o?.Id == Lancet.TwinLancetsId);
			SpellEvolutionOption bodkin = spell.Level4Options?.FirstOrDefault(o => o?.Id == Lancet.BodkinId);
			SpellEvolutionOption triage = spell.Level8Options?.FirstOrDefault(o => o?.Id == Lancet.TriageId);
			SpellEvolutionOption surgeon = spell.Level8Options?.FirstOrDefault(o => o?.Id == Lancet.SurgeonsHandId);
			if (blood == null || blood.BonusElementWeights?.ContainsKey("Poison") != true)
				warnings.Add("Lancet: Bloodletting is missing or does not deepen Poison.");
			if (twin == null || twin.ProjectileCountBonus != 1)
				warnings.Add("Lancet: Twin Lancets is missing or does not add one needle.");
			if (bodkin == null || bodkin.BonusElementWeights?.ContainsKey("Metal") != true || bodkin.Rider != ElementRider.Wither
				|| !Mathf.IsEqualApprox(bodkin.RiderMagnitude, Lancet.BodkinWither) || !bodkin.Description.Contains($"{Lancet.BodkinWither * 100f:0}%"))
				warnings.Add($"Lancet: Bodkin is missing, does not deepen Metal, or does not Wither the {Lancet.BodkinWither * 100f:0}% its card says.");
			if (triage == null)
				warnings.Add($"Lancet: no level 8 card '{Lancet.TriageId}', which the node reads by id.");
			if (surgeon == null || !surgeon.Description.Contains($"{Lancet.SurgeonsSliverShare * 100f:0}%")
				|| !surgeon.Description.Contains($"{Lancet.SliverShare * 100f:0}%"))
				warnings.Add($"Lancet: Surgeon's Hand's card does not say the sliver goes from {Lancet.SliverShare * 100f:0}% to {Lancet.SurgeonsSliverShare * 100f:0}%.");
			// Both halves deepened at level 4 on different cards, so level 8 deepens neither and no path names three of one.
			if (spell.Level8Options?.Any(o => o?.BonusElementWeights != null && o.BonusElementWeights.Count > 0) == true)
				warnings.Add("Lancet: a level 8 card deepens an element; both are deepened at level 4, so a path would reach three of one.");
		}

		// THE SHRIKE pays it: a run one poisoned kill short does not, one that makes the count does.
		AchievementDefinition shrike = AchievementDefinitions.GetById("shrike");
		if (shrike == null || shrike.SpellUnlockId != Lancet.SpellId)
			warnings.Add("Lancet: no Shrike feat pays Lancet, so nobody can ever hold it.");
		else
		{
			int target = AchievementDefinitions.ShrikePoisonedKills;
			if (shrike.IsComplete(AchievementContext.Projected(new SaveData(), new RunResult { PoisonedKills = target - 1 })))
				warnings.Add($"Lancet: Shrike completes on {target - 1} poisoned kills in a run; it asks for {target}.");
			if (!shrike.IsComplete(AchievementContext.Projected(new SaveData(), new RunResult { PoisonedKills = target })))
				warnings.Add($"Lancet: a run with {target} poisoned kills does not complete Shrike (LifetimeStats.BestPoisonedKillsInRun).");
			// Kept across a save round trip, so the menu's bar survives a restart.
			var save = new SaveData();
			save.Lifetime.Absorb(new RunResult { PoisonedKills = 42 });
			if (LifetimeStats.FromDictionary(save.Lifetime.ToGodotDictionary()).BestPoisonedKillsInRun != 42)
				warnings.Add("Lancet: LifetimeStats.BestPoisonedKillsInRun is lost through the save's round trip.");
		}
	}
}
