using Godot;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// Plays the chapter's hints (StageHintCatalog; .ai/side-events.md, "Hints that something is out
// there"): partway through a run, while a side event of the chapter still holds a wizard or a spell
// this save does not own, a sound from its direction, marks in the site's own language coming in from
// that edge of the screen on the sound's beat (StageHintCue: notes for the singer, leaves for the
// grove), and its line fading in beside them (StageHintCaption).
//
// Created by SideEventDirector beside the events it is about, so it exists only on a chapter that has
// an event with a hint, and it reads everything it needs off those events: where each is, whether the
// player has found it, and the colour of its far tell.
//
// TWO EVENTS (P2, .ai/chapter-wizards.md 5 and 6.3). A chapter may hold two events, each with its own hint
// (StageHintCatalog, by site). They share this one director, one caption and one cue, and the schedule
// below: each slot plays ONE hint, the next event in the chapter's order after the one the last slot played
// for that is still worth hinting at, so two hidden events alternate and are never heard at once. Each
// event's lines run in their own order. An event found, marked or owned drops out and the other has every
// slot after; when none is left the hints retire for good. With one event this is exactly the old director.
//
// WHEN, AND WHEN NOT. Due on the pace clock (first a third of the way in, then every two and a half
// minutes, none from 13:30), and for each event only:
//   - while it is dormant - nobody has arrived, so its announcement and objective have not taken over;
//   - while none of its sites is marked - seen once on screen, or revealed by the wayfinder. A marked
//     site has a point-of-interest arrow at the screen edge (PoiPointer), which says where it is
//     better than a voice can, so the hints step aside for good. They do not duplicate the arrows;
//     they come BEFORE them, for the player who has not found the wayfinder or seen the light;
//   - while the save still lacks something the site gives (DiscoveryRewards.IsSiteExhausted). Once
//     Tituba is free, nobody calls from the cell again;
//   - never during the boss, and never once the run is over. A caption up when the boss arrives is
//     dismissed, and so is one whose event has just been found.
// None of it touches the save: it reads the unlock state and writes nothing, so no tool scene can
// mark anything found through it.
public partial class StageHintDirector : Node
{
	public Node2DGame Game { get; set; }

	/// <summary>The chapter, for the log line.</summary>
	public string StageId { get; set; } = string.Empty;

	/// <summary>The chapter's events that have a hint, in the chapter's order, each with its hint.</summary>
	public IReadOnlyList<(SideEvent Event, StageHint Hint)> Sources { get; set; } = new List<(SideEvent, StageHint)>();

	// How far toward the source the cue is played from the player. Inside SfxPlayer's 900 px
	// audible radius, so it is heard (about 3.5 dB down), and far enough to the side that the 2D
	// listener pans it toward where it came from.
	private const float SoundOffset = 300f;

	private StageHintCaption caption;
	private StageHintCue cue;
	// Slots played this run: the pace schedule's count.
	private int shown;
	// Hints played for each source: which of its own lines comes next.
	private int[] shownFor = System.Array.Empty<int>();
	// A source whose discovery the save owns, found at the moment of playing: out for the rest of the run.
	private bool[] owned = System.Array.Empty<bool>();
	// The source the last slot played for, and the one whose line and cue are up now (-1: none).
	private int last = -1;
	private int showing = -1;
	private bool retired;
	// Holding the screen's turn (Announcements) while its line and cue are up.
	private bool holdingTurn;
	private readonly List<string> played = new();

	/// <summary>How many hints this run has played.</summary>
	public int Shown => shown;

	/// <summary>The site each hint so far was about, in order. For _ExploreProbe's alternation check.</summary>
	public IReadOnlyList<string> PlayedSites => played;

	public override void _Ready()
	{
		shownFor = new int[Sources.Count];
		owned = new bool[Sources.Count];
		(caption, cue) = BuildPresentation(Game);
	}

	public override void _ExitTree()
	{
		Announcements.Release(this);
		if (caption != null && IsInstanceValid(caption))
			caption.QueueFree();
		if (cue != null && IsInstanceValid(cue))
			cue.QueueFree();
	}

	// The line goes on the UI overlay, the cue in the world (on the run's root, at the origin, beside
	// the event).
	private static (StageHintCaption, StageHintCue) BuildPresentation(Node2DGame game)
	{
		StageHintCaption line = null;
		var overlay = game?.GetNodeOrNull<CanvasLayer>("UIOverlay");
		if (overlay != null)
		{
			line = new StageHintCaption { Name = "StageHintCaption" };
			overlay.AddChild(line);
		}
		StageHintCue marks = null;
		if (game != null)
		{
			marks = new StageHintCue { Name = "StageHintCue" };
			game.AddChild(marks);
		}
		return (line, marks);
	}

	/// <summary>
	/// Plays a chapter's hint presentation - its sound, its cue and its line - toward
	/// <paramref name="source"/>, on whatever stage is running, and frees it after. For _UiShot
	/// --hint-preview, which photographs a hint's line on grounds no hint plays on (the Fallow Fields'
	/// pale field, the Sketchbook's paper). It touches no schedule and no save.
	/// </summary>
	public static void Preview(Node2DGame game, StageHint hint, Vector2 source, Color tell)
	{
		Player player = game?.RunPlayer;
		if (player == null || hint == null)
			return;
		var (line, marks) = BuildPresentation(game);
		line?.ShowLine(StageHintCatalog.LineFor(hint, 0), hint.Voice, player, source);
		marks?.Play(hint.Cue, player, source, tell);
		Vector2 toward = source - player.GlobalPosition;
		SfxPlayer.AtPosition(hint.Sound, player.GlobalPosition + (toward.LengthSquared() > 1f ? toward.Normalized() : Vector2.Up) * SoundOffset, 0f);
		game.GetTree().CreateTimer(9.0).Timeout += () =>
		{
			if (line != null && IsInstanceValid(line))
				line.QueueFree();
			if (marks != null && IsInstanceValid(marks))
				marks.QueueFree();
		};
	}

	public override void _Process(double delta)
	{
		bool up = (caption != null && caption.Showing) || (cue != null && cue.Showing);
		if (!up)
			showing = -1;
		if (holdingTurn && !up)
		{
			holdingTurn = false;
			Announcements.Release(this);
		}
		if (retired || Game == null || Sources.Count == 0)
			return;

		if (Game.RunFinished || Game.BossFightActive)
		{
			Retire();
			return;
		}
		// The place the line on screen is about has just been found: it goes, as it always did.
		if (showing >= 0 && !StillHidden(showing))
		{
			caption?.Dismiss();
			cue?.Dismiss();
			showing = -1;
		}
		if (!AnyHidden())
		{
			Retire();
			return;
		}

		float due = StageHintCatalog.DuePaceSeconds(shown);
		if (due > StageHintCatalog.LastPaceSeconds)
		{
			retired = true;
			return;
		}
		if (Game.PaceSeconds >= due)
			Play();
	}

	/// <summary>
	/// Plays the next hint now, whatever the pace clock says - for _UiShot --force-hint and _ExploreProbe.
	/// Every other rule still holds: no hint for a found place or an owned discovery, none while another's
	/// line holds the screen. Returns whether it played.
	/// </summary>
	public bool ForceHint()
	{
		if (retired || Game == null || Sources.Count == 0 || !AnyHidden())
			return false;
		return Play();
	}

	// An event still out there, unfound and unmarked. Read every frame: four sites at most an event.
	private bool StillHidden(int index)
	{
		SideEvent sideEvent = Sources[index].Event;
		if (sideEvent == null || !IsInstanceValid(sideEvent) || !sideEvent.IsDormant)
			return false;
		foreach (PointOfInterest site in sideEvent.Sites)
		{
			if (site.Marked)
				return false;
		}
		return true;
	}

	private bool AnyHidden()
	{
		for (int i = 0; i < Sources.Count; i++)
		{
			if (!owned[i] && StillHidden(i))
				return true;
		}
		return false;
	}

	private void Retire()
	{
		retired = true;
		if (!holdingTurn)
			Announcements.Release(this);
		caption?.Dismiss();
		cue?.Dismiss();
	}

	// The source this slot plays for: the first after the last one played, round the chapter's order, that is
	// still hidden and whose discovery the save still lacks - asked at the moment of playing rather than trusted
	// from placement: once a discovery is owned there is nothing out there to call for. -1 when none is.
	private int NextSource()
	{
		var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		for (int k = 1; k <= Sources.Count; k++)
		{
			int i = (last + k) % Sources.Count;
			if (owned[i] || !StillHidden(i))
				continue;
			SideEvent sideEvent = Sources[i].Event;
			if (sideEvent.PaysChest || (saveManager?.Data != null && DiscoveryRewards.IsSiteExhausted(saveManager.Data, Sources[i].Hint.SiteId)))
			{
				owned[i] = true;
				continue;
			}
			return i;
		}
		return -1;
	}

	private bool Play()
	{
		Player player = Game.RunPlayer;
		if (player == null || player.IsDead)
			return false;
		// Never two at once: while the last one's line or cue is up (this director still holds the screen's turn,
		// which Announcements would hand straight back to it) the next waits. The schedule's slots are 150 pace
		// seconds apart, so only a forced hint (ForceHint) ever met this - _ExploreProbe asks twice on one frame.
		if (holdingTurn)
			return false;

		int index = NextSource();
		if (index < 0)
		{
			retired = true;
			return false;
		}

		// Its turn (Announcements): a hint that falls due under an achievement, an event's line or a banner
		// waits until it has gone, and is played then. The pace clock is not moved, so the next one is not.
		if (!Announcements.Request(this, Announcements.Kind.Hint))
			return false;
		holdingTurn = true;

		var (sideEvent, hint) = Sources[index];
		Vector2 source = NearestSite(sideEvent, player.GlobalPosition);
		string line = StageHintCatalog.LineFor(hint, shownFor[index]);
		shownFor[index]++;
		shown++;
		last = index;
		showing = index;
		played.Add(hint.SiteId);

		// The marks come in at the edge now; the words fade in just inside them a moment later.
		cue?.Play(hint.Cue, player, source, sideEvent.FarTellColor);
		caption?.ShowLine(line, hint.Voice, player, source);
		Vector2 toward = source - player.GlobalPosition;
		Vector2 heard = player.GlobalPosition + (toward.LengthSquared() > 1f ? toward.Normalized() : Vector2.Up) * SoundOffset;
		SfxPlayer.AtPosition(hint.Sound, heard, 0f);
		GD.Print($"StageHint: {StageId} hint {shown} ({hint.SiteId}) at pace {Game.PaceSeconds:0}s, "
			+ $"{toward.Length():0} px away - \"{line}\"");
		return true;
	}

	// The nearest part of the event that is not done: the cell, the grove, the closest lantern.
	private static Vector2 NearestSite(SideEvent sideEvent, Vector2 from)
	{
		Vector2 best = Vector2.Zero;
		float bestDistance = float.MaxValue;
		foreach (PointOfInterest site in sideEvent.Sites)
		{
			if (site.Done)
				continue;
			float d = from.DistanceSquaredTo(site.Position);
			if (d < bestDistance)
			{
				bestDistance = d;
				best = site.Position;
			}
		}
		return bestDistance < float.MaxValue ? best : from + Vector2.Up;
	}
}
