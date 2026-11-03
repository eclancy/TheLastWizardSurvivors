using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// The gibbet's cage (.ai/chapter-wizards.md 4.4): what holds the escort to its sentence, which no build sees and a single
// run would only show once. Every line is a warning, as the rest of RunAll's are.
public static partial class RegressionChecks
{
	private static void ValidateGibbetCage(List<string> warnings)
	{
		// The numbers keep their shape: he walks slower than the player, the bite is a touch well inside the ring the
		// swarm hunts him from, the landing is the walk the design names.
		if (!(GibbetCageEvent.FollowShare > 0f && GibbetCageEvent.FollowShare < 1f))
			warnings.Add($"Gibbet cage: he must walk slower than the player ({GibbetCageEvent.FollowShare}).");
		if (!(GibbetCageEvent.BiteRadius < HuntedEscort.HuntRadius * 0.5f))
			warnings.Add($"Gibbet cage: a bite ({GibbetCageEvent.BiteRadius} px) must be a touch, well inside the hunt ({HuntedEscort.HuntRadius} px).");
		if (!(GibbetCageEvent.LandingMin >= 1200f && GibbetCageEvent.LandingMax <= 2000f && GibbetCageEvent.LandingMin < GibbetCageEvent.LandingMax))
			warnings.Add($"Gibbet cage: the landing should be about 1,600 px off ({GibbetCageEvent.LandingMin}-{GibbetCageEvent.LandingMax}).");
		if (GibbetCageEvent.HealthMax <= 0 || GibbetCageEvent.BiteSeconds <= 0f || GibbetCageEvent.WinchSeconds <= 0f)
			warnings.Add("Gibbet cage: his bar, the bite and the winch all need to be more than nothing.");

		// Who goes for him: a plain chaser does; nothing with an attack of its own aimed at the player does; no boss ever.
		if (!Enemy.ClassHuntsEscort(typeof(Enemy)))
			warnings.Add("Gibbet cage: a plain chaser (Enemy) must go for the man being led.");
		foreach (Type type in typeof(Enemy).Assembly.GetTypes().Where(t => typeof(Enemy).IsAssignableFrom(t) && t != typeof(Enemy) && !t.IsAbstract))
		{
			bool boss = typeof(BossEnemy).IsAssignableFrom(type);
			bool behaviour = type.GetMethod("AdjustSteering", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
				null, new[] { typeof(Vector2), typeof(float) }, null)?.DeclaringType != typeof(Enemy);
			if ((boss || behaviour) && Enemy.ClassHuntsEscort(type))
				warnings.Add($"Gibbet cage: {type.Name} would turn on the man being led; {(boss ? "a boss never retargets" : "its own attack is aimed at the player")}.");
		}

		// He walks the player's steps, on paper. The player goes east, round a lake that fills the square x 300-700,
		// y -200-200 (down its south shore at y 260), and on east; the straight line from start to end crosses the lake.
		// He must keep to the walked path the whole way (never inside the lake), never outpace his share, and end close
		// behind the player once the player stops.
		{
			var him = new HuntedEscort { SpeedShare = GibbetCageEvent.FollowShare, FollowGap = 64f };
			him.Reset(new Vector2(-80f, 0f));
			Vector2[] path = { new(0f, 0f), new(200f, 0f), new(260f, 260f), new(740f, 260f), new(800f, 0f), new(1200f, 0f) };
			const float Dt = 1f / 60f, Walk = 220f;
			Vector2 leader = path[0];
			int leg = 1;
			float worst = 0f, fastest = 0f, t = 0f;
			bool inLake = false;
			while (t < 30f)
			{
				if (leg < path.Length)
				{
					leader = leader.MoveToward(path[leg], Walk * Dt);
					if (leader.DistanceTo(path[leg]) < 0.01f)
						leg++;
				}
				Vector2 before = him.Position;
				him.Step(Dt, leader, Walk);
				fastest = MathF.Max(fastest, before.DistanceTo(him.Position) / Dt);
				Vector2 p = him.Position;
				if (p.X > 300f && p.X < 700f && p.Y > -200f && p.Y < 200f)
					inLake = true;
				float off = float.MaxValue;
				for (int i = 0; i + 1 < path.Length; i++)
					off = MathF.Min(off, DistanceToSegment(p, path[i], path[i + 1]));
				if (p.X > 0f)
					worst = MathF.Max(worst, off);
				t += Dt;
			}
			if (inLake)
				warnings.Add("Gibbet cage: led round a lake, he walked into it - he must keep to the player's steps.");
			if (worst > HuntedEscort.CrumbSpacing + 1f)
				warnings.Add($"Gibbet cage: led round a lake, he strayed {worst:0} px off the path the player walked.");
			if (fastest > GibbetCageEvent.FollowShare * Walk * 1.01f)
				warnings.Add($"Gibbet cage: he walked {fastest:0} px/s, faster than {GibbetCageEvent.FollowShare:P0} of the player's {Walk}.");
			float gap = him.Position.DistanceTo(leader);
			if (gap > 64f + HuntedEscort.CrumbSpacing + 1f || gap < 40f)
				warnings.Add($"Gibbet cage: once the player stopped he ended {gap:0} px from him, not close behind.");
		}

		// The hint's cue is the vapour, on the sound's beats (sfx.py _FLASK_TAPS and _FLASK_COUGHS: 0.10, 0.34, 0.86, 1.10, 1.66).
		float[] beats = StageHintCue.FlaskBeatTimes;
		float[] sound = { 0.10f, 0.34f, 0.86f, 1.10f, 1.66f };
		if (beats.Length != sound.Length || beats.Zip(sound, (a, b) => MathF.Abs(a - b)).Any(d => d > 0.001f))
			warnings.Add($"Gibbet cage: the hint's vapour is on [{string.Join(", ", beats)}], not hint_flask_chime's taps and coughs.");
		if (StageHintCatalog.ForSite("gibbet_cage") is not StageHint hint || hint.Cue != HintCue.Vapour || hint.Sound != SfxCatalog.HintFlaskChime)
			warnings.Add("Gibbet cage: its hint must be hint_flask_chime with the vapour cue.");

		// The art, at the sizes the event reads it at (tools/art/gibbet.py).
		CheckGibbetSheet(warnings, "gibbet.png", new Vector2(96 * 4, 120), "the gibbet's four 96x120 frames");
		CheckGibbetSheet(warnings, "gibbet-cage.png", new Vector2(32 * 4, 54), "the cage's four 32x54 frames");
		CheckGibbetSheet(warnings, "gibbet-pool.png", new Vector2(84 * 3, 40), "the pool's three 84x40 frames");
		CheckGibbetSheet(warnings, "landing.png", new Vector2(120, 84), "the landing, 120x84");
		CheckGibbetSheet(warnings, "landing-punt.png", new Vector2(54 * 2, 56), "the punt's two 54x56 frames");
	}

	private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float t = ab.LengthSquared() > 0f ? Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0f, 1f) : 0f;
		return p.DistanceTo(a + ab * t);
	}

	private static void CheckGibbetSheet(List<string> warnings, string name, Vector2 size, string what)
	{
		string path = "res://assets/bonelight/world/gibbet/" + name;
		Texture2D texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		if (texture == null)
			warnings.Add($"Gibbet cage: {path} ({what}) is missing or not imported.");
		else if (texture.GetSize() != size)
			warnings.Add($"Gibbet cage: {path} is {texture.GetSize()}, expected {size} ({what}).");
	}
}
