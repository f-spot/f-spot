//
// Column.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Hyena.Data.Gui
{
	public class Column : ColumnDescription, IEnumerable<ColumnCell>
	{
		ColumnCell header_cell;
		List<ColumnCell> cells = new List<ColumnCell> ();

		int min_width = 0;
		int max_width = int.MaxValue;

		public Column (ColumnDescription description) :
			this (description, new ColumnCellText (description.Property, true))
		{
		}

		public Column (ColumnDescription description, ColumnCell cell) :
			this (description.Title, cell, description.Width, description.Visible)
		{
		}

		public Column (string title, ColumnCell cell, double width)
			: this (title, cell, width, true)
		{
		}

		public Column (string title, ColumnCell cell, double width, bool visible)
			: this (null, title, cell, width, visible)
		{
		}

		public Column (ColumnCell headerCell, string title, ColumnCell cell, double width)
			: this (headerCell, title, cell, width, true)
		{
		}

		public Column (ColumnCell headerCell, string title, ColumnCell cell, double width, bool visible)
			: this (headerCell, title, cell, width, visible, 0, int.MaxValue)
		{
		}

		public Column (ColumnCell headerCell, string title, ColumnCell cell, double width, bool visible, int minWidth, int maxWidth)
			: base (cell.ObjectBinder.Property, title, width, visible)
		{
			min_width = minWidth;
			max_width = maxWidth;
			header_cell = headerCell ?? new ColumnHeaderCellText (HeaderCellDataHandler);

			var header_text = header_cell as ColumnCellText;
			var cell_text = cell as ColumnCellText;
			if (header_text != null && cell_text != null) {
				header_text.Alignment = cell_text.Alignment;
			}

			PackStart (cell);
		}

		Column HeaderCellDataHandler ()
		{
			return this;
		}

		public void PackStart (ColumnCell cell)
		{
			cells.Insert (0, cell);
		}

		public void PackEnd (ColumnCell cell)
		{
			cells.Add (cell);
		}

		public ColumnCell GetCell (int index)
		{
			return cells[index];
		}

		public void RemoveCell (int index)
		{
			cells.RemoveAt (index);
		}

		public void ClearCells ()
		{
			cells.Clear ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return cells.GetEnumerator ();
		}

		IEnumerator<ColumnCell> IEnumerable<ColumnCell>.GetEnumerator ()
		{
			return cells.GetEnumerator ();
		}

		public ColumnCell HeaderCell {
			get { return header_cell; }
			set { header_cell = value; }
		}

		public void CalculateWidths (Pango.Layout layout, bool headerVisible, int headerHeight)
		{
			bool min_was_zero = MinWidth == 0;
			bool was_size_req = false;
			var sr_cell = cells[0] as ISizeRequestCell;
			if (sr_cell != null && sr_cell.RestrictSize) {
				sr_cell.GetWidthRange (layout, out var min_w, out var max_w);
				MinWidth = min_w == -1 ? MinWidth : min_w;
				MaxWidth = max_w == -1 ? MaxWidth : max_w;
				was_size_req = true;
			}

			if (headerVisible && (min_was_zero || was_size_req) && !string.IsNullOrEmpty (Title)) {
				layout.SetText (Title);
				//column_layout.SetText ("\u2026"); // ellipsis char
				layout.GetPixelSize (out var w, out var h);

				// Pretty sure the 3* is needed here only b/c of the " - 8" in ColumnCellText;
				// See TODO there
				w += 3 * (int)header_cell.Padding.Left;
				if (this is ISortableColumn) {
					w += ColumnHeaderCellText.GetArrowWidth (headerHeight);
				}

				MinWidth = Math.Max (MinWidth, w);

				// If the min/max are sufficiently close (arbitrarily choosen as < 8px) then
				// make them equal, so that the column doesn't appear resizable but in reality is on barely.
				if (MaxWidth - MinWidth < 8) {
					MinWidth = MaxWidth;
				}
			}
		}

		public int MinWidth {
			get { return min_width; }
			set {
				min_width = value;
				if (value > max_width) {
					max_width = value;
				}
			}
		}

		public int MaxWidth {
			get { return max_width; }
			set {
				max_width = value;
				if (value < min_width) {
					min_width = value;
				}
			}
		}

		string id;
		public string Id {
			get {
				if (id == null) {
					id = GetCell (0).ObjectBinder.SubProperty ?? GetCell (0).ObjectBinder.Property;
					id = StringUtil.CamelCaseToUnderCase (id);
				}
				return id;
			}
			set { id = value; }
		}
	}
}
