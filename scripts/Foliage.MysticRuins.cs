namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Mystic Ruins (stage_4, StageEnvironmentKind.Ruins). THE MOVE: the chapter's old decor case in
	/// Node2DGame.BuildDecorProps, set for set and in its order, so it scatters exactly as it did
	/// (--placement-hash). No canopy and no clutter yet.
	/// </summary>
	private static class MysticRuins
	{
		private const string Rock1 = "res://assets/bonelight/world/rock-1.png";
		private const string Rock2 = "res://assets/bonelight/world/rock-2.png";

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Ruins,
			Decor = new[]
			{
				// 0. Stones, in threes.
				new FoliageSet
				{
					Sprites = new[] { Rock1, Rock2 }, MinScale = 0.88f, MaxScale = 1.02f,
					Count = 18, ClumpSize = 3, Clump = ClumpBase.GroundAccent, Loners = 0.05f,
				},
				// 1. Crystals: the profile's RuinCount (24), never under 16.
				new FoliageSet
				{
					Sprites = new[] { "res://assets/bonelight/world/crystal-yellow.png", "res://assets/bonelight/world/crystal-violet.png" },
					MinScale = 0.95f, MaxScale = 1.12f,
					Count = 24, ClumpSize = 3, Clump = ClumpBase.Ruin, Loners = 0.06f,
				},
				// 2. Trees that grew up through the stones: gnarled survivors and dead ones, sparse.
				new FoliageSet
				{
					Grounded = new[] { "gnarled-1", "gnarled-2", "dead-tree-1" },
					Count = 48, ClumpSize = 2, Clump = ClumpBase.Tree, RadiusMin = 0.8f,
					Loners = 0.15f, MinSpacing = MatureTreeSpacing,
				},
				// 3. Brambles in the rubble.
				new FoliageSet
				{
					Grounded = new[] { "bramble-1", "bramble-2" },
					Count = 110, ClumpSize = 3, Clump = ClumpBase.Bush, Loners = 0.08f, MinSpacing = 40f,
				},
			},
		};
	}
}
