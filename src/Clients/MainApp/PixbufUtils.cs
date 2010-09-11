//
// FSpot.PixbufUtils.cs
//
// Author(s):
//	Ettore Perazzoli
//	Larry Ewing  <lewing@novell.com>
//	Stephane Delcroix  <stephane@declroix.org>
//
// This is free software. See COPYING for details
//

using Gdk;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;
using FSpot;
using FSpot.Core;
using FSpot.Utils;
using FSpot.Imaging;
using Hyena;
using TagLib.Image;

public static class PixbufUtils {
	static Pixbuf error_pixbuf = null;
	public static Pixbuf ErrorPixbuf {
		get {
			if (error_pixbuf == null)
				error_pixbuf = GtkUtil.TryLoadIcon (FSpot.Core.Global.IconTheme, "f-spot-question-mark", 256, (Gtk.IconLookupFlags)0);
			return error_pixbuf;
		}
	}
	public static Pixbuf LoadingPixbuf = PixbufUtils.LoadFromAssembly ("f-spot-loading.png");

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

		double scale = Math.Min (dest_width / (double)orig_width,
					 dest_height / (double)orig_height);

		if (scale > 1.0 && !upscale_smaller)
			scale = 1.0;

		fit_width = (int) Math.Round (scale * orig_width);
		fit_height = (int) Math.Round (scale * orig_height);

		return scale;
	}


	// FIXME: These should be in GTK#.  When my patch is committed, these LoadFrom* methods will
	// go away.

	public class AspectLoader {
		Gdk.PixbufLoader loader = new Gdk.PixbufLoader ();
		int max_width;
		int max_height;
		ImageOrientation orientation;

		public AspectLoader (int max_width, int max_height)
		{
			this.max_height = max_height;
			this.max_width = max_width;
			loader.SizePrepared += HandleSizePrepared;
		}

		private void HandleSizePrepared (object obj, SizePreparedArgs args)
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

			int scale_width = 0;
			int scale_height = 0;

			double scale = Fit (args.Width, args.Height, max_width, max_height, true, out scale_width, out scale_height);

			if (scale < 1.0)
				loader.SetSize (scale_width, scale_height);
		}

		public Pixbuf Load (System.IO.Stream stream, ImageOrientation orientation)
		{
			int count;
			byte [] data = new byte [8192];
			while (((count = stream.Read (data, 0, data.Length)) > 0) && loader.Write (data, (ulong)count))
				;

			loader.Close ();
			Pixbuf orig = loader.Pixbuf;
			Gdk.Pixbuf rotated = FSpot.Utils.PixbufUtils.TransformOrientation (orig, orientation);

			if (orig != rotated) {
				orig.Dispose ();
			}
			loader.Dispose ();
			return rotated;
		}

		public Pixbuf LoadFromFile (string path)
		{
			try {
				orientation = GetOrientation (path);
				using (FileStream fs = System.IO.File.OpenRead (path)) {
					return Load (fs, orientation);
				}
			} catch (Exception) {
				Log.ErrorFormat ("Error loading photo {0}", path);
				return null;
			}
		}
	}

	public static Pixbuf ScaleToMaxSize (Pixbuf pixbuf, int width, int height)
	{
		return ScaleToMaxSize (pixbuf, width, height, true);
	}

	public static Pixbuf ScaleToMaxSize (Pixbuf pixbuf, int width, int height, bool upscale)
	{
		int scale_width = 0;
		int scale_height = 0;
		double scale = Fit (pixbuf, width, height, upscale, out scale_width, out scale_height);

		Gdk.Pixbuf result;
		if (upscale || (scale < 1.0))
			result = pixbuf.ScaleSimple (scale_width, scale_height, (scale_width > 20) ? Gdk.InterpType.Bilinear : Gdk.InterpType.Nearest);
		else
			result = pixbuf.Copy ();

		return result;
	}

	static public Pixbuf LoadAtMaxSize (string path, int max_width, int max_height)
	{
		PixbufUtils.AspectLoader loader = new AspectLoader (max_width, max_height);
		return loader.LoadFromFile (path);
	}

	public static Pixbuf TagIconFromPixbuf (Pixbuf source)
	{
		return IconFromPixbuf (source, (int) Tag.IconSize.Large);
	}

	public static Pixbuf IconFromPixbuf (Pixbuf source, int size)
	{
		Pixbuf tmp = null;
		Pixbuf icon = null;

		if (source.Width > source.Height)
			source = tmp = new Pixbuf (source, (source.Width - source.Height) /2, 0, source.Height, source.Height);
		else if (source.Width < source.Height)
			source = tmp = new Pixbuf (source, 0, (source.Height - source.Width) /2, source.Width, source.Width);

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

		Pixbuf scaled = new Pixbuf (Colorspace.Rgb, false, 8, width, height);
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

		Pixbuf flattened = new Pixbuf (Colorspace.Rgb, false, 8, pixbuf.Width, pixbuf.Height);
		pixbuf.CompositeColor (flattened, 0, 0,
				       pixbuf.Width, pixbuf.Height,
				       0, 0, 1, 1,
				       InterpType.Bilinear,
				       255, 0, 0, 2000, 0xffffff, 0xffffff);

		return flattened;
	}

	[DllImport ("libfspot")]
	static extern IntPtr f_pixbuf_unsharp_mask (IntPtr src, double radius, double amount, double threshold);

	public static Pixbuf UnsharpMask (Pixbuf src, double radius, double amount, double threshold)
	{
		IntPtr raw_ret = f_pixbuf_unsharp_mask (src.Handle, radius, amount, threshold);
		Gdk.Pixbuf ret = (Gdk.Pixbuf) GLib.Object.GetObject(raw_ret, true);
		return ret;
	}

	[DllImport ("libfspot")]
	static extern IntPtr f_pixbuf_blur (IntPtr src, double radius);

	public static Pixbuf Blur (Pixbuf src, double radius)
	{
		IntPtr raw_ret = f_pixbuf_blur (src.Handle, radius);
		Gdk.Pixbuf ret = (Gdk.Pixbuf) GLib.Object.GetObject(raw_ret, true);
		return ret;
	}

	public unsafe static Gdk.Pixbuf RemoveRedeye (Gdk.Pixbuf src, Gdk.Rectangle area)
	{
		return RemoveRedeye (src, area, -15);
	}

	public unsafe static Gdk.Pixbuf RemoveRedeye (Gdk.Pixbuf src, Gdk.Rectangle area, int threshold)
	//threshold, factors and comparisons borrowed from the gimp plugin 'redeye.c' by Robert Merkel
	{
		Gdk.Pixbuf copy = src.Copy ();
		Gdk.Pixbuf selection = new Gdk.Pixbuf (copy, area.X, area.Y, area.Width, area.Height);
		byte *spix = (byte *)selection.Pixels;
		int h = selection.Height;
		int w = selection.Width;
		int channels = src.NChannels;

		double RED_FACTOR = 0.5133333;
		double GREEN_FACTOR = 1;
		double BLUE_FACTOR = 0.1933333;

		for (int j = 0; j < h; j++) {
			byte *s = spix;
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
					  double hue, double saturation, int src_color, int dest_color)
	{
		Pixbuf adjusted = new Pixbuf (Colorspace.Rgb, src.HasAlpha, 8, src.Width, src.Height);
		PixbufUtils.ColorAdjust (src, adjusted, brightness, contrast, hue, saturation, src_color, dest_color);
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

		Cms.Profile srgb = Cms.Profile.CreateStandardRgb ();

		Cms.Profile bchsw = Cms.Profile.CreateAbstract (256,
								0.0,
								brightness, contrast,
								hue, saturation, src_color,
								dest_color);

		Cms.Profile [] list = new Cms.Profile [] { srgb, bchsw, srgb };
		Cms.Transform trans = new Cms.Transform (list,
							 PixbufCmsFormat (src),
							 PixbufCmsFormat (dest),
							 Cms.Intent.Perceptual, 0x0100);

		ColorAdjust (src, dest, trans);

		trans.Dispose ();
		srgb.Dispose ();
		bchsw.Dispose ();
	}


	public static unsafe void ColorAdjust (Gdk.Pixbuf src, Gdk.Pixbuf dest, Cms.Transform trans)
	{
		int width = src.Width;
		byte * srcpix  = (byte *) src.Pixels;
		byte * destpix = (byte *) dest.Pixels;

		for (int row = 0; row < src.Height; row++) {
			trans.Apply ((IntPtr) (srcpix + row * src.Rowstride),
				     (IntPtr) (destpix + row * dest.Rowstride),
				     (uint)width);
		}

	}

	public static unsafe bool IsGray (Gdk.Pixbuf pixbuf, int max_difference)
	{
		int chan = pixbuf.NChannels;

		byte *pix = (byte *)pixbuf.Pixels;
		int h = pixbuf.Height;
		int w = pixbuf.Width;
		int stride = pixbuf.Rowstride;

		for (int j = 0; j < h; j++) {
			byte *p = pix;
			for (int i = 0; i < w; i++) {
				if (Math.Abs (p[0] - p[1]) > max_difference || Math.Abs (p[0] - p [2]) > max_difference) {
					goto Found;
				}
				p += chan;
			}
			pix += stride;
		}

		return true;

	Found:
		return false;
	}

	public static unsafe void ReplaceColor (Gdk.Pixbuf src, Gdk.Pixbuf dest)
	{
		if (src.HasAlpha || !dest.HasAlpha || (src.Width != dest.Width) || (src.Height != dest.Height))
			throw new ApplicationException ("invalid pixbufs");

		byte *dpix = (byte *)dest.Pixels;
		byte *spix = (byte *)src.Pixels;
		int h = src.Height;
		int w = src.Width;
		for (int j = 0; j < h; j++) {
			byte *d = dpix;
			byte *s = spix;
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
		using (var img = ImageFile.Create (uri)) {
			return img.Orientation;
		}
	}

	[Obsolete ("Use GetOrientation (SafeUri) instead")]
	public static ImageOrientation GetOrientation (string path)
	{
        return GetOrientation (new SafeUri (path));
	}

	[DllImport("libgnomeui-2-0.dll")]
	static extern IntPtr gnome_thumbnail_scale_down_pixbuf(IntPtr pixbuf, int dest_width, int dest_height);

	public static Gdk.Pixbuf ScaleDown (Gdk.Pixbuf src, int width, int height)
	{
		IntPtr raw_ret = gnome_thumbnail_scale_down_pixbuf(src.Handle, width, height);
		Gdk.Pixbuf ret;
		if (raw_ret == IntPtr.Zero)
			ret = null;
		else
			ret = (Gdk.Pixbuf) GLib.Object.GetObject(raw_ret, true);
		return ret;
	}

    public static void CreateDerivedVersion (SafeUri source, SafeUri destination)
    {
        CreateDerivedVersion (source, destination, 95);
    }

    public static void CreateDerivedVersion (SafeUri source, SafeUri destination, uint jpeg_quality)
    {
        if (source.GetExtension () == destination.GetExtension ()) {
            // Simple copy will do!
            var file_from = GLib.FileFactory.NewForUri (source);
            var file_to = GLib.FileFactory.NewForUri (destination);
            file_from.Copy (file_to, GLib.FileCopyFlags.AllMetadata | GLib.FileCopyFlags.Overwrite, null, null);
            return;
        }

        // Else make a derived copy with metadata copied
        using (var img = ImageFile.Create (source)) {
            using (var pixbuf = img.Load ()) {
                CreateDerivedVersion (source, destination, jpeg_quality, pixbuf);
            }
        }
    }

    public static void CreateDerivedVersion (SafeUri source, SafeUri destination, uint jpeg_quality, Pixbuf pixbuf)
    {
        SaveToSuitableFormat (destination, pixbuf, jpeg_quality);

        using (var metadata_from = Metadata.Parse (source)) {
            using (var metadata_to = Metadata.Parse (destination)) {
                metadata_to.CopyFrom (metadata_from);

                // Reset orientation to make sure images appear upright.
                metadata_to.ImageTag.Orientation = ImageOrientation.TopLeft;
                metadata_to.Save ();
            }
        }
    }

    private static void SaveToSuitableFormat (SafeUri destination, Pixbuf pixbuf, uint jpeg_quality)
    {
        // FIXME: this needs to work on streams rather than filenames. Do that when we switch to
        // newer GDK.
        var extension = destination.GetExtension ().ToLower ();
        if (extension == ".png") {
            pixbuf.Save (destination.LocalPath, "png");
        } else if (extension == ".jpg" || extension == ".jpeg") {
            pixbuf.Save (destination.LocalPath, "jpeg", jpeg_quality);
        } else {
            throw new NotImplementedException ("Saving this file format is not supported");
        }
    }

#region Gdk hackery

    // This hack below is needed because there is no wrapped version of
    // Save which allows specifying the variable arguments (it's not
    // possible with p/invoke).

    [DllImport("libgdk_pixbuf-2.0-0.dll")]
    static extern bool gdk_pixbuf_save(IntPtr raw, IntPtr filename, IntPtr type, out IntPtr error,
            IntPtr optlabel1, IntPtr optvalue1, IntPtr dummy);

    private static bool Save (this Pixbuf pixbuf, string filename, string type, uint jpeg_quality)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr nfilename = GLib.Marshaller.StringToPtrGStrdup (filename);
        IntPtr ntype = GLib.Marshaller.StringToPtrGStrdup (type);
        IntPtr optlabel1 = GLib.Marshaller.StringToPtrGStrdup ("quality");
        IntPtr optvalue1 = GLib.Marshaller.StringToPtrGStrdup (jpeg_quality.ToString ());
        bool ret = gdk_pixbuf_save(pixbuf.Handle, nfilename, ntype, out error, optlabel1, optvalue1, IntPtr.Zero);
        GLib.Marshaller.Free (nfilename);
        GLib.Marshaller.Free (ntype);
        GLib.Marshaller.Free (optlabel1);
        GLib.Marshaller.Free (optvalue1);
        if (error != IntPtr.Zero) throw new GLib.GException (error);
        return ret;
    }

#endregion
}
