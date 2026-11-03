using Godot;
using TheOneHeMissed.scripts;

// The frozen lake the thaw's wizard stands in (.ai/side-events.md, "The thaw").
//
// Eric, playing 0.10.0: "Wizard trapped in ice should be in the center of a large, magically animated
// lake. Current art is corny looking." The art is tools/art/thaw_lake.py, the motion is
// scenes/shaders/thaw_lake.gdshader: rings of cold light running out under the ice from where he
// stands, light wandering slowly under the rest of it, frost ferns creeping in from the shore and
// drawing back. This node only feeds the shader its clock and two numbers - how strongly he is
// singing, how far the thaw has got - so it costs one sprite and one uniform write a frame.
//
// IT IS NOT TERRAIN. It is frozen and the fight is on it, so it blocks nothing, joins no group and
// has no body: the player and the swarm walk across it like any ground. What keeps trees and rocks
// off it is the event's footprint (ThawEvent.SiteFootprint), reserved at stage start like every
// event's.
//
// IT OUTLASTS THE EVENT. A won (or stood-down) event frees itself a few seconds later, and a lake
// the size of the screen vanishing would be the strangest thing in the run. So the lake is the
// event's sibling, not its child, and stays for the rest of the run - still, once he is free.
public partial class ThawLake : Sprite2D
{
	public const string Directory = "res://assets/bonelight/world/thaw/";
	public const string ShaderPath = "res://scenes/shaders/thaw_lake.gdshader";

	/// <summary>The lake's semi-axes in world pixels: thaw_lake.py A and B, drawn at x2.</summary>
	public static readonly Vector2 WorldAxes = new Vector2(320f, 256f);

	/// <summary>The snow bank outside the shore, in world pixels (thaw_lake.py BANK at x2).</summary>
	public const float WorldBank = 20f;

	/// <summary>How far the thaw has got, 0..1. Set by the event every frame.</summary>
	public float Heat { get; set; }

	/// <summary>True once the wizard is out (or the event stood down): the song dies away.</summary>
	public bool Silent { get; set; }

	// Written every frame, so the names are built once (.ai/performance.md: no string-built StringName in a hot path).
	private static readonly StringName ClockParam = "clock";
	private static readonly StringName SongParam = "song";
	private static readonly StringName HeatParam = "heat";

	private ShaderMaterial material;
	private float clock;
	private float song = 1f;
	// How long the light takes to die away once he is free.
	private const float FallSeconds = 3f;

	/// <summary>The lake at <paramref name="world"/>, or null (with an error) if its art is missing.</summary>
	public static ThawLake Create(Vector2 world)
	{
		Texture2D lake = Load("thaw-lake.png");
		Texture2D lit = Load("thaw-lake-lit.png");
		Texture2D bright = Load("thaw-lake-bright.png");
		Texture2D data = Load("thaw-lake-data.png");
		var shader = ResourceLoader.Exists(ShaderPath) ? GD.Load<Shader>(ShaderPath) : null;
		if (lake == null || lit == null || bright == null || data == null || shader == null)
		{
			GD.PushError("ThawLake: the lake's art or shader is missing or not imported (python tools/art/thaw_lake.py, "
				+ "then import); the thaw will stand on bare ground.");
			return null;
		}
		var material = new ShaderMaterial { Shader = shader };
		material.SetShaderParameter("lit_tex", lit);
		material.SetShaderParameter("bright_tex", bright);
		material.SetShaderParameter("data_tex", data);
		return new ThawLake
		{
			Name = "ThawLake",
			Texture = lake,
			Material = material,
			material = material,
			Centered = true,
			// x2, the contract's render scale; the texture is authored 1:1.
			Scale = new Vector2(2f, 2f),
			Position = world.Round(),
			TextureFilter = TextureFilterEnum.Nearest,
			// On the ground: over the painted terrain and its decals, under the craters and pools a
			// spell leaves (DrawLayers.Ground) and every body.
			ZIndex = DrawLayers.Ground - 2,
			ZAsRelative = false,
		};
	}

	private static Texture2D Load(string file)
	{
		string path = Directory + file;
		return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		clock += dt;
		if (Silent)
			song = Mathf.Max(0f, song - dt / FallSeconds);
		material?.SetShaderParameter(ClockParam, clock);
		material?.SetShaderParameter(SongParam, song);
		material?.SetShaderParameter(HeatParam, Silent ? 0f : Mathf.Clamp(Heat, 0f, 1f));
	}
}
