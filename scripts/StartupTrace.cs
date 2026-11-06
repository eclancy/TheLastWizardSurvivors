using Godot;
using System;

// Opt-in wall-clock timings, including resource loads and synchronous _Ready work.
public sealed class StartupTrace
{
	public static bool Enabled { get; } = Array.IndexOf(OS.GetCmdlineUserArgs(), "--profile-startup") >= 0;
	private readonly string area;
	private ulong previous;

	public StartupTrace(string area)
	{
		this.area = area;
		previous = Time.GetTicksUsec();
	}

	public void Mark(string step)
	{
		if (!Enabled) return;
		ulong now = Time.GetTicksUsec();
		GD.Print($"STARTUP {area}: {step} {(now - previous) / 1000.0:0.0} ms");
		previous = now;
	}
}
