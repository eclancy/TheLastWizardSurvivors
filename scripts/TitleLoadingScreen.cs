using Godot;
using System;
using TheOneHeMissed.scripts;

/// <summary>The title's ward and fire, reused while the menu is actually being prepared.</summary>
public partial class TitleLoadingScreen : CanvasLayer
{
	private PixelArtRect farFire;
	private PixelArtRect nearFire;
	private Label caption;
	private bool failed;

	public override void _Ready()
	{
		Layer = 180; // Above the title/menu; preparation begins only after the updater finishes.
		ProcessMode = ProcessModeEnum.Always;
		var ground = new ColorRect { Color = Color.FromHtml("#05070C"), MouseFilter = Control.MouseFilterEnum.Stop };
		ground.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(ground);
		var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		ground.AddChild(center);
		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 20);
		center.AddChild(column);
		float width = Mathf.Min(720f, ResponsiveLayout.ViewportSize(ground).X - 48f);
		var art = new Control { Name = "LoadingWard", CustomMinimumSize = new Vector2(width, width * 0.5f), MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddChild(art);
		AddArt(art, "ward", 3, 1.1f);
		farFire = AddArt(art, "flames-far", 4, 7.7f);
		nearFire = AddArt(art, "flames-near", 4, 7.7f);
		farFire.Visible = nearFire.Visible = false;
		caption = new Label { Name = "LoadingCaption", Text = "Gathering your magic…", HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
		ResponsiveLayout.SetFont(caption, ResponsiveLayout.TextRole.Body);
		caption.AddThemeColorOverride("font_color", Color.FromHtml("#F2DCC4"));
		column.AddChild(caption);
		SafeArea.Apply(ground);
	}

	private static PixelArtRect AddArt(Control parent, string name, int count, float fps)
	{
		var frames = new Texture2D[count];
		for (int i = 0; i < count; i++)
			frames[i] = new AtlasTexture
			{
				Atlas = ResourceLoader.Load<Texture2D>($"res://assets/bonelight/ui/title/{name}-{i}.png"),
				Region = new Rect2(0, 520, 360, 180),
			};
		Texture2D[] animation = name == "ward" ? new[] { frames[0], frames[1], frames[2], frames[1] } : frames;
		var image = new PixelArtRect(animation, 2, 0f) { FramesPerSecond = fps, MouseFilter = Control.MouseFilterEnum.Ignore };
		image.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		parent.AddChild(image);
		return image;
	}

	public void SetProgress(float progress)
	{
		farFire.Visible = progress >= 0.35f;
		nearFire.Visible = progress >= 0.7f;
	}

	public void ShowFailure(Action retry)
	{
		failed = true;
		caption.Text = "The menu could not be loaded.";
		var button = new Button { Name = "RetryLoading", Text = "Try again", FocusMode = Control.FocusModeEnum.All };
		BonelightSkin.StyleButton(button, BonelightSkin.Emphasis.Primary);
		ResponsiveLayout.SetFont(button, ResponsiveLayout.TextRole.Body);
		caption.GetParent().AddChild(button);
		button.Pressed += retry;
		button.GrabFocus();
	}

	public override void _Input(InputEvent @event)
	{
		if (!failed) GetViewport().SetInputAsHandled();
	}
}
