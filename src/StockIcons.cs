using Gtk;
using Gdk;
using System;
using System.Runtime.InteropServices;

public class StockIcons {
	static string [] stock_icons = {
		"f-spot-camera",
		"f-spot-crop",
		"f-spot-question-mark"
	};

	// FIXME something's wonky here.
	[DllImport ("libgobject-2.0-0.dll")]
	static extern void g_object_ref (IntPtr raw);

	public static void Initialize ()
	{
		IconFactory icon_factory = new IconFactory ();
		icon_factory.AddDefault ();

		foreach (string name in stock_icons) {
			// FIXME something's wonky here.
			Pixbuf pixbuf = new Pixbuf (null, name + ".png");
			g_object_ref (pixbuf.Handle);

			IconSet icon_set = new IconSet (pixbuf);
			icon_factory.Add (name, icon_set);
		}
	}
}
