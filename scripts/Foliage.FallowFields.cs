namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Fallow Fields (stage_9, StageEnvironmentKind.Fields): no foliage pass yet. A STAND-IN, so the
	/// chapter's builder creates nothing shared - this file is the chapter's to fill.
	/// </summary>
	private static class FallowFields
	{
		internal static ChapterFoliage? Build() => null;
	}
}
