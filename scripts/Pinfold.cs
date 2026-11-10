using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// A walkable, magically sealed pound. Free each of its three ghostly companions after opening the gate.
public partial class Pinfold : SecretVault
{
	public const float WakeRadius = 520f;
	public const float LatchReach = 64f;
	public const float GuardHealthMultiplier = 10f;
	public static readonly BestiaryRole[] GuardRoles = { BestiaryRole.Wall, BestiaryRole.Slammer };
	// THE GATE'S KEEPERS (in-flight 101, Eric 2026-11-10: "the guards should stay near the gate, and be glowing and
	// marked so you notice them"). Each holds a post beside the gate: it fights him while he is within GuardLeash of
	// that post, and past it - or once drawn more than GuardLeash off - it walks back and holds again (the Gaoler's
	// warden keeps its post the same way, GaolersKeyEvent.KeepPost). They used to wander off with the swarm and be lost.
	public const float GuardLeash = 280f;
	private const float GuardHomeSlack = 40f;
	// Not zero, which would hand the body back to its chase: the Gaoler's HoldHeading.
	private static readonly Vector2 GuardHold = new(0.001f, 0f);
	private static readonly Color SealBlue = new("#6AB8E8");
	private static readonly Color SealCore = new("#DCF4FF");
	public static readonly Vector2 PoundCell = new(1944f, 1632f);
	private static readonly Vector2 PoundAnchor = new(-972f, -1608f);
	private static readonly Vector2 LatchOffset = new(132f, -24f);
	private static readonly Vector2 CentreOffset = new(0f, -768f);
	private static readonly Rect2[] Walls =
	{
		new(-960f, -1544f, 1920f, 16f), new(-960f, -1536f, 16f, 1544f),
		new(944f, -1536f, 16f, 1544f), new(-960f, -8f, 832f, 16f), new(128f, -8f, 832f, 16f),
	};
	private static readonly SecretFarTell Tell = new()
	{
		Textures = new[] { "res://assets/bonelight/world/fields/hurdle-broken-1.png", "res://assets/bonelight/world/fields/hurdle-broken-2.png", "res://assets/bonelight/world/fields/pawprints-1.png" },
		NearTextures = new[] { "res://assets/bonelight/world/fields/pawprints-1.png", "res://assets/bonelight/world/fields/pawprints-2.png" },
		InnerRadius = SecretCatalog.VaultFootprint, NearRadius = 1536f, OuterRadius = 3072f,
		PeakPerChunk = 40, NearShare = 0.75f, FootFromBottom = 4f, LeanToward = true,
	};
	public override SecretFarTell FarTell => Tell;
	public override Vector2 TellPosition => GlobalPosition + new Vector2(0f, -40f);
	public Vector2 Latch => GlobalPosition + LatchOffset;
	public Vector2 EssenceCentre => GlobalPosition + CentreOffset;
	public Vector2 Entrance => GlobalPosition + new Vector2(0f, 64f);
	public IReadOnlyList<Enemy> Guards => guards;
	public bool GuardsDown => State != null && State.Step >= 2;
	public bool GateOpen => State?.Step >= 3 || IsOpened;
	public override IEnumerable<(Vector2 Centre, float Radius)> SolidFootprints
	{
		get
		{
			// Stamp only walls: the enclosure's interior and entrance must remain walkable.
			foreach (Rect2 wall in Walls)
			{
				Vector2 a = wall.GetCenter();
				bool horizontal = wall.Size.X > wall.Size.Y;
				float span = (horizontal ? wall.Size.X : wall.Size.Y) - 16f;
				int count = Mathf.CeilToInt(span / 32f);
				for (int i = 0; i <= count; i++)
					yield return (Position + a + (horizontal ? Vector2.Right : Vector2.Down) * (-span * 0.5f + span * i / count), 34f);
			}
		}
	}
	private readonly List<Enemy> guards = new();
	private readonly List<Vector2> guardPosts = new();
	private readonly List<bool> guardsWalkingHome = new();
	private readonly List<Node2D> keeperGlows = new();
	private readonly List<Node2D> keeperMarks = new();
	private Texture2D pound;
	private Texture2D front;
	private Texture2D openFront;
	private Node2D back;
	private Node2D binding;
	private CollisionShape2D gateCollider;
	private float clock;
	private SecretToast rescueToast;
	public const int CaptiveCount = 3;
	public const float CompanionReach = 84f;
	private sealed class Captive
	{
		public AnimatedSprite2D Sprite;
		public ShaderMaterial Material;
		public Vector2 At;
		public Vector2 FreedAt;
		public int Goal;
		public float Wait;
		public float Released = -1f;
		public string Animation = "";
	}
	private readonly Captive[] captives = new Captive[CaptiveCount];
	private static readonly Vector2[][] EscapeGoals =
	{
		new[] { new Vector2(-840,-1150), new Vector2(-840,-350), new Vector2(-905,-350), new Vector2(-620,-900) },
		new[] { new Vector2(840,-500), new Vector2(840,-1200), new Vector2(905,-1200), new Vector2(600,-850) },
		new[] { new Vector2(-350,-1400), new Vector2(500,-1400), new Vector2(500,-1470), new Vector2(100,-700) },
	};
	public bool IsCaptiveFreed(int i) => ((State?.Marks ?? 0UL) & (1UL << i)) != 0;
	public int FreedCount { get { int count = 0; for (int i = 0; i < CaptiveCount; i++) if (IsCaptiveFreed(i)) count++; return count; } }
	public Vector2 CaptivePosition(int i) => GlobalPosition + captives[i].At;
	public override void LayOut(ExplorePlacer placer)
	{
		// The rolled site is the enclosure CENTER. Reserve/clear its real footprint before paint.
		Position -= CentreOffset;
		Director.PrepareFoundation(new Rect2(Position + new Vector2(-1008f, -1584f), new Vector2(2016f, 1776f)));
	}

	public override void _Ready()
	{
		YSortEnabled = true;
		pound = GD.Load<Texture2D>("res://assets/bonelight/world/fields/pinfold-back.png");
		front = GD.Load<Texture2D>("res://assets/bonelight/world/fields/pinfold-front.png");
		openFront = GD.Load<Texture2D>("res://assets/bonelight/world/fields/pinfold-open.png");
		var body = new StaticBody2D { Name = "Walls", CollisionLayer = 1u << (LevelTilePainter.TerrainCollisionLayer - 1), CollisionMask = 0 };
		foreach (Rect2 wall in Walls)
			body.AddChild(new CollisionShape2D { Position = wall.GetCenter(), Shape = new RectangleShape2D { Size = wall.Size } });
		gateCollider = new CollisionShape2D { Name = "Gate", Shape = new RectangleShape2D { Size = new Vector2(256f, 16f) } };
		body.AddChild(gateCollider);
		AddChild(body);
		// Separate sort origins: back/floor before someone INSIDE, front wall after them.
		back = new Node2D { Name = "Back", Position = new Vector2(0f, -1536f) };
		back.Draw += DrawBack;
		AddChild(back);
		Shader ghost = GD.Load<Shader>("res://scenes/shaders/pinfold_ghost.gdshader");
		string[] forms = { "Hound", "Cat", "Crow" };
		Vector2[] starts = { new(-500,-1000), new(500,-700), new(0,-1100) };
		for (int i = 0; i < CaptiveCount; i++)
		{
			var material = new ShaderMaterial { Shader = ghost };
			var sprite = new AnimatedSprite2D
			{
				Name = "Ghost" + forms[i], SpriteFrames = GD.Load<SpriteFrames>(Familiar.FramesPath(forms[i])),
				Scale = Vector2.One * 2f, Offset = new Vector2(0f, -23f),
				Material = material, TextureFilter = TextureFilterEnum.Nearest,
				ZAsRelative = i != 2, ZIndex = i == 2 ? DrawLayers.Air : 0,
			};
			captives[i] = new Captive { Sprite = sprite, Material = material, At = starts[i] };
			AddChild(sprite);
			sprite.Position = starts[i];
		}
		binding = new Node2D { Name = "BindingWisps", ZAsRelative = false, ZIndex = DrawLayers.Air };
		binding.Draw += DrawBinding;
		AddChild(binding);
		rescueToast = new SecretToast();
		AddChild(rescueToast);
	}
	public override void _ExitTree()
	{
		if (back != null && IsInstanceValid(back)) back.Draw -= DrawBack;
		if (binding != null && IsInstanceValid(binding)) binding.Draw -= DrawBinding;
		base._ExitTree();
	}
	public override void _Process(double delta)
	{
		clock += (float)delta;
		if (Player != null && State != null && !State.Done && Active)
		{
			if (State.Step == 0 && Player.GlobalPosition.DistanceTo(GlobalPosition) <= WakeRadius) Wake();
			if (State.Step == 1)
				KeepPosts();
			if (State.Step == 1 && guards.TrueForAll(IsDown))
			{
				State.Step = 2;
				// The last step, heard as the latch lights (_Draw): both keepers down, the gate is his to open.
				SfxPlayer.AtPosition(SfxCatalog.SecretSeen, Latch, 0f);
			}
			if (State.Step == 2 && Player.GlobalPosition.DistanceTo(Latch) <= LatchReach) LiftLatch();
		}
		TickCaptives((float)delta);
		if (Player != null && Player.GlobalPosition.DistanceSquaredTo(EssenceCentre) < 3200f * 3200f)
		{
			QueueRedraw();
			binding?.QueueRedraw();
		}
	}
	private static bool IsDown(Enemy guard) => guard == null || !IsInstanceValid(guard) || guard.IsDying || guard.Health <= 0;
	private void Wake()
	{
		State.Step = 1;
		guards.Clear();
		guardPosts.Clear();
		guardsWalkingHome.Clear();
		keeperGlows.Clear();
		keeperMarks.Clear();
		for (int i = 0; i < GuardRoles.Length; i++)
		{
			Vector2 at = Director.Game.ReachablePoint(GlobalPosition + new Vector2(i == 0 ? -300f : 300f, 64f));
			Enemy guard = Director.Game.SpawnEventEnemy(GuardRoles[i], at, GuardHealthMultiplier, true);
			if (guard == null) continue;
			guards.Add(guard);
			guardPosts.Add(guard.GlobalPosition);
			guardsWalkingHome.Add(false);
			MarkAsKeeper(guard);
		}
		SfxPlayer.AtPosition(SfxCatalog.EliteSpawn, GlobalPosition, 0f);
		if (guards.Count == 0) State.Step = 2;
	}
	// Each keeper's post, held as KeepPost holds the Gaoler's warden's: fight him near it, never far from it.
	private void KeepPosts()
	{
		for (int i = 0; i < guards.Count; i++)
		{
			Enemy guard = guards[i];
			if (IsDown(guard))
			{
				// The glow and the mark go with the body, on the frame it starts to fall (Enemy.StartDeath hides its own).
				if (guard != null && IsInstanceValid(guard))
				{
					keeperGlows[i].Visible = false;
					keeperMarks[i].Visible = false;
					guard.ScriptedHeading = Vector2.Zero;
				}
				continue;
			}
			keeperGlows[i].QueueRedraw();
			keeperMarks[i].QueueRedraw();
			Vector2 at = guard.GlobalPosition;
			float fromPost = at.DistanceTo(guardPosts[i]);
			bool heIsNear = Player.GlobalPosition.DistanceTo(guardPosts[i]) <= GuardLeash;
			if (guardsWalkingHome[i] && fromPost <= GuardHomeSlack)
				guardsWalkingHome[i] = false;
			else if (!guardsWalkingHome[i] && fromPost > GuardLeash)
				guardsWalkingHome[i] = true;
			// A wind-up in progress keeps its own plant (a zero heading hands steering back to the body), so a
			// slam being drawn never slides off its ring on the way home.
			if (guard.IsWindingUpAttack || (!guardsWalkingHome[i] && heIsNear))
				guard.ScriptedHeading = Vector2.Zero;
			else if (fromPost <= GuardHomeSlack)
				guard.ScriptedHeading = GuardHold;
			else
				guard.ScriptedHeading = (guardPosts[i] - at).Normalized();
		}
	}

	// The keeper's look, on top of an elite's gold ring: a pale glow of the gate's own seal-blue round the body, and
	// the seal's cross over its head, so the two bodies the gate answers to read as its own (in-flight 101). Two
	// code-made children of the guard, so they move and go with it; each draws from the guard alone (a static
	// painter, the clock from the engine), never this vault, which a streamed chunk can free first.
	private void MarkAsKeeper(Enemy guard)
	{
		var glow = new Node2D { Name = "KeeperGlow", ZIndex = -1 };
		guard.AddChild(glow);
		// First among its children, so the elite ring (also z -1) still draws over the glow.
		guard.MoveChild(glow, 0);
		glow.Draw += () => PaintKeeperGlow(glow, guard);
		keeperGlows.Add(glow);
		// Under the attack icon (AttackIcon, 12), which takes the mark's place while a wind-up shows.
		var mark = new Node2D { Name = "KeeperMark", ZAsRelative = false, ZIndex = DrawLayers.ForemostAir };
		guard.AddChild(mark);
		mark.Draw += () => PaintKeeperMark(mark, guard);
		keeperMarks.Add(mark);
	}

	private static float KeeperClock => Time.GetTicksMsec() / 1000f;

	private static void PaintKeeperGlow(Node2D canvas, Enemy guard)
	{
		if (!IsInstanceValid(guard) || guard.IsDying)
			return;
		StatusFxGeometry geo = guard.StatusFxGeometry;
		float height = Mathf.Max(24f, geo.FeetY - geo.HeadY);
		Vector2 middle = new(0f, (geo.HeadY + geo.FeetY) * 0.5f);
		float breath = SecretSite.Breath(KeeperClock);
		PixelInk.Disc(canvas, middle, height * 0.62f, new Color(SealBlue, 0.10f + 0.10f * breath));
		PixelInk.Disc(canvas, middle, height * 0.46f, new Color(SealBlue, 0.16f + 0.14f * breath));
	}

	private static void PaintKeeperMark(Node2D canvas, Enemy guard)
	{
		if (!IsInstanceValid(guard) || guard.IsDying || guard.IsWindingUpAttack)
			return;
		StatusFxGeometry geo = guard.StatusFxGeometry;
		// Where the attack icon stands (Enemy.PaintAttackIcon), bobbing a pixel with the breath.
		float px = Mathf.Max(TelegraphIcon.MinPixelScale, geo.PixelScale);
		float top = geo.HeadY - (geo.FeetY - geo.HeadY) * (0.10f / 0.84f) - px * 5f;
		float breath = SecretSite.Breath(KeeperClock);
		Vector2 c = new(0f, top - Mathf.Round(breath * 2f) * PixelInk.Grid);
		float r = px * 3f;
		// A diamond round the gate's own cross: the seal on the gate, worn.
		Vector2 n = c + new Vector2(0f, -r), e = c + new Vector2(r, 0f), s = c + new Vector2(0f, r), w = c + new Vector2(-r, 0f);
		PixelInk.Disc(canvas, c, r * 1.1f, new Color(SealBlue, 0.22f + 0.18f * breath));
		PixelInk.Line(canvas, n, e, SealBlue, 1);
		PixelInk.Line(canvas, e, s, SealBlue, 1);
		PixelInk.Line(canvas, s, w, SealBlue, 1);
		PixelInk.Line(canvas, w, n, SealBlue, 1);
		float x = r * 0.45f;
		PixelInk.Line(canvas, c + new Vector2(-x, -x), c + new Vector2(x, x), SealCore, 1);
		PixelInk.Line(canvas, c + new Vector2(-x, x), c + new Vector2(x, -x), SealCore, 1);
	}

	private void LiftLatch()
	{
		State.Step = 3;
		gateCollider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		SfxPlayer.AtPosition(SfxCatalog.EventReward, Latch, 0f);
	}
	/// <summary>Open only the gate for a photograph; each companion still has to be freed.</summary>
	public void OpenForProbe() { if (!GateOpen) LiftLatch(); }
	private void DrawBack()
	{
		if (pound == null) return;
		SpellInk.AlignToWorldGrid(back);
		back.DrawTextureRectRegion(pound, new Rect2(PoundAnchor - back.Position, PoundCell), new Rect2(Vector2.Zero, PoundCell));
		back.DrawSetTransform(Vector2.Zero);
	}
	public override void _Draw()
	{
		if (pound == null) return;
		SpellInk.AlignToWorldGrid(this);
		DrawTextureRectRegion(GateOpen ? openFront : front, new Rect2(PoundAnchor, PoundCell), new Rect2(Vector2.Zero, PoundCell));
		DrawSetTransform(Vector2.Zero);
		if (GateOpen) return;
		Color seal = new("#6AB8E8");
		float pulse = 0.4f + 0.5f * Breath(clock);
		PixelInk.Line(this, new Vector2(-124f, -50f), new Vector2(124f, -6f), seal, 1, pulse);
		PixelInk.Line(this, new Vector2(-124f, -6f), new Vector2(124f, -50f), seal, 1, pulse);
		for (int x = -120; x <= 120; x += 24)
		{
			PixelInk.Line(this, new Vector2(x - 4, -24), new Vector2(x, -30), seal, 1, pulse);
			PixelInk.Line(this, new Vector2(x, -30), new Vector2(x + 4, -24), seal, 1, pulse);
		}
		// The latch, lit once both keepers are down (in-flight 101): a gold pool round it and its glint at full, so
		// the last step - touch it - is seen from across the field rather than found. It was the glint alone, at 0.6.
		if (GuardsDown)
		{
			float lit = Breath(clock);
			PixelInk.Disc(this, LatchOffset, 34f, new Color(VaultLight.GoldLit, 0.12f + 0.12f * lit));
			PixelInk.Disc(this, LatchOffset, 18f, new Color(VaultLight.GoldHi, 0.25f + 0.25f * lit));
			DrawGlint(this, LatchOffset, clock, 1f);
		}
	}
	private Vector2 WispAt(float phase, bool inner = false)
	{
		if (inner)
			return CentreOffset + new Vector2(Mathf.Cos(phase * Mathf.Tau) * 540f,
				Mathf.Sin(phase * Mathf.Tau) * 410f - 24f + Mathf.Sin(phase * 80f + clock) * 14f);
		// A rounded rectangular circuit follows the actual walls, rather than cutting across the pen.
		float distance = Mathf.PosMod(phase, 1f) * 6912f;
		Vector2 at;
		if (distance < 1920f) at = new(-960f + distance, -1536f);
		else if ((distance -= 1920f) < 1536f) at = new(960f, -1536f + distance);
		else if ((distance -= 1536f) < 1920f) at = new(960f - distance, 0f);
		else at = new(-960f, -(distance - 1920f));
		return at + new Vector2(Mathf.Sin(phase * Mathf.Tau * 31f + clock * 0.7f) * 12f,
			-48f + Mathf.Cos(clock * 1.6f + phase * 19f) * 18f + Mathf.Cos(phase * Mathf.Tau * 31f) * 10f);
	}
	private void DrawBinding()
	{
		if (GateOpen) return;
		Color blue = new("#6AB8E8"), violet = new("#E8C0FF"), core = new("#DCF4FF");
		Rect2 visible = GetViewportRect().Grow(140f);
		Transform2D canvas = GetViewport().GetCanvasTransform();
		for (int i = 0; i < 32; i++)
		{
			bool inner = i >= 24;
			float phase = inner ? (i - 24) / 8f + clock / 22f : i / 24f + clock / 80f;
			Color body = i % 3 == 0 ? violet : blue;
			Vector2 head = WispAt(phase, inner);
			if (!visible.HasPoint(canvas * (binding.GlobalPosition + head))) continue;
			for (int k = 7; k >= 1; k--)
				PixelInk.Line(binding, WispAt(phase - k * 0.002f, inner), WispAt(phase - (k - 1) * 0.002f, inner), body, k < 3 ? 3 : 1, 1f - k / 9f);
			PixelInk.Disc(binding, head, 15f, body, 0.18f);
			PixelInk.Disc(binding, head, 9f, body, 0.85f);
			PixelInk.Disc(binding, head + new Vector2(-2f, -2f), 4.5f, core);
		}
	}
	private static void SetAnimation(Captive c, string name)
	{
		if (c.Animation == name) return;
		c.Animation = name;
		c.Sprite.Play(name);
	}
	private void TickCaptives(float dt)
	{
		if (State == null) return;
		Vector2 player = Player != null ? ToLocal(Player.GlobalPosition) : Vector2.Inf;
		bool inside = player.X > -900f && player.X < 900f && player.Y > -1480f && player.Y < -48f;
		for (int i = 0; i < CaptiveCount; i++)
		{
			Captive c = captives[i];
			if (c == null) continue;
			if (IsCaptiveFreed(i))
			{
				if (c.Released < 0f || c.Released >= 1f) { c.Sprite.Hide(); continue; }
				c.Released += dt;
				float t = Mathf.Clamp(c.Released / 1.0f, 0f, 1f);
				c.At = c.FreedAt.Lerp(Player != null ? player : c.FreedAt, t);
				c.Material.SetShaderParameter("release_amount", t);
				c.Sprite.Visible = t < 1f;
			}
			else
			{
				Vector2 goal = EscapeGoals[i][c.Goal];
				if (c.Wait > 0f)
				{
					c.Wait -= dt;
					SetAnimation(c, c.Wait > 0.35f ? "pounce" : "idle");
					if (c.Wait <= 0f) c.Goal = (c.Goal + 1) % EscapeGoals[i].Length;
				}
				else
				{
					Vector2 move = goal - c.At;
					c.Sprite.FlipH = move.X < -1f;
					c.At = c.At.MoveToward(goal, (118f + i * 22f) * dt);
					SetAnimation(c, "run");
					if (c.At.DistanceSquaredTo(goal) < 4f) c.Wait = 0.9f;
				}
				if (!State.Done && Active && GateOpen && inside && c.At.DistanceTo(player) <= CompanionReach)
				{
					State.Marks |= 1UL << i;
					c.FreedAt = c.At;
					c.Released = 0f;
					c.Material.SetShaderParameter("ghosted", false);
					SfxPlayer.AtPosition(SfxCatalog.EventReward, GlobalPosition + c.At, 0f);
					if (FreedCount < CaptiveCount)
						rescueToast.Show(i == 0 ? "The hound is free" : i == 1 ? "The cat is free" : "The bird is free",
							$"{FreedCount} of {CaptiveCount} companions freed", "", false);
				}
			}
			c.Sprite.Position = SpellInk.Snap(c.At);
		}
		if (!State.Done && Active && FreedCount == CaptiveCount)
		{
			rescueToast.Hide();
			Open(EssenceCentre);
		}
	}
}
