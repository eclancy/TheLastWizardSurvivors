using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WizardSurvivors.scripts;

public partial class MainMenu : Control
{
	private Button startRunButton = null!;
	// The player-facing names are Evolution Codex, Achievements and Powers. The button, panels, lists and
	// save keys behind them keep their older internal names (spellIndexButton, achievement*, the
	// ArcaneUpgradeLevels dictionary): the save is persisted, the harnesses press buttons by name, and the
	// rest is not worth the churn. The Spell Index's panel itself is gone: its button opens the Evolution
	// Codex (.ai/evolution-codex.md), which carries every spell, how to find the missing ones and Set aside.
	private Button spellIndexButton = null!;
	private Button unlocksButton = null!;
	private Button arcaneUpgradesButton = null!;
	private Button optionsButton = null!;
	private Button reportBugButton;
	private Button backFromArcaneButton = null!;
	private Button backFromAchievementsButton = null!;
	private Button backFromOptionsButton = null!;
	private Control mainPanel = null!;

	/// <summary>True on the root menu, false while a full-screen panel (Spell Index, Unlocks,
	/// Arcane Upgrades, Options) is open. TitleScreen reads it to hide the Android update strip,
	/// which would otherwise sit over the open panel's heading.</summary>
	public bool IsAtRoot => mainPanel == null || mainPanel.Visible;

	// Pulled out of the container stack by LayoutAroundTitleArt and pinned to the artwork, so
	// ShowPanel has to raise and lower them by hand - they are no longer children of mainPanel.
	private Control rootMenuButtons;
	private Label rootDetailLabel;
	// A 16:9 phone has no ground under the utility row, so the preset line goes above it in one
	// line instead of two (LayoutUtilityRow).
	private bool detailOnOneLine;

	// The player's Arcane Energy, said twice: small, under the Arcane Upgrades button on the root
	// menu (where the decision to go shopping is made), and large in the shop's own header (where
	// it is spent). It used to be one label alone in the top right corner of the title art, attached
	// to nothing, and the shop - the one screen that spends it - did not show it at all.
	private Label menuEnergyAmount;
	private Label shopEnergyAmount;
	private ColorRect scrim;
	private Control achievementsPanel = null!;
	private Control arcaneUpgradesPanel = null!;
	// The Options panel's rows are built and kept in MainMenu.Options.cs.
	private Control optionsPanel = null!;
	private VBoxContainer achievementList = null!;
	private VBoxContainer achievementSummary = null!;
	// The shop's list: a column of sections, each a grid of cards (MainMenu.Shop.cs).
	private VBoxContainer upgradeList = null!;
	private static readonly Texture2D DefaultSpellIcon = GD.Load<Texture2D>("res://assets/bonelight/ui/spells/unknown.png");

	private readonly Dictionary<string, UpgradeDefinition> upgradeDefinitions = new();
	// A boon's page in the shop (MainMenu.Shop.cs, BoonShopEntry). The name is the Spell Index's, whose rows
	// it once described; the Spell Index went to the Evolution Codex.
	private sealed class SpellbookEntry
	{
		public string Id = string.Empty;
		public string DisplayName = string.Empty;
		public string Description = string.Empty;
		// A boon's: the plain effect is Description, led by its stat glyph; the flavour goes under it
		// in the softer ink (MainMenu.Shop.cs, BoonShopEntry).
		public string Flavour = string.Empty;
		public string GlyphKey;
		public string Elements = string.Empty;
		public List<(Element Element, int Weight)> ElementList = new();
		public bool IsPassive;
		public Texture2D Icon;
	}

	private sealed class UpgradeDefinition
	{
		public string Id = string.Empty;
		public string DisplayName = string.Empty;
		public string EffectText = string.Empty;
		public int BaseCost;
		public int CostPerLevel;
		public int MaxLevel;
		// Curation is the one upgrade whose ceiling is not a constant: it rises as the spellbook
		// grows, so a fresh save is offered none of it and a finished one can prune hard.
		public bool HasPoolScaledMaxLevel;
		// A boon sold once (UnlockCatalog.PurchasableBoons), the one thing in the shop that is not a
		// levelled upgrade. Spells were sold the same way until schema 19; every spell is an
		// achievement's now (.ai/achievements-and-secrets.md 2.4).
		public string BoonId = string.Empty;
		public bool IsBoon => !string.IsNullOrEmpty(BoonId);
	}

	public override void _Ready()
	{
		ContentValidator.ValidateAtStartup(this);
		mainPanel = GetNode<Control>("MarginContainer/VBoxContainer/Content/MainPanel");
		arcaneUpgradesPanel = GetNode<Control>("MarginContainer/VBoxContainer/Content/ArcaneUpgradesPanel");
		optionsPanel = GetNode<Control>("MarginContainer/VBoxContainer/Content/OptionsPanel");
		EnsureUnlocksUi();
		EnsureShopHeader();

		startRunButton = GetNode<Button>("MarginContainer/VBoxContainer/Content/MainPanel/MenuButtons/StartRunButton");
		arcaneUpgradesButton = GetNode<Button>("MarginContainer/VBoxContainer/Content/MainPanel/MenuButtons/ProgressionRow/ShopColumn/ArcaneUpgradesButton");
		// The shop's one button (OnArcaneUpgradesPressed). Relabelled, not renamed: the harnesses
		// press it by its node name.
		arcaneUpgradesButton.Text = "Powers";
		unlocksButton = GetNode<Button>("MarginContainer/VBoxContainer/Content/MainPanel/MenuButtons/ProgressionRow/UnlocksButton");
		spellIndexButton = GetNode<Button>("SpellIndexButton");
		// The Spell Index's slot opens the Evolution Codex now; the node keeps its name for the harnesses.
		spellIndexButton.Text = EvolutionCodex.Title;
		optionsButton = GetNode<Button>("OptionsButton");
		EnsureMenuEnergyReadout();
		achievementList = achievementsPanel.FindChild("AchievementList", true, false) as VBoxContainer;
		achievementSummary = achievementsPanel.FindChild("AchievementSummary", true, false) as VBoxContainer;
		upgradeList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/Content/ArcaneUpgradesPanel/ArcaneUpgradesVBox/UpgradeScroll/UpgradeGutter/UpgradeList");
		upgradeScroll = GetNode<ScrollContainer>("MarginContainer/VBoxContainer/Content/ArcaneUpgradesPanel/ArcaneUpgradesVBox/UpgradeScroll");
		backFromArcaneButton = GetNode<Button>("MarginContainer/VBoxContainer/Content/ArcaneUpgradesPanel/ArcaneUpgradesVBox/BackFromArcaneButton");
		backFromAchievementsButton = GetNode<Button>("MarginContainer/VBoxContainer/Content/AchievementsPanel/AchievementsVBox/BackFromAchievementsButton");
		backFromOptionsButton = GetNode<Button>("MarginContainer/VBoxContainer/Content/OptionsPanel/OptionsVBox/BackFromOptionsButton");

		// After every GetNode above, never before: this moves the button stack out of the scene's
		// container tree, and the paths have to resolve against the scene as it was authored.
		LayoutAroundTitleArt();
		LayoutUtilityRow();
		BuildOptions();
		ApplyMenuSkin();

		// The shop needs its definitions before its cards; the boons' pages read BoonCatalog, not these
		// entries (the shop's spell pages did, until it stopped selling spells).
		BuildUpgradeDefinitions();
		BuildShop();
		BuildAchievementCards();

		startRunButton.Pressed += OnStartRunPressed;
		spellIndexButton.Pressed += OnCodexPressed;
		unlocksButton.Pressed += OnAchievementsPressed;
		arcaneUpgradesButton.Pressed += OnArcaneUpgradesPressed;
		optionsButton.Pressed += OnOptionsPressed;
		backFromArcaneButton.Pressed += ShowMainPanel;
		backFromAchievementsButton.Pressed += ShowMainPanel;
		backFromOptionsButton.Pressed += ShowMainPanel;

		RefreshArcaneEnergy();
		RefreshUpgradeControls();
		RefreshAchievementCards();
		ShowMainPanel();
		PlayMenuMusic();

		// This screen swaps whole panels in and out rather than changing scene, and the navigator
		// only ever considers controls that are visible in the tree - so one attach here covers the
		// main panel, the Spell Index, Unlocks, Arcane Upgrades and the options.
		// Start Run named explicitly: it is buried in a MarginContainer while the utility buttons are
		// direct children of the root, so tree order alone would open the game with one of them
		// selected. From there the stick walks the screen top to bottom: Start Run, the progression
		// row under it, then the utility row along the foot (Godot's own geometric focus search).
		// Clear of the phone's rounded corners and camera hole; nothing on desktop (SafeArea).
		SafeArea.Apply(this);
		MenuNavigator.Attach(this, startRunButton);
		GetViewport().SizeChanged += OnViewportSizeChanged;
	}

	public override void _ExitTree()
	{
		if (GetViewport() is Viewport viewport)
			viewport.SizeChanged -= OnViewportSizeChanged;
	}

	private void ApplyMenuSkin()
	{
		// No fullscreen backdrop here any more. It inserted ui-png-bg-1.png at child index 0, i.e.
		// *behind* the scene's own opaque Background - so it was never visible, and it was a second
		// unrelated image competing for the same job.

		// The two panel headings that live in the scene rather than in code: the scene gives them a
		// Display size but no face.
		foreach (string title in new[] {
			"MarginContainer/VBoxContainer/Content/ArcaneUpgradesPanel/ArcaneUpgradesVBox/ArcaneUpgradesTitle",
			"MarginContainer/VBoxContainer/Content/OptionsPanel/OptionsVBox/OptionsTitle" })
			ResponsiveLayout.SetFont(GetNodeOrNull<Label>(title), ResponsiveLayout.TextRole.Display);
		BonelightSkin.ApplyPanel(achievementsPanel);
		BonelightSkin.ApplyPanel(arcaneUpgradesPanel);
		BonelightSkin.ApplyPanel(optionsPanel);

		// The five menu buttons used to be identical 360x56 rows, which meant "Start Run" and
		// "Options" had exactly the same visual weight and the player had to read all six to find
		// the one they wanted. Size does most of the work now; these three tiers do the rest, so
		// the hierarchy still reads at a glance and in a screenshot.
		// No icons on the menu buttons. The fantasy-pack glyphs are a different art language from
		// the Bonelight background - a gold play triangle and two saturated blue blobs against a
		// cold cyan field - and at 22px they read as clutter rather than as symbols. Checked in a
		// screenshot; size and tier styling carry the hierarchy on their own.
		StyleMenuTier(startRunButton, MenuTier.Primary);

		// The progression row is the two places that make the next run stronger: what you buy
		// (Arcane Upgrades) and what you earn (Unlocks). The Spell Index used to stand between them
		// as a peer, and it is not one - it changes nothing, it is the catalogue you look things up
		// in - so it went down to the utilities with Options and Report a bug.
		foreach (Button secondary in new[] { arcaneUpgradesButton, unlocksButton })
		{
			StyleMenuTier(secondary, MenuTier.Secondary);
			ResponsiveLayout.SetFont(secondary, ResponsiveLayout.TextRole.Label);
		}

		// The corner button gets the gear. An icon in a corner is what makes it findable without a
		// label loud enough to compete with the stack above it.
		BonelightSkin.StyleButton(optionsButton, "res://assets/bonelight/ui/icons/gear.png", 48);
		ResponsiveLayout.SetFont(optionsButton, ResponsiveLayout.TextRole.Label);
		// A reference, not a destination: the quiet frame Report a bug wears, at the same size.
		StyleMenuTier(spellIndexButton, MenuTier.Tertiary);
		ResponsiveLayout.SetFont(spellIndexButton, ResponsiveLayout.TextRole.Label);

		BonelightSkin.StyleButton(backFromArcaneButton);
		// In the carved face, as the other panels' Back is: the scene gave this one a size and the
		// prose face, so Powers was the one panel whose Back read differently (menu pass, 2026-11-01).
		ResponsiveLayout.SetFont(backFromArcaneButton, ResponsiveLayout.TextRole.Title);
		BonelightSkin.StyleButton(backFromAchievementsButton);
		BonelightSkin.StyleButton(backFromOptionsButton);
	}

	private enum MenuTier { Primary, Secondary, Tertiary }

	/// <summary>
	/// Re-styles a menu button for its place in the hierarchy, on top of the shared skin.
	/// </summary>
	/// <remarks>
	/// Deliberately local rather than pushed into BonelightSkin: "which of these is the important
	/// one" is a question about this screen's layout, not a property of buttons in general, and
	/// every other screen in the game has a flat set where one tier is the right answer.
	/// </remarks>
	private static void StyleMenuTier(Button button, MenuTier tier)
	{
		if (button == null)
			return;

		// The three tiers survive the reskin; what carries them does not. They used to be three
		// fills, three border colours and two corner radii of hand-picked blue. They are now three
		// weights of the same carved-stone frame, so the hierarchy is expressed in the light the
		// whole game is lit by rather than in a palette this one screen invented.
		BonelightSkin.StyleButton(button, tier switch
		{
			MenuTier.Primary => BonelightSkin.Emphasis.Primary,
			MenuTier.Secondary => BonelightSkin.Emphasis.Normal,
			_ => BonelightSkin.Emphasis.Quiet,
		});
	}

	private void PlayMenuMusic()
	{
		// Through the catalog rather than by path, via the one helper the title also uses. There
		// were three places naming a track and only one of them was the catalog, which is how a
		// removal misses one. Idempotent: the title already started it, and this does not restart.
		GetNodeOrNull<MusicPlayer>("/root/MusicPlayer")?.PlayMenuMusic();
	}


	// Achievements (once Unlocks, once Deeds; .ai/achievements-and-secrets.md section 4): every
	// achievement in the game, listed or revealed as the player plays. Every entry pays something - a
	// spell, a boon, a wizard, Arcane Energy - and its reward line says which. The page itself is
	// AchievementsPage, which the pause tab builds too; this is its frame: heading, subtitle, summary,
	// the four tabs, the list and Back.
	private HBoxContainer achievementTabs;

	private void EnsureUnlocksUi()
	{
		var content = GetNode<Control>("MarginContainer/VBoxContainer/Content");
		if (content.GetNodeOrNull<Control>("AchievementsPanel") != null)
		{
			achievementsPanel = content.GetNode<Control>("AchievementsPanel");
			return;
		}

		achievementsPanel = new Control { Name = "AchievementsPanel", Visible = false };
		achievementsPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		content.AddChild(achievementsPanel);

		var vbox = new VBoxContainer { Name = "AchievementsVBox" };
		vbox.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		vbox.AnchorBottom = 1.0f;
		vbox.OffsetLeft = 24;
		vbox.OffsetTop = 20;
		vbox.OffsetRight = -24;
		vbox.OffsetBottom = -20;
		vbox.AddThemeConstantOverride("separation", 12);
		achievementsPanel.AddChild(vbox);

		var title = new Label
		{
			Name = "AchievementsTitle",
			// The button that opens it says Achievements, and so does the pause tab: one constant.
			Text = CodexRows.UnlocksTitle,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		ResponsiveLayout.SetFont(title, ResponsiveLayout.TextRole.Display);
		vbox.AddChild(title);

		vbox.AddChild(BuildPanelSubtitle(CodexRows.UnlocksSubtitle, CodexRows.Stone.InkSoft));

		vbox.AddChild(new VBoxContainer { Name = "AchievementSummary", MouseFilter = Control.MouseFilterEnum.Ignore });

		// Above the list and outside its scroll, so a tab is never scrolled away and choosing one
		// rebuilds the rows under it without freeing the button the selection is on.
		// In the list's gutter (CodexRows.WithGutter's 2 and 18), so the strip's ends line up with the
		// rows' edges rather than running on over the scroll bar - on a phone the four tabs fill the width.
		achievementTabs = AchievementsPage.BuildTabs(AchievementsPage.MenuFilter, OnAchievementFilterChosen);
		var tabMargin = new MarginContainer { Name = "AchievementTabsMargin", MouseFilter = Control.MouseFilterEnum.Ignore };
		tabMargin.AddThemeConstantOverride("margin_left", 2);
		tabMargin.AddThemeConstantOverride("margin_right", 18);
		tabMargin.AddChild(achievementTabs);
		vbox.AddChild(tabMargin);

		vbox.AddChild(CodexRows.BuildScroll("AchievementScroll", CodexRows.BuildList("AchievementList")));

		var backButton = new Button
		{
			Name = "BackFromAchievementsButton",
			Text = "Back",
			CustomMinimumSize = new Vector2(220, 50),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
		};
		ResponsiveLayout.SetFont(backButton, ResponsiveLayout.TextRole.Title);
		vbox.AddChild(backButton);

		// The title menu's button that opens this panel is the scene's, and the scene still says
		// "Unlocks"; it says the panel's name instead, from the same constant.
		if (GetNodeOrNull<Button>("MarginContainer/VBoxContainer/Content/MainPanel/MenuButtons/ProgressionRow/UnlocksButton") is Button opener)
			opener.Text = CodexRows.UnlocksTitle;
	}

	// One centred line under a panel heading that says what the panel is for. Label role, never
	// smaller: it is read to decide whether this is the right screen (.ai/level-up-screen.md 4a).
	private static Label BuildPanelSubtitle(string text, Color ink)
	{
		var label = new Label
		{
			Name = "Subtitle",
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		ResponsiveLayout.SetFont(label, ResponsiveLayout.TextRole.Label);
		label.AddThemeColorOverride("font_color", ink);
		return label;
	}

	// Glyph, then words: the stat-energy mark tools/art/stat_icons.py draws for Arcane Energy, the
	// same one that leads "200 Arcane Energy" on an Unlocks reward line. 24 pixels, unscaled - the
	// glyphs are 12x12 drawn at x2, and any other size smears them.
	private static HBoxContainer BuildEnergyLine(string name, ResponsiveLayout.TextRole role, Color ink,
		out Label amount)
	{
		var line = new HBoxContainer
		{
			Name = name,
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		line.AddThemeConstantOverride("separation", 8);
		Texture2D glyph = StatIcons.Load("energy");
		if (glyph != null)
		{
			line.AddChild(new TextureRect
			{
				Texture = glyph,
				CustomMinimumSize = new Vector2(StatIcons.IconSize, StatIcons.IconSize),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			});
		}
		amount = new Label
		{
			Name = "Amount",
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		ResponsiveLayout.SetFont(amount, role);
		amount.AddThemeColorOverride("font_color", ink);
		line.AddChild(amount);
		return line;
	}

	// Under the shop button, inside its column, so it reads as the button's own caption
	// rather than as a readout floating on the art. Gold like the shop's purse, outlined like the
	// preset line in the opposite corner: it sits on the title art, not on a panel, and the MENU_BOX
	// pocket the art keeps dark under the buttons is what it is measured against (ui_contrast.py).
	private void EnsureMenuEnergyReadout()
	{
		// The shop's column, which holds the shop button (Powers) and this.
		Control column = GetNodeOrNull<Control>("MarginContainer/VBoxContainer/Content/MainPanel/MenuButtons/ProgressionRow/ShopColumn");
		if (column == null || column.GetNodeOrNull("MenuPurse") != null)
			return;

		HBoxContainer line = BuildEnergyLine("MenuPurse", ResponsiveLayout.TextRole.Label, CodexRows.Stone.Reward, out menuEnergyAmount);
		menuEnergyAmount.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.027f, 0.047f, 0.95f));
		menuEnergyAmount.AddThemeConstantOverride("outline_size", 5);
		column.AddChild(line);
	}

	private static string FormatEnergy(int amount) =>
		amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

	private void RefreshArcaneEnergy()
	{
		int total = 0;
		string presetName = "Default";
		var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (saveManager != null)
		{
			total = saveManager.Data.TotalCurrency;
			presetName = GlobalStatsManager.GetBalancePresetDisplayName(saveManager.Data.BalancePresetId);
		}

		// Both purses, every time: this runs after a purchase too (TryPurchaseUpgrade), which is the
		// moment the shop's header has to drop, and after a reset.
		if (menuEnergyAmount != null)
			menuEnergyAmount.Text = $"{FormatEnergy(total)} Arcane Energy";
		if (shopEnergyAmount != null)
			SetShopPurse(total);

		if (rootDetailLabel != null)
		{
			// Label, outlined: it sits straight on the title art, not on a panel, and at Micro in a
			// grey with no outline it vanished into the painted forest behind it.
			ResponsiveLayout.SetFont(rootDetailLabel, ResponsiveLayout.TextRole.Label);
			rootDetailLabel.AddThemeColorOverride("font_color", new Color(0.86f, 0.89f, 0.95f));
			rootDetailLabel.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.027f, 0.047f, 0.95f));
			rootDetailLabel.AddThemeConstantOverride("outline_size", 5);
			// "Difficulty", the word Options uses for the same choice (it said "Preset" here and "Run
			// Difficulty Curve" there until the menu pass of 2026-11-01). It had a second line, "Next run
			// gain x1.10 (Momentum streak 5)", until Eric dropped it on 2026-11-02: nobody acted on it.
			rootDetailLabel.Text = $"Difficulty: {presetName}";
		}
	}

	private void OnStartRunPressed()
	{
		// Story mode opens on its prologue the first time (Prologue, .ai/prologue.md), which then
		// goes on to the character select itself. Boss Rush is a card further in and never sees it.
		if (Prologue.IsUnseen(GetNodeOrNull<SaveManager>("/root/SaveManager")))
		{
			Prologue.Play(this, Prologue.After.Story);
			return;
		}
		GetTree().ChangeSceneToFile("res://scenes/CharacterSelection.tscn");
	}

	// The shop is one button again, Powers, over the purse it spends. From 2026-10-28 to 2026-10-30 it
	// stood beside a second, Spells & Boons; then the shop stopped selling spells
	// (.ai/achievements-and-secrets.md 2.4), the three boons left on that shelf were folded into
	// Powers as its last section (decision 5), and the button went. The row is two buttons again,
	// half each, as it was before the split: Powers and Achievements (the scene's UnlocksButton,
	// relabelled by EnsureUnlocksUi).
	private void OnArcaneUpgradesPressed()
	{
		RefreshArcaneEnergy();
		// Rebuilt on every visit rather than refreshed, so what each section's note says is true NOW
		// (the boons' changes once the last is bought - MainMenu.Shop.cs).
		BuildShop();
		ShowPanel(arcaneUpgradesPanel);
		OpenShopAtTop();
	}

	// The Evolution Codex in its collection mode (.ai/evolution-codex.md): every spell, item and recipe with the
	// save's states - owned or how to find it, the Ascensions seen, the recipes made - over the menu, on a layer
	// of its own. Closing it puts the selection back on this button.
	private void OnCodexPressed()
	{
		if (EvolutionCodex.IsOpen)
			return;
		SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		EvolutionCodex.Open(this, CodexContext.ForSave(save), onClosed: () =>
		{
			// A Set aside spent in the Codex moves the shop's Redaction row.
			RefreshUpgradeControls();
			if (IsInstanceValid(spellIndexButton) && spellIndexButton.IsVisibleInTree())
				spellIndexButton.GrabFocus();
		});
	}

	private void OnAchievementsPressed()
	{
		RefreshAchievementCards();
		ShowPanel(achievementsPanel);
		ScrollAchievementsToTop();
		// The panel opens on the tab chosen last, with the selection on it: the root's navigator
		// would land on the first tab by tree order, which is not the one showing after a visit to
		// another. A pointer user sees no ring (MenuFocusHighlight hides it under the mouse), and the
		// tabs sit outside the list's scroll, so focusing one scrolls nothing.
		AchievementsPage.TabFor(achievementTabs, AchievementsPage.MenuFilter)?.GrabFocus();
	}

	// A tab rebuilds the list under it and keeps the selection on itself (.ai/achievements-and-secrets.md
	// 4.8): the tab is outside the list, so the rebuild never frees the control that has the focus.
	private void OnAchievementFilterChosen(AchievementsPage.Filter filter)
	{
		AchievementsPage.MenuFilter = filter;
		RefreshAchievementCards();
		ScrollAchievementsToTop();
	}

	private void ScrollAchievementsToTop()
	{
		if (achievementsPanel?.FindChild("AchievementScroll", true, false) is ScrollContainer scroll)
			scroll.ScrollVertical = 0;
	}

	// The two lists choose one column or two as they are built, so a canvas that changes shape
	// while one is open (a resized PC window) rebuilds it rather than leaving it in the old count.
	private int codexColumnsBuilt;

	private void OnViewportSizeChanged()
	{
		// Options' two columns of sections fold to one as the codex lists do (MainMenu.Options.cs).
		if (optionsColumns != null && optionsColumns.Columns != CodexRows.ColumnsFor(this))
		{
			optionsColumns.Columns = CodexRows.ColumnsFor(this);
			ChainOptionsFocus();
		}
		// The shop keeps its own count: it is rebuilt only when ITS columns are wrong, or every resize
		// event of a window being dragged would rebuild it and throw the selection back to the top.
		if (arcaneUpgradesPanel.Visible && CodexRows.ColumnsFor(this) != shopColumnsBuilt)
		{
			BuildShop();
			OpenShopAtTop();
			ApplyCodexPanelWidth();
		}
		if (CodexRows.ColumnsFor(this) == codexColumnsBuilt)
			return;
		if (achievementsPanel.Visible)
			RefreshAchievementCards();
		ApplyCodexPanelWidth();
	}

	// On a PC the two lists are a centred column rather than the full canvas: at an ultrawide's
	// 1700 units, two columns of rows would each run 800 wide and a description would be one line
	// a reader loses their place in. The phone keeps the full width.
	private const float CodexColumnMaxWidth = 1200f;

	private void ApplyCodexPanelWidth()
	{
		// 80: the menu's MarginContainer, 40 a side. Inset only what is over the cap, so a 960-wide
		// canvas (a 4:3 monitor) keeps every unit it has.
		float available = ResponsiveLayout.ViewportSize(this).X - 80f;
		// On a phone the panel instead reaches 24 units INTO that margin: width is the scarce axis
		// there, and the safe area (SafeArea.MinSide, 28) is applied outside the margin, so the
		// frame still clears the rounded corners with 16 to spare.
		float inset = ResponsiveLayout.IsNarrow(this) ? -24f : Mathf.Max(0f, (available - CodexColumnMaxWidth) / 2f);
		// The shop is the same kind of list and takes the same column (MainMenu.Shop.cs).
		foreach (Control panel in new[] { achievementsPanel, arcaneUpgradesPanel })
		{
			panel.OffsetLeft = inset;
			panel.OffsetRight = -inset;
		}
	}

	private void OnOptionsPressed()
	{
		DisarmReset();
		ShowPanel(optionsPanel);
		FocusFirstOption();
	}

	// The title artwork is 720x1680 drawn at a whole x2 and centred on the canvas, and the canvas
	// is 720 wide with a height that varies by device (project stretch is keep_width). So the art's
	// top edge is NOT the top of the screen, and anchoring menu furniture to the screen edge makes
	// it drift off the composition on any phone that is not exactly 16:9.
	//
	// Everything here is therefore anchored to the CENTRE and offset in artwork rows: a row R of the
	// original 1280-tall composition sits at (R - 640) from centre, on every device, always. The file
	// is 1680 tall now, but that composition is still centred in it, so these rows did not move.
	//
	// The two bands used, read off the composition in .ai/title-screen.md:
	//   rows  340-550  dark wood under the wordmark and above the staff orb -> the menu buttons,
	//                  with the Arcane Energy readout as the shop button's caption
	//   rows 1168-1232 foreground grass -> the utility row: Options, Spell Index, Report a bug
	// The figure, the ward and the orb are never covered. That is the whole point of the pass.
	private void LayoutAroundTitleArt()
	{
		// A PC gets the wide title art and its own positions (PinToWideArt); a phone keeps
		// exactly the portrait layout below. See .ai/title-screen.md, "The landscape set".
		bool landscape = !DisplayProfile.IsPortrait;

		// TitleScreen draws the art now, so the menu's own copy is dead weight - and it is the OLD
		// pre-Bonelight title image, which would fight the new one if both drew.
		var background = GetNodeOrNull<Control>("Background");
		if (background != null)
			background.Visible = false;

		// Was a permanent 28% wash over everything. It is a scrim now: clear on the root menu so the
		// artwork reads, and only drawn when a dense panel needs a legible ground to sit on.
		scrim = GetNodeOrNull<ColorRect>("ColorRect");
		if (scrim != null)
			scrim.Color = new Color(0.02f, 0.03f, 0.05f, 0.0f);

		var buttons = GetNodeOrNull<Control>("MarginContainer/VBoxContainer/Content/MainPanel/MenuButtons");
		if (buttons != null)
		{
			buttons.Reparent(this);
			if (landscape)
				PinToWideArt(buttons, 60f, 134f, 290f, 240f);
			else
				PinToArtRows(buttons, 340f, 550f, halfWidth: 230f);
			rootMenuButtons = buttons;
		}

		// The Arcane Energy readout no longer lives up here. It was a lone label in the canopy (top
		// right on a PC, above the wordmark on a phone), on a translucent TopBar plate that ShowPanel
		// kept switching back on over the art as an empty strip; it is now a caption under the
		// Arcane Upgrades button (EnsureMenuEnergyReadout) and the TopBar is gone from the scene.
		// The art's ARCANE_BOX pocket (tools/art/splash_wide.py) is left as drawn: it is soft and
		// only as dark as it needs, and redrawing the title set for it is not worth the churn.

		// The preset and the next-run multiplier are reference numbers, in the bottom corner
		// opposite Options. Placed by LayoutUtilityRow, which owns that foot of the screen.
		rootDetailLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		AddChild(rootDetailLabel);
	}

	// The landscape title art (tools/art/splash_wide.py) is a 640x360 core drawn at x2 and
	// centred, and a PC canvas is always 720 tall, so its pixel (x, y) is always at
	// ((x - 320) * 2, (y - 180) * 2) from the centre of the screen. The boxes passed in here are
	// the MENU_BOX, ARCANE_BOX, OPTIONS_BOX and DETAIL_BOX rectangles in splash_wide.py, in core
	// pixels - the art keeps each of them dark, so the numbers must move together.
	private static void PinToWideArt(Control control, float x0, float y0, float x1, float y1)
	{
		const float CoreHalfWidth = 320f;
		const float CoreHalfHeight = 180f;

		control.AnchorLeft = 0.5f;
		control.AnchorRight = 0.5f;
		control.AnchorTop = 0.5f;
		control.AnchorBottom = 0.5f;
		control.OffsetLeft = (x0 - CoreHalfWidth) * 2f;
		control.OffsetRight = (x1 - CoreHalfWidth) * 2f;
		control.OffsetTop = (y0 - CoreHalfHeight) * 2f;
		control.OffsetBottom = (y1 - CoreHalfHeight) * 2f;
	}

	// Anchors a control to the canvas centre and positions it by artwork row, so it lands on the
	// same part of the picture whatever the viewport height is.
	private static void PinToArtRows(Control control, float topRow, float bottomRow, float halfWidth)
	{
		const float ArtHalfHeight = 640f;

		control.AnchorLeft = 0.5f;
		control.AnchorRight = 0.5f;
		control.AnchorTop = 0.5f;
		control.AnchorBottom = 0.5f;
		control.OffsetLeft = -halfWidth;
		control.OffsetRight = halfWidth;
		control.OffsetTop = topRow - ArtHalfHeight;
		control.OffsetBottom = bottomRow - ArtHalfHeight;
	}

	// The foot of the screen: Options in its corner, then the Evolution Codex - the two things on this
	// screen that are not about the next run - and the difficulty readout. Report a bug stood third in
	// this row until 2026-11-02; it is in Options now (MainMenu.Options.cs).
	//
	// On a PC they are one row on the dark grass under the menu column, left to right from the
	// OPTIONS_BOX corner, and the preset keeps DETAIL_BOX in the opposite corner. The two quiet
	// buttons end at core x 318, which keeps them clear of the ward circle's left rim and its
	// flame at about x 320 and rows 245-285.
	//
	// On a phone the two share one row across the grass (rows 1168-1232), each 316 wide and
	// 64 tall - comfortably over the 48-unit touch floor. That row used to hold Options on the left
	// and the preset readout on the right, with Report a bug squeezed under Options; the readout now
	// goes under the row where a tall phone (the Pixel 9: rows to about 1410) has open ground, or
	// above it on a 16:9 phone, whose canvas ends at row 1280, between the row and the circle's rim.
	private void LayoutUtilityRow()
	{
		foreach (Button button in new[] { optionsButton, spellIndexButton })
		{
			if (button == null)
				continue;
			button.AnchorLeft = 0.5f;
			button.AnchorRight = 0.5f;
			button.AnchorTop = 0.5f;
			button.AnchorBottom = 0.5f;
		}

		if (!DisplayProfile.IsPortrait)
		{
			PinToWideArt(optionsButton, 12f, 292f, 126f, 324f);
			PinToWideArt(spellIndexButton, 134f, 296f, 236f, 320f);
			if (rootDetailLabel != null)
				PinToWideArt(rootDetailLabel, 470f, 312f, 632f, 352f);
			return;
		}

		const float RowTop = 528f;     // composition row 1168
		const float RowBottom = 592f;  // composition row 1232
		const float Half = 320f;
		const float Gap = 8f;
		Button[] row = { optionsButton, spellIndexButton };
		float width = (Half * 2f - Gap * (row.Length - 1)) / row.Length;
		for (int i = 0; i < row.Length; i++)
		{
			if (row[i] == null)
				continue;
			float left = -Half + i * (width + Gap);
			row[i].OffsetLeft = left;
			row[i].OffsetRight = left + width;
			row[i].OffsetTop = RowTop;
			row[i].OffsetBottom = RowBottom;
		}

		if (rootDetailLabel == null)
			return;
		// Above the row there is one line of grass between the row and the bottom flame of the
		// ward (rows 1132-1160); two lines there ran over the flame, so it says it in one.
		float halfCanvas = ResponsiveLayout.ViewportSize(this).Y / 2f;
		float floor = halfCanvas - SafeArea.Insets(GetViewport()).Bottom - 8f;
		float below = RowBottom + 12f;
		const float TwoLines = 60f;
		const float OneLine = 28f;
		detailOnOneLine = below + TwoLines > floor;
		float detailHeight = detailOnOneLine ? OneLine : TwoLines;
		float top = detailOnOneLine ? RowTop - 8f - OneLine : below;
		rootDetailLabel.AnchorLeft = 0.5f;
		rootDetailLabel.AnchorRight = 0.5f;
		rootDetailLabel.AnchorTop = 0.5f;
		rootDetailLabel.AnchorBottom = 0.5f;
		rootDetailLabel.OffsetLeft = -Half;
		rootDetailLabel.OffsetRight = Half;
		rootDetailLabel.OffsetTop = top;
		rootDetailLabel.OffsetBottom = top + detailHeight;
		rootDetailLabel.HorizontalAlignment = HorizontalAlignment.Center;
	}

	private void OnReportBugPressed()
	{
		// Beside the menu, not inside it: a screen under this menu's root would be inside its
		// navigator too, and two navigators would both step the same selection.
		Node host = GetParent() ?? this;
		ReportBugScreen.Open(host, null, () =>
		{
			if (IsInstanceValid(reportBugButton) && reportBugButton.IsVisibleInTree())
				reportBugButton.GrabFocus();
		});
	}

	private void ShowMainPanel()
	{
		ShowPanel(mainPanel);
	}

	// B, Escape, or Android's back gesture (all ui_cancel) back out of an open panel, as its Back
	// button does. There was no way out of a panel on a pad but to walk the selection down to Back.
	// Unhandled, and only while the selection is inside this menu, so a screen over it - Report a
	// bug, an element's card - hears its own cancel first and keeps it.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel") || IsAtRoot || !IsVisibleInTree())
			return;
		Control owner = GetViewport()?.GuiGetFocusOwner();
		if (owner != null && !IsAncestorOf(owner))
			return;
		GetViewport().SetInputAsHandled();
		SfxPlayer.Global(SfxCatalog.UiBack);
		ShowMainPanel();
	}

	// Two columns of settings since the menu pass of 2026-11-01 (MainMenu.Options.cs); it was one, 840 wide.
	private const float OptionsColumnHalfWidth = 560f;

	private void ShowPanel(Control panelToShow)
	{
		mainPanel.Visible = panelToShow == mainPanel;

		// The Options button is pinned to the corner of the SCREEN rather than parented into the
		// menu stack, so nothing hides it when a panel opens - it was drawing on top of the panel
		// it had just opened, over that panel's own Back button. A corner control that belongs to
		// the main screen leaves with the main screen.
		bool atRoot = panelToShow == mainPanel;
		// Report a bug is not one of these any more: it lives inside Options, and hiding it here hid it there.
		foreach (Control utility in new Control[] { optionsButton, spellIndexButton })
		{
			if (utility != null)
				utility.Visible = atRoot;
		}

		// The buttons (and the energy readout under the shop button, which is inside them) were
		// lifted out of mainPanel by LayoutAroundTitleArt, so hiding mainPanel no longer hides them.
		// They follow it by hand.
		if (rootMenuButtons != null)
			rootMenuButtons.Visible = atRoot;
		if (rootDetailLabel != null)
			rootDetailLabel.Visible = atRoot;

		// Clear on the root menu so the artwork is the screen; drawn behind a panel because a wall
		// of upgrade rows over a forest at night is unreadable.
		if (scrim != null)
			scrim.Color = new Color(0.02f, 0.03f, 0.05f, atRoot ? 0.0f : 0.88f);
		// The stack above Content used to be a TopBar plate and a 120-tall Spacer. The TopBar is gone:
		// once its energy label moved onto the art it was an empty translucent strip, and this line
		// was what kept switching it back on across the top of the root menu. The Spacer still took
		// 144 units off the top of every panel, so on a 720-tall PC canvas each one was a short strip
		// with most of its rows behind a scrollbar; a panel gets the full height instead. On the root
		// menu nothing is laid out by that stack any more - everything there is pinned to the art.
		if (GetNodeOrNull<Control>("MarginContainer/VBoxContainer/Spacer") is Control spacer)
			spacer.Visible = atRoot;

		// Options is two columns of sliders and switches; on a PC's landscape canvas a full-width panel
		// stretched every slider across 1180 units. A centred block instead. The phone keeps the width.
		if (!ResponsiveLayout.IsNarrow(this))
		{
			optionsPanel.AnchorLeft = 0.5f;
			optionsPanel.AnchorRight = 0.5f;
			// Never past the menu's own margin (40 a side), which a 4:3 monitor's 960 would be.
			float half = Mathf.Min(OptionsColumnHalfWidth, (ResponsiveLayout.ViewportSize(this).X - 80f) / 2f);
			optionsPanel.OffsetLeft = -half;
			optionsPanel.OffsetRight = half;
		}
		else
		{
			// On a phone it reaches into the menu's margin as the codex panels do (ApplyCodexPanelWidth):
			// width is the scarce axis there, and a slider row wants every unit of it.
			optionsPanel.OffsetLeft = -24f;
			optionsPanel.OffsetRight = 24f;
		}
		ApplyCodexPanelWidth();

		achievementsPanel.Visible = panelToShow == achievementsPanel;
		arcaneUpgradesPanel.Visible = panelToShow == arcaneUpgradesPanel;
		optionsPanel.Visible = panelToShow == optionsPanel;
	}

	private void BuildUpgradeDefinitions()
	{
		upgradeDefinitions.Clear();
		// The book's 5th and 6th pages (.ai/slow-start.md 2.1, Eric's Q3): a new save holds three and Survivor
		// pays the 4th. SpellBook holds the numbers; a book kept from before the slow start shows complete.
		RegisterUpgrade(SpellBook.ShopUpgradeId, "Bound Pages", "+1 spell slot in every run",
			SpellBook.ShopBaseCost, SpellBook.ShopCostPerLevel, SpellBook.ShopLevels);
		RegisterUpgrade("damage", "Runes of Ruin", "+8% spell damage", 30, 20, 8);
		// Re-priced with what a level pays (.ai/difficulty-curve.md, section 5; SaveData.RepricedPowers
		// refunds the difference to levels bought before).
		RegisterUpgrade("recovery", "Emberheart", $"+{Player.EmberheartRegenPerLevel * 60f:0} HP regen a minute", 15, 10, 8);
		RegisterUpgrade("cooldowns", "Chronoweave", "-5% cooldown time", 35, 25, 8);
		RegisterUpgrade("area", "Aether Bloom", "+8% area and range", 30, 20, 7);
		RegisterUpgrade("attack_speed", "Tempest Rhythm", "+6% attack speed", 35, 25, 8);
		RegisterUpgrade("duration", "Eternal Wick", "+10% effect duration", 25, 20, 6);
		RegisterUpgrade("amount", "Twinstar Sigil", "+1 projectile for every spell that fires them", 60, 40, 4);
		RegisterUpgrade("movespeed", "Fleetfoot Glyph", "+5% movement speed", 20, 15, 8);
		RegisterUpgrade("magnet", "Void Lure", "+20 pickup range", 25, 20, 7);
		RegisterUpgrade("growth", "Sage Spiral", "+10% XP gain", 30, 20, 6);
		RegisterUpgrade("greed", "Avarice Seal", "+10% Arcane Energy gain", 35, 25, 8);
		RegisterUpgrade("extra_lives", "Phoenix Oath", "+1 revive in every run", 120, 80, 3);
		// Rerolls are a pool for the run since 2026-10-31 (.ai/difficulty-curve.md, section 7): +2 a level,
		// ten at the cap, priced down from 75 + 50 with what a level buys (SaveData.RepricedPowers refunds
		// the difference).
		RegisterUpgrade("rerolls", "Fate Fracture", $"+{Player.RerollsPerFateFractureLevel} rerolls in every run", 40, 25, 5);
		// The other run-scoped level-up charge. Priced above rerolls because a ban strikes a spell for the
		// rest of the run where a reroll only re-deals one hand. Hoarded Insight (Save) and
		// Augury sold beside it until 2026-10-28 ("remove save and augury mechanics"); what was bought
		// of them is refunded by SaveData.MigrateRetiredLevelUpCharges.
		RegisterUpgrade("bans", "Proscription", "+1 ban per run: strike a spell from that run's level-ups", 90, 70, 3);
		RegisterUpgrade("vitality", "Vitality", "+5 max HP", 25, 20, 10);
		RegisterUpgrade("luck", "Fortune Thread", "+1 Luck", 45, 30, 20);
		RegisterUpgrade("crit_chance", "Keen Focus", "+3% crit chance", 55, 35, 20);
		RegisterUpgrade(GlobalStatsManager.CurationUpgradeId, "Redaction",
			"One more spell may be set aside from the level-up pool", 80, 40, 1);
		upgradeDefinitions[GlobalStatsManager.CurationUpgradeId].HasPoolScaledMaxLevel = true;

		// The boons the catalog prices, driven by UnlockCatalog rather than a hand-kept list. No screen
		// sold them until 2026-10-26, so Quicksilver Bead, Deepwater Pearl and Saltbound Chain -
		// Purchase entries since schema 9 - were cards no new save could ever be dealt.
		// No spells: the shop sold nine until schema 19, and every one is an achievement's now
		// (.ai/achievements-and-secrets.md 2.4). A copy anybody bought stays in UnlockedSpellIds.
		foreach (UnlockDefinition definition in UnlockCatalog.PurchasableBoons)
			RegisterBoonUnlock(definition.Id, definition.PurchaseCost);
	}

	private static void ClearChildren(Node node)
	{
		if (node == null)
			return;
		foreach (Node child in node.GetChildren())
		{
			// Removed as well as freed, so the rebuilt list lays out this frame without the old rows
			// still holding their space until the end of it.
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

	// The page under the tabs (AchievementsPage, which the pause tab builds too), and the summary over
	// them, which counts the whole page whichever tab is showing. The Secrets section that ended this
	// list is the Secrets tab now (.ai/achievements-and-secrets.md 4.10).
	private void BuildAchievementCards()
	{
		ClearChildren(achievementList);
		ClearChildren(achievementSummary);
		codexColumnsBuilt = CodexRows.ColumnsFor(this);
		bool narrow = ResponsiveLayout.IsNarrow(this);
		AchievementsPage.LayoutTabs(achievementTabs, narrow);
		AchievementsPage.MarkTab(achievementTabs, AchievementsPage.MenuFilter);

		SaveData data = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		AchievementsPage.Result page = AchievementsPage.Build(achievementList, AchievementContext.ForSave(data),
			AchievementsPage.PageMode.Menu, AchievementsPage.MenuFilter, string.Empty);
		AchievementsPage.LinkTabsToList(achievementTabs, achievementList, AchievementsPage.MenuFilter);
		// The bar under the count only where there is height for it: on a 720-tall PC canvas the heading,
		// the subtitle, the count and the tabs already stand over the list, and every unit they take is a
		// row the list cannot show. The phone's portrait canvas has the height to spare.
		achievementSummary.AddChild(CodexRows.BuildSummary(AchievementsPage.SummaryLine(page, narrow), CodexRows.Stone,
			narrow && page.Total > 0 ? page.Earned / (float)page.Total : (float?)null));
	}

	private void RefreshAchievementCards() => BuildAchievementCards();

	private void RegisterUpgrade(string id, string displayName, string effectText, int baseCost, int costPerLevel, int maxLevel)
	{
		upgradeDefinitions[id] = new UpgradeDefinition
		{
			Id = id,
			DisplayName = displayName,
			EffectText = effectText,
			BaseCost = baseCost,
			CostPerLevel = costPerLevel,
			MaxLevel = Math.Max(1, maxLevel)
		};
	}

	private void RegisterBoonUnlock(string boonId, int cost)
	{
		BoonDefinition boon = BoonCatalog.GetById(boonId);
		upgradeDefinitions[$"boon:{boonId}"] = new UpgradeDefinition
		{
			Id = $"boon:{boonId}",
			DisplayName = boon?.Name ?? boonId,
			EffectText = "Adds this boon to future level-up offers.",
			BaseCost = cost,
			CostPerLevel = 0,
			MaxLevel = 1,
			BoonId = boonId
		};
	}

	private static int GetShopItemLevel(SaveManager saveManager, UpgradeDefinition def)
	{
		if (def.IsBoon)
			return GlobalStatsManager.IsBoonUnlocked(saveManager.Data, def.BoonId) ? 1 : 0;
		// A book kept from before the slow start (SaveData.FullBookKept) reads as complete: nothing to buy,
		// nothing charged, nothing refunded.
		if (def.Id == SpellBook.ShopUpgradeId)
			return SpellBook.ShopLevelShown(saveManager.Data);

		return GlobalStatsManager.GetUpgradeLevel(saveManager.Data, def.Id);
	}

	private static int GetShopItemCost(UpgradeDefinition def, int level)
	{
		return def.BaseCost + (level * def.CostPerLevel);
	}

	// Every other upgrade's ceiling is a constant on the definition. Curation's rises with the size
	// of the book, which is what keeps it from being a way to trivialise a small early pool.
	private static int GetShopItemMaxLevel(SaveManager saveManager, UpgradeDefinition def)
	{
		if (!def.HasPoolScaledMaxLevel)
			return def.MaxLevel;

		return GlobalStatsManager.GetCurationSlotCapacity(saveManager?.Data);
	}
}

