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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using Cairo;

using FSpot;
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

public static class PixbufUtils
{
	static Pixbuf errorPixbuf;

	public static Pixbuf ErrorPixbuf {
		get {
			if (errorPixbuf == null)
				errorPixbuf = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, "FSpotQuestionMark", 256, (Gtk.IconLookupFlags)0);
			return errorPixbuf;
		}
	}

	public static Pixbuf LoadingPixbuf { get; } = LoadFromAssembly ("FSpotLoading.png");

	public static double Fit (Pixbuf pixbuf, int destWidth, int destHeight, bool upscaleSmaller, out int fitWidth, out int fitHeight)
	{
		return Fit (pixbuf.Width, pixbuf.Height, destWidth, destHeight, upscaleSmaller, out fitWidth, out fitHeight);
	}

	public static double Fit (int origWidth, int origHeight, int destWidth, int destHeight, bool upscaleSmaller, out int fitWidth, out int fitHeight)
	{
		if (origWidth == 0 || origHeight == 0) {
			fitWidth = 0;
			fitHeight = 0;
			return 0.0;
		}

		double scale = Math.Min (destWidth / (double)origWidth,
					 destHeight / (double)origHeight);

		if (scale > 1.0 && !upscaleSmaller)
			scale = 1.0;

		fitWidth = (int)Math.Round (scale * origWidth);
		fitHeight = (int)Math.Round (scale * origHeight);

		return scale;
	}


	// FIXME: These should be in GTK#.  When my patch is committed, these LoadFrom* methods will
	// go away.

	public class AspectLoader
	{
		Gdk.PixbufLoader loader = new Gdk.PixbufLoader ();
		int max_width;
		int max_height;
		ImageOrientation orientation;

		public AspectLoader (int maxWidth, int maxHeight)
		{
			this.max_height = maxHeight;
			this.max_width = maxWidth;
			loader.SizePrepared += HandleSizePrepared;
		}

		void HandleSizePrepared (object obj, SizePreparedArgs args)
		{
			switch (orientation) {
			case ImageOrientation.LeftTop:
			case ImageOrientation.LeftBottom:
			case ImageOrientation.RightTop:
			case ImageOrientation.RightBottom:
				int tmp = max_width;
				max_width = max_height;
				max_height = tmp;
				break;
			default:
				break;
			}

			double scale = Fit (args.Width, args.Height, max_width, max_height, true, out var scaleWidth, out var scaleHeight);

			if (scale < 1.0)
				loader.SetSize (scaleWidth, scaleHeight);
		}

		public Pixbuf Load (System.IO.Stream stream, ImageOrientation orientation)
		{
			int count;
			byte[] data = new byte[8192];
			while (((count = stream.Read (data, 0, data.Length)) > 0) && loader.Write (data, (ulong)count)) {
				;
			}

			loader.Close ();
			Pixbuf orig = loader.Pixbuf;
			Gdk.Pixbuf rotated = FSpot.Utils.PixbufUtils.TransformOrientation (orig, orientation);

			if (orig != rotated)
				orig.Dispose ();
			loader.Dispose ();
			return rotated;
		}

		public Pixbuf LoadFromFile (string path)
		{
			try {
				orientation = GetOrientation (new SafeUri (path));
				using FileStream fs = System.IO.File.OpenRead (path);
				return Load (fs, orientation);
			} catch (Exception) {
				Log.Error ($"Error loading photo {path}");
				return null;
			}
		}
	}

	static public Pixbuf LoadAtMaxSize (string path, int maxWidth, int maxHeight)
	{
		var loader = new AspectLoader (maxWidth, maxHeight);
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

	public static Gdk.Pixbuf ScaleToAspect (Gdk.Pixbuf orig, int width, int height)
	{
		Gdk.Rectangle pos;
		double scale = Fit (orig, width, height, false, out pos.Width, out pos.Height);
		pos.X = (width - pos.Width) / 2;
		pos.Y = (height - pos.Height) / 2;

		var scaled = new Pixbuf (Colorspace.Rgb, false, 8, width, height);
		scaled.Fill (0x000000);

		orig.Composite (scaled, pos.X, pos.Y,
				pos.Width, pos.Height,
				pos.X, pos.Y, scale, scale,
				Gdk.InterpType.Bilinear,
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
		byte* ptr = ((byte*)pixels) + (row * rowstride);

		Marshal.Copy (((IntPtr)ptr), dest, 0, width * channels);

		return dest;
	}

	unsafe public static void Pixbuf_SetRow (byte* pixels, byte[] dest, int row, int rowstride, int width, int channels)
	{
		byte* destPtr = pixels + row * rowstride;

		for (int i = 0; i < width * channels; i++) {
			destPtr[i] = dest[i];
		}
	}

	unsafe public static Pixbuf UnsharpMask (Pixbuf src, double radius, double amount, double threshold, ThreadProgressDialog progressDialog)
	{
		// Make sure the pixbuf has an alpha channel before we try to blur it
		src = src.AddAlpha (false, 0, 0, 0);

		var result = Blur (src, (int)radius, progressDialog);

		int sourceRowstride = src.Rowstride;
		int sourceHeight = src.Height;
		int sourceChannels = src.NChannels;
		int sourceWidth = src.Width;

		int resultRowstride = result.Rowstride;
		int resultWidth = result.Width;
		int resultChannels = result.NChannels;

		byte[] srcRow = new byte[sourceRowstride];
		byte[] destRow = new byte[resultRowstride];

		byte* sourcePixels = (byte*)src.Pixels;
		byte* resultPixels = (byte*)result.Pixels;

		for (int row = 0; row < sourceHeight; row++) {
			Pixbuf_GetRow (sourcePixels, row, sourceRowstride, sourceWidth, sourceChannels, srcRow);
			Pixbuf_GetRow (resultPixels, row, resultRowstride, resultWidth, resultChannels, destRow);

			int diff;
			for (int i = 0; i < sourceWidth * sourceChannels; i++) {
				diff = srcRow[i] - destRow[i];
				if (Math.Abs (2 * diff) < threshold)
					diff = 0;

				int val = (int)(srcRow[i] + amount * diff);

				if (val > 255)
					val = 255;
				else if (val < 0)
					val = 0;

				destRow[i] = (byte)val;
			}

			Pixbuf_SetRow (resultPixels, destRow, row, resultRowstride, resultWidth, resultChannels);

			// This is the other half of the progress so start and halfway
			if (progressDialog != null)
				progressDialog.Fraction = ((double)row / ((double)sourceHeight - 1)) * 0.25 + 0.75;
		}

		return result;
	}

	public static Pixbuf Blur (Pixbuf src, int radius, ThreadProgressDialog dialog)
	{
		using ImageSurface sourceSurface = Hyena.Gui.PixbufImageSurface.Create (src);
		using ImageSurface destinationSurface = new ImageSurface (Cairo.Format.Rgb24, src.Width, src.Height);

		// If we do it as a bunch of single lines (rectangles of one pixel) then we can give the progress
		// here instead of going deeper to provide the feedback
		for (int i = 0; i < src.Height; i++) {
			GaussianBlurEffect.RenderBlurEffect (sourceSurface, destinationSurface, new[] { new Gdk.Rectangle (0, i, src.Width, 1) }, radius);

			if (dialog != null) {
				// This only half of the entire process
				double fraction = ((double)i / (double)(src.Height - 1)) * 0.75;
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

	public unsafe static Gdk.Pixbuf RemoveRedeye (Gdk.Pixbuf src, Gdk.Rectangle area)
	{
		return RemoveRedeye (src, area, -15);
	}

	//threshold, factors and comparisons borrowed from the gimp plugin 'redeye.c' by Robert Merkel
	public unsafe static Gdk.Pixbuf RemoveRedeye (Gdk.Pixbuf src, Gdk.Rectangle area, int threshold)
	{
		Gdk.Pixbuf copy = src.Copy ();
		using Gdk.Pixbuf selection = new Gdk.Pixbuf (copy, area.X, area.Y, area.Width, area.Height);
		byte* spix = (byte*)selection.Pixels;
		int h = selection.Height;
		int w = selection.Width;
		int channels = src.NChannels;

		double RED_FACTOR = 0.5133333;
		double GREEN_FACTOR = 1;
		double BLUE_FACTOR = 0.1933333;

		for (int j = 0; j < h; j++) {
			byte* s = spix;
			for (int i = 0; i < w; i++) {
				int adjusted_red = (int)(s[0] * RED_FACTOR);
				int adjusted_green = (int)(s[1] * GREEN_FACTOR);
				int adjusted_blue = (int)(s[2] * BLUE_FACTOR);

				if (adjusted_red >= adjusted_green - threshold
					&& adjusted_red >= adjusted_blue - threshold)
					s[0] = (byte)(((double)(adjusted_green + adjusted_blue)) / (2.0 * RED_FACTOR));
				s += channels;
			}
			spix += selection.Rowstride;
		}

		return copy;
	}

	public static unsafe Pixbuf ColorAdjust (Pixbuf src, double brightness, double contrast,
					  double hue, double saturation, int srcColor, int destColor)
	{
		var adjusted = new Pixbuf (Colorspace.Rgb, src.HasAlpha, 8, src.Width, src.Height);
		ColorAdjust (src, adjusted, brightness, contrast, hue, saturation, srcColor, destColor);
		return adjusted;
	}

	public static FSpot.Cms.Format PixbufCmsFormat (Pixbuf buf)
	{
		return buf.HasAlpha ? FSpot.Cms.Format.Rgba8Planar : FSpot.Cms.Format.Rgb8;
	}

	public static unsafe void ColorAdjust (Pixbuf src, Pixbuf dest,
						   double brightness, double contrast,
						   double hue, double saturation,
						   int srcColor, int destColor)
	{
		if (src.Width != dest.Width || src.Height != dest.Height)
			throw new Exception ("Invalid Dimensions");

		var srgb = Profile.CreateStandardRgb ();

		var bchsw = Profile.CreateAbstract (256, 0.0, brightness, contrast, hue, saturation, srcColor, destColor);

		Profile[] list = { srgb, bchsw, srgb };
		var trans = new Transform (list, PixbufCmsFormat (src), PixbufCmsFormat (dest), Intent.Perceptual, 0x0100);

		ColorAdjust (src, dest, trans);

		trans.Dispose ();
		srgb.Dispose ();
		bchsw.Dispose ();
	}

	public static unsafe void ColorAdjust (Gdk.Pixbuf src, Gdk.Pixbuf dest, Transform trans)
	{
		int width = src.Width;
		byte* srcpix = (byte*)src.Pixels;
		byte* destpix = (byte*)dest.Pixels;

		for (int row = 0; row < src.Height; row++) {
			trans.Apply ((IntPtr)(srcpix + row * src.Rowstride), (IntPtr)(destpix + row * dest.Rowstride), (uint)width);
		}
	}

	public static unsafe bool IsGray (Gdk.Pixbuf pixbuf, int maxDifference)
	{
		int chan = pixbuf.NChannels;

		byte* pix = (byte*)pixbuf.Pixels;
		int h = pixbuf.Height;
		int w = pixbuf.Width;
		int stride = pixbuf.Rowstride;

		for (int j = 0; j < h; j++) {
			byte* p = pix;
			for (int i = 0; i < w; i++) {
				if (Math.Abs (p[0] - p[1]) > maxDifference || Math.Abs (p[0] - p[2]) > maxDifference)
					return false;
				p += chan;
			}
			pix += stride;
		}

		return true;
	}

	public static unsafe void ReplaceColor (Gdk.Pixbuf src, Gdk.Pixbuf dest)
	{
		if (src.HasAlpha || !dest.HasAlpha || (src.Width != dest.Width) || (src.Height != dest.Height))
			throw new ApplicationException ("invalid pixbufs");

		byte* dpix = (byte*)dest.Pixels;
		byte* spix = (byte*)src.Pixels;
		int h = src.Height;
		int w = src.Width;
		for (int j = 0; j < h; j++) {
			byte* d = dpix;
			byte* s = spix;
			for (int i = 0; i < w; i++) {
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
		using var img = FSpot.App.Instance.Container.Resolve<IImageFileFactory> ().Create (uri);
		return img.Orientation;
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

		FSpot.Utils.PixbufUtils.CreateDerivedVersion (source, destination, jpegQuality, pixbuf);
	}
}
