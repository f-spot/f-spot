//
// Theme.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Gui.Theming
{
	public class ThemeContext
	{
		public bool ToplevelBorderCollapse { get; set; }

		double radius = 0.0;
		public double Radius {
			get { return radius; }
			set { radius = value; }
		}

		double fill_alpha = 1.0;
		public double FillAlpha {
			get { return fill_alpha; }
			set { fill_alpha = Theme.Clamp (0.0, 1.0, value); }
		}

		double line_width = 1.0;
		public double LineWidth {
			get { return line_width; }
			set { line_width = value; }
		}

		bool show_stroke = true;
		public bool ShowStroke {
			get { return show_stroke; }
			set { show_stroke = value; }
		}

		double x;
		public double X {
			get { return x; }
			set { x = value; }
		}

		double y;
		public double Y {
			get { return y; }
			set { y = value; }
		}

		Cairo.Context cairo;
		public Cairo.Context Cairo {
			get { return cairo; }
			set { cairo = value; }
		}
	}
}
