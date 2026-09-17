using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using FSpot.Utils;

namespace FSpot.Avalonia.Controls;

public partial class LoupeOverlay : UserControl
{
	public static readonly StyledProperty<IImage?> SampleProperty =
		AvaloniaProperty.Register<LoupeOverlay, IImage?> (nameof (Sample));

	public static readonly StyledProperty<double> HotspotXProperty =
		AvaloniaProperty.Register<LoupeOverlay, double> (nameof (HotspotX));

	public static readonly StyledProperty<double> HotspotYProperty =
		AvaloniaProperty.Register<LoupeOverlay, double> (nameof (HotspotY));

	public static readonly StyledProperty<double> AngleProperty =
		AvaloniaProperty.Register<LoupeOverlay, double> (nameof (Angle), Math.PI / 4);

	static LoupeOverlay ()
	{
		SampleProperty.Changed.AddClassHandler<LoupeOverlay> ((c, _) => c.ApplyLayout ());
		HotspotXProperty.Changed.AddClassHandler<LoupeOverlay> ((c, _) => c.ApplyLayout ());
		HotspotYProperty.Changed.AddClassHandler<LoupeOverlay> ((c, _) => c.ApplyLayout ());
		AngleProperty.Changed.AddClassHandler<LoupeOverlay> ((c, _) => c.ApplyLayout ());
	}

	public LoupeOverlay ()
	{
		InitializeComponent ();
		ApplyLayout ();
	}

	public IImage? Sample {
		get => GetValue (SampleProperty);
		set => SetValue (SampleProperty, value);
	}

	public double HotspotX {
		get => GetValue (HotspotXProperty);
		set => SetValue (HotspotXProperty, value);
	}

	public double HotspotY {
		get => GetValue (HotspotYProperty);
		set => SetValue (HotspotYProperty, value);
	}

	public double Angle {
		get => GetValue (AngleProperty);
		set => SetValue (AngleProperty, value);
	}

	void ApplyLayout ()
	{
		if (Surface is null || Lens is null || Chrome is null)
			return;

		var layout = LoupeLayout.ForPointer (HotspotX, HotspotY, Angle);
		Width = layout.Width;
		Height = layout.Height;
		Canvas.SetLeft (this, layout.WindowX);
		Canvas.SetTop (this, layout.WindowY);

		Chrome.Data = ChromeGeometry (layout);
		LensImage.Source = Sample;

		double lensSize = layout.Radius * 2;
		Lens.Width = lensSize;
		Lens.Height = lensSize;
		Lens.CornerRadius = new CornerRadius (layout.Radius);
		Lens.BorderThickness = new Thickness (layout.Border);
		Lens.Clip = new EllipseGeometry {
			Center = new Point (layout.Radius, layout.Radius),
			RadiusX = layout.Radius,
			RadiusY = layout.Radius
		};
		Canvas.SetLeft (Lens, layout.LensX - layout.Radius);
		Canvas.SetTop (Lens, layout.LensY - layout.Radius);
	}

	// GTK Loupe.DrawShape: union of stem + handle ring + lens rim, then punch
	// the handle hole so the hotspot pixel shows through.
	static Geometry ChromeGeometry (LoupeLayout layout)
	{
		double inner = layout.Inner;
		double border = layout.Border;
		double radius = layout.Radius;
		double innerX = radius + border + inner;
		var local = LocalTransform (layout.LensX, layout.LensY, layout.Angle);

		var stem = new RectangleGeometry (new Rect (0, -(border + inner), innerX, 2 * (border + inner))) {
			Transform = local
		};
		var handleOuter = new EllipseGeometry {
			Center = new Point (innerX, 0),
			RadiusX = inner + border,
			RadiusY = inner + border,
			Transform = LocalTransform (layout.LensX, layout.LensY, layout.Angle)
		};
		var lensOuter = new EllipseGeometry {
			Center = new Point (0, 0),
			RadiusX = radius + border,
			RadiusY = radius + border,
			Transform = LocalTransform (layout.LensX, layout.LensY, layout.Angle)
		};
		var handleInner = new EllipseGeometry {
			Center = new Point (innerX, 0),
			RadiusX = inner,
			RadiusY = inner,
			Transform = LocalTransform (layout.LensX, layout.LensY, layout.Angle)
		};
		var body = new CombinedGeometry (GeometryCombineMode.Union,
			new CombinedGeometry (GeometryCombineMode.Union, stem, handleOuter),
			lensOuter);
		return new CombinedGeometry (GeometryCombineMode.Exclude, body, handleInner);
	}

	static Transform LocalTransform (double lensX, double lensY, double angle)
	{
		var transform = new TransformGroup ();
		transform.Children.Add (new RotateTransform (angle * 180 / Math.PI));
		transform.Children.Add (new TranslateTransform (lensX, lensY));
		return transform;
	}
}
