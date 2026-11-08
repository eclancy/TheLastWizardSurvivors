using Godot;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

/// <summary>
/// The tracked quests during a run (.ai/quests-and-notes.md): in the LOWER LEFT, at most three, each a
/// heading and one line of what to do with its count. Informational only - it takes no input.
/// </summary>
/// <remarks>
/// WHY THE LOWER LEFT. Every other corner and band is spoken for: the health bar, the XP bar and the
/// spells hold the upper left; the element tags the upper right; the timer, the run's banner, the
/// objective line and the boss bar the top middle; the achievement plate the band under the HUD; and on a
/// phone the camera hole sits at the top. The lower left was empty on both canvases. On a phone it is also
/// the stick's ground, which costs nothing: this ignores the mouse and sits on the HUD's layer, under the
/// stick (TouchControls, layer 5), so a thumb landing on it steers as anywhere else.
///
/// The edge pointers - the boss's badge, the chests, the event, the places - ride a ring in from the
/// screen's edge, and the lower left is on it. This registers its rectangle with HudKeepOut and they slide
/// along their side of the ring off it.
///
/// No plate behind it. The words are outlined in occlusion black, the LoadoutHud numerals' treatment, so
/// they read on every ground including the Sketchbook's white paper, and the corner stays open play field.
///
/// LIVE. Node2DGame calls <see cref="Refresh"/> once a second with the run folded into the save
/// (AchievementContext.Projected - the achievement moment's reading), so a count moves during the run, a
/// side event won mid-run shows done at once, and "free the next wizard" moves on the moment he is out.
/// </remarks>
public partial class QuestTracker : VBoxContainer
{
	private const float EdgeX = 12f;
	private const float EdgeY = 14f;
	private const float WideWidth = 340f;
	private const float NarrowWidth = 400f;
	private const int OutlineSize = 6;

	private static readonly Color TitleInk = CodexRows.Stone.Heading;
	private static readonly Color StepInk = CodexRows.Stone.InkSoft;
	private static readonly Color DoneInk = CodexRows.Stone.Done;
	private static readonly Color Outline = new(0.02f, 0.025f, 0.04f, 0.92f);

	private sealed class EntryView
	{
		public VBoxContainer Box = null!;
		public Label Title = null!;
		public Label Step = null!;
		public string Key = string.Empty;
	}

	private readonly List<EntryView> entries = new();
	private readonly List<Quest> shown = new();
	private bool held;

	/// <summary>How many quests it is showing.</summary>
	public int Count => shown.Count;

	/// <summary>The quests as last read, for the run's opening to write in big.</summary>
	public IReadOnlyList<Quest> Quests => shown;

	public override void _Ready()
	{
		Name = "QuestTracker";
		MouseFilter = MouseFilterEnum.Ignore;
		bool narrow = ResponsiveLayout.IsNarrow(this);
		AnchorLeft = 0f;
		AnchorRight = 0f;
		AnchorTop = 1f;
		AnchorBottom = 1f;
		OffsetLeft = EdgeX;
		OffsetRight = EdgeX + (narrow ? NarrowWidth : WideWidth);
		OffsetBottom = -EdgeY;
		OffsetTop = -EdgeY;
		GrowVertical = GrowDirection.Begin;
		Alignment = AlignmentMode.End;
		AddThemeConstantOverride("separation", narrow ? 12 : 10);
		ProcessMode = ProcessModeEnum.Pausable;
	}

	public override void _ExitTree() => HudKeepOut.Clear(this);

	public override void _Process(double delta)
	{
		// The pointers read this every frame they draw; a dictionary write, no allocation.
		if (IsVisibleInTree() && shown.Count > 0 && !held)
			HudKeepOut.Set(this, GetGlobalRect().Grow(4f));
		else
			HudKeepOut.Clear(this);
	}

	/// <summary>
	/// Hidden while the run's opening writes the quests in big (QuestIntro); each comes in as its words
	/// arrive (<see cref="RevealEntry"/>). Laid out meanwhile, so the opening knows where to fly to.
	/// </summary>
	public void HoldForIntro()
	{
		held = true;
		foreach (EntryView view in entries)
			view.Box.Modulate = new Color(1f, 1f, 1f, 0f);
	}

	public void RevealEntry(int index)
	{
		if (index < 0 || index >= entries.Count)
			return;
		Control box = entries[index].Box;
		Tween tween = box.CreateTween();
		tween.TweenProperty(box, "modulate:a", 1f, 0.18f);
		if (index == entries.Count - 1)
			held = false;
	}

	/// <summary>Shows everything at once - nothing is opening, or the opening was cut short.</summary>
	public void ReleaseHold()
	{
		held = false;
		foreach (EntryView view in entries)
			view.Box.Modulate = Colors.White;
	}

	/// <summary>Where entry <paramref name="index"/>'s heading is on screen, and its font size: the opening's target.</summary>
	public bool TryGetTitleRect(int index, out Rect2 rect, out int fontSize)
	{
		rect = default;
		fontSize = 0;
		if (index < 0 || index >= entries.Count)
			return false;
		Label title = entries[index].Title;
		rect = title.GetGlobalRect();
		fontSize = title.GetThemeFontSize("font_size");
		return true;
	}

	/// <summary>The chapter this run plays, so a quest done elsewhere is not shown (QuestLog.Tracked).</summary>
	public string CurrentStageId { get; set; } = string.Empty;

	/// <summary>Reads the tracked quests again and redraws what changed. Once a second, not per frame.</summary>
	public void Refresh(SaveData save, AchievementContext context)
	{
		List<Quest> quests = QuestLog.Tracked(save, context, CurrentStageId);
		shown.Clear();
		shown.AddRange(quests);
		while (entries.Count < quests.Count)
			entries.Add(BuildEntry());
		for (int i = 0; i < entries.Count; i++)
		{
			EntryView view = entries[i];
			if (i >= quests.Count)
			{
				view.Box.Visible = false;
				view.Key = string.Empty;
				continue;
			}
			Quest quest = quests[i];
			string step = StepLine(quest);
			string key = quest.Title + "\n" + step + (quest.Done ? "\nD" : "");
			view.Box.Visible = true;
			if (key == view.Key)
				continue;
			bool changed = view.Key.Length > 0;
			view.Key = key;
			// Last two words bound (ResponsiveLayout.NoWidow): "Defeat the boss of the Enchanted /
			// Forest" left "Forest" alone on a line (Eric, 2026-11-04 - no single-word wraps).
			view.Title.Text = ResponsiveLayout.NoWidow(quest.Title);
			view.Title.AddThemeColorOverride("font_color", quest.Done ? DoneInk : TitleInk);
			view.Step.Text = ResponsiveLayout.NoWidow(step);
			// A quest that moved on or was finished while on screen: one gold breath, so the change is seen.
			if (changed && !held)
			{
				view.Box.Modulate = new Color(1.6f, 1.45f, 1.0f, 1f);
				view.Box.CreateTween().TweenProperty(view.Box, "modulate", Colors.White, 0.8f);
			}
		}
	}

	/// <summary>One line of what to do, without its full stop, and the count where there is one.</summary>
	public static string StepLine(Quest quest)
	{
		if (quest.Done)
			return "Done";
		string step = (quest.Step ?? string.Empty).Trim();
		if (step.EndsWith("."))
			step = step.Substring(0, step.Length - 1);
		if (quest.Progress != null && !string.IsNullOrEmpty(quest.Progress.Display))
			step = step.Length > 0 ? $"{step}  ·  {quest.Progress.Display}" : quest.Progress.Display;
		return step;
	}

	private EntryView BuildEntry()
	{
		bool narrow = ResponsiveLayout.IsNarrow(this);
		var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		box.AddThemeConstantOverride("separation", 0);
		AddChild(box);

		// The heading in the display face, a step under its card size: it names the quest, so it is a heading.
		var title = new Label { MouseFilter = MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		ResponsiveLayout.SetFont(title, ResponsiveLayout.TextRole.Title);
		title.AddThemeFontSizeOverride("font_size", ResponsiveLayout.FontSize(this, ResponsiveLayout.TextRole.Body));
		Outlined(title);
		box.AddChild(title);

		// What to do, in the engine sans, wrapped to the corner's width.
		var step = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		ResponsiveLayout.SetFont(step, ResponsiveLayout.TextRole.Label);
		step.AddThemeColorOverride("font_color", StepInk);
		Outlined(step);
		box.AddChild(step);
		if (held)
			box.Modulate = new Color(1f, 1f, 1f, 0f);
		return new EntryView { Box = box, Title = title, Step = step };
	}

	private static void Outlined(Label label)
	{
		label.AddThemeColorOverride("font_outline_color", Outline);
		label.AddThemeConstantOverride("outline_size", OutlineSize);
	}
}
