using System;
using System.Collections.Generic;
using Godot;
using WizardSurvivors.scripts;

// THE LESSONS (.ai/slow-start.md, "The lessons"). Eric, 2026-11-02: "new systems should be unlocked in a
// post-run cutscene, after fulfilling an achievement, like finding a landmark. The cutscene can then explain
// the new system." A run whose end finds a lesson owed (Teachings.Owed: a teacher found, or the Sovereign Rite
// newly owned) plays that lesson's reel over the paused run once the run has ended - after the defeat's own
// sequence, before the results - and the save learns the system when the reel ends, played through or
// skipped. One lesson in all but a contrived save, since each chapter holds one teacher; several play in
// ladder order.
//
// The run itself was played as it began (Player reads the flags once, at the start), and the results list the
// lesson beside what else the run paid (RunUnlockKind.Lesson). A run that is quit owes the lesson to the end
// of the next one. Headless runs - the harness, the probes - teach without the reel, so no tool waits ten
// seconds on a picture nobody sees.
public partial class Node2DGame
{
	// The canvas layer the reel plays on: over the run, the defeat card and the results' own layer (10).
	private const int LessonLayer = 60;

	// Gathered by FinishRunAndReward, given by ShowGameOver before the results.
	private readonly List<Teachings.Lesson> lessonsOwed = new();

	/// <summary>The lessons this run's end owes, for the results and the reel. Call once the deeds are paid.</summary>
	private void GatherOwedLessons(SaveData data, RunUnlockSummary summary)
	{
		lessonsOwed.Clear();
		lessonsOwed.AddRange(Teachings.Owed(data));
		foreach (Teachings.Lesson lesson in lessonsOwed)
		{
			summary?.Entries.Add(new RunUnlockEntry
			{
				Kind = RunUnlockKind.Lesson,
				Id = lesson.Id,
				Name = lesson.Name,
				Source = lesson.Line,
			});
		}
	}

	/// <summary>
	/// Plays every owed lesson, one after another, then <paramref name="then"/>. True when one is playing - the
	/// caller waits for <paramref name="then"/>; false when nothing is owed.
	/// </summary>
	private bool PlayOwedLessonsThen(Action then)
	{
		if (lessonsOwed.Count == 0)
			return false;
		Teachings.Lesson lesson = lessonsOwed[0];
		lessonsOwed.RemoveAt(0);
		// Then the next lesson, if any: the caller is ShowGameOver, which asks again before it shows.
		void Learned()
		{
			TeachAndSave(lesson);
			then();
		}
		if (DisplayServer.GetName() == "headless")
		{
			GD.Print($"Node2DGame: lesson '{lesson.Id}' taught without its reel (headless)");
			Learned();
			return true;
		}
		var layer = new CanvasLayer { Name = "LessonLayer", Layer = LessonLayer, ProcessMode = ProcessModeEnum.Always };
		AddChild(layer);
		Prologue reel = Prologue.CreateEmbedded(lesson.Id, () =>
		{
			layer.QueueFree();
			Learned();
		});
		reel.ProcessMode = ProcessModeEnum.Always;
		layer.AddChild(reel);
		GD.Print($"Node2DGame: lesson '{lesson.Id}' playing before the results");
		return true;
	}

	// The save learns it now. A --no-save-writes run or a tool scene learns it in memory only (SaveManager).
	private void TeachAndSave(Teachings.Lesson lesson)
	{
		var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager?.Data == null || !Teachings.Teach(saveManager.Data, lesson))
			return;
		saveManager.SaveGame();
		GD.Print($"Node2DGame: the save has learned {lesson.Name}");
	}

	// --- in the run ----------------------------------------------------------------------------------

	/// <summary>
	/// The line a teacher's toast adds when the find makes a lesson owed (SecretDirector.Found): one short
	/// line on the same plate, through the same queue, so it is never on screen beside another message.
	/// Empty when the find owed nothing new.
	/// </summary>
	public static string OwedLessonLine(IReadOnlyCollection<Teachings.Lesson> before, IReadOnlyCollection<Teachings.Lesson> after)
	{
		foreach (Teachings.Lesson lesson in after)
		{
			bool owedBefore = false;
			foreach (Teachings.Lesson had in before)
				owedBefore |= had.Id == lesson.Id;
			if (!owedBefore)
				return $"Lesson - {lesson.Name}, when this run ends";
		}
		return string.Empty;
	}
}
