//
// ViewerZoom.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Imaging;

/// <summary>
/// Zoom math for the photo view. The preview is laid out with Stretch.Uniform and then
/// scaled by a zoom factor around its centre, so zoom 1 is GTK's "fit".
/// </summary>
public static class ViewerZoom
{
	public const double Min = 0.25;
	public const double Max = 8;

	/// <summary>GTK <c>ImageView.ZOOM_FACTOR</c>.</summary>
	public const double Step = 1.1;

	/// <summary>GTK <c>Hadjustment.StepIncrement</c> for the arrow keys when zoomed.</summary>
	public const double PanStep = 32;

	public static bool IsFit (double zoom)
		=> Math.Abs (zoom - 1) < 1e-6;

	/// <summary>
	/// The zoom factor, relative to fit, that shows <paramref name="scale"/> original pixels
	/// per device pixel: GTK <c>DoZoom (1.0)</c> for key 1 and <c>DoZoom (2.0)</c> for key 2.
	/// </summary>
	public static double ForScale (double scale, double viewWidth, double viewHeight,
		int previewWidth, int previewHeight, int originalWidth, double uiScale)
	{
		if (scale <= 0 || viewWidth <= 0 || viewHeight <= 0 || previewWidth <= 0 || previewHeight <= 0)
			return 1;
		double fit = Math.Min (viewWidth / previewWidth, viewHeight / previewHeight);
		double original = originalWidth > 0 ? originalWidth : previewWidth;
		double dipsPerPreviewPixel = scale * original / previewWidth / (uiScale > 0 ? uiScale : 1);
		return Clamp (dipsPerPreviewPixel / fit);
	}

	public static double Clamp (double zoom)
		=> Math.Clamp (zoom, Min, Max);

	public static double Normalize (double zoom)
		=> (Clamp (zoom) - Min) / (Max - Min);

	public static double FromNormalized (double position)
		=> Clamp (Min + Math.Clamp (position, 0, 1) * (Max - Min));

	// A mouse notch is about ±1. A trackpad pinch is a small magnification delta.
	// A zero wheel delta is not a zoom-out.
	public static double WheelFactor (double deltaY)
	{
		if (deltaY == 0 || double.IsNaN (deltaY))
			return 1;
		return deltaY > 0 ? Step : 1 / Step;
	}

	public static double MagnifyFactor (double delta)
	{
		if (delta == 0 || double.IsNaN (delta))
			return 1;
		double factor = 1 + delta;
		if (factor < 0.5)
			factor = 0.5;
		if (factor > 1.5)
			factor = 1.5;
		return factor;
	}

	/// <summary>
	/// The pan that keeps the view point under (<paramref name="anchorX"/>, <paramref name="anchorY"/>)
	/// fixed while the zoom changes. The render transform scales around the view centre.
	/// </summary>
	public static (double X, double Y) AnchorPan (double oldZoom, double newZoom, double panX, double panY,
		double anchorX, double anchorY, double viewWidth, double viewHeight)
	{
		if (oldZoom <= 0)
			return (panX, panY);
		double ratio = newZoom / oldZoom;
		double cx = viewWidth / 2;
		double cy = viewHeight / 2;
		double x = anchorX - cx - (anchorX - cx - panX) * ratio;
		double y = anchorY - cy - (anchorY - cy - panY) * ratio;
		return (x, y);
	}
}
