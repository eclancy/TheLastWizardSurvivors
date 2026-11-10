using Godot;

namespace TheOneHeMissed.scripts;

[GlobalClass]
public partial class CharacterData : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string Name { get; set; } = string.Empty;

    // The discipline a wizard is named FOR, when the Name is a name of their own (Eric, 2026-11-03: "Let's give
    // names to the four original elemental wizards"): "Pyromancer" for Aedan, "Frostweaver" for Isbrand,
    // "Stormcaller" for Levina, "Geomancer" for Gabbro. The character select carries it under the name as a
    // title, so the craft a starter is chosen by is never lost. Empty for everyone whose Name already says what
    // they are - The Archmage, and the rescued, who are historic or legendary people (.ai/world-and-tone.md).
    [Export] public string Craft { get; set; } = string.Empty;

    /// <summary>"the Pyromancer" - the line the character select sets under the name - or empty with no Craft.</summary>
    public string CraftTitle => string.IsNullOrWhiteSpace(Craft) ? string.Empty : $"the {Craft}";

    /// <summary>"Aedan, the Pyromancer"; the bare Name with no Craft. For a line that has to say both at once.</summary>
    public string FullName => string.IsNullOrWhiteSpace(Craft) ? Name : $"{Name}, {CraftTitle}";

    [Export] public SpellData StartingSpellResource { get; set; } = null!;

    /// <summary>
    /// The form a wizard opens his spell in, for a starting spell taken in forms (SpellData.Variants): the form his
    /// Apotheosis is the final form OF (ApotheosisDefinition.VariantId), or empty. Nimue opens on Spiritual Weapon as
    /// the Sword (.ai/endgame-castle.md decision 11) because Excalibur ends the Sword. Read from the catalog rather
    /// than kept here, so the two can never disagree: a form holds for the run, and a wizard opened in another
    /// would never meet his gate. Ma Liang opened on the Familiar as the crow (The Painted Door) the same way from
    /// 2026-11-09 until he moved to Ink Ring on 2026-11-10. Empty for a spell with no forms, which every other wizard opens on, and for a wizard who chooses his
    /// form each run (ChoosesStartingForm).
    /// </summary>
    public string StartingFormId =>
        StartingSpellResource?.HasVariants == true && !ChoosesStartingForm
            ? ApotheosisCatalog.ForCharacter(Id)?.VariantId ?? string.Empty : string.Empty;

    /// <summary>
    /// True for a wizard who chooses the form of his starting spell on his run's first screen instead of opening in
    /// his Apotheosis's form (Player.ChoosesOpeningForm, Node2DGame.OpeningForm.cs). NO WIZARD SETS IT since
    /// 2026-11-09: it was Ma Liang's, who chose dog, crow or cat though only the crow has a final form, and he now opens
    /// on the crow every run as Nimue opens on the Sword (Eric: "he automatically starts with a crow familiar";
    /// .ai/character-fit.md section 5, row 21). The page it opens is kept, working and unused.
    /// </summary>
    [Export] public bool ChoosesStartingForm { get; set; } = false;

    /// <summary>
    /// What a run's opening hand asks for: the spell, or "spell:form" when he opens it in a form (SpellVariants.Selection),
    /// the shape a level-up sends for a new spell taken in one.
    /// </summary>
    public string StartingSelection => SpellVariants.Selection(StartingSpellResource?.Id ?? string.Empty, StartingFormId);

    /// <summary>The form he opens in, or null (the spell has none, or he opens it in its default form).</summary>
    public SpellVariant StartingForm =>
        string.IsNullOrEmpty(StartingFormId) ? null : StartingSpellResource?.FindVariant(StartingFormId);
    [Export] public float HealthModifier { get; set; } = 1.0f;
    [Export] public float SpeedModifier { get; set; } = 1.0f;
    [Export] public bool IsUnlocked { get; set; } = true;

    // Character identity (issue #29): a starting passive spell id/bonus and whether this character's
    // starting spell is granted as its Legendary variant (issue #24) from run start rather than
    // needing to be rolled in-run. Element tags come only from equipped spells/passives.
    [Export] public string StartingPassiveId { get; set; } = string.Empty;
    [Export] public string StartingPassiveName { get; set; } = string.Empty;
    [Export] public string StartingPassiveDescription { get; set; } = string.Empty;
    [Export] public bool IsLegendaryStart { get; set; } = false;

    /// <summary>
    /// A TRAIT, not a passive (in-flight 74, Eric 2026-11-09): she stays on top of the water - neither slowed by
    /// wading (Player.MovePlayer) nor sunk in it (Player.Wading.cs), nor held back by the Swallow-Hole's mud -
    /// and rings still spread round her feet. Baba Yaga, whose mortar floats, and Nimue, the Lady of the Lake,
    /// who walks on it. Each keeps her own passive; this sits beside it. Wayland's Wings already carries him
    /// over water by its own flag (Player.Winged), so he does not set this.
    /// </summary>
    [Export] public bool RidesOverWater { get; set; } = false;

    // Portrait shown on the character selection card. Left null falls back to a shared default
    // image in CharacterSelection.cs.
    [Export] public Texture2D Portrait { get; set; }

    // The character's full animation set: idle, moving, hurt, death. Optional, and the fallback
    // is deliberate rather than lazy - a character with no Frames still animates, because
    // CharacterVisuals.TryBuildIdleFrames builds an idle by walking the numbered siblings of
    // Portrait. That path is how every character worked before the roster had real sheets, and
    // it keeps a half-finished character playable instead of invisible.
    [Export] public SpriteFrames Frames { get; set; }
}
