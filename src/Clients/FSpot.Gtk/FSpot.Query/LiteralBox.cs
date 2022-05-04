//  LiteralBox.cs
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

using Gtk;

namespace FSpot.Query
{
	public class LiteralBox : VBox
	{
		readonly GrabHandle handle;

		public LiteralBox ()
		{
			handle = new GrabHandle (24, 8);
			PackEnd (handle, false, false, 0);
			Show ();
		}
	}
}
