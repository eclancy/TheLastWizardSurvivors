using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

/// <summary>How a hint's caption is set: a person's words, or the sound something makes.</summary>
public enum HintVoice
{
	/// <summary>Someone held there, speaking. Set in quotation marks. Never names them.</summary>
	Speech,
	/// <summary>A sound with nobody behind it - leaves, a bell. Set plainly, as a caption of the sound.</summary>
	Sound,
}

/// <summary>
/// What a hint shows in the world, coming in from the edge of the screen on the side the sound came
/// from (StageHintCue). The ORDER IS AN API: it is the row order of tools/art/hint_cues.py's sheet.
/// </summary>
public enum HintCue
{
	/// <summary>Leaves tumbling in on no wind (the grove).</summary>
	Leaves,
	/// <summary>Arcs of a voice coming down a corridor (the cell).</summary>
	Echo,
	/// <summary>Cold flames flickering and guttering (the lanterns).</summary>
	Wisp,
	/// <summary>Notes of a song, each with a frost glint behind it (the thaw).</summary>
	Song,
	/// <summary>A bell swinging, ringing at each strike (the caravan).</summary>
	Bell,
	/// <summary>Strips of written paper tumbling in, charring at the edges as they fall (the seal).</summary>
	Paper,
	/// <summary>Sparks off a hammer rising on hot air, white-hot and cooling to embers (the anvil).</summary>
	Sparks,
	/// <summary>Written leaves blown in on a cold draught, one on each voice of the call (the nine mouths).</summary>
	Writ,
	/// <summary>Birds lifting out of the canopy on each fall of a great foot (the walking hut).</summary>
	Birds,
	/// <summary>Sea mist rolling in low, and a barge hung shimmering above the water among it (the barge).</summary>
	Mist,
	/// <summary>Pale threads of vapour drifting in low, against the wind, on each tap of a flask and each cough (the gibbet's cage).</summary>
	Vapour,
	/// <summary>Stars twinkling in, one on each line of a verse read aloud (the seer's voice in the Court).</summary>
	Stars,
	/// <summary>A thin stream of sand lifting into the air against the wind, on each knock of stone on stone (the dry tomb).</summary>
	Sand,
	/// <summary>A thin cold herb-smoke lying low over the stubble, a wisp on each phrase of her voice (the granary).</summary>
	Smoke,
	/// <summary>A line of wet paint drawing itself, dab after dab, on each stroke of a brush, and a drip off the last (the dotted door in the Court).</summary>
	Paint,
}

/// <summary>One side event's hint: what it is for, how it sounds, what it shows, and what its line says.</summary>
public sealed class StageHint
{
	/// <summary>
	/// The side event's site it is about, and its key (<see cref="StageHintCatalog.ForSite"/>): the event
	/// standing there (SideEventDirector.EventsFor) is where it comes from, and its chapter is the one whose
	/// runs play it. It plays only while something the site gives (<see cref="UnlockCatalog.ForDiscoverySite"/>)
	/// is still unowned on this save. Keyed by site, not by chapter, because a chapter may hold two events
	/// (.ai/chapter-wizards.md 6.3), and each has its own.
	/// </summary>
	public string SiteId { get; init; } = string.Empty;

	public HintVoice Voice { get; init; }

	/// <summary>The cue, by <see cref="SfxCatalog"/> name. Generated in tools/audio/sfx.py.</summary>
	public string Sound { get; init; } = string.Empty;

	/// <summary>
	/// What comes in from the edge of the screen with the sound, on its beat (StageHintCue). In the
	/// site's own language and its far tell's colour: Eric, 2026-11-01, "for the singing wizard, once in
	/// a while, the player should see musical notes on screen in the direction of the wizard".
	/// </summary>
	public HintCue Cue { get; init; }

	/// <summary>
	/// One line per hint, in order; once they run out the last one repeats. Short enough for two
	/// lines on a phone (<see cref="StageHintCatalog.MaxLineLength"/>). Speech without its quotation
	/// marks - the caption adds them.
	/// </summary>
	public string[] Lines { get; init; } = Array.Empty<string>();
}

/// <summary>
/// The single authority on chapter hints (.ai/side-events.md, "Hints that something is out there").
/// </summary>
/// <remarks>
/// <para>Reported: "give hints part way through the runs of the unlockables on maps, such as a woman
/// yelling for help on the cursed dungeon map so the player identify that there are secrets to find".
/// Every chapter with a side event hides it four to six chunks out and announces nothing until the
/// player arrives (SideEvent), which is right for the hunt and wrong for a player who does not know
/// there is anything to hunt. So, a third of the way into a run and every two and a half minutes
/// after, while the chapter still holds something this save has not carried out, a cue comes from
/// its direction: a sound panned that way, a few marks in the site's own language drifting in from
/// that edge of the screen on the sound's beat (notes for the singer, leaves for the grove), and the
/// line fading in beside them.</para>
///
/// <para>It says that something is there and roughly where. It never says what to do about it, and
/// it never names who: a rescued wizard's name is the reward (side-events.md, "Secret wizards").
/// It stops for good once the place is found - seen, or revealed by the chapter's wayfinder - since
/// from then on the point-of-interest arrow says the same thing better, and it never plays once the
/// discovery is owned, during the boss, or in the last pace minute and a half before it.</para>
///
/// <para>To add an event's: one entry below, keyed by the site its side event stands on and pays out, a
/// sound (tools/audio/sfx.py, then <see cref="SfxCatalog"/>), and its lines. A chapter with two events has
/// two entries, one a site, and StageHintDirector alternates them, one a slot.
/// <c>RegressionChecks.ValidateStageHints</c> checks every one of those against the catalogs that
/// own them, and <c>ValidateSideEventTable</c> that every event has one. A hint about something that is not
/// a side event (a stage secret, once they exist) needs StageHintDirector taught where that thing is;
/// nothing else changes.</para>
/// </remarks>
public static class StageHintCatalog
{
	// When, on the pace clock (Node2DGame.PaceSeconds) - positions on the reference 15-minute run, so
	// a shorter run hears them proportionally earlier. A third of the way in, when the build has its
	// shape and a detour is affordable; then every two and a half minutes; and none from 13:30 on,
	// when there is no longer time to walk there and back before the boss.
	public const float FirstPaceSeconds = 300f;
	public const float RepeatPaceSeconds = 150f;
	public const float LastPaceSeconds = 810f;

	/// <summary>The longest a line may be: two lines of the Body role in the caption on a phone.</summary>
	public const int MaxLineLength = 60;

	private static readonly StageHint[] Hints =
	{
		// The grove is not a person, so nothing speaks: the leaves do.
		new()
		{
			SiteId = "the_grove",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintGroveWhisper,
			Cue = HintCue.Leaves,
			Lines = new[]
			{
				"Leaves whispering, though there is no wind.",
				"The whisper again. Something out there still grows.",
				"Fainter now, the whispering. It has not stopped.",
			},
		},
		// Tituba, in a cell. Taken from Barbados and jailed on an accusation (side-events.md): a
		// prisoner who has been patient for a long time, not a witch. Her name is never said.
		new()
		{
			SiteId = "gaolers_key",
			Voice = HintVoice.Speech,
			Sound = SfxCatalog.HintCellCry,
			Cue = HintCue.Echo,
			Lines = new[]
			{
				"Help! Is someone there?",
				"Please - I can hear you. I am down here!",
				"I have been patient so long. Please, this way.",
				"Do not leave me here. Not again.",
			},
		},
		// His lanterns. "Nothing living lights them" - so the caption never says who tends them.
		new()
		{
			SiteId = "bog_lanterns",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintLanternFlare,
			Cue = HintCue.Wisp,
			Lines = new[]
			{
				"Far off, a cold light flares and gutters.",
				"The cold light again. Nothing living lit it.",
				"It is still burning out there.",
			},
		},
		// Abe no Seimei, sealed in his own five-pointed seal. A master of paper wards: his wards blow in on
		// the wind, burning at the edges, in a hand nobody here can read. Not named, never said what they
		// are for.
		new()
		{
			SiteId = "five_pointed_seal",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintPaperWind,
			Cue = HintCue.Paper,
			Lines = new[]
			{
				"Strips of paper on the wind, in a hand you do not know.",
				"More paper, burning at the edges as it falls.",
				"Still the paper comes, from the same quarter.",
			},
		},
		// Wayland the Smith, chained to his own anvil in the Emberdeep (.ai/chapter-wizards.md 4.7). He is heard
		// working - a tired hammer on an anvil, below - and the sparks off it come in on the forge's hot air.
		// Not named; never said what the hammer is, or that anyone is held.
		new()
		{
			SiteId = "great_anvil",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintAnvil,
			Cue = HintCue.Sparks,
			Lines = new[]
			{
				"Hammer on anvil, slow and steady, somewhere below.",
				"The hammer again. Whoever swings it is very tired.",
				"It does not stop. It is not allowed to.",
			},
		},
		// The Cumaean Sibyl, held behind nine mouths in the Sunken Cave (.ai/chapter-wizards.md 4.3). She wrote her
		// prophecies on leaves and let the wind have them: written leaves come in on a cold draught, out of
		// place in a cave, one on each voice of the call, and her voice is heard from far down in more places
		// than one. A sound, not speech - she is not calling for help. Not named, never said what to do.
		new()
		{
			SiteId = "nine_mouths",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintSibylVoice,
			Cue = HintCue.Writ,
			Lines = new[]
			{
				"Leaves on a cold draught, every one written on.",
				"A woman's voice from far down, in more places than one.",
				"The leaves again, written faster now.",
			},
		},
		// Baba Yaga, kept in her own walking hut in the Enchanted Forest (.ai/chapter-wizards.md 4.1). Nobody calls: the
		// hut is heard walking - two heavy falls and its timbers creaking - and the birds it puts up lift out of the
		// canopy on her side. Not named; never said what walks, or that anyone is in it.
		new()
		{
			SiteId = "walking_hut",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintHutStep,
			Cue = HintCue.Birds,
			Lines = new[]
			{
				"Something heavy walks the deep wood, on two legs.",
				"The steps again. Too heavy for a man.",
				"Still walking, round and round.",
			},
		},
		// Paracelsus, caged from a gibbet over the rot in the Blighted Swamp (.ai/chapter-wizards.md 4.4). He is heard, not
		// named: a glass flask tapped and a man coughing, out on the bog, and threads of pale vapour off his flask coming in
		// low against the wind on his side - not the lanterns' poison green, which is the other event's far tell. Never said
		// who coughs, or what the flask holds.
		new()
		{
			SiteId = "gibbet_cage",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintFlaskChime,
			Cue = HintCue.Vapour,
			Lines = new[]
			{
				"A man coughing, far out on the bog.",
				"Glass rings across the water, like a tapped flask.",
				"The coughing again. Weaker.",
			},
		},
		// Nostradamus, kept in the Court to read what is coming (.ai/chapter-wizards.md 4.10). He is heard READING,
		// and what he reads is the player: lines written for the game in the shape of his quatrains, never his own
		// words. Speech, as Tituba's is - a person's voice - but he does not call for help. Stars come in on each line
		// of the verse from his side. Not named; never said what to do, or that anyone is held.
		new()
		{
			SiteId = "seers_voice",
			Voice = HintVoice.Speech,
			Sound = SfxCatalog.HintQuatrain,
			Cue = HintCue.Stars,
			Lines = new[]
			{
				"Out of the west one light, on foot, alone.",
				"The one he missed walks every road he made.",
				"When the last door opens, the house goes dark.",
			},
		},
		// Medea, counted with the grain in the Tithe-Reeve's granary in the Fallow Fields (.ai/chapter-wizards.md 4.2). She
		// speaks, as Tituba does, but she does not plead: the granddaughter of the Sun, who has waited out worse men than
		// the reeve. A thin cold herb-smoke drifts low over the stubble from her side. Never named, never said who.
		new()
		{
			SiteId = "tithe_scale",
			Voice = HintVoice.Speech,
			Sound = SfxCatalog.HintGranaryVoice,
			Cue = HintCue.Smoke,
			Lines = new[]
			{
				"You there. The granary, past the furrows.",
				"He counts me with the grain. Settle his count.",
				"Come, or go. I have waited out worse men.",
			},
		},
		// Ma Liang, kept in the Court's Gallery to paint its portraits (.ai/chapter-wizards.md 4.8). A brush is heard drawing
		// along a wall and a line of wet paint draws itself in from his side, dab after dab, a drip running off its end.
		// Not a voice: he is never heard, only his brush. Not named; never said what to do, or that anyone is held.
		new()
		{
			SiteId = "dotted_door",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintBrush,
			Cue = HintCue.Paint,
			Lines = new[]
			{
				"A brushstroke drawing itself along the wall.",
				"Another line, wet, running the same way.",
				"The paint is still running.",
			},
		},
		// Vainamoinen, the singer, under the ice. He is heard, not named.
		new()
		{
			SiteId = "the_thaw",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintIceSong,
			Cue = HintCue.Song,
			Lines = new[]
			{
				"The ice groans. Under it, faintly, someone is singing.",
				"The song again, slow and deep, from under the ice.",
				"The singing is weaker now.",
			},
		},
		// The caravan master on the trade road. The bell walks; it does not wait.
		new()
		{
			SiteId = "caravan_master",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintCaravanBell,
			Cue = HintCue.Bell,
			Lines = new[]
			{
				"A caravan bell, somewhere out on the sand.",
				"The bell again. Something walks the old road.",
				"The bell is still moving. It does not stop.",
			},
		},
		// Morgan le Fay, in her barge moored under the Last Bridge (.ai/chapter-wizards.md 4.9). Mist rolls over the
		// parapet from her side and her barge hangs shimmering above the water there - the fata morgana, her name
		// on the strait - and a wooden hull knocks against the stone of a pier far below. A sound, not speech: she
		// does not call out. Not named, never said what to do.
		new()
		{
			SiteId = "the_barge",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintHullKnock,
			Cue = HintCue.Mist,
			Lines = new[]
			{
				"A boat on the water below, where no boat could be.",
				"The boat again, nearer the bridge.",
				"Its lantern is still lit.",
			},
		},
		// Imhotep, sealed in a dry tomb on the Scorched Sands (.ai/chapter-wizards.md 4.6). He is heard knocking - slow,
		// stone on stone, under the hiss of sand - and on each knock a thin stream of sand lifts into the air on his
		// side, against the wind. Not named; never said who knocks, or what would open it.
		new()
		{
			SiteId = "tomb_beacons",
			Voice = HintVoice.Sound,
			Sound = SfxCatalog.HintTombKnock,
			Cue = HintCue.Sand,
			Lines = new[]
			{
				"Far off in the dunes, a slow knocking, stone on stone.",
				"The knocking again, from under the sand.",
				"Slower now. It has not stopped.",
			},
		},
	};

	/// <summary>Every hint written - the real ones; a tool run's stand-in is answered by <see cref="ForSite"/> alone.</summary>
	public static IReadOnlyList<StageHint> All => Hints;

	/// <summary>
	/// The hint about the side event standing on <paramref name="siteId"/>, or null if it has none. On a tool
	/// run with the stand-in second event (<see cref="SecondEventStandIn"/>), its hint too.
	/// </summary>
	public static StageHint ForSite(string siteId)
	{
		if (string.IsNullOrWhiteSpace(siteId))
			return null;
		foreach (StageHint hint in Hints)
		{
			if (string.Equals(hint.SiteId, siteId, StringComparison.OrdinalIgnoreCase))
				return hint;
		}
		return SecondEventStandIn.Active && SecondEventStandIn.IsStandInSite(siteId) ? SecondEventStandIn.Hint : null;
	}

	/// <summary>The hints of a chapter's events, in the chapter's order (SideEventDirector.EventsFor); the ones without one skipped.</summary>
	public static List<StageHint> ForStage(string stageId)
	{
		var found = new List<StageHint>();
		foreach (SideEventEntry entry in global::SideEventDirector.EventsFor(stageId))
		{
			if (ForSite(entry.SiteId) is StageHint hint)
				found.Add(hint);
		}
		return found;
	}

	/// <summary>
	/// The side events written before a hint was asked of every one (.ai/campaign.md, the early chapters'): the
	/// one exception to ValidateSideEventTable's rule. A site leaves this list when its hint is written, and
	/// the validator says so if it is not taken off. No new event may be added here.
	/// </summary>
	public static readonly IReadOnlyList<string> SitesWithoutHints = new[] { "the_well", "the_moot" };

	/// <summary>The pace second at which hint number <paramref name="index"/> (0-based) falls due.</summary>
	public static float DuePaceSeconds(int index) => FirstPaceSeconds + Math.Max(0, index) * RepeatPaceSeconds;

	/// <summary>The line for hint number <paramref name="index"/>: in order, the last repeating.</summary>
	public static string LineFor(StageHint hint, int index)
	{
		if (hint == null || hint.Lines.Length == 0)
			return string.Empty;
		return hint.Lines[Math.Clamp(index, 0, hint.Lines.Length - 1)];
	}
}
