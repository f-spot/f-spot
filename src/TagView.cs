using System;
using Gtk;
using Gdk;

public class TagView : Gtk.Layout {
	private int thumbnail_size = 20;
	private Photo photo;
	private static int TAG_ICON_VSPACING = 5;

	public TagView (): base (null, null)
	{
		ExposeEvent += HandleExposeEvent;
	}

	public Photo Current {
		set {
			photo = value;
			SetSizeRequest ((thumbnail_size + TAG_ICON_VSPACING) * photo.Tags.Length,
					thumbnail_size);
			QueueResize ();
			QueueDraw ();
				
		}
	}

	private void HandleExposeEvent (object sender, ExposeEventArgs args)
	{
		if (photo == null)
			return; 

		int tag_x = 0;
		int tag_y = (Allocation.Height - thumbnail_size)/2;
		
		foreach (Tag t in photo.Tags) {
			Pixbuf icon = null;
			
			if (t.Category.Icon == null) {
				if (t.Icon == null)
					continue;
				icon = t.Icon;
			} else {
				Category category = t.Category;
				while (category.Category.Icon != null)
					category = category.Category;
				icon = category.Icon;
			}
			
			Pixbuf scaled_icon;
			if (icon.Width == thumbnail_size) {
				scaled_icon = icon;
			} else {
				scaled_icon = icon.ScaleSimple (thumbnail_size, thumbnail_size, InterpType.Bilinear);
			}
			
			scaled_icon.RenderToDrawable (BinWindow, Style.WhiteGC,
						      0, 0, tag_x, tag_y, thumbnail_size, thumbnail_size,
						      RgbDither.None, 0, 0);
			tag_x += thumbnail_size + TAG_ICON_VSPACING;
		}
		
	}
}
