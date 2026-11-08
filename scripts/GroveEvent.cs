using Godot;
using TheOneHeMissed.scripts;

// The Enchanted Forest's side event: hold the grove (.ai/side-events.md).
//
// "Elderbark's wood. Every path now leads inward." A ring of corrupted trees stands somewhere in the
// wood. Stand inside it and the grove slowly grows back; step out and it starts to rot again. The
// wood sends everything it has at the ring while you hold it.
//
// What it asks: STAND STILL. Every run teaches the player to keep moving, and this is the one place
// in the game where moving loses. Leaving the ring costs ground rather than ending the attempt, so
// stepping out to escape a bad moment is a choice with a price, not a failure.
//
// The grove is out in the wood from the first second (SideEvent: placed at stage start, begun on
// arrival), a ring of dead trees under a faint grey-green column of light.
public partial class GroveEvent : SideEvent
{
	[Export] public float HoldSeconds { get; set; } = 20f;
	[Export] public float RingRadius { get; set; } = 110f;
	[Export] public float DrainRate { get; set; } = 0.5f;
	[Export] public float PressureIntervalSeconds { get; set; } = 4f;

	private const int TreeCount = 8;

	private Vector2 grove;
	private float progress;
	private float pressureTimer;
	private bool inside;
	// THE WIN BEAT (in-flight 58): the grove's own moment at the grove, before the reward - the
	// crowns leaf out one after another round the ring while a wave of green runs out across its
	// floor, and only then does the column rise, at the exact spot the beat was aimed at. The bloom
	// then lingers (BloomSeconds) instead of the grove vanishing on the winning frame.
	private float winBeat = -1f;
	private Vector2 winSpot;
	private const float WinBeatSeconds = 1.4f;

	protected override string Announcement => "There is a grove in the wood that has not quite died.";
	protected override string ChestLine => "The grove holds - a chest rises from it";
	protected override string Objective => inside
		? $"Hold the grove  ({Mathf.RoundToInt(progress * 100f)}%)"
		: $"Stand inside the grove  ({Mathf.RoundToInt(progress * 100f)}%)";
	protected override Vector2? PointerTarget => grove;

	protected override string SiteGlyph => "grove";
	// The ring and the trees round it, with room to stand in it and fight.
	protected override float SiteFootprint => RingRadius + 70f;
	// Close enough that the whole ring is on screen.
	protected override float ArrivalRadius => RingRadius + 260f;
	// Lichen's light end: a cold grey-green, the colour of something not quite dead.
	protected override Color TellColor => new Color(0.42f, 0.66f, 0.62f);

	protected override void OnPlaced()
	{
		grove = MainSite;
	}

	protected override void Begin()
	{
		grove = MainSite;
	}

	protected override void Tick(float delta)
	{
		// The beat: the grove is held - no more pressure, no more drain - and it blooms before it pays.
		if (winBeat >= 0f)
		{
			winBeat += delta;
			if (winBeat >= WinBeatSeconds)
				Succeed(winSpot, exact: true);
			return;
		}

		inside = PlayerWithin(grove, RingRadius);
		if (inside)
		{
			progress += delta / HoldSeconds;
			pressureTimer -= delta;
			if (pressureTimer <= 0f)
			{
				pressureTimer = PressureIntervalSeconds;
				SpawnRing(grove, 5, 330f);
			}
		}
		else
		{
			progress = Mathf.Max(0f, progress - delta * DrainRate / HoldSeconds);
		}

		if (progress >= 1f)
		{
			progress = 1f;
			winSpot = ChooseRewardSpot(grove);
			winBeat = 0f;
			// The grove's own cue as it comes back; the reward's own sound still comes with the column.
			SfxPlayer.AtPosition(SfxCatalog.SecretSeen, grove, 0f);
		}
	}

	private static readonly Color Bark = new Color(0.16f, 0.12f, 0.10f);
	private static readonly Color BarkLit = new Color(0.34f, 0.26f, 0.20f);
	private static readonly Color Rot = new Color(0.22f, 0.20f, 0.26f, 0.55f);
	private static readonly Color Growth = new Color(0.46f, 0.80f, 0.40f, 0.9f);

	// How long a won grove stays on the ground, fully leafed, after it is held. It used to vanish on
	// the frame it was won, which made "what did standing here do?" a fair question.
	private const float BloomSeconds = 3f;

	public override void _Draw()
	{
		// One clock from the moment the hold filled: the beat runs it from 0, the linger carries it on.
		float sinceWin = -1f;
		float alpha = 1f;
		if (IsResolved)
		{
			if (!Succeeded || SecondsSinceResolved >= BloomSeconds)
				return;
			float t = SecondsSinceResolved / BloomSeconds;
			alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
			sinceWin = WinBeatSeconds + SecondsSinceResolved;
		}
		else if (winBeat >= 0f)
		{
			sinceWin = winBeat;
		}
		// Every crown swells once as the grove comes back, then settles.
		float bloom = sinceWin < 0f ? 0f : Mathf.Sin(Mathf.Clamp(sinceWin / 0.7f, 0f, 1f) * Mathf.Pi);

		Vector2 c = ToLocal(grove);
		// The ground inside the ring greens as it grows back.
		DrawCircle(c, RingRadius, new Color(Rot.Lerp(Growth, progress), (0.20f + progress * 0.15f) * alpha));
		// The beat's wave: a rim of green running out from the centre to the trees, then gone.
		if (sinceWin >= 0f && sinceWin < 1.0f)
		{
			float run = sinceWin / 1.0f;
			DrawArc(c, (RingRadius + 10f) * run, 0f, Mathf.Tau, 48, new Color(Growth, 0.7f * (1f - run)), 5f);
		}

		Color bark = new Color(Bark, alpha);
		Color barkLit = new Color(BarkLit, alpha);
		Color growth = new Color(Growth, Growth.A * alpha);
		float leaf = 4f + 2.5f * bloom;
		for (int i = 0; i < TreeCount; i++)
		{
			float a = i * Mathf.Tau / TreeCount;
			Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (RingRadius + 10f);
			// A dead trunk with two bare limbs; the limbs leaf out as the grove grows.
			DrawLine(p + new Vector2(0f, 14f), p + new Vector2(0f, -18f), bark, 5f);
			DrawLine(p + new Vector2(0f, -6f), p + new Vector2(-9f, -16f), barkLit, 2f);
			DrawLine(p + new Vector2(0f, -10f), p + new Vector2(8f, -20f), barkLit, 2f);
			if (progress > (i + 1f) / (TreeCount + 1f) || sinceWin >= 0f)
			{
				DrawCircle(p + new Vector2(-9f, -17f), leaf, growth);
				DrawCircle(p + new Vector2(8f, -21f), leaf, growth);
				// The third leaf comes to each crown in turn round the ring, as the wave reaches it.
				if (sinceWin >= 0.15f + i * 0.09f)
					DrawCircle(p + new Vector2(0f, -24f), leaf * 0.9f, growth);
			}
		}

		if (!IsResolved && winBeat < 0f)
			DrawProgressArc(grove, RingRadius, progress, Growth, 4f);
	}
}
