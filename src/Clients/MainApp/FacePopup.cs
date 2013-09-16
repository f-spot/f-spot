//
// FacePopup.cs
//
// Author:
//   Valentín Barros <valentin@sanva.net>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2010 Paul Werner Bou
// Copyright (C) 2004, 2006 Larry Ewing
// Copyright (C) 2006 Gabriel Burt
// Copyright (C) 2005 Nat Friedman
// Copyright (C) 2013 Valentín Barros
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

using Gdk;

using Gtk;

using Mono.Unix;

using FSpot;
using FSpot.Core;
using FSpot.Utils;
using FSpot.Widgets;

public class FacePopup
{
	public void Activate (EventButton eb, Face face, Face [] faces)
	{
		Menu popup_menu = new Menu ();

		GtkUtil.MakeMenuItem (popup_menu,
			Catalog.GetString ("Rename Face..."),
			"gtk-edit",
			delegate { FacesPageWidget.Instance.FacesWidget.EditSelectedFaceName (); },
			face != null && faces.Length == 1);

		GtkUtil.MakeMenuItem (popup_menu,
			Catalog.GetPluralString ("Delete Face", "Delete Faces", faces.Length),
			"gtk-delete",
			new EventHandler (FacesPageWidget.Instance.FacesWidget.HandleDeleteSelectedFaceCommand),
			face != null);

		if (eb == null)
			popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
		else
			popup_menu.Popup (null, null, null, eb.Button, eb.Time);
	}
}
