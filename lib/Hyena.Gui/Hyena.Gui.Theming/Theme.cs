//
// Theme.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Gtk;

namespace Hyena.Gui.Theming
{
	public abstract class Theme
	{
		static Cairo.Color black = new Cairo.Color (0, 0, 0);
		Stack<ThemeContext> contexts = new Stack<ThemeContext> ();
		GtkColors colors;

		Cairo.Color selection_fill;
		Cairo.Color selection_stroke;

		Cairo.Color view_fill;
		Cairo.Color view_fill_transparent;

		Cairo.Color text_mid;

		public GtkColors Colors {
			get { return colors; }
		}

		public Widget Widget { get; private set; }

		public Theme (Widget widget) : this (widget, new GtkColors ())
		{
		}

		public Theme (Widget widget, GtkColors colors)
		{
			Widget = widget;
			this.colors = colors;
			this.colors.Refreshed += delegate { OnColorsRefreshed (); };
			this.colors.Widget = widget;

			PushContext ();
		}

		protected virtual void OnColorsRefreshed ()
		{
			selection_fill = colors.GetWidgetColor (GtkColorClass.Dark, StateType.Active);
			selection_stroke = colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected);

			view_fill = colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
			view_fill_transparent = view_fill;
			view_fill_transparent.A = 0;

			text_mid = CairoExtensions.AlphaBlend (
				colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal),
				colors.GetWidgetColor (GtkColorClass.Text, StateType.Normal),
				0.5);
		}

		#region Drawing

		public abstract void DrawPie (double fraction);

		public void DrawArrow (Cairo.Context cr, Gdk.Rectangle alloc, Hyena.Data.SortType type)
		{
			DrawArrow (cr, alloc, Math.PI / 2.0 * (type == Hyena.Data.SortType.Ascending ? 1 : -1));
		}

		public abstract void DrawArrow (Cairo.Context cr, Gdk.Rectangle alloc, double rotation);

		public void DrawFrame (Cairo.Context cr, Gdk.Rectangle alloc, bool baseColor)
		{
			DrawFrameBackground (cr, alloc, baseColor);
			DrawFrameBorder (cr, alloc);
		}

		public void DrawFrame (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color)
		{
			DrawFrameBackground (cr, alloc, color);
			DrawFrameBorder (cr, alloc);
		}

		public void DrawFrameBackground (Cairo.Context cr, Gdk.Rectangle alloc, bool baseColor)
		{
			DrawFrameBackground (cr, alloc, baseColor
				? colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal)
				: colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal));
		}

		public void DrawFrameBackground (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color)
		{
			DrawFrameBackground (cr, alloc, color, null);
		}

		public void DrawFrameBackground (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Pattern pattern)
		{
			DrawFrameBackground (cr, alloc, black, pattern);
		}

		public abstract void DrawFrameBackground (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color, Cairo.Pattern pattern);

		public abstract void DrawFrameBorder (Cairo.Context cr, Gdk.Rectangle alloc);

		public abstract void DrawHeaderBackground (Cairo.Context cr, Gdk.Rectangle alloc);

		public abstract void DrawColumnHeaderFocus (Cairo.Context cr, Gdk.Rectangle alloc);

		public abstract void DrawHeaderSeparator (Cairo.Context cr, Gdk.Rectangle alloc, int x);

		public void DrawListBackground (Cairo.Context cr, Gdk.Rectangle alloc, bool baseColor)
		{
			DrawListBackground (cr, alloc, baseColor
				? colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal)
				: colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal));
		}

		public abstract void DrawListBackground (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color);

		public void DrawColumnHighlight (Cairo.Context cr, double cellWidth, double cellHeight)
		{
			var alloc = new Gdk.Rectangle ();
			alloc.Width = (int)cellWidth;
			alloc.Height = (int)cellHeight;
			DrawColumnHighlight (cr, alloc);
		}

		public void DrawColumnHighlight (Cairo.Context cr, Gdk.Rectangle alloc)
		{
			DrawColumnHighlight (cr, alloc, colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected));
		}

		public abstract void DrawColumnHighlight (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color);

		public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height)
		{
			DrawRowSelection (cr, x, y, width, height, true);
		}

		public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height, bool filled)
		{
			DrawRowSelection (cr, x, y, width, height, filled, true,
				colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected), CairoCorners.All);
		}

		public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height,
			bool filled, bool stroked, Cairo.Color color)
		{
			DrawRowSelection (cr, x, y, width, height, filled, stroked, color, CairoCorners.All);
		}

		public void DrawRowCursor (Cairo.Context cr, int x, int y, int width, int height)
		{
			DrawRowCursor (cr, x, y, width, height, colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected));
		}

		public void DrawRowCursor (Cairo.Context cr, int x, int y, int width, int height, Cairo.Color color)
		{
			DrawRowCursor (cr, x, y, width, height, color, CairoCorners.All);
		}

		public abstract void DrawRowCursor (Cairo.Context cr, int x, int y, int width, int height, Cairo.Color color, CairoCorners corners);

		public abstract void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height,
			bool filled, bool stroked, Cairo.Color color, CairoCorners corners);

		public abstract void DrawRowRule (Cairo.Context cr, int x, int y, int width, int height);

		public Cairo.Color ViewFill {
			get { return view_fill; }
		}

		public Cairo.Color ViewFillTransparent {
			get { return view_fill_transparent; }
		}

		public Cairo.Color SelectionFill {
			get { return selection_fill; }
		}

		public Cairo.Color SelectionStroke {
			get { return selection_stroke; }
		}

		public Cairo.Color TextMidColor {
			get { return text_mid; }
			protected set { text_mid = value; }
		}

		public virtual int BorderWidth {
			get { return 1; }
		}

		public virtual int InnerBorderWidth {
			get { return 4; }
		}

		public int TotalBorderWidth {
			get { return BorderWidth + InnerBorderWidth; }
		}

		#endregion

		#region Contexts

		public virtual void PushContext ()
		{
			PushContext (new ThemeContext ());
		}

		public virtual void PushContext (ThemeContext context)
		{
			lock (this) {
				contexts.Push (context);
			}
		}

		public virtual ThemeContext PopContext ()
		{
			lock (this) {
				return contexts.Pop ();
			}
		}

		public virtual ThemeContext Context {
			get { lock (this) { return contexts.Peek (); } }
		}

		#endregion

		#region Static Utilities

		public static double Clamp (double min, double max, double value)
		{
			return Math.Max (min, Math.Min (max, value));
		}

		#endregion

	}
}
