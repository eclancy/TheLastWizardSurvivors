using Godot;

namespace TheOneHeMissed.scripts;

[GlobalClass]
public partial class CharacterData : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string Name { get; set; } = string.Empty;

    // The discipline a wizard is named FOR, when the Name is a name of their own (Eric, 2026-11-03: "Let's give
    // names to the four original elemental wizards"): "Pyromancer" for Aedan, "Frostweaver" for Isbrand,
    // "Stormcaller" for Thora, "Geomancer" for Gabbro. The character select carries it under the name as a
    // title, so the craft a starter is chosen by is never lost. Empty for everyone whose Name already says what
    // they are - The Archmage, and the rescued, who are historic or legendary people (.ai/world-and-tone.md).
    [Export] public string Craft { get; set; } = string.Empty;

    /// <summary>"the Pyromancer" - the line the character select sets under the name - or empty with no Craft.</summary>
    public string CraftTitle => string.IsNullOrWhiteSpace(Craft) ? string.Empty : $"the {Craft}";

    /// <summary>"Aedan, the Pyromancer"; the bare Name with no Craft. For a line that has to say both at once.</summary>
    public string FullName => string.IsNullOrWhiteSpace(Craft) ? Name : $"{Name}, {CraftTitle}";

    [Export] public SpellData StartingSpellResource { get; set; } = null!;
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
