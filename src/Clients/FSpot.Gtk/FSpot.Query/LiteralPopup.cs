//
// TagQueryWidget.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Larry Ewing <lewing@novell.com>
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2006-2007 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
// Copyright (C) 2006-2007 Gabriel Burt
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;
using FSpot.Utils;

namespace FSpot.Query
{
	public class LiteralPopup
	{
		public void Activate (Gdk.EventButton eb, Literal literal)
		{
			Activate (eb, literal, new Gtk.Menu (), true);
		}

		public void Activate (Gdk.EventButton eb, Literal literal, Gtk.Menu popupMenu, bool isPopup)
		{
			/*MenuItem attach_item = new MenuItem (Catalog.GetString ("Find With"));
			TagMenu attach_menu = new TagMenu (attach_item, App.Instance.Database.Tags);
			attach_menu.TagSelected += literal.HandleAttachTagCommand;
			attach_item.ShowAll ();
			popup_menu.Append (attach_item);*/

			if (literal.IsNegated) {
				GtkUtil.MakeMenuItem (popupMenu,
							  string.Format (Strings.IncludePhotoTaggedX, literal.Tag.Name),
							  new EventHandler (literal.HandleToggleNegatedCommand),
							  true);
			} else {
				GtkUtil.MakeMenuItem (popupMenu,
							  string.Format (Strings.ExcludePhotosTaggedX, literal.Tag.Name),
							  new EventHandler (literal.HandleToggleNegatedCommand),
							  true);
			}

			GtkUtil.MakeMenuItem (popupMenu, Strings.RemoveFromSearch,
						  "gtk-remove",
						  new EventHandler (literal.HandleRemoveCommand),
						  true);

			if (isPopup) {
				if (eb != null)
					popupMenu.Popup (null, null, null, eb.Button, eb.Time);
				else
					popupMenu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
			}
		}
	}
}
