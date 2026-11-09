using Godot;
using System;

public static class Global
{
    // Simple static properties to mirror the earlier GDScript Global. A roster index (CharacterRoster) and a
    // stage index (StageCatalog.GetByIndex): each a stable number, never a list position. 1 is the Pyromancer:
    // roster index 0 is retired (the Test Wizard, removed 2026-11-09), as stage index 8 is (the Sketchbook).
    public static int SelectedCharacterIdx { get; set; } = 1;
    public static int SelectedStageIdx { get; set; } = 0;

    // TOOL SCENES ONLY: the spell (or "spell:form", SpellVariants.Selection) the run opens on in place of the
    // chosen wizard's own - _BalanceHarness --spell, _UiShot and _BossProbe --start-spell. Read by Player for
    // whichever wizard the tool picks; nothing in the game ever sets it. It was the Test Wizard's picker
    // (TestWizardStartingSpellId) until he was removed on 2026-11-09, and moved onto every wizard so the tools
    // that measure or photograph one spell kept working. The wizard's starting passive still applies.
    public static string ToolStartingSpellId { get; set; } = string.Empty;

    // Set by anything returning to the menu from inside the game, and cleared by TitleScreen the
    // moment it reads it. The title screen is the menu's shell now, so "back to the main menu"
    // means "load the title screen and open the menu at once" - without this the player would have
    // to press a key again after every single run.
    public static bool OpenMenuImmediately { get; set; } = false;

    // The run about to start is a Boss Rush (issue #12, BossRushCatalog) rather than a chapter.
    // Set by the stage list on every pick, so it is only ever true straight after the rush's card;
    // a Retry from the results screen keeps it, which is what retrying a rush should do.
    public static bool BossRush { get; set; } = false;
}
