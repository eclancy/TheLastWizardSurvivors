using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheOneHeMissed.scripts;

// THE CHEST SIMULATION (issue #44; .ai/passives-and-items.md section 8j). How often a run completes a
// Full Set Enchantment, which one, at which chest, and how often each relic is even shown - counted
// over thousands of runs' worth of chests rather than guessed from the rarity weights.
//
// Run (a tool scene: it touches no save and draws nothing):
//   "$GODOT_BIN" --headless --path . scenes/_ChestSim.tscn -- --runs=10000 --out=C:/path/chest_sim.md
//
// Every chest is dealt by the game's own ChestItemCatalog.GetChestItemOptions with one ChestOfferMemory
// per run, so the rarity weights, the shown-card fade and the last chest's cards sitting out are the
// shipped ones and cannot drift from them (8h's simulation was a Python re-implementation, which could).
// What it models around the deal is 8b's carrier economy, read from the same constants the game reads:
//   - the run's first elite always pays; --elites=18 more pay at DropTable.EliteChestChance, raised by the
//     Curio Ledger and made certain by the whole Cabinet, as Player.GetEliteChestChance does; --wardens=4
//     always pay. 8b planned 15 more elites (1 + 15 x 65% + 4 = 14.75 chests, "about fifteen"), but that
//     was before the chapters' elite cadence (ChapterDifficulty.EliteInterval, x0.85 in the Forest): the
//     ordinary bot killed 19 elites and 4 Wardens on a 15-minute Forest run on 2026-11-02 (8j), so the
//     default is 18, 16.7 chests before any relic raises it;
//   - the Hidden Drawer's chest in a chest (DropTable.NestedChestChance), which can chain;
//   - the Relic Key's fourth card (DropTable.BaseChestOffers + 1).
// --until=M ends the run at pace minute M (a run lost early, or the boss's arrival at 15, the default).
// It does NOT model: side events' and secrets' chests (chapter-dependent, three at most), John Dee's fifth
// card (--extra-offers=1 is that), bans, and the Penknife's "nothing to cut" rule (it is dealt like any
// relic, and no picker ever claims it).
//
// Three pickers, because how often a set completes depends on who is choosing:
//   random - any card but the Penknife: a player who does not read the sets.
//   rarest - the balance bot's ordinary policy (_BalanceHarness.PickChestOrdinary).
//   chaser - finishes a set when it can, else advances the set it is furthest into, else starts one,
//            rarest first on a tie: the ceiling for a player who plays for sets. The offer leans toward a
//            started set by ChestItemCatalog.StartedSetLean (2 in the game since 2026-11-02; --lean=X overrides).
// "chaser+rN" is the one named option with charges: the chaser spends up to N of the run's rerolls on a
// chest that offers nothing for a set it has begun (.ai/new-items.md, answer 3: rerolls work on chests).
// --chest-rerolls=0 drops it.
//
// Every configuration runs twice: today's pool, and the pool without wave 1's six relics (the Poppet and
// the Gimmel Ring, passed to the deal as unavailable), for the before and after.
//
// Flags: --runs=N (10000), --seed=N (1), --elites=N (18), --wardens=N (4), --until=M (15),
// --extra-offers=N (0), --chest-rerolls=N (3), --out=PATH (user://chest_sim.md), --set=ID:piece,piece (a
// proposal: that set with those pieces, in this count only). The tables are written as Markdown to --out;
// the console gets one CHESTSIM line per configuration.
public partial class _ChestSim : Node
{
	private enum Picker { Random, Rarest, Chaser }

	private sealed class Config
	{
		public string Name;
		public Picker Picker;
		public int Rerolls;
		public bool WithoutWave1;
	}

	// One configuration's counts over every run.
	private sealed class Tally
	{
		public int Runs;
		public long Chests;
		public long RerollsSpent;
		public int[] SetCompleted;
		public int[] SetPartial;
		public int AnyCompleted;
		public readonly int[] SetsHistogram = new int[4]; // 0, 1, 2, 3 or more
		public readonly List<int> FirstSetChest = new();
		public readonly List<float> FirstSetMinute = new();
		public readonly Dictionary<string, int> RunsShown = new(StringComparer.OrdinalIgnoreCase);
		public readonly Dictionary<string, long> TimesShown = new(StringComparer.OrdinalIgnoreCase);
		public readonly Dictionary<string, int> RunsClaimed = new(StringComparer.OrdinalIgnoreCase);
	}

	// The six relics wave 1 added (.ai/new-items.md, "Wave 1, as built").
	private static readonly string[] Wave1Ids =
	{
		ChestItemCatalog.PrickingPin, ChestItemCatalog.BindingThread, ChestItemCatalog.SingedLock,
		ChestItemCatalog.BetrothalHoop, ChestItemCatalog.FedeHoop, ChestItemCatalog.GarnetBezel,
	};

	private int runs = 10000;
	private int seed = 1;
	private int elites = 18;
	private int wardens = 4;
	private float untilMinute = 15f;
	private int extraOffers;
	private int chestRerolls = 3;
	private string outPath = "user://chest_sim.md";
	// --set=ID:piece,piece - a set's pieces as a proposal would have them, for this simulation only.
	private readonly Dictionary<string, string[]> setOverrides = new(StringComparer.OrdinalIgnoreCase);

	private ChestSetDefinition[] sets;
	private Dictionary<string, int[]> setsOfItem;
	// The run's carriers in the order they die: (pace seconds, is an elite).
	private List<(float At, bool Elite)> carriers;

	public override void _Ready()
	{
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			if (arg.StartsWith("--runs=")) runs = int.Parse(arg.Substring(7));
			else if (arg.StartsWith("--seed=")) seed = int.Parse(arg.Substring(7));
			else if (arg.StartsWith("--elites=")) elites = int.Parse(arg.Substring(9));
			else if (arg.StartsWith("--wardens=")) wardens = int.Parse(arg.Substring(10));
			else if (arg.StartsWith("--until=")) untilMinute = float.Parse(arg.Substring(8), System.Globalization.CultureInfo.InvariantCulture);
			else if (arg.StartsWith("--extra-offers=")) extraOffers = int.Parse(arg.Substring(15));
			else if (arg.StartsWith("--chest-rerolls=")) chestRerolls = int.Parse(arg.Substring(16));
			else if (arg.StartsWith("--out=")) outPath = arg.Substring(6);
			// --lean=X: a chest offers a piece of a set already started X times as often (ChestItemCatalog.StartedSetLean).
			else if (arg.StartsWith("--lean=")) ChestItemCatalog.StartedSetLean = float.Parse(arg.Substring(7), System.Globalization.CultureInfo.InvariantCulture);
			else if (arg.StartsWith("--set=") && arg.Contains(':'))
				setOverrides[arg.Substring(6, arg.IndexOf(':') - 6)] = arg.Substring(arg.IndexOf(':') + 1).Split(',', StringSplitOptions.RemoveEmptyEntries);
		}

		// A proposal that moves a set's pieces is measured here before it is built: the deal never reads the
		// sets (only the pool and the rarities), so a set's membership can change in this count alone. A
		// rarity cannot - that would change the deal - and is a change to ChestItemCatalog to try.
		sets = ChestItemCatalog.Sets.Select(set => setOverrides.TryGetValue(set.Id, out string[] pieces)
			? new ChestSetDefinition
			{
				Id = set.Id, Name = set.Name + " (proposed)", RequiredItemIds = pieces, Description = set.Description,
				IconPath = set.IconPath, Effects = set.Effects, PartialItemCount = pieces.Length >= 3 ? set.PartialItemCount : 0,
			}
			: set).ToArray();
		foreach (string id in setOverrides.Keys.Where(id => sets.All(set => set.Id != id)))
			GD.PushError($"_ChestSim: --set names no set '{id}'");
		setsOfItem = ChestItemCatalog.AllItemIds.ToDictionary(id => id,
			id => Enumerable.Range(0, sets.Length).Where(i => sets[i].RequiredItemIds.Contains(id, StringComparer.OrdinalIgnoreCase)).ToArray(),
			StringComparer.OrdinalIgnoreCase);
		carriers = BuildCarriers();

		var configs = new List<Config>();
		foreach (bool without in new[] { false, true })
		{
			configs.Add(new Config { Name = "random", Picker = Picker.Random, WithoutWave1 = without });
			configs.Add(new Config { Name = "rarest", Picker = Picker.Rarest, WithoutWave1 = without });
			configs.Add(new Config { Name = "chaser", Picker = Picker.Chaser, WithoutWave1 = without });
			if (chestRerolls > 0)
				configs.Add(new Config { Name = $"chaser+r{chestRerolls}", Picker = Picker.Chaser, Rerolls = chestRerolls, WithoutWave1 = without });
		}

		ulong started = Time.GetTicksMsec();
		var tallies = new List<(Config Config, Tally Tally)>();
		foreach (Config config in configs)
		{
			// The same seed for every configuration, so they differ by the rule under test more than by luck.
			var rng = new RandomNumberGenerator { Seed = (ulong)seed };
			var tally = new Tally { SetCompleted = new int[sets.Length], SetPartial = new int[sets.Length] };
			for (int r = 0; r < runs; r++)
				SimulateRun(config, rng, tally);
			tallies.Add((config, tally));
			GD.Print(SummaryLine(config, tally));
		}

		string report = Report(tallies);
		using (var f = FileAccess.Open(outPath, FileAccess.ModeFlags.Write))
		{
			if (f == null)
				GD.PushError($"_ChestSim: could not write {outPath} ({FileAccess.GetOpenError()})");
			else
				f.StoreString(report);
		}
		GD.Print($"CHESTSIM wrote {ProjectSettings.GlobalizePath(outPath)} in {(Time.GetTicksMsec() - started) / 1000f:0.0} s");
		GetTree().Quit();
	}

	// 8b's schedule on the pace clock: the first elite at 2:15 (Node2DGame.EliteStartTimeSeconds), the rest
	// spread evenly to 14:30, and the Wardens at 5:15, 8:00, 10:45 and 13:30. Only the ORDER matters to the
	// deal (when a Curio Ledger or a Hidden Drawer starts paying); the minute is for the report.
	private List<(float At, bool Elite)> BuildCarriers()
	{
		var list = new List<(float At, bool Elite)> { (135f, true) };
		for (int i = 1; i <= elites; i++)
			list.Add((135f + i * (870f - 135f) / elites, true));
		float[] wardenTimes = wardens == 4
			? new[] { 315f, 480f, 645f, 810f }
			: Enumerable.Range(0, wardens).Select(i => wardens == 1 ? 480f : 315f + i * (810f - 315f) / (wardens - 1)).ToArray();
		foreach (float at in wardenTimes)
			list.Add((at, false));
		return list.Where(c => c.At <= untilMinute * 60f).OrderBy(c => c.At).ToList();
	}

	private void SimulateRun(Config config, RandomNumberGenerator rng, Tally tally)
	{
		var owned = new List<string>();
		var ownedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var completed = new bool[sets.Length];
		var memory = new ChestOfferMemory();
		var shownThisRun = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var unavailable = config.WithoutWave1 ? Wave1Ids : Array.Empty<string>();
		int chests = 0, setsDone = 0, firstSetChest = -1, rerollsLeft = config.Rerolls;
		float firstSetMinute = -1f;
		bool firstElitePaid = false;

		foreach ((float at, bool elite) in carriers)
		{
			if (elite)
			{
				bool pays = !firstElitePaid || rng.Randf() < EliteChestChance(ownedSet, completed);
				firstElitePaid = true;
				if (!pays)
					continue;
			}

			// The chest, then whatever the Hidden Drawer finds in it (it counts once claimed: FinishChestClaim).
			bool another = true;
			while (another)
			{
				chests++;
				int count = DropTable.BaseChestOffers + (ownedSet.Contains(ChestItemCatalog.RelicKey) ? 1 : 0) + extraOffers;
				List<string> offer = ChestItemCatalog.GetChestItemOptions(owned, rng, count, unavailable, memory);
				Seen(offer, shownThisRun, tally);
				while (rerollsLeft > 0 && config.Picker == Picker.Chaser && WantsReroll(offer, ownedSet, completed))
				{
					rerollsLeft--;
					tally.RerollsSpent++;
					offer = ChestItemCatalog.GetChestItemOptions(owned, rng, count, unavailable, memory);
					Seen(offer, shownThisRun, tally);
				}

				string pick = Pick(config.Picker, offer, ownedSet, completed, rng);
				if (pick != null && ownedSet.Add(pick))
				{
					owned.Add(pick);
					tally.RunsClaimed[pick] = tally.RunsClaimed.GetValueOrDefault(pick) + 1;
					foreach (int s in setsOfItem[pick])
					{
						if (completed[s] || sets[s].RequiredItemIds.Any(id => !ownedSet.Contains(id)))
							continue;
						completed[s] = true;
						setsDone++;
						if (firstSetChest < 0)
						{
							firstSetChest = chests;
							firstSetMinute = at / 60f;
						}
					}
				}
				another = ownedSet.Contains(ChestItemCatalog.HiddenDrawer) && rng.Randf() < DropTable.NestedChestChance;
			}
		}

		tally.Runs++;
		tally.Chests += chests;
		for (int s = 0; s < sets.Length; s++)
		{
			if (completed[s])
				tally.SetCompleted[s]++;
			int have = sets[s].RequiredItemIds.Count(ownedSet.Contains);
			if (completed[s] || (sets[s].PartialItemCount > 0 && have >= sets[s].PartialItemCount))
				tally.SetPartial[s]++;
		}
		if (setsDone > 0)
		{
			tally.AnyCompleted++;
			tally.FirstSetChest.Add(firstSetChest);
			tally.FirstSetMinute.Add(firstSetMinute);
		}
		tally.SetsHistogram[Math.Min(3, setsDone)]++;
		foreach (string id in shownThisRun)
			tally.RunsShown[id] = tally.RunsShown.GetValueOrDefault(id) + 1;
	}

	private int CabinetIndex => Array.FindIndex(sets, s => s.Id == ChestItemCatalog.CabinetOfCuriositiesSetId);

	// Player.GetEliteChestChance, from what this run holds.
	private float EliteChestChance(HashSet<string> ownedSet, bool[] completed)
	{
		if (completed[CabinetIndex])
			return 1f;
		float chance = DropTable.EliteChestChance + (ownedSet.Contains(ChestItemCatalog.CurioLedger) ? Player.CurioLedgerEliteChestChance : 0f);
		return Math.Min(1f, chance);
	}

	private static void Seen(List<string> offer, HashSet<string> shownThisRun, Tally tally)
	{
		foreach (string id in offer)
		{
			shownThisRun.Add(id);
			tally.TimesShown[id] = tally.TimesShown.GetValueOrDefault(id) + 1;
		}
	}

	private string Pick(Picker picker, List<string> offer, HashSet<string> ownedSet, bool[] completed, RandomNumberGenerator rng)
	{
		// Never the knife: claiming it means naming a spell to cut, which is no part of collecting.
		var cards = offer.Where(id => id != ChestItemCatalog.ScribesPenknife).ToList();
		if (cards.Count == 0)
			return null;
		switch (picker)
		{
			case Picker.Random:
				return cards[rng.RandiRange(0, cards.Count - 1)];
			case Picker.Rarest:
				return cards.OrderByDescending(id => (int)ChestItemCatalog.GetRarity(id)).First();
			default:
				return cards
					.OrderByDescending(id => ChaseTier(id, ownedSet, completed))
					.ThenByDescending(id => ChaseProgress(id, ownedSet, completed))
					.ThenByDescending(id => (int)ChestItemCatalog.GetRarity(id))
					.First();
		}
	}

	// 3 completes a set, 2 advances one already begun, 1 starts one, 0 is in no set still open.
	private int ChaseTier(string id, HashSet<string> ownedSet, bool[] completed)
	{
		if (ownedSet.Contains(id))
			return 0;
		int best = 0;
		foreach (int s in setsOfItem[id])
		{
			if (completed[s])
				continue;
			int have = sets[s].RequiredItemIds.Count(ownedSet.Contains);
			int tier = have + 1 == sets[s].RequiredItemIds.Length ? 3 : have > 0 ? 2 : 1;
			best = Math.Max(best, tier);
		}
		return best;
	}

	// Among cards of one tier, the set furthest along (pieces held after the pick, as a share of the set).
	private float ChaseProgress(string id, HashSet<string> ownedSet, bool[] completed)
	{
		float best = 0f;
		foreach (int s in setsOfItem[id])
		{
			if (completed[s])
				continue;
			int have = sets[s].RequiredItemIds.Count(ownedSet.Contains);
			best = Math.Max(best, (have + 1f) / sets[s].RequiredItemIds.Length);
		}
		return best;
	}

	// The chaser rerolls a chest that does nothing for any set it has begun - once it has begun one.
	private bool WantsReroll(List<string> offer, HashSet<string> ownedSet, bool[] completed)
	{
		bool begun = false;
		for (int s = 0; s < sets.Length && !begun; s++)
			begun = !completed[s] && sets[s].RequiredItemIds.Any(ownedSet.Contains);
		return begun && !offer.Any(id => ChaseTier(id, ownedSet, completed) >= 2);
	}

	// --- the report ----------------------------------------------------------------------------

	private static float Percentile(List<int> sorted, float p) => sorted.Count == 0 ? float.NaN : sorted[(int)Math.Clamp(Math.Round(p * (sorted.Count - 1)), 0, sorted.Count - 1)];
	private static float Percentile(List<float> sorted, float p) => sorted.Count == 0 ? float.NaN : sorted[(int)Math.Clamp(Math.Round(p * (sorted.Count - 1)), 0, sorted.Count - 1)];

	private static string Pct(int n, int of) => of == 0 ? "-" : $"{100f * n / of:0.0}%";

	private static string Label(Config c) => (c.WithoutWave1 ? "without wave 1 / " : "today / ") + c.Name;

	private string SummaryLine(Config config, Tally t)
	{
		var chest = t.FirstSetChest.OrderBy(x => x).ToList();
		return $"CHESTSIM {Label(config)}: chests {(double)t.Chests / t.Runs:0.00}, any set {Pct(t.AnyCompleted, t.Runs)},"
			+ $" sets a run {(double)t.SetCompleted.Sum() / t.Runs:0.00}, first set at chest {Percentile(chest, 0.5f):0} ({Percentile(chest, 0.25f):0}-{Percentile(chest, 0.75f):0}),"
			+ $" rerolls {(double)t.RerollsSpent / t.Runs:0.00} a run";
	}

	private static string RarityLetter(string id) => ChestItemCatalog.GetRarity(id) switch
	{
		ChestItemRarity.Common => "C",
		ChestItemRarity.Uncommon => "U",
		ChestItemRarity.Rare => "R",
		ChestItemRarity.Relic => "Relic",
		_ => "?",
	};

	private string Report(List<(Config Config, Tally Tally)> tallies)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"# Chest simulation: {runs} runs a configuration, seed {seed}");
		sb.AppendLine();
		sb.AppendLine($"Carriers to minute {untilMinute:0.#}: {carriers.Count(c => c.Elite)} elites (the first always pays, the rest at {DropTable.EliteChestChance:0%}), "
			+ $"{carriers.Count(c => !c.Elite)} Wardens; "
			+ $"{DropTable.BaseChestOffers + extraOffers} cards a chest, +1 with the Relic Key. Pool: {ChestItemCatalog.AllItemIds.Count} relics, {sets.Length} sets.");
		sb.AppendLine();

		sb.AppendLine("## Per configuration");
		sb.AppendLine();
		sb.AppendLine("| pool / picker | chests a run | any set | 0 / 1 / 2 / 3+ sets | sets a run | first set: chest (median, quartiles) | first set: minute (median) | completes one by chest 5 / 10 / 15 | rerolls spent |");
		sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
		foreach ((Config c, Tally t) in tallies)
		{
			var chest = t.FirstSetChest.OrderBy(x => x).ToList();
			var minute = t.FirstSetMinute.OrderBy(x => x).ToList();
			string By(int n) => Pct(chest.Count(x => x <= n), t.Runs);
			sb.AppendLine($"| {Label(c)} | {(double)t.Chests / t.Runs:0.0} | **{Pct(t.AnyCompleted, t.Runs)}** "
				+ $"| {string.Join(" / ", t.SetsHistogram.Select(h => $"{100f * h / t.Runs:0}%"))} | {(double)t.SetCompleted.Sum() / t.Runs:0.00} "
				+ $"| {Percentile(chest, 0.5f):0} ({Percentile(chest, 0.25f):0}-{Percentile(chest, 0.75f):0}) | {Percentile(minute, 0.5f):0.0} "
				+ $"| {By(5)} / {By(10)} / {By(15)} | {(double)t.RerollsSpent / t.Runs:0.00} |");
		}
		sb.AppendLine();

		// Each set: completed (and the partial reached, for a set that has one), per configuration.
		sb.AppendLine("## Each set: completed (partial or better)");
		sb.AppendLine();
		sb.AppendLine("| set | pieces | " + string.Join(" | ", tallies.Select(x => Label(x.Config))) + " |");
		sb.AppendLine("|---|---|" + string.Concat(tallies.Select(_ => "---|")));
		for (int s = 0; s < sets.Length; s++)
		{
			string pieces = string.Join(" ", sets[s].RequiredItemIds.Select(RarityLetter));
			var cells = tallies.Select(x => sets[s].PartialItemCount > 0
				? $"{Pct(x.Tally.SetCompleted[s], x.Tally.Runs)} ({Pct(x.Tally.SetPartial[s], x.Tally.Runs)})"
				: Pct(x.Tally.SetCompleted[s], x.Tally.Runs));
			sb.AppendLine($"| {sets[s].Name} | {pieces} | {string.Join(" | ", cells)} |");
		}
		sb.AppendLine();

		// Each relic: in how many runs it was shown at all, how many times a run, and how often it was taken.
		var today = tallies.Where(x => !x.Config.WithoutWave1 && x.Config.Rerolls == 0).ToList();
		var before = tallies.FirstOrDefault(x => x.Config.WithoutWave1 && x.Config.Picker == Picker.Random);
		sb.AppendLine("## Each relic: runs it was shown in (times a run); taken");
		sb.AppendLine();
		sb.AppendLine("| relic | rarity | set | " + string.Join(" | ", today.Select(x => $"shown, {x.Config.Name}")) + " | taken, chaser | shown without wave 1, random |");
		sb.AppendLine("|---|---|---|" + string.Concat(today.Select(_ => "---|")) + "---|---|");
		var chaser = today.First(x => x.Config.Picker == Picker.Chaser);
		foreach (string id in ChestItemCatalog.AllItemIds.OrderByDescending(id => today[0].Tally.RunsShown.GetValueOrDefault(id)))
		{
			string setNames = string.Join(", ", setsOfItem[id].Select(s => sets[s].Name.Replace("The ", "")));
			var cells = today.Select(x => $"{Pct(x.Tally.RunsShown.GetValueOrDefault(id), x.Tally.Runs)} ({(double)x.Tally.TimesShown.GetValueOrDefault(id) / x.Tally.Runs:0.0})");
			string beforeCell = Wave1Ids.Contains(id) ? "-" : $"{Pct(before.Tally.RunsShown.GetValueOrDefault(id), before.Tally.Runs)} ({(double)before.Tally.TimesShown.GetValueOrDefault(id) / before.Tally.Runs:0.0})";
			sb.AppendLine($"| {ChestItemCatalog.GetDisplayName(id)} | {RarityLetter(id)} | {(setNames.Length == 0 ? "-" : setNames)} | {string.Join(" | ", cells)} "
				+ $"| {Pct(chaser.Tally.RunsClaimed.GetValueOrDefault(id), chaser.Tally.Runs)} | {beforeCell} |");
		}
		return sb.ToString();
	}
}
