using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// THE HART IN THE HILL - the White Downs' keepsake (stage_14, F3; .ai/second-half.md 4.2: "a hound and a hart cut in
// the hillsides"). Far out on the down, ten to twelve chunks from the start, a leaping hart cut in the chalk, and grown
// over: nobody has scoured it since the moon went out, and the turf has crept back across its lines in knots. Each
// knot holds a warm glint - the hill figure's chalk still faintly lit under the sod.
//
// GATHER (.ai/stage-secrets.md 6, SecretGather): walk onto every knot, in any order; each one walked over is lifted,
// and the chalk under it shows white again, a patch at a time, until the whole hart is scoured. Nothing resets: the
// knots lifted stay lifted for the run, through its chunk being forgotten and furnished again (the director's run
// state). It pays in the run: healed by half (the one heal a secret gives, once a run - .ai/difficulty-curve.md 5).
// Once ever, as a keepsake: 150 Arcane Energy (SecretCatalog.WhiteDowns; a found level of each comfort stat is
// already spoken for on the road).
//
// Its art is tools/art/secret_downs.py's: the grown figure and the scoured one, registered pixel for pixel, and the
// knot of turf drawn on each knot not yet lifted (figures.json). The glints and the scoured hart's light are drawn
// here, opaque gold stops on the world's grid. Flat on the turf under everything; never a target; nothing solid.
public partial class HartInTheHill : SecretSite
{
	public const float Footprint = 380f;
	public const float OpenRadius = 300f;
	private const float KnotReach = 46f;

	private const string GrownPath = "res://assets/bonelight/world/downs/figure-hart-grown.png";
	private const string ScouredPath = "res://assets/bonelight/world/downs/figure-hart-scoured.png";
	private const string KnotPath = "res://assets/bonelight/world/downs/figure-hart-knot.png";
	// figure-hart-*.png are 680 x 440 (340 x 220 art px at x2); the site is their middle. The knots, in art px from
	// the figures' top-left (figures.json): the body, the neck, both antler beams, the near foreleg, both hind legs.
	private static readonly Vector2 FigureSize = new(680f, 440f);
	private static readonly Vector2 KnotSize = new(48f, 32f);
	private static readonly Vector2[] KnotsArt =
	{
		new(157f, 114f), new(239f, 86f), new(229f, 21f), new(259f, 136f), new(76f, 184f), new(101f, 199f), new(216f, 44f),
	};
	// How much of the scoured figure each lifted knot shows round itself, art px either way.
	private const float ScourHalf = 30f;
	// The hart's heart, where its light rests once it is scoured (art px from the top-left).
	private static readonly Vector2 HeartArt = new(170f, 112f);

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
		NearRadius = 1536f,
		OuterRadius = 4600f,
		PeakPerChunk = 30,
		NearShare = 0.6f,
		FootFromBottom = 8f,
	};

	public override SecretFarTell FarTell => Tell;

	/// <summary>The knots, gathered in any order. Public for the probe.</summary>
	public SecretGather Gather { get; private set; }

	/// <summary>The knots in world space.</summary>
	public IReadOnlyList<Vector2> Knots => knots;

	private readonly List<Vector2> knots = new();
	private Texture2D grown, scoured, knot;
	private Node2D glints;
	private float clock;

	private static Vector2 ArtToLocal(Vector2 art) => (art - FigureSize * 0.25f) * 2f;

	public override void _Ready()
	{
		ZIndex = DrawLayers.Decal;
		ZAsRelative = false;
		grown = ResourceLoader.Exists(GrownPath) ? GD.Load<Texture2D>(GrownPath) : null;
		scoured = ResourceLoader.Exists(ScouredPath) ? GD.Load<Texture2D>(ScouredPath) : null;
		knot = ResourceLoader.Exists(KnotPath) ? GD.Load<Texture2D>(KnotPath) : null;
		if (grown == null || scoured == null || knot == null)
			GD.PushError("HartInTheHill: its art is missing; re-run python tools/art/secret_downs.py, then import.");

		knots.Clear();
		foreach (Vector2 art in KnotsArt)
			knots.Add(GlobalPosition + ArtToLocal(art));
		Gather = new SecretGather
		{
			Name = "Gather",
			Director = Director,
			State = Director.StateOf(Key + "/knots"),
			Points = knots,
			TouchRadius = KnotReach,
			DrawSelf = false,
		};
		Gather.Taken += OnTaken;
		Gather.Completed += OnCompleted;
		AddChild(Gather);

		glints = new Node2D { Name = "Glints", ZIndex = DrawLayers.GroundTell, ZAsRelative = false };
		glints.Draw += DrawGlints;
		AddChild(glints);
	}

	public override void _ExitTree()
	{
		if (Gather != null)
		{
			Gather.Taken -= OnTaken;
			Gather.Completed -= OnCompleted;
		}
		if (glints != null && IsInstanceValid(glints))
			glints.Draw -= DrawGlints;
		base._ExitTree();
	}

	// The near tell is the knot nearest him: he comes on the figure from any side.
	public override Vector2 TellPosition
	{
		get
		{
			Player player = Player;
			if (player == null || knots.Count == 0)
				return GlobalPosition;
			Vector2 best = knots[0];
			foreach (Vector2 k in knots)
				if (k.DistanceSquaredTo(player.GlobalPosition) < best.DistanceSquaredTo(player.GlobalPosition))
					best = k;
			return best;
		}
	}

	private void OnTaken(int index)
	{
		if (index >= 0 && index < knots.Count)
			SfxPlayer.AtPosition(SfxCatalog.BearingFound, knots[index], 0f);
		GD.Print($"SECRETS the Hart in the Hill: knot {index} lifted, {Gather?.Gathered ?? 0} of {knots.Count}");
	}

	private void OnCompleted()
	{
		SecretDirector.SiteState state = State;
		if (state == null || state.Done)
			return;
		state.Done = true;
		Player player = Player;
		player?.Heal(player.MaxHP / 2);
		GD.Print($"SECRETS found The Hart in the Hill at {GlobalPosition}: every knot lifted");
		Director.Found(SecretCatalog.HartInTheHillId);
	}

	public override void _Process(double delta)
	{
		clock += (float)delta;
		Player player = Player;
		if (player != null && player.GlobalPosition.DistanceSquaredTo(GlobalPosition) < 1800f * 1800f)
		{
			QueueRedraw();
			glints?.QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (grown == null || scoured == null)
			return;
		SpellInk.AlignToWorldGrid(this);
		Vector2 topLeft = -FigureSize * 0.5f;
		bool done = State?.Done == true;
		if (done)
		{
			DrawTexture(scoured, topLeft);
		}
		else
		{
			DrawTexture(grown, topLeft);
			// Each lifted knot shows the scoured chalk round itself, a square of the clean figure laid over the grown.
			for (int i = 0; i < knots.Count; i++)
			{
				if (Gather == null || !Gather.IsTaken(i))
					continue;
				Vector2 art = KnotsArt[i];
				var src = new Rect2((art - new Vector2(ScourHalf, ScourHalf)) * 2f, new Vector2(ScourHalf, ScourHalf) * 4f);
				DrawTextureRectRegion(scoured, new Rect2(topLeft + src.Position, src.Size), src);
			}
			if (knot != null)
			{
				for (int i = 0; i < knots.Count; i++)
				{
					if (Gather != null && Gather.IsTaken(i))
						continue;
					DrawTexture(knot, ArtToLocal(KnotsArt[i]) - KnotSize * 0.5f);
				}
			}
		}
		DrawSetTransform(Vector2.Zero);
	}

	// A glint in every knot not yet lifted; once the hart is scoured, a steady warm light at its heart.
	private void DrawGlints()
	{
		if (State?.Done == true)
		{
			DrawLight(glints, ArtToLocal(HeartArt), 10f, 0.6f);
			return;
		}
		for (int i = 0; i < knots.Count; i++)
		{
			if (Gather != null && Gather.IsTaken(i))
				continue;
			DrawGlint(glints, ArtToLocal(KnotsArt[i]) + new Vector2(0f, -4f), clock + i * 0.37f);
		}
	}
}
