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
