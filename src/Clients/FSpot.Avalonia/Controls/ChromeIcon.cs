// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg.Skia;

namespace FSpot.Avalonia.Controls;

public sealed class ChromeIcon : Panel
{
	public static readonly StyledProperty<string?> UriProperty =
		AvaloniaProperty.Register<ChromeIcon, string?> (nameof (Uri));

	public static readonly StyledProperty<double> RasterScaleProperty =
		AvaloniaProperty.Register<ChromeIcon, double> (nameof (RasterScale));

	static ChromeIcon ()
	{
		UriProperty.Changed.AddClassHandler<ChromeIcon> ((icon, _) => icon.Rebuild ());
		RasterScaleProperty.Changed.AddClassHandler<ChromeIcon> ((icon, _) => icon.Rebuild ());
	}

	public string? Uri {
		get => GetValue (UriProperty);
		set => SetValue (UriProperty, value);
	}

	public double RasterScale {
		get => GetValue (RasterScaleProperty);
		set => SetValue (RasterScaleProperty, value);
	}

	protected override void OnAttachedToVisualTree (VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree (e);
		if (TopLevel.GetTopLevel (this) is { } top)
			top.ScalingChanged += OnScalingChanged;
		SizeChanged += OnSizeChanged;
		if (HasDipSize)
			Rebuild ();
	}

	protected override void OnDetachedFromVisualTree (VisualTreeAttachmentEventArgs e)
	{
		SizeChanged -= OnSizeChanged;
		if (TopLevel.GetTopLevel (this) is { } top)
			top.ScalingChanged -= OnScalingChanged;
		base.OnDetachedFromVisualTree (e);
	}

	void OnSizeChanged (object? sender, SizeChangedEventArgs e)
	{
		if (e.WidthChanged || e.HeightChanged)
			Rebuild ();
	}

	bool HasDipSize
		=> (!double.IsNaN (Width) && Width > 0) || (!double.IsNaN (Height) && Height > 0) || Bounds.Width > 0;

	void OnScalingChanged (object? sender, EventArgs e)
		=> Rebuild ();

	double EffectiveScale
		=> RasterScale > 0 ? RasterScale : TopLevel.GetTopLevel (this)?.RenderScaling ?? 1;

	static double DipSize (double constraint, double bounds)
	{
		if (!double.IsNaN (constraint) && constraint > 0)
			return constraint;
		if (bounds > 0)
			return bounds;
		return 16;
	}

	void Rebuild ()
	{
		Children.Clear ();
		if (string.IsNullOrEmpty (Uri))
			return;

		double width = DipSize (Width, Bounds.Width);
		double height = DipSize (Height, Bounds.Height);
		IImage source;
		if (Uri.EndsWith (".svg", StringComparison.OrdinalIgnoreCase))
			source = new SvgImage { Source = SvgSource.Load (Uri) };
		else {
			using var stream = AssetLoader.Open (new Uri (Uri, UriKind.Absolute));
			source = new Bitmap (stream);
		}
		Children.Add (new Image {
			Source = source,
			Width = width,
			Height = height
		});
	}
}
