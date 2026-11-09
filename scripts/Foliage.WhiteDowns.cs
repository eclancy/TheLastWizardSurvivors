namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The White Downs (stage_14, F3): chalk downland at night (.ai/second-half.md 4.2), on the foliage groundwork
	/// (.ai/terrain.md section 13, "Adding a chapter"). Its stands are not woods: they are CLUMPS - a ring of beeches
	/// on a crown, as the downs carry them, with hawthorn and juniper scrub on the flanks - and the cropped turf
	/// between them is what the chapter is. Its sightlines are long, which is the boss's whole fight: the Unstrung
	/// Hunter draws on the wizard from far off, and what stops him is a barrow (LevelGenerator.Downs.cs), never a
	/// tree. So nothing here is cover: the beeches' crowns fade over him, their trunks are thin, the scrub and the
	/// flints block nothing, and the sarsens lie knee-high.
	/// </summary>
	/// <remarks>
	/// THE LADDER, largest first (tools/art/props_downs.py, downs.json; detail_downs.py). Beeches in their clump on a
	/// crown (180-250 px, grey trunks); hawthorn, wind-shaped and low, on the clumps' flanks and now and then alone on
	/// the open down; juniper on the flanks; the grey wethers - sarsen boulders lying in the turf in loose flocks,
	/// like the sheep they are named for; chalk breaking the turf and the heaps of flints the shepherds picked; and in
	/// the detail layer harebells and thyme on the open down, the cropped grass and rabbit scrapes thick toward the
	/// scrub, where the warrens are. Nothing warm, nothing in a line, nothing hound- or hart-shaped: the chapter's two
	/// secrets are a hound and a hart cut in the chalk (SecretCatalog.WhiteDowns), and their tells are fresh chalk
	/// spilled where the turf was cut, which the old chalk here stays well under.
	///
	/// THE FIELD: the Forest's two octaves of value noise (CanopyShape.Stands) on a wider lattice, read at the heath's
	/// high thresholds, so a clump is a crown's worth and most of the down is open. Salts 0x5F12-4 (the next free
	/// block after the Emberdeep's 0x5E02-4).
	/// </remarks>
	private static class WhiteDowns
	{
		private const string Manifest = "res://assets/bonelight/world/downs/downs.json";

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.WhiteDowns,

			Canopy = new CanopyProfile
			{
				Shape = CanopyShape.Stands,
				StandLattice = 24,
				EdgeLattice = 8,
				EdgeWeight = 0.28f,
				ClearingEnd0 = 0.54f,
				ClearingEnd1 = 0.6f,
				StandStart0 = 0.64f,
				StandStart1 = 0.7f,
				StandFloor = 0.76f,
				SaltStand = 0x5F12,
				SaltEdge = 0x5F13,
				SaltGap = 0x5F14,
				StandShareMin = 0.03f,
				StandShareMax = 0.2f,
				ClearingShareMin = 0.55f,
				ClearingShareMax = 0.88f,
			},

			// A habitat set's Count is its density INSIDE its habitat (per 8400 x 8400 of it).
			Decor = new[]
			{
				// 0. Beeches in their clump on a crown: one clump a crown, a handful of trees.
				new FoliageSet
				{
					Grounded = new[] { "beech-1", "beech-2", "beech-1" }, Manifest = Manifest,
					Count = 700, ClumpSize = 5, Clump = ClumpBase.Stand, RadiusMin = 0.5f, RadiusMax = 0.7f, Separation = 0.9f,
					Loners = 0.02f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0f, 0.3f, 1f),
				},
				// 1. Hawthorn on a clump's flanks, and now and then alone out on the open down.
				new FoliageSet
				{
					Grounded = new[] { "hawthorn-1", "hawthorn-2" }, Manifest = Manifest,
					Count = 320, ClumpSize = 2, Clump = ClumpBase.Tree, RadiusMin = 0.4f, RadiusMax = 0.6f, Separation = 0.8f,
					Loners = 0.35f, MinSpacing = YoungTreeSpacing, Habitat = new Woodland.Habitat(0.03f, 1f, 0.3f),
				},
				// 2. Juniper, the down's scrub, on the flanks.
				new FoliageSet
				{
					Grounded = new[] { "juniper-1", "juniper-2" }, Manifest = Manifest,
					Count = 800, ClumpSize = 4, Clump = ClumpBase.Bush, RadiusMin = 0.5f, RadiusMax = 0.8f, Separation = 0.7f,
					Loners = 0.08f, MinSpacing = 36f, Habitat = new Woodland.Habitat(0.05f, 1f, 0.45f),
				},
				// 3. The grey wethers: sarsens lying in the open turf in loose flocks. Knee-high, so they block the feet
				// and never a sightline; the footing rule keeps every one 72 px from anything else that blocks.
				new FoliageSet
				{
					Grounded = new[] { "sarsen-1", "sarsen-2", "sarsen-3", "sarsen-3" }, Manifest = Manifest,
					Count = 150, ClumpSize = 4, Clump = ClumpBase.Tree, RadiusMin = 0.5f, RadiusMax = 0.8f,
					Loners = 0.25f, MinSpacing = 64f,
				},
				// 4. Heaps of picked flints, walked over.
				new FoliageSet
				{
					Grounded = new[] { "flint-heap-1", "flint-heap-2" }, Manifest = Manifest,
					Count = 110, ClumpSize = 2, Clump = ClumpBase.GroundAccent, Loners = 0.4f, MinSpacing = 40f,
				},
				// 5. Chalk breaking through the turf, walked over: thinner in the open, more toward the clumps' flanks.
				new FoliageSet
				{
					Grounded = new[] { "chalk-outcrop-1", "chalk-outcrop-2", "chalk-outcrop-3", "chalk-outcrop-3" }, Manifest = Manifest,
					Count = 420, ClumpSize = 2, Clump = ClumpBase.GroundAccent, RadiusMax = 1.2f, Separation = 1.1f,
					Loners = 0.3f, MinSpacing = 44f, Habitat = new Woodland.Habitat(0.25f, 1f, 0.6f),
				},
			},

			// The bottom rungs (detail_downs.py), after GroundDetail's own block. Harebells and thyme on the open down;
			// the grass longer toward the scrub; rabbit scrapes where the warrens are, at a clump's edge; flints under
			// the beeches. Densities are the open ground's, before the habitat weight (clearing, fringe, stand).
			Clutter = new[]
			{
				GroundDetail.Clutter("harebells", 0.03f, 5, 1f, 0.6f, 0.1f),
				GroundDetail.Clutter("downs_thyme", 0.025f, 4, 1f, 0.5f, 0.1f),
				GroundDetail.Clutter("downs_grass", 0.04f, 4, 0.2f, 1f, 0.6f),
				GroundDetail.Clutter("rabbit_scrape", 0.012f, 3, 0.2f, 1f, 0.4f),
				GroundDetail.Clutter("flints", 0.03f, 2, 0f, 0.3f, 1f),
			},
		};
	}
}
