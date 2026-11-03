using Godot;
using TheOneHeMissed.scripts;

/// <summary>
/// A message that waits its turn (Announcements) and then shows for a while: built and shown by
/// <c>show</c> when the screen is free, held for <c>seconds</c>, then let go. For a line whose owner is
/// gone before it is - the wayfinder frees itself the moment it is taken.
/// </summary>
public partial class AnnouncementWaiter : Node
{
	public Announcements.Kind Kind { get; init; } = Announcements.Kind.Line;
	public float Seconds { get; init; } = 4f;
	public System.Action Show { get; init; }
	private bool shown;
	private float clock;

	/// <summary>Queues <paramref name="show"/> under <paramref name="host"/>; it runs when the screen is free.</summary>
	public static void Queue(Node host, Announcements.Kind kind, float seconds, System.Action show)
	{
		if (host == null || show == null)
			return;
		host.AddChild(new AnnouncementWaiter { Name = "AnnouncementWaiter", Kind = kind, Seconds = seconds, Show = show });
	}

	public override void _Ready() => ProcessMode = ProcessModeEnum.Pausable;

	public override void _Process(double delta)
	{
		if (!shown)
		{
			if (!Announcements.Request(this, Kind))
				return;
			shown = true;
			Show();
		}
		clock += (float)delta;
		if (clock >= Seconds)
		{
			Announcements.Release(this);
			QueueFree();
		}
	}

	public override void _ExitTree() => Announcements.Release(this);
}
