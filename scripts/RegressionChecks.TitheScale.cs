using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// Medea (.ai/chapter-wizards.md 4.2): the Tithe-Reeve's scale and Dragon's Teeth - what holds the price and the turn to
// their sentences, which no build sees and a single run would show once. Every line is a warning, as the rest of
// RunAll's are.
public static partial class RegressionChecks
{
	private static void ValidateTitheScale(List<string> warnings)
	{
		IReadOnlyList<SideEventEntry> fields = SideEventDirector.TableEventsFor("stage_9");
		if (fields.Count != 2 || fields[0].SiteId != "the_well" || fields[1].SiteId != "tithe_scale"
			|| SideEventDirector.HoldsSite("stage_10", "tithe_scale"))
			warnings.Add("Tithe scale: Medea must remain the Fields' second event, after the well.");

		// The beam (Medea's balance, unlock-puzzles.md 3.5): frame by frame the LEFT pan rises and the right falls, the
		// level frame has them at one height, and every bowl stays inside the scale's cell (tools/art/tithe_scale.py
		// draws it from the same numbers).
		float lastLeft = float.MaxValue, lastRight = float.MinValue;
		for (int f = 0; f < TitheScaleEvent.ScaleFrames; f++)
		{
			for (int side = -1; side <= 1; side += 2)
			{
				Vector2 bowl = TitheScaleEvent.PanAuthored(f, side);
				if (bowl.X < 8f || bowl.X > 56f || bowl.Y < 4f || bowl.Y > 74f)
					warnings.Add($"Tithe scale: frame {f}'s {(side < 0 ? "left" : "right")} pan ({bowl}) is outside the scale's 64x80 cell, or too near its edge for the bowl.");
			}
			float l = TitheScaleEvent.PanAuthored(f, -1).Y, r = TitheScaleEvent.PanAuthored(f, 1).Y;
			if (l >= lastLeft || r <= lastRight)
				warnings.Add($"Tithe scale: frame {f} does not tip further toward the right than frame {f - 1}; the frames must run left-heavy to right-heavy.");
			lastLeft = l;
			lastRight = r;
			if (f == TitheScaleEvent.LevelFrame && Mathf.Abs(l - r) > 0.01f)
				warnings.Add($"Tithe scale: the level frame ({f}) hangs its pans at {l:0.0} and {r:0.0}; level means level.");
		}

		// The beam reads the rule: inside the band is the level frame, each side of it tips toward the heavier pan, and
		// the band is the same LevelBand the hold is counted against (FrameForDifference is shared by both).
		if (TitheScaleEvent.FrameForDifference(0f) != TitheScaleEvent.LevelFrame
			|| TitheScaleEvent.FrameForDifference(TitheScaleEvent.LevelBand * 0.99f) != TitheScaleEvent.LevelFrame
			|| TitheScaleEvent.FrameForDifference(TitheScaleEvent.LevelBand * 1.5f) >= TitheScaleEvent.LevelFrame
			|| TitheScaleEvent.FrameForDifference(-TitheScaleEvent.LevelBand * 1.5f) <= TitheScaleEvent.LevelFrame
			|| TitheScaleEvent.FrameForDifference(5f) != 0 || TitheScaleEvent.FrameForDifference(-5f) != TitheScaleEvent.ScaleFrames - 1)
			warnings.Add("Tithe scale: the beam's frame must be level inside the band and tip toward the heavier pan outside it, to the end frames.");
		if (TitheScaleEvent.LevelBand <= 0f || TitheScaleEvent.LevelBand >= 0.5f || TitheScaleEvent.HoldSeconds <= 0f
			|| TitheScaleEvent.CapInLevels <= TitheScaleEvent.PriceInLevels * (1f + TitheScaleEvent.LevelBand))
			warnings.Add($"Tithe scale: the balance's numbers must leave room to play - band {TitheScaleEvent.LevelBand}, hold {TitheScaleEvent.HoldSeconds} s, "
				+ $"cap {TitheScaleEvent.CapInLevels} levels above a price of {TitheScaleEvent.PriceInLevels} plus the band.");

		// The tithe on paper, through the same static the orbs ask: an orb inside the reach is the reeve's and one
		// outside it is not, and goes to the pan on ITS side of the post; what reaches a pan is weighed there; past the
		// cap nothing more is claimed; once closed, nothing is claimed or weighed. Left closed.
		bool wasOpen = GranaryTithe.Open;
		if (!wasOpen)
		{
			Vector2 foot = new(1000f, 1000f);
			GranaryTithe.OpenAt(foot, foot + new Vector2(-48f, -82f), foot + new Vector2(48f, -82f), TitheScaleEvent.TitheReach, 30f);
			if (!GranaryTithe.Claims(foot + new Vector2(TitheScaleEvent.TitheReach - 1f, 0f))
				|| GranaryTithe.Claims(foot + new Vector2(0f, TitheScaleEvent.TitheReach + 1f)))
				warnings.Add($"Tithe scale: an orb must be the reeve's inside {TitheScaleEvent.TitheReach} px of the scale and his own outside it.");
			if (GranaryTithe.SideOf(foot + new Vector2(-1f, 200f)) != -1 || GranaryTithe.SideOf(foot + new Vector2(1f, -200f)) != 1)
				warnings.Add("Tithe scale: an orb landing left of the post must be bound for the left pan, and right for the right.");
			GranaryTithe.Pay(3.2f, 2, -1);
			GranaryTithe.Pay(16f, 10, 1);
			if (Math.Abs(GranaryTithe.TakenLeft - 3.2f) > 0.001f || Math.Abs(GranaryTithe.TakenRight - 16f) > 0.001f
				|| Math.Abs(GranaryTithe.Taken - 19.2f) > 0.001f || GranaryTithe.OrbsTaken != 2 || GranaryTithe.ValueTaken != 12)
				warnings.Add($"Tithe scale: the pans weighed {GranaryTithe.TakenLeft} and {GranaryTithe.TakenRight} in {GranaryTithe.OrbsTaken} orbs "
					+ $"({GranaryTithe.ValueTaken} XP), expected 3.2 and 16 in 2 (12 XP).");
			// The stream (in-flight 61): a reported flight is seen the same frame, bound for its pan.
			GranaryTithe.ReportFlight(foot, -1);
			if (GranaryTithe.Flights != 1 || GranaryTithe.FlightPosition(0) != foot || GranaryTithe.FlightSide(0) != -1)
				warnings.Add("Tithe scale: an orb reporting its flight must be seen by its pan's stream (GranaryTithe.Flights).");
			// The cap: past it the reeve claims nothing more.
			GranaryTithe.Pay(12f, 6, -1);
			if (!GranaryTithe.Capped || GranaryTithe.Claims(foot))
				warnings.Add($"Tithe scale: past the cap ({GranaryTithe.Taken:0.0} of 30) the reeve must take nothing more.");
			float before = GranaryTithe.Taken;
			GranaryTithe.Close();
			GranaryTithe.Pay(5f, 2, 1);
			GranaryTithe.ReportFlight(foot, 1);
			if (GranaryTithe.Claims(foot) || Math.Abs(GranaryTithe.Taken - before) > 0.001f || GranaryTithe.Flights != 0)
				warnings.Add("Tithe scale: a closed scale must claim nothing, weigh nothing and stream nothing.");
		}
		if (TitheScaleEvent.PriceInLevels <= 0f)
			warnings.Add($"Tithe scale: the price must be a share of a level greater than none (is {TitheScaleEvent.PriceInLevels}).");

		// The clue's beats are the sound's (sfx._GRANARY_SYLLABLES): six syllables, in order, the first at once.
		float[] beats = StageHintCue.GranaryVoiceBeats;
		if (beats.Length != 6 || beats[0] != 0f || beats.Zip(beats.Skip(1), (a, b) => b > a).Any(up => !up))
			warnings.Add($"Tithe scale: the granary hint's beats must be the voice's six syllables in order from 0 (are {string.Join(", ", beats)}).");

		CheckTitheSheet(warnings, "res://assets/bonelight/world/granary/granary.png", new Vector2(96 * TitheScaleEvent.GranaryFrames, 96), "the granary, barred, ajar and open");
		CheckTitheSheet(warnings, "res://assets/bonelight/world/granary/scale.png", new Vector2(64 * TitheScaleEvent.ScaleFrames, 80), "the scale, left-heavy through level to right-heavy");

		// DRAGON'S TEETH: the turn's numbers keep the sentence - three seconds, a stone turns no more than the arena
		// holds, a body strikes only what it can reach, and a blow is a real share of a body but never all of it.
		if (Math.Abs(Enemy.TurnSeconds - 3f) > 0.001f)
			warnings.Add($"Dragon's Teeth: a body is turned for three seconds (4.2), not {Enemy.TurnSeconds}.");
		if (ElementalPulse.TeethTurnPerStone > Enemy.MaxTurnedAtOnce || ElementalPulse.TeethTurnPerStone <= 0)
			warnings.Add($"Dragon's Teeth: a stone turns {ElementalPulse.TeethTurnPerStone}, but the arena holds {Enemy.MaxTurnedAtOnce} turned at once.");
		if (Enemy.TurnedStrikeReach >= Enemy.TurnedSeekRadius || Enemy.TurnedBlowShare <= 0f || Enemy.TurnedBlowShare >= 1f
			|| Enemy.TurnedBlowSeconds <= 0f)
			warnings.Add("Dragon's Teeth: a turned body must look further than it strikes, and land a blow that is a share of a body, on a cadence.");
		ApotheosisDefinition teeth = ApotheosisCatalog.ForForm(ElementalPulse.DragonsTeethId);
		if (teeth == null || teeth.CharacterId != "medea" || teeth.SpellId != "solar_flare" || teeth.Verb != "turn")
			warnings.Add("Dragon's Teeth: the form must be Medea's, on Solar Flare, with the verb turn (ApotheosisCatalog).");
	}

	private static void CheckTitheSheet(List<string> warnings, string path, Vector2 size, string what)
	{
		Texture2D texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		if (texture == null)
			warnings.Add($"Tithe scale: {path} ({what}) is missing or not imported.");
		else if (texture.GetSize() != size)
			warnings.Add($"Tithe scale: {path} is {texture.GetSize()}, expected {size} ({what}).");
	}
}
