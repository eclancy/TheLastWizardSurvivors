using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// The walking hut (.ai/chapter-wizards.md 4.1): what holds the hunt to its sentence, which no build sees and a single
// run would only show once. Every line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	private static void ValidateWalkingHut(List<string> warnings)
	{
		// The radii nest: it startles inside the ring the bar fills in (so there is a band to walk in), and is lost
		// only well outside it. It bolts quicker than his walk and strides slower, as the design says.
		if (!(WalkingHutEvent.StartleRadius < WalkingHutEvent.KeepRadius && WalkingHutEvent.KeepRadius < WalkingHutEvent.LoseRadius))
			warnings.Add($"Walking hut: the radii must nest, startle {WalkingHutEvent.StartleRadius} < keep {WalkingHutEvent.KeepRadius} < lose {WalkingHutEvent.LoseRadius}.");
		if (WalkingHutEvent.BoltShare <= 1f || WalkingHutEvent.TiredBoltShare <= 1f || WalkingHutEvent.StrideShare >= 1f || WalkingHutEvent.BoltSeconds <= 0f)
			warnings.Add($"Walking hut: a bolt must be quicker than his walk ({WalkingHutEvent.BoltShare}) and the stride after it slower ({WalkingHutEvent.StrideShare}).");

		// Chased on paper on open ground at the base walk, two ways: straight at it (the naive chase, which startles it
		// every time it can) and holding in the band between the radii (the chase that reads it). Both must fill the
		// bar in a run's reasonable stretch; the naive one must have startled it more than once.
		foreach (bool reads in new[] { false, true })
		{
			var mover = NewChaseMover(_ => false);
			Vector2 him = mover.Position + new Vector2(0f, 600f);
			float bar = 0f, t = 0f;
			const float Dt = 1f / 60f;
			while (bar < WalkingHutEvent.HuntSeconds && t < 240f)
			{
				mover.Step(Dt, him, 220f);
				Vector2 to = mover.Position - him;
				float d = to.Length();
				float want = reads ? (WalkingHutEvent.StartleRadius + WalkingHutEvent.KeepRadius) * 0.5f : 0f;
				if (d > want + 2f)
					him += to / d * MathF.Min(220f * Dt, d - want);
				if (d <= WalkingHutEvent.KeepRadius)
					bar += Dt;
				t += Dt;
			}
			if (bar < WalkingHutEvent.HuntSeconds)
				warnings.Add($"Walking hut: a {(reads ? "band-holding" : "straight")} chase on open ground never filled the bar ({bar:0.0} of {WalkingHutEvent.HuntSeconds} s in {t:0} s).");
			else if (t > 120f)
				warnings.Add($"Walking hut: a {(reads ? "band-holding" : "straight")} chase took {t:0} s to fill a {WalkingHutEvent.HuntSeconds} s bar; it should be well under two minutes.");
			if (!reads && mover.BoltsStarted < 2)
				warnings.Add($"Walking hut: a straight chase startled it {mover.BoltsStarted} time(s); it should bolt again and again.");
		}

		// Water across its way cuts a bolt short, and it never walks into it: a wall of water east of x = 300 and him
		// coming from the west.
		{
			var mover = NewChaseMover(p => p.X > 300f);
			Vector2 him = mover.Position + new Vector2(-380f, 0f);
			float furthest = mover.Position.X;
			for (int i = 0; i < 60 * 8; i++)
			{
				mover.Step(1f / 60f, him, 220f);
				him = him.MoveToward(mover.Position - new Vector2(300f, 0f), 220f / 60f);
				furthest = MathF.Max(furthest, mover.Position.X);
			}
			if (furthest > 300f)
				warnings.Add($"Walking hut: it walked into water (x {furthest:0} past the shore at 300).");
			if (mover.BoltsCut < 1)
				warnings.Add("Walking hut: run at a shore, no bolt was cut short; water across its way must stop it early.");
		}

		// Lost: left alone past the lose radius it calms and stands, which is what makes losing it cost nothing.
		{
			var mover = NewChaseMover(_ => false);
			mover.Step(1f / 60f, mover.Position + new Vector2(0f, 300f), 220f);
			Vector2 far = mover.Position + new Vector2(0f, WalkingHutEvent.LoseRadius + 600f);
			for (int i = 0; i < 60 * (int)(WalkingHutEvent.CalmSeconds + 3f); i++)
				mover.Step(1f / 60f, far, 220f);
			if (mover.Fleeing || mover.Speed > 0f || mover.TimesCalmed != 1)
				warnings.Add($"Walking hut: left {WalkingHutEvent.LoseRadius + 600f:0} px behind, it did not calm and stand (fleeing {mover.Fleeing}, speed {mover.Speed:0}).");
		}

		// Its prints point the way it went: a print's arrow is its heel, opposite its middle toe (walking_hut.py).
		foreach (Vector2 travel in new[] { Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up, new Vector2(1f, 1f).Normalized() })
		{
			int column = WalkingHutEvent.PrintDirection(travel);
			float arrow = Mathf.DegToRad(column * 45f + 180f);
			if (Mathf.Abs(Mathf.AngleDifference(arrow, travel.Angle())) > 0.01f)
				warnings.Add($"Walking hut: a print laid walking {travel} uses column {column}, whose arrow points {Mathf.RadToDeg(arrow) % 360f:0} deg, not the way it went.");
		}

		// The hint's cue is the birds, on the sound's beats (sfx.py _HUT_FALLS and _HUT_CREAK: 0.10, 0.74, 1.02).
		float[] beats = StageHintCue.HutBeatTimes;
		if (beats.Length != 3 || Mathf.Abs(beats[0] - 0.10f) > 0.001f || Mathf.Abs(beats[1] - 0.74f) > 0.001f || Mathf.Abs(beats[2] - 1.02f) > 0.001f)
			warnings.Add($"Walking hut: the hint's birds are on [{string.Join(", ", beats)}], not hint_hut_step's falls and creak (0.10, 0.74, 1.02).");
		if (StageHintCatalog.ForSite("walking_hut") is not StageHint hint || hint.Cue != HintCue.Birds || hint.Sound != SfxCatalog.HintHutStep)
			warnings.Add("Walking hut: its hint must be hint_hut_step with the birds cue.");

		// The art, at the sizes the event reads it at (tools/art/walking_hut.py).
		CheckSheet(warnings, "res://assets/bonelight/world/hut/hut.png", new Vector2(96 * 14, 128), "the hut's fourteen 96x128 frames");
		CheckSheet(warnings, "res://assets/bonelight/world/hut/hut-prints.png", new Vector2(16 * 8, 16 * 2), "its prints, eight ways fresh and old");
	}

	private static FleeingMover NewChaseMover(Func<Vector2, bool> blocked) => new()
	{
		StartleRadius = WalkingHutEvent.StartleRadius,
		BoltSeconds = WalkingHutEvent.BoltSeconds,
		BoltShare = WalkingHutEvent.BoltShare,
		TiredBoltShare = WalkingHutEvent.TiredBoltShare,
		StrideShare = WalkingHutEvent.StrideShare,
		LoseRadius = WalkingHutEvent.LoseRadius,
		CalmSeconds = WalkingHutEvent.CalmSeconds,
		CircleRadius = WalkingHutEvent.CircleRadius,
		Home = Vector2.Zero,
		Position = new Vector2(WalkingHutEvent.CircleRadius, 0f),
		IsBlocked = blocked,
	};

	private static void CheckSheet(List<string> warnings, string path, Vector2 size, string what)
	{
		Texture2D texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		if (texture == null)
			warnings.Add($"Walking hut: {path} ({what}) is missing or not imported.");
		else if (texture.GetSize() != size)
			warnings.Add($"Walking hut: {path} is {texture.GetSize()}, expected {size} ({what}).");
	}
}
