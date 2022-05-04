//
// RowActivatedHandler.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Data.Gui
{
	public delegate void RowActivatedHandler<T> (object o, RowActivatedArgs<T> args);

	public class RowActivatedArgs<T> : EventArgs
	{
		int row;
		T row_value;

		public RowActivatedArgs (int row, T rowValue)
		{
			this.row = row;
			row_value = rowValue;
		}

		public int Row {
			get { return row; }
		}

		public T RowValue {
			get { return row_value; }
		}
	}
}
