using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace FSpot.Avalonia;

public static class PreviewInterpolation
{
	public static void Apply (Visual visual, bool interpolate)
		=> RenderOptions.SetBitmapInterpolationMode (visual,
			interpolate ? BitmapInterpolationMode.HighQuality : BitmapInterpolationMode.None);
}
