//
// ColumnCellCheckBox.cs
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
	public class ColumnCellCheckBox : ColumnCell, IInteractiveCell, ISizeRequestCell
	{
		public event EventHandler Toggled;

		public ColumnCellCheckBox (string property, bool expand) : base (property, expand)
		{
		}

		public override Size Measure (Size available)
		{
			Width = Size + 2 * Xpad;
			Height = Size + 2 * Ypad;
			return DesiredSize = new Size (Width + Margin.X, Height + Margin.Y);
		}

		public override void Render (CellContext context, double cellWidth, double cellHeight)
		{
			int cell_width = (int)cellWidth - 2 * Xpad;
			int cell_height = (int)cellHeight - 2 * Ypad;
			int x = context.Area.X + xpad + ((cell_width - Size) / 2);
			int y = context.Area.Y + ypad + ((cell_height - Size) / 2);

			if (context.State == StateType.Normal && last_hover_bound == BoundObjectParent) {
				context.State = StateType.Prelight;
			}

			Style.PaintCheck (context.Widget.Style, context.Drawable, context.State,
				Value ? ShadowType.In : ShadowType.Out,
				context.Clip, context.Widget, "cellcheck", x, y, Size, Size);
		}

		object last_pressed_bound;
		object last_hover_bound;

		public override bool ButtonEvent (Point press, bool pressed, uint button)
		{
			if (pressed) {
				last_pressed_bound = BoundObjectParent;
				return false;
			}

			if (last_pressed_bound != null && last_pressed_bound.Equals (BoundObjectParent)) {
				Value = !Value;
				last_pressed_bound = null;

				Toggled?.Invoke (BoundObjectParent, EventArgs.Empty);

				InvalidateRender ();
			}

			return true;
		}

		public override bool CursorMotionEvent (Point motion)
		{
			if (last_hover_bound == BoundObjectParent) {
				return false;
			}

			last_hover_bound = BoundObjectParent;
			InvalidateRender ();
			return true;
		}

		public override bool CursorLeaveEvent ()
		{
			base.CursorLeaveEvent ();
			last_hover_bound = null;
			InvalidateRender ();
			return true;
		}

		public void GetWidthRange (Pango.Layout layout, out int min, out int max)
		{
			min = max = 2 * Xpad + Size;
		}

		bool restrict_size = true;
		public bool RestrictSize {
			get { return restrict_size; }
			set { restrict_size = value; }
		}

		bool Value {
			get { return (bool)BoundObject; }
			set { BoundObject = value; }
		}

		int size = 13;
		public int Size {
			get { return size; }
			set { size = value; }
		}

		int xpad = 2;
		public int Xpad {
			get { return xpad; }
			set { xpad = value; }
		}

		public int ypad = 2;
		public int Ypad {
			get { return ypad; }
			set { ypad = value; }
		}
	}
}
