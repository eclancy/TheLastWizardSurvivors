using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

/// <summary>
/// Picks the game's canvas shape at startup: landscape on a PC, portrait on a phone.
/// </summary>
/// <remarks>
/// The game was laid out for one shape only - a 720x1280 portrait canvas - and a PC ran it in a tall
/// narrow window. A PC now gets a 1280x720 base canvas that expands to its display's shape without
/// letterboxing. A phone keeps 720x1280 (keep_width). Fullscreen is native; saved sizes apply to windows.
///
/// project.godot carries the same split as feature-tag overrides (".mobile") so the very first frame
/// on a phone is already portrait; this node is what makes it switchable - <c>--portrait</c> on the
/// command line gives a desktop window the phone's canvas, which is how phone layouts are checked
/// through _UiShot (with --resolution 540x1212, the Pixel 9's shape).
///
/// An autoload, so it runs before the first scene lays itself out.
/// </remarks>
public partial class DisplayProfile : Node
{
	public static readonly Vector2I LandscapeCanvas = new(1280, 720);
	public static readonly Vector2I PortraitCanvas = new(720, 1280);

	/// <summary>
	/// The game's name as a player reads it (Eric, 2026-11-08): the window title, set below, and every line
	/// of text that names the game. NOT project.godot's config/name, which stays "The One He Missed": Godot
	/// names the desktop save folder after it, so changing it moves every save, and Windows forbids this
	/// name's ":" in a folder name anyway (.ai/game-name.md; .ai/releasing.md, "The rename").
	/// </summary>
	public const string GameTitle = "The Last Wizard: Survivors";

	/// <summary>True on a phone, or on a desktop launched with --portrait.</summary>
	public static bool IsPortrait { get; private set; }

	public override void _EnterTree()
	{
		IsPortrait = OS.HasFeature("mobile")
			|| Array.IndexOf(OS.GetCmdlineUserArgs(), "--portrait") >= 0
			|| Array.IndexOf(OS.GetCmdlineArgs(), "--portrait") >= 0;

		if (SaveManager.IsToolSceneLaunch())
			foreach (string arg in OS.GetCmdlineUserArgs())
				if (arg.StartsWith("--fake-monitors=") && int.TryParse(arg.Substring("--fake-monitors=".Length), out int fake))
					screenCountOverride = Math.Max(0, fake);

		Window root = GetTree().Root;
		// Godot titles the window from config/name before any autoload runs; this is the earliest the
		// game's own name can replace it. The taskbar and Alt+Tab read the same title.
		root.Title = GameTitle;
		if (IsPortrait)
		{
			root.ContentScaleSize = PortraitCanvas;
			root.ContentScaleAspect = Window.ContentScaleAspectEnum.KeepWidth;
		}
		else
		{
			root.ContentScaleSize = LandscapeCanvas;
			root.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
		}

		// F11 and Alt+Enter work over every screen, the pause screen and the level-up included.
		ProcessMode = ProcessModeEnum.Always;
		askedFullscreen = StartsFullscreen();
		// The chosen monitor first, while the window is still windowed: fullscreen takes the window's
		// current screen, so moving first is what makes the preference hold. A stale index (the monitor
		// was unplugged) is ignored and the window stays where the OS put it.
		if (DesktopControlsAvailable && !SaveManager.IsToolSceneLaunch())
			MoveToScreen(GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data.MonitorIndex ?? -1);
		if (askedFullscreen)
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
		else if (DesktopControlsAvailable && !SaveManager.IsToolSceneLaunch())
			ToWindow(GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data);
		if (DisplayServer.GetName() != "headless")
			CallDeferred(MethodName.LogWindow);
	}

	private bool askedFullscreen;

	private void LogWindow() =>
		GD.Print($"DisplayProfile: {(askedFullscreen ? "fullscreen asked" : "windowed launch")}; the window is "
			+ $"{DisplayServer.WindowGetMode()} at {DisplayServer.WindowGetSize()} (F11 or Alt+Enter toggles fullscreen)");

	// 2026-10-29, Eric: the game "should start in fullscreen when i start the game". Set here rather
	// than as window/size/mode in project.godot, because the tools photograph exact canvases with
	// --resolution (_UiShot at 1280x720, the Pixel 9's 540x1212) and a fullscreen window takes the
	// screen's size instead. Godot consumes its own flags (--windowed, --resolution, --position): none
	// of them reaches OS.GetCmdlineArgs, so they cannot be read here. What can be: every tool is an
	// underscore scene (SaveManager.IsToolSceneLaunch), the phone canvas (IsPortrait, the phone and
	// --portrait), headless, and the user argument "-- --windowed" for a window by hand. Saved preferences
	// control normal launches; tools retain their requested capture sizes.
	private bool StartsFullscreen() =>
		!IsPortrait
		&& DisplayServer.GetName() != "headless"
		&& !SaveManager.IsToolSceneLaunch()
		&& (GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data.Fullscreen ?? true)
		&& Array.IndexOf(OS.GetCmdlineUserArgs(), "--windowed") < 0;

	// F11 or Alt+Enter: fullscreen to the preferred window and back, saved with the other display settings.
	// Read in _Input, before the
	// menus: Enter is ui_accept, and a menu would otherwise press its focused button on Alt+Enter.
	public override void _Input(InputEvent @event)
	{
		if (OS.HasFeature("mobile") || @event is not InputEventKey { Pressed: true, Echo: false } key)
			return;
		bool toggle = key.Keycode == Key.F11
			|| (key.AltPressed && key.Keycode is Key.Enter or Key.KpEnter);
		if (!toggle)
			return;
		GetViewport().SetInputAsHandled();
		StoreFullscreen(this, !IsFullscreen);
		CallDeferred(MethodName.LogWindow);
	}

	// Auto windows fit the usable desktop; explicit preferences are clamped if the display changed.
	public static bool DesktopControlsAvailable => !IsPortrait && DisplayServer.GetName() != "headless";
	public static bool IsFullscreen => DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
	public static Vector2I NativeResolution => DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());

	public static Vector2I FitWindow(Vector2I wanted, Vector2I usable)
	{
		Vector2I available = new(Math.Max(320, usable.X - 32), Math.Max(240, usable.Y - 64));
		if (wanted.X <= 0 || wanted.Y <= 0)
		{
			wanted = new Vector2I(1600, 900);
			available = new Vector2I(Mathf.RoundToInt(available.X * 0.9f), Mathf.RoundToInt(available.Y * 0.9f));
		}
		float fit = Mathf.Min(1f, Mathf.Min(available.X / (float)wanted.X, available.Y / (float)wanted.Y));
		return new Vector2I(Mathf.RoundToInt(wanted.X * fit), Mathf.RoundToInt(wanted.Y * fit));
	}

	public static List<Vector2I> WindowSizeChoices(Vector2I usable)
	{
		Vector2I[] sizes = {
			new(960,540), new(1024,576), new(1024,768), new(1152,648), new(1280,720), new(1280,800),
			new(1366,768), new(1440,900), new(1600,900), new(1680,1050), new(1920,1080), new(1920,1200),
			new(2560,1440), new(2560,1600), new(3440,1440), new(3840,2160),
		};
		return sizes.Where(s => s.X <= usable.X - 32 && s.Y <= usable.Y - 64).OrderBy(s => s.X).ThenBy(s => s.Y).ToList();
	}

	/// <summary>
	/// Everything the Options screen's display controls can change, as it stands now: the saved
	/// preferences and the screen the window actually sits on (which a saved -1 does not name).
	/// Taken before a change so the timed Keep / Revert step (DisplayConfirm) can put it all back.
	/// </summary>
	public readonly struct DisplayState
	{
		public readonly bool Fullscreen;
		public readonly int WindowWidth, WindowHeight, MonitorIndex, Screen;

		public DisplayState(SaveData data)
		{
			Fullscreen = data?.Fullscreen ?? IsFullscreen;
			WindowWidth = data?.WindowWidth ?? 0;
			WindowHeight = data?.WindowHeight ?? 0;
			MonitorIndex = data?.MonitorIndex ?? -1;
			Screen = DisplayServer.WindowGetCurrentScreen();
		}
	}

	public static DisplayState Snapshot(Node context) =>
		new(context?.GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data);

	/// <summary>Puts a snapshot back: the saved fields, the screen, the mode. One save write.</summary>
	public static void Restore(Node context, DisplayState state)
	{
		if (!DesktopControlsAvailable) return;
		SaveManager manager = context?.GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (manager != null)
		{
			manager.Data.Fullscreen = state.Fullscreen;
			manager.Data.WindowWidth = state.WindowWidth;
			manager.Data.WindowHeight = state.WindowHeight;
			manager.Data.MonitorIndex = state.MonitorIndex;
		}
		MoveToScreen(state.Screen);
		if (state.Fullscreen) DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
		else ToWindow(manager?.Data);
		manager?.SaveGame();
	}

	/// <summary>The monitors a player can choose between, only asked for when there is more than one.</summary>
	public static int ScreenCount => screenCountOverride > 0 ? screenCountOverride : DisplayServer.GetScreenCount();

	// Tool-scene seam: _UiShot --fake-monitors=N photographs the Options monitor row on a
	// one-monitor machine. Only what the menu SHOWS reads it; moving the window stays
	// bounds-checked against the real screens (MoveToScreen), so a fake pick moves nothing.
	private static int screenCountOverride;

	public static void StoreMonitor(Node context, int index)
	{
		if (!DesktopControlsAvailable) return;
		SaveManager manager = context?.GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (manager != null) manager.Data.MonitorIndex = index;
		MoveToScreen(index);
		// A windowed game is re-fitted and re-centred: the new monitor's usable area may be smaller.
		if (!IsFullscreen) ToWindow(manager?.Data);
		manager?.SaveGame();
	}

	// Moves the window to that screen, through windowed mode when it is fullscreen - fullscreen is
	// bound to the screen it was entered on, so it is left, moved and entered again. -1 and a stale
	// index move nothing.
	private static void MoveToScreen(int index)
	{
		if (index < 0 || index >= DisplayServer.GetScreenCount()
			|| index == DisplayServer.WindowGetCurrentScreen())
			return;
		bool fullscreen = IsFullscreen;
		if (fullscreen) DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		DisplayServer.WindowSetCurrentScreen(index);
		if (fullscreen) DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
	}

	public static void StoreFullscreen(Node context, bool fullscreen)
	{
		if (!DesktopControlsAvailable) return;
		SaveManager manager = context?.GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (manager != null) manager.Data.Fullscreen = fullscreen;
		if (fullscreen) DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
		else ToWindow(manager?.Data);
		manager?.SaveGame();
	}

	public static void StoreWindowSize(Node context, Vector2I size)
	{
		SaveManager manager = context?.GetNodeOrNull<SaveManager>("/root/SaveManager");
		if (manager == null || !DesktopControlsAvailable) return;
		manager.Data.WindowWidth = size.X;
		manager.Data.WindowHeight = size.Y;
		if (!IsFullscreen) ToWindow(manager.Data);
		manager.SaveGame();
	}

	private static void ToWindow(SaveData data)
	{
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		int screen = DisplayServer.WindowGetCurrentScreen();
		Rect2I usable = DisplayServer.ScreenGetUsableRect(screen);
		var size = FitWindow(new Vector2I(data?.WindowWidth ?? 0, data?.WindowHeight ?? 0), usable.Size);
		if (data != null && data.WindowWidth > 0 && data.WindowHeight > 0)
		{
			data.WindowWidth = size.X;
			data.WindowHeight = size.Y;
		}
		DisplayServer.WindowSetSize(size);
		DisplayServer.WindowSetPosition(usable.Position + (usable.Size - size) / 2);
	}
}
