using Godot;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

public partial class MainMenu
{
	private CheckButton fullscreenChoice;
	private OptionButton windowSizeChoice;
	private Label nativeSizeLine;
	private List<Vector2I> windowSizes = new();

	private List<Control> BuildScreenOptions(CodexRows.Palette ink)
	{
		fullscreenChoice = SettingsRows.BuildSwitch("Fullscreen", DisplayProfile.IsFullscreen, ink, enabled =>
		{
			DisplayProfile.StoreFullscreen(this, enabled);
			RefreshScreenOptions();
		}, "FullscreenToggle");
		var sizes = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		sizes.AddThemeConstantOverride("separation", 8);
		sizes.AddChild(CodexRows.MakeText("Window size", ResponsiveLayout.TextRole.Label, ink.Ink));
		windowSizeChoice = new OptionButton { Name = "WindowResolution", FocusMode = FocusModeEnum.All,
			SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0f, 56f) };
		BonelightSkin.StyleButton(windowSizeChoice, BonelightSkin.Emphasis.Quiet);
		ResponsiveLayout.SetFont(windowSizeChoice, ResponsiveLayout.TextRole.Label);
		var popupTheme = new Theme { DefaultFont = windowSizeChoice.GetThemeFont("font"),
			DefaultFontSize = windowSizeChoice.GetThemeFontSize("font_size") };
		popupTheme.SetColor("font_color", "PopupMenu", ink.Ink);
		popupTheme.SetStylebox("panel", "PopupMenu", new StyleBoxFlat { BgColor = CodexRows.Stone.Face });
		windowSizeChoice.GetPopup().Theme = popupTheme;
		windowSizeChoice.ItemSelected += index =>
		{
			DisplayProfile.StoreWindowSize(this, windowSizes[(int)index]);
			RefreshScreenOptions();
		};
		sizes.AddChild(windowSizeChoice);
		nativeSizeLine = CodexRows.MakeText("", ResponsiveLayout.TextRole.Label, ink.InkSoft);
		sizes.AddChild(nativeSizeLine);
		RefreshScreenOptions();
		return new List<Control> { fullscreenChoice, sizes };
	}

	private void RefreshScreenOptions()
	{
		if (windowSizeChoice == null || !DisplayProfile.DesktopControlsAvailable) return;
		SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		Vector2I usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen()).Size;
		var choices = DisplayProfile.WindowSizeChoices(usable);
		choices.Insert(0, Vector2I.Zero);
		Vector2I selected = save?.WindowWidth > 0 && save.WindowHeight > 0
			? DisplayProfile.FitWindow(new Vector2I(save.WindowWidth, save.WindowHeight), usable) : Vector2I.Zero;
		if (selected != Vector2I.Zero && !choices.Contains(selected)) choices.Add(selected);
		if (!windowSizes.SequenceEqual(choices))
		{
			windowSizes = choices;
			windowSizeChoice.Clear();
			foreach (Vector2I size in windowSizes)
				windowSizeChoice.AddItem(size == Vector2I.Zero ? "Automatic (fit to screen)" : $"{size.X} × {size.Y}");
		}
		windowSizeChoice.Select(windowSizes.IndexOf(selected));
		bool fullscreen = DisplayProfile.IsFullscreen;
		windowSizeChoice.Disabled = fullscreen;
		windowSizeChoice.TooltipText = fullscreen ? "Turn off Fullscreen to choose a window size." : "";
		fullscreenChoice.SetPressedNoSignal(fullscreen);
		Vector2I native = DisplayProfile.NativeResolution;
		nativeSizeLine.Text = $"Fullscreen uses {native.X} × {native.Y} (native).";
		ChainOptionsFocus();
	}
}
