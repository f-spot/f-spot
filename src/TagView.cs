using System;
using Gtk;
using Gdk;

public class TagView : Gtk.Widget {
	private int thumbnail_size = 20;
	private Photo photo;
	private static int TAG_ICON_VSPACING = 5;

	public TagView ()
	{
			Flags |= (int)WidgetFlags.NoWindow;
	}

	protected TagView (IntPtr raw) : base (raw) {}

	public Photo Current {
		set {
			photo = value;
			SetSizeRequest ((thumbnail_size + TAG_ICON_VSPACING) * photo.Tags.Length,
					thumbnail_size);
			QueueResize ();
			QueueDraw ();
		}
	}

	protected override bool OnExposeEvent (Gdk.EventExpose args)
	{
		if (photo == null)
			return base.OnExposeEvent (args); 

		SetSizeRequest ((thumbnail_size + TAG_ICON_VSPACING) * photo.Tags.Length,
				thumbnail_size);


		int tag_x = Allocation.X;
		int tag_y = Allocation.Y + (Allocation.Height - thumbnail_size)/2;
		
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
			
			scaled_icon.RenderToDrawable (GdkWindow, Style.WhiteGC,
						      0, 0, tag_x, tag_y, thumbnail_size, thumbnail_size,
						      RgbDither.None, tag_x, tag_y);
			tag_x += thumbnail_size + TAG_ICON_VSPACING;
		}
		return base.OnExposeEvent (args);
	}
}
