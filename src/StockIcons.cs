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

	public static void Initialize ()
	{
		IconFactory icon_factory = new IconFactory ();
		icon_factory.AddDefault ();

		foreach (string name in stock_icons) {
			Pixbuf pixbuf = PixbufUtils.LoadFromAssembly (name + ".png");
			IconSet icon_set = new IconSet (pixbuf);
			icon_factory.Add (name, icon_set);
		}
	}
}
