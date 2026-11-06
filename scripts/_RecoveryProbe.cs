using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using TheOneHeMissed.scripts;

// Real keyboard/pad routing, native status tooltips and Overlooked's damage/expiry checks.
// Run scenes/_RecoveryProbe.tscn with -- --part=footer|tooltip|overlooked --no-save-writes.
public partial class _RecoveryProbe : Node
{
	private int failures;
	private string part;

	public override void _Ready()
	{
		SaveManager.WritesDisabled = true;
		ProcessMode = ProcessModeEnum.Always;
		part = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--part="))?.Substring(7) ?? "footer";
		Callable.From(Run).CallDeferred();
	}

	private async void Run()
	{
		try
		{
			if (part == "footer") await Footer();
			else if (part == "tooltip") await Tooltip();
			else if (part == "overlooked") await Overlooked();
			else throw new Exception($"Unknown part {part}");
		}
		catch (Exception ex) { Check(false, ex.ToString()); }
		GD.Print($"RECOVERY {part}: {(failures == 0 ? "PASS" : "FAIL")} ({failures} failures)");
		GetTree().Quit(failures == 0 ? 0 : 1);
	}

	private void Check(bool ok, string message)
	{
		if (!ok) failures++;
		GD.Print($"RECOVERY {(ok ? "PASS" : "FAIL")}: {message}");
	}

	private async Task Frames(int count = 3)
	{
		for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private async Task Footer()
	{
		var screen = ResourceLoader.Load<PackedScene>("res://scenes/GameOverScreen.tscn").Instantiate<GameOverScreen>();
		AddChild(screen);
		screen.Present(new RunReport { Result = new RunResult(), CharacterName = "The Archmage", StageName = "The Forest" });
		GetTree().Paused = true;
		await ToSignal(GetTree().CreateTimer(3), SceneTreeTimer.SignalName.Timeout);
		var retry = screen.FindChild("RetryButton", true, false) as Button;
		var menu = screen.FindChild("ContinueButton", true, false) as Button;
		if (OS.GetCmdlineUserArgs().Contains("--automatic-focus"))
		{
			retry.FocusNeighborRight = new NodePath();
			menu.FocusNeighborLeft = new NodePath();
		}
		GD.Print($"RECOVERY footer rects: retry {retry.GetGlobalRect()}, menu {menu.GetGlobalRect()}, viewport {GetViewport().GetVisibleRect()}");
		Check(!screen.Counting, "results finished counting before navigation");
		retry.GrabFocus();
		await Frames();
		await KeyPress(Key.Right);
		Check(menu.HasFocus(), "keyboard Right: Retry -> Main menu");
		await KeyPress(Key.Left);
		Check(retry.HasFocus(), "keyboard Left: Main menu -> Retry");
		retry.GrabFocus();
		await PadPress(JoyButton.DpadRight);
		Check(menu.HasFocus(), "D-pad Right: Retry -> Main menu");
		await PadPress(JoyButton.DpadLeft);
		Check(retry.HasFocus(), "D-pad Left: Main menu -> Retry");
		Input.ParseInputEvent(new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 1f });
		await Frames();
		Check(menu.HasFocus(), "left stick Right: Retry -> Main menu");
		Input.ParseInputEvent(new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 0f });
		await Frames();
		Input.ParseInputEvent(new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = -1f });
		await Frames();
		Check(retry.HasFocus(), "left stick Left: Main menu -> Retry");
		Input.ParseInputEvent(new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 0f });
		await Frames();
		await Shot("footer");
		foreach (Button button in new[] { retry, menu })
		{
			// Observe real BaseButton activation without replacing this probe scene with the destination.
			foreach (var connection in button.GetSignalConnectionList(BaseButton.SignalName.Pressed))
				button.Disconnect(BaseButton.SignalName.Pressed, connection["callable"].AsCallable());
			int presses = 0;
			button.Pressed += () => presses++;
			button.GrabFocus();
			await KeyPress(Key.Enter);
			Check(presses == 1, $"Enter activates {button.Text}");
			await PadPress(JoyButton.A);
			Check(presses == 2, $"pad A activates {button.Text}");
		}
	}

	private async Task KeyPress(Key key)
	{
		Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
		await Frames();
		Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
		await Frames();
	}

	private async Task PadPress(JoyButton key)
	{
		Input.ParseInputEvent(new InputEventJoypadButton { ButtonIndex = key, Pressed = true });
		await Frames();
		Input.ParseInputEvent(new InputEventJoypadButton { ButtonIndex = key, Pressed = false });
		await Frames();
	}

	private async Task Tooltip()
	{
		StatusId status = Enum.TryParse(OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--status="))?.Substring(9), out StatusId chosen)
			? chosen : StatusId.Root;
		var keyword = StatusKeyword.Create(status, status == StatusId.Root ? "Freezing" : KeywordText.Statuses[status].Name, Colors.White, 24, null);
		keyword.Position = new Vector2(GetViewport().GetVisibleRect().Size.X - 160f, 80f);
		AddChild(keyword);
		await Frames();
		// ParseInputEvent enters before the root window's content scaling, like a hardware mouse.
		Vector2 at = GetViewport().GetScreenTransform() * keyword.GetGlobalRect().GetCenter();
		Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at, Relative = Vector2.One * 20f });
		await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);
		var windows = Descendants(GetTree().Root).OfType<Window>().Where(w => w.Visible).ToArray();
		foreach (Window window in windows) GD.Print($"RECOVERY tooltip window: {window.Name} {window.Position} {window.Size}");
		var tooltip = windows.FirstOrDefault(w => w.GetChildren(true).Any(n => n is PanelContainer));
		Check(tooltip != null, "real mouse hover opens native tooltip");
		if (tooltip != null)
		{
			Check(tooltip.Size.X <= 460, "hover tooltip is at most 460 px wide");
			Check(tooltip.Size.Y >= 80, "hover tooltip has room for wrapped description");
			Check(tooltip.Size.Y < 300, "hover tooltip has no oversized empty height");
		}
		await Shot("tooltip");
		// Exercise the same card through its separate click/tap popover path.
		keyword.EmitSignal(BaseButton.SignalName.Pressed);
		await Frames(10);
		Check(ElementTagPopover.IsOpen, "status click still opens its popover");
		await Shot("popover");
		ElementTagPopover.Close();
	}

	private static System.Collections.Generic.IEnumerable<Node> Descendants(Node root)
	{
		foreach (Node child in root.GetChildren(true))
		{
			yield return child;
			foreach (Node nested in Descendants(child)) yield return nested;
		}
	}

	private async Task Overlooked()
	{
		GetNode<SaveManager>("/root/SaveManager").ReplaceInMemory(new SaveData());
		Global.SelectedCharacterIdx = CharacterRoster.IndexOf("archmage");
		var player = ResourceLoader.Load<PackedScene>("res://scenes/player.tscn").Instantiate<Player>();
		var world = new Node2D();
		AddChild(world);
		world.AddChild(player);
		player.Position = new Vector2(200f, 200f);
		await Frames();
		player.CurrentHP = 5;
		player.TakeDamage(10000);
		Check(player.CurrentHP == 5 && player.IsInvincible, "killing blow is refused without healing");
		var sprite = player.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		Check(sprite.Material is ShaderMaterial, "invincibility visual is active");
		await ToSignal(GetTree().CreateTimer(9.8, processInPhysics: true), SceneTreeTimer.SignalName.Timeout);
		player.TakeDamage(10000);
		Check(player.IsInvincible && player.CurrentHP == 5, "still immune just before ten seconds");
		await ToSignal(GetTree().CreateTimer(0.4, processInPhysics: true), SceneTreeTimer.SignalName.Timeout);
		Check(!player.IsInvincible, "immunity expires after ten seconds");
		Check(sprite.Material is not ShaderMaterial, "invincibility visual expires with protection");
		player.TakeDamage(1);
		Check(player.CurrentHP == 4, "damage resumes after expiry");
		player.TakeDamage(10000);
		Check(player.IsDead, "Overlooked cannot trigger a second time");
	}

	private async Task Shot(string name)
	{
		if (DisplayServer.GetName() == "headless") return;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		string path = $"{OS.GetUserDataDir()}/recovery-{name}-{(int)GetViewport().GetVisibleRect().Size.X}.png";
		GetViewport().GetTexture().GetImage().SavePng(path);
		GD.Print($"RECOVERY screenshot {path}");
	}
}
