/*
 * ImageTest.cs
 *
 * Author(s)
 *   Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 *
 */
using System;

namespace FSpot {
	public class ImageTest {
		public ImageTest ()
		{
			Gtk.Application.Init ();
		}

		public static string CreateFile (string name, int size)
		{
			using (Gdk.Pixbuf test = new Gdk.Pixbuf (null, "f-spot-32.png")) {
				using (Gdk.Pixbuf tmp = test.ScaleSimple (size, size, Gdk.InterpType.Bilinear)) {
					string path = System.IO.Path.GetTempPath ();
					path = System.IO.Path.Combine (path, name);
					Console.WriteLine (path);
					string extension = System.IO.Path.GetExtension (path);
					string type = "null";
					switch (extension.ToLower ()) {
					case ".jpg":
					case ".jpeg":
						type = "jpeg";
						break;
					case ".png":
						type = "png";
						break;
					case ".tiff":
						type = "tiff";
						break;
					}
					tmp.Save (path, type);
					return path;
				}
			}
		}
	}
}
