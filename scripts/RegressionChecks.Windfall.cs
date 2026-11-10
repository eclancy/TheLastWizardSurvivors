using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// WINDFALL (scripts/Windfall.cs, 2026-11-09). What a build cannot see and a run would only show as a tree that empties where it
// was planted, a crop that never grows, a ring that lands in a heap, a card promising a share the node does not deal, or a
// feat nobody can finish: the start, the shake rule walked by hand, the ring, the element path, the cards' words against the
// node's numbers, and the Fieldfare feat.
public static partial class RegressionChecks
{
	private static void ValidateWindfall(List<string> warnings)
	{
		if (!ContentValidator.SpellResourcePaths.Contains("res://SpellData_Windfall.tres"))
			warnings.Add("Windfall: SpellData_Windfall.tres is not in ContentValidator.SpellResourcePaths, so nothing validates it.");

		SpellData spell = ContentValidator.LoadAllSpells().FirstOrDefault(s => s?.Id == Windfall.SpellId);
		if (spell == null)
			warnings.Add("Windfall: no loaded spell has the id 'windfall'.");
		else
		{
			// Spells start small (Eric, 2026-11-02): one tree and a few fruit at level 1; more of both as it levels.
			if (spell.GetProjectileCountAtLevel(1) != 1)
				warnings.Add($"Windfall: level 1 may stand {spell.GetProjectileCountAtLevel(1)} trees; it starts with one.");
			int firstCrop = Windfall.CropAt(spell, 1), lastCrop = Windfall.CropAt(spell, spell.MaxLevel);
			if (firstCrop < 2 || firstCrop > 4)
				warnings.Add($"Windfall: a tree carries {firstCrop} fruit at level 1; it starts with a few.");
			if (lastCrop <= firstCrop || lastCrop > Windfall.MaxCrop)
				warnings.Add($"Windfall: the crop is {firstCrop} at level 1 and {lastCrop} at its last; it grows, within the {Windfall.MaxCrop} slots a crown has.");
			if (spell.GetProjectileCountAtLevel(spell.MaxLevel) <= 1)
				warnings.Add("Windfall: the trees never grow past one as it levels.");
			if (Windfall.FruitSlots.Length != Windfall.MaxCrop)
				warnings.Add($"Windfall: {Windfall.FruitSlots.Length} fruit slots for a crop of up to {Windfall.MaxCrop} (tools/art/windfall.py's FRUIT_SLOTS).");
			if (spell.TargetingMode != SpellTargetingMode.Planted)
				warnings.Add($"Windfall: its TargetingMode is {spell.TargetingMode}; it is planted where he stands (Planted).");
			Dictionary<Element, int> weights = spell.GetElementWeights();
			if (weights.Count != 2 || !weights.ContainsKey(Element.Grass) || !weights.ContainsKey(Element.Metal))
				warnings.Add($"Windfall: it names {string.Join("/", weights.Keys)}; it is Grass and Metal.");
			// A tree must be able to fill before it is the oldest, or "the longer it is left, the more it holds" is never true:
			// at the cap a tree stands about (trees x cooldown) before it is uprooted.
			for (int level = 1; level <= spell.MaxLevel; level++)
			{
				float stands = spell.GetProjectileCountAtLevel(level) * spell.GetCooldownAtLevel(level);
				float fills = Windfall.CropAt(spell, level) * Windfall.RipenSeconds;
				if (fills > stands)
					warnings.Add($"Windfall: at level {level} a tree fills in {fills:0.#} s but is uprooted after about {stands:0.#} s; it never holds a full crop.");
			}
		}

		// THE SHAKE RULE, walked by hand. Planted under him (inside, unarmed): standing there, nothing. Out past LeaveReach and
		// back in with fruit: one shake, and it disarms. In again without leaving: nothing. Leave, come back with nothing ripe:
		// nothing, and it stays armed for the next time.
		{
			bool armed = false, inside = true;
			float under = Windfall.ShakeReach * 0.5f, away = Windfall.LeaveReach + 10f, edge = (Windfall.ShakeReach + Windfall.LeaveReach) * 0.5f;
			bool shookWherePlanted = Windfall.StepShake(ref armed, ref inside, under, true) | Windfall.StepShake(ref armed, ref inside, under, true);
			if (shookWherePlanted)
				warnings.Add("Windfall: a tree shook on the spot it was planted; he must leave the canopy first.");
			// A step out that never reaches LeaveReach does not arm it.
			Windfall.StepShake(ref armed, ref inside, edge, true);
			if (Windfall.StepShake(ref armed, ref inside, under, true))
				warnings.Add("Windfall: a tree shook when he stepped just outside its crown and back; it asks him to have left.");
			Windfall.StepShake(ref armed, ref inside, away, true);
			if (!armed)
				warnings.Add("Windfall: walking far from a tree did not arm it.");
			if (!Windfall.StepShake(ref armed, ref inside, under, true))
				warnings.Add("Windfall: walking back under an armed tree with fruit on it did not shake it.");
			if (armed || Windfall.StepShake(ref armed, ref inside, under, true))
				warnings.Add("Windfall: a shaken tree stayed armed, or shook again while he stood under it.");
			Windfall.StepShake(ref armed, ref inside, away, false);
			if (Windfall.StepShake(ref armed, ref inside, under, false) || !armed)
				warnings.Add("Windfall: walking under an armed tree with nothing ripe shook it or disarmed it; it waits for fruit.");
		}

		// THE RING: a crop lands evenly round the trunk at the ring's radius, never in a heap.
		{
			Vector2 foot = new(300f, -200f);
			const int count = 6;
			for (int k = 0; k < count; k++)
			{
				Vector2 at = Windfall.RingPoint(foot, 60f, 0.3f, k, count);
				Vector2 next = Windfall.RingPoint(foot, 60f, 0.3f, (k + 1) % count, count);
				if (MathF.Abs(at.DistanceTo(foot) - 60f) > 0.01f || MathF.Abs(at.DistanceTo(next) - 60f) > 0.5f)
				{
					warnings.Add($"Windfall: the {k}th of {count} shaken fruit lands {at.DistanceTo(foot):0.#} from the trunk and {at.DistanceTo(next):0.#} from the next; a crop falls evenly round it.");
					break;
				}
			}
			// The ring lands round him, not on him - outside the canopy he stands under - and its thuds reach in to where he
			// stands, so what pressed in behind him is hit. Read through locals (CS0162, as above).
			float ring = Windfall.RingRadius, thud = Windfall.ThudRadius, under = Windfall.ShakeReach;
			float leave = Windfall.LeaveReach, spacing = Windfall.PlantSpacing;
			if (ring <= under || ring - thud > under)
				warnings.Add($"Windfall: the ring ({ring}) and the thud ({thud}) do not land round the wizard standing under the tree ({under}).");
			if (leave <= under + 20f || spacing <= under)
				warnings.Add("Windfall: leaving a tree, shaking it and planting beside it overlap; he could arm a tree without leaving it.");
		}

		// The ripening is drawn green, swelling, then turning, in that order.
		if (Windfall.RipeningStage(0f) != 1 || Windfall.RipeningStage(0.5f) != 2 || Windfall.RipeningStage(0.99f) != 3)
			warnings.Add("Windfall: a ripening fruit is not drawn set, green, then turning (Windfall.RipeningStage).");

		// The cards' words against the numbers the node reads. Read through locals: a constant compared with a constant is
		// CS0162 at the warning it guards.
		float thorn = Windfall.ThornShare, lash = Windfall.LashShare, bump = Windfall.BumpEvery;
		if (thorn <= 0f || thorn >= 1f)
			warnings.Add($"Windfall: Bramble Fall's thorn strikes for {thorn:0%} of a fruit; a thorn is less than the fruit it split from.");
		if (lash <= 0f || lash >= 1f)
			warnings.Add($"Windfall: Iron Bough lashes for {lash:0%} of a fruit; a branch is less than the fruit.");
		if (bump < 0.3f || bump > 1.5f)
			warnings.Add($"Windfall: a tree may be bumped every {bump} s; Eric's rule is a short cooldown, so a crowd cannot strip it in a frame.");
		if (spell != null)
		{
			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			SpellEvolutionOption orchard = spell.Level4Options?.FirstOrDefault(o => o?.Id == Windfall.OrchardId);
			SpellEvolutionOption bramble = spell.Level4Options?.FirstOrDefault(o => o?.Id == Windfall.BrambleFallId);
			SpellEvolutionOption rot = spell.Level4Options?.FirstOrDefault(o => o?.Id == Windfall.CiderRotId);
			SpellEvolutionOption bough = spell.Level8Options?.FirstOrDefault(o => o?.Id == Windfall.IronBoughId);
			if (orchard == null || orchard.ProjectileCountBonus != 1)
				warnings.Add("Windfall: Orchard is missing or does not add one tree.");
			if (bramble == null || !bramble.Description.Contains($"{thorn * 100f:0}%") || !bramble.Description.Contains($"{Windfall.ThornsPerFruit} thorns"))
				warnings.Add($"Windfall: Bramble Fall's card does not say {Windfall.ThornsPerFruit} thorns striking for {thorn * 100f:0}%.");
			if (rot == null || rot.ReplacesElement != "Grass" || rot.BonusElementWeights?.ContainsKey("Poison") != true)
				warnings.Add("Windfall: Cider Rot is missing or does not trade Grass for Poison (Iron Bough deepens the Metal it keeps).");
			if (bough == null || !bough.Description.Contains($"{lash * 100f:0}%"))
				warnings.Add($"Windfall: Iron Bough's card does not say its branches strike for {lash * 100f:0}%.");
			if (spell.Level8Options?.Any(o => o?.Id == Windfall.HarvestMoonId) != true)
				warnings.Add($"Windfall: no level 8 card '{Windfall.HarvestMoonId}', which the node reads by id.");
		}

		// THE FIELDFARE pays it: a run one Grass or Metal kill short does not, one that makes the count does.
		AchievementDefinition fieldfare = AchievementDefinitions.GetById("fieldfare");
		if (fieldfare == null || fieldfare.SpellUnlockId != Windfall.SpellId)
			warnings.Add("Windfall: no Fieldfare feat pays Windfall, so nobody can ever hold it.");
		else
		{
			int target = AchievementDefinitions.FieldfareGrassMetalKills;
			if (fieldfare.IsComplete(AchievementContext.Projected(new SaveData(), new RunResult { GrassMetalKills = target - 1 })))
				warnings.Add($"Windfall: Fieldfare completes on {target - 1} Grass and Metal kills in a run; it asks for {target}.");
			if (!fieldfare.IsComplete(AchievementContext.Projected(new SaveData(), new RunResult { GrassMetalKills = target })))
				warnings.Add($"Windfall: a run with {target} Grass and Metal kills does not complete Fieldfare (LifetimeStats.BestGrassMetalKillsInRun).");
			// Kept across a save round trip, so the menu's bar survives a restart.
			var save = new SaveData();
			save.Lifetime.Absorb(new RunResult { GrassMetalKills = 42 });
			if (LifetimeStats.FromDictionary(save.Lifetime.ToGodotDictionary()).BestGrassMetalKillsInRun != 42)
				warnings.Add("Windfall: LifetimeStats.BestGrassMetalKillsInRun is lost through the save's round trip.");
		}
	}
}
