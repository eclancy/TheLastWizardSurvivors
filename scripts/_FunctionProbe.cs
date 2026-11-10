using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TheOneHeMissed.scripts;

// Real GUI events and live familiar summons, always using an in-memory save.
// --part=menus|familiar; --portrait exercises the same pager and touch targets on a phone canvas.
public partial class _FunctionProbe : Node
{
	private int failures;
	public override void _Ready()
	{
		SaveManager.WritesDisabled = true;
		ProcessMode = ProcessModeEnum.Always;
		Callable.From(Run).CallDeferred();
	}
	private void Check(bool pass, string text)
	{
		if (!pass) failures++;
		GD.Print($"FUNCTION {(pass ? "PASS" : "FAIL")} {text}");
	}
	private async Task Frames(int count = 4)
	{
		for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}
	private async Task KeyPress(Key key, uint unicode = 0)
	{
		Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = unicode, Pressed = true });
		await Frames();
		Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = unicode, Pressed = false });
		await Frames();
	}
	private async Task Click(Control control, bool touch = false, bool move = true)
	{
		// Native Windows hover uses the hardware pointer even with parsed input events.
		if (!touch && move)
		{
			GetViewport().WarpMouse(control.GetGlobalRect().GetCenter());
			await Frames();
		}
		Vector2 at = GetViewport().GetScreenTransform() * control.GetGlobalRect().GetCenter();
		if (touch) Input.ParseInputEvent(new InputEventScreenTouch { Position = at, Index = 0, Pressed = true });
		else Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = MouseButton.Left, Pressed = true });
		await Frames();
		if (touch) Input.ParseInputEvent(new InputEventScreenTouch { Position = at, Index = 0, Pressed = false });
		else Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = MouseButton.Left, Pressed = false });
		await Frames();
	}
	private static IEnumerable<Node> Descendants(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			yield return child;
			foreach (Node nested in Descendants(child)) yield return nested;
		}
	}
	private async Task Shot(string name)
	{
		if (DisplayServer.GetName() == "headless") return;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		string path = $"{OS.GetUserDataDir()}/function-{name}-{(int)GetViewport().GetVisibleRect().Size.X}.png";
		GetViewport().GetTexture().GetImage().SavePng(path);
		GD.Print($"FUNCTION screenshot {path}");
	}
	private async void Run()
	{
		try
		{
			if (OS.GetCmdlineUserArgs().Contains("--part=familiar")) await FamiliarPack();
			else await Menus();
		}
		catch (Exception ex) { Check(false, ex.ToString()); }
		GD.Print($"FUNCTION {(failures == 0 ? "PASS" : "FAIL")} ({failures} failures)");
		GetTree().Quit(failures == 0 ? 0 : 1);
	}
	private async Task Menus()
	{
		var save = GetNode<SaveManager>("/root/SaveManager");
		save.ReplaceInMemory(new SaveData { TotalCurrency = 100000 });
		var menu = ResourceLoader.Load<PackedScene>("res://scenes/MainMenu.tscn").Instantiate<MainMenu>();
		AddChild(menu);
		await Frames(20);
		await Click(menu.FindChild("ArcaneUpgradesButton", true, false) as Button);
		await Shot("opened");
		var selector = menu.FindChild("ShopCategory", true, false) as OptionButton;
		var next = menu.FindChild("ShopNext", true, false) as Button;
		var previous = menu.FindChild("ShopPrevious", true, false) as Button;
		var back = menu.FindChild("BackFromArcaneButton", true, false) as Button;
		var pages = Descendants(menu).OfType<VBoxContainer>().Where(n => n.Name.ToString().StartsWith("ShopPage")).ToArray();
		Check(selector != null && pages.Length > 6, "Powers opened by mouse; categories have short pages");
		Rect2 view = GetViewport().GetVisibleRect();
		foreach (Control control in new Control[] { selector, next, previous, back })
			Check(view.Encloses(control.GetGlobalRect()), $"{control.Name} stays inside the viewport");
		var scroll = menu.FindChild("UpgradeScroll", true, false) as ScrollContainer;
		Check(previous.GetGlobalRect().Position.Y >= scroll.GetGlobalRect().End.Y
			&& (ResponsiveLayout.IsNarrow(menu)
				? next.GetGlobalRect().End.Y <= back.GetGlobalRect().Position.Y
				: Math.Abs(next.GlobalPosition.Y - back.GlobalPosition.Y) < 1f),
			"Powers page controls stay at the bottom, with Back below on phones or alongside on desktop");
		Check(previous.Size.Y >= 72 && next.Size.Y >= 72
			&& previous.Size.X >= 200 && next.Size.X >= 200,
			"Powers page controls have large, prominent touch targets");
		var seen = new HashSet<string>();
		for (int i = 0; i < pages.Length; i++)
		{
			var visible = pages.Where(p => p.IsVisibleInTree()).ToArray();
			Check(visible.Length == 1, "exactly one shop page is visible");
			var cards = Descendants(visible.Single()).OfType<ArcaneShopCard>().ToArray();
			// Three on a phone tall enough to show them (MainMenu.ShopCardsPerPage, in-flight 71); two elsewhere.
			int most = ResponsiveLayout.IsNarrow(menu) ? 3 : 2;
			Check(cards.Length > 0 && cards.Length <= most, $"page contains at most {most} cards");
			foreach (var card in cards) seen.Add(card.Name);
			next.GrabFocus();
			await KeyPress(Key.Enter);
			Check(next.HasFocus(), "page turn retains keyboard focus on Next");
		}
		Check(seen.Count == Descendants(menu).OfType<ArcaneShopCard>().Count(), "every upgrade and boon is reachable; Next wraps");
		GD.Print($"FUNCTION saw {seen.Count} / {Descendants(menu).OfType<ArcaneShopCard>().Count()} cards");
		// Next has wrapped back to the first page, which since in-flight 71 holds Bound Pages and then the first Powers.
		var damage = menu.FindChild("ShopCard_damage", true, false) as ArcaneShopCard;
		var boundPages = menu.FindChild("ShopCard_" + SpellBook.ShopUpgradeId, true, false) as ArcaneShopCard;
		Check(damage.IsVisibleInTree() && boundPages != null && boundPages.IsVisibleInTree(), "the first page holds Bound Pages, then the Powers");
		int purse = save.Data.TotalCurrency;
		Vector2 parked = GetViewport().GetScreenTransform() * damage.Plate.GetGlobalRect().GetCenter();
		GetViewport().WarpMouse(damage.Plate.GetGlobalRect().GetCenter());
		Input.ParseInputEvent(new InputEventMouseMotion { Position = parked, GlobalPosition = parked, Relative = new Vector2(8, 0) });
		await Frames();
		damage.FocusStop.GrabFocus();
		await KeyPress(Key.Enter);
		Check(save.Data.ArcaneUpgradeLevels.GetValueOrDefault("damage") == 1 && save.Data.TotalCurrency < purse, "Enter buys the focused upgrade");
		Check(damage.FocusStop.HasFocus(), "buying retains card focus");
		Check(!MenuFocusHighlight.PointerActive && Input.MouseMode == Input.MouseModeEnum.Hidden, "keyboard hides cursor and switches off pointer state");
		Check(damage.Plate.MouseFilter == Control.MouseFilterEnum.Ignore, "keyboard disables stale mouse hover and tooltips");
		int level = save.Data.ArcaneUpgradeLevels["damage"];
		await Click(damage.Plate, move: false);
		Check(save.Data.ArcaneUpgradeLevels["damage"] == level + 1, "first mouse click restores pointer and buys immediately");
		Check(MenuFocusHighlight.PointerActive && damage.Plate.MouseFilter != Control.MouseFilterEnum.Ignore, "mouse restores original filters");
		await KeyPress(Key.Right);
		var edit = new LineEdit { Name = "InputTest", Position = new Vector2(30, 30), Size = new Vector2(200, 50) };
		AddChild(edit);
		await Frames();
		Check(edit.MouseFilter == Control.MouseFilterEnum.Ignore, "new controls also ignore a stale pointer");
		edit.GrabFocus();
		await KeyPress(Key.X, 'x');
		Check(edit.Text == "x", "keyboard text entry still works");
		var holder = new Control();
		AddChild(holder);
		edit.Reparent(holder);
		await Frames();
		Input.ParseInputEvent(new InputEventMouseMotion { Relative = new Vector2(1, 0) });
		await Frames();
		Check(!MenuFocusHighlight.PointerActive, "mouse jitter does not change input mode");
		Input.ParseInputEvent(new InputEventMouseMotion { Relative = new Vector2(8, 0) });
		await Frames();
		Check(MenuFocusHighlight.PointerActive && edit.MouseFilter != Control.MouseFilterEnum.Ignore, "intentional motion restores a reparented control");
		holder.QueueFree();
		Input.ParseInputEvent(new InputEventJoypadButton { ButtonIndex = JoyButton.DpadRight, Pressed = true });
		await Frames();
		Input.ParseInputEvent(new InputEventJoypadButton { ButtonIndex = JoyButton.DpadRight, Pressed = false });
		Check(!MenuFocusHighlight.PointerActive, "controller uses the same navigation mode");
		await Click(previous, OS.GetCmdlineUserArgs().Contains("--portrait"));
		Check(MenuFocusHighlight.PointerActive, "mouse or touch returns from controller navigation");
		await Shot("powers");
		selector.Select(0);
		selector.EmitSignal(OptionButton.SignalName.ItemSelected, 0L);
		await Frames();
		await Shot("power");
		selector.GrabFocus();
		await KeyPress(Key.Enter);
		await KeyPress(Key.Down);
		await KeyPress(Key.Enter);
		Check(selector.GetItemText(selector.Selected) == "Survival", "keyboard opens category menu and selects Survival");
		Check(pages.Single(p => p.IsVisibleInTree()).GetChildren().OfType<GridContainer>().Single()
			.GetChildren().OfType<ArcaneShopCard>().Any(c => c.Name == "ShopCard_vitality"), "category choice displays its upgrades");
	}
	private async Task FamiliarPack()
	{
		var data = new SaveData();
		data.UnlockedSpellIds.Add("familiar");
		data.ArcaneUpgradeLevels["amount"] = 5;
		GetNode<SaveManager>("/root/SaveManager").ReplaceInMemory(data);
		Global.SelectedCharacterIdx = CharacterRoster.IndexOf("archmage");
		var arena = new Node2D();
		AddChild(arena);
		var player = ResourceLoader.Load<PackedScene>("res://scenes/player.tscn").Instantiate<Player>();
		arena.AddChild(player);
		player.Position = new Vector2(600, 350);
		Check(player.TryAddOrLevelSpell("familiar:" + Familiar.DogId), "hound form can be equipped through the real player flow");
		var spell = player.GetEquippedSpells().Single(s => s.Id == "familiar");
		var fire = typeof(Player).GetMethod("FireFamiliar", BindingFlags.NonPublic | BindingFlags.Instance);
		var familiarScene = ResourceLoader.Load<PackedScene>("res://scenes/Familiar.tscn");
		void Summon() => fire.Invoke(player, new object[] { spell, familiarScene });
		int Count() => Descendants(arena).OfType<Familiar>().Single().GetChildren().OfType<AnimatedSprite2D>().Count();
		spell.CurrentLevel = 3;
		Summon();
		await Frames();
		Check(Count() == 1, "level 3 has one hound despite +5 Amount");
		spell.CurrentLevel = 8;
		spell.ApplyEvolution(spell.Level4Options.Single(o => o.Id == Familiar.GreatHoundId));
		spell.ApplyEvolution(spell.Level8Options.Single(o => o.Id == Familiar.CerberusId));
		Summon();
		await Frames();
		Check(Count() == 1, "Great Hound and Cerberus never add companions");
		var enemy = ResourceLoader.Load<PackedScene>("res://scenes/enemy.tscn").Instantiate<Enemy>();
		enemy.Speed = 0;
		enemy.Health = 100000;
		arena.AddChild(enemy);
		enemy.Position = player.Position + new Vector2(90, 0);
		int hp = enemy.Health;
		for (int i = 0; i < 5; i++)
		{
			Summon();
			await ToSignal(GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);
			player.CurrentHP = player.MaxHP;
		}
		Check(enemy.Health < hp, $"single hound lands live bites ({hp - enemy.Health} damage)");
		spell.ApplyEvolution(spell.Level4Options.Single(o => o.Id == Familiar.LittermatesId));
		Summon();
		await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
		Check(Count() == 3, "Littermates adds exactly two pups after Ascension");
		await Shot("littermates");
		hp = enemy.Health;
		for (int i = 0; i < 5; i++)
		{
			Summon();
			await ToSignal(GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);
			player.CurrentHP = player.MaxHP;
		}
		Check(enemy.Health < hp, $"pack lands live bites ({hp - enemy.Health} damage)");
		var sprites = Descendants(arena).OfType<Familiar>().Single().GetChildren().OfType<AnimatedSprite2D>().ToArray();
		Check(sprites.Skip(1).All(s => s.SpriteFrames != sprites[0].SpriteFrames), "only main hound has the Ascension look; companions are pups");
	}
}
