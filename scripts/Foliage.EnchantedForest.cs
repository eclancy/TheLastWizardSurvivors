namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Enchanted Forest (stage_0): the pilot of the foliage pass (2026-11-03, .ai/terrain.md section 13).
	/// Stands and clearings: the mature trees massed in dark stands on a moss floor, the young trees and the
	/// bushes in three sizes crowding the stands' fringes where the light gets in, the clearings between left
	/// open to fight in, and grass, specks and litter in the detail layer.
	/// </summary>
	/// <remarks>
	/// These are the pilot's numbers exactly, moved here from Woodland, LevelGenerator, Node2DGame's decor
	/// switch and GroundDetail's table when the pass was made a profile per chapter: the same sets in the same
	/// order, so every roll is salted as before, and `--placement-hash` printed the same hash for the Forest
	/// before and after the move.
	/// </remarks>
	private static class EnchantedForest
	{
		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Forest,

			// Two octaves of value noise, 18 and 7 cells: about 28% of the ground is stand (the moss floor),
			// 20% fringe, half clearing - measured on the field, not guessed.
			Canopy = new CanopyProfile
			{
				Shape = CanopyShape.Stands,
				StandFloorTerrain = "moss",
				ClearingFloorTerrain = "meadow",
			},

			// THE NUMBERS ARE LARGE BECAUSE THE FIELD IS (.ai/terrain.md sections 4 and 10): a count is per
			// 8400 x 8400, and a habitat set's is its density INSIDE its habitat, so a stand is as thick in a
			// chunk that holds a sliver of one as in a chunk that is all wood. Overall a chunk carries about
			// 1.5x the trees and 1.6x the bushes the even scatter did, all of it where the wood is.
			Decor = new[]
			{
				// The five medium bushes, on the fringes most.
				new FoliageSet
				{
					Grounded = new[] { "bush-round-1", "bush-round-2", "bush-wide-1", "bush-berry-1", "bush-berry-2" },
					Count = 700, ClumpSize = 4, Clump = ClumpBase.Bush, RadiusMin = 0.8f, Separation = 0.8f,
					Loners = 0.04f, MinSpacing = 44f, Habitat = new Woodland.Habitat(0.25f, 1f, 0.45f),
				},
				// The mature trees, massed in the stands. The big oaks twice over: a stand is massed crowns,
				// and the oak's is the widest (240 px).
				new FoliageSet
				{
					Grounded = new[] { "oak-large-1", "oak-large-2", "oak-large-1", "oak-large-2", "oak-medium-1", "oak-medium-2", "fir-1", "fir-2" },
					Count = 1800, ClumpSize = 5, Clump = ClumpBase.Stand,
					Loners = 0.03f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0f, 0.15f, 1f),
				},
				// The young trees, on the fringes.
				new FoliageSet
				{
					Grounded = new[] { "birch-1", "birch-2", "sapling-1" },
					Count = 480, ClumpSize = 3, Clump = ClumpBase.Tree, RadiusMin = 0.6f, RadiusMax = 0.7f, Separation = 0.5f,
					Loners = 0.06f, MinSpacing = YoungTreeSpacing, Habitat = new Woodland.Habitat(0.06f, 1f, 0.35f),
				},
				// Rocks, even, as they always were.
				new FoliageSet
				{
					Sprites = new[] { "res://assets/bonelight/world/rock-1.png", "res://assets/bonelight/world/rock-2.png" },
					MinScale = 0.88f, MaxScale = 1.02f,
					Count = 240, ClumpSize = 3, Clump = ClumpBase.GroundAccent, Loners = 0.05f,
				},
				// The two new bush sizes come after the rocks, so the rocks stay exactly where they were: a
				// knee-high shrub at the wood's feet, and a thicket as tall as a man at the edge of every stand.
				new FoliageSet
				{
					Grounded = new[] { "bush-small-1", "bush-small-2", "bush-small-3" },
					Count = 420, ClumpSize = 4, Clump = ClumpBase.Bush, RadiusMin = 0.6f, RadiusMax = 0.8f, Separation = 0.6f,
					Loners = 0.08f, MinSpacing = 26f, Habitat = new Woodland.Habitat(0.35f, 1f, 0.3f),
				},
				new FoliageSet
				{
					Grounded = new[] { "bush-large-1", "bush-large-2" },
					Count = 420, ClumpSize = 2, Clump = ClumpBase.Bush, RadiusMin = 0.6f, RadiusMax = 0.9f, Separation = 0.8f,
					Loners = 0.02f, MinSpacing = 96f, Habitat = new Woodland.Habitat(0f, 1f, 0.9f),
				},
			},

			// THE BOTTOM RUNGS (tools/art/detail_forest.py): short grass across all the open ground, long grass
			// crowding a stand's fringe, wildflower specks in the clearings, leaf litter on the moss under the
			// trees. Open turf goes from about one cell in thirteen carrying something to about one in four in a
			// clearing and one in three on a fringe - all of it within about ten L* of the turf (.ai/terrain.md
			// section 13 measured it). Densities are the open ground's, before the habitat weight.
			Clutter = new[]
			{
				GroundDetail.Clutter("grass_short", 0.13f, 5, 1f, 1f, 0.4f),
				GroundDetail.Clutter("grass_tall", 0.11f, 4, 0.1f, 1f, 0.5f),
				GroundDetail.Clutter("specks", 0.03f, 6, 1f, 0.5f, 0.1f),
				GroundDetail.Clutter("pebbles", 0.006f, 1, 1f, 0.6f, 0.3f),
				GroundDetail.Clutter("litter", 0.08f, 5, 0.3f, 0.8f, 1f, "moss"),
				GroundDetail.Clutter("grass_short", 0.03f, 5, 1f, 1f, 0.3f, "moss"),
				GroundDetail.Clutter("grass_short", 0.08f, 5, 1f, 1f, 1f, "meadow"),
				GroundDetail.Clutter("specks", 0.04f, 5, 1f, 1f, 1f, "meadow"),
				GroundDetail.Clutter("grass_tall", 0.04f, 4, 0f, 1f, 0.2f, "meadow"),
			},
		};
	}
}
