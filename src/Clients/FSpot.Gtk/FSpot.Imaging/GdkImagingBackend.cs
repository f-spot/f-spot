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
			using var orig = LoadWithinBudget (stream);
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
			using var pixbuf = LoadWithinBudget (stream);
			return pixbuf.ToImageBuffer ();
		}

		public ThumbnailPng LoadPng (Stream stream)
		{
			using var pixbuf = LoadWithinBudget (stream);
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

		// SizePrepared runs before gdk allocates the pixbuf. SetSize(1, 1) keeps an
		// over-cap header from becoming the full bitmap, then the 1×1 is discarded.
		static Pixbuf LoadWithinBudget (Stream stream)
		{
			var loader = new PixbufLoader ();
			try {
				bool refused = false;
				loader.SizePrepared += (o, args) => {
					if (!DecodeBudget.Exceeds (args.Width, args.Height))
						return;
					loader.SetSize (1, 1);
					refused = true;
				};

				var data = new byte[8192];
				int count;
				while ((count = stream.Read (data, 0, data.Length)) > 0 && loader.Write (data, (ulong)count)) {
					if (refused)
						break;
				}

				try {
					loader.Close ();
				} catch (GLib.GException) {
					if (!refused)
						throw;
				}

				Pixbuf pixbuf = loader.Pixbuf;
				if (refused) {
					if (pixbuf != null)
						pixbuf.Dispose ();
					throw new InvalidDataException (DecodeBudget.TooLargeMessage);
				}

				if (pixbuf == null)
					throw new InvalidDataException ("Unable to decode the image.");
				return pixbuf;
			} finally {
				loader.Dispose ();
			}
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
