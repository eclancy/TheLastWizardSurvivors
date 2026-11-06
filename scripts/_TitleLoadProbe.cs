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
			ulong preparationStart = Time.GetTicksUsec();
			bool returning = Array.IndexOf(OS.GetCmdlineUserArgs(), "--returning") >= 0;
			Global.OpenMenuImmediately = returning;
			var title = ResourceLoader.Load<PackedScene>("res://scenes/TitleScreen.tscn").Instantiate<TitleScreen>();
			observedTitle = title;
			bool failureTest = Array.IndexOf(OS.GetCmdlineUserArgs(), "--failed-load") >= 0;
			if (failureTest) title.MenuScenePath = "res://scenes/DoesNotExist.tscn";
			AddChild(title);
			int loadingFrames = 0;
			bool loadingShot = false;
			bool retried = false;
			for (; !title.IsPrepared && loadingFrames < 3600; loadingFrames++)
			{
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				if (failureTest && !retried && title.FindChild("RetryLoading", true, false) is Button retry)
				{
					GD.Print("TITLEPROBE load failure offers retry");
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
				if (!title.IsPrepared)
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
}
