using Godot;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

public partial class MainMenu
{
	private CheckButton fullscreenChoice;
	private OptionButton windowSizeChoice;
	private OptionButton monitorChoice;
	private Label nativeSizeLine;
	private List<Vector2I> windowSizes = new();

	private List<Control> BuildScreenOptions(CodexRows.Palette ink)
	{
		fullscreenChoice = SettingsRows.BuildSwitch("Fullscreen", DisplayProfile.IsFullscreen, ink, enabled =>
		{
			ConfirmDisplayChange(fullscreenChoice, () => DisplayProfile.StoreFullscreen(this, enabled));
		}, "FullscreenToggle");
		var rows = new List<Control> { fullscreenChoice };

		var sizes = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		sizes.AddThemeConstantOverride("separation", 8);
		sizes.AddChild(CodexRows.MakeText("Window size", ResponsiveLayout.TextRole.Label, ink.Ink));
		windowSizeChoice = BuildDisplayDropdown("WindowResolution", ink);
		windowSizeChoice.ItemSelected += index =>
		{
			Vector2I size = windowSizes[(int)index];
			// Picking what is already set changes nothing, so it asks nothing.
			if (size == SelectedWindowSize())
				return;
			ConfirmDisplayChange(windowSizeChoice, () => DisplayProfile.StoreWindowSize(this, size));
		};
		sizes.AddChild(windowSizeChoice);
		nativeSizeLine = CodexRows.MakeText("", ResponsiveLayout.TextRole.Label, ink.InkSoft);
		sizes.AddChild(nativeSizeLine);
		rows.Add(sizes);

		// The monitor, only where there is a choice to make (item 20). "Automatic" is -1: wherever
		// the OS put the window, which is what every save had before the row existed.
		if (DisplayProfile.ScreenCount > 1)
		{
			var monitors = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
			monitors.AddThemeConstantOverride("separation", 8);
			monitors.AddChild(CodexRows.MakeText("Monitor", ResponsiveLayout.TextRole.Label, ink.Ink));
			monitorChoice = BuildDisplayDropdown("MonitorChoice", ink);
			monitorChoice.ItemSelected += index =>
			{
				int monitor = (int)index - 1;
				SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
				if ((save?.MonitorIndex ?? -1) == monitor)
					return;
				ConfirmDisplayChange(monitorChoice, () => DisplayProfile.StoreMonitor(this, monitor));
			};
			monitors.AddChild(monitorChoice);
			rows.Add(monitors);
		}

		RefreshScreenOptions();
		return rows;
	}

	// The window-size dropdown's construction, shared with the monitor's: the stone button and a
	// popup themed to match (an OptionButton's popup takes none of the button's own overrides).
	private static OptionButton BuildDisplayDropdown(string name, CodexRows.Palette ink)
	{
		var choice = new OptionButton { Name = name, FocusMode = Control.FocusModeEnum.All,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0f, 56f) };
		BonelightSkin.StyleButton(choice, BonelightSkin.Emphasis.Quiet);
		ResponsiveLayout.SetFont(choice, ResponsiveLayout.TextRole.Label);
		var popupTheme = new Theme { DefaultFont = choice.GetThemeFont("font"),
			DefaultFontSize = choice.GetThemeFontSize("font_size") };
		popupTheme.SetColor("font_color", "PopupMenu", ink.Ink);
		popupTheme.SetStylebox("panel", "PopupMenu", new StyleBoxFlat { BgColor = CodexRows.Stone.Face });
		choice.GetPopup().Theme = popupTheme;
		return choice;
	}

	/// <summary>The saved window size as the dropdown shows it: fitted to the usable desktop, or
	/// zero for Automatic. The same fit RefreshScreenOptions selects by.</summary>
	private Vector2I SelectedWindowSize()
	{
		SaveData save = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
		Vector2I usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen()).Size;
		return save?.WindowWidth > 0 && save.WindowHeight > 0
			? DisplayProfile.FitWindow(new Vector2I(save.WindowWidth, save.WindowHeight), usable) : Vector2I.Zero;
	}

	// Applies a display change and opens the timed Keep / Revert step over the menu
	// (DisplayConfirm): a mode, size or monitor that leaves the screen unreadable undoes itself
	// when the clock runs out. Beside the menu, like Report a bug, so the two navigators never
	// step the same selection; the selection comes back to the control that asked.
	private void ConfirmDisplayChange(Control refocus, System.Action change)
	{
		DisplayProfile.DisplayState before = DisplayProfile.Snapshot(this);
		change();
		RefreshScreenOptions();
		DisplayConfirm.Open(GetParent() ?? this, revert: () =>
		{
			DisplayProfile.Restore(this, before);
			RefreshScreenOptions();
		}, closed: () =>
		{
			if (IsInstanceValid(refocus) && refocus.IsVisibleInTree())
				refocus.GrabFocus();
		});
	}

	private void RefreshScreenOptions()
	{
		if (windowSizeChoice == null || !DisplayProfile.DesktopControlsAvailable) return;
		Vector2I usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen()).Size;
		var choices = DisplayProfile.WindowSizeChoices(usable);
		choices.Insert(0, Vector2I.Zero);
		Vector2I selected = SelectedWindowSize();
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
		RefreshMonitorChoice();
		ChainOptionsFocus();
	}

	private void RefreshMonitorChoice()
	{
		if (monitorChoice == null) return;
		int count = DisplayProfile.ScreenCount;
		if (monitorChoice.ItemCount != count + 1)
		{
			monitorChoice.Clear();
			monitorChoice.AddItem("Automatic (current monitor)");
			for (int i = 0; i < count; i++)
				monitorChoice.AddItem(MonitorLabel(i));
		}
		int saved = GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data.MonitorIndex ?? -1;
		monitorChoice.Select(saved >= 0 && saved < count ? saved + 1 : 0);
	}

	// "Monitor 2 (primary), 1920 × 1080". The size is left off a monitor the display server does
	// not actually have (the --fake-monitors seam photographs this row on a one-monitor machine).
	private static string MonitorLabel(int index)
	{
		string name = $"Monitor {index + 1}";
		if (index == DisplayServer.GetPrimaryScreen()) name += " (primary)";
		if (index < DisplayServer.GetScreenCount())
		{
			Vector2I size = DisplayServer.ScreenGetSize(index);
			name += $", {size.X} × {size.Y}";
		}
		return name;
	}
}
