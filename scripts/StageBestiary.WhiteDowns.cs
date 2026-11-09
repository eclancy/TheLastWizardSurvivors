namespace TheOneHeMissed.scripts;

public static partial class StageBestiary
{
	/// <summary>
	/// Far Shore 1, the White Downs (stage_14): the hunt that runs on the downs at night, and the people of
	/// the chalk. Boss: the Unstrung Hunter.
	/// </summary>
	/// <remarks>
	/// "No moon has risen here since he came." Section 4.2 of .ai/second-half.md: "shepherds and warreners
	/// (linen); the hunt's beaters and its hounds; the cliff-watch (steel)". His shadow wears what it took on
	/// Artemis's downs, and what it took was a hunt - the beaters walking their line, the coursing hounds, the
	/// huntsman with his horn, the saddled horse that lost its rider - with the people who kept the chalk
	/// (the shepherd, the slinger, the warrener), the flock's tup, the puffballs of the sheep-cropped turf and
	/// the cliff-watch that kept the white cliffs facing the dark sea. The watch's beacon is the one thing on
	/// the downs he built, so it burns (world-and-tone's carve-out).
	///
	/// NOTHING ECHOES THE BOSS. The Unstrung Hunter is a tall figure behind a great bow, his belt three cold
	/// stars: so no bow anywhere in the cast. The slinger throws, the huntsman calls, the beater beats.
	///
	/// STAND-INS ARE THE EMBERDEEP'S, as the castle's were: the far shore is held at the castle's difficulty
	/// (.ai/second-half.md section 8, decision 11), and every drawn creature keeps its stand-in's stat block.
	///
	/// THE TABLE leans to the hunt: more hounds and beaters than the Emberdeep's, the tup a little rarer than its
	/// bulwark. 0.36 + 0.28 x 0.75 + 0.24 x 2.2 + 0.12 x 1.4 = 1.266, the Emberdeep's 1.284 within a step, so the
	/// row in ChapterDifficultyCatalog means what it meant there.
	/// </remarks>
	private static class WhiteDowns
	{
		private const string Stage = "stage_14";

		// The hunt's beaters, walking their line across the downs with sticks to drive the quarry on.
		internal static readonly BestiaryCreature Beater =
			Creature(Stage, "downs_beater", "Beater", "res://scenes/DownsBeater.tscn", "DownsBeater");
		// The hunt's coursing hounds: lean, long-legged, fast. The huntsman's minion.
		internal static readonly BestiaryCreature Courser =
			Creature(Stage, "downs_courser", "Coursing Hound", "res://scenes/DownsCourser.tscn", "DownsCourser");
		// The flock's ram, heavy-horned, the tank.
		internal static readonly BestiaryCreature Tup =
			Creature(Stage, "downs_tup", "Downland Tup", "res://scenes/DownsTup.tscn", "DownsTup");
		// The shepherd of the night fold, in a heavy cloak, leaning on his crook: the slow wall.
		internal static readonly BestiaryCreature Shepherd =
			Creature(Stage, "downs_shepherd", "Night Shepherd", "res://scenes/DownsShepherd.tscn", "DownsShepherd");
		// A shepherd's sling and the downs' own flints: the back line.
		internal static readonly BestiaryCreature Slinger =
			Creature(Stage, "downs_slinger", "Flint-Slinger", "res://scenes/DownsSlinger.tscn", "DownsSlinger");
		// The cliff-watch's beacon, an iron fire-basket on a post: built, so it burns.
		internal static readonly BestiaryCreature Beacon =
			Creature(Stage, "downs_beacon", "Cliff Beacon", "res://scenes/DownsBeacon.tscn", "DownsBeacon");
		// The hunt's horse that lost its rider, saddled and running: the charger.
		internal static readonly BestiaryCreature HuntHorse =
			Creature(Stage, "downs_hunt_horse", "Riderless Horse", "res://scenes/DownsHuntHorse.tscn", "DownsHuntHorse");
		// The giant puffball of the sheep-cropped turf, swollen: it bursts.
		internal static readonly BestiaryCreature Puffball =
			Creature(Stage, "downs_puffball", "Giant Puffball", "res://scenes/DownsPuffball.tscn", "DownsPuffball");
		// The warrener with his long warren-spade, brought down on the turf: the slammer.
		internal static readonly BestiaryCreature Warrener =
			Creature(Stage, "downs_warrener", "The Warrener", "res://scenes/DownsWarrener.tscn", "DownsWarrener");
		// The huntsman with his horn, who calls the hounds on: the summoner; its minion is the Coursing Hound.
		internal static readonly BestiaryCreature Huntsman =
			Creature(Stage, "downs_huntsman", "Huntsman", "res://scenes/DownsHuntsman.tscn", "DownsHuntsman");
		// The miniboss, on the 48 cell: the captain of the cliff-watch, his jailer on the downs.
		internal static readonly BestiaryCreature WatchCaptain =
			Creature(Stage, "downs_watch_captain", "Cliff-Watch Captain", "res://scenes/DownsWatchCaptain.tscn", "DownsWatchCaptain");

		internal static ChapterBestiary Build()
		{
			BestiarySlot bulk = Planned(BestiaryRole.Bulk, Beater, Emberdeep.SlagThrall);
			BestiarySlot fast = Planned(BestiaryRole.Fast, Courser, Emberdeep.Chainhound);
			BestiarySlot heavy = Planned(BestiaryRole.Heavy, Tup, Emberdeep.Bulwark);
			BestiarySlot wall = Planned(BestiaryRole.Wall, Shepherd, Emberdeep.KilnBrute);

			return new ChapterBestiary
			{
				StageId = Stage,
				Slots = new[]
				{
					bulk, fast, heavy, wall,
					Planned(BestiaryRole.Ranged, Slinger, Emberdeep.Kindler),
					Planned(BestiaryRole.Turret, Beacon, Emberdeep.FlueMask),
					Planned(BestiaryRole.Charger, HuntHorse, Emberdeep.Ironjaw),
					Planned(BestiaryRole.Exploder, Puffball, Emberdeep.Bellows),
					Planned(BestiaryRole.Slammer, Warrener, Emberdeep.TripHammer),
					Planned(BestiaryRole.Summoner, Huntsman, Emberdeep.Kennelmaster),
					Planned(BestiaryRole.MiniBoss, WatchCaptain, Emberdeep.Chainwright),
				},
				// 0.36 + 0.28 x 0.75 + 0.24 x 2.2 + 0.12 x 1.4 = 1.266.
				Table = new[]
				{
					Band(bulk, 0.36f, 1.0f),
					Band(fast, 0.28f, 0.75f),
					Band(heavy, 0.24f, 2.2f),
					Band(wall, 0.12f, 1.4f),
				},
			};
		}
	}
}
