using Godot;
using System;
using TheOneHeMissed.scripts;

// THE PLAYER IS THE LAMP (.ai/light-pool.md; .ai/genre-lessons.md section 1). Eric, 2026-11-03, after
// studying the genre: a warm pool of light on the wizard that falls off into the dark at the screen's
// edges, with enemies walking out of the dark into it - and then "The light pool can possibly be used in
// certain levels? like the dungeons". So it is PER CHAPTER (StageEnvironmentProfile.Darkness): on under
// ground and indoors, where the player's own light is the only light, and nowhere under open sky.
//
// One shader (scenes/shaders/light_pool.gdshader), one material, one quad the size of what the camera
// sees, drawn by two of these nodes at two depths:
//
//   - THE ROOM (DrawLayers.RoomDark): over the floor, the walls and the decals, under everything that
//     stands or happens on them. The dark proper - deep, down to RoomFloor - with the warm lift at the
//     heart of the pool. Every ground tell, crater, pool, hazard and side-event site sits above it.
//   - THE BODIES (DrawLayers.Bodies, sorted after every body): over the swarm, the chests, the XP orbs
//     and the props, and under every spell, bolt and icon. A gentle dark with a high floor (BodyFloor),
//     so the crowd falls off into the dark without sinking into it.
//
// The two floors are the readability guardrail, and the reason there are two passes and not one. The
// title screen learned it first (.ai/title-screen.md, "The undergrowth is TONED"): a thing only reads as a
// thing if its lit side is lighter than the ground it stands on, and a falloff run over everything alike
// comes out as more of the same dark. Here the room goes three times darker than the bodies on it, so an
// enemy at the screen's edge stands out from the floor more than it does with no dark at all.
//
// WHAT IS NEVER DARKENED, by depth rather than by rule: the wizard and every boss (DrawLayers.Foremost),
// every projectile, blast and enemy bolt (Air), every telegraph icon (AttackIcon), every damage number
// (FloatingText), and the whole HUD (its own canvas layers). A boss's body, its arrival's dust and light
// (ForemostAir) and its ground tells are all above the dark - and while it arrives, the floor it steps onto
// is lifted by a cold light of its own that fades in and out with the cinematic (TickReveals), so a dark
// boss never arrives dark on dark.
//
// WARM SPELLS ARE LIGHTS TOO. Anything warm the player made (fire, light, the ward) calls Light() from
// its own _Process with where it is, how far it reaches and how bright it is; up to MaxLights reach the
// shader each frame, culled to the screen, the weakest dropped when there are more. Nothing is
// registered and nothing is freed: a light is one struct written into a fixed array, read and cleared
// once a frame by the room's node (ProcessPriority high, so after the spells). With the pool off, Light()
// is one static bool read.
//
// Off on every chapter whose profile says Darkness 0; lifted to ReducedDarknessScale by the Reduce
// Darkness switch (DisplaySettings); off entirely with the user argument --no-light-pool, for before and
// after screenshots, and on at --light-pool=X in any chapter, to try one.
// _UiShot --light-probe measures what it does to the swarm's readability (_UiShot.LightPool.cs).
public partial class LightPool : Node2D
{
	public const string ShaderPath = "res://scenes/shaders/light_pool.gdshader";

	/// <summary>The user argument that keeps the pool out of a run, for comparison shots.</summary>
	public const string NoLightPoolArg = "--no-light-pool";

	/// <summary>
	/// --light-pool=X: the pool at darkness X in any chapter, for trying it where the profile says 0 (the
	/// proposals in .ai/light-pool.md were shot with it).
	/// </summary>
	public const string ForceLightPoolArg = "--light-pool=";

	/// <summary>The most small lights the shader reads in a frame (its sixteen light_N uniforms).</summary>
	public const int MaxLights = 16;

	// THE SHAPE, as shares of half the canvas's SHORT side in world units (360 canvas units at the run's
	// zoom, 424 world units at 0.85). Both canvases share that short side and their diagonal (1280x720 is
	// 720x1280 turned), so the pool is the same size in the world on a PC and on a phone, and the dark
	// reaches the same depth into each corner.
	//
	// Fully lit out to InnerShare (about 230 world units: the ring a level-one swarm presses on), the
	// floor reached at OuterShare (about 740: just inside a PC's left and right edges and a phone's top and
	// bottom). Measured on a 1280x720 canvas, the short edges' midpoints keep about two thirds of their
	// light; the long edges and the corners are at the floor.

	/// <summary>Out to here, as a share of half the short side, the light is full.</summary>
	public const float InnerShare = 0.55f;

	/// <summary>From here on, as a share of half the short side, it is the pass's floor.</summary>
	public const float OuterShare = 1.75f;

	/// <summary>How far the warm lift carries, as a share of half the short side.</summary>
	public const float WarmReachShare = 0.75f;

	/// <summary>The room's multiplier in the full dark: the floor, the walls, the decals.</summary>
	public const float RoomFloor = 0.24f;

	/// <summary>
	/// The bodies' multiplier in the full dark. High on purpose: the dark is the room's, and a body at the
	/// edge keeps most of its light, so it reads against a floor that has lost most of its own.
	/// </summary>
	public const float BodyFloor = 0.66f;

	/// <summary>
	/// The warm lift at the heart of the pool, premultiplied: gold's deep stop (#2E2208,
	/// .ai/art-direction.md section 3) at about half its value. A wash of the ward's own pigment on the
	/// floor, never on a body - the bodies pass lifts nothing - so no enemy is tinted by it. Half, because it
	/// lifts the ground the fight stands on: at 0.8 of gold.deep it took the pool's body-to-ground contrast
	/// from 17 L* to 10 (the --light-probe); at half it is 14, and the pool still reads warm.
	/// </summary>
	public static readonly Color Warm = new(0.095f, 0.066f, 0.018f);

	// The quad covers what the camera sees with this much to spare, so a shake or a push-in never shows an edge.
	private const float Margin = 1.3f;
	// The bodies' quad is sorted by where it stands, among the bodies: put below the bottom of the screen by
	// this much, it is drawn after every body that can be seen (a boss's feet off the bottom edge included).
	private const float SortBelow = 4000f;
	// How fast the strength follows the chapter and the setting, per second (a toggle eases rather than snaps).
	private const float StrengthEase = 2.5f;

	private static readonly StringName ParamCentre = "centre";
	private static readonly StringName ParamInner = "inner";
	private static readonly StringName ParamOuter = "outer";
	private static readonly StringName ParamStrength = "strength";
	private static readonly StringName ParamWarm = "warm";
	private static readonly StringName ParamWarmReach = "warm_reach";
	private static readonly StringName ParamLightCount = "light_count";
	private static readonly StringName[] ParamLights =
	{
		"light_0", "light_1", "light_2", "light_3", "light_4", "light_5", "light_6", "light_7",
		"light_8", "light_9", "light_10", "light_11", "light_12", "light_13", "light_14", "light_15",
	};

	// The lights asked for since the last upload, and the rectangle a light must touch to be taken.
	private static LightPool driver;
	private static readonly Vector4[] pending = new Vector4[MaxLights];
	private static int pendingCount;
	private static Vector2 cullMin = new(-1e9f, -1e9f);
	private static Vector2 cullMax = new(1e9f, 1e9f);

	/// <summary>True while a run's light pool is on (its chapter is dark). For the tools and the light sources.</summary>
	public static bool Lit => driver != null;

	/// <summary>The pool in this run, or null where the chapter is lit evenly.</summary>
	public static LightPool Current => driver;

	/// <summary>
	/// A warm light this frame at <paramref name="at"/> (world), reaching <paramref name="radius"/> world
	/// units, at <paramref name="brightness"/> (0..1; fade it with the effect). Call it every frame the
	/// light is to show, from the effect's own _Process. Allocates nothing; a no-op where the chapter is lit.
	/// </summary>
	public static void Light(Vector2 at, float radius, float brightness = 1f) => Push(at, radius, brightness, true);

	/// <summary>
	/// A COLD light: it lifts the dark round <paramref name="at"/> like <see cref="Light"/> but lays no warmth
	/// on the floor - his light, not the wizard's (a boss's arrival). The sign of the shader's w says which.
	/// </summary>
	public static void ColdLight(Vector2 at, float radius, float brightness = 1f) => Push(at, radius, brightness, false);

	/// <summary>The most lights <see cref="LightAlong"/> strings along one line.</summary>
	public const int AlongLights = 4;

	/// <summary>
	/// A warm light along the line <paramref name="a"/> to <paramref name="b"/> this frame: a beam, a wall of
	/// fire, a trail of slag lights the room the whole way, not at one end. Up to <see cref="AlongLights"/>
	/// lights at the middles of equal stretches of it, each reaching <paramref name="reach"/> - or the stretch's
	/// length, on a line too long for that many, so their pools still run together into one band. Allocates
	/// nothing; a no-op where the chapter is lit.
	/// </summary>
	public static void LightAlong(Vector2 a, Vector2 b, float reach, float brightness = 1f)
	{
		if (driver == null || brightness <= 0.02f)
			return;
		float length = a.DistanceTo(b);
		int n = Math.Clamp((int)MathF.Ceiling(length / MathF.Max(1f, reach)), 1, AlongLights);
		float radius = MathF.Max(reach, length / n);
		for (int i = 0; i < n; i++)
			Push(a.Lerp(b, (i + 0.5f) / n), radius, brightness, true);
	}

	private static void Push(Vector2 at, float radius, float brightness, bool warm)
	{
		if (driver == null || brightness <= 0.02f || radius <= 2f)
			return;
		if (at.X + radius < cullMin.X || at.X - radius > cullMax.X || at.Y + radius < cullMin.Y || at.Y - radius > cullMax.Y)
			return;
		float b = Mathf.Min(1f, brightness);
		var light = new Vector4(at.X, at.Y, radius, warm ? b : -b);
		if (pendingCount < MaxLights)
		{
			pending[pendingCount++] = light;
			return;
		}
		// Full: the weakest gives way (reach times brightness), so a fireball outshines an ember.
		int weakest = 0;
		float least = float.MaxValue;
		for (int i = 0; i < MaxLights; i++)
		{
			float weight = pending[i].Z * MathF.Abs(pending[i].W);
			if (weight < least)
			{
				least = weight;
				weakest = i;
			}
		}
		if (radius * b > least)
			pending[weakest] = light;
	}

	// A BOSS NEVER ARRIVES INVISIBLE. Its body is above the dark already (DrawLayers.Foremost), but a dark boss
	// on a dark floor is dark on dark - the Hollow Choir's voices, shot on the Cave, were. So every body that is
	// arriving (BossEnemy.Arriving) carries a cold light of its own: up over RevealInSeconds from the frame its
	// arrival begins - rising through the cinematic's hold, and full by the time the pan has settled on it
	// (Node2DGame.BossEntrancePanDelay, then about a second of spring) - and down over RevealOutSeconds once
	// it has landed, while the camera eases home. Eased both ways (smoothstep), so it never snaps, and cold,
	// so the only warm light on screen stays the wizard's. It adds nothing to the cinematic and moves nothing.

	/// <summary>How far an arriving boss's cold light reaches, in world units.</summary>
	public const float RevealReach = 280f;

	/// <summary>How bright it gets at most: a reveal, not daylight.</summary>
	public const float RevealBrightness = 0.85f;

	/// <summary>Seconds it takes to come up from the arrival's first frame.</summary>
	public const float RevealInSeconds = 1.2f;

	/// <summary>Seconds it takes to go once the boss has landed.</summary>
	public const float RevealOutSeconds = 1.6f;

	private const int MaxReveals = 4;
	private readonly BossEnemy[] revealed = new BossEnemy[MaxReveals];
	private readonly float[] revealLevel = new float[MaxReveals];

	private void TickReveals(float dt)
	{
		foreach (BossEnemy boss in BossEnemy.Arriving)
		{
			if (boss == null || Array.IndexOf(revealed, boss) >= 0)
				continue;
			int free = Array.IndexOf(revealed, null);
			if (free >= 0)
			{
				revealed[free] = boss;
				revealLevel[free] = 0f;
			}
		}
		for (int i = 0; i < MaxReveals; i++)
		{
			BossEnemy boss = revealed[i];
			if (boss == null)
				continue;
			bool live = IsInstanceValid(boss) && boss.IsInsideTree();
			bool arriving = live && boss.IsEntering;
			revealLevel[i] = Mathf.MoveToward(revealLevel[i], arriving ? 1f : 0f,
				dt / (arriving ? RevealInSeconds : RevealOutSeconds));
			if (!live || (!arriving && revealLevel[i] <= 0f))
			{
				revealed[i] = null;
				continue;
			}
			float u = revealLevel[i];
			ColdLight(boss.GlobalPosition, RevealReach, RevealBrightness * u * u * (3f - 2f * u));
		}
	}

	/// <summary>
	/// Whether an element's light is warm - fire and light - and so lights a dark chapter's room. The
	/// premise's own line (.ai/world-and-tone.md): the only warm light on screen is his, and what he made.
	/// </summary>
	public static bool IsWarm(Element element) => element == Element.Fire || element == Element.Light;

	/// <summary>
	/// Whether a spell names fire or light among its elements now, its milestones' trades counted - so
	/// Spiritual Weapon's Mjolnir, which trades the hammer's Light for Lightning, does not. It walks Godot
	/// dictionaries and allocates (SpellData.GetElementWeights): ask once per cast or per node, never per frame.
	/// </summary>
	public static bool CarriesWarm(SpellData spell)
	{
		if (spell == null)
			return false;
		foreach (Element element in spell.GetElementWeights().Keys)
		{
			if (IsWarm(element))
				return true;
		}
		return false;
	}

	// The chapters that are lit under open sky (Eric, 2026-11-03: the light pool is for "certain levels ...
	// like the dungeons").
	private static readonly StageEnvironmentKind[] LitEvenly =
	{
		StageEnvironmentKind.Forest, StageEnvironmentKind.Fields, StageEnvironmentKind.Heath, StageEnvironmentKind.Swamp,
		StageEnvironmentKind.Ruins, StageEnvironmentKind.Ice, StageEnvironmentKind.Desert, StageEnvironmentKind.Bridge,
	};

	/// <summary>
	/// What dotnet build cannot see (RegressionChecks.RunAll): the shader is there, no open-air chapter is
	/// dark, every darkness is 0 to 1, and the bodies' floor stays above the room's - the order the
	/// readability rests on.
	/// </summary>
	public static void Validate(System.Collections.Generic.List<string> warnings)
	{
		if (!ResourceLoader.Exists(ShaderPath))
			warnings.Add($"LightPool: no shader at '{ShaderPath}'; the dark chapters would be lit evenly.");
		foreach (StageEnvironmentKind kind in Enum.GetValues<StageEnvironmentKind>())
		{
			float darkness = StageEnvironmentCatalog.Get(kind).Darkness;
			if (darkness < 0f || darkness > 1f)
				warnings.Add($"LightPool: the {kind} environment's Darkness is {darkness}; it must be 0 to 1.");
			if (darkness > 0f && Array.IndexOf(LitEvenly, kind) >= 0)
				warnings.Add($"LightPool: the {kind} environment is dark ({darkness}), but it is under open sky, which stays lit evenly.");
		}
		if (!(RoomFloor < BodyFloor) || BodyFloor < 0.5f)
			warnings.Add($"LightPool: the bodies' floor ({BodyFloor}) must stay above the room's ({RoomFloor}) and at 0.5 or more, or a body at the edge sinks into the dark.");
	}

	/// <summary>
	/// Lights the run's pool if its chapter is dark, under <paramref name="arena"/> (the y-sorted run).
	/// Null, and nothing added, where the chapter is lit evenly or the tool flag keeps it out.
	/// </summary>
	public static LightPool Kindle(Node2D arena, Player wizard, StageEnvironmentProfile profile)
	{
		if (arena == null || profile == null)
			return null;
		string[] args = OS.GetCmdlineUserArgs();
		float darkness = profile.Darkness;
		foreach (string arg in args)
		{
			if (arg.StartsWith(ForceLightPoolArg, StringComparison.Ordinal)
				&& float.TryParse(arg.Substring(ForceLightPoolArg.Length), System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out float forced))
				darkness = forced;
		}
		if (darkness <= 0f)
			return null;
		if (Array.IndexOf(args, NoLightPoolArg) >= 0)
		{
			GD.Print($"LightPool: {NoLightPoolArg}, so the {profile.DisplayName} is lit evenly this run.");
			return null;
		}
		Shader shader = ResourceLoader.Exists(ShaderPath) ? GD.Load<Shader>(ShaderPath) : null;
		if (shader == null)
		{
			GD.PushError($"LightPool: no shader at '{ShaderPath}'; the {profile.DisplayName} stays lit evenly.");
			return null;
		}
		var material = new ShaderMaterial { Shader = shader };
		var room = new LightPool
		{
			Name = "LightPoolRoom",
			ZAsRelative = false,
			ZIndex = DrawLayers.RoomDark,
			Material = material,
			// After every effect's _Process, so the lights they asked for this frame are drawn this frame.
			ProcessPriority = 1000,
			// Under the pause screen too, so its Reduce Darkness switch fades the run behind it (_Process
			// touches only the strength while the tree is paused).
			ProcessMode = ProcessModeEnum.Always,
			wizard = wizard,
			chapterDarkness = Mathf.Clamp(darkness, 0f, 1f),
			floorLevel = RoomFloor,
			warmth = 1f,
			shared = material,
		};
		var bodies = new LightPool
		{
			Name = "LightPoolBodies",
			ZAsRelative = false,
			ZIndex = DrawLayers.Bodies,
			Material = material,
			floorLevel = BodyFloor,
			warmth = 0f,
			isBodies = true,
		};
		room.bodies = bodies;
		arena.AddChild(room);
		arena.AddChild(bodies);
		GD.Print($"LightPool: the {profile.DisplayName} is dark (darkness {room.chapterDarkness:0.00}, room floor {RoomFloor:0.00}, "
			+ $"bodies floor {BodyFloor:0.00}{(DisplaySettings.ReduceDarkness ? $", reduced to {DisplaySettings.ReducedDarknessScale:0.00}" : "")}).");
		return room;
	}

	private Player wizard;
	private LightPool bodies;
	private bool isBodies;
	private ShaderMaterial shared;
	private float chapterDarkness;
	// What this node's quad carries in its vertex colour: r the floor, g the warm lift (light_pool.gdshader).
	private float floorLevel;
	private float warmth;
	private Vector2 halfSize = new(800f, 800f);
	private Vector2 drawOffset;

	// The run's zoom, taken on the first frame: the pool's size is in the world, so a boss's push-in brings
	// the camera closer to the light rather than shrinking it.
	private float referenceZoom;
	private float strength = -1f;
	private float uploadedStrength = -1f;
	private int uploadedCount = -1;
	private readonly Vector4[] uploaded = new Vector4[MaxLights];

	/// <summary>How dark it is now, 0 to 1 of the chapter's dark after the setting. For the tools.</summary>
	public float Strength => Mathf.Max(0f, strength);

	/// <summary>The pool's full-light and full-dark radii this run, in world units. For the tools.</summary>
	public float InnerRadius { get; private set; }
	public float OuterRadius { get; private set; }

	/// <summary>How many small lights reached the shader on the last frame. For the tools.</summary>
	public int LightsShown => Mathf.Max(0, uploadedCount);

	/// <summary>
	/// The multiplier the room (<paramref name="onBody"/> false) or a body (true) gets at
	/// <paramref name="worldAt"/> from the wizard's pool alone - the shader's own sum, for measuring.
	/// </summary>
	public float MultiplierAt(Vector2 worldAt, bool onBody)
	{
		Vector2 at = wizard != null && IsInstanceValid(wizard) ? wizard.GlobalPosition : GlobalPosition;
		float r = worldAt.DistanceTo(at);
		float t = Mathf.Clamp((r - InnerRadius) / Mathf.Max(1f, OuterRadius - InnerRadius), 0f, 1f);
		float lit = 1f - t * t * (3f - 2f * t);
		float level = onBody ? BodyFloor : RoomFloor;
		return Mathf.Lerp(1f, Mathf.Lerp(level, 1f, lit), Strength);
	}

	public override void _Ready()
	{
		if (isBodies)
		{
			SetProcess(false);
			return;
		}
		driver = this;
		pendingCount = 0;
	}

	public override void _ExitTree()
	{
		if (driver == this)
		{
			driver = null;
			pendingCount = 0;
			cullMin = new Vector2(-1e9f, -1e9f);
			cullMax = new Vector2(1e9f, 1e9f);
		}
	}

	public override void _Process(double delta)
	{
		FrameProfiler.Span span = FrameProfiler.Begin();
		Viewport viewport = GetViewport();
		Camera2D camera = viewport?.GetCamera2D();
		Vector2 canvas = viewport != null ? viewport.GetVisibleRect().Size : new Vector2(1280f, 720f);
		Vector2 zoom = camera != null ? camera.Zoom : Vector2.One;
		zoom = new Vector2(Mathf.Max(0.05f, zoom.X), Mathf.Max(0.05f, zoom.Y));
		if (referenceZoom <= 0f)
		{
			referenceZoom = zoom.X;
			float halfShort = Mathf.Min(canvas.X, canvas.Y) * 0.5f / referenceZoom;
			InnerRadius = halfShort * InnerShare;
			OuterRadius = halfShort * OuterShare;
			shared.SetShaderParameter(ParamInner, InnerRadius);
			shared.SetShaderParameter(ParamOuter, OuterRadius);
			shared.SetShaderParameter(ParamWarmReach, halfShort * WarmReachShare);
			shared.SetShaderParameter(ParamWarm, new Vector3(Warm.R, Warm.G, Warm.B));
		}

		Vector2 seen = canvas / zoom;
		Vector2 centre = camera != null ? camera.GetScreenCenterPosition() : GlobalPosition;
		Vector2 half = seen * 0.5f * Margin;
		if (!half.IsEqualApprox(halfSize))
		{
			halfSize = half;
			QueueRedraw();
			bodies.halfSize = half;
			bodies.drawOffset = new Vector2(0f, -(half.Y + SortBelow));
			bodies.QueueRedraw();
		}
		GlobalPosition = centre;
		bodies.GlobalPosition = centre + new Vector2(0f, half.Y + SortBelow);

		// The chapter's dark, after the setting; eased, so the switch on the pause screen fades it.
		float target = chapterDarkness * DisplaySettings.DarknessScale;
		strength = strength < 0f ? target : Mathf.MoveToward(strength, target, (float)delta * StrengthEase);
		if (!Mathf.IsEqualApprox(strength, uploadedStrength))
		{
			uploadedStrength = strength;
			shared.SetShaderParameter(ParamStrength, strength);
		}
		// Paused (a menu over the run): nothing moves and no spell asks for its light, so the picture keeps
		// the lights it had - clearing them would put out every fireball under a level-up card.
		if (GetTree().Paused)
		{
			FrameProfiler.End(FrameProfiler.Section.LightPool, span);
			return;
		}

		Vector2 lamp = wizard != null && IsInstanceValid(wizard) ? wizard.GlobalPosition : centre;
		shared.SetShaderParameter(ParamCentre, lamp);

		TickReveals((float)delta);

		// The small lights asked for since the last frame: only the slots that changed cross to the engine.
		int count = pendingCount;
		for (int i = 0; i < count; i++)
		{
			if (pending[i] != uploaded[i])
			{
				uploaded[i] = pending[i];
				shared.SetShaderParameter(ParamLights[i], pending[i]);
			}
		}
		if (count != uploadedCount)
		{
			uploadedCount = count;
			shared.SetShaderParameter(ParamLightCount, count);
		}
		pendingCount = 0;
		// Next frame's lights must touch what this frame sees.
		Vector2 seenHalf = seen * 0.5f;
		cullMin = centre - seenHalf;
		cullMax = centre + seenHalf;
		FrameProfiler.End(FrameProfiler.Section.LightPool, span);
	}

	public override void _Draw()
	{
		// The pass rides in the vertex colour (light_pool.gdshader): r how dark the dark gets, g the warm lift.
		DrawRect(new Rect2(drawOffset - halfSize, halfSize * 2f), new Color(floorLevel, warmth, 0f, 1f));
	}
}
