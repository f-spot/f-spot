//
// Panel.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Linq;

namespace Hyena.Gui.Canvas
{
	public class Panel : CanvasItem
	{
		CanvasItemCollection children;

		public Panel ()
		{
			children = new CanvasItemCollection (this);
		}

		public override Size Measure (Size available)
		{
			var result = new Size (0, 0);

			foreach (var child in Children) {
				if (child.Visible) {
					Size size = child.Measure (available);
					result.Width = Math.Max (result.Width, size.Width);
					result.Height = Math.Max (result.Height, size.Height);
				}
			}

			if (!double.IsNaN (Width)) {
				result.Width = Width;
			}

			if (!double.IsNaN (Height)) {
				result.Height = Height;
			}

			if (!available.IsEmpty) {
				result.Width = Math.Min (result.Width, available.Width);
				result.Height = Math.Min (result.Height, available.Height);
			}

			return DesiredSize = result;
		}

		public override void Arrange ()
		{
			foreach (var child in Children) {
				if (!child.Visible) {
					continue;
				}

				child.Allocation = new Rect (0, 0,
					Math.Min (ContentAllocation.Width, child.DesiredSize.Width),
					Math.Min (ContentAllocation.Height, child.DesiredSize.Height));

				child.Arrange ();
			}
		}

		protected override void ClippedRender (Hyena.Data.Gui.CellContext context)
		{
			foreach (var child in Children) {
				if (child.Visible) {
					child.Render (context);
				}
			}
		}

		public override void Bind (object o)
		{
			foreach (var child in Children) {
				child.Bind (o);
			}
		}

		protected CanvasItem FindChildAt (Point pt, bool grabHasPriority)
		{
			return FindChildAt (pt.X, pt.Y, grabHasPriority);
		}

		protected CanvasItem FindChildAt (double x, double y, bool grabHasPriority)
		{
			if (grabHasPriority) {
				var child = Children.FirstOrDefault (c => c.IsPointerGrabbed);
				if (child != null)
					return child;
			}

			foreach (var child in Children) {
				if (child.IsPointerGrabbed || (child.Visible && child.Allocation.Contains (x, y))) {
					return child;
				}
			}

			return null;
		}

		public override bool GetTooltipMarkupAt (Point pt, out string markup, out Rect area)
		{
			if (base.GetTooltipMarkupAt (pt, out markup, out area)) {
				return true;
			}

			pt = ChildCoord (this, pt);
			CanvasItem child = FindChildAt (pt, false);
			return child == null ? false : child.GetTooltipMarkupAt (ChildCoord (child, pt), out markup, out area);
		}

		public override bool ButtonEvent (Point cursor, bool pressed, uint button)
		{
			var child = FindChildAt (cursor, true);
			return child == null ? false : child.ButtonEvent (ChildCoord (child, cursor), pressed, button);
		}

		public override bool CursorMotionEvent (Point cursor)
		{
			var child = FindChildAt (cursor, true);
			return child == null ? false : child.CursorMotionEvent (ChildCoord (child, cursor));
		}

		Point ChildCoord (CanvasItem item, Point pt)
		{
			return new Point (pt.X - item.Allocation.X, pt.Y - item.Allocation.Y);
		}

		public override bool IsPointerGrabbed {
			get { return base.IsPointerGrabbed || FindChildAt (-1, -1, true) != null; }
		}

		public CanvasItemCollection Children {
			get { return children; }
		}
	}
}
