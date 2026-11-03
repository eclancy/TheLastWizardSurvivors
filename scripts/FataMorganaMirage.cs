using Godot;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

// FATA MORGANA (Morgan le Fay's Apotheosis of Sleet Runes, verb "double"; .ai/chapter-wizards.md 4.9).
//
// "A mirage of you walks beside you facing the other way and casts behind you. Enemies near it chase it." The
// mirage over the strait bears her name: a ship hung shimmering in the air where no ship is. Hers is of the
// player - his own figure, the frame he is on, turned the other way and drawn in the cold light of her runes,
// shimmering - walking SideDistance beside him.
//
// WHAT IT IS, in the code: a second CASTING ORIGIN. Every Sleet Runes cast is let go twice: once from his hand
// along his facing, and once from the mirage's hand along its own, which is always the other way
// (SleetRunes.Cast). The same runes, at the same strength - the verb is double - so a stream in front of him is
// a stream behind him too, and the crowd he is walking away from is the crowd it is walking into.
//
// AND A LURE. Every LureEvery seconds whatever ordinary body is within LureRadius of it, and nearer it than him,
// is lured for LureSeconds (Enemy.ApplyLure): it chases the mirage instead of him, and keeps chasing it for as
// long as it stays near - so half of what is round him walks after a ghost. A boss is never lured
// (Enemy.ApplyLure refuses it: the CC rule). Elites are; a lure turns where a body walks, not whether it fights.
//
// NOT A BODY. It joins no group, has no collision and cannot be struck; nothing targets it but the lure. It walks
// over the drop as easily as over the deck - it is a mirage.
//
// One node, made by the player when the form is taken (Player.Mirage.cs) and freed with the spell; it draws
// itself in the arena's y-sort, so it stands among the bodies at its feet. Nothing allocated per frame: the lure
// reads the swarm's grid into one reused list.
public partial class FataMorganaMirage : Node2D
{
	/// <summary>How far beside him it walks, world px: a step and a half away, a body's width clear of him.</summary>
	public const float SideDistance = 150f;

	/// <summary>How fast it walks to its place beside him when he turns or runs: it walks, it never jumps.</summary>
	public const float WalkSpeed = 560f;

	/// <summary>What is this near it, and nearer it than him, chases it.</summary>
	public const float LureRadius = 190f;

	/// <summary>How long one look at it lures for; refreshed every <see cref="LureEvery"/> while the body stays near.</summary>
	public const float LureSeconds = 0.6f;

	public const float LureEvery = 0.15f;

	/// <summary>Where a rune leaves its hand: as far out along its facing as from his (SleetRunes.HandReach).</summary>
	public const float HandReach = 18f;

	public Player PlayerRef { get; set; }

	/// <summary>Runes let go from its hand since the run began (the harness's evidence the form casts).</summary>
	public static int RunesCast { get; private set; }

	/// <summary>Lures applied since the run began, one per body per look (the harness's evidence it lures).</summary>
	public static int Lures { get; private set; }

	public static void ResetCounts()
	{
		RunesCast = 0;
		Lures = 0;
	}

	internal static void CountRune() => RunesCast++;

	/// <summary>The way it faces: always the other way from him.</summary>
	public Vector2 Facing => PlayerRef != null && IsInstanceValid(PlayerRef) ? -PlayerRef.FacingDirection : Vector2.Left;

	/// <summary>Where a rune leaves it.</summary>
	public Vector2 Hand => GlobalPosition + Facing * HandReach;

	private float lureClock;
	private readonly List<Enemy> near = new();
	private bool placed;

	// Ice's cold light, as the runes are: the mirage is LIGHT, not a second figure - his sprite drawn additively and
	// overbright in ice's pale cyan, so a dark robe comes out as a pale shimmer and nothing of his pigment is in it.
	// Drawn multiplied (a first build), a wizard in deep cloth came out as a dark smudge nobody could find.
	private static readonly Color Tint = new(1.7f, 2.4f, 2.7f, 0.8f);

	public override void _Ready()
	{
		// Among the bodies: in the arena's y-sort, at its feet - and added, as the events' lights are.
		Material = ExploreArt.Additive;
		TextureFilter = TextureFilterEnum.Nearest;
	}

	/// <summary>
	/// Where it wants to stand: beside him, square to his facing, on whichever side of him it already is - so a
	/// player who turns about (a kite turns about every few steps) keeps it where it was, and a quarter turn walks
	/// it a quarter of the way round him. The first build put it on his right hand as he faced, and every
	/// reversal sent it through him to the other side: it spent most of a kite crossing him.
	/// </summary>
	public Vector2 Place()
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return GlobalPosition;
		Vector2 f = PlayerRef.FacingDirection.LengthSquared() > 0.0001f ? PlayerRef.FacingDirection.Normalized() : Vector2.Right;
		Vector2 side = new Vector2(-f.Y, f.X);
		if (placed && (GlobalPosition - PlayerRef.GlobalPosition).Dot(side) < 0f)
			side = -side;
		return PlayerRef.GlobalPosition + side * SideDistance;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (PlayerRef == null || !IsInstanceValid(PlayerRef))
			return;
		float dt = (float)delta;
		Vector2 want = Place();
		GlobalPosition = placed ? GlobalPosition.MoveToward(want, WalkSpeed * dt) : want;
		placed = true;

		lureClock -= dt;
		if (lureClock > 0f || PlayerRef.HeldForBossEntrance)
			return;
		lureClock = LureEvery;
		Vector2 here = GlobalPosition;
		Vector2 him = PlayerRef.GlobalPosition;
		EnemyRegistry.CollectNear(here, LureRadius, near);
		foreach (Enemy enemy in near)
		{
			if (enemy == null || !IsInstanceValid(enemy) || enemy.IsDying || enemy is BossEnemy)
				continue;
			Vector2 p = enemy.GlobalPosition;
			float toMirage = p.DistanceSquaredTo(here);
			if (toMirage > LureRadius * LureRadius || toMirage > p.DistanceSquaredTo(him))
				continue;
			enemy.ApplyLure(here, LureSeconds);
			Lures++;
		}
		near.Clear();
	}

	public override void _Process(double delta) => QueueRedraw();

	// His own figure on the frame he is on, turned the other way, in bands of four art rows each nudged a pixel
	// or two sideways on the shimmer - the heat-haze a mirage hangs in.
	public override void _Draw()
	{
		AnimatedSprite2D body = PlayerRef?.BodySprite;
		if (body?.SpriteFrames == null || !body.SpriteFrames.HasAnimation(body.Animation))
			return;
		Texture2D frame = body.SpriteFrames.GetFrameTexture(body.Animation, body.Frame);
		if (frame == null)
			return;
		Vector2 size = frame.GetSize();
		Vector2 scale = body.Scale;
		float t = Time.GetTicksMsec() / 1000f;
		bool flip = !body.FlipH;
		Vector2 topLeft = body.Position - size * scale * 0.5f;
		// Turned the other way: mirrored about its own middle (the body sprite sits on the node's x).
		if (flip)
			DrawSetTransform(Vector2.Zero, 0f, new Vector2(-1f, 1f));
		int bands = Mathf.CeilToInt(size.Y / 4f);
		for (int b = 0; b < bands; b++)
		{
			float shimmer = Mathf.Round(Mathf.Sin(t * 3.4f + b * 1.3f) * 1.2f) * 2f;
			float rowY = b * 4f;
			float rows = Mathf.Min(4f, size.Y - rowY);
			var dst = new Rect2(topLeft.X + shimmer, topLeft.Y + rowY * scale.Y, size.X * scale.X, rows * scale.Y);
			var src = new Rect2(0f, rowY, size.X, rows);
			float a = Tint.A * (0.82f + 0.18f * Mathf.Sin(t * 2.1f + b));
			DrawTextureRectRegion(frame, dst, src, new Color(Tint.R, Tint.G, Tint.B, a));
		}
		if (flip)
			DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
	}
}
