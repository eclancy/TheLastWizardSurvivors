using Godot;
using TheOneHeMissed.scripts;

/// <summary>
/// Walks the prologue's flow through the real menus with real (injected) key presses, and says what
/// happened at each step. A tool scene, so the save is never written (SaveManager.WritesDisabled);
/// the flag it needs is set in memory.
/// </summary>
/// <remarks>
/// <code>"$GODOT_BIN" --path . --fixed-fps 60 scenes/_PrologueProbe.tscn</code>
/// Steps: (1) a save that has not seen it - Start Run opens the prologue; (2) a tapped Enter does not
/// skip; (3) Enter held skips, on into the character select; (4) the flag is still unset, because
/// writes are off; (5) a save that has seen it - Start Run goes straight to the character select;
/// (6) Options' Watch the Prologue plays it, and Escape held returns to the main menu; (7) left alone
/// it plays through the freeing - the chain breaking, the five standing - into the character select;
/// (8, temporary, with the button) the world map's Replay
/// intro plays it, and Escape held returns to the map; (9) once a wizard is free, Options lists his
/// freeing (.ai/cutscenes.md) and plays it. It starts from a brand-new save, in memory.
/// The driver hangs off the root, so it outlives the scene changes it is watching.
/// </remarks>
public partial class _PrologueProbe : Node
{
	public override void _Ready()
	{
		SaveManager.WritesDisabled = true;
		var driver = new Driver { Name = "PrologueProbeDriver" };
		GetTree().Root.CallDeferred(Node.MethodName.AddChild, driver);
	}

	private partial class Driver : Node
	{
		private int frame;
		private int step;
		private int stepStart;
		private int failures;
		private SaveManager save;

		public override void _Ready()
		{
			save = GetNodeOrNull<SaveManager>("/root/SaveManager");
			// A brand-new save, in memory only (writes are off): the player meeting the story for the first time.
			save.ReplaceInMemory(new SaveData());
			save.Data.HasSeenPrologue = false;
			ToMenu();
		}

		private void ToMenu()
		{
			Global.OpenMenuImmediately = true;
			GetTree().ChangeSceneToFile("res://scenes/TitleScreen.tscn");
		}

		private string Scene => GetTree().CurrentScene?.SceneFilePath ?? "(none)";

		// The whole prologue untouched, in frames at 60, and a moment for the scene change after it.
		private static int PlayThroughFrames => (int)(Prologue.ReelSeconds(Prologue.PrologueReelId) * 60f) + 90;

		private bool HasPanel(string name) => GetTree().CurrentScene?.FindChild(name, true, false) != null;

		private void Check(string what, bool ok)
		{
			GD.Print($"_PrologueProbe: {(ok ? "PASS" : "FAIL")} {what} (scene {Scene})");
			if (!ok)
				failures++;
		}

		private void Press(Key key, bool pressed)
		{
			Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = pressed });
		}

		private bool PressButton(string name)
		{
			if (GetTree().CurrentScene?.FindChild(name, true, false) is not Button button)
				return false;
			button.EmitSignal(BaseButton.SignalName.Pressed);
			return true;
		}

		private void Next()
		{
			step++;
			stepStart = frame;
		}

		public override void _Process(double delta)
		{
			frame++;
			int at = frame - stepStart;
			switch (step)
			{
				case 0 when at == 60:
					Check("menu has Start Run", PressButton("StartRunButton"));
					Next();
					break;
				case 1 when at == 30:
					Check("first Start Run opens the prologue", Scene == Prologue.ScenePath);
					Press(Key.Enter, true);
					Next();
					break;
				case 2 when at == 12:
					Press(Key.Enter, false);
					Next();
					break;
				case 3 when at == 90:
					Check("a tapped Enter does not skip", Scene == Prologue.ScenePath);
					Press(Key.Enter, true);
					Next();
					break;
				case 4 when at == 75:
					Press(Key.Enter, false);
					Next();
					break;
				case 5 when at == 30:
					Check("Enter held skips into the character select", Scene == "res://scenes/CharacterSelection.tscn");
					Check("writes off: the seen flag is left unset", !save.Data.HasSeenPrologue);
					save.Data.HasSeenPrologue = true;
					ToMenu();
					Next();
					break;
				case 6 when at == 60:
					Check("menu has Start Run (second visit)", PressButton("StartRunButton"));
					Next();
					break;
				case 7 when at == 30:
					Check("seen: Start Run goes straight to the character select", Scene == "res://scenes/CharacterSelection.tscn");
					ToMenu();
					Next();
					break;
				case 8 when at == 60:
					// A wizard still held is never named: a new save has no freeing to watch (Freeings.Watchable).
					Check("a new save lists no freeing to watch", !HasPanel("WatchFreeing_tituba"));
					Check("Options has Watch the Prologue", PressButton("WatchPrologueButton"));
					Next();
					break;
				case 9 when at == 30:
					Check("Watch the Prologue opens it", Scene == Prologue.ScenePath);
					Press(Key.Escape, true);
					Next();
					break;
				case 10 when at == 75:
					Press(Key.Escape, false);
					Next();
					break;
				case 11 when at == 30:
					Check("Escape held returns to the main menu", Scene == "res://scenes/TitleScreen.tscn"
						&& GetTree().CurrentScene?.FindChild("StartRunButton", true, false) is Button { Visible: true });
					save.Data.HasSeenPrologue = false;
					Check("menu has Start Run (third visit)", PressButton("StartRunButton"));
					Next();
					break;
				// Untouched, it plays to its end (about 38 s) and goes on into story mode by itself - through the
				// freeing (.ai/cutscenes.md): after the hiding the chain breaks (22.5 s in), then the five stand (27.2 s).
				case 12 when at == 1410:
					Check("the freeing follows the hiding: the chain breaks", Scene == Prologue.ScenePath && HasPanel("Panel_break"));
					Next();
					break;
				case 13 when at == 330:
					Check("then the five stand together", HasPanel("Panel_five"));
					Next();
					break;
				case 14 when at == PlayThroughFrames - 1680:
					Check("played through, it goes on to the character select", Scene == "res://scenes/CharacterSelection.tscn");
					// TEMPORARY, with the world map's Replay intro button (WorldMap.replayIntroButton): remove
					// steps 15-18 when the button goes.
					save.Data.HasSeenPrologue = true;
					GetTree().ChangeSceneToFile("res://scenes/WorldMap.tscn");
					Next();
					break;
				case 15 when at == 60:
					Check("the world map has Replay intro", PressButton("ReplayIntroButton"));
					Next();
					break;
				case 16 when at == 30:
					Check("Replay intro opens the prologue", Scene == Prologue.ScenePath);
					Press(Key.Escape, true);
					Next();
					break;
				case 17 when at == 75:
					Press(Key.Escape, false);
					Next();
					break;
				case 18 when at == 30:
					Check("Escape held returns to the world map", Scene == "res://scenes/WorldMap.tscn");
					Check("the replay leaves the seen flag as it was", save.Data.HasSeenPrologue);
					// THE FREEINGS (.ai/cutscenes.md): once a wizard is free, Options lists his freeing to watch again.
					GlobalStatsManager.UnlockCharacter(save.Data, "tituba");
					ToMenu();
					Next();
					break;
				case 19 when at == 60:
					Check("Options lists Freed: Tituba once she is free", PressButton("WatchFreeing_tituba"));
					Next();
					break;
				case 20 when at == 30:
					Check("it plays her freeing", Scene == Prologue.ScenePath && HasPanel("Panel_freed-tituba"));
					Press(Key.Escape, true);
					Next();
					break;
				case 21 when at == 75:
					Press(Key.Escape, false);
					Next();
					break;
				case 22 when at == 30:
					Check("Escape held returns to the main menu", Scene == "res://scenes/TitleScreen.tscn");
					Check("watching it again owes nothing", Freeings.Owed(save.Data).Count == 0);
					GD.Print($"_PrologueProbe: done, {failures} failure(s)");
					GetTree().Quit(failures == 0 ? 0 : 1);
					Next();
					break;
			}
		}
	}
}
