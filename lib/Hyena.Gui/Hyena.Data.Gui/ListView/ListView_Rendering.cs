//
// ListView_Rendering.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Gdk;

using Gtk;

using Hyena.Gui;
using Hyena.Gui.Canvas;
using Hyena.Gui.Theming;

using GtkColorClass = Hyena.Gui.Theming.GtkColorClass;

namespace Hyena.Data.Gui
{
	public delegate int ListViewRowHeightHandler (Widget widget);

	public partial class ListView<T> : ListViewBase
	{
		Cairo.Context cairo_context;
		CellContext cell_context;
		Pango.Layout pango_layout;

		public override Pango.Layout PangoLayout {
			get {
				if (pango_layout == null && GdkWindow != null && IsRealized) {
					using (var cr = Gdk.CairoHelper.Create (GdkWindow)) {
						pango_layout = CairoExtensions.CreateLayout (this, cr);
						cell_context.FontDescription = pango_layout.FontDescription;
						cell_context.Layout = pango_layout;
					}
				}
				return pango_layout;
			}
		}

		public override Pango.FontDescription FontDescription {
			get { return cell_context.FontDescription; }
		}

		List<int> selected_rows = new List<int> ();

		Theme theme;
		protected Theme Theme {
			get { return theme; }
		}

		// Using an auto-property here makes the build fail with mono 1.9.1 (bnc#396633)
		bool do_not_render_null_model;
		public bool DoNotRenderNullModel {
			get { return do_not_render_null_model; }
			set { do_not_render_null_model = value; }
		}

		bool changing_style = false;

		protected override void OnStyleSet (Style old_style)
		{
			if (changing_style) {
				return;
			}

			changing_style = true;
			GtkUtilities.AdaptGtkRcStyle (this, typeof (TreeView));
			changing_style = false;

			base.OnStyleSet (old_style);

			// FIXME: legacy list foo
			if (ViewLayout == null) {
				OnInvalidateMeasure ();
			}

			theme = Hyena.Gui.Theming.ThemeEngine.CreateTheme (this);

			// Save the drawable so we can reuse it
			Gdk.Drawable drawable = cell_context?.Drawable;

			if (pango_layout != null) {
				cell_context.FontDescription.Dispose ();
				pango_layout.Dispose ();
				pango_layout = null;
				cell_context.Layout = null;
				cell_context.FontDescription = null;
			}

			cell_context = new CellContext ();
			cell_context.Theme = theme;
			cell_context.Widget = this;
			cell_context.Drawable = drawable;
			SetDirection ();
		}

		void SetDirection ()
		{
			var dir = Direction;
			if (dir == Gtk.TextDirection.None) {
				dir = Gtk.Widget.DefaultDirection;
			}

			if (cell_context != null) {
				cell_context.IsRtl = dir == Gtk.TextDirection.Rtl;
			}
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (DoNotRenderNullModel && Model == null) {
				return true;
			}

			var damage = new Rectangle ();
			foreach (Rectangle rect in evnt.Region.GetRectangles ()) {
				damage = damage.Union (rect);
			}

			cairo_context = CairoHelper.Create (evnt.Window);

			cell_context.Layout = PangoLayout;
			cell_context.Context = cairo_context;

			// FIXME: legacy list foo
			if (ViewLayout == null) {
				OnMeasure ();
			}

			Theme.DrawFrameBackground (cairo_context, Allocation, true);

			// FIXME: ViewLayout will never be null in the future but we'll need
			// to deterministically render a header somehow...
			if (header_visible && ViewLayout == null && column_controller != null) {
				PaintHeader (damage);
			}

			if (Model != null) {
				// FIXME: ViewLayout will never be null in
				// the future, PaintList will go away
				if (ViewLayout == null) {
					PaintList (damage);
				} else {
					PaintView ((Rect)damage);
				}
			}

			Theme.DrawFrameBorder (cairo_context, Allocation);

			PaintDraggingColumn (damage);

			CairoExtensions.DisposeContext (cairo_context);

			return true;
		}

		#region Header Rendering

		void PaintHeader (Rectangle clip)
		{
			Rectangle rect = header_rendering_alloc;
			rect.Height += Theme.BorderWidth;
			clip.Intersect (rect);
			cairo_context.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
			cairo_context.Clip ();

			Theme.DrawHeaderBackground (cairo_context, header_rendering_alloc);

			var cell_area = new Rectangle ();
			cell_area.Y = header_rendering_alloc.Y;
			cell_area.Height = header_rendering_alloc.Height;

			cell_context.Clip = clip;
			cell_context.Opaque = true;
			cell_context.TextAsForeground = true;

			bool have_drawn_separator = false;

			for (int ci = 0; ci < column_cache.Length; ci++) {
				if (pressed_column_is_dragging && pressed_column_index == ci) {
					continue;
				}

				cell_area.X = column_cache[ci].X1 + Theme.TotalBorderWidth + header_rendering_alloc.X - HadjustmentValue;
				cell_area.Width = column_cache[ci].Width;
				PaintHeaderCell (cell_area, ci, false, ref have_drawn_separator);
			}

			if (pressed_column_is_dragging && pressed_column_index >= 0) {
				cell_area.X = pressed_column_x_drag + Allocation.X - HadjustmentValue;
				cell_area.Width = column_cache[pressed_column_index].Width;
				PaintHeaderCell (cell_area, pressed_column_index, true, ref have_drawn_separator);
			}

			cairo_context.ResetClip ();
		}

		void PaintHeaderCell (Rectangle area, int ci, bool dragging, ref bool have_drawn_separator)
		{
			if (ci < 0 || column_cache.Length <= ci)
				return;

			if (ci == ActiveColumn && HasFocus && HeaderFocused) {
				Theme.DrawColumnHeaderFocus (cairo_context, area);
			}

			if (dragging) {
				Theme.DrawColumnHighlight (cairo_context, area,
					CairoExtensions.ColorShade (Theme.Colors.GetWidgetColor (GtkColorClass.Dark, StateType.Normal), 0.9));

				Cairo.Color stroke_color = CairoExtensions.ColorShade (Theme.Colors.GetWidgetColor (
					GtkColorClass.Base, StateType.Normal), 0.0);
				stroke_color.A = 0.3;

				cairo_context.SetSourceColor (stroke_color);
				cairo_context.MoveTo (area.X + 0.5, area.Y + 1.0);
				cairo_context.LineTo (area.X + 0.5, area.Bottom);
				cairo_context.MoveTo (area.Right - 0.5, area.Y + 1.0);
				cairo_context.LineTo (area.Right - 0.5, area.Bottom);
				cairo_context.Stroke ();
			}

			ColumnCell cell = column_cache[ci].Column.HeaderCell;

			if (cell != null) {
				cairo_context.Save ();
				cairo_context.Translate (area.X, area.Y);
				cell_context.Area = area;
				cell_context.State = StateType.Normal;
				cell.Render (cell_context, area.Width, area.Height);
				cairo_context.Restore ();
			}

			if (!dragging && ci < column_cache.Length - 1 && (have_drawn_separator ||
				column_cache[ci].MaxWidth != column_cache[ci].MinWidth)) {
				have_drawn_separator = true;
				Theme.DrawHeaderSeparator (cairo_context, area, area.Right);
			}
		}

		#endregion

		#region List Rendering

		void RenderDarkBackgroundInSortedColumn ()
		{
			if (pressed_column_is_dragging && pressed_column_index == sort_column_index) {
				return;
			}

			CachedColumn col = column_cache[sort_column_index];
			Theme.DrawRowRule (cairo_context,
							   list_rendering_alloc.X + col.X1 - HadjustmentValue,
							   header_rendering_alloc.Bottom + Theme.BorderWidth,
							   col.Width,
							   list_rendering_alloc.Height + Theme.InnerBorderWidth * 2);
		}

		void PaintList (Rectangle clip)
		{
			if (ChildSize.Height <= 0) {
				return;
			}

			if (sort_column_index != -1) {
				RenderDarkBackgroundInSortedColumn ();
			}

			clip.Intersect (list_rendering_alloc);
			cairo_context.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
			cairo_context.Clip ();

			cell_context.Clip = clip;
			cell_context.TextAsForeground = false;

			int vadjustment_value = VadjustmentValue;
			int first_row = vadjustment_value / ChildSize.Height;
			int last_row = Math.Min (model.Count, first_row + RowsInView);
			int offset = list_rendering_alloc.Y - vadjustment_value % ChildSize.Height;

			Rectangle selected_focus_alloc = Rectangle.Zero;
			var single_list_alloc = new Rectangle ();

			single_list_alloc.X = list_rendering_alloc.X - HadjustmentValue;
			single_list_alloc.Y = offset;
			single_list_alloc.Width = list_rendering_alloc.Width + HadjustmentValue;
			single_list_alloc.Height = ChildSize.Height;

			int selection_height = 0;
			int selection_y = 0;
			selected_rows.Clear ();

			for (int ri = first_row; ri < last_row; ri++) {
				if (Selection != null && Selection.Contains (ri)) {
					if (selection_height == 0) {
						selection_y = single_list_alloc.Y;
					}

					selection_height += single_list_alloc.Height;
					selected_rows.Add (ri);

					if (Selection.FocusedIndex == ri) {
						selected_focus_alloc = single_list_alloc;
					}
				} else {
					if (rules_hint && ri % 2 != 0) {
						Theme.DrawRowRule (cairo_context, single_list_alloc.X, single_list_alloc.Y,
							single_list_alloc.Width, single_list_alloc.Height);
					}

					PaintReorderLine (ri, single_list_alloc);

					if (Selection != null && Selection.FocusedIndex == ri && !Selection.Contains (ri) && HasFocus) {
						CairoCorners corners = CairoCorners.All;

						if (Selection.Contains (ri - 1)) {
							corners &= ~(CairoCorners.TopLeft | CairoCorners.TopRight);
						}

						if (Selection.Contains (ri + 1)) {
							corners &= ~(CairoCorners.BottomLeft | CairoCorners.BottomRight);
						}

						if (HasFocus && !HeaderFocused) // Cursor out of selection.
							Theme.DrawRowCursor (cairo_context, single_list_alloc.X, single_list_alloc.Y,
												 single_list_alloc.Width, single_list_alloc.Height,
												 CairoExtensions.ColorShade (Theme.Colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected), 0.85));
					}

					if (selection_height > 0) {
						Cairo.Color selection_color = Theme.Colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected);
						if (!HasFocus || HeaderFocused)
							selection_color = CairoExtensions.ColorShade (selection_color, 1.1);

						Theme.DrawRowSelection (cairo_context, list_rendering_alloc.X, selection_y, list_rendering_alloc.Width, selection_height,
												true, true, selection_color, CairoCorners.All);
						selection_height = 0;
					}

					PaintRow (ri, single_list_alloc, StateType.Normal);
				}

				single_list_alloc.Y += single_list_alloc.Height;
			}

			// In case the user is dragging to the end of the list
			PaintReorderLine (last_row, single_list_alloc);

			if (selection_height > 0) {
				Theme.DrawRowSelection (cairo_context, list_rendering_alloc.X, selection_y,
					list_rendering_alloc.Width, selection_height);
			}

			if (Selection != null && Selection.Count > 1 &&
				!selected_focus_alloc.Equals (Rectangle.Zero) &&
				HasFocus && !HeaderFocused) { // Cursor inside selection.
				Theme.DrawRowCursor (cairo_context, selected_focus_alloc.X, selected_focus_alloc.Y,
					selected_focus_alloc.Width, selected_focus_alloc.Height,
					Theme.Colors.GetWidgetColor (GtkColorClass.Text, StateType.Selected));
			}

			foreach (int ri in selected_rows) {
				single_list_alloc.Y = offset + ((ri - first_row) * single_list_alloc.Height);
				PaintRow (ri, single_list_alloc, StateType.Selected);
			}

			cairo_context.ResetClip ();
		}

		void PaintReorderLine (int row_index, Rectangle single_list_alloc)
		{
			if (row_index == drag_reorder_row_index && IsReorderable) {
				cairo_context.Save ();
				cairo_context.LineWidth = 1.0;
				cairo_context.Antialias = Cairo.Antialias.None;
				cairo_context.MoveTo (single_list_alloc.Left, single_list_alloc.Top);
				cairo_context.LineTo (single_list_alloc.Right, single_list_alloc.Top);
				cairo_context.SetSourceColor (Theme.Colors.GetWidgetColor (GtkColorClass.Text, StateType.Normal));
				cairo_context.Stroke ();
				cairo_context.Restore ();
			}
		}

		void PaintRow (int row_index, Rectangle area, StateType state)
		{
			if (column_cache == null) {
				return;
			}

			object item = model[row_index];
			bool opaque = IsRowOpaque (item);
			bool bold = IsRowBold (item);

			var cell_area = new Rectangle ();
			cell_area.Height = ChildSize.Height;
			cell_area.Y = area.Y;

			cell_context.ViewRowIndex = cell_context.ModelRowIndex = row_index;

			for (int ci = 0; ci < column_cache.Length; ci++) {
				cell_context.ViewColumnIndex = ci;

				if (pressed_column_is_dragging && pressed_column_index == ci) {
					continue;
				}

				cell_area.Width = column_cache[ci].Width;
				cell_area.X = column_cache[ci].X1 + area.X;
				PaintCell (item, ci, row_index, cell_area, opaque, bold, state, false);
			}

			if (pressed_column_is_dragging && pressed_column_index >= 0) {
				cell_area.Width = column_cache[pressed_column_index].Width;
				cell_area.X = pressed_column_x_drag + list_rendering_alloc.X -
					list_interaction_alloc.X - HadjustmentValue;
				PaintCell (item, pressed_column_index, row_index, cell_area, opaque, bold, state, true);
			}
		}

		void PaintCell (object item, int column_index, int row_index, Rectangle area, bool opaque, bool bold,
			StateType state, bool dragging)
		{
			ColumnCell cell = column_cache[column_index].Column.GetCell (0);
			cell.Bind (item);
			cell.Manager = manager;
			ColumnCellDataProvider (cell, item);

			var text_cell = cell as ITextCell;
			if (text_cell != null) {
				text_cell.FontWeight = bold ? Pango.Weight.Bold : Pango.Weight.Normal;
			}

			if (dragging) {
				Cairo.Color fill_color = Theme.Colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
				fill_color.A = 0.5;
				cairo_context.SetSourceColor (fill_color);
				cairo_context.Rectangle (area.X, area.Y, area.Width, area.Height);
				cairo_context.Fill ();
			}

			cairo_context.Save ();
			cairo_context.Translate (area.X, area.Y);
			cell_context.Area = area;
			cell_context.Opaque = opaque;
			cell_context.State = dragging ? StateType.Normal : state;
			cell.Render (cell_context, area.Width, area.Height);
			cairo_context.Restore ();

			AccessibleCellRedrawn (column_index, row_index);
		}

		void PaintDraggingColumn (Rectangle clip)
		{
			if (!pressed_column_is_dragging || pressed_column_index < 0) {
				return;
			}

			CachedColumn column = column_cache[pressed_column_index];

			int x = pressed_column_x_drag + Allocation.X + 1 - HadjustmentValue;

			Cairo.Color fill_color = Theme.Colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
			fill_color.A = 0.45;

			Cairo.Color stroke_color = CairoExtensions.ColorShade (Theme.Colors.GetWidgetColor (
				GtkColorClass.Base, StateType.Normal), 0.0);
			stroke_color.A = 0.3;

			cairo_context.Rectangle (x, header_rendering_alloc.Bottom + 1, column.Width - 2,
				list_rendering_alloc.Bottom - header_rendering_alloc.Bottom - 1);
			cairo_context.SetSourceColor (fill_color);
			cairo_context.Fill ();

			cairo_context.MoveTo (x - 0.5, header_rendering_alloc.Bottom + 0.5);
			cairo_context.LineTo (x - 0.5, list_rendering_alloc.Bottom + 0.5);
			cairo_context.LineTo (x + column.Width - 1.5, list_rendering_alloc.Bottom + 0.5);
			cairo_context.LineTo (x + column.Width - 1.5, header_rendering_alloc.Bottom + 0.5);

			cairo_context.SetSourceColor (stroke_color);
			cairo_context.LineWidth = 1.0;
			cairo_context.Stroke ();
		}

		#endregion

		#region View Layout Rendering

		void PaintView (Rect clip)
		{
			clip.Intersect ((Rect)list_rendering_alloc);
			cairo_context.Rectangle ((Cairo.Rectangle)clip);
			cairo_context.Clip ();

			cell_context.Clip = (Gdk.Rectangle)clip;
			cell_context.TextAsForeground = false;

			selected_rows.Clear ();

			for (int layout_index = 0; layout_index < ViewLayout.ChildCount; layout_index++) {
				var layout_child = ViewLayout[layout_index];
				var child_allocation = layout_child.Allocation;

				if (!child_allocation.IntersectsWith (clip) || ViewLayout.GetModelIndex (layout_child) >= Model.Count) {
					continue;
				}

				if (Selection != null && Selection.Contains (ViewLayout.GetModelIndex (layout_child))) {
					selected_rows.Add (ViewLayout.GetModelIndex (layout_child));

					var selection_color = Theme.Colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected);
					if (!HasFocus || HeaderFocused) {
						selection_color = CairoExtensions.ColorShade (selection_color, 1.1);
					}

					Theme.DrawRowSelection (cairo_context,
						(int)child_allocation.X, (int)child_allocation.Y,
						(int)child_allocation.Width, (int)child_allocation.Height,
						true, true, selection_color, CairoCorners.All);

					cell_context.State = StateType.Selected;
				} else {
					cell_context.State = StateType.Normal;
				}

				//cairo_context.Save ();
				//cairo_context.Translate (child_allocation.X, child_allocation.Y);
				//cairo_context.Rectangle (0, 0, child_allocation.Width, child_allocation.Height);
				//cairo_context.Clip ();
				layout_child.Render (cell_context);
				//cairo_context.Restore ();
			}

			cairo_context.ResetClip ();
		}

		#endregion

		protected void InvalidateList ()
		{
			if (IsRealized) {
				QueueDirtyRegion (list_rendering_alloc);
			}
		}

		void InvalidateHeader ()
		{
			if (IsRealized) {
				QueueDirtyRegion (header_rendering_alloc);
			}
		}

		protected void QueueDirtyRegion ()
		{
			QueueDirtyRegion (list_rendering_alloc);
		}

		protected virtual void ColumnCellDataProvider (ColumnCell cell, object boundItem)
		{
		}

		bool rules_hint = false;
		public bool RulesHint {
			get { return rules_hint; }
			set {
				rules_hint = value;
				InvalidateList ();
			}
		}

		// FIXME: Obsolete all this measure stuff on the view since it's in the layout
		#region Measuring

		Gdk.Size child_size = Gdk.Size.Empty;
		public Gdk.Size ChildSize {
			get {
				return ViewLayout != null
					? new Gdk.Size ((int)ViewLayout.ChildSize.Width, (int)ViewLayout.ChildSize.Height)
					: child_size;
			}
		}

		bool measure_pending;

		protected virtual void OnInvalidateMeasure ()
		{
			measure_pending = true;
			if (IsMapped && IsRealized) {
				QueueDirtyRegion ();
			}
		}

		protected virtual Gdk.Size OnMeasureChild ()
		{
			return ViewLayout != null
				? new Gdk.Size ((int)ViewLayout.ChildSize.Width, (int)ViewLayout.ChildSize.Height)
				: new Gdk.Size (0, ColumnCellText.ComputeRowHeight (this));
		}

		void OnMeasure ()
		{
			if (!measure_pending) {
				return;
			}

			measure_pending = false;

			header_height = 0;
			child_size = OnMeasureChild ();
			UpdateAdjustments ();
		}

		#endregion

	}
}
