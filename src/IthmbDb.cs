using System;
using System.IO;

public class IthmbDb {
	string Name;
	int Width;
	int Height;
	bool YUV;

	protected IthmbDb (string name, int width, int height, bool yuv)
	{
		this.Name = name;
		this.Width = width;
		this.Height = height;
		this.YUV = yuv;
	}
	
        public static IthmbDb Thumbnail = new IthmbDb ("F1009", 42, 30, false);
	public static IthmbDb Slide = new IthmbDb ("F1015", 130, 88, false);
	public static IthmbDb FullScreen = new IthmbDb ("F1013", 176, 220, false);
	public static IthmbDb External = new IthmbDb ("F1019", 720, 480, true);
	
	public void LoadRgb565 (BinaryReader reader, Gdk.Pixbuf dest)
	{
		unsafe {
			byte * pixels;
			int row, col;
			ushort s;

			for (row = 0; row < dest.Height; row++) {
				pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				for (col = 0; col < dest.Width; col++) {
					s = reader.ReadUInt16 ();

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

	public void LoadIYUV (BinaryReader reader, Gdk.Pixbuf dest)
	{
		unsafe {
			 byte * pixels;
			 ushort y0, y1, u, v;
			 int row, col;
			 int c;

			 for (row = 0; row < dest.Height; row += 2) {
				 pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				 for (col = 0; col < dest.Width; col += 2) {
					 u = reader.ReadByte ();
					 y0 = reader.ReadByte ();
					 v = reader.ReadByte ();
					 y1 = reader.ReadByte ();

					 c = (int) Math.Max (0, Math.Min (255, y0 + (1.370705 * (v - 128)))); // r
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;
					 c = (int) Math.Max (0, Math.Min (255, y0 - (0.698001 * (v - 128)) - (0.3337633 * (u - 128)))); // g
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;
					 c = (int) Math.Max (0, Math.Min (255, y0 + (1.732446 * (u -128)))); // b
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;

					 c = (int) Math.Max (0, Math.Min (255, y1 + (1.370705 * (v - 128)))); // r
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;
					 c = (int) Math.Max (0, Math.Min (255, y1 - (0.698001 * (v - 128)) - (0.3337633 * (u - 128)))); // g
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;
					 c = (int) Math.Max (0, Math.Min (255, y1 + (1.732446 * (u -128)))); // b
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;
				 }
			 }
			 for (row = 1; row < dest.Height; row += 2) {
				 pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				 for (col = 0; col < dest.Width; col += 2) {
					 u = reader.ReadByte ();
					 y0 = reader.ReadByte ();
					 v = reader.ReadByte ();
					 y1 = reader.ReadByte ();

					 c = (int) Math.Max (0, Math.Min (255, y0 + (1.370705 * (v - 128)))); // r
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;
					 c = (int) Math.Max (0, Math.Min (255, y0 - (0.698001 * (v - 128)) - (0.3337633 * (u - 128)))); // g
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;
					 c = (int) Math.Max (0, Math.Min (255, y0 + (1.732446 * (u -128)))); // b
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;

					 c = (int) Math.Max (0, Math.Min (255, y1 + (1.370705 * (v - 128)))); // r
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;
					 c = (int) Math.Max (0, Math.Min (255, y1 - (0.698001 * (v - 128)) - (0.3337633 * (u - 128)))); // g
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;
					 c = (int) Math.Max (0, Math.Min (255, y1 + (1.732446 * (u -128)))); // b
					 //c = c * 220 / 256;
					 *(pixels ++) = (byte) c;
				 }
			 }
		 }
	 }

	 public Gdk.Pixbuf Load (string path, int offset)
	 {
		 int width = this.Width;
		 int height = this.Height;
		 path = Path.Combine (path, this.Name + "_1.ithmb");
		 Gdk.Pixbuf image = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
						    false, 8, width, height);
		 
		 FileStream stream = new FileStream (path, FileMode.Open);
		 
		 
		 stream.Position = width * height * 2 * offset;
		 BinaryReader reader = new BinaryReader (stream);
		 
		 if (!this.YUV) {
			 LoadRgb565 (reader, image);
		 } else {
			 LoadIYUV (reader, image);
		 }
		 
		 stream.Close ();
		 return image;
	}

	public void Save (Gdk.Pixbuf src, string path, int offset) 
	{
		 int width = this.Width;
		 int height = this.Height;
		 int size;
		 int row, col;
		 byte r, g, b;
		 ushort [] packed;
		 int i;

		 path = Path.Combine (path, this.Name + "_1.ithmb");
		 packed = new ushort [width * height];
		 i = 0;
		 unsafe {
			 byte * pixels;

			 for (row = 0; row < height; row ++) {
				 pixels = ((byte *)src.Pixels) + row * src.Rowstride;
				 
				 for (col = 0; col < width; col ++) {
					 r = *(pixels ++);
					 g = *(pixels ++);
					 b = *(pixels ++);

					 if (!this.YUV) 					 
						 packed [i ++] = (ushort) (((r & 0xf8) << 8) | ((g & 0xfc) << 3) | (b >> 3));
					 else {
						 throw new System.Exception ("this isn't supported yet");
					 }
				 }
			 }
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
		Gtk.Window win = new Gtk.Window ("iThumbnail Test");
		Gtk.HBox hbox = new Gtk.HBox ();
		Gtk.VBox vbox = new Gtk.VBox ();
		win.Add (hbox);
		hbox.PackStart (vbox);

		string basepath = args [0];
		int index = System.Int32.Parse (args [1]);

		Gdk.Pixbuf thumb = IthmbDb.Thumbnail.Load (basepath, index);
		Gtk.Image image = new Gtk.Image (thumb);
		vbox.PackStart (image);
		
		if (args.Length > 2)
			IthmbDb.Thumbnail.Save (thumb, basepath, System.Int32.Parse (args [2]));
		
		thumb = IthmbDb.Slide.Load (basepath, index);
		image = new Gtk.Image (thumb);
		vbox.PackStart (image);
		
		thumb = IthmbDb.FullScreen.Load (basepath, index);
		image = new Gtk.Image (thumb);
		vbox.PackStart (image);

		thumb = IthmbDb.External.Load (basepath, index);
		image = new Gtk.Image (thumb);
		hbox.PackStart (image);

		win.ShowAll ();
		Gtk.Application.Run ();
	}
}
