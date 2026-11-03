using Godot;
using TheOneHeMissed.scripts;

// The Frozen Waste's side event: the thaw (.ai/side-events.md).
//
// "A wizard lies under the ice, and the cold stands guard." A figure is sealed in a block of ice.
// The block is NOT a target and never joins "enemies" - the standing rule for anything breakable -
// so no spell can be aimed at it. It thaws from the fighting around it instead: every hit the player
// lands on an enemy within HeatRadius warms it, fire three times over, and the player's own light
// warms it slowly while they stand close. Meanwhile the cold sends more to stand guard.
//
// What it asks: fight HERE. The swarm has to be drawn onto the block and killed on it, which is the
// opposite of the kiting every run teaches. Area spells and fire are the answer, which is exactly
// what a breakable should reward.
//
// WHERE HE IS (Eric, playing 0.10.0: "Wizard trapped in ice should be in the center of a large,
// magically animated lake. Current art is corny looking"). He stands frozen in a block of ice - his
// own sprite seen through it - at the centre of a frozen lake 640 px across (ThawLake), with cold
// light running out under the ice from him, frost creeping in from the shore and a glow round him.
// The block cracks, chips and pools meltwater as the heat climbs (four stages in its sheet), the
// light under the ice brightens and reaches further, and a warm glow grows inside it. Both are
// generated (tools/art/thaw_lake.py). The lake is walkable ground, not terrain: the fight is on it.
public partial class ThawEvent : SideEvent
{
	[Export] public float HeatNeeded { get; set; } = 900f;
	[Export] public float HeatRadius { get; set; } = 150f;
	[Export] public float FireHeatMultiplier { get; set; } = 3f;
	[Export] public float WarmthRadius { get; set; } = 90f;
	[Export] public float WarmthPerSecond { get; set; } = 10f;
	[Export] public float GuardIntervalSeconds { get; set; } = 5f;

	private Vector2 block;
	private float heat;
	private float guardTimer;
	// The hits that warm it: the StrikeListener every breakable secret uses was extracted from here
	// (.ai/achievements-and-secrets.md section 5), and the thaw counts through it exactly as it did.
	private StrikeListener strike;

	protected override string Announcement => "Someone is frozen in the ice nearby.";
	protected override string ChestLine => "The ice gives up a chest";
	protected override string Objective => $"Thaw the ice: fight beside it  ({Mathf.RoundToInt(100f * heat / HeatNeeded)}%)";
	// The top of the block, not its foot: the on-screen chevron hangs above its target, and on the
	// foot it sat on his face.
	protected override Vector2? PointerTarget => block + new Vector2(0f, -BlockStandsWorld);

	protected override string SiteGlyph => "ice";
	// The whole lake and its bank, and a little more: placement wants all of it on open ground (no
	// pond under the ice), and decor stays off it.
	protected override float SiteFootprint => ThawLake.WorldAxes.X + ThawLake.WorldBank + 20f;

	// Tested over the whole disc, not on two rings (Node2DGame.IsExploreGroundClearDense): at 360 px
	// eight points a ring are 280 px apart, and a pond under walkable ice would be an invisible wall.
	public override void PlaceSites(ExplorePlacer placer) => AddSite(placer.EventSite(SiteFootprint, dense: true));
	// Inside the reach where the cold starts sending guards (360), so arriving and the fight begin together.
	protected override float ArrivalRadius => 420f;
	protected override Color TellColor => new Color(0.50f, 0.86f, 0.97f);   // the ice ramp's mid

	private ThawLake lake;
	private Sprite2D iceBlock;
	private float glintClock;

	// The block's sheet: four stages of thawing, each four frames of a glint crossing its face.
	private const string BlockPath = ThawLake.Directory + "thaw-block.png";
	private const int BlockCell = 64;
	private const int BlockStages = 4;
	private const int GlintFrames = 4;
	// The row of a cell the block stands on (its contact line), and its middle column.
	private const int BlockFootRow = 58;
	private const int BlockAnchorX = 32;
	// How tall the block stands in the world, from its foot to its top (rows 4..58 of the cell, at x2).
	private const float BlockStandsWorld = 108f;
	// A glint crosses in three frames, then the face rests.
	private const float GlintFrameSeconds = 0.11f;
	private const float GlintRestSeconds = 2.6f;

	protected override void OnPlaced()
	{
		block = MainSite;

		// The lake is the event's sibling, so it stays when the event frees itself (ThawLake).
		lake = ThawLake.Create(block);
		if (lake != null)
			Game?.CallDeferred(Node.MethodName.AddChild, lake);

		Texture2D sheet = ResourceLoader.Exists(BlockPath) ? GD.Load<Texture2D>(BlockPath) : null;
		if (sheet == null)
		{
			GD.PushError("ThawEvent: thaw-block.png is missing or not imported (python tools/art/thaw_lake.py, then import).");
			return;
		}
		// Standing in the world and sorted with the actors at its foot, as the bog lantern is (ZIndex 5
		// under this node's -5 is the actors' 0). Drawn at x2, so the offset is in sheet pixels.
		Vector2 ground = ToLocal(block);
		iceBlock = new Sprite2D
		{
			Name = "IceBlock",
			Texture = sheet,
			Hframes = BlockStages * GlintFrames,
			Centered = false,
			Scale = new Vector2(2f, 2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Position = new Vector2(Mathf.Round(ground.X), Mathf.Round(ground.Y - WorldSprites.SortLift)),
			Offset = new Vector2(-BlockAnchorX, WorldSprites.SortLift / 2f - BlockFootRow),
			ZIndex = 5,
		};
		AddChild(iceBlock);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		float t = Mathf.Clamp(heat / HeatNeeded, 0f, 1f);
		if (lake != null && IsInstanceValid(lake))
		{
			lake.Heat = t;
			// He is out, or the event stood down: the light under the ice dies away.
			if (IsResolved)
				lake.Silent = true;
		}
		if (iceBlock == null)
			return;
		if (IsResolved)
		{
			iceBlock.Visible = false;
			return;
		}
		glintClock += (float)delta;
		float cycle = GlintFrameSeconds * (GlintFrames - 1) + GlintRestSeconds;
		int glint = Mathf.Min(GlintFrames - 1, Mathf.FloorToInt((glintClock % cycle) / GlintFrameSeconds));
		int stage = Mathf.Clamp(Mathf.FloorToInt(t * BlockStages), 0, BlockStages - 1);
		iceBlock.Frame = stage * GlintFrames + glint;
	}

	// Placed at stage start and waiting (SideEvent); the listener only goes on when the player
	// arrives, so a block nobody has found costs nothing per hit.
	protected override void Begin()
	{
		block = MainSite;
		strike = new StrikeListener(block, HeatRadius)
		{
			Weight = source => StrikeListener.IsFire(source) ? FireHeatMultiplier : 1f,
			Struck = (amount, _) =>
			{
				if (!IsResolved)
					heat += amount;
			},
		};
		strike.Start();
	}

	protected override void Tick(float delta)
	{
		if (PlayerWithin(block, WarmthRadius))
			heat += WarmthPerSecond * delta;

		// The guard only comes while the player is working on the block - an event left alone
		// should not quietly fill the map.
		if (PlayerWithin(block, 360f))
		{
			guardTimer -= delta;
			if (guardTimer <= 0f)
			{
				guardTimer = GuardIntervalSeconds;
				SpawnRing(block, 4, 280f);
			}
		}

		if (heat >= HeatNeeded)
		{
			StopListening();
			Succeed(block);
		}
	}

	private void StopListening() => strike?.Stop();

	public override void _ExitTree()
	{
		StopListening();
		base._ExitTree();
	}

	protected override void OnStoodDown() => StopListening();

	private static readonly Color Warm = new Color(1.0f, 0.62f, 0.30f, 0.95f);
	private static readonly Color IceMid = new Color(0.498f, 0.863f, 0.973f);   // ice mid #7FDCF8
	private static readonly Color FireEdge = new Color(0.910f, 0.478f, 0.102f); // fire mid #E87A1A

	public override void _Draw()
	{
		if (IsResolved)
			return;
		// The progress, on the ice round the block's foot. The reach of the heat is not ringed: it is
		// where the light under the ice is (ThawLake).
		DrawProgressArc(block + new Vector2(0f, -4f), 64f, Mathf.Clamp(heat / HeatNeeded, 0f, 1f), Warm);
	}

	// The far tell, and a glow round him that breathes - found things breathe, his things flicker, and
	// this one is both: the light is the dark wizard's ice, the breath is the singer inside it. As the
	// heat climbs, a warm glow grows in the block.
	protected override void DrawLight(CanvasItem light)
	{
		// The far tell only while nobody has come: it is how the lake is found, and its column and pool,
		// added over the block once he is standing there, washed the wizard in it out.
		if (IsDormant)
			base.DrawLight(light);
		if (IsResolved || !NearPlayer(block))
			return;
		Vector2 c = ToLocal(block);
		float breath = 0.82f + 0.18f * Mathf.Sin(Clock * 1.7f);
		float t = Mathf.Clamp(heat / HeatNeeded, 0f, 1f);
		// A wide, faint breath over the lake; the bright light round him is under the ice (ThawLake).
		ExploreArt.DrawGlow(light, c, 260f, new Color(IceMid, 0.06f * breath));
		if (t > 0f)
			ExploreArt.DrawGlow(light, c + new Vector2(0f, -48f), 70f + 30f * t, new Color(FireEdge, 0.28f * t));
	}
}
