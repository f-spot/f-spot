// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg.Skia;

using FSpot.Avalonia.Imaging;
using FSpot.Cms;
using FSpot.Imaging;

using SkiaSharp;

namespace FSpot.Avalonia.Controls;

public sealed class ChromeIcon : Panel
{
	public static readonly StyledProperty<string?> UriProperty =
		AvaloniaProperty.Register<ChromeIcon, string?> (nameof (Uri));

	public static readonly StyledProperty<double> RasterScaleProperty =
		AvaloniaProperty.Register<ChromeIcon, double> (nameof (RasterScale));

	public static readonly StyledProperty<bool> ColorManageProperty =
		AvaloniaProperty.Register<ChromeIcon, bool> (nameof (ColorManage));

	static ChromeIcon ()
	{
		UriProperty.Changed.AddClassHandler<ChromeIcon> ((icon, _) => icon.Rebuild ());
		RasterScaleProperty.Changed.AddClassHandler<ChromeIcon> ((icon, _) => icon.Rebuild ());
		ColorManageProperty.Changed.AddClassHandler<ChromeIcon> ((icon, _) => icon.Rebuild ());
	}

	public string? Uri {
		get => GetValue (UriProperty);
		set => SetValue (UriProperty, value);
	}

	public double RasterScale {
		get => GetValue (RasterScaleProperty);
		set => SetValue (RasterScaleProperty, value);
	}

	// Tag icons. Toolbar artwork leaves this off: GTK only converts tag icons.
	public bool ColorManage {
		get => GetValue (ColorManageProperty);
		set => SetValue (ColorManageProperty, value);
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
		var profile = ColorManage ? DisplayColor.Selected () : null;
		IImage source;
		if (Uri.EndsWith (".svg", StringComparison.OrdinalIgnoreCase))
			source = profile == null
				? new SvgImage { Source = SvgSource.Load (Uri) }
				: RasterTagIcon (Uri, width, height, profile);
		else {
			using var stream = AssetLoader.Open (new Uri (Uri, UriKind.Absolute));
			var bitmap = new Bitmap (stream);
			var shown = profile == null ? bitmap : DisplayColor.Apply (bitmap, profile);
			if (!ReferenceEquals (shown, bitmap))
				bitmap.Dispose ();
			source = shown;
		}
		Children.Add (new Image {
			Source = source,
			Width = width,
			Height = height
		});
	}

	// A vector icon has no pixels to convert. Rasterize only when a display profile
	// is selected, and keep the straight alpha the transform copies.
	IImage RasterTagIcon (string uri, double width, double height, Profile profile)
	{
		var svg = SvgSource.Load (uri);
		var picture = svg?.Picture;
		var bounds = picture?.CullRect ?? default;
		if (picture == null || bounds.Width <= 0 || bounds.Height <= 0)
			return new SvgImage { Source = svg };

		double scale = EffectiveScale;
		int pixelWidth = Math.Max (1, (int)Math.Ceiling (width * scale));
		int pixelHeight = Math.Max (1, (int)Math.Ceiling (height * scale));
		var info = new SKImageInfo (pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
		using var bitmap = new SKBitmap (info);
		using (var canvas = new SKCanvas (bitmap)) {
			canvas.Clear (SKColors.Transparent);
			float fit = Math.Min (pixelWidth / bounds.Width, pixelHeight / bounds.Height);
			canvas.Translate ((pixelWidth - bounds.Width * fit) / 2f, (pixelHeight - bounds.Height * fit) / 2f);
			canvas.Scale (fit);
			canvas.Translate (-bounds.Left, -bounds.Top);
			canvas.DrawPicture (picture);
		}

		int stride = pixelWidth * 4;
		var rgba = new byte[stride * pixelHeight];
		for (int y = 0; y < pixelHeight; y++) {
			for (int x = 0; x < pixelWidth; x++) {
				var color = bitmap.GetPixel (x, y);
				int i = y * stride + x * 4;
				rgba[i] = color.Red;
				rgba[i + 1] = color.Green;
				rgba[i + 2] = color.Blue;
				rgba[i + 3] = color.Alpha;
			}
		}

		var shown = DisplayColor.Apply (new ImageBuffer (pixelWidth, pixelHeight, true, stride, rgba), profile);
		return AvaloniaImagingBackend.ToBgraBitmap (shown);
	}
}
