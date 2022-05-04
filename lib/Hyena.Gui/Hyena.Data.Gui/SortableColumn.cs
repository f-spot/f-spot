//
// SortableColumn.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Gui
{
	public class SortableColumn : Column, ISortableColumn
	{
		string sort_key;
		SortType sort_type = SortType.Ascending;
		Hyena.Query.QueryField field;

		public SortableColumn (string title, ColumnCell cell, double width, string sort_key, bool visible) :
			base (title, cell, width, visible)
		{
			this.sort_key = sort_key;
		}

		public SortableColumn (ColumnCell header_cell, string title, ColumnCell cell, double width, string sort_key, bool visible) :
			base (header_cell, title, cell, width, visible)
		{
			this.sort_key = sort_key;
		}

		public string SortKey {
			get { return sort_key; }
			set { sort_key = value; }
		}

		public SortType SortType {
			get { return sort_type; }
			set { sort_type = value; }
		}

		public Hyena.Query.QueryField Field {
			get { return field; }
			set { field = value; }
		}
	}
}
