using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;

using FSpot;
using FSpot.Editors;
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

	// GTK ImageView.SelectionXyRatio: 0 is unconstrained; otherwise the
	// drag is clamped to this aspect (flipped to match its orientation).
	public static readonly StyledProperty<double> SelectionXyRatioProperty =
		AvaloniaProperty.Register<ImageSelectionOverlay, double> (nameof (SelectionXyRatio));

	Point dragStart;
	bool dragging;
	FSpot.Imaging.PixelRect current;

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

	public double SelectionXyRatio {
		get => GetValue (SelectionXyRatioProperty);
		set => SetValue (SelectionXyRatioProperty, value);
	}

	protected override void OnPropertyChanged (AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged (change);
		// GTK re-constrains the live selection when the ratio changes.
		if (change.Property == SelectionXyRatioProperty && !current.IsEmpty && !dragging) {
			var constrained = Constrain (current);
			ShowSelection (constrained);
			if (!constrained.IsEmpty)
				SelectionCommand?.Execute (constrained);
		} else if (change.Property == PreviewProperty && !ReferenceEquals (change.OldValue, change.NewValue)) {
			current = default;
			Marquee.IsVisible = false;
			SelectionCommand?.Execute (current);
		}
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

	void OnPointerCaptureLost (object? sender, PointerCaptureLostEventArgs e)
	{
		if (!dragging)
			return;
		dragging = false;
		if (current.IsEmpty)
			Marquee.IsVisible = false;
		else
			ShowSelection (current);
	}

	internal void CompleteSelection (Point start, Point end)
	{
		ShowMarquee (start, end);
		var selection = Constrain (MapSelection (start, end));
		current = selection;
		SelectionCommand?.Execute (selection);
	}

	internal FSpot.Imaging.PixelRect MapSelection (Point start, Point end)
	{
		var (viewWidth, viewHeight) = ViewSize ();
		var (imageWidth, imageHeight) = ImageSize ();
		return PhotoViewTransform.MapRect (
			viewWidth, viewHeight, imageWidth, imageHeight,
			Zoom, PanX, PanY, start.X, start.Y, end.X, end.Y);
	}

	FSpot.Imaging.PixelRect Constrain (FSpot.Imaging.PixelRect selection)
	{
		var (imageWidth, imageHeight) = ImageSize ();
		return CropConstraints.Constrain (selection, SelectionXyRatio, imageWidth, imageHeight);
	}

	(int Width, int Height) ViewSize ()
	{
		int viewWidth = (int)Bounds.Width;
		int viewHeight = (int)Bounds.Height;
		if (viewWidth <= 0)
			viewWidth = (int)Width;
		if (viewHeight <= 0)
			viewHeight = (int)Height;
		return (viewWidth, viewHeight);
	}

	(int Width, int Height) ImageSize ()
		=> (Preview?.PixelSize.Width ?? ImageWidth, Preview?.PixelSize.Height ?? ImageHeight);

	void ShowMarquee (Point start, Point end)
	{
		if (SelectionXyRatio > 0) {
			ShowSelection (Constrain (MapSelection (start, end)));
			return;
		}

		SetMarquee (start.X, start.Y, end.X, end.Y);
	}

	void ShowSelection (FSpot.Imaging.PixelRect selection)
	{
		var (viewWidth, viewHeight) = ViewSize ();
		var (imageWidth, imageHeight) = ImageSize ();
		var a = PhotoViewTransform.ImageToView (viewWidth, viewHeight, imageWidth, imageHeight,
			Zoom, PanX, PanY, selection.X, selection.Y);
		var b = PhotoViewTransform.ImageToView (viewWidth, viewHeight, imageWidth, imageHeight,
			Zoom, PanX, PanY, selection.X + selection.Width, selection.Y + selection.Height);
		SetMarquee (a.X, a.Y, b.X, b.Y);
	}

	void SetMarquee (double x1, double y1, double x2, double y2)
	{
		double x = Math.Min (x1, x2);
		double y = Math.Min (y1, y2);
		double width = Math.Abs (x2 - x1);
		double height = Math.Abs (y2 - y1);
		Canvas.SetLeft (Marquee, x);
		Canvas.SetTop (Marquee, y);
		Marquee.Width = width;
		Marquee.Height = height;
		Marquee.IsVisible = width > 1 && height > 1;
	}
}
