// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Media.Imaging;

using FSpot.Cms;
using FSpot.Imaging;

using Hyena;

namespace FSpot.Avalonia.Imaging;

// The preference names the profile the screen should show. An empty name is
// "colour management off". "_x_profile_" is the system display profile.
// The source buffer is left as stored: the screen gets a converted copy.
static class DisplayColor
{
	public static Profile? Resolve (string? preference, Func<string, Profile?> find, Profile? system)
	{
		if (string.IsNullOrEmpty (preference))
			return null;
		if (preference == SystemColorProfile.Sentinel)
			return system;
		return find (preference);
	}

	public static Profile? Selected ()
		=> Resolve (LibraryPreferences.DisplayProfile, InstalledProfileNames.Find, SystemColorProfile.Current);

	public static ImageBuffer Apply (ImageBuffer buffer, Profile? destination)
	{
		if (buffer == null)
			throw new ArgumentNullException (nameof (buffer));
		if (destination == null)
			return buffer;
		return Convert (buffer, destination) ?? buffer;
	}

	public static Bitmap Present (Bitmap bitmap)
		=> Present (bitmap, Selected ());

	public static Bitmap Present (Bitmap bitmap, Profile? destination)
	{
		if (destination == null)
			return bitmap;
		var shown = Apply (bitmap, destination);
		if (!ReferenceEquals (shown, bitmap))
			bitmap.Dispose ();
		return shown;
	}

	public static Bitmap Apply (Bitmap bitmap, Profile? destination)
	{
		if (destination == null)
			return bitmap;

		int width = bitmap.PixelSize.Width;
		int height = bitmap.PixelSize.Height;
		if (width < 1 || height < 1)
			return bitmap;

		int stride = width * 4;
		var copied = new byte[stride * height];
		unsafe {
			fixed (byte* pointer = copied)
				bitmap.CopyPixels (new global::Avalonia.PixelRect (0, 0, width, height), (nint)pointer, copied.Length, stride);
		}

		// Skia decodes as Rgba8888. Treating those bytes as Bgra swaps red and blue,
		// which is the blue cast on a color-managed thumbnail.
		var straight = ToStraightRgba (copied, width, height, bitmap.Format, bitmap.AlphaFormat);
		var converted = Convert (straight, destination);
		return converted == null ? bitmap : AvaloniaImagingBackend.ToBgraBitmap (converted);
	}

	internal static ImageBuffer ToStraightRgba (byte[] copied, int width, int height,
		global::Avalonia.Platform.PixelFormat? format, global::Avalonia.Platform.AlphaFormat? alpha)
	{
		byte[] rgba = format == global::Avalonia.Platform.PixelFormat.Bgra8888
			? SwapRedAndBlue (copied)
			: (byte[])copied.Clone ();
		if (alpha == global::Avalonia.Platform.AlphaFormat.Premul)
			StraightAlpha.UnpremultiplyRgba (rgba);
		return new ImageBuffer (width, height, true, width * 4, rgba);
	}

	static byte[] SwapRedAndBlue (byte[] bgra)
	{
		var rgba = new byte[bgra.Length];
		for (int i = 0; i < bgra.Length; i += 4) {
			rgba[i] = bgra[i + 2];
			rgba[i + 1] = bgra[i + 1];
			rgba[i + 2] = bgra[i];
			rgba[i + 3] = bgra[i + 3];
		}

		return rgba;
	}

	static ImageBuffer? Convert (ImageBuffer buffer, Profile destination)
	{
		int channels = buffer.HasAlpha ? 4 : 3;
		int bytes = buffer.Rowstride * buffer.Height;
		if (bytes < 1 || buffer.Pixels.Length < bytes || buffer.Rowstride < channels * buffer.Width)
			return null;

		var pixels = new byte[bytes];
		Buffer.BlockCopy (buffer.Pixels, 0, pixels, 0, bytes);
		try {
			using var source = Profile.CreateStandardRgb ();
			var format = buffer.HasAlpha ? Format.Rgba8 : Format.Rgb8;
			using var transform = new Transform (source, format, destination, format,
				Intent.Perceptual, CmsTransforms.For (print: false, copyAlpha: buffer.HasAlpha));
			unsafe {
				fixed (byte* start = pixels) {
					for (int row = 0; row < buffer.Height; row++) {
						byte* line = start + row * buffer.Rowstride;
						transform.Apply ((IntPtr)line, (IntPtr)line, (uint)buffer.Width);
					}
				}
			}
		} catch (Exception e) when (e is CmsException || ProfileCatalog.IsNativeLibraryMissing (e)) {
			Logger.Log.Warning ($"Leaving the image unmanaged: {e.Message}");
			return null;
		}

		return new ImageBuffer (buffer.Width, buffer.Height, buffer.HasAlpha, buffer.Rowstride, pixels);
	}
}
