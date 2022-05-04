//
// ListViewAccessible_Selection.cs
//
// Authors:
//   Eitan Isaacson <eitan@ascender.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Eitan Isaacson
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Data.Gui.Accessibility
{
	public partial class ListViewAccessible<T> : Atk.SelectionImplementor
	{
		public void ListViewAccessible_Selection ()
		{
			list_view.SelectionProxy.Changed += OnSelectionChanged;
		}

		public bool AddSelection (int index)
		{
			return AddRowSelection (GetRowAtIndex (index));
		}

		public bool ClearSelection ()
		{
			list_view.Selection.Clear ();
			return true;
		}

		public bool IsChildSelected (int index)
		{
			return IsRowSelected (GetRowAtIndex (index));
		}

		public bool RemoveSelection (int index)
		{
			int row = list_view.Selection.RangeCollection[index / n_columns];
			return RemoveRowSelection (row);
		}

		public Atk.Object RefSelection (int index)
		{
			int row = list_view.Selection.RangeCollection[index / n_columns];
			int column = index % n_columns;
			return RefAt (row, column);
		}

		public int SelectionCount {
			get { return list_view.Selection.Count * n_columns; }
		}

		public bool SelectAllSelection ()
		{
			list_view.Selection.SelectAll ();
			return true;
		}

		void OnSelectionChanged (object o, EventArgs a)
		{
			GLib.Signal.Emit (this, "selection_changed");
		}
	}
}
