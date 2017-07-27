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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Unix;

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
						      string.Format (Catalog.GetString ("Include Photos Tagged \"{0}\""), literal.Tag.Name),
						      new EventHandler (literal.HandleToggleNegatedCommand),
						      true);
			} else {
				GtkUtil.MakeMenuItem (popupMenu,
						      string.Format (Catalog.GetString ("Exclude Photos Tagged \"{0}\""), literal.Tag.Name),
						      new EventHandler (literal.HandleToggleNegatedCommand),
						      true);
			}

			GtkUtil.MakeMenuItem (popupMenu, Catalog.GetString ("Remove From Search"),
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
