using Godot;
using System;
using TheOneHeMissed.scripts;

/// <summary>
/// The timed Keep / Revert step after a display change (in-flight item 20): the Options screen
/// applies the change first, then opens this over the menu. Keep closes it; Revert, Escape/B or
/// the ten-second clock running out puts the previous settings back. The clock is the whole point:
/// a change that left the screen unreadable undoes itself without needing to be seen.
/// </summary>
/// <remarks>
/// The revert action is handed in by the opener (MainMenu.Display takes a
/// <see cref="DisplayProfile.Snapshot"/> before applying), so this screen knows nothing about what
/// changed. It is a stacked screen like ReportBugScreen: opened beside the menu, not inside it, so
/// the menu's navigator and this one never step the same selection (.ai/controller-and-focus.md,
/// "Screens stack"). The backdrop is a full-screen STOP so a mouse cannot press the menu under it.
/// </remarks>
public partial class DisplayConfirm : CanvasLayer
{
	/// <summary>How long a change survives unconfirmed. Long enough to find the window again.</summary>
	public const float RevertSeconds = 10f;

	// Above the pause screen (100) and the set detail (110), with Report a bug (120).
	private const int ScreenLayer = 120;

	private Action revert;
	private Action closed;
	private double remaining = RevertSeconds;
	private Label countdown;
	private Button keepButton;
	private bool done;

	/// <summary>Opens the step over <paramref name="parent"/>. The change is already applied;
	/// <paramref name="revert"/> is what puts it back, <paramref name="closed"/> runs either way.</summary>
	public static DisplayConfirm Open(Node parent, Action revert, Action closed = null)
	{
		var screen = new DisplayConfirm { Name = "DisplayConfirm", revert = revert, closed = closed };
		parent.AddChild(screen);
		return screen;
	}

	public override void _Ready()
	{
		Layer = ScreenLayer;
		// Always: nothing pauses the main menu today, but a stopped clock here would hold a bad
		// display state forever, which is exactly what this screen exists to prevent.
		ProcessMode = ProcessModeEnum.Always;
		CodexRows.Palette ink = CodexRows.Stone;

		var root = new Control { Name = "DisplayConfirmRoot", MouseFilter = Control.MouseFilterEnum.Ignore };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		// The dim STOP backdrop: the menu is still visible - the player is judging the screen -
		// but cannot be pressed under the question.
		var backdrop = new ColorRect { Color = new Color(0f, 0f, 0f, 0.45f), MouseFilter = Control.MouseFilterEnum.Stop };
		backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(backdrop);

		var centre = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(centre);

		// The heading band's stone: the darkest fill under a gold rule (MainMenu.Shop's band inks).
		var panel = new PanelContainer { Name = "DisplayConfirmPanel" };
		var style = new StyleBoxFlat
		{
			BgColor = new Color(0.043f, 0.059f, 0.094f),
			BorderColor = new Color(0.541f, 0.416f, 0.118f),
		};
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(2);
		style.SetContentMarginAll(28);
		panel.AddThemeStyleboxOverride("panel", style);
		centre.AddChild(panel);

		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 16);
		panel.AddChild(column);

		Label title = CodexRows.MakeText("Keep these display settings?", ResponsiveLayout.TextRole.Title, ink.Ink);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		column.AddChild(title);

		countdown = CodexRows.MakeText(CountdownText(), ResponsiveLayout.TextRole.Label, ink.InkSoft);
		countdown.HorizontalAlignment = HorizontalAlignment.Center;
		// One line, never "in 8 / s." across two: the sentence sizes the panel, and it fits the
		// phone canvas with room (measured in the 540-wide shot).
		countdown.AutowrapMode = TextServer.AutowrapMode.Off;
		column.AddChild(countdown);

		keepButton = new Button
		{
			Name = "KeepDisplayButton",
			Text = "Keep",
			FocusMode = Control.FocusModeEnum.All,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(180, 56),
		};
		BonelightSkin.StyleButton(keepButton, BonelightSkin.Emphasis.Primary);
		ResponsiveLayout.SetFont(keepButton, ResponsiveLayout.TextRole.Body);
		ResponsiveLayout.EnsureTouchTarget(keepButton);
		keepButton.Pressed += OnKeep;

		var revertButton = new Button
		{
			Name = "RevertDisplayButton",
			Text = "Revert",
			FocusMode = Control.FocusModeEnum.All,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(180, 56),
		};
		BonelightSkin.StyleButton(revertButton, BonelightSkin.Emphasis.Quiet);
		ResponsiveLayout.SetFont(revertButton, ResponsiveLayout.TextRole.Body);
		ResponsiveLayout.EnsureTouchTarget(revertButton);
		revertButton.Pressed += OnRevert;

		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 16);
		row.AddChild(keepButton);
		row.AddChild(revertButton);
		column.AddChild(row);

		SfxPlayer.Global(SfxCatalog.UiOpen);
		SafeArea.Apply(root);
		MenuNavigator.Attach(this, keepButton);
	}

	public override void _Process(double delta)
	{
		if (done)
			return;
		remaining -= delta;
		countdown.Text = CountdownText();
		if (remaining <= 0)
			OnRevert();
	}

	// B / Escape is the cautious answer: the player backing out of the question gets their old
	// settings back, as the clock would have given them.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (done || !@event.IsActionPressed("ui_cancel"))
			return;
		GetViewport().SetInputAsHandled();
		OnRevert();
	}

	private string CountdownText() =>
		$"Reverting to the previous settings in {Math.Max(1, (int)Math.Ceiling(remaining))} s.";

	private void OnKeep()
	{
		if (done) return;
		done = true;
		SfxPlayer.Global(SfxCatalog.UiClick);
		Close();
	}

	private void OnRevert()
	{
		if (done) return;
		done = true;
		SfxPlayer.Global(SfxCatalog.UiBack);
		revert?.Invoke();
		Close();
	}

	private void Close()
	{
		closed?.Invoke();
		QueueFree();
	}
}
