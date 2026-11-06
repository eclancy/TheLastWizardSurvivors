using Godot;
using System.Collections.Generic;

/// <summary>One device state for the cursor, menu hover and tooltips.</summary>
public partial class CursorVisibility : Node
{
	private static CursorVisibility instance;
	private readonly Dictionary<Control, Control.MouseFilterEnum> filters = new();
	private bool pointerActive = true;

	public override void _Ready()
	{
		instance = this;
		ProcessMode = ProcessModeEnum.Always;
		MenuFocusHighlight.PointerActive = true;
		GetTree().NodeAdded += OnNodeAdded;
		GetTree().NodeRemoved += OnNodeRemoved;
	}

	public static void SetPointerActive(bool active)
	{
		if (instance != null)
			instance.ApplyPointer(active);
		else
			MenuFocusHighlight.PointerActive = active;
	}

	private void ApplyPointer(bool active)
	{
		if (pointerActive == active)
			return;
		pointerActive = active;
		MenuFocusHighlight.PointerActive = active;
		// MouseFilter does not affect keyboard focus. Ignoring the pointer prevents a stationary
		// mouse from highlighting newly opened cards, stealing focus or reopening native tooltips.
		if (active)
			RestoreFilters();
		else if (!OS.HasFeature("mobile"))
			DisablePointer(GetTree().Root);
		MenuFocusHighlight.RefreshAll(GetTree().Root);
		UpdateCursor();
	}

	private void DisablePointer(Node node)
	{
		if (node is Control control && control.MouseFilter != Control.MouseFilterEnum.Ignore)
		{
			filters[control] = control.MouseFilter;
			control.MouseFilter = Control.MouseFilterEnum.Ignore;
		}
		foreach (Node child in node.GetChildren(true))
			DisablePointer(child);
	}

	private void OnNodeAdded(Node node)
	{
		if (pointerActive || OS.HasFeature("mobile") || node is not Control)
			return;
		DisablePointer(node);
		// Builders and _Ready may set a filter after AddChild. Recheck once, without scanning
		// every menu each frame. A pointer event arriving first cancels this deferred work.
		Callable.From(() =>
		{
			if (!pointerActive && IsInstanceValid(node) && node.IsInsideTree())
				DisablePointer(node);
		}).CallDeferred();
	}

	private void OnNodeRemoved(Node node)
	{
		if (node is Control control && filters.Remove(control, out var original) && IsInstanceValid(control))
			control.MouseFilter = original;
	}

	private void RestoreFilters()
	{
		foreach (var pair in filters)
			if (IsInstanceValid(pair.Key))
				pair.Key.MouseFilter = pair.Value;
		filters.Clear();
	}

	public override void _Input(InputEvent @event) => ObserveInput(@event);

	// Navigators call this too, before changing device state. Godot dispatches child _Input
	// before autoloads, and a menu can consume an event before it reaches this node.
	public static bool ObserveInput(InputEvent @event)
	{
		bool wakingClick = instance != null && !instance.pointerActive
			&& @event is InputEventMouseButton { Pressed: true } && !OS.HasFeature("mobile");
		switch (@event)
		{
			case InputEventKey { Pressed: true }:
			case InputEventJoypadButton { Pressed: true }:
			case InputEventJoypadMotion motion when Mathf.Abs(motion.AxisValue) >= 0.5f:
				SetPointerActive(false);
				break;
			case InputEventMouseMotion mouse when mouse.Relative.LengthSquared() >= 9f:
			case InputEventMouseButton { Pressed: true }:
			case InputEventScreenTouch { Pressed: true }:
				// Before GUI dispatch, so the first click/tap works while filters are off.
				SetPointerActive(true);
				break;
		}
		if (!wakingClick) return false;
		// Native buttons require mouse-enter before accepting a click. The viewport picked hover
		// before _Input, while filters were disabled. Refresh it, then dispatch this click once in
		// viewport coordinates. The nested motion marks input handled, so explicitly replaying the
		// click is necessary; consuming its original prevents a double purchase.
		Viewport viewport = instance.GetViewport();
		viewport.UpdateMouseCursorState();
		viewport.PushInput(@event, true);
		viewport.SetInputAsHandled();
		return true;
	}

	private void UpdateCursor()
	{
		if (OS.HasFeature("mobile"))
			return;
		var wanted = pointerActive && !Node2DGame.InPlay ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Hidden;
		if (Input.MouseMode != wanted)
			Input.MouseMode = wanted;
	}

	public override void _Process(double delta) => UpdateCursor();

	public override void _ExitTree()
	{
		GetTree().NodeAdded -= OnNodeAdded;
		GetTree().NodeRemoved -= OnNodeRemoved;
		RestoreFilters();
		instance = null;
	}
}
