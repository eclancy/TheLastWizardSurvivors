using Godot;
using System;
using System.Collections.Generic;

/// <summary>Bounded, startup-only error capture for reports that can be copied on a phone.</summary>
public partial class StartupDiagnostics : Logger
{
	private readonly object gate = new();
	private readonly Queue<string> errors = new();
	private const int MaxErrors = 12;
	private const int MaxErrorLength = 2400;

	private void Record(string message)
	{
		if (string.IsNullOrWhiteSpace(message)) return;
		if (message.Length > MaxErrorLength) message = message.Substring(0, MaxErrorLength) + "...";
		// Resource loading can log from worker threads. Never call Godot or print from this lock.
		lock (gate)
		{
			if (errors.Count == MaxErrors) errors.Dequeue();
			errors.Enqueue(message);
		}
	}

	public override void _LogError(string function, string file, int line, string code,
		string rationale, bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
		=> Record($"{(string.IsNullOrEmpty(rationale) ? code : rationale)}\n  {file}:{line} ({function})");

	public override void _LogMessage(string message, bool error)
	{
		if (error) Record(message);
	}

	public void Clear()
	{
		lock (gate) errors.Clear();
	}

	public string Snapshot()
	{
		lock (gate) return string.Join("\n\n", errors);
	}
}
