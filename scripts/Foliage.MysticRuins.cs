namespace TheOneHeMissed.scripts;

public static partial class Foliage
{
	/// <summary>
	/// The Mystic Ruins (stage_4, StageEnvironmentKind.Ruins): no foliage pass yet. A STAND-IN, so the chapter's
	/// builder creates nothing shared - this file is the chapter's to fill, and Foliage.All already names it.
	/// </summary>
	/// <remarks>
	/// While Build returns null the chapter draws exactly as before the pass: its palette, its decor case in
	/// Node2DGame.BuildDecorProps, its rules in GroundDetail. To build it, follow .ai/terrain.md section 13,
	/// "Adding a chapter", and return a ChapterFoliage with Kind = StageEnvironmentKind.Ruins.
	/// </remarks>
	private static class MysticRuins
	{
		internal static ChapterFoliage? Build() => null;
	}
}
