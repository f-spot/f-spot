//
// SimpleTable.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Gtk;

namespace Hyena.Widgets
{
	public class SimpleTable<T> : Table
	{
		bool added_any;

		List<T> items = new List<T> ();
		Dictionary<T, Widget[]> item_widgets = new Dictionary<T, Widget[]> ();
		AttachOptions default_options = AttachOptions.Fill | AttachOptions.Expand;

		public SimpleTable () : this (2) { }

		public SimpleTable (int n_columns) : base (1, (uint)n_columns, false)
		{
			ColumnSpacing = 5;
			RowSpacing = 5;

			XOptions = new AttachOptions[n_columns];
			for (int i = 0; i < n_columns; i++) {
				XOptions[i] = default_options;
			}
		}

		public void AddRow (T item, params Widget[] cols)
		{
			InsertRow (item, (uint)items.Count, cols);
		}

		public AttachOptions[] XOptions { get; private set; }

		public void InsertRow (T item, uint row, params Widget[] cols)
		{
			if (!added_any) {
				added_any = true;
			} else if (NColumns != cols.Length) {
				throw new ArgumentException ("cols", string.Format ("Expected {0} column widgets, same as previous calls to Add", NColumns));
			}

			Resize ((uint)items.Count + 1, (uint)cols.Length);

			for (int y = items.Count - 1; y >= row; y--) {
				for (uint x = 0; x < NColumns; x++) {
					var widget = item_widgets[items[y]][x];
					Remove (widget);
					Attach (widget, x, x + 1, (uint)y + 1, (uint)y + 2, XOptions[x], default_options, 0, 0);
				}
			}

			items.Insert ((int)row, item);
			item_widgets[item] = cols;

			for (uint x = 0; x < NColumns; x++) {
				Attach (cols[x], x, x + 1, row, row + 1, XOptions[x], default_options, 0, 0);
			}
		}

		public void RemoveRow (T item)
		{
			FreezeChildNotify ();

			foreach (var widget in item_widgets[item]) {
				Remove (widget);
			}

			int index = items.IndexOf (item);
			for (int y = index + 1; y < items.Count; y++) {
				for (uint x = 0; x < NColumns; x++) {
					var widget = item_widgets[items[y]][x];
					Remove (widget);
					Attach (widget, x, x + 1, (uint)y - 1, (uint)y, XOptions[x], default_options, 0, 0);
				}
			}

			Resize ((uint)Math.Max (1, items.Count - 1), NColumns);

			ThawChildNotify ();
			items.Remove (item);
			item_widgets.Remove (item);
		}
	}
}
