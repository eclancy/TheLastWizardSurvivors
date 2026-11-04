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
//   lo dL*            its darkest tenth: a dark silhouette reads by how far UNDER its ground it goes
//
// Then the world itself, peeled (the foliage pass, .ai/terrain.md section 13): HUD hidden, then the ground
// detail, then the bushes, then the trees and rocks, one frame each, and a LIGHT layer line for each of
// the three measured the same way against what lay under it - so the guardrail "clutter stays lower in
// contrast than the enemies and the XP orbs" (.ai/genre-lessons.md 2 and 5) is a number set beside the
// body and orb lines of the same frame; then a LIGHT floor line, the bare floor's own pattern (MeasureFloor).
// The first frame and the world without the HUD are saved beside the shot as
// <shot>-probe-bodies.png and <shot>-probe-world.png. After a --teleport it waits for the ground there to
// stream in, so it can measure anywhere, not only at the start.
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
		GetTree().Root.AddChild(new LightProbe { Name = "LightProbe", Game = game, Hero = hero, LowHealth = lightProbeLowHealth, ShotName = ShotName });
	}

	private partial class LightProbe : Node
	{
		public Node2DGame Game;
		public Player Hero;
		public bool LowHealth;
		public string ShotName;

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
			if (phase > PeelLast)
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

		// THE PEEL. After the bodies are photographed and hidden (A, B), the world is photographed four
		// more times, one layer hidden before each: the HUD (C, the world alone), the ground detail's
		// TileMapLayers (D), the bushes - the grounded sprites with no trunk - (E), and the rest of the decor:
		// trees, rocks, themed props (F). The pixels that change between two frames are that layer's, and
		// the later frame is what lies under them, so each layer is measured exactly as a body is: its own
		// L* against the ground under it (MeasureLayer). Added for the foliage pass (.ai/genre-lessons.md 2
		// and 5): "clutter stays lower in contrast than enemies and XP gems", in numbers.
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
				return;
			}
			if (phase < 1 || phase > PeelLast)
				return;
			peel.Add(GetViewport().GetTexture().GetImage());
			if (phase == 1)
				Hide(GetTree().Root, n => n is CanvasLayer layer && layer.Layer > 0);
			else if (phase == 2)
				Hide(GetTree().Root, n => n is TileMapLayer layer && layer.Name == DetailLayerName);
			else if (phase == 3)
				Hide(GetTree().Root, n => n is Sprite2D && n.IsInGroup(DecorGroup));
			else if (phase == 4)
				Hide(GetTree().Root, n => n is CollisionObject2D && n.IsInGroup(DecorGroup));
			if (phase < PeelLast)
			{
				phase++;
				return;
			}
			foreach (CanvasItem item in hidden)
				if (IsInstanceValid(item))
					item.Visible = true;
			foreach (Placed p in placed)
				if (IsInstanceValid(p.Node))
					p.Node.Visible = true;
			GetTree().Paused = false;
			Measure(shotA, peel[0]);
			GD.Print("LIGHT layers (HUD hidden): cover = share of the frame; dL* = its L* minus the ground under it; "
				+ "hi / lo = its brightest / darkest tenth minus that ground");
			MeasureLayer("detail", peel[1], peel[2]);
			MeasureLayer("bushes", peel[2], peel[3]);
			MeasureLayer("trees+rocks", peel[3], peel[4]);
			MeasureFloor(peel[4]);
			SaveProbeFrames(shotA, peel[1]);
			phase = PeelLast + 1;
		}

		private const int PeelLast = 5;
		private const string DetailLayerName = "Detail";
		private const string DecorGroup = "decor_props";
		private readonly List<Image> peel = new();
		private readonly List<CanvasItem> hidden = new();

		private void Hide(Node n, Func<Node, bool> match)
		{
			if (n is CanvasItem item && item.Visible && match(n))
			{
				item.Visible = false;
				hidden.Add(item);
				return;
			}
			if (n is CanvasLayer layer && layer.Visible && match(n))
			{
				layer.Visible = false;
				hiddenLayers.Add(layer);
				return;
			}
			foreach (Node c in n.GetChildren())
				Hide(c, match);
		}

		private readonly List<CanvasLayer> hiddenLayers = new();

		private void SaveProbeFrames(Image a, Image world)
		{
			foreach (CanvasLayer layer in hiddenLayers)
				if (IsInstanceValid(layer))
					layer.Visible = true;
			string name = string.IsNullOrEmpty(ShotName) ? "light-probe" : ShotName;
			a.SavePng($"user://{name}-probe-bodies.png");
			world.SavePng($"user://{name}-probe-world.png");
			GD.Print($"LIGHT frames saved: {ProjectSettings.GlobalizePath($"user://{name}-probe-bodies.png")} and -probe-world.png");
		}

		// The bare floor - the ground tiles and their overlays, everything standing on them peeled away - as
		// texture: each 24 px block's median L* is the floor there, and the pixels more than 4 L* off it are
		// its pattern (a moss cushion, a speck in the turf). Printed: how much of the frame they cover and how
		// far off they sit, the busyness a body is read against. The moss under the Forest's stands is busier
		// than its turf, and this is the number that says by how much.
		private static void MeasureFloor(Image img)
		{
			img.Convert(Image.Format.Rgba8);
			int w = img.GetWidth(), h = img.GetHeight();
			byte[] px = img.GetData();
			const int block = 24;
			var lum = new float[block * block];
			var sorted = new float[block * block];
			double groundSum = 0, offSum = 0;
			int blocks = 0, off = 0, pixels = 0;
			for (int by = 0; by + block <= h; by += block)
			{
				for (int bx = 0; bx + block <= w; bx += block)
				{
					int k = 0;
					for (int y = by; y < by + block; y++)
						for (int x = bx; x < bx + block; x++)
						{
							int i = (y * w + x) * 4;
							lum[k++] = Lightness(px[i], px[i + 1], px[i + 2]);
						}
					Array.Copy(lum, sorted, lum.Length);
					Array.Sort(sorted);
					float median = sorted[sorted.Length / 2];
					groundSum += median;
					blocks++;
					foreach (float l in lum)
					{
						pixels++;
						float d = Math.Abs(l - median);
						if (d > 4f)
						{
							off++;
							offSum += d;
						}
					}
				}
			}
			if (blocks == 0)
				return;
			GD.Print($"LIGHT floor (everything on it peeled) | L* {groundSum / blocks:0.0} | pattern cover {100.0 * off / pixels:0.0}% "
				+ $"| pattern |dL*| {(off > 0 ? offSum / off : 0):0.0}");
		}

		// One layer of the peel: the pixels that differ between the frame with it and the frame without
		// it are its own; the second frame at those pixels is the ground it lies on.
		private static void MeasureLayer(string name, Image with, Image without)
		{
			with.Convert(Image.Format.Rgba8);
			without.Convert(Image.Format.Rgba8);
			int w = with.GetWidth(), h = with.GetHeight();
			byte[] pa = with.GetData(), pb = without.GetData();
			var own = new List<float>();
			double under = 0, absSum = 0;
			for (int i = 0; i < w * h * 4; i += 4)
			{
				int d = Math.Max(Math.Abs(pa[i] - pb[i]), Math.Max(Math.Abs(pa[i + 1] - pb[i + 1]), Math.Abs(pa[i + 2] - pb[i + 2])));
				if (d <= 6)
					continue;
				float l = Lightness(pa[i], pa[i + 1], pa[i + 2]);
				float g = Lightness(pb[i], pb[i + 1], pb[i + 2]);
				own.Add(l);
				under += g;
				absSum += Math.Abs(l - g);
			}
			if (own.Count == 0)
			{
				GD.Print($"LIGHT layer {name,-12} | cover 0.0%");
				return;
			}
			under /= own.Count;
			own.Sort();
			double mean = 0;
			foreach (float l in own)
				mean += l;
			mean /= own.Count;
			int tenth = Math.Max(1, own.Count / 10);
			double hi = 0, lo = 0;
			for (int k = 0; k < tenth; k++)
			{
				lo += own[k];
				hi += own[own.Count - 1 - k];
			}
			hi /= tenth;
			lo /= tenth;
			GD.Print($"LIGHT layer {name,-12} | cover {100.0 * own.Count / (w * h):0.0}% | L* own {mean:0.0} ground {under:0.0} "
				+ $"| dL* {mean - under:0.0} | |dL*| {absSum / own.Count:0.0} | hi dL* {hi - under:0.0} | lo dL* {lo - under:0.0}");
		}

		private sealed class Tally
		{
			public int Count;
			public double Body, Ground, Hi, Lo, Eye, BodyX, RoomX;
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
				double loMean = 0;
				for (int k = 0; k < top; k++)
					loMean += bodyL[k];
				loMean /= top;
				string key = (p.Orb ? "orb  " : "body ") + p.Zone;
				if (!zones.TryGetValue(key, out Tally t))
					zones[key] = t = new Tally();
				t.Count++;
				t.Body += mean;
				t.Ground += groundSum / groundN;
				t.Hi += hiMean;
				t.Lo += loMean;
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
				GD.Print($"LIGHT {z.Key,-16} n={t.Count,2} | L* body {body:0.0} ground {ground:0.0} | dL* {body - ground:0.0} | hi dL* {hi - ground:0.0} | eye L* {eye}{mult} | lo dL* {t.Lo / t.Count - ground:0.0}");
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
