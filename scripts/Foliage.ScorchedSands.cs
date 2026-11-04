namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Scorched Sands (stage_6, StageEnvironmentKind.Desert). THE MOVE ONLY: the chapter's old decor case
	/// in Node2DGame.BuildDecorProps, set for set and number for number, so --placement-hash is unchanged.
	/// </summary>
	private static class ScorchedSands
	{
		private const string Rock1 = "res://assets/bonelight/world/rock-1.png";
		private const string Rock2 = "res://assets/bonelight/world/rock-2.png";

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Desert,
			Decor = new[]
			{
				// 0. Stones, in small groups.
				new FoliageSet
				{
					Sprites = new[] { Rock1, Rock2 }, MinScale = 0.84f, MaxScale = 0.98f,
					Count = 12, ClumpSize = 3, Clump = ClumpBase.GroundAccent, RadiusMin = 0.86f, RadiusMax = 0.86f,
					Separation = 0.9f, Loners = 0.05f,
				},
				// 1. Crystals.
				new FoliageSet
				{
					Sprites = new[] { "res://assets/bonelight/world/crystal-yellow.png", "res://assets/bonelight/world/crystal-violet.png" },
					MinScale = 0.88f, MaxScale = 1.02f,
					Count = 12, ClumpSize = 3, Clump = ClumpBase.Ruin, RadiusMin = 0.86f, RadiusMax = 0.86f,
					Separation = 0.9f, Loners = 0.06f,
				},
				// 2. Smaller stones, in twos.
				new FoliageSet
				{
					Sprites = new[] { Rock1, Rock2 }, MinScale = 0.58f, MaxScale = 0.78f,
					Count = 8, ClumpSize = 2, Clump = ClumpBase.GroundAccent, RadiusMin = 0.72f, RadiusMax = 0.72f,
					Separation = 0.78f, Loners = 0.04f,
				},
				// 3. Sun-bleached dead trees, mostly alone.
				new FoliageSet
				{
					Grounded = new[] { "bleached-tree-1", "bleached-tree-2" },
					Count = 26, ClumpSize = 2, Clump = ClumpBase.Tree, Loners = 0.45f, MinSpacing = MatureTreeSpacing,
				},
				// 4. Dead scrub in loose drifts.
				new FoliageSet
				{
					Grounded = new[] { "dead-bush-1", "dead-bush-2", "dry-scrub-1" },
					Count = 150, ClumpSize = 3, Clump = ClumpBase.Bush, RadiusMin = 1.2f, RadiusMax = 1.3f,
					Loners = 0.2f, MinSpacing = 40f,
				},
			},
		};
	}
}
