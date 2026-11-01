using Godot;

/// <summary>
/// The rip laid over a level-up card while the player is choosing one to ban: a ragged split down the
/// page with the stone showing through it, torn fibres catching the light along both lips.
/// </summary>
/// <remarks>
/// From the player (2026-10-31): "ban should not change the ui other than placing a 'ripping' effect
/// over the spells that are showing up on the screen to identify that the player can select one of the
/// spells to ban." The ban used to swap the hand for a list of name buttons; now the hand stays exactly
/// as dealt and each card it can strike wears this (LevelUpMenu.SetBanMode).
///
/// It tears in: the split runs from a bite in the top edge to the foot over <see cref="RipSeconds"/>,
/// so entering ban mode is seen happening. At rest the split is a hairline that covers no more of a word
/// than a crack in the page would; the card the selection or the pointer is on opens wider and breathes
/// - "this one goes" - which is the ban's own focus cue on top of MenuFocusHighlight's ring.
///
/// Drawn, not art: a polyline of whole-unit knots (so the pixel art under it does not shimmer), its
/// jitter seeded by the spell id so a card tears the same way every time it is shown. Real time
/// (Time.GetTicksMsec) because the tree is paused under the level-up screen; no allocation per frame -
/// the knots are built on resize, and only the visible tears redraw.
/// </remarks>
public partial class BanTear : Control
{
	public const string NodeName = "BanTear";

	/// <summary>The card this tear is over: its hover and focus open the split.</summary>
	public BaseButton Card { get; set; }

	/// <summary>Seeds the ragged edge, so one spell's page always tears alike.</summary>
	public string Seed { get; set; } = string.Empty;

	private const float RipSeconds = 0.32f;
	private const float KnotStep = 12f;
	private const float BreathSeconds = 1.1f;

	// Stone deep through the split (the plate behind the page), and the fibres along its lips: flesh hi,
	// the page's own lightest tone, so on parchment the lips read as torn paper and on a dark plaque or
	// plate they read as a bright tear against it.
	private static readonly Color Gap = new Color(0.043f, 0.059f, 0.094f, 1f);
	private static readonly Color Lip = new Color(0.949f, 0.863f, 0.769f, 0.95f);
	private static readonly Color LipShade = new Color(0.431f, 0.267f, 0.314f, 0.9f);

	private float[] knotX = System.Array.Empty<float>();
	private float[] lipLeft = System.Array.Empty<float>();
	private float[] lipRight = System.Array.Empty<float>();
	private readonly Vector2[] bite = new Vector2[3];
	private ulong startMs;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		FocusMode = FocusModeEnum.None;
		ProcessMode = ProcessModeEnum.Always;
		// Anchors AND offsets: in the tree, SetAnchorsPreset alone keeps the current (empty) rect.
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		startMs = Time.GetTicksMsec();
		BuildKnots();
	}

	public override void _Process(double delta)
	{
		if (IsVisibleInTree())
			QueueRedraw();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
			BuildKnots();
	}

	private void BuildKnots()
	{
		int count = Mathf.Max(2, Mathf.CeilToInt(Size.Y / KnotStep) + 1);
		if (knotX.Length != count)
		{
			knotX = new float[count];
			lipLeft = new float[count];
			lipRight = new float[count];
		}

		// FNV-1a over the id: string.GetHashCode is randomised per process, and a tear that changed
		// shape between two photographs of the same card would look like a bug.
		uint hash = 2166136261;
		foreach (char c in Seed ?? string.Empty)
			hash = (hash ^ c) * 16777619;
		var rng = new RandomNumberGenerator { Seed = hash };

		// A slight diagonal, starting right of centre: through the middle of the page, never along its
		// edge, where the tear would read as the frame's own deckle.
		float top = Size.X * rng.RandfRange(0.54f, 0.62f);
		float bottom = Size.X * rng.RandfRange(0.38f, 0.46f);
		for (int i = 0; i < count; i++)
		{
			float t = count == 1 ? 0f : i / (float)(count - 1);
			float wander = i == 0 || i == count - 1 ? 0f : rng.RandfRange(-6f, 6f);
			knotX[i] = Mathf.Round(Mathf.Lerp(top, bottom, t) + wander);
			lipLeft[i] = Mathf.Round(rng.RandfRange(0f, 2f));
			lipRight[i] = Mathf.Round(rng.RandfRange(0f, 2f));
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (knotX.Length < 2 || Size.Y <= 0f)
			return;

		float now = Time.GetTicksMsec() / 1000f;
		float progress = Mathf.Clamp((now - startMs / 1000f) / RipSeconds, 0f, 1f);
		// Fast, then catching: a rip runs and then the last fibres give.
		progress = 1f - (1f - progress) * (1f - progress);
		float reach = Size.Y * progress;

		bool armed = Card != null && GodotObject.IsInstanceValid(Card) && !Card.Disabled
			&& (Card.IsHovered() || (Card.HasFocus() && !MenuFocusHighlight.PointerActive));
		float breath = 0.5f - 0.5f * Mathf.Cos(now * Mathf.Tau / BreathSeconds);
		float width = armed ? Mathf.Round(6f + breath * 3f) : 3f;
		float half = width * 0.5f;

		// The bite in the top edge the rip starts from.
		float x0 = knotX[0];
		bite[0] = new Vector2(x0 - half - 5f, 0f);
		bite[1] = new Vector2(x0 + half + 5f, 0f);
		bite[2] = new Vector2(x0, 10f);
		DrawColoredPolygon(bite, Gap);

		for (int i = 0; i < knotX.Length - 1; i++)
		{
			float y0 = i * KnotStep;
			if (y0 >= reach)
				break;
			float y1 = Mathf.Min((i + 1) * KnotStep, Size.Y);
			float xa = knotX[i];
			float xb = knotX[i + 1];
			if (y1 > reach)
			{
				xb = Mathf.Lerp(xa, xb, (reach - y0) / Mathf.Max(1f, y1 - y0));
				y1 = reach;
			}
			var a = new Vector2(xa, y0);
			var b = new Vector2(xb, y1);

			// A shadow under the left lip for depth, then the two lips, then the split itself.
			Vector2 left = new Vector2(-(half + 1f + lipLeft[i]), 0f);
			Vector2 leftNext = new Vector2(-(half + 1f + lipLeft[i + 1]), 0f);
			Vector2 right = new Vector2(half + 1f + lipRight[i], 0f);
			Vector2 rightNext = new Vector2(half + 1f + lipRight[i + 1], 0f);
			DrawLine(a + left + new Vector2(-1f, 0f), b + leftNext + new Vector2(-1f, 0f), LipShade, 1f);
			DrawLine(a + left, b + leftNext, Lip, 2f);
			DrawLine(a + right, b + rightNext, Lip, 2f);
			DrawLine(a, b, Gap, width);
		}
	}
}
