//
// Layout.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Hyena;

namespace FSpot.Widgets
{
	public class Layout : Gtk.Container
	{
		public Layout () : this (null, null)
		{
		}

		public Layout (Gtk.Adjustment hadjustment, Gtk.Adjustment vadjustment) : base ()
		{
			OnSetScrollAdjustments (hadjustment, vadjustment);
			children = new List<LayoutChild> ();
		}

		public Gdk.Window BinWindow { get; private set; }

		Gtk.Adjustment hadjustment;
		public Gtk.Adjustment Hadjustment {
			get { return hadjustment; }
			set { OnSetScrollAdjustments (hadjustment, Vadjustment); }
		}

		Gtk.Adjustment vadjustment;
		public Gtk.Adjustment Vadjustment {
			get { return vadjustment; }
			set { OnSetScrollAdjustments (Hadjustment, vadjustment); }
		}

		public uint Width { get; private set; } = 100;
		public uint Height { get; private set; } = 100;

		class LayoutChild
		{
			public Gtk.Widget Widget { get; private set; }

			public int X { get; set; }
			public int Y { get; set; }

			public LayoutChild (Gtk.Widget widget, int x, int y)
			{
				Widget = widget;
				X = x;
				Y = y;
			}
		}

		readonly List<LayoutChild> children;
		public void Put (Gtk.Widget widget, int x, int y)
		{
			children.Add (new LayoutChild (widget, x, y));
			if (IsRealized)
				widget.ParentWindow = BinWindow;
			widget.Parent = this;
		}

		public void Move (Gtk.Widget widget, int x, int y)
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
			Hadjustment.Upper = Width = width;
			Vadjustment.Upper = Height = height;

			if (IsRealized) {
				BinWindow.Resize ((int)Math.Max (width, Allocation.Width), (int)Math.Max (height, Allocation.Height));
			}
		}

		LayoutChild GetChild (Gtk.Widget widget)
		{
			foreach (var child in children)
				if (child.Widget == widget)
					return child;
			return null;
		}

		#region widgetry
		protected override void OnRealized ()
		{
			SetFlag (Gtk.WidgetFlags.Realized);

			var attributes = new Gdk.WindowAttr {
				WindowType = Gdk.WindowType.Child,
				X = Allocation.X,
				Y = Allocation.Y,
				Width = Allocation.Width,
				Height = Allocation.Height,
				Wclass = Gdk.WindowClass.InputOutput,
				Visual = Visual,
				Colormap = Colormap,
				Mask = Gdk.EventMask.VisibilityNotifyMask
			};
			GdkWindow = new Gdk.Window (ParentWindow, attributes,
							Gdk.WindowAttributesType.X | Gdk.WindowAttributesType.Y | Gdk.WindowAttributesType.Visual | Gdk.WindowAttributesType.Colormap);

			GdkWindow.SetBackPixmap (null, false);
			GdkWindow.UserData = Handle;

			attributes = new Gdk.WindowAttr {
				WindowType = Gdk.WindowType.Child,
				X = (int)-Hadjustment.Value,
				Y = (int)-Vadjustment.Value,
				Width = (int)Math.Max (Width, Allocation.Width),
				Height = (int)Math.Max (Height, Allocation.Height),
				Wclass = Gdk.WindowClass.InputOutput,
				Visual = Visual,
				Colormap = Colormap,
				Mask = Gdk.EventMask.ExposureMask | Gdk.EventMask.ScrollMask | Events
			};
			BinWindow = new Gdk.Window (GdkWindow, attributes,
							 Gdk.WindowAttributesType.X | Gdk.WindowAttributesType.Y | Gdk.WindowAttributesType.Visual | Gdk.WindowAttributesType.Colormap) {
				UserData = Handle
			};

			Style.Attach (GdkWindow);
			Style.SetBackground (BinWindow, Gtk.StateType.Normal);

			foreach (var child in children) {
				child.Widget.ParentWindow = BinWindow;
			}

		}

		protected override void OnUnrealized ()
		{
			BinWindow.Destroy ();
			BinWindow = null;

			base.OnUnrealized ();
		}

		protected override void OnStyleSet (Gtk.Style old_style)
		{
			base.OnStyleSet (old_style);
			if (IsRealized)
				Style.SetBackground (BinWindow, Gtk.StateType.Normal);
		}

		protected override void OnMapped ()
		{
			SetFlag (Gtk.WidgetFlags.Mapped);

			foreach (var child in children) {
				if (child.Widget.Visible && !child.Widget.IsMapped)
					child.Widget.Map ();
			}
			BinWindow.Show ();
			GdkWindow.Show ();
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			requisition.Width = requisition.Height = 0;

			foreach (var child in children) {
				child.Widget.SizeRequest ();
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			foreach (var child in children) {
				Gtk.Requisition req = child.Widget.ChildRequisition;
				child.Widget.SizeAllocate (new Gdk.Rectangle (child.X, child.Y, req.Width, req.Height));
			}

			if (IsRealized) {
				GdkWindow.MoveResize (allocation.X, allocation.Y, allocation.Width, allocation.Height);
				BinWindow.Resize ((int)Math.Max (Width, allocation.Width), (int)Math.Max (Height, allocation.Height));
			}

			Hadjustment.PageSize = allocation.Width;
			Hadjustment.PageIncrement = Width * .9;
			Hadjustment.Lower = 0;
			Hadjustment.Upper = Math.Max (Width, allocation.Width);

			Vadjustment.PageSize = allocation.Height;
			Vadjustment.PageIncrement = Height * .9;
			Vadjustment.Lower = 0;
			Vadjustment.Upper = Math.Max (Height, allocation.Height);
			base.OnSizeAllocated (allocation);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (evnt.Window != BinWindow)
				return false;

			return base.OnExposeEvent (evnt);
		}

		protected override void OnSetScrollAdjustments (Gtk.Adjustment hadjustment, Gtk.Adjustment vadjustment)
		{
			Log.Debug ("\n\nLayout.OnSetScrollAdjustments");
			if (hadjustment == null)
				hadjustment = new Gtk.Adjustment (0, 0, 0, 0, 0, 0);
			if (vadjustment == null)
				vadjustment = new Gtk.Adjustment (0, 0, 0, 0, 0, 0);
			bool need_change = false;
			if (Hadjustment != hadjustment) {
				this.hadjustment = hadjustment;
				this.hadjustment.Upper = Width;
				this.hadjustment.ValueChanged += HandleAdjustmentsValueChanged;
				need_change = true;
			}
			if (Vadjustment != vadjustment) {
				this.vadjustment = vadjustment;
				this.vadjustment.Upper = Width;
				this.vadjustment.ValueChanged += HandleAdjustmentsValueChanged;
				need_change = true;
			}

			if (need_change)
				HandleAdjustmentsValueChanged (this, EventArgs.Empty);
		}

		void HandleAdjustmentsValueChanged (object sender, EventArgs e)
		{
			if (IsRealized)
				BinWindow.Move (-(int)Hadjustment.Value, -(int)Vadjustment.Value);
		}
		#endregion widgetry

		#region container stuffs
		protected override void OnAdded (Gtk.Widget widget)
		{
			Put (widget, 0, 0);
		}

		protected override void OnRemoved (Gtk.Widget widget)
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

		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			foreach (var child in children) {
				callback (child.Widget);
			}
		}
		#endregion
	}
}
