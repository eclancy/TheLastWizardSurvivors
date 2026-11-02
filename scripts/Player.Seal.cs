using Godot;
using System;
using System.Collections.Generic;
using WizardSurvivors.scripts;

// THE SEAL (.ai/endgame-castle.md 6.4; piece C5, the dark wizard's one new shared primitive).
//
// "He takes what you have. Take it back." When his hand of dark light lands, one of the wizard's spells
// is SEALED: it stops firing, its slot on the loadout strip wears his mark (LoadoutHud, the same wax seal
// the Sovereign Rite puts on a silenced slot), and the seal - a dark orb with that spell's icon in it -
// drifts back toward him. Touch the orb and the spell is back at once; let it reach him and the spell
// stays sealed for twenty seconds. The boss owns the hand and the orb (ThroneDarkWizardBoss, DarkHand,
// SealOrb); this owns only the one fact the rest of the game has to agree on: which spells are sealed,
// and for how long.
//
// HOW A SEALED SPELL STOPS. Four places, and nothing else:
//   - the firing loop in PhysicsTick (the ONE spellFireTimers loop; no parallel timer system) skips it,
//     its timer HELD where it stood, exactly as HeldForBossEntrance holds every timer - so on release it
//     goes off as far into its cooldown as it was when it was taken;
//   - NotifyArcaneCharge does not detonate a sealed Arcane Explosion (it is charged by other casts, not by
//     its own timer, so the loop alone would not stop it), and Frost Shard's shatter and the Black Cat
//     leave a sealed spell alone, as they leave a silenced one;
//   - a spell that STANDS in the world between casts - an orbit, a flame, a pulse, a trail, the explosion
//     that follows him, the rain, the Familiar's pack, the Spiritual Weapon, Sagittarius, the facing pair -
//     has its node taken out of play while sealed (process off, which also takes an Area2D out of the
//     physics world, and hidden) and put back exactly as it was on release. The Sovereign Rite FREES those
//     nodes, because its silence is for the run; a seal can lift a second later, and a pack rebuilt from
//     nothing would lose what the cat granted;
//   - a cast spell's projectiles already in flight are left alone: what was thrown before the hand landed
//     was thrown.
//
// Keyed by the runtime SpellData, not by slot, so a spell that changes slot (a Penknife cut ahead of it)
// stays sealed and a spell cut while sealed drops its seal. Nothing here runs while nothing is sealed: an
// ordinary run's loop pays one Count check per spell.
public partial class Player
{
	/// <summary>A seal went on or came off. LoadoutHud redraws its marks on it; nothing else listens.</summary>
	[Signal] public delegate void SealsChangedEventHandler();

	private sealed class SpellSeal
	{
		public SpellData Spell = null!;
		/// <summary>Seconds until it lifts by itself; PositiveInfinity while it lasts until released.</summary>
		public float SecondsLeft;
		/// <summary>What it was set to last, for the HUD's draining mark.</summary>
		public float SecondsTotal;
		public readonly List<(Node Node, ProcessModeEnum Mode, bool Visible)> Presence = new();
	}

	private readonly List<SpellSeal> seals = new();

	// Casts made per spell id, counted where the loop spends a cooldown. For the tools only (_BossProbe's
	// seal check, which proves a sealed spell casts nothing and casts again once freed).
	private readonly Dictionary<string, int> castTally = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Spells sealed now.</summary>
	public int SealedCount => seals.Count;

	/// <summary>Casts this run of the spell with <paramref name="spellId"/> through the firing loop. For tools.</summary>
	public int CastsOf(string spellId) => spellId != null && castTally.TryGetValue(spellId, out int n) ? n : 0;

	/// <summary>
	/// Whether the spell could be sealed now: held, an attack (not a passive, and not the shield - the Rite's
	/// rule, SovereignRite.IsPassiveForRite: a ward stands on the wizard between casts and is never "fired"),
	/// firing (not silenced by the Sovereign Rite, whose silence already stops it) and not sealed already.
	/// What the dark wizard's hand chooses from.
	/// </summary>
	public bool CanSeal(SpellData spell) =>
		!IsDead && spell != null && !SovereignRite.IsPassiveForRite(spell) && equippedSpells.Contains(spell)
		&& !IsSilenced(spell) && FindSeal(spell) == null;

	/// <summary>
	/// Seals the spell in loadout <paramref name="slot"/> (an index into the held spells, as LoadoutHud
	/// draws them) for <paramref name="seconds"/>. See <see cref="SealSpell(SpellData, float)"/>.
	/// </summary>
	public SpellData SealSpell(int slot, float seconds) =>
		slot >= 0 && slot < equippedSpells.Count ? SealSpell(equippedSpells[slot], seconds) : null;

	/// <summary>
	/// Seals <paramref name="spell"/> for <paramref name="seconds"/>; <c>float.PositiveInfinity</c> holds it
	/// until <see cref="ReleaseSeal"/>. A spell already sealed has its clock set to the new value - the orb's
	/// arrival turns "until released" into twenty seconds. Returns the spell sealed, or null if it cannot be
	/// (not held, a passive or the shield, silenced by the Rite, the wizard dead).
	/// </summary>
	public SpellData SealSpell(SpellData spell, float seconds)
	{
		if (IsDead || spell == null || SovereignRite.IsPassiveForRite(spell) || seconds <= 0f || !equippedSpells.Contains(spell) || IsSilenced(spell))
			return null;

		SpellSeal seal = FindSeal(spell);
		if (seal == null)
		{
			seal = new SpellSeal { Spell = spell };
			seals.Add(seal);
			TakePresence(seal);
		}
		seal.SecondsLeft = seconds;
		seal.SecondsTotal = seconds;
		EmitSignal(SignalName.SealsChanged);
		return spell;
	}

	/// <summary>Lifts the seal on <paramref name="spell"/> at once (the orb touched). False if it was not sealed.</summary>
	public bool ReleaseSeal(SpellData spell)
	{
		SpellSeal seal = FindSeal(spell);
		if (seal == null)
			return false;
		Lift(seal);
		EmitSignal(SignalName.SealsChanged);
		return true;
	}

	/// <summary>Lifts every seal (his fall, his leaving the tree).</summary>
	public void ReleaseAllSeals()
	{
		if (seals.Count == 0)
			return;
		for (int i = seals.Count - 1; i >= 0; i--)
			Lift(seals[i]);
		EmitSignal(SignalName.SealsChanged);
	}

	/// <summary>True while <paramref name="spell"/> is sealed.</summary>
	public bool IsSealed(SpellData spell) => seals.Count > 0 && FindSeal(spell) != null;

	/// <summary>True while the spell in loadout <paramref name="slot"/> is sealed. For LoadoutHud.</summary>
	public bool IsSlotSealed(int slot) =>
		seals.Count > 0 && slot >= 0 && slot < equippedSpells.Count && FindSeal(equippedSpells[slot]) != null;

	/// <summary>
	/// How much of a timed seal is left, 0 to 1, for the slot's draining mark; 1 for a seal held until
	/// released, 0 for a slot not sealed.
	/// </summary>
	public float SealRemainingFraction(int slot)
	{
		if (seals.Count == 0 || slot < 0 || slot >= equippedSpells.Count)
			return 0f;
		SpellSeal seal = FindSeal(equippedSpells[slot]);
		if (seal == null)
			return 0f;
		if (float.IsPositiveInfinity(seal.SecondsLeft) || seal.SecondsTotal <= 0f)
			return 1f;
		return Mathf.Clamp(seal.SecondsLeft / seal.SecondsTotal, 0f, 1f);
	}

	/// <summary>True while the spell in <paramref name="slot"/> is sealed on a clock (the orb reached him).</summary>
	public bool IsSlotSealTimed(int slot) =>
		seals.Count > 0 && slot >= 0 && slot < equippedSpells.Count
		&& FindSeal(equippedSpells[slot]) is SpellSeal seal && !float.IsPositiveInfinity(seal.SecondsLeft);

	/// <summary>True when any seal runs on a clock (the HUD redraws its draining marks only then).</summary>
	public bool AnyTimedSeal
	{
		get
		{
			foreach (SpellSeal seal in seals)
				if (!float.IsPositiveInfinity(seal.SecondsLeft))
					return true;
			return false;
		}
	}

	private SpellSeal FindSeal(SpellData spell)
	{
		if (spell == null)
			return null;
		foreach (SpellSeal seal in seals)
			if (ReferenceEquals(seal.Spell, spell))
				return seal;
		return null;
	}

	// Once a physics frame, before the firing loop: the clocks, and a seal on a spell no longer held
	// (cut while sealed, or fused into a Conjunction) let go. Paused with the tree, as the loop is.
	private void TickSeals(float delta)
	{
		if (seals.Count == 0)
			return;
		bool changed = false;
		for (int i = seals.Count - 1; i >= 0; i--)
		{
			SpellSeal seal = seals[i];
			if (!float.IsPositiveInfinity(seal.SecondsLeft))
				seal.SecondsLeft -= delta;
			if (seal.SecondsLeft <= 0f || !equippedSpells.Contains(seal.Spell) || IsSilenced(seal.Spell))
			{
				Lift(seal);
				changed = true;
			}
		}
		if (changed)
			EmitSignal(SignalName.SealsChanged);
	}

	private void Lift(SpellSeal seal)
	{
		seals.Remove(seal);
		foreach ((Node node, ProcessModeEnum mode, bool visible) in seal.Presence)
		{
			if (node == null || !IsInstanceValid(node))
				continue;
			node.ProcessMode = mode;
			if (node is CanvasItem item)
				item.Visible = visible;
		}
		seal.Presence.Clear();
	}

	// The nodes a spell keeps in the world between casts, taken out of play: process off (a disabled
	// CollisionObject2D leaves the physics world, CollisionObject2D.DisableMode's default) and hidden.
	private void TakePresence(SpellSeal seal)
	{
		foreach (Node node in StandingPresence(seal.Spell))
		{
			seal.Presence.Add((node, node.ProcessMode, node is CanvasItem item && item.Visible));
			node.ProcessMode = ProcessModeEnum.Disabled;
			if (node is CanvasItem shown)
				shown.Visible = false;
		}
	}

	// Found as the Rite and the Penknife find them (Player.Sovereign.DropStandingNodes and
	// RemovePersistentSpellInstance): five kinds that live on the wizard, by type, and the six that live in
	// the arena, by their fields.
	private IEnumerable<Node> StandingPresence(SpellData spell)
	{
		foreach (Node child in GetChildren())
		{
			SpellData owner = child switch
			{
				OrbitingBlade blade => blade.SpellData,
				Flamethrower flame => flame.SpellData,
				ElementalPulse pulse => pulse.SpellData,
				TrailWeaver weaver => weaver.SpellData,
				ArcaneExplosion explosion => explosion.SpellData,
				_ => null,
			};
			if (owner != null && owner.Id.Equals(spell.Id, StringComparison.OrdinalIgnoreCase))
				yield return child;
		}
		if (IsSpell(spell, "familiar") && familiarPack != null && IsInstanceValid(familiarPack))
			yield return familiarPack;
		if (IsSpell(spell, "spiritual_weapon") && spiritWeapons != null && IsInstanceValid(spiritWeapons))
			yield return spiritWeapons;
		if (IsSpell(spell, "starfall") && starfallRain != null && IsInstanceValid(starfallRain))
			yield return starfallRain;
		if (IsSpell(spell, Sagittarius.SpellId) && sagittarius != null && IsInstanceValid(sagittarius))
			yield return sagittarius;
		if (IsSpell(spell, "stormsweep") && stormsweep != null && IsInstanceValid(stormsweep))
			yield return stormsweep;
		if (IsSpell(spell, "sleet_runes") && sleetRunes != null && IsInstanceValid(sleetRunes))
			yield return sleetRunes;
	}

	private static bool IsSpell(SpellData spell, string id) => spell.Id.Equals(id, StringComparison.OrdinalIgnoreCase);

	private void TallyCast(SpellData spell)
	{
		castTally.TryGetValue(spell.Id, out int n);
		castTally[spell.Id] = n + 1;
	}
}
