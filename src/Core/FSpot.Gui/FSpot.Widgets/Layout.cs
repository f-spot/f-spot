//
// Layout.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
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
using System.Collections.Generic;

using Gtk;

using Hyena;

namespace FSpot.Widgets
{
	public class Layout : Container
	{
		public Layout () : this (null, null)
		{
		}

		public Layout (Adjustment hadjustment, Adjustment vadjustment)
		{
			OnSetScrollAdjustments (hadjustment, vadjustment);
			children = new List<LayoutChild> ();
		}

		Gdk.Window bin_window = null;
		public Gdk.Window BinWindow {
			get { return bin_window; }
		}

		Adjustment hadjustment;
		public Adjustment Hadjustment {
			get { return hadjustment; }
			set { OnSetScrollAdjustments (hadjustment, Vadjustment); }
		}

		Adjustment vadjustment;
		public Adjustment Vadjustment {
			get { return vadjustment; }
			set { OnSetScrollAdjustments (Hadjustment, vadjustment); }
		}

		uint width = 100;
		public uint Width {
			get { return width; }
		}

		uint height = 100;
		public uint Height {
			get { return height; }
		}

		class LayoutChild {
			public Widget Widget { get; private set; }

			public int X { get; set; }
			public int Y { get; set; }

			public LayoutChild (Widget widget, int x, int y)
			{
				Widget = widget;
				X = x;
				Y = y;
			}
		}

		List<LayoutChild> children;
		public void Put (Widget widget, int x, int y)
		{
			children.Add (new LayoutChild (widget, x, y));
			if (IsRealized)
				widget.ParentWindow = bin_window;
			widget.Parent = this;
		}

		public void Move (Widget widget, int x, int y)
		{
			LayoutChild child = GetChild (widget);
			if (child == null)
				return;

			child.X = x;
			child.Y = y;
			if (Visible && widget.Visible)
				QueueResize ();
		}

		public void SetSize (uint width, uint height)
		{
			Hadjustment.Upper = this.width = width;
			Vadjustment.Upper = this.height = height;
			
			if (IsRealized) {
				bin_window.Resize ((int)Math.Max (width, Allocation.Width), (int)Math.Max (height, Allocation.Height));
			}
		}

		LayoutChild GetChild (Widget widget)
		{
			foreach (var child in children)
				if (child.Widget == widget)
					return child;
			return null;
		}
		
#region widgetry
		protected override void OnRealized ()
		{
			IsRealized = true;

			var attributes = new Gdk.WindowAttr {
							     WindowType = Gdk.WindowType.Child,
							     X = Allocation.X,
							     Y = Allocation.Y,
							     Width = Allocation.Width,
							     Height = Allocation.Height,
								 Wclass = Gdk.WindowWindowClass.InputOnly,
							     Visual = this.Visual,
							     Mask = Gdk.EventMask.VisibilityNotifyMask };
			GdkWindow = new Gdk.Window (ParentWindow, attributes, 
						    Gdk.WindowAttributesType.X | Gdk.WindowAttributesType.Y | Gdk.WindowAttributesType.Visual);

			// GTK3
//			GdkWindow.SetBackPixmap (null, false);
			GdkWindow.UserData = Handle;

			attributes = new Gdk.WindowAttr {
							     WindowType = Gdk.WindowType.Child, 
							     X = (int)-Hadjustment.Value,
							     Y = (int)-Vadjustment.Value,
							     Width = (int)Math.Max (width, Allocation.Width),
							     Height = (int)Math.Max (height, Allocation.Height),
								 Wclass = Gdk.WindowWindowClass.InputOnly,
							     Visual = this.Visual,
							     Mask = Gdk.EventMask.ExposureMask | Gdk.EventMask.ScrollMask | this.Events };
			bin_window = new Gdk.Window (GdkWindow, attributes, 
						     Gdk.WindowAttributesType.X | Gdk.WindowAttributesType.Y | Gdk.WindowAttributesType.Visual);
			bin_window.UserData = Handle;

			Style.Attach (GdkWindow);
			Style.SetBackground (bin_window, StateType.Normal);

			foreach (var child in children) {
				child.Widget.ParentWindow = bin_window;
			}

		}

		protected override void OnUnrealized ()
		{
			bin_window.Destroy ();
			bin_window = null;

			base.OnUnrealized ();
		}

		protected override void OnStyleSet (Style old_style)
		{
			base.OnStyleSet (old_style);
			if (IsRealized)
				Style.SetBackground (bin_window, StateType.Normal);
		}

		protected override void OnMapped ()
		{
			IsMapped = true;

			foreach (var child in children) {
				if (child.Widget.Visible && !child.Widget.IsMapped)
					child.Widget.Map ();
			}
			bin_window.Show ();
			GdkWindow.Show ();
		}

		// GTK3: https://developer.gnome.org/gtk3/stable/ch24s02.html#id-1.6.3.4.3
//		protected override void OnSizeRequested (ref Requisition requisition)
//		{
//			requisition.Width = requisition.Height = 0;
//
//			foreach (var child in children) {
//				child.Widget.SizeRequest ();
//			}
//		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			foreach (var child in children) {
				Requisition req = child.Widget.ChildRequisition;
				child.Widget.SizeAllocate (new Gdk.Rectangle (child.X, child.Y, req.Width, req.Height));
			}

			if (IsRealized) {
				GdkWindow.MoveResize (allocation.X, allocation.Y, allocation.Width, allocation.Height);
				bin_window.Resize ((int)Math.Max (width, allocation.Width), (int)Math.Max (height, allocation.Height));
			}

			Hadjustment.PageSize = allocation.Width;
			Hadjustment.PageIncrement = Width * .9;
			Hadjustment.Lower = 0;
			Hadjustment.Upper = Math.Max (width, allocation.Width);

			Vadjustment.PageSize = allocation.Height;
			Vadjustment.PageIncrement = Height * .9;
			Vadjustment.Lower = 0;
			Vadjustment.Upper = Math.Max (height, allocation.Height);
			base.OnSizeAllocated (allocation);
		}

		// GTK3: Not in the base class?
//		protected override bool OnDrawn (Cairo.Context cr)
//		{
//			// GTK3
////			if (evnt.Window != bin_window)
////				return false;
//			return base.OnDrawn (cr);
//		}

		// GTK3: https://developer.gnome.org/gtk3/stable/ch24s02.html#id-1.6.3.4.3
//		protected override void OnSetScrollAdjustments (Gtk.Adjustment hadjustment, Gtk.Adjustment vadjustment)
//		{
//			Log.Debug ("\n\nLayout.OnSetScrollAdjustments");
//			if (hadjustment == null)
//				hadjustment = new Adjustment (0, 0, 0, 0, 0, 0);
//			if (vadjustment == null)
//				vadjustment = new Adjustment (0, 0, 0, 0, 0, 0);
//			bool need_change = false;
//			if (Hadjustment != hadjustment) {
//				this.hadjustment = hadjustment;
//				this.hadjustment.Upper = Width;
//				this.hadjustment.ValueChanged += HandleAdjustmentsValueChanged;
//				need_change = true;
//			}
//			if (Vadjustment != vadjustment) {
//				this.vadjustment = vadjustment;
//				this.vadjustment.Upper = Width;
//				this.vadjustment.ValueChanged += HandleAdjustmentsValueChanged;
//				need_change = true;
//			}
//
//			if (need_change)
//				HandleAdjustmentsValueChanged (this, EventArgs.Empty);
//		}

		void HandleAdjustmentsValueChanged (object sender, EventArgs e)
		{
			if (IsRealized)
				bin_window.Move (-(int)Hadjustment.Value, -(int)Vadjustment.Value);
		}
#endregion widgetry

#region container stuffs
		protected override void OnAdded (Widget widget)
		{
			Put (widget, 0, 0);
		}

		protected override void OnRemoved (Widget widget)
		{
			LayoutChild child = null;
			foreach (var c in children) {
				if (child.Widget == widget) {
					child = c;
					break;
				}
			}

			if (child != null) {
				widget.Unparent ();
				children.Remove (child);
			}
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			foreach (var child in children) {
				callback (child.Widget);
			}
		}
#endregion
	}
}
