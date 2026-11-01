using Godot;
using System;
using WizardSurvivors.scripts;

/// <summary>
/// The journal over the world map (.ai/quests-and-notes.md): the quest log and the chapter notes, the same
/// page the pause screen's Journal tab builds (JournalPage), on a stone panel with a Back button.
/// </summary>
/// <remarks>
/// A CanvasLayer of its own over the map, like the bug report over the pause screen: its own navigator
/// takes the selection as it opens (MenuNavigator.Attach), and closing it frees its buttons, so the map's
/// navigator picks the selection up again - the caller puts it back on the Journal button. While it is
/// open the map hears nothing (WorldMap checks <see cref="IsOpen"/>): left and right walk the tabs here,
/// not the road under it, and Back closes this, not the map.
/// </remarks>
public partial class JournalScreen : CanvasLayer
{
	private const int ScreenLayer = 60;
	private const float WideHalfWidth = 470f;

	private Action? closed;
	private VBoxContainer host = null!;
	private ScrollContainer scroll = null!;
	private SaveData? save;

	/// <summary>True while a journal is open over the map.</summary>
	public static bool IsOpen { get; private set; }

	public static JournalScreen Open(Node parent, Action onClosed)
	{
		var screen = new JournalScreen { Name = "JournalScreen", closed = onClosed };
		parent.AddChild(screen);
		return screen;
	}

	public override void _Ready()
	{
		IsOpen = true;
		Layer = ScreenLayer;
		ProcessMode = ProcessModeEnum.Always;
		save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		bool narrow = ResponsiveLayout.IsNarrow(this);

		var root = new Control { Name = "JournalRoot", MouseFilter = Control.MouseFilterEnum.Stop };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		var dim = new ColorRect { Color = new Color(0.025f, 0.03f, 0.045f, 0.9f), MouseFilter = Control.MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(dim);

		var panel = new PanelContainer { Name = "JournalPanel" };
		panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		panel.OffsetLeft = 12f;
		panel.OffsetRight = -12f;
		panel.OffsetTop = 12f;
		panel.OffsetBottom = -12f;
		if (!narrow)
		{
			panel.AnchorLeft = 0.5f;
			panel.AnchorRight = 0.5f;
			panel.OffsetLeft = -WideHalfWidth;
			panel.OffsetRight = WideHalfWidth;
		}
		panel.AddThemeStyleboxOverride("panel", BonelightSkin.PanelStyle(BonelightSkin.Register.Stone));
		root.AddChild(panel);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 12);
		panel.AddChild(column);

		// The heading band of every screen in the stone register (the pause screen's), with Back on it.
		var band = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		var style = new StyleBoxFlat { BgColor = new Color(0.043f, 0.059f, 0.094f), BorderColor = new Color(0.541f, 0.416f, 0.118f) };
		style.BorderWidthBottom = 2;
		style.SetCornerRadiusAll(2);
		style.ContentMarginLeft = 16;
		style.ContentMarginRight = 8;
		style.ContentMarginTop = 4;
		style.ContentMarginBottom = 6;
		band.AddThemeStyleboxOverride("panel", style);
		column.AddChild(band);
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		band.AddChild(row);
		var title = new Label
		{
			Text = "Journal",
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0f, ResponsiveLayout.MinTouchTarget),
		};
		ResponsiveLayout.SetFont(title, ResponsiveLayout.TextRole.Display);
		title.AddThemeColorOverride("font_color", CodexRows.Stone.Heading);
		row.AddChild(title);
		var back = new Button { Name = "JournalBack", Text = "Back", FocusMode = Control.FocusModeEnum.All };
		ResponsiveLayout.SetFont(back, ResponsiveLayout.TextRole.Body);
		BonelightSkin.StyleButton(back, BonelightSkin.Emphasis.Normal);
		ResponsiveLayout.EnsureTouchTarget(back, 120f);
		back.Pressed += Close;
		row.AddChild(back);

		host = CodexRows.BuildList("JournalHost");
		scroll = CodexRows.BuildScroll("JournalScroll", host);
		column.AddChild(scroll);
		Build();

		MenuBackListener.Attach(this, Close, "ui_cancel");
		SafeArea.Apply(root);
		MenuNavigator.Attach(this, JournalPage.TabFor(host, JournalPage.Current));
		SfxPlayer.Global(SfxCatalog.UiOpen);
	}

	public override void _ExitTree() => IsOpen = false;

	public override void _Notification(int what)
	{
		// Android's back button closes the journal, not the map under it.
		if (what == NotificationWMGoBackRequest)
			Close();
	}

	private void Build()
	{
		foreach (Node child in host.GetChildren())
		{
			host.RemoveChild(child);
			child.QueueFree();
		}
		JournalPage.Build(host, save, AchievementContext.ForSave(save ?? new SaveData()), string.Empty, Build, null);
	}

	private void Close()
	{
		if (IsQueuedForDeletion())
			return;
		IsOpen = false;
		SfxPlayer.Global(SfxCatalog.UiClose);
		QueueFree();
		closed?.Invoke();
	}
}
