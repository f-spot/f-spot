using Gdk;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;

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

	public class AspectLoader : Gdk.PixbufLoader {
		int max_width;
		int max_height;

		// FIXME: this should be a property
		public bool ScaleAlongLongestEdge = false;

		public AspectLoader (int max_width, int max_height) 
		{
			this.max_height = max_height;
			this.max_width = max_width;
			SizePrepared += HandleSizePrepared;
		}

		private void HandleSizePrepared (object obj, SizePreparedArgs args)
		{
			double scale;

			if (ScaleAlongLongestEdge) {
				if (args.Width > args.Height)
					scale = max_width / (double)args.Width;
				else
					scale = max_height / (double)args.Height;
			} else {
				scale = Math.Min (max_width / (double)args.Width,
						  max_height / (double)args.Height);
			}
				
			
			int scale_width = (int)(scale * args.Width);
			int scale_height = (int)(scale * args.Height);

			SetSize (scale_width, scale_height);
		}

		public Pixbuf LoadFromFile (string path)
		{
			FileStream fs;
			try {
				fs = File.OpenRead (path);
			} catch (Exception e) {
				return null;
			}

			int count;
			
			byte [] data = new byte [8192];
			while (((count = fs.Read (data, 0, 8192)) > 0) && this.Write (data, (uint)count))
				;
			
			this.Close ();
			return this.Pixbuf;
		}
	}

	public static Pixbuf ScaleToMaxSize (Pixbuf pixbuf, int width, int height)
	{
		double scale = Math.Min  (width / (double)pixbuf.Width, height / (double)pixbuf.Height);
	
		int scale_width = (int)(scale * pixbuf.Width);
		int scale_height = (int)(scale * pixbuf.Height);
		return pixbuf.ScaleSimple (scale_width, scale_height, Gdk.InterpType.Bilinear);
	}

	static public Pixbuf LoadAtMaxSize (string path, int max_width, int max_height)
	{
		PixbufUtils.AspectLoader loader = new AspectLoader (max_width, max_height);
		return loader.LoadFromFile (path);
	}

	static public Pixbuf LoadAtMaxEdgeSize (string path, int longest_edge)
	{
		PixbufUtils.AspectLoader loader = new AspectLoader (longest_edge, longest_edge);
		loader.ScaleAlongLongestEdge = true;
		return loader.LoadFromFile (path);
	}

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
	

	// 
	// FIXME this is actually not public api and we should do a verison check,
	// but frankly I'm irritated that it isn't public so I don't much care.
	//
	[DllImport("libgdk_pixbuf-2.0-0.dll")]
	static extern bool gdk_pixbuf_set_option(IntPtr raw, string key, string value);
	
	public static bool SetOption(Gdk.Pixbuf pixbuf, string key, string value)
	{
		bool ret = gdk_pixbuf_set_option(pixbuf.Handle, key, value);
		return ret;
	}
	
	public static Pixbuf TagIconFromPixbuf (Pixbuf source)
	{
		// FIXME 50x50 crashes Pixdata.Serialize... what a mess.
		int size = 52;
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
		

	static public Pixbuf LoadFromAssembly (System.Reflection.Assembly assembly, string resource)
	{
		System.IO.Stream s = assembly.GetManifestResourceStream (resource);
		if (s == null)
			return null;
		else
			return LoadFromStream (s);
	}

	static public Pixbuf LoadFromScreen () {
		Screen screen = Display.Default.GetScreen (0);
		Drawable d = screen.RootWindow;
		int width = screen.Width;
		int height = screen.Height;
		
		//
		// We use the screen width and height because that reflects
		// the current resolution, the RootWindow can actually be different.
		//

		Pixbuf buf = new Pixbuf (Colorspace.Rgb, false, 8, width, height);
		
		return buf.GetFromDrawable (d,
					    d.Colormap, 0, 0, 0, 0, 
					    width, height);
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
