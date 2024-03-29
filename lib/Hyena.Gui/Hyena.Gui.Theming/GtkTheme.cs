//
// GtkTheme.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Cairo;

using Gtk;

namespace Hyena.Gui.Theming
{
	public class GtkTheme : Theme
	{
		Cairo.Color rule_color;
		Cairo.Color border_color;

		public GtkTheme (Widget widget) : base (widget)
		{
		}

		public static Cairo.Color GetCairoTextMidColor (Widget widget)
		{
			Cairo.Color text_color = CairoExtensions.GdkColorToCairoColor (widget.Style.Foreground (StateType.Normal));
			Cairo.Color background_color = CairoExtensions.GdkColorToCairoColor (widget.Style.Background (StateType.Normal));
			return CairoExtensions.AlphaBlend (text_color, background_color, 0.5);
		}

		public static Gdk.Color GetGdkTextMidColor (Widget widget)
		{
			Cairo.Color color = GetCairoTextMidColor (widget);
			var gdk_color = new Gdk.Color ((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255));
			Gdk.Colormap.System.AllocColor (ref gdk_color, true, true);
			return gdk_color;
		}

		protected override void OnColorsRefreshed ()
		{
			base.OnColorsRefreshed ();

			rule_color = CairoExtensions.ColorShade (ViewFill, 0.95);

			// On Windows we use Normal b/c Active incorrectly returns black (at least on XP)
			border_color = Colors.GetWidgetColor (GtkColorClass.Dark,
			  Hyena.PlatformDetection.IsWindows ? StateType.Normal : StateType.Active
			);
		}

		public override void DrawPie (double fraction)
		{
			// Calculate the pie path
			fraction = Theme.Clamp (0.0, 1.0, fraction);
			double a1 = 3.0 * Math.PI / 2.0;
			double a2 = a1 + 2.0 * Math.PI * fraction;

			if (fraction == 0.0) {
				return;
			}

			Context.Cairo.MoveTo (Context.X, Context.Y);
			Context.Cairo.Arc (Context.X, Context.Y, Context.Radius, a1, a2);
			Context.Cairo.LineTo (Context.X, Context.Y);

			// Fill the pie
			Color color_a = Colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected);
			Color color_b = CairoExtensions.ColorShade (color_a, 1.4);

			var fill = new RadialGradient (Context.X, Context.Y, 0,
				Context.X, Context.Y, 2.0 * Context.Radius);
			fill.AddColorStop (0, color_a);
			fill.AddColorStop (1, color_b);
			Context.Cairo.SetSource (fill);

			Context.Cairo.FillPreserve ();
			fill.Dispose ();

			// Stroke the pie
			Context.Cairo.SetSourceColor (CairoExtensions.ColorShade (color_a, 0.8));
			Context.Cairo.LineWidth = Context.LineWidth;
			Context.Cairo.Stroke ();
		}

		public override void DrawArrow (Context cr, Gdk.Rectangle alloc, double rotation)
		{
			rotation -= Math.PI / 2.0;

			double x1 = alloc.X;
			double x2 = alloc.Right;
			double x3 = alloc.X + alloc.Width / 2.0;
			double y1 = alloc.Y;
			double y2 = alloc.Bottom;

			double cx = x3;
			double cy = alloc.Y + alloc.Height / 2.0;

			if (rotation != 0) {
				// Rotate about the center of the arrow
				cr.Translate (cx, cy);
				cr.Rotate (rotation);
				cr.Translate (-cx, -cy);
			}

			cr.LineWidth = 1.0;

			bool hz = (rotation % (Math.PI / 2.0)) == 0;
			double dx = hz ? 0 : 0.5;
			double dy = hz ? 0.5 : 0.5;
			cr.Translate (dx, dy);

			cr.MoveTo (x1, y1);
			cr.LineTo (x2, y1);
			cr.LineTo (x3, y2);
			cr.LineTo (x1, y1);

			cr.SetSourceColor (Colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal));
			cr.FillPreserve ();
			cr.SetSourceColor (Colors.GetWidgetColor (GtkColorClass.Text, StateType.Normal));
			cr.Stroke ();

			cr.Translate (-dx, -dy);

			if (rotation != 0) {
				cr.Translate (cx, cy);
				cr.Rotate (-rotation);
				cr.Translate (-cx, -cy);
			}
		}

		public override void DrawFrameBackground (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color, Cairo.Pattern pattern)
		{
			color.A = Context.FillAlpha;
			if (pattern != null) {
				cr.SetSource (pattern);
			} else {
				cr.SetSourceColor (color);
			}
			CairoExtensions.RoundedRectangle (cr, alloc.X, alloc.Y, alloc.Width, alloc.Height, Context.Radius, CairoCorners.All);
			cr.Fill ();
		}

		public override void DrawFrameBorder (Cairo.Context cr, Gdk.Rectangle alloc)
		{
			var corners = CairoCorners.All;
			double top_extend = 0;
			double bottom_extend = 0;
			double left_extend = 0;
			double right_extend = 0;

			if (Context.ToplevelBorderCollapse) {
				if (Widget.Allocation.Top <= Widget.Toplevel.Allocation.Top) {
					corners &= ~(CairoCorners.TopLeft | CairoCorners.TopRight);
					top_extend = cr.LineWidth;
				}

				if (Widget.Allocation.Bottom >= Widget.Toplevel.Allocation.Bottom) {
					corners &= ~(CairoCorners.BottomLeft | CairoCorners.BottomRight);
					bottom_extend = cr.LineWidth;
				}

				if (Widget.Allocation.Left <= Widget.Toplevel.Allocation.Left) {
					corners &= ~(CairoCorners.BottomLeft | CairoCorners.TopLeft);
					left_extend = cr.LineWidth;
				}

				if (Widget.Allocation.Right >= Widget.Toplevel.Allocation.Right) {
					corners &= ~(CairoCorners.BottomRight | CairoCorners.TopRight);
					right_extend = cr.LineWidth;
				}
			}

			// FIXME Windows; shading the color by .8 makes it blend into the bg
			if (Widget.HasFocus && !Hyena.PlatformDetection.IsWindows) {
				cr.LineWidth = BorderWidth * 1.5;
				cr.SetSourceColor (CairoExtensions.ColorShade (border_color, 0.8));
			} else {
				cr.LineWidth = BorderWidth;
				cr.SetSourceColor (border_color);
			}

			double offset = (double)cr.LineWidth / 2.0;

			CairoExtensions.RoundedRectangle (cr,
				alloc.X + offset - left_extend,
				alloc.Y + offset - top_extend,
				alloc.Width - cr.LineWidth + left_extend + right_extend,
				alloc.Height - cr.LineWidth - top_extend + bottom_extend,
				Context.Radius,
				corners);

			cr.Stroke ();
		}

		public override void DrawColumnHighlight (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color)
		{
			Cairo.Color light_color = CairoExtensions.ColorShade (color, 1.6);
			Cairo.Color dark_color = CairoExtensions.ColorShade (color, 1.3);

			var grad = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Bottom - 1);
			grad.AddColorStop (0, light_color);
			grad.AddColorStop (1, dark_color);

			cr.SetSource (grad);
			cr.Rectangle (alloc.X + 1.5, alloc.Y + 1.5, alloc.Width - 3, alloc.Height - 2);
			cr.Fill ();
			grad.Dispose ();
		}

		public override void DrawHeaderBackground (Cairo.Context cr, Gdk.Rectangle alloc)
		{
			Cairo.Color gtk_background_color = Colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal);
			Cairo.Color light_color = CairoExtensions.ColorShade (gtk_background_color, 1.1);
			Cairo.Color dark_color = CairoExtensions.ColorShade (gtk_background_color, 0.95);

			CairoCorners corners = CairoCorners.TopLeft | CairoCorners.TopRight;

			var grad = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Bottom);
			grad.AddColorStop (0, light_color);
			grad.AddColorStop (0.75, dark_color);
			grad.AddColorStop (0, light_color);

			cr.SetSource (grad);
			CairoExtensions.RoundedRectangle (cr, alloc.X, alloc.Y, alloc.Width, alloc.Height, Context.Radius, corners);
			cr.Fill ();

			cr.SetSourceColor (border_color);
			cr.Rectangle (alloc.X, alloc.Bottom, alloc.Width, BorderWidth);
			cr.Fill ();
			grad.Dispose ();
		}

		public override void DrawColumnHeaderFocus (Cairo.Context cr, Gdk.Rectangle alloc)
		{
			double top_offset = 2.0;
			double right_offset = 2.0;

			double margin = 0.5;
			double line_width = 0.7;

			Cairo.Color stroke_color = CairoExtensions.ColorShade (
				Colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected), 0.8);

			stroke_color.A = 0.1;
			cr.SetSourceColor (stroke_color);

			CairoExtensions.RoundedRectangle (cr,
				alloc.X + margin + line_width + right_offset,
				alloc.Y + margin + line_width + top_offset,
				alloc.Width - (margin + line_width) * 2.0 - right_offset,
				alloc.Height - (margin + line_width) * 2.0 - top_offset,
				Context.Radius / 2.0, CairoCorners.None);

			cr.Fill ();

			stroke_color.A = 1.0;
			cr.LineWidth = line_width;
			cr.SetSourceColor (stroke_color);
			CairoExtensions.RoundedRectangle (cr,
				alloc.X + margin + line_width + right_offset,
				alloc.Y + margin + line_width + top_offset,
				alloc.Width - (line_width + margin) * 2.0 - right_offset,
				alloc.Height - (line_width + margin) * 2.0 - right_offset,
				Context.Radius / 2.0, CairoCorners.All);
			cr.Stroke ();
		}

		public override void DrawHeaderSeparator (Cairo.Context cr, Gdk.Rectangle alloc, int x)
		{
			Cairo.Color gtk_background_color = Colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal);
			Cairo.Color dark_color = CairoExtensions.ColorShade (gtk_background_color, 0.80);
			Cairo.Color light_color = CairoExtensions.ColorShade (gtk_background_color, 1.1);

			int y_1 = alloc.Top + 4;
			int y_2 = alloc.Bottom - 3;

			cr.LineWidth = 1;
			cr.Antialias = Cairo.Antialias.None;

			cr.SetSourceColor (dark_color);
			cr.MoveTo (x, y_1);
			cr.LineTo (x, y_2);
			cr.Stroke ();

			cr.SetSourceColor (light_color);
			cr.MoveTo (x + 1, y_1);
			cr.LineTo (x + 1, y_2);
			cr.Stroke ();

			cr.Antialias = Cairo.Antialias.Default;
		}

		public override void DrawListBackground (Context cr, Gdk.Rectangle alloc, Color color)
		{
			color.A = Context.FillAlpha;
			cr.SetSourceColor (color);
			cr.Rectangle (alloc.X, alloc.Y, alloc.Width, alloc.Height);
			cr.Fill ();
		}

		public override void DrawRowCursor (Cairo.Context cr, int x, int y, int width, int height,
											Cairo.Color color, CairoCorners corners)
		{
			cr.LineWidth = 1.25;
			cr.SetSourceColor (color);
			CairoExtensions.RoundedRectangle (cr, x + cr.LineWidth / 2.0, y + cr.LineWidth / 2.0,
				width - cr.LineWidth, height - cr.LineWidth, Context.Radius, corners, true);
			cr.Stroke ();
		}

		public override void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height,
			bool filled, bool stroked, Cairo.Color color, CairoCorners corners)
		{
			DrawRowSelection (cr, x, y, width, height, filled, stroked, color, corners, false);
		}

		public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height,
			bool filled, bool stroked, Cairo.Color color, CairoCorners corners, bool flat_fill)
		{
			Cairo.Color selection_color = color;
			Cairo.Color selection_highlight = CairoExtensions.ColorShade (selection_color, 1.24);
			Cairo.Color selection_stroke = CairoExtensions.ColorShade (selection_color, 0.85);
			selection_highlight.A = 0.5;
			selection_stroke.A = color.A;
			LinearGradient grad = null;

			if (filled) {
				if (flat_fill) {
					cr.SetSourceColor (selection_color);
				} else {
					Cairo.Color selection_fill_light = CairoExtensions.ColorShade (selection_color, 1.12);
					Cairo.Color selection_fill_dark = selection_color;

					selection_fill_light.A = color.A;
					selection_fill_dark.A = color.A;

					grad = new LinearGradient (x, y, x, y + height);
					grad.AddColorStop (0, selection_fill_light);
					grad.AddColorStop (0.4, selection_fill_dark);
					grad.AddColorStop (1, selection_fill_light);

					cr.SetSource (grad);
				}

				CairoExtensions.RoundedRectangle (cr, x, y, width, height, Context.Radius, corners, true);
				cr.Fill ();

				if (grad != null) {
					grad.Dispose ();
				}
			}

			if (filled && stroked) {
				cr.LineWidth = 1.0;
				cr.SetSourceColor (selection_highlight);
				CairoExtensions.RoundedRectangle (cr, x + 1.5, y + 1.5, width - 3, height - 3,
					Context.Radius - 1, corners, true);
				cr.Stroke ();
			}

			if (stroked) {
				cr.LineWidth = 1.0;
				cr.SetSourceColor (selection_stroke);
				CairoExtensions.RoundedRectangle (cr, x + 0.5, y + 0.5, width - 1, height - 1,
					Context.Radius, corners, true);
				cr.Stroke ();
			}
		}

		public override void DrawRowRule (Cairo.Context cr, int x, int y, int width, int height)
		{
			cr.SetSourceColor (new Cairo.Color (rule_color.R, rule_color.G, rule_color.B, Context.FillAlpha));
			cr.Rectangle (x, y, width, height);
			cr.Fill ();
		}
	}
}
