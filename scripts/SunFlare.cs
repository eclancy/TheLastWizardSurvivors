using Godot;
using System;

namespace TheOneHeMissed.scripts;

// The Eye of Ra's flare (Fireball.FlareEyeOfRa, .ai/chapter-wizards.md 4.6): the sun's light going out of the blast
// past its own reach, once - a white heart, a ring of light running out to exactly the radius it blinds, and rays
// between - then gone. What it is drawn to is what it blinds, the rule every area in the game keeps.
//
// One code-created Node2D a flare, on the air layer and additive, so it lights what it falls on and hides nothing. A
// flare comes on Fireball's own beat (seconds apart), so a node each is no swarm cost.
public partial class SunFlare : Node2D
{
	public const float Seconds = 0.5f;

	private float blast;
	private float reach;
	private float age;

	// The light ramp (bonelight.ELEMENTS["light"]), white to gold: the sun's, not the fire's.
	private static readonly Color Core = new(1f, 1f, 1f);
	private static readonly Color Hot = new(1f, 0.973f, 0.847f);
	private static readonly Color Mid = new(1f, 0.941f, 0.659f);
	private static readonly Color Edge = new(1f, 0.776f, 0.235f);

	public static void Spawn(Node2D arena, Vector2 at, float blastRadius, float flareRadius)
	{
		if (arena == null || !IsInstanceValid(arena))
			return;
		var flare = new SunFlare
		{
			Name = "SunFlare",
			blast = blastRadius,
			reach = flareRadius,
			ZAsRelative = false,
			ZIndex = DrawLayers.Air,
			Material = ExploreArt.Additive,
			TextureFilter = TextureFilterEnum.Linear,
		};
		arena.AddChild(flare);
		flare.GlobalPosition = at;
	}

	public override void _Process(double delta)
	{
		age += (float)delta;
		if (age >= Seconds)
		{
			QueueFree();
			return;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		float t = Mathf.Clamp(age / Seconds, 0f, 1f);
		float fade = 1f - t;
		// The heart: white, the size of the blast, gone fastest.
		ExploreArt.DrawGlow(this, Vector2.Zero, blast * (0.9f + 0.4f * t), new Color(Hot, 0.55f * fade * fade));
		ExploreArt.DrawGlow(this, Vector2.Zero, blast * 0.45f, new Color(Core, 0.7f * fade * fade * fade));
		// The wash over everything it reaches: faint, so the bodies in it are still seen.
		ExploreArt.DrawGlow(this, Vector2.Zero, reach * 1.08f, new Color(Mid, 0.16f * fade));
		// The ring, running out from the blast's edge to the flare's in the first two fifths, then holding there as it fades.
		float run = Mathf.Clamp(t / 0.4f, 0f, 1f);
		float r = Mathf.Lerp(blast, reach, 1f - (1f - run) * (1f - run));
		DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 72, new Color(Mid, 0.7f * fade), 3f);
		DrawArc(Vector2.Zero, r - 4f, 0f, Mathf.Tau, 72, new Color(Edge, 0.35f * fade), 2f);
		// Twelve rays between the heart and the ring, turning a little as they go.
		for (int k = 0; k < 12; k++)
		{
			float a = k * Mathf.Tau / 12f + t * 0.4f;
			Vector2 dir = new(MathF.Cos(a), MathF.Sin(a));
			float inner = blast * 0.5f;
			float outer = Mathf.Lerp(inner, r, k % 2 == 0 ? 1f : 0.7f);
			DrawLine(dir * inner, dir * outer, new Color(k % 2 == 0 ? Hot : Edge, 0.4f * fade), k % 2 == 0 ? 3f : 2f);
		}
	}
}
