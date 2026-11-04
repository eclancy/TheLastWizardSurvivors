namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Blighted Swamp (stage_3, StageEnvironmentKind.Swamp). THE MOVE, before the pass: the three sets
	/// its case in Node2DGame.BuildDecorProps scattered, in the same order and with the same numbers, so
	/// `--placement-hash` prints what it printed before (.ai/terrain.md section 13, "Adding a chapter").
	/// </summary>
	private static class BlightedSwamp
	{
		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Swamp,
			Decor = new[]
			{
				// 0. Sedge tussocks and dark shrubs, waded through.
				new FoliageSet
				{
					Grounded = new[] { "reed-tuft-1", "reed-tuft-2", "swamp-shrub-1" },
					Count = 220, ClumpSize = 4, Clump = ClumpBase.Bush, Loners = 0.06f, MinSpacing = 36f,
				},
				// 1. Willows on the banks.
				new FoliageSet
				{
					Grounded = new[] { "willow-1", "willow-2" },
					Count = 50, ClumpSize = 2, Clump = ClumpBase.Tree, Loners = 0.15f, MinSpacing = MatureTreeSpacing,
				},
				// 2. Drowned snags hung with moss.
				new FoliageSet
				{
					Grounded = new[] { "swamp-snag-1", "swamp-snag-2" },
					Count = 60, ClumpSize = 3, Clump = ClumpBase.Tree, RadiusMin = 0.8f, Separation = 0.8f,
					Loners = 0.2f, MinSpacing = YoungTreeSpacing,
				},
			},
		};
	}
}
