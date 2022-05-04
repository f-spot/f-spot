//
// ColumnController.cs
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
	public class ColumnController : IEnumerable<Column>
	{
		List<Column> columns = new List<Column> ();
		ISortableColumn default_sort_column;
		ISortableColumn sort_column;

		protected List<Column> Columns {
			get { return columns; }
		}

		public event EventHandler Updated;

		protected virtual void OnVisibilitiesChanged ()
		{
			OnUpdated ();
		}

		protected virtual void OnWidthsChanged ()
		{
		}

		protected void OnUpdated ()
		{
			Updated?.Invoke (this, EventArgs.Empty);
		}

		public void Clear ()
		{
			lock (this) {
				foreach (Column column in columns) {
					column.VisibilityChanged -= OnColumnVisibilityChanged;
					column.WidthChanged -= OnColumnWidthChanged;
				}
				columns.Clear ();
			}

			OnUpdated ();
		}

		public void AddRange (params Column[] range)
		{
			lock (this) {
				foreach (Column column in range) {
					column.VisibilityChanged += OnColumnVisibilityChanged;
					column.WidthChanged += OnColumnWidthChanged;
				}
				columns.AddRange (range);
			}

			OnUpdated ();
		}

		public void Add (Column column)
		{
			lock (this) {
				column.VisibilityChanged += OnColumnVisibilityChanged;
				column.WidthChanged += OnColumnWidthChanged;
				columns.Add (column);
			}

			OnUpdated ();
		}

		public void Insert (Column column, int index)
		{
			lock (this) {
				column.VisibilityChanged += OnColumnVisibilityChanged;
				column.WidthChanged += OnColumnWidthChanged;
				columns.Insert (index, column);
			}

			OnUpdated ();
		}

		public void Remove (Column column)
		{
			lock (this) {
				column.VisibilityChanged -= OnColumnVisibilityChanged;
				column.WidthChanged -= OnColumnWidthChanged;
				columns.Remove (column);
			}

			OnUpdated ();
		}

		public void Remove (int index)
		{
			lock (this) {
				Column column = columns[index];
				column.VisibilityChanged -= OnColumnVisibilityChanged;
				column.WidthChanged -= OnColumnWidthChanged;
				columns.RemoveAt (index);
			}

			OnUpdated ();
		}

		public void Reorder (int index, int newIndex)
		{
			lock (this) {
				Column column = columns[index];
				columns.RemoveAt (index);
				columns.Insert (newIndex, column);
			}

			OnUpdated ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return columns.GetEnumerator ();
		}

		IEnumerator<Column> IEnumerable<Column>.GetEnumerator ()
		{
			return columns.GetEnumerator ();
		}

		public int IndexOf (Column column)
		{
			lock (this) {
				return columns.IndexOf (column);
			}
		}

		public Column[] ToArray ()
		{
			return columns.ToArray ();
		}

		void OnColumnVisibilityChanged (object o, EventArgs args)
		{
			OnVisibilitiesChanged ();
		}

		void OnColumnWidthChanged (object o, EventArgs args)
		{
			OnWidthsChanged ();
		}

		public Column this[int index] {
			get { return columns[index]; }
		}

		public ISortableColumn DefaultSortColumn {
			get { return default_sort_column; }
			set { default_sort_column = value; }
		}

		public virtual ISortableColumn SortColumn {
			get { return sort_column; }
			set { sort_column = value; }
		}

		public int Count {
			get { return columns.Count; }
		}

		public virtual bool EnableColumnMenu {
			get { return false; }
		}
	}
}
