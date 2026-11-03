using Godot;
using System;
using TheOneHeMissed.scripts;

// THE SEAL, CARRIED HOME (.ai/endgame-castle.md 6.4). When his hand closes on a spell (Player.SealSpell), the
// spell goes out of the wizard as a dark orb with its own icon inside and is carried back to him - into
// the raised hand the prologue shows him taking the wizards' light with - at about 60 px a second.
//
//   - TOUCH IT and the spell is back at once (Player.ReleaseSeal).
//   - LET IT REACH HIM and the spell stays sealed for twenty seconds more (Player.SealSpell with a clock);
//     the loadout strip's seal drains as they run (LoadoutHud).
//   - HIS FALL lets go of everything he holds: every orb in the air goes back to the wizard.
//
// It first springs out of the wizard toward him and cannot be touched until it lands, or the wizard, who
// it leaves from, would take it back the frame it left him; and it cannot reach him in that first leap,
// or a wizard standing at his feet would lose the spell with no chance to chase it.
//
// Code-only, like DarkHand and TelegraphedGroundHit: the touch and the reach are measured, once a frame.
public partial class SealOrb : Node2D
{
	private ThroneDarkWizardBoss keeper;
	private SpellData spell;
	private Texture2D icon;
	private float driftSpeed;
	private float sealSeconds;
	private Vector2 popFrom;
	private Vector2 popTo;
	private float age;
	private float ending = -1f;
	private bool returned;

	/// <summary>The leap out of the wizard; untouchable, and unable to reach him, until it lands.</summary>
	public const float PopSeconds = 0.6f;
	/// <summary>How close the wizard's body must come to take it back.</summary>
	public const float TouchRadius = 34f;
	/// <summary>How close to his raised hand counts as home.</summary>
	public const float ReachRadius = 30f;
	private const float EndSeconds = 0.35f;

	/// <summary>The spell it carries.</summary>
	public SpellData Spell => spell;

	/// <summary>True once it has been touched, reached him, or been let go.</summary>
	public bool Spent => ending >= 0f;

	/// <summary>
	/// Sends <paramref name="sealedSpell"/>'s seal out of <paramref name="wizard"/> toward
	/// <paramref name="boss"/>. Its leap is <paramref name="popDistance"/> long, or half the way to him if
	/// he is closer than twice that.
	/// </summary>
	public static SealOrb Send(Node arena, ThroneDarkWizardBoss boss, Player wizard, SpellData sealedSpell,
		float popDistance, float driftSpeed, float sealSeconds)
	{
		if (arena == null || !IsInstanceValid(arena) || wizard == null || sealedSpell == null)
			return null;
		Vector2 from = wizard.GlobalPosition;
		Vector2 home = boss != null && IsInstanceValid(boss) ? boss.HandPoint : from + Vector2.Up * 300f;
		Vector2 toward = home - from;
		float pop = Mathf.Min(popDistance, toward.Length() * 0.5f);
		var orb = new SealOrb
		{
			Name = "SealOrb",
			keeper = boss,
			spell = sealedSpell,
			icon = EvolutionIcons.Current(sealedSpell) ?? sealedSpell.Icon,
			driftSpeed = driftSpeed,
			sealSeconds = sealSeconds,
			popFrom = from,
			popTo = from + (toward.LengthSquared() > 1f ? toward.Normalized() : Vector2.Up) * pop,
			// In the air over every body, as the hand that took it was: it must be findable in a crowd.
			ZAsRelative = false,
			ZIndex = DrawLayers.Air,
		};
		arena.AddChild(orb);
		orb.GlobalPosition = from;
		return orb;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		age += dt;
		QueueRedraw();

		if (ending >= 0f)
		{
			ending += dt;
			if (ending >= EndSeconds)
				QueueFree();
			return;
		}

		Player wizard = GetTree().GetFirstNodeInGroup("player") as Player;
		// Lifted some other way (a Penknife cut the spell, the wizard fell): nothing left to carry.
		if (wizard == null || wizard.IsDead || !wizard.IsSealed(spell))
		{
			End(false);
			return;
		}
		// He fell: what he held goes back.
		if (keeper == null || !IsInstanceValid(keeper) || keeper.IsDying)
		{
			wizard.ReleaseSeal(spell);
			End(true);
			return;
		}

		if (age < PopSeconds)
		{
			float u = age / PopSeconds;
			GlobalPosition = popFrom.Lerp(popTo, 1f - (1f - u) * (1f - u));
			return;
		}

		if (wizard.GlobalPosition.DistanceTo(GlobalPosition) <= TouchRadius)
		{
			wizard.ReleaseSeal(spell);
			SfxPlayer.Global(SfxCatalog.SpellEvolve);
			keeper.OnSealTakenBack(spell);
			End(true);
			return;
		}

		Vector2 home = keeper.HandPoint;
		Vector2 to = home - GlobalPosition;
		if (to.Length() <= ReachRadius)
		{
			wizard.SealSpell(spell, sealSeconds);
			SfxPlayer.AtPosition(SfxCatalog.UiDenied, GlobalPosition, 0.02f);
			keeper.OnSealReachedHim(spell);
			End(false);
			return;
		}
		GlobalPosition += to.Normalized() * Mathf.Min(to.Length(), driftSpeed * dt);
	}

	private void End(bool toWizard)
	{
		if (ending >= 0f)
			return;
		ending = 0f;
		returned = toWizard;
	}

	// A dark orb on the 2 px grid with his ramp for a rim, the spell's own icon inside it dimmed as a thing
	// held, and a slow pulse so it reads as alive in a crowd. Going home to the wizard it flares and grows;
	// taken into his hand it shrinks to a point.
	public override void _Draw()
	{
		ElementColors.Ramp dark = ElementColors.GetRamp(Element.Darkness);
		float u = ending >= 0f ? Mathf.Clamp(ending / EndSeconds, 0f, 1f) : 0f;
		float scale = ending < 0f ? Mathf.Clamp(age / 0.2f, 0.3f, 1f) : returned ? 1f + 0.8f * u : 1f - u;
		float alpha = ending < 0f ? 1f : 1f - u;
		if (alpha <= 0f || scale <= 0f)
			return;
		float pulse = 0.5f + 0.5f * MathF.Sin(age * 5.5f);
		float r = 25f * scale;
		BossDeath.Disc(this, Vector2.Zero, r + 3f, r + 3f, new Color(dark.Edge, 0.85f * alpha));
		BossDeath.Disc(this, Vector2.Zero, r, r, new Color(0.05f, 0.02f, 0.09f, 0.94f * alpha));
		BossDeath.Ring(this, Vector2.Zero, r + 1f, r + 1f, BossDeath.Px, new Color(dark.Mid.Lerp(dark.Hot, pulse), alpha));
		if (icon != null)
		{
			float side = 32f * scale;
			var tint = returned ? new Color(1f, 1f, 1f, alpha) : new Color(0.72f, 0.66f, 0.86f, alpha);
			DrawTextureRect(icon, new Rect2(-side * 0.5f, -side * 0.5f, side, side), false, tint);
		}
		// Two motes of his light circling it: the hold he has on it.
		for (int k = 0; k < 2; k++)
		{
			float a = age * 3.1f + k * Mathf.Pi;
			Vector2 at = BossDeath.Snap(new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.7f) * (r + 6f));
			DrawRect(WhipLimb.Cell(at, BossDeath.Px * 2f), new Color(dark.Hot, alpha));
		}
	}
}
