using Godot;
using System;
using System.Threading.Tasks;
using TheOneHeMissed.scripts;

// Measures the first rendered menu after a real Enter press; keeps the player's save untouched.
public partial class _TitleLoadProbe : Node
{
	private ulong previousFrame;
	private ulong largestGap;
	private bool measuring;
	private ulong largestPreparationGap;
	private TitleScreen observedTitle;
	private float largestGapProgress;

	public override void _Ready()
	{
		SaveManager.WritesDisabled = true;
		ProcessMode = ProcessModeEnum.Always;
		GetNode<SaveManager>("/root/SaveManager").ReplaceInMemory(new SaveData
		{
			ProgressResetNoticePending = Array.IndexOf(OS.GetCmdlineUserArgs(), "--reset-notice") >= 0,
		});
		RenderingServer.FramePostDraw += OnDraw;
		Callable.From(Run).CallDeferred();
	}

	public override void _ExitTree() => RenderingServer.FramePostDraw -= OnDraw;

	private void OnDraw()
	{
		ulong now = Time.GetTicksUsec();
		if (measuring && previousFrame > 0) largestGap = Math.Max(largestGap, now - previousFrame);
		else if (previousFrame > 0 && now - previousFrame > largestPreparationGap)
		{
			largestPreparationGap = now - previousFrame;
			largestGapProgress = (observedTitle?.FindChild("MainMenu", false, false) as MainMenu)?.PreparationProgress ?? 0f;
		}
		previousFrame = now;
	}

	private async void Run()
	{
		try
		{
			if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--release-portrait") >= 0)
			{
				if (OS.IsDebugBuild() || !DisplayProfile.IsPortrait || DisplayProfile.DesktopControlsAvailable)
					throw new Exception("This check requires the release engine and a portrait canvas");
				GD.Print("TITLEPROBE release engine, portrait layout and no desktop display controls");
			}
			foreach (string arg in OS.GetCmdlineUserArgs())
				if (arg.StartsWith("--asset-pack="))
				{
					if (!ProjectSettings.LoadResourcePack(arg.Substring("--asset-pack=".Length)))
						throw new Exception("Could not mount the exported asset pack");
					GD.Print("TITLEPROBE testing exported asset pack");
				}
			ulong preparationStart = Time.GetTicksUsec();
			bool returning = Array.IndexOf(OS.GetCmdlineUserArgs(), "--returning") >= 0;
			Global.OpenMenuImmediately = returning;
			var title = ResourceLoader.Load<PackedScene>("res://scenes/TitleScreen.tscn").Instantiate<TitleScreen>();
			observedTitle = title;
			bool failureTest = Array.IndexOf(OS.GetCmdlineUserArgs(), "--failed-load") >= 0;
			bool preparationFailure = Array.IndexOf(OS.GetCmdlineUserArgs(), "--failed-preparation") >= 0;
			if (preparationFailure)
				GetNode<SaveManager>("/root/SaveManager").Data.ArcaneUpgradeLevels = null;
			if (failureTest) title.MenuScenePath = "res://scenes/DoesNotExist.tscn";
			AddChild(title);
			int loadingFrames = 0;
			bool loadingShot = false;
			bool retried = false;
			for (; !title.IsPrepared && loadingFrames < 3600; loadingFrames++)
			{
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				if ((failureTest || preparationFailure) && !retried && title.FindChild("RetryLoading", true, false) is Button retry)
				{
					GD.Print("TITLEPROBE load failure offers retry");
					var details = title.FindChild("StartupErrorDetails", true, false) as Label;
					string expected = preparationFailure ? "NullReferenceException" : "DoesNotExist";
					if (details == null || string.IsNullOrWhiteSpace(details.Text) || !title.StartupErrorReport.Contains(expected))
						throw new Exception("Failure screen omitted the loading error");
					if (failureTest && !title.StartupErrorReport.Contains("Cannot open file"))
						throw new Exception("Failure report omitted the native resource-loader error");
					var copy = title.FindChild("CopyStartupError", true, false) as Button;
					if (copy == null) throw new Exception("Failure screen omitted Copy error details");
					string clipboardBefore = DisplayServer.ClipboardGet();
					try
					{
						copy.EmitSignal(BaseButton.SignalName.Pressed);
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
						string copied = DisplayServer.ClipboardGet().Replace("\r\n", "\n");
						if (copied != title.StartupErrorReport.Replace("\r\n", "\n"))
							throw new Exception($"Failure screen did not copy the full error report ({copied.Length}/{title.StartupErrorReport.Length} characters)");
					}
					finally { DisplayServer.ClipboardSet(clipboardBefore); }
					foreach (Control control in new Control[] { retry, copy, details })
						if (!GetViewport().GetVisibleRect().Encloses(control.GetGlobalRect()))
							throw new Exception("Failure recovery controls extend outside the viewport");
					GD.Print("TITLEPROBE failure details, copy and recovery controls pass");
					if (preparationFailure) GetNode<SaveManager>("/root/SaveManager").ReplaceInMemory(new SaveData());
					title.MenuScenePath = "res://scenes/MainMenu.tscn";
					retry.EmitSignal(BaseButton.SignalName.Pressed);
					retried = true;
				}
				if (!loadingShot && title.FindChild("MainMenu", false, false) is MainMenu { PreparationProgress: >= 0.6f })
				{
					var ward = title.FindChild("LoadingWard", true, false) as Control;
					if (ward == null || !GetViewport().GetVisibleRect().Encloses(ward.GetGlobalRect()))
						throw new Exception("Loading artwork extends outside the viewport");
					GetViewport().GetTexture().GetImage().SavePng($"{OS.GetUserDataDir()}/title-loading.png");
					loadingShot = true;
				}
				// Repeated early presses must not skip preparation or choose Start Run underneath it.
				if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--stress-gc") >= 0 && loadingFrames % 5 == 0)
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();
				}
				if (!title.IsPrepared && Array.IndexOf(OS.GetCmdlineUserArgs(), "--no-early-input") < 0)
				{
					Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = true });
					Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = false });
				}
			}
			if (!title.IsPrepared) throw new Exception("Title preparation did not complete");
			GD.Print($"TITLEPROBE prepared={(Time.GetTicksUsec() - preparationStart) / 1000.0:0.0} ms loading_frames={loadingFrames} max_loading_gap={largestPreparationGap / 1000.0:0.0} ms at progress={largestGapProgress:0.00}");
			await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
			if (!returning && title.FindChild("MainMenu", false, false) is MainMenu { Visible: true })
				throw new Exception("Loading input leaked through to the title/menu");
			if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--reset-notice") >= 0)
			{
				if (title.FindChild("StoryResetContinue", true, false) is not Button reset)
					throw new Exception("Owed story-reset notice was skipped");
				reset.EmitSignal(BaseButton.SignalName.Pressed);
				await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
				if (GetNode<SaveManager>("/root/SaveManager").Data.ProgressResetNoticePending)
					throw new Exception("Story-reset dismissal did not clear the in-memory notice");
				GD.Print("TITLEPROBE story-reset notice shown and dismissed");
			}
			var version = title.GetNode<Label>("Version");
			var prompt = title.GetNode<Control>("TitleImage/Prompt");
			if (!GetViewport().GetVisibleRect().Encloses(version.GetGlobalRect())
				|| (!returning && version.GetGlobalRect().Intersects(prompt.GetGlobalRect().Grow(8f))))
				throw new Exception("Version label overlaps the prompt or extends off screen");
			GD.Print("TITLEPROBE version label clears the title prompt");
			ulong start = Time.GetTicksUsec();
			measuring = true;
			bool startedRun = false;
			if (title.FindChild("StartRunButton", true, false) is Button startButton)
				startButton.Pressed += () => startedRun = true;
			if (!returning)
			{
				string device = "key";
				foreach (string arg in OS.GetCmdlineUserArgs()) if (arg.StartsWith("--input=")) device = arg.Substring(8);
				GD.Print($"TITLEPROBE opening with {device}");
				if (device == "pad")
				{
					Input.ParseInputEvent(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true });
					Input.ParseInputEvent(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false });
				}
				else if (device == "mouse" || device == "touch")
				{
					var button = title.FindChild("StartRunButton", true, false) as Button;
					Vector2 at = GetViewport().GetScreenTransform() * button.GetGlobalRect().GetCenter();
					if (device == "touch")
					{
						Input.ParseInputEvent(new InputEventScreenTouch { Position = at, Index = 0, Pressed = true });
						Input.ParseInputEvent(new InputEventScreenTouch { Position = at, Index = 0, Pressed = false });
					}
					else
					{
						Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = MouseButton.Left, Pressed = true });
						Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = MouseButton.Left, Pressed = false });
					}
				}
				else
				{
					Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = true });
					Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = false });
				}
			}
			for (int frame = 0; frame < 3600; frame++)
			{
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				var menu = title.FindChild("MainMenu", false, false) as MainMenu;
				if (menu == null || !menu.IsVisibleInTree()) continue;
				measuring = false;
				double pressMilliseconds = (Time.GetTicksUsec() - start) / 1000.0;
				GD.Print($"TITLEPROBE press_to_menu={pressMilliseconds:0.0} ms max_draw_gap={largestGap / 1000.0:0.0} ms");
				if (!returning && pressMilliseconds > 250.0) throw new Exception("Prepared title-to-menu press exceeded 250 ms");
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				if (startedRun || GetTree().CurrentScene != this) throw new Exception("Opening press also started a run");
				if (GetViewport().GuiGetFocusOwner()?.Name != "StartRunButton")
					throw new Exception("Prepared menu did not focus Start Run");
				GetViewport().GetTexture().GetImage().SavePng($"{OS.GetUserDataDir()}/title-load-probe.png");
				var quit = title.FindChild("QuitButton", true, false) as Button;
				Rect2 screen = GetViewport().GetVisibleRect();
				if (quit == null || !quit.IsVisibleInTree() || !screen.Encloses(quit.GetGlobalRect())
					|| quit.GetGlobalRect().GetCenter().X < screen.Size.X * 0.65f
					|| quit.GetGlobalRect().GetCenter().Y < screen.Size.Y * 0.8f)
					throw new Exception("Quit is missing or outside the lower-right corner");
				var codex = title.FindChild("SpellIndexButton", true, false) as Button;
				codex.GrabFocus();
				Input.ParseInputEvent(new InputEventKey { Keycode = Key.Right, Pressed = true });
				Input.ParseInputEvent(new InputEventKey { Keycode = Key.Right, Pressed = false });
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				if (GetViewport().GuiGetFocusOwner() != quit) throw new Exception("Right from the Codex does not reach Quit");
				Input.ParseInputEvent(new InputEventKey { Keycode = Key.Left, Pressed = true });
				Input.ParseInputEvent(new InputEventKey { Keycode = Key.Left, Pressed = false });
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				if (GetViewport().GuiGetFocusOwner() != codex) throw new Exception("Left from Quit does not return to the Codex");
				foreach (var panel in new[] { ("ArcaneUpgradesButton", "BackFromArcaneButton"), ("UnlocksButton", "BackFromAchievementsButton"), ("OptionsButton", "BackFromOptionsButton") })
				{
					(title.FindChild(panel.Item1, true, false) as Button).EmitSignal(BaseButton.SignalName.Pressed);
					if (menu.IsAtRoot) throw new Exception($"Prepared menu could not open {panel.Item1}");
					if (quit.IsVisibleInTree()) throw new Exception("Quit stays visible over a menu panel");
					if (panel.Item1 == "UnlocksButton")
					{
						foreach (AchievementsPage.Filter filter in Enum.GetValues<AchievementsPage.Filter>())
						{
							(title.FindChild("AchievementsTab" + filter, true, false) as Button).EmitSignal(BaseButton.SignalName.Pressed);
							await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
							if (title.FindChild("AchievementList", true, false)?.GetChildCount() == 0)
								throw new Exception($"Achievements tab {filter} has no sections");
						}
						(title.FindChild("AchievementsTabAll", true, false) as Button).EmitSignal(BaseButton.SignalName.Pressed);
					}
					(title.FindChild(panel.Item2, true, false) as Button).EmitSignal(BaseButton.SignalName.Pressed);
					if (!menu.IsAtRoot) throw new Exception($"Prepared menu could not close {panel.Item1}");
				}
				GD.Print("TITLEPROBE prepared Powers, Achievements and Options open and close");
				GD.Print("TITLEPROBE lower-right Quit and keyboard navigation pass");
				CheckFooter(title);
				if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--display-options") >= 0)
					await CheckDisplayOptions(title);
				if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--quit") >= 0)
				{
					GD.Print("TITLEPROBE activating Quit");
					quit.EmitSignal(BaseButton.SignalName.Pressed);
					return;
				}
				GetTree().Quit();
				return;
			}
			throw new Exception("Menu did not appear");
		}
		catch (Exception ex) { GD.PrintErr(ex); GetTree().Quit(1); }
	}

	private void CheckFooter(TitleScreen title)
	{
		Rect2? first = null;
		foreach (string name in new[] { "OptionsButton", "SpellIndexButton", "QuitButton" })
		{
			Rect2 rect = (title.FindChild(name, true, false) as Button).GetGlobalRect();
			if (!GetViewport().GetVisibleRect().Encloses(rect)) throw new Exception($"{name} extends off screen");
			if (first is Rect2 baseline && (Math.Abs(rect.Position.Y - baseline.Position.Y) > 1f || Math.Abs(rect.Size.Y - baseline.Size.Y) > 1f))
				throw new Exception("Bottom buttons do not share one baseline and height");
			first = rect;
		}
	}

	private void CheckFilledWindow(TitleScreen title)
	{
		Rect2 drawn = GetViewport().GetFinalTransform() * GetViewport().GetVisibleRect();
		Vector2I physical = DisplayServer.WindowGetSize();
		if (drawn.Position.Length() > 1f || drawn.End.DistanceTo(physical) > 2f)
			throw new Exception($"Viewport leaves bars: {drawn}, window {physical}");
		var background = title.FindChild("LandscapeBackdrop", false, false) as Control;
		if (background == null || !background.GetGlobalRect().Grow(1f).Encloses(GetViewport().GetVisibleRect()))
			throw new Exception("The title forest does not cover the expanded viewport");
	}

	private async Task DrawFrames(int count = 8)
	{
		for (int i = 0; i < count; i++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
	}

	private async Task CheckDisplayOptions(TitleScreen title)
	{
		(title.FindChild("OptionsButton", true, false) as Button).EmitSignal(BaseButton.SignalName.Pressed);
		var fullscreen = title.FindChild("FullscreenToggle", true, false) as CheckButton;
		var sizes = title.FindChild("WindowResolution", true, false) as OptionButton;
		if (fullscreen == null || sizes == null) throw new Exception("Display controls are missing");
		fullscreen.ButtonPressed = true;
		await DrawFrames();
		if (!DisplayProfile.IsFullscreen || DisplayServer.WindowGetSize() != DisplayProfile.NativeResolution || !sizes.Disabled)
			throw new Exception("Fullscreen is not native, or window sizes remain enabled");
		CheckFilledWindow(title);
		fullscreen.GrabFocus();
		Input.ParseInputEvent(new InputEventKey { Keycode = Key.Down, Pressed = true });
		Input.ParseInputEvent(new InputEventKey { Keycode = Key.Down, Pressed = false });
		await DrawFrames(1);
		if (GetViewport().GuiGetFocusOwner()?.Name != "DamageNumbersToggle")
			throw new Exception("Fullscreen navigation gets stuck on the disabled window-size control");
		GetViewport().GetTexture().GetImage().SavePng($"{OS.GetUserDataDir()}/fullscreen-options.png");
		(title.FindChild("BackFromOptionsButton", true, false) as Button).EmitSignal(BaseButton.SignalName.Pressed);
		await DrawFrames();
		CheckFooter(title);
		GetViewport().GetTexture().GetImage().SavePng($"{OS.GetUserDataDir()}/fullscreen-native.png");
		(title.FindChild("OptionsButton", true, false) as Button).EmitSignal(BaseButton.SignalName.Pressed);
		fullscreen.ButtonPressed = false;
		await DrawFrames();
		if (DisplayProfile.IsFullscreen || sizes.Disabled) throw new Exception("Windowed mode did not enable size selection");
		fullscreen.GrabFocus();
		Input.ParseInputEvent(new InputEventKey { Keycode = Key.Down, Pressed = true });
		Input.ParseInputEvent(new InputEventKey { Keycode = Key.Down, Pressed = false });
		await DrawFrames(1);
		if (GetViewport().GuiGetFocusOwner() != sizes) throw new Exception("Windowed navigation skips the resolution chooser");
		int choice = 1;
		for (int i = 1; i < sizes.ItemCount; i++) if (sizes.GetItemText(i) == "1280 × 720") choice = i;
		string[] dimensions = sizes.GetItemText(choice).Split('×');
		Vector2I wanted = new(int.Parse(dimensions[0].Trim()), int.Parse(dimensions[1].Trim()));
		sizes.Select(choice);
		sizes.EmitSignal(OptionButton.SignalName.ItemSelected, choice);
		await DrawFrames();
		if (DisplayServer.WindowGetSize() != wanted) throw new Exception("The selected window resolution was not applied");
		SaveData saved = SaveData.FromVariant(GetNode<SaveManager>("/root/SaveManager").Data.ToGodotDictionary());
		if (saved.Fullscreen || saved.WindowWidth != wanted.X || saved.WindowHeight != wanted.Y)
			throw new Exception("Display preferences did not survive serialization");
		GetViewport().GetTexture().GetImage().SavePng($"{OS.GetUserDataDir()}/resolution-options.png");
		(title.FindChild("BackFromOptionsButton", true, false) as Button).EmitSignal(BaseButton.SignalName.Pressed);
		foreach (Vector2I size in new[] { new Vector2I(1280,720), new Vector2I(1280,800), new Vector2I(1024,768), new Vector2I(1600,720) })
		{
			DisplayServer.WindowSetSize(size);
			await DrawFrames();
			CheckFilledWindow(title);
			CheckFooter(title);
			GetViewport().GetTexture().GetImage().SavePng($"{OS.GetUserDataDir()}/display-{size.X}x{size.Y}.png");
		}
		for (int i = 0; i < 2; i++)
		{
			Input.ParseInputEvent(new InputEventKey { Keycode = Key.F11, Pressed = true });
			Input.ParseInputEvent(new InputEventKey { Keycode = Key.F11, Pressed = false });
			await DrawFrames();
		}
		if (DisplayServer.WindowGetSize() != wanted || DisplayProfile.IsFullscreen)
			throw new Exception("F11 did not restore the selected window size");
		GD.Print("TITLEPROBE native fullscreen, saved window sizes, F11, filled aspect ratios and aligned footer pass");
	}
}
