using Godot;
using System;
using TheOneHeMissed.scripts;

// THE HOUND IN THE HILL - the White Downs' near secret, its anchor (stage_14, F3; .ai/second-half.md 4.2: "a hound
// and a hart cut in the hillsides"). A running hound cut in the chalk of the down, in the manner of the old hill
// figures - a few long strokes, more sign than animal - and in its eye a hollow where something warm is still lying:
// the near tell, the one warm point on the down. It is Laelaps, the hound that never failed to catch what it ran
// after, Artemis's gift; the downs remember him.
//
// LINGER (.ai/stage-secrets.md 6, the vocabulary): stand in its eye for LingerSeconds; stepping out drains it slowly,
// never resets it. It pays in the run: the hound's pace - he walks a quarter faster for a minute (a timed buff, the
// allowed kind: move speed, 60 s, Player.ApplyTemporaryBuff), every run it is found. Once ever: the page.
//
// Its figure and tells are tools/art/secret_downs.py's (figure-hound.png, at x2; its eye in figures.json); the glint
// and the lit eye are drawn here, opaque gold stops on the world's grid (SecretSite.DrawGlint, DrawLight). It lies
// flat on the turf under everything. Never a target, never in "enemies"; nothing solid.
public partial class HoundInTheHill : SecretSite
{
	/// <summary>The ground kept clear for it: the figure, half its width, and a margin.</summary>
	public const float Footprint = 330f;
	/// <summary>Open turf over the whole figure (no barrow across it).</summary>
	public const float OpenRadius = 260f;

	public const float LingerSeconds = 3f;
	private const float EyeRadius = 40f;
	private const float DrainPerSecond = 0.25f;
	public const float PaceBonus = 0.25f;
	public const float PaceSeconds = 60f;

	private const string FigurePath = "res://assets/bonelight/world/downs/figure-hound.png";
	// figure-hound.png is 600 x 280 (300 x 140 art px at x2); the site is its middle. Its eye is the corner of art
	// pixel (237, 39) (figures.json): (237 - 150, 39 - 70) art px from the middle, x2.
	private static readonly Vector2 FigureSize = new(600f, 280f);
	public static readonly Vector2 EyeOffset = new((237f - 150f) * 2f, (39f - 70f) * 2f);

	private static readonly SecretFarTell Tell = new()
	{
		Textures = new[]
		{
			"res://assets/bonelight/world/downs/tell-chalk-1.png",
			"res://assets/bonelight/world/downs/tell-chalk-2.png",
			"res://assets/bonelight/world/downs/tell-chalk-3.png",
		},
		NearTextures = new[]
		{
			"res://assets/bonelight/world/downs/tell-near-1.png",
			"res://assets/bonelight/world/downs/tell-near-2.png",
		},
		InnerRadius = Footprint,
		NearRadius = 1100f,
		OuterRadius = 2300f,
		PeakPerChunk = 26,
		NearShare = 0.5f,
		FootFromBottom = 8f,
	};

	public override SecretFarTell FarTell => Tell;

	public override Vector2 TellPosition => GlobalPosition + EyeOffset;

	/// <summary>Seconds stood in its eye, toward <see cref="LingerSeconds"/>. For the probe.</summary>
	public float Lingered => State?.Progress ?? 0f;

	private Texture2D figure;
	private Node2D eyeLayer;
	private float clock;

	public override void _Ready()
	{
		ZIndex = DrawLayers.Decal;
		ZAsRelative = false;
		figure = ResourceLoader.Exists(FigurePath) ? GD.Load<Texture2D>(FigurePath) : null;
		if (figure == null)
			GD.PushError("HoundInTheHill: its art is missing; re-run python tools/art/secret_downs.py, then import.");
		// The glint and the lit eye stand over the floor detail and under the bodies.
		eyeLayer = new Node2D { Name = "Eye", ZIndex = DrawLayers.GroundTell, ZAsRelative = false };
		eyeLayer.Draw += DrawEye;
		AddChild(eyeLayer);
	}

	public override void _ExitTree()
	{
		if (eyeLayer != null && IsInstanceValid(eyeLayer))
			eyeLayer.Draw -= DrawEye;
		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		clock += dt;
		Player player = Player;
		SecretDirector.SiteState state = State;
		if (player == null || state == null)
			return;
		bool near = player.GlobalPosition.DistanceSquaredTo(GlobalPosition) < 1600f * 1600f;
		if (!state.Done && Active)
		{
			if (player.FootPosition.DistanceTo(TellPosition) <= EyeRadius)
				state.Progress += dt;
			else
				state.Progress = MathF.Max(0f, state.Progress - DrainPerSecond * dt);
			if (state.Progress >= LingerSeconds)
			{
				state.Done = true;
				state.Progress = LingerSeconds;
				player.ApplyTemporaryBuff(0f, PaceBonus, PaceSeconds);
				Director.Found(SecretCatalog.HoundInTheHillId);
			}
		}
		if (near)
		{
			QueueRedraw();
			eyeLayer?.QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (figure == null)
			return;
		SpellInk.AlignToWorldGrid(this);
		DrawTexture(figure, -FigureSize * 0.5f);
		DrawSetTransform(Vector2.Zero);
	}

	// The eye: the glint breathing in the hollow until it is found, a steady warm light in it after - and, while he
	// stands in it, a ring of the linger filling round the eye in the same warm row.
	private void DrawEye()
	{
		SecretDirector.SiteState state = State;
		if (state != null && state.Done)
		{
			DrawLight(eyeLayer, EyeOffset, 8f, 0.6f);
			return;
		}
		DrawGlint(eyeLayer, EyeOffset, clock);
		float progress = state == null ? 0f : state.Progress / LingerSeconds;
		if (progress > 0f)
		{
			float start = -Mathf.Pi / 2f;
			eyeLayer.DrawArc(EyeOffset, EyeRadius, start, start + Mathf.Tau * progress, 32, GoldLit, 2f);
		}
	}
}
