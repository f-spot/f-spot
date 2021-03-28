//
// GtkUtil.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources;

namespace FSpot.Utils
{
	public static class GtkUtil
	{
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
			var i = new Gtk.ImageMenuItem (label);
			i.Activated += e;
			i.Sensitive = enabled;
			i.Image = Gtk.Image.NewFromIconName (image_name, Gtk.IconSize.Menu);

			menu.Append (i);
			i.Show ();

			return i;
		}

		public static Gtk.MenuItem MakeCheckMenuItem (Gtk.Menu menu, string label, EventHandler e, bool enabled, bool active, bool as_radio)
		{
			var i = new Gtk.CheckMenuItem (label) {
				Sensitive = enabled,
				DrawAsRadio = as_radio,
				Active = active
			};

			i.Activated += e;

			menu.Append (i);
			i.Show ();

			return i;
		}

		public static void MakeMenuSeparator (Gtk.Menu menu)
		{
			var i = new Gtk.SeparatorMenuItem ();
			menu.Append (i);
			i.Show ();
		}

		public static Gtk.ToolButton ToolButtonFromTheme (string theme_id, string label, bool important)
		{
			var button = new Gtk.ToolButton (null, null) {
				Label = label,
				IconName = theme_id,
				IsImportant = important,
				UseUnderline = true
			};

			return button;
		}

		public static Gdk.Pixbuf TryLoadIcon (Gtk.IconTheme theme, string[] names, int size, Gtk.IconLookupFlags flags)
		{
			try {
				var info = theme.ChooseIcon (names, size, flags);
				return info.LoadIcon ();
			} catch {
				try {
					return theme.LoadIcon ("gtk-missing-image", size, flags);
				} catch {
					return null;
				}
			}
		}

		public static Gdk.Pixbuf TryLoadIcon (Gtk.IconTheme theme, string icon_name, int size, Gtk.IconLookupFlags flags)
		{
			try {
				return ResourceLoader.GetIcon (icon_name, size);
				//return theme.LoadIcon (icon_name, size, flags);
			} catch {
				try {
					return theme.LoadIcon ("gtk-missing-image", size, flags);
				} catch {
					return null;
				}
			}
		}

		public static void ModifyColors (Gtk.Widget widget)
		{
			try {
				widget.ModifyFg (Gtk.StateType.Normal, widget.Style.TextColors[(int)Gtk.StateType.Normal]);
				widget.ModifyFg (Gtk.StateType.Active, widget.Style.TextColors[(int)Gtk.StateType.Active]);
				widget.ModifyFg (Gtk.StateType.Selected, widget.Style.TextColors[(int)Gtk.StateType.Selected]);
				widget.ModifyBg (Gtk.StateType.Normal, widget.Style.BaseColors[(int)Gtk.StateType.Normal]);
				widget.ModifyBg (Gtk.StateType.Active, widget.Style.BaseColors[(int)Gtk.StateType.Active]);
				widget.ModifyBg (Gtk.StateType.Selected, widget.Style.BaseColors[(int)Gtk.StateType.Selected]);

			} catch {
				widget.ModifyFg (Gtk.StateType.Normal, widget.Style.Black);
				widget.ModifyBg (Gtk.StateType.Normal, widget.Style.Black);
			}
		}

	}
}
