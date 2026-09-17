using System.Collections;
using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace FSpot.Avalonia.Controls;

public partial class TimelineStrip : UserControl
{
	public static readonly StyledProperty<IEnumerable?> ItemsProperty =
		AvaloniaProperty.Register<TimelineStrip, IEnumerable?> (nameof (Items));

	public static readonly StyledProperty<ICommand?> SelectCommandProperty =
		AvaloniaProperty.Register<TimelineStrip, ICommand?> (nameof (SelectCommand));

	public static readonly StyledProperty<ICommand?> RangeCommandProperty =
		AvaloniaProperty.Register<TimelineStrip, ICommand?> (nameof (RangeCommand));

	int? rangeStartIndex;
	bool ranging;

	public TimelineStrip ()
	{
		InitializeComponent ();
		AddHandler (global::Avalonia.Input.InputElement.PointerPressedEvent, OnRangePressed, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
		AddHandler (global::Avalonia.Input.InputElement.PointerReleasedEvent, OnRangeReleased,
			global::Avalonia.Interactivity.RoutingStrategies.Tunnel | global::Avalonia.Interactivity.RoutingStrategies.Bubble,
			handledEventsToo: true);
	}

	public IEnumerable? Items {
		get => GetValue (ItemsProperty);
		set => SetValue (ItemsProperty, value);
	}

	public ICommand? SelectCommand {
		get => GetValue (SelectCommandProperty);
		set => SetValue (SelectCommandProperty, value);
	}

	public ICommand? RangeCommand {
		get => GetValue (RangeCommandProperty);
		set => SetValue (RangeCommandProperty, value);
	}

	void OnRangePressed (object? sender, global::Avalonia.Input.PointerPressedEventArgs e)
	{
		if (!e.KeyModifiers.HasFlag (global::Avalonia.Input.KeyModifiers.Shift))
			return;
		if (IndexAt (e.GetPosition (this)) is not int index)
			return;
		ranging = true;
		rangeStartIndex = index;
		e.Pointer.Capture (this);
		e.Handled = true;
	}

	void OnRangeReleased (object? sender, global::Avalonia.Input.PointerReleasedEventArgs e)
	{
		if (!ranging)
			return;
		ranging = false;
		e.Pointer.Capture (null);
		e.Handled = true;
		if (rangeStartIndex is int start && IndexAt (e.GetPosition (this)) is int end)
			RangeCommand?.Execute ((start, end));
		rangeStartIndex = null;
	}

	internal int? IndexAt (Point position)
	{
		if (Items is null)
			return null;
		var buckets = Items.Cast<object> ().ToList ();
		foreach (var descendant in this.GetVisualDescendants ().OfType<Control> ()) {
			if (descendant.DataContext is null)
				continue;
			int index = buckets.IndexOf (descendant.DataContext);
			if (index < 0)
				continue;
			var origin = descendant.TranslatePoint (default, this);
			if (origin is null)
				continue;
			if (new Rect (origin.Value, descendant.Bounds.Size).Contains (position))
				return index;
		}
		return null;
	}
}
