using System.Collections;
using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FSpot.Avalonia.Controls;

public partial class TimelineStrip : UserControl
{
	public static readonly StyledProperty<IEnumerable?> ItemsProperty =
		AvaloniaProperty.Register<TimelineStrip, IEnumerable?> (nameof (Items));

	public static readonly StyledProperty<ICommand?> SelectCommandProperty =
		AvaloniaProperty.Register<TimelineStrip, ICommand?> (nameof (SelectCommand));

	public static readonly StyledProperty<ICommand?> RangeCommandProperty =
		AvaloniaProperty.Register<TimelineStrip, ICommand?> (nameof (RangeCommand));

	public TimelineStrip ()
	{
		InitializeComponent ();
		Scroller.PropertyChanged += OnScrollerPropertyChanged;
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

	public double ColumnWidth => Scale.ColumnWidth;

	public int IndexAt (Point position)
	{
		var origin = Scale.TranslatePoint (default, this);
		var local = origin is Point point ? position - point : position;
		return Scale.IndexAt (local);
	}

	protected override Size ArrangeOverride (Size finalSize)
	{
		var size = base.ArrangeOverride (finalSize);
		UpdateArrows ();
		return size;
	}

	protected override void OnKeyDown (KeyEventArgs e)
	{
		base.OnKeyDown (e);
		int direction = e.Key switch {
			Key.Left => -1,
			Key.Right => 1,
			_ => 0
		};
		if (direction != 0 && Scale.Step (direction))
			e.Handled = true;
	}

	void OnScrollLeft (object? sender, RoutedEventArgs e)
		=> ScrollBy (-1);

	void OnScrollRight (object? sender, RoutedEventArgs e)
		=> ScrollBy (1);

	void ScrollBy (int direction)
	{
		double step = Scale.ColumnWidth >= 1 ? Scale.ColumnWidth : TimelineLayout.ArrowWidth;
		double max = Math.Max (0, Scroller.Extent.Width - Scroller.Viewport.Width);
		double next = Math.Clamp (Scroller.Offset.X + step * direction, 0, max);
		if (Math.Abs (next - Scroller.Offset.X) < 0.5)
			return;
		Scroller.Offset = new global::Avalonia.Vector (next, 0);
	}

	void OnScrollerPropertyChanged (object? sender, AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == ScrollViewer.OffsetProperty
			|| change.Property == ScrollViewer.ExtentProperty
			|| change.Property == ScrollViewer.ViewportProperty)
			UpdateArrows ();
	}

	void UpdateArrows ()
	{
		double max = Math.Max (0, Scroller.Extent.Width - Scroller.Viewport.Width);
		ScrollLeft.IsEnabled = Scroller.Offset.X > 0.5;
		ScrollRight.IsEnabled = Scroller.Offset.X < max - 1;
	}
}
