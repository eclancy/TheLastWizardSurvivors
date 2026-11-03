using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// --number-swatch: every damage number's ink on one screen (DamageNumberInk; .ai/art-direction.md, "Damage
// numbers wear the element"). From the run's first frames the wizard is held still and silent (left to
// fight, he kills something, and the deed it announces covers the swatch), the field is kept clear, and
// the chapter's own enemies are stood, held, in four rows of seven round him. Give it --frames=900, so
// the chapter's opening card has gone by the shutter.
// Just before the shutter (--fx-lead frames) each is struck once through the real hit path,
// Player.DealDamageToEnemy, with a spell of one element as the source - so what is photographed is the
// spell-to-element-to-ink chain itself, not a table of colours:
//
//   row 1  Fire, Ice, Arcane, Darkness, Light, Grass, Earth        ordinary hits
//   row 2  Wind, Lightning, Poison, Metal, Water, no element, a turned enemy's blow
//   rows 3 and 4  the same, every hit a crit (no crit for the turned blow, which never has one)
//
// Each number rises from the body it struck, so it is read over a body and the chapter's ground both.
// Prints SWATCH lines naming what stood where. Run it on a dark, a bright and the paper chapter:
//   "$GODOT_BIN" --path . scenes/_UiShot.tscn -- --scene=res://scenes/node_2d_game.tscn --stage=8
//       --number-swatch --shot=swatch-sketchbook --resolution 1280x720
public partial class _UiShot
{
	private bool numberSwatch;
	private NumberSwatch swatch;

	private void StartNumberSwatch()
	{
		if (FindNodeOfType<Node2DGame>(GetTree().Root) is not Node2DGame game
			|| FindNodeOfType<Player>(GetTree().Root) is not Player hero)
			return;
		swatch = new NumberSwatch { Name = "NumberSwatch", Game = game, Hero = hero };
		GetTree().Root.AddChild(swatch);
	}

	private partial class NumberSwatch : Node
	{
		public Node2DGame Game;
		public Player Hero;
		public bool Struck { get; private set; }

		// One spell of each element, the source its hits are credited to - a pure spell, so the element is
		// unambiguous. Templates, read only: DealDamageToEnemy reads their numbers and their element.
		private static readonly (Element Element, string Path)[] Sources =
		{
			(Element.Fire, "res://SpellData_Fireball.tres"),
			(Element.Ice, "res://SpellData_ConeOfCold.tres"),
			(Element.Arcane, "res://SpellData_ArcaneExplosion.tres"),
			(Element.Darkness, "res://SpellData_HollowStar.tres"),
			(Element.Light, "res://SpellData_Starfall.tres"),
			(Element.Grass, "res://SpellData_BrambleSeed.tres"),
			(Element.Earth, "res://SpellData_ObsidianSpike.tres"),
			(Element.Wind, "res://SpellData_CycloneSlash.tres"),
			(Element.Lightning, "res://SpellData_ChainLightning.tres"),
			(Element.Poison, "res://SpellData_Contagion.tres"),
			(Element.Metal, "res://SpellData_IronPalisade.tres"),
			(Element.Water, "res://SpellData_Riptide.tres"),
		};

		// A slot is a spell's element, "none" (a hit with no element) or "turned" (a turned enemy's blow).
		private readonly List<(Enemy Body, string Slot, SpellData Source, bool Crit)> bodies = new();
		private readonly HashSet<ulong> keep = new();

		public override void _Ready()
		{
			Hero.SetHeldForBossEntrance(true);
			Place();
		}

		public override void _Process(double delta)
		{
			if (!IsInstanceValid(Hero))
				return;
			Hero.MaxHP = Math.Max(Hero.MaxHP, 100000);
			Hero.CurrentHP = Hero.MaxHP;
			Hero.CurrentXP = 0;
			Hero.SetHeldForBossEntrance(true);
			foreach (Enemy e in EnemyRegistry.Snapshot)
				if (IsInstanceValid(e) && !keep.Contains(e.GetInstanceId()))
					e.QueueFree();
			// What the numbers cost the renderer: the last drawn frame's draw calls, either side of the strike
			// (0 headless). The difference is what 27 numbers on screen at once add.
			if (Struck && ++framesSinceStrike <= 4)
				GD.Print($"SWATCH draws {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)} {framesSinceStrike} frame(s) after the strike");
		}

		private int framesSinceStrike;

		private void Place()
		{
			Viewport viewport = Hero.GetViewport();
			Camera2D camera = viewport.GetCamera2D();
			Vector2 zoom = camera != null ? camera.Zoom : Vector2.One;
			Vector2 half = viewport.GetVisibleRect().Size / zoom * 0.5f;
			// Seven across inside the narrower of the two canvases (a phone's 847 world units, where a crit's
			// "38!" must still clear the edge), four rows clear of the PC's HUD band.
			float column = Mathf.Min(115f, half.X * 2f * 0.72f / 6f);
			float row = Mathf.Min(130f, half.Y * 0.55f);
			var slots = new List<(string Slot, SpellData Source)>();
			foreach (var (element, path) in Sources)
				slots.Add((element.ToString(), GD.Load<SpellData>(path)));
			slots.Insert(12, ("none", null));
			slots.Insert(13, ("turned", null));
			Vector2 c = Hero.GlobalPosition;
			for (int crit = 0; crit < 2; crit++)
			{
				for (int i = 0; i < slots.Count; i++)
				{
					if (crit == 1 && slots[i].Slot == "turned")
						continue;
					int r = crit * 2 + i / 7;
					int k = i % 7;
					Vector2 at = c + new Vector2((k - 3) * column, (r - 1.5f) * row);
					Enemy e = Game.SpawnEventEnemy((string)null, at, 40f, false);
					if (e == null)
						continue;
					// Stood on its slot even where the ground is a wall (the Dungeon's maze moves a spawn out
					// of one): held, it never steps, and a number over a wall is a ground worth reading on too.
					e.GlobalPosition = at;
					e.SetHeld(true);
					keep.Add(e.GetInstanceId());
					bodies.Add((e, slots[i].Slot, slots[i].Source, crit == 1));
				}
			}
			GD.Print($"SWATCH placed {bodies.Count} bodies round {c}, {column:0} x {row:0} apart (half view {half.X:0}x{half.Y:0} world)");
		}

		/// <summary>Strikes every body once, now. The numbers it throws are what the shutter photographs.</summary>
		public void Strike()
		{
			if (Struck)
				return;
			Struck = true;
			GD.Print($"SWATCH draws {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)} before the strike");
			foreach (var (body, slot, source, crit) in bodies)
			{
				if (!IsInstanceValid(body) || body.IsDying)
					continue;
				// Unheld for the hit alone: a held enemy takes no damage (Enemy.ApplyHit), and held again at
				// once so it does not step toward him before the shutter.
				body.SetHeld(false);
				if (slot == "turned")
					body.TakeTurnedBlow(17);
				else
					Hero.DealDamageToEnemy(body, 24, crit ? 10f : -10f, false, source);
				body.SetHeld(true);
				GD.Print($"SWATCH {slot}{(crit ? " crit" : "")} at {body.GlobalPosition - Hero.GlobalPosition} ink {(slot == "turned" ? "turned" : DamageNumberInk.For(source?.GetNumberElement(), crit).ToHtml(false))}");
			}
		}
	}
}
