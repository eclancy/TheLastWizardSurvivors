using Godot;
using System;
using WizardSurvivors.scripts;

// THE HUNDRED MOUTHS (the Cumaean Sibyl's Apotheosis of Arcane Explosion, the verb ECHO; .ai/chapter-wizards.md
// 4.3). "Each burst sounds your other spells again from where you stand, at 40% strength." Her cave at Cumae had a
// hundred mouths and spoke with a hundred voices; Arcane Explosion is the one spell set off by the others, so in
// its final form each burst gives them back.
//
// HOW, AND WHY IT CANNOT FEED ITSELF. ArcaneExplosion asks (EchoOtherSpells) and this does the rest. The burst
// QUEUES every other spell she holds that puts something into the arena - the Black Cat's list, for the cat's
// reason: an orbit, a breath, a sun, a held weapon or the Familiar recast from where she stands would be
// re-anchored or refreshed, not cast - and the queue is spent EchoesPerFrame a physics frame, after the fire
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
public partial class Player
{
	/// <summary>An echo's strength: the share of the damage the spell's own cast would hand its projectiles.</summary>
	public const float EchoShare = 0.4f;

	/// <summary>How many queued echoes are cast in one physics frame.</summary>
	public const int EchoesPerFrame = 2;

	// What may be echoed: the spells that put something into the arena and leave it there (BlackCatCastable,
	// for the same reason the cat may only cast those).
	public static bool IsEchoable(SpellData spell) => spell != null && BlackCatCastable.Contains(spell.Id);

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
		for (int n = 0; n < EchoesPerFrame && echoCount > 0; n++)
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
