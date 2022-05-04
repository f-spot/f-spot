//  GrabHandle.cs
//
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
//
//  Copyright (c) 2013 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;

namespace FSpot.Query
{
	public class GrabHandle : DrawingArea
	{
		public GrabHandle (int w, int h)
		{
			SetSizeRequest (w, h);
			Orientation = Gtk.Orientation.Horizontal;
			Show ();
		}

		public Orientation Orientation { get; set; }

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			bool ret = base.OnExposeEvent (evnt);

			if (evnt.Window != GdkWindow) {
				return ret;
			}

			Gtk.Style.PaintHandle (Style, GdkWindow, State, ShadowType.In,
						  evnt.Area, this, "entry", 0, 0, Allocation.Width, Allocation.Height, Orientation);

			//(Style, GdkWindow, StateType.Normal, ShadowType.In,
			//evnt.Area, this, "entry", 0, y_mid - y_offset, Allocation.Width,
			//Height + (y_offset * 2));

			return ret;
		}
	}
}
