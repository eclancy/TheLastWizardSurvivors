using Godot;
using System.Collections.Generic;

/// <summary>
/// The Codex book's family-tree lines (.ai/evolution-codex.md section 10, layout B): from a node's foot down
/// to a bus just above the next tier, across, and down into each node it leads to, with an arrowhead. Drawn
/// BEHIND the tree - the first child of a holder that fits every child to its rect - so a line passing a
/// tier's heading or a chip goes under the words, never over them.
/// </summary>
/// <remarks>
/// Every point is measured from the nodes' real rects and it redraws when they move, as AscensionForkArrows
/// does, so a node that wraps to an extra line cannot leave a line pointing at the gap. Solid in an ink for
/// what is open or taken (the gold shade), dashed in the quiet ink for what is not, and the way to an
/// Apotheosis in the ember line on top of both. No glow and no spark: this is ink on a page.
/// </remarks>
public partial class CodexTreeLines : Control
{
	private const float Width = 3f;
	private const float DashedWidth = 2f;
	private const float DashOn = 6f;
	private const float DashOff = 5f;
	private const float HeadHalf = 6f;
	private const float HeadLength = 8f;
	/// <summary>How far above a node its bus runs.</summary>
	private const float BusLift = 12f;

	private readonly List<(Control From, Control To, Color Ink, bool Dashed, int Layer)> edges = new();
	private readonly Vector2[] head = new Vector2[3];
	private int settleFrames;

	/// <summary>A line from <paramref name="from"/>'s foot into <paramref name="to"/>'s head. Higher layers are drawn later.</summary>
	public void AddEdge(Control from, Control to, Color ink, bool dashed, int layer = 0)
	{
		if (from == null || to == null)
			return;
		edges.Add((from, to, ink, dashed, layer));
	}

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		FocusMode = FocusModeEnum.None;
		// The level-up and pause screens pause the tree; the lines must still settle under them.
		ProcessMode = ProcessModeEnum.Always;
		Resized += QueueRedraw;
		// Once per node: a node that leads to three others is one connection, not three.
		foreach (Control node in Watched())
			node.ItemRectChanged += QueueRedraw;
		// A node can move without its own rect changing (inside a row whose siblings resize), so it also
		// redraws for its first frames, until every container has settled.
		settleFrames = 30;
	}

	public override void _ExitTree()
	{
		foreach (Control node in Watched())
			if (IsInstanceValid(node))
				node.ItemRectChanged -= QueueRedraw;
	}

	private HashSet<Control> Watched()
	{
		var nodes = new HashSet<Control>();
		foreach (var edge in edges)
		{
			nodes.Add(edge.From);
			nodes.Add(edge.To);
		}
		return nodes;
	}

	public override void _Process(double delta)
	{
		if (settleFrames <= 0)
		{
			SetProcess(false);
			return;
		}
		settleFrames--;
		QueueRedraw();
	}

	public override void _Draw()
	{
		for (int layer = 0; layer <= 2; layer++)
		{
			// Dashed first inside a layer, so a solid line sharing a bus is drawn over it.
			foreach (bool dashed in new[] { true, false })
			{
				foreach (var edge in edges)
				{
					if (edge.Layer != layer || edge.Dashed != dashed || !IsInstanceValid(edge.From) || !IsInstanceValid(edge.To)
						|| !edge.From.IsVisibleInTree() || !edge.To.IsVisibleInTree())
						continue;
					DrawEdge(edge.From, edge.To, edge.Ink, edge.Dashed);
				}
			}
		}
	}

	private Vector2 Local(Control control, float fx, float fy)
	{
		Rect2 rect = control.GetGlobalRect();
		return (rect.Position + new Vector2(rect.Size.X * fx, rect.Size.Y * fy) - GlobalPosition).Floor();
	}

	private void DrawEdge(Control from, Control to, Color ink, bool dashed)
	{
		Vector2 a = Local(from, 0.5f, 1f);
		Vector2 b = Local(to, 0.5f, 0f);
		float bus = Mathf.Floor(Mathf.Max(a.Y + 4f, b.Y - BusLift));
		float width = dashed ? DashedWidth : Width;
		Segment(new Vector2(a.X, a.Y), new Vector2(a.X, bus), width, ink, dashed);
		Segment(new Vector2(a.X, bus), new Vector2(b.X, bus), width, ink, dashed);
		Segment(new Vector2(b.X, bus), new Vector2(b.X, b.Y - HeadLength), width, ink, dashed);
		head[0] = b;
		head[1] = new Vector2(b.X - HeadHalf, b.Y - HeadLength);
		head[2] = new Vector2(b.X + HeadHalf, b.Y - HeadLength);
		DrawColoredPolygon(head, ink);
	}

	// Axis-aligned, on whole pixels; a dashed one in DashOn/DashOff steps.
	private void Segment(Vector2 p, Vector2 q, float width, Color ink, bool dashed)
	{
		float half = Mathf.Floor(width * 0.5f);
		if (Mathf.IsEqualApprox(p.X, q.X))
		{
			float top = Mathf.Min(p.Y, q.Y), bottom = Mathf.Max(p.Y, q.Y);
			for (float y = top; y < bottom; y += dashed ? DashOn + DashOff : bottom - top)
				DrawRect(new Rect2(p.X - half, y, width, Mathf.Min(dashed ? DashOn : bottom - top, bottom - y)), ink, true);
		}
		else
		{
			float left = Mathf.Min(p.X, q.X), right = Mathf.Max(p.X, q.X);
			// Overlapping the corner by half a width, so an elbow is square rather than notched.
			left -= half;
			right += half + 1f;
			for (float x = left; x < right; x += dashed ? DashOn + DashOff : right - left)
				DrawRect(new Rect2(x, p.Y - half, Mathf.Min(dashed ? DashOn : right - left, right - x), width), ink, true);
		}
	}
}
