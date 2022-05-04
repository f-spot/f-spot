//
// PhotoPopup.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2004-2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Extensions;

using Gtk;

using Mono.Addins;

namespace FSpot
{
	public class PhotoPopup : Gtk.Menu
	{
		protected PhotoPopup (IntPtr handle) : base (handle)
		{
		}

		public PhotoPopup () : this (null)
		{
		}

		public PhotoPopup (Widget parent)
		{
			foreach (MenuNode node in AddinManager.GetExtensionNodes ("/FSpot/Menus/PhotoPopup"))
				Append (node.GetMenuItem (parent));
			ShowAll ();
		}

		public void Activate (Widget toplevel)
		{
			Activate (toplevel, null);
		}

		public void Activate (Widget toplevel, Gdk.EventButton eb)
		{
			if (eb != null)
				Popup (null, null, null, eb.Button, eb.Time);
			else
				Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
		}
	}
}
