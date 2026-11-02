using Godot;
using System;
using WizardSurvivors.scripts;

/// <summary>
/// The anointed spell's strikes, crowned where they land (.ai/sovereign-spell.md section 5). Most casts carry
/// their corona on the node they make (SovereignCorona, through Player.ApplyLegendaryVisual); the spells that
/// draw their own strikes in a layer of their own - Starfall's stars, the Familiar's bites, the Spiritual
/// Weapon's blows, Stormsweep's lashes, Sleet Runes, Frost Shard's shatters, Sagittarius's rain - have no node
/// to crown, so every hit the anointed spell deals is crowned instead, at the body it struck: a small ring in
/// the spell's ramp that throws itself outward and goes, and a mote left rising.
/// </summary>
/// <remarks>
/// One node in the arena for the run (<see cref="For"/>), a fixed pool of flares and nothing allocated while it
/// plays: a hit takes the oldest slot, and hits closer together than <see cref="MinGap"/> are not marked, so a
/// spell that hits forty bodies in one frame draws a few rings, not forty. Player.DealDamageToEnemy marks them.
/// </remarks>
public partial class SovereignHits : Node2D
{
	private const int Capacity = 16;
	private const float Life = 0.32f;
	private const float MinGap = 0.05f;
	private const int StartRadius = 4;
	private const int Reach = 5;
	private const int FlashReach = 22;
	private const float Texel = 2f;
	private const float MoteRise = 22f;

	private readonly Vector2[] at = new Vector2[Capacity];
	private readonly float[] age = new float[Capacity];
	// How far a flare throws its ring, in texels: a hit's few, the Rite's first cast's many.
	private readonly int[] reach = new int[Capacity];
	private int next;
	private float sinceLast = MinGap;
	private Element element;
	private ElementColors.Ramp ramp;

	private static readonly Color Gilt = new(1.0f, 0.784f, 0.282f);

	/// <summary>The arena's layer for this element, made the first time a hit is marked.</summary>
	public static SovereignHits For(Node arena, Element element)
	{
		if (arena == null)
			return null;
		if (arena.GetNodeOrNull<SovereignHits>("SovereignHits") is SovereignHits existing)
			return existing;
		var layer = new SovereignHits
		{
			Name = "SovereignHits",
			element = element,
			ZAsRelative = false,
			ZIndex = DrawLayers.Air + 1,
			TextureFilter = TextureFilterEnum.Nearest,
		};
		arena.AddChild(layer);
		return layer;
	}

	public override void _Ready()
	{
		ramp = ElementColors.GetRamp(element);
		for (int i = 0; i < Capacity; i++)
			age[i] = Life;
	}

	/// <summary>
	/// A hit of the anointed spell at <paramref name="where"/>. <paramref name="flash"/>: the first strike after
	/// the Rite, for a spell that makes no node to crown - its ring thrown much wider, never thinned out.
	/// </summary>
	public void Mark(Vector2 where, bool flash = false)
	{
		if (sinceLast < MinGap && !flash)
			return;
		sinceLast = 0f;
		at[next] = new Vector2(MathF.Round(where.X), MathF.Round(where.Y));
		age[next] = 0f;
		reach[next] = flash ? FlashReach : Reach;
		next = (next + 1) % Capacity;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		sinceLast += dt;
		bool any = false;
		for (int i = 0; i < Capacity; i++)
		{
			if (age[i] >= Life)
				continue;
			age[i] += dt;
			any = true;
		}
		// Once more after the last one goes, or its final frame would stay drawn.
		if (any || drewLast)
			QueueRedraw();
		drewLast = any;
	}

	private bool drewLast;

	public override void _Draw()
	{
		for (int i = 0; i < Capacity; i++)
		{
			if (age[i] >= Life)
				continue;
			float t = age[i] / Life;
			int radius = StartRadius + Mathf.RoundToInt(t * reach[i]);
			ImageTexture ring = SovereignCorona.RingFor(element, ramp, radius, (int)(t * 4f) & 1);
			Vector2 size = ring.GetSize() * Texel;
			DrawTextureRect(ring, new Rect2(at[i] - size / 2f, size), false);
			// The mote: one art pixel rising off the hit, white-hot then gilt.
			Vector2 mote = at[i] + new Vector2(0f, -MathF.Round(t * MoteRise / Texel) * Texel - radius * Texel);
			DrawRect(new Rect2(mote - new Vector2(Texel / 2f, Texel / 2f), new Vector2(Texel, Texel)), t < 0.5f ? ramp.Core : Gilt);
		}
	}
}
