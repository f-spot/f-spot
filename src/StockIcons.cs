using Gtk;
using Gdk;
using System;
using System.Runtime.InteropServices;

public class StockIcons {
	static Gtk.StockItem FromDef (string id, 
				      string label, 
				      uint keyval, 
				      Gdk.ModifierType modifier, 
					      string domain)
	{
		Gtk.StockItem item;
		item.StockId = id;
		item.Label = label;
		item.Keyval = keyval;
		item.Modifier = modifier;
		item.TranslationDomain = domain;
		return item;
	}		

	public static void Initialize ()
	{
		Gtk.StockItem [] stock_items = {	
			FromDef ("f-spot-browse", "Browse", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-camera", "Camera", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-crop", "Crop", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-edit-image", "Edit Image", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-fullscreen", "Fullscreen", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-slideshow", "Slideshow", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-logo", "Logo", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-question-mark", "Question", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-red-eye", "Reduce Red-Eye", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-rotate-270", "Rotate _Left", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-rotate-90", "Rotate _Right", 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-loading", "Loading", 0, Gdk.ModifierType.ShiftMask, null)
		};

		IconFactory icon_factory = new IconFactory ();
		icon_factory.AddDefault ();

		foreach (Gtk.StockItem item in stock_items) {
			Pixbuf pixbuf = PixbufUtils.LoadFromAssembly (item.StockId + ".png");
			IconSet icon_set = new IconSet (pixbuf);
			icon_factory.Add (item.StockId, icon_set);
			
			Gtk.StockManager.Add (item, 1);
		}
	}
}
