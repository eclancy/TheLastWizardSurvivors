using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// CORPOSANT (scripts/Corposant.cs, 2026-11-09). What a build cannot see and a run would only show as a ball that never
// turns, turns at the wrong edge, starts as a crowd, or a feat nobody can finish: the view's geometry on both canvases,
// the bounce rule, the one-ball start, the cards' words against the node's numbers, and the Storm Petrel feat.
public static partial class RegressionChecks
{
	private static void ValidateCorposant(List<string> warnings)
	{
		if (!ContentValidator.SpellResourcePaths.Contains("res://SpellData_Corposant.tres"))
			warnings.Add("Corposant: SpellData_Corposant.tres is not in ContentValidator.SpellResourcePaths, so nothing validates it.");

		SpellData spell = ContentValidator.LoadAllSpells().FirstOrDefault(s => s?.Id == Corposant.SpellId);
		if (spell == null)
			warnings.Add("Corposant: no loaded spell has the id 'corposant'.");
		else
		{
			// Spells start small (Eric, 2026-11-02): one ball at level 1.
			if (spell.GetProjectileCountAtLevel(1) != 1)
				warnings.Add($"Corposant: level 1 looses {spell.GetProjectileCountAtLevel(1)} balls; a spell starts with one on screen.");
			if (spell.TargetingMode != SpellTargetingMode.Unaimed)
				warnings.Add($"Corposant: its TargetingMode is {spell.TargetingMode}; it aims at nothing (Unaimed).");
			Dictionary<Element, int> weights = spell.GetElementWeights();
			if (weights.Count != 1 || !weights.ContainsKey(Element.Lightning))
				warnings.Add($"Corposant: it names {string.Join("/", weights.Keys)}; it is pure Lightning.");
			float first = Corposant.LifeAt(spell, 1), last = Corposant.LifeAt(spell, spell.MaxLevel);
			if (first < 2f || first > 5f || last < first)
				warnings.Add($"Corposant: a ball burns {first:0.##} s at level 1 and {last:0.##} s at its last; it burns a few seconds, never less as it levels.");
		}

		// THE VIEW. The camera's canvas transform at zoom 0.85 framing the wizard, on the PC's landscape canvas and the
		// phone's portrait one: the rectangle a ball turns at must be the canvas over the zoom, centred on him.
		Vector2 wizard = new(1234f, -567f);
		foreach (Vector2 canvas in new[] { new Vector2(1280f, 720f), new Vector2(720f, 1280f) })
		{
			const float zoom = 0.85f;
			// Camera2D's canvas transform: scale by the zoom, then put the wizard at the canvas's middle.
			var canvasTransform = new Transform2D(new Vector2(zoom, 0f), new Vector2(0f, zoom), canvas * 0.5f - wizard * zoom);
			Rect2 view = Corposant.WorldView(canvasTransform, new Rect2(Vector2.Zero, canvas));
			Vector2 expected = canvas / zoom;
			if (MathF.Abs(view.Size.X - expected.X) > 0.5f || MathF.Abs(view.Size.Y - expected.Y) > 0.5f
				|| view.GetCenter().DistanceTo(wizard) > 0.5f)
				warnings.Add($"Corposant: on a {canvas.X:0}x{canvas.Y:0} canvas the view is {view.Size.X:0}x{view.Size.Y:0} about "
					+ $"{view.GetCenter()}; the camera shows {expected.X:0}x{expected.Y:0} about the wizard at {wizard}.");
		}

		// THE BOUNCE. Past an edge heading out turns that axis; past it heading in (the camera ran past the ball) does not;
		// inside, nothing turns; a corner turns both.
		var box = new Rect2(-100f, -50f, 200f, 100f);
		Vector2 vel = new(300f, 40f);
		if (!Corposant.Bounce(new Vector2(101f, 0f), ref vel, box, out Vector2 at) || vel != new Vector2(-300f, 40f) || at.X != 100f)
			warnings.Add($"Corposant: a ball past the right edge heading out did not turn back off it (velocity {vel}).");
		vel = new Vector2(-300f, 40f);
		if (Corposant.Bounce(new Vector2(130f, 0f), ref vel, box, out _) || vel != new Vector2(-300f, 40f))
			warnings.Add("Corposant: a ball outside the view but already heading back in was turned again; it would be held outside.");
		vel = new Vector2(300f, 300f);
		if (Corposant.Bounce(Vector2.Zero, ref vel, box, out _))
			warnings.Add("Corposant: a ball inside the view was turned.");
		vel = new Vector2(-300f, -300f);
		if (!Corposant.Bounce(new Vector2(-101f, -51f), ref vel, box, out _) || vel != new Vector2(300f, 300f))
			warnings.Add($"Corposant: a ball out past a corner turned to {vel}; both axes turn there.");

		// The swept strike: a body beside the middle of a long frame's path is met, one past its end is not.
		if (Corposant.DistanceSquaredToSegment(new Vector2(50f, 10f), Vector2.Zero, new Vector2(100f, 0f)) > 100.01f
			|| Corposant.DistanceSquaredToSegment(new Vector2(130f, 0f), Vector2.Zero, new Vector2(100f, 0f)) < 899.9f)
			warnings.Add("Corposant: the swept strike does not measure a body against the frame's path.");

		// The cards' words against the numbers the node reads.
		// Read through locals: a constant compared with a constant is CS0162 at the warning it guards.
		float twin = Corposant.TwinShare, wake = Corposant.WakeShare, every = Corposant.StrikeEvery;
		if (twin <= 0f || twin >= 1f)
			warnings.Add($"Corposant: Twin Fires strikes for {twin:0%} a twin; a twin is less than the ball it split from.");
		if (wake <= 0f || wake >= 1f)
			warnings.Add($"Corposant: Static Wake zaps for {wake:0%} of a strike; a wake is less than the ball.");
		if (MathF.Abs(every - 0.5f) > 0.001f)
			warnings.Add($"Corposant: a ball strikes one body every {every} s; Eric's rule is about once every half second.");
		if (spell != null)
		{
			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			SpellEvolutionOption twins = spell.Level4Options?.FirstOrDefault(o => o?.Id == Corposant.TwinFiresId);
			SpellEvolutionOption wakeCard = spell.Level4Options?.FirstOrDefault(o => o?.Id == Corposant.StaticWakeId);
			SpellEvolutionOption spindrift = spell.Level4Options?.FirstOrDefault(o => o?.Id == Corposant.SpindriftId);
			if (twins == null || !twins.Description.Contains($"{twin * 100f:0}%"))
				warnings.Add($"Corposant: Twin Fires' card does not say its twins strike for {twin * 100f:0}%.");
			if (wakeCard == null || !wakeCard.Description.Contains($"{wake * 100f:0}%"))
				warnings.Add($"Corposant: Static Wake's card does not say it zaps for {wake * 100f:0}% of a strike.");
			if (spindrift == null || spindrift.Rider != ElementRider.Slow)
				warnings.Add("Corposant: Spindrift is missing or carries no slow; Water's rider is the drag (ElementPaths.RiderFor).");
			foreach (string id in new[] { Corposant.StormLanternId, Corposant.SaintsCrownId })
				if (spell.Level8Options?.Any(o => o?.Id == id) != true)
					warnings.Add($"Corposant: no level 8 card '{id}', which the node reads by id.");
		}

		// THE STORM PETREL pays it: a run one Lightning kill short does not, one that makes the count does.
		AchievementDefinition petrel = AchievementDefinitions.GetById("storm_petrel");
		if (petrel == null || petrel.SpellUnlockId != Corposant.SpellId)
			warnings.Add("Corposant: no Storm Petrel feat pays Corposant, so nobody can ever hold it.");
		else
		{
			int target = AchievementDefinitions.StormPetrelLightningKills;
			if (petrel.IsComplete(AchievementContext.Projected(new SaveData(), new RunResult { LightningKills = target - 1 })))
				warnings.Add($"Corposant: Storm Petrel completes on {target - 1} Lightning kills in a run; it asks for {target}.");
			if (!petrel.IsComplete(AchievementContext.Projected(new SaveData(), new RunResult { LightningKills = target })))
				warnings.Add($"Corposant: a run with {target} Lightning kills does not complete Storm Petrel (LifetimeStats.BestLightningKillsInRun).");
			// Kept across a save round trip, so the menu's bar survives a restart.
			var save = new SaveData();
			save.Lifetime.Absorb(new RunResult { LightningKills = 42 });
			if (LifetimeStats.FromDictionary(save.Lifetime.ToGodotDictionary()).BestLightningKillsInRun != 42)
				warnings.Add("Corposant: LifetimeStats.BestLightningKillsInRun is lost through the save's round trip.");
		}
	}
}
