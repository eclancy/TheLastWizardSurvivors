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
		// The radii nest, and it bolts quicker than his walk and strides slower, as the design says.
		if (!(WalkingHutEvent.StartleRadius < WalkingHutEvent.LoseRadius))
			warnings.Add($"Walking hut: the radii must nest, startle {WalkingHutEvent.StartleRadius} < lose {WalkingHutEvent.LoseRadius}.");
		if (WalkingHutEvent.BoltShare <= 1f || WalkingHutEvent.TiredBoltShare <= 1f || WalkingHutEvent.StrideShare >= 1f || WalkingHutEvent.BoltSeconds <= 0f)
			warnings.Add($"Walking hut: a bolt must be quicker than his walk ({WalkingHutEvent.BoltShare}) and the stride after it slower ({WalkingHutEvent.StrideShare}).");
		if (WalkingHutEvent.YardGapWidth < 100f || WalkingHutEvent.CorneredRadius <= 0f || WalkingHutEvent.CorneredRadius >= WalkingHutEvent.YardRadius)
			warnings.Add($"Walking hut: the yard must admit the hut (gap {WalkingHutEvent.YardGapWidth}) and corner it inside the ring (cornered {WalkingHutEvent.CorneredRadius} of {WalkingHutEvent.YardRadius}).");

		// Herded on paper (unlock-puzzles.md 3.3): her yard 750 px from the circle, its one gap facing away.
		// A herding chase - press from the far side of the hut, the gap's mouth first, then the middle - must
		// drive it in in under two minutes on open ground, and a straight chase must never get it in: running
		// straight at it just moves it, and the fence has no way in on the side it is pushed from.
		{
			Vector2 yard = new(0f, 750f);
			float gapAngle = Mathf.Pi / 2f; // facing away from the circle at the origin
			Vector2 mouth = yard + Vector2.FromAngle(gapAngle) * (WalkingHutEvent.YardRadius + 70f);
			foreach (bool herds in new[] { true, false })
			{
				var mover = NewChaseMover(_ => false);
				// The fence as the event tests it: the step's segment against the ring, every way but the gap.
				FleeingMover theMover = mover;
				mover.IsBlocked = at => CrossesPaperFence(theMover.Position, at, yard, gapAngle);
				Vector2 him = mover.Position + new Vector2(0f, -600f);
				float t = 0f;
				const float Dt = 1f / 60f;
				bool driven = false;
				while (!driven && t < 180f)
				{
					mover.Tire = Mathf.Clamp(1f - (mover.Position.DistanceTo(yard) - WalkingHutEvent.CorneredRadius)
						/ (750f - WalkingHutEvent.CorneredRadius), 0f, 1f);
					mover.Step(Dt, him, 220f);
					Vector2 hut = mover.Position;
					Vector2 press = herds
						? hut + (hut - (hut.DistanceTo(mouth) <= 150f || hut.DistanceTo(yard) <= WalkingHutEvent.YardRadius
							? yard : mouth)).Normalized() * (WalkingHutEvent.StartleRadius - 60f)
						: hut;
					him += (press - him).LimitLength(220f * Dt);
					driven = hut.DistanceTo(yard) <= WalkingHutEvent.CorneredRadius;
					t += Dt;
				}
				if (herds && !driven)
					warnings.Add($"Walking hut: a herding chase on open ground never drove it into the yard ({t:0} s; it stood {NewChaseMover(_ => false).Position.DistanceTo(yard):0} px out at the start).");
				else if (herds && t > 120f)
					warnings.Add($"Walking hut: a herding chase took {t:0} s to drive it in; it should be well under two minutes.");
				if (!herds && driven)
					warnings.Add($"Walking hut: a STRAIGHT chase drove it into the yard in {t:0} s; the gap faces away, so only herding should.");
				if (herds && driven && mover.BoltsStarted < 2)
					warnings.Add($"Walking hut: the herding drive startled it {mover.BoltsStarted} time(s); it should bolt again and again.");
			}
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

		// Its toes point the way it walked (Eric, 2026-11-05: toes-back read as backwards tracks however
		// it ran): a print's middle toe is its column's own angle (walking_hut.py).
		foreach (Vector2 travel in new[] { Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up, new Vector2(1f, 1f).Normalized() })
		{
			int column = WalkingHutEvent.PrintDirection(travel);
			float toe = Mathf.DegToRad(column * 45f);
			if (Mathf.Abs(Mathf.AngleDifference(toe, travel.Angle())) > 0.01f)
				warnings.Add($"Walking hut: a print laid walking {travel} uses column {column}, whose middle toe points {Mathf.RadToDeg(toe) % 360f:0} deg, not the way it walked.");
		}

		// The hint's cue is the birds, on the sound's beats (sfx.py _HUT_FALLS and _HUT_CREAK: 0.10, 0.74, 1.02).
		float[] beats = StageHintCue.HutBeatTimes;
		if (beats.Length != 3 || Mathf.Abs(beats[0] - 0.10f) > 0.001f || Mathf.Abs(beats[1] - 0.74f) > 0.001f || Mathf.Abs(beats[2] - 1.02f) > 0.001f)
			warnings.Add($"Walking hut: the hint's birds are on [{string.Join(", ", beats)}], not hint_hut_step's falls and creak (0.10, 0.74, 1.02).");
		if (StageHintCatalog.ForSite("walking_hut") is not StageHint hint || hint.Cue != HintCue.Birds || hint.Sound != SfxCatalog.HintHutStep)
			warnings.Add("Walking hut: its hint must be hint_hut_step with the birds cue.");

		// The art, at the sizes the event reads it at (tools/art/walking_hut.py).
		CheckSheet(warnings, "res://assets/bonelight/world/hut/hut.png", new Vector2(96 * 24, 128), "the hut's twenty-four 96x128 frames");
		CheckSheet(warnings, "res://assets/bonelight/world/hut/hut-prints.png", new Vector2(16 * 8, 16 * 2), "its prints, eight ways fresh and old");
	}

	// The paper fence, with the event's own semantics (WalkingHutEvent.CrossesFence): the step's segment
	// against the yard's ring, blocked everywhere but the gap.
	private static bool CrossesPaperFence(Vector2 from, Vector2 to, Vector2 yard, float gapAngle)
	{
		float a = from.DistanceTo(yard);
		float b = to.DistanceTo(yard);
		if ((a < WalkingHutEvent.YardRadius) == (b < WalkingHutEvent.YardRadius))
			return false;
		float t = Mathf.Clamp((WalkingHutEvent.YardRadius - a) / (b - a), 0f, 1f);
		Vector2 crossing = from.Lerp(to, t);
		float gapHalf = (WalkingHutEvent.YardGapWidth * 0.5f - 8f) / WalkingHutEvent.YardRadius;
		return Mathf.Abs(Mathf.AngleDifference((crossing - yard).Angle(), gapAngle)) > gapHalf;
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
