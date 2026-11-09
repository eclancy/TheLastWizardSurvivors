using Godot;
using System;
using TheOneHeMissed.scripts;

// THE HUNDRED MOUTHS (the Cumaean Sibyl's Apotheosis of Arcane Explosion, the verb ECHO; .ai/chapter-wizards.md
// 4.3). "Each burst sounds your other spells again from where you stand, at 40% strength." Her cave at Cumae had a
// hundred mouths and spoke with a hundred voices; Arcane Explosion is the one spell set off by the others, so in
// its final form each burst gives them back.
//
// HOW, AND WHY IT CANNOT FEED ITSELF. ArcaneExplosion asks (EchoOtherSpells) and this does the rest. The burst
// QUEUES every other spell she holds whose cast puts something new into the arena (IsEchoable: the Black Cat's
// list and the ray and the cone; an orbit, a breath, a sun, a held weapon or the Familiar would only be
// refreshed, not cast again) - and the queue is spent EchoesPerFrame a physics frame, after the fire
// loop, never inside the cast that asked. An echo runs CastSpellNow, exactly the code her own cast runs, with
// the damage the cast hands its projectiles scaled by EchoShare for the length of the call. It is not a cast:
// no timer is touched, nothing is tallied, and NotifyArcaneCharge never hears it - so an echo never charges the
// burst, and is never echoed again. A spell already waiting is not queued twice.
//
// THE COST. At most MaxSpellSlots - 1 echoes a burst, and the burst comes once every three to eight casts of
// the others; spread two a frame, a six-spell loadout's echoes cost what one more cast of each costs, a frame
// or three after the burst. Nothing allocates: the queue and the motes are fixed arrays.
//
// WHAT IT LOOKS LIKE. Each echo that puts something out throws a ring of six arcane motes off her, on the
// world's 2-unit grid (the stasis motes' look, not a vector ring), and plays its spell's own cast voice.
//
// ON THE BEATS THAT FOLLOW (the Canticle, 2026-11-09; .ai/character-fit.md section 5, row 19). The burst goes off
// on a beat of the music, so its echoes answer on the next ones: while music with a known beat is sounding
// (MusicPlayer.TryGetBeat), the queue is spent EchoesPerBeat at each counted beat after the burst's - more a beat
// if that many would not all land inside the bar after it, so an echo is never more than a bar late. With no
// beat known, or once a bar has passed, it goes out a few a frame as before.
public partial class Player
{
	/// <summary>An echo's strength: the share of the damage the spell's own cast would hand its projectiles.</summary>
	public const float EchoShare = 0.4f;

	/// <summary>How many queued echoes are cast in one physics frame.</summary>
	public const int EchoesPerFrame = 2;

	/// <summary>How many queued echoes land on one beat of the music, at least.</summary>
	public const int EchoesPerBeat = 2;

	// The beat the echoes are counted from: the burst's, then the last one they landed on. Null with no beat known.
	private MusicBeats.Piece echoBeatPiece;
	private int echoBeatOrdinal;
	private int echoesPerBeat = EchoesPerBeat;
	private double echoBeatWaited;

	// What may be echoed: every spell whose cast puts something NEW into the world. The Black Cat's list, and
	// the two the cat may not cast for the cat's own reason - Scorching Ray and Cone of Cold make a fresh ray
	// or cone on every cast, but anchored on the wizard, so from the cat they would come out of him; an echo
	// comes from where she stands, so they are cast again as they are. (Measured 2026-11-02: on the seek bot's
	// Arcane loadout - Spiritual Weapon, Magic Missile, Scorching Ray, the Familiar, Aegis Ward - the cat's list
	// alone echoed Magic Missile and nothing else.) Never echoed: whatever keeps ONE node standing and only
	// refreshes it on its cooldown (an orbit, a breath, an aura, a trail, the Spiritual Weapon, the Familiar's
	// pack, the Starfall rain, the facing pair's emitters), Aegis Ward (a shield, not a cast at anything),
	// Hunter's Draw (drawn by walking) and Frost Shard (it casts nothing).
	public static readonly System.Collections.Generic.HashSet<string> EchoOnlyCastable = new(StringComparer.OrdinalIgnoreCase)
	{
		"scorching_ray", "cone_of_cold",
	};

	public static bool IsEchoable(SpellData spell) =>
		spell != null && (BlackCatCastable.Contains(spell.Id) || EchoOnlyCastable.Contains(spell.Id));

	private readonly SpellData[] echoQueue = new SpellData[MaxSpellSlots * 2];
	private int echoHead;
	private int echoCount;
	// 1 except inside an echo's CastSpellNow: GetSpellDamageMultiplier folds it in.
	private float castDamageScale = 1f;

	/// <summary>Echoes cast this run, for the balance harness's report and the probe.</summary>
	public int EchoesCast { get; private set; }

	/// <summary>
	/// The Hundred Mouths: queue every other echoable spell she holds to sound again. Called by ArcaneExplosion
	/// when the form bursts; <paramref name="burst"/> is the burst's own spell, which is never echoed.
	/// </summary>
	public void EchoOtherSpells(SpellData burst)
	{
		foreach (SpellData spell in equippedSpells)
		{
			if (spell == null || spell == burst || spell.IsPassive || !IsEchoable(spell) || IsSilenced(spell) || IsSealed(spell))
				continue;
			if (IsEchoQueued(spell) || echoCount >= echoQueue.Length)
				continue;
			echoQueue[(echoHead + echoCount) % echoQueue.Length] = spell;
			echoCount++;
		}
		echoBeatPiece = null;
		if (echoCount > 0 && MusicPlayer.Instance != null && MusicPlayer.Instance.TryGetBeat(out MusicPlayer.BeatClock clock))
		{
			echoBeatPiece = clock.Piece;
			echoBeatOrdinal = clock.Ordinal;
			echoBeatWaited = 0.0;
			int beatsInBar = Math.Max(1, clock.Piece.Counted.Length);
			echoesPerBeat = Math.Max(EchoesPerBeat, (echoCount + beatsInBar - 1) / beatsInBar);
		}
	}

	private bool IsEchoQueued(SpellData spell)
	{
		for (int i = 0; i < echoCount; i++)
			if (echoQueue[(echoHead + i) % echoQueue.Length] == spell)
				return true;
		return false;
	}

	private void TickEchoes(float delta)
	{
		TickEchoMotes(delta);
		if (echoCount == 0 || IsDead || HeldForBossEntrance)
			return;
		int release = EchoesPerFrame;
		if (echoBeatPiece != null)
		{
			echoBeatWaited += delta;
			MusicPlayer.BeatClock clock = default;
			bool onBeat = MusicPlayer.Instance != null && MusicPlayer.Instance.TryGetBeat(out clock)
				&& clock.Piece == echoBeatPiece;
			if (!onBeat || echoBeatWaited > echoBeatPiece.BarSeconds + 0.1)
			{
				// The beat stopped sounding, or a bar has gone by: whatever is left goes out now, as it always did.
				echoBeatPiece = null;
			}
			else
			{
				if (clock.Ordinal == echoBeatOrdinal)
					return;
				echoBeatOrdinal = clock.Ordinal;
				// A beat that went by while the run was paused is not one to land on: wait for the next.
				if (clock.SinceBeat > Math.Max(0.1, delta * 1.5))
					return;
				release = echoesPerBeat;
			}
		}
		for (int n = 0; n < release && echoCount > 0; n++)
		{
			SpellData spell = echoQueue[echoHead];
			echoQueue[echoHead] = null;
			echoHead = (echoHead + 1) % echoQueue.Length;
			echoCount--;
			CastEcho(spell);
		}
	}

	private void CastEcho(SpellData spell)
	{
		// Cut, silenced or sealed since the burst asked: nothing to sound.
		if (spell == null || !equippedSpells.Contains(spell) || IsSilenced(spell) || IsSealed(spell))
			return;
		float interval = MathF.Max(0.05f, (spell.GetCooldownAtLevel(spell.CurrentLevel) * cooldownMultiplier * GetWaterCooldownMultiplier()) / EffectiveAttackSpeed);
		CastOutcome outcome;
		castDamageScale = EchoShare;
		try
		{
			outcome = CastSpellNow(spell, interval, 0.0);
		}
		finally
		{
			castDamageScale = 1f;
		}
		if (outcome != CastOutcome.Fired)
			return;
		EchoesCast++;
		SfxPlayer.Cast(SfxCatalog.DominantElement(spell), GlobalPosition);
		ShowEchoMotes();
	}

	// --- the motes ----------------------------------------------------------------------------------------

	private const int EchoMoteRings = 4;
	private const int MotesPerRing = 6;
	private const float EchoMoteSeconds = 0.4f;
	private const float EchoMoteFrom = 16f;
	private const float EchoMoteTo = 66f;
	private static readonly Color EchoMoteHot = new(0.910f, 0.753f, 1.000f);    // arcane hot #E8C0FF
	private static readonly Color EchoMoteMid = new(0.753f, 0.549f, 1.000f);    // arcane mid #C08CFF
	private readonly float[] echoMoteAge = { -1f, -1f, -1f, -1f };
	private readonly float[] echoMoteTurn = new float[EchoMoteRings];
	private int nextEchoMoteRing;
	private int echoMoteCount;
	private Node2D echoMoteCanvas;
	private bool echoMotesDrawn;

	private void ShowEchoMotes()
	{
		if (echoMoteCanvas == null || !IsInstanceValid(echoMoteCanvas))
		{
			echoMoteCanvas = new Node2D { Name = "EchoMotes", ZAsRelative = false, ZIndex = DrawLayers.Air };
			echoMoteCanvas.Draw += DrawEchoMotes;
			AddChild(echoMoteCanvas);
		}
		int i = nextEchoMoteRing;
		nextEchoMoteRing = (nextEchoMoteRing + 1) % EchoMoteRings;
		echoMoteAge[i] = 0f;
		// Each ring turned a little from the last, so two echoes on one frame read as two voices.
		echoMoteTurn[i] = (echoMoteCount++ * 0.381966f) % 1f * Mathf.Tau;
	}

	private void TickEchoMotes(float delta)
	{
		if (echoMoteCanvas == null)
			return;
		bool any = false;
		for (int i = 0; i < EchoMoteRings; i++)
		{
			if (echoMoteAge[i] < 0f)
				continue;
			echoMoteAge[i] += delta;
			if (echoMoteAge[i] >= EchoMoteSeconds)
				echoMoteAge[i] = -1f;
			else
				any = true;
		}
		if ((any || echoMotesDrawn) && IsInstanceValid(echoMoteCanvas))
			echoMoteCanvas.QueueRedraw();
		echoMotesDrawn = any;
	}

	private void DrawEchoMotes()
	{
		for (int i = 0; i < EchoMoteRings; i++)
		{
			float age = echoMoteAge[i];
			if (age < 0f)
				continue;
			float t = age / EchoMoteSeconds;
			float r = Mathf.Lerp(EchoMoteFrom, EchoMoteTo, 1f - (1f - t) * (1f - t));
			float fade = 1f - t;
			for (int k = 0; k < MotesPerRing; k++)
			{
				float a = echoMoteTurn[i] + k * Mathf.Tau / MotesPerRing;
				// Squashed as the ground is, so the ring lies round her rather than standing up.
				Vector2 p = new(MathF.Cos(a) * r, MathF.Sin(a) * r * 0.62f - 20f);
				Color c = k % 2 == 0 ? EchoMoteHot : EchoMoteMid;
				echoMoteCanvas.DrawRect(WhipLimb.Cell(p, WhipLimb.Grid), new Color(c, fade));
				// A second mote trailing each, a step behind: a voice going out, not a dot.
				Vector2 q = p * 0.82f + new Vector2(0f, -20f * 0.18f);
				echoMoteCanvas.DrawRect(WhipLimb.Cell(q, WhipLimb.Grid), new Color(EchoMoteMid, fade * 0.55f));
			}
		}
	}
}
