using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

// BOSSES A CHAPTER SENDS ON ITS OWN SCHEDULE (piece C4, the Throne's procession, .ai/endgame-castle.md 6.3
// and 11.5).
//
// The seven ChapterRules hooks let a chapter ANSWER the run - where things go, when the pace is. The
// procession needs to TELL it: send these two now, while the household guard keeps coming, and this one
// last, whose fall is the win. That is machinery any chapter could use, so it lives here, in the run, and
// the Throne's data and schedule stay in ThroneRules.cs and ThroneProcession.cs (the Muster's granted levels
// are Node2DGame.GrantedLevels.cs). Nothing here names a chapter.
//
// SENT BOSSES are not the boss phase. That phase stops the clock, the trickle, the elites and the chests
// for one boss (bossFightActive); a sent boss stops none of it, because the Throne's XP to the Apotheosis
// is killed for during the procession (6.6). What a sent boss does share with a chapter's boss: its scene,
// its health on the balance preset, its arrival with the field held (BeginBossEntranceHold, which counts
// every arriving body), its bar under the timer and its badge at the screen's edge. Two at once get two
// bars side by side in the boss bar's own slot (bossHudRoot, so the banners already sit under it). One
// sent as a FINALE wins the run when it falls, through the same CompleteBossVictory a chapter's boss
// does: the chest where it fell, the music out, the way home at hook 5.
public partial class Node2DGame
{
	private sealed class SentBoss
	{
		public BossEnemy Body = null!;
		public BossDefinition Definition = null!;
		public bool Finale;
		public Vector2 LastPosition;
		public ProgressBar? Bar;
		public WorldPointer? Pointer;
	}

	private readonly List<SentBoss> sentBosses = new();

	/// <summary>
	/// A sent boss that was not the finale has fallen: its definition and where it fell. Raised once per
	/// boss, also for one that left the tree without its defeat (with a warning), so a schedule can never
	/// stall on a body that is gone.
	/// </summary>
	public event Action<BossDefinition, Vector2>? SentBossFell;

	/// <summary>The sent bosses still standing (arriving or fighting), for the rules and the tools.</summary>
	public IReadOnlyList<BossEnemy> SentBossesStanding =>
		sentBosses.Where(s => IsInstanceValid(s.Body) && !s.Body.IsDying).Select(s => s.Body).ToList();

	/// <summary>
	/// Sends a boss at <paramref name="at"/> with <paramref name="health"/> (before the balance preset,
	/// which it rides as every boss does), outside the boss phase: the clock, the trickle and the swarm go
	/// on. With <paramref name="finale"/> its fall wins the run. Null if it could not be sent (the reason
	/// is logged).
	/// </summary>
	public BossEnemy? SendBoss(BossDefinition definition, int health, Vector2 at, bool finale)
	{
		if (definition == null || runFinished || player == null || !IsInstanceValid(player))
			return null;

		var scene = ResourceLoader.Load<PackedScene>(definition.SpawnScenePath);
		if (scene == null)
		{
			GD.PushError($"Node2DGame: boss scene not found: {definition.SpawnScenePath}");
			return null;
		}
		if (scene.Instantiate() is not BossEnemy boss)
		{
			GD.PushError($"Node2DGame: {definition.SpawnScenePath} does not have a BossEnemy script attached.");
			return null;
		}

		boss.BossId = definition.Id;
		boss.BossDisplayName = definition.DisplayName;
		boss.Health = Mathf.Max(1, Mathf.RoundToInt(health * presetElitePowerScale));
		boss.Position = at;
		var sent = new SentBoss { Body = boss, Definition = definition, Finale = finale, LastPosition = at };
		boss.Connect(BossEnemy.SignalName.BossDefeated, Callable.From<string>(_ => OnSentBossDefeated(sent)));
		AddChild(boss);
		sentBosses.Add(sent);
		totalEnemiesSpawned++;
		RebuildSentBossHud();
		// The field holds while it arrives, as for any boss (Node2DGame.BossEntrance.cs).
		BeginBossEntranceHold();
		GD.Print($"Node2DGame: sent {definition.Id} with {boss.Health} HP{(finale ? " (the finale)" : string.Empty)} "
			+ $"at real {timeElapsed:0}s, level {player.CurrentLevel}");
		return boss;
	}

	/// <summary>A chest set out where a boss fell, as a chapter's boss's is (between the body and the player).</summary>
	public void PlaceChestWhereBossFell(Vector2 fellAt) => PlaceChestNear(BossChestPoint(fellAt), ChestSourceBoss);

	/// <summary>No trickle for at least <paramref name="seconds"/> (real): a moment to reach a chest.</summary>
	public void HoldTrickle(float seconds) => spawnGraceRemaining = Mathf.Max(spawnGraceRemaining, seconds);

	/// <summary>A line across the top of the run, in the boss banner's style and place.</summary>
	public void ShowChapterBanner(string text) => ShowRunBanner(text, new Color(1.0f, 0.86f, 0.72f, 1.0f), 3.6f);

	private void OnSentBossDefeated(SentBoss sent)
	{
		if (!sentBosses.Remove(sent) || runFinished)
			return;
		Vector2 fellAt = IsInstanceValid(sent.Body) ? sent.Body.GlobalPosition : sent.LastPosition;
		FreeSentPointer(sent);

		if (sent.Finale)
		{
			// The run is won the way a chapter's boss wins it. The field stands still from here until the
			// player leaves (bossFightActive stays up, as it does after any boss), and CompleteBossVictory
			// reads the body for where it fell and frees the bars with bossHudRoot.
			foreach (SentBoss other in sentBosses)
				FreeSentPointer(other);
			sentBosses.Clear();
			bossFightActive = true;
			activeBossDefinition = sent.Definition;
			bossInstance = IsInstanceValid(sent.Body) ? sent.Body : null;
			CompleteBossVictory(sent.Definition.Id);
			return;
		}

		RebuildSentBossHud();
		GD.Print($"Node2DGame: {sent.Definition.Id} fell at real {timeElapsed:0}s, level {player?.CurrentLevel ?? 0}");
		SentBossFell?.Invoke(sent.Definition, fellAt);
	}

	private void FreeSentPointer(SentBoss sent)
	{
		if (sent.Pointer != null && IsInstanceValid(sent.Pointer))
			sent.Pointer.QueueFree();
		sent.Pointer = null;
	}

	// The bars: one per standing sent boss, side by side in the boss bar's slot. Rebuilt whenever one is
	// sent or falls, so a pair's survivor takes the whole width.
	private void RebuildSentBossHud()
	{
		if (bossFightActive)
			return;
		bossHudRoot?.QueueFree();
		bossHudRoot = null;
		bossHudBar = null;
		bossHudLabel = null;
		foreach (SentBoss s in sentBosses)
			s.Bar = null;
		if (sentBosses.Count == 0)
			return;

		var uiOverlay = GetNodeOrNull<CanvasLayer>("UIOverlay");
		if (uiOverlay == null)
			return;

		bossHudRoot = new Control
		{
			Name = "SentBossHud",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			AnchorLeft = 0f,
			AnchorRight = 1f,
			OffsetLeft = 24f,
			OffsetTop = 44f,
			OffsetRight = -24f,
			OffsetBottom = 44f + 34f
		};
		uiOverlay.AddChild(bossHudRoot);
		SafeArea.ApplyTo(bossHudRoot);
		// Under the element tags when they wrap to a second line, as the chapter boss's bar steps down.
		if (GetNodeOrNull<LoadoutHud>("UIOverlay/LoadoutHud")?.TagRow is Control tags && tags.IsVisibleInTree())
		{
			float stepDown = Mathf.Max(0f, tags.GetGlobalRect().End.Y + 6f - bossHudRoot.GetGlobalRect().Position.Y);
			bossHudRoot.OffsetTop += stepDown;
			bossHudRoot.OffsetBottom += stepDown;
		}

		var row = new HBoxContainer { Name = "Bars", MouseFilter = Control.MouseFilterEnum.Ignore };
		row.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		row.AddThemeConstantOverride("separation", 12);
		bossHudRoot.AddChild(row);

		foreach (SentBoss sent in sentBosses)
		{
			var slot = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			row.AddChild(slot);
			var bar = new ProgressBar
			{
				Name = "BossHealthFill",
				MinValue = 0,
				MaxValue = 1000,
				Value = 0,
				ShowPercentage = false,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			bar.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			var trough = new StyleBoxFlat { BgColor = new Color(0.06f, 0.04f, 0.05f, 0.88f), BorderColor = new Color(0.62f, 0.30f, 0.24f, 0.95f) };
			trough.SetBorderWidthAll(1);
			trough.SetCornerRadiusAll(4);
			var fill = new StyleBoxFlat { BgColor = new Color(0.72f, 0.20f, 0.16f, 1.0f) };
			fill.SetCornerRadiusAll(4);
			bar.AddThemeStyleboxOverride("background", trough);
			bar.AddThemeStyleboxOverride("fill", fill);
			slot.AddChild(bar);

			var label = new Label
			{
				Name = "BossHealthLabel",
				Text = ShortBossName(sent.Definition),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				ClipText = true,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			ResponsiveLayout.SetFont(label, ResponsiveLayout.TextRole.Micro);
			label.AddThemeColorOverride("font_color", new Color(1f, 0.94f, 0.88f));
			label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
			label.AddThemeConstantOverride("outline_size", 5);
			slot.AddChild(label);
			sent.Bar = bar;

			if (sent.Pointer == null || !IsInstanceValid(sent.Pointer))
			{
				sent.Pointer = new WorldPointer { Name = "SentBossPointer", Rim = new Color(0.86f, 0.22f, 0.20f, 1f), Radius = 27f, VisibleMargin = 90f };
				uiOverlay.AddChild(sent.Pointer);
			}
		}
	}

	// Every frame from ProcessTick: each bar fills as its boss arrives and then is the fight's, each badge
	// points at its boss off screen, and a body gone without its defeat is counted as fallen.
	private void UpdateSentBosses()
	{
		if (sentBosses.Count == 0 || runFinished)
			return;
		for (int i = sentBosses.Count - 1; i >= 0; i--)
		{
			// A finale counted as fallen below empties the list.
			if (i >= sentBosses.Count)
				continue;
			SentBoss sent = sentBosses[i];
			if (!IsInstanceValid(sent.Body) || !sent.Body.IsInsideTree())
			{
				GD.PushWarning($"Node2DGame: sent boss {sent.Definition.Id} left the scene without reporting a defeat; counting it as fallen.");
				OnSentBossDefeated(sent);
				continue;
			}
			sent.LastPosition = sent.Body.GlobalPosition;
			if (sent.Bar != null && IsInstanceValid(sent.Bar))
				sent.Bar.Value = sent.Bar.MaxValue * sent.Body.BossHealthFraction * sent.Body.EntranceProgress;
			if (sent.Pointer != null && IsInstanceValid(sent.Pointer))
			{
				bool show = !sent.Body.IsDying;
				sent.Pointer.Target = show ? sent.Body.GlobalPosition : null;
				sent.Pointer.Icon = show ? CurrentFrameOf(sent.Body) : null;
			}
		}
	}
}
