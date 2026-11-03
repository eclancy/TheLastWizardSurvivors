using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// --light-probe: the light pool's readability guardrail, MEASURED rather than judged (.ai/light-pool.md,
// "Readability, measured"). A third of the way through the warmup the wizard is held still and silent
// (no spell lights anything), the field is cleared, and the chapter's own enemies are stood, held, in four
// places - in the pool, at the middle of the screen's short edges, at the middle of its long edges and in
// its corners - with XP orbs between them. Once they have settled the frame is photographed twice, the
// tree paused between: as it is, then with every placed body and orb hidden. The pixels that differ are
// the body's; the second frame is the ground it stands on. Printed per zone as LIGHT lines:
//
//   L* body / ground  CIE lightness of the body's pixels and of the ground under them (0 black, 100 white)
//   dL*               how far the body stands out from its ground - the readability number
//   hi dL*            the same for its brightest tenth: the rim, the eyes, the steel
//   eye L*            its eye-light pixels (blue, saturated): the cold mark every creature carries
//   x body / room     the shader's multiplier there from the wizard's pool (LightPool.MultiplierAt)
//
// Run it with and without --no-light-pool on the same --terrain-seed for the before and the after.
// --light-probe-low-health: the same, the wizard pinned at a fifth of his health, so the shutter frame shows
// the low-health warning (Node2DGame.UpdateLowHealthWarning) over the dark.
public partial class _UiShot
{
	private bool lightProbe;
	private bool lightProbeStarted;
	private bool lightProbeLowHealth;

	private void StartLightProbe()
	{
		if (FindNodeOfType<Node2DGame>(GetTree().Root) is not Node2DGame game
			|| FindNodeOfType<Player>(GetTree().Root) is not Player hero)
			return;
		lightProbeStarted = true;
		GetTree().Root.AddChild(new LightProbe { Name = "LightProbe", Game = game, Hero = hero, LowHealth = lightProbeLowHealth });
	}

	private partial class LightProbe : Node
	{
		public Node2DGame Game;
		public Player Hero;
		public bool LowHealth;

		private const int SettleFrames = 45;

		private readonly struct Placed
		{
			public Placed(Node2D node, string zone, bool orb)
			{
				Node = node;
				Zone = zone;
				Orb = orb;
			}

			public readonly Node2D Node;
			public readonly string Zone;
			public readonly bool Orb;
		}

		private readonly List<Placed> placed = new();
		private readonly HashSet<ulong> keep = new();
		private int frame;
		private int phase;
		private Image shotA;

		public override void _Ready()
		{
			ProcessMode = ProcessModeEnum.Always;
			RenderingServer.FramePostDraw += OnPostDraw;
			Hero.SetHeldForBossEntrance(true);
			Place();
		}

		public override void _ExitTree() => RenderingServer.FramePostDraw -= OnPostDraw;

		public override void _Process(double delta)
		{
			if (!IsInstanceValid(Hero))
				return;
			Hero.MaxHP = Math.Max(Hero.MaxHP, 100000);
			Hero.CurrentHP = LowHealth ? Hero.MaxHP / 5 : Hero.MaxHP;
			if (phase >= 3)
				return;
			// Silent as well as still: no spell of his lights anything while the probe looks.
			Hero.SetHeldForBossEntrance(true);
			Hero.CurrentXP = 0;
			foreach (Enemy e in EnemyRegistry.Snapshot)
				if (IsInstanceValid(e) && !keep.Contains(e.GetInstanceId()))
					e.QueueFree();
			frame++;
		}

		// Stood relative to the wizard, inset from the edges of what the camera sees, so a phone's tall
		// canvas puts its long edges at the top and bottom and a PC's at the sides.
		private void Place()
		{
			Viewport viewport = Hero.GetViewport();
			Camera2D camera = viewport.GetCamera2D();
			Vector2 zoom = camera != null ? camera.Zoom : Vector2.One;
			Vector2 half = viewport.GetVisibleRect().Size / zoom * 0.5f;
			Vector2 c = Hero.GlobalPosition;
			const float inset = 72f;
			bool wide = half.X >= half.Y;
			// Unit offsets along the short and the long axis.
			Vector2 shortAxis = wide ? Vector2.Down : Vector2.Right;
			Vector2 longAxis = wide ? Vector2.Right : Vector2.Down;
			float shortReach = Mathf.Min(half.X, half.Y) - inset;
			float longReach = Mathf.Max(half.X, half.Y) - inset;

			for (int k = 0; k < 4; k++)
				Body(c + Vector2.FromAngle(Mathf.Pi * 0.25f + k * Mathf.Pi * 0.5f) * 150f, "pool");
			foreach (float side in new[] { -1f, 1f })
			{
				foreach (float along in new[] { -0.3f, 0f, 0.3f })
				{
					Body(c + shortAxis * side * shortReach + longAxis * along * longReach, "short-edge");
					Body(c + longAxis * side * longReach + shortAxis * along * shortReach, "long-edge");
				}
				Orb(c + shortAxis * side * shortReach + longAxis * 0.15f * longReach, "short-edge");
				Orb(c + longAxis * side * longReach + shortAxis * 0.15f * shortReach, "long-edge");
				foreach (float other in new[] { -1f, 1f })
					Body(c + shortAxis * side * shortReach + longAxis * other * longReach, "corner");
			}
			for (int k = 0; k < 4; k++)
				Orb(c + Vector2.FromAngle(k * Mathf.Pi * 0.5f) * 120f, "pool");
			GD.Print($"LIGHT probe: {placed.Count} placed round {c} (half view {half.X:0}x{half.Y:0} world), "
				+ (LightPool.Current is LightPool pool
					? $"pool on: inner {pool.InnerRadius:0} outer {pool.OuterRadius:0} strength {pool.Strength:0.00}"
					: "pool off"));
		}

		private void Body(Vector2 at, string zone)
		{
			Enemy e = Game.SpawnEventEnemy((string)null, at, 40f, false);
			if (e == null)
				return;
			e.SetHeld(true);
			keep.Add(e.GetInstanceId());
			placed.Add(new Placed(e, zone, false));
		}

		private void Orb(Vector2 at, string zone)
		{
			if (GD.Load<PackedScene>("res://scenes/XPOrb.tscn")?.Instantiate() is not XPOrb orb)
				return;
			orb.Value = 2;
			orb.Position = at;
			Game.AddChild(orb);
			placed.Add(new Placed(orb, zone, true));
		}

		private void OnPostDraw()
		{
			if (phase == 0 && frame >= SettleFrames)
			{
				shotA = GetViewport().GetTexture().GetImage();
				foreach (Placed p in placed)
					if (IsInstanceValid(p.Node))
						p.Node.Visible = false;
				GetTree().Paused = true;
				phase = 1;
			}
			else if (phase == 1)
			{
				Image shotB = GetViewport().GetTexture().GetImage();
				foreach (Placed p in placed)
					if (IsInstanceValid(p.Node))
						p.Node.Visible = true;
				GetTree().Paused = false;
				phase = 2;
				Measure(shotA, shotB);
				phase = 3;
			}
		}

		private sealed class Tally
		{
			public int Count;
			public double Body, Ground, Hi, Eye, BodyX, RoomX;
			public int EyeCount;
		}

		private void Measure(Image a, Image b)
		{
			a.Convert(Image.Format.Rgba8);
			b.Convert(Image.Format.Rgba8);
			int w = a.GetWidth(), h = a.GetHeight();
			byte[] pa = a.GetData(), pb = b.GetData();
			Viewport viewport = GetViewport();
			Vector2 toPixels = new Vector2(w, h) / viewport.GetVisibleRect().Size;
			Transform2D canvas = viewport.GetCanvasTransform();
			Camera2D camera = viewport.GetCamera2D();
			float zoom = camera != null ? camera.Zoom.X : 1f;
			LightPool pool = LightPool.Current;
			var zones = new SortedDictionary<string, Tally>();
			var bodyL = new List<float>();
			foreach (Placed p in placed)
			{
				if (!IsInstanceValid(p.Node))
					continue;
				Vector2 at = p.Node.GlobalPosition;
				// A body's sprite stands round and above its origin; an orb is small.
				Vector2 lo = p.Orb ? at + new Vector2(-16f, -20f) : at + new Vector2(-44f, -84f);
				Vector2 hi = p.Orb ? at + new Vector2(16f, 16f) : at + new Vector2(44f, 30f);
				Vector2 plo = canvas * lo * toPixels, phi = canvas * hi * toPixels;
				int x0 = Math.Max(0, (int)plo.X), y0 = Math.Max(0, (int)plo.Y);
				int x1 = Math.Min(w - 1, (int)phi.X), y1 = Math.Min(h - 1, (int)phi.Y);
				double groundSum = 0;
				int groundN = 0, eyeN = 0;
				double eyeSum = 0;
				bodyL.Clear();
				for (int y = y0; y <= y1; y++)
				{
					for (int x = x0; x <= x1; x++)
					{
						int i = (y * w + x) * 4;
						float gl = Lightness(pb[i], pb[i + 1], pb[i + 2]);
						groundSum += gl;
						groundN++;
						int d = Math.Max(Math.Abs(pa[i] - pb[i]), Math.Max(Math.Abs(pa[i + 1] - pb[i + 1]), Math.Abs(pa[i + 2] - pb[i + 2])));
						if (d <= 6)
							continue;
						float l = Lightness(pa[i], pa[i + 1], pa[i + 2]);
						bodyL.Add(l);
						float r = pa[i] / 255f, g = pa[i + 1] / 255f, bl = pa[i + 2] / 255f;
						float max = Math.Max(r, Math.Max(g, bl)), min = Math.Min(r, Math.Min(g, bl));
						if (bl >= max - 0.001f && bl - r > 0.12f && max > 0.05f && (max - min) / max > 0.35f)
						{
							eyeSum += l;
							eyeN++;
						}
					}
				}
				if (bodyL.Count < 6 || groundN == 0)
					continue;
				bodyL.Sort();
				double mean = 0;
				foreach (float l in bodyL)
					mean += l;
				mean /= bodyL.Count;
				int top = Math.Max(1, bodyL.Count / 10);
				double hiMean = 0;
				for (int k = bodyL.Count - top; k < bodyL.Count; k++)
					hiMean += bodyL[k];
				hiMean /= top;
				string key = (p.Orb ? "orb  " : "body ") + p.Zone;
				if (!zones.TryGetValue(key, out Tally t))
					zones[key] = t = new Tally();
				t.Count++;
				t.Body += mean;
				t.Ground += groundSum / groundN;
				t.Hi += hiMean;
				if (eyeN > 0)
				{
					t.Eye += eyeSum / eyeN;
					t.EyeCount++;
				}
				if (pool != null)
				{
					t.BodyX += pool.MultiplierAt(at, true);
					t.RoomX += pool.MultiplierAt(at, false);
				}
			}
			GD.Print($"LIGHT held {Hero.HeldForBossEntrance}, fireballs in flight {CountFireballs(Game)}");
			GD.Print($"LIGHT measured at {w}x{h}, zoom {zoom:0.00}, pool {(pool != null ? $"on (strength {pool.Strength:0.00})" : "off")}:");
			foreach (KeyValuePair<string, Tally> z in zones)
			{
				Tally t = z.Value;
				double body = t.Body / t.Count, ground = t.Ground / t.Count, hi = t.Hi / t.Count;
				string eye = t.EyeCount > 0 ? $"{t.Eye / t.EyeCount:0.0}" : "-";
				string mult = pool != null ? $" | x body {t.BodyX / t.Count:0.00} room {t.RoomX / t.Count:0.00}" : string.Empty;
				GD.Print($"LIGHT {z.Key,-16} n={t.Count,2} | L* body {body:0.0} ground {ground:0.0} | dL* {body - ground:0.0} | hi dL* {hi - ground:0.0} | eye L* {eye}{mult}");
			}
		}

		private static int CountFireballs(Node n)
		{
			int k = n is Fireball ? 1 : 0;
			foreach (Node c in n.GetChildren())
				k += CountFireballs(c);
			return k;
		}

		// CIE L* of an sRGB pixel: 0 black, 100 white, and a step of one is about one just-noticeable step.
		private static float Lightness(byte r, byte g, byte b)
		{
			double y = 0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(b);
			double f = y > 0.008856 ? Math.Cbrt(y) : (7.787 * y + 16.0 / 116.0);
			return (float)(116.0 * f - 16.0);
		}

		private static double Linear(byte c)
		{
			double v = c / 255.0;
			return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
		}
	}
}
