using System.Collections.Generic;
using Godot;

namespace WizardSurvivors.scripts;

/// <summary>
/// Who may put a message on the screen now: one at a time, the rest waiting their turn (Eric,
/// 2026-11-01: "announcements should not stack on top of each other. i just saw the enchanted forest
/// introduction with the first blood achievement stacked on top of it").
/// </summary>
/// <remarks>
/// <para>Every centre or band message asks here before it shows and lets go when it has gone: the run's
/// opening (the tracked quests written in big, then the chapter's banner or its note), the achievement and
/// set plate (SetAssembledMoment), the run banners (a
/// boss's arrival, a Boss Rush fall, the way home, the Throne's lines), a side event's announcement and
/// its win line, the wayfinder's line, and the chapter hints. The onboarding tips wait while anything is
/// held. The waiting are served first come, first served, so an achievement earned in the opening plays
/// as soon as the opening is done - a few seconds late, never on top of it.</para>
///
/// <para><b>A banner never waits.</b> A boss's banner must show with the boss. So a <see cref="Kind.Banner"/>
/// takes the screen at once; whoever held it is <i>suspended</i> - it keeps its turn, hides, and stops its
/// clock (<see cref="IsSuspended"/>) - and carries on when the banner has gone.</para>
///
/// <para>Not here, on purpose: things in other places that belong with what is happening - the objective
/// line under the timer while an event runs, the tracked quests in their corner, the chapter note in the
/// edge pointers. Owners are any object; a freed Godot node is dropped from the queue on the next ask, so a
/// run that ends mid-message cannot leave the next one waiting.</para>
/// </remarks>
public static class Announcements
{
	public enum Kind
	{
		/// <summary>The run's first seconds: the quests written in big, then the chapter's banner or note.</summary>
		Opening,
		/// <summary>A run banner: a boss's arrival and the like. Never waits; suspends whoever holds.</summary>
		Banner,
		/// <summary>The chapter note where it hangs in the band (a phone).</summary>
		Note,
		/// <summary>A line under the timer: a side event's announcement or win, the wayfinder's line.</summary>
		Line,
		/// <summary>The achievement or set plate.</summary>
		Achievement,
		/// <summary>A chapter hint's line and cue.</summary>
		Hint,
	}

	private static object current;
	private static Kind currentKind;
	private static object suspended;
	private static Kind suspendedKind;
	private static readonly List<object> waiting = new();

	/// <summary>Forgets everyone. A run's start calls it, so nothing from the last run is still waiting.</summary>
	public static void Reset()
	{
		current = null;
		suspended = null;
		waiting.Clear();
	}

	/// <summary>
	/// Asks for the screen. True when <paramref name="owner"/> holds it now (it may show); false when it
	/// must wait - it is then in line, and should ask again next frame. Asking again is free.
	/// </summary>
	public static bool Request(object owner, Kind kind)
	{
		if (owner == null)
			return false;
		Purge();
		if (ReferenceEquals(current, owner) || ReferenceEquals(suspended, owner))
			return true;
		if (kind == Kind.Banner)
		{
			// A banner never waits. A banner over a banner simply takes over (the last one shown wins,
			// as ShowRunBanner always did); anything else is held over until the banner has gone.
			if (current != null && currentKind != Kind.Banner && suspended == null)
			{
				suspended = current;
				suspendedKind = currentKind;
			}
			current = owner;
			currentKind = kind;
			waiting.Remove(owner);
			return true;
		}
		if (current == null && suspended == null && (waiting.Count == 0 || ReferenceEquals(waiting[0], owner)))
		{
			waiting.Remove(owner);
			current = owner;
			currentKind = kind;
			return true;
		}
		if (!waiting.Contains(owner))
			waiting.Add(owner);
		return false;
	}

	/// <summary>Lets go: the next in line may show. Also takes an owner out of the line.</summary>
	public static void Release(object owner)
	{
		if (owner == null)
			return;
		waiting.Remove(owner);
		if (ReferenceEquals(suspended, owner))
			suspended = null;
		if (ReferenceEquals(current, owner))
		{
			current = suspended;
			currentKind = suspendedKind;
			suspended = null;
		}
	}

	/// <summary>Hands the screen from one owner to the next without letting the line in between: the opening to its note.</summary>
	public static void Pass(object from, object to, Kind kind)
	{
		if (ReferenceEquals(current, from) && to != null)
		{
			current = to;
			currentKind = kind;
		}
		else
		{
			Release(from);
		}
	}

	/// <summary>True when a banner has cut in over <paramref name="owner"/>: it keeps its turn, but hides and waits.</summary>
	public static bool IsSuspended(object owner) => owner != null && ReferenceEquals(suspended, owner);

	/// <summary>True when <paramref name="owner"/> has the screen now.</summary>
	public static bool Holds(object owner) => owner != null && ReferenceEquals(current, owner);

	/// <summary>True when someone is waiting for the screen: a long message with nothing left to say may make way.</summary>
	public static bool AnyWaiting
	{
		get
		{
			Purge();
			return waiting.Count > 0;
		}
	}

	/// <summary>True while anyone holds the screen - the onboarding tips wait for it.</summary>
	public static bool AnyHeld
	{
		get
		{
			Purge();
			return current != null;
		}
	}

	// A freed node never releases; drop it, so the line moves on.
	private static void Purge()
	{
		if (current is GodotObject c && !GodotObject.IsInstanceValid(c))
		{
			current = suspended;
			currentKind = suspendedKind;
			suspended = null;
		}
		if (suspended is GodotObject s && !GodotObject.IsInstanceValid(s))
			suspended = null;
		for (int i = waiting.Count - 1; i >= 0; i--)
		{
			if (waiting[i] is GodotObject w && !GodotObject.IsInstanceValid(w))
				waiting.RemoveAt(i);
		}
	}
}
