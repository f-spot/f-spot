/*
 * FSpot.Util.GtkUtil.cs
 * 
 * Author(s):
 *	Miguel de Icaza  <miguel@ximian.com>
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;

namespace FSpot.Utils
{
	class GtkUtil {
		public static Gtk.MenuItem MakeMenuItem (Gtk.Menu menu, string l, EventHandler e)
		{
			return MakeMenuItem (menu, l, e, true);
		}
		
		public static Gtk.MenuItem MakeMenuItem (Gtk.Menu menu, string l, EventHandler e, bool enabled)
		{
			Gtk.MenuItem i;
			Gtk.StockItem item = Gtk.StockItem.Zero;
	
			if (Gtk.StockManager.Lookup (l, ref item)) {
				i = new Gtk.ImageMenuItem (l, new Gtk.AccelGroup ());
			} else {
				i = new Gtk.MenuItem (l);
			}
	
			if (e != null)
				i.Activated += e;
	
	                i.Sensitive = enabled;
			
			menu.Append (i);
			i.Show ();
	
	        return i;
		}
		
		public static Gtk.MenuItem MakeMenuItem (Gtk.Menu menu, string label, string image_name, EventHandler e, bool enabled)
		{
			Gtk.ImageMenuItem i = new Gtk.ImageMenuItem (label);
			i.Activated += e;
	                i.Sensitive = enabled;
			i.Image = Gtk.Image.NewFromIconName (image_name, Gtk.IconSize.Menu);
			
			menu.Append (i);
			i.Show ();
	
		        return i;
		}
	
		public static Gtk.MenuItem MakeCheckMenuItem (Gtk.Menu menu, string label, EventHandler e, bool enabled, bool active, bool as_radio)
		{
			Gtk.CheckMenuItem i = new Gtk.CheckMenuItem (label);
			i.Activated += e;
			i.Sensitive = enabled;
			i.DrawAsRadio = as_radio;
			i.Active = active;
	
			menu.Append(i);
			i.Show ();
	
	        return i;
		}
	
		public static void MakeMenuSeparator (Gtk.Menu menu)
		{
			Gtk.SeparatorMenuItem i = new Gtk.SeparatorMenuItem ();
			menu.Append (i);
			i.Show ();
		}
		
		public static Gtk.ToolButton ToolButtonFromTheme (string theme_id, string label, bool important)
		{
			Gtk.ToolButton button = new Gtk.ToolButton (null, null);
			button.Label = label;
			button.IconName = theme_id;
			button.IsImportant = important;
			button.UseUnderline = true;
			return button;
		}
	
		public static Gdk.Pixbuf TryLoadIcon (Gtk.IconTheme theme, string icon_name, int size, Gtk.IconLookupFlags flags)
		{
			try {
				return theme.LoadIcon (icon_name, size, flags);
			} catch {
				try {
					return theme.LoadIcon ("gtk-missing-image", size, flags);
				} catch {
					return null;
				}
			}	
		}
	}
}	
