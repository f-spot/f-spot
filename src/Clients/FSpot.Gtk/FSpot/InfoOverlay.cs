//
// InfoOverlay.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2007 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

using Gtk;

namespace FSpot
{
	public class InfoOverlay : ControlOverlay
	{
		readonly InfoItem box;

		public InfoOverlay (Widget w, BrowsablePointer item) : base (w)
		{
			XAlign = 1.0;
			YAlign = 0.1;
			DefaultWidth = 250;
			box = new InfoItem (item);
			box.BorderWidth = 15;
			Add (box);
			box.ShowAll ();
			Visibility = VisibilityType.Partial;
			KeepAbove = true;
			//WindowPosition = WindowPosition.Mouse;
			AutoHide = false;
		}
	}
}
