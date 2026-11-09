using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// A chapter's foliage pass, as data: its canopy (where its growth stands thick and where it opens), its
/// decor sets in the order they are scattered, and the clutter rules its ground detail adds. Each chapter's
/// is built in its own file, Foliage.&lt;Chapter&gt;.cs; a chapter with none draws exactly as it did before
/// the pass. .ai/terrain.md section 13, "Adding a chapter".
/// </summary>
public sealed class ChapterFoliage
{
	public StageEnvironmentKind Kind { get; init; }

	/// <summary>The field the ground, the detail and the decor read (Woodland). Null: no habitats, no stand
	/// floor, and no set or clutter rule here may name a habitat.</summary>
	public CanopyProfile? Canopy { get; init; }

	/// <summary>
	/// EVERY decor set the chapter scatters, in order - the habitat sets and the plain ones (rocks) alike,
	/// because a set's rolls are salted by its place in this list (Node2DGame.FurnishDecorSet): put a new set
	/// at the end and every set before it stays exactly where it was. Null: the chapter's old case in
	/// Node2DGame.BuildDecorProps scatters it, as before the pass.
	/// </summary>
	public IReadOnlyList<FoliageSet>? Decor { get; init; }

	/// <summary>
	/// Ground-detail rules appended after the chapter's own in GroundDetail (GroundDetail.Clutter: a set, a
	/// density and a habitat). Appended, so every older rule keeps its index and its noise, and decided only
	/// on cells no older rule took: adding clutter moves no flower, reed or lily already on the ground.
	/// </summary>
	public IReadOnlyList<GroundDetail.Rule> Clutter { get; init; } = Array.Empty<GroundDetail.Rule>();
}

/// <summary>What a set's clump sizes are measured from: Node2DGame's own numbers for each kind of thing.</summary>
public enum ClumpBase
{
	/// <summary>Bushes: radius 140-260, clumps 420 apart.</summary>
	Bush,
	/// <summary>Trees scattered evenly: radius 220-420, clumps 760 apart.</summary>
	Tree,
	/// <summary>Trees massed in a stand: radius 120-320, clumps 420 apart, so neighbouring clumps run together.</summary>
	Stand,
	/// <summary>Rocks and small things on the ground: radius 80-170, clumps 250 apart.</summary>
	GroundAccent,
	/// <summary>Ruins and crystals: radius 180-340, clumps 980 apart.</summary>
	Ruin,
}

/// <summary>
/// One decor set: what it scatters, how many (per 8400 x 8400 - of its habitat, for a habitat set), in what
/// clumps, and where it grows. Either <see cref="Grounded"/> (trees and bushes standing on a ground line, by
/// id from <see cref="Manifest"/>) or <see cref="Sprites"/> (rocks and crystals by path, a body on layer 1).
/// </summary>
public sealed record FoliageSet
{
	/// <summary>Grounded sprite ids in <see cref="Manifest"/>; one is picked per member, so an id named twice
	/// is twice as common.</summary>
	public string[]? Grounded { get; init; }
	/// <summary>A manifest in trees.json's shape (tools/art/trees.py): the shared trees, or a chapter's own.</summary>
	public string Manifest { get; init; } = WorldSprites.TreesManifest;
	/// <summary>Plain sprites by res path, centred on the point, each with a small body on layer 1.</summary>
	public string[]? Sprites { get; init; }
	public float MinScale { get; init; } = 1f;
	public float MaxScale { get; init; } = 1f;
	/// <summary>Plain sprites: tilted up to 8 degrees either way, and now and then flipped upside down.</summary>
	public bool RandomRotation { get; init; }
	public bool AllowYFlip { get; init; }

	public int Count { get; init; }
	public bool Clustered { get; init; } = true;
	/// <summary>Members a clump, about (one either way).</summary>
	public int ClumpSize { get; init; } = 3;
	public ClumpBase Clump { get; init; } = ClumpBase.Bush;
	/// <summary>Multipliers on the clump base's radius range and centre separation.</summary>
	public float RadiusMin { get; init; } = 1f;
	public float RadiusMax { get; init; } = 1f;
	public float Separation { get; init; } = 1f;
	/// <summary>The share of the set scattered alone rather than in clumps.</summary>
	public float Loners { get; init; }
	/// <summary>Grounded: how far apart two of the set's own stand, centre to centre, within a chunk.</summary>
	public float MinSpacing { get; init; }
	/// <summary>Where it grows (Woodland), or null to scatter evenly. Needs the chapter's canopy.</summary>
	public Woodland.Habitat? Habitat { get; init; }
}

/// <summary>Every chapter's foliage, each built in its own file.</summary>
public static partial class Foliage
{
	/// <summary>A mature tree keeps this from the others of its set in its chunk, centre to centre.</summary>
	public const float MatureTreeSpacing = 120f;
	/// <summary>A young tree (a birch, a sapling) likewise.</summary>
	public const float YoungTreeSpacing = 72f;
	/// <summary>The furthest a clump of trees may reach past its own chunk (Node2DGame.Footing.cs: only the eight
	/// neighbours' trunks can then come within the clearance of a chunk's own).</summary>
	public const float MaxClumpReach = 680f;

	private static ChapterFoliage[]? chapters;

	/// <summary>
	/// Every chapter's foliage. THIS LIST IS WRITTEN ONCE: every open-air chapter is already named, each by a
	/// nested class in its own file that starts as a stand-in returning null, so a chapter's builder edits
	/// only Foliage.&lt;Chapter&gt;.cs and never this line. A chapter not listed here (the Cave, the castle) is
	/// one line to add, by the integrator.
	/// </summary>
	public static IReadOnlyList<ChapterFoliage> All => chapters ??= new ChapterFoliage?[]
	{
		EnchantedForest.Build(),
		FallowFields.Build(),
		BrokenCircle.Build(),
		BlightedSwamp.Build(),
		MysticRuins.Build(),
		FrozenWaste.Build(),
		ScorchedSands.Build(),
		Emberdeep.Build(),
		// THE FAR SHORE (.ai/second-half.md 4.3, marked by F0): each chapter's piece writes Foliage.<Chapter>.cs
		// (.ai/terrain.md 13, "Adding a chapter") and replaces its own comment line with its Build().
		WhiteDowns.Build(),
		// (F6's line is below; leave this one as it is)
		// F6: DrownedShore.Build(),
		// (F7's line is below; leave this one as it is)
		// F7: ThreeWays.Build(),
		// (F8's line is below; leave this one as it is)
		// F8: TwelfthHour.Build(),
		// (F9's line is below; leave this one as it is)
		// F9: ThunderFell.Build(),
		// (F10's line is below; leave this one as it is)
		// F10: BronzeIsle.Build(),
	}.Where(c => c != null).Select(c => c!).ToArray();

	/// <summary>The chapter's foliage, or null when it has none.</summary>
	public static ChapterFoliage? For(StageEnvironmentKind kind)
	{
		foreach (ChapterFoliage chapter in All)
			if (chapter.Kind == kind)
				return chapter;
		return null;
	}

	/// <summary>The chapter's canopy, or null.</summary>
	public static CanopyProfile? CanopyFor(StageEnvironmentKind kind) => For(kind)?.Canopy;

	/// <summary>
	/// RegressionChecks: one foliage a kind; a habitat (on a set or a clutter rule) only where there is a
	/// canopy; every set says what it scatters and how many; a set of trees keeps its clumps within
	/// <see cref="MaxClumpReach"/> of its chunk; a stand floor that is one of the chapter's palette terrains;
	/// and every sprite a set names is in its manifest and imported (a sprite missing there is dropped from
	/// the set at run time with nothing on screen to say so).
	/// </summary>
	public static void Validate(List<string> warnings)
	{
		var seen = new HashSet<StageEnvironmentKind>();
		var manifests = new Dictionary<string, Dictionary<string, GroundedSprite>>();
		foreach (ChapterFoliage chapter in All)
		{
			string who = $"Foliage ({chapter.Kind})";
			if (!seen.Add(chapter.Kind))
				warnings.Add($"{who}: two chapters' foliage claim this kind.");
			CanopyProfile? canopy = chapter.Canopy;
			if (canopy != null)
			{
				IReadOnlyList<string> terrains = new LevelGenerator().Describe(chapter.Kind).Terrains;
				foreach (string? t in new[] { canopy.StandFloorTerrain, canopy.ClearingFloorTerrain })
					if (t != null && !terrains.Contains(t))
						warnings.Add($"{who}: the canopy names '{t}', which is not one of the palette's terrains ({string.Join(", ", terrains)}), so its tiles are never built.");
				if (canopy.ClearingFloorTerrain != null && canopy.StandFloorTerrain == null)
					warnings.Add($"{who}: a clearing floor without a stand floor sorts nothing.");
			}
			if (chapter.Decor != null)
			{
				for (int i = 0; i < chapter.Decor.Count; i++)
				{
					FoliageSet set = chapter.Decor[i];
					string what = $"{who} set {i}";
					if ((set.Grounded == null) == (set.Sprites == null))
						warnings.Add($"{what}: name either Grounded sprites or plain Sprites, not both or neither.");
					if (set.Count <= 0)
						warnings.Add($"{what}: a count of {set.Count} scatters nothing.");
					if (set.Habitat != null && canopy == null)
						warnings.Add($"{what}: a habitat with no canopy to read it by.");
					if (set.Habitat != null && set.Sprites != null)
						warnings.Add($"{what}: a habitat is for grounded sets (trees and bushes) only.");
					if (set.Grounded != null && set.Clustered && RadiusOf(set.Clump) * set.RadiusMax > MaxClumpReach)
						warnings.Add($"{what}: its clumps reach {RadiusOf(set.Clump) * set.RadiusMax:0} px past their chunk, over {MaxClumpReach}.");
					if (set.Grounded != null)
					{
						if (!manifests.TryGetValue(set.Manifest, out Dictionary<string, GroundedSprite>? manifest))
							manifests[set.Manifest] = manifest = WorldSprites.LoadManifest(set.Manifest);
						foreach (string id in set.Grounded)
							if (!manifest.TryGetValue(id, out GroundedSprite? sprite) || sprite.Texture == null)
								warnings.Add($"{what}: '{id}' is not in {set.Manifest}, or its PNG is not imported.");
					}
					foreach (string path in set.Sprites ?? Array.Empty<string>())
						if (!ResourceLoader.Exists(path))
							warnings.Add($"{what}: no sprite at {path}.");
				}
			}
			foreach (GroundDetail.Rule rule in chapter.Clutter)
			{
				if (rule.Habitat == null)
					warnings.Add($"{who}: the clutter rule '{rule.Set}' has no habitat - put an ordinary rule in GroundDetail.");
				else if (canopy == null)
					warnings.Add($"{who}: the clutter rule '{rule.Set}' has a habitat and the chapter no canopy.");
			}
		}
	}

	// The clump bases' largest radius, Node2DGame's defaults (its exported numbers; nothing overrides them).
	private static float RadiusOf(ClumpBase clump) => clump switch
	{
		ClumpBase.Tree => 420f,
		ClumpBase.Stand => 320f,
		ClumpBase.GroundAccent => 170f,
		ClumpBase.Ruin => 340f,
		_ => 260f,
	};
}
