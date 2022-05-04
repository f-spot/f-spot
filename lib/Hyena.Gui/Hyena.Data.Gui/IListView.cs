//
// IListView.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Gui
{
	public interface IListView
	{
		Hyena.Collections.SelectionProxy SelectionProxy { get; }
		Hyena.Collections.Selection Selection { get; }

		void ScrollTo (int index);
		void CenterOn (int index);
		void GrabFocus ();
		ColumnController ColumnController { get; set; }
	}

	public interface IListView<T> : IListView
	{
		void SetModel (IListModel<T> model);
		IListModel<T> Model { get; }
	}
}
