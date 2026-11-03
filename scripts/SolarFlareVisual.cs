using Godot;
using System;

namespace TheOneHeMissed.scripts;

// Solar Flare's bespoke visual, replacing the generic PlaceholderShape it used to share with
// Toxic Spore Burst. PlaceholderShape._Draw() lets Sunburst win over Ring, and SolarFlare.tscn set
// Sunburst while ElementalPulse set Ring at runtime, so the "expanding ring" animation was actually
// rendered as a growing *filled* disc - an opaque blob that swallowed the wizard and the swarm.
//
// Instead this draws the flare as light erupting from the player and washing outward, and nothing
// wide is solid: the bright elements are rays, a front that thins as it travels and a small kernel, so
// enemies stay readable straight through the blast.
//
// THE DRAWING IS THE HIT (2026-10-23, "their animations should make it clear what the hitbox is").
// It used to be emitted inside a transform squashed to 0.42 high and dropped to the feet, to read as
// a ground effect - but ElementalPulse hits a CIRCLE round the player's origin, so an enemy straight
// above or below him at the full radius was hit with nothing drawn within half that distance of it.
// It is a circle on the player's origin now, the same one the pulse tests, and the burst's own front -
// the leading shockwave, which travels out once at the moment of the hit and lands on the radius - is
// what shows the reach.
//
// PIXEL ART SINCE 2026-10-29 ("does not look up to our typical visual fidelity"). It was DrawArc,
// DrawCircle and see-through polygons - smooth hoops, soft discs, antialiased lines - and the hoops sat
// on the radius for the back half of every burst. Now it is one quad through
// scenes/shaders/solar_flare.gdshader, which snaps every fragment to the world's 2 px cell and draws
// in opaque Light and Fire tones with Bayer dithering as the only gradient. The charge pool at his boots
// is a second quad on a child on the ground layer, under the bodies, since it lies on the floor: it was
// drawn in the air over his boots. Its numbers are the ones the old drawing used, so a pulse looks
// exactly as wide, as long and as quick as it did; only the way it is drawn changed.
//
// FROM UNDERNEATH (2026-11-01). Eric, playing 0.10.0: Solar Flare "should animate from underneath the
// player and the enemies". The burst was drawn in the air (DrawLayers.Air), so the kernel sat over the
// wizard like a lamp held in front of him and the rays and the front lay across every body it caught.
// Now the burst is light breaking out of the GROUND: the same quad, the same numbers, on
// DrawLayers.GroundTell, under every body, so the kernel goes off beneath him and the rays and the front
// run out across the floor between the bodies' feet. And it RISES (RiseUnder): under him, and under each
// body it strikes the moment its front reaches that body, a flare of light erupts from the ground at the
// feet - a pool of white-hot light and tongues of Light shooting up past the head, then lifting off and
// going out by dither. The rising light is drawn under the body too, so the body stands in it, lit from
// below, and the light shows round its edges and over its head. One canvas (`risers`) draws them all from
// a fixed array of RiseSlots slots; nothing is allocated a pulse. What is hit, and when, is unchanged.
public partial class SolarFlareVisual : Node2D, IPulseVisual
{
	[Export] public int RayCount { get; set; } = 14;
	[Export] public int EmberCount { get; set; } = 12;
	[Export] public float BurstDuration { get; set; } = 0.55f;

	public const string ShaderPath = "res://scenes/shaders/solar_flare.gdshader";
	private static Shader shader;
	private static readonly StringName ParamPool = "pool";
	private static readonly StringName ParamRadius = "radius";
	private static readonly StringName ParamProgress = "progress";
	private static readonly StringName ParamSpin = "spin";
	private static readonly StringName ParamRays = "rays";
	private static readonly StringName ParamEmbers = "embers";
	private static readonly StringName ParamBeamCount = "beam_count";
	private static readonly StringName ParamBeamLength = "beam_length";
	private static readonly StringName ParamBeamHalf = "beam_half";
	private static readonly StringName ParamBeamTurn = "beam_turn";
	private static readonly StringName ParamCharge = "charge";
	private static readonly StringName ParamClock = "clock";
	private static readonly StringName ParamBoots = "boots";

	private float elapsed = 0f;
	private float radius = 90f;
	private float spin = 0f;
	private bool active = false;
	private ShaderMaterial burstMaterial;
	private ShaderMaterial poolMaterial;
	private Node2D poolCanvas;

	// The flares rising under what the burst struck (see FROM UNDERNEATH above): the body, when it starts
	// on this node's own clock, and where on it the feet and head are.
	private const int RiseSlots = 32;
	private const float RiseSeconds = 0.42f;
	private struct Riser
	{
		public Node2D Body;
		public float Start;
		public float FeetY;
		public float HeadY;
		public float HalfWidth;
	}
	private readonly Riser[] risers = new Riser[RiseSlots];
	private int nextRiser;
	private float riseClock;
	private bool risersLive;
	private Node2D riseCanvas;

	private static readonly Color LightCore = new Color(1f, 1f, 1f);           // light core #FFFFFF
	private static readonly Color LightHot = new Color(1f, 0.973f, 0.847f);    // light hot #FFF8D8
	private static readonly Color LightMid = new Color(1f, 0.941f, 0.659f);    // light mid #FFF0A8
	private static readonly Color LightEdge = new Color(1f, 0.776f, 0.235f);   // light edge #FFC63C
	private static readonly Color FireHot = new Color(1f, 0.784f, 0.282f);     // fire hot #FFC848
	private static readonly Color FireMid = new Color(0.910f, 0.478f, 0.102f); // fire mid #E87A1A

	public override void _Ready()
	{
		// The flare breaks out of the GROUND (FROM UNDERNEATH, above): under every body, on the layer of
		// the marks at a body's feet, so the bodies stand in it. It was in the air, over them.
		ZAsRelative = false;
		ZIndex = DrawLayers.GroundTell;
		shader ??= GD.Load<Shader>(ShaderPath);
		burstMaterial = new ShaderMaterial { Shader = shader };
		burstMaterial.SetShaderParameter(ParamPool, false);
		Material = burstMaterial;

		// The pool at his boots lies on the floor: under every body, with the other marks at a body's
		// feet (StanceGlow, the Brazier's ring, is the same layer), never over his boots.
		poolMaterial = new ShaderMaterial { Shader = shader };
		poolMaterial.SetShaderParameter(ParamPool, true);
		poolCanvas = new Node2D { Name = "ChargePool", ZAsRelative = false, ZIndex = DrawLayers.GroundTell, Material = poolMaterial };
		AddChild(poolCanvas);
		poolCanvas.Draw += DrawCharge;

		// The light rising under each body: cells on the world's grid, faded by dither, under the bodies.
		riseCanvas = new Node2D { Name = "Risers", ZAsRelative = false, ZIndex = DrawLayers.GroundTell, Material = SpellInk.DitherByCell };
		AddChild(riseCanvas);
		riseCanvas.Draw += DrawRisers;
	}

	public override void _ExitTree()
	{
		if (poolCanvas != null && IsInstanceValid(poolCanvas))
			poolCanvas.Draw -= DrawCharge;
		if (riseCanvas != null && IsInstanceValid(riseCanvas))
			riseCanvas.Draw -= DrawRisers;
	}

	/// <summary>
	/// A flare rising out of the ground under <paramref name="body"/> - something this burst struck at
	/// <paramref name="distance"/> from its centre, or the caster himself at 0 - starting the moment the
	/// burst's front, which reaches <paramref name="reach"/>, passes under it. Call after Burst.
	/// </summary>
	public void RiseUnder(Node2D body, float distance, float reach)
	{
		if (body == null || !IsInstanceValid(body))
			return;
		// The front's own ease (solar_flare.gdshader: wave = 1 - (1 - progress)^2), inverted.
		float progress = 1f - MathF.Sqrt(MathF.Max(0f, 1f - Mathf.Clamp(distance / MathF.Max(1f, reach), 0f, 1f)));
		float feetY, headY, halfWidth;
		if (body is Enemy enemy)
		{
			StatusFxGeometry geo = enemy.StatusFxGeometry;
			float scale = MathF.Abs(enemy.GlobalScale.Y);
			feetY = geo.FeetY * scale;
			headY = geo.HeadY * scale;
			halfWidth = geo.HalfWidth * MathF.Abs(enemy.GlobalScale.X);
		}
		else
		{
			// The wizard: his boots where the charge pool is told they are, his head a figure above them.
			feetY = feet.Y;
			headY = feet.Y - 78f;
			halfWidth = 16f;
		}
		risers[nextRiser] = new Riser
		{
			Body = body,
			Start = riseClock + progress * BurstDuration,
			FeetY = feetY,
			HeadY = headY,
			HalfWidth = MathF.Max(8f, halfWidth),
		};
		nextRiser = (nextRiser + 1) % RiseSlots;
		risersLive = true;
	}

	// Every live riser: a pool of light at the feet and five tongues shooting up from it past the head,
	// the middle one tallest and white-hot at its root, the outer ones cooler and shorter, their tops
	// flickering. They shoot up in the first third, lift off the ground from the bottom through the back
	// half and go out by dither. Rects a colour band tall, so a riser is a few dozen draw calls.
	private void DrawRisers()
	{
		if (!risersLive)
			return;
		riseCanvas.DrawSetTransformMatrix(riseCanvas.GetGlobalTransform().AffineInverse());
		float g = SpellInk.Grid;
		uint beat = (uint)(riseClock / 0.05f);
		for (int i = 0; i < RiseSlots; i++)
		{
			ref Riser r = ref risers[i];
			if (r.Body == null)
				continue;
			float s = (riseClock - r.Start) / RiseSeconds;
			if (s < 0f || s >= 1f || !IsInstanceValid(r.Body))
				continue;
			float cover = s < 0.6f ? 1f : (1f - s) / 0.4f;
			Vector2 foot = SpellInk.Snap(r.Body.GlobalPosition + new Vector2(0f, r.FeetY));
			float tall = (r.FeetY - r.HeadY) * 1.3f;
			float up = MathF.Min(1f, s / 0.3f);
			up = 1f - (1f - up) * (1f - up);
			float lift = s < 0.5f ? 0f : (s - 0.5f) / 0.5f;
			float hw = r.HalfWidth;

			// The pool at the feet: an ellipse of light lying flat, five rows, white-hot in the middle and
			// gold at the rim, its rim and its outer rows thinner coverage (dither) - shrinking as it lifts.
			float poolW = hw * 1.3f * (1f - 0.5f * lift);
			for (int row = -2; row <= 2; row++)
			{
				int ar = Math.Abs(row);
				float w = SpellInk.Snap(poolW * (ar == 0 ? 1f : (ar == 1 ? 0.82f : 0.48f)));
				if (w < g)
					continue;
				float y = foot.Y + row * g;
				float rim = cover * (ar == 2 ? 0.5f : 0.8f);
				riseCanvas.DrawRect(new Rect2(foot.X - w, y, w * 2f, g), new Color(ar == 2 ? FireHot : LightEdge, rim));
				float wm = SpellInk.Snap(w * 0.7f);
				if (wm > 0f && ar < 2)
					riseCanvas.DrawRect(new Rect2(foot.X - wm, y, wm * 2f, g), new Color(LightMid, cover));
				float wc = SpellInk.Snap(w * 0.38f);
				if (wc > 0f && ar < 2)
					riseCanvas.DrawRect(new Rect2(foot.X - wc, y, wc * 2f, g), new Color(ar == 0 ? LightCore : LightHot, cover));
			}

			// Five tongues across the body, the middle one two cells wide, the outer pair just outside the
			// body's sides so they show past it.
			for (int k = -2; k <= 2; k++)
			{
				int a = Math.Abs(k);
				float share = a == 0 ? 1f : (a == 1 ? 0.78f : 0.42f);
				uint h = (beat + (uint)i * 7u + (uint)(k + 2) * 13u) * 2654435761u;
				float flicker = 0.85f + 0.15f * ((h >> 24) / 255f);
				float height = SpellInk.Snap(tall * share * up * flicker);
				float bottom = SpellInk.Snap(height * lift);
				if (height - bottom < g)
					continue;
				float x = SpellInk.Snap(foot.X + k * hw * (a == 2 ? 0.55f : 0.5f)) - (a == 0 ? g : 0f);
				float width = a == 0 ? g * 2f : g;
				// Bands from the root up: white-hot, hot, mid, the Light's gold edge, and Fire's at the tip.
				Color root = a == 0 ? LightCore : (a == 1 ? LightHot : LightMid);
				Color tip = a == 2 ? FireMid : FireHot;
				Band(x, foot.Y, width, bottom, height, 0f, 0.3f, root, cover);
				Band(x, foot.Y, width, bottom, height, 0.3f, 0.55f, a == 0 ? LightHot : LightMid, cover);
				Band(x, foot.Y, width, bottom, height, 0.55f, 0.8f, a == 2 ? LightEdge : LightMid, cover);
				Band(x, foot.Y, width, bottom, height, 0.8f, 1f, tip, cover);
			}
		}
	}

	// One colour band of a tongue: from f0 to f1 of its height above the feet, cut off below `bottom`.
	private void Band(float x, float footY, float width, float bottom, float height, float f0, float f1, Color c, float cover)
	{
		float lo = MathF.Max(bottom, SpellInk.Snap(height * f0));
		float hi = SpellInk.Snap(height * f1);
		if (hi - lo < SpellInk.Grid * 0.5f)
			return;
		riseCanvas.DrawRect(new Rect2(x, footY - hi, width, hi - lo), new Color(c, cover));
	}

	// Corona Discharge's sunbeams, set by ElementalPulse just before the Burst they belong to: the
	// count, the length and half-width the beams HIT at, and their turn. Drawn once, with that burst.
	private int beamCount;
	private float beamLength;
	private float beamHalfWidth;
	private float beamTurn;

	public void SetBeams(int count, float length, float halfWidth, float turn)
	{
		beamCount = count;
		beamLength = length;
		beamHalfWidth = halfWidth;
		beamTurn = turn;
	}

	// How far the flare has built while the player stands still (ElementalPulse.StillCharge), 0 to 1.
	// Between bursts it is light pooling on the ground at his boots: a flat white-gold patch that widens
	// and fills as the charge climbs, throbbing quicker as it fills, with motes drawn in to it - the sun
	// gathering itself. Inside the Brazier's ember ring (StanceGlow, 31 x 11) so the two stand-still
	// tells never sit on each other, and small on purpose: it says "building", and the burst says how far.
	private float charge;
	private float chargeClock;
	private Vector2 feet = new Vector2(0f, 36f);
	private const float ChargeShowsFrom = 0.05f;
	private const float PoolSquash = 0.36f;
	// The furthest a mote starts from the pool's middle, as a share of the pool's widest (solar_flare's
	// gathering): what the pool's quad has to cover.
	private const float PoolReach = 26f * 1.08f * 2.4f + 4f;

	/// <param name="boots">Where his boots stand, in this node's space (the player's Feet capsule).</param>
	public void SetCharge(float value, Vector2 boots)
	{
		feet = boots;
		float next = Mathf.Clamp(value, 0f, 1f);
		if (Mathf.IsEqualApprox(next, charge) && next < ChargeShowsFrom)
			return;
		charge = next;
		poolCanvas?.QueueRedraw();
	}

	public void Burst(float burstRadius)
	{
		radius = MathF.Max(8f, burstRadius);
		elapsed = 0f;
		active = true;
		// Rotate the spoke pattern a little every cast so repeat pulses never stamp the same
		// starburst on the floor twice in a row.
		spin += 0.37f;
		burstMaterial.SetShaderParameter(ParamRadius, radius);
		burstMaterial.SetShaderParameter(ParamSpin, spin);
		burstMaterial.SetShaderParameter(ParamRays, Math.Max(4, RayCount));
		burstMaterial.SetShaderParameter(ParamEmbers, Math.Max(0, EmberCount));
		burstMaterial.SetShaderParameter(ParamBeamCount, beamLength > 0f ? beamCount : 0);
		burstMaterial.SetShaderParameter(ParamBeamLength, beamLength);
		burstMaterial.SetShaderParameter(ParamBeamHalf, beamHalfWidth);
		burstMaterial.SetShaderParameter(ParamBeamTurn, beamTurn);
		burstMaterial.SetShaderParameter(ParamProgress, 0f);
		QueueRedraw();
		poolCanvas?.QueueRedraw();
	}

	public override void _Process(double delta)
	{
		chargeClock += (float)delta;
		if (risersLive)
		{
			riseClock += (float)delta;
			// Live while any riser has yet to finish; one more redraw clears the last of them.
			bool any = false;
			for (int i = 0; i < RiseSlots && !any; i++)
				any = risers[i].Body != null && riseClock - risers[i].Start < RiseSeconds;
			risersLive = any;
			riseCanvas?.QueueRedraw();
		}
		if (charge >= ChargeShowsFrom && !active)
			poolCanvas?.QueueRedraw();
		if (!active)
			return;

		elapsed += (float)delta;
		if (elapsed >= BurstDuration)
		{
			active = false;
			// The beams belong to the burst they were set for.
			beamCount = 0;
			QueueRedraw();
			poolCanvas?.QueueRedraw();
			return;
		}
		burstMaterial.SetShaderParameter(ParamProgress, Mathf.Clamp(elapsed / MathF.Max(0.01f, BurstDuration), 0f, 1f));
		QueueRedraw();
	}

	// One quad over the reach and the beams; the shader decides every pixel and discards the rest. Sized
	// in world px, which is what the pulse hit in, whatever this node's scale.
	public override void _Draw()
	{
		if (!active)
			return;
		float scale = MathF.Max(0.01f, GlobalScale.X);
		float half = (MathF.Max(radius, beamCount > 0 ? beamLength : 0f) + 4f) / scale;
		DrawRect(new Rect2(-half, -half, half * 2f, half * 2f), Colors.White);
	}

	// The pool's quad, round his boots. The shader is told where they are in world px from this node.
	private void DrawCharge()
	{
		if (active || charge < ChargeShowsFrom)
			return;
		float scale = MathF.Max(0.01f, GlobalScale.X);
		poolMaterial.SetShaderParameter(ParamCharge, charge);
		poolMaterial.SetShaderParameter(ParamClock, chargeClock);
		poolMaterial.SetShaderParameter(ParamSpin, spin);
		poolMaterial.SetShaderParameter(ParamBoots, feet * scale);
		float w = PoolReach / scale;
		float h = (PoolReach * PoolSquash + 4f) / scale;
		poolCanvas.DrawRect(new Rect2(feet.X - w, feet.Y - h, w * 2f, h * 2f), Colors.White);
	}
}
