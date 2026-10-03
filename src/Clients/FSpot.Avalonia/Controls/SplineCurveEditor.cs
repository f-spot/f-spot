// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using FSpot.Editors;

namespace FSpot.Avalonia.Controls;

/// <summary>
/// The GtkCurve spline from the black-and-white editor. Dragging a point writes
/// the 256-byte tone table the conversion already applies.
/// </summary>
public class SplineCurveEditor : Control
{
	public static readonly StyledProperty<PhotoEditorSession?> SessionProperty =
		AvaloniaProperty.Register<SplineCurveEditor, PhotoEditorSession?> (nameof (Session));

	public static readonly StyledProperty<byte[]?> CurveProperty =
		AvaloniaProperty.Register<SplineCurveEditor, byte[]?> (nameof (Curve));

	readonly SplineCurve curve = new ();
	bool tracking;

	static SplineCurveEditor ()
		=> SessionProperty.Changed.AddClassHandler<SplineCurveEditor> ((editor, _) => editor.ResetFromSession ());

	public SplineCurveEditor ()
	{
		Focusable = true;
		MinHeight = 128 + SplineCurve.Radius * 2;
	}

	public PhotoEditorSession? Session {
		get => GetValue (SessionProperty);
		set => SetValue (SessionProperty, value);
	}

	public byte[]? Curve {
		get => GetValue (CurveProperty);
		set => SetValue (CurveProperty, value);
	}

	internal void PressAt (Point position)
		=> Edit (position, curve.Press);

	protected override void OnPointerPressed (PointerPressedEventArgs e)
	{
		base.OnPointerPressed (e);
		if (!e.GetCurrentPoint (this).Properties.IsLeftButtonPressed)
			return;
		tracking = true;
		Edit (e.GetPosition (this), curve.Press);
		e.Pointer.Capture (this);
		e.Handled = true;
	}

	protected override void OnPointerMoved (PointerEventArgs e)
	{
		base.OnPointerMoved (e);
		if (!tracking || !ReferenceEquals (e.Pointer.Captured, this))
			return;
		Edit (e.GetPosition (this), curve.Drag);
		e.Handled = true;
	}

	protected override void OnPointerReleased (PointerReleasedEventArgs e)
	{
		base.OnPointerReleased (e);
		if (!tracking || e.InitialPressMouseButton != MouseButton.Left)
			return;
		FinishGesture ();
		e.Pointer.Capture (null);
		e.Handled = true;
	}

	protected override void OnPointerCaptureLost (PointerCaptureLostEventArgs e)
	{
		base.OnPointerCaptureLost (e);
		FinishGesture ();
	}

	void FinishGesture ()
	{
		if (!tracking)
			return;
		tracking = false;
		var (width, height) = Graph ();
		curve.Release ();
		Publish (width, height);
	}

	public override void Render (DrawingContext context)
	{
		var (width, height) = Graph ();
		if (width < 2 || height < 2)
			return;

		var ink = Brush ("SystemControlForegroundBaseHighBrush", Brushes.Black);
		var grid = Brush ("SystemControlForegroundBaseMediumBrush", Brushes.Gray);
		var pen = new Pen (ink, 1);
		var gridPen = new Pen (grid, 1);
		for (int i = 0; i < 5; i++) {
			double y = SplineCurve.Radius + i * (height / 4.0);
			double x = SplineCurve.Radius + i * (width / 4.0);
			context.DrawLine (gridPen, new Point (SplineCurve.Radius, y), new Point (SplineCurve.Radius + width, y));
			context.DrawLine (gridPen, new Point (x, SplineCurve.Radius), new Point (x, SplineCurve.Radius + height));
		}

		float[] samples = curve.Sample (width);
		Point? previous = null;
		for (int i = 0; i < samples.Length; i++) {
			var next = new Point (SplineCurve.Radius + i, SplineCurve.Radius + height - Project (samples[i], height));
			if (previous is Point from)
				context.DrawLine (pen, from, next);
			previous = next;
		}

		var fill = ink;
		foreach (var point in curve.ControlPoints) {
			double x = SplineCurve.Radius + Project (point.X, width);
			double y = SplineCurve.Radius + height - Project (point.Y, height);
			context.DrawEllipse (fill, null, new Point (x, y), SplineCurve.Radius, SplineCurve.Radius);
		}
	}

	void Edit (Point position, Action<float, float, int, int> gesture)
	{
		var (width, height) = Graph ();
		gesture ((float)position.X, (float)position.Y, width, height);
		Publish (width, height);
	}

	void ResetFromSession ()
	{
		curve.Reset ();
		var (width, height) = Graph ();
		Publish (width, height);
	}

	void Publish (int width, int height)
	{
		InvalidateVisual ();
		if (Session?.Editor.Id != PhotoEditorCatalog.BW.Id || width < 2 || height < 2)
			return;
		SetCurrentValue (CurveProperty, curve.ToBytes ());
	}

	(int Width, int Height) Graph ()
	{
		int width = (int)Bounds.Width - SplineCurve.Radius * 2;
		int height = (int)Bounds.Height - SplineCurve.Radius * 2;
		return (Math.Max (0, width), Math.Max (0, height));
	}

	static double Project (float value, int norm)
		=> (norm - 1) * ((value - SplineCurve.Min) / (SplineCurve.Max - SplineCurve.Min));

	IBrush Brush (string key, IBrush fallback)
		=> TryGetResource (key, ActualThemeVariant, out var resource) && resource is IBrush brush ? brush : fallback;
}
