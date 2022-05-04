//
// ListViewBase.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

using Hyena.Gui.Canvas;

namespace Hyena.Data.Gui
{
	public abstract class ListViewBase : Widget, ICanvasHost
	{
		protected ListViewBase (IntPtr ptr) : base (ptr)
		{
		}

		public ListViewBase ()
		{
		}

		public void QueueDirtyRegion (Gdk.Rectangle region)
		{
			region.Intersect (Allocation);
			QueueDrawArea (region.X, region.Y, region.Width, region.Height);
		}

		public void QueueDirtyRegion (Rect region)
		{
			QueueDirtyRegion ((Gdk.Rectangle)region);
		}

		public void QueueDirtyRegion (Cairo.Rectangle region)
		{
			QueueDirtyRegion (new Gdk.Rectangle () {
				X = (int)Math.Floor (region.X),
				Y = (int)Math.Floor (region.Y),
				Width = (int)Math.Ceiling (region.Width),
				Height = (int)Math.Ceiling (region.Height)
			});
		}

		public void QueueRender (Hyena.Gui.Canvas.CanvasItem item, Rect rect)
		{
			QueueDirtyRegion (rect);
		}

		public abstract Pango.Layout PangoLayout { get; }
		public abstract Pango.FontDescription FontDescription { get; }
	}
}
