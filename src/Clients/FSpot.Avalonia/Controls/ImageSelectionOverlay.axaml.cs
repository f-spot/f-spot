using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;

using FSpot;
using FSpot.Imaging;

namespace FSpot.Avalonia.Controls;

public partial class ImageSelectionOverlay : UserControl
{
	public static readonly StyledProperty<int> ImageWidthProperty =
		AvaloniaProperty.Register<ImageSelectionOverlay, int> (nameof (ImageWidth));

	public static readonly StyledProperty<int> ImageHeightProperty =
		AvaloniaProperty.Register<ImageSelectionOverlay, int> (nameof (ImageHeight));

	public static readonly StyledProperty<ICommand?> SelectionCommandProperty =
		AvaloniaProperty.Register<ImageSelectionOverlay, ICommand?> (nameof (SelectionCommand));

	public static readonly StyledProperty<Bitmap?> PreviewProperty =
		AvaloniaProperty.Register<ImageSelectionOverlay, Bitmap?> (nameof (Preview));

	public static readonly StyledProperty<double> ZoomProperty =
		AvaloniaProperty.Register<ImageSelectionOverlay, double> (nameof (Zoom), 1);

	public static readonly StyledProperty<double> PanXProperty =
		AvaloniaProperty.Register<ImageSelectionOverlay, double> (nameof (PanX));

	public static readonly StyledProperty<double> PanYProperty =
		AvaloniaProperty.Register<ImageSelectionOverlay, double> (nameof (PanY));

	Point dragStart;
	bool dragging;

	public ImageSelectionOverlay ()
	{
		InitializeComponent ();
	}

	public int ImageWidth {
		get => GetValue (ImageWidthProperty);
		set => SetValue (ImageWidthProperty, value);
	}

	public int ImageHeight {
		get => GetValue (ImageHeightProperty);
		set => SetValue (ImageHeightProperty, value);
	}

	public ICommand? SelectionCommand {
		get => GetValue (SelectionCommandProperty);
		set => SetValue (SelectionCommandProperty, value);
	}

	public Bitmap? Preview {
		get => GetValue (PreviewProperty);
		set => SetValue (PreviewProperty, value);
	}

	public double Zoom {
		get => GetValue (ZoomProperty);
		set => SetValue (ZoomProperty, value);
	}

	public double PanX {
		get => GetValue (PanXProperty);
		set => SetValue (PanXProperty, value);
	}

	public double PanY {
		get => GetValue (PanYProperty);
		set => SetValue (PanYProperty, value);
	}

	void OnPointerPressed (object? sender, PointerPressedEventArgs e)
	{
		if (!e.GetCurrentPoint (this).Properties.IsLeftButtonPressed)
			return;
		dragging = true;
		dragStart = e.GetPosition (this);
		e.Pointer.Capture (this);
		e.Handled = true;
		ShowMarquee (dragStart, dragStart);
	}

	void OnPointerMoved (object? sender, PointerEventArgs e)
	{
		if (!dragging)
			return;
		ShowMarquee (dragStart, e.GetPosition (this));
	}

	void OnPointerReleased (object? sender, PointerReleasedEventArgs e)
	{
		if (!dragging)
			return;
		dragging = false;
		e.Pointer.Capture (null);
		e.Handled = true;
		CompleteSelection (dragStart, e.GetPosition (this));
	}

	internal void CompleteSelection (Point start, Point end)
	{
		ShowMarquee (start, end);
		var selection = MapSelection (start, end);
		if (!selection.IsEmpty)
			SelectionCommand?.Execute (selection);
	}

	internal FSpot.Imaging.PixelRect MapSelection (Point start, Point end)
	{
		int viewWidth = (int)Bounds.Width;
		int viewHeight = (int)Bounds.Height;
		if (viewWidth <= 0)
			viewWidth = (int)Width;
		if (viewHeight <= 0)
			viewHeight = (int)Height;
		int imageWidth = Preview?.PixelSize.Width ?? ImageWidth;
		int imageHeight = Preview?.PixelSize.Height ?? ImageHeight;
		return PhotoViewTransform.MapRect (
			viewWidth, viewHeight, imageWidth, imageHeight,
			Zoom, PanX, PanY, start.X, start.Y, end.X, end.Y);
	}

	void ShowMarquee (Point start, Point end)
	{
		double x = Math.Min (start.X, end.X);
		double y = Math.Min (start.Y, end.Y);
		double width = Math.Abs (end.X - start.X);
		double height = Math.Abs (end.Y - start.Y);
		Canvas.SetLeft (Marquee, x);
		Canvas.SetTop (Marquee, y);
		Marquee.Width = width;
		Marquee.Height = height;
		Marquee.IsVisible = width > 1 && height > 1;
	}
}
