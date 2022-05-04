//
// ICellAccessibleParent.cs
//
// Author:
//   Eitan Isaacson <eitan@ascender.com>
//
// Copyright (C) 2009 Eitan Isaacson.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Gui.Accessibility
{
	public interface ICellAccessibleParent
	{
		Gdk.Rectangle GetCellExtents (ColumnCellAccessible cell, Atk.CoordType coord_type);
		int GetCellIndex (ColumnCellAccessible cell);
		bool IsCellShowing (ColumnCellAccessible cell);
		bool IsCellFocused (ColumnCellAccessible cell);
		bool IsCellSelected (ColumnCellAccessible cell);
		bool IsCellActive (ColumnCellAccessible cell);
		void InvokeColumnHeaderMenu (ColumnCellAccessible column);
		void ClickColumnHeader (ColumnCellAccessible column);
		void CellRedrawn (int column, int row);
	}
}
