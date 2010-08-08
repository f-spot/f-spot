/*
 * FSpot.PhotoPopup
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;

using Gtk;

using Mono.Addins;

using FSpot.Extensions;

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

		public PhotoPopup (Widget parent) : base ()
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
