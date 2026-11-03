using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

public static partial class RegressionChecks
{
	public static List<string> RunAll()
	{
		var warnings = new List<string>();
		ValidateDynamicRewardBounds(warnings);
		ValidateTelemetryHistoryCap(warnings);
		ValidateCharacterRosterBasics(warnings);
		ValidateSaveDefaults(warnings);
		ValidatePlaytestChecklistDefaults(warnings);
		ValidatePresetRewardOrdering(warnings);
		ValidateSpellEvolutionCoverage(warnings);
		ValidateApotheosisCatalog(warnings);
		ValidateComboCatalog(warnings);
		ValidateCodex(warnings);
		ValidateSovereignRite(warnings);
		ValidateChestSetPresentation(warnings);
		ValidateFullSetObjects(warnings);
		ValidateTomeRelics(warnings);
		ValidateCollectorEconomy(warnings);
		ValidateBossCatalog(warnings);
		ValidateBossPatterns(warnings);
		ValidateRangedEnemies(warnings);
		ValidateMiniBoss(warnings);
		ValidateAttackPatternEnemies(warnings);
		ValidateBestiary(warnings);
		ValidateStartingCharacters(warnings);
		ValidateElementTags(warnings);
		ValidateElementPaths(warnings);
		ValidateEvolutionReaders(warnings);
		ValidateCardTextBudget(warnings);
		ValidateLevelUpReaders(warnings);
		ValidateAmountReaders(warnings);
		ValidateStarfall(warnings);
		ValidateCycloneGust(warnings);
		ValidateFear(warnings);
		ValidateSecretCatalog(warnings);
		ValidateStageRewards(warnings);
		ValidateSketchbookHoldsNothing(warnings);
		ValidateWorldMap(warnings);
		ValidateBossRush(warnings);
		ValidateHazardPlacement(warnings);
		ValidateFamiliar(warnings);
		ValidateSpellVariants(warnings);
		ValidateSpiritualWeapon(warnings);
		ValidateFacingFamily(warnings);
		ValidateSpawnFormations(warnings);
		ValidateSwarmPressure(warnings);
		ValidateQuestsAndNotes(warnings);
		ValidateBoonSources(warnings);
		ValidateBoonPresentation(warnings);
		ValidateChestRarity(warnings);
		ValidateElementReachability(warnings);
		ValidateUnlockCatalog(warnings);
		ValidateAchievements(warnings);
		ValidateTakerTaken(warnings);
		ValidateStageCatalog(warnings);
		ValidateCampaign(warnings);
		ValidateCampaignMigration(warnings);
		ValidateDungeonMove(warnings);
		ValidateGateReachability(warnings);
		ValidateAchievementMigration(warnings);
		ValidateRetiredChargeRefund(warnings);
		ValidateDifficultyMigration(warnings);
		ValidateSpellBook(warnings);
		ValidateTeachers(warnings);
		ValidatePrologueReel(warnings);
		ValidateFreeings(warnings);
		ValidateSlowStartMigration(warnings);
		ValidateStoryReset(warnings);
		ValidateUserDataCarryOver(warnings);
		ValidateChapterDifficulty(warnings);
		ValidateNoPixelsInText(warnings);
		ValidateAudio(warnings);
		ValidateMusic(warnings);
		ValidateInputMap(warnings);
		ValidateBlockingTerrain(warnings);
		ValidateTerrainSeams(warnings);
		ValidateWaterShapes(warnings);
		ValidateGroundArt(warnings);
		ValidateNavigation(warnings);
		ValidateMazeHazards(warnings);
		ValidateStageHazards(warnings);
		ValidateStatusEffectAtlas(warnings);
		ValidateTelegraphIcons(warnings);
		ValidateFlowerPen(warnings);
		ValidateSpellClassificationText(warnings);
		ValidateExploreArt(warnings);
		ValidateStageHints(warnings);
		ValidateSideEventTable(warnings);
		ValidateSealPuzzle(warnings);
		ValidateNineMouths(warnings);
		ValidateWalkingHut(warnings);
		ValidateBarge(warnings);
		ValidateGibbetCage(warnings);
		ValidateSeersVoice(warnings);
		ValidateQuatrains(warnings);
		ValidateThawArt(warnings);
		ValidateGravewell(warnings);
		ValidateCraters(warnings);
		ValidateEvolutionIcons(warnings);
		return warnings;
	}

	// The chapter hints (StageHintCatalog) name things four other catalogs own - a chapter, a
	// discovery site, the side event that stands on it, a sound - and every one of them fails
	// silently in a run: an unknown stage never plays, a site its event does not pay never comes
	// from anywhere (SideEventDirector only starts a hint whose site is its own event's), a missing
	// sound is a caption with nothing under it. And one rule a build cannot see: a hint never says
	// the name of what it is about. A rescued wizard's name is his reward, and a found spell's name
	// in a caption is the answer printed on the question.
	private static void ValidateStageHints(List<string> warnings)
	{
		// The cues (tools/art/hint_cues.py): one row per HintCue, eight cells each, read by cell. A missing
		// or unimported sheet means a hint is heard and read but never seen.
		Texture2D cues = ResourceLoader.Exists(StageHintCue.SheetPath) ? GD.Load<Texture2D>(StageHintCue.SheetPath) : null;
		var cueSize = new Vector2(StageHintCue.Cell * StageHintCue.Columns, StageHintCue.Cell * Enum.GetValues<HintCue>().Length);
		if (cues == null)
			warnings.Add($"Hints: {StageHintCue.SheetPath} is missing or not imported (python tools/art/hint_cues.py, then import).");
		else if (cues.GetSize() != cueSize)
			warnings.Add($"Hints: hint-cues.png is {cues.GetSize()}, expected {cueSize} - one row per HintCue, in its order.");

		// Keyed by site (a chapter may hold two events, each with its own hint): one hint a site, and the site
		// one chapter's event - the chapter whose runs play it.
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (StageHint hint in StageHintCatalog.All)
		{
			string at = $"Hint '{hint.SiteId}'";
			if (!seen.Add(hint.SiteId ?? string.Empty))
				warnings.Add($"{at}: a site has two hints; ForSite only ever plays the first.");
			List<StageDefinition> chapters = SideEventDirector.ChaptersOfSite(hint.SiteId);
			if (chapters.Count == 0)
				warnings.Add($"{at}: no chapter's side event stands on that site, so the hint has nowhere to come from.");
			else if (chapters.Count > 1)
				warnings.Add($"{at}: {chapters.Count} chapters' side events stand on that site; a site is one place.");

			var gives = UnlockCatalog.ForDiscoverySite(hint.SiteId).ToList();
			if (gives.Count == 0)
				warnings.Add($"{at}: site '{hint.SiteId}' gives nothing in UnlockCatalog, so there is never anything to hint at.");

			if (!SfxCatalog.AllNames.Contains(hint.Sound))
				warnings.Add($"{at}: sound '{hint.Sound}' is not in SfxCatalog (tools/audio/sfx.py, then the catalog).");

			if (hint.Lines.Length == 0)
				warnings.Add($"{at}: has no lines.");

			names.Clear();
			foreach (UnlockDefinition give in gives)
			{
				names.Add(give.Id.Replace('_', ' '));
				if (give.Kind == UnlockKind.Character)
				{
					string name = CharacterRoster.GetAll().FirstOrDefault(c => c.Id == give.Id)?.Name;
					if (!string.IsNullOrWhiteSpace(name))
						names.Add(name);
				}
				else
				{
					string name = ContentValidator.LoadAllSpells().FirstOrDefault(s => s.Id == give.Id)?.Name;
					if (!string.IsNullOrWhiteSpace(name))
						names.Add(name);
				}
			}
			foreach (string line in hint.Lines)
			{
				if (string.IsNullOrWhiteSpace(line))
					warnings.Add($"{at}: has a blank line.");
				else if (line.Length > StageHintCatalog.MaxLineLength)
					warnings.Add($"{at}: \"{line}\" is {line.Length} characters (limit {StageHintCatalog.MaxLineLength}); it will not fit two lines on a phone.");
				foreach (string name in names)
				{
					if (line.Contains(name, StringComparison.OrdinalIgnoreCase))
						warnings.Add($"{at}: \"{line}\" names '{name}', which is the reward, not the hint.");
				}
			}
		}
	}

	// THE COUPLED SEAL (FivePointedSealEvent): a press turns its stone and the two beside it. Proved here rather
	// than argued: every one of the 256 states is reached from the solved seal by presses (so every start can
	// be solved - a press is undone by three more of it), the solver's answer is the fewest presses for every
	// state (its press counts sum to the breadth-first distance to solving it), and the seeded start is at least MinPresses
	// away on a thousand roll sets, the extremes included. A puzzle with an unreachable state would be a rescue
	// that can never be made, and nothing in a run would say so.
	private static void ValidateSealPuzzle(List<string> warnings)
	{
		const int n = FivePointedSealEvent.StoneCount;
		static int Code(int[] o) => o[0] | (o[1] << 2) | (o[2] << 4) | (o[3] << 6);
		static int[] Decode(int code) => new[] { code & 3, (code >> 2) & 3, (code >> 4) & 3, (code >> 6) & 3 };

		for (int plate = 0; plate < n; plate++)
		{
			int[] turned = FivePointedSealEvent.TurnedBy(plate);
			var set = new HashSet<int>(turned);
			if (turned.Length != 3 || !set.Contains(plate) || !set.Contains((plate + 1) % n) || !set.Contains((plate + n - 1) % n))
				warnings.Add($"Seal puzzle: plate {plate} turns stones {string.Join(",", turned)}, not its own and the two beside it.");
		}

		var distance = new int[256];
		Array.Fill(distance, -1);
		var queue = new Queue<int>();
		distance[0] = 0;
		queue.Enqueue(0);
		while (queue.Count > 0)
		{
			int code = queue.Dequeue();
			for (int plate = 0; plate < n; plate++)
			{
				int next = Code(FivePointedSealEvent.Press(Decode(code), plate));
				if (distance[next] >= 0)
					continue;
				distance[next] = distance[code] + 1;
				queue.Enqueue(next);
			}
		}
		int unreached = distance.Count(d => d < 0);
		if (unreached > 0)
			warnings.Add($"Seal puzzle: {unreached} of 256 states cannot be reached from the solved seal, so a start there can never be solved.");
		// Presses only ever add, so the presses that SOLVE a state are the ones that reach its negation from the
		// solved seal (a state plus them is zero): the breadth-first distance to -s is the fewest for s.
		for (int code = 0; code < 256; code++)
		{
			int[] state = Decode(code);
			int back = Code(new[] { (n - state[0]) % n, (n - state[1]) % n, (n - state[2]) % n, (n - state[3]) % n });
			if (distance[back] < 0)
				continue;
			int presses = FivePointedSealEvent.Sum(FivePointedSealEvent.Solve(state));
			if (presses != distance[back])
			{
				warnings.Add($"Seal puzzle: state {string.Join(",", state)} takes {distance[back]} presses to solve, but the solver says {presses}.");
				break;
			}
		}

		var rng = new Random(20261101);
		int fewest = int.MaxValue, most = 0;
		for (int t = 0; t < 1000 + 16; t++)
		{
			float[] rolls = t < 16
				? new[] { (t & 1) * 0.99f, ((t >> 1) & 1) * 0.99f, ((t >> 2) & 1) * 0.99f, ((t >> 3) & 1) * 0.99f }
				: new[] { (float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble() };
			int presses = FivePointedSealEvent.Sum(FivePointedSealEvent.Solve(FivePointedSealEvent.StartFromRolls(rolls)));
			fewest = Math.Min(fewest, presses);
			most = Math.Max(most, presses);
		}
		if (fewest < FivePointedSealEvent.MinPresses || most > 3 * n)
			warnings.Add($"Seal puzzle: seeded starts take {fewest} to {most} presses; every one should take {FivePointedSealEvent.MinPresses} to {3 * n}.");
	}

	// THE NINE MOUTHS (NineMouthsEvent, .ai/chapter-wizards.md 4.3). A listening puzzle is only fair if the
	// call never lies: on every call hers is FIRST and every mouth speaks exactly once (OrderFor, over a few
	// hundred seeds); a shift never lands where hers was, never on the mouth that answered, and never next door
	// (ShiftFrom); no two thresholds overlap, so standing in one mouth is never standing in two; the nine strips
	// tile the painting with no gap and no overlap; the painting is the size the strips assume; and the call's
	// beat is the one the hint's cue keeps (StageHintCue.SibylVoices, sfx._SIBYL_VOICES).
	private static void ValidateNineMouths(List<string> warnings)
	{
		const int n = NineMouthsEvent.MouthCount;
		var rng = new RandomNumberGenerator { Seed = 20261102UL };
		var order = new int[n];
		for (int trial = 0; trial < 400; trial++)
		{
			int first = trial % n;
			NineMouthsEvent.OrderFor(first, rng, order);
			var seen = new HashSet<int>(order);
			if (order[0] != first || seen.Count != n || order.Any(i => i < 0 || i >= n))
			{
				warnings.Add($"Nine mouths: a call from mouth {first} came out as [{string.Join(",", order)}]; hers must be first and every mouth speak once.");
				break;
			}
			int answered = (first + 1 + trial % (n - 1)) % n;
			int next = NineMouthsEvent.ShiftFrom(first, answered, rng);
			if (next == first || next == answered || Math.Abs(next - first) < 2 || next < 0 || next >= n)
			{
				warnings.Add($"Nine mouths: hers shifted from {first} (mouth {answered} answered) to {next}; it must move at least two mouths, and never onto the one that answered.");
				break;
			}
		}
		for (int i = 0; i + 1 < n; i++)
		{
			float d = NineMouthsEvent.MouthOffset(i).DistanceTo(NineMouthsEvent.MouthOffset(i + 1));
			if (d <= NineMouthsEvent.ThresholdRadius * 2f)
				warnings.Add($"Nine mouths: thresholds {i} and {i + 1} are {d:0} px apart, under two radii ({NineMouthsEvent.ThresholdRadius * 2f:0}): one spot would stand in both.");
		}
		int x = NineMouthsEvent.StripRect(0).Position.X;
		for (int i = 0; i < n; i++)
		{
			Rect2I r = NineMouthsEvent.StripRect(i);
			if (r.Position.X != x || r.Size.X <= 0)
				warnings.Add($"Nine mouths: strip {i} starts at column {r.Position.X}, not {x}; the cliff would show a gap or draw a column twice.");
			x = r.End.X;
		}
		const string art = "res://assets/bonelight/world/mouths/mouths-cliff.png";
		var cliff = ResourceLoader.Exists(art) ? GD.Load<Texture2D>(art) : null;
		if (cliff == null)
			warnings.Add($"Nine mouths: {art} is missing or not imported (python tools/art/nine_mouths.py, then import).");
		else if (cliff.GetWidth() < x || cliff.GetHeight() != NineMouthsEvent.StripRect(0).Size.Y)
			warnings.Add($"Nine mouths: the painting is {cliff.GetWidth()}x{cliff.GetHeight()}, but the strips cut it to {x}x{NineMouthsEvent.StripRect(0).Size.Y}.");
		float[] beats = StageHintCue.SibylVoiceBeats;
		for (int k = 1; k < beats.Length; k++)
		{
			if (Math.Abs(beats[k] - beats[k - 1] - NineMouthsEvent.VoiceBeat) > 0.001f)
			{
				warnings.Add($"Nine mouths: the hint's leaves come {beats[k] - beats[k - 1]:0.00} s apart; the call's beat is {NineMouthsEvent.VoiceBeat:0.00}.");
				break;
			}
		}
	}

	// The thaw's lake and block (tools/art/thaw_lake.py, ThawLake, ThawEvent): four textures the shader
	// reads by texel, which must be one size, and a sheet read by frame index. A missing one draws the
	// thaw on bare ground, or without its block, and says nothing in a run.
	private static void ValidateThawArt(List<string> warnings)
	{
		Vector2? lakeSize = null;
		foreach (string file in new[] { "thaw-lake.png", "thaw-lake-lit.png", "thaw-lake-bright.png", "thaw-lake-data.png" })
		{
			string path = ThawLake.Directory + file;
			Texture2D texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
			if (texture == null)
			{
				warnings.Add($"Thaw: {path} is missing or not imported (python tools/art/thaw_lake.py, then import).");
				continue;
			}
			lakeSize ??= texture.GetSize();
			if (texture.GetSize() != lakeSize.Value)
				warnings.Add($"Thaw: {file} is {texture.GetSize()}, but the lake is {lakeSize.Value}; the shader reads them texel for texel.");
		}
		if (lakeSize is Vector2 size && size * 2f != ThawLake.WorldAxes * 2f + new Vector2(64f, 64f))
			warnings.Add($"Thaw: the lake is {size} at x2, which no longer matches ThawLake.WorldAxes {ThawLake.WorldAxes} (and the event's footprint).");
		string block = ThawLake.Directory + "thaw-block.png";
		Texture2D sheet = ResourceLoader.Exists(block) ? GD.Load<Texture2D>(block) : null;
		if (sheet == null)
			warnings.Add($"Thaw: {block} is missing or not imported; the wizard will not be in the ice.");
		else if (sheet.GetSize() != new Vector2(64 * 16, 64))
			warnings.Add($"Thaw: thaw-block.png is {sheet.GetSize()}, expected 1024x64 (four stages of four frames).");
		if (!ResourceLoader.Exists(ThawLake.ShaderPath))
			warnings.Add($"Thaw: {ThawLake.ShaderPath} is missing; the lake will not draw.");
	}

	// Gravewell's pit is a sheet read by frame index, its collapse a shader loaded by path, and its
	// trigger is drawn as the art's bank - all three invisible to a build, and every one of them fails
	// silently in a run: a null texture draws nothing (which is how the spell came to read as nothing
	// at all), a missing shader draws no collapse, and a trigger moved without the art draws a bank
	// that lies about what springs it. Re-run: python tools/art/gravewell.py, then import.
	private static void ValidateGravewell(List<string> warnings)
	{
		const string scenePath = "res://scenes/Gravewell.tscn";
		if (GD.Load<PackedScene>(scenePath)?.Instantiate() is not GravewellTrap pit)
		{
			warnings.Add($"Gravewell: {scenePath} did not instantiate as a GravewellTrap.");
			return;
		}
		var pitSize = new Vector2(GravewellTrap.PitCell * (GravewellTrap.OpenFrames + GravewellTrap.ArmedFrames + GravewellTrap.CollapseFrames), GravewellTrap.PitCell);
		var graspSize = new Vector2(GravewellTrap.GraspW * GravewellTrap.GraspFrames, GravewellTrap.GraspH);
		if (pit.PitTexture == null)
			warnings.Add("Gravewell: the pit sheet is missing or not imported; the trap will be invisible.");
		else if (pit.PitTexture.GetSize() != pitSize)
			warnings.Add($"Gravewell: the pit sheet is {pit.PitTexture.GetSize()}, expected {pitSize}.");
		if (pit.GraspTexture == null)
			warnings.Add("Gravewell: the grasp sheet is missing or not imported; caught enemies will not read as held.");
		else if (pit.GraspTexture.GetSize() != graspSize)
			warnings.Add($"Gravewell: the grasp sheet is {pit.GraspTexture.GetSize()}, expected {graspSize}.");
		if (!ResourceLoader.Exists(GravewellTrap.CollapseShaderPath))
			warnings.Add($"Gravewell: {GravewellTrap.CollapseShaderPath} is missing; the collapse will draw nothing.");
		if (MathF.Abs(pit.TriggerRadius - GravewellTrap.ArtBankRadius * 2f) > 4f)
			warnings.Add($"Gravewell: TriggerRadius {pit.TriggerRadius} no longer matches the bank drawn at {GravewellTrap.ArtBankRadius * 2f} px.");
		pit.Free();
	}

	// The craters (Meteor Swarm, Cataclysm) are a shader loaded by path and a fire sheet read by cell
	// and row, and both fail silently in a run: a missing shader draws every crater as a plain quad of
	// its parameters' colour, an unimported sheet draws no fire at all, and a sheet of another shape
	// draws the wrong rows. Re-run: python tools/art/crater_fire.py, then import.
	private static void ValidateCraters(List<string> warnings)
	{
		if (!ResourceLoader.Exists(CraterGround.ShaderPath))
			warnings.Add($"Craters: {CraterGround.ShaderPath} is missing; every crater would draw as a flat quad.");
		Texture2D fire = ResourceLoader.Exists(CraterFire.TexturePath) ? GD.Load<Texture2D>(CraterFire.TexturePath) : null;
		var size = new Vector2(CraterFire.CellW * CraterFire.Frames, CraterFire.CellH * (CraterFire.RowSmoke + 1));
		if (fire == null)
			warnings.Add($"Craters: {CraterFire.TexturePath} is missing or not imported; craters would burn with no fire.");
		else if (fire.GetSize() != size)
			warnings.Add($"Craters: the fire sheet is {fire.GetSize()}, expected {size} ({CraterFire.Frames} frames of {CraterFire.CellW}x{CraterFire.CellH} in {CraterFire.RowSmoke + 1} rows).");
	}

	// Starfall (.ai/spell-roster.md, "Starfall") is one node the Player makes from its StarfallScene
	// export. Unassigned, FireStarfall returns NoTarget every frame and the spell owns a slot and does
	// nothing, with no error anywhere; a scene that is not a Starfall fails the cast the same way. Its
	// two ascensions and two of its level 4 cards (Shooting Stars, Skipping Stars) are read by id
	// (HasEvolution), so an option renamed in the catalog would leave a card that changes nothing - the
	// ids are checked against the options the spell actually offers.
	private static void ValidateStarfall(List<string> warnings)
	{
		const string scenePath = "res://scenes/Starfall.tscn";
		if (GD.Load<PackedScene>(scenePath)?.Instantiate() is not Starfall rain)
			warnings.Add($"Starfall: {scenePath} did not instantiate as a Starfall; the spell will never fall.");
		else
		{
			if (rain.BaseStarRadius <= 0f || rain.FallSeconds <= 0f)
				warnings.Add($"Starfall: BaseStarRadius {rain.BaseStarRadius} and FallSeconds {rain.FallSeconds} must both be positive.");
			rain.Free();
		}

		SceneState player = GD.Load<PackedScene>("res://scenes/player.tscn")?.GetState();
		bool assigned = false;
		for (int i = 0; player != null && i < player.GetNodePropertyCount(0); i++)
			assigned |= player.GetNodePropertyName(0, i) == "StarfallScene";
		if (!assigned)
			warnings.Add("Starfall: player.tscn does not assign StarfallScene; the spell would take a slot and do nothing.");

		SpellData spell = ContentValidator.LoadAllSpells().FirstOrDefault(s => s?.Id == "starfall");
		if (spell == null)
		{
			warnings.Add("Starfall: SpellData_Starfall.tres is not in ContentValidator.SpellResourcePaths.");
			return;
		}
		SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
		var level4 = spell.Level4Options.Select(o => o?.Id).ToList();
		var level8 = spell.Level8Options.Select(o => o?.Id).ToList();
		foreach (string id in new[] { Starfall.ShootingStarsId, Starfall.FrostfallId, Starfall.SkippingStarsId })
			if (!level4.Contains(id))
				warnings.Add($"Starfall: level 4 offers no '{id}'.");
		foreach (string id in new[] { Starfall.StarburstId, Starfall.ConstellationId })
			if (!level8.Contains(id))
				warnings.Add($"Starfall: level 8 offers no '{id}', so the node's behaviour for it is unreachable.");
	}

	// The stage secrets (issue #20, .ai/stage-secrets.md section 7.5). Every rule here guards something a
	// run cannot show: a secret that paid a spell, a wizard or a boon would put the campaign's last gate
	// behind a seed and a walk (there is no enum value for one, which is the first half of the rule);
	// a found level of Growth, Amount or a charge would undercut the Apotheosis floor measured with the
	// shop at its cap; and two keepsakes of one stat would let a save find it twice.
	// THE BOSS RUSH (issue #12, BossRushCatalog). Its roster is derived, not written, so what can go
	// wrong is the derivation: a campaign boss dropped or doubled, a finale that is not the arena's
	// (the Emberdeep's, StageCatalog.GateChapter: the run would end on the wrong fight - and a castle
	// boss built before C2's RoadOrder would land after it), or arrivals that are not strictly before the
	// ordinary boss time and in order (two bosses at once, or one after the finale has already won).
	// Traps fit their map (2026-10-28, "there shouldn't be fire vents outside. double check that all
	// traps make sense for the map they're in"). A flame vent is an iron grate set in a floor: indoors
	// only. Spike plates are built too: only where somebody built things. The lists are the decision.
	// The castle's three are written in now (.ai/endgame-castle.md 1.9) so its chapters' agents never edit
	// this file to place a trap: the Court and the Throne are indoors, and the bridge is built though open.
	private static readonly StageEnvironmentKind[] IndoorChapters =
		{ StageEnvironmentKind.Castle, StageEnvironmentKind.Court, StageEnvironmentKind.Throne };
	private static readonly StageEnvironmentKind[] BuiltChapters =
	{
		StageEnvironmentKind.Castle, StageEnvironmentKind.Ruins,
		StageEnvironmentKind.Bridge, StageEnvironmentKind.Court, StageEnvironmentKind.Throne,
	};

	private static void ValidateHazardPlacement(List<string> warnings)
	{
		foreach (StageEnvironmentKind kind in Enum.GetValues<StageEnvironmentKind>())
		{
			StageEnvironmentProfile profile = StageEnvironmentCatalog.Get(kind);
			if (profile.HazardFlameChance > 0f && !IndoorChapters.Contains(kind))
				warnings.Add($"Hazards: {kind} places flame vents ({profile.HazardFlameChance:0.##}), but it is outdoors - a vent is a grate in a floor.");
			if (profile.HazardSpikes && !BuiltChapters.Contains(kind))
				warnings.Add($"Hazards: {kind} places spike plates, but nobody built anything there.");
		}
	}

	private static void ValidateBossRush(List<string> warnings)
	{
		IReadOnlyList<BossDefinition> roster = BossRushCatalog.Roster;
		BossDefinition finale = BossRushCatalog.Finale;
		if (roster.Count < 2)
		{
			warnings.Add($"Boss Rush: the roster has {roster.Count} bosses; a rush needs at least two.");
			return;
		}
		if (finale == null || !ReferenceEquals(roster[roster.Count - 1], finale))
			warnings.Add($"Boss Rush: the roster ends on '{roster[roster.Count - 1].Id}', not the arena's boss '{finale?.Id}' ({BossRushCatalog.Arena?.DisplayName}).");
		if (roster.Select(b => b.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != roster.Count)
			warnings.Add("Boss Rush: a boss is in the roster twice.");
		foreach (BossDefinition boss in roster)
		{
			int stage = BossCatalog.StageIndexOf(boss);
			if (stage < 0 || StageCatalog.CampaignPositionOf(StageCatalog.GetByIndex(stage)?.Id) < 0)
				warnings.Add($"Boss Rush: '{boss.Id}' is not a campaign chapter's boss.");
		}

		// HELD AS IT WAS (.ai/endgame-castle.md 1.8): RoadOrder is an explicit list, so every id must name a
		// playable chapter whose boss has its own scene - a stand-in would be the Warden of the Deep twice -
		// and nothing derived from the campaign order may creep back in.
		foreach (string stageId in BossRushCatalog.RoadOrder)
		{
			StageDefinition stage = StageCatalog.GetById(stageId);
			BossDefinition boss = stage != null ? BossCatalog.ForStageIndex(stage.Index) : null;
			if (stage == null || boss == null)
				warnings.Add($"Boss Rush: RoadOrder names '{stageId}', which has no boss in BossCatalog.");
			else if (!stage.IsPlayable)
				warnings.Add($"Boss Rush: RoadOrder names {stage.DisplayName}, which is not playable.");
			else if (boss.IsOnStandIn || !ResourceLoader.Exists(boss.ScenePath))
				warnings.Add($"Boss Rush: {boss.DisplayName} ({stageId}) has no scene of its own at '{boss.ScenePath}'.");
		}
		if (roster.Count != BossRushCatalog.RoadOrder.Count)
			warnings.Add($"Boss Rush: RoadOrder names {BossRushCatalog.RoadOrder.Count} chapters and the roster has {roster.Count} bosses.");

		// A rush health is read only for a boss the rush sends; elsewhere it is a number nothing reads. And the
		// Gaoler moved: the rush must meet him at a health of its own, not his castle one.
		foreach (BossDefinition boss in BossCatalog.All.Where(b => b.BossRushHealth > 0))
		{
			if (!roster.Contains(boss))
				warnings.Add($"Boss Rush: {boss.DisplayName} has a BossRushHealth but is not in the rush.");
		}
		StageDefinition gate = StageCatalog.GateChapter;
		foreach (BossDefinition boss in roster)
		{
			StageDefinition home = StageCatalog.GetByIndex(BossCatalog.StageIndexOf(boss));
			if (gate != null && StageCatalog.CampaignPositionOf(home.Id) > StageCatalog.CampaignPositionOf(gate.Id)
				&& boss.BossRushHealth <= 0)
				warnings.Add($"Boss Rush: {boss.DisplayName} now guards a chapter past the gate, so his catalog health is a castle boss's; give him a BossRushHealth to meet him at the road's.");
		}

		// Who it opens to: every boss on the roster beaten (.ai/endgame-castle.md 10.2 item 3), or a save that
		// had it before schema 21. The Warden alone is no longer enough - the Gaoler is behind him now.
		var everyone = new SaveData();
		foreach (BossDefinition boss in roster)
			everyone.Lifetime.BossesDefeated.Add(boss.Id);
		if (!BossRushCatalog.IsOpen(everyone))
			warnings.Add("Boss Rush: a save with every boss on its roster beaten cannot open it.");
		if (BossRushCatalog.IsOpen(new SaveData()))
			warnings.Add("Boss Rush: a brand-new save can open it.");
		foreach (BossDefinition missing in roster)
		{
			var oneShort = new SaveData();
			foreach (BossDefinition boss in roster.Where(b => !ReferenceEquals(b, missing)))
				oneShort.Lifetime.BossesDefeated.Add(boss.Id);
			if (BossRushCatalog.IsOpen(oneShort))
				warnings.Add($"Boss Rush: it opens with {missing.DisplayName} never beaten, so he would be met there first.");
		}

		float previous = 0f;
		for (int i = 0; i < roster.Count; i++)
		{
			float at = BossRushCatalog.ArrivalPaceSeconds(i, roster.Count, Node2DGame.ReferenceRunSeconds);
			if (at <= previous || at > Node2DGame.ReferenceRunSeconds)
				warnings.Add($"Boss Rush: boss {i + 1} ('{roster[i].Id}') arrives at pace {at:0}s, after {previous:0}s; arrivals must climb to {Node2DGame.ReferenceRunSeconds:0}s.");
			previous = at;
		}
		if (!Mathf.IsEqualApprox(previous, Node2DGame.ReferenceRunSeconds))
			warnings.Add($"Boss Rush: the finale arrives at pace {previous:0}s, not at the ordinary boss time.");
	}

	// THE SECRETS (.ai/stage-secrets.md 7.5, with .ai/achievements-and-secrets.md 6.2's revised payment
	// rule): every secret written down, built or planned. A secret may pay a spell now - only a spell
	// vault, one a chapter, and only a spell the unlock catalog sources from that vault's deed - and
	// still never a wizard or a boon (SecretMetaReward has no value for either).
	private static void ValidateSecretCatalog(List<string> warnings)
	{
		var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var foundStats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var keepsakesByStage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var vaultsByStage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> spellNames = ContentValidator.LoadAllSpells()
			.Where(s => s != null && !string.IsNullOrWhiteSpace(s.Id))
			.GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
		foreach (SecretDefinition secret in SecretCatalog.Defined)
		{
			if (!ids.Add(secret.Id))
				warnings.Add($"Secrets: '{secret.Id}' is listed twice.");
			StageDefinition stage = StageCatalog.GetById(secret.StageId);
			if (stage == null)
				warnings.Add($"Secrets: '{secret.Id}' names a stage that is not in StageCatalog: '{secret.StageId}'.");
			if (string.IsNullOrWhiteSpace(secret.DisplayName) || string.IsNullOrWhiteSpace(secret.FoundLine))
				warnings.Add($"Secrets: '{secret.Id}' has no name or no found line for the Secrets page.");
			// Its deed is Hidden, and a hidden achievement once seen shows what was seen (4.3).
			if (string.IsNullOrWhiteSpace(secret.SeenLine))
				warnings.Add($"Secrets: '{secret.Id}' has no seen line, so its achievement has nothing to say once its tell has been seen.");
			if (secret.IsKeepsake && secret.IsVault)
				warnings.Add($"Secrets: '{secret.Id}' is both a keepsake and a spell vault; a chapter's keepsake and its vault are two secrets.");
			// How a built secret gets into a run (SecretDirector, piece A4): a placed one names its node, a
			// custom one its install; and a vault is placed as a vault (6.4), a keepsake as a keepsake.
			if (secret.IsBuilt && (secret.Placement == SecretPlacement.Custom ? secret.Install == null : secret.CreateSite == null))
				warnings.Add($"Secrets: '{secret.Id}' is built but has no {(secret.Placement == SecretPlacement.Custom ? "Install" : "CreateSite")}, so no run ever puts it in the world.");
			if (secret.IsVault && secret.Placement != SecretPlacement.Vault)
				warnings.Add($"Secrets: '{secret.Id}' is a spell vault placed as {secret.Placement}; a vault is placed five to seven chunks out (SecretPlacement.Vault).");
			if (secret.IsKeepsake && secret.Placement != SecretPlacement.Keepsake)
				warnings.Add($"Secrets: '{secret.Id}' is a keepsake placed as {secret.Placement}; a keepsake is placed ten to twelve chunks out (SecretPlacement.Keepsake).");

			bool paysSpell = secret.MetaReward == SecretMetaReward.Spell;
			if (paysSpell != !string.IsNullOrWhiteSpace(secret.SpellUnlockId))
				warnings.Add($"Secrets: '{secret.Id}' pays {secret.MetaReward} but names spell '{secret.SpellUnlockId}'; a spell is named exactly when one is paid.");
			if (secret.IsVault != paysSpell)
				warnings.Add($"Secrets: '{secret.Id}' {(secret.IsVault ? "is a spell vault that pays no spell" : "pays a spell but is not a spell vault")}; only a vault pays a spell, and a vault pays one.");
			if (secret.IsVault)
				vaultsByStage[secret.StageId] = (vaultsByStage.TryGetValue(secret.StageId, out int v) ? v : 0) + 1;
			if (paysSpell && !string.IsNullOrWhiteSpace(secret.SpellUnlockId))
			{
				UnlockDefinition source = UnlockCatalog.GetSpell(secret.SpellUnlockId);
				string deedId = AchievementDefinitions.SecretDeedId(secret.Id);
				if (source == null)
					warnings.Add($"Secrets: '{secret.Id}' pays spell '{secret.SpellUnlockId}', which has no unlock catalog entry.");
				else if (source.Source != UnlockSource.Achievement || !source.SourceId.Equals(deedId, StringComparison.OrdinalIgnoreCase))
					warnings.Add($"Secrets: '{secret.Id}' pays '{secret.SpellUnlockId}', but the catalog sources that spell from {source.Source} '{source.SourceId}', not this vault's deed '{deedId}'.");
				if (!spellNames.TryGetValue(secret.SpellUnlockId, out string name) || !name.Equals(secret.SpellName, StringComparison.Ordinal))
					warnings.Add($"Secrets: '{secret.Id}' names its spell '{secret.SpellName}', but '{secret.SpellUnlockId}' is called '{name ?? "(no resource)"}'.");
			}

			if (secret.IsKeepsake)
				keepsakesByStage[secret.StageId] = (keepsakesByStage.TryGetValue(secret.StageId, out int n) ? n : 0) + 1;
			else if (secret.MetaReward != SecretMetaReward.PageOnly && !secret.IsVault)
				warnings.Add($"Secrets: '{secret.Id}' is not a keepsake but pays {secret.MetaReward}; only a keepsake pays beyond the page, and a vault its spell.");
			if (secret.MetaReward == SecretMetaReward.FoundLevel)
			{
				if (!SecretCatalog.FoundLevelStats.Contains(secret.MetaRewardId, StringComparer.OrdinalIgnoreCase))
					warnings.Add($"Secrets: '{secret.Id}' pays a found level of '{secret.MetaRewardId}'; only {string.Join(", ", SecretCatalog.FoundLevelStats)} may be found.");
				else if (!foundStats.Add(secret.MetaRewardId))
					warnings.Add($"Secrets: a second keepsake pays a found level of '{secret.MetaRewardId}' ('{secret.Id}'); one of each, ever.");
			}
			if (secret.MetaReward == SecretMetaReward.ArcaneEnergy
				&& (secret.MetaAmount <= 0 || secret.MetaAmount > SecretCatalog.MaxKeepsakeArcane))
				warnings.Add($"Secrets: '{secret.Id}' pays {secret.MetaAmount} Arcane Energy; a keepsake pays 1 to {SecretCatalog.MaxKeepsakeArcane}.");
			if (secret.StageId.Equals(SketchbookStageId, StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Secrets: '{secret.Id}' is in the Sketchbook, which holds no secrets (every secret is a deed; ValidateSketchbookHoldsNothing).");
		}
		foreach (var (stageId, count) in keepsakesByStage)
			if (count > 1)
				warnings.Add($"Secrets: {stageId} has {count} keepsakes; a chapter has one.");
		foreach (var (stageId, count) in vaultsByStage)
			if (count > 1)
				warnings.Add($"Secrets: {stageId} has {count} spell vaults; a chapter has one at most.");
	}

	// WHAT EACH CHAPTER HOLDS (StageRewards, .ai/world-map.md section 9) is what the world map's card
	// shows as silhouettes. It is derived from four catalogs, and each of them can grow a chapter's
	// unlock that the derivation does not see: a discovery at a site no side event stands on, a reveal
	// chapter that is not a chapter, a deed for a boss with no chapter. Each is checked against the list.
	// Then the prose net: an unlock whose hint (or whose deed's description) NAMES a chapter must be on
	// that chapter's card. That is the check that would catch a new deed tied to a place in words but
	// not in data - the Coursing without its RevealStageId, say - which every other rule here would miss.
	private const string SketchbookStageId = "stage_8";

	/// <summary>
	/// The Sketchbook holds nothing to earn (Eric, 2026-11-02: "the sketchbook should not have any unlocks or
	/// any achievements. move that to other maps"): no deed of any kind names it or its boss, and its world
	/// map card is empty - StageRewards reads every source a chapter can pay from (warden, side event, a
	/// deed done there, secrets), so an empty card means nothing anywhere points at it. Its boss's
	/// ArcaneVictoryBonus stays: that is every boss's win purse, not an unlock.
	/// </summary>
	private static void ValidateSketchbookHoldsNothing(List<string> warnings)
	{
		StageDefinition sketchbook = StageCatalog.GetById(SketchbookStageId);
		BossDefinition boss = sketchbook != null ? BossCatalog.ForStageIndex(sketchbook.Index) : null;
		foreach (AchievementDefinition deed in AchievementDefinitions.All)
		{
			if (deed.StageId.Equals(SketchbookStageId, StringComparison.OrdinalIgnoreCase)
				|| (boss != null && deed.BossId.Equals(boss.Id, StringComparison.OrdinalIgnoreCase)))
				warnings.Add($"Sketchbook: the deed '{deed.Id}' is tied to the Sketchbook, which holds no achievements.");
		}
		foreach (StageReward reward in StageRewards.ForStage(SketchbookStageId))
			warnings.Add($"Sketchbook: its card pays {reward}; the Sketchbook holds no unlocks.");
		if (SideEventDirector.EventsFor(SketchbookStageId).Count > 0)
			warnings.Add("Sketchbook: it has a side event; the Sketchbook holds no unlocks.");
	}

	private static void ValidateStageRewards(List<string> warnings)
	{
		IReadOnlyList<StageReward> all = StageRewards.All;
		bool OnCard(string stageId, string id) => all.Any(r =>
			r.StageId.Equals(stageId, StringComparison.OrdinalIgnoreCase) && r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
		// A spell a chapter's vault pays is on that chapter's card as the vault's secret slot (planned
		// vaults join the card when built): the link is in data, SecretDefinition.StageId.
		bool Has(string stageId, string id) => OnCard(stageId, id) || SecretCatalog.Defined.Any(s =>
			s.StageId.Equals(stageId, StringComparison.OrdinalIgnoreCase) && s.SpellUnlockId.Equals(id, StringComparison.OrdinalIgnoreCase));

		foreach (UnlockDefinition found in UnlockCatalog.All.Where(d => !string.IsNullOrWhiteSpace(d.SiteId)))
		{
			List<StageDefinition> sites = global::SideEventDirector.ChaptersOfSite(found.SiteId);
			if (sites.Count != 1)
				warnings.Add($"Stage rewards: '{found.Id}' is found at site '{found.SiteId}', which {(sites.Count == 0 ? "no chapter's side event stands on" : $"{sites.Count} chapters' side events stand on")}, so no single chapter's card can show it.");
			else if (!OnCard(sites[0].Id, found.Id))
				warnings.Add($"Stage rewards: '{found.Id}' is found in {sites[0].DisplayName} but its card does not list it.");
		}

		foreach (UnlockDefinition revealed in UnlockCatalog.All.Where(d => !string.IsNullOrWhiteSpace(d.RevealStageId)))
		{
			StageDefinition stage = StageCatalog.GetById(revealed.RevealStageId);
			if (stage == null)
				warnings.Add($"Stage rewards: '{revealed.Id}' is revealed by '{revealed.RevealStageId}', which is not a chapter.");
			else if (!OnCard(stage.Id, revealed.Id))
				warnings.Add($"Stage rewards: '{revealed.Id}' is tied to {stage.DisplayName} but its card does not list it.");
		}

		foreach (AchievementDefinition deed in AchievementDefinitions.All.Where(d => !string.IsNullOrWhiteSpace(d.BossId)))
		{
			BossDefinition boss = BossCatalog.GetById(deed.BossId);
			int index = boss != null ? BossCatalog.StageIndexOf(boss) : -1;
			if (index < 0)
			{
				warnings.Add($"Stage rewards: the deed '{deed.Id}' is for boss '{deed.BossId}', who has no chapter.");
				continue;
			}
			StageDefinition stage = StageCatalog.GetByIndex(index);
			foreach (string id in RewardIdsOf(deed))
			{
				if (!OnCard(stage.Id, id))
					warnings.Add($"Stage rewards: {stage.DisplayName}'s warden pays '{id}' ('{deed.Id}') but its card does not list it.");
			}
		}

		foreach (SecretDefinition secret in SecretCatalog.All)
		{
			if (StageCatalog.GetById(secret.StageId) != null && !OnCard(secret.StageId, secret.Id))
				warnings.Add($"Stage rewards: the secret '{secret.Id}' is in {secret.StageId} but its card does not list it.");
		}

		// The prose net.
		var texts = new List<(string Text, IEnumerable<string> Ids)>();
		foreach (UnlockDefinition unlock in UnlockCatalog.All.Where(d => d.Source != UnlockSource.Starter))
			texts.Add((unlock.LockedHint, new[] { unlock.Id }));
		foreach (AchievementDefinition deed in AchievementDefinitions.All)
		{
			List<string> ids = RewardIdsOf(deed).ToList();
			if (ids.Count > 0)
				texts.Add((deed.Description, ids));
		}
		foreach (var (text, ids) in texts)
		{
			if (string.IsNullOrWhiteSpace(text))
				continue;
			foreach (StageDefinition stage in StageCatalog.All)
			{
				string name = stage.DisplayName.StartsWith("The ", StringComparison.Ordinal) ? stage.DisplayName.Substring(4) : stage.DisplayName;
				if (text.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0)
					continue;
				foreach (string id in ids)
				{
					if (!Has(stage.Id, id))
						warnings.Add($"Stage rewards: '{id}' is described as {stage.DisplayName}'s (\"{text}\") but is not on its card - tie it to the chapter in data (a boss deed, a side-event site, or RevealStageId).");
				}
			}
		}

		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (StageReward reward in all)
		{
			if (!seen.Add(reward.ToString()))
				warnings.Add($"Stage rewards: {reward} is listed twice.");
		}
		// The Sketchbook is the one chapter meant to hold nothing (ValidateSketchbookHoldsNothing); the map
		// card simply has no "What he keeps here" there.
		foreach (StageDefinition stage in StageCatalog.All.Where(s => s.IsPlayable && !s.Id.Equals(SketchbookStageId, StringComparison.OrdinalIgnoreCase)))
		{
			if (!all.Any(r => r.StageId.Equals(stage.Id, StringComparison.OrdinalIgnoreCase)))
				warnings.Add($"Stage rewards: {stage.DisplayName} holds nothing, so its card has no row - give its boss a deed.");
		}
	}

	private static IEnumerable<string> RewardIdsOf(AchievementDefinition deed)
	{
		if (!string.IsNullOrWhiteSpace(deed.SpellUnlockId))
			yield return deed.SpellUnlockId;
		if (!string.IsNullOrWhiteSpace(deed.BoonUnlockId))
			yield return deed.BoonUnlockId;
		if (!string.IsNullOrWhiteSpace(deed.CharacterUnlockId))
			yield return deed.CharacterUnlockId;
		// Arcane Energy is listed under the deed's own id, but only for a chapter's own deed: a
		// lifetime-total deed that pays Energy belongs to no chapter and names none.
		if (deed.CurrencyReward > 0 && !string.IsNullOrWhiteSpace(deed.BossId))
			yield return deed.Id;
	}

	// THE WORLD MAP'S GEOMETRY is generated (tools/art/world_map.py -> worldmap.json), so a chapter
	// added to StageCatalog is on no map until somebody places it - and the screen would quietly leave
	// it out. Every chapter and the Boss Rush must have a stop; the road must visit the campaign in its
	// order and the seat after the last chapter; a chapter held until the campaign is finished (the
	// Sketchbook) is on its own page and never on the road (.ai/world-map.md, section 4); and every
	// picture the screen draws must exist.
	private static void ValidateWorldMap(List<string> warnings)
	{
		WorldMapLayout map = WorldMapLayout.Load(out string problem);
		if (map == null)
		{
			warnings.Add($"World map: {problem}");
			return;
		}
		// A chapter not built yet may stand off the map for now (.ai/endgame-castle.md 1.9): the world map
		// is its own piece (C8), and drawing the castle waits on the Dungeon's move. Its missing stop is
		// then an item on that chapter's campaign to-do line (ValidateCampaignToDo), and the screen leaves
		// it out quietly (WorldMap.BuildStops). A PLAYABLE chapter with no stop is a fault, as always, so
		// none can flip with its place undrawn - except one held until the campaign is finished, whose
		// place is the page.
		foreach (StageDefinition stage in StageCatalog.All)
		{
			if (map.Get(stage.Id) == null && stage.IsPlayable && !stage.AfterCampaign)
				warnings.Add($"World map: {stage.DisplayName} ({stage.Id}) has no place on the map - add it to PLACES in tools/art/world_map.py and draw its landmark.");
		}
		WorldMapStop rush = map.Get(WorldMapLayout.BossRushId);
		if (rush == null)
			warnings.Add("World map: the Boss Rush has no place on the map.");

		// THE STOP-ORDER RULE (WorldMap.RoadOrder, .ai/endgame-castle.md 8): the road reaches every stop in
		// its order - the campaign to its gate chapter, the Boss Rush on the gate's summit, the castle across
		// the water, then anything outside the campaign - so the wizard never walks back from the tower to
		// the summit, and a chapter outside the campaign stands past its end.
		float last = -1f;
		string lastName = "the start of the road";
		foreach (var (stage, isRush) in WorldMap.RoadOrder(withRush: true))
		{
			WorldMapStop stop = map.Get(isRush ? WorldMapLayout.BossRushId : stage.Id);
			if (stop == null)
				continue;
			string name = isRush ? "the Boss Rush's seat" : stage.DisplayName;
			if (stop.ArcLength <= last)
				warnings.Add($"World map: the road reaches {name} before {lastName}, against the stop order (WorldMap.RoadOrder) - reorder PLACES in tools/art/world_map.py.");
			last = Math.Max(last, stop.ArcLength);
			lastName = name;
			if (!isRush && StageCatalog.IsInCampaign(stage)
				&& (!stop.OnMap || stop.Focus.X < 0 || stop.Focus.Y < 0 || stop.Focus.X > map.Size.X || stop.Focus.Y > map.Size.Y))
				warnings.Add($"World map: {stage.DisplayName} is a campaign chapter but stands off the world picture.");
		}
		// Eric, 2026-10-30: "sketchbook level should not be attached to the map ... it shows up as its own
		// separate map". A stop for it would put it back in the road's flow and on the compass.
		foreach (StageDefinition stage in StageCatalog.All.Where(s => !StageCatalog.IsInCampaign(s) && s.AfterCampaign))
		{
			if (map.Get(stage.Id) != null)
				warnings.Add($"World map: {stage.DisplayName} is held until the campaign is finished and belongs on its own page, but it stands on the road (remove it from PLACES in tools/art/world_map.py).");
		}

		// The map unrolls as the campaign is won (GlobalStatsManager.IsChapterRevealed): a new save is shown
		// the first chapter and nothing past it, and no chapter a save can enter is ever hidden from it -
		// checked on the new save and on one with every chapter opened.
		var fresh = new SaveData();
		var opened = new SaveData();
		foreach (StageDefinition stage in StageCatalog.Campaign)
			GlobalStatsManager.UnlockStage(opened, stage.Id);
		foreach (StageDefinition stage in StageCatalog.Campaign)
		{
			bool first = StageCatalog.CampaignPositionOf(stage.Id) == 0;
			if (GlobalStatsManager.IsChapterRevealed(fresh, stage) != first)
				warnings.Add($"World map: on a new save {stage.DisplayName} is {(first ? "hidden" : "shown")}; only the first chapter should be.");
			foreach (SaveData save in new[] { fresh, opened })
			{
				if (GlobalStatsManager.IsStageAvailable(save, stage) && !GlobalStatsManager.IsChapterRevealed(save, stage))
					warnings.Add($"World map: {stage.DisplayName} can be entered but is hidden on the map.");
			}
		}

		// The page: its sheet drawn, the wizard's stand on the paper, the kids' drawings on the sheet.
		if (StageCatalog.All.Any(s => s.AfterCampaign && s.IsPlayable))
		{
			if (string.IsNullOrEmpty(map.PageImage) || map.PageSize.X <= 0f || map.PageSize.Y <= 0f)
				warnings.Add("World map: a chapter is held for the page, but worldmap.json has no page to show it on.");
			else if (!map.PageRect.HasPoint(map.PageStand))
				warnings.Add($"World map: the wizard's stand on the page ({map.PageStand}) is off the paper ({map.PageRect}).");
		}

		var pictures = new List<string> { "world.png", map.PageImage, "ring-open.png", "ring-locked.png", "ring-current.png",
			"shadow.png", "secret.png", "keepsake.png", "arrow-left.png", "arrow-right.png" };
		foreach (WorldMapCompass.MarkKind kind in Enum.GetValues(typeof(WorldMapCompass.MarkKind)))
			pictures.Add("mark-" + kind.ToString().ToLowerInvariant() + ".png");
		foreach (WorldMapStop stop in map.Stops)
		{
			if (!string.IsNullOrEmpty(stop.ShroudImage))
				pictures.Add(stop.ShroudImage);
		}
		foreach (string picture in pictures)
		{
			if (!ResourceLoader.Exists(WorldMapLayout.Directory + picture))
				warnings.Add($"World map: {WorldMapLayout.Directory + picture} is missing or not imported.");
		}
		foreach (var (texture, at) in map.PageDrawings)
		{
			if (!ResourceLoader.Exists(texture))
				warnings.Add($"World map: the page's drawing {texture} is missing.");
			else if (!map.PageRect.HasPoint(at))
				warnings.Add($"World map: the page's drawing {texture} is placed off the paper, at {at}.");
		}
	}

	// Cyclone Slash's running whirlwind (issue #68) reads its one Ascension by id: Typhoon Core is the
	// whirlwind that never collapses. Renamed in the catalog, the card would still promise a permanent
	// hurricane and the ring would still fall in every time the player stopped.
	private static void ValidateCycloneGust(List<string> warnings)
	{
		SpellData spell = ContentValidator.LoadAllSpells().FirstOrDefault(s => s?.Id == "cyclone_slash");
		if (spell == null)
		{
			warnings.Add("Cyclone Slash: SpellData_CycloneSlash.tres is not in ContentValidator.SpellResourcePaths.");
			return;
		}
		SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
		if (!spell.Level8Options.Any(o => o?.Id == OrbitingBlade.TyphoonEvolutionId))
			warnings.Add($"Cyclone Slash: level 8 offers no '{OrbitingBlade.TyphoonEvolutionId}', so the permanent hurricane always collapses.");
	}

	// Fear (issue #69) is decided entirely inside Enemy.ApplyFear, and every way it can go wrong is
	// silent in a run: a fear that never lands, or one that lands on a boss and stops the fight. Checked
	// on a bare enemy that is never added to the tree.
	private static void ValidateFear(List<string> warnings)
	{
		const string scenePath = "res://scenes/enemy.tscn";
		if (GD.Load<PackedScene>(scenePath)?.Instantiate() is not Enemy ordinary)
		{
			warnings.Add($"Fear: {scenePath} did not instantiate as an Enemy.");
			return;
		}
		ordinary.ApplyFear(Player.FearSeconds);
		if (!ordinary.IsFeared)
			warnings.Add("Fear: an ordinary enemy did not take ApplyFear; Darkness's fear never lands.");
		ordinary.Free();

		if (GD.Load<PackedScene>(scenePath)?.Instantiate() is Enemy elite)
		{
			elite.IsMiniBoss = true;
			elite.ApplyFear(Player.FearSeconds);
			if (elite.IsFeared)
				warnings.Add("Fear: an elite or boss (IsMiniBoss) took ApplyFear; it must be immune.");
			elite.Free();
		}
		if (Player.FearChance <= 0f || Player.FearChance >= 0.5f || Player.FearSeconds <= 0f)
			warnings.Add($"Fear: chance {Player.FearChance} and {Player.FearSeconds} s are outside what the tag card describes as a chance.");
	}

	// The Familiar (.ai/spell-roster.md, "Familiar") is one node the Player makes from its FamiliarScene
	// export, taken as a dog, a crow or a cat (SpellData.Variants), and every look it can take is a
	// SpriteFrames file loaded by name. All of it fails silently in a run: an unassigned export takes a slot
	// and summons nothing, a missing or unimported sheet is an invisible creature biting things, and a
	// pounce or howl strip shorter than the six frames Familiar.cs sets by hand throws on the frame index.
	// Its fifteen milestones (five a form) are read by id (HasEvolution), so an option renamed in the catalog
	// would be a card that changes nothing; and the two the save remembers from before forms must still
	// name milestones that exist (Familiar.RenamedMilestones). Re-run: python tools/art/familiar.py, then import.
	private static void ValidateFamiliar(List<string> warnings)
	{
		const string scenePath = "res://scenes/Familiar.tscn";
		if (GD.Load<PackedScene>(scenePath)?.Instantiate() is not Familiar pack)
			warnings.Add($"Familiar: {scenePath} did not instantiate as a Familiar; the spell would summon nothing.");
		else
		{
			if (pack.RunSpeed <= 0f || pack.LeapSeconds <= 0f || pack.BaseBiteRadius <= 0f || pack.BlinkDistance <= 0f)
				warnings.Add("Familiar: RunSpeed, LeapSeconds, BaseBiteRadius and BlinkDistance must all be positive.");
			pack.Free();
		}

		SceneState player = GD.Load<PackedScene>("res://scenes/player.tscn")?.GetState();
		bool assigned = false;
		for (int i = 0; player != null && i < player.GetNodePropertyCount(0); i++)
			assigned |= player.GetNodePropertyName(0, i) == "FamiliarScene";
		if (!assigned)
			warnings.Add("Familiar: player.tscn does not assign FamiliarScene; the spell would take a slot and summon nothing.");

		foreach (string form in Familiar.FormNames)
		{
			string path = Familiar.FramesPath(form);
			if (!ResourceLoader.Exists(path) || GD.Load<SpriteFrames>(path) is not SpriteFrames frames)
			{
				warnings.Add($"Familiar: {path} is missing or not imported; that form would be invisible.");
				continue;
			}
			foreach (string anim in new[] { "idle", "run", "pounce", "howl" })
			{
				if (!frames.HasAnimation(anim))
					warnings.Add($"Familiar: {path} has no '{anim}' animation.");
				else if (anim is "pounce" or "howl" && frames.GetFrameCount(anim) != 6)
					warnings.Add($"Familiar: {path} '{anim}' has {frames.GetFrameCount(anim)} frames; Familiar.cs sets frames 0-5 by hand.");
			}
		}

		SpellData spell = ContentValidator.LoadAllSpells().FirstOrDefault(s => s?.Id == "familiar");
		if (spell == null)
		{
			warnings.Add("Familiar: SpellData_Familiar.tres is not in ContentValidator.SpellResourcePaths.");
			return;
		}
		var expected = new Dictionary<string, (string[] Four, string[] Eight)>
		{
			[Familiar.DogId] = (new[] { Familiar.GreatHoundId, Familiar.RimehoundId, Familiar.LittermatesId },
				new[] { Familiar.CerberusId, Familiar.AnnwnId }),
			[Familiar.CrowId] = (new[] { Familiar.RavenId, Familiar.StormcrowId, Familiar.MurderId },
				new[] { Familiar.MorriganId, Familiar.GriffinId }),
			// The cat's four level 4 cards: Nine Lives moved down from level 8 for the Black Cat (2026-10-31).
			[Familiar.CatId] = (new[] { Familiar.GrimalkinId, Familiar.CaitSithId, Familiar.PurrId, Familiar.NineLivesId },
				new[] { Familiar.CathPalugId, Familiar.BlackCatId }),
		};
		var everyMilestone = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (variantId, (four, eight)) in expected)
		{
			SpellData form = ContentValidator.LoadAllSpellForms().FirstOrDefault(s => s.SelectedVariantId == variantId);
			if (form == null)
			{
				warnings.Add($"Familiar: no form '{variantId}' in SpellData_Familiar.tres; Familiar.FormOf would fall back to the dog for it.");
				continue;
			}
			if (Familiar.FormOf(form) == Familiar.Kind.Dog && variantId != Familiar.DogId)
				warnings.Add($"Familiar: form '{variantId}' reads as the dog (Familiar.FormOf).");
			var level4 = form.Level4Options.Select(o => o?.Id).ToList();
			var level8 = form.Level8Options.Select(o => o?.Id).ToList();
			everyMilestone.UnionWith(level4.Concat(level8).Where(id => id != null));
			foreach (string id in four)
				if (!level4.Contains(id))
					warnings.Add($"Familiar: '{variantId}' offers no level 4 '{id}', so the look and behaviour Familiar reads for it are unreachable.");
			foreach (string id in eight)
				if (!level8.Contains(id))
					warnings.Add($"Familiar: '{variantId}' offers no level 8 '{id}', so the look and behaviour Familiar reads for it are unreachable.");
		}
		foreach (var renamed in Familiar.RenamedMilestones)
			if (!everyMilestone.Contains(renamed.Value))
				warnings.Add($"Familiar: the save's old '{renamed.Key}' reads as '{renamed.Value}' (SaveData.IsAscensionSeen), which no form offers.");
		// The cat grants and the others do not: a dog or a crow that granted cast speed would be the cat's
		// whole worth handed to a form that already out-damages it.
		foreach (SpellData form in ContentValidator.LoadAllSpellForms().Where(s => s.Id == "familiar"))
		{
			bool grants = Familiar.GrantsOf(form).Any;
			if (grants != (Familiar.FormOf(form) == Familiar.Kind.Cat))
				warnings.Add($"Familiar: '{form.SelectedVariantId}' {(grants ? "grants" : "grants nothing")}; only the cat grants the wizard anything.");
		}
		ValidateCoursing(warnings);
	}

	// THE COURSING (AchievementDefinitions "coursing", which frees Laelaps): a Black Hare run down by a hound.
	// What fails silently: the hare's scene naming another EnemyType (Familiar.Strike would count nothing,
	// and the deed could never be done), the deed's measure not reading the run's count or the lifetime best
	// (a deed done and never granted), the best not surviving the save, and the hound counted as a wizard
	// by the final chapter's gate (an optional deed made mandatory, and the Emberdeep closed, live, to every
	// save that had opened it).
	private static void ValidateCoursing(List<string> warnings)
	{
		const string harePath = "res://scenes/HeathBlackHare.tscn";
		if (GD.Load<PackedScene>(harePath)?.Instantiate() is not Enemy hare)
			warnings.Add($"Coursing: {harePath} did not instantiate as an Enemy; the deed's quarry is missing.");
		else
		{
			if (hare.EnemyType != Familiar.CoursedEnemyType)
				warnings.Add($"Coursing: the Black Hare's EnemyType is '{hare.EnemyType}', but Familiar counts '{Familiar.CoursedEnemyType}'; the deed could never be done.");
			hare.Free();
		}

		AchievementDefinition deed = AchievementDefinitions.GetById("coursing");
		if (deed == null)
		{
			warnings.Add("Coursing: no 'coursing' achievement; Laelaps can never be freed.");
			return;
		}
		if (!deed.CharacterUnlockId.Equals("laelaps", StringComparison.OrdinalIgnoreCase))
			warnings.Add($"Coursing: the deed frees '{deed.CharacterUnlockId}', not Laelaps.");
		var shortRun = new global::RunResult { HaresCoursed = AchievementDefinitions.CoursingTarget - 1, StageId = "stage_10" };
		var fullRun = new global::RunResult { HaresCoursed = AchievementDefinitions.CoursingTarget, StageId = "stage_10" };
		if (deed.IsComplete(AchievementContext.ForRun(new SaveData(), shortRun)))
			warnings.Add($"Coursing: {AchievementDefinitions.CoursingTarget - 1} hares completes the deed; it asks for {AchievementDefinitions.CoursingTarget}.");
		if (!deed.IsComplete(AchievementContext.ForRun(new SaveData(), fullRun)))
			warnings.Add("Coursing: a run that coursed enough hares does not complete the deed; it reads neither the run nor the lifetime best.");
		var save = new SaveData();
		save.Lifetime.Absorb(fullRun);
		LifetimeStats back = LifetimeStats.FromDictionary(save.Lifetime.ToGodotDictionary());
		if (back.BestHaresCoursed != AchievementDefinitions.CoursingTarget || !deed.IsComplete(AchievementContext.ForSave(new SaveData { Lifetime = back })))
			warnings.Add("Coursing: the best run's hares do not survive the save (LifetimeStats.BestHaresCoursed), so the menu would forget the deed.");

		if (UnlockCatalog.AllWizardIds.Contains("laelaps", StringComparer.OrdinalIgnoreCase))
			warnings.Add("Coursing: Laelaps is counted by the final chapter's gate (UnlockCatalog.AllWizardIds); he is not a wizard, and his deed is optional.");
		if (!UnlockCatalog.AllCharacterIds.Contains("laelaps", StringComparer.OrdinalIgnoreCase))
			warnings.Add("Coursing: Laelaps is missing from UnlockCatalog.AllCharacterIds, so the tools that own everything do not own him.");
	}

	// SPELL FORMS (SpellData.Variants; .ai/decisions/2026-10-28-spell-variants.md). A form replaces its
	// spell's name, icon, numbers, level table and milestones in the run's copy, and is chosen by its id in a
	// level-up selection whose second half means "form" for a new spell and "milestone" for a held one -
	// none of which a build can see. What this holds, for every spell with forms:
	//   - every form has an id that is unique, starts with "<spell id>_", and is no milestone's id; a name, a
	//     prompt for the page, and an icon of its own (not the spell's, not a sibling's);
	//   - every form's milestones are its own: three level 4s and two level 8s, ids starting with the form's
	//     id, shared with no other form - which is what gives each its own icon and its own seen-ascension
	//     record - and a reader row of its own (EvolutionStatReaders.TryGet(SpellData));
	//   - a form never changes the spell's element tags (the reachability work counts the spell once);
	//   - its table is levels 2 to MaxLevel, in order;
	//   - no word only a form has (its prompt, name, words, traits) gives a size in pixels (2026-10-28, Eric:
	//     "never mention px, add a %"). Its description and milestones are ValidateNoPixelsInText's, which
	//     walks every form.

	private static void ValidateSpellVariants(List<string> warnings)
	{
		var variantIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var evolutionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var formsById = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);
		foreach (SpellData spell in ContentValidator.LoadAllSpellForms())
		{
			foreach (SpellEvolutionOption option in (spell.Level4Options ?? new()).Concat(spell.Level8Options ?? new()))
			{
				if (option != null && !string.IsNullOrEmpty(option.Id))
					evolutionIds.Add(option.Id);
			}
			if (!string.IsNullOrEmpty(spell.SelectedVariantId))
				formsById[spell.SelectedVariantId] = spell;
		}

		foreach (SpellData template in ContentValidator.LoadAllSpells())
		{
			if (template == null || !template.HasVariants)
				continue;
			string id = template.Id;
			if (string.IsNullOrWhiteSpace(template.VariantPrompt))
				warnings.Add($"Spell forms: '{id}' has forms but no VariantPrompt, so the page of them would ask nothing.");
			CheckNoPixels(warnings, id, template.Name, template.Description, template.VariantPrompt);
			var icons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (template.Icon?.ResourcePath is string own && own.Length > 0)
				icons.Add(own);
			var milestoneOwner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (SpellVariant variant in template.Variants)
			{
				if (variant == null || string.IsNullOrWhiteSpace(variant.Id))
				{
					warnings.Add($"Spell forms: '{id}' has a form with no id; it could never be chosen.");
					continue;
				}
				string vid = variant.Id;
				if (!variantIds.Add(vid))
					warnings.Add($"Spell forms: form id '{vid}' is used twice.");
				if (!vid.StartsWith(id + "_", StringComparison.OrdinalIgnoreCase))
					warnings.Add($"Spell forms: '{vid}' does not start with '{id}_'; a form's id is its spell's, extended.");
				if (evolutionIds.Contains(vid))
					warnings.Add($"Spell forms: '{vid}' is also a milestone's id, so 'spell:{vid}' would mean two things (SpellVariants.Selection).");
				if (string.IsNullOrWhiteSpace(variant.DisplayName) || string.IsNullOrWhiteSpace(variant.ShortName))
					warnings.Add($"Spell forms: '{vid}' needs a DisplayName and a ShortName.");
				string iconPath = variant.Icon?.ResourcePath ?? string.Empty;
				if (variant.Icon == null)
					warnings.Add($"Spell forms: '{vid}' has no icon; its card, the loadout strip and the results would show the spell's.");
				else if (iconPath.Length > 0 && !icons.Add(iconPath))
					warnings.Add($"Spell forms: '{vid}' shares its icon ({iconPath}) with its spell or a sibling; each form has its own.");
				CheckNoPixels(warnings, vid, variant.DisplayName, variant.Description, variant.Traits);
				int previous = 0;
				foreach (SpellLevelUpgrade upgrade in variant.LevelUpgrades ?? new())
				{
					if (upgrade == null || upgrade.Level < 2 || upgrade.Level > template.MaxLevel || upgrade.Level < previous)
						warnings.Add($"Spell forms: '{vid}' has a level-up out of order or outside 2..{template.MaxLevel}.");
					previous = upgrade?.Level ?? previous;
				}
				// The Familiar's cat deals no damage by design (2026-10-31: it debuffs and buffs); every other form must.
				if (variant.BaseCooldown <= 0f || variant.BaseProjectileCount < 1 || (variant.BaseDamage < 1 && vid != Familiar.CatId))
					warnings.Add($"Spell forms: '{vid}' needs a positive damage, cooldown and count.");

				if (!formsById.TryGetValue(vid, out SpellData form))
				{
					warnings.Add($"Spell forms: '{vid}' could not be applied to a copy of '{id}' (SpellVariants.Preview).");
					continue;
				}
				if (!EvolutionStatReaders.HasRow(vid))
					warnings.Add($"Spell forms: '{vid}' has no row of its own in EvolutionStatReaders; its milestones are checked against the spell's.");
				var baseTags = template.GetElementWeights();
				var formTags = form.GetElementWeights();
				if (baseTags.Count != formTags.Count || baseTags.Any(p => !formTags.TryGetValue(p.Key, out int w) || w != p.Value))
					warnings.Add($"Spell forms: '{vid}' changes '{id}'s element tags; a form is a way of playing the spell, never a different carrier.");
				// Three level 4 cards a form, but the Familiar's cat: Nine Lives moved down from level 8 to make room for
				// the Black Cat (2026-10-31), and the mutation page counts its branches (LevelUpMenu).
				int fours = vid == Familiar.CatId ? 4 : 3;
				if ((form.Level4Options?.Count ?? 0) != fours || (form.Level8Options?.Count ?? 0) != 2)
					warnings.Add($"Spell forms: '{vid}' has {form.Level4Options?.Count ?? 0} level 4 and {form.Level8Options?.Count ?? 0} level 8 milestones; every form has {fours} and 2 of its own.");
				foreach (SpellEvolutionOption option in (form.Level4Options ?? new()).Concat(form.Level8Options ?? new()))
				{
					if (option == null)
						continue;
					if (!option.Id.StartsWith(vid + "_", StringComparison.OrdinalIgnoreCase))
						warnings.Add($"Spell forms: '{vid}' offers '{option.Id}', which is not its own (a form's milestone ids start with '{vid}_').");
					if (milestoneOwner.TryGetValue(option.Id, out string other) && !other.Equals(vid, StringComparison.OrdinalIgnoreCase))
						warnings.Add($"Spell forms: '{option.Id}' is offered by both '{other}' and '{vid}'.");
					milestoneOwner[option.Id] = vid;
				}
			}
		}
	}

	private static void CheckNoPixels(List<string> warnings, string owner, params string[] texts)
	{
		foreach (string text in texts)
		{
			if (!string.IsNullOrEmpty(text) && PixelUnit.IsMatch(text))
				warnings.Add($"Spell forms: '{owner}' says \"{text}\"; a size on a card is a percentage, never pixels.");
		}
	}

	// Spiritual Weapon (scripts/SpiritualWeapon.cs): one node the Player makes from an export, drawing each
	// form from a pre-rotated sheet, and every milestone read by its id. What a build cannot see: the scene
	// unassigned or the wrong type (a slot that summons nothing), a sheet missing or not imported (an
	// invisible weapon), a sheet of the wrong shape (the wrong heading for every frame), and a milestone the
	// node reads by an id the catalog no longer offers (a card that changes nothing).
	private static void ValidateSpiritualWeapon(List<string> warnings)
	{
		const string scenePath = "res://scenes/SpiritualWeaponScene.tscn";
		if (GD.Load<PackedScene>(scenePath)?.Instantiate() is not SpiritualWeapon node)
			warnings.Add($"Spiritual Weapon: {scenePath} did not instantiate as a SpiritualWeapon; the spell would swing nothing.");
		else
			node.Free();

		SceneState player = GD.Load<PackedScene>("res://scenes/player.tscn")?.GetState();
		bool assigned = false;
		for (int i = 0; player != null && i < player.GetNodePropertyCount(0); i++)
			assigned |= player.GetNodePropertyName(0, i) == "SpiritualWeaponScene";
		if (!assigned)
			warnings.Add("Spiritual Weapon: player.tscn does not assign SpiritualWeaponScene; no spell would fire at all (Player stops the fire loop without it).");

		// The dagger is Twin Blades' (2026-10-30), on the projectile cell.
		foreach (var (name, cell) in new[] { ("axe", 48), ("hammer", 48), ("sword", 48), ("greataxe", 96), ("maul", 96), ("greatsword", 96), ("dagger", 32) })
		{
			string path = $"res://assets/bonelight/effects/spirit-{name}.png";
			if (!ResourceLoader.Exists(path) || GD.Load<Texture2D>(path) is not Texture2D sheet)
			{
				warnings.Add($"Spiritual Weapon: {path} is missing or not imported; that form would be invisible. Run tools/art/spirit_weapons.py and import.");
				continue;
			}
			if (sheet.GetWidth() != cell * 32 || sheet.GetHeight() % cell != 0)
				warnings.Add($"Spiritual Weapon: {path} is {sheet.GetWidth()}x{sheet.GetHeight()}; SpiritualWeapon draws 32 headings of a {cell} cell a row.");
		}
		foreach (string problem in SpiritualWeapon.CutProblems())
			warnings.Add($"Spiritual Weapon: {problem}");

		SpellData template = ContentValidator.LoadAllSpells().FirstOrDefault(s => s?.Id == "spiritual_weapon");
		if (template == null)
		{
			warnings.Add("Spiritual Weapon: SpellData_SpiritualWeapon.tres is not in ContentValidator.SpellResourcePaths.");
			return;
		}
		var expected = new Dictionary<string, (string[] Four, string[] Eight)>
		{
			[SpiritualWeapon.AxeId] = (new[] { SpiritualWeapon.BroadaxeId, SpiritualWeapon.HeadsmanId, SpiritualWeapon.DawnsplitterId },
				new[] { SpiritualWeapon.GreataxeId, SpiritualWeapon.WhirlingAxesId }),
			[SpiritualWeapon.HammerId] = (new[] { SpiritualWeapon.AftershockId, SpiritualWeapon.EarthshakerId, SpiritualWeapon.HallowedGroundId },
				new[] { SpiritualWeapon.TitansMaulId, SpiritualWeapon.HammerfallId }),
			[SpiritualWeapon.SwordId] = (new[] { SpiritualWeapon.TwinBladesId, SpiritualWeapon.RiposteId, SpiritualWeapon.SoulSiphonId },
				new[] { SpiritualWeapon.GreatswordId, SpiritualWeapon.ThousandCutsId }),
		};
		foreach (var (variantId, (four, eight)) in expected)
		{
			SpellData form = ContentValidator.LoadAllSpellForms().FirstOrDefault(s => s.SelectedVariantId == variantId);
			if (form == null)
			{
				warnings.Add($"Spiritual Weapon: no form '{variantId}' in the .tres; SpiritualWeapon.FormOf would fall back to the axe for it.");
				continue;
			}
			var level4 = form.Level4Options.Select(o => o?.Id).ToList();
			var level8 = form.Level8Options.Select(o => o?.Id).ToList();
			foreach (string id in four)
				if (!level4.Contains(id))
					warnings.Add($"Spiritual Weapon: '{variantId}' offers no level 4 '{id}', which SpiritualWeapon reads by id.");
			foreach (string id in eight)
				if (!level8.Contains(id))
					warnings.Add($"Spiritual Weapon: '{variantId}' offers no level 8 '{id}', which SpiritualWeapon reads by id.");
		}
	}

	// The facing family (.ai/spell-roster.md): Stormsweep and Sleet Runes are each one node the Player
	// makes from an export. Unassigned, or a scene that is not the right type, and the fire helper
	// returns NoTarget every frame - the spell owns a slot and does nothing, with no error anywhere.
	// A rune is one region of a pre-rotated sheet, and a sheet of the wrong shape draws the wrong
	// heading for every rune without failing. Their ascensions are read by id (HasEvolution), so an
	// option renamed in the catalog would leave a card that changes nothing.
	private static void ValidateFacingFamily(List<string> warnings)
	{
		if (GD.Load<PackedScene>("res://scenes/Stormsweep.tscn")?.Instantiate() is not Stormsweep sweep)
			warnings.Add("Stormsweep: res://scenes/Stormsweep.tscn did not instantiate as a Stormsweep; the spell will never sweep.");
		else
		{
			if (sweep.BaseHalfBand <= 0f || sweep.ArcGap <= 0f)
				warnings.Add($"Stormsweep: BaseHalfBand {sweep.BaseHalfBand} and ArcGap {sweep.ArcGap} must both be positive.");
			sweep.Free();
		}

		if (GD.Load<PackedScene>("res://scenes/SleetRunes.tscn")?.Instantiate() is not SleetRunes runes)
			warnings.Add("Sleet Runes: res://scenes/SleetRunes.tscn did not instantiate as a SleetRunes; no rune will fly.");
		else
		{
			if (runes.BaseSpeed <= 0f || runes.StreamGap <= 0f || runes.HitRadius <= 0f)
				warnings.Add($"Sleet Runes: BaseSpeed {runes.BaseSpeed}, StreamGap {runes.StreamGap} and HitRadius {runes.HitRadius} must all be positive.");
			Texture2D sheet = runes.RuneSheet;
			if (sheet == null)
				warnings.Add("Sleet Runes: the scene has no RuneSheet (or assets/bonelight/effects/sleet-rune.png is not imported); runes would fly invisible.");
			else if (sheet.GetWidth() != sheet.GetHeight() * SleetRunes.Directions)
				warnings.Add($"Sleet Runes: the rune sheet is {sheet.GetWidth()}x{sheet.GetHeight()}, not {SleetRunes.Directions} square cells in a row; every rune would be drawn at the wrong heading.");
			// Great Rune draws from its own sheet, drawn again at twice the size; without it a great rune
			// would be drawn as a small one and hit twice as wide as it looks.
			Texture2D greatSheet = runes.GreatRuneSheet;
			if (greatSheet == null)
				warnings.Add("Sleet Runes: the scene has no GreatRuneSheet (or assets/bonelight/effects/sleet-rune-great.png is not imported); a great rune would be drawn small.");
			else if (greatSheet.GetWidth() != greatSheet.GetHeight() * SleetRunes.Directions)
				warnings.Add($"Sleet Runes: the great rune sheet is {greatSheet.GetWidth()}x{greatSheet.GetHeight()}, not {SleetRunes.Directions} square cells in a row.");
			runes.Free();
		}

		// Aegis Ward's Reforged shards are drawn from their own pre-rotated sheet (tools/art/aegis_ward.py),
		// loaded by path because the node is made in code; without it they would cut unseen.
		Texture2D shardSheet = GD.Load<Texture2D>(AegisWard.ShardSheetPath);
		if (shardSheet == null)
			warnings.Add($"Aegis Ward: {AegisWard.ShardSheetPath} did not load (not imported?); Reforged's shards would cut unseen.");
		else if (shardSheet.GetWidth() != shardSheet.GetHeight() * AegisWard.ShardDirections)
			warnings.Add($"Aegis Ward: the shard sheet is {shardSheet.GetWidth()}x{shardSheet.GetHeight()}, not {AegisWard.ShardDirections} square cells in a row; every shard would be drawn at the wrong heading.");

		SceneState player = GD.Load<PackedScene>("res://scenes/player.tscn")?.GetState();
		foreach (string export in new[] { "StormsweepScene", "SleetRunesScene" })
		{
			bool assigned = false;
			for (int i = 0; player != null && i < player.GetNodePropertyCount(0); i++)
				assigned |= player.GetNodePropertyName(0, i) == export;
			if (!assigned)
				warnings.Add($"Facing family: player.tscn does not assign {export}; the spell would take a slot and do nothing.");
		}

		CheckOptionIds(warnings, "stormsweep",
			new[] { Stormsweep.BallLightningId, Stormsweep.IronCurrentId, Stormsweep.ForkedLightningId },
			new[] { Stormsweep.ThunderclapId, Stormsweep.CardinalStormId });
		CheckOptionIds(warnings, "sleet_runes",
			new[] { SleetRunes.GreatRuneId, SleetRunes.BraidedWindId, SleetRunes.PartingWindId },
			new[] { SleetRunes.SigilFanId, SleetRunes.TurningGaleId });
	}

	private static void CheckOptionIds(List<string> warnings, string spellId, string[] level4, string[] level8)
	{
		SpellData spell = ContentValidator.LoadAllSpells().FirstOrDefault(s => s?.Id == spellId);
		if (spell == null)
		{
			warnings.Add($"Facing family: '{spellId}' is not in ContentValidator.SpellResourcePaths.");
			return;
		}
		SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
		var offered4 = spell.Level4Options.Select(o => o?.Id).ToList();
		var offered8 = spell.Level8Options.Select(o => o?.Id).ToList();
		foreach (string id in level4)
			if (!offered4.Contains(id))
				warnings.Add($"Facing family: '{spellId}' level 4 offers no '{id}'.");
		foreach (string id in level8)
			if (!offered8.Contains(id))
				warnings.Add($"Facing family: '{spellId}' level 8 offers no '{id}', so the node's behaviour for it is unreachable.");
	}

	// The pause screen's spell details are written in SpellClassificationText's words. A new enum
	// value without a phrase would fall back to its code name on screen, which is the thing that
	// was reported as unreadable - so a missing phrase is a warning here, not a silent fallback.
	private static void ValidateSpellClassificationText(List<string> warnings)
	{
		foreach (SpellTargetingMode mode in Enum.GetValues<SpellTargetingMode>())
			if (string.IsNullOrEmpty(SpellClassificationText.Aim(mode)))
				warnings.Add($"Spell details: SpellTargetingMode.{mode} has no plain-English phrase in SpellClassificationText.Aim.");
		foreach (SpellDamageShape shape in Enum.GetValues<SpellDamageShape>())
			if (string.IsNullOrEmpty(SpellClassificationText.Shape(shape)))
				warnings.Add($"Spell details: SpellDamageShape.{shape} has no plain-English phrase in SpellClassificationText.Shape.");
		foreach (SpellScalingTag tag in Enum.GetValues<SpellScalingTag>())
			if (string.IsNullOrEmpty(SpellClassificationText.Scaling(tag)))
				warnings.Add($"Spell details: SpellScalingTag.{tag} has no plain-English word in SpellClassificationText.Scaling.");
	}

	// The exploration art (.ai/side-events.md, "Finding the event"): every sprite explore.json names
	// is imported and stands inside its own picture, the lantern knows where its flame is, every
	// glyph an arrow can carry exists, and every event chapter's wayfinder has a sprite. A missing
	// PNG draws nothing and says nothing (CLAUDE.md), which here would be a lantern that is only
	// light, or an arrow with no pictogram in it. Re-run: python tools/art/explore_props.py, then import.
	private static void ValidateExploreArt(List<string> warnings)
	{
		Dictionary<string, ExploreSprite> sprites = ExploreArt.Load();
		foreach (string id in new[] { "bog-lantern", "arcane-compass", "gaolers-map", "bearing-cairn" })
		{
			if (!sprites.TryGetValue(id, out ExploreSprite s))
			{
				warnings.Add($"Explore: '{id}' is missing from {ExploreArt.Manifest}.");
				continue;
			}
			if (s.Texture == null)
				warnings.Add($"Explore: '{id}.png' is not imported; it will not be drawn.");
			if (s.Frames <= 0 || s.Ground <= 0 || s.Ground > s.FrameH || s.AnchorX <= 0 || s.AnchorX >= s.FrameW)
				warnings.Add($"Explore: '{id}' has its ground line or foot column outside its frame.");
			if (id == "bog-lantern" && (s.Flame == Vector2.Zero || s.LitFrames <= 0 || s.LitFrames >= s.Frames))
				warnings.Add("Explore: the bog lantern needs a flame position and fewer lit frames than frames (the last is dark).");
		}
		foreach (string glyph in ExploreArt.Glyphs)
		{
			if (!ResourceLoader.Exists(ExploreArt.GlyphPath(glyph)))
				warnings.Add($"Explore: arrow glyph '{glyph}' is missing or not imported ({ExploreArt.GlyphPath(glyph)}).");
		}
		foreach (bool wayfinder in new[] { false, true })
		{
			if (!ResourceLoader.Exists(ExploreArt.BadgePath(wayfinder)))
				warnings.Add($"Explore: arrow badge {ExploreArt.BadgePath(wayfinder)} is missing or not imported.");
		}
		foreach (StageDefinition stage in StageCatalog.All)
		{
			WayfinderKind? wayfinder = SideEventDirector.WayfinderFor(stage.Id);
			if (wayfinder != null && Array.IndexOf(ExploreArt.Glyphs, WayfinderPickup.GlyphFor(wayfinder.Value)) < 0)
				warnings.Add($"Explore: {stage.Id}'s wayfinder {wayfinder.Value} has no arrow glyph.");
		}
	}

	// The stage hazards (StageHazard): the plate and the grate on the floor ARE the danger area - there
	// is no marker any more - and the player's boots are tested against the CollisionShape2D. So the
	// idle frame (the plate or grate alone, nothing standing up off it) must cover that shape's bounds
	// to the pixel, and every phase must have its animation: a missing `tell` is a hazard that bites
	// with no warning, which nothing else would notice. Re-run: python tools/art/props_fx.py, then import.
	private static void ValidateStageHazards(List<string> warnings)
	{
		foreach (string path in new[] { "res://scenes/SpikeTrap.tscn", "res://scenes/FlameVent.tscn" })
		{
			var scene = GD.Load<PackedScene>(path);
			Node root = scene?.Instantiate();
			if (root is not StageHazard)
			{
				warnings.Add($"Hazards: {path} did not load as a StageHazard.");
				root?.Free();
				continue;
			}
			var sprite = root.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
			var area = root.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			SpriteFrames frames = sprite?.SpriteFrames;
			if (frames == null || area?.Shape == null)
			{
				warnings.Add($"Hazards: {path} has no SpriteFrames or no collision shape.");
				root.Free();
				continue;
			}
			foreach (string anim in new[] { "idle", "tell", "active", "cool" })
			{
				if (!frames.HasAnimation(anim) || frames.GetFrameCount(anim) == 0)
					warnings.Add($"Hazards: {path} has no '{anim}' animation, so that phase shows nothing.");
			}
			Rect2 want = area.Shape switch
			{
				CircleShape2D circle => new Rect2(-circle.Radius, -circle.Radius, circle.Radius * 2f, circle.Radius * 2f),
				RectangleShape2D rect => new Rect2(-rect.Size / 2f, rect.Size),
				_ => new Rect2(),
			};
			want.Position += area.Position;
			if (frames.HasAnimation("idle") && frames.GetFrameTexture("idle", 0) is AtlasTexture idle
				&& idle.Atlas?.GetImage() is Image sheet)
			{
				if (sheet.IsCompressed()) sheet.Decompress();
				Rect2I region = (Rect2I)idle.Region;
				Rect2I used = sheet.GetRegion(region).GetUsedRect();
				// The frame is centred on the node (AnimatedSprite2D.Centered), then shifted by its offset.
				Vector2 corner = -new Vector2(region.Size.X, region.Size.Y) / 2f + sprite.Offset;
				var drawn = new Rect2(corner + used.Position, used.Size);
				if (Mathf.Abs(drawn.Position.X - want.Position.X) > 1f || Mathf.Abs(drawn.Position.Y - want.Position.Y) > 1f
					|| Mathf.Abs(drawn.Size.X - want.Size.X) > 1f || Mathf.Abs(drawn.Size.Y - want.Size.Y) > 1f)
					warnings.Add($"Hazards: {path}'s idle art covers {drawn} but it hurts on {want}: the plate on the floor no longer shows where it bites.");
			}
			else
			{
				warnings.Add($"Hazards: {path}'s idle frame is not an imported atlas frame, so its footprint cannot be checked.");
			}
			root.Free();
		}
	}

	// The status marks (StatusEffectLayer) are drawn out of one atlas by row and frame index, so a
	// sheet regenerated with a row added, or never imported at all, would draw the wrong mark or
	// nothing - silently either way, because DrawTextureRectRegion on a null or short texture does
	// not complain. Re-run: python tools/art/status_fx.py, then import.
	private static void ValidateStatusEffectAtlas(List<string> warnings)
	{
		if (!ResourceLoader.Exists(StatusEffectLayer.AtlasPath))
		{
			warnings.Add($"Status marks: {StatusEffectLayer.AtlasPath} is missing or not imported; no status effects will show on enemies.");
			return;
		}
		Texture2D atlas = GD.Load<Texture2D>(StatusEffectLayer.AtlasPath);
		var expected = new Vector2(StatusEffectLayer.Cell * StatusEffectLayer.Frames, StatusEffectLayer.Cell * StatusEffectLayer.Rows);
		if (atlas == null)
			warnings.Add($"Status marks: {StatusEffectLayer.AtlasPath} exists but did not load as a texture.");
		else if (atlas.GetSize() != expected)
			warnings.Add($"Status marks: {StatusEffectLayer.AtlasPath} is {atlas.GetSize()}, expected {expected} ({StatusEffectLayer.Rows} rows of {StatusEffectLayer.Frames} {StatusEffectLayer.Cell}px frames).");
	}

	// The attack icons (TelegraphIcon, 0.9.2: "if you need to telegraph an attack, do so with icon
	// animation"). Two silent failures. The atlas is addressed by glyph row and state column, so a sheet
	// regenerated with a row added, or never imported, draws the wrong glyph or nothing. And an enemy
	// class that holds a wind-up clock but never says what it is winding up (Enemy.ReadAttackTell) gets
	// no icon node at all - its attack lands with no tell over it, which is the one thing a telegraph
	// may never do. Re-run: python tools/art/telegraph_icons.py, then import.
	private static void ValidateTelegraphIcons(List<string> warnings)
	{
		if (!ResourceLoader.Exists(TelegraphIcon.AtlasPath))
			warnings.Add($"Attack icons: {TelegraphIcon.AtlasPath} is missing or not imported; no enemy will show a wind-up.");
		else
		{
			Texture2D atlas = GD.Load<Texture2D>(TelegraphIcon.AtlasPath);
			var expected = new Vector2(TelegraphIcon.Cell * TelegraphIcon.Columns, TelegraphIcon.Cell * TelegraphIcon.Rows);
			if (atlas == null)
				warnings.Add($"Attack icons: {TelegraphIcon.AtlasPath} exists but did not load as a texture.");
			else if (atlas.GetSize() != expected)
				warnings.Add($"Attack icons: {TelegraphIcon.AtlasPath} is {atlas.GetSize()}, expected {expected} ({TelegraphIcon.Rows} glyph rows of {TelegraphIcon.Columns} {TelegraphIcon.Cell}px states).");
		}

		const System.Reflection.BindingFlags Fields = System.Reflection.BindingFlags.Instance
			| System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
		foreach (Type type in typeof(Enemy).Assembly.GetTypes())
		{
			if (!typeof(Enemy).IsAssignableFrom(type) || type == typeof(Enemy) || type.IsAbstract)
				continue;
			bool holdsClock = false;
			for (Type t = type; t != null && t != typeof(Enemy); t = t.BaseType)
				holdsClock |= t.GetFields(Fields | System.Reflection.BindingFlags.DeclaredOnly)
					.Any(f => f.FieldType == typeof(AttackTelegraph) || f.FieldType == typeof(GroundSlamAttack));
			if (!holdsClock)
				continue;
			var read = type.GetMethod("ReadAttackTell", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
				null, new[] { typeof(TelegraphGlyph).MakeByRefType(), typeof(float).MakeByRefType() }, null);
			if (read == null || read.DeclaringType == typeof(Enemy))
				warnings.Add($"Attack icons: {type.Name} holds a wind-up clock but does not override Enemy.ReadAttackTell, so its attacks show no icon.");
		}
	}

	// Enemy navigation on a hand-made lake (.ai/terrain.md, "Enemy navigation"). Three things a build
	// cannot see and a playtest only shows as "the swarm piles up on the shore again":
	//   - a field built a slice at a time over several frames must equal one built whole, or the
	//     resumable queue has lost or double-counted a cell;
	//   - the straight-walk test must see the lake, or no enemy on an open stage ever reads the field;
	//   - the flow on the far side must go round the lake, the short way, not into it.
	// Also that a ring of water seals its middle off, so nothing is ever spawned on an island.
	private static void ValidateNavigation(List<string> warnings)
	{
		const int size = 40;
		const int tile = 48;
		var solid = new bool[size * size];
		var wall = new bool[size * size];
		// A lake from x 12 to 30, y 15 to 20: the way round its left end (x 11) is shorter than its
		// right (x 31) for anything standing above x 20.
		for (int y = 15; y <= 20; y++)
			for (int x = 12; x <= 30; x++)
				solid[x + y * size] = true;
		// A one-tile ring of water with dry ground inside it: an island.
		for (int y = 2; y <= 8; y++)
			for (int x = 30; x <= 36; x++)
				if (x == 30 || x == 36 || y == 2 || y == 8)
					solid[x + y * size] = true;

		Vector2 Cell(int x, int y) => new Vector2((x + 0.5f) * tile, (y + 0.5f) * tile);
		Vector2 player = Cell(20, 32);
		Vector2 behind = Cell(20, 6);

		var whole = new MazeNavigation(size, tile, Vector2.Zero, isMaze: false);
		whole.RebuildFromGrid(solid, wall, Vector2I.Zero, player);

		// Built first round another cell, then moved to the player's and stepped in tiny slices.
		var sliced = new MazeNavigation(size, tile, Vector2.Zero, isMaze: false) { CellsPerFrame = 37 };
		sliced.RebuildFromGrid(solid, wall, Vector2I.Zero, Cell(5, 35));
		int frames = 0;
		sliced.Update(player, 1f);
		while (sliced.IsBuildingField && frames++ < 1000)
			sliced.Update(player, 0.01f);
		if (frames < 2)
			warnings.Add("Navigation: the sliced field finished in one step - the regression check is not exercising resumption.");
		int mismatched = 0;
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
				if (whole.PathTilesAt(Cell(x, y)) != sliced.PathTilesAt(Cell(x, y)))
					mismatched++;
		if (mismatched > 0)
			warnings.Add($"Navigation: a field built in slices differs from one built whole on {mismatched} cells (MazeNavigation.StepField).");

		if (whole.HasClearWalk(behind, player))
			warnings.Add("Navigation: the straight walk across a lake reads as clear - open-stage enemies would never route round water.");
		if (!whole.HasClearWalk(Cell(5, 6), Cell(5, 32)))
			warnings.Add("Navigation: a straight walk on dry ground reads as blocked.");

		Vector2 flow = whole.FlowDirectionAt(behind);
		if (flow.X > -0.2f)
			warnings.Add($"Navigation: the flow behind a lake is {flow}, which does not head round its near (left) end.");
		float walk = whole.PathTilesAt(behind);
		if (walk < 27f)
			warnings.Add($"Navigation: walking distance round the lake is {walk} tiles, no longer than the 26-tile straight line - the lake is not in the field.");

		if (whole.IsReachable(Cell(33, 5)))
			warnings.Add("Navigation: dry ground inside a ring of water counts as reachable - enemies could be spawned on an island.");
		if (!whole.IsReachable(Cell(5, 5)))
			warnings.Add("Navigation: open ground connected to the player counts as unreachable.");
		if (whole.IsWallAt(behind) || !whole.HasLineOfSight(behind, player))
			warnings.Add("Navigation: water is stopping shots - only maze walls should.");

		ValidateSkirtTurn(warnings);
		ValidatePropClusters(warnings);
	}

	// Node2DGame.StampPropClusters' geometry (PropClusters, .ai/terrain.md section 9 "Props"): two props
	// a body cannot pass between close their cells and a four-connected way between them, so the grid has
	// no diagonal gap a body does not fit; two it can pass between close nothing, and neither does a lone
	// one; one a hand's width off water closes itself and the gap to the shore.
	private static void ValidatePropClusters(List<string> warnings)
	{
		const float tile = 48f;
		var closed = new HashSet<Vector2I>();
		var shore = new Vector2I(1, 6);
		var props = new List<Rect2>
		{
			new(new Vector2(60f, 60f), new Vector2(27f, 15f)),    // cell (1, 1)
			new(new Vector2(120f, 100f), new Vector2(27f, 15f)),  // cell (2, 2): 41 px off the first, diagonally
			new(new Vector2(400f, 60f), new Vector2(27f, 15f)),   // cell (8, 1)
			new(new Vector2(500f, 60f), new Vector2(27f, 15f)),   // cell (10, 1): 73 px off the last - a body fits
			new(new Vector2(10f, 300f), new Vector2(27f, 15f)),   // cell (0, 6): 11 px off the water at (1, 6)
			new(new Vector2(700f, 700f), new Vector2(12f, 12f)),  // a lone trunk
		};
		PropClusters.Collect(props, Vector2.Zero, tile, cell => cell == shore, cell => closed.Add(cell));

		if (!closed.Contains(new Vector2I(1, 1)) || !closed.Contains(new Vector2I(2, 2)))
			warnings.Add("Prop clusters: two rocks 41 px apart are not in the navigation grid - the swarm would wedge between them.");
		else if (!closed.Contains(new Vector2I(2, 1)) && !closed.Contains(new Vector2I(1, 2)))
			warnings.Add("Prop clusters: two rocks on a diagonal leave the corner between them open in the grid.");
		if (closed.Contains(new Vector2I(8, 1)) || closed.Contains(new Vector2I(10, 1)) || closed.Contains(new Vector2I(9, 1)))
			warnings.Add("Prop clusters: two props a body can walk between (73 px) were closed.");
		if (closed.Contains(new Vector2I(14, 14)))
			warnings.Add("Prop clusters: a lone trunk was closed - the skirt goes round it, the grid need not.");
		if (!closed.Contains(new Vector2I(0, 6)))
			warnings.Add("Prop clusters: a rock 11 px off the water is not in the grid - the swarm would wedge in the gap.");
	}

	// Enemy.Skirt's geometry (.ai/enemy-behaviour.md section 8 item 13): a body that walks square into
	// something turns along it, keeps pressing on it so it follows it round, and goes the way it already
	// leant; one brushing along a wall is left alone. Every heading a skirt can produce must also carry
	// the body along the surface, never back off it or to a stop.
	private static void ValidateSkirtTurn(List<string> warnings)
	{
		Vector2 normal = Vector2.Up;            // a surface facing north; the body walks south into it
		Vector2 square = Vector2.Down;
		if (!SkirtTurn.IsFacingInto(square, normal, 0.5f))
			warnings.Add("Skirting: a body walking square into a surface does not count as held by it.");
		if (SkirtTurn.IsFacingInto(new Vector2(1f, 0.3f).Normalized(), normal, 0.5f))
			warnings.Add("Skirting: a body brushing along a wall counts as walking into it - corridors would skirt.");

		Vector2 leaning = new Vector2(-0.3f, 1f).Normalized();
		float hand = SkirtTurn.ChooseHand(leaning, normal, 1f);
		if (SkirtTurn.Along(normal, hand).X > -0.5f)
			warnings.Add("Skirting: a body leaning west into a surface turns east - against its own lean.");
		if (Mathf.Abs(SkirtTurn.ChooseHand(square, normal, -1f)) != 1f || SkirtTurn.ChooseHand(square, normal, -1f) != -1f)
			warnings.Add("Skirting: a dead-square body does not take its own tie side.");

		for (int a = 0; a < 360; a += 15)
		{
			Vector2 heading = Vector2.FromAngle(Mathf.DegToRad(a));
			foreach (float side in new[] { -1f, 1f })
			{
				Vector2 along = SkirtTurn.Along(normal, side);
				Vector2 turn = SkirtTurn.Blend(along, heading, 0.5f);
				if (!turn.IsNormalized() || turn.Dot(along) < 0.3f)
				{
					warnings.Add($"Skirting: the turn for heading {heading} on hand {side} is {turn}, which does not carry the body along the surface.");
					return;
				}
			}
		}
	}

	// Stage hazards on the maze stand in the middle of a hallway or a hall (.ai/terrain.md section 6,
	// "Traps in the middle of the path"). LevelGenerator.MazeHallwayMiddle does it from the lattice;
	// this checks it from the WALLS: on real generated maze ground, every point it snaps to must be
	// open floor at least half a corridor from the nearest wall tile. In a six-tile corridor that is
	// only true on the centreline, so a trap that has drifted toward a wall, or into one, fails here
	// whatever the snapping code thinks it did. And since 2026-10-28 ("make the traps less random") it
	// must be one of the two kinds of middle - a hallway's, half a cell along one axis from a cell
	// centre, or a hall's, half a cell along both - never a junction and never a random point down a
	// corridor. Points within the hazards' clearance of the start are left out, as the game leaves
	// them out.
	private static void ValidateMazeHazards(List<string> warnings)
	{
		const int tile = 48;
		const int chunk = 32;
		const int margin = 48;
		var generator = new LevelGenerator();
		float need = LevelGenerator.MazeCorridorTiles / 2f * tile - 1f;
		int checkedCount = 0, failed = 0, halls = 0, hallways = 0, strays = 0;
		string firstFailure = null, firstStray = null;
		foreach (ulong seed in new ulong[] { 20261014UL, 4242UL, 987654321UL })
		{
			// One padded grid: index [0, 0] is world tile (-margin, -margin); the middle is chunk (0, 0).
			string[,] grid = generator.GenerateChunk(StageEnvironmentKind.Castle, seed, 0, 0, chunk, margin, 6f, 4f);
			int size = grid.GetLength(0);
			bool WallTile(int tx, int ty)
			{
				int gx = tx + margin, gy = ty + margin;
				return gx < 0 || gy < 0 || gx >= size || gy >= size || grid[gx, gy] == LevelGenerator.WallTerrain;
			}
			bool IsWall(Vector2 local) => WallTile(Mathf.FloorToInt(local.X / tile), Mathf.FloorToInt(local.Y / tile));

			var rng = new LevelGenerator.Rng(LevelGenerator.Hash(seed, 7, 7, 0x4A2D));
			for (int k = 0; k < 400; k++)
			{
				// Well inside the grid, so the walls a point can see are all generated.
				var p = new Vector2(
					(-margin + 12 + rng.NextFloat() * (size - 24)) * tile,
					(-margin + 12 + rng.NextFloat() * (size - 24)) * tile);
				Vector2 q = LevelGenerator.MazeHallwayMiddle(p, tile, IsWall);
				if (q.Length() < 320f)
					continue;
				checkedCount++;
				float clearance = float.MaxValue;
				int qx = Mathf.FloorToInt(q.X / tile), qy = Mathf.FloorToInt(q.Y / tile);
				for (int ty = qy - 6; ty <= qy + 6; ty++)
				{
					for (int tx = qx - 6; tx <= qx + 6; tx++)
					{
						if (!WallTile(tx, ty)) continue;
						float dx = Mathf.Max(Mathf.Max(tx * tile - q.X, q.X - (tx + 1) * tile), 0f);
						float dy = Mathf.Max(Mathf.Max(ty * tile - q.Y, q.Y - (ty + 1) * tile), 0f);
						clearance = Mathf.Min(clearance, Mathf.Sqrt(dx * dx + dy * dy));
					}
				}
				if (clearance < need)
				{
					failed++;
					firstFailure ??= $"seed {seed}: {p} snapped to {q}, {clearance:0} px from a wall";
				}
				float pitch = LevelGenerator.MazeCellTiles * tile;
				float halfPitch = pitch * 0.5f;
				bool onCellX = Mathf.IsZeroApprox(Mathf.PosMod(q.X, pitch)), onCellY = Mathf.IsZeroApprox(Mathf.PosMod(q.Y, pitch));
				bool onHalfX = Mathf.IsEqualApprox(Mathf.PosMod(q.X, pitch), halfPitch);
				bool onHalfY = Mathf.IsEqualApprox(Mathf.PosMod(q.Y, pitch), halfPitch);
				if ((onCellX && onHalfY) || (onHalfX && onCellY)) hallways++;
				else if (onHalfX && onHalfY) halls++;
				else
				{
					strays++;
					firstStray ??= $"seed {seed}: {p} snapped to {q}";
				}
			}
		}
		if (failed > 0)
			warnings.Add($"Maze: {failed} of {checkedCount} stage hazards would stand less than half a corridor ({need + 1:0} px) "
				+ $"from a wall (first: {firstFailure}). LevelGenerator.MazeHallwayMiddle no longer finds the middle of the path.");
		if (strays > 0)
			warnings.Add($"Maze: {strays} of {checkedCount} stage hazards stand neither in a hallway's middle nor a hall's "
				+ $"(first: {firstStray}) - the traps are scattered again.");
		if (checkedCount < 600 || halls == 0 || hallways == 0)
			warnings.Add($"Maze: the hazard check saw {checkedCount} points ({hallways} mid-hallway, {halls} mid-hall) - too few, "
				+ "or a kind of ground it never reached, so it is not testing what it claims.");
	}

	// Water stops the player because assets/bonelight/tiles/tiles.json says "blocking": true on
	// its sixteen tiles - and that file is GENERATED. Re-run tools/art/tiles.py from a checkout
	// where the BLOCKERS set has been lost and the flag quietly disappears: the art still renders,
	// the game still builds, the tiles still paint, and the player walks across the lake. Nothing
	// else in the project would notice.
	private static void ValidateBlockingTerrain(List<string> warnings)
	{
		const string manifest = "res://assets/bonelight/tiles/tiles.json";
		CuratedTileCatalog catalog = CuratedTileCatalog.LoadMany(manifest);
		if (catalog == null)
		{
			warnings.Add($"Tiles: {manifest} did not load; terrain blocking cannot be checked.");
			return;
		}

		if (!catalog.IsBlockingTerrain("water"))
		{
			warnings.Add("Tiles: water is not marked blocking in tiles.json, so it can be walked on. "
				+ "Check BLOCKERS in tools/art/tiles.py and re-run: python tools/art/tiles.py");
		}

		// The maze walls are the "void" material. Without it LevelTilePainter refuses to paint them,
		// and before that guard it painted them as whatever floor came first - sand.
		if (!catalog.HasGroundMaterial("void"))
		{
			warnings.Add("Tiles: the 'void' material (maze walls) is missing from tiles.json. "
				+ "Check MATERIALS in tools/art/tiles.py and re-run: python tools/art/tiles.py");
		}

		// The inverse, so the flag cannot creep onto terrain that is meant to be crossed. Lava and
		// the pits hurt you for standing in them, which only works if you can stand in them.
		foreach (string crossable in new[]
		{
			"grass", "meadow", "moss", "dirt", "overgrowth", "sand", "cobble", "dark_dirt", "mossy_rock",
			"rocky_pit", "ice", "lava", "pit",
		})
		{
			if (catalog.IsBlockingTerrain(crossable))
				warnings.Add($"Tiles: '{crossable}' is marked blocking, which makes it a wall.");
		}
	}

	// Everything a stage paints has to exist, and nothing reports it if it does not: a terrain
	// missing from tiles.json paints as nothing (EnsureTerrainSources caches the miss), a missing
	// ground-detail set paints as nothing, and a missing landmark sprite places no landmark. The
	// natural-ground pass (.ai/terrain.md section 8) added nine names across three files, so this
	// holds the palettes, GroundDetail's rules and the art against each other.
	// The res:// path of every frame in a tile manifest (tiles.json's "frames").
	private static IEnumerable<string> ManifestTilePaths(string manifestPath)
	{
		string text = Godot.FileAccess.GetFileAsString(manifestPath);
		if (string.IsNullOrEmpty(text))
			yield break;
		using var doc = System.Text.Json.JsonDocument.Parse(text);
		if (!doc.RootElement.TryGetProperty("frames", out var frames))
			yield break;
		foreach (var frame in frames.EnumerateArray())
		{
			string path = frame.TryGetProperty("path", out var p) ? p.GetString() : null;
			if (!string.IsNullOrEmpty(path))
				yield return path.StartsWith("res://") ? path : "res://" + path;
		}
	}

	private static void ValidateGroundArt(List<string> warnings)
	{
		CuratedTileCatalog catalog = CuratedTileCatalog.LoadMany(
			"res://assets/bonelight/tiles/tiles.json", "res://assets/kidsart/tiles.json");
		var generator = new LevelGenerator();
		foreach (StageEnvironmentKind kind in Enum.GetValues<StageEnvironmentKind>())
		{
			LevelLayout layout = generator.Describe(kind);
			foreach (string terrain in layout.Terrains)
			{
				if (catalog.TileVariants(terrain, "fill").Count == 0)
					warnings.Add($"Ground: {kind} stamps '{terrain}', which tiles.json does not define. Re-run: python tools/art/tiles.py");
			}
			if (!string.IsNullOrEmpty(layout.GroundTerrain) && catalog.FillTilesFor(layout.GroundTerrain).Count == 0)
				warnings.Add($"Ground: {kind} stands on terrain '{layout.GroundTerrain}', which has no fill tiles.");
			else if (string.IsNullOrEmpty(layout.GroundTerrain) && !catalog.HasGroundMaterial(layout.GroundMaterial))
				warnings.Add($"Ground: {kind} stands on material '{layout.GroundMaterial}', which no manifest defines.");
		}

		Dictionary<string, List<string>> detail = LevelTilePainter.LoadDetailManifest("res://assets/bonelight/tiles/detail/detail.json");
		foreach (string set in GroundDetail.AllSets())
		{
			if (!detail.ContainsKey(set))
				warnings.Add($"Ground: detail set '{set}' is named by GroundDetail but missing from detail.json. Re-run: python tools/art/ground_detail.py");
		}

		// The tile atlas the painter draws from (tools/art/tile_atlas.py). A tile missing from it
		// still paints - LevelTilePainter gives it a texture of its own - but costs a draw call per
		// cell again, which is the frame rate the atlas exists to save (.ai/performance.md).
		Dictionary<string, Vector2I> atlas = LevelTilePainter.LoadAtlasManifest("res://assets/bonelight/tiles/atlas.json", 48, out _);
		if (atlas.Count == 0 || !ResourceLoader.Exists("res://assets/bonelight/tiles/atlas.png"))
			warnings.Add("Ground: the tile atlas (atlas.json / atlas.png) is missing, so every tile is its own draw call. Re-run: python tools/art/tile_atlas.py");
		else
		{
			var unpacked = new List<string>();
			foreach (string path in ManifestTilePaths("res://assets/bonelight/tiles/tiles.json"))
				if (!atlas.ContainsKey(path)) unpacked.Add(path);
			foreach (List<string> variants in detail.Values)
				foreach (string path in variants)
					if (!atlas.ContainsKey(path)) unpacked.Add(path);
			if (unpacked.Count > 0)
				warnings.Add($"Ground: {unpacked.Count} tile(s) are not in the tile atlas (first: {unpacked[0]}). Re-run: python tools/art/tile_atlas.py");
		}

		// The Cursed Dungeon's jail walls (tools/art/jail_walls.py): a plain variant for every role the
		// painter can ask for, the one-tile stubs the spawn clearing leaves among them, and the four
		// shadows. A role without art paints as the old black void tile (LevelTilePainter.WallTileFor),
		// which is a wall that works and looks like a hole - nothing else would say so.
		var wallRoles = new List<string>();
		foreach (AutotileRole role in Enum.GetValues<AutotileRole>())
			wallRoles.Add(TerrainAutotiler.RoleKey(role));
		wallRoles.AddRange(new[] { "patch_v", "patch_cap", "patch_face" });
		foreach (string role in wallRoles)
		{
			if (!catalog.TileVariants(LevelGenerator.WallTerrain, role).Any(e => e.Role == role && e.Dressing == null))
				warnings.Add($"Ground: the maze walls have no plain '{role}' tile, so those cells paint as the black void. Re-run: python tools/art/tiles.py");
		}
		if (!catalog.TileVariants(LevelGenerator.WallTerrain, "edge_bottom").Any(e => e.Dressing == "sconce"))
			warnings.Add("Ground: the maze walls' faces have no lit sconce. Re-run: python tools/art/tiles.py");

		// A chapter's own wall set (StageEnvironmentProfile.WallSet; court_walls.py, ruin_walls.py...) is
		// held to the same roles the moment the manifest has any of it: the painter takes a set whole or not
		// at all (LevelTilePainter.WallArtSet), so a set missing a role would paint that role as the void.
		// A set nobody has drawn yet paints the jail walls, and is its chapter's to-do, not a fault.
		foreach (StageEnvironmentKind kind in Enum.GetValues<StageEnvironmentKind>())
		{
			string set = StageEnvironmentCatalog.Get(kind).WallSet;
			if (set == StageEnvironmentProfile.DefaultWallSet || catalog.TileVariants(set, "fill").Count == 0)
				continue;
			foreach (string role in wallRoles)
			{
				if (!catalog.TileVariants(set, role).Any(e => e.Role == role && e.Dressing == null))
					warnings.Add($"Ground: {kind}'s wall set '{set}' has no plain '{role}' tile, so those cells paint as the black void. Re-run: python tools/art/tiles.py");
			}
		}
		foreach (string shade in new[] { "shade_n", "shade_w", "shade_nw", "shade_corner" })
		{
			if (!catalog.TileVariants("wall_shadow", shade).Any(e => e.Role == shade))
				warnings.Add($"Ground: the maze walls' '{shade}' shadow tile is missing; walls will stand on the floor with no shadow. Re-run: python tools/art/tiles.py");
		}

		// The garden: its parts, and the layout its art was drawn to against the one its fence is
		// built from. A pen drawn twelve cells wide with a fence collided at ten would put an
		// invisible rail through a flower bed and leave a drawn one you walk through.
		WorldSprites.Pen pen = WorldSprites.LoadPen();
		if (pen == null)
			warnings.Add($"Ground: {WorldSprites.PenManifest} is missing; no flower pens will be drawn. Re-run: python tools/art/ground_detail.py");
		else
		{
			foreach (string part in new[] { "back", "front", "side", "post", "flower", "garden" })
			{
				if (!pen.Parts.TryGetValue(part, out GroundedSprite sprite) || sprite.Texture == null)
					warnings.Add($"Ground: flower pen part '{part}' is missing or not imported; no flower pens will be drawn.");
			}
			if (pen.CellsW != FlowerPenLayout.CellsW || pen.CellsH != FlowerPenLayout.CellsH
				|| pen.GateX != FlowerPenLayout.GateX || pen.GateW != FlowerPenLayout.GateW
				|| pen.SideGateY != FlowerPenLayout.SideGateY || pen.SideGateH != FlowerPenLayout.SideGateH
				|| pen.Line * 2 != 48 || pen.Segment != 48)
				warnings.Add($"Ground: flower-pen.json was drawn to {pen.CellsW}x{pen.CellsH} cells, gates {pen.GateX}+{pen.GateW} and {pen.SideGateY}+{pen.SideGateH}, "
					+ $"but FlowerPenLayout is {FlowerPenLayout.CellsW}x{FlowerPenLayout.CellsH}, gates {FlowerPenLayout.GateX}+{FlowerPenLayout.GateW} "
					+ $"and {FlowerPenLayout.SideGateY}+{FlowerPenLayout.SideGateH}: the fence would collide where it is not drawn. Re-run: python tools/art/ground_detail.py");
		}

		// Trees and bushes: every sprite in the manifest loads, stands inside its own picture, and a
		// tree has a trunk to stop at (a tree with no footprint is a 300 px sprite you walk through).
		foreach (GroundedSprite s in WorldSprites.LoadTrees().Values)
		{
			if (s.Texture == null)
				warnings.Add($"Ground: tree or bush '{s.Id}' is not imported; it will never be placed. Run the import.");
			if (s.Ground <= 0 || s.Ground > s.Height || s.AnchorX <= 0 || s.AnchorX >= s.Width)
				warnings.Add($"Ground: tree or bush '{s.Id}' has its ground line or trunk column outside its picture.");
			if ((s.Kind == "tree") != (s.Trunk > 0))
				warnings.Add($"Ground: '{s.Id}' is a {s.Kind} with a trunk of {s.Trunk} px: trees block at the trunk, bushes never.");
			// Nothing cut off by its own box (0.9.3: "the art for the bushes and scorched sands all
			// looked like the art got cut off due to box boundaries"). trees.py fits every canvas to its
			// art with a clear pixel all round, so a twig, leaf or shade pool on the border is one a
			// generator drew past its canvas and lost. tools/art/clip_check.py checks every set of
			// world art; this holds the reported one in every run.
			int cut = s.Texture == null ? 0 : OpaqueBorderPixels(s.Texture);
			if (cut > 0)
				warnings.Add($"Ground: tree or bush '{s.Id}' has {cut} opaque pixels on its border: its art runs past its box and is cut off there. Re-run: python tools/art/trees.py, then python tools/art/clip_check.py.");
		}
	}

	/// <summary>How many pixels of a texture's outermost rows and columns are not transparent.</summary>
	private static int OpaqueBorderPixels(Texture2D texture)
	{
		Image image = texture.GetImage();
		if (image == null)
			return 0;
		if (image.IsCompressed())
			image.Decompress();
		int w = image.GetWidth(), h = image.GetHeight(), count = 0;
		for (int x = 0; x < w; x++)
		{
			if (image.GetPixel(x, 0).A > 0f) count++;
			if (h > 1 && image.GetPixel(x, h - 1).A > 0f) count++;
		}
		for (int y = 1; y < h - 1; y++)
		{
			if (image.GetPixel(0, y).A > 0f) count++;
			if (w > 1 && image.GetPixel(w - 1, y).A > 0f) count++;
		}
		return count;
	}

	// The flower pen's fence in the navigation (.ai/terrain.md section 8). The fence used to stop
	// nothing; now it stops the boots and the horde, and the only thing standing between "solid
	// fence" and "a wall the swarm wedges on" is that its cells are in the grid and its gates are not.
	// So, on a pen stamped exactly as Node2DGame.PlanChunkFeatures stamps one:
	//   - the rails are solid and stop no shot, all four gates are open;
	//   - a straight walk through the fence reads as blocked, so an enemy follows the field;
	//   - the inside is reachable, and from just outside a rail only the long way, through a gate;
	//   - an enemy beside a side rail is sent along it, not into it;
	//   - no stretch of rail is long: a gate is never more than MaxRailToGateCells away (2026-10-28,
	//     the swarm queued at two narrow gates and "got stuck");
	//   - every rectangle of the rails' collision lies on fence cells, so a body is never stopped
	//     on a cell the field calls open (the rule water keeps, section 3).
	private const int MaxRailToGateCells = 8;

	private static void ValidateFlowerPen(List<string> warnings)
	{
		const int size = 40;
		const int tile = 48;
		const int px0 = 14, py0 = 14;
		var solid = new bool[size * size];
		var wall = new bool[size * size];
		for (int y = 0; y < FlowerPenLayout.CellsH; y++)
			for (int x = 0; x < FlowerPenLayout.CellsW; x++)
				if (FlowerPenLayout.IsFenceCell(x, y))
					solid[(px0 + x) + (py0 + y) * size] = true;

		Vector2 Cell(int x, int y) => new Vector2((x + 0.5f) * tile, (y + 0.5f) * tile);
		const int w = FlowerPenLayout.CellsW, h = FlowerPenLayout.CellsH;
		Vector2 flower = Cell(px0 + w / 2, py0 + h / 2);
		// The second row: a stretch of the west rail clear of its gate, as far from one as a rail gets.
		int railRow = py0 + 1;
		Vector2 west = Cell(px0 - 2, railRow);
		Vector2 inside = Cell(px0 + 1, railRow);
		Vector2 besideRail = Cell(px0 - 1, railRow + 1);

		var nav = new MazeNavigation(size, tile, Vector2.Zero, isMaze: false);
		nav.RebuildFromGrid(solid, wall, Vector2I.Zero, flower);

		if (!nav.IsBlockedAt(Cell(px0, railRow)) || !nav.IsBlockedAt(Cell(px0 + 2, py0)))
			warnings.Add("Navigation: a flower pen's rail is not solid in the grid - the horde would wedge on the fence.");
		if (nav.IsBlockedAt(Cell(px0 + FlowerPenLayout.GateX, py0)) || nav.IsBlockedAt(Cell(px0 + FlowerPenLayout.GateX, py0 + h - 1))
			|| nav.IsBlockedAt(Cell(px0, py0 + FlowerPenLayout.SideGateY)) || nav.IsBlockedAt(Cell(px0 + w - 1, py0 + FlowerPenLayout.SideGateY)))
			warnings.Add("Navigation: a flower pen's gate is solid - the garden has lost a way in.");
		if (nav.IsWallAt(Cell(px0, railRow)) || !nav.HasLineOfSight(west, inside))
			warnings.Add("Navigation: a garden fence is stopping shots - only maze walls should.");
		if (nav.HasClearWalk(west, inside))
			warnings.Add("Navigation: the straight walk through a garden fence reads as clear - enemies would never route to a gate.");
		if (!nav.IsReachable(flower) || !nav.IsReachable(west))
			warnings.Add("Navigation: the inside of a flower pen, or the ground outside it, counts as unreachable.");
		// Three cells apart across the rail; without the fence in the field the outside would be three
		// tiles further from the flower at most. Through a gate it is several more.
		float detour = nav.PathTilesAt(west) - nav.PathTilesAt(inside);
		float straight = west.DistanceTo(inside) / tile;
		if (detour < straight + 2f)
			warnings.Add($"Navigation: from just outside a pen's rail the flower is {detour:0.0} tiles further than from just inside, {straight:0.0} apart - the fence is not in the field.");
		// The longest run of rail between a corner and a gate, in cells.
		int longest = Math.Max(Math.Max(FlowerPenLayout.GateX, w - FlowerPenLayout.GateX - FlowerPenLayout.GateW),
			Math.Max(FlowerPenLayout.SideGateY, h - FlowerPenLayout.SideGateY - FlowerPenLayout.SideGateH));
		if (longest > MaxRailToGateCells || FlowerPenLayout.GateW * tile < 2 * 88 || FlowerPenLayout.SideGateH * tile < 2 * 88)
			warnings.Add($"Navigation: a flower pen has a rail {longest} cells long or a gate narrower than two of the widest body (176 px) - the swarm queues at it.");
		Vector2 flow = nav.FlowDirectionAt(besideRail);
		if (flow != Vector2.Zero && Mathf.Abs(flow.X) > Mathf.Abs(flow.Y) && flow.X > 0f)
			warnings.Add($"Navigation: the flow beside a pen's west rail is {flow}, into the fence rather than along it to a gate.");

		foreach (Rect2 rail in FlowerPenLayout.RailRects(tile))
		{
			foreach (Vector2 corner in new[] { rail.Position, rail.End - Vector2.One * 0.01f,
				new Vector2(rail.Position.X, rail.End.Y - 0.01f), new Vector2(rail.End.X - 0.01f, rail.Position.Y) })
			{
				int cx = Mathf.FloorToInt(corner.X / tile), cy = Mathf.FloorToInt(corner.Y / tile);
				if (!FlowerPenLayout.IsFenceCell(cx, cy))
				{
					warnings.Add($"Navigation: the flower pen's rail collision {rail} reaches cell ({cx}, {cy}), which the grid calls open.");
					break;
				}
			}
		}
	}

	// THE SEAM RULE (.ai/terrain.md, "Chunk streaming"). Endless stages are generated a chunk at a
	// time, each padded with TerrainStreamer.Margin cells of its neighbours, and the border only comes
	// out the same from both sides if that margin is wide enough for everything that reads past a
	// cell: the cleanup passes and the autotile role. Nothing would report a margin that is too
	// narrow - the game builds and runs, and there is simply a line across the world every 1536 px.
	// So: a chunk resolved with the shipping margin must match the same chunk resolved with twice
	// it, cell for cell; and two neighbours' raw grids must agree wherever they overlap.
	//
	// The maze half checks the other promise: corridors connect across chunk borders. Nine chunks
	// round the start are stitched together and flooded from the spawn point; almost all of the
	// floor must be reached inside that window alone.
	// The water's shape (WaterShape) is built per chunk from a window reaching into its neighbours,
	// and two chunks build the cells along their shared border. If those disagree, the shore steps
	// at every chunk border - and so does its collision, since the polygons are the bodies. So build
	// two neighbours on the Swamp, which has the most water, and compare every cell both build.
	private static void ValidateWaterShapes(List<string> warnings)
	{
		const ulong seed = 20261016;
		const int c = TerrainStreamer.ChunkTiles;
		const int m = TerrainStreamer.Margin;
		var generator = new LevelGenerator();
		var autotiler = new TerrainAutotiler();
		LevelLayout layout = generator.Describe(StageEnvironmentKind.Swamp);

		// Resolved water flags over chunks (-1..2, -1..1), as the streamer would hold them.
		const int x0 = -1, y0 = -1, wide = 4 * c, high = 3 * c;
		var wet = new bool[wide * high];
		for (int cy = -1; cy <= 1; cy++)
		{
			for (int cx = -1; cx <= 2; cx++)
			{
				TileResolution[,] r = autotiler.Resolve(generator.GenerateChunk(StageEnvironmentKind.Swamp, seed, cx, cy, c, m), layout.BaseTerrain);
				for (int y = 0; y < c; y++)
					for (int x = 0; x < c; x++)
						wet[(cx - x0) * c + x + ((cy - y0) * c + y) * wide] = r[x + m, y + m].Terrain == "water";
			}
		}

		WaterShape Build(Vector2I chunk)
		{
			var shape = new WaterShape(c);
			Vector2I min = shape.WindowMin(chunk * c);
			int side = shape.WindowCells;
			for (int y = 0; y < side; y++)
				for (int x = 0; x < side; x++)
				{
					int wx = min.X + x - x0 * c, wy = min.Y + y - y0 * c;
					shape.WetBuffer[x + y * side] = wx >= 0 && wy >= 0 && wx < wide && wy < high && wet[wx + wy * wide];
				}
			shape.Build(chunk * c, seed);
			return shape;
		}

		WaterShape left = Build(new Vector2I(0, 0));
		WaterShape right = Build(new Vector2I(1, 0));
		int compared = 0, differ = 0, wetEmpty = 0;
		for (int y = -WaterShape.ShapeRing; y < c + WaterShape.ShapeRing; y++)
		{
			for (int x = c - WaterShape.ShapeRing; x < c + WaterShape.ShapeRing; x++)
			{
				int a = left.KindAt(x, y), b = right.KindAt(x, y);
				compared++;
				if (a != b) { differ++; continue; }
				if (a != 2) continue;
				ReadOnlySpan<Vector2> pa = left.PolygonAt(x, y), pb = right.PolygonAt(x, y);
				bool same = pa.Length == pb.Length;
				for (int i = 0; same && i < pa.Length; i++)
					same = pa[i].DistanceSquaredTo(pb[i]) < 1e-4f;
				if (!same) differ++;
			}
		}
		for (int y = 0; y < c; y++)
			for (int x = 0; x < c; x++)
				if (left.KindAt(x, y) == 3) wetEmpty++;
		if (differ > 0)
			warnings.Add($"Water: chunks (0, 0) and (1, 0) build {differ} of {compared} shared border cells differently, so "
				+ "the shore and its collision step at every chunk border. WaterShape.WindowMargin is too narrow, or something "
				+ "in it reads the chunk rather than the world.");
		if (wetEmpty > 0)
			warnings.Add($"Water: {wetEmpty} water cells of chunk (0, 0) come out with no water drawn in them at all. The "
				+ "blocked grid still calls them water, so they are an invisible no-go square for the flow field.");
	}

	private static void ValidateTerrainSeams(List<string> warnings)
	{
		const ulong seed = 20261016;
		const int c = TerrainStreamer.ChunkTiles;
		const int m = TerrainStreamer.Margin;
		var generator = new LevelGenerator();
		var autotiler = new TerrainAutotiler();

		// The two open stages that stamp the most, and every chapter on a topology built in a partial file
		// of LevelGenerator's (the bridge's band, the Court's layout, the Ruins' walls, the Cave's
		// caverns): each is held to the rule the day its file is built, with no edit here.
		var seamKinds = new List<StageEnvironmentKind> { StageEnvironmentKind.Swamp, StageEnvironmentKind.Volcanic };
		foreach (StageEnvironmentKind kind in Enum.GetValues<StageEnvironmentKind>())
		{
			StageTopology built = generator.Describe(kind).Topology;
			if (built != StageTopology.Open && built != StageTopology.Maze)
				seamKinds.Add(kind);
		}

		foreach (StageEnvironmentKind kind in seamKinds)
		{
			LevelLayout layout = generator.Describe(kind);
			foreach ((int cx, int cy) in new[] { (0, 0), (3, -2) })
			{
				// Cleaned as the game cleans it: walled ground is not (Node2DGame, TerrainStreamer).
				TileResolution[,] narrow = autotiler.Resolve(generator.GenerateChunk(kind, seed, cx, cy, c, m), layout.BaseTerrain, cleanup: !layout.Walled);
				TileResolution[,] wide = autotiler.Resolve(generator.GenerateChunk(kind, seed, cx, cy, c, m * 2), layout.BaseTerrain, cleanup: !layout.Walled);
				int differ = 0;
				for (int x = 0; x < c; x++)
					for (int y = 0; y < c; y++)
						if (narrow[x + m, y + m].Terrain != wide[x + m * 2, y + m * 2].Terrain
							|| narrow[x + m, y + m].Role != wide[x + m * 2, y + m * 2].Role)
							differ++;
				if (differ > 0)
					warnings.Add($"Terrain: {kind} chunk ({cx}, {cy}) resolves {differ} cells differently with a wider "
						+ "margin, so chunk borders will seam. TerrainStreamer.Margin is too narrow for the cleanup passes.");
			}

			// Raw grids of two horizontal neighbours, over the cells both of them generate.
			string[,] left = generator.GenerateChunk(kind, seed, 0, 0, c, m);
			string[,] right = generator.GenerateChunk(kind, seed, 1, 0, c, m);
			int rawDiffer = 0;
			for (int wx = c - m; wx < c + m; wx++)
				for (int y = 0; y < c + 2 * m; y++)
					if (left[wx + m, y] != right[wx - c + m, y])
						rawDiffer++;
			if (rawDiffer > 0)
				warnings.Add($"Terrain: {kind} chunks (0, 0) and (1, 0) disagree on {rawDiffer} raw cells they share. "
					+ "Something in LevelGenerator draws from an order-dependent RNG instead of a hash of the tile.");
		}

		// Every walled topology in a partial file (the Ruins' broken walls, the Cave's caverns, and whatever is
		// built after them) is held to "connected by construction" too: ExplorePlacer and
		// Node2DGame.IsExploreGroundClear skip the walk-away test on walled ground because of it.
		foreach (StageEnvironmentKind kind in seamKinds)
			if (generator.Describe(kind).Walled)
				ValidateNoSealedPockets(generator, kind, seed, warnings);

		LevelLayout maze = generator.Describe(StageEnvironmentKind.Castle);
		if (maze.Topology != StageTopology.Maze)
			return;
		const int span = 3 * c;
		var open = new bool[span, span];
		for (int cy = -1; cy <= 1; cy++)
		{
			for (int cx = -1; cx <= 1; cx++)
			{
				string[,] g = generator.GenerateChunk(StageEnvironmentKind.Castle, seed, cx, cy, c, m);
				for (int x = 0; x < c; x++)
					for (int y = 0; y < c; y++)
						open[(cx + 1) * c + x, (cy + 1) * c + y] = g[x + m, y + m] == maze.BaseTerrain;
			}
		}
		int total = 0;
		foreach (bool o in open) if (o) total++;
		var seen = new bool[span, span];
		var stack = new Stack<(int, int)>();
		stack.Push((c, c));
		seen[c, c] = true;
		int reached = 0;
		while (stack.Count > 0)
		{
			(int x, int y) = stack.Pop();
			reached++;
			foreach ((int dx, int dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
			{
				int nx = x + dx, ny = y + dy;
				if (nx < 0 || ny < 0 || nx >= span || ny >= span || seen[nx, ny] || !open[nx, ny]) continue;
				seen[nx, ny] = true;
				stack.Push((nx, ny));
			}
		}
		if (!open[c, c] || reached < total * 0.9f)
			warnings.Add($"Terrain: the maze round the start reaches only {reached} of {total} floor cells across nine chunks; "
				+ "corridors are not connecting across chunk borders (LevelGenerator.DoorMask).");
	}

	// Nine chunks round the start of a walled topology, flooded over every cell a body can stand on (not wall,
	// not water). A pocket is a piece of that ground which neither holds the start nor reaches the edge of the
	// nine: whatever is placed in it - an enemy, a chest, a secret - could never walk out, and nothing on
	// walled ground checks, because the ground is meant to be connected by construction
	// (LevelGenerator.Caverns.cs says why it is). The start must be standing ground as well. With the user
	// argument --terrain-stats it also prints how much of the nine is wall and water, for tuning.
	private static void ValidateNoSealedPockets(LevelGenerator generator, StageEnvironmentKind kind, ulong seed, List<string> warnings)
	{
		const int c = TerrainStreamer.ChunkTiles;
		const int m = TerrainStreamer.Margin;
		const int span = 3 * c;
		var walk = new bool[span, span];
		int walls = 0, water = 0;
		for (int cy = -1; cy <= 1; cy++)
		{
			for (int cx = -1; cx <= 1; cx++)
			{
				string[,] g = generator.GenerateChunk(kind, seed, cx, cy, c, m);
				for (int x = 0; x < c; x++)
				{
					for (int y = 0; y < c; y++)
					{
						string t = g[x + m, y + m];
						walls += t == LevelGenerator.WallTerrain ? 1 : 0;
						water += t == "water" ? 1 : 0;
						walk[(cx + 1) * c + x, (cy + 1) * c + y] = t != LevelGenerator.WallTerrain && t != "water";
					}
				}
			}
		}
		if (OS.GetCmdlineUserArgs().Contains("--terrain-stats"))
			GD.Print($"Terrain stats: {kind} nine chunks round the start, seed {seed}: "
				+ $"wall {100f * walls / (span * span):0.0}%, water {100f * water / (span * span):0.0}%");

		if (!walk[c, c])
			warnings.Add($"Terrain: {kind}'s start (world tile 0, 0) is not standing ground; the run would begin in a wall or water.");

		var label = new int[span, span];
		var stack = new Stack<(int, int)>();
		int pocketCells = 0, pockets = 0;
		(int, int)? firstPocket = null;
		int next = 0;
		for (int sx = 0; sx < span; sx++)
		{
			for (int sy = 0; sy < span; sy++)
			{
				if (!walk[sx, sy] || label[sx, sy] != 0)
					continue;
				next++;
				label[sx, sy] = next;
				stack.Push((sx, sy));
				int size = 0;
				bool edge = false, start = false;
				while (stack.Count > 0)
				{
					(int x, int y) = stack.Pop();
					size++;
					edge |= x == 0 || y == 0 || x == span - 1 || y == span - 1;
					start |= x == c && y == c;
					foreach ((int dx, int dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
					{
						int nx = x + dx, ny = y + dy;
						if (nx < 0 || ny < 0 || nx >= span || ny >= span || label[nx, ny] != 0 || !walk[nx, ny]) continue;
						label[nx, ny] = next;
						stack.Push((nx, ny));
					}
				}
				if (!edge && !start)
				{
					pockets++;
					pocketCells += size;
					firstPocket ??= (sx - c, sy - c);
				}
			}
		}
		if (pockets > 0)
			warnings.Add($"Terrain: {kind} has {pockets} sealed pocket(s), {pocketCells} cells of standing ground walled or "
				+ $"watered in, round the start (first near world tile {firstPocket}). Its topology claims to be connected "
				+ "by construction, and nothing on walled ground checks: whatever is placed in one cannot walk out.");
	}

	// The input map lives in project.godot as a wall of serialised event objects, which is the one
	// place in this project where a merge can silently delete a feature: drop the four lines that
	// carry the joypad and the game still builds, still runs, and simply cannot be played with a
	// controller. Nothing else would report it, because there is no controller in a headless run.
	//
	// So this checks the SHAPE of the map rather than the bindings themselves - that each action
	// exists, and that the ones a pad needs actually carry a pad event.
	private static void ValidateInputMap(List<string> warnings)
	{
		// Every action any script reads by name. A typo here is a silent no-op at runtime, because
		// Input.IsActionPressed on an unknown action just returns false.
		string[] required =
		{
			"ui_accept", "ui_cancel", "pause",
			"ui_up", "ui_down", "ui_left", "ui_right",
			"move_up", "move_down", "move_left", "move_right",
		};

		foreach (string action in required)
		{
			if (!InputMap.HasAction(action))
				warnings.Add($"Input: action '{action}' is missing from project.godot; everything that reads it is dead.");
		}

		// A is select, Start is pause, and the left stick both moves and scrolls. Each of those is
		// one event in project.godot and none of them is visible in any C# file.
		RequireJoypadButton(warnings, "ui_accept", JoyButton.A, "A cannot press a menu option");
		RequireJoypadButton(warnings, "ui_cancel", JoyButton.Start, "Start cannot back out of a menu");
		RequireJoypadButton(warnings, "pause", JoyButton.Start, "Start cannot pause a run");

		RequireJoypadAxis(warnings, "move_up", JoyAxis.LeftY, "the left stick cannot move the player");
		RequireJoypadAxis(warnings, "move_left", JoyAxis.LeftX, "the left stick cannot move the player");
		RequireJoypadAxis(warnings, "ui_down", JoyAxis.LeftY, "the left stick cannot scroll a menu");
		RequireJoypadAxis(warnings, "ui_right", JoyAxis.LeftX, "the left stick cannot scroll a menu");

		// Movement reads move_* precisely because ui_* has to be deaf to a thumb resting on the
		// stick. If the two deadzones ever converge, one of the two jobs is being done wrongly.
		if (InputMap.HasAction("ui_down") && InputMap.HasAction("move_down")
			&& InputMap.ActionGetDeadzone("move_down") >= InputMap.ActionGetDeadzone("ui_down"))
		{
			warnings.Add("Input: move_* deadzone is no longer below ui_*'s. Movement then throws away "
				+ "the first half of the stick, which is the reason the two action sets are separate.");
		}
	}

	private static void RequireJoypadButton(List<string> warnings, string action, JoyButton button, string consequence)
	{
		if (!InputMap.HasAction(action))
			return;

		bool found = InputMap.ActionGetEvents(action)
			.Any(e => e is InputEventJoypadButton pad && pad.ButtonIndex == button);

		if (!found)
			warnings.Add($"Input: '{action}' has no {button} button event - {consequence}.");
	}

	private static void RequireJoypadAxis(List<string> warnings, string action, JoyAxis axis, string consequence)
	{
		if (!InputMap.HasAction(action))
			return;

		bool found = InputMap.ActionGetEvents(action)
			.Any(e => e is InputEventJoypadMotion motion && motion.Axis == axis);

		if (!found)
			warnings.Add($"Input: '{action}' has no {axis} motion event - {consequence}.");
	}

	// Music fails the same silent way, plus one of its own: a WAV whose .import does not say
	// edit/loop_mode=2 (Forward) plays its minute once and stops, which in a fifteen-minute run is
	// fourteen minutes of nothing. That setting lives in the .import file, not the WAV, so deleting
	// the .import loses it - the comparison is against LoopModeEnum.Disabled to catch exactly that.
	// The stems must also be one length, or AudioStreamSynchronized drifts them apart a loop at a time.
	private static void ValidateMusic(List<string> warnings)
	{
		foreach (string path in MusicCatalog.AllTracks())
		{
			if (!ResourceLoader.Exists(path))
			{
				warnings.Add($"Music: missing '{path}'. Run: python tools/audio/music.py");
				continue;
			}
			if (ResourceLoader.Load<AudioStream>(path) is AudioStreamWav wav
				&& wav.LoopMode == AudioStreamWav.LoopModeEnum.Disabled)
			{
				warnings.Add($"Music: '{path}' does not loop. Set edit/loop_mode=2 in its .import.");
			}
		}

		// Two stage rosters must agree (MusicCatalog's note says why it is not yet folded into
		// StageCatalog). A chapter with no row plays the Forest - audible, but in the wrong key -
		// and a row for a chapter that does not exist is a renumbering nobody followed through.
		var stageIndices = new HashSet<int>(StageCatalog.All.Select(s => s.Index));
		foreach (StageDefinition stage in StageCatalog.All)
		{
			if (!MusicCatalog.HasSetForStage(stage.Index))
				warnings.Add($"Music: chapter {stage.Index} ({stage.DisplayName}) has no set in MusicCatalog.PerStage and plays '{MusicCatalog.DefaultRunSet.Id}'.");
		}
		foreach (int index in MusicCatalog.StageIndices)
		{
			if (!stageIndices.Contains(index))
				warnings.Add($"Music: MusicCatalog.PerStage has a set for stage {index}, which StageCatalog does not list.");
		}

		foreach (MusicSet set in MusicCatalog.AllSets())
		{
			double length = -1.0;
			foreach (string path in set.Stems)
			{
				if (!ResourceLoader.Exists(path) || ResourceLoader.Load<AudioStream>(path) is not AudioStream stream)
					continue;
				double l = stream.GetLength();
				if (length >= 0.0 && Math.Abs(l - length) > 0.001)
					warnings.Add($"Music: the '{set.Id}' stems differ in length ({length:0.000} s and {l:0.000} s at {path}); they will drift apart.");
				length = l;
			}
		}
	}

	// Sound is the one system where a missing asset is not a crash and not a visible glitch - it
	// is silence, which looks exactly like a design decision. So the whole catalog is checked at
	// startup rather than discovered one absent hit at a time.
	private static void ValidateAudio(List<string> warnings)
	{
		int missing = 0;
		foreach (string name in SfxCatalog.AllNames)
		{
			string path = SfxCatalog.PathFor(name);
			if (ResourceLoader.Exists(path))
				continue;
			missing++;
			if (missing <= 5)
				warnings.Add($"Audio: missing sound '{name}' (expected {path}). Run: python tools/audio/build.py");
		}
		if (missing > 5)
			warnings.Add($"Audio: {missing - 5} further sounds missing from {SfxCatalog.Directory}");

		// A missing bus is not fatal - MusicPlayer and SfxPlayer both fall back to Master - but it
		// silently collapses the volume sliders onto one control, which is the bug that shipped
		// before default_bus_layout.tres existed.
		if (AudioServer.GetBusIndex(MusicPlayer.MusicBusName) < 0)
			warnings.Add("Audio: no 'Music' bus; the music slider is falling back to Master and now duplicates it.");
		if (AudioServer.GetBusIndex(SfxPlayer.SfxBusName) < 0)
			warnings.Add("Audio: no 'SFX' bus; the effects slider is falling back to Master and now duplicates it.");

		// The duck lowers SFX_Bed and leaves SFX_Priority alone. Without both, every sound lands
		// on one bus, the duck would fight the volume slider, and a critical sound would duck
		// itself - so the whole thing degrades to no ducking at all, silently.
		if (AudioServer.GetBusIndex(SfxPlayer.BedBusName) < 0)
			warnings.Add($"Audio: no '{SfxPlayer.BedBusName}' bus; ducking is disabled.");
		if (AudioServer.GetBusIndex(SfxPlayer.PriorityBusName) < 0)
			warnings.Add($"Audio: no '{SfxPlayer.PriorityBusName}' bus; critical sounds will duck themselves.");

		int sfxBus = AudioServer.GetBusIndex(SfxPlayer.SfxBusName);
		if (sfxBus >= 0 && AudioServer.GetBusEffectCount(sfxBus) == 0)
		{
			// tools/audio/mixsim.py measured the summed mix peaking at +1.5 to +2.5 dBFS in every
			// scenario, including the sparse early game. Without a limiter here it clips.
			warnings.Add("Audio: the SFX bus has no limiter; the summed mix clips in every measured scenario.");
		}

		// The tiers only work because Critical is rare. Promote enough sounds into it and the
		// duck never releases, the priority bus stops meaning anything, and the mix pumps.
		int critical = 0;
		foreach (string name in SfxCatalog.AllNames)
		{
			if (SfxCatalog.TierOf(name) == SfxCatalog.SfxTier.Critical)
				critical++;
		}
		if (critical > MaxCriticalSounds)
		{
			warnings.Add($"Audio: {critical} sounds are tier Critical (limit {MaxCriticalSounds}). "
				+ "Critical ducks everything else and is never stolen; it has to stay rare to mean anything.");
		}
	}

	// Ten today. The ceiling is deliberately close to that number so raising it is a decision
	// somebody makes on purpose rather than a threshold nobody notices drifting.
	private const int MaxCriticalSounds = 14;


	// The stage list is the only screen that says what a chapter is and what the dark wizard has
	// done to it, and a blank string there is invisible to the compiler: the card simply renders
	// one line shorter. Index/id agreement matters more - Global.SelectedStageIdx is an index into
	// this array, while unlocks are recorded against the id, so a mismatch unlocks the wrong place.
	private static void ValidateStageCatalog(List<string> warnings)
	{
		var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		for (int i = 0; i < StageCatalog.All.Count; i++)
		{
			StageDefinition stage = StageCatalog.All[i];

			if (stage.Index != i)
				warnings.Add($"Stage '{stage.Id}' sits at position {i} but declares Index {stage.Index}; a selection would enter a different chapter than the card clicked.");

			if (!string.Equals(stage.Id, $"stage_{i}", StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Stage at position {i} has id '{stage.Id}', not 'stage_{i}'; boss unlocks and save entries match on that id.");

			if (!seenIds.Add(stage.Id))
				warnings.Add($"Stage id '{stage.Id}' appears more than once, so one of the two can never be unlocked.");

			if (string.IsNullOrWhiteSpace(stage.DisplayName))
				warnings.Add($"Stage '{stage.Id}' has no DisplayName, so its card and the game-over screen would both be blank.");

			if (string.IsNullOrWhiteSpace(stage.FlavorText))
				warnings.Add($"Stage '{stage.Id}' has no FlavorText, so its card and the in-run intro banner say nothing about the place.");

			// Exempt the one chapter that is not somewhere he has been. CorruptionText exists to
			// say what the dark wizard has done to a place, and demanding it of a chapter outside
			// the campaign would mean inventing fiction to satisfy a check.
			if (string.IsNullOrWhiteSpace(stage.CorruptionText)
				&& stage.EnvironmentKind != StageEnvironmentKind.Sketchbook)
			{
				warnings.Add($"Stage '{stage.Id}' has no CorruptionText, so its card never says how the dark wizard holds it.");
			}

			if (!stage.IsPlayable && string.IsNullOrWhiteSpace(stage.LockedHint) && stage.Gate != StageGate.PreviousBossAndWizards)
				warnings.Add($"Stage '{stage.Id}' is not playable and has no LockedHint, so its card locks with no explanation.");
		}
	}

	// THE CAMPAIGN ORDER (.ai/campaign.md). StageCatalog's CampaignOrder is the only place it is
	// written, and every rule about which chapter opens which reads it - so a slip in it is a chapter
	// nobody can reach, a gate that points forward, or a finale that opens early. A build sees none of
	// it, and neither does a player until he is stuck.
	private static void ValidateCampaign(List<string> warnings)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string id in StageCatalog.CampaignOrderIds)
		{
			if (StageCatalog.GetById(id) == null)
				warnings.Add($"Campaign: the order names '{id}', which is not a stage in StageCatalog.");
			// A chapter listed twice comes after itself: the order would be a cycle.
			if (!seen.Add(id))
				warnings.Add($"Campaign: '{id}' appears twice in the order, so it would have to be beaten to open itself.");
		}

		IReadOnlyList<StageDefinition> campaign = StageCatalog.Campaign;
		if (campaign.Count == 0)
		{
			warnings.Add("Campaign: the order is empty, so nothing opens anything.");
			return;
		}

		if (campaign[0].Gate != StageGate.Open || !campaign[0].IsPlayable)
			warnings.Add($"Campaign: the first chapter, {campaign[0].DisplayName}, must be open and playable, or a new save has nowhere to start.");

		// The gate chapters (.ai/achievements-and-secrets.md 2.5, .ai/endgame-castle.md 1.9): the last
		// chapter is one, and once the road reaches the first, every chapter after it is boss-gated or is
		// another gate chapter (the castle's Throne). Nothing past a gate is ever open from the start.
		StageDefinition final = StageCatalog.FinalChapter;
		StageDefinition firstGate = StageCatalog.GateChapter;
		if (final == null || final.Gate != StageGate.PreviousBossAndWizards)
			warnings.Add("Campaign: the last chapter in the order must be a gate chapter (its previous boss and every wizard freed before it).");
		if (firstGate != null)
		{
			foreach (StageDefinition after in campaign.Skip(StageCatalog.CampaignPositionOf(firstGate.Id) + 1))
			{
				if (!StageCatalog.IsBossGated(after.Gate))
					warnings.Add($"Campaign: {after.DisplayName} comes after the gate chapter {firstGate.DisplayName} but is gated {after.Gate}; past the gate the road is boss-gated or another gate.");
			}
		}

		foreach (StageDefinition stage in StageCatalog.All)
		{
			bool inCampaign = StageCatalog.IsInCampaign(stage);
			if (!inCampaign && stage.Gate != StageGate.Open)
				warnings.Add($"Campaign: {stage.DisplayName} ({stage.Id}) is outside the order but gated {stage.Gate}, so nothing could ever open it.");
			if (inCampaign && stage.Gate == StageGate.Open && StageCatalog.CampaignPositionOf(stage.Id) > 0)
				warnings.Add($"Campaign: {stage.DisplayName} is in the middle of the order but always open, so the chapters before it gate nothing.");
			// Reached once the campaign is finished (the Sketchbook's page, .ai/world-map.md section 4): it
			// must stand outside the road, or the road would have to be finished to walk it.
			if (stage.AfterCampaign && (inCampaign || stage.Gate != StageGate.Open))
				warnings.Add($"Campaign: {stage.DisplayName} is held until the campaign is finished, so it must be outside the order and gated Open.");
		}

		for (int i = 0; i < campaign.Count; i++)
		{
			StageDefinition stage = campaign[i];
			if (!StageCatalog.IsBossGated(stage.Gate))
				continue;
			bool isGate = stage.Gate == StageGate.PreviousBossAndWizards;

			StageDefinition previous = StageCatalog.PreviousChapter(stage);
			if (previous == null)
			{
				warnings.Add($"Campaign: {stage.DisplayName} has no playable chapter before it, so no boss can ever open it.");
				continue;
			}
			if (StageCatalog.CampaignPositionOf(previous.Id) >= i)
				warnings.Add($"Campaign: {stage.DisplayName} is gated on {previous.DisplayName}, which is not earlier in the order.");
			if (!stage.IsPlayable)
				continue;
			if (BossCatalog.ForStageIndex(previous.Index) == null)
				warnings.Add($"Campaign: {stage.DisplayName} is opened by the boss of {previous.DisplayName}, which has none in BossCatalog.");
			// A gate chapter is never written open by its boss alone: that would skip the wizards.
			if (isGate && StageCatalog.ChapterOpenedBy(previous.Id) == stage)
				warnings.Add($"Campaign: beating {previous.DisplayName} writes the gate chapter {stage.DisplayName} open, without the wizards it counts.");
			else if (!isGate && StageCatalog.ChapterOpenedBy(previous.Id) != stage)
				warnings.Add($"Campaign: {stage.DisplayName}'s gate names {previous.DisplayName}, but beating {previous.DisplayName} opens {StageCatalog.ChapterOpenedBy(previous.Id)?.DisplayName ?? "nothing"}.");
		}

		// END TO END. A new save in memory, every available chapter's boss beaten the way a victory
		// records it (the lifetime record, and the chapter written to the save), until nothing new
		// opens. Every playable chapter on the road - before the first gate - must be reached that way;
		// a gate needs the wizards as well (ValidateGateReachability walks a new save through those).
		var save = new SaveData();
		for (int pass = 0; pass <= campaign.Count; pass++)
		{
			bool changed = false;
			foreach (StageDefinition stage in campaign)
			{
				BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
				if (boss == null || !GlobalStatsManager.IsStageAvailable(save, stage) || save.Lifetime.HasDefeatedBoss(boss.Id))
					continue;
				RecordVictory(save, boss, stage);
				changed = true;
			}
			if (!changed)
				break;
		}
		int gatePosition = firstGate != null ? StageCatalog.CampaignPositionOf(firstGate.Id) : campaign.Count;
		foreach (StageDefinition stage in campaign)
		{
			if (stage.IsPlayable && StageCatalog.CampaignPositionOf(stage.Id) < gatePosition
				&& !GlobalStatsManager.IsStageAvailable(save, stage))
				warnings.Add($"Campaign: {stage.DisplayName} cannot be reached by beating every boss in order from a new save.");
		}
		foreach (StageDefinition gate in StageCatalog.GateChapters.Where(s => s.IsPlayable))
		{
			if (GlobalStatsManager.IsStageAvailable(save, gate) && !GlobalStatsManager.IsGateMet(save, gate))
				warnings.Add($"Campaign: {gate.DisplayName} opens on bosses alone, without the wizards its gate counts.");
			SaveData through = SaveThroughGates();
			if (!GlobalStatsManager.IsStageAvailable(through, gate))
				warnings.Add($"Campaign: {gate.DisplayName} stays closed to a save that has beaten its previous boss and freed every wizard.");
		}

		// NOTHING UNBUILT IS EVER WRITTEN INTO A SAVE (.ai/endgame-castle.md 1.9). An unbuilt gate's
		// previous playable chapter is earlier than it will be - the Throne's is the Emberdeep until the
		// bridge and the Court exist - so a save with the Warden of the Deep beaten and every wizard free
		// "meets" it; writing that down (RecordMetGates, at every run's end) would hand the Throne to that
		// save the day it flips, past a bridge and a court it never crossed. Every boss beaten and every
		// wizard free is the save most likely to be handed something.
		SaveData veteran = SaveThroughGates();
		foreach (BossDefinition beaten in BossCatalog.All)
			veteran.Lifetime.BossesDefeated.Add(beaten.Id);
		foreach (StageDefinition written in GlobalStatsManager.RecordMetGates(veteran).Where(s => !s.IsPlayable))
			warnings.Add($"Campaign: {written.DisplayName} is not playable, but a save that has beaten every boss had it written into its opened chapters (GlobalStatsManager.RecordMetGates).");

		ValidateCampaignToDo(warnings);
	}

	// Every wizard free, then the road walked: every chapter the save can enter has its boss beaten the way a
	// victory records it, until nothing new opens. Past the first gate a chapter's previous one must be on
	// the save's road too (GlobalStatsManager.HasClearedPreviousChapter), so beating only the boss before
	// each gate - what this did until the Court and the Throne flipped on 2026-11-02 - left the Throne shut
	// behind a Court the sample had never earned.
	private static SaveData SaveThroughGates()
	{
		var save = new SaveData();
		foreach (string wizardId in UnlockCatalog.AllWizardIds)
			GlobalStatsManager.UnlockCharacter(save, wizardId);
		IReadOnlyList<StageDefinition> campaign = StageCatalog.Campaign;
		for (int pass = 0; pass <= campaign.Count; pass++)
		{
			bool changed = false;
			foreach (StageDefinition stage in campaign)
			{
				BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
				if (boss == null || !GlobalStatsManager.IsStageAvailable(save, stage) || save.Lifetime.HasDefeatedBoss(boss.Id))
					continue;
				RecordVictory(save, boss, stage);
				changed = true;
			}
			if (!changed)
				break;
		}
		return save;
	}

	// What a boss victory does to the save, the two halves Node2DGame performs at run end: the run
	// folds into the lifetime record, and the chapter the order says it opens is written.
	private static void RecordVictory(SaveData save, BossDefinition boss, StageDefinition stage)
	{
		save.Lifetime.Absorb(new global::RunResult
		{
			Outcome = global::RunOutcome.Victory,
			BossId = boss.Id,
			StageId = stage.Id,
		});
		StageDefinition opened = BossCatalog.ChapterOpenedBy(boss);
		if (opened != null)
			GlobalStatsManager.UnlockStage(save, opened.Id);
	}

	// Every spell and wizard owned and no boss beaten: the shape of a save that met the old, schema 18
	// gate. The migration samples use it; the gate itself needs a boss now (SaveThroughGates).
	private static SaveData SaveWithEverything()
	{
		var save = new SaveData();
		foreach (string spellId in UnlockCatalog.AllSpellIds)
			GlobalStatsManager.UnlockSpell(save, spellId);
		foreach (string wizardId in UnlockCatalog.AllWizardIds)
			GlobalStatsManager.UnlockCharacter(save, wizardId);
		return save;
	}

	// The Phase 2 to-do list for chapters still being built (.ai/campaign.md, the Phase 2 split), one
	// line per chapter, in the manner of the bestiary's. It shortens by itself: each item is a file
	// that does not exist yet, or a line not yet filled in, and the line goes when the chapter is
	// playable. A PLAYABLE chapter with any of these is not a to-do but a fault, and says so.
	private static void ValidateCampaignToDo(List<string> warnings)
	{
		// The world map is asked once, for the castle's chapters that have no stop yet (ValidateWorldMap
		// leaves those to this list).
		WorldMapLayout map = WorldMapLayout.Load(out _);
		var generator = new LevelGenerator();
		foreach (StageDefinition stage in StageCatalog.Campaign)
		{
			var todo = new List<string>();
			BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
			if (boss != null && boss.IsOnStandIn)
				todo.Add($"boss {boss.DisplayName} fights as '{ShortScenePath(boss.StandInScenePath)}' until '{ShortScenePath(boss.ScenePath)}' exists");
			MusicSet own = MusicCatalog.OwnSetForStage(stage.Index);
			if (own != null && own.StandIn != null && !own.IsRendered)
				todo.Add($"music plays '{own.StandIn.Id}' until {own.Id}_bed/_pulse/_lead.wav are rendered and imported");
			// The castle's plumbing (.ai/endgame-castle.md 1.9): a topology named and not yet built, rules
			// designed and still the null line, a place on the world map not yet drawn.
			LevelLayout ground = generator.Describe(stage.EnvironmentKind);
			if (ground.DeclaredTopology != ground.Topology)
				todo.Add($"its ground is open field until LevelGenerator.{ground.DeclaredTopology}.cs builds the {ground.DeclaredTopology} topology");
			if (ChapterRulesCatalog.IsAwaitingRules(stage.Id))
				todo.Add("its rules are still the null line in ChapterRulesCatalog");
			// (A playable chapter with no stop is ValidateWorldMap's fault to report, not a to-do.)
			if (!stage.IsPlayable && map != null && map.Get(stage.Id) == null)
				todo.Add("it has no place on the world map (PLACES in tools/art/world_map.py)");

			if (todo.Count == 0)
				continue;
			if (stage.IsPlayable)
				warnings.Add($"Campaign: {stage.DisplayName} is playable but still has stand-ins - " + string.Join("; ", todo) + ".");
			else
				warnings.Add($"Campaign to-do (.ai/campaign.md 7, .ai/endgame-castle.md 9): {stage.DisplayName} ({stage.Id}) is not playable yet - "
					+ string.Join("; ", todo) + "; then IsPlayable = true, last.");
		}

		// A thing found in the world needs somewhere to find it. A discovery whose site no chapter's
		// side event pays out can never be had - a fault once its chapter is built, a to-do before.
		foreach (UnlockDefinition found in UnlockCatalog.All.Where(d => !string.IsNullOrWhiteSpace(d.SiteId)))
		{
			StageDefinition holder = SideEventDirector.ChapterOfSite(found.SiteId);
			if (holder == null)
				warnings.Add($"Campaign to-do (Phase 2, .ai/campaign.md): {found.Kind} '{found.Id}' is found at '{found.SiteId}', and no chapter's side event (SideEventDirector.EventsFor) is that site yet, so nobody can find it.");
			else if (!holder.IsPlayable)
				warnings.Add($"Campaign to-do (Phase 2, .ai/campaign.md): {found.Kind} '{found.Id}' is found in {holder.DisplayName}, which is not playable yet.");
		}
	}

	// SCHEMA 13 ON SAMPLE SAVES (SaveData.MigrateToCampaignOrder). Each is a save as schema 12 wrote
	// it, migrated in memory and then asked which chapters it can enter. The rules being held: nobody
	// loses a chapter they had; a boss a save has beaten opens what it opens NOW; and a save already
	// past the two early chapters has them, so they can never block anyone.
	// Schema 18 (Save and Augury removed): a save holding two Hoarded Insights and one Augury is paid
	// 110 + 190 + 130 = 430 back, and the two rows leave it; a save that bought none is untouched; and
	// nothing still reads the retired ids, or the refund would be a refund for something still sold.
	private static void ValidateRetiredChargeRefund(List<string> warnings)
	{
		var bought = new SaveData { SchemaVersion = 17, TotalCurrency = 50 };
		bought.ArcaneUpgradeLevels["banked_levels"] = 2;
		bought.ArcaneUpgradeLevels["auguries"] = 1;
		bought.ArcaneUpgradeLevels["bans"] = 1;
		bought.Migrate(log: false);
		if (bought.TotalCurrency != 50 + 430 || bought.ArcaneUpgradeLevels.ContainsKey("banked_levels")
			|| bought.ArcaneUpgradeLevels.ContainsKey("auguries") || !bought.ArcaneUpgradeLevels.ContainsKey("bans"))
			warnings.Add($"Migration: schema 18 should refund 430 for two Hoarded Insights and an Augury and keep the ban; "
				+ $"the purse is {bought.TotalCurrency} (from 50) and the rows are {string.Join(", ", bought.ArcaneUpgradeLevels.Keys)}.");

		var none = new SaveData { SchemaVersion = 17, TotalCurrency = 50 };
		none.Migrate(log: false);
		if (none.TotalCurrency != 50)
			warnings.Add($"Migration: schema 18 paid {none.TotalCurrency - 50} to a save that bought no retired charge.");
	}

	// Story resets (.ai/decisions/2026-11-02-story-resets.md), on sample saves:
	//   - a save from an earlier epoch comes back as a brand-new save in every key but the settings
	//     (SaveData.SettingsKeys), which come back as the player's, with the title's notice owed;
	//   - a save in this epoch, a save from a newer one, and (while the constant is 1) a save written
	//     before epochs existed are not reset;
	//   - on disk, through the real load (SaveManager.LoadFrom) against a scratch file: the old save is
	//     kept beside it word for word and the file is rewritten in the new epoch, while a same-epoch save
	//     is left as it was and kept nowhere. Debug builds only, so a player's copy never writes a scratch
	//     file; the folder is per process, so validators run side by side cannot meet in it.
	private static void ValidateStoryReset(List<string> warnings)
	{
		SaveData Earned(int epoch)
		{
			var s = new SaveData
			{
				ProgressEpoch = epoch, TotalCurrency = 900, HasSeenPrologue = true, HasSeenGameplayOnboarding = true,
				AscensionLearned = true, BossRushBestFelled = 3, BossRushBestClearSeconds = 412f,
				// Every setting off its default, so "kept" cannot pass by coincidence.
				MasterVolume = 0.3f, MusicVolume = 0.2f, SfxVolume = 0.6f, AudioMuted = true, ShowDamageNumbers = false,
				ReduceScreenShake = true,
				EnableGameplayOnboardingTips = false, HasToggledOnboardingTipsAtLeastOnce = true,
				PlaytestModeEnabled = true, BalancePresetId = GlobalStatsManager.BalancePresetHardcore,
			};
			s.PlaytestChecklistState["tips_toggle"] = true;
			s.UnlockedSpellIds.Add("gravewell");
			s.UnlockedCharacterIds.Add("hayward");
			s.UnlockedStageIds.Add("stage_1");
			s.UnlockedBoonIds.Add("sunsteel_filament");
			s.UnlockedAchievementIds.Add("survivor");
			s.RemovedSpellIds.Add("fireball");
			s.SeenAscensionIds.Add(SaveData.AscensionKey("fireball", "inferno"));
			s.MaxDifficultyCleared["stage_0"] = 2;
			s.ArcaneUpgradeLevels["vitality"] = 4;
			s.FoundSecretIds.Add("swallow_hole");
			s.FoundUpgradeLevels["vitality"] = 1;
			s.GlimpsedSecretIds.Add("swallow_hole");
			s.TrackedQuestIds.Clear();
			s.SeenChapterNoteIds.Add("stage_0");
			s.Lifetime.TotalRuns = 12;
			s.Lifetime.BossesDefeated.Add("elderbark");
			s.Lifetime.BestTimeByStage["stage_0"] = 640f;
			s.RecordRunTelemetry(new RunResult());
			return s;
		}

		// In memory: key by key against a new save.
		SaveData old = Earned(SaveData.CurrentProgressEpoch - 1);
		if (!old.IsFromEarlierStory)
			warnings.Add("Story reset: a save one epoch behind does not read as from an earlier story.");
		SaveData reset = old.StartNewStory();
		Godot.Collections.Dictionary before = old.ToGodotDictionary();
		Godot.Collections.Dictionary after = reset.ToGodotDictionary();
		Godot.Collections.Dictionary fresh = new SaveData().ToGodotDictionary();
		foreach (string key in SaveData.SettingsKeys.Where(k => !fresh.ContainsKey(k)))
			warnings.Add($"Story reset: SettingsKeys names '{key}', which is not a key of the save - a renamed setting is reset.");
		foreach (Variant k in fresh.Keys)
		{
			string key = k.AsString();
			if (key == "ProgressEpoch" || key == "ProgressResetNoticePending")
				continue;
			bool setting = SaveData.SettingsKeys.Contains(key);
			string want = Json.Stringify(setting ? before[key] : fresh[key]);
			string got = after.ContainsKey(key) ? Json.Stringify(after[key]) : "(missing)";
			if (got != want)
				warnings.Add($"Story reset: '{key}' came back as {got}, expected {(setting ? "the player's setting" : "a new save's")} {want}.");
			else if (setting && Json.Stringify(fresh[key]) == want)
				warnings.Add($"Story reset: the sample leaves the setting '{key}' at its default, so keeping it proves nothing.");
		}
		// And named outright, because the loop above takes SettingsKeys at its word: a progress key listed
		// there as a setting would be "kept" and pass. What Eric's reset must take back, every piece of it.
		var kept = new List<string>();
		if (reset.TotalCurrency != 0) kept.Add("Arcane Energy");
		if (reset.UnlockedSpellIds.Count + reset.UnlockedBoonIds.Count + reset.UnlockedCharacterIds.Count > 0) kept.Add("unlocks");
		if (reset.UnlockedStageIds.Count + reset.MaxDifficultyCleared.Count > 0) kept.Add("chapters");
		if (reset.UnlockedAchievementIds.Count > 0 || reset.Lifetime.TotalRuns > 0 || reset.Lifetime.BossesDefeated.Count > 0) kept.Add("achievements");
		if (reset.ArcaneUpgradeLevels.Count + reset.FoundUpgradeLevels.Count > 0) kept.Add("shop and found levels");
		if (reset.FoundSecretIds.Count + reset.GlimpsedSecretIds.Count > 0) kept.Add("secrets");
		if (!reset.TrackedQuestIds.SequenceEqual(new SaveData().TrackedQuestIds)) kept.Add("quests");
		if (reset.HasSeenPrologue || reset.HasSeenGameplayOnboarding || reset.SeenChapterNoteIds.Count + reset.SeenAscensionIds.Count > 0 || reset.AscensionLearned)
			kept.Add("seen flags and the prologue");
		if (kept.Count > 0)
			warnings.Add($"Story reset: the new story still holds the old one's {string.Join(", ", kept)}.");
		if (reset.ProgressEpoch != SaveData.CurrentProgressEpoch || reset.IsFromEarlierStory)
			warnings.Add($"Story reset: the new save is in epoch {reset.ProgressEpoch}, not {SaveData.CurrentProgressEpoch}.");
		if (!reset.ProgressResetNoticePending)
			warnings.Add("Story reset: the new save does not owe the title's notice, so the player is never told.");
		if (reset.Migrate(log: false))
			warnings.Add("Story reset: the new save needed migrating; it should already be at the current schema.");

		// Not reset: this epoch, a newer one, and a save with no epoch key at all.
		if (Earned(SaveData.CurrentProgressEpoch).IsFromEarlierStory)
			warnings.Add("Story reset: a save in this build's epoch reads as from an earlier story.");
		if (Earned(SaveData.CurrentProgressEpoch + 1).IsFromEarlierStory)
			warnings.Add("Story reset: a save from a newer epoch (a newer build's) would be reset by this one.");
		Godot.Collections.Dictionary unmarked = Earned(SaveData.CurrentProgressEpoch).ToGodotDictionary();
		unmarked.Remove("ProgressEpoch");
		bool unmarkedResets = SaveData.FromVariant(unmarked).IsFromEarlierStory;
		if (unmarkedResets != (SaveData.CurrentProgressEpoch > SaveData.FirstProgressEpoch))
			warnings.Add(unmarkedResets
				? "Story reset: a save from before epochs existed is reset while the constant is still the first epoch."
				: "Story reset: a save from before epochs existed is not reset although the story has been rebuilt since.");

		if (!OS.IsDebugBuild())
			return;

		// On disk, through the real load. Never SaveManager.SavePath: a scratch folder of this process's own.
		string dir = $"user://story-reset-check-{OS.GetProcessId()}";
		string path = dir + "/savegame.json";
		DirAccess.MakeDirRecursiveAbsolute(dir);
		try
		{
			string WriteSample(SaveData sample)
			{
				string text = Json.Stringify(sample.ToGodotDictionary());
				using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
				file?.StoreString(text);
				return text;
			}
			string backup = SaveManager.BackupPathFor(path, SaveData.CurrentProgressEpoch);

			string oldText = WriteSample(Earned(SaveData.CurrentProgressEpoch - 1));
			SaveData loaded = SaveManager.LoadFrom(path, writes: true, log: false);
			if (loaded.IsFromEarlierStory || loaded.TotalCurrency != 0 || loaded.UnlockedSpellIds.Count != 0
				|| loaded.MasterVolume != 0.3f || !loaded.ProgressResetNoticePending)
				warnings.Add("Story reset (disk): an earlier-epoch save did not load as a new story with its settings and the notice.");
			if (!FileAccess.FileExists(backup))
				warnings.Add($"Story reset (disk): the old save was not kept at {backup}.");
			else if (FileAccess.GetFileAsString(backup) != oldText)
				warnings.Add("Story reset (disk): the kept save is not the old save word for word.");
			SaveData reread = SaveManager.LoadFrom(path, writes: false, log: false);
			if (reread.ProgressEpoch != SaveData.CurrentProgressEpoch || reread.TotalCurrency != 0)
				warnings.Add("Story reset (disk): the save file was not rewritten in the new epoch, so the next launch resets again.");

			DirAccess.RemoveAbsolute(backup);
			string sameText = WriteSample(Earned(SaveData.CurrentProgressEpoch));
			SaveData same = SaveManager.LoadFrom(path, writes: true, log: false);
			if (same.TotalCurrency != 900 || same.ProgressResetNoticePending)
				warnings.Add("Story reset (disk): a save in this epoch was reset.");
			if (FileAccess.FileExists(backup))
				warnings.Add("Story reset (disk): a save in this epoch was backed up as if it had been reset.");
			if (FileAccess.GetFileAsString(path) != sameText)
				warnings.Add("Story reset (disk): a save in this epoch was rewritten on load.");
		}
		finally
		{
			using DirAccess folder = DirAccess.Open(dir);
			if (folder != null)
			{
				foreach (string file in folder.GetFiles())
					folder.Remove(file);
			}
			DirAccess.RemoveAbsolute(dir);
		}
	}

	// The rename's save carry-over (UserDataCarryOver, .ai/releasing.md "The rename"), run for real against
	// scratch folders of this process's own - never the player's user:// folder or the old one beside it:
	//   - everything is copied, the save included, except the updater's scratch folder;
	//   - a file the new folder already has is not overwritten, and the old folder is left byte for byte;
	//   - a second launch copies nothing (the marker), and nor does a new folder holding a save of its own.
	private static void ValidateUserDataCarryOver(List<string> warnings)
	{
		if (!OS.IsDebugBuild())
			return;

		string root = ProjectSettings.GlobalizePath($"user://carry-over-check-{OS.GetProcessId()}");
		string old = System.IO.Path.Combine(root, "old");
		string fresh = System.IO.Path.Combine(root, "new");
		string inUse = System.IO.Path.Combine(root, "in-use");
		void Write(string path, string text)
		{
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
			System.IO.File.WriteAllText(path, text);
		}
		string Read(string path) => System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null;
		string Snapshot(string dir)
		{
			var lines = new List<string>();
			foreach (string file in System.IO.Directory.GetFiles(dir, "*", System.IO.SearchOption.AllDirectories))
				lines.Add(System.IO.Path.GetRelativePath(dir, file) + "=" + System.IO.File.ReadAllText(file));
			lines.Sort(System.StringComparer.Ordinal);
			return string.Join("\n", lines);
		}

		try
		{
			Write(System.IO.Path.Combine(old, "savegame.json"), "{\"TotalCurrency\": 900}");
			Write(System.IO.Path.Combine(old, "savegame.before-epoch-2.json"), "{\"TotalCurrency\": 12}");
			Write(System.IO.Path.Combine(old, "logs", "godot.log"), "the old log");
			Write(System.IO.Path.Combine(old, "playtest_logs", "run", "events.txt"), "deep");
			Write(System.IO.Path.Combine(old, "update", "files", "TheOneHeMissed.pck"), "scratch");
			Write(System.IO.Path.Combine(fresh, "logs", "godot.log"), "this launch's log");
			string before = Snapshot(old);

			var (outcome, files) = UserDataCarryOver.CarryOver(old, fresh);
			if (outcome != UserDataCarryOver.Outcome.Carried || files != 3)
				warnings.Add($"Save carry-over: expected 3 files carried, got {outcome} with {files}.");
			if (Read(System.IO.Path.Combine(fresh, "savegame.json")) != "{\"TotalCurrency\": 900}")
				warnings.Add("Save carry-over: the save was not copied word for word.");
			if (Read(System.IO.Path.Combine(fresh, "savegame.before-epoch-2.json")) == null
				|| Read(System.IO.Path.Combine(fresh, "playtest_logs", "run", "events.txt")) != "deep")
				warnings.Add("Save carry-over: a file beside the save, or one in a folder, was not copied.");
			if (Read(System.IO.Path.Combine(fresh, "logs", "godot.log")) != "this launch's log")
				warnings.Add("Save carry-over: a file the new folder already had was overwritten.");
			if (System.IO.Directory.Exists(System.IO.Path.Combine(fresh, "update")))
				warnings.Add("Save carry-over: the updater's scratch folder was copied.");
			if (Snapshot(old) != before)
				warnings.Add("Save carry-over: the old folder was changed.");

			System.IO.File.WriteAllText(System.IO.Path.Combine(old, "savegame.json"), "{\"TotalCurrency\": 5}");
			if (UserDataCarryOver.CarryOver(old, fresh).Outcome != UserDataCarryOver.Outcome.AlreadyCarried
				|| Read(System.IO.Path.Combine(fresh, "savegame.json")) != "{\"TotalCurrency\": 900}")
				warnings.Add("Save carry-over: a second launch copied again.");

			Write(System.IO.Path.Combine(inUse, "savegame.json"), "{\"TotalCurrency\": 1}");
			if (UserDataCarryOver.CarryOver(old, inUse).Outcome != UserDataCarryOver.Outcome.NewFolderHasASave
				|| Read(System.IO.Path.Combine(inUse, "savegame.json")) != "{\"TotalCurrency\": 1}"
				|| System.IO.Directory.Exists(System.IO.Path.Combine(inUse, "logs")))
				warnings.Add("Save carry-over: a folder with a save of its own was copied into.");

			if (UserDataCarryOver.CarryOver(System.IO.Path.Combine(root, "missing"), System.IO.Path.Combine(root, "other")).Outcome
				!= UserDataCarryOver.Outcome.NoOldFolder)
				warnings.Add("Save carry-over: a missing old folder did not read as nothing to carry.");
		}
		finally
		{
			if (System.IO.Directory.Exists(root))
				System.IO.Directory.Delete(root, recursive: true);
		}
	}

	// Schema 20 (.ai/difficulty-curve.md, section 9), on sample saves:
	//   - a save that has played runs keeps the Ascension (AscensionLearned), and one that never played
	//     starts sealed like a new save;
	//   - Fate Fracture 5 and Emberheart 3 are refunded the price difference (425 + 75) and keep their levels;
	//   - a second migration changes nothing.
	// "A save past the Fields is learned by the campaign alone" went with the slow start: a landmark teaches
	// the Ascension now (Teachings), and schema 22 keeps it for every save that had it that way
	// (ValidateSlowStartMigration).
	private static void ValidateDifficultyMigration(List<string> warnings)
	{
		var played = new SaveData { SchemaVersion = 19, TotalCurrency = 10 };
		played.Lifetime.TotalRuns = 3;
		played.ArcaneUpgradeLevels["rerolls"] = 5;
		played.ArcaneUpgradeLevels["recovery"] = 3;
		played.Migrate(log: false);
		// rerolls: sum over n<5 of (75+50n)-(40+25n) = 5x35 + 25x10 = 425; recovery: sum over n<3 of (30+20n)-(15+10n) = 45 + 30 = 75.
		const int expected = 425 + 75;
		if (!played.AscensionLearned || !Teachings.HasLearned(played, Teaching.Ascension))
			warnings.Add("Migration: schema 20 should keep the Ascension for a save that has played a run.");
		if (played.TotalCurrency != 10 + expected || played.ArcaneUpgradeLevels["rerolls"] != 5 || played.ArcaneUpgradeLevels["recovery"] != 3)
			warnings.Add($"Migration: schema 20 should refund {expected} for Fate Fracture 5 and Emberheart 3 and keep both levels;"
				+ $" the purse is {played.TotalCurrency} (from 10).");
		int purse = played.TotalCurrency;
		played.Migrate(log: false);
		if (played.TotalCurrency != purse)
			warnings.Add("Migration: a second schema 20 migration paid the refund again.");

		var unplayed = new SaveData { SchemaVersion = 19 };
		unplayed.Migrate(log: false);
		if (unplayed.AscensionLearned || Teachings.HasLearned(unplayed, Teaching.Ascension))
			warnings.Add("Migration: a save that never played a run should start with the Ascension sealed, as a new save does.");
		if (Teachings.HasLearned(new SaveData(), Teaching.Ascension))
			warnings.Add("Ascension: a brand-new save has already learned it; a landmark should teach it (Teachings).");
	}

	// The difficulty curve's table (.ai/difficulty-curve.md, section 4): every playable road chapter has a
	// row; every multiplier is positive; enemy damage is never under 1 (under it a touch can roll nothing
	// and read as a dodge) and never eases along the road; the first chapter is the easiest by health.
	private static void ValidateChapterDifficulty(List<string> warnings)
	{
		float lastDamage = 1f;
		ChapterDifficulty first = null;
		foreach (StageDefinition stage in StageCatalog.Campaign)
		{
			if (!stage.IsPlayable)
				continue;
			ChapterDifficulty row = ChapterDifficultyCatalog.For(stage.Id);
			if (ReferenceEquals(row, ChapterDifficultyCatalog.Neutral))
			{
				warnings.Add($"Difficulty: {stage.DisplayName} has no row in ChapterDifficultyCatalog.");
				continue;
			}
			if (row.EnemyHealth <= 0f || row.EliteInterval <= 0f || row.ElitePower <= 0f)
				warnings.Add($"Difficulty: {stage.DisplayName} has a multiplier at or under zero.");
			if (row.EnemyDamage < 1f)
				warnings.Add($"Difficulty: {stage.DisplayName}'s EnemyDamage {row.EnemyDamage} is under 1.");
			if (row.EnemyDamage < lastDamage)
				warnings.Add($"Difficulty: {stage.DisplayName}'s EnemyDamage {row.EnemyDamage} is under the chapter before it ({lastDamage}).");
			lastDamage = Math.Max(lastDamage, row.EnemyDamage);
			if (first == null)
				first = row;
			else if (row.EnemyHealth < first.EnemyHealth)
				warnings.Add($"Difficulty: {stage.DisplayName}'s EnemyHealth is under the first chapter's.");
		}
	}

	private static void ValidateCampaignMigration(List<string> warnings)
	{
		var fresh = new SaveData();
		foreach (StageDefinition stage in StageCatalog.All)
		{
			if (GlobalStatsManager.IsStageAvailable(fresh, stage) && stage.Gate != StageGate.Open)
				warnings.Add($"Migration: a brand-new save can enter {stage.DisplayName}, which is gated {stage.Gate}.");
		}

		// (what the old save held, what must be open after, what must still be closed)
		CheckMigration(warnings, "beat Elderbark", stored: new[] { "stage_1" },
			open: new[] { "stage_0", "stage_1", "stage_2" }, closed: new[] { "stage_3" });
		CheckMigration(warnings, "beat Elderbark and the Gaoler", stored: new[] { "stage_1", "stage_2" },
			open: new[] { "stage_0", "stage_1", "stage_2", "stage_3" }, closed: new[] { "stage_4" });
		CheckMigration(warnings, "beat everything up to the Still Warden",
			stored: new[] { "stage_1", "stage_2", "stage_3", "stage_4", "stage_5", "stage_6" },
			open: new[] { "stage_0", "stage_1", "stage_2", "stage_3", "stage_4", "stage_5", "stage_6" },
			closed: new[] { "stage_7" });
		// Known only from the lifetime record, and only from a deed: the Gaoler opens the Swamp now.
		CheckMigration(warnings, "the Gaoler in the lifetime record only", stored: new string[0],
			open: new[] { "stage_1", "stage_2", "stage_3" }, closed: new[] { "stage_4" },
			lifetimeBosses: new[] { "forest_treant", "cave_choir", "castle_warden" });
		CheckMigration(warnings, "the Gaoler's deed only", stored: new[] { "stage_1" },
			open: new[] { "stage_1", "stage_2", "stage_3" }, closed: new[] { "stage_4" },
			deeds: new[] { "castle_conqueror" });
		// A shape a real save has: chapters open out of the old order's sequence.
		CheckMigration(warnings, "a gap in the old order", stored: new[] { "stage_1", "stage_3", "stage_4" },
			open: new[] { "stage_0", "stage_1", "stage_2", "stage_3", "stage_4" }, closed: new[] { "stage_5" });

		// A save that had met the old gate - every spell and wizard - can still enter the finale after
		// every step (schema 19 writes it open for such a save, whatever the gate has become).
		SaveData done = SaveWithEverything();
		done.SchemaVersion = 12;
		done.Migrate(log: false);
		// The gate chapter (the Emberdeep), which is what such a save had met - not FinalChapter, which is
		// the castle's Throne and not playable yet, so reading it would quietly check nothing.
		StageDefinition final = StageCatalog.GateChapter;
		if (final != null && final.IsPlayable && !GlobalStatsManager.IsStageAvailable(done, final))
			warnings.Add($"Migration: a save with every spell and wizard cannot enter {final.DisplayName} after schema 13.");
	}

	private static void CheckMigration(List<string> warnings, string label, string[] stored, string[] open,
		string[] closed, string[] lifetimeBosses = null, string[] deeds = null)
	{
		var save = new SaveData { SchemaVersion = 12 };
		save.UnlockedStageIds.AddRange(stored);
		foreach (string bossId in lifetimeBosses ?? new string[0])
			save.Lifetime.BossesDefeated.Add(bossId);
		save.UnlockedAchievementIds.AddRange(deeds ?? new string[0]);

		if (!save.Migrate(log: false))
			warnings.Add($"Migration ({label}): a schema-12 save was not migrated.");

		// Nobody loses anything: every chapter the save had opened is still there and still enterable - in
		// the opened list, or, for the Cursed Dungeon since schema 21, kept grandfathered.
		foreach (string id in stored)
		{
			StageDefinition had = StageCatalog.GetById(id);
			bool kept = save.UnlockedStageIds.Contains(id) || save.GrandfatheredStageIds.Contains(id);
			if (!kept || (had != null && had.IsPlayable && !GlobalStatsManager.IsStageAvailable(save, had)))
				warnings.Add($"Migration ({label}): the save lost {had?.DisplayName ?? id}.");
		}
		foreach (string id in open)
		{
			StageDefinition stage = StageCatalog.GetById(id);
			if (stage != null && stage.IsPlayable && !GlobalStatsManager.IsStageAvailable(save, stage))
				warnings.Add($"Migration ({label}): {stage.DisplayName} should be open after every step and is not.");
		}

		// Schema 21 (.ai/endgame-castle.md 1.5): none of these saves is past the gate, so the Dungeon it reached
		// as an early chapter is kept grandfathered, out of the opened list, and earns it nothing in the castle.
		StageDefinition dungeon = StageCatalog.GetById("stage_1");
		if (dungeon != null && stored.Contains("stage_1"))
		{
			if (!save.GrandfatheredStageIds.Contains("stage_1") || save.UnlockedStageIds.Contains("stage_1"))
				warnings.Add($"Migration ({label}): the save had the Cursed Dungeon from the old order and it was not moved to the grandfathered list.");
			if (GlobalStatsManager.IsStageEarned(save, dungeon))
				warnings.Add($"Migration ({label}): the Cursed Dungeon from the old order counts as earned on the castle road.");
		}
		foreach (StageDefinition castle in ChaptersPastGate())
		{
			if (GlobalStatsManager.IsStageEarned(save, castle))
				warnings.Add($"Migration ({label}): the save earns {castle.DisplayName}, past the gate, from what it did before it.");
		}
		foreach (string id in closed)
		{
			StageDefinition stage = StageCatalog.GetById(id);
			if (stage != null && GlobalStatsManager.IsStageAvailable(save, stage))
				warnings.Add($"Migration ({label}): {stage.DisplayName} opened, though nothing this save did earns it.");
		}

		// The early chapters never block anyone: a save that is at the Cave or past it has earned
		// both, whether or not they are built yet - and so has them the day they become playable.
		bool pastThem = save.UnlockedStageIds.Any(id => StageCatalog.CampaignPositionOf(id) > StageCatalog.CampaignPositionOf("stage_10"));
		foreach (string early in new[] { "stage_9", "stage_10" })
		{
			StageDefinition stage = StageCatalog.GetById(early);
			if (pastThem && stage != null && !GlobalStatsManager.IsStageEarned(save, stage))
				warnings.Add($"Migration ({label}): the save is past {stage.DisplayName} but has not earned it, so it would block him when it opens.");
		}

		// Once is enough: a second migration finds nothing to do.
		int before = save.UnlockedStageIds.Count;
		if (save.Migrate(log: false) || save.UnlockedStageIds.Count != before)
			warnings.Add($"Migration ({label}): migrating twice changed the save again.");
	}

	// Every campaign chapter after the first gate: the castle's, and the Cursed Dungeon since C2.
	private static IEnumerable<StageDefinition> ChaptersPastGate()
	{
		StageDefinition gate = StageCatalog.GateChapter;
		return gate == null ? Enumerable.Empty<StageDefinition>()
			: StageCatalog.Campaign.Skip(StageCatalog.CampaignPositionOf(gate.Id) + 1);
	}

	// THE CURSED DUNGEON MOVES INTO THE CASTLE (SaveData.MigrateDungeonIntoCastle, schema 21;
	// .ai/endgame-castle.md 1.5). One save per row of 1.5's table, as schema 19 or 20 wrote it, migrated in
	// memory. The rules being held:
	//   - what it could enter before (worked by hand here, and the step's frozen rules must agree) it can
	//     enter after; the Dungeon is kept grandfathered, out of the opened list, exactly when it had it;
	//   - a save short of the Emberdeep earns nothing past the gate - not the castle, not the Dungeon;
	//   - a win in a grandfathered Dungeon writes nothing to the opened list, and still pays the deed;
	//   - a save that finished the old campaign keeps the Sketchbook and the Boss Rush, and its Dungeon joins
	//     the road when its previous chapter's boss is down (the Warden today, the Constable once the bridge
	//     is built), the Court with it;
	//   - a second migration changes nothing.
	private static void ValidateDungeonMove(List<string> warnings)
	{
		StageDefinition dungeon = StageCatalog.GetById("stage_1");
		StageDefinition gate = StageCatalog.GateChapter;
		BossDefinition gaoler = dungeon != null ? BossCatalog.ForStageIndex(dungeon.Index) : null;
		if (dungeon == null || gate == null || gaoler == null)
		{
			warnings.Add("Dungeon move: the Cursed Dungeon, its boss or the gate chapter is missing.");
			return;
		}
		if (StageCatalog.CampaignPositionOf(dungeon.Id) <= StageCatalog.CampaignPositionOf(gate.Id))
			warnings.Add($"Dungeon move: the Cursed Dungeon stands before {gate.DisplayName} in the order; it belongs in the castle.");
		BossDefinition finale = BossCatalog.ForStageIndex(gate.Index);
		StageDefinition court = StageCatalog.GetById("stage_12");
		string[] road = { "forest_treant", "fields_hayward", "heath_ninth_stone" };

		SaveData Sample(string label, int schema, string[] stored, string[] beaten, string[] wizards, string[] couldEnter,
			Action<SaveData> check)
		{
			var save = new SaveData { SchemaVersion = schema };
			save.UnlockedStageIds.AddRange(stored);
			save.Lifetime.BossesDefeated.AddRange(beaten);
			save.UnlockedCharacterIds.AddRange(wizards);

			List<string> frozen = save.ChaptersEnterableAtSchema19();
			if (!frozen.OrderBy(id => id).SequenceEqual(couldEnter.OrderBy(id => id)))
				warnings.Add($"Dungeon move ({label}): the step's frozen schema 19 rules say it could enter [{string.Join(", ", frozen)}]; by hand, [{string.Join(", ", couldEnter)}].");

			if (!save.Migrate(log: false))
				warnings.Add($"Dungeon move ({label}): a schema-{schema} save was not migrated.");
			foreach (string id in couldEnter)
			{
				StageDefinition had = StageCatalog.GetById(id);
				if (had != null && had.IsPlayable && !GlobalStatsManager.IsStageAvailable(save, had))
					warnings.Add($"Dungeon move ({label}): the save could enter {had.DisplayName} before and cannot now.");
			}
			bool hadDungeon = couldEnter.Contains(dungeon.Id);
			if (hadDungeon != GlobalStatsManager.IsGrandfathered(save, dungeon))
				warnings.Add($"Dungeon move ({label}): the Cursed Dungeon is {(hadDungeon ? "not " : "")}grandfathered, though the save {(hadDungeon ? "could" : "could not")} enter it.");
			if (save.UnlockedStageIds.Contains(dungeon.Id, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"Dungeon move ({label}): the Cursed Dungeon is still in the opened list, where rule 2 reads it as being past the road.");
			if (finale != null && !save.Lifetime.HasDefeatedBoss(finale.Id))
			{
				foreach (StageDefinition past in ChaptersPastGate())
				{
					if (GlobalStatsManager.IsStageEarned(save, past))
						warnings.Add($"Dungeon move ({label}): a save short of {gate.DisplayName} earns {past.DisplayName}.");
				}
			}
			check(save);

			// Twice: the step again on the migrated save, and Migrate, change nothing.
			string once = Json.Stringify(save.ToGodotDictionary());
			save.MigrateDungeonIntoCastle();
			if (save.Migrate(log: false) || Json.Stringify(save.ToGodotDictionary()) != once)
				warnings.Add($"Dungeon move ({label}): migrating twice changed the save again.");
			return save;
		}

		// A Gaoler beaten where the save keeps the Dungeon only grandfathered: everything but the road pays.
		void WinGrandfathered(string label, SaveData save)
		{
			var before = save.UnlockedStageIds.ToList();
			var result = new global::RunResult { Outcome = global::RunOutcome.Victory, BossId = gaoler.Id, StageId = dungeon.Id };
			AchievementManager.ApplyRunAchievements(save, result);
			StageDefinition opened = GlobalStatsManager.ChapterOpenedByVictory(save, gaoler);
			if (opened != null)
				GlobalStatsManager.UnlockStage(save, opened.Id);
			GlobalStatsManager.RecordFinishedCampaign(save);
			if (!save.UnlockedStageIds.SequenceEqual(before))
				warnings.Add($"Dungeon move ({label}): a win in the grandfathered Dungeon wrote [{string.Join(", ", save.UnlockedStageIds.Except(before))}] into the opened list.");
			if (!save.Lifetime.HasDefeatedBoss(gaoler.Id))
				warnings.Add($"Dungeon move ({label}): a win in the grandfathered Dungeon is not in the lifetime record.");
			if (!AchievementDefinitions.All.Any(d => d.Kind == AchievementKind.Road && d.BossId == gaoler.Id
				&& GlobalStatsManager.IsAchievementUnlocked(save, d.Id)))
				warnings.Add($"Dungeon move ({label}): a win in the grandfathered Dungeon did not pay {gaoler.DisplayName}'s deed.");
			if (GlobalStatsManager.IsStageEarned(save, dungeon) || (court != null && GlobalStatsManager.IsStageEarned(save, court)))
				warnings.Add($"Dungeon move ({label}): after a win in the grandfathered Dungeon, the castle road counts it.");
			if (GlobalStatsManager.HasFinishedCampaign(save))
				warnings.Add($"Dungeon move ({label}): a win in the grandfathered Dungeon finished the campaign.");
		}

		Sample("reached the Cave, not the Dungeon", 19,
			stored: new[] { "stage_9", "stage_10", "stage_2" }, beaten: road, wizards: new string[0],
			couldEnter: new[] { "stage_0", "stage_9", "stage_10", "stage_2" }, s =>
		{
			if (GlobalStatsManager.IsStageAvailable(s, dungeon) || GlobalStatsManager.IsStageAvailable(s, StageCatalog.GetById("stage_3")))
				warnings.Add("Dungeon move (reached the Cave): the Dungeon or the Swamp opened before the Choir fell.");
		});

		SaveData atDungeon = Sample("had the Dungeon open, not the Gaoler", 19,
			stored: new[] { "stage_9", "stage_10", "stage_2", "stage_1" }, beaten: road.Append("cave_choir").ToArray(),
			wizards: new string[0], couldEnter: new[] { "stage_0", "stage_9", "stage_10", "stage_2", "stage_1" }, s =>
		{
			if (!GlobalStatsManager.IsStageAvailable(s, StageCatalog.GetById("stage_3")))
				warnings.Add("Dungeon move (had the Dungeon): the Choir is beaten and the Swamp is still closed; the Choir opens it now.");
			if (GlobalStatsManager.IsStageAvailable(s, StageCatalog.GetById("stage_4")))
				warnings.Add("Dungeon move (had the Dungeon): the Mystic Ruins opened, though Mother Rot is unbeaten.");
			foreach (string id in new[] { "stage_9", "stage_10", "stage_2" })
			{
				if (!s.UnlockedStageIds.Contains(id))
					warnings.Add($"Dungeon move (had the Dungeon): {StageCatalog.DisplayNameFor(id)}, which it had by rule 2 from the Dungeon, was not written down.");
			}
		});
		WinGrandfathered("had the Dungeon open, then beat the Gaoler there", atDungeon);

		SaveData wentOn = Sample("beat the Gaoler and went on to the Waste", 19,
			stored: new[] { "stage_9", "stage_10", "stage_2", "stage_1", "stage_3", "stage_4", "stage_5" },
			beaten: road.Concat(new[] { "cave_choir", "castle_warden", "swamp_mother", "ruins_sentinel" }).ToArray(),
			wizards: new[] { "tituba" },
			couldEnter: new[] { "stage_0", "stage_9", "stage_10", "stage_2", "stage_1", "stage_3", "stage_4", "stage_5" }, s =>
		{
			if (GlobalStatsManager.IsStageAvailable(s, StageCatalog.GetById("stage_6")))
				warnings.Add("Dungeon move (went on to the Waste): the Scorched Sands opened, though the Still Warden is unbeaten.");
			if (BossRushCatalog.IsOpen(s))
				warnings.Add("Dungeon move (went on to the Waste): the Boss Rush opened.");
		});
		WinGrandfathered("beat the Gaoler again in the grandfathered Dungeon", wentOn);

		// The developer's own shape (schema 20): the Swamp and the Ruins open from the order before 13, the
		// Gaoler never beaten, Tituba freed.
		Sample("the Swamp and the Ruins from the oldest order, the Gaoler never beaten", 20,
			stored: new[] { "stage_1", "stage_3", "stage_4", "stage_10", "stage_9", "stage_2" },
			beaten: road.Concat(new[] { "cave_choir", "swamp_mother" }).ToArray(), wizards: new[] { "tituba", "john_dee" },
			couldEnter: new[] { "stage_0", "stage_9", "stage_10", "stage_2", "stage_1", "stage_3", "stage_4" }, s =>
		{
			if (GlobalStatsManager.IsStageAvailable(s, StageCatalog.GetById("stage_5")))
				warnings.Add("Dungeon move (the oldest order): the Frozen Waste opened, though the Archivist is unbeaten.");
		});

		string[] allTen = { "forest_treant", "fields_hayward", "heath_ninth_stone", "cave_choir", "castle_warden",
			"swamp_mother", "ruins_sentinel", "frost_warden", "sands_coil", "emberdeep_warden" };
		Sample("finished the old campaign", 19,
			stored: new[] { "stage_9", "stage_10", "stage_2", "stage_1", "stage_3", "stage_4", "stage_5", "stage_6", "stage_7" },
			beaten: allTen, wizards: new[] { "tituba", "vainamoinen", "john_dee" },
			couldEnter: new[] { "stage_0", "stage_9", "stage_10", "stage_2", "stage_1", "stage_3", "stage_4", "stage_5", "stage_6", "stage_7", "stage_8" }, s =>
		{
			if (!s.UnlockedStageIds.Contains("stage_8"))
				warnings.Add("Dungeon move (finished the old campaign): the Sketchbook was not written down, so it would close until the castle is finished.");
			if (!BossRushCatalog.IsOpen(s) || !s.BossRushGrandfathered)
				warnings.Add("Dungeon move (finished the old campaign): the Boss Rush is not kept open.");
			StageDefinition bridge = StageCatalog.GetById("stage_11");
			if (bridge != null && !GlobalStatsManager.IsStageEarned(s, bridge))
				warnings.Add("Dungeon move (finished the old campaign): the Last Bridge is not earned; the Warden of the Deep opens it.");
			// The Dungeon joins the road when the chapter before it in the castle has fallen: the Warden of the
			// Deep while the bridge is unbuilt (it is skipped), the Constable once it is - so here, exactly when
			// that boss is in the record.
			StageDefinition before = StageCatalog.PreviousChapter(dungeon);
			bool beforeBeaten = before != null && s.Lifetime.HasDefeatedBoss(BossCatalog.ForStageIndex(before.Index)?.Id ?? "");
			if (GlobalStatsManager.IsStageEarned(s, dungeon) != beforeBeaten)
				warnings.Add($"Dungeon move (finished the old campaign): the Dungeon is {(beforeBeaten ? "not " : "")}on the road with {before?.DisplayName}'s boss {(beforeBeaten ? "" : "un")}beaten.");
			if (court != null && GlobalStatsManager.IsStageEarned(s, court) != GlobalStatsManager.IsStageEarned(s, dungeon))
				warnings.Add("Dungeon move (finished the old campaign): the old Gaoler kill should open the Court exactly when the Dungeon is on the road.");
		});

		Sample("a new save", 19, stored: new string[0], beaten: new string[0], wizards: new string[0],
			couldEnter: new[] { "stage_0" }, s =>
		{
			if (s.UnlockedStageIds.Count + s.GrandfatheredStageIds.Count > 0 || s.BossRushGrandfathered)
				warnings.Add("Dungeon move (a new save): it was written a chapter or the Boss Rush.");
		});

		// The guard itself, where the order still has something to open: a chapter held only grandfathered (the
		// Swamp, for the test) opens nothing when its boss falls, though its boss opens the Ruins on the road.
		var held = new SaveData();
		StageDefinition swamp = StageCatalog.GetById("stage_3");
		BossDefinition rot = swamp != null ? BossCatalog.ForStageIndex(swamp.Index) : null;
		held.GrandfatheredStageIds.Add("stage_3");
		if (rot != null && BossCatalog.ChapterOpenedBy(rot) != null)
		{
			if (!GlobalStatsManager.IsStageAvailable(held, swamp))
				warnings.Add("Dungeon move: a grandfathered chapter cannot be entered.");
			held.Lifetime.BossesDefeated.Add(rot.Id);
			if (GlobalStatsManager.ChapterOpenedByVictory(held, rot) != null)
				warnings.Add($"Dungeon move: a win in a chapter held only grandfathered opens {BossCatalog.ChapterOpenedBy(rot).DisplayName}.");
		}

		// The gates' lock lines say who is still held (.ai/endgame-castle.md 1.6) - by epithet, never by name,
		// which is a locked wizard's reward (QuestLog). On a save that has freed nobody, every one is held.
		var nobodyFree = new SaveData();
		foreach (StageDefinition sealedGate in StageCatalog.GateChapters.Where(g => g.IsPlayable))
		{
			string hint = StageCatalog.LockedHintFor(sealedGate, nobodyFree);
			foreach (string id in GlobalStatsManager.GateWizardIds(sealedGate).Where(id => !UnlockCatalog.IsStarter(id, UnlockKind.Character)))
			{
				if (hint.Contains(QuestLog.NameOf(id), StringComparison.OrdinalIgnoreCase))
					warnings.Add($"Dungeon move: {sealedGate.DisplayName}'s lock line names '{id}', who is still held: \"{hint}\".");
			}
			if (GlobalStatsManager.HeldGateWizards(nobodyFree, sealedGate).Count != GlobalStatsManager.RemainingGateWizardCount(nobodyFree, sealedGate))
				warnings.Add($"Dungeon move: {sealedGate.DisplayName}'s lock line does not account for every wizard it waits on.");
		}
	}

	// GATE REACHABILITY (.ai/achievements-and-secrets.md 2.6; .ai/endgame-castle.md 1.6). The rule the
	// old final gate broke: a gate must never count something that can only be earned behind it. It
	// counted every spell, and Fireball was paid only by the boss behind it, so no save written since
	// schema 8 could open the Emberdeep - and nothing said so, because the old check asked only whether
	// a save that already owned everything got in. This asks it of every gate chapter, two ways:
	//   - on paper: everything the gate counts has a source before it - its previous boss is earlier
	//     in the order, and every wizard it counts is a starter, a Feat's that names no chapter at or
	//     past it, or a chapter's deed earlier in the campaign; never a secret's, which is missable;
	//   - in memory: a brand-new save walks the road before the gate - every boss beaten as a victory
	//     records it, every side event won, every built secret found, every Feat done - and must then
	//     be able to enter it. And the gate must want both halves: without the boss, or without any one
	//     wizard it counts, it stays shut.
	// It is also what keeps Tituba out of the Emberdeep's count once the Dungeon moves behind it, and in
	// the Throne's; and the last gate must count every wizard, which is what makes "he is reachable only
	// when every wizard is free" true where he is.
	private static void ValidateGateReachability(List<string> warnings)
	{
		List<StageDefinition> gates = StageCatalog.GateChapters.ToList();
		if (gates.Count == 0)
		{
			warnings.Add("Gate reachability: the campaign has no gate chapter (StageGate.PreviousBossAndWizards).");
			return;
		}

		StageDefinition lastGate = gates[gates.Count - 1];
		if (StageCatalog.FinalGateChapter?.Id != lastGate.Id)
			warnings.Add($"Gate reachability: StageCatalog.FinalGateChapter is not {lastGate.DisplayName}, the last gate in campaign order.");
		foreach (string wizardId in UnlockCatalog.AllWizardIds)
		{
			if (!GlobalStatsManager.CountsTowardGate(wizardId, lastGate))
				warnings.Add($"Gate reachability: the last gate, {lastGate.DisplayName}, does not count '{wizardId}', so the campaign can end with him still held.");
		}

		// THE CHAPTER WIZARDS (.ai/chapter-wizards.md, G2): counted at the Throne and nowhere before it, so a
		// wizard held in every chapter of the road never lengthens the Emberdeep's list. The flag is only
		// meaningful on a wizard someone frees in a chapter: on a boon or spell nothing reads it, on a starter
		// there is nothing to count, and outside the campaign gate he is counted by no gate at all.
		foreach (UnlockDefinition flagged in UnlockCatalog.All.Where(d => d.CountedAtFinalGateOnly))
		{
			if (flagged.Kind != UnlockKind.Character || flagged.Source == UnlockSource.Starter || flagged.OutsideCampaignGate)
			{
				warnings.Add($"Gate reachability: '{flagged.Id}' is counted at the final gate only, which means nothing on a {flagged.Source} {flagged.Kind}{(flagged.OutsideCampaignGate ? " outside the campaign gate" : "")}.");
				continue;
			}
			if (string.IsNullOrWhiteSpace(GlobalStatsManager.RescueChapterOf(flagged)))
				warnings.Add($"Gate reachability: chapter wizard '{flagged.Id}' is freed by no chapter's deed, so there is no road to count him on.");
			foreach (StageDefinition gate in gates.Where(g => g.Id != lastGate.Id))
			{
				if (GlobalStatsManager.CountsTowardGate(flagged.Id, gate))
					warnings.Add($"Gate reachability: {gate.DisplayName} counts chapter wizard '{flagged.Id}', who is counted at {lastGate.DisplayName} only.");
			}
		}

		foreach (StageDefinition gate in gates)
		{
			int gatePosition = StageCatalog.CampaignPositionOf(gate.Id);
			bool BeforeGate(string stageId)
			{
				if (string.IsNullOrWhiteSpace(stageId))
					return true;
				int position = StageCatalog.CampaignPositionOf(stageId);
				return position < 0 ? StageCatalog.IsOpenFromStart(StageCatalog.GetById(stageId)) : position < gatePosition;
			}

			// On paper.
			StageDefinition previous = StageCatalog.PreviousChapter(gate);
			BossDefinition gateBoss = previous != null ? BossCatalog.ForStageIndex(previous.Index) : null;
			if (gateBoss == null || StageCatalog.CampaignPositionOf(previous.Id) >= gatePosition)
				warnings.Add($"Gate reachability: {gate.DisplayName}'s gate names no boss before it, so half of it can never be met.");
			List<string> counted = GlobalStatsManager.GateWizardIds(gate).ToList();
			foreach (string wizardId in counted)
			{
				UnlockDefinition wizard = UnlockCatalog.GetCharacter(wizardId);
				if (wizard == null || wizard.Source == UnlockSource.Starter)
					continue;
				AchievementDefinition deed = UnlockCatalog.SourceDeedOf(wizard);
				if (deed == null)
					warnings.Add($"Gate reachability: {gate.DisplayName} counts '{wizardId}', whose source '{wizard.SourceId}' is no achievement, so nothing can free him.");
				else if (deed.Kind == AchievementKind.Secret)
					warnings.Add($"Gate reachability: {gate.DisplayName} counts '{wizardId}', whom a secret frees ('{deed.Id}'); a secret is missable, and no gate may wait on one.");
				else if (!BeforeGate(deed.StageId))
					warnings.Add($"Gate reachability: {gate.DisplayName} counts '{wizardId}', freed by '{deed.Id}' in {StageCatalog.DisplayNameFor(deed.StageId)}, which is not before the gate.");
			}

			// In memory: a new save walks the road.
			var save = new SaveData();
			for (int pass = 0; pass <= StageCatalog.Campaign.Count + 1; pass++)
			{
				bool changed = false;
				foreach (StageDefinition stage in StageCatalog.Campaign)
				{
					if (!BeforeGate(stage.Id) || !GlobalStatsManager.IsStageAvailable(save, stage))
						continue;
					BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
					if (boss != null && !save.Lifetime.HasDefeatedBoss(boss.Id))
					{
						WinOnPaper(save, boss, stage);
						changed = true;
					}
					foreach (SideEventEntry site in global::SideEventDirector.EventsFor(stage.Id))
					{
						if (save.Lifetime.HasWonEvent(site.SiteId))
							continue;
						DiscoveryRewards.GrantSite(save, site.SiteId);
						changed = true;
					}
					foreach (SecretDefinition secret in SecretCatalog.ForStage(stage.Id))
					{
						if (save.HasFoundSecret(secret.Id))
							continue;
						AchievementManager.Complete(save, AchievementDefinitions.SecretDeedId(secret.Id));
						changed = true;
					}
				}
				foreach (AchievementDefinition feat in AchievementDefinitions.All.Where(d =>
					(d.Kind == AchievementKind.Feat || d.Kind == AchievementKind.Collection) && BeforeGate(d.StageId)))
				{
					if (GlobalStatsManager.IsAchievementUnlocked(save, feat.Id))
						continue;
					AchievementManager.Complete(save, feat.Id);
					changed = true;
				}
				if (!changed)
					break;
			}
			if (!GlobalStatsManager.IsStageEarned(save, gate))
			{
				var held = counted.Where(id => !GlobalStatsManager.IsCharacterUnlocked(save, id)).ToList();
				warnings.Add($"Gate reachability: a new save that does everything before {gate.DisplayName} still cannot enter it"
					+ $" - boss {(gateBoss != null && save.Lifetime.HasDefeatedBoss(gateBoss.Id) ? "beaten" : "not beaten")}"
					+ (held.Count > 0 ? $", still held: {string.Join(", ", held)}" : "") + ".");
			}

			// Both halves.
			var noBoss = new SaveData();
			foreach (string id in counted)
				GlobalStatsManager.UnlockCharacter(noBoss, id);
			if (GlobalStatsManager.IsStageEarned(noBoss, gate))
				warnings.Add($"Gate reachability: {gate.DisplayName} opens with every wizard free but {gateBoss?.DisplayName ?? "its boss"} unbeaten.");
			foreach (string missing in counted.Where(id => !UnlockCatalog.IsStarter(id, UnlockKind.Character)))
			{
				var oneShort = new SaveData();
				if (gateBoss != null)
					oneShort.Lifetime.BossesDefeated.Add(gateBoss.Id);
				foreach (string id in counted.Where(id => !id.Equals(missing, StringComparison.OrdinalIgnoreCase)))
					GlobalStatsManager.UnlockCharacter(oneShort, id);
				if (GlobalStatsManager.IsStageEarned(oneShort, gate))
					warnings.Add($"Gate reachability: {gate.DisplayName} opens with '{missing}' still held.");
			}
		}
	}

	// THE WHOLE ROAD, TWICE (.ai/dark-wizard-playable.md, .ai/merlin.md). Eric, 2026-11-02: the Dark Wizard "should
	// unlock after defeating all stages on hard mode", and Merlin (asked for that day as the blue-robed wizard, who
	// is the player's figure and keeps it; Merlin has a look of his own) "when finishing the full campaign on
	// medium". Both read each chapter's hardest clear (SaveData.MaxDifficultyCleared, written by
	// AchievementManager.ApplyRunAchievements) over every chapter of the campaign, built or not. Held on paper,
	// with runs ended on sample saves - a boss victory in every chapter: every one on Default frees Merlin and not
	// the Dark Wizard; every one on Hardcore frees both; one chapter short frees neither; one chapter on Default and
	// the rest on Hardcore frees Merlin alone. A defeat, and a Boss Rush (no boss), record nothing, and a Casual
	// clear is never lifted to Default by a later one. Then what keeps each outside everything else: no gate counts
	// either, the Dark Wizard stays sealed and on the Throne's card, Merlin is never secret, and Orb of Ages is his
	// alone - his opening spell, paid with him, offered to no other wizard and not counted by The Grimoire Restored.
	private static void ValidateTakerTaken(List<string> warnings)
	{
		const string dark = "dark_wizard";
		const string merlin = "merlin";
		IReadOnlyList<StageDefinition> road = StageCatalog.Campaign;
		foreach (StageDefinition stage in road)
		{
			if (BossCatalog.ForStageIndex(stage.Index) == null)
				warnings.Add($"The whole road: {stage.DisplayName} has no boss, so no clear of it can ever be recorded and neither the Dark Wizard nor Merlin can be earned.");
		}

		// Every chapter of the road won once, on the preset each is given (null: not won).
		SaveData Walk(Func<int, string> presetAt)
		{
			var save = new SaveData();
			for (int i = 0; i < road.Count; i++)
			{
				string preset = presetAt(i);
				BossDefinition boss = BossCatalog.ForStageIndex(road[i].Index);
				if (preset == null || boss == null)
					continue;
				save.BalancePresetId = preset;
				AchievementManager.ApplyRunAchievements(save, new global::RunResult
				{
					Outcome = global::RunOutcome.Victory,
					BalancePresetId = preset,
					BossId = boss.Id,
					StageId = road[i].Id,
				});
			}
			return save;
		}
		void Expect(string label, SaveData save, bool merlinFree, bool darkFree)
		{
			if (GlobalStatsManager.IsCharacterUnlocked(save, merlin) != merlinFree)
				warnings.Add($"The whole road: {label} {(merlinFree ? "does not free" : "frees")} Merlin.");
			if (GlobalStatsManager.IsCharacterUnlocked(save, dark) != darkFree)
				warnings.Add($"The whole road: {label} {(darkFree ? "does not free" : "frees")} the Dark Wizard.");
		}
		string D = GlobalStatsManager.BalancePresetDefault, H = GlobalStatsManager.BalancePresetHardcore, C = GlobalStatsManager.BalancePresetCasual;
		Expect("every chapter on Default", Walk(_ => D), merlinFree: true, darkFree: false);
		Expect("every chapter on Hardcore", Walk(_ => H), merlinFree: true, darkFree: true);
		Expect("every chapter on Casual", Walk(_ => C), merlinFree: false, darkFree: false);
		Expect("every chapter on Hardcore but the last, never won", Walk(i => i == road.Count - 1 ? null : H), merlinFree: false, darkFree: false);
		Expect("every chapter on Hardcore but the first, never won", Walk(i => i == 0 ? null : H), merlinFree: false, darkFree: false);
		Expect("one chapter on Default and the rest on Hardcore", Walk(i => i == road.Count / 2 ? D : H), merlinFree: true, darkFree: false);
		// NO SKETCHBOOK (Eric, 2026-11-02: it has no unlocks and no achievements). Only the campaign's chapters are
		// counted: a chapter outside it is never asked for, and a win there counts toward neither deed.
		foreach (StageDefinition outside in StageCatalog.All.Where(s => !StageCatalog.IsInCampaign(s)))
		{
			BossDefinition outsideBoss = BossCatalog.ForStageIndex(outside.Index);
			if (outsideBoss == null)
				continue;
			var only = new SaveData { BalancePresetId = H };
			AchievementManager.ApplyRunAchievements(only, new global::RunResult
			{
				Outcome = global::RunOutcome.Victory, BalancePresetId = H, BossId = outsideBoss.Id, StageId = outside.Id,
			});
			if (GlobalStatsManager.ChaptersClearedAt(only, GlobalStatsManager.PresetRankCasual) > 0)
				warnings.Add($"The whole road: a win in {outside.DisplayName}, outside the campaign, counts toward Merlin and the Dark Wizard.");
		}
		if (StageCatalog.Campaign.Any(s => s.Id.Equals("stage_8", StringComparison.OrdinalIgnoreCase)))
			warnings.Add("The whole road: the Sketchbook is in the campaign, so Merlin and the Dark Wizard would ask for it; it has no unlocks.");
		SaveData orbOwned = Walk(_ => D);
		if (!GlobalStatsManager.IsSpellUnlockedForLevelUp(orbOwned, OrbOfAges.SpellId))
			warnings.Add("The whole road: every chapter on Default frees Merlin but not his Orb of Ages.");

		// Only a boss victory records a clear, and a record never comes down.
		var record = new SaveData();
		StageDefinition first = road.Count > 0 ? road[0] : null;
		BossDefinition firstBoss = first != null ? BossCatalog.ForStageIndex(first.Index) : null;
		if (first != null && firstBoss != null)
		{
			void End(global::RunOutcome outcome, string bossId, string preset) => AchievementManager.ApplyRunAchievements(record,
				new global::RunResult { Outcome = outcome, BossId = bossId, BalancePresetId = preset, StageId = first.Id });
			End(global::RunOutcome.Defeat, string.Empty, H);
			End(global::RunOutcome.Victory, string.Empty, H);
			if (GlobalStatsManager.HardestClearOf(record, first.Id) >= 0)
				warnings.Add($"The whole road: a defeat or a Boss Rush (no boss) recorded a clear of {first.DisplayName}.");
			End(global::RunOutcome.Victory, firstBoss.Id, C);
			End(global::RunOutcome.Victory, firstBoss.Id, H);
			End(global::RunOutcome.Victory, firstBoss.Id, D);
			if (GlobalStatsManager.HardestClearOf(record, first.Id) != GlobalStatsManager.PresetRankHardcore)
				warnings.Add($"The whole road: {first.DisplayName} won on Casual, Hardcore then Default keeps {GlobalStatsManager.HardestClearOf(record, first.Id)}, not Hardcore.");
		}

		// Outside every gate, both: neither was ever one of the imprisoned.
		foreach (string who in new[] { dark, merlin })
		{
			if (UnlockCatalog.GetCharacter(who) == null)
			{
				warnings.Add($"The whole road: '{who}' has no UnlockCatalog entry.");
				continue;
			}
			if (UnlockCatalog.AllWizardIds.Contains(who, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"The whole road: '{who}' is counted as an imprisoned wizard; he must be outside the campaign gate (OutsideCampaignGate).");
			foreach (StageDefinition gate in StageCatalog.GateChapters)
			{
				if (GlobalStatsManager.CountsTowardGate(who, gate))
					warnings.Add($"The whole road: {gate.DisplayName}'s gate counts '{who}', who is earned only past it.");
			}
		}

		// The Dark Wizard: sealed until taken, on the Throne's card, his spell the Conjunction he opens on.
		BossDefinition throneBoss = BossCatalog.GetById(AchievementDefinitions.ThroneBossId);
		StageDefinition throne = throneBoss != null ? StageCatalog.GetByIndex(BossCatalog.StageIndexOf(throneBoss)) : null;
		if (throne == null)
		{
			warnings.Add($"The Taker Taken: the boss '{AchievementDefinitions.ThroneBossId}' has no chapter, so the dark wizard's deed has nowhere to be listed.");
		}
		else
		{
			if (!ComboCatalog.StartsFused(ComboCatalog.ForId(TheTaking.SpellId), dark))
				warnings.Add("The Taker Taken: The Taking is not the Conjunction the dark wizard opens on (ComboDefinition.StartsFusedFor).");
			if (UnlockCatalog.GrimoireSpellIds.Contains(TheTaking.SpellId, StringComparer.OrdinalIgnoreCase))
				warnings.Add("The Taker Taken: The Grimoire Restored counts The Taking, which would put the collection behind a Hardcore win.");
			if (UnlockCatalog.GetCharacter(dark)?.SealedUntilFreed != true)
				warnings.Add("The Taker Taken: the dark wizard is not SealedUntilFreed, so his shape shows before he is taken.");
			if (!StageRewards.ForStage(throne.Id).Any(r => r.Id.Equals(dark, StringComparison.OrdinalIgnoreCase)))
				warnings.Add("The Taker Taken: the Throne's card does not list the dark wizard.");
			var locked = new SaveData();
			AchievementDefinition deed = AchievementDefinitions.GetById(AchievementDefinitions.TakerTakenId);
			if (deed == null || !CodexRows.HidesWizard(deed, locked))
				warnings.Add("The Taker Taken: his deed's row names him while he is locked.");
			if (!StageRewards.ForStage(throne.Id).Where(r => r.Id.Equals(dark, StringComparison.OrdinalIgnoreCase)).All(r => StageRewards.IsSealed(locked, r)))
				warnings.Add("The Taker Taken: the Throne's card shows the dark wizard's shape before he is taken.");
		}

		// Merlin: never secret, never sealed, and Orb of Ages his alone.
		UnlockDefinition merlinUnlock = UnlockCatalog.GetCharacter(merlin);
		if (merlinUnlock != null && (merlinUnlock.IsSecret || merlinUnlock.SealedUntilFreed))
			warnings.Add("The King-Maker: Merlin is secret or sealed; he was never held, and is shown locked from the first run.");
		CharacterData merlinData = CharacterRoster.GetAll().FirstOrDefault(c => c.Id.Equals(merlin, StringComparison.OrdinalIgnoreCase));
		if (merlinData?.StartingSpellResource?.Id?.Equals(OrbOfAges.SpellId, StringComparison.OrdinalIgnoreCase) != true)
			warnings.Add("The King-Maker: Merlin does not open on Orb of Ages.");
		if (AchievementDefinitions.GetById(AchievementDefinitions.OneHeMissedId)?.SpellUnlockId != OrbOfAges.SpellId)
			warnings.Add("The King-Maker: the deed does not pay Orb of Ages with him.");
		if (UnlockCatalog.GrimoireSpellIds.Contains(OrbOfAges.SpellId, StringComparer.OrdinalIgnoreCase))
			warnings.Add("The King-Maker: The Grimoire Restored counts Orb of Ages, which would put the collection behind the whole road.");
		foreach (CharacterData who in CharacterRoster.GetAll())
		{
			bool his = who.Id.Equals(merlin, StringComparison.OrdinalIgnoreCase);
			if (UnlockCatalog.MayBeOffered(OrbOfAges.SpellId, who.Id) != his)
				warnings.Add($"Orb of Ages: '{who.Id}' {(his ? "is never" : "may be")} offered it; it is Merlin's alone (UnlockDefinition.ExclusiveTo).");
		}
		// Any spell that is someone's alone names a wizard who exists and opens on it - or nobody could ever be dealt it.
		foreach (UnlockDefinition spell in UnlockCatalog.All.Where(d => d.Kind == UnlockKind.Spell && !string.IsNullOrEmpty(d.ExclusiveTo)))
		{
			CharacterData owner = CharacterRoster.GetAll().FirstOrDefault(c => c.Id.Equals(spell.ExclusiveTo, StringComparison.OrdinalIgnoreCase));
			if (owner?.StartingSpellResource?.Id?.Equals(spell.Id, StringComparison.OrdinalIgnoreCase) != true)
				warnings.Add($"'{spell.Id}' is exclusive to '{spell.ExclusiveTo}', who does not open on it, so no one is ever dealt it.");
		}
	}

	// A boss victory as the end of a run records it: the run's achievements (ApplyRunAchievements folds
	// the run into the lifetime record and pays the deeds), and the chapter the order says it opens.
	private static void WinOnPaper(SaveData save, BossDefinition boss, StageDefinition stage)
	{
		AchievementManager.ApplyRunAchievements(save, new global::RunResult
		{
			Outcome = global::RunOutcome.Victory,
			BossId = boss.Id,
			StageId = stage.Id,
		});
		StageDefinition opened = BossCatalog.ChapterOpenedBy(boss);
		if (opened != null)
			GlobalStatsManager.UnlockStage(save, opened.Id);
	}

	// SCHEMA 19 ON SAMPLE SAVES (SaveData.MigrateToAchievementUnlocks, .ai/achievements-and-secrets.md
	// section 3). Each is a save as schema 18 wrote it, migrated in memory. The rules being held: nothing
	// owned is taken away; a done deed is paid its current reward and never Arcane Energy; a won event
	// shows its deed done; a save that had met the old gate still enters the Emberdeep; a new save is
	// granted nothing; and a second pass changes nothing. Then two older saves, run through every step,
	// because schema 19 froze what the old steps read - a pre-campaign save must still not be handed
	// the rescued wizards, and a save that had everything at 9 must still be credited its discoveries.
	private static void ValidateAchievementMigration(List<string> warnings)
	{
		bool OwnsSpell(SaveData s, string id) => GlobalStatsManager.IsSpellUnlockedForLevelUp(s, id);

		SaveData Sample(string label, Action<SaveData> shape, Action<SaveData> check)
		{
			var save = new SaveData { SchemaVersion = 18, TotalCurrency = 100 };
			shape(save);
			var ownedBefore = save.UnlockedSpellIds.Concat(save.UnlockedBoonIds).Concat(save.UnlockedCharacterIds)
				.Concat(save.UnlockedStageIds).Concat(save.UnlockedAchievementIds).ToList();
			if (!save.Migrate(log: false))
				warnings.Add($"Achievement migration ({label}): a schema-18 save was not migrated.");
			var ownedAfter = save.UnlockedSpellIds.Concat(save.UnlockedBoonIds).Concat(save.UnlockedCharacterIds)
				.Concat(save.UnlockedStageIds).Concat(save.UnlockedAchievementIds).ToList();
			foreach (string id in ownedBefore.Where(id => !ownedAfter.Contains(id, StringComparer.OrdinalIgnoreCase)))
				warnings.Add($"Achievement migration ({label}): the save lost '{id}'.");
			if (save.TotalCurrency != 100)
				warnings.Add($"Achievement migration ({label}): the purse changed from 100 to {save.TotalCurrency}; schema 19 pays unlocks, never Arcane Energy.");
			check(save);

			// Twice: the step again, on the migrated save, changes nothing.
			string once = Json.Stringify(save.ToGodotDictionary());
			save.MigrateToAchievementUnlocks();
			AchievementManager.Reconcile(save);
			if (save.Migrate(log: false) || Json.Stringify(save.ToGodotDictionary()) != once)
				warnings.Add($"Achievement migration ({label}): migrating twice changed the save again.");
			return save;
		}

		Sample("bought Gravewell", s => s.UnlockedSpellIds.Add("gravewell"), s =>
		{
			if (!OwnsSpell(s, "gravewell"))
				warnings.Add("Achievement migration (bought Gravewell): the bought spell is gone.");
			if (GlobalStatsManager.IsAchievementUnlocked(s, AchievementDefinitions.SecretDeedId("swallow_hole")) || s.HasFoundSecret("swallow_hole"))
				warnings.Add("Achievement migration (bought Gravewell): its vault reads as found; buying the spell found nothing.");
		});

		Sample("Survivor done", s =>
		{
			s.UnlockedAchievementIds.Add("survivor");
			s.UnlockedSpellIds.Add("frost_shard");
			s.Lifetime.BestTimeByStage["stage_0"] = 640f;
		}, s =>
		{
			if (!OwnsSpell(s, "frost_shard") || !OwnsSpell(s, "hollow_star"))
				warnings.Add($"Achievement migration (Survivor done): should own Frost Shard and Hollow Star; owns {string.Join(", ", s.UnlockedSpellIds)}.");
		});

		Sample("the Long Coil and the Warden beaten", s =>
		{
			s.UnlockedAchievementIds.AddRange(new[] { "sands_unwound", "emberdeep_ended" });
			s.UnlockedSpellIds.Add("fireball");
			s.UnlockedBoonIds.Add("sunsteel_filament");
			s.Lifetime.BossesDefeated.AddRange(new[] { "sands_coil", "emberdeep_warden" });
		}, s =>
		{
			if (!OwnsSpell(s, "fireball") || !GlobalStatsManager.IsBoonUnlocked(s, "sunsteel_filament"))
				warnings.Add("Achievement migration (the Long Coil and the Warden beaten): should own Fireball and Sunsteel Filament.");
		});

		Sample("the Long Coil beaten, not the Warden", s =>
		{
			s.UnlockedAchievementIds.Add("sands_unwound");
			s.UnlockedBoonIds.Add("sunsteel_filament");
			s.Lifetime.BossesDefeated.Add("sands_coil");
		}, s =>
		{
			if (!OwnsSpell(s, "fireball"))
				warnings.Add("Achievement migration (the Long Coil beaten): Unwound done should pay Fireball, which moved to it.");
		});

		Sample("met the old gate without the Long Coil", s =>
		{
			foreach (string id in UnlockCatalog.AllSpellIds.Where(id => !UnlockCatalog.IsStarter(id, UnlockKind.Spell)))
				s.UnlockedSpellIds.Add(id);
			foreach (string id in UnlockCatalog.AllWizardIds.Where(id => !UnlockCatalog.IsStarter(id, UnlockKind.Character)))
				s.UnlockedCharacterIds.Add(id);
		}, s =>
		{
			StageDefinition final = StageCatalog.GetById("stage_7");
			if (final != null && final.IsPlayable && !GlobalStatsManager.IsStageAvailable(s, final))
				warnings.Add("Achievement migration (met the old gate without the Long Coil): the save could enter the Emberdeep at 18 and cannot now.");
		});

		Sample("owns Bramble Seed", s => s.UnlockedSpellIds.Add("bramble_seed"), s =>
		{
			if (!s.Lifetime.HasWonEvent("the_grove") || !GlobalStatsManager.IsAchievementUnlocked(s, AchievementDefinitions.EventDeedId("the_grove")))
				warnings.Add("Achievement migration (owns Bramble Seed): the grove's deed should read as done.");
			if (s.Lifetime.EventsWon.Count != 1)
				warnings.Add($"Achievement migration (owns Bramble Seed): events won {string.Join(", ", s.Lifetime.EventsWon)}; only the grove was.");
		});

		Sample("a fresh save", _ => { }, s =>
		{
			if (s.UnlockedSpellIds.Count + s.UnlockedBoonIds.Count + s.UnlockedCharacterIds.Count + s.UnlockedStageIds.Count > 0
				|| s.UnlockedAchievementIds.Count > 0 || s.Lifetime.EventsWon.Count > 0 || s.GlimpsedSecretIds.Count > 0)
				warnings.Add("Achievement migration (a fresh save): it was granted or credited with something.");
		});

		// Through every step from before the campaign: the legacy grant, and no rescued wizard.
		var legacy = new SaveData { SchemaVersion = 7 };
		legacy.Migrate(log: false);
		if (GlobalStatsManager.IsCharacterUnlocked(legacy, "tituba") || GlobalStatsManager.IsCharacterUnlocked(legacy, "vainamoinen"))
			warnings.Add("Achievement migration: a pre-campaign save was handed a rescued wizard; schema 8 must read its frozen list, not the live catalog.");
		if (!OwnsSpell(legacy, "fireball") || !OwnsSpell(legacy, "spiritual_weapon"))
			warnings.Add("Achievement migration: a pre-campaign save lost the legacy grant of schema 8.");

		// From 9, with everything that existed then: credited the discoveries (10) and John Dee (12),
		// and so the schema 18 gate, and so the Emberdeep (19).
		var nine = new SaveData { SchemaVersion = 9 };
		string[] laterSpells = { "starfall", "familiar", "stormsweep", "sleet_runes", "iron_palisade", "contagion", "bramble_seed" };
		foreach (string id in UnlockCatalog.AllSpellIds.Where(id => !UnlockCatalog.IsStarter(id, UnlockKind.Spell)
			&& !laterSpells.Contains(id, StringComparer.OrdinalIgnoreCase)))
			nine.UnlockedSpellIds.Add(id);
		nine.Migrate(log: false);
		foreach (string id in new[] { "tituba", "vainamoinen", "john_dee" })
		{
			if (!GlobalStatsManager.IsCharacterUnlocked(nine, id))
				warnings.Add($"Achievement migration: a save that had everything at schema 9 was not credited '{id}' on the way up.");
		}
		StageDefinition emberdeep = StageCatalog.GetById("stage_7");
		if (emberdeep != null && emberdeep.IsPlayable && !GlobalStatsManager.IsStageAvailable(nine, emberdeep))
			warnings.Add("Achievement migration: a save that had everything at schema 9 cannot enter the Emberdeep after every step.");
	}

	// The recurring miniboss. Node2DGame loads it by path on a timer and returns quietly when the
	// load fails, so every way this breaks produces the same symptom: the miniboss never arrives,
	// which reads as a tuning decision rather than as a broken reference. Checked here for the
	// same reason the ranged enemies are - a dotnet build sees none of it.
	private const string MiniBossScenePath = "res://scenes/WardenEnemy.tscn";

	private static void ValidateMiniBoss(List<string> warnings)
	{
		if (!ResourceLoader.Exists(MiniBossScenePath))
		{
			warnings.Add($"Miniboss scene '{MiniBossScenePath}' is missing, so the recurring miniboss would never spawn.");
			return;
		}

		var scene = GD.Load<PackedScene>(MiniBossScenePath);
		if (scene == null)
		{
			warnings.Add($"Miniboss scene '{MiniBossScenePath}' failed to load, so the recurring miniboss would never spawn.");
			return;
		}

		Node probe = scene.Instantiate();
		if (probe is not Enemy miniBoss)
		{
			warnings.Add($"Miniboss scene '{MiniBossScenePath}' has no Enemy script attached.");
			probe?.Free();
			return;
		}

		// IsMiniBoss is what the spawn tick counts to keep one on screen at a time, and it is also
		// what the reward and telemetry paths read. Off, this is an ordinary enemy with 220 HP.
		if (!miniBoss.IsMiniBoss)
			warnings.Add($"Miniboss scene '{MiniBossScenePath}' does not have IsMiniBoss set, so it would spawn as an ordinary enemy and could stack.");

		var sprite = miniBoss.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite?.SpriteFrames == null)
			warnings.Add($"Miniboss scene '{MiniBossScenePath}' has no SpriteFrames, so it would arrive invisible.");
		else if (!sprite.SpriteFrames.HasAnimation("moving"))
			warnings.Add($"Miniboss scene '{MiniBossScenePath}' has no 'moving' animation, so it would stand still while it chases.");

		miniBoss.Free();
	}

	// Scenes carrying a RangedEnemy script. There is no catalog for ordinary enemies - Node2DGame
	// holds them as fields - so this list is maintained by hand the same way SpellResourcePaths is.
	private static readonly string[] RangedEnemyScenePaths =
	{
		"res://scenes/HexerEnemy.tscn",
		"res://scenes/ForestWisp.tscn",
		"res://scenes/SkullSentry.tscn",
	};

	// A ranged enemy is the first one whose behaviour depends on a second scene resolving at
	// runtime. Every way this breaks is silent: a missing bolt scene, or a wind-up of zero, both
	// produce an enemy that still walks and still looks fine, so nobody finds out until a player
	// wonders why the caster never does anything - or takes a hit with no tell at all. A dotnet
	// build sees none of it.
	private static void ValidateRangedEnemies(List<string> warnings)
	{
		foreach (string path in RangedEnemyScenePaths)
		{
			if (!ResourceLoader.Exists(path))
			{
				warnings.Add($"Ranged enemy scene '{path}' is missing, so it would never spawn.");
				continue;
			}

			var scene = GD.Load<PackedScene>(path);
			if (scene == null)
			{
				warnings.Add($"Ranged enemy scene '{path}' failed to load.");
				continue;
			}

			Node probe = scene.Instantiate();
			if (probe is not RangedEnemy ranged)
			{
				warnings.Add($"Ranged enemy scene '{path}' has no RangedEnemy script attached, so it would chase and melee like every other enemy.");
				probe?.Free();
				continue;
			}

			ValidateRangedEnemy(warnings, path, ranged);
			ranged.Free();
		}
	}

	// One ranged enemy's tuning. Split out so ValidateBestiary runs the same checks on every caster and
	// turret a chapter's roster names, without each new scene having to be added to the list above.
	private static void ValidateRangedEnemy(List<string> warnings, string path, RangedEnemy ranged)
	{
		// The three ranges only mean anything as an ordered set. Out of order, the enemy either
		// backs away from a spot it is also trying to reach, or holds station outside the range
		// it can shoot from - both read as an enemy that has lost interest in the fight.
		if (ranged.RetreatDistance >= ranged.StandoffDistance || ranged.StandoffDistance >= ranged.AttackRange)
		{
			warnings.Add($"Ranged enemy '{path}' has ranges out of order (retreat {ranged.RetreatDistance}, standoff {ranged.StandoffDistance}, attack {ranged.AttackRange}); they must increase in that order.");
		}

		if (ranged.CastWindUpSeconds <= 0f)
			warnings.Add($"Ranged enemy '{path}' has no cast wind-up, so its shot arrives with no tell the player can read.");

		if (ranged.BoltsPerVolley < 1)
			warnings.Add($"Ranged enemy '{path}' fires {ranged.BoltsPerVolley} bolts per volley, so it would telegraph and shoot nothing.");

		// Stacked bolts are the one way a volley can be dishonest: several bolts on the exact
		// same heading look like one shot, land as one hit, and quietly multiply its damage.
		if (ranged.BoltsPerVolley > 1 && ranged.VolleySpreadDegrees <= 0f)
			warnings.Add($"Ranged enemy '{path}' fires {ranged.BoltsPerVolley} bolts with no spread, so they overlap into what looks like a single shot dealing several times its listed damage.");

		ValidateEnemyProjectile(warnings, path, ranged.ProjectileScenePath);
	}

	// The four attack-pattern enemies (issue #33). Every one of them promises the player something
	// before it hits: a streak along the line it will charge, a ring where the ground is about to
	// be hit, a ring where minions are about to stand. A wind-up of zero silently turns any of
	// those promises into damage that simply arrives, and the enemy still walks around looking
	// perfectly fine - so nothing but a check like this one catches it. A dotnet build sees none
	// of it: the scenes and their exported tuning only exist in Godot.
	private static readonly string[] AttackPatternEnemyScenePaths =
	{
		"res://scenes/LungerEnemy.tscn",
		"res://scenes/SlammerEnemy.tscn",
		"res://scenes/ExploderEnemy.tscn",
		"res://scenes/SummonerEnemy.tscn",
		// The Still Warden guard is a SlammerEnemy with the telegraph shape flipped to a cone,
		// so it validates through exactly the same path and needs no case of its own.
		"res://scenes/RimeGuard.tscn",
		// The Sketchbook's ghost (Elim's Boo): a wail ring and a phase that marks where it re-forms.
		"res://scenes/BooEnemy.tscn",
		// The Blighted Swamp event's lantern-tender: strikes, spilt oil, the dark, the rekindle.
		BogLanternsEvent.TenderScenePath,
	};

	private static void ValidateAttackPatternEnemies(List<string> warnings)
	{
		foreach (string path in AttackPatternEnemyScenePaths)
		{
			if (!ResourceLoader.Exists(path))
			{
				warnings.Add($"Attack-pattern enemy scene '{path}' is missing, so it would never spawn.");
				continue;
			}

			var scene = GD.Load<PackedScene>(path);
			if (scene == null)
			{
				warnings.Add($"Attack-pattern enemy scene '{path}' failed to load.");
				continue;
			}

			Node probe = scene.Instantiate();
			switch (probe)
			{
				case LungerEnemy lunger:
					ValidateLunger(warnings, path, lunger);
					break;
				case SlammerEnemy slammer:
					ValidateSlammer(warnings, path, slammer);
					break;
				case ExploderEnemy exploder:
					ValidateExploder(warnings, path, exploder);
					break;
				case SummonerEnemy summoner:
					ValidateSummoner(warnings, path, summoner);
					break;
				case BooEnemy ghost:
					ValidateGhost(warnings, path, ghost);
					break;
				case LanternTenderEnemy tender:
					ValidateLanternTender(warnings, path, tender);
					break;
				default:
					warnings.Add($"Attack-pattern enemy scene '{path}' has no attack-pattern script attached, so it would chase and melee like every other enemy.");
					break;
			}

			probe?.Free();
		}
	}

	private static void ValidateLunger(List<string> warnings, string path, LungerEnemy lunger)
	{
		if (lunger.LungeWindUpSeconds <= 0f)
			warnings.Add($"Lunger '{path}' has no wind-up, so its charge starts with no tell the player can read.");

		// A "dash" at or below walking pace is just a pause followed by more walking, and the
		// streak drawn during the wind-up would promise a reach it never covers.
		if (lunger.LungeSpeedMultiplier <= 1f)
			warnings.Add($"Lunger '{path}' has LungeSpeedMultiplier {lunger.LungeSpeedMultiplier}, so its charge is no faster than its walk.");

		if (lunger.MinimumLungeDistance >= lunger.LungeRange)
			warnings.Add($"Lunger '{path}' can never charge: MinimumLungeDistance {lunger.MinimumLungeDistance} is not below LungeRange {lunger.LungeRange}.");

		// The recovery is the reward for dodging. Without it the charge has no cost and the enemy
		// is simply faster than the player with extra steps.
		if (lunger.RecoverySeconds <= 0f)
			warnings.Add($"Lunger '{path}' has no recovery window, so reading and dodging its charge gains the player nothing.");
	}

	private static void ValidateSlammer(List<string> warnings, string path, SlammerEnemy slammer)
	{
		if (slammer.SlamTelegraphSeconds <= 0f)
			warnings.Add($"Slammer '{path}' has no telegraph, so its slam lands with no warning ring.");

		if (slammer.SlamRadius <= 0f)
			warnings.Add($"Slammer '{path}' has a slam radius of {slammer.SlamRadius}, so it would draw a ring and hit nothing.");

		if (slammer.SlamDamage <= 0)
			warnings.Add($"Slammer '{path}' deals {slammer.SlamDamage} slam damage, so its whole attack is decorative.");

		// Below 1 it only commits once the player is already inside the ring, which spends the
		// wind-up warning them about a hit they were given no room to avoid.
		if (slammer.SlamEngageRadiusMultiplier < 1f)
			warnings.Add($"Slammer '{path}' engages at {slammer.SlamEngageRadiusMultiplier}x its radius; below 1 the player is inside the ring before the tell begins.");

		// A cone is only worth having because it can be stepped out of sideways. At 90 degrees or
		// more it is a half-plane, there is no sideways left, and it is a worse ring.
		if (slammer.SlamShape == TelegraphShape.Cone
			&& (slammer.SlamHalfAngleDegrees <= 0f || slammer.SlamHalfAngleDegrees >= 90f))
		{
			warnings.Add($"Slammer '{path}' has a cone half-angle of {slammer.SlamHalfAngleDegrees} degrees; outside 0-90 it is either nothing or a half-plane the player cannot step out of.");
		}
	}

	private static void ValidateExploder(List<string> warnings, string path, ExploderEnemy exploder)
	{
		if (exploder.FuseSeconds <= 0f)
			warnings.Add($"Exploder '{path}' has no fuse, so it detonates on arrival with no chance to kill it or step away.");

		if (exploder.BlastRadius <= 0f)
			warnings.Add($"Exploder '{path}' has a blast radius of {exploder.BlastRadius}, so it would detonate harmlessly.");

		if (exploder.BlastDamage <= 0)
			warnings.Add($"Exploder '{path}' deals {exploder.BlastDamage} blast damage, so nothing is at stake in letting it reach you.");
	}

	private static void ValidateGhost(List<string> warnings, string path, BooEnemy ghost)
	{
		if (ghost.WailWindUpSeconds <= 0f)
			warnings.Add($"Ghost '{path}' has no wail wind-up, so its ring lands with no warning.");
		if (ghost.PhaseTellSeconds <= 0f)
			warnings.Add($"Ghost '{path}' has no phase tell, so the ring it places where it re-forms lands unseen.");
		if (ghost.WailRadius <= 0f || ghost.WailDamage <= 0)
			warnings.Add($"Ghost '{path}' wails at radius {ghost.WailRadius} for {ghost.WailDamage}, so the wail is decorative.");
		if (ghost.PhaseRadius <= 0f || ghost.PhaseDamage <= 0)
			warnings.Add($"Ghost '{path}' phases at radius {ghost.PhaseRadius} for {ghost.PhaseDamage}, so its mark hits nothing.");
		if (ghost.WailEngageRadiusMultiplier < 1f)
			warnings.Add($"Ghost '{path}' engages at {ghost.WailEngageRadiusMultiplier}x its wail radius; below 1 the player is inside the ring before the tell begins.");
		// The same rule as the bosses' burrows: the mark must land before the ghost re-forms on it,
		// or its arrival is itself an untelegraphed hit and the ring a lie about when it is safe.
		if (ghost.PhaseTravelSeconds <= ghost.PhaseTellSeconds)
			warnings.Add($"Ghost '{path}' crosses in {ghost.PhaseTravelSeconds}s but its mark takes {ghost.PhaseTellSeconds}s; it would re-form before its own warning lands.");
		if (ghost.PhaseMinDistance >= ghost.PhaseMaxDistance)
			warnings.Add($"Ghost '{path}' can never phase: PhaseMinDistance {ghost.PhaseMinDistance} is not below PhaseMaxDistance {ghost.PhaseMaxDistance}.");
		if (!ghost.IsMiniBoss)
			warnings.Add($"Ghost '{path}' is not IsMiniBoss, so it would be recycled to the spawn ring mid-fight, shoved by the player and pay ordinary XP.");
	}

	// The player walks 220 px a second (Player.Speed) and loses some of it to every body he pushes, so
	// the dark is only fair if the walk into the light fits its wind-up at well under that.
	private const float TenderDarkWalkPxPerSecond = 170f;

	// The lantern-tender (LanternTenderEnemy, .ai/side-events.md): four attacks, each a promise made
	// before it lands, and every way one breaks is quiet - a zero flight is a strike out of nowhere, a
	// light edge the player cannot reach in time is a hit he was never given, a dark that can be cast on
	// someone already in the light is a tell for nothing, a missing strip is a fire that draws nothing.
	private static void ValidateLanternTender(List<string> warnings, string path, LanternTenderEnemy tender)
	{
		string who = $"Lantern-tender '{path}'";

		// No zero wind-ups: the throw, every strike's flight (its ring is the tell), the oil, the dark,
		// the rekindle.
		if (tender.StrikeWindUpSeconds <= 0f)
			warnings.Add($"{who} has no strike wind-up, so a volley leaves the lantern with no tell.");
		if (tender.StrikeFlightSeconds < 0.6f)
			warnings.Add($"{who} lands its strikes {tender.StrikeFlightSeconds}s after throwing; the ring on the ground is the whole warning and needs at least 0.6s to be walked out of.");
		if (tender.OilWindUpSeconds <= 0f)
			warnings.Add($"{who} has no oil wind-up, so the splash lands with no wedge shown.");
		if (tender.DarkWindUpSeconds <= 0f)
			warnings.Add($"{who} has no dark wind-up, so the dark falls with no light to run to.");
		if (tender.RekindleWindUpSeconds <= 0f)
			warnings.Add($"{who} has no rekindle wind-up, so its ward arrives with no thread to say where from.");

		if (tender.StrikeRadius <= 0f || tender.StrikeDamage <= 0 || tender.StrikeCount < 1)
			warnings.Add($"{who} throws {tender.StrikeCount} strikes of radius {tender.StrikeRadius} for {tender.StrikeDamage}, so its fire strikes are decorative.");
		if (tender.StrikeCountWounded < tender.StrikeCount)
			warnings.Add($"{who} throws fewer strikes wounded ({tender.StrikeCountWounded}) than whole ({tender.StrikeCount}).");
		if (tender.OilReach <= 0f || tender.OilDamage <= 0)
			warnings.Add($"{who} spills oil {tender.OilReach} px for {tender.OilDamage}, so the splash hits nothing.");
		if (tender.OilBurnSeconds <= 0f || tender.OilBurnTickSeconds <= 0f || tender.OilBurnDamage <= 0)
			warnings.Add($"{who} leaves oil burning {tender.OilBurnSeconds}s, {tender.OilBurnDamage} every {tender.OilBurnTickSeconds}s, so nothing is left alight.");
		// The slammer's cone rule: at 90 degrees or more a wedge is a half-plane with no side to step to.
		if (tender.OilHalfAngleDegrees <= 0f || tender.OilHalfAngleDegrees >= 90f)
			warnings.Add($"{who} spills oil in a {tender.OilHalfAngleDegrees} degree half-angle; outside 0-90 there is no sideways out of it.");
		// Aimed at someone further than it reaches, the splash could never touch him.
		if (tender.OilEngageDistance <= 0f || tender.OilEngageDistance > tender.OilReach)
			warnings.Add($"{who} swings its oil at {tender.OilEngageDistance} px but it only reaches {tender.OilReach}, so it would mark a wedge that cannot hit.");
		if (tender.DarkDamage <= 0 || tender.DarkLightRadius <= 0f)
			warnings.Add($"{who} lets the dark in for {tender.DarkDamage} outside {tender.DarkLightRadius} px, so the dark is decorative.");
		// Only ever at someone outside the light, and never at someone too far to reach it in the tell.
		if (tender.DarkMinDistance <= tender.DarkLightRadius)
			warnings.Add($"{who} can lift the dark at {tender.DarkMinDistance} px, inside its own light ({tender.DarkLightRadius}), where it asks nothing.");
		if (tender.DarkMinDistance >= tender.DarkMaxDistance)
			warnings.Add($"{who} can never lift the dark: DarkMinDistance {tender.DarkMinDistance} is not below DarkMaxDistance {tender.DarkMaxDistance}.");
		float walk = (tender.DarkMaxDistance - tender.DarkLightRadius) / Mathf.Max(0.01f, tender.DarkWindUpSeconds);
		if (walk > TenderDarkWalkPxPerSecond)
			warnings.Add($"{who} lifts the dark at up to {tender.DarkMaxDistance} px with {tender.DarkWindUpSeconds}s to reach its light at {tender.DarkLightRadius}: {walk:0} px/s, over the {TenderDarkWalkPxPerSecond} a player in a crowd can be asked for.");
		if (tender.WardDamageTaken <= 0f || tender.WardDamageTaken >= 1f)
			warnings.Add($"{who} takes {tender.WardDamageTaken} of each hit while warded; at 0 it is unkillable until snuffed, at 1 the ward does nothing.");
		if (tender.RekindleHealthFraction <= 0f || tender.RekindleHealthFraction >= 1f)
			warnings.Add($"{who} rekindles at {tender.RekindleHealthFraction} health; it must be strictly between 0 and 1.");
		if (!tender.IsMiniBoss)
			warnings.Add($"{who} is not IsMiniBoss, so it would be recycled to the spawn ring mid-fight, shoved by the player and pay ordinary XP.");

		var sprite = tender.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		SpriteFrames frames = sprite?.SpriteFrames;
		if (frames == null)
			warnings.Add($"{who} has no SpriteFrames, so it would arrive invisible (tools/art/swamp_tender.py).");
		else
		{
			foreach (string anim in new[] { "moving", "attack", "death" })
				if (!frames.HasAnimation(anim) || frames.GetFrameCount(anim) == 0)
					warnings.Add($"{who} has no '{anim}' animation (tools/art/swamp_tender.py).");
			if (frames.HasAnimation("death") && frames.GetAnimationLoop("death"))
				warnings.Add($"{who} has a looping death, so its body would never free.");
		}

		// The fire is drawn from strips nothing else loads; a missing one draws nothing and says nothing.
		foreach (string strip in new[] { LanternFireStrike.StrikeTexturePath, LanternFireStrike.EmberTexturePath, BurningOil.TexturePath })
			if (!ResourceLoader.Exists(strip))
				warnings.Add($"{who}: '{strip}' is missing or not imported, so that part of its fire draws nothing (tools/art/swamp_tender.py, then import).");
	}

	private static void ValidateSummoner(List<string> warnings, string path, SummonerEnemy summoner)
	{
		if (summoner.SummonWindUpSeconds <= 0f)
			warnings.Add($"Summoner '{path}' has no wind-up, so minions appear with no ring showing where.");

		if (summoner.MaxActiveMinions < 1 || summoner.MinionsPerSummon < 1)
			warnings.Add($"Summoner '{path}' is capped at {summoner.MaxActiveMinions} active / {summoner.MinionsPerSummon} per summon, so it would telegraph and summon nothing.");

		// The lifetime cap is the anti-farming bound. Set below the live cap it would stop the
		// summoner before it ever fills the arena once, which is not what either cap is for.
		if (summoner.MaxTotalSummons < summoner.MaxActiveMinions)
			warnings.Add($"Summoner '{path}' has MaxTotalSummons {summoner.MaxTotalSummons} below MaxActiveMinions {summoner.MaxActiveMinions}, so it can never reach its own live cap.");

		if (summoner.RetreatDistance >= summoner.StandoffDistance)
			warnings.Add($"Summoner '{path}' has ranges out of order (retreat {summoner.RetreatDistance}, standoff {summoner.StandoffDistance}); retreat must be the smaller.");

		if (summoner.MinionHealthFraction <= 0f || summoner.MinionHealthFraction > 1f)
			warnings.Add($"Summoner '{path}' has MinionHealthFraction {summoner.MinionHealthFraction}; it must be above 0 and at most 1, or minions are free kills or tougher than their summoner.");

		ValidateSummonerMinion(warnings, path, summoner.MinionScenePath);
	}

	private static void ValidateSummonerMinion(List<string> warnings, string ownerPath, string minionPath)
	{
		if (string.IsNullOrWhiteSpace(minionPath) || !ResourceLoader.Exists(minionPath))
		{
			warnings.Add($"Summoner '{ownerPath}' points at a missing minion scene: '{minionPath}'.");
			return;
		}

		var scene = GD.Load<PackedScene>(minionPath);
		if (scene == null)
		{
			warnings.Add($"Minion scene '{minionPath}' failed to load, so '{ownerPath}' would telegraph and summon nothing.");
			return;
		}

		Node probe = scene.Instantiate();
		if (probe is not Enemy)
		{
			warnings.Add($"Minion scene '{minionPath}' has no Enemy script attached, so '{ownerPath}' cannot set its health or track it.");
		}
		else if (probe is SummonerEnemy)
		{
			// Each generation carries its own MaxTotalSummons, so summoners summoning summoners is
			// bounded but grows exponentially - and every one of them is another live tell to draw.
			warnings.Add($"Summoner '{ownerPath}' summons another summoner ('{minionPath}'), which multiplies across generations.");
		}

		probe?.Free();
	}

	private static void ValidateEnemyProjectile(List<string> warnings, string ownerPath, string projectilePath)
	{
		if (string.IsNullOrWhiteSpace(projectilePath) || !ResourceLoader.Exists(projectilePath))
		{
			warnings.Add($"Ranged enemy '{ownerPath}' points at a missing projectile scene: '{projectilePath}'.");
			return;
		}

		var scene = GD.Load<PackedScene>(projectilePath);
		if (scene == null)
		{
			warnings.Add($"Projectile scene '{projectilePath}' failed to load, so '{ownerPath}' would telegraph and then fire nothing.");
			return;
		}

		Node probe = scene.Instantiate();
		if (probe is not EnemyProjectile bolt)
		{
			warnings.Add($"Projectile scene '{projectilePath}' has no EnemyProjectile script attached.");
			probe?.Free();
			return;
		}

		// The bolt finds the player through its collision shape; without one it passes straight
		// through and the caster is decorative.
		if (bolt.GetNodeOrNull<CollisionShape2D>("CollisionShape2D") == null)
			warnings.Add($"Projectile scene '{projectilePath}' has no CollisionShape2D, so its bolts would pass through the player.");

		bolt.Free();
	}

	// THE BESTIARY (.ai/bestiary.md). Every chapter's roster (StageBestiary.<Chapter>.cs) names a scene
	// for every job its spawner has, and a dotnet build sees none of those scenes. Each way one breaks
	// is quiet: a missing body is a band of the table that never spawns, a caster wearing a chaser's
	// script never shoots, a sheet with a looping death leaves a corpse that never frees, and a new
	// creature copied from the wrong scene retunes a chapter nobody meant to retune.
	//
	// The last warning per chapter is not a fault: it is the Phase 2 to-do list. It names every slot
	// still filled by a stand-in or by another chapter's creature, and it shortens by itself as each
	// creature's scene is created at the path its roster already gives it.
	private static void ValidateBestiary(List<string> warnings)
	{
		var byId = new Dictionary<string, BestiaryCreature>(StringComparer.OrdinalIgnoreCase);
		var byPath = new Dictionary<string, BestiaryCreature>(StringComparer.OrdinalIgnoreCase);
		var homeByType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (BestiaryCreature creature in StageBestiary.AllCreatures())
		{
			if (!byId.TryAdd(creature.Id, creature))
				warnings.Add($"Bestiary: creature id '{creature.Id}' is declared twice ({byId[creature.Id].DisplayName}, {creature.DisplayName}).");
			// One scene, two creatures: two homes, and the ownership check below cannot say whose it is.
			if (!byPath.TryAdd(creature.ScenePath, creature))
				warnings.Add($"Bestiary: {creature} and {byPath[creature.ScenePath]} are both declared at '{creature.ScenePath}'.");
			if (StageCatalog.GetById(creature.HomeStageId) == null)
				warnings.Add($"Bestiary: {creature} belongs to '{creature.HomeStageId}', which is not a chapter.");
			// Enemy.DropXp pays these four names as a tank (5) or a runner (3) instead of 2. No shipped
			// scene uses them, so a new creature given one would be the only enemy in the game paying it.
			if (BestiaryXpTierTypes.Contains(creature.EnemyType))
				warnings.Add($"Bestiary: {creature} has EnemyType '{creature.EnemyType}', which Enemy.DropXp pays as an XP tier of its own; give it a name of its own.");
			// The lifetime kill tally is keyed by type, so two chapters' creatures sharing one would count as one.
			if (homeByType.TryGetValue(creature.EnemyType, out string? home) && !home.Equals(creature.HomeStageId, StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Bestiary: EnemyType '{creature.EnemyType}' is used in two chapters ({home}, {creature.HomeStageId}); kills of both would be tallied as one creature.");
			else
				homeByType[creature.EnemyType] = creature.HomeStageId;
		}

		foreach (StageDefinition stage in StageCatalog.All)
		{
			if (stage.IsPlayable && StageBestiary.ForStageId(stage.Id) == null)
				warnings.Add($"Bestiary: {stage.DisplayName} ({stage.Id}) is playable and has no roster in StageBestiary.All, so it would spawn the Cursed Dungeon's cast.");
		}

		// Each scene is built once and freed at the end, however many slots and chapters name it.
		var probes = new Dictionary<string, Enemy?>(StringComparer.OrdinalIgnoreCase);
		var behaviourChecked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (ChapterBestiary chapter in StageBestiary.All)
				ValidateChapterBestiary(warnings, chapter, probes, behaviourChecked);
		}
		finally
		{
			foreach (Enemy? probe in probes.Values)
				probe?.Free();
		}
	}

	private static readonly HashSet<string> BestiaryXpTierTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"Tank", "TankEnemy", "Fast", "FastEnemy",
	};

	private static void ValidateChapterBestiary(List<string> warnings, ChapterBestiary chapter,
		Dictionary<string, Enemy?> probes, HashSet<string> behaviourChecked)
	{
		string name = chapter.ChapterName;
		StageDefinition? stage = StageCatalog.GetById(chapter.StageId);
		if (stage == null)
		{
			warnings.Add($"Bestiary: a roster names '{chapter.StageId}', which is not a chapter.");
			return;
		}

		foreach (BestiaryRole role in StageBestiary.RequiredRoles)
		{
			int count = chapter.Slots.Count(s => s.Role == role);
			if (count == 0)
				warnings.Add($"Bestiary: {name} has no {role} slot; give it a creature, or an Absent slot saying why it has none.");
			else if (count > 1)
				warnings.Add($"Bestiary: {name} has {count} {role} slots and the spawner only ever reads the first.");
		}

		var todo = new List<string>();
		foreach (BestiarySlot slot in chapter.Slots)
		{
			if (slot.IsAbsent)
			{
				if (string.IsNullOrWhiteSpace(slot.AbsentReason))
					warnings.Add($"Bestiary: {name} has no {slot.Role} and does not say why.");
				// The table and the clock always need a body; only a rolled role can be skipped.
				if (slot.Role is BestiaryRole.Bulk or BestiaryRole.MiniBoss or BestiaryRole.Extra)
					warnings.Add($"Bestiary: {name}'s {slot.Role} cannot be absent - the table and the miniboss clock always need a body.");
				continue;
			}

			BestiaryCreature creature = slot.Creature!;
			if (!creature.IsDrawn)
			{
				if (slot.StandIn == null)
				{
					warnings.Add($"Bestiary: {name}'s {slot.Role} is {creature}, but there is no scene at '{creature.ScenePath}' and no stand-in, so the slot spawns nothing.");
					continue;
				}
				todo.Add($"{slot.Role} {creature.DisplayName} ({ShortScenePath(creature.ScenePath)})");
			}
			// A chapter whose cast is borrowed by design (the Throne's household guard) says so once, in its
			// roster, instead of carrying a to-do that asks for art it must never have.
			else if (!IsHome(creature, chapter) && string.IsNullOrWhiteSpace(chapter.BorrowedCastBecause))
			{
				todo.Add($"{slot.Role} is {creature.DisplayName}, borrowed from {HomeName(creature)}");
			}

			BestiaryCreature resolved = slot.Resolved!;
			Enemy? probe = ProbeBestiaryScene(warnings, resolved, probes, $"{name}'s {slot.Role}");
			if (probe == null)
				continue;

			ValidateBestiaryRole(warnings, chapter, slot.Role, resolved, probe);
			ValidateBestiaryArt(warnings, resolved, probe);
			ValidateBestiaryBehaviour(warnings, resolved.ScenePath, probe, behaviourChecked);

			// A drawn creature is a new look at the numbers its slot was tuned on, unless its roster
			// says otherwise and why.
			if (creature.IsDrawn && slot.StandIn != null && string.IsNullOrWhiteSpace(slot.StatsChange))
			{
				Enemy? template = ProbeBestiaryScene(warnings, slot.StandIn, probes, $"{name}'s {slot.Role} stand-in");
				if (template != null)
					ValidateBestiaryParity(warnings, creature, probe, slot.StandIn, template);
			}

			// A chapter's own summoner calls up the chapter's own. (A stand-in summoner is already on the list.)
			if (probe is SummonerEnemy summoner && IsHome(resolved, chapter))
			{
				BestiaryCreature? minion = StageBestiary.CreatureAt(summoner.MinionScenePath);
				if (minion == null)
					warnings.Add($"Bestiary: {resolved} calls up '{summoner.MinionScenePath}', which no chapter's roster names.");
				else if (!IsHome(minion, chapter))
					todo.Add($"{resolved.DisplayName} calls up {minion.DisplayName}, from {HomeName(minion)}");
			}
		}

		ValidateBestiaryTable(warnings, chapter, chapter.Table, "table");
		if (chapter.LateTable.Count > 0)
		{
			ValidateBestiaryTable(warnings, chapter, chapter.LateTable, "late table");
			if (chapter.LateTableFromPaceMinutes <= 0f)
				warnings.Add($"Bestiary: {name} has a late table from pace minute {chapter.LateTableFromPaceMinutes}, so its opening table is never rolled.");
		}

		foreach (BestiaryCreature attendant in chapter.Attendants)
		{
			if (!IsHome(attendant, chapter))
				warnings.Add($"Bestiary: {name} lists {attendant} as an attendant, but it belongs to {HomeName(attendant)}.");
			Enemy? probe = ProbeBestiaryScene(warnings, attendant, probes, $"{name}'s attendant");
			if (probe != null)
				ValidateBestiaryArt(warnings, attendant, probe);
		}

		// What the chapter's boss calls up in the fight is part of the chapter's cast too.
		BossDefinition? boss = BossCatalog.ForStageIndex(stage.Index);
		foreach (string addPath in BossAddScenePaths(boss))
		{
			BestiaryCreature? add = StageBestiary.CreatureAt(addPath);
			if (add == null)
				warnings.Add($"Bestiary: {boss!.DisplayName} calls up '{addPath}', which no roster names; list it as one of {name}'s attendants.");
			else if (!IsHome(add, chapter))
				todo.Add($"{boss!.DisplayName} calls up {add.DisplayName}, from {HomeName(add)}");
		}

		if (todo.Count > 0)
			warnings.Add($"Bestiary to-do (Phase 2, .ai/bestiary.md): {name} still spawns {todo.Count} creatures that are not its own - " + string.Join("; ", todo) + ".");
	}

	private static bool IsHome(BestiaryCreature creature, ChapterBestiary chapter) =>
		creature.HomeStageId.Equals(chapter.StageId, StringComparison.OrdinalIgnoreCase);

	private static string HomeName(BestiaryCreature creature) =>
		StageCatalog.GetById(creature.HomeStageId)?.DisplayName ?? creature.HomeStageId;

	private static string ShortScenePath(string path) => path.StartsWith("res://") ? path.Substring(6) : path;

	private static Enemy? ProbeBestiaryScene(List<string> warnings, BestiaryCreature creature,
		Dictionary<string, Enemy?> probes, string context)
	{
		if (probes.TryGetValue(creature.ScenePath, out Enemy? cached))
			return cached;

		Enemy? enemy = null;
		if (!ResourceLoader.Exists(creature.ScenePath))
		{
			warnings.Add($"Bestiary: {context} is {creature}, whose scene '{creature.ScenePath}' does not exist.");
		}
		else
		{
			Node? node = GD.Load<PackedScene>(creature.ScenePath)?.Instantiate();
			if (node is Enemy built)
				enemy = built;
			else
			{
				warnings.Add($"Bestiary: {context} is {creature}, but '{creature.ScenePath}' has no Enemy script on its root, so it cannot spawn.");
				node?.Free();
			}
		}
		probes[creature.ScenePath] = enemy;
		return enemy;
	}

	// The behaviour class a role needs. The spawner hands every role the same health curve and
	// elite treatment, so a role filled by the wrong class still spawns - it just does the wrong thing.
	private static void ValidateBestiaryRole(List<string> warnings, ChapterBestiary chapter, BestiaryRole role,
		BestiaryCreature creature, Enemy probe)
	{
		string name = chapter.ChapterName;
		if (probe.EnemyType != creature.EnemyType)
			warnings.Add($"Bestiary: {creature} is declared as EnemyType '{creature.EnemyType}' but its scene says '{probe.EnemyType}'; the miniboss clock and the kill tally read the declared one.");

		string? wrong = role switch
		{
			BestiaryRole.Ranged when probe is not RangedEnemy { Rooted: false } => "a RangedEnemy that is not Rooted",
			BestiaryRole.Turret when probe is not RangedEnemy { Rooted: true } => "a RangedEnemy with Rooted set",
			BestiaryRole.Charger when probe is not LungerEnemy => "a LungerEnemy",
			BestiaryRole.Exploder when probe is not ExploderEnemy => "an ExploderEnemy",
			BestiaryRole.Slammer when probe is not SlammerEnemy => "a SlammerEnemy",
			BestiaryRole.Summoner when probe is not SummonerEnemy => "a SummonerEnemy",
			BestiaryRole.MiniBoss when !probe.IsMiniBoss || probe is BossEnemy => "an Enemy with IsMiniBoss set (and not a boss)",
			_ when probe is BossEnemy => "an ordinary enemy, not a boss",
			_ => null,
		};
		if (wrong != null)
			warnings.Add($"Bestiary: {name}'s {role} is {creature}, which is a {probe.GetType().Name}; the role needs {wrong}.");

		// A table body flagged IsMiniBoss would block the player and pay the miniboss's 50 XP on every
		// spawn. Node2DGame.SpawnEnemy clears the flag on everything the table rolls, so this is the
		// scene's own mistake caught early, and the side events' fallback to the table (which keeps
		// the scene's flag) is still covered. The kids' Rockem is both the Sketchbook's heavy and its
		// miniboss, as drawn, so its scene carries the flag for the miniboss and is exempt here.
		bool body = role is BestiaryRole.Bulk or BestiaryRole.Fast or BestiaryRole.Heavy or BestiaryRole.Wall or BestiaryRole.Extra;
		if (body && probe.IsMiniBoss && !IsArtContractExempt(creature))
			warnings.Add($"Bestiary: {name}'s {role} is {creature}, which has IsMiniBoss set, so every one the table rolls blocks the player and pays 50 XP.");
	}

	private static bool IsArtContractExempt(BestiaryCreature creature) =>
		StageBestiary.ForStageId(creature.HomeStageId)?.ArtContractExempt == true;

	// The animations a behaviour plays, held to .ai/art-direction.md section 5. Every one of these
	// falls back silently in a run (Enemy.StartDeath frees a sheet with no death, PlayAttackAnimation
	// skips a sheet with no attack), which is right for the kids' drawings and wrong for anything drawn
	// to the contract.
	private static void ValidateBestiaryArt(List<string> warnings, BestiaryCreature creature, Enemy probe)
	{
		var sprite = probe.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		SpriteFrames? frames = sprite?.SpriteFrames;
		if (frames == null)
		{
			warnings.Add($"Bestiary: {creature} has no AnimatedSprite2D with SpriteFrames, so it would spawn invisible.");
			return;
		}

		bool exempt = IsArtContractExempt(creature);
		if (!frames.HasAnimation("moving"))
			warnings.Add($"Bestiary: {creature} has no 'moving' animation, so it stands still while it chases.");
		else
		{
			if (!frames.GetAnimationLoop("moving"))
				warnings.Add($"Bestiary: {creature}'s 'moving' does not loop, so it walks one cycle and freezes.");
			// A PNG Godot has not imported loads as nothing and draws nothing, with no error at all.
			Texture2D? first = frames.GetFrameCount("moving") > 0 ? frames.GetFrameTexture("moving", 0) : null;
			if (first == null || (first is AtlasTexture atlas && atlas.Atlas == null))
				warnings.Add($"Bestiary: {creature}'s 'moving' has no texture - is its PNG imported? It would spawn invisible.");
		}

		if (frames.HasAnimation("death"))
		{
			if (frames.GetAnimationLoop("death"))
				warnings.Add($"Bestiary: {creature}'s 'death' loops, so it never finishes and the corpse never frees.");
		}
		else if (!exempt)
			warnings.Add($"Bestiary: {creature} has no 'death' animation, so it vanishes on its last hit; the art contract makes one mandatory.");

		bool telegraphs = probe is RangedEnemy or LungerEnemy or SlammerEnemy or ExploderEnemy or SummonerEnemy or BooEnemy;
		if (frames.HasAnimation("attack"))
		{
			if (frames.GetAnimationLoop("attack"))
				warnings.Add($"Bestiary: {creature}'s 'attack' loops, so its wind-up pose never hands back to 'moving'.");
		}
		else if (telegraphs && !exempt)
			warnings.Add($"Bestiary: {creature} telegraphs but has no 'attack' animation, so its body does nothing while it winds up.");

		if (frames.HasAnimation("hurt") && frames.GetAnimationLoop("hurt"))
			warnings.Add($"Bestiary: {creature}'s 'hurt' loops, so the first hit leaves it flinching for good.");
	}

	// The timing checks every behaviour class already has, run on any roster scene the hand-kept
	// lists above do not name - so a chapter's new caster or slammer is held to them without anyone
	// editing those lists.
	private static void ValidateBestiaryBehaviour(List<string> warnings, string path, Enemy probe, HashSet<string> done)
	{
		if (!done.Add(path))
			return;
		if (probe is RangedEnemy ranged)
		{
			if (!RangedEnemyScenePaths.Contains(path, StringComparer.OrdinalIgnoreCase))
				ValidateRangedEnemy(warnings, path, ranged);
			return;
		}
		if (AttackPatternEnemyScenePaths.Contains(path, StringComparer.OrdinalIgnoreCase))
			return;
		switch (probe)
		{
			case LungerEnemy lunger: ValidateLunger(warnings, path, lunger); break;
			case SlammerEnemy slammer: ValidateSlammer(warnings, path, slammer); break;
			case ExploderEnemy exploder: ValidateExploder(warnings, path, exploder); break;
			case SummonerEnemy summoner: ValidateSummoner(warnings, path, summoner); break;
			case BooEnemy ghost: ValidateGhost(warnings, path, ghost); break;
		}
	}

	private static void ValidateBestiaryTable(List<string> warnings, ChapterBestiary chapter,
		IReadOnlyList<BestiaryTableEntry> table, string which)
	{
		string name = chapter.ChapterName;
		if (table.Count == 0)
		{
			warnings.Add($"Bestiary: {name} has an empty {which}, so nothing ordinary ever spawns.");
			return;
		}

		double total = 0.0;
		foreach (BestiaryTableEntry band in table)
		{
			total += band.Share;
			if (band.Share <= 0f)
				warnings.Add($"Bestiary: {name}'s {which} has a band with share {band.Share}, which never rolls.");
			if (band.HealthMultiplier <= 0f)
				warnings.Add($"Bestiary: {name}'s {which} has a band at health x{band.HealthMultiplier}, which spawns dead.");
			if (!chapter.Slots.Contains(band.Slot))
				warnings.Add($"Bestiary: {name}'s {which} rolls a slot that is not in its roster ({band.Slot.Role}).");
			else if (band.Slot.IsAbsent)
				warnings.Add($"Bestiary: {name}'s {which} rolls its {band.Slot.Role}, which it says it does not have.");
			else if (band.Slot.Role is not (BestiaryRole.Bulk or BestiaryRole.Fast or BestiaryRole.Heavy or BestiaryRole.Wall or BestiaryRole.Extra))
				warnings.Add($"Bestiary: {name}'s {which} rolls its {band.Slot.Role}, which already has a roll of its own ahead of the table.");
		}
		// The last band takes what is left over, so a short table silently over-weights it and a long
		// one silently never reaches its last bands.
		if (Math.Abs(total - 1.0) > 0.002)
			warnings.Add($"Bestiary: {name}'s {which} shares add up to {total:0.###}, not 1.");
	}

	// The balance-bearing numbers of each behaviour class. A Phase 2 creature copies its stand-in's
	// root properties and changes its art, its EnemyType and its colours - not these.
	private static readonly string[] EnemyParityProperties =
	{
		"Speed", "Health", "ContactDamage", "KnockbackResistance", "MinSlowMultiplier", "IsMiniBoss",
	};

	private static readonly Dictionary<Type, string[]> BehaviourParityProperties = new()
	{
		[typeof(RangedEnemy)] = new[]
		{
			"StandoffDistance", "RetreatDistance", "AttackRange", "Rooted", "CastIntervalSeconds", "CastWindUpSeconds",
			"ProjectileDamage", "ProjectileSpeed", "BoltsPerVolley", "VolleySpreadDegrees", "BoltLeadFraction",
		},
		[typeof(LungerEnemy)] = new[]
		{
			"LungeIntervalSeconds", "LungeWindUpSeconds", "LungeDurationSeconds", "LungeSpeedMultiplier", "RecoverySeconds",
			"RecoverySpeedMultiplier", "LungeRange", "MinimumLungeDistance", "RepositionSpeedMultiplier", "RepositionExitMultiplier",
		},
		[typeof(SlammerEnemy)] = new[]
		{
			"SlamIntervalSeconds", "SlamTelegraphSeconds", "SlamRadius", "SlamDamage", "SlamEngageRadiusMultiplier",
			"SlamShape", "SlamHalfAngleDegrees",
		},
		[typeof(ExploderEnemy)] = new[]
		{
			"FuseSeconds", "BlastRadius", "BlastDamage", "TriggerRadiusMultiplier", "BlastLingerSeconds",
		},
		[typeof(SummonerEnemy)] = new[]
		{
			"SummonIntervalSeconds", "SummonWindUpSeconds", "MinionsPerSummon", "MaxActiveMinions", "MaxTotalSummons",
			"MinionHealthFraction", "SummonSpawnRadius", "StandoffDistance", "RetreatDistance",
		},
	};

	private static void ValidateBestiaryParity(List<string> warnings, BestiaryCreature creature, Enemy probe,
		BestiaryCreature standIn, Enemy template)
	{
		if (probe.GetType() != template.GetType())
		{
			warnings.Add($"Bestiary: {creature} is a {probe.GetType().Name} but the slot it fills was tuned on {standIn}, a {template.GetType().Name}.");
			return;
		}

		var differences = new List<string>();
		IEnumerable<string> properties = EnemyParityProperties;
		if (BehaviourParityProperties.TryGetValue(probe.GetType(), out string[]? behaviour))
			properties = properties.Concat(behaviour);
		foreach (string property in properties)
		{
			var info = probe.GetType().GetProperty(property);
			if (info == null)
				continue;
			object? mine = info.GetValue(probe);
			object? theirs = info.GetValue(template);
			bool same = mine is float a && theirs is float b ? Math.Abs(a - b) < 0.0001f : Equals(mine, theirs);
			if (!same)
				differences.Add($"{property} {mine} vs {theirs}");
		}

		// The body's size: within a third either way, so a new silhouette can have the hitbox it is
		// drawn with without turning a chaser into a wall or a needle.
		float myExtent = BodyExtent(probe), theirExtent = BodyExtent(template);
		if (theirExtent > 0f && (myExtent < theirExtent * 0.7f || myExtent > theirExtent * 1.3f))
			differences.Add($"body size {myExtent:0.#} vs {theirExtent:0.#}");

		// Every basic body sits at one scale; a different one is a body a different size on screen.
		Vector2? myScale = probe.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D")?.Scale;
		Vector2? theirScale = template.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D")?.Scale;
		if (myScale.HasValue && theirScale.HasValue && !myScale.Value.IsEqualApprox(theirScale.Value))
			differences.Add($"sprite scale {myScale.Value} vs {theirScale.Value}");

		if (differences.Count > 0)
			warnings.Add($"Bestiary: {creature} differs from {standIn}, the stat block its slot was tuned on ({string.Join(", ", differences)}). Match it, or give its Planned slot a statsChange saying why.");
	}

	private static float BodyExtent(Node node) =>
		node.GetNodeOrNull<CollisionShape2D>("CollisionShape2D")?.Shape switch
		{
			CircleShape2D circle => circle.Radius,
			RectangleShape2D rect => Mathf.Max(rect.Size.X, rect.Size.Y) * 0.5f,
			CapsuleShape2D capsule => Mathf.Max(capsule.Radius, capsule.Height * 0.5f),
			_ => 0f,
		};

	// The enemy scenes a chapter's boss calls up during its fight. Two bosses do: Mother Rot sheds
	// her spawn and the Still Warden calls its guards. (The Hollow Choir calls up copies of itself.)
	private static IEnumerable<string> BossAddScenePaths(BossDefinition? boss)
	{
		// A boss on its stand-in calls up the stand-in's adds; that is what the run does.
		if (boss == null || string.IsNullOrWhiteSpace(boss.SpawnScenePath) || !ResourceLoader.Exists(boss.SpawnScenePath))
			yield break;
		Node? probe = GD.Load<PackedScene>(boss.SpawnScenePath)?.Instantiate();
		string? add = probe switch
		{
			MotherRotBoss rot => rot.SpawnScenePath,
			StillWardenBoss warden => warden.GuardScenePath,
			_ => null,
		};
		probe?.Free();
		if (!string.IsNullOrWhiteSpace(add))
			yield return add!;
	}

	// A boss is the only way to win a level, so a broken entry silently costs the player the ending
	// of that stage - the run would just keep going past the timer with nothing to kill. None of
	// this is checkable from a dotnet build: the scene path and its script only resolve in Godot.
	private static void ValidateBossCatalog(List<string> warnings)
	{
		foreach (BossDefinition boss in BossCatalog.All)
		{
			int stageIndex = BossCatalog.StageIndexOf(boss);

			if (string.IsNullOrWhiteSpace(boss.Id))
				warnings.Add($"Boss for stage {stageIndex} has no Id, so its victory cannot be recorded or matched to an achievement.");

			ValidateBossScene(warnings, boss);

			if (boss.Health <= 0)
				warnings.Add($"Boss '{boss.Id}' has non-positive Health, so it would die on spawn.");

			// Boss ids are matched exactly by the achievements now, so a typo on either side is a
			// boss whose kill silently grants nothing. The probe records the win into a throwaway
			// save's lifetime stats, which is the same path a real victory takes.
			// A boon counts, and so does currency. The check is "does beating this pay out",
			// not "does it pay out a spell": two chapter clears pay in a boon because the shop
			// and the achievement track draw on one finite pool, and the Sketchbook pays in
			// Arcane Energy because it is a bonus chapter and must not hold campaign content.
			if (!string.IsNullOrWhiteSpace(boss.Id)
				&& !AchievementDefinitions.All.Any(a =>
					(!string.IsNullOrWhiteSpace(a.SpellUnlockId)
						|| !string.IsNullOrWhiteSpace(a.BoonUnlockId)
						|| !string.IsNullOrWhiteSpace(a.CharacterUnlockId)
						|| a.CurrencyReward > 0)
					&& a.IsComplete(BuildVictoryProbe(boss.Id))))
			{
				warnings.Add($"Boss '{boss.Id}' matches no achievement, so defeating it pays nothing at all.");
			}

			// What a victory opens is the campaign order's to say now (StageCatalog.ChapterOpenedBy),
			// and ValidateCampaign checks that. A boss still fighting on a stand-in is on the Phase 2
			// to-do list there too, not here: the stand-in scene is checked as the scene that spawns.
		}
	}

	// The tuning a boss fight is actually made of, none of which a dotnet build can see: every
	// number below lives in a .tscn and only resolves in Godot.
	//
	// The failures these catch all look like nothing while they are happening. A wind-up of zero
	// is an attack that simply arrives, and the boss still walks around looking perfectly fine. A
	// charge tell of 1.0 is a boss that plants for its whole wind-up and then never moves, which
	// reads as a boss that has stopped working. A burrow shorter than the tell it places means
	// the boss surfaces before its own warning resolves and the warning lands behind it.
	private static void ValidateBossPatterns(List<string> warnings)
	{
		foreach (BossDefinition boss in BossCatalog.All)
		{
			// A boss on its stand-in is that other boss, already checked under its own entry.
			if (boss.IsOnStandIn || string.IsNullOrWhiteSpace(boss.ScenePath) || !ResourceLoader.Exists(boss.ScenePath))
				continue;

			var scene = GD.Load<PackedScene>(boss.ScenePath);
			if (scene?.Instantiate() is not BossEnemy probe)
				continue;

			// THE ONE RULE EVERY BOSS SHARES. Everything below it is per-fight.
			if (probe.SlamTelegraphSeconds <= 0f)
				warnings.Add($"Boss '{boss.Id}' has no telegraph, so its attack lands with no warning at all.");

			if (probe.SlamDamage <= 0)
				warnings.Add($"Boss '{boss.Id}' deals {probe.SlamDamage} attack damage, so its whole telegraphed attack is decorative.");

			if (probe.SlamRadius <= 0f)
				warnings.Add($"Boss '{boss.Id}' has a reach of {probe.SlamRadius}, so it would draw a warning and hit nothing.");

			if (probe.EnrageHealthFraction <= 0f || probe.EnrageHealthFraction >= 1f)
				warnings.Add($"Boss '{boss.Id}' has EnrageHealthFraction {probe.EnrageHealthFraction}; outside 0-1 the fight either never escalates or opens enraged.");

			// THE FALL (BossDeath). Long enough to be seen, short enough that the arena is not held
			// for a corpse: about the length of boss_death (2.4 s), which plays under it.
			if (probe.DeathSeconds < 1.2f || probe.DeathSeconds + probe.DeathDelaySeconds > 3.5f)
				warnings.Add($"Boss '{boss.Id}' falls for {probe.DeathSeconds}s after {probe.DeathDelaySeconds}s; outside 1.2-3.5 s it either vanishes or holds the arena for a corpse.");

			// THE ARRIVAL (2026-10-31). The player's spells and the whole field are held while it plays
			// (Node2DGame.BossEntrance), so it must be long enough to be seen and short enough that the
			// hold is a beat, not a cutscene. The Choir's last voice waits two turns of its delay.
			float arrival = probe.EntranceSeconds + probe.EntranceDelaySeconds;
			if (probe.EntranceSeconds < 2.5f || arrival > 4.0f)
				warnings.Add($"Boss '{boss.Id}' arrives over {probe.EntranceSeconds}s after {probe.EntranceDelaySeconds}s; outside 2.5-4 s the field is held for too little to see it or too long to bear.");

			// AT LEAST TWO PHASES, each a fraction strictly inside 0-1 and below the one before: a
			// threshold out of order is crossed on the same frame as its neighbour and its change is lost.
			float[] thresholds = probe.DesignedPhaseThresholds;
			if (thresholds.Length < 2)
				warnings.Add($"Boss '{boss.Id}' has {thresholds.Length} phase threshold(s); every boss changes what its fight asks at least twice.");
			for (int i = 0; i < thresholds.Length; i++)
			{
				if (thresholds[i] <= 0f || thresholds[i] >= 1f || (i > 0 && thresholds[i] >= thresholds[i - 1]))
					warnings.Add($"Boss '{boss.Id}' phase threshold {i + 1} is {thresholds[i]}; thresholds must fall strictly inside 0-1, each below the last.");
			}

			ValidateBossPattern(warnings, probe);
			probe.Free();
		}

		// What every fall throws comes from one generated sheet. Missing, or short of the cells the
		// falls name, it throws nothing and says nothing (BossDeath draws no debris without it).
		Texture2D debris = ResourceLoader.Exists(BossDeath.DebrisAtlasPath) ? GD.Load<Texture2D>(BossDeath.DebrisAtlasPath) : null;
		int lastCell = BossDeath.Cell.Hide;
		if (debris == null)
			warnings.Add($"The boss death debris atlas '{BossDeath.DebrisAtlasPath}' is missing: re-run tools/art/boss_deaths.py and import.");
		else if (debris.GetHeight() < (lastCell / 8 + 1) * BossDeath.DebrisCell || debris.GetWidth() < 8 * BossDeath.DebrisCell)
			warnings.Add($"The boss death debris atlas is {debris.GetWidth()}x{debris.GetHeight()}, short of cell {lastCell} that BossDeath.Cell names.");
	}

	// Split out only because the switch is long. Everything in it is one boss idea plus the
	// number that would silently switch that idea off.
	private static void ValidateBossPattern(List<string> warnings, BossEnemy probe)
	{
		// Elderbark's roots (bare BossEnemy): marked, and each a beat after the last, or the line lands at once.
		if (probe.GetType() == typeof(BossEnemy) && (probe.RootTellSeconds <= 0f || probe.RootStaggerSeconds < 0f || probe.RootsPerLine < 2))
			warnings.Add($"Elderbark's roots ({probe.RootsPerLine} at {probe.RootTellSeconds}s, {probe.RootStaggerSeconds}s apart) land unseen or all at once.");

		switch (probe)
		{
			// The Ninth Stone holds its own geometry to the player's walk (the gap walkable, the rest
			// of the ring closed, time to walk out); NinthStoneBoss.TuningProblems says what broke.
			case NinthStoneBoss stone:
				warnings.AddRange(stone.TuningProblems());
				break;

			// So does the Seneschal (CourtSeneschalBoss.TuningProblems): the rank's lane marked and not
			// outrun, a door each way, a household behind him when he goes, the guards' sheet present.
			case CourtSeneschalBoss seneschal:
				warnings.AddRange(seneschal.TuningProblems());
				break;

			// The Constable holds his own tuning to the player's walk and to the fight's shape (the gate's
			// pour walkable, the files' gaps wide enough, the yard visit long enough to swing twice);
			// BridgeConstableBoss.TuningProblems says what broke.
			case BridgeConstableBoss constable:
				warnings.AddRange(constable.TuningProblems());
				break;

			// The dark wizard holds his own (ThroneDarkWizardBoss.TuningProblems): the taking's long tell,
			// a hand and an orb slower than the wizard walks, a seal that ends, room between his lanes.
			case ThroneDarkWizardBoss darkWizard:
				warnings.AddRange(darkWizard.TuningProblems());
				break;

			case GaolerBoss gaoler:
				// Below 0 there is no planted tell before the charge; at 1 the charge never
				// happens, because the wind-up ends before it starts moving.
				if (gaoler.ChargeTellFraction <= 0f || gaoler.ChargeTellFraction >= 1f)
					warnings.Add($"Gaoler has ChargeTellFraction {gaoler.ChargeTellFraction}; at 0 it charges with no tell and at 1 it never charges at all.");
				if (gaoler.ChargeSpeedMultiplier <= 1f)
					warnings.Add($"Gaoler charges at {gaoler.ChargeSpeedMultiplier}x its walk, so the lane it draws promises a reach it never covers.");
				if (gaoler.DismountHealthFraction <= probe.EnrageHealthFraction)
					warnings.Add($"Gaoler dismounts at {gaoler.DismountHealthFraction} and enrages at {probe.EnrageHealthFraction}; the dismount has to come first or phase two is skipped.");
				break;

			case HollowChoirBoss choir:
				// The voices stand apart so one blast cannot take the set (2026-10-31); a place inside
				// their own reach puts them back on top of the wizard and of each other.
				if (choir.StandRadius <= choir.SlamRadius)
					warnings.Add($"Hollow Choir voices stand {choir.StandRadius} px out with a {choir.SlamRadius} px reach, so they crowd him and one blast takes all three.");
				if (choir.NoteTellSeconds <= 0f)
					warnings.Add("Hollow Choir's round sings its notes with no gesture at all, so they leave the voice unseen.");
				if (choir.VoiceCount < 2)
					warnings.Add($"Hollow Choir has {choir.VoiceCount} voices, so nothing can revive anything and the linked-health fight is an ordinary boss.");
				if (choir.NoteSpeed <= 0f || choir.NoteRadius <= 0f)
					warnings.Add($"Hollow Choir's notes fly at {choir.NoteSpeed} px/s with a {choir.NoteRadius} px reach, so the round hits nothing.");
				if (choir.ReviveReach <= 0f)
					warnings.Add("Hollow Choir's singer can never stand close enough to a fallen voice to sing it back, so a fallen voice never rises.");
				if (choir.ReviveSeconds <= 0f)
					warnings.Add("Hollow Choir revives instantly, so the window to drop all three at once does not exist.");
				if (choir.ReviveHealthFraction <= 0f || choir.ReviveHealthFraction >= 1f)
					warnings.Add($"Hollow Choir revives at {choir.ReviveHealthFraction} health; at or above 1 the fight cannot end.");
				break;

			case MotherRotBoss rot:
				if (rot.RegenPerSecond <= 0f)
					warnings.Add("Mother Rot does not regenerate, so killing her spawn achieves nothing and the whole decision the fight poses is gone.");
				if (!ResourceLoader.Exists(rot.SpawnScenePath))
					warnings.Add($"Mother Rot spawn scene '{rot.SpawnScenePath}' is missing, so she would never split.");
				if (rot.MaxLiveSpawn < rot.SpawnPerSplit)
					warnings.Add($"Mother Rot caps live spawn at {rot.MaxLiveSpawn} but sheds {rot.SpawnPerSplit} at a time, so a split can silently produce nothing.");
				break;

			case ArchivistBoss archivist:
				if (archivist.SweepDegreesPerSecond <= 0f)
					warnings.Add("The Archivist beam does not turn, so a stationary boss threatens exactly one spot on the floor.");
				if (archivist.SweepHalfAngle <= 0f || archivist.SweepHalfAngle >= 90f)
					warnings.Add($"The Archivist beam half-angle is {archivist.SweepHalfAngle} degrees; outside 0-90 it is either nothing or a half-plane.");
				if (archivist.ResurfaceTellSeconds >= archivist.BurrowTravelSeconds)
					warnings.Add($"The Archivist surfaces in {archivist.BurrowTravelSeconds}s but marks the ground for {archivist.ResurfaceTellSeconds}s, so it arrives before its own warning resolves.");
				// It is always winding up - that is how the beam stays on screen - so an interval
				// at or above the wind-up turns a continuous sweep into a strobe.
				if (probe.SlamIntervalSeconds >= probe.SlamTelegraphSeconds)
					warnings.Add($"The Archivist interval ({probe.SlamIntervalSeconds}s) is not below its wind-up ({probe.SlamTelegraphSeconds}s), so its beam blinks off between sweeps instead of turning continuously.");
				break;

			case StillWardenBoss warden:
				// The whiteout's ring has to lie outside the claim, or it falls on the ground the fight
				// tells the player to hold.
				if (warden.WhiteoutTellSeconds <= 0f || warden.WhiteoutRadius - warden.IceShardRadius <= warden.GuardPostRadius * 0.6f)
					warnings.Add($"The Still Warden's whiteout ring ({warden.WhiteoutRadius} px, {warden.WhiteoutTellSeconds}s) falls on its own claim or unseen.");
				if (warden.ShardsPerVolley <= 0 || warden.IceVolleyIntervalSeconds <= 0f)
					warnings.Add("The Still Warden drops no ice, so a boss that cannot chase has no pressure at all.");
				if (warden.IceScatterRadius <= warden.IceShardRadius)
					warnings.Add($"The Still Warden scatters ice within {warden.IceScatterRadius} using a {warden.IceShardRadius} radius, so every volley covers the player and none of it is dodgeable.");
				if (!ResourceLoader.Exists(warden.GuardScenePath))
					warnings.Add($"The Still Warden guard scene '{warden.GuardScenePath}' is missing, so it stands unguarded.");
				break;

			case LongCoilBoss coil:
				if (coil.SprayTellSeconds <= 0f)
					warnings.Add("The Long Coil's sand spray is marked for no time, so it lands unseen.");
				if (coil.SurfacedSeconds <= 0f)
					warnings.Add("The Long Coil is never above ground, so it can never be damaged and the fight cannot end.");
				if (coil.ResurfaceTellSeconds >= coil.DiveTravelSeconds)
					warnings.Add($"The Long Coil surfaces in {coil.DiveTravelSeconds}s but marks the ground for {coil.ResurfaceTellSeconds}s, so it arrives before its own warning resolves.");
				if (coil.BodySegments < 2)
					warnings.Add($"The Long Coil has {coil.BodySegments} body segments, so the one boss built around having a body behind it does not have one.");
				break;

			case DeepWardenBoss deep:
				if (deep.ChargeTellFraction <= 0f || deep.ChargeTellFraction >= 1f)
					warnings.Add($"The Warden of the Deep has ChargeTellFraction {deep.ChargeTellFraction}; at 0 it charges with no tell and at 1 it never charges.");
				if (deep.SweepDegreesPerSecond <= 0f)
					warnings.Add("The Warden of the Deep sweep does not turn, so its second phase is a fixed wedge.");
				if (deep.SurfacedSeconds <= 0f)
					warnings.Add("The Warden of the Deep is never above ground in its third phase, so the fight stalls there.");
				break;
		}
	}

	// Actually builds the boss once. A .tscn that exists but has the wrong script - or no script -
	// still passes ResourceLoader.Exists and then fails at minute fifteen, where nobody is watching.
	// Instantiate does not run _Ready (that needs the tree), so this is a cheap structural probe.
	private static void ValidateBossScene(List<string> warnings, BossDefinition boss)
	{
		// The scene a run spawns: the boss's own, or its stand-in while it is still being built.
		string path = boss.SpawnScenePath;
		if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
		{
			warnings.Add($"Boss '{boss.Id}' has a missing scene: '{path}'.");
			return;
		}

		var scene = GD.Load<PackedScene>(path);
		if (scene == null)
		{
			warnings.Add($"Boss '{boss.Id}' scene '{path}' failed to load.");
			return;
		}

		Node probe = scene.Instantiate();
		if (probe is not BossEnemy)
			warnings.Add($"Boss '{boss.Id}' scene '{path}' does not have a BossEnemy script attached, so the run could never be won.");

		probe?.Free();
	}

	// The smallest RunResult that can satisfy a boss achievement: a victory carrying that boss id.
	// Deliberately leaves every other field at its default so only the boss clause can match.
	private static AchievementContext BuildVictoryProbe(string bossId)
	{
		var run = new global::RunResult
		{
			Outcome = global::RunOutcome.Victory,
			BossId = bossId,
			StageId = "stage_0",
		};

		var save = new SaveData();
		save.Lifetime.Absorb(run);
		return AchievementContext.ForRun(save, run);
	}

	// The Full Set Enchantment screen renders each set from its icon and its Effects list, so a set that ships
	// with neither is invisible or blank to the player. This cannot check that the numbers still
	// agree with Player.RefreshChestSetEffects - that switch is imperative - but it does catch a
	// new set being added without its presentation data filled in.
	// Issue #63: every Full Set Enchantment is the pieces of one object, and a piece may belong to
	// more than one (The Crown is in the Jewelled Crown and the Regalia). What that needs checked:
	// every piece is a relic that can actually drop, nothing is listed twice in one set, and a set's
	// partial threshold is below its size - a "two-piece taste" on a two-piece set would be paid at the
	// same moment as the full effect and mean nothing. Nested sets can never block each other here
	// because relics are never consumed: holding The Crown for one set is holding it for the other.
	//
	// A RELIC MAY STAND ALONE (Eric, 2026-11-02: "Every relic does not need to belong to a set";
	// .ai/new-items.md, answer 1). This used to warn on any relic that was a piece of no set. The rule
	// that still holds runs the other way - a set's pieces are the parts of one object, each named as
	// its part, so two of them tell you the third - and it lives in the names, which no check can read.
	private static void ValidateFullSetObjects(List<string> warnings)
	{
		var pool = new HashSet<string>(ChestItemCatalog.AllItemIds, StringComparer.OrdinalIgnoreCase);
		foreach (var set in ChestItemCatalog.Sets)
		{
			if (set.RequiredItemIds == null || set.RequiredItemIds.Length < 2)
			{
				warnings.Add($"Full Set Enchantment '{set.Id}' has fewer than two pieces; an object that cannot come apart is not a set.");
				continue;
			}
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string id in set.RequiredItemIds)
			{
				if (!seen.Add(id))
					warnings.Add($"Full Set Enchantment '{set.Id}' lists '{id}' twice.");
				if (!pool.Contains(id))
					warnings.Add($"Full Set Enchantment '{set.Id}' needs '{id}', which is not in ChestItemCatalog.AllItemIds, so it can never drop and the set can never complete.");
			}
			if (set.PartialItemCount > 0 && set.PartialItemCount >= set.RequiredItemIds.Length)
				warnings.Add($"Full Set Enchantment '{set.Id}' pays its partial at {set.PartialItemCount} of {set.RequiredItemIds.Length} pieces, the same moment as the full effect.");
		}
		// The tome relics keep the opposite rule - they must be in NO set (ChestItemCatalog.TomeRelicIds
		// says why) - and ValidateTomeRelics checks it.
	}

	// The two relics that act on the spell book (.ai/passives-and-items.md, section 4b). Spells are
	// permanent once taken, so these are the only way the book changes, and each rule below is one a
	// build cannot see: a set that needed the Folio would make a set chase a hunt for the rarest
	// relic; a tag on either would double-dip the element budget a seventh slot already is; and a
	// Marginalia id that collided with a spell or boon would route a real pick to the fallback card.
	private static void ValidateTomeRelics(List<string> warnings)
	{
		foreach (string id in ChestItemCatalog.TomeRelicIds)
		{
			if (!ChestItemCatalog.AllItemIds.Contains(id, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"Tome relic '{id}' is not in ChestItemCatalog.AllItemIds, so no chest can ever offer it.");
			if (ChestItemCatalog.GetElementTags(id).Count > 0)
				warnings.Add($"Tome relic '{id}' carries an element tag; a tome relic changes the book, and the book's spells carry the tags.");
			if (ChestItemCatalog.GetAssociatedSets(id).Count > 0)
				warnings.Add($"Tome relic '{id}' is a piece of a Full Set Enchantment; tome relics are one per run and must complete nothing.");
			if (ChestItemCatalog.GetDescription(id) == ChestItemCatalog.GetDescription("__no_such_relic__"))
				warnings.Add($"Tome relic '{id}' has no description of its own.");
		}
		if (ChestItemCatalog.GetRarity(ChestItemCatalog.BlankFolio) != ChestItemRarity.Relic)
			warnings.Add("The Blank Folio is not Relic rarity; a seventh spell slot belongs in the rarest tier.");

		string marginalia = Player.MarginaliaId;
		if (BoonCatalog.IsBoon(marginalia) || UnlockCatalog.AllSpellIds.Contains(marginalia, StringComparer.OrdinalIgnoreCase))
			warnings.Add($"'{marginalia}' is both the Marginalia fallback card and a spell or boon id; Node2DGame would route the real pick to the fallback.");
		if (!ResourceLoader.Exists("res://assets/bonelight/ui/icons/marginalia.png"))
			warnings.Add("The Marginalia card has no icon (res://assets/bonelight/ui/icons/marginalia.png); run python tools/art/relic_icons.py and import.");
	}

	// The collector economy (.ai/passives-and-items.md, section 8). Each rule here fails quietly in
	// play: a drop table whose chances sum past 1 at full Luck silently stops dropping the last item
	// in the roll; a sixth relic page overruns a chest screen laid out and measured for five; and a
	// schema-12 migration that lost its credit would close the Emberdeep on a player who had opened it.
	private static void ValidateCollectorEconomy(List<string> warnings)
	{
		const int ChestPagesLaidOut = 5;
		int mostOffers = DropTable.BaseChestOffers + 1 // the Relic Key
			+ CharacterRoster.GetAll().Count(c => c != null && "chest_options_1".Equals(c.StartingPassiveId?.Trim(), StringComparison.OrdinalIgnoreCase));
		if (mostOffers > ChestPagesLaidOut)
			warnings.Add($"A chest can offer {mostOffers} relics, but the chest screen is laid out and measured for {ChestPagesLaidOut}.");

		// Read into locals: compared as constants, a rule that holds today is "unreachable code".
		float orb = DropTable.BonusOrbChance, flask = DropTable.HealthFlaskChance, potion = DropTable.SpeedPotionChance;
		float sum = (orb + flask + potion) * (1f + DropTable.LuckScale);
		if (sum >= 1f || orb <= 0f || flask <= 0f || potion <= 0f)
			warnings.Add($"DropTable's bonus chances are out of range: {sum:0.000} a kill at full Luck, and each must be above 0.");
		float elite = DropTable.EliteChestChance, nested = DropTable.NestedChestChance;
		if (elite <= 0f || elite > 1f || nested >= 1f)
			warnings.Add("DropTable's chest chances are out of range: an elite's must be in (0, 1], and a nested chest's below 1 or the Hidden Drawer never stops.");
		int bigger = DropTable.GreaterOrbValue;
		if (bigger <= 2)
			warnings.Add($"A bigger XP orb is worth {bigger}, no more than the ordinary orb it replaces.");
		// The Gleaning Bell is a find, one or two a run (.ai/passives-and-items.md 8i): the first carrier's
		// chance must be a real chance and not a certainty, the climb must climb, and the spacing must hold.
		float bell = DropTable.GleaningBellBaseChance, bellStep = DropTable.GleaningBellChanceStep;
		float bellApart = DropTable.GleaningBellMinSecondsApart, bellToss = DropTable.GleaningBellTossSeconds;
		if (bell <= 0f || bell >= 0.25f || bellStep <= 0f || bellStep >= 0.1f || bellApart <= 0f || bellToss <= 0f)
			warnings.Add($"DropTable's Gleaning Bell numbers are out of range: a first chance of {bell:0.000} (want (0, 0.25)), "
				+ $"a step of {bellStep:0.000} (want (0, 0.1)), {bellApart:0} s apart and a {bellToss:0.00} s toss (both above 0).");

		// Schema 12: a save that had everything at 11 is credited with John Dee; one that did not is not.
		var complete = new SaveData { SchemaVersion = 11 };
		foreach (string id in UnlockCatalog.AllSpellIds)
			complete.UnlockedSpellIds.Add(id);
		foreach (string id in UnlockCatalog.AllWizardIds.Where(id => !id.Equals("john_dee", StringComparison.OrdinalIgnoreCase)))
			complete.UnlockedCharacterIds.Add(id);
		complete.Migrate();
		// The gate chapter, not the last: the Emberdeep was the finale these saves had met, and the last
		// chapter is the castle's Throne now - not playable yet, which would turn this check off quietly.
		StageDefinition finale = StageCatalog.GateChapter;
		if (!GlobalStatsManager.IsCharacterUnlocked(complete, "john_dee")
			|| (finale != null && finale.IsPlayable && !GlobalStatsManager.IsStageAvailable(complete, finale)))
			warnings.Add("A save that had freed every wizard and recovered every spell at schema 11 loses the Emberdeep: SaveData.MigrateCollectorIntoCompletedCampaign did not credit John Dee, or schema 19 did not keep the gate open.");
		var partial = new SaveData { SchemaVersion = 11 };
		partial.Migrate();
		if (GlobalStatsManager.IsCharacterUnlocked(partial, "john_dee"))
			warnings.Add("A schema-11 save that had not met the campaign gate was handed John Dee by the schema-12 migration; he is earned by opening chests.");
	}

	private static void ValidateChestSetPresentation(List<string> warnings)
	{
		foreach (var set in ChestItemCatalog.Sets)
		{
			if (set.Effects == null || set.Effects.Length == 0)
				warnings.Add($"Chest set '{set.Id}' declares no Effects, so the Full Set Enchantment screen has nothing to list.");

			if (string.IsNullOrWhiteSpace(set.IconPath) || !ResourceLoader.Exists(set.IconPath))
				warnings.Add($"Chest set '{set.Id}' has a missing icon: '{set.IconPath}'.");

			if (set.RequiredItemIds == null || set.RequiredItemIds.Length == 0)
				warnings.Add($"Chest set '{set.Id}' requires no items, so it completes immediately.");
		}

		ValidateChestItemIcons(warnings);
	}

	// Relics are told apart by their icon in the chest menu, the HUD strip and the Full Set Enchantment screen,
	// so two relics sharing one image is a real readability bug rather than a cosmetic one. Three
	// pairs used to collide; this keeps them from drifting back together.
	private static void ValidateChestItemIcons(List<string> warnings)
	{
		var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (string itemId in ChestItemCatalog.AllItemIds)
		{
			string icon = ChestItemCatalog.GetIconPath(itemId);
			if (string.IsNullOrWhiteSpace(icon) || !ResourceLoader.Exists(icon))
			{
				warnings.Add($"Chest item '{itemId}' has a missing icon: '{icon}'.");
				continue;
			}

			if (seen.TryGetValue(icon, out string owner))
				warnings.Add($"Chest items '{owner}' and '{itemId}' share the icon '{icon}'.");
			else
				seen[icon] = itemId;
		}
	}

	// The Apotheosis (issue #60; the gate Eric set on 2026-11-02 - max level, ascended, not combined). Every
	// one of these produces a gate that can never open, a form handed to a spell it must not reach, or a
	// reward that names a third element, and none of them is visible to dotnet build: one verb per form,
	// the wizard's OWN spell, a gate that opens down EVERY Ascension (no path is named, so none may be
	// required by accident) and stays shut short of max level or the Ascension, and a fused spell or a
	// Conjunction's own never offered a form.
	private static void ValidateApotheosisCatalog(List<string> warnings)
	{
		var spells = ContentValidator.LoadAllSpells();
		var characters = CharacterRoster.GetAll();
		var seenCharacters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var seenVerbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (ApotheosisDefinition definition in ApotheosisCatalog.All)
		{
			string label = $"Apotheosis '{definition.Form?.Id}'";
			if (definition.Form == null || definition.Form.MilestoneLevel != ApotheosisCatalog.MilestoneLevel)
				warnings.Add($"{label} has no form, or a form whose MilestoneLevel is not {ApotheosisCatalog.MilestoneLevel}, so SpellData.ApplyEvolution would refuse it.");

			if (definition.CharacterId.Equals("test_wizard", StringComparison.OrdinalIgnoreCase))
				warnings.Add($"{label} is given to the Test Wizard, which picks its own loadout and is exempt from the covenant.");
			// One per wizard - or, for a wizard whose starting spell is taken in a form, one per form.
			if (!seenCharacters.Add($"{definition.CharacterId}:{definition.VariantId}"))
				warnings.Add($"{label}: character '{definition.CharacterId}' has more than one Apotheosis{(string.IsNullOrEmpty(definition.VariantId) ? "" : $" for '{definition.VariantId}'")}.");
			if (!seenVerbs.Add(definition.Verb))
				warnings.Add($"{label}: verb '{definition.Verb}' is already claimed. Each Apotheosis must own a verb no other does (#59).");

			CharacterData character = characters.FirstOrDefault(c => c.Id.Equals(definition.CharacterId, StringComparison.OrdinalIgnoreCase));
			if (character == null)
			{
				warnings.Add($"{label} names character '{definition.CharacterId}', which is not in CharacterRoster.");
				continue;
			}
			if (character.StartingSpellResource == null
				|| !character.StartingSpellResource.Id.Equals(definition.SpellId, StringComparison.OrdinalIgnoreCase))
			{
				warnings.Add($"{label} transforms '{definition.SpellId}', but {character.Id} starts with '{character.StartingSpellResource?.Id}'. An Apotheosis is the final form of the wizard's OWN starting spell.");
			}

			SpellData template = spells.FirstOrDefault(s => s.Id.Equals(definition.SpellId, StringComparison.OrdinalIgnoreCase));
			if (template == null)
			{
				warnings.Add($"{label} names spell '{definition.SpellId}', which is not in ContentValidator.SpellResourcePaths.");
				continue;
			}

			// A spell taken in a form: the Apotheosis is one form's, and its gate is checked against that form's own
			// milestones (SpellData.Variants; ApotheosisDefinition.VariantId).
			SpellData spell;
			if (string.IsNullOrEmpty(definition.VariantId))
			{
				if (template.HasVariants)
					warnings.Add($"{label} transforms '{definition.SpellId}', which is taken in a form; it must name the form it ends (VariantId).");
				spell = (SpellData)template.Duplicate(true);
				SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			}
			else
			{
				SpellVariant form = template.FindVariant(definition.VariantId);
				spell = form == null ? null : SpellVariants.Preview(template, form);
				if (spell == null)
				{
					warnings.Add($"{label} names form '{definition.VariantId}', which '{definition.SpellId}' does not have.");
					continue;
				}
			}
			string element = definition.Element.ToString();
			if (!spell.ElementWeights.ContainsKey(element))
				warnings.Add($"{label} deepens {element}, which '{spell.Id}' does not carry.");

			if (definition.Form == null)
				continue;

			// Preserve or increase, never reduce, and never a new element: the form may only add
			// weight to the wizard's own element.
			foreach (var pair in definition.Form.BonusElementWeights)
			{
				if (!pair.Key.Equals(element, StringComparison.OrdinalIgnoreCase))
					warnings.Add($"{label} adds {pair.Key}; a form may only deepen {element}, never name another element.");
				if (pair.Value < 0)
					warnings.Add($"{label} REDUCES {pair.Key}, which would drop the player out of a tier.");
			}

			// Any route through the tree now ends in the form, so the two-element ceiling is checked over
			// every level 4 x level 8 pairing with the form on top - and a branch that trades the wizard's
			// element away would have the form put it back as a third.
			var level4 = spell.Level4Options?.Where(o => o != null).ToList() ?? new List<SpellEvolutionOption>();
			var level8 = spell.Level8Options?.Where(o => o != null).ToList() ?? new List<SpellEvolutionOption>();
			// A Conjunction its wizard opens on takes no Ascension: its gate is its max level alone, held by
			// ValidateStartingConjunction, and none of the path checks below has a path to walk.
			bool opening = ApotheosisCatalog.OnStartingConjunction(definition);
			if (level8.Count == 0 && !opening)
				warnings.Add($"{label}: '{spell.Id}' has no level 8 Ascension, so it can never be ascended and the form never offered.");
			foreach (SpellEvolutionOption option in level4.Concat(level8))
			{
				if (element.Equals(option.ReplacesElement ?? string.Empty, StringComparison.OrdinalIgnoreCase))
					warnings.Add($"{label}: '{option.Id}' trades away {element}, the element the form deepens; every path leads to the form now, so none may.");
			}
			foreach (SpellEvolutionOption early in level4.DefaultIfEmpty())
			{
				foreach (SpellEvolutionOption late in level8)
				{
					var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
					foreach (var pair in spell.ElementWeights)
						totals[pair.Key] = pair.Value;
					foreach (SpellEvolutionOption step in new[] { early, late, definition.Form })
					{
						if (step == null)
							continue;
						if (!string.IsNullOrEmpty(step.ReplacesElement))
							totals.Remove(step.ReplacesElement);
						foreach (var pair in step.BonusElementWeights)
							totals[pair.Key] = (totals.TryGetValue(pair.Key, out int had) ? had : 0) + pair.Value;
					}
					if (totals.Count > 2)
						warnings.Add($"{label} ends up naming {totals.Count} elements via '{early?.Id}' + '{late.Id}' ({string.Join(", ", totals.Keys)}); no spell may ever name more than two.");
				}
			}

			// The gate itself, on a copy at every Ascension: open at max level down any of them, shut a level
			// short, shut without one, shut once fused.
			foreach (SpellEvolutionOption late in level8)
			{
				var ascended = (SpellData)spell.Duplicate(true);
				ascended.CurrentLevel = ascended.MaxLevel;
				if (level4.Count > 0)
					ascended.ApplyEvolution(level4[0]);
				ascended.ApplyEvolution(late);
				if (!ApotheosisCatalog.SpellMeetsGate(definition, ascended, fusedAway: false))
					warnings.Add($"{label}: the gate stays shut at level {ascended.MaxLevel} down '{late.Id}'; it must open down every Ascension.");
				if (ApotheosisCatalog.SpellMeetsGate(definition, ascended, fusedAway: true))
					warnings.Add($"{label}: the gate opens on a spell that went into a Conjunction; a combined spell is never offered its form.");
				ascended.CurrentLevel = ascended.MaxLevel - 1;
				if (ApotheosisCatalog.SpellMeetsGate(definition, ascended, fusedAway: false))
					warnings.Add($"{label}: the gate opens below max level ({ascended.CurrentLevel}/{ascended.MaxLevel}).");
			}
			var unascended = (SpellData)spell.Duplicate(true);
			unascended.CurrentLevel = unascended.MaxLevel;
			if (!opening && ApotheosisCatalog.SpellMeetsGate(definition, unascended, fusedAway: false))
				warnings.Add($"{label}: the gate opens at max level with no Ascension taken.");

			// Not combined, the structural half: a fusion never takes a wizard's own spell, so his is never
			// fused in the first place (ComboCatalog.Evaluate's ComboSeal.OwnSpell). Both parents held at level
			// 8 down the named Ascensions, to show the seal is what stops it rather than an unmet parent.
			foreach (ComboDefinition recipe in ComboCatalog.All.Where(r => !r.IsRite && r.Ingredients.Any(x =>
				x != null && x.SpellId.Equals(definition.SpellId, StringComparison.OrdinalIgnoreCase))))
			{
				var loadout = recipe.Ingredients.Select(x => new SpellData
				{
					Id = x.SpellId, Name = x.SpellName, SelectedVariantId = x.VariantId,
					MaxLevel = 8, CurrentLevel = 8, SelectedLevel8EvolutionId = x.AscensionId,
				}).ToList();
				ComboStanding standing = ComboCatalog.Evaluate(recipe, loadout, definition.SpellId);
				if (standing.Ready || standing.Seal != ComboSeal.OwnSpell)
					warnings.Add($"{label}: Conjunction '{recipe.Id}' could fuse {definition.CharacterId}'s own '{definition.SpellId}'; his spell must never be combined, or he loses his form (ComboCatalog.Evaluate, ComboSeal.OwnSpell).");
			}
		}

		// A Conjunction's own spell is never anyone's to transform: no form names one, and the gate refuses it -
		// except the one a wizard opens on (ComboDefinition.StartsFusedFor), whose form is his alone and whose gate
		// ValidateStartingConjunction holds.
		foreach (ComboDefinition recipe in ComboCatalog.All.Where(r => !r.IsRite))
		{
			if (ApotheosisCatalog.All.Any(d => d.SpellId.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase)
				&& !ComboCatalog.StartsFused(recipe, d.CharacterId)))
				warnings.Add($"Apotheosis: an entry transforms the Conjunction '{recipe.Id}'; a combined spell is never offered a form.");
			if (ComboCatalog.Template(recipe) is not SpellData template)
				continue;
			var made = (SpellData)template.Duplicate(true);
			made.CurrentLevel = made.MaxLevel;
			made.SelectedLevel8EvolutionId = made.Level8Options?.FirstOrDefault()?.Id ?? "any";
			foreach (ApotheosisDefinition definition in ApotheosisCatalog.All)
			{
				if (!ApotheosisCatalog.OnStartingConjunction(definition) && ApotheosisCatalog.SpellMeetsGate(definition, made, fusedAway: false))
					warnings.Add($"Apotheosis '{definition.Form?.Id}': the gate opens on the Conjunction '{recipe.Id}'.");
			}
			if (!ApotheosisCatalog.IsCombined(made))
				warnings.Add($"Apotheosis: '{recipe.Id}' is a Conjunction but ApotheosisCatalog.IsCombined does not say so.");
		}
	}

	// THE COMBINATIONS (.ai/combo-spells.md section 2, ComboCatalog): what `dotnet build` cannot see. Both
	// parents exist and are distinct; each names exactly one Ascension, and that id is one of the parent's level
	// 8 options (for a spell taken in forms, the named form's); no two recipes share a pair and both Ascensions;
	// no recipe names more than three elements with its named Ascensions; every verb is unique across the
	// combinations and the Apotheoses; and every combination has its derived Feat, its UnlockCatalog entry from
	// that Feat, its ContentValidator.SpellResourcePaths entry and an EvolutionStatReaders row. Its own resource
	// is terminal (one level, no level-ups, no milestones), and the names the recipe writes out for the Feat
	// are the resources' own.
	// THE EVOLUTION CODEX (.ai/evolution-codex.md section 8). Built from the catalogs and never hardcoded, so
	// what can go wrong is a catalog outgrowing it: every spell, relic, boon, combination and set must be in it
	// once; every tree must fit the detail panel by construction (section 6: no zoom and pan, so a spell that
	// outgrows the bound fails here rather than clipping); every name the search can find must lead to an entry
	// and a node in it; and on a fresh save nothing sealed may be findable (a search must not name a secret).
	private static void ValidateCodex(List<string> warnings)
	{
		CodexModel fresh = CodexModel.Build(CodexContext.ForSave(new SaveData()));
		var expected = new List<(string Id, CodexKind Kind)>();
		foreach (SpellData spell in ContentValidator.LoadAllSpells())
			expected.Add((spell.Id, ComboCatalog.IsCombination(spell.Id) ? CodexKind.Combination : CodexKind.Spell));
		foreach (string id in ChestItemCatalog.AllItemIds)
			expected.Add((id, CodexKind.Relic));
		foreach (BoonDefinition boon in BoonCatalog.All)
			expected.Add((boon.Id, CodexKind.Boon));
		foreach (ChestSetDefinition set in ChestItemCatalog.Sets)
			expected.Add((set.Id, CodexKind.FullSet));
		// One locked thing on show at a time (Teachings.IsShown, .ai/slow-start.md Q8): a fresh save is shown no
		// combination, so every recipe - the Rite's included - is looked for on a save that has learned the
		// Ascension and the Apotheosis and owns the Rite, where each must be.
		var taught = new SaveData();
		Teachings.Teach(taught, Teachings.Get(Teaching.Ascension));
		Teachings.Teach(taught, Teachings.Get(Teaching.Apotheosis));
		taught.UnlockedSpellIds.Add(SovereignRite.SpellId);
		CodexModel shown = CodexModel.Build(CodexContext.ForSave(taught));
		foreach ((string id, CodexKind kind) in expected)
		{
			CodexModel model = kind == CodexKind.Combination ? shown : fresh;
			if (!model.Entries.Any(e => e.Kind == kind && e.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
				warnings.Add($"Codex: {kind} '{id}' is missing from the Evolution Codex (CodexModel.Build).");
		}
		if (fresh.Entries.Any(e => e.Kind == CodexKind.Combination))
			warnings.Add("Codex: a fresh save is shown a combination; Conjunctions are shown only once the Apotheosis is learned (Teachings.IsShown).");
		foreach (IGrouping<string, CodexEntry> twice in fresh.Entries.GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
			warnings.Add($"Codex: the id '{twice.Key}' names {twice.Count()} entries ({string.Join(", ", twice.Select(e => e.Kind))}); the Codex selects by id, so only the first can be opened.");

		foreach (CodexSearchHit hit in fresh.SearchIndex)
		{
			CodexEntry entry = fresh.Get(hit.EntryId);
			if (entry == null || !entry.Discovered)
				warnings.Add($"Codex: on a fresh save the search finds '{hit.Name}', which is not discovered - a search must not name a secret.");
			else if (FindCodexNode(entry, hit.NodeId) is CodexNode node && !node.Revealed && !node.NameAlwaysShown)
				warnings.Add($"Codex: on a fresh save the search finds '{hit.Name}' in {entry.Id}, a sealed node.");
		}

		// Everything owned, seen and made: every tree at its fullest, every name findable.
		var all = new SaveData();
		foreach (string id in UnlockCatalog.AllSpellIds)
			GlobalStatsManager.UnlockSpell(all, id);
		foreach (string id in UnlockCatalog.AllCharacterIds)
			GlobalStatsManager.UnlockCharacter(all, id);
		foreach (string id in UnlockCatalog.AllBoonIds)
			GlobalStatsManager.UnlockBoon(all, id);
		foreach (string id in ChestItemCatalog.AllItemIds)
			all.MarkItemSeen(id);
		foreach (ComboDefinition recipe in ComboCatalog.All)
			all.UnlockedAchievementIds.Add(ComboCatalog.FeatId(recipe.Id));
		foreach ((SpellData template, List<SpellData> forms) in CodexSpellForms.All())
			foreach (SpellData form in forms)
				foreach (SpellEvolutionOption evo in form.Level8Options ?? new Godot.Collections.Array<SpellEvolutionOption>())
					if (evo != null)
						all.MarkAscensionSeen(template.Id, evo.Id);
		foreach (ApotheosisDefinition apotheosis in ApotheosisCatalog.All)
			all.MarkAscensionSeen(apotheosis.SpellId, apotheosis.Form.Id);
		CodexModel full = CodexModel.Build(CodexContext.ForSave(all));

		foreach (CodexEntry entry in full.Entries)
		{
			string label = $"Codex: {entry.Kind} '{entry.Id}'";
			if (!entry.Discovered && entry.Kind is CodexKind.Spell or CodexKind.Relic or CodexKind.Boon or CodexKind.Combination)
				warnings.Add($"{label} is still undiscovered on a save that owns and has seen everything.");
			foreach (CodexForm form in entry.Forms)
			{
				if (form.Mutations.Count > CodexModel.MaxMutations || form.Ascensions.Count > CodexModel.MaxAscensions || form.Finals.Count > CodexModel.MaxFinals)
					warnings.Add($"{label} ({form.Name}): its tree is {form.Mutations.Count}/{form.Ascensions.Count}/{form.Finals.Count} nodes (level 4/level 8/final), over the detail's bound of {CodexModel.MaxMutations}/{CodexModel.MaxAscensions}/{CodexModel.MaxFinals}. The Codex has no zoom and pan (section 6): widen the bound and photograph both canvases, or trim the tree.");
				if (form.AllNodes.Any(n => n.Revealed && string.IsNullOrWhiteSpace(n.Line)))
					warnings.Add($"{label} ({form.Name}): a revealed node has no line for the reading box.");
			}
			if (entry.UsedIn.Count > CodexModel.MaxUsedIn)
				warnings.Add($"{label} is used in {entry.UsedIn.Count} recipes; its Used in row holds {CodexModel.MaxUsedIn}.");
			if (entry.Recipe != null && entry.Recipe.Ingredients.Count > CodexModel.MaxIngredients)
				warnings.Add($"{label} has {entry.Recipe.Ingredients.Count} ingredients; a recipe card holds {CodexModel.MaxIngredients}.");
		}

		foreach (CodexSearchHit hit in full.SearchIndex)
		{
			CodexEntry entry = full.Get(hit.EntryId);
			if (entry == null)
				warnings.Add($"Codex: the search name '{hit.Name}' leads to no entry ('{hit.EntryId}').");
			else if (!string.IsNullOrEmpty(hit.NodeId) && FindCodexNode(entry, hit.NodeId) == null)
				warnings.Add($"Codex: the search name '{hit.Name}' leads to {entry.Id}, which has no node '{hit.NodeId}'.");
		}
		// Every spell's every Ascension is findable once seen: the index is what the keyboard's suggestions read.
		foreach (CodexEntry spell in full.Entries.Where(e => e.Kind == CodexKind.Spell))
			foreach (CodexNode node in spell.Forms.SelectMany(f => f.Ascensions))
				if (!full.SearchIndex.Any(h => h.EntryId == spell.Id && h.NodeId == node.Id))
					warnings.Add($"Codex: {spell.Id}'s Ascension '{node.Name}' is seen but the search cannot find it.");
	}

	private static CodexNode FindCodexNode(CodexEntry entry, string nodeId)
	{
		if (entry == null || string.IsNullOrEmpty(nodeId))
			return null;
		return entry.Forms.SelectMany(f => f.AllNodes)
			.Concat(entry.Recipe?.Ingredients ?? new List<CodexNode>())
			.FirstOrDefault(n => n.Id.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
	}

	private static void ValidateComboCatalog(List<string> warnings)
	{
		var spells = ContentValidator.LoadAllSpells();
		var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var seenRecipes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var verbs = new HashSet<string>(ApotheosisCatalog.All.Select(a => a.Verb), StringComparer.OrdinalIgnoreCase);
		foreach (ComboDefinition recipe in ComboCatalog.All)
		{
			string label = $"Combination '{recipe.Id}'";
			if (!seenIds.Add(recipe.Id))
				warnings.Add($"{label} is defined twice in ComboCatalog.");
			if (string.IsNullOrWhiteSpace(recipe.Verb) || !verbs.Add(recipe.Verb))
				warnings.Add($"{label}: verb '{recipe.Verb}' is empty or already claimed by an Apotheosis or another combination. Each owns one (.ai/combo-spells.md section 4).");
			if (recipe.First == null || recipe.Second == null)
			{
				warnings.Add($"{label} does not name two parents.");
				continue;
			}
			// The Sovereign Rite fuses nothing and names no Ascension: ValidateSovereignRite holds it.
			if (recipe.IsRite)
				continue;
			if (recipe.First.SpellId.Equals(recipe.Second.SpellId, StringComparison.OrdinalIgnoreCase))
				warnings.Add($"{label} names '{recipe.First.SpellId}' as both parents; a combination is two distinct spells.");

			// The same pair with the same two Ascensions, in either order, is one recipe.
			string a = $"{recipe.First.SpellId}:{recipe.First.VariantId}:{recipe.First.AscensionId}";
			string b = $"{recipe.Second.SpellId}:{recipe.Second.VariantId}:{recipe.Second.AscensionId}";
			string pair = string.CompareOrdinal(a, b) < 0 ? $"{a}+{b}" : $"{b}+{a}";
			if (!seenRecipes.Add(pair))
				warnings.Add($"{label} shares its pair and both named Ascensions with another recipe.");

			var elements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var baseSum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (ComboIngredient ingredient in recipe.Ingredients)
			{
				string part = $"{label}'s parent '{ingredient.SpellId}'";
				if (ComboCatalog.IsCombination(ingredient.SpellId))
					warnings.Add($"{part} is itself a combination; a combination is terminal and never fused again.");
				SpellData template = spells.FirstOrDefault(s => s.Id.Equals(ingredient.SpellId, StringComparison.OrdinalIgnoreCase));
				if (template == null)
				{
					warnings.Add($"{part} is not in ContentValidator.SpellResourcePaths.");
					continue;
				}
				if (!template.Name.Equals(ingredient.SpellName, StringComparison.Ordinal))
					warnings.Add($"{part} is written '{ingredient.SpellName}' in ComboCatalog but its resource names it '{template.Name}'.");
				if (!string.IsNullOrEmpty(ingredient.VariantId) && template.FindVariant(ingredient.VariantId) == null)
					warnings.Add($"{part} names form '{ingredient.VariantId}', which the spell does not have.");
				if (template.HasVariants && string.IsNullOrEmpty(ingredient.VariantId))
					warnings.Add($"{part} is taken in a form; the recipe must name the form whose Ascension it names (VariantId).");

				IReadOnlyList<SpellEvolutionOption> level8 = ComboCatalog.Level8OptionsOf(template, ingredient);
				SpellEvolutionOption named = level8.FirstOrDefault(o => o != null && o.Id.Equals(ingredient.AscensionId, StringComparison.OrdinalIgnoreCase));
				if (string.IsNullOrWhiteSpace(ingredient.AscensionId) || named == null)
				{
					warnings.Add($"{part} names Ascension '{ingredient.AscensionId}', which is not one of its level 8 options ({string.Join(", ", level8.Select(o => o.Id))}). A recipe names exactly one, never any.");
					continue;
				}
				if (named.MilestoneLevel != Player.AscensionSpellLevel)
					warnings.Add($"{part}'s named Ascension '{named.Id}' is a milestone {named.MilestoneLevel}, not the level 8 one.");
				if (!named.DisplayName.Equals(ingredient.AscensionName, StringComparison.Ordinal))
					warnings.Add($"{part}'s Ascension is written '{ingredient.AscensionName}' in ComboCatalog but is called '{named.DisplayName}'.");
				foreach (var weight in template.ElementWeights)
				{
					elements.Add(weight.Key);
					baseSum[weight.Key] = (baseSum.TryGetValue(weight.Key, out int had) ? had : 0) + weight.Value;
				}
				foreach (var weight in named.BonusElementWeights)
					elements.Add(weight.Key);
			}
			if (elements.Count > ComboCatalog.MaxElements)
				warnings.Add($"{label} names {elements.Count} elements with its named Ascensions ({string.Join(", ", elements)}); a combination may name {ComboCatalog.MaxElements}, never four.");

			// Its own spell: registered, terminal, named as the recipe says, carrying the parents' tags summed.
			SpellData combo = spells.FirstOrDefault(s => s.Id.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase));
			if (combo == null)
			{
				warnings.Add($"{label} has no SpellData in ContentValidator.SpellResourcePaths ({recipe.ResourcePath}).");
			}
			else
			{
				if (!combo.ResourcePath.Equals(recipe.ResourcePath, StringComparison.OrdinalIgnoreCase))
					warnings.Add($"{label}'s resource is {combo.ResourcePath}, but ComboCatalog names {recipe.ResourcePath}.");
				if (!combo.Name.Equals(recipe.Name, StringComparison.Ordinal))
					warnings.Add($"{label} is written '{recipe.Name}' in ComboCatalog but its resource names it '{combo.Name}'.");
				// A Conjunction a wizard opens on (ComboDefinition.StartsFusedFor) levels for him, so it has a level
				// table: to the Ascension level, a line at every level, every number read (ValidateLevelUpReaders).
				// Fused by anyone else it arrives whole at that level, still with nothing to take (Player.TryFuse).
				if (!string.IsNullOrEmpty(recipe.StartsFusedFor))
					ValidateStartingConjunction(recipe, combo, label, warnings);
				else if (combo.MaxLevel != 1 || (combo.LevelUpgrades?.Count ?? 0) > 0)
					warnings.Add($"{label} has MaxLevel {combo.MaxLevel} and {combo.LevelUpgrades?.Count ?? 0} level-ups; a combination is terminal - one level, none to take.");
				SpellData probe = (SpellData)combo.Duplicate(true);
				SpellEvolutionCatalog.EnsureEvolutionCoverage(probe);
				if ((probe.Level4Options?.Count ?? 0) > 0 || (probe.Level8Options?.Count ?? 0) > 0)
					warnings.Add($"{label} has milestone options; a combination never mutates or ascends again.");
				bool sameTags = combo.ElementWeights.Count == baseSum.Count
					&& baseSum.All(p => combo.ElementWeights.TryGetValue(p.Key, out int w) && w == p.Value);
				if (!sameTags)
					warnings.Add($"{label}'s resource tags ({string.Join(", ", combo.ElementWeights.Select(p => $"{p.Key} {p.Value}"))}) are not its parents' base tags summed ({string.Join(", ", baseSum.Select(p => $"{p.Key} {p.Value}"))}).");
			}
			if (!EvolutionStatReaders.HasRow(recipe.Id))
				warnings.Add($"{label} has no EvolutionStatReaders row.");

			// Its Feat, and the unlock it grants.
			AchievementDefinition feat = AchievementDefinitions.GetById(ComboCatalog.FeatId(recipe.Id));
			if (feat == null)
				warnings.Add($"{label} has no derived Feat '{ComboCatalog.FeatId(recipe.Id)}'.");
			else if (!feat.SpellUnlockId.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase) || !feat.CompletesInRun
				|| feat.Visibility != AchievementVisibility.Recipe)
				warnings.Add($"{label}'s Feat must grant it, complete in the run and be listed as a recipe.");
			UnlockDefinition unlock = UnlockCatalog.GetSpell(recipe.Id);
			if (unlock == null || unlock.Source != UnlockSource.Achievement
				|| !unlock.SourceId.Equals(ComboCatalog.FeatId(recipe.Id), StringComparison.OrdinalIgnoreCase))
				warnings.Add($"{label} is not in UnlockCatalog as the reward of its Feat '{ComboCatalog.FeatId(recipe.Id)}'.");
		}

		// Every combination resource is in a recipe: one with none could never be made.
		foreach (SpellData spell in spells)
		{
			if (UnlockCatalog.GetSpell(spell.Id)?.SourceId?.StartsWith(ComboCatalog.FeatPrefix, StringComparison.OrdinalIgnoreCase) == true
				&& ComboCatalog.ForId(spell.Id) == null)
				warnings.Add($"Spell '{spell.Id}' is unlocked by a Conjunction Feat but has no recipe in ComboCatalog.");
		}
	}

	// THE CONJUNCTION A WIZARD OPENS ON (ComboDefinition.StartsFusedFor; the dark wizard's The Taking, Eric
	// 2026-11-02): the wizard exists and opens on it; it levels 1 to the Ascension level on a line at every level
	// whose numbers its script reads; and his form names it and opens at its max level alone - shut a level short,
	// shut on a copy anyone else fused (it is no starting spell of theirs).
	private static void ValidateStartingConjunction(ComboDefinition recipe, SpellData combo, string label, List<string> warnings)
	{
		CharacterData wizard = CharacterRoster.GetAll().FirstOrDefault(c => c.Id.Equals(recipe.StartsFusedFor, StringComparison.OrdinalIgnoreCase));
		if (wizard == null || wizard.StartingSpellResource == null
			|| !wizard.StartingSpellResource.Id.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase))
			warnings.Add($"{label} is opened on by '{recipe.StartsFusedFor}', who does not start with it.");
		if (combo.MaxLevel != Player.AscensionSpellLevel)
			warnings.Add($"{label} levels to {combo.MaxLevel}; a Conjunction a wizard opens on levels to {Player.AscensionSpellLevel}, as every spell does.");
		var levels = new HashSet<int>((combo.LevelUpgrades ?? new Godot.Collections.Array<SpellLevelUpgrade>()).Where(u => u != null).Select(u => u.Level));
		for (int level = 2; level <= combo.MaxLevel; level++)
		{
			if (!levels.Contains(level))
				warnings.Add($"{label} has no level-up line for level {level}; a card that changes nothing is a dead pick.");
		}
		if (Player.CardLevelCap(combo) != combo.MaxLevel)
			warnings.Add($"{label} stops at level {Player.CardLevelCap(combo)} on cards; with no Ascension of its own, the cards must take it to {combo.MaxLevel}.");

		ApotheosisDefinition form = ApotheosisCatalog.All.FirstOrDefault(d => d.SpellId.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase)
			&& d.CharacterId.Equals(recipe.StartsFusedFor, StringComparison.OrdinalIgnoreCase));
		if (form == null)
			return;
		var mine = (SpellData)combo.Duplicate(true);
		mine.CurrentLevel = mine.MaxLevel;
		if (!ApotheosisCatalog.SpellMeetsGate(form, mine, fusedAway: false))
			warnings.Add($"{label}: {recipe.StartsFusedFor}'s form stays shut on it at level {mine.MaxLevel}; his gate is its max level alone.");
		mine.CurrentLevel = mine.MaxLevel - 1;
		if (ApotheosisCatalog.SpellMeetsGate(form, mine, fusedAway: false))
			warnings.Add($"{label}: {recipe.StartsFusedFor}'s form opens below its max level.");
		if (ApotheosisCatalog.ForSpell("pyromancer", combo) != null)
			warnings.Add($"{label}: another wizard who fused it would be offered {recipe.StartsFusedFor}'s form.");
	}

	// THE SOVEREIGN RITE (.ai/sovereign-spell.md, SovereignRite, Player.Sovereign.cs): what a build cannot see.
	//   - the gate and the per-fuel numbers are the doc's (sections 3 and 4), and the gate can be met at all;
	//   - it is the one rite in ComboCatalog, its second ingredient any active spell, its resource registered,
	//     terminal and elementless, and it is paid by the Warden of the Deep's Road deed - the end of the road,
	//     so nothing on a new save reaches it early - and never counted by The Grimoire Restored;
	//   - a passive (and the defensive spells) is never silenced, never fuel, never anointed;
	//   - the fuel reaches the spell through the one path every milestone does (SpellData.CollectActive): a
	//     spell's own readers move with the boon and come back without it;
	//   - the boon carries damage, cooldown, area or scale and amount and nothing else, so no effect, rider,
	//     slow, knockback or execute reaches a boss through it.
	private static void ValidateSovereignRite(List<string> warnings)
	{
		const string label = "Sovereign Rite";
		if (SovereignRite.MinimumPlayerLevel != 28 || SovereignRite.MinimumActiveSpells != 3 || SovereignRite.MinimumFuel != 16)
			warnings.Add($"{label}: the gate is level {SovereignRite.MinimumPlayerLevel}, {SovereignRite.MinimumActiveSpells} active spells and {SovereignRite.MinimumFuel} fuel; .ai/sovereign-spell.md (sections 3 and 8, as tuned) says 28, 3 and 16. Change the doc with it, or put it back.");
		if (!Mathf.IsEqualApprox(SovereignRite.DamagePerFuel, 0.005f) || !Mathf.IsEqualApprox(SovereignRite.CooldownPerFuel, 0.008f)
			|| !Mathf.IsEqualApprox(SovereignRite.CooldownFloor, 0.70f) || !Mathf.IsEqualApprox(SovereignRite.AreaPerFuel, 0.01f)
			|| !Mathf.IsEqualApprox(SovereignRite.AreaCap, 1.40f) || SovereignRite.FuelPerAmount != 20)
			warnings.Add($"{label}: the power per level of fuel is not section 4's as tuned (+0.5% damage, -0.8% cooldown to -30%, +1% area to +40%, +1 amount per 20).");
		// The gate can be met: the fuel behind one spell is the others' levels, at most eight each.
		if (SovereignRite.MinimumActiveSpells > Player.MaxSpellSlots
			|| SovereignRite.MinimumFuel > (Player.MaxSpellSlots - 1) * Player.AscensionSpellLevel
			|| SovereignRite.MinimumFuel > (SovereignRite.MinimumActiveSpells - 1) * Player.AscensionSpellLevel + (Player.MaxSpellSlots - SovereignRite.MinimumActiveSpells) * Player.AscensionSpellLevel)
			warnings.Add($"{label}: the gate asks for more fuel or spells than a {Player.MaxSpellSlots}-slot book can hold.");
		if (SovereignRite.MinimumPlayerLevel < ApotheosisCatalog.MinimumPlayerLevel)
			warnings.Add($"{label}: offered at level {SovereignRite.MinimumPlayerLevel}, before the Apotheosis's {ApotheosisCatalog.MinimumPlayerLevel}; it is the latest thing a run is offered.");

		// The recipe and its resource.
		List<ComboDefinition> rites = ComboCatalog.All.Where(r => r.IsRite).ToList();
		ComboDefinition rite = ComboCatalog.Rite;
		if (rites.Count != 1 || rite == null || !rite.IsRite)
		{
			warnings.Add($"{label}: ComboCatalog holds {rites.Count} rites, and '{SovereignRite.SpellId}' must be the one.");
			return;
		}
		if (rite.Second == null || !rite.Second.AnyActiveSpell || !string.IsNullOrEmpty(rite.Second.SpellId) || rite.First == null || rite.First.AnyActiveSpell)
			warnings.Add($"{label}: its recipe must be the Rite and any active spell (ComboIngredient.AnyActiveSpell on the second, a spell id on neither).");
		if (ComboCatalog.Fusions.Any(r => r.IsRite))
			warnings.Add($"{label} is listed among the fusions.");
		SpellData template = ContentValidator.LoadAllSpells().FirstOrDefault(s => s.Id.Equals(SovereignRite.SpellId, StringComparison.OrdinalIgnoreCase));
		if (template == null)
			warnings.Add($"{label} has no SpellData in ContentValidator.SpellResourcePaths ({rite.ResourcePath}).");
		else
		{
			if (!template.ResourcePath.Equals(rite.ResourcePath, StringComparison.OrdinalIgnoreCase) || !template.Name.Equals(rite.Name, StringComparison.Ordinal))
				warnings.Add($"{label}'s resource is {template.ResourcePath} named '{template.Name}'; ComboCatalog names {rite.ResourcePath}, '{rite.Name}'.");
			if (template.MaxLevel != 1 || (template.LevelUpgrades?.Count ?? 0) > 0 || (template.ElementWeights?.Count ?? 0) > 0)
				warnings.Add($"{label}'s resource must be terminal and elementless: one level, no level-ups, no element (the spell it anoints keeps its own).");
			if (template.Description.Length > 130)
				warnings.Add($"{label}'s description is {template.Description.Length} characters; a description holds 130.");
		}

		// The unlock: the Warden of the Deep's deed, at the end of the road.
		UnlockDefinition unlock = UnlockCatalog.GetSpell(SovereignRite.SpellId);
		AchievementDefinition deed = UnlockCatalog.SourceDeedOf(unlock);
		if (unlock == null || unlock.Source != UnlockSource.Achievement || deed == null || deed.Kind != AchievementKind.Road
			|| !SovereignRite.SpellId.Equals(deed.SpellUnlockId, StringComparison.OrdinalIgnoreCase))
			warnings.Add($"{label} must be the reward of a Road deed that grants it (UnlockCatalog, AchievementDefinitions).");
		else
		{
			BossDefinition boss = BossCatalog.GetById(deed.BossId);
			StageDefinition gate = StageCatalog.GateChapter;
			int at = StageCatalog.CampaignPositionOf(deed.StageId);
			if (boss == null || gate == null || at < 0 || at < StageCatalog.CampaignPositionOf(gate.Id))
				warnings.Add($"{label} is paid by '{deed.Id}' in chapter '{deed.StageId}', before the end of the road ({gate?.Id}); a new save must not reach it early.");
		}
		if (UnlockCatalog.GrimoireSpellIds.Contains(SovereignRite.SpellId, StringComparer.OrdinalIgnoreCase))
			warnings.Add($"{label} is counted by The Grimoire Restored; a combination is made, not recovered.");
		AchievementDefinition feat = AchievementDefinitions.GetById(ComboCatalog.FeatId(SovereignRite.SpellId));
		if (feat == null || !string.IsNullOrEmpty(feat.SpellUnlockId) || !feat.CompletesInRun || feat.Visibility != AchievementVisibility.Recipe
			|| !feat.DisplayName.Equals($"Conjunction: {rite.Name}", StringComparison.Ordinal))
			warnings.Add($"{label}'s Feat 'Conjunction: {rite.Name}' must exist, complete in the run, be listed as a recipe and grant no spell (the Road deed grants the Rite).");

		// Passives and the defensive spells: never silenced, never fuel, never anointed.
		foreach (string id in SovereignRite.DefensiveSpellIds.Concat(SovereignRite.RefusedTargetIds))
		{
			if (!ContentValidator.LoadAllSpells().Any(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
				warnings.Add($"{label} names '{id}' (defensive or refused), which is no spell.");
		}
		var all = ContentValidator.LoadAllSpells().Where(s => s != null && !ComboCatalog.IsCombination(s.Id)).ToList();
		foreach (SpellData spell in all)
		{
			bool passive = spell.IsPassive || SovereignRite.DefensiveSpellIds.Contains(spell.Id, StringComparer.OrdinalIgnoreCase);
			if (passive && (!SovereignRite.IsPassiveForRite(spell) || SovereignRite.RefusalFor(spell) == null
				|| SovereignRite.ActiveSpells(new[] { spell }).Any()))
				warnings.Add($"{label} would silence, count or anoint '{spell.Id}', a passive or defensive spell.");
		}
		// A loadout with a shield in it: the shield is no fuel and is not counted among the three.
		var loadout = new List<SpellData>();
		foreach (string id in new[] { "aegis_ward", "magic_missile", "fireball", "chain_lightning" })
		{
			if (all.FirstOrDefault(s => s.Id == id)?.Duplicate(true) is SpellData copy)
			{
				copy.CurrentLevel = 8;
				loadout.Add(copy);
			}
		}
		if (loadout.Count == 4)
		{
			SpellData target = loadout[1];
			if (SovereignRite.FuelFor(target, loadout) != 16 || SovereignRite.ActiveSpells(loadout).Count() != 3)
				warnings.Add($"{label}: a book of Aegis Ward and three level 8 spells gives {SovereignRite.FuelFor(target, loadout)} fuel and {SovereignRite.ActiveSpells(loadout).Count()} active; the shield must be neither.");
			if (!SovereignRite.MeetsGate(SovereignRite.MinimumPlayerLevel, loadout) || SovereignRite.MeetsGate(SovereignRite.MinimumPlayerLevel - 1, loadout))
				warnings.Add($"{label}: MeetsGate does not open at exactly level {SovereignRite.MinimumPlayerLevel} for three level 8 spells.");
		}

		// The one stat path, and nothing else in it.
		foreach (SpellData form in ContentValidator.LoadAllSpellForms())
		{
			if (form == null || ComboCatalog.IsCombination(form.Id) || SovereignRite.RefusalFor(form) != null)
				continue;
			var spell = (SpellData)form.Duplicate(true);
			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			spell.CurrentLevel = spell.MaxLevel;
			int level = spell.CurrentLevel;
			int damage = spell.GetDamageAtLevel(level);
			float cooldown = spell.GetCooldownAtLevel(level);
			float area = spell.GetAreaMultiplierAtLevel(level);
			float scale = spell.GetScaleMultiplier();
			int amount = spell.GetProjectileCountAtLevel(level);
			const int fuel = 40;
			SpellEvolutionOption boon = SovereignRite.BuildBoon(spell, fuel);
			SovereignGains gains = SovereignRite.GainsFor(spell, fuel);
			string who = $"{label} on '{ContentValidator.Label(spell)}'";
			if (boon.Effect != SpellEffect.None || boon.EffectValue != 0f || boon.Rider != ElementRider.None
				|| boon.SlowMagnitudeBonus != 0f || boon.KnockbackBonus != 0f || boon.PierceBonus != 0 || boon.ChainArcBonus != 0
				|| boon.ChainBranchBonus != 0 || boon.PoisonTickBonus != 0 || boon.DamageBonus != 0 || boon.RangeBonus != 0f
				|| boon.CooldownBonus != 0f || !Mathf.IsEqualApprox(boon.SpeedMultiplier, 1f) || (boon.BonusElementWeights?.Count ?? 0) > 0
				|| !string.IsNullOrEmpty(boon.ReplacesElement) || boon.ModulateColor != Colors.White || !string.IsNullOrEmpty(boon.VisualTag))
				warnings.Add($"{who}: the boon carries something besides damage, cooldown, area or scale and amount - a crowd-control or execute path a boss could meet.");
			if (gains.CooldownMultiplier < SovereignRite.CooldownFloor - 0.0001f || gains.AreaMultiplier > SovereignRite.AreaCap + 0.0001f
				|| gains.ScaleMultiplier > SovereignRite.AreaCap + 0.0001f || (gains.AreaMultiplier > 1f && gains.ScaleMultiplier > 1f))
				warnings.Add($"{who}: the gains pass their floor or cap, or grow both area and scale.");

			spell.SovereignBoon = boon;
			bool moved = spell.GetDamageAtLevel(level) == Math.Max(1, Mathf.RoundToInt(damage * gains.DamageMultiplier))
				&& Mathf.IsEqualApprox(spell.GetCooldownAtLevel(level), MathF.Max(0.05f, cooldown * gains.CooldownMultiplier))
				&& Mathf.IsEqualApprox(spell.GetAreaMultiplierAtLevel(level), area * gains.AreaMultiplier)
				&& Mathf.IsEqualApprox(spell.GetScaleMultiplier(), scale * gains.ScaleMultiplier)
				&& spell.GetProjectileCountAtLevel(level) == Math.Max(1, amount + gains.AmountBonus);
			bool elementsKept = spell.GetElementWeights().Count == form.GetElementWeights().Count;
			spell.SovereignBoon = null;
			bool back = spell.GetDamageAtLevel(level) == damage && Mathf.IsEqualApprox(spell.GetCooldownAtLevel(level), cooldown)
				&& spell.GetProjectileCountAtLevel(level) == amount;
			if (!moved || !back)
				warnings.Add($"{who}: the fuel does not reach (or does not leave) the spell's own stat readers through SpellData.CollectActive.");
			if (!elementsKept)
				warnings.Add($"{who}: anointing changed the spell's elements; the Rite never moves a tier.");
		}
	}

	private static void ValidateSpellEvolutionCoverage(List<string> warnings)
	{
		string[] testSpellIds = new[] { "magic_missile", "fireball", "arcane_explosion", "aegis_ward", "meteor_swarm" };
		foreach (string id in testSpellIds)
		{
			var spell = new SpellData { Id = id, Name = id };
			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			if (spell.Level4Options == null || spell.Level4Options.Count != 3)
				warnings.Add($"Spell '{id}' should have exactly 3 Level 4 evolution choices, got {spell.Level4Options?.Count ?? 0}.");
			if (spell.Level8Options == null || spell.Level8Options.Count != 2)
				warnings.Add($"Spell '{id}' should have exactly 2 Level 8 evolution choices, got {spell.Level8Options?.Count ?? 0}.");
		}
	}

	private static void ValidateDynamicRewardBounds(List<string> warnings)
	{
		var save = new SaveData();
		for (int i = 0; i < 8; i++)
		{
			save.RunTelemetryHistory.Add(new RunTelemetryRecord
			{
				Outcome = i < 4 ? "Defeat" : "Victory",
				TimeSurvived = 180f + (i * 25f),
				EnemiesKilled = 40 + (i * 7)
			});
		}

		var run = new RunResult
		{
			TimeSurvived = 480f,
			EnemiesKilled = 220,
			EquippedSpells = new List<RunSpellSnapshot>
			{
				new() { Id = "magic_missile", DisplayName = "Magic Missile" },
				new() { Id = "fireball", DisplayName = "Fireball" },
				new() { Id = "meteor_swarm", DisplayName = "Meteor Swarm" }
			}
		};

		float multiplier = GlobalStatsManager.GetDynamicArcaneRewardMultiplier(save, run, out _);
		if (multiplier < GlobalStatsManager.DynamicRewardMinMultiplier || multiplier > GlobalStatsManager.DynamicRewardMaxMultiplier)
			warnings.Add($"Dynamic reward multiplier out of bounds: {multiplier:0.000}");
	}

	private static void ValidateTelemetryHistoryCap(List<string> warnings)
	{
		var save = new SaveData();
		for (int i = 0; i < 40; i++)
		{
			save.RecordRunTelemetry(new RunResult
			{
				Outcome = i % 2 == 0 ? RunOutcome.Defeat : RunOutcome.Victory,
				TimeSurvived = 60f + i,
				EnemiesKilled = i
			});
		}

		if (save.RunTelemetryHistory.Count > 25)
			warnings.Add($"Telemetry history exceeded cap: {save.RunTelemetryHistory.Count}");
	}

	private static void ValidateCharacterRosterBasics(List<string> warnings)
	{
		var characters = CharacterRoster.GetAll();
		if (characters.Count == 0)
		{
			warnings.Add("Character roster returned zero entries.");
			return;
		}

		foreach (CharacterData character in characters)
		{
			if (character.StartingSpellResource == null)
				warnings.Add($"Character '{character.Id}' missing starting spell resource.");
		}
	}

	// The starting roster's one real invariant: a starter is its element. Each wizard a new save holds
	// opens with a spell carrying a single element weight, and no two of them open the same one, so
	// picking a starter is picking which set of element thresholds the run starts pointed at. (Freed
	// wizards are held to less since 2026-11-01: ValidateStartingCharacters says what.)
	//
	// Both halves rot silently. A starting spell quietly retagged with a second element (Magic
	// Missile carried Arcane *and* Lightning, which is why the Apprentice was dropped; it went pure
	// Arcane on 2026-11-02 for The Archmage) splits the character's opening weight without changing
	// anything visible; two characters sharing an element makes one of them redundant without
	// breaking a thing.
	// Two rules that only exist as agreement between data files, so nothing but this notices when
	// they break.
	//
	// 1. No spell carries the same element twice in its BASE tags. A spell used to be able to be
	//    "Fire x2", which made a single-element spell worth as much element weight as a hybrid and
	//    left nothing to distinguish the two. Weight is now capped at 1 per element, and what a
	//    pure spell gets instead is attunement (Player.GetAttunementMultiplier). Evolutions may
	//    still stack a second tag of the same element - that is the level 4 and level 8 choice -
	//    which is why this checks the resource's declared weights and not GetElementWeights().
	//
	// 2. Attunement is only worth anything to a spell that deals damage, so a pure spell that is
	//    passive gets no compensation at all for its halved element weight. Passives must carry
	//    two elements.
	// Wave formation geometry. None of this is visible to a build, and all of it fails quietly in
	// play: a formation with a bad placement does not crash, it just spawns a wave that looks like
	// the scatter it was meant to replace, which is indistinguishable from the feature not being
	// finished.
	// Every element must be able to reach its 6-instance capstone.
	//
	// This is the check that guarded the passives-to-boons shift, and it is why that shift did not
	// silently strand five elements below their top tier. Passive spells carried roughly a third of
	// the element weight in the game - four of Metal's six carriers, two of Water's three - and
	// nothing breaks loudly when a carrier disappears: the element simply never reaches a threshold
	// again, and the bonus at the end of it becomes text nobody ever sees.
	//
	// It stays because the same thing can happen to any future content change.
	//
	// (Magic Missile was once built in code and went uncounted here; it is SpellData.tres, in
	// ContentValidator.SpellResourcePaths, and pure Arcane since 2026-11-02 - its Lightning is a branch,
	// Twin Volley at level 4 and Overcharge at 8, so its best Lightning is the 2 it always was.)
	// Every boon needs exactly one way to be obtained, for the same reason every spell does: one
	// with no catalog entry is defined, offered by nothing, and invisible.
	// Rarity and element tags on chest items, which are two separate tables that have to agree.
	//
	// The rule they encode is the point: a tag belongs only on a scarce item. If a Common ever
	// picks one up, element weight stops being a budget - and nothing in play would look wrong,
	// the thresholds would just start arriving early and the reason would be invisible.
	private static void ValidateChestRarity(List<string> warnings)
	{
		foreach (string itemId in ChestItemCatalog.AllItemIds)
		{
			// Tome relics use rarity for how often they are offered and nothing else: they carry no
			// tag at any rarity (ValidateTomeRelics).
			if (ChestItemCatalog.IsTomeRelic(itemId))
				continue;
			ChestItemRarity rarity = ChestItemCatalog.GetRarity(itemId);
			IReadOnlyList<(string Element, int Weight)> tags = ChestItemCatalog.GetElementTags(itemId);
			bool scarce = rarity is ChestItemRarity.Rare or ChestItemRarity.Relic;

			if (!scarce && tags.Count > 0)
				warnings.Add($"Chest item '{itemId}' is {rarity} but carries {tags.Count} element tags; only Rare and Relic items may.");

			if (scarce && tags.Count == 0)
				warnings.Add($"Chest item '{itemId}' is {rarity} but carries no element tag, which is most of what that rarity is for.");

			if (rarity == ChestItemRarity.Relic && tags.Count != 2)
				warnings.Add($"Relic '{itemId}' carries {tags.Count} element tags; a Relic carries two.");

			if (rarity == ChestItemRarity.Rare && tags.Count != 1)
				warnings.Add($"Rare '{itemId}' carries {tags.Count} element tags; a Rare carries one.");

			CheckTagList(warnings, $"Chest item '{itemId}'", tags);
		}

		foreach (ChestSetDefinition set in ChestItemCatalog.Sets)
		{
			if (set.ElementTags.Length != 2)
				warnings.Add($"Full Set Enchantment '{set.Id}' grants {set.ElementTags.Length} element tags; a completed set grants two.");

			CheckTagList(warnings, $"Set '{set.Id}'", set.ElementTags);
		}
	}

	private static void CheckTagList(List<string> warnings, string owner, IReadOnlyList<(string Element, int Weight)> tags)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (element, weight) in tags)
		{
			if (!Enum.TryParse<Element>(element, true, out _))
				warnings.Add($"{owner} carries unknown element '{element}'.");
			if (!seen.Add(element))
				warnings.Add($"{owner} carries {element} twice; one instance per element, as with every other tag source.");
			if (weight != 1)
				warnings.Add($"{owner} carries {element} at weight {weight}; tags are one instance each.");
		}
	}

	private static void ValidateBoonSources(List<string> warnings)
	{
		foreach (BoonDefinition boon in BoonCatalog.All)
		{
			int sources = UnlockCatalog.All.Count(d =>
				d.Kind == UnlockKind.Boon && d.Id.Equals(boon.Id, StringComparison.OrdinalIgnoreCase));

			if (sources == 0)
				warnings.Add($"Boon '{boon.Id}' has no unlock source, so it can never be offered.");
			else if (sources > 1)
				warnings.Add($"Boon '{boon.Id}' has {sources} unlock sources; each thing needs exactly one.");

			// Same rule as spells: never the same element twice on one thing.
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var (element, weight) in boon.ElementWeights)
			{
				if (!Enum.TryParse<Element>(element, true, out _))
					warnings.Add($"Boon '{boon.Id}' carries unknown element '{element}'.");
				if (!seen.Add(element))
					warnings.Add($"Boon '{boon.Id}' carries {element} twice; base element weights are one per element.");
				if (weight != 1)
					warnings.Add($"Boon '{boon.Id}' carries {element} at weight {weight}; boons carry one instance per element.");
			}
		}
	}

	// A boon's card is its icon and its stat line, and both fail silently. The icon is drawn by
	// tools/art/boon_icons.py only for a boon with an ELEMENT entry there, and an unimported or
	// ungenerated PNG loads as null and the card shows an empty reliquary window. The stat line is
	// BoonCatalog.DescribeEffect's switch, whose default arm is an empty string - so a new BoonEffect
	// without an arm would be dealt as a card that says nothing at all.
	private static void ValidateBoonPresentation(List<string> warnings)
	{
		foreach (BoonDefinition boon in BoonCatalog.All)
		{
			if (!ResourceLoader.Exists(boon.IconPath))
				warnings.Add($"Boon '{boon.Id}' has no icon at '{boon.IconPath}'; add it to ELEMENT (and ICONS) in tools/art/boon_icons.py, run it and import.");
			if (string.IsNullOrWhiteSpace(BoonCatalog.DescribeEffect(boon)))
				warnings.Add($"Boon '{boon.Id}' has an empty stat line; BoonCatalog.DescribeEffect has no arm for {boon.Effect}.");
		}
		// Read into a local: compared as a constant, a rule that holds today is "unreachable code".
		float stance = BoonCatalog.StanceSeconds;
		if (stance <= 0f)
			warnings.Add("BoonCatalog.StanceSeconds is not positive, so a stance boon would hold even while walking.");
	}

	private static void ValidateElementReachability(List<string> warnings)
	{
		// Two pools with two separate caps: spells fill six slots, boons fill six of their own.
		// Summing them as one pool would let a six-boon element borrow spell slots it cannot use.
		var spellContributions = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
		var boonContributions = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
		// The second pass (.ai/achievements-and-secrets.md 6.3, item 5): only what a player has on the
		// road - starters, the shop's boons, Feats, and chapter deeds before the gate - and never a spell a
		// secret pays, since a secret is missable. No element's capstone may wait on one.
		var roadSpellContributions = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
		var roadBoonContributions = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

		static void RecordInto(Dictionary<string, List<int>> pool, string element, int weight)
		{
			if (weight <= 0)
				return;

			if (!pool.TryGetValue(element, out List<int> list))
			{
				list = new List<int>();
				pool[element] = list;
			}

			list.Add(weight);
		}

		void Record(string element, int weight) => RecordInto(spellContributions, element, weight);

		foreach (SpellData template in ContentValidator.LoadAllSpells())
		{
			if (template?.ElementWeights == null || string.IsNullOrWhiteSpace(template.Id))
				continue;

			// A spell taken in a form is ONE carrier whichever form it is taken in, so its best per element
			// is the best of its forms - never one entry per form, which would count three slots for one.
			var best = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (SpellData spell in SpellVariants.Forms(template))
			{
				SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
				List<(string Id, Dictionary<string, int> Gains)> level4 =
					MaterialiseGains(spell.Level4Options, spell.Id, warnings);
				List<(string Id, Dictionary<string, int> Gains)> level8 =
					MaterialiseGains(spell.Level8Options, spell.Id, warnings);

				// Every element this spell could ever touch - the ones it already carries AND the ones
				// it can branch into. Walking only the base tags missed branches entirely, which is how
				// the first run of this check under-counted Darkness: Obsidian Spike is a pure Earth
				// spell whose level 4 branch grants Darkness, and that carrier was invisible.
				var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var pair in spell.ElementWeights)
					candidates.Add(pair.Key);
				foreach (var option in level4.Concat(level8))
				{
					foreach (var gain in option.Gains)
						candidates.Add(gain.Key);
				}

				foreach (string element in candidates)
				{
					// The best this one spell could ever contribute to this one element: its base tag,
					// plus the most generous evolution available at each milestone.
					int baseWeight = spell.ElementWeights.TryGetValue(element, out int w) ? w : 0;
					int total = baseWeight + BestGain(level4, element) + BestGain(level8, element);
					best[element] = best.TryGetValue(element, out int had) ? Math.Max(had, total) : total;
				}
			}
			bool onRoad = IsOnTheRoad(UnlockCatalog.GetSpell(template.Id));
			foreach (var pair in best)
			{
				Record(pair.Key, pair.Value);
				if (onRoad)
					RecordInto(roadSpellContributions, pair.Key, pair.Value);
			}
		}

		foreach (BoonDefinition boon in BoonCatalog.All)
		{
			bool onRoad = IsOnTheRoad(UnlockCatalog.Get(boon.Id, UnlockKind.Boon));
			foreach (var (element, weight) in boon.ElementWeights)
			{
				RecordInto(boonContributions, element, weight);
				if (onRoad)
					RecordInto(roadBoonContributions, element, weight);
			}
		}

		foreach (Element element in Enum.GetValues<Element>())
		{
			string name = element.ToString();
			int fromSpells = TopN(spellContributions, name, Player.MaxSpellSlots);
			int fromBoons = TopN(boonContributions, name, Player.MaxBoonSlots);
			int reachable = fromSpells + fromBoons;

			if (reachable < 6)
			{
				warnings.Add($"Element '{name}' can reach at most {reachable} instances ({fromSpells} from spells, {fromBoons} from boons), so its tier-6 bonus is unreachable. It needs more carriers.");
				continue;
			}

			int roadSpells = TopN(roadSpellContributions, name, Player.MaxSpellSlots);
			int roadBoons = TopN(roadBoonContributions, name, Player.MaxBoonSlots);
			if (roadSpells + roadBoons < 6)
				warnings.Add($"Element '{name}' reaches only {roadSpells + roadBoons} instances ({roadSpells} from spells, {roadBoons} from boons) without the spells hidden in secrets and past the gate, so its tier-6 bonus waits on a secret. Give it a carrier on the road (.ai/achievements-and-secrets.md 6.3, item 5).");
		}
	}

	// Whether an unlock can be had on the road, before the first gate, without a secret: a starter, a
	// shop boon, or an achievement that is no secret and names no chapter at or past the gate.
	private static bool IsOnTheRoad(UnlockDefinition unlock)
	{
		if (unlock == null)
			return false;
		if (unlock.Source == UnlockSource.Starter || unlock.Source == UnlockSource.Purchase)
			return true;
		AchievementDefinition deed = UnlockCatalog.SourceDeedOf(unlock);
		if (deed == null || deed.Kind == AchievementKind.Secret)
			return false;
		return IsBeforeFirstGate(deed.StageId);
	}

	// A chapter a new save can reach before the first gate: in the campaign before it, or outside the
	// campaign and open from the start. An empty chapter (a Feat's) is before every gate.
	private static bool IsBeforeFirstGate(string stageId)
	{
		if (string.IsNullOrWhiteSpace(stageId))
			return true;
		StageDefinition gate = StageCatalog.GateChapter;
		int position = StageCatalog.CampaignPositionOf(stageId);
		if (position < 0)
			return StageCatalog.IsOpenFromStart(StageCatalog.GetById(stageId));
		return gate == null || position < StageCatalog.CampaignPositionOf(gate.Id);
	}

	private static int TopN(Dictionary<string, List<int>> pool, string element, int slots)
	{
		if (!pool.TryGetValue(element, out List<int> list))
			return 0;

		list.Sort();
		list.Reverse();
		return list.Take(slots).Sum();
	}

	private static int BestGain(List<(string Id, Dictionary<string, int> Gains)> options, string element)
	{
		int best = 0;
		foreach (var option in options)
		{
			if (option.Gains.TryGetValue(element, out int gain) && gain > best)
				best = gain;
		}

		return best;
	}

	private static void ValidateSpawnFormations(List<string> warnings)
	{
		foreach (SpawnFormation shape in Enum.GetValues<SpawnFormation>())
		{
			for (int count = 1; count <= 12; count++)
			{
				var bearings = new List<float>();
				int leftFlank = 0;
				int rightFlank = 0;

				for (int i = 0; i < count; i++)
				{
					var (bearing, radiusScale) = SpawnFormations.Placement(shape, i, count, 0f);

					if (float.IsNaN(bearing) || float.IsInfinity(bearing))
					{
						warnings.Add($"Formation {shape} member {i} of {count} has a bearing of {bearing}.");
						continue;
					}

					// A scale at or below zero puts the member on top of the player, which is the
					// same trap that silently invalidated an A/B of the spawn ring.
					if (radiusScale <= 0f)
						warnings.Add($"Formation {shape} member {i} of {count} has radius scale {radiusScale}; it would spawn inside the player.");

					bearings.Add(bearing);
					if (shape == SpawnFormation.Pincer)
					{
						if (Mathf.Sin(bearing) >= 0f) rightFlank++;
						else leftFlank++;
					}
				}

				if (bearings.Count != count)
					continue;

				// A Ring has to actually surround: its members must span most of a circle.
				if (shape == SpawnFormation.Ring && count >= 4)
				{
					float span = bearings.Max() - bearings.Min();
					if (span < Mathf.Pi * 1.4f)
						warnings.Add($"Formation Ring of {count} spans only {span:0.00} rad; it would read as an arc, not a ring.");
				}

				// An Arc has to stay in front. One that wraps past a half circle is a ring with a
				// gap, and the player would be flanked by something advertised as a wall.
				if (shape == SpawnFormation.Arc && count >= 2)
				{
					float span = bearings.Max() - bearings.Min();
					if (span > Mathf.Pi)
						warnings.Add($"Formation Arc of {count} spans {span:0.00} rad, more than half a circle.");
				}

				// A Pincer has to squeeze from both sides. Loading one flank makes it an arc that
				// happens to be off to one side.
				if (shape == SpawnFormation.Pincer && count >= 2 && Math.Abs(leftFlank - rightFlank) > 1)
					warnings.Add($"Formation Pincer of {count} splits {leftFlank}/{rightFlank} across its flanks; it should be even.");
			}
		}
	}

	// The swarm that closes on a player who falls behind (Node2DGame.SwarmPressure.cs). The knobs are
	// checked where a bad value would silently undo the feature or break a promise another system
	// makes - a ring margin under 1 puts bodies on camera out of nothing, a zero rise time is full
	// pressure on the first full frame - and the fold itself is run on two real enemy scenes, because
	// a fold that loses health or XP would quietly make falling behind easier rather than harder.
	private static void ValidateSwarmPressure(List<string> warnings)
	{
		if (Node2DGame.SwarmDefaultStartPaceSeconds < 0f || Node2DGame.SwarmDefaultStartPaceSeconds >= Node2DGame.ReferenceRunSeconds)
			warnings.Add($"Swarm pressure starts at pace {Node2DGame.SwarmDefaultStartPaceSeconds} s, outside the {Node2DGame.ReferenceRunSeconds} s reference run.");
		if (Node2DGame.SwarmDefaultRiseSeconds < 5f || Node2DGame.SwarmDefaultFallSeconds < 1f)
			warnings.Add($"Swarm pressure rises in {Node2DGame.SwarmDefaultRiseSeconds} s and falls in {Node2DGame.SwarmDefaultFallSeconds} s; a near-zero rise is full pressure on the first full frame.");
		if (Node2DGame.SwarmDefaultChokedShareToRise <= 0f || Node2DGame.SwarmDefaultChokedShareToRise > 1f)
			warnings.Add($"Swarm choked share {Node2DGame.SwarmDefaultChokedShareToRise} must be in (0, 1].");
		if (Node2DGame.SwarmDefaultFullEdgeMargin < 1f)
			warnings.Add($"Swarm full-pressure ring margin {Node2DGame.SwarmDefaultFullEdgeMargin} is under the screen edge; bodies would appear on camera.");
		if (Node2DGame.SwarmDefaultRingTighten < 0f || Node2DGame.SwarmDefaultRingTighten >= 1f)
			warnings.Add($"Swarm ring tighten {Node2DGame.SwarmDefaultRingTighten} must be in [0, 1).");
		if (Node2DGame.SwarmDefaultStragglerSpeedBonus < 0f || Node2DGame.SwarmDefaultNearSpeedBonus < 0f
			|| Node2DGame.SwarmDefaultNearSpeedBonus > Node2DGame.SwarmDefaultStragglerSpeedBonus)
			warnings.Add("Swarm speed bonuses must be non-negative, and a body on screen must never be sped more than a straggler.");
		if (Node2DGame.SwarmDefaultSwellPressure <= 0f || Node2DGame.SwarmDefaultSwellPressure > 1f
			|| Node2DGame.SwarmDefaultTrailRecyclePressure <= 0f || Node2DGame.SwarmDefaultTrailRecyclePressure > 1f)
			warnings.Add("Swarm fold and send-round thresholds must be in (0, 1].");
		if (Node2DGame.SwarmDefaultMaxMass < 2 || Node2DGame.SwarmDefaultSwellsPerSweep < 1)
			warnings.Add($"Swarm max mass {Node2DGame.SwarmDefaultMaxMass} and folds per sweep {Node2DGame.SwarmDefaultSwellsPerSweep} would fold nothing.");
		// XP that keeps pace with the run (Node2DGame.XpPacing.cs). Negative would make late kills pay
		// less than early ones; past about x5 at the boss the gates would come in the opening minutes.
		float endMultiplier = 1f + Node2DGame.XpDefaultGrowthPerPaceMinute * Node2DGame.ReferenceRunSeconds / 60f;
		if (Node2DGame.XpDefaultGrowthPerPaceMinute < 0f || endMultiplier > 5f)
			warnings.Add($"XP growth {Node2DGame.XpDefaultGrowthPerPaceMinute}/pace minute makes XP worth x{endMultiplier:0.00} at the boss; keep it in [0, 5].");

		var scene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
		if (scene?.Instantiate() is not Enemy absorber || scene.Instantiate() is not Enemy absorbed)
		{
			warnings.Add("Swarm fold check: res://scenes/enemy.tscn did not instantiate as an Enemy.");
			return;
		}
		absorber.Health = 40;
		absorbed.Health = 25;
		int contact = absorber.ContactDamage;
		absorber.AbsorbIntoSwarm(absorbed);
		if (absorber.Health != 65 || absorber.SwarmMass != 2)
			warnings.Add($"Swarm fold: 40 + 25 health and mass 1 + 1 came out as {absorber.Health} health, mass {absorber.SwarmMass}.");
		for (int i = 0; i < 3; i++)
		{
			var more = (Enemy)scene.Instantiate();
			more.Health = 10;
			absorber.AbsorbIntoSwarm(more);
		}
		if (absorber.SwarmMass != 5 || absorber.ContactDamage != contact + 2)
			warnings.Add($"Swarm fold: a mass-5 body should touch for {contact + 2}, not {absorber.ContactDamage} (mass {absorber.SwarmMass}).");
		absorber.Free();
	}

	private static void ValidateElementTags(List<string> warnings)
	{
		// One pass, and every Godot array is materialised into a plain List before it is read more
		// than once. Enumerating a Godot.Collections.Array creates a fresh managed wrapper per
		// element per enumeration, so re-walking Level4Options inside a loop over Level8Options
		// produced enough short-lived wrappers to crash the finaliser mid-validation.
		// Every form of a spell taken in one (ContentValidator.LoadAllSpellForms): each has its own milestones.
		foreach (SpellData spell in ContentValidator.LoadAllSpellForms())
		{
			if (spell?.ElementWeights == null)
				continue;

			foreach (var pair in spell.ElementWeights)
			{
				if (pair.Value > 1)
					warnings.Add($"Spell '{spell.Id}' declares {pair.Key} with weight {pair.Value}; base element weights are capped at 1 per element, and depth comes from evolutions instead.");
			}

			// A combination has no milestones and is the one spell allowed three elements, never four:
			// ValidateComboCatalog holds its ceiling.
			if (ComboCatalog.IsCombination(spell.Id))
				continue;

			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);

			var baseWeights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var pair in spell.ElementWeights)
				baseWeights[pair.Key] = pair.Value;

			string label = ContentValidator.Label(spell);
			List<(string Id, Dictionary<string, int> Gains)> level4 = MaterialiseGains(spell.Level4Options, label, warnings);
			List<(string Id, Dictionary<string, int> Gains)> level8 = MaterialiseGains(spell.Level8Options, label, warnings);
			ValidateEvolutionElementCeiling(label, baseWeights, level4, level8, warnings);
		}
	}

	// Flattens a milestone's options to plain data, checking each named element exists on the way -
	// an unknown element name is dropped in silence by SpellData.ApplyBonusElementWeights, so the
	// branch would simply grant nothing. A null entry stands for "took neither option".
	private static List<(string Id, Dictionary<string, int> Gains)> MaterialiseGains(
		Godot.Collections.Array<SpellEvolutionOption> options, string spellId, List<string> warnings)
	{
		var result = new List<(string, Dictionary<string, int>)> { ("none", new Dictionary<string, int>()) };
		if (options == null)
			return result;

		foreach (SpellEvolutionOption evo in options)
		{
			var gains = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			// A traded-away element rides in the same map under a sentinel, so every reader of the
			// gains sees it without the tuple changing shape: BestGain never picks it (it is the
			// smallest int there is) and the ceiling below removes the element instead of adding.
			if (!string.IsNullOrEmpty(evo?.ReplacesElement))
			{
				if (!Enum.TryParse<Element>(evo.ReplacesElement, true, out _))
					warnings.Add($"Evolution '{evo.Id}' on '{spellId}' replaces element '{evo.ReplacesElement}', which is not a known element, so nothing is traded away.");
				else if (evo.BonusElementWeights != null && evo.BonusElementWeights.ContainsKey(evo.ReplacesElement))
					warnings.Add($"Evolution '{evo.Id}' on '{spellId}' both replaces and grants {evo.ReplacesElement}.");
				else
					gains[evo.ReplacesElement] = ReplacedElement;
			}
			if (evo?.BonusElementWeights != null)
			{
				foreach (var pair in evo.BonusElementWeights)
				{
					if (!Enum.TryParse<Element>(pair.Key, true, out _))
					{
						warnings.Add($"Evolution '{evo.Id}' on '{spellId}' grants element '{pair.Key}', which is not a known element, so the branch grants nothing.");
						continue;
					}

					gains[pair.Key] = pair.Value;
				}
			}

			result.Add((evo?.Id ?? "none", gains));
		}

		return result;
	}

	// The gain MaterialiseGains records for an element a branch trades away.
	private const int ReplacedElement = int.MinValue;

	// Two ceilings that only hold by agreement between catalog entries, and that a player would
	// find by out-scaling the game rather than by anything looking broken:
	//
	//   1. No spell may end up naming more than TWO distinct elements. A hybrid has both of its
	//      slots filled already, so its evolutions may only deepen what it carries - or TRADE one
	//      element for another (SpellEvolutionOption.ReplacesElement), which keeps it at two;
	//      branching to a new element on top is what a pure spell trades its attunement for.
	//   2. Only a pure spell may reach 3 instances of one element. A hybrid able to put both of its
	//      gains into the same element would match a pure spell's depth and keep a second element
	//      as well, which is most of the reason to ever pick a pure spell.
	//
	// Checked over every level 4 x level 8 pairing, because both ceilings are properties of the
	// combination rather than of either option alone.
	private static void ValidateEvolutionElementCeiling(
		string spellId,
		Dictionary<string, int> baseWeights,
		List<(string Id, Dictionary<string, int> Gains)> level4,
		List<(string Id, Dictionary<string, int> Gains)> level8,
		List<string> warnings)
	{
		bool isPure = baseWeights.Count == 1;

		foreach (var early in level4)
		{
			foreach (var late in level8)
			{
				var totals = new Dictionary<string, int>(baseWeights, StringComparer.OrdinalIgnoreCase);
				foreach (var gains in new[] { early.Gains, late.Gains })
				{
					// Trades first, as SpellData.GetElementWeights applies them, then the gains.
					foreach (var pair in gains)
					{
						if (pair.Value != ReplacedElement)
							continue;
						if (!totals.Remove(pair.Key))
							warnings.Add($"Spell '{spellId}' trades away {pair.Key} via '{(gains == early.Gains ? early.Id : late.Id)}', which it does not carry at that point.");
					}
					foreach (var pair in gains)
					{
						if (pair.Value != ReplacedElement)
							totals[pair.Key] = totals.TryGetValue(pair.Key, out int existing) ? existing + pair.Value : pair.Value;
					}
				}

				if (totals.Count > 2)
					warnings.Add($"Spell '{spellId}' can reach {totals.Count} elements via '{early.Id}' + '{late.Id}'; a spell may never name more than two.");

				if (isPure)
					continue;

				foreach (var pair in totals)
				{
					if (pair.Value >= 3)
						warnings.Add($"Hybrid '{spellId}' can reach {pair.Value} {pair.Key} via '{early.Id}' + '{late.Id}'; only a pure spell may reach 3 of one element.");
				}
			}
		}
	}

	// Element paths (.ai/spell-roster.md, "Element paths"; ElementPaths). The rules the user set, which
	// no single catalog entry can see broken:
	//
	//   1. A pure spell's level 4 offers BOTH a branch that deepens its own element and one that adds
	//      a different element - so the milestone is a decision, never a free tag.
	//   2. A hybrid's path is deepening its other half (a third element is never allowed), so each of
	//      its two elements is deepened by some level 4 or level 8 option.
	//   3. A branch into a new element looks like it (a tint) and names its tag on the card; a rider,
	//      if it has one, is the status that element carries (ElementPaths.RiderFor), with numbers
	//      its status can apply.
	//   4. Every element has a pure spell, because only a pure spell can carry an element to its
	//      capstone.
	//
	// Options are flattened to plain data first, for the reason ValidateElementTags gives.
	private static void ValidateElementPaths(List<string> warnings)
	{
		var pureElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (SpellData spell in ContentValidator.LoadAllSpellForms())
		{
			if (spell?.ElementWeights == null || string.IsNullOrWhiteSpace(spell.Id))
				continue;

			// A combination never mutates or ascends: it has no element path to offer (ComboCatalog).
			if (ComboCatalog.IsCombination(spell.Id))
				continue;

			var baseElements = new HashSet<string>(spell.ElementWeights.Keys, StringComparer.OrdinalIgnoreCase);
			bool isPure = baseElements.Count == 1;
			if (isPure)
				pureElements.UnionWith(baseElements);

			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			var options = new List<(int Milestone, SpellEvolutionOption Option, List<string> Gains)>();
			foreach (var (milestone, list) in new[] { (4, spell.Level4Options), (8, spell.Level8Options) })
			{
				if (list == null)
					continue;
				foreach (SpellEvolutionOption evo in list.ToList())
				{
					if (evo == null)
						continue;
					var gains = evo.BonusElementWeights == null
						? new List<string>()
						: evo.BonusElementWeights.Where(p => p.Value > 0).Select(p => p.Key).ToList();
					options.Add((milestone, evo, gains));
				}
			}

			if (isPure)
			{
				string own = baseElements.First();
				bool deepens = options.Any(o => o.Milestone == 4 && o.Gains.Any(g => g.Equals(own, StringComparison.OrdinalIgnoreCase)));
				bool branches = options.Any(o => o.Milestone == 4 && o.Gains.Any(g => !baseElements.Contains(g)));
				if (!deepens)
					warnings.Add($"Pure spell '{spell.Id}' has no level 4 branch that deepens {own}; a pure spell's milestone must offer one.");
				if (!branches)
					warnings.Add($"Pure spell '{spell.Id}' has no level 4 element path - no branch adds a second element to {own}, so the milestone is never an element decision.");
			}
			else
			{
				foreach (string element in baseElements)
				{
					if (!options.Any(o => o.Gains.Any(g => g.Equals(element, StringComparison.OrdinalIgnoreCase))))
						warnings.Add($"Hybrid '{spell.Id}' never deepens {element}; a hybrid's element path is deepening either half.");
				}
			}

			foreach (var (milestone, evo, gains) in options)
			{
				foreach (string gain in gains.Where(g => !baseElements.Contains(g)))
				{
					if (evo.ModulateColor == Colors.White)
						warnings.Add($"Evolution '{evo.Id}' branches '{spell.Id}' into {gain} but keeps the spell's look (no ModulateColor); an element path must look like its element.");
					if (string.IsNullOrEmpty(evo.Description) || evo.Description.IndexOf(gain, StringComparison.OrdinalIgnoreCase) < 0)
						warnings.Add($"Evolution '{evo.Id}' branches '{spell.Id}' into {gain} but its description never names the {gain} tag.");
				}

				if (evo.Rider == ElementRider.None)
					continue;
				bool carried = gains.Any(g => Enum.TryParse<Element>(g, true, out Element e) && ElementPaths.RiderFor(e) == evo.Rider);
				if (!carried)
					warnings.Add($"Evolution '{evo.Id}' on '{spell.Id}' carries a {evo.Rider} rider, but no element it grants ({string.Join(", ", gains)}) carries {evo.Rider} (ElementPaths.RiderFor).");
				if (!ElementPaths.IsValid(evo.Rider, evo.RiderMagnitude, evo.RiderSeconds))
					warnings.Add($"Evolution '{evo.Id}' on '{spell.Id}' has a {evo.Rider} rider of {evo.RiderMagnitude} for {evo.RiderSeconds} s, which its status cannot apply.");
			}
		}

		foreach (Element element in Enum.GetValues<Element>())
		{
			if (!pureElements.Contains(element.ToString()))
				warnings.Add($"Element '{element}' has no pure spell, so nothing can carry it to its 6-instance capstone.");
		}
	}

	// Every number on a level 4, level 8 or Apotheosis card must be one its spell's script reads
	// (EvolutionStatReaders). Thirteen spells shared default evolutions written for a bolt, and a pit in
	// the ground offered "projectile speed" and "pierce" that nothing it ran ever read - a field that is
	// set and never read compiles, loads and shows on the card, so only a player could see it
	// (2026-10-24, "isn't it an aoe? does that even make sense?"). This is the check that sees it, for
	// every spell in ContentValidator.SpellResourcePaths, the next one to land on the default archetype
	// included. What the hand-written cards still promise and never built is listed, with the promise,
	// in EvolutionStatReaders.KnownUnbuilt; an entry there that stops being true is reported as stale,
	// so the list can only shrink. Options are flattened to plain lists first, for the reason
	// ValidateElementTags gives.
	// No pixel counts in anything a player reads (2026-10-28: "spiritual weapon mutation says it increases
	// by 60px. never mention px, add a %"). A pixel is the canvas's unit, not the player's: a size is a
	// percentage of what it was, or words ("a small splash", "farther with Area"). Walks every spell's
	// description and evolution cards - for a spell taken in a form, every form's (LoadAllSpellForms), since
	// each has its own words and milestones - every Apotheosis form, every boon and every relic.
	private static readonly System.Text.RegularExpressions.Regex PixelUnit =
		new(@"\d\s*px\b|\bpx\b|\bpixels?\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

	private static void ValidateNoPixelsInText(List<string> warnings)
	{
		void Check(string where, string text)
		{
			if (!string.IsNullOrEmpty(text) && PixelUnit.IsMatch(text))
				warnings.Add($"Text: {where} says \"{PixelUnit.Match(text).Value}\" - give a size as a percentage or in words, never in pixels: \"{text}\"");
		}

		foreach (SpellData spell in ContentValidator.LoadAllSpellForms())
		{
			if (spell == null)
				continue;
			Check($"spell {ContentValidator.Label(spell)}", spell.Description);
			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			foreach (SpellEvolutionOption option in (spell.Level4Options?.ToList() ?? new()).Concat(spell.Level8Options?.ToList() ?? new()))
			{
				Check($"evolution {option.Id}", option.Description);
				Check($"evolution {option.Id}'s synergy line", option.SynergyDescription);
			}
		}
		foreach (ApotheosisDefinition definition in ApotheosisCatalog.All)
			Check($"Apotheosis {definition.Form?.Id}", definition.Form?.Description);
		foreach (BoonDefinition boon in BoonCatalog.All)
		{
			Check($"boon {boon.Id}", boon.Description);
			Check($"boon {boon.Id}'s effect", BoonCatalog.ExplainEffect(boon));
		}
		foreach (string itemId in ChestItemCatalog.AllItemIds)
			Check($"relic {itemId}", ChestItemCatalog.GetDescription(itemId));
	}

	/// <summary>
	/// Every level 4, level 8 and Apotheosis branch says what it does within
	/// <see cref="SpellEvolutionOption.DescriptionBudget"/> characters, as the card shows it.
	/// </summary>
	/// <remarks>
	/// The level-up, mutation and Ascension screens never scroll (2026-10-31, Eric: "Overall keep text
	/// minimal"; .ai/level-up-screen.md section 3b). Their cards size to their words, so a branch written
	/// at length is a page that scrolls on a 1280x720 PC or a phone - and the old catalog ran to 385.
	/// </remarks>
	private static void ValidateCardTextBudget(List<string> warnings)
	{
		var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (SpellData spell in ContentValidator.LoadAllSpellForms())
		{
			if (spell == null || string.IsNullOrWhiteSpace(spell.Id))
				continue;
			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			var options = new List<SpellEvolutionOption>();
			if (spell.Level4Options != null)
				options.AddRange(spell.Level4Options);
			if (spell.Level8Options != null)
				options.AddRange(spell.Level8Options);
			foreach (ApotheosisDefinition definition in ApotheosisCatalog.All)
			{
				if (definition.Form != null && definition.SpellId.Equals(spell.Id, StringComparison.OrdinalIgnoreCase))
					options.Add(definition.Form);
			}
			foreach (SpellEvolutionOption option in options)
			{
				int length = option?.Description?.Length ?? 0;
				if (length > SpellEvolutionOption.DescriptionBudget && reported.Add($"{spell.Id}:{option.Id}"))
					warnings.Add($"Branch '{option.Id}' of '{spell.Id}' is {length} characters; a card holds " +
						$"{SpellEvolutionOption.DescriptionBudget} without scrolling: \"{option.Description}\"");
			}
		}
	}

	private static void ValidateEvolutionReaders(List<string> warnings)
	{
		var unbuilt = new HashSet<(string, EvolutionStat, SpellEffect)>();
		foreach (UnbuiltPromise promise in EvolutionStatReaders.KnownUnbuilt)
			unbuilt.Add((promise.OptionId.ToLowerInvariant(), promise.Stat, promise.Effect));
		var stillUnbuilt = new HashSet<(string, EvolutionStat, SpellEffect)>();

		// Every form of a spell taken in one, against its form's row (EvolutionStatReaders.TryGet(SpellData)).
		foreach (SpellData spell in ContentValidator.LoadAllSpellForms())
		{
			// The Sovereign Rite casts nothing and is never held; it has no script to read a field.
			if (spell == null || string.IsNullOrWhiteSpace(spell.Id) || ComboCatalog.ForId(spell.Id)?.IsRite == true)
				continue;
			if (!EvolutionStatReaders.TryGet(spell, out SpellStatReaders readers))
			{
				warnings.Add($"Spell '{ContentValidator.Label(spell)}' has no entry in EvolutionStatReaders, so nothing checks that its evolutions' numbers reach it. Add the fields its script reads.");
				continue;
			}

			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			var options = new List<SpellEvolutionOption>();
			if (spell.Level4Options != null)
				options.AddRange(spell.Level4Options.ToList());
			if (spell.Level8Options != null)
				options.AddRange(spell.Level8Options.ToList());
			foreach (ApotheosisDefinition definition in ApotheosisCatalog.All)
			{
				if (definition.Form != null && definition.SpellId.Equals(spell.Id, StringComparison.OrdinalIgnoreCase)
					&& (string.IsNullOrEmpty(definition.VariantId) || definition.VariantId.Equals(spell.SelectedVariantId, StringComparison.OrdinalIgnoreCase)))
					options.Add(definition.Form);
			}

			foreach (SpellEvolutionOption option in options)
			{
				if (option == null)
					continue;
				string id = option.Id.ToLowerInvariant();
				EvolutionStat set = EvolutionStatReaders.StatsSetBy(option);
				EvolutionStat unread = set & ~readers.Stats & ~EvolutionStat.Effect;
				// A rider's numbers with no rider to carry them are read by nothing, whatever the spell.
				if (option.Rider == ElementRider.None && (set & EvolutionStat.Rider) != 0)
					unread |= EvolutionStat.Rider;

				foreach (EvolutionStat stat in Enum.GetValues<EvolutionStat>())
				{
					if (stat == EvolutionStat.None || stat == EvolutionStat.Effect || (unread & stat) == 0)
						continue;
					ReportUnread(warnings, unbuilt, stillUnbuilt, spell.Id, readers, option, (id, stat, SpellEffect.None), EvolutionStatField(stat));
				}

				if (option.Effect != SpellEffect.None && !readers.ReadsEffect(option.Effect))
					ReportUnread(warnings, unbuilt, stillUnbuilt, spell.Id, readers, option, (id, EvolutionStat.Effect, option.Effect), $"Effect {option.Effect}");
				else if (option.Effect == SpellEffect.None && MathF.Abs(option.EffectValue) > 0.0001f)
					ReportUnread(warnings, unbuilt, stillUnbuilt, spell.Id, readers, option, (id, EvolutionStat.Effect, SpellEffect.None), $"an EffectValue of {option.EffectValue} with no Effect");
			}
		}

		foreach (var entry in unbuilt)
		{
			if (!stillUnbuilt.Contains(entry))
			{
				string field = entry.Item2 != EvolutionStat.Effect ? EvolutionStatField(entry.Item2)
					: entry.Item3 == SpellEffect.None ? "an EffectValue with no Effect" : $"Effect {entry.Item3}";
				warnings.Add($"EvolutionStatReaders.KnownUnbuilt lists '{entry.Item1}' for {field}, which is no longer an unread number on any card - it was built, the option stopped setting it, or the option is gone. Take it off the list.");
			}
		}
	}

	// The same rule for the ordinary level-ups, levels 2 to 8: every number a level-up card shows must be
	// one its spell reads. ValidateEvolutionReaders covered only the milestone cards, and the level table
	// is half data (the .tres) and half code (Player.EnsureRelevantLevelUps adds a level-up per scaling
	// tag), so this checks the table after Player.PrepareSpellLevelUps, as a run sees it. Added
	// 2026-10-26, when the milestone cards' list of unbuilt promises reached zero.
	private static void ValidateLevelUpReaders(List<string> warnings)
	{
		// Every form's own level table (a form replaces the spell's, SpellVariants.Apply), against its row.
		foreach (SpellData spell in ContentValidator.LoadAllSpellForms())
		{
			if (spell == null || string.IsNullOrWhiteSpace(spell.Id) || spell.IsPassive)
				continue;
			if (!EvolutionStatReaders.TryGet(spell, out SpellStatReaders readers))
				continue; // ValidateEvolutionReaders reports the missing entry

			Player.PrepareSpellLevelUps(spell);
			var reported = new HashSet<string>();
			string label = ContentValidator.Label(spell);
			foreach (SpellLevelUpgrade upgrade in spell.LevelUpgrades)
			{
				if (upgrade == null)
					continue;
				void Check(bool set, EvolutionStat stat, string field)
				{
					if (set && (readers.Stats & stat) == 0 && reported.Add(field))
						warnings.Add($"Spell '{label}' level {upgrade.Level} grants {field}, which it never reads ({readers.ReadIn}). A level-up must change the spell it levels.");
				}
				Check(upgrade.DamageBonus != 0, EvolutionStat.Damage, "DamageBonus");
				Check(MathF.Abs(upgrade.CooldownBonus) > 0.0001f, EvolutionStat.Cooldown, "CooldownBonus");
				Check(upgrade.ProjectileCountBonus != 0, EvolutionStat.Projectiles, "ProjectileCountBonus");
				Check(MathF.Abs(upgrade.RangeBonus) > 0.0001f, EvolutionStat.Range, "RangeBonus");
				Check(upgrade.ChainArcBonus != 0, EvolutionStat.ChainArcs, "ChainArcBonus");
				Check(upgrade.ChainBranchBonus != 0, EvolutionStat.ChainBranches, "ChainBranchBonus");
				// Read only by ChainLightning, beside its arcs.
				Check(MathF.Abs(upgrade.ChainChanceBonus) > 0.0001f, EvolutionStat.ChainArcs, "ChainChanceBonus");
				Check(upgrade.PoisonTickBonus != 0, EvolutionStat.PoisonTick, "PoisonTickBonus");
				if (upgrade.Effect != SpellEffect.None && !readers.ReadsEffect(upgrade.Effect) && reported.Add(upgrade.Effect.ToString()))
					warnings.Add($"Spell '{label}' level {upgrade.Level} grants Effect {upgrade.Effect} {upgrade.EffectValue}, which it never reads ({readers.ReadIn}). A level-up must change the spell it levels.");
			}
		}
	}

	// The Split Lens (0.9.3, Eric: "it should only reduce damage of spells that are affected by the
	// projectile count"). One row decides both halves of its trade - EvolutionStat.Amount, which
	// Player.AmountFor pays the +1 by and Player.GetSpellDamageMultiplier charges the -25% by - so this checks
	// the trade as a cast sees it: a Player that never enters a tree takes the lens, and every spell form
	// must come out charged exactly when it is paid, untouched otherwise. It also checks what a row cannot
	// say for itself: a spell that reads Amount has a count to add it to, and a milestone named as dropping
	// Amount is one the spell offers. A cast site that starts reading Amount without its row saying so is
	// reported by AmountFor the first time it fires, and gets nothing until it does.
	private static void ValidateAmountReaders(List<string> warnings)
	{
		var player = new Player();
		try
		{
			var spells = ContentValidator.LoadAllSpellForms()
				.Where(s => s != null && !string.IsNullOrWhiteSpace(s.Id) && EvolutionStatReaders.TryGet(s, out _)).ToList();
			var before = new Dictionary<SpellData, (float Damage, int Amount)>();
			foreach (SpellData spell in spells)
				before[spell] = player.DebugCastTerms(spell);
			player.DebugTakeChestItem(ChestItemCatalog.CrackedPrism);

			var charged = new List<string>();
			int spared = 0;
			foreach (SpellData spell in spells)
			{
				EvolutionStatReaders.TryGet(spell, out SpellStatReaders row);
				string label = ContentValidator.Label(spell);
				bool reads = row.ReadsAmount(spell);
				(float damage, int amount) = player.DebugCastTerms(spell);
				float ratio = before[spell].Damage > 0f ? damage / before[spell].Damage : 0f;
				int gained = amount - before[spell].Amount;
				float expected = reads ? Player.SplitLensDamageMultiplier : 1f;
				if (MathF.Abs(ratio - expected) > 0.001f || gained != (reads ? 1 : 0))
					warnings.Add($"Split Lens on '{label}': damage x{ratio:0.###} and {gained:+0;-0;0} projectile, where a spell that "
						+ $"{(reads ? "reads" : "does not read")} Amount should get x{expected:0.##} and {(reads ? "+1" : "none")}. "
						+ "The lens must charge exactly the spells it pays (Player.GetSplitLensMultiplier, AmountFor).");
				if (reads)
					charged.Add(spell.Id);
				else
					spared++;

				if ((row.Stats & EvolutionStat.Amount) != 0 && (row.Stats & EvolutionStat.Projectiles) == 0)
					warnings.Add($"Spell '{label}' reads Amount but no projectile count of its own ({row.ReadIn}): Amount is added to that count.");
				if (row.AmountDroppedBy.Count > 0)
				{
					SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
					foreach (string id in row.AmountDroppedBy)
					{
						SpellEvolutionOption milestone = spell.Level4Options?.FirstOrDefault(o => o?.Id == id)
							?? spell.Level8Options?.FirstOrDefault(o => o?.Id == id);
						if ((row.Stats & EvolutionStat.Amount) == 0 || milestone == null)
						{
							warnings.Add($"Spell '{label}' names '{id}' as dropping Amount, but {((row.Stats & EvolutionStat.Amount) == 0 ? "it never reads Amount" : "offers no such milestone")}.");
							continue;
						}
						// Held, the milestone frees the spell from the lens: by its id alone (HasEvolution), put
						// back at once - these are the loaded resources a run plays.
						bool atFour = milestone.MilestoneLevel == 4;
						string held = atFour ? spell.SelectedLevel4EvolutionId : spell.SelectedLevel8EvolutionId;
						try
						{
							if (atFour) spell.SelectedLevel4EvolutionId = id; else spell.SelectedLevel8EvolutionId = id;
							(float heldDamage, int heldAmount) = player.DebugCastTerms(spell);
							float heldRatio = before[spell].Damage > 0f ? heldDamage / before[spell].Damage : 0f;
							if (MathF.Abs(heldRatio - 1f) > 0.001f || heldAmount != 0)
								warnings.Add($"Split Lens on '{label}' holding '{id}': damage x{heldRatio:0.###} and {heldAmount} projectile from Amount, "
									+ "where a milestone that drops Amount should leave it untouched by the lens.");
						}
						finally
						{
							if (atFour) spell.SelectedLevel4EvolutionId = held; else spell.SelectedLevel8EvolutionId = held;
						}
					}
				}
			}
			// A row edit that flagged every spell, or none, would turn the trade back into a flat tax or a gift.
			if (charged.Count == 0 || spared == 0)
				warnings.Add($"Split Lens charges {charged.Count} spell forms and spares {spared}: it should be a trade some spells take and others do not.");
		}
		finally
		{
			player.Free();
		}
	}

	private static void ReportUnread(List<string> warnings, HashSet<(string, EvolutionStat, SpellEffect)> unbuilt,
		HashSet<(string, EvolutionStat, SpellEffect)> stillUnbuilt, string spellId, SpellStatReaders readers,
		SpellEvolutionOption option, (string, EvolutionStat, SpellEffect) key, string what)
	{
		if (unbuilt.Contains(key))
		{
			stillUnbuilt.Add(key);
			return;
		}
		warnings.Add($"Evolution '{option.Id}' on '{spellId}' sets {what}, which '{spellId}' never reads ({readers.ReadIn}). "
			+ "A number on a card must reach the spell: give the option a stat the spell reads, or make the spell read this one and add it to its EvolutionStatReaders entry.");
	}

	private static string EvolutionStatField(EvolutionStat stat) => stat switch
	{
		EvolutionStat.Damage => "DamageBonus/DamageMultiplier",
		EvolutionStat.Cooldown => "CooldownBonus/CooldownMultiplier",
		EvolutionStat.Projectiles => "ProjectileCountBonus",
		EvolutionStat.Range => "RangeBonus",
		EvolutionStat.Area => "AreaMultiplier",
		EvolutionStat.Pierce => "PierceBonus",
		EvolutionStat.ChainArcs => "ChainArcBonus",
		EvolutionStat.ChainBranches => "ChainBranchBonus",
		EvolutionStat.Knockback => "KnockbackBonus",
		EvolutionStat.Slow => "SlowMagnitudeBonus",
		EvolutionStat.PoisonTick => "PoisonTickBonus",
		EvolutionStat.Speed => "SpeedMultiplier",
		EvolutionStat.Scale => "ScaleMultiplier",
		EvolutionStat.Rider => "Rider/RiderMagnitude/RiderSeconds",
		_ => stat.ToString(),
	};

	// THE RULE, AS ERIC SET IT (2026-11-01, .ai/chapter-wizards.md section 0): "Only default starting wizards
	// that are unlocked on a new save file need to have mono element spells. Unlockable wizards can have
	// multi-element starting spells." So the one-element rule, and the one-owner-per-element rule, bind the
	// STARTERS only - the four disciplines a new save holds, which are what make picking a wizard picking an
	// element. A wizard the player frees (R2) may open on a hybrid, which never names more than two elements,
	// and may share an element with anyone (R1); what no two characters may share is the opening itself - one
	// spell, in one form - or one of them is the other in a different coat.
	private static void ValidateStartingCharacters(List<string> warnings)
	{
		var elementOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var openingOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (CharacterData character in CharacterRoster.GetAll())
		{
			// The Test Wizard picks its loadout on the selection screen, so it has no fixed
			// starting element to check and is exempt by design.
			if (character == null || character.Id.Equals("test_wizard", StringComparison.OrdinalIgnoreCase))
				continue;

			if (!string.IsNullOrWhiteSpace(character.StartingPassiveId)
				&& !Player.CharacterPassiveBonusIds.Contains(character.StartingPassiveId.Trim()))
			{
				warnings.Add($"Character '{character.Id}' names starting passive '{character.StartingPassiveId}', which is not a recognised bonus id, so it silently grants nothing.");
			}

			SpellData starter = character.StartingSpellResource;
			if (starter == null)
				continue;

			// No unlock check on the starting spell any more: Player.GetLevelUpOptions always offers a
			// character its own starting spell, which the rescued wizards need - they open on spells
			// the save may not own yet (.ai/side-events.md).

			// One spell in one form per character, whoever he is (R1). The form is the Apotheosis's: a wizard
			// who opens on a spell with forms has one per form (ApotheosisDefinition.VariantId).
			ApotheosisDefinition form = ApotheosisCatalog.ForCharacter(character.Id);
			string opening = $"{starter.Id}:{form?.VariantId ?? string.Empty}";
			if (openingOwners.TryGetValue(opening, out string twin))
				warnings.Add($"Characters '{twin}' and '{character.Id}' both open on '{starter.Id}'{(string.IsNullOrEmpty(form?.VariantId) ? "" : $" as '{form.VariantId}'")}; two wizards may share an element, never an opening spell.");
			else
				openingOwners[opening] = character.Id;

			var weights = starter.ElementWeights;
			if (weights == null || weights.Count == 0)
			{
				warnings.Add($"Character '{character.Id}' starts with '{starter.Id}', which carries no element weight, so the character opens pointed at no element at all.");
				continue;
			}

			// A freed wizard (R2): a hybrid is allowed, but never past the two-element ceiling, and the element
			// his Apotheosis claims must be one of the two (ValidateApotheosisCatalog holds the path to it).
			bool isStarter = UnlockCatalog.IsStarter(character.Id, UnlockKind.Character);
			if (!isStarter)
			{
				if (weights.Count > 2)
					warnings.Add($"Character '{character.Id}' starts with '{starter.Id}', which carries {weights.Count} elements; no spell may name more than two.");
				continue;
			}

			if (weights.Count > 1)
			{
				warnings.Add($"Starter '{character.Id}' starts with '{starter.Id}', which carries {weights.Count} elements; a wizard a new save holds must open on exactly one, so picking him is picking an element.");
				continue;
			}

			string element = string.Empty;
			foreach (Variant key in weights.Keys)
			{
				element = key.AsString();
				break;
			}

			if (elementOwners.TryGetValue(element, out string owner))
				warnings.Add($"Starters '{owner}' and '{character.Id}' both open on {element}; each wizard a new save holds should claim a different element.");
			else
				elementOwners[element] = character.Id;
		}
	}

	// The check that would have caught the whole dead economy.
	//
	// Every spell and every achievement reward used to line up perfectly on paper while granting
	// nothing at all, because the thing that decided what was locked - a 35-entry default-unlocked
	// set - lived somewhere else and contained everything. Nothing compared the two. These checks
	// are all cross-references between catalogs that must agree, and every one of them fails
	// silently in play: an unreachable spell just never appears, and a reward that grants something
	// the player already owns looks exactly like one that worked.
	private static void ValidateUnlockCatalog(List<string> warnings)
	{
		var seen = new Dictionary<string, UnlockDefinition>(StringComparer.OrdinalIgnoreCase);
		foreach (UnlockDefinition definition in UnlockCatalog.All)
		{
			string key = $"{definition.Kind}:{definition.Id}";
			if (seen.ContainsKey(key))
				warnings.Add($"Unlock catalog has two entries for {definition.Kind} '{definition.Id}'; each thing must have exactly one source.");
			else
				seen[key] = definition;

			if (definition.Source != UnlockSource.Starter && string.IsNullOrWhiteSpace(definition.LockedHint))
				warnings.Add($"Unlock '{definition.Id}' has no LockedHint, so its locked card cannot tell the player how to get it.");

			if (definition.Source == UnlockSource.Purchase && definition.PurchaseCost <= 0)
				warnings.Add($"Unlock '{definition.Id}' is purchasable for {definition.PurchaseCost} Arcane Energy, so it is free.");

			if (definition.Kind == UnlockKind.Boon && BoonCatalog.GetById(definition.Id) == null)
				warnings.Add($"Unlock catalog lists boon '{definition.Id}', which BoonCatalog does not define.");

			if (definition.Source == UnlockSource.Achievement
				&& AchievementDefinitions.GetById(definition.SourceId) == null)
			{
				warnings.Add($"Unlock '{definition.Id}' points at achievement '{definition.SourceId}', which does not exist, so it can never be earned.");
			}

			// EVERY SPELL IS EARNED (.ai/achievements-and-secrets.md section 2): a spell is a starter or an
			// achievement's - a Feat, a chapter's deed or a secret - and a wizard the same. The shop sells
			// boons only, and Discovery is retired: a side event's unlock is its Road deed's reward.
			if (definition.Source == UnlockSource.Discovery)
				warnings.Add($"Unlock '{definition.Id}' is sourced Discovery, which is retired; source it from its side event's Road deed (AchievementDefinitions.EventDeedId) with its SiteId.");
			else if (definition.Kind != UnlockKind.Boon && definition.Source == UnlockSource.Purchase)
				warnings.Add($"{definition.Kind} '{definition.Id}' is bought with Arcane Energy; since schema 19 every {definition.Kind.ToString().ToLowerInvariant()} is a starter or an achievement's.");

			// A side event's unlock names its site, and its source is that site's deed - the deed reads
			// its reward from the site, so the two must agree or the event pays nothing.
			if (!string.IsNullOrWhiteSpace(definition.SiteId)
				&& !definition.SourceId.Equals(AchievementDefinitions.EventDeedId(definition.SiteId), StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Unlock '{definition.Id}' is paid at site '{definition.SiteId}' but sourced from '{definition.SourceId}', not that site's deed '{AchievementDefinitions.EventDeedId(definition.SiteId)}'.");

			// A spell a vault pays: its locked hint is the Spell Index's line for it, and must be the
			// deed's sealed line - the chapter, never the place or the verb (decision 7).
			AchievementDefinition source = UnlockCatalog.SourceDeedOf(definition);
			if (source != null && source.Kind == AchievementKind.Secret
				&& !definition.LockedHint.TrimEnd('.').Equals(source.SealedLine, StringComparison.Ordinal))
				warnings.Add($"Unlock '{definition.Id}' is hidden in a secret, and its hint \"{definition.LockedHint}\" is not its deed's sealed line \"{source.SealedLine}.\"");

			ValidateSecret(definition, warnings);
		}

		// A side event's site pays at most one thing of each kind: its deed has one field per kind.
		foreach (IGrouping<string, UnlockDefinition> site in UnlockCatalog.All
			.Where(d => !string.IsNullOrWhiteSpace(d.SiteId)).GroupBy(d => d.SiteId, StringComparer.OrdinalIgnoreCase))
		{
			foreach (IGrouping<UnlockKind, UnlockDefinition> kind in site.GroupBy(d => d.Kind))
				if (kind.Count() > 1)
					warnings.Add($"Site '{site.Key}' pays {kind.Count()} {kind.Key}s; its deed carries one of each kind.");
		}

		// Every spell the game can offer needs a source. A spell with no entry is locked forever -
		// which is the safe direction, but only if somebody is told about it.
		foreach (string path in ContentValidator.SpellResourcePaths)
		{
			var spell = GD.Load<SpellData>(path);
			if (spell == null || string.IsNullOrWhiteSpace(spell.Id))
				continue;

			if (UnlockCatalog.GetSpell(spell.Id) == null)
				warnings.Add($"Spell '{spell.Id}' has no unlock catalog entry, so it can never be offered at level-up.");
		}

		foreach (CharacterData character in CharacterRoster.GetAll())
		{
			if (character != null && UnlockCatalog.GetCharacter(character.Id) == null)
				warnings.Add($"Character '{character.Id}' has no unlock catalog entry, so it can never be selected.");
		}

		// The other direction: an achievement that grants something the catalog says comes from
		// somewhere else. Both halves would look correct on their own.
		foreach (AchievementDefinition achievement in AchievementDefinitions.All)
		{
			// A tool run's stand-in second event pays a shop boon on purpose (SecondEventStandIn); it is reported
			// once, by ValidateSideEventTable, not here as a fault in the catalog.
			if (SecondEventStandIn.IsStandInSite(achievement.SiteId))
				continue;
			// A wizard freed by an achievement (John Dee) keeps the same agreement a spell does.
			if (!string.IsNullOrWhiteSpace(achievement.CharacterUnlockId))
			{
				UnlockDefinition wizard = UnlockCatalog.GetCharacter(achievement.CharacterUnlockId);
				if (wizard != null && (wizard.Source != UnlockSource.Achievement || !wizard.SourceId.Equals(achievement.Id, StringComparison.OrdinalIgnoreCase)))
					warnings.Add($"Achievement '{achievement.Id}' frees '{achievement.CharacterUnlockId}', but the catalog sources that wizard from {wizard.Source} '{wizard.SourceId}' - one of the two is wrong.");
			}

			// And a boon (the Long Coil's, an event's), the same agreement.
			if (!string.IsNullOrWhiteSpace(achievement.BoonUnlockId))
			{
				UnlockDefinition boon = UnlockCatalog.Get(achievement.BoonUnlockId, UnlockKind.Boon);
				if (boon == null)
					warnings.Add($"Achievement '{achievement.Id}' grants boon '{achievement.BoonUnlockId}', which has no unlock catalog entry.");
				else if (boon.Source != UnlockSource.Achievement || !boon.SourceId.Equals(achievement.Id, StringComparison.OrdinalIgnoreCase))
					warnings.Add($"Achievement '{achievement.Id}' grants '{achievement.BoonUnlockId}', but the catalog sources that boon from {boon.Source} '{boon.SourceId}' - one of the two is wrong.");
			}

			if (string.IsNullOrWhiteSpace(achievement.SpellUnlockId))
				continue;

			UnlockDefinition definition = UnlockCatalog.GetSpell(achievement.SpellUnlockId);
			if (definition == null)
				warnings.Add($"Achievement '{achievement.Id}' grants spell '{achievement.SpellUnlockId}', which has no unlock catalog entry.");
			else if (definition.Source != UnlockSource.Achievement || !definition.SourceId.Equals(achievement.Id, StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Achievement '{achievement.Id}' grants '{achievement.SpellUnlockId}', but the catalog sources that from {definition.Source} '{definition.SourceId}' - one of the two is wrong.");
		}

		// A fresh save must be able to start: at least one chapter open, and every starting
		// character's opening spell available on turn one.
		var fresh = new SaveData();
		if (!StageCatalog.All.Any(s => GlobalStatsManager.IsStageAvailable(fresh, s)))
			warnings.Add("No chapter is available on a new save, so a fresh game cannot be started.");

		foreach (StageDefinition gate in StageCatalog.GateChapters)
		{
			if (GlobalStatsManager.IsGateMet(fresh, gate) || GlobalStatsManager.IsStageEarned(fresh, gate))
				warnings.Add($"A brand-new save already satisfies {gate.DisplayName}'s gate, so it is not gated by anything.");
		}

		// The other direction, for every spell: its achievement exists and grants it. The forward check
		// above walks the deeds; this one walks the spells, so a spell pointing at a deed that pays
		// something else is caught from its own side too.
		foreach (UnlockDefinition spell in UnlockCatalog.All.Where(d => d.Kind == UnlockKind.Spell && d.Source == UnlockSource.Achievement))
		{
			AchievementDefinition deed = AchievementDefinitions.GetById(spell.SourceId);
			if (deed != null && !deed.SpellUnlockId.Equals(spell.Id, StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Spell '{spell.Id}' is sourced from '{deed.Id}', which grants '{deed.SpellUnlockId}' instead, so it can never be earned.");
		}

		ValidateCuration(fresh, warnings);
	}

	// A secret wizard's card is a sealed blank until its reveal condition holds (UnlockDefinition.
	// IsSecret, .ai/side-events.md "Secret wizards"). Every way that goes wrong is silent: a secret
	// with no reveal, or one pointing at a chapter that does not exist or never opens, is a card that
	// stays sealed for ever and looks exactly like one the player has not reached yet.
	private static void ValidateSecret(UnlockDefinition definition, List<string> warnings)
	{
		if (!definition.IsSecret)
		{
			if (!string.IsNullOrWhiteSpace(definition.RevealStageId))
				warnings.Add($"Unlock '{definition.Id}' names a reveal chapter '{definition.RevealStageId}' but is not secret, so nothing reads it.");
			return;
		}

		if (definition.Kind != UnlockKind.Character)
			warnings.Add($"Unlock '{definition.Id}' is a secret {definition.Kind}; only the character select reads IsSecret, so it would show as an ordinary lock.");
		if (definition.Source == UnlockSource.Starter)
			warnings.Add($"Starter '{definition.Id}' is marked secret; a starter is never locked, so the secret can never show.");

		if (string.IsNullOrWhiteSpace(definition.RevealStageId))
		{
			warnings.Add($"Secret '{definition.Id}' has no reveal condition (RevealStageId), so its card stays sealed for ever.");
			return;
		}

		StageDefinition stage = StageCatalog.GetById(definition.RevealStageId);
		if (stage == null)
		{
			warnings.Add($"Secret '{definition.Id}' is revealed by chapter '{definition.RevealStageId}', which StageCatalog does not define, so its card stays sealed for ever.");
			return;
		}
		if (!stage.IsPlayable)
			warnings.Add($"Secret '{definition.Id}' is revealed by '{stage.DisplayName}', which is not playable yet, so its card stays sealed.");
		// A gate opens only once the wizards it counts are free, and a wizard nobody has seen cannot be
		// looked for: a wizard a gate counts must be revealed by a chapter BEFORE that gate, or he would
		// stay sealed until after he was needed (.ai/endgame-castle.md 1.6, generalised from the one gate).
		if (definition.Kind == UnlockKind.Character)
		{
			int revealAt = StageCatalog.CampaignPositionOf(stage.Id);
			foreach (StageDefinition gate in StageCatalog.GateChapters)
			{
				if (GlobalStatsManager.CountsTowardGate(definition.Id, gate)
					&& (revealAt < 0 ? !StageCatalog.IsOpenFromStart(stage) : revealAt >= StageCatalog.CampaignPositionOf(gate.Id)))
					warnings.Add($"Secret '{definition.Id}' is counted by {gate.DisplayName}'s gate but revealed by '{stage.DisplayName}', which is not before it - he would stay sealed until after he was needed.");
			}
		}
		if (GlobalStatsManager.IsCharacterRevealed(new SaveData(), definition.Id) && !StageCatalog.IsOpenFromStart(stage))
			warnings.Add($"Secret '{definition.Id}' reads as revealed on a brand-new save although '{stage.DisplayName}' is closed on one; IsCharacterRevealed has stopped honouring the reveal.");

		// A wizard found at a side event is revealed by the chapter that holds him: the place the
		// player is shown is the place he is freed from. Anything else hints at the wrong map. Read off his
		// own site - a chapter may hold two events (.ai/chapter-wizards.md 6.3), and his is the one that counts.
		if (!string.IsNullOrWhiteSpace(definition.SiteId) && !SideEventDirector.HoldsSite(stage.Id, definition.SiteId))
		{
			StageDefinition holder = SideEventDirector.ChapterOfSite(definition.SiteId);
			string events = string.Join(", ", SideEventDirector.EventsFor(stage.Id).Select(e => $"'{e.SiteId}'"));
			warnings.Add($"Secret '{definition.Id}' is found at '{definition.SiteId}'"
				+ (holder != null ? $" in '{holder.DisplayName}'" : ", which no chapter's event stands on,")
				+ $" but revealed by '{stage.DisplayName}', whose side events are {(events.Length > 0 ? events : "none")}; reveal him with the chapter that holds him.");
		}
	}

	// Achievements are measured, not asserted: one function produces both "am I done" and "how far
	// am I", so they cannot disagree. What can still go wrong is a definition that is unreachable,
	// unmeasurable, or already satisfied - and every one of those is invisible in play, because an
	// achievement that never fires looks exactly like one the player has not earned yet.
	private static void ValidateAchievements(List<string> warnings)
	{
		var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var fresh = AchievementContext.ForSave(new SaveData());

		foreach (AchievementDefinition achievement in AchievementDefinitions.All)
		{
			if (string.IsNullOrWhiteSpace(achievement.Id))
			{
				warnings.Add("An achievement has no Id, so it can never be recorded as unlocked.");
				continue;
			}

			if (!ids.Add(achievement.Id))
				warnings.Add($"Two achievements share the id '{achievement.Id}'; the second can never be earned separately.");

			if (achievement.Target <= 0f)
				warnings.Add($"Achievement '{achievement.Id}' has a target of {achievement.Target}, so it completes instantly.");

			if (achievement.Measure == null)
			{
				warnings.Add($"Achievement '{achievement.Id}' has no Measure, so it can never complete.");
				continue;
			}

			// A brand-new save must satisfy nothing. One that does is either mis-measured or has a
			// target of zero, and would fire on the player's very first run for no reason.
			if (achievement.IsComplete(fresh))
				warnings.Add($"Achievement '{achievement.Id}' is already complete on a new save.");

			// A secret's deed always pays its mark on the page (the found level or Arcane lump of a
			// keepsake is SecretDirector's to pay, never the deed's - so it can never be paid twice).
			bool grantsSomething = !string.IsNullOrWhiteSpace(achievement.SpellUnlockId)
				|| !string.IsNullOrWhiteSpace(achievement.BoonUnlockId)
				|| !string.IsNullOrWhiteSpace(achievement.CharacterUnlockId)
				|| achievement.CurrencyReward > 0
				|| achievement.Kind == AchievementKind.Secret;
			if (!grantsSomething)
				warnings.Add($"Achievement '{achievement.Id}' grants nothing, so completing it is not a reward.");

			if (!string.IsNullOrWhiteSpace(achievement.CharacterUnlockId)
				&& UnlockCatalog.GetCharacter(achievement.CharacterUnlockId) == null)
			{
				warnings.Add($"Achievement '{achievement.Id}' frees '{achievement.CharacterUnlockId}', which has no unlock catalog entry.");
			}

			ValidateAchievementShape(achievement, warnings);
		}

		// Derivation is complete in both directions: every side event's site and every secret has its
		// deed, and the words table names no site that has none (a renamed site would leave words behind).
		var sites = global::SideEventDirector.AllEvents.Select(e => e.Entry.SiteId)
			.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
		foreach (string site in sites)
		{
			if (AchievementDefinitions.GetById(AchievementDefinitions.EventDeedId(site)) == null)
				warnings.Add($"Achievements: the side event at '{site}' has no deed, so winning it records nothing.");
			if (!AchievementDefinitions.HasSideEventWords(site))
				warnings.Add($"Achievements: the side event at '{site}' has no name, line or reward words (AchievementDefinitions.SideEventWords).");
		}
		foreach (string site in AchievementDefinitions.SideEventWordSites)
		{
			if (!sites.Contains(site, StringComparer.OrdinalIgnoreCase))
				warnings.Add($"Achievements: there are words for a side event at '{site}', which no chapter's side event stands on.");
		}
		foreach (SecretDefinition secret in SecretCatalog.Defined)
		{
			if (AchievementDefinitions.GetById(AchievementDefinitions.SecretDeedId(secret.Id)) == null)
				warnings.Add($"Achievements: the secret '{secret.Id}' has no deed, so finding it records nothing.");
		}

		// The reward words name what the deed actually grants (the derived deeds' words are written by
		// hand beside an id read from another catalog; a renamed spell or a moved reward would leave
		// the card saying the old thing).
		Dictionary<string, string> spellNames = ContentValidator.LoadAllSpells()
			.Where(s => s != null && !string.IsNullOrWhiteSpace(s.Id))
			.GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
		foreach (AchievementDefinition achievement in AchievementDefinitions.All)
		{
			var names = new List<string>();
			if (!string.IsNullOrWhiteSpace(achievement.SpellUnlockId))
				names.Add(spellNames.TryGetValue(achievement.SpellUnlockId, out string spell) ? spell : achievement.SpellUnlockId);
			if (!string.IsNullOrWhiteSpace(achievement.BoonUnlockId))
				names.Add(BoonCatalog.GetById(achievement.BoonUnlockId)?.Name ?? achievement.BoonUnlockId);
			if (!string.IsNullOrWhiteSpace(achievement.CharacterUnlockId))
				names.Add(CharacterRoster.GetAll().FirstOrDefault(c => c != null
					&& c.Id.Equals(achievement.CharacterUnlockId, StringComparison.OrdinalIgnoreCase))?.Name ?? achievement.CharacterUnlockId);
			foreach (string name in names)
			{
				if (achievement.RewardText.IndexOf(name, StringComparison.Ordinal) < 0)
					warnings.Add($"Achievement '{achievement.Id}' grants {name}, but its reward line \"{achievement.RewardText}\" does not say so.");
			}
		}
	}

	// The fields .ai/achievements-and-secrets.md 2.6 added must agree with each other, and every one of
	// them disagreeing is silent: a Hidden deed with no seen line is a blank row, a Road deed with no
	// chapter is grouped nowhere, a deed completed in the run with no fact to measure is never done.
	private static void ValidateAchievementShape(AchievementDefinition deed, List<string> warnings)
	{
		string id = deed.Id;
		if (string.IsNullOrWhiteSpace(deed.DisplayName) || string.IsNullOrWhiteSpace(deed.Description) || string.IsNullOrWhiteSpace(deed.RewardText))
			warnings.Add($"Achievement '{id}' has no name, line or reward words, so its row says nothing.");

		bool hasChapter = !string.IsNullOrWhiteSpace(deed.StageId);
		StageDefinition stage = StageCatalog.GetById(deed.StageId);
		if (hasChapter && stage == null)
			warnings.Add($"Achievement '{id}' belongs to '{deed.StageId}', which is not a chapter in StageCatalog.");

		switch (deed.Kind)
		{
			case AchievementKind.Feat:
			case AchievementKind.Collection:
				// A combination's Feat is the one Feat that waits: listed once the save owns both parents (Recipe).
				if (deed.Visibility != AchievementVisibility.Always
					&& !(deed.Kind == AchievementKind.Feat && deed.Visibility == AchievementVisibility.Recipe && !string.IsNullOrEmpty(deed.ComboId)))
					warnings.Add($"Achievement '{id}' is a {deed.Kind} shown {deed.Visibility}; a {deed.Kind} is listed from the first run.");
				break;
			case AchievementKind.Road:
				if (!hasChapter || deed.Visibility != AchievementVisibility.ChapterOpen)
					warnings.Add($"Achievement '{id}' is a Road deed with chapter '{deed.StageId}' shown {deed.Visibility}; a chapter's deed names its chapter and is listed once it is open.");
				break;
			case AchievementKind.Secret:
				if (!hasChapter || deed.Visibility != AchievementVisibility.Hidden || string.IsNullOrWhiteSpace(deed.SecretId))
					warnings.Add($"Achievement '{id}' is a Secret deed that is not Hidden, names no chapter or mirrors no secret.");
				break;
		}

		if (deed.Visibility == AchievementVisibility.Hidden)
		{
			if (string.IsNullOrWhiteSpace(deed.SealedLine) || string.IsNullOrWhiteSpace(deed.SeenLine))
				warnings.Add($"Achievement '{id}' is Hidden with no sealed line or no seen line (.ai/achievements-and-secrets.md 4.3), so its row has nothing to show before it is found.");
			if (AchievementDefinitions.IsListed(deed, new SaveData()))
				warnings.Add($"Achievement '{id}' is Hidden but listed on a brand-new save.");
		}

		// A deed for a secret wizard is sealed until his chapter opens, and says so with a line.
		if (AchievementDefinitions.IsSealed(deed, new SaveData()) && string.IsNullOrWhiteSpace(deed.SealedLine))
			warnings.Add($"Achievement '{id}' frees a secret wizard but has no sealed line for the row it shows before his chapter opens.");

		// A combination's Feat is completed in the run too, the first time its recipe is met (ComboCatalog).
		bool mirrors = !string.IsNullOrWhiteSpace(deed.SiteId) || !string.IsNullOrWhiteSpace(deed.SecretId)
			|| !string.IsNullOrWhiteSpace(deed.ComboId);
		if (deed.CompletesInRun != mirrors)
			warnings.Add($"Achievement '{id}' {(deed.CompletesInRun ? "completes in the run but mirrors no site or secret" : "mirrors a site or a secret but is not completed in the run")}.");
		if (!string.IsNullOrWhiteSpace(deed.SiteId) && !global::SideEventDirector.HoldsSite(deed.StageId, deed.SiteId))
			warnings.Add($"Achievement '{id}' mirrors site '{deed.SiteId}', which is not a side event of its chapter '{deed.StageId}'.");
		if (!string.IsNullOrWhiteSpace(deed.SecretId)
			&& SecretCatalog.Get(deed.SecretId)?.StageId.Equals(deed.StageId, StringComparison.OrdinalIgnoreCase) != true)
			warnings.Add($"Achievement '{id}' mirrors secret '{deed.SecretId}', which is not a secret of its chapter '{deed.StageId}'.");

		// The measure and the fact agree: recording the fact a derived deed mirrors completes it.
		if (mirrors)
		{
			var save = new SaveData();
			AchievementManager.Complete(save, id);
			if (!deed.IsComplete(AchievementContext.ForSave(save)) || !GlobalStatsManager.IsAchievementUnlocked(save, id))
				warnings.Add($"Achievement '{id}': completing it in the run does not satisfy its own measure, so the page and the save would disagree.");
		}
	}

	// Curation's two bounds are easy to break from opposite directions and both fail quietly: too
	// generous and a player can narrow the pool until every level-up is the same three cards, too
	// strict and the shop sells a row that can never be bought.
	private static void ValidateCuration(SaveData fresh, List<string> warnings)
	{
		if (GlobalStatsManager.GetCurationSlotCapacity(fresh) != 0)
		{
			warnings.Add("A new save can already buy Redactions; curation is meant to be unavailable "
				+ "until the spellbook has grown past the minimum offerable pool.");
		}

		// With every spell unlocked, capacity must still leave the floor intact.
		var full = new SaveData();
		foreach (string id in UnlockCatalog.AllSpellIds)
			GlobalStatsManager.UnlockSpell(full, id);

		int unlocked = GlobalStatsManager.GetUnlockedSpellCount(full);
		int capacity = GlobalStatsManager.GetCurationSlotCapacity(full);
		if (unlocked - capacity < GlobalStatsManager.MinimumOfferablePool)
		{
			warnings.Add($"With every spell unlocked ({unlocked}), curation capacity {capacity} would allow the "
				+ $"pool to fall below the {GlobalStatsManager.MinimumOfferablePool}-spell floor.");
		}

		// And the floor must actually hold when the player spends every slot.
		full.ArcaneUpgradeLevels[GlobalStatsManager.CurationUpgradeId] = capacity;
		int removed = 0;
		foreach (string id in UnlockCatalog.AllSpellIds.ToList())
		{
			if (GlobalStatsManager.SetSpellRemovedFromPool(full, id, true))
				removed++;
		}

		if (unlocked - removed < GlobalStatsManager.MinimumOfferablePool)
		{
			warnings.Add($"Spending every Redaction removed {removed} of {unlocked} spells, leaving fewer than "
				+ $"the {GlobalStatsManager.MinimumOfferablePool} the floor promises.");
		}
	}

	// THE QUEST LOG AND THE CHAPTER NOTES (.ai/quests-and-notes.md). What dotnet build cannot see: a
	// playable chapter with no note, a note too long for its card, a rescued character with no epithet to be
	// named by while he is held - and, above all, any line the log, the HUD, the run's opening or a note can
	// show that carries the name of a wizard the save has not freed. His name is his reward
	// (.ai/side-events.md, "Secret wizards"). Read on four saves: new, early (the Circle open), mid-campaign
	// (the Dungeon open, nobody freed), and late (one freed, one still held).
	private static void ValidateQuestsAndNotes(List<string> warnings)
	{
		foreach (StageDefinition stage in StageCatalog.All.Where(s => s.IsPlayable))
		{
			if (!ChapterNotes.Has(stage.Id))
				warnings.Add($"Chapter notes: {stage.DisplayName} ({stage.Id}) is playable and has no note (ChapterNotes).");
		}
		foreach (string id in ChapterNotes.WrittenFor)
		{
			if (StageCatalog.GetById(id) == null)
				warnings.Add($"Chapter notes: a note is written for '{id}', which StageCatalog does not define.");
		}

		var rescued = UnlockCatalog.AllCharacterIds.Where(id => !UnlockCatalog.IsStarter(id, UnlockKind.Character)).ToList();
		foreach (string id in rescued)
		{
			if (QuestLog.PrisonerOf(id) == null)
				warnings.Add($"Quests: '{id}' can be freed and has no epithet (QuestLog.Prisoners), so the log would call him 'a wizard'.");
		}
		foreach (QuestLog.Prisoner prisoner in QuestLog.Prisoners)
		{
			if (UnlockCatalog.GetCharacter(prisoner.CharacterId) == null)
				warnings.Add($"Quests: QuestLog.Prisoners names '{prisoner.CharacterId}', which UnlockCatalog does not.");
			if (!prisoner.FreedLine.Contains("{0}"))
				warnings.Add($"Quests: {prisoner.CharacterId}'s freed line does not name him.");
		}

		var saves = new List<(string Name, SaveData Save)> { ("a new save", new SaveData()) };
		var early = new SaveData();
		OpenThrough(early, "stage_10");
		saves.Add(("an early save", early));
		var middle = new SaveData();
		OpenThrough(middle, "stage_1");
		saves.Add(("a mid-campaign save", middle));
		var late = new SaveData();
		OpenThrough(late, "stage_5");
		GlobalStatsManager.UnlockCharacter(late, "tituba");
		saves.Add(("a late save", late));

		foreach (var (name, save) in saves)
		{
			var locked = rescued.Where(id => !GlobalStatsManager.IsCharacterUnlocked(save, id))
				.Select(id => (Id: id, Name: QuestLog.NameOf(id))).ToList();
			IEnumerable<string> text = QuestLog.AllText(save)
				.Concat(StageCatalog.All.Where(s => s.IsPlayable).SelectMany(s => ChapterNotes.LinesFor(s.Id, save)))
				.Concat(StageCatalog.All.Where(s => s.IsPlayable).SelectMany(s => ChapterNotes.CluesFor(s.Id, save)));
			foreach (string line in text.Where(t => !string.IsNullOrEmpty(t)))
			{
				foreach (var who in locked)
				{
					if (NamesWizard(line, who.Name))
						warnings.Add($"Quests: on {name}, '{line}' names {who.Id}, who is not free yet.");
				}
			}
			foreach (StageDefinition stage in StageCatalog.All.Where(s => s.IsPlayable && ChapterNotes.Has(s.Id)))
			{
				List<string> lines = ChapterNotes.LinesFor(stage.Id, save);
				if (lines.Count > ChapterNotes.MaxLinesFor(stage.Id))
					warnings.Add($"Chapter notes: {stage.DisplayName}'s note is {lines.Count} lines on {name}; at most {ChapterNotes.MaxLinesFor(stage.Id)} (two of the place, one a wizard held there).");
				if (ChapterNotes.IntroFor(stage.Id).Count > ChapterNotes.MaxIntroLines)
					warnings.Add($"Chapter notes: {stage.DisplayName}'s own lines are {ChapterNotes.IntroFor(stage.Id).Count}; at most {ChapterNotes.MaxIntroLines}.");
				foreach (string line in lines.Where(l => l.Length > ChapterNotes.MaxLineLength))
					warnings.Add($"Chapter notes: '{line}' is {line.Length} characters; at most {ChapterNotes.MaxLineLength}.");
				foreach (string clue in ChapterNotes.CluesFor(stage.Id, save).Where(l => l.Length > ChapterNotes.MaxLineLength))
					warnings.Add($"Chapter notes: the clue '{clue}' is {clue.Length} characters; at most {ChapterNotes.MaxLineLength}.");
			}
		}

		// The clues (ChapterNotes.CluesFor): each site clue belongs to a side event that pays something other
		// than a wizard, never names what it pays, and a new save gets a clue in every chapter that holds a
		// wizard, a site or a vault - and none once everything is owned (BuildEverythingSave's counterpart:
		// every unlock granted below).
		foreach (var (siteId, clue) in ChapterNotes.SiteClueLines)
		{
			var gives = UnlockCatalog.ForDiscoverySite(siteId).ToList();
			if (gives.Count == 0)
				warnings.Add($"Chapter notes: the clue for '{siteId}' belongs to no Discovery site.");
			foreach (UnlockDefinition give in gives)
			{
				string named = give.Kind == UnlockKind.Character
					? QuestLog.NameOf(give.Id)
					: ContentValidator.LoadAllSpells().FirstOrDefault(sp => sp.Id == give.Id)?.Name
						?? BoonCatalog.GetById(give.Id)?.Name ?? give.Id.Replace('_', ' ');
				if (!string.IsNullOrWhiteSpace(named) && clue.Contains(named, StringComparison.OrdinalIgnoreCase))
					warnings.Add($"Chapter notes: the clue '{clue}' names '{named}', which is what it pays.");
			}
		}
		foreach (SecretDefinition vault in SecretCatalog.All.Where(v => v.IsVault))
		{
			if (!string.IsNullOrWhiteSpace(vault.SpellName) && ChapterNotes.VaultClue.Contains(vault.SpellName, StringComparison.OrdinalIgnoreCase))
				warnings.Add($"Chapter notes: the vault clue names {vault.SpellName}.");
		}
		var fresh = new SaveData();
		foreach (StageDefinition stage in StageCatalog.All.Where(s => s.IsPlayable && ChapterNotes.Has(s.Id)))
		{
			bool holds = QuestLog.HeldOnlyLinesFor(stage.Id, fresh).Any()
				|| SideEventDirector.EventsFor(stage.Id).Count > 0
				|| SecretCatalog.ForStage(stage.Id).Any(v => v.IsVault);
			if (holds && ChapterNotes.CluesFor(stage.Id, fresh).Count == 0)
				warnings.Add($"Chapter notes: {stage.DisplayName} holds something on a new save but its note gives no clue.");
		}

		// The default: a new save tracks the next wizard, and he is somebody; a save that frees everyone has none.
		if (!new SaveData().TrackedQuestIds.Contains(QuestLog.NextWizardId))
			warnings.Add("Quests: a new save does not track the next wizard.");
		if (QuestLog.Resolve(QuestLog.NextWizardId, new SaveData(), null) == null)
			warnings.Add("Quests: a new save has no next wizard to free.");
		var everyone = new SaveData();
		foreach (string id in rescued)
			GlobalStatsManager.UnlockCharacter(everyone, id);
		if (QuestLog.NextWizard(everyone) != null)
			warnings.Add("Quests: with every wizard free, the next wizard is still somebody.");

		// Additive save fields: a save written before them keeps the defaults; an empty list stays empty.
		var old = new Godot.Collections.Dictionary { ["SchemaVersion"] = SaveData.CurrentSchemaVersion };
		SaveData read = SaveData.FromVariant(old);
		if (!read.TrackedQuestIds.Contains(QuestLog.NextWizardId) || read.SeenChapterNoteIds.Count != 0)
			warnings.Add("Quests: a save without the quest keys does not read as tracking the next wizard with no note seen.");
		var cleared = new SaveData();
		cleared.TrackedQuestIds.Clear();
		cleared.SeenChapterNoteIds.Add("stage_0");
		SaveData back = SaveData.FromVariant(cleared.ToGodotDictionary());
		if (back.TrackedQuestIds.Count != 0 || !back.SeenChapterNoteIds.Contains("stage_0"))
			warnings.Add("Quests: the tracked quests or the notes seen do not survive a save and load.");
	}

	// Opens every campaign chapter up to and including this one, with the boss of each one before it
	// beaten, as playing the road there does.
	private static void OpenThrough(SaveData save, string stageId)
	{
		foreach (StageDefinition stage in StageCatalog.Campaign.Where(s => s.IsPlayable))
		{
			GlobalStatsManager.UnlockStage(save, stage.Id);
			if (stage.Id.Equals(stageId, StringComparison.OrdinalIgnoreCase))
				return;
			BossDefinition boss = BossCatalog.ForStageIndex(stage.Index);
			if (boss != null)
				save.Lifetime.BossesDefeated.Add(boss.Id);
		}
	}

	/// <summary>
	/// Whether a line carries a name, with its diacritics or without - "Vainamoinen" is how a hand-written
	/// hint would spell it. Public for _UiShot's name audit, which reads a whole screen the same way.
	/// </summary>
	public static bool NamesWizard(string line, string name)
	{
		if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(line))
			return false;
		string plain = new string(name.Normalize(System.Text.NormalizationForm.FormD)
			.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
			.ToArray());
		return line.Contains(name, StringComparison.OrdinalIgnoreCase) || line.Contains(plain, StringComparison.OrdinalIgnoreCase);
	}

	private static void ValidateSaveDefaults(List<string> warnings)
	{
		var save = new SaveData();
		if (!save.EnableGameplayOnboardingTips)
			warnings.Add("New saves should enable onboarding tips by default.");

		if (save.HasToggledOnboardingTipsAtLeastOnce)
			warnings.Add("New saves should start with HasToggledOnboardingTipsAtLeastOnce = false.");

		if (save.PlaytestModeEnabled)
			warnings.Add("New saves should start with PlaytestModeEnabled = false.");

		if (!string.Equals(save.BalancePresetId, GlobalStatsManager.BalancePresetDefault, StringComparison.OrdinalIgnoreCase))
			warnings.Add($"New saves should default to '{GlobalStatsManager.BalancePresetDefault}' preset, got '{save.BalancePresetId}'.");
	}

	private static void ValidatePresetRewardOrdering(List<string> warnings)
	{
		float casual = GlobalStatsManager.GetArcaneRewardScaleForPreset(GlobalStatsManager.BalancePresetCasual);
		float normal = GlobalStatsManager.GetArcaneRewardScaleForPreset(GlobalStatsManager.BalancePresetDefault);
		float hardcore = GlobalStatsManager.GetArcaneRewardScaleForPreset(GlobalStatsManager.BalancePresetHardcore);

		if (!(casual < normal && normal < hardcore))
			warnings.Add($"Preset reward ordering should be Casual < Default < Hardcore, got {casual:0.00}, {normal:0.00}, {hardcore:0.00}.");
	}

	private static void ValidatePlaytestChecklistDefaults(List<string> warnings)
	{
		var save = new SaveData();
		if (save.PlaytestChecklistState == null)
			warnings.Add("Playtest checklist state should default to an empty map.");

		if (save.PlaytestChecklistState != null && save.PlaytestChecklistState.Count != 0)
			warnings.Add($"Playtest checklist defaults should be empty, got {save.PlaytestChecklistState.Count} entries.");
	}

	/// <summary>
	/// Every evolution has its own icon (EvolutionIcons; Eric, 2026-10-28: "mutations and ascensions
	/// should all have their own unique altered icons"): every level 4 and level 8 option of every spell
	/// in ContentValidator.SpellResourcePaths, and every Apotheosis form, has an imported picture at
	/// EvolutionIcons.PathFor(id). A missing one still plays - the card falls back to its spell's icon
	/// in the branch's tint, which is exactly what the request was about - so it is reported here, where
	/// a new card nobody drew is caught the first time the game starts. The fix is always the same:
	/// `_AscensionAudit -- --json=tools/art/evolution_catalog.json`, `python tools/art/evolution_icons.py`,
	/// then `--import`.
	/// </summary>
	private static void ValidateEvolutionIcons(List<string> warnings)
	{
		var missing = new List<string>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		void Check(string id)
		{
			if (!string.IsNullOrWhiteSpace(id) && seen.Add(id) && !EvolutionIcons.Exists(id))
				missing.Add(id);
		}

		foreach (SpellData template in ContentValidator.LoadAllSpellForms())
		{
			if (template == null || string.IsNullOrWhiteSpace(template.Id))
				continue;
			// A copy, so filling the cards in here never changes the shared resource a run will load. (A
			// form is already a copy; copying it again is harmless.)
			var spell = (SpellData)template.Duplicate(true);
			SpellEvolutionCatalog.EnsureEvolutionCoverage(spell);
			foreach (SpellEvolutionOption option in spell.Level4Options ?? new Godot.Collections.Array<SpellEvolutionOption>())
				Check(option?.Id);
			foreach (SpellEvolutionOption option in spell.Level8Options ?? new Godot.Collections.Array<SpellEvolutionOption>())
				Check(option?.Id);
		}
		foreach (ApotheosisDefinition definition in ApotheosisCatalog.All)
			Check(definition.Form?.Id);

		if (seen.Count == 0)
			warnings.Add("ValidateEvolutionIcons found no evolutions to check - ContentValidator.LoadAllSpells returned none.");
		if (missing.Count > 0)
			warnings.Add($"{missing.Count} of {seen.Count} evolutions have no icon of their own under {EvolutionIcons.Directory} "
				+ $"and show their spell's tinted icon instead: {string.Join(", ", missing)}. Run _AscensionAudit --json, "
				+ "tools/art/evolution_icons.py, then --import.");
	}
}
