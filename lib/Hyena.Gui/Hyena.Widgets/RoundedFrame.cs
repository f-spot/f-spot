//
// RoundedFrame.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Cairo;

using Gtk;

using Hyena.Gui;
using Hyena.Gui.Theming;

namespace Hyena.Widgets
{
	public class RoundedFrame : Bin
	{
		Theme theme;
		protected Theme Theme {
			get {
				if (theme == null) {
					InitTheme ();
				}
				return theme;
			}
		}

		void InitTheme ()
		{
			theme = Hyena.Gui.Theming.ThemeEngine.CreateTheme (this);
			frame_width = (int)theme.Context.Radius + 1;
		}

		Widget child;
		Gdk.Rectangle child_allocation;
		bool fill_color_set;
		Cairo.Color fill_color;
		bool draw_border = true;
		Pattern fill_pattern;
		int frame_width;

		// Ugh, this is to avoid the GLib.MissingIntPtrCtorException seen by some; BGO #552169
		protected RoundedFrame (IntPtr ptr) : base (ptr)
		{
		}

		public RoundedFrame ()
		{
		}

		public void SetFillColor (Cairo.Color color)
		{
			fill_color = color;
			fill_color_set = true;
			QueueDraw ();
		}

		public void UnsetFillColor ()
		{
			fill_color_set = false;
			QueueDraw ();
		}

		public Pattern FillPattern {
			get { return fill_pattern; }
			set {
				fill_pattern = value;
				QueueDraw ();
			}
		}

		public bool DrawBorder {
			get { return draw_border; }
			set { draw_border = value; QueueDraw (); }
		}

		#region Gtk.Widget Overrides

		protected override void OnStyleSet (Style previous_style)
		{
			base.OnStyleSet (previous_style);
			InitTheme ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			if (child != null && child.Visible) {
				// Add the child's width/height
				Requisition child_requisition = child.SizeRequest ();
				requisition.Width = Math.Max (0, child_requisition.Width);
				requisition.Height = child_requisition.Height;
			} else {
				requisition.Width = 0;
				requisition.Height = 0;
			}

			// Add the frame border
			requisition.Width += ((int)BorderWidth + frame_width) * 2;
			requisition.Height += ((int)BorderWidth + frame_width) * 2;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);

			child_allocation = new Gdk.Rectangle ();

			if (child == null || !child.Visible) {
				return;
			}

			child_allocation.X = (int)BorderWidth + frame_width;
			child_allocation.Y = (int)BorderWidth + frame_width;
			child_allocation.Width = (int)Math.Max (1, Allocation.Width - child_allocation.X * 2);
			child_allocation.Height = (int)Math.Max (1, Allocation.Height - child_allocation.Y -
				(int)BorderWidth - frame_width);

			child_allocation.X += Allocation.X;
			child_allocation.Y += Allocation.Y;

			child.SizeAllocate (child_allocation);
		}

		protected override void OnSetScrollAdjustments (Adjustment hadj, Adjustment vadj)
		{
			// This is to satisfy the gtk_widget_set_scroll_adjustments
			// inside of GtkScrolledWindow so it doesn't complain about
			// its child not being scrollable.
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (!IsDrawable) {
				return false;
			}

			Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);

			try {
				DrawFrame (cr, evnt.Area);
				if (child != null) {
					PropagateExpose (child, evnt);
				}
				return false;
			} finally {
				CairoExtensions.DisposeContext (cr);
			}
		}

		void DrawFrame (Cairo.Context cr, Gdk.Rectangle clip)
		{
			int x = child_allocation.X - frame_width;
			int y = child_allocation.Y - frame_width;
			int width = child_allocation.Width + 2 * frame_width;
			int height = child_allocation.Height + 2 * frame_width;

			var rect = new Gdk.Rectangle (x, y, width, height);

			Theme.Context.ShowStroke = draw_border;

			if (fill_color_set) {
				Theme.DrawFrameBackground (cr, rect, fill_color);
			} else if (fill_pattern != null) {
				Theme.DrawFrameBackground (cr, rect, fill_pattern);
			} else {
				Theme.DrawFrameBackground (cr, rect, true);
				Theme.DrawFrameBorder (cr, rect);
			}
		}

		#endregion

		#region Gtk.Container Overrides

		protected override void OnAdded (Widget widget)
		{
			child = widget;
			base.OnAdded (widget);
		}

		protected override void OnRemoved (Widget widget)
		{
			if (child == widget) {
				child = null;
			}

			base.OnRemoved (widget);
		}

		#endregion

	}
}
