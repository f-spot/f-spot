//
// GrabHandle.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace Hyena.Widgets
{
	public class GrabHandle : EventBox
	{
		Gtk.DrawingArea da;

		public GrabHandle () : this (5, 28) { }

		public GrabHandle (int w, int h)
		{
			da = new DrawingArea ();
			da.SetSizeRequest (w, h);
			Orientation = Gtk.Orientation.Vertical;

			Child = da;
			ShowAll ();

			ButtonPressEvent += (o, a) => Dragging = true;
			ButtonReleaseEvent += (o, a) => Dragging = false;
			EnterNotifyEvent += (o, a) => Inside = true;
			LeaveNotifyEvent += (o, a) => Inside = false;

			da.ExposeEvent += (o, a) => {
				if (da.IsDrawable) {
					Gtk.Style.PaintHandle (da.Style, da.GdkWindow, da.State, ShadowType.In,
						a.Event.Area, this, "entry", 0, 0, da.Allocation.Width, da.Allocation.Height, Orientation);
				}
			};
		}

		public void ControlWidthOf (Widget widget, int min, int max, bool grabberOnRight)
		{
			MotionNotifyEvent += (o, a) => {
				var x = a.Event.X;
				var w = Math.Min (max, Math.Max (min, widget.WidthRequest + (grabberOnRight ? 1 : -1) * x));
				widget.WidthRequest = (int)w;
			};
		}

		public Gtk.Orientation Orientation { get; set; }

		bool inside, dragging;
		bool Inside {
			set {
				inside = value;
				GdkWindow.Cursor = dragging || inside ? resize_cursor : null;
			}
		}

		bool Dragging {
			set {
				dragging = value;
				GdkWindow.Cursor = dragging || inside ? resize_cursor : null;
			}
		}

		static Gdk.Cursor resize_cursor = new Gdk.Cursor (Gdk.CursorType.SbHDoubleArrow);
	}
}
