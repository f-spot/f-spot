using Gdk;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;

public enum PixbufOrientation {
	TopLeft = 1,
	TopRight = 2,
	BottomRight = 3,
	BottomLeft = 4,
	LeftTop = 5,
	RightTop = 6,
	RightBottom = 7,
	LeftBottom = 8
}

class PixbufUtils {

	public static Pixbuf ErrorPixbuf = PixbufUtils.LoadFromAssembly ("f-spot-question-mark.png");
	public static Pixbuf LoadingPixbuf = PixbufUtils.LoadFromAssembly ("f-spot-loading.png");

	public static int GetSize (Pixbuf pixbuf)
	{
		return Math.Max (pixbuf.Width, pixbuf.Height);
	}

	public static double Fit (Pixbuf pixbuf,
				  int dest_width, int dest_height,
				  bool upscale_smaller,
				  out int fit_width, out int fit_height)
	{
		if (pixbuf.Width == 0 || pixbuf.Height == 0) {
			fit_width = 0;
			fit_height = 0;
			return 0.0;
		}

		double scale = Math.Min (dest_width / (double)pixbuf.Width,
					 dest_height / (double)pixbuf.Height);
		
		if (scale > 1.0 && !upscale_smaller)
			scale = 1.0;

		fit_width = (int)(scale * pixbuf.Width);
		fit_height = (int)(scale * pixbuf.Height);
		
		return scale;
	}


	// FIXME: These should be in GTK#.  When my patch is committed, these LoadFrom* methods will
	// go away.

	public class AspectLoader : Gdk.PixbufLoader {
		int max_width;
		int max_height;
		PixbufOrientation orientation;

		public AspectLoader (int max_width, int max_height) 
		{
			this.max_height = max_height;
			this.max_width = max_width;
			SizePrepared += HandleSizePrepared;
		}

		private void HandleSizePrepared (object obj, SizePreparedArgs args)
		{
			switch (orientation) {
			case PixbufOrientation.LeftTop:
			case PixbufOrientation.LeftBottom:
			case PixbufOrientation.RightTop:
			case PixbufOrientation.RightBottom:	
				int tmp = max_width;
				max_width = max_height;
				max_height = tmp;
				break;
			default:
				break;
			}

			double scale = Math.Min (max_width / (double)args.Width,
						 max_height / (double)args.Height);
			
			int scale_width = (int)(scale * args.Width);
			int scale_height = (int)(scale * args.Height);

			SetSize (scale_width, scale_height);
		}

		public Pixbuf LoadFromFile (string path)
		{
			FileStream fs;

			try {
				orientation = GetOrientation (path);
				fs = File.OpenRead (path);
				int count;
				
				byte [] data = new byte [8192];
				while (((count = fs.Read (data, 0, 8192)) > 0) && this.Write (data, (uint)count))
					;
				
				this.Close ();
				Gdk.Pixbuf rotated = TransformOrientation (this.Pixbuf, orientation, true);
				
				if (this.Pixbuf != rotated) {
					CopyThumbnailOptions (this.Pixbuf, rotated);
					this.Pixbuf.Dispose ();
				}
				return rotated;
			} catch (Exception e) {
				System.Console.Write ("Error loading image {0}", path);
				return null;
			}

		}
	}

	public static Pixbuf ScaleToMaxSize (Pixbuf pixbuf, int width, int height)
	{
		double scale = Math.Min  (width / (double)pixbuf.Width, height / (double)pixbuf.Height);
		int scale_width = (int)(scale * pixbuf.Width);
		int scale_height = (int)(scale * pixbuf.Height);

		Gdk.Pixbuf result = pixbuf.ScaleSimple (scale_width, scale_height, Gdk.InterpType.Bilinear);

		CopyThumbnailOptions (pixbuf, result);

		return pixbuf.ScaleSimple (scale_width, scale_height, Gdk.InterpType.Bilinear);
	}
		
	static public Pixbuf LoadAtMaxSize (string path, int max_width, int max_height)
	{
		PixbufUtils.AspectLoader loader = new AspectLoader (max_width, max_height);
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
		
		if (value != null)
			return gdk_pixbuf_set_option(pixbuf.Handle, key, value);
		else
			return false;
	}
	
	public static void CopyThumbnailOptions (Gdk.Pixbuf src, Gdk.Pixbuf dest)
	{
		if (src != null && dest != null) {
			PixbufUtils.SetOption (dest, "tEXt::Thumb::URI", src.GetOption ("tEXt::Thumb::URI"));
			PixbufUtils.SetOption (dest, "tEXt::Thumb::MTime", src.GetOption ("tEXt::Thumb::MTime"));
		}
	}

	[DllImport("libgdk_pixbuf-2.0-0.dll")]
	static extern bool gdk_pixbuf_save_to_bufferv (IntPtr raw, out IntPtr data, out uint length, string type, 
						       string [] keys, string [] values, out IntPtr error);
					
	public static byte [] Save (Gdk.Pixbuf pixbuf, string type, string [] options, string [] values)
	{
		IntPtr error = IntPtr.Zero;
		IntPtr data;
		uint length;
		gdk_pixbuf_save_to_bufferv (pixbuf.Handle, 
					    out data, 
					    out length, 
					    type,
					    options, values,
					    out error);

		if (error != IntPtr.Zero) 
			throw new GLib.GException (error);

		byte [] content = new byte [length];
		Marshal.Copy (data, content, 0, (int)length);

		return content;
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
		try {
			return new Pixbuf (System.Reflection.Assembly.GetCallingAssembly (), resource);
		} catch {
			return null;
		}
	}

	[DllImport ("libc")]
	static extern int rename (string oldpath, string newpath);

	public static void SaveAtomic (Gdk.Pixbuf src, string filename, string type, string [] keys, string [] values)
	{
			string tmpname = filename + ".tmp";
			src.Savev (tmpname, type, keys, values);
			if (rename (tmpname, filename) < 0)
				throw new Exception ("Error renaming file");
	}

	public static Gdk.Pixbuf ScaleToAspect (Gdk.Pixbuf orig, int width, int height)
	{
		Gdk.Rectangle pos;
		double scale = Fit (orig, width, height, false, out pos.Width, out pos.Height);
		pos.X = (width - pos.Width) / 2;
		pos.Y = (height - pos.Height) / 2;

		Pixbuf scaled = new Pixbuf (Colorspace.Rgb, false, 8, width, height);
		scaled.Fill (0x000000); 

		System.Console.WriteLine ("pos= {0}", pos.ToString ());

		orig.Composite (scaled, pos.X, pos.Y, 
				pos.Width, pos.Height,
				pos.X, pos.Y, scale, scale,
				Gdk.InterpType.Bilinear,
				255);

		return scaled;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FPixbufJpegMarker {
		public int type;
		public byte *data;
		public int length;
	}

	[DllImport ("libfspot")]
	static extern bool f_pixbuf_save_jpeg (IntPtr src, string path, int quality, FPixbufJpegMarker [] markers, int num_markers);

	public static void SaveJpeg (Pixbuf pixbuf, string path, int quality, Exif.ExifData exif_data)
	{
		// The DCF spec says thumbnails should be 160x120 always
		Pixbuf thumbnail = ScaleToAspect (pixbuf, 160, 120);
		byte [] thumb_data = Save (thumbnail, "jpeg", null, null);
		exif_data.Data = thumb_data;
		thumbnail.Dispose ();

		// Most of the things we will set will be in the 0th ifd
		Exif.ExifContent content = exif_data.GetContents (Exif.ExifIfd.Zero);

		// reset the orientation tag the default is top/left
		content.GetEntry (Exif.ExifTag.Orientation).Reset ();

		// set the write time in the datetime tag
		content.GetEntry (Exif.ExifTag.DateTime).Reset ();

		// set the software tag
		content.GetEntry (Exif.ExifTag.Software).SetData (FSpot.Defines.PACKAGE + " version " + FSpot.Defines.VERSION);

		byte [] data = exif_data.Save ();
		FPixbufJpegMarker [] marker = new FPixbufJpegMarker [0];
		bool result = false;

		unsafe {
			if (data.Length > 0) {
				
				fixed (byte *p = data) {
					marker = new FPixbufJpegMarker [1];
					marker [0].type = 0xe1; // APP1 marker
					marker [0].data = p;
					marker [0].length = data.Length;
					
					result = f_pixbuf_save_jpeg (pixbuf.Handle, path, quality, marker, marker.Length);
				}					
			} else
				result = f_pixbuf_save_jpeg (pixbuf.Handle, path, quality, marker, marker.Length);
			
		}

		if (result == false)
			throw new System.Exception ("Error Saving File");
	}

	[DllImport ("libfspot")]
	static extern IntPtr f_pixbuf_unsharp_mask (IntPtr src, double radius, double amount, double threshold);

	public static Pixbuf UnsharpMask (Pixbuf src, double radius, double amount, double threshold)
	{
		IntPtr raw_ret = f_pixbuf_unsharp_mask (src.Handle, radius, amount, threshold);
 		Gdk.Pixbuf ret = (Gdk.Pixbuf) GLib.Object.GetObject(raw_ret, true);
		return ret;
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
		return buf.HasAlpha ? Cms.Format.Rgba8 : Cms.Format.Rgb8;
	}

	public static unsafe void ColorAdjust (Pixbuf src, Pixbuf dest, 
					       double brightness, double contrast,
					       double hue, double saturation, 
					       int src_color, int dest_color)
	{
		if (src.Width != dest.Width || src.Height != dest.Height)
			throw new Exception ("Invalid Dimensions");

		//Cms.Profile eos10d = new Cms.Profile ("/home/lewing/ICCProfiles/EOS-10D-True-Color-Non-Linear.icm");
		Cms.Profile srgb = Cms.Profile.CreateSRgb ();
		Cms.Profile bchsw = Cms.Profile.CreateAbstract (10, brightness, contrast,
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

	public static Gdk.Pixbuf GetThumbnail (ExifData data)
	{
		byte [] thumb_data = data.Data;

		if (thumb_data.Length > 0) {
			PixbufOrientation orientation = GetOrientation (data);
			MemoryStream mem = new MemoryStream (thumb_data);
			Gdk.Pixbuf thumb = new Gdk.Pixbuf (mem);
			Gdk.Pixbuf rotated = PixbufUtils.TransformOrientation (thumb, orientation);
			
			if (rotated != thumb)
				thumb.Dispose ();
			
			return rotated;
		}
		return null;
	}

	public static PixbufOrientation GetOrientation (ExifData data)
        {
               byte [] value = data.LookupData (ExifTag.Orientation);
                PixbufOrientation orientation = PixbufOrientation.TopLeft;

		if (value != null) {
			orientation = (PixbufOrientation)value [0];
		}
		
		return orientation;
	}

	public static PixbufOrientation GetOrientation (Exif.ExifData data)
	{
		PixbufOrientation orientation = PixbufOrientation.TopLeft;
		
		Exif.ExifEntry e = data.GetContents (Exif.ExifIfd.Zero).Lookup (Exif.ExifTag.Orientation);

		if (e != null) {
			ushort [] value = e.GetDataUShort ();
			orientation = (PixbufOrientation) value [0];
		}

		return orientation;
	}
	
	public static PixbufOrientation GetOrientation (string path)
	{
		Exif.ExifData data = new Exif.ExifData (path);

		return GetOrientation (data);
	}

	[DllImport("gnomeui-2")]
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

	public static Gdk.Pixbuf TransformOrientation (Gdk.Pixbuf src, PixbufOrientation orientation, bool copy_data)
	{
		Gdk.Pixbuf pixbuf;
		if (src == null)
			return null;
		
		switch (orientation) {
		case PixbufOrientation.LeftTop:
		case PixbufOrientation.LeftBottom:
		case PixbufOrientation.RightTop:
		case PixbufOrientation.RightBottom:	
			pixbuf = new Gdk.Pixbuf (src.Colorspace, src.HasAlpha, 
						 src.BitsPerSample,
						 src.Height, src.Width);
			break;
		case PixbufOrientation.TopRight:
		case PixbufOrientation.BottomRight:
		case PixbufOrientation.BottomLeft:
			pixbuf = new Gdk.Pixbuf (src.Colorspace, src.HasAlpha, 
						 src.BitsPerSample,
						 src.Width, src.Height);
			break;
		default:
			pixbuf = src;
			break;
		}

		if (copy_data && src != pixbuf) 
			TransformAndCopy (src, pixbuf, orientation, new Gdk.Rectangle (0, 0, src.Width, src.Height));

		return pixbuf;
	}

	public static Gdk.Pixbuf TransformOrientation (Gdk.Pixbuf src, PixbufOrientation orientation)
	{
		return TransformOrientation (src, orientation, true);
	}

	public static Gdk.Rectangle TransformOrientation (Gdk.Pixbuf src, Gdk.Rectangle args, PixbufOrientation orientation)
	{
		return TransformOrientation (src.Width, src.Height, args, orientation);
	}
	
	public static Gdk.Rectangle TransformOrientation (int total_width, int total_height, Gdk.Rectangle args, PixbufOrientation orientation)
	{
		Gdk.Rectangle area = args;
		
		switch (orientation) {
		case PixbufOrientation.BottomRight:
			area.X = total_width - args.X - args.Width;
			area.Y = total_height - args.Y - args.Height;
			break;
		case PixbufOrientation.TopRight:
			area.X = total_width - args.X - args.Width;
			break;
		case PixbufOrientation.BottomLeft:
			area.Y = total_height - args.Y - args.Height;
			break;
		case PixbufOrientation.LeftTop:
			area.X = area.Y;
			area.Y = area.X;
			area.Width = args.Height;
			area.Height = args.Width;
			break;
		case PixbufOrientation.RightBottom:
			area.X = total_height - args.Y - args.Height;
			area.Width = args.Height;
			area.Height = args.Width;
			break;
		case PixbufOrientation.RightTop:
			area.X = total_height - args.Y - args.Height;
			area.Y = args.X;
			area.Width = args.Height;
			area.Height = args.Width;
			break;
		case PixbufOrientation.LeftBottom:
			area.X = args.Y;
			area.Y = total_width - args.X - args.Width;
			area.Width = args.Height;
			area.Height = args.Width;
			break;
		default:
			break;
		}
		
		return area;
	}
	
	public static Gdk.Rectangle TransformAndCopy (Gdk.Pixbuf src, Gdk.Pixbuf dest, PixbufOrientation orientation, Gdk.Rectangle args)
	{
		Gdk.Rectangle area = TransformOrientation (src, args, orientation);

		int step = 512;

		Gdk.Rectangle rect = new Gdk.Rectangle (args.X, args.Y, 
							Math.Min (step, args.Width),
							Math.Min (step, args.Height));

		Gdk.Pixbuf tmp = new Gdk.Pixbuf (src.Colorspace, src.HasAlpha, 
						 src.BitsPerSample,
						 rect.Height, rect.Width);

		Gdk.Rectangle subarea;
		while (rect.Y < args.Y + args.Height) {
			while (rect.X < args.X + args.Width) {
				rect.Intersect (args, out subarea);
				Gdk.Rectangle trans = TransformOrientation (src, subarea, orientation);

			        Gdk.Pixbuf ssub = new Gdk.Pixbuf (src, subarea.X, subarea.Y,
								  subarea.Width, subarea.Height);

				
				Gdk.Pixbuf tsub = new Gdk.Pixbuf (tmp, 0, 0, trans.Width, trans.Height);

				CopyWithOrientation (ssub, tsub, orientation);
				
				tsub.CopyArea (0, 0, trans.Width, trans.Height, dest, trans.X, trans.Y);
				

				
				ssub.Dispose ();
				tsub.Dispose ();
				rect.X += rect.Width;
			}
			rect.X = args.X;
			rect.Y += rect.Height;
		}

		tmp.Dispose ();
		return area;
	}

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

	[DllImport ("libfspot")]
	static extern void f_pixbuf_copy_with_orientation (IntPtr src, IntPtr dest, int orientation);

	public static void CopyWithOrientation (Gdk.Pixbuf src, Gdk.Pixbuf dest, PixbufOrientation orientation)
	{
		f_pixbuf_copy_with_orientation (src.Handle, dest.Handle, (int)orientation);
	}

#if false
	[DllImport("glibsharpglue")]
	static extern int gtksharp_object_get_ref_count (IntPtr obj);
	
	public static int RefCount (GLib.Object obj) {
		return gtksharp_object_get_ref_count (obj.Handle);
	}
#endif
}
