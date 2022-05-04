//
// IListModel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Data
{
	public interface IListModel : ISelectable
	{
		event EventHandler Cleared;
		event EventHandler Reloaded;

		void Clear ();
		void Reload ();

		int Count { get; }
		bool CanReorder { get; }

		object GetItem (int index);
	}

	public interface IListModel<T> : IListModel
	{
		T this[int index] { get; }
	}

	public interface IObjectListModel : IListModel<object>
	{
		ColumnDescription[] ColumnDescriptions { get; }
	}
}