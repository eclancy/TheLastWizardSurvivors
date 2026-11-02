using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WizardSurvivors.scripts;

// THE ENDING (.ai/endgame-castle.md 6.4, "The end"): "The results screen carries an ending panel with every
// freed wizard standing in a row, named and nothing else (no copy carries the mood), then the credits."
//
// GameOverScreen lays it over itself on a victory over the dark wizard and holds its own count until this
// is done (GameOverScreen.Phase.Ending). Two parts, each moved on by a press:
//
//   1. THE ROW. On black, the wizards stand up one by one in a row - every wizard the save holds, the one
//      who walked the road among them, each in his own idle, his name under him and nothing else. The
//      prologue's moot had five round its fire and panel 4 led them away in a chain; this is them back.
//   2. THE CREDITS, rolling up slowly over the row as it dims. A press runs them to the end; a press at
//      the end (or the roll ending by itself) hands over to the results.
//
// Every press is swallowed here, before the results or its buttons can hear it (DefeatSequence.IsButton's
// rule: keys, buttons, fingers, a pad - and each swallowed press keeps its touch-and-mouse twin quiet).
// Nothing here grants or saves anything; it is the last picture, and then the ledger.
public partial class ThroneEnding : Control
{
	/// <summary>Raised once, as it hands over to the results.</summary>
	[Signal] public delegate void FinishedEventHandler();

	// THE CREDITS. Every line is a fact the repository states (README.md, NOTICE): who made it, what it is built
	// with, and the fonts' licence. Eric: edit freely - nothing reads them. (The
	// Sketchbook's "drawn by Eric's kids" line came out at his word, 2026-11-02.)
	private static readonly (string Text, bool Heading)[] Credits =
	{
		("Wizard Survivors", true),
		("", false),
		("Made by", false),
		("Eric Clancy", true),
		("", false),
		("Built with", false),
		("Godot Engine", true),
		("", false),
		("Type", false),
		("Cinzel and Pixelify Sans", true),
		("SIL Open Font License", false),
	};

	private const float RiseGapSeconds = 0.42f;
	private const float RiseSeconds = 0.6f;
	private const float RollSeconds = 18f;
	private const float RowArmSeconds = 1.2f;
	private const float FadeSeconds = 0.6f;
	private const ulong QuietAfterPressMsec = 250;

	private static readonly Color Black = new(0.01f, 0.01f, 0.02f, 1f);
	private static readonly Color NameInk = new(0.92f, 0.88f, 0.78f, 1f);
	private static readonly Color CreditInk = new(0.86f, 0.82f, 0.72f, 1f);
	private static readonly Color CreditMeta = new(0.62f, 0.60f, 0.58f, 1f);

	private enum Part { Row, Roll, Out, Done }
	private Part part = Part.Row;
	private float age;
	private float partAge;
	private ulong quietUntilMsec;

	private Control rowHost;
	private Control rollHost;
	private VBoxContainer roll;
	private readonly List<Figure> figures = new();

	private sealed class Figure
	{
		public Control Slot = null!;
		public TextureRect Picture = null!;
		public Texture2D[] Frames = Array.Empty<Texture2D>();
		public float Fps = 6f;
	}

	/// <summary>The wizards stood in the row, by id. For the tools.</summary>
	public IReadOnlyList<string> StandingIds { get; private set; } = Array.Empty<string>();

	/// <summary>Whether a finished run ends the story: a victory over the Throne's boss, the dark wizard.</summary>
	public static bool EndsTheStory(RunResult result) =>
		result != null && result.Outcome == RunOutcome.Victory
		&& BossCatalog.ForStageIndex(ThroneProcession.FinaleStageIndex) is BossDefinition him
		&& him.Id.Equals(result.BossId ?? string.Empty, StringComparison.OrdinalIgnoreCase);

	/// <summary>True once the row has handed over to the credits. For the tools.</summary>
	public bool Rolling => part >= Part.Roll;

	public override void _Ready()
	{
		Name = "ThroneEnding";
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		var black = new ColorRect { Name = "Black", Color = Black, MouseFilter = MouseFilterEnum.Ignore };
		black.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(black);
		BuildRow();
		BuildRoll();
		SafeArea.Apply(this);
	}

	// Every wizard the save holds, in the roster's order; the one played this run among them. A run with no
	// save to read (a tool's) stands the whole roster. Never the dark wizard himself: the row is everyone he
	// took, and he is the one whose light went out (.ai/dark-wizard-playable.md) - even on a run he walked.
	private static List<CharacterData> Wizards()
	{
		SaveData save = (Engine.GetMainLoop() as SceneTree)?.Root?.GetNodeOrNull<SaveManager>("SaveManager")?.Data;
		var ids = new HashSet<string>(UnlockCatalog.AllCharacterIds, StringComparer.OrdinalIgnoreCase);
		ids.Remove(DarkWizardId);
		return CharacterRoster.GetAll()
			.Where(c => c != null && ids.Contains(c.Id) && (save == null || GlobalStatsManager.IsCharacterUnlocked(save, c.Id)))
			.ToList();
	}

	private const string DarkWizardId = "dark_wizard";

	private void BuildRow()
	{
		bool narrow = ResponsiveLayout.IsNarrow(this);
		Vector2 view = ResponsiveLayout.ViewportSize(this);
		List<CharacterData> wizards = Wizards();
		StandingIds = wizards.Select(w => w.Id).ToList();

		// x3 where the whole row fits on one line, x2 otherwise; on a phone the row wraps. A slot is never
		// narrower than a name: "Pyromancer" and "Frostweaver" are about 120 units at the label size, and a
		// name broken inside a word reads as two.
		int count = Math.Max(1, wizards.Count);
		const float NameRoom = 136f;
		int scale = !narrow && count * Mathf.Max(NameRoom, 48f * 3f + 18f) <= view.X * 0.94f ? 3 : 2;
		float slotWidth = Mathf.Max(NameRoom, 48f * scale + 18f);

		rowHost = new CenterContainer { Name = "Row", MouseFilter = MouseFilterEnum.Ignore };
		rowHost.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(rowHost);
		var flow = new HFlowContainer
		{
			Name = "Wizards",
			MouseFilter = MouseFilterEnum.Ignore,
			Alignment = FlowContainer.AlignmentMode.Center,
			CustomMinimumSize = new Vector2(Mathf.Min(view.X - 32f, count * slotWidth), 0f),
		};
		flow.AddThemeConstantOverride("h_separation", 0);
		flow.AddThemeConstantOverride("v_separation", narrow ? 26 : 18);
		rowHost.AddChild(flow);

		foreach (CharacterData wizard in wizards)
		{
			var slot = new VBoxContainer
			{
				Name = $"Wizard_{wizard.Id}",
				MouseFilter = MouseFilterEnum.Ignore,
				CustomMinimumSize = new Vector2(slotWidth, 0f),
				Modulate = new Color(1f, 1f, 1f, 0f),
			};
			slot.AddThemeConstantOverride("separation", 6);
			flow.AddChild(slot);

			Texture2D[] frames = IdleFrames(wizard.Frames, out float fps);
			var picture = new TextureRect
			{
				Name = "Figure",
				Texture = frames.Length > 0 ? frames[0] : wizard.Portrait,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = TextureFilterEnum.Nearest,
				CustomMinimumSize = new Vector2(48f * scale, 48f * scale),
				MouseFilter = MouseFilterEnum.Ignore,
				SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			};
			slot.AddChild(picture);

			var name = new Label
			{
				Name = "Name",
				Text = wizard.Name,
				HorizontalAlignment = HorizontalAlignment.Center,
				AutowrapMode = TextServer.AutowrapMode.Word,
				CustomMinimumSize = new Vector2(slotWidth - 4f, 0f),
				MouseFilter = MouseFilterEnum.Ignore,
			};
			ResponsiveLayout.SetFont(name, ResponsiveLayout.TextRole.Label);
			name.AddThemeColorOverride("font_color", NameInk);
			slot.AddChild(name);

			figures.Add(new Figure { Slot = slot, Picture = picture, Frames = frames, Fps = fps });
		}
	}

	private static Texture2D[] IdleFrames(SpriteFrames frames, out float fps)
	{
		fps = 6f;
		if (frames == null)
			return Array.Empty<Texture2D>();
		string anim = frames.HasAnimation("idle") ? "idle" : frames.HasAnimation("moving") ? "moving" : null;
		if (anim == null)
			return Array.Empty<Texture2D>();
		fps = Mathf.Max(1f, (float)frames.GetAnimationSpeed(anim));
		int n = frames.GetFrameCount(anim);
		var list = new Texture2D[n];
		for (int i = 0; i < n; i++)
			list[i] = frames.GetFrameTexture(anim, i);
		return list;
	}

	private void BuildRoll()
	{
		rollHost = new Control { Name = "Credits", MouseFilter = MouseFilterEnum.Ignore, ClipContents = true, Visible = false };
		rollHost.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(rollHost);
		roll = new VBoxContainer { Name = "Roll", MouseFilter = MouseFilterEnum.Ignore };
		roll.AddThemeConstantOverride("separation", 6);
		rollHost.AddChild(roll);
		foreach ((string text, bool heading) in Credits)
		{
			var line = new Label
			{
				Text = text,
				HorizontalAlignment = HorizontalAlignment.Center,
				MouseFilter = MouseFilterEnum.Ignore,
				CustomMinimumSize = new Vector2(0f, text.Length == 0 ? 18f : 0f),
			};
			ResponsiveLayout.SetFont(line, heading ? (text == Credits[0].Text ? ResponsiveLayout.TextRole.Display : ResponsiveLayout.TextRole.Title) : ResponsiveLayout.TextRole.Label);
			line.AddThemeColorOverride("font_color", heading ? CreditInk : CreditMeta);
			roll.AddChild(line);
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		age += dt;
		partAge += dt;

		// The row: each wizard comes out of the dark in turn, and breathes in his own idle.
		for (int i = 0; i < figures.Count; i++)
		{
			Figure f = figures[i];
			float u = Mathf.Clamp((age - 0.5f - i * RiseGapSeconds) / RiseSeconds, 0f, 1f);
			// Under the credits the row goes back almost to the dark, so the lines read clean over it.
			float dim = part >= Part.Roll ? 1f - 0.86f * Mathf.Clamp(partAge / 1.2f, 0f, 1f) : 1f;
			f.Slot.Modulate = new Color(dim, dim, dim, u);
			if (f.Frames.Length > 1)
				f.Picture.Texture = f.Frames[(int)(age * f.Fps) % f.Frames.Length];
		}

		if (part == Part.Roll)
		{
			float u = Mathf.Clamp(partAge / RollSeconds, 0f, 1f);
			PlaceRoll(u);
			if (u >= 1f)
				BeginOut();
		}
		else if (part == Part.Out)
		{
			Modulate = new Color(1f, 1f, 1f, 1f - Mathf.Clamp(partAge / FadeSeconds, 0f, 1f));
			if (partAge >= FadeSeconds)
			{
				part = Part.Done;
				EmitSignal(SignalName.Finished);
				QueueFree();
			}
		}
	}

	// From under the bottom edge to past the top, centred: a slow credits roll.
	private void PlaceRoll(float u)
	{
		Vector2 view = rollHost.Size;
		Vector2 size = roll.GetCombinedMinimumSize();
		roll.Size = new Vector2(view.X, size.Y);
		roll.Position = new Vector2(0f, Mathf.Lerp(view.Y, -size.Y, u));
	}

	/// <summary>Moves it on: the row to the credits, the credits to their end, the end to the results.</summary>
	public void Advance()
	{
		switch (part)
		{
			case Part.Row:
				// The row is not skipped before it has stood: a press during the first second only waits.
				if (age < RowArmSeconds)
					return;
				part = Part.Roll;
				partAge = 0f;
				rollHost.Visible = true;
				PlaceRoll(0f);
				break;
			case Part.Roll:
				BeginOut();
				break;
		}
	}

	private void BeginOut()
	{
		if (part >= Part.Out)
			return;
		part = Part.Out;
		partAge = 0f;
	}

	public override void _Input(InputEvent @event)
	{
		if (part == Part.Done || !DefeatSequence.IsButton(@event, out bool pressed))
			return;
		GetViewport().SetInputAsHandled();
		ulong now = Time.GetTicksMsec();
		if (!pressed || now < quietUntilMsec)
			return;
		quietUntilMsec = now + QuietAfterPressMsec;
		Advance();
	}
}
