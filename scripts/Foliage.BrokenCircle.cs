namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Broken Circle (stage_10, the high heath). THE MOVE: the chapter's decor exactly as Node2DGame's Heath
	/// case scattered it, the same five sets in the same order, so every roll is salted as before
	/// (--placement-hash unchanged). No canopy yet.
	/// </summary>
	private static class BrokenCircle
	{
		private const string Manifest = "res://assets/bonelight/world/heath/heath.json";

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Heath,
			Decor = new[]
			{
				// 0. The stones of the old circles, standing in twos and threes.
				new FoliageSet
				{
					Grounded = new[] { "standing-stone-1", "standing-stone-2", "standing-stone-3" }, Manifest = Manifest,
					Count = 70, ClumpSize = 2, Clump = ClumpBase.Tree, RadiusMin = 0.7f, RadiusMax = 0.8f,
					Loners = 0.3f, MinSpacing = YoungTreeSpacing,
				},
				// 1. Fallen stones, apart.
				new FoliageSet
				{
					Grounded = new[] { "fallen-stone-1", "fallen-stone-2" }, Manifest = Manifest,
					Count = 40, ClumpSize = 1, Clump = ClumpBase.Tree, Loners = 0.5f, MinSpacing = MatureTreeSpacing,
				},
				// 2. Gorse and bramble in low drifts.
				new FoliageSet
				{
					Grounded = new[] { "gorse-1", "gorse-2", "bramble-2" }, Manifest = Manifest,
					Count = 180, ClumpSize = 4, Clump = ClumpBase.Bush, RadiusMax = 1.3f, Loners = 0.08f, MinSpacing = 40f,
				},
				// 3. A dead tree alone here and there.
				new FoliageSet
				{
					Grounded = new[] { "dead-tree-1", "gnarled-2" },
					Count = 22, ClumpSize = 1, Clump = ClumpBase.Tree, Separation = 1.3f, Loners = 0.4f, MinSpacing = MatureTreeSpacing,
				},
				// 4. Granite lying about.
				new FoliageSet
				{
					Sprites = new[] { "res://assets/bonelight/world/rock-1.png", "res://assets/bonelight/world/rock-2.png" },
					MinScale = 0.86f, MaxScale = 1.06f,
					Count = 50, ClumpSize = 3, Clump = ClumpBase.GroundAccent, Loners = 0.05f,
				},
			},
		};
	}
}
