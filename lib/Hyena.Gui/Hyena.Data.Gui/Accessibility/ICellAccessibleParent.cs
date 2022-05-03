//
// ICellAccessibleParent.cs
//
// Author:
//   Eitan Isaacson <eitan@ascender.com>
//
// Copyright (C) 2009 Eitan Isaacson.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
