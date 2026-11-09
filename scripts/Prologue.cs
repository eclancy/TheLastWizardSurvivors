using Godot;
using System;
using System.Text.Json;
using TheOneHeMissed.scripts;

/// <summary>
/// The story-mode prologue: seven generated panels (tools/art/prologue.py), each held a few seconds
/// with its layers drifting, six short lines between them, then on to wherever story mode was
/// going. See .ai/prologue.md.
/// </summary>
/// <remarks>
/// <para><b>When it plays.</b> The first time the player presses Start Run (MainMenu), before the
/// character select, and whenever they ask for it from Options. "Seen" is
/// <see cref="SaveData.HasSeenPrologue"/>, written when it ends - finished or skipped - and never by a
/// launch whose writes are off (every <c>_</c> tool scene, every <c>--no-save-writes</c> run).</para>
///
/// <para><b>The art is never scaled to fit.</b> Every layer is 960x840 drawn at a whole x2 into a
/// 1920x1680 rect centred on the canvas, the title screen's move in both directions at once: a PC
/// canvas (720 tall) shows rows 240-600, a phone canvas (720 wide) columns 300-660, and the 360x360
/// square both always show holds every subject. A screen bigger than the frame meets the black
/// ground, which the art fades into at its edges.</para>
///
/// <para><b>Nothing here tints or alpha-fades the art</b> (.ai/title-screen.md, "Animate by frame,
/// never by tint"). Panels change by a dissolve through the Bayer matrix, the one fade the contract
/// allows: every pixel is the old panel's or the new one's, never a blend. The drift moves layers by
/// whole screen pixels. Only the caption and the skip prompt fade - type on its own layer, as the
/// title's prompt does.</para>
///
/// <para><b>Four panels move</b> (the four lifted, the town overrun, the hiding, the break): a box of the mid layer is swapped frame by frame
/// on a clock from an atlas, <c>&lt;name&gt;-anim.png</c>, by the timeline in <c>&lt;name&gt;-anim.json</c>
/// (<see cref="AnimSpec"/>) - the title's frame-swap, never a tint. Any panel the generator gives the
/// pair to animates the same way.</para>
///
/// <para><b>Reels.</b> The same player plays the lessons (.ai/slow-start.md, "The lessons"): the short
/// cutscene that teaches a system after the run that found its teacher (Teachings), drawn by
/// tools/art/lessons.py on this frame and played over the paused run, before the results
/// (<see cref="CreateEmbedded"/>), and again from Options. Each is a <see cref="Reel"/> in <see cref="Reels"/>;
/// the prologue is the reel named <see cref="PrologueReelId"/>, and only it is ever marked seen here.</para>
/// </remarks>
public partial class Prologue : Control
{
	public const string ScenePath = "res://scenes/Prologue.tscn";
	private const string PrologueFolder = "res://assets/bonelight/ui/prologue/";
	private const string LessonFolder = "res://assets/bonelight/ui/lessons/";
	private const string FreedFolder = "res://assets/bonelight/ui/freed/";
	// The story's reels that are neither the prologue nor a freeing: reel E, the light going on (LightGoesOn).
	private const string StoryFolder = "res://assets/bonelight/ui/story/";
	public const string PrologueReelId = "prologue";
	private const string CharacterSelectionPath = "res://scenes/CharacterSelection.tscn";
	private const string TitleScreenPath = "res://scenes/TitleScreen.tscn";
	private const string WorldMapPath = "res://scenes/WorldMap.tscn";

	// The frame, in art pixels, and its whole render scale. tools/art/prologue.py FW/FH.
	private static readonly Vector2 ArtSize = new(960f, 840f);
	private const float ArtScale = 2f;
	private static readonly string[] LayerNames = { "back", "mid", "fore" };

	/// <summary>Where the prologue goes when it ends.</summary>
	public enum After
	{
		/// <summary>On into story mode: the character select, as Start Run always did.</summary>
		Story,
		/// <summary>Back to the main menu it was opened from (Options, Watch the Prologue).</summary>
		Menu,
		/// <summary>
		/// Back to the world map. Two callers: WorldMap's replay-intro button (TEMPORARY, for Eric's testing, to go
		/// before the game ships for good - the button, not this value), and reel E, which the map plays for a save
		/// that owes it (LightGoesOn) and which stays. Never marks the prologue seen (<see cref="Finish"/>).
		/// </summary>
		Map,
	}

	// Static because the prologue is a scene of its own: whoever opens it says where it goes next, and which
	// reel it plays (the prologue unless a lesson is asked for).
	private static After next = After.Story;
	private static string nextReel = PrologueReelId;

	/// <summary>Opens the prologue, or the reel <paramref name="reelId"/>, which then continues to <paramref name="then"/>.</summary>
	public static void Play(Node from, After then, string reelId = PrologueReelId)
	{
		if (from == null || !IsInstanceValid(from))
			return;
		next = then;
		nextReel = Reels.ContainsKey(reelId ?? string.Empty) ? reelId : PrologueReelId;
		from.GetTree().ChangeSceneToFile(ScenePath);
	}

	/// <summary>
	/// A reel played inside whatever asks for it rather than as a scene: a lesson over the paused run, before
	/// the results (Node2DGame.Lessons.cs). <paramref name="finished"/> runs when it ends, played through or
	/// skipped; nothing is marked seen and no scene changes. The caller adds it to a CanvasLayer that keeps
	/// processing while the tree is paused.
	/// </summary>
	public static Prologue CreateEmbedded(string reelId, Action finished)
	{
		var player = new Prologue { Name = "Lesson_" + reelId, embeddedFinished = finished ?? (() => { }) };
		player.reel = Reels.TryGetValue(reelId ?? string.Empty, out Reel found) ? found : Reels[PrologueReelId];
		player.SetAnchorsPreset(LayoutPreset.FullRect);
		return player;
	}

	/// <summary>For a tool (_UiShot --reel=): the reel the next scene-mode prologue plays.</summary>
	public static void SetNextReel(string reelId) =>
		nextReel = Reels.ContainsKey(reelId ?? string.Empty) ? reelId : PrologueReelId;

	/// <summary>True when Start Run should show the prologue first: it has never been seen.</summary>
	public static bool IsUnseen(SaveManager saveManager) =>
		saveManager?.Data != null && !saveManager.Data.HasSeenPrologue;

	/// <summary>
	/// One panel: its art, how long it holds, its line (most have none - "keep text minimal"), and
	/// how far each layer drifts over the hold, in art pixels. Back drifts least and fore most, so
	/// the layers separate; a figure that moves (he rises, they march, the player walks) is its layer
	/// moving against the others.
	/// </summary>
	/// <remarks><paramref name="Ground"/>: a panel whose back and fore are shared with others (the freeings: one
	/// ground, a mid layer per wizard) names them here - <c>&lt;Ground&gt;-back.png</c> and <c>-fore.png</c>; null,
	/// the panel's own.</remarks>
	public readonly record struct Beat(string Name, float Seconds, string Caption, Vector2 Back, Vector2 Mid, Vector2 Fore,
		string Ground = null, PanelImpact? Impact = null);

	/// <summary>
	/// A panel's impact beat (.ai/cutscenes.md, "The cinematic rules", rule 1): <paramref name="At"/> seconds into the
	/// panel's own clock (the one its animated box runs on), speed lines thrown out of <paramref name="Art"/> (an art
	/// pixel of the frame, on the mid layer), a shake of <paramref name="ShakePx"/> canvas units through Reduce Screen
	/// Shake, and a dithered flash - in the warm light, the Archmage's, when <paramref name="Warm"/>, else the dark
	/// wizard's cold violet. <paramref name="FlashScale"/> scales the flash's density and reach alone (the tide's
	/// burst takes a dimmer pulse, so the break stays the prologue's biggest light). Only where something lands:
	/// most panels are calm, and have none.
	/// </summary>
	public readonly record struct PanelImpact(float At, Vector2 Art, bool Warm, float ShakePx = 8f, float FlashScale = 1f);

	// THE KEN BURNS MOVES (rules 3 and 5). Each layer's drift eases in and out (a smoothstep) and is still at both
	// ends: the panel holds as it dissolves in and a beat after (DriftStillIn) before anything moves, and comes to
	// rest DriftStillOut before its hold ends, under the next panel's dissolve. It used to run linearly from the first
	// frame to the last - moving at once, and two moving pictures crossing in every dissolve.

	/// <summary>Seconds a panel is still at the start of its hold: its dissolve in (0.7 s) and a beat.</summary>
	public const float DriftStillIn = 0.9f;

	/// <summary>Seconds a panel is still at the end of its hold.</summary>
	public const float DriftStillOut = 0.35f;

	/// <summary>The fastest any layer may drift, canvas units a second at the eased peak. The validator holds every beat to it.</summary>
	public const float MaxDriftSpeed = 28f;

	/// <summary>
	/// Seconds the black ground holds after the last panel has dissolved out, before the menu, the character select or
	/// the results (rule 4): the reel ends, and then the next screen comes, never the two on one frame.
	/// </summary>
	public const float EndHoldSeconds = 0.6f;

	/// <summary>The fastest a beat's layer drifts at the eased peak, canvas units a second: the validator's number.</summary>
	public static float PeakDriftSpeed(Beat beat)
	{
		float span = Mathf.Max(0.1f, beat.Seconds - DriftStillIn - DriftStillOut);
		float longest = Mathf.Max(beat.Back.Length(), Mathf.Max(beat.Mid.Length(), beat.Fore.Length()));
		// A smoothstep's steepest is 1.5 times its average.
		return 1.5f * longest * ArtScale / span;
	}

	/// <summary>The art frame's size in art pixels and its whole render scale, for the validator.</summary>
	public static Vector2 FrameArtSize => ArtSize;

	/// <summary>
	/// A run of panels from one art folder: the prologue, one lesson, or one freeing. <paramref name="WizardId"/>: the
	/// wizard a freeing shows, whose name its line carries ("{0}'s cell stands open.", QuestLog's freed line).
	/// </summary>
	public sealed record Reel(string Id, string Folder, Beat[] Beats, string WizardId = null);

	private static readonly Beat[] PrologueBeats =
	{
		// The circle where the wizards met, every one of their lights still in the world. Its line says where they are
		// (Eric, 2026-11-03: "When the story says 'he came here first'... where are they? can we add a little
		// context?", then "lets include that explanation in that first cutscene then 'The stones where we used to
		// meet.'"). With the next panel's line it is the Broken Circle's own note word for word (ChapterNotes,
		// stage_10), so the player who reaches the stones has heard it before.
		new("moot", 4.6f, "The stones where we used to meet.", new Vector2(-4f, 0f), new Vector2(-10f, 0f), new Vector2(-26f, 0f)),
		// HE LIFTS THEM, ONE BY ONE (Eric, 2026-11-08, of the panel as it was - his silhouette filling the frame and
		// the wizards dark specks at his feet: "it's hard to see what he's doing in the background, it should show him
		// lifting each of the wizards into the air to capture them"; then, of the first answer: "the silhouette of 4
		// wizards, cast in shadow, in the background, with the dark wizard in the foreground. remove the purple wizard,
		// since he is the wizard that you are"; and "they should still be at the stone circles. you can also show the
		// colored dark silhouettes when he lifts them"). Its box animates (tools/art/prologue.py, THRONE_BOX, 12 frames
		// a second): him in the foreground with his back to us, his black sun behind his head, and beyond him the four
		// disciplines in the moot's ring of stones, cast in shadow against the sky; from 1.25 s, one every 0.65 s, his
		// reach leaves his hand, a hoop of his violet closes on one, his own colour comes up on him dark and shaded,
		// and he is lifted out of the ring into the sky, where he hangs while his light streams back into the hand. All
		// four at rest by 4.8 s. HELD 5.9 s, not 5.2: four lifts the eye can follow one after another (each under 120
		// art px/s), a still beat before the first and 0.4 s after the last before the dissolve. No impact (rule 1):
		// the tide's burst, a few seconds on, is the one that is his. Back and mid drift as ONE, so a wizard standing
		// in the ring never slides on it; the slow tilt up follows the rise.
		new("throne", 5.9f, "He came here first.", new Vector2(0f, 4f), new Vector2(0f, 4f), new Vector2(0f, 10f)),
		// The wizards led away to his tower, their light drawn out of them. The line says WHY these wizards (Eric,
		// 2026-11-03: the story should "specifically mention that the villain was imprisoning any wizard he thought
		// could rival him"); it was "He took them all", which gave no reason. It sets up the next panel: with the
		// wizards gone, no one is left to defend the land. 41 characters, under the PC's one-line 46
		// (RegressionChecks.Cutscenes).
		new("taken", 5.2f, "He took every wizard who might rival him.", new Vector2(2f, 0f), new Vector2(14f, -2f), new Vector2(-20f, 0f)),
		// His shadows over the land, coming down on a town that is still lit. Moved after the procession and given a line
		// (Eric, 2026-11-08: "move this panel after the 'he took every wizard who might rival him' panel", "There was no
		// one left to defend them" - the full stop added, as on every other line; 37 characters) and redrawn as "a town
		// being overrun by shadows, with the people running away. the tower should be in the distant background with
		// the glowing eye". THE RUSH (Eric, 2026-11-05: the army must rush over the horizon; the old beat slid the whole
		// crowd across a still picture as one layer): its box animates (tools/art/prologue.py, TIDE_BOX) - the slope
		// empty for a breath, heads behind the crest, the wave over it, the flood down the slope and into the town's
		// near half, its people running from the other, the front rank in its streets as the panel ends. Back and mid
		// drift as ONE, so the flood never slides on the ground it runs on; the motion is the bodies' own. THE IMPACT
		// (rule 1): at 1.4 s the wave bursts over the crest (TIDE_BURST) - cold speed lines out of the ridge, a shake,
		// his violet: the one impact that is his, not the player's. Its point, (600, 404), is crest inside the 360x360
		// square both canvases show, so the phone sees the burst too - and a step west of the tower's column, so the
		// burst is the wave's and not the tower exploding.
		new("tide", 5.6f, "There was no one left to defend them.", new Vector2(-6f, 0f), new Vector2(-6f, 0f), new Vector2(-20f, 0f),
			Impact: new PanelImpact(1.4f, new Vector2(600f, 404f), Warm: false, ShakePx: 7f, FlashScale: 0.6f)),
		// The one he missed, hiding behind the biggest of the stones while his creatures search (its box animates; since
		// 2026-11-08, Eric: "the wizard is standing out in the open. he should be hiding properly", pressed against the
		// stone's far side from the hound). The back and the mid drift as one, so the searchers never slide on the
		// ground they walk.
		new("missed", 5.4f, "You are the one he missed.", new Vector2(-4f, 0f), new Vector2(-4f, 0f), new Vector2(14f, 0f)),
		// THE FREEING (.ai/cutscenes.md; Eric, 2026-11-02: "Include a cutscene where the blue robed wizard frees the
		// four starting wizards."). The Archmage goes back for the four disciplines, on their knees in the dark
		// wizard's chain, and his light breaks it (its box animates: tools/art/prologue.py, BREAK_BOX); their own
		// lights come back to them, they stand and turn out, and the dark wizard's creatures close in on the five
		// (Eric, 2026-11-08: "show enemies closing in on the wizards after he breaks the chains and you can see the
		// wizards"). Back and mid as one again: the four kneel on the ground the back layer lights.
		// THE IMPACT (rule 1): at 2.0 s the light his staff holds bursts and the chain breaks (tools/art/prologue.py,
		// BREAK_AT and _flare, drawn at the staff's head, art (442, 418)). Speed lines out of it in his warm light, a
		// shake and a flash, over the burst the generator draws.
		// THE LINE: Eric's, 2026-11-08, "You rescued four - they now stand with you." (43 characters); before it "You
		// rescued them, and they stand with you." (2026-11-03: "it should sound more like you rescued them and now
		// they can work with you"), once "You went back for them".
		new("break", 5.4f, "You rescued four - they now stand with you.", new Vector2(-3f, 0f), new Vector2(-3f, 0f), new Vector2(12f, 0f),
			Impact: new PanelImpact(2.0f, new Vector2(442f, 418f), Warm: true)),
		// The road to all of it, past the places the campaign visits in its own order (Eric, 2026-11-05: landmarks in
		// place of the empty plain; tools/art/prologue.py, "THE JOURNEY'S LANDMARKS"), and the five of them on it, small:
		// the five's own panel folded in (Eric, 2026-11-08: "remove this, combine with the last panel", and of this one
		// "make the wizard smaller on the map"). Its line is the five's old second half, the player's goal: "The rest
		// still wait for you." (28 characters). The mid layer, which carries the five, drifts up and to the left - the
		// way the road climbs where they walk it - so they go on up it as the panel holds.
		new("road", 5.4f, "The rest still wait for you.", new Vector2(0f, 3f), new Vector2(-6f, -9f), new Vector2(0f, 18f)),
	};

	// THE LESSONS (.ai/slow-start.md, "The lessons"; tools/art/lessons.py). Each explains one system in two
	// panels and two lines, held longer than the prologue's because the line is the lesson. A line says what
	// the system does; the picture says what it feels like. Ids are Teachings' lesson ids.
	// Every lesson line below and every freeing line (QuestLog.Prisoners) is Eric's own wording from the story review
	// of 2026-11-08 (.ai/cutscenes.md, "The story review"). The Conjunctions' first line carries no full stop on
	// purpose: with one it would be 47 characters, over the PC's one-line 46.
	private static readonly Beat[] AscensionBeats =
	{
		// The find giving up its light to the one he missed. Reworded 2026-11-07 (Eric: the old
		// "What he missed kept what the wizards knew." read as a riddle at the moment of the lesson).
		new("ascension-gift", 5.0f, "He took the wizards, but not what they knew.", new Vector2(-3f, 0f), new Vector2(-8f, -6f), new Vector2(-18f, 0f)),
		// A spell's light climbing seven marks and branching at the eighth.
		new("ascension-rise", 6.0f, "Spells may now ascend after max level.", new Vector2(0f, 3f), new Vector2(0f, -12f), new Vector2(0f, 12f)),
	};

	private static readonly Beat[] ApotheosisBeats =
	{
		// The circle where the wizards met, and his own spell burning in its middle.
		new("apotheosis-circle", 5.0f, "The wizards unlock an ancient power.", new Vector2(-3f, 0f), new Vector2(-10f, 0f), new Vector2(-22f, 0f)),
		// The spell at its last level, ascended, becoming it.
		new("apotheosis-form", 6.0f, "Your wizard's starting spell has a final form.", new Vector2(0f, 4f), new Vector2(0f, -14f), new Vector2(0f, 10f)),
	};

	private static readonly Beat[] ConjunctionBeats =
	{
		// Two lights, each on its own stone, leaning toward each other.
		new("conjunctions-two", 5.0f, "The whole is greater than the sum of its parts", new Vector2(2f, 0f), new Vector2(10f, -2f), new Vector2(-18f, 0f)),
		// The two made one.
		new("conjunctions-one", 6.0f, "Two ascended spells may now become one.", new Vector2(0f, 3f), new Vector2(0f, -10f), new Vector2(0f, 12f)),
	};

	private static readonly Beat[] RiteBeats =
	{
		// One spell crowned, the others' light pouring into it.
		new("rite-crown", 5.0f, "One spell may take the others' strength.", new Vector2(0f, 3f), new Vector2(0f, -12f), new Vector2(0f, 12f)),
		// The rest sealed for the run.
		new("rite-seal", 5.6f, "The rest fall silent; their levels feed it.", new Vector2(-3f, 0f), new Vector2(-8f, 0f), new Vector2(-18f, 0f)),
	};

	// REEL E, THE LIGHT GOES ON (.ai/second-half.md 6.3; tools/art/light_goes_on.py; LightGoesOn). After the Throne is
	// won: the light going home, and some of it going on east over the dark sea to the far shore. Two panels, the
	// Archmage at the centre of both, one line. Calm (no impact): the storm of motes was the dark wizard's fall, and
	// this is what came after it. The light's motes ride the mid layer and drift east against the back, so the stream
	// is seen to go.
	private static readonly Beat[] LightGoesOnBeats =
	{
		// His sea-gate on the rock's east side, from inside: the Archmage on its steps, his back to us, and the light
		// streaming out over him through the arch and away over the dark water. No line: the picture says it.
		new("lightgoes-gate", 5.6f, null, new Vector2(-2f, 0f), new Vector2(16f, 4f), new Vector2(-10f, 0f)),
		// Wide and low over the water: the stream running on east toward a line of white cliffs on the horizon and on
		// past the edge of the picture, the Archmage small on the quay below his rock. The line says the one thing the
		// picture cannot - why the light did not stop: it was never a wizard's (the doc's premise, section 2).
		new("lightgoes-sea", 6.4f, "Some of it was older than any of them.", new Vector2(4f, 0f), new Vector2(22f, -2f),
			new Vector2(-12f, 0f)),
	};

	// THE FREEINGS (.ai/cutscenes.md; tools/art/freed.py; Freeings). One panel each, made once as a template: the
	// shared ground, and a mid layer per wizard - the Archmage beside the one he has freed, that wizard's own light
	// coming back to him. Its line is the wizard's freed line from QuestLog.Prisoners, with his name in it. A god's
	// rekindling (.ai/second-half.md 6.3) is the same reel on the far shore's ground (Freeings.GroundFor), its line the
	// god's rekindled line.
	private static Reel FreedReel(string wizardId) =>
		new(Freeings.ReelIdFor(wizardId), FreedFolder,
			new[]
			{
				new Beat("freed-" + wizardId, 6.0f, QuestLog.PrisonerOf(wizardId)?.FreedLine,
					new Vector2(2f, 0f), new Vector2(2f, 0f), new Vector2(-14f, 0f), Ground: Freeings.GroundFor(wizardId)),
			},
			wizardId);

	/// <summary>Every reel, by id: the prologue, the five lessons (Teachings.All's ids) and the freeings (Freeings).</summary>
	public static readonly System.Collections.Generic.IReadOnlyDictionary<string, Reel> Reels = BuildReels();

	private static System.Collections.Generic.Dictionary<string, Reel> BuildReels()
	{
		var reels = new System.Collections.Generic.Dictionary<string, Reel>(StringComparer.OrdinalIgnoreCase)
		{
			[PrologueReelId] = new Reel(PrologueReelId, PrologueFolder, PrologueBeats),
			["ascension"] = new Reel("ascension", LessonFolder, AscensionBeats),
			["apotheosis"] = new Reel("apotheosis", LessonFolder, ApotheosisBeats),
			["conjunctions"] = new Reel("conjunctions", LessonFolder, ConjunctionBeats),
			["sovereign_rite"] = new Reel("sovereign_rite", LessonFolder, RiteBeats),
			[LightGoesOn.ReelId] = new Reel(LightGoesOn.ReelId, StoryFolder, LightGoesOnBeats),
		};
		foreach (string wizardId in Freeings.WizardIds)
			reels[Freeings.ReelIdFor(wizardId)] = FreedReel(wizardId);
		// F4, the far sky's lesson (Teachings.FarSkyLesson; its beats in Prologue.FarSky.cs).
		reels[Teachings.FarSkyLesson.Id] = new Reel(Teachings.FarSkyLesson.Id, LessonFolder, FarSkyBeats());
		return reels;
	}

	/// <summary>The path of one of a panel's layers ("back", "mid", "fore" or "anim"): a shared ground's for back and fore.</summary>
	public static string LayerPathFor(Reel reel, Beat beat, string layer)
	{
		string name = beat.Ground != null && (layer == "back" || layer == "fore") ? beat.Ground : beat.Name;
		return $"{reel.Folder}{name}-{layer}.png";
	}

	/// <summary>
	/// How long a reel plays untouched, in seconds: each panel's hold, less the dissolve the next one overlaps it by,
	/// the last one's dissolve out, and the black held after it. For a probe that waits it out (_PrologueProbe).
	/// </summary>
	public static float ReelSeconds(string reelId, float dissolveSeconds = 0.7f)
	{
		if (!Reels.TryGetValue(reelId ?? string.Empty, out Reel found) || found.Beats.Length == 0)
			return 0f;
		float total = dissolveSeconds;
		foreach (Beat beat in found.Beats)
			total += beat.Seconds - dissolveSeconds;
		return total + dissolveSeconds + EndHoldSeconds;
	}

	/// <summary>A panel's line as it is shown: a freeing's carries its wizard's name.</summary>
	public static string CaptionFor(Reel reel, Beat beat)
	{
		if (string.IsNullOrEmpty(beat.Caption) || string.IsNullOrEmpty(reel?.WizardId))
			return beat.Caption;
		return string.Format(beat.Caption, QuestLog.NameOf(reel.WizardId));
	}

	// The reel this player plays, and its beats; set before _Ready (Play's static, or CreateEmbedded).
	private Reel reel;
	private Beat[] Beats => reel.Beats;
	// Set only when embedded: what runs when it ends, in place of a scene change.
	private Action embeddedFinished;
	private bool Embedded => embeddedFinished != null;

	[Export] public float DissolveSeconds { get; set; } = 0.7f;
	[Export] public float HoldToSkipSeconds { get; set; } = 0.9f;
	[Export] public float CaptionInAt { get; set; } = 0.9f;
	[Export] public float CaptionFadeSeconds { get; set; } = 0.7f;

	// The dissolve: a pixel of the incoming panel shows once its Bayer threshold is under the
	// progress. Thresholds come from the layer's own art pixel (UV x art size), so the pattern sits on
	// the art grid at x2 and moves with the layer as it drifts.
	private const string DissolveShaderCode = @"
shader_type canvas_item;
uniform float progress : hint_range(0.0, 1.0) = 1.0;
uniform vec2 art_size = vec2(960.0, 840.0);

int bayer8(ivec2 p) {
	int x = p.x & 7;
	int y = p.y & 7;
	int xy = x ^ y;
	return ((xy & 1) << 5) | ((y & 1) << 4) | ((xy & 2) << 2) | ((y & 2) << 1) | ((xy & 4) >> 1) | ((y & 4) >> 2);
}

void fragment() {
	ivec2 p = ivec2(floor(UV * art_size));
	float threshold = (float(bayer8(p)) + 0.5) / 64.0;
	if (threshold > progress) {
		discard;
	}
}
";

	// One per prologue, shared by its panels. Not static: a Godot resource held by a C# static
	// outlives the engine's own cleanup and is reported at exit as a resource still in use.
	private Shader dissolveShader;

	private sealed class PanelView
	{
		public Control Root;
		public TextureRect[] Layers;
		public ShaderMaterial Material;
		public float Elapsed;
		// The animated box, when the panel has one: its frames, its own copy of the dissolve (its UVs are
		// the atlas's, not the frame's), and the atlas frame on screen.
		public AnimSpec Anim;
		public AtlasTexture AnimAtlas;
		public ShaderMaterial AnimMaterial;
		public int AnimFrame = -1;
		// The panel's impact beat has fired (Beat.Impact): once a panel, however long it is held.
		public bool ImpactFired;
	}

	/// <summary>
	/// A panel's animated box (tools/art/prologue.py, write_anim): where it sits on the frame and its size,
	/// in art pixels; how many frames a row of the atlas holds; the clock; and the atlas frame for each
	/// tick, so a held frame is stored once. Every number is a multiple of 8, so the dissolve's Bayer
	/// threshold, taken from the atlas's own pixel, matches the mid layer's under it pixel for pixel.
	/// </summary>
	private sealed class AnimSpec
	{
		public int X, Y, Width, Height, Columns;
		public float Fps;
		public int[] Timeline = Array.Empty<int>();

		public Rect2 Region(int frame) =>
			new(frame % Columns * Width, frame / Columns * Height, Width, Height);

		public int FrameAt(float seconds)
		{
			int tick = Mathf.Clamp((int)(seconds * Fps), 0, Timeline.Length - 1);
			return Timeline[tick];
		}
	}

	private PanelView[] views;
	private int current;
	private bool finished;
	private bool endingOut;
	// Seconds on the black ground after the last panel has gone (EndHoldSeconds).
	private float endHeld;

	private Control stage;

	// THE IMPACT BEATS (Beat.Impact). The shake moves the stage and the fx layer together, on whole art pixels;
	// the speed lines and the flash draw on the fx layer, over every panel and under the caption and the skip.
	private Control fx;
	private readonly ScreenShake panelShake = new();
	private readonly SpeedLines panelLines = new();
	private readonly DitherFlash panelFlash = new();
	private bool fxDrawn;

	/// <summary>Where the impact's shake has the picture this frame, canvas units; zero at rest. For _UiShot.</summary>
	public Vector2 ShakeOffset => stage?.Position ?? Vector2.Zero;

	// Speed lines out of the panel's subject: from inside the burst the art draws (its rays reach ~130 art px) out
	// past the edges of what a PC or a phone shows of the frame, one art pixel thick (two canvas units) on the art
	// grid. Fewer and quicker than a boss's: the drawn burst is the subject, and the four in the chain must still
	// read through them (the first cut's 28 for 0.6 s covered them).
	private const float ImpactLinesInner = 50f * ArtScale;
	private const float ImpactLinesOuter = 330f * ArtScale;
	private const int ImpactLinesCount = 20;
	private const float ImpactLinesSeconds = 0.45f;
	private const float ImpactShakeSeconds = 0.4f;
	// The flash blooms out of the same light, dense at it and thinning to nothing 260 art px out (DitherFlash).
	private const float ImpactFlashDensity = 0.8f;
	private const float ImpactFlashSeconds = 0.24f;
	private const float ImpactFlashReach = 260f * ArtScale;
	private Label caption;
	private Button skipButton;
	private ColorRect skipFill;
	private bool skipHeldByButton;
	private bool skipHeldByCancel;
	private float skipHold;
	// The prompt rests at half strength so it does not compete with the art, and comes up to full
	// for a few seconds after any input - the moment a player is looking for a way out.
	private const float SkipRestAlpha = 0.5f;
	private const float SkipWakeSeconds = 2.5f;
	private float skipWake = SkipWakeSeconds;

	public override void _Ready()
	{
		if (reel == null)
		{
			reel = Reels[nextReel];
			nextReel = PrologueReelId;
		}
		views = new PanelView[Beats.Length];
		MouseFilter = MouseFilterEnum.Ignore;
		BuildUi();
		// The menu theme carries on from the title; idempotent, never a restart. Not over a run: its music has
		// faded under the result, and the results bring their own.
		if (!Embedded)
			GetNodeOrNull<MusicPlayer>("/root/MusicPlayer")?.PlayMenuMusic();

		ShowPanel(0);
		RequestPanel(1);

		// Clear of a phone's rounded corners and camera hole; nothing on desktop. The art is centre-
		// anchored and the ground full-screen, so only the caption and the skip prompt move.
		SafeArea.Apply(this);
		// The one control: Skip. Focused on arrival so a pad's A and the keyboard's Enter hold it.
		MenuNavigator.Attach(this, skipButton);
		GD.Print($"Prologue: _Ready ({reel.Id})");
	}

	private void BuildUi()
	{
		var ground = new ColorRect { Name = "Ground", Color = new Color("05070c"), MouseFilter = MouseFilterEnum.Ignore };
		ground.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(ground);

		stage = new Control { Name = "Stage", MouseFilter = MouseFilterEnum.Ignore };
		stage.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(stage);

		fx = new Control
		{
			Name = "ImpactFx",
			MouseFilter = MouseFilterEnum.Ignore,
			TextureFilter = TextureFilterEnum.Nearest,
			TextureRepeat = TextureRepeatEnum.Enabled,
		};
		fx.SetAnchorsPreset(LayoutPreset.FullRect);
		fx.Draw += DrawFx;
		AddChild(fx);

		caption = new Label
		{
			Name = "Caption",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			AnchorLeft = 0f,
			AnchorRight = 1f,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		ResponsiveLayout.SetFont(caption, ResponsiveLayout.TextRole.Display);
		// Bone, the palette's pale stone, over an occlusion outline: the art behind every caption
		// is the darkened ground, but the outline keeps it legible whatever drifts under it.
		caption.AddThemeColorOverride("font_color", new Color("d4dcea"));
		caption.AddThemeColorOverride("font_outline_color", new Color("05070c"));
		caption.AddThemeConstantOverride("outline_size", 8);
		AddChild(caption);
		PlaceCaption();
		GetViewport().SizeChanged += PlaceCaption;

		// HOLD to skip, never tap: on a phone a stray touch must not throw away the story, and the
		// same rule for every device means the prompt says one thing. A button, so a pad and a
		// keyboard reach it through MenuNavigator; ButtonDown/ButtonUp fire for a mouse, a finger,
		// Enter and A alike. Back (Escape, B, Start) holds it too, from anywhere.
		skipButton = new Button
		{
			Name = "SkipButton",
			Text = "Hold to skip",
			AnchorLeft = 1f,
			AnchorRight = 1f,
			AnchorTop = 0f,
			AnchorBottom = 0f,
			OffsetLeft = -236f,
			OffsetRight = -24f,
			OffsetTop = 24f,
			OffsetBottom = 76f,
			FocusMode = FocusModeEnum.All,
		};
		BonelightSkin.StyleButton(skipButton, BonelightSkin.Emphasis.Quiet);
		ResponsiveLayout.SetFont(skipButton, ResponsiveLayout.TextRole.Label);
		skipButton.ButtonDown += () => skipHeldByButton = true;
		skipButton.ButtonUp += () => skipHeldByButton = false;
		AddChild(skipButton);

		skipFill = new ColorRect
		{
			Name = "SkipFill",
			Color = new Color("d8b04a"),
			MouseFilter = MouseFilterEnum.Ignore,
			AnchorLeft = 0f,
			AnchorRight = 0f,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			OffsetTop = -5f,
			OffsetBottom = -2f,
			OffsetLeft = 6f,
			OffsetRight = 6f,
		};
		skipButton.AddChild(skipFill);
	}

	public override void _ExitTree()
	{
		if (GetViewport() is Viewport viewport)
			viewport.SizeChanged -= PlaceCaption;
		if (fx != null && IsInstanceValid(fx))
			fx.Draw -= DrawFx;
	}

	// The caption sits near the foot of what the screen shows. Every panel darkens its ground
	// toward the frame edge, so on a PC (rows 560-600 of the frame) and on a phone (rows ~690-790)
	// the line lands on the darkened ground under the subject, never across it.
	private void PlaceCaption()
	{
		if (caption == null)
			return;
		Vector2 size = GetViewportRect().Size;
		bool portrait = size.Y > size.X;
		float bottom = portrait ? 120f : 44f;
		caption.OffsetLeft = portrait ? 36f : 120f;
		caption.OffsetRight = portrait ? -36f : -120f;
		caption.OffsetBottom = -bottom;
		caption.OffsetTop = -bottom - 120f;
	}

	// --- panels -----------------------------------------------------------------------------
	private string LayerPath(Beat beat, string layer) => LayerPathFor(reel, beat, layer);
	private string AnimManifestPath(string beat) => $"{reel.Folder}{beat}-anim.json";

	/// <summary>The panel's animated box, or null: most panels have none. Malformed is an error, not a still.</summary>
	private AnimSpec LoadAnim(string beat)
	{
		string path = AnimManifestPath(beat);
		if (!FileAccess.FileExists(path))
			return null;
		try
		{
			using JsonDocument doc = JsonDocument.Parse(FileAccess.GetFileAsString(path));
			JsonElement root = doc.RootElement;
			var spec = new AnimSpec
			{
				X = root.GetProperty("x").GetInt32(),
				Y = root.GetProperty("y").GetInt32(),
				Width = root.GetProperty("width").GetInt32(),
				Height = root.GetProperty("height").GetInt32(),
				Columns = root.GetProperty("columns").GetInt32(),
				Fps = root.GetProperty("fps").GetSingle(),
			};
			var timeline = new System.Collections.Generic.List<int>();
			foreach (JsonElement frame in root.GetProperty("timeline").EnumerateArray())
				timeline.Add(frame.GetInt32());
			spec.Timeline = timeline.ToArray();
			if (spec.Columns <= 0 || spec.Width <= 0 || spec.Height <= 0 || spec.Fps <= 0f || spec.Timeline.Length == 0)
				throw new FormatException("an empty box, clock or timeline");
			return spec;
		}
		catch (Exception e) when (e is JsonException or FormatException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException)
		{
			GD.PushError($"Prologue: {path} is malformed ({e.Message}). Run: python tools/art/prologue.py, then import.");
			return null;
		}
	}

	/// <summary>Starts loading a panel's three layers on a worker thread, a panel ahead.</summary>
	private void RequestPanel(int index)
	{
		if (index < 0 || index >= Beats.Length)
			return;
		foreach (string layer in LayerNames)
		{
			string path = LayerPath(Beats[index], layer);
			if (ResourceLoader.Exists(path))
				ResourceLoader.LoadThreadedRequest(path, "Texture2D");
		}
		string anim = LayerPath(Beats[index], "anim");
		if (ResourceLoader.Exists(anim))
			ResourceLoader.LoadThreadedRequest(anim, "Texture2D");
	}

	private static Texture2D TakeLayer(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			GD.PushError($"Prologue: missing panel layer {path}. Run: python tools/art/prologue.py (lessons.py for a lesson), then import.");
			return null;
		}
		// Blocks only if the prefetch has not finished; a path never requested loads here.
		ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(path);
		if (status == ResourceLoader.ThreadLoadStatus.InvalidResource)
			return GD.Load<Texture2D>(path);
		return ResourceLoader.LoadThreadedGet(path) as Texture2D ?? GD.Load<Texture2D>(path);
	}

	private void ShowPanel(int index)
	{
		Beat beat = Beats[index];
		dissolveShader ??= new Shader { Code = DissolveShaderCode };
		var material = new ShaderMaterial { Shader = dissolveShader };
		material.SetShaderParameter("progress", 0f);
		material.SetShaderParameter("art_size", ArtSize);

		Vector2 half = ArtSize * ArtScale * 0.5f;
		var root = new Control
		{
			Name = "Panel_" + beat.Name,
			MouseFilter = MouseFilterEnum.Ignore,
			AnchorLeft = 0.5f,
			AnchorRight = 0.5f,
			AnchorTop = 0.5f,
			AnchorBottom = 0.5f,
			OffsetLeft = -half.X,
			OffsetRight = half.X,
			OffsetTop = -half.Y,
			OffsetBottom = half.Y,
			Material = material,
		};
		var layers = new TextureRect[LayerNames.Length];
		for (int i = 0; i < LayerNames.Length; i++)
		{
			layers[i] = new TextureRect
			{
				Name = LayerNames[i],
				Texture = TakeLayer(LayerPath(beat, LayerNames[i])),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				TextureFilter = TextureFilterEnum.Nearest,
				MouseFilter = MouseFilterEnum.Ignore,
				UseParentMaterial = true,
				Size = ArtSize * ArtScale,
			};
			root.AddChild(layers[i]);
		}
		var view = new PanelView { Root = root, Layers = layers, Material = material };
		AttachAnim(view, beat);
		stage.AddChild(root);
		views[index] = view;
		ApplyDrift(index);
	}

	// The animated box, laid over the mid layer as its child: it drifts with the mid layer, draws over it
	// and under the fore, and dissolves with the panel through its own copy of the shader.
	private void AttachAnim(PanelView view, Beat beat)
	{
		AnimSpec spec = LoadAnim(beat.Name);
		if (spec == null)
			return;
		Texture2D atlas = TakeLayer(LayerPath(beat, "anim"));
		if (atlas == null)
			return;
		var material = new ShaderMaterial { Shader = dissolveShader };
		material.SetShaderParameter("progress", 0f);
		material.SetShaderParameter("art_size", atlas.GetSize());
		int first = spec.FrameAt(0f);
		var frames = new AtlasTexture { Atlas = atlas, Region = spec.Region(first) };
		var rect = new TextureRect
		{
			Name = "anim",
			Texture = frames,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			TextureFilter = TextureFilterEnum.Nearest,
			MouseFilter = MouseFilterEnum.Ignore,
			Material = material,
			Position = new Vector2(spec.X, spec.Y) * ArtScale,
			Size = new Vector2(spec.Width, spec.Height) * ArtScale,
		};
		view.Layers[1].AddChild(rect);
		view.Anim = spec;
		view.AnimAtlas = frames;
		view.AnimMaterial = material;
		view.AnimFrame = first;
	}

	private static void SetProgress(PanelView view, float progress)
	{
		view.Material.SetShaderParameter("progress", progress);
		view.AnimMaterial?.SetShaderParameter("progress", progress);
	}

	// The frame for the panel's own clock: it starts with the panel's dissolve in, so the beat it shows
	// is timed against the hold, and the last frame holds if the panel outstays it.
	private static void StepAnim(PanelView view)
	{
		if (view.Anim == null)
			return;
		int frame = view.Anim.FrameAt(view.Elapsed);
		if (frame == view.AnimFrame)
			return;
		view.AnimFrame = frame;
		view.AnimAtlas.Region = view.Anim.Region(frame);
	}

	private void ApplyDrift(int index)
	{
		PanelView view = views[index];
		if (view == null)
			return;
		Beat beat = Beats[index];
		// -0.5 to +0.5 over the hold, so the picture the generator composed is the one on screen
		// mid-panel, and every layer stays within half its drift of where it was drawn - still for
		// DriftStillIn and DriftStillOut at its ends and eased between (rules 3 and 5).
		float span = Mathf.Max(0.1f, beat.Seconds - DriftStillIn - DriftStillOut);
		float u = Mathf.Clamp((view.Elapsed - DriftStillIn) / span, 0f, 1f);
		float t = u * u * (3f - 2f * u) - 0.5f;
		// Whole SCREEN pixels: a layer at a fraction of one would be resampled (.ai/art-direction.md 6a).
		float perUnit = Mathf.Max(0.01f, GetViewport().GetFinalTransform().X.X);
		Vector2[] drift = { beat.Back, beat.Mid, beat.Fore };
		for (int i = 0; i < view.Layers.Length; i++)
		{
			Vector2 offset = drift[i] * t * ArtScale;
			view.Layers[i].Position = new Vector2(Mathf.Round(offset.X * perUnit), Mathf.Round(offset.Y * perUnit)) / perUnit;
		}
	}

	public override void _Process(double delta)
	{
		if (finished)
			return;
		float dt = (float)delta;
		UpdateSkip(dt);
		if (finished)
			return;

		for (int i = 0; i < views.Length; i++)
		{
			if (views[i] == null)
				continue;
			views[i].Elapsed += dt;
			ApplyDrift(i);
			StepAnim(views[i]);
			FireImpact(i);
		}
		TickFx(dt);

		if (endHeld > 0f || (endingOut && views[current] == null))
		{
			// The reel is over and the ground is black: hold it, then move on (rule 4).
			endHeld += dt;
			if (endHeld >= EndHoldSeconds)
				Finish();
			return;
		}

		PanelView now = views[current];
		Beat beat = Beats[current];
		if (!endingOut)
			SetProgress(now, Mathf.Clamp(now.Elapsed / DissolveSeconds, 0f, 1f));

		int nextIndex = current + 1;
		if (nextIndex < Beats.Length)
		{
			// The next panel dissolves in over the last moments of this one, which keeps drifting
			// underneath it; once it covers the screen this one is freed.
			if (views[nextIndex] == null && now.Elapsed >= beat.Seconds - DissolveSeconds)
			{
				ShowPanel(nextIndex);
				RequestPanel(nextIndex + 1);
			}
			PanelView incoming = views[nextIndex];
			if (incoming != null)
			{
				SetProgress(incoming, Mathf.Clamp(incoming.Elapsed / DissolveSeconds, 0f, 1f));
				if (incoming.Elapsed >= DissolveSeconds)
				{
					now.Root.QueueFree();
					views[current] = null;
					current = nextIndex;
				}
			}
		}
		else if (now.Elapsed >= beat.Seconds)
		{
			// The last panel dissolves out to the ground, and then story mode.
			endingOut = true;
			float progress = 1f - (now.Elapsed - beat.Seconds) / DissolveSeconds;
			SetProgress(now, Mathf.Clamp(progress, 0f, 1f));
			if (progress <= 0f)
			{
				// Gone to the ground. The black holds for EndHoldSeconds before the next screen (above).
				now.Root.QueueFree();
				views[current] = null;
				caption.Modulate = new Color(1f, 1f, 1f, 0f);
				endHeld = 0.0001f;
				return;
			}
		}

		UpdateCaption();
	}

	// --- the impact beats ---------------------------------------------------------------------------------

	// A panel's impact on the frame its own clock passes it: the speed lines laid out from where its subject stands
	// now, on the stage's own grid, a kick of the shake and the flash.
	private void FireImpact(int index)
	{
		PanelView view = views[index];
		if (view == null || view.ImpactFired || Beats[index].Impact is not PanelImpact impact || view.Elapsed < impact.At)
			return;
		view.ImpactFired = true;
		// A panel come to after its moment (a long first frame) fires nothing: the burst belongs to the moment.
		if (view.Elapsed - impact.At > 0.25f)
			return;
		Vector2 at = view.Root.Position + view.Layers[1].Position + impact.Art * ArtScale;
		ElementColors.Ramp ramp = ElementColors.GetRamp(impact.Warm ? Element.Light : Element.Darkness);
		// The light's pale yellow into its gold on the warm side (its two stops under white, so the lines sit inside
		// the burst rather than over it); the violet's hot into its mid on the cold, whose edge is near black.
		Color near = impact.Warm ? ramp.Mid : ramp.Hot;
		Color far = impact.Warm ? ramp.Edge : ramp.Mid;
		panelLines.Burst(at, ImpactLinesInner, ImpactLinesOuter, ImpactLinesCount, ImpactLinesSeconds, near, far, ArtScale,
			(uint)(index * 7919 + 17), SpeedLines.Motion.Outward);
		panelShake.Kick(impact.ShakePx, ImpactShakeSeconds);
		// White at the light while it is young, the ramp's own far stop (gold, or violet) out from it.
		panelFlash.Flash(ramp.Core, far, ImpactFlashDensity * impact.FlashScale, ImpactFlashSeconds,
			at, ImpactFlashReach * impact.FlashScale);
		fx.QueueRedraw();
	}

	// The shake (through Reduce Screen Shake, in ScreenShake.Kick) moves the stage and the fx layer by whole art
	// pixels and whole screen pixels both, so no layer is ever resampled (.ai/art-direction.md 6a).
	private void TickFx(float dt)
	{
		Vector2 shake = panelShake.Step(dt);
		float perUnit = Mathf.Max(0.01f, GetViewport().GetFinalTransform().X.X);
		var offset = new Vector2(Mathf.Round(shake.X / ArtScale), Mathf.Round(shake.Y / ArtScale)) * ArtScale;
		offset = new Vector2(Mathf.Round(offset.X * perUnit), Mathf.Round(offset.Y * perUnit)) / perUnit;
		if (stage.Position != offset)
		{
			stage.Position = offset;
			fx.Position = offset;
		}
		panelLines.Tick(dt);
		panelFlash.Tick(dt);
		if (panelLines.Active || panelFlash.Active || fxDrawn)
			fx.QueueRedraw();
	}

	private void DrawFx()
	{
		fxDrawn = panelLines.Active || panelFlash.Active;
		// The flash under the lines, so the streaks stay whole through the bloom.
		panelFlash.Draw(fx, new Rect2(new Vector2(-32f, -32f), fx.Size + new Vector2(64f, 64f)));
		panelLines.Draw(fx);
	}

	private void UpdateCaption()
	{
		PanelView view = views[current];
		Beat beat = Beats[current];
		string line = CaptionFor(reel, beat);
		if (view == null || string.IsNullOrEmpty(line))
		{
			caption.Modulate = new Color(1f, 1f, 1f, 0f);
			return;
		}
		if (caption.Text != line)
			caption.Text = line;
		float e = view.Elapsed;
		float fadeIn = Mathf.Clamp((e - CaptionInAt) / CaptionFadeSeconds, 0f, 1f);
		// Gone before the next panel starts to dissolve in, so a line never sits over two pictures.
		float outStart = beat.Seconds - DissolveSeconds - CaptionFadeSeconds;
		float fadeOut = 1f - Mathf.Clamp((e - outStart) / CaptionFadeSeconds, 0f, 1f);
		caption.Modulate = new Color(1f, 1f, 1f, Mathf.Min(fadeIn, fadeOut));
	}

	// --- skipping -----------------------------------------------------------------------------
	// Only watches: nothing is consumed here, so the button still gets its press.
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true } or InputEventMouseButton { Pressed: true }
			or InputEventScreenTouch { Pressed: true } or InputEventJoypadButton { Pressed: true })
			skipWake = SkipWakeSeconds;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			skipHeldByCancel = true;
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionReleased("ui_cancel"))
		{
			skipHeldByCancel = false;
			GetViewport().SetInputAsHandled();
		}
	}

	private void UpdateSkip(float dt)
	{
		// The button's own draw state as well as its signals: a finger dragged off the button stops
		// holding it, and a release that lands elsewhere must not leave the hold running.
		bool buttonDown = skipHeldByButton && skipButton.GetDrawMode() is BaseButton.DrawMode.Pressed or BaseButton.DrawMode.HoverPressed;
		bool holding = buttonDown || skipHeldByCancel;
		skipHold = holding ? skipHold + dt : Mathf.Max(0f, skipHold - dt * 3f);
		float fill = Mathf.Clamp(skipHold / HoldToSkipSeconds, 0f, 1f);
		skipFill.OffsetRight = 6f + (skipButton.Size.X - 12f) * fill;
		skipFill.Visible = fill > 0f;
		skipWake = Mathf.Max(0f, skipWake - dt);
		float alpha = holding || skipWake > 0f ? 1f : SkipRestAlpha;
		skipButton.Modulate = new Color(1f, 1f, 1f, Mathf.MoveToward(skipButton.Modulate.A, alpha, dt * 2.5f));
		if (fill >= 1f)
			Finish();
	}

	/// <summary>Ends the prologue - played through or skipped - records it as seen, and moves on.</summary>
	private void Finish()
	{
		if (finished)
			return;
		finished = true;
		// Reel E is seen once it has ended, however it was reached (the map, the Throne's end, Options).
		if (reel.Id == LightGoesOn.ReelId)
			MarkLightGoesOnSeen();
		if (Embedded)
		{
			// Over a run: the caller teaches the lesson and goes on to the results. This player goes with it.
			Action done = embeddedFinished;
			embeddedFinished = () => { };
			done();
			QueueFree();
			return;
		}
		After then = next;
		next = After.Story;
		// A replay from the map is a test, not the player meeting the story: the flag is left as it was. A
		// lesson replayed from Options marks nothing either: it was learned when it was first shown.
		if (then != After.Map && reel.Id == PrologueReelId)
			MarkSeen();
		if (then == After.Menu)
		{
			Global.OpenMenuImmediately = true;
			GetTree().ChangeSceneToFile(TitleScreenPath);
		}
		else if (then == After.Map)
		{
			GetTree().ChangeSceneToFile(WorldMapPath);
		}
		else
		{
			GetTree().ChangeSceneToFile(CharacterSelectionPath);
		}
	}

	// In memory always - a tool run's map must not play it a second time - and saved when writes are on.
	private void MarkLightGoesOnSeen()
	{
		var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (LightGoesOn.MarkSeen(saveManager?.Data))
			saveManager.SaveGame();
	}

	private void MarkSeen()
	{
		// A tool scene or a --no-save-writes run never marks it: the flag is the player's, and a
		// screenshot that played the prologue is not the player having seen it.
		if (SaveManager.WritesDisabled)
			return;
		var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager?.Data == null || saveManager.Data.HasSeenPrologue)
			return;
		saveManager.Data.HasSeenPrologue = true;
		saveManager.SaveGame();
	}
}
