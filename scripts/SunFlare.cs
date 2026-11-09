using Godot;
using System;

namespace TheOneHeMissed.scripts;

// The Eye of Ra's flare (ScorchingRayBeam.FlareEyeOfRa, .ai/chapter-wizards.md 4.6): the sun breaking out where the
// cast's hottest ray lands, at exactly the reach it blinds - and the sun-spot every blinded body wears for its two
// seconds.
//
// UPGRADED 2026-11-09 (Eric: "lets upgrade imhotep's flare to look more interesting"). It was one node a flare drawing a
// stroked ring, a translucent wash and twelve antialiased spokes: faint on a dark floor, a ring and a disc of the kind
// .ai/art-direction.md section 3 calls area markers, and a blinded body showed nothing but a pale tint. Now:
//   - THE FLARE is pixel art through scenes/shaders/sun_flare.gdshader: a white-hot sun's disc at the strike, the light
//     thrown out to land on exactly the blind radius and break up (heat shimmer along its front), god-rays turning a
//     little as they fade (more of them from a hotter ray), and the eye of Ra flashing gold at the heart for a beat;
//     just over the air, so the ray's own splash does not bury it. One quad a flare through one shared material, a slot each in a fixed array (FireburstLayer's pattern),
//     so nothing is instantiated or allocated per flare and a screen of them is one batch.
//   - THE LIGHT: in a dark chapter each flare lights the room out to what it blinds, a flash dying with it (LightPool).
//   - THE TELL: every blinded body wears a small gold glint over its head for as long as it is blind, flickering on its
//     own beat and going out by halves in its last moments (BlindGlints). One drawer for the whole swarm, reading
//     Enemy.IsBlinded off EnemyRegistry.Snapshot while any blind can still be running; no node per body, nothing
//     allocated per frame, and nothing at all once the last blind is over.
// Seared elites are not drawn here: ScorchingRayBeam gives each a small burst of the sun's fire (FireburstLayer's Sun
// style), so a seared body reads as burnt, not as blinded.
//
// On the air layer, in the spell group (Player.TagSpell), so a boss's arrival freezes it with every other spell.
public partial class SunFlare : Node2D
{
	public const string ShaderPath = "res://scenes/shaders/sun_flare.gdshader";
	/// <summary>A flare's whole life: the light lands on its radius a third of the way in and is gone by the end.</summary>
	// Half a second: a flare comes once a cast, about every 1.1-1.6 s, and at 0.6 it was on screen over half the time.
	public const float Seconds = 0.5f;
	private const int MaxFlares = 16;
	// The radius rides in a colour channel as radius / RadiusScale, so it must stay under 1 (ScorchingRayBeam caps it at 380).
	private const float RadiusScale = 1024f;

	private struct Flare
	{
		public Vector2 At;
		public float Radius;
		public float Heat;
		public float Age;
		public float Seed;
		public bool Live;
	}

	private readonly Flare[] flares = new Flare[MaxFlares];
	private int next;
	private int live;
	private uint seedClock;
	private BlindGlints glints;

	private static SunFlare current;
	private static ShaderMaterial material;
	private static Texture2D white;

	/// <summary>
	/// A flare at <paramref name="globalAt"/> reaching exactly <paramref name="radius"/> - the reach it blinds - from a
	/// ray <paramref name="heat"/> hot (0..1), on the layer of <paramref name="arena"/> (made there the first time).
	/// </summary>
	public static void Spawn(Node2D arena, Vector2 globalAt, float radius, float heat)
	{
		if (arena == null || !IsInstanceValid(arena))
			return;
		if (current == null || !IsInstanceValid(current) || !current.IsInsideTree() || current.GetParent() != arena)
		{
			current = new SunFlare { Name = "SunFlares" };
			arena.AddChild(Player.TagSpell(current));
		}
		current.Add(globalAt, radius, heat);
	}

	private static ShaderMaterial SharedMaterial
	{
		get
		{
			if (material != null && IsInstanceValid(material))
				return material;
			var shader = GD.Load<Shader>(ShaderPath);
			if (shader == null)
				GD.PrintErr($"SunFlare: {ShaderPath} did not load; the Eye of Ra's flares will draw as plain quads.");
			material = new ShaderMaterial { Shader = shader };
			return material;
		}
	}

	// A one-pixel texture, so each quad has a UV running 0..1 across it (FireburstLayer's). Never sampled.
	private static Texture2D White
	{
		get
		{
			if (white != null && IsInstanceValid(white))
				return white;
			Image image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
			image.SetPixel(0, 0, Colors.White);
			white = ImageTexture.CreateFromImage(image);
			return white;
		}
	}

	public override void _Ready()
	{
		// Just over the air (DrawLayers.Air + 1, as a launched body is): over the bodies the light falls on and over the
		// ray's own splash, which otherwise buried the eye at its heart; under the wizard and a boss (Foremost).
		ZIndex = DrawLayers.Air + 1;
		ZAsRelative = false;
		Material = SharedMaterial;
		glints = new BlindGlints { Name = "BlindGlints" };
		AddChild(glints);
		SetProcess(false);
	}

	public override void _ExitTree()
	{
		if (current == this)
			current = null;
	}

	private void Add(Vector2 globalAt, float radius, float heat)
	{
		// The oldest slot is reused when every one is live: one flare more or less on a screen that busy is not seen.
		if (!flares[next].Live)
			live++;
		seedClock++;
		flares[next] = new Flare
		{
			At = globalAt,
			Radius = Mathf.Min(radius, RadiusScale - 1f),
			Heat = Mathf.Clamp(heat, 0f, 1f),
			Seed = (seedClock * 0.6180339f) % 1f,
			Live = true,
		};
		next = (next + 1) % MaxFlares;
		glints?.Wake(ScorchingRayBeam.BlindSeconds + 0.1f);
		SetProcess(true);
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		for (int i = 0; i < MaxFlares; i++)
		{
			if (!flares[i].Live)
				continue;
			flares[i].Age += dt;
			if (flares[i].Age >= Seconds)
			{
				flares[i].Live = false;
				live--;
				continue;
			}
			// The sun lights the room round it in a dark chapter, out to what it blinds, a flash dying with the light.
			float fade = 1f - flares[i].Age / Seconds;
			LightPool.Light(flares[i].At, flares[i].Radius, fade * fade);
		}
		QueueRedraw();
		if (live <= 0)
		{
			live = 0;
			SetProcess(false);
		}
	}

	public override void _Draw()
	{
		Texture2D tex = White;
		for (int i = 0; i < MaxFlares; i++)
		{
			Flare f = flares[i];
			if (!f.Live || f.Radius < 12f)
				continue;
			// On an even world pixel, so the flare's art grid is the world's.
			Vector2 c = ToLocal(f.At);
			c = new Vector2(Mathf.Round(c.X * 0.5f) * 2f, Mathf.Round(c.Y * 0.5f) * 2f);
			var parameters = new Color(Mathf.Clamp(f.Age / Seconds, 0f, 1f), f.Radius / RadiusScale, f.Seed, f.Heat);
			DrawTextureRect(tex, new Rect2(c - new Vector2(f.Radius, f.Radius), new Vector2(f.Radius * 2f, f.Radius * 2f)), false, parameters);
		}
	}

	/// <summary>
	/// The sun-spot a blinded body wears over its head for its blind (Enemy.IsBlinded): a four-armed glint of light in
	/// square cells on the 2 px grid, white-hot at its heart and gold at its tips, its arms flickering on the body's own
	/// beat and turning to the diagonals and back; in the last of the blind it shrinks to its heart and blinks out.
	/// One drawer for every blinded body (no material - the flare's shader is the parent's alone).
	/// </summary>
	private partial class BlindGlints : Node2D
	{
		private const float Grid = WhipLimb.Grid;
		// Over the head, by this much of world space: clear of the status marks' over-the-head row's middle.
		private const float Lift = 10f;
		// The last of a blind, in seconds: the glint shrinks to its heart and blinks.
		private const float Fading = 0.4f;
		private static readonly Color Core = new(1f, 1f, 1f);           // light core #FFFFFF
		private static readonly Color Hot = new(1f, 0.973f, 0.847f);    // light hot #FFF8D8
		private static readonly Color Mid = new(1f, 0.941f, 0.659f);    // light mid #FFF0A8
		private static readonly Color Edge = new(1f, 0.776f, 0.235f);   // light edge #FFC63C
		private static readonly Color Shade = new(0.329f, 0.247f, 0.063f); // gold shade #543F10

		private float awake;
		private float clock;

		public void Wake(float seconds)
		{
			awake = MathF.Max(awake, seconds);
			SetProcess(true);
		}

		public override void _Ready()
		{
			SetProcess(false);
		}

		public override void _Process(double delta)
		{
			clock += (float)delta;
			awake -= (float)delta;
			QueueRedraw();
			if (awake <= 0f)
			{
				awake = 0f;
				SetProcess(false);
			}
		}

		public override void _Draw()
		{
			if (awake <= 0f)
				return;
			foreach (Enemy e in EnemyRegistry.Snapshot)
			{
				if (!IsInstanceValid(e) || e.IsDying || !e.IsBlinded)
					continue;
				Transform2D xf = e.GlobalTransform;
				float scale = xf.X.Length();
				Vector2 head = xf.Origin + new Vector2(0f, e.StatusFxGeometry.HeadY * scale - Lift);
				Vector2 c = ToLocal(head);
				c = new Vector2(Mathf.Round(c.X / Grid) * Grid, Mathf.Round(c.Y / Grid) * Grid);
				ulong id = e.GetInstanceId();
				float left = e.BlindSecondsLeft;
				int beat = (int)(clock * 9f + (id % 7));
				if (left < Fading)
				{
					// Going out: its heart alone, blinking.
					if (beat % 2 == 0)
						Stamp(c, Grid * 2f, Edge);
					continue;
				}
				bool diagonal = (beat / 3) % 2 == 1;
				int arm = 3 + (int)((Hash(beat, (int)(id % 977)) * 2f));
				// A dark-gold square under the heart, so it holds on a pale floor and over bone.
				Stamp(c, Grid * 4f, Shade);
				Stamp(c, Grid * 2f, Core);
				for (int k = 1; k <= arm; k++)
				{
					Color col = k == 1 ? Hot : k < arm ? Mid : Edge;
					float d = Grid * (k + 0.5f);
					if (diagonal)
					{
						float g = d * 0.75f;
						Stamp(c + new Vector2(g, g), Grid, col);
						Stamp(c + new Vector2(-g, g), Grid, col);
						Stamp(c + new Vector2(g, -g), Grid, col);
						Stamp(c + new Vector2(-g, -g), Grid, col);
					}
					else
					{
						Stamp(c + new Vector2(d, 0f), Grid, col);
						Stamp(c + new Vector2(-d, 0f), Grid, col);
						Stamp(c + new Vector2(0f, d), Grid, col);
						Stamp(c + new Vector2(0f, -d), Grid, col);
					}
				}
			}
		}

		private void Stamp(Vector2 centre, float size, Color colour) => DrawRect(WhipLimb.Cell(centre, size), colour);

		private static float Hash(int i, int salt)
		{
			uint h = (uint)i * 374761393u + (uint)salt * 668265263u;
			h = (h ^ (h >> 13)) * 1274126177u;
			h ^= h >> 16;
			return (h & 0xFFFFFF) / 16777216f;
		}
	}
}
