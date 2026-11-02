using Godot;
using System;
using WizardSurvivors.scripts;

/// <summary>
/// The wizard wears the Sovereign Rite (.ai/sovereign-spell.md section 5): a small crown-ring of the anointed
/// spell's element over his head, turning. Five points stand up from a band seen a little from above; the
/// ones on the near side are drawn in the element's white-hot core and hot stop, the far side in its mid, so
/// it reads as a ring going round rather than a row of pixels sliding sideways.
/// </summary>
/// <remarks>
/// Code-drawn in art pixels (two canvas units each), every position snapped to that grid. A child of the
/// player, so it rides with him; on the foremost layer above his body, never mixed into the arena's y-sort.
/// About forty rects a frame, once per run.
/// </remarks>
public partial class SovereignCrown : Node2D
{
	private const float Texel = 2f;
	// The band's half-width and half-depth, in texels: about the hood's width.
	private const float BandX = 7f;
	private const float BandY = 2.2f;
	private const int BandSamples = 28;
	private const int Points = 5;
	private const float TurnSpeed = 0.9f;

	/// <summary>The anointed spell's element. Set before it enters the tree.</summary>
	public Element Element { get; set; } = Element.Arcane;

	private ElementColors.Ramp ramp;
	private float angle;

	private static readonly Color Gilt = new(0.847f, 0.690f, 0.290f);

	public override void _Ready()
	{
		ramp = ElementColors.GetRamp(Element);
		ZAsRelative = false;
		ZIndex = DrawLayers.ForemostAir;
	}

	public override void _Process(double delta)
	{
		angle = (angle + (float)delta * TurnSpeed) % Mathf.Tau;
		QueueRedraw();
	}

	public override void _Draw()
	{
		// The far half of the band first, then the points behind, then the near half and the points before.
		for (int pass = 0; pass < 2; pass++)
		{
			bool near = pass == 1;
			for (int i = 0; i < BandSamples; i++)
			{
				float a = i * Mathf.Tau / BandSamples;
				float depth = MathF.Sin(a);
				if ((depth > 0f) != near)
					continue;
				Pixel(MathF.Cos(a) * BandX, depth * BandY, near ? Gilt : ramp.Mid);
			}
			for (int p = 0; p < Points; p++)
			{
				float a = angle + p * Mathf.Tau / Points;
				float depth = MathF.Sin(a);
				if ((depth > 0f) != near)
					continue;
				float x = MathF.Cos(a) * BandX;
				float y = depth * BandY;
				// A point is two texels tall and a glint at its tip; the far ones lose the glint.
				Pixel(x, y - 1f, near ? ramp.Hot : ramp.Mid);
				Pixel(x, y - 2f, near ? ramp.Core : ramp.Hot);
				if (near)
					Pixel(x, y - 3f, Colors.White);
			}
		}
	}

	private void Pixel(float x, float y, Color ink)
	{
		var at = new Vector2(MathF.Round(x) * Texel, MathF.Round(y) * Texel);
		DrawRect(new Rect2(at - new Vector2(Texel / 2f, Texel / 2f), new Vector2(Texel, Texel)), ink);
	}
}
