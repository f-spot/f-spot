//
// PixbufUtils.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2004-2007 Larry Ewing
// Copyright (C) 2006-2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Runtime.InteropServices;

using Cairo;

using FSpot.Cms;
using FSpot.Imaging;
using FSpot.Settings;
using FSpot.UI.Dialog;
using FSpot.Utils;

using Gdk;

using Hyena;

using Pinta.Core;
using Pinta.Effects;

using TagLib.Image;

namespace FSpot
{
	public static class PixbufUtils
	{
		static Pixbuf error_pixbuf = null;

		public static Pixbuf ErrorPixbuf {
			get {
				if (error_pixbuf == null)
					error_pixbuf = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, "FSpotQuestionMark", 256, 0);
				return error_pixbuf;
			}
		}

		public static Pixbuf LoadingPixbuf = LoadFromAssembly ("FSpotLoading.png");

		public static double Fit (Pixbuf pixbuf,
					  int dest_width, int dest_height,
					  bool upscale_smaller,
					  out int fit_width, out int fit_height)
		{
			return Fit (pixbuf.Width, pixbuf.Height,
					dest_width, dest_height,
					upscale_smaller,
					out fit_width, out fit_height);
		}

		public static double Fit (int orig_width, int orig_height,
					  int dest_width, int dest_height,
					  bool upscale_smaller,
					  out int fit_width, out int fit_height)
		{
			if (orig_width == 0 || orig_height == 0) {
				fit_width = 0;
				fit_height = 0;
				return 0.0;
			}

			var scale = Math.Min (dest_width / (double)orig_width,
						 dest_height / (double)orig_height);

			if (scale > 1.0 && !upscale_smaller)
				scale = 1.0;

			fit_width = (int)Math.Round (scale * orig_width);
			fit_height = (int)Math.Round (scale * orig_height);

			return scale;
		}


		// FIXME: These should be in GTK#.  When my patch is committed, these LoadFrom* methods will
		// go away.

		public class AspectLoader
		{
			PixbufLoader loader = new PixbufLoader ();
			int max_width;
			int max_height;
			ImageOrientation orientation;

			public AspectLoader (int max_width, int max_height)
			{
				this.max_height = max_height;
				this.max_width = max_width;
				loader.SizePrepared += HandleSizePrepared;
			}

			void HandleSizePrepared (object obj, SizePreparedArgs args)
			{
				switch (orientation) {
				case ImageOrientation.LeftTop:
				case ImageOrientation.LeftBottom:
				case ImageOrientation.RightTop:
				case ImageOrientation.RightBottom:
					(max_height, max_width) = (max_width, max_height);
					break;
				default:
					break;
				}


				var scale = Fit (args.Width, args.Height, max_width, max_height, true, out var scale_width, out var scale_height);

				if (scale < 1.0)
					loader.SetSize (scale_width, scale_height);
			}

			public Pixbuf Load (Stream stream, ImageOrientation orientation)
			{
				int count;
				var data = new byte[8192];
				while ((count = stream.Read (data, 0, data.Length)) > 0 && loader.Write (data, (ulong)count)) {
					;
				}

				loader.Close ();
				var orig = loader.Pixbuf;
				var rotated = orig.TransformOrientation (orientation);

				if (orig != rotated)
					orig.Dispose ();
				loader.Dispose ();
				return rotated;
			}

			public Pixbuf LoadFromFile (string path)
			{
				try {
					orientation = GetOrientation (new SafeUri (path));
					using (var fs = System.IO.File.OpenRead (path)) {
						return Load (fs, orientation);
					}
				} catch (Exception) {
					Logger.Log.Error ($"Error loading photo {path}");
					return null;
				}
			}
		}

		static public Pixbuf LoadAtMaxSize (string path, int max_width, int max_height)
		{
			var loader = new AspectLoader (max_width, max_height);
			return loader.LoadFromFile (path);
		}

		public static Pixbuf TagIconFromPixbuf (Pixbuf source)
		{
			return IconFromPixbuf (source, (int)IconSize.Large);
		}

		public static Pixbuf IconFromPixbuf (Pixbuf source, int size)
		{
			Pixbuf tmp = null;
			Pixbuf icon = null;

			if (source.Width > source.Height)
				source = tmp = new Pixbuf (source, (source.Width - source.Height) / 2, 0, source.Height, source.Height);
			else if (source.Width < source.Height)
				source = tmp = new Pixbuf (source, 0, (source.Height - source.Width) / 2, source.Width, source.Width);

			if (source.Width == source.Height)
				icon = source.ScaleSimple (size, size, InterpType.Bilinear);
			else
				throw new Exception ("Bad logic leads to bad accidents");

			if (tmp != null)
				tmp.Dispose ();

			return icon;
		}

		static Pixbuf LoadFromAssembly (string resource)
		{
			try {
				return new Pixbuf (System.Reflection.Assembly.GetEntryAssembly (), resource);
			} catch {
				return null;
			}
		}

		public static Pixbuf ScaleToAspect (Pixbuf orig, int width, int height)
		{
			Gdk.Rectangle pos;
			var scale = Fit (orig, width, height, false, out pos.Width, out pos.Height);
			pos.X = (width - pos.Width) / 2;
			pos.Y = (height - pos.Height) / 2;

			var scaled = new Pixbuf (Colorspace.Rgb, false, 8, width, height);
			scaled.Fill (0x000000);

			orig.Composite (scaled, pos.X, pos.Y,
					pos.Width, pos.Height,
					pos.X, pos.Y, scale, scale,
					InterpType.Bilinear,
					255);

			return scaled;
		}

		public static Pixbuf Flatten (Pixbuf pixbuf)
		{
			if (!pixbuf.HasAlpha)
				return null;

			var flattened = new Pixbuf (Colorspace.Rgb, false, 8, pixbuf.Width, pixbuf.Height);
			pixbuf.CompositeColor (flattened, 0, 0,
						   pixbuf.Width, pixbuf.Height,
						   0, 0, 1, 1,
						   InterpType.Bilinear,
						   255, 0, 0, 2000, 0xffffff, 0xffffff);

			return flattened;
		}

		unsafe public static byte[] Pixbuf_GetRow (byte* pixels, int row, int rowstride, int width, int channels, byte[] dest)
		{
			var ptr = pixels + row * rowstride;

			Marshal.Copy ((IntPtr)ptr, dest, 0, width * channels);

			return dest;
		}

		unsafe public static void Pixbuf_SetRow (byte* pixels, byte[] dest, int row, int rowstride, int width, int channels)
		{
			var destPtr = pixels + row * rowstride;

			for (var i = 0; i < width * channels; i++) {
				destPtr[i] = dest[i];
			}
		}

		unsafe public static Pixbuf UnsharpMask (Pixbuf src,
												 double radius,
												 double amount,
												 double threshold,
												 ThreadProgressDialog progressDialog)
		{
			// Make sure the pixbuf has an alpha channel before we try to blur it
			src = src.AddAlpha (false, 0, 0, 0);

			var result = Blur (src, (int)radius, progressDialog);

			var sourceRowstride = src.Rowstride;
			var sourceHeight = src.Height;
			var sourceChannels = src.NChannels;
			var sourceWidth = src.Width;

			var resultRowstride = result.Rowstride;
			var resultWidth = result.Width;
			var resultChannels = result.NChannels;

			var srcRow = new byte[sourceRowstride];
			var destRow = new byte[resultRowstride];

			var sourcePixels = (byte*)src.Pixels;
			var resultPixels = (byte*)result.Pixels;

			for (var row = 0; row < sourceHeight; row++) {
				Pixbuf_GetRow (sourcePixels, row, sourceRowstride, sourceWidth, sourceChannels, srcRow);
				Pixbuf_GetRow (resultPixels, row, resultRowstride, resultWidth, resultChannels, destRow);

				int diff;
				for (var i = 0; i < sourceWidth * sourceChannels; i++) {
					diff = srcRow[i] - destRow[i];
					if (Math.Abs (2 * diff) < threshold)
						diff = 0;

					var val = (int)(srcRow[i] + amount * diff);

					if (val > 255)
						val = 255;
					else if (val < 0)
						val = 0;

					destRow[i] = (byte)val;
				}

				Pixbuf_SetRow (resultPixels, destRow, row, resultRowstride, resultWidth, resultChannels);

				// This is the other half of the progress so start and halfway
				if (progressDialog != null)
					progressDialog.Fraction = row / ((double)sourceHeight - 1) * 0.25 + 0.75;
			}

			return result;
		}

		public static Pixbuf Blur (Pixbuf src, int radius, ThreadProgressDialog dialog)
		{
			var sourceSurface = Hyena.Gui.PixbufImageSurface.Create (src);
			var destinationSurface = new ImageSurface (Cairo.Format.Rgb24, src.Width, src.Height);

			// If we do it as a bunch of single lines (rectangles of one pixel) then we can give the progress
			// here instead of going deeper to provide the feedback
			for (var i = 0; i < src.Height; i++) {
				GaussianBlurEffect.RenderBlurEffect (sourceSurface, destinationSurface, new[] { new Gdk.Rectangle (0, i, src.Width, 1) }, radius);

				if (dialog != null) {
					// This only half of the entire process
					var fraction = i / (double)(src.Height - 1) * 0.75;
					dialog.Fraction = fraction;
				}
			}

			return destinationSurface.ToPixbuf ();
		}

		public static ImageSurface Clone (this ImageSurface surf)
		{
			var newsurf = new ImageSurface (surf.Format, surf.Width, surf.Height);

			using (var g = new Context (newsurf)) {
				g.SetSource (surf);
				g.Paint ();
			}

			return newsurf;
		}

		public unsafe static Pixbuf RemoveRedeye (Pixbuf src, Gdk.Rectangle area)
		{
			return RemoveRedeye (src, area, -15);
		}

		//threshold, factors and comparisons borrowed from the gimp plugin 'redeye.c' by Robert Merkel
		public unsafe static Pixbuf RemoveRedeye (Pixbuf src, Gdk.Rectangle area, int threshold)
		{
			var copy = src.Copy ();
			var selection = new Pixbuf (copy, area.X, area.Y, area.Width, area.Height);
			var spix = (byte*)selection.Pixels;
			var h = selection.Height;
			var w = selection.Width;
			var channels = src.NChannels;

			var RED_FACTOR = 0.5133333;
			double GREEN_FACTOR = 1;
			var BLUE_FACTOR = 0.1933333;

			for (var j = 0; j < h; j++) {
				var s = spix;
				for (var i = 0; i < w; i++) {
					var adjusted_red = (int)(s[0] * RED_FACTOR);
					var adjusted_green = (int)(s[1] * GREEN_FACTOR);
					var adjusted_blue = (int)(s[2] * BLUE_FACTOR);

					if (adjusted_red >= adjusted_green - threshold
						&& adjusted_red >= adjusted_blue - threshold)
						s[0] = (byte)((adjusted_green + adjusted_blue) / (2.0 * RED_FACTOR));
					s += channels;
				}
				spix += selection.Rowstride;
			}

			return copy;
		}

		public static unsafe Pixbuf ColorAdjust (Pixbuf src, double brightness, double contrast,
						  double hue, double saturation, int src_color, int dest_color)
		{
			var adjusted = new Pixbuf (Colorspace.Rgb, src.HasAlpha, 8, src.Width, src.Height);
			ColorAdjust (src, adjusted, brightness, contrast, hue, saturation, src_color, dest_color);
			return adjusted;
		}

		public static Cms.Format PixbufCmsFormat (Pixbuf buf)
		{
			return buf.HasAlpha ? Cms.Format.Rgba8Planar : Cms.Format.Rgb8;
		}

		public static unsafe void ColorAdjust (Pixbuf src, Pixbuf dest,
							   double brightness, double contrast,
							   double hue, double saturation,
							   int src_color, int dest_color)
		{
			if (src.Width != dest.Width || src.Height != dest.Height)
				throw new Exception ("Invalid Dimensions");

			var srgb = Profile.CreateStandardRgb ();

			var bchsw = Profile.CreateAbstract (256,
								0.0,
								brightness, contrast,
								hue, saturation, src_color,
								dest_color);

			Profile[] list = { srgb, bchsw, srgb };
			var trans = new Transform (list,
							 PixbufCmsFormat (src),
							 PixbufCmsFormat (dest),
							 Intent.Perceptual, 0x0100);

			ColorAdjust (src, dest, trans);

			trans.Dispose ();
			srgb.Dispose ();
			bchsw.Dispose ();
		}

		public static unsafe void ColorAdjust (Pixbuf src, Pixbuf dest, Transform trans)
		{
			var width = src.Width;
			var srcpix = (byte*)src.Pixels;
			var destpix = (byte*)dest.Pixels;

			for (var row = 0; row < src.Height; row++) {
				trans.Apply ((IntPtr)(srcpix + row * src.Rowstride),
						 (IntPtr)(destpix + row * dest.Rowstride),
						 (uint)width);
			}

		}

		public static unsafe bool IsGray (Pixbuf pixbuf, int max_difference)
		{
			var chan = pixbuf.NChannels;

			var pix = (byte*)pixbuf.Pixels;
			var h = pixbuf.Height;
			var w = pixbuf.Width;
			var stride = pixbuf.Rowstride;

			for (var j = 0; j < h; j++) {
				var p = pix;
				for (var i = 0; i < w; i++) {
					if (Math.Abs (p[0] - p[1]) > max_difference || Math.Abs (p[0] - p[2]) > max_difference)
						return false;
					p += chan;
				}
				pix += stride;
			}

			return true;
		}

		public static unsafe void ReplaceColor (Pixbuf src, Pixbuf dest)
		{
			if (src.HasAlpha || !dest.HasAlpha || src.Width != dest.Width || src.Height != dest.Height)
				throw new ApplicationException ("invalid pixbufs");

			var dpix = (byte*)dest.Pixels;
			var spix = (byte*)src.Pixels;
			var h = src.Height;
			var w = src.Width;
			for (var j = 0; j < h; j++) {
				var d = dpix;
				var s = spix;
				for (var i = 0; i < w; i++) {
					d[0] = s[0];
					d[1] = s[1];
					d[2] = s[2];
					d += 4;
					s += 3;
				}
				dpix += dest.Rowstride;
				spix += src.Rowstride;
			}
		}

		public static ImageOrientation GetOrientation (SafeUri uri)
		{
			using (var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (uri)) {
				return img.Orientation;
			}
		}

		public static void CreateDerivedVersion (SafeUri source, SafeUri destination)
		{
			CreateDerivedVersion (source, destination, 95);
		}

		public static void CreateDerivedVersion (SafeUri source, SafeUri destination, uint jpegQuality)
		{
			if (source.GetExtension () == destination.GetExtension ()) {
				System.IO.File.Copy (source.AbsolutePath, destination.AbsolutePath, true);
				return;
			}

			// Else make a derived copy with metadata copied
			using var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (source);
			using var pixbuf = img.Load ();

			Utils.PixbufUtils.CreateDerivedVersion (source, destination, jpegQuality, pixbuf);
		}
	}
}