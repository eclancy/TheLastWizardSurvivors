using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

/// <summary>
/// The in-run loadout strip: the held spells as small icons in slot order, each with its level, and
/// one chip per element the player holds a tag of, in that element's colour with its progress
/// ("3/4"). Informational only - it takes no input, and the pause screen is still where a spell or
/// an element is explained.
/// </summary>
/// <remarks>
/// The user's request: "we should show the spells and tags on the game screen while playing, but
/// small. the tags can just be colors with the 2/3 symbol, and the spells can just be the icons. we
/// don't need to show the boons." The old element badges ("Fire 2/4" at 102x28) and the 48-pixel
/// spell row had been taken off the playfield as too heavy for something the player does not act
/// on mid-fight; this puts back the glance - what do I hold, how close is the next tier - at about
/// a third of the area, and leaves the reading to the pause screen.
///
/// WHERE THE TAGS SIT: THE UPPER RIGHT (2026-10-28). "the element tags are too far down on the screen,
/// move it into the upper right." They were the second half of this strip - under the spells on a PC
/// (y 122), beside them at the foot of the touch band on a phone (y 210 of 1280). Now they are their
/// own flow (<see cref="tagBox"/>, "LoadoutTags", a sibling of the strip on the UI overlay) hung from
/// the top right at the timer's height: one right-aligned line on a PC, where even twelve chips end
/// clear of the timer; on a phone a block between the timer and the touch pause button, wrapping
/// downward (Node2DGame.ConfigureBossHud drops the boss bar below it if it ever wraps that far). The
/// spells stay where they were, below.
///
/// WHERE THE SPELLS SIT. Both canvases put them on the left under the health and XP bars, so the
/// player's own state is one block in one corner, but the two place it by different neighbours:
/// <list type="bullet">
/// <item>PC (landscape): a column the width of the spell row, starting under the boss bar's slot
/// (44-78, full width, only during the fight), so the bar never lands on it. The run's centred
/// lines - the side event objective, the stage intro, the onboarding tips - are centred on a
/// 1280-wide canvas and never reach x 260.</item>
/// <item>Phone (portrait): one row across the canvas, standing on the FLOOR of the touch pad's HUD
/// band (TouchControls.DefaultHudBandHeight). Every top-of-screen line above it - the boss bar,
/// the objective, the onboarding tip - spans the canvas width on a 720-wide screen, so the only
/// slot clear of all of them is the bottom of the band; and below the band is the stick, where
/// a thumb can land anywhere. SafeArea moves the band and the strip down together, so the strip
/// stays inside it. If the chips ever wrap to a second line the row grows UP, not into the pad.</item>
/// </list>
///
/// WHY IT CAN NEVER EAT A TAP. Every node here is MouseFilter Ignore. Below the band the pad's
/// layer (5) is above this one anyway, but inside the band nothing stands between a STOP control
/// here and the touch - and a strip that swallowed taps would be a dead patch of screen.
///
/// WHEN IT REDRAWS. On <see cref="Player.LoadoutChanged"/> only, coalesced to one rebuild per frame
/// (a Penknife cut raises it twice). Nothing is allocated per frame; the slot and chip nodes are built
/// once and re-filled. Two things run per frame, and only while there is something to show: a timed
/// seal's drain (four times a second), and the WARD GAUGE (in-flight 76) - a slot holding Aegis Ward,
/// Stormguard or Gloam Veil asks the player every frame whether it is up and how far it has come back,
/// and redraws that slot's gauge alone, and only when a whole unit of it changed.
/// </remarks>
public partial class LoadoutHud : BoxContainer
{
	/// <summary>The player whose loadout is shown. Set before adding to the tree.</summary>
	public Player? Holder { get; set; }

	// The spell icons are 32-pixel art stored at x2 (64x64 PNGs). At 32 canvas units each art pixel
	// is one unit and Nearest filtering keeps every one; at the 24-28 first sketched, Nearest drops
	// every fourth or eighth row and column, which on pixel art reads as damage, not as small.
	private const float IconSize = 32f;
	private const float SlotRim = 1f;
	private const float SlotSize = IconSize + 2f * SlotRim;
	private const int SlotGap = 2;
	private const int ChipGap = 3;
	private const int MaxSlots = Player.MaxSpellSlots + Player.BlankFolioExtraSlots;

	// Landscape. Left-aligned with the health bar (Node2DGame.ConfigurePlayerHealthHud, x 7); top
	// under the XP bar. The boss bar sits under this row (HudBand.PlayerHudFloor, in-flight 114).
	private const float EdgeLeft = 7f;
	private const float LandscapeTop = 84f;
	private const float LandscapeWidth = MaxSlots * SlotSize + (MaxSlots - 1) * SlotGap;
	// Portrait. A few units of air above the band's floor so the row does not sit exactly on the
	// line where a thumb starts to steer.
	private const float BandFloorGap = 4f;

	// Inks and rims, from the palette in .ai/art-direction.md. The slot backing is floor stone deep at
	// 0.85: the icons are drawn for the Bonelight dark ground, and on white ground (measured on the
	// Sketchbook's paper, the palest the game has had) a thinner backing (0.62 was tried) left the steel
	// rim at 1.3:1 and every milestone rim under 3:1.
	// At 0.85 the weakest, the plain held rim, is 3.0:1 on white paper (tools/art/ui_contrast.py).
	private static readonly Color SlotBacking = new(0.043f, 0.059f, 0.094f, 0.85f);
	private static readonly Color HeldRim = new(0.416f, 0.494f, 0.596f, 1f);          // steel base #6A7E98
	private static readonly Color EmptyBacking = new(0.043f, 0.059f, 0.094f, 0.28f);
	private static readonly Color EmptyRim = new(0.604f, 0.643f, 0.722f, 0.30f);      // bone base #9AA4B8, faint
	// A page the book does not have yet (SpellBook, .ai/slow-start.md): quieter than an empty one, the
	// backing half as dense and the rim a ghost, and a small lock in the middle that says why.
	private static readonly Color LockedBacking = new(0.043f, 0.059f, 0.094f, 0.16f);
	private static readonly Color LockedRim = new(0.604f, 0.643f, 0.722f, 0.14f);
	private static readonly Color LockedInk = new(0.604f, 0.643f, 0.722f, 0.34f);
	// The three milestones keep the hue their level-up family is drawn in (.ai/level-up-screen.md 2c):
	// the mutation's gilt, the ascension's arcane glass, the apotheosis's fire.
	private static readonly Color MutatedRim = new(0.847f, 0.690f, 0.290f, 1f);       // gold lit #D8B04A
	private static readonly Color AscendedRim = new(0.416f, 0.722f, 0.910f, 1f);      // arcane cyan lit #6AB8E8
	private static readonly Color ApotheosisRim = new(0.910f, 0.478f, 0.102f, 1f);    // fire mid #E87A1A
	// The level numeral: the health readout's ink and outline, so the HUD has one way of writing a
	// number over art. On its black outline it is 17:1 whatever the icon is under it.
	private static readonly Color NumeralInk = new(1f, 0.94f, 0.86f);
	private static readonly Color NumeralOutline = new(0f, 0f, 0f, 0.9f);
	private const int NumeralOutlineSize = 4;

	private sealed class SlotView
	{
		public Panel Frame = null!;
		public StyleBoxFlat Style = null!;
		public TextureRect Icon = null!;
		public Label Level = null!;
		// The Sovereign Rite's marks (.ai/sovereign-spell.md section 5): a crown over the anointed slot, a
		// closed seal across a silenced one. Drawn, one Control a slot, redrawn only when the slot is filled.
		public Control Mark = null!;
		public SlotMark MarkKind;
		// The dark wizard's seal (Player.Seal.cs) wears the same mark, and while it runs on a clock a line of
		// wax under the band drains with it: 1 full, 0 gone, -1 for no drain (the Rite's, or held until the orb).
		public float SealDrain = -1f;
		public bool WasSealed;
		// THE WARD GAUGE (in-flight 76; Player.TryGetWardGauge): the slot holds a ward - Aegis Ward, Stormguard or Gloam
		// Veil - and while it is down a dark veil covers the icon from the top, drawing back as it comes back, its edge a
		// line of the spell's own light; the slot flares the moment it is up. WardFill is the icon units back (-1 to force
		// a redraw), WardInk the spell's element.
		public Control Gauge = null!;
		public SpellData? WardSpell;
		public bool WardUp = true;
		public int WardFill = -1;
		public Color WardInk;
		// Harmony's slot (the Canticle's level 4, ArcaneExplosion.HarmonyElementOf): the slot after the Canticle's,
		// whose spell lends the burst its element, marked with a note in that element's colour and tied to the
		// Canticle's slot. Transparent when the slot is not Harmony's.
		public Color HarmonyInk = Colors.Transparent;
	}

	private enum SlotMark { None, Sovereign, Silenced, Locked }

	// The anointed slot's gold crown frame, the conjunction plate's gold lit (#D8B04A) and its light hot
	// for the crown's points; the fuel is written in the same gold. A silenced slot's icon is greyed to the
	// steel shade and sealed with the dark wizard's wax red (.ai/endgame-castle.md 6.4's seal).
	private static readonly Color SovereignRim = new(0.847f, 0.690f, 0.290f, 1f);
	private static readonly Color SovereignHot = new(1.0f, 0.973f, 0.847f, 1f);
	private static readonly Color FuelInk = new(1.0f, 0.86f, 0.42f);
	private static readonly Color SilencedIcon = new(0.42f, 0.44f, 0.50f, 1f);
	private static readonly Color SilencedRim = new(0.25f, 0.27f, 0.32f, 1f);
	private static readonly Color SealWax = new(0.545f, 0.157f, 0.157f, 1f);
	private static readonly Color SealWaxLit = new(0.765f, 0.290f, 0.235f, 1f);
	private static readonly Color SealBand = new(0.12f, 0.10f, 0.12f, 1f);

	// Whether the silenced icons have flown into the anointed slot yet: once a run, on the first redraw
	// after the Rite (the menu closes and play resumes, so it plays over the game and pauses nothing).
	private bool sovereignArrived;

	private sealed class ChipView
	{
		public Element Element;
		public PanelContainer Chip = null!;
		public Label Text = null!;
		public Control Star = null!;
	}

	private readonly SlotView[] slots = new SlotView[MaxSlots];
	private readonly Dictionary<Element, ChipView> chips = new();
	private readonly List<KeyValuePair<Element, int>> order = new();
	private HFlowContainer tagFlow = null!;
	private bool rebuildQueued;
	// How many slots hold a ward (the gauge runs only while one does).
	private int wardSlots;
	// The veil over a ward that is down: the slot backing's stone, dense enough to read as "not there yet".
	private static readonly Color WardVeil = new(0.043f, 0.059f, 0.094f, 0.78f);

	// The tags' corner. Top: the run timer's and the touch pause button's (8). Right: the canvas edge
	// mirrored from EdgeLeft, or clear of the pause button (TouchControls: 48 wide, 8 in) with a gap.
	private const float TagsTop = 8f;
	private const float PauseButtonClearance = 56f + 8f + 6f;
	// Wide: twelve chips (every element) in one line still end left of 1280 - 560 = 720, right of the
	// timer (576-704). Narrow: from 12 clear of the timer (its right edge is 424 on 720) to the pause
	// button: five chips a line.
	private const float TagsWidthWide = 560f;
	private const float TagsWidthNarrow = 214f;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		bool narrow = ResponsiveLayout.IsNarrow(this);
		Place(narrow);

		var spellRow = new HBoxContainer
		{
			Name = "SpellRow",
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		};
		spellRow.AddThemeConstantOverride("separation", SlotGap);
		AddChild(spellRow);
		for (int i = 0; i < MaxSlots; i++)
			slots[i] = BuildSlot(spellRow, i);

		tagFlow = new HFlowContainer
		{
			Name = "LoadoutTags",
			MouseFilter = MouseFilterEnum.Ignore,
			Alignment = FlowContainer.AlignmentMode.End,
		};
		tagFlow.AddThemeConstantOverride("h_separation", ChipGap);
		tagFlow.AddThemeConstantOverride("v_separation", ChipGap);
		PlaceTags(narrow);
		// A sibling on the overlay, not a child of this box: the box lays its children out in a row
		// or a column under the health bar, and the tags belong in the other corner. Added now, inside
		// the overlay's build, so the SafeArea pass Node2DGame runs on the overlay next moves it too.
		GetParent().AddChild(tagFlow);
		foreach (Element element in System.Enum.GetValues<Element>())
			chips[element] = BuildChip(element);

		if (Holder != null)
		{
			Holder.Connect(Player.SignalName.LoadoutChanged, new Callable(this, nameof(OnLoadoutChanged)));
			Holder.Connect(Player.SignalName.SealsChanged, new Callable(this, nameof(OnLoadoutChanged)));
		}
		// Only while a seal drains or a ward is held (Rebuild turns it on).
		SetProcess(false);
		Rebuild();
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(tagFlow))
			tagFlow.QueueFree();
		var callable = new Callable(this, nameof(OnLoadoutChanged));
		if (Holder != null && IsInstanceValid(Holder) && Holder.IsConnected(Player.SignalName.LoadoutChanged, callable))
			Holder.Disconnect(Player.SignalName.LoadoutChanged, callable);
		if (Holder != null && IsInstanceValid(Holder) && Holder.IsConnected(Player.SignalName.SealsChanged, callable))
			Holder.Disconnect(Player.SignalName.SealsChanged, callable);
	}

	// THE SEAL'S DRAIN. While a seal the orb did not bring back runs out its twenty seconds, the wax line
	// under its band shortens with it; redrawn a few times a second, and only then, so the strip still
	// costs nothing per frame in every other moment of every run.
	private float drainClock;
	private const float DrainRedrawSeconds = 0.25f;

	public override void _Process(double delta)
	{
		bool wards = TickWards();
		bool seals = TickSealDrain((float)delta);
		if (!wards && !seals)
			SetProcess(false);
	}

	// False once no timed seal is left to drain.
	private bool TickSealDrain(float delta)
	{
		if (Holder == null || !IsInstanceValid(Holder) || !Holder.AnyTimedSeal)
			return false;
		drainClock -= delta;
		if (drainClock > 0f)
			return true;
		drainClock = DrainRedrawSeconds;
		for (int i = 0; i < MaxSlots; i++)
		{
			SlotView view = slots[i];
			if (view.SealDrain < 0f)
				continue;
			view.SealDrain = Holder.SealRemainingFraction(i);
			view.Mark.QueueRedraw();
		}
		return true;
	}

	// THE WARD GAUGE, every frame a ward is held: each ward slot asks the player, and redraws only when it went up or
	// down or a whole icon unit came back - about thirty redraws of one small control over a whole recharge.
	private bool TickWards()
	{
		if (wardSlots == 0 || Holder == null || !IsInstanceValid(Holder))
			return false;
		for (int i = 0; i < MaxSlots; i++)
		{
			SlotView view = slots[i];
			if (view.WardSpell == null)
				continue;
			Holder.TryGetWardGauge(view.WardSpell, out bool up, out float recharge);
			int fill = up ? (int)IconSize : Mathf.Clamp((int)(recharge * IconSize), 0, (int)IconSize);
			if (up == view.WardUp && fill == view.WardFill)
				continue;
			// Back: it was down and is up again, seen on this slot, not on a rebuild.
			bool cameBack = up && !view.WardUp && view.WardFill >= 0;
			view.WardUp = up;
			view.WardFill = fill;
			view.Gauge.QueueRedraw();
			if (cameBack)
				FlareWard(view);
		}
		return true;
	}

	// Anchors and offsets as built; SafeArea.Apply on the UIOverlay (Node2DGame) moves them in from
	// a phone's corners afterwards, exactly as it does the health bar, so the strip and the bar stay
	// aligned.
	private void Place(bool narrow)
	{
		AnchorLeft = 0f;
		AnchorTop = 0f;
		AnchorBottom = 0f;
		if (narrow)
		{
			// One row across the canvas: the spells (the chips are in the top right, PlaceTags).
			Vertical = false;
			AnchorRight = 1f;
			OffsetLeft = EdgeLeft;
			OffsetRight = -EdgeLeft;
			OffsetBottom = TouchControls.DefaultHudBandHeight - BandFloorGap;
			OffsetTop = OffsetBottom - SlotSize;
			GrowVertical = GrowDirection.Begin;
			AddThemeConstantOverride("separation", 8);
		}
		else
		{
			// A column the width of a full book: the spells (the chips are in the top right, PlaceTags).
			Vertical = true;
			AnchorRight = 0f;
			OffsetLeft = EdgeLeft;
			OffsetRight = EdgeLeft + LandscapeWidth;
			OffsetTop = LandscapeTop;
			OffsetBottom = LandscapeTop + SlotSize;
			AddThemeConstantOverride("separation", 4);
		}
	}

	/// <summary>The tags' flow, for whatever must keep clear of it (the boss bar). Null before _Ready.</summary>
	public Control? TagRow => IsInstanceValid(tagFlow) ? tagFlow : null;

	private void PlaceTags(bool narrow)
	{
		float right = narrow && TouchControls.ShouldEnable() ? -PauseButtonClearance : -EdgeLeft;
		tagFlow.AnchorLeft = 1f;
		tagFlow.AnchorRight = 1f;
		tagFlow.AnchorTop = 0f;
		tagFlow.AnchorBottom = 0f;
		tagFlow.OffsetRight = right;
		tagFlow.OffsetLeft = right - (narrow ? TagsWidthNarrow : TagsWidthWide);
		tagFlow.OffsetTop = TagsTop;
		tagFlow.OffsetBottom = TagsTop;
		tagFlow.GrowHorizontal = GrowDirection.Begin;
		tagFlow.GrowVertical = GrowDirection.End;
	}

	private SlotView BuildSlot(Container row, int index)
	{
		var style = new StyleBoxFlat { BgColor = EmptyBacking, BorderColor = EmptyRim };
		style.SetBorderWidthAll((int)SlotRim);
		style.SetCornerRadiusAll(3);
		var frame = new Panel
		{
			Name = $"SpellSlot{index}",
			CustomMinimumSize = new Vector2(SlotSize, SlotSize),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		frame.AddThemeStyleboxOverride("panel", style);
		row.AddChild(frame);

		var icon = new TextureRect
		{
			Name = "Icon",
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = TextureFilterEnum.Nearest,
			MouseFilter = MouseFilterEnum.Ignore,
			OffsetLeft = SlotRim,
			OffsetTop = SlotRim,
			OffsetRight = SlotRim + IconSize,
			OffsetBottom = SlotRim + IconSize,
		};
		frame.AddChild(icon);

		var view = new SlotView { Frame = frame, Style = style, Icon = icon };
		// Over the icon, under the marks and the numeral, so the level still reads over a ward that is down.
		var gauge = new Control { Name = "WardGauge", MouseFilter = MouseFilterEnum.Ignore };
		gauge.SetAnchorsPreset(LayoutPreset.FullRect);
		gauge.Draw += () => DrawWard(gauge, view);
		frame.AddChild(gauge);
		view.Gauge = gauge;

		// Bottom right, pushed down by the font's descent so the numeral sits in the corner of the
		// icon rather than floating a third of the way up it.
		var level = new Label
		{
			Name = "Level",
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		level.SetAnchorsPreset(LayoutPreset.FullRect);
		level.OffsetRight = -1f;
		level.OffsetBottom = 5f;
		frame.AddChild(level);
		ResponsiveLayout.SetFont(level, ResponsiveLayout.TextRole.Micro);
		level.AddThemeColorOverride("font_color", NumeralInk);
		level.AddThemeColorOverride("font_outline_color", NumeralOutline);
		level.AddThemeConstantOverride("outline_size", NumeralOutlineSize);

		view.Level = level;
		var mark = new Control
		{
			Name = "SovereignMark",
			MouseFilter = MouseFilterEnum.Ignore,
		};
		mark.SetAnchorsPreset(LayoutPreset.FullRect);
		mark.Draw += () =>
		{
			DrawMark(mark, view.MarkKind, view.SealDrain);
			if (view.HarmonyInk.A > 0f)
				DrawHarmony(mark, view.HarmonyInk);
		};
		frame.AddChild(mark);
		// Under the numeral, so the level (or the fuel) still reads over the seal.
		frame.MoveChild(mark, level.GetIndex());
		view.Mark = mark;
		return view;
	}

	// The crown: three points of gilt standing on the slot's top edge, the middle one tallest, each tipped in
	// the light hot. The seal: a dark band across the icon's middle and a small round of wax on it, pressed
	// with a bar - closed, and small enough that the greyed icon still says which spell it was.
	// In whole canvas units, two to an art pixel, so it reads as part of the 32-unit icon's grid.
	private static void DrawMark(Control mark, SlotMark kind, float drain)
	{
		Vector2 size = mark.Size;
		if (kind == SlotMark.Sovereign)
		{
			float mid = MathF.Round(size.X / 2f);
			for (int i = -1; i <= 1; i++)
			{
				float x = mid + i * 8f;
				float tall = i == 0 ? 8f : 6f;
				mark.DrawRect(new Rect2(x - 2f, -tall, 4f, tall), SovereignRim);
				mark.DrawRect(new Rect2(x - 1f, -tall - 2f, 2f, 2f), SovereignHot);
			}
			mark.DrawRect(new Rect2(mid - 12f, -2f, 24f, 2f), SovereignRim);
		}
		else if (kind == SlotMark.Silenced)
		{
			Vector2 c = (size / 2f).Round();
			mark.DrawRect(new Rect2(2f, c.Y - 2f, size.X - 4f, 4f), SealBand);
			mark.DrawRect(new Rect2(c.X - 3f, c.Y - 5f, 6f, 10f), SealWax);
			mark.DrawRect(new Rect2(c.X - 5f, c.Y - 3f, 10f, 6f), SealWax);
			mark.DrawRect(new Rect2(c.X - 3f, c.Y - 3f, 2f, 2f), SealWaxLit);
			mark.DrawRect(new Rect2(c.X - 2f, c.Y, 4f, 1f), SealBand);
			// The dark wizard's timed seal: a line of wax under the band, as long as the seal has left.
			if (drain >= 0f)
			{
				float width = Mathf.Round((size.X - 4f) * Mathf.Clamp(drain, 0f, 1f));
				if (width >= 1f)
					mark.DrawRect(new Rect2(2f, c.Y + 3f, width, 2f), SealWaxLit);
			}
		}
		else if (kind == SlotMark.Locked)
		{
			// A padlock, eight units by ten, in the ghost ink: the shackle's two legs and bow, then the body.
			Vector2 c = (size / 2f).Round();
			mark.DrawRect(new Rect2(c.X - 3f, c.Y - 5f, 1f, 4f), LockedInk);
			mark.DrawRect(new Rect2(c.X + 2f, c.Y - 5f, 1f, 4f), LockedInk);
			mark.DrawRect(new Rect2(c.X - 2f, c.Y - 6f, 4f, 1f), LockedInk);
			mark.DrawRect(new Rect2(c.X - 4f, c.Y - 1f, 8f, 6f), LockedInk);
		}
	}

	// HARMONY'S SLOT (2026-11-09; .ai/in-flight.md 9a, a slot effect reads on the strip with the slot it touches
	// highlighted). Eric: "i want the player to be able to purposefully select the element" - so the strip says which
	// slot it is and what it lends: a quaver standing on the slot's top left corner in the element's colour, and a
	// tie under the two slots from the Canticle's to this one. An empty slot after the Canticle wears it in the ghost
	// ink (the burst stays Arcane until a spell is taken into it). Whole canvas units, two to an art pixel, every
	// stroke outlined in the occlusion ink so it reads over any icon and on the palest ground.
	private static readonly Color HarmonyOutline = new(0.020f, 0.027f, 0.047f, 1f);   // OCC #05070C
	private static readonly Color HarmonyGhost = new(0.604f, 0.643f, 0.722f, 0.75f);  // bone base #9AA4B8

	private static void DrawHarmony(Control mark, Color ink)
	{
		Vector2 size = mark.Size;
		// The quaver: head, stem, flag - each drawn first two units larger in the outline, then in its ink.
		Rect2 head = new(2f, 2f, 4f, 4f);
		Rect2 stem = new(6f, -8f, 2f, 12f);
		Rect2 flag = new(8f, -8f, 2f, 4f);
		foreach (Rect2 r in new[] { head, stem, flag })
			mark.DrawRect(r.Grow(2f), HarmonyOutline);
		mark.DrawRect(head, ink);
		mark.DrawRect(stem, ink);
		mark.DrawRect(flag, ink);
		// The tie: from the middle of the Canticle's slot, the one before, to the middle of this one, bowed under
		// both - two units thick on a dark line.
		float from = size.X / 2f - (SlotSize + SlotGap);
		float to = size.X / 2f;
		for (float x = from; x <= to; x += 2f)
		{
			float t = (x - from) / Mathf.Max(1f, to - from);
			float y = size.Y + 2f + Mathf.Round(Mathf.Sin(t * Mathf.Pi) * 2f) * 2f;
			mark.DrawRect(new Rect2(x, y, 2f, 4f), HarmonyOutline);
			mark.DrawRect(new Rect2(x, y, 2f, 2f), ink);
		}
	}

	private ChipView BuildChip(Element element)
	{
		// The same face every other element chip in the game wears (CodexRows.BuildChip): the
		// element's colour nudged to clear 4.5:1 against its ink, the ink picked by WCAG contrast,
		// and a rim of its own colour darkened so the pale ones keep an edge. Opaque, so the ratio
		// measured in tools/art/ui_contrast.py is the ratio on screen whatever the ground is.
		Color fill = CodexRows.ChipFill(ElementColors.GetColor(element));
		Color ink = StatIcons.ReadableOn(fill);
		var style = new StyleBoxFlat { BgColor = fill, BorderColor = fill.Darkened(0.35f) };
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(3);
		style.ContentMarginLeft = 5f;
		style.ContentMarginRight = 5f;
		style.ContentMarginTop = 0f;
		style.ContentMarginBottom = 0f;

		var chip = new PanelContainer
		{
			Name = $"LoadoutChip_{element}",
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		chip.AddThemeStyleboxOverride("panel", style);
		tagFlow.AddChild(chip);

		var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 2);
		chip.AddChild(row);

		var text = new Label
		{
			Name = "Count",
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		row.AddChild(text);
		ResponsiveLayout.SetFont(text, ResponsiveLayout.TextRole.Micro);
		text.AddThemeColorOverride("font_color", ink);

		// The top tier's star, in the chip's ink. Drawn rather than typed: the UI face (the engine's
		// Open Sans) has no star glyph, and a fallback font is not something every phone promises.
		// It draws only when the chip is laid out or shown, never per frame.
		var star = new Control
		{
			Name = "Max",
			CustomMinimumSize = new Vector2(9f, 9f),
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		var points = new Vector2[10];
		star.Draw += () =>
		{
			Vector2 centre = star.Size / 2f;
			float outer = Mathf.Min(star.Size.X, star.Size.Y) / 2f;
			for (int i = 0; i < points.Length; i++)
			{
				float angle = -Mathf.Pi / 2f + i * Mathf.Pi / 5f;
				points[i] = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (i % 2 == 0 ? outer : outer * 0.45f);
			}
			star.DrawColoredPolygon(points, ink);
		};
		row.AddChild(star);

		return new ChipView { Element = element, Chip = chip, Text = text, Star = star };
	}

	private void OnLoadoutChanged()
	{
		if (rebuildQueued)
			return;
		rebuildQueued = true;
		// Deferred calls flush while the tree is paused, so a pick made in the level-up menu is on
		// the strip the moment the menu closes.
		CallDeferred(MethodName.Rebuild);
	}

	private void Rebuild()
	{
		rebuildQueued = false;
		if (Holder == null || !IsInstanceValid(Holder))
			return;

		IReadOnlyList<SpellData> spells = Holder.GetEquippedSpells();
		int open = Mathf.Clamp(Holder.SpellSlots, 0, MaxSlots);
		SpellData? sovereign = Holder.SovereignSpell;
		int anointedSlot = -1;
		wardSlots = 0;
		// Harmony's slot: the one after a Canticle that holds the card (ArcaneExplosion.SympathyElement's rule).
		int harmonySlot = -1;
		for (int i = 0; i < spells.Count; i++)
		{
			SpellData? held = spells[i];
			if (held != null && held.Id.Equals("arcane_explosion", StringComparison.OrdinalIgnoreCase)
				&& held.HasEvolution(ArcaneExplosion.SympatheticBlastId))
			{
				harmonySlot = i + 1 < MaxSlots ? i + 1 : -1;
				break;
			}
		}
		for (int i = 0; i < MaxSlots; i++)
		{
			SpellData? spell = i < spells.Count ? spells[i] : null;
			// The pages a full book has and this one does not yet, shown quietly so the strip says the book
			// can grow (.ai/slow-start.md 7): Survivor's page opens one of them mid-run. The Folio's seventh
			// is never shown locked - it is a relic, not a page to earn.
			bool locked = spell == null && i >= open && i < Player.MaxSpellSlots;
			FillSlot(slots[i], spell, visible: spell != null || i < open || locked);
			// The dark wizard's seal is the Rite's seal (.ai/endgame-castle.md 6.4): one mark for "this spell is
			// not firing", whoever closed it.
			bool sealedByHim = spell != null && Holder.IsSlotSealed(i);
			SlotMark kind = locked ? SlotMark.Locked
				: spell == null ? SlotMark.None
				: ReferenceEquals(spell, sovereign) ? SlotMark.Sovereign
				: Holder.IsSilenced(spell) || sealedByHim ? SlotMark.Silenced
				: SlotMark.None;
			float drain = sealedByHim && Holder.IsSlotSealTimed(i) ? Holder.SealRemainingFraction(i) : -1f;
			bool drainChanged = !Mathf.IsEqualApprox(slots[i].SealDrain, drain);
			slots[i].SealDrain = drain;
			MarkSlot(slots[i], kind, Holder.SovereignFuel);
			if (drainChanged)
				slots[i].Mark.QueueRedraw();
			// The moment his hand closes on it: the slot flares his wax red and settles under the seal.
			if (sealedByHim && !slots[i].WasSealed)
				FlareSealed(slots[i]);
			Color harmony = Colors.Transparent;
			if (i == harmonySlot && !locked && slots[i].Frame.Visible)
				harmony = spell != null && ArcaneExplosion.HarmonyElementOf(spell) is Element lent
					? ElementColors.GetColor(lent)
					: HarmonyGhost;
			if (slots[i].HarmonyInk != harmony)
			{
				slots[i].HarmonyInk = harmony;
				slots[i].Mark.QueueRedraw();
			}
			slots[i].WasSealed = sealedByHim;
			if (kind == SlotMark.Sovereign)
				anointedSlot = i;
			// A ward the bar shows coming back; a silenced or sealed one is not coming back, so it shows none.
			SpellData? ward = (kind == SlotMark.None || kind == SlotMark.Sovereign) && Player.IsWardSpell(spell) ? spell : null;
			if (!ReferenceEquals(slots[i].WardSpell, ward))
			{
				slots[i].WardSpell = ward;
				slots[i].WardUp = true;
				slots[i].WardFill = -1;
				slots[i].WardInk = ward != null ? ElementColors.GetColor(SfxCatalog.DominantElement(ward)) : Colors.Transparent;
				slots[i].Gauge.QueueRedraw();
			}
			if (ward != null)
				wardSlots++;
		}
		if (Holder.AnyTimedSeal)
			drainClock = DrainRedrawSeconds;
		if (Holder.AnyTimedSeal || wardSlots > 0)
		{
			SetProcess(true);
			TickWards();
		}
		if (sovereign != null && anointedSlot >= 0 && !sovereignArrived)
		{
			sovereignArrived = true;
			FlyIntoSovereign(anointedSlot);
		}

		// Most tags first, the way the old badges and every element list in the menus order them.
		order.Clear();
		foreach (KeyValuePair<Element, int> pair in Holder.GetElementInstanceCounts())
		{
			if (pair.Value > 0)
				order.Add(pair);
		}
		order.Sort((a, b) => a.Value != b.Value
			? b.Value.CompareTo(a.Value)
			: string.CompareOrdinal(a.Key.ToString(), b.Key.ToString()));

		foreach (ChipView view in chips.Values)
			view.Chip.Visible = false;
		for (int i = 0; i < order.Count; i++)
		{
			ChipView view = chips[order[i].Key];
			FillChip(view, order[i].Value);
			tagFlow.MoveChild(view.Chip, i);
		}
	}

	private static void FillSlot(SlotView view, SpellData? spell, bool visible)
	{
		view.Frame.Visible = visible;
		if (spell == null)
		{
			view.Style.BgColor = EmptyBacking;
			view.Style.BorderColor = EmptyRim;
			view.Icon.Texture = null;
			view.Level.Text = string.Empty;
			return;
		}

		Color rim = MilestoneRim(spell);
		view.Style.BorderColor = rim;
		// An evolved spell's backing takes a breath of its rim's hue too, showing through the gaps in
		// the icon - a one-unit rim alone is easy to miss at this size.
		view.Style.BgColor = rim == HeldRim ? SlotBacking : SlotBacking.Lerp(new Color(rim, SlotBacking.A), 0.18f);
		// An evolved spell is shown as it is now: its newest milestone's own picture (EvolutionIcons),
		// at the same 32 canvas units, one per art pixel.
		view.Icon.Texture = EvolutionIcons.Current(spell);
		view.Level.Text = spell.CurrentLevel.ToString();
	}

	// The anointed slot: the gold crown frame and its fuel where the level was. A silenced slot: greyed,
	// its rim the steel shade, sealed. Anything else as FillSlot left it.
	private static void MarkSlot(SlotView view, SlotMark kind, int fuel)
	{
		if (kind == SlotMark.Sovereign)
		{
			view.Style.BorderColor = SovereignRim;
			view.Style.SetBorderWidthAll(2);
			view.Style.BgColor = SlotBacking.Lerp(new Color(SovereignRim, SlotBacking.A), 0.30f);
			view.Level.Text = fuel.ToString();
			view.Level.AddThemeColorOverride("font_color", FuelInk);
			view.Icon.Modulate = Colors.White;
		}
		else
		{
			view.Style.SetBorderWidthAll((int)SlotRim);
			view.Level.AddThemeColorOverride("font_color", NumeralInk);
			view.Icon.Modulate = kind == SlotMark.Silenced ? SilencedIcon : Colors.White;
			if (kind == SlotMark.Locked)
			{
				view.Style.BorderColor = LockedRim;
				view.Style.BgColor = LockedBacking;
			}
			if (kind == SlotMark.Silenced)
			{
				view.Style.BorderColor = SilencedRim;
				view.Style.BgColor = SlotBacking;
			}
		}
		if (view.MarkKind != kind)
		{
			view.MarkKind = kind;
			view.Mark.QueueRedraw();
		}
	}

	// THE MOMENT IT IS TAKEN (section 5): every silenced icon lifts off its slot and flies into the anointed
	// one, the last arriving as the slot flares. A tween on copies laid over the overlay; nothing pauses.
	private void FlyIntoSovereign(int anointedSlot)
	{
		// The overlay is the run's UI canvas layer; a control's global position is a position on it.
		if (GetParent() is not Node overlay)
			return;
		SlotView target = slots[anointedSlot];
		Vector2 to = target.Frame.GlobalPosition + new Vector2(SlotRim, SlotRim);
		float delay = 0f;
		for (int i = 0; i < MaxSlots; i++)
		{
			SlotView from = slots[i];
			if (from.MarkKind != SlotMark.Silenced || from.Icon.Texture == null)
				continue;
			var ghost = new TextureRect
			{
				Name = "SovereignGhost",
				Texture = from.Icon.Texture,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = TextureFilterEnum.Nearest,
				MouseFilter = MouseFilterEnum.Ignore,
				Size = new Vector2(IconSize, IconSize),
				Position = from.Frame.GlobalPosition + new Vector2(SlotRim, SlotRim),
			};
			overlay.AddChild(ghost);
			Tween fly = ghost.CreateTween();
			fly.TweenInterval(delay);
			fly.TweenProperty(ghost, "position", to, 0.55f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			fly.Parallel().TweenProperty(ghost, "modulate", new Color(1f, 0.9f, 0.55f, 1f), 0.55f);
			fly.TweenCallback(Callable.From(ghost.QueueFree));
			delay += 0.12f;
		}
		// The slot flares as the last one lands: its frame lit to the light hot and back.
		Tween flare = target.Frame.CreateTween();
		flare.TweenInterval(delay + 0.43f);
		flare.TweenProperty(target.Frame, "modulate", new Color(1.8f, 1.6f, 1.1f, 1f), 0.08f);
		flare.TweenProperty(target.Frame, "modulate", Colors.White, 0.35f);
	}

	// A ward that is down: the veil over the part of the icon still to come back, from the top, and its edge a line of
	// the spell's own light two units deep. Whole canvas units, one an icon art pixel. Nothing over a ward that is up.
	private static void DrawWard(Control gauge, SlotView view)
	{
		if (view.WardSpell == null || view.WardUp || view.WardFill < 0)
			return;
		float veil = IconSize - view.WardFill;
		if (veil > 0f)
			gauge.DrawRect(new Rect2(SlotRim, SlotRim, IconSize, veil), WardVeil);
		float edge = Mathf.Clamp(SlotRim + veil - 1f, SlotRim, SlotRim + IconSize - 2f);
		gauge.DrawRect(new Rect2(SlotRim, edge, IconSize, 2f), view.WardInk);
	}

	// The moment a ward is back: the slot lit up and settling, in the white of the seal's flare, so the eye goes to it.
	private static void FlareWard(SlotView view)
	{
		Tween flare = view.Frame.CreateTween();
		flare.TweenProperty(view.Frame, "modulate", new Color(1.8f, 1.8f, 2f, 1f), 0.06f);
		flare.TweenProperty(view.Frame, "modulate", Colors.White, 0.4f);
	}

	// A short flare of the seal's wax over the slot and back: what the player sees on the strip the frame
	// the dark wizard's hand takes the spell, whatever else is on screen.
	private static void FlareSealed(SlotView view)
	{
		Tween flare = view.Frame.CreateTween();
		flare.TweenProperty(view.Frame, "modulate", new Color(1.9f, 0.7f, 0.7f, 1f), 0.06f);
		flare.TweenProperty(view.Frame, "modulate", Colors.White, 0.45f);
	}

	private static Color MilestoneRim(SpellData spell)
	{
		if (spell.IsApotheosis)
			return ApotheosisRim;
		if (spell.SelectedLevel8Evolution != null)
			return AscendedRim;
		if (spell.SelectedLevel4Evolution != null)
			return MutatedRim;
		return HeldRim;
	}

	private static void FillChip(ChipView view, int count)
	{
		// The tier thresholds are 2, 4 and 6 (Player.GetElementTier). Below 6 the chip says what
		// every other chip in the game says (ElementPassiveDescriptions.GetProgressLabel: "1/2",
		// "3/4", "4/4"), so the strip and the level-up card never give two numbers for one state.
		// At 6 there is no next threshold: the count and a star.
		bool maxed = count >= 6;
		string text = maxed ? count.ToString() : ElementPassiveDescriptions.GetProgressLabel(count);
		if (view.Text.Text != text)
			view.Text.Text = text;
		view.Star.Visible = maxed;
		view.Chip.Visible = true;
	}
}
