using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using TheOneHeMissed.scripts;

// Captures actual runtime animation with a quiet camera and hidden HUD. All saves stay in memory.
// scenes/_DiscoveryPreview.tscn -- --mode=marks|tracks|pen --no-save-writes
public partial class _DiscoveryPreview : Node
{
	private Node2DGame game;
	private Camera2D camera;
	private Vector2 viewAt;
	private string mode;
	public override void _Ready()
	{
		SaveManager.WritesDisabled = true;
		ProcessMode = ProcessModeEnum.Always;
		mode = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--mode="))?.Substring(7) ?? "marks";
		GetNode<SaveManager>("/root/SaveManager").ReplaceInMemory(new SaveData { HasSeenGameplayOnboarding = true, HasSeenPrologue = true });
		Global.SelectedStageIdx = 9;
		Callable.From(Run).CallDeferred();
	}
	public override void _Process(double delta)
	{
		if (game == null) return;
		foreach (Enemy enemy in EnemyRegistry.Snapshot)
			if (IsInstanceValid(enemy) && !enemy.IsQueuedForDeletion()) enemy.QueueFree();
		if (camera != null) camera.GlobalPosition = viewAt;
	}
	private async Task Frames(int count)
	{
		for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}
	private async void Run()
	{
		try
		{
			game = GD.Load<PackedScene>("res://scenes/node_2d_game.tscn").Instantiate<Node2DGame>();
			AddChild(game);
			await Frames(8);
			Pinfold pen = game.Secrets.Vault as Pinfold;
			TeachingTrail trail = game.GetChildren().OfType<TeachingTrail>().Single();
			camera = GetViewport().GetCamera2D();
			camera.TopLevel = true;
			camera.PositionSmoothingEnabled = false;
			if (mode == "pen")
			{
				game.RunPlayer.GlobalPosition = pen.Entrance;
				viewAt = pen.EssenceCentre;
				camera.Zoom = Vector2.One * 0.35f;
			}
			else if (mode == "tracks")
			{
				viewAt = trail.TrackPositions[trail.TrackPositions.Count / 2];
				game.RunPlayer.GlobalPosition = viewAt + Vector2.Down * 170f;
				camera.Zoom = Vector2.One * 0.85f;
			}
			else
			{
				viewAt = pen.EssenceCentre;
				game.RunPlayer.GlobalPosition = viewAt;
				game.RunPlayer.Hide();
				pen.Hide();
				trail.PoseForProbe(viewAt);
				camera.Zoom = Vector2.One * 0.95f;
			}
			camera.GlobalPosition = viewAt;
			await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);
			GD.Print($"DISCOVERY PREVIEW {mode}, {trail.TrackPositions.Count} marks, pen {Pinfold.PoundCell}");
			for (int frame = 0; frame < 40; frame++)
			{
				foreach (CanvasLayer ui in game.FindChildren("*", "CanvasLayer", true, false).OfType<CanvasLayer>()) ui.Hide();
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				GetViewport().GetTexture().GetImage().SavePng($"{OS.GetUserDataDir()}/discovery-{mode}-{frame:00}.png");
				await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
			}
			GetTree().Quit();
		}
		catch (Exception error) { GD.PrintErr(error); GetTree().Quit(1); }
	}
}
