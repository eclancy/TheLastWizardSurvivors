using Godot;
using System;
using System.Collections.Generic;

namespace TheOneHeMissed.scripts;

/// <summary>
/// The rows a settings screen is made of: a volume slider, an on/off switch, a choice of a few, and
/// the Audio and Display sections built from them. The main menu's Options panel and the pause
/// screen's Options tab both build from here, so the two say the same settings in the same words,
/// in the same order, and look the same.
/// </summary>
/// <remarks>
/// The menu pass of 2026-11-01 (Eric: "Let's do a pass updating old menu styling to fit the rest of
/// the game, give more space and organization to menu options"). Options was the last screen still
/// in Godot's own controls - grey bars with a white disc, switches drawn as a grey dot the size of a
/// full stop, a grey dropdown - in one undivided list, while the pause tab beside it had already
/// grown sections and a gold slider. Built twice, the two had drifted: "Mute All" on one and
/// "Mute All Audio" on the other, the same three volume rows built two ways.
///
/// Everything sits in CodexRows' stone register: the sections are its headings and rules, the inks
/// are measured on the stone panel by tools/art/ui_contrast.py, and the switch and slider knob are
/// drawn by tools/art/ui_controls.py on the frames' light model. Section names are constants so a
/// screen cannot spell one its own way.
/// </remarks>
public static class SettingsRows
{
	public const string Audio = "Audio";
	public const string Display = "Display";
	public const string Gameplay = "Gameplay";
	public const string Story = "Story";
	public const string Testing = "Testing";
	public const string Feedback = "Feedback";

	/// <summary>
	/// Whether this copy shows the tester's knobs (Playtest Mode, the run logs) and honours Playtest Mode at
	/// all: an editor run or a debug export, never a release build (Eric, 2026-11-02: drop Testing from
	/// shipped builds). A player's save may still say Playtest Mode is on - it was a switch they could reach
	/// until now, and the story reset keeps settings - so a release build ignores it rather than leave a
	/// player stuck in it with no switch.
	/// </summary>
	public static bool TestingAvailable => OS.IsDebugBuild();

	private const string UiRoot = "res://assets/bonelight/ui/";

	// A label column, so the sliders and the choice start on one line down the panel rather than
	// wherever each label happens to end. Wide enough for "Sound Effects" at the phone's Body size.
	private const float LabelColumn = 170f;

	// Every row is inset by this from the section's rule, so a row's text and the focus ring round
	// it never sit hard on the panel's left edge, and a slider row lines up with a switch row.
	private const int RowInset = 8;

	/// <summary>The Audio section's rows: three volumes and the mute switch.</summary>
	public static List<Control> AudioRows(Node owner, CodexRows.Palette palette)
	{
		int master = AudioServer.GetBusIndex(AudioSettings.MasterBus);
		return new List<Control>
		{
			// "Volume" is the section's word, not each row's: the heading already says Audio.
			BuildVolumeRow(owner, "Master", AudioSettings.MasterBus, palette),
			BuildVolumeRow(owner, "Music", MusicPlayer.ResolveMusicBusName(), palette),
			BuildVolumeRow(owner, "Sound Effects", SfxPlayer.ResolveSfxBusName(), palette),
			BuildSwitch("Mute All", master >= 0 && AudioServer.IsBusMute(master), palette,
				pressed => AudioSettings.StoreMute(owner, pressed), "MuteToggle"),
		};
	}

	/// <summary>The Display section's rows: damage numbers, screen shake and the dark chapters' dark.</summary>
	public static List<Control> DisplayRows(Node owner, CodexRows.Palette palette)
	{
		return new List<Control>
		{
			// 2026-10-28, "add way to remove damage numbers from enemies". A switch says "on" by
			// itself, so the row is the thing it switches, not "Show ...".
			BuildSwitch("Damage Numbers", DisplaySettings.DamageNumbers, palette,
				pressed => DisplaySettings.StoreDamageNumbers(owner, pressed), "DamageNumbersToggle"),
			// 2026-11-02, "reduce screen shake".
			BuildSwitch("Reduce Screen Shake", DisplaySettings.ReduceShake, palette,
				pressed => DisplaySettings.StoreReduceShake(owner, pressed), "ReduceShakeToggle"),
			// 2026-11-03, the light pool: for a player who finds the dark chapters too dark.
			BuildSwitch("Reduce Darkness", DisplaySettings.ReduceDarkness, palette,
				pressed => DisplaySettings.StoreReduceDarkness(owner, pressed), "ReduceDarknessToggle"),
		};
	}

	/// <summary>
	/// A volume: its name in the label column, the bar filling the rest. Reads the bus as it stands
	/// and stores every move through AudioSettings, so a change survives the session.
	/// </summary>
	public static Control BuildVolumeRow(Node owner, string label, string busName, CodexRows.Palette palette)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 16);
		row.AddChild(BuildRowLabel(label, palette));

		// The stick reaches a slider like a button, and left and right then move it (Godot's own
		// ui_left/ui_right on a focused Range); MenuNavigator counts a focused slider as a selection.
		var slider = new HSlider
		{
			Name = busName + "VolumeSlider",
			MinValue = 0.0,
			MaxValue = 1.0,
			// Also what one press of the stick moves it by: twenty presses end to end, not a hundred.
			Step = 0.05,
			FocusMode = Control.FocusModeEnum.All,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			CustomMinimumSize = new Vector2(0, ResponsiveLayout.MinTouchTarget),
		};
		StyleSlider(slider, palette);
		slider.Value = AudioSettings.GetBusLinear(busName);
		slider.ValueChanged += value => AudioSettings.Store(owner, busName, (float)value);
		row.AddChild(slider);
		return Inset(row);
	}

	/// <summary>
	/// An on/off setting: the whole row is the button, its name on the left and the switch on the
	/// right, so a finger can land anywhere on it.
	/// </summary>
	public static CheckButton BuildSwitch(string label, bool pressed, CodexRows.Palette palette,
		Action<bool> toggled, string name = null)
	{
		var toggle = new CheckButton
		{
			Text = label,
			FocusMode = Control.FocusModeEnum.All,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		if (!string.IsNullOrEmpty(name))
			toggle.Name = name;
		ResponsiveLayout.SetFont(toggle, ResponsiveLayout.TextRole.Body);
		StyleSwitch(toggle, palette);
		ResponsiveLayout.EnsureTouchTarget(toggle);
		// Without the signal, so building a screen never writes the save.
		toggle.SetPressedNoSignal(pressed);
		if (toggled != null)
			toggle.Toggled += value => toggled(value);
		return toggle;
	}

	/// <summary>
	/// One of a few, as a row of stone buttons rather than a dropdown: every choice is in view, and
	/// a pad moves along them with left and right instead of opening a list and closing it again.
	/// The chosen one sits pressed in, with the gold inlay; the others rest unlit.
	/// </summary>
	public static Control BuildChoiceRow(string label, string[] choices, int selected, CodexRows.Palette palette,
		Action<int> chosen, out List<Button> buttons, string namePrefix = null)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 16);
		row.AddChild(BuildRowLabel(label, palette));

		var strip = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		strip.AddThemeConstantOverride("separation", 8);
		row.AddChild(strip);

		var group = new ButtonGroup();
		buttons = new List<Button>();
		for (int i = 0; i < choices.Length; i++)
		{
			int index = i;
			var button = new Button
			{
				Text = choices[i],
				ToggleMode = true,
				ButtonGroup = group,
				FocusMode = Control.FocusModeEnum.All,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};
			if (!string.IsNullOrEmpty(namePrefix))
				button.Name = namePrefix + choices[i].Replace(" ", string.Empty);
			BonelightSkin.StyleButton(button, BonelightSkin.Emphasis.Quiet);
			// Three abreast in half a PC panel: the frame's 14 a side is more than "Hardcore" can spare.
			// The iron, the inlay and the shadow inside it end 10 in at x2 (ui_frames.stone_button), so
			// 10 still clears the frame.
			foreach (string state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
			{
				if (button.GetThemeStylebox(state) is StyleBoxTexture frame)
				{
					frame.ContentMarginLeft = 10;
					frame.ContentMarginRight = 10;
				}
			}
			ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Label);
			// Pressed in is the chosen one: it reads in the heading gold, the rest in the soft ink.
			button.AddThemeColorOverride("font_color", palette.InkSoft);
			button.AddThemeColorOverride("font_pressed_color", palette.Heading);
			button.AddThemeColorOverride("font_hover_pressed_color", palette.Heading);
			button.AddThemeColorOverride("font_focus_color", palette.InkSoft);
			ResponsiveLayout.EnsureTouchTarget(button);
			button.SetPressedNoSignal(i == selected);
			button.Toggled += on =>
			{
				if (on)
					chosen?.Invoke(index);
			};
			strip.AddChild(button);
			buttons.Add(button);
		}
		return Inset(row);
	}

	/// <summary>The volume bars: the stone rule for the track, the bar gold for the filled part
	/// (CodexRows' progress bar), and the iron knob that rings in gold when it is the selection.</summary>
	public static void StyleSlider(HSlider slider, CodexRows.Palette palette)
	{
		StyleBoxFlat Track(Color colour)
		{
			var box = new StyleBoxFlat { BgColor = colour };
			box.SetCornerRadiusAll(3);
			box.ContentMarginTop = 4;
			box.ContentMarginBottom = 4;
			return box;
		}
		slider.AddThemeStyleboxOverride("slider", Track(palette.BarTrack));
		slider.AddThemeStyleboxOverride("grabber_area", Track(palette.BarFill));
		slider.AddThemeStyleboxOverride("grabber_area_highlight", Track(palette.BarFill));
		// The focus ring is MenuFocusHighlight's; Godot's own focus box would be a second one.
		slider.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		Texture2D knob = BonelightSkin.LoadTextureSafe(UiRoot + "ui-slider-knob.png");
		Texture2D knobLit = BonelightSkin.LoadTextureSafe(UiRoot + "ui-slider-knob-lit.png");
		if (knob != null)
		{
			slider.AddThemeIconOverride("grabber", knob);
			slider.AddThemeIconOverride("grabber_disabled", knob);
		}
		if (knobLit != null)
			slider.AddThemeIconOverride("grabber_highlight", knobLit);
	}

	/// <summary>A CheckButton as a settings row: no frame (the rows of a section are not buttons
	/// beside the screen's real ones), the name in the row ink, the switch from ui_controls.py.</summary>
	public static void StyleSwitch(CheckButton toggle, CodexRows.Palette palette)
	{
		StyleBoxFlat Row(float wash)
		{
			var box = new StyleBoxFlat { BgColor = new Color(palette.Reward, wash) };
			box.SetCornerRadiusAll(4);
			box.ContentMarginLeft = RowInset;
			box.ContentMarginRight = RowInset;
			box.ContentMarginTop = 6;
			box.ContentMarginBottom = 6;
			return box;
		}
		foreach (string state in new[] { "normal", "pressed", "disabled" })
			toggle.AddThemeStyleboxOverride(state, Row(0f));
		// The pointer's own feedback, under the ring MenuFocusHighlight draws.
		toggle.AddThemeStyleboxOverride("hover", Row(0.06f));
		toggle.AddThemeStyleboxOverride("hover_pressed", Row(0.06f));
		toggle.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		foreach (string state in new[] { "font_color", "font_pressed_color", "font_hover_color",
			"font_hover_pressed_color", "font_focus_color" })
			toggle.AddThemeColorOverride(state, palette.Ink);

		Texture2D off = BonelightSkin.LoadTextureSafe(UiRoot + "ui-switch-off.png");
		Texture2D on = BonelightSkin.LoadTextureSafe(UiRoot + "ui-switch-on.png");
		if (off != null && on != null)
		{
			foreach (string state in new[] { "unchecked", "unchecked_disabled", "unchecked_mirrored", "unchecked_disabled_mirrored" })
				toggle.AddThemeIconOverride(state, off);
			foreach (string state in new[] { "checked", "checked_disabled", "checked_mirrored", "checked_disabled_mirrored" })
				toggle.AddThemeIconOverride(state, on);
		}
		toggle.AddThemeConstantOverride("h_separation", 16);
	}

	/// <summary>A plain stone button inside a section (Watch the Prologue, the run logs).</summary>
	public static Button BuildActionButton(string text, string name = null)
	{
		var button = new Button
		{
			Text = text,
			FocusMode = Control.FocusModeEnum.All,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		if (!string.IsNullOrEmpty(name))
			button.Name = name;
		BonelightSkin.StyleButton(button);
		ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Label);
		ResponsiveLayout.EnsureTouchTarget(button);
		return button;
	}

	/// <summary>Buttons side by side, each an equal share of the row, inset like every other row.</summary>
	public static Control BuildButtonRow(params Button[] buttons)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 12);
		foreach (Button button in buttons)
			row.AddChild(button);
		return Inset(row);
	}

	private static Label BuildRowLabel(string text, CodexRows.Palette palette)
	{
		var label = new Label
		{
			Text = text,
			VerticalAlignment = VerticalAlignment.Center,
			CustomMinimumSize = new Vector2(LabelColumn, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		ResponsiveLayout.SetFont(label, ResponsiveLayout.TextRole.Body);
		label.AddThemeColorOverride("font_color", palette.Ink);
		return label;
	}

	private static Control Inset(Control content)
	{
		var margin = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
		margin.AddThemeConstantOverride("margin_left", RowInset);
		margin.AddThemeConstantOverride("margin_right", RowInset);
		margin.AddChild(content);
		return margin;
	}
}
