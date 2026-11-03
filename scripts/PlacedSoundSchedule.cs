using System;
using Godot;

namespace TheOneHeMissed.scripts;

/// <summary>
/// Sounds placed in the world and played after a delay - the primitive .ai/chapter-wizards.md section 7 asks
/// for: "per-mouth delayed audio". First used by the Sunken Cave's nine mouths (NineMouthsEvent), where one
/// voice comes out of nine places a beat apart, so the order the player hears them in IS the puzzle.
/// </summary>
/// <remarks>
/// <para>A fixed queue, no allocation: <see cref="Schedule"/> takes a slot, <see cref="Tick"/> plays every
/// sound that has come due through <see cref="SfxPlayer.AtPosition"/> - a positional voice, so the 2D
/// listener pans each toward where it comes from and fades it with distance - and then hands the slot's tag
/// to the owner's <c>onPlayed</c>, once, on the same frame. That hook is what keeps what is SEEN in step with
/// what is heard (the nine mouths' leaves and breath come out on the voice's own frame), and is what makes
/// the sound fair on a phone speaker, where panning is lost: the picture says the same thing at the same
/// moment.</para>
/// <para>Game time: the owner ticks it from its own process, so a pause holds the whole call where it is
/// and a level-up does not let half a call play into the menu.</para>
/// </remarks>
public sealed class PlacedSoundSchedule
{
	private struct Entry
	{
		public bool Live;
		public string Sound;
		public Vector2 At;
		public float Due;
		public int Tag;
		public float PitchJitter;
	}

	private readonly Entry[] entries;
	private readonly Action<int> onPlayed;
	private float clock;

	/// <param name="capacity">The most sounds waiting at once; a schedule past it drops the newest.</param>
	/// <param name="onPlayed">Called with an entry's tag on the frame it plays. Null for none.</param>
	public PlacedSoundSchedule(int capacity, Action<int> onPlayed = null)
	{
		entries = new Entry[Math.Max(1, capacity)];
		this.onPlayed = onPlayed;
	}

	/// <summary>Sounds waiting to play.</summary>
	public int Pending
	{
		get
		{
			int n = 0;
			foreach (Entry e in entries)
				if (e.Live) n++;
			return n;
		}
	}

	/// <summary>
	/// Plays <paramref name="sound"/> (an SfxCatalog name) at <paramref name="at"/>, <paramref name="delay"/>
	/// seconds from now. False if the queue is full.
	/// </summary>
	public bool Schedule(string sound, Vector2 at, float delay, int tag = 0, float pitchJitter = 0.02f)
	{
		for (int i = 0; i < entries.Length; i++)
		{
			if (entries[i].Live)
				continue;
			entries[i] = new Entry { Live = true, Sound = sound, At = at, Due = clock + Math.Max(0f, delay), Tag = tag, PitchJitter = pitchJitter };
			return true;
		}
		return false;
	}

	/// <summary>Every sound not yet played is dropped: the call was interrupted.</summary>
	public void Clear()
	{
		for (int i = 0; i < entries.Length; i++)
			entries[i].Live = false;
	}

	/// <summary>Advances the clock and plays what is due, earliest first - so two due on one frame keep their order.</summary>
	public void Tick(float delta)
	{
		clock += delta;
		while (true)
		{
			int next = -1;
			for (int i = 0; i < entries.Length; i++)
			{
				if (entries[i].Live && entries[i].Due <= clock && (next < 0 || entries[i].Due < entries[next].Due))
					next = i;
			}
			if (next < 0)
				return;
			Entry e = entries[next];
			entries[next].Live = false;
			if (!string.IsNullOrEmpty(e.Sound))
				SfxPlayer.AtPosition(e.Sound, e.At, e.PitchJitter);
			onPlayed?.Invoke(e.Tag);
		}
	}
}
