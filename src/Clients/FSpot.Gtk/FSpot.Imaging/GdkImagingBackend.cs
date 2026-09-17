//
// GdkImagingBackend.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Utils;

using Gdk;

using Hyena;

using TagLib.Image;

namespace FSpot.Imaging
{
	public sealed class GdkImagingBackend : IImagingBackend
	{
		public ImageBuffer Decode (Stream stream, ImageOrientation orientation)
		{
			using var orig = new Pixbuf (stream);
			return ImageBuffers.TransformOrientation (orig.ToImageBuffer (), orientation);
		}

		public ImageBuffer ScaleToMaxSize (ImageBuffer source, int maxWidth, int maxHeight, bool upscale = true)
		{
			using var pixbuf = source.ToPixbuf ();
			using var scaled = pixbuf.ScaleToMaxSize (maxWidth, maxHeight, upscale);
			return scaled.ToImageBuffer ();
		}

		public ImageBuffer Scale (ImageBuffer source, int width, int height)
		{
			using var pixbuf = source.ToPixbuf ();
			using var scaled = pixbuf.ScaleSimple (width, height, InterpType.Bilinear);
			return scaled.ToImageBuffer ();
		}

		public void SaveDerivedVersion (ImageBuffer buffer, SafeUri source, SafeUri dest, uint jpegQuality)
		{
			using var pixbuf = buffer.ToPixbuf ();
			FSpot.Utils.PixbufUtils.CreateDerivedVersion (source, dest, jpegQuality, pixbuf);
		}

		public ImageBuffer LoadThemeIcon (string name, int size)
		{
			using var pixbuf = GtkUtil.TryLoadIcon (GtkSettings.IconTheme, name, size, 0);
			return pixbuf.ToImageBuffer ();
		}

		public byte[] SerializeIcon (ImageBuffer icon)
			=> GdkPixdata.Write (icon);

		public ImageBuffer DeserializeIcon (byte[] data)
		{
			if (GdkPixdata.TryRead (data, out ImageBuffer icon))
				return icon;

			// Icons saved before both clients wrote GdkPixdata are PNGs.
			using var stream = new MemoryStream (data);
			using var pixbuf = new Pixbuf (stream);
			return pixbuf.ToImageBuffer ();
		}

		public ThumbnailPng LoadPng (Stream stream)
		{
			using var pixbuf = new Pixbuf (stream);
			return new ThumbnailPng {
				Image = pixbuf.ToImageBuffer (),
				UriOption = pixbuf.GetOption (ThumbnailPng.UriOptionKey),
				MTimeOption = pixbuf.GetOption (ThumbnailPng.MTimeOptionKey)
			};
		}

		public void SavePng (ImageBuffer image, string path, string[] optionKeys, string[] optionValues)
		{
			using var pixbuf = image.ToPixbuf ();
			pixbuf.Savev (path, "png", optionKeys, optionValues);
		}

		public static void TryEnableXcf (IImageFileFactory factory)
		{
			try {
				foreach (PixbufFormat format in Pixbuf.Formats) {
					if (format.Name != "xcf")
						continue;
					if (format.IsDisabled)
						format.SetDisabled (false);
					factory.RegisterGenericLoader (".xcf");
				}
			} catch (Exception ex) {
				Logger.Log.Debug (ex, "XCF loader unavailable");
			}
		}
	}
}
