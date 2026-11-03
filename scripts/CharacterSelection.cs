using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// Data-driven character selection (issue #1/#29): builds one scrollable card per CharacterRoster
// entry at runtime instead of a hardcoded stub list, and respects both CharacterData.IsUnlocked
// and SaveData.UnlockedCharacterIds. Each card shows the character's starting weapon, element and
// passive so the player can make an informed pick before a run.
public partial class CharacterSelection : Control
{
	private static readonly (string Id, string Path)[] TestWizardSpellOptions = new[]
	{
		("magic_missile", "res://SpellData.tres"),
		("arcane_explosion", "res://SpellData_ArcaneExplosion.tres"),
		("spiritual_weapon", "res://SpellData_SpiritualWeapon.tres"),
		("fireball", "res://SpellData_Fireball.tres"),
		("cinderbreath", "res://SpellData_Cinderbreath.tres"),
		("mirefoot", "res://SpellData_Mirefoot.tres"),
		("kindled_ward", "res://SpellData_KindledWard.tres"),
		("gravewell", "res://SpellData_Gravewell.tres"),
		("hollow_star", "res://SpellData_HollowStar.tres"),
		("starfall", "res://SpellData_Starfall.tres"),
		("familiar", "res://SpellData_Familiar.tres"),
		("stormsweep", "res://SpellData_Stormsweep.tres"),
		("sleet_runes", "res://SpellData_SleetRunes.tres"),
		("iron_palisade", "res://SpellData_IronPalisade.tres"),
		("contagion", "res://SpellData_Contagion.tres"),
		("bramble_seed", "res://SpellData_BrambleSeed.tres"),
		("frost_shard", "res://SpellData_FrostShard.tres"),
		("riptide", "res://SpellData_Riptide.tres"),
		("shadow_bolt", "res://SpellData_ShadowBolt.tres"),
		("thorn_vine", "res://SpellData_ThornVine.tres"),
		("gale_blade", "res://SpellData_GaleBlade.tres"),
		("solar_flare", "res://SpellData_SolarFlare.tres"),
		("molten_shard", "res://SpellData_MoltenShard.tres"),
		("chain_lightning", "res://SpellData_ChainLightning.tres"),
		("toxic_spore_burst", "res://SpellData_ToxicSporeBurst.tres"),
		("obsidian_spike", "res://SpellData_ObsidianSpike.tres"),
		("cyclone_slash", "res://SpellData_CycloneSlash.tres"),
		("void_lance", "res://SpellData_VoidLance.tres"),
		("glacial_spike", "res://SpellData_GlacialSpike.tres"),
		("black_tentacles", "res://SpellData_BlackTentacles.tres"),
		("cone_of_cold", "res://SpellData_ConeOfCold.tres"),
		("scorching_ray", "res://SpellData_ScorchingRay.tres"),
		("meteor_swarm", "res://SpellData_MeteorSwarm.tres"),
		("hunters_draw", "res://SpellData_HuntersDraw.tres"),
	};

	private static readonly (string Id, string Name)[] TestWizardPassiveOptions = new[]
	{
		("aegis_ward", "Aegis Ward"),
		("thornmail_barrier", "Thornmail Barrier"),
		("frozen_bulwark", "Frozen Bulwark"),
		("stormguard_aura", "Stormguard Aura"),
		("venom_cloak", "Venom Cloak"),
		("guardian_vines", "Guardian Vines"),
		("tidal_barrier", "Tidal Barrier"),
		("stone_bulwark", "Stone Bulwark"),
		("blur", "Blur"),
		("fortunes_favor", "Fortune's Favor"),
		("haste", "Haste"),
	};

	private List<CharacterData> characters = new List<CharacterData>();
	private readonly List<string> testWizardSpellIds = new List<string>();
	private OptionButton testWizardSpellPicker = null!;

	// The PC layout (landscape, not ResponsiveLayout.IsNarrow): a roster of compact portrait cards on
	// the left and a detail pane on the right that follows whatever card is hovered or focused. On a
	// 720-tall canvas the phone's tall cards fitted one row of the seven wizards and hid the rest
	// behind a scrollbar; a roster of faces fits everyone, and the words move to one place that has
	// the room for them. The phone keeps its cards.
	private bool wideLayout;

	// What a card may show (.ai/side-events.md, "Secret wizards"). A LOCKED wizard is his
	// silhouette and how he is found, with the name kept back: the name is what freeing him gives
	// you ("Wizard freed - Tituba"), and the rescued are people, not a class you can read
	// off a coat. A SECRET wizard is a sealed card and nothing else, until UnlockCatalog's reveal
	// condition holds - then he is an ordinary lock.
	private enum CardState { Unlocked, Locked, Secret }

	private readonly List<CardState> cardStates = new();
	private VBoxContainer detailBox;
	private Control testWizardPickerHost;
	private int detailIdx = -1;
	private const float CompactCardWidth = 160f;
	// Three rows of compact cards in the roster's height on a 1280x720 canvas (2026-10-28: Laelaps made
	// nine, and the ninth went under the fold). The box is the 48 cell at x3 (144) and a little; the card
	// is that, its name and its margins; the roster runs down to where the detail pane ends. Twelve fit
	// (three rows of four); a thirteenth scrolls.
	private const float CompactPortraitBox = 148f;
	private const float CompactCardHeight = 196f;
	private const float RosterBottomMargin = 16f;
	// The header row: Back at the top left, as on the world map this screen leads to, and the heading
	// centred clear of it. Back used to sit on the roster's top edge, so the first card's ring ran
	// under it, and on a phone the heading ran into it.
	private const float HeaderTop = 12f;
	private const float BackWidth = 124f;
	private const float RosterTop = 80f;
	private const float RosterFraction = 0.6f;
	private string defaultTestWizardSpellId = "magic_missile";

	public override void _Ready()
	{
		characters = CharacterRoster.GetAll();
		var grid = GetNode<GridContainer>("CardScroll/CardGrid");
		wideLayout = !ResponsiveLayout.IsNarrow(this);

		// The scene hardcodes three columns, which overflows both edges of a 720-wide portrait
		// viewport. Derive the count from the card width that actually fits, and keep it correct
		// if the window is resized or the device rotated.
		// Four across on a PC's landscape canvas: at three the grid stopped three-quarters of the way
		// across and left the right of the screen empty. The phone fits fewer and takes what fits.
		var cardScroll = GetNode<Control>("CardScroll");
		cardScroll.OffsetTop = RosterTop;
		cardScroll.OffsetBottom = -RosterBottomMargin;
		if (wideLayout)
		{
			// The roster takes the left of the screen; the detail pane is built beside it below.
			cardScroll.AnchorRight = RosterFraction;
			cardScroll.OffsetRight = -12f;
			// 12 between cards, not the scene's 16: with the header row above it, the third row of faces
			// otherwise ran 8 units under the fold.
			grid.AddThemeConstantOverride("h_separation", 12);
			grid.AddThemeConstantOverride("v_separation", 12);
			ResponsiveLayout.BindGridColumns(grid, CompactCardWidth, maxColumns: 4);
		}
		else
		{
			// One wizard to a row on a phone, his face on the left and what he brings on the right - the
			// Achievements list's shape. Two tall cards abreast set every line centred in a column 300
			// wide, and "Starting Spell: Cone of Cold" broke wherever the card ran out.
			grid.Columns = 1;
		}
		LayoutHeader();

		// Clear any placeholder cards left in the scene, then build the real roster. Free()
		// (not QueueFree()) so old children are gone immediately instead of coexisting with the
		// freshly-built cards for one frame.
		foreach (var child in grid.GetChildren())
			child.Free();

		var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		apotheosisShown = Teachings.IsShown(saveManager?.Data, Teaching.Apotheosis);

		for (int i = 0; i < characters.Count; i++)
		{
			var character = characters[i];
			// UnlockCatalog plus the save is the whole answer now. CharacterData.IsUnlocked is
			// deliberately NOT consulted: it is an [Export] that defaults to true, so honouring it
			// would mean every wizard added from here on ships unlocked unless someone remembered to
			// flip it - which is precisely the failure that left the old character rail dormant.
			// The previous check also used a case-sensitive List.Contains, alone among every unlock
			// check in the project.
			bool unlocked = GlobalStatsManager.IsCharacterUnlocked(saveManager?.Data, character.Id);
			// One who is SealedUntilFreed (the dark wizard) keeps the sealed card until he is free, whatever
			// has been revealed: his shape is the surprise (.ai/dark-wizard-playable.md section 2).
			CardState state = unlocked ? CardState.Unlocked
				: GlobalStatsManager.IsCharacterRevealed(saveManager?.Data, character.Id)
					&& UnlockCatalog.GetCharacter(character.Id)?.SealedUntilFreed != true ? CardState.Locked
				: CardState.Secret;

			// The state is kept for every roster index, a hidden one included, because cardStates is read
			// by roster index (ShowDetail) and the cards carry theirs.
			cardStates.Add(state);
			// The dark wizard is not on the roster at all until he is free - no card, no seal, no shape
			// (Eric, 2026-11-02: "the dark wizard shouldn't show up, even in silhouette, until he's unlocked").
			if (!unlocked && UnlockCatalog.GetCharacter(character.Id)?.SealedUntilFreed == true)
				hiddenUntilFreed.Add(i);
		}

		// Laid out in the screen's own order, not the roster's: The Archmage first (CharacterRoster.SelectionOrder;
		// Eric, 2026-11-02: "He should be the first one on the list"), the rest as the roster lists them. Every card
		// still carries its ROSTER index, which is what Global.SelectedCharacterIdx and the run read.
		foreach (int i in CharacterRoster.SelectionOrder())
		{
			if (!IsSelectable(i))
				continue;
			grid.AddChild(wideLayout ? BuildCompactCard(characters[i], i, cardStates[i]) : BuildCard(characters[i], i, cardStates[i]));
		}

		// Global.SelectedCharacterIdx starts at 0, which is the Test Wizard, so a fresh launch would open
		// the detail pane on a wizard who has no card. It falls to the first playable card in the screen's
		// order - The Archmage on every save, a starter drawn first.
		if (!IsSelectable(Global.SelectedCharacterIdx))
			Global.SelectedCharacterIdx = FirstSelectableIndex();

		if (wideLayout)
		{
			BuildDetailPane();
			ShowDetail(Global.SelectedCharacterIdx);
		}

		var backButton = GetNodeOrNull<Button>("BackButton");
		if (backButton != null)
			backButton.Pressed += OnBackButtonPressed;

		// Screen headings take the display face. They carry a Display font SIZE from the scene
		// file but no FACE, so without this the card titles below them came out in Cinzel and
		// the heading above them in the body face - the wrong way round.
		ResponsiveLayout.SetFont(GetNodeOrNull<Label>("Title"), ResponsiveLayout.TextRole.Display);
		ApplyMenuSkin();

		// Last, once every card exists: this grants the selection a pad needs and rings whatever
		// the player points at, with a stick or with a mouse.
		// Clear of the phone's rounded corners and camera hole; nothing on desktop (SafeArea).
		SafeArea.Apply(this);
		MenuNavigator.Attach(this);
	}

	public override void _ExitTree()
	{
		// The test wizard's picker is lifted out of the detail pane, not freed, while another wizard
		// is shown there (ShowDetail), so on leaving the screen with someone else in the pane it is
		// in no tree and nothing frees it - and an OptionButton owns a popup window, so the leak at
		// exit was a whole viewport, its render target and its text.
		if (testWizardPickerHost != null && IsInstanceValid(testWizardPickerHost) && !testWizardPickerHost.IsInsideTree())
			testWizardPickerHost.Free();
		testWizardPickerHost = null;
	}

	private void PopulateTestWizardSpellOptions()
	{
		if (testWizardSpellPicker == null)
			return;

		testWizardSpellIds.Clear();
		testWizardSpellPicker.Clear();

		foreach (var (id, path) in TestWizardSpellOptions)
		{
			var data = ResourceLoader.Load<SpellData>(path);
			if (data == null)
				continue;

			// A spell taken in a form (SpellData.Variants) is listed once per form, so the Test Wizard can
			// open on any of them: the selection names the form ("spell:form", SpellVariants.Selection).
			if (data.HasVariants)
			{
				foreach (SpellVariant variant in data.Variants)
				{
					if (variant == null)
						continue;
					testWizardSpellPicker.AddItem(string.IsNullOrWhiteSpace(variant.DisplayName) ? $"{data.Name} ({variant.Id})" : variant.DisplayName);
					testWizardSpellIds.Add(SpellVariants.Selection(id, variant.Id));
				}
				continue;
			}

			testWizardSpellPicker.AddItem(data.Name);
			testWizardSpellIds.Add(id);
		}

		foreach (var (id, name) in TestWizardPassiveOptions)
		{
			testWizardSpellPicker.AddItem($"{name} (Passive)");
			testWizardSpellIds.Add(id);
		}

		if (testWizardSpellIds.Count == 0)
			return;

		string selectedId = string.IsNullOrWhiteSpace(Global.TestWizardStartingSpellId)
			? defaultTestWizardSpellId
			: Global.TestWizardStartingSpellId;

		int selectedIndex = testWizardSpellIds.FindIndex(id => id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
		if (selectedIndex < 0)
			selectedIndex = 0;

		testWizardSpellPicker.Select(selectedIndex);
		Global.TestWizardStartingSpellId = testWizardSpellIds[selectedIndex];
	}

	private void OnTestWizardSpellSelected(long index)
	{
		if (index < 0 || index >= testWizardSpellIds.Count)
			return;

		Global.TestWizardStartingSpellId = testWizardSpellIds[(int)index];
	}

	private void ApplyMenuSkin()
	{
		// The same plate the stage list uses. It was a bought GUI backdrop - the last reference to
		// assets/organized/ anywhere in the project - and the two selection screens sit next to each
		// other in the flow, so sharing one backdrop is also the right answer independently.
		BonelightSkin.ApplyFullscreenBackdrop(this, "res://assets/bonelight/ui/menu-background.png", 0.96f);
		// No panel on the roster any more. ApplyPanel on a ScrollContainer added its backdrop as a second
		// child of the scroll, which lays it out as content at its minimum size - nothing - so it never
		// drew; the cards carry their own faces now (CardFace).
		BonelightSkin.ApplyButtonsInTree(this, 5);
		BonelightSkin.StyleButton(GetNodeOrNull<Button>("BackButton"));
	}

	private void LayoutHeader()
	{
		if (GetNodeOrNull<Button>("BackButton") is Button back)
		{
			back.OffsetLeft = 20f;
			back.OffsetRight = 20f + BackWidth;
			back.OffsetTop = HeaderTop;
			back.OffsetBottom = RosterTop - 12f;
			ResponsiveLayout.SetFont(back, ResponsiveLayout.TextRole.Label);
		}
		if (GetNodeOrNull<Label>("Title") is Label title)
		{
			// Inset by Back's width and its margin on BOTH sides, so the heading stays centred on the
			// screen and never reaches Back, whatever the safe area does to the left edge.
			float inset = 20f + BackWidth + 16f;
			title.OffsetLeft = inset;
			title.OffsetRight = -inset;
			title.OffsetTop = HeaderTop;
			title.OffsetBottom = RosterTop - 12f;
			// The heading gold every stone screen's heading wears (CodexRows.Stone.Heading).
			title.AddThemeColorOverride("font_color", CodexRows.Stone.Heading);
		}
	}

	// A card's face and the well its portrait stands in, in CodexRows' stone register - the rows of the
	// Achievements list the roster leads to. They were Godot's default panel grey, with a flat grey box
	// behind each face. The well is a step lighter than the face (stone lit) so a locked wizard's black
	// silhouette still reads as a shape on it.
	private static readonly Color PortraitWellColor = new(0.165f, 0.204f, 0.267f);

	private static StyleBoxFlat CardFace()
	{
		var style = new StyleBoxFlat { BgColor = CodexRows.Stone.Face, BorderColor = CodexRows.Stone.Edge };
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(6);
		return style;
	}

	private static PanelContainer BuildPortraitWell(Vector2 size)
	{
		var well = new PanelContainer { CustomMinimumSize = size, MouseFilter = Control.MouseFilterEnum.Ignore };
		var style = new StyleBoxFlat { BgColor = PortraitWellColor };
		style.SetCornerRadiusAll(4);
		well.AddThemeStyleboxOverride("panel", style);
		return well;
	}

	// THE STARTING SPELL ON THE PORTRAIT (Eric, 2026-11-03: "in the corner of their portraits on character select
	// put a little icon showing their starting spell"). The spell's own icon - the one the HUD's loadout strip and
	// the level-up cards show - tucked into the portrait's bottom-right corner: the face is at the top of the 48
	// cell, and the name is under the well on a PC card and beside it on a phone, so that corner is clear of both.
	//
	// Pixel-honest: the icon is 32 art pixels stored at x2, drawn at a whole number of SCREEN pixels per art pixel
	// (PixelArtRect, TexelsPerArtPixel 2), the portrait's own rule. The box decides which whole scale a window gets:
	// 36 on a PC card keeps it at 32 units on a 1:1 window and one screen pixel per art pixel at x1.5 (a third of
	// the face's width at two); 44 on a phone card and in the detail pane lets x1.5 - the Pixel 9 - take two, since
	// 32 device pixels there is two millimetres and unreadable. On a dark plate, so it reads as laid ON the portrait
	// rather than as something the figure holds.
	//
	// NEVER ON A WIZARD NOT YET FREE: a silhouette carrying its spell would say what that wizard opens on, which is
	// what freeing him tells you. A secret or a locked card gets nothing here.
	private const float CardBadgeBox = 36f;
	private const float LargeBadgeBox = 44f;
	private const int BadgeInset = 4;
	private static readonly Color BadgePlateColor = new(0.043f, 0.059f, 0.094f, 0.92f);   // stone deep, the bands' ground

	private static Control BuildSpellBadge(CharacterData character, CardState state, float box)
	{
		if (state != CardState.Unlocked)
			return null;
		Texture2D icon = StartingSpellIcon(character);
		if (icon == null)
			return null;

		var plate = new StyleBoxFlat { BgColor = BadgePlateColor, BorderColor = CodexRows.Stone.Edge };
		plate.SetBorderWidthAll(1);
		plate.SetCornerRadiusAll(3);
		var art = new PixelArtRect(new[] { icon }, 1, box)
		{
			Name = "SpellBadge",
			TexelsPerArtPixel = 2,
			Align = new Vector2(1f, 1f),
			Plate = plate,
		};
		// Laid into a PanelContainer (the well) beside the portrait, which fits every child to the whole well unless
		// told otherwise: ShrinkEnd both ways puts the badge at its own size in the bottom-right corner.
		var corner = new MarginContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
			SizeFlagsVertical = Control.SizeFlags.ShrinkEnd,
		};
		corner.AddThemeConstantOverride("margin_right", BadgeInset);
		corner.AddThemeConstantOverride("margin_bottom", BadgeInset);
		corner.AddChild(art);
		return corner;
	}

	// The icon of the spell a wizard opens on, in the form he opens it in if it has forms (CharacterData.StartingForm:
	// ApotheosisCatalog's VariantId, the one rule for which form is his) - the picture his first run's loadout strip shows.
	private static Texture2D StartingSpellIcon(CharacterData character)
	{
		SpellData spell = character?.StartingSpellResource;
		if (spell == null)
			return null;
		// Null for a wizard who chooses his form each run (Ma Liang's Familiar, CharacterData.ChoosesStartingForm):
		// his card shows the spell's own picture, not one form of it.
		return character.StartingForm?.Icon ?? spell.Icon;
	}

	// And its name, the same way: Nimue's card says "Spirit Sword", the form she opens in, not the spell's own name,
	// which is the name of a choice she never makes.
	private static string StartingSpellName(CharacterData character)
	{
		SpellVariant form = character?.StartingForm;
		return form != null && !string.IsNullOrWhiteSpace(form.DisplayName) ? form.DisplayName : character?.StartingSpellResource?.Name ?? string.Empty;
	}

	// A wizard's craft under his name ("the Pyromancer"), for a starter who carries his own name (CharacterData.Craft);
	// null for everyone whose name already says what he is, and for any card not yet free.
	private static Label BuildCraftLine(CharacterData character, CardState state, ResponsiveLayout.TextRole role)
	{
		if (state != CardState.Unlocked || string.IsNullOrWhiteSpace(character.CraftTitle))
			return null;
		Label craft = CodexRows.MakeText(character.CraftTitle, role, CodexRows.Stone.InkMeta);
		craft.Name = "CraftLine";
		return craft;
	}

	/// <summary>
	/// What a wizard brings, as a short table: what each line is in the meta ink, what it is in the row
	/// ink, the passive led by its stat glyph. It was three centred sentences ("Starting Spell: Cone of
	/// Cold", "Passive Bonus: +9 HP regeneration a minute"), each wrapping wherever its card ran out, so
	/// no two started in the same place. A wizard not yet freed shows how he is found instead.
	/// </summary>
	// Whether the cards may name a wizard's Apotheosis (Teachings.IsShown), read once per build of the roster.
	private static bool apotheosisShown = true;

	private static Control BuildFacts(CharacterData character, CardState state)
	{
		if (state != CardState.Unlocked)
		{
			// A lock that does not explain itself is indistinguishable from missing content. A secret
			// explains nothing on purpose, and says only that it is one.
			Label hint = CodexRows.MakeText(LockedLine(character, state), ResponsiveLayout.TextRole.Body, CodexRows.Stone.InkSoft);
			hint.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			return hint;
		}

		var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		grid.AddThemeConstantOverride("h_separation", 14);
		grid.AddThemeConstantOverride("v_separation", 8);
		void Add(string key, Control value)
		{
			Label keyLabel = CodexRows.MakeText(key, ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkMeta);
			keyLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			// Level with the first line of its value, which can wrap to two (a long passive).
			keyLabel.VerticalAlignment = VerticalAlignment.Top;
			keyLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
			grid.AddChild(keyLabel);
			value.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			grid.AddChild(value);
		}

		if (character.StartingSpellResource is SpellData spell)
			Add("Spell", BuildSpellValue(StartingSpellIcon(character) ?? spell.Icon, StartingSpellName(character)));
		// The wizard's final form, named where he is chosen (Eric's playtest, 2026-11-01: "make it clearer
		// that apotheosis is an option"), in the ember register the strip's button and the ascension
		// browser's entry wear. The name only: what the form does stays sealed until a run first offers
		// it (.ai/level-up-screen.md 4e). None for a wizard without one - the Test Wizard.
		// Not before the Ascension is learned (Teachings.IsShown, Eric's Q8 - one locked thing on show at a time):
		// a final form named to a save that has not met the Ascension it needs is two systems at once.
		ApotheosisDefinition apotheosis = ApotheosisCatalog.ForCharacter(character.Id);
		if (apotheosis?.Form != null && apotheosisShown)
			Add("Apotheosis", BuildApotheosisValue(character, apotheosis));
		if (!string.IsNullOrWhiteSpace(character.StartingPassiveDescription))
			Add("Passive", StatIcons.BuildLine(character.StartingPassiveDescription, CodexRows.Stone.Ink, ResponsiveLayout.TextRole.Body, expand: false));
		if (character.IsLegendaryStart)
			Add("Starts", CodexRows.MakeText("Legendary", ResponsiveLayout.TextRole.Body, CodexRows.Stone.Reward));
		return grid;
	}

	// The form's name in the ember register. For a wizard whose final form belongs to one form of a spell he chooses each
	// run (Ma Liang: the Familiar's crow, .ai/chapter-wizards.md 4.8), the line under it says which, and that the other
	// forms have none - taken as the dog or the cat, he never meets it, and the card is the only place that can say so.
	private static Control BuildApotheosisValue(CharacterData character, ApotheosisDefinition apotheosis)
	{
		Label name = CodexRows.MakeText($"\u2736 {apotheosis.Form.DisplayName}", ResponsiveLayout.TextRole.Body, CardFamilyArt.ApotheosisTitle);
		SpellData spell = character.StartingSpellResource;
		if (string.IsNullOrEmpty(apotheosis.VariantId) || spell == null || !spell.HasVariants)
			return name;
		string Short(SpellVariant v) => (string.IsNullOrWhiteSpace(v.ShortName) ? v.DisplayName : v.ShortName).ToLowerInvariant();
		SpellVariant own = spell.FindVariant(apotheosis.VariantId);
		var others = spell.Variants.Where(v => v != null && v != own).Select(Short).ToList();
		if (own == null)
			return name;
		string rest = others.Count == 0 ? string.Empty
			: $"; the {string.Join(" and the ", others)} {(others.Count == 1 ? "has" : "have")} none";
		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 0);
		column.AddChild(name);
		Label which = CodexRows.MakeText($"As the {Short(own)}{rest}", ResponsiveLayout.TextRole.Label, CodexRows.Stone.InkSoft);
		which.Name = "ApotheosisForm";
		column.AddChild(which);
		return column;
	}

	// The starting spell with its own icon before it, as the Spell Index lists it.
	private static Control BuildSpellValue(Texture2D icon, string spellName)
	{
		var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		line.AddThemeConstantOverride("separation", 8);
		if (icon != null)
		{
			line.AddChild(new TextureRect
			{
				Texture = icon,
				CustomMinimumSize = new Vector2(28f, 28f),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			});
		}
		Label name = CodexRows.MakeText(spellName, ResponsiveLayout.TextRole.Body, CodexRows.Stone.Ink);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		line.AddChild(name);
		return line;
	}

	// Card width the grid sizes its column count against; see ResponsiveLayout.BindGridColumns.
	private const float CardMinWidth = 280f;

	// What a card shows for a character with no Portrait of its own. It was a bought dungeon-pack
	// priest - the last character sheet in the game we could not publish, and a priest standing
	// in for a wizard. It is a hooded figure with no face now, which is the honest picture for
	// "not drawn yet": the cast gains eight rescued wizards long before all eight have art.
	// Loaded where it is used, not held in a static: a static Texture2D outlives the engine's own
	// reference and trips the finalizer (BonelightSkin's remarks). ResourceLoader caches it.
	private const string SharedWizardFrame1Path = "res://assets/bonelight/characters/unknown-portrait.png";

	// A secret wizard's card: a wax seal with a question struck in it (tools/art/props_fx.py,
	// sealed_portrait). It is drawn on the same 48 cell as the wizards so it sits on their grid.
	private const string SealedPortraitPath = "res://assets/bonelight/characters/sealed-portrait.png";

	// The line a secret's card carries in place of a hint. It says there is someone, not how to
	// find him: a hint here would be the spoiler the secret exists to hold back.
	private const string SecretLine = "Not yet discovered.";

	private static readonly Color HiddenNameInk = new(0.72f, 0.72f, 0.78f);

	private static string CardName(CharacterData character, CardState state) =>
		state == CardState.Unlocked ? character.Name : "???";

	private static Color CardNameInk(CharacterData character, CardState state) =>
		state == CardState.Unlocked ? CharacterVisuals.GetCharacterTint(character.Id).Lightened(0.18f) : HiddenNameInk;

	// A sealed card says only that there is someone - except one sealed until he is free, whose card carries
	// his vague line instead (UnlockDefinition.SealedUntilFreed): it names nobody and no place.
	private static string LockedLine(CharacterData character, CardState state) =>
		state == CardState.Secret && UnlockCatalog.GetCharacter(character.Id)?.SealedUntilFreed != true
			? SecretLine
			: UnlockCatalog.GetLockedHint(character.Id, UnlockKind.Character);

	// A roster card on a phone: one row, the face in its well on the left, the name and the facts on
	// the right. The whole card is the hit area (the overlay laid first, everything else click-through);
	// pressing it chooses the wizard.
	private Control BuildCard(CharacterData character, int idx, CardState state)
	{
		bool unlocked = state == CardState.Unlocked;
		var card = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		card.AddThemeStyleboxOverride("panel", CardFace());

		var cardButton = BonelightSkin.MakeCardClickOverlay(unlocked);
		cardButton.Name = $"CharacterButton{idx + 1}";
		int capturedIdx = idx;
		cardButton.Pressed += () => OnCharButtonPressed(capturedIdx);
		card.AddChild(cardButton);

		var margin = new MarginContainer();
		foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
			margin.AddThemeConstantOverride(side, 12);
		card.AddChild(margin);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 16);
		margin.AddChild(row);

		// The 48 cell at x3 in a 160 well, the largest whole scale that fits beside the words.
		PanelContainer well = BuildPortraitWell(new Vector2(160f, 160f));
		well.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
		var portraitCenter = new CenterContainer();
		well.AddChild(portraitCenter);
		portraitCenter.AddChild(BuildPortraitDisplay(character, state));
		if (BuildSpellBadge(character, state, LargeBadgeBox) is Control badge)
			well.AddChild(badge);
		row.AddChild(well);

		var text = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		text.AddThemeConstantOverride("separation", 10);
		row.AddChild(text);

		Label nameLabel = CodexRows.MakeText(CardName(character, state), ResponsiveLayout.TextRole.Title, CardNameInk(character, state));
		// The name and its craft as one block ("Aedan" over "the Pyromancer"), closer to each other than to the facts:
		// a phone has no detail pane, so this card is the one place the craft is said.
		if (BuildCraftLine(character, state, ResponsiveLayout.TextRole.Label) is Label craftLine)
		{
			var nameBlock = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			nameBlock.AddThemeConstantOverride("separation", 0);
			nameBlock.AddChild(nameLabel);
			nameBlock.AddChild(craftLine);
			text.AddChild(nameBlock);
		}
		else
		{
			text.AddChild(nameLabel);
		}
		text.AddChild(BuildFacts(character, state));

		Control testWizardPicker = null;
		if (unlocked && character.Id.Equals("test_wizard", StringComparison.OrdinalIgnoreCase))
		{
			testWizardPickerHost ??= BuildTestWizardPicker();
			text.AddChild(testWizardPickerHost);
			testWizardPicker = testWizardPickerHost;
		}

		// The test wizard's spell dropdown is the one thing on a card that must keep its own input;
		// everything else lets the click fall through to the overlay button underneath.
		BonelightSkin.MakeSubtreeClickThrough(margin, testWizardPicker);
		return card;
	}

	// A roster card on a PC: the face and the name, nothing else - the words live in the detail
	// pane. Hovering or focusing it shows that wizard there; pressing it chooses him, as on a phone.
	private Control BuildCompactCard(CharacterData character, int idx, CardState state)
	{
		bool unlocked = state == CardState.Unlocked;
		var card = new PanelContainer { CustomMinimumSize = new Vector2(CompactCardWidth, CompactCardHeight) };
		card.AddThemeStyleboxOverride("panel", CardFace());

		var cardButton = BonelightSkin.MakeCardClickOverlay(unlocked);
		cardButton.Name = $"CharacterButton{idx + 1}";
		int capturedIdx = idx;
		cardButton.Pressed += () => OnCharButtonPressed(capturedIdx);
		cardButton.FocusEntered += () => ShowDetail(capturedIdx);
		cardButton.MouseEntered += () => ShowDetail(capturedIdx);
		card.AddChild(cardButton);

		var margin = new MarginContainer();
		foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
			margin.AddThemeConstantOverride(side, 8);
		card.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 4);
		margin.AddChild(vbox);

		PanelContainer portraitBg = BuildPortraitWell(new Vector2(0, CompactPortraitBox));
		var portraitCenter = new CenterContainer();
		portraitBg.AddChild(portraitCenter);
		portraitCenter.AddChild(BuildPortraitDisplay(character, state, box: (int)CompactPortraitBox));
		if (BuildSpellBadge(character, state, CardBadgeBox) is Control badge)
			portraitBg.AddChild(badge);
		vbox.AddChild(portraitBg);

		var nameLabel = new Label
		{
			Text = CardName(character, state),
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		ResponsiveLayout.SetFont(nameLabel, ResponsiveLayout.TextRole.Label);
		nameLabel.AddThemeColorOverride("font_color", CardNameInk(character, state));
		// The name may use the card's side margins as well as the well's width (NameBleed each side): at Micro,
		// "Wayland the Smith" was still a few pixels wider than the well and wrapped onto a cut-off second line.
		var nameRow = new MarginContainer();
		nameRow.AddThemeConstantOverride("margin_left", -NameBleed);
		nameRow.AddThemeConstantOverride("margin_right", -NameBleed);
		nameRow.AddChild(nameLabel);
		vbox.AddChild(nameRow);
		// Fitted once it is in the tree: before that ResponsiveLayout cannot tell a PC from a phone, and took the
		// phone's floor (18) for Micro - one point too high for "Wayland the Smith" on a PC card.
		nameLabel.Ready += () => FitOneLine(nameLabel, CompactCardWidth - 16f + 2f * NameBleed);

		BonelightSkin.MakeSubtreeClickThrough(margin, null);
		return card;
	}

	// A name too long for the card's one line ("The Dark Wizard", and the chapter wizards to come - the Cumaean
	// Sibyl, Wayland the Smith) is set a point or two smaller, never below Micro, rather than wrapped onto a
	// second line the compact card has no room for: the wrapped half was cut off under its frame.
	private static void FitOneLine(Label label, float width)
	{
		Font font = label.GetThemeFont("font");
		if (font == null || string.IsNullOrEmpty(label.Text))
			return;
		int size = label.GetThemeFontSize("font_size");
		int floor = ResponsiveLayout.FontSize(label, ResponsiveLayout.TextRole.Micro);
		while (size > floor && font.GetStringSize(label.Text, HorizontalAlignment.Left, -1f, size).X > width)
			size--;
		label.AddThemeFontSizeOverride("font_size", size);
		// Fitted, it is one line: no wrap left to break it on a pixel of rounding.
		if (font.GetStringSize(label.Text, HorizontalAlignment.Left, -1f, size).X <= width)
			label.AutowrapMode = TextServer.AutowrapMode.Off;
	}

	// How far into the card's 8 px side margins a compact card's name may run.
	private const int NameBleed = 3;

	private void BuildDetailPane()
	{
		var pane = new PanelContainer { Name = "DetailPane" };
		pane.AnchorLeft = RosterFraction;
		pane.AnchorRight = 1f;
		pane.AnchorTop = 0f;
		pane.AnchorBottom = 1f;
		pane.OffsetLeft = 12f;
		pane.OffsetRight = -24f;
		pane.OffsetTop = RosterTop;
		pane.OffsetBottom = -RosterBottomMargin;
		// The stone slab every other panel in the game stands on, in place of a thin grey outline.
		pane.AddThemeStyleboxOverride("panel", BonelightSkin.PanelStyle());
		AddChild(pane);

		detailBox = new VBoxContainer();
		detailBox.AddThemeConstantOverride("separation", 14);
		pane.AddChild(detailBox);
	}

	// Rebuilt rather than updated in place: it is seven wizards, built on a hover, and a pane that
	// rebuilds cannot leave one wizard's line behind under the next.
	private void ShowDetail(int idx)
	{
		if (detailBox == null || idx < 0 || idx >= characters.Count || idx == detailIdx)
			return;
		detailIdx = idx;
		CharacterData character = characters[idx];
		CardState state = idx < cardStates.Count ? cardStates[idx] : CardState.Secret;
		bool unlocked = state == CardState.Unlocked;

		// The test wizard's picker is created once and only moved in and out, so a hover never
		// resets the spell the player picked.
		if (testWizardPickerHost != null && testWizardPickerHost.GetParent() == detailBox)
			detailBox.RemoveChild(testWizardPickerHost);
		foreach (Node child in detailBox.GetChildren())
			child.QueueFree();

		var portraitCenter = new CenterContainer { CustomMinimumSize = new Vector2(0, 260f) };
		Control portrait = BuildPortraitDisplay(character, state, scale: 5);
		if (BuildSpellBadge(character, state, LargeBadgeBox) is Control badge)
		{
			// The big portrait carries its spell too, in the same corner: stacked in a bare PanelContainer the size of
			// the portrait's box, so the badge sits on the portrait's corner rather than on the pane's.
			var stack = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			stack.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
			stack.AddChild(portrait);
			stack.AddChild(badge);
			portrait = stack;
		}
		portraitCenter.AddChild(portrait);
		detailBox.AddChild(portraitCenter);

		var name = new Label { Text = CardName(character, state), HorizontalAlignment = HorizontalAlignment.Center };
		ResponsiveLayout.SetFont(name, ResponsiveLayout.TextRole.Display);
		name.AddThemeColorOverride("font_color", CardNameInk(character, state));
		// The heading says the name ("Aedan") and the line under it the craft ("the Pyromancer"), tight under it as
		// its title, so a starter's discipline is never lost to his name (CharacterData.Craft).
		if (BuildCraftLine(character, state, ResponsiveLayout.TextRole.Body) is Label craftLine)
		{
			craftLine.HorizontalAlignment = HorizontalAlignment.Center;
			var nameBlock = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			nameBlock.AddThemeConstantOverride("separation", -2);
			nameBlock.AddChild(name);
			nameBlock.AddChild(craftLine);
			detailBox.AddChild(nameBlock);
		}
		else
		{
			detailBox.AddChild(name);
		}

		var facts = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		facts.AddThemeConstantOverride("margin_left", 8);
		facts.AddThemeConstantOverride("margin_right", 8);
		facts.AddChild(BuildFacts(character, state));
		detailBox.AddChild(facts);

		detailBox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });

		if (unlocked && character.Id.Equals("test_wizard", StringComparison.OrdinalIgnoreCase))
		{
			testWizardPickerHost ??= BuildTestWizardPicker();
			detailBox.AddChild(testWizardPickerHost);
		}

		if (unlocked)
		{
			var choose = new Button
			{
				Text = $"Choose {character.Name}",
				CustomMinimumSize = new Vector2(0, 52f),
				FocusMode = Control.FocusModeEnum.None
			};
			// The one thing this screen is for, so it rests on the lit frame (Start Run's tier).
			BonelightSkin.StyleButton(choose, BonelightSkin.Emphasis.Primary);
			ResponsiveLayout.SetFont(choose, ResponsiveLayout.TextRole.Body);
			int capturedIdx = idx;
			choose.Pressed += () => OnCharButtonPressed(capturedIdx);
			detailBox.AddChild(choose);
		}
	}

	private Control BuildTestWizardPicker()
	{
		var host = new VBoxContainer();
		host.AddThemeConstantOverride("separation", 6);
		var pickerLabel = new Label { Text = "Start Spell", HorizontalAlignment = HorizontalAlignment.Center };
		ResponsiveLayout.SetFont(pickerLabel, ResponsiveLayout.TextRole.Label);
		host.AddChild(pickerLabel);
		testWizardSpellPicker = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		PopulateTestWizardSpellOptions();
		testWizardSpellPicker.ItemSelected += OnTestWizardSpellSelected;
		host.AddChild(testWizardSpellPicker);
		return host;
	}

	private static Control BuildPortraitDisplay(CharacterData character, CardState state, int scale = 3, int box = 0)
	{
		// The box holds the 48px cell at a whole scale: 160 at 3x for a card, 256 at 5x for the pane,
		// 148 for the PC roster's compact card (CompactPortraitBox). x3 is the largest whole scale that
		// fits a card - x8 once showed only the middle of the coat, and the whole figure is the point
		// now that each wizard is told apart by silhouette.
		if (box <= 0)
			box = scale == 3 ? 160 : 48 * scale + 16;
		Texture2D portrait = character.Portrait ?? GD.Load<Texture2D>(SharedWizardFrame1Path);

		// Drawn straight onto the canvas at a whole number of SCREEN pixels per art pixel, which is
		// what keeps it sharp on a big or fractionally-scaled window. It used to go through a
		// SubViewport at canvas resolution, which filtered it bilinearly and was then stretched by
		// the window: the blur the user saw full screen (PixelArtRect's remarks).
		// Every wizard not yet free is his silhouette, a secret one included (Eric, 2026-11-02: "always show
		// silhouettes of characters not unlocked yet"); a secret keeps only his "Not yet discovered" line.
		// The wax seal (SealedPortraitPath) is no longer drawn here.
		List<Texture2D> frames = state switch
		{
			CardState.Secret or CardState.Locked => CharacterVisuals.BuildSilhouetteFrames(CharacterVisuals.GetIdleFrames(portrait)),
			_ => CharacterVisuals.GetIdleFrames(portrait),
		};
		return new PixelArtRect(frames, scale, box)
		{
			FramesPerSecond = CharacterVisuals.IdleFramesPerSecond,
			// White for every wizard with art of his own (CharacterVisuals.GetCharacterTint). The
			// silhouette and the seal are drawn in their final colours and take no tint.
			Modulate = state == CardState.Unlocked ? CharacterVisuals.GetCharacterTint(character.Id) : Colors.White,
		};
	}

	// The Test Wizard is a developer's tool, not a wizard (Eric, 2026-11-01: "Remove the test wizard from
	// character selection"). He stays first in CharacterRoster, so every tool that asks for --character=0
	// still gets him; only this screen leaves him out.
	private static bool IsHiddenFromSelection(CharacterData character) =>
		string.Equals(character?.Id, "test_wizard", System.StringComparison.OrdinalIgnoreCase);

	// Roster indices held off the screen until they are free (the dark wizard): never selectable either.
	private readonly HashSet<int> hiddenUntilFreed = new();

	private bool IsSelectable(int idx) =>
		idx >= 0 && idx < characters.Count && !IsHiddenFromSelection(characters[idx]) && !hiddenUntilFreed.Contains(idx);

	// The first wizard on the screen the save can play; the first on the screen at all if it can play none. In the
	// screen's order (CharacterRoster.SelectionOrder), so a fresh launch opens on the card drawn first.
	private int FirstSelectableIndex()
	{
		int firstShown = -1;
		foreach (int i in CharacterRoster.SelectionOrder())
		{
			if (!IsSelectable(i))
				continue;
			if (cardStates[i] == CardState.Unlocked)
				return i;
			if (firstShown < 0)
				firstShown = i;
		}
		return Math.Max(0, firstShown);
	}

	private void OnCharButtonPressed(int idx)
	{
		Global.SelectedCharacterIdx = idx;
		string chosen = idx >= 0 && idx < characters.Count ? characters[idx].Name : string.Empty;
		// To the world map (.ai/world-map.md), where the chosen wizard walks to a chapter. The stage
		// list (StageSelection) is still in the tree, and is what the map falls back to if its art
		// is missing.
		SceneTransition.ChangeScene(this, "res://scenes/WorldMap.tscn", chosen);
	}

	private void OnBackButtonPressed()
	{
		// The menu lives on the title screen now, so going "back to the menu" means loading
		// its shell and telling it to skip the press-any-key beat.
		Global.OpenMenuImmediately = true;
		SceneTransition.ChangeScene(this, "res://scenes/TitleScreen.tscn");
	}
}

