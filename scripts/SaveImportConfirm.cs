using Godot;
using System;

namespace TheOneHeMissed.scripts;

/// <summary>
/// The screen that stands between picking a save file and replacing the save with it
/// (.ai/in-flight.md item 14): what the file holds against what this device holds, side by side,
/// and the promise that the old save is kept. Nothing is written until "Replace my save".
/// </summary>
/// <remarks>
/// ReportBugScreen's shape: a CanvasLayer put up beside the menu (never inside its navigator), its
/// own MenuNavigator, SafeArea for the phone's corners, Escape/B to back out. The selection opens
/// on "Keep my save": a pad resting on A must land on the harmless answer, the way Reset Progress
/// cannot fire on one press. The replace itself is the caller's (SaveManager.ImportSave, passed in),
/// so _UiShot can photograph this screen with a fabricated file and no save anywhere in danger.
/// </remarks>
public partial class SaveImportConfirm : CanvasLayer
{
	// Above the pause screen (100) and the set detail screen (110), like ReportBugScreen.
	private const int ScreenLayer = 120;

	private static readonly CodexRows.Palette Ink = CodexRows.Stone;

	// The danger ink: a warm red on the unlit stone frame, measured in tools/art/ui_contrast.py
	// (options_pairs, DANGER_INK) - the same red Reset Progress wears.
	private static readonly Color DangerInk = new(0.949f, 0.541f, 0.502f);

	private SaveTransfer.Summary current;
	private SaveTransfer.Summary incoming;
	private Func<string> replace;
	private Action done;
	private Action closed;

	private Control root = null!;
	private Label heading = null!;
	private Control comparison = null!;
	private Label keptLine = null!;
	private Label status = null!;
	private Button keepButton = null!;
	private Button replaceButton = null!;
	private bool replaced;

	/// <summary>
	/// Puts the screen up over <paramref name="parent"/>. <paramref name="replace"/> performs the
	/// import when the player confirms - null for success, otherwise the refusal in the player's
	/// words. <paramref name="onDone"/> runs when the player leaves after a successful import (the
	/// menu is a view over the save and all of it is stale, so the caller rebuilds it);
	/// <paramref name="onClosed"/> when they back out with nothing changed.
	/// </summary>
	public static SaveImportConfirm Open(Node parent, SaveData currentSave, SaveData incomingSave,
		Func<string> replace, Action onDone, Action onClosed)
	{
		var screen = new SaveImportConfirm
		{
			Name = "SaveImportConfirm",
			current = SaveTransfer.Summarize(currentSave),
			incoming = SaveTransfer.Summarize(incomingSave),
			replace = replace,
			done = onDone,
			closed = onClosed,
		};
		parent.AddChild(screen);
		return screen;
	}

	public override void _Ready()
	{
		Layer = ScreenLayer;
		root = Build();
		SfxPlayer.Global(SfxCatalog.UiOpen);
		// Clear of the phone's rounded corners and camera hole; nothing on desktop.
		SafeArea.Apply(root);
		MenuNavigator.Attach(this, keepButton);
	}

	private Control Build()
	{
		var screenRoot = new Control
		{
			Name = "SaveImportRoot",
			// STOP: a click that misses the panel must not reach the menu underneath.
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		screenRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(screenRoot);

		var dim = new ColorRect
		{
			Color = new Color(0.02f, 0.025f, 0.04f, 0.86f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		screenRoot.AddChild(dim);

		bool narrow = ResponsiveLayout.IsNarrow(this);
		var panel = new PanelContainer { Name = "SaveImportPanel" };
		// Centred and sized by its content: a question, not a form, so it does not take the screen.
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.GrowHorizontal = Control.GrowDirection.Both;
		panel.GrowVertical = Control.GrowDirection.Both;
		panel.CustomMinimumSize = new Vector2(narrow ? 620f : 780f, 0f);
		BonelightSkin.ApplyPanel(panel);
		screenRoot.AddChild(panel);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 14);
		panel.AddChild(column);

		heading = new Label { Text = "Replace your save?" };
		ResponsiveLayout.SetFont(heading, ResponsiveLayout.TextRole.Display);
		heading.AddThemeColorOverride("font_color", Ink.Heading);
		column.AddChild(heading);

		comparison = BuildComparison();
		column.AddChild(comparison);

		keptLine = new Label
		{
			Text = "Whatever you choose, the save on this device is kept beside the new one.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		ResponsiveLayout.SetBodyText(keptLine, ResponsiveLayout.TextRole.Label);
		keptLine.AddThemeColorOverride("font_color", Ink.InkSoft);
		column.AddChild(keptLine);

		status = new Label { Name = "SaveImportStatus", Visible = false, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		ResponsiveLayout.SetBodyText(status, ResponsiveLayout.TextRole.Label);
		status.AddThemeColorOverride("font_color", DangerInk);
		column.AddChild(status);

		var buttons = new HBoxContainer();
		buttons.AddThemeConstantOverride("separation", 10);
		column.AddChild(buttons);

		keepButton = MakeButton("SaveImportKeep", "Keep my save", BonelightSkin.Emphasis.Primary);
		keepButton.Pressed += OnKeepPressed;
		buttons.AddChild(keepButton);

		replaceButton = MakeButton("SaveImportReplace", "Replace my save", BonelightSkin.Emphasis.Quiet);
		foreach (string state in new[] { "font_color", "font_hover_color", "font_focus_color", "font_pressed_color" })
			replaceButton.AddThemeColorOverride(state, DangerInk);
		replaceButton.Pressed += OnReplacePressed;
		buttons.AddChild(replaceButton);

		return screenRoot;
	}

	/// <summary>The two saves side by side: a row a line, this device on the left, the file on the
	/// right - the numbers a player would cross a room to compare by hand.</summary>
	private Control BuildComparison()
	{
		var grid = new GridContainer
		{
			Columns = 3,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		grid.AddThemeConstantOverride("h_separation", 24);
		grid.AddThemeConstantOverride("v_separation", 8);

		void Cell(string text, Color ink, bool expand = false, bool heading = false)
		{
			var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
			ResponsiveLayout.SetFont(label, heading ? ResponsiveLayout.TextRole.Body : ResponsiveLayout.TextRole.Label);
			label.AddThemeColorOverride("font_color", ink);
			if (expand)
				label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			grid.AddChild(label);
		}

		Cell(string.Empty, Ink.InkMeta, expand: true);
		Cell("This device", Ink.Heading, heading: true);
		Cell("The file", Ink.Heading, heading: true);

		void Row(string name, string here, string file)
		{
			Cell(name, Ink.InkSoft, expand: true);
			Cell(here, Ink.Ink);
			Cell(file, Ink.Ink);
		}

		Row("Playtime", current.Playtime, incoming.Playtime);
		Row("Victories", current.Victories.ToString(), incoming.Victories.ToString());
		Row("Wizards freed", current.Wizards.ToString(), incoming.Wizards.ToString());
		Row("Chapters opened", current.Chapters.ToString(), incoming.Chapters.ToString());
		Row("Achievements", current.Achievements.ToString(), incoming.Achievements.ToString());
		Row("Arcane Energy", current.Currency.ToString(), incoming.Currency.ToString());

		// On the sunk stone face the bug report's "Sent with it" uses, so it reads as information
		// rather than as something to press.
		var face = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		var faceStyle = new StyleBoxFlat { BgColor = Ink.Face, BorderColor = Ink.Edge };
		faceStyle.SetBorderWidthAll(1);
		faceStyle.SetCornerRadiusAll(4);
		faceStyle.SetContentMarginAll(12);
		face.AddThemeStyleboxOverride("panel", faceStyle);
		face.AddChild(grid);
		return face;
	}

	private Button MakeButton(string name, string text, BonelightSkin.Emphasis emphasis)
	{
		var button = new Button
		{
			Name = name,
			Text = text,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0f, 56f),
		};
		BonelightSkin.StyleButton(button, emphasis);
		ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Body);
		ResponsiveLayout.EnsureTouchTarget(button);
		return button;
	}

	private void OnReplacePressed()
	{
		if (replaced)
			return;
		string error = replace?.Invoke();
		if (error != null)
		{
			status.Text = error;
			status.Visible = true;
			return;
		}

		// Done: one thing left to read and one thing left to press. The old numbers are gone from
		// view because they are gone from the save; the caller rebuilds the menu on the way out.
		replaced = true;
		heading.Text = "Save replaced";
		comparison.Visible = false;
		status.Visible = false;
		keptLine.Text = "The file's progress is on this device now. The old save was kept beside it, in case you change your mind.";
		replaceButton.Visible = false;
		keepButton.Text = "Back to the menu";
		keepButton.GrabFocus();
	}

	private void OnKeepPressed()
	{
		SfxPlayer.Global(SfxCatalog.UiBack);
		// Hidden first, so the navigators under it stop treating its buttons as a selection on
		// this frame rather than on the next.
		Visible = false;
		QueueFree();
		if (replaced)
			done?.Invoke();
		else
			closed?.Invoke();
	}

	// B, Escape, or Android's back gesture: the harmless answer, like the Keep button.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel"))
			return;
		GetViewport().SetInputAsHandled();
		OnKeepPressed();
	}
}
