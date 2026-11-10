using Godot;
using System;
using TheOneHeMissed.scripts;

// THE WADE, SEEN (in-flight 74, Eric 2026-11-09: "When walking over water, we should make the feet of the wizard sink
// in (if they walk on the ground)"). Wading used to be only a slow (MovePlayer, IsWading) and nothing changed in how he
// looked. Now, while his boots are in the water - or in the Swallow-Hole's mud (NoteMudUnderfoot) - his body sinks in:
// the figure drops SinkDropTexels art pixels and is cut off at a waterline WaterlineRiseTexels above where his boots
// stood, so the boots and shins are under the water (about eight rows of the 48 cell, the contact line under them
// included - which is his shadow gone), with a line of the water's glint where he meets it. It eases in and out over
// SinkEaseSeconds, one whole art pixel at a time. The cut is in the body's own shader
// (scenes/shaders/invincibility_white.gdshader): the cut part is discarded, never faded, and the body stays on the
// Foremost layer - only the bottom of it is gone. The Brazier's and Taproot's gauge at his feet (StanceGlow) is hidden
// while he is in; rings round him are WadeRings'.
//
// WHO DOES NOT SINK - "if they walk on the ground". Wayland, whose wings hold him whenever he is over water or mud
// (Winged, Player.Wings.cs): no sinking and no rings, he does not touch it. Baba Yaga and Nimue RIDE OVER THE WATER (a
// trait, CharacterData.RidesOverWater; Eric: "Baba Yaga and Nimue should also ignore being slowed by water"): her mortar
// floats and the Lady of the Lake walks on it - no sinking, no slow, rings at their feet. One flag answers both "does
// she sink" and "is she slowed", here and in MovePlayer and the Swallow-Hole.
public partial class Player
{
	/// <summary>
	/// True for a wizard who stays on top of the water (CharacterData.RidesOverWater): never slowed by wading, never sunk
	/// in it. Read beside Winged wherever the water or the mud asks who is in it.
	/// </summary>
	public bool RidesOverWater { get; private set; }

	/// <summary>How far he is sunk, 0 (dry) to 1 (in to the shins), eased. The ground marks at his feet hide while above 0.</summary>
	public float WadeSink => wadeSink;

	// The look's numbers, in the body's own texels (x2 on screen). Boots and shins: the figure drops four and the water
	// stands four above where the boots stood, so the bottom eight rows of the 48 cell are under it.
	private const int SinkDropTexels = 4;
	private const int WaterlineRiseTexels = 4;
	// "Eased in and out over about a sixth of a second."
	private const float SinkEaseSeconds = 1f / 6f;

	private float wadeSink;
	private int appliedDrop = -1;
	private int appliedRise = -1;
	private ShaderMaterial? wadeMaterial;
	private WadeRings? wadeRings;
	// The physics frame the Swallow-Hole's mud last found his boots in it; -2 while it never has.
	private long mudUnderfootFrame = -2;

	private static readonly StringName ParamSinkDrop = "sink_drop";
	private static readonly StringName ParamWaterline = "waterline";
	private static readonly StringName ParamWaterlineInk = "waterline_ink";
	private static readonly StringName ParamWhiten = "whiten";
	// The shader's "nothing is cut" waterline.
	private const float NoWaterline = 100000f;

	/// <summary>
	/// Told by the Swallow-Hole on a physics frame his boots are in its mud: he sinks in it as in water. Its step may run
	/// before or after his own, so a stamp from this frame or the last counts (the trap stamp's rule, FliesOverTrap).
	/// </summary>
	public void NoteMudUnderfoot() => mudUnderfootFrame = (long)Engine.GetPhysicsFrames();

	// Water at his boots (IsWading, the one point query MovePlayer makes) or the vault's mud.
	private bool FeetInWater => IsWading || (long)Engine.GetPhysicsFrames() - mudUnderfootFrame <= 1;

	// From PhysicsTick, after MovePlayer has said whether he is wading.
	private void TickWading(float delta)
	{
		bool inWater = FeetInWater && !Winged;
		bool sinks = inWater && !RidesOverWater;
		wadeSink = Mathf.MoveToward(wadeSink, sinks ? 1f : 0f, delta / SinkEaseSeconds);

		if (inWater && wadeRings == null)
		{
			wadeRings = new WadeRings { Name = "WadeRings", PlayerRef = this };
			AddChild(wadeRings);
		}
		if (wadeRings != null)
			wadeRings.Wading = inWater;

		float eased = wadeSink * wadeSink * (3f - 2f * wadeSink);
		ApplySink(Mathf.RoundToInt(eased * SinkDropTexels), Mathf.RoundToInt(eased * WaterlineRiseTexels));
	}

	// Writes the cut to the body's materials, only on the frames a whole texel of it changes.
	private void ApplySink(int drop, int rise)
	{
		if (bodySprite == null || (drop == appliedDrop && rise == appliedRise))
			return;
		appliedDrop = drop;
		appliedRise = rise;
		if (wadeMaterial == null)
		{
			if (drop == 0 && rise == 0)
				return;
			wadeMaterial = new ShaderMaterial { Shader = InvincibilityShader };
			wadeMaterial.SetShaderParameter(ParamWhiten, 0f);
			wadeMaterial.SetShaderParameter(ParamWaterlineInk, wadeRings?.Glint ?? Colors.White);
			invincibilityMaterial?.SetShaderParameter(ParamWaterlineInk, wadeRings?.Glint ?? Colors.White);
			// Under a white grace, the white keeps the body and hands it back to this material when it ends.
			if (invincibilityVisualActive)
				materialBeforeInvincibility ??= wadeMaterial;
			else if (bodySprite.Material == null)
				bodySprite.Material = wadeMaterial;
		}

		float waterline = drop == 0 && rise == 0 ? NoWaterline : CellBottomTexels() - rise;
		SetSinkOn(wadeMaterial, drop, waterline);
		SetSinkOn(invincibilityMaterial, drop, waterline);
	}

	// The white grace's material, made after he was already in: the same cut and ink as the ordinary one carries.
	private void SyncWadeTo(ShaderMaterial material)
	{
		if (wadeMaterial == null)
			return;
		material.SetShaderParameter(ParamWaterlineInk, wadeMaterial.GetShaderParameter(ParamWaterlineInk));
		material.SetShaderParameter(ParamSinkDrop, wadeMaterial.GetShaderParameter(ParamSinkDrop));
		material.SetShaderParameter(ParamWaterline, wadeMaterial.GetShaderParameter(ParamWaterline));
	}

	private static void SetSinkOn(ShaderMaterial? material, int drop, float waterline)
	{
		if (material == null)
			return;
		material.SetShaderParameter(ParamSinkDrop, (float)drop);
		material.SetShaderParameter(ParamWaterline, waterline);
	}

	// The bottom edge of the body's cell in its own texels: where every sheet stands its boots (FitFeetToSprite's rule).
	private float CellBottomTexels()
	{
		Texture2D? frame = bodySprite?.SpriteFrames != null && bodySprite.SpriteFrames.GetFrameCount(bodySprite.Animation) > 0
			? bodySprite.SpriteFrames.GetFrameTexture(bodySprite.Animation, 0)
			: null;
		if (bodySprite == null || frame == null)
			return 24f;
		return bodySprite.Centered ? bodySprite.Offset.Y + frame.GetHeight() / 2f : bodySprite.Offset.Y + frame.GetHeight();
	}
}
