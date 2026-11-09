namespace TheOneHeMissed.scripts;

/// <summary>
/// Reel E, "The light goes on" (.ai/second-half.md 6.3; .ai/cutscenes.md, E): the cutscene that announces the far
/// shore. When the dark wizard falls, the light he took goes home - most of it down onto the land, to the wizards -
/// and some of it goes on, east over the dark sea, because it was never a wizard's. Two panels of the prologue's
/// player (Prologue.Reels, "light_goes_on"; the art is tools/art/light_goes_on.py): the Archmage on the steps of his
/// sea-gate as the light streams out over him, then the light going on over the water toward a line of white cliffs,
/// past the edge of the picture, with the one line "Some of it was older than any of them."
/// </summary>
/// <remarks>
/// <para><b>When.</b> Owed to a save that has taken back the Throne and has not seen it (<see cref="IsOwed"/>). It
/// plays after the ending's row and credits on the run that wins the Throne - after the freeing of the one he kept,
/// the story's last beat on the road, and before any lesson (Node2DGame.Lessons.cs, PlayOwedReelsThen) - and, for a
/// save that won the Throne before this reel existed, the first time the world map opens (WorldMap), which then comes
/// back with the far shore's first stop drawn out of the dark. Replayable from Options > Story once seen.</para>
///
/// <para><b>The save.</b> <see cref="SaveData.HasSeenLightGoesOn"/>, a flag that defaults to unseen and is owed only
/// with Taken Back, so no save that has not won the Throne ever owes it and no schema step is needed (the doc's own
/// words, 6.3 and 6.4). Marked when the reel ends, played through or skipped - in memory always, so a tool run's map
/// does not play it twice, and on disk only when writes are on.</para>
/// </remarks>
public static class LightGoesOn
{
	/// <summary>Its reel's id in Prologue.Reels.</summary>
	public const string ReelId = "light_goes_on";

	/// <summary>The Options > Story label for watching it again.</summary>
	public const string ReplayLabel = "The Light Goes On";

	/// <summary>The Throne is taken back and the reel has not been shown.</summary>
	public static bool IsOwed(SaveData data) =>
		data != null && !data.HasSeenLightGoesOn && GlobalStatsManager.HasTakenBackTheThrone(data);

	/// <summary>Options lists it once it has been shown: its line and its picture are the far shore's announcement.</summary>
	public static bool IsWatchable(SaveData data) =>
		data != null && data.HasSeenLightGoesOn && GlobalStatsManager.HasTakenBackTheThrone(data);

	/// <summary>The reel has ended (played or skipped). True when this changed the save; the caller saves.</summary>
	public static bool MarkSeen(SaveData data)
	{
		if (data == null || data.HasSeenLightGoesOn)
			return false;
		data.HasSeenLightGoesOn = true;
		return true;
	}
}
