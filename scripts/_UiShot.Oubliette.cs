using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOneHeMissed.scripts;

// THE OUBLIETTE WALK: --secret-probe=oubliette on the Cursed Dungeon (--stage=1). D7 is not a vault, so the vault
// walks do not take it; this walks to it and works it the way a player would, checking what .ai/endgame-castle.md
// 4.4 and decision 11 promise:
//
//   placed five to nine chunks out (SecretPlacement.Mid) on a fixed seed, in a dead end (walls on three sides), never
//   a target and never solid; not seen before he has been near it and seen from its mouth; the far tell (puddles,
//   rimed nearest) thicker within its near band than beyond; on the grate, the count running; a blow (TakeDamage)
//   starting it again, and stepping off doing the same; then five seconds untouched - the voice below if the one
//   below is still held, none if she is free - and after it the find: on the save, its deed done, and Nimue's card
//   turned (GlobalStatsManager.IsCharacterRevealed) while she stays locked. No chest, no Arcane Energy.
//
// Run it on a fresh save (the reveal itself), with --open-stages=stage_13 (already revealed by the Throne: the voice,
// the page, nothing turned twice) and with --free-wizards=nimue (free: no voice, "No one is held here now"). PASS /
// FAIL lines, exit code = failures, oubliette-N.png in the user folder at each beat (under a window). Writes are
// off: the save is in memory.
public partial class _UiShot
{
	private bool secretProbeOubliette;
	private int oublietteStage = -1;
	private bool oublietteStaged;

	// --oubliette-stage=N, a third of the way in: the count posed for a photograph (Oubliette.PoseForProbe).
	private void PoseOubliette()
	{
		if (oublietteStage < 0 || oublietteStaged || frames < WarmupFrames / 3
			|| FindNodeOfType<SecretDirector>(GetTree().Root)?.PlacedSite(SecretCatalog.OublietteId) is not Oubliette site)
			return;
		oublietteStaged = true;
		site.PoseForProbe(oublietteStage);
		GD.Print($"_UiShot: the Oubliette posed at stage {oublietteStage} ({site.Held:0.00} s held, answered {site.Answered})");
	}

	private partial class OublietteWalk : Node
	{
		public SecretDirector Director;
		public Player Hero;
		private Oubliette site;
		private SaveData save;
		private string keptId = "";
		private bool revealedBefore;
		private bool heldBefore;
		private bool foundBefore;
		private int currencyBefore;
		private int chestsBefore;
		private int failures;
		private int shots;
		private int step;
		private float wait;
		private float clock;
		private float onSince;
		private int strikes;
		private Vector2 stand;
		private Vector2? pin;

		private void Check(string what, bool ok)
		{
			if (!ok)
				failures++;
			GD.Print($"OUBLIETTE {(ok ? "PASS" : "FAIL")} {what}");
		}

		private void Shot(string what)
		{
			shots++;
			if (DisplayServer.GetName() == "headless")
				return;
			Image image = GetViewport().GetTexture().GetImage();
			string path = OS.GetUserDataDir() + $"/oubliette-{shots}.png";
			image.SavePng(path);
			GD.Print($"OUBLIETTE shot {path} - {what}");
		}

		private void Finish()
		{
			GD.Print($"OUBLIETTE {(failures == 0 ? "ALL PASS" : $"{failures} FAILED")}");
			GetTree().Quit(failures);
			step = -1;
		}

		private void StandFeetAt(Vector2 foot)
		{
			Hero.GlobalPosition = foot - (Hero.FootPosition - Hero.GlobalPosition);
			pin = Hero.GlobalPosition;
		}

		private static void Release()
		{
			foreach (string action in new[] { "move_left", "move_right", "move_up", "move_down" })
				Input.ActionRelease(action);
		}

		// The run's swarm cleared every frame (nothing touches him but the probe's own blow), his health kept full,
		// and where he was put he stays.
		private void Hold()
		{
			Release();
			foreach (Enemy e in EnemyRegistry.Snapshot)
				if (e != null && IsInstanceValid(e))
					e.QueueFree();
			Hero.MaxHP = Math.Max(Hero.MaxHP, 20);
			Hero.CurrentHP = Hero.MaxHP;
			if (pin is Vector2 at)
				Hero.GlobalPosition = at;
		}

		private static bool DismissLevelUp(Node root)
		{
			if (root is LevelUpMenu menu && menu.Visible)
			{
				IReadOnlyList<LevelUpOption> options = menu.GetOfferedOptions();
				if (options != null && options.Count > 0)
					menu.EmitSignal(LevelUpMenu.SignalName.WeaponSelected, options[0].AutoChoice());
				return true;
			}
			foreach (Node child in root.GetChildren())
				if (DismissLevelUp(child))
					return true;
			return false;
		}

		public override void _Ready() => ProcessMode = ProcessModeEnum.Always;

		private float BearingOf(Vector2 p) => (p - Director.Game.StageOrigin).Angle();

		public override void _Process(double delta)
		{
			if (step < 0)
				return;
			if (GetTree().Paused)
			{
				DismissLevelUp(GetTree().Root);
				return;
			}
			float dt = (float)delta;
			clock += dt;
			Hold();
			if (wait > 0f)
			{
				wait -= dt;
				return;
			}
			switch (step)
			{
				case 0:
				{
					site = Director.PlacedSite(SecretCatalog.OublietteId) as Oubliette;
					save = Director.GetNodeOrNull<SaveManager>("/root/SaveManager")?.Data;
					Check("the Oubliette was placed", site != null && save != null && Director.Mid == site);
					if (site == null || save == null)
					{
						Finish();
						return;
					}
					keptId = Oubliette.KeptBelow(site.SecretId)?.Id ?? "";
					revealedBefore = GlobalStatsManager.IsCharacterRevealed(save, keptId);
					heldBefore = !GlobalStatsManager.IsCharacterUnlocked(save, keptId);
					foundBefore = save.HasFoundSecret(site.SecretId);
					currencyBefore = save.TotalCurrency;
					Vector2 at = site.GlobalPosition;
					float chunks = at.DistanceTo(Director.Game.StageOrigin) / 1536f;
					GD.Print($"OUBLIETTE at {at}, {chunks:0.0} chunks out, bearing {Mathf.RadToDeg(BearingOf(at)):0} deg, its mouth {site.Mouth}; "
						+ $"turns '{keptId}' (revealed before: {revealedBefore}, held: {heldBefore}, found before: {foundBefore})");
					Check($"it turns Nimue's card (KeptBelow '{keptId}')", keptId == "nimue");
					Check($"it is five to nine chunks out ({chunks:0.00})", chunks >= SecretDirector.MidMinChunks - 0.3f && chunks <= SecretDirector.MidMaxChunks + 0.3f);
					// Clear of the vault and of the event, by bearing: 90 degrees where the circle leaves room.
					var others = new List<(string Name, float Bearing)>();
					if (Director.Vault != null)
						others.Add(("the vault", BearingOf(Director.Vault.GlobalPosition)));
					if (Director.Game.GetNodeOrNull<SideEventDirector>("SideEventDirector") is SideEventDirector events)
						foreach (SideEvent e in events.Events.Where(e => e != null && e.Sites.Count > 0))
							others.Add(("the side event", BearingOf(e.Sites[0].Position)));
					foreach (var (name, bearing) in others)
					{
						float gap = Mathf.RadToDeg(Mathf.Abs(Mathf.AngleDifference(BearingOf(at), bearing)));
						GD.Print($"OUBLIETTE {gap:0} deg round from {name}");
						Check($"clear of {name} by bearing ({gap:0} deg; 90 wanted, less only when two others leave no such gap)",
							gap >= 89f || others.Count >= 2);
					}
					Check("it stands in a dead end: walls on three sides of its cell", Oubliette.DeadEndAt(Director.Game, at) != null && site.Mouth != Vector2.Zero);
					Check("it is never a target: not in \"enemies\", nor anything in it",
						!site.IsInGroup("enemies") && !site.FindChildren("*", "", true, false).Any(n => n.IsInGroup("enemies")));
					Check("it is not solid ground: the horde walks over the grate", Director.Game.Terrain?.IsFenceAtWorld(at) != true);
					Check("not seen before he has been near it", !Director.IsGlimpsed(site.SecretId));
					// One cell out of the dead end, in the corridor its mouth opens on.
					stand = at + site.Mouth * LevelGenerator.MazeCellTiles * Director.Game.ProceduralTilePixels;
					step = 1;
					return;
				}
				case 1:
				{
					// A third of a chunk a step, every few frames, so the ground streams in round him.
					Vector2 to = stand - Hero.GlobalPosition;
					float left = to.Length();
					float toTell = Hero.GlobalPosition.DistanceTo(site.TellPosition);
					if (toTell > SecretDirector.SeenRadius + 50f && toTell <= SecretDirector.SeenRadius + 562f)
						Check($"not seen at {toTell:0} px from the grate", !Director.IsGlimpsed(site.SecretId));
					Hero.GlobalPosition += left <= 512f ? to : to / left * 512f;
					pin = null;
					wait = 0.1f;
					if (left <= 512f)
					{
						StandFeetAt(stand);
						step = 2;
						wait = 1.5f;
					}
					return;
				}
				case 2:
				{
					Check($"seen from its mouth ({Hero.GlobalPosition.DistanceTo(site.TellPosition):0} px from the grate)", Director.IsGlimpsed(site.SecretId));
					Check("and the seen mark is on the save", save.HasGlimpsedSecret(site.SecretId));
					int inner = 0, outer = 0, near = 0, nearFar = 0;
					foreach (Node n in GetTree().GetNodesInGroup(SecretFarTell.Group))
					{
						if (n is not Node2D s) continue;
						float d = s.GlobalPosition.DistanceTo(site.GlobalPosition);
						if (d <= 1024f) inner++;
						else if (d <= 2304f) outer++;
					}
					foreach (Node n in GetTree().GetNodesInGroup(SecretFarTell.GroupNear))
					{
						if (n is not Node2D s) continue;
						if (s.GlobalPosition.DistanceTo(site.GlobalPosition) <= 1024f) near++;
						else nearFar++;
					}
					GD.Print($"OUBLIETTE far tell: {inner} puddles within 1024 px (+{near} rimed), {outer} from 1024 to 2304 px (+{nearFar} rimed)");
					// Per area: the disc within 1024 (less the dead end) is about a quarter of the annulus out to 2304.
					Check("the far tell thickens toward it (density within 1024 px > from there to 2304)",
						inner + near > 0 && (inner + near) / 1f > (outer + nearFar) / 4f);
					Check("its rimed pieces are only near", near > 0 && nearFar == 0);
					Check($"the count has not started off the grate ({site.Held:0.00} s)", site.Held <= 0f);
					Shot("at the mouth of the dead end");
					step = 3;
					return;
				}
				case 3:
				{
					// Onto the grate: his feet put there and held (the tool's kiting bot owns the movement keys, so a
					// probe that steered by them would be walked back toward the start - VaultWalk's SteerTo can, this
					// cannot, since it arrives from far out). The site reads the feet, as it does in play.
					StandFeetAt(site.GlobalPosition);
					GD.Print($"OUBLIETTE on the grate (feet {Hero.FootPosition.DistanceTo(site.GlobalPosition):0} px from its middle; probe clock {clock:0.0} s)");
					onSince = clock;
					step = 4;
					wait = 2.5f;
					return;
				}
				case 4:
				{
					Check($"on the grate the count runs ({site.Held:0.00} s after 2.5)", site.Held >= 2.0f && site.Held <= 3.0f && !site.Answered);
					Shot("on the grate, the draught thickening");
					// A blow (Player.TakeDamage, through the dodge roll: a dodged one raises nothing, so strike again).
					Hero.TakeDamage(1);
					strikes++;
					step = 5;
					return;
				}
				case 5:
				{
					if (site.Held > 0.2f && strikes < 6)
					{
						Hero.TakeDamage(1);
						strikes++;
						return;
					}
					Check($"a blow starts it again ({site.Held:0.00} s held after {strikes} blow(s))", site.Held <= 0.2f);
					// Stepping off: the feet a cell's quarter toward the mouth, then back.
					StandFeetAt(site.GlobalPosition + site.Mouth * 110f);
					step = 6;
					wait = 1.2f;
					return;
				}
				case 6:
				{
					Check($"off the grate it does not count ({site.Held:0.00} s)", site.Held <= 0f);
					StandFeetAt(site.GlobalPosition);
					onSince = clock;
					step = 7;
					return;
				}
				case 7:
				{
					// Five seconds untouched.
					if (!site.Answered)
					{
						if (clock - onSince > 8f)
						{
							Check("five seconds untouched on the grate answered it", false);
							Finish();
						}
						return;
					}
					float took = clock - onSince;
					Check($"five seconds untouched answered it ({took:0.00} s)", took >= HoldSecondsFloor && took <= Oubliette.HoldSeconds + 0.6f);
					Check(heldBefore ? "the voice below rose (she is still held)" : "no voice (she is free)", site.VoicePlayed == heldBefore);
					Check("the find waits the voice out (not paid yet)", !site.Paid || !heldBefore);
					chestsBefore = Director.ChestsPaid;
					step = 8;
					wait = 1.0f;
					return;
				}
				case 8:
				{
					Shot(heldBefore ? "the voice rising up the shaft" : "the grate, no voice");
					step = 9;
					return;
				}
				case 9:
				{
					if (!site.Paid)
					{
						if (clock - onSince > 16f)
						{
							Check("the find was paid after the voice", false);
							Finish();
						}
						return;
					}
					Check("it is found this run", Director.FoundThisRun(site.SecretId));
					Check("it is found on the save", save.HasFoundSecret(site.SecretId));
					Check("its deed is recorded", GlobalStatsManager.IsAchievementUnlocked(save, AchievementDefinitions.SecretDeedId(site.SecretId)));
					Check($"'{keptId}' is revealed now (her card is a silhouette with her hint)", GlobalStatsManager.IsCharacterRevealed(save, keptId));
					Check($"and only revealed: still {(heldBefore ? "locked" : "free, as before")}",
						GlobalStatsManager.IsCharacterUnlocked(save, keptId) == !heldBefore);
					Check($"no Arcane Energy ({currencyBefore} -> {save.TotalCurrency})", save.TotalCurrency == currencyBefore);
					Check($"no chest ({chestsBefore} -> {Director.ChestsPaid})", Director.ChestsPaid == chestsBefore);
					if (!revealedBefore)
						GD.Print("OUBLIETTE her card turned: it was 'Not yet discovered.' and is her hint now");
					step = 10;
					wait = 0.8f;
					return;
				}
				case 10:
					Shot("the find's toast");
					step = 11;
					wait = 0.5f;
					return;
				case 11:
					Finish();
					return;
			}
		}

		// The count begins on the first frame his feet are on the grate; a frame's slack either way.
		private const float HoldSecondsFloor = Oubliette.HoldSeconds - 0.1f;
	}
}
