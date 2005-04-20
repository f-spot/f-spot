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
			ushort s;
			int row, col;

			for (row = 0; row < dest.Height; row++) {
				pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				for (col = 0; col < dest.Width; col++) {
					s = reader.ReadUInt16 ();
#if true
					*(pixels++) = (byte)(((s >> 8) & 0xf8) | ((s >> 13) & 0x7)); // r
					*(pixels++) = (byte)(((s >> 3) & 0xfc) | ((s >> 9) & 0x3));  // g
					*(pixels++) = (byte)(((s >> 3) & 0xf8) | ((s >> 2) & 0x7));  // b
#else
					*(pixels++) = (byte)(((s & 0x7c00) >> 7) | ((s & 0x7000) >> 12)); // r
					*(pixels++) = (byte)(((s & 0x03e0) >> 2) | ((s & 0x0380) >> 7));  // g
					*(pixels++) = (byte)(((s & 0x001f) << 3) | ((s & 0x001c) >> 2));  // b
#endif
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

	 public void LoadIYUV (BinaryReader reader, Gdk.Pixbuf dest)
	 {
		 unsafe {
			 byte * pixels;
			 ushort y0, y1, u, v;
			 int row, col;

			 for (row = 0; row < dest.Height; row += 2) {
				 pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				 for (col = 0; col < dest.Width; col += 2) {
					 u = reader.ReadByte ();
					 y0 = reader.ReadByte ();
					 v = reader.ReadByte ();
					 y1 = reader.ReadByte ();

					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y0 + (1.370705 * (v - 128)))); // r
					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y0 - (0.698001 * (v - 128)) - (0.3337633 * (u - 128)))); // g
					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y0 + (1.732446 * (u -128)))); // b

					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y1 + (1.370705 * (v - 128)))); // r
					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y1 - (0.698001 * (v - 128)) - (0.3337633 * (u - 128)))); // g
					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y1 + (1.732446 * (u -128)))); // b
				 }
			 }
			 for (row = 1; row < dest.Height; row += 2) {
				 pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
				 for (col = 0; col < dest.Width; col += 2) {
					 u = reader.ReadByte ();
					 y0 = reader.ReadByte ();
					 v = reader.ReadByte ();
					 y1 = reader.ReadByte ();

					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y0 + (1.370705 * (v - 128)))); // r
					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y0 - (0.698001 * (v - 128)) - (0.3337633 * (u - 128)))); // g
					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y0 + (1.732446 * (u -128)))); // b

					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y1 + (1.370705 * (v - 128)))); // r
					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y1 - (0.698001 * (v - 128)) - (0.3337633 * (u - 128)))); // g
					 *(pixels++) = (byte) Math.Max (0, Math.Min (255, y1 + (1.732446 * (u -128)))); // b
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


		 stream.Position = width * height * 4 * offset;
		 BinaryReader reader = new BinaryReader (stream);

		 if (!this.YUV) {
			 LoadRgb565 (reader, image);
		 } else {
			 LoadIYUV (reader, image);
		}
		
		return image;
	}

	static void Main (string [] args) 
	{
		Gtk.Application.Init ();
		Gtk.Window win = new Gtk.Window ("iThumbnail Test");
		Gdk.Pixbuf thumb = IthmbDb.Thumbnail.Load (args [0], System.Int32.Parse (args [1]));
		Gtk.Image image = new Gtk.Image (thumb);
		win.Add (image);
		win.ShowAll ();
		win = new Gtk.Window ("iThumbnail Test");
		thumb = IthmbDb.FullScreen.Load (args [0], System.Int32.Parse (args [1]));
		image = new Gtk.Image (thumb);
		win.Add (image);
		win.ShowAll ();
		win = new Gtk.Window ("iThumbnail Test");
		thumb = IthmbDb.Slide.Load (args [0], System.Int32.Parse (args [1]));
		image = new Gtk.Image (thumb);
		win.Add (image);
		win.ShowAll ();
		win = new Gtk.Window ("iThumbnail Test");
		thumb = IthmbDb.External.Load (args [0], System.Int32.Parse (args [1]));
		image = new Gtk.Image (thumb);
		win.Add (image);
		win.ShowAll ();
		Gtk.Application.Run ();
	}
}
