using Gdk;
using System.Collections;
using System.Runtime.InteropServices;
using System;

class PixbufUtils {

	public static int GetSize (Pixbuf pixbuf)
	{
		return Math.Max (pixbuf.Width, pixbuf.Height);
	}

	public static int ComputeScaledWidth (Pixbuf pixbuf, int size)
	{
		int orig_width = (int) pixbuf.Width;
		int orig_height = (int) pixbuf.Height;

		if (orig_width > orig_height)
			return size;
		else
			return (int) Math.Round ((int) size * ((double) orig_width / (double) orig_height));
	}

	public static int ComputeScaledHeight (Pixbuf pixbuf, int size)
	{
		int orig_width = (int) pixbuf.Width;
		int orig_height = (int) pixbuf.Height;

		if (orig_width > orig_height)
			return (int) Math.Round ((int) size * ((double) orig_height / (double) orig_width));
		else
			return size;
	}

	public static void Fit (Pixbuf pixbuf,
				int dest_width, int dest_height,
				bool upscale_smaller,
				out int fit_width, out int fit_height)
	{
		if (pixbuf.Width == 0 || pixbuf.Height == 0) {
			fit_width = 0;
			fit_height = 0;
			return;
		}

		if (pixbuf.Width <= dest_width && pixbuf.Height <= dest_height && ! upscale_smaller) {
			fit_width = pixbuf.Width;
			fit_height = pixbuf.Height;
			return;
		}

		fit_width = dest_width;
		fit_height = (int) Math.Round ((double) (pixbuf.Height * fit_width) / pixbuf.Width);

		if (fit_height > dest_height) {
			fit_height = dest_height;
			fit_width = (int) Math.Round ((double) (pixbuf.Width * fit_height) / pixbuf.Height);
		}
	}


	// FIXME: These should be in GTK#.  When my patch is committed, these LoadFrom* methods will
	// go away.

	static private Pixbuf LoadFromStream (System.IO.Stream input)
	{
		Gdk.PixbufLoader loader = new Gdk.PixbufLoader ();
		byte [] buffer = new byte [8192];
		int n;

		while ((n = input.Read (buffer, 0, 8192)) != 0)
			loader.Write (buffer, (uint) n);
		
		loader.Close ();
		return loader.Pixbuf;
	}
		
	static public Pixbuf LoadFromAssembly (System.Reflection.Assembly assembly, string resource)
	{
		System.IO.Stream s = assembly.GetManifestResourceStream (resource);
		if (s == null)
			return null;
		else
			return LoadFromStream (s);
	}

	static public Pixbuf LoadFromAssembly (string resource)
	{
		return LoadFromAssembly (System.Reflection.Assembly.GetCallingAssembly (), resource);
	}


	[DllImport ("libfspot")]
	static extern IntPtr f_pixbuf_unsharp_mask (IntPtr src, double radius, double amount, double threshold);

	public static Pixbuf UnsharpMask (Pixbuf src, double radius, double amount, double threshold)
	{
		IntPtr raw_ret = f_pixbuf_unsharp_mask (src.Handle, radius, amount, threshold);
 		Gdk.Pixbuf ret = (Gdk.Pixbuf) GLib.Object.GetObject(raw_ret, true);
		return ret;
	}	

#if STUFF_WE_HAVE_TO_RESTORE

	// Bindings from libf.

	[DllImport ("libfspot")]
	static extern IntPtr f_pixbuf_copy_rotate_90 (IntPtr src, bool counter_clockwise);

	public static Pixbuf Rotate90 (Pixbuf src, bool counter_clockwise)
	{
		return new Pixbuf (f_pixbuf_copy_rotate_90 (src.Handle, counter_clockwise));
	}

	[DllImport ("libfspot")]
	static extern IntPtr f_pixbuf_copy_mirror (IntPtr src, bool mirror, bool flip);

	// FIXME not very readable
	public static Pixbuf Mirror (Pixbuf src, bool mirror, bool flip)
	{
		return new Pixbuf (f_pixbuf_copy_mirror (src.Handle, mirror, flip));
	}

	[DllImport ("libfspot")]
	static extern IntPtr f_pixbuf_copy_apply_brightness_and_contrast (IntPtr src, float brightness, float contrast);

	public static Pixbuf ApplyBrightnessAndContrast (Pixbuf src, float brightness, float contrast)
	{
		return new Pixbuf (f_pixbuf_copy_apply_brightness_and_contrast (src.Handle, brightness, contrast));
	}

	[DllImport ("libfspot")]
	static extern bool f_pixbuf_save_jpeg_atomic (IntPtr pixbuf, string filename, int quality, out IntPtr error);

	public static void SaveAsJpegAtomically (Pixbuf pixbuf, string filename, int quality)
	{
		IntPtr error = IntPtr.Zero;

		if (! f_pixbuf_save_jpeg_atomic (pixbuf.Handle, filename, quality, out error)) {
			throw new GLib.GException (error);
		}
	}

#endif
}
