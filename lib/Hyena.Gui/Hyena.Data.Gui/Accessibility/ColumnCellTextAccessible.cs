//
// ColumnCellTextAccessible.cs
//
// Author:
//   Eitan Isaacson <eitan@ascender.com>
//
// Copyright (C) 2009 Eitan Isaacson.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Gui.Accessibility
{
	class ColumnCellTextAccessible : ColumnCellAccessible
	{
		public ColumnCellTextAccessible (object bound_object, ColumnCellText cell, ICellAccessibleParent parent) : base (bound_object, cell as ColumnCell, parent)
		{
			Name = cell.GetTextAlternative (bound_object);
		}
	}
}
