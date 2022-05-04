//
// CellContext.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena.Gui.Theming;

namespace Hyena.Data.Gui
{
	public class CellContext
	{
		public CellContext ()
		{
			Opaque = true;
		}

		public Cairo.Context Context { get; set; }
		public Pango.Layout Layout { get; set; }
		public Pango.FontDescription FontDescription { get; set; }
		public Gtk.Widget Widget { get; set; }
		public Gtk.StateType State { get; set; }
		public Gdk.Drawable Drawable { get; set; }
		public Theme Theme { get; set; }
		public Gdk.Rectangle Area { get; set; }
		public Gdk.Rectangle Clip { get; set; }
		public bool TextAsForeground { get; set; }
		public bool Opaque { get; set; }
		public int ViewRowIndex { get; set; }
		public int ViewColumnIndex { get; set; }
		public int ModelRowIndex { get; set; }
		public bool IsRtl { get; set; }
	}
}
