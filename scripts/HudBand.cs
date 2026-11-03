using Godot;
using System;
using System.Collections.Generic;
using TheOneHeMissed.scripts;

// THE BAND UNDER THE RUN TIMER, and the one authority on where its rows go.
//
// Five kinds of thing hang from the timer, centred or across the HUD: the boss bar, a chapter's own line
// (ChapterRules hook 7: the Court's rooms found, the Last Bridge's strip), a side event's objective line
// (or its announcement, or its win), the run banner (the stage intro, a boss's name) and a new player's
// onboarding tip. Each used to be given a fixed slot of its own, and the slots were drawn for the rows one
// at a time: the tip's (82-128) and the event line's (84-112) were the same place, so a new player who
// found an event in his first minutes read "Tip: Prioritize one" printed through "Stand inside the grove";
// the Court's event line was moved one line down by hand under the chapter's; the boss bar and the tip
// were stepped down by hand under wrapped element tags; the Last Bridge hid its strip while a tip was up.
// Every new row was another exception placed by hand, and a chapter can now run two events.
//
// THE RULE: the band is a STACK, in a fixed order (Row), top to bottom. A row that is showing sits at its
// resting place or under the foot of the showing row above it, whichever is lower, and never on anything
// fixed in the band - the element tags where they wrap (a phone with six elements or more) and, on a phone,
// the spell row across the band's floor, which a row that would reach it jumps.
// A row that is not showing takes no room. So no two rows can overlap at either canvas, however many are up.
// The order is the reading order: what stays (the boss's bar, the lines the player is following) is above
// what comes and goes (a banner, a tip), so it is a message that moves to make room, never a line.
//
// Air: a box (the bar, the banner, the tags, the spell row) has Gap under it; a line of text has none,
// because its line box already carries the font's leading. That is what lets the Court's two lines sit
// between the boss bar's slot and the phone's spell row (54 units for two 26-unit lines) as they always did,
// rather than the second jumping the row.
//
// NOT IN IT: the run timer. Its Body-sized digits in a padded panel make it 8-52, not the 8-36 it was laid
// out as, so the boss bar resting at 44 and the banner at 42 overlap the foot of its box (its padding, not its
// digits). Counting it would put the bar at 58, and a PC's spell column starts at 84 under the bar's slot -
// the bar would land on the spells for the whole fight. The rows rest under the timer as they were drawn.
//
// Rows register as they are made (Add) - after SafeArea has inset them, so where they stand then is their
// resting place - and drop out when they are freed. Laid out every frame, late (ProcessPriority, after every
// node that shows, hides or rewrites a row), and again the moment a banner or a tip is put up. Nothing
// allocates. Stack is the rule on plain numbers, so the validator (Validate) runs it with no run on screen,
// and _UiShot measures the real rows at every shutter over a run (Audit).
public partial class HudBand : Node
{
	/// <summary>The band's rows, in the order they stack, top to bottom.</summary>
	public enum Row
	{
		/// <summary>The boss's health (Node2DGame.ConfigureBossHud), or the sent bosses' bars side by side.</summary>
		BossBar,
		/// <summary>A chapter's own line (ChapterRules.CreateObjective): the Court's rooms found, the Last Bridge's strip.</summary>
		ChapterLine,
		/// <summary>A side event's objective, announcement or win line (SideEvent). Two events share it; one speaks at a time.</summary>
		EventLine,
		/// <summary>The run banner (Node2DGame.ShowRunBanner): the stage intro, a boss's arrival, the way home.</summary>
		Banner,
		/// <summary>A new player's onboarding tip.</summary>
		Tip,
	}

	/// <summary>
	/// Air under a box - a bar, a panel, anything fixed - before the next row: the boss bar's foot (78) to the
	/// lines' resting place (84), which is the spacing the band was drawn with. A line of text has none under it.
	/// </summary>
	public const float Gap = 6f;

	/// <summary>One row on plain numbers: where it rests, how tall and how wide it is, a floor of its own, and where it went.</summary>
	public struct Slot
	{
		public bool Showing;
		public float RestTop;
		public float Height;
		/// <summary>Air under it: <see cref="Gap"/> under a box, 0 under a line of text.</summary>
		public float GapAfter;
		public float Left;
		public float Right;
		/// <summary>A row's own floor besides the stack's (the banner's: under a filmed arrival's letterbox).</summary>
		public float Floor;
		/// <summary>Set by <see cref="Stack"/>.</summary>
		public float Top;
	}

	private struct Entry
	{
		public Row Row;
		public Control Control;
		public float RestTop;
		public float RestHeight;
	}

	private readonly List<Entry> entries = new();
	private Slot[] slots = new Slot[8];
	private readonly Rect2[] fixedRects = new Rect2[2];

	/// <summary>The loadout: its element tags (upper right) and, on a phone, its spell row across the band's floor.</summary>
	public LoadoutHud? Loadout { get; set; }

	/// <summary>The banner's own floor, asked each layout: under a filmed arrival's letterbox, and on a phone under the spell row.</summary>
	public Func<float>? BannerFloor { get; set; }

	public override void _Ready()
	{
		// After the rows' owners have run this frame (SideEvent, the chapter's rules, Node2DGame), so a row is
		// never drawn for one frame where it stood before something came up above it. Always, because the
		// tags can grow under a paused level-up (LoadoutHud rebuilds deferred) and the bar must be under them
		// on the first frame back.
		ProcessPriority = 1000;
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Process(double delta) => Layout();

	/// <summary>
	/// Puts a row in the band, after any already in it of the same kind. Call it once the control is on the
	/// overlay and inset (SafeArea.ApplyTo, or the HUD's SafeArea.Apply): where it stands then is where it rests.
	/// </summary>
	public void Add(Row row, Control control)
	{
		if (control == null)
			return;
		var entry = new Entry
		{
			Row = row,
			Control = control,
			RestTop = control.OffsetTop,
			RestHeight = control.OffsetBottom - control.OffsetTop,
		};
		int at = entries.Count;
		while (at > 0 && entries[at - 1].Row > row)
			at--;
		entries.Insert(at, entry);
		Layout();
	}

	/// <summary>Stacks the rows that are showing now. Cheap; called every frame and when a banner or a tip goes up.</summary>
	public void Layout()
	{
		for (int i = entries.Count - 1; i >= 0; i--)
		{
			Control gone = entries[i].Control;
			if (!IsInstanceValid(gone) || gone.IsQueuedForDeletion())
				entries.RemoveAt(i);
		}
		if (slots.Length < entries.Count)
			slots = new Slot[entries.Count * 2];

		for (int i = 0; i < entries.Count; i++)
		{
			Entry e = entries[i];
			bool showing = IsShowing(e.Control);
			Rect2 rect = showing ? e.Control.GetGlobalRect() : default;
			slots[i] = new Slot
			{
				Showing = showing,
				RestTop = e.RestTop,
				Height = showing ? HeightOf(e) : 0f,
				GapAfter = e.Control is Label ? 0f : Gap,
				Left = rect.Position.X,
				Right = rect.End.X,
				Floor = e.Row == Row.Banner && BannerFloor != null ? BannerFloor() : 0f,
			};
		}

		Stack(slots.AsSpan(0, entries.Count), fixedRects.AsSpan(0, GatherFixed()));

		for (int i = 0; i < entries.Count; i++)
		{
			if (!slots[i].Showing)
				continue;
			Control c = entries[i].Control;
			// A label's box is its text (its minimum height, wrapped at its width); anything else keeps the
			// height it was built with and grows past it only if its content asks.
			float top = slots[i].Top;
			float bottom = top + (c is Label ? slots[i].Height : entries[i].RestHeight);
			if (Mathf.Abs(c.OffsetTop - top) > 0.01f || Mathf.Abs(c.OffsetBottom - bottom) > 0.01f)
			{
				c.OffsetTop = top;
				c.OffsetBottom = bottom;
			}
		}
	}

	/// <summary>
	/// THE RULE, on plain numbers: each showing slot, in order, at the lowest of its resting place, the foot of
	/// the showing slot above it and its own floor - then stepped under any fixed rect it would meet across its
	/// width. Only ever moves a slot down, so a slot never rises over the one above it.
	/// </summary>
	public static void Stack(Span<Slot> stack, ReadOnlySpan<Rect2> fixedRects)
	{
		float floor = float.MinValue;
		for (int i = 0; i < stack.Length; i++)
		{
			ref Slot s = ref stack[i];
			if (!s.Showing)
				continue;
			float top = Mathf.Max(s.RestTop, Mathf.Max(floor, s.Floor));
			// Each fixed rect can move it at most once (it ends below it), so this ends.
			bool moved = true;
			while (moved)
			{
				moved = false;
				foreach (Rect2 f in fixedRects)
				{
					if (f.Size.Y <= 0f || f.End.X <= s.Left || f.Position.X >= s.Right)
						continue;
					if (top < f.End.Y + Gap && top + s.Height + s.GapAfter > f.Position.Y)
					{
						top = f.End.Y + Gap;
						moved = true;
					}
				}
			}
			s.Top = top;
			floor = top + s.Height + s.GapAfter;
		}
	}

	// Showing: on screen with something in it. An event that does not hold the line (SideEvent.HideObjectiveLine)
	// is faded to nothing rather than hidden, and a dormant one's line is empty; neither takes room. A label's
	// emptiness is read as its character count, not its Text, which would marshal a new string every frame.
	private static bool IsShowing(Control c) =>
		c.IsVisibleInTree() && c.Modulate.A > 0.004f && c.SelfModulate.A > 0.004f
		&& !(c is Label label && label.GetTotalCharacterCount() == 0);

	private static float HeightOf(Entry e)
	{
		float content = e.Control.GetCombinedMinimumSize().Y;
		return e.Control is Label ? content : Mathf.Max(e.RestHeight, content);
	}

	private int GatherFixed()
	{
		int n = 0;
		if (Loadout != null && IsInstanceValid(Loadout))
		{
			if (Loadout.TagRow is Control tags && tags.IsVisibleInTree())
				fixedRects[n++] = tags.GetGlobalRect();
			// On a phone the spell row runs across the band's floor. On a PC it is a column down the left,
			// which no centred row reaches - and a full-width line's box would, so it is not counted there.
			if (Loadout.IsVisibleInTree() && ResponsiveLayout.IsNarrow(Loadout))
				fixedRects[n++] = Loadout.GetGlobalRect();
		}
		return n;
	}

	/// <summary>
	/// For _UiShot: every showing row and fixed rect as measured on screen now, and the first pair that meets,
	/// or null when none does. Rows are measured by their boxes, which the band sets to what it reserved.
	/// </summary>
	public (string Rows, string? Overlap) Audit()
	{
		var named = new List<(string Name, Rect2 Rect, bool Fixed)>();
		foreach (Entry e in entries)
		{
			if (IsInstanceValid(e.Control) && IsShowing(e.Control))
				named.Add(($"{e.Row} {e.Control.Name}", e.Control.GetGlobalRect(), false));
		}
		int fixedCount = GatherFixed();
		for (int i = 0; i < fixedCount; i++)
			named.Add(($"fixed {i}", fixedRects[i], true));

		var rows = new System.Text.StringBuilder();
		foreach (var (name, rect, _) in named)
			rows.Append($"{(rows.Length > 0 ? "; " : "")}{name} y {rect.Position.Y:0}..{rect.End.Y:0} x {rect.Position.X:0}..{rect.End.X:0}");

		for (int a = 0; a < named.Count; a++)
		{
			for (int b = a + 1; b < named.Count; b++)
			{
				if (named[a].Fixed && named[b].Fixed)
					continue;
				Rect2 ra = named[a].Rect, rb = named[b].Rect;
				if (ra.Size.Y > 0f && rb.Size.Y > 0f && ra.Intersects(rb))
					return (rows.ToString(), $"{named[a].Name} and {named[b].Name}");
			}
		}
		return (rows.ToString(), null);
	}

	/// <summary>
	/// The validator's check (RegressionChecks.RunAll): the rule run on every row at once, at both canvases,
	/// with the tags wrapped as deep as twelve elements take them on a phone and the spell row across its
	/// band - the worst the band is ever asked to hold - must leave no two rows meeting and no row on anything fixed.
	/// </summary>
	public static void Validate(List<string> warnings)
	{
		// Canvas width, the safe inset's left, top and right, and whether the spell row is across the band.
		foreach (var (name, width, insetLeft, insetTop, insetRight, narrow) in new[]
		{
			("PC 1280x720", 1280f, 0f, 0f, 0f, false),
			("phone 720x1280", 720f, SafeArea.MinSide, SafeArea.MinTop, SafeArea.MinSide, true),
		})
		{
			float mid = width * 0.5f;
			var fixedRects = new List<Rect2>();
			if (narrow)
			{
				// The tags: twelve chips, five a line in a 214-wide block left of the pause button, three lines;
				// and the spell row on the band's floor (LoadoutHud.Place).
				fixedRects.Add(new Rect2(width - insetRight - 70f - 214f, 8f + insetTop, 214f, 3f * 26f));
				fixedRects.Add(new Rect2(7f + insetLeft, TouchControls.DefaultHudBandHeight - 4f - 34f + insetTop, width - 14f - insetLeft - insetRight, 34f));
			}
			else
			{
				fixedRects.Add(new Rect2(width - 7f - 560f, 8f, 560f, 26f));
			}

			// Every row up at once, at the size its tallest content gives it: a two-line win line in the title face,
			// a two-line banner and tip. The bar and the banner are boxes, the rest lines of text.
			Slot[] rows =
			{
				new() { Showing = true, RestTop = 44f + insetTop, Height = 34f, GapAfter = Gap, Left = 24f + insetLeft, Right = width - 24f - insetRight },
				new() { Showing = true, RestTop = 84f + insetTop, Height = 26f, Left = insetLeft, Right = width - insetRight },
				new() { Showing = true, RestTop = 84f + insetTop, Height = 80f, Left = insetLeft, Right = width - insetRight },
				new() { Showing = true, RestTop = 42f + insetTop, Height = 62f, GapAfter = Gap, Left = mid - 260f, Right = mid + 260f },
				new() { Showing = true, RestTop = 82f + insetTop, Height = 55f, Left = mid - 320f, Right = mid + 320f },
			};
			Stack(rows, fixedRects.ToArray());

			for (int a = 0; a < rows.Length; a++)
			{
				var ra = new Rect2(rows[a].Left, rows[a].Top, rows[a].Right - rows[a].Left, rows[a].Height);
				if (rows[a].Top < rows[a].RestTop)
					warnings.Add($"HUD band ({name}): {(Row)a} rose above its resting place ({rows[a].Top:0} < {rows[a].RestTop:0}).");
				for (int b = a + 1; b < rows.Length; b++)
				{
					var rb = new Rect2(rows[b].Left, rows[b].Top, rows[b].Right - rows[b].Left, rows[b].Height);
					if (ra.Intersects(rb))
						warnings.Add($"HUD band ({name}): {(Row)a} ({ra.Position.Y:0}..{ra.End.Y:0}) meets {(Row)b} ({rb.Position.Y:0}..{rb.End.Y:0}).");
				}
				foreach (Rect2 f in fixedRects)
				{
					if (ra.Intersects(f))
						warnings.Add($"HUD band ({name}): {(Row)a} ({ra.Position.Y:0}..{ra.End.Y:0}) is drawn on a fixed piece of the HUD at {f.Position.Y:0}..{f.End.Y:0}.");
				}
			}
		}
	}
}
