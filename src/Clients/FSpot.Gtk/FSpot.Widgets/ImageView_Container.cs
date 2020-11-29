//
// ImageView_Container.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Gtk;

namespace FSpot.Widgets
{
	public partial class ImageView : Container
	{
		readonly List<LayoutChild> children = new List<LayoutChild> ();

		protected override void OnAdded (Widget widget)
		{
			Put (widget, 0, 0);
		}

		protected override void OnRemoved (Widget widget)
		{
			if (widget == null)
				throw new ArgumentNullException (nameof (widget));

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
			if (callback == null)
				throw new ArgumentNullException (nameof (callback));

			foreach (var child in children)
				callback (child.Widget);
		}

		class LayoutChild
		{
			public Widget Widget { get; }

			public int X { get; set; }
			public int Y { get; set; }

			public LayoutChild (Widget widget, int x, int y)
			{
				Widget = widget;
				X = x;
				Y = y;
			}
		}

		LayoutChild GetChild (Widget widget)
		{
			foreach (var child in children) {
				if (child.Widget == widget)
					return child;
			}

			return null;
		}

		public void Put (Widget widget, int x, int y)
		{
			if (widget == null)
				throw new ArgumentNullException (nameof (widget));

			children.Add (new LayoutChild (widget, x, y));
			if (IsRealized)
				widget.ParentWindow = GdkWindow;

			widget.Parent = this;
		}

		public void Move (Widget widget, int x, int y)
		{
			if (widget == null)
				throw new ArgumentNullException (nameof (widget));

			LayoutChild child = GetChild (widget);
			if (child == null)
				return;

			child.X = x;
			child.Y = y;
			if (Visible && widget.Visible)
				QueueResize ();
		}

		void OnRealizedChildren ()
		{
			foreach (var child in children)
				child.Widget.ParentWindow = GdkWindow;
		}

		void OnMappedChildren ()
		{
			foreach (var child in children) {
				if (child.Widget.Visible && !child.Widget.IsMapped)
					child.Widget.Map ();
			}
		}

		void OnSizeRequestedChildren ()
		{
			foreach (var child in children)
				child.Widget.SizeRequest ();
		}

		void OnSizeAllocatedChildren ()
		{
			foreach (var child in children) {
				var req = child.Widget.ChildRequisition;
				child.Widget.SizeAllocate (new Gdk.Rectangle (child.X, child.Y, req.Width, req.Height));
			}
		}
	}
}
