using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// A walkable, magically sealed pound. Opening its gate reveals a separate center pickup.
public partial class Pinfold : SecretVault
{
	public const float WakeRadius = 520f;
	public const float LatchReach = 64f;
	public const float EssenceReach = 52f;
	public const float GuardHealthMultiplier = 10f;
	public static readonly BestiaryRole[] GuardRoles = { BestiaryRole.Wall, BestiaryRole.Slammer };
	private static readonly Vector2 PoundCell = new(448f, 352f);
	private static readonly Vector2 PoundAnchor = new(-224f, -328f);
	private static readonly Vector2 LatchOffset = new(68f, -24f);
	private static readonly Vector2 CentreOffset = new(0f, -124f);
	private static readonly Rect2[] Walls =
	{
		new(-212f, -256f, 424f, 16f), new(-212f, -248f, 16f, 256f),
		new(196f, -248f, 16f, 256f), new(-212f, -8f, 148f, 16f), new(64f, -8f, 148f, 16f),
	};
	private static readonly SecretFarTell Tell = new()
	{
		Textures = new[] { "res://assets/bonelight/world/fields/hurdle-broken-1.png", "res://assets/bonelight/world/fields/hurdle-broken-2.png", "res://assets/bonelight/world/fields/pawprints-1.png" },
		NearTextures = new[] { "res://assets/bonelight/world/fields/pawprints-1.png", "res://assets/bonelight/world/fields/pawprints-2.png" },
		InnerRadius = SecretCatalog.VaultFootprint, NearRadius = 1536f, OuterRadius = 3072f,
		PeakPerChunk = 40, NearShare = 0.75f, FootFromBottom = 4f, LeanToward = true,
	};
	public override SecretFarTell FarTell => Tell;
	public override Vector2 TellPosition => EssenceCentre;
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
	private Texture2D pound;
	private Texture2D essence;
	private Node2D back;
	private Node2D energy;
	private CollisionShape2D gateCollider;
	private float clock;
	private float absorbed = -1f;

	public override void _Ready()
	{
		YSortEnabled = true;
		pound = GD.Load<Texture2D>("res://assets/bonelight/world/fields/pinfold.png");
		essence = GD.Load<Texture2D>("res://assets/bonelight/world/fields/pinfold-essence.png");
		var body = new StaticBody2D { Name = "Walls", CollisionLayer = 1u << (LevelTilePainter.TerrainCollisionLayer - 1), CollisionMask = 0 };
		foreach (Rect2 wall in Walls)
			body.AddChild(new CollisionShape2D { Position = wall.GetCenter(), Shape = new RectangleShape2D { Size = wall.Size } });
		gateCollider = new CollisionShape2D { Name = "Gate", Shape = new RectangleShape2D { Size = new Vector2(128f, 16f) } };
		body.AddChild(gateCollider);
		AddChild(body);
		// Separate sort origins: back/floor before someone INSIDE, front wall after them.
		back = new Node2D { Name = "Back", Position = new Vector2(0f, -248f) };
		back.Draw += DrawBack;
		AddChild(back);
		energy = new Node2D { Name = "Essence", Position = CentreOffset };
		energy.Draw += DrawEnergy;
		AddChild(energy);
	}
	public override void _ExitTree()
	{
		if (back != null && IsInstanceValid(back)) back.Draw -= DrawBack;
		if (energy != null && IsInstanceValid(energy)) energy.Draw -= DrawEnergy;
		base._ExitTree();
	}
	public override void _Process(double delta)
	{
		clock += (float)delta;
		if (absorbed >= 0f)
		{
			absorbed += (float)delta;
			if (Player != null) energy.Position = CentreOffset.Lerp(ToLocal(Player.GlobalPosition), Mathf.SmoothStep(0f, 1f, Mathf.Min(absorbed / 0.7f, 1f)));
			energy.Visible = absorbed < 0.7f;
		}
		else if (Player != null && State != null && !State.Done && Active)
		{
			if (State.Step == 0 && Player.GlobalPosition.DistanceTo(GlobalPosition) <= WakeRadius) Wake();
			if (State.Step == 1 && guards.TrueForAll(IsDown)) State.Step = 2;
			if (State.Step == 2 && Player.GlobalPosition.DistanceTo(Latch) <= LatchReach) LiftLatch();
			if (GateOpen && Player.GlobalPosition.DistanceTo(EssenceCentre) <= EssenceReach)
			{
				absorbed = 0f;
				Open(EssenceCentre);
				SfxPlayer.AtPosition(SfxCatalog.EventReward, EssenceCentre, 0f);
			}
		}
		if (Player != null && Player.GlobalPosition.DistanceSquaredTo(GlobalPosition) < 1600f * 1600f)
		{
			QueueRedraw();
			energy?.QueueRedraw();
		}
	}
	private static bool IsDown(Enemy guard) => guard == null || !IsInstanceValid(guard) || guard.IsDying || guard.Health <= 0;
	private void Wake()
	{
		State.Step = 1;
		guards.Clear();
		for (int i = 0; i < GuardRoles.Length; i++)
		{
			Vector2 at = Director.Game.ReachablePoint(GlobalPosition + new Vector2(i == 0 ? -300f : 300f, 64f));
			Enemy guard = Director.Game.SpawnEventEnemy(GuardRoles[i], at, GuardHealthMultiplier, true);
			if (guard != null) guards.Add(guard);
		}
		SfxPlayer.AtPosition(SfxCatalog.EliteSpawn, GlobalPosition, 0f);
		if (guards.Count == 0) State.Step = 2;
	}
	private void LiftLatch()
	{
		State.Step = 3;
		gateCollider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		SfxPlayer.AtPosition(SfxCatalog.EventReward, Latch, 0f);
	}
	/// <summary>Open only the gate for a photograph; the essence still has to be approached.</summary>
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
		DrawTextureRectRegion(pound, new Rect2(PoundAnchor, PoundCell), new Rect2(new Vector2((GateOpen ? 2 : 1) * PoundCell.X, 0f), PoundCell));
		DrawSetTransform(Vector2.Zero);
		if (GateOpen) return;
		Color seal = new("#6AB8E8");
		float pulse = 0.4f + 0.5f * Breath(clock);
		PixelInk.Line(this, new Vector2(-60f, -34f), new Vector2(60f, -6f), seal, 1, pulse);
		PixelInk.Line(this, new Vector2(-60f, -6f), new Vector2(60f, -34f), seal, 1, pulse);
		for (int x = -60; x <= 60; x += 24)
		{
			PixelInk.Line(this, new Vector2(x - 4, -24), new Vector2(x, -30), seal, 1, pulse);
			PixelInk.Line(this, new Vector2(x, -30), new Vector2(x + 4, -24), seal, 1, pulse);
		}
		if (GuardsDown) DrawGlint(this, LatchOffset, clock, 0.6f);
	}
	private void DrawEnergy()
	{
		if (essence == null || (IsOpened && absorbed < 0f)) return;
		SpellInk.AlignToWorldGrid(energy);
		int frame = (int)(clock * 8f) % 8;
		energy.DrawTextureRectRegion(essence, new Rect2(-64f, -102f, 128f, 128f), new Rect2(frame * 128f, 0f, 128f, 128f));
		energy.DrawSetTransform(Vector2.Zero);
	}
}
