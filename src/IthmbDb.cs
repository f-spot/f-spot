using System;
using System.IO;

public class IthmbDb {
	int Width;
	int Height;
	Format Type;

	protected IthmbDb (int width, int height, Format format)
	{
		this.Width = width;
		this.Height = height;
		this.Type = format;
	}

	public enum Format {
		Rgb565,
		Rgb565BE,
		IYUV
	}
	
	public static Gdk.Pixbuf Load (int type, long position, long size, string path)
	{
		IthmbDb db;
		switch (type) {
		case 1009:
			db = new IthmbDb (42, 30, Format.Rgb565);
			break;
		case 1013:  // 90 ccw
			db = new IthmbDb (176, 220, Format.Rgb565);
			break;
		case 1015:
			db = new IthmbDb (130, 88, Format.Rgb565);
			break;
		case 1016:  // untested
			db = new IthmbDb (140, 140, Format.Rgb565);
			break;
		case 1017: // untested
			db = new IthmbDb (56, 56, Format.Rgb565);
			break;
		case 1019:
			db = new IthmbDb (720, 480, Format.IYUV);
			break;
		case 1020: // 90 ccw
			db = new IthmbDb (176, 220, Format.Rgb565BE);
			break;
		case 1024:
			db = new IthmbDb (320, 240, Format.Rgb565);
			break;
		case 1028:
			db = new IthmbDb (100, 100, Format.Rgb565);
			break;
		case 1029:
			db = new IthmbDb (200, 200, Format.Rgb565);
			break;
		case 1036:
			db = new IthmbDb (50, 41, Format.Rgb565);
			break;
		default:
			throw new ApplicationException ("unknown database type");
		}

		if (size != db.Width * db.Height)
			System.Console.WriteLine ("Unexpected thumbnail size");

		return db.Load (path, position);
	}

	public void LoadRgb565 (BinaryReader reader, Gdk.Pixbuf dest, bool IsBigEndian)
	{
		unsafe {
			byte * pixels;
			int row, col;
			ushort s;
			
			bool flip = IsBigEndian == System.BitConverter.IsLittleEndian;
			
			for (row = 0; row < dest.Height; row++) {
				pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				for (col = 0; col < dest.Width; col++) {
					s = reader.ReadUInt16 ();

					if (flip)
						s = (ushort) ((s >> 8) | (s << 8));
					
					*(pixels++) = (byte)(((s >> 8) & 0xf8) | ((s >> 13) & 0x7)); // r
					*(pixels++) = (byte)(((s >> 3) & 0xfc) | ((s >> 9) & 0x3));  // g
					*(pixels++) = (byte)(((s << 3) & 0xf8) | ((s >> 2) & 0x7));  // b
				}
			}
		}
	}

	public void LoadRgb888 (BinaryReader reader, Gdk.Pixbuf dest)
	{
		unsafe {
			byte * pixels;
			int row, col;

			for (row = 0; row < dest.Height; row++) {
				pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				for (col = 0; col < dest.Width; col++) {
					*(pixels++) = reader.ReadByte ();
					*(pixels++) = reader.ReadByte ();
					*(pixels++) = reader.ReadByte ();
				}
			}
		}
	}
	
	public void LoadRgb555 (BinaryReader reader, Gdk.Pixbuf dest)
	{
		unsafe {
			byte * pixels;
			int row, col;
			ushort s;
			
			for (row = 0; row < dest.Height; row++) {
				pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				for (col = 0; col < dest.Width; col++) {
					s = reader.ReadUInt16 ();

					/* rgb555 */
					*(pixels++) = (byte)(((s & 0x7c00) >> 7) | ((s & 0x7000) >> 12)); // r
					*(pixels++) = (byte)(((s & 0x03e0) >> 2) | ((s & 0x0380) >> 7));  // g
					*(pixels++) = (byte)(((s & 0x001f) << 3) | ((s & 0x001c) >> 2));  // b
				}
			}
		}
	}

	public static int Clamp (int val, int bottom, int top)
	{
		if (val < bottom)
			return bottom;
		else if (val > top)
			return top;

		return val;
	}

	private void UnpackYUV (ushort y, ushort u, ushort v, out int r, out int g, out int b)
	{
		r = Clamp ((int) (y + (1.370705 * (v - 128))), 0, 255); // r
		g = Clamp ((int) (y - (0.698001 * (v - 128)) - (0.3337633 * (u - 128))), 0, 255); // g
		b = Clamp ((int) (y + (1.732446 * (u -128))), 0, 255); // b
	}

	public void LoadIYUV (BinaryReader reader, Gdk.Pixbuf dest)
	{
		unsafe {
			 byte * pixels;
			 ushort y0, y1, u, v;
			 int r, g, b;
			 int row, col;

			 for (row = 0; row < dest.Height; row += 2) {
				 pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				 for (col = 0; col < dest.Width; col += 2) {
					 u = reader.ReadByte ();
					 y0 = reader.ReadByte ();
					 v = reader.ReadByte ();
					 y1 = reader.ReadByte ();
					 
					 UnpackYUV (y0, u, v, out r, out g, out b);
					 *(pixels ++) = (byte) r;
					 *(pixels ++) = (byte) g;
					 *(pixels ++) = (byte) b;
					 
					 UnpackYUV (y1, u, v, out r, out g, out b);
					 *(pixels ++) = (byte) r;
					 *(pixels ++) = (byte) g;
					 *(pixels ++) = (byte) b;
				 }
			 }
			 for (row = 1; row < dest.Height; row += 2) {
				 pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				 for (col = 0; col < dest.Width; col += 2) {
					 u = reader.ReadByte ();
					 y0 = reader.ReadByte ();
					 v = reader.ReadByte ();
					 y1 = reader.ReadByte ();

					 UnpackYUV (y0, u, v, out r, out g, out b);
					 *(pixels ++) = (byte) r;
					 *(pixels ++) = (byte) g;
					 *(pixels ++) = (byte) b;
					 
					 UnpackYUV (y1, u, v, out r, out g, out b);
					 *(pixels ++) = (byte) r;
					 *(pixels ++) = (byte) g;
					 *(pixels ++) = (byte) b;
				 }
			 }
		 }
	 }

	 public Gdk.Pixbuf Load (string path, long offset)
	 {
		 int width = this.Width;
		 int height = this.Height;
		 Gdk.Pixbuf image = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
						    false, 8, width, height);
		 FileStream stream = new FileStream (path, FileMode.Open);
		 
		 stream.Position = offset;
		 BinaryReader reader = new BinaryReader (stream);
		 
		 switch (this.Type) {
		 case Format.IYUV:
			 LoadIYUV (reader, image);
			 break;
		 case Format.Rgb565BE:
			 LoadRgb565 (reader, image, true);
			 break;
		 case Format.Rgb565:
			 LoadRgb565 (reader, image, false);
			 break;
		 }

		 
		 stream.Close ();
		 return image;
	}
	
	private ushort [] PackIYUV (Gdk.Pixbuf src)
	{
		int row, col;
		int r, g, b;
		int y, u, v;
		ushort [] packed;
		int i;
		int width = src.Width;
		int height = src.Height;

		unsafe {
			byte * pixels;

			packed = new ushort [width * height];
			for (row = 0; row < height; row ++) {
				pixels = ((byte *)src.Pixels) + row * src.Rowstride;
				i = row * width / 2;
				if (row % 2 > 0)
					i += (height - 1)  * width / 2;
				
				for (col = 0; col < width; col ++) {
					r = *(pixels ++);
					g = *(pixels ++);
					b = *(pixels ++);

#if true
					y = ((16829 * r + 33039 * g +  6416 * b + 32768) >> 16) + 16;
					u = ((-9714 * r - 19071 * g + 28784 * b + 32768) >> 16) + 128;
					v = ((28784 * r - 24103 * g -  4681 * b + 32768) >> 16) + 128;
#else
					// These were taken directly from the jfif spec
					y  = (int)(   0.299  * r + 0.587  * g + 0.114  * b);
					u  = (int)(  -0.1687 * r - 0.3313 * g + 0.5    * b + 128);
					v  = (int)(   0.5    * r - 0.4187 * g - 0.0813 * b + 128);
#endif
					y = Clamp (y, 0, 255);
					u = Clamp (u, 0, 255);
					v = Clamp (v, 0, 255);
					//y = Clamp (y, 16, 235);
					//u = Clamp (u, 16, 240);
					//v = Clamp (v, 16, 240);
					
					if (col % 2 > 0)
						packed [i ++] = (ushort) ((y << 8) | v);
					else
						packed [i ++] = (ushort) ((y << 8) | u);
				}
			}
		}

		return packed;
	}

	private ushort [] PackRgb565 (Gdk.Pixbuf src, bool IsBigEndian)
	{
		int row, col;
		byte r, g, b;
		ushort [] packed;
		int i;
		int width = src.Width;
		int height = src.Height;
		ushort s;

		bool flip = IsBigEndian == System.BitConverter.IsLittleEndian;

		unsafe {
			byte * pixels;			 
			
			packed = new ushort [width * height];
			for (row = 0; row < height; row ++) {
				pixels = ((byte *)src.Pixels) + row * src.Rowstride;
				i = row * width;
				
				for (col = 0; col < width; col ++) {
					r = *(pixels ++);
					g = *(pixels ++);
					b = *(pixels ++);
					
					s = (ushort) (((r & 0xf8) << 8) | ((g & 0xfc) << 3) | (b >> 3));
					
					if (flip)
						s = (ushort)((s >> 8) | (s << 8));

					packed [i ++] = s;
				}
			}
		}

		return packed;
	}

	public void Save (Gdk.Pixbuf src, string path, int offset) 
	{
		int width = this.Width;
		int height = this.Height;
		ushort [] packed = null;
		
		switch (this.Type) {
		case Format.Rgb565:
			packed = PackRgb565 (src, false);
			break;
		case Format.Rgb565BE:
			packed = PackRgb565 (src, true);
			break;
		case Format.IYUV:
			packed = PackIYUV (src);
			break;
		}
		
		FileStream stream = new FileStream (path, FileMode.Open);
		stream.Position = width * height * 2 * offset;
		BinaryWriter writer = new BinaryWriter (stream);
		foreach (short val in packed) {
			writer.Write (val);
		}
		stream.Close ();
	}
	
	static void Main (string [] args)
	{
		Gtk.Application.Init ();
			
		string path = args [0];
		string name = System.IO.Path.GetFileName (path);
		int type = Int32.Parse (name.Substring (1, 4));
		

		Console.WriteLine ("path {0}, name {1}, id {2}", path, name, type);

		Gdk.Pixbuf thumb = IthmbDb.Load (type,
						 Int64.Parse (args [1]),
						 0,
						 path);

		Gtk.Window win = new Gtk.Window ("iThumbnail Test");
		Gtk.HBox hbox = new Gtk.HBox ();
		Gtk.VBox vbox = new Gtk.VBox ();
		win.Add (hbox);
		hbox.PackStart (vbox);
		Gtk.Image image = new Gtk.Image (thumb);
		vbox.PackStart (image);
		win.ShowAll ();
		
		Gtk.Application.Run ();
	}
#if false	
	internal static void DisplayItem (iPodSharp.ImageItemRecord item, string basepath)
	{		
		Gtk.Window win = new Gtk.Window ("iThumbnail Test");
		Gtk.HBox hbox = new Gtk.HBox ();
		Gtk.VBox vbox = new Gtk.VBox ();
		win.Add (hbox);
		hbox.PackStart (vbox);

		foreach (iPodSharp.ImageDataObjectRecord file in item.Versions) {
			if (file.Child.CorrelationID > 1) {
				string path = file.Child.Path.Replace (':','/');
				path = basepath + path;

				Gdk.Pixbuf thumb = IthmbDb.Load (file.Child.CorrelationID, 
								 file.Child.ThumbPosition, 
								 file.Child.ThumbSize,
								 path);
				Gtk.Image image = new Gtk.Image (thumb);
				vbox.PackStart (image);
			}
		}

		win.ShowAll ();
	}
#endif
}
