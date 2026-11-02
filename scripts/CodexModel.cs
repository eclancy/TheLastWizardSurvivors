using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WizardSurvivors.scripts;

// THE EVOLUTION CODEX'S MODEL (.ai/evolution-codex.md, approved by Eric 2026-11-02). Every spell, item,
// evolution and recipe as plain data, built from the catalogs and the save - never hardcoded - so a new
// spell, relic or recipe appears in the Codex with no edit here. Nothing in this file draws anything:
// EvolutionCodex lays it out, RegressionChecks.ValidateCodex holds its bounds, and _UiShot --codex-dump
// prints it.
//
// One model, two modes, and the mode decides only what the states MEAN (section 1):
//   - IN-RUN (from a level-up or the pause screen): levels are this run's, Ready is what the strip can
//     offer now, requirements count this run's tags and level, the held spells sort first. The run's own
//     previews (Player.BuildAscensionPreview, BuildApotheosisPreview, BuildComboPreviews) are the only
//     source of a held spell's gates - this file never restates a rule.
//   - COLLECTION (the main menu): no run, so no levels. Owned or not, how to find a missing one
//     (UnlockCatalog's line), which Ascensions have been seen (SeenAscensionIds), which recipes made.

public enum CodexMode
{
	InRun,
	Collection,
}

/// <summary>The four filter tabs (section 4).</summary>
public enum CodexFilter
{
	All,
	Spells,
	Items,
	Recipes,
}

public enum CodexKind
{
	Spell,
	Relic,
	Boon,
	/// <summary>A combination spell (ComboCatalog): two Ascensions made one.</summary>
	Combination,
	/// <summary>A Full Set Enchantment (ChestItemCatalog.Sets).</summary>
	FullSet,
}

/// <summary>A grid cell's state (section 3). Each is drawn the same everywhere.</summary>
public enum CodexState
{
	/// <summary>Never met: the sealed glyph and "???". Not searchable.</summary>
	Undiscovered,
	/// <summary>Known but not owned, or not taken this run: the icon dimmed, a plain frame.</summary>
	Known,
	/// <summary>Owned (collection), or in the save's pool: the icon at full light.</summary>
	Owned,
	/// <summary>Held this run and still levelling: pips under the icon.</summary>
	Leveling,
	/// <summary>Something on it can be taken now (in-run): the gold frame breathing.</summary>
	Ready,
	/// <summary>Taken, held at its last form, or made: the solid gold rim and a star.</summary>
	Taken,
}

/// <summary>A tree node's state (section 3).</summary>
public enum CodexNodeState
{
	/// <summary>Never met: "???", the sealed glyph, one line saying how it is found.</summary>
	Sealed,
	/// <summary>Known, not taken: the resting frame.</summary>
	Known,
	/// <summary>Takeable now (in-run): lit, with a gold arrow and its spark.</summary>
	Ready,
	/// <summary>Taken this run (or made): solid in its milestone's colour.</summary>
	Taken,
	/// <summary>Out of reach this run: another path was taken. A lock.</summary>
	Closed,
}

/// <summary>One requirement chip under a node ("Spell 5/7", "Fire 4/4", "You 17").</summary>
public sealed class CodexChip
{
	public string Text = string.Empty;
	/// <summary>The element whose colour the chip takes; null for a plain chip.</summary>
	public Element? Element;
	/// <summary>Met (ticked), unmet (on the rubric band), or null when there is no run to measure (collection).</summary>
	public bool? Met;
	/// <summary>Closed for the run: a lock, not a socket.</summary>
	public bool Closed;
	/// <summary>Have and need, for an element chip's popover (ElementTagPopover).</summary>
	public int Have;
	public int Need;

	public override string ToString() => $"{Text}{(Met == true ? " ok" : Met == false ? (Closed ? " closed" : " unmet") : string.Empty)}";
}

/// <summary>One node of a spell's tree, or one ingredient or result of a recipe.</summary>
public sealed class CodexNode
{
	/// <summary>The evolution's id, the item's, or the recipe's. Unique inside its entry.</summary>
	public string Id = string.Empty;
	/// <summary>The spell it belongs to (an evolution), or the entry an ingredient points at.</summary>
	public string SpellId = string.Empty;
	/// <summary>0 the base, 4 a mutation, 8 an Ascension, 9 the Apotheosis, 10 a combination, -1 an item.</summary>
	public int Milestone;
	public string Name = string.Empty;
	/// <summary>What it does, one line; empty while sealed.</summary>
	public string Line = string.Empty;
	public Texture2D Icon;
	public CodexNodeState State = CodexNodeState.Known;
	/// <summary>The name always shows even sealed (an Apotheosis: the character select names it).</summary>
	public bool NameAlwaysShown;
	public List<CodexChip> Chips = new();
	/// <summary>A quiet word under the node: "Chosen", "Out of reach this run", "Joins Starfall: Constellation".</summary>
	public string Note = string.Empty;
	/// <summary>For an ingredient: the grid entry it opens (a spell, a relic), or empty.</summary>
	public string OpensEntryId = string.Empty;
	/// <summary>The element whose colour an item ingredient's chip takes, for sets.</summary>
	public List<(Element Element, int Weight)> Elements = new();
	/// <summary>Whose final form it is ("Pyromancer"), on an Apotheosis node.</summary>
	public string Owner = string.Empty;
	/// <summary>Not a name to search for (the Sovereign Rite's "any active spell you hold").</summary>
	public bool Unsearchable;

	public bool Revealed => State != CodexNodeState.Sealed;
	public string ShownName => Revealed || NameAlwaysShown ? Name : CodexModel.SealedName;
}

/// <summary>One form of a spell (SpellData.Variants), or the spell itself: its tree.</summary>
public sealed class CodexForm
{
	public string Id = string.Empty;
	public string Name = string.Empty;
	public Texture2D Icon;
	/// <summary>The gates every mutation shares (spell level 4).</summary>
	public List<CodexChip> MutationGates = new();
	public List<CodexNode> Mutations = new();
	/// <summary>The gates every Ascension shares (spell level, element, your level).</summary>
	public List<CodexChip> AscensionGates = new();
	public List<CodexNode> Ascensions = new();
	/// <summary>
	/// The Apotheosis forms that end this spell: the wizard's in a run, every freed wizard's otherwise. Every
	/// mutation and every Ascension leads to them (ApotheosisCatalog.SpellMeetsGate names no path), so the book
	/// draws the whole open tree in the ember line while one is still open (EvolutionCodex.ArrangeTree).
	/// </summary>
	public List<CodexNode> Finals = new();

	public IEnumerable<CodexNode> AllNodes => Mutations.Concat(Ascensions).Concat(Finals);
}

/// <summary>A recipe drawn as ingredients into a result: a combination, or a Full Set Enchantment.</summary>
public sealed class CodexRecipe
{
	public string Id = string.Empty;
	public CodexKind Kind;
	public List<CodexNode> Ingredients = new();
	public CodexNode Result = new();
	/// <summary>A set's partial tier ("2 of 3: Damage taken -5%"); empty for a combination.</summary>
	public string Partial = string.Empty;
	/// <summary>A set's pieces for the partial tier, and how many this run holds.</summary>
	public int PartialCount;
	public int HeldCount;
	/// <summary>Made (a combination's Feat done, or this run) / every piece held this run.</summary>
	public bool Made;
	/// <summary>The strip can make it now (in-run).</summary>
	public bool Offered;
	/// <summary>Out of reach this run.</summary>
	public bool Sealed;
}

/// <summary>One cell of the grid, and everything its detail shows.</summary>
public sealed class CodexEntry
{
	public string Id = string.Empty;
	public CodexKind Kind;
	public string Name = string.Empty;
	/// <summary>One line: a spell's tagline, an item's effect.</summary>
	public string Line = string.Empty;
	public Texture2D Icon;
	public List<(Element Element, int Weight)> Elements = new();
	public CodexState State = CodexState.Known;
	public bool Discovered;
	/// <summary>Owned by the save (spells, boons), or made once (combinations).</summary>
	public bool Owned;
	/// <summary>In the loadout this run.</summary>
	public bool Held;
	public bool IsPassive;
	/// <summary>Held this run: its level and the level its pips run to.</summary>
	public int Level;
	public int LevelCap;
	/// <summary>How to find it while it is not owned (UnlockCatalog's line), or how to meet it.</summary>
	public string Hint = string.Empty;
	/// <summary>Set aside with a Redaction (GlobalStatsManager.IsSpellRemovedFromPool).</summary>
	public bool SetAside;
	/// <summary>"Rare relic", "Boon".</summary>
	public string KindLine = string.Empty;
	/// <summary>The element a spell is filed under in the index: its own first, heaviest tag (the template's, never a gain).</summary>
	public Element? IndexElement;
	/// <summary>The wizard's own starting spell (in a run): the index stars it.</summary>
	public bool Own;
	/// <summary>A relic's rarity, and every Full Set Enchantment it is a piece of, discovered or not (the index files it).</summary>
	public ChestItemRarity? Rarity;
	public List<string> SetIds = new();

	public List<CodexForm> Forms = new();
	/// <summary>The form the detail opens on: the held one in a run, else the first.</summary>
	public int CurrentForm;
	/// <summary>Recipes this is an ingredient of (entry ids), for a spell's "Used in" and an item's sets.</summary>
	public List<string> UsedIn = new();
	/// <summary>A combination's or a set's card (also an item's sets, through UsedIn).</summary>
	public CodexRecipe Recipe;

	/// <summary>How many requirements stand between this and its next step (in-run sort: near misses first).</summary>
	public int Unmet = int.MaxValue;
	/// <summary>The order the model was built in, the last tie-break.</summary>
	public int Order;

	public CodexFilter Group => Kind switch
	{
		CodexKind.Spell => CodexFilter.Spells,
		CodexKind.Relic or CodexKind.Boon => CodexFilter.Items,
		_ => CodexFilter.Recipes,
	};

	public string ShownName => Discovered ? Name : CodexModel.SealedName;
	public Element? PrimaryElement => Elements.Count == 0 ? null : Elements.OrderByDescending(e => e.Weight).First().Element;
}

/// <summary>One name the search field can find, and where it leads.</summary>
public sealed class CodexSearchHit
{
	public string Name = string.Empty;
	/// <summary>What it is, for the suggestion row: "Ascension of Cinderbreath", "Relic", "Recipe".</summary>
	public string What = string.Empty;
	public string EntryId = string.Empty;
	/// <summary>The node it lights inside the entry; empty for the entry itself.</summary>
	public string NodeId = string.Empty;
	public int Score;
}

/// <summary>What a run hands the Codex. Null fields read as "no run": collection mode.</summary>
public sealed class CodexContext
{
	public CodexMode Mode = CodexMode.Collection;
	public SaveData Save;
	/// <summary>The wizard playing (in-run), or empty.</summary>
	public string CharacterId = string.Empty;
	public string WizardName = string.Empty;
	public int PlayerLevel;
	public IReadOnlyList<SpellData> HeldSpells = Array.Empty<SpellData>();
	/// <summary>The run's previews of each held spell (Node2DGame.BuildEquippedInfo).</summary>
	public IReadOnlyList<EquippedSpellInfo> Equipped = Array.Empty<EquippedSpellInfo>();
	public IReadOnlyList<ComboPreview> Combos = Array.Empty<ComboPreview>();
	public IReadOnlyList<string> HeldRelics = Array.Empty<string>();
	public IReadOnlyList<string> HeldBoons = Array.Empty<string>();
	/// <summary>The run's element counts by name, for an element chip's popover.</summary>
	public IReadOnlyDictionary<string, int> ElementCounts = new Dictionary<string, int>();
	/// <summary>The wizard's own starting spell (Player.SignatureSpellId), or empty.</summary>
	public string StarterSpellId = string.Empty;

	public static CodexContext ForSave(SaveData save) => new() { Mode = CodexMode.Collection, Save = save };
}

public sealed class CodexModel
{
	public const string SealedName = "???";

	/// <summary>The Apotheosis's two gates on the spell beside its level (ApotheosisCatalog.SpellMeetsGate).</summary>
	public const string AscendedChip = "Ascended";
	public const string NotCombinedChip = "Not combined";

	/// <summary>
	/// The tree's bound (section 6, "no zoom and pan"): a detail panel holds at most this many nodes in each
	/// column on both canvases. RegressionChecks.ValidateCodex fails a spell that outgrows it rather than
	/// letting it clip.
	/// </summary>
	// Four level 4 paths, not the design's three: the Familiar's cat has four. A column of four fits the
	// phone's sheet; on a 1280x720 PC, under the form tabs, it pushes the reading box about 50 units below the
	// fold and the detail scrolls (_UiShot --open-codex=familiar_cat_purr) - to settle with the chosen layout.
	public const int MaxMutations = 4;
	public const int MaxAscensions = 3;
	public const int MaxFinals = 2;
	public const int MaxUsedIn = 3;
	public const int MaxIngredients = 4;

	public CodexMode Mode;
	public List<CodexEntry> Entries = new();
	/// <summary>"Pyromancer, level 17" in a run; empty otherwise.</summary>
	public string RunLine = string.Empty;
	public CodexContext Context;

	public int Total => Entries.Count;
	public int DiscoveredCount => Entries.Count(e => e.Discovered);

	public CodexEntry Get(string id) =>
		string.IsNullOrWhiteSpace(id) ? null : Entries.FirstOrDefault(e => e.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));

	// ---- what the grid shows ------------------------------------------------------------------------

	/// <summary>
	/// The entries under <paramref name="filter"/>, in the order of section 5: in a run held and ready
	/// first, then the near misses, then the rest; in the collection owned first, then by element. With a
	/// query, only entries a search name of matches (an evolution's hit shows its spell).
	/// </summary>
	public List<CodexEntry> Filtered(CodexFilter filter, string query = null)
	{
		IEnumerable<CodexEntry> pool = Entries.Where(e => filter == CodexFilter.All || e.Group == filter);
		if (!string.IsNullOrWhiteSpace(query))
		{
			var hits = new HashSet<string>(Search(query, int.MaxValue).Select(h => h.EntryId), StringComparer.OrdinalIgnoreCase);
			pool = pool.Where(e => hits.Contains(e.Id));
		}
		return Sort(pool).ToList();
	}

	private IEnumerable<CodexEntry> Sort(IEnumerable<CodexEntry> pool)
	{
		// The kind first, so All reads Spells, Items, Recipes in their own sections.
		var ordered = pool.OrderBy(e => (int)e.Group);
		if (Mode == CodexMode.InRun)
		{
			return ordered
				.ThenBy(e => e.State == CodexState.Ready ? 0 : e.Held ? 1 : e.Discovered ? 2 : 3)
				.ThenBy(e => e.Held ? 0 : e.Unmet)
				.ThenBy(e => e.Order);
		}
		return ordered
			.ThenBy(e => e.Owned ? 0 : e.Discovered ? 1 : 2)
			.ThenBy(e => e.PrimaryElement.HasValue ? (int)e.PrimaryElement.Value : int.MaxValue)
			.ThenBy(e => e.Order);
	}

	// ---- search -------------------------------------------------------------------------------------

	private List<CodexSearchHit> searchIndex;

	/// <summary>Every name the search can find (section 5). Undiscovered names are not in it.</summary>
	public IReadOnlyList<CodexSearchHit> SearchIndex => searchIndex ??= BuildSearchIndex();

	private List<CodexSearchHit> BuildSearchIndex()
	{
		var index = new List<CodexSearchHit>();
		void Add(string name, string what, string entryId, string nodeId = "")
		{
			if (string.IsNullOrWhiteSpace(name) || name == SealedName)
				return;
			if (index.Any(h => h.EntryId == entryId && h.NodeId == nodeId && h.Name == name))
				return;
			index.Add(new CodexSearchHit { Name = name, What = what, EntryId = entryId, NodeId = nodeId });
		}

		foreach (CodexEntry entry in Entries)
		{
			if (!entry.Discovered)
				continue;
			Add(entry.Name, KindWord(entry), entry.Id);
			foreach (CodexForm form in entry.Forms)
			{
				foreach (CodexNode node in form.Mutations.Where(n => n.Revealed))
					Add(node.Name, $"Mutation of {entry.Name}", entry.Id, node.Id);
				foreach (CodexNode node in form.Ascensions.Where(n => n.Revealed))
					Add(node.Name, $"Ascension of {entry.Name}", entry.Id, node.Id);
				// An Apotheosis's name is always shown (the character select names it), so it is always findable.
				foreach (CodexNode node in form.Finals.Where(n => n.Revealed || n.NameAlwaysShown))
					Add(node.Name, $"Apotheosis of {entry.Name}", entry.Id, node.Id);
			}
			if (entry.Recipe != null)
			{
				// A recipe's ingredients find the recipe: the reverse lookup.
				foreach (CodexNode ingredient in entry.Recipe.Ingredients.Where(n => n.Revealed && !n.Unsearchable))
					Add(ingredient.Name, $"In {entry.Name}", entry.Id, ingredient.Id);
			}
		}
		return index;
	}

	private static string KindWord(CodexEntry entry) => entry.Kind switch
	{
		CodexKind.Spell => entry.IsPassive ? "Passive" : "Spell",
		CodexKind.Relic => "Relic",
		CodexKind.Boon => "Boon",
		CodexKind.Combination => "Recipe",
		_ => "Full Set Enchantment",
	};

	/// <summary>
	/// The names matching <paramref name="query"/>, best first: a name that starts with it, then a word in
	/// it that does, then anywhere inside it; shorter names first among equals. Case-insensitive.
	/// </summary>
	public List<CodexSearchHit> Search(string query, int max = 6)
	{
		string q = (query ?? string.Empty).Trim();
		if (q.Length == 0)
			return new List<CodexSearchHit>();
		var hits = new List<CodexSearchHit>();
		foreach (CodexSearchHit hit in SearchIndex)
		{
			int score = Score(hit.Name, q);
			if (score <= 0)
				continue;
			hits.Add(new CodexSearchHit { Name = hit.Name, What = hit.What, EntryId = hit.EntryId, NodeId = hit.NodeId, Score = score });
		}
		return hits
			.OrderByDescending(h => h.Score)
			.ThenBy(h => string.IsNullOrEmpty(h.NodeId) ? 0 : 1)
			.ThenBy(h => h.Name.Length)
			.ThenBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
			.Take(max)
			.ToList();
	}

	private static int Score(string name, string q)
	{
		if (name.StartsWith(q, StringComparison.OrdinalIgnoreCase))
			return 3;
		foreach (string word in name.Split(' ', '-', '\''))
			if (word.StartsWith(q, StringComparison.OrdinalIgnoreCase))
				return 2;
		return name.Contains(q, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
	}

	// ---- building it --------------------------------------------------------------------------------

	public static CodexModel Build(CodexContext context)
	{
		context ??= CodexContext.ForSave(null);
		var model = new CodexModel { Mode = context.Mode, Context = context };
		if (context.Mode == CodexMode.InRun)
		{
			string who = string.IsNullOrWhiteSpace(context.WizardName) ? "This run" : context.WizardName;
			model.RunLine = context.PlayerLevel > 0 ? $"{who}, level {context.PlayerLevel}" : who;
		}

		var builder = new Builder(context);
		foreach (CodexEntry spell in builder.Spells())
			model.Add(spell);
		foreach (CodexEntry relic in builder.Relics())
			model.Add(relic);
		foreach (CodexEntry boon in builder.Boons())
			model.Add(boon);
		foreach (CodexEntry combo in builder.Combinations())
			model.Add(combo);
		foreach (CodexEntry set in builder.Sets())
			model.Add(set);
		builder.LinkUsedIn(model.Entries);
		return model;
	}

	private void Add(CodexEntry entry)
	{
		entry.Order = Entries.Count;
		Entries.Add(entry);
	}

	/// <summary>A readable dump of the whole model, for _UiShot --codex-dump.</summary>
	public string Dump()
	{
		var text = new StringBuilder();
		text.AppendLine($"CodexModel: mode={Mode} entries={Total} discovered={DiscoveredCount}{(string.IsNullOrEmpty(RunLine) ? "" : $" run=\"{RunLine}\"")}");
		foreach (CodexFilter group in new[] { CodexFilter.Spells, CodexFilter.Items, CodexFilter.Recipes })
		{
			List<CodexEntry> list = Filtered(group);
			text.AppendLine($"== {group} ({list.Count(e => e.Discovered)}/{list.Count})");
			foreach (CodexEntry e in list)
			{
				text.Append($"  [{e.State}] {e.Id} \"{e.ShownName}\"");
				if (e.Held)
					text.Append($" held L{e.Level}/{e.LevelCap}");
				if (e.SetAside)
					text.Append(" set-aside");
				if (!string.IsNullOrEmpty(e.KindLine))
					text.Append($" ({e.KindLine})");
				if (e.Elements.Count > 0)
					text.Append($" {{{string.Join(",", e.Elements.Select(x => x.Weight > 1 ? $"{x.Element}x{x.Weight}" : x.Element.ToString()))}}}");
				if (!e.Discovered && !string.IsNullOrEmpty(e.Hint))
					text.Append($" hint=\"{e.Hint}\"");
				if (e.UsedIn.Count > 0)
					text.Append($" usedIn=[{string.Join(",", e.UsedIn)}]");
				text.AppendLine();
				foreach (CodexForm form in e.Forms)
				{
					if (e.Forms.Count > 1)
						text.AppendLine($"     form {form.Id} \"{form.Name}\"");
					AppendColumn(text, "L4", form.MutationGates, form.Mutations);
					AppendColumn(text, "L8", form.AscensionGates, form.Ascensions);
					AppendColumn(text, "final", new List<CodexChip>(), form.Finals);
				}
				if (e.Recipe != null)
				{
					text.AppendLine($"     recipe made={e.Recipe.Made} offered={e.Recipe.Offered} sealed={e.Recipe.Sealed}");
					foreach (CodexNode ingredient in e.Recipe.Ingredients)
						text.AppendLine($"       + {Node(ingredient)}");
					text.AppendLine($"       = {Node(e.Recipe.Result)}");
				}
			}
		}
		return text.ToString();
	}

	private static void AppendColumn(StringBuilder text, string label, List<CodexChip> gates, List<CodexNode> nodes)
	{
		if (nodes.Count == 0)
			return;
		text.AppendLine($"     {label}{(gates.Count > 0 ? $" gates[{string.Join("; ", gates)}]" : "")}");
		foreach (CodexNode node in nodes)
			text.AppendLine($"       - {Node(node)}");
	}

	private static string Node(CodexNode node)
	{
		string chips = node.Chips.Count > 0 ? $" [{string.Join("; ", node.Chips)}]" : string.Empty;
		string note = string.IsNullOrEmpty(node.Note) ? string.Empty : $" ({node.Note})";
		return $"{node.State} {node.Id} \"{node.ShownName}\"{chips}{note}";
	}

	// ---- the builder --------------------------------------------------------------------------------

	private sealed class Builder
	{
		private readonly CodexContext context;
		private readonly SaveData save;
		private readonly bool inRun;
		private readonly Dictionary<Element, int> counts = new();

		public Builder(CodexContext context)
		{
			this.context = context;
			save = context.Save;
			inRun = context.Mode == CodexMode.InRun;
			foreach (var pair in context.ElementCounts ?? new Dictionary<string, int>())
				if (Enum.TryParse(pair.Key, true, out Element element))
					counts[element] = pair.Value;
		}

		private bool SpellOwned(string id) => GlobalStatsManager.IsSpellUnlockedForLevelUp(save, id);

		private SpellData Held(string spellId) => context.HeldSpells?.FirstOrDefault(s => s != null
			&& s.Id.Equals(spellId, StringComparison.OrdinalIgnoreCase));

		private EquippedSpellInfo Preview(string spellId) => context.Equipped?.FirstOrDefault(s => s != null
			&& s.Id.Equals(spellId, StringComparison.OrdinalIgnoreCase));

		private bool Seen(string spellId, string branchId) => save != null && save.IsAscensionSeen(spellId, branchId);

		// -- spells --

		public IEnumerable<CodexEntry> Spells()
		{
			foreach ((SpellData template, List<SpellData> forms) in CodexSpellForms.All())
			{
				if (ComboCatalog.IsCombination(template.Id))
					continue;
				SpellData held = Held(template.Id);
				bool owned = SpellOwned(template.Id);
				var entry = new CodexEntry
				{
					Id = template.Id,
					Kind = CodexKind.Spell,
					Name = template.Name,
					Line = string.IsNullOrWhiteSpace(template.Tagline) ? template.Description : template.Tagline,
					Icon = template.Icon,
					Elements = CodexRows.FromWeights(template.GetElementWeights()),
					IsPassive = template.IsPassive,
					IndexElement = template.GetElementWeights().OrderByDescending(p => p.Value).Select(p => (Element?)p.Key).FirstOrDefault(),
					Own = inRun && template.Id.Equals(context.StarterSpellId ?? string.Empty, StringComparison.OrdinalIgnoreCase),
					Owned = owned,
					Held = held != null,
					Discovered = owned || held != null,
					SetAside = GlobalStatsManager.IsSpellRemovedFromPool(save, template.Id),
					Hint = UnlockCatalog.GetLockedHint(template.Id, UnlockKind.Spell),
					KindLine = template.IsPassive ? "Passive" : "Attack",
				};
				if (held != null)
				{
					// The run's own copy: its name, icon and tags are the form it was taken in.
					entry.Name = held.Name;
					entry.Icon = held.Icon ?? entry.Icon;
					entry.Elements = CodexRows.FromWeights(held.GetElementWeights());
					entry.Level = held.CurrentLevel;
					entry.LevelCap = held.MaxLevel;
				}

				for (int i = 0; i < forms.Count; i++)
				{
					SpellData form = forms[i];
					bool heldForm = held != null && (string.IsNullOrEmpty(form.SelectedVariantId)
						|| form.SelectedVariantId.Equals(held.SelectedVariantId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
					entry.Forms.Add(BuildForm(entry, form, heldForm ? held : null));
					if (heldForm)
						entry.CurrentForm = i;
				}

				entry.State = SpellState(entry, held);
				entry.Unmet = UnmetOf(entry);
				yield return entry;
			}
		}

		private CodexState SpellState(CodexEntry entry, SpellData held)
		{
			if (!entry.Discovered)
				return CodexState.Undiscovered;
			if (!inRun)
				return entry.Owned ? CodexState.Owned : CodexState.Known;
			if (held == null)
				return CodexState.Known;
			CodexForm form = entry.Forms.ElementAtOrDefault(entry.CurrentForm);
			if (form != null && form.AllNodes.Any(n => n.State == CodexNodeState.Ready && n.Milestone >= 8))
				return CodexState.Ready;
			bool done = held.IsApotheosis || held.CurrentLevel >= held.MaxLevel;
			return done ? CodexState.Taken : CodexState.Leveling;
		}

		// The fewest requirements still unmet on any next step: the near misses sort first in a run.
		private static int UnmetOf(CodexEntry entry)
		{
			CodexForm form = entry.Forms.ElementAtOrDefault(entry.CurrentForm);
			if (form == null)
				return int.MaxValue;
			int best = int.MaxValue;
			foreach (CodexNode node in form.Ascensions.Concat(form.Finals))
			{
				if (node.State is CodexNodeState.Taken or CodexNodeState.Closed)
					continue;
				int own = node.Chips.Count(c => c.Met == false) + form.AscensionGates.Count(c => c.Met == false);
				best = Math.Min(best, own);
			}
			return best;
		}

		private CodexForm BuildForm(CodexEntry entry, SpellData form, SpellData held)
		{
			SpellData shown = held ?? form;
			SpellVariant variant = string.IsNullOrEmpty(form.SelectedVariantId) ? null : form.SelectedVariant;
			var tree = new CodexForm
			{
				Id = string.IsNullOrEmpty(form.SelectedVariantId) ? form.Id : form.SelectedVariantId,
				Name = variant != null && !string.IsNullOrWhiteSpace(variant.ShortName) ? variant.ShortName : form.Name,
				Icon = form.Icon,
			};
			if (!entry.Discovered)
				return tree;

			// Level 4: shown whenever the spell is known, as the level 4 page always has.
			int level = held?.CurrentLevel ?? 0;
			tree.MutationGates.Add(held != null
				? new CodexChip { Text = $"Spell {Math.Min(level, 4)}/4", Met = level >= 4, Have = level, Need = 4 }
				: new CodexChip { Text = "Spell 4" });
			string mutated = held?.SelectedLevel4EvolutionId ?? string.Empty;
			foreach (SpellEvolutionOption evo in shown.Level4Options ?? new Godot.Collections.Array<SpellEvolutionOption>())
			{
				if (evo == null)
					continue;
				var node = EvolutionNode(form.Id, evo, 4, shown.Icon);
				if (held != null)
				{
					node.State = string.IsNullOrEmpty(mutated) ? (level == 3 ? CodexNodeState.Ready : CodexNodeState.Known)
						: mutated.Equals(evo.Id, StringComparison.OrdinalIgnoreCase) ? CodexNodeState.Taken
						: CodexNodeState.Closed;
					if (node.State == CodexNodeState.Taken)
						node.Note = "Chosen";
				}
				tree.Mutations.Add(node);
			}

			// Level 8.
			EquippedSpellInfo preview = held != null ? Preview(form.Id) : null;
			if (preview?.Ascensions != null && preview.Ascensions.Count > 0)
				BuildHeldAscensions(tree, form.Id, preview);
			else
				BuildAscensionsFromData(tree, form.Id, shown);

			// The final form.
			BuildFinals(tree, form, held, preview);
			return tree;
		}

		private static CodexNode EvolutionNode(string spellId, SpellEvolutionOption evo, int milestone, Texture2D fallback)
		{
			return new CodexNode
			{
				Id = evo.Id,
				SpellId = spellId,
				Milestone = milestone,
				Name = evo.DisplayName,
				Line = evo.Description,
				// The path's own picture (EvolutionIcons); the spell's only for one that has none.
				Icon = EvolutionIcons.Own(evo) ?? evo.Icon ?? fallback,
				State = CodexNodeState.Known,
			};
		}

		// A held spell's Ascensions, exactly as the run's preview (Player.BuildAscensionPreview) has them:
		// every gate measured, the shared ones on the column, a path's own under it (the browser's split).
		private void BuildHeldAscensions(CodexForm tree, string spellId, EquippedSpellInfo preview)
		{
			var branches = preview.Ascensions.Where(a => a != null).ToList();
			List<AscensionRequirement> first = branches.FirstOrDefault()?.Requirements ?? new List<AscensionRequirement>();
			var shared = first.Where(g => branches.All(b => (b.Requirements ?? new List<AscensionRequirement>()).Any(r => SameGate(r, g)))).ToList();
			foreach (AscensionRequirement gate in shared)
				tree.AscensionGates.AddRange(Chips(gate));
			foreach (AscensionInfo branch in branches)
			{
				var node = new CodexNode
				{
					Id = branch.Id,
					SpellId = spellId,
					Milestone = 8,
					Name = branch.DisplayName,
					Line = branch.Description,
					Icon = EvolutionIcons.Own(branch.Id) ?? branch.Icon ?? preview.Icon,
				};
				bool revealed = branch.Taken || branch.IsAvailable || Seen(spellId, branch.Id);
				node.State = branch.Taken ? CodexNodeState.Taken
					: branch.Closed ? CodexNodeState.Closed
					: !revealed ? CodexNodeState.Sealed
					: branch.Open ? CodexNodeState.Ready
					: CodexNodeState.Known;
				if (branch.Taken)
					node.Note = "Chosen";
				else if (branch.Closed)
					node.Note = "Not taken";
				if (!branch.Taken && !branch.Closed)
					foreach (AscensionRequirement gate in branch.Requirements ?? new List<AscensionRequirement>())
						if (!shared.Any(s => SameGate(s, gate)))
							node.Chips.AddRange(Chips(gate));
				if (node.Revealed && !branch.Closed && JoinsLine(spellId, branch.Id) is string joins)
					node.Note = string.IsNullOrEmpty(node.Note) ? joins : $"{node.Note} · {joins}";
				tree.Ascensions.Add(node);
			}
		}

		// Not held (or no run): every path of the spell, sealed until seen, its gates as the rule states them.
		private void BuildAscensionsFromData(CodexForm tree, string spellId, SpellData spell)
		{
			var options = (spell.Level8Options ?? new Godot.Collections.Array<SpellEvolutionOption>()).Where(o => o != null).ToList();
			if (options.Count == 0)
				return;
			int cap = Player.CardLevelCap(spell);
			// Not yet taught (Teachings): where it is learned, first, as the run's own gate list has it.
			if (!Teachings.HasLearned(save, Teaching.Ascension))
				tree.AscensionGates.Add(new CodexChip { Text = Teachings.SealedLine(save, Teaching.Ascension), Met = false });
			tree.AscensionGates.Add(new CodexChip { Text = $"Spell {cap}", Need = cap });
			tree.AscensionGates.Add(new CodexChip { Text = $"You {Player.AscensionMinimumPlayerLevel}", Need = Player.AscensionMinimumPlayerLevel });
			// The element gate is the path's own when it carries one, else any of the spell's; written once on
			// the column when every path asks the same, under each path when they differ.
			var gates = options.Select(o => Player.EvaluateElementGate(o, spell, counts)).ToList();
			bool sameElement = gates.All(g => g != null && gates[0] != null && g.AnyOf.Count == gates[0].AnyOf.Count && g.AnyOf.All(gates[0].AnyOf.Contains));
			if (sameElement && gates.Count > 0 && gates[0] != null)
				tree.AscensionGates.AddRange(ElementChips(gates[0], measured: false));
			for (int i = 0; i < options.Count; i++)
			{
				SpellEvolutionOption evo = options[i];
				CodexNode node = EvolutionNode(spellId, evo, 8, spell.Icon);
				node.State = Seen(spellId, evo.Id) ? CodexNodeState.Known : CodexNodeState.Sealed;
				if (!sameElement && gates[i] != null)
					node.Chips.AddRange(ElementChips(gates[i], measured: false));
				if (node.Revealed && JoinsLine(spellId, evo.Id) is string joins)
					node.Note = joins;
				tree.Ascensions.Add(node);
			}
		}

		// "Joins Starfall: Constellation" when a recipe the save knows names this path. Not before Conjunctions
		// may be shown (Teachings.IsShown): the line names a system the save has not met.
		private string JoinsLine(string spellId, string ascensionId)
		{
			if (!Teachings.IsShown(save, Teaching.Conjunctions))
				return null;
			foreach (ComboDefinition recipe in ComboCatalog.Naming(spellId, ascensionId))
			{
				if (!ComboCatalog.IsKnown(recipe, save))
					continue;
				ComboIngredient partner = ComboCatalog.PartnerOf(recipe, spellId);
				if (partner != null)
					return $"Joins {partner.SpellName}: {partner.AscensionName}";
			}
			return null;
		}

		private void BuildFinals(CodexForm tree, SpellData form, SpellData held, EquippedSpellInfo preview)
		{
			// One locked thing on show at a time (Teachings.IsShown, Eric's Q8): no final form, sealed or not,
			// until the Ascension it needs has been learned.
			if (!Teachings.IsShown(save, Teaching.Apotheosis))
				return;
			if (inRun)
			{
				// The wizard's own, on his own spell only (Player.BuildApotheosisPreview).
				ApotheosisPreview apotheosis = preview?.Apotheosis;
				ApotheosisDefinition definition = apotheosis == null && held == null
					? ApotheosisCatalog.All.FirstOrDefault(d => d.CharacterId.Equals(context.CharacterId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
						&& d.SpellId.Equals(form.Id, StringComparison.OrdinalIgnoreCase)
						&& (string.IsNullOrEmpty(d.VariantId) || d.VariantId.Equals(form.SelectedVariantId ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
					: null;
				if (apotheosis != null)
					tree.Finals.Add(FinalFromPreview(form.Id, apotheosis));
				else if (definition != null)
					tree.Finals.Add(FinalFromDefinition(form, definition));
				foreach (CodexNode final in tree.Finals)
					final.Owner = context.WizardName ?? string.Empty;
				return;
			}
			// No run: the form of every freed wizard whose final form ends this spell. A locked wizard's is
			// left out: a sealed node would name that he has one.
			foreach (ApotheosisDefinition definition in ApotheosisCatalog.All)
			{
				if (!definition.SpellId.Equals(form.Id, StringComparison.OrdinalIgnoreCase)
					|| (!string.IsNullOrEmpty(definition.VariantId) && !definition.VariantId.Equals(form.SelectedVariantId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
					|| !GlobalStatsManager.IsCharacterUnlocked(save, definition.CharacterId))
					continue;
				tree.Finals.Add(FinalFromDefinition(form, definition));
			}
		}

		private CodexNode FinalFromPreview(string spellId, ApotheosisPreview apotheosis)
		{
			bool revealed = apotheosis.Taken || apotheosis.Offered || Seen(spellId, apotheosis.FormId);
			var node = new CodexNode
			{
				Id = apotheosis.FormId,
				SpellId = spellId,
				Milestone = ApotheosisCatalog.MilestoneLevel,
				Name = apotheosis.FormName,
				Line = apotheosis.Description,
				Icon = apotheosis.Icon,
				NameAlwaysShown = true,
				State = apotheosis.Taken ? CodexNodeState.Taken
					: apotheosis.Closed ? CodexNodeState.Closed
					: !revealed ? CodexNodeState.Sealed
					: apotheosis.Offered ? CodexNodeState.Ready
					: CodexNodeState.Known,
			};
			if (apotheosis.Taken)
				node.Note = "Chosen";
			else if (apotheosis.Closed)
				node.Note = "Out of reach this run";
			else
				foreach (AscensionRequirement gate in apotheosis.Requirements)
					node.Chips.AddRange(Chips(gate));
			return node;
		}

		private CodexNode FinalFromDefinition(SpellData form, ApotheosisDefinition definition)
		{
			SpellEvolutionOption shape = definition.Form;
			var node = new CodexNode
			{
				Id = shape.Id,
				SpellId = form.Id,
				Milestone = ApotheosisCatalog.MilestoneLevel,
				Name = shape.DisplayName,
				Line = shape.Description,
				Icon = EvolutionIcons.Own(shape) ?? form.Icon,
				NameAlwaysShown = true,
				State = Seen(form.Id, shape.Id) ? CodexNodeState.Known : CodexNodeState.Sealed,
			};
			// The gate as ApotheosisCatalog.SpellMeetsGate states it (Eric, 2026-11-02): no path and no element
			// are named, so nothing here can give a sealed Ascension away.
			// Not yet taught (Teachings): where it is learned comes first, as the run's own gate list has it.
			if (!Teachings.HasLearned(save, Teaching.Apotheosis))
				node.Chips.Add(new CodexChip { Text = Teachings.SealedLine(save, Teaching.Apotheosis), Met = false });
			node.Chips.Add(new CodexChip { Text = $"Spell {form.MaxLevel}", Need = form.MaxLevel });
			// A Conjunction its wizard opens on has no Ascension and is combined from the start: its gate is
			// its max level (ApotheosisCatalog.OnStartingConjunction).
			if (!ApotheosisCatalog.OnStartingConjunction(definition))
			{
				node.Chips.Add(new CodexChip { Text = AscendedChip });
				node.Chips.Add(new CodexChip { Text = NotCombinedChip });
			}
			node.Chips.Add(new CodexChip { Text = $"You {ApotheosisCatalog.MinimumPlayerLevel}", Need = ApotheosisCatalog.MinimumPlayerLevel });
			if (!inRun)
			{
				CharacterData wizard = CharacterRoster.GetAll().FirstOrDefault(c => c.Id.Equals(definition.CharacterId, StringComparison.OrdinalIgnoreCase));
				if (wizard != null)
					node.Owner = wizard.Name;
			}
			return node;
		}

		// One run gate as chips. A run measured it, so each chip is met or not.
		private List<CodexChip> Chips(AscensionRequirement gate)
		{
			var chips = new List<CodexChip>();
			switch (gate.Kind)
			{
				case AscensionRequirementKind.SpellLevel:
					chips.Add(new CodexChip { Text = $"Spell {Math.Min(gate.Have, gate.Need)}/{gate.Need}", Met = gate.Met, Have = gate.Have, Need = gate.Need });
					break;
				case AscensionRequirementKind.PlayerLevel:
					chips.Add(new CodexChip { Text = $"You {Math.Min(gate.Have, gate.Need)}/{gate.Need}", Met = gate.Met, Have = gate.Have, Need = gate.Need });
					break;
				case AscensionRequirementKind.Element:
					chips.AddRange(ElementChips(gate, measured: true));
					break;
				case AscensionRequirementKind.Learned:
					// Where it is learned (Teachings.SealedLine), when the gate says.
					if (!gate.Met)
						chips.Add(new CodexChip { Text = string.IsNullOrEmpty(gate.Line) ? "Not yet learned" : gate.Line, Met = false });
					break;
				case AscensionRequirementKind.Ascended:
					chips.Add(new CodexChip { Text = AscendedChip, Met = gate.Met });
					break;
				case AscensionRequirementKind.NotCombined:
					chips.Add(new CodexChip { Text = NotCombinedChip, Met = gate.Met, Closed = gate.Closed });
					break;
				case AscensionRequirementKind.Ingredient:
					chips.Add(new CodexChip { Text = $"{gate.SpellName}: {gate.PathName}", Met = gate.Met, Closed = gate.Closed });
					break;
				case AscensionRequirementKind.OwnSpell:
					chips.Add(new CodexChip { Text = $"{gate.SpellName} is your own", Met = false, Closed = true });
					break;
				case AscensionRequirementKind.ElementCeiling:
					chips.Add(new CodexChip { Text = "A fourth element", Met = false, Closed = true });
					break;
			}
			return chips;
		}

		// The element gate's chips: met, only the element that met it; not, every element that would do.
		private List<CodexChip> ElementChips(AscensionRequirement gate, bool measured)
		{
			var chips = new List<CodexChip>();
			foreach (Element element in gate.AnyOf)
			{
				if (measured && gate.Met && element != gate.Element)
					continue;
				int held = element == gate.Element ? gate.Have : counts.TryGetValue(element, out int n) ? n : 0;
				chips.Add(measured
					? new CodexChip { Text = $"{element} {Math.Min(held, gate.Need)}/{gate.Need}", Element = element, Met = gate.Met, Have = held, Need = gate.Need }
					: new CodexChip { Text = $"{element} {gate.Need}", Element = element, Need = gate.Need });
			}
			return chips;
		}

		private static bool SameGate(AscensionRequirement a, AscensionRequirement b)
		{
			if (a == null || b == null || a.Kind != b.Kind || a.Have != b.Have || a.Need != b.Need || a.Element != b.Element)
				return false;
			var x = a.AnyOf ?? new List<Element>();
			var y = b.AnyOf ?? new List<Element>();
			return x.Count == y.Count && x.All(y.Contains);
		}

		// -- items --

		public IEnumerable<CodexEntry> Relics()
		{
			foreach (string id in ChestItemCatalog.AllItemIds)
			{
				bool held = context.HeldRelics?.Contains(id, StringComparer.OrdinalIgnoreCase) == true;
				bool seen = held || (save?.IsItemSeen(id) == true);
				ChestItemRarity rarity = ChestItemCatalog.GetRarity(id);
				var entry = new CodexEntry
				{
					Id = id,
					Kind = CodexKind.Relic,
					Name = ChestItemCatalog.GetDisplayName(id),
					Line = ChestItemCatalog.GetDescription(id),
					Icon = BonelightSkin.LoadTextureSafe(ChestItemCatalog.GetIconPath(id)),
					Elements = ChestItemCatalog.GetElementTags(id)
						.Select(t => Enum.TryParse(t.Element, true, out Element e) ? (e, t.Weight) : ((Element, int)?)null)
						.Where(t => t.HasValue).Select(t => t.Value).ToList(),
					Discovered = seen,
					Held = held,
					Hint = "Found in a chest.",
					KindLine = rarity == ChestItemRarity.Relic ? "Relic" : $"{rarity} relic",
					Rarity = rarity,
					SetIds = ChestItemCatalog.Sets.Where(s => s.RequiredItemIds.Contains(id, StringComparer.OrdinalIgnoreCase)).Select(s => s.Id).ToList(),
				};
				entry.State = !seen ? CodexState.Undiscovered : held ? CodexState.Taken : inRun ? CodexState.Known : CodexState.Owned;
				yield return entry;
			}
		}

		public IEnumerable<CodexEntry> Boons()
		{
			foreach (BoonDefinition boon in BoonCatalog.All)
			{
				bool held = context.HeldBoons?.Contains(boon.Id, StringComparer.OrdinalIgnoreCase) == true;
				bool owned = GlobalStatsManager.IsBoonUnlocked(save, boon.Id);
				bool seen = held || owned || (save?.IsItemSeen(boon.Id) == true);
				var entry = new CodexEntry
				{
					Id = boon.Id,
					Kind = CodexKind.Boon,
					Name = boon.Name,
					Line = BoonCatalog.ExplainEffect(boon),
					Icon = BonelightSkin.LoadTextureSafe(boon.IconPath),
					Elements = boon.ElementWeights
						.Select(t => Enum.TryParse(t.Element, true, out Element e) ? (e, t.Weight) : ((Element, int)?)null)
						.Where(t => t.HasValue).Select(t => t.Value).ToList(),
					Discovered = seen,
					Owned = owned,
					Held = held,
					Hint = UnlockCatalog.GetLockedHint(boon.Id, UnlockKind.Boon),
					KindLine = "Boon",
				};
				entry.State = !seen ? CodexState.Undiscovered
					: held ? CodexState.Taken
					: inRun ? CodexState.Known
					: owned ? CodexState.Owned : CodexState.Known;
				yield return entry;
			}
		}

		// -- recipes --

		public IEnumerable<CodexEntry> Combinations()
		{
			foreach (ComboDefinition recipe in ComboCatalog.All)
			{
				// One locked thing on show at a time (Teachings.IsShown, Eric's Q8): the Rite once it is owned or
				// Conjunctions are learned, and the recipes once the Apotheosis is.
				if (recipe.IsRite)
				{
					if (Teachings.IsShown(save, Teaching.SovereignRite))
						yield return RiteEntry(recipe);
					continue;
				}
				if (!Teachings.IsShown(save, Teaching.Conjunctions))
					continue;
				SpellData template = ComboCatalog.Template(recipe);
				ComboPreview preview = context.Combos?.FirstOrDefault(c => c != null && c.Id.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase));
				bool known = ComboCatalog.IsKnown(recipe, save) || preview != null;
				bool made = ComboCatalog.IsMet(recipe, save) || preview?.Taken == true;
				bool revealed = made || preview?.Revealed == true || preview?.Offered == true;
				var card = new CodexRecipe
				{
					Id = recipe.Id,
					Kind = CodexKind.Combination,
					Made = made,
					Offered = preview?.Offered == true,
					Sealed = preview?.Sealed == true,
				};
				foreach (ComboIngredient part in recipe.Ingredients)
					card.Ingredients.Add(IngredientNode(recipe, part, preview));
				// What seals it this run (the wizard's own spell, a fourth element) is a chip on the result.
				var result = new CodexNode
				{
					Id = recipe.Id,
					SpellId = recipe.Id,
					Milestone = ComboCatalog.MilestoneLevel,
					Name = template?.Name ?? recipe.Name,
					Line = template?.Description ?? string.Empty,
					Icon = template?.Icon,
					State = preview?.Taken == true ? CodexNodeState.Taken
						: card.Sealed ? CodexNodeState.Closed
						: !revealed ? CodexNodeState.Sealed
						: card.Offered ? CodexNodeState.Ready
						: CodexNodeState.Known,
				};
				if (card.Sealed)
					result.Note = "Sealed this run";
				else if (card.Offered)
					result.Note = "Waiting above the cards";
				else if (made && !inRun)
					result.Note = "Made";
				foreach (AscensionRequirement gate in preview?.Requirements ?? new List<AscensionRequirement>())
					if (gate.Kind is AscensionRequirementKind.OwnSpell or AscensionRequirementKind.ElementCeiling)
						result.Chips.AddRange(Chips(gate));
				card.Result = result;

				var entry = new CodexEntry
				{
					Id = recipe.Id,
					Kind = CodexKind.Combination,
					Name = result.Name,
					Line = result.Line,
					Icon = result.Icon,
					Elements = template != null ? CodexRows.FromWeights(template.GetElementWeights()) : new List<(Element, int)>(),
					Discovered = known,
					Owned = made,
					Held = preview?.Taken == true,
					Hint = "Two spells, each ascended down the path its recipe names, joined into one.",
					KindLine = "Conjunction",
					Recipe = card,
				};
				// The grid names the recipe by its result once made; before that by its parents.
				if (!revealed)
				{
					entry.Name = $"{recipe.First.SpellName} + {recipe.Second.SpellName}";
					entry.Line = "Two Ascensions, made one.";
					entry.Icon = template?.Icon;
				}
				entry.State = !known ? CodexState.Undiscovered
					: preview?.Taken == true ? CodexState.Taken
					: card.Offered ? CodexState.Ready
					: inRun ? CodexState.Known
					: made ? CodexState.Owned : CodexState.Known;
				entry.Unmet = preview?.Requirements?.Count(r => !r.Met) ?? int.MaxValue;
				yield return entry;
			}
		}

		// THE SOVEREIGN RITE (.ai/sovereign-spell.md): not two Ascensions but the Rite and whichever active spell it
		// is pointed at, so neither ingredient asks for level 8. The Rite carries its own gate - your level, the
		// active spells held, the fuel the others would pour in (every OTHER active spell's level) - read from
		// SovereignRite, never copied; the second ingredient is "any active spell you hold", with nothing to load
		// or look up; the result is that spell, anointed. Measured in a run, the rule as written otherwise.
		private CodexEntry RiteEntry(ComboDefinition recipe)
		{
			SpellData template = ComboCatalog.Template(recipe);
			bool known = ComboCatalog.IsKnown(recipe, save);
			bool made = ComboCatalog.IsMet(recipe, save);
			bool riteHeld = Held(recipe.First.SpellId) != null;
			var rite = new CodexNode
			{
				Id = recipe.First.SpellId,
				SpellId = recipe.First.SpellId,
				Milestone = -1,
				Name = recipe.First.SpellName,
				Line = template?.Description ?? string.Empty,
				Icon = template?.Icon,
				State = riteHeld ? CodexNodeState.Taken : CodexNodeState.Known,
			};
			if (inRun)
			{
				List<SpellData> active = SovereignRite.ActiveSpells(context.HeldSpells).ToList();
				int fuel = active.Count == 0 ? 0 : active.Max(target => SovereignRite.FuelFor(target, active));
				int level = context.PlayerLevel;
				rite.Chips.Add(new CodexChip { Text = $"You {Math.Min(level, SovereignRite.MinimumPlayerLevel)}/{SovereignRite.MinimumPlayerLevel}", Met = level >= SovereignRite.MinimumPlayerLevel, Have = level, Need = SovereignRite.MinimumPlayerLevel });
				rite.Chips.Add(new CodexChip { Text = $"{Math.Min(active.Count, SovereignRite.MinimumActiveSpells)}/{SovereignRite.MinimumActiveSpells} active spells", Met = active.Count >= SovereignRite.MinimumActiveSpells, Have = active.Count, Need = SovereignRite.MinimumActiveSpells });
				rite.Chips.Add(new CodexChip { Text = $"{Math.Min(fuel, SovereignRite.MinimumFuel)}/{SovereignRite.MinimumFuel} levels of fuel", Met = fuel >= SovereignRite.MinimumFuel, Have = fuel, Need = SovereignRite.MinimumFuel });
			}
			else
			{
				rite.Chips.Add(new CodexChip { Text = $"You {SovereignRite.MinimumPlayerLevel}", Need = SovereignRite.MinimumPlayerLevel });
				rite.Chips.Add(new CodexChip { Text = $"{SovereignRite.MinimumActiveSpells} active spells", Need = SovereignRite.MinimumActiveSpells });
				rite.Chips.Add(new CodexChip { Text = $"{SovereignRite.MinimumFuel} levels of fuel", Need = SovereignRite.MinimumFuel });
			}
			var anySpell = new CodexNode
			{
				Id = "any_active_spell",
				Milestone = -1,
				Name = recipe.Second.SpellName,
				Line = "The spell the Rite is pointed at.",
				State = CodexNodeState.Known,
				Unsearchable = true,
			};
			var result = new CodexNode
			{
				Id = recipe.Id,
				SpellId = recipe.Id,
				Milestone = ComboCatalog.MilestoneLevel,
				Name = $"{SovereignRite.NamePrefix}<your spell>",
				Line = template?.Description ?? string.Empty,
				Icon = template?.Icon,
				NameAlwaysShown = true,
				State = CodexNodeState.Known,
			};
			var card = new CodexRecipe { Id = recipe.Id, Kind = CodexKind.Combination, Made = made, Result = result };
			card.Ingredients.Add(rite);
			card.Ingredients.Add(anySpell);
			var entry = new CodexEntry
			{
				Id = recipe.Id,
				Kind = CodexKind.Combination,
				Name = recipe.Name,
				Line = string.IsNullOrWhiteSpace(template?.Tagline) ? template?.Description ?? string.Empty : template.Tagline,
				Icon = template?.Icon,
				Discovered = known,
				Owned = made,
				Hint = UnlockCatalog.GetLockedHint(recipe.First.SpellId, UnlockKind.Spell),
				KindLine = "Conjunction",
				Recipe = card,
			};
			entry.State = !known ? CodexState.Undiscovered : inRun ? CodexState.Known : made ? CodexState.Owned : CodexState.Known;
			return entry;
		}

		private CodexNode IngredientNode(ComboDefinition recipe, ComboIngredient part, ComboPreview preview)
		{
			AscensionRequirement gate = preview?.Requirements?.FirstOrDefault(r => r.Kind == AscensionRequirementKind.Ingredient
				&& r.SpellName.Equals(part.SpellName, StringComparison.OrdinalIgnoreCase));
			SpellData template = ContentValidator.LoadAllSpells().FirstOrDefault(s => s != null && s.Id.Equals(part.SpellId, StringComparison.OrdinalIgnoreCase));
			var node = new CodexNode
			{
				Id = $"{part.SpellId}:{part.AscensionId}",
				SpellId = part.SpellId,
				Milestone = 8,
				Name = part.AscensionName,
				Line = part.SpellName,
				Icon = EvolutionIcons.Own(part.AscensionId) ?? template?.Icon,
				OpensEntryId = part.SpellId,
				State = gate == null ? CodexNodeState.Known
					: gate.Met ? CodexNodeState.Taken
					: gate.Closed ? CodexNodeState.Closed
					: CodexNodeState.Known,
			};
			node.Chips.Add(gate != null
				? new CodexChip { Text = $"{part.SpellName} L8", Met = gate.Met, Closed = gate.Closed }
				: new CodexChip { Text = $"{part.SpellName} L8" });
			if (gate?.Closed == true)
				node.Note = "Took the other path";
			return node;
		}

		public IEnumerable<CodexEntry> Sets()
		{
			foreach (ChestSetDefinition set in ChestItemCatalog.Sets)
			{
				var held = context.HeldRelics ?? Array.Empty<string>();
				int heldCount = set.RequiredItemIds.Count(id => held.Contains(id, StringComparer.OrdinalIgnoreCase));
				bool complete = inRun && heldCount >= set.RequiredItemIds.Length;
				var card = new CodexRecipe { Id = set.Id, Kind = CodexKind.FullSet, Made = complete, PartialCount = set.PartialItemCount, HeldCount = heldCount };
				bool anySeen = false;
				foreach (string id in set.RequiredItemIds)
				{
					bool isHeld = held.Contains(id, StringComparer.OrdinalIgnoreCase);
					bool seen = isHeld || save?.IsItemSeen(id) == true;
					anySeen |= seen;
					var piece = new CodexNode
					{
						Id = id,
						SpellId = id,
						Milestone = -1,
						Name = ChestItemCatalog.GetDisplayName(id),
						Line = ChestItemCatalog.GetDescription(id),
						Icon = BonelightSkin.LoadTextureSafe(ChestItemCatalog.GetIconPath(id)),
						OpensEntryId = id,
						State = isHeld ? CodexNodeState.Taken : seen ? CodexNodeState.Known : CodexNodeState.Sealed,
					};
					if (inRun)
						piece.Note = isHeld ? "Held" : string.Empty;
					card.Ingredients.Add(piece);
				}
				if (set.PartialItemCount > 0 && set.PartialEffects != null && set.PartialEffects.Length > 0)
					card.Partial = $"{set.PartialItemCount} of {set.RequiredItemIds.Length}: " +
						string.Join(", ", set.PartialEffects.Select(e => $"{e.Label} {e.Value}"));
				card.Result = new CodexNode
				{
					Id = set.Id,
					SpellId = set.Id,
					Milestone = -1,
					Name = set.Name,
					Line = set.Description,
					Icon = BonelightSkin.LoadTextureSafe(set.IconPath),
					State = complete ? CodexNodeState.Taken : anySeen ? CodexNodeState.Known : CodexNodeState.Sealed,
					Note = inRun ? $"{heldCount}/{set.RequiredItemIds.Length} held" : string.Empty,
				};
				var entry = new CodexEntry
				{
					Id = set.Id,
					Kind = CodexKind.FullSet,
					Name = set.Name,
					Line = set.Description,
					Icon = card.Result.Icon,
					Elements = (set.ElementTags ?? Array.Empty<(string Element, int Weight)>())
						.Select(t => Enum.TryParse(t.Element, true, out Element e) ? (e, t.Weight) : ((Element, int)?)null)
						.Where(t => t.HasValue).Select(t => t.Value).ToList(),
					// Known once a piece has been seen: the chest page names the set beside its first piece.
					Discovered = anySeen,
					Held = complete,
					Hint = "Its pieces are found in chests.",
					KindLine = "Full Set Enchantment",
					Recipe = card,
					Level = heldCount,
					LevelCap = set.RequiredItemIds.Length,
				};
				entry.State = !anySeen ? CodexState.Undiscovered
					: complete ? CodexState.Taken
					: inRun && heldCount > 0 ? CodexState.Leveling
					: CodexState.Known;
				entry.Unmet = set.RequiredItemIds.Length - heldCount;
				yield return entry;
			}
		}

		/// <summary>Each spell's "Used in" and each relic's sets: every recipe it is an ingredient of.</summary>
		public void LinkUsedIn(List<CodexEntry> entries)
		{
			foreach (CodexEntry recipe in entries.Where(e => e.Recipe != null))
			{
				foreach (CodexNode ingredient in recipe.Recipe.Ingredients)
				{
					CodexEntry part = entries.FirstOrDefault(e => e.Id.Equals(ingredient.OpensEntryId, StringComparison.OrdinalIgnoreCase));
					// A recipe the save does not know is not named on its ingredient: its partner may be a secret.
					// The Sovereign Rite is its own first ingredient: it is not "used in" itself.
					if (part == null || part == recipe || !recipe.Discovered || part.UsedIn.Contains(recipe.Id))
						continue;
					part.UsedIn.Add(recipe.Id);
				}
			}
		}
	}
}

/// <summary>
/// Every spell's forms with their milestones filled in, built once a process: a copy per form (SpellVariants
/// Preview), or one duplicate of a plain spell given EnsureEvolutionCoverage - never the loaded template,
/// which the run's own copies come from. Held for the life of the process for the reason
/// ContentValidator.LoadAllSpells gives (a static list is a GC root, so no wrapper is collected mid-load).
/// </summary>
public static class CodexSpellForms
{
	private static List<(SpellData Template, List<SpellData> Forms)> all;

	public static IReadOnlyList<(SpellData Template, List<SpellData> Forms)> All()
	{
		if (all != null)
			return all;
		all = new List<(SpellData, List<SpellData>)>();
		foreach (SpellData template in ContentValidator.LoadAllSpells())
		{
			if (template == null || string.IsNullOrWhiteSpace(template.Id))
				continue;
			var forms = new List<SpellData>();
			if (template.HasVariants)
			{
				foreach (SpellVariant variant in template.Variants)
					if (SpellVariants.Preview(template, variant) is SpellData form)
						forms.Add(form);
			}
			else if (template.Duplicate(true) is SpellData copy)
			{
				SpellEvolutionCatalog.EnsureEvolutionCoverage(copy);
				forms.Add(copy);
			}
			if (forms.Count > 0)
				all.Add((template, forms));
		}
		return all;
	}
}

/// <summary>
/// Which relics and boons the player has met (SaveData.SeenItemIds): a chest that offered it, a level-up
/// that dealt it, or a run that held it. The Codex draws the rest as "???" until then. The same two rules
/// as AscensionSightings: written the moment it happens (a run quit from the pause screen never reaches the
/// game-over save), and never by a tool scene, which would reveal to the player what only a bot stood in
/// front of.
/// </summary>
public static class CodexSightings
{
	/// <summary>Remembers every id in <paramref name="itemIds"/> and saves if any was new. Returns how many were.</summary>
	public static int Record(Node context, IEnumerable<string> itemIds)
	{
		if (context == null || AscensionSightings.IsToolRun(context))
			return 0;
		var saveManager = context.GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager?.Data == null)
			return 0;
		int added = 0;
		foreach (string id in itemIds ?? Enumerable.Empty<string>())
			if (saveManager.Data.MarkItemSeen(id))
				added++;
		if (added > 0)
			saveManager.SaveGame();
		return added;
	}
}
