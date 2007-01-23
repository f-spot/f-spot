using Gtk;
using Gdk;
using System;
using System.Runtime.InteropServices;
using Mono.Unix;

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
			FromDef ("f-spot-adjust-colors", Catalog.GetString ("Adjust Colors"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-autocolor", Catalog.GetString ("Auto Color"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-browse", Catalog.GetString ("Browse"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-camera", Catalog.GetString ("Camera"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-crop", Catalog.GetString ("Crop"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-desaturate", Catalog.GetString ("Desaturate"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-edit-image", Catalog.GetString ("Edit Photo"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-fullscreen", Catalog.GetString ("Fullscreen"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-horizon", Catalog.GetString ("Straighten"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-loading", Catalog.GetString ("Loading"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-new-tag", Catalog.GetString ("Create New Tag"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-question-mark", Catalog.GetString ("Question"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-red-eye", Catalog.GetString ("Reduce Red-Eye"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-rotate-270", Catalog.GetString ("Rotate _Left"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-rotate-90", Catalog.GetString ("Rotate _Right"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-sepia", Catalog.GetString ("Sepia Tone"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-slideshow", Catalog.GetString ("Slideshow"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-soft-focus", Catalog.GetString ("Soft Focus"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-stock_near", Catalog.GetString ("Near"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-stock_far", Catalog.GetString ("Far"), 0, Gdk.ModifierType.ShiftMask, null),
			FromDef ("f-spot-view-restore", Catalog.GetString ("Restore View"), 0, Gdk.ModifierType.ShiftMask, null),
		};

		IconFactory icon_factory = new IconFactory ();
		icon_factory.AddDefault ();

		foreach (Gtk.StockItem item in stock_items) {
			Pixbuf pixbuf = PixbufUtils.LoadFromAssembly (item.StockId + ".png");
			IconSet icon_set = new IconSet (pixbuf);
			icon_factory.Add (item.StockId, icon_set);
			
			Gtk.StockManager.Add (item);
		}
	}
}
