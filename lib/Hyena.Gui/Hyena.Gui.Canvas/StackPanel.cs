//
// StackPanel.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

namespace Hyena.Gui.Canvas
{
	public class StackPanel : Panel
	{
		Orientation orientation;
		public double spacing;

		public StackPanel ()
		{
		}

		public override Size Measure (Size available)
		{
			var result = new Size (0, 0);

			int visible_children = 0;
			foreach (var child in Children) {
				if (!child.Visible) {
					continue;
				}

				Size size = child.Measure (available);

				if (Orientation == Orientation.Vertical) {
					result.Height += size.Height;
					result.Width = Math.Max (result.Width, size.Width);
				} else {
					result.Width += size.Width;
					result.Height = Math.Max (result.Height, size.Height);
				}

				visible_children++;
			}

			if (!double.IsNaN (Width)) {
				result.Width = Width;
			}

			if (!double.IsNaN (Height)) {
				result.Height = Height;
			}

			result.Width += Margin.X;
			result.Height += Margin.Y;

			if (!available.IsEmpty) {
				result.Width = Math.Min (result.Width, available.Width);
				result.Height = Math.Min (result.Height, available.Height);
			}

			if (Orientation == Orientation.Vertical) {
				result.Height += Spacing * (visible_children - 1);
			} else {
				result.Width += Spacing * (visible_children - 1);
			}

			return DesiredSize = result;
		}

		public override void Arrange ()
		{
			int visible_child_count = 0;
			int flex_count = 0;
			double offset = 0;
			double static_space = 0;
			double flex_space = 0;

			foreach (var child in Children) {
				if (!child.Visible) {
					continue;
				}

				child.Measure (ContentSize);

				visible_child_count++;

				if (Orientation == Orientation.Vertical) {
					static_space += double.IsNaN (child.Height) ? 0 : child.DesiredSize.Height;
					flex_count += double.IsNaN (child.Height) ? 1 : 0;
				} else {
					static_space += double.IsNaN (child.Width) ? 0 : child.DesiredSize.Width;
					flex_count += double.IsNaN (child.Width) ? 1 : 0;
				}
			}

			flex_space = (Orientation == Orientation.Vertical ? ContentAllocation.Height : ContentAllocation.Width) -
				static_space - (visible_child_count - 1) * Spacing;
			if (flex_space < 0) {
				flex_space = 0;
			}

			foreach (var child in Children) {
				if (!child.Visible) {
					continue;
				}

				double variable_size = 0;

				if ((Orientation == Orientation.Vertical && double.IsNaN (child.Height)) ||
					(Orientation == Orientation.Horizontal && double.IsNaN (child.Width))) {
					variable_size = flex_space / flex_count--;
					flex_space -= variable_size;
					if (flex_count == 0) {
						variable_size += flex_space;
					}
				} else if (Orientation == Orientation.Vertical) {
					variable_size = child.DesiredSize.Height;
				} else {
					variable_size = child.DesiredSize.Width;
				}

				child.Allocation = Orientation == Orientation.Vertical
					? new Rect (0, offset, ContentAllocation.Width, variable_size)
					: new Rect (offset, 0, variable_size, ContentAllocation.Height);
				child.Arrange ();

				offset += variable_size + Spacing;
			}
		}

		public Orientation Orientation {
			get { return orientation; }
			set { orientation = value; }
		}

		public double Spacing {
			get { return spacing; }
			set { spacing = value; }
		}
	}
}
