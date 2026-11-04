namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Frozen Waste (stage_5, StageEnvironmentKind.Ice): THE MOVE, before the pass. The old Ice case of
	/// Node2DGame.BuildDecorProps, set for set and number for number, so --placement-hash proves it moved nothing.
	/// </summary>
	private static class FrozenWaste
	{
		private const string Rock1 = "res://assets/bonelight/world/rock-1.png";
		private const string Rock2 = "res://assets/bonelight/world/rock-2.png";

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Ice,
			Decor = new[]
			{
				// 0. Stones, in small groups.
				new FoliageSet
				{
					Sprites = new[] { Rock1, Rock2 }, MinScale = 0.80f, MaxScale = 0.96f,
					Count = 16, ClumpSize = 3, Clump = ClumpBase.GroundAccent, RadiusMin = 1f, RadiusMax = 0.82f,
					Separation = 0.86f, Loners = 0.05f,
				},
				// 1. Crystals.
				new FoliageSet
				{
					Sprites = new[] { "res://assets/bonelight/world/crystal-yellow.png", "res://assets/bonelight/world/crystal-violet.png" },
					MinScale = 0.82f, MaxScale = 0.96f,
					Count = 10, ClumpSize = 3, Clump = ClumpBase.Ruin, RadiusMin = 0.78f, RadiusMax = 0.78f,
					Separation = 0.82f, Loners = 0.06f,
				},
				// 2. Smaller stones, in twos.
				new FoliageSet
				{
					Sprites = new[] { Rock1, Rock2 }, MinScale = 0.54f, MaxScale = 0.74f,
					Count = 6, ClumpSize = 2, Clump = ClumpBase.GroundAccent, RadiusMin = 0.60f, RadiusMax = 0.60f,
					Separation = 0.70f, Loners = 0.04f,
				},
			},
		};
	}
}
