//  LiteralMenu.cs
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

using Gtk;

namespace FSpot.Query
{
	public class LiteralMenu : Menu
	{
		readonly LiteralPopup popup;
		readonly Literal literal;

		public LiteralMenu (MenuItem item, Literal literal)
		{
			popup = new LiteralPopup ();

			this.literal = literal;

			item.Submenu = this;
			item.Activated += HandlePopulate;
		}

		void HandlePopulate (object obj, EventArgs args)
		{
			foreach (Widget child in Children) {
				Remove (child);
				child.Destroy ();
			}
			popup.Activate (null, literal, this, false);
		}
	}
}
