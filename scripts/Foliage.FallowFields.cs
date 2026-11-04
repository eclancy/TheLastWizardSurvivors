namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Fallow Fields (stage_9): the first chapter on the shared groundwork after the Forest's pilot
	/// (2026-11-03, .ai/terrain.md section 13, "The Fallow Fields"). Its stands are not woods: they are
	/// HEDGEROWS, the black hedges between fields gone to stubble (.ai/campaign.md section 5), so the land
	/// reads as fields you fight in, framed by hedges you can see across.
	/// </summary>
	/// <remarks>
	/// THE LADDER, largest first. Hedgerow oaks and a hawthorn grown into a tree, standing in the hedge
	/// lines (the only trunks: the rule that no stand pins the player keeps them apart); the hedge itself,
	/// long low runs of dark leaf you wade through; hawthorn and blackthorn bushes and briars on its fringe,
	/// the briars straying into the stubble; and in the detail layer, grass coming back through the
	/// stubble, docks and nettles on the banks, and clods the plough left. Stooks stand out in the open
	/// field, where the harvest was. All of it in the dark half of verdant (tools/art/props_fields.py,
	/// detail_fields.py): the old Fields borrowed the Forest's lime bushes, the brightest thing on its floor.
	///
	/// WHAT STAYS AS IT WAS. The fences and the rocks keep their places in the list (3 and 4), so they
	/// are where they were; the ground keeps its palette; every older detail rule keeps its index. The side
	/// events (the Well, the Tithe-Reeve's scale) and the Pinfold keep their footprints clear, as every
	/// decor set and clutter rule does (Node2DGame.IsInExploreReserve), and nothing here draws a lone
	/// upright sheaf, a straight furrow, an empty cross or a drift of chaff: those are the tells of the
	/// Fields' planned secrets (.ai/achievements-and-secrets.md 8.2).
	/// </remarks>
	private static class FallowFields
	{
		private const string Manifest = "res://assets/bonelight/world/fields/fields.json";

		internal static ChapterFoliage Build() => new()
		{
			Kind = StageEnvironmentKind.Fields,

			// A field a little over a PC screen across (30 cells, 1440 px), its hedge 3.5 cells to the open either
			// side of the line, broken over about a third of its length where a hedge has gone to gaps. Measured
			// on the field (Woodland.Validate's 70 x 70 sample, three seeds): 13-14% of the ground is hedge (the
			// bank under it), 75-76% open field, the rest the hedges' fringe. Tried first at 26 cells, 4 wide and
			// a fifth gap: a fifth of the ground was hedge and nearly every field was shut in by one.
			Canopy = new CanopyProfile
			{
				Shape = CanopyShape.Hedgerows,
				FieldCells = 30,
				FieldJitter = 0.75f,
				HedgeHalfWidth = 3.5f,
				EdgeLattice = 5,
				EdgeWeight = 0.8f,
				GapLattice = 7,
				GapShare = 0.32f,
				ClearingEnd0 = 0.1f,
				ClearingEnd1 = 0.25f,
				StandStart0 = 0.55f,
				StandStart1 = 0.7f,
				StandFloor = 0.62f,
				// The grassy bank a hedgerow grows on, under every hedge; a bank blob out in the open field is
				// ploughed instead, and a ploughed blob under a hedge is bank.
				StandFloorTerrain = "hedge_bank",
				ClearingFloorTerrain = "furrow",
				SaltStand = 0x58B2,
				SaltEdge = 0x58B3,
				SaltGap = 0x58B4,
				StandShareMin = 0.08f,
				StandShareMax = 0.2f,
				ClearingShareMin = 0.65f,
				ClearingShareMax = 0.88f,
			},

			Decor = new[]
			{
				// 0. The hedge: long runs of leaf along the line, overlapping, so a hedge reads as one. Walked
				// through, like every bush.
				new FoliageSet
				{
					Grounded = new[] { "hedge-1", "hedge-2", "hedge-3" }, Manifest = Manifest,
					Count = 11000, ClumpSize = 6, Clump = ClumpBase.Bush, RadiusMin = 0.25f, RadiusMax = 0.4f, Separation = 0.45f,
					MinSpacing = 50f, Habitat = new Woodland.Habitat(0f, 0.3f, 1f),
				},
				// 1. The hedgerow trees: oaks and a hawthorn in the hedge lines, now and then one alone in a field.
				new FoliageSet
				{
					Grounded = new[] { "hedge-oak-1", "hedge-oak-2", "field-thorn-1" }, Manifest = Manifest,
					Count = 1100, ClumpSize = 2, Clump = ClumpBase.Tree, RadiusMin = 0.3f, RadiusMax = 0.45f, Separation = 0.6f,
					Loners = 0.12f, MinSpacing = MatureTreeSpacing, Habitat = new Woodland.Habitat(0.03f, 0.4f, 1f),
				},
				// 2. Stooks of sheaves, out in the open field where the harvest was.
				new FoliageSet
				{
					Grounded = new[] { "stook-1", "stook-2" }, Manifest = Manifest,
					Count = 150, ClumpSize = 5, Clump = ClumpBase.Bush, RadiusMax = 1.2f, Separation = 1.4f,
					Loners = 0.1f, MinSpacing = 56f, Habitat = new Woodland.Habitat(1f, 0.25f, 0f),
				},
				// 3. Runs of broken fence, as before.
				new FoliageSet
				{
					Grounded = new[] { "fence-1", "fence-2" }, Manifest = Manifest,
					Count = 40, ClumpSize = 2, Clump = ClumpBase.Bush, Separation = 1.6f, Loners = 0.3f, MinSpacing = 96f,
				},
				// 4. The odd stone, as before.
				new FoliageSet
				{
					Sprites = new[] { "res://assets/bonelight/world/rock-1.png", "res://assets/bonelight/world/rock-2.png" },
					MinScale = 0.84f, MaxScale = 0.98f,
					Count = 50, ClumpSize = 2, Clump = ClumpBase.GroundAccent, Loners = 0.05f,
				},
				// 5. Hawthorn and blackthorn on a hedge's fringe.
				new FoliageSet
				{
					Grounded = new[] { "thorn-bush-1", "thorn-bush-2" }, Manifest = Manifest,
					Count = 1800, ClumpSize = 3, Clump = ClumpBase.Bush, RadiusMin = 0.4f, RadiusMax = 0.6f, Separation = 0.6f,
					Loners = 0.04f, MinSpacing = 40f, Habitat = new Woodland.Habitat(0.03f, 1f, 0.4f),
				},
				// 6. Briars, on the fringe and straying out into the stubble.
				new FoliageSet
				{
					Grounded = new[] { "briar-1", "briar-2" }, Manifest = Manifest,
					Count = 1100, ClumpSize = 3, Clump = ClumpBase.Bush, RadiusMin = 0.4f, RadiusMax = 0.6f, Separation = 0.6f,
					Loners = 0.15f, MinSpacing = 30f, Habitat = new Woodland.Habitat(0.2f, 1f, 0.1f),
				},
			},

			// The bottom rungs (tools/art/detail_fields.py): grass returning through the stubble, thickest at
			// a hedge's fringe; docks and nettles on the fringe and the bank; clods in the open and on the
			// ploughed patches; a few more of the old poppies where a hedge's edge is. Not more of the old
			// stubble tufts or thistles: their gold ears and purple heads stand +43 and +48 L* over the stubble
			// at their brightest (detail_contrast.py), above the cast's rims - more of them would break the
			// guardrail. Densities are the open ground's, before the habitat weight (clearing, fringe, stand).
			Clutter = new[]
			{
				GroundDetail.Clutter("fallow_grass", 0.07f, 5, 0.8f, 1f, 0.6f),
				GroundDetail.Clutter("clods", 0.035f, 4, 1f, 0.3f, 0f),
				GroundDetail.Clutter("weeds", 0.05f, 4, 0.1f, 1f, 0.5f),
				GroundDetail.Clutter("poppy", 0.01f, 5, 0.1f, 1f, 0.3f),
				GroundDetail.Clutter("weeds", 0.04f, 4, 1f, 1f, 1f, "hedge_bank"),
				GroundDetail.Clutter("fallow_grass", 0.04f, 5, 1f, 1f, 1f, "hedge_bank"),
				GroundDetail.Clutter("clods", 0.06f, 3, 1f, 1f, 1f, "furrow"),
			},
		};
	}
}
