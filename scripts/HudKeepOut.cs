using Godot;
using System.Collections.Generic;

namespace WizardSurvivors.scripts;

/// <summary>
/// Screen rectangles the edge pointers (WorldPointer, ChestPointer, SideEventPointer, PoiPointer) slide
/// out of: the tracked quests in their corner (QuestTracker) and the chapter note while it is up
/// (ChapterNoteCard). Canvas units, the same space a pointer measures the visible rect in.
/// </summary>
/// <remarks>
/// Every pointer rides a ring a fixed inset in from the screen's edge, so anything kept in a corner
/// sits on that ring - the boss's badge, toward the lower left, landed on the quests. Rather than push
/// the ring in for every pointer everywhere, a badge that would land in one of these rectangles is
/// moved along the side of the ring it is on to the near end of the rectangle: it still points the
/// same way, from the same side of the screen. Owners register while visible and clear on leaving.
/// </remarks>
public static class HudKeepOut
{
	private static readonly Dictionary<ulong, Rect2> Zones = new();

	public static void Set(GodotObject owner, Rect2 rect)
	{
		if (owner != null)
			Zones[owner.GetInstanceId()] = rect;
	}

	public static void Clear(GodotObject owner)
	{
		if (owner != null)
			Zones.Remove(owner.GetInstanceId());
	}

	/// <summary>
	/// <paramref name="at"/>, a badge's centre on the edge of <paramref name="ring"/>, moved out of every
	/// keep-out zone grown by <paramref name="radius"/> (the badge's half-size), along the side it is on.
	/// </summary>
	public static Vector2 Avoid(Vector2 at, Rect2 ring, float radius)
	{
		if (Zones.Count == 0)
			return at;
		foreach (Rect2 zone in Zones.Values)
		{
			Rect2 grown = zone.Grow(radius);
			if (!grown.HasPoint(at))
				continue;
			bool onSide = Mathf.Abs(at.X - ring.Position.X) < 1f || Mathf.Abs(at.X - ring.End.X) < 1f;
			Vector2 centre = ring.GetCenter();
			if (onSide)
				at.Y = grown.GetCenter().Y > centre.Y ? grown.Position.Y : grown.End.Y;
			else
				at.X = grown.GetCenter().X > centre.X ? grown.Position.X : grown.End.X;
		}
		return at;
	}
}
