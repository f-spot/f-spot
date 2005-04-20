using System.IO;

public class IthmbDb {
	static public Gdk.Pixbuf Load (string path, int offset)
	{
		path += "F1009_1.ithmb";
		int width = 42;
		int height = 30;
		
		Gdk.Pixbuf image = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
						   false, 8, width, height);

		FileStream stream = new FileStream (path, FileMode.Read);
		BinaryReader reader = new BinaryReader (stream);

		unsafe {
			byte * pixels;
			ushort s;
			int row, col;

			for (row = 0; row < height; row++) {
				pixels = ((byte *)image.Pixels) + row * image.Rowstride;
				for (col = 0; col < width; col++) {
					s = reader.ReadUInt16 ();
					*(pixels++) = (byte)(((s >> 8) & 0xf8) | ((s >> 13) & 0x7)); // r
					*(pixels++) = (byte)(((s >> 3) & 0xfc) | ((s >> 9) & 0x3));  // g
					*(pixels++) = (byte)(((s >> 3) & 0xf8) | ((s >> 2) & 0x7));  // b
				}
			}
		}
		
		return image;
	}

	static void Main (string [] args) 
	{
		Gtk.Application.Init ();
		Gtk.Window win = new Gtk.Window ("iThumbnail Test");
		Gdk.Pixbuf thumb = Load (args [0], 0);
		Gtk.Image image = new Gtk.Image (thumb);
		win.Add (image);
		win.ShowAll ();
		Gtk.Application.Run ();
	}
}
