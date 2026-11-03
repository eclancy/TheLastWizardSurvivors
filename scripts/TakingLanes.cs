using Godot;
using System;
using TheOneHeMissed.scripts;

// HIS LIGHT (.ai/dark-wizard-playable.md section 3): The Taking's third verb, what a caught light throws.
// Two lanes of his dark light cross where the wizard caught it - one along the way the light came home,
// which runs back into the crowd it was taken from, and one across it - and everything standing on either
// is struck once. The boss's lanes in the dark (ThroneDarkWizardBoss, phase 2), his own now, and with no
// tell: they are his, and they do not fall on him.
//
//   - Eightfold Cross: a star of four lanes, the second pair turned an eighth.
//   - All He Took (the Apotheosis, take): whatever a lane kills gives up its light too (TakenLight.Loose),
//     to go home and be caught in its turn - at most TakenLight.MaxInAir in the air.
//
// Struck on the frame it is made and then only drawn: one pass over EnemyRegistry.Snapshot, nothing held,
// nothing allocated a frame (.ai/performance.md).
public partial class TakingLanes : Node2D
{
	/// <summary>A body counts as on a lane while its centre is this much past the lane's edge.</summary>
	public const float BodyAllowance = 10f;
	private const float ShowSeconds = 0.45f;

	/// <summary>
	/// The most crosses standing at once. At level 8 three hands each close on four bodies, and their twelve
	/// lights come home together: twelve crosses on one spot drew a sunburst of forty-eight lanes, hid the
	/// field and cost twelve thousand cells a frame. Past the cap a caught light strikes down the lanes of the
	/// newest cross again and relights it, so every light still pays and the screen holds a few crosses.
	/// </summary>
	public const int MaxStanding = 4;

	private static int standing;
	private static TakingLanes newest;
	private bool counted;

	private Vector2 along = Vector2.Right;
	private int laneCount = 2;
	private float length;
	private float width;
	private float age;

	/// <summary>Throws the cross at <paramref name="at"/>, its first lane along <paramref name="homeward"/>.</summary>
	public static TakingLanes Cross(Node arena, Player caster, Vector2 at, Vector2 homeward, TakenLight.Setup setup)
	{
		if (arena == null || !IsInstanceValid(arena) || caster == null || !IsInstanceValid(caster))
			return null;
		if (standing >= MaxStanding && newest != null && IsInstanceValid(newest) && !newest.IsQueuedForDeletion())
		{
			newest.age = 0f;
			newest.Strike(caster, setup);
			return newest;
		}
		var lanes = new TakingLanes
		{
			Name = "TakingLanes",
			along = homeward.LengthSquared() > 0.0001f ? homeward.Normalized() : Vector2.Right,
			laneCount = setup.Star ? 4 : 2,
			length = setup.LaneLength,
			width = setup.LaneWidth,
			ZAsRelative = false,
			ZIndex = DrawLayers.Air,
		};
		arena.AddChild(Player.TagSpell(lanes));
		lanes.GlobalPosition = at;
		lanes.Strike(caster, setup);
		SfxPlayer.AtPosition(SfxCatalog.SpellBeamStart, at, 0.05f);
		return lanes;
	}

	// The lane directions: the first along the light's way home, the rest an even turn apart round a half
	// circle (two lanes: square; four: a star).
	private Vector2 Lane(int i) => along.Rotated(Mathf.Pi * i / laneCount);

	private void Strike(Player caster, TakenLight.Setup setup)
	{
		Vector2 at = GlobalPosition;
		float half = width * 0.5f + BodyAllowance;
		foreach (Enemy body in EnemyRegistry.Snapshot)
		{
			if (body == null || !IsInstanceValid(body) || body.IsDying || body.Health <= 0)
				continue;
			Vector2 rel = body.GlobalPosition - at;
			if (rel.LengthSquared() > (length + BodyAllowance) * (length + BodyAllowance))
				continue;
			bool onLane = false;
			for (int i = 0; i < laneCount && !onLane; i++)
			{
				Vector2 dir = Lane(i);
				onLane = MathF.Abs(rel.Dot(dir)) <= length && MathF.Abs(rel.Cross(dir)) <= half;
			}
			if (!onLane)
				continue;
			Vector2 fellAt = body.GlobalPosition;
			caster.DealDamageToEnemy(body, setup.LaneDamage, 0f, true, setup.Spell);
			if (setup.AllHeTook && (!IsInstanceValid(body) || body.IsDying || body.Health <= 0))
				TakenLight.Loose(GetParent(), caster, fellAt, setup);
		}
	}

	public override void _EnterTree()
	{
		if (!counted)
		{
			counted = true;
			standing++;
		}
		newest = this;
	}

	public override void _ExitTree()
	{
		if (counted)
		{
			counted = false;
			standing = Math.Max(0, standing - 1);
		}
		if (newest == this)
			newest = null;
	}

	public override void _PhysicsProcess(double delta)
	{
		age += (float)delta;
		QueueRedraw();
		if (age >= ShowSeconds)
			QueueFree();
	}

	// Each lane a band of his dark light on the 2 px grid, out to exactly the length and width it struck:
	// the edge of his ramp at the rim, its mid inside, a hot line down the middle that burns to the core in
	// the first instant. Every colour opaque - overlapping cells of one colour cannot stack into a darker
	// one - so it goes out the pixel way: the band thins, the hot line drops to the mid, and in its last
	// third the cells go out every other one, from the ends inward.
	public override void _Draw()
	{
		ElementColors.Ramp dark = ElementColors.GetRamp(Element.Darkness);
		float u = Mathf.Clamp(age / ShowSeconds, 0f, 1f);
		if (u >= 1f)
			return;
		float reach = length * (1f - 0.35f * u);
		float band = width * (1f - 0.6f * u);
		bool flash = age < 0.08f;
		bool dither = u > 0.66f;
		for (int i = 0; i < laneCount; i++)
		{
			Vector2 dir = Lane(i);
			Vector2 a = -dir * reach;
			Vector2 b = dir * reach;
			if (band >= BossDeath.Px * 2f)
				Band(this, a, b, band, dark.Edge, dither);
			Band(this, a, b, Mathf.Max(BossDeath.Px, band * 0.5f), u < 0.4f ? dark.Mid : dark.Edge, dither);
			Band(this, a, b, BossDeath.Px * 2f, flash ? dark.Core : u < 0.5f ? dark.Hot : dark.Mid, dither);
		}
		// Where it was caught: the light's own warmth, going out.
		if (u < 0.5f)
		{
			ElementColors.Ramp light = ElementColors.GetRamp(Element.Light);
			BossDeath.Disc(this, Vector2.Zero, 10f * (1f + u), 10f * (1f + u), light.Mid);
			DrawRect(WhipLimb.Cell(Vector2.Zero, BossDeath.Px * 3f), light.Core);
		}
	}

	// A straight band of square cells on the art grid, each <paramref name="size"/> across, stepped half a
	// cell at a time so a wide band is solid at any angle, and a whole cell at a time for the thin hot line
	// (4 px cells that abut). About a hundred and fifty cells a lane at level 8 where a 2 px line of squares
	// would be four hundred - and a late run stands several crosses at once (Many Hands, All He Took).
	private static void Band(CanvasItem canvas, Vector2 a, Vector2 b, float size, Color color, bool dither)
	{
		size = WhipLimb.SnapSize(size);
		float step = size <= BossDeath.Px * 2f ? size : Mathf.Max(BossDeath.Px, WhipLimb.SnapSize(size * 0.5f));
		int steps = Math.Max(1, (int)(a.DistanceTo(b) / step));
		for (int i = 0; i <= steps; i++)
		{
			// Going out from the ends: the outer cells drop first.
			if (dither && (i % 2 == 1 || Math.Min(i, steps - i) < steps / 6))
				continue;
			canvas.DrawRect(WhipLimb.Cell(a.Lerp(b, i / (float)steps), size), color);
		}
	}
}
